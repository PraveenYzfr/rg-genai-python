namespace GenAI.Application.Common.Models.LLM;

public sealed class LLMStreamChunk
{
    public string Content { get; init; } = string.Empty;
    public bool IsComplete { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
}
