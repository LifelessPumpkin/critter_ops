# System architecture

CritterOps manages animal husbandry through a web application and API backed by PostgreSQL.
See the [repository structure](../project-structure.md) for the authoritative project map and
[documentation index](../README.md) for domain contracts.

```text
Browser → Gilligan /api/skipper proxy → Skipper → Professor PostgreSQL
External catalog provider → Howell Sync → Professor PostgreSQL
```

Gilligan renders user workflows with the Ginger UI components and tokens. Its server-side proxy
uses runtime `SKIPPER_API_URL`; browsers use relative API URLs. Skipper owns HTTP endpoints and
application services. Professor Persistence owns the shared EF Core model, constraints, and
migrations used by both Skipper and Howell; Skipper retains the EF design-time factory.

Howell is a standalone run-once worker with a GBIF adapter, bounded import/sync scopes,
transactional batches, a PostgreSQL session advisory lock, and retained retry/history state.
Normal animal entry, API startup, and catalog search use persisted local data, independent of
provider availability. There is no catalog administration UI or built-in scheduler.

## Domain boundaries

An Animal has one current Enclosure, optional catalog Species/group references or permanent custom
species text, and multiple structured/custom variant assignments. Internal IDs remain authoritative
across external taxonomy changes. Species defines catalog variants and optional preferred variant
terminology. Consult [animal identity](animal-domain-design.md), [catalog metadata](animal-catalog-metadata.md),
[assignments](../animal-variant-assignments.md), and [search contracts](catalog-search.md).

The [activity ledger](activity-ledger-domain-design.md) records shared history associated with
animals/enclosures; timeline APIs project that history. Task completion links operational work to
activity records. Migration audit snapshots retain historical reference information and are not
substitutes for database backups.

## Infrastructure and deployment

Implemented infrastructure consists of database-only Docker Compose, Skipper and Gilligan
Dockerfiles, and pre-merge build/test/security workflows. The Compose `db` service runs
PostgreSQL 16 with a named volume. Apps do not automatically apply database migrations.

Use the [Skipper image/runtime guide](../../apps/skipper-api/README.md#docker-image) and
[Gilligan deployment guide](../../apps/gilligan-web/README.md#production-docker-image) for build
commands, runtime configuration, networking and health-validation limitations. Skipper's container
default port is 5198; Gilligan's is 3000. Gilligan does not connect directly to the database.

A full application Compose/Swarm stack, reverse proxy/TLS configuration, registry publishing,
production deployment automation, backup/restore runbook, and scheduled Howell execution remain
future operational work. Example future service names/ports in app guides are not deployed services.
See [security scanning](../security-scanning.md) for existing gates and retained evidence.
