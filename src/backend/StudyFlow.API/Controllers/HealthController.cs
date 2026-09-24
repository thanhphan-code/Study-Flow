using Microsoft.AspNetCore.Mvc;

namespace StudyFlow.API.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse("healthy", DateTimeOffset.UtcNow));
}

public sealed record HealthResponse(string Status, DateTimeOffset Timestamp);
