using LogApi.Services;

namespace LogApi.Services;

public class ServiceRegistrationHostedService : IHostedService
{
    private readonly IServiceRegistrationService _registrationService;
    private readonly ILogger<ServiceRegistrationHostedService> _logger;
    private readonly IConfiguration _configuration;

    public ServiceRegistrationHostedService(
        IServiceRegistrationService registrationService,
        ILogger<ServiceRegistrationHostedService> logger,
        IConfiguration configuration)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Check if auto registration is enabled (default: true)
        var autoRegister = _configuration.GetValue("ServiceRegistration:AutoRegister", true);

        if (!autoRegister)
        {
            _logger.LogInformation("Auto registration is disabled");
            return;
        }

        _logger.LogInformation("Starting service registration...");

        try
        {
            // Small delay to ensure the web server is ready
            await Task.Delay(2000, cancellationToken);

            var success = await _registrationService.RegisterServiceAsync();
            if (success)
            {
                _logger.LogInformation("Service registered successfully on startup");
            }
            else
            {
                _logger.LogWarning("Failed to register service on startup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service registration on startup");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping service registration...");

        try
        {
            var success = await _registrationService.UnregisterServiceAsync();
            if (success)
            {
                _logger.LogInformation("Service unregistered successfully on shutdown");
            }
            else
            {
                _logger.LogWarning("Failed to unregister service on shutdown");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service unregistration on shutdown");
        }
    }
}
