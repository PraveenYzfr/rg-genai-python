using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Models.RAG;
using GenAI.Application.Common.Options;
using GenAI.Domain.Entities;
using GenAI.Domain.Enums;
using GenAI.RAG.Embeddings;
using GenAI.RAG.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenAI.RAG.Retrieval;

public sealed class RagService : IRagService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IVectorSearchRepository _vectorSearchRepository;
    private readonly IFileStorage _fileStorage;
    private readonly CompositeDocumentParser _documentParser;
    private readonly TextChunker _chunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly RagOptions _options;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IDocumentRepository documentRepository,
        IVectorSearchRepository vectorSearchRepository,
        IFileStorage fileStorage,
        IEnumerable<IDocumentParser> parsers,
        TextChunker chunker,
        IEmbeddingService embeddingService,
        IOptions<RagOptions> options,
        ILogger<RagService> logger)
    {
        _documentRepository = documentRepository;
        _vectorSearchRepository = vectorSearchRepository;
        _fileStorage = fileStorage;
        _documentParser = new CompositeDocumentParser(parsers);
        _chunker = chunker;
        _embeddingService = embeddingService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Document> IngestDocumentAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileStream.CanSeek ? fileStream.Length : 0,
            Status = DocumentStatus.Processing,
            CreatedAt = DateTimeOffset.UtcNow
        };

        document.StoragePath = await _fileStorage.SaveAsync(fileStream, fileName, cancellationToken);
        await _documentRepository.AddAsync(document, cancellationToken);

        try
        {
            await using var readStream = await _fileStorage.OpenReadAsync(document.StoragePath, cancellationToken);
            var text = await _documentParser.ParseAsync(readStream, fileName, cancellationToken);
            var textChunks = _chunker.Chunk(text);

            if (textChunks.Count == 0)
            {
                throw new InvalidOperationException("No text content could be extracted from the document.");
            }

            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(
                textChunks.Select(c => c.Content),
                cancellationToken);

            var chunks = textChunks.Zip(embeddings, (chunk, embedding) => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ChunkIndex = chunk.Index,
                Content = chunk.Content,
                PageNumber = chunk.PageNumber,
                Embedding = embedding
            }).ToList();

            await _vectorSearchRepository.AddChunksAsync(chunks, cancellationToken);

            document.Status = DocumentStatus.Ready;
            document.UpdatedAt = DateTimeOffset.UtcNow;
            await _documentRepository.UpdateAsync(document, cancellationToken);

            _logger.LogInformation(
                "Ingested document {DocumentId} with {ChunkCount} chunks.",
                document.Id,
                chunks.Count);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest document {DocumentId}.", document.Id);
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            document.UpdatedAt = DateTimeOffset.UtcNow;
            await _documentRepository.UpdateAsync(document, cancellationToken);
            throw;
        }
    }

    public async Task<RagSearchResult> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var effectiveTopK = topK > 0 ? topK : _options.DefaultTopK;
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        var citations = await _vectorSearchRepository.SearchSimilarAsync(
            queryEmbedding,
            effectiveTopK,
            cancellationToken);

        var context = string.Join("\n\n", citations.Select((c, i) =>
            $"[{i + 1}] {c.FileName} (p.{c.PageNumber ?? 0}, score: {c.Score:F3})\n{c.Excerpt}"));

        return new RagSearchResult
        {
            Query = query,
            Citations = citations,
            Context = context
        };
    }

    public Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _documentRepository.GetByIdAsync(documentId, cancellationToken);

    public Task<IReadOnlyList<Document>> ListDocumentsAsync(CancellationToken cancellationToken = default) =>
        _documentRepository.ListAsync(cancellationToken);
}
