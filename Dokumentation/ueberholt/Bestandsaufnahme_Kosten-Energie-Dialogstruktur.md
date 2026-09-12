# EPOS-Plan — Bestandsaufnahme Kosten, Energiedaten, Dialogstruktur

**Stand:** 19.08.2026 · Arbeitskopie `C:\Users\DirkEngelmann\Documents\WP-Plan` (main, origin-gleich bis auf lokale KI-Etappe-2-Dateien) · erhoben mit drei parallelen Analyse-Agenten (~230 gezielte Dateizugriffe).
**Pfadbasis:** `WindowsFormsApplication1\`, sofern nicht anders angegeben. Parallelstände (`mit_Puffer_KI_Lösungsversuch\`, `Tempkib2\`, `* - Kopie`, `*.bak`) bewusst ausgeklammert.

> Aktualisierung zu `Grundlagen_4_WP-Plan_Repo-Analyse.md` (29.07.): Das Hauptprojekt steht inzwischen auf **net8.0-windows** (x86, `Platforms x86;x64;AnyCPU`), nicht mehr .NET Framework 4.8. Die dortigen Grundbefunde gelten weiter: zwei Datenzugriffsschichten (ODBC-DSN „TEST" mit String-SQL vs. `Allgemein\DataRepository.cs` mit OLE DB und `?`-Parametern), Rechenkern `bhkwplan.dll` über Out-of-Proc-COM (`CSExeCOMServer`), keine Projektdatei — Kataloge **und** Projektdaten in `Kenndaten.accdb`.

---

## 0. Kurzfassung

1. **Kosten liegen in zwei getrennten Datenwelten:** deutsche `Tab_*`-Kostenpositionen (Kategorien 1=Investition, 2=Betrieb, 3=Energie) und englische, stichtagsversionierte `energy_*`-Preistabellen. Die Wirtschaftlichkeit zieht Energiekosten **nicht** aus Kategorie 3, sondern aus `energy_*` über den `KostenEmissionRechner` (Entscheidung 11.08.2026 „keine Doppelpflege", `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:21-23`).
2. **Gerechnet wird nach Kapitalwertmethode (DIN EN 17463 / ValERI)**, nicht nach VDI-2067-Annuität. Von VDI 2067 stammen nur Kostengliederung und der 12-Positionen-Betriebskostenkatalog. Das Stammprojekt ist die Unterlassensalternative; Kennzahlen entstehen aus der Differenz-Zahlungsreihe.
3. **`Tab_Energieanlagen` hat 57 Spalten** (56 im zentralen INSERT + Autowert `ID`); gelesen wird ausschließlich namensbasiert.
4. **Energieträger laufen über eine Doppelachse:** `Tab_Brennstoff_Stamm` (deutsch: Hi/Hs/CO₂/SO₂/NOx/Staub/PE_Faktor) ↔ `energy_carrier` (englisch: Preise, Einheiten, pricing_model), verknüpft über `id_brennstoff`; Projektüberschreibungen in `energy_project_settings`, Effektivwerte über die Access-Abfrage `Abfrage_Energietraeger_Effektiv`.
5. **EPOS-Plan kennt keine Messdatenhaltung** — keine Zählerstände, keine Ablesungen; alle Energiemengen sind simuliert. Ergebnis-Zeitreihen werden bewusst nicht persistiert, alle `Tab_Ergebnis*` führen nur Skalare.
6. **Die Oberfläche hat drei Navigationsebenen:** MenuStrip (`MDIMainForm`, trotz Namens kein MDI mehr) → eingebettete Kachel-Startseite `Form_Start` (6 Reiter) → modaler Assistent `WizardParent`. Daneben ein Altzweig `FormMain` (modale Komponentenansicht mit Kontextmenüs).
7. **Kosten/Wirtschaftlichkeit hängen unter Reiter 6 „Berichte && Kosten"** (4 Seiten: Übersicht, Kosten, Wirtschaftlichkeit, Bericht); eine eigene „Energiebilanz"-Maske gibt es nicht — Energiedaten verteilen sich auf Simulations-Ergebnisreiter, Bericht und Wirtschaftlichkeitsseite.
8. **Mehrere doppelte Wahrheiten:** Schema-DDL doppelt (WirtschaftlichkeitCtrl legt eigene Tabellen an, SchemaMigration teils ebenfalls), Brennstoff-ID→Zähler-Zuordnung doppelt (Simulation + gespiegelt in der Anzeige), Optik-Konstanten dreifach.

---

## 1. Technik-Steckbrief (aktualisiert)

| Aspekt | Befund |
|---|---|
| Framework | `net8.0-windows`, C#, WinForms; `PlatformTarget x86` (`WindowsFormsApplication1.csproj`) |
| Solution | `WP-Plan.sln`: WindowsFormsApplication1, SpeicherEngine(+Tests), KiKern(+Tests); daneben nicht eingebundene Ordner (CSExeCOMServer, KiHarnisch, …) |
| Datenhaltung | MS Access `Kenndaten.accdb` (Anwender-DB nicht im Repo; im Repo: `Kenndaten-ok.accdb` veraltet, `Referenzlaeufe\Arbeitskopie\Kenndaten.accdb` Stand 17.08.); Kataloge + Projektdaten in einer Datei; Migration ausschließlich über `Allgemein\Update\SchemaKatalog.cs` + `SchemaMigration.cs` — das in `Grundlagen_4` beschriebene `UpdateDB.ini`-Verfahren existiert im aktiven Repo **nicht mehr** (kein Code liest `.ini`/`.sql`; `IniFileParser.cs` ist verwaist); `migration.manuell.sql` ist ein manuell auszuführendes Skript |
| Datenzugriff | alt: `RecordSet` über ODBC-DSN „TEST", String-SQL (~50 Dateien) · neu: `DataRepository` über ACE-OLE-DB, `?`-Parameter (~135 Dateien) |
| Rechenkern | `bhkwplan.dll` (nativ) über `CSExeCOMServer`; feste Raster 8760 h / 35040 ¼h / 168 Wochen-h / 365 d / 12 Monate |
| Sprachen | Drei-Schichten-Regel: deutsche eingefrorene Persistenzwerte (`Allgemein\DbWerte.cs`) · sprachneutrale Schlüssel · Anzeige nur über `MyResource\Resource.*` |

---

## 2. Kostendaten

### 2.1 Grundbefund: zwei Datenwelten

- **Kostenpositionen** (Investition/Betrieb) — deutsche `Tab_*`-Welt, Kategorien 1/2/3 (`Tab_KostenKategorie`).
- **Energiepreise** — englische `energy_*`-Welt (snake_case), stichtagsversioniert (`energy_price.valid_from/valid_to`).
- Treffpunkt: `Allgemein/Bericht/KostenEmissionRechner.cs` liefert der Wirtschaftlichkeit die Energiekosten aus `energy_*`; Kategorie 3 in `Tab_ProjektWerte` wird von der Kostenmaske gepflegt, von `WirtschaftlichkeitCtrl` aber **nicht gelesen** (nur Kategorie 1 und 2 — `WirtschaftlichkeitCtrl.cs:2359-2381` bzw. `:2413-2466`).

### 2.2 Datenhaltung

**Kostenpositionen (Projektdaten):**

| Tabelle | Rolle | Beleg |
|---|---|---|
| `Tab_ProjektWerte` | zentrale Kostentabelle, eine Zeile je Position | `Allgemein/Update/SchemaKatalog.cs:646` |
| `Tab_KostenKategorie` | 1=Investition, 2=Betrieb, 3=Energie | `Views/Kosten/Form_Kosten.cs:20-22` |
| `Tab_KostenKomponente` | 7 Komponenten: WP, Kessel, BHKW, PV, Solar, Stromspeicher, Pufferspeicher | `Controller/TechnikPlanwertCtrl.cs:159-165` |
| `Tab_Kostenfaktor` | Katalog: `Bezeichnung`, `StammID`, `IsMainComponent` | `Controller/KostenPositionCtrl.cs:157-201` |
| `Tab_KostenGruppenKatalog` | freie Gruppierung („Lern"-Katalog) | `Controller/KostenPositionCtrl.cs:202-231` |
| `Abfrage_Kostenfaktoren`, `Abfrage_ProjektKostenInvestBetrieb`, `Abfrage_KostenKomponenten` | gespeicherte Access-Abfragen (Definition in der .accdb, nicht im Repo) | `Views/Kosten/Form_Kosten.cs:659-666`, `Controller/KostenPositionCtrl.cs:199` |

Spalten `Tab_ProjektWerte`: `ID, ProjektID, StammID, KomponentenID, KategorieID, Gruppe, Einheit` (immer `"€"`), Szenariospalten `EingegebenerWert/BestCase/WorstCase`, Nutzungsdauern (×3); seit Migrationsschritt 19 zusätzlich `Kostenart` TEXT(20), `Bemessung` TEXT(30), `IstErloes` YESNO, `Menge` DOUBLE, `Einheitpreis` DOUBLE (`SchemaKatalog.cs:725-738`). INSERT `Form_Kosten.cs:889-903`, UPDATE `:1002-1020`, zweiter Schreibweg `KostenPositionCtrl.cs:285-317`.

**Wirtschaftlichkeits-Tabellen** — DDL führt das Modul selbst in `WirtschaftlichkeitCtrl.StelleTabellenSicher()` (`:133-344`; bewusste doppelte Wahrheit gegenüber `SchemaMigration`, Kommentar `:290-295`):

| Tabelle | Inhalt (Auszug) |
|---|---|
| `Tab_ProjektWirtschaftlichkeit` | `Zinssatz, Betrachtungszeitraum, Preissteigerung_Energie/_Betrieb, Einspeiseverguetung, CO2_Preis, KWKG_*`; + Schritt 20: `Unternehmensart, Raeumlicher_Zusammenhang, Hocheffizienz_Nachweis, Jahresnutzungsgrad, Energiesteuer_Wahl, Aufteilung_Methode` (`SchemaKatalog.cs:828-836`) |
| `Tab_ErgebnisWirtschaftlichkeit` | `Investition, Betriebskosten, Energiekosten, Einspeiseerloes, BarwertAusgaben/-Einnahmen, Restwert, Kapitalwert, KapitalwertDiff, AnnuitaetKW, AmortisationJahre, Gestehungskosten, IRR, CO2Abgabe, KWKGErloes, Fehlgrund`; nachgerüstet u. a. `StromkostenTarif, EnergiesteuerErloes, StromsteuerBefreiung/-Entlastung, RefKessel_*` (`:279-335`) |
| `Tab_ErgebnisWirtSensitivitaet` | `Parameter, KwMinus, KwBasis, KwPlus` |
| `Tab_ProjektTarif` | HT/NT × Winter/Sommer, Bezug + Einspeisung, Leistungspreis-Staffel |
| `Tab_ErgebnisStromMatrix` | Strommengen je Tarifzone |
| `Tab_KWKG_Staffel` | degressive Vbh-Staffel, Seed `WirtschaftlichkeitCtrl.cs:246-248` |

**Energiepreise (`energy_*`):**

| Tabelle | Kostenspalten | Beleg |
|---|---|---|
| `energy_price` | `arbeitspreis, grundpreis, leistungspreis, Heizwert, arbeitspreis_unit, valid_from/valid_to` | `Views/Kosten/Form_Kosten.cs:1457-1466`, `ucFuelSettings.cs:362-400` |
| `energy_project_settings` | `custom_price_work/_base/_power, custom_hi, custom_Hs, co2, so2, nox` + Strom-Aufschläge (Schritt 12: `Aufschlag_Netzentgelt/_Umlagen/_Stromsteuer/_Konzession/_Vertrieb` je `_Aktiv`, `Aufschlag_Modus/_Override`, `Verguetung_PV/_BHKW`, `SchemaKatalog.cs:421-495`) | `Form_Kosten.cs:1471-1487` |
| `energy_carrier` | `price_work, price_base, co2, id_brennstoff, …` (vollständige Spalten s. § 3.2) | `Allgemein/Bericht/KostenEmissionRechner.cs:199-204` |
| `Abfrage_Energietraeger_Effektiv` | `eff_hi, eff_hs, billing_unit` (Projektwert vor Katalogwert) | `WirtschaftlichkeitCtrl.cs:1493` |

**Preiszeitreihen (Speicher-Simulation):** `Tab_Preisreihe`, `Tab_PreisreiheDaten`, `Tab_Kostenprofil` — DDL `Allgemein/Update/SchemaMigration.cs:2036/:2056/:2083`; Verweise an der Speichervariante `ID_Preisreihe, ID_Kostenprofil, Aufschlag_Anwenden` (`SchemaKatalog.cs:512-524`).

**Kostenfelder an Technik-Tabellen** (eine Leseschicht: `Controller/TechnikPlanwertCtrl.cs:159-165`, Feldzuordnung `:300-355`):

| Gewerk | Tabelle | Kostenfelder |
|---|---|---|
| BHKW | `Tab_BHKW` | `Kosten_Modul`, `Investition_kwel`×`Pel`, `Kosten_Montage/_Lieferung/_Schallschutzhaube/_Abgasreinigung`, `Wartungskosten_kwhel` |
| Heizkessel | `Tab_Heizkessel(_STAMM)` | `Investitionskosten`, `Wartungskosten` + `Wartungskosten_Einheit` |
| Wärmepumpe | `Tab_WP` | `Modulkosten` |
| PV | `Tab_PV` | `Modulkosten` × Modulanzahl |
| Solarthermie | `Tab_Solarkollektoren` | `Investitionskosten` × Modulanzahl |
| Stromspeicher | `Tab_Stromspeicher(_STAMM)` | `Modulkosten` [€/kWh]×`Energie` + `Leistungskosten` [€/kW]×`Leistung` + `Investition_Fix` |
| Pufferspeicher | `Tab_Pufferspeicher` | `Investitionskosten` |

**Gesetzeskatalog:** `Tab_Gesetzesparameter` (`Schluessel, Klasse, JahrVon, Wert, Einheit, Status, Quelle` — `Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs:388-397`, Seeds ab `:461`): KWKG-Zuschläge, Strom-/Energiesteuersätze, CO₂-Preispfad, Emissionsfaktoren, Umsatzsteuer.

### 2.3 Modellklassen

Zentral `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs`: `WirtschaftlichkeitParameter` (`:22-155`), `WirtschaftlichkeitErgebnis` (`:294-374`, durchgängig `double?` — „null = nicht bestimmbar, nie 0"), `SensitivitaetZeile`, `TarifParameter`, `WirtschaftlichkeitSzenario` (Erwartet/Best/Worst), `IWirtschaftlichkeitProvider` (`:394-410`), `VerlaufSerie`, `EmissionsBilanz`, `Kraftwerkspark`.
Weitere: `KapitalwertRechner.InvestPosition/Zahlungsbild/ErloesReihe` (`KapitalwertRechner.cs:59-124`), UI-Modell `KostenPosition` (`Views/Kosten/ucKostenItem.cs:199-247`), `BetriebskostenCtrl.Position` (VDI-2067-Katalog, `:70-114`), `TechnikPlanwertCtrl.Basiswert/Nebenposten/Anlage` (`:74-95`), `PreisreiheModel`, `KostenprofilModel`, `StromAufschlagModel`, `StromPreisCtrl.StromPreisErgebnis` (`:13-72`), `SteuerGutschriftRechner.SteuerAnlage` (`:12-60`).
Persistenzkonstanten in `Allgemein/DbWerte.cs`: `KOSTENART_*` (`:225-247`), `BEMESSUNG_*` (`:274-310`), `VDI_POS_*` (12 Positionen, `:413-478`), `KOSTEN_EINHEIT_EURO` (`:129`), `UNTERNEHMENSART_*`, `ENERGIESTEUER_WAHL_*`, `SP_AUFSCHLAG_MODUS_*`, `PREISREIHE_*`.

### 2.4 Berechnung

| Verfahren | Norm/Basis | Fundstelle |
|---|---|---|
| Kapitalwertmethode (Leitverfahren) | **DIN EN 17463 / ValERI** | `KapitalwertRechner.cs:7-11`, `Rechne()` `:146-216` |
| Annuität | abgeleitete Kennzahl `AnnuitaetKW = KapitalwertDiff × a(i,T)` | `:127-133` |
| Interner Zinsfuß | Bisektion über Differenzreihe | `:224-252` |
| Amortisation | dynamisch, mit Jahres-Interpolation | `:260-282` |
| Kostengliederung + 12 Betriebskostenpositionen | VDI 2067 (nur Gliederung/Katalog, **keine** Annuitätenmethode Blatt 1) | `DbWerte.cs:205-247`, `BetriebskostenCtrl.cs:118-235` |
| KWKG 2025, EnergieStG §53/§53a, StromStG §9/§9b, BEHG | Gesetze | `GesetzKatalog.cs`, `SteuerGutschriftRechner.cs` (616 Z.) |

Orchestrierung `WirtschaftlichkeitCtrl.cs` (2847 Z.): `LadeParameter :375` → `Berechne :765-817` (3 Szenarien × alle Varianten; Investitionen = Kategorie 1 `:2359`, Betriebskosten = Kategorie 2 `:2413`, Energie über `KostenEmissionRechner`) → Sensitivität ±1 pp Zins / ±1 pp p_E / ±10 % Invest / ±10 % Energie (`:124-127`, `:809`) → `Persistiere :2502-2560`. KWKG-Prüfungen `Foerderbeginn :1271`, `AusschreibungsgrenzeKW :1846`, Heizöl-Guard `:2175`. Kapitalwert-Verlauf `:832`.
Ergänzende Rechner: `KostenEmissionRechner.cs:33` (Energiekosten p. a. + CO₂; Preiskette Projektwert → Katalog → Fallback `:183-204`), `BetriebskostenCtrl.Betrag() :261-292` (eine Formel je Bemessungsart; Erlöse auf negatives Vorzeichen geklemmt), `StromMatrix.cs` (HT/NT), `StromPreisCtrl.cs:340-410` (**einzige** Stelle für p_bezug der Speichersimulation; Fallback-Kette `energy_price` → `custom_price_work` → `energy_carrier.price_work` → Platzhalter), `EmissionsBilanzRechner.cs` (gekoppelt vs. getrennt).

### 2.5 Vorgabewerte und Pflegemasken

Defaults (Auszug): Zins **3,0 %**, Betrachtungszeitraum **20 a**, Preissteigerungen 0 %/a, Einspeisevergütung/CO₂-Preis/KWKG-Bonus 0 (= aus), KWKG-Kontingent 30 000 h, Referenzkessel η 90 % / Brennstoff-ID 3 (Erdgas E), Strommix-CO₂-Fallback 380 g/kWh, Nutzungsdauer < 1 a → wie T (`WirtschaftlichkeitDaten.cs:25-100`, `KapitalwertRechner.cs:171`, `KostenEmissionRechner.cs:36`). Muster: **kein DDL-DEFAULT auf Fachwerten** — Vorbelegung per DML-Migrationsschritt (`SchemaKatalog.cs:463-473`, `:709-715`).

Masken: `Form_Kosten` (1604 Z., 3 Reiter Investition/Wartung/Energie), `Form_Betriebskosten` (VDI 2067, Satz/netto/brutto), `Form_WirtschaftlichkeitParameter`, `Form_Tarifstruktur`, `Form_Gesetzesparameter`, `Form_KostenAdmin`/`Form_KostenfaktorItem` (Katalogpflege), `Form_PlanwertUebernahme`, `Form_CaseEingabe` (Best/Worst), `Form_Kostenprofil`, `Form_SpotpreisImport`, `ucFuelSettings`/`ucStromAufschlaege`.

### 2.6 Anzeige und Export

Anzeige: `Views/BerichteKosten/UcBerichteKosten` (4 Seiten), `UcBkKosten` (683 Z., inkl. Abweichung Planwert ↔ erfasst), `Views/Wirtschaftlichkeit/UcWirtschaftlichkeit` (637 Z.), `Form_WirtschaftlichkeitVerlauf`.
Export: **Excel** (ClosedXML, Blatt „Wirtschaftlichkeit", `ExcelBerichtGenerator.cs:210-286`), **Word** (OpenXML, `BausteineWirtschaftlichkeit.cs` + Vorlage `Vorlagen/Berichtsvorlage.docx`), Dateinamen `BerichtCtrl.cs:36/69`. **Kein PDF, kein CSV-Export von Kosten.** CSV-Import Spotpreise → `Tab_PreisreiheDaten` (`SpotpreisLeser.cs`).
KI-Schnittstelle: lesend `Allgemein/KI/Aktionen/KiAktionenWirtschaft.cs`; schreibend `KiAktionenSchreiben.cs:289-460` (`kostenposition_setzen`, über `KiSchreibschutz`).
Konsistenzzusage: Reiter, Word und Excel lesen **dieselben persistierten** Ergebnisse aus `Tab_ErgebnisWirtschaftlichkeit` (`WirtschaftlichkeitDaten.cs:389-393`).

---

## 3. Energiedaten

### 3.1 `Tab_Energieanlagen` — 57 Spalten

Eine Wahrheit über die Spaltenliste: `SQL_ANLAGE_INSERT` mit 56 benannten Spalten + Autowert `ID` (`Controller/WizardCtrl.cs:159`; Kommentar „21 der 57 Spalten" `Controller/WErzeugerCtrl.cs:92`). Beide Einfügewege nutzen dieselbe Anweisung; gelesen wird namensbasiert über `WErzeugerCtrl.AusZeile` (`:174-243`).

| Block | Spalten |
|---|---|
| Bestand (29) | `ID_Projekt, Bezeichner, ID_Type, ID_WP, Betriebsart, Sperrung, Sperrzeit_von/bis, Vorlauf, Rücklauf, Bivalenter_Betrieb, Abschaltpunkt, Nutzungszeit, ID_SP, ID_PV, ID_Solar, Heizstab, Volumen, rendeMix, Solaranteil, ID_Kessel, ID_BHKW, Grenzleistung, Kollektormodulanzahl, PV_Leistung, Neigung, Azimut, ID_PUFFER, **ID_Carrier**` |
| Kaskade (2) | `Prioritaet, BM_Typ` |
| Wärmequelle (15) | `WQ_Typ, WQ_Temp, WQ_Monatswerte, WQ_Wochenwerte, WQ_CSV, WQ_Puffer, WQ_ID_Puffer, WQ_Spreizung, WQ_Regeneration, WQ_Unbegrenzt, WQ_Tiefe, WQ_Flaeche, WQ_Anzahl, WQ_Bodentyp, WQ_Quellsystem` |
| Wärmesenke (10) | `WS_Typ, WS_Ziel, WS_ID_Puffer, WS_Ladeprio, WS_Ladegrenze, WS_Ladeprio_PV, WS_Ziel2, WS_ID_Puffer2, WS_Ladeprio2, WS_Ladegrenze2` |

`ID_Carrier` ist NULL-treu modelliert (NULL ≠ 0, `Model/WErzeugerModel.cs:50-71`), bewusst **ohne** erzwungene Beziehung auf `energy_carrier.id` (`SchemaKatalog.cs:243-263`). Typ-Dispatch über `Tab_Typ_Energieanlagen` (`ID_Type`).

### 3.2 Energieträger: die Doppelachse

```
Tab_BrennstoffKategorien.ID   (Gruppe: Gas, Öl, …)
        ▲ ID_Kategorie
