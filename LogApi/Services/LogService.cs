using LogApi.Models;
using LogApi.Stores;
using LogApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LogApi.Services;

public class LogService : ILogService
{
    private readonly KeyValueStore _keyValueStore;
    private readonly IHubContext<ReplicationHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly object _readLock = new();
    private const string LOG_PREFIX = "log_";

    public LogService(KeyValueStore keyValueStore, IHubContext<ReplicationHub> hubContext, IConfiguration configuration)
    {
        _keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IEnumerable<List<LogDataModel>> GetLogs()
    {
        // Create a snapshot of logs for thread-safe enumeration
        LogDataModel[] logsSnapshot;

        lock (_readLock)
        {
            // Get all log entries from KeyValueStore
            var allData = _keyValueStore.GetAll();
            var logEntries = allData
                .Where(kvp => kvp.Key.StartsWith(LOG_PREFIX))
                .Select(kvp =>
                {
                    try
                    {
                        return JsonSerializer.Deserialize<LogDataModel>(kvp.Value);
                    }
                    catch (JsonException)
                    {
                        // Skip invalid entries
                        return null;
                    }
                })
                .Where(log => log != null)
                .ToArray();

            logsSnapshot = logEntries!;
        }

        // For demonstration, returning logs in batches of 10
        // Sort by timestamp to maintain consistent ordering
        return logsSnapshot
            .OrderBy(log => log.Timestamp)
            .Select((log, index) => new { log, index })
            .GroupBy(x => x.index / 10)
            .Select(g => g.Select(x => x.log).ToList());
    }

    public void AddLog(LogDataModel log)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        // Generate a unique key for the log entry
        var key = $"{LOG_PREFIX}{Guid.NewGuid()}_{log.Timestamp:yyyyMMddHHmmssfff}";
        var serializedLog = JsonSerializer.Serialize(log);

        // Store locally
        _keyValueStore.AddOrUpdate(key, serializedLog);
    }

    public void AddLogWithoutReplication(LogDataModel log)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        // Generate a unique key for the log entry
        var key = $"{LOG_PREFIX}{Guid.NewGuid()}_{log.Timestamp:yyyyMMddHHmmssfff}";
        var serializedLog = JsonSerializer.Serialize(log);

        // Store locally without broadcasting (used for receiving replicated logs)
        _keyValueStore.AddOrUpdate(key, serializedLog);
        Console.WriteLine($"Added replicated log without broadcasting: {log.Name}");
    }
}