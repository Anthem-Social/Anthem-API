using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("health")]
public class HealthController: ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var timestamp = DateTime.Now;
        string version = Environment.GetEnvironmentVariable("VERSION") ?? "none";
        return Ok(new { timestamp, version });
    }
}
