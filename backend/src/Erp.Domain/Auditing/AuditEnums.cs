namespace Erp.Domain.Auditing;

/// <summary>Outcome recorded on an audit entry (Identity spec §8.2).</summary>
public enum AuditResult
{
    Success = 0,
    Denied = 1,
    Failed = 2,
}

/// <summary>Origin of the audited action (Identity spec §8.2).</summary>
public enum AuditSource
{
    Ui = 0,
    Api = 1,
    BackgroundJob = 2,
    Integration = 3,
}
