using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PancakeBot.Api.Client;
using PancakeBot.Api.data;
using PancakeBot.Api.model.totd;

namespace PancakeBot.Api.Service;

public interface IPreviousTotdService
{
    Task<TotdDay?> GetPreviousTotd();
}

public class PreviousTotdService : IPreviousTotdService
{
    private readonly ITrackmaniaLiveClient _live;
    private readonly ILogger<PreviousTotdService> _logger;
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PreviousTotdService(
        ITrackmaniaLiveClient live,
        ILogger<PreviousTotdService> logger,
        AppDbContext db,
        TimeProvider timeProvider)
    {
        _live = live;
        _logger = logger;
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<TotdDay?> GetPreviousTotd()
    {
        TotdMonthResponse? data;

        try
        {
            data = await _live.GetTotdMonth();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to fetch TOTD month data.");
            return null;
        }

        if (data?.MonthList is null || data.MonthList.Count == 0)
        {
            _logger.LogWarning("TOTD month response did not contain any months.");
            return null;
        }
        
        var now = _timeProvider.GetUtcNow();
        var previousTotd = data.MonthList
            .Where(month => month.Days is { Count: > 0 })
            .SelectMany(month => month.Days)
            .Where(day => !string.IsNullOrWhiteSpace(day.MapUid))
            .Where(day => ToDateTimeOffset(day.EndTimestamp) <= now)
            .OrderByDescending(day => ToDateTimeOffset(day.EndTimestamp))
            .FirstOrDefault();

        if (previousTotd is null)
        {
            _logger.LogWarning("Unable to determine previous TOTD from month response.");
            return null;
        }

        await StoreMapAsync(previousTotd);
        return previousTotd;
    }

    private async Task StoreMapAsync(TotdDay day)
    {
        var map = await _db.TotdMaps.SingleOrDefaultAsync(map => map.MapUid == day.MapUid);
        if (map is null)
        {
            map = new TotdMap { MapUid = day.MapUid };
            _db.TotdMaps.Add(map);
        }

        map.MapName = day.MapName;
        map.TotdDate = DateOnly.FromDateTime(ToUtcDateTime(day.StartTimestamp));
        map.Year = map.TotdDate.Year;
        map.Month = map.TotdDate.Month;
        map.CampaignId = day.CampaignId;
        map.CampaignUid = day.CampaignUid;
        map.SeasonUid = day.SeasonUid;
        map.StartTimestamp = day.StartTimestamp;
        map.EndTimestamp = day.EndTimestamp;

        await _db.SaveChangesAsync();
    }

    private static DateTime ToUtcDateTime(long timestamp)
    {
        return ToDateTimeOffset(timestamp).UtcDateTime;
    }

    private static DateTimeOffset ToDateTimeOffset(long timestamp)
    {
        return timestamp > 10_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
            : DateTimeOffset.FromUnixTimeSeconds(timestamp);
    }
}
