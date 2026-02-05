using Microsoft.AspNetCore.Mvc;

namespace PancakeBot.Api.Controller;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }
}