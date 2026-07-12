#!/usr/bin/env python3
from __future__ import annotations

import argparse
import html
import json
import re
import shutil
from dataclasses import dataclass
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit

BASE_URL = "https://masarray.github.io/arsvin/"
REPO_URL = "https://github.com/masarray/arsvin"


@dataclass(frozen=True)
class DocPage:
    source: Path
    slug: str
    title: str
    description: str


def slugify(value: str) -> str:
    value = re.sub(r"<[^>]+>", "", value)
    value = re.sub(r"[`*_~]", "", value)
    value = value.lower().strip()
    value = re.sub(r"[^a-z0-9]+", "-", value)
    return value.strip("-") or "section"


def strip_markdown(value: str) -> str:
    value = re.sub(r"`([^`]+)`", r"\1", value)
    value = re.sub(r"!\[([^\]]*)\]\([^)]*\)", r"\1", value)
    value = re.sub(r"\[([^\]]+)\]\([^)]*\)", r"\1", value)
    value = re.sub(r"[*_~>#]", "", value)
    return re.sub(r"\s+", " ", value).strip()


def page_metadata(path: Path) -> tuple[str, str]:
    lines = path.read_text(encoding="utf-8").splitlines()
    title = path.stem.replace("-", " ").title()
    title_index = -1
    for index, line in enumerate(lines):
        match = re.match(r"^#\s+(.+?)\s*$", line)
        if match:
            title = strip_markdown(match.group(1))
            title_index = index
            break

    paragraph: list[str] = []
    for line in lines[title_index + 1 :]:
        stripped = line.strip()
        if not stripped:
            if paragraph:
                break
            continue
        if stripped.startswith(("#", "- ", "* ", ">", "```", "|")):
            if paragraph:
                break
            continue
        paragraph.append(stripped)

    description = strip_markdown(" ".join(paragraph))
    if not description:
        description = f"ARSVIN engineering documentation for {title}."
    if len(description) > 158:
        description = description[:155].rsplit(" ", 1)[0] + "..."
    return title, description


def rewrite_link(target: str, current_slug: str) -> str:
    target = html.unescape(target.strip())
    if not target or target.startswith("#"):
        return target
    if re.match(r"^(?:https?:|mailto:|tel:|data:)", target, re.IGNORECASE):
        return target

    parts = urlsplit(target)
    path = parts.path.replace("\\", "/")
    fragment = parts.fragment
    query = parts.query

    if path.endswith(".md"):
        normalized = path
        while normalized.startswith("./"):
            normalized = normalized[2:]
        if normalized.startswith("../"):
            github_target = f"{REPO_URL}/blob/main/{normalized[3:]}"
            return urlunsplit(("https", "github.com", github_target.split("github.com/", 1)[1], query, fragment))

        stem = Path(normalized).stem
        parent = Path(normalized).parent.as_posix()
        if parent not in (".", ""):
            github_target = f"{REPO_URL}/blob/main/docs/{normalized}"
            return urlunsplit(("https", "github.com", github_target.split("github.com/", 1)[1], query, fragment))

        if current_slug == "index":
            href = "./" if stem == "index" else f"{stem}/"
        else:
            href = "../" if stem == "index" else f"../{stem}/"
        if query:
            href += f"?{query}"
        if fragment:
            href += f"#{fragment}"
        return href

    return target


def inline_markdown(text: str, current_slug: str) -> str:
    code_values: list[str] = []

    def code_repl(match: re.Match[str]) -> str:
        token = f"@@ARSVINCODE{len(code_values)}@@"
        code_values.append(f"<code>{html.escape(match.group(1), quote=False)}</code>")
        return token

    text = re.sub(r"`([^`]+)`", code_repl, text)
    text = html.escape(text, quote=False)

    def image_repl(match: re.Match[str]) -> str:
        alt = match.group(1)
        src = rewrite_link(html.unescape(match.group(2)), current_slug)
        return f'<img src="{html.escape(src, quote=True)}" alt="{alt}" loading="lazy" />'

    def link_repl(match: re.Match[str]) -> str:
        label = match.group(1)
        target = rewrite_link(html.unescape(match.group(2)), current_slug)
        external = bool(re.match(r"^https?://", target, re.IGNORECASE))
        attrs = ' target="_blank" rel="noopener noreferrer"' if external else ""
        return f'<a href="{html.escape(target, quote=True)}"{attrs}>{label}</a>'

    text = re.sub(r"!\[([^\]]*)\]\(([^)]+)\)", image_repl, text)
    text = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", link_repl, text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"__([^_]+)__", r"<strong>\1</strong>", text)
    text = re.sub(r"(?<!\*)\*([^*]+)\*(?!\*)", r"<em>\1</em>", text)

    for index, value in enumerate(code_values):
        text = text.replace(f"@@ARSVINCODE{index}@@", value)
    return text


