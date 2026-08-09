using PancakeBot.Api.model;
using Microsoft.Extensions.Options;
using PancakeBot.Api.Option;

namespace PancakeBot.Api.Service;

public class TrackmaniaOAuthService
{
    private readonly HttpClient _httpClient;

    private string? _accessToken;
    private DateTime _expiresAt;

    private readonly string _clientId;
    private readonly string _clientSecret;

    public TrackmaniaOAuthService(HttpClient httpClient, IOptions<TrackmaniaOptions> options)
    {
        _httpClient = httpClient;
        _clientId = options.Value.ClientId;
        _clientSecret = options.Value.ClientSecret;

        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            throw new InvalidOperationException(
                "Trackmania:ClientId and Trackmania:ClientSecret must be configured.");
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt)
            return _accessToken;

        var content = new FormUrlEncodedContent(new Dictionary<string,string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        });

        var response = await _httpClient.PostAsync("access_token", content);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TrackmaniaTokenResponse>();

        if (token == null || string.IsNullOrEmpty(token.AccessToken))
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to deserialize Trackmania token. Response body: {body}");
        }

        _accessToken = token.AccessToken;
        _expiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 60);

        return _accessToken;
    }
}
