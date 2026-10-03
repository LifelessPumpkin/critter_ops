# Repository Security Scanning

CritterOps runs the **Security Scanning** GitHub Actions workflow on every push to `develop`, including pull requests merged into that branch. Maintainers can also run it manually with `workflow_dispatch` from the Actions tab.

The workflow runs three independent, blocking jobs so one scanner failure does not prevent the others from reporting results:

- **Semgrep SAST** scans the monorepo's supported source and configuration files with Semgrep's maintained default ruleset. Generated dependencies, build output, and coverage directories are excluded.
- **Trivy Dependency Scan** scans repository dependency metadata for `HIGH` and `CRITICAL` vulnerabilities. The committed npm lock file is scanned directly; the job runs a .NET restore to create current NuGet lock metadata for `apps/skipper-api/skipper-api.csproj` before scanning. A matching vulnerability fails the job.
- **TruffleHog Secret Scan** checks the complete Git history and current repository contents. Verified secrets fail the job, and scanner errors are also blocking.

The workflow uses read-only repository permissions, requires no paid scanner service, and does not build, deploy, or scan container images. Scanner findings and execution errors are reported in each job's GitHub Actions log. Developers do not need to install these scanners for normal local development.
