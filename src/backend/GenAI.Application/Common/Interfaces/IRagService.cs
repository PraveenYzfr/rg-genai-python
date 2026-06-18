using GenAI.Application.Common.Models.RAG;
using GenAI.Domain.Entities;

namespace GenAI.Application.Common.Interfaces;

public interface IRagService
{
    Task<RagSearchResult> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
    Task<Document> IngestDocumentAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> ListDocumentsAsync(CancellationToken cancellationToken = default);
}
