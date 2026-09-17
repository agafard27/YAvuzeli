# YAvuzeli Architecture Overview

Layered, modular architecture:

- `YAvuzeli.Domain` — Entities and domain primitives (no external deps).
- `YAvuzeli.Application` — Use-cases, DTOs, service interfaces.
- `YAvuzeli.Infrastructure` — EF Core DbContext, repositories, external integrations.
- `YAvuzeli.API` — ASP.NET Core Web API exposing application services.
- `YAvuzeli.Client` — WPF desktop client (MVVM) consuming the API.

Principles:
- Single Responsibility per project and class
- Dependency Inversion: higher layers depend on abstractions in Application/Domain
- Explicit project references only from higher to lower layers
- DTOs for API boundaries

Next steps:
- Implement repositories and concrete services in `Infrastructure`.
- Add migrations and initial DB seeding (SQLite for local/dev, Postgres/MS SQL for prod).
- Implement authentication and role-based authorization.
