# Klimadatenimport: Vorschau vor dem Import, Vollimport aller Parameter, CSV-Export

**Gegenstand.** Befund und Konzept zum Anwenderbefund vom 16.10.2026 (Windows, Administration →
Klimadaten → Import). Eingabe „stuttgart-vaihingen“, Quelle TRY-Regionaldaten, Regionalpaket
leer (Abruf über die Adresse), Prognose 2045, sommerwarm. „Region ermitteln“ meldete „Region 14
Stötten, Station 48,6536 / 9,8666, Entfernung 56 km · vorhanden: 2015 mittleres Jahr, …, 2045
winterkalt“. Longitude, Latitude und Bezeichnung blieben leer, ob ein Datensatz gefunden wurde,
sagte der Dialog nicht, und die Schlussleiste war am Fensterrand abgeschnitten.

**Anwenderwünsche.** (1) Prüfen, ob die Ermittlung stimmt. (2) Koordinaten und Daten des
gefundenen Datensatzes **vor** dem Import im Dialog zeigen; übernommen wird erst mit „Import“.
(3) Alle Parameter aller Quellen, die der Import kennt, lesen und speichern; CSV-Export mit Wahl
der Daten und des Datenformats, angelehnt an die vorhandenen CSV-Wege.

**Abgrenzung.** Quellen, Formate, Zeitbasis und Herkunftsvermerk beschreibt
[`Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md`](../Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md).
Dieses Papier ergänzt es. Was umgesetzt ist, wird dort nachgezogen (Schritt KD-7); danach wandert
dieses Papier nach `ueberholt/`. Mockup:
`Dokumentation/aktuell/Mockups/Klimadatenimport_Mockup.html`.

---

## 1. Befund

### 1.1 Wie die Region heute bestimmt wird

| Schritt | Code | Was geschieht |
|---|---|---|
| Geokodierung | `KlimaImportAblauf.RegionErmittelnAsync` → `IOrtsQuelle` (Nominatim, `Settings`: `https://nominatim.openstreetmap.org`) | Der Ortsname wird zu Länge/Breite. Der Anzeigename der Antwort wird verworfen. |
| Regionsliste | `TryPaketLeser.Zentralverzeichnis` | 90 Einträge `1_raw-data/<1…15>/TRY<2015\|2045>_<lat·1e4><lon·1e4>_<Jahr\|Somm\|Wint>.dat` des offenen Pakets `RE-Lab-Projects/TRY_DE_2015_2045` v1.4.0 (`data.zip`, rund 892 MB, Bereichsabruf). |
| Regionswahl | `TryPaketLeser.Regionswahl` | Gewählt wird der Eintrag mit der kleinsten Großkreisentfernung (Haversine, Erdradius 6 371 km) unter den Einträgen des **Bezugsjahres**. Das Szenario spielt keine Rolle, die Grenze liegt bei 300 km. |
| Anzeige | `KlimaImportAblauf.Vorschauzeile`, `TryRegionsnamen` | Der Text nennt Regionsnummer, Stationsname, die **Stationskoordinate** und die Entfernung, dazu alle Ausführungen der Region. |

Ein Polygon der TRY-Klimaregion wird nicht benutzt, ein 1-km-Raster ebenso wenig. Die 15 Punkte sind
die Koordinaten der Repräsentanzstationen aus den Dateinamen. Das Konzept spricht dabei
missverständlich von Regionsmittelpunkten.

### 1.2 Nachrechnung

Entfernungen nach Haversine mit R = 6 371 km zu den 15 Punkten aus `LambertKoordinatenTests`:

| Standort | Länge/Breite | nächste Punkte |
|---|---|---|
| Stuttgart-Vaihingen | 48,73 N / 9,10 O | **14 Stötten 56,9 km** · 12 Mannheim 96,4 km · 15 Garmisch-Partenkirchen 202,5 km · 13 Mühldorf/Inn 258,4 km |
| Stuttgart-Mitte | 48,78 N / 9,18 O | 14 Stötten 52,2 km · 12 Mannheim 94,8 km |
| Gegenprobe Vaihingen an der Enz | 48,93 N / 8,96 O | **12 Mannheim 71,5 km** · 14 Stötten 73,6 km |

Der Dialog nannte 56 km, die Nachrechnung ergibt 56,3 bis 56,9 km. Damit hat die Geokodierung
**Stuttgart-Vaihingen** getroffen und nicht Vaihingen an der Enz, das zu Region 12 gefallen wäre.
Nach der eigenen Regel rechnet die Ermittlung also **richtig**. Für alle Orte im Großraum Stuttgart
liegt Stötten immer am nächsten: Mannheim ist rund 40 km weiter entfernt, und Mühldorf als Station
der Region 13 liegt 250 km im Osten.

### 1.3 Bewertung gegen die Quelle: Die Ermittlung stimmt **nicht** im Sinne des DWD

1. **Die Region ist eine Fläche und keine nächste Station.** Die 15 TRY-Klimaregionen der
   Fassung 2011 sind Klimaräume mit Höhenstufen. Region 14 „Schwäbische Alb und Baar“
   (Station Stötten, 734 m) umfasst die Hochlagen der Alb. Die Station steht auf der Albhochfläche
   und ist für das Neckarbecken nicht repräsentativ. Stuttgart (rund 250 bis 450 m, Vaihingen auf
   den Fildern rund 450 m) gehört nach der Regionenkarte zu einer Tieflandregion: Region 12
   „Oberrheingraben und unteres Neckartal“ (Mannheim) oder Region 13 „Schwäbisch-fränkisches
   Stufenland und Alpenvorland“ (Mühldorf/Inn). **Die genaue Zuordnung ließ sich hier nicht
   belegen**, weil `dwd.de` aus der Arbeitsumgebung nicht erreichbar war. Sie bleibt deshalb
   Prüfpunkt KD-Q1 (Abschnitt 9).
