#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Legt das Referenzprojekt 1064 "Referenzprojekt Freie Kühlung" der Testdatenbank an - die Kopie des
Kuehlreferenzprojekts 1017 (Gebaeude nach VDI 6007 mit Kuehlung, Sole-Wasser-Waermepumpe im Kuehlbetrieb an der
Erdsonde 5 x 120 m, Kuehlvorlauf 18 Grad C) mit eingeschalteter freier Kuehlung ueber die Waermequelle (Stufe KU3-6,
Entscheid E75, Schemaschritt 187 FreieKuehlungSoleSchema).

AUSGANG. Die freie Kuehlung ueber die Waermequelle rechnet kein Referenzprojekt: An jeder Anlagenzeile der
Testdatenbank steht Kuehl_Frei 0. 1017 bleibt Zelle fuer Zelle; das Paar 1017/1064 zeigt die Wirkung allein des
Schalters, seiner Graedigkeit und seiner Leistungsgrenze.

WARUM 1017 ALS VORLAGE. Die freie Kuehlung greift nur an einer Sole-Wasser- oder Wasser-Wasser-Waermepumpe im
Kuehlbetrieb mit gepflegter Quelle (Erdreich, Konstant, Profil, CSV), deren Quellentemperatur plus Graedigkeit den
Kuehlvorlauf erreicht. 1017 bringt alles mit: die einzige Waermepumpe eines einfachen Referenzprojekts, die
kuehlt, an der Erdsonde (WQ_Typ Erdreich, Sonde), Kuehlvorlauf 18 Grad C. 1055/1063 (Kaeltemaschine) kuehlen nicht
mit der Waermepumpe, 1057 (Erdsonde) kuehlt nicht, 1047 und seine Kopien rechnen ueber die Anlagenkopplung - dort
wirkten Kopplung und freie Kuehlung zugleich.

WAS DIESES SKRIPT TUT (eine Transaktion, kein VACUUM).
  1. Die Kopie 1017 -> 1064 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl.Duplizieren), Wortlaut der
     Tabellen und Regeln wie im Programm (Stand Schemaschritt 210); die Kopie faellt auf die naechste freie Id,
     vorausgesetzt ist 1063 als hoechste. Die zwei Nachzuege (Kostenpositionen, Speicherauslegung) haben bei 1017
     nichts zu tun - geprueft, sonst Abbruch. Gegenprobe: jede kopierte Tabelle traegt fuer 1064 so viele Zeilen
     wie fuer 1017.
  2. Zellen der Kopie:
       Tab_Projekt        Beschreibung (neutraler Zweck), Kosten_Geaendert leer
       Tab_Energieanlagen (Anlagenzeile der Waermepumpe)
                          Kuehl_Frei 1, Kuehl_Frei_Graedigkeit_K 4,0 K, Kuehl_Frei_Leistung_kW 4,0 kW
     Gebaeude, Kuehlkennlinie, Erdsonde, Kaskade und Traeger bleiben wie in 1017.

DIE WERTE UND WARUM.
  Graedigkeit 4,0 K statt der Vorgabe 3,0 K (KaelteFestwerte.FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K): Die gepflegte Zelle
      wird gelesen, nicht die Vorgabe. Mit 18 Grad C Kuehlvorlauf kuehlt die Quelle frei, solange die Sole hoechstens
      14 Grad C hat.
  Leistungsgrenze 4,0 kW statt der Kaelteleistung der Kennlinie (13 bis 15 kW bei 18 Grad C): Die Grenze greift in
      einem Teil der freien Stunden, dort deckt der Verdichter den Rest - beide Wege der Kaeltestunde stehen im
      Netz. Gemessen an einer Arbeitskopie (Lauf von 1064, Sole 12,9 bis 19,5 Grad C, Kaeltespitze 15,0 kW; die
      Zeile 4 K / 4 kW ist die Basis R51):
        Graedigkeit / Grenze      freie Stunden   freie Kaelte   Stunden an der Grenze
        leer (3 K) / leer         251             0,84 MWh       -
        4 K / 8 kW                127             0,32 MWh       0
        4 K / 4 kW                125             0,28 MWh       35
      Mit 8 kW griffe die Grenze nie; 4 kW haelt sie in rund einem Viertel der freien Stunden.

