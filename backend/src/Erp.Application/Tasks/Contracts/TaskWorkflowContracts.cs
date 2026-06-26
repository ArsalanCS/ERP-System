using Erp.Domain.Tasks;

namespace Erp.Application.Tasks.Contracts;

// ---- Responses ----
public sealed record TaskStatusDto(
    Guid Id,
    Guid StatusTypeId,
    string Name,
    TaskStatusCategory Category,
    string? Color,
    int SortOrder,
    bool IsInitial,
    bool IsFinal);

public sealed record TaskStatusTypeDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsActive,
    int SortOrder);

/// <summary>A status type together with its ordered statuses (admin screen + task create picker).</summary>
public sealed record TaskWorkflowDto(TaskStatusTypeDto Type, IReadOnlyList<TaskStatusDto> Statuses);

// ---- Requests ----
public sealed record CreateStatusTypeRequest(string Name, string? Description);

public sealed record UpdateStatusTypeRequest(string Name, string? Description, int SortOrder, bool IsActive, bool IsDefault);

public sealed record CreateStatusRequest(
    Guid StatusTypeId,
    string Name,
    TaskStatusCategory Category,
    string? Color,
    bool IsInitial,
    bool IsFinal);

public sealed record UpdateStatusRequest(
    string Name,
    TaskStatusCategory Category,
    string? Color,
    int SortOrder,
    bool IsInitial,
    bool IsFinal);
