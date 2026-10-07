# SLP25 — BDEW-Standardlastprofile Strom 2025 als Katalogzeilen der Datenbank Strombedarf, Schemaschritt 193 (Protokoll, 07.10.2026)

Statuszeile #787 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Sitzung „EPOS Plan
Wirtschaftlichkeit" (Orchestrierung Fable 5.1, Analyse und Bau Opus 5.5, Papiere Sonnet). Zweig `slp25` ab origin
`6bbb8c15b` (Anmeldung Schemaschritt 193), Merges mit origin `59e6d1705` (Schemaschritt 191) und `7d925f0ab`
(Schemaschritt 192).

## Anlass

Anwender 06.10.2026: „füge die Standardlastprofile aus Z:\…\40-Daten\Standardlastprofile hinzu", „lastprofile in db
aufnehmen", „fahre fort, danach Push von SLP25 und BA-4a". Die Datenbank Strombedarf (`Tab_Stromverbraucher_STAMM`
mit Typprofil `Tab_Stromverbrauchertyp_STAMM`) führte bislang nur den Altsatz `Haushalt-VDEW`; die BDEW veröffentlicht
seit 2025 eigene Standardlastprofile für Haushalt (H25), Gewerbe (G25) und Landwirtschaft (L25) sowie zwei
PV-Varianten (P25, S25).

## Analyse (Befund außerhalb des Repos)

Die Excel-Veröffentlichung der BDEW führt je Profil 12 Monate × 3 Typtage (Werktag, Samstag, Sonn-/Feiertag) ×
96 Viertelstunden, kWh je Viertelstunde, normiert auf 1.000 MWh/a. H25 ist entdynamisiert (Dynamisierungsfunktion der
Veröffentlichung, Koeffizienten wie beim VDEW-H0); G25 und L25 kommen ohne Dynamisierung. P25 und S25 bilden den
Netzbezug von Haushalten mit PV bzw. PV und Speicher ab und wurden nicht aufgenommen — EPOS-Plan rechnet PV und
Speicher selbst.

Das Datenmodell der Strombedarfsdatenbank bildet einen Verbraucher über `Tab_Stromverbraucher_STAMM` (12 Monatswerte
in MWh) und einen Typ über `Tab_Stromverbrauchertyp_STAMM` (168 Wochenstunden) ab; `ProfilBedarf` ruft
`BhkwPlan.StromWocheToJahr` auf, die die Wochenform ab dem Wochentag des 1. Januar über das Jahr kachelt und je Monat
normiert (Feiertage laufen über den Betriebskalender als Sonntag). Eine Verlustprobe der Wochenform gegen die
stündliche Abrollung der BDEW-Viertelstundenreihe ergab: H25 5,5 %, G25 2,9 % (13 % ohne Feiertagskalender), L25
4,0 % RMSE bezogen auf das Mittel, Korrelation jeweils > 0,98.

## Entscheide

Nach Empfehlung der Orchestrierung, Anwender informiert, kein Einspruch:

- **E-SLP1** Aufgenommen werden H25, G25 und L25; P25 und S25 nicht.
- **E-SLP2** Weg (a): Typprofil (Monatswerte × Wochenform) statt eines nativen BDEW-Verfahrens; keine Ganglinie —
  damit ist Konzept Stromspeicher 10.1 beantwortet, das native Verfahren bleibt zurückgestellt.
- **E-SLP3** Jahressumme 1.000 MWh (BDEW-Normierung); die Skalierung auf den tatsächlichen Bedarf geschieht im
  Projekt.
- **E-SLP4** Monatswerte kalenderneutral: Mittel über die 7 Wochentage des 1. Januar, 365 Tage, ohne Feiertage; H25
  dynamisiert; die Summe der zwölf Monatswerte trifft exakt 1.000,000000 MWh.
