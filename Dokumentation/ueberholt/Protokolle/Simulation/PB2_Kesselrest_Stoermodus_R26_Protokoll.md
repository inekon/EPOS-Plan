# PB-2 — Kessel-Rechenrest, Störmodus-Wache, Basis R26 (#604)

Stand: 29.09.2026 · Zweig `ios_migration_september` · Commits `d31c4241` Kessellauf mit Zahlenrand,
`f09fbfe2` Störmodus-Wache, `07bd1531` Basis R26 eingefroren/R25 archiviert, `b262473b` Papiere
(Opus-Agent im Worktree, Zweig `plattform-nachzug` ab `4cf63281`); Merge `7d99408b`. Kein Schemaschritt.
Anwenderentscheide 29.09.2026: Stelle der Plattformabweichung suchen; Behebung übernehmen und neu
einfrieren; ±1-ulp-Störmodus als dauerhafte Wache; Kessel-Rechenrest und Wache auf den Stand von #599
nachziehen, Basis R26. Statuszeile „#604“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Vorgänger
[`PB1_Plattformbefund_Referenzlauf_Protokoll.md`](PB1_Plattformbefund_Referenzlauf_Protokoll.md) (#598/#599).

## 1 Anlass

Nach #595 rechneten 1008, 1023 und 1042 auf Windows und Linux jenseits der Toleranz verschieden
(„Nach #595“ (a)). Diese Sitzung hat die Stelle unabhängig von PB-1 gesucht und dieselbe Ursache gefunden.

## 2 Befund (Stand R23, `bd820bdb`, Linux gegen die Windows-Basis)

- **Ursache:** `Math.Exp`, `Math.Sin`, `Math.Cos` (Gebäudematrix, Sonnenstand, Erdreich) gehen an die
  C-Laufzeit der Plattform und weichen im letzten Bit ab; die Operanden der Entscheidungen selbst sind reine
  IEEE-Arithmetik. Eine lokale Störprobe (±1 ulp an etwa jedem 16. Ergebnis) erzeugt genau die Signatur
  zwischen Windows und Linux; Störungen an `Pow`, `Log`, Parsen oder Reihenfolge wirken nicht.
- **1008/1023:** Abschaltprüfung der Phase G (`Kaskadenschleife.cs`) ohne Zahlenrand; 1008 Stunde 2531:
  Füllstand 6,611999999999999 gegen Schwelle 6,612 (−1 ulp). 1636 (1008) bzw. 335 (1023) Entscheidungen
  liegen innerhalb ±2 ulp.
- **1042:** Rest von 4,4·10⁻¹⁶ kWh im Quellspeicher bucht eine volle Betriebsstunde (506 Scheinstunden).
- **1024:** `_kesselAbgabe > 0` zählt Rechenreste als Kessellauf; die Laufstunden gleichen sich zwischen den
  Plattformen nur zufällig aus.

## 3 Abgleich mit #599

Die PB-1-Sitzung hat Phase G und Quellspeicher parallel behoben (#599, R25 `Plattformrand`, auf Windows
eingefroren). Die Behebung dieser Sitzung (Zweig `plattform-schwelle`, auf Linux eingefroren) rechnete
dagegen **454/460 CSV byte-gleich**; verschieden nur die Reste `heizstab` 1007/1046, `puffer_soc` 1042 und
1024 (Kessel). Damit ist die Behebung von #599 auf beiden Plattformen belegt. `plattform-schwelle` wurde
nicht zusammengeführt; nachgezogen wurde nur, was #599 fehlte.

## 4 Umsetzung

- **Kessellauf:** `SimulationSPK.KesselLaeuft(abgabe)` verlangt `abgabe >= Rechenrand.ABSOLUT` (dieselbe
  Schwelle wie `QuellInhalt` aus #599); ein Rest darunter ist keine Laufstunde und kein Start.
  `PlattformrandTests` Abschnitt 3: gemessene Reste ±1 ulp, Gegenprobe, Quelltextwache, Lauf von 1024.
- **Störmodus-Wache:** Naht `Plattformrundung` für `Exp`, `Sin`, `Cos`, `Asin`, `Acos` in den fünf Dateien,
  die PB-1 als plattformwirksam gemessen hat; ohne Schalter bitgleich zu `Math.*` (`static readonly`).
  `EPOS.Referenzlauf lauf … --stoerung ulp` verschiebt etwa jedes 16. Ergebnis deterministisch um ±1 ulp.
  `kern.yml` rechnet die sieben CI-Projekte zusätzlich gestört und vergleicht mit dem ungestörten Lauf;
  `Werkzeuge/Gate/gate_linux.sh` Schritt 6 tut dasselbe über alle fünfzehn. `PlattformrundungTests` hält die
  Naht. Gegenprobe mit blanken Vergleichen: 1008, 1018, 1023, 1024, 1039, 1042 fallen durch.

## 5 Basis R26

`Referenzlaeufe/2026-09-29_R26_Kesselrest/` — fünfzehn Projekte, 460 CSV, **auf Linux** eingefroren,
Testdatenbank unverändert (`2e417b36…`, Schemastand 154); zweiter Lauf 460/460 byte-gleich. A/B gegen R25
(Windows): 14/15 PASS, 454/460 byte-gleich, getrennt nach Ursache:

| Ursache | Dateien | Befund |
|---|---|---|
| Rechenweg | `aggregate.csv` von 1024 | Kessel-Laufstunden 4 921 → 4 895, Starts 228 → 233, Bereitschaftsstunden 2 582 → 2 608 (0 kWh, Bereitschaftsleistung 0) |
| Plattformwechsel | `heizstab` 1007/1046, `kessel_leistung`/`kessel_strom` 1024, `puffer_soc` 1042 | Reste ≤ 8,9·10⁻¹⁶, innerhalb der Toleranz |

Der R25-Rechenweg auf Linux gegen R25 ergab GESAMT PASS, 455/460 byte-gleich — genau diese fünf Dateien.
Die Einfrierregel „gesäte Kesseldaten“ ist nicht berührt. R25 archiviert unter
[`Referenzbasen/`](../../Referenzbasen/LIESMICH.md).

## 6 Gate

Gate auf `7d99408b`: Kern-Filter 0 Fehler; Tests 16 929 grün, 0 rot (2 übersprungen); Werkzeugtests grün; SQL-Prüfer 0 Fundstellen (2 117 Texte); ChartProben 221 Prüfungen, 0 Verstöße; die sieben CI-Projekte gegen R26 PASS; gestört (`--stoerung ulp`) gegen ungestört PASS; Windows-Schale 0 Fehler. Nach dem Merge mit origin `e465f067` (#602/#603, Merge `75fc713b`): Kern-Filter 0 Fehler, `EPOS.UI.Tests` 6 908 grün, 758 gezielte Kern-Tests (Wachen, Wirtschaftlichkeit, Ergebnisansicht, Gruppenregel, Szenario, Plattform) grün, fünfzehn Projekte gegen R26 PASS, gestört gegen ungestört 15/15 PASS`.

## 7 Offene Punkte

1. Windows-Lauf gegen R26: erwartet 15/15 PASS mit den fünf Restdateien (Tabelle in
   `Werkzeuge/Gate/LIESMICH.md`).
2. Logbuch-Satz unter 1.2.0.5: „Ein Heizkessel zählt Rechenreste unter 10⁻⁹ kWh nicht mehr als Laufstunde oder
   Start.“
