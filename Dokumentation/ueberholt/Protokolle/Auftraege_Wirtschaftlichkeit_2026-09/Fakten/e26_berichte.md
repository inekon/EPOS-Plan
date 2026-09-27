# E26 — Bericht Phase 0 (Opus, 25.09.2026 ca. 19:45, Worktree e26 = 868afc57, kein Commit; Proben auf Kopie 0c2fe21a im Scratchpad e26probe)

**N1 Ursache:** `SimulationPV.cs:444-452` füllt `Stromproduktion_Theoretisch` (Erzeugung AC nach WR-Kennlinie und Clipping) und `Stromproduktion`
(nur Direktverbrauch; Umdeutung „Geänderte Ausweissemantik" AP2b `SimulationPV.cs:22-28`); `SimulationRunner.cs:989` summiert die
Direktverbrauchsreihe in `pvm.Stromproduktion`, Modell sagt „Gesamte Stromerzeugung der Module" (`ErgebnisModel.cs:559`); Modulzeilen führen die
Erzeugung (`SimulationRunner.cs:1034`, `SimulationPV.cs:416`); E25-Vermutung Stunde/Viertelstunde trifft nicht zu — Schreibfehler an einer Stelle.
Falsch rechnende Leser: `WirtschaftlichkeitCtrl.cs:6167` PvVermiedenerBezugAusweis, `:2576` Eigenverbrauch Degradations-Mehrbezug (kapitalwertwirksam
nur bei Dialog + Degradation; Testdatenbank 0 Zeilen Tab_ProjektPhotovoltaik), `KennzahlenKatalog.cs:375-376/430-435`, `PhotovoltaikVerguetungHuelle.cs:83`
→ `PvKennzahlenRechner.cs:58/82`, `BausteineVergleich.cs:429`; gespeichert `ErgebnisCtrl.cs:637` (Tab_ErgebnisPhotovoltaik); aggregate.csv-Skalar
`Photovoltaik.Stromproduktion`; `pv_produktion.csv` aus Array (`Ergebnisexport.cs:204`) bleibt. Richtig: Ergebnisansicht `SimulationErgebnisCtrl.cs:847-859`,
Diagramm `Huelle.Bilder.cs:665-673`, `PV_GENUTZT` `ZeitreihenExtraktor.cs:54`. **N1-A (empf.):** Zeile 989 → `Stromproduktion_Theoretisch.Sum()/1000`
(Probe: = Summe Modulzeilen bitgleich). N1-B: Leser umstellen, R17 bleibt. Zahlen (MWh) alt→neu: 1040 4,441→6,713 (Einsp. 2,273; Ausweis Eigenverbrauch
2,168→4,441; Quote 48,8→66,1 %); 1026 4,197→6,713 (1,246; 2,950→5,467; 70,3→81,4 %); 1042 0; 1007 5,081→6,014 (0,326); 1045 2,744→3,545 (0,802);
1046 5,081→6,014 (0,895); 1048 geschätzt 6,37→13,43, Ausweis −0,69→6,37.
**N3 Ursache:** `ZeitreihenExtraktor.cs:31-32` STROMBEDARF = nur Haushalt/Lastgang; `StromMatrix.cs:169-212` nimmt ihn als „Bedarf ohne Eigenerzeugung";
NETZBEZUG (`:109`) = Rest nach Kaskade (WP + Heizstab `SimulationControl.cs:835-840`, E-Kessel `:868/896` bei Brennstoff 13 `SimulationSPK.cs:413-426`,
Kältestrom Stufenrechnung `SimulationControl.Kaelte.cs:297-300`); BHKW ohne Klemme abgezogen (`:880/919`); Kältestrom mit eigenem Zähler bewusst nicht
(E34); keine eigene Hilfsenergie-Reihe. Konzept § 3.6 (`:2364-2381`) verlangt Bruttobedarf − Restbezug → #437 bekam die falsche Reihe. **Vorschlag:** neue
Reihe `STROMBEDARF_GESAMT` = Rest nach Kaskade + BHKW-Strom (Probe: Brutto − BHKW = PV-Eingangsbedarf, max. Abweichung 0,000; 1040 27,427 = 8,000 + 19,427 WP);
`StromMatrix.Baue` nimmt sie für BedarfGesamtMWh, PvEigenGesamtMWh, LastBedarf, Rückfall STROMBEDARF (synthetische Tests unverändert); in
`SzenarioMengen.ENERGIEREIHEN` (`:37-46`) nachtragen. Wirkung Rollentarif 0,30 €/kWh: 1040 Bedarf 8,000→27,427, Netzbezug 22,986, vermiedene Menge
−14,986→4,441, Kosten −4.496→+1.332 €/a, PvEigen 2,602→4,441; 1026 8,000→31,351, −18,004→5,348 (PV 4,197 + Entladung ~1,15), −5.401→+1.604; 1042
8,000→41,345, −33,345→0; weitere WP: 1007 −38,8→5,6, 1045 −20,8→2,7, 1046 −39,9→4,5, 1023 −137,5→0, 1039 −60,9→0; BHKW: 1017 16,1→36,8, 1024 3,7→95,7,
1047 30,8→35,9, 1030/1018 gleich. Reststromkosten (Kapitalwert) in allen Proben bitgleich. Kapitalwert: nur `r.Reststrom` (`:2465-2467`); Bezug,
vermiedene Kosten, § 9b (`:6427-6439`), Verteilschlüssel (`:6254/6275`) Ausweis; Ausnahme KWK-Split (KWKG-Zuschlag, Stromsteuer `:4172-4173`,
Einspeiseerlös `:2444`) — an Referenzen unverändert, in fremden Projekten (BHKW zwischen Haushalt und Brutto) wandert er (Q3).
**R17:** N3 ändert nichts (aggregate.csv ohne Wirtschaftlichkeit); N1-A ändert einen Skalar je PV-Projekt `Photovoltaik.Stromproduktion`: 1007 5,08→6,01,
1040 4,44→6,71, 1045 2,74→3,55, 1046 5,08→6,01 (1041/1042 0) → **R18** mit A/B-Tafel 4 Zeilen.
**Fragen (Entscheid Orchestrator 25.09.2026 ~19:50 nach Empfehlung, Anwender „Empfehlung/bearbeiten"):** Q1 a N1-A + R18; Q2 a E26 friert R18 selbst ein;
Q3 a KWK-Split auf Bruttobedarf (Konzeptvermerk; Kapitalwert fremder Projekte kann wandern → Anwender gemeldet); Q4 a Eigenverbrauch = Erzeugung − Einspeisung
(inkl. Speicherladung; Flotte 1046 Abregelung messen); Q5 a kein Schemaschritt (alte Tab_ErgebnisPhotovoltaik heilen beim nächsten Lauf); Q6 b Strombilanz-
Diagramm/Excel „Strombedarf" später; Q7 N5 (1018 negativer Netzbezug −27,46 MWh, kapitalwertwirksam Reststrom −8.237 €; Rest ohne Klemme
`SimulationControl.cs:880/919, :612-614`) und N6 (Übersicht „Strombedarf mit Eigenverbrauch" `SimulationErgebnisCtrl.cs:198-201` ohne Kältestrom) nur melden.
Aufwand ~5 h + Gate. Bau freigegeben.

# E26 — Bericht Phase 1 (Opus, 25.09.2026 ca. 20:25, e26 = 4653fa06 über 868afc57)

Commits: bc1d8ad8 E26/1 N1 `SimulationRunner.cs:989-998` `pvm.Stromproduktion = pvs.Stromproduktion_Theoretisch.Sum()/1000` (Leser unverändert:
WirtschaftlichkeitCtrl.cs:6167/:2576, KennzahlenKatalog.cs:376/:434, PhotovoltaikVerguetungHuelle.cs:83, BausteineVergleich.cs:429); 4fd6eaa6 E26/2 N3
(Q3 a): `SimulationControl.Strombedarf_Verbraucher_viertelstuendlich` nach `Kaskade_Zweikanalig()` = Rest + BHKW-Strom (Projektbedarf, WP, Heizstab,
E-Kessel, Kältestrom Stufenrechnung; nicht Kältestrom mit eigenem Zähler E34), `ZeitreihenSatz.STROMBEDARF_GESAMT` (BerichtsDaten.cs),
ZeitreihenExtraktor.cs:31ff, `StromMatrix.Baue` nimmt STROMBEDARF_GESAMT (Rückfall STROMBEDARF) für Bedarf, PvEigen, Lastbild, KWK-Split;
`SzenarioMengen.ENERGIEREIHEN` ergänzt; 3136a267 E26/3 `EPOS.Kern.Tests/PvAusweisStromMatrixTests.cs` 11 Fälle (N1 1040/1026/1042 Stromproduktion =
Σ Modulzeilen = genutzt + Überschuss, Ausweis-Eigenverbrauch ≥ Direktverbrauch ≥ 0, Kennzahlen pv_strom/pv_eigen; N3 Bedarf = Σ Verbraucherreihen =
PV-Eingangsbedarf + BHKW stundenweise < 1e-6, Bedarf ≥ Netzbezug, PvEigen = PV_GENUTZT, vermiedene Menge = PvEigen + Entladung; Anker; Kapitalwert
bitgleich 1024 Erwartet −2.772.642,2674731 / Best −2.801.567,7561814, 1030 Erwartet −31.141.242,7086938, KWKG Jahr 1 7.322,63); 4653fa06 E26/4
Einfrierung R18. Zahlen (Rollentarif 0,30): 1040 Stromproduktion 4,441→6,713, Ausweis-EV 2,168→4,441, Bedarf 8,000→27,427, vermiedene Menge
−14,986→4,441, Kosten −4.496→+1.332; 1026 4,197→6,713, 2,950→5,467, 8,000→31,351, −18,004→5,348, −5.401→+1.604; 1042 0, Bedarf 8,000→41,345,
−33,345→0, −10.003→0. Reststrom, KWK-Split (1017/1024/1047/1030/1018 KwkEigen/KwkEinsp gleich), Kapitalwert überall unverändert. Q4 gemessen: 1046
Abregelung 0,000 MWh, EV = Erzeugung − Einspeisung 5,119 (genutzt 5,081); 1046 vermiedene Menge 4,523 < PV + Entladung 1,057, weil Flotte auch aus dem
Netz lädt (kein Fehler). Tests voll mit Schaltern 14.632 grün / 2 übersprungen / 0 rot (Kern 7.292/7.293, UI 6.378, KiKern 549, SpeicherEngine 386,
SpeicherPlanung 27/28); ChartProben 174 Bilder 0 Verstöße; SQL-Prüfer 1.920/0; Testhost-Regel einmal verletzt (gefilterter 4-s-Lauf bei fremdem
testhost, grün). A/B R17→R18: 10/14 PASS byte-gleich, 428/432 CSV, nur `aggregate.csv` `Photovoltaik.Stromproduktion`: 1007 5,08→6,01, 1040 4,44→6,71,
1045 2,74→3,55, 1046 5,08→6,01; Zeitreihen byte-gleich; Determinismus 2 Läufe 14/14 byte-gleich, GESAMT PASS 4.610.207 Werte, 2.447 Skalare;
-wal/-shm aus Phase 0 gelöscht, Einfrierlauf wiederholt (DB unverändert 0c2fe21a). Einfrierung: neu `Referenzlaeufe/2026-09-25_R18_PvAusweis/` (432 CSV +
protokoll.txt); R17 protokoll.txt git mv nach `Dokumentation/ueberholt/Referenzbasen/2026-09-25_R17_Datenpflege/`, Rest git rm; Archiv-LIESMICH 35 Basen/36
Dateien, Zeile R17, Link :19, Abschnitt „Die Basis R17 im Einzelnen"; Referenzlaeufe/LIESMICH „Aktuelle Basis" R18 mit A/B-Tafel, Entfernte Basen 35/elf
Protokolle, Weg A. Basisname R17→R18: CLAUDE.md:146, kern.yml:264, 268-273 (7), ios.yml:224/228, Dokumentation/LIESMICH.md:251,
Konzept_Gebaeudesimulation:1931, Systementwurf:179, Konzept_Wirtschaftlichkeit :3/:2882/:3007, Analysepapier :8/:565 („seit E26 gilt … R18"),
Basenhistorie:7; Geschichte unverändert (Archiv-LIESMICH:2121, DatenpflegeKesseltraegerTests.cs:22 Kommentar). Konzeptvermerk Q3 (§ 3.6 / § 6.3 Nr. 32):
„Bedarf ohne jede Eigenerzeugung ist der Strombedarf aller Verbraucher des Anschlusses (Projektbedarf, Wärmepumpe, Heizstab, Elektrokessel, Kältestrom der
Stufenrechnung; Reihe STROMBEDARF_GESAMT); auch der KWK-Split (Eigenstrom = min(BHKW-Strom, Bedarf nach PV-Eigennutzung)) misst sich daran, weil die
Simulation das BHKW den Strom der Wärmepumpe decken lässt (E26, Entscheid E26‑Q3)." Logbuch-Vorschlag: „Die Photovoltaik weist als Stromproduktion die
gesamte Erzeugung der Module aus; vermiedener Netzbezug und vermiedene Stromkosten beziehen den Strom von Wärmepumpe, Heizstab und Elektrokessel ein."
Abnahme A‑E26‑1: WP-Projekt mit PV (1040 mit Strompreis oder 1048): Ergebnisansicht/Bericht PV-Stromerzeugung = Σ Modulzeilen; „PV: vermiedener Bezug"
≥ 0 (1048 ~6,37 MWh); Rollentarif vermiedene Menge/Kosten positiv (1040 4,44 MWh); Kapitalwert unverändert; Kern-Lauf grün gegen R18. Restpunkte: Q6
Strombilanz-Diagramm ChartRenderer.cs:457-471 und Excel „Strombedarf" ExcelBerichtGenerator.cs:1834 weiter Projektbedarf; N5 1018 negativer Netzbezug
−27,46 MWh (SimulationControl.cs SubVectors(…, false) :880/:919, ReststromMwh :612-614; Rollentarif Reststrom −8.237 € kapitalwertwirksam); N6
SimulationErgebnisCtrl.cs:198-201 StrombedarfMitEigenverbrauchMwh ohne Kältestrom; PV-Projekte der Testdatenbank ohne Strompreis → Kapitalwert-Nachweis an
1024/1030; 1048 kommt mit E25.
