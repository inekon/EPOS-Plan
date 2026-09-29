# E25 — Bericht Phase 0 (Opus, 25.09.2026 ca. 15:15, Worktree e25 ab 4434983b; Testdatenbank 76dd9e48, Schemastand 143 = SCHRITT_BAUSTOFF_QUELLEN)

Befund: Lücke bestätigt (e9a_berichte.md:163,169) — `Tab_ProjektPhotovoltaik` 0 Zeilen, `Tab_ProjektTarif` 0 Zeilen, PV-Kostenpositionen (Komponente 3)
nur in 1026 mit 0, Stromträger 60 mit Preis nur in 1030. Freie Projekt-ID 1048 (`sqlite_sequence` 1047; `ProjektDuplizierenCtrl.cs:378` MAX+1).
Vorlage 1040 (WP, Kessel Träger 63, PV 20 × 260 W = 5,2 kWp, fünf Puffer, Kühlbetrieb 0, 20 Investitionszeilen 54.975,50 €); Gebäude 10645
`TAGESBILANZ` → in der Kopie `Gebaeude_Modell` NULL (VDI 6007, wie 1045); ausgeschieden 1045 (Ost/West, Wechselrichter kappt), 1026 (Prüffall
ohne Strom), 1028 (Batterie/Solar schlucken Überschuss), 1007/1046 (Brennstoff ohne Träger, Flotte), 1017/1047 (Kühlbetrieb). Kopierweg:
`ProjektDuplizierenCtrl.Duplizieren` aus einem dotnet-Dateiskript (kopiert keine Ergebnisse/keine Tab_ProjektPhotovoltaik; Probe: ID 1048, 20
Kostenzeilen, Trägerzeile 63). Kostenpositionen ohne neue Katalogzeile: `Tab_Kostenfaktor` 80 Photovoltaik, 149 Wartung/Inspektion PV, 150
Instandhaltung PV; NICHT über `KostenVorlagenUebernahmeCtrl.AusVorlage` (legte fünf Katalogzeilen an, `NutzungsdauerID` → bräche
`NutzungsdauerTests.cs:140`); Szenariowerte der Positionen sind €-Beträge (`InvestKaskade.cs:200`). Szenarien: Spalten `energy_project_settings.
custom_price_*_best/_worst`, `Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung_Best/_Worst`, `Szen_*`, `Tab_ProjektPhotovoltaik.DvEntgelt/
PpaPreis_Best/_Worst` (Konzept § 2.11.5 Z. 905–1023); flache Einspeisevergütung neben aktivem Vergütungsdialog wirkungslos (E9a‑Q7). Keine
Ergebniszeilen nötig (`BerichtsDatenSammler.cs:363-376` rechnet frisch, 0,2–0,4 s; 1045/1046/1047 ebenso ohne). Werkzeuge: Testdatenbankschema
generisch; Auslieferungsvorlage löscht alle Projekte (`Projektsicht.cs`, `Vorlagenbau.cs:94-108`), 36 Tests zählen keine Projekte (Voraussetzung:
keine neue Katalogzeile). Wiki-Wache (`WikiProduktdatenWacheTests.cs:88-95`) liest Namen auch aus Projekttabellen → Gerätezeilen nicht umbenennen,
nur Projektname/Beschreibung neutral. Zählende Tests nachziehen: `GebaeudeKatalogverweisTests.cs:72-73` 27→28; `ErgebnisansichtTests.cs:673-691`
101/95 Investitionszeilen und 27/33; `PreisbasisSchrittTests.cs:51-52` kWh 8→9, Nm³ 17→18 (nach E24 19). Überschneidung E24 (LFS): IDs zur Laufzeit,
Skript wiederholbar. Messprobe (Kopie 1040, VDI 6007; Strom 0,30 €/kWh + 120 €/a, Gas 0,80 €/Nm³ + 150 €/a, Einspeisung 0,08 €/kWh, 1.200 €/kWp,
Wartung 150 €/a + 1 %): 20 Module 5,20 kWp Eigenverbrauch 2,60 MWh, Einspeisung 2,44 MWh, Energiekosten 10.137 €/a, Invest 61.216 €, Erlös PV
195,20/214,72/175,68 €/a, KW Erwartet −247.194,45 €; 40 Module 10,40 kWp: 3,07 / 7,06 MWh, 9.507 €/a, 67.456 €, 564,80/621,28/508,32 €/a,
KW −237.134,73 €.
Entwurf 1048 „Prüfprojekt PV mit Preisen" (keine Referenzrolle), Skript `Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs`: (1) Kopie 1040 → 1048,
Beschreibung neutral mit Anlass; (2) Gebäude `Gebaeude_Modell` NULL; (3) PV_Leistung 20 → 40 (10,40 kWp, kopiertes Modul bleibt); (4) Träger Gas 63
0,80 €/Nm³ (0,70/0,95), Grund 150 €/a; Strom 60 neu nach Muster 1030-Zeile 10068 (Umrechnung 51, hi/hs 1, co2/so2/nox 560/200/280, kWh) 0,30 €/kWh
(0,26/0,36), Grund 120 €/a (100/150), Leistung 0; (5) `Tab_ProjektWirtschaftlichkeit` über `SpeichereParameter(LadeParameter)`: Zins 3 %, T 20 a,
p_E 2 %, p_B 1,5 %, Einspeisevergütung PV 0,08 (0,10/0,06); (6) `Tab_ProjektWerte` PV: Invest StammID 80 EUR_PRO_KWP 1.200 €/kWp = 12.480 €
(10.400/14.560), Nutzungsdauer 25 a ohne NutzungsdauerID; Betrieb 149 JAHRESBETRAG 150 €/a (120/180) Pflicht; 150 PROZENT_INVESTITION 1 % Pflicht;
20 kopierte Zeilen bleiben; (7) keine Vergütungszeile, kein Tarif, keine Ergebnis-/Katalogzeilen, kein VACUUM; wiederholbar (Ziel erreicht → nichts;
Abweichung/ID ≠ 1048 → Abbruch rc 2, eine Transaktion). Merge-Reihenfolge: E24 (R17) zuerst, dann Skript auf dessen DB, ein LFS-Objekt.
Testplan E25/2 `PvPreisProjektTests` (Arbeitskopie, frisch simuliert): Aufbau-Wache (nicht in Referenzlisten; Kühlbetrieb 0, VDI 6007, 10,40 kWp
`KwpSumme`; zwei Träger mit Preis, Parametersatz, keine Vergütungs-/Ergebniszeile); Erlösrubrik PV = Einspeisung × 0,08/0,10/0,06; vermiedene Kosten
(Eigenverbrauch > 0, Zeile „PV: vermiedener Bezug" Flat); Szenarien C/D (Trägerpreise + Einspeisevergütung wirken, Erwartet bitgleich, Ausweis „n von m",
Dialogweg DV/PPA auf Kopie); Formelmappe PV-Block, Parameterzeilen je Szenario, ClosedXML „abweichend 0"; Kapitalwert (Beziehungen + Anker 1e‑6);
Kosten (Herleitung „× 10,40 kWp" = 12.480 €, Instandhaltung 1 %). E25/3: voll, Referenzlauf 14/14 byte-gleich, SQL-Prüfer, Auslieferungsvorlage,
Testdatenbankschema --trocken 0/0, Zellvergleich, integrity/foreign_key. Aufwand ~5 h + Gate.
Fragen (Entscheid Orchestrator 25.09.2026 ~15:20, alle a): Q1 Kopie 1040 VDI 6007; Q2 40 Module; Q3 flache Vergütung (DV/PPA nur Test); Q4 Flat;
Q5 Werte wie Entwurf; Q6 Beziehungen + KW-Anker 1e‑6; Q7 direkte Zeilen ohne NutzungsdauerID; Q8 erst E24 mergen, dann Skript einmal committen;
Q9 Skript unter Referenzlaeufe/Skripte + LIESMICH; Q10 kopierte Zeilen (inkl. Solarthermie 3.775 € ohne Anlage) unverändert.
**Nebenbefunde:** N1 `Ergebnis.Photovoltaik.Stromproduktion` liegt unter dem Überschuss (10,4 kWp: 6,37 MWh Produktion gegen 7,06 MWh Überschuss,
obwohl Eigenverbrauch + Einspeisung 10,13 MWh; R16 1040: 4,44 MWh gegen 2,27 + Eigenverbrauch; Ansatz `SimulationRunner.cs:989` Stundenreihe vs.
Viertelstunde) — E25 verankert die Stromproduktion nicht, **Kern-Befund für den Anwender**; N2 Modul der Kopie rechnet mit Ersatzwerten (T_NOCT 0 → 45,
gamma_PMP 0), Bestand. Bau freigegeben (E25/1 Skript + E25/2 Tests jetzt gegen Arbeitskopie; LFS-Commit erst nach E24-Push).

# E25 — Zwischenbericht E25/1+2 (Opus, 25.09.2026 ca. 15:50, e25 = 51eee6a6 über 4434983b; Testdatenbank unverändert 76dd9e48, kein LFS-Commit)

Commit 51eee6a6 „E25/1+2": Skript `Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs` (Aufruf `dotnet run … -- <db> [--trocken]`; prüft Vorlage
1040 [Tagesbilanz-Gebäude, 20 × 260 W, nur Erdgas, kein Parametersatz, keine PV-Kosten, 20 Kostenzeilen, Kühlbetrieb aus, StammIDs 80/149/150] und
Ziel-ID 1048; kopiert per `ProjektDuplizierenCtrl` in Arbeitsdatei, setzt Zielzellen, prüft jede, 1040 unverändert, integrity/foreign_key, dann ersetzt;
rc 0 angelegt/vorhanden, rc 2 Abweichung; IDs zur Laufzeit; Datumsfelder fest 2026-09-25; PV-Positionen Einheit „€", Herkunftsvorlage 5/16, zwei
Pflichtpositionen ohne NutzungsdauerID). Messung Arbeitskopie: Schema gleich (145 Tabellen), 44.537 Zeilen neu (35.040 Stromganglinie, 8.760 Solar),
25 sqlite_sequence-Zähler (energy_project_settings 10129 → 10131), integrity ok, foreign_key leer, zweiter Lauf „nichts zu tun" rc 0, SQL-Prüfer
1.920/0. Tests `EPOS.Kern.Tests/PvPreisProjektTests.cs` 11/11 grün auf der Arbeitskopie (Entwicklungshaken `EPOS_E25_TESTDB`, fällt mit E25/1b):
Aufbau; keine Referenzrolle; Preissatz; Kapitalwert-Anker 1e‑6 Erwartet −237.134,727 €, Günstig −204.001,508 €, Ungünstig −279.852,534 €;
Einspeiseerlös 7,06 MWh × 0,08 = 564,80 €/a (Günstig × 0,10 × 1,1 = 776,60; Ungünstig × 0,06 × 0,9 = 381,24); Investition 54.975,50 + 12.480 = 67.455,50 €
(59.877,95 / 75.033,05); Betriebskosten 274,80 / 224,00 / 325,60 €/a; Szenario C „4 von 18 Parametern szenariert"; Erwartet bitgleich ohne
Szenariopreise; Szenario D (Marktprämie DV 0,40 ct/kWh [0,20/0,60], Kohärenzzeile WIRT_SZ_PV_DIALOG_EINSPEISUNG; PPA 7,0 [8,0/5,5]); Formelmappe
Parameterblock + ClosedXML. Zählungs-Nachzüge (E25/1b): GebaeudeKatalogverweisTests 27 → 28; ErgebnisansichtTests 101 → 122, 95 → 115, Hinweiszahlen
27/33 nach Merge messen; PreisbasisSchrittTests kWh 8 → 9, Nm³ 17 → 18 (nach E24 19).
**KERN-BEFUNDE (nicht behoben, Tests verankern keine vermiedenen Kosten/PV-Stromproduktion):**
**N1:** `SimulationRunner.cs:989` schreibt `pvm.Stromproduktion = pvs.Stromproduktion.Sum()` — dieselbe Reihe geht als `PV_GENUTZT` in die Zeitreihen
(`ZeitreihenExtraktor.cs:54`) → das Feld „Stromproduktion" ist der selbst genutzte PV-Strom (1048: Modulproduktion 13,43 = 6,37 + 7,06 Überschuss;
1040/R16: 6,71 = 4,44 + 2,27). Folge: `WirtschaftlichkeitCtrl.PvVermiedenerBezugAusweis` (`:6167-6169`) zieht den Überschuss nochmals ab → 1048: 6,37 −
7,06 < 0 → Zeile null, im Flat-Tarif entfällt „PV: vermiedener Bezug".
**N3:** `StromMatrix.Baue` (`StromMatrix.cs:169-212`) nimmt als Bedarf nur `STROMBEDARF` (Haushaltsstrom aus simulation_Strombedarf); WP-Strom, Heizstab,
Hilfsenergie fehlen, `NETZBEZUG` enthält sie → 1048: Bedarf 8,00 MWh, Netzbezug 25,13 MWh, PV-Eigenverbrauch 3,07 statt 6,37 MWh; im Rollentarif
vermiedene Menge −17,13 MWh, vermiedene Kosten −5.140 €/a; trifft vermutlich jedes WP-Projekt; nur Ausweis, nicht Kapitalwert.
Weiter nach „E24 gepusht": Merge, Skript auf Repo-DB, E25/1b (LFS, Haken weg, Zählungen, LIESMICH „Was hier liegt", Anker prüfen), E25/3.

