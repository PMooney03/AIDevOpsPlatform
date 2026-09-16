using DevOps.Application.Observability;
using OpenTelemetry.Metrics;

namespace DevOps.Api.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddPlatformTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(PlatformMetrics.MeterName);
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
                metrics.AddPrometheusExporter();
            });

        return services;
    }
}
