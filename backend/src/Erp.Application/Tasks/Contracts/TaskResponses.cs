using Erp.Domain.Tasks;

namespace Erp.Application.Tasks.Contracts;

/// <summary>Read models for the UI (Refactor Guide §5.5). Display-friendly, no EF navigations.</summary>
public sealed record TaskListItemDto(
    Guid Id,
    string TaskNumber,
    string Title,
    TaskPriority Priority,
    Guid StatusId,
    string StatusName,
    TaskStatusCategory StatusCategory,
    string? StatusColor,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTimeOffset? DueDate,
    bool IsOverdue,
    int CompletionPercent,
    DateTimeOffset CreatedAt);

public sealed record TaskDetailsDto(
    Guid Id,
    string TaskNumber,
    TaskEventType EventType,
    string Title,
    string? Description,
    Guid StatusTypeId,
    Guid StatusId,
    string StatusName,
    TaskStatusCategory StatusCategory,
    string? StatusColor,
    bool StatusIsFinal,
    TaskPriority Priority,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid? ReporterId,
    string? ReporterName,
    Guid? ParentTaskId,
    string? SourceType,
    Guid? SourceId,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    decimal? EstimatedHours,
    decimal? ActualHours,
    DateTimeOffset? ReminderAt,
    int CompletionPercent,
    bool IsOverdue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record TaskActivityDto(
    Guid Id,
    TaskActivityKind Kind,
    string Message,
    Guid? ActorId,
    string? ActorName,
    DateTimeOffset OccurredAt);
