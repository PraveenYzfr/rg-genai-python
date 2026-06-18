using GenAI.Application.Common.Models.Agents;

namespace GenAI.Application.Common.Interfaces;

public interface IAgentOrchestrator
{
    Task<AgentExecutionResult> ExecuteAsync(AgentExecutionRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<string> GetAvailableAgents();
}
