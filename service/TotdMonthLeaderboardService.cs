using Microsoft.EntityFrameworkCore;
using PancakeBot.Api.data;
using PancakeBot.Api.model.totd;

namespace PancakeBot.Api.Service;

public interface ITotdMonthLeaderboardService
{
    Task<TotdMonthLeaderboardResponse> GetCurrentMonthLeaderboardAsync();
}

public sealed class TotdMonthLeaderboardService : ITotdMonthLeaderboardService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public TotdMonthLeaderboardService(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<TotdMonthLeaderboardResponse> GetCurrentMonthLeaderboardAsync()
    {
        var currentDate = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var monthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var monthlyResults = await _db.TotdResults
            .AsNoTracking()
            .Where(result => result.TotdMap!.TotdDate >= monthStart && result.TotdMap.TotdDate < nextMonthStart)
            .GroupBy(result => new { result.AccountId, PlayerName = result.Player!.DisplayName })
            .Select(group => new
            {
                group.Key.AccountId,
                group.Key.PlayerName,
                PointsAwarded = group.Sum(result => result.PointsAwarded),
                FirstPlaceCount = group.Count(result => result.Rank == 1),
                SecondPlaceCount = group.Count(result => result.Rank == 2),
                ThirdPlaceCount = group.Count(result => result.Rank == 3),
                FourthPlaceCount = group.Count(result => result.Rank == 4),
                FifthPlaceCount = group.Count(result => result.Rank == 5)
            })
            .ToListAsync();

        var sortedResults = monthlyResults
            .OrderByDescending(result => result.PointsAwarded)
            .ThenByDescending(result => result.FirstPlaceCount)
            .ThenByDescending(result => result.SecondPlaceCount)
            .ThenByDescending(result => result.ThirdPlaceCount)
            .ThenByDescending(result => result.FourthPlaceCount)
            .ThenByDescending(result => result.FifthPlaceCount)
            .ThenBy(result => result.PlayerName)
            .ThenBy(result => result.AccountId)
            .ToList();

        var results = new List<TotdMonthLeaderboardResult>(sortedResults.Count);
        var previousTieBreaker = (PointsAwarded: int.MinValue, First: int.MinValue, Second: int.MinValue,
            Third: int.MinValue, Fourth: int.MinValue, Fifth: int.MinValue);
        var rank = 0;

        foreach (var result in sortedResults)
        {
            var tieBreaker = (result.PointsAwarded, result.FirstPlaceCount, result.SecondPlaceCount,
                result.ThirdPlaceCount, result.FourthPlaceCount, result.FifthPlaceCount);
            if (tieBreaker != previousTieBreaker)
                rank = results.Count + 1;

            results.Add(new TotdMonthLeaderboardResult(
                rank, result.PlayerName, result.PointsAwarded, result.FirstPlaceCount,
                result.SecondPlaceCount, result.ThirdPlaceCount, result.FourthPlaceCount,
                result.FifthPlaceCount));
            previousTieBreaker = tieBreaker;
        }

        return new TotdMonthLeaderboardResponse(monthStart, results);
    }
}
