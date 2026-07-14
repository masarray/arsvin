#!/usr/bin/env python3
"""Build the ARSVIN product site and searchable HTML documentation."""

from __future__ import annotations

import html
import json
import re
import shutil
from dataclasses import dataclass
from pathlib import Path
from urllib.parse import urlsplit

BASE_URL = "https://masarray.github.io/arsvin/"
REPO_URL = "https://github.com/masarray/arsvin"


@dataclass(frozen=True)
class DocPage:
    source: Path
    slug: str
    title: str
    description: str


def plain(text: str) -> str:
    text = re.sub(r"`([^`]+)`", r"\1", text)
    text = re.sub(r"!\[([^\]]*)\]\([^)]*\)", r"\1", text)
    text = re.sub(r"\[([^\]]+)\]\([^)]*\)", r"\1", text)
    text = re.sub(r"[*_~>#]", "", text)
    return re.sub(r"\s+", " ", text).strip()


def slugify(text: str) -> str:
    value = re.sub(r"[^a-z0-9]+", "-", plain(text).lower()).strip("-")
    return value or "section"


def metadata(path: Path) -> tuple[str, str]:
    lines = path.read_text(encoding="utf-8").splitlines()
    title = path.stem.replace("-", " ").title()
    start = 0
    for index, line in enumerate(lines):
        match = re.match(r"^#\s+(.+?)\s*$", line)
        if match:
            title = plain(match.group(1))
            start = index + 1
            break

    paragraph: list[str] = []
    for line in lines[start:]:
        value = line.strip()
        if not value:
            if paragraph:
                break
            continue
        if value.startswith(("#", "- ", "* ", ">", "```", "|")):
            if paragraph:
                break
            continue
        paragraph.append(value)

    description = plain(" ".join(paragraph)) or f"ARSVIN engineering documentation for {title}."
    if len(description) > 158:
        description = description[:155].rsplit(" ", 1)[0] + "..."
    return title, description


def rewrite_link(target: str, current_slug: str, docs: dict[str, DocPage]) -> str:
    target = html.unescape(target.strip())
    if not target or target.startswith("#"):
        return target
    if re.match(r"^(?:https?:|mailto:|tel:|data:)", target, re.IGNORECASE):
        return target

    parsed = urlsplit(target)
    path = parsed.path.replace("\\", "/")
    fragment = f"#{parsed.fragment}" if parsed.fragment else ""

    if path.endswith(".md"):
        normalized = path
        while normalized.startswith("./"):
            normalized = normalized[2:]
        if normalized.startswith("../"):
            return f"{REPO_URL}/blob/main/{normalized[3:]}{fragment}"
        name = Path(normalized).name
        page = docs.get(name)
        if page:
            if current_slug == "index":
                base = "./" if page.slug == "index" else f"{page.slug}/"
            else:
                base = "../" if page.slug == "index" else f"../{page.slug}/"
            return base + fragment
        return f"{REPO_URL}/blob/main/docs/{normalized}{fragment}"

    if path.startswith("samples/") or path.startswith("src/"):
        return f"{REPO_URL}/tree/main/{path}{fragment}"
    return target


def inline(text: str, current_slug: str, docs: dict[str, DocPage]) -> str:
    code: list[str] = []

    def protect(match: re.Match[str]) -> str:
        token = f"@@CODE{len(code)}@@"
        code.append(f"<code>{html.escape(match.group(1), quote=False)}</code>")
        return token

    text = re.sub(r"`([^`]+)`", protect, text)
    text = html.escape(text, quote=False)

    def image(match: re.Match[str]) -> str:
        alt = html.escape(match.group(1), quote=True)
        src = html.escape(rewrite_link(html.unescape(match.group(2)), current_slug, docs), quote=True)
        return f'<img src="{src}" alt="{alt}" loading="lazy" />'

    def link(match: re.Match[str]) -> str:
        label = match.group(1)
        href = rewrite_link(html.unescape(match.group(2)), current_slug, docs)
        escaped = html.escape(href, quote=True)
        external = bool(re.match(r"^https?://", href, re.IGNORECASE))
        attrs = ' target="_blank" rel="noopener noreferrer"' if external else ""
        return f'<a href="{escaped}"{attrs}>{label}</a>'

    text = re.sub(r"!\[([^\]]*)\]\(([^)]+)\)", image, text)
    text = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", link, text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"__([^_]+)__", r"<strong>\1</strong>", text)
    text = re.sub(r"(?<!\*)\*([^*]+)\*(?!\*)", r"<em>\1</em>", text)

    for index, value in enumerate(code):
        text = text.replace(f"@@CODE{index}@@", value)
    return text


