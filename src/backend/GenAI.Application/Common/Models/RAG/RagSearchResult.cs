namespace GenAI.Application.Common.Models.RAG;

public sealed class RagSearchResult
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<RagCitation> Citations { get; init; } = Array.Empty<RagCitation>();
    public string Context { get; init; } = string.Empty;
}
