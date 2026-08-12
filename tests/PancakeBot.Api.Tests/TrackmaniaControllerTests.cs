using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PancakeBot.Api.Client;
using PancakeBot.Api.Controller;
using PancakeBot.Api.data;
using PancakeBot.Api.model.player;
using PancakeBot.Api.model.totd;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Tests;

[TestClass]
public class TrackmaniaControllerTests
{
    [TestMethod]
    public async Task GetTotdMonthLeaderboard_SumsCurrentMonthPointsOnly()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);

        db.Players.AddRange(
            new Player { AccountId = "alice", DisplayName = "Alice" },
            new Player { AccountId = "bob", DisplayName = "Bob" },
            new Player { AccountId = "cara", DisplayName = "Cara" },
            new Player { AccountId = "dan", DisplayName = "Dan" });
        db.TotdMaps.AddRange(
            new TotdMap { MapUid = "aug-1", TotdDate = new DateOnly(2026, 8, 1) },
            new TotdMap { MapUid = "aug-2", TotdDate = new DateOnly(2026, 8, 2) },
            new TotdMap { MapUid = "jul-31", TotdDate = new DateOnly(2026, 7, 31) });
        db.TotdResults.AddRange(
            new TotdResult { MapUid = "aug-1", AccountId = "alice", Rank = 1, PointsAwarded = 5 },
            new TotdResult { MapUid = "aug-2", AccountId = "alice", Rank = 2, PointsAwarded = 4 },
            new TotdResult { MapUid = "aug-1", AccountId = "bob", Rank = 2, PointsAwarded = 5 },
            new TotdResult { MapUid = "aug-1", AccountId = "cara", Rank = 2, PointsAwarded = 5 },
            new TotdResult { MapUid = "aug-1", AccountId = "dan", Rank = 3, PointsAwarded = 4 },
            new TotdResult { MapUid = "jul-31", AccountId = "bob", PointsAwarded = 99 });
        await db.SaveChangesAsync();

        var controller = new TrackmaniaController(
            null!, null!, null!, null!, db,
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero)),
            NullLogger<TrackmaniaController>.Instance);

        var actionResult = await controller.GetTotdMonthLeaderboard();
        var response = (TotdMonthLeaderboardResponse)((OkObjectResult)actionResult).Value!;

        Assert.AreEqual(new DateOnly(2026, 8, 1), response.Month);
        Assert.AreEqual(4, response.Results.Count);
        Assert.AreEqual("Alice", response.Results[0].PlayerName);
        Assert.AreEqual(1, response.Results[0].Rank);
        Assert.AreEqual(9, response.Results[0].PointsAwarded);
        Assert.AreEqual(1, response.Results[0].FirstPlaceCount);
        Assert.AreEqual(1, response.Results[0].SecondPlaceCount);
        Assert.AreEqual(0, response.Results[0].ThirdPlaceCount);
        Assert.AreEqual("Bob", response.Results[1].PlayerName);
        Assert.AreEqual(2, response.Results[1].Rank);
        Assert.AreEqual(5, response.Results[1].PointsAwarded);
        Assert.AreEqual(0, response.Results[1].FirstPlaceCount);
        Assert.AreEqual(1, response.Results[1].SecondPlaceCount);
        Assert.AreEqual("Cara", response.Results[2].PlayerName);
        Assert.AreEqual(2, response.Results[2].Rank);
        Assert.AreEqual("Dan", response.Results[3].PlayerName);
        Assert.AreEqual(4, response.Results[3].Rank);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
