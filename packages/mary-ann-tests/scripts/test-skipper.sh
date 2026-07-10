#!/usr/bin/env sh
set -eu

repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)"
test_projects_file="$(mktemp)"

cleanup() {
  rm -f "$test_projects_file"
}
trap cleanup EXIT

find "$repo_root/apps" "$repo_root/packages" \
  \( -name "*Tests*.csproj" -o -name "*.Tests.csproj" \) > "$test_projects_file"

if [ -s "$test_projects_file" ]; then
  while IFS= read -r project; do
    dotnet test "$project"
  done < "$test_projects_file"
else
  dotnet test "$repo_root/apps/skipper-api/skipper-api.csproj"
fi
