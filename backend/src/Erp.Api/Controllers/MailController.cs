using Erp.Api.Security;
using Erp.Application.Mail;
using Erp.Application.Mail.Contracts;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Mail outbox monitoring. Messages are queued by module triggers (e.g. task assigned) and
/// delivered asynchronously by the dispatcher worker; these endpoints expose the queue for
/// monitoring and operator retry/cancel. Template management lives in
/// <see cref="MailTemplatesController"/>.
/// </summary>
[Authorize]
[Route("api/v1/mail")]
public sealed class MailController(IMailService mail) : ApiControllerBase
{
    [HttpGet("outbox")]
    [RequirePermission(PermissionCatalog.MailView)]
    public async Task<IActionResult> Outbox([FromQuery] OutboxQuery query, CancellationToken ct)
        => FromResult(await mail.ListOutboxAsync(query, ct), Ok);

    [HttpGet("outbox/{id:long}")]
    [RequirePermission(PermissionCatalog.MailView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
        => FromResult(await mail.GetAsync(id, ct), Ok);

    [HttpPost("outbox/{id:long}/retry")]
    [RequirePermission(PermissionCatalog.MailManage)]
    public async Task<IActionResult> Retry(long id, CancellationToken ct)
        => FromResult(await mail.RetryAsync(id, ct), NoContent);

    [HttpPost("outbox/{id:long}/cancel")]
    [RequirePermission(PermissionCatalog.MailManage)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => FromResult(await mail.CancelAsync(id, ct), NoContent);
}