def table_separator(line: str) -> bool:
    cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
    return bool(cells) and all(re.fullmatch(r":?-{3,}:?", cell or "") for cell in cells)


def markdown_to_html(markdown: str, current_slug: str, docs: dict[str, DocPage]) -> str:
    lines = markdown.splitlines()
    output: list[str] = []
    used_ids: dict[str, int] = {}
    first_h1 = False
    index = 0

    def anchor(text: str) -> str:
        base = slugify(text)
        count = used_ids.get(base, 0)
        used_ids[base] = count + 1
        return base if count == 0 else f"{base}-{count + 1}"

    while index < len(lines):
        value = lines[index].strip()
        if not value:
            index += 1
            continue

        fence = re.match(r"^```\s*([A-Za-z0-9_+.-]*)\s*$", value)
        if fence:
            language = fence.group(1)
            block: list[str] = []
            index += 1
            while index < len(lines) and not re.match(r"^```\s*$", lines[index].strip()):
                block.append(lines[index])
                index += 1
            index += 1 if index < len(lines) else 0
            attr = f' class="language-{html.escape(language, quote=True)}"' if language else ""
            output.append(f"<pre><code{attr}>{html.escape(chr(10).join(block), quote=False)}</code></pre>")
            continue

        heading = re.match(r"^(#{1,6})\s+(.+?)\s*#*\s*$", value)
        if heading:
            level = len(heading.group(1))
            text = heading.group(2)
            if level == 1:
                if first_h1:
                    level = 2
                else:
                    first_h1 = True
            output.append(f'<h{level} id="{anchor(text)}">{inline(text, current_slug, docs)}</h{level}>')
            index += 1
            continue

        if index + 1 < len(lines) and "|" in value and table_separator(lines[index + 1]):
            headers = [cell.strip() for cell in value.strip("|").split("|")]
            index += 2
            rows: list[list[str]] = []
            while index < len(lines):
                candidate = lines[index].strip()
                if not candidate or "|" not in candidate:
                    break
                rows.append([cell.strip() for cell in candidate.strip("|").split("|")])
                index += 1
            output.append('<div class="table-wrap"><table><thead><tr>')
            output.extend(f"<th>{inline(cell, current_slug, docs)}</th>" for cell in headers)
            output.append("</tr></thead><tbody>")
            for row in rows:
                padded = row + [""] * max(0, len(headers) - len(row))
                output.append("<tr>" + "".join(f"<td>{inline(cell, current_slug, docs)}</td>" for cell in padded[: len(headers)]) + "</tr>")
            output.append("</tbody></table></div>")
            continue

        unordered = re.match(r"^[-*+]\s+(.+)$", value)
        ordered = re.match(r"^\d+[.)]\s+(.+)$", value)
        if unordered or ordered:
            tag = "ul" if unordered else "ol"
            output.append(f"<{tag}>")
            while index < len(lines):
                candidate = lines[index].strip()
                match = re.match(r"^[-*+]\s+(.+)$", candidate) if tag == "ul" else re.match(r"^\d+[.)]\s+(.+)$", candidate)
                if not match:
                    break
                output.append(f"<li>{inline(match.group(1), current_slug, docs)}</li>")
                index += 1
            output.append(f"</{tag}>")
            continue

        if value.startswith(">"):
            quoted: list[str] = []
            while index < len(lines) and lines[index].strip().startswith(">"):
                quoted.append(lines[index].strip()[1:].lstrip())
                index += 1
            output.append(f"<blockquote><p>{inline(' '.join(quoted), current_slug, docs)}</p></blockquote>")
            continue

        paragraph: list[str] = []
        while index < len(lines):
            candidate = lines[index].strip()
            if not candidate:
                break
            if re.match(r"^(?:#{1,6}\s|```|[-*+]\s+|\d+[.)]\s+|>)", candidate):
                break
            if index + 1 < len(lines) and "|" in candidate and table_separator(lines[index + 1]):
                break
            paragraph.append(candidate)
            index += 1
        if paragraph:
            output.append(f"<p>{inline(' '.join(paragraph), current_slug, docs)}</p>")
        else:
            index += 1

    if not first_h1:
        output.insert(0, '<h1 id="documentation">Documentation</h1>')
    return "\n".join(output)


