using Erp.Api.Security;
using Erp.Application.Mail;
using Erp.Application.Mail.Contracts;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Per-workspace mail template management. Templates back the messages queued by module
/// triggers (e.g. task assigned) and delivered asynchronously by the dispatcher worker.
/// </summary>
[Authorize]
[Route("api/v1/mail/templates")]
public sealed class MailTemplatesController(IMailService mail) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.MailView)]
    public async Task<IActionResult> Templates(CancellationToken ct)
        => FromResult(await mail.ListTemplatesAsync(ct), Ok);

    [HttpPut("{id:long}")]
    [RequirePermission(PermissionCatalog.MailManage)]
    public async Task<IActionResult> UpdateTemplate(long id, [FromBody] UpdateMailTemplateRequest request, CancellationToken ct)
        => FromResult(await mail.UpdateTemplateAsync(id, request, ct), NoContent);
}
