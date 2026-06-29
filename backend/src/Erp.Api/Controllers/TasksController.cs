using Erp.Api.Security;
using Erp.Application.Tasks;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Task Management (Event/Asset architecture). A task is identified by its event id.
/// Reads require task.view; sub-resources require their specific permissions.
/// </summary>
[Authorize]
[Route("api/v1/tasks")]
public sealed class TasksController(
    ITaskService tasks,
    ITaskSettingsService settings,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<CreateDailyReportRequest> createReportValidator,
    IValidator<UpdateDailyReportRequest> updateReportValidator,
    IValidator<CreateStatusRequest> createStatusValidator,
    IValidator<UpdateStatusRequest> updateStatusValidator) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> List([FromQuery] TaskListQuery query, CancellationToken ct)
        => FromResult(await tasks.ListAsync(query, ct), Ok);

    [HttpGet("my")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> My(CancellationToken ct) => FromResult(await tasks.GetMyTasksAsync(ct), Ok);

    [HttpGet("dashboard")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Dashboard(CancellationToken ct) => FromResult(await tasks.GetDashboardAsync(ct), Ok);

    [HttpGet("report")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Report([FromQuery] TaskListQuery query, CancellationToken ct)
        => FromResult(await tasks.GetReportAsync(query, ct), Ok);

    [HttpGet("report/daily-reports")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> DailyReportsReport([FromQuery] TaskDailyReportQuery query, CancellationToken ct)
        => FromResult(await tasks.GetDailyReportsReportAsync(query, ct), Ok);

    [HttpGet("statuses")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Statuses([FromQuery] string code, CancellationToken ct)
        => FromResult(await tasks.ListStatusesAsync(code, ct), Ok);

    [HttpGet("{eventId:long}")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Get(long eventId, CancellationToken ct)
        => FromResult(await tasks.GetAsync(eventId, ct), Ok);

    [HttpGet("{eventId:long}/activity")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Activity(long eventId, CancellationToken ct)
        => FromResult(await tasks.GetActivityAsync(eventId, ct), Ok);

    [HttpGet("{eventId:long}/audit")]
    [RequirePermission(PermissionCatalog.TaskAuditView)]
    public async Task<IActionResult> Audit(long eventId, CancellationToken ct)
        => FromResult(await tasks.GetAuditAsync(eventId, ct), Ok);

    [HttpGet("{eventId:long}/subtasks")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Subtasks(long eventId, CancellationToken ct)
        => FromResult(await tasks.ListSubtasksAsync(eventId, ct), Ok);

    [HttpPost]
    [RequirePermission(PermissionCatalog.TaskCreate)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.CreateAsync(request, ct), r => Created($"/api/v1/tasks/{r.EventId}", r));
    }

    [HttpPost("{eventId:long}/subtasks")]
    [RequirePermission(PermissionCatalog.TaskCreate)]
    public async Task<IActionResult> CreateSubtask(long eventId, [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.CreateSubtaskAsync(eventId, request, ct), r => Created($"/api/v1/tasks/{r.EventId}", r));
    }

    [HttpPut("{eventId:long}")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> Update(long eventId, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.UpdateAsync(eventId, request, ct), NoContent);
    }

    [HttpPost("{eventId:long}/status")]
    [RequirePermission(PermissionCatalog.TaskChangeStatus)]
    public async Task<IActionResult> ChangeStatus(long eventId, [FromBody] ChangeStatusRequest request, CancellationToken ct)
        => FromResult(await tasks.ChangeStatusAsync(eventId, request, ct), NoContent);

    [HttpPost("{eventId:long}/assign")]
    [RequirePermission(PermissionCatalog.TaskAssign)]
    public async Task<IActionResult> Assign(long eventId, [FromBody] AssignTaskRequest request, CancellationToken ct)
        => FromResult(await tasks.AssignAsync(eventId, request, ct), NoContent);

    [HttpPost("{eventId:long}/priority")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> SetPriority(long eventId, [FromBody] SetPriorityRequest request, CancellationToken ct)
        => FromResult(await tasks.SetPriorityAsync(eventId, request, ct), NoContent);

    [HttpDelete("{eventId:long}")]
    [RequirePermission(PermissionCatalog.TaskArchive)]
    public async Task<IActionResult> Archive(long eventId, CancellationToken ct)
        => FromResult(await tasks.ArchiveAsync(eventId, ct), NoContent);

    // ---- Notes ----
    [HttpGet("{eventId:long}/notes")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Notes(long eventId, CancellationToken ct)
        => FromResult(await tasks.ListNotesAsync(eventId, ct), Ok);

    [HttpPost("{eventId:long}/notes")]
    [RequirePermission(PermissionCatalog.TaskNoteManage)]
    public async Task<IActionResult> AddNote(long eventId, [FromBody] CreateNoteRequest request, CancellationToken ct)
        => FromResult(await tasks.AddNoteAsync(eventId, request, ct), id => Created($"/api/v1/tasks/{eventId}/notes/{id}", new { id }));

    [HttpPut("{eventId:long}/notes/{noteId:long}")]
    [RequirePermission(PermissionCatalog.TaskNoteManage)]
    public async Task<IActionResult> UpdateNote(long eventId, long noteId, [FromBody] UpdateNoteRequest request, CancellationToken ct)
        => FromResult(await tasks.UpdateNoteAsync(eventId, noteId, request, ct), NoContent);

    [HttpDelete("{eventId:long}/notes/{noteId:long}")]
    [RequirePermission(PermissionCatalog.TaskNoteManage)]
    public async Task<IActionResult> RemoveNote(long eventId, long noteId, CancellationToken ct)
        => FromResult(await tasks.RemoveNoteAsync(eventId, noteId, ct), NoContent);

    // ---- Documents ----
    [HttpGet("{eventId:long}/documents")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Documents(long eventId, CancellationToken ct)
        => FromResult(await tasks.ListDocumentsAsync(eventId, ct), Ok);

    [HttpPost("{eventId:long}/documents")]
    [RequirePermission(PermissionCatalog.TaskDocumentManage)]
    public async Task<IActionResult> AddDocument(long eventId, [FromBody] CreateDocumentRequest request, CancellationToken ct)
        => FromResult(await tasks.AddDocumentAsync(eventId, request, ct), id => Created($"/api/v1/tasks/{eventId}/documents/{id}", new { id }));

    [HttpDelete("{eventId:long}/documents/{documentId:long}")]
    [RequirePermission(PermissionCatalog.TaskDocumentManage)]
    public async Task<IActionResult> RemoveDocument(long eventId, long documentId, CancellationToken ct)
        => FromResult(await tasks.RemoveDocumentAsync(eventId, documentId, ct), NoContent);

    // ---- Dependencies ----
    [HttpGet("{eventId:long}/dependencies")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Dependencies(long eventId, CancellationToken ct)
        => FromResult(await tasks.ListDependenciesAsync(eventId, ct), Ok);

    [HttpPost("{eventId:long}/dependencies")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> AddDependency(long eventId, [FromBody] CreateDependencyRequest request, CancellationToken ct)
        => FromResult(await tasks.AddDependencyAsync(eventId, request, ct), id => Created($"/api/v1/tasks/{eventId}/dependencies/{id}", new { id }));

    [HttpDelete("{eventId:long}/dependencies/{dependencyId:long}")]
    [RequirePermission(PermissionCatalog.TaskUpdate)]
    public async Task<IActionResult> RemoveDependency(long eventId, long dependencyId, CancellationToken ct)
        => FromResult(await tasks.RemoveDependencyAsync(eventId, dependencyId, ct), NoContent);

    // ---- Daily reports ----
    [HttpGet("{eventId:long}/daily-reports")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> DailyReports(long eventId, CancellationToken ct)
        => FromResult(await tasks.ListDailyReportsAsync(eventId, ct), Ok);

    [HttpPost("{eventId:long}/daily-reports")]
    [RequirePermission(PermissionCatalog.TaskDailyReportManage)]
    public async Task<IActionResult> AddDailyReport(long eventId, [FromBody] CreateDailyReportRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createReportValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.AddDailyReportAsync(eventId, request, ct), id => Created($"/api/v1/tasks/{eventId}/daily-reports/{id}", new { id }));
    }

    [HttpPut("{eventId:long}/daily-reports/{reportId:long}")]
    [RequirePermission(PermissionCatalog.TaskDailyReportManage)]
    public async Task<IActionResult> UpdateDailyReport(long eventId, long reportId, [FromBody] UpdateDailyReportRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateReportValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await tasks.UpdateDailyReportAsync(eventId, reportId, request, ct), NoContent);
    }

    [HttpDelete("{eventId:long}/daily-reports/{reportId:long}")]
    [RequirePermission(PermissionCatalog.TaskDailyReportManage)]
    public async Task<IActionResult> RemoveDailyReport(long eventId, long reportId, CancellationToken ct)
        => FromResult(await tasks.RemoveDailyReportAsync(eventId, reportId, ct), NoContent);

    // ---- Settings: statuses & priorities ----
    [HttpGet("settings/statuses")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> SettingsStatuses([FromQuery] string code, CancellationToken ct)
        => FromResult(await settings.ListAsync(code, ct), Ok);

    [HttpPost("settings/statuses")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> CreateStatus([FromBody] CreateStatusRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createStatusValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await settings.CreateStatusAsync(request, ct), id => Created($"/api/v1/tasks/settings/statuses/{id}", new { id }));
    }

    [HttpPut("settings/statuses/{id:long}")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateStatusValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await settings.UpdateStatusAsync(id, request, ct), NoContent);
    }

    [HttpPost("settings/statuses/reorder")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> ReorderStatuses([FromBody] ReorderStatusesRequest request, CancellationToken ct)
        => FromResult(await settings.ReorderAsync(request, ct), NoContent);

    [HttpDelete("settings/statuses/{id:long}")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> DeleteStatus(long id, CancellationToken ct)
        => FromResult(await settings.DeleteStatusAsync(id, ct), NoContent);

    // ---- Settings: workspace config (daily-report rules / notifications / dashboard defaults) ----
    [HttpGet("settings/config")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
        => FromResult(await settings.GetSettingsAsync(ct), Ok);

    [HttpPut("settings/config")]
    [RequirePermission(PermissionCatalog.TaskWorkflowManage)]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateTaskSettingsRequest request, CancellationToken ct)
        => FromResult(await settings.UpdateSettingsAsync(request, ct), NoContent);
}
