using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace GenAI.Infrastructure.Integrations;

public sealed class ServiceNowClient : IServiceNowClient
{
    private readonly ServiceNowOptions _options;

    public ServiceNowClient(IOptions<ServiceNowOptions> options)
    {
        _options = options.Value;
    }

    public Task<ServiceNowChangeRequest> GenerateChangeRequestAsync(
        ServiceNowCrInput input,
        CancellationToken cancellationToken = default)
    {
        var changesList = string.Join("\n", input.Changes.Select(c => $"- {c}"));
        var risk = input.RiskLevel;

        var cr = new ServiceNowChangeRequest
        {
            ShortDescription = $"Release {input.ReleaseVersion} deployment",
            Description = $"""
                ## Release Summary
                {input.Summary}

                ## Changes Included
                {changesList}

                ## Release Version
                {input.ReleaseVersion}
                """,
            Category = "Software",
            Risk = risk,
            Impact = risk.Equals("High", StringComparison.OrdinalIgnoreCase) ? "High" : "Medium",
            ImplementationPlan = $"""
                1. Deploy release {input.ReleaseVersion} to staging environment
                2. Execute automated regression test suite
                3. Obtain change approval from CAB
                4. Deploy to production during maintenance window
                5. Run smoke tests and monitor dashboards for 30 minutes
                """,
            BackoutPlan = """
                1. Revert to previous release artifact
                2. Restore database snapshot if schema changes were applied
                3. Notify stakeholders and incident commander
                4. Open problem record if rollback was required
                """,
            TestPlan = """
                1. Unit and integration tests passed in CI pipeline
                2. Staging validation completed by QA
                3. Post-deployment smoke tests on critical user journeys
                4. Monitor error rates, latency, and business KPIs
                """
        };

        return Task.FromResult(cr);
    }
}
