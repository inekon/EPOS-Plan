# -*- coding: utf-8 -*-
r"""PRUEFREZEPT 2 - Typ-Rueckweg-Vermessung (Arbeitspaket S5, DB-Migration Access -> SQLite).

ZWECK
  Misst, WIE der Quellbaum Ergebniswerte aus DataTable/DataReader/Scalar konsumiert,
  und findet damit die Stellen, die ein Typwechsel auf dem Rueckweg brechen wuerde.
  Unter Microsoft.Data.Sqlite kaeme roh INTEGER als Int64 und Boolean als 0/1 an;
  die zentrale Typangleichung (D9 in DataRepository.LadeTabelle) stellt Int32,
  Boolean und DateTime wieder her. Dieses Rezept belegt, dass keine Konsumstelle
  mehr auf der harten Cast-Form steht, die das umgehen wuerde.

  Abschnitte:
    A) riskante Direkt-Casts auf DB-Werte ((int)/(bool)/(string)/(double) auf dr[...])
    B) sichere Konsumenten (Convert.ToXxx) als Zaehlwerk
    C) LINQ .Field<T>()          D) Reader-Getter (GetValue/GetString/...)

AUFRUF
  python typ_rueckweg_scan.py                    # Bericht nach stdout
  python typ_rueckweg_scan.py > befund.txt       # Bericht sichern
  Zusaetzlich entsteht typ_rueckweg_ergebnis.json neben dem Skript.

  Rein LESEND, was den Quellbaum angeht.
  Unter Windows-Konsolen empfiehlt sich  PYTHONIOENCODING=utf-8.

SOLLWERTE NACH S5 und die bewussten Ausnahmen: siehe sql/MIGRATION_Pruefrezepte.md.
"""
import io
import json
import os
import re
import sys
from collections import Counter, defaultdict

ROOT = r"C:\Users\DirkEngelmann\Documents\WP-Plan\WindowsFormsApplication1"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "typ_rueckweg_ergebnis.json")

# ----------------------------------------------------------------- Datei-Einlesen
def read_source(path):
    with open(path, "rb") as f:
        raw = f.read()
    for enc in ("utf-8-sig", "utf-8", "cp1252"):
        try:
            return raw.decode(enc), enc
        except UnicodeDecodeError:
            continue
    return raw.decode("cp1252", errors="replace"), "cp1252/replace"


def iter_files():
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d.lower() not in ("bin", "obj", ".vs", ".git")]
        for fn in filenames:
            if fn.lower().endswith(".cs"):
                yield os.path.join(dirpath, fn)


# ----------------------------------------------------------------- C#-Maske (Code / Kommentar / String)
CODE, COMMENT, STRING = 0, 1, 2

def build_mask(t):
    """Zeichenweise Klassifikation, damit Treffer in Kommentaren/Strings ausgefiltert werden.
    Positionen bleiben stabil (keine Ersetzung)."""
    n = len(t)
    m = bytearray(n)
    i = 0
    while i < n:
        c = t[i]
        if c == '/' and i + 1 < n and t[i + 1] == '/':
            j = t.find('\n', i)
            j = n if j < 0 else j
            for k in range(i, j):
                m[k] = COMMENT
            i = j
        elif c == '/' and i + 1 < n and t[i + 1] == '*':
            j = t.find('*/', i + 2)
            j = n if j < 0 else j + 2
            for k in range(i, j):
                m[k] = COMMENT
            i = j
        elif c == '@' and i + 1 < n and t[i + 1] == '"':
            j = i + 2
            while j < n:
                if t[j] == '"':
                    if j + 1 < n and t[j + 1] == '"':
                        j += 2
                        continue
                    j += 1
                    break
                j += 1
            for k in range(i, min(j, n)):
                m[k] = STRING
            i = j
        elif c == '"':
            j = i + 1
            while j < n:
                if t[j] == '\\':
                    j += 2
                    continue
                if t[j] == '"':
                    j += 1
                    break
                if t[j] == '\n':
                    break
                j += 1
            for k in range(i, min(j, n)):
                m[k] = STRING
            i = j
        elif c == "'":
            j = i + 1
            while j < n:
                if t[j] == '\\':
                    j += 2
                    continue
                if t[j] == "'":
                    j += 1
                    break
                if t[j] == '\n':
                    break
                j += 1
            for k in range(i, min(j, n)):
                m[k] = STRING
            i = j
        else:
            i += 1
    return m


def line_starts(t):
    ls = [0]
    for mo in re.finditer('\n', t):
        ls.append(mo.end())
    return ls


def lineno(ls, pos):
    lo, hi = 0, len(ls) - 1
    while lo < hi:
        mid = (lo + hi + 1) // 2
        if ls[mid] <= pos:
            lo = mid
        else:
            hi = mid - 1
    return lo + 1


def line_text(t, ls, ln):
    s = ls[ln - 1]
    e = t.find('\n', s)
    e = len(t) if e < 0 else e
    return t[s:e].strip()


# ----------------------------------------------------------------- Ausdrucks-Scanner
def read_primary(s, i):
    """Liest ab i den 'primaeren' C#-Ausdruck (Ident/./[..]/(..)).
    Liefert (text, ops) mit ops = Liste von ('ident'|'index'|'call', text)."""
    n = len(s)
    start = i
    ops = []
    while i < n:
        c = s[i]
        if c.isalnum() or c == '_':
            j = i
            while j < n and (s[j].isalnum() or s[j] == '_'):
                j += 1
            ops.append(('ident', s[i:j]))
            i = j
        elif c == '.':
            i += 1
        elif c == '?' and i + 1 < n and s[i + 1] == '.':
            i += 2
        elif c == '[':
            d, j = 1, i + 1
            while j < n and d:
                if s[j] == '[':
                    d += 1
                elif s[j] == ']':
                    d -= 1
                j += 1
            if d:
                break
            ops.append(('index', s[i:j]))
            i = j
        elif c == '(':
            d, j = 1, i + 1
            while j < n and d:
                if s[j] == '(':
                    d += 1
                elif s[j] == ')':
                    d -= 1
                j += 1
            if d:
                break
            ops.append(('call', s[i:j]))
            i = j
        elif c in ' \t\r\n':
            # Zeilenumbruch innerhalb der Kette zulassen, sonst Ende
            k = i
            while k < n and s[k] in ' \t\r\n':
                k += 1
            if k < n and (s[k] == '.' or s[k] == '['):
                i = k
            else:
                break
        else:
            break
    return s[start:i], ops


