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
    py Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py [--norm vdi6002|vdi4655|beide]
        [--quelle <ordner vdi6002>] [--ziel <json>]
        [--quelle4655 <ordner vdi4655>] [--ziel4655 <json>]
Ohne --norm laufen beide Ableitungen; fehlen die Originale einer, bleibt sie ungeschrieben.
Ein zweiter Lauf auf denselben Originalen schreibt dieselben Bytes. Die Ausgabe nennt Zahl und
Abweichungsband der Werte, nie einen Originalwert.

VDI 4655 (Stufe Z4b) laeuft unter DERSELBEN Regel, mit einer Ausnahme fuer GANZE ZAHLEN: Die Zahl
der Typtage je Klimazone ist ganzzahlig und summiert sich je Zone auf 365 - ein Wert von 3 Tagen
liesse sich innerhalb von 6 % nicht veraendern. Fuer sie gilt deshalb: jeder Wert weicht um
MINDESTENS EINEN und HOECHSTENS max(2; 6 %) Tag(e) ab, bleibt >= 0, und die Zeilensumme ist wieder
genau 365 (Ausgleich auf den Werten mit der groessten verbleibenden Spanne). Die Faktoren der
Tagesenergie sind Schwankungen um einen Jahresmittelwert und duerfen negativ sein; sie werden
multiplikativ abgeleitet und NICHT renormiert - die Pruefsumme des Originals (Summe aus Anzahl mal
Faktor nahe 0) gilt fuer die abgeleiteten Werte nicht mehr, und der Kopf der JSON-Datei sagt das.
Codes der Typtage, Namen der Klimazonen und Bezeichnungen der Gebaeudevarianten uebernimmt die
Ausgabe NICHT: Die Typtage heissen TT01 bis TTnn in der Reihenfolge der Quelldatei, die
Gebaeudevarianten variante_1 bis variante_n in der Reihenfolge ihres ersten Auftretens, und von den
Klimazonen bleibt allein die Nummer. Ausgabe: Referenzlaeufe/Skripte/vdi4655_abgeleitet.json.

ANWENDERENTSCHEID ZU23 (24.09.2026): Auch das Grundlagenpapier
Dokumentation/aktuell/Grundlagen_5_VDI-4655_Auswertung.md traegt nur noch abgeleitete Zahlen.
Dafuer fuehrt die Ausgabe neben den Rechenwerten den Abschnitt "papierwerte" mit allem, was allein
das Papier braucht: Jahresmittel der Aussentemperatur je Klimazone, die beiden Urlaubstaganteile,
die Jahresstrombedarfe und die Beispielrechnung des Abschnitts 8. Drei Groessen der
Beispielrechnung werden NICHT einzeln gestoert, sondern aus schon abgeleiteten Werten GERECHNET,
damit das Papier in sich stimmt: der Jahres-TWW-Bedarf (Personenzahl mal abgeleiteter
Personenkennwert), der Jahresstrombedarf (Personenzahl mal abgeleiteter Personenkennwert) und die
Tages-TWW-Energien nach Gleichung (3) aus dem abgeleiteten Jahresbedarf und den abgeleiteten
Faktoren. Sie sind damit ebenso wenig Originalwerte wie alles andere in dieser Datei.

Die Sperren fuer A100-Referenzprofil und DIN-4708-Profil bleiben (DIN, nicht VDI).

VERWENDUNG IM TESTKATALOG (tww_testkatalog_fiktiv.py; die JSON-Datei bleibt vollstaendig):
  - Niedrig/mittel/hoch einer Nutzungsart sind Minimum/Mittel/Maximum aus bedarfskennwerte.csv.
    Die Extrema der Quelle sind Monatsextrema; zusammen mit den Monatsfaktoren zaehlt der
    Jahresgang auf den Stufen niedrig und hoch damit doppelt - fuer Testdaten tragbar, fuer einen
    Auslieferungskatalog zu pruefen.
  - Die Montags- und Freitagsprofile des Studentenwohnheims bleiben ungenutzt: Der Katalog kennt
    die Tagtypen Werktag, Samstag, Sonn-/Feiertag und Ruhetag.
  - Campingplatz, die beiden Hallenbaeder und das Ein- und Zweifamilienhaus stehen nicht im
    Katalog (Bezug, den das Schema nicht kennt, keine Profile oder kein Mittelwert); ihre
    abgeleiteten Werte bleiben in der Datei, ungenutzt.
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
DATUM4655 = "2026-09-24"         # Stand der VDI-4655-Ableitung (Stufe Z4b)

