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
4. **Jede Bezugsmenge nennt ihre Herkunft** (`bezugsmenge_herkunft`): `Veroeffentlichung`
   (belegt), `Abgeleitet` (aus einer belegten Größe mit benannter Annahme), `Platzhalter` oder
   `Unbekannt` (das Niveau kommt allein aus der Kalibrierung). Platzhalter und unbekannte Mengen
   tragen die √N-Skalierung nicht; das Werkzeug lässt sie dort benannt weg. Die Kennwerte stehen je
   Quelle als Stammdatentabelle mit Zitat im Konverter — nur anonyme Kennung und Kennwert.

Dazu der **Kalender**: die Feiertage des Landes und Messjahrs als Jahrestage (`feiertage`),
berechnet, nicht abgeschrieben (Osterformel, n-ter Wochentag); Quellen bei `feiertage_datum`.
**Ferienfenster bleiben leer** — im Format sind sie Ruhetage der Zone (Betrieb geschlossen);
Schulferien machen aber weder ein Wohnhaus noch ein Hotel oder Pflegeheim zu, ein Ruhetag würde dort
Bedarf wegrechnen, den es gibt.
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

HERKUNFT = ("Veroeffentlichung", "Abgeleitet", "Platzhalter", "Unbekannt")


# ---------------------------------------------------------------------------------------------
#  Kalender: Feiertage und Sommerzeit
# ---------------------------------------------------------------------------------------------

def ostersonntag(jahr):
    """Ostersonntag nach der Gaußschen Osterformel (gregorianisch, anonyme Fassung)."""
    a = jahr % 19
    b, c = divmod(jahr, 100)
    d, e = divmod(b, 4)
    f = (b + 8) // 25
    g = (b - f + 1) // 3
    h = (19 * a + b - d - g + 15) % 30
    i, k = divmod(c, 4)
    l = (32 + 2 * e + 2 * i - h - k) % 7
    m = (a + 11 * h + 22 * l) // 451
    monat, tag = divmod(h + l - 7 * m + 114, 31)
    return datetime.date(jahr, monat, tag + 1)


def nter_wochentag(jahr, monat, wochentag, n):
    """Der n-te Wochentag (0 = Montag) im Monat; n = -1 ist der letzte."""
    if n > 0:
        d = datetime.date(jahr, monat, 1)
        d += datetime.timedelta(days=(wochentag - d.weekday()) % 7)
        return d + datetime.timedelta(weeks=n - 1)
    folge = datetime.date(jahr + (monat == 12), monat % 12 + 1, 1)
    d = folge - datetime.timedelta(days=1)
    return d - datetime.timedelta(days=(d.weekday() - wochentag) % 7)


def us_beobachtet(d):
    """Der arbeitsfreie Tag eines festen US-Bundesfeiertags: Samstag -> Freitag davor, Sonntag ->
    Montag danach (5 U.S.C. 6103(b); Executive Order 11582)."""
    if d.weekday() == 5:
        return d - datetime.timedelta(days=1)
    if d.weekday() == 6:
        return d + datetime.timedelta(days=1)
    return d