2. **Der Höhenunterschied wirkt.** Zwischen 734 m und rund 450 m liegen 284 m. Mit dem
   Standardgradienten von 0,65 K je 100 m ergibt das ein um **rund 1,8 K kälteres Jahresmittel**.
   Gradtagzahl und Heizwärmebedarf eines Stuttgarter Projekts fallen damit deutlich zu hoch aus,
   und der Kühlbedarf fällt zu niedrig aus.
3. **Die Bezugsjahre 2015 und 2045 kennen keine Regionen mehr.** Für die Testreferenzjahre 2017
   (Bezugsjahre 2015 und 2045, Projekt „TRY-Weiterentwicklung“ der Forschungsinitiative Zukunft Bau
   des BBSR) hat der DWD vom Regionsmodell auf **ortsgenaue Datensätze im 1-km-Raster** umgestellt.
   Höhenlage und Stadteffekt sind eingerechnet. Abgegeben werden sie je Standort über das
   Klimaberatungsmodul des DWD. Die Rohdaten liegen im Climate Data Center unter
   `grids_germany/…/Project_TRY/` (1 km × 1 km, ETRS89/LCC). Das offene Paket von RE-Lab führt
   dagegen **nur die 15 Punkte der Repräsentanzstationen** (90 Dateien: 15 Regionen × 3 Szenarien
   × 2 Bezugsjahre, Lizenz CC BY 4.0) und ordnet nach seinem eigenen Text jeder Postleitzahl einen
   repräsentativen Datensatz über die Regionen zu.

**Ergebnis:** Rechnerisch stimmt der Lauf (56 km, Stötten). Fachlich stimmt die Ermittlung nicht.
Die nächste Station ist keine Regionszuordnung, und für 2015/2045 ist der ortsgenaue Datensatz der
richtige. **Richtig wäre** für Stuttgart-Vaihingen die TRY-Datei des DWD für den Rasterpunkt des
Standorts, eingelesen über die Quelle „TRY-Datei“. Wer keine Datei hat, braucht im Regionalweg
einen ehrlichen Hinweis: „nächste Repräsentanzstation, nicht die Klimaregion; Höhe 734 m gegen
Standort 450 m“. Empfehlung in Abschnitt 9, KD-Q1.

### 1.4 Warum der Dialog nicht sagt, ob ein Datensatz gefunden wurde

- `KlimaVorschauErgebnis` trägt `Region` und `Ausfuehrungen`, aber keine Antwort auf die Frage
  „gibt es die **gewählte** Ausführung (2045 · sommerwarm)?“. `Vorschauzeile` zählt alle
  Ausführungen auf, und der Dialog zeigt nur diesen Text (`_regionszeile`).
- Geprüft wird das Szenario erst im Import (`TryPaketLeser.AusStrom`, Meldung
  `KLIMA_TRY_KEIN_SZENARIO`). Die Vorschau nimmt **keinen Eintrag** aus dem Paket (Auftrag KL-3)
  und kann deshalb weder Kennzahlen noch Stationshöhe (`TryKopf.Hoehe`) noch Parameterbestand
  zeigen.

### 1.5 Warum Longitude, Latitude und Bezeichnung leer bleiben

- `RegionErmittelnAsync` geokodiert in **lokale** Variablen `lon`/`lat` und gibt sie nicht zurück.
  Den Anzeigenamen der Nominatim-Antwort verwirft sie. Der Dialog bekommt nur die Zeile.
- Der Dialog führt Ortsname und Koordinaten als **Alternativen** (Zwischenüberschrift „oder“,
  `KlimadatenDialog.razor`). Der Import geokodiert den Ortsnamen ein zweites Mal, und die
  Bezeichnung entsteht erst im Import aus dem Ortsnamen.
- Folge: Der Anwender sieht nicht, **welchen Ort** die Geokodierung getroffen hat. Er sieht nur die
  Stationskoordinate, und die liegt 56 km entfernt.

### 1.6 Die abgeschnittene Schlussleiste

- Der Import steht als `Ueberlagerung` im Katalogdialog. Ihr Inhalt ist `div.epos-einlesen` und
  kein `.epos-dialog`. Ihre Knopfleiste (Abbrechen · Daten einlesen) hat **keine Statusspanne**.
