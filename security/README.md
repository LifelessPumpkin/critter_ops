# Dependency security exceptions

Mary Ann blocks unapproved High/Critical dependency vulnerabilities. Acceptance is
a temporary risk-management decision, **not remediation**. It requires a reviewed
repository change; local evaluation of a proposed record does not mean a reviewer
has approved it. Semgrep findings and verified secrets cannot use these exceptions.

## One source of truth

Edit **`security/exceptions.json`**. Its `openvex` object is a standard OpenVEX
0.2.0 document, and its `exceptions` list adds CritterOps risk-policy metadata by
referencing each statement's `@id`. Custom policy properties are outside the VEX
document. Mary Ann validates both and exports the unchanged standard document to
`artifacts/security/critterops.openvex.json`. Do not edit that generated artifact.
Keeping the policy and VEX together avoids conflicting ignore lists or duplicate
approval sources. No `.trivyignore` is used.

The official schema is vendored in `schema/openvex-0.2.0.schema.json`, unchanged
from [openvex/spec commit 61b5f885d0f481f48683c93345e49ef1a6e9fdff](https://github.com/openvex/spec/blob/61b5f885d0f481f48683c93345e49ef1a6e9fdff/openvex_json_schema.json),
with its license. Python's small `jsonschema` validator performs schema checking;
the policy adds stricter identity, scope, date, and relationship checks.

[Trivy 0.70.0 supports OpenVEX for filesystem scanning](https://github.com/aquasecurity/trivy/blob/v0.70.0/docs/guide/supply-chain/vex/file.md).
It also supports CSAF; CycloneDX VEX is limited to CycloneDX SBOM scans. We use
OpenVEX and `--vex` with `--show-suppressed`. Trivy suppresses `not_affected` and
`fixed`, whereas `affected` does not suppress findings. Mary Ann re-evaluates
original suppressed findings too, so native suppression cannot bypass policy.

## How to report a vulnerability as temporarily accepted

1. Run the local scan below. Use the report's exact vulnerability ID, aliases,
   package PURL/version, target lockfile, severity, and dependency scope.
2. Verify the advisory, dependency chain, production exposure, and available fixes.
   Upgrade a dependency when a compatible fix exists. Development scope alone is
   insufficient justification: consider malicious repository inputs and CI tools.
3. Add a statement under `openvex.statements`. Give it a unique `urn:` `@id`, the
   CVE/GHSA and verified aliases, and one exact-version npm/NuGet package PURL.
   Use **`affected`** for a vulnerable dependency whose risk you accept; include
   an `action_statement` describing mitigation/follow-up. Do not label acceptance
   `not_affected`. Use `not_affected` only with evidence of non-applicability,
   an official justification and/or impact statement; this policy requires the
   impact evidence text. `fixed` and `under_investigation` never accept an active
   finding in Mary Ann.
4. Add a matching record under `exceptions`. Copy the existing braces record as a
   structural example, then replace its identities and assessment. Required:
   `statement_id`, `decision: "accept_temporarily"`, exact `target`,
   `scope: "development"` or `"production"`, `severity: "HIGH"` or `"CRITICAL"`,
   `reason`, `runtime_exposure`, `remediation` (`available: false`, `details`),
   `reviewed_on`, `expires_on`, and `follow_up`. Add dependency chain, evidence,
   advisory URL, and issue reference when available. These are facts and proposed
   decisions, not an invented reviewer approval.
5. Use UTC `YYYY-MM-DD` dates. Set a short expiry with a specific review action.
   Increment `openvex.version` and update its UTC `timestamp` whenever the document
   changes. Validate and run `make test-security`, then scan again. Check the
   accepted row and that unrelated findings still block.
6. Submit the policy change through normal pull request review. The reviewed
   policy on the branch being scanned supplies approval; JSON cannot independently
   prove human approval. Repository review/branch protection remains essential.

The initial proposed record is **braces 3.0.3, CVE-2026-93687 /
GHSA-vfj7-8cjw-p6xm, High**, scoped to Gilligan's development dependencies. On
October 10, 2026, Trivy detected it, npm confirmed its ESLint dependency chain,
the production dependency listing and current Next file traces omitted it, and
the [official advisory](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm) and npm
registry had no patched release. It remains `affected`. The proposed acceptance
expires **November 9, 2026 at 00:00 UTC** and still requires PR review.

## Matching and lifecycle

Matching requires a verified advisory ID/alias, exact package PURL/version, exact
repository target, matching ecosystem/name/version metadata, severity, and current
development/production scope. Unknown scope or identity blocks. Multiple paths
are retained in JSON; duplicate identical findings share a count but keep their
contexts. To cover another package/version/lockfile, create another reviewed
statement/record rather than broadening a vulnerability-ID-only rule.

Expiration is inclusive: on `expires_on` the risk blocks again. Future assessment
dates and invalid dates fail validation. Review before expiry, recheck patches and
exposure, and renew through PR review with updated dates/evidence. An available
`FixedVersion` in Trivy blocks even an accepted finding; this conservative initial
policy requires remediation/review rather than guessing patch compatibility.

An unmatched exception is **stale** and fails the gate until it is removed through
review. A package upgrade, removal, or changed scanner advisory can make a record
stale. Remove both its statement and policy record. If none remain, use:

```json
{"schema_version": 1, "openvex": null, "exceptions": []}
```

No empty/fictitious VEX document is emitted. Dependabot remains free to propose
updates; exceptions neither pin packages nor alter update groups/schedules.
When an update resolves a finding, remove the stale exception in that update PR
or a follow-up PR. Nothing is deleted or accepted automatically.

## Local commands and reports

From the repository root, with Python 3.10+, .NET 8, and Trivy 0.70.0 or Docker:

```sh
python3 -m venv /tmp/critter-security-venv
/tmp/critter-security-venv/bin/pip install -r security/requirements.txt
export SECURITY_PYTHON=/tmp/critter-security-venv/bin/python

$SECURITY_PYTHON security/evaluate.py validate
make test-security
SECURITY_USE_DOCKER=true ./security/scan.sh
```

Omit `SECURITY_USE_DOCKER` to use native Trivy 0.70.0. The wrapper restores Skipper,
validates/exports VEX, and scans the repository including development dependencies.
It writes a fresh run directory under `artifacts/security/` (ignored by Git).
It exits nonzero for scanner/preparation errors, invalid policy/output, unapproved
High/Critical findings, expired exceptions, stale records, or report-write errors.
No scan error is converted into success. The JSON collection uses scanner
`--exit-code 0` for findings only; the final policy gate enforces failure.

To evaluate the same scan with a candidate policy, without another scan:

```sh
$SECURITY_PYTHON security/evaluate.py evaluate \
  --policy security/exceptions.json --report artifacts/security/run.EXAMPLE/trivy.json \
  --require-target apps/gilligan-web/package-lock.json \
  --require-target apps/skipper-api/packages.lock.json \
  --result-output artifacts/security/evaluation.json \
  --summary artifacts/security/summary.md
```

Use `SECURITY_POLICY=/path/to/candidate.json` for an isolated local policy fixture.
The CI summary and local report come from the same evaluator. **PASS with accepted
findings is not vulnerability-free**. JSON retains paths, locations, matching
decisions, review/expiry dates, and follow-up actions. Scan/preparation errors are
reported as FAIL. CI publishes summaries and evidence even on failure, retaining
dependency evidence for seven days. Semgrep and TruffleHog remain independent
blocking jobs and never consult this policy.

Initial limitations: exact versions only (no version ranges), npm/NuGet acceptance
only, and parser pinned to Trivy 0.70.0/schema 2. Review tests/parser when upgrading
the scanner. PURL qualifiers/subpaths and ambiguous dependency graphs cannot be
accepted. PR approval is enforced by the repository process, not a JSON flag.
