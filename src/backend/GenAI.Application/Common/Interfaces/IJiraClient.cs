namespace GenAI.Application.Common.Interfaces;

public interface IJiraClient
{
    Task<JiraReleaseInfo> GetReleaseInfoAsync(string releaseVersion, CancellationToken cancellationToken = default);
}

public sealed class JiraReleaseInfo
{
    public string ReleaseVersion { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public IReadOnlyList<JiraIssueSummary> Issues { get; init; } = Array.Empty<JiraIssueSummary>();
}

public sealed class JiraIssueSummary
{
    public string Key { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string IssueType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
}
