using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Enables PostgreSQL Row-Level Security on tenant-owned tables — the
    /// database-layer half of tenant isolation (CLAUDE.md §4.1). Policies read
    /// the session GUCs set per connection by RlsConnectionInterceptor.
    ///
    /// A reusable function <c>erp_enable_tenant_rls(regclass)</c> applies the
    /// standard policy so future tenant tables opt in with a single call.
    /// FORCE ROW LEVEL SECURITY makes the policy apply even to the table owner
    /// (the app role owns its tables).
    /// </summary>
    public partial class AddRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                            OR workspace_id = NULLIF(current_setting('app.current_workspace_id', true), '')::uuid
                        )
                        WITH CHECK (
                            current_setting('app.is_platform_admin', true) = 'true'
                            OR workspace_id = NULLIF(current_setting('app.current_workspace_id', true), '')::uuid
                        );
                    $pol$, policy_name, target);
                END;
                $func$ LANGUAGE plpgsql;
                """);

            // Apply to existing tenant-owned tables.
            migrationBuilder.Sql("SELECT erp_enable_tenant_rls('users');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON users;");
            migrationBuilder.Sql("ALTER TABLE users NO FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE users DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS erp_enable_tenant_rls(regclass);");
        }
    }
}
