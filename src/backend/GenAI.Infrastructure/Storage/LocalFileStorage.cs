using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace GenAI.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly RagOptions _options;

    public LocalFileStorage(IOptions<RagOptions> options)
    {
        _options = options.Value;
        Directory.CreateDirectory(_options.StoragePath);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var safeName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var path = Path.Combine(_options.StoragePath, safeName);

        await using var fileStream = File.Create(path);
        await content.CopyToAsync(fileStream, cancellationToken);

        return path;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        Stream stream = File.OpenRead(storagePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }

        return Task.CompletedTask;
    }
}