- Die Haftregel `.epos-ueberlagerung-inhalt > .epos-dialog > .epos-leiste:has(> .epos-status)`
  (`epos-ui.css`, #572) greift deshalb nicht. Die Überlagerung rollt bei `max-height: 90vh`, und
  die Leiste liegt am Ende des Rollbereichs, also außerhalb des Bildes.
- Das Fenster ist fest 1 180 × 780 groß (`WindowsFormsApplication1/Views/Admin/KlimadatenFenster.cs`,
  `MASS`) und nimmt `Fenstermass.MitUeberlagerung` nicht. Bei 125 bis 150 % Anzeigeskalierung auf
  einem Bildschirm mit 768 bis 1 080 Pixeln Höhe reicht die Höhe nicht.
- Die Regel „Dialog im eigenen Fenster“ (`EPOS.UI/CLAUDE.md`) nimmt Katalogdialoge und
  Überlagerungen ausdrücklich aus. Zu beheben ist also die **Haftregel der Überlagerung** und nicht
  die Fensterregel. Nachweis: `Proben/Rasterprobe/fensterprobe.mjs` bekommt einen Fall
  „Import-Überlagerung im Katalogdialog“ mit Gegenprobe, dazu ein Fall in `FensterrahmenTests`.
- **Umgesetzt (KD-1):** Die Haftregel gilt zusätzlich für
  `.epos-ueberlagerung-inhalt > .epos-einlesen > .epos-leiste:last-child` — erkannt an der Stellung,
  nicht an Statusspanne oder Primärknopf. Sie erfasst damit alle sechs Einlese-Überlagerungen
  (Klimadaten, Ganglinienkatalog, Wärmebedarf, Stromganglinie, Nutzungsarten, Lastspitzenkappung;
  die letzte trägt nur „Abbrechen“). Das Klimadatenfenster wünscht über
  `KlimadatenHuelle.Wunschmass()` mindestens das Maß der Überlagerung (900 × 870, Ergebnis
  1 180 × 870). Nachweis: Fall `klimaimport` der `fensterprobe.mjs` (vier Fenster, mit Gegenprobe),
  `FensterrahmenTests`, `FenstermassTests`.

---

## 2. Inventar: Quellen, Parameter, Speicherung

**Bekannte Quellen** (`KlimaQuelle`): `PvgisTmy` (PVGIS-API, JSON, `tmy`), `TryDatei` (DWD-TRY
`.dat`, 17 Felder je Stunde) und `TryRegional` (dieselbe `.dat` aus `data.zip`). EPW, Meteonorm und
eine freie CSV kennt der Import **nicht**.

**Schema der Testdatenbank** (gelesen aus `Referenzlaeufe/Kenndaten_Test.sqlite`):
- `Tab_Klimaregion_STAMM` (32 Zeilen): `ID_Klimaregion, Name, Longitude, Latitude, Details,
  ReadOnly, Quelle, Importdatum, Szenario, Bezugsjahr`.
- `Tab_Klimaregion` (50 Zeilen): dieselben Angaben je Projekt, dazu `Klimazone_DIN4710`.
- `Tab_Solar_STAMM` (280 320 Zeilen = 32 × 8 760) und `Tab_Solar` (438 000 Zeilen): `Temperatur,
  Sol_Nord/Ost/Sued/West, Globalstrahlung, Direktstrahlung, Diffusstrahlung, Sonnenwinkel,
  Gegenstrahlung, Luftfeuchte, Bedeckungsgrad`.
- `Tab_Klimadaten(_STAMM)`: 365 Tageswerte aus Temperatur, Strahlung, `WE`, `TagTyp_W/NW` und
  `Sonnenwinkel`.

| Parameter | Einheit | PVGIS-TMY | TRY | heute gelesen | heute gespeichert | Leser im Rechenweg | Ziel |
|---|---|---|---|---|---|---|---|
| Lufttemperatur | °C | `T2m` | `t` | ja | `Tab_Solar(_STAMM).Temperatur`, Tagesmittel in `Tab_Klimadaten` | Gebäude, Wärmepumpe, Erdreich, Kälte, Tageswerte | unverändert, zusätzlich roh |
| relative Feuchte | % | `RH` | `RF` | ja | `Luftfeuchte` | Gebäude (VDI 6007), Rückkühlwerk und Kältemaschine | unverändert, zusätzlich roh |
| Globalstrahlung horizontal | W/m² | `G(h)` | `B + D` | ja | `Globalstrahlung` | PV, Solarthermie, Gebäude | unverändert, zusätzlich roh |
| Direktstrahlung normal | W/m² | `Gb(n)` | aus `B` gerechnet (Konzept Klimadatenquellen § 5) | ja | `Direktstrahlung` | PV, Solarthermie, Fassadenwerte | unverändert |
| Direktstrahlung horizontal | W/m² | – | `B` | ja, umgerechnet | nicht roh | – | **roh speichern** |
| Diffusstrahlung horizontal | W/m² | `Gd(h)` | `D` | ja | `Diffusstrahlung` (TRY: + `B` bei Sonnenhöhe < 5°) | PV, Solarthermie, Gebäude | unverändert, zusätzlich roh |
| atmosphärische Gegenstrahlung | W/m² | `IR(h)` | `A` | ja | `Gegenstrahlung` | Gebäude (VDI 6007) | unverändert, zusätzlich roh |
| Bedeckungsgrad | Achtel | – | `N` | ja | `Bedeckungsgrad` | Zapfprofil (Tagesmittel) | unverändert, zusätzlich roh |
| Luftdruck | hPa | `SP` [Pa] | `p` | nein | – | – | **roh (hPa)** |
| Windgeschwindigkeit | m/s | `WS10m` | `WG` | PVGIS: ja (`WindSpeed`, verworfen); TRY: auf 0 gesetzt | – | – | **roh** |
| Windrichtung | ° | `WD10m` | `WR` | nein | – | – | **roh** |
| Wasserdampfgehalt | g/kg | – | `x` | nein | – | – | **roh** |
| terrestrische Ausstrahlung | W/m² | – | `E` | nein | – | – | **roh** |
| Qualitätskennung Strahlung | – | – | `IL` | nein | – | – | **roh** |
| Taupunkt | °C | – | – | – | – | – | **nur Export**, abgeleitet aus t und RF (Magnus), nie gespeichert |
| Fassadenwerte, Sonnenwinkel | W/m², ° | gerechnet | gerechnet | – | `Sol_*`, `Sonnenwinkel` | Gebäude, Altweg | unverändert |
| Höhe des Bezugspunkts | m | `inputs.location.elevation` | Kopf „Höhenlage“ (`TryKopf.Hoehe`) | TRY: Text | nur im Freitext `Details` | – | **Spalte an der Region** |
| Koordinate des Bezugspunkts | ° | `inputs.location` | Kopf Rechts-/Hochwert, Dateiname | ja | `Longitude/Latitude` (Standort **oder** Station, je Weg) | Sonnengeometrie | **getrennt: Standort und Bezugspunkt** |
| Monatsjahre des TMY | – | `outputs.months_selected` | – | nein | – | – | Herkunftsvermerk |

**Schreibstellen mit fester Spaltenliste**, die ein neuer Parameter berührt:
`SolarPVGISCalculator.SaveTmyData` (Insert in `Tab_Solar_STAMM`), `KlimaregionStammCtrl`
(Projektkopie `Tab_Solar_STAMM` → `Tab_Solar`, Insert mit fester Liste) und `KatalogRegistry`
(Eintrag `KLIMAREGION`, Datenblöcke für die Katalogfassung). `SolardatenCtrl` liest mit `SELECT *`.

---

## 3. Datenhaltung

**Grundsatz:** Der Rechenweg liest weiter `Tab_Solar` und `Tab_Klimadaten`. Er bekommt keine neue
Spalte, damit Referenzlauf und Basis unberührt bleiben. Damit gilt auch die Hausregel „keine Spalte
ohne Leser“ (Umsetzungskonzept Gebäudesimulation F-S3, Klimakonzept KL-Q3) weiter **für die
Rechentabellen**. Die Rohwerte bekommen eine **eigene Tabelle**, deren Leser der Export ist.

**Ein Schemaschritt** (`KlimaRohwerteSchema`). Er legt die Rohtabelle leer an und ergänzt die
Regionsspalten; er schreibt keine Zeile um und lädt nichts nach.

| Tabelle | Spalte | Typ | Bedeutung |
|---|---|---|---|
| `Tab_Klimastunde_STAMM` (neu, `STRICT`) | `ID_Klimaregion` | INTEGER, FK → `Tab_Klimaregion_STAMM`, `ON DELETE CASCADE` | Region |
| | `Stunde` | INTEGER `CHECK (Stunde BETWEEN 1 AND 8760)` | Stunde im Raster der Quelle, Primärschlüssel mit `ID_Klimaregion` |
| | `Temperatur_C`, `Luftdruck_hPa`, `Windrichtung_Grad`, `Windgeschwindigkeit_m_s`, `Bedeckungsgrad_Achtel`, `Wasserdampfgehalt_g_kg`, `Luftfeuchte_Prozent`, `Direkt_Horizontal_W_m2`, `Direkt_Normal_W_m2`, `Diffus_Horizontal_W_m2`, `Global_Horizontal_W_m2`, `Gegenstrahlung_W_m2`, `Ausstrahlung_W_m2` | REAL, **alle nullbar** | Rohwert der Quelle in Hauseinheit, Einheit im Namen. NULL heißt „Quelle führt die Größe nicht“, nie 0. Prüfklauseln nur, wo sie sicher sind: Luftdruck > 0, Windrichtung 0 bis 360, Windgeschwindigkeit, Wasserdampfgehalt und Feuchte ≥ 0 |
| | `Qualitaet_Strahlung` | INTEGER, nullbar | TRY `IL` |
| `Tab_Klimaregion(_STAMM)` | `Standort_Hoehe_m` | REAL, nullbar | Höhe des Standorts, soweit bekannt (Eingabe, PVGIS, TRY-Kopf als Standort) |
| | `Bezugspunkt_Art` | TEXT `CHECK IN ('station','datei','pvgis')`, nullbar | Repräsentanzstation, Dateistandort oder PVGIS-Punkt |
| | `Bezugspunkt_Name`, `Bezugspunkt_Latitude`, `Bezugspunkt_Longitude`, `Bezugspunkt_Hoehe_m`, `Entfernung_km` | TEXT bzw. REAL, nullbar | Station, Rasterpunkt oder PVGIS-Punkt; `Longitude/Latitude` bleiben der Rechenstandort |
| | `Ausfuehrung` | TEXT, nullbar | gelesener Datensatz: TRY-Datei, Eintrag im Regionalpaket, PVGIS-Datenbanken |
| `Tab_Klimaregion_STAMM` | `Zeitbezug` | TEXT `CHECK (Zeitbezug IN ('UTC','MEZ'))`, nullbar | Raster der Rohtabelle: PVGIS `UTC`, TRY `MEZ` (ohne Sommerzeit, ungedreht) |
| | `Rohdaten` | INTEGER `NOT NULL DEFAULT 0 CHECK (Rohdaten IN (0,1))` | 1 = `Tab_Klimastunde_STAMM` ist gefüllt |

**Belegung je Quelle.** TRY (Datei wie Regionalpaket) füllt alle Felder außer `Direkt_Normal_W_m2`
und `Global_Horizontal_W_m2` — beide sind gerechnet, nicht geführt. PVGIS füllt Temperatur, Feuchte,
Global-, Direkt-normal- und Diffusstrahlung, Gegenstrahlung, Wind (Geschwindigkeit und Richtung) und
Luftdruck (`SP` in Pa, gespeichert in hPa); Bedeckungsgrad, Wasserdampfgehalt, Ausstrahlung,
Qualitätskennung und Direktstrahlung horizontal bleiben NULL. Der Import schreibt Rohwerte und
Herkunft nur, wenn der Schemaschritt steht; sonst überspringt er sie benannt, und die
Rechenspalten entstehen gleich. Der Herkunftsvermerk in `Details` sagt dann „alle Felder als
Rohwerte gespeichert“ statt der Liste der nicht übernommenen Größen.

- **Rohdaten gibt es nur im Katalog**, nicht in der Projektkopie (KD-Q4). Der Export liest den
  Katalogsatz. Eine Projektregion ohne Katalogsatz exportiert die Rechenspalten aus `Tab_Solar`.
- **Altbestand:** Regionen, die vor dem Schritt importiert wurden, tragen `Rohdaten = 0`. Ihr
  Export bietet nur die Rechenspalten an, und der Dialog nennt den Grund. Nachgeladen wird nichts
  still (KD-Q7).
- **Größe:** Eine Region braucht rund 8 760 Zeilen × 16 Spalten, etwa 1,5 bis 2 MB. Die
  Auslieferungsvorlage führt Rohdaten nur, wenn KD-Q7 das so entscheidet.
- **Katalog:** Die Rohtabelle ist wie der übrige Klimakatalog von der Katalogfassung ausgenommen
  und kein Datenblock des Registers; die Herkunftsspalten stehen im Ausschluss des Eintrags
  `KLIMAREGION` und ändern keine Prüfsumme. Die Auslieferungsvorlage führt die Rohtabelle als
  Kindkatalog der Region (Kaskade); ob ausgelieferte Regionen Rohwerte tragen, entscheidet KD-Q7
  — die Testdatenbank und die heutige Auslieferung tragen keine.
- **Projektkopie:** Die Herkunftsspalten wandern mit, die Rohwerte nicht (KD-Q4).
- **SQL:** `DataRepository` mit `?`-Parametern, danach `SqlDialektPruefer`.

---

## 4. Dialogablauf: Region ermitteln → Vorschau → Import

Die Import-Überlagerung behält Quelle, Standort und Schlussleiste. Neu ist eine **Vorschaukarte**
zwischen Standort und Leiste. Der Knopf „Region ermitteln“ heißt künftig **„Datensatz suchen“**
und gilt für **alle drei Quellen**: PVGIS ruft die API, die TRY-Datei liest die Datei, der
Regionalweg liest das Paket.

1. **Eingabe:** Quelle, bei TRY Jahr und Szenario, Ort **oder** Koordinaten.
2. **„Datensatz suchen“** geokodiert, wählt den Bezugspunkt und **liest den Datensatz** (bei TRY ein
   Eintrag mit rund 155 KB, bei PVGIS ein Abruf). Nichts wird gespeichert.
3. **Vorschaukarte** mit diesen Angaben:
   - **Standort:** Anzeigename der Geokodierung, Breite/Länge, Höhe, soweit bekannt. Breite, Länge
     und Bezeichnung stehen danach **in den Feldern** und bleiben änderbar. Eine Änderung lässt die
     Vorschau verfallen.
   - **Datensatz:** Quelle, Bezugspunkt (Station, Rasterpunkt oder PVGIS-Punkt) mit Koordinate,
     Höhe, Entfernung und Höhenunterschied.
   - **Treffer:** Die Zeile „Datensatz gefunden: TRY 2045 · sommerwarm“ trägt ein grünes Kennzeichen.
     Fehlt die Ausführung, steht dort „nicht im Paket“ mit Kennzeichen und den vorhandenen
     Ausführungen als Wahlknöpfen. Ein Klick setzt Jahr und Szenario.
   - **Kennzahlen:** Jahresmittel, Minimum und Maximum der Lufttemperatur, Jahressumme der
     Globalstrahlung [kWh/m²a], Gradtagzahl G20/15 [Kd/a] (Summe von 20 − θm über die Tage mit
     θm < 15 °C, Tagesmittel aus den Stunden) und die Zahl der Heiztage.
   - **Vorschaugrafik:** Monatsmittel der Temperatur (Linie) und Monatssumme der Globalstrahlung
     (Säulen) als `Zeichenmodell` aus dem Kern im Baustein `DiagrammSvg`.
   - **Parameterbestand:** jede Größe aus Abschnitt 2 mit Haken oder Strich, so wie die Quelle sie
     führt.
   - **Hinweis (leise Zeile, kein Banner):** Ab 50 km Entfernung **oder** 200 m Höhenunterschied
     erscheint „Bezugspunkt nicht ortstypisch; für ortsgenaue Werte die TRY-Datei des DWD für
     diesen Standort einlesen“. Im Regionalweg steht immer „nächste Repräsentanzstation, nicht die
     amtliche Klimaregion“.
4. **„Import“** (Primärknopf) übernimmt **den gelesenen Vorschausatz**, ohne zweiten Abruf.
   Überlagerung schließen, neue Region in der Liste wählen. Ohne gültige Vorschau ist „Import“
   weich gesperrt mit dem Grund „Erst Datensatz suchen“, bis KD-Q3 entschieden ist. Die
   Alternative ist der heutige Weg mit Lesen beim Import.
5. **Schlussleiste:** Abbrechen · Import haftet am unteren Rand der Überlagerung (Abschnitt 1.6).

**Kernseite:** `KlimaVorschauErgebnis` erweitert sich um Standort (Länge, Breite, Anzeigename,
Höhe), Bezugspunkt, `AusfuehrungGefunden`, Kennzahlen, Monatsreihen, Parameterbestand und den
gelesenen Stundensatz (intern, für den Import). Die Regionswahl bleibt **eine** Funktion. Die
Kennzahlen rechnet der Kern (`KlimaKennzahlen`), nie die Razor-Seite.

**Umgesetzt (KD-2):** `KlimaImportAblauf.RegionErmittelnAsync` liest für alle drei Quellen über
denselben Leseweg wie der Import (`DatensatzLesen`). Das Ergebnis trägt `Standort`
(`KlimaStandort`), `Bezugspunkt` (`KlimaBezugspunkt`: Repräsentanzstation, Dateistandort oder
PVGIS-Punkt mit Höhe aus Kopf bzw. `inputs.location` und Entfernung), `AusfuehrungGefunden` mit
`Grund`, `HoehenunterschiedM`, `Kennzahlen` (`KlimaKennzahlen`, G20/15 nach VDI 3807 Blatt 1, mit
Monatsreihen), `Parameterbestand` und die Hinweise (`KlimaVorschauFestwerte`: 50 km, 200 m,
einschließlich). Die Standorthöhe kommt aus `KlimaImportAuftrag.StandortHoeheM`, bei PVGIS aus der
Antwort, bei einer TRY-Datei ohne Koordinaten aus dem Kopf; eine Höhenabfrage über das Netz gibt
es nicht. `Laufen` übernimmt eine passende, importierbare Vorschau ohne zweiten Abruf und ohne
zweite Geokodierung (`PasstZu`: Ort bzw. Koordinaten, Quelle, Datei, Jahr, Szenario); die Hülle
hält die letzte Vorschau. Nachweis: `KlimaImportAblaufTests`, `TryPaketLeserTests`.

**Umgesetzt (KD-3):** Die Überlagerung trägt unter dem Standort das Feld „Höhe des Ortes (m)“
(`StandortHoeheM`, geht in Suche und Import), darunter „Datensatz suchen“ für alle drei Quellen und
die Vorschaukarte nach dem Mockup: Kennzeichen „Datensatz gefunden: …“ bzw. „… ist nicht im Paket“
mit Grund und den vorhandenen Ausführungen als Wahlknöpfen (ein Klick setzt Jahr und Szenario und
sucht neu), Standort- und Bezugspunktblock (Koordinate, Entfernung, Höhe, Höhenunterschied mit der
aktuellen Höhe des Ortes), die Hinweiszeilen (Schwellen über `KlimaVorschauFestwerte.NichtOrtstypisch`),
fünf Kennzahlen, das Bild der Monatsreihen (`KlimaVorschaubild` im Kern über
`ChartRenderer.SummenlinieModell`, Strahlung links, Temperatur rechts, im Baustein `DiagrammSvg`) und
der Parameterbestand (geführt / gerechnet / fehlt). Longitude, Latitude und eine leere Bezeichnung
werden aus dem Standort vorbelegt (auf dem Ortsweg heißt die Region wie der Ort). Der Dialog hält den
Eingabeschlüssel der Suche (Ort bzw. Koordinaten, Quelle, Datei, Paket, Jahr, Szenario); weicht er ab,
ist die Karte sichtbar verfallen. „Daten einlesen“ ist ohne gültige, passende Vorschau weich gesperrt
(`aria-disabled`, Grund im Titel und in der Leiste) — nur, wo die Hülle eine Vorschau anbietet. Das
Wunschmaß der Überlagerung steigt auf 1 000 px (Formular vor der Suche 900 px); die Karte rollt unter
der haftenden Leiste. Nachweis: `KlimadatenDialogTests` (Treffer, kein Treffer mit Ausführungswahl,
Verfall, weiche Sperre, Felder, Höhe im Auftrag, Hinweis ab 50 km/200 m, Fehlschlag),
`KiMaskenabdeckungWacheTests` (Feld `hoehe`), `fensterprobe.mjs` Fall `klimaimport` mit Karte.

---

## 5. CSV-Export

**Bestand, an den der Export anschließt:**
- `EPOS.Kern/Allgemein/Export/ZeitreihenCsv.cs` ist der eine Schreiber für Zeitreihen am Bild:
  erste Spalte das Zeitraster (`Zeitraster.Stunde` 1…8 760), dann je Reihe „Name [Einheit]“,
  Semikolon, Dezimalkomma, `0.0##`, UTF-8 mit BOM, Spaltennamen entschärft
  (`CsvExportClass.Export` für die benannten Simulationsexporte).
- Die Formatwahl auf der **Leseseite**: `GanglinienImportOptionen` (Trennzeichen, Dezimaltrenner,
  Kopfzeile, Wert- und Zeitspalte, erste Datenzeile) im Dialog „Format und Vorschau“
  (`EPOS.UI/Dialoge/Strom/GanglinieImportOptionenDialog.razor`, Baustein `GanglinienFormatLauf`,
  Formatbeschreibung `GanglinienImportFormat`). Die Spaltenzuordnung je Größe zeigt
  `SpeicherFlottenCsvOptionen`.
- Einen Exportdialog **mit Format- und Spaltenwahl** gibt es noch nicht. Der Gebäudeexport
  (`GebaeudeExportDialog`) zeigt das Muster einer Export-Überlagerung: Formatwahl als
  `Optionsgruppe`, Meldungen vor dem Schreiben, „Speichern…“ im OK-Weg mit Dateiwahl der Plattform.

**Der Klima-Export** sitzt als Handlung „Exportieren…“ in der `Auswahlleiste` der Regionsliste (eine
Region) und öffnet eine Überlagerung:

| Gruppe | Inhalt | Vorgabe |
|---|---|---|
| Parameter | Kästchen je Größe, gruppiert nach Temperatur und Feuchte, Strahlung, Wind und Druck, abgeleitet (Taupunkt), Fassadenwerte. Nicht verfügbare Größen sind weich gesperrt mit Grund („Quelle führt die Größe nicht“, „Region ohne Rohdaten importiert“). „Alle“ und „Keine“ | Temperatur, Feuchte, Global-, Direkt- und Diffusstrahlung |
| Trennzeichen | Semikolon · Komma · Tabulator | Semikolon |
| Dezimalzeichen | Komma · Punkt. Komma neben Trennzeichen Komma ist weich gesperrt | Komma |
| Zeitspalte | laufende Stunde 1…8 760 · Datum und Uhrzeit `TT.MM. hh:00` · ISO 8601 mit Bezugsjahr · keine | laufende Stunde |
| Zeitbezug | MEZ ohne Sommerzeit · UTC | MEZ (KD-Q8) |
| Kopf | „Name [Einheit]“ in einer Zeile · Name und Einheitenzeile · ohne Kopf; dazu Herkunftsblock mit `#`-Zeilen (Region, Quelle, Bezugspunkt, Lizenzvermerk) ja/nein | eine Zeile, Herkunft ja |
| Vorlagen | „Haus (wie CSV am Bild)“ und „International (Komma, Punkt, ISO)“ setzen die Formatfelder | – |
| Vorschau | die ersten fünf Datenzeilen, wie sie geschrieben werden | – |

Die Schlussleiste lautet Abbrechen · Speichern… und schreibt nur im OK-Weg. Im Kern liegen
`KlimaCsv` (neben `ZeitreihenCsv`, teilt dessen Entschärfung und Zahlformat) und
`KlimaExportOptionen`. Die Hülle liefert Dateiwahl und Schreibweg. Die Rechtsangabe der Quelle (CC
BY 4.0, DWD-Bedingungen, PVGIS) steht im Herkunftsblock.

**Import aus CSV** (Rückweg der eigenen Datei, Spaltenzuordnung nach `GanglinienImportOptionen`)
wäre eine vierte Quelle mit Pflichtspalten. Er ist eigener Schritt KD-6 und folgt erst auf
Entscheid KD-Q5.

---

## 6. Auswirkung auf Rechenweg, Referenzbasis und Einfrierregeln

- **Rechenweg:** unverändert. `Tab_Solar(_STAMM)` und `Tab_Klimadaten(_STAMM)` behalten Spalten,
  Werte und Umrechnungen (Direkt-Normal, Diffusregel unter 5°, UTC-Drehung). Die Rohtabelle hat
  keinen Leser im Rechenweg. Eine Wache hält das fest: Keine Datei unter `Allgemein/Simulation/**`
  und `BhkwPlan.cs` nennt `Tab_Klimastunde`.
- **Referenzbasis:** Kein Wechsel. Die Testdatenbank bekommt durch den Schemaschritt eine **leere**
  Tabelle und Nullspalten. Ihre Klimaregionen bleiben ohne Rohdaten (`Rohdaten = 0`). Die
  Abnahme ist der Referenzlauf der 18 CI-Projekte mit `GESAMT: PASS`.
- **Einfrierregeln:** keine neue. Die Regel „gesäte Gemeinjahr-Konvention … Klimaregion-Zuordnung
  der Referenzprojekte“ bleibt unberührt, solange die Testdatenbank keine Region neu importiert.
  Ein späterer Leser einer Rohspalte (etwa Wind für die Lüftung oder `E` für die Abstrahlung)
  braucht eigenen Entscheid, eigene Spalte in `Tab_Solar` und eine neue Basis.
- **Regionswahl (KD-Q1):** Eine geänderte Zuordnung wirkt nur auf **neue** Importe und nicht auf
  vorhandene Regionen. Die Referenzbasis ist nicht betroffen.

---

## 7. Umsetzungsschritte

Jeder Schritt umfasst höchstens rund 150 Werkzeugaufrufe, Bau mit `WP-Plan.Kern.slnf`, Tests mit
`--filter`. Das Gate fährt die Orchestrierung.

| Schritt | Inhalt | Abnahme |
|---|---|---|
| **KD-1** Schlussleiste — *umgesetzt 16.10.2026* | Haftregel der Überlagerung auf die Leiste mit Primärknopf in `.epos-einlesen` erweitern (oder Leiste mit Statusspanne), Fenstermaß Klimadaten über `Fenstermass.MitUeberlagerung`, kein Schemaschritt | `StilblattTests`, `FensterrahmenTests`, neuer Fall in `fensterprobe.mjs` mit Gegenprobe, Bau der Windows-Schale mit `EnableWindowsTargeting=true` |
| **KD-2** Vorschau Kern — *umgesetzt 16.10.2026* | `KlimaVorschauErgebnis` erweitern, Vorschau liest den Datensatz für alle drei Quellen, `AusfuehrungGefunden`, `KlimaKennzahlen`, Höhe aus Kopf bzw. PVGIS, Hinweis nach KD-Q1, Import übernimmt den Vorschausatz | `TryPaketLeserTests`, `KlimaImportAblaufTests` mit lokaler Probe-`data.zip`, Gegenprobe Szenario fehlt, Nachrechnung Stuttgart-Vaihingen (56 km, Stötten) als fester Fall |
| **KD-3** Vorschau Dialog — *umgesetzt 16.10.2026* | Vorschaukarte, Felder befüllen, Grafik, Kennzeichen, Parameterbestand, Ressourcen in beiden Sprachen, KI-Feldliste (`KiDialoge`) nachziehen | `KlimadatenDialogTests` (Treffer, kein Treffer, Verfall bei Eingabe, Import ohne Vorschau weich gesperrt), `KiMaskenabdeckungWacheTests`, `designer_neu.py` |
| **KD-4** Rohdaten — *umgesetzt 16.10.2026, Schemaschritt geparkt bis zum Umhängen in die Kette* | Schemaschritt `KlimaRohwerteSchema`: `Tab_Klimastunde_STAMM`, Spalten an `Tab_Klimaregion(_STAMM)`; `DwdTryLeser` und PVGIS-Leser liefern alle Felder, Schreibweg füllt sie, Katalogregister und Katalogfassung, Projektkopie ohne Rohwerte; Nachzüge aus KD-3: Bezeichnung auf dem Ortsweg, Parameterbestand mit „wird gespeichert“ | `SqlDialektPruefer`, `KlimaRohwerteTests` (Schritt, Leser, Schreibweg mit und ohne Rohtabelle, Projektkopie, Ortsweg, Wache „Rechenweg liest Rohtabelle nicht“), Referenzlauf 18 Projekte `PASS` |
| **KD-5** Export | `KlimaCsv`, `KlimaExportOptionen`, Exportdialog, Handlung in der Auswahlleiste, Taupunkt abgeleitet | Kern-Tests (Trennzeichen × Dezimal × Zeitspalte, Einheitenzeile, Altbestand ohne Rohdaten), bunit Exportdialog, `CsvAmBildWacheTests` unberührt |
| **KD-6** CSV-Import (nur nach KD-Q5) | vierte Quelle mit Spaltenzuordnung über `GanglinienImportOptionen` | Rundlauf Export → Import bitgleich in den Rechenspalten |
| **KD-7** Doku | Konzept Klimadatenquellen §§ 1, 2, 6, 7, 10, 12 nachziehen, Wiki-Quelle „Programm Dokumentation/Klimadaten“, Logbuch-Entwurf, dieses Papier nach `ueberholt/` | `DokumentationLinkWacheTests`, `WikiProduktdatenWacheTests` |

KD-1 lässt sich unabhängig vorziehen. KD-2 und KD-3 gehen vor KD-4, weil die Vorschau keine neuen
Spalten braucht. KD-5 setzt KD-4 voraus, wenn die Rohwerte exportiert werden sollen.

---

## 8. Berührungspunkte mit anderen Sitzungen

- **Am Klimadatenimport arbeitet derzeit keine Sitzung.** Die letzten Klimawellen sind #367 bis #385
  (19./20.09.2026, Regionsvorschau KL-3, Bezugsjahr KL-7). Offen aus der Statusdatei: die
  Windows-Abnahmen A-KL3-* (Block „Nach #369“).
- **Schemaschritte:** angemeldet sind 234 bis 237, **238 ist frei** (Stand 16.10.2026). KD-4 meldet
  vor dem Bau die dann nächste freie Nummer an.
- **Gebäudesimulation, Welle DA-8** (Schritt 237, Absorptions- und Emissionsgrad je Bauteil): Sie
  ist der wahrscheinliche erste Leser von `E` (Ausstrahlung) und `Gegenstrahlung`. Die Rohtabelle
  liefert `E`, aber eine Rechenspalte bleibt eine Entscheidung jener Sitzung.
- **KB-7 Wasserkosten des Rückkühlwerks** (Entwurf 16.10.2026): Sie liest `Luftfeuchte`. Keine
  Änderung, nur Kenntnis.
- **Zapfprofil** liest `Bedeckungsgrad` (`ZapfprofilCtrl.Eingang`). Nicht betroffen.
- **HottCAD-Leseweg** (Statusdatei, Nach #730): Er wäre eine spätere Klimaquelle und schlösse an
  dieselbe Vorschau an.

---

## 9. Offene Entscheide

| Kennung | Frage | Empfehlung |
|---|---|---|
| **KD-Q1** | Regionswahl im Regionalweg: (a) nächste Station belassen, ehrlich benennen, Höhe und Entfernung zeigen und ab 50 km oder 200 m auf die ortsgenaue TRY-Datei hinweisen; (b) Zuordnung über die Regionenkarte des DWD (Polygone mit Höhenstufen; braucht Kartendaten samt Lizenz und Höhenmodell); (c) Regionalweg nur noch als Rückfall, Vorgabe „TRY-Datei“ | **(a), dazu der Hinweis aus (c).** Vorher die Region von Stuttgart in der DWD-Regionenkarte nachsehen (Prüfpunkt aus 1.3). — *entschieden 16.10.2026: Empfehlung übernommen* |
| **KD-Q2** | Rohwerte in eigener Tabelle `Tab_Klimastunde_STAMM` oder als Spalten an `Tab_Solar(_STAMM)` | **Eigene Tabelle.** Keine Spalte ohne Leser in den Rechentabellen, Projektkopie bleibt schlank. — *entschieden 16.10.2026: Empfehlung übernommen* |
| **KD-Q3** | Liest die Vorschau den Datensatz und übernimmt der Import ihn ohne zweiten Abruf? Das ersetzt den Anwenderentscheid 19.09.2026 (b) „kein Eintrag entpackt, zweiter Abruf angenommen“. | **Ja.** Nur so zeigt der Dialog Daten vor dem Import. Rund 155 KB statt 0 sind tragbar. — *entschieden 16.10.2026: Empfehlung übernommen* |
| **KD-Q4** | Rohdaten auch in der Projektkopie (`Tab_Klimastunde`)? | **Nein.** Kein Leser im Lauf, der Export liest den Katalog. — *entschieden 16.10.2026: Empfehlung übernommen* |
| **KD-Q5** | CSV als vierte Importquelle (Rückweg des Exports, Fremddateien)? | **Später**, eigener Schritt KD-6 nach dem Export. — *entschieden 16.10.2026: Empfehlung übernommen* |
| **KD-Q6** | Export einer oder mehrerer Regionen je Lauf? | **Eine Region je Datei**, Mehrfachwahl später als mehrere Dateien. — *entschieden 16.10.2026: Empfehlung übernommen* |
| **KD-Q7** | Rohdaten für Bestands- und Auslieferungsregionen nachladen? | **Nein**, nur Neuimporte. Die Auslieferungsvorlage führt Rohdaten erst nach eigenem Entscheid (Größe). — *entschieden 16.10.2026: Empfehlung übernommen* |
| **KD-Q8** | Zeitbezug des Exports als Vorgabe | **MEZ ohne Sommerzeit** (Raster der TRY und der Anwender), UTC wählbar. — *entschieden 16.10.2026: Empfehlung übernommen* |
| **KD-Q9** | Schwellen des Hinweises (50 km, 200 m) | **Ja**, als Festwerte im Kern, nicht in den Einstellungen. — *entschieden 16.10.2026: Empfehlung übernommen* |

---

## 10. Fundstellen

- RE-Lab-Projects, *TRY_DE_2015_2045*, README und Release v1.4.0 (15 TRY-Regionen mit
  Repräsentanzstationen, je 3 Szenarien × 2 Bezugsjahre, Daten CC BY 4.0, Rohquelle
  DWD-Klimaberatungsmodul `kunden.dwd.de/obt/`): https://github.com/RE-Lab-Projects/TRY_DE_2015_2045
- DWD Climate Data Center, Beschreibungen `grids_germany/{daily,monthly}/Project_TRY/*`
  (Rohdaten zur Erstellung der Testreferenzjahre 2017, 1 km × 1 km, ETRS89/LCC, Projekt
  „TRY-Weiterentwicklung“ des BBSR): https://opendata.dwd.de/climate_environment/CDC/grids_germany/monthly/Project_TRY/
- Meldung „Neue, hoch aufgelöste TRY-Daten erleichtern energie- und klimaoptimiertes Planen und
  Bauen“ (baulinks, 2017): Umstellung von 15 Klimaregionen mit Repräsentanzstation auf Rasterdaten
  je Quadratkilometer.
- Nicht erreichbar aus der Arbeitsumgebung: `dwd.de` (Seite „Testreferenzjahre“, Regionenkarte
  2011). Der Prüfpunkt in 1.3 bleibt deshalb offen.
- Code: `EPOS.Kern/Allgemein/Import/TryPaketLeser.cs`, `KlimaImportAblauf.cs`, `DwdTryLeser.cs`,
  `EPOS.Kern/Allgemein/SolarPVGISCalculator.cs`, `EPOS.UI/Dialoge/Klimadaten/KlimadatenDialog.razor`,
  `EPOS.UI.Daten/Klimadaten/KlimadatenHuelle.cs`, `EPOS.UI/wwwroot/epos-ui.css` (Überlagerung).
