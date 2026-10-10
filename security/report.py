#!/usr/bin/env python3
"""Safe scanner status reporting; dependency classification stays in evaluate.py."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile


def semgrep_result(report, exit_code):
    if (exit_code not in (0, 1) or not isinstance(report, dict)
            or not isinstance(report.get('results'), list) or report.get('errors')):
        return {'status': 'Scanner Execution Failure', 'blocking': None}
    count = len(report['results'])
    if (exit_code == 1) != bool(count):
        return {'status': 'Scanner Execution Failure', 'blocking': None}
    return {'status': 'Security Gate Failure' if count else 'PASS', 'blocking': count}


def secret_result(exit_code, lines):
    try:
        records = [json.loads(line) for line in lines if line.strip()]
        if any(not isinstance(r, dict) or r.get('Verified') is not True for r in records):
            raise ValueError('Invalid verified output')
        count = len(records)
        if exit_code not in (0, 183) or (exit_code == 183) != bool(count):
            raise ValueError('Scanner error')
        return {'status': 'Security Gate Failure' if count else 'PASS', 'blocking': count}
    except (ValueError, TypeError):
        return {'status': 'Scanner Execution Failure', 'blocking': None}


def validate_sarif(path):
    report = json.loads(Path(path).read_text())
    if report.get('version') != '2.1.0' or not report.get('runs'):
        raise ValueError('Invalid SARIF envelope')
    for run in report['runs']:
        if not run.get('tool', {}).get('driver', {}).get('name') or not isinstance(run.get('results'), list):
            raise ValueError('Missing SARIF scanner/results')
        for result in run['results']:
            if not result.get('ruleId') or not result.get('message'):
                raise ValueError('Missing SARIF finding identity/message')
    return report


def trivy_reporting_input(report):
    """Restore VEX-suppressed rows for visibility, without classifying any risks."""
    if report.get('SchemaVersion') != 2 or not isinstance(report.get('Results'), list):
        raise ValueError('Invalid Trivy reporting input')
    for section in report['Results']:
        for item in section.get('ExperimentalModifiedFindings', []):
            if item.get('Type') != 'vulnerability' or not isinstance(item.get('Finding'), dict):
                raise ValueError('Unknown suppressed finding')
            section.setdefault('Vulnerabilities', []).append(item['Finding'])
        section.pop('ExperimentalModifiedFindings', None)
    return report


def publish(scanner, result):
    text = (f'## {scanner}\n\nResult: **{result["status"]}**\n\n'
            f'Blocking findings: {result["blocking"] if result["blocking"] is not None else "unavailable (scanner execution error)"}\n')
    directory = Path('artifacts/security')
    directory.mkdir(parents=True, exist_ok=True)
    (directory / f'{scanner.lower()}-status.json').write_text(json.dumps(result) + '\n')
    summary = os.environ.get('GITHUB_STEP_SUMMARY')
    if summary:
        with open(summary, 'a') as stream:
            stream.write(text)
    output = os.environ.get('GITHUB_OUTPUT')
    if output:
        with open(output, 'a') as stream:
            stream.write(f'status={result["status"]}\nblocking={result["blocking"] if result["blocking"] is not None else "unknown"}\n')
    print(text)
    return 0 if result['status'] == 'PASS' else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('scanner', choices=['semgrep', 'trufflehog', 'sarif', 'trivy-report'])
    parser.add_argument('--exit-code', type=int)
    parser.add_argument('--report')
    parser.add_argument('--output')
    argv = sys.argv[1:]
    separator = argv.index('--') if '--' in argv else len(argv)
    command = argv[separator + 1:]
    args = parser.parse_args(argv[:separator])
    if args.scanner == 'trivy-report':
        data = trivy_reporting_input(json.loads(Path(args.report).read_text()))
        Path(args.output).write_text(json.dumps(data))
        return 0
    if args.scanner == 'sarif':
        validate_sarif(args.report)
        return 0
    if args.scanner == 'semgrep':
        try:
            result = semgrep_result(json.loads(Path(args.report).read_text()), args.exit_code)
        except (OSError, ValueError):
            result = {'status': 'Scanner Execution Failure', 'blocking': None}
        return publish('Semgrep', result)
    # Keep all stdout/stderr outside the workspace and delete on every exit.
    # Never print scanner diagnostics: they can contain secret values.
    with tempfile.TemporaryFile(mode='w+') as stdout, tempfile.TemporaryFile(mode='w+') as stderr:
        try:
            code = subprocess.run(command, stdout=stdout, stderr=stderr, check=False).returncode
            stdout.seek(0)
            result = secret_result(code, stdout)
        except OSError:
            result = {'status': 'Scanner Execution Failure', 'blocking': None}
    return publish('TruffleHog', result)


if __name__ == '__main__':
    sys.exit(main())
