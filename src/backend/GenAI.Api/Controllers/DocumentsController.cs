using GenAI.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GenAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IRagService _ragService;

    public DocumentsController(IRagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "File is empty." });
        }

        await using var stream = file.OpenReadStream();
        var document = await _ragService.IngestDocumentAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return Ok(new
        {
            document.Id,
            document.FileName,
            document.Status,
            document.CreatedAt,
            chunkCount = document.Chunks.Count
        });
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var documents = await _ragService.ListDocumentsAsync(cancellationToken);
        return Ok(documents.Select(d => new
        {
            d.Id,
            d.FileName,
            d.ContentType,
            d.Status,
            d.CreatedAt,
            d.FileSizeBytes
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var document = await _ragService.GetDocumentAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            document.Id,
            document.FileName,
            document.ContentType,
            document.Status,
            document.ErrorMessage,
            document.CreatedAt,
            chunkCount = document.Chunks.Count
        });
    }
}
