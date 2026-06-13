using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Creates the append-only, monthly-partitioned <c>audit_logs</c> table
    /// (CLAUDE.md §4.3 / Identity spec §8). UPDATE/DELETE are blocked by a
    /// trigger; tenant isolation via RLS. A helper provisions monthly partitions
    /// (a scheduled job creates future months in production).
    /// </summary>
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Partitioned parent. PK must include the partition key (occurred_at).
            migrationBuilder.Sql("""
                CREATE TABLE audit_logs (
                    id                  uuid        NOT NULL,
                    workspace_id        uuid        NOT NULL,
                    organization_id     uuid        NULL,
                    cluster_id          uuid        NULL,
                    occurred_at         timestamptz NOT NULL,
                    correlation_id      varchar(64) NULL,
                    actor_user_id       uuid        NULL,
                    actor_display_name  varchar(256) NULL,
                    module              varchar(50) NOT NULL,
                    resource_type       varchar(80) NOT NULL,
                    resource_id         varchar(80) NULL,
                    action              varchar(40) NOT NULL,
                    old_values          jsonb       NULL,
                    new_values          jsonb       NULL,
                    ip_address          varchar(64) NULL,
                    user_agent          varchar(512) NULL,
                    result              varchar(10) NOT NULL,
                    source              varchar(20) NOT NULL,
                    reason              varchar(1000) NULL,
                    created_at          timestamptz NOT NULL,
                    created_by          uuid        NULL,
                    updated_at          timestamptz NULL,
                    updated_by          uuid        NULL,
                    CONSTRAINT pk_audit_logs PRIMARY KEY (id, occurred_at)
                ) PARTITION BY RANGE (occurred_at);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX ix_audit_logs_workspace_occurred ON audit_logs (workspace_id, occurred_at DESC);
                CREATE INDEX ix_audit_logs_actor ON audit_logs (actor_user_id);
                CREATE INDEX ix_audit_logs_action ON audit_logs (action);
                """);

            // Provision a month partition for the month containing the given timestamp.
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
                """);

            // Create previous, current and next month partitions.
            migrationBuilder.Sql("""
                SELECT erp_ensure_audit_partition(now() - interval '1 month');
                SELECT erp_ensure_audit_partition(now());
                SELECT erp_ensure_audit_partition(now() + interval '1 month');
                """);

            // Append-only: block UPDATE/DELETE via a trigger (cascades to partitions).
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

            // Tenant isolation (RLS) on the parent (applies to partitions via parent access).
            migrationBuilder.Sql("SELECT erp_enable_tenant_rls('audit_logs');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_audit_logs_no_update ON audit_logs;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS audit_logs CASCADE;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS erp_block_audit_mutation();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS erp_ensure_audit_partition(timestamptz);");
        }
    }
}
