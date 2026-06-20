using Erp.Shared.Errors;

namespace Erp.Application.Users;

public static class UserErrors
{
    public static Error NotFound() => Error.NotFound("User not found.");

    public static Error EmailTaken() =>
        new("USER_EMAIL_TAKEN", "A user with this email already exists in this workspace.", ErrorType.Conflict);

    public static Error UnknownRole() =>
        new("USER_UNKNOWN_ROLE", "One or more roles do not exist in this workspace.", ErrorType.Validation);

    public static Error UnknownPlacementNode() =>
        new("USER_UNKNOWN_PLACEMENT", "The selected structure node does not exist in this workspace.", ErrorType.Validation);

    public static Error LastWorkspaceOwner() =>
        new("USER_LAST_OWNER", "Cannot remove the last active Workspace Owner.", ErrorType.Conflict);

    public static Error NoWorkspaceScope() =>
        new("USER_NO_WORKSPACE", "No workspace scope on the request.", ErrorType.Forbidden);
}
