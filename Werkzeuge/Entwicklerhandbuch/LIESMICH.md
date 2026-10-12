# Entwicklerhandbuch — Word-Fassung erzeugen

`erzeugen.js` baut aus der Markdown-Quelle
`Dokumentation/aktuell/Entwicklerhandbuch_Administration_Support_EPOS-Plan.md` die Word-Fassung
`Entwicklerhandbuch_Administration_Support_EPOS-Plan.docx` daneben: Überschriften, Absätze, Listen,
Tabellen und Code-Spannen des Markdowns werden in Word-Absätze übersetzt, Kopf- und Fußzeile tragen
Titel und Seitenzahl. Die Markdown-Fassung bleibt die Quelle; die Word-Fassung wird nie von Hand
bearbeitet.

Das Werkzeug braucht Node.js und das npm-Modul `docx`. Ohne Paketdatei im Repositorium wird das
Modul global installiert und über `NODE_PATH` gefunden:

    npm install -g docx
    NODE_PATH="$(npm root -g)" node Werkzeuge/Entwicklerhandbuch/erzeugen.js

Der Aufruf gilt aus der Repowurzel; das Skript findet Quelle und Ziel über seinen eigenen Ort.
Es meldet `ok <Bytes>`.

**Nach jeder Änderung am Handbuch-Markdown ziehen und die `.docx` mit committen.** Jeder Lauf
schreibt einen neuen Zeitstempel in die Datei; ohne Änderung am Markdown die Datei deshalb nicht
neu erzeugen, sonst entsteht ein Commit ohne Inhalt.
