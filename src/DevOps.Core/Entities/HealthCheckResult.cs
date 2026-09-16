using DevOps.Core.Enums;

namespace DevOps.Core.Entities;

public class HealthCheckResult
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public MonitoredService? Service { get; set; }
    public ServiceStatus Status { get; set; }
    public long ResponseTimeMs { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CheckedAt { get; set; }
}
