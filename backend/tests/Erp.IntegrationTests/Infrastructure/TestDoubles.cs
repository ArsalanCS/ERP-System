using Erp.Application.Abstractions;

namespace Erp.IntegrationTests.Infrastructure;

/// <summary>Fixed clock for deterministic tests.</summary>
public sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

/// <summary>Configurable current-user double.</summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public string? Email { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public IReadOnlySet<Guid> ClusterIds { get; set; } = new HashSet<Guid>();
    public IReadOnlySet<string> Actions { get; set; } = new HashSet<string>();
    public bool Can(string action) => Actions.Contains(action);
}
