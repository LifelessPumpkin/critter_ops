#!/usr/bin/env sh
set -eu

repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)"
app_dir="$repo_root/apps/gilligan-web"

cd "$app_dir"

if [ ! -d "node_modules" ]; then
  echo "Gilligan dependencies are missing. Run npm ci in apps/gilligan-web before testing." >&2
  exit 1
fi

if ! command -v node >/dev/null 2>&1 || ! command -v npm >/dev/null 2>&1; then
  echo "Node.js and npm are required to run Gilligan tests." >&2
  exit 1
fi
if [ ! -x node_modules/.bin/vitest ]; then
  echo "Gilligan test dependencies are incomplete. Run npm ci in apps/gilligan-web." >&2
  exit 1
fi
npm test
