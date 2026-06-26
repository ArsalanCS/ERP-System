using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// A note linked to a task (Task Model spec §5 Notes tab). A supporting record,
/// not an event. Can be pinned and marked internal-only.
/// </summary>
public sealed class TaskNote : TenantEntity
{
    private TaskNote() { } // EF

    public TaskNote(Guid workspaceId, Guid taskId, string body, Guid? authorId)
    {
        AssignWorkspace(workspaceId);
        TaskId = taskId;
        Body = body.Trim();
        AuthorId = authorId;
        IsInternal = true;
    }

    public Guid TaskId { get; private set; }
    public string Body { get; private set; } = default!;
    public bool IsPinned { get; private set; }
    public bool IsInternal { get; private set; }
    public Guid? AuthorId { get; private set; }

    public void Update(string body, bool isPinned, bool isInternal)
    {
        Body = body.Trim();
        IsPinned = isPinned;
        IsInternal = isInternal;
    }
}
