#!/bin/bash
# Gate für Linux/Cloud-Sitzungen (Auftrag #584-Übergabe, 27.09.2026): gate_linux.sh <Statusnummer> [Repo-Wurzel]
# Schritte wie gate_windows.sh, aber ohne tasklist-Wartezeit (keine fremden testhosts in der Cloud) und mit der
# Linux-Bildmesslatte Proben/ChartProben/Messlatte_2026-09-26.sha256 statt der Windows-Liste; zusätzlich der Referenzlauf
# gegen die aktuelle Basis aus Referenzlaeufe/LIESMICH.md (EPOS.Referenzlauf wird eigens gebaut — es liegt nicht in der slnf).
set -u
NR="${1:?Statusnummer}"; WT="${2:-$(git rev-parse --show-toplevel)}"; cd "$WT" || exit 1
G="${GATE_ABLAGE:-/tmp/gate}/GATE$NR"; mkdir -p "$G/ablage"
SCHALTER="-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2"
echo "=== Beginn $(date +%H:%M:%S) auf $(git rev-parse --short HEAD) (Gate #$NR, $WT)"
echo "=== 1 Kern-Filter (Release)"
dotnet build WP-Plan.Kern.slnf -c Release -nologo -v q -clp:ErrorsOnly 2>&1 | grep -E 'error|Fehler' | head -10
echo "KERN-BUILD rc=${PIPESTATUS[0]} $(date +%H:%M:%S)"
echo "=== 2 ChartProben (Linux-Messlatte)"
dotnet build Proben/ChartProben/ChartProben.csproj -c Release -nologo -v q -clp:ErrorsOnly 2>&1 | grep -E 'error|Fehler' | head -5
dotnet run --project Proben/ChartProben -c Release --no-build -- --ablage "$G/ablage" --hashes "$G/gate.sha256" 2>&1 | tail -2
M=$(ls Proben/ChartProben/Messlatte_*.sha256 | sort | tail -1)
echo "HASHES: $(grep -c -E '^[0-9a-f]{64}' "$G/gate.sha256") Zeilen; gleich mit $M: $(diff <(sort "$M") <(sort "$G/gate.sha256") >/dev/null && echo JA || echo NEIN)"
echo "=== 3 Tests Kern-Filter"
dotnet test WP-Plan.Kern.slnf -c Release --no-build --logger "console;verbosity=minimal" $SCHALTER 2>&1 | grep -E 'Bestanden!|Fehler!|Passed!|Failed!|Fehlgeschlagen|\[FAIL\]|error' | head -60
echo "TESTS-FERTIG $(date +%H:%M:%S)"
echo "=== 4 Dokumentationswachen"
dotnet test WP-Plan.Kern.slnf -c Release --no-build --filter "FullyQualifiedName~DokumentationLinkWache|FullyQualifiedName~RepositoryOrdnungWache|FullyQualifiedName~WikiProduktdatenWache" --logger "console;verbosity=minimal" $SCHALTER 2>&1 | grep -E 'Bestanden!|Fehler!|Passed!|Failed!|\[FAIL\]' | head -5
echo "=== 5 Referenzlauf gegen die aktuelle Basis"
dotnet build EPOS.Referenzlauf/EPOS.Referenzlauf.csproj -c Release -nologo -v q -clp:ErrorsOnly 2>&1 | grep -E 'error|Fehler' | head -3
B=$(grep -o -m1 -E '\*\*`20[0-9]{2}-[0-9]{2}-[0-9]{2}_R[0-9]+[^`]*`\*\*' Referenzlaeufe/LIESMICH.md | tr -d '*`' | tr -d '/')
P=$(ls -d Referenzlaeufe/$B/Projekt_* | sed 's/.*Projekt_//' | sort -n | paste -sd, -)
echo "Basis $B, Projekte $P"
rm -rf "$G/ref"; dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte "$P" --ziel "$G/ref" 2>&1 | grep -E 'Erfolgreich|Fehler' | tail -2
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- vergleich "Referenzlaeufe/$B" "$G/ref" 2>&1 | grep -E 'GESAMT|FAIL' | tail -3
n=0; g=0; for f in $(cd "Referenzlaeufe/$B" && find . -name '*.csv'); do n=$((n+1)); cmp -s "Referenzlaeufe/$B/$f" "$G/ref/$f" && g=$((g+1)); done; echo "CSV byte-gleich: $g von $n"
echo "=== GATE-ENDE $(date +%H:%M:%S)"
