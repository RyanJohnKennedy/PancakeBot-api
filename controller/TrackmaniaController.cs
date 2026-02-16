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
    private readonly TrackmaniaLiveService _liveService;

    public TrackmaniaController(
        ITrackmaniaLiveClient live, 
        ITrackmaniaCoreClient core, 
        TrackmaniaLiveService liveService)
    {
        _live = live;
        _core = core;
        _liveService = liveService;
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
        var totd = await _liveService.GetPreviousTotd();

        var result =
            await _liveService.GetLeaderboard(totd.MapUid, "3022580b-7e13-11e8-8060-e284abfd2bc4", onlyWorld: false);

        return Ok(result);
    }
}