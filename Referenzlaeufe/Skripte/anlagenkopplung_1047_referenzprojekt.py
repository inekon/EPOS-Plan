#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Legt das VIERZEHNTE Referenzprojekt der Testdatenbank an: 1047 "Referenz Anlagenkopplung AK1" -
die Kopie des Kuehlreferenzprojekts 1017 mit gekoppeltem Heizkreis und gekoppelter Kuehluebergabe
(Stufe AK1 der Anlagenkopplung, fuenfte Welle; Konzept Anlagenkopplung 11.4, Einfrierregel
"gesaete Auslegungsdaten der Uebergabe").

WOZU. Die Wellen 2 bis 4 von AK1 haben den Heizkreis (Schritt H) und die Kuehluebergabe (Schritt K)
gebaut - ergebnisneutral fuer alle dreizehn Referenzprojekte, weil keines die Kopplung einschaltet.
Die gekoppelte Rechnung hatte damit kein Regressionsnetz ausser den Rechenproben der Tests. Dieses
Projekt haengt sie hinein: Heizseite und Kaelteseite gekoppelt, ueber ein ganzes Jahr, mit einer
Waermepumpe, die heizt und kuehlt.

WARUM 1017 ALS VORLAGE. 1017 ist das Kuehlreferenzprojekt: ein Einzelgebaeude (10599, VDI 6007),
Kuehlung mit Haken, Kuehlsollwert 24 Grad C und Kuehlleistungsgrenze 15 kW
(kuehlung_1017_referenzprojekt.py), dazu die reversible Waermepumpe auf Kaskadenplatz 3 mit
Kuehl-Vorlauf 18 Grad C und gesaeter Kuehlkennlinie (kaelteerzeuger_1017_referenzprojekt.py). Nur
dort haben beide Seiten der Kopplung etwas zu rechnen. Auf 1017 selbst waere die Kopplung ein
Basiswechsel des Kuehlreferenzfalls; deshalb eine KOPIE, und 1017 bleibt unveraendert und
ungekoppelt - das Paar 1017/1047 ist zugleich der Vergleich ideal gegen gekoppelt.

WAS DIESES SKRIPT TUT.
  1. Die Kopie 1017 -> 1047 auf dem KOPIERWEG DES PROGRAMMS: ProjektDuplizierenCtrl.Duplizieren
     ("Projekt Speichern unter") Schritt fuer Schritt nachgebildet - Tabellenplan (Projektspalte,
     KINDER, Kinder ueber deklarierte Fremdschluessel, Ausschluesse, Ergebnistabellen nicht
     kopiert), Reihenfolge (Sortiere), Versatz je Tabelle (MAX ueber alle - MIN ueber die
     Quellzeilen + 1), freie Projekt-Id ueber alle Projekttabellen und dieselben Anweisungen
     INSERT ... SELECT mit IIF(Spalte > 0, Spalte + Versatz, Spalte). Die zwei Nachzuege des
     Programms (Geraeteanker der Kostenpositionen, Bezuege der Speicherauslegung) haben bei 1017
     nichts zu tun: 1017 fuehrt weder Kostenpositionen noch eine Speicherauslegung - geprueft,
     sonst Abbruch. Die Vollstaendigkeit ist gegen ein vom Programm dupliziertes Projekt
     geprueft (Zellvergleich aller Tabellen: gleich bis auf die Zellen aus 2.).
  2. Sieben Zellen der Kopie:
       Tab_Projekt 1047:        Beschreibung             ''   -> Zweck des Projekts (neutral)
       Tab_Einstellungen 1047:  Anlagenkopplung          NULL -> 'AK1'        (DbWerte.ANLAGENKOPPLUNG_AK1)
       Tab_Gebaeude (Kopie von 10599):
                                Heizkreis_Aktiv          0    -> 1
                                Uebergabe_Art            NULL -> 'RADIATOR'   (DbWerte.UEBERGABE_RADIATOR)
                                Heizkurve_Aktiv          0    -> 1            (Heizkurve gefahren)
                                Kuehluebergabe_Aktiv     0    -> 1
                                Kuehl_Uebergabe_Art      NULL -> 'KUEHLDECKE' (DbWerte.KUEHLUEBERGABE_KUEHLDECKE)
     Alle uebrigen Uebergabespalten bleiben NULL und werden geprueft: Exponent, Nennleistung,
     Auslegungspunkt (Vorlauf, Ruecklauf, Raum, aussen), Heizkurve (Niveau, Steilheit),
     Regler_Proportionalband (NULL = 1,0 K), Sollwertprofil (NULL = die Sollwerte des Gebaeudes)
     und die Kuehl_*-Spalten der Kuehluebergabe. Damit rechnen die EPOS-Vorgaben der Art: Radiator
     mit n = 1,3 und 55/45 Grad C, Heizkurve aus dem Auslegungspunkt mit Niveau 0 K
     und Steilheit 1,0, Auslegungs-Aussentemperatur aus dem kaeltesten Tagesmittel (H10),
     Nennleistung aus der Auslegungsheizlast (8.4); Kuehldecke mit 16/19 Grad C, n = 1,1,
     Vorlaufgrenze 16 Grad C und der Nennleistung aus dem Auslegungstag (E37, A2).
  Kein VACUUM, keine andere Zeile, keine andere Spalte. 1017 bleibt Zelle fuer Zelle, wie es war.

