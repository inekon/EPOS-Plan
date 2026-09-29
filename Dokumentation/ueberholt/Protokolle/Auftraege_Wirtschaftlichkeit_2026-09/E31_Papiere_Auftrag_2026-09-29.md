# Papierauftrag 29.09.2026: CI-Vermerke #548/#556 und Konzeptentwurf E31

Auftrag der Sitzung „EPOS Plan Wirtschaftlichkeit" an einen Opus-Agenten (Modellregel des Anwenders vom 29.09.2026: Opus für
Papiere). Arbeitsort: Worktree `.claude/worktrees/wi31`, Zweig `wi31-bericht-szenario` ab `2bd992c66` (= origin/ios_migration_september).
Kein Push; Commits mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## A. CI-Vermerke nachtragen (Statusdatei, Protokoll E30)

- Statuszeile **#548**: `**CI:** steht aus (…)` durch den geprüften Vermerk nach dem Muster von #536 ersetzen. Läufe zum
  E30-Push: Kern `main` 36236837124, Windows `main` 36236837113 (beide grün), Kern `ios_migration_september` 36236833414
  (überholt). Push-Commit und Ergebnis mit `gh run view <id> --json headSha,conclusion,createdAt,updatedAt` prüfen.
- Statuszeile **#556**: `**CI:** steht aus (…)` ersetzen. Push `8034733c` (26.09.2026 15:43): Kern `main` 36246178044 grün,
  Windows `main` 36246178033 grün, Kern `ios_migration_september` 36246173879 überholt; nächtlicher Windows-Lauf `main`
  36307781758 grün (27.09.).
- Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E30_Hilfsenergie_BhkwDeckung_R21_Protokoll.md`: steht dort „CI …
  steht aus", denselben Vermerk eintragen.
- Nach-Blöcke #582 und #574 nicht anfassen (schreibt die Cloud-Sitzung Berichterstellung).

## B. Konzeptentwurf E31 „Der Bericht folgt dem gewählten Szenario" (Stand: in Umsetzung)

Grundlage: `E31_Fachvorgabe_Bericht_Szenario_2026-09-29.md` in diesem Ordner. Der Bau läuft in der Cloud-Sitzung
Berichterstellung; das Konzept beschreibt die Regel mit der Kennzeichnung „in Umsetzung (E31)", die Statusnummer und der
Commit werden nach dem Push der Cloud-Sitzung nachgetragen.

1. `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`: Etappentabelle
   (Zeile E30, danach „## 6.2 Regressionsanker") um die Zeile **E31** ergänzen; § 2.13 (5) („zweites Bild … im
   Erwartungsfall") auf „im gewählten Szenario des Berichts, Vorgabe Erwartet" fassen; an der Stelle, die den Baustein
   Wirtschaftlichkeit des Wortberichts beschreibt, ein kurzer Regelabsatz mit `> **Stand: in Umsetzung (E31).**`: was dem
   Szenario folgt, was bleibt, Rückfall, Wahl und Ablage; Verweis auf die Fachvorgabe als relativer Link.
2. `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`: Block R‑E31 nach
   dem Muster R‑E30 mit E31‑E1 (27.09.2026, Nach #582, eingetragen mit #588: ja, der Bericht folgt dem gewählten Szenario)
   und E31‑E2 (29.09.2026: Bau in der Cloud-Sitzung Berichterstellung nach der Fachvorgabe; Konzeptabsatz und Abnahme bei
   der Wirtschaftlichkeit).

## C. Prüfung und Commits

Dokumentationswachen mit `--no-build` (gebaute `EPOS.Kern.Tests` in Debug liegt vor), vorher `tasklist | grep -i testhost`
(bei Treffer 5 bis 30 s warten und erneut prüfen). Zwei Commits: (1) CI-Vermerke, (2) Konzeptentwurf E31 samt Register und
dieser Auftragsdatei. Bericht: Befund mit Zeilennummern, Wachen-Ergebnis und Commit-Kennungen, keine Dateiabzüge.
