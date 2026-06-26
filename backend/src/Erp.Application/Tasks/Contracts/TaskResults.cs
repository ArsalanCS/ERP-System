namespace Erp.Application.Tasks.Contracts;

/// <summary>Small command result (Refactor Guide §5.6): the new id + generated number.</summary>
public sealed record CreateTaskResult(Guid Id, string TaskNumber);
