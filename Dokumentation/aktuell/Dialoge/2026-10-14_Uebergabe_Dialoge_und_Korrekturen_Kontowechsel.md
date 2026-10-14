# Übergabe der Sitzung „Dialoge und Korrekturen (Übersicht Berichte & Kosten)“ (14.10.2026)

Dieses Papier übergibt die Sitzung „Dialoge und Korrekturen (Übersicht Berichte & Kosten)“
(`session_01Mt7MsJGzcGr77yqBsxwpLj`) an eine Sitzung auf einem anderen Konto. Es nennt den Stand, die
geltenden Entscheide, die Regeln der Sitzung über `CLAUDE.md` hinaus, die Cloud-Umgebung und die offenen
Punkte; der Einstiegs-Prompt steht in Abschnitt 5. Regelquelle bleibt `CLAUDE.md`. Es löst das Papier
[`2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md`](../../ueberholt/2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md)
ab; dessen Entscheide gelten weiter und werden hier nicht abgeschrieben. Den gemeinsamen Stand mit der
Anlagendialog-Sitzung führt der Abschnitt dieser Sitzung im Papier
[`2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md`](../../ueberholt/2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md)
(Punkte 10 bis 13).

## 1 Stand

**Auf origin, mit grünem CI (Kern-Lauf auf `claude/dialoge-korrekturen-okt`):**

| Statuszeilen | Inhalt | Nachweis |
|---|---|---|
| #1033, #1035, #1036 | Übersicht Berichte & Kosten (Vergleich je Gewerk), Kältemaschine als Gewerk, Einheiten in Spaltenköpfen samt Leistungspreis €/(kW·a) | vermerkt |
| #1067–#1072 | Übersicht: Detail-Unterschiede nur für Varianten, Variantendialoge (Anlegen aus Stamm oder Variante, Umbenennen, Löschen), Ergebnisse — Energie, „Anzeige anpassen…“ je Benutzer, Grafik „Tabelle \| Grafik“, Darstellungsprüfung | Run 38479986896 grün |
| #1073–#1075 | Diagramme: Oberflächenregel (Bedarf Linie, Erzeugung Fläche, Tagesband), Achse „Datum \| Stunde im Jahr“ an allen Jahresdiagrammen, Schalter „PV aufteilen“; Kosten-Einheiten (Preisbasis €/kWh bei neuer Auswahl) | Run 38479986896 grün |

**Auf origin, mit grünem CI:**

| Statuszeilen | Inhalt | Nachweis |
|---|---|---|
| #1086 | Gesetzeskatalog: Einheit vorbelegt (Schlüssel, sonst Bereich, Energiesteuer Pflichtwahl), EEG und Umlagen wählbar | Run 38518439346 grün |
| #1087 | Konzept und Mockup Kältebedarfsprofile, Entscheide OE-1 bis OE-6 | Run 38518439346 grün |
| #1088 | KBP-1: Baustein `Betriebskalenderwahl`, „Aus Gebäude übernehmen…“ | Run 38518439346 grün |

Zuletzt gepusht auf alle drei Zweige: `136db6d49`; danach allein auf `ios_migration_september` die
Anmeldung des Schemaschritts 230 (`ecc5c307c`, `[skip ci]`).

**Fertig, nicht auf `ios_migration_september` (wartet auf die Schemaschritte 228 und 229):**
KBP-2 (Schemaschritt 230 `KaeltebedarfsprofilVorlagenSchema`: `Z_Projekt_Kaeltebedarf.Eingabeart`,
`Kaelte_Art` an `Tab_Kaeltebedarf(_STAMM)`, Saat der Vorlagen 7–16), KBP-3a (Eingabearten Monatswerte und
Wochenprofil, Art-Vorschlag, Mitzieher), KBP-3b (Grafikblätter Jahresgang, Monate, Woche, Dauerlinie,
Kennzahlenzeile) und der Quellen-Nachzug (Nutzungszeiten nach DIN/TS 18599-10:2025-10, Labor Profil 38,
Prozesskälte einschichtig nach den Hallenprofilen 22–24). Geparkt auf `origin/claude/elegant-cerf-qdr6kp`
(dessen Kopf; Prüfmerkmal: der Zweig enthält `EPOS.Kern/Allgemein/Update/KaeltebedarfsprofilVorlagenSchema.cs`;
Commits mit `[skip ci]`, weil die Testdatenbank dort noch auf 227 steht).

