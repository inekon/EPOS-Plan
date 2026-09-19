# KL1 — Klimaquelle DWD-Testreferenzjahr (TRY): Umsetzungsprotokoll

Stand: 19.09.2026 · Zweig `ios_migration_september` · Bezug:
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
