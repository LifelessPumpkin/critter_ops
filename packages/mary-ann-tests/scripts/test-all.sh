#!/usr/bin/env sh
set -eu
repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)"
status=0
"$repo_root/packages/mary-ann-tests/scripts/test-skipper.sh" || status=1
"$repo_root/packages/mary-ann-tests/scripts/test-gilligan.sh" || status=1
exit "$status"
