using DevOps.Core.Entities;
using DevOps.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevOps.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public static async Task EnsureDemoServicesAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var payments = await db.MonitoredServices.FirstOrDefaultAsync(
            service => service.Name == "payments-api",
            cancellationToken);

        if (payments is null)
        {
            payments = new MonitoredService
            {
                Id = Guid.CreateVersion7(),
                Name = "payments-api",
                Description = "Intentionally failing demo service",
                BaseUrl = "http://demo-unhealthy",
                HealthEndpoint = "/health",
                Status = ServiceStatus.Unknown,
                MonitoringEnabled = true,
                ContainerName = "demo-unhealthy",
                CreatedAt = now,
                UpdatedAt = now
            };
            db.MonitoredServices.Add(payments);
        }
        else if (string.IsNullOrWhiteSpace(payments.ContainerName))
        {
            payments.ContainerName = "demo-unhealthy";
            payments.UpdatedAt = now;
        }

        if (!await db.MonitoredServices.AnyAsync(service => service.Name == "crash-loop", cancellationToken))
        {
            db.MonitoredServices.Add(new MonitoredService
            {
                Id = Guid.CreateVersion7(),
                Name = "crash-loop",
                Description = "Demo container that exits and restarts",
                BaseUrl = "http://demo-restarting",
                HealthEndpoint = "/health",
                Status = ServiceStatus.Unknown,
                MonitoringEnabled = true,
                ContainerName = "demo-restarting",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (!await db.MonitoredServices.AnyAsync(service => service.Name == "demo-chaos", cancellationToken))
        {
            db.MonitoredServices.Add(new MonitoredService
            {
                Id = Guid.CreateVersion7(),
                Name = "demo-chaos",
                Description = "DEVELOPMENT / DEMO ONLY controllable fault service",
                BaseUrl = "http://demo-chaos:8080",
                HealthEndpoint = "/health",
                Status = ServiceStatus.Unknown,
                MonitoringEnabled = true,
                ContainerName = "demo-chaos",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        const string historicalTitle = "Database connection timeout";
        if (!await db.Incidents.AnyAsync(
                incident => incident.ServiceId == payments.Id && incident.Title == historicalTitle,
                cancellationToken))
        {
            var detected = now.AddDays(-8);
            var resolvedAt = detected.AddHours(2);
            db.Incidents.Add(new Incident
            {
                Id = Guid.CreateVersion7(),
                ServiceId = payments.Id,
                Title = historicalTitle,
                Description = "PostgreSQL connectivity failure after a configuration change.",
                Severity = IncidentSeverity.High,
                Status = IncidentStatus.Resolved,
                DetectedAt = detected,
                ResolvedAt = resolvedAt,
                Resolution = "DB_HOST configuration corrected.",
                RootCause = "Incorrect database hostname following deployment.",
                ActionsTaken = "Compared environment variables with the previous release and restored DB_HOST.",
                CreatedAt = detected,
                UpdatedAt = resolvedAt
            });
        }

        if (!await db.Deployments.AnyAsync(deployment => deployment.ServiceId == payments.Id, cancellationToken))
        {
            var started = now.AddMinutes(-12);
            db.Deployments.Add(new Deployment
            {
                Id = Guid.CreateVersion7(),
                ServiceId = payments.Id,
                CommitSha = "a1b2c3d4e5f6789012345678901234567890abcd",
                Branch = "main",
                BuildStatus = DeploymentStageStatus.Succeeded,
                TestStatus = DeploymentStageStatus.Succeeded,
                DeploymentStatus = DeploymentStageStatus.Succeeded,
                StartedAt = started,
                CompletedAt = started.AddMinutes(6),
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
