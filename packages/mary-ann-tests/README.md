# Mary Ann Tests

Mary Ann provides centralized local and CI test orchestration. Skipper's dedicated
xUnit projects live under `tests/skipper/{Unit,Integration}`; Gilligan's Vitest tests
remain inside `apps/gilligan-web`.

```sh
make test
make test-skipper
make test-gilligan
./packages/mary-ann-tests/scripts/test-skipper.sh unit
./packages/mary-ann-tests/scripts/test-skipper.sh integration
```

Requires .NET 8, Node.js 22, Gilligan dependencies (`npm ci`), and a running Docker
daemon for disposable PostgreSQL integration tests. Missing projects or dependencies
fail clearly. All suites are attempted and failures propagate to callers and CI.

See [Testing and CI](../../documentation/testing-and-ci.md) for isolation, regression
conventions, workflow triggers, and branch protection requirements.
