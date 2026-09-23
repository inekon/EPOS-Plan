#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Leitet aus den lokalen VDI-6002-Normzahlen GERINGFUEGIG ABWEICHENDE Katalogwerte ab und schreibt
sie nach Referenzlaeufe/Skripte/tww_katalogwerte_abgeleitet.json (Anwenderentscheid ZU19 vom
23.09.2026: "geringfuegig abweichende VDI-Werte ins Repository"; Umsetzungskonzept
Zapfprofilgenerator, Stufe Z3, Gruppe 2).

NUR LOKAL LAUFFAEHIG. Die Originale liegen gitignoriert unter Referenzlaeufe/Normzahlen/vdi6002/
(bedarfskennwerte.csv, tagesprofile.csv, wochenanteile.csv, saisonfaktoren.csv, vdi6002.json;
Struktur und Bedeutung in der dortigen QUELLE.txt). Sie verlassen den Rechner nie; committet wird
allein die abgeleitete JSON-Datei, und sie traegt KEINEN Originalwert. Das Katalogskript
tww_testkatalog_fiktiv.py liest nur diese JSON-Datei und laeuft ohne die Originale.

DIE REGEL (deterministisch, wiederholbar, byte-gleich):

  1. Jede Datei wird fuer sich zeilenweise gelesen; i ist der Index der Datenzeile (ab 0, ohne
     Kopfzeile) in ihrer Datei. Jeder Zahlenwert v der Zeile i wird
         v' = v * (1 + delta)     mit delta = D[(i + k) mod 6],
         D = (+0,04; -0,03; +0,05; -0,04; +0,03; -0,05),  k = 0 fuer den ersten Versuch.
     In bedarfskennwerte.csv tragen alle Zahlen einer Zeile (mittel, minimum, maximum,
     winterspitze, sommerschwachlast) dasselbe delta; leere Felder bleiben leer.
  2. Gerundet wird kaufmaennisch auf d Nachkommastellen, d = max(Stellen der Quelle, Stellen fuer
     zwei signifikante Ziffern des Originals).
  3. Verteilungen werden nach der Stoerung renormiert und erst dann gerundet: je Nutzungsart und
     Tagtyp die 24 Stundenanteile (Summe 1), je Nutzungsart die sieben Wochenanteile (Summe 1),
     je Nutzungsart die zwoelf Monatsfaktoren (Mittel 1). Das Katalogskript normiert die
     gerundeten Werte beim Einspielen noch einmal exakt (Summe bzw. Mittel genau 1).
  4. Kein Wert darf seinem Original gleichen (relativ <= 1e-9), und jeder Wert - der gerundete der
     JSON-Datei wie der exakt normierte des Katalogs - liegt hoechstens 5,9 % vom Original entfernt
     (Wache: 6 %). Verletzt ein Wert das, nimmt er das naechste delta (k + 1); hat er alle sechs
     ohne Erfolg versucht, rundet er eine Nachkommastelle feiner (d + 1) und beginnt wieder bei
     k = 0. Eine Verteilung wird danach als Ganzes neu normiert, bis jeder Wert besteht.
  5. Schluessel (Nutzungsarten, Tagtypen, Stunden, Wochentage, Monate), Kategorien und Struktur
     bleiben unveraendert; Seiten- und Tabellenverweise der Quelle werden nicht uebernommen.

Aufruf (Windows: `py`, sonst `python3`; ohne Argument die Originale im Arbeitsbaum):
    py Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py [--quelle <ordner vdi6002>] [--ziel <json>]
Ein zweiter Lauf auf denselben Originalen schreibt dieselben Bytes. Die Ausgabe nennt Zahl und
Abweichungsband der Werte, nie einen Originalwert.

