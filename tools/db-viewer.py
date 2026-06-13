#!/usr/bin/env python3
"""
Tiny read-only database viewer for the ERP demo.

Zero dependencies: it shells out to the `psql` CLI (already installed) and
renders the results as HTML. Lists every table, lets you browse rows, and run
read-only SELECT queries. Local-only and read-only by design.

Run via ../view-db.sh (recommended) or:  python3 db-viewer.py
"""
import csv
import html
import io
import os
import shutil
import subprocess
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

DB = os.environ.get("PGDATABASE", "erp")
USER = os.environ.get("PGUSER", os.environ.get("USER", "postgres"))  # superuser bypasses RLS
HOST = os.environ.get("PGHOST", "localhost")
PORT_DB = os.environ.get("PGPORT", "5432")
PORT = int(os.environ.get("VIEWER_PORT", "8090"))

PSQL = shutil.which("psql") or "/opt/homebrew/opt/postgresql@16/bin/psql"

FORBIDDEN = ("insert", "update", "delete", "drop", "alter", "truncate",
             "create", "grant", "revoke", "copy", "comment", "merge")

CSS = """
*{box-sizing:border-box} body{margin:0;font-family:system-ui,-apple-system,sans-serif;
background:#f7f6f3;color:#1c1a17} a{color:#c96442;text-decoration:none} a:hover{text-decoration:underline}
header{background:#fff;border-bottom:1px solid #e4e0d8;padding:14px 22px;display:flex;align-items:center;gap:12px;position:sticky;top:0;z-index:5}
header h1{font-size:16px;margin:0;font-weight:700} .badge{background:#fbeee8;color:#9d4a30;border-radius:6px;padding:2px 8px;font-size:12px;font-weight:600}
.wrap{display:flex;min-height:calc(100vh - 53px)}
aside{width:280px;flex:none;background:#fff;border-right:1px solid #e4e0d8;padding:14px;overflow:auto;height:calc(100vh - 53px);position:sticky;top:53px}
aside h2{font-size:11px;text-transform:uppercase;letter-spacing:.06em;color:#928b7d;margin:8px 4px}
.tlink{display:flex;justify-content:space-between;padding:6px 8px;border-radius:6px;font-size:13.5px}
.tlink:hover{background:#f3f1ed} .tlink.active{background:#fbeee8;font-weight:600}
.tlink .n{color:#928b7d;font-size:12px} main{flex:1;padding:22px;overflow:auto}
h2.title{font-size:20px;margin:0 0 4px} .muted{color:#6e675b;font-size:13px;margin:0 0 16px}
table{border-collapse:collapse;width:100%;background:#fff;border:1px solid #e4e0d8;border-radius:10px;overflow:hidden;font-size:13px}
th{background:#faf9f7;text-align:left;padding:8px 10px;border-bottom:1px solid #e4e0d8;font-size:11.5px;text-transform:uppercase;letter-spacing:.03em;color:#6e675b;white-space:nowrap}
td{padding:8px 10px;border-bottom:1px solid #f3f1ed;vertical-align:top;max-width:380px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
tr:hover td{background:#faf9f7} .null{color:#c0bbb0;font-style:italic}
.qbox{width:100%;font-family:ui-monospace,monospace;font-size:13px;padding:10px;border:1px solid #d4cfc4;border-radius:8px;min-height:70px}
.btn{background:#c96442;color:#fff;border:0;border-radius:8px;padding:9px 16px;font-weight:600;cursor:pointer;font-size:13.5px;margin-top:8px}
.err{background:#f7e3df;color:#852b23;padding:10px 12px;border-radius:8px;font-size:13px;margin:10px 0}
.cards{display:flex;gap:16px;flex-wrap:wrap;margin-bottom:18px}
.card{background:#fff;border:1px solid #e4e0d8;border-radius:10px;padding:14px 16px;min-width:150px}
.card .v{font-size:24px;font-weight:700} .card .l{color:#6e675b;font-size:12.5px}
"""


def run_sql(sql):
    """Runs SQL via psql, returns (header, rows) or raises with stderr."""
    proc = subprocess.run(
        [PSQL, "-h", HOST, "-p", PORT_DB, "-U", USER, "-d", DB, "--csv", "-c", sql],
        capture_output=True, text=True, timeout=30,
        env={**os.environ, "PGCLIENTENCODING": "UTF8"},
    )
    if proc.returncode != 0:
        raise RuntimeError(proc.stderr.strip() or "query failed")
    reader = list(csv.reader(io.StringIO(proc.stdout)))
    if not reader:
        return [], []
    return reader[0], reader[1:]


def list_tables():
    _, rows = run_sql(
        "SELECT table_name FROM information_schema.tables "
        "WHERE table_schema='public' AND table_type='BASE TABLE' ORDER BY table_name;")
    out = []
    for (name,) in rows:
        try:
            _, c = run_sql(f'SELECT count(*) FROM "{name}";')
            out.append((name, c[0][0]))
        except Exception:
            out.append((name, "?"))
    return out


def render_table(header, rows):
    if not header:
        return "<p class='muted'>No columns.</p>"
    th = "".join(f"<th>{html.escape(h)}</th>" for h in header)
    body = []
    for r in rows:
        tds = []
        for cell in r:
            if cell == "":
                tds.append("<td class='null'>∅</td>")
            else:
                v = html.escape(cell)
                tds.append(f"<td title='{v}'>{v}</td>")
        body.append("<tr>" + "".join(tds) + "</tr>")
    return f"<table><thead><tr>{th}</tr></thead><tbody>{''.join(body)}</tbody></table>"


