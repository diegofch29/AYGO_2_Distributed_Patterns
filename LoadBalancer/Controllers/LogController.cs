using Microsoft.AspNetCore.Mvc;
using LoadBalancer.Helper;
using LoadBalancer.Models;
using LoadBalancer.Services;

namespace LoadBalancer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogController : ControllerBase
{
    private readonly ILogger<LogController> _logger;
    private readonly IHttpClientHelper _httpClientHelper;
    private List<string> ipAddresses = new List<string>();
    private readonly IRegistrationService _registrationService;
    private readonly ILogNotificationService _logNotificationService;

    public LogController(ILogger<LogController> logger, IHttpClientHelper httpClientHelper, IRegistrationService registrationService, ILogNotificationService logNotificationService)
    {
        _logger = logger;
        _httpClientHelper = httpClientHelper;
        _registrationService = registrationService;
        _logNotificationService = logNotificationService;
    }

    [HttpPost]
    public async Task<IActionResult> LogData([FromBody] LogDataModel logData)
    {
        _logger.LogInformation($"Received log data: {logData.Name}");
        Console.WriteLine($"Received log data: {logData.Name}");
        // Send SignalR notification to all connected clients
        await _logNotificationService.NotifyLogAdded(logData);
        return Ok(new { Status = "Logged" });
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs()
    {
        try
        {
            var currentServiceUrl = _registrationService.GetNextServiceRoundRobin();
            _logger.LogInformation("Retrieving logs from service: {Service}", currentServiceUrl);

            // First, try the robust method that handles nested arrays automatically
            var response = await _httpClientHelper.GetAsyncWithFallback<List<LogDataModel>>($"{currentServiceUrl}/api/log");

            return Ok(response ?? new List<LogDataModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve logs from backend service");
            return StatusCode(500, new { Error = "Failed to retrieve logs", Details = ex.Message });
        }
    }
}
