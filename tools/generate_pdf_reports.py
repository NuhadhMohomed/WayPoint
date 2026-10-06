"""
PDF Report Generator for SE3110 Quality Management Submission
==============================================================
Converts Markdown deliverables in docs/testing/ into styled HTML and compiles
official PDF documents using Microsoft Edge headless engine.
"""

import os
import sys
import subprocess
from pathlib import Path
import markdown_it

EDGE_PATH = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
if not os.path.exists(EDGE_PATH):
    EDGE_PATH = r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"

DOCS_DIR = Path(__file__).resolve().parent.parent / "docs" / "testing"

HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>{title}</title>
<style>
  @page {{
    size: A4;
    margin: 1.8cm 1.5cm;
    @bottom-right {{
      content: counter(page);
    }}
  }}
  body {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    line-height: 1.55;
    color: #1f2328;
    max-width: 900px;
    margin: 0 auto;
    font-size: 11pt;
  }}
  h1 {{
    color: #0969da;
    border-bottom: 2px solid #d0d7de;
    padding-bottom: 0.3em;
    font-size: 20pt;
    margin-top: 0;
  }}
  h2 {{
    color: #1f2328;
    border-bottom: 1px solid #d0d7de;
    padding-bottom: 0.25em;
    font-size: 14pt;
    margin-top: 1.5em;
    page-break-after: avoid;
  }}
  h3 {{
    color: #24292f;
    font-size: 12pt;
    margin-top: 1.2em;
    page-break-after: avoid;
  }}
  table {{
    border-collapse: collapse;
    width: 100%;
    margin: 14px 0;
    font-size: 9.5pt;
    page-break-inside: auto;
  }}
  tr {{
    page-break-inside: avoid;
    page-break-after: auto;
  }}
  th, td {{
    border: 1px solid #d0d7de;
    padding: 6px 10px;
    text-align: left;
    vertical-align: top;
  }}
  th {{
    background-color: #f6f8fa;
    font-weight: 600;
  }}
  tr:nth-child(even) {{
    background-color: #fbfcfd;
  }}
  code {{
    background-color: rgba(175, 184, 193, 0.2);
    padding: 0.15em 0.35em;
    border-radius: 4px;
    font-family: ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace;
    font-size: 85%;
  }}
  pre {{
    background-color: #f6f8fa;
    border: 1px solid #d0d7de;
    border-radius: 6px;
    padding: 10px;
    overflow-x: auto;
    font-size: 8.5pt;
    page-break-inside: avoid;
  }}
  pre code {{
    background-color: transparent;
    padding: 0;
  }}
  blockquote {{
    border-left: 4px solid #0969da;
    padding: 0.4em 1em;
    color: #57609a;
    background-color: #f6f8fa;
    margin: 1em 0;
  }}
  hr {{
    border: 0;
    height: 1px;
    background: #d0d7de;
    margin: 1.5em 0;
  }}
</style>
</head>
<body>
{body}
</body>
</html>
"""

def convert_md_to_pdf(md_file: Path):
    print(f"Processing: {md_file.name} ...")
    md = markdown_it.MarkdownIt("commonmark", {"breaks": True, "html": True}).enable("table")
    
    with open(md_file, "r", encoding="utf-8") as f:
        md_text = f.read()

    rendered_html = md.render(md_text)
    full_html = HTML_TEMPLATE.format(
        title=md_file.stem.replace("_", " "),
        body=rendered_html
    )

    html_file = md_file.with_suffix(".html")
    pdf_file = md_file.with_suffix(".pdf")

    with open(html_file, "w", encoding="utf-8") as f:
        f.write(full_html)

    if not os.path.exists(EDGE_PATH):
        print(f"[WARN] Edge executable not found. Generated HTML only: {html_file}")
        return

    cmd = [
        EDGE_PATH,
        "--headless",
        "--disable-gpu",
        "--no-pdf-header-footer",
        f"--print-to-pdf={pdf_file.resolve()}",
        f"file:///{html_file.resolve()}"
    ]

    subprocess.run(cmd, check=True)
    if pdf_file.exists():
        print(f"  -> Generated PDF: {pdf_file.name} ({pdf_file.stat().st_size:,} bytes)")
    else:
        print(f"  [ERROR] Failed to produce {pdf_file.name}")

def main():
    target_files = [
        "SE3110_Software_Testing_Report.md",
        "SE3110_Test_Case_Document.md",
        "SE3110_Defect_Bug_Report.md",
        "SE3110_Tool_Generated_Evidence.md",
        "SE3110_Viva_Preparation_Guide.md"
    ]

    for filename in target_files:
        md_path = DOCS_DIR / filename
        if md_path.exists():
            convert_md_to_pdf(md_path)
        else:
            print(f"[SKIP] Not found: {filename}")

if __name__ == "__main__":
    main()
