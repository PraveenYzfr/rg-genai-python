using System.ComponentModel;
using System.Text.Json;
using GenAI.Application.Common.Interfaces;
using Microsoft.SemanticKernel;

namespace GenAI.AI.Plugins;

public sealed class RagSearchPlugin
{
    private readonly IRagService _ragService;

    public RagSearchPlugin(IRagService ragService)
    {
        _ragService = ragService;
    }

    [KernelFunction("search_knowledge_base")]
    [Description("Searches the document knowledge base and returns relevant context with source citations.")]
    public async Task<string> SearchKnowledgeBaseAsync(
        [Description("Natural language search query")] string query,
        [Description("Number of results to return")] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _ragService.SearchAsync(query, topK, cancellationToken);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
