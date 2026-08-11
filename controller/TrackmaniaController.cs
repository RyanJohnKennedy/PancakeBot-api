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
    
    [HttpGet("zones")]
    public async Task<IActionResult> GetZones()
    {
        var result = await _core.GetZonesInfo();

        return Ok(result);
    }
    
    [HttpGet("totd")]
    public async Task<IActionResult> GetTotdLeaderboard()
    {
        var yesterday = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(-1));
        var map = await _db.TotdMaps
            .AsNoTracking()
            .SingleOrDefaultAsync(totdMap => totdMap.TotdDate == yesterday);

        if (map is null || !await _db.TotdResults.AnyAsync(totdResult => totdResult.MapUid == map.MapUid))
        {
            _logger.LogInformation(
                "No stored TOTD results found for {TotdDate}; fetching and syncing from Trackmania.",
                yesterday);

            var syncResult = await _totdSyncService.SyncPreviousTotdAsync();
            if (syncResult is null)
            {
                _logger.LogWarning("Trackmania did not return a previous TOTD for {TotdDate}.", yesterday);
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
                yesterday,
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
