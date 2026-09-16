using DevOps.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevOps.Infrastructure.Persistence.Configurations;

public sealed class HealthCheckResultConfiguration : IEntityTypeConfiguration<HealthCheckResult>
{
    public void Configure(EntityTypeBuilder<HealthCheckResult> builder)
    {
        builder.ToTable("health_check_results");
        builder.HasKey(result => result.Id);

        builder.Property(result => result.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(result => result.Message)
            .HasMaxLength(2000);

        builder.HasIndex(result => new { result.ServiceId, result.CheckedAt });

        builder.HasOne(result => result.Service)
            .WithMany(service => service.HealthChecks)
            .HasForeignKey(result => result.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
