@echo off
setlocal
cd /d "%~dp0"
if exist ".venv\Scripts\python.exe" (
  ".venv\Scripts\python.exe" "code\visualize_results.py" --open
) else (
  python "code\visualize_results.py" --open
)
if errorlevel 1 (
  echo.
  echo Die Auswertung konnte nicht erstellt werden. Bitte die Meldung oben beachten.
  pause
)