# ----------------------------------------------------------------- DB-Quellen-Erkennung
# Helfer, die object liefern (im Repo verifiziert)
OBJ_HELPERS = (
    "ExecuteScalar", "GetValueById", "SkalarStill", "WertLesenStill", "WertLesen",
    "ColOrNull", "Skalar", "Scalar", "Feld",
)
OBJ_HELPER_RE = re.compile(r"(?:^|[^\w.])(" + "|".join(OBJ_HELPERS) + r")\s*\($")

# Namensheuristik fuer DataRow-/Reader-Variablen (dt/ds bewusst NICHT, die sind Tabelle/Set)
NAME_RE = re.compile(
    r"^(dr|drv|dro|drow|row|rows|r|rw|zeile|zl|reader|rdr|rs|leser|datensatz|dsatz|"
    r"kopf|satz|rec|record|eintrag|treffer|zl1|zl2)$", re.I)
NAME_SUFFIX_RE = re.compile(r"(row|zeile|reader|rdr|satz|datensatz)$", re.I)

DECL_TYPES = r"(?:DataRow|DataRowView|OleDbDataReader|SqliteDataReader|DbDataReader|IDataReader|IDataRecord|SQLiteDataReader)"


DB_SOURCE_RE = re.compile(r"\.Rows\b|\.Select\s*\(|\.AsEnumerable\s*\(|\.DefaultView\b|"
                          r"ExecuteReader\s*\(|GetDataTable\s*\(")


def build_bindings(text):
    """Positionsbehaftete Bindungen: [(pos, name, 'db'|'no')].
    Loest Namenskollisionen (z. B. 'foreach (DataRow t in ...)' vs.
    'foreach (object[] t in <C#-Konstante>)') ueber die naechstliegende Bindung."""
    b = []
    # foreach (TYP name in quelle)
    for mo in re.finditer(r"foreach\s*\(\s*([\w\.\?]+(?:\s*<[^>()]{0,80}>)?(?:\s*\[\s*\])*)\s+"
                          r"(\w+)\s+in\s+([^)\n]{0,220})\)", text):
        typ, name, src = mo.group(1), mo.group(2), mo.group(3)
        base = re.sub(r"[\[\]<>?].*$", "", typ)
        if base in ("DataRow", "DataRowView", "OleDbDataReader", "SqliteDataReader",
                    "DbDataReader", "IDataReader", "IDataRecord") and "[]" not in typ:
            b.append((mo.start(), name, 'db'))
        elif typ == "var" and DB_SOURCE_RE.search(src):
            b.append((mo.start(), name, 'db'))
        else:
            b.append((mo.start(), name, 'no'))
    # Deklaration mit DB-Typ
    for mo in re.finditer(DECL_TYPES + r"\s+(\w+)\s*(?:[=;,)])", text):
        b.append((mo.start(), mo.group(1), 'db'))
    # var/object[] x = ....Rows[..]
    for mo in re.finditer(r"\b(?:var|object\[\]|DataRow)\s+(\w+)\s*=\s*[^;\n]{0,140}?\.Rows\s*\[", text):
        b.append((mo.start(), mo.group(1), 'db'))
    for mo in re.finditer(r"\b(\w+)\s*=\s*[^;\n]{0,160}?\.Rows\s*\[\s*\d", text):
        b.append((mo.start(), mo.group(1), 'db'))
    # Nicht-DB-Deklarationen (Arrays, Listen, Skalare) - blockieren Namensheuristik
    for mo in re.finditer(r"\b(?:object\[\]|string\[\]|double\[\]|float\[\]|int\[\]|decimal\[\]|"
                          r"object\[\]\[\]|List<[^>;\n]{1,60}>|Dictionary<[^>;\n]{1,80}>|"
                          r"double|float|int|string|decimal|bool|long|short)(?:\s*\[\s*\])*\s+"
                          r"(\w+)\s*(?:=|;|,|\))", text):
        b.append((mo.start(), mo.group(1), 'no'))
    b.sort()
    idx = defaultdict(list)
    for pos, name, kind in b:
        idx[name].append((pos, kind))
    return idx


def lookup_binding(idx, name, pos):
    """Naechstliegende Bindung vor pos; sonst die erste danach; sonst None."""
    lst = idx.get(name)
    if not lst:
        return None
    best = None
    for p, k in lst:
        if p <= pos:
            best = k
        else:
            break
    if best is None:
        best = lst[0][1]
    return best


ROWS_CHAIN_RE = re.compile(r"\.Rows\s*\[|\.Rows\s*\.\s*\w+\s*\[|\.DefaultView\s*\[|\.Item\s*\[")


OBJ_TAIL_MEMBERS = {"Value", "ItemArray"}


