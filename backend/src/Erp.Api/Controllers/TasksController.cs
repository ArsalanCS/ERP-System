using Erp.Api.Security;
using Erp.Application.Tasks;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Task Management (Task Model spec). Task is the only Event. Reads require
/// task.view (scoped by the caller's DataScope); writes declare their own
/// permission. Sub-resources (subtasks/checklist/dependencies/relations) live
/// under /tasks/{id}. Writes are audited and tenant-scoped in the service.
/// </summary>
[Authorize]
[Route("api/v1/tasks")]
public sealed class TasksController(
    ITaskService tasks,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<CreateChecklistItemRequest> checklistCreateValidator,
    IValidator<UpdateChecklistItemRequest> checklistUpdateValidator,
    IValidator<CreateDependencyRequest> dependencyValidator,
    IValidator<CreateRelationRequest> relationValidator) : ApiControllerBase
{
    // ---- Tasks ----
    [HttpGet]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> List([FromQuery] TaskListQuery query, CancellationToken ct)
        => FromResult(await tasks.ListAsync(query, ct), Ok);

    [HttpGet("my")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> My(CancellationToken ct)
        => FromResult(await tasks.GetMyTasksAsync(ct), Ok);

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => FromResult(await tasks.GetAsync(id, ct), Ok);

    [HttpGet("{id:guid}/activity")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Activity(Guid id, CancellationToken ct)
        => FromResult(await tasks.GetActivityAsync(id, ct), Ok);

    [HttpGet("{id:guid}/audit")]
    [RequirePermission(PermissionCatalog.TaskAuditView)]
    public async Task<IActionResult> Audit(Guid id, CancellationToken ct)
        => FromResult(await tasks.GetAuditAsync(id, ct), Ok);

    [HttpPost]
    [RequirePermission(PermissionCatalog.TaskCreate)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.CreateAsync(request, ct), r => CreatedAtAction(nameof(Get), new { id = r.Id }, r));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.UpdateAsync(id, request, ct), NoContent);
    }

    [HttpPost("{id:guid}/status")]
    [RequirePermission(PermissionCatalog.TaskChangeStatus)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeTaskStatusRequest request, CancellationToken ct)
        => FromResult(await tasks.ChangeStatusAsync(id, request, ct), NoContent);

    [HttpPost("{id:guid}/assign")]
    [RequirePermission(PermissionCatalog.TaskAssign)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTaskRequest request, CancellationToken ct)
        => FromResult(await tasks.AssignAsync(id, request, ct), NoContent);

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCatalog.TaskArchive)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
        => FromResult(await tasks.ArchiveAsync(id, ct), NoContent);

    // ---- Subtasks ----
    [HttpGet("{id:guid}/subtasks")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Subtasks(Guid id, CancellationToken ct)
        => FromResult(await tasks.ListSubtasksAsync(id, ct), Ok);

    [HttpPost("{id:guid}/subtasks")]
    [RequirePermission(PermissionCatalog.TaskCreate)]
    public async Task<IActionResult> CreateSubtask(Guid id, [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.CreateSubtaskAsync(id, request, ct), r => CreatedAtAction(nameof(Get), new { id = r.Id }, r));
    }

    // ---- Checklist ----
    [HttpGet("{id:guid}/checklist")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Checklist(Guid id, CancellationToken ct)
        => FromResult(await tasks.ListChecklistAsync(id, ct), Ok);

    [HttpPost("{id:guid}/checklist")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> AddChecklistItem(Guid id, [FromBody] CreateChecklistItemRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(checklistCreateValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.AddChecklistItemAsync(id, request, ct), itemId => Created($"/api/v1/tasks/{id}/checklist", new { id = itemId }));
    }

    [HttpPut("{id:guid}/checklist/{itemId:guid}")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> UpdateChecklistItem(Guid id, Guid itemId, [FromBody] UpdateChecklistItemRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(checklistUpdateValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.UpdateChecklistItemAsync(id, itemId, request, ct), NoContent);
    }

    [HttpDelete("{id:guid}/checklist/{itemId:guid}")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> RemoveChecklistItem(Guid id, Guid itemId, CancellationToken ct)
        => FromResult(await tasks.RemoveChecklistItemAsync(id, itemId, ct), NoContent);

    // ---- Dependencies ----
    [HttpGet("{id:guid}/dependencies")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Dependencies(Guid id, CancellationToken ct)
        => FromResult(await tasks.ListDependenciesAsync(id, ct), Ok);

    [HttpPost("{id:guid}/dependencies")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> AddDependency(Guid id, [FromBody] CreateDependencyRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(dependencyValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.AddDependencyAsync(id, request, ct), depId => Created($"/api/v1/tasks/{id}/dependencies", new { id = depId }));
    }

    [HttpDelete("{id:guid}/dependencies/{dependencyId:guid}")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> RemoveDependency(Guid id, Guid dependencyId, CancellationToken ct)
        => FromResult(await tasks.RemoveDependencyAsync(id, dependencyId, ct), NoContent);

    // ---- Relations ----
    [HttpGet("{id:guid}/relations")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Relations(Guid id, CancellationToken ct)
        => FromResult(await tasks.ListRelationsAsync(id, ct), Ok);

    [HttpPost("{id:guid}/relations")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> AddRelation(Guid id, [FromBody] CreateRelationRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(relationValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.AddRelationAsync(id, request, ct), relId => Created($"/api/v1/tasks/{id}/relations", new { id = relId }));
    }

    [HttpPost("{id:guid}/relations/refresh")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> RefreshRelations(Guid id, CancellationToken ct)
        => FromResult(await tasks.RefreshRelationsAsync(id, ct), Ok);

    [HttpDelete("{id:guid}/relations/{relationId:guid}")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> RemoveRelation(Guid id, Guid relationId, CancellationToken ct)
        => FromResult(await tasks.RemoveRelationAsync(id, relationId, ct), NoContent);
}
