namespace GenAI.Application.Common.Interfaces;

public interface IAgentRunRepository
{
    Task<Domain.Entities.AgentRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Entities.AgentRun run, CancellationToken cancellationToken = default);
    Task UpdateAsync(Domain.Entities.AgentRun run, CancellationToken cancellationToken = default);
}
