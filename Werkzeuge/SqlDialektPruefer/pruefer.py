#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""SQL-Dialekt-Pruefer: haelt alle SQL-Texte des Quellbestands gegen SQLite.

Angelegt am 03.09.2026 nach zwei Access-Altlasten, die erst unter SQLite auffielen,
weil ihre Pfade selten laufen: ``id_ENERGIETRAEGER`` gegen die Spalte
``ID_Energietraeger`` (SQLite faltet Gross/Klein nur bei ASCII, c288e1c) und ein
``UPDATE ... INNER JOIN ... SET`` in Access-Schreibweise (dd4113f). Der Referenzlauf
deckt nur den Rechenweg ab - Dialog- und Pflegepfade hat bis dahin niemand geprueft.

Aufruf
------
    python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite

    --alle        auch die fehlerfreien und die dynamischen Texte auflisten
    --dynamisch   nur die Texte, deren Objekte erst zur Laufzeit feststehen
    --csv DATEI   die vollstaendige Liste als CSV ablegen
    --selbsttest  nur die Regeln gegen die eingebauten Beispiele halten

Rueckgabewert 1, sobald eine Fundstelle bleibt - daran haengt der CI-Schritt.

Wie geprueft wird
-----------------
1.  Jede C#-Datei der Baeume EPOS.Kern und WindowsFormsApplication1 wird in Token
    zerlegt (der Access-Zweig der Erststart-Migration bleibt aussen vor, siehe
    AUSGENOMMEN). Die Zeichenketten einer Verkettung werden wieder zusammengesetzt -
    ueber ``+``, ueber ``sql += ...`` und ueber ``sb.Append(...).Append(...)``.
2.  ``const string``-Konstanten werden AUFGELOEST (SchemaKatalog.TAB_*, SPALTE_*,
    Controller-TABLE, SchemaStand.SQL_*). Ein Kurzname zaehlt nur, wenn es ihn in
    genau EINER Klasse gibt - sonst zoege "TABLE" die Tabelle einer fremden Klasse
    herein. Die Vereinbarung selbst ist ein Baustein; geprueft wird die VERWENDUNG.
3.  Der fertige Text geht als ``EXPLAIN`` an eine nur lesend geoeffnete
    Testdatenbank. Das faengt Syntax UND Objekte ("no such column: ...").
4.  Bleibt eine Luecke (Tabellen- oder Spaltenname entsteht erst zur Laufzeit),
    wird sie nacheinander mit ``0``, einem Bezeichner und leer belegt. Besteht eine
    Belegung, liegt es nicht an der Syntax; der Text gilt als "dynamisch" und die
    Musterregeln sind fuer ihn das Netz. Nennt SQLite dagegen einen Namen, der
    WOERTLICH im Quelltext steht, ist auch ein dynamischer Text falsch.
5.  Musterregeln laufen unabhaengig vom EXPLAIN, in zwei Klassen:
      leise - SQLite nimmt es klaglos an und tut etwas anderes als Access
              (``&`` als Verkettung, ``LIKE 'x*'``): IMMER gemeldet.
      laut  - SQLite bricht ab (UPDATE ... JOIN, Nz, TOP n, #Datum#, Left/Mid,
              Val, CDbl, IIf-Verwandte, ALTER COLUMN, ADD CONSTRAINT ...): nur
              gemeldet, wo EXPLAIN nicht abschliessend urteilen konnte.
    ``= True`` / ``= False`` schlaegt nur an, wenn die verglichene Spalte in der
    Testdatenbank etwas anderes als 0/1/NULL fuehrt - SQLite kennt TRUE seit 3.23
    als Alias von 1, Access fuehrte WAHR als -1.
6.  Bezeichner mit Nicht-ASCII werden BUCHSTABENGETREU gegen das Schema gehalten.
"""

from __future__ import annotations

import argparse
import os
import re
import sqlite3
import sys

LOCH = "\x01"          # Interpolationsloch  $"...{x}..."
UNBEK = "\x02"         # nicht aufloesbarer Verkettungsteil


# =====================================================================================
# 1. Lexer
# =====================================================================================

def tokenize(text):
    """Zerlegt C#-Quelltext in (Art, Wert, Zeile). Art: str|ident|num|chr|op."""
    toks = []
    i = 0
    n = len(text)
    zeile = 1
    while i < n:
        c = text[i]
        if c == "\n":
            zeile += 1
            i += 1
            continue
        if c in " \t\r\f\v":
            i += 1
            continue
        if c == "/" and i + 1 < n:
            if text[i + 1] == "/":
                j = text.find("\n", i)
                i = n if j < 0 else j
                continue
            if text[i + 1] == "*":
                j = text.find("*/", i + 2)
                j = n if j < 0 else j + 2
                zeile += text.count("\n", i, j)
                i = j
                continue
        if c in '@$"':
            m = re.match(r'[@$]{0,2}"', text[i:])
            if m:
                praefix = m.group(0)
                start = zeile
                wert, i2, zeilen = _lies_string(
                    text, i + len(praefix), "@" in praefix, "$" in praefix)
                zeile += zeilen
                toks.append(("str", wert, start))
                i = i2
                continue
        if c == "'":
            j = i + 1
            while j < n:
                if text[j] == "\\":
                    j += 2
                    continue
                if text[j] == "'":
                    j += 1
                    break
                j += 1
            toks.append(("chr", text[i:j], zeile))
            i = j
            continue
        if c.isalpha() or c == "_":
            j = i
            while j < n and (text[j].isalnum() or text[j] == "_"):
                j += 1
            toks.append(("ident", text[i:j], zeile))
            i = j
            continue
        if c.isdigit():
            j = i
            while j < n and (text[j].isalnum()
                             or (text[j] == "." and j + 1 < n and text[j + 1].isdigit())):
                j += 1
            toks.append(("num", text[i:j], zeile))
            i = j
            continue
        toks.append(("op", c, zeile))
        i += 1
    return toks