Tab_Brennstoff_Stamm.ID  ── Einheit, PreisEinheit, Hi, Hs, CO2, SO2, NOx, Staub,
        ▲ id_brennstoff     PE_Faktor, Standard_Grund-/Arbeits-/Leistungspreis, ReadOnly
        │                   ▲ Tab_BHKW.Brennstoff / Tab_Heizkessel.Brennstoff
energy_carrier.id ── code, name, group_code, pricing_model, billing_unit,
        ▲ ID_Energieträger  hi_/hs_kwh_per_unit, price_work, price_base, co2, is_active
        │                   ▲ Tab_Energieanlagen.ID_Carrier
energy_project_settings  (custom_hi/hs, custom_price_*, co2/so2/nox, Aufschläge)
        └─► Abfrage_Energietraeger_Effektiv  (eff_hi, eff_hs, billing_unit)
energy_price  (valid_from/valid_to, arbeitspreis, grundpreis, leistungspreis, Heizwert)
```

Dazu: `pricing_model` (`code, has_hi, has_hs, has_powerprice`; Werte `GAS/FUEL/GRID/ELECTRICITY`, `Form_Kosten.cs:1300/:1576`), `energy_conversion` (`id_brennstoff, from_unit, to_unit, factor, user_edited` — `migration.manuell.sql:498`, `ucFuelSettings.cs:477/:528/:539`), `energy_unit` (nur in der Duplizier-Ausschlussliste `ProjektDuplizierenCtrl.cs:48`, kein Lesecode — vermutlich tot), `Tab_Brennstoff_Projekt` (Altweg, nur noch `migration.manuell.sql:488`).
Anlage → Träger beim Lauf: `SimulationControl.cs:530-531` / `:669` (`EnergietraegerZuordnungLesen`, NULL-tolerant); Ergebnis-Modul → Träger: `ErgebnisCtrl.cs:1366-1395` (Vorrang: Projektträger, sonst erster Katalogträger des Brennstoffs, sonst 0). Trägerauflösung der Wirtschaftlichkeit mit Altweg-Fallback: `WirtschaftlichkeitCtrl.cs:1985-2070`.

### 3.3 Brennstoff-ID → Verbrauchszähler

Einzige explizite Aufzählung: Kesselbilanz `Allgemein/Simulation/SimulationSPK.cs:264-286`; gleiche Kaskade beim BHKW (`SimulationBHKW.cs:379-401`); für die Anzeige **gespiegelt** in `Form_Simulation_Detail.cs:923-930` (`_kesselBrennstoffIds`, dort als offener Punkt markiert).

| `Tab_Brennstoff_Stamm.ID` | Zähler |
|---|---|
| 1–5, 14 (Biogas) | `Gasverbrauch_SPK` |
| 6–9, 18–22 | `Oelverbrauch_SPK` |
| 10 / 11 / 12 | `Koks_SPK` / `Kohle_SPK` / `Holzverbrauch_SPK` |
| 13 (Elektrowärme) | `Stromverbrauch_Spk` |
| 15 / 16 / 17 | `Pellets_SPK` / `Rapsoelverbrauch_SPK` / `TierischeFette_SPK` |
| 23 (Fernwärme), 24 (Sonstige), 25 (Wasserstoff), künftige | `Sonstigverbrauch_SPK` (else-Sammelposten) |

### 3.4 Heizwerte, Umrechnung, CO₂-Kette

- **Heizwerte:** Katalog-Default `Tab_Brennstoff_Stamm.Hi/Hs` bzw. `energy_carrier.hi_/hs_kwh_per_unit` (kWh je Abrechnungseinheit); Projektwert `energy_project_settings.custom_hi/custom_hs`; Historie `energy_price.Heizwert`; Effektivwert (Projekt vor Katalog) `Abfrage_Energietraeger_Effektiv.eff_hi/eff_hs`.
- **Einheiten-Umrechnung:** `energy_conversion(from_unit, to_unit, factor)`; Kernformel Menge `Views/Varianten/EnergieMengen.cs:63-79`: `Menge = Verbrauch[MWh] × 1000 / eff_hi[kWh je Einheit]`.
- **CO₂-Vorrangkette** (`KostenEmissionRechner.cs:19-27`, `:183-230`): ① Projektwert `energy_project_settings.co2` → ② Katalog `Tab_Brennstoff_Stamm.CO2` (über `energy_carrier.id_brennstoff`) → ③ `energy_carrier.co2` → ④ Konstante 380 g/kWh (nur Netzbezug). Einheit g/kWh (= kg/MWh). Achtung: `energy_carrier`-Kopien tragen fast durchweg `co2 = 0` — Kette über `Tab_Brennstoff_Stamm` ist zwingend (`Allgemein/Bericht/LIESMICH_Phase1.md:226-229`).
- Emissionsfaktoren **Nachweis** (GEG Anlage 9) und **Bilanz** (UBA/EBeV/BAFA) sind strikt getrennte Schlüsselgruppen in `Tab_Gesetzesparameter` (`DbWerte.cs:1030-1101`).

### 3.5 Bedarfs-, Ganglinien- und Ergebnistabellen

- **Bedarf/Ganglinien:** `Tab_Waermebedarf(+Daten)(_STAMM)`, `Tab_Stromganglinie(+Daten)(_STAMM)`, `Tab_Solarganglinie(+Daten)(_STAMM)`, `Tab_Stromverbraucher(+typ)`, `Tab_Brauchwasser(+typ)`, `Tab_Prozesswaerme`, `Tab_DBTagV(+Daten)`, `Tab_Klimadaten/Klimaregion/Solar`. Feste Raster 8760/35040/168/365/12.
- **Ergebnis:** `Tab_Ergebnis` (Kopf) + `Tab_ErgebnisEnergiebedarf`, `…Waermepumpe(+Modul)`, `…BHKW(+Modul)`, `…Heizkessel(+Modul)`, `…Solarthermie(+Modul)`, `…Photovoltaik(+Modul)`, `…Pufferspeicher`, `…Stromspeicher` (`Controller/ErgebnisCtrl.cs:20-33`); dazu `Tab_ErgebnisStromMatrix`, `Tab_ProjektTarif`. **Nur Skalare — Zeitreihen werden bewusst nicht persistiert** (`Model/ErgebnisModel.cs:271-276`).
- Jahresbilanz-Modelle in `Model/ErgebnisModel.cs`: u. a. `ErgebnisEnergiebedarfModel` (`Waermebedarf_Gesamt, Strombedarf_Gesamt, Waermerestbedarf, Stromrestbedarf` = Netzbezug), `ErgebnisBHKWModel` (`VbhElektrisch`, 9 Brennstoffzähler), `ErgebnisHeizkesselModel(+Modul)` (`Verbrauch`, `CarrierId`, `Jahresnutzungsgrad`), `ErgebnisStromspeicherModel` (`:282-346`: Ladung PV/BHKW/Netz, Entladung, Verluste, Netzbezug/Einspeisung mit-ohne, Eigenverbrauchsquote, Autarkiegrad, Vollzyklen, SoC, Erträge, NPV).

### 3.6 Simulationskaskade und Bilanzaufbau

Zentral `Allgemein/Simulation/SimulationControl.cs`:

```
:487-490  Start: Restwaerme = 0; Reststrom = Strombedarf_gesamt;
                 Rest_Waermebedarf_stuendlich = Waermebedarf.Clone()
