# KD-8 Teil A — weitere Klimadatenreihen in Vorschau und Jahresverlauf

Sitzung Dialoge und Korrekturen (Klimadaten), 16.10.2026, Zweig `kd8a`. Konzept:
[`2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md`](../../../aktuell/Dialoge/2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md),
Abschnitt 4a, Entscheide KD-Q10 bis KD-Q14 nach Empfehlung; Mockup:
[`../../../aktuell/Mockups/Klimadatenreihen_Mockup.html`](../../../aktuell/Mockups/Klimadatenreihen_Mockup.html).
Kein Schemaschritt, keine Änderung an der Testdatenbank, `ChartRenderer` und `DiagrammSvg` unverändert.

## Stand

| Teil | Inhalt | Nachweis |
|---|---|---|
| Kern | `KlimaReihenkatalog` (Schlüssel und Spalten wie `KlimaExportOptionen.Groessen` auf `kd5`): je Größe Name, Kurzname, Einheit, Gruppe, Monatsregel, Herkunft, Farbrolle; Achsenregel des Monatsbilds (höchstens zwei Einheiten, Strahlung links, dritte Einheit weich gesperrt), Klickregel des Jahresverlaufs (eine Einheit, bis drei Reihen), Rechenspalten einer Region aus `SolardatenCtrl` mit „führt nicht“ bei durchgehend NULL oder 0; `KlimaKennzahlen` mit Monatsreihen je Größe und Stundenzahl; `KlimaVorschaubild.Modell(kennzahlen, auswahl)` mit Vorgabe gleich dem bisherigen Bild und `Jahresverlauf(reihen)` über `JahresgangModell` | `KlimaReihenkatalogTests` (31 Fälle) |
| Baustein | `Reihenwahl`: Chips je Gruppe mit `aria-pressed`, Einheit, Marke „R“, weiche Sperre mit Grund im Titel und in der leisen Zeile, „Vorgabe“; Gruppenname als eigene Zeile über den Chips | `ReihenwahlTests` (6) |
| Vorschau | Reihenwahl unter dem Monatsbild, Vorgabe Globalstrahlung und Temperatur, Stundenzahl bei Teilfüllung im Titel | `KlimadatenDialogTests` (KD8_*, 8) |
| Jahresverlauf | dritter Reiter „Weitere Reihen“ mit Reihenwahl über dem Bild (Kennung `klima-reihen`), „groß…“ und „CSV…“ wie die beiden anderen Reiter | `KlimadatenDialogTests`, `CsvAmBildWacheTests` |
| Texte | `KLIMA_RW_*` beider Sprachen, dazu `KLIMA_RW_NICHT_VERFUEGBAR` und die vollen Reihennamen `KLIMA_RW_NAME_*`; Designer nachgezogen | Designer 0 Abweichungen |

## Nicht verfügbar bis zur Rohwerttabelle

Die Größen, die erst die Rohwerttabelle `Tab_Klimastunde_STAMM` (Schemaschritt 238, Zweig `kd4`) liefert, stehen
benannt als weich gesperrter Chip „Nicht verfügbar: Diese Fassung speichert die Rohwerte der Quelle nicht“ mit Marke
„R“: Wasserdampfgehalt, Direktstrahlung horizontal, Ausstrahlung, Luftdruck, Windgeschwindigkeit — im Reiter „Weitere
Reihen“ immer, in der Vorschau, solange der Parameterbestand die Größe nicht als „fehlt“ führt (dann nennt er sie
selbst). Windrichtung und Qualitätskennung haben kein Bild (KD-Q12) und stehen in keiner Wahl. Die Freischaltung ist
ein Schalter im Kern (`KlimaReihenkatalog.RohwerteFreigeschaltet`), dazu kommt mit Teil B der Leser der Rohwerttabelle.

## Abnahme (lokal, Linux)

- Kern-Filter 0 Fehler; Windows-Schale (`EnableWindowsTargeting=true`) 0 Fehler.
- Kern-Tests gefiltert (`Klima|Reihenwahl|Wache`) 709/709; UI-Tests gefiltert (`Klima|Reihenwahl`) 122/122,
  `CsvAmBildWache|KiMaskenabdeckungWache` 39/39.
- ChartProben: 257 Bilder, 0 Verstöße; jede Zeile der Messlatte `Messlatte_2026-10-14.sha256` (225) unverändert in der
  Hashliste des Laufs.
- Fensterprobe (Chromium headless): `klimaimport` 0 Verstöße in vier Fenstern, Gegenprobe 4 Verstöße je Fenster —
  die Vorschaukarte trägt jetzt die Reihenwahl (335 px bei 1 088 px Breite), Rollhöhe 2 058/560 px; `dubletten`
  0 Verstöße, Gegenprobe rot; neuer Fall `klimareihen` 0 Verstöße in vier Fenstern (15 Chips, 6 weich gesperrt,
  nichts ragt heraus, Bild darunter, Schlussleiste im Fenster), Gegenprobe ohne Umbruch 2 Verstöße je schmalem Fenster.

## Offen

- Im Stammblatt (340–440 px breit) ist die Wahl des Reiters „Weitere Reihen“ mit fünf gesperrten Rohwert-Chips rund
  620 px hoch; das Bild steht darunter im rollenden Stammblatt, breit über „groß…“.
- Teil B (Rohwerte) hängt am Schemaschritt der Rohwerttabelle und dessen Abnahme.
- Export (KD-5) setzt beim Zusammenführen auf den Katalog auf.
