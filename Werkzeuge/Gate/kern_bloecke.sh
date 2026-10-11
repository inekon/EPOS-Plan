#!/bin/bash
# EPOS.Kern.Tests in Buchstabenblöcken: kern_bloecke.sh <Repo-Wurzel> <Ablage> <Block>... (Block = Anfangsbuchstaben der
# Testklassen, etwa AB CD EF GH IJ K LM NO PQ R S TUVWXYZ). Je Block ein Protokoll <Ablage>/<Block>.log; ein fertiger Block
# wird beim nächsten Aufruf übersprungen, sodass ein Container-Neustart nur den laufenden Block kostet. Zwei Aufrufe mit
# getrennten Blocklisten dürfen parallel laufen. Voraussetzung: EPOS.Kern.Tests ist im Release gebaut.
set -u
WT="$1"; S="$2"; shift 2; cd "$WT" || exit 1; mkdir -p "$S"
[ -x "$HOME/.dotnet/dotnet" ] && export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="$HOME/.dotnet"
for b in "$@"; do
  [ -f "$S/$b.fertig" ] && continue
  F=""; for c in $(echo "$b" | grep -o .); do F="$F|FullyQualifiedName~EPOS.Kern.Tests.$c"; done; F=${F#|}
  timeout 3000 dotnet test EPOS.Kern.Tests/EPOS.Kern.Tests.csproj -c Release --no-build --filter "$F" \
    --logger "console;verbosity=normal" -- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2 > "$S/$b.log" 2>&1
  rc=$?; echo "RC $rc" >> "$S/$b.log"
  z=$(grep -E '^Total tests:|^ +(Passed|Failed|Skipped):' "$S/$b.log" | tr -s ' ' | tr '\n' ' ')
  grep -q 'No test matches' "$S/$b.log" && z="Total tests: 0"
  [ -n "$z" ] && echo "$z" > "$S/$b.fertig"
  echo "$b: ${z:-kein Ergebnis} RC $rc"
  grep -E '^\s+Failed EPOS' "$S/$b.log" | head -10
done
