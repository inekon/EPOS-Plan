# Zweitlauf-Verifikation: bhkw_list_Namen wird jetzt geleert

**Datum:** 14.08.2026 · **Fix:** `SimulationControl.Simulation_BHKW_Ctrl` leert
`bhkw_list_Namen` an derselben Stelle wie `bhkw_list` (Bestandsfehler 4b-5 aus dem
Paket-4-Protokoll, Nebenbefund der Paket-4a-Umsetzung).

Der Referenzlauf rechnet jedes Projekt in einem eigenen Kindprozess und kann das
Zweitlauf-Szenario deshalb nicht abbilden. Verifiziert wurde daher zusaetzlich mit einem
Reflection-Harness (ein `SimulationRunner`, also EINE `SimulationControl`-Instanz, mehrere
`SimuliereUndSpeichere`-Aufrufe im selben Prozess) gegen die migrierte Arbeitskopie —
Sequenz 1017 → 1018 → 1017 → 1018.

## Mit Fix (Worktree-Build): STABIL, Exit 0

```
Lauf 1 Projekt 1017: KopfId=167 bhkw_list=1 Namen=1 Module=[BHKW EW K 10 S [K] Heizol]
Lauf 2 Projekt 1018: KopfId=168 bhkw_list=2 Namen=2 Module=[EC_Power_15kw.el Gas | EC_Power_6kw.el FL]
Lauf 3 Projekt 1017: KopfId=169 bhkw_list=1 Namen=1 Module=[BHKW EW K 10 S [K] Heizol]
Lauf 4 Projekt 1018: KopfId=170 bhkw_list=2 Namen=2 Module=[EC_Power_15kw.el Gas | EC_Power_6kw.el FL]
ERGEBNIS: STABIL
```

## Gegenprobe ohne Fix (identischer Stand, nur die neue Clear-Zeile entfernt): INSTABIL, Exit 1

```
Lauf 1 Projekt 1017: KopfId=171 bhkw_list=1 Namen=1 Module=[BHKW EW K 10 S [K] Heizol]
Lauf 2 Projekt 1018: KopfId=172 bhkw_list=2 Namen=3 Module=[BHKW EW K 10 S [K] Heizol | EC_Power_15kw.el Gas]
  -> NAMENSLISTE GEWACHSEN (3 statt 2)
Lauf 3 Projekt 1017: KopfId=173 bhkw_list=1 Namen=4 Module=[BHKW EW K 10 S [K] Heizol]
  -> NAMENSLISTE GEWACHSEN (4 statt 1)
Lauf 4 Projekt 1018: KopfId=174 bhkw_list=2 Namen=6 Module=[BHKW EW K 10 S [K] Heizol | EC_Power_15kw.el Gas]
  -> NAMENSLISTE GEWACHSEN (6 statt 2)
ERGEBNIS: INSTABIL
```

Projekt 1018 erbt ohne Fix den Modulnamen von Projekt 1017 — exakt die gemeldete
Namensverschiebung beim zweiten Simulationslauf derselben Sitzung.

## Regressionscheck Einzellauf (Referenzlauf-Suite, Kindprozess-Modus)

`lauf --projekte 1017,1018` einmal mit und einmal ohne Fix, dann `vergleich`:

```
Projekt_1017: PASS (20 Dateien, 245378 Werte)
Projekt_1018: PASS (19 Dateien, 210343 Werte)
GESAMT: PASS (455721 Werte innerhalb der Toleranz)
```

Im frischen Prozess ist das zusaetzliche `Clear()` ein No-op — Einzellaufergebnisse
unveraendert. Ordner `..._unfixed` enthaelt die Vergleichs-`aggregate.csv`.

Harness-Quelle: Konsolenprojekt nach dem Muster aus dem Verifikations-Memory
(net8.0-windows x86, `Assembly.LoadFrom` + Reflection, `Settings.DBPath` auf den
Arbeitskopie-Ordner, OleDb-DLL aus `runtimes\win\lib\net8.0` an die Bin-Wurzel).