WIEDERHOLBAR. Steht Projekt 1064 schon, bricht das Skript mit Meldung ab und schreibt nichts (Rueckgabe 0,
wenn es "Referenzprojekt Freie Kühlung" heisst, sonst 2). Weicht die Vorlage ab, faellt die Kopie auf eine andere
Id als 1064 oder misslingt eine Pruefung, rollt die Transaktion zurueck (Rueckgabe 2).

PRUEFUNGEN. Vorlage 1017 (Name, ein Gebaeude mit Kuehlung, keine Anlagenkopplung, eine Sole-Wasser-Waermepumpe im
Kuehlbetrieb an einer Erdreichquelle mit Kuehl_Frei 0), Gegenprobe der Zeilenzahlen, jede gesaete Zelle,
foreign_key_check und integrity_check.

FOLGE. Die Zellen sind gesaete Daten der freien Kuehlung eines Referenzprojekts (Einfrierregel,
`Referenzlaeufe/LIESMICH.md`); gehalten von `EPOS.Kern.Tests/FreieKuehlungReferenzprojektWacheTests`. Die Regeln
fuer Gebaeude-, Kaelte- und Erdreichdaten gelten fuer 1064 wie fuer 1017. Die Basis R51 friert 1064 ein.

Aufruf (Windows: `py`, sonst `python3`; vorher sichern):
    py Referenzlaeufe/Skripte/referenzprojekt_1064_freie_kuehlung.py Referenzlaeufe/Kenndaten_Test.sqlite
