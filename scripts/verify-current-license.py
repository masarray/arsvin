#!/usr/bin/env python3
"""Verify ARSVIN current licensing, packaging, and public wording boundaries."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

REQUIRED_FILES = (
    "LICENSE",
    "NOTICE",
    "COMMERCIAL-LICENSE.md",
    "COPYRIGHT.md",
    "TRADEMARK.md",
    "CONTRIBUTOR-LICENSE-AGREEMENT.md",
    "DCO.txt",
    "THIRD_PARTY_NOTICES.md",
    "docs/LICENSING.md",
    "docs/EXTERNAL_IP_AND_PROVENANCE_REVIEW_2026-07-14.md",
    "docs/WORDING_AND_CLAIM_REVIEW_2026-07-14.md",
)


def text(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(condition: bool, message: str, errors: list[str]) -> None:
    if not condition:
        errors.append(message)


def main() -> int:
    errors: list[str] = []

    for relative in REQUIRED_FILES:
        require((ROOT / relative).is_file(), f"Missing required licensing or provenance file: {relative}", errors)

    historical_license = ROOT / "LICENSE-APACHE-2.0"
    require(not historical_license.exists(), "Current branch must not contain LICENSE-APACHE-2.0", errors)

    license_text = text("LICENSE")
    require("GNU GENERAL PUBLIC LICENSE" in license_text, "LICENSE is not the GNU GPL text", errors)
    require("Version 3, 29 June 2007" in license_text, "LICENSE does not identify GNU GPL version 3", errors)
    require("Apache License" not in license_text, "Current LICENSE contains Apache license wording", errors)

    props = text("Directory.Build.props")
    require("<PackageLicenseExpression>GPL-3.0-or-later</PackageLicenseExpression>" in props,
            "Directory.Build.props does not declare GPL-3.0-or-later", errors)
    require("<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>" not in props,
            "Directory.Build.props still declares Apache-2.0", errors)

    commercial = text("COMMERCIAL-LICENSE.md")
    require("not itself a commercial license" in commercial,
            "Commercial notice must state that it is not itself a commercial license", errors)
    require("grants no additional rights" in commercial,
            "Commercial notice must state that it grants no additional rights", errors)

    licensing = text("docs/LICENSING.md")
    require("9440f08b6909ef2dc93dd483cfdcb4e1e86077d0" in licensing,
            "Licensing document does not record the historical boundary commit", errors)
    require("archive/apache-2.0-final" in licensing,
            "Licensing document does not identify the historical archive branch", errors)
    require("current `main`" in licensing and "GPL-3.0-or-later" in licensing,
            "Licensing document does not clearly identify the current GPL branch", errors)

    public_checks = {
        "README.md": (
            "license-GPL--3.0--or--later",
            "current `main` branch and current public release packages are licensed **only**",
            "COMMERCIAL-LICENSE.md",
        ),
        "site/index.html": (
            '"license": "https://spdx.org/licenses/GPL-3.0-or-later.html"',
            "GPL-3.0-or-later",
            "Commercial licensing notice",
        ),
        "NOTICE": ("GPL-3.0-or-later", "archive/apache-2.0-final"),
        "scripts/public_site_builder.py": ("GPL-3.0-or-later",),
        "scripts/publish-release.ps1": ("COMMERCIAL-LICENSE.md", "LICENSE.txt"),
        "installer/ARSVIN.iss": ("COMMERCIAL-LICENSE.md", "GPL License"),
    }
    for relative, required in public_checks.items():
        value = text(relative)
        for marker in required:
            require(marker in value, f"{relative} is missing required marker: {marker}", errors)

    stale_patterns = {
        "README.md": (
            r"license-Apache--2\.0",
            r"focused, Apache-2\.0 engineering suite",
            r"Licensed under the \[Apache License 2\.0",
        ),
        "site/index.html": (
            r'"license"\s*:\s*"https://www\.apache\.org/licenses/LICENSE-2\.0"',
            r"Apache-2\.0 · Windows",
            r"Apache-2\.0 SV Publisher",
        ),
        "NOTICE": (r"^Licensed under the Apache License",),
        "scripts/public_site_builder.py": (r"Apache-2\.0 engineering documentation",),
        "CONTRIBUTING.md": (r"Preserve Apache-2\.0 compatibility",),
    }
    for relative, patterns in stale_patterns.items():
        value = text(relative)
        for pattern in patterns:
            require(re.search(pattern, value, flags=re.IGNORECASE | re.MULTILINE) is None,
                    f"Stale active-license wording in {relative}: {pattern}", errors)

    wording = text("docs/WORDING_AND_CLAIM_REVIEW_2026-07-14.md")
    for marker in ("formal conformance", "functional safety", "IED consumed", "guarded live workflow"):
        require(marker in wording, f"Public claim review is missing boundary: {marker}", errors)

    if errors:
        print("Current-license and public-wording verification failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("Current-license and public-wording verification passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())