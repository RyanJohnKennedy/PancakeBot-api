using PancakeBot.Api.Client;
using PancakeBot.Api.model.Leaderboard;
using PancakeBot.Api.model.totd;

namespace PancakeBot.Api.Service;

public class TrackmaniaLiveService
{
    private readonly ITrackmaniaLiveClient _live;
    
    public TrackmaniaLiveService(ITrackmaniaLiveClient live)
    {
        _live = live;
    }

    public async Task<TotdDay?> GetPreviousTotd()
    {
        var data = await _live.GetTotdMonth();

        var month = data?.MonthList.FirstOrDefault();
        if (month == null) return null;

        return month.Days
            .Where(d => !string.IsNullOrWhiteSpace(d.MapUid))
            .OrderByDescending(d => d.StartTimestamp)
            .Skip(1)
            .FirstOrDefault();
    }

    public async Task<LeaderboardResponse> GetLeaderboard(string mapId, string zoneId = "", int length = 5, int offset = 0, bool onlyWorld = true )
    {
        var data = await _live.GetLeaderboard(mapId, zoneId: zoneId, length: length, offset: offset, onlyWorld: onlyWorld);
        
        return data!;
    }
}