namespace GenAI.Application.Common.Options;

public sealed class LlmOptions
{
    public const string SectionName = "LLM";

    public string DefaultProvider { get; set; } = "OpenAI";
    public string DefaultModel { get; set; } = "gpt-4o-mini";
    public OpenAiProviderOptions OpenAI { get; set; } = new();
    public ClaudeProviderOptions Claude { get; set; } = new();
    public GeminiProviderOptions Gemini { get; set; } = new();
    public List<ModelMappingOptions> ModelMappings { get; set; } = new();
}

public sealed class OpenAiProviderOptions
{
    public bool Enabled { get; set; } = true;
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}

public sealed class ClaudeProviderOptions
{
    public bool Enabled { get; set; } = true;
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "claude-3-5-sonnet-20241022";
    public string ApiUrl { get; set; } = "https://api.anthropic.com/v1/messages";
}

public sealed class GeminiProviderOptions
{
    public bool Enabled { get; set; } = true;
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "gemini-1.5-pro";
}

public sealed class ModelMappingOptions
{
    public string Key { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
