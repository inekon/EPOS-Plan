# Konverter für offen lizenzierte Messreihen

Drei Skripte, die offen lizenzierte Messdaten zum Trinkwarmwasserverbrauch in das Format des
`Messreihenleser` umsetzen, damit
[`Werkzeuge/ZapfprofilValidierung`](../LIESMICH.md) sie rechnen kann (Stufe Z5, offener Punkt K5).

**Quelle und Ziel liegen außerhalb des Repositoriums.** Hier stehen nur die Skripte; die
heruntergeladenen Daten und die umgesetzten Reihen bleiben draußen, ins Repositorium kommt allein der
Bericht mit Verhältniszahlen. Vorgeschlagene Ablage: ein Ordner `Messreihen_extern/` **neben** dem
Repositorium mit den Rohdaten je Quelle und `konvertiert/objekte/` für das Ergebnis.

Die Skripte laufen mit dem Starter `py` (Windows) bzw. `python3` und brauchen **kein Zusatzpaket** —
auch die Excel-Quelle wird mit `zipfile` und `xml.etree` gelesen. Mit `PYTHONIOENCODING=utf-8`
davor, weil sie Unicode ausgeben.

---

## Die Quellen

| Skript | Quelle | Lizenz | Inhalt |
|---|---|---|---|
| `norwegen.py` | Sørensen, Å. L. et al.: *Measurement data on domestic hot water consumption and related energy use in hotels, nursing homes and apartment buildings in Norway.* Mendeley Data V2, doi:10.17632/m3xy22pf4j.2; Beschreibung: Data in Brief 2021, doi:10.1016/j.dib.2021.107228 | CC BY 4.0 | zwölf Gebäude im Raum Oslo (AB Wohngebäude, HO Hotels, NH Pflegeheime), je sechs bis acht Wochen 2018/2019, **stündlich**: `Q_chw` (Zapfenergie), `Q_hwc` (Zirkulationsverluste) |
| `hihaigua.py` | *hihAigua dataset*, Zenodo, doi:10.5281/zenodo.18456405 | CC BY 4.0 | zehn spanische Wohnhäuser, Jahr 2025, Ablesungen etwa alle sieben Minuten; Zähler `hotWater` mit kumuliertem Zählerstand `volume` [m³] |
| `forbell.py` | NREL/OpenEI, *Domestic hot water distribution system losses and demand control* (Forbell), doi:10.25984/2204257 | CC BY 4.0 | zwei Mehrfamilienhäuser in New York mit zentraler Trinkwassererwärmung und Zirkulation, Juni 2013 bis April 2014, **5-Minuten-Werte** je Wochenblatt; genommen wird `QU` (useful delivered energy) |

**Namensnennung nach CC BY 4.0** gehört in jeden Bericht, der diese Daten benutzt.

**Geprüft und nicht verwendet:** Forschungsprojekt *Flexitility — Dezentrale
Trinkwasserzwischenspeicher*, Zenodo doi:10.5281/zenodo.17831069 (CC BY 4.0). Die beiden Dateien
führen den **Gesamt-Trinkwasserdurchfluss am Hausanschluss** (`Durchfluss [l/min]`, Minutenwerte)
eines Einfamilien- und eines Mehrfamilienhauses — **kein getrenntes Warmwasser**. Für einen Vergleich
mit dem Zapfprofilgenerator ist das ungeeignet; ein Konverter dafür gibt es nicht.

---

## Aufruf

```
PYTHONIOENCODING=utf-8 py Werkzeuge/ZapfprofilValidierung/Konverter/norwegen.py \
    --quelle <ordner mit *_2.csv>            --ziel <ordner ausserhalb>/konvertiert/objekte
PYTHONIOENCODING=utf-8 py Werkzeuge/ZapfprofilValidierung/Konverter/hihaigua.py \
    --archiv <ordner>/hihAigua_dataset.zip   --ziel <ordner ausserhalb>/konvertiert/objekte
PYTHONIOENCODING=utf-8 py Werkzeuge/ZapfprofilValidierung/Konverter/forbell.py \
    --quelle <ordner mit den beiden .xlsx>   --ziel <ordner ausserhalb>/konvertiert/objekte
```

Jeder Lauf schreibt je Objekt einen Unterordner `<Kennung>/` mit `messreihe.csv` und `objekt.json`
und protokolliert eine Zeile je Objekt — Stundenzahl, Länge in Tagen, Lückenanteil, Nullsetzungen;
**keine Menge**. Alle drei dürfen in denselben Zielordner schreiben; die Kennungen sind mit `NO-`,
`ES-` und `US-` getrennt.