def classify_source(expr, ops, bind_idx, pos, depth=0):
    """-> (kategorie, quelle_text) mit kategorie in {'db','grid',None}."""
    if not ops:
        return None, ""
    kind, txt = ops[-1]

    # (0) reine Klammerung: (int)(row["x"]) -> Inhalt rekursiv pruefen
    if depth < 2 and len(ops) == 1 and kind == 'call' and expr.strip().startswith('('):
        inner = txt[1:-1].strip()
        iexpr, iops = read_primary(inner, 0)
        if iexpr.strip() == inner:
            return classify_source(iexpr, iops, bind_idx, pos, depth + 1)

    # (a) Tail ist ein Member wie .Value / .ItemArray hinter einem Indexer
    if kind == 'ident' and txt in OBJ_TAIL_MEMBERS and len(ops) >= 2:
        if re.search(r"\.Cells\s*\[", expr):
            return 'grid', "DataGridView-Zelle .Value"
        if ROWS_CHAIN_RE.search(expr) or re.search(r"\.CurrentRow\b|\.SelectedRows\s*\[", expr):
            return 'grid', "Grid-/Row-Value"
        return None, ""

    if kind == 'index':
        # ...Rows[..][..] / dt.Rows[i][j] / dr["X"] / reader["X"]
        if re.search(r"\.Cells\s*\[[^\]]*\]\s*$", expr):
            return 'grid', "DataGridView-Zelle"
        if ROWS_CHAIN_RE.search(expr):
            return 'db', "DataRow (.Rows[..] / .Item[..])"
        # Empfaengerkette unmittelbar vor dem letzten Indexer
        head = expr[:expr.rfind(txt)]
        mo = re.search(r"([A-Za-z_]\w*(?:\s*\.\s*[A-Za-z_]\w*)*)\s*$", head)
        if mo:
            chain = re.sub(r"\s+", "", mo.group(1))
            leaf = chain.split('.')[-1]
            root = chain.split('.')[0]
            if leaf in ("Cells", "Rows", "SelectedRows", "SelectedCells"):
                return 'grid', "Grid"
            if len(chain.split('.')) == 1:
                bk = lookup_binding(bind_idx, leaf, pos)
                if bk == 'db':
                    return 'db', "DataRow/Reader (deklariert)"
                if bk == 'no':
                    return None, ""          # C#-Array/Liste/Skalar - kein DB-Wert
            else:
                if lookup_binding(bind_idx, root, pos) == 'db':
                    return 'db', "DataRow/Reader (deklariert)"
            if NAME_RE.match(leaf) or NAME_SUFFIX_RE.search(leaf):
                return 'db', "DataRow/Reader (Namensheuristik)"
        return None, ""

    if kind == 'call':
        head = expr[:expr.rfind(txt)]
        name = re.split(r"[.\s]", head.strip())[-1] if head.strip() else ""
        if name in OBJ_HELPERS:
            return 'db', "object-Helfer %s()" % name
        return None, ""
    return None, ""


# Handverifizierte Herkunft der Typ-Dispatch-Stellen (Quelltext gelesen, s. Bericht):
# 'db' = geprueft wird ein DataRow-Wert / DataColumn.DataType
# 'ui' = Excel-Zelle, UI-Auswahl oder C#-Konstante -> von der Umstellung NICHT betroffen
J_PROVENIENZ = [
    (r"Katalog.DublettenPruefung\.cs",      'db', "Kanonisch(object v) auf DataRow-Werten"),
    (r"Katalog.KatalogBereinigung\.cs",     'db', "Leerwert(object v) auf DataRow-Werten"),
    (r"Update.SchemaMigration\.cs",         'db', "Leerwert-Helfer auf DataRow-Werten"),
    (r"Update.AnlagenEindeutigkeit\.cs",    'db', "Spaltentyp(DataColumn c) -> OleDbType"),
    (r"KomponentenUebernahmeCtrl\.cs",      'db', "Gleich(object,object) + DataColumn.DataType"),
    (r"MerkmalUebernahmeCtrl\.cs",          'db', "DataColumn.DataType -> OleDbType"),
    (r"ProjektExportImportCtrl\.cs",        'db', "Exportliteral + DataColumn.DataType -> OleDbType"),
    (r"Import.GanglinienDatei\.cs",         'ui', "Excel-Zelle (Value2), keine DB"),
    (r"VdiAuswahlFilter\.cs",               'ui', "UI-Auswahlindizes, keine DB"),
    (r"UcBkKosten\.cs",                     'ui', "Row.Tag = typisierte int-Property"),
    (r"Form_Simulation_Config\.Karten\.cs", 'ui', "Karte.Tag = lokale int-Variable"),
    (r"NavigatorWaerme\.cs",                'ui', "SchluesselEintrag.Wert = DbWerte-Konstante"),
]


def j_provenienz(rel):
    for pat, kind, grund in J_PROVENIENZ:
        if re.search(pat, rel):
            return kind, grund
    return '?', "nicht handgeprueft"


# ----------------------------------------------------------------- A) Direkt-Casts
CAST_TYPES = ["int", "short", "long", "bool", "double", "float", "decimal", "DateTime",
              "string", "byte", "Int16", "Int32", "Int64", "Boolean", "Double", "Single",
              "Decimal", "String", "DateTime?", "int?", "bool?", "double?", "decimal?"]
CAST_RE = re.compile(
    r"\(\s*(int|short|long|bool|double|float|decimal|DateTime|string|byte|sbyte|uint|ulong|ushort|"
    r"Int16|Int32|Int64|Boolean|Double|Single|Decimal|String|DateTime)\s*(\?)?\s*\)\s*")