def _lies_string(text, i, verbatim, interp):
    n = len(text)
    out = []
    zeilen = 0
    while i < n:
        c = text[i]
        if c == "\n":
            zeilen += 1
        if verbatim:
            if c == '"':
                if i + 1 < n and text[i + 1] == '"':
                    out.append('"')
                    i += 2
                    continue
                return "".join(out), i + 1, zeilen
        else:
            if c == "\\" and i + 1 < n:
                e = text[i + 1]
                out.append({"n": "\n", "t": "\t", "r": "\r", "0": "\0",
                            '"': '"', "\\": "\\", "'": "'"}.get(e, e))
                i += 2
                continue
            if c == '"':
                return "".join(out), i + 1, zeilen
            if c == "\n":
                return "".join(out), i, zeilen
        if interp and c == "{":
            if i + 1 < n and text[i + 1] == "{":
                out.append("{")
                i += 2
                continue
            j, zz = _ueberspringe_loch(text, i)
            zeilen += zz
            out.append(LOCH)
            i = j
            continue
        if interp and c == "}" and i + 1 < n and text[i + 1] == "}":
            out.append("}")
            i += 2
            continue
        out.append(c)
        i += 1
    return "".join(out), i, zeilen


def _ueberspringe_loch(text, i):
    n = len(text)
    tiefe = 0
    zeilen = 0
    while i < n:
        c = text[i]
        if c == "\n":
            zeilen += 1
        if c == "{":
            tiefe += 1
            i += 1
            continue
        if c == "}":
            tiefe -= 1
            i += 1
            if tiefe == 0:
                return i, zeilen
            continue
        if c == '"':
            _, i, zz = _lies_string(text, i + 1, False, False)
            zeilen += zz
            continue
        if c == "'":
            i += 1
            while i < n and text[i] != "'":
                i += 2 if text[i] == "\\" else 1
            i += 1
            continue
        i += 1
    return i, zeilen


# =====================================================================================
# 2. Ausdruecke: Verkettungsketten lesen
# =====================================================================================

KLAMMER_ZU = {"(": ")", "[": "]", "{": "}"}


def _gruppe(toks, i):
    """i zeigt auf eine oeffnende Klammer; gibt den Index HINTER der schliessenden."""
    auf = toks[i][1]
    zu = KLAMMER_ZU[auf]
    tiefe = 0
    n = len(toks)
    while i < n:
        a, w, _ = toks[i]
        if a == "op" and w == auf:
            tiefe += 1
        elif a == "op" and w == zu:
            tiefe -= 1
            if tiefe == 0:
                return i + 1
        i += 1
    return n


def _operand(toks, i):
    """Liest einen Primaerausdruck. Gibt (art, text, neuer_index) oder None.

    art ist 'lit' (Zeichenkette) oder 'expr' (alles andere; text = Quelltext).
    """
    n = len(toks)
    if i >= n:
        return None
    a, w, _ = toks[i]
    if a == "str":
        return ("lit", w, i + 1)
    if a == "op" and w == "(":
        j = _gruppe(toks, i)
        j = _postfix(toks, j)
        return ("expr", _quelltext(toks, i, j), j)
    if a == "ident" or a == "num" or (a == "op" and w in "-!"):
        j = i
        if a == "op":
            j += 1
            if j >= n:
                return None
        j += 1
        j = _postfix(toks, j)
        return ("expr", _quelltext(toks, i, j), j)
    return None


def _postfix(toks, i):
    n = len(toks)
    while i < n:
        a, w, _ = toks[i]
        if a == "op" and w == "." and i + 1 < n and toks[i + 1][0] == "ident":
            i += 2
            continue
        if a == "op" and w in "([":
            i = _gruppe(toks, i)
            continue
        if a == "op" and w == "?" and i + 1 < n and toks[i + 1] == ("op", ".", toks[i + 1][2]):
            i += 1
            continue
        break
    return i


def _quelltext(toks, i, j):
    out = []
    for a, w, _ in toks[i:j]:
        out.append('"%s"' % w if a == "str" else w)
    return "".join(out)


def _kette(toks, i):
    """Liest eine +-Kette ab i. Gibt (teile, startindizes, neuer_index) oder None."""
    r = _operand(toks, i)
    if r is None:
        return None
    teile = [(r[0], r[1])]
    starts = [i]
    j = r[2]
    n = len(toks)
    while j < n and toks[j][0] == "op" and toks[j][1] == "+":
        r = _operand(toks, j + 1)
        if r is None:
            break
        starts.append(j)
        starts.append(j + 1)
        teile.append((r[0], r[1]))
        j = r[2]
    return teile, starts, j


# =====================================================================================
# 3. Konstanten einsammeln
# =====================================================================================

def sammle_konstanten(dateien):
    """Baut {Name -> Wert} und {Klasse.Name -> Wert} aus const/readonly string."""
    roh = {}       # (klasse, name) -> teile
    for pfad in dateien:
        text = _lies(pfad)
        toks = tokenize(text)
        klasse = ""
        stack = []
        n = len(toks)
        i = 0
        while i < n:
            a, w, _ = toks[i]
            if a == "op" and w == "{":
                stack.append(klasse)
            elif a == "op" and w == "}":
                klasse = stack.pop() if stack else ""
            elif a == "ident" and w in ("class", "struct", "record") and i + 1 < n \
                    and toks[i + 1][0] == "ident":
                klasse = toks[i + 1][1]
            elif a == "ident" and w in ("const", "readonly") and i + 1 < n \
                    and toks[i + 1] [0] == "ident" and toks[i + 1][1] == "string" \
                    and i + 2 < n and toks[i + 2][0] == "ident" \
                    and i + 3 < n and toks[i + 3] == ("op", "=", toks[i + 3][2]):
                name = toks[i + 2][1]
                k = _kette(toks, i + 4)
                if k:
                    roh[(klasse, name)] = k[0]
                    i = k[2]
                    continue
            i += 1
    # Aufloesen in mehreren Runden - Konstanten zeigen auf Konstanten.
    # WICHTIG: der Kurzname gilt nur, wenn er in genau EINER Klasse vorkommt.
    # Sonst zoege "TABLE" die Tabelle einer fremden Klasse herein.
    je_kurzname = {}
    for (kl, na) in roh:
        je_kurzname.setdefault(na, set()).add(kl)

    class Katalog(object):
        def __init__(self):
            self.lang = {}      # "Klasse.Name" -> Text
            self.kurz = {}      # "Name" -> Text (nur eindeutige)

    kat = Katalog()
    for runde in range(6):
        aenderung = False
        for (kl, na), teile in roh.items():
            txt = _verkette(teile, kat, kl, tief=True)
            if txt is None:
                continue
            schl = "%s.%s" % (kl, na)
            if kat.lang.get(schl) != txt:
                kat.lang[schl] = txt
                aenderung = True
            if len(je_kurzname.get(na, ())) == 1 and kat.kurz.get(na) != txt:
                kat.kurz[na] = txt
                aenderung = True
        if not aenderung:
            break
    return kat