WIEDERHOLBAR. Steht das Projekt "Referenz Anlagenkopplung AK1" schon (Id 1047) und tragen seine
Zellen die Zielwerte, aendert das Skript nichts und meldet das. Steht irgendwo etwas anderes als
der Ausgangs- oder der Zielwert, weicht die Vorlage 1017 ab oder faellt die Kopie auf eine andere
Id als 1047, bricht es VOR dem Festschreiben ab (Rueckgabe 2; die Kopie laeuft in einer
Transaktion).

FOLGE. Die Zellen sind gesaete Auslegungsdaten der Uebergabe eines Referenzprojekts (Einfrierregel
"gesaete Auslegungsdaten der Uebergabe", `Referenzlaeufe/LIESMICH.md`); die Basis wird im selben
Schritt neu eingefroren. Die Kopie ist zugleich ein neues Referenzprojekt mit Gebaeude-, Kaelte-
und Kaelteerzeugerdaten - die Regeln "gesaete Gebaeudedaten" und "gesaete Kaeltedaten" gelten fuer
1047 wie fuer 1017.

Aufruf (Windows: `py`, sonst `python3`; vorher sichern):
    py Referenzlaeufe/Skripte/anlagenkopplung_1047_referenzprojekt.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

VORLAGE = 1017
VORLAGE_NAME = "WP_PV-Speicher"
VORLAGE_GEBAEUDE = 10599
NEU = 1047
NAME = "Referenz Anlagenkopplung AK1"
BESCHREIBUNG = (
    "Vierzehntes Referenzprojekt der Basis: Kopie von Projekt 1017 mit Anlagenkopplung AK1 - "
    "Heizkreis mit Radiator und gefahrener Heizkurve, Kühlübergabe mit Kühldecke, alle "
    "Auslegungswerte als Vorgaben der Art. Es hält die gekoppelte Rechnung beider Seiten im "
    "Regressionsnetz; 1017 bleibt der ungekoppelte Vergleichsfall.")

AK1 = "AK1"                   # DbWerte.ANLAGENKOPPLUNG_AK1
RADIATOR = "RADIATOR"         # DbWerte.UEBERGABE_RADIATOR
KUEHLDECKE = "KUEHLDECKE"     # DbWerte.KUEHLUEBERGABE_KUEHLDECKE
WAERMEPUMPE = "Wärmepumpe"    # DbWerte.ERZEUGER_WAERMEPUMPE

# Die Uebergabespalten aus AK-S1 und KAK-S1, die NULL bleiben (Vorgaben der Art).
LEER = ["Uebergabe_Exponent", "Uebergabe_Leistung_Nenn", "Auslegung_Vorlauf", "Auslegung_Ruecklauf",
        "Auslegung_Raumtemperatur", "Auslegung_Aussentemperatur", "Heizkurve_Niveau",
        "Heizkurve_Steilheit", "Regler_Proportionalband", "Sollwertprofil",
        "Kuehl_Uebergabe_Exponent", "Kuehl_Uebergabe_Leistung_Nenn", "Kuehl_Auslegung_Vorlauf",
        "Kuehl_Auslegung_Ruecklauf", "Kuehl_Auslegung_Raumtemperatur", "Kuehl_Vorlaufgrenze"]

