# Documentation index

Use this index to find the authoritative guide for each topic. Update that guide when behavior
changes; avoid duplicating contracts in reports or creating permanent per-ticket summaries.

## Architecture and domain

| Topic | Maintained source |
| --- | --- |
| System boundaries and deployment status | [System architecture](architecture/README.md) |
| Project paths, identities, and responsibilities | [Repository structure](project-structure.md) |
| Animal identity and lifecycle | [Animal domain](architecture/animal-domain-design.md) |
| Animal groups, species, catalog API entry points, and enclosure-return drafts | [Animal reference catalog](architecture/animal-reference-catalog.md) |
| Multiple structured/custom characteristics and migration audit | [Variant assignments](animal-variant-assignments.md) |
| Catalog provenance, names, ownership, and constraints | [Catalog metadata](architecture/animal-catalog-metadata.md) |
| Current catalog search and paging contract | [Catalog search](architecture/catalog-search.md) |
| Measured query behavior and reproduction | [Search performance](performance/catalog-search.md) |
| Enclosures | [Enclosure domain](architecture/enclosure-domain-design.md) |
| Shared historical activities and task completion | [Activity ledger](architecture/activity-ledger-domain-design.md) |
| Animal and enclosure timeline contracts | [Animal timeline](architecture/animal-timeline-domain-design.md), [enclosure timeline](architecture/enclosure-timeline-domain-design.md) |

## Development, operations, and security

| Topic | Maintained source |
| --- | --- |
| Engineering policy and project-specific testing philosophy | [Engineering standards](engineering-standards.md) |
| First-time setup and local application startup | [Local development](local-development.md) |
| Database environment, connection, volume lifecycle | [Local PostgreSQL](operations/local-database.md) |
| Skipper build, migrations, API conventions, image/runtime configuration | [Skipper README](../apps/skipper-api/README.md) |
| Gilligan startup, runtime API proxy, container deployment | [Gilligan README](../apps/gilligan-web/README.md) |
| Howell configuration, provider rules, CLI, locking, retries, logs | [Howell Sync README](../apps/howell-sync/README.md) |
| Development catalog profiles, import, verification, troubleshooting | [Local catalog setup](operations/local-catalog-setup.md) |
| Test commands, isolation, and CI checks | [Testing and CI](testing-and-ci.md) |
| Security scanning, CI enforcement, evidence retention | [Security scanning](security-scanning.md) |
| Exceptions, OpenVEX, and local policy validation | [Security policy](../security/README.md) |
| Contribution and completion | [GitHub workflow](github-workflow.md), [definition of done](definition-of-done.md) |
| Ginger UI components and tokens | [Gilligan design system](gilligan-design-system.md) |

## Retained decisions and historical material

- [ADR-001](decisions/adr-001-development-workflow.md) records the accepted branch/workflow decision.
- [Legacy animal reference migration](architecture/legacy-animal-reference-migration.md) records the
  original matching and preservation rules. Its historical single-variant sections are explicitly
  scoped to that migration; current variant storage is in the assignment guide above.
- [Migration validation SQL](operations/validate-animal-reference-migration.sql) remains an operational
  migration-time check. Retain audit data and review populated rollback restrictions.
- [Catalog verification output](operations/catalog-verification-results.txt) preserves a dated isolated
  development run, not a live deployment inventory. Its context is in the local catalog guide.
- [UI structure specification](web-design.md) retains design intent and proposed workflows, not an
  inventory of shipped features. The design-system guide describes maintained components.

The architecture guides distinguish implemented behavior from future extension ideas. Keep useful
historical rationale, migration evidence, and measurements; remove superseded file lists, temporary
implementation states, and duplicated test-run summaries instead of automatically archiving them.
