#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Legt das Referenzprojekt 1058 "Referenzprojekt AK3" der Testdatenbank an - die Kopie des Referenzprojekts
1056 (Anlagenfahrplan), die auf der Stufe AK3 rechnet: der geschlossene Kreis zwischen Gebaeude und
Erzeugern (Entwurf AK3 Abschnitt 5, RP-AK3; Entscheide E100, E102 mit Q-AK3-2 und Q-AK3-7; Einfrierregel
"gesaete Auslegungsdaten der Uebergabe").

AUSGANG. 1056 haelt den Anlagenfahrplan auf dem Profilweg (AK1/AK2): Die Waermepumpe ist von 0 bis 6 Uhr
gesperrt, Kessel und BHKW sind in denselben Stunden per Zeitprogramm aus, und ohne Vorrat kuehlt das
Gebaeude aus (Komfortstunden). Keines der Referenzprojekte zeigt, dass ein Speicher die Sperre ueberbrueckt
(1056 hat keinen Puffer, 1054 keine Sperre), und keines rechnet den Kreis AK3. Auf 1056 selbst waere das
ein Basiswechsel des AK2-Vergleichsfalls - deshalb eine KOPIE, und 1056 bleibt Zelle fuer Zelle, wie es war.

WAS DIESES SKRIPT TUT (eine Transaktion, kein VACUUM).
  1. Die Kopie 1056 -> 1058 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl.Duplizieren,
     "Projekt Speichern unter"), Wortlaut und Regeln wie im Skript von 1056. Die zwei Nachzuege des
     Programms (Kostenpositionen, Speicherauslegung) haben bei 1056 nichts zu tun - geprueft, sonst Abbruch.
     Gegenprobe: jede kopierte Tabelle traegt fuer 1058 so viele Zeilen wie fuer 1056.
  2. Zellen der Kopie:
       Tab_Einstellungen      Anlagenkopplung AK1 -> AK3
       Tab_Gebaeude           Heizkurve_Raumeinfluss NULL -> 1,0 K/K (H2, Q-AK3-2)
       Tab_Projekt            Beschreibung (neutraler Zweck), Kosten_Geaendert leer
  3. Ein Heizungspuffer, wie PufferSpCtrl.ProjektPufferAnlegen ihn schreibt:
       Tab_Pufferspeicher     "Heizungspuffer 10000 l", Speichertyp Pufferspeicher, Verwendung Heizung,
                              10000 l, Vorlauf 50 / Ruecklauf 35 Grad C, Bereitschaftsverlust 5 kWh/24 h,
                              Schwellen 10/95 %, Nachrang leer, Entladeprio 0, Reserve 10 %, Waermeflag
                              Heizung, eine Schicht, Lade- und Entladeleistung je 60 kW
       Tab_Energieanlagen     Typ 12 mit ID_PUFFER (die Anlagenzeile des Puffers)
  4. Die Senken der drei Waermeerzeuger (Z_AnlageSenke, Rang 1; die Altspalten WS_Ziel, WS_ID_Puffer,
     WS_Ladeprio gleichgezogen wie der Speicherweg der Erzeuger): alle laden den Heizungspuffer
     ("PufferHeizung", Bedarfsart wie bisher), Lade-Prioritaet Waermepumpe 1 vor BHKW 2 vor Kessel 3.

DIE WERTE UND WARUM.
  Puffer 10000 l, 50/35 Grad C: Das Gebaeude von 1047/1056 braucht in den sechs Sperrstunden einer Nacht
      hoechstens rund 196 kWh, im Mittel der 60 kaeltesten Naechte rund 112 kWh (Basis R41, Projekt 1047,
      waermebedarf_gebaeude). 10 m3 x 15 K x 1,163 kWh/(m3 K) = 174 kWh decken die mittlere Winternacht und
      fast die kaelteste. Vorlauf 50 Grad C = Vorlauf_Max der Waermepumpe (der Speicher wird nicht heisser
      geladen, als die Waermepumpe liefert); Ruecklauf 35 Grad C, ein runder Wert unter dem Ruecklauf der
      Heizkurve.
  Lade- und Entladeleistung je 60 kW: rund die Nennleistung der Waermepumpe (59 kW) und ueber der
      Spitzenlast des Gebaeudes (42 kW) - der Puffer begrenzt weder Laden noch Entladen sichtbar.
  Lade-Prioritaet Waermepumpe zuerst: Der Puffer soll die Sperre der Waermepumpe ueberbruecken; Kessel und
      BHKW laden nach.
  Heizkurve_Raumeinfluss 1,0 K/K: der Vorgabewert des Raumeinflusses (H2, Q-AK3-2) - der Vorlauf hebt sich
      um 1 K je Kelvin Unterschreitung der kaeltesten Zone.

