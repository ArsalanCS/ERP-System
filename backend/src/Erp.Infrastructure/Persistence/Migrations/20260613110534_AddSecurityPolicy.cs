using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workspace_security_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    password_min_length = table.Column<int>(type: "integer", nullable: false),
                    require_uppercase = table.Column<bool>(type: "boolean", nullable: false),
                    require_lowercase = table.Column<bool>(type: "boolean", nullable: false),
                    require_digit = table.Column<bool>(type: "boolean", nullable: false),
                    require_symbol = table.Column<bool>(type: "boolean", nullable: false),
                    password_expiry_days = table.Column<int>(type: "integer", nullable: true),
                    max_failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    lockout_minutes = table.Column<int>(type: "integer", nullable: false),
                    session_idle_timeout_minutes = table.Column<int>(type: "integer", nullable: false),
                    refresh_token_days = table.Column<int>(type: "integer", nullable: false),
                    require_two_factor = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_security_policies", x => x.id);
                });

            // One policy row per workspace.
            migrationBuilder.CreateIndex(
                name: "ix_workspace_security_policies_workspace_id",
                table: "workspace_security_policies",
                column: "workspace_id",
                unique: true);

            // Tenant isolation: FORCE row-level security keyed on the workspace GUC.
            migrationBuilder.Sql("SELECT erp_enable_tenant_rls('workspace_security_policies');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workspace_security_policies");
        }
    }
}
