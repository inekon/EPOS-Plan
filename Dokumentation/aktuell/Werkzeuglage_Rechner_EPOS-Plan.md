# Werkzeuglage des Entwicklungsrechners (EPOS-Plan)

**Stand 23.09.2026.** Was auf dem Windows-Rechner fehlt oder abweicht, obwohl die
`CLAUDE.md` es voraussetzt — damit Sitzungen und Agenten nicht erneut daran scheitern.
Gemessen in der Sitzung zur Gebäudesimulation (Konzept
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)).

| Werkzeug | Lage | Ausweg |
|---|---|---|
| Python | Python 3.14 unter `C:\Python314`, aufgerufen über den Starter `py`; `python3` ist nur der Windows-Store-Stub unter `…\Microsoft\WindowsApps\`, kein Interpreter. Die Werkzeuge geben Unicode aus und scheitern an der Konsolenkodierung | `py` statt `python3`, mit `PYTHONIOENCODING=utf-8` davor — etwa `PYTHONIOENCODING=utf-8 py Werkzeuge/ResourceDesigner/designer_neu.py schreiben` |
| `sqlite3`-Kommandozeile | vorhanden (3.53, über WinGet) | Proben an der Testdatenbank lesend; geschrieben wird sie nur über die Skripte unter `Referenzlaeufe/Skripte/` und `Werkzeuge/Testdatenbankschema` |
| `pdftoppm` (poppler) | fehlt; das PDF-Lesen der Werkzeuge schlägt fehl | Word ist installiert: COM-Automation (`Word.Application`, `Documents.Open`, `SaveAs2(…, 7)`) wandelt PDF in Unicode-Text; Dateien vorher unter einen kurzen lokalen Pfad kopieren |
| Scratchpad-Pfad der Sitzung | über 300 Zeichen; MSBuild und Windows PowerShell 5.1 scheitern an der Win32-Pfadlänge | Spikes und Prototypen unter `C:\Users\Dirk\AppData\Local\Temp\epos-spike\` bauen |
| `dotnet` | 10.0.401 vorhanden; NuGet-Cache mit `Microsoft.Data.Sqlite` und SQLitePCLRaw | — |
| Perl | 5.42 in der Git-Bash vorhanden | — |
| Git LFS | 3.7 vorhanden; die Testdatenbank liegt als echte Datei vor (68 MB, SQLite-Kopf) | — |

Regeln, die daraus folgen: Netzlaufwerk `Z:` nur lesen und Material vor der Auswertung
lokal kopieren; Datenbankproben ausschließlich lesend; der Ordner `epos-spike` ist keine
Auslieferung und kein Teil des Repositoriums.
