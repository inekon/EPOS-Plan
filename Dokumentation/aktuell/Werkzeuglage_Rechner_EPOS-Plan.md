# Werkzeuglage des Entwicklungsrechners (EPOS-Plan)

**Stand 15.09.2026.** Was auf dem Windows-Rechner fehlt oder abweicht, obwohl die
`CLAUDE.md` es voraussetzt — damit Sitzungen und Agenten nicht erneut daran scheitern.
Gemessen in der Sitzung zur Gebäudesimulation (Konzept
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)).

| Werkzeug | Lage | Ausweg |
|---|---|---|
| `python3` / `python` | nur der Windows-Store-Stub unter `…\Microsoft\WindowsApps\`, kein Interpreter. Die Werkzeuge `Werkzeuge/ResourceDesigner/designer_neu.py`, `Werkzeuge/SqlDialektPruefer/pruefer.py` und `Werkzeuge/KlimazonenPfade/erzeugen.py` laufen hier nicht | Python installieren, oder die Prüfungen der CI überlassen (`kern.yml`, ubuntu); Textwerkzeuge in Perl 5.38 |
| `sqlite3`-Kommandozeile | fehlt | kleines net10.0-Konsolenprojekt mit `Microsoft.Data.Sqlite` (Version aus `Directory.Packages.props`), Öffnung mit `Mode=ReadOnly`; niemals `VACUUM` oder Schreibzugriff auf die Testdatenbank |
| `pdftoppm` (poppler) | fehlt; das PDF-Lesen der Werkzeuge schlägt fehl | Word ist installiert: COM-Automation (`Word.Application`, `Documents.Open`, `SaveAs2(…, 7)`) wandelt PDF in Unicode-Text; Dateien vorher unter einen kurzen lokalen Pfad kopieren |
| Scratchpad-Pfad der Sitzung | über 300 Zeichen; MSBuild und Windows PowerShell 5.1 scheitern an der Win32-Pfadlänge | Spikes und Prototypen unter `C:\Users\Dirk\AppData\Local\Temp\epos-spike\` bauen |
| `dotnet` | 10.0.401 vorhanden; NuGet-Cache mit `Microsoft.Data.Sqlite` und SQLitePCLRaw | — |
| Perl | 5.38 (msys) vorhanden; `JSON::PP` ja, `CAM::PDF` nein | — |
| Git LFS | Testdatenbank liegt als echte Datei vor (70 MB, SQLite-Kopf) | — |

Regeln, die daraus folgen: Netzlaufwerk `Z:` nur lesen und Material vor der Auswertung
lokal kopieren; Datenbankproben ausschließlich lesend; der Ordner `epos-spike` ist keine
Auslieferung und kein Teil des Repositoriums.
