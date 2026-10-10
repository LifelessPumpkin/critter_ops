"""Synthetic findings only; never add vulnerable production dependencies."""
import copy
import datetime as dt
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("security_policy", ROOT / "security/evaluate.py")
policy_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(policy_module)
TODAY = dt.date(2026, 10, 10)


def fixture():
    package = {"ID": "braces@3.0.3", "Name": "braces", "Version": "3.0.3", "Dev": True, "Identifier": {"PURL": "pkg:npm/braces@3.0.3"}, "Locations": [{"StartLine": 10}]}
    finding = {"VulnerabilityID": "CVE-2026-93687", "VendorIDs": ["GHSA-vfj7-8cjw-p6xm"], "PkgID": package["ID"], "PkgName": "braces", "InstalledVersion": "3.0.3", "PkgIdentifier": package["Identifier"], "Severity": "HIGH"}
    return {"SchemaVersion": 2, "Trivy": {"Version": "0.70.0"}, "ArtifactType": "repository", "Results": [{"Target": "apps/gilligan-web/package-lock.json", "Class": "lang-pkgs", "Type": "npm", "Packages": [package], "Vulnerabilities": [finding]}]}


def policy_fixture():
    # Independent of live exceptions: removing/resolving braces must not break tests.
    statement_id = "urn:critterops:test:braces"
    return {
        "schema_version": 1,
        "openvex": {
            "@context": "https://openvex.dev/ns/v0.2.0",
            "@id": "urn:critterops:test:vex",
            "author": "Synthetic test fixture",
            "timestamp": "2026-10-10T00:00:00Z",
            "version": 1,
            "statements": [{
                "@id": statement_id,
                "vulnerability": {"name": "CVE-2026-93687", "aliases": ["GHSA-vfj7-8cjw-p6xm"]},
                "products": [{"@id": "pkg:npm/braces@3.0.3"}],
                "status": "affected",
                "action_statement": "Synthetic mitigation and follow-up for tests only.",
            }],
        },
        "exceptions": [{
            "statement_id": statement_id,
            "decision": "accept_temporarily",
            "target": "apps/gilligan-web/package-lock.json",
            "scope": "development",
            "severity": "HIGH",
            "reason": "Synthetic test-only acceptance reason.",
            "runtime_exposure": "Synthetic fixture, no deployed component.",
            "remediation": {"available": False, "details": "Synthetic fixture assumes no patch."},
            "reviewed_on": "2026-10-10",
            "expires_on": "2026-11-09",
            "follow_up": "Review this synthetic fixture before expiry.",
        }],
    }


