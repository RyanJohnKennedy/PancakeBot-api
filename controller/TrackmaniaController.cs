using Microsoft.AspNetCore.Mvc;
using PancakeBot.Api.Client;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Controller;

[ApiController]
[Route("api/trackmania")]
public class TrackmaniaController : ControllerBase
{
    private readonly ITrackmaniaLiveClient _live;
    private readonly ITrackmaniaCoreClient _core;
    private readonly ITrackmaniaOAuthClient _oauth;
    private readonly ITotdLeaderboardService _totdLeaderboardService;
    private readonly ITotdMonthLeaderboardService _totdMonthLeaderboardService;
    private readonly ILogger<TrackmaniaController> _logger;

    public TrackmaniaController(
        ITrackmaniaLiveClient live,
        ITrackmaniaCoreClient core,
        ITrackmaniaOAuthClient oauth,
        ITotdLeaderboardService totdLeaderboardService,
        ITotdMonthLeaderboardService totdMonthLeaderboardService,
        ILogger<TrackmaniaController> logger)
    {
        _live = live;
        _core = core;
        _oauth = oauth;
        _totdLeaderboardService = totdLeaderboardService;
        _totdMonthLeaderboardService = totdMonthLeaderboardService;
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
        var leaderboard = await _totdMonthLeaderboardService.GetCurrentMonthLeaderboardAsync();

        _logger.LogInformation(
            "Returning {ResultCount} monthly TOTD leaderboard entries for {MonthStart}.",
            leaderboard.Results.Count,
            leaderboard.Month);

        return Ok(leaderboard);
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
        var leaderboard = await _totdLeaderboardService.GetLatestCompletedLeaderboardAsync();
        return leaderboard is null ? NotFound("Previous TOTD not found.") : Ok(leaderboard);
    }
    
    [HttpGet("accountName/{accountId}")]
    public async Task<IActionResult> GetAccountName(string accountId)
    {
        var result = await _oauth.GetAccountName(accountId);
        
        return Ok(result);
    }
}
