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
| **DWD-TRY-Datei** (`.dat`) | Deutschland, Datei vom Rechner | keines | Pfad der Datei; den Standort bringt ihr Kopf mit, änderbar |
| **TRY-Regionaldaten** (`data.zip`) | Deutschland, 15 Regionen | Bereichsabrufe; bei lokaler Datei keines | Standort, Jahr, Szenario; wahlweise Pfad einer lokalen `data.zip` |

Eine Klimaregion trägt das, was bei ihrem Import geholt wurde; die Quelle steht im
Herkunftsvermerk (`Tab_Klimaregion_STAMM.Details`). Regionen unterschiedlicher Herkunft
stehen nebeneinander, ohne einander zu beeinflussen.

**Warum jede Quelle Longitude und Latitude braucht:** Sonnengeometrie und die Umrechnung
auf Direkt-Normal rechnen damit — bei den Regionaldaten kommt die Wahl der nächsten Region
dazu. Eine TRY-Datei führt in ihrem Kopf **Lambert-Koordinaten**; der Kern rechnet sie in
Länge und Breite um, und der Dialog belegt beide Felder damit vor (Abschnitt 3.1). Der
Anwender kann sie überschreiben.

## 2. Bedienweg

Im Dialog **Klimadaten** steht über der Gruppe „Standort" die Gruppe **Klimaquelle**:

1. **Quelle wählen** — PVGIS (vorgewählt), TRY-Datei oder TRY-Regionaldaten.
2. **Standort** — Ortsname oder Longitude/Latitude mit Bezeichnung, wie bei PVGIS.
3. **Bei TRY-Datei** — die Datei über den Dateiwähler (`*.dat`). Mit der Wahl
   belegt der Dialog **Longitude, Latitude und — falls leer — die Bezeichnung** aus dem
   Dateikopf vor (Abschnitt 3.1); darunter steht, woher die Zahlen kommen. Jedes Feld
   bleibt änderbar.
4. **Bei Regionaldaten** — Jahr (2015 oder 2045) und Szenario (mittleres Jahr,
   sommerwarm, winterkalt); Vorgabe **2015, mittleres Jahr**. Ohne Dateipfad läuft der
   Bereichsabruf über die hinterlegte Adresse; mit Pfad wird die lokale `data.zip` gelesen.
5. **Daten einlesen** — die Region entsteht mit 8 760 Stundenwerten und 365 Tageswerten;
   die Meldung nennt die Quelle, bei den Regionaldaten zusätzlich Region, Koordinate,
   Entfernung, Szenario und Jahr.

Der Lauf ist abbrechbar. Eine Region desselben Namens wird benannt abgelehnt — und genau
das ist der Schutz vor einem Doppelimport: Die Eingabefelder bleiben nach einem Erfolg
stehen, damit „Daten einlesen" für den nächsten Ort bedienbar bleibt.

**Die Knöpfe stehen in EINER Fußleiste** unter dem Rahmen: `Daten einlesen · Füller ·
Löschen · Beenden`, Beenden hervorgehoben — dasselbe Muster wie in den Katalogdialogen.
„Durchsuchen" ist kein Knopf der Fußleiste, sondern steht neben seinem Feld: Die Blöcke
Klimaquelle und Standort stehen im einspaltigen Formularraster, das Beschriftung, Feld und
Knopf in eine Zeile setzt.

### 2.1 Die Regionsliste

Links steht die **Katalogliste des Hauses** — derselbe Baustein wie in den vierzehn anderen
Katalogen, mit Suchfeld über alle Spalten, Trichter und Sortierpfeil im Spaltenkopf und der
Trefferzahl. Sieben Spalten:

| Spalte | Inhalt |
|---|---|
| Klimaregion | der Bezeichner; er bleibt der Schlüssel jeder Aktion (Ansicht, Löschen) |
| Quelle | das ganze Wetterjahr in einem Satz: Quelle · Bezugsjahr · Szenario, etwa „TRY-Regionaldaten (Deutschland) · 2045 · sommerwarm". Die Quelle steht mit denselben Worten da wie in der Gruppe „Klimaquelle"; ohne Bezugsjahr und Szenario bleibt es bei ihr allein |
| Standort | der Ortsname aus `Details`, wo einer steht; sonst das Koordinatenpaar |
| Longitude, Latitude | Grad mit vier Nachkommastellen (rund 11 m), als **Zahl** sortierbar und filterbar |
| Importdatum | ISO `yyyy-MM-dd` — als **Text** geführt, weil ISO als Zeichenkette in der Reihenfolge des Datums sortiert und „enthält 2026-09" damit zum Monatsfilter wird |
| Schreibschutz | Ja/Nein; ein Satz der Auslieferung lässt sich nicht löschen |