VDI 4655 folgt erst mit Stufe Z4b unter derselben Regel; die Sperren fuer A100-Referenzprofil und
DIN-4708-Profil bleiben (DIN, nicht VDI).
"""

import csv
import json
import os
import sys
from decimal import Decimal, ROUND_HALF_UP

D = (Decimal("0.04"), Decimal("-0.03"), Decimal("0.05"), Decimal("-0.04"), Decimal("0.03"), Decimal("-0.05"))
BAND = Decimal("0.059")          # hoechste relative Abweichung der Ableitung (Wache: 0,06)
GLEICH = Decimal("1e-9")         # relativ darunter gilt ein Wert als unveraendert
DATUM = "2026-09-23"             # Stand der Ableitung (fest, damit der Lauf byte-gleich bleibt)

HIER = os.path.dirname(os.path.abspath(__file__))
WURZEL = os.path.dirname(os.path.dirname(HIER))
QUELLE_VORGABE = os.path.join(WURZEL, "Referenzlaeufe", "Normzahlen", "vdi6002")
ZIEL_VORGABE = os.path.join(HIER, "tww_katalogwerte_abgeleitet.json")

BEDARF_SPALTEN = ("mittel", "minimum", "maximum", "winterspitze", "sommerschwachlast")


def stellen(text):
    """Nachkommastellen der Quellzahl (Dezimalpunkt)."""
    return len(text.split(".")[1]) if "." in text else 0


def zwei_signifikante(v):
    """Nachkommastellen fuer zwei signifikante Ziffern von v > 0."""
    return max(0, 1 - v.adjusted())


def runden(x, d):
    return x.quantize(Decimal(1).scaleb(-d), rounding=ROUND_HALF_UP)


def abweichung(neu, alt):
    return abs(neu / alt - 1)


def zulaessig(neu, alt):
    a = abweichung(neu, alt)
    return GLEICH < a <= BAND


class Wert:
    """Ein Originalwert samt Zeilenindex, Versuch k und Stellenzahl d."""

    def __init__(self, text, zeile):
        self.text = text
        self.v = Decimal(text)
        if self.v <= 0:
            raise SystemExit("Ein Wert <= 0 laesst sich nicht multiplikativ ableiten (Zeile %d)." % zeile)
        self.i = zeile
        self.k = 0
        self.d = max(stellen(text), zwei_signifikante(self.v))
        self.feiner = 0

    def roh(self):
        return self.v * (1 + D[(self.i + self.k) % len(D)])

    def weiter(self):
        """Das naechste delta; nach allen sechs eine Stelle feiner (Regel 4)."""
        self.k += 1
        if self.k >= len(D):
            self.k = 0
            self.d += 1
            self.feiner += 1
            if self.feiner > 6:
                raise SystemExit("Keine zulaessige Ableitung fuer Zeile %d." % self.i)


def einzeln(werte):
    """Werte ohne Verteilung (Bedarfskennwerte): runden, pruefen, sonst weiter."""
    ergebnis = []
    for w in werte:
        while True:
            neu = runden(w.roh(), w.d)
            if zulaessig(neu, w.v):
                ergebnis.append(neu)
                break
            w.weiter()
    return ergebnis


def verteilung(werte, ziel):
    """Werte einer Verteilung mit Summe `ziel`: stoeren, renormieren, runden, pruefen (Regel 3, 4)."""
    for _ in range(1000):
        roh = [w.roh() for w in werte]
        s = sum(roh)
        json_werte = [runden(r * ziel / s, w.d) for r, w in zip(roh, werte)]
        sj = sum(json_werte)
        katalog = [j * ziel / sj for j in json_werte]
        schlecht = [w for w, j, c in zip(werte, json_werte, katalog)
                    if not (zulaessig(j, w.v) and zulaessig(c, w.v))]
        if not schlecht:
            return json_werte, katalog
        for w in schlecht:
            w.weiter()
    raise SystemExit("Die Verteilung kommt nicht zur Ruhe.")


def zahl(x):
    """Ein Decimal als JSON-Zahl in der gerundeten Schreibweise (ohne Exponent)."""
    t = format(x, "f")
    return json.loads(t)


def lies(pfad):
    with open(pfad, encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f, delimiter=";"))


def main():
    args = sys.argv[1:]
    quelle = QUELLE_VORGABE
    ziel = ZIEL_VORGABE
    if "--quelle" in args:
        quelle = args[args.index("--quelle") + 1]
    if "--ziel" in args:
        ziel = args[args.index("--ziel") + 1]
    for datei in ("bedarfskennwerte.csv", "tagesprofile.csv", "wochenanteile.csv", "saisonfaktoren.csv", "vdi6002.json"):
        if not os.path.isfile(os.path.join(quelle, datei)):
            print("Die Originale fehlen (%s) - das Skript laeuft nur lokal. Abbruch ohne Schreiben." % datei)
            return 2

    statistik = {"werte": 0, "feiner": 0, "min": Decimal(1), "max": Decimal(0)}

    def buchen(werte, ergebnis):
        for w, e in zip(werte, ergebnis):
            a = abweichung(e, w.v)
            statistik["werte"] += 1
            statistik["feiner"] += 1 if w.feiner else 0
            statistik["min"] = min(statistik["min"], a)
            statistik["max"] = max(statistik["max"], a)

    # --- Bedarfskennwerte ------------------------------------------------------------------
    bedarf = []
    for i, r in enumerate(lies(os.path.join(quelle, "bedarfskennwerte.csv"))):
        werte = [Wert(r[s], i) for s in BEDARF_SPALTEN if r[s] != ""]
        abgeleitet = einzeln(werte)
        buchen(werte, abgeleitet)
        ergebnis = iter(abgeleitet)
        zeile = {"nutzungsart": r["nutzungsart"], "bezug": r["bezug"], "blatt": r["blatt"], "einheit": r["einheit"]}
        for s in BEDARF_SPALTEN:
            zeile[s] = zahl(next(ergebnis)) if r[s] != "" else None
        bedarf.append(zeile)

    # --- Verteilungen ---------------------------------------------------------------------
    def gruppen(datei, schluessel, feld):
        zeilen = lies(os.path.join(quelle, datei))
        reihen = {}
        for i, r in enumerate(zeilen):
            reihen.setdefault(tuple(r[s] for s in schluessel), []).append((r, Wert(r[feld], i)))
        return reihen

    tagesprofile = {}
    for (art, tagtyp), liste in gruppen("tagesprofile.csv", ("nutzungsart", "tagtyp"), "anteil").items():
        liste.sort(key=lambda p: int(p[0]["stunde"]))
        if [int(p[0]["stunde"]) for p in liste] != list(range(24)):
            raise SystemExit("Tagesprofil %s/%s traegt nicht die Stunden 0..23." % (art, tagtyp))
        werte = [w for _, w in liste]
        json_werte, _ = verteilung(werte, Decimal(1))
        buchen(werte, json_werte)
        tagesprofile.setdefault(art, {})[tagtyp] = [zahl(x) for x in json_werte]

    wochentage = ("mo", "di", "mi", "do", "fr", "sa", "so")
    wochenanteile = {}
    for (art,), liste in gruppen("wochenanteile.csv", ("nutzungsart",), "anteil").items():
        liste.sort(key=lambda p: wochentage.index(p[0]["wochentag"]))
        if [p[0]["wochentag"] for p in liste] != list(wochentage):
            raise SystemExit("Wochenanteile %s tragen nicht Mo..So." % art)
        werte = [w for _, w in liste]
        json_werte, _ = verteilung(werte, Decimal(1))
        buchen(werte, json_werte)
        wochenanteile[art] = {t: zahl(x) for t, x in zip(wochentage, json_werte)}

    monate = ("jan", "feb", "mar", "apr", "mai", "jun", "jul", "aug", "sep", "okt", "nov", "dez")
    saisonfaktoren = {}
    for (art,), liste in gruppen("saisonfaktoren.csv", ("nutzungsart",), "faktor").items():
        liste.sort(key=lambda p: monate.index(p[0]["monat_oder_periode"]))
        if [p[0]["monat_oder_periode"] for p in liste] != list(monate):
            raise SystemExit("Saisonfaktoren %s tragen nicht Jan..Dez." % art)
        werte = [w for _, w in liste]
        json_werte, _ = verteilung(werte, Decimal(12))
        buchen(werte, json_werte)
        saisonfaktoren[art] = {m: zahl(x) for m, x in zip(monate, json_werte)}

    # Gegenprobe: die JSON-Fassung der Originale nennt dieselben Nutzungsarten wie die CSV.
    with open(os.path.join(quelle, "vdi6002.json"), encoding="utf-8") as f:
        original = json.load(f)
    for teil, abgeleitet in (("tagesprofile", tagesprofile), ("wochenanteile", wochenanteile),
                             ("saisonfaktoren", saisonfaktoren)):
        if set(original.get(teil, {}).keys()) != set(abgeleitet.keys()):
            raise SystemExit("vdi6002.json und die CSV nennen im Teil %s verschiedene Nutzungsarten." % teil)
    if [b["nutzungsart"] for b in original.get("bedarf", [])] != [b["nutzungsart"] for b in bedarf]:
        raise SystemExit("vdi6002.json und bedarfskennwerte.csv nennen verschiedene Nutzungsarten.")

    ergebnis = {
        "kopf": {
            "quelle": "abgeleitet aus VDI 6002 Blatt 1 und Blatt 2 (Ausgabe 2014-03)",
            "regel": "v' = v * (1 + delta), delta zyklisch je Zeilenindex aus (+0,04; -0,03; +0,05; -0,04; "
                     "+0,03; -0,05); Rundung auf die Stellenzahl der Quelle, mindestens zwei signifikante "
                     "Ziffern; Verteilungen renormiert (Tagesgang und Woche Summe 1, Monate Mittel 1); kein "
                     "Wert gleich dem Original, jeder hoechstens 5,9 % entfernt (sonst naechstes delta, dann "
                     "eine Stelle feiner). Skript: Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py",
            "datum": DATUM,
            "hinweis": "Kein Wert dieser Datei ist ein Originalwert der Richtlinie (Anwenderentscheid ZU19). "
                       "Bedarfswerte in Litern je Einheit und Tag bei 60 Grad C wie die Quelle.",
            "werte": statistik["werte"],
        },
        "bedarf": bedarf,
        "tagesprofile": tagesprofile,
        "wochenanteile": wochenanteile,
        "saisonfaktoren": saisonfaktoren,
    }
    text = json.dumps(ergebnis, ensure_ascii=False, indent=2) + "\n"
    with open(ziel, "w", encoding="utf-8") as f:   # Zeilenende des Arbeitsbaums (Windows: CRLF)
        f.write(text)

    print("abgeleitet: %d Werte (%d mit feinerer Rundung); Abweichung vom Original %.2f %% bis %.2f %%; "
          "geschrieben: %s" % (statistik["werte"], statistik["feiner"], statistik["min"] * 100,
                               statistik["max"] * 100, os.path.relpath(ziel, WURZEL)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
