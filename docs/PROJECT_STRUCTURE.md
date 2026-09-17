# Project structure

Top-level layout (short):

- `backend/` — Server projects and libraries
  - `YAvuzeli.API` — ASP.NET Core Web API
  - `YAvuzeli.Domain` — Domain entities
  - `YAvuzeli.Application` — DTOs and service interfaces
  - `YAvuzeli.Infrastructure` — EF Core DbContext and repositories
  - `YAvuzeli.Shared` — Common utilities
- `client/` — WPF desktop client (MVVM)
- `docs/` — Documentation files (this file)
- `IDE_SETUP.md` — Visual Studio install/open instructions
- `open-in-vs.bat` — Helper to open solution

Notes & recommendations:
- Open `YAvuzeli.sln` in Visual Studio for the best WPF designer and debugging experience.
- Keep domain models in `YAvuzeli.Domain` and service implementations in `YAvuzeli.Infrastructure`.
- When adding tests, create a `tests/` folder and add unit/integration projects there.
