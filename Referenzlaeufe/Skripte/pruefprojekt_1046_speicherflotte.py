#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Legt das DREIZEHNTE Pruefprojekt der Referenzbasis an: 1046 "Pruefprojekt Speicherflotte"
(Anwenderentscheid SP-O-8 vom 11.09.2026, Doku_Mehrspeicher_Konzept_und_Umsetzung.md).

WOZU. Das Mehrspeicherkonzept hat mit `SpeicherEngine/Flotten*.cs`,
`EPOS.Kern/Controller/SpeicherFlotten*Ctrl.cs` und der Weiche in
`SimulationControl.Stromspeicher.cs` einen ZWEITEN Speicherpfad in den gewoehnlichen
Projektlauf gelegt. Keines der zwoelf Referenzprojekte betritt ihn: Sie fahren alle die
Einzelanlage ueber `StromspeicherSimCtrl.RechneAktiveVariante`. Der Flottenpfad hatte
damit kein Regressionsnetz - eine stille Aenderung an Verteilung, Reserve, Wirkungsgrad
oder Netzbilanz waere in keinem Referenzlauf aufgefallen, sondern allein in den
Pruefstaenden von `SpeicherEngine.Tests`. Dieses Projekt haengt den Flottenpfad ins
Regressionsnetz.

WARUM 1007 ALS VORLAGE. Gesucht war ein Bestandsprojekt mit echtem Strombedarf UND
Photovoltaik. Gemessen an der Basis `2026-09-07_R6_PvKoeffizienten` bleiben drei uebrig:

    Projekt | Bezugsspitze | PV-Ueberschuss | Stromspeicher im Projekt
    1007    |  19,776 kW   |    887,6 kWh   | ja (vier SP-Anlagen, Variante 1 aktiv)
    1040    |   8,370 kW   |  2 272,7 kWh   | nein
    1045    |   8,372 kW   |    674,8 kWh   | nein

Genommen ist **1007**. Das Betriebsziel dieses Pruefprojekts ist `PeakShaving`, und dafuer
zaehlt die BEZUGSSPITZE: Sie ist bei 1007 mehr als doppelt so hoch wie bei den beiden
anderen, und nur dort laesst sich ein wirtschaftliches Peak-Ziel setzen, das deutlich
unter der Spitze liegt und von einer Flotte realistischer Groesse auch erreicht werden
kann. 1040 hat zwar den groesseren PV-Ueberschuss, aber eine Spitze von 8,4 kW - ein
Peak-Ziel darunter waere eine Rechenuebung ohne Aussage. Dazu kommt ein zweiter Grund:
1007 FUEHRT bereits Stromspeicheranlagen. Damit rechnet dasselbe Projekt mit
deaktivierter Flotte den EINZELpfad und mit aktivierter Flotte den FLOTTENpfad - genau
die Gegenprobe, die das protokoll.txt der Basis R7 fuehrt.

WAS ES TUT.
  1. Tiefkopie des Projekts 1007 auf die freie Id 1046 - samt Klimaregion, Klimadaten,
     Solarstunden, Gebaeude mit Tagesverteilung, Brauchwasser, Stromverbrauchern,
     Waermepumpe mit Kennfeld, Kessel, Pufferspeichern, den vier Stromspeicheranlagen
     samt ihren Varianten, den zwei PV-Modulkopien und den Senkenzeilen. NICHT kopiert
     werden die Ergebnistabellen (`Tab_Ergebnis*`), die Berichtskonfiguration (gilt je
     Stammprojekt) und `Tab_SpeicherAuslegung` - der Flottenstand entsteht unten neu.
  2. Den reservierten Projektflottenstand `@Projektflotte` in `Tab_SpeicherAuslegung`:
     Anlagenbezug NULL, `Daten` als `gz1:`-Nutzlast (gzip + Base64 des JSON von
     `SpeicherOptimierungEingaben`, Format `SpeicherAuslegungCtrl.Serialisieren`),
     `FlotteImProjektAktiv = true`. Damit schaltet
     `SpeicherFlottenProjektCtrl.IstAktiv` den Flottenpfad im gewoehnlichen Projektlauf
     ein, ohne dass ein Dialog oder ein Auslegungslauf gelaufen sein muss.
  3. `VACUUM`.

