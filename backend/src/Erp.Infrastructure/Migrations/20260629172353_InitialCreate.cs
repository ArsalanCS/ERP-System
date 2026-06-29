using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bpm");

            migrationBuilder.CreateTable(
                name: "asset_types",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_types",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mail_templates",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject_template = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body_html_template = table.Column<string>(type: "text", nullable: false),
                    body_text_template = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mail_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_high_risk = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "send_mails",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    mail_template_id = table.Column<long>(type: "bigint", nullable: true),
                    template_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    body_text = table.Column<string>(type: "text", nullable: true),
                    template_data_json = table.Column<string>(type: "jsonb", nullable: true),
                    send_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_send_mails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status_types",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_status_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "structure_nodes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    node_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    manager_id = table.Column<long>(type: "bigint", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_structure_nodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_structure_nodes_structure_nodes_parent_id",
                        column: x => x.parent_id,
                        principalTable: "structure_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_settings",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
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
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_security_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
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
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_security_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    default_language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "assets",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    asset_type_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_assets_asset_types_asset_type_id",
                        column: x => x.asset_type_id,
                        principalSchema: "bpm",
                        principalTable: "asset_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "events",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    event_type_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_events_event_types_event_type_id",
                        column: x => x.event_type_id,
                        principalSchema: "bpm",
                        principalTable: "event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    effect = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    cluster_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "send_mail_attempts",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    send_mail_id = table.Column<long>(type: "bigint", nullable: false),
                    attempt_no = table.Column<int>(type: "integer", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    provider_response = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
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
                    id = table.Column<long>(type: "bigint", nullable: false),
                    send_mail_id = table.Column<long>(type: "bigint", nullable: false),
                    address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recipient_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "statuses",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    status_type_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_initial = table.Column<bool>(type: "boolean", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_statuses_status_types_status_type_id",
                        column: x => x.status_type_id,
                        principalSchema: "bpm",
                        principalTable: "status_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    security_stamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    require_password_change = table.Column<bool>(type: "boolean", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    preferred_language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    access_start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    access_expiry_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false),
                    lockout_ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_secret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    asset_id = table.Column<long>(type: "bigint", nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "bpm",
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    asset_id = table.Column<long>(type: "bigint", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    is_internal = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_notes_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "bpm",
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_activities",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_id = table.Column<long>(type: "bigint", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    from_status_id = table.Column<long>(type: "bigint", nullable: true),
                    to_status_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_activities_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "bpm",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_assets",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    asset_id = table.Column<long>(type: "bigint", nullable: false),
                    relation_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_assets_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "bpm",
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_assets_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "bpm",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_dependencies",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    depends_on_event_id = table.Column<long>(type: "bigint", nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_dependencies_events_depends_on_event_id",
                        column: x => x.depends_on_event_id,
                        principalSchema: "bpm",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_dependencies_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "bpm",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_events",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    reference_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    assignee_id = table.Column<long>(type: "bigint", nullable: true),
                    reporter_id = table.Column<long>(type: "bigint", nullable: true),
                    parent_event_id = table.Column<long>(type: "bigint", nullable: true),
                    priority_status_id = table.Column<long>(type: "bigint", nullable: true),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estimated_time = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    actual_time = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    completion_percent = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_events_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "bpm",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_daily_reports",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    report_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    estimated_time = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    actual_time = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    remaining_time = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_daily_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_daily_reports_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "bpm",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_daily_reports_statuses_status_id",
                        column: x => x.status_id,
                        principalSchema: "bpm",
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_statuses",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    status_id = table.Column<long>(type: "bigint", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_statuses_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "bpm",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_statuses_statuses_status_id",
                        column: x => x.status_id,
                        principalSchema: "bpm",
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    employee_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    job_title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    mobile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    placement_node_id = table.Column<long>(type: "bigint", nullable: true),
                    manager_id = table.Column<long>(type: "bigint", nullable: true),
                    hire_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.ForeignKey(
                        name: "fk_employees_structure_nodes_placement_node_id",
                        column: x => x.placement_node_id,
                        principalTable: "structure_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_employees_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<long>(type: "bigint", nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    inserted_by = table.Column<long>(type: "bigint", nullable: true),
                    inserted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_types_code",
                schema: "bpm",
                table: "asset_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assets_asset_type_id",
                schema: "bpm",
                table: "assets",
                column: "asset_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_workspace_id_asset_type_id",
                schema: "bpm",
                table: "assets",
                columns: new[] { "workspace_id", "asset_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_asset_id",
                schema: "bpm",
                table: "documents",
                column: "asset_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_placement_node_id",
                table: "employees",
                column: "placement_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_user_id",
                table: "employees",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_workspace_id_user_id",
                table: "employees",
                columns: new[] { "workspace_id", "user_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_activities_event_id_occurred_at",
                schema: "bpm",
                table: "event_activities",
                columns: new[] { "event_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_event_assets_asset_id",
                schema: "bpm",
                table: "event_assets",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_assets_event_id_asset_id_relation_type",
                schema: "bpm",
                table: "event_assets",
                columns: new[] { "event_id", "asset_id", "relation_type" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_assets_workspace_id_event_id",
                schema: "bpm",
                table: "event_assets",
                columns: new[] { "workspace_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_daily_reports_event_id_report_date",
                schema: "bpm",
                table: "event_daily_reports",
                columns: new[] { "event_id", "report_date" });

            migrationBuilder.CreateIndex(
                name: "ix_event_daily_reports_event_id_report_date_user_id",
                schema: "bpm",
                table: "event_daily_reports",
                columns: new[] { "event_id", "report_date", "user_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_daily_reports_status_id",
                schema: "bpm",
                table: "event_daily_reports",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_dependencies_depends_on_event_id",
                schema: "bpm",
                table: "event_dependencies",
                column: "depends_on_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_dependencies_event_id_depends_on_event_id",
                schema: "bpm",
                table: "event_dependencies",
                columns: new[] { "event_id", "depends_on_event_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_statuses_event_id_is_current",
                schema: "bpm",
                table: "event_statuses",
                columns: new[] { "event_id", "is_current" });

            migrationBuilder.CreateIndex(
                name: "ix_event_statuses_status_id",
                schema: "bpm",
                table: "event_statuses",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_types_code",
                schema: "bpm",
                table: "event_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_event_type_id",
                schema: "bpm",
                table: "events",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_workspace_id_event_type_id",
                schema: "bpm",
                table: "events",
                columns: new[] { "workspace_id", "event_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_mail_templates_workspace_id_code",
                schema: "bpm",
                table: "mail_templates",
                columns: new[] { "workspace_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_notes_asset_id",
                schema: "bpm",
                table: "notes",
                column: "asset_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_token_hash",
                table: "password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_user_id",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_id_permission_id",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_workspace_id_code",
                table: "roles",
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
                name: "ix_send_mails_send_status_next_attempt_at",
                schema: "bpm",
                table: "send_mails",
                columns: new[] { "send_status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_status_types_workspace_id_code",
                schema: "bpm",
                table: "status_types",
                columns: new[] { "workspace_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_statuses_status_type_id",
                schema: "bpm",
                table: "statuses",
                column: "status_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_statuses_workspace_id_status_type_id",
                schema: "bpm",
                table: "statuses",
                columns: new[] { "workspace_id", "status_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_structure_nodes_parent_id",
                table: "structure_nodes",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_structure_nodes_workspace_id_code",
                table: "structure_nodes",
                columns: new[] { "workspace_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_task_events_assignee_id",
                schema: "bpm",
                table: "task_events",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_events_due_at",
                schema: "bpm",
                table: "task_events",
                column: "due_at");

            migrationBuilder.CreateIndex(
                name: "ix_task_events_event_id",
                schema: "bpm",
                table: "task_events",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_events_parent_event_id",
                schema: "bpm",
                table: "task_events",
                column: "parent_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_events_workspace_id_reference_no",
                schema: "bpm",
                table: "task_events",
                columns: new[] { "workspace_id", "reference_no" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_task_settings_workspace_id",
                schema: "bpm",
                table: "task_settings",
                column: "workspace_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_permission_id",
                table: "user_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_user_id_permission_id",
                table: "user_permissions",
                columns: new[] { "user_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id_role_id_cluster_id",
                table: "user_roles",
                columns: new[] { "user_id", "role_id", "cluster_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_workspace_id",
                table: "users",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_workspace_id_normalized_email",
                table: "users",
                columns: new[] { "workspace_id", "normalized_email" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_workspaces_is_deleted",
                table: "workspaces",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_workspaces_slug",
                table: "workspaces",
                column: "slug",
                unique: true);

            // ---- Tenant RLS helper (database-layer isolation, CLAUDE.md §4.1) -------
            // Keyed on bigint workspace ids (company standard: BigInt -> long).
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION erp_enable_tenant_rls(target regclass)
                RETURNS void AS $func$
                DECLARE
                    policy_name text := 'tenant_isolation';
                BEGIN
                    EXECUTE format('ALTER TABLE %s ENABLE ROW LEVEL SECURITY;', target);
                    EXECUTE format('ALTER TABLE %s FORCE ROW LEVEL SECURITY;', target);
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %s;', policy_name, target);
                    EXECUTE format($pol$
                        CREATE POLICY %I ON %s
                        USING (
                            current_setting('app.is_platform_admin', true) = 'true'
                            OR workspace_id = NULLIF(current_setting('app.current_workspace_id', true), '')::bigint
                        )
                        WITH CHECK (
                            current_setting('app.is_platform_admin', true) = 'true'
                            OR workspace_id = NULLIF(current_setting('app.current_workspace_id', true), '')::bigint
                        );
                    $pol$, policy_name, target);
                END;
                $func$ LANGUAGE plpgsql;
                """);

            // ---- Append-only, monthly-partitioned audit_logs (CLAUDE.md §4.3) ------
            // Created by raw SQL (entity is ExcludeFromMigrations). Columns mirror the
            // BaseEntity shape (bigint ids, is_active/is_deleted, inserted_*/changed_*).
            migrationBuilder.Sql("""
                CREATE TABLE audit_logs (
                    id                  bigint       NOT NULL,
                    workspace_id        bigint       NOT NULL,
                    organization_id     bigint       NULL,
                    cluster_id          bigint       NULL,
                    occurred_at         timestamptz  NOT NULL,
                    correlation_id      varchar(64)  NULL,
                    actor_user_id       bigint       NULL,
                    actor_display_name  varchar(256) NULL,
                    module              varchar(50)  NOT NULL,
                    resource_type       varchar(80)  NOT NULL,
                    resource_id         varchar(80)  NULL,
                    action              varchar(40)  NOT NULL,
                    old_values          jsonb        NULL,
                    new_values          jsonb        NULL,
                    ip_address          varchar(64)  NULL,
                    user_agent          varchar(512) NULL,
                    result              varchar(10)  NOT NULL,
                    source              varchar(20)  NOT NULL,
                    reason              varchar(1000) NULL,
                    is_active           boolean      NOT NULL DEFAULT true,
                    is_deleted          boolean      NOT NULL DEFAULT false,
                    inserted_by         bigint       NULL,
                    inserted_date       timestamptz  NOT NULL,
                    changed_by          bigint       NULL,
                    changed_date        timestamptz  NULL,
                    CONSTRAINT pk_audit_logs PRIMARY KEY (id, occurred_at)
                ) PARTITION BY RANGE (occurred_at);

                CREATE INDEX ix_audit_logs_workspace_occurred ON audit_logs (workspace_id, occurred_at DESC);
                CREATE INDEX ix_audit_logs_actor ON audit_logs (actor_user_id);
                CREATE INDEX ix_audit_logs_action ON audit_logs (action);
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION erp_ensure_audit_partition(ts timestamptz)
                RETURNS void AS $func$
                DECLARE
                    start_ts date := date_trunc('month', ts)::date;
                    end_ts   date := (date_trunc('month', ts) + interval '1 month')::date;
                    part     text := 'audit_logs_' || to_char(start_ts, 'YYYY_MM');
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = part) THEN
                        EXECUTE format(
                            'CREATE TABLE %I PARTITION OF audit_logs FOR VALUES FROM (%L) TO (%L);',
                            part, start_ts, end_ts);
                    END IF;
                END;
                $func$ LANGUAGE plpgsql;

                SELECT erp_ensure_audit_partition(now() - interval '1 month');
                SELECT erp_ensure_audit_partition(now());
                SELECT erp_ensure_audit_partition(now() + interval '1 month');
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION erp_block_audit_mutation()
                RETURNS trigger AS $func$
                BEGIN
                    RAISE EXCEPTION 'audit_logs is append-only; % is not permitted', TG_OP;
                END;
                $func$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_audit_logs_no_update
                    BEFORE UPDATE OR DELETE ON audit_logs
                    FOR EACH ROW EXECUTE FUNCTION erp_block_audit_mutation();
                """);

            // ---- Enable RLS on every tenant-owned table ----------------------------
            migrationBuilder.Sql("""
                SELECT erp_enable_tenant_rls('audit_logs');
                SELECT erp_enable_tenant_rls('users');
                SELECT erp_enable_tenant_rls('refresh_tokens');
                SELECT erp_enable_tenant_rls('password_reset_tokens');
                SELECT erp_enable_tenant_rls('roles');
                SELECT erp_enable_tenant_rls('role_permissions');
                SELECT erp_enable_tenant_rls('user_roles');
                SELECT erp_enable_tenant_rls('user_permissions');
                SELECT erp_enable_tenant_rls('workspace_security_policies');
                SELECT erp_enable_tenant_rls('structure_nodes');
                SELECT erp_enable_tenant_rls('employees');
                SELECT erp_enable_tenant_rls('bpm.events');
                SELECT erp_enable_tenant_rls('bpm.task_events');
                SELECT erp_enable_tenant_rls('bpm.event_activities');
                SELECT erp_enable_tenant_rls('bpm.event_dependencies');
                SELECT erp_enable_tenant_rls('bpm.event_daily_reports');
                SELECT erp_enable_tenant_rls('bpm.task_settings');
                SELECT erp_enable_tenant_rls('bpm.status_types');
                SELECT erp_enable_tenant_rls('bpm.statuses');
                SELECT erp_enable_tenant_rls('bpm.event_statuses');
                SELECT erp_enable_tenant_rls('bpm.assets');
                SELECT erp_enable_tenant_rls('bpm.notes');
                SELECT erp_enable_tenant_rls('bpm.documents');
                SELECT erp_enable_tenant_rls('bpm.event_assets');
                SELECT erp_enable_tenant_rls('bpm.send_mails');
                SELECT erp_enable_tenant_rls('bpm.send_mail_recipients');
                SELECT erp_enable_tenant_rls('bpm.send_mail_attempts');
                """);
            // mail_templates, event_types, asset_types (global catalogues),
            // permissions and workspaces (registry) intentionally have NO RLS.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "event_activities",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "event_assets",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "event_daily_reports",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "event_dependencies",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "event_statuses",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "mail_templates",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "send_mail_attempts",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "send_mail_recipients",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "task_events",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "task_settings",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "workspace_security_policies");

            migrationBuilder.DropTable(
                name: "structure_nodes");

            migrationBuilder.DropTable(
                name: "statuses",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "assets",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "send_mails",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "events",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "status_types",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "asset_types",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropTable(
                name: "event_types",
                schema: "bpm");
        }
    }
}
