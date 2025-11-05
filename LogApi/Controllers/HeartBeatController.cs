namespace LogApi.Controllers;

using LogApi.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class HeartBeatController : ControllerBase
{
    private readonly ILogService _logService;

    public HeartBeatController(ILogService logService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
    }

    [HttpGet]
    [Route("api/heartbeat")]
    public IActionResult Get()
    {
        return Ok("Heartbeat is alive");
    }
}