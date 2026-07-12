#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from urllib.parse import unquote, urlsplit

BASE_URL = "https://masarray.github.io/arsvin/"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def local_target(site_root: Path, page: Path, reference: str) -> Path | None:
    reference = reference.strip()
    if not reference or reference.startswith("#"):
        return None
    if re.match(r"^(?:https?:|mailto:|tel:|data:|javascript:)", reference, re.IGNORECASE):
        return None

    parsed = urlsplit(reference)
    path = unquote(parsed.path)
    if not path:
        return None

    if path.startswith("/arsvin/"):
        candidate = site_root / path[len("/arsvin/") :]
    elif path.startswith("/"):
        candidate = site_root / path.lstrip("/")
    else:
        candidate = page.parent / path

    candidate = candidate.resolve()
    try:
        candidate.relative_to(site_root)
    except ValueError as exc:
        raise RuntimeError(f"Local reference escapes the public-site root: {page}: {reference}") from exc

    if candidate.is_dir() or path.endswith("/"):
        candidate = candidate / "index.html"
    return candidate


def validate_html(site_root: Path, page: Path) -> str:
    text = read_text(page)
    relative = page.relative_to(site_root).as_posix()

    require(re.search(r'<meta\s+name=["\']viewport["\']', text, re.IGNORECASE) is not None, f"Missing viewport metadata: {relative}")
    require(re.search(r'<meta\s+name=["\']description["\']', text, re.IGNORECASE) is not None, f"Missing description metadata: {relative}")
    canonical_match = re.search(r'<link\s+rel=["\']canonical["\'][^>]*href=["\']([^"\']+)', text, re.IGNORECASE)
    if canonical_match is None:
        canonical_match = re.search(r'<link\s+href=["\']([^"\']+)["\'][^>]*rel=["\']canonical["\']', text, re.IGNORECASE)
    require(canonical_match is not None, f"Missing canonical URL: {relative}")

    h1_count = len(re.findall(r"<h1\b", text, re.IGNORECASE))
    require(h1_count == 1, f"Each public page must contain exactly one h1; {relative} has {h1_count}.")

    json_ld_blocks = re.findall(
        r'<script\s+type=["\']application/ld\+json["\'][^>]*>(.*?)</script>',
        text,
        re.IGNORECASE | re.DOTALL,
    )
    require(json_ld_blocks, f"No JSON-LD structured data found: {relative}")
    for block in json_ld_blocks:
        json.loads(block)

    references = re.findall(r'(?:src|href)=["\']([^"\']+)["\']', text, re.IGNORECASE)
    missing: list[str] = []
    for reference in references:
        target = local_target(site_root, page, reference)
        if target is not None and not target.is_file():
            missing.append(reference)
    require(not missing, f"Missing local references in {relative}: {', '.join(sorted(set(missing)))}")

    return canonical_match.group(1)


def validate(site_root: Path) -> None:
    site_root = site_root.resolve()
    required = [
        "index.html",
        "styles.css",
        "docs.css",
        "site.webmanifest",
        "sitemap.xml",
        "robots.txt",
        ".nojekyll",
        "docs/index.html",
        "docs/search-index.json",
    ]
    for relative in required:
        require((site_root / relative).is_file(), f"Required public-site file is missing: {relative}")

    landing = read_text(site_root / "index.html")
    landing_patterns = [
        r'<meta\s+property=["\']og:title["\']',
        r'<meta\s+property=["\']og:image["\']',
        r'<meta\s+name=["\']twitter:card["\']',
        r"ARSVIN-Suite-Setup-win-x64\.exe",
        r"ARSVIN-Publisher-win-x64\.exe",
        r"ArSubsv-Subscriber-win-x64\.exe",
        r"SHA256SUMS\.txt",
        r'href=["\']docs/',
    ]
    for pattern in landing_patterns:
        require(re.search(pattern, landing, re.IGNORECASE) is not None, f"Required landing-page content was not found: {pattern}")

    html_pages = sorted(site_root.rglob("*.html"))
    require(len(html_pages) >= 6, f"Expected landing page plus multiple documentation pages; found {len(html_pages)} HTML files.")
    canonicals: dict[str, str] = {}
    for page in html_pages:
        canonical = validate_html(site_root, page)
        relative = page.relative_to(site_root).as_posix()
        require(canonical not in canonicals, f"Duplicate canonical URL in {relative} and {canonicals.get(canonical)}: {canonical}")
        canonicals[canonical] = relative

    docs_pages = [page for page in html_pages if page.relative_to(site_root).as_posix().startswith("docs/")]
    require(len(docs_pages) >= 5, f"Expected at least five generated documentation pages; found {len(docs_pages)}.")

    manifest = json.loads(read_text(site_root / "site.webmanifest"))
    require(bool(manifest.get("name")), "The web manifest name is missing.")
    require(bool(manifest.get("short_name")), "The web manifest short_name is missing.")
    icons = manifest.get("icons") or []
    require(bool(icons), "The web manifest does not declare any icons.")
    for icon in icons:
        icon_path = site_root / str(icon.get("src", "")).lstrip("/")
        require(icon_path.is_file(), f"Web-manifest icon is missing: {icon.get('src')}")

    search_index = json.loads(read_text(site_root / "docs/search-index.json"))
    require(isinstance(search_index, list) and len(search_index) >= 5, "Documentation search index is missing expected entries.")
    for entry in search_index:
        require(bool(entry.get("title")) and bool(entry.get("url")), "Documentation search index contains an incomplete entry.")
        target = site_root / "docs" / str(entry["url"])
        if target.is_dir() or str(entry["url"]).endswith("/"):
            target = target / "index.html"
        require(target.is_file(), f"Documentation search-index target is missing: {entry['url']}")

    sitemap_root = ET.fromstring(read_text(site_root / "sitemap.xml"))
    sitemap_urls = [element.text.strip() for element in sitemap_root.findall("{*}url/{*}loc") if element.text]
    require(BASE_URL in sitemap_urls, f"Sitemap does not contain the product homepage: {BASE_URL}")
    require(BASE_URL + "docs/" in sitemap_urls, "Sitemap does not contain the documentation index.")
    for canonical, relative in canonicals.items():
        require(canonical in sitemap_urls, f"Sitemap does not contain canonical URL for {relative}: {canonical}")

    robots = read_text(site_root / "robots.txt")
    require(
        re.search(r"Sitemap:\s*https://masarray\.github\.io/arsvin/sitemap\.xml", robots) is not None,
        "robots.txt does not reference the public sitemap.",
    )

    print(f"Public site validation passed: {site_root}")
    print(f"HTML pages: {len(html_pages)}")
    print(f"Documentation pages: {len(docs_pages)}")
    print(f"Sitemap URLs: {len(sitemap_urls)}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Validate the staged ARSVIN public site and generated HTML documentation.")
    parser.add_argument("--site-root", type=Path, default=Path(__file__).resolve().parents[1] / "site")
    args = parser.parse_args()
    try:
        validate(args.site_root)
    except Exception as exc:
        print(f"Public site validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1) from exc


if __name__ == "__main__":
    main()