**Schemakette** (Kopf der Statusdatei, abgestimmt 14.10.2026 07:14 UTC): 228 `GeraetekostenEntfallSchema`
(Sitzung Kältemaschinen, KB-4) → 229 `StromspeicherKostenSchema` (Sitzung Kältemaschinen, KB-5) → **230
`KaeltebedarfsprofilVorlagenSchema` (diese Sitzung)** → 231 `RaumnutzungDinWerteSchema` (Gebäudesimulation)
→ 232 `BauteilkatalogAbschnittSchema` (Gebäudesimulation). Referenzbasis zuletzt
`2026-10-14_R55_KaeltebedarfDezentral` (Referenzlauf byte-gleich in allen Wellen dieser Sitzung).

## 2 Geltende Entscheide des Anwenders (13./14.10.2026)

- **Modellwahl:** Fable nur für Orchestrierung und sehr komplexe Analysen; Opus 5.5 für Fachcode,
  Dialoge, Tests und Konzepte mit Fachinhalt; Sonnet für Merges, Gates, Status, Push, CI und Wiki;
  Haiku für Zählungen. Höchstens zwei Agenten parallel. Das Modell bei jedem Agentenaufruf setzen.
- **Übersicht Berichte & Kosten:** „Detail-Unterschiede für Variante“ enthält nur Varianten, Vorgabe
  Komponentenvergleich; Variantendialoge statt Bezeichnerfeld; Ergebnisse nur Energie (Kosten gehören zur
  Wirtschaftlichkeit), CO₂ zugeklappt; Anzeige je Benutzer für alle Projekte; Grafik als Option.
- **Diagramme:** überdeckende Reihen im Oberflächenmodus auflösen; X-Achse mit zwei Skalen „Datum“
  (ohne Jahr) und „Stunde im Jahr“ (im gezeigten Ausschnitt) an allen Jahresdiagrammen; PV-Reiter mit
  Schalter „PV aufteilen“. Word-Bericht und ChartProben bleiben bitgleich.
- **Kosten-Einheiten:** Vorbelegung €/kWh (Arbeitspreis) bzw. €/(kW·a) (Leistungspreis); gespeicherte
  Einheiten bleiben.
- **Gesetzeskatalog:** Empfehlung A (Einheit aus gleichem Schlüssel, sonst häufigste des Bereichs,
  Energiesteuer Pflichtwahl).
- **Kältebedarfsprofile:** OE-1 bis OE-6 nach Empfehlung (Konzept Abschnitt 11): Typtage als eigene Welle,
  keine Witterungsführung im Rechenweg, alle zehn Vorlagen, Kalenderbaustein zuerst, Eingabearten danach
  auch für Prozesswärme und Brauchwasser, „Aus Gebäude übernehmen…“ als Kopie.
- **Dialogwünsche** gehen zuerst als Mockup, außer der Anwender sagt „direkt umsetzen“.

## 3 Regeln der Sitzung (über `CLAUDE.md` hinaus)

**Wellenfolge:** Umsetzung durch Opus im Worktree (Auftrag mit Abnahme, höchstens rund 150 Werkzeugaufrufe)
→ Welle durch Sonnet im Worktree: Merge von origin und der Zweige → Build → Restgate
`bash Werkzeuge/Gate/gate_rest_linux.sh . <scratchpad-ablage>` (rund 15 Minuten, ohne Kern-Tests) plus
gefilterte Kern-Tests → Statuszeilen (Nummer unmittelbar vor dem Commit gegen origin messen) →
Logbuchsatz im Entwurf `Dokumentation/aktuell/Wiki_Update_2026-09-26.md` → Push auf
`ios_migration_september`, den eigenen Sitzungszweig und `claude/dialoge-korrekturen-okt` (dieser nur
Fast-Forward) → CI-Lauf auf `claude/dialoge-korrekturen-okt` (rund 110 Minuten; ein neuer Push auf
denselben Zweig bricht den laufenden ab) → CI-Vermerk durch Sonnet mit `[skip ci]` im Betreff.
Kennung der Statuszeilen: „Sitzung „Dialoge und Korrekturen (Übersicht Berichte & Kosten)“, <Sitzung>,
<Modell> im Worktree“.

