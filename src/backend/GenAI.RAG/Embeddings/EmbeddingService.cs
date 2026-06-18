using GenAI.Application.Common.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace GenAI.RAG.Embeddings;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}

public sealed class SemanticKernelEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public SemanticKernelEmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await _embeddingGenerator.GenerateAsync([text], cancellationToken: cancellationToken);
        return result[0].Vector.ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var batch = texts.ToList();
        var result = await _embeddingGenerator.GenerateAsync(batch, cancellationToken: cancellationToken);
        return result.Select(e => e.Vector.ToArray()).ToList();
    }
}
