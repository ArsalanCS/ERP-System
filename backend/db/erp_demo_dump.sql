--
-- PostgreSQL database dump
--

\restrict M8lDLGObbkNH5XNlfU6PKBsgL3ma9OXknOg73teaJH0LbPMU4vA6TDNU43ptyIv

-- Dumped from database version 16.14 (Homebrew)
-- Dumped by pg_dump version 16.14 (Homebrew)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: bpm; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA bpm;


--
-- Name: fn_task_assignee_workload(bigint, boolean, bigint[], bigint, timestamp with time zone, bigint, bigint, boolean, boolean); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_assignee_workload(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamp with time zone, p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean) RETURNS TABLE(assignee_id bigint, assignee_name text, open integer, overdue integer)
    LANGUAGE sql STABLE
    AS $$
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
$$;


--
-- Name: fn_task_daily_report_summary(bigint, boolean, bigint[], bigint, date, date, bigint, bigint, integer, integer); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_daily_report_summary(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_from date, p_to date, p_author bigint, p_status bigint, p_offset integer, p_limit integer) RETURNS TABLE(id bigint, event_id bigint, reference_no text, task_title text, report_date date, description text, estimated_time numeric, actual_time numeric, remaining_time numeric, status_id bigint, status_name text, status_color text, author_id bigint, author_name text, created_at timestamp with time zone, total bigint)
    LANGUAGE sql STABLE
    AS $$
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
$$;


--
-- Name: fn_task_dashboard_summary(bigint, boolean, bigint[], bigint, timestamp with time zone, bigint, bigint, boolean, boolean); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_dashboard_summary(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamp with time zone, p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean) RETURNS TABLE(total integer, open integer, in_progress integer, overdue integer, due_today integer, due_this_week integer, high_priority integer, completed integer, unassigned integer, completed_last7 integer, reports_today integer, avg_completion integer, estimated_total numeric, actual_total numeric)
    LANGUAGE sql STABLE
    AS $$
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
$$;


--
-- Name: fn_task_gantt_list(bigint, boolean, bigint[], bigint, integer); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_gantt_list(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_limit integer) RETURNS TABLE(event_id bigint, reference_no text, title text, start_at timestamp with time zone, due_at timestamp with time zone, completion_percent integer, status_color text, is_closed boolean)
    LANGUAGE sql STABLE
    AS $$
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
$$;


--
-- Name: fn_task_priority_summary(bigint, boolean, bigint[], bigint, timestamp with time zone, bigint, bigint, boolean, boolean); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_priority_summary(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamp with time zone, p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean) RETURNS TABLE(id bigint, name text, color text, count integer)
    LANGUAGE sql STABLE
    AS $$
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
$$;


