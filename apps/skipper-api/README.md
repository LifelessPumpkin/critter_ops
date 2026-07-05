# Skipper API

The backend API for the CritterOps application, built using ASP.NET Core 8.

## Getting Started

### Prerequisites

Ensure you have the .NET SDK (version 8.0 or later) installed. You can verify this by running:

```bash
dotnet --version
```

### Build the Project

To build the project and verify there are no compilation errors:

```bash
dotnet build
```

### Run the Project

To run the application locally:

```bash
dotnet run
```

Or run with a specific launch profile (e.g., HTTP on port `5198`):

```bash
dotnet run --launch-profile http
```

### Endpoints

- **Swagger UI**: [http://localhost:5198/swagger](http://localhost:5198/swagger) (Development environment)
- **Health — Liveness**: [http://localhost:5198/health](http://localhost:5198/health) (Returns `Healthy` whenever the API process is running)
- **Health — Database**: [http://localhost:5198/health/db](http://localhost:5198/health/db) (Checks PostgreSQL connectivity via `ProfessorDbContext`)

---

## Database

Skipper API uses **Entity Framework Core 8** with the **Npgsql** PostgreSQL provider. The `ProfessorDbContext` is the primary DbContext and is registered with the ASP.NET Core DI container.

### Local Prerequisites

1. Start the PostgreSQL container from the repo root:

   ```bash
   docker compose up -d
   ```

   This starts `postgres:16-alpine` on port **5433** (see `.env`).

2. Ensure `appsettings.Development.json` contains the correct connection string (already pre-configured for local development):

   ```json
   "ConnectionStrings": {
     "ProfessorDb": "Host=localhost;Port=5433;Database=critterops_dev;Username=postgres;Password=postgres"
   }
   ```

### EF Core Migrations

Install the `dotnet-ef` CLI tool if you haven't already:

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

> **Note:** After installation you may need to add `~/.dotnet/tools` to your `PATH`. Follow the instructions printed by the installer.

#### Apply migrations to the local database

```bash
dotnet ef database update
```

#### Create a new migration

```bash
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
```

#### Remove the last unapplied migration

```bash
dotnet ef migrations remove
```

### Health Endpoints

| Endpoint | Purpose | Fails when |
|---|---|---|
| `GET /health` | API liveness | Never — returns `200 Healthy` as long as the process is up |
| `GET /health/db` | PostgreSQL connectivity | DB is unreachable → `503 Unhealthy` |

The liveness endpoint intentionally has **no dependency on the database**, so a DB outage never causes the API process itself to appear down. The `GET /health/db` check uses `AddDbContextCheck<ProfessorDbContext>` and is the correct endpoint for readiness/dependency monitoring.

Neither endpoint blocks application startup.
