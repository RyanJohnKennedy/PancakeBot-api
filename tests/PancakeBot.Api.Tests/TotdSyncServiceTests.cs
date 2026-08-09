using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PancakeBot.Api.Client;
using PancakeBot.Api.data;
using PancakeBot.Api.model.Leaderboard;
using PancakeBot.Api.model.totd;
using PancakeBot.Api.Option;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Tests;

[TestClass]
public class TotdSyncServiceTests
{
    [TestMethod]
    public async Task SyncPreviousTotdAsync_UpsertsMapPlayersAndResults()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var totd = new TotdDay
        {
            MapUid = "map-uid",
            MapName = "Yesterday's Map",
            CampaignId = 42,
            CampaignUid = "campaign-uid",
            SeasonUid = "season-uid",
            StartTimestamp = DateTimeOffset.Parse("2026-08-08T18:00:00Z").ToUnixTimeSeconds()
        };
        var service = new TotdSyncService(
            new PreviousTotdStub(totd),
            new LiveClientStub(),
            new OAuthClientStub(),
            db,
            Options.Create(new TrackmaniaOptions
            {
                DailyLeaderboardSize = 5,
                PointsByPosition = new Dictionary<int, int> { [1] = 5, [2] = 4 }
            }),
            TimeProvider.System,
            NullLogger<TotdSyncService>.Instance);

        var first = await service.SyncPreviousTotdAsync();
        var second = await service.SyncPreviousTotdAsync();

        Assert.AreEqual("map-uid", first!.MapUid);
        Assert.AreEqual(2, first.ResultsUpserted);
        Assert.AreEqual(1, await db.TotdMaps.CountAsync());
        Assert.AreEqual(2, await db.Players.CountAsync());
        Assert.AreEqual(2, await db.TotdResults.CountAsync());
        Assert.AreEqual(5, (await db.TotdResults.SingleAsync(result => result.AccountId == "player-1")).PointsAwarded);
        Assert.AreEqual(2, second!.ResultsUpserted);
        Assert.AreEqual(2, await db.TotdResults.CountAsync());
    }

    private sealed class PreviousTotdStub(TotdDay totd) : IPreviousTotdService
    {
        public Task<TotdDay?> GetPreviousTotd() => Task.FromResult<TotdDay?>(totd);
    }

    private sealed class LiveClientStub : ITrackmaniaLiveClient
    {
        public Task<LeaderboardResponse?> GetLeaderboard(string mapId, string groupUid = "Personal_Best", int length = 5,
            int offset = 0, bool onlyWorld = true, string? zoneId = null) =>
            Task.FromResult<LeaderboardResponse?>(new LeaderboardResponse
            {
                MapUid = mapId,
                Tops =
                [
                    new LeaderboardTopZone
                    {
                        ZoneId = "zone-1",
                        ZoneName = "South Africa",
                        Top =
                        [
                            new LeaderboardEntry { AccountId = "player-1", Position = 1, Score = 1000, Timestamp = 10 },
                            new LeaderboardEntry { AccountId = "player-2", Position = 2, Score = 900, Timestamp = 11 }
                        ]
                    }
                ]
            });

        public Task<TotdMonthResponse?> GetTotdMonth(int offset = 0, int length = 2) => Task.FromResult<TotdMonthResponse?>(null);
    }

    private sealed class OAuthClientStub : ITrackmaniaOAuthClient
    {
        public Task<Dictionary<string, string>> GetAccountName(params string[] accountIds) => Task.FromResult(
            accountIds.ToDictionary(accountId => accountId, accountId => $"name-{accountId}"));
    }
}
