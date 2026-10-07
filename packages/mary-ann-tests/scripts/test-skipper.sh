#!/usr/bin/env sh
set -eu
repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)"
if ! command -v dotnet >/dev/null 2>&1; then
  echo "The .NET 8 SDK is required to run Skipper tests." >&2
  exit 1
fi
suite="${1:-all}"
case "$suite" in
  all) suites="Unit Integration" ;;
  unit) suites="Unit" ;;
  integration) suites="Integration" ;;
  *) echo "Usage: $0 [all|unit|integration] [dotnet test options...]" >&2; exit 1 ;;
esac
if [ "$#" -gt 0 ]; then shift; fi
# Explicit projects ensure accidentally missing suites cannot silently pass.
for selected in $suites; do
  project="$repo_root/tests/skipper/$selected/MaryAnn.Skipper.${selected}Tests.csproj"
  if [ ! -f "$project" ]; then
    echo "Missing required Skipper test project: $project" >&2
    exit 1
  fi
done
status=0
for selected in $suites; do
  project="$repo_root/tests/skipper/$selected/MaryAnn.Skipper.${selected}Tests.csproj"
  dotnet test "$project" "$@" || status=1
done
exit "$status"
