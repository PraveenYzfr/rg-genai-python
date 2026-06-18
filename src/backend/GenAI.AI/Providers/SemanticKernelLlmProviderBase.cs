using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Models.LLM;
using GenAI.Application.Common.Options;
using GenAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace GenAI.AI.Providers;

public abstract class SemanticKernelLlmProviderBase : ILLMProvider
{
    private readonly Kernel _kernel;
    private readonly ILogger _logger;
    private readonly string _defaultModel;

    protected SemanticKernelLlmProviderBase(
        Kernel kernel,
        IOptions<LlmOptions> options,
        LlmProviderType providerType,
        string providerName,
        string defaultModel,
        ILogger logger)
    {
        _kernel = kernel;
        _logger = logger;
        ProviderType = providerType;
        ProviderName = providerName;
        _defaultModel = defaultModel;
    }

    public LlmProviderType ProviderType { get; }
    public string ProviderName { get; }

    public async Task<LLMCompletionResult> CompleteAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var history = BuildChatHistory(request);
        var settings = BuildSettings(request);

        var model = request.Model ?? _defaultModel;
        _logger.LogDebug("LLM complete: {Provider}/{Model}", ProviderName, model);

        var response = await chat.GetChatMessageContentAsync(
            history,
            settings,
            _kernel,
            cancellationToken);

        return new LLMCompletionResult
        {
            Content = response.Content ?? string.Empty,
            Provider = ProviderName,
            Model = model
        };
    }

    public async IAsyncEnumerable<LLMStreamChunk> StreamAsync(
        LLMRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var history = BuildChatHistory(request);
        var settings = BuildSettings(request);
        var model = request.Model ?? _defaultModel;

        await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(
                           history,
                           settings,
                           _kernel,
                           cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return new LLMStreamChunk
                {
                    Content = chunk.Content,
                    IsComplete = false,
                    Provider = ProviderName,
                    Model = model
                };
            }
        }

        yield return new LLMStreamChunk
        {
            Content = string.Empty,
            IsComplete = true,
            Provider = ProviderName,
            Model = model
        };
    }

    private static ChatHistory BuildChatHistory(LLMRequest request)
    {
        var history = new ChatHistory();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            history.AddSystemMessage(request.SystemPrompt);
        }

        foreach (var message in request.Messages)
        {
            switch (message.Role.ToLowerInvariant())
            {
                case "assistant":
                    history.AddAssistantMessage(message.Content);
                    break;
                case "system":
                    history.AddSystemMessage(message.Content);
                    break;
                default:
                    history.AddUserMessage(message.Content);
                    break;
            }
        }

        return history;
    }

    private static OpenAIPromptExecutionSettings BuildSettings(LLMRequest request)
    {
        return new OpenAIPromptExecutionSettings
        {
            Temperature = (float)request.Temperature,
            MaxTokens = request.MaxTokens
        };
    }
}
