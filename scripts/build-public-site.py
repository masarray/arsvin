#!/usr/bin/env python3
"""CLI wrapper for the ARSVIN public-site builder."""

from __future__ import annotations

import argparse
from pathlib import Path

from public_site_builder import build


def main() -> None:
    parser = argparse.ArgumentParser(description="Build the ARSVIN public site and searchable HTML engineering documentation.")
    parser.add_argument("--repo-root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--output", type=Path, default=Path("artifacts/public-site"))
    args = parser.parse_args()
    repo_root = args.repo_root.resolve()
    output = args.output if args.output.is_absolute() else (repo_root / args.output)
    build(repo_root, output.resolve())


if __name__ == "__main__":
    main()