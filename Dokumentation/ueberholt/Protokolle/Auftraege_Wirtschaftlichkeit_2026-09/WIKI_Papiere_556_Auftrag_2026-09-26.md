# Auftrag Papiere #556 — Wiki-Sammel-Upload 26.09.2026, Version 1.2.0.4 (nur Papiere)

Der Sammel-Upload ist am 26.09.2026 um 15:01–15:02 Uhr (13:01–13:02 UTC) durch den Anwender (Benutzer Epos, Skript `C:\Waermeplan\.claude\wiki\wiki_upload.py`)
erfolgt: 18 Seiten (11 ersetzt, 7 neu), Update-Logbuch mit Abschnitt „Version 1.2.0.4 – September 2026" (149 Sätze). Rücklese aller 18 Seiten byte-gleich mit den
Repo-Quellen (Prüfung der Hauptsitzung um 15:20). Revisionen: `Fakten_2026-09-23/wiki_upload_revisionen.txt` (Seiten 593–610 in der Reihenfolge des Upload-Papiers,
Logbuch 611). Vorprüfung: `Fakten_2026-09-23/wiki_upload_pruefung.md` (Live gegen Repo; Klimadaten-Live-Text übernommen, Kategorie bei Gebäudemodell/Kühlung
ergänzt — Commit 795911db auf pm26). Statusnummer #556 (beim Push messen; #553 Zapfprofil, #554/#555 Dialoge).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zum Wiki-Sammel-Upload 26.09.2026 (Statuszeile #556) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push,
kein Merge, kein Stash, kein Wiki-Zugriff. ARBEITSORT: Worktree `.claude/worktrees/papiere556` (Zweig `papiere556` ab pm26 795911db); von der Repowurzel
`C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere556`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
Edit-Werkzeug, Zeilenenden erhalten (Statusdatei CRLF); Datum mit `date`.

FAKTEN: dieser Auftrag; `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\wiki_upload_revisionen.txt`; `wiki_upload_pruefung.md`; `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`
(Seitenliste § 1, Logbuch § 2, Ablauf § 4); Hilfesystem-Konzept `Dokumentation/aktuell/Konzept_Hilfesystem_Wikidokumentation.md` (13.3, Abschnitt „Bedienungsseiten mit
Repo-Quelle", Muster der Revisionsvermerke „Dokumentationspflege …" mit Revision/Datum/Probe/Nachprobe); Statusdatei mit 39 Stellen „Upload ausstehend".

AUFGABEN: (1) Statusdatei `Dokumentation/aktuell/Status_iOS_Migration.md`: Zeile #556 als letzte vor `---` („Wiki-Sammel-Upload 26.09.2026, Version 1.2.0.4: 18 Seiten
[11 ersetzt, 7 neu], Update-Logbuch 149 Sätze; Revisionen 593–611; Rücklese byte-gleich; Vorprüfung Live gegen Repo mit zwei Nachbesserungen; Upload durch den Anwender per
Skript, weil die Orchestrierung keine Zugangsdaten eingibt"; Gate: entfällt [nur Papiere und drei Wiki-Quellen ohne Code; Wachen NACHTRAG-556-WACHEN]; CI: NACHTRAG-556-CI; kein
Logbuchsatz) und Block Nach #556 (Ablauf, Prüfung, Revisionen je Seite als Tafel, Restpunkte: zwei Berichtsvorlagen-Sätze BV‑E1/BV‑E2 noch ohne Version im Logbuch,
Wiki-Seite Simulationsergebnisse ohne Definition der BHKW-Stromdeckung, Seiten ohne Repo-Quelle aus Wiki_Update § 3, Bot-Passwort/Passwort nach dem Upload wechseln).
(2) Alle 39 Stellen „Upload ausstehend" in der Statusdatei additiv ergänzen: „→ hochgeladen 26.09.2026 (#556, Revision NNN)" mit der Revision der jeweils genannten
Seite(n); Wortlaut der Stellen nicht löschen. (3) Hilfesystem-Konzept: neuer Abschnitt „Sammel-Upload 26.09.2026 (Version 1.2.0.4, Auftrag #556)" nach dem Muster der
Dokumentationspflege-Abschnitte mit der Revisionstafel (Seite, alte Revision falls bekannt, neue Revision, neu/ersetzt), Verweis auf Vorprüfung und Skript, Hinweis auf
Regel 5 (Upload durch den Anwender). (4) `Wiki_Update_2026-09-26.md`: Kopf um „**Durchgeführt 26.09.2026, Revisionen 593–611**" ergänzen, § 4 Schritte als erledigt
kennzeichnen, § 2 Berichtsvorlagen-Abschnitt Vermerk „nicht hochgeladen, Version offen". (5) Nichts sonst. (6) Bericht: Commit-SHAs, `git diff --stat`, Zahl der
ersetzten „Upload ausstehend"-Stellen, Zeilennummern, verbliebene Platzhalter, keine Dateiabzüge.
