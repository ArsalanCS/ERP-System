using System.Data.Common;
using Erp.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Erp.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Sets the PostgreSQL RLS session variables from the current
/// <see cref="ITenantContext"/> every time a connection is opened. This is the
/// database-layer half of tenant isolation; the EF global query filter is the
/// application-layer half (CLAUDE.md §4.1 — belt and suspenders).
///
/// Npgsql issues <c>DISCARD ALL</c> when a connection returns to the pool, so
/// these settings never leak across requests.
/// </summary>
public sealed class RlsConnectionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ApplyAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
        base.ConnectionOpened(connection, eventData);
    }

    private async Task ApplyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is not NpgsqlConnection npgsql)
        {
            return;
        }

        var workspaceId = tenantContext.WorkspaceId?.ToString() ?? string.Empty;
        var isPlatformAdmin = tenantContext.IsPlatformAdmin ? "true" : "false";

        await using var command = npgsql.CreateCommand();
        // set_config(name, value, is_local=false) -> session-scoped, parameterizable (unlike SET).
        command.CommandText =
            "SELECT set_config(@wsKey, @ws, false), set_config(@adminKey, @admin, false);";
        command.Parameters.AddWithValue("wsKey", RlsConstants.WorkspaceSetting);
        command.Parameters.AddWithValue("ws", workspaceId);
        command.Parameters.AddWithValue("adminKey", RlsConstants.PlatformAdminSetting);
        command.Parameters.AddWithValue("admin", isPlatformAdmin);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
