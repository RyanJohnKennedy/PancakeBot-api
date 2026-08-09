using Microsoft.Extensions.Options;
using PancakeBot.Api.Option;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Handler;

public class TrackmaniaAuthHandler : DelegatingHandler
{
    private readonly ITrackmaniaAuthService _auth;
    private readonly TrackmaniaOptions _options;
    private readonly string _audience;

    public TrackmaniaAuthHandler(
        ITrackmaniaAuthService auth,
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
        var token = await _auth.GetAccessTokenAsync(_audience, cancellationToken);

        request.Headers.Remove("Authorization");
        request.Headers.Add("Authorization", $"nadeo_v1 t={token}");

        request.Headers.Remove("User-Agent");
        request.Headers.Add("User-Agent", _options.UserAgent);

        return await base.SendAsync(request, cancellationToken);
    }
}
