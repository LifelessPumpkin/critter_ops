#!/usr/bin/env sh
set -eu
repo_root="$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)"
"${SECURITY_PYTHON:-python3}" -m unittest discover -s "$repo_root/tests/security" -p 'test_*.py'
