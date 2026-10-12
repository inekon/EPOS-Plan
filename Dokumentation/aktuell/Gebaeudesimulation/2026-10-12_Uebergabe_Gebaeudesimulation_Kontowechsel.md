# Übergabe der Sitzung „Gebäudesimulation“ — Stand 12.10.2026 (Kontowechsel)

Dieses Papier hält, was eine neue Cloud-Sitzung auf einem anderen Konto braucht, um die Sitzung „Gebäudesimulation“
ohne Verlust fortzuführen: Stand, laufende und nächste Arbeit, offene Anwenderpunkte, Arbeitsweise und am Ende den
Einstiegs-Prompt. Regelquelle bleibt [`CLAUDE.md`](../../../CLAUDE.md) mit den Projekt-`CLAUDE.md`; der dauerhafte Stand steht
in der [Statusdatei iOS-Migration](../Status_iOS_Migration.md) (eine Zeile je Schritt) und in der
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md) (Entscheidungsregister bis E132, Stufentabelle).
Es löst die [Übergabe vom 08.10.2026](../../ueberholt/2026-10-08_Uebergabe_Gebaeudesimulation_Kontowechsel.md) ab und wandert
nach `Dokumentation/ueberholt/`, sobald die Fortsetzung GD-4 abgeschlossen hat.

## 1. Stand bei der Übergabe

