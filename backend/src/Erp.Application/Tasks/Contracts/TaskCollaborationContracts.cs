using Erp.Domain.Tasks;

namespace Erp.Application.Tasks.Contracts;

// ---- Checklist ----
public sealed record ChecklistItemDto(Guid Id, string Text, bool IsDone, int SortOrder);
public sealed record CreateChecklistItemRequest(string Text);
public sealed record UpdateChecklistItemRequest(string Text, bool IsDone, int SortOrder);

// ---- Dependencies ----
public sealed record TaskDependencyDto(
    Guid Id, Guid DependsOnTaskId, string DependsOnNumber, string DependsOnTitle,
    TaskDependencyType DependencyType, bool IsBlocking);
public sealed record CreateDependencyRequest(Guid DependsOnTaskId, TaskDependencyType DependencyType, bool IsBlocking);

// ---- Relations ----
public sealed record TaskRelationDto(Guid Id, string RelatedEntityType, Guid RelatedEntityId, TaskRelationRole Role, string? Reason);
public sealed record CreateRelationRequest(string RelatedEntityType, Guid RelatedEntityId, TaskRelationRole Role, string? Reason);

// ---- Notes ----
public sealed record TaskNoteDto(
    Guid Id, string Body, bool IsPinned, bool IsInternal, Guid? AuthorId, string? AuthorName,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
public sealed record CreateNoteRequest(string Body, bool IsPinned, bool IsInternal);
public sealed record UpdateNoteRequest(string Body, bool IsPinned, bool IsInternal);

// ---- Documents (reference-based) ----
public sealed record TaskDocumentDto(
    Guid Id, string FileName, string? FileType, string? Url, string? Note,
    Guid? UploadedBy, string? UploadedByName, DateTimeOffset CreatedAt);
public sealed record CreateDocumentRequest(string FileName, string? FileType, string? Url, string? Note);

// ---- Audit (protected compliance history for a task) ----
public sealed record TaskAuditDto(Guid Id, string Action, string? ActorName, DateTimeOffset OccurredAt, string? Reason);

// ---- My Tasks (grouped dashboard) ----
public sealed record MyTasksGroups(
    IReadOnlyList<TaskListItemDto> Overdue,
    IReadOnlyList<TaskListItemDto> Today,
    IReadOnlyList<TaskListItemDto> Upcoming,
    IReadOnlyList<TaskListItemDto> Waiting);
