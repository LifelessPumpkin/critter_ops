# Skipper API

The backend API for the CritterOps application, built using ASP.NET Core 10.

## Getting Started

### Prerequisites

Ensure you have the .NET SDK (version 10.0) installed. You can verify this by running:

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

Skipper API uses **Entity Framework Core 10** with the **Npgsql** PostgreSQL provider. The `ProfessorDbContext` is the primary DbContext and is registered with the ASP.NET Core DI container.

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

Run these commands from `apps/skipper-api`. Models, mappings, and migrations now live in
`packages/professor-persistence`, shared with [Howell Sync](../howell-sync/README.md).
Skipper retains the design-time factory and configuration; migration IDs and schema are unchanged.

Install the `dotnet-ef` CLI tool if you haven't already:

```bash
dotnet tool install --global dotnet-ef --version 10.0.12
```

> **Note:** After installation you may need to add `~/.dotnet/tools` to your `PATH`. Follow the instructions printed by the installer.

#### Apply migrations to the local database

```bash
dotnet ef database update --project ../../packages/professor-persistence --startup-project .
```

#### Create a new migration

```bash
dotnet ef migrations add <MigrationName> --project ../../packages/professor-persistence --startup-project . --output-dir Data/Migrations
```

#### Remove the last unapplied migration

```bash
dotnet ef migrations remove --project ../../packages/professor-persistence --startup-project .
```

### Health Endpoints

| Endpoint | Purpose | Fails when |
|---|---|---|
| `GET /health` | API liveness | Never — returns `200 Healthy` as long as the process is up |
| `GET /health/db` | PostgreSQL connectivity | DB is unreachable → `503 Unhealthy` |

The liveness endpoint intentionally has **no dependency on the database**, so a DB outage never causes the API process itself to appear down. The `GET /health/db` check uses `AddDbContextCheck<ProfessorDbContext>` and is the correct endpoint for readiness/dependency monitoring.

Neither endpoint blocks application startup.

## Docker image

Run the build from the **CritterOps monorepo root**, the directory containing
`apps`, `packages`, and `docker-compose.yml`:

```bash
cd /path/to/critter_ops
docker build -f apps/skipper-api/Dockerfile -t critterops/skipper:local .
```

Replace `/path/to/critter_ops` with your checkout's path. `-f` selects the
Dockerfile; `-t` names the resulting local image; the final `.` supplies the
current directory as the build context. Keep that final dot, and do not run
this command from `apps/skipper-api`. The build only copies Skipper. If shared
project references or root-level NuGet/MSBuild configuration are introduced,
include their restore inputs before restore and their sources before publish.

Start the image and check the API:

```bash
docker run -d --name critterops-skipper -p 5198:5198 critterops/skipper:local
curl --fail http://localhost:5198/health
docker logs critterops-skipper
docker restart critterops-skipper
docker stop critterops-skipper
docker rm critterops-skipper
```

The internal HTTP port defaults to **5198**, matching Skipper's local HTTP
launch profile. In `-p HOST_PORT:CONTAINER_PORT`, only the host port must be
unique on your machine. Containers have separate network namespaces and can
use the same internal port. For example, `-p 5000:5198` exposes this image at
`http://localhost:5000`. `EXPOSE` documents a port; it does not reserve or
publish a host port. An already-running local Skipper process on host port
5198 requires choosing another host port for the container.

`ASPNETCORE_HTTP_PORTS` controls the internal port; `ASPNETCORE_URLS` also
works and takes precedence. For example:

```bash
docker run --rm -e ASPNETCORE_HTTP_PORTS=9090 -p 5000:9090 critterops/skipper:local
```

The multi-stage Dockerfile uses official .NET 10 SDK and ASP.NET Core runtime
images, caches dependency restore, and publishes Release output. The runtime
contains published artifacts, runs as the built-in non-root `APP_UID` user,
and contains no SDK, source, tests, or added OS packages.

### Health monitoring

The API retains `GET /health` for liveness and `GET /health/db` for PostgreSQL
connectivity. There is **no image-level Docker HEALTHCHECK or probe utility**.
A deployment platform or external monitor can call these endpoints. Docker
reports running/stopped state but does not automatically report `healthy` or
`unhealthy` for this image. If Docker-native health status is required later,
a client inside the container or another deliberate probe mechanism is needed;
an HTTP endpoint alone does not make Docker poll it.

### Runtime configuration

The default environment is Production. Normal ASP.NET Core configuration
overrides work, including `ASPNETCORE_ENVIRONMENT`,
`ConnectionStrings__ProfessorDb`, and indexed CORS values such as
`Cors__AllowedOrigins__0`. Development appsettings and local secrets are
excluded from the build context; even when selecting Development in a container,
provide your own database and CORS configuration.

Liveness and startup work with no connection string or an unavailable database.
Database endpoints need a valid connection string and, for data operations,
an existing schema. The image does not create databases or apply migrations.

For database access, prepare an uncommitted local environment file outside the
build context, such as `/tmp/skipper-runtime.env`:

```dotenv
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__ProfessorDb=Host=postgres;Port=5432;Database=critterops;Username=<user>;Password=<password>
```

Then start with the network on which your database is reachable:

```bash
docker run -d --name critterops-skipper --network <database-network> \
  --env-file /tmp/skipper-runtime.env -p 5198:5198 critterops/skipper:local
curl --fail http://localhost:5198/health/db
```

Use a Compose service name, network hostname, or external PostgreSQL hostname
in `Host`; `localhost` inside this container refers to Skipper itself. Protect
and remove the runtime environment file when no longer needed. Runtime
variables are visible to users with Docker inspection access. Production
secrets should come from the deployment platform's secret manager; file-based
Docker secrets need future configuration support.

The image is stateless and needs no application volume. Persistent data lives
in PostgreSQL. Standard Docker, Compose, and Swarm use the same image with
runtime configuration, networks, and port mappings. OCI labels are included;
the image can later be tagged as `ghcr.io/<owner>/<repository>/skipper:<tag>`.
Publishing, signing, deployment automation, Compose/Swarm stacks, and production
health monitoring are follow-up work.

Mary Ann's existing pre-merge Skipper unit job builds this Dockerfile using
.NET 10. It does not publish or deploy the image.
