#!/usr/bin/env python3
"""Reject proprietary comparison wording from active public repository content.

The blocked tokens are assembled from character codes so this validator does not
re-publish the terms it is designed to exclude.
"""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PUBLIC_PATHS = (
    ROOT / "README.md",
    ROOT / "CHANGELOG.md",
    ROOT / "docs",
    ROOT / "site",
    ROOT / "samples",
)

BLOCKED = (
    "".join(map(chr, (79, 77, 73, 67, 82, 79, 78))),
    "".join(map(chr, (83, 116, 97, 116, 105, 111, 110, 83, 99, 111, 117, 116))),
    "".join(map(chr, (68, 65, 78, 69, 79))),
)
TEXT_SUFFIXES = {
    ".md", ".txt", ".html", ".htm", ".css", ".js", ".json", ".xml",
    ".yml", ".yaml", ".ps1", ".py", ".cs", ".csproj", ".sln", ".iss",
}


def iter_files(path: Path):
    if path.is_file():
        yield path
        return
    if path.is_dir():
        for candidate in path.rglob("*"):
            if candidate.is_file() and candidate.suffix.lower() in TEXT_SUFFIXES:
                yield candidate


def main() -> int:
    violations: list[str] = []
    for public_path in PUBLIC_PATHS:
        for file_path in iter_files(public_path):
            text = file_path.read_text(encoding="utf-8", errors="replace")
            folded = text.casefold()
            for token in BLOCKED:
                if token.casefold() in folded:
                    relative = file_path.relative_to(ROOT).as_posix()
                    violations.append(f"{relative}: prohibited proprietary comparison term")

    if violations:
        print("Public terminology neutrality validation failed:", file=sys.stderr)
        for violation in sorted(set(violations)):
            print(f"- {violation}", file=sys.stderr)
        return 1

    print("Public terminology neutrality validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
