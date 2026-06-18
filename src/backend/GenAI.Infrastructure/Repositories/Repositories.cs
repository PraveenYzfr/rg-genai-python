using GenAI.Application.Common.Interfaces;
using GenAI.Domain.Entities;
using GenAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GenAI.Infrastructure.Repositories;

public sealed class DocumentRepository(ApplicationDbContext context) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Document>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Documents
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        context.Documents.Update(document);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AgentRunRepository(ApplicationDbContext context) : IAgentRunRepository
{
    public async Task<AgentRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.AgentRuns
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task AddAsync(AgentRun run, CancellationToken cancellationToken = default)
    {
        context.AgentRuns.Add(run);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AgentRun run, CancellationToken cancellationToken = default)
    {
        context.AgentRuns.Update(run);
        await context.SaveChangesAsync(cancellationToken);
    }
}
