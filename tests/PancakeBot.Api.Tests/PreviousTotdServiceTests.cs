using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PancakeBot.Api.Client;
using PancakeBot.Api.data;
using PancakeBot.Api.model.Leaderboard;
using PancakeBot.Api.model.totd;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Tests;

[TestClass]
public class PreviousTotdServiceTests
{
    [DataTestMethod]
    [DataRow("2026-07-15T16:59:00Z", "2026-07-15T17:00:00Z")]
    [DataRow("2026-12-15T17:59:00Z", "2026-12-15T18:00:00Z")]
    public async Task GetPreviousTotd_BeforeFranceCutoff_ExcludesActiveTotd(string now, string activeEnd)
    {
        var result = await GetPreviousTotdAsync(now, activeEnd);

        Assert.AreEqual("previous", result!.MapUid);
    }

    [DataTestMethod]
    [DataRow("2026-07-15T17:00:00Z", "2026-07-15T17:00:00Z")]
    [DataRow("2026-12-15T18:00:00Z", "2026-12-15T18:00:00Z")]
    public async Task GetPreviousTotd_AtFranceCutoff_SelectsJustFinishedTotd(string now, string activeEnd)
    {
        var result = await GetPreviousTotdAsync(now, activeEnd);

        Assert.AreEqual("just-finished", result!.MapUid);
    }

    private static async Task<TotdDay?> GetPreviousTotdAsync(string now, string activeEnd)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var activeEndTimestamp = DateTimeOffset.Parse(activeEnd).ToUnixTimeSeconds();
        var response = new TotdMonthResponse
        {
            MonthList =
            [
                new TotdMonth
                {
                    Days =
                    [
                        new TotdDay { MapUid = "previous", StartTimestamp = activeEndTimestamp - 172800, EndTimestamp = activeEndTimestamp - 86400 },
                        new TotdDay { MapUid = "just-finished", StartTimestamp = activeEndTimestamp - 86400, EndTimestamp = activeEndTimestamp },
                        new TotdDay { MapUid = "current", StartTimestamp = activeEndTimestamp, EndTimestamp = activeEndTimestamp + 86400 }
                    ]
                }
            ]
        };
        var service = new PreviousTotdService(
            new LiveClientStub(response),
            NullLogger<PreviousTotdService>.Instance,
            db,
            new TestTimeProvider(DateTimeOffset.Parse(now)));

        return await service.GetPreviousTotd();
    }

    private sealed class LiveClientStub(TotdMonthResponse response) : ITrackmaniaLiveClient
    {
        public Task<TotdMonthResponse?> GetTotdMonth(int offset = 0, int length = 2) => Task.FromResult<TotdMonthResponse?>(response);
        public Task<LeaderboardResponse?> GetLeaderboard(string mapId, string groupUid = "Personal_Best", int length = 5, int offset = 0, bool onlyWorld = true, string? zoneId = null) => throw new NotSupportedException();
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
