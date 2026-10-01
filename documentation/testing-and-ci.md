# Testing and CI

CritterOps uses `packages/mary-ann-tests` as the shared testing foundation for
the monorepo.

## Run Tests Locally

From the repository root:

```sh
make test
```

Run only Gilligan:

```sh
make test-gilligan
```

Run only Skipper:

```sh
make test-skipper
```

## Local Requirements

Install:

* Node.js
* npm dependencies for Gilligan with `npm ci` from `apps/gilligan-web`
* .NET SDK 8

## Exit Codes

Each test command returns the exit code from the underlying tool. A frontend lint
failure, backend build/test failure, or future unit test failure exits non-zero.
The root `make test` command exits non-zero as soon as either application test
command fails.

## GitHub Actions

The `CritterOps Tests` workflow runs on pull requests targeting `develop` and
`main`. The stable job name is:

```text
test
```

The recommended required branch protection status check is:

```text
CritterOps Tests / test
```

GitHub branch protection or repository rulesets must be configured separately to
require that check before merging.

## Future Test Locations

Add frontend tests inside `apps/gilligan-web`. Gilligan currently uses Vitest
and React Testing Library through its `npm test` script.

The first backend integration test project lives in
`apps/skipper-api/tests/Skipper.Api.Tests`. Add future backend unit or
integration test projects under `apps/` or `packages/` using project names that
match `*Tests*.csproj` or `*.Tests.csproj`; the Skipper test script will
discover and run them automatically.

Do not put database-dependent integration tests into the default CI path until
their service requirements are documented and provisioned in the workflow.