# Die Schalter der Vorlage, die in der Kopie umgelegt werden - in 1017 aus bzw. leer.
SCHALTER_AUS = {"Heizkreis_Aktiv": 0, "Uebergabe_Art": None, "Heizkurve_Aktiv": 0,
                "Kuehluebergabe_Aktiv": 0, "Kuehl_Uebergabe_Art": None}

# Was an Vorlage und Kopie stehen bleiben muss (Kuehlung aus KU1, Kaelteerzeuger aus KU2).
GEBAEUDE_BLEIBT = {"Kuehlung_Aktiv": 1, "Kuehl_Sollwert": 24.0, "Kuehlleistung_Max": 15.0}
EINSTELLUNGEN_BLEIBT = {"Kuehlbetrieb": 1, "Tool_3": WAERMEPUMPE}


# =====================================================================================
#  Der Kopierweg des Programms (ProjektDuplizierenCtrl), Wortlaut der Tabellen und Regeln
# =====================================================================================

KATALOG_TABELLEN = {"Tab_BrennstoffKategorien", "Tab_Typ_Energieanlagen", "Tab_KostenGruppenKatalog",
                    "Tab_KostenKomponente", "Tab_Kostenfaktor", "energy_carrier", "energy_conversion",
                    "pricing_model", "Tab_Applikation", "Einfügefehler"}

AUSNAHME_TABELLEN = {"Berichtskonfiguration", "Tab_ProjektPhotovoltaik"}

KATALOG_SPALTEN = {"ID_Type", "ID_Stamm", "StammID", "KomponentenID", "KategorieID", "carrier_id",
                   "ID_Energieträger", "ID_Umrechnung", "ID_Brennstoff", "ID_Nutzungsart",
                   "ID_Tagesgangsatz", "ID_Bedarfstag", "ID_Ausstattung", "ID_Gebaeude_Stamm"}

FK_MAP = {
    "ID_WP": "Tab_WP", "ID_SP": "Tab_Stromspeicher", "ID_PV": "Tab_PV",
    "ID_Solar": "Tab_Solarkollektoren", "ID_Kessel": "Tab_Heizkessel", "ID_BHKW": "Tab_BHKW",
    "ID_PUFFER": "Tab_Pufferspeicher", "ID_Pufferspeicher": "Tab_Pufferspeicher",
    "WS_ID_Puffer": "Tab_Pufferspeicher", "WS_ID_Puffer2": "Tab_Pufferspeicher",
    "WQ_ID_Puffer": "Tab_Pufferspeicher",
    "ID_Klimaregion": "Tab_Klimaregion", "ID_ProjektGebaeude": "Z_ProjektGebaeude",
    "ID_Gebaeude": "Tab_Gebaeude", "ID_TagV": "Tab_DBTagV",
    "ID_Stromverbraucher": "Tab_Stromverbraucher", "ID_Prozesswaerme": "Tab_Prozesswaerme",
    "ID_Brauchwasser": "Tab_Brauchwasser",
    "ID_Anlage": "Tab_Energieanlagen",
    "ID_Senke": "Z_AnlageSenke",
    "WQ_ID_Quellprofil": "Tab_Quellprofil", "ID_Quellprofil": "Tab_Quellprofil",
    "ID_Wechselrichter": "Tab_Wechselrichter",
    "ID_Zone": "Tab_TwwZone",
    "ID_Aufbau": "Tab_Bauteilaufbau", "ID_Baustoff": "Tab_Baustoff",
    "ID_Importquelle": "Tab_Importquelle", "ID_Bauteil": "Tab_Bauteil",
}

FK_OVERRIDE = {
    "Z_ProjektWaermebedarf": {"ID_Ganglinie": "Tab_Waermebedarf"},
    "Z_ProjektStromganglinie": {"ID_Ganglinie": "Tab_Stromganglinie"},
    "Z_ProjektSolarganglinie": {"ID_Ganglinie": "Tab_Solarganglinie"},
    "Tab_WaermebedarfDaten": {"ID_Ganglinie": "Tab_Waermebedarf"},
    "Tab_StromganglinieDaten": {"ID_Ganglinie": "Tab_Stromganglinie"},
    "Tab_SolarganglinieDaten": {"ID_Ganglinie": "Tab_Solarganglinie"},
    "Tab_Bauteil": {"ID_Zone": "Tab_Zone"},
    "Tab_Importzuordnung": {"ID_Zone": "Tab_Zone"},
}

