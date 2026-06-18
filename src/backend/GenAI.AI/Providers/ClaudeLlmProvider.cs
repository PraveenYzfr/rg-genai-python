using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Models.LLM;
using GenAI.Application.Common.Options;
using GenAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenAI.AI.Providers;

public sealed class ClaudeLlmProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ClaudeProviderOptions _options;
    private readonly ILogger<ClaudeLlmProvider> _logger;

    public ClaudeLlmProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<LlmOptions> options,
        ILogger<ClaudeLlmProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(ClaudeLlmProvider));
        _options = options.Value.Claude;
        _logger = logger;
    }

    public LlmProviderType ProviderType => LlmProviderType.Claude;
    public string ProviderName => "Claude";

    public async Task<LLMCompletionResult> CompleteAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? _options.DefaultModel;
        var payload = BuildPayload(request, model, stream: false);

        using var httpRequest = CreateRequest(payload);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Claude API.");

        var text = body.Content?.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty;

        return new LLMCompletionResult
        {
            Content = text,
            Provider = ProviderName,
            Model = model,
            InputTokens = body.Usage?.InputTokens,
            OutputTokens = body.Usage?.OutputTokens
        };
    }

    public async IAsyncEnumerable<LLMStreamChunk> StreamAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? _options.DefaultModel;
        var payload = BuildPayload(request, model, stream: true);

        using var httpRequest = CreateRequest(payload);
        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line["data: ".Length..];
            if (data == "[DONE]")
            {
                break;
            }

            AnthropicStreamEvent? streamEvent;
            try
            {
                streamEvent = JsonSerializer.Deserialize<AnthropicStreamEvent>(data);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Claude stream event.");
                continue;
            }

            if (streamEvent?.Type == "content_block_delta"
                && streamEvent.Delta?.Type == "text_delta"
                && !string.IsNullOrEmpty(streamEvent.Delta.Text))
            {
                yield return new LLMStreamChunk
                {
                    Content = streamEvent.Delta.Text,
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

    private HttpRequestMessage CreateRequest(object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        return request;
    }

    private static object BuildPayload(LLMRequest request, string model, bool stream)
    {
        var messages = request.Messages
            .Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
            .Select(m => new { role = m.Role, content = m.Content })
            .ToList();

        return new
        {
            model,
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            system = request.SystemPrompt,
            stream,
            messages
        };
    }

    private sealed class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContent>? Content { get; set; }

        [JsonPropertyName("usage")]
        public AnthropicUsage? Usage { get; set; }
    }

    private sealed class AnthropicContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class AnthropicUsage
    {
        [JsonPropertyName("input_tokens")]
        public int? InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int? OutputTokens { get; set; }
    }

    private sealed class AnthropicStreamEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("delta")]
        public AnthropicDelta? Delta { get; set; }
    }

    private sealed class AnthropicDelta
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
