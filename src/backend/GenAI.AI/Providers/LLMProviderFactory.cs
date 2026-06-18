using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Options;
using GenAI.Domain.Enums;
using GenAI.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GenAI.AI.Providers;

public sealed class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LlmOptions _options;

    public LLMProviderFactory(IServiceProvider serviceProvider, IOptions<LlmOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public ILLMProvider GetProvider(LlmProviderType providerType) =>
        providerType switch
        {
            LlmProviderType.OpenAI when _options.OpenAI.Enabled =>
                _serviceProvider.GetRequiredService<OpenAiLlmProvider>(),
            LlmProviderType.Claude when _options.Claude.Enabled =>
                _serviceProvider.GetRequiredService<ClaudeLlmProvider>(),
            LlmProviderType.Gemini when _options.Gemini.Enabled =>
                _serviceProvider.GetRequiredService<GeminiLlmProvider>(),
            _ => throw new DomainException($"Provider '{providerType}' is not enabled or configured.")
        };

    public ILLMProvider GetProvider(string modelKey)
    {
        var mapping = _options.ModelMappings.FirstOrDefault(m =>
            string.Equals(m.Key, modelKey, StringComparison.OrdinalIgnoreCase));

        if (mapping is not null)
        {
            if (Enum.TryParse<LlmProviderType>(mapping.Provider, true, out var mappedType))
            {
                return GetProvider(mappedType);
            }
        }

        if (modelKey.Contains("gpt", StringComparison.OrdinalIgnoreCase))
        {
            return GetProvider(LlmProviderType.OpenAI);
        }

        if (modelKey.Contains("claude", StringComparison.OrdinalIgnoreCase))
        {
            return GetProvider(LlmProviderType.Claude);
        }

        if (modelKey.Contains("gemini", StringComparison.OrdinalIgnoreCase))
        {
            return GetProvider(LlmProviderType.Gemini);
        }

        return GetDefaultProvider();
    }

    public ILLMProvider GetDefaultProvider()
    {
        if (Enum.TryParse<LlmProviderType>(_options.DefaultProvider, true, out var providerType))
        {
            return GetProvider(providerType);
        }

        return GetProvider(LlmProviderType.OpenAI);
    }

    public IReadOnlyList<string> GetAvailableModels() =>
        _options.ModelMappings.Select(m => m.Key).ToList();
}