KINDER = {
    "Tab_Kenndaten": "ID_WP IN (SELECT ID FROM Tab_WP WHERE ID_Projekt = {0})",
    "Tab_Kenndaten_Kuehlung": "ID_WP IN (SELECT ID FROM Tab_WP WHERE ID_Projekt = {0})",
    "Tab_DBTagV": "ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0})",
    "Tab_DBTagVDaten": "ID_TagV IN (SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN "
                       "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0}))",
    "Tab_WaermebedarfDaten": "ID_Ganglinie IN (SELECT ID FROM Tab_Waermebedarf WHERE ID_Projekt = {0})",
    "Tab_StromganglinieDaten": "ID_Ganglinie IN (SELECT ID FROM Tab_Stromganglinie WHERE ID_Projekt = {0})",
    "Tab_SolarganglinieDaten": "ID_Ganglinie IN (SELECT ID FROM Tab_Solarganglinie WHERE ID_Projekt = {0})",
    "Tab_Stromverbrauchertyp": "ID_Stromverbraucher IN (SELECT ID FROM Tab_Stromverbraucher WHERE ID_Projekt = {0})",
    "Z_AnlageSenke": "ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {0})",
    "Z_AnlagePufferVerbund": "ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {0})",
    "Z_AnlageStrang": "ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {0})",
    "Tab_QuellprofilDaten": "ID_Quellprofil IN (SELECT ID FROM Tab_Quellprofil WHERE ID_Projekt = {0})",
    "Tab_TwwWohnungstyp": "ID_Zone IN (SELECT ID FROM Tab_TwwZone WHERE ID_Projekt = {0})",
    "Tab_Zone": "ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0})",
    "Tab_Bauteil": "ID_Zone IN (SELECT ID FROM Tab_Zone WHERE ID_Gebaeude IN "
                   "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0}))",
    "Tab_Bauteilschicht": "ID_Aufbau IN (SELECT ID FROM Tab_Bauteilaufbau WHERE ID_Projekt = {0})",
    "Tab_Importquelle": "ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0})",
    "Tab_Importzuordnung": "ID_Importquelle IN (SELECT ID FROM Tab_Importquelle WHERE ID_Gebaeude IN "
                           "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0}))",
}

# Alle Namensvergleiche des Programms sind OrdinalIgnoreCase.
KATALOG_TABELLEN_K = {n.lower() for n in KATALOG_TABELLEN}
AUSNAHME_TABELLEN_K = {n.lower() for n in AUSNAHME_TABELLEN}
KATALOG_SPALTEN_K = {n.lower() for n in KATALOG_SPALTEN}
FK_MAP_K = {k.lower(): v for k, v in FK_MAP.items()}
FK_OVERRIDE_K = {t.lower(): {k.lower(): v for k, v in m.items()} for t, m in FK_OVERRIDE.items()}
KINDER_K = {k.lower(): v for k, v in KINDER.items()}


class Spec:
    def __init__(self, tabelle, pk, filter_, spalten, ergebnis, namespalte=None):
        self.tabelle = tabelle
        self.pk = pk
        self.filter = filter_
        self.spalten = spalten
        self.ergebnis = ergebnis
        self.namespalte = namespalte


def tabellenliste(c):
    return [r[0] for r in c.execute(
        "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name")]


def spalten(c, tabelle):
    return [r[1] for r in c.execute('PRAGMA table_info("%s")' % tabelle)]


def enthaelt(cols, col):
    return any(x.lower() == col.lower() for x in cols)


def ermittle_pk(cols):
    for kandidat in ("ID", "id", "ID_Z"):
        if enthaelt(cols, kandidat):
            return kandidat
    return cols[0]


def ist_ergebnis(name):
    return name.lower().startswith("tab_ergebnis")


def ausgeschlossen(name):
    n = name.lower()
    if n.endswith("_stamm") or n.startswith("msys") or n.startswith("~") or n.startswith("f_"):
        return True
    return n in KATALOG_TABELLEN_K or n in AUSNAHME_TABELLEN_K


def echte_fks(c):
    """(tabelle, spalte) -> Zieltabelle, klein geschrieben (LiesEchteFks)."""
    karte = {}
    for t in tabellenliste(c):
        for r in c.execute("SELECT [table], [from] FROM pragma_foreign_key_list(?) ORDER BY id, seq", (t,)):
            if r[0] and r[1]:
                karte[(t.lower(), r[1].lower())] = r[0]
    return karte


