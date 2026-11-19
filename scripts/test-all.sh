#!/usr/bin/env sh
set -eu
repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
status=0
"$repo_root/scripts/test-security.sh" || status=1
"$repo_root/scripts/test-skipper.sh" || status=1
"$repo_root/scripts/test-gilligan.sh" || status=1
"$repo_root/scripts/test-howell-script.sh" || status=1
exit "$status"
