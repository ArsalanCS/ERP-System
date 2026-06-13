namespace Erp.Application.Abstractions;

/// <summary>
/// The authenticated caller's resolved security context for the current request.
/// Scope (workspace, clusters, actions) is derived from the JWT + membership on
/// the server — never from client-supplied values (CLAUDE.md §4.1).
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The user account id, or null when unauthenticated.</summary>
    Guid? UserId { get; }

    /// <summary>The workspace the request is scoped to, or null when unauthenticated.</summary>
    Guid? WorkspaceId { get; }

    string? Email { get; }

    /// <summary>True for the platform-level super admin (cross-tenant).</summary>
    bool IsPlatformAdmin { get; }

    /// <summary>Clusters the caller may act within, in the active workspace.</summary>
    IReadOnlySet<Guid> ClusterIds { get; }

    /// <summary>Effective allowed actions (e.g. "user.manage"). Deny-wins is pre-applied.</summary>
    IReadOnlySet<string> Actions { get; }

    bool Can(string action);
}
