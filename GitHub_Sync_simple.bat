@echo off
REM Synchronisiert das Waermeplan-Repository mit GitHub.
REM Doppelklick genuegt: committet lokale Aenderungen und synchronisiert den
REM AKTUELLEN Branch mit seinem GitHub-Gegenstueck (main <-> origin/main,
REM Arbeitsbranch <-> origin/<Branch>). Vorher wurde fest origin/main gepusht -
REM auf einem Arbeitsbranch sicherte das nichts (Vorfall vom 26.08.2026).
cd /d "%~dp0"
echo.
echo === GitHub-Synchronisation Waermeplan ===
for /f "delims=" %%b in ('git rev-parse --abbrev-ref HEAD') do set "BRANCH=%%b"
echo Branch: %BRANCH%
git add -A
git diff --cached --quiet || git commit -m "Synchronisation vom %date% %time%"
REM Merge statt Rebase: erhaelt die lokale Merge-Historie und kann nicht
REM mitten in einem Rebase steckenbleiben (Vorfall vom 16.08.2026).
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