def render(page: DocPage, pages: list[DocPage], docs: dict[str, DocPage]) -> str:
    is_index = page.slug == "index"
    prefix = "../" if is_index else "../../"
    canonical = f"{BASE_URL}docs/" if is_index else f"{BASE_URL}docs/{page.slug}/"
    source_url = f"{REPO_URL}/blob/main/docs/{page.source.name}"
    body = markdown_to_html(page.source.read_text(encoding="utf-8"), page.slug, docs)
    nav_items = []
    for item in pages:
        href = ("./" if item.slug == "index" else f"{item.slug}/") if is_index else ("../" if item.slug == "index" else f"../{item.slug}/")
        active = ' class="active" aria-current="page"' if item.slug == page.slug else ""
        nav_items.append(f'<li><a href="{href}"{active}>{html.escape(item.title)}</a></li>')
    structured = json.dumps({
        "@context": "https://schema.org",
        "@type": "TechArticle",
        "headline": page.title,
        "description": page.description,
        "url": canonical,
        "license": "https://spdx.org/licenses/GPL-3.0-or-later.html",
        "isPartOf": {"@type": "WebSite", "name": "ARSVIN", "url": BASE_URL},
        "author": {"@type": "Person", "name": "Ari Sulistiono", "url": "https://github.com/masarray"},
    }, ensure_ascii=False, separators=(",", ":"))

    return f'''<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{html.escape(page.title)} | ARSVIN Documentation</title>
  <meta name="description" content="{html.escape(page.description, quote=True)}" />
  <meta name="robots" content="index,follow,max-image-preview:large" />
  <meta name="theme-color" content="#f5f8fc" />
  <link rel="canonical" href="{canonical}" />
  <meta property="og:type" content="article" />
  <meta property="og:site_name" content="ARSVIN" />
  <meta property="og:title" content="{html.escape(page.title, quote=True)}" />
  <meta property="og:description" content="{html.escape(page.description, quote=True)}" />
  <meta property="og:url" content="{canonical}" />
  <meta property="og:image" content="{BASE_URL}assets/arsvin-social-preview.png" />
  <meta name="twitter:card" content="summary_large_image" />
  <link rel="icon" type="image/png" sizes="32x32" href="{prefix}assets/favicon-32x32.png" />
  <link rel="stylesheet" href="{prefix}styles.css" />
  <link rel="stylesheet" href="{prefix}docs.css" />
  <script type="application/ld+json">{structured}</script>
</head>
<body class="docs-body">
  <header class="topbar docs-topbar">
    <a class="brand" href="{prefix}" aria-label="ARSVIN home"><img src="{prefix}assets/arsvin.png" alt="" width="30" height="30" /><span>ARSVIN</span></a>
    <nav aria-label="Primary navigation"><a href="{prefix}">Product</a><a href="{prefix}#downloads">Downloads</a><a href="{prefix}docs/" aria-current="page">Docs</a><a href="{prefix}#licensing">Licensing</a></nav>
    <a class="header-link" href="{REPO_URL}">GitHub</a>
  </header>
  <main class="docs-shell">
    <aside class="docs-sidebar" aria-label="Documentation navigation">
      <div class="docs-sidebar-head"><strong>Engineering docs</strong><input id="docs-filter" type="search" placeholder="Filter topics" aria-label="Filter documentation topics" /></div>
      <ul id="docs-nav">{''.join(nav_items)}</ul>
    </aside>
    <article class="docs-article">
      <nav class="breadcrumbs" aria-label="Breadcrumb"><a href="{prefix}">ARSVIN</a><span>/</span><a href="{prefix}docs/">Docs</a><span>/</span><span>{html.escape(page.title)}</span></nav>
      {body}
      <div class="docs-source"><span>GPL-3.0-or-later community documentation. Commercial terms require a separate agreement.</span><a href="{source_url}">Review this page on GitHub →</a></div>
    </article>
  </main>
  <footer><div class="shell footer-inner"><span>ARSVIN · GPL-3.0-or-later · © 2026 Ari Sulistiono</span><nav aria-label="Footer navigation"><a href="{prefix}">Product</a><a href="{prefix}docs/">Docs</a><a href="{REPO_URL}/issues">Issues</a></nav></div></footer>
  <script>(() => {{ const input = document.getElementById('docs-filter'); const items = Array.from(document.querySelectorAll('#docs-nav li')); if (!input) return; input.addEventListener('input', () => {{ const query = input.value.trim().toLowerCase(); items.forEach((item) => {{ item.hidden = Boolean(query) && !item.textContent.toLowerCase().includes(query); }}); }}); }})();</script>
</body>
</html>
'''


