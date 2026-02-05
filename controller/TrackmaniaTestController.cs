using Microsoft.AspNetCore.Mvc;
using PancakeBot.Api.Client;

namespace PancakeBot.Api.Controller;

[ApiController]
[Route("test/tm")]
public class TrackmaniaTestController : ControllerBase
{
    private readonly ITrackmaniaCoreClient _core;
    private readonly ITrackmaniaLiveClient _live;

    public TrackmaniaTestController(
        ITrackmaniaCoreClient core,
        ITrackmaniaLiveClient live)
    {
        _core = core;
        _live = live;
    }

    [HttpGet("core-ping")]
    public async Task<IActionResult> CorePing()
    {
        var result = await _core.GetMapInfo(
            "d3507069-8751-441e-bffc-ef128d56d596");

        return Ok(result);
    }

    [HttpGet("live-ping")]
    public async Task<IActionResult> LivePing()
    {
        var result = await _live.GetLeaderboard(
            "d3507069-8751-441e-bffc-ef128d56d596");

        return Ok(result);
    }
}
