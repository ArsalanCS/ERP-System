using System.Net;
using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public sealed class RegistrationEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public RegistrationEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string Password = "Str0ngPass!";

    private static RegisterWorkspaceRequest NewRegistration(out string slug, out string email)
    {
        slug = $"acme-{Guid.NewGuid():N}"[..14];
        email = $"owner-{Guid.NewGuid():N}"[..12] + "@x.test";
        return new RegisterWorkspaceRequest(
            WorkspaceName: "Acme Trading Co.",
            Slug: slug,
            BaseCurrency: "SAR",
            Language: "en",
            FullName: "Ahmed Al-Qahtani",
            Email: email,
            Password: Password);
    }

    [Fact]
    public async Task Register_then_verify_then_login_succeeds()
    {
        var client = _factory.CreateClient();
        var req = NewRegistration(out var slug, out var email);

        // 1) Register → 201, no tokens returned.
        var reg = await client.PostAsJsonAsync("/api/v1/auth/register", req);
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var result = await reg.Content.ReadFromJsonAsync<RegisterWorkspaceResult>();
        Assert.Equal(slug, result!.Slug);

        // 2) Login before verifying is refused — the owner is still PendingInvitation.
        var early = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(slug, email, Password));
        Assert.Equal(HttpStatusCode.Forbidden, early.StatusCode);

        // 3) Verify with the emailed token → 204.
        var token = _factory.Email.VerificationTokenFor(email);
        Assert.False(string.IsNullOrEmpty(token));
        var verify = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new VerifyEmailRequest(token!));
        Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);

        // 4) Login now works.
        var ok = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(slug, email, Password));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var tokens = await ok.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.False(string.IsNullOrEmpty(tokens!.AccessToken));
    }

    [Fact]
    public async Task Register_with_taken_slug_returns_conflict()
    {
        var client = _factory.CreateClient();
        var req = NewRegistration(out var slug, out _);
        var first = await client.PostAsJsonAsync("/api/v1/auth/register", req);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var dup = req with { Email = $"other-{Guid.NewGuid():N}"[..12] + "@x.test" };
        var second = await client.PostAsJsonAsync("/api/v1/auth/register", dup);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_with_invalid_slug_is_rejected()
    {
        var client = _factory.CreateClient();
        var req = NewRegistration(out _, out _) with { Slug = "Has Spaces!" };
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", req);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task Verify_with_garbage_token_is_rejected()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new VerifyEmailRequest("not-a-real-token"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task Resend_verification_is_always_accepted()
    {
        var client = _factory.CreateClient();
        // Unknown account → still 202 (no enumeration).
        var res = await client.PostAsJsonAsync("/api/v1/auth/resend-verification",
            new ResendVerificationRequest("no-such-workspace", "nobody@x.test"));
        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);
    }
}