# E25 — Bericht Phase 1 (Opus, 25.09.2026 ca. 21:25, e25 = e0f6d847 über a499feb7 [Merge origin ba798d8a]; Statusnummer #519)

Commits: 51eee6a6 E25/1+2 (Skript `Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs`, `PvPreisProjektTests.cs` Zwischenstand); a499feb7 Merge
origin ba798d8a (konfliktfrei); 65458a10 E25/1b (Testdatenbank LFS, Entwicklungshaken entfernt, zwei Tests nach E26, Zählungen, LIESMICH-Abschnitt
Prüfprojekt); a313d776 E25/3 (drei Wachen an 1048 angepasst, voller Lauf); e0f6d847 E25/3 LIESMICH „Aktuelle Basis" SHA b68638da + Nachtrag 1048.
Projektaufbau: Kopie 1040 → 1048 über `ProjektDuplizierenCtrl` (Kopierweg des Programms), Skript lief einmal auf der Repo-DB 19a7b632 (144, E24-Zellen);
geänderte Zellen der Kopie: Tab_Projekt Beschreibung, Änderungs-/Erstelldatum fest 2026-09-25; Gebäude Gebaeude_Modell TAGESBILANZ → NULL (VDI 6007);
PV_Leistung 20 → 40 Module (10,40 kWp); Erdgas-Zeile (Träger 63, kopiert) 0,80 €/Nm³ (0,70/0,95), Grundpreis 150 €/a. Neue Zeilen: Stromträger 60 nach
Muster 1030 (Umrechnung 51, co2/so2/nox 560/200/280, kWh) 0,30 €/kWh (0,26/0,36), 120 €/a (100/150), Leistungspreis 0; Parametersatz über
`WirtschaftlichkeitCtrl.SpeichereParameter` Zins 3 %, 20 a, Energie 2 %/a, Betrieb 1,5 %/a, Einspeisevergütung PV 0,08 (0,10/0,06); Kostenposition
Kat. 1 StammID 80 1.200 €/kWp = 12.480 € (10.400/14.560), 25 a; Kat. 2 StammID 149 Wartung 150 €/a (120/180), StammID 150 Instandhaltung 1 % der
Investition (Pflichtpositionen, ohne NutzungsdauerID); 20 kopierte Investitionszeilen unverändert; nicht angelegt: Vergütungszeile, Tarifstruktur,
Ergebnis-, Katalogzeilen; kein VACUUM; wiederholbar (zweiter Lauf „steht schon", Rückgabe 0; IDs zur Laufzeit). Zellvergleich gegen 19a7b632: Schema
gleich 145 Tabellen, 0 bestehende Zeilen geändert/entfernt, 44.537 neue Zeilen in 30 Tabellen (35.040 Viertelstundenwerte Stromganglinie, 8.760
Solarwerte, 365 Klimatage, 165 Kennlinienzeilen, 23 Kostenpositionen), 25 Zähler; integrity ok, foreign_key leer; 70.680.576 Byte, LFS-SHA-256
**b68638da…**. Wirkung E26 an 1048: Stromproduktion 6,37 → 13,43 MWh (= Σ Module), Strommatrix-Bedarf 8,00 → 31,50, PV-Eigenverbrauch 3,07 → 6,368,
Einspeisung 7,06 gleich, Rollentarif 0,30: vermiedene Menge −17,13 → +6,368 MWh, vermiedene Kosten −5.140 → +1.910,38 €/a; Kapitalwert-Anker unverändert
Erwartet −237.134,7270351314 / Günstig −204.001,50754334457 / Ungünstig −279.852,53447363194 €; „PV: vermiedener Bezug" nur bei aktivem
Vergütungsdialog (`WirtschaftlichkeitCtrl.cs:6399`), bei 1048 mit flacher Vergütung null (so gebaut); mit Dialog auf Arbeitskopie 6,368 MWh × 0,30/0,26/0,36
(Test). Tests: `PvPreisProjektTests` 13 Fälle (Aufbau, keine Referenzrolle, Preissatz, KW-Anker rel. 1e-6, Einspeiseerlös je Szenario, Investition je kWp,
Betriebskosten, Erzeugung/EV/Einspeisung nach E26, vermiedene Kosten Rollentarif = EV × Bezugspreis, Szenario C „4 von 18 Parametern szenariert",
Erwartet bitgleich, Szenario D mit DV-Entgelt und PPA samt vermiedenem Bezug, Formelmappe ClosedXML); Zählungen GebaeudeKatalogverweisTests 27 → 28 /
23 → 24, ErgebnisansichtTests 101/95 → 122/115 und Hinweise 27/33/7 → 32/39/8, PreisbasisSchrittTests kWh 8 → 9, Nm³ 18 → 19; Wachen:
TestDatenbankEntsorgungWacheTests (Fabrik `Neue()` → überall `new TestDatenbank()` in using/Vorrichtung mit Dispose), SzenarioParameterTests E9a nimmt
1048 aus, StromsteuerBefreiungModusTests nimmt 1048 aus (Parameterdialog schreibt „AUSWEIS", ausdrücklich geprüft). Voller Lauf mit Schaltern: Kern
7.450 (+1 übersprungen), UI 6.395, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (+1), 0 Fehler; Auslieferungsvorlage-Tests 36/36; SQL-Prüfer
1.927/0; Testdatenbankschema --trocken 0/0, Schritt 144; Referenzlauf 14/14 gegen R18 4.610.207 Werte 432/432 byte-gleich; Testhost-Regel einmal
verletzt (gefilterter Lauf 68 Fälle 7 s bei zwei fremden testhosts, grün). Erledigt-Grund: E9a-Befund (e9a_berichte.md:163,169) und E21‑Q9 a; N1/N3 durch
E26 behoben und an 1048 gemessen. LIESMICH: Abschnitt „Das Prüfprojekt 1048 „PV mit Preisen" (ohne Referenzrolle)", Skript unter „Was hier liegt",
Aktuelle Basis SHA b68638da + Nachtrag 1048 (R18 bleibt). Abnahme A‑E25‑1: 1048 öffnen und rechnen; Kapitalwerte Erwartet −237.135 / Günstig −204.002 /
Ungünstig −279.853 €; Erlösrubrik Einspeiseerlös 564,80 / 776,60 / 381,24 €/a; PV-Stromerzeugung 13,43 MWh, „4 von 18 Parametern szenariert";
Formelmappe Parameterblock 0,08/0,10/0,06 und Trägerpreise; Rollentarif vermiedene Kosten ~1.910 €/a. Scratchpad e25\: e25_test_voll2.txt,
rl_vergleich.txt, rl\, zellvergleich.py, vorher_19a7.sqlite.
