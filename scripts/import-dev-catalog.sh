#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
profile=demo
if [[ $# -gt 0 && $1 != --* ]]; then profile=$1; shift; fi
case "$profile" in demo|expanded) ;; *) echo 'Unknown profile. Use demo or expanded.' >&2; exit 2 ;; esac
: "${DOTNET_ENVIRONMENT:?Set DOTNET_ENVIRONMENT=Development}"
: "${ConnectionStrings__ProfessorDb:?Set the local Professor connection string}"
: "${Providers__gbif__UserAgent:?Set a CritterOps UserAgent with a real project URL/contact}"
exec dotnet run --project apps/howell-sync -- dev-import --provider gbif --profile "$profile" "$@"
