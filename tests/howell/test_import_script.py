"""Script contract only. PostgreSQL/provider behavior lives in the Howell .NET tests."""
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]

class ImportScriptTests(unittest.TestCase):
    def run_script(self, args=(), configured=True, exit_code=0, verify=False):
        with tempfile.TemporaryDirectory() as directory:
            stub = Path(directory) / 'dotnet'
            stub.write_text('#!/bin/sh\nprintf "%s\\n" "$@"\nexit ' + str(exit_code) + '\n')
            stub.chmod(0o755)
            env = {'PATH': directory + os.pathsep + os.environ['PATH']}
            if configured:
                env.update(DOTNET_ENVIRONMENT='Development', ConnectionStrings__ProfessorDb='local-test',
                           Providers__gbif__UserAgent='CritterOps/1.0 (+https://critterops.test/contact)')
            script = 'verify-dev-catalog.sh' if verify else 'import-dev-catalog.sh'
            return subprocess.run([str(ROOT / 'scripts' / script), *args], env=env, capture_output=True, text=True)

    def test_default_demo_and_explicit_profiles(self):
        for args, name in [((), 'demo'), (('demo',), 'demo'), (('expanded',), 'expanded')]:
            result = self.run_script(args)
            self.assertEqual(result.returncode, 0)
            self.assertIn('--profile\n' + name, result.stdout)
            self.assertIn('dev-import', result.stdout)

    def test_dry_run_retry_and_exit_codes(self):
        result = self.run_script(('expanded', '--dry-run', '--retry-run', '42'))
        self.assertIn('--dry-run\n--retry-run\n42', result.stdout)
        for code in (1, 2, 3, 4, 5, 130):
            self.assertEqual(self.run_script(exit_code=code).returncode, code)

    def test_missing_configuration_and_unknown_profile(self):
        self.assertNotEqual(self.run_script(configured=False).returncode, 0)
        self.assertEqual(self.run_script(('unknown',)).returncode, 2)

    def test_verification_profile(self):
        result = self.run_script(('expanded',), verify=True)
        self.assertEqual(result.returncode, 0)
        self.assertIn('verify-dev\n--provider\ngbif\n--profile\nexpanded', result.stdout)

if __name__ == '__main__':
    unittest.main()