class PolicyTests(unittest.TestCase):
    def setUp(self):
        self.policy = policy_fixture()
        self.report = fixture()

    def run_policy(self, today=TODAY):
        return policy_module.evaluate(self.policy, self.report, today)

    def clear_policy(self):
        self.policy["exceptions"] = []
        self.policy["openvex"] = None

    def finding(self):
        return self.report["Results"][0]["Vulnerabilities"][0]

    def test_valid_acceptance_is_visible(self):
        result = self.run_policy()
        self.assertEqual((result["result"], len(result["accepted"]), len(result["blocking"])), ("PASS", 1, 0))
        self.assertIn("CVE-2026-93687", policy_module.summary(result))
        self.assertIn(self.policy["exceptions"][0]["reason"], policy_module.summary(result))

    def test_no_or_removed_exception_blocks_high_and_critical(self):
        self.clear_policy()
        for severity in ("HIGH", "CRITICAL"):
            self.finding()["Severity"] = severity
            self.assertEqual(self.run_policy()["result"], "FAIL")

    def test_new_critical_alongside_accepted_high(self):
        other = copy.deepcopy(self.finding())
        other.update(VulnerabilityID="CVE-2026-12345", VendorIDs=[], Severity="CRITICAL")
        self.report["Results"][0]["Vulnerabilities"].append(other)
        result = self.run_policy()
        self.assertEqual((result["result"], len(result["accepted"]), len(result["blocking"])), ("FAIL", 1, 1))

    def test_wrong_package_version_target_and_ecosystem_block(self):
        for field, value in (("PkgName", "other"), ("InstalledVersion", "3.0.2")):
            report = copy.deepcopy(self.report)
            report["Results"][0]["Vulnerabilities"][0][field] = value
            self.assertEqual(policy_module.evaluate(self.policy, report, TODAY)["result"], "FAIL")
        for target in ("apps/other/package-lock.json", "apps/gilligan-web/other-lock.json"):
            report = copy.deepcopy(self.report)
            report["Results"][0]["Target"] = target
            self.assertEqual(policy_module.evaluate(self.policy, report, TODAY)["result"], "FAIL")
        self.finding()["PkgIdentifier"] = {"PURL": "pkg:nuget/braces@3.0.3"}
        self.assertEqual(self.run_policy()["result"], "FAIL")

    def test_expired_at_start_of_expiry_date(self):
        result = self.run_policy(dt.date(2026, 11, 9))
        self.assertEqual(result["result"], "FAIL")
        self.assertEqual(len(result["expired"]), 1)
        self.assertIn("Exception expired", result["blocking"][0]["reason"])

    def test_stale_exception_requires_cleanup(self):
        self.report["Results"][0]["Vulnerabilities"] = []
        result = self.run_policy()
        self.assertEqual((result["result"], len(result["stale"]), len(result["accepted"])), ("FAIL", 1, 0))

    def test_production_or_unknown_scope_blocks(self):
        self.report["Results"][0]["Packages"][0]["Dev"] = False
        self.assertEqual(self.run_policy()["result"], "FAIL")
        self.report["Results"][0]["Packages"] = []
        self.assertEqual(self.run_policy()["result"], "FAIL")

    def test_available_patch_blocks_even_with_exception(self):
        self.finding()["FixedVersion"] = "3.0.4"
        result = self.run_policy()
        self.assertEqual(result["result"], "FAIL")
        self.assertIn("available patch", result["blocking"][0]["reason"])

    def test_severity_change_blocks(self):
        self.finding()["Severity"] = "CRITICAL"
        self.assertEqual(self.run_policy()["result"], "FAIL")

    def test_advisory_alias_matches(self):
        self.finding()["VulnerabilityID"] = "GHSA-vfj7-8cjw-p6xm"
        self.finding()["VendorIDs"] = []
        self.assertEqual(self.run_policy()["result"], "PASS")

    def test_duplicates_preserve_context_and_dependency_paths(self):
        section = self.report["Results"][0]
        section["Vulnerabilities"].append(copy.deepcopy(self.finding()))
        section["Packages"] += [{"ID": "lint-a", "Name": "lint-a", "DependsOn": ["braces@3.0.3"]}, {"ID": "lint-b", "Name": "lint-b", "DependsOn": ["braces@3.0.3"]}]
        result = self.run_policy()
        self.assertEqual(len(result["accepted"]), 1)
        self.assertEqual(len(result["accepted"][0]["contexts"]), 2)
        self.assertEqual(len(result["accepted"][0]["dependency_paths"]), 2)

    def test_suppressed_findings_are_re_evaluated(self):
        section = self.report["Results"][0]
        section["ExperimentalModifiedFindings"] = [{"Type": "vulnerability", "Status": "ignored", "Finding": self.finding()}]
        section["Vulnerabilities"] = []
        self.clear_policy()
        self.assertEqual(self.run_policy()["result"], "FAIL")

    def test_investigation_and_fixed_do_not_accept_active_risk(self):
        for status in ("under_investigation", "fixed"):
            self.policy["openvex"]["statements"][0]["status"] = status
            self.assertEqual(self.run_policy()["result"], "FAIL")

    def test_schema_and_required_metadata_errors(self):
        original = copy.deepcopy(self.policy)
        for field in self.policy["exceptions"][0]:
            if field in {"dependency_chain", "evidence"}:
                continue
            self.policy = copy.deepcopy(original)
            del self.policy["exceptions"][0][field]
            with self.assertRaises(policy_module.PolicyError):
                self.run_policy()
        self.policy = copy.deepcopy(original)
        self.policy["openvex"]["statements"][0]["status"] = "accepted"
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()

    def test_dates_ids_products_and_duplicates_rejected(self):
        original = copy.deepcopy(self.policy)
        for field, value in (("reviewed_on", "2026-10-11"), ("reviewed_on", "2026-02-30"), ("expires_on", "2026-10-09"), ("reason", ""), ("target", "**/package-lock.json")):
            self.policy = copy.deepcopy(original)
            self.policy["exceptions"][0][field] = value
            with self.assertRaises(policy_module.PolicyError):
                self.run_policy()
        self.policy = copy.deepcopy(original)
        self.policy["openvex"]["statements"][0]["vulnerability"]["name"] = "CVE-only"
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()
        self.policy = copy.deepcopy(original)
        self.policy["openvex"]["statements"][0]["products"][0]["@id"] = "pkg:npm/braces"
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()
        self.policy = copy.deepcopy(original)
        self.policy["exceptions"].append(copy.deepcopy(self.policy["exceptions"][0]))
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()

    def test_unreferenced_or_missing_vex_statements_rejected(self):
        self.policy["exceptions"] = []
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()
        self.policy["openvex"] = None
        self.assertEqual(self.run_policy()["result"], "FAIL")

    def test_not_affected_requires_real_impact_evidence(self):
        statement = self.policy["openvex"]["statements"][0]
        statement.update(status="not_affected", justification="vulnerable_code_not_in_execute_path")
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()
        statement["impact_statement"] = "Synthetic fixture evidence only: affected code cannot execute."
        self.assertEqual(self.run_policy()["result"], "PASS")

    def test_unknown_report_or_missing_required_target_fails(self):
        for report in ({}, {**self.report, "SchemaVersion": 3}, {**self.report, "Results": {}}, {**self.report, "Trivy": {"Version": "future"}}):
            with self.assertRaises(policy_module.PolicyError):
                policy_module.evaluate(self.policy, report, TODAY)
        with self.assertRaises(policy_module.PolicyError):
            policy_module.evaluate(self.policy, self.report, TODAY, ["apps/skipper-api/packages.lock.json"])

    def test_bad_suppressed_format_and_dependency_cycles_fail(self):
        self.report["Results"][0]["ExperimentalModifiedFindings"] = [{"Type": "unknown"}]
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()
        self.report["Results"][0]["ExperimentalModifiedFindings"] = []
        self.report["Results"][0]["Packages"][0]["DependsOn"] = ["braces@3.0.3"]
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()

    def test_summary_escapes_scanner_content(self):
        self.clear_policy()
        self.finding()["PkgName"] = "<script>|injection"
        text = policy_module.summary(self.run_policy())
        self.assertNotIn("<script>", text)
        self.assertIn("&lt;script&gt;&#124;injection", text)

    def test_same_id_other_package_and_multiple_versions(self):
        section = self.report["Results"][0]
        for name, version in (("unrelated", "3.0.3"), ("braces", "3.0.2")):
            package = {"ID": f"{name}@{version}", "Name": name, "Version": version, "Dev": True, "Identifier": {"PURL": f"pkg:npm/{name}@{version}"}}
            other = {**copy.deepcopy(self.finding()), "PkgID": package["ID"], "PkgName": name, "InstalledVersion": version, "PkgIdentifier": package["Identifier"]}
            section["Packages"].append(package)
            section["Vulnerabilities"].append(other)
        result = self.run_policy()
        self.assertEqual((len(result["accepted"]), len(result["blocking"]), result["result"]), (1, 2, "FAIL"))

    def test_empty_policy_and_clean_report_pass(self):
        self.clear_policy()
        self.report["Results"][0]["Vulnerabilities"] = []
        self.assertEqual(self.run_policy()["result"], "PASS")

    def test_overlap_through_aliases_is_invalid(self):
        statement = copy.deepcopy(self.policy["openvex"]["statements"][0])
        statement["@id"] += "-duplicate"
        self.policy["openvex"]["statements"].append(statement)
        record = copy.deepcopy(self.policy["exceptions"][0])
        record["statement_id"] = statement["@id"]
        self.policy["exceptions"].append(record)
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()

    def test_statement_dates_and_custom_openvex_fields_fail(self):
        statement = self.policy["openvex"]["statements"][0]
        statement["timestamp"] = "2026-10-11T00:00:00Z"
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()
        del statement["timestamp"]
        statement["accepted"] = True
        with self.assertRaises(policy_module.PolicyError):
            self.run_policy()

    def test_unparseable_scanner_output_is_not_clean(self):
        with tempfile.TemporaryDirectory() as folder:
            directory = Path(folder)
            bad = directory / "report.json"
            bad.write_text("{incomplete scanner output")
            command = [sys.executable, str(ROOT / "security/evaluate.py"), "evaluate", "--report", str(bad), "--summary", str(directory / "summary.md")]
            self.assertEqual(subprocess.run(command, capture_output=True).returncode, 1)
            self.assertIn("unavailable (evaluation error)", (directory / "summary.md").read_text())

    def test_cli_failures_and_summary_write_failure(self):
        with tempfile.TemporaryDirectory() as folder:
            directory = Path(folder)
            bad = directory / "bad.json"
            for content in ("", "{", '{"schema_version":1,"schema_version":1}'):
                bad.write_text(content)
                command = [sys.executable, str(ROOT / "security/evaluate.py"), "evaluate", "--policy", str(bad), "--report", str(bad), "--summary", str(directory / "summary.md")]
                self.assertEqual(subprocess.run(command, capture_output=True).returncode, 1)
                self.assertIn("FAIL", (directory / "summary.md").read_text())
            command = [sys.executable, str(ROOT / "security/evaluate.py"), "evaluate", "--scanner-error", "controlled scanner failure", "--summary", str(directory)]
            self.assertEqual(subprocess.run(command, capture_output=True).returncode, 1)
            command = [sys.executable, str(ROOT / "security/evaluate.py"), "evaluate", "--policy", str(directory / "missing.json"), "--report", str(bad)]
            self.assertEqual(subprocess.run(command, capture_output=True).returncode, 1)


if __name__ == "__main__":
    unittest.main()
