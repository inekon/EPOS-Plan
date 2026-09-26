# Die Protokolle der 38 entfernten Referenzbasen

**Was hier liegt.** Für jede der **38 historischen Referenzbasen** unter `Referenzlaeufe/` das
Protokoll ihrer Entstehung — `lauf_protokoll.md` beziehungsweise `protokoll.txt`, byte-gleich
aus dem Stand `b02f986^` (= dem letzten Commit vor der Löschung) gesichert; das Protokoll von R7 kam am
16.09.2026 dazu, das von R8 am 18.09.2026, das von R9 am 19.09.2026, das von R10 am 22.09.2026, das von R11 und das von R12 am 23.09.2026, das von R13 am 24.09.2026, das von R14, das von R15, das von R16, das von R17 und das von R18 am 25.09.2026, das von R19 und das von R20 am 26.09.2026. **39 Dateien.** Nur Text: was gemessen wurde, gegen welche Vorgängerbasis, mit welchem
Ergebnis, welche Abweichung gewollt war und welche Gegenprobe sie belegt.

**Warum hier.** Die Ordner der Basen sind am 11.09.2026 mit dem Sync-Commit `b02f986` aus dem
Arbeitsbaum gefallen (Anwenderentscheid **SYNC‑Q1**: „entfernt lassen"); abrufbar blieben sie
über die Git-Geschichte. Mit dem Anwenderentscheid **AUF‑Q1** vom **12.09.2026** („ausführen",
Auftrag #244) ist die Geschichte umgeschrieben worden — **die Basen sind seither auch dort
nicht mehr enthalten.** Damit wäre die Begründung jeder einzelnen Basiswahl verloren gegangen.
Deshalb sind die Protokolle **vor** dem Umschreiben hierher gesichert worden.

> **Die Messdaten selbst sind endgültig weg.** Die rund **8 000 CSV-Dateien** der 25 Basen
> sind weder im Arbeitsbaum noch in der Git-Geschichte. Wer eine alte Zahl braucht, findet
> sie **nur noch im Protokoll** — oder rechnet sie neu. Die einzige lauffähige Basis ist
> [`Referenzlaeufe/2026-09-26_R21_BhkwDeckung`](../../../Referenzlaeufe/2026-09-26_R21_BhkwDeckung/);
> gegen sie prüfen Gate und CI.

Die Übersicht der Basen mit Datum und Zweck steht — samt der Begründung der Löschung — im
Abschnitt „Entfernte Basen" von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md); die Tabelle unten ist von
dort übernommen und um die Spalte des gesicherten Protokolls ergänzt.

## Basis → Datum → Zweck → Protokoll

| Ordner | Datum | Zweck | Protokoll |
|---|---|---|---|
| `2026-08-27_V0` | 27.08.2026 | Stand nach den V0-Bestandsfehler-Fixes (Mehrgebäude-Doppelzählung, summierte statt überschriebene Stromprofile) und vor der Dreikanal-Umstellung; neun Projekte, 216 CSV | [`2026-08-27_V0/lauf_protokoll.md`](2026-08-27_V0/lauf_protokoll.md) |
| `2026-08-27_K1` | 27.08.2026 | Basis der Pakete K1 bis S2 nach der Dreikanal-Umstellung (anteilige Netzverlustverteilung F2, Wochenprofile am Klimadaten-Kalender F3); neun Projekte, 216 CSV | [`2026-08-27_K1/lauf_protokoll.md`](2026-08-27_K1/lauf_protokoll.md) |
| `2026-08-27_A1` | 27.08.2026 | erste Basis mit den vier Konzept-11.1-Projekten nach dem Altpfad-Abriss, Meilenstein „ein Rechenweg"; dreizehn Projekte, 329 CSV | [`2026-08-27_A1/lauf_protokoll.md`](2026-08-27_A1/lauf_protokoll.md) |
| `2026-08-27_E1` | 27.08.2026 | Basis des Meilensteins Z3 (Paket E1, Ergebnis je Kanal, Migrationsschritt 52); dreizehn Projekte, 329 CSV | [`2026-08-27_E1/lauf_protokoll.md`](2026-08-27_E1/lauf_protokoll.md) |
| `2026-08-28_P1` | 28.08.2026 | Stand nach Paket P1 (Schichtspeichermodell, Migrationsschritt 53), konstruktiv byte-gleich zu E1; dreizehn Projekte, 329 CSV | [`2026-08-28_P1/lauf_protokoll.md`](2026-08-28_P1/lauf_protokoll.md) |
| `2026-08-28_B2` | 28.08.2026 | Stand nach Paket B2 (Kessel-Temperaturmodus, wählbarer Booster-Lesepunkt, Schema 55) plus Datenänderung an 1042; gemeinsamer Ausgangspunkt beider Basen vom 29.08.2026; dreizehn Projekte, 332 CSV | [`2026-08-28_B2/lauf_protokoll.md`](2026-08-28_B2/lauf_protokoll.md) |
| `2026-08-28_E2` | 28.08.2026 | erste Basis mit scharfer Booster-Temperaturkopplung in Projekt 1042 (Codestand E2/D-Check); dreizehn Projekte, 332 CSV | [`2026-08-28_E2/lauf_protokoll.md`](2026-08-28_E2/lauf_protokoll.md) |
| `2026-08-29_Booster` | 29.08.2026 | Referenz des 13-Projekte-Zweitstands mit erstmals scharfer Booster-Temperaturkopplung (Anlage 14818, `WQ_Unbegrenzt = False`); dreizehn Projekte, 332 CSV | [`2026-08-29_Booster/lauf_protokoll.md`](2026-08-29_Booster/lauf_protokoll.md) |
| `2026-08-29_E1E2` | 29.08.2026 | Referenz des 10-Projekte-Bestands dieses Rechners nach den Emissions-Etappen E1/E2 (CO2-Saat, Emissionsarten-Katalog) samt Umbau von 1039 und Löschung von 1040–1042; zehn Projekte, 234 CSV | [`2026-08-29_E1E2/lauf_protokoll.md`](2026-08-29_E1E2/lauf_protokoll.md) |
| `2026-08-30_B3-Kaskade` | 30.08.2026 | Stand nach Wiederherstellung der 1030-BHKW-Kaskade und Neuaufbau von 1042, Zusammenführung der beiden Datenbestände vom 29.08.2026; dreizehn Projekte, 332 CSV | [`2026-08-30_B3-Kaskade/lauf_protokoll.md`](2026-08-30_B3-Kaskade/lauf_protokoll.md) |
| `2026-09-02_PA0_vor-PaketA` | 02.09.2026 | Ausgangsbasis vor Paket A, einzige Quelle der Ganglinien im UTC-Raster, nach dem Wechsel auf SQLite und dem Projektwechsel (1040–1042 gelöscht, 1026/1028/1029/1043 neu); vierzehn Projekte, 355 CSV | [`2026-09-02_PA0_vor-PaketA/lauf_protokoll.md`](2026-09-02_PA0_vor-PaketA/lauf_protokoll.md) |
| `2026-09-02_PA1_nach-PaketA` | 02.09.2026 | Stand nach Paket A des PV-Ertragsmodell-Konzepts (Zeitbasis UTC → Ortszeit, Stufe E1 „Eine Wahrheit"), byte-gleich zu PB1; vierzehn Projekte, 355 CSV | [`2026-09-02_PA1_nach-PaketA/lauf_protokoll.md`](2026-09-02_PA1_nach-PaketA/lauf_protokoll.md) |
| `2026-09-03_M1_nach-Merge` | 03.09.2026 | Stand nach dem Merge von `origin/ios_migration` (Umzug des Rechenkerns nach `EPOS.Kern`/`EPOS.UI`), byte-gleich zu PB1; vierzehn Projekte, 355 CSV | [`2026-09-03_M1_nach-Merge/lauf_protokoll.md`](2026-09-03_M1_nach-Merge/lauf_protokoll.md) |
| `2026-09-03_M2_nach-Merge2` | 03.09.2026 | Stand nach dem zweiten Merge (Blazor-Dialoge iU9, iOS-Hülle iU10, SQL-Dialekt-Audit, Wirtschaftlichkeitspakete FX2–FX5/B5), byte-gleich zu M1; vierzehn Projekte, 355 CSV | [`2026-09-03_M2_nach-Merge2/lauf_protokoll.md`](2026-09-03_M2_nach-Merge2/lauf_protokoll.md) |
| `2026-09-03_M3_nach-Merge3` | 03.09.2026 | Stand nach dem dritten Merge (iU9 Welle 1: sieben WinForms-Masken der Kosten-/Wirtschaftlichkeitsseite durch Razor-Komponenten ersetzt), byte-gleich zu M2; vierzehn Projekte, 355 CSV | [`2026-09-03_M3_nach-Merge3/lauf_protokoll.md`](2026-09-03_M3_nach-Merge3/lauf_protokoll.md) |
| `2026-09-03_M4_nach-Merge4` | 03.09.2026 | Stand nach dem vierten Merge (iU9 Welle 0: Stilllegung von neun Altmasken), byte-gleich zu M3; vierzehn Projekte, 355 CSV | [`2026-09-03_M4_nach-Merge4/lauf_protokoll.md`](2026-09-03_M4_nach-Merge4/lauf_protokoll.md) |
| `2026-09-03_PB1_nach-PaketB` | 03.09.2026 | Stand nach Paket B (PV-Modellwahl je Anlage: Hay-Davies, Huld-Schwachlichtmodell, Wechselrichter-Teillastkennlinie, Degradation), byte-gleich zu PA1; vierzehn Projekte, 355 CSV | [`2026-09-03_PB1_nach-PaketB/lauf_protokoll.md`](2026-09-03_PB1_nach-PaketB/lauf_protokoll.md) |
| `2026-09-05_M5_nach-Merge5` | 05.09.2026 | Stand nach dem fünften Merge (iU9-Wellen W2–W16c, Umzug auf die Razor-Struktur, zwölf stillgelegte WinForms-Masken), byte-gleich zu M4; vierzehn Projekte, 355 CSV | [`2026-09-05_M5_nach-Merge5/lauf_protokoll.md`](2026-09-05_M5_nach-Merge5/lauf_protokoll.md) |
| `2026-09-05_R2_Zeitbasis` | 05.09.2026 | plattformfreie CI-Basis nach Zusammenführung der Rechner-2-Linie (Paket A: Solar-Zeitbasis UTC → Ortszeit); elf Projekte, 282 CSV | [`2026-09-05_R2_Zeitbasis/protokoll.txt`](2026-09-05_R2_Zeitbasis/protokoll.txt) |
| `2026-09-06_R3_Straenge` | 06.09.2026 | CI-Basis mit dem ersten Strang-Projekt 1045 „Prüfprojekt Ost/West Stränge" (Wechselrichterkonzept, Vorrangregel Kapitel 3.5); zwölf Projekte, 312 CSV | [`2026-09-06_R3_Straenge/protokoll.txt`](2026-09-06_R3_Straenge/protokoll.txt) |
| `2026-09-07_M7_nach-Merge7` | 07.09.2026 | Stand nach dem siebten Merge (PV-Strangtabelle W6‑B‑4, Schemaschritt 69 samt Linux-Basis R6) — die Windows-Reihen-Basis „Aktuelle Basis" bis zur Ablösung; vierzehn Projekte, 355 CSV | [`2026-09-07_M7_nach-Merge7/lauf_protokoll.md`](2026-09-07_M7_nach-Merge7/lauf_protokoll.md), dazu [`vergleich_M5_zu_M7.txt`](2026-09-07_M7_nach-Merge7/vergleich_M5_zu_M7.txt) |
| `2026-09-07_R4_Double` | 07.09.2026 | CI-Basis nach Anwenderentscheid W8‑O‑5d („alles in double" statt `float`); zwölf Projekte, 312 CSV | [`2026-09-07_R4_Double/protokoll.txt`](2026-09-07_R4_Double/protokoll.txt) |
| `2026-09-07_R5_Zahlenrand` | 07.09.2026 | CI-Basis nach den Entscheiden W8‑O‑5d‑Q1 (Zahlenrand), W8‑O‑5d‑Q2 (keine `int`-Abschneidung im BHKW-Plan-Port) und Em‑9.8 (zehn Emissionsskalare); zwölf Projekte, 312 CSV, 1 792 Skalare | [`2026-09-07_R5_Zahlenrand/protokoll.txt`](2026-09-07_R5_Zahlenrand/protokoll.txt) |
| `2026-09-07_R6_PvKoeffizienten` | 07.09.2026 | CI-Basis nach Befund W6‑B‑5 (Reparatur der PV-Modulkoeffizienten aus der CEC-Liste, Schemaschritt 69); zwölf Projekte, 312 CSV, 1 792 Skalare — abgelöst durch R7 am 11.09.2026 | [`2026-09-07_R6_PvKoeffizienten/protokoll.txt`](2026-09-07_R6_PvKoeffizienten/protokoll.txt) |
| `2026-09-11_R7_Speicherflotte` | 11.09.2026 | CI-Basis nach Anwenderentscheid SP‑O‑8 mit dem Prüfprojekt 1046 „Prüfprojekt Speicherflotte" (Mehrspeicherpfad im gewöhnlichen Projektlauf); dreizehn Projekte, 345 CSV, 1 937 Skalare — abgelöst durch R8 am 16.09.2026 | [`2026-09-11_R7_Speicherflotte/protokoll.txt`](2026-09-11_R7_Speicherflotte/protokoll.txt) |
| `2026-09-16_R8_Heizkessel_Kaskade` | 16.09.2026 | CI-Basis nach Anwenderentscheid HK‑E‑1 (ein Heizkessel im Projekt bekommt seinen Kaskadenplatz automatisch, nachrangig); dreizehn Projekte, 357 CSV, 2 057 Skalare — abgelöst durch R9 am 18.09.2026 | [`2026-09-16_R8_Heizkessel_Kaskade/protokoll.txt`](2026-09-16_R8_Heizkessel_Kaskade/protokoll.txt) |
| `2026-09-18_R9_Kesselbrennstoff` | 18.09.2026 | CI-Basis nach Befund `B-1` (der Brennstoffverbrauch des Heizkessels steht in der Modulzeile); dreizehn Projekte, 357 CSV, 2 057 Skalare — abgelöst durch R10 am 19.09.2026 | [`2026-09-18_R9_Kesselbrennstoff/protokoll.txt`](2026-09-18_R9_Kesselbrennstoff/protokoll.txt) |
| `2026-09-19_R10_BhkwWirkungsgrad` | 19.09.2026 | CI-Basis nach Anwenderentscheid BH1‑O1 (der BHKW-Wirkungsgrad ist ein Faktor, Katalog vereinheitlicht, Schemaschritt 98); dreizehn Projekte, 357 CSV, 2 057 Skalare — abgelöst durch R11 am 22.09.2026 | [`2026-09-19_R10_BhkwWirkungsgrad/protokoll.txt`](2026-09-19_R10_BhkwWirkungsgrad/protokoll.txt) |
| `2026-09-22_R11_Bestandsbefunde` | 22.09.2026 | CI-Basis nach der Stufe GB der Gebäudesimulation (Bestandsbefunde des Tagesbilanz-Wegs, `Bauweise` von Gebäude 10576); die letzte Basis allein auf dem Tagesbilanz-Weg; dreizehn Projekte, 357 CSV, 2 057 Skalare — abgelöst durch R12 am 23.09.2026 | [`2026-09-22_R11_Bestandsbefunde/protokoll.txt`](2026-09-22_R11_Bestandsbefunde/protokoll.txt) |
| `2026-09-23_R12_Gebaeudemodell` | 23.09.2026 | CI-Basis nach der Schlusswelle G1 + G2 der Gebäudesimulation (VDI 6007 als Vorgabemodell aller Gebäude, 1040 auf dem Tagesbilanz-Weg); ungekühlte Gebäude noch an `Maximaleraumtemperatur` gekappt; dreizehn Projekte, 399 CSV, 2 239 Skalare — abgelöst durch R13 am 23.09.2026 | [`2026-09-23_R12_Gebaeudemodell/protokoll.txt`](2026-09-23_R12_Gebaeudemodell/protokoll.txt) |
| `2026-09-23_R13_Kuehlung` | 23.09.2026 | CI-Basis nach der Schlusswelle KU1 der Kühlung (E32: ungekühlte Gebäude laufen frei; Projekt 1017 als Referenzprojekt mit Kühlung, Kältebedarf ungedeckt); dreizehn Projekte, 387 CSV, 2 207 Skalare — abgelöst durch R14 am 24.09.2026 | [`2026-09-23_R13_Kuehlung/protokoll.txt`](2026-09-23_R13_Kuehlung/protokoll.txt) |
| `2026-09-24_R14_Kaelteerzeuger` | 24.09.2026 | CI-Basis nach der Schlusswelle KU2 der Kühlung (Projekt 1017 mit Kälteerzeuger: die Wärmepumpe kühlt über ihre Kühlkennlinie); dreizehn Projekte, 394 CSV, 2 249 Skalare — abgelöst durch R15 am 25.09.2026 | [`2026-09-24_R14_Kaelteerzeuger/protokoll.txt`](2026-09-24_R14_Kaelteerzeuger/protokoll.txt) |
| `2026-09-25_R15_Anlagenkopplung` | 25.09.2026 | CI-Basis nach der Stufe AK1 der Anlagenkopplung (Referenzprojekt 1047: Kopie von 1017 mit gekoppeltem Heizkreis und gekoppelter Kühlübergabe); getragen bis Schemastand 142; vierzehn Projekte, 432 CSV, 2 447 Skalare — abgelöst durch R16 am 25.09.2026 | [`2026-09-25_R15_Anlagenkopplung/protokoll.txt`](2026-09-25_R15_Anlagenkopplung/protokoll.txt) |
| `2026-09-25_R16_Anlagenprio` | 25.09.2026 | CI-Basis nach der Rechenweg-Sortierung der Anlagen nach der Regel „99“ (E22, Konzept Wirtschaftlichkeit § 6.3 Nr. 18: gepflegte Priorität zuerst; einzige Wirkung die Modulreihenfolge der Wärmepumpen von 1042); getragen bis Schemastand 143; vierzehn Projekte, 432 CSV, 2 447 Skalare — abgelöst durch R17 am 25.09.2026 | [`2026-09-25_R16_Anlagenprio/protokoll.txt`](2026-09-25_R16_Anlagenprio/protokoll.txt) |
| `2026-09-25_R17_Datenpflege` | 25.09.2026 | CI-Basis nach der Datenpflege nach Konzept Wirtschaftlichkeit § 6.3 Nr. 24 (E24: die Kessel von 1018 und 1023 tragen den Energieträger 63, 1023 dazu eine Projektzeile und einen Preisstand für Erdgas; einzige Wirkung `HeizkesselModul[0].carrier_id` von 1018 und 1023); Schemastand 143; vierzehn Projekte, 432 CSV, 2 447 Skalare — abgelöst durch R18 am 25.09.2026 | [`2026-09-25_R17_Datenpflege/protokoll.txt`](2026-09-25_R17_Datenpflege/protokoll.txt) |
| `2026-09-25_R18_PvAusweis` | 25.09.2026 | CI-Basis nach dem PV-Ausweis (E26, Befund N1: `Photovoltaik.Stromproduktion` ist die Erzeugung der Module; einzige Wirkung dieser Skalar von 1007, 1040, 1045 und 1046); getragen bis Schemastand 144 samt Datenpflege E24; vierzehn Projekte, 432 CSV, 2 447 Skalare — abgelöst durch R19 am 25.09.2026 | [`2026-09-25_R18_PvAusweis/protokoll.txt`](2026-09-25_R18_PvAusweis/protokoll.txt) |
| `2026-09-25_R19_BhkwNetzbezug` | 25.09.2026 | CI-Basis nach Anwenderentscheid E27‑Q1…Q8 (der Netzbezug ist nie negativ, Befund N5 aus E26); getragen bis Schemastand 148 samt Prüfprojekt 1048 (ohne Referenzrolle) und den Schemaschritten 145–148; vierzehn Projekte, 432 CSV, 2 447 Skalare — abgelöst durch R20 am 26.09.2026 | [`2026-09-25_R19_BhkwNetzbezug/protokoll.txt`](2026-09-25_R19_BhkwNetzbezug/protokoll.txt) |
| `2026-09-26_R20_Zapfprofil` | 26.09.2026 | CI-Basis nach Anwenderentscheid ZU7 (Projekt 1045 rechnet sein Brauchwasser über den Zapfprofilgenerator); getragen bis zur Testdatenbank `22e67400…` (Speicherauslegung #543); vierzehn Projekte, 432 CSV, 2 447 Skalare — abgelöst durch R21 am 26.09.2026 | [`2026-09-26_R20_Zapfprofil/protokoll.txt`](2026-09-26_R20_Zapfprofil/protokoll.txt) |

## Die Basis R7 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis" hat bis zum 16.09.2026 die Basis R7 beschrieben — Anlass,
Aufbau des Prüfprojekts 1046 und die sechs Nachträge, mit denen die Basis die Schemastände 74
bis 81 unverändert überstanden hat. Er steht hier im Wortlaut, weil die Herleitung des
Prüfprojekts 1046 und die Begründung seiner Flottengrößen weiterhin gebraucht werden: Das
Projekt selbst lebt in der Testdatenbank weiter und trägt die dritte Einfrierregel.

**Abgelöst wurde R7 durch `2026-09-16_R8_Heizkessel_Kaskade`** (Anwenderentscheid HK‑E‑1 vom
15.09.2026: Ein Heizkessel, den das Projekt führt, bekommt seinen Kaskadenplatz automatisch —
nachrangig). Drei der dreizehn Projekte verschieben sich dadurch (1007, 1008, 1046), zehn
bleiben byte-gleich; die Tabelle der Abweichungen steht im Abschnitt „Die Basis R8 im
Einzelnen" weiter unten.

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-11_R7_Speicherflotte/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046), **345 CSV**, **1 937 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Linux gegen `Kenndaten_Test.sqlite` (**Schemastand 73**;
die Datei selbst steht auf **Schemastand 81** — der Lauf gegen diese Basis bleibt davon
byte-gleich, siehe die Nachträge am Ende des Abschnitts). Gegen diese Basis hält
`.github/workflows/kern.yml` (1030, 1007, 1017, 1045, **1046**) jeden Push, `ios.yml` den
iZ6-Vergleich für 1030; das Gate der Orchestrierung zieht getrennt nach. Sie ist die **einzige**
Basis im Arbeitsbaum.

> **Anlass: der Anwenderentscheid SP‑O‑8, „Empfehlung".** Das Mehrspeicherkonzept hat mit
> `SpeicherEngine/Flotten*.cs`, den Controllern `SpeicherFlotten*Ctrl` und der Weiche in
> `SimulationControl.Stromspeicher.cs` einen **zweiten Speicherpfad** in den gewöhnlichen
> Projektlauf gelegt, den **kein Referenzprojekt betrat** — sie fahren alle die Einzelanlage
> über `StromspeicherSimCtrl.RechneAktiveVariante`. Eine stille Änderung an Verteilung,
> Reserve, Richtungswirkungsgrad oder Netzbilanz wäre in keinem Referenzlauf aufgefallen.
>
> **Das dreizehnte Projekt: 1046 „Prüfprojekt Speicherflotte"**, eine Tiefkopie von **1007**
> („Laurentiuskirche" — von den drei Projekten mit Strombedarf UND PV das mit der höchsten
> Bezugsspitze, 19,776 kW gegen 8,37 kW bei 1040 und 1045; und das einzige, das schon
> Stromspeicheranlagen führt, wodurch dasselbe Projekt mit ausgeschalteter Flotte den
> Einzelpfad rechnet). Darin eine **Flotte aus zwei Einheiten**: A mit 24 kWh an 10/12 kW
> (η 0,96/0,94, SoC 0,05–0,95, Reserve 2,4 kWh), B mit 16 kWh an 6/7 kW (η 0,93/0,91,
> SoC 0,10–0,90, Reserve 1,2 kWh). Betriebsziel **`PeakShaving`** gegen **16,0 kW**,
> Verteilung **`Kaskade`**, Netzladung frei, Batterieexport gesperrt. **Kein planendes Ziel** —
> `PvPlanung`, `Arbitrage` und `MultiUse` brauchen einen `IFlottenPlaner` und damit
> Google OR-Tools, die der plattformfreie Referenzlauf bewusst nicht einbindet.
>
> **Die zwölf übrigen Projekte sind byte-gleich zur Vorgängerbasis** — `diff -rq` je Projekt
> ohne einen einzigen Unterschied in 312 CSV. Der Schritt auf diese Basis ist reine
> **Erweiterung**: das neue Projekt in der Testdatenbank und 42 Skalare plus vier Ganglinien
> im Export, beide unter der Bedingung „die Flotte hat gerechnet".
>
> | Projekt 1046 | Flotte AUS (Einzelspeicher) | Flotte AN | Differenz |
> |---|---:|---:|---:|
> | Bezugsspitze [kW] | 19,7762 | **16,7428** | −3,0334 (−15,3 %) |
> | Netzbezug [kWh] | 50 538,68 | **51 611,01** | +1 072,33 (+2,1 %) |
> | Intervalle über 16 kW | 2 864 | **20** | — |
> | CSV / Skalare | 29 / 99 | **33 / 145** | +4 / +46 |
>
> Der höhere Netzbezug ist die Rechnung, nicht ein Fehler: Peak Shaving mit freigegebener
> Netzladung kauft unter dem Peak-Ziel und gibt später mit Wirkungsgradverlust wieder ab
> (122,42 kWh Umwandlungsverlust); bezahlt wird das mit dem Leistungspreis auf 3,03 kW weniger
> Bezugsspitze.
>
> **Die Flottenwege sind wirklich betreten:** A entlädt in 1 761 und lädt in 877 Intervallen,
> B in 1 177 bzw. 734, „nur B entlädt" in 1 152 (A steht an seiner unteren Grenze — das ist
> die **Kaskade**); beide SoC-Bänder voll ausgefahren, die **Peak-Reserve** nur bei
> tatsächlicher Peak-Überschreitung freigegeben (254 bzw. 672), gleichzeitiges Laden und
> Entladen innerhalb der Flotte: **0 Intervalle**.
>
> **Keine Lebensdauerkurve, mit Absicht:** `RainflowKurve` bleibt leer, der Miner-Schaden
> damit 0. Mit Kurve bricht die Auswertung ab, sobald eine Zyklustiefe außerhalb der
> Stützstellen liegt — ein Referenzprojekt, das bei einer harmlosen Änderung nicht abweicht,
> sondern abstürzt, wäre ein schlechtes Regressionsnetz. Der Skalar steht trotzdem, damit eine
> später hinterlegte Kurve eine ZAHL ändert und keinen SCHLÜSSEL hinzufügt (Muster `Em.*.CoKg`).
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich**,
> Toleranzvergleich **13/13 PASS** (3 777 497 Werte), Laufzeit 00:00:04; auch das Skript ist
> wiederholbar.
>
> ```bash
> python3 Referenzlaeufe/Skripte/pruefprojekt_1046_speicherflotte.py \
>   Referenzlaeufe/Kenndaten_Test.sqlite
>
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-11_R7_Speicherflotte
> ```
>
> Aufbau des Projekts, Herleitung der Größen — auch des Peak-Ziels 16,0 kW — und die drei
> Gegenproben im Wortlaut stehen im `protokoll.txt` der Basis.

> **Nachtrag: Schemastand 74 (Auftrag #178), die Basis bleibt.** Migrationsschritt **74**
> (`SCHRITT_74_SPEICHERAUSLEGUNG_STRICT`) baut `Tab_SpeicherAuslegung` als **STRICT**-Tabelle neu
> auf — `CREATE` unter Hilfsnamen, `INSERT … SELECT` mit **namentlich genannten** Spalten, `DROP`,
> `RENAME`, Index neu, alles in EINER Transaktion
> (`EPOS.Kern/Allgemein/Update/SpeicherAuslegungStrict.cs`); Spalten, Typen, Schlüssel und
> `idx_SpeicherAuslegung` bleiben wortgleich, und `SpeicherAuslegungCtrl.SQL_TABELLE` trägt das
> `STRICT` selbst, damit eine NEUE Datenbank die Tabelle gleich richtig anlegt. Stand der Datei:
> **Schemastand 74**, **70 803 456 Byte**, **119 Tabellen, davon 118 STRICT**, **25 Projekte**;
> die einzige Zeile — der Flottenstand `@Projektflotte` des Projekts 1046 — ist byte-gleich
> übernommen (SHA-256 vorher wie nachher `251c8554…add0d9`). **Der Referenzlauf ist 13/13
> byte-gleich gegen diese Basis** (Toleranzvergleich 13/13 PASS, 3 777 497 Werte): Kein
> Rechenwert ändert sich, die Basis wird nicht neu eingefroren, und **keine der drei
> Einfrierregeln ist berührt** — der Schritt kopiert Zeilen, er schreibt keine.

> **Nachtrag: Schemastand 75 (Auftrag #269), die Basis bleibt.** Migrationsschritt **75**
> (`SCHRITT_75_NUTZUNGSDAUER`) legt die Nutzungsdauertabelle `Tab_Nutzungsdauer` an
> (**STRICT** von der ersten Zeile an), sät ihre **28 Auslieferungszeilen** mit Richtwerten
> und Quellenangabe, hängt die nullbare Verweisspalte `NutzungsdauerID` an
> `Tab_KostenVorlagePosition` und `Tab_ProjektWerte` und ordnet **31 der 53
> Investitionspositionen** über ihren Namen einer Positionsart zu
> (`EPOS.Kern/Allgemein/Update/NutzungsdauerSchema.cs`). Stand der Datei: **Schemastand 75**,
> **70 762 496 Byte**, **120 Tabellen, davon 119 STRICT**, **25 Projekte**.
> **`Tab_ProjektWerte` bekommt die Spalte, aber keinen Wert** — alle 175 Projektzeilen sind
> Feld für Feld unverändert, und die 120 Vorlagenpositionen behalten ihre Altspalten
> wortgleich. **Der Referenzlauf ist 5/5 byte-gleich gegen diese Basis** (Toleranzvergleich
> 5/5 PASS, 1 586 257 Werte): Kein Rechenwert ändert sich, die Basis wird nicht neu
> eingefroren, und **keine der drei Einfrierregeln ist berührt** — der Schritt legt eine
> Tabelle an und füllt sie, er rührt keine gerechnete Größe an.

> **Nachtrag: Schemastand 76 (Auftrag #278), die Basis bleibt.** Migrationsschritt **76**
> (`SCHRITT_76_TRAEGERSATZ_EINDEUTIG`) entdoppelt `energy_project_settings` und legt darüber
> den eindeutigen Index `idx_EnergyProjectSettings_Traeger` auf
> `(ID_Projekt, ID_Energieträger)` — die Regel „ein Preis und ein Emissionssatz je
> Energieträger im Projekt" hält ab hier die Datenbank und nicht mehr allein die
> Anwendungslogik (`EPOS.Kern/Allgemein/Update/ProjektEnergietraegerEindeutig.cs`).
> **Auf der Messlatte gab es nichts zu entdoppeln:** Die 28 Zeilen der Tabelle verteilen
> sich auf 18 Projekte, kein Paar kommt zweimal vor, also entfernt der Schritt keine Zeile.
> Stand der Datei: **Schemastand 76**, **70 766 592 Byte**, **120 Tabellen, davon 119
> STRICT**, **25 Projekte**; `PRAGMA integrity_check` = `ok`, `PRAGMA foreign_key_check`
> bleibt leer. **Der Referenzlauf ist 5/5 byte-gleich gegen diese Basis**
> (Toleranzvergleich 5/5 PASS, 1 586 257 Werte): Kein Rechenwert ändert sich, die Basis
> wird nicht neu eingefroren, und **keine der drei Einfrierregeln ist berührt** — der
> Schritt legt einen Index an, er schreibt keinen Wert.

> **Nachtrag: Schemastand 77 (Auftrag #284), die Basis bleibt.** Migrationsschritt **77**
> (`SCHRITT_77_PUFFER_VOLUMENBEMESSUNG`) stellt die ausgelieferte Investitionsvorlage des
> Pufferspeichers von „je kWh Kapazität" auf die Bemessung je **Liter Gesamtvolumen** um —
> der Pufferspeicher führt keine kWh-Kapazität, die Art blieb dort ohne Bezugsgröße
> (`EPOS.Kern/Allgemein/Update/PufferspeicherBemessungVolumen.cs`). **Genau eine Zeile war
> betroffen:** `Tab_KostenVorlagePosition` 38 („Speicher", Vorlage 6), **Satz `NULL`** — die
> Vorlage gibt die Art vor, keine Zahl. Der Schritt fasst ausschließlich Zeilen **ohne**
> gepflegten Satz an; auf der Messlatte gab es keine mit Satz. `Tab_ProjektWerte` führt die
> Art am Pufferspeicher überhaupt nicht und bleibt unberührt, die Vorlagen der übrigen neun
> Komponenten ebenso (die PV-Position „Batteriespeicher" und die Stromspeicher-Position
> „Speicher" tragen sie weiter). Stand der Datei: **Schemastand 77**, **70 766 592 Byte**,
> **120 Tabellen, davon 119 STRICT**, **25 Projekte**; `PRAGMA integrity_check` = `ok`,
> `PRAGMA foreign_key_check` bleibt leer. **Der Referenzlauf ist 5/5 byte-gleich gegen diese
> Basis** (Toleranzvergleich 5/5 PASS, 1 586 257 Werte): Kein Rechenwert ändert sich, die
> Basis wird nicht neu eingefroren, und **keine der drei Einfrierregeln ist berührt** — der
> Schritt ändert eine Bemessungsart ohne Satz, er schreibt keinen Wert. Die beiden
> Pufferspeicher der Referenzprojekte 1007 und 1046 (Anlagenzeilen 11238 und 14941) sind
> nicht angefasst.

> **Nachtrag: Schemastand 78 (Auftrag #287), die Basis bleibt.** Migrationsschritt **78**
> (`SCHRITT_78_PV_BATTERIESPEICHER`) stellt die ausgelieferte Investitionsposition
> „Batteriespeicher" der **Photovoltaik** von „je kWh Kapazität" auf den **festen Betrag**
> um — die Photovoltaik führt keine kWh-Kapazität, ihre einzige Baugröße ist die
> installierte Leistung in kWp, und die Art blieb dort ohne Bezugsgröße
> (`EPOS.Kern/Allgemein/Update/PvVorlageBatteriespeicher.cs`). **Genau eine Zeile war
> betroffen:** `Tab_KostenVorlagePosition` 33 („Batteriespeicher", Vorlage 5), **Satz
> `NULL`** — die Vorlage gibt die Art vor, keine Zahl. Der Schritt fasst ausschließlich
> Zeilen **ohne** gepflegten Satz an; auf der Messlatte gab es keine mit Satz.
> `Tab_ProjektWerte` führt die Art an der Photovoltaik überhaupt nicht und bleibt unberührt,
> die Vorlagen der übrigen neun Komponenten ebenso — die Stromspeicher-Position „Speicher"
> trägt die Kapazitätsbemessung weiter, dort ist sie die Baugröße. Stand der Datei:
> **Schemastand 78**, **70 766 592 Byte**, **120 Tabellen, davon 119 STRICT**, **25
> Projekte**; `PRAGMA integrity_check` = `ok`, `PRAGMA foreign_key_check` bleibt leer. **Der
> Referenzlauf ist 5/5 byte-gleich gegen diese Basis** (135 von 135 Dateien, Toleranzvergleich
> 5/5 PASS, 1 586 257 Werte): Kein Rechenwert ändert sich, die Basis wird nicht neu
> eingefroren, und **keine der drei Einfrierregeln ist berührt** — der Schritt ändert eine
> Bemessungsart ohne Satz, er schreibt keinen Wert. Die Photovoltaik-Anlagen der
> Referenzprojekte sind nicht angefasst.

> **Nachtrag: Schemastand 80 (Auftrag #299), die Basis bleibt.** Zwei Migrationsschritte,
> ein Anwenderentscheid vom 16.09.2026. Schritt **79**
> (`SCHRITT_79_HEIZSTAB_JE_WP`) übergibt den Heizstab an die **Wärmepumpen-Anlagen**:
> Bis dahin las der Lauf ausschließlich den projektweiten Schalter
> `Tab_Einstellungen.WP_Heizstab`, obwohl die Zuheizleistung je Gerät in `Tab_WP.Heizung`
> steht. Die Übernahme setzt an jeder Anlage mit `ID_Type = 1` den Wert des
> Projektschalters ihres Projekts, danach entfernt der Schritt die Projektspalte
> (`EPOS.Kern/Allgemein/Update/HeizstabJeWaermepumpe.cs`) — der erste Schritt des
> SQLite-Zweigs, der eine **Spalte entfernt**. **Betroffen waren genau sieben
> Wärmepumpen-Anlagen** (Projekte 1007, 1019 ×2, 1023 ×2, 1024, 1046), alle von 0 auf 1;
> Anlagen anderer Arten sind unberührt (im Bestand stand dort ausnahmslos 0). Schritt
> **80** (`SCHRITT_80_WP_KATALOGVERWEIS`) gibt `Tab_WP` den nullbaren Katalogverweis
> `ID_Stamm` samt Index `Tab_WP_ID_Stamm` und trägt ihn bei **eindeutigem** Bezeichner
> nach (`EPOS.Kern/Allgemein/Update/WaermepumpeKatalogverweis.cs`): **29 von 29
> Projektkopien** haben ihn bekommen, keine blieb ohne. Kein Rechenweg liest die Spalte.
> Stand der Datei: **Schemastand 80**, **70 770 688 Byte**, **120 Tabellen, davon 119
> STRICT**, **25 Projekte**; `PRAGMA integrity_check` = `ok`, `PRAGMA foreign_key_check`
> bleibt leer. **Der Referenzlauf ist 5/5 byte-gleich gegen diese Basis** (135 von 135
> Dateien, Toleranzvergleich 5/5 PASS, 1 586 257 Werte): Die Übernahme erhält die
> Semantik Zeichen für Zeichen — 1007 und 1046 rechnen ihre Heizstabphase weiter
> (26,63 MWh Heizstabstrom), 1017 und 1030 weiter ohne. Die Basis wird nicht neu
> eingefroren, und **keine der drei Einfrierregeln ist berührt**.

> **Nachtrag: Schemastand 81 (Auftrag #301), die Basis bleibt.** Ein Migrationsschritt,
> ein Befund des Anwenders vom 16.09.2026. Schritt **81**
> (`SCHRITT_81_PROJEKTWERTE_LOESCHSCHUTZ`) stellt den Fremdschlüssel
> `Tab_ProjektWerte.StammID → Tab_Kostenfaktor(StammID)` von `ON DELETE CASCADE` auf
> **`ON DELETE RESTRICT`**; `ON UPDATE CASCADE` bleibt. Bis dahin riss **ein** gelöschter
> Katalogeintrag im Dialog „Administration Kostenfaktoren" **jede** Projektposition
> derselben `StammID` mit — quer durch alle Projekte und alle Gewerke, ohne dass die
> Rückfrage davon etwas nannte. In dieser Datei betraf das **46 der 65 löschbaren
> Katalogeinträge**; nachgerechnet: „Planung / Baunebenkosten" (`StammID` 114) nahm 6
> Positionen aus 5 Projekten mit, „Wärmepumpe (Aggregat)" (108) 4 aus 4 Projekten,
> darunter drei Wärmepumpen-Investitionen von je 13.000,00 €. Die Kaskade war nie
> gewollt: Dieselbe Beziehung an `Tab_KostenVorlagePosition.StammID` trägt gar keinen
> Fremdschlüssel. SQLite ändert keine Fremdschlüsselregel per `ALTER TABLE`, der Schritt
> ist deshalb der **zweite Tabellenneubau** des SQLite-Zweigs nach Schritt 74
> (`EPOS.Kern/Allgemein/Update/ProjektWerteLoeschschutz.cs`): Kopie unter Hilfsnamen,
> `INSERT … SELECT` über die 24 namentlich genannten Spalten, `DROP`, `RENAME`, die fünf
> Indizes neu — alles in **einer** Transaktion. Zwei Zugaben gegenüber Schritt 74: Der
> **AUTOINCREMENT-Stand** reist mit (sonst käme eine vergebene `ID` ein zweites Mal
> heraus), und die Umbenennung läuft unter `PRAGMA legacy_alter_table`, weil die Sicht
> `Abfrage_Kostenfaktoren` diese Tabelle liest. Stand der Datei: **Schemastand 81**,
> **70 770 688 Byte** (unverändert), **120 Tabellen, davon 119 STRICT**, **25 Projekte**;
> `Tab_ProjektWerte` führt weiterhin **175 Zeilen** mit demselben Zählerstand
> (`sqlite_sequence` = 101 600 605) und denselben fünf Indizes, `PRAGMA integrity_check`
> = `ok`, `PRAGMA foreign_key_check` bleibt leer, und
> `PRAGMA foreign_key_list('Tab_ProjektWerte')` nennt für `Tab_Kostenfaktor` jetzt
> `CASCADE` / `RESTRICT`. **Der Referenzlauf ist 5/5 byte-gleich gegen diese Basis** (135
> von 135 Dateien, Toleranzvergleich 5/5 PASS, 1 586 257 Werte): Der Schritt kopiert
> Zeilen, er rechnet nicht. Die Basis wird nicht neu eingefroren, und **keine der drei
> Einfrierregeln ist berührt**.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R8 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis" hat vom 16. bis zum 18.09.2026 die Basis R8 beschrieben — Anlass
(Anwenderentscheid `HK‑E‑1`), die Tabelle der drei verschobenen Projekte und die sieben
Nachträge, mit denen die Basis die Schemastände 83 bis 89 unverändert überstanden hat. Er steht
hier im Wortlaut, weil diese Nachträge die einzige Herleitung dafür sind, dass die Schemaschritte
83 bis 89 ergebnisneutral waren — die Schritte selbst leben im Bestand weiter.

**Abgelöst wurde R8 durch `2026-09-18_R9_Kesselbrennstoff`** (Anwenderentscheid vom 18.09.2026:
„Ja, neue Basis einfrieren", nach Befund `B-1` und Auftrag #331). **37 Werte von 3 882 737**
verschieben sich, alle in den drei Spalten `HeizkesselModul[0].Verbrauch`, `.Waermeproduktion`
und `.Brennstoff` der `aggregate.csv`; die 344 Ganglinien-CSV sind byte-gleich. Die Tabelle je
Projekt steht in [`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md), der
vollständige Vergleich im `protokoll.txt` von R9.

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-16_R8_Heizkessel_Kaskade/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023,
1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046), **357 CSV**, **2 057 Skalare**, gerechnet mit
dem plattformfreien `EPOS.Referenzlauf` auf Linux gegen `Kenndaten_Test.sqlite`
(**Schemastand 89**; die Basis selbst ist unter Stand 82 eingefroren worden und gilt
unverändert weiter — Schritt 83 faltet den Strom-Aufschlag in den Arbeitspreis, Schritt 84
zieht die Einspeisevergütung von der Trägerkarte in die Wirtschaftlichkeitsparameter um,
Schritt 85 entfernt die fünf Spalten, die beide ohne Leser zurückgelassen haben
(`Aufschlag_Modus`, `Aufschlag_Override`, `Verguetung_PV`, `Verguetung_BHKW`,
`Tab_ProjektWirtschaftlichkeit.Aufschlaege_Anwenden`); alle drei sind ergebnisneutral,
der Lauf ist vor und nach jedem Schritt byte-gleich. Bei Schritt 84 ist das doppelt
belegt: Die fünf Projekte des CI-Laufs hängen an `v_pv`/`v_bhkw` überhaupt nicht — eine
Gegenprobe mit beiden Sätzen auf 0 liefert dieselben Bytes. Schritt 85 schreibt gar
keinen Wert: Er entfernt nur, und keine der fünf Spalten trägt eine Rechengröße.
Schritt 86 legt an `Tab_StromspeicherVariante` die zwei Steuergrößen der
Lastspitzenkappung an (`PeakZiel_kW` REAL nullbar, `PeakZiel_Adaptiv` 0/1) und schreibt
keinen Wert; gelesen werden sie nur bei der Berechnungsart „Lastspitzenkappung", und die
führt im ganzen Bestand keine Variante — der Lauf ist auch nach diesem Schritt für alle
fünf CI-Projekte byte-gleich, Projekt 1046 mitsamt seiner Flotte.
Schritt 87 entdoppelt `Tab_Gesetzesparameter` und legt darüber den eindeutigen Index
`idx_Gesetzesparameter_Eindeutig` (`Schluessel`, `Klasse`, `JahrVon`); in der
Testdatenbank steht nichts zu entdoppeln — sie hat nie ein Programm gestartet, also auch
nie abgebrochen gesät —, der Index allein ändert keinen Wert. Derselbe Schritt schaltet
je Projekt jede aktive Speichervariante außer der mit der kleinsten `ID` ab; getroffen
hat das genau eine Zeile: Projekt **1026**, Anlage 11280, Variante **13** (Variante 10
bleibt aktiv). 1026 ist kein CI- und kein Basisprojekt, und `ReadAktiveVariante` nimmt
ohnehin die kleinste `ID` — der Lauf ist auch nach Schritt 87 für alle fünf CI-Projekte
byte-gleich.
Schritt 88 legt an `Tab_ProjektWirtschaftlichkeit` die Spalte `Stromst_Befreiung_Modus`
an (TEXT, `AUSWEIS`/`ERLOES`) und schreibt **keinen Wert**; NULL heißt AUSWEIS. Die
Rechenwirkung liegt nicht im Schema, sondern in der Vorgabe: § 9 Abs. 1 Nr. 3 StromStG
wird ab hier ausgewiesen statt als Erlös gebucht. Im ganzen Bestand bucht kein Lauf diese
Reihe — die Befreiung setzt Stundenreihen voraus, und die führt die Testdatenbank zu
keinem Projekt —, der Lauf ist auch nach Schritt 88 für alle fünf CI-Projekte
byte-gleich.
Schritt 89 legt an `Tab_Energieanlagen` die Spalte `KWKG_Kostenanteil` an (DOUBLE,
nullbar) und trägt danach — als erster KWKG-Schritt mit DML — die KWKG-Vorgaben des
Projekts in jede BHKW-Anlagenzeile nach, die an der betreffenden Stelle leer ist (Sätze,
Kontingent, Jahresdeckel, Anlagenart, Tatbestand, Kostenanteil, Stichtag,
Inbetriebnahme). Erst danach gibt der Rechenweg den Rückfall Anlage → Projekt auf;
§ 7 und § 8 KWKG bemessen Satz und Kontingent an der EINZELNEN Anlage. Getroffen hat der
Schritt genau ein Projekt: **1030** mit seinen zwei Modulen (Satz Eigen 4,00 ct/kWh,
Satz Einspeisung 8,00 ct/kWh, Kontingent 30.000 Vbh, Stichtag 01.09.2026, Inbetriebnahme
01.03.2027) — kein anderes Projekt der Testdatenbank führt KWKG-Vorgaben, und keine
einzige Anlagenzeile trug vorher eine eigene Angabe. **Ergebnisneutral, gemessen:** Der
KWK-Zuschlag von 1030 im Jahr 1 beträgt vor und nach dem Schritt 7.315,956634 €, der
Kapitalwert −21.895.377,275113 € (Szenario Erwartet, flache Stundenreihen aus dem
gebuchten Lauf); die Gegenprobe ohne den Datenschritt liefert 0,00 € Zuschlag und
−21.954.815,753214 € Kapitalwert. Die Basis führt ohnehin keine Geldgröße — der Lauf ist
auch nach Schritt 89 für alle fünf CI-Projekte byte-gleich). Gegen
diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017,
1045, **1046**) jeden Push, `ios.yml` den iZ6-Vergleich für 1030; das Gate der Orchestrierung
zieht getrennt nach. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Die Netzladung der Preissteuerung zählt im Netzbezug — die Basis bleibt unverändert,
> belegt.** Der Projektlauf schlägt die Netzladung der Berechnungsart „Arbitrage" jetzt auf
> `Rest_Strombedarf` auf (Netzwirkung `Entladung − Netzladung`, wie die Lastspitzenkappung
> ihre eigene liefert). **Kein Basisprojekt ist betroffen:** Alle neun Speichervarianten der
> Testdatenbank — Projekte 1007 (vier), 1017 (eine), 1046 (vier) — tragen
> `Berechnungsart = 'Dauernutzung'`, `Betriebsart = 'Grünstrom'`, `Netzentladung = 0` und
> `A_Netzlade = 0`; die übrigen zehn Basisprojekte führen überhaupt keine Speichervariante.
> Die Dauernutzung lädt nur aus Überschuss und bekommt deshalb keine Netzwirkungsreihe — der
> Lauf über alle dreizehn Projekte meldet `GESAMT: PASS` (3 882 737 Werte) und ist im
> Byte-Vergleich ohne einen einzigen Unterschied in den 357 CSV. Die Einfrierregel greift
> nicht.

> **Anlass: der Anwenderentscheid HK‑E‑1 vom 15.09.2026, „Umsetzen".** Ein Wärmeerzeuger
> rechnet nur, wenn seine Technologie in einem der vier Plätze `Tab_Einstellungen.Tool_1..4`
> steht. Einen Heizkessel anzulegen legte aber keinen Platz an — die Anlage stand im Projekt
> und wurde still übergangen. Seit diesem Entscheid zieht das Lesen der Konfiguration den
> Platz nach (`KonfigurationCtrl.HeizkesselNachziehen`), **nachrangig** am hinteren Ende der
> Kaskade und in die Projektdaten geschrieben; die vorhandene Umordnung der
> Simulationskonfiguration überschreibt diese Vorgabe dauerhaft. **Nur der Heizkessel** —
> Wärmepumpe, Solarthermie, BHKW, Photovoltaik und Stromspeicher ohne Platz werden weiterhin
> nur gemeldet.
>
> **Drei Projekte verschieben sich, zehn sind byte-gleich zur Vorgängerbasis.** Betroffen
> ist genau, wer eine Heizkesselanlage führt und keinen Kesselplatz hatte: **1007**, **1008**
> und **1046**. Die übrigen zehn Projekte sind `diff -rq` ohne einen einzigen Unterschied.
>
> | Größe | 1007 | 1008 | 1046 |
> |---|---:|---:|---:|
> | `Sim.Restwaerme` [MWh] | 6,1313 → **0** | 2,7781 → **0** | 6,1313 → **0** |
> | `Heizkessel.Waermeproduktion` [MWh] | — → **6,13** | — → **2,78** | — → **6,13** |
> | `Heizkessel.Gasverbrauch` [MWh] | — → **15,47** | — → **12,02** | — → **15,47** |
> | `Heizkessel.Waermebedarfsdeckung` [%] | — → **10,73** | — → **5,07** | — → **10,73** |
> | `Em.Kessel.Co2T` [t/a] | — → **3,7136** | — → **2,8851** | — → **3,7136** |
> | `Sim.bSimulationKessel` | False → **True** | False → **True** | False → **True** |
> | CSV / Skalare | 29/99 → **33/139** | 21/101 → **25/141** | 33/145 → **37/185** |
>
> **Die Richtung stimmt fachlich:** Der Kessel steht am hinteren Ende der Kaskade und nimmt
> nur, was die vorderen Stufen übrig lassen. Deshalb sinkt die ungedeckte Restwärme auf null,
> und Kesselwärme, Brennstoff und Emissionen kommen hinzu — **kein anderer Erzeuger verliert
> Deckung**: Wärmepumpe, Solarthermie und Photovoltaik rechnen in allen drei Projekten Wert
> für Wert wie zuvor. Der niedrige Jahresnutzungsgrad (39,6 % bzw. 23,1 %) ist die Rechnung
> eines Kessels, der nur wenige Spitzenstunden fährt und den Rest des Jahres
> Betriebsbereitschaft vorhält.
>
> **Zwei weitere Änderungen dieser Welle verschieben die Basis NICHT** und sind deshalb
> getrennt gemessen: Die Bereinigung der zwei Probierpuffer „test" (2 Ltr, 4 000 €) in 1007
> und 1046 ist **13/13 byte-gleich** — sie hängen an keiner Zeile von `Z_ProjektPufferSp` und
> rechnen im hydraulischen Weg nicht mit. Und im Größenlauf der Speicherflotte wurde **nichts
> geändert**; die dort offene Frage nach den Produktparametern ist durch den Entscheid vom
> 15.09.2026 zur Gerätesuche bereits beantwortet.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich**,
> Toleranzvergleich **13/13 PASS** (3 882 737 Werte), Laufzeit 00:00:06.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-16_R8_Heizkessel_Kaskade
> ```
>
> Ablauf, Warnungen und Ausstattung je Projekt stehen im `protokoll.txt` der Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Was in diesen Protokollen steht — und was nicht

- **Steht darin:** der Codestand, der Schemastand, die feste Projektliste, der Selbstvergleich,
  der Vergleich gegen die Vorgängerbasis Datei für Datei, jede gewollte Abweichung mit ihrer
  Ursache und die Gegenprobe, die sie belegt. Die drei Einfrierregeln der Testdatenbank
  (Em‑9.8‑Q4 Emissionsfaktoren, W6‑B‑5‑Q3 PV-Modulkoeffizienten, SP‑O‑8 Flottenparameter)
  stützen sich auf die Herleitungen in `2026-09-07_R5_Zahlenrand/protokoll.txt` und
  `2026-09-07_R6_PvKoeffizienten/protokoll.txt`.
- **Steht nicht darin:** die Ganglinien und Kennzahlen selbst. Ein Vergleich gegen eine dieser
  Basen ist nicht mehr möglich.

**Diese Protokolle sind Geschichte, keine Regelquelle.** Sie erklären, wie die heutige Basis
geworden ist; was gilt, steht in [`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md)
und in der Wurzel-[`CLAUDE.md`](../../../CLAUDE.md). Die sechs Verweise, die aus fünf dieser
Protokolle auf ihren damaligen Ablageort zeigen (viermal
`WindowsFormsApplication1/Allgemein/Simulation/…`, zweimal ein Konzept in der
Repository-Wurzel), sind **bewusst nicht nachgezogen** — ein Protokoll darf nennen, worauf es
sich damals bezogen hat. Einer davon trifft nach dem Umzug #241 zufällig wieder; die anderen
fünf führt die Wache `EPOS.Kern.Tests/DokumentationLinkWacheTests` als vorbestehende Lücken.

## Die Basis R9 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat vom 18. bis zum 19.09.2026 die Basis R9
beschrieben — Anlass (Befund `B-1`), die Tabelle der 37 Abweichungen und die
Nachträge zu den Schemaständen 90 bis 97, mit denen die unter Stand 89 eingefrorene
Basis ergebnisneutral geblieben ist. Er steht hier im Wortlaut, weil diese Nachträge
die Ergebnisneutralität der Schritte 90 bis 97 belegen.

**Abgelöst wurde R9 durch `2026-09-19_R10_BhkwWirkungsgrad`** (Anwenderentscheid vom
19.09.2026, BH1‑O1: Der BHKW-Wirkungsgrad ist ein Faktor, der Katalog wird
vereinheitlicht und die Basis neu eingefroren). Zwei der dreizehn Projekte verschieben
sich dadurch (1018 und 1030), elf bleiben byte-gleich; die Tabelle der Abweichungen
steht im Abschnitt „Aktuelle Basis“ von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-18_R9_Kesselbrennstoff/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023,
1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046), **357 CSV**, **2 057 Skalare**, gerechnet mit
dem plattformfreien `EPOS.Referenzlauf` auf Linux gegen `Kenndaten_Test.sqlite`
(**Schemastand 97** — die Basis ist unter Stand 89 eingefroren; die Testdatenbank steht auf
Stand 97, und die Schritte 90 bis 97 sind ergebnisneutral, gemessen). Schritt 92 legt die
Spalte `Tab_ProjektWirtschaftlichkeit.ID_Referenzprojekt` an und schreibt keinen Wert — NULL
heißt Stamm, also genau die Referenz jeder Bestandsrechnung. Schritt 93 legt
`Tab_ProjektPhotovoltaik.Uebernahme_Stamm` an und leitet die Vergütungswahl aus dem Bestand
ab; die Testdatenbank führt in dieser Tabelle **keine Zeile**, beide DML des Schritts fassen
dort nichts an. Schritt 94 trägt kein DDL: Er stellt die drei Hilfsstrom-Positionen der
Katalogvorlage „Standard" (BHKW, Heizkessel, Wärmepumpe) von `PROZENT_ENDENERGIEKOSTEN` auf
`PROZENT_ENDENERGIEBEDARF` um. Angefasst wird allein `Tab_KostenVorlagePosition`;
`Tab_ProjektWerte` bleibt Zeile für Zeile unverändert, und keine Referenzrechnung liest eine
Vorlagenposition — die fünf CI-Projekte rechnen **byte-gleich**, gemessen.
Schritt 95 ist reines DDL: Er legt `Gegenstrahlung`, `Luftfeuchte` und `Bedeckungsgrad` an
`Tab_Solar` und `Tab_Solar_STAMM` an sowie `Quelle` und `Importdatum` an `Tab_Klimaregion`
und `Tab_Klimaregion_STAMM` — zehn nullbare Spalten, **kein DML**. In der Testdatenbank
bleiben alle zehn in jeder Zeile NULL (280 320 Stundenwerte, 32 Regionen), und kein
Rechenweg liest eine von ihnen; die fünf CI-Projekte rechnen **byte-gleich**, gemessen.
Schritt 96 rüstet in 28 Projekttabellen 29 Fremdschlüssel auf `Tab_Projekt` nach
(Tabellenneubau, `ON DELETE CASCADE`), heilt vorher 1 466 Zeilen ohne Projektbezug über
ihren Elternsatz (1 446 Kennlinien in `Tab_Kenndaten`, 20 in `Tab_Stromverbrauchertyp`) und
entfernt 129 verwaiste Zeilen samt 131 557 abhängigen (Ganglinien- und Ergebnisdetails ohne
Projekt). Keine Referenzrechnung liest eine der entfernten Zeilen, kein Rechenweg wertet
einen Fremdschlüssel aus; die fünf CI-Projekte rechnen **byte-gleich**, gemessen.
Schritt 97 ist reines DDL: Er legt `Szenario` und `Bezugsjahr` an `Tab_Klimaregion` und
`Tab_Klimaregion_STAMM` an — vier nullbare Spalten, **kein DML**. In der Testdatenbank
bleiben beide in jeder Zeile NULL (32 Regionen im Katalog, die Projektkopien dazu), und kein
Rechenweg liest eine von ihnen; die fünf CI-Projekte rechnen **byte-gleich**, gemessen.
Schritt 90 räumt hinter Schritt 89 auf und hat **zwei Teile**. Der **DDL-Teil** entfernt aus
`Tab_ProjektWirtschaftlichkeit` die sechs KWKG-Spalten `KWKG_Bonus`,
`KWKG_Bonus_Einspeisung`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel`,
`KWKG_Tatbestand` und `KWKG_Anlagenart`; seit Schritt 89 und dem Umbau des Ersatzwegs auf
eine leistungsgewichtete virtuelle Gesamtanlage liest sie kein Rechenweg mehr.
`KWKG_Kostenanteil` bleibt samt Dialogfeld stehen (Anwenderentscheid), ebenso Stichtag,
Inbetriebnahme, Pauschalmodus und Abschlag Negativstunden. **Ergebnisneutral, gemessen
auf dem Ersatzweg** (Projekt 1030, Modulzuordnung absichtlich verstellt): Der KWK-Zuschlag
im Jahr 1 beträgt vor und nach dem Umbau **7.315,948722 €**, der Kapitalwert
**−21.895.377,339395 €**, und die volle Reihe t = 1…20 ist zahlengleich. **Eine Ausnahme
ist abgenommen:** Leert man das Vbh-Kontingent an Projekt UND Anlagen, rechnete der
Ersatzweg bisher still mit dem Feldvorgabewert 30.000 h und lieferte dieselben
7.315,948722 €; jetzt leitet er 0 h mit Begründung ab — 0,00 € Zuschlag und
−21.954.815,753214 € Kapitalwert. Der **DML-Teil** entfernt aus `Tab_ProjektWerte` die
Nullzeilen der drei nicht anlagenfähigen Erfassungsgruppen (Wärmezentrale, Bauliche
Anlagen, Stromeinspeisung): Hauptkomponentenzeilen der früheren Kostenmaske, Gruppe
„Allgemein", ausnahmslos 0,00. Getroffen hat er **elf Zeilen** — 1018 eine, 1019 sechs,
1031 eine, 1032 drei (Kategorien 1 und 2); eine Gruppe mit irgendeiner Position mit Wert
bleibt vollständig stehen. Keines der vier Projekte ist CI- oder Basisprojekt, und die
Basis führt keine Kostengröße — der Lauf ist auch nach Schritt 90 für alle fünf
CI-Projekte byte-gleich.
**Schritt 91** nimmt die siebte und letzte KWKG-Projektspalte: `KWKG_Kostenanteil` aus
`Tab_ProjektWirtschaftlichkeit`, samt ihrem Dialogfeld in Gruppe 2 des
BHKW-Wirtschaftlichkeitsdialogs. § 8 Abs. 2/3 KWKG leitet das Vbh-Kontingent aus dem
Kostenanteil **der Anlage** ab (`Tab_Energieanlagen.KWKG_Kostenanteil`, Schritt 89); der
Projektwert hatte seit dem Umbau des Ersatzwegs keinen Rechenleser mehr. **Kein DML** —
Schritt 89 hat den Wert einmalig in jede BHKW-Anlagenzeile übertragen, die dort leer war.
`Tab_ProjektWirtschaftlichkeit` führt danach noch vier KWKG-Spalten: `KWKG_Stichtag`,
`KWKG_Inbetriebnahme`, `KWKG_Abschlag_Negativ` und `KWKG_Pauschalmodus`.
**Ergebnisneutral, gemessen:** Projekt 1030 rechnet den KWK-Zuschlag im Jahr 1 unverändert
mit **7.315,948722 €** und den Kapitalwert mit **−21.895.377,339395 €**; die Basis führt
keine KWKG-Projektgröße — der Lauf ist auch nach Schritt 91 für alle fünf CI-Projekte
byte-gleich.
**Ohne eigenen Schritt** trägt die Testdatenbank zusätzlich die Spalte
`Nachweis_Json` an `Tab_ErgebnisWirtschaftlichkeit` — eine **Konservenspalte**: Diese
Ergebnistabelle ist keine Schematabelle, sie entsteht und wächst erst beim ersten
Programmlauf über `WirtschaftlichkeitCtrl.SpalteSicher`. Der SQL-Dialektprüfer löst das
INSERT des Ergebnisses aber gegen genau diese Datei auf und meldete ohne die Spalte eine
Fundstelle, die in der Anwendung keine ist; angelegt wird sie deshalb von
`Werkzeuge/Testdatenbankschema` (leer, kein Wert, ohne eigene `Zielversion`), und der Lauf
ist auch danach für alle fünf CI-Projekte byte-gleich. Gegen
diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017,
1045, **1046**) jeden Push, `ios.yml` den iZ6-Vergleich für 1030; das Gate der Orchestrierung
zieht getrennt nach. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: der Anwenderentscheid vom 18.09.2026, „Ja, neue Basis einfrieren".** Auftrag #331
> hat zum Befund **`B-1`** der Wirtschaftlichkeit den Brennstoffverbrauch des Heizkessels aus
> dem Simulationslauf in die **Modulzeile** nachgezogen: Die drei Spalten
> `HeizkesselModul[0].Verbrauch`, `.Waermeproduktion` und `.Brennstoff` trägt jetzt der Lauf,
> statt sie leer zu lassen. Herleitung, Rückfälle, Warnung und die benannte Ausnahme des
> Elektrokessels stehen im Protokoll
> [`B-1_Kesselbrennstoff_Modulzeile_Protokoll.md`](../Protokolle/Reporting/B-1_Kesselbrennstoff_Modulzeile_Protokoll.md).
>
> **37 Abweichungen von 3 882 737 Werten — alle in denselben drei Spalten, keine unerklärte.**
> Elf Projekte tragen drei davon, die beiden Elektrokesselprojekte **1017** und **1024** nur die
> letzten beiden: Ihr Verbrauch bleibt 0, weil der Elektrokessel die benannte Ausnahme des
> Rechenwegs ist. 11 × 3 + 2 × 2 = 37.
>
> | Projekt | `…Brennstoff` | `…Verbrauch` [MWh/a] | `…Waermeproduktion` [MWh/a] |
> |---|---|---:|---:|
> | 1007, 1046 | → Gas | 0 → **15,47** | 0 → **6,13** |
> | 1008 | → Gas | 0 → **12,02** | 0 → **2,78** |
> | 1017 | → Strom | 0 (Ausnahme) | 0 → **8,91** |
> | 1018 | → Gas | 0 → **16,76** | 0 → **16,76** |
> | 1023 | → Gas | 0 → **78,64** | 0 → **66,61** |
> | 1024 | → Strom | 0 (Ausnahme) | 0 → **47,44** |
> | 1030 | → Gas | 0 → **5 403,10** | 0 → **5 403,10** |
> | 1039 | → Gas | 0 → **225,04** | 0 → **220,54** |
> | 1040, 1045 | → Gas | 0 → **16,19** | 0 → **16,19** |
> | 1041 | → Gas | 0 → **133,33** | 0 → **133,33** |
> | 1042 | → Gas | 0 → **13,81** | 0 → **13,53** |
>
> **Was byte-gleich geblieben ist:** die **344 Ganglinien-CSV** — keine Zeitreihe hat sich
> bewegt — und in den Skalaren jede andere Größe: Anlagensummen, Emissionen, Puffer,
> Speicherflotte, Wirtschaftlichkeit. Je `aggregate.csv` sind genau drei bzw. zwei Zeilen
> anders; keine kommt hinzu, keine fällt weg, die Skalarzahl bleibt Projekt für Projekt
> dieselbe.
>
> **Die Gegenprobe hält in allen dreizehn Projekten:**
> `HeizkesselModul[0].Verbrauch` = `Heizkessel.Gasverbrauch` und
> `HeizkesselModul[0].Waermeproduktion` = `Heizkessel.Waermeproduktion`. Der Modulwert ist
> nicht neu gerechnet, sondern der Anlagenwert des Laufs an der Stelle, an der die
> Wirtschaftlichkeit ihn liest.
>
> **Weder die Testdatenbank noch das Schema sind dafür angefasst worden**, und keine der drei
> Einfrierregeln ist berührt.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich**,
> Laufzeit 00:00:06.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-18_R9_Kesselbrennstoff
> ```
>
> Ablauf, Warnungen und Ausstattung je Projekt sowie der vollständige Vergleich R8 → R9 stehen
> im `protokoll.txt` der Basis.

> **Die Vorgängerbasis `2026-09-16_R8_Heizkessel_Kaskade`** ist mit dieser Einfrierung aus dem
> Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zu `HK‑E‑1`, den Nachträgen zu den
> Schemaständen 83 bis 89 und dem Beleg zur Netzladung der Preissteuerung steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](../Referenzbasen/LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R10 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat vom 19. bis zum 22.09.2026 die Basis R10
beschrieben — Anlass (BH1‑O1), die Tabelle der 13 Abweichungen und die Nachträge zu den
Schemaständen 99 und 100, mit denen die unter Stand 98 eingefrorene Basis
ergebnisneutral geblieben ist. Er steht hier im Wortlaut, weil diese Nachträge die
Ergebnisneutralität der Schritte 99 und 100 belegen.

**Abgelöst wurde R10 durch `2026-09-22_R11_Bestandsbefunde`** (Stufe GB der
Gebäudesimulation, vom Anwender am 22.09.2026 freigegeben: Bestandsbefunde des
Tagesbilanz-Wegs und die Korrektur der `Bauweise` von Gebäude 10576 in der
Testdatenbank). Eines der dreizehn Projekte verschiebt sich dadurch (1008), zwölf bleiben
byte-gleich; die Tabelle der Abweichungen steht im Abschnitt „Aktuelle Basis“ von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-19_R10_BhkwWirkungsgrad/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023,
1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046), **357 CSV**, **2 057 Skalare**, gerechnet mit
dem plattformfreien `EPOS.Referenzlauf` auf Linux gegen `Kenndaten_Test.sqlite`
(gerechnet auf **Schemastand 98**). Anders als ihre Vorgängerinnen ist diese Basis **auf ihrem
eigenen Schemastand gerechnet**: Schritt 98 ändert Werte, die der Rechenweg liest, also gibt
es hier nichts nachzutragen — was die Schritte 90 bis 97 an dieser Stelle zu erklären hatten,
steht beim Abschnitt „Die Basis R9 im Einzelnen" in
[`Dokumentation/ueberholt/Referenzbasen/`](../Referenzbasen/LIESMICH.md).

**Die Testdatenbank steht inzwischen auf Schemastand 99** — reines Nachziehen: Schritt 99
legt `Wirkungsgrad_el` und `Wirkungsgrad_th` an `Tab_BHKW_STAMM` und `Tab_BHKW` an und teilt
den gepflegten Gesamtwirkungsgrad im Verhältnis der Leistungen auf (78 Katalogsätze und 6
Projektkopien; ein Satz ohne Gesamtwirkungsgrad bleibt benannt ausgewiesen). **Die Spalte
`Wirkungsgrad` bleibt unverändert**, und nur sie liest `SimulationBHKW` — die **Referenzbasis
R10 bleibt**, der Lauf der fünf CI-Projekte gegen sie ist **PASS und byte-gleich gemessen**
(1 656 417 Werte, 143 Dateien).

**Und inzwischen auf Schemastand 100** — wieder reines Nachziehen: Schritt 100 nimmt
**41 Fremdschlüsselspalten in 25 Tabellen** ihre Vorgabe `DEFAULT 0`. Eine Elterntabelle mit
einer Zeile 0 gibt es nicht, also war die Vorgabe eine Falle: Ein Schreibweg, der eine solche
Spalte weglässt, bekam still die 0 und damit eine Fremdschlüsselmeldung weit weg von der
Ursache. `NOT NULL` bleibt stehen, wo es steht. **Kein Wert ist angefasst** — keine Zeile trug
den Wert 0, Zeilenzahlen, Ids, `sqlite_sequence`-Stände, Indizes, Sichten und
`integrity_check` sind vor und nach dem Lauf gleich —, die **Referenzbasis R10 bleibt**, und
der Lauf der fünf CI-Projekte gegen sie ist **PASS und byte-gleich gemessen** (1 656 417
Werte, 143 Dateien).
**Ohne eigenen Schritt** trägt die Testdatenbank zusätzlich die Spalte `Nachweis_Json` an
`Tab_ErgebnisWirtschaftlichkeit` — eine **Konservenspalte**: Diese Ergebnistabelle ist keine
Schematabelle, sie entsteht und wächst erst beim ersten Programmlauf über
`WirtschaftlichkeitCtrl.SpalteSicher`. Der SQL-Dialektprüfer löst das INSERT des Ergebnisses
aber gegen genau diese Datei auf und meldete ohne die Spalte eine Fundstelle, die in der
Anwendung keine ist; angelegt wird sie deshalb von `Werkzeuge/Testdatenbankschema` (leer, kein
Wert, ohne eigene `Zielversion`). Gegen diese Basis hält `.github/workflows/kern.yml` (1030,
1007, 1017, 1045, **1046**) jeden Push, `ios.yml` den iZ6-Vergleich für 1030; das Gate der
Orchestrierung zieht getrennt nach. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: der Anwenderentscheid vom 19.09.2026, „Katalog vereinheitlichen + Basis neu"
> (BH1‑O1).** `Tab_BHKW[_STAMM].Wirkungsgrad` ist der GESAMTwirkungsgrad als **Faktor** — so
> sagt es die Maske, so rechnet `SimulationBHKW.Auswertung`
> (`Verbrauch = (Wärme + Strom) / Wirkungsgrad`). Ein Teil des Katalogs trug dort einen
> **Prozentwert**, und zwar den des **elektrischen** Wirkungsgrads; geteilt wurde dann durch
> 29,5 statt durch 0,92, und Brennstoff, Gasspitze, Emissionen und Brennstoffkosten des BHKW
> fielen um rund **Faktor 32** zu klein aus. **Schemaschritt 98** rechnet den Bestand um —
> `(Ptherm + Pel) · Wirkungsgrad / 100 / Pel`, vier Stellen, übernommen nur im Band
> **[0,5; 1,05]**. Herleitung, Zählung und die benannt ausgewiesenen Zeilen stehen im Protokoll
> [`BW1_BhkwWirkungsgrad_Protokoll.md`](../Protokolle/Update/BW1_BhkwWirkungsgrad_Protokoll.md).
>
> **13 Abweichungen von 3 882 737 Werten — alle in zwei Projekten, alle am BHKW, keine
> unerklärte.** Betroffen ist genau, wer ein umgerechnetes Modul fährt: **1018** („BHKW Test
> München", EC‑POWER XRGI 15, 29,5 → 0,9216) und **1030** („Referenz BHKW-Kaskade", EC‑POWER
> XRGI 9, 29,3 → 0,9474; das zweite Modul der Kaskade trug schon einen Faktor). Die elf
> übrigen Projekte sind **byte-gleich**.
>
> | Projekt | `BHKW.Gasverbrauch` [MWh/a] | `Em.Bhkw.Co2T` [t/a] | `Em.Bhkw.NoxKg` [kg/a] |
> |---|---:|---:|---:|
> | 1018 | 1,56 → **49,94** | 0,374 → **11,986** | 0,172 → **5,494** |
> | 1030 | 1 048,29 → **1 241,55** | 251,589 → **297,971** | 115,312 → **136,570** |
>
> Dazu je Projekt `Em.Bhkw.So2Kg` und `Em.Bhkw.StaubKg` im selben Verhältnis und die
> Modulzeilen `BHKWModul[i].Verbrauch` — sie sind die anteilige Aufteilung des Anlagenwerts
> nach Wärmeproduktion (`ErgebnisCtrl`) und wandern deshalb im Gleichschritt mit ihm: eine
> Zeile in 1018, zwei in 1030. 6 + 7 = 13.
>
> **Was byte-gleich geblieben ist:** **alle 357 Ganglinien- und Vektordateien** — keine
> Zeitreihe hat sich bewegt, auch nicht in 1018 und 1030; je Projekt weicht höchstens die
> `aggregate.csv` ab. Und in den Skalaren jede andere Größe: Wärme- und Stromproduktion des
> BHKW, Betriebsstunden, Vollbenutzungsstunden, Kessel, Puffer, Speicherflotte, Wirtschaft-
> lichkeit. Keine Zeile kommt hinzu, keine fällt weg, die Skalarzahl bleibt Projekt für
> Projekt dieselbe.
>
> **Die Gegenprobe:** 1018 fährt ein Modul mit 1 016 Volllaststunden; (30,8 + 14,5) kW ×
> 1 016 h / 0,9216 = **49,9 MWh/a** — genau der Wert, den der Lauf jetzt ausweist, und genau
> das Bild, das der Anwender erwartet hatte. Der Rechenweg ist dafür **nicht angefasst**
> worden: `SimulationBHKW` steht unverändert, geändert haben sich allein die Daten.
>
> **Die Testdatenbank ist angefasst worden** (Schemastand 97 → 98, 34 Katalogsätze und 3
> Projektkopien umgerechnet); **keine der drei Einfrierregeln ist berührt** — weder ein
> Emissionsfaktor noch ein PV-Modulkoeffizient noch der Flottenstand des Projekts 1046.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich**,
> Laufzeit 00:00:05.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-19_R10_BhkwWirkungsgrad
> ```
>
> Ablauf, Warnungen und Ausstattung je Projekt stehen im `protokoll.txt` der Basis.

> **Die Vorgängerbasis `2026-09-18_R9_Kesselbrennstoff`** ist mit dieser Einfrierung aus dem
> Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zum Befund `B-1` und den Nachträgen
> zu den Schemaständen 90 bis 97 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](../Referenzbasen/LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R11 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat vom 22. bis zum 23.09.2026 die Basis R11 beschrieben —
Anlass (Stufe GB), die Tabelle der Abweichungen von 1008 und die Nachträge zu den
Schemaständen 101 bis 103, mit denen die unter Stand 100 eingefrorene Basis ergebnisneutral
geblieben ist. Er steht hier im Wortlaut, weil diese Nachträge die Ergebnisneutralität der
Schritte 101 bis 103 belegen; die Verweise sind auf diesen Ort umgestellt.

**Abgelöst wurde R11 durch `2026-09-23_R12_Gebaeudemodell`** (Schlusswelle G1 + G2 der
Gebäudesimulation, vom Anwender am 23.09.2026 beauftragt: VDI 6007 ist das Vorgabemodell
aller Gebäude, Projekt 1040 bleibt als einziges Referenzprojekt auf dem Tagesbilanz-Weg).
Elf der dreizehn Projekte verschieben sich, 1030 (ohne Gebäude) und 1040 bleiben
byte-gleich; die Tabelle steht im Abschnitt „Aktuelle Basis“ von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-22_R11_Bestandsbefunde/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023,
1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046), **357 CSV**, **2 057 Skalare**, gerechnet mit
dem plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite`
(Schemastand **100**). Dass Windows hier dieselben Bytes liefert wie Linux, ist gemessen: Der
Lauf des unveränderten Standes auf diesem Rechner war gegen die Vorgängerbasis R10 (auf Linux
gerechnet) in allen 357 Dateien byte-gleich. Gegen diese Basis hält
`.github/workflows/kern.yml` (1030, 1007, 1017, 1045, 1046) jeden Push, `ios.yml` den
iZ6-Vergleich für 1030. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Die letzte reine Bestandsbasis.** Diese Basis rechnet alle Gebäude ausschließlich auf
> dem Tagesbilanz-Weg. Mit der Stufe **G1 + G2** der Gebäudesimulation rechnen alle
> dreizehn Projekte stündlich nach VDI 6007, und die Basis wird vollständig neu
> eingefroren. Bis zu diesem Merge ist sie die aktuelle Basis: Jeder Schritt dazwischen
> (M2, M3, die Trennung der Rechenwege G1.0) muss gegen sie `GESAMT: PASS` melden, G1.0
> ausdrücklich **byte-gleich**, und die Rückweg-Probe des Tagesbilanz-Wegs
> (Arbeitskopie mit `Gebaeude_Modell = 'TAGESBILANZ'`) rechnet gegen sie. Danach wandert sie
> mit ihrem Protokoll nach `Dokumentation/ueberholt/Referenzbasen/` wie jede Basis vor ihr;
> einen zweiten Basisordner gibt es nicht
> ([Umsetzungskonzept Gebäudesimulation](../../aktuell/Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
> Kapitel 1.8 und 4).

> **Anlass: die Stufe GB der Gebäudesimulation** — die Bestandsbefunde des Tagesbilanz-Wegs
> **vor** G1 und vor der Verschiebung des Altwegs in sein eigenes Modul (Entscheide E4, E20
> und E28; vom Anwender am 22.09.2026 freigegeben). Fünf Änderungen, eine davon an den Daten:
>
> 1. **Die Vortemperatur ist Instanzzustand.** `BhkwPlan._prevRoomTemp` war statisch und
>    trug die Raumtemperatur über Gebäude, Projekte und Läufe eines Prozesses; sie lebt jetzt
>    in einem `Tagesbilanzzustand` je `SimulationWaermebedarf`, je Gebäude zurückgesetzt.
> 2. **Warnungen statt stiller Fehlgriffe in der Ferienmaske** (ein Zeitraum über Tag 365
>    hinaus brach mit `IndexOutOfRangeException` ab; Zeitraum 1 mit Beginn vor Ende senkte das
>    ganze Jahr ab; ein Zeitraum ohne Ferientag blieb still) — die Lesart bleibt, gemeldet wird
>    über den Warnkanal des Laufs.
> 3. **Die Ferienabsenkung wird im Jahreslauf je Tag nachgeführt.** Bis hierher galt der
>    Wert des letzten Vorlauftags für alle 365 Tage (Befund X 3.4 Punkt 8).
> 4. **Keine feste Grenze von 100 Gebäuden** (U9): `HeizwaermebedarfGeb` wächst mit der
>    Gebäudezahl, das tote Feld `MaxP` ist gelöscht.
> 5. **Testdatenbank:** `Tab_Gebaeude.Bauweise` von Gebäude **10576** (Projekt 1008, 304 m²)
>    von **50** auf **15 200 Wh/K** (50 Wh/(m²K) × 304 m², Befund D) — genau diese eine Zelle,
>    über [`Skripte/gebaeude_10576_bauweise.py`](../../../Referenzlaeufe/Skripte/gebaeude_10576_bauweise.py). Der
>    Zellvergleich aller Tabellen vor und nach dem Lauf (11 959 209 Zellen) zeigt genau diese
>    eine Abweichung; `integrity_check` ok, Schema und Schemastand unverändert.
>
> **Ein Projekt von dreizehn bewegt sich: 1008.** Erwartet waren nach Plan **1008 und 1039**;
> **1039 bleibt byte-gleich**, und das ist erklärt: Der statische Zustand hat nie ein Ergebnis
> erreicht. Der Jahreslauf beginnt mit Tag 1, und dort setzt `TaeglHeizlastWG` die
> Vortemperatur ohnehin auf den Nachtsollwert; die Heizlasten des Vorlaufs (Tage 351–365),
> in dem die Temperatur des Vorgängergebäudes nachwirkte, überschreibt der Jahreslauf. Belegt
> ist das durch einen Zwischenlauf **mit allen Codeänderungen, aber noch ohne die Korrektur
> der Testdatenbank: 13/13 Projekte, 357/357 Dateien byte-gleich** gegen R10. Die Änderungen
> 2 bis 4 wirken in keinem Referenzprojekt, weil keines einen aktiven Ferienfahrplan führt
> (`Ferien = 0`, `Ferienbeginn_1 = 366` in allen fünfzehn Gebäuden) und keines mehr als drei
> Gebäude hat. Die ganze Abweichung kommt aus der Datenkorrektur:
>
> | Projekt 1008 | R10 | R11 |
> |---|---:|---:|
> | `Energiebedarf.Waermebedarf_Gesamt` [MWh/a] | 54,82 | **77,32** |
> | `Energiebedarf.Waermelast_Max` [kW] | 37,82 | **51,37** |
> | `Waermepumpe.Waermeproduktion_WP` [MWh/a] | 52,69 | **65,92** |
> | `Waermepumpe.Stromverbrauch_WP` [MWh/a] | 12,19 | **15,63** |
> | `Waermepumpe.Deckung_Heizung` [%] | 94,93 | **84,49** |
> | `Waermepumpe.Bivalenzpunkt` [°C] | 6,48 | **10,1** |
> | `Heizkessel.Waermeproduktion` [MWh/a] | 2,78 | **11,68** |
> | `Heizkessel.Gasverbrauch` [MWh/a] | 12,02 | **21,28** |
> | `Em.Kessel.Co2T` [t/a] | 2,885 | **5,107** |
> | `Energiebedarf.Waermerestbedarf` [MWh/a] | 0 | **0,31** |
>
> Dazu die übrigen Kessel-, Wärmepumpen-, Puffer- und Emissionsskalare desselben Projekts:
> **68 von 142 Skalaren** in 1008 und **15 der 24 Ganglinien- und Vektordateien**; der
> Toleranzvergleich zählt 86 281 abweichende von 262 941 Werten in 1008. Die zwölf übrigen
> Projekte sind **byte-gleich**. Keine Zeile kommt hinzu, keine fällt weg.
>
> **Die Gegenprobe:** Konzept Gebäudesimulation 5.11 hat das Gebäude 10576 allein im
> Tagesmodell nachgerechnet — 43 267 kWh/a mit 50 Wh/K, 65 773 kWh/a mit 15 200 Wh/K, also
> **+22 506 kWh/a**. Der Lauf weist für das ganze Projekt
> `Vektor.waermebedarf.Summe` 54 817,8 → 77 324,1 kWh aus: **+22 506,3 kWh/a** — dieselbe
> Differenz; das zweite Gebäude des Projekts (10577) ist unberührt. Neu ist ein
> **Restwärmebedarf von 0,31 MWh/a**: Die Spitzenlast steigt auf 51,4 kW, und in 75 Stunden
> (höchstens 10,4 kW) decken Wärmepumpe und Kessel sie nicht mehr ganz — eine Folge der
> Daten, der Erzeugerweg ist nicht angefasst.
>
> **Einfrierregeln:** Die Regel „gesäte Gebäudedaten“ entsteht mit diesem Schritt und ist
> sein Anlass; die drei älteren (Emissionsfaktoren, PV-Modulkoeffizienten, Flottenstand
> 1046) sind nicht berührt.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich**,
> Laufzeit 00:00:03.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-22_R11_Bestandsbefunde
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis.

**Und inzwischen auf Schemastand 101** — reines Nachziehen: Schritt 101 ist der
Gebäudespalten-Schritt M3 der Gebäudesimulation. `Wohnflaeche` heißt in `Tab_Gebaeude` und
`Tab_Gebaeude_STAMM` jetzt `Nutzflaeche` (Werte 1:1), jede der beiden Tabellen hat fünfzehn
neue Spalten (alle NULL, die zwei Schalter 0), und die Sicht `Abfrage_Projektgebaeude` ist aus
`GebaeudeSchema.SQL_VIEW_NEU` neu gebaut (die 58 Bestandsspalten an ihren Stellen, die neuen
dahinter). `integrity_check` ok, Zeilen- und Tabellenzahl unverändert, beide Tabellen weiter
`STRICT`; die **Referenzbasis R11 bleibt**, der Lauf aller dreizehn Projekte gegen sie ist **PASS
und in allen 357 Dateien byte-gleich** (3 882 737 Werte).

> **Nachtrag: Schemastand 102 (Auftrag #437, Etappe E7a; umnummeriert, 101 gehört der
> Gebäudesimulation), die Basis bleibt.** Migrationsschritt
> **102** (`SCHRITT_102_KWKG_ANLAGENART_LEER`, Quelle
> `EPOS.Kern/Allgemein/Update/KwkgAnlagenartLeer.cs`; Konzept Wirtschaftlichkeit § 6.3 Nr. 30)
> setzt die leere Zeichenkette in `Tab_Energieanlagen.KWKG_Anlagenart` auf NULL — **reines DML**,
> genau sieben Zellen: Anlage 12310 (Projekt 1032) und die Anlagen 14819, 14842, 14843, 14844,
> 14851, 14852 (Projekt 1043), Wärmepumpen, ein Kessel und Pufferspeicher, **kein BHKW und kein
> Referenzprojekt**. `KWKG_Eigenstromfall` derselben Zeilen bleibt `''`. Der Zellvergleich aller
> 119 Tabellen vor und nach dem Schritt zeigt genau diese sieben Zeilen und den Schemastand in
> `Tab_Applikation`; Größe (67 624 960 Byte) und Schema bleiben. **Keine Einfrierregel ist
> berührt**, und der Referenzlauf ist **13/13 byte-gleich** gegen diese Basis (357/357 CSV, vor
> wie nach dem Schritt gerechnet). Nachgezogen mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`.

> **Nachtrag: Schemastand 103 (Zapfprofilgenerator, Stufe Z0; umnummeriert), die Basis bleibt.**
> Migrationsschritt **103** (`SCHRITT_103_ZAPFPROFIL_KATALOG`, Quelle `EPOS.Kern/Allgemein/Update/TwwSchema.cs`;
> [Umsetzungskonzept Zapfprofilgenerator](../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md)
> 3.1/3.2, Schritt T1) legt die zehn Tabellen `Tab_Tww*` samt vier Indizes an — **reines DDL**,
> nachgezogen mit `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`.
> Danach spielt [`Skripte/tww_testkatalog_fiktiv.py`](../../../Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py) den
> **fiktiven Testkatalog** ein (Konzept Kapitel 6 (b)): ein Tagesgangsatz mit vier Tagesgängen,
> drei Nutzungsarten, drei Parameter mit neutralen Schlüsseln `Test.*`, ein Bedarfstag mit drei
> Ereignissen und vier DIN-4708-Werte — **19 Zeilen**, alle erfunden und rund, Katalogversion
> `TEST-1`, Status `EIGEN`, Herkunftsart `FIKTIV`, Quelle „Testkatalog (fiktiv)"; **keine** Zeile
> mit Status `AUSLIEFERUNG` oder `IMPORT`, keine Zone, keine Zeile in `Tab_TwwProjekt`. Das Skript
> ist wiederholbar (ein zweiter Lauf legt nichts an). Der Zellvergleich aller 120 Tabellen vor und
> nach dem Schritt zeigt nur den Schemastand in `Tab_Applikation`; `integrity_check` ok,
> `foreign_key_check` ohne Befund. Kein Projekt steht auf dem Generator, **keine Einfrierregel ist
> berührt**, und der Referenzlauf der fünf CI-Projekte (1007, 1017, 1030, 1045, 1046) ist
> **byte-gleich** gegen diese Basis (143/143 CSV).
> Die erfundene Kaltwasser-Bezugstemperatur der drei Nutzungsarten ist bewusst **kein** Wert,
> der mit einer Normvorgabe zusammenfällt; das Skript führt vorhandene Zeilen auf seine Werte
> nach (Zellvergleich: nur `Bezug_Kaltwasser` der drei Nutzungsarten, Schemastand unverändert).
> **Umnummeriert:** Schritt 101 gehört der Gebäudesimulation, der KWKG-Schritt ist 102, T1 ist 103.
> Die Testdatenbank ist aus der Fassung auf Schemastand 101 (Gebäudespalten) mit den Schritten 102
> und 103 und dem Skript neu nachgezogen: Zellvergleich gegen die Fassung 101 nur Schemastand, die
> sieben Anlagenzeilen und die zehn neuen Tabellen (19 Zeilen), gegen die frühere Fassung 102 nur
> Schemastand und Gebäudespalten; `integrity_check` ok, `foreign_key_check` leer, 129 Tabellen
> `STRICT`, 67 727 360 Byte.

> **Die Vorgängerbasis `2026-09-19_R10_BhkwWirkungsgrad`** ist mit dieser Einfrierung aus dem
> Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zu BH1‑O1 und den Nachträgen zu den
> Schemaständen 99 und 100 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R12 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat am 23.09.2026 die Basis R12 beschrieben — Anlass (Schlusswelle
G1 + G2 der Gebäudesimulation), die Tabelle je Projekt gegen R11 und die Nachträge zu den
Schemaständen 104 bis 113 und zum Zapfprofil-Testkatalog, mit denen die Basis ergebnisneutral
geblieben ist. Er steht hier im Wortlaut, weil diese Nachträge die Ergebnisneutralität der
Schritte belegen; die Verweise sind auf diesen Ort umgestellt.

**Abgelöst wurde R12 durch `2026-09-23_R13_Kuehlung`** (Stufe KU1 der Kühlung, vierte Welle,
vom Anwender am 23.09.2026 beauftragt: Entscheid E32 — Gebäude ohne wirksame Kühlung laufen frei —
und Projekt 1017 als Referenzprojekt mit Kühlung). Elf der dreizehn Projekte verschieben sich, 1030
(ohne Gebäude) und 1040 (Tagesbilanz-Weg) bleiben byte-gleich; die Tabelle steht im Abschnitt
„Aktuelle Basis“ von [`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-23_R12_Gebaeudemodell/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023,
1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046), **399 CSV**, **2 239 Skalare**, gerechnet mit
dem plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite`
(Schemastand **103**). Gegen diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017,
1045, 1046) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und
`EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040. Sie ist die
**einzige** Basis im Arbeitsbaum.

> **Anlass: die Schlusswelle G1 + G2 der Gebäudesimulation** (Entscheide E1/Q14, A15 mit E27;
> vom Anwender am 23.09.2026 beauftragt). **VDI 6007 ist das Vorgabemodell aller Gebäude:** Ein
> Gebäude ohne Angabe (`Gebaeude_Modell` NULL) rechnet stündlich nach dem 2-K-Modell
> (`EPOS.Kern/Allgemein/Simulation/Gebaeude/`), nicht mehr auf dem Tagesbilanz-Weg. Zwei
> Änderungen, eine davon an den Daten:
>
> 1. **Die NULL-Regel der Weiche** (`SimulationWaermebedarf.MODELL_OHNE_ANGABE`) steht auf
>    `VDI6007`. Der Ergebnisexport schreibt je Gebäude des VDI-Wegs drei neue Reihen —
>    `raumtemperatur_<n>.csv` und `operative_temperatur_<n>.csv` in °C, `kuehlbedarf_<n>.csv` in
>    kWh (n = Merkplatz des Gebäudes im Lauf) — und die Skalare `Geb[n].ID_Gebaeude`,
>    `Geb[n].Modell`, `…JahresheizwaermeMwh`, `…SpitzeKw`, `…SpitzeTagesmittelKw`,
>    `…Spitze95Kw`, `…KuehlenergieMwh`, `…StundenMitKuehlbedarf`,
>    `…MittlereRaumtemperaturHeizzeit`, `…Ueberhitzungsstunden`. Ein Gebäude auf dem
>    Tagesbilanz-Weg erzeugt keinen dieser Einträge.
> 2. **Testdatenbank (A15):** `Tab_Gebaeude.Gebaeude_Modell` von Gebäude **10645** (Projekt
>    **1040**, einziges Gebäude) von NULL auf **`TAGESBILANZ`** — genau diese eine Zelle, über
>    [`Skripte/gebaeude_1040_tagesbilanz.py`](../../../Referenzlaeufe/Skripte/gebaeude_1040_tagesbilanz.py). Der
>    Zellvergleich aller Tabellen vor und nach dem Skript (11 964 203 Zellen) zeigt genau diese
>    eine Abweichung; `integrity_check` ok, Schema und Schemastand unverändert.
>
> **Warum 1040 auf dem Altweg bleibt.** Bis zur Stufe GA („Altweg ablösen“) steht genau ein
> Referenzprojekt auf dem Tagesbilanz-Weg und hält den Rückweg-Test (Systementwurf
> Gebäudesimulation 8.4). 1040 hat ein einziges Gebäude, liegt nicht in den fünf Projekten der
> CI (deren Gebäude sollen den Vorgabeweg zeigen), und dasselbe Katalogobjekt „EFH-A-U-347s“
> steht in 1041, 1042 und 1045 auf dem VDI-Weg — die Basis führt beide Wege am selben Gebäude.
>
> **Elf Projekte bewegen sich, zwei bleiben byte-gleich:** **1040** (Altweg, alle 30 Dateien
> byte-gleich gegen R11) und **1030** (kein Gebäude; alle 22 Dateien byte-gleich). Die übrigen
> elf bekommen je Gebäude drei neue Dateien (42 Dateien für 14 Gebäudezeilen) und 13 neue
> Skalare; kein Schlüssel und keine Datei fällt weg.
>
> | Projekt | Gebäudewärme R11 [MWh/a] | R12 [MWh/a] | relativ | `Waermebedarf_Gesamt` R11 → R12 [MWh/a] | `Waermelast_Max` R11 → R12 [kW] |
> |---|---:|---:|---:|---:|---:|
> | 1007, 1046 | 53,07 | 69,07 | +30,2 % | 57,13 → 73,13 | 36,41 → 44,60 |
> | 1008 | 77,32 | 104,20 | +34,8 % | 77,32 → 104,20 | 51,37 → 72,69 |
> | 1017 | 62,96 | 90,19 | +43,2 % | 62,96 → 90,19 | 35,95 → 63,16 |
> | 1018 | 46,88 | 68,27 | +45,6 % | 46,88 → 68,27 | 34,10 → 37,36 |
> | 1023, 1024 | 329,80 | 450,56 | +36,6 % | 389,80 → 510,56 | 204,08 → 311,78 |
> | 1039 | 445,62 | 597,72 | +34,1 % | 466,62 → 618,72 | 257,71 → 382,10 |
> | 1041 | 59,35 | 75,98 | +28,0 % | 159,78 → 176,41 | 78,53 → 77,33 |
> | 1042 | 59,35 | 75,98 | +28,0 % | 89,35 → 105,98 | 45,38 → 44,77 |
> | 1045 | 59,35 | 75,98 | +28,0 % | 64,35 → 80,98 | 38,61 → 40,01 |
> | 1040 (Altweg) | 59,35 | 59,35 | ±0 | unverändert | unverändert |
> | 1030 (ohne Gebäude) | — | — | — | unverändert | unverändert |
>
> (Gebäudewärme = `Vektor.waermebedarf_gebaeude.Summe`; die Datei führt Watt je Stunde, die
> Summe ist also Wh — hier in MWh umgerechnet.)
>
> **Die Größenordnung ist die erwartete.** Konzept Gebäudesimulation 5.5 hatte mit dem
> Prototyp +7 bis +33 % gemessen, **ohne** den Abzug R_si/A in R_Rest,AW; der Abzug hebt die
> Jahresheizwärme um rund 12 % (Rechenschritte 9.6). Die Stufe G1 hatte im Speicher +25 bis
> +46 % gemessen — dieselben Zahlen wie hier je Gebäude (+24,9 % bei 10643 bis +45,6 % bei
> 10632). Die Jahresheizwärme trifft die Katalogkennzahl `spez_Waermeverbrauch` zu 95,8 bis
> 109,4 % (Tagesbilanz-Weg: 48 bis 88 %).
>
> **Auffälligkeiten, erklärt:**
>
> - **Die Spitzenlast steigt stärker als die Arbeit** (1017 +76 %, 1023/1024 +53 %, 1039
>   +48 %): Der ideale Heizer des VDI-Wegs deckt den Sprung vom Nacht- auf den Tagsollwert in
>   einer Stunde (Konzept 5.6: die Prototyp-Spitze lag bei 1017 schon +57 % über der des
>   Tagesmodells, dazu der R_si-Abzug). Bei 1041 und 1042 bestimmt der Prozess- bzw.
>   Brauchwasserkanal die Projektspitze; sie sinkt dort leicht (−1,5 % bzw. −1,3 %).
> - **Mehr ungedeckte Restwärme** in den Projekten mit knapp ausgelegten Erzeugern (1023
>   125 → 230 MWh/a, 1024 79 → 166, 1039 55 → 132, 1042 4,2 → 9,8, 1008 0,31 → 1,62) und
>   erstmals ein kleiner Rest in 1007/1046 (22 kWh in sechs Morgenstunden, höchstens 8,2 kW)
>   und 1017 (0,14 MWh/a in 34 Stunden) — dieselbe Morgenspitze trifft die festen Leistungen
>   der Erzeuger; der Erzeugerweg ist nicht angefasst.
> - **1018: Der Kessel liefert weniger, obwohl der Bedarf um 46 % steigt** (16,8 → 11,1
>   MWh/a). Das BHKW läuft 1 894 statt 1 016 Stunden und deckt 84,6 statt 66,1 %: Der VDI-Weg
>   hat eine physikalische Nachtgrundlast, wo das Tagesprofil die Nacht kappte, und ein
>   wärmegeführtes BHKW findet damit mehr Laufzeit (Stromproduktion 14,7 → 27,5 MWh/a).
> - **1041:** Die Wärmepumpe deckt dort den Prozesskanal; den Mehrbedarf des Gebäudes trägt
>   der Kessel (+12,5 %), der Strom der Wärmepumpe bleibt gleich.
>
> **Kein Fehlschlag, kein NaN, keine Ablehnung:** 13/13 Projekte gerechnet; keine Datei führt
> `NaN` oder `Inf`. Die Plausibilitätsprüfung des Klassenwegs lehnt kein Referenzgebäude ab;
> abgelehnt wird allein das Gebäude von Projekt 1009, das kein Referenzprojekt ist
> (`BauweiseUnplausibel`, 50 Wh/K auf 304 m²).
>
> **Einfrierregeln:** Berührt ist die Regel „gesäte Gebäudedaten“ (`Gebaeude_Modell` von
> 10645) — sie ist Teil dieses Einfrierens. Emissionsfaktoren, PV-Modulkoeffizienten und
> Flottenstand 1046 sind nicht berührt.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich** (399/399
> CSV), Laufzeit 4 bis 5 s für alle dreizehn Projekte.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-23_R12_Gebaeudemodell
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis; die Abnahme der Stufe
> (Kriterien, Vergleichsläufe) im
> [Status der Gebäudesimulation](../../aktuell/Status_Gebaeudesimulation_VDI6007.md).

> **Nachträge nach der Einfrierung von R12 (Merge vom 23.09.2026):** Die Schritte 104 und 105 der Etappen E7b und E7c1
> wurden gegen die damalige Basis R11 nachgewiesen und beim Zusammenführen mit R12 erneut gerechnet:
> **Nachweis gegen R12 (Merge a1985884, 23.09.2026):** Referenzlauf aller dreizehn Projekte auf Schemastand 105 mit dem Testdaten-UPDATE gegen `2026-09-23_R12_Gebaeudemodell` — **13/13 PASS**, 4 250 839 Werte innerhalb der Toleranz, 399 von 399 CSV byte-gleich; voller Testlauf des Kern-Filters auf dem gemergten Stand 11 091 grün, 1 übersprungen. Die Basis R12 bleibt.

> **Nachtrag: Schemastand 104 (Auftrag #439, Etappe E7b), die Basis bleibt.** Migrationsschritt
> **104** (`SCHRITT_104_ZEITZONENTARIF_ABLOESUNG`, Quellen `SchemaKatalog.Schritt104_LeistungspreisStaffel`
> und `EPOS.Kern/Allgemein/Update/ZeitzonentarifAbloesung.cs`; Konzept Wirtschaftlichkeit § 3.5,
> Register Q11) legt drei REAL-Spalten `Leistungspreis_Staffelgrenze`, `Leistungspreis_Staffel1` und
> `Leistungspreis_Staffel2` an `energy_project_settings` an (DDL) und führt den Datenteil in einer
> Transaktion: Er übernimmt die Staffel aktiver Zonensätze an den Stromträger (in der Testdatenbank
> keiner), löscht die Zonensätze (keiner) und verwirft ihre gespeicherten Läufe (0 Zeilen); die acht
> Zonenzeilen der Strommatrix in den Projekten 1018 und 1031 werden zu je einer Jahreszeile
> zusammengefasst (Spalte `Zone` = „Jahr", gleiche Summe). `Tab_Applikation` trägt 104; die Größe
> bleibt 67 727 360 Byte. **Keine Einfrierregel ist berührt**, und der Referenzlauf ist **13/13
> byte-gleich** gegen diese Basis (357/357 CSV, auf 103 wie 104 gerechnet). Nachgezogen mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`
> (Commit E7b/11 `bfbfbbb9`, LFS-SHA-256 `044e44db…`).

> **Nachtrag: Schemastand 105 (Auftrag #440, Etappe E7c1), die Basis bleibt.** Migrationsschritt
> **105** (`SCHRITT_105_KWKG_ABWAERMEABFUHR`, Quelle `SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr`; Konzept
> Wirtschaftlichkeit § 3.6, Befund K‑1) legt an `Tab_Energieanlagen` zwei Spalten an — `KWKG_Abwaermeabfuhr`
> (INTEGER NOT NULL DEFAULT 0, `CHECK` 0/1) und `KWKG_Stromkennzahl` (REAL, nullbar) —, **reines DDL**; die
> Tabelle bleibt `STRICT`, jede Bestandsanlage steht auf 0 (Fall 1) bzw. NULL. Dazu sät das Werkzeug die
> Katalog-Generation 8 nach: **eine Zeile** `KWKG_INBETRIEBNAHME_FRISTENDE` (2030 — Ende der Frist zur
> Inbetriebnahme, Quelle KWKG 2025 § 6) in `Tab_Gesetzesparameter`. Mit demselben Commit das
> **Testdaten-UPDATE** nach Entscheid E7‑Q1 (Lesart b): Die Anlagen 14920 und 14921 des Projekts 1030 tragen
> `KWKG_Anlagenart` 'NEUANLAGE' statt NULL (nur die Neuanlage erreicht 30.000 Vbh ohne Kostenanteil); ihr
> Kontingent 30.000 h bleibt gepflegt, **kein Anker bewegt sich**. `Tab_Applikation` trägt 105; die Größe
> bleibt 67 727 360 Byte, `quick_check` ok. **Keine Einfrierregel ist berührt**, und der Referenzlauf ist
> **13/13 byte-gleich** gegen diese Basis (357/357 CSV, 3 882 737 Werte, mit und ohne das UPDATE gerechnet).
> Nachgezogen mit `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`,
> das UPDATE als SQL außerhalb des Repos (Commit E7c1/9 `ccf9f22f`, LFS-SHA-256 `66aa52b0…`).

> **Nachtrag: fiktiver Tww-Testkatalog auf Schemastand 105 (Auftrag #443, Zapfprofilgenerator Z1), die Basis bleibt.**
> [`Skripte/tww_testkatalog_fiktiv.py`](../../../Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py) ist nach dem Zusammenführen mit Stand 105 erneut
> eingespielt: 15 erfundene Parameter des Bilanzrechenwegs (`Tab_TwwParameter_STAMM`, Katalogversion `TEST-1`, jetzt 18 Zeilen),
> keine Zone, kein Projekt auf dem Generator; ein zweiter Lauf ändert nichts. `integrity_check` ok, `foreign_key_check` leer,
> Größe unverändert 67 727 360 Byte; der Referenzlauf der fünf CI-Projekte ist byte-gleich gegen diese Basis
> (LFS-SHA-256 `a978270a…`).

> **Nachtrag: Schemastand 106 (Auftrag #444, Kopierweg der Wirtschaftlichkeit), die Basis bleibt.** Migrationsschritt
> **106** (`SCHRITT_106_WIRTSCHAFTLICHKEIT_FREMDVERWEIS`, Quelle
> `EPOS.Kern/Allgemein/Update/WirtschaftlichkeitFremdverweis.cs`) setzt jeden Verweis
> `Tab_ErgebnisWirtschaftlichkeit.ID_Ergebnis` auf NULL, zu dem kein Simulationslauf desselben Projekts gehört — das
> Erbe des Duplizierens, das den Verweis bis dahin unversetzt kopierte. **Reines DML**, genau 21 Zellen: Zeilen 16,
> 18, 20 (1028), 21, 23, 25 (1029), 189, 191, 193 (1040), 194, 196, 198 (1041) auf Lauf 167 von 1026 sowie 213–218
> (1043) und 219–221 (1044) auf Lauf 206 von 1042. Die Zeilen bleiben stehen und gelten als „passt nicht zum
> Simulationsstand". Größe und Schema bleiben, `integrity_check` ok, `foreign_key_check` leer, ein zweiter Lauf fasst
> nichts an. **Keine Einfrierregel ist berührt.** Nachgezogen mit `dotnet run --project Werkzeuge/Testdatenbankschema
> -- Referenzlaeufe/Kenndaten_Test.sqlite` auf der Datenbank des Gebäudemodell-Stands (Basis R12), LFS-SHA-256
> `49284ce3…`.

> **Nachtrag: Schemastand 107 (Entscheid E30, Gebäudesimulation), die Basis bleibt.** Migrationsschritt
> **107** (`SCHRITT_107_ERGEBNIS_GEBAEUDE`, Quelle `ErgebnisGebaeudeSchema`; Konzept Gebäudesimulation N1.35)
> legt die leere STRICT-Tabelle `Tab_ErgebnisGebaeude` samt zwei Indizes an, **reines DDL**; der Lauf schreibt
> sie, der Referenzlauf exportiert sie nicht (die Kennzahlen stehen schon als Skalare `Geb[n].*`). Nachgezogen
> mit `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` auf der
> Fassung 106, danach
> [`Skripte/gebaeude_10612_233_bauweise.py`](../../../Referenzlaeufe/Skripte/gebaeude_10612_233_bauweise.py) erneut eingespielt
> (Datenwechsel oben). Zellvergleich aller Tabellen gegen die Fassung 106: `SchemaVersion` 106 → 107, die neue
> leere Tabelle mit ihren zwei Indizes und die zwei `Bauweise`-Zellen (`Tab_Gebaeude` 10612,
> `Tab_Gebaeude_STAMM` 233) — sonst nichts. `integrity_check` ok, `foreign_key_check` leer, Größe
> 67 739 648 Byte. **Keine Einfrierregel mit Rechenwirkung ist berührt**, und der Referenzlauf aller dreizehn
> Projekte ist **13/13 PASS** gegen diese Basis (4 250 839 Werte, 399/399 CSV byte-gleich, außer
> `protokoll.txt`) (LFS-SHA-256 `36e693ad…`).

> **Nachtrag: Schemastände 108 bis 110 (Kühlung, Stufe KU1, Welle 1), die Basis bleibt.** Migrationsschritte
> **108** (`SCHRITT_108_KUEHLUNG_GEBAEUDE`, KU-S1: vier Kühleingaben an `Tab_Gebaeude(_STAMM)` und der zweite
> Neubau der Sicht `Abfrage_Projektgebaeude`, Quelle `GebaeudeSchema`), **109** (`SCHRITT_109_KUEHLUNG_PROJEKTEINSTELLUNG`,
> KU-S2: `Tab_Einstellungen.Kuehlbetrieb`, 0/1, Vorgabe 0) und **110** (`SCHRITT_110_KUEHLUNG_ERGEBNIS`, KU-S4: neun
> nullbare Ergebnisspalten des Kühlkanals; beide Quelle `KuehlungSchema`), **reines DDL** (Kühlkonzept Kapitel 7).
> Nachgezogen mit `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` auf
> der Fassung 107; ein zweiter Lauf legt nichts an. Zellvergleich aller 130 Tabellen gegen die Fassung 107
> (10 493 796 Zellen): `SchemaVersion` 107 → 110 und die 18 neuen Spalten — Kühleingaben und Ergebnisspalten
> NULL, `Kuehlung_Aktiv` und `Kuehlbetrieb` 0 —, sonst nichts; die Sicht behält ihre 73 Spalten an ihren Stellen
> und bekommt vier dahinter. `integrity_check` ok, `foreign_key_check` leer, 130 von 130 Tabellen STRICT, Größe
> 67 743 744 Byte. **Keine Einfrierregel ist berührt.** Der Referenzlauf-Export nimmt die neun Ergebnisspalten
> erst auf, wenn ein Lauf sie erhebt — NULL heißt „nicht erhoben" (`Referenzlauf/Ergebnisexport.cs`); damit bleibt
> `aggregate.csv` ohne `--ohne` gleich. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis
> (4 250 839 Werte, 399/399 CSV byte-gleich, außer `protokoll.txt`) (LFS-SHA-256 `a3355a80…`).

> **Nachtrag: Schemastand 113 (Auftrag #446, Etappe E7c2), die Basis bleibt.** Drei Migrationsschritte
> (Konzept Wirtschaftlichkeit § 2.5, § 2.13 (3) und § 5; Register A6, ET‑D‑3, U‑1 mit A9): **111**
> (`SCHRITT_111_ERSATZ_RESTWERT_KENNZEICHEN`, Quelle `SchemaKatalog.Schritt111_ErsatzRestwertKennzeichen`) legt
> die nullbaren Kennzeichen `ErsatzFuehren` und `RestwertAnsetzen` (`CHECK` 0/1) an `Tab_ProjektWerte` und
> `Tab_KostenVorlagePosition` an — vier Spalten, **reines DDL**, alle Zeilen leer (= wie bisher); **112**
> (`SCHRITT_112_PREISBASIS`, Quellen `SchemaKatalog.Schritt112_Preisbasis` und
> `EPOS.Kern/Allgemein/Update/PreisbasisUebernahme.cs`) legt die Textspalte `Preisbasis` an
> `energy_project_settings` an und füllt sie einmalig aus `ID_Umrechnung` (5 Zeilen „kWh", 23 mit der
> Abrechnungseinheit); **113** (`SCHRITT_113_GASE_NM3`, Quelle `EPOS.Kern/Allgemein/Update/GaseNormkubikmeter.cs`),
> **reines DML**, setzt `Tab_Brennstoff_Stamm.Einheit`/`PreisEinheit` der fünf Gase auf Nm³ und €/Nm³ und des
> Brennstoffs 24 „Sonstige" auf kWh und €/kWh, dazu die eine Preiszeile mit „m³" (Projekt 1039, Erdgas E).
> `Tab_Applikation` trägt 113, `quick_check` ok, Größe 67 743 744 Byte. **Keine Einfrierregel ist berührt** (die
> Liste nennt von `Tab_Brennstoff_Stamm` nur CO₂/SO₂/NOx/Staub), und der Referenzlauf aller dreizehn Projekte ist
> **13/13 PASS** gegen diese Basis (4 250 839 Werte in der Toleranz, 399/399 CSV byte-gleich; Gate #446 auf
> `41764ab0`). Nachgezogen mit `dotnet run --project Werkzeuge/Testdatenbankschema --
> Referenzlaeufe/Kenndaten_Test.sqlite` auf der Fassung 110 der Kühlung (Commit E7c2/14 `94db9f92`, LFS-SHA-256
> `689a0755…`). Gebaut waren die drei Schritte als 107 bis 109; die Zwischenfassung aus E7c2/12 (`b943f534`, 107 →
> 110 mit den Nummern 108 bis 110 für diese Schritte) ist ersetzt.

> **Nachtrag: fiktiver Tww-Testkatalog der Stufe Z2 auf Schemastand 113 (Zapfprofilgenerator Z2), die Basis bleibt.**
> [`Skripte/tww_testkatalog_fiktiv.py`](../../../Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py) ist nach dem Zusammenführen mit der Fassung 113
> eingespielt: 63 Zeilen angelegt, 2 nachgeführt — die erfundenen Parameter der Auslegung (Summenlinie, Kennzahl samt
> Zapfblöcken, Speicherauslegung samt Nenninhaltsliste, Großanlage, Konstruktorregel; `Tab_TwwParameter_STAMM` jetzt
> 72 Zeilen), zwei weitere Bedarfstage (jetzt drei, neun Ereignisse) und ein weiterer DIN-4708-Wert (jetzt fünf); keine
> Zone, kein Projekt auf dem Generator, kein Status `AUSLIEFERUNG`/`IMPORT`. Ein zweiter Lauf meldet 0/0, jeder
> Parameterschlüssel des Rechenwegs ist da (`EPOS.Kern.Tests/TwwKatalogWacheTests` verlangt das von der Repo-Datei).
> Schemastand unverändert 113, `integrity_check` ok, `foreign_key_check` leer, Größe 67 751 936 Byte. **Keine
> Einfrierregel ist berührt**, und der Referenzlauf der fünf CI-Projekte ist **5/5 PASS** und byte-gleich gegen diese
> Basis (1 761 589 Werte) (LFS-SHA-256 `fc13e3a7…`).

> **Nachtrag: Katalog-Generation 9 (Auftrag #452), die Basis bleibt.** Kein Schemaschritt: Mit der Generation 9
> sät der Gesetzeskatalog keine Zeile, sondern pflegt bestehende einmal nach (`GesetzKatalog.Nachpflege`; Konzept
> Wirtschaftlichkeit § 3.6 und § 5; Register E7c1‑Q8, E7c2‑Q4) — Brennstoff 24 „Sonstige" in `Tab_Brennstoff_Stamm`
> H_i 0 → 1 und H_s 0 → 1, die Katalogzeilen `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB` Status
> GESICHERT → ABGEKUENDIGT, Katalog-Marker 8 → 9: genau fünf Zellen im Zellvergleich über alle 130 Tabellen; Schema
> gleich, Integritätsprüfung ok, ein zweiter Lauf pflegt nichts. Nachgezogen mit `dotnet run --project
> Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` in zwei Schritten: erst auf der Fassung 113
> aus E7c2/14 (LFS-SHA-256 `689a0755…` → `db143098…`, 67 743 744 Byte; Commit E7c3/11 `ffff2fe2`), dann nach dem
> Nachzug des Zapfprofilgenerators Z2 auf dessen Fassung aus `a228185d` (LFS-SHA-256 `fc13e3a7…` → `9df1b7a5…`,
> 67 751 936 Byte, 10 496 533 Zellen verglichen, der Z2-Testkatalog unberührt; Commit E7c3/12 `6ebb26b6`), die die
> erste ersetzt. Schemastand unverändert 113. **Keine Einfrierregel ist berührt** (die Liste nennt von
> `Tab_Brennstoff_Stamm` nur CO₂/SO₂/NOx/Staub, den Gesetzeskatalog gar nicht), und der Referenzlauf aller dreizehn
> Projekte ist **13/13 PASS** gegen diese Basis, byte-gleich mit beiden Ständen der Datenbank (4 250 839 Werte in der
> Toleranz, 399/399 CSV byte-gleich; Gate #452 auf `9c7a0023`, vervollständigt mit `387c2d9f`).

> **Die Vorgängerbasis `2026-09-22_R11_Bestandsbefunde`**, die letzte Basis allein auf dem
> Tagesbilanz-Weg, ist mit dieser Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt
> der Begründung zur Stufe GB und den Nachträgen zu den Schemaständen 101 bis 103 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R13 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat vom 23. bis zum 24.09.2026 die Basis R13 beschrieben — Anlass
(Schlusswelle KU1 der Kühlung: E32 und Projekt 1017 als Referenzprojekt mit Kühlung), die Tabelle je
Projekt gegen R12 und die Nachträge zu den Schemaständen 114 bis 121, mit denen die Basis
ergebnisneutral geblieben ist. Er steht hier im Wortlaut, weil diese Nachträge die Ergebnisneutralität
der Schritte belegen; die Verweise sind auf diesen Ort umgestellt.

**Abgelöst wurde R13 durch `2026-09-24_R14_Kaelteerzeuger`** (Stufe KU2 der Kühlung, vierte Welle,
vom Anwender am 24.09.2026 beauftragt: Projekt 1017 bekommt seinen Kälteerzeuger — die Wärmepumpe im
Kühlbetrieb). Allein 1017 bewegt sich, die übrigen zwölf Projekte bleiben byte-gleich; die Tabelle
steht im Abschnitt „Aktuelle Basis“ von [`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-23_R13_Kuehlung/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046), **387 CSV**, **2 207 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand
**113**, LFS-SHA-256 `769143e4…`, Nachträge 114 bis 121 in diesem Abschnitt; die Katalog-Generation 9 aus Auftrag #452 ist enthalten und bewegt
kein Referenzprojekt — ihr Nachtrag steht beim R12-Abschnitt unter `ueberholt/`). Gegen diese Basis hält `.github/workflows/kern.yml` (1030,
1007, 1017, 1045, 1046) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und
`EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040. Sie ist die
**einzige** Basis im Arbeitsbaum.

> **Nachtrag Schemastand 115 (Zapfprofilgenerator, Stufe Z3, Schemaschritt T2).** Die
> Testdatenbank steht auf Schemastand **115** (LFS-SHA-256 `fbc30835…`): Auf der Fassung mit
> Schemastand 114 (Kühlung, Nachtrag unten) hat Werkzeuge/Testdatenbankschema
> `Tab_TwwZapfkategorie_STAMM` angelegt (Schritt 115), und
> [`Skripte/tww_testkatalog_fiktiv.py`](../../../Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py) hat 82 Katalogzeilen
> ergänzt — vier aus VDI 6002 abgeleitete Nutzungsarten samt Tagesgangsätzen (Herkunftsart `FIKTIV`,
> ZU19, Abschnitt „Abgeleitete VDI-Werte im Tww-Testkatalog“) und aus dem freien Paketteil
> (Abschnitt „Der freie Paketteil“) das Ecodesign-Zapfprofil L mit 24 Ereignissen, die fünf Parameter
> der Stochastik und den Vorgabesatz der Zapfkategorien an jeder der sieben Nutzungsarten (28 Zeilen,
> Herkunftsart `FREI`). Ein zweiter Skriptlauf meldet 0/0; `integrity_check` ok, `foreign_key_check`
> leer. Kein Referenzprojekt steht auf dem Generator: Der Referenzlauf der fünf CI-Projekte gegen
> diese Basis ist **byte-gleich**, die Basis bleibt.

> **Anlass: die vierte und letzte Welle der Stufe KU1 der Kühlung** (vom Anwender am 23.09.2026
> beauftragt). Zwei Änderungen, eine davon an den Daten:
>
> 1. **Entscheid E32 — Gebäude ohne wirksame Kühlung laufen frei**
>    ([Konzept Gebäudesimulation](../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
>    N1.37). Ein Gebäude, dessen Kühlung nicht wirksam ist (Projektschalter „Kühlung rechnen" aus,
>    `Kuehlung_Aktiv` = 0 oder kein Kühlsollwert), wird nicht mehr an `Maximaleraumtemperatur`
>    gekappt: Die Raumtemperatur darf darüber steigen, es wird keine Wärme abgeführt, und die
>    Überhitzungsstunden zählen die Stunden darüber im freien Lauf. Eine Kühlreihe gibt es für ein
>    solches Gebäude nicht mehr — der Ergebnisexport schreibt `kuehlbedarf_<n>.csv`,
>    `Geb[n].KuehlenergieMwh` und `Geb[n].StundenMitKuehlbedarf` nur noch bei wirksamer Kühlung.
> 2. **Testdatenbank: das Referenzprojekt mit Kühlung** (Kühlkonzept 10.4, Einfrierregel „gesäte
>    Kältedaten" oben): Projekt **1017** rechnet Kälte — genau vier Zellen über
>    [`Skripte/kuehlung_1017_referenzprojekt.py`](../../../Referenzlaeufe/Skripte/kuehlung_1017_referenzprojekt.py):
>    `Tab_Einstellungen.Kuehlbetrieb` 0 → 1, Gebäude 10599 `Kuehlung_Aktiv` 0 → 1,
>    `Kuehl_Sollwert` NULL → 24,0 °C, `Kuehlleistung_Max` NULL → 15,0 kW. Sicherung vorher außerhalb
>    des Repositoriums; der Zellvergleich aller 130 Tabellen (10 496 533 Zellen) zeigt genau diese
>    vier Zellen, `integrity_check` ok, `foreign_key_check` leer, Größe unverändert 67 751 936 Byte,
>    ein zweiter Lauf des Skripts ändert nichts.
>
> **Warum 1017.** Ein Einzelgebäude-Projekt auf dem VDI-Weg (Gebäude „GMH-D-S-118", 744,4 m²,
> Skalierung fast 1) mit genau einer Wärmepumpe — sie bekommt mit KU2 den Kühlbetrieb — und mit PV
> und Stromspeicher, der üblichen Umgebung einer reversiblen Wärmepumpe; und 1017 gehört zu den
> fünf Projekten der CI, die Kühlung ist damit bei jedem Push im Netz. Nicht 1040 (Tagesbilanz-Weg,
> A15), nicht 1045 (die Kühltests schalten die Kühlung an dessen Gebäude auf Arbeitskopien ein und
> aus), nicht 1046 (Flottenstand eingefroren), nicht 1007 (Gebäude wie 1046, Skalierung 4,6).
> **Die Werte:** Der Kühlsollwert ist die Maximaleraumtemperatur des Gebäudes — die Anlage hält die
> Grenze, gegen die die Überhitzungsstunden zählen, und er liegt mehr als 1 K über dem höchsten
> Heizsollwert; die Grenze 15 kW liegt unter der Spitze ohne Grenze (rund 21 kW), sodass die Basis
> auch den Betriebsfall „Kühlgrenze" trägt.
>
> **Elf Projekte bewegen sich, zwei bleiben byte-gleich:** **1030** (kein Gebäude) und **1040**
> (Tagesbilanz-Weg), alle Dateien byte-gleich gegen R12. Die Wärmelast `Waermelast_Max` bleibt in
> allen Projekten gleich.
>
> | Projekt | Gebäudewärme R12 → R13 [MWh/a] | relativ | `Waermebedarf_Gesamt` R12 → R13 [MWh/a] | Überhitzungsstunden je Gebäude R12 → R13 [h] | Dateien R12 → R13 |
> |---|---:|---:|---:|---|---:|
> | 1007, 1046 | 69,07 → 68,97 | −0,145 % | 73,13 → 73,03 | 469 → 600 | 36 → 35, 40 → 39 |
> | 1008 | 104,20 → 104,14 | −0,058 % | 104,20 → 104,14 | 295 → 440; 469 → 600 | 31 → 29 |
> | **1017 (gekühlt)** | 90,19 → 90,19 | −0,0001 % | 90,19 → 90,19 | 305 → 306 | 24 → 25 |
> | 1018 | 68,27 → 68,25 | −0,032 % | 68,27 → 68,25 | 154 → 233 | 25 → 24 |
> | 1023, 1024 | 450,56 → 449,90 | −0,145 % | 510,56 → 509,90 | 628 → 840 | 28 → 27, 29 → 28 |
> | 1039 | 597,72 → 597,02 | −0,117 % | 618,72 → 618,02 | 72 → 102; 264 → 308; 628 → 840 | 34 → 31 |
> | 1041 | 75,98 → 75,94 | −0,050 % | 176,41 → 176,37 | 277 → 331 | 30 → 29 |
> | 1042 | 75,98 → 75,94 | −0,050 % | 105,98 → 105,94 | 277 → 331 | 37 → 36 |
> | 1045 | 75,98 → 75,94 | −0,050 % | 80,98 → 80,94 | 277 → 331 | 33 → 32 |
> | 1040 (Tagesbilanz-Weg) | 59,35 → 59,35 | ±0 | unverändert | — | 30, byte-gleich |
> | 1030 (ohne Gebäude) | — | — | unverändert | — | 22, byte-gleich |
>
> (Gebäudewärme = `Vektor.waermebedarf_gebaeude.Summe`, in MWh umgerechnet.)
>
> **Auffälligkeiten, erklärt:**
>
> - **Die Heizwärme sinkt um höchstens 0,15 %** (E32): Die Wärme, die die Kappung im Sommer
>   abführte, bleibt in den Speichermassen und senkt den Heizbedarf der Übergangszeit ein wenig. Mit
>   ihr sinkt die ungedeckte Restwärme leicht (1023 229,95 → 229,88, 1024 166,36 → 166,32, 1039
>   131,75 → 131,74 MWh/a), und die Erzeugerreihen der betroffenen Projekte verschieben sich im
>   selben Maß.
> - **Die Überhitzungsstunden steigen um 17 bis 51 %:** Im freien Lauf liegt die operative
>   Temperatur in mehr Stunden über der Maximaleraumtemperatur als unter der Kappung, die nur die
>   Raumluft hielt; die Raumluft erreicht bis 33,4 °C (die Referenzgebäude führen keine
>   Sommerlüftung).
> - **1017 bleibt fast bei R12:** gekühlt auf 24 °C ist die frühere Kappung — bis auf die 25
>   Stunden an der Grenze von 15 kW, in denen die Raumluft bis 25,0 °C steigt (Heizwärme
>   −0,0001 %, Überhitzung 305 → 306 h, 525 Werte außerhalb der Toleranz gegen R12). **Neu ist die
>   Kälteseite:** Kältebedarf 2,52 MWh/a in 402 Stunden, Kältelast 14,99 kW (die Grenze des
>   Katalogbaus mal der Skalierung 744/744,4), ungedeckt (`Kaelterestbedarf` 2,52 MWh/a, die Warnung
>   „Kältebedarf ohne Kälteerzeuger" ist gewollt — KU1 hat keinen Kälteerzeuger). Dazu die Kanaldatei
>   `waermebedarf_kuehlung.csv` und sechs der neun Kältespalten (`Waermebedarf_Kuehlung`,
>   `Kaeltebedarf_Gesamt`, `Kaeltelast_Max`, `Kaelterestbedarf`, `Deckung_Kuehlung` von BHKW und
>   Heizkessel mit 0); Wärmepumpe, Solarthermie und Pufferspeicher schreiben in 1017 keine
>   Ergebniszeile bzw. bleiben nach K7 leer.
> - **Dateien und Schlüssel:** 13 Gebäude ohne wirksame Kühlung verlieren `kuehlbedarf_<n>.csv` und
>   je drei Skalare (`Geb[n].KuehlenergieMwh`, `Geb[n].StundenMitKuehlbedarf`,
>   `Vektor.kuehlbedarf_<n>.Summe`); 1017 behält seine Kühlreihe und bekommt eine Datei und sieben
>   Skalare dazu — 399 → 387 CSV, 2 239 → 2 207 Skalare.
>
> **Kein Fehlschlag, kein NaN, keine Ablehnung:** 13/13 Projekte gerechnet; keine Datei führt `NaN`
> oder `Inf`.
>
> **Einfrierregeln:** Mit dieser Einfrierung entsteht die Regel „gesäte Kältedaten" (vier Zellen in
> 1017). E32 ist eine Änderung des Rechenwegs, keine Datenänderung; Emissionsfaktoren,
> PV-Modulkoeffizienten, Flottenstand 1046 und gesäte Gebäudedaten sind nicht berührt.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich** (387/387 CSV),
> Laufzeit 4 s für alle dreizehn Projekte; ebenso byte-gleich ein Lauf auf der Testdatenbank vor dem
> Zusammenführen mit dem Zapfprofil-Testkatalog Z2 — der Testkatalog bewegt kein Referenzprojekt.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-23_R13_Kuehlung
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis; die Abnahme der Stufe im
> [Protokoll der Schlusswelle KU1](../Protokolle/Gebaeudesimulation/2026-09-23_Schlusswelle_KU1.md)
> und im [Status der Gebäudesimulation](../../aktuell/Status_Gebaeudesimulation_VDI6007.md).

> **Nachtrag: Schemastand 114 (Kühlung, Stufe KU2, Welle 1), die Basis bleibt.** Migrationsschritt
> **114** (`SCHRITT_114_KUEHLUNG_ERZEUGER`, KU-S3: `Kuehlbetrieb` (0/1, Vorgabe 0), `Kuehl_Vorlauf` und
> `Kuehl_Hilfsstromanteil` an `Tab_WP` und `Tab_WP_STAMM`, dazu `Tab_Energieanlagen.Kuehl_ID_Carrier` mit
> Beziehung auf `energy_carrier.id` und `ON DELETE SET NULL`; Quelle `KuehlungSchema`), **reines DDL**
> (Kühlkonzept 7.3, Entscheide E15 und E33). Nachgezogen mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` auf der
> Fassung 113; ein zweiter Lauf legt nichts an. Zellvergleich aller 130 Tabellen gegen die Fassung 113
> (10 496 532 Zellen): `SchemaVersion` 113 → 114 und die sieben neuen Spalten — `Kuehlbetrieb` 0, die
> übrigen NULL —, sonst nichts; die 14 Sichten unverändert. `integrity_check` ok, `foreign_key_check`
> leer, 130 von 130 Tabellen STRICT, Größe unverändert 67 751 936 Byte. **Keine Einfrierregel ist
> berührt:** Die neuen Spalten tragen keinen gesäten Wert, und kein Rechenweg liest sie. Referenzlauf
> aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 145 687 Werte, 387/387 CSV
> byte-gleich, außer `protokoll.txt`) (LFS-SHA-256 `8a3bebaf…`).

> **Nachtrag: Schemastände 116 bis 118 (Szenarioabdeckung der Wirtschaftlichkeit, Etappe E9a,
> #461), die Basis bleibt.** Migrationsschritte **116** (`SCHRITT_116_SZENARIO_RAHMEN`: vier nullbare
> Spalten `Szen_Best_/Szen_Worst_Zeitraum` und `…_Menge` an `Tab_ProjektWirtschaftlichkeit`), **117**
> (`SCHRITT_117_TRAEGERPREIS_SZENARIO`: sechs nullbare Spalten `custom_price_work/base/power_best/_worst`
> an `energy_project_settings`) und **118** (`SCHRITT_118_ERLOESSATZ_SZENARIO`: acht nullbare Spalten —
> `Einspeiseverguetung(_KWK)_Best/_Worst` an `Tab_ProjektWirtschaftlichkeit`, `DvEntgelt_` und
> `PpaPreis_Best/_Worst` an `Tab_ProjektPhotovoltaik`), **reines DDL**; NULL heißt „wie Erwartet". Mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` auf der
> Fassung 115 (LFS-SHA-256 `fbc30835…`, 67 780 608 Byte) nachgezogen: 18 Spalten angelegt, Marker 118;
> ein zweiter Lauf legt nichts an. Gegen 115 unterscheiden sich allein die drei Tabellen um die 18
> neuen, leeren Spalten und `SchemaVersion` 115 → 118; `integrity_check` ok, `foreign_key_check` leer,
> Größe 67 784 704 Byte (LFS-SHA-256 `e9748b7f…`). **Keine Einfrierregel ist berührt:** Keine Spalte
> trägt einen gesäten Wert, und der Referenzlauf führt keine Wirtschaftlichkeitsgröße — er bleibt
> byte-gleich.

> **Nachtrag: Schemastand 119 (Kühlung, Stufe KU2, Welle 3), die Basis bleibt.** Migrationsschritt
> **119** (`SCHRITT_119_KAELTESTROM`: `Tab_Energieanlagen.Kuehl_EigenerZaehler` — 0/1 mit `CHECK`, nullbar,
> ohne Vorgabe, NULL = anteilig am Netzbezug, Entscheid E34 — und sieben nullbare Ergebnisspalten der
> Kälteseite an `Tab_ErgebnisWaermepumpe` (`Kaelteproduktion_WP`, `Stromverbrauch_Kuehlung`) und
> `Tab_ErgebnisWaermepumpeModul` (`Kaelteproduktion`, `Stromverbrauch_Kuehlung`, `Kaeltestrom_Netzbezug`,
> `Kuehl_carrier_id`, `Kuehl_EigenerZaehler`); Quelle `KuehlungSchema`), **reines DDL** (Kühlkonzept 6.1–6.4,
> 8.4). Er folgt auf die Schritte 115 (Zapfprofil, Nachtrag oben) und 116 bis 118 (Szenarioabdeckung der
> Wirtschaftlichkeit, E9a) und ist auf deren Fassung **118** nachgezogen, mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`; ein zweiter Lauf
> findet nichts offen. Zellvergleich aller 132 Tabellen (samt `sqlite_sequence`) gegen die Fassung 118
> (10 498 685 Zellen): `SchemaVersion` 118 → 119 und die acht neuen Spalten, alle NULL, sonst nichts; die 14
> Sichten und alle 208 Indizes unverändert. `integrity_check` ok, `foreign_key_check` leer, 131 von 131
> Fachtabellen STRICT, Größe unverändert 67 784 704 Byte. **Keine Einfrierregel ist berührt:** Die neuen
> Spalten tragen keinen gesäten Wert; die Ergebnisspalten schreibt nur ein Lauf mit Kältekaskade, und kein
> Referenzprojekt kühlt. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis
> (4 145 687 Werte, 387/387 CSV byte-gleich, außer `protokoll.txt`) (LFS-SHA-256 `6259b348…`).

> **Nachtrag E10 (#463): Schemastand 120 und der Anschluss der Speicherflotte an die
> Nutzungsdauertabelle, die Basis bleibt.** Migrationsschritt **120**
> (`SCHRITT_120_NUTZUNGSDAUER_SAETZE`, Nutzungsdauer-Konzept Stufe S3), **reines DML** an
> `Tab_Nutzungsdauer`: Die leeren Satzzellen der Standardzeilen bekommen die Mitte des
> Empfehlungsbereichs derselben Position der Betriebsvorlagen-Saat (Quelle `NutzungsdauerSaetze`) —
> fünf Zellen `Instandsetzung_Prozent`: Heizkessel · Wärmeerzeuger 2,0, BHKW · Modul 6,0,
> Wärmezentrale · Rohrleitungen 2,0, Stromeinspeisung · Netzanschluss 2,0, Bauliche Anlagen 1,25;
> `Wartung_Prozent` bleibt überall leer. Mit `Werkzeuge/Testdatenbankschema` auf der Fassung 119
> nachgezogen; ein zweiter Lauf setzt nichts (0/5). Zellvergleich gegen die Fassung 119: allein
> `SchemaVersion` 119 → 120 und diese fünf Zellen; `integrity_check` ok, `foreign_key_check` leer,
> Größe unverändert 67 784 704 Byte (LFS-SHA-256 `52c4729d…`). **Ergebnisneutral:** Ein Satz der
> Tabelle rechnet erst, wenn der Anwender ihn über „Sätze vorbelegen…" oder die Übernahme einer
> Kostenvorlage in eine Position schreibt; der Rechenweg liest weiter den Satz der Position.
>
> **Mit derselben Welle** rechnet die Wirtschaftlichkeit der Speicherflotten-STUDIE den Restwert je
> Einheit linear aus ihrer Nutzungsdauer auf der Ersatzkette der Flotte (Betrag der letzten
> Beschaffung × Restdauer ÷ Nutzungsdauer); eine Einheit ohne eigenes Ersatzintervall nimmt die
> Nutzungsdauer der Standardzeile „Stromspeicher · Batterie" (10 a), und der feste Restwert je
> Einheit ist ein Altfeld, das nicht mehr rechnet. Der Projektlauf rechnet keine
> Flottenwirtschaftlichkeit, und `aggregate.csv` führt für 1046 nur die Physik der Flotte — die
> Referenz bewegt sich nicht (dritte Einfrierregel, Absatz „Anschluss an die Nutzungsdauertabelle").
> Referenzlauf aller dreizehn Projekte gegen diese Basis: **13/13 PASS** (4 145 687 Werte, 387/387 CSV
> byte-gleich, außer `protokoll.txt`) — deshalb keine neue Basis R14.

> **Nachtrag #468: Schemastand 121 (Katalogverweis des Projektgebäudes), die Basis bleibt.**
> Migrationsschritt **121** (`SCHRITT_121_GEBAEUDE_KATALOGVERWEIS`, Konzept Administrationsdialoge
> 7.1 (a), Quelle `GebaeudeKatalogverweis`): `Tab_Gebaeude.ID_Gebaeude_Stamm` (`INTEGER`, nullbar,
> `REFERENCES Tab_Gebaeude_STAMM(ID) ON DELETE SET NULL`) samt Index `Tab_Gebaeude_ID_Gebaeude_Stamm`,
> einmalig über den eindeutigen Gebäudenamen nachgetragen — alle **26** Projektgebäude der Testdatenbank
> tragen danach den Verweis auf den Katalogsatz ihres Namens, keines bleibt ohne. Dazu die Reparatur
> nach Schadensbild im Katalog (`GebaeudeSonstigeFlaeche`): Wo `Sonstige_Flaechen` > 0 bei U-Wert
> „Sonstiges" 0 steht, wird die Fläche 0 — U · A war 0 und bleibt 0. Getroffen sind die vier
> Katalogsätze 1 `AltenH-95-EnEV2016` und 6 `Pflegeheim-122-EnEV2016` (je 4,0 m²), 100
> `SpH-Umkl-287-EnEV2016` und 103 `SpH-Umkl-NE` (je 2,2 m²); **keiner ist einem Projekt zugeordnet**.
> Der fünfte regelwidrige Satz, 79 `Krankenhaus_92-EnEV2016` (U-Wert Fenster 0,09 bei 11 646 m²
> Fensterfläche), ist nicht nach Schadensbild herzuleiten und bleibt unverändert. Mit
> `Werkzeuge/Testdatenbankschema` auf der Fassung 120 nachgezogen; ein zweiter Lauf findet nichts offen.
> Zellvergleich aller 132 Tabellen gegen die Fassung 120: `SchemaVersion` 120 → 121, die neue Spalte
> (26/26 gesetzt), der neue Index und die vier Zellen `Sonstige_Flaechen`, sonst nichts; 14 Sichten
> unverändert. `integrity_check` ok, `foreign_key_check` leer, 131 von 131 Fachtabellen STRICT, Größe
> 67 792 896 Byte (LFS-SHA-256 `00fbbb8b…`). **Keine Einfrierregel ist berührt:** Der Verweis trägt keinen
> Rechenwert und kein Rechenweg liest ihn; die geänderten Katalogflächen nutzt kein Referenzprojekt.
> Referenzlauf aller dreizehn Projekte gegen diese Basis: **13/13 PASS** (4 145 687 Werte, 387/387 CSV
> byte-gleich, außer `protokoll.txt`).

> **Die Vorgängerbasis `2026-09-23_R12_Gebaeudemodell`**, die erste Basis auf dem VDI-Weg, ist mit
> dieser Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zu G1 + G2 und
> den Nachträgen zu den Schemaständen 104 bis 113 und zum Zapfprofil-Testkatalog steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R14 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat vom 24. bis zum 25.09.2026 die Basis R14 beschrieben — Anlass
(Schlusswelle KU2 der Kühlung: Projekt 1017 mit Kälteerzeuger), die Tabelle für 1017 gegen R13 und die
Nachträge zu den Schemaständen 119 bis 139 (140 und 141 am Ende), mit denen die Basis ergebnisneutral geblieben ist. Er steht
hier im Wortlaut, weil diese Nachträge die Ergebnisneutralität der Schritte belegen; die Verweise sind auf
diesen Ort umgestellt.

**Abgelöst wurde R14 durch `2026-09-25_R15_Anlagenkopplung`** (Stufe AK1 der Anlagenkopplung, fünfte
Welle, vom Anwender am 25.09.2026 beauftragt: das Referenzprojekt 1047 mit Kopplung — eine Kopie von 1017
mit gekoppeltem Heizkreis und gekoppelter Kühlübergabe — kommt als vierzehntes Projekt dazu). Die
dreizehn Projekte von R14 bleiben byte-gleich; die Tabelle 1017 gegen 1047 steht im Abschnitt „Aktuelle
Basis“ von [`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-24_R14_Kaelteerzeuger/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046), **394 CSV**, **2 249 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (eingefroren auf
Schemastand **119**, LFS-SHA-256 `63cc2d64…`; heute Schemastand **139**, LFS-SHA-256 `f700e81e…`,
Nachträge unten). Gegen diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017, 1045,
1046) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und `EPOS.Kern.Tests/GebaeudeRueckwegTests` den
Tagesbilanz-Weg an Projekt 1040. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: die vierte und letzte Welle der Stufe KU2 der Kühlung** (vom Anwender am 24.09.2026 samt
> dem Einfrieren beauftragt). Eine Änderung an den Daten, zwei an der Wirtschaftlichkeit:
>
> 1. **Testdatenbank: das Referenzprojekt mit Kälteerzeuger** (Kühlkonzept 10.4, Einfrierregel „gesäte
>    Kältedaten" oben): Die Wärmepumpe von 1017 kühlt — vier Zellen und zehn Zeilen über
>    [`Skripte/kaelteerzeuger_1017_referenzprojekt.py`](../../../Referenzlaeufe/Skripte/kaelteerzeuger_1017_referenzprojekt.py):
>    `Tab_Einstellungen.Tool_3` leer → „Wärmepumpe" (Kaskadenplatz 3), Projektgerät 1017033
>    `Kuehlbetrieb` 0 → 1, `Kuehl_Vorlauf` NULL → 18, `Kuehl_Hilfsstromanteil` NULL → 0,05, dazu die
>    zehn Zeilen der gesäten Kühlkennlinie in `Tab_Kenndaten_Kuehlung` (die Tabelle war leer; SQLite
>    führt dazu die Zeile der Tabelle in `sqlite_sequence`). Sicherung vorher außerhalb des
>    Repositoriums; der Zellvergleich aller 132 Tabellen (10 498 993 Zellen) zeigt genau diese vier
>    Zellen, zehn Zeilen und die Sequenzzeile, die 14 Sichten und 208 Indizes gleich;
>    `integrity_check` ok, `foreign_key_check` leer, Größe unverändert 67 784 704 Byte, ein zweiter Lauf
>    des Skripts ändert nichts.
> 2. **E35 und die Reste von E34** — Grund- und Leistungspreis eines eigenen Zählers, der Preis des
>    vermiedenen Bezugs, die Bemessungsmenge nach § 9b StromStG, das Mengenszenario — ändern Kosten,
>    nicht die Simulation; der Referenzlauf führt keine Kosten, und kein Referenzprojekt trägt einen
>    Kühlträger. Vor der Datenänderung waren alle dreizehn Projekte gegen R13 byte-gleich.
>
> **Warum so** — Kaskadenplatz 3 hinter BHKW und Elektrokessel (ohne Platz rechnet die Maschine nicht,
> auf Platz 3 bleibt die Wärmeseite fast unverändert), Kühl-Vorlauf 18 °C (Flächenkühlung, sensible
> Kälte), Hilfsstromanteil 5 % (die Basis trägt den Zuschlag), kein Kühlträger (der Referenzfall ohne
> den Sonderweg aus E34), eine gesäte Kühlkennlinie aus runden, erfundenen Werten (der Katalogsatz des
> Geräts trägt keine), die Nennkühlleistung bleibt leer: Kühlkonzept 10.4.
>
> **Allein 1017 bewegt sich, zwölf Projekte bleiben byte-gleich** (alle Dateien):
>
> | 1017 | R13 | R14 |
> |---|---:|---:|
> | Kältebedarf [MWh/a] | 2,52 | 2,52 |
> | Kältedeckung durch die Wärmepumpe [MWh/a] | — | 2,48 (98,4 %) |
> | ungedeckte Kälte `Kaelterestbedarf` [MWh/a] | 2,52 | 0,04 |
> | Kältestrom, davon Hilfsstrom [MWh/a] | — | 0,55; 0,03 |
> | Jahresarbeitszahl Kälte (EER-Jahreswert) | — | 4,52 |
> | Kühltage | — | 43 |
> | Wärme der Wärmepumpe auf Platz 3 [MWh/a] | — | 0,04 |
> | Restwärme [MWh/a] | 0,14 | 0,10 |
> | Netzbezug `Stromrestbedarf` [MWh/a] | 655,31 | 655,88 |
>
> Der Kältestrom kommt ganz aus dem Netz (0,5487 von 0,5487 MWh/a): In den Kühlstunden deckt die
> Eigenerzeugung schon den übrigen Strombedarf nicht — Stromspeicher, BHKW und Elektrokessel bleiben
> Zeichen für Zeichen, wie sie waren; 1017 führt keine Photovoltaik. Kosten und CO₂ führt der
> Referenzlauf nicht; gerechnet (`ReferenzprojektKaelteerzeugerTests`) steigen die Stromkosten des
> Anschlusses um 283,55 €/a, der Kältestrom trägt 273,60 €/a und 0,24 t/a CO₂.
>
> **Dateien und Schlüssel:** 1017 bekommt die sieben Dateien der Wärmepumpe (`heizstab.csv`,
> `wp_produktion.csv`, `wp_quellentemperatur.csv`, `wp_restwaerme.csv`, `wp_strom.csv`,
> `wp_waermebedarf.csv`, `wp_warmwasserbedarf.csv`) und 42 Skalare (`Kaelte.*`, `Kaelte[0].*`,
> `Waermepumpe.*`, `WaermepumpeModul[0].*`, sieben Vektorsummen) — 387 → 394 CSV, 2 207 → 2 249
> Skalare. Gegen R13 meldet der Vergleich 1 689 Abweichungen, alle in 1017: die neuen Dateien und
> Schlüssel, die geänderten Skalare und die Reihen `reststrom_viertelstunde.csv` (1 660 geänderte
> Viertelstunden) und `restwaerme.csv` (19 geänderte Stunden).
>
> **Kein Fehlschlag, kein NaN, keine Ablehnung:** 13/13 Projekte gerechnet.
>
> **Einfrierregeln:** Die Regel „gesäte Kältedaten" umfasst jetzt die Kälteerzeugung. Emissionsfaktoren,
> PV-Modulkoeffizienten, Flottenstand 1046 und gesäte Gebäudedaten sind nicht berührt.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich** (394/394 CSV) und
> **GESAMT: PASS** gegen diese Basis (4 207 049 Werte); ebenso byte-gleich ein Lauf vor dem Zusammenführen
> mit der Szenariopflege E9b (#462) — sie bewegt kein Referenzprojekt.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-24_R14_Kaelteerzeuger
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis; die Abnahme der Stufe im
> [Protokoll der Schlusswelle KU2](../Protokolle/Gebaeudesimulation/2026-09-24_Schlusswelle_KU2.md)
> und im [Status der Gebäudesimulation](../../aktuell/Status_Gebaeudesimulation_VDI6007.md).

> **Nachtrag: Schemastände 120 und 121 (Zusammenführung mit E10 #463 und #468), die Basis bleibt.** Die
> Testdatenbank ist die Fassung von origin mit Schemastand **121** (LFS-SHA-256 `00fbbb8b…`; die
> Schritte 120 und 121 stehen mit ihren Nachträgen beim R13-Abschnitt unter `ueberholt/`), auf die
> [`Skripte/kaelteerzeuger_1017_referenzprojekt.py`](../../../Referenzlaeufe/Skripte/kaelteerzeuger_1017_referenzprojekt.py) erneut
> angewandt ist; `kuehlung_1017_referenzprojekt.py` und `gebaeude_10612_233_bauweise.py` finden nichts zu
> tun, ein zweiter Lauf des Skripts ändert nichts. Zellvergleich aller 132 Tabellen gegen die Fassung von
> origin (10 499 019 Zellen): genau die vier Zellen, zehn Zeilen und die Sequenzzeile der vierten Welle;
> 14 Sichten und 209 Indizes gleich, `integrity_check` ok, `foreign_key_check` leer, 67 792 896 Byte
> (LFS-SHA-256 `9acda529…`). Referenzlauf aller dreizehn Projekte gegen diese Basis: **13/13 PASS**
> (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag: Schemastände 122 und 123 (Anlagenkopplung, Stufe AK1 Welle 1), die Basis bleibt.** Die
> Testdatenbank steht über `Werkzeuge/Testdatenbankschema` auf Schemastand **123**: Schritt 122
> (`AK-S1`) legt die dreizehn Spalten der Wärmeübergabe an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`
> und baut `Abfrage_Projektgebaeude` ein drittes Mal neu (90 Spalten), dazu
> `Tab_Einstellungen.Anlagenkopplung` (Wertliste AUS/AK1/AK2/AK3, NULL = aus); Schritt 123 (`AK-S3`,
> Wärmeteil) die drei nullbaren Ergebnisspalten `Vorlauf_Mittel`, `Ruecklauf_Mittel` und
> `Uebergabe_Begrenzt_Stunden` an `Tab_ErgebnisEnergiebedarf`. Reines DDL: Sicherung vorher außerhalb
> des Repositoriums; der Zellvergleich aller 132 Tabellen gegen die Fassung 121 (10 499 091 Zellen)
> zeigt allein `SchemaVersion` 121 → 123, die 30 neuen Spalten sind leer (die vier Schalter 0), und
> nur die DDL der vier Tabellen und der Sicht hat sich geändert; 14 Sichten und 209 Indizes, 131 von
> 132 Tabellen STRICT, `integrity_check` ok, `foreign_key_check` leer, 67 792 896 Byte (LFS-SHA-256
> `1ba28e23…`). Ein zweiter Lauf des Werkzeugs legt nichts an; die Migration der Schale ergibt aus der
> Fassung 121 dieselbe DDL und dieselben Zellen. Kein Rechenweg liest die Spalten, und die drei
> Ergebnisspalten gehen erst mit einem Wert in `aggregate.csv` (`Referenzlauf/Ergebnisexport.cs`).
> Referenzlauf aller dreizehn Projekte gegen diese Basis: **13/13 PASS** (4 207 049 Werte, 394/394 CSV
> byte-gleich, außer `protokoll.txt`). Einfrierregeln sind nicht berührt; die Regel „gesäte
> Auslegungsdaten der Übergabe" (Anlagenkopplung 11.4) entsteht erst mit dem Referenzprojekt der
> Kopplung.

> **Nachtrag: Schemastand 119 (Kühlung, Stufe KU2, Welle 3), die Basis bleibt.** Migrationsschritt
> **119** (`SCHRITT_119_KAELTESTROM`: `Tab_Energieanlagen.Kuehl_EigenerZaehler` — 0/1 mit `CHECK`, nullbar,
> ohne Vorgabe, NULL = anteilig am Netzbezug, Entscheid E34 — und sieben nullbare Ergebnisspalten der
> Kälteseite an `Tab_ErgebnisWaermepumpe` (`Kaelteproduktion_WP`, `Stromverbrauch_Kuehlung`) und
> `Tab_ErgebnisWaermepumpeModul` (`Kaelteproduktion`, `Stromverbrauch_Kuehlung`, `Kaeltestrom_Netzbezug`,
> `Kuehl_carrier_id`, `Kuehl_EigenerZaehler`); Quelle `KuehlungSchema`), **reines DDL** (Kühlkonzept 6.1–6.4,
> 8.4). Er folgt auf die Schritte 115 (Zapfprofil, Nachtrag oben) und 116 bis 118 (Szenarioabdeckung der
> Wirtschaftlichkeit, E9a) und ist auf deren Fassung **118** nachgezogen, mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`; ein zweiter Lauf
> findet nichts offen. Zellvergleich aller 132 Tabellen (samt `sqlite_sequence`) gegen die Fassung 118
> (10 498 685 Zellen): `SchemaVersion` 118 → 119 und die acht neuen Spalten, alle NULL, sonst nichts; die 14
> Sichten und alle 208 Indizes unverändert. `integrity_check` ok, `foreign_key_check` leer, 131 von 131
> Fachtabellen STRICT, Größe unverändert 67 784 704 Byte. **Keine Einfrierregel ist berührt:** Die neuen
> Spalten tragen keinen gesäten Wert; die Ergebnisspalten schreibt nur ein Lauf mit Kältekaskade, und kein
> Referenzprojekt kühlt. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis
> (4 145 687 Werte, 387/387 CSV byte-gleich, außer `protokoll.txt`) (LFS-SHA-256 `6259b348…`).

> **Nachtrag E10 (#463): Schemastand 120 und der Anschluss der Speicherflotte an die
> Nutzungsdauertabelle, die Basis bleibt.** Migrationsschritt **120**
> (`SCHRITT_120_NUTZUNGSDAUER_SAETZE`, Nutzungsdauer-Konzept Stufe S3), **reines DML** an
> `Tab_Nutzungsdauer`: Die leeren Satzzellen der Standardzeilen bekommen die Mitte des
> Empfehlungsbereichs derselben Position der Betriebsvorlagen-Saat (Quelle `NutzungsdauerSaetze`) —
> fünf Zellen `Instandsetzung_Prozent`: Heizkessel · Wärmeerzeuger 2,0, BHKW · Modul 6,0,
> Wärmezentrale · Rohrleitungen 2,0, Stromeinspeisung · Netzanschluss 2,0, Bauliche Anlagen 1,25;
> `Wartung_Prozent` bleibt überall leer. Mit `Werkzeuge/Testdatenbankschema` auf der Fassung 119
> nachgezogen; ein zweiter Lauf setzt nichts (0/5). Zellvergleich gegen die Fassung 119: allein
> `SchemaVersion` 119 → 120 und diese fünf Zellen; `integrity_check` ok, `foreign_key_check` leer,
> Größe unverändert 67 784 704 Byte (LFS-SHA-256 `52c4729d…`). **Ergebnisneutral:** Ein Satz der
> Tabelle rechnet erst, wenn der Anwender ihn über „Sätze vorbelegen…" oder die Übernahme einer
> Kostenvorlage in eine Position schreibt; der Rechenweg liest weiter den Satz der Position.
>
> **Mit derselben Welle** rechnet die Wirtschaftlichkeit der Speicherflotten-STUDIE den Restwert je
> Einheit linear aus ihrer Nutzungsdauer auf der Ersatzkette der Flotte (Betrag der letzten
> Beschaffung × Restdauer ÷ Nutzungsdauer); eine Einheit ohne eigenes Ersatzintervall nimmt die
> Nutzungsdauer der Standardzeile „Stromspeicher · Batterie" (10 a), und der feste Restwert je
> Einheit ist ein Altfeld, das nicht mehr rechnet. Der Projektlauf rechnet keine
> Flottenwirtschaftlichkeit, und `aggregate.csv` führt für 1046 nur die Physik der Flotte — die
> Referenz bewegt sich nicht (dritte Einfrierregel, Absatz „Anschluss an die Nutzungsdauertabelle").
> Referenzlauf aller dreizehn Projekte gegen diese Basis: **13/13 PASS** (4 145 687 Werte, 387/387 CSV
> byte-gleich, außer `protokoll.txt`) — deshalb keine neue Basis R14.

> **Nachtrag: Schemastand 124 (Zapfprofilgenerator, Stufe Z4, Schemaschritt T3) und drei Setzungen des
> freien Paketteils, die Basis bleibt.** Migrationsschritt **124** (`SCHRITT_124_ZAPFPROFIL_LAUFANGABEN`;
> Quelle `TwwSchema.SpaltenT3`, die Wertemengen stehen je einmal in `TwwSchema` für DDL und Schreibweg): an
> `Tab_TwwProjekt` die Laufangaben der Auslegung `Erzeugerart` (1, 2), `Uebertrager_Werkstoff` (1, 2),
> `Personen_Auto` (0/1, Vorgabe 1), `Personen_Manuell` (≥ 0) und `Fuellstand_Bezug` (1 bis 4), an
> `Tab_TwwBedarfstag_STAMM` die `Bezugsart` (1 bis 7, nullbar) — alle mit `CHECK`, sonst nullbar.
> Nachgezogen auf der Fassung von origin mit Schemastand **123** (Nachtrag Anlagenkopplung oben,
> `1ba28e23…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` (sechs Spalten
> angelegt; ein zweiter Lauf legt nichts an), danach
> [`Skripte/tww_testkatalog_fiktiv.py`](../../../Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py) mit `--stochastik`: Es führt am
> Ecodesign-Zapfprofil L des freien Paketteils die Bezugsart 2 (Wohneinheiten) nach und spielt aus
> [`Katalogpaket_frei/Tab_TwwParameter_STAMM.csv`](../../../Referenzlaeufe/Katalogpaket_frei/LIESMICH.md) die drei Setzungen
> `Zapfprofil.Zirkulation.Hinweisverhaeltnis` (1,5), `Zapfprofil.Anzeigetemperatur` (45 °C) und
> `Zapfprofil.Stundenschwelle` (0,1 kW) nach der Regel der Testdatenbank ein — Setzungen von INEKON zur
> Bestätigung (ZU21) —, zusammen 3 angelegt, 1 nachgeführt; ein zweiter Lauf meldet 0/0. Zellvergleich
> aller 132 Tabellen gegen die Fassung 123 (10 503 133 Zellen): `SchemaVersion` 123 → 124, die sechs neuen
> Spalten — `Tab_TwwProjekt` ohne Zeile, die Bezugsart allein am Ecodesign-Tag gesetzt, an den drei
> fiktiven Tagen NULL —, die drei Zeilen in `Tab_TwwParameter_STAMM` (samt `sqlite_sequence`), sonst
> nichts; die 14 Sichten und alle 209 Indizes unverändert. `integrity_check` ok, `foreign_key_check` leer,
> Größe unverändert 67 792 896 Byte (LFS-SHA-256 `1d971b1a…`). **Keine Einfrierregel ist berührt:**
> Kein Referenzprojekt steht auf dem Generator. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen
> diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag E15 (#478): Schemastand 125 (Risikomodul der Wirtschaftlichkeit), die Basis bleibt.**
> Migrationsschritt **125** (`SCHRITT_125_RISIKOMODUL`; Quelle `SchemaKatalog.RisikomodulSpalten`), **reines DDL**
> an `Tab_ProjektWirtschaftlichkeit`: `Risiko_Art` (TEXT(10); leer = kein Risiko, `ZINS`, `ABZUG`),
> `Risiko_Zinszuschlag` [%-Punkte], `Risiko_Verlust` [€ je Periode] und `Risiko_Wahrscheinlichkeit` [%], nullbar,
> ohne Vorgabe (DIN EN 17463, 6.5 und Anhang F; Konzept Wirtschaftlichkeit § 2.11.2, V‑G7). Nachgezogen auf der
> Fassung **124** (Nachtrag oben, `1d971b1a…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: vier Spalten angelegt,
> keine Tabelle, Marker 125. `integrity_check` ok, `foreign_key_check` leer, Größe unverändert 67 792 896 Byte
> (LFS-SHA-256 `6c4c32f9…`); die vier Spalten sind in allen fünf Parameterzeilen leer. **Ergebnisneutral:** Leer
> heißt „kein Risiko", und kein Referenzprojekt trägt eines; die Wirtschaftlichkeit geht ohnehin nicht in
> `aggregate.csv`. **Keine Einfrierregel ist berührt.** Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen
> diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag: Schemastand 126 (Reparatur der Gebäude-Katalogsätze, Welle #485), die Basis bleibt.**
> Migrationsschritt **126** (`SCHRITT_GEBAEUDE_KATALOGREPARATUR`; die Nummer steht allein bei
> `GebaeudeKatalogReparatur.SCHRITT`, dort auch Anweisungen und Schadensbilder), **reines DML** an
> `Tab_Gebaeude_STAMM`, je Satz nach Bezeichner UND Schadensbild (Konzept Administrationsdialoge 7.1 (a)):
> `Krankenhaus_92-EnEV2016` U-Wert Fenster 0,09 → 1,3, Fensterfläche Nord 10 000 → 250 m², gesamte
> Fensterfläche 11 645,9 → 1 895,9 m² (Süd + Ost/West + Nord wie im Editor); „Fläche je Nutzer" leer →
> Wohnfläche ÷ Bewohner bei `EFH-BZ2` (40,0), `KrankenH-F-U-400` (50,0, Bewohner 360 → 360,24 wie die
> Geschwister), `KMEH-M-U-54` (31,78), `Z-EFH-A-S-126` (28,71); die acht Testreste 275–282
> (`Z2-EFH-A-S*`, `EFH-BZ2 XXX`) gelöscht — nur, weil keine Projektkopie sie über
> `ID_Gebaeude_Stamm` oder den Namen führt (ein benutzter Rest bliebe und stünde im Protokoll).
> Nachgezogen auf der Fassung 125 (Nachtrag E15 oben, `6c4c32f9…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` (offen
> vorher 14, danach 0; ein zweiter Lauf findet nichts). Zellvergleich aller Tabellen gegen die Fassung 125
> (10 503 021 Zellen): `SchemaVersion` 125 → 126, die acht Zellen der fünf berichtigten Sätze und die acht
> gelöschten Zeilen, sonst nichts; 355 Schemaobjekte unverändert, `integrity_check` ok,
> `foreign_key_check` leer, 67 788 800 Byte (LFS-SHA-256 `0fe67575…`). Der Katalog zählt 269 Sätze,
> und jeder besteht die Prüfung des Gebäudeeditors (Wächter
> `GebaeudeKatalogverweisTests.Nach_der_Reparatur_besteht_jeder_Katalogsatz_die_Editorpruefung`).
> **Keine Einfrierregel ist berührt:** Keiner der dreizehn Sätze ist einem Referenzprojekt zugeordnet,
> und Projektkopien bleiben unberührt. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese
> Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag E17 (#479): Schemastand 127 (nicht monetarisierbare Wirkungen der Wirtschaftlichkeit), die Basis bleibt.**
> Migrationsschritt **127** (`SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN`; Quelle `ProjektWirkungSchema` für Migration,
> Werkzeug und Testvorrichtung) legt die Tabelle `Tab_ProjektWirkung` an (STRICT; `ID`, `ID_Projekt` mit
> Fremdschlüssel auf `Tab_Projekt` ON DELETE/UPDATE CASCADE, `Sortierung`, `Kategorie` mit CHECK
> `ENERGIEFLUSS`/`FINANZIELL`/`SONSTIG`, `Beschreibung`, `Dauer` 1–3, `Wirkung_Organisation`, `Wirkung_Mitarbeiter`,
> `Wirkung_Umwelt` je 0–3, NULL = nicht beurteilt) samt Index `idx_ProjektWirkung_Projekt` und übernimmt einen
> gepflegten Freitext `Tab_ProjektWirtschaftlichkeit.Nicht_Monetaer` als eine Wirkung SONSTIG ohne Beurteilung
> (DIN EN 17463, 6.1 und 8.2; Konzept Wirtschaftlichkeit § 2.11.2, V‑G11). Der Schritt steht nach **126** (Reparatur
> der Gebäude-Katalogsätze, #485, Nachtrag Schemastand 126). Nachgezogen auf der Fassung **126** (`0fe67575…`,
> 67 788 800 Byte) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: nur Tabelle und Index
> neu, **0 Freitexte übernommen** (kein Referenzprojekt pflegt einen), Schritt 126 fand nichts offen, Marker 127;
> 132 Tabellen, alle STRICT, 14 Sichten, 210 Indizes; `integrity_check` ok, `foreign_key_check` leer, ein zweiter Lauf
> meldet den Schritt als stehend. Größe 67 796 992 Byte (LFS-SHA-256 `87e49ed1…`). **Ergebnisneutral:** Kein Rechenweg liest die Tabelle,
> und die Wirtschaftlichkeit geht ohnehin nicht in `aggregate.csv`. **Keine Einfrierregel ist berührt.** Referenzlauf
> aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer
> `protokoll.txt`).

> **Nachtrag AK1 Welle 3: Schemastand 128 (Heizkreis je Gebäude im Ergebnis, Anlagenkopplung), die Basis bleibt.**
> Migrationsschritt **128** (`SCHRITT_128_ERGEBNIS_HEIZKREIS`; die Nummer steht allein bei
> `ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS`, Quelle `ErgebnisGebaeudeSchema.SpaltenHeizkreis` für Migration, Werkzeug
> und Testvorrichtung) hängt vier nullbare Spalten an `Tab_ErgebnisGebaeude` (Muster E30; Konzept Anlagenkopplung 8.3,
> 9.4): `Uebergabe_Art` (CHECK `RADIATOR`/`FLAECHE`/`KONVEKTOR`; NULL = nicht gekoppelt gerechnet), `VorlaufMittel_C`,
> `RuecklaufMittel_C` und `UebergabeBegrenzt_H` (0 … 8 760) — **reines DDL**. Vergeben beim Merge mit origin
> (125 Risikomodul, 126 Katalogreparatur, 127 Wirkungen waren belegt); er steht nach **127**. Nachgezogen auf der
> Fassung **127** (Nachtrag E17 oben, `87e49ed1…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: vier Spalten neu, alle
> leer, Marker 128. Zellvergleich aller 133 Tabellen gegen die Fassung 127 (11 973 380 Zellen): allein `SchemaVersion`
> 127 → 128 und der Tabellentext von `Tab_ErgebnisGebaeude`; `integrity_check` ok, `foreign_key_check` leer, 133 Tabellen
> (132 STRICT), 14 Sichten, 210 Indizes. Größe 67 796 992 Byte (LFS-SHA-256 `81209c50…`). **Ergebnisneutral:** Kein
> Referenzprojekt rechnet gekoppelt, und der Referenzlauf exportiert die Tabelle nicht. **Keine Einfrierregel ist
> berührt.** Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394 CSV
> byte-gleich, außer `protokoll.txt`).

> **Nachtrag E16 (#484): Schemastand 129 (Wiederholperiode je Kostenposition der Wirtschaftlichkeit), die Basis bleibt.**
> Migrationsschritt **129** (`SCHRITT_WIEDERHOLPERIODE`; die Nummer steht allein bei `WiederholperiodeSchema.SCHRITT`,
> der Quelle für Migration, Werkzeug und Testvorrichtung) hängt die nullbare Spalte `Wiederholperiode_a` (INTEGER, ohne
> Vorgabe; leer, 0 und 1 = jährlich) an `Tab_ProjektWerte` und an `Tab_KostenVorlagePosition` (DIN EN 17463, 6.3.1
> „alle n Jahre"; Konzept Wirtschaftlichkeit § 2.11.2, V‑G3) — **reines DDL**. In Phase 1 vorläufig 128; nach dem Push
> des Schritts 128 (Nachtrag AK1 Welle 3 oben) steht er als **129** nach 128, der Zwischenstand 127 → 128 der Welle
> (`f4a6ee8b…`) ist überholt. Nachgezogen auf der Fassung **128** (`81209c50…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: zwei Spalten neu, keine
> Tabelle, Schritt 128 fand nichts offen, Marker 129; 133 Tabellen (132 STRICT), 14 Sichten, 210 Indizes;
> `integrity_check` ok, `foreign_key_check` leer. Größe 67 796 992 Byte, unverändert (LFS-SHA-256 `4c546a7c…`).
> **Ergebnisneutral:** Keine Zeile pflegt `Wiederholperiode_a`, leer rechnet jährlich wie vor dem Schritt, und die
> Wirtschaftlichkeit geht ohnehin nicht in `aggregate.csv`. **Keine Einfrierregel ist berührt.** Referenzlauf aller
> dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag #493: Schemastand 130 (Anschlusslängen im Gebäudekatalog), die Basis bleibt.**
> Migrationsschritt **130** (`SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN`; die Nummer steht allein bei
> `GebaeudeAnschlusslaengenReparatur.SCHRITT`, der Quelle für Migration, Werkzeug und Testvorrichtung; der Schritt der
> Zapfprofil-Sitzung folgt ihm) berichtigt nach Anwenderentscheid vom 24.09.2026 („Ersetzt durch plausible Werte“)
> 20 Zellen an sieben Sätzen von `Tab_Gebaeude_STAMM` — **reines DML, je Satz, Spalte und Schadensbild** (Bezeichner und
> unplausibler Wert ± 0,05), nichts gelöscht, Projektkopien unberührt:
>
> | Satz | Spalte | vorher | nachher | Herleitung |
> |---|---|---|---|---|
> | 79 `Krankenhaus_92-EnEV2016` | `Abmessung_Anschluß_Fenster_Wand` | 1 800 | 4 812,0 m | Laibung je m² Fenster des Ausgangssatzes 78 `KrankenH_NE` 7 655,75 / 3 016,3 = 2,5381 m/m² × 1 895,9 m² |
> | 79 | `Flaeche_Außenwand` | 12 094 | 13 214,4 m² | Hüllfläche der Geometrie von 78: 12 094 + 3 016,3 = 15 110,3 m² minus Fenster 1 895,9 m² (Ost/West bleibt 400) |
> | 80–83 `KrankenH-F-*`, 37 `gr_Hotel-G-134` | `Abmessung_Anschluß_Fenster_Wand` | 243,7 | 7 879,0 m | Tausch mit der Dachkante trägt: 7 879 / 3 062,3 = 2,573 m/m² (78: 2,538); 243,7 m wären 0,08 m/m² |
> | dieselben | `Abmessung_Anschluß_Wand_Dach` | 7 879 | 313,8 m | Umfang der Grundfläche 1 469 m² wie bei 78 (Wand–Dach = Keller = 313,8). Der Tausch (243,7 m) trägt hier nicht: Hülle 13 156,3 m² / 243,7 m = 54,0 m = 4,40 m je Geschoss (12,26 Geschosse, Raumhöhe 2,55 m); mit 313,8 m 3,42 m je Geschoss (78: 3,54 m bei 3,0 m) |
> | dieselben | `Abmessung_Anschluß_Außenwand_Kellerdecke` | 1 392,8 | 313,8 m | Umfang wie bei 78; 1 392,8 m wären das 4,4-Fache |
> | 77 `Kaufhaus` | `Abmessung_Anschluß_Fenster_Wand` | 243,7 | 5 820,8 m | Abwandlung der F-Sätze (Fenster 2 262,36 m²); Tausch gäbe 3,48 m/m², daher Verhältnis der F-Quelle 2,5729 m/m² × 2 262,36 m² |
> | 77 | `Abmessung_Anschluß_Wand_Dach`, `…_Außenwand_Kellerdecke` | 7 879 / 1 392,78 | 313,8 / 313,8 m | Umfang der Grundfläche 1 469 m² wie bei 78 und den F-Sätzen |
>
> Nachgezogen auf der Fassung **129** (Nachtrag E16 oben, `4c546a7c…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`: offen vorher
> 20, berichtigt 20, offen danach 0, Marker 130. Zellvergleich aller 133 Tabellen gegen die Fassung 129: allein
> `SchemaVersion` 129 → 130 und die 20 Zellen der Tabelle; Schema unverändert; `integrity_check` ok, `foreign_key_check`
> leer, 133 Tabellen (132 STRICT), 14 Sichten, 210 Indizes. Größe 67 796 992 Byte (LFS-SHA-256 `f8fe1b76…`).
> **Ergebnisneutral:** Keinen der sieben Sätze führt ein Projekt der Testdatenbank (weder über `ID_Gebaeude_Stamm` noch
> über den Namen); die dreizehn Referenzprojekte führen die Sätze 125, 129, 142–146, 233, 56. **Keine Einfrierregel ist
> berührt.** Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte).

> **Nachtrag: Schemastand 131 (Zapfprofilgenerator, Stufe Z4b, Schemaschritt T3 „Typtage"), die Basis
> bleibt.** Migrationsschritt **131** (`SCHRITT_131_ZAPFPROFIL_TYPTAGE`; Quelle
> `TwwSchema.AnweisungenT3Typtage`): die Tabelle `Tab_TwwTyptag_IMPORT` — STRICT, elf Spalten (`ID`,
> `Art`, `Klimazone`, `Gebaeudeart`, `Typtag`, `Aufloesung_min`, `Zeilenindex`, `Wert`, `Quelle`,
> `Ausgabe`, `Datum_Import`), natürlicher Schlüssel (`Art`, `Klimazone`, `Gebaeudeart`, `Typtag`,
> `Zeilenindex`), **kein `Status` und kein `ReadOnly`**, kein Fremdschlüssel. Sie nimmt die Typtage auf,
> die der **lizenzierte Anwender** selbst einspielt; das Repositorium bringt keine Zeile mit (Konzept
> Kapitel 6), und die Auslieferungsvorlage leert sie. Im SELBEN Schritt stehen die drei Projektspalten
> der Wahl an `Tab_TwwProjekt` — `Typtage_Aktiv` (0/1, Vorgabe 0), `Typtage_Klimazone` und
> `Typtage_Gebaeudeart` (beide NULL = keine Wahl), Quelle `TwwSchema.SpaltenT3Typtage`. Die Nummer war in
> der Arbeit 125; beim Zusammenführen mit origin waren 125 bis 129 belegt (Risikomodul, Gebaeude-
> Katalogreparatur, Wirkungen, Heizkreis, Wiederholperiode) und mit der Welle #493 auch 130
> (Anschlusslängen im Gebäudekatalog); der Schritt steht jetzt nach **130**.
> Nachgezogen auf der Fassung von origin mit Schemastand **130** (Nachtrag #493 oben,
> `f8fe1b76…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` (eine
> Tabelle und drei Spalten angelegt; ein zweiter Lauf legt nichts an), danach
> `tww_testkatalog_fiktiv.py --stochastik` (0 angelegt, 0 nachgeführt — der Testkatalog stand schon
> vollständig da; zweiter Lauf 0/0). **Kein DML:** Die Tabelle ist und bleibt LEER, `Typtage_Aktiv`
> steht auf 0 und beide Angaben auf NULL. Zellvergleich aller Tabellen gegen die Fassung 130
> (10 502 791 Zellen): allein `SchemaVersion` 130 → 131, die drei neuen Spalten und der Tabellentext von
> `Tab_TwwProjekt`; `integrity_check` ok, `foreign_key_check` leer, 134 Tabellen (133 STRICT) statt 133
> (132), 14 Sichten und 211 Indizes unverändert. Größe 67 805 184 Byte (LFS-SHA-256 `a4a88c33…`).
> **Keine Einfrierregel ist berührt:** Kein Referenzprojekt steht auf dem Generator, und ohne
> eingespielte Typtage ist der Typtagweg benannt nicht verfügbar.
> Referenzlauf der fünf CI-Projekte (1030, 1007, 1017, 1045, 1046) **5/5 PASS** gegen diese Basis,
> 160/160 CSV **byte-gleich**.

> **Nachtrag G3 Welle B: Schemastände 132 bis 134 (Baustoffe, Bauteilaufbauten, Zonen der Stufe G3),
> die Basis bleibt.** Drei Migrationsschritte; die Nummer steht allein bei `BaustoffSchema.SCHRITT`,
> `BauteilaufbauSchema` und `ZonenSchema` zählen davon weiter (Quelle für Migration, Werkzeug und
> Testvorrichtung): **132** (S-A) legt `Tab_Baustoff_STAMM` und `Tab_Baustoff` an — spaltengleich, mit
> der Spalte `Hersteller`, die Projektkopie mit Fremdschlüssel auf `Tab_Projekt` — und sät 132 Zeilen:
> 65 herstellerneutrale Stoffe und 67 Herstellerprodukte (E39), alle `ReadOnly = 1`; **133** (S-B) legt
> `Tab_Bauteilaufbau(_STAMM)` und `Tab_Bauteilschicht(_STAMM)` an; **134** (S-C) `Tab_Zone` samt den
> Blöcken aus KU-S1 und AK-S1 und `Tab_Bauteil` — DDL und die eine Saat des Baustoffkatalogs. In der
> Arbeit trugen die Schritte die Nummern 130 bis 132, dann 131 bis 133; beim Zusammenführen mit origin
> waren 129 bis 131 belegt. Nachgezogen auf der Fassung von origin mit Schemastand **131** (Nachtrag
> Z4b oben, `a4a88c33…`) mit `Werkzeuge/Testdatenbankschema`: 141 Tabellen ohne `sqlite_sequence`
> (alle STRICT) statt 133, 215 Indizes, 25 Projekte, 132 Saatzeilen im Baustoffkatalog, die übrigen
> neuen Tabellen leer. Größe 67 883 008 Byte (LFS-SHA-256 `9d3c006a…`). **Ergebnisneutral, keine
> Einfrierregel berührt:** Kein Referenzprojekt hat eine Zone, und ohne Zone rechnet jedes Gebäude
> bitgleich den Klassenweg; der Baustoffkatalog gehört nicht zu den gesäten Gebäudedaten. Referenzlauf
> aller dreizehn Projekte **13/13 byte-gleich** gegen diese Basis. Eine Einfrierregel „gesäte
> Zonendaten" kommt mit dem ersten Referenzprojekt mit Zone (G6d, Mehrzonenkonzept 8.3).

> **Nachtrag AK1 Welle 4 (E37): Schemastände 135 bis 137 (Kälteseite der Anlagenkopplung), die Basis
> bleibt.** Drei Migrationsschritte, die Nummern stehen allein bei `KuehluebergabeSchema` (Quelle für
> Migration, Werkzeug und Testvorrichtung): **135** (`SCHRITT_KUEHLUEBERGABE`, KAK-S1) legt die acht
> Spalten der Kühlübergabe an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` — `Kuehluebergabe_Aktiv` (0/1,
> Vorgabe 0), `Kuehl_Uebergabe_Art`, `Kuehl_Uebergabe_Exponent`, `Kuehl_Uebergabe_Leistung_Nenn`,
> `Kuehl_Auslegung_Vorlauf`, `Kuehl_Auslegung_Ruecklauf`, `Kuehl_Auslegung_Raumtemperatur`,
> `Kuehl_Vorlaufgrenze`, sonst alle NULL — und baut die Sicht `Abfrage_Projektgebaeude` zum vierten Mal
> neu (98 Spalten, die 90 davor an ihren Stellen); **136** (`SCHRITT_KUEHLUEBERGABE_ERGEBNIS`, KAK-S3)
> hängt `Kuehl_Vorlauf_Mittel`, `Kuehl_Ruecklauf_Mittel` und `Kuehl_Uebergabe_Begrenzt_Stunden` an
> `Tab_ErgebnisEnergiebedarf` und `Kuehl_Uebergabe_Art` (CHECK der drei Arten), `KuehlVorlaufMittel_C`,
> `KuehlRuecklaufMittel_C`, `KuehlUebergabeBegrenzt_H` und `KuehlVorlaufgrenze_H` (0 … 8 760) an
> `Tab_ErgebnisGebaeude`; **137** (`SCHRITT_KUEHLUEBERGABE_ZONE`) hängt `Kuehl_Uebergabe_Art` (CHECK samt
> `IDEAL`), `Kuehl_Uebergabe_Exponent` und `Kuehl_Uebergabe_Leistung_Nenn` an `Tab_Zone` — **reines
> DDL**. Sie stehen nach S-C (134, Stufe G3). Die drei Energiebedarf-Spalten gehen erst mit einem Wert in
> `aggregate.csv` (`SpaltenNurMitWert`). Nachgezogen auf der Fassung von origin mit Schemastand **134**
> (G3 Welle B, `9d3c006a…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: 27 Spalten
> neu, Marker 137; ein zweiter Lauf legt nichts an. Zellvergleich aller Tabellen gegen die Fassung 134
> (10 504 069 Zellen): allein `SchemaVersion` 134 → 137, die 27 neuen Spalten (der Schalter 0, alles
> andere NULL) und die Sicht; `integrity_check` ok, `foreign_key_check` leer, 141 Tabellen (alle STRICT),
> 14 Sichten, 215 Indizes. Größe 67 887 104 Byte (LFS-SHA-256 `834aa718…`). **Ergebnisneutral:** Kein
> Referenzprojekt rechnet gekoppelt. **Keine Einfrierregel ist berührt**; die Einfrierregel der
> Auslegungsdaten der Wärme- und Kühlübergabe samt `Kuehluebergabe_Aktiv` kommt mit der fünften Welle von
> AK1. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394
> CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag G4c Welle 3: Schemastand 138 (Herkunftsablage der Gebäudeimporte, Schritt S-F), die Basis bleibt.**
> Migrationsschritt **138** (`SCHRITT_IMPORTZUORDNUNG`; die Nummer steht allein bei `ImportzuordnungSchema.SCHRITT`,
> der Quelle für Migration, Werkzeug und Testvorrichtung) folgt den Mehrzonenschritten 132–134 der Stufe G3 und der
> Kälteseite 135–137 (AK1 Welle 4) und legt zwei leere STRICT-Tabellen an — **reines DDL, keine Saat**:
> `Tab_Importquelle` (eine Zeile je Importlauf: Gebäude mit Kaskade, Format `IFC`/`GBXML`, Dateiname ohne Pfad,
> SHA-256, Größe, Schemastand, Zeitpunkt, Programmfassung, Zonenregel, Zahl fehlender Entitäten) und
> `Tab_Importzuordnung` (eine Zeile je Paarung: Quelle mit Kaskade, fünf nullbare Zielverweise auf Gebäude, Zone,
> Bauteil, Aufbau und Baustoff, genau einer gesetzt, ebenfalls mit Kaskade — Begründung der Abweichung von
> Softwarearchitektur 2.2 im Kopf von `ImportzuordnungSchema` —, Quellkennung bis 64 und Quelltyp bis 40 Zeichen)
> samt den Indizes über `ID_Importquelle` und `Quellkennung`. In der Arbeit trug der Schritt die Nummer 135; beim
> Zusammenführen mit origin waren 135 bis 137 von der Kälteseite belegt.
> Nachgezogen auf der Fassung von origin mit Schemastand **137** (Nachtrag AK1 Welle 4 oben, `834aa718…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite` (zwei Tabellen
> angelegt; ein zweiter Lauf legt nichts an). Zellvergleich aller 141 Tabellen gegen die Fassung 137 (11 977 604 Zellen):
> allein `SchemaVersion` 137 → 138; neu sind die zwei leeren Tabellen und die zwei Indizes, kein bestehender Tabellen-,
> Sicht- oder Indextext ist geändert; `integrity_check` ok, `foreign_key_check` leer, 143 Tabellen (alle STRICT) statt
> 141, 14 Sichten, 217 Indizes statt 215. Größe 67 903 488 Byte (LFS-SHA-256 `3f5c892d…`). **Ergebnisneutral, keine
> Einfrierregel berührt:** Kein Rechenweg liest die Tabellen, und kein Referenzprojekt führt einen Import.
> Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich,
> außer `protokoll.txt`).

> **Nachtrag G4a Welle 3: Schemastand 139 (Baujahr des Gebäudes), die Basis bleibt.** Migrationsschritt **139**
> (`SCHRITT_BAUJAHR`; die Nummer steht allein bei `BaujahrSchema.SCHRITT`, der Quelle für Migration, Werkzeug und
> Testvorrichtung, die Definitionen bei `GebaeudeSchema`) folgt auf S-F (138) und legt an `Tab_Gebaeude` und
> `Tab_Gebaeude_STAMM` spaltengleich die Spalte `Baujahr INTEGER` mit
> `CHECK (Baujahr IS NULL OR Baujahr BETWEEN 1500 AND 2100)` an und baut die Sicht `Abfrage_Projektgebaeude` zum
> fünften Mal neu (99 Spalten, die 98 von KAK-S1 an ihren Stellen, `Baujahr` an Stelle 98) — **reines DDL, keine
> Saat**. Nachgezogen auf der Fassung von origin mit Schemastand **138** (Nachtrag G4c Welle 3 oben, `3f5c892d…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite` (zwei Spalten
> angelegt; ein zweiter Lauf legt nichts an). Zellvergleich aller 143 Tabellen gegen die Fassung 138 (10 506 686 Zellen
> der gemeinsamen Spalten): allein `SchemaVersion` 138 → 139; neu ist die Spalte `Baujahr` in beiden Gebäudetabellen,
> in allen 26 bzw. 269 Zeilen NULL, geändert allein der Text der Sicht; `integrity_check` ok, `foreign_key_check` leer,
> 143 Tabellen (alle STRICT), 14 Sichten, 217 Indizes. Größe 67 903 488 Byte (LFS-SHA-256 `f700e81e…`).
> **Ergebnisneutral, keine Einfrierregel berührt:** Die Spalte bleibt in jeder Zeile NULL, es ist also nichts gesät,
> und kein Rechenweg liest sie — weder der Eingangsbauer des Gebäudemodells noch der Tagesbilanz-Weg noch eine Vorgabe
> (die Vorgaben hängen an der Baualtersklasse, nicht am Jahr); sie gehört damit nicht zu den Spalten des Gebäudemodells,
> die die Einfrierregel „gesäte Gebäudedaten“ nennt. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese
> Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Die Vorgängerbasis `2026-09-23_R13_Kuehlung`**, die erste Basis mit Kühlung, ist mit dieser
> Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zu E32 und dem
> Referenzprojekt mit Kühlung und den Nachträgen zu den Schemaständen 114 bis 121 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

**Zwei Nachträge nach dem ersten Einfrieren von R15.** Die Stufe Z5 des Zapfprofilgenerators
(Schemastand 140) und #496 (Schemastand 141) sind auf origin gegen R14 gemessen worden, während R15
noch nicht veröffentlicht war; R15 ist danach auf Schemastand 141 neu eingefroren worden. Beide
Nachträge stehen deshalb hier, im Wortlaut:

> **Nachtrag Stufe Z5: Schemastand 140 (Zapfprofilgenerator, Schemaschritt T4 „Messreihen"), die
> Basis bleibt.** Ein Migrationsschritt, die Nummer steht allein bei
> `TwwSchema.SCHRITT_T4_MESSREIHEN` (Quelle für Migration, Werkzeug und Testvorrichtung; sie folgt
> lückenlos auf das Baujahr des Gebäudes, 139): **140** (`SCHRITT_140_ZAPFPROFIL_MESSREIHEN`) legt
> `Tab_TwwMessreihe` an — STRICT, zehn Spalten, eine Zeile je Wert, natürlicher Schlüssel
> (`ID_Projekt`, `Bezeichnung`, `Zeilenindex`), `ID_Projekt` mit `ON DELETE CASCADE`, kein `Status`
> und kein `ReadOnly` — samt ihrem Index auf `ID_Projekt`. **Reines DDL;** die Tabelle entsteht LEER
> und bleibt es: Gemessene Reihen gehören dem Objekt (Konzept Kapitel 9 K5), das Repositorium bringt
> keine mit, und ohne eingespielte Messreihe ist der Vergleich benannt nicht verfügbar.
> In der Arbeit trug der Schritt zuerst die Nummer 138, dann 139; beim Zusammenführen mit origin
> war 138 vom Schritt S-F der Gebäudeimporte und 139 vom Baujahr der Stufe G4a belegt — wer zuerst
> schiebt, hält die Nummer.
> Nachgezogen auf der Fassung von origin mit Schemastand **139** (Nachtrag G4a Welle 3 oben,
> `f700e81e…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`:
> 1 Tabelle und 1 Index neu, Marker 140; ein zweiter Lauf legt nichts an. Danach der fiktive
> Testkatalog der Stufen Z0 bis Z5
> (`py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite --stochastik`):
> 14 Zeilen neu, 28 nachgeführt — fünf Tagesgangsätze, 20 Tagesgänge, acht Nutzungsarten, vier
> Bedarfstage mit 33 Ereignissen, 85 Parameter, fünf DIN-4708-Werte und 24 Zapfkategorien, alle
> `FIKTIV` oder „(abgeleitet)"; kein Normwert, kein Herstellerwert, keine Projektzeile. Ein zweiter
> Lauf schreibt nichts (0 neu, 0 nachgeführt).
> `integrity_check` ok, `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten,
> 205 Indizes (219 samt den von SQLite angelegten). Größe 67 923 968 Byte
> (LFS-SHA-256 `5de448e8…`). Zellvergleich aller 143 gemeinsamen Tabellen gegen die Fassung von
> origin (10 506 805 Zellen): abweichend allein `Tab_Applikation.SchemaVersion` (139 → 140) und die
> Katalogzeilen des Skripts (`Tab_TwwNutzungsart_STAMM` 8 statt 7, `Tab_TwwParameter_STAMM` 85 statt
> 80, `Tab_TwwZapfkategorie_STAMM` 24 statt 28 — die Stufe Z5 führt die Nichtwohnen-Nutzungsarten
> mit zwei statt vier Kategorien); `Tab_TwwMessreihe` steht mit 0 Zeilen. **Ergebnisneutral:** Kein
> Referenzprojekt führt eine Messreihe, und kein Rechenweg der dreizehn liest den Tww-Katalog.
> **Keine Einfrierregel ist berührt.**

> **Nachtrag #496: Schemastand 141 (Folgeberichtigung im Gebäudekatalog), die Basis bleibt.**
> Migrationsschritt **141** (`SCHRITT_GEBAEUDE_FOLGEREPARATUR`; die Nummer steht allein bei
> `GebaeudeAnschlusslaengenFolgereparatur.SCHRITT`, der Quelle für Migration, Werkzeug und Testvorrichtung; er folgt
> auf die Messreihen, 140) berichtigt nach Anwenderauftrag vom 25.09.2026 („setze um: weiteren Scan-Kandidaten mit
> vertauschten Anschlusslängen, die Außenwand des Kaufhauses“) 39 Zellen an zwanzig Sätzen von `Tab_Gebaeude_STAMM` —
> **reines DML** in der Bauart von #493 (Bezeichner und unplausibler Wert ± 0,05 je Spalte, dieselben Anweisungen),
> nichts gelöscht, Projektkopien unberührt. **Regel der Herleitung:** (1) führt ein Satz gleicher Geometrie
> (Ausgangssatz, nicht die gerundete EnEV-Abwandlung) den Umfang, gilt er; (2) sonst der Tausch von Laibung und
> Dachkante, wenn er für beide Spalten trägt (Laibung im Band des Katalogs 1,2 … 3,4 m je m² Fenster, Median 2,57;
> Dachkante nicht unter der Quadratkante 4·√Grundfläche und nahe dem Umfang U = (Außenwand + Fenster) /
> (Nutzfläche / Grundfläche × Raumhöhe)); (3) sonst Laibung = Verhältnis der Quelle × Fensterfläche, Dachkante =
> Umfang aus der eigenen Geometrie. ΔH_T = Σ ψ·ΔL bzw. U_AW·ΔA je Satz.
>
> | Satz | Spalte | vorher | nachher | Herleitung | ΔH_T |
> |---|---|---|---|---|---|
> | 2, 4, 5, 7, 9, 10 `AltenH-C-*`, `Pflegeheim-C-*`; 92 `Schule-C-U-202` | `Abmessung_Anschluß_Fenster_Wand` / `…_Wand_Dach` | 185 / 985 | 985 / 185 m | Regel (2), Tausch: Laibung 1,81 m/m² (185 m wären 0,34); Umfang der Geometrie (2 132 + 545) / (3 020 / 540 × 2,55) = 187,7 m neben 185 m, Quadratkante 93,0 m | +70,4 W/K (ψ 0,228 / 0,14) |
> | 94 `Schule-NE1`, 96 `Schule-NE-66` | dieselben | 185 / 985 | 985 / 185 m | dieselbe Geometrie wie die Heime | −48,0 W/K (ψ 0,04 / 0,10) |
> | 17 `Hallenbad-652`, 20 `Hallenbad-Sauna-750` | dieselben | 185 / 985 | 985 / 185 m | Regel (2): Laibung 2,35 m/m² (420 m² Fenster); Dachkante zwischen Quadratkante 132,7 m und Umfang der Geometrie 206,3 m (1 100 m², 78,5 × 14,0 m) | +70,4 W/K |
> | 54 `Hotel-F-228` | `Abmessung_Anschluß_Wand_Dach` | 5 380,75 | 116,16 m | Regel (1): `Kaufhalle_NE` (76) hat dieselbe Geometrie (Wand 834, Fenster 243,4, Dach 473,7, Grund 480,8, Nutzfläche 1 138 m²) und führt Wand–Dach = Keller = 116,16 m (48,08 × 10,0 m; Hülle 1 077,4 m² / 116,16 m = 9,28 m = drei Geschosse zu 3,09 m); 5 380,8 m wären das 61,8-Fache der Quadratkante | −1 316,1 W/K (ψ 0,25) |
> | 69 `ml_Hotel-F-228`, 71 `ml-Hotel-F-228` | `Abmessung_Anschluß_Wand_Dach` | 5 380,8 | 116,16 m | wie 54 | −1 316,2 W/K |
> | 54, 69, 71 | `Abmessung_Anschluß_Außenwand_Kellerdecke` | 40 | 116,16 m | wie 54 — der Ausgangssatz führt die Kellerkante als Umfang; 40 m wären weniger als die halbe Quadratkante. Laibung 515,2 m (2,12 m/m²) bleibt | +38,1 W/K (ψ 0,50); je Satz −1 278,1 W/K |
> | 42 `Hotel_G_96`, 72 `ml-Hotel-G-096`, 134 `GMH-G-U-97` | `Abmessung_Anschluß_Fenster_Wand` / `…_Wand_Dach` | 86,6 / 295,5 | 295,5 / 86,6 m | Regel (2): Laibung 1,24 m/m² (unterer Rand des Katalogs wie `GMH KfW 55` mit 1,20; 86,6 m wären 0,36); Umfang der Geometrie (433 + 237,6) / (1 263 / 431,2 × 2,61) = 87,7 m, Quadratkante 83,1 m | +31,3 W/K (ψ 0,22 / 0,07) |
> | 84 `GMH-BZ_T`, 85 `GMH-J-015` | `Abmessung_Anschluß_Fenster_Wand` | 86,6 | 382,6 m | Regel (3): dieselben Längen auf fremder Geometrie, der Tausch trägt nicht (0,96 m/m²; 86,6 m lägen unter der Quadratkante 88,1 m) — Verhältnis von 42 nach dem Tausch 295,5 / 237,6 = 1,2437 m/m² × 307,6 m² | +65,1 W/K |
> | 84, 85 | `Abmessung_Anschluß_Wand_Dach` | 295,5 | 122,3 m | Umfang der Geometrie (633 + 307,6) / (1 430 / 485,2 × 2,61) = 122,28 m (51,8 × 9,4 m) | −12,1 W/K; je Satz +53,0 W/K |
> | 77 `Kaufhaus` | `Flaeche_Außenwand` | 10 093,99 | 1 820,9 m² | die 10 094 m² stammen aus den F-Sätzen (Nutzfläche 18 012 m², zwölf Geschosse); eigene Geometrie: 4 201 / 1 468,97 = 2,86 Geschosse × 4,55 m = 13,01 m, Hülle 313,8 m × 13,01 m = 4 083,2 m² minus Fenster 2 262,36 m² (Fensteranteil 55 %). Wand–Dach = Keller = 313,8 m aus #493 bleiben: einziger belegter Umfang dieser Grundfläche (`KrankenH_NE`), trägt die Fensterfläche (mindestens 173,9 m Fassade); die 10 094 m² ergäben 949,6 m Umfang — eine Grundfläche von 3 m Tiefe | −4 963,9 W/K (U 0,6) |
>
> **Nicht geändert, berichtet.** Kellerkanten: 0 m bei den Heimen, Schulen und Hallenbädern (die
> EnEV-Abwandlungen 3, 8 führen 140 m bei 200 m Dachkante, nicht den Umfang; ψ der C-Sätze 0), 14,6 m bei 42, 72,
> 134, 84, 85 (kein Ausgangssatz führt sie als Umfang; Vorschlag: der Umfang 86,6 bzw. 122,3 m; bei ψ 0,65/0,67
> +46,8 bis +48,2 bzw. +72,2 W/K; **✔ erledigt mit #505**) — der Katalog führt die Kellerkante systematisch klein. Dazu die
> Scan-Gruppen, Entscheidung beim Anwender (**alle zehn Zeilen ✔ erledigt mit #505**, Nachtrag unten):
>
> | Satz | Befund | Vorschlag Laibung | Herleitung des Vorschlags |
> |---|---|---|---|
> | 6 `Pflegeheim-122-EnEV2016` ✔ #505 | 0 m bei 545 m² Fenster (Kanten 40 / 30 m) | 540 m | EnEV-Abwandlung 8 gleicher Geometrie: 540 / 545 = 0,99 m/m² (nach dem C-Verhältnis 1,81 wären es 985 m) |
> | 15 `Industriehalle-320` ✔ #505 | alle drei Längen 0 | 16 000 m | Satz 14 `Industrie_ne_81` gleicher Geometrie: 2,5 m/m² × 6 400 m²; Kanten dort 7 337,4 m |
> | 43 `Hotel_H_BZ`, 64 `kl_Hotel-H-086` ✔ #505 | alle drei Längen 0 | 391,5 m | Sätze 65, 73 gleicher Geometrie: 2,5 m/m² × 156,6 m²; Kanten dort 71,0 m |
> | 117 `Verw_H_75` ✔ #505 | alle drei Längen leer | 391,5 m | Satz 118 `Verw_I_33` gleicher Geometrie; Kanten dort 70,98 m |
> | 105 `Büro1-F-U-89`, 107 `Bürogebäude_F_72` ✔ #505 | alle drei Längen leer, auch ψ | 1 462,1 m | Verwaltung F (115 `Verw_F_147`): 2,901 m/m² × 504 m²; Umfang der Geometrie 103,4 m |
> | 106 `Bürogebäude KfW 55` ✔ #505 | alle drei Längen 0 | 2 875 m | Verhältnis der NE-/I-Sätze 2,5 m/m² × 1 150 m²; Umfang der Geometrie 264,1 m |
> | 207 `KMH-G-U-120` ✔ #505 | Laibung 0 (Kanten 250,68 / 28 m) | 238,3 m | KMH G (206, 209): 268,6 / 112,02 = 2,398 m/m² × 99,37 m² |
> | 23 `Hallenbad-Umkl-140-EnEV2016` ✔ #505 | 50 m (0,12 m/m²), gerundet | 865,1 m | Hallenbad-Umkleide 24: 142 / 70,4 = 2,017 m/m² × 428,9 m² |
> | 34 `gr_Hotel-80-EnEV2016` ✔ #505 | 600 m (0,25 m/m²), gerundet | 6 164,4 m | F-Quelle gleicher Geometrie 2,5729 m/m² × 2 395,9 m² |
> | 108 `Bürogebäude_gross-30-EnEV2016` ✔ #505 | 330 m (0,18 m/m²), gerundet | 4 460 m | Verhältnis der NE-/I-Sätze 2,5 m/m² × 1 784 m² |
>
> „Nur Dachkante“ (35, 39–41, 47–49, 52, 55, 58, 61, 63, 66, 67, 112–114, 127, 128, 130, 144–146, 151, 169, 173,
> 189–191, 195–197, 205, 206, 209–213, 274; 4,7- bis 8-fache Quadratkante, vermutlich geneigte Dächer) bleibt
> unberührt, darunter die eingefrorenen Referenzsätze 145 und 146. Nebenbefunde ohne Scan-Eintrag: Laibung 0,64 /
> 0,46 / 0,32 m/m² bei 46 `Hotel-72-EnEV2016`, 57 `Hotel-KfW 55` und 120 `Verwaltung_40-EnEV2016` (bleiben,
> Anwenderentscheid #505); Dachkante 7 337,4 m (9,6-fache Quadratkante) bei 14 `Industrie_ne_81` (✔ erledigt mit #505).
>
> Nachgezogen auf der Fassung von origin mit Schemastand **140** (Nachtrag Stufe Z5 oben, `5de448e8…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`: offen vorher
> 39, berichtigt 39, offen danach 0, Marker 141. Zellvergleich aller 144 Tabellen gegen die Fassung 140
> (10 506 856 Zellen): allein `SchemaVersion` 140 → 141 und die 39 Zellen der Tabelle; Schema unverändert;
> `integrity_check` ok, `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten, 205 Indizes (219 samt den
> von SQLite angelegten). Größe 67 915 776 Byte (LFS-SHA-256 `a427aa72…`). **Ergebnisneutral:** Keinen der zwanzig
> Sätze führt ein Projekt der Testdatenbank (weder über `ID_Gebaeude_Stamm` noch über den Namen); die dreizehn
> Referenzprojekte führen die Sätze 125, 129, 142–146, 233, 56. **Keine Einfrierregel ist berührt.** Referenzlauf
> aller dreizehn Projekte **13/13 PASS gegen diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`)**.

## Die Basis R15 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat am 25.09.2026 die Basis R15 beschrieben — Anlass (Stufe AK1 der
Anlagenkopplung, fünfte Welle: das Referenzprojekt 1047 mit Kopplung), die Tabelle 1017 gegen 1047 und den
Nachtrag #505 zu Schemastand 142 samt der Zusammenführung mit AK1 Welle 5, mit dem die Basis ergebnisneutral
geblieben ist. Er steht hier im Wortlaut, weil der Nachtrag die Testdatenbank herleitet, auf der auch die
Nachfolgebasis steht; die Verweise sind auf diesen Ort umgestellt.

**Abgelöst wurde R15 durch `2026-09-25_R16_Anlagenprio`** (Anwenderentscheid vom 25.09.2026 zu Konzept
Wirtschaftlichkeit § 6.3 Nr. 18: Der Rechenweg ordnet die Anlagen nach der Regel des Hydraulikbilds, eine
Anlage ohne Priorität steht hinten). Allein die Modulreihenfolge der beiden Wärmepumpen von 1042 in
`aggregate.csv` wechselt, alle Werte und alle Zeitreihen bleiben gleich, 1047 und die übrigen zwölf
Projekte sind byte-gleich; die Tabelle steht im Abschnitt „Aktuelle Basis“ von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-25_R15_Anlagenkopplung/`** — **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047), **432 CSV**, **2 447 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (eingefroren auf
Schemastand **141**, LFS-SHA-256 `b48add6a…`; heute Schemastand **142**, LFS-SHA-256 `1360e2be…`, Nachtrag unten). Gegen diese Basis hält `.github/workflows/kern.yml` (1030,
1007, 1017, 1045, 1046, 1047) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und
`EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040. Sie ist die **einzige**
Basis im Arbeitsbaum.

> **Anlass: die fünfte und letzte Welle der Stufe AK1 der Anlagenkopplung** (vom Anwender am 25.09.2026
> samt dem Einfrieren und der Aufnahme des neuen Projekts in die CI beauftragt). Eine Änderung, allein an
> den Daten — **das Referenzprojekt mit Kopplung** (Anlagenkopplung 11.4, Einfrierregel „gesäte
> Auslegungsdaten der Übergabe" oben): Projekt **1047 „Referenz Anlagenkopplung AK1"** ist eine Kopie von
> 1017 auf dem Kopierweg des Programms (`ProjektDuplizierenCtrl`, im Skript Schritt für Schritt
> nachgebildet) mit neun gesetzten Zellen, über
> [`Skripte/anlagenkopplung_1047_referenzprojekt.py`](../../../Referenzlaeufe/Skripte/anlagenkopplung_1047_referenzprojekt.py):
> `Tab_Einstellungen.Anlagenkopplung` NULL → „AK1", die Kaskade `Tool_2` „Heizkessel" → „Wärmepumpe" und
> `Tool_3` „Wärmepumpe" → „Heizkessel" (die Wärmepumpe vor dem Elektrokessel, Anwenderentscheid vom
> 25.09.2026; der Tausch der Platzinhalte wie der Pfeil „nach vorn" der Simulationskonfiguration,
> `Kaskade_Gepflegt` bleibt 0 wie überall in der Testdatenbank), am Projektgebäude 10653 (der Kopie von 10599)
> `Heizkreis_Aktiv` 0 → 1, `Uebergabe_Art` NULL → „RADIATOR", `Heizkurve_Aktiv` 0 → 1,
> `Kuehluebergabe_Aktiv` 0 → 1 und `Kuehl_Uebergabe_Art` NULL → „KUEHLDECKE", dazu die Beschreibung des
> Projekts. Alle übrigen Übergabespalten bleiben NULL — es rechnen die EPOS-Vorgaben der Art: Radiator
> 55/45 °C mit n = 1,3, Heizkurve aus dem Auslegungspunkt (Niveau 0 K, Steilheit 1,0),
> Auslegungs-Außentemperatur aus dem kältesten Tagesmittel, Nennleistung aus der Auslegungsheizlast,
> Proportionalband 1,0 K, Sollwerte des Gebäudes; Kühldecke 16/19 °C mit n = 1,1, Vorlaufgrenze 16 °C,
> Nennleistung aus dem Auslegungstag (19. Juni, 20,41 kW am Katalog-Gebäude). **1017 bleibt unverändert
> und ungekoppelt.** Die Testdatenbank ist die Fassung von origin mit Schemastand 141 (`a427aa72…`, die
> Schritte 140 und 141 stehen mit ihren Nachträgen beim R14-Abschnitt unter `ueberholt/`), auf die das
> Skript angewandt ist; Sicherung vorher außerhalb des Repositoriums. Der Zellvergleich aller Tabellen
> gegen diese Fassung (10 507 032 Zellen) zeigt 9 365 neue Zeilen in 24 Tabellen und 21 geänderte Zeilen
> in `sqlite_sequence`, keine geänderte oder entfernte Zeile sonst, das Schema gleich. Die Kopie gleicht
> Zelle für Zelle einem vom Programm duplizierten Projekt (Prüfstand außerhalb des Repositoriums) bis auf
> die neun Zellen. `integrity_check` ok, `foreign_key_check` leer, 68 747 264 Byte; ein zweiter Lauf des
> Skripts ändert nichts (byte-gleich).
>
> **Warum so** — eine Kopie statt 1017 selbst, damit das Kühlreferenzprojekt bleibt, wie es ist, und das
> Paar 1017/1047 zugleich „ideal gegen gekoppelt" zeigt; 1017 als Vorlage, weil nur dort beide Seiten der
> Kopplung zu rechnen haben (Einzelgebäude nach VDI 6007, Kühlung mit Kälteerzeuger); Radiator mit
> gefahrener Heizkurve und Kühldecke mit allen Vorgaben, damit die Basis die hergeleiteten Wege der Art
> trägt und keine Zahl von Hand (Anlagenkopplung 8.4, 11.4); die Wärmepumpe vor dem Elektrokessel, damit
> sie Wärme liefert und die Kennlinienwahl am gerechneten Vorlauf (Anlagenkopplung 6.1) auf ein Ergebnis
> der Basis wirkt — auf Platz 3 deckten BHKW und Elektrokessel die gekappte Last ganz.
>
> **Die dreizehn alten Projekte bleiben byte-gleich** — Nullnachweis vor dem Einfrieren, auf dem Stand
> mit 1047 in der Testdatenbank: 13/13 PASS gegen R14 (4 207 049 Werte), 394/394 CSV byte-gleich, nur
> `protokoll.txt` anders; nach der Kaskade von 1047 und den Schritten 140 und 141 ebenso byte-gleich zur
> ersten Einfrierung dieser Basis. **1047 kommt mit 38 Dateien und 198 Skalaren dazu**: gegen 1017 die sechs Reihen
> des Heiz- und des Kältekreises (`vorlauf_0.csv`, `ruecklauf_0.csv`, `uebergabe_0.csv`,
> `kuehlvorlauf_0.csv`, `kuehlruecklauf_0.csv`, `kuehluebergabe_0.csv`) und die Skalare
> `Energiebedarf.Vorlauf_Mittel`, `Ruecklauf_Mittel`, `Uebergabe_Begrenzt_Stunden` samt ihren
> Kühl-Gegenstücken und den Vektorsummen der sechs Reihen — 394 → 432 CSV, 2 249 → 2 447 Skalare.
>
> | | 1017 (ideal) | 1047 (gekoppelt) |
> |---|---:|---:|
> | Heizwärme [MWh/a] | 90,19 | 82,75 (−8,3 %) |
> | Heizlastspitze [kW] | 63,16 | 45,64 |
> | mittlere Raumtemperatur der Heizzeit [°C] | 20,72 | 20,08 |
> | Vorlauf / Rücklauf, bedarfsgewichtet [°C] | — | 36,98 / 33,56 |
> | Stunden mit begrenzter Wärmeübergabe [h] | — | 1 108,2 (bis 2,4 K unter dem Sollwert) |
> | Kältebedarf [MWh/a] | 2,52 | 2,33 (−7,5 %) |
> | Kühlvorlauf / Kühlrücklauf, bedarfsgewichtet [°C] | — | 18,00 / 18,81 |
> | Stunden mit begrenzter Kühlübergabe [h] | — | 0 |
> | Stunden mit Kühlbedarf; Überhitzungsstunden [h] | 402; 306 | 423; 327 |
> | Kaskade der Wärmeerzeuger | BHKW, Elektrokessel, WP | BHKW, WP, Elektrokessel |
> | Wärmedeckung BHKW / Wärmepumpe / Elektrokessel [%] | 77,5 / 0,05 / 22,3 | 82,4 / 16,4 / 1,2 |
> | Wärme der Wärmepumpe [MWh/a] | 0,04 | 13,60 |
> | Strom der Wärmepumpe Heizseite / Kühlseite [MWh/a] | 0,02 / 0,55 | 3,54 / 0,51 |
> | Jahresarbeitszahl der Wärmepumpe im Heizbetrieb | 2,49 | 3,84 |
> | Wärme des Elektrokessels [MWh/a] | 20,12 | 1,00 |
> | Restwärme [MWh/a] | 0,10 | 0 |
> | Kältedeckung durch die Wärmepumpe | 98,4 % | 98,8 % |
> | Netzbezug `Stromrestbedarf` [MWh/a] | 655,88 | 641,18 |
>
> **Die Abweichung ist die Kopplung, kein Fehler** (Anlagenkopplung 3.5, 4.4, 7.1). Die Wärmeübergabe
> ist nach den Vorgaben auf die stationäre Auslegungsheizlast bemessen; nach der Absenkung reicht sie in
> 1 108 Stunden nicht, die Aufheizspitze wird gekappt, und der P-Regler hält den Raum mit seinem Band von
> 1 K im Mittel etwas unter dem Sollwert — beides senkt die Heizwärme um 8,3 % (Heizwärme, Spitze und
> begrenzte Stunden wie in der Probe der zweiten Welle mit dem Heizkreis allein). Auf der Kälteseite hebt
> das Band die Raumluft bis zu 1 K über den Kühlsollwert, bevor die Kühldecke voll liefert: Der
> Kältebedarf sinkt um 7,5 %, die Stunden mit Kühlbedarf steigen von 402 auf 423 und die
> Überhitzungsstunden von 306 auf 327 (die Probe der vierten Welle mit der Kühldecke allein: 2,36 MWh/a
> und 330 Überhitzungsstunden; mit dem Heizkreis dazu liegt der Raum in der Heizzeit tiefer). Das ist
> gewollt; die Sollwerte von 1017 bleiben.
>
> **Die Wärmepumpe auf Platz 2 wählt ihre Kennlinie am gerechneten Vorlauf.** Hinter dem BHKW übernimmt
> sie 13,60 MWh/a (16,4 %), der Elektrokessel nur noch 1,00 MWh/a; der Netzbezug sinkt gegen 1017 um
> 14,7 MWh/a. Der Lauf nennt die Stunden je Stützstelle des Heizkreises — 35 °C 3 858 h, 45 °C 1 782 h,
> 55 °C 122 h, dazu 2 309 Stunden unter 35 °C (dort gilt die unterste Kennlinie) —, und die
> Jahresarbeitszahl im Heizbetrieb ist 13,60 / 3,54 = **3,84**. **Gegenprobe ohne Kopplung** (nur an einer
> Arbeitskopie außerhalb des Repositoriums, 1047 mit `Anlagenkopplung` NULL, sonst gleich): Die Wärmepumpe
> rechnet dann durchgehend an der Kennlinie des Anlagenvorlaufs 55 °C und kommt auf eine Jahresarbeitszahl
> von 19,04 / 6,25 = **3,05** — bei höherem Heizbedarf (90,19 MWh/a, ideal) und mehr Betriebsstunden
> (575 statt 396 h). Die Kennlinienwahl am gerechneten Vorlauf wirkt also, und die Basis hält sie. Die
> Kälteseite bleibt am festen Kühl-Vorlauf 18 °C der Maschine (E37, A3): EER-Jahreswert 4,52 wie in 1017,
> Kältestrom 0,51 MWh/a, alles aus dem Netz; die Kaskade ändert daran nichts.
>
> **Rechenzeit (E36):** Das beidseitig gekoppelte Gebäude von 1047 rechnet in 50 bis 62 ms je Jahr
> (Heizwärme eines Gebäudes samt Eingang, das Beste aus fünf Läufen nach dem Anlauf, zwei Messungen),
> das ungekoppelte von 1017 in 22 ms — unter der Grenze von 100 ms.
>
> **Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet. NaN steht nur, wo es gewollt ist:
> `vorlauf_0.csv` und `ruecklauf_0.csv` von 1047 tragen in den 1 063 Stunden ohne Heizbetrieb NaN (die
> Lücke der Reihe, Anlagenkopplung 8.3). Der Vergleich nimmt NaN gegen NaN als gleich; `pruefen` nennt die
> Lücken dieser vier Reihenmuster als Hinweis (`Referenzlauf/Plausibilitaet.cs`, benannte Ausnahme) und
> meldet die Basis **plausibel**.
>
> **Einfrierregeln:** Die Regel „gesäte Auslegungsdaten der Übergabe" entsteht mit diesem Projekt und umfasst
> die Kaskade des gekoppelten Projekts; für den Platz der Wärmepumpe gilt zugleich „gesäte Kältedaten". 1047 ist
> zugleich ein Referenzprojekt mit Gebäude-, Kälte- und Kälteerzeugerdaten und mit eigenen Zeilen in
> `energy_project_settings` — die Regeln „gesäte Gebäudedaten", „gesäte Kältedaten" und die der
> Emissionsfaktoren gelten für seine Zeilen wie für die von 1017. PV-Modulkoeffizienten und Flottenstand
> 1046 sind nicht berührt.
>
> **Zweimal eingefroren, am selben Tag und vor der Veröffentlichung:** zuerst mit der Wärmepumpe auf Platz 3
> (Schemastand 139), dann mit der Kaskade oben auf Schemastand 141. Zwischen beiden bewegt sich allein 1047
> (neun Dateien: `aggregate.csv`, die vier Reihen der Wärmepumpe `wp_produktion`, `wp_strom`,
> `wp_waermebedarf`, `wp_restwaerme`, drei des Kessels und `reststrom_viertelstunde.csv`); die dreizehn
> übrigen Projekte sind byte-gleich.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **14/14 byte-gleich** (432/432 CSV) und
> **GESAMT: PASS** gegen diese Basis (4 610 207 Werte). Der Lauf der sechs CI-Projekte mit der
> Kommandozeile aus `kern.yml`: **6/6 PASS** (2 208 587 Werte), 198/198 CSV byte-gleich.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 \
>   --ziel Referenzlaeufe/2026-09-25_R15_Anlagenkopplung
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis; die Abnahme der Stufe im
> [Protokoll der fünften Welle AK1](../Protokolle/Gebaeudesimulation/2026-09-25_AK1_Welle5_Referenzprojekt.md)
> und im [Status der Gebäudesimulation](../../aktuell/Status_Gebaeudesimulation_VDI6007.md).

> **Nachtrag #505: Schemastand 142 (dritte Berichtigung der Anschlusslängen), die Basis bleibt.**
> Migrationsschritt **142** (`SCHRITT_GEBAEUDE_DRITTE_REPARATUR`; die Nummer steht allein bei
> `GebaeudeAnschlusslaengenDritteReparatur.SCHRITT`, der Quelle für Migration, Werkzeug und Testvorrichtung; er folgt
> auf die Folgeberichtigung, 141) berichtigt nach dem Anwenderentscheid vom 25.09.2026 („Empfehlung übernommen —
> eindeutig unplausible Werte berichtigen, den Rest lassen“) die Berichtstabelle des Nachtrags #496: 19 Zellen an
> achtzehn Sätzen von `Tab_Gebaeude_STAMM` — **reines DML** in der Bauart von #493 und #496 (Bezeichner und
> unplausibler Wert ± 0,05 je Spalte, dieselben Anweisungen), nichts gelöscht, Projektkopien unberührt. Neu ist allein
> das Bild **„leer“** (NULL) für die Laibung dreier Sätze: eine feste Anweisung mehr bei
> `GebaeudeAnschlusslaengenReparatur` (`SQL_FENSTER_WAND_LEER` samt Zählung), weil ein Wertebereich keine leere Zelle
> trifft. Regel der Herleitung wie #496: Zwilling gleicher Geometrie, sonst Verhältnis der Quelle × Fensterfläche;
> Kanten = Umfang U = (Außenwand + Fenster) / (Nutzfläche / Grundfläche × Raumhöhe), nicht unter der Quadratkante
> 4·√Grundfläche. ΔH_T = Σ ψ·ΔL je Satz (ψ des Satzes; ψ 0 oder leer ergibt 0).
>
> | Satz | Spalte | vorher | nachher | Herleitung | ΔH_T |
> |---|---|---|---|---|---|
> | 6 `Pflegeheim-122-EnEV2016` | `Abmessung_Anschluß_Fenster_Wand` | 0 | 540 m | Zwilling 8 `Pflegeheim-C-S-140-EnEV2016` gleicher Geometrie (Wand 2 132, Fenster 545, Grund 540 m²): 540 m = 0,99 m/m² | +81,0 W/K (ψ 0,15) |
> | 15 `Industriehalle-320` | dieselbe | 0 | 16 000 m | Zwilling 14 `Industrie_ne_81` (Wand 15 100, Fenster 6 400, Grund 36 587 m²): 2,5 m/m², dort plausibel | 0 (ψ 0) |
> | 43 `Hotel_H_BZ`, 64 `kl_Hotel-H-086` | dieselbe | 0 | 391,5 m | Zwillinge 65 `kl_Hotel-I-080`, 73 `ml-Hotel-NE-68` (Wand 613,1, Fenster 156,6, Grund 254,9 m²): 2,5 m/m² | 0 (ψ 0) |
> | 117 `Verw_H_75` | dieselbe | leer | 391,5 m | Zwilling 118 `Verw_I_33` gleicher Geometrie | +15,7 W/K (ψ 0,04) |
> | 105 `Büro1-F-U-89`, 107 `Bürogebäude_F_72` | dieselbe | leer | 1 462,1 m | Verwaltung F (115 `Verw_F_147`): 476,8 / 164,36 = 2,901 m/m² × 504 m² | 0 (ψ leer) |
> | 106 `Bürogebäude KfW 55` | dieselbe | 0 | 2 875 m | kein Satz gleicher Geometrie; Verhältnis der NE-/I-Sätze 2,5 m/m² × 1 150 m² | 0 (ψ 0) |
> | 207 `KMH-G-U-120` | dieselbe | 0 | 238,3 m | KMH G (206, 209): 268,6 / 112,015 = 2,398 m/m² × 99,37 m² | 0 (ψ 0) |
> | 23 `Hallenbad-Umkl-140-EnEV2016` | dieselbe | 50 | 865,1 m | Ausgangssatz 24 `Hallenbad-Umkl-180`: 142 / 70,4 = 2,017 m/m² × 428,9 m² (50 m wären 0,12 m/m²) | +32,6 W/K (ψ 0,04) |
> | 34 `gr_Hotel-80-EnEV2016` | dieselbe | 600 | 6 164,4 m | Geometrie der F-Sätze und von 37 `gr_Hotel-G-134` (Wand 10 094, Grund 1 469, Nutzfläche 18 012 m²) nach #493: 7 879 / 3 062,3 = 2,5729 m/m² × 2 395,9 m² (600 m wären 0,25 m/m²); die gerundeten Kanten 300 m (Umfang 313,8 m) bleiben | +500,8 W/K (ψ 0,09) |
> | 108 `Bürogebäude_gross-30-EnEV2016` | dieselbe | 330 | 4 460 m | kein Satz gleicher Geometrie; Verhältnis der NE-/I-Sätze 2,5 m/m² × 1 784 m² (330 m wären 0,18 m/m²) | +371,7 W/K (ψ 0,09) |
> | 42 `Hotel_G_96`, 72 `ml-Hotel-G-096` | `Abmessung_Anschluß_Außenwand_Kellerdecke` | 14,6 | 86,6 m | der Umfang, den #496 als Dachkante gesetzt hat (Geometrie 87,7 m, Quadratkante 83,1 m); 14,6 m wären weniger als ein Fünftel der Quadratkante | je +46,8 W/K (ψ 0,65) |
> | 134 `GMH-G-U-97` | dieselbe | 14,6 | 86,6 m | wie 42 | +47,9 W/K (ψ 0,665) |
> | 84 `GMH-BZ_T`, 85 `GMH-J-015` | dieselbe | 14,6 | 122,3 m | der Umfang der eigenen Geometrie aus #496 (Dachkante), Quadratkante 88,1 m | je +71,6 W/K (ψ 0,665) |
> | 14 `Industrie_ne_81` | `Abmessung_Anschluß_Wand_Dach` | 7 337,4 | 2 362,1 m | Umfang der eigenen Geometrie (15 100 + 6 400) / (39 645 / 36 587 × 8,4 m) = 2 362,1 m; Quadratkante 765,1 m, 7 337,4 m wären ihr 9,6-Faches | −497,5 W/K (ψ 0,10) |
> | 14 `Industrie_ne_81` | `Abmessung_Anschluß_Außenwand_Kellerdecke` | 7 337,4 | 2 362,1 m | derselbe Umfang (der Satz führte beide Kanten gleich); die Laibung 16 000 m (2,5 m/m²) trägt und bleibt | −248,8 W/K (ψ 0,05); Satz −746,3 W/K |
>
> **Nicht geändert (Anwenderentscheid):** die Kanten der Laibungssätze (0 m bei ψ 0; bei 117 leer, bei 105, 107 samt
> ψ leer), die Kellerkanten 0 m der Heime, Schulen und Hallenbäder (ψ 0), die Laibungen 0,32 bis 0,64 m/m² der Sätze
> 46, 57, 120, die Gruppe „nur Dachkante“ (geneigte Dächer) samt den Referenzsätzen 145 und 146. Danach führt kein
> Katalogsatz eine leere Laibung, eine Laibung 0 m bei Fenstern (`AltenH-95-EnEV2016` hat keine Fenster), eine
> Kellerkante 14,6 m oder eine Kellerkante über dem Neunfachen der Quadratkante.
>
> Nachgezogen auf der Fassung von origin mit Schemastand **141** (Nachtrag #496 oben, `a427aa72…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`: offen vorher
> 19, berichtigt 19, offen danach 0, Marker 142; ein zweiter Lauf berichtigt nichts. Zellvergleich aller 144 Tabellen
> gegen die Fassung 141 (10 506 856 Zellen): allein `SchemaVersion` 141 → 142 und die 19 Zellen der Tabelle; Schema
> unverändert; `integrity_check` ok, `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten, 219 Indizes
> samt den von SQLite angelegten. Größe 67 915 776 Byte (LFS-SHA-256 `fc5f143e…`). **Ergebnisneutral:** Keinen der
> achtzehn Sätze führt ein Projekt der Testdatenbank (weder über `ID_Gebaeude_Stamm` noch über den Namen); die
> dreizehn Referenzprojekte führen die Sätze 125, 129, 142–146, 233, 56. **Keine Einfrierregel ist berührt.**
> Referenzlauf aller dreizehn Projekte **13/13 PASS gegen diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich,
> außer `protokoll.txt`)**.
>
> *Gemessen hat #505 gegen R14; der Nachtrag #496, auf den er sich bezieht, steht mit dem Nachtrag Z5 am
> Ende des R14-Abschnitts unter `Dokumentation/ueberholt/Referenzbasen/`.* **Zusammenführung mit AK1
> Welle 5 — R15 bleibt:** Die Testdatenbank ist
> die Fassung von origin mit Schemastand 142 (`fc5f143e…`), auf die
> [`Skripte/anlagenkopplung_1047_referenzprojekt.py`](../../../Referenzlaeufe/Skripte/anlagenkopplung_1047_referenzprojekt.py)
> angewandt ist; ein zweiter Lauf ändert nichts. Zellvergleich gegen die Fassung von origin (10 507 032
> Zellen): genau die 9 365 Zeilen des Projekts 1047 in 24 Tabellen und 21 Zeilen `sqlite_sequence`, Schema
> gleich. `integrity_check` ok, `foreign_key_check` leer, 68 747 264 Byte (LFS-SHA-256 `1360e2be…`).
> Referenzlauf aller vierzehn Projekte gegen diese Basis: **14/14 PASS** (4 610 207 Werte, 432/432 CSV
> byte-gleich, außer `protokoll.txt`); die berichtigten Sätze führt auch 1047 nicht (es führt Satz 125 wie 1017).

> **Die Vorgängerbasis `2026-09-24_R14_Kaelteerzeuger`**, die erste Basis mit Kälteerzeuger, ist mit dieser
> Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zum Kälteerzeuger von 1017
> und den Nachträgen zu den Schemaständen 119 bis 141 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R16 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat am 25.09.2026 die Basis R16 beschrieben — Anlass (Konzept
Wirtschaftlichkeit § 6.3 Nr. 18, Rechenweg-Sortierung nach der Regel „99“), die Modultafel von 1042, die
A/B-Tafel und die Nachträge #504 und Schemastand 143, mit denen die Basis ergebnisneutral geblieben ist.
Er steht hier im Wortlaut; die Verweise sind auf diesen Ort umgestellt.

**Abgelöst wurde R16 durch `2026-09-25_R17_Datenpflege`** (Anwenderentscheid vom 25.09.2026 zu Konzept
Wirtschaftlichkeit § 6.3 Nr. 24, Etappe E24: die Kessel von 1018 und 1023 tragen den Energieträger 63
„Erdgas E“, 1023 dazu eine Projektzeile und einen Preisstand für Erdgas). Allein
`HeizkesselModul[0].carrier_id` in `aggregate.csv` von 1018 und 1023 wechselt von leer auf 63, alle übrigen
Werte und alle Zeitreihen bleiben gleich; die Tafel steht im Abschnitt „Aktuelle Basis“ von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-25_R16_Anlagenprio/`** — **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047), **432 CSV**, **2 447 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand
**143**, LFS-SHA-256 `76dd9e48…` — R16 wurde auf der Fassung `1360e2be…` mit Schemastand 142 eingefroren
(dieselbe Datei, auf der R15 zuletzt gehalten wurde; Herleitung samt Projekt 1047 im Abschnitt der Basis R15
unter [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md)); danach
änderten #504 nur 93 Zellen des Tww-Testkatalogs und Schemaschritt 143 nur zwei Quelltexte des
Baustoffkatalogs, beide ohne Referenzwirkung (Nachträge unten). Gegen
diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017, 1045, 1046, 1047) jeden Push, `ios.yml`
den iZ6-Vergleich für 1030, und `EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt
1040. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: der Anwenderentscheid vom 25.09.2026 zu Konzept Wirtschaftlichkeit § 6.3 Nr. 18** — der
> Rechenweg ordnet die Anlagen nach derselben Regel wie Hydraulikbild und Erzeugerkarten
> (`Ladeordnung.SqlAnlagenprio`, Regel „99“: gepflegte Priorität zuerst, eine Anlage ohne Priorität —
> NULL oder 0 — hinten, bei Gleichstand die ID). Eine Änderung am Rechenweg, keine an den Daten, kein
> Schemaschritt:
>
> 1. **Die fünf Rechenweg-Leser**, die bis dahin `ORDER BY Prioritaet, ID` lasen und damit die
>    ungepflegte Anlage VOR die gepflegte stellten: `SimulationControl.WP_Liste_Laden`,
>    `…QuellbezuegeAufbauen`, `…SenkenPufferDerAnlagen`, `WaermesenkeClass.SenkenLaden`,
>    `…SenkenlistenLaden`.
> 2. **Die drei Modul-Lader**, die ganz ohne `ORDER BY` in der Zeilenfolge der Datenbank luden:
>    `SimulationControl.SPK_Liste_Laden`, `…Solar_Liste_Laden`, `…BHKW_Liste_Laden`.
>
> **Allein 1042 bewegt sich, und nur im Index der Module** — alle Werte und alle Zeitreihen bleiben
> Zeichen für Zeichen gleich, 1047 und die übrigen zwölf Projekte sind in allen Dateien byte-gleich zu R15.
> Die beiden Wärmepumpen von 1042 tauschen die Plätze in `aggregate.csv` (10 Werte):
>
> | 1042, `aggregate.csv` | R15 `WaermepumpeModul[0]` | R15 `[1]` | R16 `WaermepumpeModul[0]` | R16 `[1]` |
> |---|---|---|---|---|
> | `.Modul` (Anlage, Priorität) | CS7800iLW 16 (14818, keine) | CS6800iAW MB + AW 10 OR-T (14817, 1) | CS6800iAW MB + AW 10 OR-T (14817, 1) | CS7800iLW 16 (14818, keine) |
> | `.Leistung` [kW] | 15 | 11 | 11 | 15 |
> | `.Waermeproduktion` [MWh/a] | 20,84 | 71,45 | 71,45 | 20,84 |
> | `.Stromverbrauch` [MWh/a] | 7,06 | 26,29 | 26,29 | 7,06 |
> | `.Betriebsstunden` [h] | 2 073,4 | 5 995,29 | 5 995,29 | 2 073,4 |
>
> **A/B der beiden Teile** (gemessen vor dem Zusammenführen mit AK1 Welle 5 an den dreizehn Projekten
> gegen R14, danach an allen vierzehn gegen R15):
>
> | Stand | Vergleich | Ergebnis |
> |---|---|---|
> | Teil 1 allein | gegen R14 (13 Projekte) | 12/13 PASS und byte-gleich; 1042 FAIL mit den **10 Werten** oben, die übrigen 35 Dateien byte-gleich |
> | Teil 1 + 2 | gegen Teil 1 allein (13 Projekte) | **394/394 CSV byte-gleich** — Teil 2 ändert in keinem Projekt etwas, nicht einmal einen Index |
> | Teil 1 + 2 | gegen R15 (14 Projekte, Schemastand 142) | 13/14 PASS, 431/432 CSV byte-gleich; allein 1042 `aggregate.csv` mit denselben **10 Werten**; 1047 PASS und byte-gleich (38 Dateien) |
>
> **Warum ohne Rechenwirkung:** Die Deckungsreihenfolge legt die Kaskade über den Typ fest (`Tool_1..4`),
> Anlagen finden ihre Senken über die Anlagen-ID und Puffer über `Z_AnlageSenke.Ladeprio`; die Rechenfolge
> der Wärmepumpen von 1042 steht in `ModulEbenen` (getrennte Senken: 14817 Heizkreis und Puffer 1054196,
> 14818 nur Brauchwasserpuffer 1054202). Die Reihenfolge der Senken- und Pufferlisten ändert sich außerdem
> in 1030 (BHKW ohne Priorität hinter BHKW und Kessel mit Priorität) und in 1040, 1041, 1045 (Kessel ohne
> Priorität hinter der Wärmepumpe mit Priorität 1) — ohne Wirkung auf eine Zahl. Teil 2 greift in keinem
> Projekt: Keines führt zwei Kessel oder zwei Kollektorfelder, und die beiden BHKW von 1030 stehen nach der
> Regel wie nach der Zeilenfolge (14920 mit Priorität 1 vor 14921 ohne). Rechnerisch wirkt die Regel erst
> bei zwei Anlagen gleichen Typs auf derselben Rechenebene und Senke, deren Priorität von der ID abweicht.
>
> **Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet; NaN nur in den gewollten Lücken der
> Vorlauf- und Rücklaufreihen von 1047 (wie in R15).
>
> **Einfrierregeln:** nicht berührt — keine gesäten Daten geändert, die Testdatenbank ist byte-gleich.
>
> **Determinismus geprüft:** zwei Läufe desselben Standes nacheinander **14/14 byte-gleich** (432/432 CSV)
> und untereinander **GESAMT: PASS** (4 610 207 Werte); der Einfrierlauf ist mit beiden byte-gleich.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 \
>   --ziel Referenzlaeufe/2026-09-25_R16_Anlagenprio
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis. Nachweis im Test:
> `EPOS.Kern.Tests/AnlagenprioRechenwegTests` (die acht Leser nutzen die Regel; in 1042 steht die
> Wärmepumpe ohne Priorität hinter der mit Priorität 1).

> **Nachtrag #504: die abgeleiteten VDI-6002-Typen im Tww-Testkatalog, die Basis bleibt.**
> **Kein Schemaschritt** — der Folgeposten der Anwenderentscheide ZU20 und ZU24 vom 25.09.2026 setzt
> allein Werte: Quelle, Version und Herkunftsart `VERFAHREN` der fünf Nutzungsarten „… (abgeleitet)“
> und ihrer Tagesgänge (Abschnitt „Abgeleitete VDI-Werte im Tww-Testkatalog“ oben). Nachgezogen auf der
> Fassung von origin mit Schemastand **142** (AK1 Welle 5 oben, `1360e2be…`) mit
> `py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite --stochastik`:
> 0 Zeilen angelegt, 21 nachgeführt; ein zweiter Lauf schreibt nichts (0 neu, 0 nachgeführt).
> Zellvergleich aller 144 Tabellen gegen die Fassung von origin (10 645 525 Zellen): abweichend allein
> **93 Zellen** — 45 in `Tab_TwwNutzungsart_STAMM` und 48 in `Tab_TwwTagesgang_STAMM` —, Zeilenzahlen
> unverändert, Schema gleich, `sqlite_sequence` gleich (88 Zeilen). `integrity_check` ok,
> `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten, 219 Indizes samt den von SQLite
> angelegten, keine `AUSLIEFERUNG`-Zeile. Größe 68 747 264 Byte (LFS-SHA-256 `22eeb75c…`).
> **Ergebnisneutral:** Keines der vierzehn Referenzprojekte führt eine Tww-Zone, und kein Rechenweg
> der vierzehn liest den Tww-Katalog. **Keine Einfrierregel ist berührt.** Referenzlauf der sechs
> Projekte der CI (1030, 1007, 1017, 1045, 1046, 1047) gegen diese Basis: **6/6 PASS** (198 Dateien,
> 2 208 587 Werte).

> **Nachtrag Schemastand 143 (Herkunft der Rohdichte in der Baustoffsaat, E39), die Basis bleibt.**
> Migrationsschritt **143** (`SCHRITT_BAUSTOFF_QUELLEN`; die Nummer steht allein bei
> `BaustoffQuellenBerichtigung.SCHRITT`, der Quelle für Migration, Werkzeug und Testvorrichtung; er folgt
> auf die dritte Berichtigung der Anschlusslängen, 142) setzt die Regel aus E39 in bestehenden Datenbanken
> durch (Konzept Gebäudesimulation N1.44, „Benannte Lücken“): Stammt die Rohdichte einer Herstellerzeile
> aus einer Umweltproduktdeklaration, dann nennt die Quelle das. **Reines DML** an der Spalte `Quelle`
> zweier Saatzeilen — in `Tab_Baustoff_STAMM` über die Saat-Id, in der Projektkopie `Tab_Baustoff` über
> Hersteller und Bezeichner (den Schlüssel von `BaustoffCtrl.CopyFromStamm`), jeweils nur, wo der alte
> Saattext wortgleich steht; eine vom Anwender geänderte Quelle bleibt. Die Saat trägt die neuen Texte,
> eine neue Datenbank bekommt sie gleich.
>
> | Id | Quelle vorher | angefügt |
> |---|---|---|
> | 1041 | `Kingspan, Produktblatt Kooltherm K5 WDVS-Dämmplatte (DE), Version 15, 07/2026` | `; Rohdichte aus FDES 120 mm` |
> | 1066 | `Baumit, Produktdatenblatt DämmPutz DP 85, 18.09.2025` | `; Rohdichte Mindestwert A2-s1,d0 nach VDPM-EPD` |
>
> Nachgezogen auf der Fassung von origin mit Schemastand **142** (Nachtrag #504 oben, `22eeb75c…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`:
> offen vorher 2, berichtigt 2 Katalogzeilen und 0 Projektkopien (die Testdatenbank führt keine
> Projektkopie eines Baustoffs), offen danach 0, Marker 143; ein Trockenlauf danach findet nichts offen.
> Zellvergleich aller Tabellen samt `sqlite_sequence` gegen die Fassung 142: allein `SchemaVersion`
> 142 → 143 und die zwei Quellzellen; Schema gleich, Zeilenzahlen unverändert. `integrity_check` ok,
> `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten, 219 Indizes samt den von SQLite
> angelegten. Größe 68 714 496 Byte (das Werkzeug verdichtet mit `VACUUM`; LFS-SHA-256 `76dd9e48…`).
> **Ergebnisneutral:** Kein Rechenweg liest den Baustoffkatalog, und kein Referenzprojekt hat eine Zone.
> **Keine Einfrierregel ist berührt** — der Baustoffkatalog gehört nicht zu den gesäten Gebäudedaten.
> Referenzlauf aller vierzehn Projekte gegen diese Basis: **14/14 PASS** (4 610 207 Werte), 432/432 CSV
> byte-gleich.

> **Die Vorgängerbasis `2026-09-25_R15_Anlagenkopplung`**, die erste Basis mit Anlagenkopplung, ist mit
> dieser Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zum Referenzprojekt
> 1047 und dem Nachtrag zu Schemastand 142 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R17 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat am 25.09.2026 die Basis R17 beschrieben — Anlass (Konzept
Wirtschaftlichkeit § 6.3 Nr. 24, Datenpflege der Kesselträger von 1018 und 1023), die Pflegetafel, der
Zellvergleich, die A/B-Tafel gegen R16 und die benannten Kessel ohne Träger. Er steht hier im Wortlaut;
die Verweise sind auf diesen Ort umgestellt.

**Abgelöst wurde R17 durch `2026-09-25_R18_PvAusweis`** (Anwender 25.09.2026, Etappe E26, Befund N1 aus
E25: `Ergebnis.Photovoltaik.Stromproduktion` ist die Erzeugung der Module statt des Direktverbrauchs).
Allein `Photovoltaik.Stromproduktion` in `aggregate.csv` von 1007, 1040, 1045 und 1046 wechselt, alle
übrigen Werte und alle Zeitreihen bleiben gleich; die Tafel steht im Abschnitt „Aktuelle Basis“ von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-25_R17_Datenpflege/`** — **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047), **432 CSV**, **2 447 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand **143**,
LFS-SHA-256 `0c2fe21a…`). Gegen diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017, 1045,
1046, 1047) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und `EPOS.Kern.Tests/GebaeudeRueckwegTests`
den Tagesbilanz-Weg an Projekt 1040. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: die Datenpflege nach Konzept Wirtschaftlichkeit § 6.3 Nr. 24** (Anwender 25.09.2026,
> Etappe E24, Entscheide E24‑Q1 … Q6 nach Empfehlung). Zwei Kessel der Referenzprojekte trugen keinen
> Energieträger und rechneten ihre Emissionen über den Rückfall auf den Gerätebrennstoff; 1023 hatte zudem
> keine Projektzeile für Erdgas und damit in einer frischen Wirtschaftlichkeitsrechnung keine
> Energiekosten. Kein Schemaschritt, kein Code am Rechenweg — allein Werte der Testdatenbank, gesetzt mit
> einem einmaligen dotnet-Dateiskript auf der Fassung mit Schemastand 143 (`76dd9e48…` → `0c2fe21a…`):
>
> | Tabelle, Zeile | Projekt | vorher | nachher | Quelle |
> |---|---|---|---|---|
> | `Tab_Energieanlagen` 10369 (Kessel 1018251, Brennstoff 3) | 1018 | `ID_Carrier` NULL | **63** „Erdgas E“ | wie das BHKW 11327 desselben Projekts und die Kessel von 1026–1030, 1039–1045 |
> | `Tab_Energieanlagen` 11205 (Kessel 1018254, Brennstoff 3) | 1023 | `ID_Carrier` NULL | **63** | dieselbe |
> | `energy_project_settings` **10130** (neu) | 1023 | — | Träger 63, `ID_Umrechnung` 40, Hi 10,5, Hs 11,6, 0,84 €/Nm³, Grundpreis 1 200 €/a, CO₂ **240**, SO₂ 0,3, NOx 110, Nm³ | Kopie der Zeile 10067 von 1030 (E24‑Q1 a: CO₂ wie 1030/1018/1026) |
> | `energy_price` **10185** (neu) | 1023 | — | Träger 63, 0,84 / 1 200 / Nm³ / Heizwert 10,5, `valid_from` wie 1030 | Kopie der Zeile 10130 von 1030 (E24‑Q2) |
>
> Zellvergleich aller 145 Tabellen samt `sqlite_sequence` gegen die Fassung 143 (10 645 701 Zellen): allein
> die zwei `ID_Carrier`-Zellen, die zwei neuen Zeilen und die zwei Zähler in `sqlite_sequence`
> (`energy_project_settings` 10129 → 10130, `energy_price` 10184 → 10185); Schema gleich, Zeilenzahlen sonst
> unverändert. `integrity_check` ok, `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten,
> 219 Indizes samt den von SQLite angelegten. Größe 68 714 496 Byte (LFS-SHA-256 `0c2fe21a…`). Ein zweiter
> Lauf des Skripts findet nichts offen.
>
> **Einfrierregel „Emissionsfaktoren“ berührt** — die neue Projektzeile führt `energy_project_settings.co2`
> an einem Referenzprojekt; darum diese Neueinfrierung. **Die Emissionen bewegen sich trotzdem nicht:** Der
> Projektwert 240 g/kWh steht in der Lesekette vor der aktiven Katalogzeile (BAFA 201 g/kWh) und ist
> derselbe Wert, den vorher der Rückfall auf `Tab_Brennstoff_Stamm` 3 lieferte. Ohne Projektwert hätte 1023
> mit 201 g/kWh gerechnet (`Em.Kessel.Co2T` 22,44 → 18,79 t/a, in Phase 0 an einer Kopie gemessen, nicht
> gewählt).
>
> **A/B gegen R16** (14 Projekte): **12/14 PASS und byte-gleich**, 430/432 CSV byte-gleich; 1018 und 1023
> FAIL mit je **einem** Wert in `aggregate.csv`, alle Zeitreihen byte-gleich:
>
> | Projekt, `aggregate.csv` | R16 | R17 |
> |---|---|---|
> | 1018 `HeizkesselModul[0].carrier_id` | leer | 63 |
> | 1023 `HeizkesselModul[0].carrier_id` | leer | 63 |
>
> Die Simulation liest weder Arbeits- noch Grundpreis; beides wirkt erst in der Wirtschaftlichkeit: 1023
> hat seither in einer frischen Rechnung Energiekosten für Erdgas (0,84 €/Nm³ ÷ 10,5 kWh/Nm³ = 0,08 €/kWh)
> und damit einen Kapitalwert. Die **gebuchten** Ergebnisse der Gruppe „Wöhler“ (1019, 1023, 1024) bleiben
> unverändert und ohne Nachweisumschlag. 1018 bleibt bewusst ohne Gaspreis (E24‑Q4, Prüffall
> `ProjektkostenArtenTests`). Nachweis im Test: `EPOS.Kern.Tests/DatenpflegeKesseltraegerTests`.
>
> **Benannt, nicht gepflegt (E24‑Q3):** Ohne Energieträger bleiben die Kessel der Referenzprojekte 1007,
> 1008, 1017, 1046 und 1047 (in 1017 und 1047 auch das BHKW); der Kessel von 1024 trägt `ID_Carrier` = 0.
>
> **Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet; NaN nur in den gewollten Lücken der
> Vorlauf- und Rücklaufreihen von 1047 (wie in R16).
>
> **Determinismus geprüft:** zwei Läufe desselben Standes nacheinander **14/14 byte-gleich** (432/432 CSV)
> und untereinander **GESAMT: PASS** (4 610 207 Werte); der Einfrierlauf ist mit beiden byte-gleich.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 \
>   --ziel Referenzlaeufe/2026-09-25_R17_Datenpflege
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis.

> **Die Vorgängerbasis `2026-09-25_R16_Anlagenprio`**, die erste Basis mit der Anlagenreihenfolge nach der
> Regel „99“ im Rechenweg, ist mit dieser Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt der
> Modultafel von 1042 und den Nachträgen #504 und Schemastand 143 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R18 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis“ hat am 25.09.2026 die Basis R18 beschrieben — Anlass (E26, Befund N1: die
Stromproduktion der Photovoltaik ist die Erzeugung der Module), die A/B-Tafel gegen R17, der Nachtrag
Schemastand 144 und die Zusammenführung mit der Datenpflege E24. Er steht hier im Wortlaut; die Verweise
sind auf diesen Ort umgestellt.

**Abgelöst wurde R18 durch `2026-09-25_R19_BhkwNetzbezug`** (Anwender 25.09.2026, Etappe E27, Befund N5
aus E26: der Netzbezug ist nie negativ; ein BHKW-Überschuss ohne nachfolgende Photovoltaik oder
Stromspeicher steht allein im KWK-Split als Einspeisung). Allein `aggregate.csv` und
`reststrom_viertelstunde.csv` von 1018 und 1030 wechseln, alle übrigen Werte und Zeitreihen bleiben
gleich; die Tafel steht im Abschnitt „Aktuelle Basis“ von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-25_R18_PvAusweis/`** — **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047), **432 CSV**, **2 447 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand **144**,
LFS-SHA-256 `19a7b632…` — R18 wurde auf der Fassung `0c2fe21a…` mit Schemastand 143 eingefroren, dieselbe
Datei wie R17; danach änderte Schemaschritt 144 nur das Schema (vier leere Spalten der Nachtzeit), ohne
Referenzwirkung, zusammengeführt mit der Datenpflege E24 (Nachtrag unten)). Gegen diese Basis hält
`.github/workflows/kern.yml` (1030, 1007, 1017, 1045, 1046, 1047) jeden Push, `ios.yml` den iZ6-Vergleich
für 1030, und `EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040. Sie ist die
**einzige** Basis im Arbeitsbaum.

> **Anlass: der PV-Ausweis** (Anwender 25.09.2026, Etappe E26, Befund N1 aus E25, Entscheide E26‑Q1 … Q7
> nach Empfehlung). `Ergebnis.Photovoltaik.Stromproduktion` führte die Summe der Direktverbrauchsreihe
> (`SimulationPV.Stromproduktion`, der genutzte Anteil), obwohl Modell und Leser die Erzeugung der Module
> erwarten; der Ausweis „PV: vermiedener Bezug" (Erzeugung − Einspeisung) zog den Überschuss damit ein
> zweites Mal ab. Das Feld ist jetzt die Erzeugung nach Wechselrichter und Clipping
> (`Stromproduktion_Theoretisch`, gleich der Summe der Modulzeilen). Die Testdatenbank ist unverändert, kein
> Schemaschritt; die Reihe `pv_produktion.csv` bleibt der Direktverbrauch.
>
> **A/B gegen R17** (14 Projekte): **10/14 PASS und byte-gleich**, 428/432 CSV byte-gleich; 1007, 1040, 1045
> und 1046 FAIL mit je **einem** Wert in `aggregate.csv`, alle Zeitreihen byte-gleich:
>
> | Projekt, `aggregate.csv` | R17 | R18 | Erzeugung = genutzt + Überschuss |
> |---|---|---|---|
> | 1007 `Photovoltaik.Stromproduktion` | 5,08 | 6,01 | `pv_produktion_theoretisch` 6 014,3 kWh |
> | 1040 `Photovoltaik.Stromproduktion` | 4,44 | 6,71 | 6 713,5 kWh = 4 440,7 + 2 272,7 |
> | 1045 `Photovoltaik.Stromproduktion` | 2,74 | 3,55 | 3 545,5 kWh |
> | 1046 `Photovoltaik.Stromproduktion` | 5,08 | 6,01 | 6 014,3 kWh |
>
> 1041 und 1042 führen eine Photovoltaik ohne Ertrag (0 → 0). **Der Strommatrix-Bedarf (Befund N3,
> dieselbe Etappe) wirkt nicht auf die Basis:** `aggregate.csv` führt keine Wirtschaftlichkeitsgröße, und die
> neue Reihe `STROMBEDARF_GESAMT` entsteht nur im Zeitreihensatz des Berichts. Die Kapitalwerte bleiben
> bitgleich; Nachweis im Test: `EPOS.Kern.Tests/PvAusweisStromMatrixTests`.
>
> **Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet; NaN nur in den gewollten Lücken der
> Vorlauf- und Rücklaufreihen von 1047 (wie in R17).
>
> **Determinismus geprüft:** zwei Läufe desselben Standes nacheinander **14/14 byte-gleich** (432/432 CSV)
> und untereinander **GESAMT: PASS** (4 610 207 Werte); der Einfrierlauf ist mit beiden byte-gleich.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf >   --quelle Referenzlaeufe/Kenndaten_Test.sqlite >   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 >   --ziel Referenzlaeufe/2026-09-25_R18_PvAusweis
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis.

> **Nachtrag Schemastand 144 (Nachtzeit je Gebäude, E43), die Basis bleibt.** Migrationsschritt
> **144** (`SCHRITT_NACHTZEIT`; die Nummer steht allein bei `NachtzeitSchema.SCHRITT`, der Quelle für
> Migration, Werkzeug und Testvorrichtung; er folgt auf die Herkunft der Rohdichte, 143) legt an
> `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` je zwei nullbare Spalten `Nachtabsenkung_Beginn` und
> `Nachtabsenkung_Ende` an (`INTEGER`, `CHECK … IS NULL OR … BETWEEN 0 AND 23`, Stunde des Tages; die
> Nacht ist [Beginn, Ende), zyklisch über Mitternacht) und baut die Sicht `Abfrage_Projektgebaeude` zum
> sechsten Mal neu — mit allen Spalten der fünf früheren Durchgänge samt Kühlübergabe und Baujahr, 101
> Spalten, als letzter Sichtneubau. **Reines DDL, keine Saat:** Beide Spalten stehen überall auf NULL,
> und NULL heißt die Vorgabe 22 bis 6 Uhr, abgeleitet aus den Stunden des Tagsollwerts und bitgleich mit
> dem Fahrplan davor. Der Tagesbilanz-Weg (Projekt 1040) liest die Spalten nicht.
>
> Die Gebäudesimulations-Sitzung (G4) zog den Schritt auf der Fassung von origin mit Schemastand **143**
> (`76dd9e48…`, vor der Datenpflege E24) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`
> nach: 4 von 4 Spalten angelegt, Sicht mit 101 Spalten, Marker 144; ein zweiter Lauf legt nichts an.
> Zellvergleich aller Tabellen gegen die Fassung 143: allein `SchemaVersion` 143 → 144, die vier neuen
> Spalten überall NULL und die Schematexte von `Tab_Gebaeude`, `Tab_Gebaeude_STAMM` und
> `Abfrage_Projektgebaeude`; Zeilenzahlen unverändert. `integrity_check` ok, `foreign_key_check` leer,
> 144 Tabellen (alle STRICT), 14 Sichten, 219 Indizes samt den von SQLite angelegten. Größe 68 714 496
> Byte (LFS-SHA-256 `9a71b714…`). **Keine Einfrierregel ist berührt** — die Spalten sind leer, keine
> gesäte Gebäudeangabe ändert sich. Referenzlauf aller vierzehn Projekte gegen die Basis: **14/14
> PASS** (4 610 207 Werte), 432/432 CSV byte-gleich.
>
> **Zusammenführung mit der Datenpflege E24 (#514):** Beide Fassungen gingen von `76dd9e48…` aus — die
> Datenpflege (Träger 63 an den Kesseln 10369 und 11205, Erdgaszeilen 10130 und 10185 für 1023) und der
> Schemaschritt 144. Zusammengeführt wurde, indem das Pflegeskript `e24_pflege` (wiederholbar; Vorzustand
> geprüft, `integrity_check` ok, `foreign_key_check` leer) auf der Fassung 144 (`9a71b714…`) lief:
> Ergebnis Schemastand 144 mit den gepflegten Zellen, 68 714 496 Byte, LFS-SHA-256 `19a7b632…`. Gegen
> R18 bleibt der Referenzlauf 14/14 PASS byte-gleich (Nachweis im Gate der Statuszeile #518).

> **Die Vorgängerbasis `2026-09-25_R17_Datenpflege`**, die Basis der Datenpflege nach Konzept
> Wirtschaftlichkeit § 6.3 Nr. 24 (Kesselträger von 1018 und 1023), ist mit dieser Einfrierung aus dem
> Arbeitsbaum gefallen; ihr Protokoll samt Pflegetafel und A/B-Tafel gegen R16 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R19 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis" hat am 25.09.2026 die Basis R19 beschrieben — Anlass (E27, Befund N5
aus E26: der Netzbezug ist nie negativ), die A/B-Tafel gegen R18, die Übernahme des Schemastands 144
mit der Datenpflege E24 und die Nachträge Prüfprojekt 1048, Schemaschritt 145 (Konstruktorzeilen),
146 (Namensabgleich der Baustoffe) und 148 (Baualtersklassen samt Energiestandard). Er steht hier im
Wortlaut; die Verweise sind auf diesen Ort umgestellt.

**Abgelöst wurde R19 durch `2026-09-26_R20_Zapfprofil`** (Umsetzungskonzept Zapfprofilgenerator 3.4,
Anwenderentscheid ZU7: Projekt 1045 rechnet sein Brauchwasser über den Zapfprofilgenerator statt aus
der eingefrorenen Jahresreihe). Allein 16 von 32 Dateien in `Projekt_1045` wechseln, die übrigen
dreizehn Projekte bleiben byte-gleich; die Tafel steht im Abschnitt „Aktuelle Basis" von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-25_R19_BhkwNetzbezug/`** — **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047), **432 CSV**, **2 447 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand **148**,
LFS-SHA-256 `b02fa02e…` — R19 wurde auf der Fassung `19a7b632…` mit Schemastand 144 eingefroren, den Zellen
der Datenpflege E24, noch ohne das Prüfprojekt „PV mit Preisen“, ohne den Schemaschritt 145 des
Zapfprofilgenerators, ohne die Tabellen des Namensabgleichs (Schritt 146), ohne die Zonenkopplung
(Schritt 147) und ohne die Baualtersklassen nach Bauzeitraum samt Energiestandard (Schritt 148); alles kam ohne Referenzrolle hinzu und bewegt keine Basis (Nachträge unten)). Gegen diese Basis hält `.github/workflows/kern.yml` (1030, 1007,
1017, 1045, 1046, 1047) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und
`EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040. Sie ist die **einzige** Basis
im Arbeitsbaum.

> **Anlass: der Netzbezug ist nie negativ** (Anwender 25.09.2026, Etappe E27, Befund N5 aus E26,
> Entscheide E27‑Q1 … Q8 nach Empfehlung). Die Kaskade zieht die BHKW-Erzeugung ungeklemmt vom
> Reststrom ab, damit spätere Verbraucher derselben Viertelstunde und die Photovoltaik den Überschuss
> sehen. Folgte keine klemmende Stufe (Photovoltaik, Stromspeicher), blieb der Überschuss negativ im
> Vektor und minderte Netzbezug, Stromkosten und CO₂ — obwohl der KWK-Split ihn schon als Einspeisung
> führt. Der Lauf setzt jetzt am Ende jeden Wert unter 0 auf 0 (`SimulationControl.NetzbezugGeklemmt`,
> nicht bei der Speicherflotte); der Reststrombedarf der BHKW-Zeile ist je Stunde geklemmt. Jeder
> nichtnegative Wert bleibt bitgleich. Die Testdatenbank ist unverändert, kein Schemaschritt.
>
> **A/B gegen R18** (14 Projekte): **12/14 PASS und byte-gleich**, 428/432 CSV byte-gleich; 1018 und 1030
> FAIL mit `aggregate.csv` und `reststrom_viertelstunde.csv`, alle übrigen Zeitreihen byte-gleich:
>
> | Projekt, Datei, Größe | R18 | R19 |
> |---|---|---|
> | 1018 `aggregate.csv` `Sim.Reststrom` | −27,4575103 | 0 |
> | 1018 `aggregate.csv` `Energiebedarf.Stromrestbedarf` | −27,46 | 0 |
> | 1018 `aggregate.csv` `BHKW.Reststrombedarf` | −27,46 | 0 |
> | 1018 `aggregate.csv` `Vektor.reststrom_viertelstunde.Summe` | −109 830,041 | 0 |
> | 1018 `reststrom_viertelstunde.csv` | 14 004 Werte < 0 | 0 |
> | 1030 `aggregate.csv` `Sim.Reststrom` | 4 357,78079 | 4 358,17279 |
> | 1030 `aggregate.csv` `Energiebedarf.Stromrestbedarf` | 4 357,78 | 4 358,17 |
> | 1030 `aggregate.csv` `BHKW.Reststrombedarf` | 4 357,78 | 4 358,17 |
> | 1030 `aggregate.csv` `Vektor.reststrom_viertelstunde.Summe` | 17 431 123,2 | 17 432 691,2 |
> | 1030 `reststrom_viertelstunde.csv` | 48 Werte < 0 (12 Stunden) | 0 |
>
> Die Toleranz meldet 1018 mit 14 008 und 1030 mit 48 Abweichungen. Die BHKW-Erzeugung
> (`bhkw_strom.csv`) und der KWK-Split bleiben gleich (1018 Einspeisung 27,4575 MWh, 1030 0,392 MWh);
> Kapitalwerte und CO₂ hält `EPOS.Kern.Tests/BhkwNetzbezugKlemmeTests` samt den neu gesetzten Ankern
> von 1030 in `PvAusweisStromMatrixTests`.
>
> **Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet; NaN nur in den gewollten Lücken der
> Vorlauf- und Rücklaufreihen von 1047 (wie in R18).
>
> **Determinismus geprüft:** zwei Läufe desselben Standes nacheinander **14/14 byte-gleich** (432/432 CSV)
> und untereinander **GESAMT: PASS** (4 610 207 Werte); der Einfrierlauf ist mit beiden byte-gleich.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 \
>   --ziel Referenzlaeufe/2026-09-25_R19_BhkwNetzbezug
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis.

> **Übernommen aus R18: Schemastand 144 (Nachtzeit je Gebäude, E43) und Datenpflege E24.** R19 ist
> auf dieser Fassung eingefroren; der Nachtrag beschreibt, wie sie entstand. Migrationsschritt
> **144** (`SCHRITT_NACHTZEIT`; die Nummer steht allein bei `NachtzeitSchema.SCHRITT`, der Quelle für
> Migration, Werkzeug und Testvorrichtung; er folgt auf die Herkunft der Rohdichte, 143) legt an
> `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` je zwei nullbare Spalten `Nachtabsenkung_Beginn` und
> `Nachtabsenkung_Ende` an (`INTEGER`, `CHECK … IS NULL OR … BETWEEN 0 AND 23`, Stunde des Tages; die
> Nacht ist [Beginn, Ende), zyklisch über Mitternacht) und baut die Sicht `Abfrage_Projektgebaeude` zum
> sechsten Mal neu — mit allen Spalten der fünf früheren Durchgänge samt Kühlübergabe und Baujahr, 101
> Spalten, als letzter Sichtneubau. **Reines DDL, keine Saat:** Beide Spalten stehen überall auf NULL,
> und NULL heißt die Vorgabe 22 bis 6 Uhr, abgeleitet aus den Stunden des Tagsollwerts und bitgleich mit
> dem Fahrplan davor. Der Tagesbilanz-Weg (Projekt 1040) liest die Spalten nicht.
>
> Die Gebäudesimulations-Sitzung (G4) zog den Schritt auf der Fassung von origin mit Schemastand **143**
> (`76dd9e48…`, vor der Datenpflege E24) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`
> nach: 4 von 4 Spalten angelegt, Sicht mit 101 Spalten, Marker 144; ein zweiter Lauf legt nichts an.
> Zellvergleich aller Tabellen gegen die Fassung 143: allein `SchemaVersion` 143 → 144, die vier neuen
> Spalten überall NULL und die Schematexte von `Tab_Gebaeude`, `Tab_Gebaeude_STAMM` und
> `Abfrage_Projektgebaeude`; Zeilenzahlen unverändert. `integrity_check` ok, `foreign_key_check` leer,
> 144 Tabellen (alle STRICT), 14 Sichten, 219 Indizes samt den von SQLite angelegten. Größe 68 714 496
> Byte (LFS-SHA-256 `9a71b714…`). **Keine Einfrierregel ist berührt** — die Spalten sind leer, keine
> gesäte Gebäudeangabe ändert sich. Referenzlauf aller vierzehn Projekte gegen die Basis: **14/14
> PASS** (4 610 207 Werte), 432/432 CSV byte-gleich.
>
> **Zusammenführung mit der Datenpflege E24 (#514):** Beide Fassungen gingen von `76dd9e48…` aus — die
> Datenpflege (Träger 63 an den Kesseln 10369 und 11205, Erdgaszeilen 10130 und 10185 für 1023) und der
> Schemaschritt 144. Zusammengeführt wurde, indem das Pflegeskript `e24_pflege` (wiederholbar; Vorzustand
> geprüft, `integrity_check` ok, `foreign_key_check` leer) auf der Fassung 144 (`9a71b714…`) lief:
> Ergebnis Schemastand 144 mit den gepflegten Zellen, 68 714 496 Byte, LFS-SHA-256 `19a7b632…`. Gegen
> R18 bleibt der Referenzlauf 14/14 PASS byte-gleich (Nachweis im Gate der Statuszeile #518).

> **Nachtrag Prüfprojekt 1048 (ohne Referenzrolle), die Basis bleibt.** Auf der Fassung `19a7b632…`
> (Schemastand 144, mit den Zellen der Datenpflege E24) hat
> `dotnet run Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs -- Referenzlaeufe/Kenndaten_Test.sqlite`
> das Projekt 1048 „Prüfprojekt PV mit Preisen“ angelegt (Aufbau im Abschnitt „Das Prüfprojekt 1048“ von
> `Referenzlaeufe/LIESMICH.md`). Zellvergleich aller Tabellen samt `sqlite_sequence` gegen `19a7b632…`: Schema gleich
> (145 Tabellen), **keine bestehende Zeile entfernt oder geändert**, 44 537 neue Zeilen in 30 Tabellen
> (davon 35 040 Viertelstundenwerte der Stromganglinie, 8 760 Solarwerte, 365 Klimatage), dazu 25 fortgeschriebene
> Zähler in `sqlite_sequence`; `integrity_check` ok, `foreign_key_check` leer; ein zweiter Lauf findet
> nichts zu tun. Größe 70 680 576 Byte (kein `VACUUM`), LFS-SHA-256 `b68638da…`. **Keine Einfrierregel
> ist berührt** — 1048 ist kein Referenzprojekt, und die Vorlage 1040 bleibt Zelle für Zelle.
> Referenzlauf aller vierzehn Projekte gegen R18 auf dieser Fassung: **14/14 PASS** (4 610 207 Werte),
> 432/432 CSV byte-gleich; gegen R19 nach der Zusammenführung mit E27 erneut 14/14 (Nachweis in der
> Statuszeile #521).

> **Nachtrag Schemaschritt 145 (die Zeilen des Bedarfstag-Konstruktors), die Basis bleibt.** Auf der
> Fassung `b68638da…` (Schemastand 144) hat
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`
> den Schemaschritt **145** des Zapfprofilgenerators (T5 „Konstruktor", Anwenderentscheid ZU25)
> nachgezogen: die Tabelle `Tab_TwwKonstruktorzeile` (STRICT, zehn Spalten, `ID_TwwProjekt` mit
> `ON DELETE CASCADE`, natürlicher Schlüssel ID_TwwProjekt/Reihenfolge, kein eigener Index) und das
> `DROP INDEX` des redundanten Index `Tab_TwwMessreihe_ID_Projekt`. Zellvergleich aller Tabellen gegen
> `b68638da…`: **genau zwei Unterschiede** — der Marker `Tab_Applikation.SchemaVersion` 144 → 145 und die
> neue, LEERE Tabelle; kein `CREATE`-Text einer bestehenden Tabelle, Sicht oder Trigger geändert, **keine
> Zeile entfernt, hinzugefügt oder geändert** (alle 27 Projekte samt dem Prüfprojekt ohne Referenzrolle
> stehen Zelle für Zelle). Bei den Indizes fällt `Tab_TwwMessreihe_ID_Projekt` weg, der UNIQUE-Index der
> neuen Tabelle kommt hinzu. STRICT-Tabellen 144 → **145**; `integrity_check` ok, `foreign_key_check`
> leer; ein zweiter Lauf legt 0 Tabellen an und 0 Spalten. Größe 70 590 464 Byte (`VACUUM` des
> Werkzeugs), LFS-SHA-256 `cba0aa41…`. **Keine Einfrierregel ist berührt** — reines DDL, kein Rechenweg
> liest eine Konstruktorzeile, und ein Index ändert kein Ergebnis, nur den Weg dorthin. Referenzlauf der
> sechs CI-Projekte gegen R19 auf dieser Fassung: **6/6 PASS** (198 CSV, 2 208 587 Werte; Nachweis in der
> Statuszeile #522).

> **Nachtrag Schritt 146 (Namensabgleich der Baustoffe) ohne Neufreigabe, die Basis bleibt.**
> Migrationsschritt **146** (`SCHRITT_BAUSTOFFABGLEICH`; die Nummer steht allein bei
> `BaustoffabgleichSchema.SCHRITT`, der Quelle für Migration, Werkzeug und Testvorrichtung; er folgt auf
> den Konstruktor des Zapfprofilgenerators, 145) legt `Tab_Baustoffsynonym_STAMM` (Synonyme der Auslieferung: normalisierter
> Materialname, Sprache, Verweis auf `Tab_Baustoff_STAMM`, Quelle, `ReadOnly`) und `Tab_Baustoffzuordnung`
> (gemerkte Zuordnungen je Projekt: `ID_Projekt`, normalisierter Materialname, Verweis auf
> `Tab_Baustoff_STAMM`, Zeitpunkt) an, beide STRICT mit Löschweitergabe, dazu vier Indizes, und sät
> 212 Synonyme mit festen Ids und `ReadOnly = 1`. Auf der Fassung `cba0aa41…` (Schemastand 145, mit der
> Tabelle der Konstruktorzeilen) zog
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`
> den Schritt nach: 2 von 2 Tabellen, 212 von 212 Synonymen, Marker 146; ein zweiter Lauf findet den
> Schritt stehend. Zellvergleich aller Tabellen samt `sqlite_sequence` gegen `cba0aa41…`: die zwei neuen
> Tabellen und vier Indizes, `SchemaVersion` 145 → 146 und ein neuer Zähler in `sqlite_sequence`
> (`Tab_Baustoffsynonym_STAMM` auf 9 999, die Saatgrenze); keine bestehende Zeile und kein bestehender
> Schematext geändert, die Zuordnungstabelle leer, die Konstruktorzeilen unberührt. `integrity_check` ok,
> `foreign_key_check` leer, 147 Tabellen (alle STRICT), 14 Sichten, 223 Indizes samt den von SQLite
> angelegten. Größe 70 631 424 Byte (nach `VACUUM`), LFS-SHA-256 `91362688…`. **Keine Einfrierregel ist
> berührt** — sie nennen weder Baustoffe noch Synonyme, und kein Rechenweg liest die Tabellen.
> Referenzlauf aller vierzehn Projekte gegen R19 auf dieser Fassung: **14/14 PASS** (4 610 207 Werte),
> 432/432 CSV byte-gleich.

> **Nachtrag Schritt 148 (Baualtersklassen nach Bauzeitraum, Energiestandard; Entscheid E47) ohne
> Neufreigabe, die Basis bleibt.** Migrationsschritt **148** (`SCHRITT_BAUALTERSKLASSEN`; die Nummer steht
> allein bei `BaualtersklassenSchema.SCHRITT`, der Quelle für Migration, Werkzeug und Testvorrichtung; er
> folgt auf die Zonenkopplung, 147) legt an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` die Spalte
> `Energiestandard` (TEXT, `CHECK` auf die elf Codes, NULL = keiner) an, schlüsselt die gespeicherten
> Baualtersklassen A…U einmalig auf die Bauzeiträume A…M um (das Baujahr führt, sonst die Tabelle des
> Konzepts Baualtersklassen, Abschnitt 5; der Energiestandard folgt derselben Tabelle), benennt die
> Auslieferungssätze mit dem alten Buchstaben im Namen um und baut die Sicht `Abfrage_Projektgebaeude` zum
> siebten Mal neu (102 Spalten). Auf der Fassung `40c9cf26…` (Schemastand 147) zog
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`
> den Schritt nach: 2 von 2 Spalten, 28 Projektkopien und 267 Katalogsätze umgeschlüsselt, 41
> Protokollzeilen für Klassen ohne eindeutigen Bauzeitraum (34 × Niedrigenergiebauweise und 3 × Passivhaus
> ohne Baujahr → J, 4 × „Eff. 155" → L ohne Standard), keine Umbenennung (kein Katalogsatz der
> Testdatenbank trägt `ReadOnly = 1`), Marker 148; ein zweiter Lauf legt nichts an und verschiebt keinen
> Buchstaben. Zellvergleich aller Tabellen gegen `40c9cf26…`: allein `SchemaVersion` 147 → 148, die
> Baualtersklasse von 28 Projektkopien (A 18 → B, D 2 → E, F 4 → G, G 2 → H, H 2 → I) und 264 Katalogsätzen,
> die neue Spalte (Katalog: 34 × `NIEDRIGENERGIE`, 3 × `PASSIVHAUS`, 3 × `EH70`, sonst NULL; Projektkopien
> NULL) und die Schematexte der zwei Gebäudetabellen und der Sicht; keine Zeile entfernt oder hinzugefügt,
> kein Name geändert. `integrity_check` ok, `foreign_key_check` leer, 149 Tabellen (alle STRICT), 14
> Sichten, 228 Indizes. Größe 70 676 480 Byte (nach `VACUUM`), LFS-SHA-256 `b02fa02e…`. **Keine
> Einfrierregel ist berührt** — Baualtersklasse und Energiestandard sind keine Spalten des Gebäudemodells
> im Sinn der Regel, kein Rechenweg liest sie. Referenzlauf aller vierzehn Projekte gegen R19 auf dieser
> Fassung: **14/14 PASS** (4 610 207 Werte), 432/432 CSV byte-gleich.

> **Die Vorgängerbasis `2026-09-25_R18_PvAusweis`**, die Basis des PV-Ausweises (Stromproduktion der
> Photovoltaik ist die Erzeugung der Module, E26), ist mit dieser Einfrierung aus dem Arbeitsbaum
> gefallen; ihr Protokoll samt A/B-Tafel gegen R17 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->

## Die Basis R20 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis" hat am 26.09.2026 die Basis R20 beschrieben — Anlass (ZU7: Projekt 1045
rechnet sein Brauchwasser über den Zapfprofilgenerator), die A/B-Tafel gegen R19, die Übernahme aus R19
und den Nachtrag Schemaschritt 147. Er steht hier im Wortlaut; die Verweise sind auf diesen Ort
umgestellt.

**Abgelöst wurde R20 durch `2026-09-26_R21_BhkwDeckung`** (Welle E30, #548, Befund N10: der
Stromdeckungsgrad des BHKW ist sein Eigenverbrauch am Bedarf aller Verbraucher). Allein
`BHKW.Strombedarfsdeckung` in `aggregate.csv` von 1017, 1024 und 1047 wechselt, 429/432 CSV bleiben
byte-gleich; die Tafel steht im Abschnitt „Aktuelle Basis" von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT, BEGINN -->

**`2026-09-26_R20_Zapfprofil/`** — **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047), **432 CSV**, **2 447 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand **148**,
LFS-SHA-256 `3ac19fa9…` — reine Saat auf der unveränderten Fassung `b02fa02e…`, kein Schemaschritt;
Herkunft der Fassung im Nachtrag unten). Gegen diese Basis hält `.github/workflows/kern.yml` (1030,
1007, 1017, 1045, 1046, 1047) jeden Push, `ios.yml` den iZ6-Vergleich für 1030,
`EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040 und
`EPOS.Kern.Tests/ZapfprofilReferenzprojektWacheTests` die Generator-Bilanz von Projekt 1045. Sie
ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: Projekt 1045 rechnet sein Brauchwasser über den Zapfprofilgenerator** (Umsetzungskonzept
> Zapfprofilgenerator 3.4, Anwenderentscheid ZU7). Bislang deckte kein Referenzprojekt den
> Generatorweg ab — er lief nur gegen eine Projektkopie der Testdatenbank
> (`ZapfprofilWeicheTests`). [`Skripte/referenzprojekt_zapfprofil.py`](../../../Referenzlaeufe/Skripte/referenzprojekt_zapfprofil.py)
> stellt Projekt 1045 „Prüfprojekt Ost/West Stränge" um: eine Projektzeile in `Tab_TwwProjekt`
> (`Weg = GENERATOR`, Seed 1045, 10 Realisierungen, P99, Zirkulationsmethode Flächenkennwert) und
> eine Zone der Nutzungsart „Wohnen groß (abgeleitet)" am Gebäude 10651 (8,3 Personen, Bilanzgrenze
> Zapfstelle wie der Bestandsweg). Genau zwei neue Zeilen, wiederholbar (zweiter Lauf 0/0),
> gehalten von der neuen Einfrierregel „gesäte Zapfprofil-Eingaben" (oben) und
> `ZapfprofilReferenzprojektWacheTests`.
>
> **A/B gegen R19** (14 Projekte): **13/14 PASS und byte-gleich**, 416/432 CSV byte-gleich; allein
> 1045 FAIL mit 16 von 32 Dateien (91 713 Abweichungen von 324 299 Werten) — der Bestandsweg rechnete
> eine feste Jahresreihe, der Generator eine tagesgangbasierte Bilanz mit anderem Stundenprofil bei
> nahezu gleicher Jahresenergie:
>
> | Projekt, Datei, Größe | R19 | R20 |
> |---|---|---|
> | 1045 `aggregate.csv` `Energiebedarf.Waermebedarf_Brauchwasser` | 5,00 | 5,01 |
> | 1045 `aggregate.csv` `Vektor.waermebedarf_brauchwasser.Summe` | 5 000 | 5 006,62138 |
> | 1045 `aggregate.csv` `Energiebedarf.Waermebedarf_Gesamt` | 80,94 | 80,95 |
> | 1045 `aggregate.csv` `Vektor.waermebedarf.Summe` | 80 940,6927 | 80 947,3141 |
> | 1045 `aggregate.csv` `Waermepumpe.Deckung_Brauchwasser` | 3,65 | 3,66 |
>
> Die zusätzlichen 6,62 kWh/a wandern in die Deckung der Wärmepumpe (`Heizkessel.Deckung_Brauchwasser`
> bleibt bei 2,53 MWh/a); das andere Stundenprofil verschiebt zugleich, wann die Wärmepumpe Strom
> zieht, und damit die stundenweise PV-Eigenverbrauchszuordnung (`pv_produktion.csv`,
> `pv_reststrom.csv`, `pv_strombedarf.csv`, `pv_ueberschuss.csv`, `reststrom_viertelstunde.csv`) mit —
> die theoretische Erzeugung (`pv_produktion_theoretisch.csv`) bleibt unverändert. Die übrigen
> geänderten Dateien sind `kessel_leistung.csv`, `kessel_restwaerme.csv`, `kessel_waermebedarf.csv`,
> `waermebedarf.csv`, `waermebedarf_brauchwasser.csv`, `waermebedarf_dauerlinie.csv`,
> `wp_produktion.csv`, `wp_strom.csv`, `wp_waermebedarf.csv`, `wp_warmwasserbedarf.csv`.
>
> **Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet; NaN nur in den gewollten Lücken der
> Vorlauf- und Rücklaufreihen von 1047 (wie in R19).
>
> **Determinismus geprüft:** mehrere Läufe desselben Standes — vor und nach dem Merge von origin, auf
> unveränderten Zahlen — **14/14 byte-gleich** (432/432 CSV); der Einfrierlauf ist mit allen
> byte-gleich.
>
> ```bash
> py Referenzlaeufe/Skripte/referenzprojekt_zapfprofil.py Referenzlaeufe/Kenndaten_Test.sqlite
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 \
>   --ziel Referenzlaeufe/2026-09-26_R20_Zapfprofil
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis.

> **Übernommen aus R19: der Netzbezug-Fix (E27) und drei weitere Schemaschritte, alle ohne
> Rechenwirkung auf die vierzehn Projekte.** R19 selbst war am 25.09.2026 mit dem Anwenderentscheid
> „der Netzbezug ist nie negativ" (Etappe E27, Befund N5 aus E26) eingefroren worden — die Kaskade
> zieht seither die BHKW-Erzeugung ungeklemmt vom Reststrom ab und klemmt erst am Ende jeden Wert
> unter 0 auf 0; A/B-Tafel gegen R18 (12/14 PASS, 1018 und 1030 FAIL) und Determinismus stehen in
> ihrem archivierten Protokoll
> ([`Dokumentation/ueberholt/Referenzbasen/2026-09-25_R19_BhkwNetzbezug/protokoll.txt`](2026-09-25_R19_BhkwNetzbezug/protokoll.txt)).
> Auf ihrer Fassung `cba0aa41…` (Schemastand 145, Schemaschritt 145 „Konstruktorzeilen" des
> Zapfprofilgenerators) sind seither drei weitere Schemaschritte ohne Referenzrolle gefallen: **146**
> (Namensabgleich der Baustoffe — `Tab_Baustoffsynonym_STAMM` und `Tab_Baustoffzuordnung`, 212 gesäte
> Synonyme, Fassung `91362688…`), **147** (Zonenkopplung, Fassung `40c9cf26…`) und **148**
> (Baualtersklassen nach Bauzeitraum samt Energiestandard, Entscheid E47 — 28 Projektkopien und 267
> Katalogsätze auf die neue Buchstabenfolge umgeschlüsselt, keine Spalte des Gebäudemodells im Sinn
> der Einfrierregel, Fassung `b02fa02e…`). Jeder Schritt: reines DDL bzw. Katalogsaat,
> `integrity_check` ok, `foreign_key_check` leer, Referenzlauf 14/14 PASS byte-gleich gegen R19.
> **R20 setzt unmittelbar auf `b02fa02e…` auf** — die Saat von ZU7 ist die einzige inhaltliche
> Änderung seit R19; Einzelheiten zu 144 und dem Prüfprojekt 1048 (ohne Referenzrolle) stehen im
> archivierten Protokoll.

> **Schemaschritt S-G (147, Zonenkopplung) ohne neue Basis.** Der Schritt legt `Tab_Bauteil.ID_Nachbarzone`
> und `Tab_Bauteil.Trennflaeche_Zuordnung` an, dazu `Tab_Zonenluftstrom` und `Tab_ErgebnisZone` (STRICT).
> Die Testdatenbank wurde aus der origin-Fassung (Schemastand 146) mit `Werkzeuge/Testdatenbankschema`
> nachgezogen — zwei Spalten, zwei Tabellen, fünf Indizes; ein zweiter Lauf 0/0. Zellvergleich über
> 10 893 413 Zellen: einzige Abweichung `Tab_Applikation.SchemaVersion` 146 → 147, die neuen Spalten
> leer, die neuen Tabellen leer; `integrity_check` ok, `foreign_key_check` leer, STRICT 149 von 150;
> 70 664 192 Byte, LFS-SHA-256 `40c9cf26626e4c13461dc64d3c9f57cd6c79eeeac00453ee54b989b48f2efb0f`.
> Referenzlauf 14/14 PASS, 432/432 CSV byte-gleich gegen R19. Kein Referenzprojekt trägt Zonen, keine
> Einfrierregel ist berührt; mit der Freischaltung (G6b W5) bleibt der Lauf gegen R20 14/14 PASS und 432/432 CSV byte-gleich.

> **Die Vorgängerbasis `2026-09-25_R19_BhkwNetzbezug`** ist mit dieser Einfrierung aus dem Arbeitsbaum
> gefallen; ihr Protokoll steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

<!-- ÜBERNOMMENER ABSCHNITT, ENDE -->
