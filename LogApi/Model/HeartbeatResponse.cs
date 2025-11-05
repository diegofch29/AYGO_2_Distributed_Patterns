namespace LogApi.Models;

public class HeartbeatResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}