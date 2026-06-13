namespace Erp.Shared.Errors;

/// <summary>
/// A field-level validation detail, surfaced in the error envelope's
/// <c>details</c> array (e.g. { field: "email", message: "..." }).
/// </summary>
public sealed record ErrorDetail(string Field, string Message);

/// <summary>
/// An application error: a stable <see cref="Code"/>, a human-readable
/// <see cref="Message"/>, an <see cref="ErrorType"/> the API layer maps to an
/// HTTP status, and optional field-level <see cref="Details"/>.
/// Domain/Application code returns these via <see cref="Result"/> rather than
/// throwing for expected failures.
/// </summary>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type = ErrorType.Failure,
    IReadOnlyList<ErrorDetail>? Details = null)
{
    public static Error Validation(string message, IReadOnlyList<ErrorDetail>? details = null)
        => new(ErrorCodes.Validation, message, ErrorType.Validation, details);

    public static Error NotFound(string message)
        => new(ErrorCodes.NotFound, message, ErrorType.NotFound);

    public static Error Conflict(string message)
        => new(ErrorCodes.Conflict, message, ErrorType.Conflict);

    public static Error Unauthorized(string message)
        => new(ErrorCodes.Unauthorized, message, ErrorType.Unauthorized);

    public static Error Forbidden(string message)
        => new(ErrorCodes.Forbidden, message, ErrorType.Forbidden);

    public static Error Concurrency(string message)
        => new(ErrorCodes.Concurrency, message, ErrorType.Conflict);
}

/// <summary>
/// Categorizes an <see cref="Error"/> so the API edge can translate it to the
/// correct HTTP status without the domain/application layers knowing about HTTP.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
}
