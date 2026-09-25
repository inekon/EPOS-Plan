# -*- coding: utf-8 -*-
"""Gemeinsame Bausteine der Konverter (Umsetzungskonzept Zapfprofilgenerator, Stufe Z5, K5).

Jeder Konverter liest eine offen lizenzierte Messdatenquelle und schreibt sie in das Format des
`Messreihenleser` (`Zeitstempel;Wert (kWh)` bzw. `(m³)`, Trenner `;`, Zahlen mit Punkt, UTF-8)
samt einer `objekt.json` für `Werkzeuge/ZapfprofilValidierung`.

**Nichts davon liegt im Repositorium.** Quelle und Ziel sind Ordner außerhalb; im Repositorium
stehen nur diese Skripte und der Bericht mit Verhältniszahlen (Kapitel 9 K5).

Vier Regeln, die alle Quellen teilen:

1. **Ein Kalenderjahr.** Der Vergleich hält die Messung gegen einen Kalender aus 365 Tagen, der an
   einem bestimmten Wochentag beginnt. Eine Reihe über einen Jahreswechsel hätte zwei
   Wochentagszuordnungen; genommen wird deshalb das Kalenderjahr mit den meisten Werten.
2. **Ein zusammenhängendes Fenster mit höchstens 5 % Lücken** (Parameter
   `Zapfprofil.Validierung.Lueckenanteil`). Fehlende Stunden innerhalb des Fensters bleiben in der
   Datei weg — der Leser füllt und zählt sie. Ein Fenster unter 30 Tagen ist für die Kalibrierung
   zu kurz (`Zapfprofil.Validierung.Kalibrierung.MindestTage`); die Quelle wird dann benannt
   übergangen.
3. **Negative Werte werden auf 0 gesetzt und gezählt.** Eine Zapfung zählt nie rückwärts; negative
   Werte sind Artefakte der Energiebilanz des Zählers. Der Leser lehnt sie ab, die Zahl der
   Nullsetzungen steht im Protokoll und im Vermerk des Objekts.
4. **Bezugsmengen sind Platzhalter**, solange sie nicht aus der Veröffentlichung der Quelle
   belegt sind. Sie gehen nur in die √N-Skalierung (das objektübergreifende vierte Kriterium), nicht
   in die drei Kriterien je Objekt. Jeder Platzhalter steht als solcher im Vermerk.
"""

import datetime
import io
import json
import os

KOPF_ENERGIE = "Zeitstempel;Wert (kWh)"
KOPF_VOLUMEN = "Zeitstempel;Wert (m³)"

LUECKENANTEIL = 0.05
MINDESTTAGE = 30
BTU_JE_KWH = 0.000293071

# Realisierungen des Jahresensembles je Objekt. Der Vergleich braucht sie fuer das Band der
# Dauerlinie: Die Lehre, an der das Band haengt (Konzept 3.6), spricht von der SYNTHETISCHEN Reihe
# des stochastischen Wegs; eine deterministische Reihe traegt die Gleichzeitigkeit nicht. Zehn
# Realisierungen genuegen fuer die Spitzenstreuung und halten den Lauf kurz.
REALISIERUNGEN = 10


def wochentag_jan1(jahr):
    """Wochentag des 1. Januar, 0 = Montag … 6 = Sonntag."""
    return datetime.date(jahr, 1, 1).weekday()


def jahr_mit_meisten(werte):
    """Das Kalenderjahr mit den meisten Werten; `werte` ist eine Liste (datetime, float)."""
    zaehler = {}
    for z, _ in werte:
        zaehler[z.year] = zaehler.get(z.year, 0) + 1
    return max(zaehler, key=lambda j: zaehler[j]) if zaehler else None


def auf_jahr(werte, jahr):
    """Nur die Werte dieses Kalenderjahrs."""
    return [(z, w) for z, w in werte if z.year == jahr]


def nullsetzen(werte):
    """Negative Werte auf 0; liefert (neue Liste, Zahl der Nullsetzungen)."""
    neu, n = [], 0
    for z, w in werte:
        if w < 0.0:
            n += 1
            w = 0.0
        neu.append((z, w))
    return neu, n


