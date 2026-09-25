# E27 — BHKW-Überschuss: der Netzbezug ist nie negativ, Referenzbasis R19 (Protokoll, 25.09.2026)

Statuszeile #521 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Befund N5 aus E26
(Statusdatei, Nach #518 (g); Register R‑E26, E26‑Q7) und der Anwenderentscheid vom 25.09.2026, „E27: nach Empfehlung
bauen“ (E27‑Q1 a, E27‑Q2 a); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 3.6 (Strommatrix, Vermiedene Stromkosten) und § 6.3 Nr. 34 und Nr. 36;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E27 (neu) und R‑E26 (E26‑Q7); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; die Basis in [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md), Abschnitt „Aktuelle Basis“,
und im [Archiv der Referenzbasen](../../Referenzbasen/LIESMICH.md). Vorgänger:
[`E26_PvAusweis_Strommatrix_R18_Protokoll.md`](E26_PvAusweis_Strommatrix_R18_Protokoll.md). Zweig `e27` von
`ba798d8a`; Opus 5.5 im Worktree `.claude/worktrees/e27`: `bba74bca` (E27/1), `8aa53a99` (E27/1, Nachschliff),
`13ae6216` (E27/3), `a96500eb` (E27/4). Merge `80a7b9fb` („Merge e27: BHKW-Ueberschuss, Netzbezug nie negativ (N5),
Referenzbasis R19 (#520)“) auf `pm26` über `origin` = `50ecd802` (#519 samt Papieren). **Kein Schemaschritt** —
Schemastand 144 bleibt; die Testdatenbank hat E27 nicht geändert: `e27` fror R19 auf der Fassung `19a7b632…` ein, der
Merge bringt die Fassung `b68638da…` mit dem Prüfprojekt 1048 (#519, ohne Referenzrolle). Die Simulation bewegt
`aggregate.csv` und `reststrom_viertelstunde.csv` von 1018 und 1030, deshalb die neue Basis
`2026-09-25_R19_BhkwNetzbezug`.

## Befund vor der Welle (Phase 0, Worktree `e27` = `ba798d8a`, Vorher-Lauf auf einer Kopie)

Die Zeilennummern sind die vor E27.

1. **Einzige Quelle des negativen Rests** ist der ungeklemmte BHKW-Abzug `SimulationControl.cs:903-904`
   (Stundenschleife) und `:942-943` (Vektorstufe). Alle anderen Stufen addieren: Wärmepumpe und Heizstab
   (`:861/863`), Kessel (`:891/919`), Kälte (`SimulationControl.Kaelte.cs:300`).
2. **Wer den negativen Rest braucht:** spätere Verbraucher derselben Viertelstunde (Wärmepumpe, Heizstab,
   Elektrokessel, Kälte hinter dem BHKW) — physikalisch richtig, deshalb wäre eine Klemme an der BHKW-Stufe falsch;
   die Photovoltaik (`:578-581`, `BhkwUeberschuss` V1 `:583-593`, danach die korrigierten `SubVectors` ≥ 0); der
   Einzelspeicher (`:618-624`, klemmt; seine Last aus `StromspeicherSimCtrl.cs:526/1198`); die Speicherflotte
   (`Stromspeicher.cs:121-135`, ersetzt den Rest durch `NetzbezugKw` ≥ 0).
3. **Negativ bleibt der Rest genau** bei einem BHKW ohne Photovoltaik und ohne gerechneten Speicher (`Tool_6` ohne
   Anlage = null, klemmt nicht): 1018 und 1030; außerhalb der Referenzliste 1031 (gespeichert −14,72).
4. **Kein eigener Überschussvektor nötig:** der KWK-Split (`StromMatrix.cs:227-238`) liefert die Einspeisung
   stundengenau — 1018 `KwkEinsp` 27,4575 MWh = Summe der negativen Viertelstunden, 1030 0,392 MWh.
5. **Wo ein negativer Netzbezug sichtbar war:** Übersicht, Legende (`SimulationErgebnisHuelle.Bilder.cs:343`,
   `Anzeige.cs:398`); BHKW-Reiter, Reststrombedarf (`BhkwReiter.razor:93` ← `SimulationErgebnisCtrl.cs:743`);
   Kennzahl `energie.netzbezug` und Autarkie (`KennzahlenKatalog.cs:380-381/455`); Energiekosten und CO₂
   (`KostenEmissionRechner.cs:536`); `EndenergieAufloeser.cs:956`; Monatsdiagramm (`ChartRenderer.cs:465-466`); Excel,
   Monatsspalte (`:1853`); Strommatrix-Tabelle (`BausteineWirtschaftlichkeit.cs:964-967`, Excel `:1002-1003`,
   gespeichert `WirtschaftlichkeitCtrl.cs:8482`). Eine Größe „BHKW-Einspeisung“ gibt es nur in der
   Wirtschaftlichkeit (`KwkEinspeisungGesamtMWh`); die Ergebnisansicht führt Einspeisung nur mit Photovoltaik oder
   Flotte (`ZeitreihenExtraktor.cs:84-86/103`).
6. **Wirkung (gerechnet aus den R18-Reihen):** 12/14 Projekte unverändert (die Anker von 1024 bleiben); 1018 und 1030
   wandern (Zahlen unten); die Kapitalwert-Änderung an 1030 liegt relativ bei 5,6e‑5 — unter der Toleranz des
   Referenzlaufs, die Anker mit sechs Nachkommastellen aber rot. 1017 und 1047 führen keinen Kapitalwert (Brennstoff
   ohne Träger). Als wandernd benannt: `PvAusweisStromMatrixTests.cs:239` (1030), `KwkgErsatzwegGewichtetTests`
   (~`:82`), `Co2StromtraegerRueckfallTests.cs:70`, `EnergiekostenGrundTests.cs:356`, `ProjektkostenArtenTests.cs:93`.
7. **Referenzbasis:** 4/432 CSV wandern; 1030 steht in der CI-Liste → Neueinfrierung R19 nötig.

## Gebaut

- **E27/1 — die Klemme** (`bba74bca`, drei Dateien, +54/−2): `SimulationControl.cs:633-641` ruft vor `ReststromMwh`
  die neue Methode `NetzbezugGeklemmt` (~`:4400`) — nur Werte < 0 → 0, in einem neuen Array (der Rest kann auf
  `simulation_Strombedarf` zeigen, `:521`), nicht bei der Speicherflotte (`!SpeicherflotteErsetztReststrom`); ohne
  negativen Wert bleibt es dasselbe Array, jeder Wert ≥ 0 bitgleich (E27‑Q1 a, Q5 a). Dazu `BhkwReststrombedarfMwh`:
  der Reststrombedarf der BHKW-Zeile je Stunde geklemmt, Σ max(0, Bedarf_h − Strom_h) (E27‑Q4 a; Aufrufe
  `SimulationRunner.cs:607-609`, `SimulationErgebnisCtrl.cs:743-745`).
- **E27/1 — Nachschliff** (`8aa53a99`, dieselben drei Dateien, +12/−5): ohne Überschussstunde rechnet
  `BhkwReststrombedarfMwh` die Jahresdifferenz wie vorher — 1017, 1024 und 1047 bleiben bitgleich.
- **E27/2 — Ausweis:** nichts nötig; alle Anzeigen lesen `ReststromMwh` bzw. `NETZBEZUG`.
- **E27/3 — Tests** (`13ae6216`, +228/−2): neu `EPOS.Kern.Tests/BhkwNetzbezugKlemmeTests.cs` mit 7 Fällen;
  `PvAusweisStromMatrixTests.cs:237-247` Kapitalwert-Anker 1030 Erwartet/Best/Worst neu gesetzt (Kommentar „E27“,
  E27‑Q2 a).
- **E27/4 — die Basis** (`a96500eb`): Neueinfrierung R19, R18 ins Archiv, Basisname ersetzt (Abschnitt „Die neue
  Basis“).

## Zahlen vorher und nachher

Mengen in MWh, Kosten in €/a, CO₂ in t/a, Kapitalwert in €.

| Größe | 1018 vorher | 1018 nachher | 1030 vorher | 1030 nachher |
|---|---|---|---|---|
| Netzbezug (Stromrestbedarf) | −27,4575 | 0 | 4.357,7808 | 4.358,1728 |
| `BHKW.Reststrombedarf` | −27,46 | 0 | 4.357,78 | 4.358,17 |
| negative Viertelstunden | 14.004 | 0 | 48 (12 h) | 0 |
| KWK-Split Eigen / Einspeisung | 0 / 27,4575 | gleich | 431,913 / 0,392 | gleich |
| Reststromkosten, Rollentarif 0,30 €/kWh | −8.237,25 | 0 | — | — |
| vermiedene Menge, Rollentarif | 27,46 | 0 | — | — |
| Energiekosten Flat | — | — | 1.624.616,20 | 1.624.713,70 (+97,50) |
| Stromkosten Netz | — | — | 1.091.845 | 1.091.942,50 |
| CO₂ gesamt | 13,0557 | 25,0008 | 4.035,0704 | 4.035,2888 |
| Kapitalwert Erwartet | — | — | −31.141.242,7087 | −31.142.971,0615 |
| Kapitalwert Best | — | — | −31.309.741,5799 | −31.311.485,3390 |
| Kapitalwert Worst | — | — | −31.005.297,5828 | −31.007.010,7983 |

An 1030 bleiben KWKG Jahr 1 (7.322,63) und die Bezugsspitze (2.011 kW) gleich. 1018 führt keinen Kapitalwert — die
Testdatenbank kennt für 1018 keinen Erdgas-Arbeitspreis. Der CO₂-Anstieg an 1018 ist 27,46 MWh × 435 g/kWh =
11,9451 t/a: die Gutschrift über den negativen Netzbezug (`KostenEmissionRechner.cs:735`) entfällt (E27‑Q8). Die
übrigen zwölf Projekte sind unverändert; die Anker von 1024: −2.772.642,27 / −2.801.567,76 / −2.745.356,04 €.

**Photovoltaik nach dem BHKW** (Kopie 1018 + PV von 1040): Rest- und Netzbezug-Hashes gleich, `BhkwUeberschuss`
27.457,51 kWh, allein `BHKW.Reststrombedarf` −27,46 → 0.

## Die neue Basis

`Referenzlaeufe/2026-09-25_R19_BhkwNetzbezug`, **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039,
1040, 1041, 1042, 1045, 1046, 1047), 432 CSV und `protokoll.txt`, 2.447 Skalare; gerechnet auf der Testdatenbank
`19a7b632…` (Schemastand 144, vor dem Prüfprojekt 1048).

**A/B gegen R18** (vierzehn Projekte): **12/14 PASS und byte-gleich**, 428/432 CSV byte-gleich; 1018 und 1030 FAIL mit
`aggregate.csv` und `reststrom_viertelstunde.csv`, alle übrigen Zeitreihen byte-gleich:

| Projekt, Datei, Größe | R18 | R19 |
|---|---|---|
| 1018 `aggregate.csv` `Sim.Reststrom` | −27,4575103 | 0 |
| 1018 `aggregate.csv` `Energiebedarf.Stromrestbedarf` | −27,46 | 0 |
| 1018 `aggregate.csv` `BHKW.Reststrombedarf` | −27,46 | 0 |
| 1018 `aggregate.csv` `Vektor.reststrom_viertelstunde.Summe` | −109.830,041 | 0 |
| 1018 `reststrom_viertelstunde.csv` | 14.004 Werte < 0 | 0 |
| 1030 `aggregate.csv` `Sim.Reststrom` | 4.357,78079 | 4.358,17279 |
| 1030 `aggregate.csv` `Energiebedarf.Stromrestbedarf` | 4.357,78 | 4.358,17 |
| 1030 `aggregate.csv` `BHKW.Reststrombedarf` | 4.357,78 | 4.358,17 |
| 1030 `aggregate.csv` `Vektor.reststrom_viertelstunde.Summe` | 17.431.123,2 | 17.432.691,2 |
| 1030 `reststrom_viertelstunde.csv` | 48 Werte < 0 (12 Stunden) | 0 |

Die Toleranz meldet 1018 mit 14.008 und 1030 mit 48 Abweichungen.

**Determinismus:** zwei Läufe desselben Standes 14/14 byte-gleich, untereinander GESAMT: PASS (4.610.207 Werte); der
Einfrierlauf ist mit beiden byte-gleich, der Kontrolllauf gegen R19 PASS. Einfrierbefehl: `dotnet run --project
EPOS.Referenzlauf -c Release -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte
1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 --ziel
Referenzlaeufe/2026-09-25_R19_BhkwNetzbezug`.

**Archiv.** Das `protokoll.txt` von R18 ist per `git mv` nach
`Dokumentation/ueberholt/Referenzbasen/2026-09-25_R18_PvAusweis/` gewandert, der Rest per `git rm` entfernt. Das
Archiv führt 36 Basen und 37 Dateien, den Verweis auf die aktuelle Basis (Z. 19), die Tabellenzeile R18 („abgelöst
durch R19 am 25.09.2026“) und den Abschnitt „Die Basis R18 im Einzelnen“ mit dem Grund der Ablösung.
`Referenzlaeufe/LIESMICH.md`: „Aktuelle Basis“ R19 (Anlass N5, A/B-Tafel, Determinismus, Einfrierbefehl), der
Nachtrag Schemastand 144 und die Zusammenführung mit E24 aus R18 übernommen, die Vorgängernotiz R18, „Entfernte Basen“
mit 36 Basen und zwölf gesicherten Protokollen, Weg A. **Beim Merge `80a7b9fb`** ist der Abschnitt mit dem Nachtrag
Prüfprojekt 1048 aus #519 zusammengeführt: der Kopfabsatz nennt die Fassung `b68638da…` und das Prüfprojekt „PV mit
Preisen“ ohne Nummer (die Wache `PvPreisProjektTests` prüft die ersten 800 Zeichen), der Nachtrag darunter verweist
auf den Lauf gegen R19 in #521.

**Fundstellen des Basisnamens** `2026-09-25_R18_PvAusweis` → `2026-09-25_R19_BhkwNetzbezug` (20 Stellen):
`CLAUDE.md` (Z. 146), `kern.yml` (Z. 264, 268–273, 7 Stellen), `ios.yml` (Z. 224, 228), `Dokumentation/LIESMICH.md`
(Z. 251), Konzept Gebäudesimulation (Z. 1936), Systementwurf Gebäudesimulation (Z. 179), Konzept Wirtschaftlichkeit
(Z. 3, Tafel der Regressionsanker, § 6.3 Nr. 21), Analysepapier (Z. 8, Legende von § 5 „seit E27 gilt … R19“),
Basenhistorie (Z. 7), Archiv der Referenzbasen (Z. 19). Geschichte bleibt unverändert: Statuszeilen #517 und #518,
Nach #518, die Protokolle E25, E26 und Katalogimport, Register E26‑Q2, Konzept § 6.1 Zeile E26, § 6.3 Nr. 34 und
Anhang, Analysepapier (Nachtrag und § 5 Zeile E26), das Protokoll der Entscheidwege, das Archiv (Abschnitt R17/R18) und
die Vorgängernotiz in `Referenzlaeufe/LIESMICH.md`; `git grep -n R18_PvAusweis` trifft nur diese Stellen.

## Fragen aus der Welle

E27‑Q1 und E27‑Q2 hat der Anwender am 25.09.2026 mit dem Auftrag entschieden („E27: nach Empfehlung bauen“); E27‑Q3
bis Q8 stellt der Phase‑0-Bericht, entschieden hat sie der Orchestrator am 25.09.2026 (~21:15) mit der Baufreigabe,
nach Empfehlung; gebaut ist jeweils der Entscheid (→ Register R‑E27).

| Frage | Gegenstand | Entscheid |
|---|---|---|
| **E27‑Q1** Klemme | Klemme bei null, wenn keine spätere Stufe den Überschuss braucht; die Einspeisung bleibt allein beim KWK-Split | a — gebaut am Laufende, nicht an der BHKW-Stufe (Befund 1) |
| **E27‑Q2** Anker | die Kapitalwert-Anker der BHKW-Referenzprojekte bei Änderung neu setzen | a — gewandert ist allein 1030 (`PvAusweisStromMatrixTests`) |
| **E27‑Q3** Ausweisgröße | (a) keine neue Größe; (b) Diagnosereihe und Zeile „BHKW-Einspeisung“ im BHKW-Reiter | a — b bleibt Restpunkt |
| **E27‑Q4** `BHKW.Reststrombedarf` | an `SimulationRunner.cs:607` und `SimulationErgebnisCtrl.cs:743` je Stunde klemmen, Σ max(0, Bedarf_h − Strom_h) | a |
| **E27‑Q5** Ort der Klemme | am Laufende, nicht bei der Speicherflotte | a |
| **E27‑Q6** Altergebnisse | gespeicherte Ergebnisse 1018/1031 bleiben und heilen beim nächsten Lauf | a — kein Schemaschritt |
| **E27‑Q7** N7 | Vorab-Überschuss des PV-Modus der Wärmepumpe und Kessel-Vektorstufe hinter dem BHKW | nur melden (Restpunkt) |
| **E27‑Q8** CO₂ | der CO₂-Anstieg an 1018 ist folgerichtig | messen — +11,9451 t/a |

## Abweichungen und Befunde

1. **Die Klemme sitzt nicht an der BHKW-Stufe:** spätere Verbraucher derselben Viertelstunde und die Photovoltaik
   brauchen den Überschuss; der Lauf klemmt am Ende, nach Photovoltaik und Speicher, vor `ReststromMwh`.
2. **Photovoltaik nach dem BHKW byte-gleich:** Probe 1018 + PV von 1040 — Rest 0, `BhkwUeberschuss` 27.457,5 kWh,
   Einspeisung PV 6,60 MWh; die Klemme ist dort wirkungslos.
3. **1018 ohne Kapitalwert in der Testdatenbank** (kein Erdgas-Arbeitspreis) — die Doppelgutschrift ist dort
   hypothetisch; der Kapitalwert-Nachweis hängt an 1030.
4. **Die Anker-Klassen lesen gespeicherte Ergebnisse:** `KwkgErsatzwegGewichtetTests`, `Co2StromtraegerRueckfallTests`,
   `EnergiekostenGrundTests` und `ProjektkostenArtenTests` bleiben grün; rot wurde allein der 1030-Anker in
   `PvAusweisStromMatrixTests` (neu gesetzt).
5. **Nachschliff `8aa53a99`** für bitgleiche 1017, 1024 und 1047 (ohne Überschussstunde die Jahresdifferenz wie
   vorher).
6. **Phase 0 ohne Kern-Eingriff:** der Klassifizierer lehnte einen temporären Eingriff in den Kern ab („Modify Shared
   Resources“); die Nachher-Werte der Phase 0 sind aus den R18-Reihen gerechnet, der Bau hat sie bestätigt. Im Bau
   lehnte er nichts ab; die Testhost-Regel ist eingehalten.

## Nachweis

- **Zahlen:** Abschnitt „Zahlen vorher und nachher“.
- **Referenzlauf:** A/B gegen R18 und Determinismus im Abschnitt „Die neue Basis“; Kontrolllauf gegen R19 PASS vor dem
  Merge (Testdatenbank `19a7b632…`). Nach dem Merge mit dem Prüfprojekt 1048 (Testdatenbank `b68638da…`):
  14/14 PASS, GESAMT 4.610.207 Werte innerhalb der Toleranz, 432/432 CSV byte-gleich (Lauf 22:03–22:04 auf `80a7b9fb`).
- **Tests** (Worktree `e27` nach E27/4, mit den xUnit-Schaltern, zweimal): 14.803 bestanden / 2 übersprungen / 0 rot
  (EPOS.Kern 7.446/7.447, EPOS.UI 6.395, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27/28); ChartProben 174
  Bilder, 0 Verstöße; SqlDialektPruefer 1.927/0.
- **Merge** `80a7b9fb` auf `pm26` über `50ecd802`.
- **Gate:** Gate #521 auf `80a7b9fb` (21:56–22:03; Gate-Log noch unter der Nummer 520): Kern-Filter 0 Fehler, ChartProben 0 Fehler, 161 Bild-Hashes gleich mit der Messlatte, Tests Kern 7.495 (1 übersprungen), UI 6.396, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), Dokumentationswachen 29/29.
- **CI:** steht aus (Beobachtung nach dem Push) — die CI muss gegen R19 laufen (`kern.yml` und `ios.yml` mit dem Basispfad R19).

## Abnahme am Gerät (A‑E27‑1, Windows)

1. 1018 rechnen: Ergebnisansicht, BHKW-Reiter und Übersicht zeigen Netzbezug und Reststrombedarf 0 (nicht
   −27,46 MWh).
2. 1018 mit Strompreis im Rollentarif: Reststromkosten 0, KWK-Einspeisung 27,46 MWh in der Strommatrix-Tabelle.
3. 1030: Netzbezug 4.358,17 MWh, Kapitalwert Erwartet ≈ −31.142.971 €.
4. Der Kern-Lauf ist grün gegen R19.

Belegt in `BhkwNetzbezugKlemmeTests`, im 1030-Anker von `PvAusweisStromMatrixTests` und in `aggregate.csv` von R19;
die Sichtprüfung liegt beim Anwender.

## Konzeptvermerk (E27‑Q1/Q4)

In Konzept § 3.6 eingearbeitet: „Der Netzbezug ist nie negativ: Ein BHKW-Überschuss, den keine spätere Stufe
(Verbraucher derselben Viertelstunde, Photovoltaik, Stromspeicher) aufnimmt, steht allein im KWK-Split als
Einspeisung; der Reststrom wird am Laufende bei 0 geklemmt, der Reststrombedarf der BHKW-Zeile je Stunde (E27,
Entscheide E27‑Q1/Q4).“

## Logbuch

Vorschlag (Version beim Anwender erfragen, mit dem nächsten Sammel-Upload): „Ein Stromüberschuss des BHKW mindert den
Netzbezug nicht mehr, sondern wird ausschließlich als Einspeisung ausgewiesen.“ Kein Wiki-Fachtext geändert.

## Papiere mit der Statuszeile

Mit dem Merge (E27/4): der Basisname und die Referenzbasen-Papiere (Abschnitt „Die neue Basis“). Mit dieser
Statuszeile: Konzept (Kopf mit Codestand `80a7b9fb`; Schrittabsatz „E27 (#521) ohne Schritt“; § 3.6 Konzeptvermerk;
§ 6.1 Zeile E27; § 6.3 Nr. 34 N5 erledigt und neue Nr. 36; § 7; Anhang), Register (Kopf, Familientafel, neue Familie
R‑E27, R‑E26 Q7 „N5 gebaut #521“), Analysepapier (Kopf, Nachtrag, § 5 Zeile E27), Protokoll der Entscheidwege (Kopf
von § 8, § 8.54, § 8.55), Dokumentations-Index (Reporting 141 → 142, Referenzbasen 36 Basen und 37 Dateien),
Statusdatei (#521, Nach #521; in #519 der CI-Vermerk des Pushs `50ecd802`). Das Archiv der Referenzbasen ist geprüft:
Tabellenzeile und Abschnitt R18 nennen Datum und Grund der Ablösung. Kein Mockup (kein Dialog), kein Wiki-Fachtext.

## Offen

- **Q3 b:** eine eigene Ausweisgröße „BHKW-Einspeisung“ (Diagnosereihe und Zeile im BHKW-Reiter; Ressourcen, Mockup,
  Papier).
- **N7:** Der Vorab-Überschuss für den PV-Modus der Wärmepumpe (`SimulationControl.cs:4330ff`) liest den negativen
  Rest nach dem BHKW als PV-Überschuss; die Kessel-Vektorstufe hinter dem BHKW bekommt einen negativen Stromeingang
  (`:913-915`). Empfehlung: Prüfwelle E28 nach Anwenderentscheid.
- **Q6:** Die gespeicherten Altergebnisse 1018 (−14,47) und 1031 (−14,72) heilen erst beim nächsten Lauf.
- Die **Abnahme am Gerät** A‑E27‑1.
- **Gate**, **Referenzlauf nach dem Merge** und **CI** (Nachweis oben).
