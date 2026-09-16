# Die Protokolle der 26 entfernten Referenzbasen

**Was hier liegt.** Für jede der **26 historischen Referenzbasen** unter `Referenzlaeufe/` das
Protokoll ihrer Entstehung — `lauf_protokoll.md` beziehungsweise `protokoll.txt`, byte-gleich
aus dem Stand `b02f986^` (= dem letzten Commit vor der Löschung) gesichert; die Protokolle von R7 und
R8 kamen am 16.09.2026 dazu. **27 Dateien.** Nur Text: was gemessen wurde, gegen welche Vorgängerbasis, mit welchem
Ergebnis, welche Abweichung gewollt war und welche Gegenprobe sie belegt.

**Warum hier.** Die Ordner der Basen sind am 11.09.2026 mit dem Sync-Commit `b02f986` aus dem
Arbeitsbaum gefallen (Anwenderentscheid **SYNC‑Q1**: „entfernt lassen"); abrufbar blieben sie
über die Git-Geschichte. Mit dem Anwenderentscheid **AUF‑Q1** vom **12.09.2026** („ausführen",
Auftrag #244) ist die Geschichte umgeschrieben worden — **die Basen sind seither auch dort
nicht mehr enthalten.** Damit wäre die Begründung jeder einzelnen Basiswahl verloren gegangen.
Deshalb sind die Protokolle **vor** dem Umschreiben hierher gesichert worden.

> **Die Messdaten selbst sind endgültig weg.** Die rund **8 400 CSV-Dateien** der 26 Basen
> sind weder im Arbeitsbaum noch in der Git-Geschichte. Wer eine alte Zahl braucht, findet
> sie **nur noch im Protokoll** — oder rechnet sie neu. Die einzige lauffähige Basis ist
> [`Referenzlaeufe/2026-09-16_R9_Energietraeger_Brenner`](../../../Referenzlaeufe/2026-09-16_R9_Energietraeger_Brenner/);
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
| `2026-09-16_R8_Heizkessel_Kaskade` | 16.09.2026 | CI-Basis nach Anwenderentscheid HK‑E‑1 (ein Heizkessel, den das Projekt führt, bekommt seinen Kaskadenplatz automatisch und nachrangig); dreizehn Projekte, 357 CSV, 2 057 Skalare — abgelöst durch R9 am 16.09.2026 | [`2026-09-16_R8_Heizkessel_Kaskade/protokoll.txt`](2026-09-16_R8_Heizkessel_Kaskade/protokoll.txt) |

## Die Basis R7 im Einzelnen (aus `Referenzlaeufe/LIESMICH.md` übernommen)

Der Abschnitt „Aktuelle Basis" hat bis zum 16.09.2026 die Basis R7 beschrieben — Anlass,
Aufbau des Prüfprojekts 1046 und die sechs Nachträge, mit denen die Basis die Schemastände 74
bis 81 unverändert überstanden hat. Er steht hier im Wortlaut, weil die Herleitung des
Prüfprojekts 1046 und die Begründung seiner Flottengrößen weiterhin gebraucht werden: Das
Projekt selbst lebt in der Testdatenbank weiter und trägt die dritte Einfrierregel.

**Abgelöst wurde R7 durch `2026-09-16_R8_Heizkessel_Kaskade`** (Anwenderentscheid HK‑E‑1 vom
15.09.2026: Ein Heizkessel, den das Projekt führt, bekommt seinen Kaskadenplatz automatisch —
nachrangig). Drei der dreizehn Projekte verschieben sich dadurch (1007, 1008, 1046), zehn
bleiben byte-gleich; die Tabelle der Abweichungen steht in
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

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

Der Abschnitt „Aktuelle Basis" hat am 16.09.2026 für wenige Stunden die Basis R8 beschrieben.
Er steht hier im Wortlaut, weil die Messung des Kaskadennachzugs weiterhin gebraucht wird: Sie
ist der Nachweis dafür, dass ein Heizkessel ohne eigenen Platz seither mitrechnet.

**Abgelöst wurde R8 durch `2026-09-16_R9_Energietraeger_Brenner`** (Anwenderentscheid vom
16.09.2026: jede Brenner-Anlage bekommt ihren Energieträger). Sieben der dreizehn Projekte
verschieben sich dadurch (1007, 1008, 1017, 1018, 1023, 1024, 1046 — 1018 und 1024 nur in der
`carrier_id`), sechs bleiben byte-gleich; die Tabelle der Abweichungen steht in
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

<!-- ÜBERNOMMENER ABSCHNITT R8, BEGINN -->

**`2026-09-16_R8_Heizkessel_Kaskade/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023,
1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046), **357 CSV**, **2 057 Skalare**, gerechnet mit
dem plattformfreien `EPOS.Referenzlauf` auf Linux gegen `Kenndaten_Test.sqlite`
(**Schemastand 82**).

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
> **Zwei weitere Änderungen jener Welle verschieben die Basis NICHT** und sind deshalb
> getrennt gemessen: Die Bereinigung der zwei Probierpuffer „test" (2 Ltr, 4 000 €) in 1007
> und 1046 ist **13/13 byte-gleich** — sie hängen an keiner Zeile von `Z_ProjektPufferSp` und
> rechnen im hydraulischen Weg nicht mit. Und im Größenlauf der Speicherflotte wurde **nichts
> geändert**.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich**,
> Toleranzvergleich **13/13 PASS** (3 882 737 Werte), Laufzeit 00:00:06.

<!-- ÜBERNOMMENER ABSCHNITT R8, ENDE -->

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
