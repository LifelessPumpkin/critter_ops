# Testing and CI

Mary Ann test entry points in `scripts/` orchestrate tests; application test implementations
belong to their application. Skipper production code lives in `apps/skipper-api`,
xUnit projects in `tests/skipper/Unit` and `tests/skipper/Integration`, and Gilligan
Vitest/React Testing Library tests remain within `apps/gilligan-web`.

## Local execution

Install .NET SDK 10, Node.js 22, and Gilligan dependencies (`npm ci` in
`apps/gilligan-web`). Start a Docker-compatible daemon for integration tests.

```sh
make test
make test-skipper
make test-gilligan
make test-security
make test-howell-script
./scripts/test-skipper.sh unit
./scripts/test-skipper.sh integration --configuration Release
```

`test-all.sh` attempts security policy, Skipper, Gilligan and Howell script suites and returns
nonzero if any fails. Security tests require Python 3.10+ and `security/requirements.txt`;
use a virtual environment and set `SECURITY_PYTHON` to its executable. See
[security test setup](../security/README.md) before running `make test` or `make test-security`.
`HOWELL_TEST_PYTHON` optionally selects Python for Howell script contracts.
Skipper validates expected projects before running; it never tests the production
project as a fallback. Missing tooling, missing dependencies, failed containers,
failed migrations, builds, and test failures cause unsuccessful execution.

## PostgreSQL isolation

Integration tests use Testcontainers.PostgreSql with `postgres:16-alpine`, matching
Professor. Each test receives a randomly named database within a disposable container
with a random host port. Tests apply the real Professor EF Core migrations and use the
production context without SQLite-specific model overrides. They construct connections
exclusively from the container, never from appsettings, `.env`, or production secrets.
Class fixtures dispose containers, and Testcontainers resource cleanup covers abandoned
resources. Parallel classes have independent containers; parallel tests have independent
databases. Docker and image-pull access are prerequisites; failures are never skipped.
See [Testcontainers PostgreSQL documentation](https://dotnet.testcontainers.org/modules/postgres/).

Unit tests require no database or Docker. Add regression tests for actual business rules,
request validation, and application results. LanguageExt is optional; test meaningful domain
outcomes if adopted, without requiring a conversion. Integration coverage protects persistence,
relationships, constraints, and transactional workflows. Gilligan tests should protect
API request contracts, failures/recovery, forms, custom-value preservation, complex async
behavior, and keyboard accessibility. Avoid presentation/layout tests and duplicated coverage.
See [application testing standards](engineering-standards.md#testing-by-application).

## GitHub Actions and required checks

`Mary Ann - Regression Testing` runs on PRs targeting `develop` and `main`, with no push trigger.
Ubuntu runners provide Docker for integration tests. Jobs build Skipper, run isolated
unit/integration tests through Mary Ann, and install, build, and test Gilligan. No local
configuration files or persistent databases are needed.

The Skipper unit job also validates the production Docker image with
`docker build -f apps/skipper-api/Dockerfile -t critterops/skipper:ci .` from
the monorepo root. This is build validation only; it does not publish or deploy.
See [Skipper Docker instructions](../apps/skipper-api/README.md#docker-image)
for local runtime and health validation.

Require these exact job names in branch protection or repository rulesets:

- `Skipper Build / Unit Tests`
- `Skipper Integration Tests`
- `Gilligan Build / Regression Tests`

`Mary Ann - Security Scanning` runs independently on the same PR targets and supports
manual execution. Require `Mary Ann Security Scanning`, `Semgrep SAST`,
`Trivy Dependency Scan`, and `TruffleHog Secret Scan` alongside the three regression
checks above. Workflow display names are not status-check names. The workflows build and test applications; frontend lint is currently a local check, not a CI job.

Branch protection was not changed remotely. See
[Security scanning](security-scanning.md#required-checks-and-protected-branches)
for exact ruleset settings, reporting permissions, fork behavior and hosted validation.
Neither workflow reruns automatically after merge. Deployment automation is not implemented.

## Task activity coverage

Task/activity tests live in `tests/skipper/Integration` and use the shared PostgreSQL
fixture. They cover completion metadata, relationships, idempotency, and recurrence.
Typed task completion records use their corresponding activity types (including
Feeding and Cleaning); Other tasks retain the generic Task activity. Integration
assertions protect structured details as well as completion metadata and relationships.

## Validation in GitHub

After publishing, verify PRs to both branches execute these workflows and merges do not rerun them. A
failing test must fail its job; security failures must remain separately visible. Do
not introduce intentionally failing commits on shared protected branches to validate CI.

See [Animal reference catalog and enclosure return workflow](architecture/animal-reference-catalog.md) for schema, compatibility, API contracts, draft behavior, and verification guidance.

## Howell and script contracts

Howell CLI/configuration/provider tests run in the Skipper unit project. PostgreSQL integration
tests cover import correctness, idempotency, lifecycle, advisory locking, retries, batch rollback,
dry runs, cancellation, and process crashes. The process harness lives only in the test assembly.

`make test-howell-script` runs standard-library Python contracts for profile/default forwarding,
dry-run/retry flags, missing configuration and exit propagation. It is included in `make test`.
Real-provider manual runs are separate, bounded operations described in
[local catalog setup](operations/local-catalog-setup.md). Security scanning remains an independent
CI gate; policy unit tests are not substitutes for running scanners.
