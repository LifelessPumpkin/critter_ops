#!/usr/bin/env sh
set -eu
repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)"
"${HOWELL_TEST_PYTHON:-python3}" -m unittest discover -s "$repo_root/tests/howell" -p 'test_*.py'
