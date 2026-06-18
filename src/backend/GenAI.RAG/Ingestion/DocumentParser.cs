using GenAI.Application.Common.Interfaces;

namespace GenAI.RAG.Ingestion;

public interface IDocumentParser
{
    bool CanParse(string contentType, string fileName);
    Task<string> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}

public sealed class PdfDocumentParser : IDocumentParser
{
    public bool CanParse(string contentType, string fileName) =>
        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        using var document = UglyToad.PdfPig.PdfDocument.Open(stream);
        var pages = document.GetPages()
            .Select((page, index) => $"[Page {index + 1}]\n{page.Text}")
            .ToList();

        return Task.FromResult(string.Join("\n\n", pages));
    }
}

public sealed class PlainTextDocumentParser : IDocumentParser
{
    public bool CanParse(string contentType, string fileName) =>
        contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}

public sealed class CompositeDocumentParser(IEnumerable<IDocumentParser> parsers) : IDocumentParser
{
    public bool CanParse(string contentType, string fileName) =>
        parsers.Any(p => p.CanParse(contentType, fileName));

    public async Task<string> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var contentType = GetContentType(fileName);
        var parser = parsers.FirstOrDefault(p => p.CanParse(contentType, fileName))
            ?? throw new NotSupportedException($"Unsupported document type: {fileName}");

        return await parser.ParseAsync(stream, fileName, cancellationToken);
    }

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };
}
