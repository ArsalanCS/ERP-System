using Erp.Domain.Common;

namespace Erp.Domain.Assets;

/// <summary>
/// Bridge linking an <see cref="Event"/> to an <see cref="Asset"/> (Event/Asset architecture §11).
/// <see cref="RelationType"/> describes the link (NOTE, DOCUMENT, CUSTOMER, EVIDENCE, …).
/// Event, asset, and this row must all belong to the same workspace.
/// </summary>
public sealed class EventAsset : TenantEntity
{
    private EventAsset() { } // EF

    public EventAsset(long workspaceId, long eventId, long assetId, string relationType, string? description)
    {
        AssignWorkspace(workspaceId);
        EventId = eventId;
        AssetId = assetId;
        RelationType = relationType.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public long EventId { get; private set; }
    public long AssetId { get; private set; }
    public string RelationType { get; private set; } = default!;
    public string? Description { get; private set; }
}

/// <summary>Stable event↔asset relation-type codes.</summary>
public static class EventAssetRelationTypes
{
    public const string Note = "NOTE";
    public const string Document = "DOCUMENT";
    public const string Customer = "CUSTOMER";
    public const string Vehicle = "VEHICLE";
    public const string Invoice = "INVOICE";
    public const string Evidence = "EVIDENCE";
    public const string RelatedAsset = "RELATED_ASSET";
}
