using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PancakeBot.Api.Client;
using PancakeBot.Api.data;
using PancakeBot.Api.model.Leaderboard;
using PancakeBot.Api.model.player;
using PancakeBot.Api.model.totd;
using PancakeBot.Api.Option;

namespace PancakeBot.Api.Service;

public interface ITotdSyncService
{
    Task<TotdSyncResult?> SyncPreviousTotdAsync();
    Task<TotdMonthSyncResult> SyncCurrentMonthCompletedTotdsAsync();
}

public sealed record TotdSyncResult(string MapUid, int ResultsUpserted, int PlayersUpserted);
public sealed record TotdMonthSyncResult(int CompletedDaysFound, IReadOnlyList<TotdSyncResult> SyncedDays);

/// <summary>Synchronizes the previous TOTD's leaderboard and participating players.</summary>
public sealed class TotdSyncService : ITotdSyncService
{
    private const int AccountLookupBatchSize = 50;
    private readonly IPreviousTotdService _previousTotd;
    private readonly ITrackmaniaLiveClient _live;
    private readonly ITrackmaniaOAuthClient _oauth;
    private readonly AppDbContext _db;
    private readonly TrackmaniaOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TotdSyncService> _logger;

    public TotdSyncService(
        IPreviousTotdService previousTotd,
        ITrackmaniaLiveClient live,
        ITrackmaniaOAuthClient oauth,
        AppDbContext db,
        IOptions<TrackmaniaOptions> options,
        TimeProvider timeProvider,
        ILogger<TotdSyncService> logger)
    {
        _previousTotd = previousTotd;
        _live = live;
        _oauth = oauth;
        _db = db;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TotdSyncResult?> SyncPreviousTotdAsync()
    {
        var totd = await _previousTotd.GetPreviousTotd();
        if (totd is null)
            return null;

        return await SyncTotdAsync(totd);
    }

    public async Task<TotdMonthSyncResult> SyncCurrentMonthCompletedTotdsAsync()
    {
        var now = _timeProvider.GetUtcNow();
        var currentMonth = DateOnly.FromDateTime(now.UtcDateTime);
        var monthData = await _live.GetTotdMonth();

        // The latest completed day is the same "previous TOTD" used by the /api/trackmania/totd endpoint.
        // Work backwards from it, never touching the currently active track.
        var completedDays = monthData?.MonthList
            .SelectMany(month => month.Days)
            .Where(day => !string.IsNullOrWhiteSpace(day.MapUid))
            .Where(day => DateOnly.FromDateTime(ToUtcDateTime(day.StartTimestamp)).Year == currentMonth.Year
                && DateOnly.FromDateTime(ToUtcDateTime(day.StartTimestamp)).Month == currentMonth.Month)
            .Where(day => ToDateTimeOffset(day.EndTimestamp) <= now)
            .OrderByDescending(day => day.StartTimestamp)
            .ToList()
            ?? [];

        var completedMapUids = completedDays.Select(day => day.MapUid).ToArray();
        var mapUidsWithStoredResults = (await _db.TotdResults
            .Where(result => completedMapUids.Contains(result.MapUid))
            .Select(result => result.MapUid)
            .Distinct()
            .ToListAsync())
            .ToHashSet();
        var unsyncedDays = completedDays
            .Where(day => !mapUidsWithStoredResults.Contains(day.MapUid))
            .ToList();

        var syncedDays = new List<TotdSyncResult>(unsyncedDays.Count);
        foreach (var day in unsyncedDays)
            syncedDays.Add(await SyncTotdAsync(day));

        _logger.LogInformation(
            "Synchronized {SyncedDayCount} previously unsaved TOTDs out of {CompletedDayCount} completed TOTDs for {Year}-{Month:D2}.",
            syncedDays.Count,
            completedDays.Count,
            currentMonth.Year,
            currentMonth.Month);

        return new TotdMonthSyncResult(completedDays.Count, syncedDays);
    }

    private async Task<TotdSyncResult> SyncTotdAsync(TotdDay totd)
    {
        await EnsureTotdMapAsync(totd);

        var leaderboard = await _live.GetLeaderboard(
            totd.MapUid,
            length: _options.DailyLeaderboardSize,
            onlyWorld: false,
            zoneId: _options.GetDefaultRegionZoneId());

        var entries = FlattenEntries(leaderboard).ToList();
        if (entries.Count == 0)
        {
            _logger.LogWarning("No leaderboard entries were returned for TOTD {MapUid}.", totd.MapUid);
            return new TotdSyncResult(totd.MapUid, 0, 0);
        }

        var accountIds = entries.Select(entry => entry.Entry.AccountId).Distinct(StringComparer.Ordinal).ToArray();
        var existingPlayers = await _db.Players
            .Where(player => accountIds.Contains(player.AccountId))
            .ToDictionaryAsync(player => player.AccountId);
        var displayNames = await GetDisplayNamesAsync(accountIds);

        var playersUpserted = 0;
        foreach (var groupedEntry in entries.GroupBy(item => item.Entry.AccountId, StringComparer.Ordinal))
        {
            var item = groupedEntry.OrderBy(entry => entry.Entry.Position).First();
            if (!existingPlayers.TryGetValue(item.Entry.AccountId, out var player))
            {
                if (!displayNames.TryGetValue(item.Entry.AccountId, out var displayName) || string.IsNullOrWhiteSpace(displayName))
                {
                    _logger.LogWarning("Skipping TOTD result for account {AccountId}: no display name was returned.", item.Entry.AccountId);
                    continue;
                }

                player = new Player
                {
                    AccountId = item.Entry.AccountId,
                    DisplayName = displayName,
                    CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
                };
                _db.Players.Add(player);
                existingPlayers[player.AccountId] = player;
            }

            if (displayNames.TryGetValue(player.AccountId, out var latestDisplayName) && !string.IsNullOrWhiteSpace(latestDisplayName))
                player.DisplayName = latestDisplayName;

            player.ZoneId = item.Entry.ZoneId ?? item.TopZone.ZoneId;
            player.ZoneName = item.Entry.ZoneName ?? item.TopZone.ZoneName;
            player.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            playersUpserted++;
        }

        var eligibleEntries = entries
            .Where(item => existingPlayers.ContainsKey(item.Entry.AccountId))
            .GroupBy(item => item.Entry.AccountId, StringComparer.Ordinal)
            .Select(group => group.OrderBy(item => item.Entry.Position).First())
            .ToList();

        var existingResults = await _db.TotdResults
            .Where(result => result.MapUid == totd.MapUid && accountIds.Contains(result.AccountId))
            .ToDictionaryAsync(result => result.AccountId);

        foreach (var item in eligibleEntries)
        {
            if (!existingResults.TryGetValue(item.Entry.AccountId, out var result))
            {
                result = new TotdResult
                {
                    MapUid = totd.MapUid,
                    AccountId = item.Entry.AccountId,
                    CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
                };
                _db.TotdResults.Add(result);
            }

            result.Rank = item.Entry.Position;
            result.Score = item.Entry.Score;
            result.Timestamp = item.Entry.Timestamp;
            result.PointsAwarded = _options.PointsByPosition.GetValueOrDefault(item.Entry.Position);
        }

        await _db.SaveChangesAsync();
        return new TotdSyncResult(totd.MapUid, eligibleEntries.Count, playersUpserted);
    }

    private async Task<Dictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> accountIds)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var batch in accountIds.Chunk(AccountLookupBatchSize))
        {
            var result = await _oauth.GetAccountName(batch);
            foreach (var (accountId, displayName) in result)
                names[accountId] = displayName;
        }

        return names;
    }

    private async Task EnsureTotdMapAsync(TotdDay day)
    {
        var map = await _db.TotdMaps.SingleOrDefaultAsync(map => map.MapUid == day.MapUid);
        if (map is null)
        {
            map = new TotdMap { MapUid = day.MapUid };
            _db.TotdMaps.Add(map);
        }

        var start = ToUtcDateTime(day.StartTimestamp);
        map.MapName = day.MapName;
        map.TotdDate = DateOnly.FromDateTime(start);
        map.Year = start.Year;
        map.Month = start.Month;
        map.CampaignId = day.CampaignId;
        map.CampaignUid = day.CampaignUid;
        map.SeasonUid = day.SeasonUid;
        map.StartTimestamp = day.StartTimestamp;
        map.EndTimestamp = day.EndTimestamp;
    }

    private static DateTime ToUtcDateTime(long timestamp) => timestamp > 10_000_000_000
        ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime
        : DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

    private static DateTimeOffset ToDateTimeOffset(long timestamp) => timestamp > 10_000_000_000
        ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
        : DateTimeOffset.FromUnixTimeSeconds(timestamp);

    private static IEnumerable<(LeaderboardEntry Entry, LeaderboardTopZone TopZone)> FlattenEntries(LeaderboardResponse? leaderboard) =>
        leaderboard?.Tops
            .Where(topZone => topZone.Top is not null)
            .SelectMany(topZone => topZone.Top
                .Where(entry => !string.IsNullOrWhiteSpace(entry.AccountId))
                .Select(entry => (entry, topZone)))
        ?? [];
}
