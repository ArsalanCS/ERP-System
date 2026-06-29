using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventDailyReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_daily_reports",
                schema: "bpm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_date = table.Column<DateOnly>(type: "date", nullable: false),
                    summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    hours_spent = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    completion_percent = table.Column<int>(type: "integer", nullable: true),
                    blockers = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_event_daily_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_daily_reports_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "bpm",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_daily_reports_event_id_report_date",
                schema: "bpm",
                table: "event_daily_reports",
                columns: new[] { "event_id", "report_date" });

            migrationBuilder.CreateIndex(
                name: "ix_event_daily_reports_event_id_report_date_author_id",
                schema: "bpm",
                table: "event_daily_reports",
                columns: new[] { "event_id", "report_date", "author_id" },
                unique: true,
                filter: "is_deleted = false");

            // Two-layer tenant isolation: enable Postgres RLS on the new tenant-owned table.
            migrationBuilder.Sql("SELECT erp_enable_tenant_rls('bpm.event_daily_reports');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_daily_reports",
                schema: "bpm");
        }
    }
}
