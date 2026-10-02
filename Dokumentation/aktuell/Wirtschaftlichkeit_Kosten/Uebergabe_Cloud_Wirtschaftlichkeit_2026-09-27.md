# Übergabe der Sitzung „EPOS Plan Wirtschaftlichkeit" — Stand 30.09.2026 (fortgeschrieben aus der Fassung vom 27.09.2026)

Diese Datei ist der Einstieg für die Fortführung in einer neuen Sitzung, gleich ob Cloud (claude.ai/code), Desktop-App oder ein
anderes Claude-Konto. Sie ersetzt das maschinenlokale Gedächtnis der bisherigen Sitzung; alles Fachliche steht in Statusdatei,
Protokollen, Register und Konzept. Das Gedächtnis der Desktop-App liegt je Rechner und Windows-Benutzer unter
`C:\Users\<Benutzer>\.claude\projects\C--Waermeplan\memory\`, nicht je Konto — auf demselben Rechner steht es einem anderen Konto zur Verfügung.

## 1 Stand

- **Alle Etappen des Konzepts sind gebaut und gepusht:** E0–E10, E13–E31 (E11 entfällt, E12 = Wiki-Sammel-Upload #556). E31 „Der Bericht
  folgt dem gewählten Szenario" baute die Cloud-Sitzung Berichterstellung nach der Fachvorgabe der Wirtschaftlichkeit (#591); die Abnahme
  E31‑A1 steht im Register. Dazu die Nachlese-Wellen **#603** (P555: Gruppenregel im Konzept, Szenarioabdeckung je Lauf, Befund zu #555),
  **#612** (P556: Leistungspreis nur bei stromverwendendem Erzeuger, EZ‑17, mit Hinweis) und **#633** (P630: Rollentarif unter EZ‑17,
  Veraltet-Band am Haken, Rückfallträger im Ausweis, Stromsteuer-Kohärenz am Rückfallträger, EZ‑18). Das Kostenkapitel des Berichts
  (Einzelzahl mit Fußzeile der Gruppenregel, Fachvorgabe P555‑B) baute die Berichterstellung als #609 und ist abgenommen; #613 trägt den
  Wiki-Satz zu EZ‑17.
- **Letzter eigener Push:** `56140d04f` (30.09.2026 13:56) auf `ios_migration_september` — Statuszeile #633 samt CI-Vermerk (Kern-Lauf
  36708236840 grün). `main` steht auf einem fremden Stand (`8692ab40`, 29.09.) und wurde nicht mitgezogen; der Windows-Nachtlauf prüft
  inzwischen den Arbeitszweig (`windows.yml`), ein Fast-Forward von `main` ist nur nach einem grünen Kern-Lauf des Zweigstands sinnvoll.
- **Nummern und Basis:** Statuszeilen bis #634 vergeben (Stand 12:03), vor jeder Vergabe `git fetch` und die Statusdatei auf origin
  messen; die Nachbarn pushen im Minutentakt, Kreuzungen sind der Regelfall. Referenzbasis `Referenzlaeufe/2026-09-30_R29_Kesseltakten`
  (sechzehn Projekte, 1050 neu; Nachbarn), Schemastand 158 (Schritte bis 158 vergeben, KP2 hat 156 angemeldet, nächster Schritt nach
  fetch prüfen). Die Wirtschaftlichkeit hat seit R21 keinen Rechenweg und kein Schema mehr berührt.
- **Messlatten:** Windows-Bildmesslatte des lokalen Gates `C:\Waermeplan\.claude\gate\messlatte_windows.sha256` mit 194 Hashes (Sicherungen
  daneben); Linux-Messlatte im Repo `Proben/ChartProben/Messlatte_2026-09-30.sha256`, die `kern.yml` auf ubuntu prüft. Die sechs
  Bericht-Messlatten `EPOS.Kern.Tests/Messlatten/Bericht_*` sind byte-gleich zu halten.
- **Offene Anwenderentscheide** (Wortlaut in den Nach-Blöcken der Statusdatei): Nach #633 (a) Bezugsrolle ohne Leistungspreis an der
  Gruppenregel-Kopie bestätigen; (b) Laufvermerk je Ergebnis, damit die Seite veraltete Ergebnisse auch nach einem Wechsel in Übersicht
  oder Kosten erkennt — Schemaschritt; (c) Kohärenz am Rückfallträger kann nie „stimmig" melden, weil dort kein Stromsteueranteil
  pflegbar ist; (d) Wiki-Upload der Quelle Wirtschaftlichkeit mit den Logbuchsätzen unter 1.2.0.6; (e) Windows-Proben (Veraltet-Band,
  Hinweis zum Leistungspreismodell) und die älteren aus #590, #591, #600, #609. Ferner § 6.3 Nr. 37 (Vorlagenweg: Kapitelplatzhalter
  gemischt mit Einzelplatzhaltern zeigt zwei Szenarien — nur mit neuen Platzhaltern lösbar), die E30-Reste (Satzfeld im Kostenraster,
  Katalogempfehlung „Hilfsenergie Kessel 4–8 %", N11 Flotten-Einspeisung), Abnahmen A‑E13‑1 … A‑E31 in der App, iOS-Lauf E18.
- **Kostenrahmen der offenen Bauwellen** (Erfahrungswerte 29./30.09.: Welle mit vier Punkten samt Gate 8 %, kleine Welle 5 %, Papierschritt
  1 % des Wochenbudgets): Laufvermerk 8–10 %, Kohärenz „stimmig" 5–8 %, Nr. 37 5–6 %, E30-Reste je 3–4 %.

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

Erste Anweisung: „Lies `CLAUDE.md`, diese Übergabe und in `Dokumentation/aktuell/Status_iOS_Migration.md` die Zeilen #603, #612 und #633
samt Nach-Blöcken; dann `git fetch`, Nummern und Basis messen, Nutzung prüfen und die offenen Anwenderentscheide aus Abschnitt 1
einholen, beginnend mit Nach #633 (a) und (b)." Sitzungsname „EPOS Plan Wirtschaftlichkeit", Modell Opus 5.5.
