using Microsoft.Extensions.Options;
using PancakeBot.Api.Option;
using PancakeBot.Api.Model.Response;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;

namespace PancakeBot.Api.Service;

public interface ITrackmaniaAuthService
{
    Task<string> GetAccessTokenAsync(string audience, CancellationToken cancellationToken = default);
}

/// <summary>Obtains and safely caches Nadeo service tokens per audience.</summary>
public sealed class TrackmaniaAuthService : ITrackmaniaAuthService
{
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromMinutes(1);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TrackmaniaOptions _options;
    private readonly ILogger<TrackmaniaAuthService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, TokenCacheEntry> _tokens = new(StringComparer.Ordinal);

    public TrackmaniaAuthService(
        IHttpClientFactory httpClientFactory,
        IOptions<TrackmaniaOptions> options,
        ILogger<TrackmaniaAuthService> logger,
        TimeProvider? timeProvider = null)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<string> GetAccessTokenAsync(
        string audience,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audience))
            throw new ArgumentException("A token audience is required.", nameof(audience));

        var entry = _tokens.GetOrAdd(audience, static _ => new TokenCacheEntry());
        if (entry.HasValidAccessToken(_timeProvider.GetUtcNow()))
            return entry.AccessToken!;

        await entry.Gate.WaitAsync(cancellationToken);
        try
        {
            if (entry.HasValidAccessToken(_timeProvider.GetUtcNow()))
                return entry.AccessToken!;

            NadeoTokenResponse token;
            if (!string.IsNullOrWhiteSpace(entry.RefreshToken))
            {
                try
                {
                    token = await RefreshTokenAsync(audience, entry.RefreshToken, cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    // A refresh token may have expired or been revoked. Re-authenticate below.
                    _logger.LogWarning(ex,
                        "Trackmania token refresh failed for audience {Audience}; requesting a new token.", audience);
                    entry.RefreshToken = null;
                    token = await RequestTokenAsync(audience, cancellationToken);
                }
            }
            else
            {
                token = await RequestTokenAsync(audience, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(token.AccessToken))
                throw new InvalidOperationException("Trackmania returned a token response without an access token.");

            entry.AccessToken = token.AccessToken;
            entry.RefreshToken = token.RefreshToken;
            entry.AccessTokenExpiresAt = _timeProvider.GetUtcNow().AddSeconds(
                Math.Max(0, token.ExpiresIn) - ExpirySkew.TotalSeconds);

            return entry.AccessToken;
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    private async Task<NadeoTokenResponse> RequestTokenAsync(string audience, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Login) || string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException("Trackmania:Login and Trackmania:Password must be configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "v2/authentication/token/basic")
        {
            Content = JsonContent.Create(new { audience })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_options.Login}:{_options.Password}")));
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);

        return await SendForTokenAsync(request, audience, "authentication", cancellationToken);
    }

    private async Task<NadeoTokenResponse> RefreshTokenAsync(
        string audience, string refreshToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "v2/authentication/token/refresh");
        request.Headers.Authorization = new AuthenticationHeaderValue("nadeo_v1", $"t={refreshToken}");
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);

        return await SendForTokenAsync(request, audience, "refresh", cancellationToken);
    }

    private async Task<NadeoTokenResponse> SendForTokenAsync(
        HttpRequestMessage request, string audience, string operation, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClientFactory.CreateClient("TrackmaniaAuth")
                .SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Trackmania {Operation} failed for audience {Audience} with status {StatusCode}.",
                    operation, audience, (int)response.StatusCode);
                throw new HttpRequestException($"Trackmania {operation} failed with status {(int)response.StatusCode}.", null,
                    response.StatusCode);
            }

            var token = await response.Content.ReadFromJsonAsync<NadeoTokenResponse>(cancellationToken: cancellationToken);
            return token ?? throw new InvalidOperationException("Trackmania returned an empty token response.");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex) when (ex is TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Trackmania {Operation} failed for audience {Audience}.", operation, audience);
            throw;
        }
    }

    private sealed class TokenCacheEntry
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTimeOffset AccessTokenExpiresAt { get; set; }

        public bool HasValidAccessToken(DateTimeOffset now) =>
            !string.IsNullOrWhiteSpace(AccessToken) && now < AccessTokenExpiresAt;
    }
}
