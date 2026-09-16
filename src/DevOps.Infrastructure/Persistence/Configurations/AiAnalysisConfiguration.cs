using DevOps.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevOps.Infrastructure.Persistence.Configurations;

public sealed class AiAnalysisConfiguration : IEntityTypeConfiguration<AiAnalysis>
{
    public void Configure(EntityTypeBuilder<AiAnalysis> builder)
    {
        builder.ToTable("ai_analyses");
        builder.HasKey(analysis => analysis.Id);
        builder.Property(analysis => analysis.Summary).HasMaxLength(4000).IsRequired();
        builder.Property(analysis => analysis.ProbableCause).HasMaxLength(4000).IsRequired();
        builder.Property(analysis => analysis.SeverityAssessment).HasMaxLength(200).IsRequired();
        builder.Property(analysis => analysis.EvidenceJson).IsRequired();
        builder.Property(analysis => analysis.RecommendedChecksJson).IsRequired();
        builder.Property(analysis => analysis.SuggestedRemediationJson).IsRequired();
        builder.Property(analysis => analysis.LimitationsJson).IsRequired();
        builder.Property(analysis => analysis.ModelName).HasMaxLength(200);
        builder.Property(analysis => analysis.FailureReason).HasMaxLength(2000);
        builder.HasIndex(analysis => new { analysis.IncidentId, analysis.CreatedAt });
        builder.HasOne(analysis => analysis.Incident)
            .WithMany()
            .HasForeignKey(analysis => analysis.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DeploymentConfiguration : IEntityTypeConfiguration<Deployment>
{
    public void Configure(EntityTypeBuilder<Deployment> builder)
    {
        builder.ToTable("deployments");
        builder.HasKey(deployment => deployment.Id);
        builder.Property(deployment => deployment.CommitSha).HasMaxLength(64).IsRequired();
        builder.Property(deployment => deployment.Branch).HasMaxLength(200).IsRequired();
        builder.Property(deployment => deployment.BuildStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(deployment => deployment.TestStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(deployment => deployment.DeploymentStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(deployment => new { deployment.ServiceId, deployment.StartedAt });
        builder.HasOne(deployment => deployment.Service)
            .WithMany()
            .HasForeignKey(deployment => deployment.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RemediationConfiguration : IEntityTypeConfiguration<RemediationAction>
{
    public void Configure(EntityTypeBuilder<RemediationAction> builder)
    {
        builder.ToTable("remediation_actions");
        builder.HasKey(action => action.Id);
        builder.Property(action => action.Description).HasMaxLength(2000).IsRequired();
        builder.Property(action => action.ActionType).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(action => action.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(action => action.ApprovedBy).HasMaxLength(200);
        builder.Property(action => action.RejectedBy).HasMaxLength(200);
        builder.Property(action => action.RejectionReason).HasMaxLength(2000);
        builder.Property(action => action.Result).HasMaxLength(8000);
        builder.Property(action => action.FailureReason).HasMaxLength(2000);
        builder.HasIndex(action => action.IncidentId);
        builder.HasIndex(action => action.Status);
        builder.HasOne(action => action.Incident)
            .WithMany()
            .HasForeignKey(action => action.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