---

## Die vier Regeln, die alle Konverter teilen

Sie stehen in `gemeinsam.py` und sind dort begründet:

1. **Ein Kalenderjahr.** Der Vergleich hält die Messung gegen einen Kalender aus 365 Tagen mit einem
   festen Wochentag am 1. Januar. Eine Reihe über den Jahreswechsel hätte zwei Zuordnungen; genommen
   wird das Kalenderjahr mit den meisten Werten.
2. **Ein zusammenhängendes Fenster mit höchstens 5 % Lücken** und mindestens 30 Tagen. Fehlende
   Stunden bleiben in der Datei weg — der Leser füllt und zählt sie. Reicht kein Fenster, wird die
   Quelle **benannt übergangen** (mit der Länge des längsten Fensters), nicht stillschweigend
   verkürzt.
3. **Negative Werte werden auf 0 gesetzt und gezählt.** Eine Zapfung zählt nie rückwärts; negative
   Stundenwerte sind Artefakte der Energiebilanz des Zählers (sie kommen in den norwegischen Dateien
   vor). Die Zahl steht im Protokoll und im Vermerk des Objekts.
4. **Bezugsmengen sind Platzhalter**, solange sie nicht aus der Veröffentlichung belegt sind. Sie
   gehen in die √N-Skalierung, nicht in die drei Kriterien je Objekt; nach der Kalibrierung steht der
   Niveaufehler allein im Kalibrierfaktor. Jeder Platzhalter steht als solcher im `vermerk` der
   `objekt.json` — **wer die Zahlen kennt, trägt sie dort nach**. Belegt sind bisher nur AB2
   (56 Wohnungen), HO1 (434 Zimmer) und NH1 (148 Betten).

## Was jeder Konverter für sich entscheidet

* **Norwegen:** Führt die Datei `Q_hwc`, wird sie zu `Q_chw` addiert und die Bilanzgrenze ist
  `MitVerteilung`; fehlt sie, bleibt es bei `Q_chw` und `Zapfstelle` — der Vergleich bildet dieselbe
  Summe. Die erste Spalte trägt in einigen Dateien keinen Namen; sie gilt trotzdem als Zeitstempel.
  Zeitstempelart `Ortszeit` (CET mit Sommerzeit).
* **hihAigua:** Der Zuwachs zwischen zwei Ablesungen fällt der Stunde der späteren Ablesung zu; ein
  Rückwärtssprung des Zählerstands gilt als 0. Die Reihe ist ein **Volumen**; die Energie folgt über
  die Spreizung aus `objekt.json` (die Bezugstemperaturen des Katalogs). Das ist eine Annahme, sie
  wirkt aber auf **beide** Seiten des Vergleichs. Zeitstempelart `Normalzeit` (die Quelle ist UTC).
* **Forbell:** Alle Wochenblätter werden aneinandergesetzt — die Regelstrategie der Woche wirkt auf
  die Verteilverluste, nicht auf die Zapfungen. `QU` ist in **Btu je 5 Minuten**; die Einheit ist
  nicht angeschrieben und folgt aus der Probe gegen den Wasserzähler (Wochensumme `QU` je Gallone
  ergibt eine Spreizung von etwa 55 K und passt zu `TS` ≈ 160 °F, `TC` ≈ 60 °F). Eine Stunde zählt
  nur mit allen zwölf Schritten. `QU` schließt die Verteilverluste `QDL` **nicht** ein, deshalb
  Bilanzgrenze `Zapfstelle` und `zirkulation: false`. Zeitstempelart `Ortszeit` (New York).

## Was die Konverter nicht können

* **Feiertage** nennt keine der Quellen. `kalender.feiertage` bleibt leer; ein echter Feiertag fällt
  damit in den Tagtyp „Werktag" und verbiegt den Formabgleich ein wenig. Wer die Feiertage des Landes
  und Jahres kennt, trägt sie in die `objekt.json` nach.
* **Eine Zone je Objekt.** Ein Hotel mit Restaurant oder ein Wohngebäude mit Gewerbe ist in
  Wirklichkeit eine Mischnutzung; das Werkzeug rechnet eine Zone. Das ist eine Vereinfachung, die im
  Formabgleich sichtbar wird.
