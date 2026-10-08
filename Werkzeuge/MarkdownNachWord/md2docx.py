# -*- coding: utf-8 -*-
"""Markdown -> HTML -> DOCX (über Word COM). Aufruf: py md2docx.py <in.md> <out.docx>"""
import html, re, subprocess, sys, tempfile, os

src = open(sys.argv[1], encoding="utf-8-sig").read().replace("\r\n", "\n").split("\n")
out_docx = os.path.abspath(sys.argv[2])
out_html = os.path.splitext(out_docx)[0] + ".html"

CODE = {}
def inline(t):
    def keep(m):
        k = f"\x00{len(CODE)}\x00"; CODE[k] = html.escape(m.group(1)); return k
    t = re.sub(r"`([^`]+)`", keep, t)
    t = html.escape(t, quote=False)
    t = re.sub(r"\[([^\]]+)\]\((https?://[^)\s]+)\)", r'<a href="\2">\1</a>', t)
    t = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", t)
    t = re.sub(r"(?<![\w*])\*(?!\s)([^*\n]+?)(?<!\s)\*(?![\w*])", r"<i>\1</i>", t)
    t = re.sub(r"~~(.+?)~~", r"<s>\1</s>", t)
    for k, v in CODE.items():
        t = t.replace(k, f"<code>{v}</code>")
    return t

o = []; para = []; lst = None; table = None; fence = False; code = []
def flush_para():
    global para
    if para: o.append("<p>" + inline(" ".join(s.strip() for s in para)) + "</p>"); para = []
def flush_list():
    global lst
    if lst: o.append(f"</{lst}>"); lst = None
def flush_table():
    global table
    if table:
        rows = [r for r in table if not re.match(r"^\|?\s*:?-{3,}", r)]
        cells = lambda r: [c.strip() for c in r.strip().strip("|").split("|")]
        h = "".join(f"<th>{inline(c)}</th>" for c in cells(rows[0]))
        b = "".join("<tr>" + "".join(f"<td>{inline(c)}</td>" for c in cells(r)) + "</tr>" for r in rows[1:])
        o.append(f"<table><tr>{h}</tr>{b}</table>"); table = None

for line in src:
    if line.startswith("```"):
        flush_para(); flush_list(); flush_table()
        if fence: o.append("<pre>" + html.escape("\n".join(code)) + "</pre>"); code = []
        fence = not fence; continue
    if fence: code.append(line); continue
    s = line.strip()
    if s.startswith("|"):
        flush_para(); flush_list(); table = (table or []) + [s]; continue
    flush_table()
    m = re.match(r"^(#{1,6})\s+(.*)$", s)
    if m:
        flush_para(); flush_list(); o.append(f"<h{len(m.group(1))}>{inline(m.group(2))}</h{len(m.group(1))}>"); continue
    if re.match(r"^(-{3,}|\*{3,})$", s):
        flush_para(); flush_list(); o.append("<hr>"); continue
    m = re.match(r"^([-*]|\d+\.)\s+(.*)$", s)
    if m:
        flush_para(); kind = "ol" if m.group(1)[0].isdigit() else "ul"
        if lst != kind: flush_list(); o.append(f"<{kind}>"); lst = kind
        o.append(f"<li>{inline(m.group(2))}</li>"); continue
    if s.startswith(">"):
        flush_para(); flush_list(); o.append(f"<blockquote>{inline(s.lstrip('> '))}</blockquote>"); continue
    if not s:
        flush_para(); flush_list(); continue
    if lst and line.startswith("  "):
        o[-1] = o[-1][:-5] + " " + inline(s) + "</li>"; continue
    flush_list(); para.append(line)
flush_para(); flush_list(); flush_table()

css = """body{font-family:Calibri,Arial,sans-serif;font-size:11pt;line-height:1.25}
h1{font-size:18pt}h2{font-size:14pt;margin-top:18pt}h3{font-size:12pt;margin-top:12pt}h4{font-size:11pt}
table{border-collapse:collapse;margin:6pt 0}th,td{border:1px solid #808080;padding:2pt 5pt;vertical-align:top;font-size:9.5pt}
th{background:#e7e6e6}pre,code{font-family:Consolas,'Courier New',monospace;font-size:9pt}
pre{background:#f2f2f2;border:1px solid #bfbfbf;padding:4pt;white-space:pre-wrap}blockquote{margin-left:14pt;color:#404040}"""
doc = f'<html><head><meta charset="utf-8"><style>{css}</style></head><body>{"".join(o)}</body></html>'
open(out_html, "w", encoding="utf-8-sig").write(doc)

ps = f"""$w = New-Object -ComObject Word.Application; $w.Visible = $false; $w.DisplayAlerts = 0
$d = $w.Documents.Open('{out_html}', $false, $false, $false)
$d.SaveAs2('{out_docx}', 12); $d.Close(0); $w.Quit()
Write-Output 'docx geschrieben'"""
r = subprocess.run(["powershell", "-NoProfile", "-Command", ps], capture_output=True, text=True)
print(r.stdout.strip(), r.stderr.strip()[:300])
os.remove(out_html)
print("Blöcke:", len(o), "| Größe docx:", os.path.getsize(out_docx) if os.path.exists(out_docx) else "fehlt")
