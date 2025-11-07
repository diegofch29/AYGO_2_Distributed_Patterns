using Microsoft.AspNetCore.SignalR.Client;
using LogApi.Models;
using LogApi.Services;
using System.Text.Json;

namespace LogApi.Services;

public class SignalRReplicationService : BackgroundService
{
    private readonly ILogger<SignalRReplicationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private HubConnection? _connection;

    public SignalRReplicationService(
        ILogger<SignalRReplicationService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Get current instance info
        var currentServiceName = _configuration["ServiceRegistration:ServiceName"];
        var loadBalancerUrl = _configuration["ServiceRegistration:LoadBalancerUrl"];

        _logger.LogInformation("Starting SignalR replication service for {ServiceName}", currentServiceName);

        // Build connection to the LoadBalancer SignalR hub (this enables Redis backplane sync)
        string hubUrl;

        if (!string.IsNullOrEmpty(loadBalancerUrl))
        {
            // Ensure the LoadBalancerUrl has a proper scheme
            var baseUrl = loadBalancerUrl.TrimEnd('/');
            if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
            {
                baseUrl = $"http://{baseUrl}";
            }
            hubUrl = $"{baseUrl}/replicate";
        }
        else
        {
            // Fallback to localhost with default port
            hubUrl = "http://localhost:5000/replicate";
        }

        _logger.LogInformation("Connecting to SignalR hub at: {HubUrl}", hubUrl);

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Listen for log events from other instances (via Redis backplane)
        _connection.On<LogDataModel>("LogAdded", (replicationData) =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var logService = scope.ServiceProvider.GetRequiredService<ILogService>();

                logService.AddLogWithoutReplication(replicationData!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing replicated log: {ex.Message}");
                _logger.LogError(ex, "Error processing replicated log");
            }
        });

        // Start connection with retry logic
        await StartConnectionWithRetry(stoppingToken);

        // Keep running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SignalR replication service is stopping...");
        }
    }

    private async Task StartConnectionWithRetry(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _connection!.StartAsync(stoppingToken);
                _logger.LogInformation("Successfully connected to SignalR hub for replication");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to SignalR hub, retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _logger.LogInformation("SignalR replication connection disposed");
        }

        await base.StopAsync(cancellationToken);
    }
}