#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Probe fuer sql/tools/Reduziere-Testdatenbank.sql.

Baut eine synthetische Datenbank aus dem Zielschema (sql/schema/001..003),
fuellt sie mit Projekt- und Katalogdaten, laesst das Reduzierungsskript darauf
laufen und prueft das Ergebnis. Die Probe ersetzt den Windows-Nachweis nicht -
sie sichert nur zu, dass das Skript syntaktisch laeuft, die richtigen Zeilen
trifft, keine Katalogzeile verliert, keine Waisen hinterlaesst und beim zweiten
Lauf nichts mehr aendert.

Aufruf (Linux/macOS/Windows, nur Standardbibliothek):

    python3 sql/tools/Reduziere-Testdatenbank.probe.py

Voraussetzung: sqlite3 >= 3.37 (STRICT-Tabellen). Rueckgabewert 0 = alles gut,
1 = mindestens eine Pruefung fehlgeschlagen.
"""

import os
import sqlite3
import sys
import tempfile

HIER = os.path.dirname(os.path.abspath(__file__))
WURZEL = os.path.dirname(os.path.dirname(HIER))          # Repo-Wurzel
SCHEMA = os.path.join(WURZEL, "sql", "schema")
SKRIPT = os.path.join(HIER, "Reduziere-Testdatenbank.sql")

# Die dreizehn Referenzprojekte (Referenzlaeufe/LIESMICH.md, Basis B3-Kaskade)
BEHALTEN = [1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030, 1039, 1040, 1041, 1042]
# Drei Projekte, die verschwinden muessen
WEG = [1043, 1044, 2000]
ALLE = sorted(BEHALTEN + WEG)

MIN_SQLITE = (3, 37, 0)

fehler = []
hinweise = []


def pruefe(bedingung, text):
    if bedingung:
        print("  ok    %s" % text)
    else:
        print("  FEHL  %s" % text)
        fehler.append(text)


# ---------------------------------------------------------------------------
# 1. Leere Datenbank aus dem Zielschema
# ---------------------------------------------------------------------------
def baue_schema(pfad):
    con = sqlite3.connect(pfad, isolation_level=None)
    for datei in ("001_grundschema.sql", "002_views.sql", "003_indizes_fk.sql"):
        with open(os.path.join(SCHEMA, datei), encoding="utf-8") as fh:
            con.executescript(fh.read())
    con.commit()
    return con


# ---------------------------------------------------------------------------
# 2. Synthetische Daten
# ---------------------------------------------------------------------------
# Fuenf der neunzehn Tabellen mit FOREIGN KEY ... ON DELETE CASCADE auf
# Tab_Projekt. Sie muessen allein durch DELETE FROM Tab_Projekt verschwinden.
# Dritter Eintrag: Zeilen je Projekt. Tab_Einstellungen traegt einen UNIQUE-Index
# auf ID_Projekt und vertraegt deshalb nur eine Zeile je Projekt.
FK_TABELLEN = [
    ("Tab_Einstellungen", "ID_Projekt", 1),
    ("Tab_Energieanlagen", "ID_Projekt", 2),
    ("Tab_Kostenprofil", "ID_Projekt", 2),
    ("Tab_ProjektWerte", "ProjektID", 2),     # abweichender Spaltenname
    ("Z_ProjektStromganglinie", "ID_Projekt", 2),
]

# Acht der 28 Tabellen mit ID_Projekt ohne Fremdschluessel. Drei davon sind
# Katalogtabellen mit Projektkopien: sie bekommen zusaetzlich Zeilen mit
# ID_Projekt = 0 bzw. NULL, die vollstaendig erhalten bleiben muessen.
NOFK_TABELLEN = [
    "Tab_Brauchwassertyp",       # Katalogmarker 0 (NOT NULL DEFAULT 0)
    "Tab_BHKW",                  # Katalogmarker NULL (kein DEFAULT)
    "Tab_Heizkessel",            # Katalogmarker 0 (NOT NULL, kein DEFAULT)
    "Tab_Ergebnis",
    "Tab_WP",
    "Tab_Waermebedarf",
    "Tab_Stromganglinie",
    "Tab_Klimadaten",
]

# Tabellen mit Projektspalte, die absichtlich Fremdprojekt-Werte behalten duerfen:
#   Tab_Kenndaten_Kuehlung_STAMM - Auslieferungskatalog, wird nicht angefasst
#   Tab_Applikation              - wird auf 1030 umgehaengt, eigene Pruefung
AUSGENOMMEN = ["Tab_Kenndaten_Kuehlung_STAMM", "Tab_Applikation"]

# Alle uebrigen Tabellen mit Projektspalte - dagegen laeuft die Gegenprobe.
# Pruefung 0 vergleicht diese Liste gegen das Schema, damit sie nicht veraltet.
ALLE_PROJEKTSPALTEN = [
    ("Tab_Einstellungen", "ID_Projekt"), ("Tab_Energieanlagen", "ID_Projekt"),
    ("Tab_Klimaregion", "ID_Projekt"), ("Tab_Kostenprofil", "ID_Projekt"),
    ("Tab_Preisreihe", "ID_Projekt"), ("Tab_ProjektTarif", "ID_Projekt"),
    ("Tab_ProjektWerte", "ProjektID"), ("Tab_ProjektWirtschaftlichkeit", "ID_Projekt"),
    ("Tab_Pufferspeicher", "ID_Projekt"), ("Z_ProjektGebaeude", "ID_Projekt"),
    ("Z_ProjektPufferSp", "ID_Projekt"), ("Z_ProjektSolarganglinie", "ID_Projekt"),
    ("Z_ProjektStromganglinie", "ID_Projekt"), ("Z_ProjektWaermebedarf", "ID_Projekt"),
    ("Z_Projekt_Brauchwasser", "ID_Projekt"), ("Z_Projekt_Prozesswaerme", "ID_Projekt"),
    ("Z_Projekt_Stromverbraucher", "ID_Projekt"), ("energy_price", "ID_Projekt"),
    ("energy_project_settings", "ID_Projekt"),
    ("Tab_BHKW", "ID_Projekt"), ("Tab_Brauchwasser", "ID_Projekt"),
    ("Tab_Brauchwassertyp", "ID_Projekt"), ("Tab_Ergebnis", "ID_Projekt"),
    ("Tab_ErgebnisStromMatrix", "ID_Projekt"), ("Tab_ErgebnisWirtSensitivitaet", "ID_Projekt"),
    ("Tab_ErgebnisWirtschaftlichkeit", "ID_Projekt"), ("Tab_Gebaeude", "ID_Projekt"),
    ("Tab_Heizkessel", "ID_Projekt"), ("Tab_Kenndaten", "ID_Projekt"),
    ("Tab_Klimadaten", "ID_Projekt"), ("Tab_PV", "ID_Projekt"),
    ("Tab_ProjektPhotovoltaik", "ID_Projekt"), ("Tab_Prozesstyp", "ID_Projekt"),
    ("Tab_Prozesswaerme", "ID_Projekt"), ("Tab_Quellprofil", "ID_Projekt"),
    ("Tab_Solar", "ID_Projekt"), ("Tab_Solarganglinie", "ID_Projekt"),
    ("Tab_Solarkollektoren", "ID_Projekt"), ("Tab_Stromganglinie", "ID_Projekt"),
    ("Tab_Stromspeicher", "ID_Projekt"), ("Tab_Stromverbraucher", "ID_Projekt"),
    ("Tab_Stromverbrauchertyp", "ID_Projekt"), ("Tab_Variante", "ID_Projekt"),
    ("Tab_WP", "ID_Projekt"), ("Tab_Waermebedarf", "ID_Projekt"),
    ("Berichtskonfiguration", "ProjektID"),
]

# Zweite Ebene: Detailtabelle -> (Elterntabelle, Verweisspalte, PK der Eltern)
DETAIL = [
    ("Tab_StromganglinieDaten", "Tab_Stromganglinie", "ID_Ganglinie", "ID"),
    ("Tab_WaermebedarfDaten", "Tab_Waermebedarf", "ID_Ganglinie", "ID"),
    ("Tab_ErgebnisBHKW", "Tab_Ergebnis", "ID_Ergebnis", "ID"),
    ("Tab_Kenndaten_Kuehlung", "Tab_WP", "ID_WP", "ID"),
]

ZEILEN_JE_GANGLINIE = 100     # in der Praxis 8760

# Ersatzwerte fuer STRICT-Spalten, die NOT NULL sind und keinen DEFAULT haben
FUELLWERT = {"INTEGER": 0, "REAL": 0.0, "TEXT": "x", "BLOB": b"", "ANY": 0}


# Genau eine FK-Verletzung wird bewusst gesaet: Tab_Einstellungen fuehrt
# ID_Projekt NOT NULL DEFAULT 0 UND einen Fremdschluessel auf Tab_Projekt - ein
# Projekt 0 gibt es aber nie. Solche Zeilen sind die globalen Vorgaben; sie
# muessen den Lauf unveraendert ueberstehen (Skriptkopf, GRENZEN 3).
ERWARTETE_FK_VERLETZUNGEN = [("Tab_Einstellungen", "Tab_Projekt")]


def spaltenbild(con):
    """{Tabelle: ([(Name, Typ, notnull, default, pk-Rang)], {FK-Spalte: notnull})}"""
    bild = {}
    for (t,) in con.execute("SELECT name FROM sqlite_master WHERE type='table' "
                            "AND name NOT LIKE 'sqlite_%'"):
        cols = [(c[1], c[2], c[3], c[4], c[5]) for c in con.execute('PRAGMA table_info("%s")' % t)]
        fk = set(f[3] for f in con.execute('PRAGMA foreign_key_list("%s")' % t))
        bild[t] = (cols, fk)
    return bild


def einfuegen(cur, bild, tab, werte):
    """INSERT fuer die Probe.

    * Fehlende NOT-NULL-Spalten ohne DEFAULT werden mit einem Ersatzwert gefuellt.
    * Eine einzelne INTEGER-PRIMARY-KEY-Spalte bleibt frei (rowid-Alias).
      Zusammengesetzte Schluessel muss der Aufrufer selbst belegen.
    * Nicht angegebene Fremdschluesselspalten werden ausdruecklich auf NULL
      gesetzt - sonst zoege ihr DEFAULT 0 eine FK-Verletzung nach sich, und die
      Probe koennte hinterher nicht mehr zwischen gesaeten und vom Skript
      erzeugten Waisen unterscheiden.
    """
    cols, fkspalten = bild[tab]
    w = dict(werte)
    pk_anzahl = sum(1 for c in cols if c[4])
    for name, typ, notnull, dflt, pkrang in cols:
        if name in w:
            continue
        if name in fkspalten:
            if notnull:
                raise AssertionError("%s.%s ist NOT NULL und Fremdschluessel - "
                                     "die Probe muss einen Wert liefern" % (tab, name))
            w[name] = None
            continue
        if not notnull or dflt is not None:
            continue
        if pkrang and pk_anzahl == 1 and typ == "INTEGER":
            continue                      # rowid-Alias: SQLite vergibt den Wert
        w[name] = FUELLWERT.get(typ, 0)
    spalten = ", ".join('"%s"' % k for k in w)
    frage = ", ".join("?" for _ in w)
    cur.execute('INSERT INTO "%s" (%s) VALUES (%s)' % (tab, spalten, frage), list(w.values()))
    return cur.lastrowid


def fuelle(con):
    """Legt Projekte, projektbezogene Zeilen, Katalogzeilen und Details an."""
    cur = con.cursor()
    cur.execute("PRAGMA foreign_keys = OFF")     # Aufbau ohne Reihenfolgezwang
    bild = spaltenbild(con)

    for pid in ALLE:
        einfuegen(cur, bild, "Tab_Projekt", {"ID": pid, "Projektname": "Probeprojekt %d" % pid})

    # --- Eltern, die andere Zeilen zwingend brauchen ------------------------
    # Tab_WP_STAMM fuer Tab_Kenndaten_Kuehlung_STAMM.ID_WP
    wp_stamm = einfuegen(cur, bild, "Tab_WP_STAMM", {"Bezeichner": "Katalog-WP"})
    # Tab_Brauchwasser: eine Katalogzeile (ID_Projekt = 0) und je Projekt eine.
    # Die Tabelle hat keinen Fremdschluessel auf Tab_Projekt, die 0 ist also
    # zulaessig und muss den Lauf ueberstehen.
    bw_katalog = einfuegen(cur, bild, "Tab_Brauchwasser",
                           {"ID_Projekt": 0, "Bezeichner": "Katalog-Brauchwasser"})
    bw = dict((pid, einfuegen(cur, bild, "Tab_Brauchwasser",
                              {"ID_Projekt": pid, "Bezeichner": "BW %d" % pid}))
              for pid in ALLE)
    # Tab_Klimaregion haengt per NOT-NULL-Fremdschluessel an Tab_Projekt - hier
    # ist keine Katalogzeile moeglich, ohne eine FK-Verletzung zu saeen.
    region = dict((pid, einfuegen(cur, bild, "Tab_Klimaregion",
                                  {"ID_Projekt": pid, "Bezeichner": "Region %d" % pid}))
                  for pid in ALLE)

    # --- 28er-Gruppe (Auswahl): je Projekt zwei Zeilen ----------------------
    lauf = 0
    ganglinien = {}
    for tab in NOFK_TABELLEN:
        for pid in ALLE:
            for _ in range(2):
                lauf += 1
                extra = {"ID_Projekt": pid}
                if tab == "Tab_Brauchwassertyp":
                    # zusammengesetzter Schluessel (ID, ID_Brauchwasser) und ein
                    # eigener UNIQUE-Index auf ID -> Werte selbst vergeben
                    extra.update({"ID": lauf, "ID_Brauchwasser": bw[pid],
                                  "Typname": "Projekttyp %d" % lauf})
                elif tab == "Tab_Klimadaten":
                    extra["ID_Klimaregion"] = region[pid]
                neu = einfuegen(cur, bild, tab, extra)
                if tab == "Tab_Stromganglinie":
                    ganglinien.setdefault(pid, []).append(neu)

    # --- 19er-Gruppe (Auswahl) ----------------------------------------------
    # nach der 28er-Gruppe, weil Z_ProjektStromganglinie eine echte Ganglinie
    # braucht (ID_Ganglinie ist NOT NULL und Fremdschluessel).
    for tab, spalte, anzahl in FK_TABELLEN:
        for pid in ALLE:
            for i in range(anzahl):
                extra = {spalte: pid}
                if tab == "Z_ProjektStromganglinie":
                    extra["ID_Ganglinie"] = ganglinien[pid][i]
                einfuegen(cur, bild, tab, extra)

    # --- Katalogzeilen, die ueberleben muessen ------------------------------
    # Tab_Brauchwassertyp: ID_Projekt ist NOT NULL DEFAULT 0 -> Marker 0
    for i in range(11):
        lauf += 1
        einfuegen(cur, bild, "Tab_Brauchwassertyp",
                  {"ID": lauf, "ID_Brauchwasser": bw_katalog, "ID_Projekt": 0,
                   "Typname": "VDI6002-Typ %d" % i})
    # Tab_BHKW: ID_Projekt ohne DEFAULT -> Marker NULL
    for i in range(7):
        einfuegen(cur, bild, "Tab_BHKW", {"ID_Projekt": None, "Bezeichner": "Katalog-BHKW %d" % i})
    # Tab_Heizkessel: ID_Projekt NOT NULL ohne DEFAULT -> Marker 0
    for i in range(5):
        einfuegen(cur, bild, "Tab_Heizkessel",
                  {"ID_Projekt": 0, "Bezeichner": "Katalog-Kessel %d" % i})
    # Tab_Einstellungen: globale Vorgabe mit ID_Projekt = 0 (UNIQUE -> genau eine).
    # Das ist zugleich die einzige bewusst gesaete FK-Verletzung, siehe oben.
    einfuegen(cur, bild, "Tab_Einstellungen", {"ID_Projekt": 0})
    # _STAMM-Katalog mit ID_Projekt-Spalte: darf nicht angefasst werden -
    # deshalb bewusst mit einem Projekt, das geloescht wird.
    for i in range(4):
        einfuegen(cur, bild, "Tab_Kenndaten_Kuehlung_STAMM",
                  {"ID_Projekt": 2000, "ID_WP": wp_stamm})

    # --- Tab_Applikation: zeigt auf ein Projekt, das verschwindet -----------
    einfuegen(cur, bild, "Tab_Applikation",
              {"ID": 1, "Projektname": "Probeprojekt 2000", "ID_Projekt": 2000})

    # --- Tab_Variante mit Verweis auf ein zu loeschendes Zweitprojekt -------
    # UNIQUE-Index auf ID_Projekt: genau eine Zeile je Projekt.
    einfuegen(cur, bild, "Tab_Variante",
              {"ID_Projekt": 1007, "ID_ProjektRef": 2000, "Variantenname": "Vergleich gegen 2000"})
    einfuegen(cur, bild, "Tab_Variante",
              {"ID_Projekt": 1008, "ID_ProjektRef": 1011, "Variantenname": "Vergleich gegen 1011"})
    einfuegen(cur, bild, "Tab_Variante",
              {"ID_Projekt": 2000, "ID_ProjektRef": 1030, "Variantenname": "muss weg"})

    # --- Berichtskonfiguration (Spalte heisst ProjektID, ohne FK) -----------
    for pid in ALLE:
        einfuegen(cur, bild, "Berichtskonfiguration", {"ProjektID": pid, "KonfigJson": "{}"})

    # --- zweite Ebene: Detailzeilen an jede Elternzeile ---------------------
    for kind, eltern, spalte, pk in DETAIL:
        for (eid,) in cur.execute('SELECT "%s" FROM "%s"' % (pk, eltern)).fetchall():
            for _ in range(ZEILEN_JE_GANGLINIE):
                einfuegen(cur, bild, kind, {spalte: eid})

    con.commit()
    cur.execute("PRAGMA foreign_keys = ON")


# ---------------------------------------------------------------------------
# 3. Hilfsfunktionen
# ---------------------------------------------------------------------------
def alle_tabellen(con):
    return [r[0] for r in con.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' "
        "ORDER BY name")]


def zeilenzahlen(con):
    return dict((t, con.execute('SELECT COUNT(*) FROM "%s"' % t).fetchone()[0])
                for t in alle_tabellen(con))


def fuehre_skript_aus(pfad, ohne_fremdschluessel=False):
    """Fuehrt das Reduzierungsskript so aus, wie sqlite3.exe es mit .read tut.

    ohne_fremdschluessel=True dreht die erste Anweisung des Skripts um und
    simuliert damit eine Umgebung, in der PRAGMA foreign_keys nicht greift
    (SQLiteStudio verwaltet seine Verbindung selbst). Der Skriptkopf behauptet,
    dass die Reduzierung auch dann vollstaendig ist - das wird hier geprueft.
    """
    con = sqlite3.connect(pfad, isolation_level=None)   # Autocommit, sonst kein VACUUM
    with open(SKRIPT, encoding="utf-8") as fh:
        text = fh.read()
    if ohne_fremdschluessel:
        # nur die Anweisung am Zeilenanfang, nicht die Erwaehnung im Kommentarkopf
        marke = "\nPRAGMA foreign_keys = ON;"
        anzahl = text.count(marke)
        assert anzahl == 1, "erwartet genau eine Anweisung PRAGMA foreign_keys = ON, " \
                            "gefunden %d" % anzahl
        text = text.replace(marke, "\nPRAGMA foreign_keys = OFF;")
    con.executescript(text)
    con.close()


# Katalogzeilen, die die Reduzierung vollstaendig ueberstehen muessen:
# {Bezeichnung: (Tabelle, WHERE-Bedingung)}
KATALOGPROBEN = {
    "Tab_Brauchwassertyp/0": ("Tab_Brauchwassertyp", '"ID_Projekt" = 0'),
    "Tab_BHKW/NULL": ("Tab_BHKW", '"ID_Projekt" IS NULL'),
    "Tab_Heizkessel/0": ("Tab_Heizkessel", '"ID_Projekt" = 0'),
    "Tab_Brauchwasser/0": ("Tab_Brauchwasser", '"ID_Projekt" = 0'),
    "Tab_Einstellungen/0": ("Tab_Einstellungen", '"ID_Projekt" = 0'),
    "Tab_Kenndaten_Kuehlung_STAMM": ("Tab_Kenndaten_Kuehlung_STAMM", "1"),
}


def katalogzahlen(con):
    return dict((name, con.execute('SELECT COUNT(*) FROM "%s" WHERE %s' % (tab, bed)).fetchone()[0])
                for name, (tab, bed) in KATALOGPROBEN.items())


def fk_verletzungen(con):
    """PRAGMA foreign_key_check, verdichtet auf (Kindtabelle, Elterntabelle)."""
    return sorted((r[0], r[2]) for r in con.execute("PRAGMA foreign_key_check"))


def projektspalten_aus_schema(con):
    """Alle (Tabelle, Spalte) mit einer Projektspalte, direkt aus dem Schema."""
    gefunden = []
    for t in alle_tabellen(con):
        if t == "Tab_Projekt":
            continue
        for c in con.execute('PRAGMA table_info("%s")' % t):
            if c[1].lower() in ("id_projekt", "projektid"):
                gefunden.append((t, c[1]))
    return sorted(gefunden)


def fremdprojekt_reste(con):
    """Zeilen in den Projekttabellen, die zu keinem der dreizehn Projekte gehoeren."""
    reste = []
    liste = ",".join(str(p) for p in BEHALTEN)
    for tab, spalte in ALLE_PROJEKTSPALTEN:
        n = con.execute('SELECT COUNT(*) FROM "%s" WHERE "%s" IS NOT NULL AND "%s" <> 0 '
                        'AND "%s" NOT IN (%s)'
                        % (tab, spalte, spalte, spalte, liste)).fetchone()[0]
        if n:
            reste.append("%s.%s: %d" % (tab, spalte, n))
    return reste


def detail_waisen(con):
    """Detailzeilen ohne Elternzeile - eigene Gegenprobe neben foreign_key_check."""
    waisen = []
    for kind, eltern, spalte, pk in DETAIL:
        n = con.execute('SELECT COUNT(*) FROM "%s" WHERE "%s" IS NOT NULL AND "%s" NOT IN '
                        '(SELECT "%s" FROM "%s")'
                        % (kind, spalte, spalte, pk, eltern)).fetchone()[0]
        if n:
            waisen.append("%s: %d" % (kind, n))
    return waisen


def kaskadentabellen_aus_schema(con):
    """Tabellen mit FOREIGN KEY ... REFERENCES Tab_Projekt ON DELETE CASCADE."""
    treffer = []
    for t in alle_tabellen(con):
        for f in con.execute('PRAGMA foreign_key_list("%s")' % t):
            if f[2] == "Tab_Projekt" and f[6] == "CASCADE":
                treffer.append((t, f[3]))
    return sorted(treffer)


# ---------------------------------------------------------------------------
# 4. Ablauf
# ---------------------------------------------------------------------------
def main():
    print("Probe fuer %s" % os.path.relpath(SKRIPT, WURZEL))
    print("sqlite3-Bibliothek: %s" % sqlite3.sqlite_version)
    if tuple(int(x) for x in sqlite3.sqlite_version.split(".")) < MIN_SQLITE:
        print("ABBRUCH: STRICT-Tabellen brauchen SQLite >= %d.%d" % MIN_SQLITE[:2])
        return 2
    if not os.path.exists(SKRIPT):
        print("ABBRUCH: %s fehlt" % SKRIPT)
        return 2

    verzeichnis = tempfile.mkdtemp(prefix="epos_probe_")
    pfad = os.path.join(verzeichnis, "Kenndaten_Probe.sqlite")

    # ---- Aufbau ----------------------------------------------------------
    con = baue_schema(pfad)
    tabellen = alle_tabellen(con)
    print("\nSchema: %d Tabellen, %d Views" % (
        len(tabellen),
        con.execute("SELECT COUNT(*) FROM sqlite_master WHERE type='view'").fetchone()[0]))

    # Pruefung 0: die Listen dieser Probe gegen das Schema. Schlaegt an, sobald
    # das Schema eine neue Projektspalte oder Kaskade bekommt - dann muss auch
    # Reduziere-Testdatenbank.sql nachgezogen werden.
    kaskaden = kaskadentabellen_aus_schema(con)
    aus_schema = projektspalten_aus_schema(con)
    print("Schema fuehrt %d Tabellen mit Projektspalte und %d Kaskaden auf Tab_Projekt."
          % (len(aus_schema), len(kaskaden)))
    erwartet = sorted(ALLE_PROJEKTSPALTEN)
    tatsaechlich = sorted(x for x in aus_schema if x[0] not in AUSGENOMMEN)
    fehlt = [x for x in tatsaechlich if x not in erwartet]
    zuviel = [x for x in erwartet if x not in tatsaechlich]
    pruefe(not fehlt and not zuviel,
           "Gegenprobe deckt alle %d Projektspalten des Schemas ab%s"
           % (len(tatsaechlich),
              "" if not (fehlt or zuviel) else " -> fehlt %s / zuviel %s" % (fehlt, zuviel)))
    pruefe(len(kaskaden) == 19, "19 Tabellen mit ON DELETE CASCADE auf Tab_Projekt (%d)"
           % len(kaskaden))
    pruefe(len(aus_schema) - len(kaskaden) == 29,
           "29 Tabellen mit Projektspalte ohne Kaskade (%d): 28 x ID_Projekt + "
           "Berichtskonfiguration.ProjektID" % (len(aus_schema) - len(kaskaden)))

    fuelle(con)

    vorher = zeilenzahlen(con)
    katalog_vorher = katalogzahlen(con)
    con.execute("PRAGMA foreign_keys = ON")
    fk_vorher = fk_verletzungen(con)
    groesse_vorher = os.path.getsize(pfad)
    con.close()

    if fk_vorher != sorted(ERWARTETE_FK_VERLETZUNGEN):
        hinweise.append("Ausgangsbestand traegt andere FK-Verletzungen als erwartet: %s"
                        % fk_vorher)

    print("Aufbau: %d Projekte (%s), %d Zeilen in %d gefuellten Tabellen, %.2f MB" % (
        len(ALLE), ",".join(str(p) for p in ALLE),
        sum(vorher.values()), sum(1 for v in vorher.values() if v), groesse_vorher / 1048576.0))

    # ---- erster Lauf -----------------------------------------------------
    print("\n--- erster Lauf des Skripts ---")
    fuehre_skript_aus(pfad)

    con = sqlite3.connect(pfad, isolation_level=None)
    con.execute("PRAGMA foreign_keys = ON")
    nachher = zeilenzahlen(con)
    groesse_nachher = os.path.getsize(pfad)
    print("nach der Reduzierung: %d Zeilen, %.2f MB (vorher %.2f MB)" % (
        sum(nachher.values()), groesse_nachher / 1048576.0, groesse_vorher / 1048576.0))

    # Pruefung 1: genau die dreizehn Projekte
    ids = [r[0] for r in con.execute('SELECT "ID" FROM "Tab_Projekt" ORDER BY "ID"')]
    pruefe(ids == BEHALTEN, "Tab_Projekt enthaelt genau die 13 Referenzprojekte (%d)" % len(ids))

    # Pruefung 2: keine Projektzeile ausserhalb der dreizehn - 46 Tabellen
    reste = fremdprojekt_reste(con)
    pruefe(not reste, "keine Fremdprojekt-Zeile in den %d Tabellen mit Projektspalte%s"
           % (len(ALLE_PROJEKTSPALTEN), "" if not reste else " -> " + "; ".join(reste)))

    # Pruefung 3: Katalogzeilen vollstaendig erhalten
    katalog_nachher = katalogzahlen(con)
    for schluessel in sorted(katalog_vorher):
        pruefe(katalog_vorher[schluessel] == katalog_nachher[schluessel],
               "Katalogzeilen %s unveraendert (%d -> %d)"
               % (schluessel, katalog_vorher[schluessel], katalog_nachher[schluessel]))

    # Pruefung 4: Tab_Applikation umgehaengt
    app = [r[0] for r in con.execute('SELECT "ID_Projekt" FROM "Tab_Applikation"')]
    pruefe(app == [1030], "Tab_Applikation.ID_Projekt = 1030 (war 2000), gelesen %s" % app)

    # Pruefung 5: Tab_Variante.ID_ProjektRef bereinigt bzw. erhalten
    refs = dict(con.execute('SELECT "ID_Projekt", "ID_ProjektRef" FROM "Tab_Variante"'))
    pruefe(refs.get(1007) is None and refs.get(1008) == 1011,
           "Tab_Variante: Verweis auf geloeschtes Projekt geleert, gueltiger erhalten (%s)" % refs)

    # Pruefung 6: keine Waisen in den Detailtabellen (eigene Gegenprobe)
    waisen = detail_waisen(con)
    pruefe(not waisen, "keine Waisen in den Detailtabellen%s"
           % ("" if not waisen else " -> " + "; ".join(waisen)))

    # Pruefung 7: Detailzeilen der behaltenen Projekte sind noch da
    n_detail = con.execute('SELECT COUNT(*) FROM "Tab_StromganglinieDaten"').fetchone()[0]
    erwartet = len(BEHALTEN) * 2 * ZEILEN_JE_GANGLINIE
    pruefe(n_detail == erwartet,
           "Tab_StromganglinieDaten: %d Zeilen (13 Projekte x 2 Ganglinien x %d), erwartet %d"
           % (n_detail, ZEILEN_JE_GANGLINIE, erwartet))

    # Pruefung 8: PRAGMA-Kontrollen. foreign_key_check darf nur die vorher schon
    # vorhandene, bewusst gesaete Verletzung melden - keine neue.
    fk_nachher = fk_verletzungen(con)
    neu = [x for x in fk_nachher if x not in fk_vorher]
    pruefe(not neu, "PRAGMA foreign_key_check meldet keine NEUE Verletzung%s"
           % ("" if not neu else " -> " + repr(neu)))
    pruefe(fk_nachher == fk_vorher,
           "vorbestehende FK-Verletzungen unveraendert (%s)" % (fk_nachher or "keine"))
    iv = con.execute("PRAGMA integrity_check").fetchone()[0]
    pruefe(iv == "ok", "PRAGMA integrity_check = %s" % iv)

    # Pruefung 9: alle 114 Tabellen noch vorhanden, temp-Tabelle weg
    pruefe(alle_tabellen(con) == tabellen,
           "alle %d Tabellen unveraendert vorhanden" % len(tabellen))
    con.close()

    # ---- zweiter Lauf: Idempotenz ----------------------------------------
    print("\n--- zweiter Lauf des Skripts (Idempotenz) ---")
    fuehre_skript_aus(pfad)
    con = sqlite3.connect(pfad, isolation_level=None)
    zweiter = zeilenzahlen(con)
    abweichung = dict((t, (nachher[t], zweiter[t])) for t in nachher if nachher[t] != zweiter[t])
    pruefe(not abweichung, "Zeilenzahlen aller %d Tabellen identisch%s"
           % (len(zweiter), "" if not abweichung else " -> " + repr(abweichung)))
    app2 = [r[0] for r in con.execute('SELECT "ID_Projekt" FROM "Tab_Applikation"')]
    pruefe(app2 == app, "Tab_Applikation nach zweitem Lauf unveraendert (%s)" % app2)
    con.close()

    # ---- dritter Lauf: frische Datenbank, PRAGMA foreign_keys wirkungslos --
    # Prueft die Zusage aus dem Skriptkopf: die Stufen 1b, 2 und 3 reduzieren
    # auch dann vollstaendig, wenn keine Kaskade laeuft (Fall SQLiteStudio).
    print("\n--- dritter Lauf: ohne wirksame Fremdschluessel ---")
    pfad_ofk = os.path.join(verzeichnis, "Kenndaten_Probe_ohne_FK.sqlite")
    con = baue_schema(pfad_ofk)
    fuelle(con)
    con.close()
    fuehre_skript_aus(pfad_ofk, ohne_fremdschluessel=True)

    con = sqlite3.connect(pfad_ofk, isolation_level=None)
    ids3 = [r[0] for r in con.execute('SELECT "ID" FROM "Tab_Projekt" ORDER BY "ID"')]
    pruefe(ids3 == BEHALTEN, "ohne FK: Tab_Projekt enthaelt die 13 Referenzprojekte (%d)"
           % len(ids3))
    reste3 = fremdprojekt_reste(con)
    pruefe(not reste3, "ohne FK: keine Fremdprojekt-Zeile in den %d Tabellen%s"
           % (len(ALLE_PROJEKTSPALTEN), "" if not reste3 else " -> " + "; ".join(reste3)))
    waisen3 = detail_waisen(con)
    pruefe(not waisen3, "ohne FK: keine Waisen in den Detailtabellen%s"
           % ("" if not waisen3 else " -> " + "; ".join(waisen3)))
    katalog3 = katalogzahlen(con)
    gleich = all(katalog_vorher[k] == katalog3[k] for k in katalog_vorher)
    pruefe(gleich, "ohne FK: Katalogzeilen vollstaendig erhalten (%s)"
           % ("alle gleich" if gleich else katalog3))
    con.close()

    # ---- Abschluss --------------------------------------------------------
    print("\nDatei: %.2f MB -> %.2f MB (%.0f %% kleiner)" % (
        groesse_vorher / 1048576.0, groesse_nachher / 1048576.0,
        100.0 * (1 - groesse_nachher / float(groesse_vorher))))
    print("Probedatenbank: %s" % pfad)
    for h in hinweise:
        print("Hinweis: %s" % h)
    if fehler:
        print("\nERGEBNIS: %d Pruefung(en) fehlgeschlagen." % len(fehler))
        return 1
    print("\nERGEBNIS: alle Pruefungen bestanden.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
