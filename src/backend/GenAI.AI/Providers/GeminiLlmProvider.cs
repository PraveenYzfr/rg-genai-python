using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Options;
using GenAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace GenAI.AI.Providers;

public sealed class GeminiLlmProvider : SemanticKernelLlmProviderBase, ILLMProvider
{
    public GeminiLlmProvider(
        IOptions<LlmOptions> options,
        ILogger<GeminiLlmProvider> logger)
        : base(
            BuildKernel(options.Value),
            options,
            LlmProviderType.Gemini,
            "Gemini",
            options.Value.Gemini.DefaultModel,
            logger)
    {
    }

    private static Kernel BuildKernel(LlmOptions options)
    {
        var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
        builder.AddGoogleAIGeminiChatCompletion(
            modelId: options.Gemini.DefaultModel,
            apiKey: options.Gemini.ApiKey);
#pragma warning restore SKEXP0070
        return builder.Build();
    }
}
