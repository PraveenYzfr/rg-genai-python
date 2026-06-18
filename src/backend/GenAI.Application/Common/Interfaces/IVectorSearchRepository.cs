using GenAI.Application.Common.Models.RAG;
using GenAI.Domain.Entities;

namespace GenAI.Application.Common.Interfaces;

public interface IVectorSearchRepository
{
    Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RagCitation>> SearchSimilarAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);
    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
}
