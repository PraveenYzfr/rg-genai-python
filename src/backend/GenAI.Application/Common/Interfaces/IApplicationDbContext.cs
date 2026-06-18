using GenAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenAI.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Document> Documents { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<AgentRun> AgentRuns { get; }
    DbSet<AgentStep> AgentSteps { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
