using Erp.Domain.Common;

namespace Erp.Domain.Assets;

/// <summary>
/// Base record for reusable/supporting objects (Event/Asset architecture §9). Assets are
/// not events. Type-specific detail lives in extension tables (e.g. <see cref="Note"/>,
/// <see cref="Document"/>); assets are linked to events through <c>EventAsset</c>.
/// </summary>
public sealed class Asset : TenantEntity
{
    private Asset() { } // EF

    public Asset(Guid workspaceId, Guid assetTypeId, string? name, string? code)
    {
        AssignWorkspace(workspaceId);
        AssetTypeId = assetTypeId;
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
    }

    public Guid AssetTypeId { get; private set; }
    public string? Name { get; private set; }
    public string? Code { get; private set; }
}
