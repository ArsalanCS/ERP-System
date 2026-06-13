namespace Erp.Api.Security;

public static class RateLimitPolicies
{
    /// <summary>Tight limit for authentication endpoints (login, refresh, reset, …).</summary>
    public const string Auth = "auth";
}
