# Local development

Run commands from the repository root unless shown otherwise. Use .NET SDK 10, Node.js 22/npm,
Python 3.10+ for script/security tests, and Docker with Compose for PostgreSQL. Testcontainers
integration tests also require Docker and image-pull access. There is no root .NET solution or npm
workspace; use explicit project paths.

## Database and migrations

For a fresh local database:

```sh
cp .env.example .env
# Review local credentials and POSTGRES_PORT before starting.
docker compose up -d db
```

Compose reads `.env`; .NET processes do not automatically load it. Export a matching
`ConnectionStrings__ProfessorDb` in each terminal running Skipper, EF, or Howell. For unchanged
example credentials/port, this is:

```sh
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development
export ConnectionStrings__ProfessorDb='Host=localhost;Port=5432;Database=critterops_dev;Username=postgres;Password=postgres'
```

Use your configured credentials and host port. Skipper's checked-in Development fallback uses
5433, while `.env.example` uses 5432; the explicit connection setting avoids that mismatch.
Do not commit credentials. Existing Compose volumes retain their original database/user/password;
editing `.env` does not reinitialize them. See [local database operations](operations/local-database.md)
for connections, stopping and deliberately resetting disposable data.

Install EF CLI 10.0.12 once (`dotnet tool update --global dotnet-ef --version 10.0.12` if already
installed), then apply the shared migrations:

```sh
dotnet tool install --global dotnet-ef --version 10.0.12
(cd apps/skipper-api && CRITTEROPS_EF_DESIGN_TIME=true dotnet ef database update --project ../../packages/professor-persistence --startup-project .)
```

The migration role must be permitted to install `pg_trgm`, or an administrator must preinstall it.
Apps do not migrate automatically. Review [reference migration safeguards](architecture/legacy-animal-reference-migration.md)
and [assignment audit/rollback rules](animal-variant-assignments.md#migration-and-verification)
before migrating a database with retained animal history. Never reset a volume to bypass a migration problem.

## Start the applications

In one terminal with the database connection exported:

```sh
dotnet run --project apps/skipper-api --launch-profile http
```

In a second terminal:

```sh
npm --prefix apps/gilligan-web ci
SKIPPER_API_URL=http://localhost:5198 npm --prefix apps/gilligan-web run dev
```

Open `http://localhost:3000/animals` and `http://localhost:5198/swagger` (Development only).
The [Skipper README](../apps/skipper-api/README.md) and [Gilligan README](../apps/gilligan-web/README.md)
own app-specific configuration, build, API proxy and container behavior.

## Catalog and verification

Migrations install the small curated catalog. Larger GBIF-backed demo/expanded catalogs are optional;
follow [local catalog setup](operations/local-catalog-setup.md), including contact configuration,
development-target restrictions, bounded imports, dry runs, retries, and SQL verification.
The [Howell runbook](../apps/howell-sync/README.md) owns worker configuration and recovery.

```sh
dotnet build apps/skipper-api/skipper-api.csproj
dotnet build apps/howell-sync/howell-sync.csproj
make test-howell-script
```

See [Testing and CI](testing-and-ci.md) for application suites, disposable PostgreSQL isolation,
security-test dependencies and all-suite prerequisites. Read [engineering standards](engineering-standards.md)
and [contributor workflow](github-workflow.md) before contributing.
