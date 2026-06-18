using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Models.LLM;
using Microsoft.AspNetCore.Mvc;

namespace GenAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LlmController : ControllerBase
{
    private readonly ILLMProviderFactory _providerFactory;

    public LlmController(ILLMProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    [HttpGet("models")]
    public IActionResult GetModels() =>
        Ok(new { models = _providerFactory.GetAvailableModels() });

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(
        [FromBody] LlmCompletionApiRequest request,
        CancellationToken cancellationToken)
    {
        var provider = ResolveProvider(request.ModelKey);
        var result = await provider.CompleteAsync(new LLMRequest
        {
            SystemPrompt = request.SystemPrompt,
            Messages = request.Messages.Select(m => new LLMMessage { Role = m.Role, Content = m.Content }).ToList(),
            Model = request.Model,
            Temperature = request.Temperature ?? 0.7,
            MaxTokens = request.MaxTokens ?? 4096
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPost("stream")]
    public async Task Stream(
        [FromBody] LlmCompletionApiRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        var provider = ResolveProvider(request.ModelKey);
        await foreach (var chunk in provider.StreamAsync(new LLMRequest
        {
            SystemPrompt = request.SystemPrompt,
            Messages = request.Messages.Select(m => new LLMMessage { Role = m.Role, Content = m.Content }).ToList(),
            Model = request.Model,
            Temperature = request.Temperature ?? 0.7,
            MaxTokens = request.MaxTokens ?? 4096
        }, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                await Response.WriteAsync($"data: {chunk.Content}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            if (chunk.IsComplete)
            {
                await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            }
        }
    }

    private ILLMProvider ResolveProvider(string? modelKey) =>
        string.IsNullOrWhiteSpace(modelKey)
            ? _providerFactory.GetDefaultProvider()
            : _providerFactory.GetProvider(modelKey);
}

public sealed class LlmCompletionApiRequest
{
    public string? ModelKey { get; set; }
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public List<LlmMessageDto> Messages { get; set; } = [];
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}

public sealed class LlmMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}
