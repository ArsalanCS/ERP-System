using Erp.Domain.Events;

namespace Erp.Application.Tasks.Contracts;

/// <summary>Read models for the UI. The task is identified by its <c>EventId</c>.</summary>
public sealed record TaskListItemDto(
    long EventId,
    string ReferenceNo,
    string Title,
    long? StatusId,
    string? StatusName,
    string? StatusColor,
    bool StatusIsClosed,
    long? PriorityStatusId,
    string? PriorityName,
    string? PriorityColor,
    long? AssigneeId,
    string? AssigneeName,
    DateTimeOffset? DueAt,
    bool IsOverdue,
    int CompletionPercent,
    DateTimeOffset CreatedAt);

public sealed record TaskDetailsDto(
    long EventId,
    string ReferenceNo,
    string Title,
    string? Description,
    long? StatusId,
    string? StatusName,
    string? StatusColor,
    bool StatusIsClosed,
    long? PriorityStatusId,
    string? PriorityName,
    string? PriorityColor,
    long? AssigneeId,
    string? AssigneeName,
    long? ReporterId,
    string? ReporterName,
    long? ParentEventId,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    decimal? EstimatedTime,
    decimal? ActualTime,
    int CompletionPercent,
    bool IsOverdue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record TaskActivityDto(
    long Id,
    EventActivityKind Kind,
    string Message,
    long? ActorId,
    string? ActorName,
    DateTimeOffset OccurredAt);

public sealed record TaskNoteDto(
    long Id,
    string Body,
    bool IsPinned,
    bool IsInternal,
    long? AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt);

public sealed record TaskDocumentDto(
    long Id,
    string FileName,
    string FilePath,
    string? MimeType,
    long? UploadedById,
    string? UploadedByName,
    DateTimeOffset CreatedAt);

public sealed record TaskDependencyDto(
    long Id,
    long DependsOnEventId,
    string DependsOnReferenceNo,
    string DependsOnTitle,
    bool IsBlocking);

public sealed record TaskDailyReportDto(
    long Id,
    DateOnly ReportDate,
    string Description,
    decimal? EstimatedTime,
    decimal? ActualTime,
    decimal? RemainingTime,
    long? StatusId,
    string? StatusName,
    string? StatusColor,
    long? AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt);

/// <summary>A daily-report row for the workspace-wide reports view (carries its task identity).</summary>
public sealed record TaskDailyReportRowDto(
    long Id,
    long EventId,
    string ReferenceNo,
    string TaskTitle,
    DateOnly ReportDate,
    string Description,
    decimal? EstimatedTime,
    decimal? ActualTime,
    decimal? RemainingTime,
    long? StatusId,
    string? StatusName,
    string? StatusColor,
    long? AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt);

public sealed record TaskAuditDto(
    long Id,
    string Action,
    DateTimeOffset CreatedAt,
    long? ActorUserId,
    string? ActorName);

/// <summary>A status value (for status/priority dropdowns and settings).</summary>
public sealed record StatusDto(
    long Id,
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
