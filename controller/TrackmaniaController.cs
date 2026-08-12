using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PancakeBot.Api.Client;
using PancakeBot.Api.data;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Controller;

[ApiController]
[Route("api/trackmania")]
public class TrackmaniaController : ControllerBase
{
    private readonly ITrackmaniaLiveClient _live;
    private readonly ITrackmaniaCoreClient _core;
    private readonly ITrackmaniaOAuthClient _oauth;
    private readonly ITotdSyncService _totdSyncService;
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TrackmaniaController> _logger;

    public TrackmaniaController(
        ITrackmaniaLiveClient live,
        ITrackmaniaCoreClient core,
        ITrackmaniaOAuthClient oauth,
        ITotdSyncService totdSyncService,
        AppDbContext db,
        TimeProvider timeProvider,
        ILogger<TrackmaniaController> logger)
    {
        _live = live;
        _core = core;
        _oauth = oauth;
        _totdSyncService = totdSyncService;
        _db = db;
        _timeProvider = timeProvider;
        _logger = logger;
    }
    
    [HttpGet("totd-month")]
    public async Task<IActionResult> GetMonth()
    {
        var result = await _live.GetTotdMonth();

        return Ok(result);
    }

    [HttpGet("totd-month/leaderboard")]
    public async Task<IActionResult> GetTotdMonthLeaderboard()
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
                rank,
                result.PlayerName,
                result.PointsAwarded,
                result.FirstPlaceCount,
                result.SecondPlaceCount,
                result.ThirdPlaceCount,
                result.FourthPlaceCount,
                result.FifthPlaceCount));
            previousTieBreaker = tieBreaker;
        }

        _logger.LogInformation(
            "Returning {ResultCount} monthly TOTD leaderboard entries for {MonthStart}.",
            results.Count,
            monthStart);

        return Ok(new TotdMonthLeaderboardResponse(monthStart, results));
    }
    
    [HttpGet("zones")]
    public async Task<IActionResult> GetZones()
    {
        var result = await _core.GetZonesInfo();

        return Ok(result);
    }
    
    [HttpGet("totd")]
    public async Task<IActionResult> GetTotdLeaderboard()
    {
        var nowTimestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var map = await _db.TotdMaps
            .AsNoTracking()
            .Where(totdMap => totdMap.EndTimestamp <= nowTimestamp)
            .OrderByDescending(totdMap => totdMap.EndTimestamp)
            .FirstOrDefaultAsync();

        if (map is null || !await _db.TotdResults.AnyAsync(totdResult => totdResult.MapUid == map.MapUid))
        {
            _logger.LogInformation(
                "No stored results found for the most recently completed TOTD; fetching and syncing from Trackmania.");

            var syncResult = await _totdSyncService.SyncPreviousTotdAsync();
            if (syncResult is null)
            {
                _logger.LogWarning("Trackmania did not return a completed TOTD.");
                return NotFound("Previous TOTD not found.");
            }

            map = await _db.TotdMaps
                .AsNoTracking()
                .SingleAsync(totdMap => totdMap.MapUid == syncResult.MapUid);
        }
        else
        {
            _logger.LogInformation(
                "Using stored TOTD results for {TotdDate} (map {MapUid}); no Trackmania request is needed.",
                map.TotdDate,
                map.MapUid);
        }

        var result = await _db.TotdResults
            .AsNoTracking()
            .Where(totdResult => totdResult.MapUid == map.MapUid)
            .OrderBy(totdResult => totdResult.Rank)
            .Select(totdResult => new TotdLeaderboardResult(
                totdResult.Rank,
                totdResult.Player!.DisplayName,
                totdResult.Score,
                totdResult.PointsAwarded))
            .ToListAsync();

        _logger.LogInformation(
            "Returning {ResultCount} TOTD results for map {MapUid}.",
            result.Count,
            map.MapUid);

        return Ok(new TotdLeaderboardResponse(
            map.MapUid,
            map.MapName,
            map.TotdDate,
            result));
    }
    
    [HttpGet("accountName/{accountId}")]
    public async Task<IActionResult> GetAccountName(string accountId)
    {
        var result = await _oauth.GetAccountName(accountId);
        
        return Ok(result);
    }
}

public sealed record TotdLeaderboardResponse(
    string MapUid,
    string? MapName,
    DateOnly TotdDate,
    IReadOnlyList<TotdLeaderboardResult> Results);

public sealed record TotdLeaderboardResult(
    int Rank,
    string PlayerName,
    int Score,
    int PointsAwarded);

public sealed record TotdMonthLeaderboardResponse(
    DateOnly Month,
    IReadOnlyList<TotdMonthLeaderboardResult> Results);

public sealed record TotdMonthLeaderboardResult(
    int Rank,
    string PlayerName,
    int PointsAwarded,
    int FirstPlaceCount,
    int SecondPlaceCount,
    int ThirdPlaceCount,
    int FourthPlaceCount,
    int FifthPlaceCount);