**Abstimmung mit anderen Sitzungen:** Nachrichten gehen als einmalige Routine
(`create_trigger` mit `persistent_session_id`, `run_once_at` zwei Minuten voraus, ohne `connectors`);
das geht nur innerhalb desselben Kontos. Liegt die neue Sitzung auf einem anderen Konto, laufen
Absprachen über den Kopf der Statusdatei, die Übergabepapiere und den Anwender. Partner: Anlagendialog
(`session_01UNeGChCYZu2EpgFx4P3q1q`, Kältebedarf K6, Berichtskatalog Fassung 23), Kältemaschinen
(`session_0178YLFEbS4xWdGaXFaZqG1r`, Kosten KB-2 bis KB-6, Schemaschritte 228/229),
Gebäudesimulation (`session_01NPHEwQvnQDiWMWnQbMmEou`, Schemaschritte 231/232),
Zapfprofilgenerator (`session_017jJMrbTBmVq3vxUtLN4evQ`, ändert bis 20.10. keinen Code).

**Bindende Bitten der Partner:** Feldgruppe „Deckung“ und Naht
`SimulationPlattformwege.KaelteDezentralBearbeiten` unverändert; `BedarfsProfileHuelle.Oeffnen(...)` mit
Pflicht-Schreibweg und `Projektkopievormerkung.Schliessen(...)` unverändert (Wachen
`BedarfsprofilVormerkungWacheTests`, `BedarfsprofilZuordnungVormerkungTests`); Kältevorlagen 1–6 bytegleich
(Referenzprojekt 1072); Kältebilder bleiben Stapel; Berichtsvorlagen und Kurzberichte nicht neu erzeugen;
`Jahresraster.razor` (Kalenderkarte des Zapfprofils) bekommt keine Achswahl; `.resx` hinten anhängen,
CSS vor „FORMULARRASTER“ ohne Hex-Farben.

**Fallstricke:** `.resx`-Konflikte verschlucken ein `</data>` (nach dem Auflösen XML prüfen, Designer neu
erzeugen); in Worktrees ist die Testdatenbank oft ein LFS-Zeiger
(`git lfs pull --include Referenzlaeufe/Kenndaten_Test.sqlite`); Worktree-Agenten dürfen keine Heredocs und
zusammengesetzten Befehle, Skripte liegen als Dateien im Scratchpad; der Scratchpad wird geteilt, eigene
Dateien tragen ein Präfix; Agenten laufen nach dem Bericht manchmal weiter (`TaskStop`); `<datalist>`
ist im Hausblatt verboten; ChartProben weichen auf dem Container wegen der Schrift von der Messlatte ab,
maßgeblich ist der Vergleich vorher/nachher auf demselben Container.

### Cloud-Umgebung

- `.NET`: `dotnet --version` muss 10.0.400 oder neuer zeigen (in dieser Umgebung unter `/root/.dotnet`);
  fehlt es, den conda-forge-Weg aus Abschnitt 3 des abgelösten Papiers gehen.
- GitHub: `gh api …` (eingebauter Client) und die GitHub-MCP-Werkzeuge; CI-Abfrage z. B.
  `gh api repos/inekon/EPOS-Plan/actions/runs/<id> --jq '.status+" "+(.conclusion//"")'`.
- Playwright: global unter `/opt/node22/lib/node_modules/playwright`, Chromium unter
  `/opt/pw-browsers/chromium-1134`; nie `playwright install`.
- WordPress-Konnektor (Wiki-Upload) war in dieser Sitzung nicht verbunden; ein Upload geschieht nur auf
  Zuruf.

**Nie ohne Rückfrage:** iOS-, macOS- und Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags.
**Antworten:** auf Deutsch, knapp.

## 4 Offene Punkte (mit Empfehlung)

1. **Welle 5 (sobald 228 und 229 auf origin liegen; Ankündigung der Kältemaschinen-Sitzung):** den
   geparkten Stand übernehmen, die Kette von 230 auf `StromspeicherKostenSchema` umhängen (Kommentar
   `// KETTE:` in `KaeltebedarfsprofilVorlagenSchema.cs`, erste Assertion in
   `KaeltebedarfsprofilVorlagenSchemaTests.Nummer_Kette_und_Register`, Reihenfolge in `TestDatenbank.cs`
   und `Werkzeuge/Testdatenbankschema`), die Testdatenbank auf der 229-Datei der Kältemaschinen-Sitzung
   auf 230 heben (LFS, dann wird `TestdatenbankSchemastandWacheTests` grün), Gate, Statuszeilen KBP-2,
   KBP-3a, KBP-3b und Quellen, Logbuchsätze, Push auf drei Zweige; danach der Gebäudesimulation den SHA
   melden, damit sie 231 umhängt. Empfehlung: Opus für Umhängen und Datenbankhebung, Sonnet für den Rest.
