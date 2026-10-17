#!/usr/bin/env bash
# Nachgate nach weiteren Merges. Aufruf: nachgate_gebaeudesimulation.sh <Worktree> <Gatelog> (Log liegt neben dem Worktree).
# Umgebung: REF=1 (Referenzlauf), KERN_VOLL=1, ZIEL (Ordner des Referenzlaufs), REFBASIS (Basis, aktuelle aus Referenzlaeufe/LIESMICH.md).
# Wartet auf das Ende des Gates, dann Bau, UI-Tests voll, Kern-Tests gefiltert.
set -u
export DOTNET_ROOT=/usr/lib/dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 PYTHONIOENCODING=utf-8
WT="$1"; GATELOG="$2"; LOG="$(dirname "$WT")/nachgate_$(basename "$WT").log"
REFBASIS="${REFBASIS:-Referenzlaeufe/2026-10-17_R58_Kaeltesummen}"  # aktuelle Basis aus Referenzlaeufe/LIESMICH.md
XU='-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2'
until grep -q 'GATE-ENDE' "$GATELOG"; do sleep 30; done
cd "$WT" || exit 2
: > "$LOG"; declare -a ERG=()
schritt() { printf '\n===== %s (%s) =====\n' "$1" "$(date -u +%H:%M:%S)" >> "$LOG"; }
ergebnis() { ERG+=("$1: $2"); printf '>>> %s: %s\n' "$1" "$2" >> "$LOG"; }
echo "Nachgate auf $(git log --format=%h -n 1) begonnen $(date -u +%H:%M:%S)" >> "$LOG"

schritt "0 Marker und BOM"
marker=$(git grep -l '^<<<<<<< \|^>>>>>>> ' HEAD | wc -l)
bom=0; for f in $(git diff --name-only origin/ios_migration_september...HEAD | grep -E '\.(cs|razor|resx|csproj)$'); do [ -f "$f" ] && { head -c3 "$f" | od -An -tx1 | grep -q 'ef bb bf' || { bom=$((bom+1)); echo "ohne BOM: $f" >> "$LOG"; }; }; done
mdbom=0; for f in $(git diff --name-only origin/ios_migration_september...HEAD | grep -E '\.md$'); do [ -f "$f" ] && { head -c3 "$f" | od -An -tx1 | grep -q 'ef bb bf' && { mdbom=$((mdbom+1)); echo "md mit BOM: $f" >> "$LOG"; }; }; done
if [ "$marker" = 0 ] && [ "$bom" = 0 ] && [ "$mdbom" = 0 ]; then ergebnis "0 Dateien" "PASS"; else ergebnis "0 Dateien" "FAIL (Marker $marker, ohne BOM $bom, md mit BOM $mdbom)"; fi

schritt "1 Designer"
if python3 Werkzeuge/ResourceDesigner/designer_neu.py >> "$LOG" 2>&1; then ergebnis "1 Designer" "PASS"; else ergebnis "1 Designer" "FAIL"; fi

schritt "2 SQL-Prüfer"
if python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite >> "$LOG" 2>&1; then ergebnis "2 SQL-Prüfer" "PASS ($(tail -n 1 "$LOG" | cut -c1-80))"; else ergebnis "2 SQL-Prüfer" "FAIL"; fi

schritt "3 Bau Kern-Filter Release"
if dotnet build WP-Plan.Kern.slnf -c Release -nologo -v q -clp:ErrorsOnly >> "$LOG" 2>&1; then ergebnis "3 Bau Kern" "PASS"; else ergebnis "3 Bau Kern" "FAIL"; fi

schritt "4 Bau Windows-Schale"
if dotnet build WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Debug -p:Platform=x64 -p:EnableWindowsTargeting=true -nologo -v q -clp:ErrorsOnly >> "$LOG" 2>&1; then ergebnis "4 Bau Windows" "PASS"; else ergebnis "4 Bau Windows" "FAIL"; fi

schritt "5 EPOS.UI.Tests voll"
if dotnet test EPOS.UI.Tests -c Release --no-build --logger "console;verbosity=minimal" $XU >> "$LOG" 2>&1; then ergebnis "5 UI-Tests" "PASS ($(grep -E '^(Passed|Failed)!' "$LOG" | tail -n 1 | cut -c1-90))"; else ergebnis "5 UI-Tests" "FAIL ($(grep -E '^(Passed|Failed)!' "$LOG" | tail -n 1 | cut -c1-90))"; fi

if [ "${KERN_VOLL:-0}" = 1 ]; then
  schritt "6 EPOS.Kern.Tests voll"
  if dotnet test EPOS.Kern.Tests -c Release --no-build --logger "console;verbosity=minimal" $XU >> "$LOG" 2>&1; then ergebnis "6 Kern-Tests voll" "PASS ($(grep -E '^(Passed|Failed)!' "$LOG" | tail -n 1 | cut -c1-90))"; else ergebnis "6 Kern-Tests voll" "FAIL ($(grep -E '^(Passed|Failed)!' "$LOG" | tail -n 1 | cut -c1-90))"; fi
