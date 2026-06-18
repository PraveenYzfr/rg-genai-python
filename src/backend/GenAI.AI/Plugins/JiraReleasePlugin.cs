using System.ComponentModel;
using System.Text.Json;
using GenAI.Application.Common.Interfaces;
using Microsoft.SemanticKernel;

namespace GenAI.AI.Plugins;

public sealed class JiraReleasePlugin
{
    private readonly IJiraClient _jiraClient;

    public JiraReleasePlugin(IJiraClient jiraClient)
    {
        _jiraClient = jiraClient;
    }

    [KernelFunction("get_jira_release_info")]
    [Description("Reads Jira release information including issues, summaries, and statuses for a release version.")]
    public async Task<string> GetReleaseInfoAsync(
        [Description("The Jira release version identifier, e.g. 2024.06 or RG-1.2.0")] string releaseVersion,
        CancellationToken cancellationToken = default)
    {
        var release = await _jiraClient.GetReleaseInfoAsync(releaseVersion, cancellationToken);
        return JsonSerializer.Serialize(release, new JsonSerializerOptions { WriteIndented = true });
    }
}
