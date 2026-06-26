using Erp.Api.Security;
using Erp.Application.Tasks;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>Task notes (Task Model spec §5). Read = task.view; write = task.note.manage.</summary>
[Authorize]
[Route("api/v1/tasks/{taskId:guid}/notes")]
public sealed class TaskNotesController(
    ITaskNoteService notes,
    IValidator<CreateNoteRequest> createValidator,
    IValidator<UpdateNoteRequest> updateValidator) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> List(Guid taskId, CancellationToken ct)
        => FromResult(await notes.ListAsync(taskId, ct), Ok);

    [HttpPost]
    [RequirePermission(PermissionCatalog.TaskNoteManage)]
    public async Task<IActionResult> Create(Guid taskId, [FromBody] CreateNoteRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await notes.AddAsync(taskId, request, ct), id => Created($"/api/v1/tasks/{taskId}/notes", new { id }));
    }

    [HttpPut("{noteId:guid}")]
    [RequirePermission(PermissionCatalog.TaskNoteManage)]
    public async Task<IActionResult> Update(Guid taskId, Guid noteId, [FromBody] UpdateNoteRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await notes.UpdateAsync(taskId, noteId, request, ct), NoContent);
    }

    [HttpDelete("{noteId:guid}")]
    [RequirePermission(PermissionCatalog.TaskNoteManage)]
    public async Task<IActionResult> Remove(Guid taskId, Guid noteId, CancellationToken ct)
        => FromResult(await notes.RemoveAsync(taskId, noteId, ct), NoContent);
}
