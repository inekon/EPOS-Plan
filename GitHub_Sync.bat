@echo off
REM Synchronisiert das Waermeplan-Repository mit GitHub.
REM Doppelklick genuegt: committet lokale Aenderungen und pusht zu origin/main.
cd /d "%~dp0"
echo.
echo === GitHub-Synchronisation Waermeplan ===
git add -A
git diff --cached --quiet || git commit -m "Synchronisation vom %date% %time%"
REM Merge statt Rebase: erhaelt die lokale Merge-Historie und kann nicht
REM mitten in einem Rebase steckenbleiben (Vorfall vom 16.08.2026).
git pull --no-rebase --no-edit origin main
if errorlevel 1 (
  echo.
  echo FEHLER beim Zusammenfuehren mit GitHub - Meldung oben pruefen.
  echo Es wurde NICHT gepusht.
  pause
  exit /b 1
)
git push origin main
if errorlevel 1 (
  echo.
  echo FEHLER beim Push - Meldung oben pruefen.
  pause
) else (
  echo.
  echo Synchronisation erfolgreich abgeschlossen.
  timeout /t 5 >nul
)