WIEDERHOLBAR. Steht Projekt 1058 schon, bricht das Skript mit Meldung ab und schreibt nichts (Rueckgabe
0, wenn es "Referenzprojekt AK3" heisst, sonst 2). Weicht die Vorlage 1056 ab, faellt die Kopie auf eine
andere Id als 1058 oder misslingt eine Pruefung, rollt die Transaktion zurueck (Rueckgabe 2).

PRUEFUNGEN. Vorlage (Name, Gebaeude, Kopplungsstufe AK1, Heizkreis mit Radiator und Heizkurve, Kaskade,
je eine Waermepumpe mit Sperre 0-6 Uhr, ein Kessel, ein BHKW, kein Puffer, Senken je Rang 1 am Heizkreis),
Gegenprobe der Zeilenzahlen, die gesetzten Zellen nach dem Schreiben, foreign_key_check und integrity_check.
Danach die PRUEFAUSGABE: alle gesaeten Zellen der Kopie.

FOLGE. Die Zellen sind gesaete Auslegungsdaten der Uebergabe eines gekoppelten Referenzprojekts
(Einfrierregel, `Referenzlaeufe/LIESMICH.md`); gehalten von `EPOS.Kern.Tests/Ak3ReferenzprojektWacheTests`.
Die Kopie ist zugleich ein Referenzprojekt mit Gebaeude-, Kaelte- und Fahrplandaten - die Regeln dafuer
gelten fuer 1058 wie fuer 1056.

Aufruf (Windows: `py`, sonst `python3`; vorher sichern):
    py Referenzlaeufe/Skripte/referenzprojekt_1058_ak3.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

VORLAGE = 1056
VORLAGE_NAME = "Referenz Kopplung mit Fahrplan"
VORLAGE_GEBAEUDE = 10662
NEU = 1058
NAME = "Referenzprojekt AK3"
BESCHREIBUNG = (
    "Referenzprojekt Anlagenkopplung AK3: Kopie von Projekt 1056 (Anlagenfahrplan) auf der Stufe AK3, dem "
    "geschlossenen Kreis zwischen Gebäude und Erzeugern, mit einem Heizungspuffer an der Wärmepumpe, der die "
    "Sperre von 0 bis 6 Uhr überbrückt, und dem Raumeinfluss der Heizkurve 1 K/K. 1056 bleibt der Fall ohne "
    "Kreis und ohne Puffer.")

TYP_WP = 1
TYP_KESSEL = 10
TYP_BHKW = 11
TYP_PUFFER = 12
STUFE_VORLAGE = "AK1"
STUFE = "AK3"
RAUMEINFLUSS = 1.0
KASKADE = {"Tool_1": "BHKW", "Tool_2": "Wärmepumpe", "Tool_3": "Heizkessel", "Tool_4": ""}

PUFFER_NAME = "Heizungspuffer 10000 l"
PUFFER_TYP = "Pufferspeicher"
PUFFER_VERWENDUNG = "Heizung"
PUFFER_VOLUMEN = 10000
PUFFER_VORLAUF = 50
PUFFER_RUECKLAUF = 35
PUFFER_VERLUST = 5.0
SCHWELLE_EIN = 10.0
SCHWELLE_AUS = 95.0
SCHWELLE_RESERVE = 10.0
LADELEISTUNG = 60.0
ENTLADELEISTUNG = 60.0
ZIEL = "PufferHeizung"
LADEPRIO = {TYP_WP: 1, TYP_BHKW: 2, TYP_KESSEL: 3}


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



