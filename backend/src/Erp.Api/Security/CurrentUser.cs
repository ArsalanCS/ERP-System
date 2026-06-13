using System.Security.Claims;
using Erp.Application.Abstractions;

namespace Erp.Api.Security;

/// <summary>
/// Resolves the caller's security context from the validated JWT claims on the
/// current HttpContext. Returns an unauthenticated context when there is no
/// principal. Scope is always derived here from the token — never from
/// client-supplied request values (CLAUDE.md §4.1).
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ErpClaimTypes.UserId) ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    public Guid? WorkspaceId =>
        Guid.TryParse(Principal?.FindFirstValue(ErpClaimTypes.WorkspaceId), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue(ErpClaimTypes.Email);

    public bool IsPlatformAdmin =>
        string.Equals(Principal?.FindFirstValue(ErpClaimTypes.PlatformAdmin), "true", StringComparison.OrdinalIgnoreCase);

    public IReadOnlySet<Guid> ClusterIds =>
        Principal?.FindAll(ErpClaimTypes.Cluster)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet() ?? [];

    public IReadOnlySet<string> Actions =>
        Principal?.FindAll(ErpClaimTypes.Action).Select(c => c.Value).ToHashSet() ?? [];

    public bool Can(string action) => Actions.Contains(action);
}

/// <summary>Custom JWT claim type names used across the platform.</summary>
public static class ErpClaimTypes
{
    public const string UserId = "uid";
    public const string WorkspaceId = "wsid";
    public const string Email = "email";
    public const string PlatformAdmin = "padmin";
    public const string Cluster = "cluster";
    public const string Action = "act";
    public const string SecurityStamp = "sstamp";
}