def ermittle_plan(c, fks):
    plan = {"tab_projekt": Spec("Tab_Projekt", "ID", "ID = {0}", spalten(c, "Tab_Projekt"), False, "Projektname")}
    uebrig = {}
    for name in tabellenliste(c):
        if not name or ausgeschlossen(name) or name.lower() == "tab_projekt":
            continue
        cols = spalten(c, name)
        if not cols:
            continue
        erg = ist_ergebnis(name)
        if name.lower() in KINDER_K:
            plan[name.lower()] = Spec(name, ermittle_pk(cols), KINDER_K[name.lower()], cols, erg)
        elif enthaelt(cols, "ID_Projekt"):
            plan[name.lower()] = Spec(name, ermittle_pk(cols), "[ID_Projekt] = {0}", cols, erg)
        elif enthaelt(cols, "ProjektID"):
            plan[name.lower()] = Spec(name, ermittle_pk(cols), "[ProjektID] = {0}", cols, erg)
        else:
            uebrig[name] = cols
    neu_hinzu = True
    while neu_hinzu:
        neu_hinzu = False
        for name, cols in list(uebrig.items()):
            fk_spalte = eltern = None
            for col in cols:
                p = fks.get((name.lower(), col.lower()))
                if p is not None and p.lower() in plan and p.lower() != name.lower():
                    fk_spalte, eltern = col, p
                    break
            if fk_spalte is None:
                continue
            p_spec = plan[eltern.lower()]
            filter_ = "[%s] IN (SELECT [%s] FROM [%s] WHERE %s)" % (fk_spalte, p_spec.pk, eltern, p_spec.filter)
            plan[name.lower()] = Spec(name, ermittle_pk(cols), filter_, cols, p_spec.ergebnis or ist_ergebnis(name))
            del uebrig[name]
            neu_hinzu = True
    return sortiere(list(plan.values()), fks)


def sortiere(specs, fks):
    kopier = {s.tabelle.lower() for s in specs}
    deps = {s.tabelle.lower(): set() for s in specs}
    for (fk_t, _), pk_t in fks.items():
        if fk_t not in deps or pk_t.lower() not in kopier or fk_t == pk_t.lower():
            continue
        deps[fk_t].add(pk_t.lower())
    ergebnis, erledigt, rest = [], set(), list(specs)
    while rest:
        platziert = 0
        i = 0
        while i < len(rest):
            s = rest[i]
            if all(d in erledigt for d in deps[s.tabelle.lower()]):
                ergebnis.append(s)
                erledigt.add(s.tabelle.lower())
                rest.pop(i)
                platziert += 1
                continue
            i += 1
        if platziert:
            continue
        best, best_offen, best_abh = 0, None, -1
        for i, s in enumerate(rest):
            offen = sum(1 for d in deps[s.tabelle.lower()] if d not in erledigt)
            abh = sum(1 for o in rest if s.tabelle.lower() in deps[o.tabelle.lower()])
            if best_offen is None or offen < best_offen or (offen == best_offen and abh > best_abh):
                best, best_offen, best_abh = i, offen, abh
        ergebnis.append(rest[best])
        erledigt.add(rest[best].tabelle.lower())
        rest.pop(best)
    return ergebnis


def ermittle_ziel(fks, tabelle, col, pk):
    if col.lower() == pk.lower():
        return tabelle
    if col.lower() in ("id_projekt", "projektid"):
        return "Tab_Projekt"
    if col.lower() in KATALOG_SPALTEN_K:
        return None
    echt = fks.get((tabelle.lower(), col.lower()))
    if echt is not None:
        return echt
    ov = FK_OVERRIDE_K.get(tabelle.lower(), {})
    if col.lower() in ov:
        return ov[col.lower()]
    return FK_MAP_K.get(col.lower())


def filter_fuer(spec, projekt):
    return spec.filter.replace("{0}", str(projekt))


def versatz(c, spec, quelle):
    hoechste = c.execute("SELECT MAX([%s]) FROM [%s]" % (spec.pk, spec.tabelle)).fetchone()[0]
    kleinste = c.execute("SELECT MIN([%s]) FROM [%s] WHERE %s"
                         % (spec.pk, spec.tabelle, filter_fuer(spec, quelle))).fetchone()[0]
    if kleinste is None:
        return None
    o = int(hoechste or 0) - int(kleinste) + 1
    return o if o >= 1 else 1


