#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Legt das ZWOELFTE Pruefprojekt der Referenzbasis an: 1045 "Pruefprojekt Ost/West Straenge"
(Anwenderentscheid W6-O-7 vom 06.09.2026, Konzept_Wechselrichter_EPOS-Plan.md Kapitel 12).

WOZU. Die elf Bestandsprojekte fuehren KEINE Strangzeile - und genau das ist ihr Nachweis
der Vorrangregel (Konzept 3.5). Bis hierher rechnete deshalb kein Referenzlauf den
Strangweg der Stufe S3 mit; ihn hielt allein der Pruefstand
`EPOS.Kern.Tests/PvStrangRechnungTests`. Dieses Projekt haengt den Strangweg ins
Regressionsnetz: Ost/West an EINEM knapp ausgelegten Geraet, damit Clipping, Kennlinie,
Nachtverbrauch UND das Modul je Strang (W6-O-6) in jedem Lauf mitrechnen.

WAS ES TUT.
  1. Tiefkopie des Projekts 1040 ("zwei Puffer je Kanal") auf die freie Id 1045 - samt
     Klimaregion, Klimadaten, Solarstunden, Gebaeude mit Tagesverteilung, Brauchwasser,
     Standardlastprofil (Viertelstundenreihe), Waermepumpe mit Kennfeld, Kessel, den
     fuenf Pufferspeichern und den Senkenzeilen. Damit hat der Eigenverbrauch etwas zu
     rechnen und der Netzbezug auch. NICHT kopiert werden die Ergebnistabellen
     (`Tab_Ergebnis*`) und die Modulkopie `Tab_PV` - das Modul wird unten neu gepflegt.
  2. Zwei SAUBERE Modulkopien in `Tab_PV`: Ablytek 6MN6A275 (Ost) und 6MN6A290 (West).
     Der Katalog `Tab_PV_STAMM` traegt in `alpha_SC`, `beta_OC` und `T_NOCT` den
     Kurzschlussstrom (Paket-A-Befund A1) - mit diesen Werten waere die Ampel gelb und
     die Zelltemperatur des erweiterten Modells Unsinn. Gepflegt werden deshalb genau
     diese drei Spalten (und die Technologie fuer den Huld-Satz); Leistung, U und I
     bleiben die des Katalogs.
  3. Den Wechselrichter "Muster 2500TL" aus Anhang A des Konzepts - in
     `Tab_Wechselrichter_STAMM` (ReadOnly = 1, Auslieferungskatalog) und als
     Projektkopie in `Tab_Wechselrichter`.
  4. Zwei Strangzeilen in `Z_AnlageStrang` an EINEM Geraet (Geraetenummer 1):
     Ost (Azimut -90, Neigung 10) und West (+90, 10), je 6 Module in Reihe, 1 parallel.
     Der Weststrang traegt ueber `ID_PV` SEIN eigenes Modul (W6-O-6).
  5. Die PV-Anlagenzeile auf den Katalogweg: `PV_Wechselrichterweg = KATALOG`,
     `PV_Modell = PV_MODELL_ERWEITERT`, `PV_Systemverluste = 3`, `PV_Leistung = 12`
     (die Modulzahl - Bezugsgroesse der Ampelpruefung P8).

ZWEI MPP-TRACKER STATT EINEM - die einzige begruendete Abweichung von Anhang A.
Anhang A rechnet EINEN Strang an einem Geraet mit einem MPP-Tracker. Zwei Straenge an
EINEM Tracker sind dort ausdruecklich die Gegenprobe: `2 x 9,55 A = 19,1 A > 12,0 A`
faellt in P4 ROT aus. Ein Ost/West-Feld an EINEM Geraet braucht deshalb zwei Tracker -
die Clipping-Grenze bleibt trotzdem EINE (Entscheidungsfrage Q7: gruppiert wird nach
Geraet, nicht nach Tracker). Alles Uebrige ist Zeile fuer Zeile Anhang A.

