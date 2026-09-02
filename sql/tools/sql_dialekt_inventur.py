# -*- coding: utf-8 -*-
r"""PRUEFREZEPT 1 - SQL-Dialekt-Inventur (Arbeitspaket S5, DB-Migration Access -> SQLite).

ZWECK
  Weist die VOLLSTAENDIGKEIT eines Dialekt-Sweeps nach. Das Skript extrahiert mit
  einem C#-Lexer die echten String-Literale aus WindowsFormsApplication1\ (Kommentare,
  Char-Literale, Verbatim- und interpolierte Strings werden korrekt behandelt) und
  misst NUR IN DIESEN LITERALEN die Dialekt-Merkmale. Dadurch zaehlen Erwaehnungen in
  Kommentaren und Dokumentation nicht mit - genau das unterscheidet das Rezept von
  einem blossen grep.

  Gemessen werden u. a.:
    d) SELECT TOP / TOP <n>        h) Access-Funktionen (IIf, Nz, UCase, DLookup, ...)
    e) @@IDENTITY                  i) #Datum#, &-Verkettung, DISTINCTROW, TRANSFORM
    f) = TRUE / = FALSE            j) Abfrage_-Namen (gespeicherte Access-Abfragen)
    g) LIKE                        a-c) '?'-Platzhalter, OleDbParameter

AUFRUF
  python sql_dialekt_inventur.py                 # Bericht nach stdout
  python sql_dialekt_inventur.py > befund.txt    # Bericht sichern

  Rein LESEND. Schreibt nichts in den Quellbaum.
  Unter Windows-Konsolen empfiehlt sich  PYTHONIOENCODING=utf-8.

SOLLWERTE NACH S5 und die bewussten Ausnahmen: siehe sql/MIGRATION_Pruefrezepte.md.

Extrahiert echte C#-String-Literale mit einem Lexer (Kommentare, Char-Literale,
Verbatim-/Interpolierte Strings korrekt behandelt) und misst SQL-Dialekt-Merkmale.
Fuer jedes Zeichen des Literalinhalts wird die Quelltextzeile mitgefuehrt,
damit Treffer in mehrzeiligen Verbatim-Strings exakt lokalisiert werden.
"""
import os, re, sys, json, io
from collections import defaultdict, Counter

ROOT = r"C:\Users\DirkEngelmann\Documents\WP-Plan\WindowsFormsApplication1"
OUT  = os.path.dirname(os.path.abspath(__file__))

# ---------------------------------------------------------------- file loading
def read_text(path):
    with open(path, "rb") as f:
        raw = f.read()
    if raw.startswith(b"\xef\xbb\xbf"):
        return raw.decode("utf-8-sig"), "utf-8-bom"
    try:
        return raw.decode("utf-8"), "utf-8"
    except UnicodeDecodeError:
        return raw.decode("cp1252"), "cp1252"

def cs_files(root):
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d.lower() not in ("bin", "obj", ".git", ".vs")]
        for fn in filenames:
            if fn.lower().endswith(".cs"):
                yield os.path.join(dirpath, fn)

# ---------------------------------------------------------------- C# lexer
class Lit(object):
    __slots__ = ("file", "line", "kind", "content", "linemap", "raw")
    def __init__(self, file, line, kind, content, linemap, raw):
        self.file = file; self.line = line; self.kind = kind
        self.content = content; self.linemap = linemap; self.raw = raw
    def line_of(self, idx):
        if not self.linemap:
            return self.line
        if idx < 0: idx = 0
        if idx >= len(self.linemap): idx = len(self.linemap) - 1
        return self.linemap[idx]

