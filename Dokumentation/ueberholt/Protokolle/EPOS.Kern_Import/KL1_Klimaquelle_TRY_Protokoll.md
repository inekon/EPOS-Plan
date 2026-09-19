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
2. **Lambert → WGS 84 nicht umgesetzt** — den Standort einer TRY-Datei gibt der Anwender an.
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
