namespace GenAI.Application.Common.Options;

public sealed class AgentOptions
{
    public const string SectionName = "Agents";

    public int DefaultMaxIterations { get; set; } = 10;
    public string DefaultModelKey { get; set; } = "gpt-4o-mini";
}

public sealed class JiraOptions
{
    public const string SectionName = "Jira";

    public string BaseUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public bool UseMockData { get; set; } = true;
}

public sealed class ServiceNowOptions
{
    public const string SectionName = "ServiceNow";

    public string InstanceUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseMockData { get; set; } = true;
}
