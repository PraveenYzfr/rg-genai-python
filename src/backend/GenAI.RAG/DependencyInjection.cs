using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Options;
using GenAI.RAG.Embeddings;
using GenAI.RAG.Ingestion;
using GenAI.RAG.Retrieval;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace GenAI.RAG;

public static class DependencyInjection
{
    public static IServiceCollection AddRAG(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));

        var llmOptions = configuration.GetSection(LlmOptions.SectionName).Get<LlmOptions>() ?? new LlmOptions();

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var kernel = Kernel.CreateBuilder()
                .AddOpenAIEmbeddingGenerator(
                    modelId: llmOptions.OpenAI.EmbeddingModel,
                    apiKey: llmOptions.OpenAI.ApiKey)
                .Build();

            return kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        });

        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, PlainTextDocumentParser>();
        services.AddSingleton<TextChunker>();
        services.AddScoped<IEmbeddingService, SemanticKernelEmbeddingService>();
        services.AddScoped<IRagService, RagService>();

        return services;
    }
}
