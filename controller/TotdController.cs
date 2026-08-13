using Microsoft.AspNetCore.Mvc;
using PancakeBot.Api.Service;

namespace PancakeBot.Api.Controller;

[ApiController]
[Route("api/totd")]
public class TotdController : ControllerBase
{
    private readonly ITotdSyncService _totdSyncService;

    public TotdController(ITotdSyncService totdSyncService)
    {
        _totdSyncService = totdSyncService;
    }

    [HttpPost("sync-current-month")]
    public async Task<ActionResult<TotdMonthSyncResult>> SyncCurrentMonth()
    {
        var result = await _totdSyncService.SyncCurrentMonthCompletedTotdsAsync();
        return Ok(result);
    }
}
