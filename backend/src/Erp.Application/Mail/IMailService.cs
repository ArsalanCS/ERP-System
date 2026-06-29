using Erp.Application.Common;
using Erp.Application.Mail.Contracts;
using Erp.Shared.Results;

namespace Erp.Application.Mail;

/// <summary>
/// Read + management surface for the mail outbox and per-workspace templates. The actual
/// delivery is performed asynchronously by the dispatcher background worker.
/// </summary>
public interface IMailService
{
    Task<Result<PagedResult<SendMailListItemDto>>> ListOutboxAsync(OutboxQuery query, CancellationToken ct = default);
    Task<Result<SendMailDetailDto>> GetAsync(long id, CancellationToken ct = default);
    Task<Result> RetryAsync(long id, CancellationToken ct = default);
    Task<Result> CancelAsync(long id, CancellationToken ct = default);

    Task<Result<IReadOnlyList<MailTemplateDto>>> ListTemplatesAsync(CancellationToken ct = default);
    Task<Result> UpdateTemplateAsync(long id, UpdateMailTemplateRequest request, CancellationToken ct = default);
}
