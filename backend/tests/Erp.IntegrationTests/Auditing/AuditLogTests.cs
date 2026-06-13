using Erp.Domain.Auditing;
using Erp.Domain.Tenancy;
using Erp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Erp.IntegrationTests.Auditing;

/// <summary>
/// Verifies audit_logs is append-only at the database layer (CLAUDE.md §4.3):
/// UPDATE and DELETE are rejected by the trigger.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AuditLogTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Audit_log_is_append_only()
    {
        await using var ctx = fixture.CreateContext(out _, platformAdmin: true);

        var ws = new Workspace($"WS {Guid.NewGuid():N}"[..16], $"ws-{Guid.NewGuid():N}"[..16], "en", "Asia/Riyadh", "SAR");
        ctx.Workspaces.Add(ws);
        await ctx.SaveChangesAsync();

        var log = new AuditLog(ws.Id, AuditActions.Login, "Identity", "User",
            AuditResult.Success, AuditSource.Api, DateTimeOffset.UtcNow, "corr-1");
        ctx.AuditLogs.Add(log);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            ctx.Database.ExecuteSqlAsync($"UPDATE audit_logs SET action = 'TAMPERED' WHERE id = {log.Id}"));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            ctx.Database.ExecuteSqlAsync($"DELETE FROM audit_logs WHERE id = {log.Id}"));
    }
}
