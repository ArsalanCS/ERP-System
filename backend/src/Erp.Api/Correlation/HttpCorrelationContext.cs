using Erp.Shared.Correlation;

namespace Erp.Api.Correlation;

/// <summary>
/// Per-request <see cref="ICorrelationContext"/> backed by HttpContext.Items.
/// Set by <see cref="Middleware.CorrelationIdMiddleware"/> early in the pipeline.
/// </summary>
public sealed class HttpCorrelationContext(IHttpContextAccessor accessor) : ICorrelationContext
{
    internal const string ItemKey = "ErpCorrelationId";

    public string CorrelationId =>
        accessor.HttpContext?.Items[ItemKey] as string ?? string.Empty;
}