def build(repo_root: Path, output: Path) -> None:
    site = repo_root / "site"
    docs_dir = repo_root / "docs"
    if not site.is_dir() or not docs_dir.is_dir():
        raise SystemExit("Expected site/ and docs/ directories under the repository root.")
    if output.exists():
        shutil.rmtree(output)
    shutil.copytree(site, output, ignore=shutil.ignore_patterns("docs"))

    pages_by_name: dict[str, DocPage] = {}
    for source in sorted(docs_dir.glob("*.md")):
        title, description = metadata(source)
        slug = "index" if source.name == "index.md" else source.stem
        pages_by_name[source.name] = DocPage(source, slug, title, description)
    if "index.md" not in pages_by_name:
        raise SystemExit("docs/index.md is required.")

    pages: list[DocPage] = [pages_by_name["index.md"]]
    pages.extend(page for name, page in sorted(pages_by_name.items()) if name != "index.md")
    docs_output = output / "docs"
    docs_output.mkdir(parents=True, exist_ok=True)
    for page in pages:
        target = docs_output / "index.html" if page.slug == "index" else docs_output / page.slug / "index.html"
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(render(page, pages, pages_by_name), encoding="utf-8", newline="\n")

    index = [{"title": page.title, "description": page.description, "url": "./" if page.slug == "index" else f"{page.slug}/", "source": page.source.name} for page in pages]
    (docs_output / "search-index.json").write_text(json.dumps(index, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    urls = [BASE_URL] + [BASE_URL + "docs/" if page.slug == "index" else BASE_URL + f"docs/{page.slug}/" for page in pages]
    sitemap = ['<?xml version="1.0" encoding="UTF-8"?>', '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">']
    for url in urls:
        sitemap.extend(["  <url>", f"    <loc>{html.escape(url)}</loc>", "  </url>"])
    sitemap.append("</urlset>")
    (output / "sitemap.xml").write_text("\n".join(sitemap) + "\n", encoding="utf-8")
    (output / ".nojekyll").write_text("", encoding="utf-8")
    print(f"Built public site: {output}")
    print(f"Generated documentation pages: {len(pages)}")