def fenster(werte, schritt_min, lueckenanteil=LUECKENANTEIL, mindesttage=MINDESTTAGE):
    """Das längste zusammenhängende Fenster mit höchstens `lueckenanteil` fehlenden Schritten.

    `werte` ist nach Zeit geordnet und enthält nur vorhandene Schritte. Gerechnet wird auf dem
    Raster `schritt_min`: Die Zahl der Soll-Schritte zwischen zwei Werten folgt aus ihrem Abstand.
    Liefert (Teil der Liste, Lückenanteil) oder (None, None), wenn kein Fenster lang genug ist.
    """
    if not werte:
        return None, None
    schritt = datetime.timedelta(minutes=schritt_min)

    def index(z):
        return int((z - werte[0][0]).total_seconds() // (schritt_min * 60))

    stellen = [index(z) for z, _ in werte]

    def fehlend(links, rechts):
        """Fehlende Schritte im Fenster; bei einem WIEDERHOLTEN Zeitstempel (Herbstumstellung der
        Sommerzeit) liegen zwei Werte auf derselben Stelle — dann ist der Wert negativ und die
        Lücke ist 0, nicht weniger als null."""
        soll = stellen[rechts] - stellen[links] + 1
        return soll, max(0, soll - (rechts - links + 1))

    bester, best_anteil, best_soll = None, None, 0
    links = 0
    for rechts in range(len(werte)):
        while links < rechts:
            soll, fehlt = fehlend(links, rechts)
            if fehlt <= lueckenanteil * soll:
                break
            links += 1
        soll, fehlt = fehlend(links, rechts)
        if fehlt > lueckenanteil * soll:
            continue
        if soll > best_soll:
            best_soll = soll
            if soll * schritt_min / 1440.0 >= mindesttage:
                bester = (links, rechts)
                best_anteil = fehlt / float(soll)
    if bester is None:
        return None, best_soll * schritt_min / 1440.0
    return werte[bester[0]:bester[1] + 1], best_anteil


def reihe_schreiben(pfad, kopf, werte):
    """Schreibt die Reihe als CSV (UTF-8 ohne Vorspann, CRLF, Punkt als Dezimalzeichen)."""
    os.makedirs(os.path.dirname(pfad), exist_ok=True)
    with io.open(pfad, "w", encoding="utf-8", newline="\r\n") as f:
        f.write(kopf + "\n")
        for z, w in werte:
            f.write("%s;%.6f\n" % (z.strftime("%Y-%m-%dT%H:%M"), w))


def objekt_schreiben(pfad, kennung, nutzungsart, bezugsmenge, groesse, zeitstempel, jahr,
                     bilanzgrenze, vermerk, quelle, zapf=60.0, kalt=12.0, amplitude=2.0,
                     zirkulation=True, niveau="Mittel", realisierungen=REALISIERUNGEN, seed=20260926):
    """Schreibt die `objekt.json` des Objekts."""
    inhalt = {
        "kennung": kennung,
        "nutzungsart": nutzungsart,
        "bezugsmenge": bezugsmenge,
        "niveau": niveau,
        "bilanzgrenze": bilanzgrenze,
        "zirkulation": zirkulation,
        "zapftemperatur_c": zapf,
        "kaltwasser_mittel_c": kalt,
        "kaltwasser_amplitude_k": amplitude,
        "kalender": {
            "wochentag_jan1": wochentag_jan1(jahr),
            "feiertagsregion": "unbekannt (die Quelle nennt keine Feiertage)",
            "feiertage": [],
            "ferien": []
        },
        "messung": {
            "datei": "messreihe.csv",
            "groesse": groesse,
            "zeitstempel": zeitstempel,
            "lueckenanteil_hoechstens": LUECKENANTEIL,
            "quelle": quelle
        },
        "stochastik": {
            "seed": seed,
            "realisierungen": realisierungen,
            "jahresreihe_stochastisch": realisierungen >= 2
        },
        "vermerk": vermerk
    }
    os.makedirs(os.path.dirname(pfad), exist_ok=True)
    with io.open(pfad, "w", encoding="utf-8", newline="\r\n") as f:
        json.dump(inhalt, f, ensure_ascii=False, indent=2)
        f.write("\n")


def stunden_summieren(werte, feiner_schritt_min):
    """Fasst feinere Schritte zu vollen Stunden zusammen (Summe je Kalenderstunde).

    Eine Stunde zählt nur, wenn sie ALLE ihre Schritte trägt — eine angeschnittene Stunde täuschte
    einen kleinen Stundenwert vor (dieselbe Regel wie `Messreihe.Stundenwerte`).
    """
    je_stunde = int(60 // feiner_schritt_min)
    eimer = {}
    for z, w in werte:
        s = z.replace(minute=0, second=0, microsecond=0)
        a, n = eimer.get(s, (0.0, 0))
        eimer[s] = (a + w, n + 1)
    return [(s, a) for s, (a, n) in sorted(eimer.items()) if n == je_stunde]


def bericht(kennung, werte, anteil, null, zusatz=""):
    """Eine Protokollzeile — ohne Absolutmengen, nur Zählungen und Anteile."""
    tage = len(werte) * 1.0 / 24.0
    return ("%-14s %5d Stunden (%.1f d), Lueckenanteil %.4f, Nullsetzungen %d%s"
            % (kennung, len(werte), tage, anteil or 0.0, null, (" — " + zusatz) if zusatz else ""))
