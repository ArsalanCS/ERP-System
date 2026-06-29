using Erp.Shared.Errors;

namespace Erp.Application.Tasks;

internal static class TaskErrors
{
    public static Error NotFound(string what) => Error.NotFound($"{what} not found.");
    public static Error NoScope() => new("TASK_NO_WORKSPACE", "No workspace scope on the request.", ErrorType.Forbidden);
    public static Error NoStatuses() => new("TASK_NO_STATUSES", "Configure task statuses before adding tasks.", ErrorType.Validation);
    public static Error NoInitialStatus() => new("TASK_NO_INITIAL_STATUS", "The task workflow has no starting status.", ErrorType.Validation);
    public static Error StatusInvalid() => new("TASK_STATUS_INVALID", "That status is not a task workflow status in this workspace.", ErrorType.Validation);
    public static Error PriorityInvalid() => new("TASK_PRIORITY_INVALID", "That priority is not a task priority in this workspace.", ErrorType.Validation);
    public static Error Closed() => new("TASK_CLOSED", "This task is closed. Reopen it before editing.", ErrorType.Conflict);
    public static Error SelfDependency() => new("TASK_SELF_DEPENDENCY", "A task cannot depend on itself.", ErrorType.Validation);
    public static Error DuplicateLink() => new("TASK_DUPLICATE_LINK", "That link already exists.", ErrorType.Conflict);
    public static Error DuplicateReport() => new("TASK_DUPLICATE_REPORT", "You already filed a report for that day. Edit the existing one.", ErrorType.Conflict);
    public static Error StatusTypeNotFound() => new("TASK_STATUS_TYPE_NOT_FOUND", "That status type does not exist in this workspace.", ErrorType.Validation);
    public static Error StatusInUse() => new("TASK_STATUS_IN_USE", "This status is in use and cannot be deleted. Deactivate it instead.", ErrorType.Conflict);
    public static Error StatusIsInitial() => new("TASK_STATUS_IS_INITIAL", "Set another status as the starting status before removing this one.", ErrorType.Conflict);
    public static Error ReportTimeRequired(string which) => new("TASK_REPORT_TIME_REQUIRED", $"This workspace requires {which} time on daily reports.", ErrorType.Validation);
}
