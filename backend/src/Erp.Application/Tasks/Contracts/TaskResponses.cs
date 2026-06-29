using Erp.Domain.Events;

namespace Erp.Application.Tasks.Contracts;

/// <summary>Read models for the UI. The task is identified by its <c>EventId</c>.</summary>
public sealed record TaskListItemDto(
    Guid EventId,
    string ReferenceNo,
    string Title,
    Guid? StatusId,
    string? StatusName,
    string? StatusColor,
    bool StatusIsClosed,
    Guid? PriorityStatusId,
    string? PriorityName,
    string? PriorityColor,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTimeOffset? DueAt,
    bool IsOverdue,
    int CompletionPercent,
    DateTimeOffset CreatedAt);

public sealed record TaskDetailsDto(
    Guid EventId,
    string ReferenceNo,
    string Title,
    string? Description,
    Guid? StatusId,
    string? StatusName,
    string? StatusColor,
    bool StatusIsClosed,
    Guid? PriorityStatusId,
    string? PriorityName,
    string? PriorityColor,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid? ReporterId,
    string? ReporterName,
    Guid? ParentEventId,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    decimal? EstimatedTime,
    decimal? ActualTime,
    int CompletionPercent,
    bool IsOverdue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record TaskActivityDto(
    Guid Id,
    EventActivityKind Kind,
    string Message,
    Guid? ActorId,
    string? ActorName,
    DateTimeOffset OccurredAt);

public sealed record TaskNoteDto(
    Guid Id,
    string Body,
    bool IsPinned,
    bool IsInternal,
    Guid? AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt);

public sealed record TaskDocumentDto(
    Guid Id,
    string FileName,
    string FilePath,
    string? MimeType,
    Guid? UploadedById,
    string? UploadedByName,
    DateTimeOffset CreatedAt);

public sealed record TaskDependencyDto(
    Guid Id,
    Guid DependsOnEventId,
    string DependsOnReferenceNo,
    string DependsOnTitle,
    bool IsBlocking);

public sealed record TaskDailyReportDto(
    Guid Id,
    DateOnly ReportDate,
    string Description,
    decimal? EstimatedTime,
    decimal? ActualTime,
    decimal? RemainingTime,
    Guid? StatusId,
    string? StatusName,
    string? StatusColor,
    Guid? AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt);

/// <summary>A daily-report row for the workspace-wide reports view (carries its task identity).</summary>
public sealed record TaskDailyReportRowDto(
    Guid Id,
    Guid EventId,
    string ReferenceNo,
    string TaskTitle,
    DateOnly ReportDate,
    string Description,
    decimal? EstimatedTime,
    decimal? ActualTime,
    decimal? RemainingTime,
    Guid? StatusId,
    string? StatusName,
    string? StatusColor,
    Guid? AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt);

public sealed record TaskAuditDto(
    Guid Id,
    string Action,
    DateTimeOffset CreatedAt,
    Guid? ActorUserId,
    string? ActorName);

/// <summary>A status value (for status/priority dropdowns and settings).</summary>
public sealed record StatusDto(
    Guid Id,
    string Code,
    string Name,
    string? Color,
    int SortOrder,
    bool IsInitial,
    bool IsClosed,
    bool IsActive);

public sealed record MyTasksGroups(
    IReadOnlyList<TaskListItemDto> Overdue,
    IReadOnlyList<TaskListItemDto> Today,
    IReadOnlyList<TaskListItemDto> Upcoming,
    IReadOnlyList<TaskListItemDto> Waiting);