HIER = os.path.dirname(os.path.abspath(__file__))
WURZEL = os.path.dirname(os.path.dirname(HIER))
QUELLE_VORGABE = os.path.join(WURZEL, "Referenzlaeufe", "Normzahlen", "vdi6002")
ZIEL_VORGABE = os.path.join(HIER, "tww_katalogwerte_abgeleitet.json")
QUELLE4655_VORGABE = os.path.join(WURZEL, "Referenzlaeufe", "Normzahlen", "vdi4655")
ZIEL4655_VORGABE = os.path.join(HIER, "vdi4655_abgeleitet.json")

# Die Dateien des VDI-4655-Ordners (Struktur in dessen QUELLE.txt).
D4655 = ("typtage.csv", "klimazonen.csv", "typtage_je_zone.csv", "f_twe_tt.csv", "kennwerte.csv")

# Mindest- und Hoechstabweichung einer GANZEN ZAHL (Typtage je Zone) in Tagen.
GANZ_MINDESTENS = 1

# Die Beispielrechnung des Abschnitts 8 der Richtlinie: Strukturangaben, keine Messwerte.
BEISPIEL_PERSONEN = 3
BEISPIEL_ZONE = 5

# Der Jahresstrombedarf je Person faellt in der Quelle mit der Personenzahl; abgeleitet faellt er
# weiter (sonst stuende im Papier eine Reihe, die der Aussage der Richtlinie widerspricht).
FALLENDE_REIHE = tuple("w_a_efh_%d_pers_kwh_je_person" % n for n in range(2, 7))

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


def vdi6002(quelle, ziel):
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


# =====================================================================================
#  VDI 4655 (Stufe Z4b) - Typtage, Anzahl je Klimazone, Faktoren, Kennwerte
# =====================================================================================

JAHRESZEIT = {"\u00dcbergang": "uebergang", "Uebergang": "uebergang", "Sommer": "sommer", "Winter": "winter"}
TAGART = {"Werktag": "werktag", "Sonntag": "sonntag"}
BEWOELKUNG = {"heiter": "heiter", "bewoelkt": "bewoelkt", "bew\u00f6lkt": "bewoelkt",
              "ohne Unterscheidung": "ohne", "ohne": "ohne"}

# Die vier Kennwerte, die der Leser als Grenzen des Verfahrens braucht; je Quellschluessel der
# Zielschluessel. Der Rest von kennwerte.csv wandert unter seinem eigenen Namen mit, sofern er
# eine Zahl ist - Texte bleiben draussen.
KENNWERT_ZIEL = {
    "grenze_uebergang_winter": "wintergrenze",
    "grenze_bewoelkt_bedeckungsgrad": "bewoelkung.schwelle",
    "q_twe_a_efh_kwh_je_person": "q_twe_a_efh_kwh_je_person",
    "q_twe_a_mfh_kwh_je_we": "q_twe_a_mfh_kwh_je_we",
}


def signifikant(text):
    """Zahl der signifikanten Ziffern der Quellzahl (Exponentialschreibweise erlaubt)."""
    mantisse = text.strip().lower().split("e")[0].lstrip("+-")
    ziffern = mantisse.replace(".", "").lstrip("0")
    return max(2, len(ziffern) if ziffern else 2)


def runden_signifikant(x, ziffern):
    return Decimal("%.*g" % (ziffern, x))


def ableiten_reell(text, zeile, verboten=None):
    """
    Ein reeller Wert nach der Regel; er darf negativ sein, nicht aber 0. Ist `verboten` gegeben
    (Menge aller Originalzahlen), meidet der Wert auch die Originalwerte ANDERER Zellen - sonst
    stuende in einer Tabelle des Papiers wieder eine Zahl der Richtlinie, nur in der falschen Zeile.
    """
    v = Decimal(text)
    if v == 0:
        return v, Decimal(0), True            # 0 laesst sich nicht multiplikativ ableiten
    ziffern = signifikant(text)
    for feiner in range(0, 7):
        for k in range(len(D)):
            neu = runden_signifikant(v * (1 + D[(zeile + k) % len(D)]), ziffern + feiner)
            if neu == 0:
                continue
            if verboten is not None and neu.normalize() in verboten:
                continue
            a = abs(neu / v - 1)
            if GLEICH < a <= BAND:
                return neu, a, False
    raise SystemExit("Keine zulaessige Ableitung fuer einen Faktor der Zeile %d." % zeile)