2. **Nacharbeit KBP-3b** (in Welle 5 oder danach, Opus): Die Grafik der Kältezeile folgt Jahressumme und
   Kalender des Arbeitsstands, Monatswerte und Wochenprofil gehen erst nach dem Speichern ein (die Vorschau
   kennt keine Übersteuerung dieser Form; die Herleitungszeile sagt es); die Woche zeigt Tagesarten als
   Reihen statt als farbigen Hintergrund (`--epos-kalb-*`), eine Kalenderwoche ist nicht wählbar; bei
   ungespeichertem Kalenderwechsel weichen Woche und Stundenreihe ab; Probe-Ansicht `kaelteprofile` in
   `Fensterprobe.razor` und `Proben/Rasterprobe/LIESMICH.md` um die Grafik ergänzen. Vorlage 8
   (Prozesskälte einschichtig) rechnet Mo–Fr (260 d) gegen 230 d der Norm; angleichen nur mit einem
   Entscheid zu Feiertagen oder Ferien.
3. **KBP-4 Typtage** (Konzept 3.4, 8.2): Schemaschritt B (`Tab_Kaeltetyptag`, `Tab_Kaeltetyptag_Matrix`);
   die nächste freie Nummer vor dem Bau im Statuskopf anmelden und allein pushen.
4. **KBP-5:** Eingabearten für Prozesswärme und Brauchwasser, Wiki-Quelle der Bedienungsseite,
   Logbuchsatz; danach das Konzept per `git mv` nach `Dokumentation/ueberholt/`.
5. **Kleinkram:** Probe-Ansicht `kaelteprofile` in die Fallliste von `fensterprobe.mjs`; Gebäude mit mehr als
   vier Ferienzeiträumen im Kern-Test des Betriebskalenders, sobald die Testdatenbank eine Ferienliste führt.
6. **Beim Anwender:** Versionsnummer der Logbuchsätze im Entwurf; Sichtprüfung unter Windows (Übersicht,
   Diagramme samt PV-Schalter, Zapfprofil-Vorschau in drei Stufen, Gesetzeskatalog, Kalenderbaustein,
   Kältebedarfsprofile); gemessene Kälte-Lastgänge, um die Richtwerte der Vorlagen (Tagesgang, Monate,
   Grundlast) zu ersetzen; Wiki-Sammelupload frühestens 20.10., nur auf Zuruf.

## 5 Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Dialoge und Korrekturen (Übersicht Berichte & Kosten)“ für EPOS-Plan
(Repo inekon/EPOS-Plan, Arbeitszweig ios_migration_september). Lies zuerst CLAUDE.md und
Dokumentation/aktuell/Dialoge/2026-10-14_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md,
dann den Kopf (Schemaschritt angemeldet, Referenzbasis angemeldet) und die letzten zwanzig Zeilen der
Tabelle in Dokumentation/aktuell/Status_iOS_Migration.md sowie
`git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september`.

Rolle: Du orchestrierst. Umsetzung an opus-umsetzung (model: opus), Merges, Gates, Status, Push, CI
und Wiki an sonnet-mechanik (model: sonnet), Zählungen an haiku-pruefung (model: haiku); Fable nur
für sehr komplexe Analysen. Höchstens zwei Agenten parallel, jeder im eigenen Worktree. Wellenfolge,
Regeln, Fallstricke und Partner stehen in Abschnitt 3 des Papiers und gelten vollständig. Gepusht
werden drei Zweige: ios_migration_september, dein Sitzungszweig und claude/dialoge-korrekturen-okt
(nur Fast-Forward; dort läuft der CI-Nachweis). Trailer: Co-Authored-By des arbeitenden Modells und
Claude-Session mit der URL deiner Sitzung. Nie ohne Rückfrage: iOS-, macOS-, Setup-Läufe,
Wiki-Upload, Pull Requests, Force-Push, Tags. Antworten auf Deutsch, knapp.

Umgebung prüfen: `dotnet --version` (mindestens 10.0.400), Testdatenbank kein LFS-Zeiger, `df -h`,
`gh api repos/inekon/EPOS-Plan --jq .full_name`.

Erste Schritte: (1) Stand prüfen: geparkter Kopf auf origin/claude/elegant-cerf-qdr6kp (Abschnitt 1)
vorhanden, keine Konfliktmarker, keine liegen gebliebene AGENT_LAEUFT. (2) Entfällt (CI-Vermerke gesetzt). (3) Prüfen, ob 228 GeraetekostenEntfallSchema und 229 StromspeicherKostenSchema auf origin
liegen; wenn ja, Welle 5 nach Abschnitt 4 Punkt 1 starten. (4) Den Stand in höchstens fünf Zeilen
melden und mit dem Anwender die Punkte aus Abschnitt 4 abstimmen.
```
