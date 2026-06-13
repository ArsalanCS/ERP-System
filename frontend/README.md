# ERP Frontend (Foundation / Identity admin SPA)

Vite + React + TypeScript SPA for the Cloud Accounting & ERP platform. This is
the **Phase 0 frontend skeleton** (no feature pages yet) — the app shell, the
seven admin route placeholders, and all cross-cutting machinery are in place.

## Stack

- **Vite + React 19 + TypeScript** (strict)
- **Tailwind CSS v4** — CSS-first config in [`src/styles/index.css`](src/styles/index.css); design tokens ported verbatim from the Claude Design handoff (`docs/Design Kickoff`)
- **React Router v7** — routes in [`src/app/router.tsx`](src/app/router.tsx)
- **TanStack Query** — all server state (no server data in global stores)
- **React Hook Form + Zod** — forms (wired as dependencies; used per feature slice)
- **react-i18next** — Arabic + English with an RTL/LTR switch

## Commands

```bash
npm install
npm run dev        # http://localhost:5173 (proxies /api -> http://localhost:5080)
npm run build      # tsc -b && vite build
npm run lint       # eslint
npm run typecheck  # tsc -b --noEmit
```

## Layout (feature-sliced — see docs/CONVENTIONS.md)

```
src/
├── app/            router, providers, layouts, route placeholder pages
│   ├── providers/  Query, Direction (RTL/LTR), Auth (stub session)
│   └── layouts/    AdminLayout (sidebar + topbar shell)
├── components/
│   └── shell/      Sidebar, Topbar, WorkspaceSwitcher, nav model
├── shared/
│   ├── ui/         design-system primitives (Button, Badge, Card, Input, Icon…)
│   ├── api/        typed fetch client + standard error envelope types
│   ├── rbac/       action-based permission mirror (Can, usePermissions, session)
│   ├── i18n/       i18next config + en/ar resource files
│   ├── hooks/      useDirection
│   └── lib/        cn, env
└── styles/         Tailwind entry + design tokens + component classes
```

## Notes for the next slice

- **Auth is stubbed.** [`AuthProvider`](src/app/providers/AuthProvider.tsx) seeds a
  dev session so the shell is navigable. The real session (JWT + backend-resolved
  effective actions) lands in Phase 1 · slice 1.
- **RBAC in the UI hides, never authorizes.** The backend remains the source of
  truth for every request (CLAUDE.md §4.2). Unauthorized nav items are hidden, not
  disabled (Identity spec §1.3).
- **No secrets in the client** (CLAUDE.md §4.4). Build-time config goes through
  [`src/shared/lib/env.ts`](src/shared/lib/env.ts); copy `.env.example` to `.env.local`.
