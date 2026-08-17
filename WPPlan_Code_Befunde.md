# Code-Prüfung EPO-Plan (WP-Plan) gegen das Stromspeicher-Konzept Rev. 2

Stand: 2026-08-16 · Geprüfte Quelle: `/mnt/user-data/uploads/WP_Plan/` (Teilbestand: `WindowsFormsApplication1/`
mit `CLAUDE.md`, `Program.cs`, `MDIMainForm.cs`, `.csproj`, `Allgemein/`, `Controller/`, `Model/`,
`Views/{Stromspeicher,Stromverbraucher,Kosten,Simulation,Hauptformular}` sowie `../migration.manuell.sql`).

> **Geltungsbereich / Lücken im Prüfmaterial.** Nicht im Upload enthalten und deshalb nur indirekt (über
> Aufrufstellen) rekonstruierbar: `Allgemein/Simulation/*` (`SimulationControl`, `SimulationStrombedarf`,
> `SimulationPV`, `SimulationBHKW`, `SimulationSSP`, `SimulationWaermebedarf`, `SimulationWaermepumpe`,
> `SimulationSPK`, `SimulationSolarthermie`), `Allgemein/GrafikTools/ChartManager.cs`,
> `Allgemein/Export/CsvExportClass.cs`, `Allgemein/Import/CsvReader.cs`, `WErzeugerModel/-Ctrl`,
> `WizardCtrl`, `WizardItemClass`, `Views/Wizard/*`, sämtliche `*.Designer.cs`. Alle Aussagen zu diesen
> Klassen sind aus verifizierten Aufrufstellen abgeleitet und im Text als solche gekennzeichnet
> („aus Aufrufstelle"). Signaturen/Feldnamen stimmen, Einheiten teils nur erschlossen.

---

## 1. Stromprofil — welche Optionen zur Lastgang-Erzeugung existieren wirklich?

**Die Annahme „drei Optionen" ist so nicht belegbar.** Belegt sind **zwei** eigenständige Quellen für den
Strom-Lastgang plus die intern gerechneten Anlagen-Eigenverbräuche, die im Strom-Chart aufaddiert werden.

### (a) Stromverbraucher + Stromverbrauchertyp (synthetisches Profil)

* Kopf: `Tab_Stromverbraucher_STAMM` / `Tab_Stromverbraucher` — Felder `ID, Bezeichner, Typ, Beschreibung,
  Monat_1 … Monat_12, ReadOnly` (`migration.manuell.sql:113-116`; `StromverbraucherModel.cs`,
  `StromverbraucherStammCtrl.cs:18-45`). `Monat_i` = Monatsverbrauch (kWh).
* Profil: `Tab_Stromverbrauchertyp_STAMM` / `Tab_Stromverbrauchertyp` — `ID, Typname, Beschreibung` plus
  **168 numerische Spalten `[1] … [168]`** = 7 Tage × 24 Stunden (`migration.manuell.sql:118-121`).
  Bearbeitung in `Views/Stromverbraucher/Form_EingStromTyp.cs` (`arr[7,24]`, `arr_seriell[168]`,
  Spaltenindex `Tag*24 + stunde + 3`, Zeile 97-116; Speichern feldweise per
  `UPDATE … SET [<n>] = ?`, Zeile 188-211).
* Verknüpfung Kopf→Profil **namensbasiert** (`Tab_Stromverbraucher.Typ = Tab_Stromverbrauchertyp.Typname`,
  `StromverbraucherStammCtrl.cs:122-128`).
* Projektzuordnung: `Z_Projekt_Stromverbraucher (ID, ID_Projekt, ID_Stromverbraucher, Bezeichner, Summe)` —
  `Summe` ist ein projektspezifischer Jahresverbrauchs-Override
  (`Z_ProjektStromverbraucherCtrl.UpdateSumme`, `Form_Stromverbraucher.cs:214-218`).
* Woche→Jahr-Expansion: **`WPPlan.Core.BhkwPlan.StromWocheToJahr(wo[168], monatsverbrauch[12], outJahr[8760],
  moAnfang[12], moEnde[12])`** in `Allgemein/BhkwPlan.cs:175-196`. Phase 1: `out[0..23] = wo[144..167]`
  (Sonntag zuerst, Kalenderausrichtung 1. Januar) + 52 × 168 h → **8760**. Phase 2: monatsweise Normierung
  `out[h] = out[h]/summe · monatsverbrauch[m] · 1000` — d. h. **die Ausgabe ist in Wh je Stunde**
  (kWh → Wh). Passend dazu `VectorSumme` (× 0,001) und `WattToKw` in derselben Klasse.

### (b) Stromganglinie (importierte Messreihe)

* Kopf `Tab_Stromganglinie_STAMM` / `Tab_Stromganglinie`: `ID, [ID_Projekt,] Bezeichner, Zeitinterval[, ReadOnly]`.
* Daten `Tab_StromganglinieDaten_STAMM` / `Tab_StromganglinieDaten`: `ID, ID_Ganglinie, Wert[, ReadOnly]` —
  **eine Zeile je Intervall**, Reihenfolge = ID-Reihenfolge, **kein Zeitstempelfeld**
  (`StromganglinieStammCtrl.cs:206-213`: `SELECT Wert … ORDER BY ID`).
* `Zeitinterval` = Werte je Stunde: **1 = Stundenwerte, 4 = Viertelstundenwerte, 60 = Minutenwerte**
  (`Form_Stromganglinie_Admin.cs:122-124`).
* Projektzuordnung `Z_ProjektStromganglinie (ID/ID_Z, ID_Projekt, ID_Ganglinie, Bezeichner)`;
  Kopie STAMM→Projekt inkl. aller Datenzeilen über
  `StromganglinieStammCtrl.ApplyGanglinieToProjekt()` / `CopyGanglinieToProjekt()` (Zeile 145-231).

### (c) Tagesverteilung `TagV` — gehört **nicht** zum Strompfad

`TagVModel/TagVDatenModel/TagVCtrl` bedienen `Tab_DBTagV(_STAMM)` (`ID, Bezeichner/Name, Beschreibung,
Veraenderbar, ReadOnly`) und `Tab_DBTagVDaten(_STAMM)` (`ID, ID_TagV, Verteilung`). Die Verknüpfung läuft
über den **Gebäudetyp** (`migration.manuell.sql:472-484`: `Tab_Gebaeude.Typ = Tab_DBTagV.Name`), die
Verteilung wird per `BhkwPlan.StdWerte(waermebedarf[8760], tagTyp[365], tagesgang[typ*24], tageslast[365])`
auf den **Wärmebedarf** angewandt (`BhkwPlan.cs:215-245`). Für den Strom-Lastgang irrelevant.
**Ein SLP-Verfahren (VDI 4655 / BDEW-Standardlastprofile) ist im geprüften Bestand nirgends vorhanden.**

### (d) Anlagen-Eigenverbrauch (dritter Summand im Strom-Chart)

`Views/Simulation/NavigatorStrom.cs:96-103`:
```
temp_profil = sim.simulation_Strombedarf.Strombedarf_viertelStundenwerte;   // 35040
temp_wp     = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.WP_Strombedarf_stuendlich);
temp_hs     = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.Heizstab_stuendlich);
temp_hk     = sim.Stundenwerte_zu_viertelstunden(sim.simulation_spk.Strombedarf_stuendlich);
temp_bhkw   = sim.Stundenwerte_zu_viertelstunden(sim.simulation_bhkw.stromproduktion);
temp_ges    = new float[8760 * 4];   // = 35040
for (…) temp_ges[i] = temp_wp[i] + temp_hs[i] + temp_hk[i] + temp_profil[i];
```

### Auflösung, Klasse, Einheit — die belastbare Antwort

| Frage | Befund |
|---|---|
| Wer liefert den Jahres-Lastgang? | `SimulationStrombedarf` (Datei nicht im Upload). Einstieg: `Stromprofil_Strombedarf_berechnen(List<string> verbraucherNamen)` → `float[]` (`Form_Stromverbraucher.cs:223`, `StromTestClass.cs:19-29`) |
| Auflösung | **beides**: `prozesswerte[0..8759]` stündlich (`StromTestClass.MyTestProfil`), `Strombedarf_viertelStundenwerte` mit **35 040** Werten (`NavigatorStrom.cs:96,101,113`) |
| Datentyp | `float[]` durchgängig (nicht `double[]`) |
| Einheit | Leistungsreihen in **kW** (Chart: `YAxisTitle="Leistung"`, `toolTipUnit="kW"`); Zwischenreihen aus `BhkwPlan.StromWocheToJahr` in **Wh/h**; Aggregate in MWh (`ErgebnisModel`-Kommentare) |
| Umrechner | `SimulationControl.Stundenwerte_zu_viertelstunden(float[8760]) → float[35040]` |
| Weitere Felder | `Strombedarf_gesamt`, `Strombedarf_Max`, `Strombedarf_monat[12]`, `mo_anfang[12]/mo_ende[12]`, `Strombedarf_Gebaeude_gesamt`, `Maximaler_Strombedarf(float[])` |

**Offene Unstimmigkeit (bei Umsetzung prüfen):** `Form_Stromverbraucher.cs:205,223,229` deklariert
`float[] result = new float[8760]` und kopiert das Ergebnis mit `Array.Copy(result, …_viertelStundenwerte,
result.Length)` in ein 35 040-Array; direkt danach läuft `BhkwPlan.MonatsSumme` mit **Stunden**-Monatsgrenzen
über dasselbe Array. Entweder liefert `Stromprofil_Strombedarf_berechnen` doch 35 040 Werte (dann sind
`mo_anfang/mo_ende` Viertelstunden-Indizes), oder hier liegt eine Altlast/ein Fehler vor.
`SimulationStrombedarf.cs` ist dafür zwingend einzusehen.

---

## 2. Existiert bereits ein Lastgang-Datei-Import?

**Ja — und er ist einfacher als vom Konzept unterstellt.**

`Views/Stromverbraucher/Form_Stromganglinie_Admin.cs`, `btn_Einlesen_Click` (Zeile 77-165):

1. `OpenFileDialog` mit Filter **`(*.txt)|*.txt`** — kein CSV, kein Excel.
2. Datei wird nach `Path.Combine(Program.ApplicationPath_User, "Strom")` kopiert.
3. Einlesen über `ToolsClass.OpenText(datei)` (`Allgemein/ToolsClass.cs:95-114`): `File.ReadAllLines`,
   **ein Wert je Zeile**; einzige Prüfung: keine Zeile darf auf `,` oder `;` enden. Keine Kopfzeilen-,
   Trennzeichen-, Dezimaltrenner- oder Zeitstempelbehandlung.
4. Zeitraster aus ComboBox `comboBox_Zeitinterval`: „Stundenwerte" (=1), „1/4 Stundenwerte" (=4),
   „Minutenwerte" (=60).
5. **Harte Anzahlprüfung**: `!= 8760` bzw. `!= 8760*4` (= 35 040) bzw. `!= 8760*60` (= 525 600) → Abbruch
   mit MessageBox. **Schaltjahr (35 136 / 8 784) wird abgelehnt.**
6. Schreiben: `StromganglinieStammCtrl.ImportGanglinie(bezeichner, zeitinterval, List<string>)`
   (`StromganglinieStammCtrl.cs:82-131`) — Kopf-ID = `MAX(ID)+1`, dann **je Wert ein
   `INSERT INTO Tab_StromganglinieDaten_STAMM (ID_Ganglinie, Wert, ReadOnly)`** in **einer** Transaktion;
   Parsing mit **`double.Parse(s, CultureInfo.InvariantCulture)`** (also Punkt als Dezimaltrenner).
   Der Bezeichner ist der Dateiname ohne Endung.
7. Projektvariante identisch in `StromganglinieDatenCtrl.InsertKompletteGanglinie()` (Zeile 111-213) bzw.
   `Insert()` (Zeile 54-109) — Kommentar dort: „Massendaten (z.B. 8760 Stundenwerte) performant schreiben".

**Fazit:** Die 15-Minuten-Fähigkeit ist bereits vorhanden (Zeitinterval = 4). Es fehlen: CSV/Excel,
Dezimalkomma, Zeitstempelspalte, Einheitenwahl (kW/kWh), Schaltjahr, Lücken-/Dublettenprüfung,
Sommerzeit, Validierungsprotokoll. Ein `Allgemein/Import/CsvReader.cs` existiert laut `CLAUDE.md`,
wird für Ganglinien aber **nicht** genutzt.

---

## 3. PV — liefert `SolarPVGISCalculator` / `PhotovoltaikCtrl` eine Erzeugungszeitreihe?

**Nein, beide nicht.** Die Konzeptannahme in Abschnitt 3 ist falsch adressiert.

* **`Allgemein/SolarPVGISCalculator.cs` (520 Zeilen)** enthält drei Dinge:
  * `PVGIS_EPW_Downloader.GetTMY(lon, lat, azimut)` — HTTP-Abruf `…/tmy?…&outputformat=json`
    (`Properties.Settings.Default.PVGISUrl`), Ergebnis `List<TmyHourlyData>`: `time(UTC)` im Format
    `yyyyMMdd:HHmm`, `T2m` [°C], `RH` [%], **`G(h)`, `Gb(n)`, `Gd(h)` [W/m²]**, `WS10m` [m/s], plus
    berechnete Felder `Sol_nord/ost/sued/west`, `WE`, `TagTyp_W`, `TagTyp_NW`, `Sonnenwinkel`.
    **8760 Stundenwerte (TMY), Einheit W/m² — Einstrahlung, nicht Strom.**
  * `SolarCalculator.CalculateHourly(...)` / `.Calculate(...)` — Transposition auf die geneigte Fläche
    (Zeitgleichung, Deklination, Einfallswinkel, Albedo 0,2), Rückgabe `gTotal` in **W/m²**. Die
    Temperaturkorrektur/Modulleistung ist auskommentiert (Zeile 325-331).
  * `PVGIS_EPW_Downloader.GetCoordinatesAsync` — Nominatim-Geokodierung.
  * `AccessRepository.SaveTmyData(...)` — schreibt **8760 Zeilen je Klimaregion** nach `Tab_Klimadaten(_STAMM)`
    bzw. `Tab_Solar(_STAMM)`, zeilenweise, in vorgegebener Transaktion (Zeile 436-517).
* **`Controller/PhotovoltaikCtrl.cs`** ist reine Stammdaten-/Projekt-CRUD auf `Tab_PV` / `Tab_PV_STAMM`
  (`Bezeichner, Firma, Beschreibung, Leistung, Wirkungsgrad, U_Mpp, U_Leerlauf, I_Mpp, I_Kurzschluss,
  alpha_SC, beta_OC, gamma_PMP, T_NOCT, Laenge, Breite, Modulkosten`) — **keine Zeitreihe**.

### Wo die PV-Erzeugungsreihe wirklich liegt

`SimulationPV` (Datei nicht im Upload). Aus den Aufrufstellen in
`Form_Simulation_Detail.cs:1643-1676` und `NavigatorStrom.cs:64,125` gesichert:

| Member | Länge | Einheit (erschlossen) | Beleg |
|---|---|---|---|
| `Stromproduktion` | 8760 | kWh/h (`Sum()/1000` → MWh) | `Form_Simulation_Detail.cs:1643` |
| `Stromproduktion_viertelstunde` | 35 040 | kW | `:1667`, `NavigatorStrom.cs:125` |
| `Ueberschuss` / `Ueberschuss_viertelstunde` | 8760 / 35 040 | wie oben | `:1644,1665` |
| `Strombedarf` | 35 040 | kW (`Sum()/4000` → MWh) | `:1646` |
| `Strombedarf_stuendlich` | 8760 | kWh/h | `:1645` |
| **`Speicherfuellstand`** | 8760 | **kWh** | `:2106` |
| **`Speicherfuellstand_viertelstunde`** | 35 040 | kWh | `:1664` |
| `MaxPSolar` | – | W/m² | `:1671` |
| `Modul_Ergebnisse` | Liste | `Name, Flaeche, Anzahl, Stromproduktion` | `:1676-1687` |

**Wichtig:** Der Speicherfüllstand hängt heute am PV-Objekt, nicht an einem eigenen Speicherobjekt.

---

## 4. BHKW — elektrische Erzeugungszeitreihe und Überschuss

**`Allgemein/BhkwPlan.cs` ist kein BHKW-Anlagenmodell.** Es ist der verwaltete C#-Port des nativen
Rechenkerns `BHKWPLAN.DLL` (Borland C, x86), Namespace **`WPPlan.Core`**, `public static class BhkwPlan`
(Zeile 3-31). Konstanten `Hours = 8760`, `WeekHours = 168`, `Days = 365`, `Months = 12`, `HoursPerDay = 24`;
Vektoren durchgängig `float[]`, Arrays werden **in-place** überschrieben. Enthaltene Funktionen:
`VectorInit`, `WattToKw`, `VectorenAddieren`, `VectorSumme` (×0,001), `Normieren`, `NetzverlusteC`,
`MonatsSumme`, `MonatsGrenzen`, `Heapsort` (→ Jahresdauerlinie), `StromWocheToJahr`, `StdWerte`,
`SolareGewinneC`, `SpezWaermeverlusteC`, `TaeglHeizlastWG` (instationäres RC-Modell mit **globalem
Zustand** `_prevRoomTemp` — nicht thread-sicher!).

**`Controller/BHKWCtrl.cs` / `BHKWStammCtrl.cs`** sind reine CRUD auf `Tab_BHKW` / `Tab_BHKW_STAMM`
(`Ptherm, Pel, Brennstoff, Wirkungsgrad, Investition_kwel, Wartungskosten_kwhel, Nutzungsdauer,
Grenzleistung, Motortyp, Kosten_Modul/Montage/Lieferung/Schallschutzhaube/Abgasreinigung, NOx/SO2/CO/CO2/Staub,
Vorlauf, Ruecklauf`) — keine Zeitreihe.

### Erzeugungszeitreihe und Fahrweise

* Reihe: **`sim.simulation_bhkw.stromproduktion`** — **stündlich (8760)**, wird für die Anzeige mit
  `sim.Stundenwerte_zu_viertelstunden(...)` auf 35 040 gespreizt (`NavigatorStrom.cs:100`). Ferner
  `simulation_bhkw.waermebedarf` (`Form_Simulation_Detail.cs:1698`).
* **Fahrweise ist bereits wählbar**, aber projektweit, nicht je Anlage:
  `radioButton_Waermegefuehrt` / `radioButton_Stromgefuehrt` / `radioButton_OhneStromEinspeisung` mit
  `Tag` = **0 / 1 / 2**, gespeichert in **`Tab_Einstellungen.Betriebsart`**
  (`Form_Simulation_Detail.cs:2172-2194`) und an die Engine übergeben als `sim.modeBHKW`
  (`:918`). Ergänzend `sim.GrenzleistungBHKW` ← `numericUpDown_UnteresteLG` ← `Tab_Einstellungen.Leistungsgrenze`
  und `sim.VolumenPendelspeicherBHKW` ← `Tab_Einstellungen.Pendelspeicher`.
  `Tab_Energieanlagen` führt zusätzlich je Anlagenzeile ein eigenes Feld `Betriebsart`.
* **Ein expliziter BHKW-Strom-Überschuss existiert nicht.** `ErgebnisBHKWModel` (`ErgebnisModel.cs:83-99`)
  kennt `Stromproduktion`, `Strombedarf`, `Reststrombedarf`, `Strombedarfsdeckung`,
  `Waermeueberschuss` (thermisch!), `Gasverbrauch_Hu` — aber keinen elektrischen Überschuss als Reihe.
  Der ladefähige BHKW-Überschuss ist also **neu zu bilden**: `max(0, stromproduktion[i] − Last[i])`
  bzw. bei Modus 2 („ohne Stromeinspeisung") ist er per Definition gleich der abgeregelten Menge.

---

## 5. Kostenmodul — Preismodell und Andockpunkt

### Struktur `Views/Kosten/Form_Kosten.cs` (1005 Zeilen)

Drei Reiter über `tabMain`, `kategorieID = tabMain.SelectedIndex + 1`:
**1 = Investitionskosten, 2 = Betriebskosten, 3 = Energiekosten** (`:437-470`).
Die Komponentenliste wird über die Bitmaske `Program.startfrm.status` freigeschaltet (`:47-53`) —
**`0x4` = Stromspeicher**; `GetKomponentenID("Stromspeicher") = 5` (`:592-604`).

**Invest/Betrieb (Kategorie 1/2)** — reine Beträge, keine Preisreihen:
* `Tab_ProjektWerte (ID, ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, BestCase,
  WorstCase, Nutzungsdauer, BestCase_Nutzungsdauer, WorstCase_Nutzungsdauer, Einheit, Gruppe)`
  (`:566-585`, `migration.manuell.sql:492-494`).
* Katalog `Tab_Kostenfaktor` (mit `IsMainComponent`), `Tab_KostenGruppenKatalog (GruppenName)`;
  Abfragen `Abfrage_Kostenfaktoren`, `Abfrage_KostenKomponenten`, `Abfrage_ProjektKostenKomponenten`.
* UI: `ucKostenZeile` / `KostenPosition` (`ucKostenItem.cs`) — Betrag, Nutzungsdauer, Best-/Worst-Case,
  Einheit als Freitext; „🔄 Planwert übernehmen" holt die Modulkosten aus dem Technikmodul
  (`GetModulKosten`, `:704-717`).

**Energiekosten (Kategorie 3)** — `ucFuelSettings.cs` (589 Zeilen), Tabellen:

| Tabelle | Relevante Spalten |
|---|---|
| `energy_carrier` | `id, ID_Brennstoff, code, name, group_code, pricing_model, billing_unit, hi_kwh_per_unit, hs_kwh_per_unit, price_work, price_base, co2, so2, nox, is_active` |
| `pricing_model` | `code, has_hi, has_hs, **has_powerprice**` |
| `energy_conversion` | `ID, id_brennstoff, from_unit, to_unit, factor, user_edited` |
| `energy_project_settings` | `ID, ID_Projekt, ID_Energieträger, ID_Umrechnung, custom_hi, custom_hs, custom_price_work, custom_price_base, **custom_price_power**, co2, so2, nox` |
| `energy_price` (Historie) | `id, ID_Projekt, carrier_id, **valid_from**, valid_to, grundpreis, arbeitspreis, arbeitspreis_unit, Heizwert, **leistungspreis**, notes` |
| `Tab_Brennstoff_Stamm` | `… Standard_Grundpreis, Standard_Arbeitspreis, **Standard_Leistungspreis**, Hi, Hs, CO2/SO2/NOx/Staub, PE_Faktor, Einheit, PreisEinheit` |

* Strom läuft als `pricing_model = "ELECTRICITY"` durch dasselbe `ucFuelSettings`; wegen `HasHi = false`
  gilt „Direktabrechnung nach kWh", das Arbeitspreis-Label wird auf **„€ / kWh"** gesetzt und die
  Heizwert-Formel ausgeblendet (`ucFuelSettings.cs:79-89, 260-279`).
* **Preise sind Skalare mit Gültigkeitsdatum — es gibt kein Tages-/Wochen-/Monatsprofil, kein HT/NT,
  keine Spotreihe.** Die einzige zeitliche Dimension ist `valid_from` (Versionshistorie im DataGridView
  `dgvHistory`).
* **Ein Leistungspreis existiert bereits** (`numLeistungspreis`, `_basePowerPrice`,
  `custom_price_power`, `energy_price.leistungspreis`, Schalter `pricing_model.has_powerprice`,
  Default aus `Tab_Brennstoff_Stamm.Standard_Leistungspreis`) — **aber mit unklarer Einheit**: das Label
  wird als `€/{conv.ToUnitCode}` gesetzt (`:185, 251`), für Strom also **„€/kWh"**, während die
  Auslese-Eigenschaft `LeistungspreisEurYear` heißt (`:460`). Faktisch ist es ein freies Zahlenfeld
  ohne durchgesetzte Einheitensemantik.
* **Aufschlagskomponenten (Netzentgelt, Umlagen, Stromsteuer, Konzessionsabgabe, Vertrieb) existieren
  nicht** — weder als Tabelle noch als Felder. Es gibt nur Grundpreis / Arbeitspreis / Leistungspreis.

**Andockpunkt für das Bezugskosten-/Aufschlagsmodell (Konzept 4.2/4.4):** `energy_project_settings`
je (`ID_Projekt`, `ID_Energieträger` mit `pricing_model='ELECTRICITY'`) um die Aufschlagskomponenten
+ Aktiv-Flags + Override erweitern, Historie weiterhin in `energy_price`. `p_bezug[i]` = Arbeitspreis
(oder Profil/Spot) + Summe der aktiven Aufschläge. `L_P` sollte **nicht** auf `leistungspreis`
umgedeutet werden, sondern als eigenes Feld mit erzwungener Einheit €/(kW·a) daneben stehen
(oder `arbeitspreis_unit`-analoges Einheitenfeld ergänzen).

---

## 6. Bestehendes Stromspeicher-Modul — Ist-Zustand

### 6.1 Feldbestand (Abgleich gegen Konzept-Parameterliste 5.1)

`Model/StromspeicherModel.cs` hat **exakt acht** Felder; `Tab_Stromspeicher_STAMM` hat dieselben
Spalten plus `ReadOnly`, `Tab_Stromspeicher` dieselben plus `ID_Projekt`
(`migration.manuell.sql:103-106, 313-317`; `StromspeicherStammCtrl.cs:78-95`):

| Konzept 5.1 | Bestand | DB-Spalte | Bemerkung |
|---|---|---|---|
| C_nom [kWh] | **ja** | `Energie` | Einheit nicht deklariert; Eingabefeld nur `checkInt`-validiert (`Form_Stromspeicher.cs:246`) |
| P [kW] | **ja** | `Leistung` | genau **ein** Feld → deckt sich mit der Entscheidung „eine gemeinsame Lade-/Entladeleistung" |
| Min. Ladezustand | **teilweise** | – | nicht im Speicher-Datensatz, aber projektweit `Tab_Einstellungen.Ladefuellstand_Min` (+ `_Auswahl`) |
| Max. Ladezustand | **teilweise** | – | projektweit `Tab_Einstellungen.Ladefuellstand_Max` (+ `_Auswahl`) |
| Verlustfaktor / η_RT, η_ch, η_dis | **fehlt** | – | – |
| N_zyk | **fehlt** | – | – |
| Degradation d [%/a] | **ja** | `Degradation` | Einheit nicht deklariert |
| c_ver | **fehlt** | – | – |
| c_cap [€/kWh] | **teilweise** | `Modulkosten` | Einheit unklar (€ oder €/kWh); Feld nur `checkInt` |
| c_pow [€/kW], I_fix | **fehlt** | – | – |
| L_P [€/(kW·a)] | **fehlt** hier | (`energy_price.leistungspreis`) | s. Punkt 5 |
| a_netzlade, i_z, N, Standby, Betriebskosten | **fehlt** | – | – |
| Betriebsart / Quellen-Flags / Netzentladung | **fehlt** | – | – |
| Berechnungsart, Preisquelle, Kompatibilitätsmodus | **fehlt** | – | – |
| Variantenschlüssel + „aktiv" | **fehlt** in `Tab_Stromspeicher` | – | vorhanden als Instanzmuster in `Tab_Energieanlagen` (Punkt 9) |
| **zusätzlich vorhanden, im Konzept nicht geführt** | | `Typ` (Freitext, Vorgabe „Lithium-Ionen"), `Ladezustand`, `Bezeichner`, `ReadOnly` | `Ladezustand` ist semantisch unbestimmt (Start-SoC? nutzbarer SoC?) und muss geklärt werden |

Weiter existieren die **Ladeparameter je Projekt** in `Tab_Einstellungen` / `KonfigurationModel`
(`KonfigurationCtrl.cs:57-66, 77-114`):
`Ladefuellstand_Min`, `Ladefuellstand_Max`, `Ladeleistung_Max` (je mit `*_Auswahl` = Einheitenauswahl als
Text) und `Ladeschwellwert`. Bedient in `Form_Simulation_Detail` auf `tabPage_Stromspeicher_Parameter`
(`textBox_Stromspeicher_Ladeenergie_min/_max`, `textBox_Stromspeicher_Ladeleistung_max`,
`textBox_Speicher_Ladeschwelle` + drei ComboBoxen, `:2400-2434`), Speicherung sofort bei `Leave` über
`SpeichereKonfigurationsAenderung()`. **Damit sind SoC-Band und Ladeleistung heute projektweit,
nicht speicherbezogen** — ein direkter Konflikt zum Varianten-Konzept.

### 6.2 Was tun Controller und Formulare heute?

* `StromspeicherStammCtrl` (`Controller/`): CRUD auf `Tab_Stromspeicher_STAMM`, `ReadOnly`-Schutz,
  explizite IDs (`GetMaxID+1`).
* `StromspeicherCtrl`: Lesen aus `Tab_Stromspeicher` + `CopyFromStamm(stammId|bezeichner, idProjekt)`,
  `GetProjektId`, `ExistsInProjekt`, `DeleteFromProjekt` — exakt das Muster von `BHKWCtrl`/`PhotovoltaikCtrl`.
* `Views/Stromspeicher/Form_Stromspeicher.cs`: Auswahl-Dialog (DataGridView aus `Tab_Stromspeicher_STAMM`
  → ListBox der Projektauswahl). Schreibt in `list_werzmodel` (`WErzeugerModel` mit
  `ID_SP`, `ID_Type = WizardItemClass.SP_TYP`) — die Persistenz erfolgt später über
  `WizardCtrl.Del_Projekt_Waermeerzeuger` / `Add_WP_Waermeerzeuger` in `Tab_Energieanlagen`
  (`StromspeicherKontextMenuCtrl.cs:135-148`). Die Textfelder sind **reine Anzeige aus STAMM**.
* `Form_AdminStromspeicher.cs`: Stammdatenpflege (Neu/Speichern/Löschen). **Achtung:**
  `double.Parse(textBox…​.Text)` **ohne** `CultureInfo` (`:84-88`).
* `Form_Sp_ItemNeu` (Datei `Form_StromspeicherItemNeu.cs`): nur Namensabfrage.
* `Controller/StromspeicherKontextMenuCtrl.cs` enthält die Klasse **`SpKontextMenuCtrl : Form`**
  (Dateiname ≠ Klassenname!) — Kontextmenü „Hinzufügen/Bearbeiten" / „Löschen" für `listView_SP`
  in `FormMain`; unterscheidet `listView_SP_REF` (→ `REF_SP_TYP`) von `listView_SP` (→ `SP_TYP`).
* `MenueCtrl.StromspeicherBearbeiten()` → `Form_AdminStromspeicher` (Menüpunkt
  `MDIMainForm.MenuItem_Stromspeicher_Click`, `:335-339`).
* `FormMain.SetSPControl(projekt)` (`:266-301`): listet je Projekt alle `Tab_Energieanlagen`-Zeilen mit
  `ID_Type IN (SP_TYP, REF_SP_TYP)` und liest zu jeder `Tab_Stromspeicher` über `ID_SP`;
  Spalten: Name, Typ, Leistung [kW], Energie, Degradation, Ladezustand.

### 6.3 **Es gibt bereits Speicher-Simulation** (Konzeptannahme „nur Stammdaten" ist falsch)

Drei Fundstellen:

1. **`SimulationSSP`** — in `CLAUDE.md` als Simulationsmodul gelistet, Flag `sim.bSimulationSSP`
   wird in `Form_Simulation_Detail.cs:980` in `ErgebnisModel.Sim_Stromspeicher` geschrieben.
   Quellcode liegt nicht vor.
2. **SoC-Zeitreihe + Chart**: `sim.simulation_pv.Speicherfuellstand_viertelstunde` wird als Chart-Serie
   **„Speicherfüllstand"** in `_chartManager[9]` gezeichnet, auf die **Sekundärachse „Speicher [kWh]"**
   gelegt und über `checkBox_Speicherzustand` ein-/ausgeschaltet
   (`Form_Simulation_Detail.cs:1664-1670, 2098-2170`).
3. **`Views/Simulation/DashboardForm.cs`** enthält eine **zweite, unabhängige Speicherrechnung**:
   stündlich (`for i < 8760`), verlustfrei bis auf einen festen Wechselrichterfaktor 0,95, **nur
   Kapazität** (`numSpeicherKWh`), keine Leistungsgrenze, kein SoC-Band, keine Degradation
   (`:113-131`), plus eine Monatsauswertung mit **730-Stunden-Pseudomonaten** (`:182-184`).
   Ausgaben: Autarkiegrad PV, „Speichernutzen [kWh/Jahr]", CO₂-Ersparnis, gestapeltes Monatsdiagramm
   (Direktverbrauch / Speichernutzung / Netzbezug).

`ErgebnisModel`/`Tab_Ergebnis` führen zwar das Flag `Sim_Stromspeicher`, es gibt aber **kein**
`ErgebnisStromspeicherModel` und keine Detailtabelle `Tab_ErgebnisStromspeicher`
(`ErgebnisModel.cs:29-37`, `ErgebnisCtrl.cs:20-31`).

---

## 7. Persistenz — Access-Zugriffsmuster und Zeitreihenablage

### Zugriffsmuster

* Backend: **MS Access `Kenndaten.accdb`** über **ACE OLEDB 12.0**;
  Pfad = `Properties.Settings.Default.DBPath` sonst `%ProgramData%\EPOS_PLAN\`
  (`DataRepository.GetDBPath()`, `:170-182`). x86-Pflicht.
* **`Allgemein/DataRepository.cs`** (statisch, 246 Zeilen): `GetDataTable`, `ExecuteSQL`,
  `ExecuteNonQuery`, `ExecuteScalar`, `ExecuteInsertAndGetId` (`SELECT @@IDENTITY`), `BeginTransaction`
  (liefert `(OleDbConnection, OleDbTransaction)`), `GetMaxID`, `GetIdByName`, `GetValueById`,
  `DeleteWithDependencies`. **Pro Aufruf eine neue Verbindung.** Fehler werden per **`MessageBox.Show`**
  gemeldet — eine UI-freie Engine darf `DataRepository` daher nicht direkt verwenden.
* **`Allgemein/RecordSet.cs`** (153 Zeilen): Legacy-Forward-Reader (`Open(sql)`, `Next()`, `EOF()`,
  `Read(name)`, `Close()`, `IDisposable`). Wird an vielen Stellen mit **String-Verkettung** aufgerufen
  (z. B. `Form_Stromspeicher.cs:105`, `StromTestClass.cs:43-48`) — SQL-Injection und Kulturprobleme.
* **IDs werden fast überall explizit als `MAX(ID)+1` vergeben**, nicht per AutoWert
  (Kommentar dazu in `migration.manuell.sql:28-31`). Das ist das verbindliche Muster für neue Tabellen.

### Relevante Tabellen (aus `migration.manuell.sql`)

| Bereich | Tabellen |
|---|---|
| Stromspeicher | `Tab_Stromspeicher_STAMM` (Z. 103-106), `Tab_Stromspeicher` (Z. 220, 313-317) |
| Ganglinie | `Tab_Stromganglinie_STAMM` / `Tab_StromganglinieDaten_STAMM` (Z. 158-161, 190-194), `Tab_Stromganglinie` / `Tab_StromganglinieDaten` (Z. 413-422), `Z_ProjektStromganglinie` (Z. 424-426) |
| Verbraucher | `Tab_Stromverbraucher(_STAMM)`, `Tab_Stromverbrauchertyp(_STAMM)` (168 Spalten), `Z_Projekt_Stromverbraucher` |
| TagV | `Tab_DBTagV(_STAMM)`, `Tab_DBTagVDaten(_STAMM)` |
| Kosten | `Tab_ProjektWerte` (Z. 492-494), `Tab_Kostenfaktor`, `Tab_KostenGruppenKatalog`, `energy_carrier`, `pricing_model`, `energy_conversion` (Z. 498-500), `energy_price` (Z. 502-504), `energy_project_settings` (Z. 506-508), `Tab_Brennstoff_Stamm` (Z. 69-71) |
| Projekt/Anlagen | `Tab_Projekt`, **`Tab_Energieanlagen`** (Z. 348-351), `Tab_Einstellungen` (Z. 510-512), `Z_ProjektPufferSp`, `Z_ProjektWaermebedarf`, `Z_ProjektGebaeude`, `Z_Projekt_Brauchwasser`, `Z_Projekt_Prozesswaerme`, `Z_ProjektSolarganglinie` |
| Ergebnis | `Tab_Ergebnis`, `Tab_ErgebnisEnergiebedarf`, `Tab_ErgebnisWaermepumpe(+Modul)`, `…BHKW(+Modul)`, `…Heizkessel(+Modul)`, `…Solarthermie(+Modul)`, `…Photovoltaik(+Modul)` |

### Wie werden große Zeitreihen heute gespeichert?

**Zeilenweise in Access — ausnahmslos. Kein BLOB, keine Datei.**

* `Tab_StromganglinieDaten(_STAMM)`: 1 Zeile je Intervall; der Import lässt **8 760, 35 040 oder
  525 600** Werte zu (`Form_Stromganglinie_Admin.cs:126-140`). Schreiben: 1 `ExecuteNonQuery` je Zeile
  in **einer** Transaktion (`StromganglinieStammCtrl.cs:106-119`).
* Ebenso `Tab_SolarganglinieDaten`, `Tab_WaermebedarfDaten`, `Tab_Klimadaten`/`Tab_Solar`
  (8 760 Zeilen je Klimaregion, `SolarPVGISCalculator.cs:436-517`).
* Die Migration kopiert diese Reihen **je Projekt erneut** (`migration.manuell.sql:418-422`) —
  ID-Schema `(ID_Projekt+OFFSET)*1000000 + alte ID`, d. h. **maximal 10⁶ Datenzeilen je Projekt**.
* Reihenfolge = ID-Reihenfolge; **es gibt keine Zeitstempelspalte**.

**Bewertung gegen die Konzeptempfehlung (Abschnitt 8, „keine 35.040 Zeilen in Access"):**
Die Empfehlung ist in dieser Absolutheit **nicht durch den Code gedeckt** — das Haus-Muster tut
genau das schon heute, inklusive 15-Minuten-Raster. Belastbar sind dagegen diese Einschränkungen:
(i) Schreiben und Lesen erfolgen zeilenweise und sind entsprechend langsam;
(ii) das ID-Schema der Migration begrenzt auf < 10⁶ Zeilen je Projekt;
(iii) Access-Grenze 2 GB je Datei;
(iv) **Ergebniszeitreihen werden heute überhaupt nicht persistiert** — `Tab_Ergebnis*` speichert
ausschließlich Skalare (MWh/kW/%/h). Für Eingangs-Lastgänge ist die Wiederverwendung von
`Tab_Stromganglinie(Daten)` daher das systemkonforme Vorgehen; für SoC-/Geldwert-Ergebnisreihen ist
Datei/BLOB neben dem Projekt sinnvoll, weil dafür schlicht kein Bestandsmuster existiert.

**Fallstrick für Schemaerweiterungen:** `KonfigurationCtrl.ReadSingle` liest `Tab_Einstellungen`
über **Positionsindizes `row[0] … row[22]`** (`:44-66`). Neue Spalten müssen **am Ende** angefügt und
die Indizes erweitert werden, sonst verschieben sich alle Werte.

---

## 8. Simulationsfluss und Einhängepunkte

### Kette

```
MDIMainForm / FormMain  ──▶  Form_Simulation_Config   (Tab_Einstellungen.Tool_1..Tool_6, Z_ProjektPufferSp)
                                     │
                                     ▼
                        Form_Simulation_Detail.UpdateTabPages()      ← liest Tab_Einstellungen
                                     │  baut dictAllTabPages / dictParameterTabPages
                                     │  BefuelleQuellenListe() → listViewQuellen (Tag = TabPage-Name)
                                     ▼
                        btn_Simulation_Click()  →  sim.Do_Simulation(m_ID_Projekt)
                                     │              (SimulationControl, synchron)
                                     ▼
                        Endergebniss_Simulation()  → TextBoxen + _chartManager[0..n]
                        FuelleUebersicht()
                                     ▼
                        btn_ErgebnisSpeichern_Click → SpeichereErgebnis() → ErgebnisCtrl.Save()
```

### Konfiguration (`Form_Simulation_Config.cs`, 1704 Zeilen)

Sechs „Tools" mit Checkbox + ComboBox → `Tab_Einstellungen.Tool_1 … Tool_6` (`:1511-1533`):
Tool 1-4 = Wärmeerzeuger (`BHKW`, `Heizkessel`, `Solarthermie`, `Wärmepumpe`), **Tool 5 = `Photovoltaik`**
(comboBox5), **Tool 6 = `Stromspeicher`** (comboBox6, `:119-122, 155-159`; Anzeigetext aus
`MyResource.Resource.KONFIG_STROMSPEICHER`). Zusätzlich Pufferspeicher-Zuordnung in `Z_ProjektPufferSp`
(`Erzeuger, PufferSp, Vorlauf, Ruecklauf, Prioritaet`) und je Anlage Wärmequelle/-senke/Betriebsmodus
über `WaermequelleClass` auf `Tab_Energieanlagen`.

### Detail (`Form_Simulation_Detail.cs`, 2522 Zeilen) — die Stromspeicher-Haken sind schon da

* `dictAllTabPages` enthält **`"tabPage_Stromspeicher"`** (`:338`),
  `dictParameterTabPages` enthält **`"tabPage_Stromspeicher_Parameter"`** (`:367`).
* `BefuelleQuellenListe()` fügt bei `tool[5] == "Stromspeicher"` den Navigationseintrag
  **„Stromspeicher"** mit `Tag = "tabPage_Stromspeicher"` ein (POS 4, `:480-487`).
* Icon: `ZeichneGewerkIcon`, `case "tabPage_Stromspeicher": // Batterie` (`:704-714`).
* `TabListMapper` (`Views/Simulation/TabListMapper.cs`, 462 Zeilen) koppelt ListView ↔ TabControl
  für die Parameter-Unterseiten (`BuildItems()`, `IconKeyFromPage()`, Menü-Styling).
* Bestehende Speicher-Eingaben auf `tabPage_Stromspeicher_Parameter`: s. Punkt 6.1.
* **Threading:** die gesamte Kette läuft **synchron im UI-Thread**; kein `Task`, kein
  `BackgroundWorker`, kein `async`. Nur `Cursor.Current = Cursors.WaitCursor` (Import) und
  `Application.DoEvents()` (`ToolsClass.ReadExcel`) als Notbehelf.

### Einhängepunkte für die vier Berechnungsarten und die Ergebnisanzeige

| Was | Wohin |
|---|---|
| Parameter (Betriebsart, Quellen, SoC-Band, η, Wirtschaftlichkeit) | `tabPage_Stromspeicher_Parameter` in `Form_Simulation_Detail`; Persistenz s. Punkt 9 |
| Auswahl der Berechnungsart | neue ComboBox/RadioGroup dort; Vorbild `radioButton_Waermegefuehrt/_Stromgefuehrt/_OhneStromEinspeisung` mit `Tag`-Wert und `SpeichereKonfigurationsAenderung(...)` (`:2172-2194`) |
| Rechenaufruf | entweder in `SimulationControl.Do_Simulation()` einreihen (dann läuft die Speicherrechnung nur zusammen mit der Wärmesimulation) **oder** eigener Button auf der Speicherseite, der die Engine mit Lastgang/PV/BHKW-Reihen füttert — Letzteres ist für die 120 Rasterläufe der Optimierung zwingend |
| SoC-Gang | Chart-Serie wie „Speicherfüllstand" auf der **Sekundärachse** (`Form_Simulation_Detail.cs:2124-2160` als fertige Vorlage) |
| Kennzahlen | `ErgebnisModel` + `ErgebnisCtrl`: neues `ErgebnisStromspeicherModel` und Tabelle `Tab_ErgebnisStromspeicher` (+ ggf. `…Variante` als Modulliste, exakt nach dem Muster `Tab_ErgebnisPhotovoltaik(+Modul)`); **`Sim_Stromspeicher` existiert bereits** in `Tab_Ergebnis` und wird schon geschrieben |
| Export | `CsvExportClass.Export(dateiname, float[] temperatur, List<CsvSpalte> spalten, bool)` mit `new CsvSpalte("Name [kW]", float[])` — in `NavigatorStrom.cs:48-72` mit 35 040 Werten je Spalte im Einsatz, plus `InitCsvExportButtons()` in `Form_Simulation_Detail.cs:213-303` |

### Chart-Muster

`ChartManager` (`Allgemein/GrafikTools/`, Wrapper um `System.Windows.Forms.DataVisualization.Charting.Chart`).
Belegte API (aus `NavigatorStrom.cs:91-134` und `Form_Simulation_Detail.cs:1607-1670`):
`_chart`, `BackColor`, `YMinValue`, `YMaxValue`, `XAxisAsNumber`, `XAxisTitle`, `YAxisTitle`,
`toolTipUnit`, `ChartTitle`, `MitLegende`, `MitChartBorder`, `AreaLine`, `MaxXVALUE`,
**`MitViertelStunde`**, `Init()`, `AddSeries(string, Color, float[])`, `CalculateNiceInterval(double, int)`.
In `Form_Simulation_Detail` als Array `_chartManager[0..n]` geführt (Index 8 = Solarthermie,
**9 = PV/Speicher**, 10 = BHKW-Wärme). `DashboardForm` nutzt daneben direkt die Chart-Klasse
(StackedColumn, Monatsbalken) — das ist das Muster für die Energieflussbilanz aus Konzept 7.2.

---

## 9. Projektstruktur — Stamm- vs. Projektdaten, Muster für Speicher-Varianten

Es existieren **drei** Muster, nicht zwei:

**A · Katalog → Projektkopie (`XxxStammCtrl` ↔ `XxxCtrl.CopyFromStamm`).**
`Tab_Xxx_STAMM` (global, mit `ReadOnly`) → `Tab_Xxx` (mit `ID_Projekt`, ohne `ReadOnly`).
Identisch implementiert in `StromspeicherCtrl.CopyFromStamm` (`:198-260`), `BHKWCtrl.CopyFromStamm`
(`:215-299`), `PhotovoltaikCtrl.CopyFromStamm` (`:201-265`): Stammsatz lesen → Dublettenprüfung
über (`Bezeichner`, `ID_Projekt`) → neue ID `DataRepository.GetMaxID("Tab_Xxx") + 1` → INSERT.
Rückgabe = Projekt-ID, die in `WErzeugerModel.ID_SP` / `ID_BHKW` / `ID_PV` einzutragen ist.

**B · Zuordnungstabellen `Z_*`** (n:m Projekt ↔ Katalogobjekt, mit projektspezifischen Zusatzfeldern):
`Z_Projekt_Stromverbraucher (…, Summe)`, `Z_ProjektStromganglinie (ID/ID_Z, ID_Projekt, ID_Ganglinie,
Bezeichner)`, `Z_ProjektSolarganglinie`, `Z_ProjektWaermebedarf`, `Z_ProjektGebaeude`,
`Z_Projekt_Brauchwasser`, `Z_Projekt_Prozesswaerme`, `Z_ProjektPufferSp (…, Erzeuger, Vorlauf,
Ruecklauf, Prioritaet)`. Controller-Muster: `Z_ProjektXxxCtrl.ReadAll(sql)` mit voller SQL-Zeichenkette.

**C · `Tab_Energieanlagen` — die Anlagen-Instanztabelle. Das ist das Muster für Speicher-Varianten.**
Spalten (vollständig aus `migration.manuell.sql:349-351`):
```
ID, ID_Projekt, Bezeichner, ID_Type, ID_WP, Betriebsart, Sperrung, Sperrzeit_von, Sperrzeit_bis,
Vorlauf, Rücklauf, Bivalenter_Betrieb, Abschaltpunkt, Nutzungszeit, ID_SP, ID_PV, ID_Solar,
Heizstab, Volumen, rendeMix, Solaranteil, ID_Kessel, ID_BHKW, Grenzleistung,
Kollektormodulanzahl, PV_Leistung, Neigung, Azimut, ID_PUFFER
```
* Eine Zeile = **eine Anlageninstanz im Projekt**, mit **instanzspezifischen Parametern** direkt in der
  Zeile (`Betriebsart`, `Vorlauf`, `Grenzleistung`, `Neigung`, `Azimut`, `Volumen`, …).
* `ID_Type` aus `WizardItemClass`: `SP_TYP`, **`REF_SP_TYP`**, `WP_TYP`/`REF_WP_TYP`,
  `KESSEL_TYP`/`REF_KESSEL_TYP`, `BHKW_TYP`, `PV_TYP`, `SOLAR_TYP`, `PUFFER_TYP`
  (`FormMain.cs:242,273,406,1018,1074,1102,1134`; `StromspeicherKontextMenuCtrl.cs:108-117`).
  **Ein Referenz-/Planvarianten-Konzept ist damit bereits vorhanden** (`REF_*` = Bestand/Referenz).
* **Mehrere Speicher je Projekt sind heute schon möglich**: `FormMain.SetSPControl` iteriert über alle
  Zeilen mit `ID_Type IN (SP_TYP, REF_SP_TYP)` und listet sie (`:266-301`).
* Schreiben über `WizardCtrl.Del_Projekt_Waermeerzeuger(id, id_type)` +
  `Add_WP_Waermeerzeuger(id, List<WErzeugerModel>)`; Lesen über
  `WErzeugerCtrl.ReadAllFilter("ID_Projekt=… and ID_Type=…")`.

**Anlage einer projektbezogenen Speicher-Variante (empfohlener Ablauf, systemkonform):**
1. `StromspeicherCtrl.CopyFromStamm(bezeichner, idProjekt)` → Projekt-Datensatz in `Tab_Stromspeicher`
   (technische Basisdaten C_nom, P, Typ, Degradation, Modulkosten).
2. Eine Zeile in `Tab_Energieanlagen` mit `ID_Type = SP_TYP` (bzw. `REF_SP_TYP` für die Referenzvariante),
   `ID_SP` = Projekt-ID aus (1), `Bezeichner` = Variantenname.
3. Variantenspezifische Parameter (Betriebsart, Quellen-Flags, SoC-Band, η, N_zyk, c_ver, c_cap, c_pow,
   i_z, N, Berechnungsart, „aktiv") entweder als **neue Spalten in `Tab_Energieanlagen`** (so macht es die
   Anwendung bei allen anderen Gewerken) oder als neue 1:1-Tabelle `Tab_StromspeicherVariante`
   (`ID, ID_Energieanlage, …`) — Letzteres ist sauberer, weil `Tab_Energieanlagen` bereits 29 Spalten hat
   und von allen Gewerken geteilt wird.
4. **`Tab_Einstellungen.Ladefuellstand_Min/_Max/Ladeleistung_Max/Ladeschwellwert` (projektweit) sind
   damit redundant** und müssen entweder auf die Variante migriert oder als Vorgabewert umdeklariert werden.

**Absprung-/Aktualisierungsmuster zwischen Formularen** (überall gleich, z. B.
`StromspeicherKontextMenuCtrl.cs:100-149`, `StrombedarfKontextMenuCtrl.cs:90-133`):
`*KontextMenuCtrl.Init(ListView, ID_Projekt, Projektname)` → `Form.ShowDialog()` → bei `DialogResult.OK`:
Zuordnungen löschen + neu schreiben → `ProjektCtrl.ReadSingle(name); m_Aenderungsdatum = DateTime.Now;
Update();` → `Program.mainfrm.SetXxxControl(...)` zur Auffrischung der Übersicht.

---

## 10. Sonstige Befunde mit Konzeptrelevanz

1. **.NET 8, nicht .NET Framework.** `WindowsFormsApplication1.csproj`: `net8.0-windows`,
   `UseWindowsForms` + `UseWPF`, `PlatformTarget x86` (ACE OLEDB), `LangVersion latest`,
   `Nullable disable`, `SatelliteResourceLanguages de-DE;en-US`. `Parallel.For`, `Span<T>`, moderne
   Sprachfeatures stehen zur Verfügung → die UI-freie `SpeicherEngine` (Konzept 8) ist unproblematisch.
   **Es gibt keine `.sln`** (CLAUDE.md), ein zweites `.csproj` müsste also manuell referenziert werden;
   pragmatischer ist ein eigener Ordner/Namespace ohne `System.Windows.Forms`-Referenzen im
   bestehenden Projekt.
2. **Chart-Bibliothek — Prüfpunkt 6 des Konzepts beantwortet:** produktiv ist
   **`WinForms.DataVisualization` 1.10.2** über `ChartManager`. **`ChartManagerNeu.cs` ist per
   `<Compile Remove>` vom Build ausgeschlossen** — also nicht verwenden. Zusätzlich ist
   **`ScottPlot.WinForms` 5.1.57** referenziert (plus SkiaSharp 3.119) und für 35 040 Punkte mit Zoom
   die deutlich bessere Wahl für den SoC-Jahresgang; im geprüften Code wird ScottPlot allerdings
   nirgends verwendet.
3. **`MathNet.Numerics` 5.0.0** ist verfügbar (Statistik, Interpolation, Optimierung) — **aber kein
   LP-Solver**. Die Konzeptempfehlung „Greedy zuerst, LP nur bei Bedarf" bleibt damit richtig.
4. **Threading:** kein Async-Muster im Bestand (s. Punkt 8). Die Rastersuche (120 Jahresläufe) würde die
   UI blockieren. `Parallel.For` ist möglich, **aber `BhkwPlan.TaeglHeizlastWG` hält globalen Zustand
   (`_prevRoomTemp`)** und `Program.*` ist prozessweit veränderlich — die Speicher-Engine muss davon
   strikt getrennt bleiben (keine Statics, keine `DataRepository`-Aufrufe, kein `MessageBox`).
5. **de-DE-Parsing ist im Bestand uneinheitlich** — die pauschale Konzeptvorgabe
   „`double.Parse` mit `InvariantCulture`" ist nur für **Dateien** richtig:
   * `Program.checkDouble/checkInt` → `double.TryParse(text, out n)` / `int.TryParse(...)`
     **kulturabhängig** (`Program.cs:95-118`).
   * `Program.convertTxt2Double` → **InvariantCulture** (`:119-127`).
   * `Form_AdminStromspeicher.btn_Speichern_Click` → `double.Parse(text)` **ohne** Kultur (`:84-88`).
   * `StromganglinieStammCtrl.ImportGanglinie` / `StromganglinieDatenCtrl` → **InvariantCulture**.
   * `Form_Quellprofil` serialisiert Profile als `";"`-Strings mit **InvariantCulture** (`:63-111`).
   * Sprache/Kultur: `HKCU\Software\wp-plan`, Wert `Language` (0 = de) setzt nur
     **`Thread.CurrentThread.CurrentUICulture`**, nicht `CurrentCulture` (`Program.cs:51-62`) —
     die Zahlformatierung folgt also der Windows-Einstellung des Anwenders.
   → Regel fürs Konzept: **UI-Eingaben mit `CultureInfo.CurrentCulture`, Datei-/DB-Ein- und Ausgabe mit
   `CultureInfo.InvariantCulture`**, explizit getrennt.
6. **Modulfreischaltung über Bitmaske** `Program.startfrm.status` (`Form_Kosten.cs:47-53`):
   `0x1` Heizkessel, `0x2` Wärmepumpe, **`0x4` Stromspeicher**, `0x100` BHKW, `0x200` Solarthermie,
   `0x400` PV, `0x800` Pufferspeicher. Neu hinzugekommen (nicht in CLAUDE.md): Lizenzprüfung mit
   `BouncyCastle.Cryptography` 2.7.0 (Ed25519) + `System.Security.Cryptography.ProtectedData` (DPAPI).
   Das neue Modul muss die Maske respektieren.
7. **Lokalisierung:** sichtbare Texte über `MyResource.Resource.*` (+ `Resource.en-US`), Formulartexte
   über `X.de-DE.resx` / `X.en-US.resx`. `KONFIG_STROMSPEICHER` existiert bereits. Jeder neue Label-Text
   des Speichermoduls ist zweisprachig zu pflegen.
8. **Excel-Export:** `Microsoft.Office.Interop.Excel` ist per COMReference eingebunden und wird in
   `ToolsClass.ReadExcel` zellweise genutzt (mit `Application.DoEvents()` je Zeile) — für 35 040 Zeilen
   praktisch unbrauchbar. Für den Zeitreihenexport ist `CsvExportClass` zu verwenden
   (offener Punkt 1 des Konzepts damit faktisch entschieden: CSV).
9. **Namens-/Dateifallen:** `Controller/StromspeicherKontextMenuCtrl.cs` enthält die Klasse
   `SpKontextMenuCtrl` (und erbt unnötig von `Form`); `Controller/StromverbraucherCtrl .cs` hat ein
   **Leerzeichen** im Dateinamen; `Form_StromspeicherItemNeu.cs` enthält `Form_Sp_ItemNeu`.
10. **`Views/Simulation/Form_Quellprofil.cs` ist die fertige UI-Vorlage für das Kostenprofil**
    (Konzept 4.1 b): Reiter „Monatswerte" (12 Werte), „Wochenwerte" (7 × 24 mit Tag kopieren/einfügen/
    auf alle Tage übertragen), „Grafik" (8760-h-Vorschau); Persistenz als zwei `";"`-separierte
    Zeichenketten (12 bzw. 168 Werte) in DB-Spalten, Jahresprofil über
    `WaermequelleClass.ProfilAusMonatsUndWochenwerten(monat, woche)`. Vollständig programmatisch
    aufgebaut, ohne Designer/`.resx`.
11. **`StromTestClass.cs`** (`Allgemein/`) ist eine explizite Beispiel-/Anleitungsklasse für genau diese
    Aufgabe: `MyTestProfil(stromprofil)` (Profilrechnung), `MyTestLastgang(stromgang)` (Lastgang über
    `Z_ProjektStromganglinie` + Abfrage **`Abfrage_ProjektStromGanglinie`**, Rückgabe `float[8760]`),
    `StromspeicherDaten()` (Leistung/Energie/Ladezustand/Degradation über `StromspeicherCtrl.ReadSingle`).
    Sie enthält bereits die Felder `m_szStromspeicher` und `m_ID_Projekt` — offenkundig als Vorarbeit
    für dieses Modul angelegt.

---

# Konsequenzen fürs Konzept

Nummerierte Korrekturen und Bestätigungen, jeweils mit dem Konzeptabschnitt, der zu ändern ist.

## Abschnitt 3 (Datenquellen Lastgang und Erzeugung)

1. **Korrektur — „drei Optionen" streichen.** Es gibt **zwei** Lastgangquellen:
   (a) synthetisches Profil aus `Tab_Stromverbraucher(_STAMM)` (12 Monatswerte) × `Tab_Stromverbrauchertyp
   (_STAMM)` (168 Stundenspalten), expandiert über `WPPlan.Core.BhkwPlan.StromWocheToJahr` auf 8760 h;
   (b) importierte Stromganglinie aus `Tab_Stromganglinie(Daten)`. `TagV`/`Tab_DBTagV` gehört zum
   **Wärme**pfad (Gebäudetyp → `BhkwPlan.StdWerte`) und ist keine Stromprofil-Option. Ein SLP-Verfahren
   existiert nicht. Dritter Beitrag zum Strom-Lastgang sind die gerechneten Eigenverbräuche
   (WP, Heizstab, Kessel-Hilfsstrom) — die Formulierung sollte das als „Last = Profil/Lastgang +
   Anlagen-Eigenbedarf" abbilden.
2. **Korrektur — internes Raster.** Die Anwendung führt **beide** Raster parallel: 8 760 `float`-Werte
   (Physik, BHKW-Kernel) und 35 040 `float`-Werte (Strompfad, Charts), Umrechnung über
   `SimulationControl.Stundenwerte_zu_viertelstunden()`. Die Konzeptvorgabe „alles einheitlich
   `double[]` 15 min" bleibt richtig, muss aber um eine **Adapterschicht `float[8760] ↔ double[35040]`**
   ergänzt werden; ein reines „liefern sie Stundenwerte, gilt dieselbe Expansion" unterschätzt das.
   Einheiten explizit benennen: Leistungsreihen kW, `StromWocheToJahr`-Ausgabe Wh/h, Aggregate MWh.
3. **Bestätigung + Präzisierung — Lastgangimport existiert bereits.** `Form_Stromganglinie_Admin.
   btn_Einlesen_Click` + `StromganglinieStammCtrl.ImportGanglinie`. Der Konzeptabschnitt „(b) Import"
   ist von „neu bauen" auf **„vorhandenen Import erweitern"** umzuschreiben. Vorhanden: `.txt`, ein Wert
   je Zeile, InvariantCulture, Raster 1/4/60 pro Stunde (35 040 wird bereits unterstützt), Ablage in
   `Tab_StromganglinieDaten`, Projektkopie. Fehlend und zu ergänzen: CSV/Excel, Trennzeichen/Dezimalkomma,
   Zeitstempelspalte und -konvention, Einheit kW/kWh, **Schaltjahr (heute hart abgelehnt: nur 8 760 /
   35 040 / 525 600 zulässig)**, Lücken/Dubletten/Sommerzeit, Plausibilität, Validierungsprotokoll.
   Zusätzlich unterstützt der Bestand **Minutenwerte (525 600)** — im Konzept nicht vorgesehen,
   entweder aufnehmen oder bewusst ausschließen.
4. **Korrektur — PV-Quelle falsch benannt.** `SolarPVGISCalculator` liefert **Wetter/Einstrahlung**
   (PVGIS-TMY, 8760 h, W/m², plus Nominatim-Geokodierung, Ablage in `Tab_Klimadaten`/`Tab_Solar`),
   `PhotovoltaikCtrl` nur Modulstammdaten. Die Erzeugungsreihe kommt aus **`SimulationPV`**:
   `Stromproduktion` (8760), `Stromproduktion_viertelstunde` (35 040, kW), `Ueberschuss[_viertelstunde]`,
   `Strombedarf` (35 040), `Strombedarf_stuendlich`, `MaxPSolar`, `Modul_Ergebnisse`.
   **Und: `SimulationPV` führt bereits `Speicherfuellstand` / `Speicherfuellstand_viertelstunde` [kWh].**
5. **Korrektur — BHKW-Quelle falsch benannt.** `Allgemein/BhkwPlan.cs` ist der Port des nativen
   Rechenkerns `BHKWPLAN.DLL` (`namespace WPPlan.Core`, Vektor-/Physik-Primitive, `Hours = 8760`),
   **kein BHKW-Anlagenmodell**. Die el. Erzeugung liefert `SimulationBHKW.stromproduktion`
   (**stündlich, 8760**). Fahrweise ist bereits wählbar (wärmegeführt / stromgeführt / ohne
   Einspeisung) über `Tab_Einstellungen.Betriebsart` → `sim.modeBHKW`; das Konzept kann darauf aufsetzen
   statt es neu einzuführen. **Ein elektrischer BHKW-Überschuss existiert nirgends als Reihe** und ist
   im Vorverarbeitungsschritt selbst zu bilden (`max(0, P_bhkw[i] − P_last[i])`).

## Abschnitt 4 (Preis- und Vergütungsmodell)

6. **Bestätigung — der Fixpreis-Fall ist der Bestandsfall.** `energy_carrier` mit
   `pricing_model = 'ELECTRICITY'` führt genau einen skalaren Arbeitspreis in €/kWh
   (`ucFuelSettings`: „Direktabrechnung nach kWh"), versioniert über `energy_price.valid_from`.
   Die Konzeptaussage „Fixpreis ist die konstante Reihe" passt eins zu eins.
7. **Korrektur — es gibt keine Preisprofile im Bestand.** Quelle (b) „Kostenprofil analog den vorhandenen
   Verlaufsprofilen (`TagVCtrl`, `TagVModel`, `TagVDatenModel`)" ist falsch verankert: `TagV` ist ein
   **Wärme**-Tagesverteilungskatalog (`Tab_DBTagV`/`Tab_DBTagVDaten`, Feld `Verteilung`, Bezug über
   Gebäudetyp) und trägt keine Preise. Die tragfähige Vorlage ist stattdessen
   **`Views/Simulation/Form_Quellprofil.cs`** (12 Monatswerte + 7 × 24 Wochenwerte, Serialisierung als
   `";"`-String mit InvariantCulture, 8760-h-Vorschauchart) — Referenz im Konzept austauschen.
8. **Korrektur/Ergänzung — Aufschlagskomponenten sind vollständig neu.** Im Kostenmodul existieren nur
   `grundpreis`, `arbeitspreis`, `leistungspreis` (+ Heizwert/Einheit/Emissionen). Netzentgelt Arbeit,
   Umlagen, Stromsteuer, Konzessionsabgabe, Vertrieb gibt es weder als Feld noch als Tabelle. Der
   Aufschlagsblock (4.2) ist als **Erweiterung von `energy_project_settings`** je (`ID_Projekt`,
   Strom-Carrier) zu spezifizieren, mit Aktiv-Flags und Override; die Historie bleibt in `energy_price`.
9. **Ergänzung — Leistungspreis existiert, aber mit unklarer Einheit.** Prüfpunkt 7 aus Abschnitt 8 ist
   damit beantwortet: `energy_price.leistungspreis` / `energy_project_settings.custom_price_power` /
   `pricing_model.has_powerprice` / `Tab_Brennstoff_Stamm.Standard_Leistungspreis` sind vorhanden, das
   UI-Label lautet für Strom aber `€/kWh` (die Auslese-Property heißt `LeistungspreisEurYear`).
   Empfehlung fürs Konzept: **`L_P` als eigenes, explizit in €/(kW·a) deklariertes Feld** einführen und
   optional aus dem Kostenmodul vorbelegen — nicht das bestehende Feld umdeuten. Offener Punkt 2 des
   Konzepts ist damit entscheidungsreif.
10. **Ergänzung — Preisgültigkeit.** Der Bestand kennt Preisversionen (`valid_from`/`valid_to`). Das
    Konzept sollte festlegen, welche Preisversion eine Simulation zieht (Stichtag/aktuellste), damit
    Ergebnisse reproduzierbar bleiben.

## Abschnitt 5 (Speicherkonfiguration)

11. **Bestätigung — C_nom, P, d und ein Kostenfeld existieren.** `Tab_Stromspeicher(_STAMM).Energie`,
    `.Leistung`, `.Degradation`, `.Modulkosten`; genau **eine** Leistungsangabe, was die Entscheidung
    „eine gemeinsame Lade-/Entladeleistung" stützt. `Modulkosten` ist als c_cap-Kandidat zu verwenden,
    **Einheit ist aber nirgends deklariert** (Validierung sogar per `checkInt`) — im Konzept als
    Migrationsaufgabe „Modulkosten → c_cap [€/kWh] mit Einheitenklärung" aufnehmen.
12. **Korrektur — SoC-Band und Ladeleistung existieren bereits, aber projektweit.**
    `Tab_Einstellungen.Ladefuellstand_Min`, `Ladefuellstand_Max`, `Ladeleistung_Max` (je mit
    Einheiten-Auswahlfeld `*_Auswahl`) und `Ladeschwellwert`, gepflegt auf
    `tabPage_Stromspeicher_Parameter` in `Form_Simulation_Detail`. Die Parameterliste 5.1 darf diese
    nicht als „neu" führen; sie sind zu **migrieren** (projektweit → je Speichervariante) und die
    bestehenden Felder als Vorgabewerte umzuwidmen oder zu entfernen.
13. **Korrektur — zusätzliches Bestandsfeld `Ladezustand`.** Semantik unbestimmt (Start-SoC? nutzbarer
    Anteil?). Muss im Konzept explizit auf einen der neuen Parameter abgebildet oder als „deprecated"
    gekennzeichnet werden; es wird heute in `FormMain.SetSPControl` angezeigt.
14. **Bestätigung — der Rest der Parameterliste fehlt tatsächlich:** η_RT/η_ch/η_dis, N_zyk, c_ver,
    c_pow, I_fix, L_P, a_netzlade, i_z, N, Standby, Betriebskosten, Betriebsart, Quellen-Flags,
    Netzentladung, Berechnungsart, Preisquelle, Kompatibilitätsmodus, Variantenschlüssel/„aktiv".
15. **Ergänzung zu 5.5 (Anzeige und Absprung).** Die Übersichtsanzeige existiert bereits:
    `FormMain.listView_SP` mit den Spalten Name/Typ/Leistung/Energie/Degradation/Ladezustand, gespeist
    aus `Tab_Energieanlagen` (`SetSPControl`), Kontextmenü `SpKontextMenuCtrl` (Datei
    `StromspeicherKontextMenuCtrl.cs`), Absprung nach `Form_Stromspeicher` bzw.
    `Form_AdminStromspeicher`. Ertrag/Amortisation der letzten Rechnung sind dort als **neue Spalten**
    zu ergänzen; das Konzept nennt bisher fälschlich nur `NavigatorStrom`/`DashboardForm`.

## Abschnitt 8 (Integration) — und die dortigen Prüfpunkte 1-7

16. **Prüfpunkt 1 (Feldbestand `StromspeicherModel`) — beantwortet:** acht Felder, siehe Ziffer 11-14.
17. **Prüfpunkt 2 (Signatur/Zeitraster PV und BHKW) — beantwortet:** PV liefert 8760 **und** 35 040
    (`_viertelstunde`-Suffix), BHKW nur 8760; alles `float[]`, Leistungsreihen in kW.
    Umrechner `SimulationControl.Stundenwerte_zu_viertelstunden`.
18. **Prüfpunkt 3 (drei Stromprofil-Optionen) — beantwortet:** siehe Ziffer 1; Datenstrukturen
    `Tab_Stromverbraucher(typ)(_STAMM)` (Monat_1..12 / Spalten [1]..[168]) und
    `Tab_Stromganglinie(Daten)(_STAMM)` (Kopf + Wertzeilen, `Zeitinterval` 1/4/60).
19. **Prüfpunkt 4 (Konventionen `DataRepository` für Binärfelder / mehrfache Datensätze je Projekt) —
    beantwortet:** **Binärfelder werden nirgends verwendet**; `DataRepository` bietet keine
    BLOB-Unterstützung. Mehrfache Datensätze je Projekt laufen über `ID_Projekt` in der Kopie-Tabelle
    (Muster A) bzw. über mehrere `Tab_Energieanlagen`-Zeilen mit gleichem `ID_Type` (Muster C).
    Konsequenz: Ein Binär-/BLOB-Feld für Zeitreihen wäre ein **Bruch mit dem Hausmuster** und benötigt
    eine eigene Zugriffsschicht.
20. **Korrektur zur Persistenzempfehlung.** Die Aussage „von einer zeilenweisen Ablage wird abgeraten"
    ist zu relativieren: Genau das ist das Bestandsmuster (`Tab_StromganglinieDaten`,
    `Tab_SolarganglinieDaten`, `Tab_WaermebedarfDaten`, `Tab_Klimadaten`, `Tab_Solar`), inklusive
    35 040-Werte-Import und projektweiser Kopie in der Migration. Empfohlene Neuformulierung:
    * **Eingangs-Lastgang:** vorhandene Struktur `Tab_Stromganglinie(Daten)` + `Z_ProjektStromganglinie`
      **wiederverwenden** — keine neue Ablage.
    * **Ergebnisreihen (SoC, €/Intervall, Netzbezug vor/nach):** Datei neben dem Projekt oder
      komprimiertes Binärfeld, Metadaten/Prüfsumme in der DB. Begründung nicht „Access schafft das
      nicht", sondern: für Ergebnisse existiert **kein** Bestandsmuster (`Tab_Ergebnis*` speichert nur
      Skalare), die Migration limitiert über das ID-Schema `Projekt*1000000 + ID` auf < 10⁶ Zeilen je
      Projekt, und bei Varianten × Berechnungsarten vervielfacht sich das Volumen.
21. **Ergänzung — Schemaerweiterung `Tab_Einstellungen` nur am Ende.**
    `KonfigurationCtrl.ReadSingle` liest über Positionsindizes `row[0..22]`; neue Spalten müssen
    angehängt und die Indexliste erweitert werden. In den Migrationsabschnitt aufnehmen.
22. **Prüfpunkt 5 (Absprungmuster) — beantwortet:** `*KontextMenuCtrl.Init(ListView, ID_Projekt,
    Projektname)` → `ShowDialog()` → bei OK: Zuordnung löschen/neu schreiben (`WizardCtrl`),
    `ProjektCtrl.m_Aenderungsdatum = DateTime.Now; Update()`, `Program.mainfrm.SetXxxControl(...)`.
23. **Prüfpunkt 6 (ChartManager vs. ChartManagerNeu) — beantwortet:** **`ChartManager`**
    (WinForms.DataVisualization 1.10.2). `ChartManagerNeu.cs` ist vom Build ausgeschlossen.
    Fertige API inkl. `MitViertelStunde`, `MaxXVALUE`, `AddSeries(name, Color, float[])` und
    Sekundärachsen-Muster für den Speicherfüllstand. Ergänzender Hinweis fürs Konzept: für den
    35 040-Punkte-Jahresgang mit Zoom ist das ebenfalls referenzierte **ScottPlot 5.1.57** die
    technisch bessere, im Bestand aber noch ungenutzte Option — bewusst entscheiden.
24. **Prüfpunkt 7 (Leistungspreis im Kostenmodul) — beantwortet:** ja, aber mit Einheitenproblem,
    siehe Ziffer 9.
25. **Ergänzung — Andockpunkte präzisieren.** Statt der bisherigen Vermutungsliste gelten:
    Lastgang → `StromganglinieStammCtrl` / `Z_ProjektStromganglinieCtrl` / Abfrage
    `Abfrage_ProjektStromGanglinie` (Vorlage: `StromTestClass.MyTestLastgang`);
    Profil → `SimulationStrombedarf.Stromprofil_Strombedarf_berechnen` (Vorlage:
    `StromTestClass.MyTestProfil`); PV → `SimulationPV`; BHKW → `SimulationBHKW`;
    Preise → `ucFuelSettings` / `energy_*`-Tabellen; Anzeige → `tabPage_Stromspeicher` und
    `tabPage_Stromspeicher_Parameter` in `Form_Simulation_Detail` (beide **existieren bereits**),
    Navigation über `listViewQuellen` / `TabListMapper`; Kennzahlen → `ErgebnisCtrl`/`ErgebnisModel`
    (**`Sim_Stromspeicher` ist bereits vorhanden**, Detailmodell/-tabelle fehlt); Export →
    `CsvExportClass.Export` mit `CsvSpalte`.
26. **Neu — bestehende Speicherlogik ablösen, nicht ignorieren.** Das Konzept muss einen Abschnitt
    „Ablösung des Bestands" erhalten: (a) `SimulationSSP` (Flag `sim.bSimulationSSP`),
    (b) `SimulationPV.Speicherfuellstand[_viertelstunde]` samt Chart-Serie „Speicherfüllstand" und
    `checkBox_Speicherzustand`, (c) die davon unabhängige, stündliche, verlustfreie Speicherrechnung in
    `DashboardForm.UpdateSimulationData()`/`FillMonthlyChart()` (nur Kapazität, keine Leistungsgrenze,
    kein SoC-Band, 730-h-Pseudomonate). Ohne diese Ablösung existieren nach der Umsetzung **drei**
    Speichermodelle mit unterschiedlichen Ergebnissen im selben Programm.
27. **Neu — Engine-Randbedingungen.** Die UI-freie `SpeicherEngine` darf weder `DataRepository`
    (MessageBox im Fehlerfall) noch `Program.*`-Statics noch `WPPlan.Core.BhkwPlan` (globaler Zustand
    `_prevRoomTemp`) benutzen; Ein-/Ausgabe ausschließlich über übergebene Arrays. Das ist die
    Voraussetzung für `Parallel.For` in der Rastersuche (Konzept 6.3) und für die Referenztests.
28. **Neu — Abnahmekriterien präzisieren (Kulturen).** Punkt (ii) der Abnahmekriterien ist zu teilen:
    Datei-/DB-Ein-/Ausgabe `CultureInfo.InvariantCulture`, UI-Eingabefelder `CultureInfo.CurrentCulture`
    (deutscher Anwender tippt „0,25"). Der Bestand ist an dieser Stelle inkonsistent
    (`Program.checkDouble` kulturabhängig vs. `Program.convertTxt2Double` invariant vs.
    `Form_AdminStromspeicher` ohne Kulturangabe).
29. **Neu — Modulfreischaltung.** Das Stromspeicher-Modul hängt an `Program.startfrm.status & 0x4`
    (Kostenmodul-Komponente „Stromspeicher", `KomponentenID = 5`) und an
    `Tab_Einstellungen.Tool_6 == "Stromspeicher"`. Beide Schalter sind in den Etappenplan aufzunehmen,
    sonst bleiben die neuen Seiten unsichtbar. Zusätzlich Lizenzprüfung (Ed25519/DPAPI) beachten.
30. **Neu — offener Punkt 1 (Excel-Export) faktisch entschieden.** Excel läuft nur über COM-Interop
    (`ToolsClass.ReadExcel`, zellweise) und ist für 35 040 Zeilen untauglich; `CsvExportClass` ist das
    etablierte, viertelstundenfähige Exportmuster. Empfehlung: Kennzahlen und Variantenvergleich als
    CSV/Excel, Intervallzeitreihen ausschließlich als CSV.

## Abschnitt 7.3 / 9 (Variantenvergleich, Mehrspeicherbetrieb)

31. **Bestätigung + Konkretisierung.** Mehrere Speicher je Projekt sind strukturell **bereits möglich**:
    `Tab_Energieanlagen` mit `ID_Type = SP_TYP` (mehrere Zeilen) bzw. `REF_SP_TYP` für die
    Referenzvariante — die Anwendung kennt also schon eine Referenz-/Planvarianten-Trennung
    (`FormMain.SetSPControl`, `SpKontextMenuCtrl`). Der Variantenvergleich ist darauf zu setzen:
    je Variante eine `Tab_Energieanlagen`-Zeile (`ID_SP` → Projektkopie in `Tab_Stromspeicher`) plus
    variantenspezifische Parameter in einer neuen 1:1-Tabelle `Tab_StromspeicherVariante`
    (`ID_Energieanlage` als Schlüssel, „aktiv"-Kennzeichen). Die Konzeptformulierung „die Tabelle um
    einen Variantenschlüssel je Projekt plus ein aktiv-Kennzeichen erweitern" (bezogen auf
    `Tab_Stromspeicher`) ist entsprechend zu ersetzen — sonst kollidiert sie mit der
    `CopyFromStamm`-Dublettenprüfung auf (`Bezeichner`, `ID_Projekt`).
