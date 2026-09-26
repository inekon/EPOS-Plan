# E29 — Anzeige-Welle Stromausweis: BHKW-Einspeisung, Strombilanz und Excel am Gesamtbedarf, Übersicht mit Kältestrom, PV-Deckungsgrad geklemmt, Kesselstrom im Stromgang (Protokoll, 26.09.2026)

Statuszeile #536 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: die Anwenderentscheide
vom 26.09.2026 (~08:25) zu den Restpunkten nach #531 — E27‑Q3 **b** (eigene Diagnosereihe und Zeile „BHKW-Einspeisung“
im BHKW-Reiter; Statusdatei, Nach #521; Register R‑E27) und E26‑Q6/N6 „in einer kleinen Welle nachziehen“ (Nach #518;
Register R‑E26) —, dazu der Restpunkt aus E28 (Nach #535 (e) (1), Strombedarfsdeckung der PV-Zeile); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 3.6 (Absatz „BHKW-Einspeisung und Gesamtbedarf im Ausweis“) und § 6.3 Nr. 34 und 36;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E29 (neu), R‑E26 (E26‑Q6), R‑E27 (E27‑Q3) und R‑E28 (E28‑Q3); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5. Vorgänger: [`E28_Stromstufeneingang_Klemme_Protokoll.md`](E28_Stromstufeneingang_Klemme_Protokoll.md). Zweig `e29`
von `5826f97d`; Opus 5.5 im Worktree `.claude/worktrees/e29`: `71d74522` (E29/1), `656718a1` (E29/2), `39e540c3`
(E29/3), `30572adb` (E29/4), `83418db8` (E29/5), `80b29c7f` (E29/6), `ed838081` (Nachschliff); 18 Dateien, +667/−18.
Merge `32d84023` („Merge e29: Anzeige-Welle — BHKW-Einspeisung, Strombilanz und Excel am Gesamtbedarf, Übersicht mit
Kältestrom, PV-Deckung geklemmt, Kesselstrom im Stromgang (#536)“) auf `pm26` über `origin` = `4121813c` (G6b W5,
#538–#540; Zwischenmerge `235f09e9`). **Kein Schemaschritt, keine Neueinfrierung** — die Referenzbasis
`2026-09-26_R20_Zapfprofil` bleibt, die Testdatenbank ist von E29 unverändert (`3ac19fa9`).

## Befund vor der Welle (Phase 0, Worktree `e29` = `5826f97d`, ohne Kern-Eingriff)

Die Zeilennummern sind die vor E29.

1. **Alles ist Ausweis.** Kein Kernwert der Wirtschaftlichkeit ändert sich, keine CSV der R20 wandert
   (`Referenzlauf/Ergebnisexport.cs` liest weder `ZeitreihenSatz` noch `StromMatrix` noch `SimulationErgebnisCtrl`), und
   bei der Umstellung mit Rückfall bleibt jeder ChartProben-Hash gleich.
2. **Die BHKW-Einspeisung gibt es schon genau:** stündlich im KWK-Split der Strommatrix (`StromMatrix.cs:227-238`),
   `Einspeisung_h = max(0, BHKW_h − min(BHKW_h, max(0, STROMBEDARF_GESAMT_h − PV_GENUTZT_h)))`, Jahressumme
   `KwkEinspeisungGesamtMWh` (`:65/:237`). `STROMBEDARF_GESAMT` ist der Rest nach der Kaskade plus BHKW-Strom
   (`SimulationControl.cs:566-572`); spätere Verbraucher derselben Viertelstunde stecken darin, die Klemme am Laufende
   (E27) setzt nur den Netzbezug auf 0. Die Diagnosereihe `ZeitreihenSatz.BHKW_UEBERSCHUSS` (`BerichtsDaten.cs:470`)
   stand nur mit PV (`SimulationPV.BhkwUeberschuss`, dieselbe Größe) oder mit Flotte (`BhkwNetzeinspeisungKw`), ohne
   beide (1018, 1030) fehlte sie; der BHKW-Reiter kannte nur Stufengrößen.
3. **Strombilanz-Diagramm und Excel „Strombedarf“** (`ChartRenderer.cs:455-476`, `ExcelBerichtGenerator.cs:1830`) lasen
   den Projektbedarf `STROMBEDARF`; die ChartProben zeichnen aus einem Satz ohne `STROMBEDARF_GESAMT`, der Rückfall hält
   `strombilanz_monate.png` bei `6d916351…`.
4. **N6:** Die Übersicht „Strombedarf mit Eigenverbrauch“ (`SimulationErgebnisCtrl.cs:199-202`) rechnete Projekt + WP +
   Heizstab + Kessel ohne Kältestrom; mit Kälte der Stufenrechnung enthält `ReststromMwh` den Kältestrom, der Nenner des
   Stromrings nicht — Deckung zu hoch, Rest und Deckung über 100 % möglich.
5. **PV-Deckungsgrad** (`SimulationRunner.cs:1036-1037`, `SimulationErgebnisCtrl.cs:858/864`): Nenner Σ
   `Strombedarf_stuendlich` ungeklemmt, hinter einem BHKW-Überschuss überhöht; kein Projekt betroffen.
6. **N9 (neu):** Der Stromgang der Ergebnisansicht (`SimulationErgebnisHuelle.Bilder.cs:932-933`, Summenlinie
   `:950-955`, CSV `Wege.cs:316-318`) las als „Heizkessel“ `simulation_spk.Strombedarf_stuendlich`, den Strom-Stufeneingang
   des Kessels, nicht den Stromverbrauch des Elektrokessels (`Stromverbrauch_stuendlich`, `SimulationSPK.cs:48/426`).
7. **N10 (neu):** Stromring, Stromtabelle und Word-Deckungstorte zählen beim BHKW die ganze Produktion samt Einspeisung;
   `BHKW.Strombedarfsdeckung` teilt durch den Projekt-Strombedarf (bewegt R20).
8. **N11 (neu):** Das Strombilanz-Diagramm stapelt die Flotten-Netzeinspeisung in der Deckung statt im Nebenbalken.

## Messung

| Projekt | BHKW-Einspeisung (= Stufenüberschuss) | Stromgang „Heizkessel“ vorher | nachher |
|---|---|---|---|
| 1017 | 0 (keine Überschussstunde) | 635,2 MWh | 20,12 MWh |
| 1018 | 27,4575 MWh (Strombedarf 0, BHKW 27,46) | – | – |
| 1024 | 0 | 409,31 MWh | 47,67 MWh |
| 1030 | 0,392 MWh (12 h) | 4.790,09 MWh | 0 |
| 1047 | 0 | 640,19 MWh | 1,0 MWh |

Strombilanz-Linie und Excel „Strombedarf“ an 1040: 8,0 → 27,4 MWh/a (E26-Werte der Phase 0: 1026 8,000 → 31,351,
1042 8,000 → 41,345). N10 in R20: `BHKW.Strombedarfsdeckung` 1017 5,48 %, 1024 26,22 %, 1030 9,02 %, 1047 5,34 %.
N11 an 1046: 0,895 MWh/a (`Flotte.NetzeinspeisungKwh` 894,9).

## Fragen aus der Welle

Den Anlass hat der Anwender am 26.09.2026 (~08:25) entschieden (E27‑Q3 b; E26‑Q6/N6 „in einer kleinen Welle
nachziehen“); die Fragen E29‑Q1 bis Q12 stellt der Phase‑0-Bericht, entschieden hat sie der Orchestrator am 26.09.2026
(09:35) mit der Baufreigabe, nach Empfehlung; gebaut ist jeweils der Entscheid (→ Register R‑E29).

| Frage | Gegenstand | Entscheid |
|---|---|---|
| **E29‑Q1** Definition | (a) KWK-Split, stündlich, gleich `KwkEinspeisungGesamtMWh`; (b) Stufenüberschuss Σ max(0, Strom − Stufeneingang) | a — `BhkwEinspeisungStuendlich` |
| **E29‑Q2** Reihenkonstante | (a) `BHKW_UEBERSCHUSS` wiederverwenden, ohne PV/Flotte füllen; (b) neue Konstante `BHKW_EINSPEISUNG` | a |
| **E29‑Q3** Flotte im Reiter | (a) Wert der Flottenbilanz; (b) immer der KWK-Split | a |
| **E29‑Q4** Zeile | (a) immer zeigen, nach „Stromproduktion“, mit Tooltip; (b) nur bei > 0 | a |
| **E29‑Q5** Orte der Reihe | (a) Zeitreihensatz und Reiter; (b) zusätzlich Stromgang samt CSV; (c) zusätzlich Vektor-CSV (R21) | a; b als Folgepunkt |
| **E29‑Q6** Excel ohne Flotte | (a) Spalte „BHKW-Einspeisung“, Messlatte 1030 begründet neu; (b) keine Spalte | a |
| **E29‑Q7** Strombilanz-Diagramm | (a) Linie `STROMBEDARF_GESAMT` mit Rückfall; (b) BHKW-Stapel teilen; (c) Beschriftung „Strombedarf gesamt“ | a |
| **E29‑Q8** Excel „Strombedarf“ | (a) Quelle umstellen; (b) zusätzlich „davon Lastgang“ | a |
| **E29‑Q9** N6 | (a) Kältestrom der Stufenrechnung als vierter Summand; (b) auch Kältestrom mit eigenem Zähler | a |
| **E29‑Q10** PV-Deckung | (a) Nenner je Stunde bei 0 klemmen; (b) viertelstündlich; (c) lassen | a |
| **E29‑Q11** N9 | (a) Kesselstrom in Bild, Summenlinie und CSV; (b) dazu Reihe „Kältestrom“; (c) eigene Welle | a |
| **E29‑Q12** N10/N11 | nur benennen | so — N10 Anwenderentscheid 26.09.2026 (~09:50): korrigieren in E30 (#545); N11 offen |

## Gebaut

- **E29/1 — BHKW-Einspeisung** (`71d74522`; Q1 a, Q2 a, Q3 a, Q4 a, Q5 a): `SimulationControl.BhkwEinspeisungStuendlich`
  (`:4492`, die Stundenformel des KWK-Splits wörtlich) und `BhkwEinspeisungDesLaufs` (`:4519`); `ZeitreihenExtraktor.cs:108-121`
  füllt `BHKW_UEBERSCHUSS` auch ohne PV und ohne Flotte (Schwelle 0,5 kWh; PV- und Flottenzweig unverändert), Doc der
  Konstante `BerichtsDaten.cs:470`; `BhkwErgebnis.EinspeisungMwh` (`SimulationErgebnisCtrl.cs:741/781/805`, mit Flotte
  `BhkwNetzeinspeisungKwh`); Zeile `BhkwReiter.razor:94` nach „Stromproduktion“, `Wertzeile(…, hinweis)` → `title` am
  `dt` (`:318`, ohne Hinweis kein Attribut); Ressourcen und Designer (`py Werkzeuge/ResourceDesigner/designer_neu.py schreiben`).
- **E29/2 — Excel-Spalte** (`656718a1`; Q6 a): Monatsblock ohne Flotte mit „BHKW-Einspeisung“
  (`ExcelBerichtGenerator.cs:1855-1858`); Messlatte `EPOS.Kern.Tests/Messlatten/Bericht_Excel_1030.txt` neu (unten).
- **E29/3 — Gesamtbedarf** (`39e540c3`; Q7 a, Q8 a; E26‑Q6): `ChartRenderer.StrombilanzMonateModell` Linie
  `STROMBEDARF_GESAMT ?? STROMBEDARF` (`:457-463`), Excel „Strombedarf“ dieselbe Quelle (`ExcelBerichtGenerator.cs:1831-1837`);
  Beschriftung und Stapel unverändert.
- **E29/4 — N6** (`30572adb`; Q9 a): `UebersichtKennzahlen.KaeltestromStufeMwh` (Σ `Kaeltestrom_Stufenrechnung_stuendlich`/1000)
  als vierter Summand am Ende (`SimulationErgebnisCtrl.cs:211-216`); `UebersichtReiter.razor:405/630` Satz mit Kälte und
  „Kältestrom x“ hinter dem Kessel.
- **E29/5 — PV-Deckung und N9** (`83418db8`; Q10 a, Q11 a): Nenner des PV-Deckungsgrads je Stunde geklemmt
  (`SimulationRunner.cs:1037-1043`, `SimulationErgebnisCtrl.cs:909`); `SimulationErgebnisHuelle.StromverbrauchKessel`
  (`Bilder.cs:919`) für Stapel „Heizkessel“ (`:943`), Summenlinie (`:964`) und CSV (`Wege.cs:320`).
- **E29/6 — Tests** (`80b29c7f`, Nachschliff `ed838081` UTF-8-BOM): neu `EPOS.Kern.Tests/BhkwEinspeisungAusweisTests.cs`
  (21 Fälle, `[Collection("Testdatenbank")]`, `Kulturvorrichtung`):
  - Theorie der Stundenformel (10/4/0 → 6, 10/4/3 → 9, 10/15/0 → 0, 0/5/0 → 0, 10/4/6 → 10); kurzer Bedarfsvektor =
    Eigenstrom; 8.760 Zufallsstunden Σ = `StromMatrix.KwkEinspeisungGesamtMWh` (1e‑9);
  - Reiter = Reihe = KWK-Split: 1018 27,4575, 1030 0,392, 1017/1024/1047 0 (Reihe fehlt);
  - 1018 + PV von 1040: Stundenformel = `SimulationPV.BhkwUeberschuss` (< 1e‑9), Reiter 27,4575, PV-Deckung ∈ [0; 100];
    1040 PV-Deckung bitgleich zur alten Formel; Klemme des Nenners;
  - Strombilanz-Linie = Gesamtbedarf (PNG-Gleichheit, Rückfall ungleich); Excel-Monatsblock (Kopf, Januar 2,232 / 0,372
    MWh, ohne Gesamtreihe 0,744);
  - N9: 1017 20,12 statt 635,2, 1030 0 statt 4.790,09;
  - N6 an 1045 mit Kühlung: Nenner = vier Glieder + Kältestrom = Σ `Strombedarf_Verbraucher`/4000; mit eigenem Zähler
    Kältestrom 0; ohne Kälte (1040) bitgleich.

  Dazu bUnit `ErzeugerReiterTests` (Stromliste „Strombedarf:, Stromproduktion:, Stromeinspeisung:, Strombedarfsdeckung:,
  Reststrombedarf:“; neu `Bhkw_zeigt_die_Stromeinspeisung_mit_Formel_im_Tooltip`) und `UebersichtReiterTests` (neu
  `Mit_Kaelte_nennt_die_Eigenverbrauchszeile_den_Kaeltestrom`); `ExcelBerichtGenerator.MonatsBlock` internal.

**Texte:**

| Schlüssel | de | en |
|---|---|---|
| `SIMERG_LBL_BHKW_EINSPEISUNG` | Stromeinspeisung: | Electricity feed-in: |
| `SIMERG_TIP_BHKW_EINSPEISUNG` | Je Stunde der BHKW-Strom, den die Verbraucher des Anschlusses nach der PV-Eigennutzung nicht abnehmen: Σ max(0, BHKW − max(0, Strombedarf aller Verbraucher − PV-Eigenverbrauch)). Dieselbe Menge wie die KWK-Einspeisung der Wirtschaftlichkeit; mit Speicherflotte die BHKW-Einspeisung der Flottenbilanz. | Per hour, the CHP electricity not taken by the site's consumers after PV self-consumption: Σ max(0, CHP − max(0, demand of all consumers − PV self-consumption)). Same quantity as the CHP feed-in of the economic analysis; with a storage fleet, the CHP feed-in of the fleet balance. |
| `SIMUEB_EIGENVERBRAUCH_MIT_KAELTE` | davon Eigenverbrauch der Wärme- und Kälteerzeuger: {0} MWh/a | of which own consumption of the heating and cooling generators: {0} MWh/a |
| (vorhanden) `SIMUEB_LBL_KAELTESTROM` | Kältestrom | Cooling electricity |
| Excel-Kopf (Bericht, deutsch fest) | BHKW-Einspeisung | — |

**Messlatte `Bericht_Excel_1030.txt`** (begründet neu eingefroren, E29‑Q6 a):

- alt: `## Blatt 4 „Stamm“ · 25 Z. × 7 Sp. · …` und
  `Z13: A=Monat | B=Wärmebedarf | C=BHKW-Wärme | D=Spitzenkessel | E=Strombedarf | F=BHKW-Strom | G=Netzbezug`
- neu: `## Blatt 4 „Stamm“ · 25 Z. × 8 Sp. · …` und
  `Z13: A=Monat | B=Wärmebedarf | C=BHKW-Wärme | D=Spitzenkessel | E=Strombedarf | F=BHKW-Strom | G=BHKW-Einspeisung | H=Netzbezug`
- Grund: 1030 hat 0,392 MWh BHKW-Einspeisung in 12 Stunden; die Reihe steht jetzt auch ohne PV/Flotte, der Monatsblock
  führt sie als Spalte. Sonst keine Zeile anders (2 Zeilen im Diff); Word-Messlatten und Excel „Gruppe“ unverändert.

## Nachweise

- **Build:** Release `WP-Plan.Kern.slnf` 0 Fehler.
- **ChartProben:** 174 Bilder, 0 Verstöße; 161 Hashes, `diff` gegen die Messlatte leer.
- **Voller Lauf** `WP-Plan.Kern.slnf` mit den xUnit-Schaltern (09:54–10:02): UI 6.533, KiKern 549, SpeicherEngine 386,
  SpeicherPlanung 27 (+1 übersprungen), Kern 8.120/8.122 mit 1 rot (BOM der neuen Datei, `QuelltextKodierungWacheTests`)
  → Nachschliff `ed838081`; Nachlauf Kodierungswache, neue Klasse und Dokumentationswachen 58/58 grün → Kern 8.121 grün,
  1 übersprungen, 0 rot.
- **Referenzlauf** 15 Projekte (`ed838081`), `vergleich` gegen R20: 14/14 PASS, 432/432 CSV byte-gleich; GESAMT-FAIL
  allein durch 1048 „nur im Vergleichslauf“ (keine R20-Basis); 1048 32/32 CSV byte-gleich zum Lauf auf `5826f97d`
  (temporärer Worktree, danach entfernt).
- **Wirkung:** keine R20-CSV, kein Kapitalwert, kein Schemaschritt, kein ChartProben-Hash; Testdatenbank unverändert
  (`3ac19fa9`); Testhost-Regel eingehalten (jeder Lauf über das Warteskript).
- **Merge** `32d84023` auf `pm26` über `4121813c`.
- **Gate:** Gate #536 auf `32d84023` (26.09.2026 10:07–10:26, Kern-Filter Release 0 Fehler, ChartProben 161 Bild-Hashes gleich mit der Messlatte, Tests mit Schaltern: KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (+1 übersprungen), EPOS.UI.Tests 6.547, EPOS.Kern.Tests 8.136 (+1 übersprungen); Dokumentationswachen 31/31 auf dem Papierstand `caf440f0`)
- **CI:** Push `50fa9e5a`: alle drei Läufe grün — Kern `main` 36232272209 (18:39 min), Windows `main` 36232272213 (36:46 min), Kern `ios_migration_september` 36232268489 (20:00 min)

## Abweichungen und Befunde

1. **BOM der neuen Testklasse:** Die Kodierungswache war im Voll-Lauf rot; Nachschliff `ed838081`, danach grün.
2. **Aufwand:** ~1 h Bau und Tests, ~45 min Läufe (Schätzung der Phase 0: ~6 h + Gate).

## Abnahme am Gerät (A‑E29‑1, Windows, Testdatenbank)

1. 1018 rechnen: Der BHKW-Reiter zeigt „Stromeinspeisung: 27,46 MWh/a“ nach „Stromproduktion“, der Tooltip die Formel;
   1030: 0,39; 1017: 0,00.
2. 1017 Stromgang: „Heizkessel“ ≈ 20 MWh/a, die Summenlinie ohne den Stufeneingang; die CSV-Spalte Heizkessel ebenso.
3. Bericht 1030 (Word und Excel): Excel-Monatsblock mit Spalte „BHKW-Einspeisung“; 1040: Strombilanz-Linie und Excel
   „Strombedarf“ ≈ 27,4 statt 8,0 MWh/a.
4. 1045 mit Kühlung (wie E34): Übersicht-Stromspalte „davon Eigenverbrauch der Wärme- und Kälteerzeuger: … · Kältestrom x“,
   Deckung plausibel.
5. Englische Oberfläche: „Electricity feed-in:“.
6. Der Kern-Lauf ist grün gegen R20.

## Konzeptvermerk (E29‑Q1…Q11)

In Konzept § 3.6, Absatz „BHKW-Einspeisung und Gesamtbedarf im Ausweis“, eingearbeitet: Die BHKW-Einspeisung ist eine
Ausweisgröße gleich dem KWK-Split (Zeile im BHKW-Reiter, Diagnosereihe, Excel), und Strombilanz-Diagramm, Excel-Spalte
„Strombedarf“ und Übersicht messen den Strombedarf aller Verbraucher; § 6.3 Nr. 34 (Q6, N6) und Nr. 36 (Q3 b) erledigt,
N10 und N11 unter Nr. 36 benannt.

## Logbuch

Logbuchsatz im [Wiki-Upload-Papier](../../../aktuell/Wiki_Update_2026-09-26.md) unter Version 1.2.0.4: „Der BHKW-Reiter
weist die Stromeinspeisung des BHKW aus; Strombilanz und Excel-Monatswerte messen den Strombedarf aller Verbraucher; der
Stromgang zeigt den Stromverbrauch des Heizkessels.“ Kein Wiki-Fachtext geändert.

## Papiere mit der Statuszeile

Statusdatei (#536, Nach #536); dieses Protokoll und der Eintrag im Dokumentations-Index (Reporting 143 → 144); Register
(Kopf, Familientafel, neue Familie R‑E29, R‑E26 Q6 „gebaut in E29 (#536)“, R‑E27 Q3 „b gebaut in E29 (#536)“, R‑E28 Q3
Restpunkt erledigt); Konzept (§ 3.6, § 6.1 Zeile E29, § 6.3 Nr. 34 und Nr. 36); Analysepapier § 5 Zeile E29;
Wiki-Upload-Papier (Logbuchsatz). Im selben Papierschritt, aber nicht Teil von E29: die CI-Vermerke #534 und #535
(Statusdatei, Protokoll E28). Kein Mockup (kein Dialog), kein Wiki-Fachtext.

## Offen

- **N10** — Übersicht-Stromring (`SimulationErgebnisHuelle.Bilder.cs:336`), Stromtabelle (`Anzeige.cs:381`) und
  Word-Deckungstorte (`BausteineVergleich.cs:223-226`) zählen beim BHKW die ganze Produktion samt Einspeisung als Deckung;
  `BHKW.Strombedarfsdeckung` (`SimulationRunner.cs:624`, persistiert, `aggregate.csv`) teilt durch den Projekt-Strombedarf
  statt durch den Gesamtbedarf (R20: 1017 5,48 %, 1024 26,22 %, 1030 9,02 %, 1047 5,34 %). Anwenderentscheid 26.09.2026:
  nach Empfehlung korrigieren — **wird in E30 (#545) korrigiert**, mit der Neueinfrierung R21.
- **N11** — Das Strombilanz-Diagramm stapelt die „Netzeinspeisung gesamt“ der Flotte in der Deckung
  (`ChartRenderer.cs:473-475`), der Nebenbalken gilt nur für den Namen „Einspeisung“ (`:773`); 1046: 0,895 MWh/a —
  **offen**, Anwenderentscheid.
- Folgepunkte, nicht beauftragt: eine wählbare Reihe „BHKW-Einspeisung“ im Stromgang samt CSV (E29‑Q5 b) und eine Reihe
  „Kältestrom“ im Stromgang (E29‑Q11 b).
- Die **Abnahme am Gerät** A‑E29‑1.
- **Gate** und **CI** (Nachweis oben).
