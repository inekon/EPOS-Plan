# Reparatur des PV-Modulkatalogs — der Handlauf und sein Nachfolger

**Seit dem 07.09.2026 macht das Programm diese Arbeit selbst: Migrationsschritt 69**
(`EPOS.Kern/Allgemein/Update/PvKoeffizientenReparatur.cs`, Befund **W6‑B‑5** mit den
Anwenderentscheiden **Q1–Q3**). Jede Anwenderdatenbank wird beim nächsten Programmstart
migriert und dabei repariert — mit Sicherung, mit Protokollzeile je Satz und idempotent.
**Die Skripte hier sind damit nicht mehr der Weg**; sie bleiben als **Handlauf für
Altbestände** und als Beleg der ursprünglichen Messung liegen.

## Was hier liegt

| Datei | Wofür |
|---|---|
| `messung_pv_katalog.py` | Liest `Tab_PV_STAMM` und `Tab_PV` **nur lesend** (URI `mode=ro`) und klassifiziert jedes der vier Felder: `=I_Kurzschluss`, `NULL`, `0`, `plausibel`, `unplausibel`. Markdown- und CSV-Bericht. **Die vier physikalischen Fenster dieses Skripts sind die Quelle** für `PvKoeffizientenReparatur.ALPHA_MIN…NOCT_MAX` |
| [`messung_tab_pv_VORHER.md`](../../Dokumentation/ueberholt/Protokolle/sql/messung_tab_pv_VORHER.md) | Die Messung der Produktivdatenbank vom 02.09.2026 — der Befund im Original: elf reparaturbedürftige Zeilen (seit Auftrag #241 unter `Dokumentation/ueberholt/Protokolle/sql/`) |
| [`messung_tab_pv_NACHHER_probe.md`](../../Dokumentation/ueberholt/Protokolle/sql/messung_tab_pv_NACHHER_probe.md) | Die Kontrollmessung nach dem Trockenlauf (ebenda) |
| `reparatur_pv_katalog.sql` | Elf `UPDATE`, **je mit einem Wächter auf die gemessenen Ist-Werte einer bestimmten ID**. Ein zweiter Lauf ändert 0 Zeilen |
| `reparatur_pv_katalog.py` | Der Runner dazu: Vorbedingungen (keine `-wal`/`-shm`), datierte Sicherung, Transaktion, `changes()` je Anweisung, Kontrollmessung. Ohne `--ausfuehren` ein Trockenlauf mit `ROLLBACK`; die Produktivdatenbank braucht zusätzlich `--produktiv-freigegeben` |

## Warum die Skripte trotzdem bleiben

* **Sie sind der Beleg.** [`messung_tab_pv_VORHER.md`](../../Dokumentation/ueberholt/Protokolle/sql/messung_tab_pv_VORHER.md) ist die Messung, aus der der Befund A1
  überhaupt entstanden ist. Ohne sie stünde in `PvKoeffizientenReparatur` eine Regel ohne
  nachlesbare Herkunft.
* **Sie arbeiten an einer Datei, die das Programm nicht anfasst.** Eine Sicherungskopie, ein
  Bestand auf einem Rechner ohne EPOS-Plan, eine Datei, die man vor dem Update ansehen will —
  dafür ist der read-only-Messlauf da.
* **Gefährlich sind sie nicht.** Jedes `UPDATE` trägt seinen Wächter auf die gemessenen
  Ist-Werte **einer** ID; auf einer bereits durch Schritt 69 reparierten Datei greift kein
  einziger, der Lauf meldet „0 geänderte Zeilen".

## Der eine gewollte Unterschied zwischen Skript und Schritt 69

Das Skript trägt für **„Jinkosolar JKM 260P-60"** und **„LG Electronics LG 320 N1K-A5"**
Werte aus PVsyst-`.PAN`-Dateien ein (`muISC/1000`, `muVocSpec/1000`, `muPmpReq`, `T_NOCT = 0`).
**Schritt 69 tut das nicht** — er setzt diese Felder auf `NULL`. Der Grund steht im Entscheid
**Q1**: repariert wird aus der **CEC-Liste**, und die führt diese zwei Module nicht (ihre
Schwesterzeilen „Jinko Solar Co, Ltd JKM260P-60" und „LG Electronics Inc, LG320N1K-A5"
stammen aus einem anderen Prüflabor — I_sc 8,98 statt 9,014 bzw. 10,19 statt 10,35). Eine
Zahl aus einer zweiten Quelle daneben zu stellen hiesse, den Katalog aus zwei Messprotokollen
zu mischen; `NULL` sagt stattdessen ehrlich „nicht gepflegt", die Strangampel meldet „fehlt"
und die Simulation nimmt ihren NOCT-Rückfall. **Wer die PAN-Werte will, pflegt sie im
Modulkatalog von Hand ein** — Schritt 69 fasst einen gesunden Wert nie wieder an.
