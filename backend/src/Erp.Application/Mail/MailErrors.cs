using Erp.Shared.Errors;

namespace Erp.Application.Mail;

internal static class MailErrors
{
    public static Error NotFound(string what) => Error.NotFound($"{what} not found.");
    public static Error NoScope() => new("MAIL_NO_WORKSPACE", "No workspace scope on the request.", ErrorType.Forbidden);
    public static Error CannotRetry() => new("MAIL_CANNOT_RETRY", "Only failed or cancelled messages can be requeued.", ErrorType.Conflict);
}
