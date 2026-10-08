#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Legt das Referenzprojekt 1061 "Referenzprojekt KK" der Testdatenbank an - die Kopie des Referenzprojekts 1058
(Stufe AK3, Heizungspuffer, Raumeinfluss, Anlagenfahrplan, reversible Waermepumpe als Kaelteerzeuger) mit
gesetzter Kuehlkurve am Gebaeude (Entwurf KK Abschnitt 4, RP-KK; E106 Q-KK-6 (a); Einfrierregel "gesaete
Auslegungsdaten der Uebergabe").

AUSGANG. 1058 rechnet die Kaelteseite am festen Kuehlvorlauf: Tab_WP.Kuehl_Vorlauf 18 Grad C, die Kuehldecke
(Vorgaben 16/19 Grad C, Vorlaufgrenze 16 Grad C) bekommt max(18, 16) = 18 Grad C. 18 Grad C ist zugleich die obere
Stuetzstelle der Kuehlkennlinie (7/18 Grad C): Eine Kuehlkurve kann dort nur kaelter fahren und senkt die EER
(Befund KK3). Auf 1058 selbst waere die Kurve ein Basiswechsel - deshalb eine KOPIE, 1058 bleibt Zelle fuer Zelle.

WAS DIESES SKRIPT TUT (eine Transaktion, kein VACUUM).
  1. Die Kopie 1058 -> 1061 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl.Duplizieren), Wortlaut und
     Regeln wie in den Skripten von 1058 und 1059; einzige Abweichung: Die Kopie faellt auf die Id 1061 statt
     der naechsten freien (1060 ist vorgemerkt). Die zwei Nachzuege (Kostenpositionen, Speicherauslegung) haben
     bei 1058 nichts zu tun - geprueft, sonst Abbruch. Gegenprobe: jede kopierte Tabelle traegt fuer 1061 so
     viele Zeilen wie fuer 1058.
  2. Zellen der Kopie:
       Tab_Projekt   Beschreibung (neutraler Zweck), Kosten_Geaendert leer
       Tab_Gebaeude  Kuehlkurve_Aktiv 1, Kuehlkurve_Raumeinfluss 3 K/K, Kuehlkurve_Auslegung_Weg 'tagesmittel'
                     (Weg 2, die Vorgabe), Kuehlkurve_Fusspunkt und Kuehlkurve_Auslegung_Aussen leer (Fusspunkt =
                     Auslegungsruecklauf 19 Grad C der Kuehldecke, ueber dem festen Vorlauf)
       Tab_WP        Kuehl_Vorlauf 18 -> 12 Grad C (der Anlagenvorlauf, an dem der feste Weg die Kennlinie liest)
     Kopplungsstufe AK3, Puffer, Senken, Heizkurve mit Raumeinfluss, Fahrplan, Kaskade und Kuehlkennlinie bleiben
     wie in 1058.

DIE WERTE UND WARUM.
  Anlagenvorlauf 12 Grad C (der Weg mit den wenigsten neuen Einfrierdaten: eine Zelle, keine neue Stuetzstelle):
      Am festen Weg liefert die Waermepumpe 12 Grad C, die Kuehldecke mischt auf ihre Grenze 16 Grad C hoch; mit
      Kurve gleitet der Erzeuger mit dem verlangten Vorlauf zwischen Grenze und Fusspunkt und faehrt an milden
      Tagen waermer - die Kennlinie zwischen 7 und 18 Grad C traegt den Gewinn. Eine Stuetzstelle dazwischen
      haette die Kennlinie (gesaete Kaeltedaten) geaendert, ein tieferer Auslegungsvorlauf die Kuehldecke.
  Raumeinfluss 3 K/K: die Vorgabe des Dialogs (GebaeudeFestwerte.KUEHLKURVE_RAUMEINFLUSS_VORGABE, Probe KK3).
  MESSUNG (KK5a, EPOS.Kern.Tests KuehlkurveFeldlaufTests.Messreihe_der_Staerke mit KK3_PROJEKT=1061; Kuehlen:
      Ueberschreitungsstunden / Kelvinstunden, Kaelte, EER der Kaeltekaskade, Durchlaeufe Mittel / max):
        Weg                                  Kuehlen h / Kh   Kaelte kWh   EER    Durchlaeufe
        1058 fest 18 Grad C (Bestand)        34 / 45,9        3808         5,24   3,21 / 7
        1061 fest 12 Grad C (ohne Kurve)     42 / 62,0        3871         4,46   3,21 / 7
        1061 Kurve, Raumeinfluss 0           34 / 47,9        3767         5,18   3,21 / 7
        1061 Kurve, Raumeinfluss 1 K/K       34 / 47,9        3791         5,15   3,43 / 8
        1061 Kurve, Raumeinfluss 3 K/K       32 / 45,1        3831         5,09   3,48 / 10
        1061 Kurve, Raumeinfluss 5 K/K       32 / 44,7        3855         5,05   3,48 / 9
      Mit Kurve rechnet 1061 Zahl fuer Zahl wie 1058 mit Kurve (der Erzeuger gleitet, der feste Anlagenvorlauf
      wirkt nicht). Gegen den festen Weg desselben Projekts hebt die Kurve die EER um 14 % (4,46 -> 5,09) und
      senkt die Ueberschreitung (42 -> 32 h, 62 -> 45 Kh); der Raumeinfluss 3 K/K senkt den mittleren Vorlauf der
      Kuehlstunden von 18,5 auf 17,7 Grad C (568 h abgesenkt, 684 Kh, 83 h an der Vorlaufgrenze 16 Grad C) und
      kostet 1,8 % EER fuer 2 Ueberschreitungsstunden weniger. Bei 18 Grad C fest (1058) senkt die Kurve die EER
      (5,24 -> 5,09) - der Befund KK3, der den tieferen Anlagenvorlauf begruendet.

