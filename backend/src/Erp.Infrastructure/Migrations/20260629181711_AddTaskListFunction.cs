using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Migrations
{
    /// <summary>
    /// Heavy task list as a PostgreSQL function (Refactor Guide §10). Returns the
    /// TaskListItemDto columns plus a window total_count for paging. DataScope is
    /// preserved via p_all / p_users / p_me; filters mirror TaskListQuery.
    /// </summary>
    public partial class AddTaskListFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION bpm.get_task_list(
                    p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamptz,
                    p_search text, p_status bigint, p_priority bigint, p_assignee bigint,
                    p_overdue boolean, p_closed boolean, p_parent bigint, p_offset int, p_limit int)
                RETURNS TABLE (
                    event_id bigint, reference_no text, title text,
                    status_id bigint, status_name text, status_color text, status_is_closed boolean,
                    priority_status_id bigint, priority_name text, priority_color text,
                    assignee_id bigint, assignee_name text, due_at timestamptz, is_overdue boolean,
                    completion_percent int, created_at timestamptz, total_count int)
                LANGUAGE sql STABLE AS $fn$
                    SELECT
                        te.event_id, te.reference_no, te.title,
                        st.id, st.name, st.color, COALESCE(st.is_closed,false),
                        te.priority_status_id, pr.name, pr.color,
                        te.assignee_id, au.display_name, te.due_at,
                        (te.due_at IS NOT NULL AND te.due_at < p_now AND COALESCE(st.is_closed,false) = false),
                        te.completion_percent, te.inserted_date,
                        count(*) OVER()::int
                    FROM bpm.task_events te
                    JOIN bpm.events ev ON ev.id = te.event_id AND ev.is_deleted = false
                    LEFT JOIN bpm.event_statuses es ON es.event_id = te.event_id AND es.is_current AND es.is_deleted = false
                    LEFT JOIN bpm.statuses st ON st.id = es.status_id
                    LEFT JOIN bpm.statuses pr ON pr.id = te.priority_status_id
                    LEFT JOIN public.users au ON au.id = te.assignee_id
                    WHERE te.is_deleted = false AND te.workspace_id = p_ws
                      AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
                      AND (p_search IS NULL OR te.title ILIKE '%' || p_search || '%' OR te.reference_no ILIKE '%' || p_search || '%')
                      AND (p_status IS NULL OR st.id = p_status)
                      AND (p_priority IS NULL OR te.priority_status_id = p_priority)
                      AND (p_assignee IS NULL OR te.assignee_id = p_assignee)
                      AND (p_parent IS NULL OR te.parent_event_id = p_parent)
                      AND (NOT COALESCE(p_overdue,false) OR (te.due_at < p_now AND COALESCE(st.is_closed,false) = false))
                      AND (NOT COALESCE(p_closed,false) OR COALESCE(st.is_closed,false) = true)
                    ORDER BY te.inserted_date DESC
                    OFFSET p_offset LIMIT p_limit;
                $fn$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS bpm.get_task_list(bigint,boolean,bigint[],bigint,timestamptz,text,bigint,bigint,bigint,boolean,boolean,bigint,integer,integer);");
        }
    }
}
