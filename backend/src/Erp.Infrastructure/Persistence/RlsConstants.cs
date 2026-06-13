namespace Erp.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL session-variable (GUC) names that RLS policies read. Set per
/// connection by <see cref="Interceptors.RlsConnectionInterceptor"/> and
/// referenced in the RLS policy SQL (kept in one place so both stay in sync).
/// </summary>
public static class RlsConstants
{
    public const string WorkspaceSetting = "app.current_workspace_id";
    public const string PlatformAdminSetting = "app.is_platform_admin";
}
