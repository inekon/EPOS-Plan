#!/bin/bash
# Gate der Orchestrierung: gate_haupt.sh <Nr> <Repo-Wurzel>
# Ruft Werkzeuge/Gate/gate_linux.sh und ergaenzt Windows-Schale, Designer, SqlDialektPruefer, Werkzeugtests der CI,
# BOM-Suche in Markdown und Konfliktmarker. Ablage unter $GATE_ABLAGE/GATE<Nr> (Vorgabe /tmp/gate).
set -u
NR="${1:?Statusnummer}"; WT="${2:?Repo-Wurzel}"; cd "$WT" || exit 1
G="${GATE_ABLAGE:-/tmp/gate}/GATE$NR"; mkdir -p "$G"
SCHALTER="-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2"
echo "=== GATE $NR Beginn $(date +%H:%M:%S) auf $(git rev-parse --short HEAD)"
bash Werkzeuge/Gate/gate_linux.sh "$NR" "$WT" 2>&1 | tee "$G/gate_linux.txt"
echo "=== 7 Windows-Schale auf Linux"
dotnet build WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Debug -p:Platform=x64 -p:EnableWindowsTargeting=true -nologo -v q -clp:ErrorsOnly 2>&1 | grep -E 'error' | head -10
echo "SCHALE rc=${PIPESTATUS[0]}"
echo "=== 8 Designer-Pruefung"
python3 Werkzeuge/ResourceDesigner/designer_neu.py 2>&1 | tail -2
echo "DESIGNER rc=${PIPESTATUS[0]}"
echo "=== 9 SqlDialektPruefer"
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite 2>&1 | tail -2
echo "SQL rc=${PIPESTATUS[0]}"
echo "=== 10 Werkzeugtests der CI"
for s in Werkzeuge/Formularkarte/Formularkarte.sln Werkzeuge/Auslieferungsvorlage/Auslieferungsvorlage.sln Werkzeuge/Gebaeudevergleich/Gebaeudevergleich.sln Werkzeuge/ZapfprofilValidierung/ZapfprofilValidierung.sln; do
  echo "--- $s"; dotnet test "$s" -c Release --logger "console;verbosity=minimal" 2>&1 | grep -E 'Bestanden!|Fehler!|Passed!|Failed!|error' | head -5
done
echo "=== 11 BOM in Markdown"
B=$(grep -rl $'^\xEF\xBB\xBF' --include='*.md' . 2>/dev/null | grep -v '/bin/\|/obj/' | head); echo "BOM-Markdown: ${B:-keine}"
echo "=== 12 Konfliktmarker"
K=$(grep -rn -E '^(<{7}|={7}|>{7})( |$)' --include='*.cs' --include='*.md' --include='*.razor' --include='*.resx' --include='*.csproj' . 2>/dev/null | grep -v '/bin/\|/obj/' | head -5); echo "Konfliktmarker: ${K:-keine}"
echo "=== GATE $NR Ende $(date +%H:%M:%S)"
echo GATE-FERTIG