def freie_projekt_id(c, specs, vorschlag):
    hoechste = vorschlag - 1
    for s in specs:
        spalte = "ID_Projekt" if enthaelt(s.spalten, "ID_Projekt") else (
            "ProjektID" if enthaelt(s.spalten, "ProjektID") else None)
        if spalte is None:
            continue
        wert = c.execute("SELECT MAX([%s]) FROM [%s]" % (spalte, s.tabelle)).fetchone()[0]
        if wert is not None and int(wert) > hoechste:
            hoechste = int(wert)
    return hoechste + 1


def insert_sql(fks, spec, quelle, offset, kopier, ergebnisse):
    cols, exprs = [], []
    for col in spec.spalten:
        cols.append("[%s]" % col)
        if spec.namespalte and col.lower() == spec.namespalte.lower():
            exprs.append("?")
            continue
        ziel = ermittle_ziel(fks, spec.tabelle, col, spec.pk)
        if ziel is not None and ziel.lower() in ergebnisse:
            exprs.append("NULL")
        elif ziel is not None and ziel.lower() in offset and ziel.lower() in kopier:
            exprs.append("IIF([%s] > 0, [%s] + %d, [%s])" % (col, col, offset[ziel.lower()], col))
        else:
            exprs.append("[%s]" % col)
    return ("INSERT INTO [%s] (%s) SELECT %s FROM [%s] WHERE %s"
            % (spec.tabelle, ", ".join(cols), ", ".join(exprs), spec.tabelle, filter_fuer(spec, quelle)))


def duplizieren(c):
    """ProjektDuplizierenCtrl.Duplizieren(VORLAGE_NAME, NAME) - in der offenen Transaktion.
    Rueckgabe: neue Projekt-Id und die Tabellen mit ihren kopierten Zeilen."""
    fks = echte_fks(c)
    specs = ermittle_plan(c, fks)
    kopier = {s.tabelle.lower() for s in specs if not s.ergebnis}
    ergebnisse = {s.tabelle.lower() for s in specs if s.ergebnis}

    offset = {}
    for s in specs:
        if s.ergebnis:
            continue
        o = versatz(c, s, VORLAGE)
        if o is not None:
            offset[s.tabelle.lower()] = o
    frei = freie_projekt_id(c, specs, VORLAGE + offset["tab_projekt"])
    offset["tab_projekt"] = frei - VORLAGE

    bericht = []
    for s in specs:
        if s.tabelle.lower() not in offset:
            continue
        sql = insert_sql(fks, s, VORLAGE, offset, kopier, ergebnisse)
        n = c.execute(sql, (NAME,) if s.namespalte else ()).rowcount
        bericht.append((s.tabelle, n))
    return frei, specs, bericht


# =====================================================================================
#  Pruefen und Setzen
# =====================================================================================

def eine_zeile(c, sql, parameter):
    zeilen = c.execute(sql, parameter).fetchall()
    if len(zeilen) != 1:
        raise LookupError("%s %r: %d Zeilen statt einer" % (sql, parameter, len(zeilen)))
    return zeilen[0]


def spaltenwerte(c, tabelle, schluesselspalte, schluessel, namen):
    zeile = eine_zeile(c, "SELECT %s FROM %s WHERE %s = ?"
                       % (", ".join(namen), tabelle, schluesselspalte), (schluessel,))
    return dict(zip(namen, zeile))


