# Auftrag Papiere #525 — Windows-CI-Reparatur: PvPreisProjektTests Szenario-C pinnt de-DE (nur Testcode, 25.09.2026)

Stand: pm26 = d39847b6 (per Fast-Forward: origin 4fdda0c2 + Commit d39847b6 aus dem Worktree cipv). Muster: Statuszeile #515 und Nach #515
(CI-Reparatur Kulturleck). Zusatz: CI-Vermerk des Pushs 57c6c53b in der Statuszeile #521 nachtragen.

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #525 (Windows-CI-Reparatur: der Windows-Lauf 36184495006 auf `main` `57c6c53b` [Push #521] war mit 1 von 7.584
Kern-Tests rot — `PvPreisProjektTests.Szenario_C_ordnet_die_Energiekosten_und_der_Ausweis_zaehlt_vier` (`EPOS.Kern.Tests/PvPreisProjektTests.cs:387ff`,
Test aus E25 #519) prüft die deutschen Namen der gepflegten Parameter („Einspeisevergütung PV", „Arbeitspreis Elektrische Energie", „Grundpreis
Elektrische Energie", „Arbeitspreis Erdgas E") ohne Kulturvorrichtung; der Windows-Läufer läuft unter en-US und lieferte „Feed-in tariff PV" usw.;
die übrigen Fälle der Klasse pinnen schon de-DE (Zeilen 356, 416, 445, 502). Fix d39847b6: `using var kultur = new Kulturvorrichtung();` am Anfang
des Falls mit Kommentar; Klasse danach 13/13 grün; nur Testcode, kein Produktcode, kein Schemaschritt, Testdatenbank unverändert b68638da) für
EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge, kein Stash. ARBEITSORT: Worktree
`.claude/worktrees/papiere525` (Zweig `papiere525` ab d39847b6); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere525`,
nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Formregeln wie #515;
Änderungen mit dem Edit-Werkzeug, Zeilenenden erhalten (Statusdatei CRLF); Datum mit `date` prüfen (25.09.2026, spätabends).

FAKTEN: dieser Auftrag; `git show d39847b6` (Diff, 3 Zeilen); Statuszeile #515 und #519 als Muster und Bezug. CI-Nachweis für die Statuszeile #521
(Push 57c6c53b): Kern `main` 36184495009 grün (samt Referenzlauf der sechs CI-Projekte gegen R19), Windows `main` 36184495006 rot durch genau diesen
einen Test (UI 6.432 grün, Kern 7.582/7.584), Kern `ios_migration_september` 36184487520 abgebrochen (überholt durch den fremden Folge-Push e3809d46,
kein Befund).

AUFGABEN: (1) Statusdatei `Dokumentation/aktuell/Status_iOS_Migration.md`: Zeile #525 nach #521 vor `---` (Anlass CI-Lauf, Ursache mit Fundstelle,
Änderung, Nachweis Klasse 13/13, Stand d39847b6 per Fast-Forward auf pm26 über origin 4fdda0c2; **Gate:** Platzhalter NACHTRAG-525-GATE; **CI:**
NACHTRAG-525-CI; kein Logbuchsatz und kein Protokoll mit Begründung wie #515) und kurzer Block Nach #525 vor Nach #521: (a) Ursache und Muster (wie
#515: deutsche Ressourcentexte in Tests brauchen die Kulturvorrichtung), (b) Hinweis: der Wächter-Vorschlag aus Nach #515 (b) hätte diesen Fall nicht
erkannt, weil die Klasse eine Vorrichtung besitzt, nur nicht in jedem Fall — Empfehlung: Wächter, der in Testklassen mit deutschen Ressourcentexten
jeden Fall prüft, als Restpunkt für den Anwender, (c) Nachweis Gate/CI-Platzhalter. (2) In der Statuszeile #521 den CI-Vermerk „steht aus (Beobachtung
nach dem Push)" durch den Text aus FAKTEN ersetzen (auch im Block Nach #521 (i), falls dort derselbe Wortlaut steht). (3) Nichts sonst. (4) Bericht:
Commit-SHA, `git diff --stat` gegen d39847b6, Zeilennummern, verbliebene Platzhalter, keine Dateiabzüge.