def ableiten_ganz(werte, zeile0):
    """
    Eine Zeile ganzer Zahlen mit fester Summe: jeder Wert weicht um mindestens einen und
    hoechstens max(2; 6 %) Tag(e) ab, bleibt >= 0, und die Summe bleibt die des Originals.
    """
    summe = sum(werte)
    spanne = [max(2, int((Decimal("0.06") * w).to_integral_value(rounding=ROUND_HALF_UP))) for w in werte]
    neu = []
    for i, w in enumerate(werte):
        d = D[(zeile0 + i) % len(D)]
        kandidat = int((Decimal(w) * (1 + d)).to_integral_value(rounding=ROUND_HALF_UP))
        if kandidat == w:                                   # mindestens ein Tag Unterschied
            kandidat = w + (1 if d > 0 else -1)
        kandidat = max(0, min(w + spanne[i], max(w - spanne[i], kandidat)))
        if kandidat == w:
            kandidat = w + 1 if spanne[i] >= 1 else w
        neu.append(kandidat)

    # Ausgleich auf die Originalsumme - immer auf dem Wert mit der groessten freien Spanne.
    for _ in range(10000):
        rest = summe - sum(neu)
        if rest == 0:
            break
        schritt = 1 if rest > 0 else -1
        beste, freiheit = -1, 0
        for i, w in enumerate(werte):
            kandidat = neu[i] + schritt
            if kandidat < 0 or kandidat == w:
                continue
            if abs(kandidat - w) > spanne[i]:
                continue
            frei = spanne[i] - abs(kandidat - w)
            if beste < 0 or frei > freiheit:
                beste, freiheit = i, frei
        if beste < 0:
            raise SystemExit("Die Zeile der Typtage kommt nicht auf ihre Summe zurueck.")
        neu[beste] += schritt
    if sum(neu) != summe:
        raise SystemExit("Die Zeile der Typtage kommt nicht auf ihre Summe zurueck.")
    for i, w in enumerate(werte):
        if neu[i] == w or abs(neu[i] - w) > spanne[i] or neu[i] < 0:
            raise SystemExit("Eine abgeleitete Zahl der Typtage haelt die Regel nicht (Index %d)." % i)
    return neu


def ableiten_fallend(texte, zeile0):
    """
    Eine Reihe, die in der Quelle streng fallend ist (Jahresstrombedarf je Person), bleibt es auch
    abgeleitet: jeder Wert nimmt das erste delta seiner Reihenfolge, das die Regel haelt UND unter
    dem schon angenommenen Vorgaenger bleibt.
    """
    ergebnis, vorher = [], None
    for j, text in enumerate(texte):
        v = Decimal(text)
        ziffern = signifikant(text)
        gewaehlt = None
        for feiner in range(0, 7):
            for k in range(len(D)):
                neu = runden_signifikant(v * (1 + D[(zeile0 + j + k) % len(D)]), ziffern + feiner)
                a = abs(neu / v - 1)
                if not (GLEICH < a <= BAND):
                    continue
                if vorher is not None and neu >= vorher:
                    continue
                gewaehlt = (neu, a)
                break
            if gewaehlt:
                break
        if not gewaehlt:
            raise SystemExit("Keine fallende Ableitung fuer Glied %d der Reihe." % j)
        ergebnis.append(gewaehlt)
        vorher = gewaehlt[0]
    return ergebnis


def kennwertzahl(text, einheit):
    """
    Ein Kennwert als Zahl. Ein Bruch "a/b" (die Bewoelkungsschwelle steht so in der Quelle) wird
    ausgerechnet; traegt er die Einheit "Achtel", zaehlt er in Achteln (also mal 8). Ein Text
    ergibt None.
    """
    s = (text or "").strip()
    try:
        return Decimal(s)
    except Exception:
        pass
    teile = s.split("/")
    if len(teile) != 2:
        return None
    try:
        wert = Decimal(teile[0].strip()) / Decimal(teile[1].strip())
    except Exception:
        return None
    return wert * 8 if (einheit or "").strip().lower().startswith("achtel") else wert


