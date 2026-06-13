namespace Erp.Shared.Errors;

/// <summary>
/// Canonical machine-readable error codes used in the standard error envelope.
/// Keep these stable — the frontend and API consumers branch on them.
/// (CONVENTIONS.md: consistent error envelope; documented HTTP codes.)
/// </summary>
public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string RateLimited = "RATE_LIMITED";
    public const string Concurrency = "CONCURRENCY_CONFLICT";
    public const string Internal = "INTERNAL_ERROR";
}