def lex_strings(text, fname):
    """Yield Lit objects for every C# string literal in `text`."""
    lits = []
    i = 0
    n = len(text)
    line = 1
    while i < n:
        c = text[i]
        if c == "\n":
            line += 1; i += 1; continue
        # comments
        if c == "/" and i + 1 < n:
            if text[i+1] == "/":
                j = text.find("\n", i)
                if j < 0: break
                i = j; continue
            if text[i+1] == "*":
                j = text.find("*/", i + 2)
                if j < 0: break
                line += text.count("\n", i, j)
                i = j + 2; continue
        # char literal
        if c == "'":
            j = i + 1
            while j < n:
                if text[j] == "\\":
                    j += 2; continue
                if text[j] == "'":
                    j += 1; break
                if text[j] == "\n":
                    break
                j += 1
            i = j; continue
        # string prefixes
        verbatim = False; interp = False
        start = i
        k = i
        while k < n and text[k] in "@$":
            if text[k] == "@": verbatim = True
            else: interp = True
            k += 1
        if k < n and text[k] == '"' and (k > i or c == '"'):
            startline = line
            content = []
            lmap = []
            p = k + 1
            cur = line
            if verbatim:
                while p < n:
                    ch = text[p]
                    if ch == '"':
                        if p + 1 < n and text[p+1] == '"':
                            content.append('"'); lmap.append(cur); p += 2; continue
                        p += 1; break
                    if interp and ch == "{":
                        if p + 1 < n and text[p+1] == "{":
                            content.append("{"); lmap.append(cur); p += 2; continue
                        # skip hole
                        depth = 1; p += 1
                        while p < n and depth > 0:
                            if text[p] == "{": depth += 1
                            elif text[p] == "}": depth -= 1
                            elif text[p] == "\n": cur += 1
                            p += 1
                        content.append("\x00"); lmap.append(cur)
                        continue
                    if ch == "\n":
                        cur += 1
                    content.append(ch); lmap.append(cur)
                    p += 1
            else:
                while p < n:
                    ch = text[p]
                    if ch == "\\":
                        nxt = text[p+1] if p + 1 < n else ""
                        mapping = {"n": "\n", "t": "\t", "r": "\r", "0": "\0",
                                   "\\": "\\", '"': '"', "'": "'", "a": "\a",
                                   "b": "\b", "f": "\f", "v": "\v"}
                        if nxt in mapping:
                            content.append(mapping[nxt]); lmap.append(cur); p += 2; continue
                        if nxt == "u" or nxt == "x":
                            m = re.match(r"\\[ux]([0-9A-Fa-f]{1,8})", text[p:p+10])
                            if m:
                                try: content.append(chr(int(m.group(1), 16)))
                                except Exception: content.append("?")
                                lmap.append(cur); p += m.end(); continue
                        content.append(nxt); lmap.append(cur); p += 2; continue
                    if ch == '"':
                        p += 1; break
                    if ch == "\n":
                        break  # unterminated -> stop
                    if interp and ch == "{":
                        if p + 1 < n and text[p+1] == "{":
                            content.append("{"); lmap.append(cur); p += 2; continue
                        depth = 1; p += 1
                        while p < n and depth > 0:
                            if text[p] == "{": depth += 1
                            elif text[p] == "}": depth -= 1
                            elif text[p] == "\n": break
                            p += 1
                        content.append("\x00"); lmap.append(cur)
                        continue
                    content.append(ch); lmap.append(cur)
                    p += 1
            kind = ("$" if interp else "") + ("@" if verbatim else "") + '"'
            lits.append(Lit(fname, startline, kind, "".join(content), lmap, text[start:p]))
            line = cur
            i = p
            continue
        i += 1
    return lits

# ---------------------------------------------------------------- scan
files = sorted(cs_files(ROOT))
all_lits = []
enc_stat = Counter()
file_text = {}
for f in files:
    t, enc = read_text(f)
    enc_stat[enc] += 1
    file_text[f] = t
    all_lits.extend(lex_strings(t, f))

def rel(p):
    return os.path.relpath(p, os.path.dirname(ROOT)).replace("\\", "/")

report = []
def out(s=""):
    report.append(s)

out("### Basis")
out("Dateien (.cs, ohne bin/obj): %d   Encodings: %s" % (len(files), dict(enc_stat)))
out("Extrahierte String-Literale: %d" % len(all_lits))
kinds = Counter(l.kind for l in all_lits)
out("Literaltypen: %s" % dict(kinds))
out("")