def is_table_separator(line: str) -> bool:
    cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
    return bool(cells) and all(re.fullmatch(r":?-{3,}:?", cell or "") for cell in cells)


def markdown_to_html(markdown: str, current_slug: str) -> str:
    lines = markdown.splitlines()
    output: list[str] = []
    used_ids: dict[str, int] = {}
    first_h1_seen = False
    index = 0

    def unique_id(text: str) -> str:
        base = slugify(strip_markdown(text))
        count = used_ids.get(base, 0)
        used_ids[base] = count + 1
        return base if count == 0 else f"{base}-{count + 1}"

    while index < len(lines):
        line = lines[index]
        stripped = line.strip()

        if not stripped:
            index += 1
            continue

        fence = re.match(r"^```\s*([A-Za-z0-9_+.-]*)\s*$", stripped)
        if fence:
            language = fence.group(1)
            code_lines: list[str] = []
            index += 1
            while index < len(lines) and not re.match(r"^```\s*$", lines[index].strip()):
                code_lines.append(lines[index])
                index += 1
            if index < len(lines):
                index += 1
            class_attr = f' class="language-{html.escape(language, quote=True)}"' if language else ""
            output.append(f"<pre><code{class_attr}>{html.escape(chr(10).join(code_lines), quote=False)}</code></pre>")
            continue

        heading = re.match(r"^(#{1,6})\s+(.+?)\s*#*\s*$", stripped)
        if heading:
            level = len(heading.group(1))
            text = heading.group(2)
            if level == 1:
                if first_h1_seen:
                    level = 2
                else:
                    first_h1_seen = True
            anchor = unique_id(text)
            output.append(f'<h{level} id="{anchor}">{inline_markdown(text, current_slug)}</h{level}>')
            index += 1
            continue

        if index + 1 < len(lines) and "|" in stripped and is_table_separator(lines[index + 1]):
            headers = [cell.strip() for cell in stripped.strip("|").split("|")]
            index += 2
            rows: list[list[str]] = []
            while index < len(lines):
                candidate = lines[index].strip()
                if not candidate or "|" not in candidate:
                    break
                rows.append([cell.strip() for cell in candidate.strip("|").split("|")])
                index += 1
            output.append('<div class="table-wrap"><table><thead><tr>')
            output.extend(f"<th>{inline_markdown(cell, current_slug)}</th>" for cell in headers)
            output.append("</tr></thead><tbody>")
            for row in rows:
                padded = row + [""] * max(0, len(headers) - len(row))
                output.append("<tr>")
                output.extend(f"<td>{inline_markdown(cell, current_slug)}</td>" for cell in padded[: len(headers)])
                output.append("</tr>")
            output.append("</tbody></table></div>")
            continue

        unordered = re.match(r"^[-*+]\s+(.+)$", stripped)
        ordered = re.match(r"^\d+[.)]\s+(.+)$", stripped)
        if unordered or ordered:
            tag = "ul" if unordered else "ol"
            output.append(f"<{tag}>")
            while index < len(lines):
                candidate = lines[index].strip()
                match = re.match(r"^[-*+]\s+(.+)$", candidate) if tag == "ul" else re.match(r"^\d+[.)]\s+(.+)$", candidate)
                if not match:
                    break
                output.append(f"<li>{inline_markdown(match.group(1), current_slug)}</li>")
                index += 1
            output.append(f"</{tag}>")
            continue

        if stripped.startswith(">"):
            quoted: list[str] = []
            while index < len(lines) and lines[index].strip().startswith(">"):
                quoted.append(lines[index].strip()[1:].lstrip())
                index += 1
            output.append(f"<blockquote><p>{inline_markdown(' '.join(quoted), current_slug)}</p></blockquote>")
            continue

        if re.fullmatch(r"(?:-{3,}|\*{3,}|_{3,})", stripped):
            output.append("<hr />")
            index += 1
            continue

        paragraph: list[str] = []
        while index < len(lines):
            candidate = lines[index].strip()
            if not candidate:
                break
            if re.match(r"^(?:#{1,6}\s|```|[-*+]\s+|\d+[.)]\s+|>|(?:-{3,}|\*{3,}|_{3,})$)", candidate):
                break
            if index + 1 < len(lines) and "|" in candidate and is_table_separator(lines[index + 1]):
                break
            paragraph.append(candidate)
            index += 1
        if paragraph:
            output.append(f"<p>{inline_markdown(' '.join(paragraph), current_slug)}</p>")
        else:
            output.append(f"<p>{inline_markdown(stripped, current_slug)}</p>")
            index += 1

    if not first_h1_seen:
        output.insert(0, '<h1 id="documentation">Documentation</h1>')
    return "\n".join(output)