**Kein Vergleichsknopf:** Eine Klimaregion ist kein Gerät mit Kennwerten — sie
nebeneinanderzulegen hieße, die Liste noch einmal danebenzustellen.

**Quelle und Importdatum sind leer, wo eine Region aus dem Altbestand stammt** (angelegt vor
Schemaschritt 95): Dort steht der Halbgeviertstrich. Nachdatiert wird nichts.

**Warum Bezugsjahr und Szenario in der Spalte „Quelle" stehen und nicht in zwei eigenen.**
Gesucht wird in dieser Liste über alle Spalten; „2045" engt damit auf das Bezugsjahr ein,
ohne dass die Liste auf neun Spalten wächst. Der Dialog ist schmal, und die drei Angaben
beantworten eine einzige Frage — „welches Wetterjahr trägt diese Region". Eine Sortierung
nach Jahr allein gibt es dafür nicht; sie wäre der Preis, und er ist kleiner als zwei
weitere Spalten.

**Die Schlüssel in der Datenbank.** `Tab_Klimaregion(_STAMM).Szenario` führt `MITTEL`,
`SOMMERWARM` oder `WINTERKALT`, `Bezugsjahr` die Zahl 2015 oder 2045 (Schemaschritt 97).
Anzeigetexte stehen in `MyResource` und werden von `KlimaAnzeige` eingesetzt — **einer
Stelle** für Liste, Herkunftszeile und Auswahlfeld. NULL heißt „sagt nichts dazu":
Altbestand, PVGIS (kennt keine TRY-Szenarien) und jede TRY-Datei, deren Kopf die Art des
Datensatzes nicht nennt.

**Der Standort ist gerechnet, nicht gespeichert.** `Details` ist Freitext und sagt je Quelle
etwas anderes: Der PVGIS-Abruf schreibt den geokodierten Ort, die zwei TRY-Quellen einen
Herkunftsvermerk, der mit dem Namen der Quelle beginnt und Lizenz, Station und Entfernung
mit „ · " aneinanderreiht. `KlimaregionStammCtrl.Standorttext` nimmt deshalb das erste Glied
— aber nur, wenn es ein Ort ist und nicht der Quellenname; sonst stehen die Koordinaten.

### 2.2 Die Herkunft auf der Übersicht

Die Startseite nennt im Klimakasten als zweite, leise Zeile, **womit gerechnet wird**:
Quelle samt Bezugsjahr und Szenario, Bezeichner, Standort und Importdatum der Region des
offenen Projekts — „Klimadaten: TRY-Regionaldaten (Deutschland) · 2045 · sommerwarm ·
hagelloch · Tübingen, Deutschland · Import 19.09.2026". Gelesen wird
die **Projektkopie** `Tab_Klimaregion` — dieselbe Zeile, mit der der Rechenlauf arbeitet,
nicht der Katalogsatz daneben. Eine Region ohne Quelle und Importdatum bekommt die Kurzform
aus Bezeichner und Standort; ohne offenes Projekt steht keine Zeile.

Die Klimawahl darüber ist **durchsuchbar** (Baustein `Standards/Suchauswahl`): Tippen
filtert über alle Einträge, ↑ ↓ wandern, Enter übernimmt, Esc schließt die Liste. Gewählt
und gespeichert wird die **Id** der Stammregion, nicht ihr Name.

Jeder Eintrag nennt in Klammern das Wetterjahr in Kurzform — „hagelloch (TRY 2045
sommerwarm)", „München (PVGIS)"; eine Region ohne Herkunft behält ihren blanken Namen. Das
Kürzel „TRY" steht für beide TRY-Quellen: In einer Klammer hinter dem Regionsnamen zählt,
welches Wetterjahr gemeint ist, nicht, aus welcher Datei es kam — das sagt die Liste im
Klimadaten-Dialog. Den Text baut der Kern (`StartseiteCtrl.KlimaregionenMitId` über
`KlimaregionStammCtrl.Auswahlzeilen`), nicht die Razor-Seite; gesucht wird über den ganzen
Eintrag, also auch über das Jahr.

## 3. Das TRY-Format und seine Zuordnung

