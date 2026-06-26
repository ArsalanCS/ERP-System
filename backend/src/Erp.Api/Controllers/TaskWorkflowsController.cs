using Erp.Api.Security;
using Erp.Application.Tasks;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Task status workflows (Task Model spec §4). Reading requires task.view (the
/// task create/edit UI needs the status list); managing types/statuses requires
/// task.workflow.manage. Writes are audited in the service.
/// </summary>
[Authorize]
[Route("api/v1/task-workflows")]
public sealed class TaskWorkflowsController(
    ITaskWorkflowService workflows,
    IValidator<CreateStatusTypeRequest> createTypeValidator,
    IValidator<UpdateStatusTypeRequest> updateTypeValidator,
    IValidator<CreateStatusRequest> createStatusValidator,
    IValidator<UpdateStatusRequest> updateStatusValidator) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Get(CancellationToken ct)
        => FromResult(await workflows.GetWorkflowsAsync(ct), Ok);

    [HttpPost("types")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> CreateType([FromBody] CreateStatusTypeRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createTypeValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await workflows.CreateStatusTypeAsync(request, ct),
            id => Created($"/api/v1/task-workflows", new { id }));
    }

    [HttpPut("types/{id:guid}")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> UpdateType(Guid id, [FromBody] UpdateStatusTypeRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateTypeValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await workflows.UpdateStatusTypeAsync(id, request, ct), NoContent);
    }

    [HttpDelete("types/{id:guid}")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> ArchiveType(Guid id, CancellationToken ct)
        => FromResult(await workflows.ArchiveStatusTypeAsync(id, ct), NoContent);

    [HttpPost("statuses")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> CreateStatus([FromBody] CreateStatusRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createStatusValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await workflows.CreateStatusAsync(request, ct),
            id => Created($"/api/v1/task-workflows", new { id }));
    }

    [HttpPut("statuses/{id:guid}")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateStatusValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await workflows.UpdateStatusAsync(id, request, ct), NoContent);
    }

    [HttpDelete("statuses/{id:guid}")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> ArchiveStatus(Guid id, CancellationToken ct)
        => FromResult(await workflows.ArchiveStatusAsync(id, ct), NoContent);
}