def docs_order(docs_dir: Path, pages_by_name: dict[str, DocPage]) -> list[DocPage]:
    ordered: list[DocPage] = []
    seen: set[str] = set()
    index_path = docs_dir / "index.md"
    if index_path.exists():
        for match in re.finditer(r"\[[^\]]+\]\(([^)#?]+\.md)(?:#[^)]+)?\)", index_path.read_text(encoding="utf-8")):
            name = Path(match.group(1)).name
            page = pages_by_name.get(name)
            if page and name not in seen:
                ordered.append(page)
                seen.add(name)
    for name in sorted(pages_by_name):
        if name not in seen:
            ordered.append(pages_by_name[name])
    return ordered


def nav_html(pages: list[DocPage], current_slug: str) -> str:
    items: list[str] = []
    for page in pages:
        if current_slug == "index":
            href = "./" if page.slug == "index" else f"{page.slug}/"
        else:
            href = "../" if page.slug == "index" else f"../{page.slug}/"
        active = ' class="active" aria-current="page"' if page.slug == current_slug else ""
        items.append(f'<li><a href="{href}"{active}>{html.escape(page.title)}</a></li>')
    return "\n".join(items)


def render_page(page: DocPage, pages: list[DocPage]) -> str:
    is_index = page.slug == "index"
    prefix = "../" if is_index else "../../"
    canonical = f"{BASE_URL}docs/" if is_index else f"{BASE_URL}docs/{page.slug}/"
    source_url = f"{REPO_URL}/blob/main/docs/{page.source.name}"
    body = markdown_to_html(page.source.read_text(encoding="utf-8"), page.slug)
    nav = nav_html(pages, page.slug)
    structured = {
        "@context": "https://schema.org",
        "@type": "TechArticle",
        "headline": page.title,
        "description": page.description,
        "url": canonical,
        "isPartOf": {"@type": "WebSite", "name": "ARSVIN", "url": BASE_URL},
        "author": {"@type": "Person", "name": "Ari Sulistiono", "url": "https://github.com/masarray"},
        "license": "https://www.apache.org/licenses/LICENSE-2.0",
        "about": ["IEC 61850", "Sampled Values", "Digital substation", "Process bus"],
    }
    json_ld = json.dumps(structured, ensure_ascii=False, separators=(",", ":"))
    breadcrumb_label = "Documentation" if is_index else page.title

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
  <meta name="twitter:title" content="{html.escape(page.title, quote=True)}" />
  <meta name="twitter:description" content="{html.escape(page.description, quote=True)}" />
  <link rel="icon" type="image/png" sizes="32x32" href="{prefix}assets/favicon-32x32.png" />
  <link rel="stylesheet" href="{prefix}styles.css" />
  <link rel="stylesheet" href="{prefix}docs.css" />
  <script type="application/ld+json">{json_ld}</script>
