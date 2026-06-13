#!/usr/bin/env bash
#
# One-command launcher for the ERP Foundation / Identity demo.
# Starts PostgreSQL (if needed) + the backend API + the frontend dev server,
# waits until the API is healthy, prints the demo login, and tears everything
# down cleanly on Ctrl+C.
#
# Usage:   ./run-demo.sh
#
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND="$ROOT/backend"
FRONTEND="$ROOT/frontend"
LOGDIR="$ROOT/logs"
mkdir -p "$LOGDIR"

# .NET lives in ~/.dotnet (installed via dotnet-install.sh) — put it on PATH.
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

# Load local secrets / overrides (e.g. SMTP credentials) if present. This file
# is git-ignored and must never be committed. See .env.local.example.
if [ -f "$ROOT/.env.local" ]; then
  echo "==> Loading .env.local (SMTP / overrides)"
  set -a; . "$ROOT/.env.local"; set +a
fi

PG_BIN="/opt/homebrew/opt/postgresql@16/bin"

# Free our ports from any stale/zombie process left by a previous run, otherwise
# the new API can't bind to 5263 and create/save actions silently fail.
echo "==> Clearing ports 5263 (API) and 5173 (web)…"
for port in 5263 5173; do
  pids="$(lsof -ti:"$port" 2>/dev/null || true)"
  if [ -n "$pids" ]; then
    echo "    freeing port $port (pids: $pids)"
    echo "$pids" | xargs kill -9 2>/dev/null || true
  fi
done
pkill -f "Erp.Api" 2>/dev/null || true
sleep 1

echo "==> Checking PostgreSQL 16…"
if ! "$PG_BIN/pg_isready" -q 2>/dev/null; then
  echo "    not running — starting postgresql@16 via brew…"
  brew services start postgresql@16 >/dev/null 2>&1 || true
  for _ in $(seq 1 15); do "$PG_BIN/pg_isready" -q 2>/dev/null && break; sleep 1; done
fi
if ! "$PG_BIN/pg_isready" -q 2>/dev/null; then
  echo "!!  PostgreSQL is not available on localhost:5432. Start it and retry."
  exit 1
fi
echo "    PostgreSQL is up."

echo "==> Starting backend API (http://localhost:5263)…"
# Dev email outbox → ./logs/mail (invitation & reset emails are written here as
# HTML files; the link is also printed into ./logs/backend.log).
mkdir -p "$LOGDIR/mail"
( cd "$BACKEND" && Email__OutboxPath="$LOGDIR/mail" App__WebBaseUrl="http://localhost:5173" \
    dotnet run --project src/Erp.Api --launch-profile http ) > "$LOGDIR/backend.log" 2>&1 &
BACKEND_PID=$!

cleanup() {
  echo
  echo "==> Stopping demo…"
  kill "$BACKEND_PID" 2>/dev/null || true
  pkill -f "Erp.Api" 2>/dev/null || true
  pkill -f "vite" 2>/dev/null || true
  echo "    Done."
}
trap cleanup EXIT INT TERM

echo "    waiting for the API to become healthy (first run builds + migrates, ~30s)…"
READY=0
for _ in $(seq 1 60); do
  if curl -s -o /dev/null -w '%{http_code}' http://localhost:5263/api/v1/health 2>/dev/null | grep -q 200; then
    READY=1; break
  fi
  sleep 2
done
if [ "$READY" != "1" ]; then
  echo "!!  API did not start. Last 25 log lines:"
  tail -25 "$LOGDIR/backend.log"
  exit 1
fi
echo "    API is ready."

if [ ! -d "$FRONTEND/node_modules" ]; then
  echo "==> Installing frontend dependencies (first run only)…"
  ( cd "$FRONTEND" && npm install )
fi

cat <<'BANNER'

────────────────────────────────────────────────────────────
   ERP — Foundation / Identity   ·   DEMO READY
────────────────────────────────────────────────────────────
   App        →  http://localhost:5173
   API (Swagger) →  http://localhost:5263/swagger

   Sign in with:
      Workspace :  demo
      Email     :  owner@demo.test
      Password  :  Demo1234!

   Tip: toggle العربية / English from the top bar to show RTL.
   Press  Ctrl+C  to stop the backend and frontend together.
────────────────────────────────────────────────────────────

BANNER

cd "$FRONTEND" && npm run dev
