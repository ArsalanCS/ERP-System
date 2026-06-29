using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.Application.Common;
using Erp.Application.Tasks.Contracts;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Tasks;

[Collection(ApiCollection.Name)]
public sealed class TasksEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public TasksEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string Password = "Str0ngPass!";
    private static string Slug() => $"ws-{Guid.NewGuid():N}"[..16];

    private async Task<HttpClient> OwnerClientAsync(string slug, string email = "owner@x.test")
    {
        await _factory.SeedOwnerUserAsync(slug, email, Password);
        return await LoginAsync(slug, email);
    }

    private async Task<HttpClient> LoginAsync(string slug, string email)
    {
        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    private static async Task<List<StatusDto>> StatusesAsync(HttpClient client, string code)
        => (await client.GetFromJsonAsync<List<StatusDto>>($"/api/v1/tasks/statuses?code={code}"))!;

    private static CreateTaskRequest NewTask(string title) =>
        new(title, "desc", null, null, null, null, null);

    [Fact]
    public async Task Owner_can_create_get_and_progress_a_task()
    {
        var client = await OwnerClientAsync(Slug());

        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Ship it")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();
        Assert.NotNull(created);
        Assert.StartsWith("TSK-", created!.ReferenceNo);

        var detail = await client.GetFromJsonAsync<TaskDetailsDto>($"/api/v1/tasks/{created.EventId}");
        Assert.Equal("New", detail!.StatusName);

        var statuses = await StatusesAsync(client, "TASK_STATUS");
        var inProgress = statuses.First(s => s.Name == "In Progress");
        var change = await client.PostAsJsonAsync($"/api/v1/tasks/{created.EventId}/status",
            new ChangeStatusRequest(inProgress.Id, null));
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var after = await client.GetFromJsonAsync<TaskDetailsDto>($"/api/v1/tasks/{created.EventId}");
        Assert.Equal("In Progress", after!.StatusName);

        var page = await client.GetFromJsonAsync<PagedResult<TaskListItemDto>>("/api/v1/tasks");
        Assert.Contains(page!.Items, x => x.EventId == created.EventId);

        var activity = await client.GetFromJsonAsync<List<TaskActivityDto>>($"/api/v1/tasks/{created.EventId}/activity");
        Assert.Contains(activity!, a => a.Message.Contains("created"));
    }

    [Fact]
    public async Task A_closed_task_cannot_be_edited_until_reopened()
    {
        var client = await OwnerClientAsync(Slug());
        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Close me")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        var statuses = await StatusesAsync(client, "TASK_STATUS");
        var done = statuses.First(s => s.IsClosed);
        var initial = statuses.First(s => s.IsInitial);

        await client.PostAsJsonAsync($"/api/v1/tasks/{created!.EventId}/status", new ChangeStatusRequest(done.Id, null));

        var edit = await client.PutAsJsonAsync($"/api/v1/tasks/{created.EventId}",
            new UpdateTaskRequest("Renamed", "x", null, null, null, null, 50));
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);

        await client.PostAsJsonAsync($"/api/v1/tasks/{created.EventId}/status", new ChangeStatusRequest(initial.Id, null));
        var reEdit = await client.PutAsJsonAsync($"/api/v1/tasks/{created.EventId}",
            new UpdateTaskRequest("Renamed", "x", null, null, null, null, 50));
        Assert.Equal(HttpStatusCode.NoContent, reEdit.StatusCode);
    }

    [Fact]
    public async Task Notes_are_stored_as_assets_and_listed()
    {
        var client = await OwnerClientAsync(Slug());
        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("With notes")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        var add = await client.PostAsJsonAsync($"/api/v1/tasks/{created!.EventId}/notes",
            new CreateNoteRequest("Customer prefers email.", true, true));
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var notes = await client.GetFromJsonAsync<List<TaskNoteDto>>($"/api/v1/tasks/{created.EventId}/notes");
        Assert.Single(notes!);
        Assert.True(notes![0].IsPinned);
    }

    [Fact]
    public async Task A_subtask_is_linked_to_its_parent()
    {
        var client = await OwnerClientAsync(Slug());
        var parent = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Parent")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        var sub = await (await client.PostAsJsonAsync($"/api/v1/tasks/{parent!.EventId}/subtasks", NewTask("Child")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();
        Assert.NotNull(sub);

        var subs = await client.GetFromJsonAsync<List<TaskListItemDto>>($"/api/v1/tasks/{parent.EventId}/subtasks");
        Assert.Contains(subs!, x => x.EventId == sub!.EventId);
    }

    [Fact]
    public async Task Daily_reports_are_filed_listed_and_one_per_day()
    {
        var client = await OwnerClientAsync(Slug());
        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Report me")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        var add = await client.PostAsJsonAsync($"/api/v1/tasks/{created!.EventId}/daily-reports",
            new CreateDailyReportRequest(new DateOnly(2026, 6, 29), "Did the thing.", 4.5m, 4m, 2m, null));
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        // Same author + same day collides (multiple-per-day disabled by default).
        var dup = await client.PostAsJsonAsync($"/api/v1/tasks/{created.EventId}/daily-reports",
            new CreateDailyReportRequest(new DateOnly(2026, 6, 29), "Again.", 1m, 1m, 0m, null));
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var reports = await client.GetFromJsonAsync<List<TaskDailyReportDto>>($"/api/v1/tasks/{created.EventId}/daily-reports");
        Assert.Single(reports!);
        Assert.Equal("Did the thing.", reports![0].Description);
        Assert.Equal(4m, reports![0].ActualTime);

        // Logs reflect the report.
        var activity = await client.GetFromJsonAsync<List<TaskActivityDto>>($"/api/v1/tasks/{created.EventId}/activity");
        Assert.Contains(activity!, a => a.Message.Contains("Daily report"));
    }

    [Fact]
    public async Task Dashboard_summarises_the_visible_tasks()
    {
        var client = await OwnerClientAsync(Slug());

        var a = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Alpha"))).Content.ReadFromJsonAsync<CreateTaskResult>();
        await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Beta"));

        // Close one so the breakdown has both open and completed.
        var statuses = await StatusesAsync(client, "TASK_STATUS");
        var done = statuses.First(s => s.IsClosed);
        await client.PostAsJsonAsync($"/api/v1/tasks/{a!.EventId}/status", new ChangeStatusRequest(done.Id, null));

        var dash = await client.GetFromJsonAsync<TaskDashboardDto>("/api/v1/tasks/dashboard");
        Assert.NotNull(dash);
        Assert.True(dash!.Total >= 2);
        Assert.True(dash.Completed >= 1);
        Assert.True(dash.Open >= 1);
        Assert.Contains(dash.ByStatus, b => b.Name == "Done" && b.Count >= 1);
        Assert.Equal(14, dash.Trend.Count);

        var report = await client.GetFromJsonAsync<TaskReportDto>("/api/v1/tasks/report?closedOnly=true");
        Assert.True(report!.Completed >= 1);
        Assert.All(report.ByStatus, b => Assert.True(b.Count >= 1));

        // The enriched widgets are present on the dashboard payload.
        Assert.True(dash.InProgress >= 0);
        Assert.True(dash.DueThisWeek >= 0);
        Assert.NotNull(dash.RecentActivity);
        Assert.NotNull(dash.Gantt);
    }

    [Fact]
    public async Task Workspace_config_round_trips()
    {
        var client = await OwnerClientAsync(Slug());

        var defaults = await client.GetFromJsonAsync<TaskSettingsDto>("/api/v1/tasks/settings/config");
        Assert.NotNull(defaults);
        Assert.True(defaults!.NotifyOnTaskCreated);
        Assert.Equal(14, defaults.DashboardDefaultRangeDays);

        var updated = defaults with { NotifyOnTaskCreated = false, AllowMultipleReportsPerDay = true, DashboardDefaultRangeDays = 30 };
        var put = await client.PutAsJsonAsync("/api/v1/tasks/settings/config", updated);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var after = await client.GetFromJsonAsync<TaskSettingsDto>("/api/v1/tasks/settings/config");
        Assert.False(after!.NotifyOnTaskCreated);
        Assert.True(after.AllowMultipleReportsPerDay);
        Assert.Equal(30, after.DashboardDefaultRangeDays);
    }

    [Fact]
    public async Task Daily_reports_report_lists_filed_reports()
    {
        var client = await OwnerClientAsync(Slug());
        var task = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Reportable")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        var add = await client.PostAsJsonAsync($"/api/v1/tasks/{task!.EventId}/daily-reports",
            new CreateDailyReportRequest(new DateOnly(2026, 6, 28), "Worked on it.", 3m, 2m, 1m, null));
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var rows = await client.GetFromJsonAsync<PagedResult<TaskDailyReportRowDto>>(
            "/api/v1/tasks/report/daily-reports?fromDate=2026-06-01&toDate=2026-06-30");
        Assert.NotNull(rows);
        Assert.Contains(rows!.Items, r => r.EventId == task.EventId && r.Description == "Worked on it." && r.ActualTime == 2m);
    }

    [Fact]
    public async Task Settings_create_update_reorder_and_delete_a_status()
    {
        var client = await OwnerClientAsync(Slug());

        // Create a new TASK_STATUS value.
        var created = await client.PostAsJsonAsync("/api/v1/tasks/settings/statuses",
            new CreateStatusRequest("TASK_STATUS", "Blocked", "#f59e0b", false, false));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var statuses = await client.GetFromJsonAsync<List<StatusDto>>("/api/v1/tasks/settings/statuses?code=TASK_STATUS");
        var blocked = statuses!.First(s => s.Name == "Blocked");

        // Update it.
        var upd = await client.PutAsJsonAsync($"/api/v1/tasks/settings/statuses/{blocked.Id}",
            new UpdateStatusRequest("On Hold", "#f97316", false, false, true));
        Assert.Equal(HttpStatusCode.NoContent, upd.StatusCode);

        // Reorder (put the new one first).
        var ids = statuses!.Select(s => s.Id).OrderBy(_ => Guid.NewGuid()).ToList();
        var reorder = await client.PostAsJsonAsync("/api/v1/tasks/settings/statuses/reorder",
            new ReorderStatusesRequest("TASK_STATUS", ids));
        Assert.Equal(HttpStatusCode.NoContent, reorder.StatusCode);

        // Delete the unused status.
        var del = await client.DeleteAsync($"/api/v1/tasks/settings/statuses/{blocked.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task A_status_in_use_cannot_be_deleted()
    {
        var client = await OwnerClientAsync(Slug());
        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Uses status")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        // The task's current status (New) is now in use.
        var statuses = await client.GetFromJsonAsync<List<StatusDto>>("/api/v1/tasks/settings/statuses?code=TASK_STATUS");
        var newStatus = statuses!.First(s => s.Code == "NEW");
        Assert.NotNull(created);

        var del = await client.DeleteAsync($"/api/v1/tasks/settings/statuses/{newStatus.Id}");
        // NEW is both initial and in use — either guard yields a conflict.
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
    }

    [Fact]
    public async Task Managing_statuses_requires_workflow_permission()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "plain2@x.test", Password);
        var client = await LoginAsync(slug, "plain2@x.test");
        var resp = await client.PostAsJsonAsync("/api/v1/tasks/settings/statuses",
            new CreateStatusRequest("TASK_STATUS", "Nope", null, false, false));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Tasks_require_permission()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "plain@x.test", Password);
        var client = await LoginAsync(slug, "plain@x.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/tasks")).StatusCode);
    }

    [Fact]
    public async Task A_task_is_not_visible_from_another_workspace()
    {
        var clientA = await OwnerClientAsync(Slug(), "a@x.test");
        var created = await (await clientA.PostAsJsonAsync("/api/v1/tasks", NewTask("Secret")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        var clientB = await OwnerClientAsync(Slug(), "b@x.test");
        var resp = await clientB.GetAsync($"/api/v1/tasks/{created!.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
