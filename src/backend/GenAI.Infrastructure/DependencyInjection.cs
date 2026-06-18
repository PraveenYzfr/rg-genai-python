using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Options;
using GenAI.Infrastructure.Integrations;
using GenAI.Infrastructure.Persistence;
using GenAI.Infrastructure.Repositories;
using GenAI.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace GenAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));
        services.Configure<JiraOptions>(configuration.GetSection(JiraOptions.SectionName));
        services.Configure<ServiceNowOptions>(configuration.GetSection(ServiceNowOptions.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.UseVector()));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IVectorSearchRepository, VectorSearchRepository>();
        services.AddScoped<IAgentRunRepository, AgentRunRepository>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.AddHttpClient<IJiraClient, JiraClient>();

        services.AddScoped<IServiceNowClient, ServiceNowClient>();

        return services;
    }
}
