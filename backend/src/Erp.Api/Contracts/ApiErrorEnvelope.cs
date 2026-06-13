using Erp.Shared.Errors;

namespace Erp.Api.Contracts;

/// <summary>
/// The standard error response envelope. Identical shape for every 4xx/5xx
/// (CONVENTIONS.md "Standard error envelope").
/// </summary>
public sealed record ApiErrorEnvelope(ApiError Error)
{
    public static ApiErrorEnvelope From(string code, string message, string correlationId, IReadOnlyList<ErrorDetail>? details = null)
        => new(new ApiError(
            code,
            message,
            correlationId,
            details?.Select(d => new ApiErrorDetail(d.Field, d.Message)).ToArray()));
}

public sealed record ApiError(
    string Code,
    string Message,
    string CorrelationId,
    IReadOnlyList<ApiErrorDetail>? Details);

public sealed record ApiErrorDetail(string Field, string Message);
