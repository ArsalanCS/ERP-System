using Erp.Domain.Authorization;

namespace Erp.Application.Abstractions;

/// <summary>
/// Resolves a user's effective permissions following the spec §5.5 evaluation
/// order: role permissions → user overrides → explicit-deny-wins.
/// </summary>
public interface IPermissionResolver
{
    Task<EffectivePermissions> ResolveAsync(long userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// The computed result: the set of allowed action codes and the widest data
/// scope granted per action (used later for data-scope filtering).
/// </summary>
public sealed class EffectivePermissions(IReadOnlyDictionary<string, DataScope> scopesByAction)
{
    public IReadOnlyDictionary<string, DataScope> ScopesByAction { get; } = scopesByAction;

    public IReadOnlyCollection<string> Actions => (IReadOnlyCollection<string>)ScopesByAction.Keys;

    public bool Can(string action) => ScopesByAction.ContainsKey(action);

    public DataScope? ScopeFor(string action) =>
        ScopesByAction.TryGetValue(action, out var scope) ? scope : null;

    public static readonly EffectivePermissions None = new(new Dictionary<string, DataScope>());
}
