# Database setup (PostgreSQL)

This project uses PostgreSQL for production and Docker compose for local development.

Start Postgres + pgAdmin locally:

```bash
docker-compose up -d
```

pgAdmin is available at http://localhost:8080 (login: admin@local / admin).

Connection string (used in `backend/YAvuzeli.API/appsettings.json`):

```
Host=localhost;Port=5432;Database=yavuzeli_db;Username=yavuzeli;Password=yavuzeli_pwd
```

Migrations (once `dotnet-ef` tool is available):

```bash
dotnet tool install --global dotnet-ef
cd backend/YAvuzeli.Infrastructure
dotnet ef migrations add InitialCreate -s ../YAvuzeli.API -p .
dotnet ef database update -s ../YAvuzeli.API -p .
```

If you prefer using Docker for the DB during migrations, ensure Postgres container is running before `dotnet ef database update`.