def originalzahlen(quelle):
    """
    Die KENNZEICHNENDEN Zahlen der lokalen Originale als Menge (normierte Decimal): jede, die
    keine ganze Zahl ist, und jede ganze ab 100. Ein abgeleiteter oder gerechneter Wert des
    Papiers darf keiner von ihnen gleichen - auch nicht der einer anderen Zelle, sonst stuende
    eine Zahl der Richtlinie wieder im Papier, nur in der falschen Zeile. Kleine ganze Zahlen
    (Typtage je Zone, Personen- und Wohneinheitenzahlen, Prozentangaben) bleiben draussen: sie
    treten im Papier auch als Seiten-, Tabellen- und Abschnittsnummer auf, sind also nicht
    unterscheidbar; fuer sie gilt die Probe Zelle gegen Zelle.
    """
    menge = set()
    for datei in D4655:
        for r in lies(os.path.join(quelle, datei)):
            for wert in r.values():
                for stueck in str(wert or "").replace(",", " ").replace(";", " ").split():
                    try:
                        v = Decimal(stueck).normalize()
                    except Exception:
                        continue
                    if v != v.to_integral_value() or abs(v) >= 100:
                        menge.add(v)
    return menge


def ohne_kollision(exakt, verboten, d):
    """Rundet `exakt` auf d Stellen; kollidiert das mit einem Originalwert, eine Stelle feiner."""
    for stellen_ in range(d, d + 4):
        kandidat = runden(exakt, stellen_)
        if kandidat.normalize() not in verboten:
            return kandidat
    raise SystemExit("Ein gerechneter Wert des Papiers trifft einen Originalwert.")


def zahlen_pruefen(knoten, verboten, pfad=""):
    """Wacht, dass kein Zahlwert des Abschnitts `papierwerte` einem Originalwert gleicht."""
    if isinstance(knoten, dict):
        for k, v in knoten.items():
            if k in ("personen", "zone"):               # Strukturangaben der Beispielrechnung
                continue
            zahlen_pruefen(v, verboten, pfad + "/" + str(k))
    elif isinstance(knoten, (int, float)) and knoten != 0:
        if Decimal(str(knoten)).normalize() in verboten:
            raise SystemExit("Der abgeleitete Wert %s gleicht einem Originalwert." % pfad)


