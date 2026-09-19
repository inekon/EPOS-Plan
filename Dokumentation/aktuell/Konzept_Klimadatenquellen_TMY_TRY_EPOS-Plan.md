# Klimadatenquellen: PVGIS-TMY und DWD-Testreferenzjahre

**Gegenstand.** Woher EPOS-Plan die Stundenwerte einer Klimaregion bezieht, was davon
gespeichert wird und was benannt verworfen wird. Das Papier beschreibt den Klimadaten-Import
(`EPOS.Kern/Allgemein/Import/`) und die Rubrik „Klimadaten" in den globalen
Anwendungseinstellungen.

**Abgrenzung.** Die Rechenwege, die auf den Stundenwerten aufsetzen — Transposition auf
geneigte Flächen, PV-Ertrag, Gebäudesimulation —, stehen in
[`Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`](Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md)
und [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md).
Ablage und Sicherung der Datenbank: [`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md).

---

## 1. Die drei Quellen

| Quelle | Deckung | Netz | Was der Anwender angibt |
|---|---|---|---|
| **PVGIS-TMY** | weltweit | ein Abruf der PVGIS-Schnittstelle | Ortsname (Geokodierung) oder Longitude/Latitude und Bezeichnung |
| **DWD-TRY-Datei** (`.dat`) | Deutschland, Datei vom Rechner | keines | Pfad der Datei **und** Standort |
| **TRY-Regionaldaten** (`data.zip`) | Deutschland, 15 Regionen | Bereichsabrufe; bei lokaler Datei keines | Standort, Jahr, Szenario; wahlweise Pfad einer lokalen `data.zip` |

Eine Klimaregion trägt das, was bei ihrem Import geholt wurde; die Quelle steht im
Herkunftsvermerk (`Tab_Klimaregion_STAMM.Details`). Regionen unterschiedlicher Herkunft
stehen nebeneinander, ohne einander zu beeinflussen.

**Warum eine TRY-Datei zusätzlich den Standort braucht:** Ihr Kopf führt nur
Lambert-Koordinaten. Longitude und Latitude sind aber für die Sonnengeometrie und für die
Umrechnung auf Direkt-Normal nötig — bei den Regionaldaten zusätzlich für die Wahl der
nächsten Region. Die Umrechnung Lambert → WGS 84 gibt es nicht; den Standort gibt der
Anwender an.

## 2. Bedienweg

Im Dialog **Klimadaten** steht über der Gruppe „Standort" die Gruppe **Klimaquelle**:

1. **Quelle wählen** — PVGIS (vorgewählt), TRY-Datei oder TRY-Regionaldaten.
2. **Standort** — Ortsname oder Longitude/Latitude mit Bezeichnung, wie bei PVGIS.
3. **Bei TRY-Datei** — die Datei über den Dateiwähler (`*.dat`).
4. **Bei Regionaldaten** — Jahr (2015 oder 2045) und Szenario (mittleres Jahr,
   sommerwarm, winterkalt); Vorgabe **2015, mittleres Jahr**. Ohne Dateipfad läuft der
   Bereichsabruf über die hinterlegte Adresse; mit Pfad wird die lokale `data.zip` gelesen.
5. **Daten einlesen** — die Region entsteht mit 8 760 Stundenwerten und 365 Tageswerten;
   die Meldung nennt die Quelle, bei den Regionaldaten zusätzlich Region, Koordinate,
   Entfernung, Szenario und Jahr.

Der Lauf ist abbrechbar. Eine Region desselben Namens wird benannt abgelehnt.

## 3. Das TRY-Format und seine Zuordnung

Eine DWD-TRY-Datei besteht aus einem freien Kopf (34 Zeilen bei den Testreferenzjahren
2015, 36 bei den Projektionen 2045) bis zu der Zeile, die getrimmt mit `***` beginnt, und
danach aus genau **8 760 Datenzeilen mit 17 weißraumgetrennten Feldern**:

```
RW HW MM DD HH t p WR WG N x RF B D A E IL
```

| Feld | Bedeutung | Ziel |
|---|---|---|
| `RW`, `HW` | Lambert-Koordinaten der Station | nicht übernommen — die Region trägt Longitude/Latitude |
| `MM`, `DD`, `HH` | Monat, Tag, Stunde **1…24 MEZ**; `HH` benennt das Intervall, das zu `HH:00` endet | Reihenfolge der Zeile (Abschnitt 4) |
| `t` | Lufttemperatur [°C] | `Tab_Solar_STAMM.Temperatur` |
| `B` | Direktstrahlung **horizontal** [W/m²] | `Direktstrahlung`, nach der Umrechnung aus Abschnitt 5 |
| `D` | Diffusstrahlung horizontal [W/m²] | `Diffusstrahlung` |
| `B + D` | Globalstrahlung horizontal | `Globalstrahlung` |
| `p WR WG N x RF A E IL` | Druck, Windrichtung, Windgeschwindigkeit, Bedeckung, Wasserdampfgehalt, relative Feuchte, atmosphärische Gegenstrahlung, langwellige Ausstrahlung, Qualitätsbit | **benannt verworfen** (Abschnitt 6) |

Aus Stundenwerten und Sonnengeometrie entstehen anschließend — über dieselben Methoden wie
bei PVGIS — die vier Fassadenwerte `Sol_Nord/Ost/Sued/West`, der `Sonnenwinkel` und daraus
die 365 Tageswerte in `Tab_Klimadaten_STAMM` samt `WE`, `TagTyp_W` und `TagTyp_NW`. **Der
Weg hinter dem Leser ist derselbe wie bei PVGIS**; die TRY-Quellen treten nur an die Stelle
des Netzabrufs.

Ein Kopf ohne Trennzeile, eine falsche Feld- oder Zeilenzahl, ein unlesbares Zeit- oder
Zahlenfeld und eine doppelt belegte Stunde führen jeweils zu einer benannten Meldung mit der
Zeilennummer — und zu keiner Region.

## 4. Zeitbasis

Die TRY-Datei läuft in **Ortszeit ohne Sommerzeit**. Der Ortszeitindex einer Zeile ist

```
L = (TagImJahr − 1) · 24 + (HH − 1)          im 365-Tage-Raster, kein Schaltjahr
```

`Tab_Solar_STAMM` führt die Reihe dagegen in **UTC-Reihenfolge** — das ist der Vertrag, den
`SolarZeitbasis` beim Lesen wieder auflöst. TRY kennt keine MESZ, der Versatz ist deshalb
ganzjährig eine Stunde; der Leser dreht die Reihe:

```
utc[u] = mez[(u + 1) % 8760]
```

Der Jahresumlauf ist gewollt: Die erste MEZ-Stunde des 1. Januar ist die letzte UTC-Stunde
des Jahres. Der Zeitstempel entsteht aus dem UTC-Index im Format `yyyyMMdd:HHmm` mit dem
Referenzjahr des Hauses.

**Was daraus folgt, ausdrücklich festgehalten.** `SolarZeitbasis` holt im **Winter**
`mez[L]` zurück — genau die Drehung —, im **Sommer** `mez[L − 1]`, also eine Stunde zu früh:
Das Haus liest die Reihe als UTC und wendet die EU-Sommerzeitregel an, die TRY selbst nicht
kennt. Eine TRY-Reihe beim Import „mitzudrehen" hieße, sie gegen die übrigen Regionen
unterschiedlich zu behandeln. Ein quellenbewusstes Lesen setzt eine Quellenkennung an der
Region voraus und ist damit ein Schemaschritt (Abschnitt 12).

## 5. Direkt-horizontal → Direkt-normal

TRY führt die Direktstrahlung **horizontal**, `Tab_Solar_STAMM` und `SolarCalculator`
erwarten sie **normal** (`direct = dni · cosθ`). Der Import rechnet um, mit zwei Regeln:

```
alpha < 5°  ->  Direct = 0, Diffuse += B
sonst       ->  Direct = min(B / sin alpha, 1367)
```

- **Die Mindesthöhe 5°:** Darunter geht `sin alpha` gegen null, und die Division machte aus
  wenigen W/m² Messrauschen einen vierstelligen Wert. Der Betrag geht nicht verloren,
  sondern in den Diffusanteil — so bleibt die **Globalstrahlung `B + D` in jeder Stunde
  unverändert**, und genau sie geht in die Bilanz ein.
- **Die Klemme auf die Solarkonstante 1367 W/m²:** Mehr als die extraterrestrische
  Normalstrahlung kann am Boden nicht ankommen; ein höherer Wert wäre ein Rechenartefakt
  kleiner Sonnenhöhen.

Die Sonnenhöhe liefert `SolarCalculator.Sonnenhoehe` aus Longitude, Latitude, Tag im Jahr
und Stunde — dieselbe Geometrie, die auch die Fassadenwerte rechnet. Nach der Umrechnung
stimmen die vier Fassadenwerte einer TRY-Region mit denen einer PVGIS-Region überein.

## 6. Das benannte Verwerfen

`Tab_Solar_STAMM` hat keine Spalten für Druck, Wind, Bedeckung, Feuchte, Gegenstrahlung und
Qualitätsbit. Diese neun Größen werden **nicht still übergangen**: Sie stehen im
Herkunftsvermerk der Region (`Tab_Klimaregion_STAMM.Details`, „nicht übernommen: p, WR, WG,
N, x, RF, A, E, IL") und in der Meldung nach dem Import — damit niemand sie später in der
Datenbank sucht.

Ein Schemaschritt, der `N` (Bedeckungsgrad), `A` (atmosphärische Gegenstrahlung) und `E`
(langwellige Ausstrahlung) aufnimmt, ist Kandidat für die Gebäudesimulation: Sie braucht
Bedeckungsgrad und langwelligen Austausch heute als Schätzung aus dem Diffusanteil. Wind und
Feuchte lägen bei derselben Gelegenheit mit.

## 7. Regionaldaten: Auswahl, Bereichsabruf, Rückfall

Das offene Regionalpaket (`data.zip`) ist rund 892 MB groß; **heruntergeladen wird es nie**.
Gebraucht werden zwei Dinge: das Zentralverzeichnis des ZIP am Dateiende und **eine**
`.dat`-Datei von rund 155 KB.

- **Das Zentralverzeichnis ist die Regionsliste.** Im Code steht keine Tabelle der
  15 Regionen. Die Einträge heißen
  `1_raw-data/<1…15>/TRY<2015|2045>_<12 Ziffern>_<Jahr|Somm|Wint>.dat`; die zwölf Ziffern
  sind `lat · 1e4` (sechs Stellen) und `lon · 1e4` (sechs Stellen) —
  `TRY2015_535591085872_Jahr.dat` also 53,5591 / 8,5872.
- **Gewählt wird die Region mit der kleinsten Entfernung** (Haversine) zum angegebenen
  Standort. Das ist eine Näherung: Maßgeblich ist der Abstand zum Regionsmittelpunkt, nicht
  eine amtliche Regionsgrenze.
- **Grenze 300 km.** Liegt auch die nächste Region weiter weg, bricht der Lauf mit
  Entfernungsangabe ab — das Paket deckt nur Deutschland ab.
- **Bereichsabruf.** Ein Strom über der Adresse holt Blöcke von 256 KiB und bedient alle
  Lesewünsche daraus; die Gesamtlänge kommt aus `Content-Range`. Fehlt sie oder liefert die
  Adresse mehr als den angeforderten Block, wird das **benannt abgelehnt** („Die Adresse
  erlaubt keine Teilabrufe; data.zip herunterladen und als Datei wählen.") — ein stiller
  Volldownload wäre die schlechtere Überraschung.
- **Rückfall.** Eine lokal abgelegte `data.zip` wird ohne Netz gelesen; derselbe Weg macht
  den Prüfstand netzfrei.

## 8. Lizenz und Herkunftsvermerk

Jede TRY-Region bekommt ihren Herkunftsvermerk in `Tab_Klimaregion_STAMM.Details` und
denselben Text in der Importmeldung:

- **TRY-Datei:** Dateiname, Importdatum, Lizenzvermerk, verworfene Größen.
- **Regionaldaten:** Regionsnummer, Koordinate, Entfernung, Szenario, Jahr, Quelle (Adresse
  oder Dateiname), Importdatum, Lizenzvermerk, verworfene Größen.

Der Lizenzvermerk lautet **„CC BY 4.0, RE-Lab-Projects/TRY_DE_2015_2045, Rohdaten Deutscher
Wetterdienst"**. Die Datei aus dem DWD-Klimaberatungsmodul bezieht der Anwender selbst; sie
liegt nie im Repository und nie in der Auslieferung.

## 9. Einstellungen und Werksvorgaben

Die globalen Anwendungseinstellungen führen fünf Rubriken: **VDI | DATENBANK | WEB | KLIMA |
ANWENDUNG**. Die Rubrik **Klimadaten** trägt die drei Adressen der Klimaquellen; die
Web-Rubrik behält Wiki und Geokodierung.

| Schlüssel | Bedeutung | Werksvorgabe |
|---|---|---|
| `PVGISUrl` | PVGIS-TMY-Schnittstelle | `https://re.jrc.ec.europa.eu/api/tmy` |
| `TRYPortalUrl` | Portal der DWD-Testreferenzjahre (Konto nötig, kein REST-API) | `https://kunden.dwd.de/obt/` |
| `TRYRegionalUrl` | `data.zip` der offenen Regionaldaten | Veröffentlichungsadresse des Pakets `RE-Lab-Projects/TRY_DE_2015_2045` |

Die Adressen sind Textfelder ohne Durchsuchen-Knopf; „Standardwerte" setzt sie auf die
Werksvorgaben, übernommen wird erst mit „Speichern". Eine Installation, deren `user.config`
die zwei TRY-Schlüssel nicht kennt, bekommt die Werksvorgabe.

## 10. Entscheide

| Kennung | Inhalt |
|---|---|
| **E5** (Anwender, fortgeschrieben 19.09.2026) | Die Datenträger der Richtlinien bleiben abgelehnt; **DWD-TRY ist zweite Importquelle** neben PVGIS-TMY. Klimabasis einer Region ist das, was bei ihrem Import geholt wurde. |
| **KL-Q2** | Beide TRY-Wege: die eigene Datei aus dem DWD-Klimaberatungsmodul **und** die offenen Regionaldaten. |
| **KL-Q3** | Kein Schemaschritt. Temperatur, Global/Direkt/Diffus und die Flächenwerte werden übernommen; `p WR WG N x RF A E IL` werden benannt verworfen — Kandidat eines späteren Schemaschritts zusammen mit dem Bedeckungsgrad der Gebäudesimulation. |
| **KL-Q4** | Jahr und Szenario sind wählbar, Vorgabe 2015, mittleres Jahr. |
| **KL-Q5** | Eigene Wiki-Seite „Programm Dokumentation/Klimadaten". |

## 11. Nachweise

- `EPOS.Kern.Tests/DwdTryLeserTests` — 18 Tests: Kopferkennung, Feld- und Zeilenzahl,
  Zeitfelder, doppelte Stunde, die Drehung MEZ → UTC, Zeitstempel, Direkt-Normal mit
  Mindesthöhe und Klemme, Erhalt der Globalstrahlung.
- `EPOS.Kern.Tests/TryPaketLeserTests` — 17 Tests, darunter der Bereichsabruf gegen ein
  Paket im Speicher: **528 402 Byte in drei Abrufen bei einem 8,9-MB-Paket**; fehlender
  Bereichsabruf, fehlendes Szenario, Ort außerhalb der 300-km-Grenze.
- `KatalogpflegeTests` (+5) und `KlimadatenDialogTests` (+8) — Quellenwahl, Pflichtangaben
  je Quelle, Herkunftsvermerk.
- `EinstellungenDialogTests` — 31 Tests, beide Kulturen.
- Importprobe `Referenzlaeufe/Importproben/dwd_try_synthetisch_72h.dat` — eine
  **synthetische** Probe im DWD-Format, ausdrücklich keine amtlichen Daten; der Leser hat für
  sie die Prüfoption „volles Jahr aus".
- Referenzlauf der fünf Projekte byte-gleich gegen die geltende Basis; der PVGIS-Weg bleibt
  unverändert.

## 12. Offene Punkte

1. **Keine Regionsanzeige vor dem Import** — welche Region der Standort trifft, sagt erst
   die Meldung danach; eine Vorschau kostete einen zweiten Netzabruf.
2. **Lambert → WGS 84 ist nicht umgesetzt** — der Standort einer TRY-Datei kommt vom
   Anwender, nicht aus dem Dateikopf.
3. **Sommerstunden einer TRY-Reihe liegen beim Lesen eine Stunde früher** (Abschnitt 4).
   Ein quellenbewusstes Lesen braucht eine Quellenkennung an der Region und damit einen
   Schemaschritt.
4. **`N`, `A`, `E` werden nicht gespeichert** (Abschnitt 6) — der Schemaschritt dafür gehört
   mit dem Bedeckungsgrad der Gebäudesimulation zusammen.
