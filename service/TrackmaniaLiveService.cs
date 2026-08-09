using PancakeBot.Api.Client;
using PancakeBot.Api.model.Leaderboard;
using Microsoft.Extensions.Options;
using PancakeBot.Api.Option;

namespace PancakeBot.Api.Service;

public class TrackmaniaLiveService
{
    private readonly ITrackmaniaLiveClient _live;
    private readonly TrackmaniaOptions _options;
    
    public TrackmaniaLiveService(ITrackmaniaLiveClient live, IOptions<TrackmaniaOptions> options)
    {
        _live = live;
        _options = options.Value;
    }

    public async Task<LeaderboardResponse> GetLeaderboard(
        string mapId,
        string? zoneId = null,
        int? length = null,
        int offset = 0,
        bool onlyWorld = true)
    {
        var data = await _live.GetLeaderboard(
            mapId,
            zoneId: zoneId ?? _options.GetDefaultRegionZoneId(),
            length: length ?? _options.DailyLeaderboardSize,
            offset: offset,
            onlyWorld: onlyWorld);
        
        return data!;
    }
}
