using Erp.Domain.Common;

namespace Erp.Domain.Assets;

/// <summary>
/// Document detail for an <see cref="Asset"/> of type DOCUMENT (Event/Asset architecture §13).
/// Reference-based this phase: <see cref="FilePath"/> holds a URL/path (real binary upload to
/// object storage is a later phase). Linked to an event via <c>EventAsset</c>.
/// </summary>
public sealed class Document : TenantEntity
{
    private Document() { } // EF

    public Document(long workspaceId, long assetId, string fileName, string filePath, string? mimeType, long? fileSize)
    {
        AssignWorkspace(workspaceId);
        AssetId = assetId;
        FileName = fileName.Trim();
        FilePath = filePath.Trim();
        MimeType = string.IsNullOrWhiteSpace(mimeType) ? null : mimeType.Trim();
        FileSize = fileSize;
    }

    public long AssetId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string FilePath { get; private set; } = default!;
    public string? MimeType { get; private set; }
    public long? FileSize { get; private set; }
}
