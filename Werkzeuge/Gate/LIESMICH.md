# Gate-Skripte der Wirtschaftlichkeits-Sitzung

- `gate_windows.sh <Nr> <Worktree>` — die bisherige Windows-Fassung (Git Bash): wartet mit `tasklist` auf fremde `testhost`-Prozesse,
  vergleicht die ChartProben mit der lokalen Windows-Messlatte `C:\Waermeplan\.claude\gate\messlatte_windows.sha256` (183 Hashes seit BV‑E5)
  und legt das Ergebnis unter `C:\Waermeplan\.claude\gate\GATE<Nr>` ab. Den Referenzlauf enthält sie nicht (eigener Schritt).
- `gate_linux.sh <Nr> [Repo-Wurzel]` — die Fassung für Cloud- und Linux-Sitzungen: Kern-Filter, ChartProben gegen die versionierte
  Linux-Messlatte `Proben/ChartProben/Messlatte_*.sha256`, Tests mit den xUnit-Schaltern, Dokumentationswachen und der Referenzlauf
  gegen die in `Referenzlaeufe/LIESMICH.md` genannte aktuelle Basis (mit eigenem Build von `EPOS.Referenzlauf`). Ablage unter
  `$GATE_ABLAGE/GATE<Nr>` (Vorgabe `/tmp/gate`).

Regeln, die beide voraussetzen: Tests nie ohne die Schalter `-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`;
`EPOS.Referenzlauf` vor jedem Referenzlauf bauen; das Bildmaß der Windows-Liste gilt nur auf Windows. Kommen Probebilder
hinzu, zieht das nächste Windows-Gate die lokale Liste nach — jede alte Zeile muss gleich bleiben, die neuen kommen dazu.
Ändert eine gewollte Bildänderung Zeilen, nennt der Abschnitt der Etappe in `Proben/ChartProben/LIESMICH.md` genau diese
Bilder, und nur sie dürfen abweichen. Gegenwärtig (`Messlatte_2026-09-30.sha256`): neun Bilder der Kalenderkarte neu, die
zwölf Bilder der Stufenregel des Stapels geändert.

## Referenzlauf und Plattform

Schritt 5 von `gate_linux.sh` vergleicht mit der Basis, die `Referenzlaeufe/LIESMICH.md` als aktuelle nennt.
Er gibt die Plattform an, auf der sie eingefroren ist (aus dem Quellpfad ihres `protokoll.txt`), zeigt alle
roten Projekte und nennt die byte-verschiedenen CSV. Windows und Linux rechnen alle sechzehn Projekte
innerhalb der Toleranz gleich; Ursache und Behebung des früheren Unterschieds stehen im Protokoll
[`PB1_Plattformbefund_Referenzlauf_Protokoll.md`](../../Dokumentation/ueberholt/Protokolle/Simulation/PB1_Plattformbefund_Referenzlauf_Protokoll.md).
**Jedes rote Projekt ist ein Befund des geprüften Stands.** Fehlt die Zeile `GESAMT`, ist Lauf oder Vergleich
gescheitert (etwa ohne `dotnet` im Pfad); Schritt 5 meldet das als „REFERENZLAUF ROT“.

| Basis | Lauf | erwartet |
|---|---|---|
| R32 (auf Linux eingefroren) | Linux (`gate_linux.sh`, CI) | 16/16 PASS, 487/487 CSV byte-gleich |
| R32 | Windows | 16/16 PASS; byte-verschieden nur Reste im Band: `heizstab.csv` von 1007 und 1046, `kessel_leistung.csv` und `kessel_strom.csv` von 1024, `puffer_soc.csv` von 1042 |

Die Windows-Zeile ist die Gegenrichtung des gemessenen Plattformwechsels: Der Rechenweg der Basis R25 ergab
auf Linux gegen die Windows-Basis R25 genau diese fünf Dateien (GESAMT PASS, 455/460), und die Kesselregel
von R26 streicht auf beiden Plattformen dieselben Reststunden; Teil- und Brennwertkennlinie des Kessels sind
lineare Arithmetik ohne Funktion der Plattformnaht, Brennwertbetrieb und Takten (Mindestleistung, Vielfache eines
Mindestlaufs) entscheiden am Zahlenrand, die Jahressumme der Stromprofile ist eine Skalierung, die Untergrenze des
BHKW ist eine Schwelle am Zahlenrand, und keine fügt eine
Plattformkante hinzu — der gestörte Lauf hält `aggregate.csv` aller sechzehn Projekte byte-gleich. Auf Windows
nachgerechnet ist die Zeile noch nicht.
`gate_windows.sh` rechnet den Referenzlauf nicht; die CI (`kern.yml`) rechnet auf ubuntu die sieben
CI-Projekte.

Schritt 6 rechnet dieselben Projekte mit `--stoerung ulp` (±1 ulp an Exp, Sin, Cos, Asin und Acos der
Naht `Plattformrundung`) und vergleicht mit dem ungestörten Lauf aus Schritt 5; erwartet ist GESAMT PASS
(Abschnitt „Der Plattformnachweis“ in `Referenzlaeufe/LIESMICH.md`). Ein FAIL dort ist eine Entscheidung,
die am letzten Bit kippt.
