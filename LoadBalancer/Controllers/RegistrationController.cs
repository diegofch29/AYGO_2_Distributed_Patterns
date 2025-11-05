using Microsoft.AspNetCore.Mvc;
using LoadBalancer.Helper;
using LoadBalancer.Models;
using LoadBalancer.Services;

namespace LoadBalancer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly ILogger<RegistrationController> _logger;
    private readonly IRegistrationService _registrationService;

    public RegistrationController(ILogger<RegistrationController> logger, IRegistrationService registrationService)
    {
        _logger = logger;
        _registrationService = registrationService;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterService([FromBody] RegistrationModel service)
    {
        _logger.LogInformation("Registering service: {Service}", service.ServiceName);
        await _registrationService.RegisterServiceAsync(service);

        return Ok(new { Status = "Registered" });
    }
}