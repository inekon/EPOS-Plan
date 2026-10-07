# SLP25b — BDEW-Netzbezugsprofile P25 und S25 (Haushalt mit PV bzw. PV und Speicher) als Katalogzeilen der Datenbank Strombedarf, Schemaschritt 196 (Protokoll, 07.10.2026)

Statuszeile #799 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Sitzung „EPOS Plan
Wirtschaftlichkeit" (Orchestrierung Fable 5.1, Bau Opus 5.5, Papiere Sonnet). Zweig `slp25b` ab origin
`676e12e85` (Anmeldung Schemaschritt 196), Merges mit origin `95570cc39` (Schemaschritt 195) und `83643e1a1`.

## Anlass

Anwender 07.10.2026: „Nehme auch PV Profile auf" — nach SLP25 (Statuszeile #787), das P25 und S25 nach Empfehlung
weggelassen hatte. Die BDEW-Veröffentlichung Standardlastprofile Strom 2025 führt neben H25, G25 und L25 (SLP25)
auch zwei PV-Varianten: P25 (Haushalt mit PV-Anlage) und S25 (Haushalt mit PV-Anlage und Batteriespeicher).

## Datenlage

Die BDEW-Excel führt auch für P25 und S25 je 12 Monate × 3 Typtage (Werktag, Samstag, Sonn-/Feiertag) ×
96 Viertelstunden, kWh je Viertelstunde; beide Blätter tragen den Kopf „Bei Ausrollen der Profile ist die
Dynamisierungsfunktion anzuwenden!" — beide sind damit entdynamisiert wie H25, die Dynamisierung wird angewendet.
P25 bildet den Netzbezug eines Haushalts mit PV-Anlage ab, S25 den eines Haushalts mit PV-Anlage und
Batteriespeicher; beide sind Netzbezugsprofile, keine Verbrauchsprofile. Der BDEW-Hinweis zur SOT-Zeitreihe betrifft
die Einspeiseseite der PV-Anlage und ist in EPOS-Plan nicht abgebildet — EPOS-Plan rechnet PV und Speicher über den
eigenen Rechenweg.

## Entscheide

Orchestrierung nach Anwenderauftrag (fortlaufend ab E-SLP8 aus SLP25):

- **E-SLP9** P25 und S25 werden aufgenommen, als Netzbezugsprofile gekennzeichnet: Bezeichner, Beschreibung und
  Typbeschreibung nennen den Netzbezug, die Hilfe trägt eine Warnung, sie nicht mit der PV-Rechnung des Projekts zu
  kombinieren — sonst zählte die PV doppelt; wer die PV im Projekt rechnet, nimmt H25.
- **E-SLP10** Eigener Schemaschritt 196 `StandardlastprofilPvSchema`, statt den ausgelieferten Schritt 193 zu
  ändern.
- **E-SLP11** Bezeichner `BDEW_P25_Haushalt_PV`, `BDEW_S25_Haushalt_PV_Speicher`; Typ `BDEW_P25`, `BDEW_S25`;
  `ReadOnly = 1`; Schlüssel `SV:BDEW_P25_HAUSHALT_PV`, `SV:BDEW_S25_HAUSHALT_PV_SPEICHER`, `SVT:BDEW_P25`,
  `SVT:BDEW_S25`.
- **E-SLP12** Jahressumme 1.000 MWh Netzbezug; Wochenform und Monatswerte wie bei SLP25 (Jahresmittel je Typtag;
  kalenderneutral, H25-artig dynamisiert).

## Umsetzung

### Phase 1

Commits `402c37c84`, `572a65989`, `e793eb8ac`.

- `Werkzeuge/Standardlastprofile/ableiten.py` erweitert: liest zusätzlich die Blätter P25/S25, neue Hilfsfunktion
  `saetze_cs`, neue Prüfungen, `kennzahlen` um die Monatsanteile ergänzt.
- `EPOS.Kern/Allgemein/Update/StandardlastprofilSaat.cs` um 84 Zeilen erweitert, H25/G25/L25 bleiben byte-gleich;
  eigene Liste `StandardlastprofilSaattabelle.Netzbezug` für P25/S25.
- Gemeinsame Mechanik `StandardlastprofilSchema.SaatAusfuehren`/`SaatVollstaendig`, von beiden Schemaschritten
  benutzt.
- Neuer Schritt `EPOS.Kern/Allgemein/Update/StandardlastprofilPvSchema.cs`.
- Tests `StandardlastprofilSaatTests` (jetzt fünf Profile), neu `StandardlastprofilPvSchemaTests`.
- Hilfe `EPOS.Kern/Allgemein/Hilfe/Berechnung/Strombedarf.wiki`: neuer Absatz P25/S25 mit der Warnung vor
  Doppelzählung, Satz unter Grenzen.
- Logbuch-Entwurf `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`, Platzhalter 1.2.0.8 um einen Satz ergänzt.
- Konzept Stromspeicher 10.1 angepasst.
- Beide LIESMICH (Quellen, Werkzeug) nachgezogen.

