#!/bin/bash
# Gate für Linux/Cloud-Sitzungen (Auftrag #584-Übergabe, 27.09.2026): gate_linux.sh <Statusnummer> [Repo-Wurzel]
# Schritte wie gate_windows.sh, aber ohne tasklist-Wartezeit (keine fremden testhosts in der Cloud) und mit der
# Linux-Bildmesslatte Proben/ChartProben/Messlatte_2026-09-30.sha256 statt der Windows-Liste; zusätzlich der Referenzlauf
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
# Die Plattform der Basis steht im Quellpfad ihres protokoll.txt. Laufen Basis und Lauf auf verschiedenen Plattformen,
# sind Reste im Band byte-verschieden (Werkzeuge/Gate/LIESMICH.md); jedes rote Projekt ist ein Befund dieses Stands.
case "$(grep -m1 '^Quelle:' "Referenzlaeufe/$B/protokoll.txt" | sed 's/^Quelle: *//')" in /Users/*) BP=macOS;; /*) BP=Linux;; *) BP=Windows;; esac
case "$(uname -s)" in Linux*) LP=Linux;; Darwin*) LP=macOS;; MINGW*|MSYS*|CYGWIN*) LP=Windows;; *) LP=$(uname -s);; esac
echo "Basis $B (eingefroren auf $BP), Lauf auf $LP, Projekte $P"
rm -rf "$G/ref"; dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte "$P" --ziel "$G/ref" 2>&1 | grep -E 'Erfolgreich|Fehler' | tail -2
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- vergleich "Referenzlaeufe/$B" "$G/ref" > "$G/vergleich.txt" 2>&1
grep -E '^Projekt_[0-9]+: FAIL|^GESAMT' "$G/vergleich.txt"
grep -q '^GESAMT' "$G/vergleich.txt" || echo "REFERENZLAUF ROT: keine GESAMT-Zeile - Lauf oder Vergleich gescheitert, siehe $G/vergleich.txt"
n=0; g=0; v=""; for f in $(cd "Referenzlaeufe/$B" && find . -name '*.csv' | sort); do n=$((n+1)); if cmp -s "Referenzlaeufe/$B/$f" "$G/ref/$f"; then g=$((g+1)); else v="$v ${f#./}"; fi; done
echo "CSV byte-gleich: $g von $n"; [ -n "$v" ] && echo "byte-verschieden:$v"
echo "=== 6 Plattformnachweis: gestoerter Lauf (--stoerung ulp) gegen den ungestoerten aus Schritt 5"
rm -rf "$G/stoer"; dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte "$P" --ziel "$G/stoer" --stoerung ulp 2>&1 | grep -E 'Erfolgreich|Fehler|ABBRUCH' | tail -2
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- vergleich "$G/ref" "$G/stoer" > "$G/stoerung.txt" 2>&1
grep -E '^Projekt_[0-9]+: FAIL|^GESAMT' "$G/stoerung.txt"
echo "=== GATE-ENDE $(date +%H:%M:%S)"
