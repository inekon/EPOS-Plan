# Übergabe der Sitzung „EPOS Plan Wirtschaftlichkeit" — Stand 03.10.2026 (fortgeschrieben aus den Fassungen vom 27.09., 30.09. und 02.10.2026)

Diese Datei ist der Einstieg für die Fortführung in einer neuen Sitzung, gleich ob Cloud (claude.ai/code), Desktop-App oder ein
anderes Claude-Konto. Sie ersetzt das maschinenlokale Gedächtnis der bisherigen Sitzung; alles Fachliche steht in Statusdatei,
Protokollen, Register und Konzept. Das Gedächtnis der Desktop-App liegt je Rechner und Windows-Benutzer unter
`C:\Users\<Benutzer>\.claude\projects\C--Waermeplan\memory\`, nicht je Konto — auf demselben Rechner steht es einem anderen Konto zur Verfügung.

## 1 Stand

- **Alle Etappen des Konzepts sind gebaut und gepusht:** E0–E10, E13–E31 (E11 entfällt, E12 = Wiki-Sammel-Upload #556), dazu die
  Nachlese-Wellen #603, #612, #633 (bis 30.09.) und am 02.10.2026 vier Wellen: **#645** (P641: Laufvermerk `Lauf_Staende` je
  Ergebnis, Spalte additiv über `StelleTabellenSicher`, Testdatenbank nachgezogen; EZ‑19), **#650** (P646: Nachlese der fremden
  Welle #642 Wärmegestehung nach fachlicher Prüfung — Stromsteuer einmal, EZ‑6 gilt, Nachweisumschlag Fassung 13 mit Vermerk an
  Altläufen, Register EZ‑20/EZ‑21, Protokoll W642 nachgetragen, Zielvorgabe neu gerechnet; dazu der flatterhafte fremde Test
  `AssistentAbgleichTests` behoben), **#653** (P651: § 9b-Abzug der Gestehung mit Sockelbetrag und Deckelung, gemeinsame
  Hilfsfunktion `SteuerGutschriftRechner.Entgangene9bEur`; EZ‑22) und **#656** (P654: Deckel auch im § 9b-Ausweis der vermiedenen
  Stromkosten; EZ‑23). Alle vier mit grünem Gate, Referenzlauf 16/16 gegen R30 und CI-Vermerk (grüne Kern-Läufe auf fremden Zweigen
  mit demselben Stand, weil Läufe auf dem Arbeitszweig fortlaufend durch Pushes abgebrochen werden).
- **Letzter eigener Push:** `258bee90c` (02.10.2026, 10:15 UTC) auf `ios_migration_september` — CI-Vermerk zu #656. `main` steht
  weiter auf `8692ab40` (29.09.); ein Fast-Forward nur nach Gate auf dem Zweigstand und auf Zuruf.
- **Nummern und Basis:** Statuszeilen bis #673 vergeben (gemessen 03.10.2026 auf origin), #673 vorläufig für P671 (die
  Orchestrierung prüft beim Push); vor jeder Vergabe `git fetch` und die Statusdatei auf origin
  messen; am 02.10. wanderte jede eigene Nummer zwei- bis dreimal (Nachbarn: KP2/KP3 Gebäudesimulation, Dialogsitzung, Berichterstellung,
  Kostenstempel). Referenzbasis `Referenzlaeufe/2026-10-02_R32_Solarthermie` (Nachbarn; die Wirtschaftlichkeit steht in keiner CSV), Testdatenbank
  Schemastand 167 (fremde Schritte bis 166, Kostenstempel 159; 167 die Katalogempfehlung der Hilfsenergie aus P671; LFS-Zeiger
  `8f172db1…`), `SchemaStand.Zielversion = HilfsenergieEmpfehlungNachzug.SCHRITT`; Basis und Schemastand wandern täglich, vor
  jeder Welle neu messen. Die Wirtschaftlichkeit hat keinen Rechenweg der Simulation berührt; ihr einziger nummerierter
  Schemaschritt seit den Etappen ist der Katalogschritt 167 (P671, reines DML an den Auslieferungsvorlagen); Ergebnisspalten von `Tab_ErgebnisWirtschaftlichkeit`
  laufen additiv über `SpalteSicher` und `Werkzeuge/Testdatenbankschema` (kein Schritt, Hausmuster).
- **Messlatten:** Linux-Messlatte `Proben/ChartProben/Messlatte_2026-09-30.sha256` (200 Hashes, `kern.yml` auf ubuntu); Windows-Messlatte
  des lokalen Gates beim Anwender; die sechs Bericht-Messlatten `EPOS.Kern.Tests/Messlatten/Bericht_*` byte-gleich zu halten. Gate in
  der Cloud: `Werkzeuge/Gate/gate_linux.sh <Nr> <Worktree>` (rund 25 Minuten, `dotnet` unter `/root/.dotnet`, SDK 10.0.400 per
  `dotnet-install.sh`; in einer neuen Cloud-Sitzung erneut zu installieren).
- **Offene Anwenderpunkte** (Wortlaut in den Nach-Blöcken): Wiki-Sammel-Upload am 05.10.2026 mit 25 Logbuchsätzen aller Sitzungen unter
  1.2.0.6 (Nach #633 (d), #645 (a), #650 (a), #653 (a), #656 (a)); Windows-Proben am 05.10. (Nach #633 (e), #645 (b), #650 (b),
  #653 (b) und (d) Hinweiszeile „Obergrenze Regelsatz", #656 (b)); Unterrichtung der Nachbarsitzung Wärmegestehung (Zweige
  `wirt-gestehung`, `wirt-merge`) über Nachlese, EZ‑20 bis EZ‑23 und die fachliche Führung (Nach #650 (d)); ferner § 6.3 Nr. 37
  (Vorlagenweg, 5–6 %), die E30-Reste E30‑Q5 (Emission und Stromsteuer des Hilfsstroms) und N11 (Flotten-Netzeinspeisung) — Katalogempfehlung und Satzfeld sind mit P671 (#673, EZ‑24) erledigt —, Abnahmen A‑E13‑1 … A‑E31 in der App, iOS-Lauf E18 (macOS-Läufer, nur nach Rückfrage).
- **Nutzung:** Anwenderangabe 02.10.2026 „unter 40 %" vor den Wellen P651 und P654; Erfahrungswerte des Tages: Welle mit Code und
  Gate 6–9 %, kleine Welle 3–5 %, jeder zusätzliche Merge mit Nachtest etwa 1 %, volles Gate wiederholt etwa 1 %.
- **Lehren des Tages:** (1) Ein Skriptabbruch in einer Befehlskette darf den Commit nicht mitnehmen — Papieränderungen vor dem Commit
  prüfen (`git diff --stat`), Merge-Ausgaben nicht per Textfilter auswerten (Fast-Forward). (2) Fremde Wellen prüfen, bevor sie
  gemergt werden: `git diff --stat <alt> origin -- EPOS.Kern …`, bei Code erneut Nachtest. (3) Flatterhafte fremde Tests reproduzieren
  (mehrfach, im Hauptbaum auf origin-Stand) und die Ursache beheben, nicht abschalten.

## 2 Arbeitsweise

1. **Welle** = Auftragsdatei im Ordner `Dokumentation/ueberholt/Protokolle/Auftraege_Wirtschaftlichkeit_2026-09/` (Muster
   `P630_Auftrag_2026-09-30.md`; Fachvorgaben an andere Sitzungen wie `E31_Fachvorgabe_…` und `P555B_Fachvorgabe_…`) → Worktree ab origin
   (`git worktree add .claude/worktrees/<n> -b <n> origin/ios_migration_september`) → Opus-Agent baut in Commits je Punkt, pusht nicht →
   Merge origin → Gate → Statuszeile, Nach-Block, Protokoll unter `Dokumentation/ueberholt/Protokolle/Reporting/`, Register (Familie
   R‑E<n> oder R‑EZ), Konzept (§ 6.1 Etappe, § 6.2 Anker, § 6.3 Nr., § 3 Regel), Wiki-Quelle, Logbuchsatz → Dokumentationswachen → erneut
   `git fetch`, Nummer prüfen, mergen → Push → CI-Nachweis → Nachbarn informieren → Worktree entfernen.
2. **Gate:** lokal Windows `C:\Waermeplan\.claude\gate\gate_wt.sh <Nr> <Worktree>` (Kern-Filter Release, ChartProben gegen die
   Windows-Messlatte, alle fünf Testprojekte, Dokumentationswachen; Ablage `GATE<Nr>`), im Repo `Werkzeuge/Gate/gate_windows.sh` und
   `gate_linux.sh` (mit Referenzlauf). Referenzlauf danach eigens: `dotnet build EPOS.Referenzlauf -c Release`, `lauf` der sieben
   CI-Projekte, `vergleich` gegen die Basis — die sieben Projekte PASS, `GESAMT: FAIL` wegen der nicht gerechneten Basisprojekte ist normal.
   Windows-Schale `dotnet build WindowsFormsApplication1/… -c Debug -p:Platform=x64` durch den Agenten. Tests nur mit
   `-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`, nie parallel zu fremden Testprozessen (`tasklist | grep -i testhost`).
3. **Nummern:** Statusnummer und Schemaschritt erst beim Push gegen origin endgültig. Wird umnummeriert, nur die eigenen Fundstellen
   ändern — Anwenderzitate mit der alten Nummer und fremde Zeilen, die durch einen Merge schon im Baum liegen, vorher ausschließen
   (Lehre aus #633: die fremde #630 wurde mit ersetzt und musste wiederhergestellt werden). CI-Nachweis ist der erste grüne Kern-Lauf,
   dessen Commit den eigenen enthält (`git merge-base --is-ancestor <eigener> <Lauf-Commit>`); Folge-Pushes brechen laufende Läufe ab.
4. **Nachbarn:** lokale Sitzungen antworten über `SendMessage` (Dialog Design, Zapfprofil, Legendenfarbe …), Cloud-Sitzungen
   (Berichterstellung, Dialoge und Korrekturen, Gebäudesimulation, Zapfprofilgenerator) erreicht eine Nachricht nur einseitig, Antworten
   kommen über den Anwender. Fachliche Führung der Wirtschaftlichkeit (Konzept, Register) bleibt hier; Berichte baut die Berichterstellung
   nach Fachvorgabe, die Wirtschaftlichkeit nimmt ab und setzt Konzept und Register auf „umgesetzt".
5. **Modell und Nutzung:** Sitzung auf Opus 5.5 führen, Agenten mit `model: opus` (Bau, Papiere), `sonnet` (Suchen, Textpflege), `haiku`
   (Zählungen); Fable nur für komplexe Aufgaben mit Begründung. Vor jedem Agentenstart und nach jedem Bericht die Nutzung prüfen; Stopp
   bei 96 % des Wochenbudgets und Übergabe vorbereiten (Regel des Anwenders). Commit-Trailer `Co-Authored-By: Claude Opus 5.5
   <noreply@anthropic.com>` für Agenten, das arbeitende Modell für eigene Commits. Kein Push auf `main` ohne Gate auf dem Zweigstand.
6. **Weitere Regeln:** Testdatenbank nur per wiederholbarem Skript ändern (`Referenzlaeufe/Skripte/`), bei LFS-Konflikt die origin-Fassung
   nehmen und das Skript darauf anwenden; Einfrierregeln der `Referenzlaeufe/LIESMICH.md`; Wiki-Quellen ohne Tabu-Wörter (Regex in
   `CLAUDE.md`), Upload nur durch den Anwender (`Werkzeuge/WikiUpload/`), keine Zugangsdaten eingeben; Papiere mit dem Edit-Werkzeug
   (CRLF bleibt), keine `sed -i`; Merge-Konflikte in der Statusdatei inhaltlich zusammenführen (beide Zeilen, aufsteigend).

## 3 Einstieg in der neuen Sitzung

Erste Anweisung: „Lies `CLAUDE.md`, diese Übergabe und in `Dokumentation/aktuell/Status_iOS_Migration.md` die Zeilen #645, #650, #653
und #656 samt Nach-Blöcken; dann `git fetch`, Nummern und Basis messen, Nutzung prüfen und die offenen Anwenderpunkte aus Abschnitt 1
einholen." Sitzungsname „EPOS Plan Wirtschaftlichkeit"; Orchestrierung Fable 5.1, Agenten mit `model: opus`. Die Sitzung vom 02.10.2026
pausiert auf Anwenderwunsch bis 03.10.2026, 10:00 Uhr.
