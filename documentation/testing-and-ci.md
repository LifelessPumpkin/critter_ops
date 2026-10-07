# Testing and CI

Mary Ann (`packages/mary-ann-tests`) orchestrates tests; application test implementations
belong to their application. Skipper production code lives in `apps/skipper-api`,
xUnit projects in `tests/skipper/Unit` and `tests/skipper/Integration`, and Gilligan
Vitest/React Testing Library tests remain within `apps/gilligan-web`.

## Local execution

Install .NET SDK 8, Node.js 22, and Gilligan dependencies (`npm ci` in
`apps/gilligan-web`). Start a Docker-compatible daemon for integration tests.

```sh
make test
make test-skipper
make test-gilligan
./packages/mary-ann-tests/scripts/test-skipper.sh unit
./packages/mary-ann-tests/scripts/test-skipper.sh integration --configuration Release
```

`test-all.sh` attempts both applications and returns nonzero if either fails.
Skipper validates expected projects before running; it never tests the production
project as a fallback. Missing tooling, missing dependencies, failed containers,
failed migrations, builds, and test failures cause unsuccessful execution.

## PostgreSQL isolation

Integration tests use Testcontainers.PostgreSql with `postgres:16-alpine`, matching
Professor. Each test receives a randomly named database within a disposable container
with a random host port. Tests apply the real Skipper EF Core migrations and use the
production context without SQLite-specific model overrides. They construct connections
exclusively from the container, never from appsettings, `.env`, or production secrets.
Class fixtures dispose containers, and Testcontainers resource cleanup covers abandoned
resources. Parallel classes have independent containers; parallel tests have independent
databases. Docker and image-pull access are prerequisites; failures are never skipped.
See [Testcontainers PostgreSQL documentation](https://dotnet.testcontainers.org/modules/postgres/).

Unit tests require no database or Docker. Add regression tests for actual business rules,
request validation, and application results. Future LanguageExt Option/Fin/domain-error
coverage belongs here when that migration lands. Integration coverage protects persistence,
relationships, constraints, and transactional workflows. Gilligan tests should protect
critical user behavior, loading/errors, filters, forms, navigation, and timelines rather
than duplicate presentation markup. The existing enclosure-name regression is retained.

## GitHub Actions and required checks

`Mary Ann - Regression Testing` runs on PRs and pushes to `develop` and `main`.
Ubuntu runners provide Docker for integration tests. Jobs build Skipper, run isolated
unit/integration tests through Mary Ann, and install, build, and test Gilligan. No local
configuration files or persistent databases are needed.

Require these exact job names in branch protection or repository rulesets:

- `Skipper Build / Unit Tests`
- `Skipper Integration Tests`
- `Gilligan Build / Regression Tests`

`Mary Ann - Security Scanning` preserves the workflow from the repository's
`64-add-automated-repository-security-scanning` branch, changing only its display name
and filename. Semgrep, Trivy, and TruffleHog retain their pinned tools, failure flags,
permissions, and push-to-develop/manual triggers.

Neither main nor develop currently has branch protection. Protection has not been
changed remotely as part of these uncommitted changes. Enable regression checks after
publishing the workflow. Security checks cannot be required on PRs under the preserved
push-only policy: first explicitly authorize PR security triggers, then require
`Semgrep SAST`, `Trivy Dependency Scan`, and `TruffleHog Secret Scan`. Future deployment
workflows should depend on successful regression and security runs; deployment is outside
this change.

## Task activity coverage

Task/activity tests live in `tests/skipper/Integration` and use the shared PostgreSQL
fixture. They cover completion metadata, relationships, idempotency, and recurrence.
The former local test file referenced mapping and operational activity APIs absent
from this checkout. Those cases were removed during integration; add their regression
coverage with the corresponding application features. Current task completion records
use `ActivityEventType.Task`.

## Validation in GitHub

After publishing, verify PRs to both branches and pushes execute these workflows. A
failing test must fail its job; security failures must remain separately visible. Do
not introduce intentionally failing commits on shared protected branches to validate CI.
