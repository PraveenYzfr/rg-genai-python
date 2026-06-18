using GenAI.Domain.Common;
using GenAI.Domain.Enums;

namespace GenAI.Domain.Entities;

public class AgentRun : BaseEntity, IAuditableEntity
{
    public string AgentName { get; set; } = string.Empty;
    public string InputGoal { get; set; } = string.Empty;
    public AgentRunStatus Status { get; set; } = AgentRunStatus.Running;
    public string? OutputSummary { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ICollection<AgentStep> Steps { get; set; } = new List<AgentStep>();
}
