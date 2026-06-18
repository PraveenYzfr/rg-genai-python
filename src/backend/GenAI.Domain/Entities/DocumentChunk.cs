using GenAI.Domain.Common;

namespace GenAI.Domain.Entities;

public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public float[]? Embedding { get; set; }
}