- **E-SLP5** Feiertage werden nur über den Betriebskalender abgebildet (Hinweis in der Hilfe).
- **E-SLP6** Bezeichner `BDEW_H25_Haushalt`, `BDEW_G25_Gewerbe`, `BDEW_L25_Landwirtschaft`; Typ `BDEW_H25` usw.;
  `ReadOnly = 1`; Katalogschlüssel `SV:BDEW_H25_HAUSHALT` / `SVT:BDEW_H25` mit Prüfsumme.
- **E-SLP7** Excel und PDF der BDEW als Fremdquelle unter `Quellen/Standardlastprofile/` versioniert; keine
  Lizenzangabe in den Dateien, Anwender informiert.
- **E-SLP8** Wochenform = Jahresmittel je Typtag; eine Wochenstunde ist die Summe der vier zugehörigen
  Viertelstunden.

## Umsetzung

### Phase 1

Commits `0c06a59ba`, `c69b72ff5`, `9dfbe1309`, `f8b1f620f`, `d0ec99fc0`, `fa38da81f`, `5766513cc`, `36bbfc3e0`,
`c7b32b610`.

- `Quellen/Standardlastprofile/` mit PDF, XLSX und LIESMICH (Herkunft, SHA-256, Abruf 06.10.2026).
- `Werkzeuge/Standardlastprofile/ableiten.py` samt LIESMICH: liest die Excel per `zipfile`/XML, Prüfmodus ohne
  Argument meldet „unverändert", `schreiben` erzeugt die Konstanten, `kennzahlen` die Kennzahlen.
- `EPOS.Kern/Allgemein/Update/StandardlastprofilSaat.cs` (erzeugt) und `StandardlastprofilSchema.cs`: sät je Profil
  Typprofil und Kopf nur dort, wo der Name fehlt, nie überschreibend; eigene Sätze des Anwenders bleiben im
  Protokoll; Schlüssel über `KatalogSchluesselSaat.Ausfuehren`.
- Tests `StandardlastprofilSaatTests`, `StandardlastprofilSchemaTests`,
  `Werkzeuge/Auslieferungsvorlage.Tests/StandardlastprofilVorlageTests`.
- Hilfe `EPOS.Kern/Allgemein/Hilfe/Berechnung/Strombedarf.wiki` (neuer Absatz samt Grenzen).
- Logbuch-Entwurf `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`, Platzhalter 1.2.0.8 (Version beim Anwender
  offen).
- Konzept Stromspeicher: 10.1 beantwortet, 3.1 Monatswerte in MWh berichtigt.

### Phase 2

Commits `1dd9bfc99`, `28e37a68f`, `c0ca9ce2b`, `f881aeaf5`, `a5eb1329e`, `601c6ee46`.

- `StandardlastprofilSchema.SCHRITT = TypaufbauSchema.SCHRITT + 1`; `SchemaStand.Zielversion` 193;
  `Paketanhebung.cs` Stufe 193 Art Katalog; `SchemaMigration.cs` (`SCHRITT_STANDARDLASTPROFIL`,
  `Schritt_Standardlastprofil`).
- `Werkzeuge/Testdatenbankschema/Program.cs`, `EPOS.Kern.Tests/TestDatenbank.cs` auf Schemastand 193 gehoben.
- Testdatenbank LFS: 87 818 240 → 87 822 336 Byte, OID alt `6999c3c9…`, neu
  `7bc44eb8a3d460d794ff5ac24f62860396b9f7fc2adb7a806e1c68a3ae751a91`. Zellvergleich: nur
  `Tab_Applikation.SchemaVersion` 192→193, `Tab_Stromverbraucher_STAMM` 41→44 (Ids 136–138),
  `Tab_Stromverbrauchertyp_STAMM` 40→43 (Ids 113–115), `sqlite_sequence`; zweiter Lauf sät 0 Zeilen; `integrity_check`
  ok, `foreign_key_check` leer.