def _verkette(teile, kat, klasse, tief=False, lokal=None):
    out = []
    for art, txt in teile:
        if art == "lit":
            out.append(txt)
        else:
            w = _konstante(txt, kat, klasse, lokal)
            if w is None:
                if tief:
                    return None
                out.append(UNBEK)
            else:
                out.append(w)
    return "".join(out)


NAME_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")


def _konstante(ausdruck, kat, klasse, lokal=None):
    """Loest einen Ausdruck zu einem Konstantentext auf - oder gibt None.

    ``lokal`` sind die Namen, die IN DIESER DATEI als gewoehnliche Variable
    vereinbart sind (``string felder = ...``). Sie duerfen nicht ueber den
    Kurznamen aus einer fremden Klasse aufgeloest werden: Was hier ``felder``
    heisst, ist der Inhalt der lokalen Variablen und nicht die gleichnamige
    Konstante irgendwo sonst im Bestand (Befund iU9-W6.7 - zwei falsche
    Fundstellen in WirtschaftlichkeitCtrl, sobald die zweite Vereinbarung des
    Namens mit einer geloeschten Maske verschwand und der Kurzname damit
    "eindeutig" wurde).
    """
    a = ausdruck.strip()
    if NAME_RE.match(a):
        teile = a.split(".")
        if len(teile) == 1:
            if lokal and teile[0] in lokal:
                return None
            # Bezug innerhalb der eigenen Klasse geht vor.
            if klasse and ("%s.%s" % (klasse, teile[0])) in kat.lang:
                return kat.lang["%s.%s" % (klasse, teile[0])]
            return kat.kurz.get(teile[0])
        # A.B / A.B.C: die letzten beiden Glieder sind Klasse.Name
        schl = ".".join(teile[-2:])
        if schl in kat.lang:
            return kat.lang[schl]
        return kat.kurz.get(teile[-1])
    m = re.match(r"^nameof\(([A-Za-z0-9_.]+)\)$", a)
    if m:
        return m.group(1).split(".")[-1]
    return None


# =====================================================================================
# 4. SQL aus einer Datei ziehen
# =====================================================================================

# Nur was auch wirklich nach SQL aussieht - "Delete(int id) verwenden ..." ist kein SQL.
SQL_START = re.compile(
    r"^\s*("
    r"SELECT\s+[\w\[(*]"
    r"|INSERT\s+(INTO|OR)\b"
    r"|REPLACE\s+INTO\b"
    r"|UPDATE\s+[\w\[]"
    r"|DELETE\s+FROM\b"
    r"|CREATE\s+(TEMP\s+|TEMPORARY\s+|UNIQUE\s+)?(TABLE|INDEX|VIEW|TRIGGER)\b"
    r"|ALTER\s+TABLE\b"
    r"|DROP\s+(TABLE|INDEX|VIEW|TRIGGER)\b"
    r"|PRAGMA\s+\w"
    r"|WITH\s+\w+\s+AS\s*\("
    r")", re.I)

def klassen_karte(toks):
    """Je Tokenindex die umschliessende Klasse (fuer Konstanten der eigenen Klasse)."""
    n = len(toks)
    karte = [""] * n
    klasse = ""
    stack = []
    for i in range(n):
        a, w, _ = toks[i]
        if a == "op" and w == "{":
            stack.append(klasse)
        elif a == "op" and w == "}":
            klasse = stack.pop() if stack else ""
        elif a == "ident" and w in ("class", "struct", "record") and i + 1 < n \
                and toks[i + 1][0] == "ident":
            klasse = toks[i + 1][1]
        karte[i] = klasse
    return karte


def ziehe_sql(pfad, kat):
    """Gibt Liste von (zeile, sql_text, startindex, endindex, toks)."""
    text = _lies(pfad)
    toks = tokenize(text)
    karte = klassen_karte(toks)
    n = len(toks)
    konst_rhs = _konstanten_rhs(toks)
    lokal = _lokale_namen(toks)

    def aufloesen(teile, start):
        return _verkette(teile, kat, karte[start] if start < n else "", lokal=lokal)

    ketten = {}          # startindex -> (teile, ende, zeile, text)
    verboten = set()
    i = 0
    while i < n:
        if i in verboten:
            i += 1
            continue
        k = _kette(toks, i)
        if not k:
            i += 1
            continue
        teile, starts, ende = k
        if any(a == "lit" for a, _ in teile):
            txt = aufloesen(teile, i)
            if not SQL_START.match(txt):
                # fuehrende, nicht aufloesbare Teile abschneiden und erneut sehen
                rest = teile
                while rest and rest[0][0] != "lit":
                    rest = rest[1:]
                txt2 = aufloesen(rest, i) if rest else ""
                if rest and SQL_START.match(txt2):
                    teile, txt = rest, txt2
                else:
                    i += 1
                    continue
            ketten[i] = (teile, ende, toks[i][2], txt)
            verboten.update(starts)
        elif all(a == "expr" and NAME_RE.match(t.strip()) for a, t in teile) \
                and not (i > 0 and toks[i - 1][0] == "op" and toks[i - 1][1] == ".") \
                and not (ende < n and toks[ende][0] == "op" and toks[ende][1] == "="):
            # Reiner Konstantenbezug: DataRepository.ExecuteSQL(SchemaStand.SQL_FK_...)
            # Die Vereinbarung selbst ist ein Baustein; GEPRUEFT wird die Verwendung.
            # Der Name LINKS eines "=" ist die Vereinbarung, keine Verwendung.
            txt = aufloesen(teile, i)
            if txt and UNBEK not in txt and SQL_START.match(txt):
                ketten[i] = (teile, ende, toks[i][2], txt)
                verboten.update(range(i, ende))
        i += 1

    # Zusammengesetzte Texte: string sql = "..."; sql += " ..."; sb.Append("...")
    episoden = _episoden(toks, ketten, aufloesen, konst_rhs)

    gefunden = []
    for start, (teile, ende, zeile, txt) in sorted(ketten.items()):
        if start in episoden["unterdrueckt"] or start in konst_rhs:
            # Die rechte Seite einer const/readonly-Vereinbarung ist ein BAUSTEIN.
            # Geprueft wird sie an jeder Verwendungsstelle - dort ist sie vollstaendig.
            continue
        gefunden.append((zeile, txt, start, ende))

    for zeile, teile, start, ende in episoden["treffer"]:
        gefunden.append((zeile, aufloesen(teile, start), start, ende))

    gefunden.sort(key=lambda t: t[0])
    aufgeloest = []
    for zeile, roh, start, ende in gefunden:
        sql = FORMATLOCH.sub(UNBEK, roh)      # string.Format-Platzhalter {0}
        aufgeloest.append((zeile, sql, start, ende, toks))
    return aufgeloest, toks, text