DIE FLOTTE. Zwei physisch gleichzeitig betriebene AC-Einheiten UNTERSCHIEDLICHER Groesse
mit getrennten Richtungsleistungen, getrennten Richtungswirkungsgraden, eigenen
SoC-Grenzen, eigener Peak-Reserve und eigenem Hilfsverbrauch:

    Einheit | C [kWh] | P_lade  | P_entlade | eta_c | eta_d | SoC-Band  | Reserve
    A       |   24,0  | 10,0 kW |  12,0 kW  | 0,96  | 0,94  | 0,05-0,95 | 2,4 kWh
    B       |   16,0  |  6,0 kW |   7,0 kW  | 0,93  | 0,91  | 0,10-0,90 | 1,2 kWh

ABWEICHUNG VON DER GROESSENANGABE DES AUFTRAGS - UND IHR GRUND. Die Empfehlung nannte
"z. B. 50 kW/100 kWh und 30 kW/90 kWh", zusammen 190 kWh an 80 kW. Das sind Groessen fuer
einen Industriestandort; Projekt 1007 hat eine Bezugsspitze von 19,8 kW und einen
Jahresbezug von 50,5 MWh. Eine 80-kW-Flotte davor waere in JEDEM Intervall unbegrenzt -
Verteilung, SoC-Grenzen, Reserve und Richtungsleistungen haetten nie eine Wirkung, und
genau diese Wege soll das Projekt halten. Die Flotte ist deshalb auf die Vorlage
massstaeblich verkleinert (40 kWh an 19 kW); die GESTALT ist die der Empfehlung: zwei
Einheiten im Verhaeltnis 3:2 der Kapazitaet und 12:7 der Entladeleistung, getrennte
Richtungsleistungen, getrennte Wirkungsgrade, getrennte SoC-Grenzen, getrennte Reserve,
getrennter Hilfsverbrauch.

DIE GROESSE IST AM PEAK-ZIEL GEMESSEN, nicht geraten. Aus der Reihe
`reststrom_viertelstunde.csv` des Projekts 1007 in der Basis R6 laesst sich je Schwelle T
die LAENGSTE zusammenhaengende Energie ueber T ablesen - das ist die Energie, die eine
Flotte vorhalten muss, um T zu halten:

    T [kW]         | 12     | 14    | 15    | 16    | 17    | 18
    Exkursion [kWh]| 101,5  | 67,5  | 50,5  | 33,5  | 19,5  |  8,5

Die Flotte hat 34,4 kWh nutzbar (0,90 * 24 + 0,80 * 16) und passt damit zu T = 16,0 kW.
Ein tieferes Ziel waere keine schaerfere Probe, sondern eine stumpfe: Bei T = 12 entlaedt
die reaktive Regel schon an jedem mittleren Tag und steht am Jahreshoechstwert leer - die
Bezugsspitze bleibt dann unveraendert bei 19,8 kW (gemessen). Bei T = 18 wiederum arbeitet
die Flotte kaum noch (rund vier Vollzyklen im Jahr). T = 16,0 trifft die Mitte: Die Spitze
faellt auf 16,73 kW, das Ziel wird in 20 von 35 040 Intervallen knapp verfehlt - und genau
diese 20 Intervalle halten den Weg der PEAK-RESERVE im Netz, die nur bei einer
tatsaechlichen Ueberschreitung freigegeben wird.

