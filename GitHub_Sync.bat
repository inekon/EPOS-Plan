@echo off
REM Synchronisiert das EPOS-Plan-Repository mit GitHub.
REM Doppelklick genuegt: committet lokale Aenderungen und synchronisiert den
REM AKTUELLEN Branch mit seinem GitHub-Gegenstueck (main <-> origin/main,
REM Arbeitsbranch <-> origin/<Branch>).
REM
REM Drei Waechter brechen ab, bevor etwas committet wird:
REM   1. AGENT_LAEUFT in der Repowurzel: Claude legt die Datei an, solange ein
REM      Agent im Hauptbaum arbeitet, und loescht sie nach der Abnahme. Solange
REM      sie liegt, wuerde "git add -A" einen halbfertigen Stand einsammeln und
REM      veroeffentlichen. Liegt die Datei ohne laufenden Agenten herum, loeschen
REM      und erneut starten.
REM   2. Ein offener Merge oder Rebase (.git\MERGE_HEAD, rebase-Ordner) oder
REM      unaufgeloeste Pfade (git ls-files -u): erst aufloesen und committen.
REM   3. Konfliktmarker in den Aenderungen, die committet wuerden
REM      (git diff --cached --check): erst bereinigen.
cd /d "%~dp0"
echo.
echo === GitHub-Synchronisation EPOS-Plan ===
for /f "delims=" %%b in ('git rev-parse --abbrev-ref HEAD') do set "BRANCH=%%b"
echo Branch: %BRANCH%

if exist "AGENT_LAEUFT" (
  echo.
  echo ABBRUCH: Im Arbeitsbaum laeuft noch Agentenarbeit. Inhalt von AGENT_LAEUFT:
  echo ----------------------------------------------------------------------
  type "AGENT_LAEUFT"
  echo ----------------------------------------------------------------------
  echo Erst nach der Abnahme synchronisieren. Ist die Datei eine Leiche, weil kein
  echo Agent mehr aktiv ist: AGENT_LAEUFT loeschen und erneut starten.
  pause
  exit /b 2
)

if exist ".git\MERGE_HEAD" (
  echo.
  echo ABBRUCH: Ein Merge ist noch offen - .git\MERGE_HEAD existiert. Erst aufloesen und committen.
  pause
  exit /b 3
)
if exist ".git\rebase-merge" (
  echo.
  echo ABBRUCH: Ein Rebase ist noch offen. Erst abschliessen oder abbrechen.
  pause
  exit /b 3
)
if exist ".git\rebase-apply" (
  echo.
  echo ABBRUCH: Ein Rebase ist noch offen. Erst abschliessen oder abbrechen.
  pause
  exit /b 3
)
for /f "delims=" %%u in ('git ls-files -u') do (
  echo.
  echo ABBRUCH: Unaufgeloeste Pfade im Index. Erst aufloesen und committen.
  git ls-files -u
  pause
  exit /b 3
)

git add -A
git diff --cached --check 2>nul | findstr /C:"leftover conflict marker" >nul
if not errorlevel 1 (
  echo.
  echo ABBRUCH: Konfliktmarker in den Aenderungen. Fundstellen:
  git diff --cached --check 2>nul | findstr /C:"leftover conflict marker"
  git reset -q
  pause
  exit /b 4
)
git diff --cached --quiet || git commit -m "Synchronisation vom %date% %time%"
REM Merge statt Rebase: erhaelt die lokale Merge-Historie und kann nicht
REM mitten in einem Rebase steckenbleiben.
REM Gepullt wird nur, wenn der Branch auf GitHub schon existiert -
REM einen frisch angelegten lokalen Branch legt unten "push -u" neu an.
set "PULLFEHLER=0"
git ls-remote --exit-code --heads origin "%BRANCH%" >nul 2>&1
if not errorlevel 1 (
  git pull --no-rebase --no-edit origin "%BRANCH%"
  if errorlevel 1 set "PULLFEHLER=1"
)
if "%PULLFEHLER%"=="1" (
  echo.
  echo FEHLER beim Zusammenfuehren mit GitHub - Meldung oben pruefen.
  echo Es wurde NICHT gepusht.
  pause
  exit /b 1
)
git push -u origin "%BRANCH%"
if errorlevel 1 (
  echo.
  echo FEHLER beim Push - Meldung oben pruefen.
  pause
) else (
  echo.
  echo Synchronisation erfolgreich abgeschlossen.
  timeout /t 5 >nul
)