def feiertage_datum(land, jahr):
    """Die Feiertage eines Landes als Daten.

    NO — die gesetzlichen Feiertage Norwegens: Lov om helligdager og helligdagsfred (1995) §2
    (Neujahr, Gründonnerstag, Karfreitag, Ostersonntag, Ostermontag, Christi Himmelfahrt,
    Pfingstsonntag, Pfingstmontag, 1. und 2. Weihnachtstag) und Lov om 1. og 17. mai som
    høgtidsdagar (1947).
    ES — die landesweiten Feiertage Spaniens (fiestas de ámbito nacional, Estatuto de los
    Trabajadores Art. 37.2, jährliche Resolución der Dirección General de Trabajo im BOE):
    Neujahr, Dreikönig, Karfreitag, 1. Mai, 15. August, 12. Oktober, 1. November, 6. und
    8. Dezember, 25. Dezember — ohne die regionalen, weil die Quelle keine Region nennt.
    US — die Bundesfeiertage (5 U.S.C. 6103(a)) mit dem arbeitsfreien Tag nach 6103(b).
    """
    o = ostersonntag(jahr)
    t = datetime.timedelta
    if land == "NO":
        return [datetime.date(jahr, 1, 1), o - t(3), o - t(2), o, o + t(1), datetime.date(jahr, 5, 1),
                datetime.date(jahr, 5, 17), o + t(39), o + t(49), o + t(50),
                datetime.date(jahr, 12, 25), datetime.date(jahr, 12, 26)]
    if land == "ES":
        return [datetime.date(jahr, 1, 1), datetime.date(jahr, 1, 6), o - t(2), datetime.date(jahr, 5, 1),
                datetime.date(jahr, 8, 15), datetime.date(jahr, 10, 12), datetime.date(jahr, 11, 1),
                datetime.date(jahr, 12, 6), datetime.date(jahr, 12, 8), datetime.date(jahr, 12, 25)]
    if land == "US":
        return [us_beobachtet(datetime.date(jahr, 1, 1)), nter_wochentag(jahr, 1, 0, 3),
                nter_wochentag(jahr, 2, 0, 3), nter_wochentag(jahr, 5, 0, -1),
                us_beobachtet(datetime.date(jahr, 7, 4)), nter_wochentag(jahr, 9, 0, 1),
                nter_wochentag(jahr, 10, 0, 2), us_beobachtet(datetime.date(jahr, 11, 11)),
                nter_wochentag(jahr, 11, 3, 4), us_beobachtet(datetime.date(jahr, 12, 25))]
    raise ValueError("Land %s unbekannt" % land)


def jahrestag(datum):
    """Der Jahrestag 1 … 365 im Raster des Kerns (ohne Schalttag); der 29.02. hat keinen (None)."""
    if datum.month == 2 and datum.day == 29:
        return None
    n = datum.timetuple().tm_yday
    if datum.year % 4 == 0 and (datum.year % 100 != 0 or datum.year % 400 == 0) and n > 60:
        n -= 1
    return n


def feiertage(land, jahr):
    """Die Feiertage als sortierte Jahrestage 1 … 365; ein Tag des Vorjahrs fällt weg."""
    return sorted({j for j in (jahrestag(d) for d in feiertage_datum(land, jahr) if d.year == jahr)
                   if j is not None})


def eu_ortszeit(utc):
    """UTC -> mitteleuropäische Ortszeit (MEZ/MESZ): Sommerzeit vom letzten Sonntag im März
    01:00 UTC bis zum letzten Sonntag im Oktober 01:00 UTC (Richtlinie 2000/84/EG)."""
    j = utc.year
    beginn = datetime.datetime.combine(nter_wochentag(j, 3, 6, -1), datetime.time(1))
    ende = datetime.datetime.combine(nter_wochentag(j, 10, 6, -1), datetime.time(1))
    return utc + datetime.timedelta(hours=2 if beginn <= utc < ende else 1)


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
                     zirkulation=True, niveau="Mittel", realisierungen=REALISIERUNGEN, seed=20260926,
                     herkunft="Platzhalter", land=None, region=None):
    """Schreibt die `objekt.json` des Objekts; `land` (NO, ES, US) setzt die Feiertage."""
    if herkunft not in HERKUNFT:
        raise ValueError("Herkunft %s unbekannt" % herkunft)
    inhalt = {
        "kennung": kennung,
        "nutzungsart": nutzungsart,
        "bezugsmenge": bezugsmenge,
        "bezugsmenge_herkunft": herkunft,
        "niveau": niveau,
        "bilanzgrenze": bilanzgrenze,
        "zirkulation": zirkulation,
        "zapftemperatur_c": zapf,
        "kaltwasser_mittel_c": kalt,
        "kaltwasser_amplitude_k": amplitude,
        "kalender": {
            "wochentag_jan1": wochentag_jan1(jahr),
            "feiertagsregion": region or "unbekannt (die Quelle nennt keine Feiertage)",
            "feiertage": feiertage(land, jahr) if land else [],
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
