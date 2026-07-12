# Mary Ann Tests

Mary Ann is the shared test orchestration package for the CritterOps monorepo.
It provides stable commands for local development and CI without introducing a
large monorepo task runner.

## Local Commands

Run all current test commands from the repository root:

```sh
make test
```

Run Gilligan only:

```sh
make test-gilligan
```

Run Skipper only:

```sh
make test-skipper
```

The underlying scripts can also be run directly:

```sh
./packages/mary-ann-tests/scripts/test-all.sh
./packages/mary-ann-tests/scripts/test-gilligan.sh
./packages/mary-ann-tests/scripts/test-skipper.sh
```

## Dependencies

Local test runs require:

* Node.js for `apps/gilligan-web`
* npm dependencies installed with `npm ci` in `apps/gilligan-web`
* .NET SDK 8 for `apps/skipper-api`

## Current Test Wiring

Gilligan runs its native npm test command. The current frontend test runner is
Vitest with React Testing Library:

```sh
npm test
```

Skipper runs discovered future .NET test projects matching `*Tests*.csproj` or
`*.Tests.csproj`. Until a dedicated backend test project exists, the script runs
`dotnet test apps/skipper-api/skipper-api.csproj`, which validates that the
current backend project builds through the .NET test command.

Any failing command exits non-zero. The root `make test` command stops and exits
non-zero if either Gilligan or Skipper fails.

## Future Tests

Add frontend unit tests under `apps/gilligan-web` and update that app's native
`npm test` script to run the chosen test runner.

The first backend integration tests live in
`apps/skipper-api/tests/Skipper.Api.Tests`. Add future backend unit or
integration test projects under `apps/` or `packages/` with project names
matching `*Tests*.csproj` or `*.Tests.csproj`; Mary Ann will pick them up
automatically.

Database integration tests should live in a dedicated test project and should
document any required service dependencies before being added to CI.

## CI

The GitHub Actions workflow `CritterOps Tests` runs on pull requests targeting
`develop` and `main`. It installs Gilligan dependencies, restores Skipper
dependencies, and runs:

```sh
make test
```

Use this required branch protection status check:

```text
CritterOps Tests / test
```