def sidebar(active=""):
    items = []
    for name, count in list_tables():
        cls = "tlink active" if name == active else "tlink"
        items.append(
            f"<a class='{cls}' href='/table?name={html.escape(name)}'>"
            f"<span>{html.escape(name)}</span><span class='n'>{count}</span></a>")
    return "<aside><h2>Tables</h2>" + "".join(items) + \
           "<h2 style='margin-top:16px'>Tools</h2>" \
           "<a class='tlink' href='/query'>SQL query (read-only)</a></aside>"


def page(title, body, active=""):
    return f"""<!doctype html><html><head><meta charset='utf-8'><title>{html.escape(title)} · ERP DB</title>
<style>{CSS}</style></head><body>
<header><h1>ERP Database Viewer</h1><span class='badge'>{html.escape(DB)} · read-only</span>
<span style='margin-left:auto;color:#928b7d;font-size:12.5px'>{html.escape(USER)}@{html.escape(HOST)}:{PORT_DB}</span></header>
<div class='wrap'>{sidebar(active)}<main>{body}</main></div></body></html>"""


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *a):  # quiet
        pass

    def _send(self, content, code=200):
        data = content.encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        u = urlparse(self.path)
        q = parse_qs(u.query)
        try:
            if u.path == "/":
                self._send(self.home())
            elif u.path == "/table":
                self._send(self.table(q.get("name", [""])[0], int(q.get("limit", ["200"])[0])))
            elif u.path == "/query":
                self._send(self.query_form())
            else:
                self._send(page("Not found", "<p class='muted'>Not found.</p>"), 404)
        except Exception as e:
            self._send(page("Error", f"<div class='err'>{html.escape(str(e))}</div>"), 500)

    def do_POST(self):
        if urlparse(self.path).path != "/query":
            self._send(page("Not found", "<p>Not found</p>"), 404)
            return
        length = int(self.headers.get("Content-Length", "0"))
        body = self.rfile.read(length).decode("utf-8")
        sql = parse_qs(body).get("sql", [""])[0].strip()
        self._send(self.query_form(sql))

    def home(self):
        tables = list_tables()
        total = sum(int(c) for _, c in tables if str(c).isdigit())
        cards = (
            f"<div class='cards'>"
            f"<div class='card'><div class='v'>{len(tables)}</div><div class='l'>tables</div></div>"
            f"<div class='card'><div class='v'>{total}</div><div class='l'>total rows</div></div>"
            f"<div class='card'><div class='v'>{html.escape(DB)}</div><div class='l'>database</div></div>"
            f"</div>")
        rows = "".join(
            f"<tr><td><a href='/table?name={html.escape(n)}'>{html.escape(n)}</a></td>"
            f"<td>{c}</td></tr>" for n, c in tables)
        tbl = f"<table><thead><tr><th>Table</th><th>Rows</th></tr></thead><tbody>{rows}</tbody></table>"
        return page("Overview",
                    f"<h2 class='title'>Overview</h2><p class='muted'>Click any table to browse its rows.</p>{cards}{tbl}")

    def table(self, name, limit):
        if not name.replace("_", "").isalnum():
            return page("Error", "<div class='err'>Invalid table name.</div>")
        header, rows = run_sql(f'SELECT * FROM "{name}" LIMIT {limit};')
        body = (f"<h2 class='title'>{html.escape(name)}</h2>"
                f"<p class='muted'>Showing up to {limit} rows. ∅ = NULL.</p>"
                f"{render_table(header, rows)}")
        return page(name, body, active=name)

    def query_form(self, sql=""):
        result = ""
        if sql:
            low = sql.lower().lstrip("(")
            if not (low.startswith("select") or low.startswith("with")) or \
               any(f in low for f in FORBIDDEN):
                result = "<div class='err'>Only read-only SELECT / WITH queries are allowed.</div>"
            else:
                try:
                    header, rows = run_sql(sql.rstrip(";") + " LIMIT 500;" if "limit" not in sql.lower() else sql)
                    result = f"<p class='muted'>{len(rows)} row(s).</p>{render_table(header, rows)}"
                except Exception as e:
                    result = f"<div class='err'>{html.escape(str(e))}</div>"
        form = (f"<h2 class='title'>SQL query</h2>"
                f"<p class='muted'>Read-only. SELECT / WITH only; a LIMIT is added automatically.</p>"
                f"<form method='post' action='/query'>"
                f"<textarea class='qbox' name='sql' placeholder='SELECT * FROM users;'>{html.escape(sql)}</textarea>"
                f"<br><button class='btn' type='submit'>Run</button></form><div style='margin-top:16px'>{result}</div>")
        return page("Query", form)


def main():
    print(f"  ERP DB viewer  →  http://localhost:{PORT}")
    print(f"  database: {DB}   user: {USER}   (read-only)\n  Ctrl+C to stop.")
    try:
        ThreadingHTTPServer(("127.0.0.1", PORT), Handler).serve_forever()
    except KeyboardInterrupt:
        print("\n  stopped.")


if __name__ == "__main__":
    main()
