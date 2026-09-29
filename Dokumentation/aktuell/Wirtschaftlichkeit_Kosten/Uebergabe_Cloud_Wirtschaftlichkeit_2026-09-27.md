# Übergabe der Sitzung „EPOS Plan Wirtschaftlichkeit" an die Cloud-Umgebung — 27.09.2026

Diese Datei ist der Einstieg für die Fortführung in einer Cloud-Sitzung (claude.ai/code über die Claude-Desktop-App). Sie ersetzt das
maschinenlokale Gedächtnis der bisherigen Sitzung; alles Fachliche steht in Statusdatei, Protokollen, Register und Konzept.

## 1 Stand

- Alle Etappen des Konzepts „EPOS Plan Wirtschaftlichkeit" sind gebaut und gepusht: E0–E10, E13–E30 (E11 entfällt, E12 = Wiki-Sammel-Upload am
  26.09.2026 erfolgt, Version 1.2.0.4, Statuszeile #556). Dazu die CI-Wächter-Wellen #531 (Kulturwächter, Standardkultur en-US, runner.json) und #534.
- Letzter eigener Push: `8034733c` (26.09.2026 15:44) auf `ios_migration_september` und `main`. Die Nachbarsitzungen pushen nur `ios_migration_september`;
  `main` zieht die Wirtschaftlichkeits-Sitzung beim eigenen Push per Fast-Forward mit (Windows-CI läuft nur auf `main`).
- Referenzbasis: seit #568 `Referenzlaeufe/2026-09-26_R23_KesselBereitschaft` (Dialog-Sitzung); davor R22 (Solarthermie, #560), R21 (BHKW-Deckung, #548,
  eigene). Testdatenbank zuletzt LFS `22b1f882…` (Schemastand 151, Gebäudesimulation #583). Vor jeder Arbeit `git fetch`, `git lfs pull`, Nummern messen.
- Offen beim Anwender: Windows-Abnahmen A‑E13‑1 … A‑E30‑1 (Nach-Blöcke), iOS-Lauf E18, Versionsnummer für die Logbuchsätze der Berichtsvorlagen
  (BV‑E1/BV‑E2, `Werkzeuge/WikiUpload/logbuch_bv_offen.wiki`), Nachschlag-Upload der Wiki-Seiten Mehrzonenmodell und Projekttransfer (live 404,
  Quellen fertig), Restpunkte aus E30 (Satzfeld im Kostenraster bei Satz aus dem Anlagenanteil; Katalogempfehlung „Hilfsenergie Kessel 4–8 %"
  für Weg B zu hoch; N11 Flotten-Einspeisung im Strombilanz-Stapel), E16‑Q1…Q4 und E5‑Q3 formal offen, § 6.3 Nr. 19 nur dokumentiert.
- Fremde Änderung mit Bezug zur Wirtschaftlichkeit, noch nicht gegen das Konzept geprüft: #582 („Wirtschaftlichkeit: ‚Zum Bericht ›' statt eigenem
  Bericht", Dialog-Sitzung) und #555 („Strom ohne Verwendung" je Vergleichsgruppe).

## 2 Arbeitsweise (unverändert)

1. Welle = Auftragsdatei (Muster `Dokumentation/ueberholt/Auftraege_Wirtschaftlichkeit_2026-09/E30_Auftrag_2026-09-26.md`) → Worktree ab origin →
   Opus Phase 0 (Befund, Fragen mit Empfehlung) → Entscheide „nach Empfehlung" (Anwenderregel) → „Bau freigegeben" → Bau in Commits je Schritt →
   Merge auf den Sammelzweig „(#Nr)" → Gate → Papieragent mit Platzhaltern NACHTRAG-<Nr>-GATE/-CI → Platzhalter füllen → Wachen → `git fetch`,
   origin mergen, Nachtest → Push auf beide Zweige → CI beobachten → Nachbarn informieren → Worktrees löschen.
2. Regeln: Testläufe nur mit `-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`; `EPOS.Referenzlauf` vor jedem Referenzlauf eigens
   bauen (liegt nicht in der slnf); Testdatenbank nur per wiederholbarem Skript ändern (`Referenzlaeufe/Skripte/`), bei LFS-Konflikt die origin-Fassung
   nehmen und das eigene Skript darauf anwenden; Neueinfrierung nach den Einfrierregeln der `Referenzlaeufe/LIESMICH.md`; Statusnummern und
   Schemaschritte erst beim Push gegen origin endgültig (heute Nummern bis #583 vergeben, Reservierungen der Nachbarn bis #582; Schemaschritt
   152 frei); Commit-Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` für Agenten.
3. Cloud-Besonderheiten: Kein Windows — Windows-Schale und Windows-Bildmesslatte entfallen, die Windows-CI auf `main` bleibt der Nachweis nach dem
   Push. Gate: `Werkzeuge/Gate/gate_linux.sh <Nr>` (Kern-Filter, ChartProben gegen die Linux-Messlatte `Proben/ChartProben/Messlatte_*.sha256`,
   Tests, Wachen, Referenzlauf). Keine Nachbarsitzungen erreichbar: Nummernabstimmung über die Statusdatei auf origin (fetch vor der Vergabe,
   Kreuzungen wie am 26.09. mehrfach vorgekommen). Wiki-Upload nur durch den Anwender (`Werkzeuge/WikiUpload/`), keine Zugangsdaten eingeben.
4. Papiere je Welle: Statuszeile + Nach-Block in `Dokumentation/aktuell/Status_iOS_Migration.md`, Protokoll unter
   `Dokumentation/ueberholt/Protokolle/Reporting/`, Register `Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md` (Familie R‑E<n>), Konzept
   `Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` (§ 6.1 Etappe, § 6.2 Anker, § 6.3 Nr., § 3 Regel), Analyse
   `2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md` § 5, Logbuchsatz im Wiki-Upload-Papier, Wiki-Quelle unter `Projekte/Wiki/`.

## 3 Einstieg in der Cloud-Sitzung

Erste Anweisung: „Lies `CLAUDE.md`, diese Übergabe und `Dokumentation/aktuell/Status_iOS_Migration.md` (Nach #548, #556); dann `git fetch`,
Nummern messen und den nächsten Punkt aus Abschnitt 1 vorschlagen." Sitzungsname „EPOS Plan Wirtschaftlichkeit" beibehalten.
