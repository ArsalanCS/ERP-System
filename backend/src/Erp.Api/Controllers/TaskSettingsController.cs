using Erp.Api.Security;
using Erp.Application.Tasks;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Task workspace settings: status/priority catalog and the workspace task configuration
/// (daily-report rules, notifications, dashboard defaults).
/// </summary>
[Authorize]
[Route("api/v1/tasks/settings")]
public sealed class TaskSettingsController(
    ITaskSettingsService settings,
    IValidator<CreateStatusRequest> createStatusValidator,
    IValidator<UpdateStatusRequest> updateStatusValidator) : ApiControllerBase
{
    // ---- Statuses & priorities ----
    [HttpGet("statuses")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> SettingsStatuses([FromQuery] string code, CancellationToken ct)
        => FromResult(await settings.ListAsync(code, ct), Ok);

    [HttpPost("statuses")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> CreateStatus([FromBody] CreateStatusRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createStatusValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await settings.CreateStatusAsync(request, ct), id => Created($"/api/v1/tasks/settings/statuses/{id}", new { id }));
    }

    [HttpPut("statuses/{id:long}")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateStatusValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await settings.UpdateStatusAsync(id, request, ct), NoContent);
    }

    [HttpPost("statuses/reorder")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> ReorderStatuses([FromBody] ReorderStatusesRequest request, CancellationToken ct)
        => FromResult(await settings.ReorderAsync(request, ct), NoContent);

    [HttpDelete("statuses/{id:long}")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> DeleteStatus(long id, CancellationToken ct)
        => FromResult(await settings.DeleteStatusAsync(id, ct), NoContent);

    // ---- Workspace config (daily-report rules / notifications / dashboard defaults) ----
    [HttpGet("config")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
        => FromResult(await settings.GetSettingsAsync(ct), Ok);

    [HttpPut("config")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateTaskSettingsRequest request, CancellationToken ct)
        => FromResult(await settings.UpdateSettingsAsync(request, ct), NoContent);
}
