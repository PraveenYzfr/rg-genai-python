namespace GenAI.Application.Common.Models.RAG;

public sealed class RagCitation
{
    public Guid DocumentId { get; init; }
    public Guid ChunkId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public int? PageNumber { get; init; }
    public int ChunkIndex { get; init; }
    public string Excerpt { get; init; } = string.Empty;
    public double Score { get; init; }
}
