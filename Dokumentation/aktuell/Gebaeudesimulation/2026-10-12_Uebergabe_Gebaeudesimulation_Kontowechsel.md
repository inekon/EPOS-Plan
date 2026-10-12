# Übergabe der Sitzung „Gebäudesimulation“ — Stand 12.10.2026 (Kontowechsel)

Dieses Papier hält, was eine neue Cloud-Sitzung auf einem anderen Konto braucht, um die Sitzung „Gebäudesimulation“
ohne Verlust fortzuführen: Stand, laufende und nächste Arbeit, offene Anwenderpunkte, Arbeitsweise und am Ende den
Einstiegs-Prompt. Regelquelle bleibt [`CLAUDE.md`](../../../CLAUDE.md) mit den Projekt-`CLAUDE.md`; der dauerhafte Stand steht
in der [Statusdatei iOS-Migration](../Status_iOS_Migration.md) (eine Zeile je Schritt) und in der
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md) (Entscheidungsregister bis E133, Stufentabelle).
Es löst die [Übergabe vom 08.10.2026](../../ueberholt/2026-10-08_Uebergabe_Gebaeudesimulation_Kontowechsel.md) ab und wandert
nach `Dokumentation/ueberholt/`, sobald die Fortsetzung BK-3 und die GD-Nachzüge abgeschlossen hat.

## 1. Stand bei der Übergabe

