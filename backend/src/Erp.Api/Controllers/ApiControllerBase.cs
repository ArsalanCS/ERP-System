using Erp.Api.Contracts;
using Erp.Api.Correlation;
using Erp.Api.Errors;
using Erp.Shared.Errors;
using Erp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Base controller that maps <see cref="Result"/>/<see cref="Error"/> and
/// validation failures to the standard error envelope with the request's
/// correlation ID (CONVENTIONS.md).
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected string CorrelationId =>
        HttpContext.Items[HttpCorrelationContext.ItemKey] as string ?? string.Empty;

    protected string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>Validates a request; returns null when valid, otherwise a 422 result.</summary>
    protected async Task<IActionResult?> ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken ct)
    {
        ValidationResult result = await validator.ValidateAsync(instance, ct);
        if (result.IsValid)
        {
            return null;
        }

        var details = result.Errors
            .Select(e => new ErrorDetail(ToCamelCase(e.PropertyName), e.ErrorMessage))
            .ToList();

        return Envelope(Error.Validation("One or more validation errors occurred.", details));
    }

    /// <summary>Converts an <see cref="Error"/> to its envelope + HTTP status.</summary>
    protected IActionResult Envelope(Error error)
    {
        var envelope = ApiErrorEnvelope.From(error.Code, error.Message, CorrelationId, error.Details);
        return StatusCode(error.Type.ToHttpStatusCode(), envelope);
    }

    /// <summary>Maps a typed result: <paramref name="onSuccess"/> projection or the error envelope.</summary>
    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
        => result.IsSuccess ? onSuccess(result.Value) : Envelope(result.Error!);

    protected IActionResult FromResult(Result result, Func<IActionResult> onSuccess)
        => result.IsSuccess ? onSuccess() : Envelope(result.Error!);

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) || char.IsLower(name[0])
            ? name
            : char.ToLowerInvariant(name[0]) + name[1..];
}
