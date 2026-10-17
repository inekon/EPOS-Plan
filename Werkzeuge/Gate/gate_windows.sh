#!/bin/bash
# Gate-Variante für einen Worktree: gate_wt.sh <Statusnummer> <Worktree-Pfad>; Ablage wie gate.sh unter .claude/gate/GATE<nr>.
export PATH="/c/Program Files/dotnet:$PATH"
NR="${1:?Statusnummer}"; WT="${2:?Worktree}"
cd "$WT" || exit 1
S="/c/Waermeplan/.claude/gate"; G="$S/GATE$NR"; mkdir -p "$G/ablage"
warte() { while tasklist | grep -qi testhost; do echo "fremder testhost, warte 60 s ($(date +%H:%M:%S))"; sleep 60; done; }
echo "=== Beginn $(date +%H:%M:%S) auf $(git rev-parse --short HEAD) (Gate #$NR, Worktree $WT)"
echo "=== 1 Kern-Filter (Release)"
dotnet build WP-Plan.Kern.slnf -c Release -nologo -v q -clp:ErrorsOnly 2>&1 | grep -E 'error|Fehler' | head -10
echo "KERN-BUILD rc=${PIPESTATUS[0]} $(date +%H:%M:%S)"
echo "=== 2 ChartProben (Bau, Hashes, Proben)"
dotnet build Proben/ChartProben/ChartProben.csproj -c Release -nologo -v q -clp:ErrorsOnly 2>&1 | grep -E 'error|Fehler' | head -5
dotnet run --project Proben/ChartProben -c Release --no-build -- --ablage "$G/ablage" --hashes "$G/gate.sha256" 2>&1 | tail -2
echo "HASHES: $(grep -c -E '^[0-9a-f]{64}' "$G/gate.sha256") Zeilen; gleich mit Messlatte: $(diff <(sort "$S/messlatte_windows.sha256") <(sort "$G/gate.sha256") >/dev/null && echo JA || echo NEIN)"
echo "=== 3 Tests Kern-Filter"; warte
dotnet test WP-Plan.Kern.slnf -c Release --no-build --logger "console;verbosity=minimal" -- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2 2>&1 | grep -E 'Bestanden!|Fehler!|Passed!|Failed!|Fehlgeschlagen|\[FAIL\]|error' | head -60
echo "TESTS-FERTIG $(date +%H:%M:%S)"
echo "=== 4 Dokumentationswachen (nach den Tests, nie parallel)"; warte
dotnet test WP-Plan.Kern.slnf -c Release --no-build --filter "FullyQualifiedName~DokumentationLinkWache|FullyQualifiedName~RepositoryOrdnungWache|FullyQualifiedName~WikiProduktdatenWache" --logger "console;verbosity=minimal" 2>&1 | grep -E 'Bestanden!|Fehler!|Fehlgeschlagen|\[FAIL\]' | head -5
echo "WACHEN-FERTIG $(date +%H:%M:%S)"
echo "=== GATE-ENDE $(date +%H:%M:%S)"
