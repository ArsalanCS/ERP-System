#!/usr/bin/env bash
# Opens a local, read-only web viewer for the demo database.
#   ./view-db.sh            (views the "erp" database)
#   PGDATABASE=erp_test ./view-db.sh   (views the test database)
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export PATH="/opt/homebrew/opt/postgresql@16/bin:$PATH"
export PGDATABASE="${PGDATABASE:-erp}"
export PGUSER="${PGUSER:-$USER}"   # your macOS user is the Postgres superuser (sees all rows)
export VIEWER_PORT="${VIEWER_PORT:-8090}"

if ! pg_isready -q 2>/dev/null; then
  echo "PostgreSQL isn't running. Start it with:  brew services start postgresql@16"
  exit 1
fi

# Free the viewer port if a previous instance is still up.
lsof -ti:"$VIEWER_PORT" 2>/dev/null | xargs kill -9 2>/dev/null || true

echo "Opening DB viewer at http://localhost:$VIEWER_PORT …"
( sleep 1; open "http://localhost:$VIEWER_PORT" >/dev/null 2>&1 || true ) &
exec python3 "$ROOT/tools/db-viewer.py"
