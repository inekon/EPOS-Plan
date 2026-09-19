# Befund B — der heutige Rechenweg des Heizwärmebedarfs im Kern (15.09.2026)

**Protokoll.** Befund eines Erkundungs-Agenten (Modell Opus, nur lesend) im Auftrag des Konzepts
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Sitzung vom 15.09.2026, Codestand des Zweigs `ios_migration_september`. Wortlaut wie
geliefert; einzelne Zeilenbelege sind im Konzept nach dem Gegenlesen korrigiert
(`BhkwPlan.cs`: Heizlastformel `:418`, Solarfaktor `:420/:428`, Kappung `:430`, Rückgabe
`:435`, Gewichte `:347-353`; `Gebaeudebauweise.cs:66`; `SolarPVGISCalculator.cs:455` für
Hay-Davies) — bei Widerspruch gilt das Konzept.

---

## 1. Heutiger Rechenweg Heizwärmebedarf

**Die eine Klasse ist `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs`.** Einstieg ist `Waermebedarf_berechnen(ID_Projekt, ID_Klimaregion)` (`:128`). Ihr Ablauf:

1. `KlimakalenderLesen(ID_Klimaregion)` (`:175`, Methode `:513-535`) — liest über `KlimadatenCtrl` die **365 Tageswerte** `Sol_Nord/Ost/Sued/West`, `Temperatur`, `WE`, `TagTyp_W`, `TagTyp_NW`, dazu über `Stundentemperatur_aus_DB` (`:912-920`) die **8 760 Außentemperaturen** aus `Tab_Solar` im Ortszeit-Lesepfad (`SolardatenCtrl.ReadOrtszeit`). Am Ende `WochentagJan1 = ProfilBedarf.WochentagJan1AusWE(WE)`.
2. Gebäudeschleife über `ProjektGebaeudeCtrl.ReadAll(idProjekt)` (`:177-214`), je Gebäude `HeizwaermeEinesGebaeudes(item, i, puffer)` (`:566-611`).
3. `WattToKw` auf den Heizkanal (`:222`), Jahressumme `Waermebedarf_Gebaeude_Gesamt` (`:228`).
4. Externe Wärmelastgänge aus `Z_ProjektWaermebedarf` (`:231-312`), Rasterprüfung 8 760 / 35 040, Kanalzuordnung über `Z_ProjektWaermebedarf.Kanal`.
5. `Prozesswaerme_berechnen()` (`:324`, Methode `:935-962`), `Brauchwasserwaerme_berechnen()` (`:334`, `:994-1012`).
6. Netzverluste (`:344-375`), anteilige Verteilung auf die drei Kanäle, Summenvektor, Dauerlinie, Energieprobe (`:385-411`).

**Das Verfahren ist weder Gradtagzahl noch VDI 4655 noch BDEW.** Es ist ein **Tageslast-Modell mit instationärem Ein-Kapazitäten-Ansatz (RC), stündlich innerhalb des Tages**, portiert aus `BHKWPLAN.DLL`:

- `HeizwaermeEinesGebaeudes` ruft `Berechnung_Gebaeude_Tageswerte` (`:684-888`). Dort werden je Tag drei Physikfunktionen aus `EPOS.Kern/Allgemein/BhkwPlan.cs` gerufen:
  - `SolareGewinneC` (`BhkwPlan.cs:311-318`) — `(E_N·A_N + ((E_O+E_W)/2)·A_OW + E_S·A_S)·g·100`; **es gibt nur EINE Ost/West-Fensterfläche**.
  - `SpezWaermeverlusteC` (`:342-362`) — Transmission `0,83·U_W·A_W + U_F·A_F + 0,95·U_D·A_D + 0,45·U_G·A_G + U_S·A_S`, Wärmebrücken `(Ψ₁L₁+Ψ₂L₂+Ψ₃L₃)·0,83`, Lüftung `f·(A_Wohn·h)·1,2·n·0,2777…` mit `f = 1 + 0,025·θ_a` für θ_a < 0.
  - `TaeglHeizlastWG` (`:388-436`) — 24-Stunden-Schleife mit Sollwertfahrplan (Tag 7–22 h, sonst Nacht; WE- und Ferienabsenkung), Heizlast `(T_prev − θ_a)·L + (T_soll − T_prev)·C − Q_i`, Solarentlastung `4·Q_sol` nur in den Stunden 9–14, Fortschreibung `T = e^(−L/C)·T_prev + (1−e^(−L/C))·(…)/L`, Kappung auf `Maximaleraumtemperatur`. **`C` ist `item.Bauweise`** — die einzige thermische Masse; `_prevRoomTemp` ist ein **statischer, über Tage hinweg fortgeschriebener Zustand**, deshalb der Einschwingvorlauf über die Tage 350…364 (`SimulationWaermebedarf.cs:748-814`) vor dem Jahreslauf (`:818-886`).
