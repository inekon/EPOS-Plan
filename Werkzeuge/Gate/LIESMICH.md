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