FORMATLOCH = re.compile(r"\{\d+(?::[^}]*)?\}")


def _lokale_namen(toks):
    """Namen, die in dieser Datei als gewoehnliche `string`-Variable vereinbart sind.

    Erfasst `string x = ...` und `var x = ...` OHNE `const`/`readonly` davor - also
    genau die Faelle, in denen der Name fuer diese Datei etwas anderes bedeutet als
    eine gleichnamige Konstante anderswo.
    """
    raus = set()
    n = len(toks)
    for i in range(n - 3):
        if toks[i][0] != "ident" or toks[i][1] not in ("string", "var"):
            continue
        if i > 0 and toks[i - 1][0] == "ident" and toks[i - 1][1] in ("const", "readonly"):
            continue
        if toks[i + 1][0] == "ident" and toks[i + 2][0] == "op" and toks[i + 2][1] == "=" \
                and not (i + 3 < n and toks[i + 3][0] == "op" and toks[i + 3][1] == "="):
            raus.add(toks[i + 1][1])
    return raus


def _konstanten_rhs(toks):
    """Startindizes der rechten Seiten von `const/readonly string X = ...`."""
    raus = set()
    n = len(toks)
    for i in range(n - 4):
        if toks[i][0] == "ident" and toks[i][1] in ("const", "readonly") \
                and toks[i + 1][0] == "ident" and toks[i + 1][1] == "string" \
                and toks[i + 2][0] == "ident" \
                and toks[i + 3][0] == "op" and toks[i + 3][1] == "=":
            raus.add(i + 4)
    return raus


ANHAENGER = ("Append", "AppendLine", "AppendFormat", "Insert")


def _episoden(toks, ketten, aufloesen, konst_rhs):
    """Setzt SQL zusammen, das ueber mehrere Anweisungen entsteht.

    Erfasst drei Bauweisen des Bestands:
      string sql = "SELECT ..."; sql += " WHERE ...";
      sql = sql + " AND ...";
      sb.Append("SELECT ...").Append(x).Append(" FROM ...");
    """
    n = len(toks)
    treffer = []
    unterdrueckt = set()
    offen = {}     # Name -> [teile, zeile, startindex, endindex]
    i = 0
    while i < n:
        a, w, _ = toks[i]

        # IDENT = <kette>    (Neubelegung)
        if a == "ident" and i + 1 < n and toks[i + 1][0] == "op" and toks[i + 1][1] == "=" \
                and not (i + 2 < n and toks[i + 2][0] == "op" and toks[i + 2][1] == "="):
            name = w
            k = ketten.get(i + 2)
            if k and (i + 2) in konst_rhs:
                # const string x = "SELECT ..." ist ein Baustein, keine Anweisung.
                i = k[1]
                continue
            if k:
                if name in offen:
                    e = offen.pop(name)
                    treffer.append((e[1], e[0], e[2], e[3]))
                offen[name] = [list(k[0]), k[2], i + 2, k[1]]
                unterdrueckt.add(i + 2)
                i = k[1]
                continue
            kk = _kette(toks, i + 2)
            if kk and name in offen and kk[0] and kk[0][0][0] == "expr" \
                    and kk[0][0][1].strip() == name:
                offen[name][0] += list(kk[0][1:])
                offen[name][3] = kk[2]
                unterdrueckt.add(i + 2)
                i = kk[2]
                continue

        # IDENT += <kette>
        if a == "ident" and i + 2 < n and toks[i + 1][0] == "op" and toks[i + 1][1] == "+" \
                and toks[i + 2][0] == "op" and toks[i + 2][1] == "=":
            name = w
            kk = _kette(toks, i + 3)
            if kk and name in offen:
                offen[name][0] += list(kk[0])
                offen[name][3] = kk[2]
                unterdrueckt.add(i + 3)
                i = kk[2]
                continue

        # IDENT.Append(<kette>).Append(<kette>) ...
        if a == "ident" and i + 3 < n and toks[i + 1][0] == "op" and toks[i + 1][1] == "." \
                and toks[i + 2][0] == "ident" and toks[i + 2][1] in ANHAENGER \
                and toks[i + 3][0] == "op" and toks[i + 3][1] == "(":
            name = w
            j = i + 3
            stuecke = []
            while j < n and toks[j][0] == "op" and toks[j][1] == "(":
                zu = _gruppe(toks, j)
                kk = _kette(toks, j + 1)
                if kk and kk[2] <= zu:
                    stuecke += list(kk[0])
                    unterdrueckt.add(j + 1)
                j = zu
                if j + 2 < n and toks[j][0] == "op" and toks[j][1] == "." \
                        and toks[j + 1][0] == "ident" and toks[j + 1][1] in ANHAENGER \
                        and toks[j + 2][0] == "op" and toks[j + 2][1] == "(":
                    j += 2
                    continue
                break
            if stuecke:
                if name in offen:
                    offen[name][0] += stuecke
                    offen[name][3] = j
                elif SQL_START.match(aufloesen(stuecke, i)):
                    offen[name] = [stuecke, toks[i][2], i, j]
                i = j
                continue

        i += 1

    for name, e in offen.items():
        treffer.append((e[1], e[0], e[2], e[3]))
    return {"treffer": treffer, "unterdrueckt": unterdrueckt}


def _roh(teile):
    return [(a, t) for a, t in teile]


# =====================================================================================
# 5. Pruefungen
# =====================================================================================

# -------------------------------------------------------------------------------------
# Musterregeln.
#
# "leise": SQLite nimmt die Anweisung KLAGLOS an und tut etwas anderes als Access.
#          Solche Stellen faengt kein EXPLAIN - sie werden IMMER gemeldet.
# "laut":  SQLite bricht ab. EXPLAIN faengt sie, sobald der Text vollstaendig
#          aufloesbar ist. Gemeldet werden sie nur bei Texten, die sich NICHT
#          vollstaendig aufloesen lassen (dynamische Tabellen-/Spaltennamen) -
#          dort ist die Musterregel das einzige Netz.
# -------------------------------------------------------------------------------------