CRIT = {
    "int": "Int64 -> InvalidCastException", "short": "Int64 -> InvalidCastException",
    "Int32": "Int64 -> InvalidCastException", "Int16": "Int64 -> InvalidCastException",
    "long": "ok (Int64)", "Int64": "ok (Int64)",
    "bool": "Int64 0/1 -> InvalidCastException", "Boolean": "Int64 0/1 -> InvalidCastException",
    "DateTime": "String -> InvalidCastException",
    "decimal": "Double -> InvalidCastException", "Decimal": "Double -> InvalidCastException",
    "float": "Double -> InvalidCastException", "Single": "Double -> InvalidCastException",
    "double": "ok (REAL->Double)", "Double": "ok (REAL->Double)",
    "string": "bricht auf Nicht-TEXT-Spalten", "String": "bricht auf Nicht-TEXT-Spalten",
    "byte": "Int64 -> InvalidCastException",
}

# ----------------------------------------------------------------- Muster B/C/D/E/F
SAFE_PATTERNS = {
    "Convert.ToInt32(": re.compile(r"Convert\.ToInt32\s*\("),
    "Convert.ToInt64(": re.compile(r"Convert\.ToInt64\s*\("),
    "Convert.ToInt16(": re.compile(r"Convert\.ToInt16\s*\("),
    "Convert.ToBoolean(": re.compile(r"Convert\.ToBoolean\s*\("),
    "Convert.ToDouble(": re.compile(r"Convert\.ToDouble\s*\("),
    "Convert.ToSingle(": re.compile(r"Convert\.ToSingle\s*\("),
    "Convert.ToDecimal(": re.compile(r"Convert\.ToDecimal\s*\("),
    "Convert.ToDateTime(": re.compile(r"Convert\.ToDateTime\s*\("),
    "Convert.ToString(": re.compile(r"Convert\.ToString\s*\("),
}
TOSTRING_RE = re.compile(r"\?\s*\.\s*ToString\s*\(\s*\)")
PARSE_RE = re.compile(r"\b(int|double|decimal|bool|DateTime|long|short|float)\s*\.\s*(Parse|TryParse)\s*\(")
FIELD_RE = re.compile(r"\.Field\s*<\s*([A-Za-z_][\w\.\?<>\[\]]*)\s*>\s*\(")
GETTER_RE = re.compile(r"([A-Za-z_]\w*)\s*\.\s*(GetInt32|GetInt16|GetInt64|GetBoolean|GetDateTime|GetDouble|"
                       r"GetString|GetDecimal|GetFloat|GetByte|GetValue|GetChar|GetOrdinal|GetFieldType)\s*\(")
# Empfaenger, die sicher KEIN DbDataReader sind (Ressourcen, Encoding, Registry, Reflection ...)
NON_READER_RE = re.compile(r"^(ResourceManager|resources|Resources|UTF8|ASCII|Unicode|UTF7|UTF32|Encoding|"
                           r"key|Registry\w*|fields?|arr\w*|_?\w*[Dd]ict\w*|Enum|typeof|"
                           r"\w*[Ss]ettings|\w*[Cc]onfig|\w*[Bb]uffer|\w*[Tt]able[Ss]tyle)$")
ISAS_RE = re.compile(r"\b(is|as)\s+(int|short|long|bool|double|float|decimal|DateTime|string|"
                     r"Int32|Int64|Int16|Boolean|Double|Decimal|String)\s*(\?)?\b")
ROWFILTER_RE = re.compile(r"\.RowFilter\s*=")
DTSELECT_RE = re.compile(r"\.Select\s*\(\s*(?:\$?@?\")")
DB_TOUCH_RE = re.compile(r"(?:\.Rows\s*\[|\bdr\s*\[|\brow\s*\[|\breader\s*\[|\brdr\s*\[|\brs\s*\[|"
                         r"\bzeile\s*\[|\bkopf\s*\[|\.Item\s*\[|ExecuteScalar|GetValueById|"
                         r"WertLesen|SkalarStill|ColOrNull|\w*[Rr]ow\s*\[)", re.I)

# Datumsspalten: Endung/Ganzwort-basiert, damit "UPDATE"/"VALIDATE" nicht treffen
DATE_COL_RE = re.compile(
    r"\b(?:"
    r"[A-Za-z_]*(?:datum|zeitstempel|zeitpunkt)|"
    r"(?:geaendert|geändert|erstellt|angelegt|created|modified|changed)_?am|"
    r"(?:geaendert|erstellt|angelegt)Am|"
    r"[A-Za-z_]*_(?:date|time|timestamp)|"
    r"(?:date|timestamp)_[A-Za-z_]+"
    r")\b", re.I)
SQL_KEYWORD_RE = re.compile(r"\b(SELECT|UPDATE|INSERT\s+INTO|DELETE\s+FROM|FROM)\b", re.I)
SQL_NOISE = {"update", "validate", "candidate", "mandate", "aktualisierungsdatum_x"}

# --- I) stille Bruchvektoren (kein Absturz, aber geaendertes Verhalten)
FORMAT_DATE_RE = re.compile(r"\.Format\s*=\s*\"[^\"]*(?:dd|MM|yyyy|HH)[^\"]*\"")
TYPEOF_RE = re.compile(r"typeof\s*\(\s*(DateTime|bool|Boolean|int|Int32|Int64|long|double|decimal|string)\s*\)")
CHECKBOXCOL_RE = re.compile(r"DataGridViewCheckBoxColumn|\.ValueType\s*=|AsCheckBox")
FUNNEL_RE = re.compile(r"new\s+OleDb(?:DataAdapter|Command|Connection)\s*\(")

# --- J) Typ-Dispatch: Code, der auf dem CLR-Laufzeittyp eines DB-Werts verzweigt
TYPDISP_IS_RE = re.compile(r"\bis\s+(DateTime|bool|Boolean|int|Int32|Int64|long|short|Int16|"
                           r"double|Double|decimal|Decimal|float|Single|string|String)\b\s*(?!\s*[\w])")
