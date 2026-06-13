using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Liveness/metadata endpoint. Unauthenticated by design (no tenant data).
/// Demonstrates the versioned REST convention: <c>/api/v1/...</c>.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse(
        Status: "healthy",
        Service: "Erp.Api",
        UtcNow: DateTimeOffset.UtcNow));
}

public sealed record HealthResponse(string Status, string Service, DateTimeOffset UtcNow);
