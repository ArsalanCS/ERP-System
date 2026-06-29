using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMailOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mail_templates",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mail_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "send_mails",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    template_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    related_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("pk_send_mails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "send_mail_attempts",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    send_mail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("pk_send_mail_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_send_mail_attempts_send_mails_send_mail_id",
                        column: x => x.send_mail_id,
                        principalSchema: "bpm",
                        principalTable: "send_mails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "send_mail_recipients",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    send_mail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                    table.PrimaryKey("pk_send_mail_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_send_mail_recipients_send_mails_send_mail_id",
                        column: x => x.send_mail_id,
                        principalSchema: "bpm",
                        principalTable: "send_mails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mail_templates_workspace_id_code",
                schema: "bpm",
                table: "mail_templates",
                columns: new[] { "workspace_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_send_mail_attempts_send_mail_id",
                schema: "bpm",
                table: "send_mail_attempts",
                column: "send_mail_id");

            migrationBuilder.CreateIndex(
                name: "ix_send_mail_recipients_send_mail_id",
                schema: "bpm",
                table: "send_mail_recipients",
                column: "send_mail_id");

            migrationBuilder.CreateIndex(
                name: "ix_send_mails_related_event_id",
                schema: "bpm",
                table: "send_mails",
                column: "related_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_send_mails_status_next_attempt_at",
                schema: "bpm",
                table: "send_mails",
                columns: new[] { "status", "next_attempt_at" });

            // Two-layer tenant isolation: enable Postgres RLS on every new tenant-owned bpm table.
            foreach (var table in new[]
            {
                "bpm.mail_templates", "bpm.send_mails", "bpm.send_mail_recipients", "bpm.send_mail_attempts",
            })
            {
                migrationBuilder.Sql($"SELECT erp_enable_tenant_rls('{table}');");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mail_templates",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "send_mail_attempts",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "send_mail_recipients",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "send_mails",
                schema: "bpm");
        }
    }
}
