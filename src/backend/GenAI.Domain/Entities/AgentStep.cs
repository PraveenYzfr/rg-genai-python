using GenAI.Domain.Common;

namespace GenAI.Domain.Entities;

public class AgentStep : BaseEntity
{
    public Guid AgentRunId { get; set; }
    public AgentRun AgentRun { get; set; } = null!;
    public int StepIndex { get; set; }
    public string StepType { get; set; } = string.Empty;
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
