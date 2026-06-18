using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Models.RAG;
using GenAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace GenAI.Infrastructure.Repositories;

public sealed class VectorSearchRepository(ApplicationDbContext context) : IVectorSearchRepository
{
    public async Task AddChunksAsync(IEnumerable<Domain.Entities.DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        context.DocumentChunks.AddRange(chunks);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RagCitation>> SearchSimilarAsync(
        float[] queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var queryVector = new Vector(queryEmbedding);

        var chunks = await context.DocumentChunks
            .AsNoTracking()
            .Include(c => c.Document)
            .Where(c => c.Embedding != null)
            .OrderBy(c => c.Embedding!.CosineDistance(queryVector))
            .Take(topK)
            .ToListAsync(cancellationToken);

        return chunks.Select(c =>
        {
            var distance = new Vector(c.Embedding!).CosineDistance(queryVector);
            return new RagCitation
            {
                ChunkId = c.Id,
                DocumentId = c.DocumentId,
                FileName = c.Document.FileName,
                PageNumber = c.PageNumber,
                ChunkIndex = c.ChunkIndex,
                Excerpt = c.Content.Length > 500 ? c.Content[..500] + "..." : c.Content,
                Score = 1 - distance
            };
        }).ToList();
    }
}
