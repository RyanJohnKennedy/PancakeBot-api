using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PancakeBot.Api.Option;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Tests;

[TestClass]
public class TrackmaniaAuthServiceTests
{
    [TestMethod]
    public async Task GetAccessTokenAsync_CachesValidTokenPerAudience()
    {
        var handler = new StubHandler(_ => JsonResponse("first-access", "first-refresh", 3600));
        var service = CreateService(handler, new TestTimeProvider(DateTimeOffset.UnixEpoch));

        var first = await service.GetAccessTokenAsync("NadeoLiveServices");
        var second = await service.GetAccessTokenAsync("NadeoLiveServices");

        Assert.AreEqual("first-access", first);
        Assert.AreEqual(first, second);
        Assert.AreEqual(1, handler.RequestCount);
        Assert.AreEqual("/v2/authentication/token/basic", handler.Requests[0].PathAndQuery);
    }

    [TestMethod]
    public async Task GetAccessTokenAsync_RefreshesExpiredToken()
    {
        var responses = new Queue<HttpResponseMessage>([
            JsonResponse("initial-access", "initial-refresh", 3600),
            JsonResponse("refreshed-access", "refreshed-refresh", 3600)
        ]);
        var handler = new StubHandler(_ => responses.Dequeue());
        var clock = new TestTimeProvider(DateTimeOffset.UnixEpoch);
        var service = CreateService(handler, clock);

        await service.GetAccessTokenAsync("NadeoLiveServices");
        clock.Advance(TimeSpan.FromHours(1));
        var token = await service.GetAccessTokenAsync("NadeoLiveServices");

        Assert.AreEqual("refreshed-access", token);
        Assert.AreEqual(2, handler.RequestCount);
        Assert.AreEqual("/v2/authentication/token/refresh", handler.Requests[1].PathAndQuery);
        Assert.AreEqual("nadeo_v1", handler.Requests[1].Scheme);
        Assert.AreEqual("t=initial-refresh", handler.Requests[1].Parameter);
    }

    private static TrackmaniaAuthService CreateService(StubHandler handler, TimeProvider clock) => new(
        new StubHttpClientFactory(handler),
        Options.Create(new TrackmaniaOptions
        {
            CoreUrl = "https://prod.trackmania.core.nadeo.online/",
            Login = "server-login",
            Password = "server-password",
            UserAgent = "PancakeBot/1.0"
        }),
        NullLogger<TrackmaniaAuthService>.Instance,
        clock);

    private static HttpResponseMessage JsonResponse(string accessToken, string refreshToken, int expiresIn) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            $$"""{"accessToken":"{{accessToken}}","refreshToken":"{{refreshToken}}","expiresIn":{{expiresIn}}}""",
            Encoding.UTF8,
            "application/json")
    };

    private sealed class StubHttpClientFactory(StubHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler) { BaseAddress = new Uri("https://prod.trackmania.core.nadeo.online/") };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public List<(string PathAndQuery, string? Scheme, string? Parameter)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            AuthenticationHeaderValue? authorization = request.Headers.Authorization;
            Requests.Add((request.RequestUri!.PathAndQuery, authorization?.Scheme, authorization?.Parameter));
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan amount) => _now = _now.Add(amount);
    }
}
