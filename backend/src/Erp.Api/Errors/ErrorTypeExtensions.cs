using Erp.Shared.Errors;

namespace Erp.Api.Errors;

/// <summary>
/// Maps an <see cref="ErrorType"/> to the HTTP status code used by the API
/// (CONVENTIONS.md documented codes). Keeps HTTP concerns at the edge only.
/// </summary>
public static class ErrorTypeExtensions
{
    public static int ToHttpStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest,
    };
}