# ---------------------------------------------------------------- SQL-Heuristik
SQL_RE = re.compile(r"(?is)\b(select|insert\s+into|update\s+|delete\s+from|from\s+\w|where\s|inner\s+join|left\s+join|order\s+by|group\s+by|create\s+table|alter\s+table|drop\s+table|values\s*\()")
def is_sqlish(s):
    return bool(SQL_RE.search(s))

# ---------------------------------------------------------------- a) '?'
lines_with_q = set()
lits_with_q = 0
q_total = 0
sql_lines_with_q = set()
for l in all_lits:
    if "?" in l.content:
        lits_with_q += 1
        q_total += l.content.count("?")
        for idx, ch in enumerate(l.content):
            if ch == "?":
                lines_with_q.add((l.file, l.line_of(idx)))
        if is_sqlish(l.content):
            for idx, ch in enumerate(l.content):
                if ch == "?":
                    sql_lines_with_q.add((l.file, l.line_of(idx)))

out("### a) '?' im Literalinhalt")
out("distinkte Quelltextzeilen mit '?' in Literal : %d" % len(lines_with_q))
out("Literale mit mindestens einem '?'           : %d" % lits_with_q)
out("'?'-Zeichen gesamt in Literalen              : %d" % q_total)
out("distinkte Zeilen, Literal SQL-verdaechtig    : %d" % len(sql_lines_with_q))
per_file_q = Counter(f for f, _ in lines_with_q)
out("Top-Dateien (Zeilen mit '?'):")
for f, c in per_file_q.most_common(15):
    out("   %5d  %s" % (c, rel(f)))
out("")

# ---------------------------------------------------------------- b) '?' in '...'
out("### b) '?' innerhalb eines SQL-Hochkomma-Textliterals")
hits_b = []
for l in all_lits:
    if "?" not in l.content or "'" not in l.content:
        continue
    s = l.content
    inq = False
    i = 0
    while i < len(s):
        ch = s[i]
        if ch == "'":
            if inq and i + 1 < len(s) and s[i+1] == "'":
                i += 2; continue
            inq = not inq
            i += 1; continue
        if ch == "?" and inq:
            hits_b.append((l, i))
        i += 1
out("Treffer (naiver Toggle-Scan je Literal): %d" % len(hits_b))
for l, idx in hits_b:
    ctx = l.content[max(0, idx-70):idx+70].replace("\n", "\\n").replace("\x00", "{}")
    out("   %s:%d  ...%s..." % (rel(l.file), l.line_of(idx), ctx))
out("")

# ---------------------------------------------------------------- c) OleDbParameter
out("### c) new OleDbParameter")
pat_c = re.compile(r"\bnew\s+OleDbParameter\b")
tot_c = 0
per_c = Counter()
for f in files:
    # Kommentare grob ausblenden? -> roher Zaehler + Zaehler ohne Kommentarzeilen
    c = len(pat_c.findall(file_text[f]))
    if c:
        per_c[f] = c; tot_c += c
out("Vorkommen gesamt: %d  in %d Dateien" % (tot_c, len(per_c)))
# ohne Kommentarzeilen
tot_c2 = 0
for f in files:
    for ln in file_text[f].splitlines():
        st = ln.strip()
        if st.startswith("//") or st.startswith("*") or st.startswith("/*"):
            continue
        tot_c2 += len(pat_c.findall(ln))
out("Vorkommen ohne offensichtliche Kommentarzeilen: %d" % tot_c2)
out("Top-Dateien:")
for f, c in per_c.most_common(12):
    out("   %5d  %s" % (c, rel(f)))
out("")

# ---------------------------------------------------------------- generic literal regex scan
def scan(pattern, flags=re.I):
    rx = re.compile(pattern, flags)
    res = []
    for l in all_lits:
        for m in rx.finditer(l.content):
            res.append((l, m))
    return res