### Phase 2

Commits `a1c4f1f7f`, `6fc314977`, `1b658a167`, `8dbf859a4`.

- `StandardlastprofilPvSchema.SCHRITT = ErdsondenfeldSchema.SCHRITT + 1`; `SchemaStand.Zielversion` 196;
  `Paketanhebung.cs` Stufe 196 Art Katalog; `SchemaMigration.cs` (`SCHRITT_STANDARDLASTPROFIL_PV`,
  `Schritt_StandardlastprofilPv`).
- `Werkzeuge/Testdatenbankschema/Program.cs`, `EPOS.Kern.Tests/TestDatenbank.cs` auf Schemastand 196 gehoben.
- Testdatenbank LFS: 89 690 112 → 89 698 304 Byte, OID alt
  `29dbf1dd08e329196c74ee73604892d8b3e9e9852f0a8efc988172687cacf382`, neu
  `6f83f95faf13496f43f172f89e5366c6983cb64dae50bc77764a6acb1d7af318`. Zellvergleich: nur `Tab_Stromverbraucher_STAMM`
  44→46 (Ids 139/140), `Tab_Stromverbrauchertyp_STAMM` 43→45 (Ids 116/117), `Tab_Applikation.SchemaVersion`
  195→196, `sqlite_sequence` (eigene Zähler); zweiter Lauf sät 0 Zeilen; `integrity_check` ok, `foreign_key_check`
  leer.
- Eingefrorene Zahlen nachgezogen: `BedarfVerwaltungTests` 46, `KatalogfilterBedarfTests` 46/5/44,
  `KatalogpflegeTests` 46/45, `StandardlastprofilSchemaTests` Grundzahlen relativ (`…_46_Saetze_…`),
  `StandardlastprofilVorlageTests` +46/5/41 und +45/5/40 über beide Schritte.
- `Referenzlaeufe/LIESMICH.md` Nachtrag Schritt 196 (Basis-Absatz bleibt auf Schemastand 195/R40).

## Kennzahlen

### Monatswerte (MWh, zwölf Monate, normiert auf 1.000 MWh/a Netzbezug)

| Profil | Jan | Feb | Mär | Apr | Mai | Jun | Jul | Aug | Sep | Okt | Nov | Dez |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| P25 | 145,198 | 105,655 | 94,799 | 71,642 | 45,050 | 40,387 | 42,046 | 46,797 | 53,388 | 78,719 | 115,208 | 161,109 |
| S25 | 207,396 | 124,201 | 78,565 | 42,122 | 14,689 | 10,777 | 13,891 | 18,635 | 21,911 | 70,428 | 151,142 | 246,244 |

Summe je Zeile exakt 1.000,000000 MWh.

### Wochenform

| Profil | Spitzenfaktor der Woche | Spitzenfaktor BDEW-Viertelstunde | Werktag : Samstag : Sonntag |
|---|---|---|---|
| P25 | 1,679 | 2,89 | 1 : 1,042 : 1,047 |
| S25 | 1,484 | 4,62 | 1 : 1,086 : 1,043 |

### Anteil einzelner Monate

| Profil | Januar | Juni | Dezember |
|---|---|---|---|
| P25 | 14,52 % | 4,04 % | 16,11 % |
| S25 | 20,74 % | 1,08 % | 24,62 % |

### Abrollung vor Skalierung

| Profil | Abrollung vor Skalierung |
|---|---|
| P25 | 998,8 MWh |
| S25 | 998,7 MWh |

Vor der abschließenden Normierung auf exakt 1.000,000000 MWh/a.

## Testdatenbank

Testdatenbank LFS auf Schemastand 196: 89 690 112 → 89 698 304 Byte, OID alt
`29dbf1dd08e329196c74ee73604892d8b3e9e9852f0a8efc988172687cacf382`, neu
`6f83f95faf13496f43f172f89e5366c6983cb64dae50bc77764a6acb1d7af318`. Zellvergleich gegen Schemastand 195: nur
`Tab_Stromverbraucher_STAMM` (44→46 Zeilen, Ids 139/140: `BDEW_P25_Haushalt_PV`, `BDEW_S25_Haushalt_PV_Speicher`),
`Tab_Stromverbrauchertyp_STAMM` (43→45 Zeilen, Ids 116/117), `Tab_Applikation.SchemaVersion` (195→196) und
`sqlite_sequence` (eigene Zähler). Zweiter Lauf des Schemaschritts sät 0 Zeilen; `integrity_check` ok,
`foreign_key_check` leer. Nebenbefund (wie #787): die fremden Zähler `Tab_Kenndaten_Kaeltemaschine_STAMM`,
`Tab_Nutzungsprofil_STAMM` und `Z_Nutzungsprofil` laufen durch `Werkzeuge/Testdatenbankschema` weiter hoch, ohne
dass sich deren Zeilen ändern.

## Tests

Gezielt grün: 437 Kern + 58 UI (Standardlastprofil 28, GebaeudeSaat 8, Katalogfassung 5, Stromverbraucher 27,
TestDatenbank 91, Auslieferung 178, BedarfVerwaltung 37, KatalogfilterBedarf 17, Katalogpflege 123), Schema 89
(ProjektpaketAnhebung 10, AufheizSchema 10, AufheizManuellSchema 8, TwwSchema 20, Erdsondenfeld 27, Typaufbau 14),
Wachen 35, `Auslieferungsvorlage.sln` 61/61. Kern-Filter Release 0 Fehler, Windows-Schale Debug x64 0 Fehler,
SqlDialektPruefer 2 482 Texte / 0 Fundstellen, Werkzeug „unverändert". Schemaschritt 197 (Sitzung IFC,
`FlaechenherkunftSchema`) hängt an 196.

