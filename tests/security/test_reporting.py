"""Controlled output fixtures, including fake secret strings; no real credentials."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('report', ROOT / 'security/report.py')
report = importlib.util.module_from_spec(spec)
spec.loader.exec_module(report)


class ReportingTests(unittest.TestCase):
    def test_semgrep_clean_findings_and_execution_errors(self):
        self.assertEqual(report.semgrep_result({'results': [], 'errors': []}, 0)['status'], 'PASS')
        self.assertEqual(report.semgrep_result({'results': [{}], 'errors': []}, 1)['blocking'], 1)
        for data, code in [({}, 0), ({'results': [], 'errors': [{}]}, 1), ({'results': [{}]}, 2), ({'results': []}, 1)]:
            self.assertIsNone(report.semgrep_result(data, code)['blocking'])

    def test_verified_secrets_fail_without_raw_values(self):
        raw = json.dumps({'Verified': True, 'Raw': 'FAKE-SENSITIVE-TEST-VALUE'})
        result = report.secret_result(183, [raw])
        self.assertEqual(result, {'status': 'Security Gate Failure', 'blocking': 1})
        self.assertNotIn('FAKE-SENSITIVE', json.dumps(result))
        self.assertEqual(report.secret_result(0, [])['status'], 'PASS')
        for code, lines in [(1, [raw]), (0, [raw]), (183, []), (0, ['bad json'])]:
            self.assertEqual(report.secret_result(code, lines)['status'], 'Scanner Execution Failure')

    def test_secret_wrapper_never_emits_stdout_or_stderr(self):
        with tempfile.TemporaryDirectory() as directory:
            result = subprocess.run([sys.executable, str(ROOT / 'security/report.py'), 'trufflehog', '--', sys.executable, '-c',
                                     "import sys; print('FAKE-SENSITIVE-STDOUT'); print('FAKE-SENSITIVE-STDERR', file=sys.stderr); sys.exit(1)"],
                                    cwd=directory, capture_output=True, text=True)
            self.assertEqual(result.returncode, 1)
            self.assertNotIn('FAKE-SENSITIVE', result.stdout + result.stderr)
            evidence = Path(directory, 'artifacts/security/trufflehog-status.json').read_text()
            self.assertNotIn('FAKE-SENSITIVE', evidence)
            self.assertIn('Scanner Execution Failure', evidence)

    def test_trivy_suppressed_findings_remain_visible_without_classification(self):
        finding = {'VulnerabilityID': 'CVE-2026-12345', 'Severity': 'HIGH'}
        result = report.trivy_reporting_input({'SchemaVersion': 2, 'Results': [
            {'Vulnerabilities': [], 'ExperimentalModifiedFindings': [{'Type': 'vulnerability', 'Finding': finding}]}]})
        self.assertEqual(result['Results'][0]['Vulnerabilities'], [finding])
        self.assertNotIn('classification', finding)
        with self.assertRaises(ValueError):
            report.trivy_reporting_input({'SchemaVersion': 2, 'Results': [{'ExperimentalModifiedFindings': [{'Type': 'unknown'}]}]})

    def test_sarif_missing_or_invalid_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory, 'report.sarif')
            with self.assertRaises(OSError):
                report.validate_sarif(path)
            path.write_text('{"version":"2.1.0","runs":[]}')
            with self.assertRaises(ValueError):
                report.validate_sarif(path)

    def test_cli_reports_missing_semgrep_output_as_error(self):
        with tempfile.TemporaryDirectory() as directory:
            result = subprocess.run([sys.executable, str(ROOT / 'security/report.py'), 'semgrep', '--exit-code', '2', '--report', 'missing.json'],
                                    cwd=directory, capture_output=True, text=True)
            self.assertEqual(result.returncode, 1)
            self.assertIn('Scanner Execution Failure', result.stdout)


class WorkflowTests(unittest.TestCase):
    def test_premerge_triggers_and_no_postmerge_execution(self):
        for name in ('mary-ann-security.yml', 'mary-ann-testing.yml'):
            text = (ROOT / '.github/workflows' / name).read_text()
            trigger = text.split('on:\n', 1)[1].split('\npermissions:', 1)[0]
            self.assertIn('pull_request:\n    branches: [develop, main]', trigger)
            self.assertNotIn('push:', trigger)
            self.assertNotIn('pull_request_target:', trigger)
        self.assertIn('workflow_dispatch:', (ROOT / '.github/workflows/mary-ann-security.yml').read_text())

    def test_aggregate_fails_on_any_failed_skipped_cancelled_job(self):
        import os
        import textwrap
        text = (ROOT / '.github/workflows/mary-ann-security.yml').read_text()
        script = textwrap.dedent(text.split("python3 - <<'PYCODE'\n", 1)[1].split('          PYCODE', 1)[0])
        with tempfile.TemporaryDirectory() as directory:
            Path(directory, 'reports').mkdir()
            Path(directory, 'reports/summary.md').write_text('Blocking: 0\nAccepted: 1\n')
            Path(directory, 'reports/evaluation.json').write_text(json.dumps({'errors': [], 'result': 'PASS', 'blocking': [], 'accepted': [{}]}))
            env = {**os.environ, 'SEMGREP': 'success', 'TRIVY': 'success', 'TRUFFLEHOG': 'success', 'EVIDENCE': 'success',
                   'SEMGREP_STATUS': 'PASS', 'SECRET_STATUS': 'PASS', 'SEMGREP_COUNT': '0', 'SECRET_COUNT': '0',
                   'GITHUB_STEP_SUMMARY': str(Path(directory, 'summary.md'))}
            def run(changes):
                return subprocess.run([sys.executable, '-c', script], cwd=directory, env={**env, **changes}, capture_output=True)
            self.assertEqual(run({}).returncode, 0)
            for scanner in ('SEMGREP', 'TRIVY', 'TRUFFLEHOG', 'EVIDENCE'):
                for status in ('failure', 'skipped', 'cancelled'):
                    self.assertEqual(run({scanner: status}).returncode, 1)
            Path(directory, 'reports/summary.md').unlink()
            self.assertEqual(run({}).returncode, 1)