WOHER DIE TABELLENLISTE KOMMT. Die Tabellen mit `ID_Projekt` werden zur Laufzeit aus dem
Schema gelesen; die Kindtabellen ohne eigenes `ID_Projekt` und die Umsetzung der
Fremdschluessel stehen unten - beides Zeile fuer Zeile nach `ProjektDuplizierenCtrl`
(`KINDER`, `FK_MAP`, `KATALOG_SPALTEN`), damit hier keine zweite Wahrheit entsteht.
Zum Schluss vergleicht das Skript die Zeilenzahlen beider Projekte je Tabelle - fehlt
etwas, faellt es hier auf und nicht erst im Rechenergebnis.

AUFRUF (aus der Wurzel des Arbeitsbaums):

    python3 Referenzlaeufe/Skripte/pruefprojekt_1045_ost_west.py Referenzlaeufe/Kenndaten_Test.sqlite

Steht Projekt 1045 schon, bricht das Skript ab. Fuer einen zweiten Lauf die Datenbank aus
der Sicherung zuruecklegen - dann vergibt er dieselben Ids. (Ein `--neu` gibt es
bewusst nicht: Die Kindzeilen tragen kein `ID_Projekt`, ein halbherziges Loeschen
hinterliesse Waisen.)
"""

import os
import sqlite3
import sys

VORLAGE = 1040
NEU = 1045
NAME = "Prüfprojekt Ost/West Stränge"

# Tabellen mit ID_Projekt, die trotzdem NICHT mitkopiert werden.
#   Tab_Applikation  - anwendungsweit, kein Projektbezug (KATALOG_TABELLEN)
#   Tab_PV           - die Modulkopien werden unten neu gepflegt
#   Tab_Wechselrichter - die Vorlage fuehrt keine; die Geraetekopie entsteht unten
#   Tab_Projekt      - der Kopfsatz wird zuletzt einzeln geschrieben
#   Tab_Ergebnis*    - Ergebnisse; der Lauf schreibt sie selbst
NICHT_KOPIEREN = {"Tab_Applikation", "Tab_PV", "Tab_Wechselrichter", "Tab_Projekt",
                  "Berichtskonfiguration"}

# Kindtabellen ohne (verlaessliches) ID_Projekt: Filter auf der QUELLE, {p} = Vorlage-Id.
# Wortlaut aus ProjektDuplizierenCtrl.KINDER.
KINDER = {
    "Tab_Kenndaten":           "ID_WP IN (SELECT ID FROM Tab_WP WHERE ID_Projekt = {p})",
    "Tab_Kenndaten_Kuehlung":  "ID_WP IN (SELECT ID FROM Tab_WP WHERE ID_Projekt = {p})",
    "Tab_DBTagV":              "ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {p})",
    "Tab_DBTagVDaten":         "ID_TagV IN (SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN "
                               "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {p}))",
    "Tab_WaermebedarfDaten":   "ID_Ganglinie IN (SELECT ID FROM Tab_Waermebedarf WHERE ID_Projekt = {p})",
    "Tab_StromganglinieDaten": "ID_Ganglinie IN (SELECT ID FROM Tab_Stromganglinie WHERE ID_Projekt = {p})",
    "Tab_SolarganglinieDaten": "ID_Ganglinie IN (SELECT ID FROM Tab_Solarganglinie WHERE ID_Projekt = {p})",
    "Tab_Stromverbrauchertyp": "ID_Stromverbraucher IN (SELECT ID FROM Tab_Stromverbraucher WHERE ID_Projekt = {p})",
    "Tab_QuellprofilDaten":    "ID_Quellprofil IN (SELECT ID FROM Tab_Quellprofil WHERE ID_Projekt = {p})",
    "Z_AnlageSenke":           "ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {p})",
    "Z_AnlagePufferVerbund":   "ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {p})",
    "Tab_StromspeicherVariante": "ID_Energieanlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {p})",
    # Z_AnlageStrang steht hier NICHT: Die Vorlage fuehrt keine Strangzeile, und die
    # zwei Zeilen dieses Projekts werden unten von Hand gesetzt.
}

# Spalte -> Zieltabelle (ProjektDuplizierenCtrl.FK_MAP). Was hier nicht steht, bleibt
# unveraendert - das sind die Verweise auf Kataloge (KATALOG_SPALTEN: ID_Type,
# carrier_id, ID_Energieträger, ID_Umrechnung, ID_Brennstoff ...).
FK_MAP = {
    "ID_WP": "Tab_WP", "ID_SP": "Tab_Stromspeicher", "ID_PV": "Tab_PV",
    "ID_Solar": "Tab_Solarkollektoren", "ID_Kessel": "Tab_Heizkessel", "ID_BHKW": "Tab_BHKW",
    "ID_PUFFER": "Tab_Pufferspeicher", "ID_Puffer": "Tab_Pufferspeicher",
    "ID_Pufferspeicher": "Tab_Pufferspeicher",
    "WS_ID_Puffer": "Tab_Pufferspeicher", "WS_ID_Puffer2": "Tab_Pufferspeicher",
    "WQ_ID_Puffer": "Tab_Pufferspeicher",
    "ID_Klimaregion": "Tab_Klimaregion", "ID_ProjektGebaeude": "Z_ProjektGebaeude",
    "ID_Gebaeude": "Tab_Gebaeude", "ID_TagV": "Tab_DBTagV",
    "ID_Stromverbraucher": "Tab_Stromverbraucher", "ID_Prozesswaerme": "Tab_Prozesswaerme",
    "ID_Brauchwasser": "Tab_Brauchwasser",
    "ID_Anlage": "Tab_Energieanlagen", "ID_Energieanlage": "Tab_Energieanlagen",
    "ID_Senke": "Z_AnlageSenke",
    "WQ_ID_Quellprofil": "Tab_Quellprofil", "ID_Quellprofil": "Tab_Quellprofil",
    "ID_Wechselrichter": "Tab_Wechselrichter",
}

# Spalten, die beim Kopieren GELEERT werden. Die Vorlage haengt ihre PV-Anlage an ihre
# eigene Modulkopie; die wird hier nicht mitkopiert, sondern durch die zwei gepflegten
# Modulkopien ersetzt (`pv_anlage` setzt die Spalte danach).
LEEREN = {"Tab_Energieanlagen": ["ID_PV"]}

# Mehrdeutige Spaltennamen je Tabelle (ProjektDuplizierenCtrl.FK_OVERRIDE).
FK_OVERRIDE = {
    "Z_ProjektWaermebedarf":    {"ID_Ganglinie": "Tab_Waermebedarf"},
    "Z_ProjektStromganglinie":  {"ID_Ganglinie": "Tab_Stromganglinie"},
    "Z_ProjektSolarganglinie":  {"ID_Ganglinie": "Tab_Solarganglinie"},
    "Tab_WaermebedarfDaten":    {"ID_Ganglinie": "Tab_Waermebedarf"},
    "Tab_StromganglinieDaten":  {"ID_Ganglinie": "Tab_Stromganglinie"},
    "Tab_SolarganglinieDaten":  {"ID_Ganglinie": "Tab_Solarganglinie"},
}

# Die zwei Modulkopien. Leistung, Wirkungsgrad, U, I und Masse sind die des Katalogs
# Tab_PV_STAMM; gepflegt sind alpha_SC, beta_OC, T_NOCT und Technologie.
MODULE = [
    dict(schluessel="OST", bezeichner="Ablytek 6MN6A275", firma="Ablytek",
         beschreibung="Prüfprojekt W6-O-7, Strang Ost. alpha_SC, beta_OC und T_NOCT gepflegt "
                      "(Anhang A des Wechselrichterkonzepts); der Katalog trägt dort den "
                      "Kurzschlussstrom (Paket-A-Befund A1).",
         leistung=275.1912, wirkungsgrad=16.9140135218193,
         u_mpp=30.99, u_leerlauf=38.97, i_mpp=8.88, i_kurzschluss=9.42,
         alpha_sc=0.0047, beta_oc=-0.118, gamma_pmp=-0.4509, t_noct=45.0,
         laenge=1.64, breite=0.992, modulkosten=0.0, technologie="C_SI"),
    dict(schluessel="WEST", bezeichner="Ablytek 6MN6A290", firma="Ablytek",
         beschreibung="Prüfprojekt W6-O-7, Strang West — das eigene Modul der Strangzeile "
                      "(W6-O-6). alpha_SC, beta_OC und T_NOCT gepflegt.",
         leistung=290.016, wirkungsgrad=17.8251997541487,
         u_mpp=31.8, u_leerlauf=39.99, i_mpp=9.12, i_kurzschluss=9.67,
         alpha_sc=0.0048, beta_oc=-0.128, gamma_pmp=-0.4509, t_noct=45.0,
         laenge=1.64, breite=0.992, modulkosten=0.0, technologie="C_SI"),
]

# Muster 2500TL, Anhang A des Konzepts - bis auf Anzahl_Mppt (siehe Kopfkommentar).
GERAET = dict(
    Bezeichner="Muster 2500TL",
    Firma="Muster",
    Beschreibung="Prüfmuster aus Anhang A des Wechselrichterkonzepts (W6). Zwei MPP-Tracker "
                 "statt einem: Zwei Stränge an EINEM Tracker sind dort die Gegenprobe zu P4 "
                 "(19,1 A > 12,0 A). Die Clipping-Grenze bleibt eine (Q7).",
    P_AC_Nenn=2.50, S_AC_Max=2.50, P_DC_Max=3.75,
    U_Mpp_Min=80.0, U_Mpp_Max=500.0, U_Dc_Max=600.0, U_Start=120.0, I_Dc_Max=12.0,
    Anzahl_Mppt=2, Straenge_Je_Mppt=2,
    Eta05=0.900, Eta10=0.940, Eta20=0.962, Eta30=0.970, Eta50=0.975, Eta100=0.970,
    Eta_Euro=0.968, Eta_Max=0.975,
    P_Standby=10.0, P_Nacht=2.0, Kosten=1200.0,
    Herkunft="HAND",
)

# FLACHDACH-AUFSTAENDERUNG, 10 GRAD - die zweite begruendete Abweichung von der Vorgabe
# (dort standen 30 Grad). Bei 30 Grad stehen die zwei Ebenen so steil, dass ihre
# Tagesgaenge kaum ueberlappen: Die Anlagenspitze bleibt bei 2,33 kW und das Geraet
# klippt in KEINER Stunde - genau die Kennzahl, die dieses Projekt tragen soll, waere
# null. Zehn Grad ist die uebliche Ost/West-Aufstaenderung eines Flachdachs und der
# Grund, warum so ein Feld ueberhaupt an EINEM knapp ausgelegten Geraet haengt: Die
# Mittagsspitze der zwei Ebenen faellt zusammen, das Geraet kappt. Gemessen: 5 Stunden
# an der 2,50-kW-Grenze, 2,0 kWh/a. Mehr gibt die Ampel nicht her - ueber DC/AC 1,5
# faellt P6 auf Gelb (siehe Bericht zu W6-O-7).
NEIGUNG = 10

# Die zwei Straenge. Neigung und Azimut stehen AN DER STRANGZEILE - das ist der
# Ost/West-Fall (Konzept 3.4, Entwurfsentscheidung 2).
STRAENGE = [
    dict(Rang=1, Bezeichner="Dach Ost", Geraetenummer=1, Mppt=1,
         Module_Reihe=6, Straenge_Parallel=1, Neigung=NEIGUNG, Azimut=-90, Modul="OST"),
    dict(Rang=2, Bezeichner="Dach West", Geraetenummer=1, Mppt=2,
         Module_Reihe=6, Straenge_Parallel=1, Neigung=NEIGUNG, Azimut=90, Modul="WEST"),
]

PV_SYSTEMVERLUSTE = 3.0
PV_MODELL_ERWEITERT = "PV_MODELL_ERWEITERT"     # DbWerte.PV_MODELL_ERWEITERT
PV_WR_WEG_KATALOG = "KATALOG"                   # DbWerte.PV_WR_WEG_KATALOG
ID_TYPE_PV = 3                                  # Tab_Typ_Energieanlagen: Photovoltaik


# =====================================================================================
#  Kleinigkeiten
# =====================================================================================

def spalten(c, tabelle):
    return [r[1] for r in c.execute('PRAGMA table_info("%s")' % tabelle)]


def idspalte(c, tabelle):
    """Die Schluesselspalte - fast ueberall `ID`, in `energy_price` `id`, in
    `Z_ProjektWaermebedarf` `ID_Z`. Genommen wird die erste Spalte des
    Primaerschluessels."""
    felder = list(c.execute('PRAGMA table_info("%s")' % tabelle))
    for r in felder:
        if r[5]:
            return r[1]
    raise SystemExit("Tabelle %s hat keinen Primaerschluessel." % tabelle)


def naechste_id(c, tabelle, spalte=None):
    spalte = spalte or idspalte(c, tabelle)
    wert = c.execute('SELECT MAX("%s") FROM "%s"' % (spalte, tabelle)).fetchone()[0]
    return (wert or 0) + 1


def basistabellen(c):
    return [r[0] for r in c.execute(
        "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name")]


def plan(c):
    """Die Bausteine der Kopie: (Tabelle, Filter auf der Quelle). Reihenfolge egal -
    die Ids werden in einem eigenen Durchgang vorab vergeben."""
    bausteine = []
    for t in basistabellen(c):
        if t.endswith("_STAMM") or t in NICHT_KOPIEREN or t.startswith("Tab_Ergebnis"):
            continue
        if t in KINDER:
            bausteine.append((t, KINDER[t].format(p=VORLAGE)))
        elif "ID_Projekt" in spalten(c, t):
            bausteine.append((t, "ID_Projekt = %d" % VORLAGE))
    return bausteine


# =====================================================================================
#  Kopieren
# =====================================================================================

def kopieren(c):
    """Tiefkopie VORLAGE -> NEU. Rueckgabe: Abbildung (Tabelle, alte Id) -> neue Id."""
    bausteine = plan(c)
    karte = {}
    zeilen = {}

    # 1. Durchgang: lesen und neue Ids vergeben.
    for t, filter_ in bausteine:
        sp = spalten(c, t)
        pk = idspalte(c, t)
        rows = [dict(zip(sp, r)) for r in
                c.execute('SELECT * FROM "%s" WHERE %s ORDER BY "%s"' % (t, filter_, pk))]
        zeilen[t] = rows
        naechste = naechste_id(c, t, pk)
        for i, row in enumerate(rows):
            karte[(t, row[pk])] = naechste + i

    # 2. Durchgang: schreiben, Fremdschluessel umsetzen.
    for t, _ in bausteine:
        if not zeilen[t]:
            continue
        sp = spalten(c, t)
        pk = idspalte(c, t)
        besonders = FK_OVERRIDE.get(t, {})
        leeren = LEEREN.get(t, [])
        sql = ('INSERT INTO "%s" (%s) VALUES (%s)'
               % (t, ",".join('"%s"' % s for s in sp), ",".join("?" * len(sp))))

        for row in zeilen[t]:
            neu = dict(row)
            neu[pk] = karte[(t, row[pk])]
            if "ID_Projekt" in neu and neu["ID_Projekt"]:
                neu["ID_Projekt"] = NEU

            for feld in leeren:
                neu[feld] = None

            for feld in sp:
                if feld == pk or feld == "ID_Projekt" or feld in leeren:
                    continue
                ziel = besonders.get(feld) or FK_MAP.get(feld)
                if ziel is None:
                    continue
                alt = row.get(feld)
                if alt in (None, 0):
                    continue
                if (ziel, alt) not in karte:
                    raise SystemExit("Verweis %s.%s = %s zeigt auf %s - dort wird nichts kopiert."
                                     % (t, feld, alt, ziel))
                neu[feld] = karte[(ziel, alt)]

            c.execute(sql, [neu[s] for s in sp])

        print("  %-30s %6d Zeilen" % (t, len(zeilen[t])))

    return karte


def kopfsatz(c, karte):
    sp = spalten(c, "Tab_Projekt")
    row = dict(zip(sp, c.execute("SELECT * FROM Tab_Projekt WHERE ID = ?", (VORLAGE,)).fetchone()))
    row["ID"] = NEU
    row["Projektname"] = NAME
    row["Beschreibung"] = (
        "Zwölftes Prüfprojekt der Referenzbasis (Anwenderentscheid W6-O-7, 06.09.2026): "
        "Ost/West-Feld an EINEM knapp ausgelegten Wechselrichter. Es hält den Strangweg "
        "der Stufe S3 im Regressionsnetz — Clipping, Kennlinie, Nachtverbrauch und das "
        "Modul je Strang. Vorlage: Projekt %d." % VORLAGE)
    row["ID_Klimaregion"] = karte[("Tab_Klimaregion", row["ID_Klimaregion"])]
    c.execute("INSERT INTO Tab_Projekt (%s) VALUES (%s)"
              % (",".join('"%s"' % s for s in sp), ",".join("?" * len(sp))),
              [row[s] for s in sp])
    print('  Tab_Projekt %d "%s", Klimaregion %d' % (NEU, NAME, row["ID_Klimaregion"]))


def modulkopien(c):
    """Die zwei sauberen Projektkopien in Tab_PV; Rueckgabe: Schluessel -> Id."""
    ids = {}
    naechste = naechste_id(c, "Tab_PV")
    for i, m in enumerate(MODULE):
        neu = naechste + i
        c.execute(
            "INSERT INTO Tab_PV (ID, ID_Projekt, Bezeichner, Firma, Beschreibung, Leistung, "
            "Wirkungsgrad, U_Mpp, U_Leerlauf, I_Mpp, I_Kurzschluss, alpha_SC, beta_OC, "
            "gamma_PMP, T_NOCT, Laenge, Breite, Modulkosten, Technologie) "
            "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
            (neu, NEU, m["bezeichner"], m["firma"], m["beschreibung"], m["leistung"],
             m["wirkungsgrad"], m["u_mpp"], m["u_leerlauf"], m["i_mpp"], m["i_kurzschluss"],
             m["alpha_sc"], m["beta_oc"], m["gamma_pmp"], m["t_noct"],
             m["laenge"], m["breite"], m["modulkosten"], m["technologie"]))
        ids[m["schluessel"]] = neu
        print("  Tab_PV %d = %s (%.4f W)" % (neu, m["bezeichner"], m["leistung"]))
    return ids


def wechselrichter(c):
    """Katalogsatz (ReadOnly) und Projektkopie; Rueckgabe: Id der Projektkopie."""
    felder = list(GERAET.keys())
    werte = [GERAET[f] for f in felder]

    stamm = naechste_id(c, "Tab_Wechselrichter_STAMM")
    c.execute('INSERT INTO Tab_Wechselrichter_STAMM ("ID", %s, "ReadOnly") VALUES (?, %s, 1)'
              % (",".join('"%s"' % f for f in felder), ",".join("?" * len(felder))),
              [stamm] + werte)

    kopie = naechste_id(c, "Tab_Wechselrichter")
    c.execute('INSERT INTO Tab_Wechselrichter ("ID", "ID_Projekt", %s) VALUES (?, ?, %s)'
              % (",".join('"%s"' % f for f in felder), ",".join("?" * len(felder))),
              [kopie, NEU] + werte)

    print("  Tab_Wechselrichter_STAMM %d / Tab_Wechselrichter %d = %s"
          % (stamm, kopie, GERAET["Bezeichner"]))
    return kopie


def pv_anlage(c, modulIds, idGeraet):
    """Die PV-Anlagenzeile auf den Katalogweg setzen und die zwei Straenge anlegen."""
    treffer = [r[0] for r in c.execute(
        "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? ORDER BY ID",
        (NEU, ID_TYPE_PV))]
    if len(treffer) != 1:
        raise SystemExit("Erwartet wird GENAU eine PV-Anlagenzeile, gefunden: %d." % len(treffer))
    idAnlage = treffer[0]

    modulzahl = sum(s["Module_Reihe"] * s["Straenge_Parallel"] for s in STRAENGE)

    c.execute(
        "UPDATE Tab_Energieanlagen SET Bezeichner = ?, ID_PV = ?, PV_Leistung = ?, "
        "Neigung = ?, Azimut = ?, PV_Modell = ?, PV_Systemverluste = ?, "
        "PV_Wechselrichterweg = ? WHERE ID = ?",
        ("PV Ost/West an einem Wechselrichter", modulIds["OST"], float(modulzahl),
         NEIGUNG, 0, PV_MODELL_ERWEITERT, PV_SYSTEMVERLUSTE, PV_WR_WEG_KATALOG, idAnlage))

    naechste = naechste_id(c, "Z_AnlageStrang")
    for i, s in enumerate(STRAENGE):
        c.execute(
            "INSERT INTO Z_AnlageStrang (ID, ID_Anlage, Rang, Bezeichner, ID_Wechselrichter, "
            "Geraetenummer, Mppt, Module_Reihe, Straenge_Parallel, Neigung, Azimut, ID_PV) "
            "VALUES (?,?,?,?,?,?,?,?,?,?,?,?)",
            (naechste + i, idAnlage, s["Rang"], s["Bezeichner"], idGeraet,
             s["Geraetenummer"], s["Mppt"], s["Module_Reihe"], s["Straenge_Parallel"],
             s["Neigung"], s["Azimut"], modulIds[s["Modul"]]))
        print("  Z_AnlageStrang %d: %-9s Modul %d, MPPT %d, Azimut %+d"
              % (naechste + i, s["Bezeichner"], modulIds[s["Modul"]], s["Mppt"], s["Azimut"]))

    kwp = sum(s["Module_Reihe"] * s["Straenge_Parallel"] *
              next(m["leistung"] for m in MODULE if m["schluessel"] == s["Modul"]) / 1000.0
              for s in STRAENGE)
    print("  Anlage %d: %d Module, %.4f kWp gegen %.2f kW -> DC/AC = %.4f"
          % (idAnlage, modulzahl, kwp, GERAET["P_AC_Nenn"], kwp / GERAET["P_AC_Nenn"]))
    return idAnlage


def gegenprobe(c, idAnlage):
    """Zeilenzahlen je Tabelle: Vorlage gegen Kopie. Ungleiches wird gemeldet."""
    print("Gegenprobe (Vorlage %d / Kopie %d):" % (VORLAGE, NEU))
    fehler = 0
    for t, filter_ in plan(c):
        alt = c.execute('SELECT COUNT(*) FROM "%s" WHERE %s' % (t, filter_)).fetchone()[0]
        neuFilter = KINDER[t].format(p=NEU) if t in KINDER else "ID_Projekt = %d" % NEU
        neu = c.execute('SELECT COUNT(*) FROM "%s" WHERE %s' % (t, neuFilter)).fetchone()[0]
        if alt != neu:
            print("  ABWEICHUNG %-28s Vorlage %d, Kopie %d" % (t, alt, neu))
            fehler += 1
    strang = c.execute("SELECT COUNT(*) FROM Z_AnlageStrang WHERE ID_Anlage = ?", (idAnlage,)).fetchone()[0]
    module = c.execute("SELECT COUNT(*) FROM Tab_PV WHERE ID_Projekt = ?", (NEU,)).fetchone()[0]
    geraete = c.execute("SELECT COUNT(*) FROM Tab_Wechselrichter WHERE ID_Projekt = ?", (NEU,)).fetchone()[0]
    print("  Straenge %d, Modulkopien %d, Geraetekopien %d" % (strang, module, geraete))
    if fehler:
        raise SystemExit("%d Tabelle(n) weichen ab - die Kopie ist unvollstaendig." % fehler)
    print("  alle Zeilenzahlen gleich.")


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    pfad = sys.argv[1]

    if not os.path.exists(pfad):
        print("Datei nicht gefunden: " + pfad)
        return 2

    c = sqlite3.connect(pfad)
    c.execute("PRAGMA foreign_keys = OFF")

    if c.execute("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?", (NEU,)).fetchone()[0]:
        print("Projekt %d steht bereits in Tab_Projekt. Fuer einen Neulauf die Datenbank "
              "aus der Sicherung zuruecklegen." % NEU)
        return 1

    print("Tiefkopie Projekt %d -> %d:" % (VORLAGE, NEU))
    karte = kopieren(c)
    kopfsatz(c, karte)

    print("Module, Geraet und Straenge:")
    modulIds = modulkopien(c)
    idGeraet = wechselrichter(c)
    idAnlage = pv_anlage(c, modulIds, idGeraet)

    c.commit()
    gegenprobe(c, idAnlage)
    c.execute("VACUUM")
    c.close()
    print("Fertig.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
