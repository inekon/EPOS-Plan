#!/bin/bash
# Gate ohne Kern-Testlauf für Cloud-Sitzungen: gate_rest_linux.sh <Repo-Wurzel> <Ablage>
# Für Stände, deren EPOS.Kern.Tests die CI auf demselben Commit fährt (Push auf den Sitzungszweig): Ein Container-Neustart
# beendet den rund 75 Minuten langen Kern-Testlauf von gate_linux.sh; dieser Rest dauert rund 15 Minuten. Schritte: Kern-Filter,
# kleine Testprojekte, Dokumentationswachen, Windows-Schale auf Linux, Designer, SQL-Dialekt, Werkzeugtests der CI, BOM,
# Konfliktmarker, Referenzlauf aller Projekte der aktuellen Basis samt Byte-Vergleich. Die ChartProben prüft die CI.
set -u
WT="$1"; G="$2"; cd "$WT" || exit 1; mkdir -p "$G"
[ -x "$HOME/.dotnet/dotnet" ] && export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="$HOME/.dotnet"
SCHALTER="-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2"
echo "=== REST Beginn $(date +%H:%M:%S) auf $(git rev-parse --short HEAD)"
echo "=== 1 Kern-Filter (Release, inkrementell)"
dotnet build WP-Plan.Kern.slnf -c Release -nologo -v q -clp:ErrorsOnly 2>&1 | grep -E 'error|Fehler' | head -10
echo "KERN-BUILD rc=${PIPESTATUS[0]} $(date +%H:%M:%S)"
echo "=== 3b kleine Testprojekte"
for p in EPOS.UI.Tests KiKern.Tests SpeicherEngine.Tests SpeicherPlanung.Tests; do
  echo "--- $p"; dotnet test $p -c Release --no-build --logger "console;verbosity=minimal" $SCHALTER 2>&1 | grep -E 'Bestanden!|Fehler!|Passed!|Failed!|\[FAIL\]|error' | head -10
done
echo "=== 4 Dokumentationswachen"
dotnet test EPOS.Kern.Tests -c Release --no-build --filter "FullyQualifiedName~DokumentationLinkWache|FullyQualifiedName~RepositoryOrdnungWache|FullyQualifiedName~WikiProduktdatenWache" --logger "console;verbosity=minimal" $SCHALTER 2>&1 | grep -E 'Bestanden!|Fehler!|Passed!|Failed!|\[FAIL\]' | head -5
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
echo "=== 5 Referenzlauf gegen die aktuelle Basis (alle Projekte)"
dotnet build EPOS.Referenzlauf/EPOS.Referenzlauf.csproj -c Release -nologo -v q -clp:ErrorsOnly 2>&1 | grep -E 'error|Fehler' | head -3
B=$(grep -o -m1 -E '\*\*`20[0-9]{2}-[0-9]{2}-[0-9]{2}_R[0-9]+[^`]*`\*\*' Referenzlaeufe/LIESMICH.md | tr -d '*`' | tr -d '/')
P=$(ls -d Referenzlaeufe/$B/Projekt_* | sed 's/.*Projekt_//' | sort -n | paste -sd, -)
echo "Basis $B, Projekte $P"
rm -rf "$G/ref"; dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte "$P" --ziel "$G/ref" 2>&1 | grep -E 'Erfolgreich|Fehler' | tail -2
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- vergleich "Referenzlaeufe/$B" "$G/ref" > "$G/vergleich.txt" 2>&1
grep -E '^Projekt_[0-9]+: FAIL|^GESAMT' "$G/vergleich.txt"
grep -q '^GESAMT' "$G/vergleich.txt" || echo "REFERENZLAUF ROT: keine GESAMT-Zeile, siehe $G/vergleich.txt"
n=0; g=0; v=""; for f in $(cd "Referenzlaeufe/$B" && find . -name '*.csv' | sort); do n=$((n+1)); if cmp -s "Referenzlaeufe/$B/$f" "$G/ref/$f"; then g=$((g+1)); else v="$v ${f#./}"; fi; done
echo "CSV byte-gleich: $g von $n"; [ -n "$v" ] && echo "byte-verschieden:$v" | cut -c1-400
echo "=== REST Ende $(date +%H:%M:%S)"
echo REST-FERTIG