Eine DWD-TRY-Datei besteht aus einem freien Kopf (34 Zeilen bei den Testreferenzjahren
2015, 36 bei den Projektionen 2045) bis zu der Zeile, die getrimmt mit `***` beginnt, und
danach aus genau **8 760 Datenzeilen mit 17 weißraumgetrennten Feldern**:

```
RW HW MM DD HH t p WR WG N x RF B D A E IL
```

| Feld | Bedeutung | Ziel |
|---|---|---|
| `RW`, `HW` | Lambert-Koordinaten der Station, je Datenzeile wiederholt | aus der Datenzeile nicht übernommen — der **Standort** kommt aus dem KOPF (Abschnitt 3.1) |
| `MM`, `DD`, `HH` | Monat, Tag, Stunde **1…24 MEZ**; `HH` benennt das Intervall, das zu `HH:00` endet | Reihenfolge der Zeile (Abschnitt 4) |
| `t` | Lufttemperatur [°C] | `Tab_Solar_STAMM.Temperatur` |
| `B` | Direktstrahlung **horizontal** [W/m²] | `Direktstrahlung`, nach der Umrechnung aus Abschnitt 5 |
| `D` | Diffusstrahlung horizontal [W/m²] | `Diffusstrahlung` |
| `B + D` | Globalstrahlung horizontal | `Globalstrahlung` |
| `A` | atmosphärische Gegenstrahlung [W/m²] | `Tab_Solar_STAMM.Gegenstrahlung` |
| `RF` | relative Feuchte [%] | `Tab_Solar_STAMM.Luftfeuchte` |
| `N` | Bedeckungsgrad [Achtel, 0…8] | `Tab_Solar_STAMM.Bedeckungsgrad` |
| `p WR WG x E IL` | Druck, Windrichtung, Windgeschwindigkeit, Wasserdampfgehalt, langwellige Ausstrahlung, Qualitätsbit | **benannt verworfen** (Abschnitt 6) |

Aus Stundenwerten und Sonnengeometrie entstehen anschließend — über dieselben Methoden wie
bei PVGIS — die vier Fassadenwerte `Sol_Nord/Ost/Sued/West`, der `Sonnenwinkel` und daraus
die 365 Tageswerte in `Tab_Klimadaten_STAMM` samt `WE`, `TagTyp_W` und `TagTyp_NW`. **Der
Weg hinter dem Leser ist derselbe wie bei PVGIS**; die TRY-Quellen treten nur an die Stelle
des Netzabrufs.

Ein Kopf ohne Trennzeile, eine falsche Feld- oder Zeilenzahl, ein unlesbares Zeit- oder
Zahlenfeld und eine doppelt belegte Stunde führen jeweils zu einer benannten Meldung mit der
Zeilennummer — und zu keiner Region.

### 3.1 Der Standort aus dem Dateikopf

Der Kopf führt den Standort als **Rechtswert** und **Hochwert** — nicht als Länge und
Breite. Beides braucht das Haus aber: für die Sonnengeometrie, für die Fassadenwerte und
für `Tab_Klimaregion_STAMM`.

**Die Projektion ist ETRS89 / LCC Europa (`EPSG:3034`)**: Lambert konform konisch, zwei
Standardparallelen 35° N und 65° N, Ursprung 52° N / 10° O, falscher Ostwert
4 000 000 m, falscher Nordwert 2 800 000 m, Ellipsoid GRS80 (`a = 6 378 137 m`,
`1/f = 298,257222101`). Die **Formelquelle** ist Snyder, *Map Projections — A Working
Manual*, USGS Professional Paper 1395 (1987), Abschnitt 15; gerechnet wird ellipsoidisch in
`double` (`EPOS.Kern/Allgemein/Import/LambertKoordinaten.cs`).

**Nachgemessen, nicht angenommen.** Die offenen Regionaldaten tragen Breite und Länge ihrer
fünfzehn Regionsmittelpunkte im DATEINAMEN und Rechts-/Hochwert im KOPF — zwei unabhängige
Angaben desselben Punktes. Über alle fünfzehn Regionen, von 47,49° N / 11,10° O bis
54,09° N / 12,14° O, weicht die gerechnete Koordinate um höchstens **0,00005°** vom
Dateinamen ab; der Rest ist die Rundung der Kopfwerte auf das 500-m-Raster. Die fünfzehn
Paare stehen als feste Fälle in `LambertKoordinatenTests` — der Nachweis braucht kein Netz.

