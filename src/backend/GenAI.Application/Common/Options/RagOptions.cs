namespace GenAI.Application.Common.Options;

public sealed class RagOptions
{
    public const string SectionName = "RAG";

    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
    public int DefaultTopK { get; set; } = 5;
    public int EmbeddingDimensions { get; set; } = 1536;
    public string StoragePath { get; set; } = "uploads";
}
