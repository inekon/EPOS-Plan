# Gebaeudevergleich

Vergleicht an einer Momentaufnahme der Arbeitsdatenbank je Gebäude den Tagesbilanz-Weg
(„alt") mit dem VDI-6007-Weg („neu") — Bedingung (1) des Ablösekriteriums Q24/E27 der
Gebäudesimulation. Konzept: [Umsetzungskonzept Gebäudesimulation](../../Dokumentation/aktuell/Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Löschliste 6.1 (das Werkzeug fällt mit Stufe GA).

## Aufruf

```
dotnet build Werkzeuge/Gebaeudevergleich/Gebaeudevergleich.sln -c Release
Gebaeudevergleich aufnahme  --quelle <sqlite> --ziel <ordner>
Gebaeudevergleich vergleich --db <sqlite> --ziel <ordner> [--projekte a,b] [--mit-namen] [--stundenreihen]
Gebaeudevergleich variante  --db <sqlite> --modell TAGESBILANZ|VDI6007 --ziel <datei>
```

Rückgabe: 0 = keine rote Zeile, 1 = mindestens eine rote Zeile, 2 = Abbruch mit Grund.

## Ablauf an der Arbeitsdatenbank

1. EPOS-Plan in der Fassung des Werkzeugstands starten: Es migriert die Arbeitsdatenbank mit
   eigener Sicherung auf `SchemaStand.Zielversion`. Danach das Programm **schließen**.
2. `aufnahme --quelle C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite --ziel %TEMP%\EPOS_Gebaeudevergleich\<stempel>`
   — `immutable=1`, `VACUUM INTO`, SHA-256 davor und danach. Neben der Quelle entsteht keine
   Datei. Läuft `EPOS_Plan`, liegt eine nichtleere `-wal` oder eine `-journal` daneben, bricht
   die Aufnahme ab.
3. `vergleich --db <stempel>\aufnahme.sqlite --ziel <stempel>\ergebnis` — ohne `--mit-namen`
   steht in keiner Ausgabe ein Projekt-, Kunden-, Bearbeiter- oder Gebäudename.
4. Projektwirkung: `variante` je Rechenweg, beide Kopien mit `EPOS.Referenzlauf lauf` rechnen
   und mit `EPOS.Referenzlauf vergleich` gegenüberstellen.

**Ersatzweg der Migration** (nur wenn die Programm-Migration scheitert): Die Momentaufnahme
über den Windows-Referenzlauf des eigenen Worktrees migrieren —
`dotnet build Referenzlauf/Referenzlauf.csproj -c Release -p:Platform=x64`, dann
`Referenzlauf.exe lauf --quelle <aufnahme.sqlite> --projekte 999999 --ziel <ordner>` mit der
Konsole in einer Datei. Die Projekt-ID gibt es nicht: Migriert wird vor der Projektauswahl,
danach endet der Lauf mit Exit 2 ohne Simulation. Aus der Datei nur die Zeilen
`Projektwurzel:` und `Migration: ERFOLG (Zielstand …)` lesen. Dann
`aufnahme --quelle <worktree>/Referenzlaeufe/Arbeitskopie/Kenndaten.sqlite --ziel …\migriert`
und `Referenzlaeufe/Arbeitskopie/` sofort leeren.

## Sicherheit

- Ausgabe nur außerhalb des Repositoriums und nie unter `%ProgramData%\EPOS_PLAN`;
  `vergleich` und `variante` lesen nie eine `--db` unter `%ProgramData%\EPOS_PLAN`.
- Die Schreibnaht steht auf „nein" (`Schreibrecht = () => false`); das Werkzeug liest.
  Eigene SQL sind drei feste Texte über eigene Verbindungen: `VACUUM INTO ?`,
  `PRAGMA integrity_check` und `UPDATE Tab_Gebaeude SET Gebaeude_Modell = ?` (nur `variante`,
  nur auf der Kopie).
- `zusammenfassung.md` trägt weder Namen noch IDs; sie ist der einzige Teil für die Papiere.

## Ausgabe von `vergleich`

`gebaeude.csv` (Merkmale, Bauform-Gruppe, Kennzahlen alt/neu mit Δ, Katalog- und
Verbrauchstreffer, Ampel, Regeln, bereinigte Meldungen), `gebaeude_monate.csv`,
`projekte.csv` (samt übersprungener Projekte und Summenzeile), `stunden/` (mit
`--stundenreihen`), `bericht.html`, `zusammenfassung.md`, `protokoll.txt`. Die Spitzen sind
Spitzen der Stundenreihe, keine Normheizlast.

Ampel: **Fehler** (ein Weg gescheitert, ein unplausibler Wert, U-BW „Datenfehler"),
**erklärt** (U-E8 im Band E8, oder Flächenangabe mit Δ Jahr 5–50 % ohne Katalogwert bzw. mit
Katalogtreffer 90–115 %), sonst **zu prüfen**. Die Regeln und ihre Schwellen stehen mit
Quelle in `Ursachenregeln.cs`.

## Proben

`dotnet test Werkzeuge/Gebaeudevergleich/Gebaeudevergleich.sln -c Release` — T1 bis T15 gegen
Kopien der Testdatenbank und die Referenzbasis unter `Referenzlaeufe/`; ohne Testdatenbank
(nur LFS-Zeiger) schweigen die Fälle.
