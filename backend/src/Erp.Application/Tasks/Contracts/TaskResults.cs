namespace Erp.Application.Tasks.Contracts;

/// <summary>Returned on task creation. The event id is the task's stable identifier.</summary>
public sealed record CreateTaskResult(Guid EventId, string ReferenceNo);