## Abnahme

Gate (Worktree `slp25b` auf `0ff2881d7`, Log `GATESLP25B`, 07.10.2026 12:46–13:15): Kern-Filter Release 0 Fehler; ChartProben 211 Bilder, Hashes gleich der Windows-Messlatte (211/211); voller Lauf 20 512 bestanden / 0 Fehler / 4 übersprungen (EPOS.Kern.Tests 11 764 / 0 / 3 von 11 767, EPOS.UI.Tests 7 775, KiKern.Tests 549, SpeicherEngine.Tests 397, SpeicherPlanung.Tests 27 / 1 übersprungen); Dokumentationswachen 35/35. Die zehn fremden Windows-Fehler aus dem Gate von #787 (GebaeudeEinzonennetz, Zonenuebergabe, GebaeudeImportHuelle) sind auf diesem Stand nicht mehr vorhanden.
Referenzlauf: 8/8 PASS gegen `Referenzlaeufe/2026-10-07_R40_Erdreichquellen` (Projekte 1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051; 2 883 668 Werte im Toleranzvergleich; `EPOS.Referenzlauf` Release auf `0ff2881d7`, 07.10. 13:16), die Basis bleibt — die Katalogsaat hat keinen Projektbezug.
CI: (folgt)

## Offen

- **Wiki-Upload** der Berechnungshilfe Strombedarf (Absatz P25/S25) und **Logbuch 1.2.0.8**, Versionsnummer beim
  Anwender erfragen.
- **CI-Nachweis** nachtragen, sobald der Kern-Lauf vorliegt.
- **Grenze der Wochenform** bei P25/S25: der sommerliche Mittagsbezug wird um rund 60 % (P25) bzw. 23 % (S25)
  überschätzt (in der Hilfe unter „Grenzen" benannt); das native BDEW-Verfahren (Ganglinie) bleibt zurückgestellt
  (Konzept Stromspeicher 10.1).
- **Warnung zur Doppelzählung** mit der PV-Rechnung des Projekts steht nur in Hilfe und Beschreibung, keine Sperre
  im Programm; ein Hinweis im Dialog wäre ein eigener Auftrag.
- **Nebenbefund (wie #787):** `Werkzeuge/Testdatenbankschema` treibt bei diesem Lauf zusätzlich die
  AUTOINCREMENT-Zähler fremder Tabellen (`Tab_Kenndaten_Kaeltemaschine_STAMM`, `Tab_Nutzungsprofil_STAMM`,
  `Z_Nutzungsprofil`) hoch, ohne deren Zeilen zu ändern.

## Dateien

- Quellen: `Quellen/Standardlastprofile/` (dieselbe BDEW-Excel wie SLP25, jetzt zusätzlich die Blätter P25/S25
  gelesen).
- Werkzeug: `Werkzeuge/Standardlastprofile/ableiten.py` (erweitert um P25/S25, `saetze_cs`, Monatsanteile in
  `kennzahlen`).
- Kern: `EPOS.Kern/Allgemein/Update/StandardlastprofilSaat.cs`, `StandardlastprofilSchema.cs` (gemeinsame
  Mechanik), `StandardlastprofilPvSchema.cs` (neu), `EPOS.Kern/Allgemein/Hilfe/Berechnung/Strombedarf.wiki`.
- Schema: `Werkzeuge/Testdatenbankschema/Program.cs`, `Paketanhebung.cs`, `SchemaMigration.cs`.
- Tests: `StandardlastprofilSaatTests`, `StandardlastprofilPvSchemaTests` (neu), `EPOS.Kern.Tests/TestDatenbank.cs`.
- Testdatenbank: `Referenzlaeufe/Kenndaten_Test.sqlite` (LFS), `Referenzlaeufe/LIESMICH.md`.
- Papiere: `Dokumentation/aktuell/Status_iOS_Migration.md`, `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`,
  Konzept Stromspeicher (Abschnitt 10.1).

## Commit

Phase 1: `402c37c84`, `572a65989`, `e793eb8ac`. Phase 2: `a1c4f1f7f`, `6fc314977`, `1b658a167`, `8dbf859a4`.
Papiere dieser Welle (Statuszeile #799, dieses Protokoll, LIESMICH-Indexzeile): Zweig `slp25bp`, HEAD `0ff2881d7`.
