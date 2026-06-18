using GenAI.AI.Agents;
using GenAI.AI.Plugins;
using GenAI.AI.Providers;
using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenAI.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));
        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));

        services.AddHttpClient(nameof(ClaudeLlmProvider));

        services.AddSingleton<OpenAiLlmProvider>();
        services.AddSingleton<GeminiLlmProvider>();
        services.AddSingleton<ClaudeLlmProvider>();

        services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();

        services.AddScoped<JiraReleasePlugin>();
        services.AddScoped<ServiceNowCrPlugin>();
        services.AddScoped<RagSearchPlugin>();
        services.AddScoped<ReleaseCoordinatorAgent>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

        return services;
    }
}
