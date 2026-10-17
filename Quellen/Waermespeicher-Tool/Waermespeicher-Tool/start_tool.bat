@echo off
REM ============================================================================
REM  Waermespeicher-Tool - Startskript fuer Windows
REM
REM  Beim ersten Start wird eine virtuelle Umgebung (.venv) angelegt und alle
REM  Pakete aus requirements.txt installiert. Danach startet das Skript nur
REM  noch die Streamlit-Oberflaeche (schneller Start).
REM
REM  Voraussetzung: Python 3.10 oder neuer (python.org, Haken bei
REM  "Add Python to PATH" setzen).
REM ============================================================================

setlocal enabledelayedexpansion
cd /d "%~dp0"

echo.
echo ============================================================
echo   Waermespeicher-Tool
echo ============================================================
echo.

REM --- 1) Python suchen: erst der Launcher "py", dann "python" ---------------
set "PYCMD="
py -3 --version >nul 2>&1
if %errorlevel%==0 (
    set "PYCMD=py -3"
) else (
    python --version >nul 2>&1
    if !errorlevel!==0 set "PYCMD=python"
)

if not defined PYCMD (
    echo [FEHLER] Es wurde keine Python-Installation gefunden.
    echo.
    echo   Bitte Python 3.10+ von https://www.python.org/downloads/ installieren
    echo   und bei der Installation "Add Python to PATH" aktivieren.
    echo.
    pause
    exit /b 1
)

echo [1/3] Python gefunden:
%PYCMD% --version

REM --- 2) virtuelle Umgebung anlegen (nur beim ersten Start) ----------------
if not exist ".venv\Scripts\python.exe" (
    echo.
    echo [2/3] Virtuelle Umgebung .venv wird angelegt ^(einmalig^) ...
    %PYCMD% -m venv .venv
    if errorlevel 1 (
        echo [FEHLER] Anlegen der virtuellen Umgebung fehlgeschlagen.
        pause
        exit /b 1
    )
    set "NEUINSTALL=1"
) else (
    echo.
    echo [2/3] Virtuelle Umgebung .venv vorhanden.
    set "NEUINSTALL=0"
)

set "VENVPY=.venv\Scripts\python.exe"

REM --- 3) Abhaengigkeiten installieren --------------------------------------
REM  Neuinstallation immer, sonst nur wenn Streamlit fehlt (z. B. abgebrochene
REM  Erstinstallation) oder wenn der Nutzer start_tool.bat update aufruft.
set "INSTALL=0"
if "%NEUINSTALL%"=="1" set "INSTALL=1"
if /i "%~1"=="update" set "INSTALL=1"
"%VENVPY%" -c "import streamlit" >nul 2>&1
if errorlevel 1 set "INSTALL=1"

if "%INSTALL%"=="1" (
    echo.
    echo [3/3] Pakete werden installiert ^(dauert beim ersten Mal einige Minuten^) ...
    "%VENVPY%" -m pip install --upgrade pip
    "%VENVPY%" -m pip install -r requirements.txt
    if errorlevel 1 (
        echo.
        echo [FEHLER] Installation der Pakete fehlgeschlagen.
        echo   Pruefen Sie die Internetverbindung bzw. Proxy-/Firewall-Einstellungen.
        pause
        exit /b 1
    )
) else (
    echo [3/3] Pakete bereits installiert ^(Update erzwingen: start_tool.bat update^).
)

REM --- Start -----------------------------------------------------------------
echo.
echo Streamlit wird gestartet - der Browser oeffnet sich automatisch.
echo Zum Beenden dieses Fenster schliessen oder STRG+C druecken.
echo.
"%VENVPY%" -m streamlit run app.py

if errorlevel 1 (
    echo.
    echo [FEHLER] Streamlit konnte nicht gestartet werden - siehe Meldung oben.
    pause
    exit /b 1
)

endlocal
