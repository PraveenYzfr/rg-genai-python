namespace GenAI.Application.Common.Interfaces;

public interface IServiceNowClient
{
    Task<ServiceNowChangeRequest> GenerateChangeRequestAsync(ServiceNowCrInput input, CancellationToken cancellationToken = default);
}

public sealed class ServiceNowCrInput
{
    public string ReleaseVersion { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Changes { get; init; } = Array.Empty<string>();
    public string RiskLevel { get; init; } = "Moderate";
}

public sealed class ServiceNowChangeRequest
{
    public string ShortDescription { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Risk { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string ImplementationPlan { get; init; } = string.Empty;
    public string BackoutPlan { get; init; } = string.Empty;
    public string TestPlan { get; init; } = string.Empty;
}
