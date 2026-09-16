# Befund D — Gebäude- und Klimadaten der Testdatenbank (15.09.2026)

**Protokoll.** Befund eines Prüf-Agenten (Modell Opus, Workflow ‚gebaeudemodell-pruefen‘, Stufe Daten) im Auftrag des Konzepts [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Wortlaut wie geliefert; Zeilenbelege und Zahlen sind im Konzept nach dem Gegenlesen korrigiert, bei Widerspruch gilt das Konzept. Pfade unter `epos-spike` (Temp-Ordner des Rechners) bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

## Befund „Daten" — VDI 6007-1 (7R2C) aus der Testdatenbank

**Werkzeuge** (alles ausserhalb des Repos, Repo unveraendert):
`C:\Users\Dirk\AppData\Local\Temp\epos-spike\DbProbe\{DbProbe.csproj,Program.cs,global.json}` (net10.0, `Microsoft.Data.Sqlite` 10.0.11 aus `Directory.Packages.props:22`, Oeffnung `Data Source=...Kenndaten_Test.sqlite;Mode=ReadOnly`), `C:\Users\Dirk\AppData\Local\Temp\epos-spike\ref.pl` (Referenz-CSV), Rohprotokoll `...\epos-spike\daten\probe_log.txt` (363 Zeilen).

**Gegenprobe der Leseart:** DbProbe rechnet `SolareGewinneC`/`SpezWaermeverlusteC`/`TaeglHeizlastWG` (EPOS.Kern/Allgemein/BhkwPlan.cs:311-435) nach. Die Jahressumme trifft `waermebedarf_gebaeude.csv` jedes Projekts auf < 0,01 % (z. B. 1007: 53 072 nachgerechnet / 53 071,74 Referenz). Alle folgenden Skalierungsfaktoren und Einheiten sind damit nicht gedeutet, sondern nachgewiesen.

---

### A) Vollstaendigkeitsmatrix Gebaeudefelder

13 Projekte, **15 Gebaeudezeilen**. Projekt **1030 hat KEIN Gebaeude** (`gebaeude_1030.json` = `[]`) — sein Waermebedarf (6 137 560 kWh, Max 2 206 kW) kommt vollstaendig aus Ganglinien. Zeilen je Projekt: 1007=1, 1008=2, 1017=1, 1018=1, 1023=1, 1024=1, 1030=0, 1039=3, 1040/1041/1042/1045=1, 1046=1.

Ueber alle 15 Zeilen (n = 15):

| Feldgruppe | NULL | = 0 | gefuellt |
|---|---|---|---|
| Geometrie (Wohnflaeche, Wohnflaeche_gesamt, Flaeche_Aussenwand, gesamte_Fensterflaeche, Dachflaeche, Grundflaeche, Raumhoehe) | 0 | 0 | 15/15 |
| U-Werte AW/Fenster/Dach/Grund | 0 | 0 | 15/15 |
| k_Wert_Sonstiges · Sonstige_Flaechen | 0 | **8** | 7/15 |
| Fensterflaeche_Sued / _Ost_West | 0 | 0 | 15/15 |
| Fensterflaeche_Nord | 0 | **3** | 12/15 |
| Bauweise, Interne_Waermegewinne, Luftwechselrate, Fensterdurchlassgrad, Flaeche_Nutzer, Bewohner | 0 | 0 | 15/15 |
| WBVK 1/2/3 (psi) | 0 | 0 | 15/15 |
| Abmessung Fenster-Wand / Wand-Dach | 0 | 0 | 15/15 |
| Abmessung Aussenwand-Kellerdecke | 0 | **4** | 11/15 |
| Raumsolltemperatur_Tag / _Nachtabsenkung / Maximaleraumtemperatur | 0 | 0 | 15/15 |
| Raumsolltemperatur_Wochenende | **1** (ID 10599) | **14** | 0/15 wirksam |
| Raumsolltemperatur_Ferien | **1** (ID 10599) | **14** | 0/15 wirksam |
| Wochenende · Ferien (Flags) | 0 | **15** | 0/15 |
| Ferienbeginn_1 | 0 | 0 | 15× Wert **366** (= ausserhalb 1..365 ⇒ aus) |
| Ferienende_1, Ferienbeginn/-ende 2-4 | 0 | **15** | 0/15 |
| Waermebedarf [kW] · spez_Waermeverbrauch | 0 | **4** | 11/15 |
| WW_Bedarf, Baualtersklasse, Gebaeudeart, Typ, WNW | 0 | 0 | 15/15 |
| Z_ProjektGebaeude: Wohnflaeche_Waermebedarf, Einheit, Jahresnutzungsgrad | 0 | 0 | 15/15 (JNG stets 1) |
| Z_ProjektGebaeude: dezWarmwasserbereitung | 0 | **15** | 0/15 |

**Textwerte** (ganze `Tab_Gebaeude`): `Typ` = „Wohngebaeude  VDI 2067"(19, zwei Leerzeichen!) / „Wohnblock"(5) / „Hotel"(2). `Baualtersklasse` = A(17), F(4), H(2), G(2), D(1). `Gebaeudeart` = Einfamilienhaus(17), grosses Mehrfamilienhaus(5), Mehrfamilienhaus(2), Hotel(2). `Wohngebaeude_Nicht_Wohngebaeude` = „Wohngebaeude"(24) / „Nicht Wohngebaeude"(2). `Einheit_Waermebedarf_Wohnflaeche` = „Wohnfläche [m²]"(25) / „Brennstoffverbrauch [MWh/a]"(1, ausserhalb der 13 Projekte).

---

### B) Plausibilitaet

| Geb | Projekt | Wfl m² | U_AW | U_F | U_D | U_G | A_AW | A_F | A_D | A_G | Bauw. Wh/K | spez Wh/m²K | IWG W | W/m² | n 1/h | g | h m | m²/Pers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
|10614|1007/1046*|74|0,81|2,5|0,35|0,35|114|16,26|90|54|3 700|50|210|2,84|0,7|0,70|2,25|18,0|
|10576|1008|304|0,48|1,4|0,21|0,47|222|44,8|349|168|**50**|**0,16**|560|1,84|0,7|0,59|2,60|38,0|
|10577|1008|74|0,81|2,5|0,35|0,35|114|16,26|90|54|3 700|50|210|2,84|0,7|0,70|2,25|18,0|
|10599|1017|744,4|0,28|1,3|0,29|0,39|521,3|132,1|946,4|946,4|37 220|50|1 540|2,07|0,7|0,62|2,45|33,8|
|10632|1018|1 975,3|0,757|1,8|0,30|0,55|718,1|345,1|2 820|2 100|98 765|50|4 466|2,26|0,7|0,70|2,50|30,96|
|10628|1023/1024/1039|3 596|0,45|2,5|0,58|1,14|2 713|819,6|1 060|1 060|179 800|50|7 560|2,10|0,7|0,70|2,50|33,3|
|10642|1039|130|1,35|2,8|1,10|1,19|138|22,4|154|154|13 000|**100**|420|3,23|0,7|0,70|2,50|21,6|
|10643|1039|201|**2,90**|2,8|0,80|0,80|280,3|45,06|116,9|88|10 050|50|462|2,30|0,7|0,75|2,75|30,5|
|10645|1040/1041/1042/1045|201|1,84|2,8|0,80|0,80|280,3|45,06|116,9|88|10 050|50|462|2,30|0,7|0,75|2,75|30,5|

(*identische Katalogzeilen, eigene IDs 10652 / 10629 / 10644 / 10646 / 10647 / 10651)

- **U-Werte** 0,28–2,90 W/m²K (AW), 1,3–2,8 (Fenster), 0,21–1,10 (Dach), 0,35–1,19 (Grund) — Bestandsspanne unsaniert bis teilsaniert, plausibel. `k_Wert_Aussenwand = 2,90` (10643) ist der Extremwert (einschaliges Mauerwerk).
- **Flaechenkonsistenz**: `gesamte_Fensterflaeche` = Sued + Ost_West + Nord in **allen 15 Zeilen exakt** (z. B. 10643: 10,5+20,16+14,4 = 45,06). Sauber.
- **Bauweise**: 13× spez. 50 Wh/(m²K) („schwere Bauart"), 1× 100 („sehr schwer"), **1× 0,16 Wh/(m²K)** — Zeile 10576 traegt den nackten Rueckfallwert **50 Wh/K** von `Gebaeudebauweise.BauweiseAusBauart` (EPOS.Kern/Allgemein/Gebaeudebauweise.cs:69, dort als Altlast dokumentiert: frueher kam der Index der GEBAEUDEART-Liste herein). Die RC-Zeitkonstante bricht damit auf Minuten zusammen. Fuer 7R2C unbrauchbar.
- **Innere Gewinne** 1,84–3,23 W/m² — am unteren Rand (DIN V 18599 Wohnen ≈ 2,1 W/m²), konstant ueber das ganze Jahr, kein Tagesprofil.
- **g-Wert** 0,59 / 0,62 / 0,70 / 0,75 — sauber im Bereich 0..1, also Bruch, nicht Prozent.
- **Luftwechsel** 0,7 1/h in allen 15 Zeilen (Einheitswert, keine Differenzierung).
- **Wohnflaeche == Wohnflaeche_gesamt in allen 15 Zeilen** — das Paar traegt keine Information.
- **Nutzung**: Flaeche_Nutzer 18–38 m²/Person; Bewohner konsistent = Wohnflaeche_gesamt / Flaeche_Nutzer.
- **Katalogkennzahlen gegen Lauf**: `spez_Waermeverbrauch` (kWh/m²a) wird vom Lauf nur zu **48–88 %** erreicht (10576: 54,1 vs. 112,5; 10614: 156,1 vs. 212; 10643: 394,9 vs. 451). `Waermebedarf` (kW) trifft bei 10632 gut (134,7 zurueckgerechnet vs. 137), bei 10599 nicht (35,95 vs. 47).

---

### C) Klimadaten je Region, Einheiten, Ortszeitregel

**13 Regionen, nur ZWEI Orte** (jedes Projekt haelt seine eigene Kopie):

| Region | Projekt | Bezeichner | Lon | Lat | Klimazone_DIN4710 |
|---|---|---|---|---|---|
|1007001, 1008001, 1017001, 1020033, 1020034, 1020049, 1020050, 1020051, 1020052, 1020055, 1020056|1007, 1008, 1017, 1023, 1024, 1039, 1040, 1041, 1042, 1045, 1046|stuttgart|9,1800132|48,7784485|NULL (7×) / 0 (6×)|
|1018047, 1020040|1018, 1030|München|11,5753822|48,1371079|NULL / 0|

`Details` = „ERA5 - PVGIS-SARAH3: …". **`Klimazone_DIN4710` ist nirgends belegt** (NULL oder 0) — eine DIN-4710-Zuordnung existiert in der Testdatenbank nicht. `Tab_Klimaregion` hat keine Spalte `Name`, sondern `Bezeichner` (sql/schema/001_grundschema.sql:1392-1401).

**`Tab_Solar` — 8 760 Zeilen je Region, kein NULL in keiner Spalte.** Einheiten aus EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:59-77 (PVGIS-Feldnamen) und :63/69/72/75:

| Spalte | PVGIS-Feld | Einheit | Stuttgart min/max/Mittel/Jahressumme | München min/max/Mittel/Summe |
|---|---|---|---|---|
|Temperatur|`T2m`|°C|−18,17 / 33,53 / **9,875** / 86 502,5|−9,26 / 31,46 / **9,575** / 83 874,2|
|Globalstrahlung|`G(h)` GHI|W/m²|0 / 942,0 / 139,364 / **1 220 829 Wh/m² = 1 220,8 kWh/m²a**|0 / 955,0 / 137,200 / 1 201 872|
|Direktstrahlung|`Gb(n)` **DNI, normal**|W/m²|0 / 959,22 / 137,344 / 1 203 130|0 / 990,22 / 133,401 / 1 168 593|
|Diffusstrahlung|`Gd(h)` DHI|W/m²|0 / 448,0 / 65,567 / 574 363|0 / 451,0 / 65,955 / 577 770|
|Sonnenwinkel|Rueckgabe `SolarCalculator.sonnenwinkel`|Grad (Hoehenwinkel)|0 / 64,20 / 13,286|0 / 65,10 / 13,446|
|Sol_Nord|gerechnet|W/m²|0 / 285,70 / 49,265 / 431 557|0 / 284,40 / 49,103 / 430 142|
|Sol_Ost|gerechnet|W/m²|0 / 798,997 / 88,684 / 776 870|0 / 761,547 / 86,918 / 761 404|
|Sol_Sued|gerechnet|W/m²|0 / 894,306 / 106,734 / 934 991|0 / 921,486 / 104,969 / 919 529|
|Sol_West|gerechnet|W/m²|0 / 742,363 / 81,137 / 710 762|0 / 727,967 / 81,013 / 709 677|

**Ja — `Sol_*` sind Stundenwerte je senkrechter Fassade in W/m².** Beleg: `KlimaImportAblauf.NEIGUNG_FASSADE = 90` und `AZ_SUED=0, AZ_OST=-90, AZ_NORD=180, AZ_WEST=90` (EPOS.Kern/Allgemein/Import/KlimaImportAblauf.cs:132-136), gerechnet in `Rechnen` (:305-338) je Stunde aus derselben GHI/DNI/DHI-Zeile mit `SolarCalculator.CalculateHourly` (isotropes Transpositionsmodell, Albedo 0,2, `SolarPVGISCalculator.cs:311`). Ost und West sind **stuendlich getrennt** gespeichert (Jahresmittel 88,68 vs. 81,14 W/m², 9 % Unterschied).

**Ortszeitregel.** `Tab_Solar` hat KEINE Zeitspalte; der einzige Zeitbezug ist `ORDER BY ID`, und das ist `time(UTC)` von PVGIS (EPOS.Kern/Allgemein/Simulation/SolarZeitbasis.cs, Kopf). Die Korrektur sitzt beim LESEN: `SolardatenCtrl.ReadOrtszeit` (EPOS.Kern/Controller/SolardatenCtrl.cs:156-208) liest `ORDER BY ID`, prueft auf 8 760 Zeilen und sortiert ueber `SolarZeitbasis.Zuordnung(referenzjahr)` um: `ortszeit[L] = utc[L − Offset(L)]`, Offset = 1 h (MEZ) bzw. 2 h (MESZ), EU-Regel letzter Maerzsonntag 02:00 bis letzter Oktobersonntag 03:00 Ortszeit, Jahresumlauf fuer L < Offset. Es werden **ganze Zeilen** verschoben, nie einzelne Spalten. Die beiden Umstellstunden fallen doppelt bzw. weg (Nachtstunden, Strahlung = 0). Bei ≠ 8 760 Zeilen bleibt die Umrechnung AUS und es gibt eine Warnung (:183-191).

*Gegenprobe aus den Daten:* Die Speicherstunde mit der groessten Jahressumme Globalstrahlung ist **11** (Stuttgart 153 756, München 155 992) — bei 9,18 °O liegt der wahre Mittag ~11:15 UTC. Die Ablage ist also tatsaechlich UTC. Die abgelegten `klima_<id>.json` stehen deshalb wie gefordert **in Speicherreihenfolge = UTC**; die Ortszeitregel muss der Prototyp beim Lesen selbst anwenden.

**`Tab_Klimadaten` — 365 Zeilen je Region.** Die Werte sind **24-Stunden-MITTEL** der Stundenreihe (`SolarCalculator.GetDailyAverages`, EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:483-503, `group.Average(...)`), Ausnahme `Sonnenwinkel` = **Tagesmaximum** (`group.Max`, :501). Numerisch bestaetigt: Tagesmittel Temperatur = 9,875 °C = Stundenmittel; Tageswerte Stuttgart Temperatur −10,949 … 26,560 °C, Sol_Sued 0 … 249,932 W/m², Sonnenwinkel 17,6 … 64,2 Grad (Mittel 40,92 statt 13,29 der Stundenreihe).

- **WE** — 0/1, Sa oder So (`KlimaImportAblauf.cs:356`). **104 Tage** mit WE=1 in allen 13 Regionen.
- **TagTyp_W** — 1 oder 2: **2, wenn Diffusanteil > 0,5 × Globalstrahlung** des Tages („trueber Tag"), sonst 1 (`KlimaImportAblauf.cs:355`). Stuttgart 148×1 / 217×2, München 136×1 / 229×2.
- **TagTyp_NW** — 1..8 aus Quartal × Werktag/Wochenende (`Jahreszeitwert`, `KlimaImportAblauf.cs:360-374`): Q1 Wt=1/WE=2, Q2=3/4, Q3=5/6, Q4=7/8. Verteilung identisch in allen Regionen: 64/26/65/26/66/26/66/26.
- **Verwendung**: `Typ == "Wohngebaeude  VDI 2067"` ⇒ Profilkachelung mit `TagTyp_W`, sonst mit `TagTyp_NW` (EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:601-608).
- **Achtung**: Die Tageswerte werden mit `SELECT * FROM Tab_Klimadaten WHERE ID_Klimaregion=… ORDER BY ID` gelesen, **ohne** Ortszeitkorrektur (EPOS.Kern/Controller/KlimadatenCtrl.cs:37). Tageskalender = UTC-Tage, Stundenreihe = Ortszeit — zwei Zeitbasen in einem Lauf.

---

### D) Einheiten- und Semantiktabelle mit Codebeleg

| Feld | Einheit / Bedeutung | Beleg |
|---|---|---|
|`Interne_Waermegewinne`|**W** (Leistung des GANZEN Katalogbaus, nicht je m², nicht Wh/Tag)|`pHzg = (tPrev−tAussen)*L + (tSoll−tPrev)*C − innereGewinne`; L ist W/K ⇒ innere in W. BhkwPlan.cs:419|
|`Bauweise`|**Wh/K** (wirksame Waermekapazitaet des Gebaeudes). Spez. 20 / 50 / 100 Wh/(m²K) fuer leicht / schwer / sehr schwer|`a = 1 − exp(−L/C)` mit L [W/K] und Stundenschritt ⇒ C [Wh/K], BhkwPlan.cs:428; `Bauweise = Wohnflaeche × 20/50/100`, Gebaeudebauweise.cs:24-70|
|`Luftwechselrate`|**1/h**|`f·(Wohnflaeche·Raumhoehe)·1,2·LWR·0,2778` = V[m³]·rho[kg/m³]·n[1/h]·c[Wh/kgK] ⇒ W/K. BhkwPlan.cs:325-330|
|`Fensterdurchlassgrad`|**g-Wert 0..1**, roher Faktor (kein Prozent, kein F_S/F_F/F_W)|`s = s * transmissionsgrad * 100.0`, BhkwPlan.cs:316; Daten 0,59..0,75|
|`Ferienbeginn_n` / `Ferienende_n`|**Tag des Jahres 1..365** (Feldindex), kein Datum. 0 = aus, 366 = aus|`for (int Tag = (int)item.Ferienbeginn_1; Tag < 365; …)` und Pruefung `> 0 && <= 365`, SimulationWaermebedarf.cs:701-731|
|`Ferien`|Flag, wirksam erst `> 0.9`; zusaetzlich `Raumsolltemperatur_Ferien < 1` ⇒ Ferien = 0|SimulationWaermebedarf.cs:689-699|
|`Wochenende`|Flag; die WE-Absenkung greift nur, wenn `Raumsolltemperatur_Wochenende > 5`|SimulationWaermebedarf.cs:777-780|
|`k_Wert_*`|**W/(m²K)**, mit fest verdrahteten Gewichten 0,83 (Wand) / 1,0 (Fenster) / 0,95 (Dach) / 0,45 (Grundflaeche) / 1,0 (Sonstiges)|BhkwPlan.cs:323|
|`Flaeche_*`, `Dachflaeche`, `Grundflaeche`, `gesamte_Fensterflaeche`, `Fensterflaeche_*`|**m²**|ebd.|
|`WBVK_*`|**W/(m·K)** (psi), `Abmessung_*` in **m**, Summe × 0,83|BhkwPlan.cs:324|
|`Raumhoehe`|**m** (geht nur ins Lueftungsvolumen)|BhkwPlan.cs:325|
|`Wohnflaeche`|**m²** — Bezugsflaeche des Katalogsatzes, **Nenner** des Skalierungsbruchs|`return acc * gesamtflaeche / wohnflaeche`, BhkwPlan.cs:434|
|`Wohnflaeche_gesamt`|**m²** — Gesamtflaeche des Katalogbaus; Basis fuer Bewohner und fuer den Verbrauchsweg|SimulationWaermebedarf.cs:641, 645-648|
|`Flaeche_Nutzer`|**m²/Person**|`item.Bewohner = Z_AuswahlWohnflaeche / Flaeche_Nutzer`, SimulationWaermebedarf.cs:571|
|`Raumsolltemperatur_*`, `Maximaleraumtemperatur`|**°C**|BhkwPlan.cs:403-407, 432|
|`WW_Bedarf`|**kWh/a** (Katalogwert; die Referenzlaeufe speisen das Brauchwasser aber aus dem eigenen Brauchwasserkanal, nicht daraus)|AbweichungsErmittler.cs:128 `("Gebäude","Tab_Gebaeude","WW_Bedarf","Warmwasserbedarf","kWh/a",0)`|
|`spez_Waermeverbrauch`|**kWh/(m²a)**, Katalogkennzahl (nicht Rechnungseingang)|—|
|`Waermebedarf`|**kW**, Auslegungsheizlast (Katalog). Gegenprobe 10632: 34,095 kW / 0,2531 = 134,7 kW vs. Feld 137|—|
|`Baualtersklasse` / `Gebaeudeart` / `Wohngebaeude_Nicht_Wohngebaeude` / `Typ`|Text, Werteliste siehe A)|—|
|`Tab_Solar.Sol_*`|**W/m²** auf senkrechter Fassade|KlimaImportAblauf.cs:132-136, 305-338|
|`Tab_Klimadaten.Sol_*`, `.Temperatur`, `.Global/Direkt/Diffus`|**24-h-Mittel** derselben Einheiten|SolarPVGISCalculator.cs:489-500|
|`Tab_Klimadaten.Sonnenwinkel`|**Grad, Tagesmaximum**|SolarPVGISCalculator.cs:501|

**Eine Besonderheit, die fuer 7R2C zentral ist:** Die solaren Gewinne gehen nur in den **Stunden 9..14** ein, dort aber mit dem **Faktor 4,0** (BhkwPlan.cs:420 und :431). Da `Sol_*` in `Tab_Klimadaten` das 24-h-MITTEL ist, wird die Tagesenergie damit exakt erhalten (6 h × 4 = 24 h) — aber als Rechteckblock. Die Stundenaufloesung der Einstrahlung liegt in `Tab_Solar` bereit und wird vom Gebaeudeweg **nicht benutzt**.

---

### E) Skalierung je Projekt

**Regel** (SimulationWaermebedarf.cs:568-578, :613-656 und BhkwPlan.cs:434):
1. `Einheit_Waermebedarf_Wohnflaeche == "Wohnfläche [m²]"` ⇒ wirksame Flaeche = `Z_ProjektGebaeude.Wohnflaeche_Waermebedarf`; `Bewohner = wirksame Flaeche / Flaeche_Nutzer` (:571).
2. Sonst (`Bewohner_und_Flaeche_berechnen`, :613-656): Verbrauch → kWh/a (Oel × JNG × 10,08; Gas m³ × JNG × 11,48; Gas MWh(Ho) × JNG / 1,1 × 1000; Brennstoff MWh × JNG × 1000; Verbrauch MWh × 1000), dann Katalogbau EINMAL bei `Wohnflaeche_gesamt` durchrechnen (`VerbrauchAlt = HeizwaermebedarfGeb[index]/1000`, :650) und **fiktive Flaeche** `FlaecheNeu = VerbrauchNeu / VerbrauchAlt × Wohnflaeche_gesamt` (:651).
3. Wirksam wird der Faktor erst im Rueckgabewert: `acc × gesamtflaeche / wohnflaeche` = `Z_AuswahlWohnflaeche / Tab_Gebaeude.Wohnflaeche` (BhkwPlan.cs:434). **Rein linear und rein geometrisch** — L, C, Fensterflaechen, innere Gewinne, U-Werte bleiben die des Katalogbaus.

**Alle 15 Zeilen der 13 Projekte benutzen Weg 1** („Wohnfläche [m²]"); der Verbrauchsweg kommt in diesen Projekten nicht vor.

| Projekt | Geb-ID | Wohnflaeche (Nenner) | Wohnflaeche_Waermebedarf (Zaehler) | **Faktor** | Typ ⇒ Tagesprofil |
|---|---|---|---|---|---|
|1007|10614|74,0|340,0|**4,5946**|„Wohngebaeude  VDI 2067" ⇒ TagTyp_W, 120 Werte|
|1008|10576|304,0|800,0|**2,6316**|„Wohngebaeude  VDI 2067", 120|
|1008|10577|74,0|74,0|1,0000|„Wohngebaeude  VDI 2067", 120|
|1017|10599|744,4|744,0|0,9995|**„Wohnblock" ⇒ TagTyp_NW, 192**|
|1018|10632|1 975,3|500,0|**0,2531**|**„Hotel" ⇒ TagTyp_NW, 192**|
|1023|10628|3 596,0|3 596,0|1,0000|„Wohnblock", 192|
|1024|10629|3 596,0|3 596,0|1,0000|„Wohnblock", 192|
|1030|—|—|—|— (kein Gebaeude)|—|
|1039|10642|130,0|130,0|1,0000|„Wohngebaeude  VDI 2067", 120|
|1039|10643|201,0|201,0|1,0000|„Wohngebaeude  VDI 2067", 120|
|1039|10644|3 596,0|3 596,0|1,0000|„Wohnblock", 192|
|1040/1041/1042/1045|10645/46/47/51|201,0|201,0|1,0000|„Wohngebaeude  VDI 2067", 120|
|1046|10652|74,0|340,0|**4,5946**|„Wohngebaeude  VDI 2067", 120|

Zu **jedem** der 15 Gebaeude existiert die Tagesverteilung in `Abfrage_Tagverteilung` (Bezeichner = `Typ`, ID = Gebaeude-ID; SimulationWaermebedarf.cs:670): 120 Zeilen fuer „Wohngebaeude  VDI 2067", 192 fuer „Wohnblock" und „Hotel". Kein Abbruchfall.

---

### F) Referenzwerte je Projekt

`waermebedarf_gebaeude.csv` fuehrt 8 760 Zeilen `Index;Wert`. **Einheit geprueft: WATT je Stunde, also Wh** — die Summe / 1000 trifft `Energiebedarf.Waermebedarf_Heizung` [MWh] exakt (1007: 53 071 741 W-Stunden = 53 071,74 kWh vs. Aggregat 53,07). `waermebedarf.csv` steht dagegen in **kW**.

| Projekt | wirks. Flaeche m² | **Jahresheizwaerme kWh** (Gebaeudereihe) | **kWh/m²a** | **Max kW** | Stunde (Tag/Uhrzeit) | Waermebedarf_Gesamt MWh | Waermelast_Max kW | Heizung / Brauchwasser / Prozess MWh |
|---|---|---|---|---|---|---|---|---|
|1007|340,0|53 071,74|**156,1**|34,991|451 (19. Jan, 19 h)|57,13|36,41|53,07 / 4,06 / 0|
|1008|874,0 (800+74)|54 817,81|**62,7**|37,821|1 363 (57. Tag, 19 h)|54,82|37,82|54,82 / 0 / 0|
|1017|744,0|62 964,71|**84,6**|35,952|426 (18. Jan, 18 h)|62,96|35,95|62,96 / 0 / 0|
|1018|500,0|46 881,44|**93,8**|34,095|8 371 (349. Tag, 19 h)|46,88|34,10|46,88 / 0 / 0|
|1023|3 596,0|329 796,53|**91,7**|193,961|8 357 (349. Tag, 5 h)|389,80|204,08|329,80 / 60 / 0|
|1024|3 596,0|329 796,53|**91,7**|193,961|8 357|389,80|204,08|329,80 / 60 / 0|
|1030|— (kein Gebaeude)|**0,00**|—|0,000|—|6 137,56|2 206,00|6 137,56 / 0 / 0 (aus Ganglinie)|
|1039|3 927,0 (130+201+3 596)|445 619,74|**113,5**|253,020|426 (18. Jan, 18 h)|466,62|257,71|445,62 / 21 / 0|
|1040|201,0|59 354,12|**295,3**|37,290|1 387 (58. Tag, 19 h)|64,35|38,61|59,35 / 5 / 0|
|1041|201,0|59 354,12|**295,3**|37,290|1 387|159,78|78,53|124,78 / 5 / 30|
|1042|201,0|59 354,12|**295,3**|37,290|1 387|89,35|45,38|59,35 / 30 / 0|
|1045|201,0|59 354,12|**295,3**|37,290|1 387|64,35|38,61|59,35 / 5 / 0|
|1046|340,0|53 071,74|**156,1**|34,991|451|57,13|36,41|53,07 / 4,06 / 0|

Spaltennamen in `aggregate.csv` (Format `Groesse;Wert`, BOM, Semikolon): `Energiebedarf.Waermebedarf_Gesamt`, `Energiebedarf.Waermelast_Max`, `Energiebedarf.Waermebedarf_Heizung`, `Energiebedarf.Waermebedarf_Brauchwasser`, `Energiebedarf.Waermebedarf_Prozess`, `Energiebedarf.Strombedarf_Gesamt`, `Energiebedarf.Strombedarf_Max`, `Energiebedarf.Waermerestbedarf`, `Ergebnis.ID_Klimaregion`, `Ergebnis.Bezeichner` u. a. **Waermebedarf_* stehen in MWh, Waermelast_Max in kW.**

Anmerkung: 1041/1042 haben abweichende Gesamtwerte, weil Prozess- bzw. Brauchwasserprofile dazukommen; die Gebaeudereihe selbst ist in 1040/1041/1042/1045 identisch.

---

### G) Bewertung: reicht der Bestand fuer VDI 6007-1 (7R2C)?

**Klimadaten: ja, vollstaendig ausreichend.** 8 760 h Aussentemperatur (°C), GHI, DNI, DHI (W/m²), Sonnenhoehenwinkel (Grad) und vier senkrechte Fassadenwerte N/O/S/W (W/m²), lueckenlos, ohne NULL, mit dokumentierter UTC-Ablage und einer eindeutigen UTC→MEZ/MESZ-Regel.

**Gebaeudedaten: NICHT ausreichend.** Sie reichen fuer das heutige Ein-Kapazitaeten-RC (1R1C), nicht fuer 7R2C. Es fehlen:

1. **Fenster Ost und West getrennt.** Die DB fuehrt nur `Fensterflaeche_Ost_West` als EINEN Summenwert, und `SolareGewinneC` mittelt `(Eo + Ew) × 0,5` gegen diese eine Flaeche (BhkwPlan.cs:301-316, dort auch ausdruecklich vermerkt). Die stuendlichen `Sol_Ost` / `Sol_West` liegen getrennt vor (Jahresmittel 88,68 vs. 81,14 W/m²) — der Gebaeudesatz kann sie nicht aufnehmen. **Ohne Ost/West-Trennung sind Morgen- und Abendspitze nicht darstellbar**; das ist der wichtigste Datenmangel.
2. **Zweite Kapazitaet (Innenbauteile).** 7R2C braucht C_AW und C_IW getrennt, dazu R1_AW/R1_IW/R_rest. Die DB hat **eine einzige** Zahl `Bauweise` [Wh/K] und keinerlei Innenbauteilflaechen (Innenwaende, Decken, Estrich).
3. **Schichtaufbau.** VDI 6007-1 leitet R1 und C1 aus der Schichtfolge (d, lambda, rho, c) ueber die periodische Antwort ab. Vorhanden sind nur U-Werte. `Bauweise` ist eine **Dreipunktwahl** 20/50/100 Wh/(m²K) (Gebaeudebauweise.cs:24-32), keine gemessene Kapazitaet.
4. **Trennung Luftknoten / Oberflaechenknoten.** Der heutige Kern fuehrt EINE Raumtemperatur (BhkwPlan.cs:429-431). 7R2C braucht theta_L und theta_S getrennt sowie die Aufteilung des Waermeuebergangs in konvektiv (h_c ≈ 2,7) und langwellig strahlend (h_r ≈ 5 W/m²K). Nichts davon steht in der DB.
5. **Verschattung, Rahmen, Einfallswinkel.** Kein F_S (Verschattung/Horizont/Ueberstand), kein F_F (Rahmenanteil), kein F_W (Einfallswinkelkorrektur), kein Sonnenschutz. `Fensterdurchlassgrad` wird roh als Faktor benutzt.
6. **Solarer Eintrag auf opake Bauteile.** Es gibt keinen Absorptionsgrad alpha der Aussenflaechen und keinen langwelligen Himmelsaustausch (theta_sky / Delta E_r) — die Aussenwand bekommt im Bestand ueberhaupt keine Strahlungslast.
7. **Erdreich-/Kellerrandbedingung.** Die Grundflaeche wird mit dem Pauschalfaktor 0,45 gegen Aussenluft gerechnet (BhkwPlan.cs:323). VDI 6007 braucht eine eigene Randtemperatur.
8. **Wind und Feuchte.** `WS10m` und `RH` kommen von PVGIS an (SolarPVGISCalculator.cs:65, 77), werden aber **nicht** in `Tab_Solar` abgelegt — kein windabhaengiger Uebergang, keine latenten Lasten.
9. **Zonierung und Leistungsbegrenzung.** Ein Gebaeude = eine Zone, keine Raumtabelle; die Heizleistung ist unbegrenzt (`pHzg` wird nur nach unten auf 0 geklemmt).
10. **Fensterneigung.** Alle Fassaden fest auf 90 Grad (KlimaImportAblauf.cs:133) — keine Dachfenster, keine geneigten Flaechen.

**Fragwuerdig am Bestand:**

- **Zeile 10576 (Projekt 1008) hat `Bauweise = 50 Wh/K` bei 304 m²** = 0,16 Wh/(m²K) statt 50. Das ist der dokumentierte Rueckfallwert aus `Gebaeudebauweise.cs:69`. Fuer 7R2C ist dieser Datensatz zu korrigieren, bevor er benutzt wird.
- **Der Skalierungsfaktor ist rein geometrisch und reicht bis 4,59** (1007/1046: 114 m² Aussenwand und 16,26 m² Fenster werden auf 340 m² Wohnflaeche gedehnt) bzw. bis 0,25 (1018). Fuer ein 1R1C ist das ein Multiplikator; fuer 7R2C, wo Flaechen die Kapazitaet und den Strahlungsaustausch tragen, ist es physikalisch falsch. Der Prototyp muss entscheiden: echte Huelle modellieren oder die Geometrie mitskalieren.
- **Zwei Zeitbasen im selben Lauf**: Stundenreihe ortszeitkorrigiert (SolardatenCtrl.cs:156-208), Tageskalender roh in UTC (KlimadatenCtrl.cs:37).
- **Die Einstrahlung erreicht das Gebaeude nur als Tagesmittel im 6-Stunden-Rechteck** (Stunden 9..14, Faktor 4,0). Ein 7R2C mit Stundenschritt braucht `Tab_Solar` direkt — die Daten liegen bereit, der Weg dorthin fehlt.
- **`Sol_Nord` = 431,6 kWh/(m²a)** (Stuttgart) ist fuer eine senkrechte Nordfassade **zu hoch** (Messwerte in Deutschland ~300–350). Ursache: isotropes Diffusmodell plus 0,2 Bodenalbedo ohne Horizontaufhellungsdefizit (SolarPVGISCalculator.cs:311, 476-480). Auch `Sol_Sued` = 935 kWh/(m²a) liegt am oberen Rand. Fuer VDI 6007 sollte das anisotrope Hay-Davies-Modell (bereits vorhanden, `CalculateHourlyHayDavies`, :421) benutzt werden.
- **`Klimazone_DIN4710` ist in allen 13 Regionen leer** (NULL oder 0) — ein DIN-4710-Testreferenzjahr laesst sich aus der DB nicht auswaehlen.
- **WE- und Ferienabsenkung sind in allen 15 Zeilen inaktiv** (Sollwerte 0, Flags 0, `Ferienbeginn_1 = 366`). Zur Validierung eines Nutzungsprofils ist damit nichts in den Daten.
- **Projekt 1030 hat kein Gebaeude** — als Referenz fuer ein Gebaeudemodell ungeeignet, es prueft nur den Ganglinienweg.
- **Nur zwei Klimaorte** (Stuttgart 11×, München 2×) und **effektiv nur 7 verschiedene Katalogbauten** in 15 Zeilen. Die Bandbreite fuer eine Validierung ist schmal.

**Fazit:** Ein 7R2C-Prototyp laesst sich aus dem Bestand **starten**, aber jede der folgenden Groessen ist eine Annahme, kein Datum: Aufteilung `Bauweise` → C_AW/C_IW, R1 aus U-Wert und typischem Aufbau je Baualtersklasse, Innenbauteilflaechen, Aufteilung Ost/West aus `Fensterflaeche_Ost_West`, h_c/h_r, alpha, F_S/F_F/F_W. Belastbar validierbar ist der Prototyp nur gegen die hier abgelegte Referenz (Abschnitt F), und die stammt selbst aus dem 1R1C-Modell — eine Uebereinstimmung waere also kein Guetebeweis, sondern nur ein Nachweis gleicher Jahresenergie.

---

### H) Abgelegte Dateien

Alle in `C:\Users\Dirk\AppData\Local\Temp\epos-spike\daten\` (66 Dateien, 21 MB):

- **Gebaeude, 13 Dateien** (alle Spalten von `Abfrage_Projektgebaeude` inkl. Z_ProjektGebaeude-Feldern, dazu `ID_Klimaregion`, `Wirksame_Flaeche_m2`, `Skalierungsfaktor`, `Skalierungsweg`, `Heizwaerme_nachgerechnet_kWh`):
 `gebaeude_1007.json` (1 Objekt), `gebaeude_1008.json` (2), `gebaeude_1017.json` (1), `gebaeude_1018.json` (1), `gebaeude_1023.json` (1), `gebaeude_1024.json` (1), `gebaeude_1030.json` (**0 — leeres Feld**), `gebaeude_1039.json` (3), `gebaeude_1040.json` (1), `gebaeude_1041.json` (1), `gebaeude_1042.json` (1), `gebaeude_1045.json` (1), `gebaeude_1046.json` (1).
- **Klima stuendlich, 13 Dateien je 8 760 Objekte** in Speicherreihenfolge (= UTC), Felder `Temperatur, Globalstrahlung, Direktstrahlung, Diffusstrahlung, Sonnenwinkel, Sol_Nord, Sol_Ost, Sol_Sued, Sol_West`:
 `klima_1007001.json`, `klima_1008001.json`, `klima_1017001.json`, `klima_1018047.json`, `klima_1020033.json`, `klima_1020034.json`, `klima_1020040.json`, `klima_1020049.json`, `klima_1020050.json`, `klima_1020051.json`, `klima_1020052.json`, `klima_1020055.json`, `klima_1020056.json`.
- **Klima taeglich, 13 Dateien je 365 Objekte** (`Temperatur, WE, TagTyp_W, TagTyp_NW`):
 `klimatage_1007001.json`, `klimatage_1008001.json`, `klimatage_1017001.json`, `klimatage_1018047.json`, `klimatage_1020033.json`, `klimatage_1020034.json`, `klimatage_1020040.json`, `klimatage_1020049.json`, `klimatage_1020050.json`, `klimatage_1020051.json`, `klimatage_1020052.json`, `klimatage_1020055.json`, `klimatage_1020056.json`.
- **Referenz, 13 JSON** (Jahressumme kWh, Maximum kW, Stunde des Maximums, Gesamt-/Brauchwasser-/Prozesssummen, vollstaendiger `aggregate`-Block):
 `referenz_1007.json` … `referenz_1046.json` (1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046).
- **Referenz-Stundenreihe, 13 CSV** (unveraenderte Kopie von `waermebedarf_gebaeude.csv`, 8 761 Zeilen, `Index;Wert`, Werte in W):
 `referenz_1007_gebaeude.csv` … `referenz_1046_gebaeude.csv`.
- **`probe_log.txt`** — Rohprotokoll der Probe (PROJ / REGION / SOLSTAT / TAGSTAT / TAGWE / GEB / GEBD / GEBTV / TEXT).

Die Testdatenbank wurde ausschliesslich mit `Mode=ReadOnly` geoeffnet; im Repository wurde keine Datei angelegt oder geaendert.