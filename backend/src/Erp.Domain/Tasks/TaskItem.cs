using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// The operational work item — the only Event in this phase (Task Model spec §1-2).
/// A subtask is also a <see cref="TaskItem"/> linked via <see cref="ParentTaskId"/>
/// (parent/child added in a later slice; the column is present now to avoid a
/// migration later). Source/relations to business records are deferred until those
/// modules exist. Named <c>TaskItem</c> to avoid clashing with <c>System.Threading.Tasks.Task</c>.
/// </summary>
public sealed class TaskItem : TenantEntity
{
    private TaskItem() { } // EF

    public TaskItem(
        Guid workspaceId,
        string taskNumber,
        string title,
        Guid statusTypeId,
        Guid statusId,
        Guid? reporterId)
    {
        AssignWorkspace(workspaceId);
        TaskNumber = taskNumber;
        Title = title.Trim();
        StatusTypeId = statusTypeId;
        StatusId = statusId;
        ReporterId = reporterId;
        EventType = TaskEventType.Task;
        Priority = TaskPriority.Normal;
    }

    /// <summary>Human-readable per-workspace number (e.g. TSK-000123).</summary>
    public string TaskNumber { get; private set; } = default!;
    public TaskEventType EventType { get; private set; }

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    public Guid StatusTypeId { get; private set; }
    public Guid StatusId { get; private set; }
    public TaskPriority Priority { get; private set; }

    public Guid? AssigneeId { get; private set; }
    public Guid? ReporterId { get; private set; }
    public Guid? ParentTaskId { get; private set; }

    /// <summary>The business record that originated the task (Task Model spec §3). Manual = null.</summary>
    public string? SourceType { get; private set; }
    public Guid? SourceId { get; private set; }

    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public decimal? EstimatedHours { get; private set; }
    public decimal? ActualHours { get; private set; }
    public DateTimeOffset? ReminderAt { get; private set; }

    /// <summary>Manual completion percentage, 0-100.</summary>
    public int CompletionPercent { get; private set; }

    public void UpdateDetails(string title, string? description, TaskPriority priority)
    {
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
    }

    public void SetSchedule(DateTimeOffset? start, DateTimeOffset? due, decimal? estimatedHours, DateTimeOffset? reminderAt)
    {
        StartDate = start;
        DueDate = due;
        EstimatedHours = estimatedHours;
        ReminderAt = reminderAt;
    }

    public void SetCompletion(int percent, decimal? actualHours)
    {
        CompletionPercent = Math.Clamp(percent, 0, 100);
        ActualHours = actualHours;
    }

    public void Assign(Guid? assigneeId) => AssigneeId = assigneeId;

    public void SetSource(string? sourceType, Guid? sourceId)
    {
        SourceType = string.IsNullOrWhiteSpace(sourceType) ? null : sourceType.Trim();
        SourceId = sourceId;
    }

    /// <summary>Moves the task to a status (validated against its workflow in the service).</summary>
    public void ChangeStatus(Guid statusId) => StatusId = statusId;

    public void PlaceUnderParent(Guid? parentTaskId) => ParentTaskId = parentTaskId;
}
