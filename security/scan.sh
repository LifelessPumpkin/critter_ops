#!/usr/bin/env sh
# Same scan and policy contract as the Mary Ann Trivy job. Native Trivy or Docker.
set -eu
repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
cd "$repo_root"
output_root="${1:-$repo_root/artifacts/security}"
mkdir -p "$output_root"
output_root="$(CDPATH= cd -- "$output_root" && pwd)"
run_dir="$(mktemp -d "$output_root/run.XXXXXX")"
policy="${SECURITY_POLICY:-$repo_root/security/exceptions.json}"
python_bin="${SECURITY_PYTHON:-python3}"
scan_finished=false
on_exit() {
  status=$?
  if [ "$scan_finished" != true ]; then
    "$python_bin" "$repo_root/security/evaluate.py" evaluate \
      --scanner-error 'Policy preparation, restore, or Trivy execution failed; no clean result is available.' \
      --summary "$run_dir/summary.md" --result-output "$run_dir/evaluation.json" || status=1
  fi
  echo "Security evidence: $run_dir"
  exit "$status"
}
trap on_exit EXIT
"$python_bin" "$repo_root/security/evaluate.py" validate --policy "$policy" \
  --vex-output "$run_dir/critterops.openvex.json"
dotnet restore apps/skipper-api/skipper-api.csproj --use-lock-file
set -- fs --scanners vuln --severity HIGH,CRITICAL --include-dev-deps --list-all-pkgs \
  --exit-code 0 --ignore-unfixed=false --ignorefile /dev/null \
  --skip-dirs '**/node_modules,**/.next,**/bin,**/obj,**/build,**/dist,**/coverage' \
  --show-suppressed --format json --output "$run_dir/trivy.json"
if [ -s "$run_dir/critterops.openvex.json" ]; then
  set -- "$@" --vex "$run_dir/critterops.openvex.json"
fi
if [ "${SECURITY_USE_DOCKER:-false}" = true ]; then
  docker run --rm -v "$repo_root:/src:ro" -v "$run_dir:$run_dir" -w /src \
    aquasec/trivy:0.70.0@sha256:be1190afcb28352bfddc4ddeb71470835d16462af68d310f9f4bca710961a41e \
    "$@" .
else
  trivy --version | head -1 | "$python_bin" -c 'import sys; sys.exit(0 if sys.stdin.read().strip() == "Version: 0.70.0" else "Trivy 0.70.0 is required")'
  trivy "$@" .
fi
scan_finished=true
"$python_bin" "$repo_root/security/evaluate.py" evaluate --policy "$policy" \
  --report "$run_dir/trivy.json" --result-output "$run_dir/evaluation.json" \
  --summary "$run_dir/summary.md" \
  --require-target apps/gilligan-web/package-lock.json \
  --require-target apps/skipper-api/packages.lock.json
