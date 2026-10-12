# Übergabe der Sitzung „Gebäudesimulation“ — Stand 12.10.2026 (Kontowechsel)

Dieses Papier hält, was eine neue Cloud-Sitzung auf einem anderen Konto braucht, um die Sitzung „Gebäudesimulation“
ohne Verlust fortzuführen: Stand, laufende und nächste Arbeit, offene Anwenderpunkte, Arbeitsweise und am Ende den
Einstiegs-Prompt. Regelquelle bleibt [`CLAUDE.md`](../../../CLAUDE.md) mit den Projekt-`CLAUDE.md`; der dauerhafte Stand steht
in der [Statusdatei iOS-Migration](../Status_iOS_Migration.md) (eine Zeile je Schritt) und in der
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md) (Entscheidungsregister bis E134, Stufentabelle).
Es löst die [Übergabe vom 08.10.2026](../../ueberholt/2026-10-08_Uebergabe_Gebaeudesimulation_Kontowechsel.md) ab und wandert
nach `Dokumentation/ueberholt/`, sobald die Fortsetzung den ersten Importlauf, BK-2b und die GD-Nachzüge abgeschlossen hat.

## 1. Stand bei der Übergabe

| Was | Wert |
|---|---|
| Arbeitszweig | `ios_migration_september` (Remote `origin`). Der Sitzungszweig des alten Kontos `claude/magical-bohr-ff5ufj` wurde bei jedem Push mitgezogen; die neue Sitzung nimmt ihren eigenen Sitzungszweig |
| Letzte Wellen | #1032 BK-2b Oberfläche und Bänder, #1031 BK-3 Import, #1030 BK-2 (Schritt 221), #1026 GD-4, #1023 BK-1, #1020 GD-5, #1022 NP7/NP7b (Schritt 220); davor #1011 NP6, #1012 Quick Assist (Admin-Zweig), #1013 AB2, #1014 AB3, #1015 GD-1, #1016 GD-2, #1017 Entwurf BK — gepusht `f40fe2fa1`; danach Anmeldung Schritt 220 (`6870f7169`) und GD-3 mit diesem Papier (#1019) |
| CI | Kern-Lauf 38206954119 grün auf `02d4ddfe0` (enthält alles bis #1022); #1010 grün mit 38188012085 auf `228a10287`; vermerkt in #1010, #1011, #1013–#1017, #1019, #1020, #1022. #1023 (BK-1) und die Papiere danach sind mit [skip ci] gepusht und laufen mit dem nächsten Push ohne [skip ci] durch die CI |
| Referenzbasis | **R52** `Referenzlaeufe/2026-10-11_R52_Vorheizen`, dreißig Projekte; CI-Auswahl 1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051, 1058, 1060, 1063, 1065. Nächste freie Basis **R53** (vorgesehen für SA4) |
| Testdatenbank | Schemastand **220** (Git LFS) |
| Schemakette | 218 `KaelteArtSchema` (Sitzung Kälteanlagen), 219 `RaumnutzungNormkategorienSchema` (NP6), 220 `RaumnutzungNormwerteSchema` (NP7); 221 `BauteilkatalogSchema` (BK-2); **222 frei** |
| Statuszeilen | höchste vergebene #1032; vor jeder neuen Zeile Tabelle und Kopf auf Reservierungen anderer Sitzungen prüfen. Entscheide bis **E134**, **E135 frei** |
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
| BK-2 | #1030 | Schritt 221: Herkunft, Quellkennung, Gruppe/Klasse, Kennzeichen „meist nicht geeignet“, Felder der Quelle, Abschnitte |
| BK-3 | #1031 | „Bauteilkatalog einlesen…“ direkt aus dem CSV-Export: Leser, Regeln, Zusammenführung, Vorschau, Dialog |
| BK-2b | #1032 | Gruppe, Klasse, Herkunft und Kennzeichen in Katalogen und Bauteilwahl; Bänder des Rechenwegs ohne Ausnahme, Filter „nur geeignete“ |

## 3. Laufende Arbeit beim Schreiben

Kein Agent läuft. **GD-6a** wurde beim Kontowechsel mitten im Bau angehalten; seine Arbeit ist verworfen und wird mit dem
Auftrag in Abschnitt 4 neu beauftragt. Die Routinen des alten Kontos sind gelöscht. Festlegungen, die Folgewellen kennen müssen:

- **Außenluft je Person** (NP7) ist eine eigene Spalte `Aussenluft_Person` (m³/(h·Person)) neben `Aussenluft`/`Aussenluft_Einheit`;
  im Generator geht die raum- oder flächenbezogene Außenluft vor.
- **Bauteilkatalog** (BK-2): Gruppe und Klasse sind Code-Spalten mit CHECK (Texte zweisprachig), Abschnitte stehen in
  `Tab_Bauteilabschnitt(_STAMM)`, das Kennzeichen `Simulation_Ungeeignet` setzt nur der Kern; importierte Sätze tragen
  Herkunft IMPORT, Quelle und Quellkennung und bleiben in der Auslieferung.

## 4. Nächste Arbeit (Reihenfolge)

1. **CI-Vermerk** für #1023 (BK-1), #1026 (GD-4), #1030 (BK-2), #1031 (BK-3) und #1032 (BK-2b) mit dem jüngsten Kern-Lauf auf
   `ios_migration_september`; die Pushes danach trugen `[skip ci]`. Rot → `get_job_logs` mit `failed_only`, Ursache eingrenzen,
   beheben.
2. **GD-6a** (Anwenderwünsche 12.10.2026), Auftrag an `opus-umsetzung`, Datei `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor`:
   - „Simulation nur von ‚Im Projekt‘ – alle Gebäude oder Auswahl (auswählbar machen)“: Knopf „Simulation…“ in die Kopfleiste
     des Abschnitts IM PROJEKT (neben „In die Datenbank übernehmen…“, „Aus dem Projekt entfernen“); ohne Wahl alle Gebäude des
     Projekts („Simulation… (alle n Gebäude)“), sonst die angekreuzten („(m gewählt)“); Mehrfachwahl der Projektzeilen mit
     Kopfkästchen, ohne die Satzwahl des Katalogs zu stören; nicht im Assistenten, nicht in der Katalogverwaltung. Ursache:
     AW3b (`de9bbc779`, „eine Wahl je Zeile, Knopfleiste am Satz“) — der Knopf (`BedarfGaben`, `BeiSimulation`) steht seitdem
     nur noch in der Satz-Überlagerung hinter „⤢“ am Projektsatz (`epos-gebaeude-satzknoepfe`) und wird nicht gefunden; prüfen, ob die Windows-Schale `BedarfGaben` überhaupt durchreicht (Startseite › Gebäude,
     `GebaeudeFenster`), und ob Export, Neulesen, Ausrichtung ebenso unerreichbar wurden.
   - „Baustoff-Zuordnungen…“ aus der Leiste des Projektsatzes nehmen; anbieten im Gebäudeimport an der Liste
     „Bauteilaufbauten“ und im Gebäudeeditor (`GebaeudeKatalogDialog.razor`) bei Bauteilen/Aufbauten.
   - bunit, Zählwachen, Hilfeziele; Kurzprotokoll `2026-10-12_GD-6a_Knopfleiste.md`.
2a. **AB4** (Anwenderwunsch 12.10.2026, Wortlaut: „Ergänzung ifc-viewer: Es soll möglich sein einzelne Komponenten in der
   Körper-Ansicht ein- und auszublenden (Zonen, Randbedingungen, Aufbau). Evtl. ein Select-Button mit zuordnen und
   Ein/Ausblenden?“): In der Gebäudeansicht (`EPOS.UI/Bausteine/GebaeudeAnsicht.razor` samt JS-Modul) eine Werkzeugwahl
   „Zuordnen | Ein-/Ausblenden“ an der Legende: im Modus Zuordnen wie AB2 (Legendensymbol wählt das Ziel, Klick auf Räume ordnet
   zu), im Modus Ein-/Ausblenden schaltet ein Klick auf einen Legendeneintrag (Zone, Randbedingung bzw. Aufbau/Befund je
   Farbmodus) dessen Körper unsichtbar/sichtbar, dazu „alle zeigen“; ergänzt (Wortlaut: „Es soll möglich sein, einzelne
   Bauteilgruppen ein- und auszublenden.“): zusätzlich je Bauteilgruppe (Außenwand, Innenwand, Dach, Decke, Boden, Fenster, Tür
   u. a. nach Bauteilart) ein- und ausblenden, unabhängig vom Farbmodus, z. B. als Kästchenliste „Bauteilgruppen“ neben der
   Legende; Sichtbarkeit nur Anzeige, nichts wird gespeichert;
   `diagrammprobe`/Rasterprobe nach `Proben/Rasterprobe/LIESMICH.md`, bunit. Auftrag an `opus-umsetzung`.
3. **BK-3b** Nachzug am Import nach E134 (Auftrag an `opus-umsetzung`, Datei
   `EPOS.Kern/Allgemein/Import/Bauteilkatalog/BauteilkatalogImport.cs` samt Tests): Code-Tafel nach der Deutung im
   [Protokoll BK-3](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-12_BK-3_Import.md) — Stoffgruppen 1 Putz/Mörtel/Estrich,
   2 Beton, 3 Dämmstoffe mit Bauplatten, 4 Mauerwerk, 5 Dämmstoffe, 6 Holz, 7 Beläge/Abdichtungen, 8 Sonstige, 0/leer über
   TcBasis sonst Sonstige; Basis-Codes 14, 19, 29, 30 → Gruppe „Sonstige“, Klasse „Haustechnik“ (19 an Aufbauten: Luftdurchlass,
   gekennzeichnet); 15|0 und 16|0 → Wand; λ > 10 nur bei ρ ≥ 2 500 kg/m³; Nichtmetalle mit λ > 60 und λ/1 000 im Band 0,02–0,5
   durch 1 000 teilen und im Protokoll nennen, übrige ablehnen. Danach **erneuter Lauf beim Anwender** („Bauteilkatalog
   einlesen…“ ordnet per Quellkennung um, keine Dubletten) und Zahlen ins Protokoll.
4. **GD-Nachzüge** (offene Punkte von GD-4, #1026): gespeicherten Zonenplan eines früheren Imports vorlegen; beim Weg
   „Gebäudedatei + Projektdatei“ auch die Projektdatei prüfen; Sprung zur Kühlung auf das zuletzt gewählte Gebäude; eigener
   Maskenschlüssel für `Sprungziele.Kuehlung`; Zeile Jahreswärmebedarf nach Anwenderentscheid (Abschnitt 5).
5. **NP7-Nachzug:** Nummern 70/71 der Projektdatei auf die Wohnprofile (nach Anwenderentscheid, Abschnitt 5).
6. **Wiki und Logbuch** der Wellen GD, AB, NP6/NP7, KS, BK (Seiten „Gebäude“, „Gebäudeimport“, „Nutzungsprofile“, „Baustoffe“,
   „Bauteilaufbauten“, „Bauteilkatalog einlesen“); Upload nur gebündelt und auf Zuruf, Versionsnummer beim Anwender erfragen.
7. **BK-5 bis BK-7** (Entwurf Abschnitte 6a und 7): inhomogene Schichten nach DIN EN ISO 6946 (Abschnitte in Editor,
   Projektkopie `CopyFromStamm`, Katalogpaket), Fenster ans Bauteil, Teilkatalognamen aus `Katalog_Inhalt.csv`, Hilfeschlüssel
   des Importdialogs; BK-4 LocalDB-Werkzeug nur für einen neuen Katalogstand.
8. **SA4:** Referenzprojekt mit Schnellaufheizung, Basis R53, Wiki (Stufentabelle SA).

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
Sitzung „Gebäudesimulation“ (EPOS-Plan, inekon/EPOS-Plan) auf neuem Konto fortsetzen. Arbeitszweig ios_migration_september;
der eigene Sitzungszweig wird bei jedem Push mitgezogen.

1. Lies CLAUDE.md und das Übergabepapier
   Dokumentation/aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md ganz; aus
   Dokumentation/aktuell/Status_iOS_Migration.md nur den Kopf (Schemaanmeldung) und die Zeilen ab #1019; aus
   Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md die Entscheide E128–E134. Weitere Papiere nur gezielt.
2. Prüfe mit git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september, was seit dem Papier hinzukam.
3. Arbeite Abschnitt 4 der Reihe nach ab: CI-Vermerk, GD-6a, BK-3b (danach Anwender um den erneuten Importlauf bitten),
   GD-Nachzüge, Wiki, BK-5 ff. GD-6a und BK-3b berühren verschiedene Dateien und dürfen parallel laufen.
4. Arbeitsweise: Orchestrierung mit Agenten (opus-umsetzung für Fachcode, sonnet-mechanik für Status/Protokolle, Modell immer
   ausdrücklich setzen), je Welle ein Worktree; Merge im Integrations-Worktree, Gate nach Abschnitt 6, Statuszeile (nächste
   freie Nummer vor dem Push gegen origin prüfen — andere Sitzungen vergeben laufend Nummern), Push auf
   ios_migration_september und den Sitzungszweig; doppelten Kern-Lauf des Sitzungszweigs abbrechen; reine Papier-Pushes mit
   [skip ci], damit ein laufender CI-Lauf nicht abbricht. Übergabepapier bei jedem Push nachführen.
5. Grenzen: keine macOS-, iOS- oder Setup-Läufe, kein Wiki-Upload, keine Pull Requests, kein Force-Push ohne Rückfrage.
   Anwenderdateien (.sqproj, .mdf, CSV-Export) und Normwerte nie ins Repositorium; aus ihnen nur Struktur und Zählungen.
   Die lokale Sitzung „iOS migration merge conflicts“ auf dem Rechner des Anwenders kann den Hottgenroth-Export auswerten
   (nur Kategorien und Zählungen zurückmelden lassen).
6. Offene Anwenderfragen (Abschnitt 5) gesammelt mit Empfehlung vorlegen; kurze Antworten in Stichpunkten, Tabellen für
   Stände.
```
