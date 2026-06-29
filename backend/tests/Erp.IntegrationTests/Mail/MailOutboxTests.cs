using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.Application.Common;
using Erp.Application.Mail.Contracts;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Mail;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Mail;

[Collection(ApiCollection.Name)]
public sealed class MailOutboxTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public MailOutboxTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string Password = "Str0ngPass!";
    private static string Slug() => $"ws-{Guid.NewGuid():N}"[..16];

    private async Task<HttpClient> LoginAsync(string slug, string email)
    {
        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    private static CreateTaskRequest TaskFor(long assignee) =>
        new("Notify me", "desc", assignee, null, null, null, null);

    private static async Task<SendMailListItemDto?> FindByRefAsync(HttpClient client, string referenceNo)
    {
        var page = await client.GetFromJsonAsync<PagedResult<SendMailListItemDto>>("/api/v1/mail/outbox?pageSize=100");
        return page!.Items.FirstOrDefault(m => m.Subject.Contains(referenceNo));
    }

    [Fact]
    public async Task Assigning_a_task_queues_a_notification_that_the_dispatcher_delivers()
    {
        var slug = Slug();
        var (wsId, _) = await _factory.SeedOwnerUserAsync(slug, "owner@x.test", Password);
        var client = await LoginAsync(slug, "owner@x.test");
        var assignee = await _factory.SeedExtraUserAsync(wsId, "assignee-deliver@x.test");

        var created = await (await client.PostAsJsonAsync("/api/v1/tasks", TaskFor(assignee)))
            .Content.ReadFromJsonAsync<CreateTaskResult>();

        // A pending message was queued for the assignee.
        var queued = await FindByRefAsync(client, created!.ReferenceNo);
        Assert.NotNull(queued);
        Assert.Equal(SendStatus.Pending, queued!.Status);

        var processed = await _factory.DispatchMailAsync();
        Assert.True(processed >= 1);

        var sent = await FindByRefAsync(client, created.ReferenceNo);
        Assert.Equal(SendStatus.Sent, sent!.Status);
        Assert.Contains(_factory.Email.Messages, m => m.To == "assignee-deliver@x.test");

        // Detail exposes recipients + a successful attempt.
        var detail = await client.GetFromJsonAsync<SendMailDetailDto>($"/api/v1/mail/outbox/{sent.Id}");
        Assert.Contains(detail!.Recipients, r => r.Address == "assignee-deliver@x.test");
        Assert.Contains(detail.Attempts, a => a.Success);
    }

    [Fact]
    public async Task A_failing_transport_keeps_the_message_for_retry()
    {
        var slug = Slug();
        var (wsId, _) = await _factory.SeedOwnerUserAsync(slug, "owner2@x.test", Password);
        var client = await LoginAsync(slug, "owner2@x.test");
        var assignee = await _factory.SeedExtraUserAsync(wsId, "assignee-fail@x.test");

        _factory.Email.FailMessages = true;
        try
        {
            var created = await (await client.PostAsJsonAsync("/api/v1/tasks", TaskFor(assignee)))
                .Content.ReadFromJsonAsync<CreateTaskResult>();

            await _factory.DispatchMailAsync();

            var failedOnce = await FindByRefAsync(client, created!.ReferenceNo);
            Assert.Equal(SendStatus.Pending, failedOnce!.Status); // back to pending, scheduled for retry
            Assert.Equal(1, failedOnce.RetryCount);
            Assert.NotNull(failedOnce.NextAttemptAt);
            Assert.False(string.IsNullOrEmpty(failedOnce.LastError));
        }
        finally
        {
            _factory.Email.FailMessages = false;
        }
    }

    [Fact]
    public async Task Default_mail_templates_are_seeded_for_a_workspace()
    {
        var slug = Slug();
        await _factory.SeedOwnerUserAsync(slug, "owner3@x.test", Password);
        var client = await LoginAsync(slug, "owner3@x.test");

        var templates = await client.GetFromJsonAsync<List<MailTemplateDto>>("/api/v1/mail/templates");
        Assert.Contains(templates!, t => t.Code == MailTemplateCodes.TaskAssigned);
        Assert.Equal(MailTemplateCodes.All.Count, templates!.Count);
    }

    [Fact]
    public async Task Outbox_requires_permission()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "plain@x.test", Password);
        var client = await LoginAsync(slug, "plain@x.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/mail/outbox")).StatusCode);
    }
}
