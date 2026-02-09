namespace PancakeBot.Api.Client;

public interface ITrackmaniaLiveClient
{
    Task<string> GetLeaderboard(string mapId);
    Task<string> GetTotdMonth();
}

public class TrackmaniaLiveClient : ITrackmaniaLiveClient
{
    private readonly HttpClient _http;

    public TrackmaniaLiveClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetLeaderboard(string mapId)
    {
        var response = await _http.GetAsync(
            $"/leaderboard/map/{mapId}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
    
    public async Task<string> GetTotdMonth()
    {
        var response = await _http.GetAsync(
            $"token/campaign/month?length=1");
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
}
