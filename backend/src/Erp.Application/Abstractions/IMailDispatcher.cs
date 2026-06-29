namespace Erp.Application.Abstractions;

/// <summary>
/// Delivers due outbox messages (send_mails). Invoked on a timer by the background worker;
/// also callable directly (e.g. in tests) for deterministic dispatch. Assumes the ambient
/// tenant scope can read across workspaces (platform admin) for the trusted server-side job.
/// </summary>
public interface IMailDispatcher
{
    /// <summary>Claims and attempts delivery of up to <paramref name="batchSize"/> due messages. Returns the count processed.</summary>
    Task<int> DispatchDueAsync(int batchSize = 25, CancellationToken ct = default);
}