MUSTER_LEISE = [
    ("& als Verkettung", re.compile(r"(?<![&\x01\x02])&(?![&])")),
    ("LIKE mit * statt %", re.compile(r"\bLIKE\b[^,)]{0,80}?[*]", re.I)),
    ("Access-Platzhalter ?", re.compile(r"\bLIKE\s*'[^']*\?[^']*'", re.I)),
]

MUSTER_LAUT = [
    ("UPDATE ... JOIN", re.compile(
        r"^\s*UPDATE\b(?:(?!\bSET\b)[\s\S])*?\b(INNER|LEFT|RIGHT|CROSS)?\s*JOIN\b", re.I)),
    ("DELETE ... JOIN", re.compile(
        r"^\s*DELETE\b(?:(?!\bWHERE\b)[\s\S])*?\bJOIN\b", re.I)),
    ("Nz(",             re.compile(r"\bNz\s*\(", re.I)),
    ("DISTINCTROW",     re.compile(r"\bDISTINCTROW\b", re.I)),
    ("TOP n",           re.compile(r"\bSELECT\s+(?:DISTINCT\s+)?TOP\s+\d", re.I)),
    ("Datum #..#",      re.compile(r"#\s*\d{1,4}[-/.]\d{1,2}[-/.]\d{1,4}[^#]*#")),
    ("Now/Date/Time()", re.compile(r"\b(Now|Date|Time)\s*\(\s*\)", re.I)),
    ("Year/Month/Day(", re.compile(
        r"\b(Year|Month|Day|Weekday|Hour|Minute|Second)\s*\(", re.I)),
    ("DateAdd/Diff/Part", re.compile(
        r"\b(DateAdd|DateDiff|DatePart|DateSerial|DateValue)\s*\(", re.I)),
    ("CDbl/CInt/CStr(", re.compile(r"\bC(Dbl|Int|Lng|Str|Date|Bool|Cur|Sng|Var)\s*\(", re.I)),
    ("Val(",            re.compile(r"\bVal\s*\(", re.I)),
    ("Str(",            re.compile(r"(?<![A-Za-z0-9_.])Str\s*\(", re.I)),
    ("Left/Right/Mid(", re.compile(r"\b(Left|Right|Mid)\s*\(", re.I)),
    ("UCase/LCase(",    re.compile(r"\b(UCase|LCase)\s*\(", re.I)),
    ("IsNull(",         re.compile(r"\bIsNull\s*\(", re.I)),
    ("Int(",            re.compile(r"(?<![A-Za-z0-9_.])Int\s*\(", re.I)),
    ("Switch/Choose(",  re.compile(r"\b(Switch|Choose)\s*\(", re.I)),
    ("First/Last(",     re.compile(r"\b(First|Last)\s*\(", re.I)),
    ("TRANSFORM/PIVOT", re.compile(r"\b(TRANSFORM|PIVOT)\b", re.I)),
    ("SELECT ... INTO", re.compile(r"^\s*SELECT\b(?:(?!\bFROM\b)[\s\S])*?\bINTO\s+\[?\w", re.I)),
    ("ALTER COLUMN",    re.compile(r"\bALTER\s+COLUMN\b", re.I)),
    ("ADD CONSTRAINT",  re.compile(r"\bADD\s+CONSTRAINT\b", re.I)),
    ("@@IDENTITY",      re.compile(r"@@IDENTITY", re.I)),
    ("WITH OWNERACCESS", re.compile(r"\bWITH\s+OWNERACCESS\b", re.I)),
    ("Rnd(",            re.compile(r"\bRnd\s*\(", re.I)),
    ("StrComp/StrConv(", re.compile(r"\b(StrComp|StrConv)\s*\(", re.I)),
    ("Expr1000 (Access-Aliasname)", re.compile(r"\bExpr\d{3,4}\b")),
]

# = True / = False ist in SQLite ab 3.23 gueltig - aber NUR als Alias fuer 1 bzw. 0.
# Access fuehrte WAHR als -1. Die Regel schlaegt deshalb erst an, wenn die
# verglichene Spalte in der Testdatenbank etwas anderes als 0/1/NULL enthaelt.
WAHRHEIT_RE = re.compile(
    r"(?:\[?(\w+)\]?\.)?\[?(\w+)\]?\s*(?:=|<>|!=)\s*(True|False)\b", re.I)


def pruefe_muster(sql, aufloesbar, bool_ausnahmen):
    """Gibt Liste (Regelname, Fundtext). aufloesbar=False -> auch die 'lauten' Regeln."""
    hits = []
    for name, rx in MUSTER_LEISE:
        m = rx.search(sql)
        if m:
            hits.append((name, m.group(0)[:60]))
    if not aufloesbar:
        for name, rx in MUSTER_LAUT:
            m = rx.search(sql)
            if m:
                hits.append((name, m.group(0)[:60]))
    for m in WAHRHEIT_RE.finditer(sql):
        spalte = m.group(2)
        if spalte.lower() in bool_ausnahmen:
            hits.append(("= True/False auf Nicht-0/1-Spalte", m.group(0)[:60]))
    return hits


BEZ_RE = re.compile(r"\[([^\]\[]+)\]|([A-Za-z_À-ɏ][A-Za-z0-9_À-ɏ]*)")


def pruefe_umlaute(sql, schema_namen, schema_klein):
    """Bezeichner mit Nicht-ASCII muessen buchstabengetreu im Schema stehen."""
    schlecht = []
    for m in BEZ_RE.finditer(sql):
        name = m.group(1) or m.group(2)
        if all(ord(c) < 128 for c in name):
            continue
        if name in schema_namen:
            continue
        k = name.lower()
        if k in schema_klein:
            schlecht.append((name, sorted(schema_klein[k])))
    return schlecht


def explain(conn, sql):
    """Gibt None bei Erfolg, sonst die Fehlermeldung."""
    teile = [t.strip() for t in _split_stmts(sql) if t.strip()]
    if not teile:
        return "leer"
    for t in teile:
        fehler = _explain_eins(conn, t)
        if fehler:
            return fehler
    return None


BINDUNG_RE = re.compile(r"current statement uses (\d+)")