| Was | Wert |
|---|---|
| Arbeitszweig | `ios_migration_september` (Remote `origin`). Der Sitzungszweig des alten Kontos `claude/magical-bohr-ff5ufj` wurde bei jedem Push mitgezogen; die neue Sitzung nimmt ihren eigenen Sitzungszweig |
| Letzte Wellen | #1023 BK-1 (gepusht mit [skip ci]), #1020 GD-5, #1022 NP7/NP7b (Schritt 220); davor #1011 NP6, #1012 Quick Assist (Admin-Zweig), #1013 AB2, #1014 AB3, #1015 GD-1, #1016 GD-2, #1017 Entwurf BK — gepusht `f40fe2fa1`; danach Anmeldung Schritt 220 (`6870f7169`) und GD-3 mit diesem Papier (#1019) |
| CI | Kern-Lauf 38195098863 auf `f40fe2fa1` wurde von späteren Pushes abgebrochen (kein Nachweis); maßgeblich ist der jüngste Kern-Lauf auf `ios_migration_september` (beim Schreiben 38198823539 auf `b523a99da`, danach die Pushes von #1020 und #1022); #1010 grün mit Lauf 38188012085 auf `228a10287`; Vermerke für #1010, #1011, #1013–#1017, #1019, #1020 und #1022 stehen noch aus (Abschnitt 4 a) |
| Referenzbasis | **R52** `Referenzlaeufe/2026-10-11_R52_Vorheizen`, dreißig Projekte; CI-Auswahl 1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051, 1058, 1060, 1063, 1065. Nächste freie Basis **R53** (vorgesehen für SA4) |
| Testdatenbank | Schemastand **220** (Git LFS) |
| Schemakette | 218 `KaelteArtSchema` (Sitzung Kälteanlagen), 219 `RaumnutzungNormkategorienSchema` (NP6), 220 `RaumnutzungNormwerteSchema` (NP7); **221 frei** |
| Statuszeilen | höchste vergebene #1023; vor jeder neuen Zeile Tabelle und Kopf auf Reservierungen anderer Sitzungen prüfen. Entscheide bis **E132**, **E133 frei** |
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

## 3. Laufende Arbeit beim Schreiben

Keine: GD-5 (#1020) und NP7 mit NP7b (#1022, Schritt 220) sind gepusht, kein Agent läuft. Festlegung aus NP7, die
Folgewellen kennen müssen: **Außenluft je Person** ist eine eigene Spalte `Aussenluft_Person` (m³/(h·Person)) neben
`Aussenluft`/`Aussenluft_Einheit`; im Generator geht die raum- oder flächenbezogene Außenluft vor.

Die Routine „CI-Vermerk Welle f40fe2fa1“ des alten Kontos feuert in die alte Sitzung; die neue Sitzung übernimmt ihren Inhalt
selbst (Abschnitt 4 a).

## 4. Nächste Arbeit (Reihenfolge)

1. **CI-Vermerke:** jüngsten Kern-Lauf auf `ios_migration_september` prüfen, der `f40fe2fa1` bzw. den GD-3-Push enthält; grün →
   in #1010 „CI-Kern-Lauf 38188012085 grün auf `228a10287`“, in #1011, #1013–#1017, #1019, #1020 und #1022 Lauf-ID und Commit eintragen, Commit `[skip ci]`.
   Rot → `get_job_logs` mit `failed_only`, Ursache eingrenzen, beheben.
2. **NP7-Nachzug:** Nummern 70/71 der Projektdatei auf die Wohnprofile (nach Anwenderentscheid, Abschnitt 5).
3. **GD-4 „Als Mehrzonenmodell übernehmen“** (Entwurf Abschnitt 7, nach GD-3 und AB3 frei): Knopf, Neulesen der gespeicherten
   Importquelle mit Prüfsumme (E130 F7), Zonenbaum als Blatt, Schreiben in das bestehende Gebäude mit Ersetzen in einem Vorgang;
   `fensterprobe.mjs`.
4. **Wiki und Logbuch** der Wellen GD, AB, NP6/NP7, KS (Seiten „Gebäude“, „Gebäudeimport“, „Nutzungsprofile“); Upload nur
   gebündelt und auf Zuruf, Versionsnummer beim Anwender erfragen.
5. **BK** (E132 entschieden, Entwurf Abschnitt 6a; BK-1 Editoren gebaut, #1023): BK-2 Schemaschritt mit allen
   Feldern der Quelle, Herkunft, Gruppe, Klasse und Kennzeichen (ohne Löschregel der Auslieferung), BK-3 Austauschformat und
   Import mit Zusammenführung der Dubletten, BK-4 LocalDB-Werkzeug (Windows), BK-5 inhomogene Schichten, BK-6 Fenster im
   Aufbaukatalog, BK-7 Wiki. Rund 12–16 PT.
   Grundlage für BK-2: [Feldinventar](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-12_BK_Feldinventar_Hottgenroth.md)
   mit Vorschlag der zu übernehmenden Felder, [Strukturbericht](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-12_BK_Strukturbericht_Hottgenroth.md);
   die `.mdf` und der Export liegen nur beim Anwender (lokale Sitzung „iOS migration merge conflicts“, `C:\Temp\Hsetu\`).
6. **Übernommen von der Sitzung „Anlagendialog/Kältebedarf“:** (a) Sprung der Kachel Gebäudekühlung (K5-Teil,
   [`Konzept_Kaeltebedarf_EPOS-Plan.md`](../Konzept_Kaeltebedarf_EPOS-Plan.md) Abschnitt 3.5): Sprungziel `Sprungziele.Kuehlung` im
   Kern neben `Masken`, Aufruf über `OeffneMaske(Masken.…, args)`; der Projektgebäudedialog wählt das erste bzw. zuletzt gewählte
   Gebäude, klappt die Gruppe „Kühlung“ auf und rollt sie in Sicht (kleines JS-Modul, Fokus aufs erste Feld); ohne
   Projektgebäude Banner „Noch kein Gebäude im Projekt“; die Zustandszeile der Kachel stammt aus K4 und bleibt. (b) Zeile
   Jahreswärmebedarf der Gebäudezusammenfassung (Abschnitt 5). Beides mit GD-4 oder einer GD-Nachwelle.
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
Dann arbeite Abschnitt 4 der Reihe nach ab: CI-Vermerke, GD-4, Wiki, danach BK nach E132 und nach meinen Entscheiden der NP7-Nachzug.
Orchestriere mit Agenten (Modell ausdrücklich setzen), Reihenfolge Merge → Gate → Statuszeile und Protokoll → Push beider
Zweige → CI-Vermerk [skip ci]. Push auf ios_migration_september und den Sitzungszweig ist erlaubt; keine macOS-, iOS- oder
Setup-Läufe, kein Wiki-Upload, keine Pull Requests, kein Force-Push ohne Rückfrage. Anwenderdateien und Normwerte nie ins
Repositorium. Offene Anwenderfragen stehen in Abschnitt 5 — lege sie mir gesammelt vor, mit Empfehlung.
```
