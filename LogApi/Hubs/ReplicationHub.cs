using System.Text.Json;
using LogApi.Models;
using LogApi.Services;
using Microsoft.AspNetCore.SignalR;

namespace LogApi.Hubs;

public class ReplicationHub : Hub
{
    private readonly ILogService _logService;
    private readonly ILogger<ReplicationHub> _logger;

    public ReplicationHub(ILogService logService, ILogger<ReplicationHub> logger)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ReplicateData(string key, LogDataModel value)
    {
        Console.WriteLine($"ReplicateData called - Key: {key}, Value: {JsonSerializer.Serialize(value)}, ConnectionId: {Context.ConnectionId}");
        Console.WriteLine(value);
        _logger.LogDebug("ReplicateData called - Key: {Key}, Value: {Value}, ConnectionId: {ConnectionId}",
            key, JsonSerializer.Serialize(value), Context.ConnectionId);

        if (value == null)
        {
            _logger.LogWarning("ReplicateData received null value for key: {Key} from ConnectionId: {ConnectionId}",
                key, Context.ConnectionId);
            throw new ArgumentNullException(nameof(value));
        }

        try
        {
            // Add log to the service (which handles KeyValueStore internally)
            _logger.LogDebug("Adding log to service for key: {Key}", key);
            _logService.AddLogWithoutReplication(value); // Don't broadcast again
            _logger.LogDebug("Successfully added log to service for key: {Key}", key);

            // Broadcast to all other clients (except sender)
            _logger.LogDebug("Broadcasting to other clients - Key: {Key}", key);
            await Clients.Others.SendAsync("ReceiveReplication", key, value);
            _logger.LogDebug("Successfully broadcasted to other clients for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReplicateData for key: {Key}, ConnectionId: {ConnectionId}",
                key, Context.ConnectionId);
            throw;
        }
    }

    public async Task ReplicateLog(LogDataModel logData)
    {
        _logger.LogDebug("ReplicateLog called - LogData: {LogData}, ConnectionId: {ConnectionId}",
            JsonSerializer.Serialize(logData), Context.ConnectionId);

        if (logData == null)
        {
            _logger.LogWarning("ReplicateLog received null logData from ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            throw new ArgumentNullException(nameof(logData));
        }

        try
        {
            // Add log to the service (thread-safe operation)
            _logger.LogDebug("Adding log to service - Name: {LogName}, Timestamp: {Timestamp}",
                logData.Name, logData.Timestamp);
            _logService.AddLogWithoutReplication(logData); // Don't broadcast again
            _logger.LogDebug("Successfully added log to service - Name: {LogName}", logData.Name);

            // Broadcast to all other clients (except sender)
            _logger.LogDebug("Broadcasting log replication to other clients - Name: {LogName}", logData.Name);
            await Clients.Others.SendAsync("ReceiveLogReplication", logData);
            _logger.LogDebug("Successfully broadcasted log replication - Name: {LogName}", logData.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReplicateLog for log: {LogName}, ConnectionId: {ConnectionId}",
                logData?.Name, Context.ConnectionId);
            throw;
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to ReplicationHub - ConnectionId: {ConnectionId}",
            Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from ReplicationHub - ConnectionId: {ConnectionId}, Exception: {Exception}",
            Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    // This method is called when a log is received from another instance via Redis backplane
    public Task ProcessIncomingLog(LogDataModel logData)
    {
        _logger.LogDebug("ProcessIncomingLog called - LogData: {LogData}, ConnectionId: {ConnectionId}",
            JsonSerializer.Serialize(logData), Context.ConnectionId);

        if (logData == null)
        {
            _logger.LogWarning("ProcessIncomingLog received null logData from ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return Task.CompletedTask;
        }

        try
        {
            // Store the incoming log without broadcasting (to prevent loops)
            _logger.LogDebug("Storing incoming log - Name: {LogName}, Timestamp: {Timestamp}",
                logData.Name, logData.Timestamp);
            _logService.AddLogWithoutReplication(logData);
            _logger.LogDebug("Successfully stored incoming log - Name: {LogName}", logData.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing incoming log: {LogName}, ConnectionId: {ConnectionId}",
                logData?.Name, Context.ConnectionId);
        }

        return Task.CompletedTask;
    }
}
