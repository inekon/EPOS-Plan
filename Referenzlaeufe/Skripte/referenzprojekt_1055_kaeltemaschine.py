#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Legt das Referenzprojekt 1055 "Kältemaschine mit Kältespeicher" der Testdatenbank an - die Kopie des
Kuehlreferenzprojekts 1017, in der eine Kaeltemaschine mit Trocken-Rueckkuehler und eigenem Zaehler
samt Kaltwasserspeicher die Kaelte deckt (Stufe KU3, Auftrag KU3-4b, Anwenderentscheid E68; Kuehlkonzept
5.3, 4.6 und 10.4, Einfrierregel "gesaete Kaeltemaschinendaten").

AUSGANG. Die Kaeltemaschine ist eine Anlage (Typ 13, KU3-4a), rechnet mit Kennlinie, Rueckkuehlung und
freier Kuehlung (KU3-2), rechnet ihren Kaeltestrom ueber Kuehltraeger und Abrechnungsart ab (KU3-4d), und
der Kaltwasserspeicher ist eine Pufferzeile mit Verwendung "Kaelte" (KU3-5). Kein Referenzprojekt fuehrt
eines davon; der Rechenweg hatte damit kein Regressionsnetz ausser den Datenbanktests. 1017 ist das
Kuehlreferenzprojekt (Gebaeude 10599 nach VDI 6007, Kuehlung mit Haken, Kuehlsollwert 24 Grad C,
Kuehlleistungsgrenze 15 kW; reversible Waermepumpe auf Kaskadenplatz 3). Auf 1017 selbst waere die
Kaeltemaschine ein Basiswechsel des Kuehlreferenzfalls - deshalb eine KOPIE, und 1017 bleibt Zelle fuer
Zelle, wie es war. Das Paar 1017/1055 ist zugleich der Vergleich Waermepumpe gegen Kaeltemaschine.

WAS DIESES SKRIPT TUT (eine Transaktion, kein VACUUM).
  1. Die Kopie 1017 -> 1055 auf dem KOPIERWEG DES PROGRAMMS: ProjektDuplizierenCtrl.Duplizieren
     ("Projekt Speichern unter") nachgebildet wie im Skript von 1047 - Tabellenplan, Reihenfolge,
     Versatz je Tabelle, freie Projekt-Id, INSERT ... SELECT mit IIF(Spalte > 0, Spalte + Versatz,
     Spalte) -, mit den Karten des heutigen Kopierwegs (Kaeltemaschine, Zonen, Konditionierung,
     Sperrfenster, Tww-Konstruktor, Ergebnisgebaeude). Die zwei Nachzuege des Programms
     (Kostenpositionen, Speicherauslegung) haben bei 1017 nichts zu tun - geprueft, sonst Abbruch.
     Gegenprobe: jede kopierte Tabelle traegt fuer 1055 so viele Zeilen wie fuer 1017.
  2. Zwei Zellen der Kopie:
       Tab_Projekt 1055:     Beschreibung   ''  -> Zweck des Projekts (neutral)
       Tab_WP (Kopie der Waermepumpe 1017033):
                             Kuehlbetrieb   1   -> 0
     Die Waermepumpe heizt weiter auf Kaskadenplatz 3; sie kuehlt nicht mehr, damit die Kaelte allein
     die Kaeltemaschine deckt. Ihre Kuehlkennlinie und Kuehl_Vorlauf/Kuehl_Hilfsstromanteil reisen mit
     der Kopie und bleiben stehen (ohne Kuehlbetrieb wirkungslos).
  3. Die Kaeltemaschine wie KaeltemaschineAnlageCtrl.Anlegen und .Speichern sie schreiben:
       Tab_Kaeltemaschine     Projektkopie des Katalogsatzes "Kältemaschine 200 kW wassergekühlt mit
                              Trockenkühler" (ID_Stamm 2), auf ein Zehntel skaliert:
                                Bezeichner                  "Kältemaschine 20 kW mit Trockenkühler"
                                Nennkaelteleistung_kW       200  -> 20
                                Hilfsstrom_Rueckkuehlung_kW 6    -> 0,6
                                Nenn_EER 4,0, Kaeltemittel, Rueckkuehlart TROCKENKUEHLER,
                                Mindestteillast 20 %, Kaltwasser_Vorlauf_Min 5 Grad C: wie Katalog
                                Kuehl_Vorlauf               NULL (Kaltwasser = kleinste Stuetzstelle 6 Grad C)
                                Kuehl_Hilfsstromanteil      0,05
       Tab_Kenndaten_Kaeltemaschine  die sechs Punkte des Katalogsatzes, Kaelteleistung / 10, EER wie
                              Katalog (Rueckkuehlung 25/35/45 Grad C x Kaltwasser 6/12 Grad C)
       Tab_Energieanlagen     Typ 13 "Kältemaschine 20 kW", ID_Kaeltemaschine, Kaeltemaschine_Anzahl 1,
                              Kuehl_ID_Carrier 58 ("Elektrische Energie 2"), Kuehl_EigenerZaehler 1
  4. Der Kaltwasserspeicher wie PufferSpCtrl.ProjektPufferAnlegen ihn schreibt:
       Tab_Pufferspeicher     "Kaltwasserspeicher", Speichertyp Pufferspeicher, 2000 l, Verwendung
                              'Kaelte', Vorlauf 6 / Ruecklauf 12 Grad C, Bereitschaftsverlust 1 kWh/24 h,
                              Schwellen 10/95 %, Nachrang leer, Entladeprio 0, Reserve 10 %,
                              Klassen-Set ohne Waermeflag, Schichtung Vorbelegung (eine Schicht)
       Tab_Energieanlagen     Typ 12 mit ID_PUFFER (die Anlagenzeile des Puffers)

