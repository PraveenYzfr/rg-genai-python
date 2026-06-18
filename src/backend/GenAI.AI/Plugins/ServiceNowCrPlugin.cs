using System.ComponentModel;
using System.Text.Json;
using GenAI.Application.Common.Interfaces;
using Microsoft.SemanticKernel;

namespace GenAI.AI.Plugins;

public sealed class ServiceNowCrPlugin
{
    private readonly IServiceNowClient _serviceNowClient;

    public ServiceNowCrPlugin(IServiceNowClient serviceNowClient)
    {
        _serviceNowClient = serviceNowClient;
    }

    [KernelFunction("generate_servicenow_change_request")]
    [Description("Generates ServiceNow change request details from a release summary and list of changes.")]
    public async Task<string> GenerateChangeRequestAsync(
        [Description("Release version for the change request")] string releaseVersion,
        [Description("Executive summary of the release changes")] string summary,
        [Description("Comma-separated list of individual changes")] string changes,
        [Description("Risk level: Low, Moderate, or High")] string riskLevel = "Moderate",
        CancellationToken cancellationToken = default)
    {
        var input = new ServiceNowCrInput
        {
            ReleaseVersion = releaseVersion,
            Summary = summary,
            Changes = changes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            RiskLevel = riskLevel
        };

        var cr = await _serviceNowClient.GenerateChangeRequestAsync(input, cancellationToken);
        return JsonSerializer.Serialize(cr, new JsonSerializerOptions { WriteIndented = true });
    }
}
