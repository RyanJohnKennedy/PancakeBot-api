namespace PancakeBot.Api.Client;

public interface ITrackmaniaCoreClient
{
    Task<string> GetMapInfo(string mapId);
}

public class TrackmaniaCoreClient : ITrackmaniaCoreClient
{
    private readonly HttpClient _http;

    public TrackmaniaCoreClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetMapInfo(string mapId)
    {
        var response = await _http.GetAsync(
            $"/maps/{mapId}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
