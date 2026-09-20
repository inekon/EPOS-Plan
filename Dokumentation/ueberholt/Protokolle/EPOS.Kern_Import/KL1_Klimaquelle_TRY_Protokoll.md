# KL1 — Klimaquelle DWD-Testreferenzjahr (TRY): Umsetzungsprotokoll

Stand: 20.09.2026 · Zweig `ios_migration_september` · Bezug:
[`Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md`](../../../aktuell/Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md)
(der gültige Stand; dieses Protokoll ist die Geschichte dazu).

## 1. Auftrag

Anwender, 19.09.2026, im Wortlaut: „Import von Klimadaten analog zu tmy-Daten mit try Daten.
Finde die Dokumentation dazu.“ und „Erstelle dazu eine Konfiguration im Administrations-Menü,
erstelle eine neue Rubrik Klimadaten 1. bestehende API für TMY 2. neue für try“ (mit Bildschirmbild
des Dialogs „Administration – Globale Anwendungseinstellungen“). Daraus die fünf Fragen KL-Q1 bis KL-Q5 an den Anwender und, nach deren
Beantwortung, drei Pakete: **KL1-A** (Rubrik „Klimadaten" in der Administration),
**KL1-B** (TRY-Import), **KL1-C** (Papiere, Wiki, Status).

## 2. Entscheide des Anwenders (19.09.2026)

| Kennung | Frage | Entscheid |
|---|---|---|
| **KL-Q1** | Wie verhält sich das zu Entscheid **E5** vom 15.09.2026? | (a) **E5 wird fortgeschrieben:** Datenträger der Richtlinien bleiben abgelehnt, DWD-TRY ist zweite Importquelle neben PVGIS-TMY. Klimabasis ist je Klimaregion das, was importiert wurde. |
| **KL-Q2** | Eigene Datei, offene Regionaldaten oder beides? | **Beides** — TRY-Datei aus dem DWD-Klimaberatungsmodul (Konto, kein REST-API) und die offenen Regionaldaten (`data.zip`, 15 Regionen, 2015/2045, mittleres/sommerwarmes/winterkaltes Jahr). |
| **KL-Q3** | Schemaschritt für die zusätzlichen Größen? | (a) **Kein Schemaschritt.** Temperatur, Global/Direkt/Diffus und die Flächenwerte werden übernommen; `p WR WG N x RF A E IL` werden benannt verworfen — Kandidat eines späteren Schritts zusammen mit dem Bedeckungsgrad der Gebäudesimulation. |
| **KL-Q4** | Jahr und Szenario fest oder wählbar? | **Wählbar**, Vorgabe 2015, mittleres Jahr. |
| **KL-Q5** | Wiki: eigene Seite oder Abschnitt? | **Eigene Seite** „Programm Dokumentation/Klimadaten". |

## 3. Befund vor der Umsetzung

- **Kein offenes API beim DWD.** Die Testreferenzjahre kommen aus dem Klimaberatungsmodul
  (`https://kunden.dwd.de/obt/`) und setzen ein Konto voraus; ein maschineller Abruf ist
  nicht vorgesehen. Der Import muss deshalb die **Datei** lesen können, die der Anwender
  dort zieht.
- **Offene Regionaldaten gibt es.** Das Paket `RE-Lab-Projects/TRY_DE_2015_2045` (`data.zip`,
  CC BY 4.0, Rohdaten DWD) führt 15 Regionen, zwei Bezugsjahre und drei Szenarien im
  DWD-Originalformat; die Koordinaten der Bezugsstation stehen im Dateinamen. Das Paket ist
  rund 892 MB groß — ein Volldownload schied aus.
- **Entscheid E5** (15.09.2026) sagte bis dahin „keine Datenträger, keine DWD-TRY". Er
  brauchte eine Fortschreibung, keinen Widerruf: Abgelehnt waren die **Datenträger der
  Richtlinien**; ein Importweg für Klimareihen ist etwas anderes.
- **Der Aufsetzpunkt war günstig.** `KlimaImportAblauf` trennte Abruf, Rechnen und Schreiben
  bereits sauber; eine zweite Quelle musste nur an die Stelle des Netzabrufs treten und
  dieselbe `TmyHourlyData`-Liste liefern. `Rechnen`, `GetDailyAverages`, `Tagtypen` und
  `SaveTmyData` blieben unverändert.

## 4. Umsetzung

### KL1-A — Rubrik „Klimadaten" in den globalen Einstellungen

Commits `2f7cd1af`, Merge `b6e40d1b`.

Fünf Reiter **VDI | DATENBANK | WEB | KLIMA | ANWENDUNG**. Die Klimarubrik trägt die
PVGIS-Adresse (aus „Web-Schnittstellen" hierher verschoben), `TRYPortalUrl` (Werksvorgabe
`https://kunden.dwd.de/obt/`) und `TRYRegionalUrl` (Werksvorgabe: Veröffentlichungsadresse
des Pakets v1.4.0). Werksvorgaben in `Settings.settings`, `Settings.Designer.cs` und
`app.config`; `Einstellungensatz.TryPortalUrl`/`TryRegionalUrl`; Ressourcen
`ADM_SET_RUBRIK_KLIMA`, `ADM_SET_LBL_TRY_PORTAL`, `ADM_SET_LBL_TRY_REGIONAL`.

### KL1-B — Der TRY-Import

Commits `39623b13`, `cc6fe2db`, Merge `600cae1b`, Merge in den Zweig `8fb71023`.

- **`EPOS.Kern/Allgemein/Import/DwdTryLeser.cs`** (neu) — Kopf bis `***`, 8 760 Zeilen,
  17 Spalten; Drehung MEZ → UTC; `DirektNormal` mit Mindesthöhe 5° und Klemme 1367 W/m²;
  Prüfoption `vollesJahr` für die synthetische Probe.
- **`TryPaketLeser.cs`** (neu) — das Zentralverzeichnis des ZIP ist die Regionsliste,
  Koordinaten aus den zwölf Ziffern des Dateinamens, Haversine, Grenze 300 km;
  `BereichStream` mit 256-KiB-Blöcken, ohne `Content-Range` benannte Ablehnung
  (`KLIMA_TRY_KEIN_BEREICH`), nie ein Volldownload; lokale `data.zip` als Rückfall.
- **`KlimaImportAblauf.cs`** — `KlimaQuelle`, `TrySzenario`, Auftragsfelder `TryPfad`,
  `TryPaketPfad`, `Szenario`, `TryJahr`; `Laufen(…, INetzbereich bereich = null)`; neuer
  Ausgang `Dateifehler` **am Ende** der Aufzählung, damit sich keine Zahlenwerte verschieben;
  Adresse aus `Dienste.Einstellungen.Lies("TRYRegionalUrl")`.
- **`SolarPVGISCalculator.cs`** — allein die neue Methode `Sonnenhoehe`.
- **`KlimadatenDialog.razor`** — Gruppe „Klimaquelle" über „Standort", Jahr, Szenario,
  Dateiwahl `.dat` bzw. `data.zip`. Eine TRY-Datei braucht Pfad **und** Standort, weil ihr
  Kopf nur Lambert-Koordinaten führt.
- **`KlimadatenHuelle.cs`** — Bereichsabruf mit absoluten Grenzen, ein Delegat
  `DateiWaehlen(filter)`.
- **Herkunft und Lizenz** („CC BY 4.0, RE-Lab-Projects/TRY_DE_2015_2045, Rohdaten Deutscher
  Wetterdienst") samt den verworfenen Größen in `Tab_Klimaregion_STAMM.Details` und in der
  Importmeldung. 33 Ressourcenschlüssel `KLIMA_*`, beide Sprachen.
- **Importprobe** `Referenzlaeufe/Importproben/dwd_try_synthetisch_72h.dat` — synthetisch,
  ausdrücklich keine amtlichen Daten.

### KL1-C — Papiere, Wiki, Status

Dieses Paket: neues Konzept
[`Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md`](../../../aktuell/Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md)
mit Indexzeile, Fortschreibung von **E5** an fünf Stellen (Konzept Gebäudesimulation N1.10,
Systementwurf, Statusdatei Gebäudesimulation, Kühlkonzept B-K1, Befund B Abschnitt 3),
Querverweis im PV-Konzept beim Befund B1, dieses Protokoll, Statuszeile **#367**, die
Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Klimadaten.wiki` und der
Logbuch-Entwurf (Abschnitt 8).

## 5. Nachweise

| Prüfstand | Ergebnis |
|---|---|
| `DwdTryLeserTests` | 18 grün |
| `TryPaketLeserTests` | 17 grün; Bereichsabruf 528 402 Byte in 3 Abrufen bei einem 8,9-MB-Paket |
| `KatalogpflegeTests` | +5 grün |
| `KlimadatenDialogTests` | +8 grün |
| `EinstellungenDialogTests` | 31 grün, beide Kulturen |
| Volle Suite | 9 140 grün nach KL1-A, 9 188 grün nach KL1-B |
| Referenzlauf | fünf Projekte byte-gleich gegen R9 |
| Windows-Schale | 0 Fehler |
| SQL-Dialekt-Prüfer | 0 Fundstellen |

## 6. Windows-Abnahmepunkte

**KL1-A (Einstellungen):**

- **A-KL1a-1** — fünf Rubriken in der Folge VDI, Datenbank, Web, Klima, Anwendung;
  „Web-Schnittstellen" führt nur noch Wiki und Geokodierung.
- **A-KL1a-2** — die Klimarubrik zeigt drei gefüllte Adressen, ohne Durchsuchen-Knopf.
- **A-KL1a-3** — ändern, speichern, neu öffnen; „Standardwerte" setzt die drei Klimaadressen
  auf die Werksvorgaben, übernommen wird erst mit „Speichern".

**KL1-B (Import):**

- **A-KL1b-1** — die Gruppe „Klimaquelle" steht über „Standort", PVGIS ist vorgewählt, der
  PVGIS-Import läuft wie zuvor.
- **A-KL1b-2** — TRY-Datei: Dateiwähler auf `*.dat`; eine echte DWD-Datei mit Standort ergibt
  eine Region mit 8 760 Stunden und 365 Tagen, die Details nennen Dateiname, Datum, Lizenz
  und die verworfenen Größen. Eine beschnittene Datei ergibt eine benannte Meldung mit
  Zeilennummer und **keine** Region.
- **A-KL1b-3** — Regionaldaten über die Adresse: übertragene Datenmenge deutlich unter 2 MB,
  Meldung mit Region, Koordinate, Entfernung, Szenario und Jahr. Eine Adresse ohne
  Bereichsabruf ergibt den Hinweis, `data.zip` herunterzuladen. Ein Ort außerhalb (Madrid)
  bricht mit Entfernungsangabe ab.
- **A-KL1b-4** — lokale `data.zip`, 2045 winterkalt ergibt `TRY2045_…_Wint.dat`, die
  Diagramme sind plausibel, eine vorhandene PVGIS-Region bleibt unverändert.

## 7. Offene Punkte

1. **Keine Regionsanzeige vor dem Import** — welche Region getroffen wird, sagt erst die
   Meldung danach; eine Vorschau kostete einen zweiten Netzabruf.
2. **Lambert → WGS 84** — erledigt mit dem Nachtrag KL-2 (Abschnitt 9).
3. **Sommerstunden einer TRY-Reihe liegen beim Lesen eine Stunde früher.** Bewusste
   Festlegung; anders nur mit einer Quellenkennung an der Region, also einem Schemaschritt.
4. **`N`, `A`, `E` werden nicht gespeichert** — Kandidat desselben Schemaschritts wie der
   Bedeckungsgrad der Gebäudesimulation.
5. **`user.config` einer Bestandsinstallation** kennt die zwei TRY-Schlüssel nicht; die
   Werksvorgabe greift — beim Windows-Test zu sehen.
6. **Wiki-Upload steht aus** — die Seite „Programm Dokumentation/Klimadaten" geht mit dem
   nächsten Sammel-Upload live; die Versionsnummer für das Logbuch ist beim Anwender zu
   erfragen.

## 8. Logbuch-Entwurf

Version 1.2.0.2, ein Satz:

> Klimaregionen lassen sich aus den PVGIS-Daten oder aus einem DWD-Testreferenzjahr anlegen —
> aus einer eigenen Datei oder aus den offenen Regionaldaten.

## 9. Nachtrag KL-2 — Standort aus dem Dateikopf

### 9.1 Auftrag

Anwender, 19.09.2026, im Wortlaut: „TRY-Datei braucht den Standort zusätzlich im Dialog:
In Dialog aufnehmen und entsprechende Umrechnung in Lambert-Koordinaten". Damit ist der
offene Punkt 2 dieses Protokolls beantwortet: Der Standort einer TRY-Datei kommt aus ihrem
Kopf, nicht mehr von Hand.

### 9.2 Die Projektion — gemessen, nicht geraten

Der Wertebereich der Kopfangaben (Rechtswert rund 3 670 500…4 389 500, Hochwert rund
2 242 500…3 179 500) deutet auf **ETRS89 / LCC Europa, `EPSG:3034`**. Geprüft wurde das an
den offenen Regionaldaten (`RE-Lab-Projects/TRY_DE_2015_2045`, `data.zip` v1.4.0): Deren
fünfzehn Regionsdateien tragen Breite und Länge im DATEINAMEN
(`TRY2015_<Breite·1e4><Länge·1e4>_Jahr.dat`) und Rechts-/Hochwert im KOPF — zwei unabhängige
Angaben desselben Punktes. Die Köpfe wurden mit HTTP-Bereichsabrufen über das
Zentralverzeichnis des Archivs gelesen (kein Volldownload) und gegen die Dateinamen
gerechnet:

| Region | Rechtswert | Hochwert | gerechnet (Länge / Breite) | Dateiname | Abweichung |
|---|---|---|---|---|---|
| 1 | 3 909 500 | 2 968 500 | 8,587223 / 53,559070 | 8,5872 / 53,5591 | +0,000023 / −0,000030 |
| 3 | 4 000 500 | 2 964 500 | 10,007800 / 53,529934 | 10,0078 / 53,5299 | −0,000000 / +0,000034 |
| 4 | 4 201 500 | 2 846 500 | 13,065145 / 52,393763 | 13,0651 / 52,3938 | +0,000045 / −0,000037 |
| 10 | 4 131 500 | 2 621 500 | 11,912402 / 50,322649 | 11,9124 / 50,3226 | +0,000002 / +0,000049 |
| 15 | 4 080 500 | 2 316 500 | 11,104566 / 47,494499 | 11,1046 / 47,4945 | −0,000034 / −0,000001 |

Über **alle fünfzehn** Regionen beträgt die größte Abweichung **0,000049°** — zwei
Größenordnungen unter der Abnahmegrenze 0,001°. Der Rest ist die Rundung der Kopfwerte auf
das 500-m-Raster; rückwärts gerechnet liegen die Meterwerte um wenige Meter neben dem
Kopfwert. Die fünfzehn Paare stehen als feste Fälle in `LambertKoordinatenTests`; der Test
selbst geht nie ins Netz.

### 9.3 Umsetzung

- **`EPOS.Kern/Allgemein/Import/LambertKoordinaten.cs`** (neu) — `NachGeographisch` und
  `NachLambert`, ellipsoidisch nach Snyder (*Map Projections — A Working Manual*, USGS
  Professional Paper 1395, 1987, Abschnitt 15), alle Parameter als benannte Konstanten mit
  Herkunft im Kommentar. Außerhalb 45…56° N / 5…16° O: `false` und `NaN`, keine stille
  Null. Der Bereichsrand beträgt 1e-9° (rund 0,1 mm), damit ein Punkt genau auf einer
  Grenze die Hin- und Rückrechnung übersteht.
- **`DwdTryLeser`** — `TryKopf.Laenge`/`.Breite` (`double?`) und `StandortBekannt`; neu
  `KopfLesen(string, out TryKopf)` und `KopfLesen(IEnumerable<string>)`, die an der
  Trennzeile aufhören; `Meterwert` liest die Zahl vor der Einheit invariant.
- **`KlimaImportAblauf`** — im Zweig `TryDatei` gilt der Auftrag; bringt er keine
  Koordinaten, kommt der Standort aus dem Kopf. Meldung und `Details` nennen die Herkunft
  benannt. Weder Auftrag noch Kopf: `Eingabefehler`, keine Region bei 0°/0°.
- **`KlimadatenDialog`** — `BeiTryPfad` ruft `KopfLesen` (kein Netz, keine Datenbank) und
  belegt Longitude, Latitude und, falls leer, die Bezeichnung aus dem Dateinamen vor;
  darunter eine `Herleitungszeile` mit Rechts- und Hochwert und dem Zusatz „änderbar".
  Schreibt der Anwender selbst in eines der Zahlenfelder, fällt die Zeile weg. Nicht
  lesbarer Kopf: Hinweis, Felder bleiben, `ImportErlaubt` unverändert.
- **Ressourcen** — fünf Schlüssel `KLIMA_TRY_STANDORT_KOPF`, `…_ANWENDER`, `…_FEHLT`,
  `…_HINWEIS`, `…_UNLESBAR` in beiden `.resx`, Designer neu erzeugt.
- **Importprobe** — Kopfzeilen 6 und 7 auf einen stimmigen Punkt gesetzt: Rechtswert
  3 929 310 / Hochwert 2 478 193 ergeben 9,0000° O / 49,0000° N (mit `NachLambert`
  gerechnet); dieselben Werte stehen in den 72 Datenzeilen.

**Kein Schemaschritt.**

### 9.4 Nachweise

| Prüfstand | Ergebnis |
|---|---|
| `LambertKoordinatenTests` | 30 grün (fünfzehn Regionen, Ursprung, Rundlauf < 1e-9°, Punkt der Importprobe, Grenzen) |
| `DwdTryLeserTests` | 32 grün |
| `KatalogpflegeTests` | 117 grün (drei neue: Standort aus dem Kopf, Vorrang des Anwenders, ohne jeden Standort) |
| `KlimadatenDialogTests` | 28 grün (drei neue: Vorbelegung, Überschreiben, Kopf ohne Standort) |
| Volle Suite | grün, beide Kulturen |
| Referenzlauf | fünf Projekte byte-gleich gegen R9 |
| Windows-Schale | 0 Fehler |

### 9.5 Windows-Abnahmepunkte

- **A-KL2-1** — Quelle „Testreferenzjahr (Datei)", eine echte DWD-Datei wählen: Längen-
  und Breitengrad und die Bezeichnung sind gefüllt, darunter steht die Herkunftszeile mit
  Rechts- und Hochwert; „Daten einlesen" läuft ohne weitere Eingabe durch, die Details
  nennen „Standort aus dem Dateikopf …".
- **A-KL2-2** — dieselbe Datei, Längen- und Breitengrad von Hand überschreiben: Die
  Herkunftszeile verschwindet, gerechnet wird mit der Eingabe, die Details nennen
  „Standort vom Anwender".

## 10. Nachtrag KL-3 — Schemaschritt 95: Klimaspalten und Regionsvorschau

### 10.1 Auftrag

Anwenderentscheid vom 19.09.2026, Block „Nach #367":

- **(e)** „Alles Relevante für die Gebäudesimulation aufnehmen" — ein Schemaschritt mit
  `Gegenstrahlung`, `Luftfeuchte` und `Bedeckungsgrad` in `Tab_Solar` **und**
  `Tab_Solar_STAMM`. Windgeschwindigkeit **nicht**: keine Spalte ohne Leser
  (Umsetzungskonzept Gebäudesimulation F-S3). Das ist der Schritt **M4** jenes Papiers,
  erweitert um den Bedeckungsgrad, weil TRY ihn echt liefert (Spalte `N`).
- **(b)** Die Regionsvorschau vor dem Import ist gewünscht; der zweite Netzabruf ist
  angenommen.
- Dazu der Anwenderauftrag KL-4: `Quelle` und `Importdatum` an `Tab_Klimaregion` und
  `Tab_Klimaregion_STAMM` — im **selben** Schritt.

### 10.2 Umsetzung

**Schemaschritt 95** — zehn nullbare Spalten an vier Tabellen, **kein DML**. Die eine
Quelle ist `SchemaKatalog.Schritt95_Klimaspalten`; daraus bedienen sich die drei Leser
`SchemaMigration.Schritt_95_Klimaspalten`, `Werkzeuge/Testdatenbankschema` und
`EPOS.Kern.Tests/TestDatenbank`. `SchemaStand.Zielversion` steht zuletzt auf **95**.

| Tabelle | Spalte | Typ | NULL heißt |
|---|---|---|---|
| `Tab_Solar`, `Tab_Solar_STAMM` | `Gegenstrahlung` | `REAL` | nicht verfügbar |
| `Tab_Solar`, `Tab_Solar_STAMM` | `Luftfeuchte` | `REAL` | nicht verfügbar |
| `Tab_Solar`, `Tab_Solar_STAMM` | `Bedeckungsgrad` | `REAL` | nicht verfügbar (jede PVGIS-Region) |
| `Tab_Klimaregion`, `Tab_Klimaregion_STAMM` | `Quelle` | `TEXT` | Altbestand |
| `Tab_Klimaregion`, `Tab_Klimaregion_STAMM` | `Importdatum` | `TEXT` (ISO) | Altbestand |

**Die Befüllung.**

- **PVGIS** — `TmyHourlyData` bekommt `[JsonPropertyName("IR(h)")] Gegenstrahlung` und
  `Bedeckungsgrad`; `Humidity` wird **nullbar** (fehlt `RH`, steht NULL statt 0).
  `SaveTmyData` schreibt die drei Spalten in die STUNDENreihe — die Tageswerte
  (`Tab_Klimadaten*`) führen sie nicht.
- **TRY** — `DwdTryLeser` liest `A` → Gegenstrahlung, `RF` → Luftfeuchte, `N` →
  Bedeckungsgrad; alle drei als Pflichtfelder der Datenzeile, mit benannter Meldung samt
  Zeilennummer, wenn sie keine Zahl sind. Die Verworfen-Liste schrumpft von neun auf sechs:
  `p, WR, WG, x, E, IL`. Die Drehung MEZ → UTC gilt für alle Größen gleich, weil sie an
  derselben Zeile hängen.
- **Herkunft** — `KlimaregionStammCtrl.Add` bekommt eine Überladung mit `quelle` und
  `importdatum`; `KlimaImportAblauf` setzt beide bei **allen drei** Quellen an derselben
  Stelle. Der Freitext `Details` bleibt unverändert daneben.
- **Projektkopie** — `KlimaregionStammCtrl.CopyRegionToProjekt` kopiert die drei
  Klimagrößen je Stunde und `Quelle`/`Importdatum` am Kopfsatz mit.
- **Toter Bestand entfernt** — `SolardatenCtrl.Insert` und `WriteDataTable` (beide ohne
  Aufrufer, beide schrieben nur `Temperatur`) sind gelöscht; `MapDataRowToModel` liest die
  drei Größen NULLBAR, und NULL bleibt NULL.

**Regionsvorschau.** `TryPaketLeser.RegionErmitteln` liest **allein das
Zentralverzeichnis** und gibt Region, Stationskoordinate, Entfernung und die vorhandenen
Jahr/Szenario-Kombinationen zurück — ohne einen Eintrag zu entpacken; die Regionswahl ist
dieselbe Funktion, die der Import ruft (`Regionswahl`, eine Funktion, zwei Aufrufer).
`KlimaImportAblauf.RegionErmittelnAsync` legt die Geokodierung darüber. Im Dialog steht im
Regionaldaten-Zweig der Knopf „Region ermitteln" (frei, sobald ein Ortsname **oder** ein
Koordinatenpaar da ist — eine Bezeichnung braucht die Vorschau nicht) und darunter eine
Herleitungszeile: „Region 14 Stötten, Station 48,6600 / 9,8600, Entfernung 12 km · vorhanden:
2015 mittleres Jahr, …" bzw. der benannte Fehler. Jede Standort- oder Quellenänderung
verwirft die Zeile. Die Region wird mit **Namen** genannt (`TryRegionsnamen`, 1 Bremerhaven
… 15 Garmisch-Partenkirchen; reine Anzeige, die Zuordnung bleibt der nächste
Stationsmittelpunkt) — auch im Herkunftsvermerk.

**Ressourcen** — vier Schlüssel `KLIMA_TRY_BTN_REGION`, `KLIMA_TRY_VORSCHAU`,
`KLIMA_TRY_VORSCHAU_BESTAND`, `KLIMA_TRY_VORSCHAU_LAEUFT` in beiden `.resx`, Designer neu
erzeugt.

**Testdatenbank** — `Referenzlaeufe/Kenndaten_Test.sqlite` mit
`Werkzeuge/Testdatenbankschema` auf Stand 95 gezogen: zehn Spalten angelegt,
`integrity_check` = ok, alle zehn in jeder Zeile NULL (280 320 Stundenwerte, 32 Regionen).

### 10.3 Nachweise

| Prüfstand | Ergebnis |
|---|---|
| `KlimaspaltenTests` (neu) | 9 grün — Spaltenliste, Wiederholbarkeit, Bestand leer, PVGIS-, TRY- und Kopiefall, Altbestand, Leser |
| `DwdTryLeserTests` | 34 grün (zwei neue: Zuordnung der drei Größen, Drehung) |
| `TryPaketLeserTests` | 31 grün (sieben neue: Vorschau gegen Import, Ausführungen, kein Eintragsbyte, benannte Ablehnungen, lokale Datei, Stationsnamen) |
| `KlimadatenDialogTests` | 34 grün (sechs neue: Knopf nur im Regionaldaten-Zweig, Freigabe, ohne Delegat, Ergebniszeile, Fehlerzeile, Verwerfen) |
| Volle Suite | grün, beide Kulturen |
| SQL-Dialekt-Prüfer | 1 505 Texte, 0 Fundstellen |
| Referenzlauf | fünf Projekte byte-gleich gegen R9 |
| Windows-Schale | 0 Fehler (Linux-Bau mit `EnableWindowsTargeting`) |

### 10.4 Windows-Abnahmepunkte

- **A-KL3-1** — Bestandsdatenbank: Der Programmstart migriert auf Stand 95; in
  `Tab_Solar(_STAMM)` stehen die drei neuen Spalten und sind in jeder Bestandszeile leer.
- **A-KL3-2** — PVGIS-Import einer neuen Region: `Gegenstrahlung` und `Luftfeuchte` sind je
  Stunde gefüllt, `Bedeckungsgrad` bleibt leer.
- **A-KL3-3** — Import einer DWD-TRY-Datei: alle drei Spalten gefüllt; die Meldung nennt als
  nicht übernommen nur noch `p, WR, WG, x, E, IL`.
- **A-KL3-4** — Regionaldaten, „Region ermitteln": Die Zeile nennt Region mit Namen, Station
  und Entfernung, bevor eingelesen wird; außerhalb Deutschlands steht dort der benannte
  Fehler, und „Daten einlesen" bleibt frei.
- **A-KL3-5** — Details eines neuen Imports: `Quelle` und `Importdatum` sind gesetzt, bei
  einer Altbestandsregion leer (in der Oberfläche sichtbar erst mit KL-4).

## 11. Nachtrag KL-4 — Bedienung der Klimadaten

### 11.1 Auftrag

Anwenderwunsch vom 19.09.2026, im Wortlaut, zu fünf Bildschirmbildern:

1. „Der Button Beenden, Durchsuchen, Löschen, Daten Einlesen sollen besser platziert
   werden."
2. „‚Daten Einlesen' verschwindet nach dem Einlesen."
3. „Bei der Auswahl der Klimadaten soll das gleiche Schema (Filter, Sortieren …) verwendet
   werden [wie bei ‚Module in Datenbank']. Die Quelle und Bezeichnung der Klimadaten soll
   mit angezeigt werden."
4. „Die verwendeten Klimadaten sollen sich auch auf der Übersicht befinden. Das Dropdown
   soll auch durchsuchbar sein."

### 11.2 Befund

**Zu 1.** Der Dialog hatte ZWEI Knopfzeilen: eine listenlokale `.epos-leiste` am Ende der
Listenspalte mit „Löschen" und die Schlussleiste „Daten einlesen · Füller · Beenden"
außerhalb des Rahmens. Zwischen der listenlokalen Leiste und der Reiterleiste daneben stand
nur der Rasterabstand — bei schmalem Fenster stießen sie aneinander. „Durchsuchen" gehörte
nie in eine Fußleiste: Es ist der Knopf des Bausteins `Dateiwahl` und gehört neben sein
Feld.

**Die Ursache, dass er dort nicht stand:** Die zwei Eingabeblöcke standen in einem nackten
`div.epos-klimaregion-eingabe` — einer Klasse, die im Stilblatt **nie angekommen** ist
(derselbe Befund wie bei `epos-klimaregion-mitte/-liste/-bilder`). Ohne Regel stand jedes
Feld über die volle Breite, die Beschriftung darüber und der Knopf der `Dateiwahl` unter
seinem Feld. Dasselbe galt für `epos-feld-beschriftung` am Ortsfeld — die Klasse des Hauses
heißt `epos-feld-text`.

**Zu 2.** Der Knopf war **nie weg**. Er steht unverändert im Markup und trägt
`disabled="@(!ImportErlaubt)"`. `ImportErlaubt` verlangt `StandortSteht` — einen Ortsnamen
ODER Longitude, Latitude und Bezeichnung —, und der Erfolgsfall in `BeiImport` leerte
`_ortsname` und `_bezeichnung`. Damit hatte der Dialog nach jedem erfolgreichen Import
keinen Standort mehr, und der Knopf war gesperrt. Aus Sicht des Anwenders: verschwunden.

**Zu 3.** Die Regionsliste war ein nacktes `Raster` mit zwei Spalten über einem
`record Regionszeile(string Name, bool NurLesen)` — kein Suchfeld, kein Trichter, kein
Sortierpfeil, keine Trefferzahl, und vor allem: nicht die Quelle und nicht der Standort,
nach denen der Anwender sucht. Die Spalten `Quelle` und `Importdatum` gab es seit
Schemaschritt 95 (KL-3), aber keinen Leser dafür.

**Zu 4.** Die Startseite trug ein natives `<select>` über KLARNAMEN — aufklappen und rollen,
und gespeichert wurde ein Text. Einen durchsuchbaren Auswahlbaustein gab es im Haus nicht:
`Standards/Auswahlfeld` ist Id-basiert, aber ohne Suche.

### 11.3 Umsetzung

**Paket A — der Dialog.**

- **Eine Fußleiste** außerhalb des `Katalograhmen`: `Daten einlesen · Füller · Löschen ·
  Beenden`, Beenden primär — Hausmuster `ModulKatalogDialog`. Die listenlokale Leiste
  entfällt; `LoeschenErlaubt` und die Rückfrage bleiben unverändert.
- Die Blöcke Klimaquelle und Standort stehen im `Formularraster Einspaltig`; das „oder" ist
  eine `Formulargruppe` (leise Zwischenüberschrift über die volle Breite). Damit trägt die
  `Dateiwahl` Beschriftung, Feld und „Durchsuchen" in einer Zeile
  (`.epos-dateiwahl > .epos-knopf`) — **ohne eine einzige neue CSS-Klasse**. Die toten
  Klassen `epos-klimaregion*` und `epos-feld-beschriftung` sind entfernt.
- **Import-Freigabe:** Die zwei Zuweisungen, die `_ortsname` und `_bezeichnung` leerten,
  sind gefallen; der Grund steht als Kommentar an der Stelle. Vor einem Doppelimport
  schützt der Dublettenschutz des Ablaufs (`GetStammId(…) > 0` →
  `KlimaImportAusgang.Dublette`, `KLIMA_MSG_SCHON_VORHANDEN`) — und der ist der richtige
  Ort dafür: Er kennt den Katalog, das Formular kennt nur seine Felder.
- **Die Regionsliste ist die `Katalogliste`** mit dem Profil
  `Katalogfilterprofil.AusSpalten("KLIMAREGION", …)` und sieben Spalten: `BEZEICHNER`,
  `QUELLE`, `STANDORT`, `LONGITUDE` und `LATITUDE` (Zahl, vier Nachkommastellen),
  `IMPORTDATUM` (Text — ISO sortiert als Zeichenkette in der Reihenfolge des Datums) und
  `SCHREIBSCHUTZ` (Ja/Nein). `Raster` und `Regionszeile` entfallen; die Wahl hängt
  unverändert am Bezeichner, der Schreibschutz an `Katalogfilterzeile.Geschuetzt`. Der
  Filterstand kommt aus `Katalogfilterregister.Stand("KLIMAREGION")`, Rückweg
  `Filterstandvorgabe`.
- **Neu im Kern** (`KlimaregionStammCtrl`):
  `Katalogfilterzeilen()` liest die acht Spalten mit `ORDER BY Name` und ist tolerant, wenn
  `Quelle` und `Importdatum` fehlen (Muster `KostenVorlagenCtrl.PflichtSpalteVorhanden`);
  `Standorttext(details, lon, lat)` nimmt das erste Glied von `Details` — aber nur, wenn es
  ein Ort ist und nicht der Name einer TRY-Quelle. Gemessen wird gegen die **Vorlagen
  selbst** (`KLIMA_TRY_DETAILS_DATEI` / `_REGIONAL`), in aktueller **und** neutraler Kultur:
  Der Vermerk ist in der Sprache geschrieben worden, die beim Import eingestellt war.
- Die **Quelle bleibt im Kern ihr Schlüssel** (`PVGIS`, `TRY_DATEI`, `TRY_REGIONAL`);
  `KlimadatenHuelle.RegionenLesen` setzt den Anzeigetext ein — dieselben drei Namen, die die
  Optionsgruppe „Klimaquelle" trägt.
- **`Katalogliste` bekommt `Vergleichbar`** (Vorgabe `true`): Wo die Zeilen keine Kennwerte
  tragen, fällt der Vergleichsknopf weg — und mit ihm das Markieren, das sonst ein
  unsichtbarer Zustand wäre. Strg-Klick wählt dann wie ein gewöhnlicher Klick.

**Paket B — die Startseite.**

- **Neuer Baustein `EPOS.UI/Standards/Suchauswahl.razor`**: eine Textzeile mit eigener
  Vorschlagsliste. Tippfilter über `Contains` (`ToUpperInvariant`, `Ordinal`), Tastatur ↑ ↓
  Enter Esc, Wahl als **Id**, `@key` an der Id, Einträge ≥ `--epos-touchziel`, höchstens 50
  gezeichnete Vorschläge, `forced-colors` beachtet. Geschlossen wird über eine
  **Schließfläche** (`position: fixed; inset: 0`, drei z-Ebenen wie beim Menüband), **nicht**
  über `focusout`: Das feuert auch bei einem Fokuswechsel innerhalb der Liste, und auf dem
  iPad setzt eine Berührung überhaupt keinen Fokus (Befund W16c-B13). Kein JS-Interop.
- **Warum kein `<datalist>`:** Es trägt keine Id (die Zuordnung Text → Id müsste die Seite
  raten, und zwei gleichnamige Regionen wären nicht unterscheidbar), es führt keine Tastatur
  (welche Taste öffnet, ob ↑ ↓ wandert, was Esc tut, entscheidet der Browser), und es sieht
  je Browser anders aus. Der Kopfkommentar der Datei sagt das.
- **Die drei Klimadelegaten der Startseite laufen über Ids**: `Klimaregionen` liefert
  `(Id, Text)`, `KlimaregionId` statt `Klimaregion`, `KlimaSpeichern` nimmt die Stamm-Id.
  Im Kern ist `KlimaregionSpeichern(idProjekt, projektname, stammRegionId)` **der** Ablauf;
  die Namensfassung schlägt nur die Id nach und ruft ihn — zwei Methoden desselben Inhalts
  driften (Lehre aus Befund W16a-B5).
- **Neu `StartseiteCtrl.KlimaHerkunft(idProjekt)`** → Record
  `KlimaHerkunft(Quelle, Bezeichner, Standort, Importdatum)`, gelesen aus der
  **Projektkopie** `Tab_Klimaregion` über `Tab_Projekt.ID_Klimaregion` — dieselbe Zeile, mit
  der der Rechenlauf arbeitet, und derselbe Weg wie `ProjektKlimazone`. Ein Griff in den
  Stammkatalog wäre der falsche Schlüsselraum (Befund W16b-B2).
- **Die Herkunftszeile** steht als zweite, leise Zeile im Klimakasten. Ohne Quelle und
  Importdatum (Altbestand) steht die **Kurzform** aus Bezeichner und Standort; ohne Gaben —
  kein Projekt offen, oder die Plattform liefert sie nicht (iOS, `StartseiteGaben` → `null`)
  — steht keine Zeile. Eine leere Zeile wäre eine Behauptung ohne Inhalt.
- **Hülle `StartseiteHuelle`**: übersetzt den Quellenschlüssel und schreibt das ISO-Datum
  kulturgerecht (`DateTime.TryParseExact`, sonst der Rohtext). `IProjektQuelle.StartseiteGaben`
  bleibt unverändert.

**Ressourcen** (beide Sprachen, Designer neu erzeugt): `KLIMA_SP_QUELLE`,
`KLIMA_SP_STANDORT`, `KLIMA_SP_LONGITUDE`, `KLIMA_SP_LATITUDE`, `KLIMA_SP_IMPORTDATUM`,
`KLIMA_SP_SCHREIBSCHUTZ`, `START_KLIMA_HERKUNFT`, `START_KLIMA_HERKUNFT_KURZ`,
`SUCHAUSWAHL_PLATZHALTER`, `SUCHAUSWAHL_KEIN_TREFFER`, `SUCHAUSWAHL_LISTE`. Die
`KLIMA_QUELLE_*` aus KL-1 werden wiederverwendet — ein zweiter Wortlaut für dieselbe Sache
wäre eine zweite Wahrheit.

**Eine Stilblattfalle:** Der CSS-Block der Suchauswahl steht **vor** dem
Formularraster-Abschnitt. Die Wache
`FormularrasterTests.Ausserhalb_des_Rasters_bleibt_ein_Feld_unveraendert` liest alles nach
dessen Kopf und verlangt dort `.epos-formularraster` in jeder Selektorzeile.

### 11.4 Nachweise

| Prüfstand | Ergebnis |
|---|---|
| `Proben/Rasterprobe` **vor** der Änderung | ALLE 9 FÄLLE ERFÜLLEN DIE SOLLWERTE (Rückgabe 0) |
| `Proben/Rasterprobe` **nach** der Änderung | ALLE 9 FÄLLE ERFÜLLEN DIE SOLLWERTE (Rückgabe 0) |
| `KlimaregionKatalogTests` (neu) | 8 grün — sieben Spalten, Sortierung, Schreibschutz, `Standorttext` gegen drei echte Muster, leerer Altbestand, ISO-Sortierung, Fall ohne die zwei Spalten |
| `SuchauswahlTests` (neu) | 14 grün — Tippfilter, Leerfall, Wahl als Id, ↑ ↓ Enter Esc, Schließfläche, gesperrt, Berührungsklasse, gedeckelte Vorschlagszahl |
| `KlimadatenDialogTests` | 41 grün (sieben neue: Fußleiste, freibleibende Felder, Dublette, sieben Spalten ohne Vergleich, Wahl über den Filterwechsel, Dateizeile, 150 Zeilen) |
| `KataloglisteTests` | 2 neue (Vorgabe `true`, `Vergleichbar="false"` samt Strg-Klick) |
| `StartseiteTests`, `HauptfensterTests`, `StartseiteCtrlTests` | grün; vier neue Startseitenfälle, vier neue Kernfälle, iOS-Fall ergänzt |
| Volle Suite | grün, beide Kulturen |
| SQL-Dialekt-Prüfer | 1 505 Texte, 0 Fundstellen |
| Referenzlauf | fünf Projekte byte-gleich gegen R9 |
| Windows-Schale | 0 Fehler (Linux-Bau mit `EnableWindowsTargeting`) |

### 11.5 Windows-Abnahmepunkte

- **A-KL4-1** — Klimadaten öffnen: Unter dem Rahmen steht EINE Leiste „Daten einlesen ·
  Löschen · Beenden" (Beenden hervorgehoben); die Liste trägt keinen Aktionsknopf, und bei
  schmalem Fenster überlappt nichts mehr.
- **A-KL4-2** — Quelle „Testreferenzjahr (Datei)": Beschriftung, Pfadfeld und „Durchsuchen"
  stehen in EINER Zeile; dasselbe beim Regionalpaket.
- **A-KL4-3** — PVGIS-Import über einen Ortsnamen: Nach der Erfolgsmeldung stehen Ortsname
  und Bezeichnung noch da, und „Daten einlesen" ist frei.
- **A-KL4-4** — Denselben Ort ein zweites Mal einlesen: Die Meldung sagt, dass es die Region
  schon gibt; die Liste bleibt, wie sie war.
- **A-KL4-5** — Die Regionsliste zeigt Suchfeld, Trefferzahl („n von m"), Trichter und
  Sortierpfeil in sieben Spalten — und KEINEN Vergleichsknopf; eine Suche nach „tüb" oder
  „2026-09" engt ein, die gewählte Zeile bleibt gewählt.
- **A-KL4-6** — Eine Auslieferungsregion löschen wird abgewiesen („schreibgeschützt"); eine
  eigene Region fragt zuerst und verschwindet nach „Ja" samt ihren Stunden- und Tageswerten.
- **A-KL4-7** — Übersicht: „tüb" führt auf Tübingen, ↑ ↓ und Enter wählen, Esc schließt die
  Liste ohne zu ändern, ein Klick daneben schließt sie ebenfalls; „Speichern" übernimmt.
- **A-KL4-8** — Unter dem Klimafeld steht „Klimadaten: … · … · … · Import …"; bei einer
  Region aus dem Altbestand die Kurzform ohne Quelle und Datum, ohne offenes Projekt keine
  Zeile.
- **A-KL4-9** — Sprachumschaltung auf Englisch: Spaltenköpfe, Platzhalter, „Kein Treffer."
  und die Herkunftszeile sind übersetzt.

## 12. Nachtrag KL-6 — Schemaschritt 97: Szenario und Bezugsjahr sichtbar

**Anwenderentscheid 19.09.2026:** „Wird die Quelle und Auswahl (z. B. TRY 2045 sommerwarm …)
der Klimadaten angezeigt? Diese sollte auch bei der Klimaregion sichtbar sein." — Entscheid:
Schemaschritt 97, angezeigt in der Regionsliste **und** bei der Klimaregion auf der Übersicht.

### 12.1 Befund

Szenario und Bezugsjahr standen ausschließlich im Freitext `Tab_Klimaregion_STAMM.Details`
(„… · Szenario mittleres Jahr · Bezugsjahr 2015 · …"), geschrieben beim Import der
Regionaldaten. Das ist ein Satz für den Leser: nicht sortierbar, nicht filterbar, in der
Sprache geschrieben, die beim Import eingestellt war — und bei einer TRY-Datei gar nicht
vorhanden. Wer wissen wollte, ob eine Region das mittlere Jahr 2015 oder das sommerwarme
2045 trägt, musste das Detailfeld lesen.

### 12.2 Schemaschritt 97

`Tab_Klimaregion` und `Tab_Klimaregion_STAMM` bekommen `Szenario` (`TEXT(12)`, Schlüssel
`MITTEL` | `SOMMERWARM` | `WINTERKALT`, nie ein Anzeigetext) und `Bezugsjahr` (`INTEGER`,
2015 oder 2045). Katalog und Projektkopie im selben Schritt — eine Spalte nur auf einer
Seite wäre in `CopyRegionToProjekt` sofort ein Datenverlust. **Kein DML:** Beide bleiben im
Bestand NULL, und NULL heißt „sagt nichts dazu".

Eine Quelle (`SchemaKatalog.Schritt97_KlimaSzenario`), drei Leser: `SchemaMigration`,
`Werkzeuge/Testdatenbankschema`, `EPOS.Kern.Tests/TestDatenbank`; `SchemaStand.Zielversion`
zuletzt auf 97. In der Migration steht der Schritt **nach** 96 (ein späterer `ADD COLUMN`
hängt sich an die neu gebaute Tabelle), im Werkzeug und in der Testkopie **vor** 96, damit
der Tabellenneubau die Spalten gleich mitnimmt und ein Lauf in einem Durchgang fertig ist.

### 12.3 Schreiben

| Quelle | Szenario | Bezugsjahr |
|---|---|---|
| PVGIS-TMY | NULL — PVGIS kennt keine TRY-Szenarien | NULL |
| TRY-Datei | aus der Kopfzeile „Art des TRY" (`TryKopf.Art`), tolerant gegen Schreibweise, Bindestrich und Zwischenraum; unlesbar oder fehlend → NULL | **NULL**, benannt: Der Kopf nennt einen Bezugs*zeitraum* („1995-2012"), nicht das Bezugsjahr dieses Hauses |
| TRY-Regionaldaten | aus dem Auftrag | aus dem Auftrag |

`KlimaregionStammCtrl.Add` hat dafür eine Überladung um beide Angaben; die bisherige
Sieben-Parameter-Fassung bleibt als Weiche stehen und schreibt NULL.

### 12.4 Anzeige

Der neue `KlimaAnzeige` (Kern) reiht Quelle, Bezugsjahr und Szenario zu **einem** Satz —
lang für die Regionsliste und die Herkunftszeile („TRY-Regionaldaten (Deutschland) · 2045 ·
sommerwarm"), kurz für einen Eintrag des Auswahlfeldes („hagelloch (TRY 2045 sommerwarm)",
„München (PVGIS)"). Was fehlt, wird weggelassen, nie durch eine Vorgabe ersetzt; fehlt
alles, bleibt die leere Zeichenfolge, und die Katalogliste macht daraus ihren
Halbgeviertstrich.

**Die beiden bisherigen Übersetzungen in den Hüllen sind entfallen.** Bis KL-6 setzte
`KlimadatenHuelle` bzw. `StartseiteHuelle` den Anzeigetext des Quellenschlüssels ein — aus
drei Angaben EINEN Satz zu bauen ist aber kein Übersetzen mehr, sondern Satzbau, und zwei
Stellen wären zwei Wortlaute. Der Kern baut ihn; die Datenbank führt unverändert nur
Schlüssel.

**Keine achte und neunte Spalte.** Gesucht wird in der Liste über alle Spalten, „2045" engt
damit auf das Bezugsjahr ein. Der Preis ist, dass nach dem Jahr allein nicht sortiert werden
kann; er ist kleiner als zwei weitere Spalten in einem ohnehin schmalen Dialog.

### 12.5 Nachweise

`KlimaSzenarioTests` (neu): die vier Spalten und ihre Wiederholbarkeit, der NULL gebliebene
Bestand, die Schlüssel je Szenario, das Szenario aus dem Dateikopf samt Gegenproben, der
Import mit und ohne „Art des TRY", die Projektkopie, die Altbestandskopie, der
zusammengesetzte Satz der Liste, der Halbgeviertstrich, eine Datenbank auf Stand 95, die
Herkunft des Projekts und der Eintragstext — dazu der Satzbau in beiden Kulturen.
`KlimaregionKatalogTests` hält den Schlüssel jetzt gegen die Datenbank und den Anzeigetext
gegen die Spalte. bunit: Herkunftszeile, Klimafeld samt Suche über das Jahr, Spalte „Quelle"
im Klimadaten-Dialog.

Gate: Kern 3 746, UI 4 789, KiKern 499, SpeicherEngine 378, SpeicherPlanung 27 (1
übersprungen) — 0 Fehler, beide Kulturen. Windows-Schale 0 Fehler, SQL-Dialekt-Prüfer 0
Fundstellen, ResourceDesigner wiederholbar, Schemawerkzeug-Trockenlauf „Stand 97, nichts
offen", Referenzlauf 1030/1007/1017/1045/1046 PASS und **byte-gleich** gegen
`2026-09-18_R9_Kesselbrennstoff`.

### 12.6 Windows-Abnahmepunkte

- **A-KL6-1** — Klimadaten öffnen: Die Spalte „Quelle" nennt bei einer neu eingelesenen
  Region aus den Regionaldaten „TRY-Regionaldaten (Deutschland) · 2045 · sommerwarm"; eine
  Suche nach „2045" engt auf sie ein. Bei PVGIS steht die Quelle allein.
- **A-KL6-2** — Übersicht: Unter dem Klimafeld steht „Klimadaten: TRY-Regionaldaten
  (Deutschland) · 2045 · sommerwarm · ‹Region› · ‹Standort› · Import ‹Datum›", und die
  Einträge des Klimafeldes tragen die Kurzform in Klammern („‹Region› (TRY 2045
  sommerwarm)", „München (PVGIS)"). Auf Englisch sind Quelle und Szenario übersetzt, die
  Jahreszahl steht ohne Tausenderpunkt.
- **A-KL6-3** — Bestandsdatenbank: Der Programmstart migriert auf Stand 97;
  `Tab_Klimaregion` und `Tab_Klimaregion_STAMM` führen `Szenario` und `Bezugsjahr`, in jeder
  Bestandszeile leer, und die Liste zeigt dort den Halbgeviertstrich.

### 12.7 Offene Punkte

- **Eine TRY-Datei bekommt kein Bezugsjahr.** Ihr Kopf nennt einen Bezugszeitraum; die
  Zuordnung „1995-2012 → 2015" und „2031-2060 → 2045" wäre die DWD-Konvention und nicht die
  Aussage der Datei. Anwenderentscheid nötig, ob die Zuordnung gewollt ist — oder ob der
  Dialog bei dieser Quelle nach dem Bezugsjahr fragen soll.
- **Die Szenario-Anzeigetexte sind die bestehenden `KLIMA_TRY_SZ_*`**, nicht neue
  `KLIMA_SZENARIO_*`: Sie sagen wörtlich dasselbe („mittleres Jahr", „sommerwarm",
  „winterkalt"), und ein zweiter Schlüssel gleichen Inhalts wäre die Wahrheit doppelt.
- **Der Altbestand wird nicht nachdatiert.** Eine Region, die vor Schritt 97 angelegt wurde,
  sagt nichts über ihr Wetterjahr — auch dann nicht, wenn ihr `Details`-Text es nennt: Aus
  einem Freitext einen Schlüssel zu lesen wäre eine Behauptung über einen Satz, der in einer
  von zwei Sprachen geschrieben sein kann.

## 13. Nachtrag KL-7 — Bezugsjahr einer TRY-Datei aus dem Bezugszeitraum des Kopfs

**Anwenderentscheid 20.09.2026:** Die DWD-Konvention wird übernommen — `1995-2012 → 2015`,
`2031-2060 → 2045`. Damit ist der offene Punkt aus 12.7 beantwortet; der Dialog fragt bei
dieser Quelle nicht nach dem Bezugsjahr.

### 13.1 Befund

`TryKopf.Bezugszeitraum` wurde von `KopfzeileDeuten` (`DwdTryLeser`) bereits gefüllt, aber
nirgends ausgewertet. Im TRY-Datei-Zweig von `KlimaImportAblauf.Laufen` stand allein
`szenario = SzenarioschluesselAusKopf(kopf.Art)`; `bezugsjahr` blieb null. Der Regionalzweig
nimmt `auftrag.TryJahr` (`TRY_JAHR_VORGABE` 2015, `TRY_JAHR_PROJEKTION` 2045). Die
`Add`-Überladung mit Szenario und Bezugsjahr war seit Schritt 97 vorhanden — es fehlte nur
die Ableitung. Kein Schemaschritt, kein Rechenweg, kein Dialog- und kein Hüllenbedarf: Die
Anzeige läuft über `KlimaAnzeige.Quellenzeile` aus KL-6 von selbst mit.

### 13.2 Zuordnung

Neu `KlimaImportAblauf.BezugsjahrAusZeitraum(string)`. Gelesen werden die ersten beiden
**vierstelligen** Jahreszahlen des freien Textes; eine Ziffernfolge anderer Länge ist keine
Jahreszahl. Womit die beiden verbunden sind, ist gleichgültig — Bindestrich, Gedankenstrich,
„bis", Zwischenräume. Das Paar wird gegen die **benannte Tabelle** `BEZUGSZEITRAEUME`
gehalten:

| Bezugszeitraum im Kopf | Bezugsjahr |
|---|---|
| 1995–2012 | `TRY_JAHR_VORGABE` = 2015 |
| 2031–2060 | `TRY_JAHR_PROJEKTION` = 2045 |
| alles andere (leer, ein einzelnes Jahr, „1961-1990", unlesbar) | NULL |

Eine **Tabelle, keine Schwelle**: Ein fremder Zeitraum fällt nicht still auf die Vorgabe,
sondern ergibt kein Jahr. Der gelesene Zeitraum steht wörtlich im Herkunftsvermerk
`Details` (neuer Schlüssel `KLIMA_TRY_BEZUGSZEITRAUM`, beide Sprachen), damit die Ableitung
in der Zeile nachlesbar bleibt.

### 13.3 Nachweise

`KlimaSzenarioTests` gewachsen: Theory `Der_Bezugszeitraum_des_Kopfs_ergibt_das_Jahr_oder_nichts`
(elf Fälle), `Der_TRY_Dateiimport_schreibt_Szenario_und_Jahr_aus_dem_Kopf` (Kopf „Art des
TRY: Sommer warm" + „Bezugszeitraum: 2031-2060" → `SOMMERWARM`/2045, Zeitraum im
`Details`-Text, Regionsliste „DWD-Testreferenzjahr aus Datei · 2045 · sommerwarm"),
`Ohne_Bezugszeitraum_im_Kopf_bleibt_das_Jahr_leer` und
`Ein_fremder_Bezugszeitraum_ergibt_kein_Jahr`. Der Testkopf-Generator `TryDatei` nimmt jetzt
mehrere Kopfzeilen (`params string[]`). 39 Prüfungen der Klasse grün in beiden Kulturen; kein
Rechenweg berührt, Referenzlauf byte-gleich gegen die geltende Basis.

### 13.4 Windows-Abnahmepunkt

- **A-KL7-1** — Eine echte DWD-TRY-Datei mit dem Bezugszeitraum 2031-2060 einlesen: Die
  Regionsliste nennt in der Spalte „Quelle" Jahr **und** Szenario („DWD-Testreferenzjahr aus
  Datei · 2045 · sommerwarm"), das Detailfeld führt den Bezugszeitraum wörtlich mit. Eine
  Datei ohne Bezugszeitraum im Kopf bleibt ohne Jahr.
