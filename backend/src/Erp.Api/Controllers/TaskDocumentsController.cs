using Erp.Api.Security;
using Erp.Application.Tasks;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>Task documents (Task Model spec §5). Read = task.view; write = task.document.manage.</summary>
[Authorize]
[Route("api/v1/tasks/{taskId:guid}/documents")]
public sealed class TaskDocumentsController(
    ITaskDocumentService documents,
    IValidator<CreateDocumentRequest> createValidator) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> List(Guid taskId, CancellationToken ct)
        => FromResult(await documents.ListAsync(taskId, ct), Ok);

    [HttpPost]
    [RequirePermission(PermissionCatalog.TaskDocumentManage)]
    public async Task<IActionResult> Create(Guid taskId, [FromBody] CreateDocumentRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await documents.AddAsync(taskId, request, ct), id => Created($"/api/v1/tasks/{taskId}/documents", new { id }));
    }

    [HttpDelete("{documentId:guid}")]
    [RequirePermission(PermissionCatalog.TaskDocumentManage)]
    public async Task<IActionResult> Remove(Guid taskId, Guid documentId, CancellationToken ct)
        => FromResult(await documents.RemoveAsync(taskId, documentId, ct), NoContent);
}