def _explain_eins(conn, t, nachschlag=True):
    try:
        conn.execute("EXPLAIN " + t)
        return None
    except sqlite3.Error as ex:
        msg = str(ex)
        if "already exists" in msg or "duplicate column name" in msg:
            return None
        # EXPLAIN fuehrt nichts aus, verlangt aber die richtige Zahl an Bindungen.
        m = BINDUNG_RE.search(msg)
        if m and nachschlag:
            try:
                conn.execute("EXPLAIN " + t, [None] * int(m.group(1)))
                return None
            except sqlite3.Error as ex2:
                msg2 = str(ex2)
                if "already exists" in msg2 or "duplicate column name" in msg2:
                    return None
                return msg2
            except Exception as ex2:
                return type(ex2).__name__ + ": " + str(ex2)
        return msg
    except Exception as ex:            # z.B. ValueError bei NUL-Zeichen
        return type(ex).__name__ + ": " + str(ex)


def _split_stmts(sql):
    out = []
    akt = []
    inq = None
    for c in sql:
        if inq:
            akt.append(c)
            if c == inq:
                inq = None
            continue
        if c in "'\"":
            inq = c
            akt.append(c)
            continue
        if c == ";":
            out.append("".join(akt))
            akt = []
            continue
        akt.append(c)
    out.append("".join(akt))
    return out


SYNTAXWORT = ("syntax error", "unrecognized token", "incomplete input",
              "malformed", "expected")


def ist_syntax(fehler):
    return fehler is not None and any(w in fehler for w in SYNTAXWORT)


FUELLUNGEN = ("0", "zzdyn", "")


def vorbereite(sql, fuellung="0"):
    """Macht aus dem gezogenen Text eine fuer EXPLAIN taugliche Anweisung.

    Nicht aufloesbare Verkettungsteile werden mit ``fuellung`` belegt: "0" fuer
    eine Wertstelle, ein Bezeichner fuer eine Namensstelle, leer fuer einen
    Satzteil, der zur Laufzeit ganz entfaellt. Geprueft wird mit allen dreien -
    besteht EINE Belegung die Syntaxpruefung, liegt es nicht an der Syntax.

    An der Nahtstelle wird ein Leerzeichen gesetzt, wo sonst zwei Woerter
    zusammenwuechsen (``...KomponentenID<dyn>FROM``) - aber nicht hinter einem
    Punkt, einer Klammer oder einem Anfuehrungszeichen.
    """
    s = re.sub(r"@[A-Za-z_][A-Za-z0-9_]*", "?", sql)      # @name -> ?
    out = []
    for i, c in enumerate(s):
        if c not in (LOCH, UNBEK):
            out.append(c)
            continue
        vor = out[-1][-1:] if out and out[-1] else ""
        nach = s[i + 1] if i + 1 < len(s) else ""
        vorn = "" if vor in ("", "'", '"', "[", "(", ".", "@") else " "
        hint = "" if nach in ("", "'", '"', "]", ")", ".", ",", ";") else " "
        out.append(vorn + fuellung + hint if fuellung else " ")
    return "".join(out)


# =====================================================================================
# 6. Hauptlauf
# =====================================================================================

AUSGENOMMEN = (
    "Allgemein/Update/SchemaMigration.cs",
    "Allgemein/Update/GeraeteWaisen.cs",
    "Allgemein/Update/ErststartMigration.cs",
    "Allgemein/Update/SchemaVersionAccess.cs",
    "Allgemein/DbParamOleDb.cs",
)

WURZELN = ("EPOS.Kern", "WindowsFormsApplication1")


def dateien_im_bereich(basis):
    out = []
    for wurzel in WURZELN:
        for dp, dn, fn in os.walk(os.path.join(basis, wurzel)):
            dn[:] = [d for d in dn if d not in ("obj", "bin", ".vs", ".claude")]
            for f in fn:
                if not f.endswith(".cs"):
                    continue
                p = os.path.join(dp, f)
                rel = os.path.relpath(p, basis).replace(os.sep, "/")
                if any(rel.endswith(a) for a in AUSGENOMMEN):
                    continue
                out.append(p)
    return sorted(out)


def alle_cs(basis):
    out = []
    for dp, dn, fn in os.walk(basis):
        # .claude: Worktrees paralleler Agenten - fremde Arbeitsstaende, die sich waehrend des
        # Laufs bewegen (Gate 06.09.2026: FileNotFoundError mitten im Scan).
        dn[:] = [d for d in dn if d not in ("obj", "bin", ".vs", ".git", "artifacts", ".claude")]
        for f in fn:
            if f.endswith(".cs"):
                out.append(os.path.join(dp, f))
    return sorted(out)


def _lies(pfad):
    with open(pfad, "rb") as f:
        b = f.read()
    if b.startswith(b"\xef\xbb\xbf"):
        b = b[3:]
    return b.decode("utf-8", "replace")


def schema_namen(conn):
    namen = set()
    klein = {}
    for (t,) in conn.execute(
            "SELECT name FROM sqlite_master WHERE type IN ('table','view')").fetchall():
        namen.add(t)
        for r in conn.execute('PRAGMA table_info("%s")' % t.replace('"', '""')):
            namen.add(r[1])
    for nm in namen:
        klein.setdefault(nm.lower(), set()).add(nm)
    return namen, klein


