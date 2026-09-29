# Gate-Skripte der Wirtschaftlichkeits-Sitzung

- `gate_windows.sh <Nr> <Worktree>` — die bisherige Windows-Fassung (Git Bash): wartet mit `tasklist` auf fremde `testhost`-Prozesse,
  vergleicht die ChartProben mit der lokalen Windows-Messlatte `C:\Waermeplan\.claude\gate\messlatte_windows.sha256` (183 Hashes seit BV‑E5)
  und legt das Ergebnis unter `C:\Waermeplan\.claude\gate\GATE<Nr>` ab. Den Referenzlauf enthält sie nicht (eigener Schritt).
- `gate_linux.sh <Nr> [Repo-Wurzel]` — die Fassung für Cloud- und Linux-Sitzungen: Kern-Filter, ChartProben gegen die versionierte
  Linux-Messlatte `Proben/ChartProben/Messlatte_*.sha256`, Tests mit den xUnit-Schaltern, Dokumentationswachen und der Referenzlauf
  gegen die in `Referenzlaeufe/LIESMICH.md` genannte aktuelle Basis (mit eigenem Build von `EPOS.Referenzlauf`). Ablage unter
  `$GATE_ABLAGE/GATE<Nr>` (Vorgabe `/tmp/gate`).

Regeln, die beide voraussetzen: Tests nie ohne die Schalter `-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`;
`EPOS.Referenzlauf` vor jedem Referenzlauf bauen; das Bildmaß der Windows-Liste gilt nur auf Windows.

## Referenzlauf und Plattformbefund PB-1

Schritt 5 von `gate_linux.sh` vergleicht mit der Basis, die `Referenzlaeufe/LIESMICH.md` als aktuelle nennt.
**Drei Projekte rechnen auf Windows und Linux verschieden:** 1008, 1023 und 1042. Der Grund: `Math.Sin`,
`Cos`, `Asin`, `Acos` und `Exp` runden unter Windows (UCRT) und Linux (glibc) im letzten Bit verschieden,
und an zwei Stellen des Rechenkerns kippt davon eine Entscheidung. Ursache, Stellen und
Entscheidungsvorlage stehen in
[`PB1_Plattformbefund_Referenzlauf_Protokoll.md`](../../Dokumentation/ueberholt/Protokolle/Simulation/PB1_Plattformbefund_Referenzlauf_Protokoll.md).
Bis zum Anwenderentscheid gilt:

| Basis | Lauf | erwartet |
|---|---|---|
| R24 (auf Linux eingefroren) | Linux (`gate_linux.sh`, CI) | 15/15 PASS, 460/460 CSV byte-gleich |
| R24 | Windows | 1008 (52), 1023 (21), 1042 (2) FAIL; 441/460 byte-gleich, im Band verschieden dazu `heizstab.csv` von 1007 und 1046, `kessel_leistung.csv` und `kessel_strom.csv` von 1024 |
| eine auf Windows eingefrorene Basis (so R23) | Linux | 1008 (54), 1023 (21), 1042 (2) FAIL |

Schritt 5 liest die Plattform der Basis aus dem Quellpfad ihres `protokoll.txt`, zeigt alle roten Projekte
und die byte-verschiedenen CSV und schreibt zu jedem roten Projekt eine Einordnung:
- **„vorbestehender Plattformbefund PB-1“** — das Projekt steht in `PB1_PROJEKTE`, Basis und Lauf liegen auf
  verschiedenen Plattformen. Das ist kein Befund des geprüften Stands.
- **„PB-1-Projekt, aber … prüfen“** und **„NEU“** — das sind Befunde des geprüften Stands.

`gate_windows.sh` rechnet den Referenzlauf nicht. Wer ihn auf Windows gegen R24 rechnet, sieht die zweite
Zeile der Tabelle. Die CI (`kern.yml`) rechnet auf ubuntu gegen die Linux-Basis und ist nicht berührt.
