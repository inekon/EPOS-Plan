@echo off
setlocal
cd /d "%~dp0"
echo Neuberechnung des Multi-Use-Beispiels mit config_gui.json
if exist ".venv\Scripts\python.exe" (
  ".venv\Scripts\python.exe" "code\recalculate_example.py" --open
) else (
  python "code\recalculate_example.py" --open
)
if errorlevel 1 (
  echo.
  echo Die Berechnung wurde nicht abgeschlossen. Bitte die Meldung oben beachten.
  pause
)
