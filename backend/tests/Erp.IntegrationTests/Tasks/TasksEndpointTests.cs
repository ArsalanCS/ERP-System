using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.Application.Common;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Tasks;
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

    private sealed record IdHolder(Guid Id);

    /// <summary>Creates a 3-status workflow via the API and returns the status ids.</summary>
    private static async Task<(Guid typeId, Guid newId, Guid progressId, Guid doneId)> SetupWorkflowAsync(HttpClient client, string suffix)
    {
        var typeId = (await (await client.PostAsJsonAsync("/api/v1/task-workflows/types",
            new CreateStatusTypeRequest($"Flow {suffix}", null))).Content.ReadFromJsonAsync<IdHolder>())!.Id;

        async Task<Guid> AddStatus(string name, TaskStatusCategory cat, bool initial, bool final)
            => (await (await client.PostAsJsonAsync("/api/v1/task-workflows/statuses",
                new CreateStatusRequest(typeId, name, cat, null, initial, final))).Content.ReadFromJsonAsync<IdHolder>())!.Id;

        var newId = await AddStatus("New", TaskStatusCategory.Open, true, false);
        var progressId = await AddStatus("In Progress", TaskStatusCategory.InProgress, false, false);
        var doneId = await AddStatus("Done", TaskStatusCategory.Completed, false, true);
        return (typeId, newId, progressId, doneId);
    }

    private static CreateTaskRequest NewTask(string title) =>
        new(title, "desc", TaskPriority.Normal, null, null, null, null, null, null);

    [Fact]
    public async Task Owner_can_define_workflow_create_and_progress_a_task()
    {
        var client = await OwnerClientAsync(Slug());
        var u = Guid.NewGuid().ToString("N")[..6];
        var (_, newId, progressId, _) = await SetupWorkflowAsync(client, u);

        // Create a task — starts at the initial status, gets a generated number.
        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask($"Ship {u}")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();
        Assert.NotNull(created);
        Assert.StartsWith("TSK-", created!.TaskNumber);

        var detail = await client.GetFromJsonAsync<TaskDetailsDto>($"/api/v1/tasks/{created.Id}");
        Assert.Equal(newId, detail!.StatusId);
        Assert.Equal("New", detail.StatusName);

        // Move it forward.
        var change = await client.PostAsJsonAsync($"/api/v1/tasks/{created.Id}/status",
            new ChangeTaskStatusRequest(progressId));
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var afterChange = await client.GetFromJsonAsync<TaskDetailsDto>($"/api/v1/tasks/{created.Id}");
        Assert.Equal(progressId, afterChange!.StatusId);

        // Activity log captures creation + the status change (status history).
        var activity = await client.GetFromJsonAsync<List<TaskActivityDto>>($"/api/v1/tasks/{created.Id}/activity");
        Assert.Contains(activity!, a => a.Kind == TaskActivityKind.Created);
        Assert.Contains(activity!, a => a.Kind == TaskActivityKind.StatusChanged);

        // List shows the task.
        var page = await client.GetFromJsonAsync<PagedResult<TaskListItemDto>>("/api/v1/tasks");
        Assert.Contains(page!.Items, t => t.Id == created.Id);
    }

    [Fact]
    public async Task Create_task_without_a_workflow_is_rejected()
    {
        var client = await OwnerClientAsync(Slug());
        var resp = await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Orphan"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task Create_task_with_empty_title_is_rejected()
    {
        var client = await OwnerClientAsync(Slug());
        await SetupWorkflowAsync(client, Guid.NewGuid().ToString("N")[..6]);
        var resp = await client.PostAsJsonAsync("/api/v1/tasks", NewTask("   "));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task Status_change_to_a_foreign_workflow_status_is_rejected()
    {
        var client = await OwnerClientAsync(Slug());
        var (_, _, _, _) = await SetupWorkflowAsync(client, "a" + Guid.NewGuid().ToString("N")[..5]);
        var (_, foreignNew, _, _) = await SetupWorkflowAsync(client, "b" + Guid.NewGuid().ToString("N")[..5]);

        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("X")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{created!.Id}/status",
            new ChangeTaskStatusRequest(foreignNew));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task Tasks_and_workflow_management_require_permission()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "plain@x.test", Password);
        var client = await LoginAsync(slug, "plain@x.test");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/task-workflows/types",
            new CreateStatusTypeRequest("Nope", null))).StatusCode);
    }

    [Fact]
    public async Task A_closed_task_cannot_be_edited_until_reopened()
    {
        var client = await OwnerClientAsync(Slug());
        var (_, newId, _, doneId) = await SetupWorkflowAsync(client, Guid.NewGuid().ToString("N")[..6]);

        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", NewTask("Close me")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        // Move to a Final (Done) status.
        await client.PostAsJsonAsync($"/api/v1/tasks/{created!.Id}/status", new ChangeTaskStatusRequest(doneId));

        // Editing details on a closed task is rejected (spec §10 closed-status protection).
        var edit = await client.PutAsJsonAsync($"/api/v1/tasks/{created.Id}",
            new UpdateTaskRequest("Renamed", "x", TaskPriority.High, null, null, null, null, null, 50));
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);

        // Adding a checklist item to a closed task is likewise rejected.
        var addChecklist = await client.PostAsJsonAsync($"/api/v1/tasks/{created.Id}/checklist",
            new CreateChecklistItemRequest("step"));
        Assert.Equal(HttpStatusCode.Conflict, addChecklist.StatusCode);

        // Reopen (back to the initial status) and the edit now succeeds.
        await client.PostAsJsonAsync($"/api/v1/tasks/{created.Id}/status", new ChangeTaskStatusRequest(newId));
        var reEdit = await client.PutAsJsonAsync($"/api/v1/tasks/{created.Id}",
            new UpdateTaskRequest("Renamed", "x", TaskPriority.High, null, null, null, null, null, 50));
        Assert.Equal(HttpStatusCode.NoContent, reEdit.StatusCode);
    }

    [Fact]
    public async Task A_subtask_inherits_the_parent_task_source_by_default()
    {
        var client = await OwnerClientAsync(Slug());
        await SetupWorkflowAsync(client, Guid.NewGuid().ToString("N")[..6]);

        var sourceId = Guid.NewGuid();
        var parentReq = new CreateTaskRequest("Parent", "d", TaskPriority.Normal, null, null, null, null, null, null, "customer", sourceId);
        var parent = await (await client.PostAsJsonAsync("/api/v1/tasks", parentReq))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        // Subtask created with no source inherits the parent's.
        var sub = await (await client.PostAsJsonAsync($"/api/v1/tasks/{parent!.Id}/subtasks", NewTask("Child")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        var subDetail = await client.GetFromJsonAsync<TaskDetailsDto>($"/api/v1/tasks/{sub!.Id}");
        Assert.Equal("customer", subDetail!.SourceType);
        Assert.Equal(sourceId, subDetail.SourceId);
    }

    [Fact]
    public async Task A_task_is_not_visible_from_another_workspace()
    {
        var clientA = await OwnerClientAsync(Slug(), "a@x.test");
        await SetupWorkflowAsync(clientA, Guid.NewGuid().ToString("N")[..6]);
        var created = await (await clientA.PostAsJsonAsync("/api/v1/tasks", NewTask("Secret")))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        var clientB = await OwnerClientAsync(Slug(), "b@x.test");
        var resp = await clientB.GetAsync($"/api/v1/tasks/{created!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