def vdi4655(quelle, ziel):
    for datei in D4655:
        if not os.path.isfile(os.path.join(quelle, datei)):
            print("Die VDI-4655-Originale fehlen (%s) - nichts geschrieben." % datei)
            return 2

    # --- Typtage: neutrale Codes in der Reihenfolge der Quelldatei ----------------------
    quell_typtage = lies(os.path.join(quelle, "typtage.csv"))
    code_neu = {}
    typtage = []
    for i, r in enumerate(quell_typtage):
        neu = "TT%02d" % (i + 1)
        code_neu[r["code"]] = neu
        js, ta = JAHRESZEIT.get(r["jahreszeit"]), TAGART.get(r["tagart"])
        bw = BEWOELKUNG.get(r["bewoelkung"])
        if not js or not ta or not bw:
            raise SystemExit("Unbekannte Typtagkategorie in Zeile %d von typtage.csv." % (i + 2))
        typtage.append({"code": neu, "jahreszeit": js, "tagart": ta, "bewoelkung": bw})

    # --- Klimazonen: allein die Nummer --------------------------------------------------
    quell_zonen = lies(os.path.join(quelle, "klimazonen.csv"))
    zonen = [int(r["zone"]) for r in quell_zonen]

    # --- Gebaeudevarianten: neutrale Namen in der Reihenfolge des Auftretens ------------
    quell_faktoren = lies(os.path.join(quelle, "f_twe_tt.csv"))
    art_neu, arten = {}, []
    for r in quell_faktoren:
        if r["gebaeude"] in art_neu:
            continue
        art_neu[r["gebaeude"]] = "variante_%d" % (len(arten) + 1)
        arten.append(art_neu[r["gebaeude"]])

    statistik = {"werte": 0, "null": 0, "min": Decimal(1), "max": Decimal(0)}

    def buchen(a):
        statistik["werte"] += 1
        statistik["min"] = min(statistik["min"], a)
        statistik["max"] = max(statistik["max"], a)

    # --- Anzahl der Typtage je Zone: ganze Zahlen mit Summe 365 -------------------------
    # Die Quelle fuehrt sie je VARIANTE (Bestand, Niedrigenergie), die Faktoren je GEBAEUDEART.
    # Eine Gebaeudeart erbt die Zeile der Variante, deren Namen sie als Endung traegt.
    quell_anzahl = lies(os.path.join(quelle, "typtage_je_zone.csv"))
    codes_alt = [r["code"] for r in quell_typtage]
    je_variante = {}
    for i, r in enumerate(quell_anzahl):
        werte = [int(r[c]) for c in codes_alt]
        if sum(werte) != 365:
            raise SystemExit("Zeile %d von typtage_je_zone.csv summiert nicht auf 365." % (i + 2))
        abgeleitet = ableiten_ganz(werte, i * len(werte))
        for w, n in zip(werte, abgeleitet):
            buchen(abs(Decimal(n) / Decimal(w) - 1) if w else Decimal(0))
        je_variante.setdefault(r["variante"], {})[int(r["zone"])] = abgeleitet

    anzahl = {}
    for alt, neu in art_neu.items():
        variante = next((v for v in je_variante if alt.endswith(v)), None)
        if variante is None:
            raise SystemExit("Zur Gebaeudeart %s fuehrt typtage_je_zone.csv keine Variante." % alt)
        anzahl[neu] = {str(z): {code_neu[c]: n for c, n in zip(codes_alt, je_variante[variante][z])}
                       for z in sorted(je_variante[variante])}

    # --- Faktoren der Tagesenergie: reell, duerfen negativ sein -------------------------
    faktoren = {}
    faktoren_dec = {}                                   # dieselben Werte als Decimal (Gl. (3))
    for i, r in enumerate(quell_faktoren):
        neu, a, war_null = ableiten_reell(r["f_twe_tt"], i)
        if war_null:
            statistik["null"] += 1
        else:
            buchen(a)
        faktoren.setdefault(art_neu[r["gebaeude"]], {}).setdefault(str(int(r["zone"])), {})[code_neu[r["typtag"]]] = zahl(neu)
        faktoren_dec.setdefault(art_neu[r["gebaeude"]], {}).setdefault(str(int(r["zone"])), {})[code_neu[r["typtag"]]] = neu

    # --- Kennwerte: nur Zahlen, Texte bleiben draussen ----------------------------------
    quell_kennwerte = lies(os.path.join(quelle, "kennwerte.csv"))
    kennwerte = {}
    for i, r in enumerate(quell_kennwerte):
        schluessel = r["schluessel"]
        # Abgeleitet wird NUR, was der Rechenweg braucht oder was ein echter Kennwert der
        # Richtlinie ist. Struktur- und Geltungsangaben (Tage je Jahr, Zeitaufloesungen, Grenzen
        # der Wohneinheitenzahl, Beispielrechnung) bleiben draussen - sie sind keine Messwerte,
        # und eine "abgeleitete" Zahl von Tagen je Jahr waere Unsinn.
        if not (schluessel in KENNWERT_ZIEL or schluessel.startswith("heizgrenztemperatur_")):
            continue
        roh = kennwertzahl(r["wert"], r.get("einheit"))
        if roh is None:
            continue                                    # ein Text
        if schluessel.startswith("heizgrenztemperatur_"):
            variante = schluessel[len("heizgrenztemperatur_"):]
            treffer = [n for a2, n in art_neu.items() if a2.endswith(variante)]
            if not treffer:
                continue
            abgeleitet, a, _ = ableiten_reell(format(roh, "f"), i)
            buchen(a)
            for tr in treffer:
                kennwerte["heizgrenze." + tr] = zahl(abgeleitet)
            continue
        abgeleitet, a, _ = ableiten_reell(format(roh, "f"), i)
        buchen(a)
        kennwerte[KENNWERT_ZIEL.get(schluessel, schluessel)] = zahl(abgeleitet)

    for pflicht in ("wintergrenze", "bewoelkung.schwelle"):
        if pflicht not in kennwerte:
            raise SystemExit("Der Kennwert %s fehlt in kennwerte.csv." % pflicht)
    for a in arten:
        if "heizgrenze." + a not in kennwerte:
            raise SystemExit("Die Heizgrenze der Gebaeudeart %s fehlt." % a)

    # --- Werte, die allein das Grundlagenpapier fuehrt (Anwenderentscheid ZU23) ----------
    verboten = originalzahlen(quelle)
    # Jahresmittel der Aussentemperatur je Zone (Tabelle 3 des Papiers): reell, je Zeile ein delta.
    # Ihre Originale sind teils ganzzahlig (9,0 oder 3,0 °C) und stehen darum nicht in `verboten`;
    # fuer diese Spalte gilt zusaetzlich ihr eigener Satz, damit kein Wert den einer anderen Zone traegt.
    verboten_zonen = set(verboten)
    for r in quell_zonen:
        try:
            verboten_zonen.add(Decimal((r.get("jahresmittel_c") or "").strip()).normalize())
        except Exception:
            pass
    jahresmittel = {}
    for i, r in enumerate(quell_zonen):
        roh = (r.get("jahresmittel_c") or "").strip()
        if not roh:
            continue
        abgeleitet, a, _ = ableiten_reell(roh, i, verboten_zonen)
        buchen(a)
        jahresmittel[str(int(r["zone"]))] = zahl(abgeleitet)

    # Jahresstrombedarfe, Urlaubstaganteile und die einzeln stoerbaren Groessen des Beispiels.
    # beispiel_q_twe_* bleibt aussen vor: der Jahresbedarf und die zehn Tagesenergien werden aus
    # schon abgeleiteten Werten gerechnet, damit Gleichung (3) im Papier aufgeht.
    papier = {}
    reihe = {}
    for i, r in enumerate(quell_kennwerte):
        schluessel = r["schluessel"]
        if not schluessel.startswith(("w_a_", "urlaubstag_", "beispiel_")):
            continue
        if schluessel.startswith("beispiel_q_twe"):
            continue
        if schluessel in FALLENDE_REIHE:                # geschlossen abgeleitet, damit sie faellt
            reihe[schluessel] = (i, r["wert"])
            continue
        roh = kennwertzahl(r["wert"], r.get("einheit"))
        if roh is None:
            continue                                    # ein Text
        abgeleitet, a, war_null = ableiten_reell(format(roh, "f"), i, verboten)
        if war_null:
            statistik["null"] += 1
        else:
            buchen(a)
        papier[schluessel] = zahl(abgeleitet)

    if len(reihe) == len(FALLENDE_REIHE):
        i0 = reihe[FALLENDE_REIHE[0]][0]
        if [reihe[s][0] for s in FALLENDE_REIHE] != list(range(i0, i0 + len(FALLENDE_REIHE))):
            raise SystemExit("Die Reihe der Jahresstrombedarfe steht nicht in aufeinanderfolgenden Zeilen.")
        for s, (neu, a) in zip(FALLENDE_REIHE, ableiten_fallend([reihe[s][1] for s in FALLENDE_REIHE], i0)):
            buchen(a)
            papier[s] = zahl(neu)

    strom = {s[len("w_a_"):]: w for s, w in papier.items() if s.startswith("w_a_")}
    urlaub = {s[len("urlaubstag_"):]: w for s, w in papier.items() if s.startswith("urlaubstag_")}

    # Die Beispielrechnung des Abschnitts 8: EFH Bestand, drei Personen, Zone 5. Personenzahl und
    # Zone sind Strukturangaben der Richtlinie, keine Messwerte - sie bleiben, wie sie sind.
    beispiel = {}
    art_beispiel = art_neu.get("efh_bestand")
    if art_beispiel and "5" in faktoren_dec.get(art_beispiel, {}):
        n = Decimal(BEISPIEL_PERSONEN)
        q_twe_a = runden(Decimal(str(kennwerte["q_twe_a_efh_kwh_je_person"])) * n, 0)
        w_a = runden(Decimal(str(strom["efh_3_pers_kwh_je_person"])) * n, 0) \
            if "efh_3_pers_kwh_je_person" in strom else None
        q_heiz = {code_neu[c]: papier["beispiel_q_heiz_tt_" + c] for c in codes_alt
                  if "beispiel_q_heiz_tt_" + c in papier}
        w_tt = {code_neu[c]: papier["beispiel_w_tt_" + c] for c in codes_alt
                if "beispiel_w_tt_" + c in papier}
        q_twe, anteil = {}, {}
        for c in codes_alt:
            code = code_neu[c]
            f = faktoren_dec[art_beispiel][str(BEISPIEL_ZONE)][code]
            tag = ohne_kollision(q_twe_a * (Decimal(1) / Decimal(365) + n * f), verboten, 2)
            q_twe[code] = zahl(tag)
            if code in q_heiz:
                nenner = Decimal(str(q_heiz[code])) + tag
                anteil[code] = zahl(ohne_kollision(tag / nenner * 100, verboten, 1)) if nenner else None
        beispiel = {
            "personen": BEISPIEL_PERSONEN,
            "zone": BEISPIEL_ZONE,
            "wohnflaeche_m2": papier.get("beispiel_wohnflaeche_m2"),
            "q_heiz_a_kwh": papier.get("beispiel_q_heiz_a"),
            "w_a_kwh": zahl(w_a) if w_a is not None else None,
            "q_twe_a_kwh": zahl(q_twe_a),
            "q_heiz_tt_kwh": q_heiz,
            "w_tt_kwh": w_tt,
            "q_twe_tt_kwh": q_twe,
            "twe_anteil_prozent": anteil,
        }

    papierwerte = {
        "hinweis": "Werte, die allein das Grundlagenpapier fuehrt. Jahres-TWW-Bedarf, "
                   "Jahresstrombedarf und die zehn Tages-TWW-Energien des Beispiels sind aus den "
                   "abgeleiteten Kennwerten und Faktoren gerechnet (Gleichung (3)), nicht einzeln "
                   "gestoert - sie sind darum ebenfalls keine Originalwerte.",
        "klimazonen_jahresmittel_c": jahresmittel,
        "urlaubstag_anteil_prozent": urlaub,
        "strom_jahresbedarf_kwh": strom,
        "beispiel": beispiel,
    }
    zahlen_pruefen(papierwerte, verboten)

    ergebnis = {
        "kopf": {
            "quelle": "abgeleitet aus VDI 4655 (Ausgabe 2021-07)",
            "regel": "v' = v * (1 + delta), delta zyklisch je Zeilenindex aus (+0,04; -0,03; +0,05; "
                     "-0,04; +0,03; -0,05); reelle Werte auf die signifikanten Ziffern der Quelle "
                     "gerundet, kein Wert gleich dem Original, jeder hoechstens 5,9 % entfernt. GANZE "
                     "ZAHLEN (Typtage je Klimazone): jeder Wert weicht um mindestens einen und "
                     "hoechstens max(2; 6 %) Tag(e) ab, bleibt >= 0, und die Zeilensumme ist wieder 365. "
                     "Skript: Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py",
            "datum": DATUM4655,
            "hinweis": "Kein Wert dieser Datei ist ein Originalwert der Richtlinie (Anwenderentscheid "
                       "ZU19). Codes der Typtage, Namen der Klimazonen und Bezeichnungen der "
                       "Gebaeudevarianten sind neutral vergeben (TT01.., variante_1.., nur die "
                       "Zonennummer). Die Faktoren sind Schwankungen um einen Jahresmittelwert und "
                       "nicht renormiert - die Pruefsumme des Originals gilt hier nicht mehr.",
            "werte": statistik["werte"],
        },
        "typtage": typtage,
        "klimazonen": zonen,
        "gebaeudearten": arten,
        "typtage_je_zone": anzahl,
        "f_twe_tt": faktoren,
        "kennwerte": kennwerte,
        "papierwerte": papierwerte,
    }
    text = json.dumps(ergebnis, ensure_ascii=False, indent=2) + "\n"
    with open(ziel, "w", encoding="utf-8") as f:   # Zeilenende des Arbeitsbaums (Windows: CRLF)
        f.write(text)

    print("VDI 4655 abgeleitet: %d Werte (%d Nullwerte unveraendert); Abweichung vom Original "
          "%.2f %% bis %.2f %%; geschrieben: %s"
          % (statistik["werte"], statistik["null"], statistik["min"] * 100, statistik["max"] * 100,
             os.path.relpath(ziel, WURZEL)))
    return 0


def main():
    args = sys.argv[1:]

    def wert(name, vorgabe):
        return args[args.index(name) + 1] if name in args else vorgabe

    norm = wert("--norm", "beide").lower()
    if norm not in ("beide", "vdi6002", "vdi4655"):
        print("--norm kennt vdi6002, vdi4655 und beide.")
        return 2
    ergebnis = 0
    if norm in ("beide", "vdi6002"):
        ergebnis = max(ergebnis, vdi6002(wert("--quelle", QUELLE_VORGABE), wert("--ziel", ZIEL_VORGABE)))
    if norm in ("beide", "vdi4655"):
        ergebnis = max(ergebnis, vdi4655(wert("--quelle4655", QUELLE4655_VORGABE),
                                        wert("--ziel4655", ZIEL4655_VORGABE)))
    return ergebnis


if __name__ == "__main__":
    sys.exit(main())