:530-531  Energieträger-Zuordnung je Anlage lesen (ID_Carrier)
:534-537  KaskadeZweikanalig → Kaskadenschleife (Phasen A–G, Kaskadenschleife.cs:6-48)
:546-598  sonst Altpfad tool[0..3]: WP (:550) → Kessel (:569) → Solar (:582); BHKW-Zweig entfallen
:602-608  PV viertelstündlich von Rest_Strombedarf abziehen
:627-635  Stromspeicher: nur die ENTLADUNG mindert den Bezug (Ladung mindert Einspeisung)
:638/:642 Umrechnung in MWh
```

Bedarf: `SimulationWaermebedarf` (8760), `SimulationStrombedarf` (35040). Erzeuger-Module: `SimulationWaermepumpe/SPK/BHKW/Solarthermie/PV/Pufferspeicher`; Kessel-Teilbilanz `SimulationSPK.Bilanz_und_Nutzungsgrad :253-320` (Brennstoffzähler, `Em_CO2_SPK += Verbrauch_MWh × CO2_SPK[i]`).
Stromspeicher: `SimulationControl.cs:3589` → `StromspeicherSimCtrl.cs` (`BaueEingang :738`, Lastreihe `:840`, PV `:878`, BHKW `:899`, Rückabbildung `AlsErgebnismodell :1385`) → Rechenkern im Projekt `SpeicherEngine\` (`Dauernutzung, PeakShaving, Arbitrage, Wirtschaftlichkeit, Amortisation`).
Bilanzaufbau (eine Quelle der Wahrheit): `SimulationRunner.BaueErgebnis :197` (Energiebedarf `:222-228`, WP `:231-367`, BHKW `:370 ff.`, …), Persistenz `SimuliereUndSpeichere :783` → `ErgebnisCtrl`.

### 3.7 Import/Export

Import: Lastgang/Ganglinie CSV/TXT/Excel (`Allgemein/Import/GanglinienDatei.cs:15-45`: Trennzeichen, Dezimaltrenner, Kopfzeile, Wert-/Zeitspalte, Einheit, Raster, Intervallkonvention, Blattname; Prüfung `SpeicherEngine/GanglinienPruefung.cs`), Spotpreise (`SpotpreisLeser.cs:75`), VDI 3805 (`Allgemein/Import/VDI 3805/`), PV-Module CEC/PAN.
Export: CSV-Ganglinien 8760/35040 (`CsvExportClass.cs:27-42`; Aufrufer u. a. `Form_Simulation_Detail.cs:1088/:1799/:6553`, `NavigatorStrom.cs:128`, `Form_PeakShaving.cs:885`), Excel-/Word-Bericht (s. § 2.6), `ZeitreihenExtraktor.cs:18` (Schlüssel `BerichtsDaten.cs:119-146`). Referenzläufe als Regressionssuite: `Referenzlaeufe\2026-08-19_B5\Projekt_1030\*.csv`.
Kennzahlen: `KennzahlenKatalog.cs:36` Gruppe „Energiebilanz" (`:74-106`), Brennstoffsumme `:50-62`, Kostengruppe `GR_KOSTEN` (`:39/:157-160`).

---

## 4. Dialogstruktur

### 4.1 Einstieg und Navigationsebenen

`Program.Main()` (`Program.cs:32`) → Sprachwahl aus `HKCU\Software\wp-plan` (`:45-61`), `KiTextlieferant.Einrichten` (`:66`), `Form_KiHinweis.Einhaengen` (`:71`), Schema-Migration (`:83`), globale `MenueCtrl`/`WizardCtrl` (`:97-98`), `WordPressHelpCatalog` (`:112`) → `Application.Run(mdifrm)` (`:118-119`). Prozessweite Statics als „Fensterregister": `mdifrm, mainfrm, startfrm, menuectrl, wizardctrl` (`:15-19`).

| Ebene | Träger | Art |
|---|---|---|
| 1 | `MDIMainForm` | MenuStrip: **Projekte · Administration · Help · Deutsch · Englisch**; trotz Namens **kein** MDI (`IsMdiContainer = false`, `MDIMainForm.cs:20`) |
| 2 | `Form_Start`, eingebettet `TopLevel=false`/`Dock=Fill` (`MDIMainForm.cs:372-380`) | TabControl, 6 Reiter mit Kacheln; Reiter 2–6 bis zur Projektwahl gesperrt (`Form_Start.cs:56`, frei in `ProjektKontextUebernehmen :153`) |
| 3 | `WizardParent` (`Views\Wizard\`) | modaler Assistent; Seiten sind Forms mit `TopLevel=false` |

Reiter: `Projekt · Wärmebedarf · Strombedarf · Energieerzeuger · Simulation · Berichte && Kosten`.

Menübaum (Kurzform): **Projekte** (Neu/Öffnen/Bearbeiten/zuletzt/Löschen) · **Administration** (Wärmebedarf und Heizung; Strombedarf und Speicher inkl. programmatischem Peak-Shaving `MDIMainForm.cs:85-105`; Energiesysteme; Klima; Datenimport inkl. Kosten/Kosten-Admin `:608-625`; Gebäude; Einstellungen mit Lizenz `:291-318` und Gesetzesparametern `:50-75`) · **Help** (Version/Lizenz/Doku/KI-Chat F1 `:222-282`) · Sprachumschaltung über Registry + `Application.Restart()` (`:501-522`).

### 4.2 Dialoghierarchie (Kernketten)

```
MDIMainForm
├─ Menü Projekte → MenueCtrl.ProjektNeu/-Bearbeiten [MenueCtrl.cs:26/54]
│    └─ WizardParent (modal) — Seitenfolge (WizardItemClass.cs:11-24):
│       0 Wizard_Komponenten · 1 Wizard_Projekt · 2 Form_Gebaeude · 3 Form_Waermebedarf
│       4 Form_Prozesswaerme · 5 Form_Stromverbraucher · 6 Wizard_Stromlastgang
│       7 Form_WPAuswahl · 8 Form_SolarKollektoren · 9 Form_PV · 10 Form_Stromspeicher
│       11 Form_Heizkessel · 12 Form_BHKWEing   (13 PUFFER_ITEM definiert, unbestückt)
├─ MenueCtrl.ProjektOeffnen [MenueCtrl.cs:81]
│    └─ Form_ProjektSpeichernUnter → FormMain (modal!, :130/:178) — Altzweig:
│       TabControl „Komponenten", 12 ListViews, je ein *KontextMenuCtrl
├─ Menü Administration → Stammdaten-Admin-Dialoge je Gewerk (Form_*_Admin, Form_*_einlesen,
│       Form_Sp_ItemNeu als generischer „Neu"-Dialog), Form_Kosten, Form_KostenAdmin,
│       Form_AdminSettings, Form_LizenzVerwaltung, Form_Gesetzesparameter, Form_Klimadaten
├─ Help → Form_Lizenz · Form_KiChat (nicht-modal, F1; Einwilligung Form_KiHinweis)
└─ Form_Start (eingebettet)
   ├─ Reiter Projekt      → ProjektNeu/-Bearbeiten/-Oeffnen/-Delete, Form_ProjektSpeichernUnter
   ├─ Reiter Wärmebedarf  → Form_Gebaeude → Form_Gebaeude1 → Form_Gebaeude2 → Form_Brauchwasser;
   │                        Form_Waermebedarf; Form_Prozesswaerme; Form_Brauchwasser
   ├─ Reiter Strombedarf  → Form_Stromverbraucher; Form_EingStromTyp; Form_Stromganglinie
   ├─ Reiter Energieerzeuger → Form_WPAuswahl (→ Wizard_WPItem); Form_Heizkessel; Form_BHKWEing;
   │                        Form_SolarKollektoren(+Solarganglinie); Form_PV; Form_Stromspeicher;
   │                        Form_PufferSp
   ├─ Reiter Simulation   → Form_Simulation_Config (Karten-/Schemaansicht; Quellen/Senken-Dialoge:
   │                        Form_Waermesenke, Form_QuellePufferspeicher, Form_Quellprofil,
   │                        Form_QuelleErdreich, Form_KonfigPufferspeicher, Form_PufferSp_Projekt)
   │                        · Form_Simulation_Detail (11 Reiter; Ergebnis → TabNavigationManager:
   │                          NavigatorUebersicht · DashboardForm (TopLevel=false) · NavigatorWaerme
   │                          · NavigatorStrom; ferner Form_SpeicherOptimierung,
   │                          Form_SpeicherVariantenVergleich)
   └─ Reiter Berichte&&Kosten → UcBerichteKosten (senkrechte ListView-Navigation, 4 Seiten):
        Übersicht → UcBkUebersicht (→ Form_BkUebernahme)
        Kosten    → UcBkKosten (→ Form_Kosten)
        Wirtschaftlichkeit → UcWirtschaftlichkeit (→ Form_Tarifstruktur,
                              Form_WirtschaftlichkeitParameter, Form_WirtschaftlichkeitVerlauf)
        Bericht   → UcBericht (Word/Excel-Erzeugung)

Form_Kosten (Reiter Investition | Wartung | Energie; KategorieID = SelectedIndex+1)
   ├─ Form_SpotpreisImport [:142] · Form_Kostenprofil [:181] · Form_KostenfaktorItem [:863]
   ├─ Form_PlanwertUebernahme [:1108] · Form_Betriebskosten [:1148] · Form_Kosten_Auswahl [:1393]
   ├─ ucKostenZeile [:465] → Form_CaseEingabe
   └─ ucFuelSettings [:1264] → ucStromAufschlaege
```

### 4.3 Gemeinsame Muster

- **Basisklasse:** nur `Allgemein\BaseForm.cs` (AutoScale/AutoScroll/`FensterEinpassung`), von genau 7 Formularen geerbt; Rest direkt `Form`. Anomalie: 9 Kontextmenü-Controller erben von `Form`, werden aber nie angezeigt (z. B. `Controller\WPKontextMenuCtrl.cs:10`).
- **Keine Datenbindung:** `BindingSource` nur in `Form_CECImport.cs`. Stattdessen Controller je Gewerk (`ReadAll/ReadSingle/Update/Delete`), öffentliche Listenfelder, die der Aufrufer vor `ShowDialog()` füllt und nach `DialogResult.OK` zurückliest (Musterbeispiel `Form_Start.cs:217-254`).
- **Konventionen:** `SetControls(...)` vor dem Anzeigen (61 Dateien); `btn_OK_Click`/`btn_Abbrechen_Click` (92/73 Dateien), der Aufrufer schreibt — nicht der Dialog; neu und erst zweimal: nicht schließender Speichern-Knopf `SpeichernLeiste` (BHKW-/Heizkessel-Admin).
- **Validierung** zentral in `Program.cs`, zwei Generationen: alt `checkInt` (`:186`), neu Färben beim Tippen + Melden beim Knopf (`ZahlFaerben :244`, `ZahlPruefen :272`); Parsing komma-/punkttolerant (`ZahlParsen :206`).
- **Mehrsprachigkeit** Drei-Schichten-Regel (`WindowsFormsApplication1\CLAUDE.md:70-78`); `MyResource.Resource.*` in 97 Dateien.
- **Programmatische Erweiterungen** statt Designer-Änderungen sind Hausregel (`MDIMainForm.cs:47-48`, `Form_Start.cs:2003-2010`, `SpeichernLeiste.cs:19-27`).
- **Optik:** dunkles „WordPress-Admin"-Menü dreifach identisch dupliziert (`Form_Simulation_Detail.cs:151-158`, `TabListMapper.cs:43-51`, `UcBerichteKosten.cs:44-51`).

### 4.4 Einhängepunkte Kosten/Wirtschaftlichkeit und Energie/Bilanz

**Kosten** — drei Einstiege auf denselben Dialog `Form_Kosten(idProjekt)`: ① Menü Administration → Datenimport → Kosten (`MDIMainForm.cs:608-619`), ② Reiter 6 → Seite „Kosten" → `UcBkKosten` „Verwaltung" (`UcBkKosten.cs:590-599`) — **heutiger Hauptweg**, arbeitet auf der markierten Zeile (Stamm oder Variante), ③ `Form_Start.btn_Kosten_Click` (`Form_Start.cs:1899-1906`) — **toter Weg** (Knopf wird in `BaueBerichteKostenSeite :2018-2019` entfernt).
**Wirtschaftlichkeit** — ausschließlich Reiter 6, Seite 3 (`UcBerichteKosten.cs:218-230` → `UcWirtschaftlichkeit(_idStamm)`). `Form_Wirtschaftlichkeit` ist nur noch Hülle (`:19-42`); einziger Aufrufer der abgelöste `Form_Variantentest.cs:353`.
**Energiedaten/Bilanz** — kein eigener Dialog. Drei Orte: ① Simulationsergebnis (`Form_Simulation_Detail`, Reiter „Ergebnis" → `TabNavigationManager.cs:104-196`), ② Brennstoffmengen nur im Bericht (`EnergieMengen.BaueBrennstoffmengen`, Aufrufer `BerichtsDatenSammler.cs:417`, `ProjektvergleichBericht.cs:693`), ③ Emissionsbilanz auf der Wirtschaftlichkeitsseite (`UcWirtschaftlichkeit.cs:49`).

---

## 5. Querbezüge: die durchgehende Kette

```
Tab_Energieanlagen.ID_Carrier ─► energy_carrier ─► Tab_Brennstoff_Stamm (Hi/Hs/CO₂/Kategorie)
      │                                                    │
      ▼ Simulation (SimulationControl → Module)            ▼ Brennstoff-ID→Zähler (SimulationSPK/BHKW)
Tab_Ergebnis* (nur Skalare, je Modul CarrierId/Verbrauch)
      │
      ▼ KostenEmissionRechner  (Preise: energy_price → custom_* → energy_carrier; CO₂-Kette § 3.4)
      ▼ WirtschaftlichkeitCtrl (Kat. 1+2 aus Tab_ProjektWerte; Energie aus energy_*; Steuern/KWKG)
Tab_ErgebnisWirtschaftlichkeit ─► UcWirtschaftlichkeit · Word · Excel  (eine persistierte Wahrheit)
```

- **Sprachgrenze als Altersindikator:** deutsche `Tab_*`-Namen = gewachsener Kern; englische snake_case-Namen (`energy_*`, `pricing_model`) = neuere Preis-/Trägerschicht. Die Naht (`id_brennstoff`, `ID_Carrier`) ist bewusst FK-los.
- **Kategorie 3 („Energie") in `Tab_ProjektWerte`** wird von `Form_Kosten` gepflegt, aber von keiner Rechnung gelesen — Doppelpflege wurde am 11.08.2026 zugunsten `energy_*` entschieden; der Eingabepfad existiert noch.
- **Mehrfache Wahrheiten:** Schema (WirtschaftlichkeitCtrl-Selbst-DDL vs. `SchemaMigration`, als bekannt vermerkt `WirtschaftlichkeitCtrl.cs:290-295`, `SchemaKatalog.cs:921-927`) · Brennstoff-ID-Zuordnung (Simulation vs. Anzeige-Spiegel) · Optik-Konstanten (3×).
- **Abgrenzung EDL-G-Werkzeug:** Die Excel-Vorlage „EDLG-Daten_und_Maßnahmenplan" (Aktionsplan-Ordner der EPOS-Downloads) erfasst **gemessene** Verbräuche/Kosten je Energieträger inkl. Tankablesung — EPOS-Plan hat bewusst keine Messdatenhaltung; eine Anbindung wäre ein eigenes Vorhaben.

---

## 6. Verifizierte Fehlanzeigen (repoweit gesucht, nicht vorhanden)

1. **Kein PDF-Export** — Berichte nur `.docx`/`.xlsx`.
2. **Keine Förderung/Zuschüsse** (BAFA/KfW-Logik fehlt; „Förder*" nur im KWKG-Kontext). Investitionszuschuss derzeit nur als negative Position/`IstErloes` abbildbar.
3. **Keine VDI-2067-Annuitätenmethode** (keine preisdynamischen Barwertfaktoren, keine Jahresgesamtkosten je Kostenart).
4. **Keine Finanzierungsrechnung** — kein Fremdkapitalzins, keine Tilgung, keine AfA; nur ein Kalkulationszins. **Netto-Rechnung** (USt nur zur Brutto-Anzeige im Betriebskosten-Dialog).
5. **Kein CSV-Export von Kosten**; keine Exportfunktion direkt aus den Kostenmasken.
6. **Keine Zählerstände/Ablesungen/Verbrauchszähler-Verwaltung** — alle Mengen simuliert.
7. **Kein `Tab_Bilanz`, kein `Tab_Energietraeger`** (Träger in `energy_carrier` + `Tab_Brennstoff_Stamm`).
8. **Keine Kraftstoffe** (Diesel/Benzin) im Katalog; **kein VDI-4655-Import** (nur Analysedokumente).
9. **Keine Primärenergiebilanz** — `PE_Faktor` existiert als Spalte ohne lesenden Code; nur PEF-Schlüssel im Gesetzeskatalog (vorbereitet, nicht implementiert).
10. **Kein Ribbon, kein TreeView, praktisch keine `BindingSource`-Datenbindung.**
11. **Keine Preissteigerung auf Investitionen** (Ersatzbeschaffung nominal, ausdrückliche W1-Vereinfachung `KapitalwertRechner.cs:14-16`).

---

## 7. Tote Enden und Auffälligkeiten

| Befund | Beleg |
|---|---|
| Toter Kosten-Knopf auf `Form_Start` (wird zur Laufzeit entfernt) | `Form_Start.cs:1899-1906`, `:2018-2019` |
| `Form_Wirtschaftlichkeit` nur Hülle; einziger Aufrufer ist der abgelöste `Form_Variantentest` | `Form_Wirtschaftlichkeit.cs:19-42`, `Form_Variantentest.cs:353` |
| `Form_AlsVariante` ohne Aufrufer | `Form_AlsVariante.cs:56` |
| `WizardItemClass.PUFFER_ITEM = 13` definiert, im Wizard unbestückt | `WizardItemClass.cs:24` |
| `MenueCtrl.PVImport()` leer — Menüpunkt ohne Wirkung | `MenueCtrl.cs:380-383` |
| `energy_unit` nur in Duplizier-Ausschlussliste | `ProjektDuplizierenCtrl.cs:48` |
| `Tab_Brennstoff_Projekt` Altweg, kein C#-Lesecode gefunden | `migration.manuell.sql:488` |
| `PE_Faktor` ohne Leser | `migration.manuell.sql:69` |
| Kachel-Handler `pBox_Optimierung_Click` leer | `Form_Start.cs:1533` |
| `ucKategorieHeader`, `SectionPanel`, `RoundedPanel`, `Form3Src`, `Form_ChartZoom` ohne Verwendung | — |
| `Form_Simulation_Kurz`, `Form_Simulation_Detail - Kopie` vom Build ausgeschlossen | `WindowsFormsApplication1.csproj:61-70` |
| `FormMain` (modale Komponentenansicht) wirkt wie MDI-Altbestand; Verhältnis zu `Program.mainfrm` unklar | `MenueCtrl.cs:94-178`, `:18-24` |
| Doku-Widerspruch: `Allgemein/Reporting/W4_Umsetzungsstand.md` führt Etappe E4 (Steuergutschriften) als „offen", der Code enthält sie | `SteuerGutschriftRechner.cs`, Migrationsschritt 20 |

---

## 8. Offene Punkte / Unsicherheiten

1. **`Kenndaten.accdb` nicht lesbar** (nicht im Repo): Alle Schema-Aussagen aus C#-DDL, SQL-Literalen, `SchemaKatalog`/`SchemaMigration`, `migration.manuell.sql` rekonstruiert. Insbesondere die **gespeicherten Access-Abfragen** (`Abfrage_Kostenfaktoren`, `Abfrage_KostenKomponenten`, `Abfrage_ProjektKostenInvestBetrieb`, `Abfrage_Energietraeger_Effektiv`, …) sind nur über Nutzungsstellen belegt.
2. **Spaltenlisten unvollständig** für `Tab_Kostenfaktor`, `Tab_KostenKategorie`, `Tab_KostenKomponente`, `Tab_KostenGruppenKatalog`, `Tab_ErgebnisStromMatrix`, `Tab_ProjektTarif` (Code liest nur Teilmengen).
3. **Brennstoff-ID-Namen** (welche Bezeichnung zu ID 1–25 gehört) stehen in der DB; belegt sind nur Bereichszuordnung, Erdgas E = ID 3, Kategorie „Öl" = 2.
4. **Welche Schema-Fassung in Bestands-DBs steht** (Selbst-DDL vs. Migration) ist ohne DB-Zugriff nicht bestimmbar.
5. **Kategorie 3 in `Tab_ProjektWerte`:** gewollte Redundanz oder tote Eingabe — nicht abschließend geklärt.
6. 93 von 372 `.cs`-Dateien sind nicht UTF-8 (`WindowsFormsApplication1/CLAUDE.md:97`) — Zeichenwiedergabe umlauthaltiger Bezeichner kann abweichen; beim Editieren cp1252-Falle beachten.
7. `Form_Simulation_Detail` (6 200 Z., 11 Reiter + verschachtelte TabControls) wurde nicht Seite für Seite gesichtet; Laufzeit-Ein-/Ausblendung über `UpdateTabPages()` offen.
8. Kachelmengen je Startreiter aus Click-Handlern abgeleitet, nicht aus dem Designer-Layout.
