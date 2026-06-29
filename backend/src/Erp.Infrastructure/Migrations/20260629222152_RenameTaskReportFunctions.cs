using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTaskReportFunctions : Migration
    {
        // Aligns Task read-model function names with the Dashboard/Reports/Settings
        // documentation §8.1. Bodies are unchanged; only the names change.
        private const string Args = "bigint, boolean, bigint[], bigint, timestamp with time zone, bigint, bigint, boolean, boolean";
        private const string DailyArgs = "bigint, boolean, bigint[], bigint, date, date, bigint, bigint, integer, integer";
        private const string GanttArgs = "bigint, boolean, bigint[], bigint, integer";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_summary({Args}) RENAME TO fn_task_dashboard_summary;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_status_breakdown({Args}) RENAME TO fn_task_status_summary;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_priority_breakdown({Args}) RENAME TO fn_task_priority_summary;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_assignee_load({Args}) RENAME TO fn_task_assignee_workload;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_daily_reports({DailyArgs}) RENAME TO fn_task_daily_report_summary;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_gantt({GanttArgs}) RENAME TO fn_task_gantt_list;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_dashboard_summary({Args}) RENAME TO fn_task_summary;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_status_summary({Args}) RENAME TO fn_task_status_breakdown;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_priority_summary({Args}) RENAME TO fn_task_priority_breakdown;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_assignee_workload({Args}) RENAME TO fn_task_assignee_load;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_daily_report_summary({DailyArgs}) RENAME TO fn_task_daily_reports;");
            migrationBuilder.Sql($"ALTER FUNCTION bpm.fn_task_gantt_list({GanttArgs}) RENAME TO fn_task_gantt;");
        }
    }
}
