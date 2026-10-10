# Repository Security Scanning

**Mary Ann - Security Scanning** runs on `pull_request` events targeting `develop`
and `main`, and on manual `workflow_dispatch`. **Mary Ann - Regression Testing**
runs independently on PRs to both branches. Neither workflow runs on pushes or on
a schedule. This follows the additional CI/CD requirement: validate before merge,
with no redundant post-merge suite and no deployment functionality in this ticket.
Manual security runs support new advisories and exception reviews.

Checkout's default PR merge ref validates the proposed merge, including target
branch integration; new PR commits rerun checks. TruffleHog fetches full history.
No path filters skip required validation. No `pull_request_target` is used.

## Enforcement and reporting

Three existing independent checks retain their names and policies:

- **Semgrep SAST** uses pinned Semgrep 1.179.0 and `p/default`, with `--error` and
  `--strict`. Native JSON and SARIF come from one scan. All reported findings block.
- **Trivy Dependency Scan** uses Trivy 0.70.0 to scan monorepo dependency metadata,
  including Gilligan npm development dependencies and restored Skipper NuGet
  lockfiles. Unapproved HIGH/CRITICAL vulnerabilities block, including unfixed
  findings. The existing `security/evaluate.py` policy is the only classification
  source; approved exact exceptions remain visible. Invalid, expired, stale or
  inconsistent exceptions block. No new exception mechanism is introduced.
- **TruffleHog Secret Scan** retains verified-only scanning, `--fail` and
  `--fail-on-scan-errors`. The wrapper captures stdout/stderr into temporary files
  outside the workspace and deletes them on exit. Only status and verified count
  are published; never raw values, detector payloads or scanner diagnostics.

**Mary Ann Security Scanning** waits for all three jobs with `always()` and fails
if any fails, is skipped/cancelled, or dependency summary evidence is unavailable.
Its Actions summary shows overall/job results, Semgrep and secret counts, and the
existing dependency summary with accepted/blocking counts, CVEs, packages, severity,
policy errors, and review dates. PASS with accepted findings is not vulnerability-free.
Execution/evaluation failures show unavailable counts rather than zero findings.
A job may also fail because report generation, validation or artifact upload failed;
inspect its failed step alongside the scanner status.

Semgrep/Trivy native SARIF is checked for required envelope/scanner/finding fields;
GitHub performs full ingestion validation. Trivy converts the same JSON scan rather
than rescanning. A reporting-only JSON copy restores VEX-suppressed original rows
before conversion, so accepted findings remain visible. It never changes policy
classification. SARIF retains native IDs, severity and available dependency locations;
the policy JSON/summary is authoritative for acceptance, which is not automatically
translated into GitHub alert dismissal. Existing lower-severity behavior remains:
Trivy collects HIGH/CRITICAL, while Semgrep reports its unchanged ruleset findings.

Reports are retained for **seven days** in this Actions run's Artifacts:
`mary-ann-semgrep-security` (JSON, SARIF, status),
`mary-ann-dependency-security` (Trivy JSON/SARIF, evaluation, summary, OpenVEX),
and `mary-ann-trufflehog-security` (sanitized status only).
Explicit allowlists exclude environment files and raw secret scan output.
Uploads run after findings; missing reports and scanner crashes never imply success.
Artifact upload failures fail their scanner job and the aggregate gate.

Separate **Code Scanning Reports (semgrep/trivy)** jobs download artifacts without
checking out or executing PR scripts. Only these jobs have `security-events: write`
and `actions: read`; scanner and gate jobs retain `contents: read`. New action pins
were resolved against upstream release tags. Code Scanning upload failures issue a
warning and preserve artifacts and security enforcement; upload success cannot
change a scanner failure. Do not require these optional reporting checks as gates.

Find uploaded results under **Security → Code scanning**; dependency alerts, if
configured, remain under **Security → Dependabot**. This public repository supports
Code Scanning, but enabled settings and actual ingestion still need a hosted run.
Private repositories require an eligible GitHub Code Security plan. See
[GitHub SARIF upload documentation](https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/integrate-with-existing-tools/upload-sarif-file).
There is no `.github/dependabot.yml` in this checkout; this ticket adds none.

Fork PRs run all scanners and gates with restricted tokens and no repository secrets.
Code Scanning reporting jobs are skipped for forks; safe artifacts and summaries
remain available. A maintainer may need to approve Actions execution for a new fork
contributor. No permissions are increased to work around fork restrictions.

## Required checks and protected branches