DIE WERTE UND WARUM.
  Trockenkuehler-Geraet, skaliert: Der Katalog fuehrt den Trockenkuehler nur mit 200 kW; seine
      Mindestteillast (20 % = 40 kW) laege ueber der ganzen Kuehllast von 1017 (Grenze 15 kW), die
      Maschine taktete jede Stunde. Ein Zehntel davon (20 kW) deckt die Spitze auch an der heissesten
      Stuetzstelle (Rueckkuehlung 45 Grad C, Kaltwasser 6 Grad C: 18 kW), taktet erst unter 4 kW und
      behaelt die Kennlinienform des Katalogs. Der Trockenkuehler (Rueckkuehlung = aussen + 10 K) ist
      die Rueckkuehlart, an der die freie Kuehlung haengt (KU3-2: Rueckkuehlung mindestens 3 K unter dem
      Kaltwasser). Der Name sagt die skalierte Leistung, ID_Stamm haelt die Herkunft.
  Hilfsstromanteil 0,05 und Hilfsstrom der Rueckkuehlung 0,6 kW: gesetzte runde Werte, damit die Basis
      beide Zuschlaege des Kaeltestroms traegt (K23 und KU3-2); NULL hiesse "kein Zuschlag".
  Kuehltraeger 58 mit eigenem Zaehler: 58 "Elektrische Energie 2" ist der zweite Stromtraeger, den 1017
      schon in energy_project_settings fuehrt (mit eigenen Preisen); Traeger des Netzbezugs ist 54
      "Strom Variante". Nur ein ABWEICHENDER Kuehltraeger macht die Abrechnungsart wirksam
      (Kaeltestromabrechnung.Abweichend, E34) - mit dem Traeger 54 waere der eigene Zaehler ein
      wirkungsloser Haken. So rechnet die Basis den Zweig "eigener Zaehler" (Menge, Grund- und
      Leistungspreis aus der Stromspitze der Maschine) mit.
  Kaltwasserspeicher 2 m3, 6/12 Grad C: Kapazitaet 2000 l x 1,16 Wh/(l K) x 6 K = 13,92 kWh, knapp
      eine Stunde der Kuehlspitze - genug, dass er an jedem Kuehltag laedt und entlaedt, klein genug,
      dass die Maschine die Last traegt. Das Paar ist das uebliche Kaltwasserpaar und die Vorgabe
      des Rechenwegs (ohne Paar 6/12). Bereitschaftsverlust 1 kWh/24 h: runder Wert fuer einen
      gedaemmten Kaltwasserspeicher (kleiner als beim Waermepuffer, weil die Temperaturdifferenz zur
      Umgebung klein ist). Schwellen 10/95 % und Reserve 10 % sind die Vorgaben des Dialogs.
  Kaskade: Tool_1 bis Tool_6 bleiben wie in 1017 (BHKW, Heizkessel, Waermepumpe, -, -, Stromspeicher).
      Sie ordnet nur die Waermeseite; die Kaeltemaschine hat keinen Kaskadenplatz (Kuehlkonzept 5.5:
      freie Kuehlung, Waermepumpen im Kuehlbetrieb, dann Kaeltemaschinen in Anlagenreihenfolge).