</head>
<body class="docs-body">
  <header class="topbar docs-topbar">
    <a class="brand" href="{prefix}" aria-label="ARSVIN home">
      <img src="{prefix}assets/arsvin.png" alt="" width="30" height="30" />
      <span>ARSVIN</span>
    </a>
    <nav aria-label="Primary navigation">
      <a href="{prefix}">Product</a>
      <a href="{prefix}#downloads">Downloads</a>
      <a href="{prefix}docs/" aria-current="page">Docs</a>
    </nav>
    <a class="header-link" href="{REPO_URL}">GitHub</a>
  </header>

  <main class="docs-shell">
    <aside class="docs-sidebar" aria-label="Documentation navigation">
      <div class="docs-sidebar-head">
        <strong>Engineering docs</strong>
        <input id="docs-filter" type="search" placeholder="Filter topics" aria-label="Filter documentation topics" />
      </div>
      <ul id="docs-nav">{nav}</ul>
    </aside>

    <article class="docs-article">
      <nav class="breadcrumbs" aria-label="Breadcrumb">
        <a href="{prefix}">ARSVIN</a><span>/</span><a href="{prefix}docs/">Docs</a><span>/</span><span>{html.escape(breadcrumb_label)}</span>
      </nav>
      {body}
      <div class="docs-source">
        <span>Apache-2.0 engineering documentation.</span>
        <a href="{source_url}">Edit or review this page on GitHub →</a>
      </div>
    </article>
  </main>

  <footer>
    <div class="shell footer-inner">
      <span>ARSVIN · Apache-2.0 · © 2026 Ari Sulistiono</span>
      <nav aria-label="Footer navigation"><a href="{prefix}">Product</a><a href="{prefix}docs/">Docs</a><a href="{REPO_URL}/issues">Issues</a></nav>
    </div>
  </footer>
  <script>
    (() => {{
      const input = document.getElementById('docs-filter');
      const links = Array.from(document.querySelectorAll('#docs-nav li'));
      if (!input) return;
      input.addEventListener('input', () => {{
        const query = input.value.trim().toLowerCase();
        links.forEach((item) => {{ item.hidden = query && !item.textContent.toLowerCase().includes(query); }});
      }});
    }})();
  </script>
</body>
</html>
'''


def build(repo_root: Path, output: Path) -> None:
    site_dir = repo_root / "site"
    docs_dir = repo_root / "docs"
    if not site_dir.is_dir() or not docs_dir.is_dir():
        raise SystemExit("Expected site/ and docs/ directories under the repository root.")

    if output.exists():
        shutil.rmtree(output)
    shutil.copytree(site_dir, output, ignore=shutil.ignore_patterns("docs"))

    docs_output = output / "docs"
    docs_output.mkdir(parents=True, exist_ok=True)

    pages_by_name: dict[str, DocPage] = {}
    for source in sorted(docs_dir.glob("*.md")):
        title, description = page_metadata(source)
        slug = "index" if source.name == "index.md" else source.stem
        pages_by_name[source.name] = DocPage(source=source, slug=slug, title=title, description=description)

    if "index.md" not in pages_by_name:
        raise SystemExit("docs/index.md is required for the documentation site.")

    pages = docs_order(docs_dir, pages_by_name)
    for page in pages:
        target = docs_output / "index.html" if page.slug == "index" else docs_output / page.slug / "index.html"
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(render_page(page, pages), encoding="utf-8", newline="\n")

    search_index = [
        {
            "title": page.title,
            "description": page.description,
            "url": "./" if page.slug == "index" else f"{page.slug}/",
            "source": page.source.name,
        }
        for page in pages
    ]
    (docs_output / "search-index.json").write_text(
        json.dumps(search_index, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n"
    )

    urls = [BASE_URL] + [BASE_URL + "docs/" if page.slug == "index" else BASE_URL + f"docs/{page.slug}/" for page in pages]
    sitemap_lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
    ]
    for url in urls:
        sitemap_lines.extend(["  <url>", f"    <loc>{html.escape(url)}</loc>", "  </url>"])
    sitemap_lines.append("</urlset>")
    (output / "sitemap.xml").write_text("\n".join(sitemap_lines) + "\n", encoding="utf-8", newline="\n")
    (output / ".nojekyll").write_text("", encoding="utf-8")

    print(f"Built public site: {output}")
    print(f"Generated documentation pages: {len(pages)}")


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
