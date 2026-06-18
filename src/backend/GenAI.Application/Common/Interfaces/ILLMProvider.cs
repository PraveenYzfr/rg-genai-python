using GenAI.Application.Common.Models.LLM;
using GenAI.Domain.Enums;

namespace GenAI.Application.Common.Interfaces;

public interface ILLMProvider
{
    LlmProviderType ProviderType { get; }
    string ProviderName { get; }
    Task<LLMCompletionResult> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<LLMStreamChunk> StreamAsync(LLMRequest request, CancellationToken cancellationToken = default);
}
