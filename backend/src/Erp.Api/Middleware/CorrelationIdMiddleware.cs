using Erp.Api.Correlation;
using Erp.Shared.Correlation;

namespace Erp.Api.Middleware;

/// <summary>
/// Assigns a correlation ID to every request (honoring an inbound
/// <c>X-Correlation-ID</c> header if present, otherwise generating one),
/// stashes it for <see cref="ICorrelationContext"/>, echoes it on the response,
/// and pushes it into the log scope so all logs for the request carry it.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[HttpCorrelationContext.ItemKey] = correlationId;

        // Echo on the response before the body is written.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationConstants.HeaderName, out var header))
        {
            var value = header.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return Guid.NewGuid().ToString("n");
    }
}
