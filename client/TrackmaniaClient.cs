namespace PancakeBot.Api.Client;

public class TrackmaniaClient
{
    private readonly HttpClient _http;

    public TrackmaniaClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetPlayerInfo(string accountId, string token)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://prod.trackmania.core.nadeo.online/accounts/{accountId}"
        );

        request.Headers.Add("Authorization", $"nadeo_v1 t={token}");
        request.Headers.Add("User-Agent", "My TM Tool / BigCheese / you@email.com");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
