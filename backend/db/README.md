# Database files

These SQL files are a snapshot of the PostgreSQL `erp` database for inspection and
portability. **EF Core migrations (`src/Erp.Infrastructure/Migrations`) remain the
source of truth** — these dumps are generated from a migrated + seeded dev DB.

| File | Contents |
|------|----------|
| `schema.sql` | Structure only — schemas (`public`, `bpm`), all tables, the partitioned append-only `audit_logs`, the `bpm.fn_task_*` / `bpm.get_task_list` report functions, RLS helper + 28 tenant-isolation policies. |
| `erp_demo_dump.sql` | Full dump — schema **+ demo data** (16 tasks, statuses, daily reports, queued mails, the demo workspace + users). Restores the whole demo. |

## Model notes (so the tables make sense)
- There is **no `tasks` table**. A task = a row in **`bpm.events`** + a row in **`bpm.task_events`** (Event/Asset architecture). Workflow status history is in **`bpm.event_statuses`** (one `is_current`); priority is a **`bpm.statuses`** row of type `TASK_PRIORITY`; notes/documents are **`bpm.assets`** linked via **`bpm.event_assets`**; daily reports are **`bpm.event_daily_reports`**.
- All BPM tables live in the **`bpm` schema**, not `public`.
- All ids are **BigInt** (C# `long`), serialized to the SPA as strings.

## Viewing every table in a GUI tool (Valentina Studio / DBeaver / pgAdmin / TablePlus)
Tenant tables use PostgreSQL **Row-Level Security with FORCE**, so the normal app role
(`erp`) sees **no rows** unless the tenant session variables are set. Use the dedicated
read role that **bypasses RLS** instead:

| Setting | Value |
|---------|-------|
| Host | `localhost` |
| Port | `5432` |
| Database | `erp` |
| User | `erp_admin` |
| Password | `erp_admin` |

`erp_admin` has `BYPASSRLS` + read (`SELECT`) on all tables, so every table/row is
visible. The application keeps using the RLS-enforced `erp` role, so tenant isolation
is unchanged.

> If you must browse with the `erp` role instead, run this first in the same SQL session:
> `SET app.is_platform_admin = 'true';`  (or `SET app.current_workspace_id = '<workspaceId>';`)

## Restore into a fresh database
```bash
createdb -O erp erp_restore
psql -d erp_restore -f erp_demo_dump.sql      # schema + demo data
# or structure only:
psql -d erp_restore -f schema.sql

# (re)create the read role once per server:
psql -d postgres -c "CREATE ROLE erp_admin LOGIN PASSWORD 'erp_admin' BYPASSRLS;"
psql -d erp_restore -c "GRANT USAGE ON SCHEMA public, bpm TO erp_admin; GRANT SELECT ON ALL TABLES IN SCHEMA public, bpm TO erp_admin;"
```
