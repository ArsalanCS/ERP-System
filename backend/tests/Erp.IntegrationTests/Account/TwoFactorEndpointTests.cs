using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Erp.Application.Account;
using Erp.Application.Auth;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Account;

[Collection(ApiCollection.Name)]
public sealed class TwoFactorEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public TwoFactorEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string Password = "Str0ngPass!";

    private async Task<(HttpClient client, string slug, string email)> OwnerClientAsync()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var email = $"u{Guid.NewGuid():N}"[..10] + "@x.test";
        await _factory.SeedOwnerUserAsync(slug, email, Password);
        var client = _factory.CreateClient();
        var tokens = await Login(client, slug, email, Password, null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return (client, slug, email);
    }

    private static async Task<AuthTokens?> Login(HttpClient client, string slug, string email, string pw, string? code)
    {
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(slug, email, pw, code));
        return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<AuthTokens>() : null;
    }

    [Fact]
    public async Task Enroll_then_login_requires_a_valid_code()
    {
        var (client, slug, email) = await OwnerClientAsync();

        // 1) Begin enrollment → get the shared secret.
        var setup = await (await client.PostAsync("/api/v1/me/2fa/setup", null))
            .Content.ReadFromJsonAsync<TwoFactorSetupDto>();
        Assert.NotNull(setup);
        Assert.Contains("otpauth://", setup!.OtpAuthUri);

        // 2) A wrong code is rejected.
        var bad = await client.PostAsJsonAsync("/api/v1/me/2fa/enable", new TwoFactorCodeRequest("000000"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);

        // 3) The correct current code enables 2FA.
        var good = await client.PostAsJsonAsync("/api/v1/me/2fa/enable",
            new TwoFactorCodeRequest(Totp(setup.Secret)));
        Assert.Equal(HttpStatusCode.NoContent, good.StatusCode);

        // 4) Login now requires a code: without one → 401 AUTH_2FA_REQUIRED.
        var fresh = _factory.CreateClient();
        var noCode = await fresh.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(slug, email, Password));
        Assert.Equal(HttpStatusCode.Unauthorized, noCode.StatusCode);

        // 5) With a valid code → success.
        var ok = await Login(fresh, slug, email, Password, Totp(setup.Secret));
        Assert.NotNull(ok);
    }

    [Fact]
    public async Task Disable_requires_a_valid_code_and_restores_single_factor_login()
    {
        var (client, slug, email) = await OwnerClientAsync();

        var setup = await (await client.PostAsync("/api/v1/me/2fa/setup", null))
            .Content.ReadFromJsonAsync<TwoFactorSetupDto>();
        await client.PostAsJsonAsync("/api/v1/me/2fa/enable", new TwoFactorCodeRequest(Totp(setup!.Secret)));

        // Re-authenticate with a code to get a fresh token (enable rotated the stamp).
        var withCode = await Login(_factory.CreateClient(), slug, email, Password, Totp(setup.Secret));
        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", withCode!.AccessToken);

        var bad = await client2.PostAsJsonAsync("/api/v1/me/2fa/disable", new TwoFactorCodeRequest("000000"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);

        var ok = await client2.PostAsJsonAsync("/api/v1/me/2fa/disable", new TwoFactorCodeRequest(Totp(setup.Secret)));
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);

        // Single-factor login works again.
        var plain = await Login(_factory.CreateClient(), slug, email, Password, null);
        Assert.NotNull(plain);
    }

    // ---- Test-side TOTP generator (RFC 6238, mirrors TotpService) -----------
    private static string Totp(string base32Secret)
    {
        var key = Base32Decode(base32Secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);
        var hash = HMACSHA1.HashData(key, counterBytes);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString().PadLeft(6, '0');
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        input = input.TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        int buffer = 0, bitsLeft = 0;
        foreach (var c in input)
        {
            buffer = (buffer << 5) | alphabet.IndexOf(c);
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)((buffer >> bitsLeft) & 0xff));
            }
        }
        return [.. output];
    }
}
