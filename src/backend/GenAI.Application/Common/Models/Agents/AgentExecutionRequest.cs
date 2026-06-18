namespace GenAI.Application.Common.Models.Agents;

public sealed class AgentExecutionRequest
{
    public string AgentName { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
    public string? ModelKey { get; init; }
    public int MaxIterations { get; init; } = 10;
}
