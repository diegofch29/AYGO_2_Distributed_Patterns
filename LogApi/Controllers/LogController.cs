using Microsoft.AspNetCore.Mvc;
using LogApi.Models;
using LogApi.Services;

namespace LogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogController : ControllerBase
{
    private readonly ILogService _logService;

    public LogController(ILogService logService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
    }

    [HttpGet]
    public IEnumerable<List<LogDataModel>> Get()
    {
        return _logService.GetLogs();
    }
}