WIEDERHOLBAR. Steht Projekt 1061 schon, bricht das Skript mit Meldung ab und schreibt nichts (Rueckgabe 0,
wenn es "Referenzprojekt KK" heisst, sonst 2). Weicht die Vorlage ab, faellt die Kopie auf eine andere Id als
1061 oder misslingt eine Pruefung, rollt die Transaktion zurueck (Rueckgabe 2).

PRUEFUNGEN. Vorlage 1058 (Name, ein Gebaeude mit Kuehlung und Kuehldecke ohne Kuehlkurve, Stufe AK3, eine
Waermepumpe im Kuehlbetrieb mit Kuehl_Vorlauf 18, keine Zonen), Gegenprobe der Zeilenzahlen, foreign_key_check
und integrity_check.

FOLGE. Die Zellen sind gesaete Auslegungsdaten der Uebergabe eines Referenzprojekts (Einfrierregeln,
`Referenzlaeufe/LIESMICH.md`); gehalten von `EPOS.Kern.Tests/KuehlkurveReferenzprojektWacheTests`. Die Regeln
fuer Gebaeude-, Kaelte- und Fahrplandaten gelten fuer 1061 wie fuer 1058. Die Basis R44 friert KK5b ein.

Aufruf (Windows: `py`, sonst `python3`; vorher sichern):
    py Referenzlaeufe/Skripte/referenzprojekt_1061_kk.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

VORLAGE = 1058
VORLAGE_NAME = "Referenzprojekt AK3"
VORLAGE_GEBAEUDE = 10664
NEU = 1061
NAME = "Referenzprojekt KK"
STUFE = "AK3"
KUEHL_VORLAUF_VORLAGE = 18.0
KUEHL_VORLAUF = 12.0
RAUMEINFLUSS = 3.0
AUSLEGUNG_WEG = "tagesmittel"


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
    # Einzige Abweichung vom Kopierweg des Programms: 1060 ist vorgemerkt, die Kopie nimmt NEU statt der naechsten freien Id.
    frei = freie_projekt_id(c, specs, max(VORLAGE + offset["tab_projekt"], NEU))
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


