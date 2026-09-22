# Die Protokolle der 28 entfernten Referenzbasen

**Was hier liegt.** Für jede der **28 historischen Referenzbasen** unter `Referenzlaeufe/` das
Protokoll ihrer Entstehung — `lauf_protokoll.md` beziehungsweise `protokoll.txt`, byte-gleich
aus dem Stand `b02f986^` (= dem letzten Commit vor der Löschung) gesichert; das Protokoll von R7 kam am
16.09.2026 dazu, das von R8 am 18.09.2026, das von R9 am 19.09.2026, das von R10 am 22.09.2026. **29 Dateien.** Nur Text: was gemessen wurde, gegen welche Vorgängerbasis, mit welchem
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
> [`Referenzlaeufe/2026-09-22_R11_Bestandsbefunde`](../../../Referenzlaeufe/2026-09-22_R11_Bestandsbefunde/);
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
