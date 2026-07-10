#!/usr/bin/env sh
set -eu

repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)"
app_dir="$repo_root/apps/gilligan-web"

cd "$app_dir"

if [ ! -d "node_modules" ]; then
  echo "Gilligan dependencies are missing. Run npm ci in apps/gilligan-web before testing." >&2
  exit 1
fi

if command -v npm >/dev/null 2>&1; then
  npm test
elif command -v zsh >/dev/null 2>&1; then
  APP_DIR="$app_dir" zsh -lc 'cd "$APP_DIR" && npm test'
else
  echo "npm is required to run Gilligan tests." >&2
  exit 1
fi
