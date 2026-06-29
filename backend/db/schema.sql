--
-- PostgreSQL database dump
--

\restrict Q4lqw6nqVpbl6E2Zd7ThSW3qjV3xnaItd59kEOQi07GBfqipIkZCkdZECJ8yidp

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
-- Name: fn_task_assignee_load(bigint, boolean, bigint[], bigint, timestamp with time zone, bigint, bigint, boolean, boolean); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_assignee_load(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamp with time zone, p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean) RETURNS TABLE(assignee_id bigint, assignee_name text, open integer, overdue integer)
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
-- Name: fn_task_daily_reports(bigint, boolean, bigint[], bigint, date, date, bigint, bigint, integer, integer); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_daily_reports(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_from date, p_to date, p_author bigint, p_status bigint, p_offset integer, p_limit integer) RETURNS TABLE(id bigint, event_id bigint, reference_no text, task_title text, report_date date, description text, estimated_time numeric, actual_time numeric, remaining_time numeric, status_id bigint, status_name text, status_color text, author_id bigint, author_name text, created_at timestamp with time zone, total bigint)
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
-- Name: fn_task_gantt(bigint, boolean, bigint[], bigint, integer); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_gantt(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_limit integer) RETURNS TABLE(event_id bigint, reference_no text, title text, start_at timestamp with time zone, due_at timestamp with time zone, completion_percent integer, status_color text, is_closed boolean)
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
-- Name: fn_task_priority_breakdown(bigint, boolean, bigint[], bigint, timestamp with time zone, bigint, bigint, boolean, boolean); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_priority_breakdown(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamp with time zone, p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean) RETURNS TABLE(id bigint, name text, color text, count integer)
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
-- Name: fn_task_status_breakdown(bigint, boolean, bigint[], bigint, timestamp with time zone, bigint, bigint, boolean, boolean); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_status_breakdown(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamp with time zone, p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean) RETURNS TABLE(id bigint, name text, color text, count integer)
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
-- Name: fn_task_summary(bigint, boolean, bigint[], bigint, timestamp with time zone, bigint, bigint, boolean, boolean); Type: FUNCTION; Schema: bpm; Owner: -
--

CREATE FUNCTION bpm.fn_task_summary(p_ws bigint, p_all boolean, p_users bigint[], p_me bigint, p_now timestamp with time zone, p_status bigint, p_priority bigint, p_overdue boolean, p_closed boolean) RETURNS TABLE(total integer, open integer, in_progress integer, overdue integer, due_today integer, due_this_week integer, high_priority integer, completed integer, unassigned integer, completed_last7 integer, reports_today integer, avg_completion integer, estimated_total numeric, actual_total numeric)
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

\unrestrict Q4lqw6nqVpbl6E2Zd7ThSW3qjV3xnaItd59kEOQi07GBfqipIkZCkdZECJ8yidp