--
-- Name: fn_task_recent_activity(bigint, boolean, bigint[], bigint, integer); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_recent_activity(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_limit integer) RETURNS TABLE(id bigint, event_id bigint, reference_no text, message text, actor_id bigint, actor_name text, occurred_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT a.id, a.event_id, te.reference_no, a.message, a.actor_id, u.display_name, a.occurred_at
    FROM bpm.event_activities a
    JOIN bpm.task_events te ON te.event_id = a.event_id AND te.is_deleted = false
    LEFT JOIN public.users u ON u.id = a.actor_id
    WHERE a.is_deleted = false AND te.workspace_id = p_ws
      AND (p_all OR te.assignee_id = ANY(p_users) OR te.reporter_id = p_me)
    ORDER BY a.occurred_at DESC
    LIMIT p_limit;
$$;


--
-- Name: fn_task_status_summary(bigint, boolean, bigint[], bigint, timestamp with time zone, bigint, bigint, boolean, boolean); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_status_summary(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamp with time zone, p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean) RETURNS TABLE(id bigint, name text, color text, count integer)
    LANGUAGE sql STABLE
    AS $$
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
$$;


--
-- Name: fn_task_trend(bigint, boolean, bigint[], bigint, date, date); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_trend(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_from date, p_to date) RETURNS TABLE(day date, created integer, completed integer)
    LANGUAGE sql STABLE
    AS $$
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
$$;


--
-- Name: get_task_list(bigint, boolean, bigint[], bigint, timestamp with time zone, text, bigint, bigint, bigint, boolean, boolean, bigint, integer, integer); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.get_task_list(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamp with time zone, p_search text, p_status bigint, p_priority bigint, p_assignee bigint, p_overdue boolean, p_closed boolean, p_parent bigint, p_offset integer, p_limit integer) RETURNS TABLE(event_id bigint, reference_no text, title text, status_id bigint, status_name text, status_color text, status_is_closed boolean, priority_status_id bigint, priority_name text, priority_color text, assignee_id bigint, assignee_name text, due_at timestamp with time zone, is_overdue boolean, completion_percent integer, created_at timestamp with time zone, total_count integer)
    LANGUAGE sql STABLE
    AS $$
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
$$;


--
-- Name: erp_block_audit_mutation(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.erp_block_audit_mutation() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    RAISE EXCEPTION 'audit_logs is append-only; % is not permitted', TG_OP;
END;
$$;


--
-- Name: erp_enable_tenant_rls(regclass); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.erp_enable_tenant_rls(target regclass) RETURNS void
    LANGUAGE plpgsql
    AS $_$
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
$_$;


--
-- Name: erp_ensure_audit_partition(timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.erp_ensure_audit_partition(ts timestamp with time zone) RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: asset_types; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.asset_types (
    id bigint NOT NULL,
    code character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone
);


--
-- Name: assets; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.assets (
    id bigint NOT NULL,
    asset_type_id bigint NOT NULL,
    name character varying(300),
    code character varying(100),
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.assets FORCE ROW LEVEL SECURITY;


--
-- Name: documents; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.documents (
    id bigint NOT NULL,
    asset_id bigint NOT NULL,
    file_name character varying(300) NOT NULL,
    file_path text NOT NULL,
    mime_type character varying(150),
    file_size bigint,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.documents FORCE ROW LEVEL SECURITY;


--
-- Name: event_activities; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.event_activities (
    id bigint NOT NULL,
    event_id bigint NOT NULL,
    kind character varying(30) NOT NULL,
    message character varying(500) NOT NULL,
    actor_id bigint,
    occurred_at timestamp with time zone NOT NULL,
    from_status_id bigint,
    to_status_id bigint,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.event_activities FORCE ROW LEVEL SECURITY;


--
-- Name: event_assets; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.event_assets (
    id bigint NOT NULL,
    event_id bigint NOT NULL,
    asset_id bigint NOT NULL,
    relation_type character varying(100) NOT NULL,
    description text,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.event_assets FORCE ROW LEVEL SECURITY;


--
-- Name: event_daily_reports; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.event_daily_reports (
    id bigint NOT NULL,
    event_id bigint NOT NULL,
    user_id bigint,
    report_date date NOT NULL,
    description text NOT NULL,
    estimated_time numeric(9,2),
    actual_time numeric(9,2),
    remaining_time numeric(9,2),
    status_id bigint,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.event_daily_reports FORCE ROW LEVEL SECURITY;


--
-- Name: event_dependencies; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.event_dependencies (
    id bigint NOT NULL,
    event_id bigint NOT NULL,
    depends_on_event_id bigint NOT NULL,
    is_blocking boolean NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.event_dependencies FORCE ROW LEVEL SECURITY;


--
-- Name: event_statuses; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.event_statuses (
    id bigint NOT NULL,
    event_id bigint NOT NULL,
    status_id bigint NOT NULL,
    is_current boolean NOT NULL,
    note text,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.event_statuses FORCE ROW LEVEL SECURITY;


--
-- Name: event_types; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.event_types (
    id bigint NOT NULL,
    code character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone
);


--
-- Name: events; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.events (
    id bigint NOT NULL,
    event_type_id bigint NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.events FORCE ROW LEVEL SECURITY;


--
-- Name: mail_templates; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.mail_templates (
    id bigint NOT NULL,
    workspace_id bigint,
    code character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    subject_template character varying(300) NOT NULL,
    body_html_template text NOT NULL,
    body_text_template text,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone
);


--
-- Name: notes; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.notes (
    id bigint NOT NULL,
    asset_id bigint NOT NULL,
    body text NOT NULL,
    is_pinned boolean NOT NULL,
    is_internal boolean NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.notes FORCE ROW LEVEL SECURITY;


--
-- Name: send_mail_attempts; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.send_mail_attempts (
    id bigint NOT NULL,
    send_mail_id bigint NOT NULL,
    attempt_no integer NOT NULL,
    success boolean NOT NULL,
    provider_response text,
    error_message character varying(2000),
    attempted_at timestamp with time zone NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.send_mail_attempts FORCE ROW LEVEL SECURITY;


--
-- Name: send_mail_recipients; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.send_mail_recipients (
    id bigint NOT NULL,
    send_mail_id bigint NOT NULL,
    address character varying(320) NOT NULL,
    display_name character varying(200),
    recipient_type character varying(10) NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.send_mail_recipients FORCE ROW LEVEL SECURITY;


--
-- Name: send_mails; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.send_mails (
    id bigint NOT NULL,
    mail_template_id bigint,
    template_code character varying(100),
    subject character varying(300) NOT NULL,
    body_html text NOT NULL,
    body_text text,
    template_data_json jsonb,
    send_status character varying(20) NOT NULL,
    scheduled_at timestamp with time zone NOT NULL,
    next_attempt_at timestamp with time zone,
    sent_at timestamp with time zone,
    retry_count integer NOT NULL,
    max_retries integer NOT NULL,
    last_error character varying(2000),
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.send_mails FORCE ROW LEVEL SECURITY;


--
-- Name: status_types; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.status_types (
    id bigint NOT NULL,
    code character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.status_types FORCE ROW LEVEL SECURITY;


--
-- Name: statuses; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.statuses (
    id bigint NOT NULL,
    status_type_id bigint NOT NULL,
    code character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    sort_order integer NOT NULL,
    is_initial boolean NOT NULL,
    is_closed boolean NOT NULL,
    color character varying(30),
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.statuses FORCE ROW LEVEL SECURITY;


--
-- Name: task_events; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.task_events (
    id bigint NOT NULL,
    event_id bigint NOT NULL,
    reference_no character varying(50) NOT NULL,
    title character varying(300) NOT NULL,
    description text,
    assignee_id bigint,
    reporter_id bigint,
    parent_event_id bigint,
    priority_status_id bigint,
    start_at timestamp with time zone,
    due_at timestamp with time zone,
    estimated_time numeric(9,2),
    actual_time numeric(9,2),
    completion_percent integer NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.task_events FORCE ROW LEVEL SECURITY;


--
-- Name: task_settings; Type: TABLE; Schema: bpm; Owner: -
--

CREATE TABLE bpm.task_settings (
    id bigint NOT NULL,
    daily_report_required boolean NOT NULL,
    allow_status_change_from_report boolean NOT NULL,
    require_actual_time boolean NOT NULL,
    require_estimated_time boolean NOT NULL,
    allow_multiple_reports_per_day boolean NOT NULL,
    notify_on_task_created boolean NOT NULL,
    notify_on_task_assigned boolean NOT NULL,
    notify_on_status_change boolean NOT NULL,
    notify_on_daily_report boolean NOT NULL,
    dashboard_default_range_days integer NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY bpm.task_settings FORCE ROW LEVEL SECURITY;


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL
);


--
-- Name: audit_logs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.audit_logs (
    id bigint NOT NULL,
    workspace_id bigint NOT NULL,
    organization_id bigint,
    cluster_id bigint,
    occurred_at timestamp with time zone NOT NULL,
    correlation_id character varying(64),
    actor_user_id bigint,
    actor_display_name character varying(256),
    module character varying(50) NOT NULL,
    resource_type character varying(80) NOT NULL,
    resource_id character varying(80),
    action character varying(40) NOT NULL,
    old_values jsonb,
    new_values jsonb,
    ip_address character varying(64),
    user_agent character varying(512),
    result character varying(10) NOT NULL,
    source character varying(20) NOT NULL,
    reason character varying(1000),
    is_active boolean DEFAULT true NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone
)
PARTITION BY RANGE (occurred_at);

ALTER TABLE ONLY public.audit_logs FORCE ROW LEVEL SECURITY;


--
-- Name: audit_logs_2026_05; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.audit_logs_2026_05 (
    id bigint NOT NULL,
    workspace_id bigint NOT NULL,
    organization_id bigint,
    cluster_id bigint,
    occurred_at timestamp with time zone NOT NULL,
    correlation_id character varying(64),
    actor_user_id bigint,
    actor_display_name character varying(256),
    module character varying(50) NOT NULL,
    resource_type character varying(80) NOT NULL,
    resource_id character varying(80),
    action character varying(40) NOT NULL,
    old_values jsonb,
    new_values jsonb,
    ip_address character varying(64),
    user_agent character varying(512),
    result character varying(10) NOT NULL,
    source character varying(20) NOT NULL,
    reason character varying(1000),
    is_active boolean DEFAULT true NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone
);


--
-- Name: audit_logs_2026_06; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.audit_logs_2026_06 (
    id bigint NOT NULL,
    workspace_id bigint NOT NULL,
    organization_id bigint,
    cluster_id bigint,
    occurred_at timestamp with time zone NOT NULL,
    correlation_id character varying(64),
    actor_user_id bigint,
    actor_display_name character varying(256),
    module character varying(50) NOT NULL,
    resource_type character varying(80) NOT NULL,
    resource_id character varying(80),
    action character varying(40) NOT NULL,
    old_values jsonb,
    new_values jsonb,
    ip_address character varying(64),
    user_agent character varying(512),
    result character varying(10) NOT NULL,
    source character varying(20) NOT NULL,
    reason character varying(1000),
    is_active boolean DEFAULT true NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone
);


--
-- Name: audit_logs_2026_07; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.audit_logs_2026_07 (
    id bigint NOT NULL,
    workspace_id bigint NOT NULL,
    organization_id bigint,
    cluster_id bigint,
    occurred_at timestamp with time zone NOT NULL,
    correlation_id character varying(64),
    actor_user_id bigint,
    actor_display_name character varying(256),
    module character varying(50) NOT NULL,
    resource_type character varying(80) NOT NULL,
    resource_id character varying(80),
    action character varying(40) NOT NULL,
    old_values jsonb,
    new_values jsonb,
    ip_address character varying(64),
    user_agent character varying(512),
    result character varying(10) NOT NULL,
    source character varying(20) NOT NULL,
    reason character varying(1000),
    is_active boolean DEFAULT true NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone
);


--
-- Name: employees; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.employees (
    id bigint NOT NULL,
    user_id bigint NOT NULL,
    employee_number character varying(40),
    job_title character varying(150),
    mobile character varying(32),
    placement_node_id bigint,
    manager_id bigint,
    hire_date timestamp with time zone,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.employees FORCE ROW LEVEL SECURITY;


--
-- Name: password_reset_tokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.password_reset_tokens (
    id bigint NOT NULL,
    user_id bigint NOT NULL,
    token_hash character varying(128) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    used_at timestamp with time zone,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.password_reset_tokens FORCE ROW LEVEL SECURITY;


--
-- Name: permissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions (
    id bigint NOT NULL,
    code character varying(100) NOT NULL,
    module character varying(50) NOT NULL,
    resource character varying(50) NOT NULL,
    action character varying(50) NOT NULL,
    is_high_risk boolean NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone
);


--
-- Name: refresh_tokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.refresh_tokens (
    id bigint NOT NULL,
    user_id bigint NOT NULL,
    token_hash character varying(128) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    revoked_at timestamp with time zone,
    replaced_by_token_id bigint,
    created_by_ip character varying(64),
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.refresh_tokens FORCE ROW LEVEL SECURITY;


--
-- Name: role_permissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.role_permissions (
    id bigint NOT NULL,
    role_id bigint NOT NULL,
    permission_id bigint NOT NULL,
    scope character varying(20) NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.role_permissions FORCE ROW LEVEL SECURITY;


--
-- Name: roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.roles (
    id bigint NOT NULL,
    name character varying(100) NOT NULL,
    code character varying(60) NOT NULL,
    description character varying(500),
    type character varying(20) NOT NULL,
    color character varying(20),
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.roles FORCE ROW LEVEL SECURITY;


--
-- Name: structure_nodes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.structure_nodes (
    id bigint NOT NULL,
    parent_id bigint,
    node_type character varying(20) NOT NULL,
    name character varying(200) NOT NULL,
    code character varying(60) NOT NULL,
    description character varying(500),
    manager_id bigint,
    sort_order integer NOT NULL,
    status character varying(20) NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.structure_nodes FORCE ROW LEVEL SECURITY;


--
-- Name: user_permissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_permissions (
    id bigint NOT NULL,
    user_id bigint NOT NULL,
    permission_id bigint NOT NULL,
    effect character varying(10) NOT NULL,
    scope character varying(20) NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.user_permissions FORCE ROW LEVEL SECURITY;


--
-- Name: user_roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_roles (
    id bigint NOT NULL,
    user_id bigint NOT NULL,
    role_id bigint NOT NULL,
    cluster_id bigint,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.user_roles FORCE ROW LEVEL SECURITY;


--
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    id bigint NOT NULL,
    email character varying(256) NOT NULL,
    normalized_email character varying(256) NOT NULL,
    password_hash character varying(512),
    security_stamp character varying(64) NOT NULL,
    require_password_change boolean NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    display_name character varying(200) NOT NULL,
    preferred_language character varying(8) NOT NULL,
    time_zone character varying(64) NOT NULL,
    avatar_url character varying(1024),
    status character varying(20) NOT NULL,
    access_start_date timestamp with time zone,
    access_expiry_date timestamp with time zone,
    last_login_at timestamp with time zone,
    access_failed_count integer NOT NULL,
    lockout_ends_at timestamp with time zone,
    two_factor_enabled boolean NOT NULL,
    two_factor_secret character varying(256),
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.users FORCE ROW LEVEL SECURITY;


--
-- Name: workspace_security_policies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.workspace_security_policies (
    id bigint NOT NULL,
    password_min_length integer NOT NULL,
    require_uppercase boolean NOT NULL,
    require_lowercase boolean NOT NULL,
    require_digit boolean NOT NULL,
    require_symbol boolean NOT NULL,
    password_expiry_days integer,
    max_failed_attempts integer NOT NULL,
    lockout_minutes integer NOT NULL,
    session_idle_timeout_minutes integer NOT NULL,
    refresh_token_days integer NOT NULL,
    require_two_factor boolean NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone,
    workspace_id bigint NOT NULL
);

ALTER TABLE ONLY public.workspace_security_policies FORCE ROW LEVEL SECURITY;


--
-- Name: workspaces; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.workspaces (
    id bigint NOT NULL,
    name character varying(200) NOT NULL,
    slug character varying(80) NOT NULL,
    legal_name character varying(250),
    default_language character varying(8) NOT NULL,
    time_zone character varying(64) NOT NULL,
    base_currency character varying(3) NOT NULL,
    country character varying(2),
    status character varying(20) NOT NULL,
    is_active boolean NOT NULL,
    is_deleted boolean NOT NULL,
    inserted_by bigint,
    inserted_date timestamp with time zone NOT NULL,
    changed_by bigint,
    changed_date timestamp with time zone
);


--
-- Name: audit_logs_2026_05; Type: TABLE ATTACH; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs ATTACH PARTITION public.audit_logs_2026_05 FOR VALUES FROM ('2026-05-01 00:00:00+05') TO ('2026-06-01 00:00:00+05');


--
-- Name: audit_logs_2026_06; Type: TABLE ATTACH; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs ATTACH PARTITION public.audit_logs_2026_06 FOR VALUES FROM ('2026-06-01 00:00:00+05') TO ('2026-07-01 00:00:00+05');


--
-- Name: audit_logs_2026_07; Type: TABLE ATTACH; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs ATTACH PARTITION public.audit_logs_2026_07 FOR VALUES FROM ('2026-07-01 00:00:00+05') TO ('2026-08-01 00:00:00+05');


--
-- Data for Name: asset_types; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.asset_types (id, code, name, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date) FROM stdin;
330038748779020292	NOTE	Note	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748820963328	DOCUMENT	Document	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748820963329	CUSTOMER	Customer	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748820963330	SUPPLIER	Supplier	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748820963331	VEHICLE	Vehicle	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748820963332	INVOICE	Invoice	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748820963333	RESOURCE	Resource	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
\.


--
-- Data for Name: assets; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.assets (id, asset_type_id, name, code, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053065133547520	330038748779020292	Note	\N	t	f	330038749018095616	2026-06-29 23:33:02.834934+05	\N	\N	330038748963569665
330053065305513984	330038748779020292	Note	\N	t	f	330038749018095616	2026-06-29 23:33:02.858933+05	\N	\N	330038748963569665
330053065355845632	330038748779020292	Note	\N	t	f	330038749018095616	2026-06-29 23:33:02.870606+05	\N	\N	330038748963569665
330053065397788672	330038748779020292	Note	\N	t	f	330038749018095616	2026-06-29 23:33:02.880396+05	\N	\N	330038748963569665
330053065443926016	330038748779020292	Note	\N	t	f	330038749018095616	2026-06-29 23:33:02.891764+05	\N	\N	330038748963569665
330053065485869056	330038748779020292	Note	\N	t	f	330038749018095616	2026-06-29 23:33:02.901739+05	\N	\N	330038748963569665
\.


--
-- Data for Name: documents; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.documents (id, asset_id, file_name, file_path, mime_type, file_size, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
\.


--
-- Data for Name: event_activities; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.event_activities (id, event_id, kind, message, actor_id, occurred_at, from_status_id, to_status_id, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053061929099264	330053061811658752	Created	Task TSK-00001 created.	330038749018095616	2026-06-29 23:33:02.053898+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.115752+05	\N	\N	330038748963569665
330053062444998658	330053062440804352	Created	Task TSK-00002 created.	330038749018095616	2026-06-29 23:33:02.176255+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.183822+05	\N	\N	330038748963569665
330053062533079042	330053062528884736	Created	Task TSK-00003 created.	330038749018095616	2026-06-29 23:33:02.197479+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.207218+05	\N	\N	330038748963569665
330053062658908162	330053062654713856	Created	Task TSK-00004 created.	330038749018095616	2026-06-29 23:33:02.22729+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.227874+05	\N	\N	330038748963569665
330053062696656899	330053062696656896	Created	Task TSK-00005 created.	330038749018095616	2026-06-29 23:33:02.236779+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.237284+05	\N	\N	330038748963569665
330053062738599939	330053062738599936	Created	Task TSK-00006 created.	330038749018095616	2026-06-29 23:33:02.246714+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.250588+05	\N	\N	330038748963569665
330053062797320194	330053062793125888	Created	Task TSK-00007 created.	330038749018095616	2026-06-29 23:33:02.260377+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.264101+05	\N	\N	330038748963569665
330053062860234754	330053062847651840	Created	Task TSK-00008 created.	330038749018095616	2026-06-29 23:33:02.27583+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.279287+05	\N	\N	330038748963569665
330053062935732226	330053062927343616	Created	Task TSK-00009 created.	330038749018095616	2026-06-29 23:33:02.293499+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.294156+05	\N	\N	330038748963569665
330053062981869571	330053062981869568	Created	Task TSK-00010 created.	330038749018095616	2026-06-29 23:33:02.304778+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.308479+05	\N	\N	330038748963569665
330053063048978435	330053063048978432	Created	Task TSK-00011 created.	330038749018095616	2026-06-29 23:33:02.320806+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.326571+05	\N	\N	330038748963569665
330053063116087298	330053063111892992	Created	Task TSK-00012 created.	330038749018095616	2026-06-29 23:33:02.336063+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.338921+05	\N	\N	330038748963569665
330053063162224643	330053063162224640	Created	Task TSK-00013 created.	330038749018095616	2026-06-29 23:33:02.347931+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.348489+05	\N	\N	330038748963569665
330053063199973378	330053063195779073	Created	Task TSK-00014 created.	330038749018095616	2026-06-29 23:33:02.356865+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.357933+05	\N	\N	330038748963569665
330053063250305026	330053063246110721	Created	Task TSK-00015 created.	330038749018095616	2026-06-29 23:33:02.368282+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.373856+05	\N	\N	330038748963569665
330053063321608193	330053063317413888	Created	Task TSK-00016 created.	330038749018095616	2026-06-29 23:33:02.385043+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.388118+05	\N	\N	330038748963569665
330053063480991745	330053061811658752	StatusChanged	Status changed to In Progress.	330038749018095616	2026-06-29 23:33:02.42392+05	330038749466886144	330038749500440576	t	f	330038749018095616	2026-06-29 23:33:02.428674+05	\N	\N	330038748963569665
330053063585849346	330053062440804352	StatusChanged	Status changed to In Progress.	330038749018095616	2026-06-29 23:33:02.448779+05	330038749466886144	330038749500440576	t	f	330038749018095616	2026-06-29 23:33:02.459474+05	\N	\N	330038748963569665
330053063694901250	330053062528884736	StatusChanged	Status changed to In Progress.	330038749018095616	2026-06-29 23:33:02.474339+05	330038749466886144	330038749500440576	t	f	330038749018095616	2026-06-29 23:33:02.478424+05	\N	\N	330038748963569665
330053063778787331	330053062654713856	StatusChanged	Status changed to In Progress.	330038749018095616	2026-06-29 23:33:02.494769+05	330038749466886144	330038749500440576	t	f	330038749018095616	2026-06-29 23:33:02.495398+05	\N	\N	330038748963569665
330053063824924675	330053062696656896	StatusChanged	Status changed to Done.	330038749018095616	2026-06-29 23:33:02.505486+05	330038749466886144	330038749500440577	t	f	330038749018095616	2026-06-29 23:33:02.507218+05	\N	\N	330038748963569665
330053063879450624	330053062738599936	StatusChanged	Status changed to Done.	330038749018095616	2026-06-29 23:33:02.518005+05	330038749466886144	330038749500440577	t	f	330038749018095616	2026-06-29 23:33:02.525895+05	\N	\N	330038748963569665
330053063942365187	330053062793125888	StatusChanged	Status changed to Done.	330038749018095616	2026-06-29 23:33:02.533455+05	330038749466886144	330038749500440577	t	f	330038749018095616	2026-06-29 23:33:02.535725+05	\N	\N	330038748963569665
330053063992696835	330053062847651840	StatusChanged	Status changed to Cancelled.	330038749018095616	2026-06-29 23:33:02.545926+05	330038749466886144	330038749500440578	t	f	330038749018095616	2026-06-29 23:33:02.550707+05	\N	\N	330038748963569665
330053064059805698	330053063048978432	StatusChanged	Status changed to In Progress.	330038749018095616	2026-06-29 23:33:02.561295+05	330038749466886144	330038749500440576	t	f	330038749018095616	2026-06-29 23:33:02.569696+05	\N	\N	330038748963569665
330053064131108866	330053063111892992	StatusChanged	Status changed to In Progress.	330038749018095616	2026-06-29 23:33:02.578488+05	330038749466886144	330038749500440576	t	f	330038749018095616	2026-06-29 23:33:02.582886+05	\N	\N	330038748963569665
330053064194023428	330053063162224640	StatusChanged	Status changed to In Progress.	330038749018095616	2026-06-29 23:33:02.593833+05	330038749466886144	330038749500440576	t	f	330038749018095616	2026-06-29 23:33:02.594198+05	\N	\N	330038748963569665
330053064223383554	330053063195779073	StatusChanged	Status changed to In Progress.	330038749018095616	2026-06-29 23:33:02.600153+05	330038749466886144	330038749500440576	t	f	330038749018095616	2026-06-29 23:33:02.600483+05	\N	\N	330038748963569665
330053064261132289	330053063246110721	StatusChanged	Status changed to Done.	330038749018095616	2026-06-29 23:33:02.609042+05	330038749466886144	330038749500440577	t	f	330038749018095616	2026-06-29 23:33:02.611805+05	\N	\N	330038748963569665
330053064294686724	330053063317413888	StatusChanged	Status changed to Done.	330038749018095616	2026-06-29 23:33:02.617951+05	330038749466886144	330038749500440577	t	f	330038749018095616	2026-06-29 23:33:02.625028+05	\N	\N	330038748963569665
330053064454070272	330053061811658752	DailyReportAdded	Daily report filed for 2026-06-29.	330038749018095616	2026-06-29 23:33:02.655828+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.659452+05	\N	\N	330038748963569665
330053064571510785	330053062440804352	DailyReportAdded	Daily report filed for 2026-06-28.	330038749018095616	2026-06-29 23:33:02.683932+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.686868+05	\N	\N	330038748963569665
330053064638619649	330053062528884736	DailyReportAdded	Daily report filed for 2026-06-27.	330038749018095616	2026-06-29 23:33:02.699724+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.709057+05	\N	\N	330038748963569665
330053064718311425	330053062654713856	DailyReportAdded	Daily report filed for 2026-06-26.	330038749018095616	2026-06-29 23:33:02.718422+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.718486+05	\N	\N	330038748963569665
330053064756060162	330053062696656896	DailyReportAdded	Daily report filed for 2026-06-29.	330038749018095616	2026-06-29 23:33:02.727692+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.727731+05	\N	\N	330038748963569665
330053064802197506	330053062738599936	DailyReportAdded	Daily report filed for 2026-06-28.	330038749018095616	2026-06-29 23:33:02.738884+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.743241+05	\N	\N	330038748963569665
330053064852529153	330053062793125888	DailyReportAdded	Daily report filed for 2026-06-27.	330038749018095616	2026-06-29 23:33:02.750361+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.754145+05	\N	\N	330038748963569665
330053064902860802	330053062847651840	DailyReportAdded	Daily report filed for 2026-06-26.	330038749018095616	2026-06-29 23:33:02.762567+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.765663+05	\N	\N	330038748963569665
330053064969969665	330053062927343616	DailyReportAdded	Daily report filed for 2026-06-29.	330038749018095616	2026-06-29 23:33:02.77821+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.778267+05	\N	\N	330038748963569665
330053065011912705	330053062981869568	DailyReportAdded	Daily report filed for 2026-06-28.	330038749018095616	2026-06-29 23:33:02.788803+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.793602+05	\N	\N	330038748963569665
330053065204850688	330053061811658752	NoteAdded	Note added.	330038749018095616	2026-06-29 23:33:02.83485+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.834934+05	\N	\N	330038748963569665
330053065305513987	330053062440804352	NoteAdded	Note added.	330038749018095616	2026-06-29 23:33:02.858896+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.858933+05	\N	\N	330038748963569665
330053065355845635	330053062528884736	NoteAdded	Note added.	330038749018095616	2026-06-29 23:33:02.87056+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.870606+05	\N	\N	330038748963569665
330053065397788675	330053062654713856	NoteAdded	Note added.	330038749018095616	2026-06-29 23:33:02.880349+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.880396+05	\N	\N	330038748963569665
330053065443926019	330053062696656896	NoteAdded	Note added.	330038749018095616	2026-06-29 23:33:02.891726+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.891764+05	\N	\N	330038748963569665
330053065485869059	330053062738599936	NoteAdded	Note added.	330038749018095616	2026-06-29 23:33:02.901712+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:02.901739+05	\N	\N	330038748963569665
330053262328750080	330053063162224640	Updated	Task details updated.	330038749018095616	2026-06-29 23:33:49.832133+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:49.832293+05	\N	\N	330038748963569665
330053262425219072	330053062927343616	Updated	Task details updated.	330038749018095616	2026-06-29 23:33:49.855553+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:49.855623+05	\N	\N	330038748963569665
330053262517493760	330053062654713856	Updated	Task details updated.	330038749018095616	2026-06-29 23:33:49.877907+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:49.877988+05	\N	\N	330038748963569665
330053262630739968	330053063195779073	Updated	Task details updated.	330038749018095616	2026-06-29 23:33:49.904891+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:49.90498+05	\N	\N	330038748963569665
330053262727208960	330053061811658752	Updated	Task details updated.	330038749018095616	2026-06-29 23:33:49.927785+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:49.927884+05	\N	\N	330038748963569665
330053262798512128	330053062981869568	Updated	Task details updated.	330038749018095616	2026-06-29 23:33:49.944666+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:49.944739+05	\N	\N	330038748963569665
330053262894981120	330053062528884736	Updated	Task details updated.	330038749018095616	2026-06-29 23:33:49.967598+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:49.967671+05	\N	\N	330038748963569665
330053262966284288	330053063111892992	Updated	Task details updated.	330038749018095616	2026-06-29 23:33:49.984455+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:49.984554+05	\N	\N	330038748963569665
330053263075336192	330053062440804352	Updated	Task details updated.	330038749018095616	2026-06-29 23:33:50.010166+05	\N	\N	t	f	330038749018095616	2026-06-29 23:33:50.010248+05	\N	\N	330038748963569665
\.


--
-- Data for Name: event_assets; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.event_assets (id, event_id, asset_id, relation_type, description, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053065183879168	330053061811658752	330053065133547520	NOTE	\N	t	f	330038749018095616	2026-06-29 23:33:02.834934+05	\N	\N	330038748963569665
330053065305513986	330053062440804352	330053065305513984	NOTE	\N	t	f	330038749018095616	2026-06-29 23:33:02.858933+05	\N	\N	330038748963569665
330053065355845634	330053062528884736	330053065355845632	NOTE	\N	t	f	330038749018095616	2026-06-29 23:33:02.870606+05	\N	\N	330038748963569665
330053065397788674	330053062654713856	330053065397788672	NOTE	\N	t	f	330038749018095616	2026-06-29 23:33:02.880396+05	\N	\N	330038748963569665
330053065443926018	330053062696656896	330053065443926016	NOTE	\N	t	f	330038749018095616	2026-06-29 23:33:02.891764+05	\N	\N	330038748963569665
330053065485869058	330053062738599936	330053065485869056	NOTE	\N	t	f	330038749018095616	2026-06-29 23:33:02.901739+05	\N	\N	330038748963569665
\.


--
-- Data for Name: event_daily_reports; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.event_daily_reports (id, event_id, user_id, report_date, description, estimated_time, actual_time, remaining_time, status_id, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053064416321536	330053061811658752	330038749018095616	2026-06-29	Worked on it (2026-06-29).	5.40	4.50	0.90	\N	t	f	330038749018095616	2026-06-29 23:33:02.659452+05	\N	\N	330038748963569665
330053064571510784	330053062440804352	330038749018095616	2026-06-28	Worked on it (2026-06-28).	2.60	3.10	0.00	\N	t	f	330038749018095616	2026-06-29 23:33:02.686868+05	\N	\N	330038748963569665
330053064638619648	330053062528884736	330038749018095616	2026-06-27	Worked on it (2026-06-27).	5.40	6.10	0.00	\N	t	f	330038749018095616	2026-06-29 23:33:02.709057+05	\N	\N	330038748963569665
330053064718311424	330053062654713856	330038749018095616	2026-06-26	Worked on it (2026-06-26).	5.00	5.40	0.00	\N	t	f	330038749018095616	2026-06-29 23:33:02.718486+05	\N	\N	330038748963569665
330053064756060161	330053062696656896	330038749018095616	2026-06-29	Worked on it (2026-06-29).	6.70	6.90	0.00	\N	t	f	330038749018095616	2026-06-29 23:33:02.727731+05	\N	\N	330038748963569665
330053064802197505	330053062738599936	330038749018095616	2026-06-28	Worked on it (2026-06-28).	7.50	7.10	0.40	\N	t	f	330038749018095616	2026-06-29 23:33:02.743241+05	\N	\N	330038748963569665
330053064852529152	330053062793125888	330038749018095616	2026-06-27	Worked on it (2026-06-27).	3.50	2.90	0.60	\N	t	f	330038749018095616	2026-06-29 23:33:02.754145+05	\N	\N	330038748963569665
330053064902860801	330053062847651840	330038749018095616	2026-06-26	Worked on it (2026-06-26).	6.70	5.10	1.60	\N	t	f	330038749018095616	2026-06-29 23:33:02.765663+05	\N	\N	330038748963569665
330053064969969664	330053062927343616	330038749018095616	2026-06-29	Worked on it (2026-06-29).	3.80	4.00	0.00	\N	t	f	330038749018095616	2026-06-29 23:33:02.778267+05	\N	\N	330038748963569665
330053065011912704	330053062981869568	330038749018095616	2026-06-28	Worked on it (2026-06-28).	4.10	4.20	0.00	\N	t	f	330038749018095616	2026-06-29 23:33:02.793602+05	\N	\N	330038748963569665
\.


--
-- Data for Name: event_dependencies; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.event_dependencies (id, event_id, depends_on_event_id, is_blocking, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
\.


--
-- Data for Name: event_statuses; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.event_statuses (id, event_id, status_id, is_current, note, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053062935732225	330053062927343616	330038749466886144	t	\N	t	f	330038749018095616	2026-06-29 23:33:02.294156+05	\N	\N	330038748963569665
330053062981869570	330053062981869568	330038749466886144	t	\N	t	f	330038749018095616	2026-06-29 23:33:02.308479+05	\N	\N	330038748963569665
330053061908127744	330053061811658752	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.115752+05	330038749018095616	2026-06-29 23:33:02.428674+05	330038748963569665
330053063480991744	330053061811658752	330038749500440576	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.428674+05	\N	\N	330038748963569665
330053062444998657	330053062440804352	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.183822+05	330038749018095616	2026-06-29 23:33:02.459474+05	330038748963569665
330053063585849345	330053062440804352	330038749500440576	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.459474+05	\N	\N	330038748963569665
330053062533079041	330053062528884736	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.207218+05	330038749018095616	2026-06-29 23:33:02.478424+05	330038748963569665
330053063694901249	330053062528884736	330038749500440576	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.478424+05	\N	\N	330038748963569665
330053062658908161	330053062654713856	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.227874+05	330038749018095616	2026-06-29 23:33:02.495398+05	330038748963569665
330053063778787330	330053062654713856	330038749500440576	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.495398+05	\N	\N	330038748963569665
330053062696656898	330053062696656896	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.237284+05	330038749018095616	2026-06-29 23:33:02.507218+05	330038748963569665
330053063824924674	330053062696656896	330038749500440577	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.507218+05	\N	\N	330038748963569665
330053062738599938	330053062738599936	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.250588+05	330038749018095616	2026-06-29 23:33:02.525895+05	330038748963569665
330053063875256322	330053062738599936	330038749500440577	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.525895+05	\N	\N	330038748963569665
330053062797320193	330053062793125888	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.264101+05	330038749018095616	2026-06-29 23:33:02.535725+05	330038748963569665
330053063942365186	330053062793125888	330038749500440577	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.535725+05	\N	\N	330038748963569665
330053062860234753	330053062847651840	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.279287+05	330038749018095616	2026-06-29 23:33:02.550707+05	330038748963569665
330053063992696834	330053062847651840	330038749500440578	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.550707+05	\N	\N	330038748963569665
330053063048978434	330053063048978432	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.326571+05	330038749018095616	2026-06-29 23:33:02.569696+05	330038748963569665
330053064059805697	330053063048978432	330038749500440576	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.569696+05	\N	\N	330038748963569665
330053063116087297	330053063111892992	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.338921+05	330038749018095616	2026-06-29 23:33:02.582886+05	330038748963569665
330053064131108865	330053063111892992	330038749500440576	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.582886+05	\N	\N	330038748963569665
330053063162224642	330053063162224640	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.348489+05	330038749018095616	2026-06-29 23:33:02.594198+05	330038748963569665
330053064194023427	330053063162224640	330038749500440576	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.594198+05	\N	\N	330038748963569665
330053063199973377	330053063195779073	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.357933+05	330038749018095616	2026-06-29 23:33:02.600483+05	330038748963569665
330053064223383553	330053063195779073	330038749500440576	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.600483+05	\N	\N	330038748963569665
330053063250305025	330053063246110721	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.373856+05	330038749018095616	2026-06-29 23:33:02.611805+05	330038748963569665
330053064261132288	330053063246110721	330038749500440577	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.611805+05	\N	\N	330038748963569665
330053063321608192	330053063317413888	330038749466886144	f	\N	t	f	330038749018095616	2026-06-29 23:33:02.388118+05	330038749018095616	2026-06-29 23:33:02.625028+05	330038748963569665
330053064294686723	330053063317413888	330038749500440577	t	Progress update.	t	f	330038749018095616	2026-06-29 23:33:02.625028+05	\N	\N	330038748963569665
\.


--
-- Data for Name: event_types; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.event_types (id, code, name, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date) FROM stdin;
330038748758048768	TASK_MANAGEMENT	Task Management	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748779020288	ISSUE	Issue	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748779020289	MAINTENANCE	Maintenance	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748779020290	APPROVAL	Approval	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
330038748779020291	SALES_ACTIVITY	Sales Activity	t	f	\N	2026-06-29 22:36:09.542564+05	\N	\N
\.


--
-- Data for Name: events; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.events (id, event_type_id, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053061811658752	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.115752+05	\N	\N	330038748963569665
330053062440804352	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.183822+05	\N	\N	330038748963569665
330053062528884736	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.207218+05	\N	\N	330038748963569665
330053062654713856	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.227874+05	\N	\N	330038748963569665
330053062696656896	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.237284+05	\N	\N	330038748963569665
330053062738599936	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.250588+05	\N	\N	330038748963569665
330053062793125888	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.264101+05	\N	\N	330038748963569665
330053062847651840	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.279287+05	\N	\N	330038748963569665
330053062927343616	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.294156+05	\N	\N	330038748963569665
330053062981869568	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.308479+05	\N	\N	330038748963569665
330053063048978432	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.326571+05	\N	\N	330038748963569665
330053063111892992	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.338921+05	\N	\N	330038748963569665
330053063162224640	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.348489+05	\N	\N	330038748963569665
330053063195779073	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.357933+05	\N	\N	330038748963569665
330053063246110721	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.373856+05	\N	\N	330038748963569665
330053063317413888	330038748758048768	t	f	330038749018095616	2026-06-29 23:33:02.388118+05	\N	\N	330038748963569665
\.


--
-- Data for Name: mail_templates; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.mail_templates (id, workspace_id, code, name, subject_template, body_html_template, body_text_template, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date) FROM stdin;
330038748862906368	\N	TASK_CREATED	Task Created	New task {{TaskRef}}: {{TaskTitle}}	<p>A new task <strong>{{TaskRef}} — {{TaskTitle}}</strong> was created by {{Actor}}.</p>	\N	t	f	\N	2026-06-29 22:36:09.559722+05	\N	\N
330038748892266496	\N	TASK_ASSIGNED	Task Assigned	You've been assigned {{TaskRef}}: {{TaskTitle}}	<p>{{Actor}} assigned <strong>{{TaskRef}} — {{TaskTitle}}</strong> to you. Priority: {{Priority}}. Due: {{DueDate}}.</p>	\N	t	f	\N	2026-06-29 22:36:09.559722+05	\N	\N
330038748892266497	\N	TASK_OPENED	Task Opened	{{TaskRef}} has started	<p>Work has started on <strong>{{TaskRef}} — {{TaskTitle}}</strong> (status {{Status}}).</p>	\N	t	f	\N	2026-06-29 22:36:09.559722+05	\N	\N
330038748892266498	\N	TASK_STATUS_CHANGED	Task Status Changed	{{TaskRef}} is now {{Status}}	<p>The status of <strong>{{TaskRef}} — {{TaskTitle}}</strong> was changed from {{OldStatus}} to <strong>{{Status}}</strong> by {{Actor}}.</p>	\N	t	f	\N	2026-06-29 22:36:09.559722+05	\N	\N
330038748892266499	\N	TASK_COMPLETED	Task Completed	{{TaskRef}} completed	<p><strong>{{TaskRef}} — {{TaskTitle}}</strong> was completed ({{Status}}) by {{Actor}}.</p>	\N	t	f	\N	2026-06-29 22:36:09.559722+05	\N	\N
330038748892266500	\N	DAILY_REPORT_SUBMITTED	Daily Report Submitted	Daily report on {{TaskRef}}	<p>{{Actor}} filed a daily report on <strong>{{TaskRef}} — {{TaskTitle}}</strong> for {{Date}}.</p><p>{{DailyReportDescription}}</p>	\N	t	f	\N	2026-06-29 22:36:09.559722+05	\N	\N
330038748892266501	\N	DAILY_REPORT_STATUS_CHANGED	Daily Report Status Changed	{{TaskRef}}: report + status now {{Status}}	<p>{{Actor}} filed a daily report on <strong>{{TaskRef}} — {{TaskTitle}}</strong> for {{Date}} and changed status from {{OldStatus}} to <strong>{{Status}}</strong>.</p><p>{{DailyReportDescription}}</p>	\N	t	f	\N	2026-06-29 22:36:09.559722+05	\N	\N
\.


--
-- Data for Name: notes; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.notes (id, asset_id, body, is_pinned, is_internal, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053065162907648	330053065133547520	Customer prefers email updates.	f	t	t	f	330038749018095616	2026-06-29 23:33:02.834934+05	\N	\N	330038748963569665
330053065305513985	330053065305513984	Customer prefers email updates.	f	t	t	f	330038749018095616	2026-06-29 23:33:02.858933+05	\N	\N	330038748963569665
330053065355845633	330053065355845632	Customer prefers email updates.	f	t	t	f	330038749018095616	2026-06-29 23:33:02.870606+05	\N	\N	330038748963569665
330053065397788673	330053065397788672	Customer prefers email updates.	f	t	t	f	330038749018095616	2026-06-29 23:33:02.880396+05	\N	\N	330038748963569665
330053065443926017	330053065443926016	Customer prefers email updates.	f	t	t	f	330038749018095616	2026-06-29 23:33:02.891764+05	\N	\N	330038748963569665
330053065485869057	330053065485869056	Customer prefers email updates.	f	t	t	f	330038749018095616	2026-06-29 23:33:02.901739+05	\N	\N	330038748963569665
\.


--
-- Data for Name: send_mail_attempts; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.send_mail_attempts (id, send_mail_id, attempt_no, success, provider_response, error_message, attempted_at, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053091188563968	330053062113648640	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.029443+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091247284224	330053062474358785	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.043341+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091251478529	330053062575022081	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.044287+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091255672832	330053062755377153	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.045112+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091255672834	330053062814097408	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.045734+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091268255745	330053062877011969	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.048662+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091272450049	330053062998646785	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.049714+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091280838657	330053063074144257	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.051737+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091285032961	330053063124475905	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.052838+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091293421568	330053063271276545	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.054044+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091297615872	330053063334191104	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.055083+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091301810177	330053063501963265	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.056742+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091306004481	330053063631986689	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.057897+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091314393088	330053063711678465	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.059041+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091314393090	330053063908810753	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.059972+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091322781697	330053063950753793	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.061643+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091331170304	330053064013668353	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.063488+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091343753216	330053064093360129	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.066113+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091347947520	330053064147886081	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.067215+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091352141825	330053064269520897	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.068237+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091356336129	330053064324046849	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.069914+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091368919041	330053064470847489	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.07279+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091373113345	330053064584093697	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.073935+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091381501953	330053064676368385	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.075487+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053091385696257	330053064823169025	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:09.076609+05	t	f	\N	2026-06-29 23:33:09.076645+05	\N	\N	330038748963569665
330053154480611328	330053064869306369	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:24.11928+05	t	f	\N	2026-06-29 23:33:24.127473+05	\N	\N	330038748963569665
330053154505777152	330053064915443713	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:24.125034+05	t	f	\N	2026-06-29 23:33:24.127473+05	\N	\N	330038748963569665
330053154514165760	330053065032884225	1	t	Delivered to 1 recipient(s).	\N	2026-06-29 23:33:24.127411+05	t	f	\N	2026-06-29 23:33:24.127473+05	\N	\N	330038748963569665
\.


--
-- Data for Name: send_mail_recipients; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.send_mail_recipients (id, send_mail_id, address, display_name, recipient_type, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053062159785984	330053062113648640	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.115752+05	\N	\N	330038748963569665
330053062474358786	330053062474358785	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.183822+05	\N	\N	330038748963569665
330053062575022082	330053062575022081	layla@demo.test	Layla Hassan	To	t	f	330038749018095616	2026-06-29 23:33:02.207218+05	\N	\N	330038748963569665
330053062755377154	330053062755377153	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.250588+05	\N	\N	330038748963569665
330053062814097409	330053062814097408	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.264101+05	\N	\N	330038748963569665
330053062877011970	330053062877011969	layla@demo.test	Layla Hassan	To	t	f	330038749018095616	2026-06-29 23:33:02.279287+05	\N	\N	330038748963569665
330053062998646786	330053062998646785	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.308479+05	\N	\N	330038748963569665
330053063074144258	330053063074144257	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.326571+05	\N	\N	330038748963569665
330053063124475906	330053063124475905	layla@demo.test	Layla Hassan	To	t	f	330038749018095616	2026-06-29 23:33:02.338921+05	\N	\N	330038748963569665
330053063271276546	330053063271276545	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.373856+05	\N	\N	330038748963569665
330053063334191105	330053063334191104	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.388118+05	\N	\N	330038748963569665
330053063501963266	330053063501963265	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.428674+05	\N	\N	330038748963569665
330053063631986690	330053063631986689	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.459474+05	\N	\N	330038748963569665
330053063711678466	330053063711678465	layla@demo.test	Layla Hassan	To	t	f	330038749018095616	2026-06-29 23:33:02.478424+05	\N	\N	330038748963569665
330053063908810754	330053063908810753	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.525895+05	\N	\N	330038748963569665
330053063950753794	330053063950753793	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.535725+05	\N	\N	330038748963569665
330053064013668354	330053064013668353	layla@demo.test	Layla Hassan	To	t	f	330038749018095616	2026-06-29 23:33:02.550707+05	\N	\N	330038748963569665
330053064093360130	330053064093360129	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.569696+05	\N	\N	330038748963569665
330053064147886082	330053064147886081	layla@demo.test	Layla Hassan	To	t	f	330038749018095616	2026-06-29 23:33:02.582886+05	\N	\N	330038748963569665
330053064269520898	330053064269520897	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.611805+05	\N	\N	330038748963569665
330053064324046850	330053064324046849	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.625028+05	\N	\N	330038748963569665
330053064470847490	330053064470847489	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.659452+05	\N	\N	330038748963569665
330053064584093698	330053064584093697	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.686868+05	\N	\N	330038748963569665
330053064680562688	330053064676368385	layla@demo.test	Layla Hassan	To	t	f	330038749018095616	2026-06-29 23:33:02.709057+05	\N	\N	330038748963569665
330053064823169026	330053064823169025	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.743241+05	\N	\N	330038748963569665
330053064869306370	330053064869306369	omar@demo.test	Omar Ali	To	t	f	330038749018095616	2026-06-29 23:33:02.754145+05	\N	\N	330038748963569665
330053064915443714	330053064915443713	layla@demo.test	Layla Hassan	To	t	f	330038749018095616	2026-06-29 23:33:02.765663+05	\N	\N	330038748963569665
330053065032884226	330053065032884225	sara@demo.test	Sara Khan	To	t	f	330038749018095616	2026-06-29 23:33:02.793602+05	\N	\N	330038748963569665
\.


--
-- Data for Name: send_mails; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.send_mails (id, mail_template_id, template_code, subject, body_html, body_text, template_data_json, send_status, scheduled_at, next_attempt_at, sent_at, retry_count, max_retries, last_error, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053063631986689	330038748892266497	TASK_OPENED	TSK-00002 has started	<p>Work has started on <strong>TSK-00002 — Design GL period-close workflow</strong> (status In Progress).</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "In Progress", "DueDate": "2026-07-11", "TaskRef": "TSK-00002", "Priority": "High", "UserName": "Demo Owner", "NewStatus": "In Progress", "OldStatus": "New", "TaskTitle": "Design GL period-close workflow", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00002", "AssigneeName": "Omar Ali", "PriorityName": "High", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.459405+05	\N	2026-06-29 23:33:09.057896+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.459474+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064013668353	330038748892266499	TASK_COMPLETED	TSK-00008 completed	<p><strong>TSK-00008 — Vehicle maintenance reminder job</strong> was completed (Cancelled) by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "Cancelled", "DueDate": "2026-08-03", "TaskRef": "TSK-00008", "Priority": "Low", "UserName": "Demo Owner", "NewStatus": "Cancelled", "OldStatus": "New", "TaskTitle": "Vehicle maintenance reminder job", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00008", "AssigneeName": "Layla Hassan", "PriorityName": "Low", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.550649+05	\N	2026-06-29 23:33:09.063485+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.550707+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064324046849	330038748892266499	TASK_COMPLETED	TSK-00016 completed	<p><strong>TSK-00016 — Document ClamAV scan hook</strong> was completed (Done) by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "Done", "DueDate": "2026-07-18", "TaskRef": "TSK-00016", "Priority": "Medium", "UserName": "Demo Owner", "NewStatus": "Done", "OldStatus": "New", "TaskTitle": "Document ClamAV scan hook", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00016", "AssigneeName": "Omar Ali", "PriorityName": "Medium", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.624956+05	\N	2026-06-29 23:33:09.069913+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.625028+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064676368385	330038748892266500	DAILY_REPORT_SUBMITTED	Daily report on TSK-00003	<p>Demo Owner filed a daily report on <strong>TSK-00003 — Onboarding wizard for new workspaces</strong> for 2026-06-27.</p><p>Worked on it (2026-06-27).</p>	\N	{"Date": "2026-06-27", "Actor": "Demo Owner", "Status": "", "DueDate": "2026-07-13", "TaskRef": "TSK-00003", "Priority": "Critical", "UserName": "Demo Owner", "NewStatus": "", "OldStatus": "", "TaskTitle": "Onboarding wizard for new workspaces", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00003", "AssigneeName": "Layla Hassan", "PriorityName": "Critical", "ReporterName": "Demo Owner", "DailyReportDescription": "Worked on it (2026-06-27)."}	Sent	2026-06-29 23:33:02.708981+05	\N	2026-06-29 23:33:09.075486+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.709057+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064823169025	330038748892266500	DAILY_REPORT_SUBMITTED	Daily report on TSK-00006	<p>Demo Owner filed a daily report on <strong>TSK-00006 — Add Arabic RTL polish to reports</strong> for 2026-06-28.</p><p>Worked on it (2026-06-28).</p>	\N	{"Date": "2026-06-28", "Actor": "Demo Owner", "Status": "", "DueDate": "2026-07-02", "TaskRef": "TSK-00006", "Priority": "High", "UserName": "Demo Owner", "NewStatus": "", "OldStatus": "", "TaskTitle": "Add Arabic RTL polish to reports", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00006", "AssigneeName": "Sara Khan", "PriorityName": "High", "ReporterName": "Demo Owner", "DailyReportDescription": "Worked on it (2026-06-28)."}	Sent	2026-06-29 23:33:02.743167+05	\N	2026-06-29 23:33:09.076608+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.743241+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053062113648640	330038748862906368	TASK_CREATED	New task TSK-00001: Fix payroll WPS export rounding	<p>A new task <strong>TSK-00001 — Fix payroll WPS export rounding</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-07-02", "TaskRef": "TSK-00001", "Priority": "Medium", "UserName": "Demo Owner", "TaskTitle": "Fix payroll WPS export rounding", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00001", "AssigneeName": "Sara Khan", "PriorityName": "Medium", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.097302+05	\N	2026-06-29 23:33:09.028969+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.115752+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053062474358785	330038748862906368	TASK_CREATED	New task TSK-00002: Design GL period-close workflow	<p>A new task <strong>TSK-00002 — Design GL period-close workflow</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-07-11", "TaskRef": "TSK-00002", "Priority": "High", "UserName": "Demo Owner", "TaskTitle": "Design GL period-close workflow", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00002", "AssigneeName": "Omar Ali", "PriorityName": "High", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.183757+05	\N	2026-06-29 23:33:09.043338+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.183822+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053062575022081	330038748862906368	TASK_CREATED	New task TSK-00003: Onboarding wizard for new workspaces	<p>A new task <strong>TSK-00003 — Onboarding wizard for new workspaces</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-07-13", "TaskRef": "TSK-00003", "Priority": "Critical", "UserName": "Demo Owner", "TaskTitle": "Onboarding wizard for new workspaces", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00003", "AssigneeName": "Layla Hassan", "PriorityName": "Critical", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.207135+05	\N	2026-06-29 23:33:09.044286+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.207218+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053062755377153	330038748862906368	TASK_CREATED	New task TSK-00006: Add Arabic RTL polish to reports	<p>A new task <strong>TSK-00006 — Add Arabic RTL polish to reports</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-07-02", "TaskRef": "TSK-00006", "Priority": "High", "UserName": "Demo Owner", "TaskTitle": "Add Arabic RTL polish to reports", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00006", "AssigneeName": "Sara Khan", "PriorityName": "High", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.250468+05	\N	2026-06-29 23:33:09.045111+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.250588+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053062814097408	330038748862906368	TASK_CREATED	New task TSK-00007: Inventory reorder-point alerts	<p>A new task <strong>TSK-00007 — Inventory reorder-point alerts</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-07-22", "TaskRef": "TSK-00007", "Priority": "Critical", "UserName": "Demo Owner", "TaskTitle": "Inventory reorder-point alerts", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00007", "AssigneeName": "Omar Ali", "PriorityName": "Critical", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.264047+05	\N	2026-06-29 23:33:09.045733+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.264101+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053062877011969	330038748862906368	TASK_CREATED	New task TSK-00008: Vehicle maintenance reminder job	<p>A new task <strong>TSK-00008 — Vehicle maintenance reminder job</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-08-03", "TaskRef": "TSK-00008", "Priority": "Low", "UserName": "Demo Owner", "TaskTitle": "Vehicle maintenance reminder job", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00008", "AssigneeName": "Layla Hassan", "PriorityName": "Low", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.279239+05	\N	2026-06-29 23:33:09.04866+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.279287+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053062998646785	330038748862906368	TASK_CREATED	New task TSK-00010: Two-factor recovery codes	<p>A new task <strong>TSK-00010 — Two-factor recovery codes</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-06-29", "TaskRef": "TSK-00010", "Priority": "Critical", "UserName": "Demo Owner", "TaskTitle": "Two-factor recovery codes", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00010", "AssigneeName": "Sara Khan", "PriorityName": "Critical", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.308428+05	\N	2026-06-29 23:33:09.049712+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.308479+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064915443713	330038748892266500	DAILY_REPORT_SUBMITTED	Daily report on TSK-00008	<p>Demo Owner filed a daily report on <strong>TSK-00008 — Vehicle maintenance reminder job</strong> for 2026-06-26.</p><p>Worked on it (2026-06-26).</p>	\N	{"Date": "2026-06-26", "Actor": "Demo Owner", "Status": "", "DueDate": "2026-08-03", "TaskRef": "TSK-00008", "Priority": "Low", "UserName": "Demo Owner", "NewStatus": "", "OldStatus": "", "TaskTitle": "Vehicle maintenance reminder job", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00008", "AssigneeName": "Layla Hassan", "PriorityName": "Low", "ReporterName": "Demo Owner", "DailyReportDescription": "Worked on it (2026-06-26)."}	Sent	2026-06-29 23:33:02.765608+05	\N	2026-06-29 23:33:24.12502+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.765663+05	\N	2026-06-29 23:33:24.127473+05	330038748963569665
330053063950753793	330038748892266499	TASK_COMPLETED	TSK-00007 completed	<p><strong>TSK-00007 — Inventory reorder-point alerts</strong> was completed (Done) by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "Done", "DueDate": "2026-07-22", "TaskRef": "TSK-00007", "Priority": "Critical", "UserName": "Demo Owner", "NewStatus": "Done", "OldStatus": "New", "TaskTitle": "Inventory reorder-point alerts", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00007", "AssigneeName": "Omar Ali", "PriorityName": "Critical", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.535671+05	\N	2026-06-29 23:33:09.061642+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.535725+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064147886081	330038748892266497	TASK_OPENED	TSK-00012 has started	<p>Work has started on <strong>TSK-00012 — Purchase order receiving screen</strong> (status In Progress).</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "In Progress", "DueDate": "2026-07-21", "TaskRef": "TSK-00012", "Priority": "Medium", "UserName": "Demo Owner", "NewStatus": "In Progress", "OldStatus": "New", "TaskTitle": "Purchase order receiving screen", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00012", "AssigneeName": "Layla Hassan", "PriorityName": "Medium", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.582838+05	\N	2026-06-29 23:33:09.067214+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.582886+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064269520897	330038748892266499	TASK_COMPLETED	TSK-00015 completed	<p><strong>TSK-00015 — GOSI contribution calculation</strong> was completed (Done) by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "Done", "DueDate": "2026-07-03", "TaskRef": "TSK-00015", "Priority": "Low", "UserName": "Demo Owner", "NewStatus": "Done", "OldStatus": "New", "TaskTitle": "GOSI contribution calculation", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00015", "AssigneeName": "Sara Khan", "PriorityName": "Low", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.611749+05	\N	2026-06-29 23:33:09.068236+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.611805+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064470847489	330038748892266500	DAILY_REPORT_SUBMITTED	Daily report on TSK-00001	<p>Demo Owner filed a daily report on <strong>TSK-00001 — Fix payroll WPS export rounding</strong> for 2026-06-29.</p><p>Worked on it (2026-06-29).</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "", "DueDate": "2026-07-02", "TaskRef": "TSK-00001", "Priority": "Medium", "UserName": "Demo Owner", "NewStatus": "", "OldStatus": "", "TaskTitle": "Fix payroll WPS export rounding", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00001", "AssigneeName": "Sara Khan", "PriorityName": "Medium", "ReporterName": "Demo Owner", "DailyReportDescription": "Worked on it (2026-06-29)."}	Sent	2026-06-29 23:33:02.659372+05	\N	2026-06-29 23:33:09.072789+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.659452+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064869306369	330038748892266500	DAILY_REPORT_SUBMITTED	Daily report on TSK-00007	<p>Demo Owner filed a daily report on <strong>TSK-00007 — Inventory reorder-point alerts</strong> for 2026-06-27.</p><p>Worked on it (2026-06-27).</p>	\N	{"Date": "2026-06-27", "Actor": "Demo Owner", "Status": "", "DueDate": "2026-07-22", "TaskRef": "TSK-00007", "Priority": "Critical", "UserName": "Demo Owner", "NewStatus": "", "OldStatus": "", "TaskTitle": "Inventory reorder-point alerts", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00007", "AssigneeName": "Omar Ali", "PriorityName": "Critical", "ReporterName": "Demo Owner", "DailyReportDescription": "Worked on it (2026-06-27)."}	Sent	2026-06-29 23:33:02.754086+05	\N	2026-06-29 23:33:24.119267+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.754145+05	\N	2026-06-29 23:33:24.127473+05	330038748963569665
330053065032884225	330038748892266500	DAILY_REPORT_SUBMITTED	Daily report on TSK-00010	<p>Demo Owner filed a daily report on <strong>TSK-00010 — Two-factor recovery codes</strong> for 2026-06-28.</p><p>Worked on it (2026-06-28).</p>	\N	{"Date": "2026-06-28", "Actor": "Demo Owner", "Status": "", "DueDate": "2026-06-29", "TaskRef": "TSK-00010", "Priority": "Critical", "UserName": "Demo Owner", "NewStatus": "", "OldStatus": "", "TaskTitle": "Two-factor recovery codes", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00010", "AssigneeName": "Sara Khan", "PriorityName": "Critical", "ReporterName": "Demo Owner", "DailyReportDescription": "Worked on it (2026-06-28)."}	Sent	2026-06-29 23:33:02.793543+05	\N	2026-06-29 23:33:24.127407+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.793602+05	\N	2026-06-29 23:33:24.127473+05	330038748963569665
330053063074144257	330038748862906368	TASK_CREATED	New task TSK-00011: Bank reconciliation import	<p>A new task <strong>TSK-00011 — Bank reconciliation import</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-08-01", "TaskRef": "TSK-00011", "Priority": "Low", "UserName": "Demo Owner", "TaskTitle": "Bank reconciliation import", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00011", "AssigneeName": "Omar Ali", "PriorityName": "Low", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.326504+05	\N	2026-06-29 23:33:09.051735+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.326571+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053063124475905	330038748862906368	TASK_CREATED	New task TSK-00012: Purchase order receiving screen	<p>A new task <strong>TSK-00012 — Purchase order receiving screen</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-07-21", "TaskRef": "TSK-00012", "Priority": "Medium", "UserName": "Demo Owner", "TaskTitle": "Purchase order receiving screen", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00012", "AssigneeName": "Layla Hassan", "PriorityName": "Medium", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.338879+05	\N	2026-06-29 23:33:09.052837+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.338921+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053063271276545	330038748862906368	TASK_CREATED	New task TSK-00015: GOSI contribution calculation	<p>A new task <strong>TSK-00015 — GOSI contribution calculation</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-07-03", "TaskRef": "TSK-00015", "Priority": "Low", "UserName": "Demo Owner", "TaskTitle": "GOSI contribution calculation", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00015", "AssigneeName": "Sara Khan", "PriorityName": "Low", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.373776+05	\N	2026-06-29 23:33:09.054041+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.373856+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053063334191104	330038748862906368	TASK_CREATED	New task TSK-00016: Document ClamAV scan hook	<p>A new task <strong>TSK-00016 — Document ClamAV scan hook</strong> was created by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "DueDate": "2026-07-18", "TaskRef": "TSK-00016", "Priority": "Medium", "UserName": "Demo Owner", "TaskTitle": "Document ClamAV scan hook", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00016", "AssigneeName": "Omar Ali", "PriorityName": "Medium", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.388067+05	\N	2026-06-29 23:33:09.055081+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.388118+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053063501963265	330038748892266497	TASK_OPENED	TSK-00001 has started	<p>Work has started on <strong>TSK-00001 — Fix payroll WPS export rounding</strong> (status In Progress).</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "In Progress", "DueDate": "2026-07-02", "TaskRef": "TSK-00001", "Priority": "Medium", "UserName": "Demo Owner", "NewStatus": "In Progress", "OldStatus": "New", "TaskTitle": "Fix payroll WPS export rounding", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00001", "AssigneeName": "Sara Khan", "PriorityName": "Medium", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.428607+05	\N	2026-06-29 23:33:09.056737+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.428674+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053063711678465	330038748892266497	TASK_OPENED	TSK-00003 has started	<p>Work has started on <strong>TSK-00003 — Onboarding wizard for new workspaces</strong> (status In Progress).</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "In Progress", "DueDate": "2026-07-13", "TaskRef": "TSK-00003", "Priority": "Critical", "UserName": "Demo Owner", "NewStatus": "In Progress", "OldStatus": "New", "TaskTitle": "Onboarding wizard for new workspaces", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00003", "AssigneeName": "Layla Hassan", "PriorityName": "Critical", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.478379+05	\N	2026-06-29 23:33:09.05904+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.478424+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053063908810753	330038748892266499	TASK_COMPLETED	TSK-00006 completed	<p><strong>TSK-00006 — Add Arabic RTL polish to reports</strong> was completed (Done) by Demo Owner.</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "Done", "DueDate": "2026-07-02", "TaskRef": "TSK-00006", "Priority": "High", "UserName": "Demo Owner", "NewStatus": "Done", "OldStatus": "New", "TaskTitle": "Add Arabic RTL polish to reports", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00006", "AssigneeName": "Sara Khan", "PriorityName": "High", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.525823+05	\N	2026-06-29 23:33:09.059971+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.525895+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064093360129	330038748892266497	TASK_OPENED	TSK-00011 has started	<p>Work has started on <strong>TSK-00011 — Bank reconciliation import</strong> (status In Progress).</p>	\N	{"Date": "2026-06-29", "Actor": "Demo Owner", "Status": "In Progress", "DueDate": "2026-08-01", "TaskRef": "TSK-00011", "Priority": "Low", "UserName": "Demo Owner", "NewStatus": "In Progress", "OldStatus": "New", "TaskTitle": "Bank reconciliation import", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00011", "AssigneeName": "Omar Ali", "PriorityName": "Low", "ReporterName": "Demo Owner"}	Sent	2026-06-29 23:33:02.569627+05	\N	2026-06-29 23:33:09.066111+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.569696+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
330053064584093697	330038748892266500	DAILY_REPORT_SUBMITTED	Daily report on TSK-00002	<p>Demo Owner filed a daily report on <strong>TSK-00002 — Design GL period-close workflow</strong> for 2026-06-28.</p><p>Worked on it (2026-06-28).</p>	\N	{"Date": "2026-06-28", "Actor": "Demo Owner", "Status": "", "DueDate": "2026-07-11", "TaskRef": "TSK-00002", "Priority": "High", "UserName": "Demo Owner", "NewStatus": "", "OldStatus": "", "TaskTitle": "Design GL period-close workflow", "ReportDate": "2026-06-29", "ReferenceNo": "TSK-00002", "AssigneeName": "Omar Ali", "PriorityName": "High", "ReporterName": "Demo Owner", "DailyReportDescription": "Worked on it (2026-06-28)."}	Sent	2026-06-29 23:33:02.686812+05	\N	2026-06-29 23:33:09.073934+05	0	5	\N	t	f	330038749018095616	2026-06-29 23:33:02.686868+05	\N	2026-06-29 23:33:09.076645+05	330038748963569665
\.


--
-- Data for Name: status_types; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.status_types (id, code, name, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330038749441720320	TASK_STATUS	Task Status	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
330038749500440579	TASK_PRIORITY	Task Priority	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
\.


--
-- Data for Name: statuses; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.statuses (id, status_type_id, code, name, sort_order, is_initial, is_closed, color, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330038749466886144	330038749441720320	NEW	New	0	t	f	#64748b	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
330038749500440576	330038749441720320	IN_PROGRESS	In Progress	1	f	f	#2563eb	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
330038749500440577	330038749441720320	DONE	Done	2	f	t	#16a34a	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
330038749500440578	330038749441720320	CANCELLED	Cancelled	3	f	t	#dc2626	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
330038749500440580	330038749500440579	LOW	Low	0	f	f	#64748b	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
330038749500440581	330038749500440579	MEDIUM	Medium	1	f	f	#2563eb	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
330038749500440582	330038749500440579	HIGH	High	2	f	f	#d97706	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
330038749500440583	330038749500440579	CRITICAL	Critical	3	f	f	#dc2626	t	f	\N	2026-06-29 22:36:09.704259+05	\N	\N	330038748963569665
\.


--
-- Data for Name: task_events; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.task_events (id, event_id, reference_no, title, description, assignee_id, reporter_id, parent_event_id, priority_status_id, start_at, due_at, estimated_time, actual_time, completion_percent, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053062696656897	330053062696656896	TSK-00005	Customer statement PDF export	Demo task: Customer statement PDF export.	330038749018095616	330038749018095616	\N	330038749500440581	2026-06-27 14:00:00+05	2026-06-27 14:00:00+05	3.60	\N	0	t	f	330038749018095616	2026-06-29 23:33:02.237284+05	\N	\N	330038748963569665
330053062738599937	330053062738599936	TSK-00006	Add Arabic RTL polish to reports	Demo task: Add Arabic RTL polish to reports.	330053061153153024	330038749018095616	\N	330038749500440582	2026-06-27 14:00:00+05	2026-07-02 14:00:00+05	10.10	\N	0	t	f	330038749018095616	2026-06-29 23:33:02.250588+05	\N	\N	330038748963569665
330053062797320192	330053062793125888	TSK-00007	Inventory reorder-point alerts	Demo task: Inventory reorder-point alerts.	330053061354479616	330038749018095616	\N	330038749500440583	2026-06-27 14:00:00+05	2026-07-22 14:00:00+05	4.50	\N	0	t	f	330038749018095616	2026-06-29 23:33:02.264101+05	\N	\N	330038748963569665
330053062860234752	330053062847651840	TSK-00008	Vehicle maintenance reminder job	Demo task: Vehicle maintenance reminder job.	330053061438365696	330038749018095616	\N	330038749500440580	2026-06-27 14:00:00+05	2026-08-03 14:00:00+05	4.00	\N	0	t	f	330038749018095616	2026-06-29 23:33:02.279287+05	\N	\N	330038748963569665
330053063048978433	330053063048978432	TSK-00011	Bank reconciliation import	Demo task: Bank reconciliation import.	330053061354479616	330038749018095616	\N	330038749500440580	2026-06-27 14:00:00+05	2026-08-01 14:00:00+05	3.80	\N	0	t	f	330038749018095616	2026-06-29 23:33:02.326571+05	\N	\N	330038748963569665
330053063250305024	330053063246110721	TSK-00015	GOSI contribution calculation	Demo task: GOSI contribution calculation.	330053061153153024	330038749018095616	\N	330038749500440580	2026-06-27 14:00:00+05	2026-07-03 14:00:00+05	8.20	\N	0	t	f	330038749018095616	2026-06-29 23:33:02.373856+05	\N	\N	330038748963569665
330053063317413889	330053063317413888	TSK-00016	Document ClamAV scan hook	Demo task: Document ClamAV scan hook.	330053061354479616	330038749018095616	\N	330038749500440581	2026-06-27 14:00:00+05	2026-07-18 14:00:00+05	4.80	\N	0	t	f	330038749018095616	2026-06-29 23:33:02.388118+05	\N	\N	330038748963569665
330053063162224641	330053063162224640	TSK-00013	Project budget burn-down chart	Demo task: Project budget burn-down chart.	\N	330038749018095616	\N	330038749500440582	2026-06-27 14:00:00+05	2026-07-15 14:00:00+05	7.90	9.70	60	t	f	330038749018095616	2026-06-29 23:33:02.348489+05	330038749018095616	2026-06-29 23:33:49.832293+05	330038748963569665
330053062935732224	330053062927343616	TSK-00009	Sales quotation approval flow	Demo task: Sales quotation approval flow.	\N	330038749018095616	\N	330038749500440581	2026-06-27 14:00:00+05	2026-07-14 14:00:00+05	19.10	13.50	35	t	f	330038749018095616	2026-06-29 23:33:02.294156+05	330038749018095616	2026-06-29 23:33:49.855623+05	330038748963569665
330053062658908160	330053062654713856	TSK-00004	Optimize dashboard query performance	Demo task: Optimize dashboard query performance.	\N	330038749018095616	\N	330038749500440580	2026-06-27 14:00:00+05	2026-07-10 14:00:00+05	18.50	12.10	90	t	f	330038749018095616	2026-06-29 23:33:02.227874+05	330038749018095616	2026-06-29 23:33:49.877988+05	330038748963569665
330053063199973376	330053063195779073	TSK-00014	Help-desk SLA breach checker	Demo task: Help-desk SLA breach checker.	330038749018095616	330038749018095616	\N	330038749500440583	2026-06-27 14:00:00+05	2026-06-27 14:00:00+05	12.20	10.70	75	t	f	330038749018095616	2026-06-29 23:33:02.357933+05	330038749018095616	2026-06-29 23:33:49.90498+05	330038748963569665
330053061857796096	330053061811658752	TSK-00001	Fix payroll WPS export rounding	Demo task: Fix payroll WPS export rounding.	330053061153153024	330038749018095616	\N	330038749500440581	2026-06-27 14:00:00+05	2026-07-02 14:00:00+05	14.10	12.40	90	t	f	330038749018095616	2026-06-29 23:33:02.115752+05	330038749018095616	2026-06-29 23:33:49.927884+05	330038748963569665
330053062981869569	330053062981869568	TSK-00010	Two-factor recovery codes	Demo task: Two-factor recovery codes.	330053061153153024	330038749018095616	\N	330038749500440583	2026-06-27 14:00:00+05	2026-06-29 14:00:00+05	12.80	15.20	35	t	f	330038749018095616	2026-06-29 23:33:02.308479+05	330038749018095616	2026-06-29 23:33:49.944739+05	330038748963569665
330053062533079040	330053062528884736	TSK-00003	Onboarding wizard for new workspaces	Demo task: Onboarding wizard for new workspaces.	330053061438365696	330038749018095616	\N	330038749500440583	2026-06-27 14:00:00+05	2026-07-13 14:00:00+05	9.20	9.30	75	t	f	330038749018095616	2026-06-29 23:33:02.207218+05	330038749018095616	2026-06-29 23:33:49.967671+05	330038748963569665
330053063116087296	330053063111892992	TSK-00012	Purchase order receiving screen	Demo task: Purchase order receiving screen.	330053061438365696	330038749018095616	\N	330038749500440581	2026-06-27 14:00:00+05	2026-07-21 14:00:00+05	3.80	3.10	20	t	f	330038749018095616	2026-06-29 23:33:02.338921+05	330038749018095616	2026-06-29 23:33:49.984554+05	330038748963569665
330053062444998656	330053062440804352	TSK-00002	Design GL period-close workflow	Demo task: Design GL period-close workflow.	330053061354479616	330038749018095616	\N	330038749500440582	2026-06-27 14:00:00+05	2026-07-11 14:00:00+05	17.00	17.60	20	t	f	330038749018095616	2026-06-29 23:33:02.183822+05	330038749018095616	2026-06-29 23:33:50.010248+05	330038748963569665
\.


--
-- Data for Name: task_settings; Type: TABLE DATA; Schema: bpm; Owner: -
--

COPY bpm.task_settings (id, daily_report_required, allow_status_change_from_report, require_actual_time, require_estimated_time, allow_multiple_reports_per_day, notify_on_task_created, notify_on_task_assigned, notify_on_status_change, notify_on_daily_report, dashboard_default_range_days, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" (migration_id, product_version) FROM stdin;
20260629172353_InitialCreate	10.0.9
20260629175018_AddTaskReportFunctions	10.0.9
20260629181711_AddTaskListFunction	10.0.9
20260629221555_AlignDomainNamespaces	10.0.9
20260629222152_RenameTaskReportFunctions	10.0.9
\.


--
-- Data for Name: audit_logs_2026_05; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.audit_logs_2026_05 (id, workspace_id, organization_id, cluster_id, occurred_at, correlation_id, actor_user_id, actor_display_name, module, resource_type, resource_id, action, old_values, new_values, ip_address, user_agent, result, source, reason, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date) FROM stdin;
\.


--
-- Data for Name: audit_logs_2026_06; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.audit_logs_2026_06 (id, workspace_id, organization_id, cluster_id, occurred_at, correlation_id, actor_user_id, actor_display_name, module, resource_type, resource_id, action, old_values, new_values, ip_address, user_agent, result, source, reason, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date) FROM stdin;
330052016284917760	330038748963569665	\N	\N	2026-06-29 23:28:52.752022+05	a4daa26d39b843599bcdd942f21ec817	330038749018095616	owner@demo.test	Identity	User	330038749018095616	LOGIN	\N	\N	\N	\N	Success	Api	\N	t	f	\N	2026-06-29 23:28:52.784742+05	\N	\N
330052513507074048	330038748963569665	\N	\N	2026-06-29 23:30:51.299525+05	da8457ae7eb640f58a1d4cd791b598b5	330038749018095616	owner@demo.test	Identity	User	330038749018095616	FAILED_LOGIN	\N	\N	\N	\N	Failed	Api	Invalid password	t	f	\N	2026-06-29 23:30:51.300038+05	\N	\N
330052536353447937	330038748963569665	\N	\N	2026-06-29 23:30:56.746468+05	f14fe4ff11e34ac68dfec0044acd0e91	330038749018095616	owner@demo.test	Identity	User	330038749018095616	LOGIN	\N	\N	\N	\N	Success	Api	\N	t	f	\N	2026-06-29 23:30:56.746563+05	\N	\N
330053060968603649	330038748963569665	\N	\N	2026-06-29 23:33:01.824473+05	eec42eea2ff945a186fd560ee5b476e9	330038749018095616	owner@demo.test	Identity	User	330038749018095616	LOGIN	\N	\N	\N	\N	Success	Api	\N	t	f	\N	2026-06-29 23:33:01.824624+05	\N	\N
330053061190901760	330038748963569665	\N	\N	2026-06-29 23:33:01.877436+05	1f1657acc42c4ac999e6a664a72ea68c	330038749018095616	owner@demo.test	Identity	User	330053061153153024	CREATE	\N	{"email": "sara@demo.test", "status": "PendingInvitation"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:01.877505+05	\N	\N
330053061354479618	330038748963569665	\N	\N	2026-06-29 23:33:01.916281+05	8012895f298447dba7a97c482a42cd48	330038749018095616	owner@demo.test	Identity	User	330053061354479616	CREATE	\N	{"email": "omar@demo.test", "status": "PendingInvitation"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:01.916333+05	\N	\N
330053061438365698	330038748963569665	\N	\N	2026-06-29 23:33:01.936434+05	1ffff1c9ae8e490c991e5d4cad234cd1	330038749018095616	owner@demo.test	Identity	User	330053061438365696	CREATE	\N	{"email": "layla@demo.test", "status": "PendingInvitation"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:01.936483+05	\N	\N
330053061962653696	330038748963569665	\N	\N	2026-06-29 23:33:02.061631+05	e30bfb444135464580b8e66e13baaff3	330038749018095616	owner@demo.test	Tasks	Task	330053061811658752	CREATE	\N	{"reference": "TSK-00001"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.115752+05	\N	\N
330053062444998659	330038748963569665	\N	\N	2026-06-29 23:33:02.176278+05	26e6032793284558ac031a8ebc55c921	330038749018095616	owner@demo.test	Tasks	Task	330053062440804352	CREATE	\N	{"reference": "TSK-00002"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.183822+05	\N	\N
330053062533079043	330038748963569665	\N	\N	2026-06-29 23:33:02.197496+05	86ced37765284079a9833b5f3cf137b4	330038749018095616	owner@demo.test	Tasks	Task	330053062528884736	CREATE	\N	{"reference": "TSK-00003"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.207218+05	\N	\N
330053062658908163	330038748963569665	\N	\N	2026-06-29 23:33:02.227313+05	2b7708ee64ba44e6b64ce1de3bb0563c	330038749018095616	owner@demo.test	Tasks	Task	330053062654713856	CREATE	\N	{"reference": "TSK-00004"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.227874+05	\N	\N
330053062696656900	330038748963569665	\N	\N	2026-06-29 23:33:02.236798+05	b610d68cebb3444fa83e82ece4a053c1	330038749018095616	owner@demo.test	Tasks	Task	330053062696656896	CREATE	\N	{"reference": "TSK-00005"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.237284+05	\N	\N
330053062738599940	330038748963569665	\N	\N	2026-06-29 23:33:02.246735+05	fdc1dcaecad6493288eebaa17fb8804a	330038749018095616	owner@demo.test	Tasks	Task	330053062738599936	CREATE	\N	{"reference": "TSK-00006"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.250588+05	\N	\N
330053062797320195	330038748963569665	\N	\N	2026-06-29 23:33:02.260393+05	4ba0d6b24e4d457b9f2824319e7f3c02	330038749018095616	owner@demo.test	Tasks	Task	330053062793125888	CREATE	\N	{"reference": "TSK-00007"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.264101+05	\N	\N
330053062860234755	330038748963569665	\N	\N	2026-06-29 23:33:02.275852+05	cd7c4b113b4a41e9a0e27d8e2b8a53c0	330038749018095616	owner@demo.test	Tasks	Task	330053062847651840	CREATE	\N	{"reference": "TSK-00008"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.279287+05	\N	\N
330053062935732227	330038748963569665	\N	\N	2026-06-29 23:33:02.29352+05	e0bd6389215f4c499680f3cbb71db07d	330038749018095616	owner@demo.test	Tasks	Task	330053062927343616	CREATE	\N	{"reference": "TSK-00009"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.294156+05	\N	\N
330053062981869572	330038748963569665	\N	\N	2026-06-29 23:33:02.304799+05	637888207e90455ab010b4787105f06a	330038749018095616	owner@demo.test	Tasks	Task	330053062981869568	CREATE	\N	{"reference": "TSK-00010"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.308479+05	\N	\N
330053063048978436	330038748963569665	\N	\N	2026-06-29 23:33:02.320825+05	60e20ffb08f2484184c9d34db1c85dc0	330038749018095616	owner@demo.test	Tasks	Task	330053063048978432	CREATE	\N	{"reference": "TSK-00011"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.326571+05	\N	\N
330053063116087299	330038748963569665	\N	\N	2026-06-29 23:33:02.33608+05	a21d1171a1f54dddb0fef79a87b8522b	330038749018095616	owner@demo.test	Tasks	Task	330053063111892992	CREATE	\N	{"reference": "TSK-00012"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.338921+05	\N	\N
330053063162224644	330038748963569665	\N	\N	2026-06-29 23:33:02.347947+05	c7fe07edf7e242eab07f28d7649faa22	330038749018095616	owner@demo.test	Tasks	Task	330053063162224640	CREATE	\N	{"reference": "TSK-00013"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.348489+05	\N	\N
330053063199973379	330038748963569665	\N	\N	2026-06-29 23:33:02.356879+05	7ffcd32dfa144697927a42a50b5f5689	330038749018095616	owner@demo.test	Tasks	Task	330053063195779073	CREATE	\N	{"reference": "TSK-00014"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.357933+05	\N	\N
330053063250305027	330038748963569665	\N	\N	2026-06-29 23:33:02.3683+05	2f159e9b8a3342c28ba72585be7a76ac	330038749018095616	owner@demo.test	Tasks	Task	330053063246110721	CREATE	\N	{"reference": "TSK-00015"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.373856+05	\N	\N
330053063321608194	330038748963569665	\N	\N	2026-06-29 23:33:02.385059+05	3f42e2f058fa4848b8573f954c5bdb64	330038749018095616	owner@demo.test	Tasks	Task	330053063317413888	CREATE	\N	{"reference": "TSK-00016"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.388118+05	\N	\N
330053063485186048	330038748963569665	\N	\N	2026-06-29 23:33:02.424278+05	e98fb6b7ebd2400ebb2975b35798db3a	330038749018095616	owner@demo.test	Tasks	Task	330053061811658752	UPDATE	\N	{"status": "In Progress"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.428674+05	\N	\N
330053063585849347	330038748963569665	\N	\N	2026-06-29 23:33:02.448804+05	9adc5102c4d14dc49784b0169aef8201	330038749018095616	owner@demo.test	Tasks	Task	330053062440804352	UPDATE	\N	{"status": "In Progress"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.459474+05	\N	\N
330053063694901251	330038748963569665	\N	\N	2026-06-29 23:33:02.474357+05	bf71b90bc06e4ebcaa353858ce7234f0	330038749018095616	owner@demo.test	Tasks	Task	330053062528884736	UPDATE	\N	{"status": "In Progress"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.478424+05	\N	\N
330053063778787332	330038748963569665	\N	\N	2026-06-29 23:33:02.494785+05	ad6e44c539ff4bf586f377de5c8b5af8	330038749018095616	owner@demo.test	Tasks	Task	330053062654713856	UPDATE	\N	{"status": "In Progress"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.495398+05	\N	\N
330053063824924676	330038748963569665	\N	\N	2026-06-29 23:33:02.505504+05	23e34f324f4241c188833bb53e25ebac	330038749018095616	owner@demo.test	Tasks	Task	330053062696656896	UPDATE	\N	{"status": "Done"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.507218+05	\N	\N
330053063879450625	330038748963569665	\N	\N	2026-06-29 23:33:02.518023+05	a786aa15275247dc8898f8a3cdf59ac3	330038749018095616	owner@demo.test	Tasks	Task	330053062738599936	UPDATE	\N	{"status": "Done"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.525895+05	\N	\N
330053063942365188	330038748963569665	\N	\N	2026-06-29 23:33:02.533477+05	60e4f8070a6144929dc20181cd9e91b1	330038749018095616	owner@demo.test	Tasks	Task	330053062793125888	UPDATE	\N	{"status": "Done"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.535725+05	\N	\N
330053063992696836	330038748963569665	\N	\N	2026-06-29 23:33:02.545943+05	e020d41df28a4f40ba9d01cde1422e74	330038749018095616	owner@demo.test	Tasks	Task	330053062847651840	UPDATE	\N	{"status": "Cancelled"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.550707+05	\N	\N
330053064059805699	330038748963569665	\N	\N	2026-06-29 23:33:02.561327+05	55c9a3cf030444889b798b6e4a666a3c	330038749018095616	owner@demo.test	Tasks	Task	330053063048978432	UPDATE	\N	{"status": "In Progress"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.569696+05	\N	\N
330053064131108867	330038748963569665	\N	\N	2026-06-29 23:33:02.578505+05	0575d471775445e184bd6e62bbe8a7e4	330038749018095616	owner@demo.test	Tasks	Task	330053063111892992	UPDATE	\N	{"status": "In Progress"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.582886+05	\N	\N
330053064194023429	330038748963569665	\N	\N	2026-06-29 23:33:02.59385+05	c5a3379c99074083bb1eb04b93d96021	330038749018095616	owner@demo.test	Tasks	Task	330053063162224640	UPDATE	\N	{"status": "In Progress"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.594198+05	\N	\N
330053064223383555	330038748963569665	\N	\N	2026-06-29 23:33:02.60017+05	c08c7668867e428ebdc9386155ad6e0c	330038749018095616	owner@demo.test	Tasks	Task	330053063195779073	UPDATE	\N	{"status": "In Progress"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.600483+05	\N	\N
330053064261132290	330038748963569665	\N	\N	2026-06-29 23:33:02.60907+05	cc58d5d8fa444b44b5df9fb68d634d2f	330038749018095616	owner@demo.test	Tasks	Task	330053063246110721	UPDATE	\N	{"status": "Done"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.611805+05	\N	\N
330053064294686725	330038748963569665	\N	\N	2026-06-29 23:33:02.617966+05	ef8acf913d15475a820c3951a94b89e5	330038749018095616	owner@demo.test	Tasks	Task	330053063317413888	UPDATE	\N	{"status": "Done"}	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.625028+05	\N	\N
330053064638619650	330038748963569665	\N	\N	2026-06-29 23:33:02.699743+05	440884679d0e4067889cd578e31e0cb1	330038749018095616	owner@demo.test	Tasks	Task	330053062528884736	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.709057+05	\N	\N
330053064802197507	330038748963569665	\N	\N	2026-06-29 23:33:02.738903+05	3b0574912d2746999b29771ce9f8d51f	330038749018095616	owner@demo.test	Tasks	Task	330053062738599936	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.743241+05	\N	\N
330053064969969666	330038748963569665	\N	\N	2026-06-29 23:33:02.778227+05	92ec084134e34bdf9c172186ad77d007	330038749018095616	owner@demo.test	Tasks	Task	330053062927343616	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.778267+05	\N	\N
330053065204850689	330038748963569665	\N	\N	2026-06-29 23:33:02.834892+05	136a426abc4f49008fe23104d8fa42cf	330038749018095616	owner@demo.test	Tasks	Task	330053061811658752	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.834934+05	\N	\N
330053065397788676	330038748963569665	\N	\N	2026-06-29 23:33:02.880365+05	b87a3b040c734f3bbafcc0a1cdafb30f	330038749018095616	owner@demo.test	Tasks	Task	330053062654713856	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.880396+05	\N	\N
330053064454070273	330038748963569665	\N	\N	2026-06-29 23:33:02.655865+05	b2cc7badf2544af1a91dcb18cad0cfb9	330038749018095616	owner@demo.test	Tasks	Task	330053061811658752	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.659452+05	\N	\N
330053064718311426	330038748963569665	\N	\N	2026-06-29 23:33:02.718446+05	8c052b7f44424006a19f6b0ce7ad4dbb	330038749018095616	owner@demo.test	Tasks	Task	330053062654713856	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.718486+05	\N	\N
330053064852529154	330038748963569665	\N	\N	2026-06-29 23:33:02.750378+05	206e9dfadf4d4c82894d9c376e6fc317	330038749018095616	owner@demo.test	Tasks	Task	330053062793125888	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.754145+05	\N	\N
330053065011912706	330038748963569665	\N	\N	2026-06-29 23:33:02.788819+05	bdacde9f861c480e85201df0e31bd3b1	330038749018095616	owner@demo.test	Tasks	Task	330053062981869568	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.793602+05	\N	\N
330053065355845636	330038748963569665	\N	\N	2026-06-29 23:33:02.870585+05	608c688e434e477b9dd775f00d8289f9	330038749018095616	owner@demo.test	Tasks	Task	330053062528884736	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.870606+05	\N	\N
330053065485869060	330038748963569665	\N	\N	2026-06-29 23:33:02.901722+05	db448104c20243428c555d313ccc32c1	330038749018095616	owner@demo.test	Tasks	Task	330053062738599936	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.901739+05	\N	\N
330053064571510786	330038748963569665	\N	\N	2026-06-29 23:33:02.683958+05	13f04f9a82514ad3a5f9bc2c8fa348a0	330038749018095616	owner@demo.test	Tasks	Task	330053062440804352	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.686868+05	\N	\N
330053064756060163	330038748963569665	\N	\N	2026-06-29 23:33:02.727704+05	dc9c1ff74a3b4a5b94f2e37dcd52a23f	330038749018095616	owner@demo.test	Tasks	Task	330053062696656896	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.727731+05	\N	\N
330053064902860803	330038748963569665	\N	\N	2026-06-29 23:33:02.762586+05	5d4f1ca110124a47aed85a1adc838627	330038749018095616	owner@demo.test	Tasks	Task	330053062847651840	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.765663+05	\N	\N
330053065305513988	330038748963569665	\N	\N	2026-06-29 23:33:02.858909+05	76058a4446ac48c3bfb9be4059cc70a7	330038749018095616	owner@demo.test	Tasks	Task	330053062440804352	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.858933+05	\N	\N
330053065443926020	330038748963569665	\N	\N	2026-06-29 23:33:02.891743+05	84bebbd9314a4b069a5bf032b9eed0a0	330038749018095616	owner@demo.test	Tasks	Task	330053062696656896	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:02.891764+05	\N	\N
330053261938679808	330038748963569665	\N	\N	2026-06-29 23:33:49.739032+05	c7de4d3e39a248c48b93154714900afe	330038749018095616	owner@demo.test	Identity	User	330038749018095616	LOGIN	\N	\N	\N	\N	Success	Api	\N	t	f	\N	2026-06-29 23:33:49.739177+05	\N	\N
330053262328750081	330038748963569665	\N	\N	2026-06-29 23:33:49.832253+05	520b2cb5d5b642baa3c0fa45e7a4c8c9	330038749018095616	owner@demo.test	Tasks	Task	330053063162224640	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:49.832293+05	\N	\N
330053262425219073	330038748963569665	\N	\N	2026-06-29 23:33:49.855595+05	9660da4cb19d457f8c44c6d4df848e1a	330038749018095616	owner@demo.test	Tasks	Task	330053062927343616	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:49.855623+05	\N	\N
330053262517493761	330038748963569665	\N	\N	2026-06-29 23:33:49.877956+05	2f52ea98d3c14349804c32f143064c4e	330038749018095616	owner@demo.test	Tasks	Task	330053062654713856	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:49.877988+05	\N	\N
330053262630739969	330038748963569665	\N	\N	2026-06-29 23:33:49.904943+05	5933935e336648729d6c9c6fe0bee61c	330038749018095616	owner@demo.test	Tasks	Task	330053063195779073	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:49.90498+05	\N	\N
330053262727208961	330038748963569665	\N	\N	2026-06-29 23:33:49.927841+05	05c6b90cc0324f9eacc630531b54f43b	330038749018095616	owner@demo.test	Tasks	Task	330053061811658752	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:49.927884+05	\N	\N
330053262798512129	330038748963569665	\N	\N	2026-06-29 23:33:49.944706+05	74874f0867364f7e8910d44311aae5b5	330038749018095616	owner@demo.test	Tasks	Task	330053062981869568	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:49.944739+05	\N	\N
330053262894981121	330038748963569665	\N	\N	2026-06-29 23:33:49.967639+05	50613029f1c04c4bb8e30d1a6dd346b9	330038749018095616	owner@demo.test	Tasks	Task	330053062528884736	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:49.967671+05	\N	\N
330053262966284289	330038748963569665	\N	\N	2026-06-29 23:33:49.984521+05	9aa357dfa7a24c1598c94fd3e72bebec	330038749018095616	owner@demo.test	Tasks	Task	330053063111892992	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:49.984554+05	\N	\N
330053263075336193	330038748963569665	\N	\N	2026-06-29 23:33:50.010216+05	209303d1e1944079b47d7c0e52dbcb45	330038749018095616	owner@demo.test	Tasks	Task	330053062440804352	UPDATE	\N	\N	\N	\N	Success	Api	\N	t	f	330038749018095616	2026-06-29 23:33:50.010248+05	\N	\N
\.


--
-- Data for Name: audit_logs_2026_07; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.audit_logs_2026_07 (id, workspace_id, organization_id, cluster_id, occurred_at, correlation_id, actor_user_id, actor_display_name, module, resource_type, resource_id, action, old_values, new_values, ip_address, user_agent, result, source, reason, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date) FROM stdin;
\.


--
-- Data for Name: employees; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.employees (id, user_id, employee_number, job_title, mobile, placement_node_id, manager_id, hire_date, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053061153153025	330053061153153024	\N	Project Lead	\N	\N	\N	\N	t	f	330038749018095616	2026-06-29 23:33:01.877505+05	\N	\N	330038748963569665
330053061354479617	330053061354479616	\N	Senior Engineer	\N	\N	\N	\N	t	f	330038749018095616	2026-06-29 23:33:01.916333+05	\N	\N	330038748963569665
330053061438365697	330053061438365696	\N	QA Analyst	\N	\N	\N	\N	t	f	330038749018095616	2026-06-29 23:33:01.936483+05	\N	\N	330038748963569665
\.


--
-- Data for Name: password_reset_tokens; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.password_reset_tokens (id, user_id, token_hash, expires_at, used_at, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
\.


--
-- Data for Name: permissions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.permissions (id, code, module, resource, action, is_high_risk, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date) FROM stdin;
330038748682551296	admin.overview.view	Admin	Dashboard	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911424	user.view	Users	User	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911425	user.manage	Users	User	Manage	t	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911426	user.invite	Users	Invitation	Create	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911427	user.export	Users	User	Export	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911428	role.view	AccessControl	Role	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911429	role.manage	AccessControl	Role	Manage	t	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911430	structure.view	BusinessStructure	Structure	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911431	structure.manage	BusinessStructure	Structure	Manage	t	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911432	security.view	Security	Security	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911433	security.manage	Security	Security	Manage	t	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911434	audit.view	Audit	AuditLog	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911435	audit.export	Audit	AuditLog	Export	t	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911436	settings.view	Settings	Setting	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911437	settings.manage	Settings	Setting	Manage	t	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911438	task.view	Tasks	Task	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911439	task.create	Tasks	Task	Create	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911440	task.update	Tasks	Task	Update	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911441	task.assign	Tasks	Task	Assign	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911442	task.change-status	Tasks	Task	ChangeStatus	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911443	task.archive	Tasks	Task	Archive	t	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911444	task.workflow.manage	Tasks	Workflow	Manage	t	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911445	task.audit.view	Tasks	AuditLog	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911446	task.note.manage	Tasks	Note	Manage	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911447	task.document.manage	Tasks	Document	Manage	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911448	task.daily-report.manage	Tasks	DailyReport	Manage	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911449	mail.view	Mail	Outbox	View	f	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
330038748711911450	mail.manage	Mail	Outbox	Manage	t	t	f	\N	2026-06-29 22:36:09.516971+05	\N	\N
\.


--
-- Data for Name: refresh_tokens; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.refresh_tokens (id, user_id, token_hash, expires_at, revoked_at, replaced_by_token_id, created_by_ip, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330052016226197504	330038749018095616	ebddeec4bce9518950aa2a0e76d6d59f6641ebaa9328615c24dcedc734284a62	2026-07-29 23:28:52.548178+05	\N	\N	::1	t	f	\N	2026-06-29 23:28:52.784742+05	\N	\N	330038748963569665
330053060968603648	330038749018095616	2cfb28bf118df96949e160aa81f9d68fab717ca74260012f17c4cc675a31a33c	2026-07-29 23:33:01.755099+05	\N	\N	::1	t	f	\N	2026-06-29 23:33:01.824624+05	\N	\N	330038748963569665
330053261934485504	330038749018095616	61883f8cc5f878797dfa64d4422702c9316946a12b5c671b6f449e95e8cd77a9	2026-07-29 23:33:49.687452+05	\N	\N	::1	t	f	\N	2026-06-29 23:33:49.739177+05	\N	\N	330038748963569665
330052536353447936	330038749018095616	e0bdafac3fdd986805d65f1cae7f03daa9215c42fc83fca7948676722710cde0	2026-07-29 23:30:56.681153+05	2026-06-30 00:02:17.612234+05	330060425432424448	::1	t	f	\N	2026-06-29 23:30:56.746563+05	\N	2026-06-30 00:02:17.650094+05	330038748963569665
330060425432424448	330038749018095616	5151ca1644a31f1f7495ff14b69aa120a5e7f408ff21c86acbe9628fc1d9141a	2026-07-30 00:02:17.612234+05	2026-06-30 00:34:54.190559+05	330068631881801728	::1	t	f	\N	2026-06-30 00:02:17.650094+05	\N	2026-06-30 00:34:54.219323+05	330038748963569665
330068631881801728	330038749018095616	28df44c15c66bb16619dbcd2df28e518bed6d00db3c4bd5d346cf0671a5ebb54	2026-07-30 00:34:54.190559+05	\N	\N	::1	t	f	\N	2026-06-30 00:34:54.219323+05	\N	\N	330038748963569665
\.


--
-- Data for Name: role_permissions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.role_permissions (id, role_id, permission_id, scope, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330038749332668417	330038749320085504	330038748682551296	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749332668418	330038749320085504	330038748711911424	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749332668419	330038749320085504	330038748711911425	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862720	330038749320085504	330038748711911426	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862721	330038749320085504	330038748711911427	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862722	330038749320085504	330038748711911428	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862723	330038749320085504	330038748711911429	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862724	330038749320085504	330038748711911430	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862725	330038749320085504	330038748711911431	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862726	330038749320085504	330038748711911432	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862727	330038749320085504	330038748711911433	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862728	330038749320085504	330038748711911434	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862729	330038749320085504	330038748711911435	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862730	330038749320085504	330038748711911436	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862731	330038749320085504	330038748711911437	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862732	330038749320085504	330038748711911438	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862733	330038749320085504	330038748711911439	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862734	330038749320085504	330038748711911440	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862735	330038749320085504	330038748711911441	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862736	330038749320085504	330038748711911442	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862737	330038749320085504	330038748711911443	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862738	330038749320085504	330038748711911444	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862739	330038749320085504	330038748711911445	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862740	330038749320085504	330038748711911446	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862741	330038749320085504	330038748711911447	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862742	330038749320085504	330038748711911448	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862743	330038749320085504	330038748711911449	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
330038749336862744	330038749320085504	330038748711911450	Workspace	t	f	\N	2026-06-29 22:36:09.665168+05	\N	\N	330038748963569665
\.


--
-- Data for Name: roles; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.roles (id, name, code, description, type, color, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330038749320085504	Workspace Owner	workspace-owner	Full administrative access to the workspace.	System	\N	t	f	\N	2026-06-29 22:36:09.66203+05	\N	\N	330038748963569665
\.


--
-- Data for Name: structure_nodes; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.structure_nodes (id, parent_id, node_type, name, code, description, manager_id, sort_order, status, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
\.


--
-- Data for Name: user_permissions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.user_permissions (id, user_id, permission_id, effect, scope, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
\.


--
-- Data for Name: user_roles; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.user_roles (id, user_id, role_id, cluster_id, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330038749357834241	330038749018095616	330038749320085504	\N	t	f	\N	2026-06-29 22:36:09.675789+05	\N	\N	330038748963569665
\.


--
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.users (id, email, normalized_email, password_hash, security_stamp, require_password_change, first_name, last_name, display_name, preferred_language, time_zone, avatar_url, status, access_start_date, access_expiry_date, last_login_at, access_failed_count, lockout_ends_at, two_factor_enabled, two_factor_secret, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
330053061153153024	sara@demo.test	SARA@DEMO.TEST	\N	f03c5cc00ae246fba26cc1985dc1971b	f	Sara	Khan	Sara Khan	en	Asia/Riyadh	\N	PendingInvitation	\N	\N	\N	0	\N	f	\N	t	f	330038749018095616	2026-06-29 23:33:01.877505+05	\N	\N	330038748963569665
330053061354479616	omar@demo.test	OMAR@DEMO.TEST	\N	27eaaec9ffef4cb7920f15231dd6a0b0	f	Omar	Ali	Omar Ali	en	Asia/Riyadh	\N	PendingInvitation	\N	\N	\N	0	\N	f	\N	t	f	330038749018095616	2026-06-29 23:33:01.916333+05	\N	\N	330038748963569665
330053061438365696	layla@demo.test	LAYLA@DEMO.TEST	\N	ed13fc009a904cbd8fc7af26e84a4b40	f	Layla	Hassan	Layla Hassan	en	Asia/Riyadh	\N	PendingInvitation	\N	\N	\N	0	\N	f	\N	t	f	330038749018095616	2026-06-29 23:33:01.936483+05	\N	\N	330038748963569665
330038749018095616	owner@demo.test	OWNER@DEMO.TEST	AQAAAAIAAYagAAAAEFCxaQdYxvA15/iO7MzAkiYKADfWdVy4RpDmffCZR/Gy8/Kw8JlH+GcGg+jlRYDYTQ==	bdb6ab32865f44e1a3a3946f2027ddd3	f	Demo	Owner	Demo Owner	en	Asia/Riyadh	\N	Active	\N	\N	2026-06-29 23:33:49.687452+05	0	\N	f	\N	t	f	\N	2026-06-29 22:36:09.647943+05	\N	2026-06-29 23:33:49.739177+05	330038748963569665
\.


--
-- Data for Name: workspace_security_policies; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.workspace_security_policies (id, password_min_length, require_uppercase, require_lowercase, require_digit, require_symbol, password_expiry_days, max_failed_attempts, lockout_minutes, session_idle_timeout_minutes, refresh_token_days, require_two_factor, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date, workspace_id) FROM stdin;
\.


--
-- Data for Name: workspaces; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.workspaces (id, name, slug, legal_name, default_language, time_zone, base_currency, country, status, is_active, is_deleted, inserted_by, inserted_date, changed_by, changed_date) FROM stdin;
330038748963569665	WS demo	demo	\N	en	Asia/Riyadh	SAR	\N	Active	t	f	\N	2026-06-29 22:36:09.647943+05	\N	\N
\.


--
-- Name: asset_types pk_asset_types; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.asset_types
    ADD CONSTRAINT pk_asset_types PRIMARY KEY (id);


--
-- Name: assets pk_assets; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.assets
    ADD CONSTRAINT pk_assets PRIMARY KEY (id);


--
-- Name: documents pk_documents; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.documents
    ADD CONSTRAINT pk_documents PRIMARY KEY (id);


--
-- Name: event_activities pk_event_activities; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_activities
    ADD CONSTRAINT pk_event_activities PRIMARY KEY (id);


--
-- Name: event_assets pk_event_assets; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_assets
    ADD CONSTRAINT pk_event_assets PRIMARY KEY (id);


--
-- Name: event_daily_reports pk_event_daily_reports; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_daily_reports
    ADD CONSTRAINT pk_event_daily_reports PRIMARY KEY (id);


--
-- Name: event_dependencies pk_event_dependencies; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_dependencies
    ADD CONSTRAINT pk_event_dependencies PRIMARY KEY (id);


--
-- Name: event_statuses pk_event_statuses; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_statuses
    ADD CONSTRAINT pk_event_statuses PRIMARY KEY (id);


--
-- Name: event_types pk_event_types; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_types
    ADD CONSTRAINT pk_event_types PRIMARY KEY (id);


--
-- Name: events pk_events; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.events
    ADD CONSTRAINT pk_events PRIMARY KEY (id);


--
-- Name: mail_templates pk_mail_templates; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.mail_templates
    ADD CONSTRAINT pk_mail_templates PRIMARY KEY (id);


--
-- Name: notes pk_notes; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.notes
    ADD CONSTRAINT pk_notes PRIMARY KEY (id);


--
-- Name: send_mail_attempts pk_send_mail_attempts; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.send_mail_attempts
    ADD CONSTRAINT pk_send_mail_attempts PRIMARY KEY (id);


--
-- Name: send_mail_recipients pk_send_mail_recipients; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.send_mail_recipients
    ADD CONSTRAINT pk_send_mail_recipients PRIMARY KEY (id);


--
-- Name: send_mails pk_send_mails; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.send_mails
    ADD CONSTRAINT pk_send_mails PRIMARY KEY (id);


--
-- Name: status_types pk_status_types; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.status_types
    ADD CONSTRAINT pk_status_types PRIMARY KEY (id);


--
-- Name: statuses pk_statuses; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.statuses
    ADD CONSTRAINT pk_statuses PRIMARY KEY (id);


--
-- Name: task_events pk_task_events; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.task_events
    ADD CONSTRAINT pk_task_events PRIMARY KEY (id);


--
-- Name: task_settings pk_task_settings; Type: CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.task_settings
    ADD CONSTRAINT pk_task_settings PRIMARY KEY (id);


--
-- Name: audit_logs pk_audit_logs; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT pk_audit_logs PRIMARY KEY (id, occurred_at);


--
-- Name: audit_logs_2026_05 audit_logs_2026_05_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs_2026_05
    ADD CONSTRAINT audit_logs_2026_05_pkey PRIMARY KEY (id, occurred_at);


--
-- Name: audit_logs_2026_06 audit_logs_2026_06_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs_2026_06
    ADD CONSTRAINT audit_logs_2026_06_pkey PRIMARY KEY (id, occurred_at);


--
-- Name: audit_logs_2026_07 audit_logs_2026_07_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs_2026_07
    ADD CONSTRAINT audit_logs_2026_07_pkey PRIMARY KEY (id, occurred_at);


--
-- Name: __EFMigrationsHistory pk___ef_migrations_history; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id);


--
-- Name: employees pk_employees; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.employees
    ADD CONSTRAINT pk_employees PRIMARY KEY (id);


--
-- Name: password_reset_tokens pk_password_reset_tokens; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.password_reset_tokens
    ADD CONSTRAINT pk_password_reset_tokens PRIMARY KEY (id);


--
-- Name: permissions pk_permissions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions
    ADD CONSTRAINT pk_permissions PRIMARY KEY (id);


--
-- Name: refresh_tokens pk_refresh_tokens; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.refresh_tokens
    ADD CONSTRAINT pk_refresh_tokens PRIMARY KEY (id);


--
-- Name: role_permissions pk_role_permissions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT pk_role_permissions PRIMARY KEY (id);


--
-- Name: roles pk_roles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT pk_roles PRIMARY KEY (id);


--
-- Name: structure_nodes pk_structure_nodes; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.structure_nodes
    ADD CONSTRAINT pk_structure_nodes PRIMARY KEY (id);


--
-- Name: user_permissions pk_user_permissions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_permissions
    ADD CONSTRAINT pk_user_permissions PRIMARY KEY (id);


--
-- Name: user_roles pk_user_roles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT pk_user_roles PRIMARY KEY (id);


--
-- Name: users pk_users; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT pk_users PRIMARY KEY (id);


--
-- Name: workspace_security_policies pk_workspace_security_policies; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.workspace_security_policies
    ADD CONSTRAINT pk_workspace_security_policies PRIMARY KEY (id);


--
-- Name: workspaces pk_workspaces; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.workspaces
    ADD CONSTRAINT pk_workspaces PRIMARY KEY (id);


--
-- Name: ix_asset_types_code; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_asset_types_code ON bpm.asset_types USING btree (code);


--
-- Name: ix_assets_asset_type_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_assets_asset_type_id ON bpm.assets USING btree (asset_type_id);


--
-- Name: ix_assets_workspace_id_asset_type_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_assets_workspace_id_asset_type_id ON bpm.assets USING btree (workspace_id, asset_type_id);


--
-- Name: ix_documents_asset_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_documents_asset_id ON bpm.documents USING btree (asset_id);


--
-- Name: ix_event_activities_event_id_occurred_at; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_event_activities_event_id_occurred_at ON bpm.event_activities USING btree (event_id, occurred_at);


--
-- Name: ix_event_assets_asset_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_event_assets_asset_id ON bpm.event_assets USING btree (asset_id);


--
-- Name: ix_event_assets_event_id_asset_id_relation_type; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_event_assets_event_id_asset_id_relation_type ON bpm.event_assets USING btree (event_id, asset_id, relation_type) WHERE (is_deleted = false);


--
-- Name: ix_event_assets_workspace_id_event_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_event_assets_workspace_id_event_id ON bpm.event_assets USING btree (workspace_id, event_id);


--
-- Name: ix_event_daily_reports_event_id_report_date; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_event_daily_reports_event_id_report_date ON bpm.event_daily_reports USING btree (event_id, report_date);


--
-- Name: ix_event_daily_reports_event_id_report_date_user_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_event_daily_reports_event_id_report_date_user_id ON bpm.event_daily_reports USING btree (event_id, report_date, user_id) WHERE (is_deleted = false);


--
-- Name: ix_event_daily_reports_status_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_event_daily_reports_status_id ON bpm.event_daily_reports USING btree (status_id);


--
-- Name: ix_event_dependencies_depends_on_event_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_event_dependencies_depends_on_event_id ON bpm.event_dependencies USING btree (depends_on_event_id);


--
-- Name: ix_event_dependencies_event_id_depends_on_event_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_event_dependencies_event_id_depends_on_event_id ON bpm.event_dependencies USING btree (event_id, depends_on_event_id) WHERE (is_deleted = false);


--
-- Name: ix_event_statuses_event_id_is_current; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_event_statuses_event_id_is_current ON bpm.event_statuses USING btree (event_id, is_current);


--
-- Name: ix_event_statuses_status_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_event_statuses_status_id ON bpm.event_statuses USING btree (status_id);


--
-- Name: ix_event_types_code; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_event_types_code ON bpm.event_types USING btree (code);


--
-- Name: ix_events_event_type_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_events_event_type_id ON bpm.events USING btree (event_type_id);


--
-- Name: ix_events_workspace_id_event_type_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_events_workspace_id_event_type_id ON bpm.events USING btree (workspace_id, event_type_id);


--
-- Name: ix_mail_templates_workspace_id_code; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_mail_templates_workspace_id_code ON bpm.mail_templates USING btree (workspace_id, code) WHERE (is_deleted = false);


--
-- Name: ix_notes_asset_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_notes_asset_id ON bpm.notes USING btree (asset_id);


--
-- Name: ix_send_mail_attempts_send_mail_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_send_mail_attempts_send_mail_id ON bpm.send_mail_attempts USING btree (send_mail_id);


--
-- Name: ix_send_mail_recipients_send_mail_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_send_mail_recipients_send_mail_id ON bpm.send_mail_recipients USING btree (send_mail_id);


--
-- Name: ix_send_mails_send_status_next_attempt_at; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_send_mails_send_status_next_attempt_at ON bpm.send_mails USING btree (send_status, next_attempt_at);


--
-- Name: ix_status_types_workspace_id_code; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_status_types_workspace_id_code ON bpm.status_types USING btree (workspace_id, code) WHERE (is_deleted = false);


--
-- Name: ix_statuses_status_type_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_statuses_status_type_id ON bpm.statuses USING btree (status_type_id);


--
-- Name: ix_statuses_workspace_id_status_type_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_statuses_workspace_id_status_type_id ON bpm.statuses USING btree (workspace_id, status_type_id);


--
-- Name: ix_task_events_assignee_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_task_events_assignee_id ON bpm.task_events USING btree (assignee_id);


--
-- Name: ix_task_events_due_at; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_task_events_due_at ON bpm.task_events USING btree (due_at);


--
-- Name: ix_task_events_event_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_task_events_event_id ON bpm.task_events USING btree (event_id);


--
-- Name: ix_task_events_parent_event_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE INDEX ix_task_events_parent_event_id ON bpm.task_events USING btree (parent_event_id);


--
-- Name: ix_task_events_workspace_id_reference_no; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_task_events_workspace_id_reference_no ON bpm.task_events USING btree (workspace_id, reference_no) WHERE (is_deleted = false);


--
-- Name: ix_task_settings_workspace_id; Type: INDEX; Schema: bpm; Owner: -
--

CREATE UNIQUE INDEX ix_task_settings_workspace_id ON bpm.task_settings USING btree (workspace_id) WHERE (is_deleted = false);


--
-- Name: ix_audit_logs_action; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_audit_logs_action ON ONLY public.audit_logs USING btree (action);


--
-- Name: audit_logs_2026_05_action_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX audit_logs_2026_05_action_idx ON public.audit_logs_2026_05 USING btree (action);


--
-- Name: ix_audit_logs_actor; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_audit_logs_actor ON ONLY public.audit_logs USING btree (actor_user_id);


--
-- Name: audit_logs_2026_05_actor_user_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX audit_logs_2026_05_actor_user_id_idx ON public.audit_logs_2026_05 USING btree (actor_user_id);


--
-- Name: ix_audit_logs_workspace_occurred; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_audit_logs_workspace_occurred ON ONLY public.audit_logs USING btree (workspace_id, occurred_at DESC);


--
-- Name: audit_logs_2026_05_workspace_id_occurred_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX audit_logs_2026_05_workspace_id_occurred_at_idx ON public.audit_logs_2026_05 USING btree (workspace_id, occurred_at DESC);


--
-- Name: audit_logs_2026_06_action_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX audit_logs_2026_06_action_idx ON public.audit_logs_2026_06 USING btree (action);


--
-- Name: audit_logs_2026_06_actor_user_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX audit_logs_2026_06_actor_user_id_idx ON public.audit_logs_2026_06 USING btree (actor_user_id);


--
-- Name: audit_logs_2026_06_workspace_id_occurred_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX audit_logs_2026_06_workspace_id_occurred_at_idx ON public.audit_logs_2026_06 USING btree (workspace_id, occurred_at DESC);


--
-- Name: audit_logs_2026_07_action_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX audit_logs_2026_07_action_idx ON public.audit_logs_2026_07 USING btree (action);


--
-- Name: audit_logs_2026_07_actor_user_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX audit_logs_2026_07_actor_user_id_idx ON public.audit_logs_2026_07 USING btree (actor_user_id);


--
-- Name: audit_logs_2026_07_workspace_id_occurred_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX audit_logs_2026_07_workspace_id_occurred_at_idx ON public.audit_logs_2026_07 USING btree (workspace_id, occurred_at DESC);


--
-- Name: ix_employees_placement_node_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_employees_placement_node_id ON public.employees USING btree (placement_node_id);


--
-- Name: ix_employees_user_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_employees_user_id ON public.employees USING btree (user_id);


--
-- Name: ix_employees_workspace_id_user_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_employees_workspace_id_user_id ON public.employees USING btree (workspace_id, user_id) WHERE (is_deleted = false);


--
-- Name: ix_password_reset_tokens_token_hash; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_password_reset_tokens_token_hash ON public.password_reset_tokens USING btree (token_hash);


--
-- Name: ix_password_reset_tokens_user_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_password_reset_tokens_user_id ON public.password_reset_tokens USING btree (user_id);


--
-- Name: ix_permissions_code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_permissions_code ON public.permissions USING btree (code);


--
-- Name: ix_refresh_tokens_token_hash; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_refresh_tokens_token_hash ON public.refresh_tokens USING btree (token_hash);


--
-- Name: ix_refresh_tokens_user_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_refresh_tokens_user_id ON public.refresh_tokens USING btree (user_id);


--
-- Name: ix_role_permissions_permission_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_role_permissions_permission_id ON public.role_permissions USING btree (permission_id);


--
-- Name: ix_role_permissions_role_id_permission_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_role_permissions_role_id_permission_id ON public.role_permissions USING btree (role_id, permission_id);


--
-- Name: ix_roles_workspace_id_code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_roles_workspace_id_code ON public.roles USING btree (workspace_id, code) WHERE (is_deleted = false);


--
-- Name: ix_structure_nodes_parent_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_structure_nodes_parent_id ON public.structure_nodes USING btree (parent_id);


--
-- Name: ix_structure_nodes_workspace_id_code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_structure_nodes_workspace_id_code ON public.structure_nodes USING btree (workspace_id, code) WHERE (is_deleted = false);


--
-- Name: ix_user_permissions_permission_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_user_permissions_permission_id ON public.user_permissions USING btree (permission_id);


--
-- Name: ix_user_permissions_user_id_permission_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_user_permissions_user_id_permission_id ON public.user_permissions USING btree (user_id, permission_id);


--
-- Name: ix_user_roles_role_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_user_roles_role_id ON public.user_roles USING btree (role_id);


--
-- Name: ix_user_roles_user_id_role_id_cluster_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_user_roles_user_id_role_id_cluster_id ON public.user_roles USING btree (user_id, role_id, cluster_id);


--
-- Name: ix_users_workspace_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_users_workspace_id ON public.users USING btree (workspace_id);


--
-- Name: ix_users_workspace_id_normalized_email; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_users_workspace_id_normalized_email ON public.users USING btree (workspace_id, normalized_email) WHERE (is_deleted = false);


--
-- Name: ix_workspaces_is_deleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_workspaces_is_deleted ON public.workspaces USING btree (is_deleted);


--
-- Name: ix_workspaces_slug; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_workspaces_slug ON public.workspaces USING btree (slug);


--
-- Name: audit_logs_2026_05_action_idx; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.ix_audit_logs_action ATTACH PARTITION public.audit_logs_2026_05_action_idx;


--
-- Name: audit_logs_2026_05_actor_user_id_idx; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.ix_audit_logs_actor ATTACH PARTITION public.audit_logs_2026_05_actor_user_id_idx;


--
-- Name: audit_logs_2026_05_pkey; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.pk_audit_logs ATTACH PARTITION public.audit_logs_2026_05_pkey;


--
-- Name: audit_logs_2026_05_workspace_id_occurred_at_idx; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.ix_audit_logs_workspace_occurred ATTACH PARTITION public.audit_logs_2026_05_workspace_id_occurred_at_idx;


--
-- Name: audit_logs_2026_06_action_idx; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.ix_audit_logs_action ATTACH PARTITION public.audit_logs_2026_06_action_idx;


--
-- Name: audit_logs_2026_06_actor_user_id_idx; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.ix_audit_logs_actor ATTACH PARTITION public.audit_logs_2026_06_actor_user_id_idx;


--
-- Name: audit_logs_2026_06_pkey; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.pk_audit_logs ATTACH PARTITION public.audit_logs_2026_06_pkey;


--
-- Name: audit_logs_2026_06_workspace_id_occurred_at_idx; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.ix_audit_logs_workspace_occurred ATTACH PARTITION public.audit_logs_2026_06_workspace_id_occurred_at_idx;


--
-- Name: audit_logs_2026_07_action_idx; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.ix_audit_logs_action ATTACH PARTITION public.audit_logs_2026_07_action_idx;


--
-- Name: audit_logs_2026_07_actor_user_id_idx; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.ix_audit_logs_actor ATTACH PARTITION public.audit_logs_2026_07_actor_user_id_idx;


--
-- Name: audit_logs_2026_07_pkey; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.pk_audit_logs ATTACH PARTITION public.audit_logs_2026_07_pkey;


--
-- Name: audit_logs_2026_07_workspace_id_occurred_at_idx; Type: INDEX ATTACH; Schema: public; Owner: -
--

ALTER INDEX public.ix_audit_logs_workspace_occurred ATTACH PARTITION public.audit_logs_2026_07_workspace_id_occurred_at_idx;


--
-- Name: audit_logs trg_audit_logs_no_update; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_audit_logs_no_update BEFORE DELETE OR UPDATE ON public.audit_logs FOR EACH ROW EXECUTE FUNCTION public.erp_block_audit_mutation();


--
-- Name: assets fk_assets_asset_types_asset_type_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.assets
    ADD CONSTRAINT fk_assets_asset_types_asset_type_id FOREIGN KEY (asset_type_id) REFERENCES bpm.asset_types(id) ON DELETE RESTRICT;


--
-- Name: documents fk_documents_assets_asset_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.documents
    ADD CONSTRAINT fk_documents_assets_asset_id FOREIGN KEY (asset_id) REFERENCES bpm.assets(id) ON DELETE RESTRICT;


--
-- Name: event_activities fk_event_activities_events_event_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_activities
    ADD CONSTRAINT fk_event_activities_events_event_id FOREIGN KEY (event_id) REFERENCES bpm.events(id) ON DELETE RESTRICT;


--
-- Name: event_assets fk_event_assets_assets_asset_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_assets
    ADD CONSTRAINT fk_event_assets_assets_asset_id FOREIGN KEY (asset_id) REFERENCES bpm.assets(id) ON DELETE RESTRICT;


--
-- Name: event_assets fk_event_assets_events_event_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_assets
    ADD CONSTRAINT fk_event_assets_events_event_id FOREIGN KEY (event_id) REFERENCES bpm.events(id) ON DELETE RESTRICT;


--
-- Name: event_daily_reports fk_event_daily_reports_events_event_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_daily_reports
    ADD CONSTRAINT fk_event_daily_reports_events_event_id FOREIGN KEY (event_id) REFERENCES bpm.events(id) ON DELETE RESTRICT;


--
-- Name: event_daily_reports fk_event_daily_reports_statuses_status_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_daily_reports
    ADD CONSTRAINT fk_event_daily_reports_statuses_status_id FOREIGN KEY (status_id) REFERENCES bpm.statuses(id) ON DELETE RESTRICT;


--
-- Name: event_dependencies fk_event_dependencies_events_depends_on_event_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_dependencies
    ADD CONSTRAINT fk_event_dependencies_events_depends_on_event_id FOREIGN KEY (depends_on_event_id) REFERENCES bpm.events(id) ON DELETE RESTRICT;


--
-- Name: event_dependencies fk_event_dependencies_events_event_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_dependencies
    ADD CONSTRAINT fk_event_dependencies_events_event_id FOREIGN KEY (event_id) REFERENCES bpm.events(id) ON DELETE RESTRICT;


--
-- Name: event_statuses fk_event_statuses_events_event_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_statuses
    ADD CONSTRAINT fk_event_statuses_events_event_id FOREIGN KEY (event_id) REFERENCES bpm.events(id) ON DELETE RESTRICT;


--
-- Name: event_statuses fk_event_statuses_statuses_status_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.event_statuses
    ADD CONSTRAINT fk_event_statuses_statuses_status_id FOREIGN KEY (status_id) REFERENCES bpm.statuses(id) ON DELETE RESTRICT;


--
-- Name: events fk_events_event_types_event_type_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.events
    ADD CONSTRAINT fk_events_event_types_event_type_id FOREIGN KEY (event_type_id) REFERENCES bpm.event_types(id) ON DELETE RESTRICT;


--
-- Name: notes fk_notes_assets_asset_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.notes
    ADD CONSTRAINT fk_notes_assets_asset_id FOREIGN KEY (asset_id) REFERENCES bpm.assets(id) ON DELETE RESTRICT;


--
-- Name: send_mail_attempts fk_send_mail_attempts_send_mails_send_mail_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.send_mail_attempts
    ADD CONSTRAINT fk_send_mail_attempts_send_mails_send_mail_id FOREIGN KEY (send_mail_id) REFERENCES bpm.send_mails(id) ON DELETE CASCADE;


--
-- Name: send_mail_recipients fk_send_mail_recipients_send_mails_send_mail_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.send_mail_recipients
    ADD CONSTRAINT fk_send_mail_recipients_send_mails_send_mail_id FOREIGN KEY (send_mail_id) REFERENCES bpm.send_mails(id) ON DELETE CASCADE;


--
-- Name: statuses fk_statuses_status_types_status_type_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.statuses
    ADD CONSTRAINT fk_statuses_status_types_status_type_id FOREIGN KEY (status_type_id) REFERENCES bpm.status_types(id) ON DELETE RESTRICT;


--
-- Name: task_events fk_task_events_events_event_id; Type: FK CONSTRAINT; Schema: bpm; Owner: -
--

ALTER TABLE ONLY bpm.task_events
    ADD CONSTRAINT fk_task_events_events_event_id FOREIGN KEY (event_id) REFERENCES bpm.events(id) ON DELETE RESTRICT;


--
-- Name: employees fk_employees_structure_nodes_placement_node_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.employees
    ADD CONSTRAINT fk_employees_structure_nodes_placement_node_id FOREIGN KEY (placement_node_id) REFERENCES public.structure_nodes(id) ON DELETE SET NULL;


--
-- Name: employees fk_employees_users_user_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.employees
    ADD CONSTRAINT fk_employees_users_user_id FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE RESTRICT;


--
-- Name: password_reset_tokens fk_password_reset_tokens_users_user_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.password_reset_tokens
    ADD CONSTRAINT fk_password_reset_tokens_users_user_id FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: refresh_tokens fk_refresh_tokens_users_user_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.refresh_tokens
    ADD CONSTRAINT fk_refresh_tokens_users_user_id FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: role_permissions fk_role_permissions_permissions_permission_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT fk_role_permissions_permissions_permission_id FOREIGN KEY (permission_id) REFERENCES public.permissions(id) ON DELETE RESTRICT;


--
-- Name: role_permissions fk_role_permissions_roles_role_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT fk_role_permissions_roles_role_id FOREIGN KEY (role_id) REFERENCES public.roles(id) ON DELETE CASCADE;


--
-- Name: structure_nodes fk_structure_nodes_structure_nodes_parent_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.structure_nodes
    ADD CONSTRAINT fk_structure_nodes_structure_nodes_parent_id FOREIGN KEY (parent_id) REFERENCES public.structure_nodes(id) ON DELETE RESTRICT;


--
-- Name: user_permissions fk_user_permissions_permissions_permission_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_permissions
    ADD CONSTRAINT fk_user_permissions_permissions_permission_id FOREIGN KEY (permission_id) REFERENCES public.permissions(id) ON DELETE RESTRICT;


--
-- Name: user_roles fk_user_roles_roles_role_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT fk_user_roles_roles_role_id FOREIGN KEY (role_id) REFERENCES public.roles(id) ON DELETE CASCADE;


--
-- Name: users fk_users_workspaces_workspace_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT fk_users_workspaces_workspace_id FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE RESTRICT;


--
-- Name: assets; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.assets ENABLE ROW LEVEL SECURITY;

--
-- Name: documents; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.documents ENABLE ROW LEVEL SECURITY;

--
-- Name: event_activities; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.event_activities ENABLE ROW LEVEL SECURITY;

--
-- Name: event_assets; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.event_assets ENABLE ROW LEVEL SECURITY;

--
-- Name: event_daily_reports; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.event_daily_reports ENABLE ROW LEVEL SECURITY;

--
-- Name: event_dependencies; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.event_dependencies ENABLE ROW LEVEL SECURITY;

--
-- Name: event_statuses; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.event_statuses ENABLE ROW LEVEL SECURITY;

--
-- Name: events; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.events ENABLE ROW LEVEL SECURITY;

--
-- Name: notes; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.notes ENABLE ROW LEVEL SECURITY;

--
-- Name: send_mail_attempts; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.send_mail_attempts ENABLE ROW LEVEL SECURITY;

--
-- Name: send_mail_recipients; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.send_mail_recipients ENABLE ROW LEVEL SECURITY;

--
-- Name: send_mails; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.send_mails ENABLE ROW LEVEL SECURITY;

--
-- Name: status_types; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.status_types ENABLE ROW LEVEL SECURITY;

--
-- Name: statuses; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.statuses ENABLE ROW LEVEL SECURITY;

--
-- Name: task_events; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.task_events ENABLE ROW LEVEL SECURITY;

--
-- Name: task_settings; Type: ROW SECURITY; Schema: bpm; Owner: -
--

ALTER TABLE bpm.task_settings ENABLE ROW LEVEL SECURITY;

--
-- Name: assets tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.assets USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: documents tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.documents USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: event_activities tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.event_activities USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: event_assets tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.event_assets USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: event_daily_reports tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.event_daily_reports USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: event_dependencies tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.event_dependencies USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: event_statuses tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.event_statuses USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: events tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.events USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: notes tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.notes USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: send_mail_attempts tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.send_mail_attempts USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: send_mail_recipients tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.send_mail_recipients USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: send_mails tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.send_mails USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: status_types tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.status_types USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: statuses tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.statuses USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: task_events tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.task_events USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: task_settings tenant_isolation; Type: POLICY; Schema: bpm; Owner: -
--

CREATE POLICY tenant_isolation ON bpm.task_settings USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: audit_logs; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.audit_logs ENABLE ROW LEVEL SECURITY;

--
-- Name: employees; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.employees ENABLE ROW LEVEL SECURITY;

--
-- Name: password_reset_tokens; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.password_reset_tokens ENABLE ROW LEVEL SECURITY;

--
-- Name: refresh_tokens; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.refresh_tokens ENABLE ROW LEVEL SECURITY;

--
-- Name: role_permissions; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.role_permissions ENABLE ROW LEVEL SECURITY;

--
-- Name: roles; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.roles ENABLE ROW LEVEL SECURITY;

--
-- Name: structure_nodes; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.structure_nodes ENABLE ROW LEVEL SECURITY;

--
-- Name: audit_logs tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.audit_logs USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: employees tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.employees USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: password_reset_tokens tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.password_reset_tokens USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: refresh_tokens tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.refresh_tokens USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: role_permissions tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.role_permissions USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: roles tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.roles USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: structure_nodes tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.structure_nodes USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: user_permissions tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.user_permissions USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: user_roles tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.user_roles USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: users tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.users USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: workspace_security_policies tenant_isolation; Type: POLICY; Schema: public; Owner: -
--

CREATE POLICY tenant_isolation ON public.workspace_security_policies USING (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint))) WITH CHECK (((current_setting('app.is_platform_admin'::text, true) = 'true'::text) OR (workspace_id = (NULLIF(current_setting('app.current_workspace_id'::text, true), ''::text))::bigint)));


--
-- Name: user_permissions; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.user_permissions ENABLE ROW LEVEL SECURITY;

--
-- Name: user_roles; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.user_roles ENABLE ROW LEVEL SECURITY;

--
-- Name: users; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

--
-- Name: workspace_security_policies; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.workspace_security_policies ENABLE ROW LEVEL SECURITY;

--
-- PostgreSQL database dump complete
--

\unrestrict M8lDLGObbkNH5XNlfU6PKBsgL3ma9OXknOg73teaJH0LbPMU4vA6TDNU43ptyIv