TYPDISP_TYPEOF_RE = re.compile(r"(==|!=)\s*typeof\s*\(\s*(DateTime|bool|Boolean|int|Int32|Int64|long|"
                               r"short|Int16|double|Double|decimal|Decimal|float|Single|string|String)\s*\)")
# Verhalten nach der Umstellung je gepruefter Typ
DISPATCH_WIRKUNG = {
    "DateTime": "wird NIE mehr wahr (Datum kommt als String)",
    "bool": "wird NIE mehr wahr (Boolean kommt als Int64)",
    "Boolean": "wird NIE mehr wahr (Boolean kommt als Int64)",
    "int": "wird NIE mehr wahr (Long kommt als Int64)",
    "Int32": "wird NIE mehr wahr (Long kommt als Int64)",
    "short": "wird NIE mehr wahr (Integer kommt als Int64)",
    "Int16": "wird NIE mehr wahr (Integer kommt als Int64)",
    "decimal": "wird NIE mehr wahr (kommt als Double)",
    "Decimal": "wird NIE mehr wahr (kommt als Double)",
    "float": "wird NIE mehr wahr (kommt als Double)",
    "Single": "wird NIE mehr wahr (kommt als Double)",
    "long": "wird NEU wahr - faengt jetzt alle Ganzzahlen ab",
    "Int64": "wird NEU wahr - faengt jetzt alle Ganzzahlen ab",
    "string": "wird NEU wahr fuer Datumswerte",
    "String": "wird NEU wahr fuer Datumswerte",
    "double": "unveraendert (REAL -> Double)",
    "Double": "unveraendert (REAL -> Double)",
}

ROWDENSITY_RE = re.compile(r"foreach\s*\(\s*(?:var|DataRow|DataRowView)\s+\w+\s+in\s|\.Rows\s*\[")


