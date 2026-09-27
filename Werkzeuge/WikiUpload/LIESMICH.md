# Wiki-Sammel-Upload (MediaWiki wiki.epos-plan.de)

`wiki_upload.py` lädt die Seiten aus `seiten.tsv` (Wikititel TAB Repo-Quelle unter `Projekte/Wiki/`) und einen Logbuch-Abschnitt über die
MediaWiki-API hoch; Zugangsdaten nur als Umgebungsvariablen `WIKI_BOT_USER`/`WIKI_BOT_PASS` (Bot-Passwort aus `Spezial:BotPasswords`),
niemals als Argument, niemals durch einen Agenten eingegeben (Hilfesystem-Konzept, Regel 5: Upload durch die Orchestrierung bzw. den
Anwender). `upload_start.ps1` fragt die Daten verdeckt ab und startet das Skript. `--trocken` zeigt nur den Plan; `--seiten`, `--logbuch`,
`--nur 1,2` wählen. Nach jedem Speichern liest das Skript die Seite zurück (byte-gleich) und prüft den Parser. Erster Lauf: 26.09.2026,
Version 1.2.0.4, Revisionen 593–611 (Statuszeile #556). `logbuch_bv_offen.wiki` hält die zwei Berichtsvorlagen-Sätze ohne Version.
Vorher gilt Regel 3 des Hilfesystem-Konzepts: Live-Stand lesen und mit der Repo-Quelle vergleichen (Bericht `wiki_upload_pruefung.md`).
