namespace Erp.Domain.Auditing;

/// <summary>
/// Canonical audit action names (CLAUDE.md §4.3). The set of sensitive actions
/// that must be logged across the platform.
/// </summary>
public static class AuditActions
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
    public const string Approve = "APPROVE";
    public const string Reject = "REJECT";
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string FailedLogin = "FAILED_LOGIN";
    public const string Export = "EXPORT";
    public const string PermissionChange = "PERMISSION_CHANGE";
    public const string PasswordChange = "PASSWORD_CHANGE";
    public const string TwoFactorEnabled = "TWO_FACTOR_ENABLED";
    public const string TwoFactorDisabled = "TWO_FACTOR_DISABLED";
    public const string PeriodClose = "PERIOD_CLOSE";
    public const string ZatcaSubmit = "ZATCA_SUBMIT";
}
