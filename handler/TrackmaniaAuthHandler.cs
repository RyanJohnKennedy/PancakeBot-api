using Microsoft.Extensions.Options;
using PancakeBot.Api.Option;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Handler;

public class TrackmaniaAuthHandler : DelegatingHandler
{
    private readonly TrackmaniaAuthService _auth;
    private readonly TrackmaniaOptions _options;
    private readonly string _audience;

    private string? _token;
    private DateTime _expires;

    public TrackmaniaAuthHandler(
        TrackmaniaAuthService auth,
        IOptions<TrackmaniaOptions> options,
        string audience)
    {
        _auth = auth;
        _options = options.Value;
        _audience = audience;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_token == null || DateTime.UtcNow >= _expires)
        {
            var token = await _auth.GetTokenAsync(
                _audience,
                _options.Login,
                _options.Password);

            _token = token.AccessToken;
            _expires = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 60);
        }

        request.Headers.Remove("Authorization");
        request.Headers.Add("Authorization", $"nadeo_v1 t={_token}");

        request.Headers.Remove("User-Agent");
        request.Headers.Add("User-Agent", _options.UserAgent);

        if (!string.IsNullOrWhiteSpace(_options.Email))
        {
            var encodedEmail = Uri.EscapeDataString(_options.Email);
            request.Headers.Remove("X-User-Email");
            request.Headers.Add("X-User-Email", encodedEmail);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}