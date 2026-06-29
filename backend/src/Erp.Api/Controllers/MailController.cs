using Erp.Api.Security;
using Erp.Application.Mail;
using Erp.Application.Mail.Contracts;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Mail outbox + template management. Messages are queued by module triggers (e.g. task
/// assigned) and delivered asynchronously by the dispatcher worker; these endpoints expose
/// the queue for monitoring and let admins edit per-workspace templates.
/// </summary>
[Authorize]
[Route("api/v1/mail")]
public sealed class MailController(IMailService mail) : ApiControllerBase
{
    [HttpGet("outbox")]
    [RequirePermission(PermissionCatalog.MailView)]
    public async Task<IActionResult> Outbox([FromQuery] OutboxQuery query, CancellationToken ct)
        => FromResult(await mail.ListOutboxAsync(query, ct), Ok);

    [HttpGet("outbox/{id:guid}")]
    [RequirePermission(PermissionCatalog.MailView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => FromResult(await mail.GetAsync(id, ct), Ok);

    [HttpPost("outbox/{id:guid}/retry")]
    [RequirePermission(PermissionCatalog.MailManage)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
        => FromResult(await mail.RetryAsync(id, ct), NoContent);

    [HttpPost("outbox/{id:guid}/cancel")]
    [RequirePermission(PermissionCatalog.MailManage)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => FromResult(await mail.CancelAsync(id, ct), NoContent);

    [HttpGet("templates")]
    [RequirePermission(PermissionCatalog.MailView)]
    public async Task<IActionResult> Templates(CancellationToken ct)
        => FromResult(await mail.ListTemplatesAsync(ct), Ok);

    [HttpPut("templates/{id:guid}")]
    [RequirePermission(PermissionCatalog.MailManage)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateMailTemplateRequest request, CancellationToken ct)
        => FromResult(await mail.UpdateTemplateAsync(id, request, ct), NoContent);
}
