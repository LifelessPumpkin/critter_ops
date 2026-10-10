#!/usr/bin/env python3
"""Mary Ann dependency policy: strict OpenVEX validation and fail-closed matching."""
import argparse
import datetime as dt
import json
from pathlib import Path, PurePosixPath
import re
import sys
from urllib.parse import unquote, urlsplit

from jsonschema import Draft202012Validator, FormatChecker

ROOT = Path(__file__).resolve().parents[1]
ID = re.compile(r"(?:CVE-\d{4}-\d{4,}|GHSA-[23456789cfghjmpqrvwx]{4}-[23456789cfghjmpqrvwx]{4}-[23456789cfghjmpqrvwx]{4})\Z")
SEVERITIES = {"HIGH", "CRITICAL"}


class PolicyError(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise PolicyError(message)


def string(value, label):
    require(isinstance(value, str) and bool(value.strip()), f"{label} must be nonempty text")
    require(not any(ord(c) < 32 and c not in "\n\t" for c in value), f"{label} contains control characters")
    return value


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, f"Duplicate JSON key: {key}")
        result[key] = value
    return result


def load(path):
    try:
        return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=unique_object)
    except (OSError, ValueError) as exc:
        raise PolicyError(f"Cannot read valid JSON from {path}: {exc}") from exc


def date(value, label):
    require(isinstance(value, str) and re.fullmatch(r"\d{4}-\d{2}-\d{2}", value), f"{label} must be a UTC YYYY-MM-DD date")
    try:
        return dt.date.fromisoformat(value)
    except ValueError as exc:
        raise PolicyError(f"Invalid {label}: {value}") from exc


def timestamp(value, label, today):
    string(value, label)
    parsed = dt.datetime.fromisoformat(value.replace("Z", "+00:00"))
    require(parsed.utcoffset() == dt.timedelta(0), f"{label} must use UTC")
    require(parsed.date() <= today, f"{label} is future-dated")


def target(value):
    string(value, "target")
    p = PurePosixPath(value)
    require(not p.is_absolute() and ".." not in p.parts and str(p) == value and not re.search(r"[\\*?\[\]]", value), "target must be an exact repository-relative path")
    return value


