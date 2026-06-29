namespace Erp.Domain.Mailing;

/// <summary>Lifecycle of an outbox message (Mail doc §8: PENDING/PROCESSING/SENT/FAILED/CANCELLED).</summary>
public enum SendStatus
{
    /// <summary>Queued and eligible to be picked up by the dispatcher.</summary>
    Pending = 0,
    /// <summary>Claimed by the dispatcher for the current attempt.</summary>
    Processing = 1,
    /// <summary>Delivered to the mail transport.</summary>
    Sent = 2,
    /// <summary>All attempts exhausted; will not retry without manual requeue.</summary>
    Failed = 3,
    /// <summary>Cancelled before delivery.</summary>
    Cancelled = 4,
}

/// <summary>Recipient role on a message.</summary>
public enum MailRecipientKind
{
    To = 0,
    Cc = 1,
    Bcc = 2,
}

/// <summary>
/// Built-in template codes the Task module triggers on. A workspace may customise
/// the subject/body of each; if none exists, the outbox falls back to a built-in body.
/// </summary>
public static class MailTemplateCodes
{
    public const string TaskCreated = "TASK_CREATED";
    public const string TaskAssigned = "TASK_ASSIGNED";
    public const string TaskOpened = "TASK_OPENED";
    public const string TaskStatusChanged = "TASK_STATUS_CHANGED";
    public const string TaskCompleted = "TASK_COMPLETED";
    public const string DailyReportSubmitted = "DAILY_REPORT_SUBMITTED";
    public const string DailyReportStatusChanged = "DAILY_REPORT_STATUS_CHANGED";

    public static readonly IReadOnlyList<string> All =
        [TaskCreated, TaskAssigned, TaskOpened, TaskStatusChanged, TaskCompleted, DailyReportSubmitted, DailyReportStatusChanged];
}
