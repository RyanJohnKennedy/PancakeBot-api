using System.Text.Json;
using PancakeBot.Api.Client;
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

    public PreviousTotdService(
        ITrackmaniaLiveClient live,
        ILogger<PreviousTotdService> logger)
    {
        _live = live;
        _logger = logger;
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

        var previousTotd = data.MonthList
            .Where(month => month.Days is { Count: > 0 })
            .SelectMany(month => month.Days)
            .Where(day => !string.IsNullOrWhiteSpace(day.MapUid))
            .OrderByDescending(day => day.StartTimestamp)
            .Skip(1)
            .FirstOrDefault();

        if (previousTotd is null)
        {
            _logger.LogWarning("Unable to determine previous TOTD from month response.");
        }

        return previousTotd;
    }
}
