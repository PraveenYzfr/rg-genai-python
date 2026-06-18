using GenAI.AI.Plugins;
using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Models.Agents;
using GenAI.Application.Common.Options;
using GenAI.Domain.Entities;
using GenAI.Domain.Enums;
using GenAI.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace GenAI.AI.Agents;

public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ReleaseCoordinatorAgent _releaseCoordinator;
    private readonly IAgentRunRepository _repository;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly RagSearchPlugin _ragSearchPlugin;
    private readonly JiraReleasePlugin _jiraPlugin;
    private readonly ServiceNowCrPlugin _serviceNowPlugin;
    private readonly LlmOptions _llmOptions;
    private readonly AgentOptions _options;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        ReleaseCoordinatorAgent releaseCoordinator,
        IAgentRunRepository repository,
        ILLMProviderFactory providerFactory,
        RagSearchPlugin ragSearchPlugin,
        JiraReleasePlugin jiraPlugin,
        ServiceNowCrPlugin serviceNowPlugin,
        IOptions<LlmOptions> llmOptions,
        IOptions<AgentOptions> options,
        ILogger<AgentOrchestrator> logger)
    {
        _releaseCoordinator = releaseCoordinator;
        _repository = repository;
        _providerFactory = providerFactory;
        _ragSearchPlugin = ragSearchPlugin;
        _jiraPlugin = jiraPlugin;
        _serviceNowPlugin = serviceNowPlugin;
        _llmOptions = llmOptions.Value;
        _options = options.Value;
        _logger = logger;
    }

    public IReadOnlyList<string> GetAvailableAgents() =>
        [ReleaseCoordinatorAgent.AgentName, "GeneralPlanner"];

    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var run = new AgentRun
        {
            Id = Guid.NewGuid(),
            AgentName = request.AgentName,
            InputGoal = request.Goal,
            Status = AgentRunStatus.Running,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(run, cancellationToken);

        if (string.Equals(request.AgentName, ReleaseCoordinatorAgent.AgentName, StringComparison.OrdinalIgnoreCase))
        {
            return await _releaseCoordinator.RunAsync(request, run, _repository, cancellationToken);
        }

        if (string.Equals(request.AgentName, "GeneralPlanner", StringComparison.OrdinalIgnoreCase))
        {
            return await RunGeneralPlannerAsync(request, run, cancellationToken);
        }

        throw new DomainException($"Unknown agent '{request.AgentName}'.");
    }

    private async Task<AgentExecutionResult> RunGeneralPlannerAsync(
        AgentExecutionRequest request,
        AgentRun run,
        CancellationToken cancellationToken)
    {
        var steps = new List<AgentStepResult>();
        var kernel = BuildPlannerKernel();
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory("""
            You are an enterprise AI agent with access to tools for Jira, ServiceNow, and knowledge base search.
            Plan and execute multi-step tasks. Use tools when needed. Be concise and actionable.
            """);
        history.AddUserMessage(request.Goal);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true),
            Temperature = 0.2f,
            MaxTokens = 4096
        };

        var maxIterations = request.MaxIterations > 0 ? request.MaxIterations : _options.DefaultMaxIterations;
        ChatMessageContent? finalResponse = null;

        for (var i = 0; i < maxIterations; i++)
        {
            var stepStart = DateTimeOffset.UtcNow;
            var response = await chat.GetChatMessageContentAsync(history, settings, kernel, cancellationToken);
            history.Add(response);
            finalResponse = response;

            var step = new AgentStep
            {
                Id = Guid.NewGuid(),
                AgentRunId = run.Id,
                StepIndex = i,
                StepType = "llm_call",
                InputJson = request.Goal,
                OutputJson = response.Content,
                DurationMs = (int)(DateTimeOffset.UtcNow - stepStart).TotalMilliseconds,
                CreatedAt = DateTimeOffset.UtcNow
            };
            run.Steps.Add(step);
            steps.Add(new AgentStepResult
            {
                StepIndex = step.StepIndex,
                StepType = step.StepType,
                Input = step.InputJson,
                Output = step.OutputJson,
                DurationMs = step.DurationMs
            });

            if (!response.Items.Any(item => item is FunctionCallContent))
            {
                break;
            }
        }

        run.Status = AgentRunStatus.Completed;
        run.OutputSummary = finalResponse?.Content;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(run, cancellationToken);

        return new AgentExecutionResult
        {
            RunId = run.Id,
            AgentName = request.AgentName,
            Status = run.Status.ToString(),
            Output = run.OutputSummary,
            Steps = steps
        };
    }

    private Kernel BuildPlannerKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: _llmOptions.OpenAI.DefaultModel,
            apiKey: _llmOptions.OpenAI.ApiKey);

        builder.Plugins.AddFromObject(_jiraPlugin, "jira");
        builder.Plugins.AddFromObject(_serviceNowPlugin, "servicenow");
        builder.Plugins.AddFromObject(_ragSearchPlugin, "rag");

        return builder.Build();
    }
}