def pruefe_vorlagen(c):
    """1058 wie AK3-W5 es hinterlassen hat."""
    if eine_zeile(c, "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (VORLAGE,))[0] != VORLAGE_NAME:
        return "Vorlage %d heisst nicht %r" % (VORLAGE, VORLAGE_NAME)
    gebaeude = [r[0] for r in c.execute("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", (VORLAGE,))]
    if gebaeude != [VORLAGE_GEBAEUDE]:
        return "Vorlage %d fuehrt die Gebaeude %r statt [%d]" % (VORLAGE, gebaeude, VORLAGE_GEBAEUDE)
    g = spaltenwerte(c, "Tab_Gebaeude", "ID", VORLAGE_GEBAEUDE,
                     ["Kuehlung_Aktiv", "Kuehluebergabe_Aktiv", "Kuehl_Uebergabe_Art", "Kuehlkurve_Aktiv"])
    if g != {"Kuehlung_Aktiv": 1, "Kuehluebergabe_Aktiv": 1, "Kuehl_Uebergabe_Art": "KUEHLDECKE", "Kuehlkurve_Aktiv": None}:
        return "Vorlagengebaeude %d: %r" % (VORLAGE_GEBAEUDE, g)
    if anzahl(c, "SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude = ?", (VORLAGE_GEBAEUDE,)):
        return "Vorlagengebaeude %d fuehrt Zonen" % VORLAGE_GEBAEUDE
    e = spaltenwerte(c, "Tab_Einstellungen", "ID_Projekt", VORLAGE, ["Anlagenkopplung", "Kuehlbetrieb"])
    if e != {"Anlagenkopplung": STUFE, "Kuehlbetrieb": 1}:
        return "Vorlage %d: %r statt Stufe AK3 mit Kuehlbetrieb" % (VORLAGE, e)
    wp = [r for r in c.execute("SELECT ID, Kuehlbetrieb, Kuehl_Vorlauf FROM Tab_WP WHERE ID_Projekt = ?", (VORLAGE,))]
    if len(wp) != 1 or wp[0][1] != 1 or wp[0][2] != KUEHL_VORLAUF_VORLAGE:
        return "Vorlage %d: Waermepumpen %r statt einer im Kuehlbetrieb mit 18 Grad C" % (VORLAGE, wp)
    if anzahl(c, "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt Kostenpositionen - der Ankernachzug des Programms fehlt hier" % VORLAGE
    if anzahl(c, "SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt eine Speicherauslegung - deren Bezugsnachzug fehlt hier" % VORLAGE
    return None


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
        print("Aufruf: referenzprojekt_1061_kk.py <Kenndaten_Test.sqlite> [--kuehl-vorlauf <Grad C>]")
        return 2
    # --kuehl-vorlauf: nur fuer die Messung an einer Arbeitskopie, nie fuer die Testdatenbank.
    vorlauf = float(sys.argv[sys.argv.index("--kuehl-vorlauf") + 1]) if "--kuehl-vorlauf" in sys.argv else KUEHL_VORLAUF

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
        grund = pruefe_vorlagen(con)
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
                        ("Referenzprojekt KK: Kopie von Projekt 1058 (Stufe AK3, Heizungspuffer, Raumeinfluss, Fahrplan) "
                         "mit Kühlkurve am Gebäude (Raumeinfluss 3 K/K) und Kühlvorlauf der Wärmepumpe %g °C - der "
                         "Erzeuger gleitet mit der Kurve." % vorlauf, NEU))
            geb = [r[0] for r in con.execute("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", (NEU,))]
            if len(geb) != 1:
                raise RuntimeError("Projekt %d fuehrt %d Gebaeude statt eines" % (NEU, len(geb)))
            con.execute("UPDATE Tab_Gebaeude SET Kuehlkurve_Aktiv = 1, Kuehlkurve_Raumeinfluss = ?, "
                        "Kuehlkurve_Auslegung_Weg = ?, Kuehlkurve_Fusspunkt = NULL, Kuehlkurve_Auslegung_Aussen = NULL "
                        "WHERE ID = ?", (RAUMEINFLUSS, AUSLEGUNG_WEG, geb[0]))
            print("gesetzt: Tab_Gebaeude %d Kuehlkurve_Aktiv 1, Raumeinfluss %g K/K, Weg %s" % (geb[0], RAUMEINFLUSS, AUSLEGUNG_WEG))
            wp = [r[0] for r in con.execute("SELECT ID FROM Tab_WP WHERE ID_Projekt = ? AND Kuehlbetrieb = 1", (NEU,))]
            if len(wp) != 1:
                raise RuntimeError("Projekt %d fuehrt %d Waermepumpen im Kuehlbetrieb statt einer" % (NEU, len(wp)))
            con.execute("UPDATE Tab_WP SET Kuehl_Vorlauf = ? WHERE ID = ?", (vorlauf, wp[0]))
            print("gesetzt: Tab_WP %d Kuehl_Vorlauf %g -> %g" % (wp[0], KUEHL_VORLAUF_VORLAGE, vorlauf))

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
        fremd = con.execute("PRAGMA foreign_key_check").fetchall()
        print("integrity_check: %s; foreign_key_check: %d Zeilen" % (pruefung, len(fremd)))
        return 0 if pruefung == "ok" and not fremd else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