else
  schritt "6 EPOS.Kern.Tests gefiltert"
  F='FullyQualifiedName~Gbxml|FullyQualifiedName~GebaeudeImport|FullyQualifiedName~Zonenplan|FullyQualifiedName~Bauteilkatalog|FullyQualifiedName~Schema|FullyQualifiedName~Dokumentation|FullyQualifiedName~Repository|FullyQualifiedName~Wiki|FullyQualifiedName~Resource|FullyQualifiedName~Sqproj|FullyQualifiedName~Konditionierung|FullyQualifiedName~Raumnutzung|FullyQualifiedName~GebaeudeAnsicht|FullyQualifiedName~GebaeudeAufbau|FullyQualifiedName~ReferenzprojektWache|FullyQualifiedName~GebaeudeKatalog|FullyQualifiedName~Modellpruefung|FullyQualifiedName~ZonenNutzung|FullyQualifiedName~Ids|FullyQualifiedName~Datenaustausch|FullyQualifiedName~Rundlauf|FullyQualifiedName~Ifc|FullyQualifiedName~GebaeudeZonen|FullyQualifiedName~Wiederimport|FullyQualifiedName~Restdaten|FullyQualifiedName~GebaeudeExportAblauf|FullyQualifiedName~KiMaskenabdeckung|FullyQualifiedName~VorlagenfeldStilblatt'
  if dotnet test EPOS.Kern.Tests -c Release --no-build --filter "$F" --logger "console;verbosity=minimal" $XU >> "$LOG" 2>&1; then ergebnis "6 Kern-Tests gefiltert" "PASS ($(grep -E '^(Passed|Failed)!' "$LOG" | tail -n 1 | cut -c1-90))"; else ergebnis "6 Kern-Tests gefiltert" "FAIL ($(grep -E '^(Passed|Failed)!' "$LOG" | tail -n 1 | cut -c1-90))"; fi
fi

# Projektliste der CI-Auswahl: genau die Zeile „--projekte … --ziel artifacts/reflauf/…_kern“ des
# Referenzlauf-Schritts in kern.yml (der gestörte Lauf mit „_stoerung“ zählt nicht mit).
ci_projekte() {
  local ky=.github/workflows/kern.yml zeilen n
  zeilen=$(grep -E -- '--projekte [0-9,]+ --ziel artifacts/reflauf/.*_kern([[:space:]]|$)' "$ky" 2>/dev/null)
  n=$(printf '%s' "$zeilen" | grep -c .)
  if [ "$n" != 1 ]; then echo "Projektliste der CI-Auswahl nicht eindeutig: $n Fundstellen in $ky (erwartet genau eine)" >&2; return 1; fi
  printf '%s\n' "$zeilen" | sed -E 's/.*--projekte ([0-9,]+).*/\1/'
}

if [ "${REF:-0}" = 1 ]; then
 if ! PROJEKTE=$(ci_projekte 2>> "$LOG"); then
  schritt "7 Referenzlauf"; ergebnis "7 Referenzlauf" "FAIL (Projektliste der CI-Auswahl nicht ermittelbar, siehe Log)"
 else
  schritt "7 Referenzlauf $(printf '%s' "$PROJEKTE" | tr ',' '\n' | grep -c .) CI-Projekte gegen $(basename "$REFBASIS")"
  echo "Projekte: $PROJEKTE" >> "$LOG"
  ZIEL="${ZIEL:-${TMPDIR:-/tmp}/referenzlauf_nachgate}"; rm -rf "$ZIEL"; mkdir -p "$ZIEL"
  if dotnet build EPOS.Referenzlauf -c Release -nologo -v q -clp:ErrorsOnly >> "$LOG" 2>&1 \
     && dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte "$PROJEKTE" --ziel "$ZIEL" >> "$LOG" 2>&1 \
     && { dotnet run --project EPOS.Referenzlauf -c Release --no-build -- vergleich "$REFBASIS" "$ZIEL" > "$ZIEL/vergleich.txt" 2>&1 || true; } && grep -q 'Projekt_[0-9]*: PASS' "$ZIEL/vergleich.txt"; then
    cat "$ZIEL/vergleich.txt" >> "$LOG"
    echt=$(grep -E '^Projekt_[0-9]+: FAIL' "$ZIEL/vergleich.txt" | grep -vc 'nicht vorhanden')
    if [ "$echt" = 0 ]; then ergebnis "7 Referenzlauf" "PASS ($(grep -c ': PASS' "$ZIEL/vergleich.txt") Projekte PASS, $(grep -o '[0-9]* Werte' "$ZIEL/vergleich.txt" | awk '{s+=$1} END {print s}') Werte)"; else ergebnis "7 Referenzlauf" "FAIL ($echt echte FAIL-Zeilen)"; fi
  else
    cat "$ZIEL/vergleich.txt" >> "$LOG" 2>/dev/null; ergebnis "7 Referenzlauf" "FAIL"
  fi
 fi
fi

schritt "ZUSAMMENFASSUNG"
printf '%s\n' "${ERG[@]}" | tee -a "$LOG"
echo "NACHGATE-ENDE $(date -u +%H:%M:%S)" >> "$LOG"