| Was | Wert |
|---|---|
| Arbeitszweig | `ios_migration_september` (Remote `origin`). Der Sitzungszweig des alten Kontos `claude/magical-bohr-ff5ufj` wurde bei jedem Push mitgezogen; die neue Sitzung nimmt ihren eigenen Sitzungszweig |
| Letzte Wellen | #1027 BK-2 (Schritt 221), #1026 GD-4, #1023 BK-1, #1020 GD-5, #1022 NP7/NP7b (Schritt 220); davor #1011 NP6, #1012 Quick Assist (Admin-Zweig), #1013 AB2, #1014 AB3, #1015 GD-1, #1016 GD-2, #1017 Entwurf BK — gepusht `f40fe2fa1`; danach Anmeldung Schritt 220 (`6870f7169`) und GD-3 mit diesem Papier (#1019) |
| CI | Kern-Lauf 38206954119 grün auf `02d4ddfe0` (enthält alles bis #1022); #1010 grün mit 38188012085 auf `228a10287`; vermerkt in #1010, #1011, #1013–#1017, #1019, #1020, #1022. #1023 (BK-1) und die Papiere danach sind mit [skip ci] gepusht und laufen mit dem nächsten Push ohne [skip ci] durch die CI |
| Referenzbasis | **R52** `Referenzlaeufe/2026-10-11_R52_Vorheizen`, dreißig Projekte; CI-Auswahl 1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051, 1058, 1060, 1063, 1065. Nächste freie Basis **R53** (vorgesehen für SA4) |
| Testdatenbank | Schemastand **220** (Git LFS) |
| Schemakette | 218 `KaelteArtSchema` (Sitzung Kälteanlagen), 219 `RaumnutzungNormkategorienSchema` (NP6), 220 `RaumnutzungNormwerteSchema` (NP7); 221 `BauteilkatalogSchema` (BK-2); **222 frei** |
| Statuszeilen | höchste vergebene #1027; vor jeder neuen Zeile Tabelle und Kopf auf Reservierungen anderer Sitzungen prüfen. Entscheide bis **E133**, **E134 frei** |
| Parallele Sitzungen | „Kälteanlagen“, „Dialoge und Korrekturen“ (Konto 3), „Administration und Support“; alle pushen auf denselben Zweig. Statuskonflikte zeilenweise lösen, Nummernkollisionen über die nächste freie Nummer |

## 2. Was die Sitzung seit dem 08.10.2026 gebaut hat

Alle Zeilen mit grünem Gate gepusht; Einzelheiten in der Statuszeile und im Protokoll unter
[`ueberholt/Protokolle/Gebaeudesimulation/`](../../ueberholt/Protokolle/Gebaeudesimulation/).

| Welle | Zeilen | Inhalt |
|---|---|---|
| UB, KM3 | #837–#878 | Übergabegrenze und Bivalenz (Referenzprojekt 1060), Teillast der Kältemaschine (1063) |
| AH V0–V8 | bis #990 | Aufheizoptimierung Fassung 2 mit berechneter Vorheizzeit (E122, E124, E125), Referenzprojekt 1065, Basis R52 |
| SA0–SA3 | #984–#995 | Schnellaufheizung (E128): Kern, Heizkurve im Vorheizfenster, Schritt 217, Oberfläche, Bericht, Export; offen SA4 |
| SQ, SQ2, EG | #996, #997, #1008 | Projektdatei-Import bleibt nicht stehen; Raumflächen werden richtig gelesen; Geste des Entwicklermodus Strg+Umschalt+F12 |
| KS | #1002 | Datum im Gemeinjahr (Tag und Monat) für Saison, Ferien, Zeiträume, Einzeltage, Perioden |
| GD-Entwurf | #1004 | [Entwurf Gebäudedialog und Mehrzonenübernahme](2026-10-11_Entwurf_Gebaeudedialog_Mehrzonen.md), F1–F8 mit E130 entschieden |
| AB, AB2, AB3 | #1010, #1013, #1014 | Farbmodi „Aufbau“/„Befund“, Zuordnung über die Legende, Baustoffwahl an der Schicht statt Abschnitt „Baustoffe“ im Import |
| NP6 | #1011 | SIA 2024 und VDI 2078 aus dem Katalog der Nutzungsprofile, zwei Wohnprofile der DIN, Schritt 219 (E129) |
| GD-1, GD-2, GD-3, GD-5 | #1015, #1016, #1019, #1020 | Gebäudekopf mit Herkunft; Gebäudewerte der Projektdatei und Name = Dateiname; Räume ohne DIN-Zone automatisch platziert; Vorbesetzung der Zonen mit Profilen |
| NP7, NP7b | #1022 | Normwerte der DIN einlesen (CSV oder Projektdatei), Außenluft je Person, Schritt 220, Dialog, Anleitung im Assistenten (E131) |
| BK-Entwurf | #1017 | [Entwurf Bauteilkatalog und Hottgenroth-Import](2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md), Fragen F1–F8 mit E132 entschieden |
| BK-1 | #1023 | Editoren ohne Schema: Wärmekapazität immer sichtbar, Stoffgruppe als Auswahl mit Bändern, Summenfuß R_si…U/C, Kapazität im Bauteildialog |
| GD-4 | #1026 | Als Mehrzonenmodell übernehmen (Prüfsumme, Blatt, Ersetzen in einem Vorgang), Sprung Kachel Gebäudekühlung → Gruppe „Kühlung“ |
| BK-2 | #1027 | Schritt 221: Herkunft, Quellkennung, Gruppe/Klasse, Kennzeichen „meist nicht geeignet“, Felder der Quelle, Abschnitte |

## 3. Laufende Arbeit beim Schreiben

Ein Opus-Agent arbeitet im Container des alten Kontos, ohne Push: **BK-3 „Bauteilkatalog einlesen“** direkt aus dem
CSV-Export (E133) — Leser, Regeln 1–12, Zusammenführung, Wiederimport per Quellkennung, Vorschau mit Richtungsstichprobe,
Transaktion, Dialog unter Administration › Gebäude. Ist er beim Wechsel nicht gepusht, wird er neu beauftragt: Grundlage
sind Entwurf Abschnitte 2.3, 5.1, 6a, das Feldinventar mit „Format des CSV-Exports“ und das Protokoll BK-2.

Festlegungen, die Folgewellen kennen müssen:

- **Außenluft je Person** (NP7) ist eine eigene Spalte `Aussenluft_Person` (m³/(h·Person)) neben `Aussenluft`/`Aussenluft_Einheit`;
  im Generator geht die raum- oder flächenbezogene Außenluft vor.
- **Bauteilkatalog** (BK-2): Gruppe und Klasse sind Code-Spalten mit CHECK (Texte zweisprachig), Abschnitte stehen in
  `Tab_Bauteilabschnitt(_STAMM)`, das Kennzeichen `Simulation_Ungeeignet` setzt nur der Kern; importierte Sätze tragen
  Herkunft IMPORT, Quelle und Quellkennung und bleiben in der Auslieferung.

## 4. Nächste Arbeit (Reihenfolge)

1. **CI-Vermerk für #1023** (BK-1) und alle folgenden Zeilen mit dem nächsten Kern-Lauf ohne [skip ci]; rot → `get_job_logs` mit
   `failed_only`, Ursache eingrenzen, beheben.
2. **NP7-Nachzug:** Nummern 70/71 der Projektdatei auf die Wohnprofile (nach Anwenderentscheid, Abschnitt 5).
3. **GD-Nachzüge** (offene Punkte von GD-4, #1026): gespeicherten Zonenplan eines früheren Imports vorlegen; beim Weg
   „Gebäudedatei + Projektdatei“ auch die Projektdatei prüfen; Sprung zur Kühlung auf das zuletzt gewählte Gebäude; eigener
   Maskenschlüssel für `Sprungziele.Kuehlung` (Seitenschlüssel, `AppWurzel`, beide Navigationsadapter); Zeile Jahreswärmebedarf
   nach Anwenderentscheid (Abschnitt 5).
4. **Wiki und Logbuch** der Wellen GD, AB, NP6/NP7, KS (Seiten „Gebäude“, „Gebäudeimport“, „Nutzungsprofile“); Upload nur
   gebündelt und auf Zuruf, Versionsnummer beim Anwender erfragen.
5. **BK** (E132, E133; BK-1 #1023 und BK-2 #1027 gebaut): BK-2b Oberfläche (Gruppe und Klasse in Katalogdialog und -liste,
   Filter nach dem Kennzeichen, Texte der Klassen in beiden Sprachen); BK-3 Import **direkt aus dem CSV-Export der
   Quelltabellen (E133)** mit Abbildungsregeln, Fremdcode-Tafel, Einheiten der offenen Felder, Zusammenführung der Dubletten und
   Dialog „Bauteilkatalog einlesen…“ — Formatdetails und `Basis_Auszug.csv` (Bauteilart) sind bei der lokalen Sitzung
   angefragt; BK-4 LocalDB-Werkzeug nur für einen neuen Katalogstand; BK-5 inhomogene Schichten (Abschnitte in Editor,
   Projektkopie, Katalogpaket); BK-6 Fenster; BK-7 Wiki. Rund 8–11 PT.
   Grundlagen: [Feldinventar](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-12_BK_Feldinventar_Hottgenroth.md)
   mit Vorschlag der zu übernehmenden Felder, [Strukturbericht](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-12_BK_Strukturbericht_Hottgenroth.md);
   die `.mdf` und der Export liegen nur beim Anwender (lokale Sitzung „iOS migration merge conflicts“, `C:\Temp\Hsetu\`).
6. **Übernommen von der Sitzung „Anlagendialog/Kältebedarf“:** Sprung der Kachel Gebäudekühlung mit GD-4 gebaut (#1026);
   offen nur die Zeile Jahreswärmebedarf (Abschnitt 5).
7. **SA4:** Referenzprojekt mit Schnellaufheizung, Basis R53, Wiki (Stufentabelle SA).

## 5. Offen beim Anwender

- **BK-1, Katalogprobe:** `Proben/Rasterprobe` Katalogprobe meldet 8 von 66 Fällen mit Überlagerung (Kessel, BHKW, Wärmepumpe,
  Klimadaten) — gegen den Stand vor BK-1 prüfen, ob vorbestehend (kein Baustofffall betroffen).
- **NP7, Nummern 70/71 der Projektdatei:** beim Einlesen der Normwerte auf „Wohngebäude – Einfamilienhaus“ bzw.
  „– Mehrfamilienhaus“ abbilden? Empfehlung: ja, nur für das Einlesen; die Raumzuordnung auf das Muster Wohnen bleibt.
- **Jahreswärmebedarf in der Zusammenfassung des Gebäudedialogs** (übernommen von der Sitzung „Anlagendialog/Kältebedarf“,
  Konzept Projektdialoge Katalogauswahl, Abschnitt 5): Wert aus dem letzten Rechenlauf mit Datum zeigen oder die Zeile streichen?
  Empfehlung: Wert des letzten Laufs mit Datum, ohne Lauf „noch nicht gerechnet“; gehört zu GD-4 bzw. einer GD-Nachwelle.
- **Sichtabnahmen unter Windows:** gesperrte Farbmodusknöpfe mit Grund (#1010), Legendenzuordnung (#1013), Schichtwahl im
  Aufbau (#1014), Herkunftsmarken und Rundung im Gebäudekopf (#1015), Namensvorschlag „Übernehmen“ im Importdialog (#1016),
  Marke und „Zurücknehmen“ der automatisch platzierten Räume (#1019), Saisondatum (#1002), Geste Strg+Umschalt+F12 (#997).
- **SQ2:** gegen welchen Wert der Anwender die Raumgrößen des Sportheims verglichen hat.
- **GD-2:** fachliche Bestätigung Gebäudetyp 3 = Nichtwohngebäude und Code 0 = keine Angabe an weiteren Projektdateien.

## 6. Arbeitsweise in der Cloud

- **Anwenderdateien** (`Quellen/*.sqproj`, Hottgenroth-Katalog `.mdf`) liegen nie im Repositorium; Namen und Werte daraus nie in
  Code, Tests, Protokollen oder Nachrichten — nur Struktur, Zählungen, Anteile. Katalogwerte eines Herstellers nur in die
  Datenbank des Anwenders, nie in die Auslieferung. Google-Drive-Freigaben des Anwenders lassen sich mit
  `curl -L "https://drive.usercontent.google.com/download?id=<ID>&export=download&confirm=t"` laden (`drive.google.com` selbst
  ist gesperrt); große Dateien (> 5 GB) sprengen das Plattenkontingent des Containers.
- **Agenten** mit ausdrücklichem Modell (`opus-umsetzung` für Fachcode, `sonnet-mechanik`, `haiku-pruefung`), je Welle ein
  Worktree, Commits auf dem eigenen Zweig, kein Push. Die Orchestrierung mergt in einem Integrations-Worktree, fährt das Gate
  und pusht.
- **Gate nach dem Merge:** `python3 Werkzeuge/ResourceDesigner/designer_neu.py`, `python3 Werkzeuge/SqlDialektPruefer/pruefer.py
  --db Referenzlaeufe/Kenndaten_Test.sqlite`, Bau `WP-Plan.Kern.slnf` (Release) und Windows-Schale mit
  `-p:EnableWindowsTargeting=true`, Referenzlauf der zwölf CI-Projekte gegen R52 (FAIL nur für nicht gerechnete Projekte ist
  erwartet), `EPOS.UI.Tests` voll, `EPOS.Kern.Tests` mit `--filter` auf die betroffenen Klassen, Werkzeugtests
  `Werkzeuge/Auslieferungsvorlage`, `Formularkarte`, `Gebaeudevergleich` — immer mit
  `-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`. Die CI rechnet den Kern-Filter **ohne** Filter:
  Tests, die feste Zahlen der Kataloge erwarten, auf Konstanten der Saat beziehen.
- **Ressourcenkonflikte** beim Merge (`.resx`, Anhänge am Dateiende): beide Seiten behalten — zuerst die eigene, dann ein
  `</data>`, dann die fremde Seite, wenn hinter dem Konfliktblock das schließende `</data>` steht; danach
  `designer_neu.py schreiben` und die Namen auf Dubletten prüfen.
- **Testdatenbank** nach Merges oft als LFS-Zeiger: `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite`.
- **Push:** `git -c "lfs.https://github.com/inekon/EPOS-Plan.git/info/lfs.locksverify=false" push -q origin
  HEAD:refs/heads/<zweig>` auf `ios_migration_september` und den Sitzungszweig; den doppelten Kern-Lauf des Sitzungszweigs
  abbrechen. Reihenfolge einer Welle: Merge → Gate → Statuszeile und Protokoll → Push → CI-Vermerk `[skip ci]`.
- Keine macOS-, iOS- oder Setup-Läufe, kein Wiki-Upload, keine Pull Requests, kein Force-Push und keine Tags ohne Rückfrage.

## 7. Einstiegs-Prompt

```text
Sitzung „Gebäudesimulation“ (EPOS-Plan, inekon/EPOS-Plan) auf neuem Konto fortsetzen. Arbeitszweig ios_migration_september,
eigener Sitzungszweig wird bei jedem Push mitgezogen. Lies zuerst CLAUDE.md und das Übergabepapier
Dokumentation/aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md (ganz), dann aus
Dokumentation/aktuell/Status_iOS_Migration.md nur den Kopf (Schemaanmeldung) und die Zeilen ab #1010, aus
Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md die Entscheide E128–E131. Prüfe mit
git log --format='%h %<(72,trunc)%s' -n 15 origin/ios_migration_september, welche Wellen aus Abschnitt 3 schon gepusht sind.
Dann arbeite Abschnitt 4 der Reihe nach ab: CI-Vermerke, BK-3 (Import direkt aus dem CSV-Export, E133) mit BK-2b, Wiki, danach BK-5 ff. und nach meinen Entscheiden der NP7-Nachzug.
Orchestriere mit Agenten (Modell ausdrücklich setzen), Reihenfolge Merge → Gate → Statuszeile und Protokoll → Push beider
Zweige → CI-Vermerk [skip ci]. Push auf ios_migration_september und den Sitzungszweig ist erlaubt; keine macOS-, iOS- oder
Setup-Läufe, kein Wiki-Upload, keine Pull Requests, kein Force-Push ohne Rückfrage. Anwenderdateien und Normwerte nie ins
Repositorium. Offene Anwenderfragen stehen in Abschnitt 5 — lege sie mir gesammelt vor, mit Empfehlung.
```
