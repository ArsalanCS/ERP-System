using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTasksWithEventAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_activities");

            migrationBuilder.DropTable(
                name: "task_checklist_items");

            migrationBuilder.DropTable(
                name: "task_dependencies");

            migrationBuilder.DropTable(
                name: "task_documents");

            migrationBuilder.DropTable(
                name: "task_notes");

            migrationBuilder.DropTable(
                name: "task_relations");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "task_statuses");

            migrationBuilder.DropTable(
                name: "task_status_types");

            migrationBuilder.EnsureSchema(
                name: "bpm");

            migrationBuilder.CreateTable(
                name: "asset_types",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status_types",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("pk_status_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "assets",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "statuses",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_initial = table.Column<bool>(type: "boolean", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
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
                name: "documents",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    is_internal = table.Column<bool>(type: "boolean", nullable: false),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    from_status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_status_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depends_on_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    priority_status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estimated_time = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    actual_time = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    completion_percent = table.Column<int>(type: "integer", nullable: false),
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
                name: "event_statuses",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
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
                name: "ix_notes_asset_id",
                schema: "bpm",
                table: "notes",
                column: "asset_id",
                unique: true);

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

            // Two-layer tenant isolation: enable Postgres RLS on every tenant-owned bpm table.
            // event_types and asset_types are global lookups (no workspace_id) and are excluded.
            foreach (var table in new[]
            {
                "bpm.events", "bpm.task_events", "bpm.event_activities", "bpm.event_dependencies",
                "bpm.status_types", "bpm.statuses", "bpm.event_statuses",
                "bpm.assets", "bpm.notes", "bpm.documents", "bpm.event_assets",
            })
            {
                migrationBuilder.Sql($"SELECT erp_enable_tenant_rls('{table}');");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "event_activities",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "event_assets",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "event_dependencies",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "event_statuses",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "task_events",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "statuses",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "assets",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "events",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "status_types",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "asset_types",
                schema: "bpm");

            migrationBuilder.DropTable(
                name: "event_types",
                schema: "bpm");

            migrationBuilder.CreateTable(
                name: "task_status_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_status_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_final = table.Column<bool>(type: "boolean", nullable: false),
                    is_initial = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    status_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_statuses_task_status_types_status_type_id",
                        column: x => x.status_type_id,
                        principalTable: "task_status_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actual_hours = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completion_percent = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estimated_hours = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    event_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    parent_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reminder_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_type = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_tasks_task_status_types_status_type_id",
                        column: x => x.status_type_id,
                        principalTable: "task_status_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tasks_task_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "task_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tasks_tasks_parent_task_id",
                        column: x => x.parent_task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    from_status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_activities_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_checklist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_done = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_checklist_items_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_dependencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    dependency_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    depends_on_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_dependencies_tasks_depends_on_task_id",
                        column: x => x.depends_on_task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_task_dependencies_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_documents_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_internal = table.Column<bool>(type: "boolean", nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_notes_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_relations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_entity_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_relations", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_relations_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_task_activities_task_id",
                table: "task_activities",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_activities_workspace_id_task_id",
                table: "task_activities",
                columns: new[] { "workspace_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_checklist_items_task_id",
                table: "task_checklist_items",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_checklist_items_workspace_id_task_id",
                table: "task_checklist_items",
                columns: new[] { "workspace_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_dependencies_depends_on_task_id",
                table: "task_dependencies",
                column: "depends_on_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_dependencies_task_id_depends_on_task_id",
                table: "task_dependencies",
                columns: new[] { "task_id", "depends_on_task_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_task_dependencies_workspace_id_task_id",
                table: "task_dependencies",
                columns: new[] { "workspace_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_documents_task_id",
                table: "task_documents",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_documents_workspace_id_task_id",
                table: "task_documents",
                columns: new[] { "workspace_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_notes_task_id",
                table: "task_notes",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_notes_workspace_id_task_id",
                table: "task_notes",
                columns: new[] { "workspace_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_relations_task_id_related_entity_type_related_entity_i",
                table: "task_relations",
                columns: new[] { "task_id", "related_entity_type", "related_entity_id", "role" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_task_relations_workspace_id_task_id",
                table: "task_relations",
                columns: new[] { "workspace_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_status_types_workspace_id_name",
                table: "task_status_types",
                columns: new[] { "workspace_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_task_statuses_status_type_id",
                table: "task_statuses",
                column: "status_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_statuses_workspace_id_status_type_id",
                table: "task_statuses",
                columns: new[] { "workspace_id", "status_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_parent_task_id",
                table: "tasks",
                column: "parent_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_status_id",
                table: "tasks",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_status_type_id",
                table: "tasks",
                column: "status_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_workspace_id_assignee_id",
                table: "tasks",
                columns: new[] { "workspace_id", "assignee_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_workspace_id_due_date",
                table: "tasks",
                columns: new[] { "workspace_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_workspace_id_status_id",
                table: "tasks",
                columns: new[] { "workspace_id", "status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_workspace_id_task_number",
                table: "tasks",
                columns: new[] { "workspace_id", "task_number" },
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
