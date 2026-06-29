using Erp.Domain.Common;

namespace Erp.Domain.Events;

/// <summary>
/// Task-specific detail for an <see cref="Event"/> of type TASK_MANAGEMENT
/// (Event/Asset architecture §7). One row per task event (<see cref="EventId"/> unique).
/// Priority is a <c>Status</c> under the TASK_PRIORITY status type; workflow status is
/// tracked separately in <c>EventStatus</c>. Notes/documents/relations are linked via
/// <c>EventAsset</c>, not stored here.
/// </summary>
public sealed class TaskEvent : TenantEntity
{
    private TaskEvent() { } // EF

    public TaskEvent(long workspaceId, long eventId, string referenceNo, string title, long? reporterId)
    {
        AssignWorkspace(workspaceId);
        EventId = eventId;
        ReferenceNo = referenceNo;
        Title = title.Trim();
        ReporterId = reporterId;
        CompletionPercent = 0;
    }

    public long EventId { get; private set; }
    /// <summary>Human-readable per-workspace number (e.g. TSK-00001).</summary>
    public string ReferenceNo { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    public long? AssigneeId { get; private set; }
    public long? ReporterId { get; private set; }
    /// <summary>Parent task's <c>event_id</c> for subtasks.</summary>
    public long? ParentEventId { get; private set; }
    /// <summary>FK to a Status whose StatusType code is TASK_PRIORITY.</summary>
    public long? PriorityStatusId { get; private set; }

    public DateTimeOffset? StartAt { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public decimal? EstimatedTime { get; private set; }
    public decimal? ActualTime { get; private set; }
    public int CompletionPercent { get; private set; }

    public void UpdateDetails(string title, string? description)
    {
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetSchedule(DateTimeOffset? start, DateTimeOffset? due, decimal? estimatedTime)
    {
        StartAt = start;
        DueAt = due;
        EstimatedTime = estimatedTime;
    }

    public void SetCompletion(int percent, decimal? actualTime)
    {
        CompletionPercent = Math.Clamp(percent, 0, 100);
        ActualTime = actualTime;
    }

    public void Assign(long? assigneeId) => AssigneeId = assigneeId;
    public void SetPriority(long? priorityStatusId) => PriorityStatusId = priorityStatusId;
    public void PlaceUnderParent(long? parentEventId) => ParentEventId = parentEventId;
}
