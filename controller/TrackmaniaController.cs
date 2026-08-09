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
    private readonly TrackmaniaLiveService _liveService;
    private readonly IPreviousTotdService _previousTotdService;

    public TrackmaniaController(
        ITrackmaniaLiveClient live,
        ITrackmaniaCoreClient core,
        ITrackmaniaOAuthClient oauth,
        TrackmaniaLiveService liveService,
        IPreviousTotdService previousTotdService)
    {
        _live = live;
        _core = core;
        _oauth = oauth;
        _liveService = liveService;
        _previousTotdService = previousTotdService;
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
        var totd = await _previousTotdService.GetPreviousTotd();
        if (totd is null)
        {
            return NotFound("Previous TOTD not found.");
        }

        var result =
            await _liveService.GetLeaderboard(totd.MapUid, onlyWorld: false);

        return Ok(result);
    }
    
    [HttpGet("accountName/{accountId}")]
    public async Task<IActionResult> GetAccountName(string accountId)
    {
        var result = await _oauth.GetAccountName(accountId);
        
        return Ok(result);
    }
}
