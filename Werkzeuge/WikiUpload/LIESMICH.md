# Wiki-Sammel-Upload (MediaWiki wiki.epos-plan.de)

`wiki_upload.py` lädt die Seiten aus `seiten.tsv` (Wikititel TAB Repo-Quelle unter `Projekte/Wiki/`, für die Rechenwegseiten der Rubrik „Programm Dokumentation/Berechnung“ unter `EPOS.Kern/Allgemein/Hilfe/Berechnung/`) und einen Logbuch-Abschnitt über die
MediaWiki-API hoch; Zugangsdaten nur als Umgebungsvariablen `WIKI_BOT_USER`/`WIKI_BOT_PASS` (Bot-Passwort aus `Spezial:BotPasswords`),
niemals als Argument, niemals durch einen Agenten eingegeben (Hilfesystem-Konzept, Regel 5: Upload durch die Orchestrierung bzw. den
Anwender). `upload_start.ps1` fragt die Daten verdeckt ab und startet das Skript. `--trocken` zeigt nur den Plan; `--seiten`, `--logbuch`,
`--nur 1,2` wählen. Nach jedem Speichern liest das Skript die Seite zurück (byte-gleich) und prüft den Parser. Erster Lauf: 26.09.2026,
Version 1.2.0.4, Revisionen 593–611 (Statuszeile #556). `logbuch_bv_offen.wiki` hält die zwei Berichtsvorlagen-Sätze ohne Version.
Vorher gilt Regel 3 des Hilfesystem-Konzepts: Live-Stand lesen und mit der Repo-Quelle vergleichen (Bericht `wiki_upload_pruefung.md`).

`vorschau.py` zeigt eine Repo-Quelle so, wie das Wiki sie darstellt, ohne etwas zu speichern: Es löst die Diagrammvorlagen (`Projekte/Wiki/Vorlage - *.wiki`) lokal auf, lässt das Ergebnis vom Wiki per `action=parse` säubern und schreibt je eine HTML-Seite hell und dunkel; mit `--bild` entstehen Aufnahmen (Chromium). Aufruf: `python3 Werkzeuge/WikiUpload/vorschau.py <quelle.wiki> <zielordner> [--bild]`.

`weiterleitungen.tsv` (Titel TAB Ziel) hält die Seiten, die zu Weiterleitungen werden: die älteren Seiten der Rubrik Programmfunktionen und die Synonyme, die auf sie zeigten (MediaWiki folgt keiner doppelten Weiterleitung). `--weiterleitungen` legt sie an — erst nach `--seiten`, denn das Ziel muss stehen. In `seiten.tsv` stehen die Diagrammvorlagen vorn, weil die Seiten danach sie einbinden. Regel und Liste: `Dokumentation/aktuell/Konzept_Technikdokumentation_Wiki_EPOS-Plan.md`, Abschnitt 8.

`dateien.tsv` (Dateiname TAB Repo-Pfad TAB Beschreibung; Grafiken liegen unter `Projekte/Wiki/Dateien/`) hält die Dateien, die `--dateien` als Wiki-Dateien hochlädt — vor `--seiten`, damit eine Seite eine frisch hochgeladene Grafik schon einbinden kann. Je Zeile wird der SHA-1 der lokalen Datei gegen `prop=imageinfo` geprüft (NEU/GLEICH/ERSATZ wie bei den Seiten), davor `meta=siteinfo` (Uploads freigeschaltet, Endung zugelassen) und bei einer SVG zusätzlich eine lokale Prüfung auf `<script>`, `on…=`-Attribute, `<foreignObject>`, externe `href`/`xlink:href` und `<title>` — ein Treffer ist ein Abbruch ohne Upload. Die Beschreibung wird zusammen mit `[[Kategorie:Grafiken]]` zur Dateibeschreibungsseite. `--dateiliste <Datei>` wählt eine andere Liste als die neben dem Skript.

`entfalten.py` führt hart umbrochene Absätze, Listenpunkte und Tabellenzellen einer Wiki-Quelle auf je eine Zeile zusammen (Konzept Hilfesystem 13.5): Ohne Schalter zeigt es je Datei, was es zusammenführen würde, mit `--schreiben` schreibt es die Dateien byte-erhaltend. Kommentare, `<math>`, `<pre>`, `<syntaxhighlight>` und mehrzeilige Vorlagenaufrufe bleiben unberührt; eine eingerückte Zeile nach einer Leerzeile gilt als gewollter Kasten. Aufruf: `python3 Werkzeuge/WikiUpload/entfalten.py [--schreiben] <datei>…`.