Branch protection was **not changed** by this implementation. In GitHub **Settings →
Rules → Rulesets**, edit the existing active branch rulesets targeting both `develop`
and `main` (or **Settings → Branches** for classic protection). Preserve existing
checks and protections, require a pull request, and add these exact GitHub Actions
check names after a first PR run registers them:

- `Mary Ann Security Scanning` (aggregate security gate)
- `Semgrep SAST`
- `Trivy Dependency Scan`
- `TruffleHog Secret Scan`
- `Skipper Build / Unit Tests`
- `Skipper Integration Tests`
- `Gilligan Build / Regression Tests`

The workflow display names themselves are not required status-check names. Select
GitHub Actions as the expected source where available. Require branches up to date
before merging, dismiss stale approvals, require review for security policy/workflow
changes, and restrict direct pushes and bypass actors according to existing policy.
Do not weaken protections. With PR-only CI, an administrator bypass/direct push has
no automatic validation; protection is essential. Merge/squash commits need no second
suite in this design. Merge queues (`merge_group`) are not configured by this ticket;
if enabled later, add their required-check triggers before enforcing the queue.
Future CD must verify successful validation for the merged change; no CD is added.

## Hosted verification still required

After review/publishing, open controlled PRs to both branches; confirm all seven
required checks, merge-ref checkout, summaries, downloadable artifacts and Code
Scanning categories. Use synthetic fixtures on a test branch to verify blocking
findings still publish evidence, then remove the fixture and verify green checks.
Test fork restrictions and upload-disabled fallback, manual scanning, stale checks
after new commits, and ruleset refusal to merge failing/stale checks. Confirm merges
produce no redundant Mary Ann runs. Never add real secrets or unsafe dependencies.

## Action and dependency maintenance

Pin every third-party `uses:` reference to a full 40-character upstream commit SHA,
with the exact release version in a comment. Verify the release tag and commit via
the action's official repository before updating. Keep action major versions unless
a compatibility review justifies changing them. The testing workflow retains v4
actions (Node 20 action runtime), Node 22 for Gilligan, and .NET 8 for Skipper;
checkout v4.4.0's safer `pull_request_target` defaults do not affect its existing
`pull_request` trigger.

Gilligan uses npm inside `apps/gilligan-web`, without npm workspaces. Inspect fresh
scanner advisories and `npm explain` before changing dependencies. Prefer updating
the introducing dependency, keep `next` and `eslint-config-next` aligned, regenerate
the lockfile with npm, and validate with `npm ci`. Add overrides only when a parent
upgrade cannot safely resolve a finding, with a documented compatibility rationale.

## Local scanner commands

Run from the repository root with Docker available. These use CI's Semgrep image,
Trivy version (explicitly 0.70.0), scope, severity thresholds,
and failure flags. Restore first so NuGet metadata is included.

```sh
dotnet restore apps/skipper-api/skipper-api.csproj --use-lock-file

docker run --rm -v "$PWD:/src:ro" -w /src \
  semgrep/semgrep:1.179.0@sha256:93963d9295a366f59e4850127b1550400ee7b388f04fe144e4a1f6325d96e01b \
  semgrep scan --config p/default --error --strict --metrics off \
  --exclude '**/node_modules/**' --exclude '**/.next/**' \
  --exclude '**/bin/**' --exclude '**/obj/**' --exclude '**/build/**' \
  --exclude '**/dist/**' --exclude '**/coverage/**' .

# Virtual environment setup: see security/README.md.
SECURITY_PYTHON=/tmp/critter-security-venv/bin/python \
  SECURITY_USE_DOCKER=true ./security/scan.sh

python3 security/report.py trufflehog -- docker run --rm -v "$PWD:/repo:ro" -w /repo \
  ghcr.io/trufflesecurity/trufflehog:3.97.9@sha256:52e67fef4d054ecff5c2ce4b4ae376626d1ef54aa0898b53cac19c25e92e14db \
  git file:///repo --results verified --fail --fail-on-scan-errors --no-update --json
```

Mary Ann explicitly includes development/test dependencies. Standard OpenVEX and
temporary risk metadata are maintained together in
[security/exceptions.json](../security/exceptions.json). See
[Security exceptions](../security/README.md) for approval, expiration, stale-record
cleanup, and local evaluation. JSON collection precedes the final blocking gate;
accepted vulnerabilities remain visible and scanner execution errors fail the job.
Semgrep's `p/default` and the Trivy database change over time, so record versions,
date, exit statuses, and findings when reporting results.

## Historical remediation verification — October 10, 2026 (before exception integration)

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