**Grenzen.** Ein Punkt außerhalb **45…56° N** und **5…16° O** ist für eine TRY-Datei kein
Standort, sondern ein Lesefehler: Beide Rechenwege liefern dann `false` und `NaN`, nie eine
stille Null. Dasselbe gilt für einen Kopf ohne oder mit unlesbarem Rechts-/Hochwert —
`TryKopf.Laenge`/`.Breite` bleiben `null`.

**Wer gewinnt.** Der Auftrag hat Vorrang: Was der Anwender eingetragen oder über den
Ortsnamen geholt hat, bleibt stehen. Erst wenn er keine Koordinaten mitbringt, kommt der
Standort aus dem Kopf. Beides steht **benannt** in Meldung und Herkunftsvermerk — „Standort
aus dem Dateikopf: Rechtswert …, Hochwert … → …° O / …° N" oder „Standort vom
Anwender". Fehlt beides, ist es ein benannter Eingabefehler und keine Region bei 0°/0°.

Für den Dialog liest der Kern den Kopf mit `DwdTryLeser.KopfLesen` — **nur bis zur
Trennzeile**, ohne die 8 760 Datenzeilen zu deuten, ohne Netz und ohne Datenbank.

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

`Tab_Solar` und `Tab_Solar_STAMM` führen je Stunde `Gegenstrahlung` [W/m²], `Luftfeuchte` [%]
und `Bedeckungsgrad` [Achtel] — die Größen, die die Gebäudesimulation nach VDI 6007 braucht.
PVGIS füllt die ersten beiden (`IR(h)`, `RH`), TRY alle drei (`A`, `RF`, `N`); **NULL heißt
„nicht verfügbar", nie 0.** Eine PVGIS-Region trägt deshalb keinen Bedeckungsgrad, und die
Schätzung aus dem Diffusanteil bleibt das, was sie ist: der Rückfall bei NULL.

