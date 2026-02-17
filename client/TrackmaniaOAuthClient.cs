using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Client;

public interface ITrackmaniaOAuthClient
{
    Task<Dictionary<string, string>> GetAccountName(params string[] accountIds);
}

public class TrackmaniaOAuthClient : ITrackmaniaOAuthClient
{
    private readonly HttpClient _http;
    private readonly TrackmaniaOAuthService _oauthService;

    public TrackmaniaOAuthClient(
        HttpClient http,
        TrackmaniaOAuthService oauthService)
    {
        _http = http;
        _oauthService = oauthService;
    }

    public async Task<Dictionary<string, string>> GetAccountName(params string[] accountIds)
    {
        if (accountIds == null || accountIds.Length == 0)
            throw new ArgumentException("At least one accountId is required.", nameof(accountIds));

        if (accountIds.Length > 50)
            throw new ArgumentException("Maximum of 50 accountIds allowed.", nameof(accountIds));

        var token = await _oauthService.GetAccessTokenAsync();
        Console.WriteLine($"Using access token: {(string.IsNullOrEmpty(token) ? "<empty>" : token + "...")}");

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var queryParams = accountIds
            .Select(id => new KeyValuePair<string, string?>("accountId[]", id));

        var url = new Uri(_http.BaseAddress!, "display-names");
        var fullUrl = QueryHelpers.AddQueryString(url.ToString(), queryParams);
        Console.WriteLine($"Request URL: {fullUrl}");

        var response = await _http.GetAsync(fullUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result!;
    }
}
