namespace GenAI.Application.Common.Models.LLM;

public sealed class LLMRequest
{
    public IReadOnlyList<LLMMessage> Messages { get; init; } = Array.Empty<LLMMessage>();
    public string? SystemPrompt { get; init; }
    public string? Model { get; init; }
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 4096;
}
