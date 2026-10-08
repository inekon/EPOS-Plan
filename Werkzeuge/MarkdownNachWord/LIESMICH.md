# MarkdownNachWord — Konzeptpapier als Word-Datei ablegen

`md2docx.py` wandelt ein Markdown-Papier aus `Dokumentation/` in eine Word-Datei (`.docx`), wenn der Anwender eine
Kopie außerhalb des Repositoriums verlangt. Das Werkzeug braucht keine Zusatzpakete: Es schreibt aus dem Markdown
eine HTML-Zwischendatei (Überschriften, Absätze, Fett, Kursiv, Code-Spannen, Tabellen mit Pipe-Syntax, Codeblöcke,
Listen, Zitate, Verweise) und lässt ein installiertes Microsoft Word über COM daraus die `.docx` speichern
(`SaveAs2`, Format 12). Es läuft deshalb nur unter Windows mit Word; auf Linux oder macOS stattdessen `pandoc`
nehmen (`pandoc <in.md> -o <out.docx>`), falls vorhanden.

```powershell
py Werkzeuge\MarkdownNachWord\md2docx.py Dokumentation\aktuell\<Papier>.md "<Zielordner>\<Papier>.docx"
```

- Eingabe UTF-8 (mit oder ohne BOM), Zeilenenden beliebig; die Zwischendatei wird nach dem Lauf gelöscht.
- Die Markdown-Kopie daneben entsteht nicht hier, sondern mit einer Kopie der Repo-Datei (unter Windows mit CRLF).
- Grenzen: verschachtelte Listen nur eine Ebene, Fußnoten und HTML im Markdown werden als Text übernommen.
- Das Werkzeug ist in keiner Projektmappe und keiner CI; es ändert nichts im Repositorium.
