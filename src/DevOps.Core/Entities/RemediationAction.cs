using DevOps.Core.Enums;

namespace DevOps.Core.Entities;

public class RemediationAction
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public Guid ServiceId { get; set; }
    public RemediationActionType ActionType { get; set; }
    public required string Description { get; set; }
    public RemediationStatus Status { get; set; } = RemediationStatus.Recommended;
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public string? Result { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