def purl(value):
    """Small, intentionally strict identity subset: npm/NuGet with exact versions."""
    string(value, "PURL")
    match = re.fullmatch(r"pkg:(npm|nuget)/([^?#]+)@([^@/?#]+)", value)
    require(match is not None, "Use an exact-version npm or NuGet PURL without qualifiers/subpaths")
    kind, name, version = match.groups()
    name = unquote(name)
    require(bool(re.fullmatch(r"(?:@[a-z0-9_.-]+/)?[a-z0-9_.-]+", name)) if kind == "npm" else bool(re.fullmatch(r"[A-Za-z0-9_.-]+", name)), "Invalid package identity")
    require(bool(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9.+_-]*", version)), "Invalid exact package version")
    return kind, name, version


def validate(policy, today):
    require(isinstance(policy, dict) and set(policy) == {"schema_version", "openvex", "exceptions"}, "Invalid policy envelope")
    require(type(policy["schema_version"]) is int and policy["schema_version"] == 1, "Unsupported policy schema")
    if policy["openvex"] is None:
        require(policy["exceptions"] == [], "A policy without VEX must have no exceptions")
        return []
    schema = load(ROOT / "security/schema/openvex-0.2.0.schema.json")
    errors = sorted(Draft202012Validator(schema, format_checker=FormatChecker()).iter_errors(policy["openvex"]), key=lambda e: str(e.path))
    require(not errors, "OpenVEX schema: " + "; ".join(e.message for e in errors))
    vex = policy["openvex"]
    require(vex["@context"] == "https://openvex.dev/ns/v0.2.0", "Unsupported OpenVEX context")
    require(type(vex["version"]) is int and vex["version"] > 0, "OpenVEX version must be a positive integer")
    for field in ("@id", "author", "timestamp"):
        string(vex[field], field)
    require(bool(urlsplit(vex["@id"]).scheme), "OpenVEX document ID must be an IRI")
    for field in ("timestamp", "last_updated"):
        if field in vex:
            timestamp(vex[field], field, today)
    statements = {}
    for statement in vex["statements"]:
        for field in ("timestamp", "last_updated", "action_statement_timestamp"):
            if field in statement:
                timestamp(statement[field], field, today)
        key = string(statement.get("@id"), "statement @id")
        require(bool(urlsplit(key).scheme) and key not in statements, "Duplicate or invalid statement ID")
        vulnerability = statement["vulnerability"]
        aliases = [vulnerability.get("name")] + vulnerability.get("aliases", [])
        require(all(isinstance(v, str) and ID.fullmatch(v) for v in aliases), "Invalid CVE/GHSA identifier")
        require(len(set(aliases)) == len(aliases), "Duplicate advisory aliases")
        require(len(statement.get("products", [])) == 1 and set(statement["products"][0]) == {"@id"}, "Each exception requires exactly one exact package product")
        identity = purl(statement["products"][0]["@id"])
        if statement["status"] == "affected":
            string(statement.get("action_statement"), "affected action_statement")
            require("justification" not in statement, "affected risk acceptance must not claim not_affected justification")
        if statement["status"] == "not_affected":
            string(statement.get("impact_statement"), "not_affected evidence/impact_statement")
        statements[key] = (statement, aliases, identity)
    require(isinstance(policy["exceptions"], list), "exceptions must be a list")
    required = {"statement_id", "decision", "target", "scope", "severity", "reason", "runtime_exposure", "remediation", "reviewed_on", "expires_on", "follow_up"}
    optional = {"dependency_chain", "evidence", "issue"}
    records, seen, referenced = [], set(), set()
    for record in policy["exceptions"]:
        require(isinstance(record, dict) and required <= set(record) <= required | optional, "Missing or unknown exception metadata")
        key = record["statement_id"]
        require(isinstance(key, str) and key in statements and key not in referenced, "Missing or duplicate VEX statement reference")
        referenced.add(key)
        statement, aliases, identity = statements[key]
        require(record["decision"] == "accept_temporarily", "Unsupported risk decision")
        target(record["target"])
        require(record["scope"] in {"development", "production"}, "Unsupported dependency scope")
        require(record["severity"] in SEVERITIES, "Unsupported exception severity")
        for field in ("reason", "runtime_exposure", "follow_up"):
            string(record[field], field)
        remediation = record["remediation"]
        require(isinstance(remediation, dict) and set(remediation) == {"available", "details"} and remediation["available"] is False, "Do not accept vulnerabilities with available remediation")
        string(remediation["details"], "remediation details")
        reviewed = date(record["reviewed_on"], "reviewed_on")
        expires = date(record["expires_on"], "expires_on")
        require(reviewed <= today and reviewed < expires, "Review must not be future-dated; expiry must follow review")
        for field in ("dependency_chain", "evidence"):
            if field in record:
                require(isinstance(record[field], list) and bool(record[field]), f"{field} must be a nonempty list")
                for item in record[field]:
                    string(item, field)
        if "issue" in record:
            string(record["issue"], "issue")
        for alias in aliases:
            duplicate = (record["target"], identity, alias)
            require(duplicate not in seen, "Duplicate/overlapping exception identity")
            seen.add(duplicate)
        records.append({**record, "statement": statement, "aliases": aliases, "identity": identity, "expired": today >= expires})
    require(referenced == set(statements), "Every VEX statement requires matching policy metadata")
    return records


def dependency_paths(packages, package_id):
    """Preserve all discovered introducing paths; stop rather than lose context."""
    parents = {}
    names = {}
    for package in packages:
        if isinstance(package, dict) and isinstance(package.get("ID"), str):
            names[package["ID"]] = package.get("Name", package["ID"])
            for dependency in package.get("DependsOn", []):
                parents.setdefault(dependency, set()).add(package["ID"])
    paths = []

    def walk(current, path):
        require(len(path) < 100 and len(paths) < 100, "Dependency paths too ambiguous/large for automatic acceptance")
        for parent in sorted(parents.get(current, set())):
            require(parent not in path, "Dependency cycle prevents safe exception matching")
            walk(parent, [parent] + path)
        if not parents.get(current):
            paths.append([names.get(item, item) for item in path])

    walk(package_id, [package_id])
    return paths


def findings(report):
    require(isinstance(report, dict) and type(report.get("SchemaVersion")) is int and report["SchemaVersion"] == 2, "Unsupported/missing Trivy report schema")
    require(isinstance(report.get("Trivy"), dict) and report["Trivy"].get("Version") == "0.70.0", "Expected Trivy 0.70.0 output; review parser before upgrading")
    require(report.get("ArtifactType") in {"repository", "filesystem"} and isinstance(report.get("Results"), list), "Incomplete filesystem dependency report")
    result = []
    for section in report["Results"]:
        require(isinstance(section, dict) and isinstance(section.get("Target"), str), "Malformed scan target")
        packages = section.get("Packages", [])
        vulnerabilities = section.get("Vulnerabilities", [])
        modified = section.get("ExperimentalModifiedFindings", [])
        require(all(isinstance(v, list) for v in (packages, vulnerabilities, modified)), "Malformed scanner findings/packages")
        vulnerabilities = list(vulnerabilities)
        for item in modified:
            require(isinstance(item, dict) and item.get("Type") == "vulnerability" and isinstance(item.get("Finding"), dict), "Unknown suppressed finding format")
            # Never trust scanner suppression as risk approval; re-evaluate the original finding.
            vulnerabilities.append(item["Finding"])
        for vuln in vulnerabilities:
            require(isinstance(vuln, dict), "Malformed vulnerability")
            for field in ("VulnerabilityID", "PkgName", "InstalledVersion", "Severity"):
                string(vuln.get(field), field)
            require(vuln["Severity"] in {"UNKNOWN", "LOW", "MEDIUM", "HIGH", "CRITICAL"}, "Unknown scanner severity")
            require(vuln["Severity"] in SEVERITIES, "Unexpected severity outside HIGH,CRITICAL scan contract")
            vendor_ids = vuln.get("VendorIDs", [])
            require(isinstance(vendor_ids, list) and all(isinstance(v, str) for v in vendor_ids), "Malformed advisory aliases")
            identifier = vuln.get("PkgIdentifier", {})
            require(isinstance(identifier, dict), "Malformed package identifier")
            package_purl = identifier.get("PURL")
            candidates = [p for p in packages if isinstance(p, dict) and p.get("ID") == vuln.get("PkgID") and p.get("Name") == vuln["PkgName"] and p.get("Version") == vuln["InstalledVersion"] and p.get("Identifier", {}).get("PURL") == package_purl]
            scope = "unknown"
            if candidates and all(type(p.get("Dev", False)) is bool for p in candidates):
                scope = "development" if all(p.get("Dev", False) for p in candidates) else "production"
            result.append({"target": section["Target"], "type": section.get("Type"), "id": vuln["VulnerabilityID"], "aliases": vendor_ids, "purl": package_purl, "package": vuln["PkgName"], "version": vuln["InstalledVersion"], "severity": vuln["Severity"], "scope": scope, "fixed_version": vuln.get("FixedVersion", ""), "dependency_paths": dependency_paths(packages, vuln.get("PkgID", vuln["PkgName"])), "locations": [loc for p in candidates for loc in p.get("Locations", [])]})
    return result


def evaluate(policy, report, today, required_targets=()):
    records = validate(policy, today)
    detected = findings(report)
    scanned = {r["Target"] for r in report["Results"]}
    require(set(required_targets) <= scanned, "Required dependency target missing from scanner output")
    accepted, blocking, used = [], [], set()
    grouped = {}
    for finding in detected:
        matches = []
        for record in records:
            try:
                identity = purl(finding["purl"])
            except PolicyError:
                continue
            if (record["target"] == finding["target"] and record["identity"] == identity and set(record["aliases"]) & {finding["id"], *finding["aliases"]}):
                # A changed scope/severity still matches the record for reporting, but cannot be accepted.
                matches.append(record)
        require(len(matches) <= 1, "Ambiguous exception match")
        record = matches[0] if matches else None
        reason = "No exact exception"
        allowed = False
        if record:
            used.add(record["statement_id"])
            kind, name, version = record["identity"]
            checks = [(not record["expired"], "Exception expired"), (finding["scope"] == record["scope"], "Dependency scope changed or unknown"), (finding["severity"] == record["severity"], "Severity changed"), (finding["type"] == kind and finding["package"] == name and finding["version"] == version, "Package metadata inconsistent"), (not finding["fixed_version"], "Scanner reports an available patch; remediate/review"), (record["statement"]["status"] in {"affected", "not_affected"}, "VEX status does not authorize temporary acceptance")]
            allowed = all(check for check, _ in checks)
            reason = record["reason"] if allowed else next(message for check, message in checks if not check)
        row = {**finding, "classification": "accepted" if allowed else "blocking", "reason": reason}
        if record:
            row["exception"] = {k: record[k] for k in ("statement_id", "reviewed_on", "expires_on", "follow_up")}
            row["vex_status"] = record["statement"]["status"]
        canonical_id = record["statement"]["vulnerability"]["name"] if record else finding["id"]
        key = (finding["target"], finding["purl"], finding["package"], finding["version"], canonical_id, finding["severity"], finding["scope"], allowed, reason)
        if key in grouped:
            grouped[key]["contexts"].append(finding)
        else:
            row["contexts"] = [finding]
            grouped[key] = row
            (accepted if allowed else blocking).append(row)
    stale = [r["statement_id"] for r in records if r["statement_id"] not in used]
    expired = [r["statement_id"] for r in records if r["expired"]]
    return {"result": "FAIL" if blocking or stale or expired else "PASS", "date_utc": today.isoformat(), "blocking": blocking, "accepted": accepted, "stale": stale, "expired": expired, "errors": []}


def cell(value):
    return str(value).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace("|", "&#124;").replace("\n", " ").replace("\r", " ").replace("`", "&#96;")


def summary(result):
    blocking_count = "unavailable (evaluation error)" if result["errors"] else len(result["blocking"])
    accepted_count = "unavailable (evaluation error)" if result["errors"] else len(result["accepted"])
    lines = ["# Mary Ann Security Report", "", "Dependency gate; Semgrep and TruffleHog enforce their results independently.", "", f"Result: **{result['result']}**", "", f"Blocking findings: {blocking_count}", f"Accepted findings: {accepted_count}", "", "Accepted vulnerabilities remain unresolved security risks."]
    for title, key in (("Accepted Vulnerabilities", "accepted"), ("Blocking Vulnerabilities", "blocking")):
        lines += ["", f"## {title}", "", "| Vulnerability | Package/version | Severity | Scope/target | Reason | Review / expires (UTC) |", "|---|---|---|---|---|---|"]
        for finding in result[key]:
            exception = finding.get("exception", {})
            values = [finding["id"], f"{finding['package']} {finding['version']}", finding["severity"], f"{finding['scope']}: {finding['target']}", finding["reason"], f"{exception.get('reviewed_on', '—')} / {exception.get('expires_on', '—')}"]
            lines.append("| " + " | ".join(cell(v) for v in values) + " |")
        if not result[key]:
            lines.append("None.")
    for title, key in (("Stale exceptions — remove through review", "stale"), ("Expired exceptions — renew or remediate", "expired"), ("Evaluation errors", "errors")):
        if result[key]:
            lines += ["", f"## {title}", ""] + ["- " + cell(item) for item in result[key]]
    return "\n".join(lines) + "\n"


def write(path, content):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("validate", "evaluate"))
    parser.add_argument("--policy", default=str(ROOT / "security/exceptions.json"))
    parser.add_argument("--report")
    parser.add_argument("--scanner-error", help="Fail explicitly when scanner execution did not complete")
    parser.add_argument("--vex-output")
    parser.add_argument("--result-output")
    parser.add_argument("--summary")
    parser.add_argument("--require-target", action="append", default=[])
    args = parser.parse_args(argv)
    today = dt.datetime.now(dt.timezone.utc).date()
    try:
        require(not args.scanner_error, args.scanner_error or "Scanner execution failed")
        policy = load(args.policy)
        validate(policy, today)
        if args.command == "validate":
            if args.vex_output:
                if policy["openvex"] is None:
                    Path(args.vex_output).unlink(missing_ok=True)
                else:
                    write(args.vex_output, json.dumps(policy["openvex"], indent=2) + "\n")
            print("Policy and official OpenVEX schema validation: PASS")
            return 0
        require(args.report is not None, "--report is required")
        result = evaluate(policy, load(args.report), today, args.require_target)
    except (PolicyError, ValueError, TypeError, KeyError, OSError) as exc:
        result = {"result": "FAIL", "blocking": [], "accepted": [], "stale": [], "expired": [], "errors": [str(exc)]}
    text = summary(result)
    try:
        if args.result_output:
            write(args.result_output, json.dumps(result, indent=2) + "\n")
        if args.summary:
            write(args.summary, text)
    except OSError as exc:
        print(f"FAIL: cannot publish evaluation results: {exc}", file=sys.stderr)
        return 1
    print(text)
    return 0 if result["result"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
