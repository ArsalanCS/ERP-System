using Erp.Application.Abstractions;

namespace Erp.Api.Middleware;

/// <summary>
/// Copies the authenticated caller's scope (resolved from the JWT by
/// <see cref="Security.CurrentUser"/>) into the request's <see cref="ITenantContext"/>,
/// which drives the EF query filter and the RLS session variables. Runs after
/// authentication. Unauthenticated requests get no scope.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        if (currentUser.IsAuthenticated)
        {
            if (currentUser.IsPlatformAdmin)
            {
                tenantContext.SetScope(currentUser.WorkspaceId, currentUser.ClusterIds, isPlatformAdmin: true);
            }
            else if (currentUser.WorkspaceId is { } workspaceId)
            {
                tenantContext.SetScope(workspaceId, currentUser.ClusterIds);
            }
        }

        await next(context);
    }
}
