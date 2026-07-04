# ADR-001: Development Workflow

**Status:** Accepted

## Context

CritterOps is currently developed by a single developer but is intended to grow into a long-term software project. The development workflow should encourage small, isolated changes while remaining simple enough to avoid unnecessary overhead.

## Decision

The project will use a simplified Git Flow workflow.

The repository will contain two long-lived branches:

* main
* develop

All development work will begin from develop.

Every GitHub Issue will have its own feature branch named using the issue number followed by a short descriptive title.

Example:

12-create-feeding-log-entity

Feature branches will merge into develop using Pull Requests.

When a release is considered stable, develop will be merged into main.

## Consequences

### Advantages

* Stable production branch.
* Predictable release process.
* Easy rollback if necessary.
* Small, isolated Pull Requests.
* Strong traceability between Issues, branches, and Pull Requests.

### Trade-offs

* Requires an extra merge during releases.
* Slightly more ceremony than committing directly to main, but significantly improves organization and maintainability.