WIEDERHOLBAR. Steht Projekt 1055 schon, bricht das Skript mit Meldung ab und schreibt nichts (Rueckgabe
0, wenn es "Kältemaschine mit Kältespeicher" heisst, sonst 2). Weicht die Vorlage 1017 ab, faellt die
Kopie auf eine andere Id als 1055 oder misslingt eine Pruefung, rollt die Transaktion zurueck
(Rueckgabe 2).

PRUEFUNGEN. Vorlage (Name, Gebaeude, Kuehleingaben, Kaskade, Waermepumpe im Kuehlbetrieb, keine
Kaeltemaschine, kein Puffer), Katalogsatz (Bezeichner, Rueckkuehlart, sechs Punkte), Kuehltraeger am
Projekt und ungleich dem Netzbezugstraeger, Gegenprobe der Zeilenzahlen, foreign_key_check und
integrity_check. Kosten_Geaendert der Kopie wird nach den Schreibwegen geleert (die Trigger des
Kostenstempels stempeln die Uhrzeit des Laufs).

FOLGE. Die Zeilen sind gesaete Kaeltemaschinendaten eines Referenzprojekts (Einfrierregel "gesaete
Kaeltemaschinendaten", `Referenzlaeufe/LIESMICH.md`); gehalten von
`EPOS.Kern.Tests/KaeltemaschineReferenzprojektWacheTests`. Die Kopie ist zugleich ein Referenzprojekt
mit Gebaeude- und Kaeltedaten - die Regeln "gesaete Gebaeudedaten" und "gesaete Kaeltedaten" gelten
fuer 1055 wie fuer 1017.

Aufruf (Windows: `py`, sonst `python3`; vorher sichern):
    py Referenzlaeufe/Skripte/referenzprojekt_1055_kaeltemaschine.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

VORLAGE = 1017
VORLAGE_NAME = "WP_PV-Speicher"
VORLAGE_GEBAEUDE = 10599
VORLAGE_WP = 1017033
NEU = 1055
NAME = "Kältemaschine mit Kältespeicher"
BESCHREIBUNG = (
    "Referenzprojekt Kältemaschine: Kopie von Projekt 1017, die Wärmepumpe heizt nur, die Kälte "
    "deckt eine Kältemaschine 20 kW mit Trockenkühler (freie Kühlung) und eigenem Zähler, dazu ein "
    "Kaltwasserspeicher 2 m³ (6/12 °C). Es hält Kältemaschine, Rückkühlung, Kältestromabrechnung und "
    "Kältespeicher im Regressionsnetz.")

# Die Kaeltemaschine (KaeltemaschineSchema, KaeltemaschineAnlageSchema)
TYP_KAELTEMASCHINE = 13
TYP_PUFFER = 12
STAMM_BEZEICHNER = "Kältemaschine 200 kW wassergekühlt mit Trockenkühler"
STAMM_RUECKKUEHLART = "TROCKENKUEHLER"
MASSSTAB = 0.1
KM_BEZEICHNER = "Kältemaschine 20 kW mit Trockenkühler"
KM_NENN = 20.0
KM_HILFSSTROM_RUECKKUEHLUNG = 0.6
KM_KUEHL_HILFSSTROMANTEIL = 0.05
ANLAGE_KM = "Kältemaschine 20 kW"
KUEHLTRAEGER = 58             # "Elektrische Energie 2", zweiter Stromtraeger von 1017
NETZTRAEGER = 54              # "Strom Variante", Traeger des Netzbezugs von 1017

# Der Kaltwasserspeicher (ProjektPuffer, PufferSpCtrl.ProjektPufferAnlegen)
PUFFER_NAME = "Kaltwasserspeicher"
PUFFER_TYP = "Pufferspeicher"            # DbWerte.PSP_SPEICHERTYP_PUFFER
PUFFER_VERWENDUNG = "Kaelte"             # DbWerte.PSP_VERWENDUNG_KAELTE
PUFFER_VOLUMEN = 2000
PUFFER_VERLUST = 1.0
PUFFER_VORLAUF = 6
PUFFER_RUECKLAUF = 12
SCHWELLE_EIN = 10.0
SCHWELLE_AUS = 95.0
SCHWELLE_RESERVE = 10.0

WAERMEPUMPE = "Wärmepumpe"
KASKADE = {"Tool_1": "BHKW", "Tool_2": "Heizkessel", "Tool_3": WAERMEPUMPE, "Tool_4": "",
           "Tool_5": "", "Tool_6": "Stromspeicher"}
GEBAEUDE_KUEHLUNG = {"Kuehlung_Aktiv": 1, "Kuehl_Sollwert": 24.0, "Kuehlleistung_Max": 15.0}


# =====================================================================================
#  Der Kopierweg des Programms (ProjektDuplizierenCtrl), Wortlaut der Tabellen und Regeln
# =====================================================================================

KATALOG_TABELLEN = {"Tab_BrennstoffKategorien", "Tab_Typ_Energieanlagen", "Tab_KostenGruppenKatalog",
                    "Tab_KostenKomponente", "Tab_Kostenfaktor", "energy_carrier", "energy_conversion",
                    "pricing_model", "Tab_Applikation", "Einfügefehler"}

AUSNAHME_TABELLEN = {"Berichtskonfiguration", "Tab_ProjektPhotovoltaik"}

KATALOG_SPALTEN = {"ID_Type", "ID_Stamm", "StammID", "KomponentenID", "KategorieID", "carrier_id",
                   "ID_Energieträger", "ID_Umrechnung", "ID_Brennstoff", "ID_Nutzungsart",
                   "ID_Tagesgangsatz", "ID_Bedarfstag", "ID_Ausstattung", "ID_Betriebskalender",
                   "ID_Gebaeude_Stamm", "ID_ProjektRef"}

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
    "ID_Energieanlage": "Tab_Energieanlagen",
    "WQ_ID_Quellprofil": "Tab_Quellprofil", "ID_Quellprofil": "Tab_Quellprofil",
    "ID_Wechselrichter": "Tab_Wechselrichter",
    "ID_Kaeltemaschine": "Tab_Kaeltemaschine",
    "ID_Zone": "Tab_TwwZone",
    "ID_TwwProjekt": "Tab_TwwProjekt",
    "ID_Aufbau": "Tab_Bauteilaufbau", "ID_Baustoff": "Tab_Baustoff",
    "ID_Importquelle": "Tab_Importquelle", "ID_Bauteil": "Tab_Bauteil",
    "ID_Nachbarzone": "Tab_Zone", "ID_ZoneA": "Tab_Zone", "ID_ZoneB": "Tab_Zone",
    "ID_ErgebnisGebaeude": "Tab_ErgebnisGebaeude",
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
    "Tab_Baustoffzuordnung": {"ID_Baustoff": "Tab_Baustoff_STAMM"},
    "Tab_ErgebnisZone": {"ID_Zone": "Tab_Zone"},
}

