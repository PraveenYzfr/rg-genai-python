using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenAI.Infrastructure.Integrations;

public sealed class JiraClient : IJiraClient
{
    private readonly HttpClient _httpClient;
    private readonly JiraOptions _options;
    private readonly ILogger<JiraClient> _logger;

    public JiraClient(HttpClient httpClient, IOptions<JiraOptions> options, ILogger<JiraClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<JiraReleaseInfo> GetReleaseInfoAsync(
        string releaseVersion,
        CancellationToken cancellationToken = default)
    {
        if (_options.UseMockData || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return BuildMockRelease(releaseVersion);
        }

        var jql = Uri.EscapeDataString(
            $"project = {_options.ProjectKey} AND fixVersion = \"{releaseVersion}\" ORDER BY priority DESC");

        var requestUri = $"{_options.BaseUrl.TrimEnd('/')}/rest/api/3/search?jql={jql}&maxResults=100";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Email}:{_options.ApiToken}")));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var issues = document.RootElement.GetProperty("issues").EnumerateArray()
            .Select(issue => new JiraIssueSummary
            {
                Key = issue.GetProperty("key").GetString() ?? string.Empty,
                Summary = issue.GetProperty("fields").GetProperty("summary").GetString() ?? string.Empty,
                IssueType = issue.GetProperty("fields").GetProperty("issuetype").GetProperty("name").GetString() ?? string.Empty,
                Status = issue.GetProperty("fields").GetProperty("status").GetProperty("name").GetString() ?? string.Empty,
                Description = issue.GetProperty("fields").TryGetProperty("description", out var desc) ? desc.ToString() : null
            })
            .ToList();

        return new JiraReleaseInfo
        {
            ReleaseVersion = releaseVersion,
            ProjectKey = _options.ProjectKey,
            Issues = issues
        };
    }

    private static JiraReleaseInfo BuildMockRelease(string releaseVersion) => new()
    {
        ReleaseVersion = releaseVersion,
        ProjectKey = "RG",
        Issues =
        [
            new JiraIssueSummary
            {
                Key = "RG-101",
                Summary = "Add multi-model LLM provider abstraction",
                IssueType = "Story",
                Status = "Done",
                Description = "Implemented OpenAI, Claude, and Gemini providers with streaming."
            },
            new JiraIssueSummary
            {
                Key = "RG-102",
                Summary = "Implement RAG pipeline with pgvector",
                IssueType = "Story",
                Status = "Done",
                Description = "PDF upload, chunking, embeddings, similarity search."
            },
            new JiraIssueSummary
            {
                Key = "RG-103",
                Summary = "Release Coordinator Agent",
                IssueType = "Feature",
                Status = "Done",
                Description = "Jira to ServiceNow CR automation."
            },
            new JiraIssueSummary
            {
                Key = "RG-104",
                Summary = "Fix authentication token refresh",
                IssueType = "Bug",
                Status = "Done",
                Description = "Resolved session expiry issue affecting API clients."
            }
        ]
    };
}