def fmt(l, m, w=60):
    s = l.content
    a = max(0, m.start()-w); b = min(len(s), m.end()+w)
    ctx = s[a:b].replace("\n", " ").replace("\r", " ").replace("\x00", "{}")
    ctx = re.sub(r"\s+", " ", ctx).strip()
    return "%s:%d  %s" % (rel(l.file), l.line_of(m.start()), ctx)

# ---------------------------------------------------------------- d) TOP
out("### d) SELECT TOP / TOP <n>")
d_hits = scan(r"\bTOP\s+\d+")
out("Treffer 'TOP <n>': %d  (Zeilen distinkt: %d)" % (
    len(d_hits), len(set((l.file, l.line_of(m.start())) for l, m in d_hits))))
for l, m in d_hits:
    out("   " + fmt(l, m, 45))
d2 = scan(r"\bSELECT\s+TOP\b")
out("davon 'SELECT TOP': %d" % len(d2))
d3 = [ (l,m) for l,m in scan(r"\bTOP\b") ]
out("alle 'TOP'-Woerter in Literalen: %d" % len(d3))
extra = [(l,m) for l,m in d3 if not re.match(r"(?i)TOP\s+\d", l.content[m.start():m.start()+10])]
if extra:
    out("   TOP ohne folgende Zahl (Verkettungsgrenze/Fremdtext):")
    for l, m in extra:
        out("      " + fmt(l, m, 40))
out("")

# ---------------------------------------------------------------- e) @@IDENTITY
out("### e) @@IDENTITY")
e_hits = scan(r"@@IDENTITY")
e_lines = sorted(set((l.file, l.line_of(m.start())) for l, m in e_hits))
out("Vorkommen: %d   distinkte Zeilen: %d   Dateien: %d" % (
    len(e_hits), len(e_lines), len(set(f for f, _ in e_lines))))
for f, ln in e_lines:
    out("   %s:%d" % (rel(f), ln))
out("")

# ---------------------------------------------------------------- f) Boolean
out("### f) = TRUE / = FALSE / <> TRUE / <> FALSE")
f_hits = scan(r"(=|<>)\s*\b(TRUE|FALSE)\b")
f_lines = sorted(set((l.file, l.line_of(m.start())) for l, m in f_hits))
out("Vorkommen: %d   distinkte Zeilen: %d" % (len(f_hits), len(f_lines)))
for l, m in f_hits:
    out("   " + fmt(l, m, 45))
# breiter: alle TRUE/FALSE-Woerter in Literalen
f2 = scan(r"\b(TRUE|FALSE)\b")
out("alle TRUE/FALSE-Woerter in Literalen: %d (distinkte Zeilen %d)" % (
    len(f2), len(set((l.file, l.line_of(m.start())) for l, m in f2))))
rest = [(l,m) for l,m in f2 if not re.search(r"(=|<>)\s*$", l.content[max(0,m.start()-6):m.start()])]
out("   davon ohne unmittelbar vorangehendes =/<> : %d" % len(rest))
for l, m in rest[:60]:
    out("      " + fmt(l, m, 45))
out("")

# ---------------------------------------------------------------- g) LIKE
out("### g) ' LIKE '")
g_hits = scan(r"\sLIKE\s")
out("Vorkommen ' LIKE ': %d" % len(g_hits))
for l, m in g_hits:
    out("   " + fmt(l, m, 70))
g2 = scan(r"\bLIKE\b")
out("alle LIKE-Woerter: %d" % len(g2))
for l, m in g2:
    key = (rel(l.file), l.line_of(m.start()))
    pass
out("")

# ---------------------------------------------------------------- h) Access-Funktionen
out("### h) Access-Funktionen (Wortgrenze + '(')")
funcs = ["IIf","UCase","LCase","Trim","Nz","Format","Switch","Choose","Val","Str","CStr",
         "CInt","CDbl","CDate","CLng","CBool","DLookup","DCount","DSum","DMax","DMin","DAvg",
         "DateAdd","DateDiff","DatePart","DateSerial","InStr","InStrRev","StrComp",
         "IsNumeric","IsDate","Environ","CurrentUser","Eval","Partition","Mid","Left","Right",
         "Len","Replace","Round","Asc","Chr"]
