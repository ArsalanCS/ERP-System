using Erp.Domain.Common;

namespace Erp.Domain.Events;

/// <summary>
/// A daily progress report against an event/task (Event/Asset architecture §16). One row per author
/// per day per event: what was done, estimated/actual/remaining time, and an optional status the user
/// selected at report time. If that status differs from the task's current status, the service also
/// inserts a new <c>event_statuses</c> row (status change driven by the report).
/// </summary>
public sealed class EventDailyReport : TenantEntity
{
    private EventDailyReport() { } // EF

    public EventDailyReport(
        long workspaceId, long eventId, long? userId, DateOnly reportDate, string description,
        decimal? estimatedTime, decimal? actualTime, decimal? remainingTime, long? statusId)
    {
        AssignWorkspace(workspaceId);
        EventId = eventId;
        UserId = userId;
        ReportDate = reportDate;
        Description = description.Trim();
        EstimatedTime = estimatedTime;
        ActualTime = actualTime;
        RemainingTime = remainingTime;
        StatusId = statusId;
    }

    public long EventId { get; private set; }
    /// <summary>The user who wrote the report.</summary>
    public long? UserId { get; private set; }
    public DateOnly ReportDate { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal? EstimatedTime { get; private set; }
    public decimal? ActualTime { get; private set; }
    public decimal? RemainingTime { get; private set; }
    /// <summary>Status selected at report time (a Status under TASK_STATUS), or null.</summary>
    public long? StatusId { get; private set; }

    public void Update(DateOnly reportDate, string description, decimal? estimatedTime,
        decimal? actualTime, decimal? remainingTime, long? statusId)
    {
        ReportDate = reportDate;
        Description = description.Trim();
        EstimatedTime = estimatedTime;
        ActualTime = actualTime;
        RemainingTime = remainingTime;
        StatusId = statusId;
    }
}
