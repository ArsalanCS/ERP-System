using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Migrations
{
    /// <summary>
    /// Task Management reporting as PostgreSQL functions (company standard: complex
    /// reads via DB functions). Each returns a tabular Row Model that the
    /// TaskReadRepository maps explicitly to a DTO. DataScope is preserved by the
    /// caller passing its visible user-id set (p_all / p_users / p_me); the bodies
    /// also run under RLS (SECURITY INVOKER) and filter by p_ws.
    /// </summary>
    public partial class AddTaskReportFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Scalar KPI summary (filterable). Dashboard passes NULL filters; reports pass them.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION bpm.fn_task_summary(
                    p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamptz,
                    p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean)
                RETURNS TABLE (
                    total int, "open" int, in_progress int, overdue int, due_today int,
                    due_this_week int, high_priority int, completed int, unassigned int,
                    completed_last7 int, reports_today int, avg_completion int,
                    estimated_total numeric, actual_total numeric)
                LANGUAGE sql STABLE AS $fn$
                    WITH t AS (
                        SELECT te.assignee_id, te.due_at, te.completion_percent,
                               te.estimated_time, te.actual_time,
                               COALESCE(st.is_closed,false) AS is_closed,
                               COALESCE(st.is_initial,false) AS is_initial,
                               pr.code AS priority_code, es.inserted_date AS status_since
                        FROM bpm.task_events te
                        JOIN bpm.events ev ON ev.id = te.event_id AND ev.is_deleted = false
                        LEFT JOIN bpm.event_statuses es ON es.event_id = te.event_id AND es.is_current AND es.is_deleted = false
                        LEFT JOIN bpm.statuses st ON st.id = es.status_id
                        LEFT JOIN bpm.statuses pr ON pr.id = te.priority_status_id
                        WHERE te.is_deleted = false AND te.workspace_id = p_ws
                          AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
                          AND (p_status IS NULL OR st.id = p_status)
                          AND (p_priority IS NULL OR te.priority_status_id = p_priority)
                          AND (NOT COALESCE(p_overdue,false) OR (te.due_at < p_now AND COALESCE(st.is_closed,false) = false))
                          AND (NOT COALESCE(p_closed,false) OR COALESCE(st.is_closed,false) = true)
                    )
                    SELECT
                        count(*)::int,
                        count(*) FILTER (WHERE NOT is_closed)::int,
                        count(*) FILTER (WHERE NOT is_closed AND NOT is_initial)::int,
                        count(*) FILTER (WHERE NOT is_closed AND due_at < p_now)::int,
                        count(*) FILTER (WHERE NOT is_closed AND (due_at AT TIME ZONE 'UTC')::date = (p_now AT TIME ZONE 'UTC')::date)::int,
                        count(*) FILTER (WHERE NOT is_closed AND due_at >= p_now AND due_at <= p_now + interval '7 day')::int,
                        count(*) FILTER (WHERE NOT is_closed AND priority_code IN ('HIGH','CRITICAL'))::int,
                        count(*) FILTER (WHERE is_closed)::int,
                        count(*) FILTER (WHERE NOT is_closed AND assignee_id IS NULL)::int,
                        count(*) FILTER (WHERE is_closed AND status_since >= p_now - interval '7 day')::int,
                        (SELECT count(*) FROM bpm.event_daily_reports dr
                         JOIN bpm.task_events te2 ON te2.event_id = dr.event_id
                         WHERE dr.is_deleted = false AND te2.workspace_id = p_ws
                           AND dr.report_date = (p_now AT TIME ZONE 'UTC')::date
                           AND (p_all OR te2.assignee_id = ANY(p_users) OR te2.reporter_id = p_me))::int,
                        COALESCE(round(avg(completion_percent)),0)::int,
                        COALESCE(sum(estimated_time),0),
                        COALESCE(sum(actual_time),0)
                    FROM t;
                $fn$;
                """);

            // Status / priority breakdowns (filterable).
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION bpm.fn_task_status_breakdown(
                    p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamptz,
                    p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean)
                RETURNS TABLE (id bigint, name text, color text, count int)
                LANGUAGE sql STABLE AS $fn$
                    SELECT st.id, st.name, st.color, count(*)::int
                    FROM bpm.task_events te
                    JOIN bpm.events ev ON ev.id = te.event_id AND ev.is_deleted = false
                    LEFT JOIN bpm.event_statuses es ON es.event_id = te.event_id AND es.is_current AND es.is_deleted = false
                    LEFT JOIN bpm.statuses st ON st.id = es.status_id
                    WHERE te.is_deleted = false AND te.workspace_id = p_ws
                      AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
                      AND (p_status IS NULL OR st.id = p_status)
                      AND (p_priority IS NULL OR te.priority_status_id = p_priority)
                      AND (NOT COALESCE(p_overdue,false) OR (te.due_at < p_now AND COALESCE(st.is_closed,false) = false))
                      AND (NOT COALESCE(p_closed,false) OR COALESCE(st.is_closed,false) = true)
                    GROUP BY st.id, st.name, st.color
                    ORDER BY count(*) DESC;
                $fn$;

                CREATE OR REPLACE FUNCTION bpm.fn_task_priority_breakdown(
                    p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamptz,
                    p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean)
                RETURNS TABLE (id bigint, name text, color text, count int)
                LANGUAGE sql STABLE AS $fn$
                    SELECT pr.id, pr.name, pr.color, count(*)::int
                    FROM bpm.task_events te
                    JOIN bpm.events ev ON ev.id = te.event_id AND ev.is_deleted = false
                    LEFT JOIN bpm.event_statuses es ON es.event_id = te.event_id AND es.is_current AND es.is_deleted = false
                    LEFT JOIN bpm.statuses st ON st.id = es.status_id
                    LEFT JOIN bpm.statuses pr ON pr.id = te.priority_status_id
                    WHERE te.is_deleted = false AND te.workspace_id = p_ws
                      AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
                      AND (p_status IS NULL OR st.id = p_status)
                      AND (p_priority IS NULL OR te.priority_status_id = p_priority)
                      AND (NOT COALESCE(p_overdue,false) OR (te.due_at < p_now AND COALESCE(st.is_closed,false) = false))
                      AND (NOT COALESCE(p_closed,false) OR COALESCE(st.is_closed,false) = true)
                    GROUP BY pr.id, pr.name, pr.color
                    ORDER BY count(*) DESC;
                $fn$;
                """);

            // Assignee workload (filterable).
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION bpm.fn_task_assignee_load(
                    p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamptz,
                    p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean)
                RETURNS TABLE (assignee_id bigint, assignee_name text, "open" int, overdue int)
                LANGUAGE sql STABLE AS $fn$
                    SELECT te.assignee_id, au.display_name,
                           count(*) FILTER (WHERE NOT COALESCE(st.is_closed,false))::int,
                           count(*) FILTER (WHERE NOT COALESCE(st.is_closed,false) AND te.due_at < p_now)::int
                    FROM bpm.task_events te
                    JOIN bpm.events ev ON ev.id = te.event_id AND ev.is_deleted = false
                    LEFT JOIN bpm.event_statuses es ON es.event_id = te.event_id AND es.is_current AND es.is_deleted = false
                    LEFT JOIN bpm.statuses st ON st.id = es.status_id
                    LEFT JOIN public.users au ON au.id = te.assignee_id
                    WHERE te.is_deleted = false AND te.workspace_id = p_ws
                      AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
                      AND (p_status IS NULL OR st.id = p_status)
                      AND (p_priority IS NULL OR te.priority_status_id = p_priority)
                      AND (NOT COALESCE(p_overdue,false) OR (te.due_at < p_now AND COALESCE(st.is_closed,false) = false))
                      AND (NOT COALESCE(p_closed,false) OR COALESCE(st.is_closed,false) = true)
                    GROUP BY te.assignee_id, au.display_name
                    ORDER BY count(*) FILTER (WHERE NOT COALESCE(st.is_closed,false)) DESC;
                $fn$;
                """);

            // 14-day created/completed trend.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION bpm.fn_task_trend(
                    p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_from date, p_to date)
                RETURNS TABLE (day date, created int, completed int)
                LANGUAGE sql STABLE AS $fn$
                    WITH days AS (SELECT generate_series(p_from, p_to, interval '1 day')::date AS d),
                    t AS (
                        SELECT (te.inserted_date AT TIME ZONE 'UTC')::date AS created_day,
                               CASE WHEN COALESCE(st.is_closed,false)
                                    THEN (es.inserted_date AT TIME ZONE 'UTC')::date END AS completed_day
                        FROM bpm.task_events te
                        JOIN bpm.events ev ON ev.id = te.event_id AND ev.is_deleted = false
                        LEFT JOIN bpm.event_statuses es ON es.event_id = te.event_id AND es.is_current AND es.is_deleted = false
                        LEFT JOIN bpm.statuses st ON st.id = es.status_id
                        WHERE te.is_deleted = false AND te.workspace_id = p_ws
                          AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
                    )
                    SELECT d,
                           (SELECT count(*) FROM t WHERE t.created_day = d)::int,
                           (SELECT count(*) FROM t WHERE t.completed_day = d)::int
                    FROM days ORDER BY d;
                $fn$;
                """);

            // Recent activity feed.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION bpm.fn_task_recent_activity(
                    p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_limit int)
                RETURNS TABLE (id bigint, event_id bigint, reference_no text, message text,
                               actor_id bigint, actor_name text, occurred_at timestamptz)
                LANGUAGE sql STABLE AS $fn$
                    SELECT a.id, a.event_id, te.reference_no, a.message, a.actor_id, u.display_name, a.occurred_at
                    FROM bpm.event_activities a
                    JOIN bpm.task_events te ON te.event_id = a.event_id AND te.is_deleted = false
                    LEFT JOIN public.users u ON u.id = a.actor_id
                    WHERE a.is_deleted = false AND te.workspace_id = p_ws
                      AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
                    ORDER BY a.occurred_at DESC
                    LIMIT p_limit;
                $fn$;
                """);

            // Open-task schedule (mini-gantt).
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION bpm.fn_task_gantt(
                    p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_limit int)
                RETURNS TABLE (event_id bigint, reference_no text, title text, start_at timestamptz,
                               due_at timestamptz, completion_percent int, status_color text, is_closed boolean)
                LANGUAGE sql STABLE AS $fn$
                    SELECT te.event_id, te.reference_no, te.title, te.start_at, te.due_at,
                           te.completion_percent, st.color, COALESCE(st.is_closed,false)
                    FROM bpm.task_events te
                    JOIN bpm.events ev ON ev.id = te.event_id AND ev.is_deleted = false
                    LEFT JOIN bpm.event_statuses es ON es.event_id = te.event_id AND es.is_current AND es.is_deleted = false
                    LEFT JOIN bpm.statuses st ON st.id = es.status_id
                    WHERE te.is_deleted = false AND te.workspace_id = p_ws
                      AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
                      AND COALESCE(st.is_closed,false) = false
                      AND (te.start_at IS NOT NULL OR te.due_at IS NOT NULL)
                    ORDER BY COALESCE(te.start_at, te.due_at)
                    LIMIT p_limit;
                $fn$;
                """);

            // Workspace-wide daily reports (paged; total via window count).
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION bpm.fn_task_daily_reports(
                    p_ws bigint, p_all boolean, p_users bigint[], p_me bigint,
                    p_from date, p_to date, p_author bigint, p_status bigint, p_offset int, p_limit int)
                RETURNS TABLE (id bigint, event_id bigint, reference_no text, task_title text,
                               report_date date, description text, estimated_time numeric,
                               actual_time numeric, remaining_time numeric, status_id bigint,
                               status_name text, status_color text, author_id bigint,
                               author_name text, created_at timestamptz, total bigint)
                LANGUAGE sql STABLE AS $fn$
                    SELECT dr.id, dr.event_id, te.reference_no, te.title, dr.report_date, dr.description,
                           dr.estimated_time, dr.actual_time, dr.remaining_time, dr.status_id,
                           st.name, st.color, dr.user_id, u.display_name, dr.inserted_date,
                           count(*) OVER()
                    FROM bpm.event_daily_reports dr
                    JOIN bpm.task_events te ON te.event_id = dr.event_id AND te.is_deleted = false
                    LEFT JOIN bpm.statuses st ON st.id = dr.status_id
                    LEFT JOIN public.users u ON u.id = dr.user_id
                    WHERE dr.is_deleted = false AND te.workspace_id = p_ws
                      AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
                      AND (p_from IS NULL OR dr.report_date >= p_from)
                      AND (p_to IS NULL OR dr.report_date <= p_to)
                      AND (p_author IS NULL OR dr.user_id = p_author)
                      AND (p_status IS NULL OR dr.status_id = p_status)
                    ORDER BY dr.report_date DESC, dr.inserted_date DESC
                    OFFSET p_offset LIMIT p_limit;
                $fn$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var fn in new[]
                     {
                         "bpm.fn_task_summary(bigint,boolean,bigint[],bigint,timestamptz,bigint,bigint,boolean,boolean)",
                         "bpm.fn_task_status_breakdown(bigint,boolean,bigint[],bigint,timestamptz,bigint,bigint,boolean,boolean)",
                         "bpm.fn_task_priority_breakdown(bigint,boolean,bigint[],bigint,timestamptz,bigint,bigint,boolean,boolean)",
                         "bpm.fn_task_assignee_load(bigint,boolean,bigint[],bigint,timestamptz,bigint,bigint,boolean,boolean)",
                         "bpm.fn_task_trend(bigint,boolean,bigint[],bigint,date,date)",
                         "bpm.fn_task_recent_activity(bigint,boolean,bigint[],bigint,integer)",
                         "bpm.fn_task_gantt(bigint,boolean,bigint[],bigint,integer)",
                         "bpm.fn_task_daily_reports(bigint,boolean,bigint[],bigint,date,date,bigint,bigint,integer,integer)",
                     })
            {
                migrationBuilder.Sql($"DROP FUNCTION IF EXISTS {fn};");
            }
        }
    }
}
