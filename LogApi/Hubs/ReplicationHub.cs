using System.Text.Json;
using LogApi.Models;
using LogApi.Services;
using Microsoft.AspNetCore.SignalR;

namespace LogApi.Hubs;

public class ReplicationHub : Hub
{
    private readonly ILogService _logService;

    public ReplicationHub(ILogService logService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
    }

    public async Task ReplicateData(string key, LogDataModel value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        // Add log to the service (which handles KeyValueStore internally)
        _logService.AddLog(value);

        // Broadcast to all other clients (except sender)
        await Clients.Others.SendAsync("ReceiveReplication", key, value);
    }

    public async Task ReplicateLog(LogDataModel logData)
    {
        if (logData == null)
            throw new ArgumentNullException(nameof(logData));

        // Add log to the service (thread-safe operation)
        _logService.AddLog(logData);

        // Broadcast to all other clients (except sender)
        await Clients.Others.SendAsync("ReceiveLogReplication", logData);
    }
}
