using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// A document linked to a task (Task Model spec §5 Documents tab). A supporting
/// record, not an event. Reference-based for now (name + type + URL/link); binary
/// upload arrives when shared file storage (S3) exists.
/// </summary>
public sealed class TaskDocument : TenantEntity
{
    private TaskDocument() { } // EF

    public TaskDocument(Guid workspaceId, Guid taskId, string fileName, string? fileType, string? url, string? note, Guid? uploadedBy)
    {
        AssignWorkspace(workspaceId);
        TaskId = taskId;
        FileName = fileName.Trim();
        FileType = string.IsNullOrWhiteSpace(fileType) ? null : fileType.Trim();
        Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UploadedBy = uploadedBy;
    }

    public Guid TaskId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string? FileType { get; private set; }
    /// <summary>External link/reference to the document (until binary storage exists).</summary>
    public string? Url { get; private set; }
    public string? Note { get; private set; }
    public Guid? UploadedBy { get; private set; }
}
