# Auftrag CWN — Nachlese CI-Wächter: Quelltextleser in eigene Datei (Restpunkt aus #531, Empfehlung (a) des Bauberichts; 26.09.2026, nur Testcode, keine Regeländerung)

Anlass: `EPOS.Kern.Tests/KulturwaechterTests.cs` ist mit #531 auf 1.291 Zeilen gewachsen; 467 Zeilen davon sind der Quelltextleser (Maskierer für Kommentare
und Zeichenketten, Klassenleser, `Quelle`, `Klassenbestand`/`ErsteBasis`). Der Baubericht (`Fakten_2026-09-23/ciw_bericht.md`, Phase 1 Abschnitt 4) empfiehlt
(a) das Auslagern in eine eigene Datei (klein; Wächter schrumpft auf rund 820 Zeilen, andere Quelltext-Wachen können den Leser nutzen); (b) Roslyn bleibt
Anwenderentscheid und wird hier nicht gebaut. Statusnummer #534 vorgesehen (#532 BV‑E4, #530 Zapfprofil; beim Push messen). Worktree `.claude/worktrees/cwn`
ab origin ffa196be9 (Testdatenbank 3ac19fa9, Schemastand 148, Referenzbasis R20).

## Wortlaut des Agentenauftrags (model: opus)

Nachlese CI-Wächter für EPOS-Plan: den Quelltextleser aus `EPOS.Kern.Tests/KulturwaechterTests.cs` in eine eigene Datei auslagern, ohne die Regeln der
Wächter zu ändern. Antworten auf Deutsch. ARBEITSORT: Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\cwn` (Zweig `cwn`, HEAD ffa196be9); von der
Repowurzel aus `cd .claude/worktrees/cwn`, nie im Hauptbaum, kein Zweigwechsel, kein Push, kein Merge, kein Stash; kein Produktcode, kein Schema, keine
Papiere, kein Wiki, CLAUDE.md unverändert. `dotnet` unter `C:\Program Files\dotnet`. Testhost-Regel: vor jedem `dotnet test` `tasklist | grep -i testhost`;
bei fremdem Prozess warten, nach dem Freiwerden `sleep $((RANDOM % 26 + 5))` und erneut prüfen. Tests immer mit `-- xUnit.ParallelizeTestCollections=false
xUnit.MaxParallelThreads=2`. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Datum mit `date` prüfen.

LESEN: `EPOS.Kern.Tests/KulturwaechterTests.cs` vollständig; `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\ciw_bericht.md` Phase 1, Abschnitte 2–4;
`EPOS.Kern.Tests/QuelltextKodierungWacheTests.cs` (BOM/CRLF-Regel für `.cs`), `.editorconfig`.

AUFGABE: (1) Den Abschnitt „Werkzeug — Quelltextleser" (Maskierer, Klassenleser, `Quelle`, `Klassenbestand`, `ErsteBasis` und was sonst allgemeiner
Lesecode ohne Wächterbezug ist) nach `EPOS.Kern.Tests/Quelltextleser.cs` verschieben (gleicher Namespace, `internal static class Quelltextleser` oder eine
sinnvolle kleine Klassenfamilie; UTF-8 mit BOM, CRLF), mit Kopfkommentar (Zweck, Herkunft #531, Nummer #534 dieser Nachlese) und den XML-Kommentaren der
öffentlichen Mitglieder. Die Wächter- und Selbsttestklasse behält Regeln, Selbsttests, Bestandsproben, Standardkultur-Wächter und die Datei-/Arbeitsbaumhilfen,
soweit sie wächterspezifisch sind; Klassendoku im Kopf um einen Satz zur Auslagerung ergänzen. Keine Regel, keine Meldung, kein Selbsttest ändern; reine
Verschiebung mit den nötigen Sichtbarkeits- und Namensanpassungen. (2) Nachweis: Kulturwächter-Klasse vorher und nachher grün (alle 19 Fälle namentlich),
`QuelltextKodierungWacheTests` und die drei Dokumentationswachen grün, ein Release-Build von `WP-Plan.Kern.slnf` mit 0 Fehlern; Gegenprobe einmal:
`IDisposable` an einer Klasse mit Vorrichtungsfeld entfernen → Wächter A rot mit derselben Meldung wie vorher → zurücknehmen (nicht committen). (3) Zeilen
vorher/nachher je Datei (`wc -l`) und `git diff --stat`. Ein voller Kern-Lauf ist nicht nötig (macht das Gate der Hauptsitzung).

BERICHT: an `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\cwn_bericht.md` schreiben (Write) UND als Antwort zurückgeben: Commit(s) mit SHA, Dateien
und Zeilen vorher/nachher, was verschoben wurde (Mitglieder), Testzahlen aller Läufe, Gegenprobe, Aufwand, Restpunkte.
