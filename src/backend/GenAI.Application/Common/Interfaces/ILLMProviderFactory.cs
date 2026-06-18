using GenAI.Domain.Enums;

namespace GenAI.Application.Common.Interfaces;

public interface ILLMProviderFactory
{
    ILLMProvider GetProvider(LlmProviderType providerType);
    ILLMProvider GetProvider(string modelKey);
    ILLMProvider GetDefaultProvider();
    IReadOnlyList<string> GetAvailableModels();
}
