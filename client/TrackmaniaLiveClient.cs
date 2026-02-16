using Microsoft.AspNetCore.WebUtilities;
using PancakeBot.Api.model.Leaderboard;
using PancakeBot.Api.model.totd;

namespace PancakeBot.Api.Client;

public interface ITrackmaniaLiveClient
{
    Task<LeaderboardResponse?> GetLeaderboard(string mapId,
        string groupUid = "Personal_Best",
        int length = 5,
        int offset = 0,
        bool onlyWorld = true,
        string? zoneId = null);
    
    Task<TotdMonthResponse?> GetTotdMonth();
}

public class TrackmaniaLiveClient : ITrackmaniaLiveClient
{
    private readonly HttpClient _http;

    public TrackmaniaLiveClient(HttpClient http)
    {
        _http = http;
    }
    
    public async Task<LeaderboardResponse?> GetLeaderboard(
        string mapId,
        string groupUid = "Personal_Best",
        int length = 5,
        int offset = 0,
        bool onlyWorld = true,
        string? zoneId = null)
    {
        var path =
            $"token/leaderboard/group/{groupUid}/map/{mapId}/top";

        var query = new Dictionary<string, string?>
        {
            ["length"] = length.ToString(),
            ["offset"] = offset.ToString(),
            ["onlyWorld"] = onlyWorld.ToString().ToLower()
        };

        if (!string.IsNullOrWhiteSpace(zoneId) && !onlyWorld)
        {
            query["zoneId"] = zoneId;
        }

        var url = QueryHelpers.AddQueryString(path, query);

        var response = await _http.GetAsync(url);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<LeaderboardResponse>();
    }
    
    public async Task<TotdMonthResponse?> GetTotdMonth()
    {
        var response = await _http.GetAsync("token/campaign/month?length=1");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TotdMonthResponse>();
    }

}