Für Druck (`p`), Windrichtung (`WR`), Windgeschwindigkeit (`WG`), Wasserdampfgehalt (`x`),
langwellige Ausstrahlung (`E`) und Qualitätsbit (`IL`) gibt es keine Spalte. Diese sechs Größen
werden **nicht still übergangen**: Sie stehen im Herkunftsvermerk der Region
(`Tab_Klimaregion_STAMM.Details`, „nicht übernommen: p, WR, WG, x, E, IL") und in der Meldung
nach dem Import — damit niemand sie später in der Datenbank sucht. Die **Windgeschwindigkeit**
bleibt ausdrücklich draußen: keine Spalte ohne Leser (Umsetzungskonzept Gebäudesimulation F-S3).

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
- **Regionsvorschau.** „Region ermitteln" sagt **vor** dem Einlesen, welche Region der
  Standort trifft: „Region 14 Stötten, Station 48,6600 / 9,8600, Entfernung 12 km", dazu
  Bezugsjahre und Szenarien, die das Paket für diese Region führt. Gelesen wird dafür allein
  das Zentralverzeichnis — **kein Eintrag wird entpackt**. Das Einlesen liest das Paket danach
  ein zweites Mal; die Regionswahl ist dieselbe Funktion, Vorschau und Lauf können deshalb
  nicht auseinanderlaufen. Ein Fehlschlag (kein Bereichsabruf, außerhalb der 300-km-Grenze,
  Ort unbekannt) steht als Zeile da und sperrt nichts.
- **Die Region wird mit Namen genannt.** Region 1 ist Bremerhaven, Region 15
  Garmisch-Partenkirchen; die fünfzehn Namen stehen in `TryRegionsnamen` und sind reine
  Anzeige. Die Zuordnung bleibt der nächste Stationsmittelpunkt aus dem Paket.

## 8. Lizenz und Herkunftsvermerk

Jede TRY-Region bekommt ihren Herkunftsvermerk in `Tab_Klimaregion_STAMM.Details` und
denselben Text in der Importmeldung:

- **TRY-Datei:** Dateiname, Importdatum, Lizenzvermerk, verworfene Größen.
- **Regionaldaten:** Regionsnummer, Koordinate, Entfernung, Szenario, Jahr, Quelle (Adresse
  oder Dateiname), Importdatum, Lizenzvermerk, verworfene Größen.

Daneben — und unabhängig vom Freitext — hält `Tab_Klimaregion(_STAMM)` die Herkunft als
Angabe **für das Programm**: `Quelle` trägt den sprachneutralen Schlüssel `PVGIS`, `TRY_DATEI`
oder `TRY_REGIONAL`, `Importdatum` den Tag des Imports als ISO-Text `yyyy-MM-dd`. **NULL heißt
bei beiden Altbestand**; nachdatiert wird nichts. Beide wandern mit der Projektkopie.

Seit Schemaschritt 97 stehen daneben **`Szenario`** (`MITTEL` | `SOMMERWARM` | `WINTERKALT`)
und **`Bezugsjahr`** (2015 oder 2045) — die Angabe, welches Wetterjahr die Reihe beschreibt.
Sie wird je Quelle so gefüllt:

| Quelle | Szenario | Bezugsjahr |
|---|---|---|
| PVGIS-TMY | NULL — PVGIS kennt keine TRY-Szenarien | NULL |
| TRY-Datei | aus der Kopfzeile „Art des TRY"; ist sie unlesbar oder fehlt sie, NULL | aus der Kopfzeile „Bezugszeitraum" nach der **DWD-Konvention** (`1995-2012 → 2015`, `2031-2060 → 2045`); jeder andere oder fehlende Zeitraum ergibt NULL, nie die Vorgabe |
| TRY-Regionaldaten | aus dem Auftrag — der Anwender hat es gewählt, und danach ist das Paket gelesen worden | aus dem Auftrag |

Gemessen wird die Kopfzeile gegen die tragenden Wortteile („sommer"+"warm",
„winter"+"kalt", „mittl") ohne Rücksicht auf Schreibweise, Bindestrich und Zwischenraum;
was sich nicht zuordnen lässt, bleibt leer. Auch diese zwei Spalten wandern mit der
Projektkopie.

Das **Bezugsjahr einer TRY-Datei** kommt aus der Kopfzeile „Bezugszeitraum". Gelesen werden
die ersten beiden vierstelligen Jahreszahlen des Textes, gleich womit sie verbunden sind
(Bindestrich, Gedankenstrich, „bis", Zwischenräume); das Paar wird gegen eine **benannte
Tabelle** gehalten — `1995-2012 → 2015`, `2031-2060 → 2045` (DWD-Konvention). Ein Zeitraum,
der nicht darin steht, ein einzelnes Jahr und eine fehlende Zeile ergeben NULL; eine
Vorgabe wäre hier eine Behauptung über die Datei. Der gelesene Zeitraum steht **wörtlich**
im Herkunftsvermerk `Details`, damit die Ableitung nachlesbar bleibt.

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
| **KL-Q3** | Temperatur, Global/Direkt/Diffus und die Flächenwerte werden übernommen; `p WR WG x E IL` werden benannt verworfen. |
| **Anwender 19.09.2026 (e)** | „Alles Relevante für die Gebäudesimulation aufnehmen": `Gegenstrahlung`, `Luftfeuchte` und `Bedeckungsgrad` an `Tab_Solar(_STAMM)`, dazu `Quelle` und `Importdatum` an `Tab_Klimaregion(_STAMM)` — ein Schemaschritt. Windgeschwindigkeit nicht: keine Spalte ohne Leser. |
| **Anwender 19.09.2026 (b)** | Regionsvorschau vor dem Import; der zweite Netzabruf ist angenommen. |
| **KL-Q4** | Jahr und Szenario sind wählbar, Vorgabe 2015, mittleres Jahr. |
| **KL-Q5** | Eigene Wiki-Seite „Programm Dokumentation/Klimadaten". |
| **Anwender 19.09.2026 (KL-6)** | „Wird die Quelle und Auswahl (z. B. TRY 2045 sommerwarm …) der Klimadaten angezeigt? Diese sollte auch bei der Klimaregion sichtbar sein." — `Szenario` und `Bezugsjahr` an `Tab_Klimaregion(_STAMM)` (Schemaschritt 97), angezeigt in der Regionsliste **und** bei der Klimaregion auf der Übersicht. |

## 11. Nachweise

- `EPOS.Kern.Tests/LambertKoordinatenTests` — 30 Tests: die fünfzehn Regionsmittelpunkte
  gegen ihre Dateinamen, der Ursprung auf falschem Ost-/Nordwert, Hin- und Rückrechnung an
  fünf Punkten unter 1e-9°, der Punkt der Importprobe, die Plausibilitätsgrenzen.
- `EPOS.Kern.Tests/DwdTryLeserTests` — 32 Tests: Kopferkennung, Feld- und Zeilenzahl,
  Zeitfelder, doppelte Stunde, die Drehung MEZ → UTC, Zeitstempel, Direkt-Normal mit
  Mindesthöhe und Klemme, Erhalt der Globalstrahlung, der Standort aus dem Kopf und
  `KopfLesen` gegen den vollen Leser.
- `EPOS.Kern.Tests/TryPaketLeserTests` — 17 Tests, darunter der Bereichsabruf gegen ein
  Paket im Speicher: **528 402 Byte in drei Abrufen bei einem 8,9-MB-Paket**; fehlender
  Bereichsabruf, fehlendes Szenario, Ort außerhalb der 300-km-Grenze.
- `KatalogpflegeTests` und `KlimadatenDialogTests` — Quellenwahl, Pflichtangaben je Quelle,
  Herkunftsvermerk; dazu der Standort aus dem Kopf, der Vorrang der Anwendereingabe, der
  Fall ohne jeden Standort und die Vorbelegung im Dialog samt Überschreiben. Für die
  Bedienung (Abschnitt 2): die eine Fußleiste mit ihren Knöpfen in der Reihenfolge, die
  knopffreie Liste, die Dateizeile in EINER Zeile, die freibleibenden Felder nach einem
  Erfolg, die gemeldete Dublette, die sieben Spalten ohne Vergleichsknopf, die Wahl über
  einen Filterwechsel und 150 Zeilen an der Virtualisierungsschwelle.
- `EPOS.Kern.Tests/KlimaregionKatalogTests` — die sieben Spalten, die Sortierung nach Namen,
  der Schreibschutz als Kennzeichen und als Spalte, `Standorttext` gegen drei echte
  `Details`-Muster, der leere Altbestand, die ISO-Sortierung und der Fall ohne die zwei
  Spalten aus Schemaschritt 95.
- `EPOS.UI.Tests/Standards/SuchauswahlTests` — Tippfilter, Leerfall, Wahl als Id,
  Tastaturführung, Schließfläche, gesperrtes Feld, Berührungsziel und die gedeckelte
  Vorschlagszahl; `StartseiteTests` für die durchsuchbare Klimawahl und die Herkunftszeile
  samt Kurzform und dem Fall ohne Gaben.
- `Proben/Rasterprobe` vor **und** nach jeder Änderung an der Katalogliste — alle neun Fälle
  erfüllen die Sollwerte (Rückgabe 0); `bunit` misst weder Layout noch die
  Sichtbarkeitsmelder.
- `EinstellungenDialogTests` — 31 Tests, beide Kulturen.
- Importprobe `Referenzlaeufe/Importproben/dwd_try_synthetisch_72h.dat` — eine
  **synthetische** Probe im DWD-Format, ausdrücklich keine amtlichen Daten; der Leser hat für
  sie die Prüfoption „volles Jahr aus". Ihr Kopf steht auf einem stimmigen Punkt:
  Rechtswert 3 929 310 / Hochwert 2 478 193 ergeben 9,0000° O / 49,0000° N.
- `KlimaSzenarioTests` — Schemaschritt 97: die vier Spalten und ihre Wiederholbarkeit, der
  NULL gebliebene Bestand, Szenario **und Bezugsjahr** aus dem Dateikopf samt Gegenproben
  (fehlender und fremder Bezugszeitraum), die Projektkopie, der zusammengesetzte Satz der
  Liste, der Halbgeviertstrich des Altbestands, eine Datenbank auf Stand 95 und der Satzbau
  in beiden Kulturen.
- Referenzlauf der fünf Projekte byte-gleich gegen die geltende Basis; der PVGIS-Weg bleibt
  unverändert.

## 12. Offene Punkte

1. **Sommerstunden einer TRY-Reihe liegen beim Lesen eine Stunde früher** (Abschnitt 4). Die
   Quellenkennung an der Region steht jetzt (`Tab_Klimaregion.Quelle`); das quellenbewusste
   LESEN fehlt noch — kein Leser wertet sie bisher aus.
2. **`E` (langwellige Ausstrahlung) wird nicht gespeichert** (Abschnitt 6) — sie hätte keinen
   Leser. Sie kommt, wenn der Rechenweg sie braucht.
3. **Nach Szenario und Jahr wird über die Spalte „Quelle" gefiltert, nicht sortiert**
   (Abschnitt 2.1). Zwei eigene Spalten wären der andere Weg; sie kosten Breite in einem
   ohnehin schmalen Dialog.
