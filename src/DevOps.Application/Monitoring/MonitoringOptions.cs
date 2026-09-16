namespace DevOps.Application.Monitoring;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public int DegradedResponseTimeMs { get; set; } = 1000;
    public int UnhealthyFailureThreshold { get; set; } = 3;
    public TimeSpan OfflineUnreachablePeriod { get; set; } = TimeSpan.FromMinutes(2);
    public int HistoryDefaultLimit { get; set; } = 50;
    public int HistoryMaxLimit { get; set; } = 500;
    public int ContainerRestartIncidentThreshold { get; set; } = 3;
    public int MaxConcurrentHealthChecks { get; set; } = 8;
    public int HealthCheckRetentionDays { get; set; } = 14;
}