- Auslieferung: Stromtabellen aus den Ausnahmen gestrichen (`Werkzeuge/Auslieferungsvorlage.Tests/Werkzeuglauf.cs`,
  `.github/workflows/windows.yml`, `LEERE_PAKETTEILE_DER_TESTDATENBANK`); es bleiben neun Ausnahmen.
- Eingefrorene Zahlen nachgezogen: `BedarfVerwaltungTests` (44, erste Zeile `BDEW_G25_Gewerbe`),
  `KatalogfilterBedarfTests` (44; 42 mit Jahressumme > 1; 3 Auslieferungssätze), `KatalogpflegeTests` (44/0/0,
  43/0/1); `TypaufbauSchemaTests` auf die Kette plus `>=`.
- `Referenzlaeufe/LIESMICH.md` Nachtrag Schritt 193 (Basis R39 bleibt); Statuskopf 193 „1.000 MWh/a"; `CLAUDE.md`
  Werkzeugzeile `Werkzeuge/Standardlastprofile`.

## Kennzahlen

### Monatswerte (MWh, zwölf Monate, normiert auf 1.000 MWh/a)

| Profil | Jan | Feb | Mär | Apr | Mai | Jun | Jul | Aug | Sep | Okt | Nov | Dez |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| H25 | 101,047 | 88,185 | 88,293 | 81,527 | 77,137 | 71,521 | 74,370 | 73,546 | 73,163 | 83,531 | 88,299 | 99,381 |
| G25 | 93,642 | 83,681 | 90,015 | 81,551 | 79,761 | 77,307 | 75,673 | 77,063 | 76,754 | 82,065 | 90,077 | 92,409 |
| L25 | 92,022 | 83,116 | 89,510 | 81,761 | 80,823 | 74,671 | 77,160 | 77,160 | 78,216 | 84,486 | 89,053 | 92,022 |

### Wochenform

| Profil | Spitzenfaktor der Woche | Werktag : Samstag : Sonntag |
|---|---|---|
| H25 | 1,613 | 1 : 1,141 : 1,178 |
| G25 | 2,088 | 1 : 0,620 : 0,482 |
| L25 | 1,801 | 1 : 0,963 : 1,013 |

### Verlustprobe Wochenform gegen stündliche Abrollung

| Profil | RMSE/Mittel | Korrelation |
|---|---|---|
| H25 | 5,5 % | > 0,98 |
| G25 | 2,9 % (13 % ohne Feiertagskalender) | > 0,98 |
| L25 | 4,0 % | > 0,98 |

## Testdatenbank

Testdatenbank LFS auf Schemastand 193: 87 818 240 → 87 822 336 Byte, OID alt `6999c3c9…`, neu
`7bc44eb8a3d460d794ff5ac24f62860396b9f7fc2adb7a806e1c68a3ae751a91`. Zellvergleich gegen Schemastand 192: nur
`Tab_Applikation.SchemaVersion` (192→193), `Tab_Stromverbraucher_STAMM` (41→44 Zeilen, Ids 136–138:
`BDEW_H25_Haushalt`, `BDEW_G25_Gewerbe`, `BDEW_L25_Landwirtschaft`), `Tab_Stromverbrauchertyp_STAMM` (40→43 Zeilen,
Ids 113–115) und `sqlite_sequence`. Zweiter Lauf des Schemaschritts sät 0 Zeilen (nicht überschreibend bei
vorhandenem Namen); `integrity_check` ok, `foreign_key_check` leer. Nebenbefund: `Werkzeuge/Testdatenbankschema`
treibt bei jedem Lauf die AUTOINCREMENT-Zähler älterer Schritte (178, 182) hoch, ohne dass sich Zeilen ändern.

## Tests

