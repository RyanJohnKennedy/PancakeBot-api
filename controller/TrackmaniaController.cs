using Microsoft.AspNetCore.Mvc;
using PancakeBot.Api.Client;

namespace PancakeBot.Api.Controller;

[ApiController]
[Route("api/trackmania")]
public class TrackmaniaController : ControllerBase
{
    private readonly ITrackmaniaLiveClient _live;

    public TrackmaniaController(ITrackmaniaLiveClient live)
    {
        _live = live;
    }
    
    [HttpGet("totd-month")]
    public async Task<IActionResult> GetMonth()
    {
        var result = await _live.GetTotdMonth();

        return Ok(result);
    }
}