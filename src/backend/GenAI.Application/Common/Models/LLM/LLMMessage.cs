namespace GenAI.Application.Common.Models.LLM;

public sealed class LLMMessage
{
    public string Role { get; init; } = "user";
    public string Content { get; init; } = string.Empty;
}
