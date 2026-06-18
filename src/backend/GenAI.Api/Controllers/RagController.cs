using GenAI.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GenAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RagController : ControllerBase
{
    private readonly IRagService _ragService;

    public RagController(IRagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] RagSearchRequest request, CancellationToken cancellationToken)
    {
        var result = await _ragService.SearchAsync(request.Query, request.TopK ?? 5, cancellationToken);
        return Ok(result);
    }
}

public sealed class RagSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int? TopK { get; set; }
}
