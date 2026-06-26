using Erp.Domain.Tasks;

namespace Erp.Application.Tasks.Contracts;

/// <summary>
/// Command inputs (Refactor Guide §5.4). Only fields the client may send — no
/// workspace id, audit fields, or task number (generated server-side). Assignment
/// is its own command/permission, so it is not part of the details update.
/// </summary>
public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    Guid? StatusTypeId,
    Guid? AssigneeId,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    decimal? EstimatedHours,
    DateTimeOffset? ReminderAt,
    string? SourceType = null,
    Guid? SourceId = null);

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    decimal? EstimatedHours,
    decimal? ActualHours,
    DateTimeOffset? ReminderAt,
    int CompletionPercent);

public sealed record ChangeTaskStatusRequest(Guid StatusId);

public sealed record AssignTaskRequest(Guid? AssigneeId);