h_res = {}
for fn in funcs:
    hits = scan(r"\b%s\s*\(" % fn)
    if hits:
        h_res[fn] = hits
for fn in funcs:
    hits = h_res.get(fn, [])
    perf = Counter(rel(l.file) for l, m in hits)
    out("%-12s %4d   %s" % (fn, len(hits), dict(perf) if len(perf) <= 8 else "%d Dateien" % len(perf)))
out("")
out("Fundorte fuer die kleinen Zaehler:")
for fn in funcs:
    hits = h_res.get(fn, [])
    if 0 < len(hits) <= 20:
        out("  -- %s (%d)" % (fn, len(hits)))
        for l, m in hits:
            out("     " + fmt(l, m, 45))
out("")

# ---------------------------------------------------------------- i) Access-Syntax
out("### i) #Datum#, &-Verkettung, DISTINCTROW, TRANSFORM, PARAMETERS")
i_date = scan(r"#\s*\d{1,4}[./-]\d{1,2}[./-]\d{1,4}[^#]*#")
out("#Datum#-Literale: %d" % len(i_date))
for l, m in i_date: out("   " + fmt(l, m, 40))
i_hash = scan(r"#")
hash_sql = [(l,m) for l,m in i_hash if is_sqlish(l.content)]
out("alle '#' in Literalen: %d   davon in SQL-verdaechtigen Literalen: %d" % (len(i_hash), len(hash_sql)))
for l, m in hash_sql[:40]: out("   " + fmt(l, m, 40))
for name, pat in [("DISTINCTROW", r"\bDISTINCTROW\b"), ("TRANSFORM", r"\bTRANSFORM\b"),
                  ("PIVOT", r"\bPIVOT\b"), ("PARAMETERS", r"\bPARAMETERS\b")]:
    hh = scan(pat)
    out("%-12s %d" % (name, len(hh)))
    for l, m in hh[:20]: out("   " + fmt(l, m, 40))
amp = [(l,m) for l,m in scan(r"&") if is_sqlish(l.content)]
out("'&' in SQL-verdaechtigen Literalen: %d" % len(amp))
for l, m in amp[:40]: out("   " + fmt(l, m, 45))
out("")

# ---------------------------------------------------------------- j) Abfrage_
out("### j) Abfrage_-Namen in Literalen")
j_rx = re.compile(r"Abfrage_[A-Za-z0-9_]+")
j_count = Counter()
j_files = defaultdict(Counter)
j_locs = defaultdict(list)
for l in all_lits:
    for m in j_rx.finditer(l.content):
        nm = m.group(0)
        j_count[nm] += 1
        j_files[nm][rel(l.file)] += 1
        j_locs[nm].append((rel(l.file), l.line_of(m.start())))
for nm, c in sorted(j_count.items(), key=lambda x: (-x[1], x[0])):
    out("%-45s %4d   %s" % (nm, c, dict(j_files[nm])))
out("")
out("Fundorte der Phantom-/Erwartet-0-Namen:")
for nm in ["Abfrage_Heizkessel_Kosten","Abfrage_KostenKomponenten","Abfrage_Neues_Kosten_Model",
           "Abfrage_ProjektKostenInvestBetrieb","Abfrage_Erzeuger_Vorlauftemperaturen",
           "Abfrage_Max_Vorlauf","Abfrage_Min_Vorlauf","Abfrage_MaxMin_Vorlauf"]:
    out("  %-42s %d  %s" % (nm, j_count.get(nm, 0), sorted(set(j_locs.get(nm, []))) if j_count.get(nm,0) else ""))
out("")

