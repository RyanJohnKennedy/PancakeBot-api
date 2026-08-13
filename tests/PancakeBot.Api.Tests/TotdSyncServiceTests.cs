using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PancakeBot.Api.Client;
using PancakeBot.Api.data;
using PancakeBot.Api.model.Leaderboard;
using PancakeBot.Api.model.player;
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

    [TestMethod]
    public async Task SyncCurrentMonthCompletedTotdsAsync_SyncsOnlyCompletedDaysWithoutStoredResults()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var completedDay = new TotdDay
        {
            MapUid = "aug-1",
            StartTimestamp = DateTimeOffset.Parse("2026-08-01T18:00:00Z").ToUnixTimeSeconds(),
            EndTimestamp = DateTimeOffset.Parse("2026-08-02T18:00:00Z").ToUnixTimeSeconds()
        };
        var processedDay = new TotdDay
        {
            MapUid = "aug-2",
            StartTimestamp = DateTimeOffset.Parse("2026-08-02T18:00:00Z").ToUnixTimeSeconds(),
            EndTimestamp = DateTimeOffset.Parse("2026-08-03T18:00:00Z").ToUnixTimeSeconds()
        };
        var activeDay = new TotdDay
        {
            MapUid = "aug-3",
            StartTimestamp = DateTimeOffset.Parse("2026-08-03T18:00:00Z").ToUnixTimeSeconds(),
            EndTimestamp = DateTimeOffset.Parse("2026-08-04T18:00:00Z").ToUnixTimeSeconds()
        };
        var previousMonthDay = new TotdDay
        {
            MapUid = "jul-31",
            StartTimestamp = DateTimeOffset.Parse("2026-07-31T18:00:00Z").ToUnixTimeSeconds(),
            EndTimestamp = DateTimeOffset.Parse("2026-08-01T18:00:00Z").ToUnixTimeSeconds()
        };
        db.Players.Add(new Player { AccountId = "saved-player", DisplayName = "Saved Player" });
        db.TotdMaps.Add(new TotdMap { MapUid = processedDay.MapUid, TotdDate = new DateOnly(2026, 8, 2) });
        db.TotdResults.Add(new TotdResult { MapUid = processedDay.MapUid, AccountId = "saved-player" });
        await db.SaveChangesAsync();

        var service = new TotdSyncService(
            new PreviousTotdStub(null),
            new LiveClientStub(new TotdMonthResponse
            {
                MonthList = [new TotdMonth { Days = [completedDay, processedDay, activeDay, previousMonthDay] }]
            }),
            new OAuthClientStub(),
            db,
            Options.Create(new TrackmaniaOptions { DailyLeaderboardSize = 5 }),
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 3, 18, 0, 0, TimeSpan.Zero)),
            NullLogger<TotdSyncService>.Instance);

        var result = await service.SyncCurrentMonthCompletedTotdsAsync();

        Assert.AreEqual(2, result.CompletedDaysFound);
        Assert.AreEqual("aug-1", result.SyncedDays.Single().MapUid);
        Assert.AreEqual(2, await db.TotdMaps.CountAsync());
        Assert.AreEqual(3, await db.TotdResults.CountAsync());
    }

    private sealed class PreviousTotdStub(TotdDay? totd) : IPreviousTotdService
    {
        public Task<TotdDay?> GetPreviousTotd() => Task.FromResult<TotdDay?>(totd);
    }

    private sealed class LiveClientStub(TotdMonthResponse? monthResponse = null) : ITrackmaniaLiveClient
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

        public Task<TotdMonthResponse?> GetTotdMonth(int offset = 0, int length = 2) => Task.FromResult(monthResponse);
    }

    private sealed class OAuthClientStub : ITrackmaniaOAuthClient
    {
        public Task<Dictionary<string, string>> GetAccountName(params string[] accountIds) => Task.FromResult(
            accountIds.ToDictionary(accountId => accountId, accountId => $"name-{accountId}"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
