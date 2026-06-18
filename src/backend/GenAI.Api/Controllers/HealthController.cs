using Microsoft.AspNetCore.Mvc;

namespace GenAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", service = "GenAI.Api" });
}
