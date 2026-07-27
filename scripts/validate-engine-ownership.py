#!/usr/bin/env python3
"""Validate that ARSVIN applications consume ARIEC61850 instead of an embedded engine.

The legacy src/ARSVIN.Engine directory may remain temporarily during migration, but it must
not be referenced by active projects or the solution. Reusable protocol namespaces must not
be reintroduced in application source folders. Application-specific namespaces such as
AR.Iec61850.SvPublisher remain presentation ownership and are allowed.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
ACTIVE_PROJECTS = (
    ROOT / "src" / "ARSVIN" / "ARSVIN.csproj",
    ROOT / "src" / "ARSVIN.Subscriber" / "ARSVIN.Subscriber.csproj",
    ROOT / "tests" / "ARSVIN.Tests" / "ARSVIN.Tests.csproj",
)
SOLUTION = ROOT / "ARSVIN.sln"
LEGACY_ENGINE = ROOT / "src" / "ARSVIN.Engine"
ENGINE_NAMESPACE_PREFIXES = (
    "AR.Iec61850.Asn1",
    "AR.Iec61850.Capture",
    "AR.Iec61850.Diagnostics",
    "AR.Iec61850.Ethernet",
    "AR.Iec61850.Goose",
    "AR.Iec61850.Mms",
    "AR.Iec61850.Reporting",
    "AR.Iec61850.SampledValues",
    "AR.Iec61850.Scl",
    "AR.Iec61850.Transports",
)
NAMESPACE_PATTERN = re.compile(r"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)", re.MULTILINE)


def read(path: Path) -> str:
    if not path.is_file():
        raise RuntimeError(f"Required file is missing: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def main() -> int:
    errors: list[str] = []

    for project in ACTIVE_PROJECTS:
        text = read(project)
        relative = project.relative_to(ROOT)
        if "ARSVIN.Engine" in text:
            errors.append(f"{relative}: active project still references ARSVIN.Engine")
        if "$(ARIEC61850_CORE_PROJECT)" not in text:
            errors.append(f"{relative}: missing ARIEC61850 core ProjectReference")
        if "$(ARIEC61850_NPCAP_PROJECT)" not in text:
            errors.append(f"{relative}: missing ARIEC61850 Npcap ProjectReference")

    solution_text = read(SOLUTION)
    if "ARSVIN.Engine" in solution_text:
        errors.append("ARSVIN.sln: embedded ARSVIN.Engine is still an active solution project")

    application_roots = (ROOT / "src" / "ARSVIN", ROOT / "src" / "ARSVIN.Subscriber")
    for application_root in application_roots:
        for source in application_root.rglob("*.cs"):
            text = source.read_text(encoding="utf-8-sig", errors="replace")
            for match in NAMESPACE_PATTERN.finditer(text):
                namespace = match.group(1)
                if namespace.startswith(ENGINE_NAMESPACE_PREFIXES):
                    errors.append(
                        f"{source.relative_to(ROOT)}: reusable namespace {namespace} belongs in the sibling engine repository"
                    )

    legacy_count = 0
    if LEGACY_ENGINE.is_dir():
        legacy_count = sum(1 for path in LEGACY_ENGINE.rglob("*.cs") if path.is_file())

    report_lines = [
        "ARSVIN engine ownership validation",
        "active engine: sibling masarray/ARIEC61850",
        f"active application projects checked: {len(ACTIVE_PROJECTS)}",
        f"inactive legacy embedded C# files remaining: {legacy_count}",
    ]

    if errors:
        report_lines.append("result: FAILED")
        report_lines.extend(f"ERROR: {error}" for error in errors)
    else:
        report_lines.append("result: PASSED")

    report = "\n".join(report_lines) + "\n"
    print(report, end="")

    artifact = ROOT / "artifacts" / "engine-ownership-report.txt"
    artifact.parent.mkdir(parents=True, exist_ok=True)
    artifact.write_text(report, encoding="utf-8")

    return 1 if errors else 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except RuntimeError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