def nicht_01_spalten(conn):
    """Spalten (klein geschrieben), die etwas anderes als 0/1/NULL enthalten.

    Nur fuer sie ist ``= True`` in SQLite eine Falle: TRUE ist dort der Alias
    von 1, Access fuehrte WAHR als -1.
    """
    schlecht = set()
    tabellen = [r[0] for r in conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table'")]
    for t in tabellen:
        for r in conn.execute('PRAGMA table_info("%s")' % t.replace('"', '""')):
            sp = r[1]
            typ = (r[2] or "").upper()
            if "INT" not in typ and "BOOL" not in typ and typ != "":
                continue
            try:
                treffer = conn.execute(
                    'SELECT 1 FROM "%s" WHERE "%s" NOT IN (0,1) AND "%s" IS NOT NULL LIMIT 1'
                    % (t.replace('"', '""'), sp.replace('"', '""'), sp.replace('"', '""'))
                ).fetchone()
            except sqlite3.Error:
                continue
            if treffer:
                schlecht.add(sp.lower())
    return schlecht


# =====================================================================================
# 7. Selbsttest
#
# Ein Pruefer, der nichts findet, ist erst dann eine gute Nachricht, wenn er
# BEWEISEN kann, dass er etwas finden WUERDE. Die Liste enthaelt je Regel eine
# Anweisung, die auffallen muss, und daneben Anweisungen, die durchgehen muessen.
# =====================================================================================

MUSS_AUFFALLEN = [
    "UPDATE Tab_ProjektWerte AS w INNER JOIN Tab_Energieanlagen AS a "
    "ON w.ID_Anlage = a.ID SET w.ID_AnlageGeraet = a.ID_WP WHERE w.ID = 1",
    "DELETE Tab_ProjektWerte.* FROM Tab_ProjektWerte INNER JOIN Tab_Projekt "
    "ON Tab_ProjektWerte.ProjektID = Tab_Projekt.ID WHERE Tab_Projekt.ID = 1",
    "SELECT Nz(Projektname, '') FROM Tab_Projekt",
    "SELECT DISTINCTROW Projektname FROM Tab_Projekt",
    "SELECT TOP 5 Projektname FROM Tab_Projekt",
    "SELECT * FROM Tab_Projekt WHERE Datum = #2026-01-31#",
    "SELECT Projektname & ' (' & Ort & ')' FROM Tab_Projekt",
    "SELECT * FROM Tab_Projekt WHERE Projektname LIKE 'Haus*'",
    "SELECT Left(Projektname, 3) FROM Tab_Projekt",
    "SELECT Mid(Projektname, 2, 3) FROM Tab_Projekt",
    "SELECT UCase(Projektname) FROM Tab_Projekt",
    "SELECT IsNull(Projektname) FROM Tab_Projekt",
    "SELECT CDbl(ID) FROM Tab_Projekt",
    "SELECT Val(ID) FROM Tab_Projekt",
    "SELECT Year(Now()) FROM Tab_Projekt",
    "SELECT DateAdd('d', 1, Datum) FROM Tab_Projekt",
    "ALTER TABLE Tab_Projekt ADD CONSTRAINT FK_X FOREIGN KEY (ID) REFERENCES Tab_Projekt (ID)",
    "ALTER TABLE Tab_Projekt ALTER COLUMN Projektname TEXT(50)",
    "SELECT * FROM (SELECT COUNT(*) FROM Tab_Projekt) AS T WHERE T.Expr1000 = 0",
    "SELECT * INTO Tab_Kopie FROM Tab_Projekt",
    "SELECT ID_ENERGIETRÄGER FROM energy_project_settings",
]

DARF_DURCHGEHEN = [
    "SELECT ID, Projektname FROM Tab_Projekt WHERE ID = ?",
    "SELECT date('now'), strftime('%Y', 'now') FROM Tab_Projekt",
    "UPDATE Tab_ProjektWerte SET ID_AnlageGeraet = "
    "(SELECT a.ID_WP FROM Tab_Energieanlagen AS a WHERE a.ID = Tab_ProjektWerte.ID_Anlage) "
    "WHERE Tab_ProjektWerte.ID = ?",
    "SELECT IIF(ist_aktiv, 0, 1) FROM emissionswert",
    "SELECT COALESCE(Projektname, '') FROM Tab_Projekt",
    "SELECT substr(Projektname, 1, 3) FROM Tab_Projekt",
    "SELECT Projektname || ' x' FROM Tab_Projekt",
    "SELECT * FROM Tab_Projekt WHERE Projektname LIKE 'Haus%'",
    "SELECT Projektname FROM Tab_Projekt LIMIT 5",
    "SELECT * FROM energy_project_settings WHERE ID_Energieträger = ?",
    "SELECT * FROM Tab_Kostenfaktor WHERE IsMainComponent = True",
]


def selbsttest(conn, namen, klein, bool_ausnahmen):
    fehler = 0
    for sql in MUSS_AUFFALLEN:
        f = explain(conn, vorbereite(sql, "0"))
        m = pruefe_muster(sql, f is None, bool_ausnahmen)
        u = pruefe_umlaute(sql, namen, klein)
        if not (m or u or f):
            print("SELBSTTEST FEHLT: %s" % _kurz(sql, 90))
            fehler += 1
    for sql in DARF_DURCHGEHEN:
        f = explain(conn, vorbereite(sql, "0"))
        m = pruefe_muster(sql, f is None, bool_ausnahmen)
        u = pruefe_umlaute(sql, namen, klein)
        if m or u or f:
            print("SELBSTTEST FALSCHALARM: %s -> %s %s %s" % (_kurz(sql, 90), m, u, f))
            fehler += 1
    print("Selbsttest: %d Anweisungen, %d Abweichungen."
          % (len(MUSS_AUFFALLEN) + len(DARF_DURCHGEHEN), fehler))
    return fehler



def main():
    ap = argparse.ArgumentParser(
        description="Haelt alle SQL-Texte des Quellbestands gegen SQLite.")
    ap.add_argument("--db", required=True, help="Testdatenbank (wird nur gelesen)")
    ap.add_argument("--basis", default=".", help="Wurzel des Arbeitsbaums")
    ap.add_argument("--alle", action="store_true",
                    help="auch die fehlerfreien und die dynamischen Texte zeigen")
    ap.add_argument("--dynamisch", action="store_true",
                    help="nur die nicht aufloesbaren Texte auflisten")
    ap.add_argument("--csv", help="vollstaendige Liste als CSV ablegen")
    ap.add_argument("--selbsttest", action="store_true",
                    help="nur die Regeln gegen eingebaute Beispiele halten")
    args = ap.parse_args()

    basis = os.path.abspath(args.basis)
    conn = sqlite3.connect("file:%s?mode=ro" % os.path.abspath(args.db), uri=True)
    namen, klein = schema_namen(conn)
    bool_ausnahmen = nicht_01_spalten(conn)

    if args.selbsttest:
        return 1 if selbsttest(conn, namen, klein, bool_ausnahmen) else 0

    kat = sammle_konstanten(alle_cs(basis))

    zeilen = []
    anzahl = 0
    for pfad in dateien_im_bereich(basis):
        rel = os.path.relpath(pfad, basis).replace(os.sep, "/")
        try:
            treffer, toks, text = ziehe_sql(pfad, kat)
        except Exception as ex:
            print("FEHLER beim Lesen von %s: %s" % (rel, ex), file=sys.stderr)
            continue
        for zeile, sql, start, ende, _ in treffer:
            anzahl += 1
            dyn = (UNBEK in sql) or (LOCH in sql)

            fehler = explain(conn, vorbereite(sql, "0"))
            if fehler and dyn:
                # Andere Belegungen der Luecken versuchen, bevor geurteilt wird.
                for f in FUELLUNGEN[1:]:
                    zweit = explain(conn, vorbereite(sql, f))
                    if zweit is None or not ist_syntax(zweit):
                        fehler = zweit
                        break
            # Der Text gilt als vollstaendig geprueft, wenn EXPLAIN ihn annimmt -
            # dann sind Syntax UND Objekte bestaetigt und die "lauten" Musterregeln
            # haetten nichts mehr zu melden.
            geprueft = fehler is None

            muster = pruefe_muster(sql, geprueft, bool_ausnahmen)
            umlaute = pruefe_umlaute(sql, namen, klein)

            grund = []
            if muster:
                grund.append("MUSTER " + "; ".join("%s [%s]" % (a, b) for a, b in muster))
            if umlaute:
                grund.append("UMLAUT " + "; ".join(
                    "%s -> Schema schreibt %s" % (a, "/".join(b)) for a, b in umlaute))
            if fehler and not dyn:
                grund.append(("SYNTAX " if ist_syntax(fehler) else "OBJEKT ") + fehler)
            elif fehler and dyn and _objekt_im_text(fehler, sql) \
                    and not _spaltenliste_ohne_tabelle(fehler, sql):
                # Der bemaengelte Name steht WOERTLICH im Quelltext - er stammt also
                # nicht aus einer Luecke. Auch ein dynamischer Text ist dann falsch.
                # AUSNAHME: eine SELECT-Spaltenliste, deren FROM erst spaeter
                # angehaengt wird - dort scheitert ohne Tabellenbezug jeder
                # Spaltenname, auch der richtige (siehe _spaltenliste_ohne_tabelle).
                grund.append("OBJEKT " + fehler)

            if grund:
                art = "FUND"
            elif fehler:
                # Tabellen-/Spaltenname entsteht erst zur Laufzeit: EXPLAIN kann den
                # Text nicht abschliessend beurteilen. Die Musterregeln oben sind
                # fuer diese Stellen das Netz.
                art = "dynamisch"
            else:
                art = "OK"
            zeilen.append((art, rel, zeile, sql, " | ".join(grund)))

    zeilen.sort(key=lambda z: (z[1], z[2]))
    fund = [z for z in zeilen if z[0] == "FUND"]
    dynamisch = [z for z in zeilen if z[0] == "dynamisch"]

    if args.dynamisch:
        for art, rel, zeile, sql, info in dynamisch:
            print("%s:%d  %s" % (rel, zeile, _kurz(sql)))
    else:
        for art, rel, zeile, sql, info in zeilen:
            if art == "FUND" or args.alle:
                print("%-9s %s:%d\n          %s\n          -> %s"
                      % (art, rel, zeile, _kurz(sql), info))

    print("\n%d SQL-Texte geprueft: %d Fundstellen, %d dynamisch (Syntax geprueft, "
          "Objekte erst zur Laufzeit bekannt), %d in Ordnung."
          % (anzahl, len(fund), len(dynamisch), anzahl - len(fund) - len(dynamisch)))

    if args.csv:
        import csv
        with open(args.csv, "w", newline="", encoding="utf-8") as f:
            w = csv.writer(f, delimiter=";")
            w.writerow(["Art", "Datei", "Zeile", "SQL", "Befund"])
            for z in zeilen:
                w.writerow([z[0], z[1], z[2], re.sub(r"\s+", " ", z[3]).strip(), z[4]])
    return 1 if fund else 0



OBJEKT_RE = re.compile(
    r"no such (?:table|column): (?:main\.)?(\S+)|table (\S+) has no column named (\S+)")

SPALTE_FEHLT_RE = re.compile(r"no such column: ", re.I)
SELECT_OHNE_FROM_RE = re.compile(r"(?is)^\s*SELECT\b(?:(?!\bFROM\b).)*$")


def _spaltenliste_ohne_tabelle(fehler, sql):
    """
    Ist der Text eine SELECT-SPALTENLISTE, deren ``FROM`` erst spaeter angehaengt wird?

    Dann kann EXPLAIN ueber die Spalten NICHTS sagen: Ohne Tabellenbezug scheitert
    jeder Spaltenname, auch der richtige - die Meldung sagt etwas ueber den
    Ausschnitt, nicht ueber den Quelltext.

    Der Fall entsteht, wenn der Rumpf einer Anweisung in einer Schleife waechst und
    das ``FROM`` in einer ANDEREN Anweisung dazukommt. Einziger Vertreter im Bestand
    ist ``WizardCtrl.FachspaltenSelect``: Die Spaltenliste entsteht aus
    ``DataRepository.SpaltenVonTabelle("Tab_Energieanlagen")``, und erst das
    ``return`` haengt ``FROM Tab_Energieanlagen WHERE ID_Projekt = ?`` an. Der Leser
    dieses Pruefers sieht davon nur die Liste.

    Eng gehalten: Es greift NUR bei fehlender SPALTE (eine fehlende TABELLE bliebe
    ein Fund) und nur, solange im Text ueberhaupt kein ``FROM`` steht. Ein Text mit
    ``FROM`` behaelt seinen Tabellenbezug und wird weiter voll beurteilt.
    """
    if not SPALTE_FEHLT_RE.search(fehler or ""):
        return False
    return SELECT_OHNE_FROM_RE.match(sql or "") is not None


def _objekt_im_text(fehler, sql):
    """Steht der bemaengelte Name woertlich im Quelltext (statt in einer Luecke)?"""
    m = OBJEKT_RE.search(fehler or "")
    if not m:
        return False
    name = m.group(1) or m.group(3)
    if not name:
        return False
    name = name.split(".")[-1]
    if name in ("0", "zzdyn", "zzdynzzdyn"):
        return False
    roh = sql.replace(LOCH, "\x00").replace(UNBEK, "\x00")
    return re.search(r"(?<![A-Za-z0-9_])" + re.escape(name) + r"(?![A-Za-z0-9_])", roh) is not None


def _kurz(sql, n=220):
    k = re.sub(r"\s+", " ", sql.replace(LOCH, "{…}").replace(UNBEK, "‹dyn›")).strip()
    return k if len(k) <= n else k[:n] + " ..."


if __name__ == "__main__":
    sys.exit(main())
