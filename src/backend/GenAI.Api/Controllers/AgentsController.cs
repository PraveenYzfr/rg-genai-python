using GenAI.Application.Common.Interfaces;
using GenAI.Application.Common.Models.Agents;
using Microsoft.AspNetCore.Mvc;

namespace GenAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly IAgentOrchestrator _orchestrator;

    public AgentsController(IAgentOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpGet]
    public IActionResult ListAgents() =>
        Ok(new { agents = _orchestrator.GetAvailableAgents() });

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] AgentRunApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.ExecuteAsync(new AgentExecutionRequest
        {
            AgentName = request.AgentName,
            Goal = request.Goal,
            ModelKey = request.ModelKey,
            MaxIterations = request.MaxIterations ?? 10
        }, cancellationToken);

        return Ok(result);
    }
}

public sealed class AgentRunApiRequest
{
    public string AgentName { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string? ModelKey { get; set; }
    public int? MaxIterations { get; set; }
}
