using Erp.Shared.Errors;

namespace Erp.Application.Tasks;

internal static class TaskErrors
{
    public static Error NotFound(string what) => Error.NotFound($"{what} not found.");
    public static Error NoScope() => new("TASK_NO_WORKSPACE", "No workspace scope on the request.", ErrorType.Forbidden);
    public static Error NoWorkflow() => new("TASK_NO_WORKFLOW", "Create a task status workflow before adding tasks.", ErrorType.Validation);
    public static Error NoInitialStatus() => new("TASK_NO_INITIAL_STATUS", "The chosen workflow has no starting status.", ErrorType.Validation);
    public static Error StatusNotInWorkflow() => new("TASK_STATUS_MISMATCH", "That status does not belong to this task's workflow.", ErrorType.Validation);
    public static Error NameTaken() => new("TASK_NAME_TAKEN", "That name is already used in this workspace.", ErrorType.Conflict);
    public static Error StatusInUse() => new("TASK_STATUS_IN_USE", "Tasks are using this status. Move them first.", ErrorType.Conflict);
    public static Error WorkflowInUse() => new("TASK_WORKFLOW_IN_USE", "Tasks are using this workflow. Move them first.", ErrorType.Conflict);
    public static Error Closed() => new("TASK_CLOSED", "This task is closed (Done/Cancelled/Rejected). Reopen it before editing.", ErrorType.Conflict);
    public static Error SelfDependency() => new("TASK_SELF_DEPENDENCY", "A task cannot depend on itself.", ErrorType.Validation);
    public static Error DuplicateLink() => new("TASK_DUPLICATE_LINK", "That link already exists.", ErrorType.Conflict);
}
