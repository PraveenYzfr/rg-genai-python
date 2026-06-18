using GenAI.Application.Common.Options;

namespace GenAI.RAG.Ingestion;

public sealed class TextChunker
{
    private readonly RagOptions _options;

    public TextChunker(Microsoft.Extensions.Options.IOptions<RagOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<TextChunk>();
        }

        var chunks = new List<TextChunk>();
        var chunkSize = _options.ChunkSize;
        var overlap = _options.ChunkOverlap;
        var index = 0;
        var position = 0;

        while (position < text.Length)
        {
            var length = Math.Min(chunkSize, text.Length - position);
            var content = text.Substring(position, length).Trim();

            if (!string.IsNullOrWhiteSpace(content))
            {
                chunks.Add(new TextChunk
                {
                    Index = index++,
                    Content = content,
                    PageNumber = ExtractPageNumber(content)
                });
            }

            if (position + length >= text.Length)
            {
                break;
            }

            position += chunkSize - overlap;
        }

        return chunks;
    }

    private static int? ExtractPageNumber(string content)
    {
        if (content.StartsWith("[Page ", StringComparison.Ordinal))
        {
            var end = content.IndexOf(']', 6);
            if (end > 6 && int.TryParse(content[6..end], out var page))
            {
                return page;
            }
        }

        return null;
    }
}

public sealed class TextChunk
{
    public int Index { get; init; }
    public string Content { get; init; } = string.Empty;
    public int? PageNumber { get; init; }
}
