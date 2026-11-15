#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
profile=${1:-demo}
case "$profile" in demo|expanded) ;; *) echo 'Unknown profile. Use demo or expanded.' >&2; exit 2 ;; esac
if [[ $# -gt 1 ]]; then echo 'Usage: scripts/verify-dev-catalog.sh [demo|expanded]' >&2; exit 2; fi
: "${DOTNET_ENVIRONMENT:?Set DOTNET_ENVIRONMENT=Development}"
: "${ConnectionStrings__ProfessorDb:?Set the local Professor connection string}"
exec dotnet run --project apps/howell-sync -- verify-dev --provider gbif --profile "$profile"
