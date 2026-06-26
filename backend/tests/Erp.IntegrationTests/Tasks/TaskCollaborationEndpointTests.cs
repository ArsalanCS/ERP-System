using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Abstractions;
using Erp.Application.Auth;
using Erp.Application.Common;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;
using Erp.Domain.Identity;
using Erp.Domain.Tasks;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Tenancy;
using Erp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Erp.IntegrationTests.Tasks;

[Collection(ApiCollection.Name)]
public sealed class TaskCollaborationEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public TaskCollaborationEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string Password = "Str0ngPass!";
    private static string Slug() => $"ws-{Guid.NewGuid():N}"[..16];

    private sealed record IdHolder(Guid Id);

    private async Task<HttpClient> LoginAsync(string slug, string email)
    {
        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    private async Task<(HttpClient client, Guid workspaceId)> OwnerAsync(string slug, string email = "owner@x.test")
    {
        var (workspaceId, _) = await _factory.SeedOwnerUserAsync(slug, email, Password);
        return (await LoginAsync(slug, email), workspaceId);
    }

    private static async Task<Guid> SetupWorkflowAsync(HttpClient client)
    {
        var typeId = (await (await client.PostAsJsonAsync("/api/v1/task-workflows/types",
            new CreateStatusTypeRequest($"Flow {Guid.NewGuid():N}"[..12], null))).Content.ReadFromJsonAsync<IdHolder>())!.Id;
        await client.PostAsJsonAsync("/api/v1/task-workflows/statuses",
            new CreateStatusRequest(typeId, "New", TaskStatusCategory.Open, null, true, false));
        return typeId;
    }

    private static async Task<Guid> CreateTaskAsync(HttpClient client, string title, Guid? assignee = null)
    {
        var body = new CreateTaskRequest(title, null, TaskPriority.Normal, null, assignee, null, null, null, null);
        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", body)).Content.ReadFromJsonAsync<CreateTaskResult>();
        return created!.Id;
    }

    [Fact]
    public async Task Subtasks_and_checklist()
    {
        var (client, _) = await OwnerAsync(Slug());
        await SetupWorkflowAsync(client);
        var taskId = await CreateTaskAsync(client, "Parent");

        var sub = await (await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/subtasks",
            new CreateTaskRequest("Child", null, TaskPriority.Normal, null, null, null, null, null, null)))
            .Content.ReadFromJsonAsync<CreateTaskResult>();
        Assert.NotNull(sub);
        var subs = await client.GetFromJsonAsync<List<TaskListItemDto>>($"/api/v1/tasks/{taskId}/subtasks");
        Assert.Contains(subs!, s => s.Id == sub!.Id);

        var item = await (await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/checklist",
            new CreateChecklistItemRequest("Confirm phone"))).Content.ReadFromJsonAsync<IdHolder>();
        var upd = await client.PutAsJsonAsync($"/api/v1/tasks/{taskId}/checklist/{item!.Id}",
            new UpdateChecklistItemRequest("Confirm phone", true, 0));
        Assert.Equal(HttpStatusCode.NoContent, upd.StatusCode);
        var checklist = await client.GetFromJsonAsync<List<ChecklistItemDto>>($"/api/v1/tasks/{taskId}/checklist");
        Assert.Single(checklist!);
        Assert.True(checklist![0].IsDone);
    }

    [Fact]
    public async Task Notes_and_documents()
    {
        var (client, _) = await OwnerAsync(Slug());
        await SetupWorkflowAsync(client);
        var taskId = await CreateTaskAsync(client, "T");

        await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/notes", new CreateNoteRequest("Call back", true, true));
        var notes = await client.GetFromJsonAsync<List<TaskNoteDto>>($"/api/v1/tasks/{taskId}/notes");
        Assert.Single(notes!);
        Assert.True(notes![0].IsPinned);

        await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/documents",
            new CreateDocumentRequest("contract.pdf", "pdf", "https://files/x", null));
        var docs = await client.GetFromJsonAsync<List<TaskDocumentDto>>($"/api/v1/tasks/{taskId}/documents");
        Assert.Single(docs!);
        Assert.Equal("contract.pdf", docs![0].FileName);
    }

    [Fact]
    public async Task Dependencies_and_relations()
    {
        var (client, _) = await OwnerAsync(Slug());
        await SetupWorkflowAsync(client);
        var a = await CreateTaskAsync(client, "A");
        var b = await CreateTaskAsync(client, "B");

        var dep = await client.PostAsJsonAsync($"/api/v1/tasks/{a}/dependencies",
            new CreateDependencyRequest(b, TaskDependencyType.FinishToStart, true));
        Assert.Equal(HttpStatusCode.Created, dep.StatusCode);
        var deps = await client.GetFromJsonAsync<List<TaskDependencyDto>>($"/api/v1/tasks/{a}/dependencies");
        Assert.Single(deps!);

        // self-dependency rejected, duplicate rejected
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync($"/api/v1/tasks/{a}/dependencies",
            new CreateDependencyRequest(a, TaskDependencyType.FinishToStart, false))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/tasks/{a}/dependencies",
            new CreateDependencyRequest(b, TaskDependencyType.FinishToStart, false))).StatusCode);

        var rel = await client.PostAsJsonAsync($"/api/v1/tasks/{a}/relations",
            new CreateRelationRequest("Invoice", Guid.NewGuid(), TaskRelationRole.PrimarySource, "review before pay"));
        Assert.Equal(HttpStatusCode.Created, rel.StatusCode);
        // Refresh is idempotent — does not duplicate
        var refreshed = await (await client.PostAsync($"/api/v1/tasks/{a}/relations/refresh", null))
            .Content.ReadFromJsonAsync<List<TaskRelationDto>>();
        Assert.Single(refreshed!);
    }

    [Fact]
    public async Task My_tasks_and_audit()
    {
        var (client, _) = await OwnerAsync(Slug());
        await SetupWorkflowAsync(client);
        var taskId = await CreateTaskAsync(client, "Mine");

        var my = await client.GetFromJsonAsync<MyTasksGroups>("/api/v1/tasks/my");
        Assert.NotNull(my);

        var audit = await client.GetFromJsonAsync<List<TaskAuditDto>>($"/api/v1/tasks/{taskId}/audit");
        Assert.NotEmpty(audit!); // creation is audited
    }

    [Fact]
    public async Task Own_scope_user_sees_only_their_tasks()
    {
        var slug = Slug();
        var (owner, workspaceId) = await OwnerAsync(slug);
        await SetupWorkflowAsync(owner);

        // A second user in the SAME workspace, granted only task.view at Own scope.
        var memberId = await SeedScopedMemberAsync(workspaceId, "member@x.test", DataScope.Own);
        var member = await LoginAsync(slug, "member@x.test");

        await CreateTaskAsync(owner, "Owner task");                 // not visible to member
        var mineId = await CreateTaskAsync(owner, "Member task", assignee: memberId); // visible (assigned)

        var page = await member.GetFromJsonAsync<PagedResult<TaskListItemDto>>("/api/v1/tasks");
        Assert.NotNull(page);
        Assert.All(page!.Items, t => Assert.Equal(mineId, t.Id));
        Assert.Contains(page.Items, t => t.Id == mineId);
    }

    private async Task<Guid> SeedScopedMemberAsync(Guid workspaceId, string email, DataScope scope)
    {
        using var scopeSp = _factory.Services.CreateScope();
        var tenant = scopeSp.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetScope(workspaceId, [], isPlatformAdmin: true);
        var db = scopeSp.ServiceProvider.GetRequiredService<ErpDbContext>();
        var hasher = scopeSp.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User(workspaceId, email, "Mem", "Ber");
        user.SetPasswordHash(hasher.Hash(Password));
        user.Activate();
        db.Users.Add(user);

        var role = new Role(workspaceId, "Task Viewer", $"viewer-{Guid.NewGuid():N}"[..14], RoleType.Custom, "Scoped task viewer");
        db.Roles.Add(role);
        var permId = await db.Permissions.Where(p => p.Code == PermissionCatalog.TaskView).Select(p => p.Id).FirstAsync();
        db.RolePermissions.Add(new RolePermission(workspaceId, role.Id, permId, scope));
        db.UserRoles.Add(new UserRole(workspaceId, user.Id, role.Id));
        await db.SaveChangesAsync();
        return user.Id;
    }
}