- Die Tageslast wird über ein **24-h-Tagesprofil je Tagtyp** auf Stunden verteilt: `DBTagesVeteilung` (`:658-682`, Sicht `Abfrage_Tagverteilung`) liefert bis zu 8 × 24 Werte; `BhkwPlan.StdWerte` (`BhkwPlan.cs:259-289`) normiert je Typ auf Summe 1 und multipliziert. Tagtypwahl: `TagTyp_W` bei `Typ == "Wohngebaeude  VDI 2067"`, sonst `TagTyp_NW` (`:601-608`).
- **Einheiteneinstieg:** Gebäudereihe entsteht in **Watt**, wird einmal nach kW gebracht; Zeitreihen führen kWh, Jahressummen MWh (`EPOS.Kern/CLAUDE.md`, Abschnitt „Einheiten").

**Eingangsgrößen** (alle aus `ProjektGebaeudeModel`): U-Werte Außenwand/Fenster/Dach/Grund/Sonstiges, die zugehörigen Flächen, drei Wärmebrücken-Ψ mit Längen, `Wohnflaeche`, `Raumhoehe`, `Luftwechselrate`, `Bauweise` (Wärmekapazität), `Interne_Waermegewinne`, Fensterflächen S/O-W/N mit `Fensterdurchlassgrad`, vier Raumsolltemperaturen, `Maximaleraumtemperatur`, Ferienzeiten, `Z_AuswahlWohnflaeche`/`Einheit` (Skalierung, siehe `Bewohner_und_Flaeche_berechnen` `:613-656`: Rückrechnung der Fläche aus Öl-/Gas-/MWh-Verbrauch). **Keine Heizgrenztemperatur, keine Normaußentemperatur, keine Gradtagzahl** — im ganzen Kern nicht vorhanden.

**Aufrufkette:** `SimulationRunner.Simuliere_Intern` (`SimulationRunner.cs:123`) → Konfiguration/Klimaregion lesen (`:144-170`) → `simulation_Waermebedarf.Waermebedarf_berechnen` (`:184`) → `SimulationStrombedarf.Berechnung` (`:191`) → `sim.Stundentemperatur = …` und `sim.Do_Simulation` (`:207-217`). Derselbe Weg dialogseitig in `SimulationLaufCtrl.Bedarf` (`:114-127`), `Bestuecken` (`:142-155`), `Laufen`, `ErgebnisSpeichern` (`:210`). Die Erzeugerdeckung läuft zweikanalig über `SimulationControl.Do_Simulation_Intern` (`SimulationControl.cs:389`) mit `Kaskadenschleife` und `Ladeordnung`; die Bedarfsreihe kommt als `Kanalsatz` herein (`SimulationKanaele.cs:426-472`, `Kanal.HEIZUNG/BRAUCHWASSER/PROZESS`).

**Ein Andockmuster besteht bereits:** `GebaeudeBedarfCtrl.Rechnen` (`EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs:94-136`) ruft für EIN Gebäude dieselben zwei Methoden wie der Lauf — Regel „Eine Auskunft ruft den Rechenweg des Laufs" (`EPOS.Kern/CLAUDE.md`).

## 2. Datenmodell Gebäude/Projekt

- **`Tab_Gebaeude`** (`sql/schema/001_grundschema.sql:1131-1188`, STRICT): die Projektkopie. Sie führt **Hüllflächen** (`Flaeche_Außenwand`, `gesamte_Fensterflaeche`, `Dachflaeche`, `Grundflaeche`, `Sonstige_Flaechen`), **U-Werte** (`k_Wert_*`), **Wärmebrücken** (`WBVK_*` + `Abmessung_*`), **Luftwechsel** (`Luftwechselrate`), **innere Lasten** (`Interne_Waermegewinne`), **thermische Masse** (`Bauweise`), **Fensterflächen nach Orientierung** — aber nur **Süd, Nord und ein gemeinsames `Fensterflaeche_Ost_West`**; keine Neigung, keine freien Azimute, kein Verschattungsgrad, keine getrennten Bauteil-Kapazitäten. Dazu `Baualtersklasse`, `Gebaeudeart`, `Wohngebaeude_Nicht_Wohngebaeude`, `Typ`, `Wohnflaeche_gesamt`, `Raumhoehe`, `WW_Bedarf`, `spez_Waermeverbrauch`, `Waermebedarf`. **Kein Baujahr als Zahl** — nur die Klasse.
- **`Tab_Gebaeude_STAMM`** (`:1190…`) ist der Katalog mit `Bezeichner` statt `Gebaeudename`; Kopie über `GebaeudeStammCtrl.CopyFromStamm` (`:439`).
- **`Z_ProjektGebaeude`** (`:2873-2881`): `Wohnflaeche_Waermebedarf`, `Einheit_Waermebedarf_Wohnflaeche`, `Jahresnutzungsgrad`, `dezWarmwasserbereitung`. `Tab_Gebaeude.ID_ProjektGebaeude` zeigt darauf.
- **`Tab_Projekt`** (`:1494-1504`) führt nur `ID_Klimaregion` und den Emissionsmodus — **keine Gebäudedaten**. **`energy_project_settings`** (`:99-136`) ist ausschließlich Energieträger/Preise/Emissionen.
- Tagesprofile: `Tab_DBTagV` / `Tab_DBTagVDaten` (`:641-670`), Sicht `Abfrage_Tagverteilung` (`002_views.sql:109-112`).
- Externe Wärmelastgänge: `Tab_Waermebedarf`/`Tab_WaermebedarfDaten`, Zuordnung `Z_ProjektWaermebedarf` mit Spalte `Kanal` (`:2916-2925`).

**Modellklassen und Controller:** `EPOS.Kern/Model/ProjektGebaeudeModel.cs` (Lauf-DTO, 57 Felder), `Model/GebaeudeModel.cs` (Katalog/Projektzeile), `Model/Z_ProjGebModel.cs`. Lesen: `Controller/ProjektGebaeudeCtrl.cs:26-104` über die Sicht `Abfrage_Projektgebaeude` (`002_views.sql:89-91`), `Controller/GebaeudeCtrl.cs` (namensbasiert), `Controller/Z_ProjGebCtrl.cs:35`; Katalogpflege `Controller/GebaeudeStammCtrl.cs` (`Insert :406`, `Overwrite :418`, `CopyFromStamm :439`), Bauart↔Bauweise in `Allgemein/Gebaeudebauweise.cs:22-67` (`Bauweise = Wohnfläche × 20/50/100`).

> **Falle für einen Schemaschritt:** `ProjektGebaeudeCtrl.ReadAll` liest **nach Spaltenindex** `row[0]…row[57]`; `Tab_Gebaeude.ID` steht als letzte Spalte der Sicht auf Index 57. Neue Spalten müssen **hinter** `Tab_Gebaeude.ID` in die Sicht — oder der Leser wird auf Namen umgestellt (wie `GebaeudeCtrl.MapRowToModel`). Sonst verschiebt sich die ganze Zuordnung still. (Ergänzung aus dem Gegenlesen: die Sicht hat eine feste Spaltenliste; ohne Neubau der Sicht erreicht keine neue Spalte irgendeinen Leser — beides ist nötig.)

**Letzter Schemaschritt:** `SchemaStand.Zielversion = 76` (`EPOS.Kern/Allgemein/Update/SchemaStand.cs:93`; `FREEZE_VERSION = 61` für den Access-Zweig). Schritt 76 = „ein Satz je Energieträger und Projekt" (`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs:2816` Konstante, `:3720-3730` Eintrag in `SCHRITTE`, `:4961` Methode `Schritt_76_TraegersatzEindeutig`, Anweisungen in `EPOS.Kern/Allgemein/Update/ProjektEnergietraegerEindeutig.cs`). **Reihenfolge für einen neuen Schritt 77:** Schrittkonstante → Methode → `SCHRITTE`-Eintrag → `SchemaStand.Zielversion` hochsetzen; Spaltendefinitionen gehören in `EPOS.Kern/Allgemein/Update/SchemaKatalog.cs` (Muster `Schritt70_*`, `Schritt71_*`, `Schritt72_*`). Rahmen: `Dokumentation/aktuell/ADR-001_Schema-Ausrollung.md`.

## 3. Klimadaten

- **Quelle ist PVGIS-TMY oder DWD-TRY**, nicht DIN 4710; welche es ist, entscheidet sich je Klimaregion beim Import (`Dokumentation/aktuell/Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md`). Abruf in `EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:100 GetTMY` (Geokodierung `:212` über Nominatim). Der Ablauf steht in `EPOS.Kern/Allgemein/Import/KlimaImportAblauf.cs`.
- `Tab_Solar` / `Tab_Solar_STAMM` (`001_grundschema.sql:2153-2182`): **8 760 Zeilen je Klimaregion** mit `Temperatur`, `Sol_Nord/Ost/Sued/West`, `Globalstrahlung`, `Direktstrahlung`, `Diffusstrahlung`, `Sonnenwinkel`. **Keine Zeitspalte** — Reihenfolge = `ORDER BY ID` = UTC; die Korrektur auf Ortszeit sitzt beim Lesen: `Allgemein/Simulation/SolarZeitbasis.cs` + `SolardatenCtrl.ReadOrtszeit`.
- `Tab_Klimadaten` / `_STAMM` (`:1355-1389`): **365 Tageswerte** — Tagesmittel aus `SolarCalculator.GetDailyAverages` (`SolarPVGISCalculator.cs:484`), dazu `WE`, `TagTyp_W`, `TagTyp_NW` aus `KlimaImportAblauf.Tagtypen` (`:345-357`: `TagTyp_W = 2`, wenn Diffus > ½ Global; `TagTyp_NW` = Quartal × Wochenende, `Jahreszeitwert` `:363`).
- **Geneigte/orientierte Flächen: ja, zweifach.** `SolarCalculator.Sonnengeometrie` (`SolarPVGISCalculator.cs:353-384`) rechnet Zeitgleichung, Deklination, Sonnenhöhe, Azimut und `cosθ`. Darauf setzen **isotrop** `CalculateHourly` (`:389-418`, Albedo 0,2) und **anisotrop Hay-Davies** `CalculateHourlyHayDavies` (`:455-482`, `A_i = DNI/I_0n`, `R_b` mit cos-85°-Klemme). **Perez gibt es nicht.**
- Die vier Gebäude-Fassadenwerte `Sol_*` entstehen genau so: `KlimaImportAblauf.Rechnen` (`:305-338`) ruft `CalculateHourly` mit `NEIGUNG_FASSADE = 90` und `AZ_SUED=0, AZ_OST=−90, AZ_NORD=180, AZ_WEST=90` (`:132-135`) — also **senkrechte Fassaden, isotrope Transposition**, und werden anschließend zu Tagesmitteln verdichtet. Für VDI 6007 ließe sich die Stundenauflösung ohne neue Datenquelle aus `Tab_Solar` (GHI/DNI/DHI) über dieselbe Klasse je Orientierung/Neigung neu rechnen.
- **Windgeschwindigkeit und Feuchte** liest `TmyHourlyData` (`SolarPVGISCalculator.cs:66, 78`), aber `AccessRepository.SaveTmyData` (`:586-640`) schreibt sie **nicht** — es gibt keine Spalten dafür.
- `Tab_Klimaregion.Klimazone_DIN4710` (`:1392-1400`) existiert, dient aber nur der Erdreichrechnung (`VDI4640Pruefung.cs:117, 343`) und der Zonenkarte (`Simulation/KlimazonenPfade.cs`).

## 4. Brauchwasser und Prozesswärme

Beide laufen über **eine** Routine: `Allgemein/Simulation/ProfilBedarf.cs` (`Rechnen :467`, Quellen `ProfilQuelle.Brauchwasser :128`, `ProfilQuelle.Prozesswaerme :154`, `Strom :203`). Muster: 168-Stunden-Wochenprofil je Typ, auf 8 760 gekachelt ab dem Wochentag des 1. Januar und je Monat auf die 12 Monatsverbräuche normiert (`BhkwPlan.StromWocheToJahr`, `BhkwPlan.cs:221-240`). Anbindung in `SimulationWaermebedarf.Prozesswaerme_berechnen` (`:935-962`) und `Brauchwasserwaerme_berechnen` (`:994-1012`); Summen über `ProzesssummeUebernehmen` / `BrauchwassersummeUebernehmen` (`:983`, `:1036`) in MWh.

**Zusammenführung:** Seit Paket K1 sind die **drei Kanäle die führende Größe**, `Waermebedarf` ist ihre Summe (`SummenvektorAusKanaelen`, `:439`). Externe Ganglinien gehen über `Z_ProjektWaermebedarf.Kanal` in ihren Kanal (`:301-302`), Netzverluste werden **anteilig** verteilt (`Kanalsatz.NetzverlusteVerteilen`, `:374`), eine unabhängige `Energieprobe` (`:458`) prüft die Kanalsumme. Ein Gebäudemodell schreibt also in **`Kanal.HEIZUNG`** — mehr nicht.

## 5. Simulationsablauf und Ergebnisse

- **Ergebnistabellen:** `Tab_Ergebnis` (Kopf, `001_grundschema.sql:790-802`) mit den Kindtabellen `Tab_ErgebnisEnergiebedarf` (`:850-863`: `Waermebedarf_Gesamt`, `Waermelast_Max`, `Waermebedarf_Heizung/Brauchwasser/Prozess`), `Tab_ErgebnisBHKW`, `…Heizkessel`, `…Waermepumpe`, `…Solarthermie`, `…Photovoltaik`, `…Pufferspeicher`, `…Stromspeicher`, `…Wirtschaftlichkeit`. Geschrieben über `Controller/ErgebnisCtrl.cs` aus `SimulationLaufCtrl.ErgebnisSpeichern` (`:210`).
- **Anzeige-DTO:** `Controller/SimulationErgebnisCtrl.cs` — `Bedarf(wb, sb)` (`:813-828`, Kanalwerte über `SimulationRunner.BedarfJeKanal` `:1041`), `WarmwasserAnteil` (`:839`). Hülle `EPOS.UI.Daten/Bedarf/BedarfErgebnisHuelle.cs` und `EPOS.UI.Daten/Simulation/SimulationErgebnisHuelle*.cs`; Seite `EPOS.UI/Seiten/Simulation/SimulationErgebnisSeite.razor`.
- **Darstellungsregeln** (`Dokumentation/aktuell/Doku_Simulationsergebnis_Darstellung.md`): feste Reihenfolge je Reiter — Leerhinweis, Parameterblock, Kennzahlenblöcke (erste Rasterzeile immer `Wärme | Strom`), Tabellen, Diagramme mit einheitlicher Steuerzeile und Datenzoom (§ 5.1), Export-/Sprungknöpfe. Bilder liefert der Kern als PNG (`Allgemein/Bericht/ChartRenderer.cs`).
- **Bericht:** `Allgemein/Bericht/KennzahlenKatalog.cs` (`:65-100, :205`) zieht die Kanalwerte; `AbweichungsErmittler.cs:125-129` führt `Tab_Gebaeude`-Merkmale (`Waermebedarf`, `Wohnflaeche_gesamt`, `WW_Bedarf`, `Luftwechselrate`) im Variantenvergleich; `ProjektDetails.cs:72` lädt `Tab_Gebaeude` in den Bericht.

**Stellen, die eine neue Bedarfsquelle „Gebäudemodell" kennen müssten:**
1. `SimulationWaermebedarf.HeizwaermeEinesGebaeudes` (`:566`) — der Umschaltpunkt je Gebäude (Muster: `SimulationPV.cs:700-709`, ein Textwert entscheidet).
2. Ein Persistenzwert in `EPOS.Kern/Allgemein/DbWerte.cs` (Muster `PV_MODELL_EINFACH/ERWEITERT`, `:2173/2180`) plus Spalte an `Tab_Gebaeude` (+ `_STAMM`) über Schemaschritt 77.
3. Dialoge: `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor` (Gruppe „Verbrauch", ab `:153`, Rechenwegparameter), `GebaeudeKatalogDialog.razor`, `GebaeudeBedarfDialog.razor` (`:117` Parameter `GebaeudeBedarfDaten`).
4. Hüllen: **heute in der Windows-Schale** — `WindowsFormsApplication1/Views/Gebäude/GebaeudeHuelle.cs:322-350` baut `GebaeudeBedarfDaten`; `EPOS.UI.Daten` hat für Gebäude noch keine Hülle, nur `Bedarf/BedarfErgebnisHuelle.cs`. Eine neue Naht gehört nach `EPOS.UI.Daten`.
5. `EPOS.UI/Bausteine/Menuetabelle.cs:274-278` (`MenuItem_Gebaeude` → `GebaeudeAdmin`, `GebaeudetypenAdmin`) und `EPOS.UI/Seiten/Seitenschluessel.cs:215-219` — nur nötig, wenn ein eigener Pflegedialog entsteht (Regel: kein Untermenü mit nur einem Punkt).
6. Texte in `EPOS.Kern/MyResource/*.resx` beider Sprachen, danach `Werkzeuge/ResourceDesigner` ziehen.

## 6. Referenzlauf und Tests

- **Referenzlauf:** `EPOS.Referenzlauf` (plattformfrei, `lauf`/`vergleich`) und die Windows-Suite `Referenzlauf/`. Basis: `Referenzlaeufe/2026-09-11_R7_Speicherflotte`, dreizehn Projekte; die CI rechnet 1030, 1007, 1017, 1045, 1046.
- **Toleranz:** `Referenzlauf/Vergleich.cs:43-44` — `TOLERANZ_RELATIV = 1e-4` ab Betrag 1, sonst `TOLERANZ_ABSOLUT = 0,01`; gleich für Skalare und jedes Vektorelement (`:290-296`). Der Byte-Vergleich ist nur Information.
- **Verglichen werden** je Projekt `aggregate.csv` (Skalare) und die Vektordateien aus `Referenzlauf/Ergebnisexport.cs:58-67`: `waermebedarf.csv`, **`waermebedarf_gebaeude.csv`**, `waermebedarf_brauchwasser.csv`, `waermebedarf_prozess.csv`, `waermebedarf_extern.csv`, `waermebedarf_dauerlinie.csv`, `stundentemperatur.csv`, `restwaerme.csv` — dazu die Erzeugerreihen. **Ein neues Gebäudemodell trifft `waermebedarf_gebaeude.csv` unmittelbar**; solange es als abwählbare Variante gebaut wird und die Referenzprojekte auf dem Bestandsweg bleiben, bleibt die Basis gültig.
- **Tests (`EPOS.Kern.Tests/`)**, die den Bedarf abdecken: `GebaeudeBedarfCtrlTests.cs` (misst den Dialogwert **gegen den Lauf**, Projekte 1007/1017 mit einem, 1008/1039 mit mehreren Gebäuden), `BhkwPlanRueckgabeTests.cs` (die drei Physikfunktionen, ohne Datenbank), `BedarfProfilTests.cs`, `BedarfsProfilVorschauTests.cs`, `WaermebedarfKatalogTests.cs`, `ProzesssummeEinheitTests.cs`, `GebaeudeKatalogTests.cs`, `TagVCtrlTests.cs`, `SimulationLaufCtrlTests.cs`, `SimulationErgebnisCtrlTests.cs`, `InitTests.cs` (Monatsgrenzen). Wächter: `DoubleWacheTests` (kein `float` unter `Allgemein/Simulation/**`), `EinheitenWacheTests`, `RechenrandTests`, `ParallelitaetWacheTests`, `DokumentationLinkWacheTests`. Testdatenbank-Fälle gehören in `[Collection("Testdatenbank")]` mit `Kulturvorrichtung`.

## 7. Vorhandene Muster für ein neues Rechenmodul

**Das beste Vorbild ist der PV-Zweig (Stufe E2).** Bauform:

| Rolle | Datei |
|---|---|
| Reine Rechenklasse, **ohne Datenbank und Oberfläche** | `EPOS.Kern/Allgemein/Simulation/PvErweitertesModell.cs` (Huld-Koeffizienten, Wirkungsgrad), `PvStrangModell.cs` (Kennlinie, Gruppierung, Stundenschritt), `SolarZeitbasis.cs` (reine Indexrechnung) |
| Geometrie/Transposition an EINER Stelle | `Allgemein/SolarPVGISCalculator.cs` — `Sonnengeometrie` privat, zwei Modelle darauf |
| Modellschalter als Persistenzwert | `Allgemein/DbWerte.cs:2173/2180`, gelesen in `SimulationPV.cs:700-709` |
| Eingangs-DTO / Record | `Allgemein/Import/Auslegungstemperaturen.cs:19` (`sealed record` mit `Vorgabe`) |
| Ergebnis-DTO | `SimulationPV.cs:1491 PVModulErgebnis`; Anzeige-DTO `EPOS.UI/Dialoge/Bedarf/GebaeudeBedarfDaten.cs` (Einheit am Feldnamen, `init`-Eigenschaften, keine 8 760 Werte im DTO — Bilder über einen Delegaten) |
| Datenmodell + Migration | `Allgemein/Update/SchemaKatalog.cs` (`Schritt70_*`), `SchemaMigration.Schritt_70_PvStrangpruefung` (`:4672`) |
| Tests | `PvModulparameterTests`, `PvKoeffizientenTests`, `PvStrangRechnungTests`, `StrangAuslegungTests` — reine Rechenproben ohne Datenbank, plus DB-Fälle getrennt |
| Konzeptpapier | `Dokumentation/aktuell/Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`, `Konzept_Wechselrichter_EPOS-Plan.md` |

Die tragenden Regeln daraus: **die Physik in eine statische Klasse ohne `DataRepository` und ohne `SimulationProtokoll`**; das bestehende Modell bleibt **Zeichen für Zeichen unberührt** und wird über einen Persistenzwert umgeschaltet; ein Prüfkriterium, das den Grenzfall festnagelt („bei DNI = 0 liefert Hay-Davies exakt das isotrope Ergebnis"); neue Spalten **ohne DDL-DEFAULT auf Fachwerten**, NULL = Vorgabe.

## 8. Gliederung und Stil eines Konzeptpapiers

Aus `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`, `Konzept_Wechselrichter_EPOS-Plan.md` und `Konzept_Nutzungsdauer_AfA_EPOS-Plan.md` ergibt sich ein stabiles Muster: Kopf `# Konzept: <Gegenstand> EPOS-Plan — <Untertitel>`, darunter `**Rev. n — TT.MM.JJJJ — Prüfung und Vorschlag, zur Entscheidung durch Philipp**`, dann Auftrag im Wortlaut, Grundlage, `---`. Abschnittsfolge: Ergebnis in wenigen Sätzen, Befund/Ist-Stand mit `Datei.cs:Zeile`, Bewertung, Datenmodell mit Migrationsschritten, Rechenweg in nummerierten Schritten mit Formeln, Import, Verwaltung, Oberfläche, Vorschlag in Stufen mit Aufwandsklasse und Abnahme, Referenzlauf, Fragen mit Empfehlung (`Q1…Qn`), Abgrenzung, Reihenfolge, Nachträge (`## Nachtrag n (TT.MM.JJJJ) — …`). Ton: deutsch, Aktiv, kurze Hauptsätze; jede Behauptung mit Datei- und Zeilenbeleg; Entscheide tragen Kennungen mit Datum. Ablage `Dokumentation/aktuell/` mit Indexzeile in `Dokumentation/LIESMICH.md`; Markdown UTF-8 ohne BOM, CRLF.
