using System.Text.Json;

namespace LoadBalancer.Models;

public class LogDataModel
{
    public string SourceInstance { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }
}