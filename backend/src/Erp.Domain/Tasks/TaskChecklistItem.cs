using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// A lightweight checkbox step inside a task (Task Model spec §6). Not an event,
/// not a task — no owner/status/dates. Use a subtask when those are needed.
/// </summary>
public sealed class TaskChecklistItem : TenantEntity
{
    private TaskChecklistItem() { } // EF

    public TaskChecklistItem(Guid workspaceId, Guid taskId, string text, int sortOrder)
    {
        AssignWorkspace(workspaceId);
        TaskId = taskId;
        Text = text.Trim();
        SortOrder = sortOrder;
    }

    public Guid TaskId { get; private set; }
    public string Text { get; private set; } = default!;
    public bool IsDone { get; private set; }
    public int SortOrder { get; private set; }

    public void Update(string text, int sortOrder)
    {
        Text = text.Trim();
        SortOrder = sortOrder;
    }

    public void SetDone(bool done) => IsDone = done;
}
