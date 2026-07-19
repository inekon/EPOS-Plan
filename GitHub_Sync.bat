@echo off
REM Synchronisiert das Waermeplan-Repository mit GitHub.
REM Doppelklick genuegt: committet lokale Aenderungen und pusht zu origin/main.
cd /d "%~dp0"
echo.
echo === GitHub-Synchronisation Waermeplan ===
git add -A
git diff --cached --quiet || git commit -m "Synchronisation vom %date% %time%"
git pull --rebase origin main
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
