namespace Erp.Shared.Correlation;

/// <summary>
/// Correlation-ID constants. A correlation ID rides on every request and is
/// propagated into jobs and audit logs for end-to-end traceability
/// (CLAUDE.md §5, CONVENTIONS.md).
/// </summary>
public static class CorrelationConstants
{
    /// <summary>Inbound/outbound HTTP header carrying the correlation ID.</summary>
    public const string HeaderName = "X-Correlation-ID";
}

/// <summary>
/// Abstraction for reading the current request's correlation ID from any layer
/// without taking a dependency on HTTP. Implemented in the API/Infrastructure layer.
/// </summary>
public interface ICorrelationContext
{
    string CorrelationId { get; }
}
