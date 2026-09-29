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

## Referenzlauf und Plattform

Schritt 5 von `gate_linux.sh` vergleicht mit der Basis, die `Referenzlaeufe/LIESMICH.md` als aktuelle nennt.
Er gibt die Plattform an, auf der sie eingefroren ist (aus dem Quellpfad ihres `protokoll.txt`), zeigt alle
roten Projekte und nennt die byte-verschiedenen CSV. Seit R25 rechnen Windows und Linux alle fünfzehn
Projekte innerhalb der Toleranz gleich; Ursache und Behebung des früheren Unterschieds stehen im Protokoll
[`PB1_Plattformbefund_Referenzlauf_Protokoll.md`](../../Dokumentation/ueberholt/Protokolle/Simulation/PB1_Plattformbefund_Referenzlauf_Protokoll.md).
**Jedes rote Projekt ist ein Befund des geprüften Stands.** Fehlt die Zeile `GESAMT`, ist Lauf oder Vergleich
gescheitert (etwa ohne `dotnet` im Pfad); Schritt 5 meldet das als „REFERENZLAUF ROT“.

| Basis | Lauf | erwartet |
|---|---|---|
| R25 (auf Windows eingefroren) | Windows | 15/15 PASS, 460/460 CSV byte-gleich |
| R25 | Linux (`gate_linux.sh`, CI) | 15/15 PASS; byte-verschieden nur Reste im Band: `heizstab.csv` von 1007 und 1046, `kessel_leistung.csv` und `kessel_strom.csv` von 1024, `puffer_soc.csv` von 1042 |

Die Linux-Zeile ist auf einem Linux-Läufer gemessen (Cloud-Gate): 455/460 CSV byte-gleich, byte-verschieden genau
die fünf Dateien der Tabelle. `gate_windows.sh` rechnet den Referenzlauf nicht; die CI
(`kern.yml`) rechnet auf ubuntu die sieben CI-Projekte.
