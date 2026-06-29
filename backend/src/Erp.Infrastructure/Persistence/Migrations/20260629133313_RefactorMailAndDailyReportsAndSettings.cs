using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMailAndDailyReportsAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_send_mails_related_event_id",
                schema: "bpm",
                table: "send_mails");

            migrationBuilder.DropColumn(
                name: "subject",
                schema: "bpm",
                table: "mail_templates");

            migrationBuilder.DropColumn(
                name: "blockers",
                schema: "bpm",
                table: "event_daily_reports");

            migrationBuilder.DropColumn(
                name: "completion_percent",
                schema: "bpm",
                table: "event_daily_reports");

            migrationBuilder.DropColumn(
                name: "summary",
                schema: "bpm",
                table: "event_daily_reports");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "bpm",
                table: "send_mails",
                newName: "send_status");

            migrationBuilder.RenameColumn(
                name: "related_event_id",
                schema: "bpm",
                table: "send_mails",
                newName: "mail_template_id");

            migrationBuilder.RenameColumn(
                name: "max_attempts",
                schema: "bpm",
                table: "send_mails",
                newName: "retry_count");

            migrationBuilder.RenameColumn(
                name: "body",
                schema: "bpm",
                table: "send_mails",
                newName: "body_html");

            migrationBuilder.RenameColumn(
                name: "attempt_count",
                schema: "bpm",
                table: "send_mails",
                newName: "max_retries");

            migrationBuilder.RenameIndex(
                name: "ix_send_mails_status_next_attempt_at",
                schema: "bpm",
                table: "send_mails",
                newName: "ix_send_mails_send_status_next_attempt_at");

            migrationBuilder.RenameColumn(
                name: "kind",
                schema: "bpm",
                table: "send_mail_recipients",
                newName: "recipient_type");

            migrationBuilder.RenameColumn(
                name: "error",
                schema: "bpm",
                table: "send_mail_attempts",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "body",
                schema: "bpm",
                table: "mail_templates",
                newName: "body_html_template");

            migrationBuilder.RenameColumn(
                name: "hours_spent",
                schema: "bpm",
                table: "event_daily_reports",
                newName: "remaining_time");

            migrationBuilder.RenameColumn(
                name: "author_id",
                schema: "bpm",
                table: "event_daily_reports",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_daily_reports_event_id_report_date_author_id",
                schema: "bpm",
                table: "event_daily_reports",
                newName: "ix_event_daily_reports_event_id_report_date_user_id");

            migrationBuilder.AlterColumn<string>(
                name: "subject",
                schema: "bpm",
                table: "send_mails",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "body_text",
                schema: "bpm",
                table: "send_mails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "template_data_json",
                schema: "bpm",
                table: "send_mails",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "attempt_no",
                schema: "bpm",
                table: "send_mail_attempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "provider_response",
                schema: "bpm",
                table: "send_mail_attempts",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "workspace_id",
                schema: "bpm",
                table: "mail_templates",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "body_text_template",
                schema: "bpm",
                table: "mail_templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_template",
                schema: "bpm",
                table: "mail_templates",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "actual_time",
                schema: "bpm",
                table: "event_daily_reports",
                type: "numeric(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "bpm",
                table: "event_daily_reports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_time",
                schema: "bpm",
                table: "event_daily_reports",
                type: "numeric(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "status_id",
                schema: "bpm",
                table: "event_daily_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "task_settings",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    daily_report_required = table.Column<bool>(type: "boolean", nullable: false),
                    allow_status_change_from_report = table.Column<bool>(type: "boolean", nullable: false),
                    require_actual_time = table.Column<bool>(type: "boolean", nullable: false),
                    require_estimated_time = table.Column<bool>(type: "boolean", nullable: false),
                    allow_multiple_reports_per_day = table.Column<bool>(type: "boolean", nullable: false),
                    notify_on_task_created = table.Column<bool>(type: "boolean", nullable: false),
                    notify_on_task_assigned = table.Column<bool>(type: "boolean", nullable: false),
                    notify_on_status_change = table.Column<bool>(type: "boolean", nullable: false),
                    notify_on_daily_report = table.Column<bool>(type: "boolean", nullable: false),
                    dashboard_default_range_days = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_task_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_daily_reports_status_id",
                schema: "bpm",
                table: "event_daily_reports",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_settings_workspace_id",
                schema: "bpm",
                table: "task_settings",
                column: "workspace_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_event_daily_reports_statuses_status_id",
                schema: "bpm",
                table: "event_daily_reports",
                column: "status_id",
                principalSchema: "bpm",
                principalTable: "statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // task_settings is tenant-owned: enable two-layer isolation (RLS).
            migrationBuilder.Sql("SELECT erp_enable_tenant_rls('bpm.task_settings');");

            // mail_templates is now a GLOBAL catalogue (nullable workspace_id, null = shared default),
            // so it must NOT be RLS-restricted. Remove the policy added by the previous migration.
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON bpm.mail_templates;");
            migrationBuilder.Sql("ALTER TABLE bpm.mail_templates NO FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE bpm.mail_templates DISABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_daily_reports_statuses_status_id",
                schema: "bpm",
                table: "event_daily_reports");

            migrationBuilder.DropTable(
                name: "task_settings",
                schema: "bpm");

            migrationBuilder.DropIndex(
                name: "ix_event_daily_reports_status_id",
                schema: "bpm",
                table: "event_daily_reports");

            migrationBuilder.DropColumn(
                name: "body_text",
                schema: "bpm",
                table: "send_mails");

            migrationBuilder.DropColumn(
                name: "template_data_json",
                schema: "bpm",
                table: "send_mails");

            migrationBuilder.DropColumn(
                name: "attempt_no",
                schema: "bpm",
                table: "send_mail_attempts");

            migrationBuilder.DropColumn(
                name: "provider_response",
                schema: "bpm",
                table: "send_mail_attempts");

            migrationBuilder.DropColumn(
                name: "body_text_template",
                schema: "bpm",
                table: "mail_templates");

            migrationBuilder.DropColumn(
                name: "subject_template",
                schema: "bpm",
                table: "mail_templates");

            migrationBuilder.DropColumn(
                name: "actual_time",
                schema: "bpm",
                table: "event_daily_reports");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "bpm",
                table: "event_daily_reports");

            migrationBuilder.DropColumn(
                name: "estimated_time",
                schema: "bpm",
                table: "event_daily_reports");

            migrationBuilder.DropColumn(
                name: "status_id",
                schema: "bpm",
                table: "event_daily_reports");

            migrationBuilder.RenameColumn(
                name: "send_status",
                schema: "bpm",
                table: "send_mails",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "retry_count",
                schema: "bpm",
                table: "send_mails",
                newName: "max_attempts");

            migrationBuilder.RenameColumn(
                name: "max_retries",
                schema: "bpm",
                table: "send_mails",
                newName: "attempt_count");

            migrationBuilder.RenameColumn(
                name: "mail_template_id",
                schema: "bpm",
                table: "send_mails",
                newName: "related_event_id");

            migrationBuilder.RenameColumn(
                name: "body_html",
                schema: "bpm",
                table: "send_mails",
                newName: "body");

            migrationBuilder.RenameIndex(
                name: "ix_send_mails_send_status_next_attempt_at",
                schema: "bpm",
                table: "send_mails",
                newName: "ix_send_mails_status_next_attempt_at");

            migrationBuilder.RenameColumn(
                name: "recipient_type",
                schema: "bpm",
                table: "send_mail_recipients",
                newName: "kind");

            migrationBuilder.RenameColumn(
                name: "error_message",
                schema: "bpm",
                table: "send_mail_attempts",
                newName: "error");

            migrationBuilder.RenameColumn(
                name: "body_html_template",
                schema: "bpm",
                table: "mail_templates",
                newName: "body");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "bpm",
                table: "event_daily_reports",
                newName: "author_id");

            migrationBuilder.RenameColumn(
                name: "remaining_time",
                schema: "bpm",
                table: "event_daily_reports",
                newName: "hours_spent");

            migrationBuilder.RenameIndex(
                name: "ix_event_daily_reports_event_id_report_date_user_id",
                schema: "bpm",
                table: "event_daily_reports",
                newName: "ix_event_daily_reports_event_id_report_date_author_id");

            migrationBuilder.AlterColumn<string>(
                name: "subject",
                schema: "bpm",
                table: "send_mails",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<Guid>(
                name: "workspace_id",
                schema: "bpm",
                table: "mail_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject",
                schema: "bpm",
                table: "mail_templates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "blockers",
                schema: "bpm",
                table: "event_daily_reports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "completion_percent",
                schema: "bpm",
                table: "event_daily_reports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "summary",
                schema: "bpm",
                table: "event_daily_reports",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_send_mails_related_event_id",
                schema: "bpm",
                table: "send_mails",
                column: "related_event_id");
        }
    }
}
