namespace Erp.Application.Tasks.Contracts;

/// <summary>
/// Command inputs for Task Management. The client never sends workspace id, reference
/// number, or audit fields. A task is an Event(TASK_MANAGEMENT) + a TaskEvent row.
/// </summary>
public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    Guid? AssigneeId,
    Guid? PriorityStatusId,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    decimal? EstimatedTime);

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    decimal? EstimatedTime,
    decimal? ActualTime,
    int CompletionPercent);

public sealed record ChangeStatusRequest(Guid StatusId, string? Note);

public sealed record AssignTaskRequest(Guid? AssigneeId);

public sealed record SetPriorityRequest(Guid? PriorityStatusId);

public sealed record CreateNoteRequest(string Body, bool IsPinned, bool IsInternal);

public sealed record UpdateNoteRequest(string Body, bool IsPinned, bool IsInternal);

/// <summary>Reference-based document (name + URL/path). Real binary upload is a later phase.</summary>
public sealed record CreateDocumentRequest(string FileName, string FilePath, string? MimeType);

public sealed record CreateDependencyRequest(Guid DependsOnEventId, bool IsBlocking);

/// <summary>
/// A daily progress report (architecture §16). Date defaults to today (server) when omitted.
/// If <see cref="StatusId"/> is set and differs from the task's current status, the task status is
/// also changed (a new event_statuses row is inserted).
/// </summary>
public sealed record CreateDailyReportRequest(
    DateOnly? ReportDate,
    string Description,
    decimal? EstimatedTime,
    decimal? ActualTime,
    decimal? RemainingTime,
    Guid? StatusId);

public sealed record UpdateDailyReportRequest(
    DateOnly ReportDate,
    string Description,
    decimal? EstimatedTime,
    decimal? ActualTime,
    decimal? RemainingTime,
    Guid? StatusId);
