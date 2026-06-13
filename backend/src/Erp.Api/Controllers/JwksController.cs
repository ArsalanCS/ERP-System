using Erp.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Publishes the JSON Web Key Set so clients/services can validate RS256 access
/// tokens (CLAUDE.md §4.5). Public key material only.
/// </summary>
[ApiController]
public sealed class JwksController(IJwtKeyProvider keyProvider) : ControllerBase
{
    [HttpGet("api/v1/.well-known/jwks.json")]
    [HttpGet(".well-known/jwks.json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(keyProvider.GetJwks());
}
