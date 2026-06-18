namespace GenAI.Application.Common.Models.Agents;

public sealed class AgentExecutionResult
{
    public Guid RunId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<AgentStepResult> Steps { get; init; } = Array.Empty<AgentStepResult>();
}
