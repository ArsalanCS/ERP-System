# ERP Backend (.NET 10)

ASP.NET Core Web API for the Cloud Accounting & ERP platform. This is the
**Phase 0 backend skeleton** — the solution, layered projects, and cross-cutting
machinery are in place; **no tenant/business tables or features yet**.

## Stack

- **.NET 10 LTS**, C#, nullable on, **warnings-as-errors** ([Directory.Build.props](Directory.Build.props))
- **EF Core 10 + Npgsql** (PostgreSQL)
- **FluentValidation** (Application layer)
- **Swashbuckle** (OpenAPI/Swagger)

## Solution layout (dependency rule: `Api → Application → Domain`; `Infrastructure → Application/Domain`; Domain depends on nothing)

```
backend/
├── Erp.sln
├── Directory.Build.props          net10.0, nullable, warnings-as-errors
├── src/
│   ├── Erp.Domain/                entities, enums, invariants (no infra deps)
│   ├── Erp.Application/           use-cases, DTOs, validators (Domain + Shared)
│   ├── Erp.Infrastructure/        EF Core DbContext + design-time factory, DI
│   │   └── Persistence/Migrations/  InitialCreate (empty)
│   ├── Erp.Shared/                Result<T>, Error/ErrorCodes, correlation
│   └── Erp.Api/                   Program.cs, middleware, controllers, Swagger
└── tests/
    ├── Erp.UnitTests/             Result/Error rules
    └── Erp.IntegrationTests/      WebApplicationFactory health + correlation
```

## What's wired

- **Standard error envelope** ([ApiErrorEnvelope](src/Erp.Api/Contracts/ApiErrorEnvelope.cs)) — same shape for every 4xx/5xx (CONVENTIONS.md).
- **Result/Error** ([Erp.Shared](src/Erp.Shared/Results/Result.cs)) — expected failures flow as values, not exceptions; `ErrorType` maps to HTTP status at the edge only.
- **Correlation-ID middleware** ([CorrelationIdMiddleware](src/Erp.Api/Middleware/CorrelationIdMiddleware.cs)) — honors/generates `X-Correlation-ID`, echoes it, adds it to the log scope; exposed app-wide via `ICorrelationContext`.
- **Global exception handler** ([GlobalExceptionHandler](src/Erp.Api/Errors/GlobalExceptionHandler.cs)) — never leaks stack traces; logs with correlation ID.
- **Health endpoint** `GET /api/v1/health` ([HealthController](src/Erp.Api/Controllers/HealthController.cs)).
- **Swagger** at `/swagger` (Development); root redirects there.

## Prerequisites

The .NET 10 SDK is installed at `~/.dotnet` (via `dotnet-install.sh`) and on `PATH`
through `~/.zprofile`. If `dotnet` is not found in a new shell, run
`source ~/.zprofile`.

## Commands

```bash
# Build & test
dotnet build
dotnet test                       # 7 unit + 3 integration tests

# Run the API (Swagger at the printed URL /swagger)
dotnet run --project src/Erp.Api

# Local PostgreSQL (requires Docker — not yet installed locally)
docker compose -f ../infra/docker-compose.yml up -d

# EF migrations
dotnet ef migrations add <Name> -p src/Erp.Infrastructure -s src/Erp.Api -o Persistence/Migrations
dotnet ef database update -s src/Erp.Api
```

## Notes / next slices

- **No DB required to boot.** With an empty `ConnectionStrings:Postgres`, the
  DbContext registers without a provider so Swagger/health work before Postgres
  exists. Local dev creds live in `appsettings.Development.json` (local-only, not
  a secret); real environments read from **AWS Secrets Manager** (CLAUDE.md §4.4).
- **Next:** tenant-resolution + RLS plumbing (ROADMAP Phase 0) — needs Docker for
  Testcontainers — then **Auth core** (Phase 1 · slice 1). Auth requires the
  password-hashing decision (BCrypt cost 12 **vs** ASP.NET PBKDF2 — CLAUDE.md §4.5)
  to be made first.