def anlage(c, projekt, typ):
    ids = [r[0] for r in c.execute("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                                   (projekt, typ))]
    if len(ids) != 1:
        raise LookupError("Projekt %d fuehrt %d Anlagen vom Typ %d statt einer" % (projekt, len(ids), typ))
    return ids[0]



def pruefe_vorlage(c):
    """1056 muss stehen, wie AK2-4 es hinterlassen hat - Sperre, Zeitprogramm, kein Puffer, Senken am Heizkreis."""
    name = eine_zeile(c, "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (VORLAGE,))[0]
    if name != VORLAGE_NAME:
        return "Vorlage %d heisst %r statt %r" % (VORLAGE, name, VORLAGE_NAME)
    gebaeude = [r[0] for r in c.execute("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", (VORLAGE,))]
    if gebaeude != [VORLAGE_GEBAEUDE]:
        return "Vorlage %d fuehrt die Gebaeude %r statt [%d]" % (VORLAGE, gebaeude, VORLAGE_GEBAEUDE)
    g = spaltenwerte(c, "Tab_Gebaeude", "ID", VORLAGE_GEBAEUDE,
                     ["Heizkreis_Aktiv", "Uebergabe_Art", "Heizkurve_Aktiv", "Heizkurve_Raumeinfluss"])
    if g != {"Heizkreis_Aktiv": 1, "Uebergabe_Art": "RADIATOR", "Heizkurve_Aktiv": 1, "Heizkurve_Raumeinfluss": None}:
        return "Vorlagengebaeude %d: %r statt Heizkreis mit Radiator und Heizkurve ohne Raumeinfluss" % (VORLAGE_GEBAEUDE, g)
    e = spaltenwerte(c, "Tab_Einstellungen", "ID_Projekt", VORLAGE, list(KASKADE) + ["Anlagenkopplung"])
    if e["Anlagenkopplung"] != STUFE_VORLAGE:
        return "Vorlage %d: Anlagenkopplung = %r statt %r" % (VORLAGE, e["Anlagenkopplung"], STUFE_VORLAGE)
    for spalte, soll in KASKADE.items():
        if (e[spalte] or "") != soll:
            return "Vorlage %d: %s = %r statt %r" % (VORLAGE, spalte, e[spalte], soll)
    try:
        ids = {typ: anlage(c, VORLAGE, typ) for typ in (TYP_WP, TYP_KESSEL, TYP_BHKW)}
    except LookupError as ex:
        return str(ex)
    wp = spaltenwerte(c, "Tab_Energieanlagen", "ID", ids[TYP_WP], ["Sperrung", "Sperrzeit_von", "Sperrzeit_bis"])
    if wp != {"Sperrung": 1, "Sperrzeit_von": 0, "Sperrzeit_bis": 6}:
        return "Waermepumpe %d der Vorlage: %r statt Sperre 0 bis 6 Uhr" % (ids[TYP_WP], wp)
    for typ, i in ids.items():
        senken = c.execute("SELECT Rang, Ziel, ID_Puffer FROM Z_AnlageSenke WHERE ID_Anlage = ?", (i,)).fetchall()
        if senken != [(1, "Heizkreis", None)]:
            return "Anlage %d der Vorlage: Senken %r statt Rang 1 am Heizkreis" % (i, senken)
    if anzahl(c, "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt einen Pufferspeicher" % VORLAGE
    if anzahl(c, "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", (VORLAGE, TYP_PUFFER)):
        return "Vorlage %d fuehrt eine Pufferanlage" % VORLAGE
    # Die zwei Nachzuege des Programms nach dem Kopieren - bei 1056 ohne Gegenstand.
    if anzahl(c, "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt Kostenpositionen - der Ankernachzug des Programms fehlt hier" % VORLAGE
    if anzahl(c, "SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt eine Speicherauslegung - deren Bezugsnachzug fehlt hier" % VORLAGE
    return None


def stufe_setzen(c):
    """Kopplungsstufe AK3 und Raumeinfluss der Heizkurve am Gebaeude der Kopie."""
    if c.execute("UPDATE Tab_Einstellungen SET Anlagenkopplung = ? WHERE ID_Projekt = ?", (STUFE, NEU)).rowcount != 1:
        raise RuntimeError("Tab_Einstellungen %d: Stufe nicht gesetzt" % NEU)
    g = [r[0] for r in c.execute("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", (NEU,))]
    if len(g) != 1:
        raise RuntimeError("Kopie %d fuehrt %d Gebaeude" % (NEU, len(g)))
    c.execute("UPDATE Tab_Gebaeude SET Heizkurve_Raumeinfluss = ? WHERE ID = ?", (RAUMEINFLUSS, g[0]))
    print("gesetzt: Anlagenkopplung %s, Gebaeude %d Heizkurve_Raumeinfluss %.1f K/K" % (STUFE, g[0], RAUMEINFLUSS))
    return g[0]


def puffer_anlegen(c):
    """PufferSpCtrl.ProjektPufferAnlegen mit Verwendung Heizung, eine Schicht, Lade-/Entladeleistung gesetzt."""
    pid = (anzahl(c, "SELECT MAX(ID) FROM Tab_Pufferspeicher") or 0) + 1
    c.execute("INSERT INTO Tab_Pufferspeicher (ID, ID_Projekt, Bezeichner, Hersteller, Speichertyp, Gesamtvolumen, "
              "Bereitschaftsverluste, Investitionskosten, Verwendung, Vorlauf, Ruecklauf, Schwelle_Ein, Schwelle_Aus, "
              "Schwelle_Aus_Nachrang, Entladeprio, Schwelle_Reserve) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
              (pid, NEU, PUFFER_NAME, "", PUFFER_TYP, PUFFER_VOLUMEN, PUFFER_VERLUST, 0.0, PUFFER_VERWENDUNG,
               PUFFER_VORLAUF, PUFFER_RUECKLAUF, SCHWELLE_EIN, SCHWELLE_AUS, None, 0, SCHWELLE_RESERVE))
    # Klassen-Set: der Heizungspuffer traegt das Waermeflag Heizung (KlassenSetBestimmen).
    c.execute("UPDATE Tab_Pufferspeicher SET Nutzung_Heizung = 1, Nutzung_Brauchwasser = 0, Nutzung_Prozess = 0 "
              "WHERE ID = ?", (pid,))
    c.execute("UPDATE Tab_Pufferspeicher SET Bereitschaft_Weg = NULL, Aufstellraum_Temperatur_C = NULL, "
              "Schicht_Anteile = NULL, Frischwassermodul = NULL, FWM_Graedigkeit_K = NULL WHERE ID = ?", (pid,))
    c.execute("UPDATE Tab_Pufferspeicher SET Schichten_Anzahl = 1, Hoehe = NULL, Lambda_Eff = NULL, T_Nutz_BW = NULL, "
              "Entnahme_Heizung = NULL, Entnahme_BW = NULL, Entnahme_Prozess = NULL, Ladeleistung_Max = ?, "
              "Entladeleistung_Max = ? WHERE ID = ?", (LADELEISTUNG, ENTLADELEISTUNG, pid))
    # Die Anlagenzeile (ProjektPuffer.SQL_ANLAGENZEILE_INSERT, AnlagenzeileParameter).
    c.execute("INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_Type, Betriebsart, Sperrung, Sperrzeit_von, "
              "Sperrzeit_bis, Vorlauf, Rücklauf, Bivalenter_Betrieb, Abschaltpunkt, Nutzungszeit, Grenzleistung, "
              "Kollektormodulanzahl, PV_Leistung, Neigung, Azimut, ID_WP, ID_Solar, ID_PV, ID_SP, ID_Kessel, ID_BHKW, "
              "ID_PUFFER, ID_Carrier, Heizstab, Volumen, rendeMix, Solaranteil, WS_Ladeprio, WS_Ladegrenze, "
              "WS_Ladeprio_PV, WS_Ladeprio2, WS_Ladegrenze2) "
              "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
              (NEU, PUFFER_NAME, TYP_PUFFER, "", 0, 0, 0, 0, 0, 0, 0.0, 0, 0.0, 0, 0.0, 0, 0,
               None, None, None, None, None, None, pid, None, 0, 0.0, 0, 0, 0, 0.0, 0, 0, 0.0))
    anlage_id = anzahl(c, "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_PUFFER = ?", (NEU, pid))
    print("Heizungspuffer: Puffer %d (%d l, %d/%d Grad C, %.0f/%.0f kW), Anlage %d"
          % (pid, PUFFER_VOLUMEN, PUFFER_VORLAUF, PUFFER_RUECKLAUF, LADELEISTUNG, ENTLADELEISTUNG, anlage_id))
    return pid, anlage_id


def senken_setzen(c, pid):
    """Rang 1 jedes Waermeerzeugers: der Heizungspuffer, Lade-Prioritaet WP vor BHKW vor Kessel."""
    for typ, prio in LADEPRIO.items():
        i = anlage(c, NEU, typ)
        n = c.execute("UPDATE Z_AnlageSenke SET Ziel = ?, ID_Puffer = ?, Ladeprio = ? WHERE ID_Anlage = ? AND Rang = 1",
                      (ZIEL, pid, prio, i)).rowcount
        if n != 1:
            raise RuntimeError("Anlage %d: %d Senken auf Rang 1" % (i, n))
        c.execute("UPDATE Tab_Energieanlagen SET WS_Ziel = ?, WS_ID_Puffer = ?, WS_Ladeprio = ? WHERE ID = ?",
                  (ZIEL, pid, prio, i))
        print("gesetzt: Anlage %d (Typ %d) Senke Rang 1 %s Puffer %d, Ladeprio %d" % (i, typ, ZIEL, pid, prio))


def pruefausgabe(c, gebaeude, pid, puffer_anlage):
    """Die gesaeten Zellen der Kopie, gelesen nach dem Schreiben."""
    print("Pruefausgabe %d:" % NEU)
    print("  Tab_Einstellungen: %r" % (spaltenwerte(c, "Tab_Einstellungen", "ID_Projekt", NEU,
                                                    ["Anlagenkopplung"] + list(KASKADE)),))
    print("  Tab_Gebaeude %d: %r" % (gebaeude, spaltenwerte(c, "Tab_Gebaeude", "ID", gebaeude,
                                                           ["Heizkreis_Aktiv", "Uebergabe_Art", "Heizkurve_Aktiv",
                                                            "Heizkurve_Raumeinfluss"])))
    print("  Tab_Pufferspeicher %d: %r" % (pid, spaltenwerte(c, "Tab_Pufferspeicher", "ID", pid,
                                                             ["Bezeichner", "Verwendung", "Gesamtvolumen", "Vorlauf",
                                                              "Ruecklauf", "Bereitschaftsverluste", "Schwelle_Ein",
                                                              "Schwelle_Aus", "Schwelle_Reserve", "Nutzung_Heizung",
                                                              "Ladeleistung_Max", "Entladeleistung_Max"])))
    print("  Pufferanlage %d: ID_PUFFER %r" % (puffer_anlage, spaltenwerte(c, "Tab_Energieanlagen", "ID", puffer_anlage,
                                                                         ["ID_PUFFER"])["ID_PUFFER"]))
    for r in c.execute("SELECT a.ID, a.ID_Type, s.Rang, s.Ziel, s.Bedarfsart, s.ID_Puffer, s.Ladeprio FROM Z_AnlageSenke s "
                       "JOIN Tab_Energieanlagen a ON a.ID = s.ID_Anlage WHERE a.ID_Projekt = ? ORDER BY s.Ladeprio", (NEU,)):
        print("  Senke: Anlage %d (Typ %d) Rang %d %s/%s Puffer %r Ladeprio %d" % r)

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
        print("Aufruf: referenzprojekt_1058_ak3.py <Kenndaten_Test.sqlite>")
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
        print("Vorlage %d \"%s\": Gebaeude %d, AK1, Radiator mit Heizkurve, Sperre 0-6 Uhr, ohne Puffer."
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

            if con.execute("UPDATE Tab_Projekt SET Beschreibung = ? WHERE ID = ?", (BESCHREIBUNG, NEU)).rowcount != 1:
                raise RuntimeError("Tab_Projekt %d: Beschreibung nicht gesetzt" % NEU)
            gebaeude = stufe_setzen(con)
            pid, puffer_anlage = puffer_anlegen(con)
            senken_setzen(con, pid)

            # Die Schreibwege stempeln ueber die Trigger des Kostenstempels die Uhrzeit des Laufs in
            # Kosten_Geaendert - leer wie bei jedem anderen Projekt der Testdatenbank (Muster 1051/1052/1055/1056).
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

        pruefausgabe(con, gebaeude, pid, puffer_anlage)
        pruefung = con.execute("PRAGMA integrity_check").fetchone()[0]
        fremd = con.execute("PRAGMA foreign_key_check").fetchall()
        print("integrity_check: %s; foreign_key_check: %d Zeilen" % (pruefung, len(fremd)))
        return 0 if pruefung == "ok" and not fremd else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
