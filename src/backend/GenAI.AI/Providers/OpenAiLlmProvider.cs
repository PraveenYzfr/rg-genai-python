using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Options;
using GenAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace GenAI.AI.Providers;

public sealed class OpenAiLlmProvider : SemanticKernelLlmProviderBase, ILLMProvider
{
    public OpenAiLlmProvider(
        IOptions<LlmOptions> options,
        ILogger<OpenAiLlmProvider> logger)
        : base(
            BuildKernel(options.Value),
            options,
            LlmProviderType.OpenAI,
            "OpenAI",
            options.Value.OpenAI.DefaultModel,
            logger)
    {
    }

    private static Kernel BuildKernel(LlmOptions options)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: options.OpenAI.DefaultModel,
            apiKey: options.OpenAI.ApiKey);
        return builder.Build();
    }
}
