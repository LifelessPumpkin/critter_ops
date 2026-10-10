# Repository Security Scanning

CritterOps runs the **Security Scanning** GitHub Actions workflow on every push to `develop`, including pull requests merged into that branch. Maintainers can also run it manually with `workflow_dispatch` from the Actions tab.

The workflow runs three independent, blocking jobs so one scanner failure does not prevent the others from reporting results:

- **Semgrep SAST** scans the monorepo's supported source and configuration files with Semgrep's maintained default ruleset. Generated dependencies, build output, and coverage directories are excluded.
- **Trivy Dependency Scan** scans repository dependency metadata for `HIGH` and `CRITICAL` vulnerabilities. The committed npm lock file is scanned directly; the job runs a .NET restore to create current NuGet lock metadata for `apps/skipper-api/skipper-api.csproj` before scanning. A matching vulnerability fails the job.
- **TruffleHog Secret Scan** checks the complete Git history and current repository contents. Verified secrets fail the job, and scanner errors are also blocking.

The workflow uses read-only repository permissions, requires no paid scanner service, and does not build, deploy, or scan container images. Scanner findings and execution errors are reported in each job's GitHub Actions log. Developers do not need to install these scanners for normal local development.

## Action and dependency maintenance

Pin every third-party `uses:` reference to a full 40-character upstream commit SHA,
with the exact release version in a comment. Verify the release tag and commit via
the action's official repository before updating. Keep action major versions unless
a compatibility review justifies changing them. The testing workflow retains v4
actions (Node 20 action runtime), Node 22 for Gilligan, and .NET 8 for Skipper;
checkout v4.4.0's safer `pull_request_target` defaults do not affect its existing
`push` and `pull_request` triggers.

Gilligan uses npm inside `apps/gilligan-web`, without npm workspaces. Inspect fresh
scanner advisories and `npm explain` before changing dependencies. Prefer updating
the introducing dependency, keep `next` and `eslint-config-next` aligned, regenerate
the lockfile with npm, and validate with `npm ci`. Add overrides only when a parent
upgrade cannot safely resolve a finding, with a documented compatibility rationale.

## Local scanner commands

Run from the repository root with Docker available. These use CI's Semgrep image,
Trivy version (the pinned action defaults to 0.70.0), scope, severity thresholds,
and failure flags. Restore first so NuGet metadata is included.

```sh
dotnet restore apps/skipper-api/skipper-api.csproj --use-lock-file

docker run --rm -v "$PWD:/src:ro" -w /src \
  semgrep/semgrep:1.179.0@sha256:93963d9295a366f59e4850127b1550400ee7b388f04fe144e4a1f6325d96e01b \
  semgrep scan --config p/default --error --strict --metrics off \
  --exclude '**/node_modules/**' --exclude '**/.next/**' \
  --exclude '**/bin/**' --exclude '**/obj/**' --exclude '**/build/**' \
  --exclude '**/dist/**' --exclude '**/coverage/**' .

docker run --rm -v "$PWD:/src:ro" -w /src \
  aquasec/trivy:0.70.0@sha256:be1190afcb28352bfddc4ddeb71470835d16462af68d310f9f4bca710961a41e \
  fs --scanners vuln --severity HIGH,CRITICAL --exit-code 1 \
  --ignore-unfixed=false \
  --skip-dirs '**/node_modules,**/.next,**/bin,**/obj,**/build,**/dist,**/coverage' .

docker run --rm -v "$PWD:/repo:ro" -w /repo \
  ghcr.io/trufflesecurity/trufflehog:3.97.9@sha256:52e67fef4d054ecff5c2ce4b4ae376626d1ef54aa0898b53cac19c25e92e14db \
  git file:///repo --results verified --fail --fail-on-scan-errors \
  --no-update --github-actions
```

Trivy excludes development/test dependencies by default. To inspect them as an
additional local check, add `--include-dev-deps`; this does not change CI policy.
Semgrep's `p/default` and the Trivy database change over time, so record versions,
date, exit statuses, and findings when reporting results.

## Remediation verification — October 10, 2026

All six mutable testing-action findings were resolved with verified v4 release
SHAs. Fresh CI-equivalent Trivy scanning reproduced 13 findings (10 High,
3 Critical), rather than the historical 15. Next.js 16.4.0 and matching ESLint
configuration replace 16.2.10; npm resolves Next's PostCSS to 8.5.23 (from 8.4.31)
and Sharp to 0.35.5 (from 0.34.5). No overrides or new direct dependencies were
needed. React/React DOM remain 19.2.4. nanoid 3.3.20 and source-map-js 1.2.2
already had no current High/Critical advisories and remain unchanged.

Semgrep completed with zero blocking findings; CI-equivalent Trivy completed with
zero High/Critical findings in Gilligan and restored Skipper metadata; TruffleHog
completed with zero verified secrets. Isolated original-file controls still fail
for the six mutable actions and 13 dependency findings. Missing configuration or
scan-target controls also return nonzero. Scanner policies and the security
workflow were preserved.

The additional development-dependency scan reports **braces 3.0.3,
CVE-2026-93687 / GHSA-vfj7-8cjw-p6xm, High**, introduced by
`eslint-config-next → @next/eslint-plugin-next → fast-glob → micromatch → braces`.
The [upstream advisory](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm) lists
`<=3.0.3` as vulnerable and no patched release. The latest compatible Next ESLint
configuration still introduces it. An override cannot select an available fix;
npm's proposed downgrade to Next ESLint 14 is incompatible with the chosen
framework version. Follow up when upstream releases a fix; no ignore was added.
npm audit reports this as five affected packages along the same dependency chain.

Clean npm installation, TypeScript, Gilligan's existing test, production build,
Skipper restore/build, and all 11 Skipper tests passed. Lint still reports six
pre-existing React Hooks errors in `AnimalOverview.tsx` and shared `Selection.tsx`,
reproduced using the original lockfile in an isolated directory. Resolve those in
a separate focused UI ticket; the lint configuration was not weakened.

Browser smoke tests used the production build and Skipper with a disposable
PostgreSQL database. Enclosure/animal/task creation, task completion, navigation,
timeline list/week rendering, type-filter clearing, shared selection controls, and
existing saved-view management rendering succeeded. Multi-page infinite scrolling
and saved-view write operations were not exercised.