# Rohtext-Gegenprobe (auch ausserhalb von Literalen)
out("### j-Gegenprobe: Abfrage_-Namen im gesamten Quelltext (auch ausserhalb Literale)")
raw_count = Counter()
raw_files = defaultdict(set)
for f in files:
    for m in j_rx.finditer(file_text[f]):
        raw_count[m.group(0)] += 1
        raw_files[m.group(0)].add(rel(f))
for nm, c in sorted(raw_count.items(), key=lambda x: (-x[1], x[0])):
    diff = c - j_count.get(nm, 0)
    out("%-45s roh %4d  literal %4d  diff %d" % (nm, c, j_count.get(nm,0), diff))
out("")

# ---------------------------------------------------------------- k) DELETE <Spalte> FROM
# Befund B2 (S7): "DELETE <feld> FROM <tabelle>" ist Jet-Syntax. ACE verwirft den
# Feldnamen stillschweigend, SQLite bricht mit 'near "<feld>": syntax error' ab.
# Der S5-Sweep hatte das Muster nicht auf seiner Liste; gefunden hat es erst der
# Referenzlauf (S7), weil der Fehler in der Speicher-Transaktion steckte.
# SOLL: 0. Ausgenommen sind die beiden Ausnahmebereiche (SchemaMigration/GeraeteWaisen),
# die weiter Access-Dialekt fahren.
out("### k) DELETE <Spalte> FROM  (Jet-Idiom, Befund B2/S7)")
k_hits = scan(r"\bDELETE\s+(?!FROM\b)[A-Za-z_]\w*\s+FROM\b")
k_lines = sorted(set((l.file, l.line_of(m.start())) for l, m in k_hits))
k_aussen = [(f, ln) for f, ln in k_lines
            if "SchemaMigration.cs" not in rel(f) and "GeraeteWaisen.cs" not in rel(f)]
out("Vorkommen: %d   distinkte Zeilen: %d   davon ausserhalb der Ausnahmebereiche: %d  (SOLL 0)"
    % (len(k_hits), len(k_lines), len(k_aussen)))
for l, m in k_hits:
    out("   " + fmt(l, m, 45))
out("")

# ---------------------------------------------------------------- l) qualifizierte Sichtspalten
# Befund B1 (S7): Ein SQL, das aus einer Sicht (Abfrage_*) liest und darin eine Spalte
# ueber den Namen der ZUGRUNDE LIEGENDEN TABELLE anspricht (Tab_Waermebedarf.ID statt ID).
# Jet loest das auf, SQLite nicht - eine Sicht hat nur ihre eigenen Ausgabespalten
# ("no such column: Tab_Waermebedarf.ID").
# SOLL: 0. Das Rezept misst Literale, die BEIDES enthalten: "FROM Abfrage_<x>" und einen
# "Tab_<y>."-Qualifizierer.
out("### l) tabellenqualifizierte Spalten an einer Sicht (Befund B1/S7)")
l_view = re.compile(r"(?i)\bFROM\s+\[?(Abfrage_[A-Za-z0-9_]+)\]?")
l_qual = re.compile(r"\b(Tab_[A-Za-z0-9_]+|Z_[A-Za-z0-9_]+)\s*\.")
l_treffer = []
for l in all_lits:
    mv = l_view.search(l.content)
    if not mv:
        continue
    for mq in l_qual.finditer(l.content):
        l_treffer.append((l, mv.group(1), mq))
out("Literale mit 'FROM Abfrage_*' UND einem Tabellen-Qualifizierer: %d   distinkte Zeilen: %d  (SOLL 0)"
    % (len(l_treffer), len(set((l.file, l.line_of(mq.start())) for l, _, mq in l_treffer))))
for l, view, mq in l_treffer:
    out("   %s:%d  Sicht %s  qualifiziert mit %s" % (
        rel(l.file), l.line_of(mq.start()), view, mq.group(1)))
out("")

txt = "\n".join(report)
with io.open(os.path.join(OUT, "inventur_report.txt"), "w", encoding="utf-8") as fh:
    fh.write(txt)
sys.stdout.reconfigure(encoding="utf-8")
print(txt)
