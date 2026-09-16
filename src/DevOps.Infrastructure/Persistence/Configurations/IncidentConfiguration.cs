using DevOps.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevOps.Infrastructure.Persistence.Configurations;

public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");
        builder.HasKey(incident => incident.Id);

        builder.Property(incident => incident.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(incident => incident.Description)
            .HasMaxLength(4000);

        builder.Property(incident => incident.Resolution)
            .HasMaxLength(4000);

        builder.Property(incident => incident.RootCause)
            .HasMaxLength(4000);

        builder.Property(incident => incident.ActionsTaken)
            .HasMaxLength(4000);

        builder.Property(incident => incident.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(incident => incident.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(incident => incident.ServiceId);
        builder.HasIndex(incident => incident.Status);
        builder.HasIndex(incident => incident.DetectedAt);
    }
}
