using GenAI.AI.Plugins;
using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Models.Agents;
using GenAI.Application.Common.Models.LLM;
using GenAI.Application.Common.Options;
using GenAI.Domain.Entities;
using GenAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace GenAI.AI.Agents;

public sealed class ReleaseCoordinatorAgent
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly JiraReleasePlugin _jiraPlugin;
    private readonly ServiceNowCrPlugin _serviceNowPlugin;
    private readonly AgentOptions _options;
    private readonly ILogger<ReleaseCoordinatorAgent> _logger;

    public const string AgentName = "ReleaseCoordinator";

    public ReleaseCoordinatorAgent(
        ILLMProviderFactory providerFactory,
        JiraReleasePlugin jiraPlugin,
        ServiceNowCrPlugin serviceNowPlugin,
        IOptions<AgentOptions> options,
        ILogger<ReleaseCoordinatorAgent> logger)
    {
        _providerFactory = providerFactory;
        _jiraPlugin = jiraPlugin;
        _serviceNowPlugin = serviceNowPlugin;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> RunAsync(
        AgentExecutionRequest request,
        AgentRun run,
        IAgentRunRepository repository,
        CancellationToken cancellationToken)
    {
        var steps = new List<AgentStepResult>();
        var stepIndex = 0;

        try
        {
            var releaseVersion = ExtractReleaseVersion(request.Goal);

            // Step 1: Read Jira release information
            var jiraStart = DateTimeOffset.UtcNow;
            var jiraData = await _jiraPlugin.GetReleaseInfoAsync(releaseVersion, cancellationToken);
            var jiraStep = CreateStep(run, stepIndex++, "tool_call", releaseVersion, jiraData, jiraStart);
            await repository.UpdateAsync(run);
            steps.Add(MapStep(jiraStep));

            // Step 2: Summarize changes via LLM
            var summaryStart = DateTimeOffset.UtcNow;
            var provider = _providerFactory.GetProvider(request.ModelKey ?? _options.DefaultModelKey);
            var summaryResult = await provider.CompleteAsync(new LLMRequest
            {
                SystemPrompt = """
                    You are a release manager. Summarize Jira release data into a concise executive summary
                    and a bullet list of key changes grouped by issue type. Focus on user impact and risk.
                    """,
                Messages =
                [
                    new LLMMessage { Role = "user", Content = $"Summarize this Jira release data:\n{jiraData}" }
                ],
                MaxTokens = 2048,
                Temperature = 0.3
            }, cancellationToken);

            var summaryStep = CreateStep(run, stepIndex++, "llm_call", jiraData, summaryResult.Content, summaryStart);
            await repository.UpdateAsync(run);
            steps.Add(MapStep(summaryStep));

            // Step 3: Generate ServiceNow CR details
            var crStart = DateTimeOffset.UtcNow;
            var changes = ExtractChangeLines(summaryResult.Content);
            var crData = await _serviceNowPlugin.GenerateChangeRequestAsync(
                releaseVersion,
                summaryResult.Content,
                string.Join(", ", changes),
                "Moderate",
                cancellationToken);

            var crStep = CreateStep(run, stepIndex++, "tool_call", summaryResult.Content, crData, crStart);
            await repository.UpdateAsync(run);
            steps.Add(MapStep(crStep));

            run.Status = AgentRunStatus.Completed;
            run.OutputSummary = crData;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.UpdatedAt = DateTimeOffset.UtcNow;
            await repository.UpdateAsync(run);

            return new AgentExecutionResult
            {
                RunId = run.Id,
                AgentName = AgentName,
                Status = run.Status.ToString(),
                Output = crData,
                Steps = steps
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Release Coordinator agent failed.");
            run.Status = AgentRunStatus.Failed;
            run.ErrorMessage = ex.Message;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.UpdatedAt = DateTimeOffset.UtcNow;
            await repository.UpdateAsync(run);

            return new AgentExecutionResult
            {
                RunId = run.Id,
                AgentName = AgentName,
                Status = AgentRunStatus.Failed.ToString(),
                ErrorMessage = ex.Message,
                Steps = steps
            };
        }
    }

    private static string ExtractReleaseVersion(string goal)
    {
        var tokens = goal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.LastOrDefault(t => t.Contains('.') || t.Contains('-')) ?? goal;
    }

    private static IReadOnlyList<string> ExtractChangeLines(string summary) =>
        summary.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.TrimStart().StartsWith('-') || line.TrimStart().StartsWith('*'))
            .Select(line => line.Trim().TrimStart('-', '*', ' '))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .DefaultIfEmpty(summary)
            .Take(10)
            .ToList();

    private static AgentStep CreateStep(
        AgentRun run,
        int index,
        string type,
        string? input,
        string? output,
        DateTimeOffset started)
    {
        var step = new AgentStep
        {
            Id = Guid.NewGuid(),
            AgentRunId = run.Id,
            StepIndex = index,
            StepType = type,
            InputJson = input,
            OutputJson = output,
            DurationMs = (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds,
            CreatedAt = DateTimeOffset.UtcNow
        };
        run.Steps.Add(step);
        return step;
    }

    private static AgentStepResult MapStep(AgentStep step) => new()
    {
        StepIndex = step.StepIndex,
        StepType = step.StepType,
        Input = step.InputJson,
        Output = step.OutputJson,
        DurationMs = step.DurationMs
    };
}