def main():
    files = sorted(iter_files())
    enc_stats = Counter()

    a_hits = []          # riskante Direkt-Casts auf DB-Werte
    a_grid_hits = []     # Casts auf DataGridView-Zellen (best effort, oft DataTable-gebunden)
    near_miss = []       # Audit: Cast+Indexer, aber Empfaenger nicht als DB erkannt
    b_counts = Counter()
    b_db_counts = Counter()
    c_field = Counter()
    c_field_hits = []
    d_hits = []
    d_maybe = []
    d_skipped = Counter()
    e_hits = []
    f_hits = []
    g_files = {}
    g_cols = Counter()
    g_art = Counter()
    i_format = []
    i_typeof = []
    i_tostring = 0
    i_funnels = {}
    j_hits = []
    j_typ = Counter()
    h_density = Counter()
    h_lines = Counter()
    total_lines = 0

    for path in files:
        text, enc = read_source(path)
        enc_stats[enc] += 1
        mask = build_mask(text)
        ls = line_starts(text)
        total_lines += len(ls)
        rel = os.path.relpath(path, os.path.dirname(ROOT))
        bind_idx = build_bindings(text)

        def is_code(pos):
            return mask[pos] == CODE

        # ---- A: Casts
        for mo in CAST_RE.finditer(text):
            if not is_code(mo.start()):
                continue
            ctype = mo.group(1)
            nullable = bool(mo.group(2))
            expr, ops = read_primary(text, mo.end())
            if not expr.strip():
                continue
            kat, src = classify_source(expr, ops, bind_idx, mo.start())
            ln = lineno(ls, mo.start())
            entry = {
                "file": rel, "line": ln, "cast": ctype + ("?" if nullable else ""),
                "expr": re.sub(r"\s+", " ", expr.strip())[:150],
                "src": src, "ctx": re.sub(r"\s+", " ", line_text(text, ls, ln))[:190],
            }
            if kat == 'db':
                a_hits.append(entry)
            elif kat == 'grid':
                a_grid_hits.append(entry)
            elif ops and ops[-1][0] == 'index':
                entry["src"] = "verworfen"
                near_miss.append(entry)

        # ---- B: sichere Konsumenten
        for name, rx in SAFE_PATTERNS.items():
            for mo in rx.finditer(text):
                if not is_code(mo.start()):
                    continue
                b_counts[name] += 1
                arg, _ops = read_primary(text, mo.end() - 1)
                if DB_TOUCH_RE.search(arg):
                    b_db_counts[name] += 1
        for mo in TOSTRING_RE.finditer(text):
            if not is_code(mo.start()):
                continue
            b_counts["?.ToString()"] += 1
            ln = lineno(ls, mo.start())
            if DB_TOUCH_RE.search(line_text(text, ls, ln)):
                b_db_counts["?.ToString()"] += 1
        for mo in PARSE_RE.finditer(text):
            if not is_code(mo.start()):
                continue
            key = "%s.%s(" % (mo.group(1), mo.group(2))
            b_counts[key] += 1
            arg, _ops = read_primary(text, mo.end() - 1)
            ln = lineno(ls, mo.start())
            if DB_TOUCH_RE.search(arg) or DB_TOUCH_RE.search(line_text(text, ls, ln)):
                b_db_counts[key] += 1

        # ---- C: LINQ Field<T>
        for mo in FIELD_RE.finditer(text):
            if not is_code(mo.start()):
                continue
            t = mo.group(1)
            c_field[t] += 1
            ln = lineno(ls, mo.start())
            c_field_hits.append({"file": rel, "line": ln, "T": t,
                                 "ctx": re.sub(r"\s+", " ", line_text(text, ls, ln))[:170]})

        # ---- D: Reader-Getter
        for mo in GETTER_RE.finditer(text):
            if not is_code(mo.start()):
                continue
            recv, getter = mo.group(1), mo.group(2)
            if NON_READER_RE.match(recv):
                d_skipped[recv + "." + getter] += 1
                continue
            reader_like = (lookup_binding(bind_idx, recv, mo.start()) == 'db') or bool(re.match(
                r"^(r|rd|rdr|rs|reader|leser|dbreader|DBReader|dr)$", recv, re.I))
            ln = lineno(ls, mo.start())
            rec = {"file": rel, "line": ln, "getter": getter, "recv": recv,
                   "sicher": reader_like,
                   "ctx": re.sub(r"\s+", " ", line_text(text, ls, ln))[:170]}
            if reader_like:
                d_hits.append(rec)
            else:
                d_maybe.append(rec)

        # ---- E: is/as auf Boxwerten
        for mo in ISAS_RE.finditer(text):
            if not is_code(mo.start()):
                continue
            ln = lineno(ls, mo.start())
            lt = line_text(text, ls, ln)
            # Ausdruck unmittelbar VOR dem is/as bestimmen (praeziser Treffer)
            head = text[max(0, mo.start() - 200):mo.start()].rstrip()
            direkt = False
            hm = re.search(r"([A-Za-z_][\w\.]*\s*\[[^\[\]]*\](?:\s*\[[^\[\]]*\])?)\s*$", head)
            if hm:
                hexpr = hm.group(1)
                hops = read_primary(hexpr, 0)[1]
                hk, _hs = classify_source(hexpr, hops, bind_idx, mo.start())
                direkt = (hk == 'db')
            if direkt or DB_TOUCH_RE.search(lt):
                e_hits.append({"file": rel, "line": ln, "op": mo.group(1),
                               "type": mo.group(2) + (mo.group(3) or ""), "direkt": direkt,
                               "ctx": re.sub(r"\s+", " ", lt)[:180]})

        # ---- F: RowFilter / DataTable.Select
        for rx, kind in ((ROWFILTER_RE, "RowFilter"), (DTSELECT_RE, "DataTable.Select")):
            for mo in rx.finditer(text):
                if not is_code(mo.start()):
                    continue
                ln = lineno(ls, mo.start())
                # Ausdruck bis Semikolon/Zeilenende einsammeln (max. 3 Zeilen)
                seg = text[mo.start():mo.start() + 400]
                seg = seg.split(';')[0]
                seg = re.sub(r"\s+", " ", seg).strip()[:230]
                f_hits.append({"file": rel, "line": ln, "kind": kind, "expr": seg})

        # ---- G: Datumsspalten in SQL-Literalen + deren Konsum
        date_cols = set()
        for mo in re.finditer(r"@?\"((?:[^\"\\]|\\.)*)\"", text):
            s = mo.group(1)
            if len(s) < 8 or not SQL_KEYWORD_RE.search(s):
                continue
            for dc in DATE_COL_RE.findall(s):
                if len(dc) > 3 and dc.lower() not in SQL_NOISE:
                    date_cols.add(dc)
                    g_cols[dc] += 1
        # Konsum: Indexer-Zugriffe auf datumsartige Spaltennamen (dateiweit)
        uses = []
        for mo in re.finditer(r"\[\s*\"([^\"]{3,60})\"\s*\]", text):
            col = mo.group(1)
            if not DATE_COL_RE.fullmatch(col) or col.lower() in SQL_NOISE:
                continue
            if not is_code(mo.start()):
                continue
            ln = lineno(ls, mo.start())
            lt = re.sub(r"\s+", " ", line_text(text, ls, ln))
            # wie wird der Wert konsumiert?
            if re.search(r"\(\s*DateTime\s*\)\s*[\w\.]*\[\s*\"" + re.escape(col), lt):
                art = "CAST (DateTime)  -> BRICHT"
            elif re.search(r"Convert\.ToDateTime", lt):
                art = "Convert.ToDateTime -> ok"
            elif re.search(r"Convert\.ToString|\.ToString\s*\(", lt):
                art = "ToString() -> STILLER FORMATWECHSEL"
            elif re.search(r"=\s*[^=]", lt) and re.search(r"\[\s*\"" + re.escape(col) + r"\"\s*\]\s*=", lt):
                art = "SCHREIBEN"
            else:
                art = "sonstiger Zugriff"
            uses.append({"col": col, "line": ln, "art": art, "ctx": lt[:170]})
            g_art[art] += 1
        if date_cols or uses:
            g_files[rel] = {"cols": sorted(date_cols), "uses": uses}

        # ---- I: stille Bruchvektoren
        for mo in FORMAT_DATE_RE.finditer(text):
            if is_code(mo.start()):
                ln = lineno(ls, mo.start())
                i_format.append({"file": rel, "line": ln,
                                 "ctx": re.sub(r"\s+", " ", line_text(text, ls, ln))[:170]})
        for mo in TYPEOF_RE.finditer(text):
            if is_code(mo.start()):
                ln = lineno(ls, mo.start())
                lt = re.sub(r"\s+", " ", line_text(text, ls, ln))
                if re.search(r"Column|DataType|\.Rows|dt\b|dr\b|row", lt, re.I):
                    i_typeof.append({"file": rel, "line": ln, "typ": mo.group(1), "ctx": lt[:170]})
        # ToString() direkt auf einem DB-Indexer (stiller Datums-Formatwechsel)
        for mo in re.finditer(r"([A-Za-z_][\w\.]*\s*\[[^\[\]]{1,80}\])\s*\.\s*ToString\s*\(", text):
            if not is_code(mo.start()):
                continue
            hexpr = mo.group(1)
            hk, _ = classify_source(hexpr, read_primary(hexpr, 0)[1], bind_idx, mo.start())
            if hk == 'db':
                i_tostring += 1
        i_funnels[rel] = len([m for m in FUNNEL_RE.finditer(text) if is_code(m.start())])

        # ---- J: Typ-Dispatch auf dem CLR-Laufzeittyp
        for rx, art in ((TYPDISP_IS_RE, "is T"), (TYPDISP_TYPEOF_RE, "== typeof(T)")):
            for mo in rx.finditer(text):
                if not is_code(mo.start()):
                    continue
                typ = mo.group(2) if art == "== typeof(T)" else mo.group(1)
                ln = lineno(ls, mo.start())
                lt = re.sub(r"\s+", " ", line_text(text, ls, ln))
                prov, grund = j_provenienz(rel)
                j_hits.append({"file": rel, "line": ln, "art": art, "typ": typ, "prov": prov,
                               "grund": grund, "wirkung": DISPATCH_WIRKUNG.get(typ, "?"),
                               "ctx": lt[:165]})
                if prov == 'db':
                    j_typ[typ] += 1

        # ---- H: DataRow-Dichte
        cnt = len([m for m in ROWDENSITY_RE.finditer(text) if is_code(m.start())])
        if cnt:
            h_density[rel] = cnt
            h_lines[rel] = len(ls)

    # ------------------------------------------------------------- Report
    out = sys.stdout
    def p(*a):
        out.write(" ".join(str(x) for x in a) + "\n")

    p("=" * 100)
    p("TYP-RUECKWEG-VERMESSUNG  ACE-OLE-DB -> Microsoft.Data.Sqlite")
    p("Wurzel:", ROOT)
    p("Dateien (.cs, ohne bin/obj):", len(files), " Zeilen gesamt:", total_lines)
    p("Encodings:", dict(enc_stats))
    p("=" * 100)

    # --- A
    p("\n### A) RISKANTE DIREKT-CASTS AUF DB-WERTE  (gesamt: %d)" % len(a_hits))
    by_cast = defaultdict(list)
    for h in a_hits:
        by_cast[h["cast"]].append(h)
    order = ["int", "int?", "short", "bool", "bool?", "DateTime", "DateTime?", "decimal",
             "float", "byte", "long", "double", "double?", "string"]
    keys = [k for k in order if k in by_cast] + sorted(k for k in by_cast if k not in order)
    for k in keys:
        hs = sorted(by_cast[k], key=lambda x: (x["file"], x["line"]))
        p("\n--- Cast (%s)  n=%d   [%s]" % (k, len(hs), CRIT.get(k.rstrip('?'), "?")))
        for h in hs:
            p("  %s:%d  %s   |  %s" % (h["file"], h["line"], h["expr"], h["ctx"]))
    p("\n--- Anhang A2: Casts auf DataGridView-Zellen (oft DataTable-gebunden)  n=%d" % len(a_grid_hits))
    for h in sorted(a_grid_hits, key=lambda x: (x["file"], x["line"])):
        p("  (%s) %s:%d  %s" % (h["cast"], h["file"], h["line"], h["expr"]))

    # --- B
    p("\n\n### B) SICHERE KONSUMENTEN (Zaehlwerte)")
    p("  %-24s %8s %14s" % ("Muster", "gesamt", "davon DB-nah"))
    for k in sorted(b_counts, key=lambda x: -b_counts[x]):
        p("  %-24s %8d %14d" % (k, b_counts[k], b_db_counts.get(k, 0)))
    p("  %-24s %8d %14d" % ("SUMME", sum(b_counts.values()), sum(b_db_counts.values())))

    # --- C
    p("\n\n### C) LINQ .Field<T>(  (gesamt: %d)" % sum(c_field.values()))
    if not c_field:
        p("  KEINE Fundstellen.")
    for t in sorted(c_field, key=lambda x: -c_field[x]):
        p("  Field<%s>: %d" % (t, c_field[t]))
    for h in c_field_hits:
        if re.match(r"(int|bool|DateTime|Int32|Int64|Boolean)", h["T"]):
            p("  %s:%d  Field<%s>  |  %s" % (h["file"], h["line"], h["T"], h["ctx"]))

    # --- D
    p("\n\n### D) READER-GETTER  (gesamt: %d)" % len(d_hits))
    if not d_hits:
        p("  KEINE Fundstellen.")
    for h in sorted(d_hits, key=lambda x: (x["file"], x["line"])):
        p("  %s:%d  %s  |  %s" % (h["file"], h["line"], h["getter"], h["ctx"]))

    # --- E
    p("\n\n### E) TYPPRUEFUNGEN is/as AUF BOXWERTEN  (gesamt: %d)" % len(e_hits))
    if not e_hits:
        p("  KEINE Fundstellen.")
    p("  davon direkt auf einem DB-Indexer-Ausdruck: %d" % sum(1 for h in e_hits if h["direkt"]))
    for h in sorted(e_hits, key=lambda x: (not x["direkt"], x["file"], x["line"])):
        p("  %s %s:%d  %s %s  |  %s" % ("[DIREKT]" if h["direkt"] else "[ Zeile]",
                                        h["file"], h["line"], h["op"], h["type"], h["ctx"]))

    # --- F
    p("\n\n### F) DATATABLE-AUSDRUCKSFILTER  (gesamt: %d)" % len(f_hits))
    for h in sorted(f_hits, key=lambda x: (x["file"], x["line"])):
        flags = []
        ex = h["expr"]
        if re.search(r"#|Datum|Date|geaendert|erstellt|Zeitstempel", ex, re.I):
            flags.append("DATUM?")
        if re.search(r"=\s*(true|false)|<>\s*(true|false)|\bTrue\b|\bFalse\b|=\s*[01]\b", ex, re.I):
            flags.append("BOOL?")
        if re.search(r"\bLIKE\b", ex, re.I):
            flags.append("LIKE")
        if re.search(r"Convert\s*\(", ex, re.I):
            flags.append("Convert()")
        p("  %s:%d  [%s] %s\n      %s" % (h["file"], h["line"], h["kind"],
                                          ("<<" + ",".join(flags) + ">>") if flags else "", ex))

    # --- G
    p("\n\n### G) DATUMSSPALTEN IN SQL-LITERALEN + deren Konsum")
    p("  Datumsartige Spaltennamen in SQL-Literalen (Nennungen):")
    for c, v in g_cols.most_common(40):
        p("      %-34s %d" % (c, v))
    p("  Konsumarten der Datumsspalten-Indexer (gesamt %d):" % sum(g_art.values()))
    for a, v in g_art.most_common():
        p("      %-40s %d" % (a, v))
    p("  Fundstellen:")
    for rel in sorted(g_files):
        info = g_files[rel]
        if not info["uses"]:
            continue
        p("  %s   [SQL-Spalten: %s]" % (rel, ", ".join(info["cols"]) or "-"))
        for u in info["uses"]:
            p("      :%-6d %-34s %-34s %s" % (u["line"], '["' + u["col"] + '"]', u["art"], u["ctx"][:90]))

    p("\n\n### I) STILLE BRUCHVEKTOREN (kein Absturz, geaendertes Verhalten)")
    p("  ToString() direkt auf DB-Indexer (Datum -> ISO statt dd.MM.yyyy): %d" % i_tostring)
    p("  Zellen-/Spalten-Formatstrings mit Datumsmuster: %d" % len(i_format))
    for h in i_format[:25]:
        p("      %s:%d  %s" % (h["file"], h["line"], h["ctx"][:120]))
    p("  typeof(T)-Pruefungen auf Spalten-/Zeilentypen: %d" % len(i_typeof))
    for h in i_typeof[:25]:
        p("      %s:%d  typeof(%s)  %s" % (h["file"], h["line"], h["typ"], h["ctx"][:110]))
    nf = {k: v for k, v in i_funnels.items() if v}
    p("  Dateien mit eigenem 'new OleDb...' (Zugriffs-Trichter neben DataRepository): %d "
      "(%d Vorkommen)" % (len(nf), sum(nf.values())))
    for k, v in sorted(nf.items(), key=lambda x: -x[1])[:15]:
        p("      %-70s %d" % (k[:70], v))

    jdb = [h for h in j_hits if h["prov"] == 'db']
    jui = [h for h in j_hits if h["prov"] == 'ui']
    jun = [h for h in j_hits if h["prov"] == '?']
    p("\n\n### J) TYP-DISPATCH auf dem CLR-Laufzeittyp  (Rohtreffer: %d)" % len(j_hits))
    p("  Herkunft handgeprueft:  DB-Werte: %d | Excel/UI/Konstante (nicht betroffen): %d | ungeprueft: %d"
      % (len(jdb), len(jui), len(jun)))
    p("  Verteilung der DB-Stellen nach geprueftem Typ:")
    for t, v in j_typ.most_common():
        p("      %-10s %3d   %s" % (t, v, DISPATCH_WIRKUNG.get(t, "?")))
    krit = [h for h in jdb if h["wirkung"].startswith("wird NIE")]
    p("  --> DB-Zweige, die nach der Umstellung nie mehr erreicht werden: %d" % len(krit))
    p("\n  -- DB-BEZUG (echte Bruchstellen) --")
    for h in sorted(jdb, key=lambda x: (not x["wirkung"].startswith("wird NIE"), x["file"], x["line"])):
        flag = "!!" if h["wirkung"].startswith("wird NIE") else ("~ " if h["wirkung"].startswith("wird NEU") else "  ")
        p("  %s %s:%d  [%s %s]  %s" % (flag, h["file"], h["line"], h["art"], h["typ"], h["ctx"]))
    p("\n  -- OHNE DB-BEZUG (handgeprueft, NICHT betroffen) --")
    seen_ui = set()
    for h in sorted(jui, key=lambda x: x["file"]):
        if h["file"] not in seen_ui:
            seen_ui.add(h["file"])
            n = sum(1 for x in jui if x["file"] == h["file"])
            p("     %-64s %2d Stellen  (%s)" % (h["file"][-64:], n, h["grund"]))
    if jun:
        p("\n  -- NICHT HANDGEPRUEFT --")
        for h in jun:
            p("     %s:%d  [%s %s]  %s" % (h["file"], h["line"], h["art"], h["typ"], h["ctx"][:110]))

    # --- H
    p("\n\n### H) TOP-10 DATEIEN NACH DATAROW-KONSUM-DICHTE")
    p("  %-72s %7s %7s %8s" % ("Datei", "Treffer", "Zeilen", "je 1000Z"))
    for rel, cnt in h_density.most_common(15):
        p("  %-72s %7d %7d %8.1f" % (rel[:72], cnt, h_lines[rel], 1000.0 * cnt / max(1, h_lines[rel])))
    p("  GESAMT DataRow-Konsumstellen: %d in %d Dateien" % (sum(h_density.values()), len(h_density)))

    p("\n\n### AUDIT) Cast + Indexer, Empfaenger NICHT als DB erkannt  (n=%d)" % len(near_miss))
    nm = Counter()
    for h in near_miss:
        nm[re.sub(r"\[.*", "[", h["expr"])] += 1
    for k, v in nm.most_common(50):
        p("  %4d  %s" % (v, k))

    with open(OUT, "w", encoding="utf-8") as f:
        json.dump({"A": a_hits, "A2": a_grid_hits, "B": dict(b_counts), "B_db": dict(b_db_counts),
                   "C": dict(c_field), "D": d_hits, "E": e_hits, "F": f_hits,
                   "G": g_files, "J": j_hits, "H": h_density.most_common(30), "A_nearmiss": near_miss, "D_maybe": d_maybe}, f, ensure_ascii=False, indent=1)
    p("\nJSON:", OUT)


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
    main()
