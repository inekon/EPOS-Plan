# E26 — PV-Ausweis: Stromproduktion der Module, Strommatrix-Bedarf aller Verbraucher, Referenzbasis R18 (Protokoll, 25.09.2026)

Statuszeile #518 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Anwender am
25.09.2026, „Befunde aus E25: Empfehlung/bearbeiten“, zu den Kern-Befunden N1 und N3, die E25 am Prüfprojekt 1048
gefunden hat; Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 3.6 (Vermiedene Stromkosten — Ausweis, kein Zahlungsstrom) und § 6.3 Nr. 32 und Nr. 34;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E26 (neu); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; die Basis in [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md), Abschnitt „Aktuelle Basis“,
und im [Archiv der Referenzbasen](../../Referenzbasen/LIESMICH.md). Vorgänger:
[`E24_Datenpflege_1018_1023_R17_Protokoll.md`](E24_Datenpflege_1018_1023_R17_Protokoll.md). Zweig `e26` von
`868afc57`; Opus 5.5 im Worktree `.claude/worktrees/e26`: `bc1d8ad8` (E26/1), `4fd6eaa6` (E26/2), `3136a267`
(E26/3), `4653fa06` (E26/4). Merge `025a8707` („Merge e26: PV-Ausweis Stromproduktion (N1) und Strommatrix-Bedarf
(N3), Referenzbasis R18 (#518)“) auf `pm26` über `b8f8a168` (#514 und #515 samt Papieren). **Kein Schemaschritt** —
Schemastand 143 bleibt, die Testdatenbank ist unverändert (`0c2fe21a…`). Die Simulation bewegt allein den Skalar
`Photovoltaik.Stromproduktion` in vier `aggregate.csv`, deshalb die neue Basis `2026-09-25_R18_PvAusweis`.

## Befund vor der Welle (Phase 0, Proben auf einer Kopie der Testdatenbank)

1. **N1 — die Stromproduktion war der Direktverbrauch.** `SimulationPV.cs:444-452` füllt zwei Reihen:
   `Stromproduktion_Theoretisch` (Erzeugung AC nach Wechselrichter-Kennlinie und Clipping) und `Stromproduktion`
   (nur der Direktverbrauch; die Umdeutung „Geänderte Ausweissemantik“ aus AP2b, `SimulationPV.cs:22-28`).
   `SimulationRunner.cs:989` summierte die Direktverbrauchsreihe in `pvm.Stromproduktion`, obwohl das Modell
   „Gesamte Stromerzeugung der Module“ sagt (`ErgebnisModel.cs:559`) und die Modulzeilen die Erzeugung führen
   (`SimulationRunner.cs:1034`, `SimulationPV.cs:416`). Die Vermutung aus E25 (Stunde gegen Viertelstunde) trifft
   nicht zu — ein Schreibfehler an einer Stelle.
2. **Leser, die damit falsch rechneten:** `WirtschaftlichkeitCtrl.cs:6167` (`PvVermiedenerBezugAusweis`, Erzeugung
   − Einspeisung — zog den Überschuss ein zweites Mal ab), `:2576` (Eigenverbrauch beim Degradations-Mehrbezug,
   kapitalwertwirksam nur mit Dialog und Degradation; die Testdatenbank führt 0 Zeilen `Tab_ProjektPhotovoltaik`),
   `KennzahlenKatalog.cs:375-376/430-435`, `PhotovoltaikVerguetungHuelle.cs:83` → `PvKennzahlenRechner.cs:58/82`,
   `BausteineVergleich.cs:429`; gespeichert in `Tab_ErgebnisPhotovoltaik` (`ErgebnisCtrl.cs:637`); der Skalar
   `Photovoltaik.Stromproduktion` in `aggregate.csv`. `pv_produktion.csv` (aus der Reihe, `Ergebnisexport.cs:204`)
   bleibt der Direktverbrauch. **Richtig** lasen schon die Ergebnisansicht (`SimulationErgebnisCtrl.cs:847-859`),
   das Diagramm (`Huelle.Bilder.cs:665-673`) und `PV_GENUTZT` (`ZeitreihenExtraktor.cs:54`).
3. **N3 — der Bedarf der Strommatrix war nur der Projektbedarf.** `ZeitreihenExtraktor.cs:31-32` liefert als
   `STROMBEDARF` allein Haushalt/Lastgang; `StromMatrix.cs:169-212` nahm ihn als „Bedarf ohne Eigenerzeugung“.
   `NETZBEZUG` (`:109`) ist dagegen der Rest nach der Kaskade — mit Wärmepumpe und Heizstab
   (`SimulationControl.cs:835-840`), Elektrokessel (`:868/896` bei Brennstoff 13, `SimulationSPK.cs:413-426`) und
   dem Kältestrom der Stufenrechnung (`SimulationControl.Kaelte.cs:297-300`); der BHKW-Strom wird ohne Klemme
   abgezogen (`:880/919`); der Kältestrom mit eigenem Zähler gehört bewusst nicht dazu (E34); eine eigene
   Hilfsenergie-Reihe gibt es nicht. Konzept § 3.6 verlangt Bruttobedarf − Restbezug — #437 bekam die falsche Reihe.
4. **Kapitalwert:** er liest allein `r.Reststrom` (`WirtschaftlichkeitCtrl.cs:2465-2467`); Bezug, vermiedene Kosten,
   § 9b-Korrektur (`:6427-6439`) und Verteilschlüssel (`:6254/6275`) sind Ausweis. Ausnahme ist der KWK-Split
   (KWKG-Zuschlag, Stromsteuer `:4172-4173`, Einspeiseerlös `:2444`) — an den Referenzen unverändert, in fremden
   Projekten mit BHKW-Strom zwischen Haushalts- und Bruttobedarf kann er wandern (E26‑Q3).
5. **Wirkung auf R17:** N3 ändert nichts (`aggregate.csv` führt keine Wirtschaftlichkeitsgröße); N1 ändert einen
   Skalar je PV-Projekt → Neueinfrierung R18.

## Gebaut

- **E26/1 — N1** (`bc1d8ad8`, `EPOS.Kern/Allgemein/Simulation/SimulationRunner.cs`, +10/−1):
  `pvm.Stromproduktion = pvs.Stromproduktion_Theoretisch.Sum()/1000` (`SimulationRunner.cs:989-998`), gleich der
  Summe der Modulzeilen (in Phase 0 bitgleich geprüft). Die Leser (`WirtschaftlichkeitCtrl.cs:6167/:2576`,
  `KennzahlenKatalog.cs:376/:434`, `PhotovoltaikVerguetungHuelle.cs:83`, `BausteineVergleich.cs:429`) bleiben
  unverändert und rechnen damit richtig.
- **E26/2 — N3** (`4fd6eaa6`, fünf Dateien, +50/−2): `SimulationControl.Strombedarf_Verbraucher_viertelstuendlich`
  nach `Kaskade_Zweikanalig()` = Rest nach der Kaskade + BHKW-Strom (Projektbedarf, Wärmepumpe, Heizstab,
  Elektrokessel, Kältestrom der Stufenrechnung; nicht der Kältestrom mit eigenem Zähler, E34);
  `ZeitreihenSatz.STROMBEDARF_GESAMT` (`BerichtsDaten.cs`), `ZeitreihenExtraktor.cs:31ff`; `StromMatrix.Baue`
  nimmt `STROMBEDARF_GESAMT` (Rückfall `STROMBEDARF`, die synthetischen Tests bleiben) für Bedarf, PV-Eigenverbrauch,
  Lastbild und KWK-Split (E26‑Q3 a); `SzenarioMengen.ENERGIEREIHEN` ergänzt.
- **E26/3 — Tests** (`3136a267`): neu `EPOS.Kern.Tests/PvAusweisStromMatrixTests.cs` mit 11 Fällen — N1 an
  1040/1026/1042: Stromproduktion = Summe der Modulzeilen = genutzt + Überschuss, Ausweis-Eigenverbrauch ≥
  Direktverbrauch ≥ 0, Kennzahlen `pv_strom`/`pv_eigen`; N3: Bedarf = Summe der Verbraucherreihen = PV-Eingangsbedarf
  + BHKW stundenweise (< 1e‑6), Bedarf ≥ Netzbezug, PV-Eigenverbrauch = `PV_GENUTZT`, vermiedene Menge =
  PV-Eigenverbrauch + Entladung; Kapitalwert-Anker bitgleich.
- **E26/4 — die Basis** (`4653fa06`): Neueinfrierung R18, R17 ins Archiv, Basisname ersetzt (Abschnitt „Die neue
  Basis“).

## Zahlen vorher und nachher

Rollentarif 0,30 €/kWh; Mengen in MWh, Kosten in €/a.

| Projekt | Stromproduktion | Ausweis-Eigenverbrauch | Bedarf der Strommatrix | vermiedene Menge | vermiedene Kosten |
|---|---|---|---|---|---|
| 1040 | 4,441 → 6,713 | 2,168 → 4,441 | 8,000 → 27,427 | −14,986 → 4,441 | −4.496 → +1.332 |
| 1026 | 4,197 → 6,713 | 2,950 → 5,467 | 8,000 → 31,351 | −18,004 → 5,348 | −5.401 → +1.604 |
| 1042 (PV ohne Ertrag) | 0 | 0 | 8,000 → 41,345 | −33,345 → 0 | −10.003 → 0 |

Eigenverbrauchsquote des Ausweises (Phase 0): 1040 48,8 → 66,1 %, 1026 70,3 → 81,4 %. Weitere Proben der
Stromproduktion (Phase 0): 1007 5,081 → 6,014, 1045 2,744 → 3,545, 1046 5,081 → 6,014; 1048 geschätzt 6,37 → 13,43,
„PV: vermiedener Bezug“ −0,69 → 6,37. Vermiedene Menge im Rollentarif (Phase 0): 1007 −38,8 → 5,6, 1045
−20,8 → 2,7, 1046 −39,9 → 4,5, 1023 −137,5 → 0, 1039 −60,9 → 0; BHKW-Projekte 1017 16,1 → 36,8, 1024 3,7 → 95,7,
1047 30,8 → 35,9, 1030 und 1018 gleich.

**Kapitalwert unverändert:** Reststrom, KWK-Split (`KwkEigen`/`KwkEinsp` an 1017, 1024, 1047, 1030, 1018) und
Kapitalwert überall gleich. Anker bitgleich: 1024 Erwartet −2.772.642,2674731, Best −2.801.567,7561814; 1030
Erwartet −31.141.242,7086938; KWKG Jahr 1 7.322,63.

## Die neue Basis

`Referenzlaeufe/2026-09-25_R18_PvAusweis`, **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039,
1040, 1041, 1042, 1045, 1046, 1047), 432 CSV und `protokoll.txt`, 2.447 Skalare; gerechnet auf der unveränderten
Testdatenbank (Schemastand 143, `0c2fe21a…`).

**A/B gegen R17** (vierzehn Projekte): **10/14 PASS und byte-gleich**, 428/432 CSV byte-gleich; 1007, 1040, 1045
und 1046 FAIL mit je einem Wert in `aggregate.csv`, alle Zeitreihen byte-gleich:

| Projekt, `aggregate.csv` | R17 | R18 |
|---|---|---|
| 1007 `Photovoltaik.Stromproduktion` | 5,08 | 6,01 |
| 1040 `Photovoltaik.Stromproduktion` | 4,44 | 6,71 |
| 1045 `Photovoltaik.Stromproduktion` | 2,74 | 3,55 |
| 1046 `Photovoltaik.Stromproduktion` | 5,08 | 6,01 |

1041 und 1042 führen eine Photovoltaik ohne Ertrag (0 → 0). Der Strommatrix-Bedarf (N3) wirkt nicht auf die Basis:
`aggregate.csv` führt keine Wirtschaftlichkeitsgröße, `STROMBEDARF_GESAMT` entsteht nur im Zeitreihensatz des
Berichts.

**Determinismus:** zwei Läufe desselben Standes 14/14 byte-gleich, untereinander GESAMT: PASS (4.610.207 Werte);
die `-wal`/`-shm`-Dateien aus Phase 0 sind gelöscht und der Einfrierlauf ist wiederholt (Testdatenbank unverändert
`0c2fe21a`). Einfrierbefehl: `dotnet run --project EPOS.Referenzlauf -c Release -- lauf --quelle
Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047
--ziel Referenzlaeufe/2026-09-25_R18_PvAusweis`.

**Archiv.** Das `protokoll.txt` von R17 ist per `git mv` nach
`Dokumentation/ueberholt/Referenzbasen/2026-09-25_R17_Datenpflege/` gewandert, der Rest per `git rm` entfernt. Das
Archiv führt 35 Basen und 36 Dateien, die Tabellenzeile R17 („abgelöst durch R18 am 25.09.2026“, Verweis Z. 19) und
den Abschnitt „Die Basis R17 im Einzelnen“ mit dem Grund der Ablösung. `Referenzlaeufe/LIESMICH.md`: „Aktuelle
Basis“ R18 (Anlass N1, A/B-Tafel, Wirkung von N3, Determinismus, Einfrierbefehl, Vorgängernotiz R17), „Entfernte
Basen“ mit 35 Basen und elf gesicherten Protokollen, Weg A.

**Fundstellen des Basisnamens** `2026-09-25_R17_Datenpflege` → `2026-09-25_R18_PvAusweis`: `CLAUDE.md` (Z. 146),
`kern.yml` (Z. 264, 268–273, 7 Stellen), `ios.yml` (Z. 224, 228), `Dokumentation/LIESMICH.md` (Z. 251), Konzept
Gebäudesimulation (Z. 1931), Systementwurf Gebäudesimulation (Z. 179), Konzept Wirtschaftlichkeit (Z. 3, Tafel der
Regressionsanker, § 6.3 Nr. 21), Analysepapier (Z. 8, Legende von § 5 „seit E26 gilt … R18“), Basenhistorie (Z. 7).
Geschichte bleibt unverändert: das Archiv (Abschnitt R16/R17), der Kommentar in
`DatenpflegeKesseltraegerTests.cs:22`, Statuszeile #514, Protokoll E24, Register, Konzept § 6.3 Nr. 24, Analysepapier
(Nachtrag und § 5 Zeile E24) und das Protokoll der Entscheidwege.

## Fragen aus der Welle

Die Fragen stellt der Phase‑0-Bericht; entschieden hat sie der Orchestrator am 25.09.2026 (~19:50) mit der
Baufreigabe, nach Empfehlung, nachdem der Anwender die Befunde mit „Empfehlung/bearbeiten“ freigegeben hatte; gebaut
ist jeweils der Entscheid (→ Register R‑E26).

| Frage | Gegenstand | Entscheid |
|---|---|---|
| **E26‑Q1** Weg für N1 | (a) N1-A: `Stromproduktion` im Kern = Erzeugung der Module, neue Basis R18; (b) N1-B: die Leser umstellen, R17 bleibt | a |
| **E26‑Q2** Einfrierung | E26 friert R18 selbst ein | a — ja |
| **E26‑Q3** KWK-Split | auch der KWK-Split misst sich am Bruttobedarf (`STROMBEDARF_GESAMT`) | a — ja; Konzeptvermerk § 3.6; an den Referenzen unverändert, in fremden Projekten mit BHKW-Strom zwischen Haushalts- und Bruttobedarf kann der Kapitalwert wandern (dem Anwender gemeldet) |
| **E26‑Q4** Eigenverbrauch des Ausweises | Eigenverbrauch = Erzeugung − Einspeisung, einschließlich Speicherladung; die Abregelung der Flotte 1046 messen | a — gemessen: Abregelung 0,000 MWh |
| **E26‑Q5** Schemaschritt | kein Schemaschritt; alte Zeilen in `Tab_ErgebnisPhotovoltaik` heilen beim nächsten Lauf | a |
| **E26‑Q6** Strombilanz-Diagramm und Excel-Spalte „Strombedarf“ | mit E26 umstellen oder später | b — später (Restpunkt) |
| **E26‑Q7** N5 und N6 | nur melden | melden (Restpunkte) |

## Abweichungen und Befunde

1. **1046:** Abregelung 0,000 MWh; Eigenverbrauch = Erzeugung − Einspeisung 5,119 MWh (direkt genutzt 5,081); die
   vermiedene Menge 4,523 MWh liegt unter PV + Entladung (1,057), weil die Flotte auch aus dem Netz lädt — kein
   Fehler.
2. **Die PV-Projekte der Testdatenbank führen keinen Strompreis** — der Kapitalwert-Nachweis hängt deshalb an den
   Ankern von 1024 und 1030; 1048 kommt mit E25.
3. **Testhost-Regel einmal verletzt:** ein gefilterter Lauf von 4 s bei fremdem testhost, grün.

## Nachweis

- **Zahlen:** Abschnitt „Zahlen vorher und nachher“.
- **Referenzlauf:** A/B gegen R17 und Determinismus im Abschnitt „Die neue Basis“; Referenzlauf 14/14 gegen R18.
- **Tests** (Worktree `e26` nach E26/4, mit den xUnit-Schaltern): 14.632 bestanden / 2 übersprungen / 0 rot
  (EPOS.Kern 7.292/7.293, EPOS.UI 6.378, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27/28); ChartProben 174
  Bilder, 0 Verstöße; SqlDialektPruefer 1.920/0.
- **Merge** `025a8707` auf `pm26` über `b8f8a168`.
- **Gate:** Gate #518 auf `025a8707` (25.09.2026 20:01–20:19): Kern-Filter 0 Fehler, ChartProben 0 Fehler, 161 Bild-Hashes gleich mit der Messlatte, Tests Kern 7.292 (1 übersprungen), UI 6.378, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), Dokumentationswachen 29/29.
- **CI:** steht aus (Beobachtung nach dem Push) — die CI muss gegen R18 laufen (`kern.yml` und `ios.yml` mit dem Basispfad R18).

## Abnahme am Gerät (A‑E26‑1, Windows)

Ein Wärmepumpen-Projekt mit Photovoltaik rechnen (1040 mit gepflegtem Strompreis oder 1048 nach E25):

1. Ergebnisansicht und Bericht zeigen als PV-Stromerzeugung die Summe der Modulzeilen.
2. „PV: vermiedener Bezug“ ist ≥ 0 (1048 rund 6,37 MWh).
3. Im Rollentarif sind vermiedene Menge und vermiedene Kosten positiv (1040 4,44 MWh).
4. Der Kapitalwert ist unverändert.
5. Der Kern-Lauf ist grün gegen R18.

Belegt in `PvAusweisStromMatrixTests` und in `aggregate.csv` von R18; die Sichtprüfung liegt beim Anwender.

## Konzeptvermerk (E26‑Q3)

In Konzept § 3.6 eingearbeitet: „Bedarf ohne jede Eigenerzeugung ist der Strombedarf aller Verbraucher des
Anschlusses (Projektbedarf, Wärmepumpe, Heizstab, Elektrokessel, Kältestrom der Stufenrechnung; Reihe
STROMBEDARF_GESAMT); auch der KWK-Split (Eigenstrom = min(BHKW-Strom, Bedarf nach PV-Eigennutzung)) misst sich daran,
weil die Simulation das BHKW den Strom der Wärmepumpe decken lässt (E26, Entscheid E26‑Q3).“

## Logbuch

Vorschlag (Version beim Anwender erfragen, mit dem nächsten Sammel-Upload): „Die Photovoltaik weist als
Stromproduktion die gesamte Erzeugung der Module aus; vermiedener Netzbezug und vermiedene Stromkosten beziehen den
Strom von Wärmepumpe, Heizstab und Elektrokessel ein.“ Kein Wiki-Fachtext geändert.

## Papiere mit der Statuszeile

Mit dem Merge (E26/4): der Basisname und die Referenzbasen-Papiere (Abschnitt „Die neue Basis“). Mit dieser
Statuszeile: Konzept (Kopf mit Codestand `025a8707`; Schrittabsatz „E26 (#518) ohne Schritt“; § 2.6 Block B Zeile
B2; § 3.6 Konzeptvermerk und PV-Stromproduktion des Ausweises; § 6.1 Zeile E26; § 6.3 Nr. 32 Hinweis und neue
Nr. 34; § 7; Anhang), Register (Kopf, Familientafel, neue Familie R‑E26), Analysepapier (Kopf, Nachtrag, § 5 Zeile
E26), Protokoll der Entscheidwege (Kopf von § 8, § 8.50, § 8.51), Dokumentations-Index (Reporting 139 → 140,
Referenzbasen 35 Basen und 36 Dateien), Statusdatei (#518, Nach #518). Das Archiv der Referenzbasen ist geprüft:
Tabellenzeile und Abschnitt R17 nennen Datum und Grund der Ablösung. Kein Mockup (kein Dialog), kein Wiki-Fachtext.

## Offen

- **Q6:** Das Strombilanz-Diagramm (`ChartRenderer.cs:457-471`) und die Excel-Spalte „Strombedarf“
  (`ExcelBerichtGenerator.cs:1834`) zeigen weiter den Projektbedarf (E26‑Q6 b).
- **N5:** 1018 hat einen negativen Netzbezug von −27,46 MWh — der Rest nach der Kaskade wird ohne Klemme abgezogen
  (`SimulationControl.cs` `SubVectors(…, false)` `:880/:919`, `ReststromMwh` `:612-614`); im Rollentarif ist der
  Reststrom −8.237 €, **kapitalwertwirksam**. Empfehlung: eigene Welle E27.
- **N6:** Die Übersicht „Strombedarf mit Eigenverbrauch“ (`SimulationErgebnisCtrl.cs:198-201`,
  `StrombedarfMitEigenverbrauchMwh`) führt den Kältestrom nicht.
- Die **Abnahme am Gerät** A‑E26‑1; an 1048 prüfbar, sobald E25 das Prüfprojekt nach dem Push angelegt hat.
- **Gate** und **CI** (Nachweis oben).
