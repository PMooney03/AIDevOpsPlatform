using DevOps.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevOps.Infrastructure.Persistence.Configurations;

public sealed class MonitoredServiceConfiguration : IEntityTypeConfiguration<MonitoredService>
{
    public void Configure(EntityTypeBuilder<MonitoredService> builder)
    {
        builder.ToTable("monitored_services");
        builder.HasKey(service => service.Id);

        builder.Property(service => service.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(service => service.Name)
            .IsUnique();

        builder.Property(service => service.Description)
            .HasMaxLength(2000);

        builder.Property(service => service.BaseUrl)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(service => service.HealthEndpoint)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(service => service.ContainerName)
            .HasMaxLength(128);

        builder.Property(service => service.ContainerId)
            .HasMaxLength(128);

        builder.Property(service => service.ContainerImage)
            .HasMaxLength(500);

        builder.Property(service => service.ContainerHealth)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(service => service.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasMany(service => service.Incidents)
            .WithOne(incident => incident.Service)
            .HasForeignKey(incident => incident.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