BETRIEB. Reaktives Ziel `PeakShaving` gegen ein wirtschaftliches Peak-Ziel von 16,0 kW -
unterhalb der Bezugsspitze von 19,8 kW. Verteilung `Kaskade`: Einheit A wird zuerst
ausgelastet, B deckt den Rest; die Reihenfolge ist die der Konfiguration. Netzladung ist
freigegeben (sonst laedt die Flotte allein aus dem PV-Ueberschuss und stuende die meiste
Zeit leer - Peak Shaving waere dann nicht gepruefte Physik, sondern ein Zufall des
Wetters), Batterieexport nicht. KEIN planendes Ziel: `PvPlanung`, `Arbitrage` und
`MultiUse` brauchen einen `IFlottenPlaner` und damit Google OR-Tools. Der
plattformfreie `EPOS.Referenzlauf` bindet die nicht ein und soll es auch nicht - ein
MILP-Ergebnis waere weder auf allen Plattformen bitgleich noch ohne Zeitlimit
reproduzierbar. Die zwei reaktiven Ziele kommen ohne Planer aus und sind deterministisch.

KEINE LEBENSDAUERKURVE. `FlottenEinheit.RainflowKurve` bleibt bei beiden Einheiten LEER.
Die Rainflow-Auswertung zaehlt dann die Zyklen, berechnet aber keinen Miner-Schaden
(`FlottenRainflow.Auswerten`, Absatz "Eine leere Kurve liefert die gezaehlten Zyklen ohne
Schaden"). Das ist Absicht: Mit Kurve bricht die Auswertung ab, sobald EINE Zyklustiefe
ausserhalb der gelieferten Stuetzstellen liegt - und die kleinste Tiefe eines Jahreslaufs
haengt an der Feinstruktur der Rechnung, nicht an der Konfiguration. Ein Referenzprojekt,
das bei einer harmlosen Aenderung am Flottenpfad nicht ABWEICHT, sondern ABSTUERZT, waere
ein schlechtes Regressionsnetz. Der Miner-Schaden selbst ist in `SpeicherEngine.Tests`
gepruefte Physik; `aggregate.csv` fuehrt den Skalar trotzdem (Wert 0), damit eine spaetere
Kurve eine ZAHL aendert und keinen SCHLUESSEL hinzufuegt - dasselbe Muster wie beim
strukturell leeren `Em.Kessel.CoKg` (Em-9.9).

WOHER DIE TABELLENLISTE KOMMT. Die Tabellen mit `ID_Projekt` werden zur Laufzeit aus dem
Schema gelesen; die Kindtabellen ohne eigenes `ID_Projekt` und die Umsetzung der
Fremdschluessel stehen unten - beides Zeile fuer Zeile nach `ProjektDuplizierenCtrl`
(`KINDER`, `FK_MAP`, `KATALOG_SPALTEN`), damit hier keine zweite Wahrheit entsteht.
Zum Schluss vergleicht das Skript die Zeilenzahlen beider Projekte je Tabelle - fehlt
etwas, faellt es hier auf und nicht erst im Rechenergebnis.

AUFRUF (aus der Wurzel des Arbeitsbaums):

    python3 Referenzlaeufe/Skripte/pruefprojekt_1046_speicherflotte.py Referenzlaeufe/Kenndaten_Test.sqlite

Steht Projekt 1046 schon, bricht das Skript ab. Fuer einen zweiten Lauf die Datenbank aus
der Sicherung zuruecklegen - dann vergibt er dieselben Ids und schreibt dieselbe Nutzlast
(der Zeitstempel der Standspalte ist fest, nicht "jetzt"). Ein `--neu` gibt es bewusst
nicht: Die Kindzeilen tragen kein `ID_Projekt`, ein halbherziges Loeschen hinterliesse
Waisen.
"""

import base64
import gzip
import json
import os
import sqlite3
import sys

VORLAGE = 1007
NEU = 1046
NAME = "Prüfprojekt Speicherflotte"

# Der reservierte Projektflottenstand - SpeicherFlottenProjektCtrl.ProjektflottenStand.
PROJEKTFLOTTENSTAND = "@Projektflotte"

# Fester Stand statt DateTimeOffset.UtcNow: Das Skript soll bei jedem Lauf dieselbe
# Datenbank erzeugen. Gelesen wird die Spalte vom Rechenweg nicht.
STAND = "2026-09-11T00:00:00.0000000+00:00"

# Tabellen mit ID_Projekt, die trotzdem NICHT mitkopiert werden.
#   Tab_Applikation        - anwendungsweit, kein Projektbezug (KATALOG_TABELLEN)
#   Tab_Projekt            - der Kopfsatz wird zuletzt einzeln geschrieben
#   Berichtskonfiguration  - gilt je Stammprojekt (ProjektDuplizierenCtrl.AUSNAHME_TABELLEN)
#   Tab_SpeicherAuslegung  - der Flottenstand wird unten neu geschrieben
#   Tab_Ergebnis*          - Ergebnisse; der Lauf schreibt sie selbst (Praefixregel in plan)
NICHT_KOPIEREN = {"Tab_Applikation", "Tab_Projekt", "Berichtskonfiguration",
                  "Tab_SpeicherAuslegung"}

# Kindtabellen ohne (verlaessliches) ID_Projekt: Filter auf der QUELLE, {p} = Vorlage-Id.
# Wortlaut aus ProjektDuplizierenCtrl.KINDER; Tab_StromspeicherVariante haengt dort an
# der Auto-Erkennung ueber die deklarierte Beziehung auf Tab_Energieanlagen.
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
    "Z_AnlageStrang":          "ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {p})",
    "Tab_StromspeicherVariante": "ID_Energieanlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = {p})",
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

# Mehrdeutige Spaltennamen je Tabelle (ProjektDuplizierenCtrl.FK_OVERRIDE).
FK_OVERRIDE = {
    "Z_ProjektWaermebedarf":    {"ID_Ganglinie": "Tab_Waermebedarf"},
    "Z_ProjektStromganglinie":  {"ID_Ganglinie": "Tab_Stromganglinie"},
    "Z_ProjektSolarganglinie":  {"ID_Ganglinie": "Tab_Solarganglinie"},
    "Tab_WaermebedarfDaten":    {"ID_Ganglinie": "Tab_Waermebedarf"},
    "Tab_StromganglinieDaten":  {"ID_Ganglinie": "Tab_Stromganglinie"},
    "Tab_SolarganglinieDaten":  {"ID_Ganglinie": "Tab_Solarganglinie"},
}


# =====================================================================================
#  Die Flotte (SpeicherEngine.FlottenStudieKonfiguration)
# =====================================================================================
#
# Die Enums werden als ZAHL geschrieben. SpeicherAuslegungKopie.JsonOptionen ist
# `new JsonSerializerOptions { IncludeFields = true }` - ohne JsonStringEnumConverter;
# System.Text.Json liest einen Enum dann aus der Zahl, nicht aus dem Namen.
ZIEL_PEAKSHAVING = 1        # FlottenBetriebsziel.PeakShaving
VERTEILUNG_KASKADE = 1      # FlottenVerteilung.Kaskade
PROGNOSE_VERIFIZIERT = 0    # PrognoseArt.VerifiziertBekannt
ERZEUGER_PV_VOR_BHKW = 0    # FlottenErzeugerPrioritaet.PvVorBhkw
ENDE_KEINE_VORGABE = 0      # FlottenEndbedingung.KeineVorgabe
QUELLE_EPOS = 0             # SpeicherAuslegungQuelle.Epos
KOSTEN_DIALOG = 0           # SpeicherKostenQuelle.Dialog

PEAK_ZIEL_KW = 16.0

EINHEITEN = [
    {
        "Id": "flotte-1046-a",
        "Name": "Flottenspeicher A (Bestand)",
        "AnlageId": None,
        "EigeneKosten": True,
        "KapazitaetKWh": 24.0,
        "LadeleistungKw": 10.0,
        "EntladeleistungKw": 12.0,
        "Ladewirkungsgrad": 0.96,
        "Entladewirkungsgrad": 0.94,
        "SocMin": 0.05,
        "SocMax": 0.95,
        "SocStart": 0.50,
        "PeakReserveKWh": 2.4,
        "HilfsverbrauchKw": 0.03,
        "GrenzverschleissEuroProKWhEntladung": 0.02,
        "InvestitionEuro": 2000.0,
        "InvestitionEuroProKWh": 350.0,
        "InvestitionEuroProKw": 200.0,
        "JaehrlicheFixeOpexEuro": 120.0,
        "JaehrlicheOpexEuroProKWhKapazitaet": 2.5,
        "JaehrlicheOpexEuroProKw": 5.0,
        "DurchsatzkostenEuroProKWhEntladung": 0.01,
        "ErsatzkostenEuro": 4000.0,
        "ErsatzintervallJahre": 10,
        "RestwertEuro": 500.0,
        "RainflowKurve": [],
    },
    {
        "Id": "flotte-1046-b",
        "Name": "Flottenspeicher B (Erweiterung)",
        "AnlageId": None,
        "EigeneKosten": True,
        "KapazitaetKWh": 16.0,
        "LadeleistungKw": 6.0,
        "EntladeleistungKw": 7.0,
        "Ladewirkungsgrad": 0.93,
        "Entladewirkungsgrad": 0.91,
        "SocMin": 0.10,
        "SocMax": 0.90,
        "SocStart": 0.40,
        "PeakReserveKWh": 1.2,
        "HilfsverbrauchKw": 0.02,
        "GrenzverschleissEuroProKWhEntladung": 0.03,
        "InvestitionEuro": 1500.0,
        "InvestitionEuroProKWh": 390.0,
        "InvestitionEuroProKw": 230.0,
        "JaehrlicheFixeOpexEuro": 90.0,
        "JaehrlicheOpexEuroProKWhKapazitaet": 3.0,
        "JaehrlicheOpexEuroProKw": 6.0,
        "DurchsatzkostenEuroProKWhEntladung": 0.012,
        "ErsatzkostenEuro": 3000.0,
        "ErsatzintervallJahre": 10,
        "RestwertEuro": 300.0,
        "RainflowKurve": [],
    },
]

OPTIONEN = {
    "Betriebsziel": ZIEL_PEAKSHAVING,
    "Verteilung": VERTEILUNG_KASKADE,
    # Keine HARTEN Anschlussgrenzen: Eine verbleibende Ueberschreitung machte die
    # Variante unzulaessig und damit den ganzen Projektlauf zum Fehlerfall.
    "NetzbezugGrenzeKw": None,
    "NetzeinspeisungGrenzeKw": None,
    "NetzladungErlaubt": True,
    "BatterieexportErlaubt": False,
    "WirtschaftlicherPeakZielwertKw": PEAK_ZIEL_KW,
    "ArbitrageLadepreisSchwelle": None,
    "ArbitrageEntladepreisSchwelle": None,
    "PrognoseArt": PROGNOSE_VERIFIZIERT,
    "ErzeugerPrioritaet": ERZEUGER_PV_VOR_BHKW,
    # Ohne Wirkung beim reaktiven Ziel; die Werte stehen trotzdem, damit ein spaeterer
    # Wechsel auf ein planendes Ziel nicht an einer 0 scheitert (Pruefe: > 0).
    "PlanungshorizontIntervalle": 192,
    "NeuplanungAlleIntervalle": 96,
    "Endbedingung": ENDE_KEINE_VORGABE,
    "EndenergieZielKWh": [],
    # Pflicht, solange keine Endenergiegleichheit gefordert ist (FlottenSimulator.Pruefe):
    # Anfangsenergie soll kein kostenloser Ertrag werden.
    "EnergieAusgleichEuroProKWh": 0.28,
    "PrognoseFallbackErlaubt": False,
}

TARIF = {
    "LeistungspreisEuroProKw": 120.0,
    "FixkostenEuro": 0.0,
    "BatterieVerkaufspreisEuroProKWh": None,
}

WIRTSCHAFTLICHKEIT = {
    "Einheiten": [],
    "Jahreskonten": [],
    "Kalkulationszins": 0.03,
    "RestwertEuro": 0.0,
    "ReferenzjahrExplizitWiederholen": True,
    "ProjektjahreBeiWiederholung": 20,
}

# Ohne Achsen wird genau diese eine Flotte gerechnet - keine Rastersuche.
AUSLEGUNG_SUCHRAUM = {
    "Achsen": [],
    "Betriebsziele": [],
    "MaximaleKandidaten": 10000,
}

# Die Kostensaetze der Quelle "Dialog". Beide Einheiten tragen `EigeneKosten = true` und
# behalten deshalb ihre eigenen Saetze (SpeicherFlottenStudieCtrl.Konfiguration); diese
# hier sind der Fallback und die Pflichtangabe von KostenAufloesen.
DIREKTE_KOSTEN = {
    "InvestEurProKw": 200.0,
    "InvestEurProKwh": 350.0,
    "BetriebEurProKwJahr": 5.0,
    "BetriebEurProKwhJahr": 2.5,
    "BetriebEurProKwhEntladen": 0.01,
    "InvestVorhanden": True,
    "BetriebVorhanden": True,
    "Herkunft": "Prüfprojekt 1046 (SP-O-8): Kostensätze des Mehrspeicherkonzepts",
    "AusgelassenePositionen": [],
}


def flottenstand():
    """Die Nutzlast des Standes `@Projektflotte` - Aufbau von SpeicherOptimierungEingaben."""
    auslegung = {
        "Investitionsquelle": KOSTEN_DIALOG,
        "Betriebsquelle": KOSTEN_DIALOG,
        "DirekteKosten": dict(DIREKTE_KOSTEN),
        # Wird von SpeicherAuslegungCtrl.AusQuellenVorbereiten bei jedem Lauf neu
        # aufgeloest; der Wert hier ist der Stand der Freigabe.
        "VerwendeteKosten": dict(DIREKTE_KOSTEN),
        "Lastquelle": QUELLE_EPOS,
        "PvQuelle": QUELLE_EPOS,
        "Preisquelle": QUELLE_EPOS,
        "Strompreisprofil": None,
        "LastDatei": None,
        "PvDatei": None,
        "PreisDatei": None,
        "EposModelljahrZuordnen": False,
        "Profilname": PROJEKTFLOTTENSTAND,
        "Revision": 1,
        "Flotte": {
            "Einheiten": [dict(e) for e in EINHEITEN],
            "Optionen": dict(OPTIONEN),
            "Tarif": dict(TARIF),
            "Wirtschaftlichkeit": dict(WIRTSCHAFTLICHKEIT),
            "Auslegung": dict(AUSLEGUNG_SUCHRAUM),
        },
        "FlotteImProjektAktiv": True,
        "FlottenProjektbetriebDeaktiviert": False,
        "FlottenGroessenOptimieren": False,
        "FlottenPrognosen": [],
        "FlottenPrognoseDatei": "",
        "FlottenPrognoseCsvOptionen": None,
        "FlottenProjektjahre": [],
        "FlottenJahresdatenDatei": "",
        "FlottenJahresCsvOptionen": None,
        "FlottenModelljahr": 2026,
        "FlottenBedienvorgabenVersion": 1,
    }
    return {
        "Auslegung": auslegung,
        "LeistungspreisEurProKwA": TARIF["LeistungspreisEuroProKw"],
    }


def serialisieren(wert):
    """`gz1:` + Base64(gzip(JSON)) - das Format von SpeicherAuslegungCtrl.Serialisieren.

    `mtime = 0` im gzip-Kopf, damit dieselbe Eingabe dieselben Bytes ergibt."""
    roh = json.dumps(wert, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    gepackt = gzip.compress(roh, compresslevel=1, mtime=0)
    return "gz1:" + base64.b64encode(gepackt).decode("ascii")


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
        sql = ('INSERT INTO "%s" (%s) VALUES (%s)'
               % (t, ",".join('"%s"' % s for s in sp), ",".join("?" * len(sp))))

        for row in zeilen[t]:
            neu = dict(row)
            neu[pk] = karte[(t, row[pk])]
            if "ID_Projekt" in neu and neu["ID_Projekt"]:
                neu["ID_Projekt"] = NEU

            for feld in sp:
                if feld == pk or feld == "ID_Projekt":
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
        "Dreizehntes Prüfprojekt der Referenzbasis (Anwenderentscheid SP-O-8, 11.09.2026): "
        "Kopie von Projekt %d mit aktivierter Speicherflotte aus zwei Einheiten "
        "(PeakShaving gegen %.1f kW, Verteilung Kaskade). Es hält den Flottenpfad des "
        "gewöhnlichen Projektlaufs im Regressionsnetz — Verteilung, SoC-Grenzen, "
        "Richtungswirkungsgrade, Reserve, Hilfsverbrauch und die getrennte Netzbilanz."
        % (VORLAGE, PEAK_ZIEL_KW))
    row["ID_Klimaregion"] = karte[("Tab_Klimaregion", row["ID_Klimaregion"])]
    c.execute("INSERT INTO Tab_Projekt (%s) VALUES (%s)"
              % (",".join('"%s"' % s for s in sp), ",".join("?" * len(sp))),
              [row[s] for s in sp])
    print('  Tab_Projekt %d "%s", Klimaregion %d' % (NEU, NAME, row["ID_Klimaregion"]))


def projektflotte(c):
    """Den reservierten Stand `@Projektflotte` schreiben - Anlagenbezug NULL."""
    daten = serialisieren(flottenstand())
    neu = naechste_id(c, "Tab_SpeicherAuslegung")
    c.execute("INSERT INTO Tab_SpeicherAuslegung "
              "(ID, ID_Projekt, ID_Energieanlage, Bezeichner, Daten, Stand) "
              "VALUES (?,?,NULL,?,?,?)",
              (neu, NEU, PROJEKTFLOTTENSTAND, daten, STAND))
    print("  Tab_SpeicherAuslegung %d = %s, %d Zeichen Nutzlast"
          % (neu, PROJEKTFLOTTENSTAND, len(daten)))
    for e in EINHEITEN:
        print("    %-30s %6.1f kWh, %.1f/%.1f kW, eta %.2f/%.2f, SoC %.2f-%.2f, Reserve %.1f kWh"
              % (e["Name"], e["KapazitaetKWh"], e["LadeleistungKw"], e["EntladeleistungKw"],
                 e["Ladewirkungsgrad"], e["Entladewirkungsgrad"],
                 e["SocMin"], e["SocMax"], e["PeakReserveKWh"]))
    print("    Ziel PeakShaving gegen %.1f kW, Verteilung Kaskade, Netzladung frei, "
          "Batterieexport gesperrt" % PEAK_ZIEL_KW)


def gegenprobe(c):
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
    stand = c.execute("SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ? "
                      "AND ID_Energieanlage IS NULL AND Bezeichner = ?",
                      (NEU, PROJEKTFLOTTENSTAND)).fetchone()[0]
    anlagen = c.execute("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?",
                        (NEU,)).fetchone()[0]
    print("  Energieanlagen %d, Projektflottenstand %d" % (anlagen, stand))
    if stand != 1:
        fehler += 1
        print("  ABWEICHUNG Projektflottenstand: erwartet genau 1, gefunden %d" % stand)
    if fehler:
        raise SystemExit("%d Abweichung(en) - die Kopie ist unvollstaendig." % fehler)
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

    print("Projektflottenstand:")
    projektflotte(c)

    c.commit()
    gegenprobe(c)
    c.execute("VACUUM")
    c.close()
    print("Fertig.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
