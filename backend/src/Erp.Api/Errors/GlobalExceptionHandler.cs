using Erp.Api.Contracts;
using Erp.Api.Correlation;
using Erp.Shared.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace Erp.Api.Errors;

/// <summary>
/// Last-resort handler for unhandled exceptions. Produces the standard error
/// envelope, never leaks stack traces or secrets to the client, and logs the
/// failure with the request's correlation ID for traceability.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[HttpCorrelationContext.ItemKey] as string ?? string.Empty;

        logger.LogError(
            exception,
            "Unhandled exception processing {Method} {Path}. CorrelationId={CorrelationId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            correlationId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        // In production never leak details; in Development include the message to aid debugging.
        var message = environment.IsDevelopment()
            ? $"{exception.GetType().Name}: {exception.Message}"
            : "An unexpected error occurred. Please contact support with the correlation ID if it persists.";

        var envelope = ApiErrorEnvelope.From(ErrorCodes.Internal, message, correlationId);

        await httpContext.Response.WriteAsJsonAsync(envelope, cancellationToken);
        return true;
    }
}
