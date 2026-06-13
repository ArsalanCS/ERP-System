using Erp.Api.Contracts;
using Erp.Api.Correlation;
using Erp.Api.Errors;
using Erp.Application.Abstractions;
using Erp.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Erp.Api.Security;

/// <summary>
/// Declares the permission an endpoint requires (CONVENTIONS.md: every endpoint
/// declares its required permission). Enforced against the caller's effective
/// actions (resolved server-side into the JWT). Returns 401 when unauthenticated,
/// 403 (deny-wins already applied) when the action is missing — both as the
/// standard error envelope.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string action) : Attribute, IAuthorizationFilter
{
    public string Action { get; } = action;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        var correlationId = context.HttpContext.Items[HttpCorrelationContext.ItemKey] as string ?? string.Empty;

        if (!currentUser.IsAuthenticated)
        {
            context.Result = Envelope(Error.Unauthorized("Authentication is required."), correlationId);
            return;
        }

        // Platform admins bypass per-action checks; otherwise require the action.
        if (!currentUser.IsPlatformAdmin && !currentUser.Can(Action))
        {
            context.Result = Envelope(
                Error.Forbidden($"You do not have permission to perform this action ({Action})."),
                correlationId);
        }
    }

    private static ObjectResult Envelope(Error error, string correlationId) => new(
        ApiErrorEnvelope.From(error.Code, error.Message, correlationId))
    {
        StatusCode = error.Type.ToHttpStatusCode(),
    };
}