def pruefe_vorlage(c):
    """1017 muss stehen, wie KU1 und KU2 es hinterlassen haben - und ungekoppelt."""
    name = eine_zeile(c, "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (VORLAGE,))[0]
    if name != VORLAGE_NAME:
        return "Vorlage %d heisst %r statt %r" % (VORLAGE, name, VORLAGE_NAME)
    gebaeude = [r[0] for r in c.execute("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", (VORLAGE,))]
    if gebaeude != [VORLAGE_GEBAEUDE]:
        return "Vorlage %d fuehrt die Gebaeude %r statt [%d]" % (VORLAGE, gebaeude, VORLAGE_GEBAEUDE)
    g = spaltenwerte(c, "Tab_Gebaeude", "ID", VORLAGE_GEBAEUDE,
                     list(SCHALTER_AUS) + LEER + list(GEBAEUDE_BLEIBT))
    for spalte, soll in list(SCHALTER_AUS.items()) + [(s, None) for s in LEER] + list(GEBAEUDE_BLEIBT.items()):
        if g[spalte] != soll:
            return "Vorlagengebaeude %d: %s = %r statt %r" % (VORLAGE_GEBAEUDE, spalte, g[spalte], soll)
    e = spaltenwerte(c, "Tab_Einstellungen", "ID_Projekt", VORLAGE, ["Anlagenkopplung"] + list(EINSTELLUNGEN_BLEIBT))
    if e["Anlagenkopplung"] is not None:
        return "Vorlage %d: Anlagenkopplung = %r statt NULL" % (VORLAGE, e["Anlagenkopplung"])
    for spalte, soll in EINSTELLUNGEN_BLEIBT.items():
        if e[spalte] != soll:
            return "Vorlage %d: %s = %r statt %r" % (VORLAGE, spalte, e[spalte], soll)
    # Die zwei Nachzuege des Programms nach dem Kopieren - bei 1017 ohne Gegenstand.
    if c.execute("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", (VORLAGE,)).fetchone()[0]:
        return "Vorlage %d fuehrt Kostenpositionen - der Ankernachzug des Programms fehlt hier" % VORLAGE
    if c.execute("SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?", (VORLAGE,)).fetchone()[0]:
        return "Vorlage %d fuehrt eine Speicherauslegung - deren Bezugsnachzug fehlt hier" % VORLAGE
    return None


def zellen(c):
    """(Tabelle, Schluesselspalte, Schluessel, Spalte, Ausgangswert, Zielwert) der Kopie."""
    gebaeude = [r[0] for r in c.execute("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", (NEU,))]
    if len(gebaeude) != 1:
        raise LookupError("Projekt %d fuehrt %d Gebaeude statt eines" % (NEU, len(gebaeude)))
    g = gebaeude[0]
    return g, [
        ("Tab_Projekt", "ID", NEU, "Beschreibung", "", BESCHREIBUNG),
        ("Tab_Einstellungen", "ID_Projekt", NEU, "Anlagenkopplung", None, AK1),
        ("Tab_Gebaeude", "ID", g, "Heizkreis_Aktiv", 0, 1),
        ("Tab_Gebaeude", "ID", g, "Uebergabe_Art", None, RADIATOR),
        ("Tab_Gebaeude", "ID", g, "Heizkurve_Aktiv", 0, 1),
        ("Tab_Gebaeude", "ID", g, "Kuehluebergabe_Aktiv", 0, 1),
        ("Tab_Gebaeude", "ID", g, "Kuehl_Uebergabe_Art", None, KUEHLDECKE),
    ]


def gegenprobe(c, specs):
    """Zeilenzahlen je kopierter Tabelle: Vorlage gegen Kopie."""
    fehler = 0
    for s in specs:
        if s.ergebnis:
            continue
        alt = c.execute("SELECT COUNT(*) FROM [%s] WHERE %s" % (s.tabelle, filter_fuer(s, VORLAGE))).fetchone()[0]
        neu = c.execute("SELECT COUNT(*) FROM [%s] WHERE %s" % (s.tabelle, filter_fuer(s, NEU))).fetchone()[0]
        if alt != neu:
            print("  ABWEICHUNG %-32s Vorlage %d, Kopie %d" % (s.tabelle, alt, neu))
            fehler += 1
    return fehler


def main():
    if len(sys.argv) < 2:
        print("Aufruf: anlagenkopplung_1047_referenzprojekt.py <Kenndaten_Test.sqlite>")
        return 2

    con = sqlite3.connect(sys.argv[1])
    con.isolation_level = None
    try:
        con.execute("PRAGMA foreign_keys = ON")

        grund = pruefe_vorlage(con)
        if grund:
            print(grund + " - Abbruch ohne Schreiben.")
            return 2
        print("Vorlage %d \"%s\": Gebaeude %d, ungekoppelt, Kuehlung und Kaelteerzeuger wie KU1/KU2."
              % (VORLAGE, VORLAGE_NAME, VORLAGE_GEBAEUDE))

        vorhanden = con.execute("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", (NAME,)).fetchall()
        belegt = con.execute("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (NEU,)).fetchall()
        if vorhanden and vorhanden[0][0] != NEU:
            print("\"%s\" steht unter Id %d statt %d - Abbruch ohne Schreiben." % (NAME, vorhanden[0][0], NEU))
            return 2
        if belegt and belegt[0][0] != NAME:
            print("Id %d ist von \"%s\" belegt - Abbruch ohne Schreiben." % (NEU, belegt[0][0]))
            return 2

        con.execute("BEGIN")
        try:
            specs = None
            if not vorhanden:
                neu_id, specs, bericht = duplizieren(con)
                if neu_id != NEU:
                    raise RuntimeError("die Kopie faellt auf Id %d statt %d" % (neu_id, NEU))
                print("Kopie %d -> %d \"%s\" (Kopierweg des Programms):" % (VORLAGE, NEU, NAME))
                for tabelle, n in bericht:
                    print("  %-32s %6d Zeilen" % (tabelle, n))
                print("  zusammen %d Zeilen in %d Tabellen" % (sum(n for _, n in bericht), len(bericht)))
            else:
                print("Projekt %d \"%s\" steht schon - keine Kopie." % (NEU, NAME))

            gebaeude, liste = zellen(con)
            offen = []
            for tabelle, schluesselspalte, schluessel, spalte, alt, neu in liste:
                ist = spaltenwerte(con, tabelle, schluesselspalte, schluessel, [spalte])[spalte]
                if ist == neu:
                    continue
                if ist != alt:
                    raise RuntimeError("%s %s=%s %s = %r ist weder %r noch Zielwert"
                                       % (tabelle, schluesselspalte, schluessel, spalte, ist, alt))
                offen.append((tabelle, schluesselspalte, schluessel, spalte, alt, neu))
            g = spaltenwerte(con, "Tab_Gebaeude", "ID", gebaeude, LEER + list(GEBAEUDE_BLEIBT))
            for spalte, soll in [(s, None) for s in LEER] + list(GEBAEUDE_BLEIBT.items()):
                if g[spalte] != soll:
                    raise RuntimeError("Gebaeude %d: %s = %r statt %r" % (gebaeude, spalte, g[spalte], soll))
            e = spaltenwerte(con, "Tab_Einstellungen", "ID_Projekt", NEU, list(EINSTELLUNGEN_BLEIBT))
            for spalte, soll in EINSTELLUNGEN_BLEIBT.items():
                if e[spalte] != soll:
                    raise RuntimeError("Einstellungen %d: %s = %r statt %r" % (NEU, spalte, e[spalte], soll))

            for tabelle, schluesselspalte, schluessel, spalte, alt, neu in offen:
                bedingung = spalte + " IS NULL" if alt is None else spalte + " = ?"
                parameter = (neu, schluessel) if alt is None else (neu, schluessel, alt)
                n = con.execute("UPDATE " + tabelle + " SET " + spalte + " = ? WHERE " + schluesselspalte +
                                " = ? AND " + bedingung, parameter).rowcount
                if n != 1:
                    raise RuntimeError("%s %s %s: %d Zeilen statt einer" % (tabelle, schluessel, spalte, n))
                print("gesetzt: %s %s=%s %s: %r -> %r" % (tabelle, schluesselspalte, schluessel, spalte, alt,
                                                          neu if spalte != "Beschreibung" else "(Zweck)"))
            if not offen:
                print("Alle %d Zellen stehen schon auf den Zielwerten - nichts zu tun." % len(liste))

            if specs is not None:
                fehler = gegenprobe(con, specs)
                if fehler:
                    raise RuntimeError("%d Tabelle(n) mit ungleicher Zeilenzahl" % fehler)
                print("Gegenprobe: alle Zeilenzahlen der Kopie gleich der Vorlage.")
            fremd = con.execute("PRAGMA foreign_key_check").fetchall()
            if fremd:
                raise RuntimeError("foreign_key_check: %d Zeilen" % len(fremd))
            con.execute("COMMIT")
        except Exception as ex:
            con.execute("ROLLBACK")
            print("%s - zurueckgerollt, nichts geschrieben." % ex)
            return 2

        pruefung = con.execute("PRAGMA integrity_check").fetchone()[0]
        fremd = con.execute("PRAGMA foreign_key_check").fetchall()
        print("integrity_check: %s; foreign_key_check: %d Zeilen" % (pruefung, len(fremd)))
        return 0 if pruefung == "ok" and not fremd else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
