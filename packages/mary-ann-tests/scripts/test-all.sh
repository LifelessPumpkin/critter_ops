#!/usr/bin/env sh
set -eu

repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)"

"$repo_root/packages/mary-ann-tests/scripts/test-gilligan.sh"
"$repo_root/packages/mary-ann-tests/scripts/test-skipper.sh"
