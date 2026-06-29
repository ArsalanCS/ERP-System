using Erp.Domain.Common;

namespace Erp.Domain.Assets;

/// <summary>
/// Note detail for an <see cref="Asset"/> of type NOTE (Event/Asset architecture §12).
/// Linked to an event via <c>EventAsset</c>. Carries WorkspaceId (kept equal to the asset's)
/// so the same RLS + query-filter isolation applies directly to note rows.
/// </summary>
public sealed class Note : TenantEntity
{
    private Note() { } // EF

    public Note(Guid workspaceId, Guid assetId, string body, bool isPinned, bool isInternal)
    {
        AssignWorkspace(workspaceId);
        AssetId = assetId;
        Body = body.Trim();
        IsPinned = isPinned;
        IsInternal = isInternal;
    }

    public Guid AssetId { get; private set; }
    public string Body { get; private set; } = default!;
    public bool IsPinned { get; private set; }
    public bool IsInternal { get; private set; }

    public void Update(string body, bool isPinned, bool isInternal)
    {
        Body = body.Trim();
        IsPinned = isPinned;
        IsInternal = isInternal;
    }
}
