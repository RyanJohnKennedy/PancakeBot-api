using PancakeBot.Api.Model.Response;
using System.Text;
using Microsoft.Extensions.Options;
using PancakeBot.Api.Option;

namespace PancakeBot.Api.Service;

public class TrackmaniaAuthService
{
    private readonly HttpClient _http;
    private readonly TrackmaniaOptions _options;

    public TrackmaniaAuthService(
        HttpClient http,
        IOptions<TrackmaniaOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<NadeoTokenResponse> GetTokenAsync(
        string audience,
        string login,
        string password)
    {
        var authValue = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{login}:{password}")
        );

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"{_options.CoreUrl}/v2/authentication/token/basic")
        );

        request.Headers.Add("Authorization", $"Basic {authValue}");
        request.Headers.Add("User-Agent", _options.UserAgent);

        var encodedEmail = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(_options.Email)
        );
        request.Headers.Add("X-User-Email", encodedEmail);

        request.Content = JsonContent.Create(new
        {
            audience
        });

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Trackmania auth failed: {response.StatusCode} - {error}");
        }

        var token = await response.Content
            .ReadFromJsonAsync<NadeoTokenResponse>();

        if (token == null || string.IsNullOrEmpty(token.AccessToken))
            throw new Exception("Invalid token response from Trackmania");

        return token;
    }
}