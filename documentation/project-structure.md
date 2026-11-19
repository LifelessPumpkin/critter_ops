# Repository structure

This is the authoritative map of the CritterOps monorepo. Run repository-level commands
from the directory containing `apps/`, `packages/`, and `docker-compose.yml`.

| Location | Responsibility |
| --- | --- |
| `apps/skipper-api/` | Skipper: ASP.NET Core API, HTTP DTOs, application services, and the EF design-time factory. |
| `apps/gilligan-web/` | Gilligan: Next.js frontend. Ginger components live in `src/components/ui`; tokens live in `src/app/globals.css`. |
| `apps/howell-sync/` | Howell Sync: standalone .NET catalog importer/synchronizer with GBIF retrieval, bounded execution, and recovery. |
| `packages/professor-persistence/` | Professor: shared EF Core context, domain entities, mappings, and all database migrations, referenced by Skipper and Howell. |
| `tests/` | Skipper/Professor/Howell xUnit suites under `skipper/`; Python script contracts under `howell/`; security policy/reporting tests under `security/`. Gilligan tests stay beside frontend code. |
| `scripts/` | Repository tooling: Mary Ann test entry points and local catalog import/verification scripts. |
| `documentation/` | Current architecture, development, operations, design guidance, and retained decisions; see the [index](README.md). |
| `security/` | Scanning entry point, report generation, fail-closed exception evaluator, OpenVEX schema, and exception policy. |
| `artifacts/` | Generated build/test/security output. Security evidence is retained by CI; local generated files are ignored selectively. |
| `.github/` | Regression/security workflows, issue templates, and PR template. |

`docker-compose.yml` currently runs only PostgreSQL (`db`) with a named persistent volume.
The two application Dockerfiles live beside their apps; there is no deployed Swarm stack,
reverse proxy, registry publishing pipeline, or standalone infrastructure project.

## Project identities and issue labels

Use `skipper` for API work, `gilligan` for frontend behavior, `professor` for persistence,
and `ginger` for UI components/design. Howell Sync owns external catalog synchronization.
Mary Ann names testing and CI. The infrastructure identity is Millionare (the GitHub label
is `millionaire`). Identities describe responsibilities; they do not require separate packages.

Build, run, migration and test commands belong in [local development](local-development.md),
[Testing and CI](testing-and-ci.md), and the application READMEs rather than another project map.
