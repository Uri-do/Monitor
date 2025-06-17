using Microsoft.AspNetCore.Mvc;

namespace MonitoringGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong", timestamp = DateTime.UtcNow });
    }

    [HttpPost("echo")]
    public IActionResult Echo([FromBody] object data)
    {
        return Ok(new { echo = data, timestamp = DateTime.UtcNow });
    }
}