Gezielt grün: Standardlastprofil 17, GebaeudeSaat 7, Katalogfassung 4, Stromverbraucher 17, TestDatenbank 16,
Auslieferung 123, BedarfVerwaltung 37, KatalogfilterBedarf 17, Katalogpflege 123, Schemafolge 48, Wachen 35,
KatalogpaketSetupWache 7, `Auslieferungsvorlage.sln` 61/61, zusätzlich Kern 717 + 2 088 und UI 702. Kern-Filter
Release 0 Fehler, Windows-Schale Debug x64 0 Fehler, SqlDialektPruefer 2 474 Texte / 0 Fundstellen, Werkzeug
„unverändert".

## Abnahme

Gate: (folgt)
Referenzlauf: (folgt)
CI: (folgt)

## Offen

- **Wiki-Upload** der Berechnungshilfe Strombedarf und **Logbuch 1.2.0.8**, Versionsnummer beim Anwender erfragen.
- **CI-Nachweis** nachtragen, sobald der Kern-Lauf vorliegt.
- **Grenzen der Wochenform**: keine Monatsform, keine Viertelstunde, keine Dynamisierung innerhalb des Monats; ohne
  gepflegten Betriebskalender gilt ein Feiertag als Werktag, wodurch G25 etwas zu hoch liegt. Das native
  BDEW-Verfahren (Ganglinie) bleibt zurückgestellt (Konzept Stromspeicher 10.1).
- **P25/S25** (Netzbezug von Haushalten mit PV bzw. PV und Speicher) nicht aufgenommen; bei Bedarf eigener Auftrag.
- **Lizenz der BDEW-Dateien** unter `Quellen/Standardlastprofile/` ohne Angabe — Entscheid des Anwenders bleibt
  offen.
- **Nebenbefund:** `Werkzeuge/Testdatenbankschema` treibt die AUTOINCREMENT-Zähler älterer Schritte (178, 182) hoch,
  ohne Zeilen zu ändern.
- `Haushalt-VDEW` (Altsatz) bleibt unverändert neben den neuen Zeilen stehen.

## Dateien

- Quellen: `Quellen/Standardlastprofile/` (PDF, XLSX, LIESMICH).
- Werkzeug: `Werkzeuge/Standardlastprofile/ableiten.py`, `Werkzeuge/Standardlastprofile/LIESMICH.md`.
- Kern: `EPOS.Kern/Allgemein/Update/StandardlastprofilSaat.cs`, `StandardlastprofilSchema.cs`,
  `EPOS.Kern/Allgemein/Hilfe/Berechnung/Strombedarf.wiki`.
- Schema: `Werkzeuge/Testdatenbankschema/Program.cs`, `Paketanhebung.cs`, `SchemaMigration.cs`.
- Tests: `StandardlastprofilSaatTests`, `StandardlastprofilSchemaTests`,
  `Werkzeuge/Auslieferungsvorlage.Tests/StandardlastprofilVorlageTests`,
  `Werkzeuge/Auslieferungsvorlage.Tests/Werkzeuglauf.cs`, `EPOS.Kern.Tests/TestDatenbank.cs`.
- Auslieferung/CI: `.github/workflows/windows.yml`.
- Testdatenbank: `Referenzlaeufe/Kenndaten_Test.sqlite` (LFS), `Referenzlaeufe/LIESMICH.md`.
- Papiere: `Dokumentation/aktuell/Status_iOS_Migration.md`, `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`,
  `Dokumentation/aktuell/Konzept_Setup_InnoSetup_EPOS-Plan.md`, `CLAUDE.md`.

## Commit

Phase 1: `0c06a59ba`, `c69b72ff5`, `9dfbe1309`, `f8b1f620f`, `d0ec99fc0`, `fa38da81f`, `5766513cc`, `36bbfc3e0`,
`c7b32b610`. Phase 2: `1dd9bfc99`, `28e37a68f`, `c0ca9ce2b`, `f881aeaf5`, `a5eb1329e`, `601c6ee46`. Papiere dieser
Welle (Statuszeile #787, dieses Protokoll, Setup-Konzept): Zweig `slp25p` ab `601c6ee46`.