_G = "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0})"
KINDER = {
    "Tab_Kenndaten": "ID_WP IN (SELECT ID FROM Tab_WP WHERE ID_Projekt = {0})",
    "Tab_Kenndaten_Kuehlung": "ID_WP IN (SELECT ID FROM Tab_WP WHERE ID_Projekt = {0})",
    "Tab_Kenndaten_Kaeltemaschine": "ID_Kaeltemaschine IN (SELECT ID FROM Tab_Kaeltemaschine WHERE ID_Projekt = {0})",
    "Tab_DBTagV": "ID_Gebaeude IN " + _G,
    "Tab_DBTagVDaten": "ID_TagV IN (SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN " + _G + ")",
    "Tab_WaermebedarfDaten": "ID_Ganglinie IN (SELECT ID FROM Tab_Waermebedarf WHERE ID_Projekt = {0})",
    "Tab_StromganglinieDaten": "ID_Ganglinie IN (SELECT ID FROM Tab_Stromganglinie WHERE ID_Projekt = {0})",
    "Tab_SolarganglinieDaten": "ID_Ganglinie IN (SELECT ID FROM Tab_Solarganglinie WHERE ID_Projekt = {0})",
    "Tab_Stromverbrauchertyp": "ID_Stromverbraucher IN (SELECT ID FROM Tab_Stromverbraucher WHERE ID_Projekt = {0})",
    "Z_AnlageSenke": "ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {0})",
    "Z_AnlagePufferVerbund": "ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {0})",
    "Z_AnlageStrang": "ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {0})",
    "Tab_Sperrfenster": "ID_Energieanlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {0})",
    "Tab_QuellprofilDaten": "ID_Quellprofil IN (SELECT ID FROM Tab_Quellprofil WHERE ID_Projekt = {0})",
    "Tab_TwwWohnungstyp": "ID_Zone IN (SELECT ID FROM Tab_TwwZone WHERE ID_Projekt = {0})",
    "Tab_TwwKonstruktorzeile": "ID_TwwProjekt IN (SELECT ID FROM Tab_TwwProjekt WHERE ID_Projekt = {0})",
    "Tab_Zone": "ID_Gebaeude IN " + _G,
    "Tab_Bauteil": "ID_Zone IN (SELECT ID FROM Tab_Zone WHERE ID_Gebaeude IN " + _G + ")",
    "Tab_Bauteilschicht": "ID_Aufbau IN (SELECT ID FROM Tab_Bauteilaufbau WHERE ID_Projekt = {0})",
    "Tab_Importquelle": "ID_Gebaeude IN " + _G,
    "Tab_Importzuordnung": "ID_Importquelle IN (SELECT ID FROM Tab_Importquelle WHERE ID_Gebaeude IN " + _G + ")",
    "Tab_Zonenluftstrom": "ID_ZoneA IN (SELECT ID FROM Tab_Zone WHERE ID_Gebaeude IN " + _G + ")",
    "Tab_ErgebnisZone": "ID_ErgebnisGebaeude IN (SELECT ID FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis IN "
                        "(SELECT ID FROM Tab_Ergebnis WHERE ID_Projekt = {0}))",
    "Tab_Konditionierungskalender": "ID_Gebaeude IN " + _G,
    "Tab_Konditionierungsvorgabe": "ID_Gebaeude IN " + _G,
    "Tab_Konditionierungsperiode": "ID_Kalender IN (SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IN "
                                   + _G + ")",
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


def anzahl(c, sql, parameter=()):
    return c.execute(sql, parameter).fetchone()[0]


def pruefe_vorlage(c):
    """1017 muss stehen, wie KU1 und KU2 es hinterlassen haben - ohne Kaeltemaschine und Puffer."""
    name = eine_zeile(c, "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (VORLAGE,))[0]
    if name != VORLAGE_NAME:
        return "Vorlage %d heisst %r statt %r" % (VORLAGE, name, VORLAGE_NAME)
    gebaeude = [r[0] for r in c.execute("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", (VORLAGE,))]
    if gebaeude != [VORLAGE_GEBAEUDE]:
        return "Vorlage %d fuehrt die Gebaeude %r statt [%d]" % (VORLAGE, gebaeude, VORLAGE_GEBAEUDE)
    g = spaltenwerte(c, "Tab_Gebaeude", "ID", VORLAGE_GEBAEUDE, list(GEBAEUDE_KUEHLUNG))
    for spalte, soll in GEBAEUDE_KUEHLUNG.items():
        if g[spalte] != soll:
            return "Vorlagengebaeude %d: %s = %r statt %r" % (VORLAGE_GEBAEUDE, spalte, g[spalte], soll)
    e = spaltenwerte(c, "Tab_Einstellungen", "ID_Projekt", VORLAGE, list(KASKADE) + ["Kuehlbetrieb"])
    if e["Kuehlbetrieb"] != 1:
        return "Vorlage %d: Kuehlbetrieb = %r statt 1" % (VORLAGE, e["Kuehlbetrieb"])
    for spalte, soll in KASKADE.items():
        if (e[spalte] or "") != soll:
            return "Vorlage %d: %s = %r statt %r" % (VORLAGE, spalte, e[spalte], soll)
    wp = spaltenwerte(c, "Tab_WP", "ID", VORLAGE_WP, ["ID_Projekt", "Kuehlbetrieb"])
    if wp != {"ID_Projekt": VORLAGE, "Kuehlbetrieb": 1}:
        return "Waermepumpe %d: %r statt Projekt %d im Kuehlbetrieb" % (VORLAGE_WP, wp, VORLAGE)
    if anzahl(c, "SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt schon eine Kaeltemaschine" % VORLAGE
    if anzahl(c, "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt schon einen Pufferspeicher" % VORLAGE
    traeger = [r[0] for r in c.execute(
        "SELECT ec.id FROM energy_project_settings s JOIN energy_carrier ec ON ec.id = s.[ID_Energieträger] "
        "WHERE s.ID_Projekt = ? AND ec.pricing_model = 'ELECTRICITY' ORDER BY ec.id", (VORLAGE,))]
    if traeger != [NETZTRAEGER, KUEHLTRAEGER]:
        return "Vorlage %d fuehrt die Stromtraeger %r statt [%d, %d]" % (VORLAGE, traeger, NETZTRAEGER, KUEHLTRAEGER)
    if anzahl(c, "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Carrier IS NOT NULL", (VORLAGE,)):
        return "Vorlage %d waehlt an einer Anlage einen Traeger - der Netzbezugstraeger waere nicht %d" % (VORLAGE, NETZTRAEGER)
    # Die zwei Nachzuege des Programms nach dem Kopieren - bei 1017 ohne Gegenstand.
    if anzahl(c, "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt Kostenpositionen - der Ankernachzug des Programms fehlt hier" % VORLAGE
    if anzahl(c, "SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt eine Speicherauslegung - deren Bezugsnachzug fehlt hier" % VORLAGE
    return None


def katalogsatz(c):
    """Der Katalogsatz des Trockenkuehlers samt seinen sechs Punkten."""
    kopf = ["Bezeichner", "Firma", "Typ", "Beschreibung", "Nennkaelteleistung_kW", "Nenn_EER", "Kaeltemittel",
            "Rueckkuehlart", "Mindestteillast_Prozent", "Hilfsstrom_Rueckkuehlung_kW", "Kaltwasser_Vorlauf_Min",
            "Modulkosten"]
    stamm = eine_zeile(c, "SELECT ID FROM Tab_Kaeltemaschine_STAMM WHERE Bezeichner = ?", (STAMM_BEZEICHNER,))[0]
    werte = spaltenwerte(c, "Tab_Kaeltemaschine_STAMM", "ID", stamm, kopf)
    if werte["Rueckkuehlart"] != STAMM_RUECKKUEHLART or werte["Nennkaelteleistung_kW"] != 200.0 \
            or werte["Hilfsstrom_Rueckkuehlung_kW"] != 6.0:
        raise RuntimeError("Katalogsatz %d weicht ab: %r" % (stamm, werte))
    punkte = c.execute("SELECT Rueckkuehltemperatur, Kaltwassertemperatur, EER, Kaelteleistung_kW "
                       "FROM Tab_Kenndaten_Kaeltemaschine_STAMM WHERE ID_Kaeltemaschine = ? ORDER BY ID",
                       (stamm,)).fetchall()
    if len(punkte) != 6:
        raise RuntimeError("Katalogsatz %d fuehrt %d Kennlinienpunkte statt 6" % (stamm, len(punkte)))
    return stamm, kopf, werte, punkte


def kaeltemaschine_anlegen(c):
    """KaeltemaschineCtrl.AusKatalogUebernehmen + Anlagenzeile (Anlegen) + Speichern - skaliert."""
    stamm, kopf, werte, punkte = katalogsatz(c)
    werte = dict(werte)
    werte["Bezeichner"] = KM_BEZEICHNER
    werte["Nennkaelteleistung_kW"] = KM_NENN
    werte["Hilfsstrom_Rueckkuehlung_kW"] = KM_HILFSSTROM_RUECKKUEHLUNG
    c.execute("INSERT INTO Tab_Kaeltemaschine (%s, ID_Projekt, ID_Stamm) VALUES (%s, ?, ?)"
              % (", ".join(kopf), ", ".join("?" for _ in kopf)), [werte[k] for k in kopf] + [NEU, stamm])
    km = c.execute("SELECT last_insert_rowid()").fetchone()[0]
    for rk, kw, eer, leistung in punkte:
        c.execute("INSERT INTO Tab_Kenndaten_Kaeltemaschine (ID_Kaeltemaschine, Rueckkuehltemperatur, "
                  "Kaltwassertemperatur, EER, Kaelteleistung_kW, ID_Projekt) VALUES (?, ?, ?, ?, ?, ?)",
                  (km, rk, kw, eer, round(leistung * MASSSTAB, 6), NEU))
    c.execute("INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_Type, ID_Kaeltemaschine, "
              "Kaeltemaschine_Anzahl) VALUES (?, ?, ?, ?, 1)", (NEU, ANLAGE_KM, TYP_KAELTEMASCHINE, km))
    anlage = c.execute("SELECT MAX(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Kaeltemaschine = ?",
                       (NEU, km)).fetchone()[0]
    c.execute("UPDATE Tab_Energieanlagen SET Kuehl_ID_Carrier = ?, Kuehl_EigenerZaehler = 1 WHERE ID = ? "
              "AND ID_Type = ?", (KUEHLTRAEGER, anlage, TYP_KAELTEMASCHINE))
    c.execute("UPDATE Tab_Kaeltemaschine SET Kuehl_Vorlauf = NULL, Kuehl_Hilfsstromanteil = ? WHERE ID = ?",
              (KM_KUEHL_HILFSSTROMANTEIL, km))
    print("Kaeltemaschine: Projektkopie %d (Stamm %d, x%.1f), %d Kennlinienpunkte, Anlage %d"
          % (km, stamm, MASSSTAB, len(punkte), anlage))
    return km, anlage


def puffer_anlegen(c):
    """PufferSpCtrl.ProjektPufferAnlegen mit Verwendung Kaelte und Vorbelegung der Schichtung."""
    pid = (anzahl(c, "SELECT MAX(ID) FROM Tab_Pufferspeicher") or 0) + 1
    c.execute("INSERT INTO Tab_Pufferspeicher (ID, ID_Projekt, Bezeichner, Hersteller, Speichertyp, Gesamtvolumen, "
              "Bereitschaftsverluste, Investitionskosten, Verwendung, Vorlauf, Ruecklauf, Schwelle_Ein, Schwelle_Aus, "
              "Schwelle_Aus_Nachrang, Entladeprio, Schwelle_Reserve) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
              (pid, NEU, PUFFER_NAME, "", PUFFER_TYP, PUFFER_VOLUMEN, PUFFER_VERLUST, 0.0, PUFFER_VERWENDUNG,
               PUFFER_VORLAUF, PUFFER_RUECKLAUF, SCHWELLE_EIN, SCHWELLE_AUS, None, 0, SCHWELLE_RESERVE))
    # Klassen-Set: der Kaeltespeicher traegt kein Waermeflag (KlassenSetBestimmen, KU3-5).
    c.execute("UPDATE Tab_Pufferspeicher SET Nutzung_Heizung = 0, Nutzung_Brauchwasser = 0, Nutzung_Prozess = 0 "
              "WHERE ID = ?", (pid,))
    # Schichtung und Optionen: die Vorbelegung, wie SchichtdatenSchreiben sie bei vorhandenen Spalten schreibt.
    c.execute("UPDATE Tab_Pufferspeicher SET Bereitschaft_Weg = NULL, Aufstellraum_Temperatur_C = NULL, "
              "Schicht_Anteile = NULL, Frischwassermodul = NULL, FWM_Graedigkeit_K = NULL WHERE ID = ?", (pid,))
    c.execute("UPDATE Tab_Pufferspeicher SET Schichten_Anzahl = 1, Hoehe = NULL, Lambda_Eff = NULL, T_Nutz_BW = NULL, "
              "Entnahme_Heizung = NULL, Entnahme_BW = NULL, Entnahme_Prozess = NULL, Ladeleistung_Max = 0.0, "
              "Entladeleistung_Max = 0.0 WHERE ID = ?", (pid,))
    # Die Anlagenzeile (ProjektPuffer.SQL_ANLAGENZEILE_INSERT, AnlagenzeileParameter).
    c.execute("INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_Type, Betriebsart, Sperrung, Sperrzeit_von, "
              "Sperrzeit_bis, Vorlauf, Rücklauf, Bivalenter_Betrieb, Abschaltpunkt, Nutzungszeit, Grenzleistung, "
              "Kollektormodulanzahl, PV_Leistung, Neigung, Azimut, ID_WP, ID_Solar, ID_PV, ID_SP, ID_Kessel, ID_BHKW, "
              "ID_PUFFER, ID_Carrier, Heizstab, Volumen, rendeMix, Solaranteil, WS_Ladeprio, WS_Ladegrenze, "
              "WS_Ladeprio_PV, WS_Ladeprio2, WS_Ladegrenze2) "
              "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
              (NEU, PUFFER_NAME, TYP_PUFFER, "", 0, 0, 0, 0, 0, 0, 0.0, 0, 0.0, 0, 0.0, 0, 0,
               None, None, None, None, None, None, pid, None, 0, 0.0, 0, 0, 0, 0.0, 0, 0, 0.0))
    anlage = anzahl(c, "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_PUFFER = ?", (NEU, pid))
    print("Kaltwasserspeicher: Puffer %d (%d l, %d/%d Grad C), Anlage %d"
          % (pid, PUFFER_VOLUMEN, PUFFER_VORLAUF, PUFFER_RUECKLAUF, anlage))
    return pid, anlage


def gegenprobe(c, specs):
    """Zeilenzahlen je kopierter Tabelle: Vorlage gegen Kopie (vor den Zusaetzen)."""
    fehler = 0
    for s in specs:
        if s.ergebnis:
            continue
        alt = anzahl(c, "SELECT COUNT(*) FROM [%s] WHERE %s" % (s.tabelle, filter_fuer(s, VORLAGE)))
        neu = anzahl(c, "SELECT COUNT(*) FROM [%s] WHERE %s" % (s.tabelle, filter_fuer(s, NEU)))
        if alt != neu:
            print("  ABWEICHUNG %-32s Vorlage %d, Kopie %d" % (s.tabelle, alt, neu))
            fehler += 1
    return fehler


def main():
    if len(sys.argv) < 2:
        print("Aufruf: referenzprojekt_1055_kaeltemaschine.py <Kenndaten_Test.sqlite>")
        return 2

    con = sqlite3.connect(sys.argv[1])
    con.isolation_level = None
    try:
        con.execute("PRAGMA foreign_keys = ON")

        belegt = con.execute("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (NEU,)).fetchall()
        if belegt:
            if belegt[0][0] == NAME:
                print("Projekt %d \"%s\" steht schon - Abbruch ohne Schreiben." % (NEU, NAME))
                return 0
            print("Id %d ist von \"%s\" belegt - Abbruch ohne Schreiben." % (NEU, belegt[0][0]))
            return 2
        if con.execute("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", (NAME,)).fetchall():
            print("\"%s\" steht unter einer anderen Id - Abbruch ohne Schreiben." % NAME)
            return 2

        grund = pruefe_vorlage(con)
        if grund:
            print(grund + " - Abbruch ohne Schreiben.")
            return 2
        print("Vorlage %d \"%s\": Gebaeude %d, Kuehlung, Waermepumpe im Kuehlbetrieb, keine Kaeltemaschine."
              % (VORLAGE, VORLAGE_NAME, VORLAGE_GEBAEUDE))

        con.execute("BEGIN")
        try:
            neu_id, specs, bericht = duplizieren(con)
            if neu_id != NEU:
                raise RuntimeError("die Kopie faellt auf Id %d statt %d" % (neu_id, NEU))
            print("Kopie %d -> %d \"%s\" (Kopierweg des Programms):" % (VORLAGE, NEU, NAME))
            for tabelle, n in bericht:
                print("  %-32s %6d Zeilen" % (tabelle, n))
            print("  zusammen %d Zeilen in %d Tabellen" % (sum(n for _, n in bericht), len(bericht)))
            if gegenprobe(con, specs):
                raise RuntimeError("Tabelle(n) mit ungleicher Zeilenzahl")
            print("Gegenprobe: alle Zeilenzahlen der Kopie gleich der Vorlage.")

            n = con.execute("UPDATE Tab_Projekt SET Beschreibung = ? WHERE ID = ? AND COALESCE(Beschreibung, '') = ''",
                            (BESCHREIBUNG, NEU)).rowcount
            if n != 1:
                raise RuntimeError("Tab_Projekt %d: Beschreibung nicht leer" % NEU)
            wp = [r[0] for r in con.execute("SELECT ID FROM Tab_WP WHERE ID_Projekt = ? AND Kuehlbetrieb = 1", (NEU,))]
            if len(wp) != 1:
                raise RuntimeError("Projekt %d fuehrt %d Waermepumpen im Kuehlbetrieb statt einer" % (NEU, len(wp)))
            con.execute("UPDATE Tab_WP SET Kuehlbetrieb = 0 WHERE ID = ?", (wp[0],))
            print("gesetzt: Tab_WP %d Kuehlbetrieb 1 -> 0" % wp[0])

            kaeltemaschine_anlegen(con)
            puffer_anlegen(con)

            # Die Schreibwege stempeln ueber die Trigger des Kostenstempels (KostenStempelSchema) die Uhrzeit
            # des Laufs in Kosten_Geaendert - leer wie bei jedem anderen Projekt der Testdatenbank, sonst
            # waere die Datei nicht wiederholbar (Muster 1051/1052). Aenderungs- und Erstelldatum sind die
            # der Vorlage.
            con.execute("UPDATE Tab_Projekt SET Kosten_Geaendert = NULL WHERE ID = ?", (NEU,))
            if anzahl(con, "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", (NEU,)) != 1:
                raise RuntimeError("Kosten_Geaendert von %d bleibt gestempelt" % NEU)

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
