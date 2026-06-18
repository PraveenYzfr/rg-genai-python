namespace GenAI.Application.Common.Models.LLM;

public sealed class LLMCompletionResult
{
    public string Content { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
}
