using Microsoft.EntityFrameworkCore;
using PancakeBot.Api.data;
using PancakeBot.Api.model.totd;

namespace PancakeBot.Api.Service;

public interface ITotdLeaderboardService
{
    Task<TotdLeaderboardResponse?> GetLatestCompletedLeaderboardAsync();
}

public sealed class TotdLeaderboardService : ITotdLeaderboardService
{
    private readonly AppDbContext _db;
    private readonly ITotdSyncService _totdSyncService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TotdLeaderboardService> _logger;

    public TotdLeaderboardService(
        AppDbContext db,
        ITotdSyncService totdSyncService,
        TimeProvider timeProvider,
        ILogger<TotdLeaderboardService> logger)
    {
        _db = db;
        _totdSyncService = totdSyncService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TotdLeaderboardResponse?> GetLatestCompletedLeaderboardAsync()
    {
        var nowTimestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var map = await _db.TotdMaps
            .AsNoTracking()
            .Where(totdMap => totdMap.EndTimestamp <= nowTimestamp)
            .OrderByDescending(totdMap => totdMap.EndTimestamp)
            .FirstOrDefaultAsync();

        if (map is null || !await _db.TotdResults.AnyAsync(result => result.MapUid == map.MapUid))
        {
            _logger.LogInformation(
                "No stored results found for the most recently completed TOTD; fetching and syncing from Trackmania.");

            var syncResult = await _totdSyncService.SyncPreviousTotdAsync();
            if (syncResult is null)
            {
                _logger.LogWarning("Trackmania did not return a completed TOTD.");
                return null;
            }

            map = await _db.TotdMaps
                .AsNoTracking()
                .SingleAsync(totdMap => totdMap.MapUid == syncResult.MapUid);
        }
        else
        {
            _logger.LogInformation(
                "Using stored TOTD results for {TotdDate} (map {MapUid}); no Trackmania request is needed.",
                map.TotdDate,
                map.MapUid);
        }

        var results = await _db.TotdResults
            .AsNoTracking()
            .Where(result => result.MapUid == map.MapUid)
            .OrderBy(result => result.Rank)
            .Select(result => new TotdLeaderboardResult(
                result.Rank,
                result.Player!.DisplayName,
                result.Score,
                result.PointsAwarded))
            .ToListAsync();

        _logger.LogInformation("Returning {ResultCount} TOTD results for map {MapUid}.", results.Count, map.MapUid);

        return new TotdLeaderboardResponse(map.MapUid, map.MapName, map.TotdDate, results);
    }
}