Fuer Messungen an einer Arbeitskopie (nie fuer die Testdatenbank): --graedigkeit <K|leer>, --leistung <kW|leer>.
"""

import sqlite3
import sys

VORLAGE = 1017
VORLAGE_NAME = "WP_PV-Speicher"
VORLAGE_GEBAEUDE = 10599
VORLAGE_WP = 1017033
VORLAGE_ANLAGE_WP = 10211
VORHER_HOECHSTE = 1063
NEU = 1064
NAME = "Referenzprojekt Freie Kühlung"
GRAEDIGKEIT_K = 4.0
LEISTUNG_KW = 4.0
BESCHREIBUNG = (
    "Referenzprojekt Freie Kühlung: Kopie von Projekt 1017, die Sole-Wasser-Wärmepumpe an der Erdsonde kühlt "
    "frei über ihre Wärmequelle (Grädigkeit %s, Leistungsgrenze %s), solange die Sole den Kühlvorlauf 18 °C "
    "erreicht; den Rest deckt der Verdichter.")

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
    "Z_ProjektPvGanglinie": {"ID_Ganglinie": "Tab_PvGanglinie"},
    "Tab_PvGanglinieDaten": {"ID_Ganglinie": "Tab_PvGanglinie"},
    "Tab_Bauteil": {"ID_Zone": "Tab_Zone"},
    "Tab_Importzuordnung": {"ID_Zone": "Tab_Zone"},
    "Tab_Raumgrundriss": {"ID_Zone": "Tab_Zone"},
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
    "Tab_PvGanglinieDaten": "ID_Ganglinie IN (SELECT ID FROM Tab_PvGanglinie WHERE ID_Projekt = {0})",
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
    "Tab_Raumgrundriss": "ID_Importquelle IN (SELECT ID FROM Tab_Importquelle WHERE ID_Gebaeude IN " + _G + ")",
    "Tab_Zonenluftstrom": "ID_ZoneA IN (SELECT ID FROM Tab_Zone WHERE ID_Gebaeude IN " + _G + ")",
    "Tab_ErgebnisZone": "ID_ErgebnisGebaeude IN (SELECT ID FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis IN "
                        "(SELECT ID FROM Tab_Ergebnis WHERE ID_Projekt = {0}))",
    "Tab_Konditionierungskalender": "ID_Gebaeude IN " + _G,
    "Tab_Konditionierungsvorgabe": "ID_Gebaeude IN " + _G,
    "Tab_Konditionierungswoche": "ID_Gebaeude IN " + _G,
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
    """1017 wie die Kuehl- und Erdreichskripte es hinterlassen haben."""
    if eine_zeile(c, "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (VORLAGE,))[0] != VORLAGE_NAME:
        return "Vorlage %d heisst nicht %r" % (VORLAGE, VORLAGE_NAME)
    hoechste = anzahl(c, "SELECT MAX(ID) FROM Tab_Projekt")
    if hoechste != VORHER_HOECHSTE:
        return "Die hoechste Projekt-Id ist %r statt %d" % (hoechste, VORHER_HOECHSTE)
    gebaeude = [r[0] for r in c.execute("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", (VORLAGE,))]
    if gebaeude != [VORLAGE_GEBAEUDE]:
        return "Vorlage %d fuehrt die Gebaeude %r statt [%d]" % (VORLAGE, gebaeude, VORLAGE_GEBAEUDE)
    if spaltenwerte(c, "Tab_Gebaeude", "ID", VORLAGE_GEBAEUDE, ["Kuehlung_Aktiv"])["Kuehlung_Aktiv"] != 1:
        return "Vorlagengebaeude %d kuehlt nicht" % VORLAGE_GEBAEUDE
    e = spaltenwerte(c, "Tab_Einstellungen", "ID_Projekt", VORLAGE, ["Anlagenkopplung", "Kuehlbetrieb"])
    if e["Kuehlbetrieb"] != 1 or e["Anlagenkopplung"] not in (None, ""):
        return "Vorlage %d: %r statt Kuehlbetrieb ohne Anlagenkopplung" % (VORLAGE, e)
    wp = [r for r in c.execute("SELECT ID, Typ, Kuehlbetrieb, Kuehl_Vorlauf FROM Tab_WP WHERE ID_Projekt = ?", (VORLAGE,))]
    if wp != [(VORLAGE_WP, "Sole-Wasser", 1, 18.0)]:
        return "Vorlage %d: Waermepumpen %r statt einer Sole-Wasser im Kuehlbetrieb mit 18 Grad C" % (VORLAGE, wp)
    a = spaltenwerte(c, "Tab_Energieanlagen", "ID", VORLAGE_ANLAGE_WP,
                     ["ID_Projekt", "ID_WP", "WQ_Typ", "WQ_Quellsystem", "Kuehl_Frei",
                      "Kuehl_Frei_Graedigkeit_K", "Kuehl_Frei_Leistung_kW"])
    if a != {"ID_Projekt": VORLAGE, "ID_WP": VORLAGE_WP, "WQ_Typ": "Erdreich", "WQ_Quellsystem": "Sonde",
             "Kuehl_Frei": 0, "Kuehl_Frei_Graedigkeit_K": None, "Kuehl_Frei_Leistung_kW": None}:
        return "Anlagenzeile %d der Waermepumpe: %r" % (VORLAGE_ANLAGE_WP, a)
    if anzahl(c, "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE Kuehl_Frei = 1"):
        return "Eine Anlagenzeile der Testdatenbank kuehlt schon frei"
    if anzahl(c, "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt Kostenpositionen - der Ankernachzug des Programms fehlt hier" % VORLAGE
    if anzahl(c, "SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt eine Speicherauslegung - deren Bezugsnachzug fehlt hier" % VORLAGE
    return None


def gegenprobe(c, specs):
    """Zeilenzahlen je kopierter Tabelle: Vorlage gegen Kopie."""
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


def wert_aus_argument(name, vorgabe):
    """--graedigkeit / --leistung: nur fuer Messungen an einer Arbeitskopie; 'leer' = NULL."""
    if name not in sys.argv:
        return vorgabe
    text = sys.argv[sys.argv.index(name) + 1]
    return None if text == "leer" else float(text)


def zahl(wert, einheit):
    return "leer" if wert is None else ("%g" % wert).replace(".", ",") + " " + einheit


def main():
    if len(sys.argv) < 2:
        print("Aufruf: referenzprojekt_1064_freie_kuehlung.py <Kenndaten_Test.sqlite> "
              "[--graedigkeit <K|leer>] [--leistung <kW|leer>]")
        return 2
    graedigkeit = wert_aus_argument("--graedigkeit", GRAEDIGKEIT_K)
    leistung = wert_aus_argument("--leistung", LEISTUNG_KW)

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
        grund = pruefe_vorlage(con)
        if grund:
            print(grund + " - Abbruch ohne Schreiben.")
            return 2

        con.execute("BEGIN")
        try:
            neu_id, specs, bericht = duplizieren(con)
            if neu_id != NEU:
                raise RuntimeError("die Kopie faellt auf Id %d statt %d" % (neu_id, NEU))
            print("Kopie %d -> %d \"%s\": %d Zeilen in %d Tabellen"
                  % (VORLAGE, NEU, NAME, sum(n for _, n in bericht), len(bericht)))
            if gegenprobe(con, specs):
                raise RuntimeError("Tabelle(n) mit ungleicher Zeilenzahl")

            con.execute("UPDATE Tab_Projekt SET Beschreibung = ? WHERE ID = ?",
                        (BESCHREIBUNG % (zahl(graedigkeit, "K"), zahl(leistung, "kW")), NEU))
            anlage = [r[0] for r in con.execute(
                "SELECT a.ID FROM Tab_Energieanlagen a JOIN Tab_WP w ON w.ID = a.ID_WP "
                "WHERE a.ID_Projekt = ? AND w.Kuehlbetrieb = 1", (NEU,))]
            if len(anlage) != 1:
                raise RuntimeError("Projekt %d fuehrt %d Waermepumpen im Kuehlbetrieb statt einer" % (NEU, len(anlage)))
            con.execute("UPDATE Tab_Energieanlagen SET Kuehl_Frei = 1, Kuehl_Frei_Graedigkeit_K = ?, "
                        "Kuehl_Frei_Leistung_kW = ? WHERE ID = ?", (graedigkeit, leistung, anlage[0]))
            ist = spaltenwerte(con, "Tab_Energieanlagen", "ID", anlage[0],
                               ["Kuehl_Frei", "Kuehl_Frei_Graedigkeit_K", "Kuehl_Frei_Leistung_kW", "WQ_Typ"])
            if ist != {"Kuehl_Frei": 1, "Kuehl_Frei_Graedigkeit_K": graedigkeit,
                       "Kuehl_Frei_Leistung_kW": leistung, "WQ_Typ": "Erdreich"}:
                raise RuntimeError("Anlagenzeile %d: %r" % (anlage[0], ist))
            print("gesetzt: Tab_Energieanlagen %d Kuehl_Frei 1, Graedigkeit %s, Leistungsgrenze %s"
                  % (anlage[0], zahl(graedigkeit, "K"), zahl(leistung, "kW")))

            # Die Trigger des Kostenstempels stempeln die Uhrzeit des Laufs - leer wie jedes Projekt (Muster 1055).
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
        schnell = con.execute("PRAGMA quick_check").fetchone()[0]
        fremd = con.execute("PRAGMA foreign_key_check").fetchall()
        print("integrity_check: %s; quick_check: %s; foreign_key_check: %d Zeilen" % (pruefung, schnell, len(fremd)))
        return 0 if pruefung == "ok" and schnell == "ok" and not fremd else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
