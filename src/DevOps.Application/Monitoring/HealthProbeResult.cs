namespace DevOps.Application.Monitoring;

public sealed record HealthProbeResult(
    bool ReachedHost,
    bool IsSuccessStatusCode,
    int? HttpStatusCode,
    long ResponseTimeMs,
    string Message);
