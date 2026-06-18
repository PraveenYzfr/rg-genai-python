namespace GenAI.Application.Common.Models.Agents;

public sealed class AgentStepResult
{
    public int StepIndex { get; init; }
    public string StepType { get; init; } = string.Empty;
    public string? Input { get; init; }
    public string? Output { get; init; }
    public int DurationMs { get; init; }
}
