using Erp.Domain.Common;

namespace Erp.Domain.Tenancy;

/// <summary>
/// Top-level customer tenant (Identity spec §6.4). The root every other
/// tenant-owned record hangs off via <c>workspace_id</c>. A workspace is the
/// tenant registry entry itself, so it is not <see cref="ITenantOwned"/>.
/// </summary>
public sealed class Workspace : Entity, ISoftDeletable
{
    private Workspace() { } // EF

    public Workspace(string name, string slug, string defaultLanguage, string timeZone, string baseCurrency)
    {
        Name = name;
        Slug = slug;
        DefaultLanguage = defaultLanguage;
        TimeZone = timeZone;
        BaseCurrency = baseCurrency;
        Status = WorkspaceStatus.Trial;
    }

    /// <summary>Display name of the workspace.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>URL-safe unique identifier used to resolve the workspace at login.</summary>
    public string Slug { get; private set; } = null!;

    public string? LegalName { get; private set; }
    public string DefaultLanguage { get; private set; } = "en";
    public string TimeZone { get; private set; } = "Asia/Riyadh";
    public string BaseCurrency { get; private set; } = "SAR";
    public string? Country { get; private set; }

    public WorkspaceStatus Status { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public bool AllowsLogin => Status is WorkspaceStatus.Trial or WorkspaceStatus.Active && !IsDeleted;

    public void Activate() => Status = WorkspaceStatus.Active;
    public void Suspend() => Status = WorkspaceStatus.Suspended;

    public void UpdateProfile(string name, string? legalName, string defaultLanguage, string timeZone, string baseCurrency, string? country)
    {
        Name = name;
        LegalName = legalName;
        DefaultLanguage = defaultLanguage;
        TimeZone = timeZone;
        BaseCurrency = baseCurrency;
        Country = country;
    }

    public void Archive(Guid? by, DateTimeOffset when)
    {
        Status = WorkspaceStatus.Archived;
        IsDeleted = true;
        DeletedAt = when;
        DeletedBy = by;
    }
}
