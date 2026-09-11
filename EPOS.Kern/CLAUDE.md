# CLAUDE.md — `EPOS.Kern`, der Rechenkern

Der plattformfreie Kern von EPOS-Plan: **403 `.cs`-Dateien** (168 aus iU4, dazu
`IDatenzugriff`/`SqliteDatenzugriff` aus iU6, `ChartRenderer` aus iU7-5, die 22 Dienste-Dateien
aus iU5, `EnergietraegerVarianteCtrl` aus iU8-8b, die **74 Dateien des zweiten Umzugs**
iU5-U1…U5, die sechs Dateien der Ergebnisseite aus iU9‑W11a, die **acht Dateien der
Importkette und der Lastspitzenkappung** aus iU9‑W12, die **sechs Dateien des
KI-Assistenten** aus iU9‑W15b, `Allgemein/Datenbank/Erstbereitstellung.cs` aus W3 und die
**acht Dateien der Speicherflotte** aus dem Stromspeicher-Sync vom 11.09.2026),
`net10.0` **ohne** `-windows`, AnyCPU.

**Der KI-Assistent ist mit iU9‑W15b vollständig hier** (bis auf das, was an lebenden
`Control`/`Form` hängt): `Allgemein/KI/KiChatService` (1 751 Z., der Gemini-Zugang),
`KiAusfuehrungsweg` mit `IKiAusfuehrung` und `KiVorbereitung` (die Naht zur
Ausführungsschicht — dieselbe Bauart wie `Dienste.*`, mit stiller Standardfassung),
`KiChatKontext` (Positivliste und Bereichszuordnung, plattformfrei), `KiVerlaufstexte`
(was im Gesprächsverlauf steht — der Kern sagt, WAS eine Zeile ist, `EPOS.UI` wie sie
aussieht), `KiWerkzeugWerte` (die Kulturgrenze der Werkzeugliste) und
`Allgemein/Hilfe/Kurzbeschreibung` (der Umbruch der Hilfe-Kurzbeschreibung).
Seit Paket iU4 (03.09.2026) liegen sie physisch hier; bis dahin waren sie aus
`../WindowsFormsApplication1/` verlinkt. Seit Paket iU6 (03.09.2026) **ohne jeden Verweis
auf `System.Data.OleDb`** — weder im Quelltext noch als `PackageReference`; **CA1416 steht
bei 0**. Fachdomäne und Datenmodell stehen in der
[`CLAUDE.md` der Repo-Wurzel](../CLAUDE.md), die Windows-Anwendung in
[`../WindowsFormsApplication1/CLAUDE.md`](../WindowsFormsApplication1/CLAUDE.md).

**Die eine Regel: Eine Fachänderung am Rechenkern wird EINMAL gemacht — hier.** Die Anwendung
übersetzt diese Dateien nicht mehr mit, sie referenziert das Projekt.

```powershell
dotnet build ..\EPOS.Kern\EPOS.Kern.csproj -c Release   # 0 Fehler, 3 Warnungen
dotnet test  ..\WP-Plan.Kern.slnf -c Release            # 3 430 Tests (Stand iU9-W14c)
```

Die dritte Warnung ist mit `Controller\StromverbraucherStammCtrl.cs` aus der Anwendung
mitgewandert (CS0108, `items` verdeckt `StromverbraucherModel.items`) — sie ist nicht neu. Die
Gesamtzahl der Lösung liegt bei **34** (sie war 36, bis iU8-9 das Formular `Form_Kosten_Auswahl`
mit seinen beiden WFO1000 löschte).

## Was hier liegt

| Ordner | Inhalt |
|---|---|
| `Allgemein/` (22) | `BhkwPlan.cs` (der Rechenkern selbst, Namespace `WPPlan.Core`), Zugriffsschicht (`IDatenzugriff`, `SqliteDatenzugriff`, `DataRepository` als Fassade, `DbParam`, `DbVorgang`, `DbWerte`, `RecordSet`), `Meldung` (Melde-Haken), `Sprache`, `ZahlText`, `Zeilenumbruch`, `SolarPVGISCalculator`, `WizardItemClass` (Typ- und Nummernkatalog) — seit iU5-U5 dazu `FileDlgClass` und `chart_test` (`ToolsClass` ist mit iU9-W14b geloescht: ihre beiden Nutzer sind gefallen, das Lesen liegt in `GanglinienTextDatei`, das Oeffnen in `Dienste.Datei`), seit iU9-W6.1 `EmissionsVorgaben` (die Vorgabewerte der beiden Katalogeditoren, vorher dreimal im Oberflächencode; seit iU9‑W11a.5 zusätzlich die beiden SUBSTITUTIONSFAKTOREN der Autarkiekachel — `CO2_NETZSTROM_KG_JE_KWH` 0,42 und `CO2_WAERME_KG_JE_KWH` 0,20, wörtlich aus `DashboardForm.cs:355`, Befund W11‑B31), seit iU9-W9 `Ferienzeit` (die vier Ferienregeln des Gebäudekatalogs samt der Umrechnung Tag/Monat ↔ Jahrestag), `Suchmuster` (die Platzhaltersuche, die zuvor zweimal wortgleich dastand) und `Gebaeudebauweise` (der Rundweg Bauart ↔ Bauweise, Entscheid W9‑O‑2) — seit dem 04.09.2026 dazu `Energieeinheit` und `BedarfEinheitWahl` |
| `Allgemein/Simulation/` (33) | die vollständige Engine — `SimulationControl` (beide `partial`-Hälften), `Kaskadenschleife`, `SimulationKanaele`, `Init`, `SimulationRunner`, die Module je Erzeuger/Bedarf, `WaermequelleClass`/`WaermesenkeClass`, `Warnkriterien`, `ProfilBedarf`, `StilleDb`. **Mit iU9‑W10a** kommen die Rechen- und Anzeigewege der sieben Simulationsdialoge dazu: `WaermesenkeClass.SenkeAnzeige`/`SENKE_LEER` (sie war eine STATISCHE Methode auf `Form_Waermesenke` mit drei fremden Aufrufern, Befund W10‑B22), `VDI4640Pruefung.Sondenmeter`/`Volllaststunden`, `ErdreichAuswertung.ErdreichLaufErgebnis`/`ErgebnisZuordnen` (die Zuordnung stand doppelt in Maske und Aufrufer, W10‑B8) und die **erzeugte** Datei `KlimazonenPfade.cs` — 15 Zonen als SVG-Pfade, gebaut von `../Werkzeuge/KlimazonenPfade/erzeugen.py`, weil der Vorläufer die Karte zur Laufzeit mit einem Regex aus einer eingebetteten SVG las (W10‑B5). **Mit iU9‑W10b** kommt der Rest der Simulationskonfiguration dazu: `SchemaModell.cs` (unverändert verschoben — die letzte Datei, die noch in der Anwendung lag), `SchemaLayout.cs` (die ANORDNUNG des Schemas, bis dahin GDI+ in `SchemaAnsicht`: Spaltenbreiten, Knotenhöhen, Bézierbögen, Kaskadenband, Legende — headless prüfbar), `Kaskade.cs` (die vier Plätze `Tab_Einstellungen.Tool_1..4` samt den beiden Stromplätzen, bis dahin sechs unsichtbare Steuerelemente) und `WaermequelleClass.QuelleSchreiben` mit dem Satz `QuelleErgebnis` (die sechs Zweige der Quellenwahl als EIN Schreibweg). **Mit iU9‑W11a** kommen `ErgebnisPraesenz` (war `internal` in `Views/Simulation/` und steuert fünf der sechs Ergebnismasken), `Ganglinie` (`Dauerlinie`/`Anzeigewerte` aus `GanglinienDarstellung`; `Stapeltyp`/`StapelEinstellen` arbeiten auf einer WinForms-`Series` und bleiben) und `LaufFortschritt` dazu. **`SimulationControl.Do_Simulation` nimmt seither `IProgress<LaufFortschritt>` und `CancellationToken` entgegen** — ohne die beiden Zusatzangaben unverändert; der Abbruch wird ZWISCHEN den fünf Phasen geprüft (Start, Kaskade, Photovoltaik, Stromspeicher, Abschluss). Eine Meldung je Erzeuger gibt der Rechenweg nicht her: Die Kaskade läuft stundenweise und bedient in jeder Stunde alle Erzeuger nacheinander. **Die vier EIGENANTEILE** (`SimulationRunner.EigenanteilWpMwh`/`…KesselMwh`/`…SolarKwh`/`…BhkwMwh`) und die zwei Ableitungen `RestNachEigenanteil`/`DeckungProzent` sind aus `BaueErgebnis` herausgezogen: Dieselben Ausdrücke standen wortgleich in `Form_Simulation_Detail`. **Seit dem Stromspeicher-Sync vom 11.09.2026 ist `SimulationControl.Stromspeicher.cs` die WEICHE zwischen Einzelspeicher und Flotte**: Der `[ModuleInitializer]`-Haken reicht seither zusätzlich den `CancellationToken` und `SpeicherflotteAktiv` herein; `SpeicherlaufAusfuehren` ruft `SpeicherFlottenProjektCtrl.Rechnen`, sobald eine Flotte anliegt oder für das Projekt aktiviert ist, und fällt sonst **unverändert** auf `StromspeicherSimCtrl` zurück — genau deshalb bleibt der Referenzlauf byte-gleich. Der übrige Projektlauf liest `Rest_Strombedarf` weiterhin als **nichtnegativen Netzbezug**; der vorzeichenbehaftete Saldo steht getrennt in der Flottenbilanz (`StromspeicherSimCtrl.ProjektNetzbilanz`), sonst minderte eine Einspeisung den Jahresbezug. Passt das Viertelstundenraster nicht, bricht der Lauf mit `SIMENG_SPEICHER_RASTER_ABWEICHUNG` ab, statt stillschweigend falsch zu rechnen |
| `Allgemein/Wirtschaftlichkeit/` (21) | alle 21 Dateien — `KapitalwertRechner` (DIN EN 17463), `EmissionsBilanzRechner`, `StromMatrix`, `WirtschaftlichkeitCtrl`, die KWKG-/EEG-/Steuer-Rechner; **seit dem Anwenderentscheid W14a‑E‑8‑B1 (07.09.2026) `Emissionsquelle`** — DIE eine Emissionsquelle aller Erzeuger (siehe unten) |
| `Allgemein/Bericht/` (14 + 4) | die **DATEN**-Hälfte: `BerichtTexte`, `BerichtsDaten`, `EmissionsAusweis`, `KostenEmissionRechner`, `ProjektDetails`, `KennzahlenKatalog`, `AbweichungsErmittler`; seit iU7-5 der **Renderer** `ChartRenderer` (seit iU9‑W10a mit `Jahresgang` — 1 304 × 440, zwei Reihen, Monatsachse 0…12, vorzeichenfähige y-Achse, für das Quelltemperaturbild des Erdreichdialogs); seit iU5-U3 die **AUSGABE** `WordBerichtGenerator`, `ExcelBerichtGenerator`, `IBerichtsBaustein`, `BerichtsKonfiguration`, `ZeitreihenExtraktor` und `Bausteine/` (4 Dateien); seit iU9‑W12 `PeakShavingBild` — die drei Reihen und Farben des Vorher/Nachher-Bildes der Lastspitzenkappung. Es ist **kein neuer Renderer**: `ChartRenderer.ErzeugerStapel` trägt seit iU9‑W11a eine Sekundärachse, und genau die braucht der Ladezustand (kWh und kW teilen keine Skala) |
| `Allgemein/Dienste/` (22) | die **neun Umgebungsdienste** (iU5): `Dienste` (Halter), `IDialogDienst`, `IDateiDienst`, `IPfade`, `IEinstellungen`, `ILizenzAblage`, `IGeraeteId`, `ISprache`, `INavigation`, `IProjektKontext`, ihre Standardfassungen (`StilleDialoge`, `KeineDateiwahl`, `StandardPfade`, `FluechtigeEinstellungen`, `KeineAblage`, `KeineGeraeteId`, `StandardSprache`, `KeineNavigation`, `LeererProjektKontext`) und die sprachneutralen Schlüssel `Masken`, `Ansichten`, `Projektwahl`. **Die Konstantenklasse `Gewerke` und `INavigation.OeffneGewerk` sind mit iU9‑W16b.1 entfallen** — sie existierten ausschließlich für `FormMain` (Befunde W16‑B27/B28) |
| `Allgemein/Update/` (8) | `Anlagenzeilen`, `ProjektPuffer`, `SchemaKatalog`, `SchemaStand` (Ergebniszustand der Migration und die DDL-Konstanten, die Controller zur Selbstanlage brauchen; **`Zielversion` steht seit `SCHRITT_74_SPEICHERAUSLEGUNG_STRICT` (Auftrag #178, 11.09.2026) auf 74; die Testdatenbank steht auf demselben Stand — davor 73 aus dem Stromspeicher-Sync desselben Tages und 72 aus den Schritten 70–72 (09.09.2026) und Aufgabe #154. Schritt 73 legt `Tab_SpeicherAuslegung` samt eindeutigem Index über `ID_Projekt`, `COALESCE(ID_Energieanlage,0)` und `Bezeichner` an — die DDL steht als `SpeicherAuslegungCtrl.SQL_TABELLE`/`SQL_INDEX` beim Controller, weil dieser die Tabelle bei Bedarf still selbst anlegt; die Migration ruft dieselben zwei Texte. Er legt KEINE Zeile an und verschiebt keinen Rechenwert**), **`SpeicherAuslegungStrict`** (**Schritt 74**, Auftrag #178: dieselbe Tabelle als **STRICT**-Tabelle — sie war die EINZIGE Fachtabelle des Zielschemas ohne `STRICT` (Befund aus dem iOS-Lauf 41: 119 Tabellen, 117 STRICT, die zwei ohne sind `sqlite_sequence` und diese). SQLite kennt kein `ALTER TABLE … STRICT`, deshalb **der erste TABELLENNEUBAU des SQLite-Zweigs** nach dem Rezept des Handbuchs: Rest abräumen, `CREATE` unter dem Hilfsnamen `Tab_SpeicherAuslegung_neu` aus DEMSELBEN Text `SpeicherAuslegungCtrl.SQL_TABELLE` (der seither selbst `STRICT` trägt — sonst entstünde auf jeder neuen Datenbank wieder eine Tabelle ohne), `INSERT … SELECT` mit NAMENTLICH genannten Spalten, `DROP`, `RENAME`, Index neu. **Alles in EINER Transaktion über `DataRepository.Vorgang()`** und nicht über `SqliteDdl`: Zwischen `DROP` und `RENAME` gibt es einen Augenblick ohne die Tabelle, und die Zugriffsschicht öffnet je Einzelanweisung eine Verbindung aus dem Pool. Wiederholbar über `Zaehlung()` gegen `sqlite_master`; ergebnisneutral — Zeilen und IDs werden mitkopiert), **`StromspeicherFirmaNachtrag`** (Schritt 68, Anwenderentscheid **W14a‑E‑10‑Q7**: `Firma` in `Tab_Stromspeicher_STAMM` und der Projektkopie — der EINZIGE Gerätekatalog ohne Herstellerspalte; die DML trägt den Text vor dem ersten Doppelpunkt des Bezeichners nach, idempotent, Muster der Schritte 57/58/67; was kein Präfix hat, bleibt leer, der Nachtrag rät nicht) und **`PvKoeffizientenReparatur`** (**Schritt 69**, Befund **W6‑B‑5** mit den Entscheiden Q1–Q3: die verdorbenen Modulkoeffizienten `alpha_SC`/`beta_OC`/`gamma_PMP`/`T_NOCT` in `Tab_PV_STAMM` **und** `Tab_PV`. Giftsignatur statt IDs — Wert = `I_Kurzschluss` auf 1e‑6 genau, oder ausserhalb des physikalischen Fensters, und die 0 liegt in jedem der vier ausserhalb; repariert wird aus der CEC-Liste über `CECDataService`, mit den vier ausgelieferten Modulen EINGEBETTET, weil der Ordner `VDI-3805-Daten` seit W6‑O‑9 abwählbar ist; was keinen Treffer hat, wird `NULL` mit einer Protokollzeile je Satz. **Der einzige Schritt des SQLite-Zweigs, der ein Rechenergebnis ändert** — `T_NOCT` geht in beide PV-Modelle, und genau das war der Zweck: Basis `2026-09-07_R6_PvKoeffizienten`) — seit iU5-U5 dazu `AnlagenEindeutigkeit`, seit iU9‑W10a `ProjektPuffer.NutzbareKapazitaetKWh` (Volumen × 1,16 × Spreizung ÷ 1000 — die Formel stand in ZWEI Masken, Befund W10‑B12; die Leerregeln bleiben je Maske) |
| `Allgemein/Lizenz/` (6) | seit iU5-U1: `LizenzManager`, `LizenzToken` (Ed25519 über BouncyCastle), `LizenzServerClient`, `GeraeteId` — die Umgebung kommt über `Dienste.Lizenzablage`, `Dienste.Pfade`, `Dienste.Einstellungen` und `Dienste.GeraeteId`. **Mit iU9‑W15c ist die reine Zustandsrechnung herausgezogen**: `LizenzManager.Bewerten(token, geraeteId, heute, anker)` beantwortet die sechs Zustände OHNE Ablage und mit vorgegebenem Datum, `Pruefe()` bleibt die Fassade (Token laden, Anker lesen, bewerten, Anker fortschreiben) — Verhalten unverändert, nur verschoben. Ohne diese Trennung liefe jeder Test gegen den echten Zeitanker und setzte ihn fort (Risiko R‑W15c‑3). `StatusText()` und `TypText()` lesen ihre neun Sätze seither aus `MyResource.Resource.LIZ_ST_*`/`LIZ_TYP_*` statt aus dem Quelltext — es war der letzte unlokalisierte Anwendertext des Lizenzwegs. **Mit iF30 (06.09.2026) kommen zwei Dateien dazu**: **`Schreibnaht`** — die EINE Stelle, an der der Lesemodus durchgesetzt wird (gerufen von `SqliteDatenzugriff.ErzeugeKommando`, durch das jede Anweisung des Kerns läuft), mit `Schreibrecht` als Func-Naht, `Freigabe(grund)` als benanntem `using`-Bereich für die fünf Ausnahmen des Programms, `WerkzeugFreigabe(grund)` für Referenzlauf, iOS-Prüfmodus und Testvorrichtung und `LesemodusException` mit fertigem Anwendertext — und **`LizenzLage`**, das sprachfertige Lagebild für das Banner der `AppWurzel` (`Bilden` rein, `Ermitteln` die Fassade; die Razor-Komponente darf `Pruefe()` nicht selbst rufen, Regel S‑2). `LizenzManager` trägt dafür die drei Warnstufen `WARNSTUFE_1/2/3` (30/14/7) samt `RestTage` und `Warnstufe` und verwirft den Zwischenspeicher der Naht bei jedem Token-Wechsel |
| `Allgemein/Import/` (34) | seit iU5-U1: `AnsiEncoding`, `CsvReader` (NReco, MIT), `GanglinienDatei` (CSV/TXT und Excel über ClosedXML), `CEC/` (5), `Pan/` (2), `OND/` (2), `Stromspeicher/` (3), `VDI 3805/` (5 — Heizkessel, Pufferspeicher, Solarkollektoren, Wärmepumpen, `VdiAuswahlFilter`). **Mit iU9‑W12** kommt die AP5-Kette selbst dazu: `GanglinienImportAblauf` (sie stand ZWEIMAL wörtlich im Oberflächencode — mit Ablage in der Stammdatenverwaltung, ohne in der Lastspitzenkappung; die drei Entscheidungen kommen als Rückrufe herein, angezeigt wird nichts), `GanglinienOptionenModell` (die acht Steuerwertlisten des Importdialogs — Blazor und iOS brauchen dieselben Plätze in derselben Reihenfolge) und `GanglinienProtokollText` (Schlüssel → Text; die Farbe ist eine Stufe geworden, `System.Drawing` gibt es hier nicht) . **Mit iU9-W13** kommt der KATALOGIMPORT dazu: `KatalogImportProfil` (die Auspraegung der vier VDI-3805-Importe als DATEN samt dem Aufzaehlungstyp `KatalogImportArt` — Katalogschluessel, Unterordner, Dateifilter, die gefilterte Groesse samt Spaltenkopf und Nachkommastellen, Detailfeldliste; der Bauplan stand viermal wortgleich im Formularcode, Befund W13-B3), `KatalogImportAblauf` (Lesen, Filtern, Vorpruefen, Ausfuehren mit `IProgress` und `CancellationToken`; der Konfliktdialog ist kein Rueckruf, sondern eine ZAESUR zwischen zwei Aufrufen), `KatalogImportSatz` mit vier Auspraegungen (die vier `FuelleModellwerte` — beim Heizkessel die einzige echte Rechnung der Vierlinge mit Brennstoffdeckel aus `Tab_Brennstoff_Stamm`, Oel-/Gas-Weiche und Platzhalter 1, bei der Waermepumpe die vier Regelungstexte als benannte Persistenzwerte) und `GanglinienTextDatei` (`ToolsClass.OpenText` ohne Dialog IM Parser, mit Kopfzeilenschalter fuer W14b). `WaermepumpenImport` liefert seine Kennlinien jetzt TYPISIERT (`KennlinienZu`) statt als `';'`-Ketten, die das Formular ein zweites Mal zerlegte (B34), und meldet einen unbekannten Aufstellungsindex, statt den ganzen Dateiimport mitzureissen (B35). In `CEC/` und `Pan/` fallen die deutschen Anzeigetexte weg (`Bifacial` ist ein Wahrheitswert, B50), die PAN-Sitzungsliste ist ein Instanzfeld statt `static` (B46), die PTC-Naeherung steht als `PanModule.PtcGeschaetzt` im Modell (B43), und `CECDataService.Filter`/`BuildWildcardMatcher` sind geloescht — die dritte Platzhaltersuche des Bestands, ohne Aufrufer (B41). **Mit iU9‑W14c** kommt `KlimaImportAblauf` dazu — Geokodierung, PVGIS-Abruf, Sonnenstandsrechnung und EINE Transaktion (Kopf, 8 760 Stunden-, 365 Tageswerte), bis dahin ein 177-Zeilen-Handler in der Oberfläche. **Der einzige Netzzugriff des Programms hängt an einem DELEGATEN** (`ITmyQuelle`): Unter Windows ist es `PVGIS_EPW_Downloader.GetTMY`, in der Probe eine eingefrorene Datei — der Ablauf ist damit ohne Internet nachweisbar (Risiko R‑W14c‑5). Angeglichen sind dabei EIN statt VIER PVGIS-Abrufen (drei wurden geholt und weggeworfen, W14c‑B28), der Sonnenwinkel als Wert statt aus einem statischen Feld (W14c‑B29), der `Listbezeichner` in den Tageswerten (W14c‑B31) und eine Dublettenprüfung, die die DATENBANK fragt und MELDET (W14c‑B26). **Mit W13‑E‑2** (07.09.2026, Stufe S1) kommt der STROMSPEICHERIMPORT dazu — der erste Katalogimport ohne VDI 3805: `Stromspeicher/CecSpeicherImport` (die CEC Energy Storage System List, **XLSX über ClosedXML UND CSV**; die Kopfzeile steht an Zeile 16 und wird an ihren Pflichtspalten gesucht statt festgeschrieben), `Stromspeicher/BslibImport` (die vier vermessenen Systeme der HTW Berlin; übergangene Zeilen werden BENANNT) und `Stromspeicher/StromspeicherImportSatz` mit den geteilten Umrechnungen — darunter die **wertabhängige Wirkungsgradweiche**: Die CEC-Spalte heißt „%", führt aber neben 85 und 97,5 auch 0,88, und wer stur durch 100 teilt, schreibt 0,0088 in den Katalog. Der Ablauf bekommt dafür einen **Quellschlüssel** (`KatalogImportAblauf.Lesen(…, quelle)`) — beide Quellen kommen als CSV und sind an der Endung nicht zu unterscheiden —, das Profil vier Teile, die bei den vier VDI-Ausprägungen leer bleiben (`Quellen`, `Listenspalten`, `Zweitfilter`, `Hinweis` — der fünfte, `HerstellerFilter`, ist mit **O‑12** gefallen, seit die Herstellerklappliste der `Katalogliste` gewichen ist), und `KatalogImportSatz` die fünfte Ausprägung `StromspeicherKatalogSatz`. Der Netzabruf steht als `CEC/CecSpeicherDienst` neben seinem Wechselrichterzwilling: dieselbe Rückfallkette, 45 Sekunden, 30-Tage-Zwischenspeicher — aber er lädt **Bytes** (die Quelle liefert XLSX, ein `ReadAsStringAsync` zerstörte die Mappe), er zerlegt NICHTS (das tut der Zerleger, es gibt die Kopfzeilenerkennung genau einmal), und er nimmt einen `HttpMessageHandler` entgegen und ist damit **ohne Netz prüfbar** |
| `Allgemein/Katalog/` (16) | seit iU5-U1: `DublettenPruefung`, `KatalogBereinigung`, `KatalogRegistry`; **seit iU9‑W14c** `DublettenBefundText` (Blatt- und Gruppentext als Spalten-/Wertepaare statt `DataRow` — ohne ihn zöge `EPOS.UI` `System.Data` herein, Befund W14c‑B42) und `DublettenBaum` (die vier Ebenen des Befunds als anzeigefreie Knotenliste mit SCHLÜSSEL statt Index; Wurzel und Ast von vorn offen, die Gruppe zu — bitgleich zu `BaumFuellen`). `KatalogRegistry.Anzeige` löst dabei die neunzehn Anzeigenamen ab, die als neunzehn `case` ein zweites Mal in `Form_KatalogDubletten` standen (Befund W14c‑B40); `KatalogBereinigung` bekommt `SatzUmbenennen` (der letzte verkettete `UPDATE` einer Maske, W14c‑B45) und `VerwendungZaehlen` MIT Grund — ein Fehlschlag der Prüfung ist nicht „nicht verwendet" (W14c‑B44); **seit iU9‑W14a.0a** `KatalogBrowserProfil` (die Ausprägung der vier Erzeuger-Katalogbrowser als DATEN — Stammtabelle, ein- oder zweispaltige Liste samt Textbauplan, Filterart, Detailfeldliste, Speicherweg, Schreibschutzanzeige, Meldung ohne Auswahl, Hilfeziel; Aufzählungstypen `KatalogBrowserArt` und `BrowserFeldArt`; `KatalogFilterArt` ist mit W14a‑E‑10 entfallen) und `ModulKatalogProfil` (dasselbe für die zwei Modulkataloge, mit der `leerErlaubt`-Regel und den dreizehn Vorbelegungen je Feld). Zwillinge zu `KatalogImportProfil`, Muster `BedarfsArt`; seit iU9‑W12 `ImportKonfliktModell` — `KonfliktAktion`, `KonfliktEntscheidung`, `ErlaubteAktionen`, `BefundText`, `NamensVorschlag` und die OK-Prüfung. Seit iU9-W13 fuehrt `KatalogRegistry` fuer `WAERMEBEDARF` ein LEERES `ImportSpalten`-Array: `null` hiess „kein Dateiimport“, und genau daran lag es, dass die Waermebedarfsverwaltung als einzige Importmaske ohne Dublettenpruefung auskam (B2). Sie lagen in `Views/Import/Form_ImportKonflikte.cs`, also in einer WinForms-Datei, die FÜNF Importmasken benutzen; solange sie dort lagen, zog jede Razor-Komponente eine WinForms-Abhängigkeit nach `EPOS.UI` (Befund W12‑B18). Die Aktion ist hier ein **Wert**, nicht der Anzeigetext einer Zelle (W12‑B19); **seit W14c‑E‑6** `KlimaWaisenBereinigung` — die zwei Löschanweisungen des Schema-Schritts 62 als EINE Wahrheit für `SchemaMigration` (Windows-App) und den Kern-Test; **seit dem Anwenderentscheid W14a‑E‑10 vom 07.09.2026 (Stufe S1 des `Konzept_Katalogfilter_EPOS-Plan.md`) vier Dateien des KATALOGFILTERS**: `Katalogfilterprofil` (die ACHT Ausprägungen mit 5 bis 9 Spalten aus Kapitel 4 des Konzepts, dazu `Katalogspalte`, `Katalogwert` und `Katalogfilterzeile` — der vierte Zwilling zu `KatalogImportProfil`, `KatalogBrowserProfil` und `ModulKatalogProfil`, und wie sie DATEN ohne Datenbank), `Katalogfilter` mit `Katalogfilterstand` (die RECHNUNG: Spaltenfilter UND, Suche ODER über die Spalten und UND über die Begriffe — `VdiAuswahlFilter.Passt` und `Suchmuster` werden GERUFEN, nicht abgeschrieben —, dazu der Sortierzyklus auf → ab → aus mit Leerwerten hinten), **`Zahlenausdruck`** (Frage **Q1 = ja**: EIN Feld je Zahlenspalte, das `>10`, `>=10`, `<60`, `<=60`, `=15`, `10..60` und die bloße Zahl `15` versteht; das Dezimaltrennzeichen ist das der Kultur, der Bereichspunkt wird vorher abgespalten, und ein unverstandener Ausdruck ist **kein** Filter — dieselbe Regel wie beim kaputten Suchmuster) und `Katalogfeld` (die vier Leseregeln der acht `…StammCtrl.Katalogfilterzeilen()`: NULL bleibt NULL statt 0, Wirkungsgrad > 2 gilt als Prozent, Quotient und Produkt für die abgeleiteten Größen, Hersteller aus dem Bezeichnerpräfix — **seit Schemaschritt 68 nur noch als RÜCKFALL**, die gepflegte Spalte `Firma` schlägt ihn, Q7). **Mit Stufe S2 (07.09.2026) kommen ZWEI Dateien dazu**, beide ebenfalls Rechnung ohne Oberfläche: **`Katalogfilterregister`** (Frage **Q2 = ja**: der `Katalogfilterstand` je Katalog über die SITZUNG, gemeinsam für Verwaltung und Projektdialog — eine Instanz je `Anlagenart` unter einem Schloss, `Leeren()` als einziger Weg hinaus. **Kein `static` in einer Razor-Komponente**: prozessweiter Zustand an einem Ort, den keine Probe zurücksetzen kann, und „welcher Katalog steht wie gefiltert" ist eine FACHLICHE Frage. Es wird **nichts geschrieben** — kein `Dienste.Einstellungen`, keine Datei; der Projektwechsel räumt nicht auf, weil der Filter am Katalog hängt und der projektübergreifend ist) und **`Katalogverwendung`** (Frage **Q12 = ja**: die Spalte „im Projekt verwendet" der sieben Projektdialoge. `Stempeln(katalog, imProjekt)` setzt je Zeile ein Kennzeichen und liefert die Trefferzahl. Die Quelle ist die **lebende Projektliste** und nicht eine Zählabfrage — die Dialoge schreiben erst beim OK zurück, eine Abfrage wäre nach der ersten Übernahme veraltet; **einmal** für die ganze Liste über ein `HashSet`, danach jede Zeile ein Nachschlagen in konstanter Zeit). Alle sechs sind **ohne Oberfläche prüfbar** — genau dafür stehen sie hier: `ZahlenausdruckTests`, `KatalogspaltenfilterTests` (39 Fälle, davon elf mit gemessenen Trefferzahlen gegen `Kenndaten_Test.sqlite` — Heizkessel „Gas" 52, `P_th 10..60` 54, `η >=0,95` 32, zusammen 15 von 63; Wärmepumpe „Luft" 34, `5..12` 31, VL max `>=60` 18, zusammen 7 von 51 — und die **Gegenprobe**, dass die elf Bedienelemente des alten `WaermepumpenKatalogFilter` dieselbe Menge treffen) und `KatalogfilterstandTests` (13 Fälle). **Mit Stufe S3 (07.09.2026) kommen SECHS weitere Ausprägungen und die zwei Importlisten dazu**, alle im selben Muster: `Katalogfilterprofil.FuerBedarf(BedarfsArt)` — fünf Spalten für Brauchwasser, Prozesswärme und Stromverbraucher (Bezeichner, Typ, Jahressumme [MWh] als Σ `Monat_1…12`, Beschreibung einzeilig, Auslieferung als Kennzeichen aus `ReadOnly`) —, `Katalogfilterprofil.FuerZeitreihe(Zeitreihenart)` — Bezeichner, Zeitintervall (nur Strom), Beschreibung (nur Solar), Jahresarbeit [MWh] und Spitze [kW] — und `Katalogfilterprofil.AusSpalten(schluessel, spalten)`, über das die zwei Importprofile ihr `Listenprofil` bilden (S3.4). Ein Profil trägt seither einen **`Schluessel`** statt nur einer `Anlagenart`: Die sechs neuen Kataloge sind keine Anlagen, und ein zweiter Aufzählungstyp in `ParameterVerwendung` — der den VERWENDUNGSkatalog beschreibt und gegen `pragma table_info` gehalten wird — wäre die falsche Stelle gewesen; die acht Anlagenkataloge tragen ihn nicht ein und heißen weiter `ANLAGE_<Art>`, damit der gemeinsame Filterstand aus S2.5 Zeichen für Zeichen derselbe bleibt. Ebenso trägt eine `Katalogfilterzeile` seither einen `Schluessel` neben dem `Bezeichner` (Vorgabe: der Bezeichner): Ein IMPORTKANDIDAT hat keinen Datenbank-Bezeichner, die CEC-Liste bringt 20 743 Module mit Namensdubletten, und bei den VDI-Importen ist der Bezeichner obendrein vom Anwender änderbar — die Importmasken vergeben deshalb eine laufende Kennung. Die Zahlen liefern `BedarfStammCtrl.Katalogfilterzeilen`/`Vergleichszeilen`, `ZeitreihenKatalogCtrl.Katalogfilterzeilen` und `GanglinienAuswertungCtrl.Kennzahlen` — letzteres mit **einer** Abfrage je Katalog (`ROW_NUMBER() OVER (PARTITION BY …)` verdichtet eine Viertelstundenreihe auf Stundenmittel, damit die Spitze dieselbe ist, die die Grafik zeigt; eine naive `MAX(Wert)`-Abfrage meldete 4 590 statt 1 513,5 kW). Prüffälle: `KatalogfilterBedarfTests` (17), `KatalogfilterZeitreihenTests` (12) und `KatalogfilterImportTests` (9) |
| `Allgemein/Export/` (1) | seit iU5-U1: `CsvExportClass` |
| `Allgemein/KI/` (15) | seit iU5-U2 das, was der Assistent **weiß**: `HilfeWissen` (`WissensAbschnitt`), `WikiWissen`, `SemantikIndex`, `SemantikModell` (ONNX), `KiSchreibschutz`, `KiSicherungspunkt`, `KiEinwilligung`, `KiTextlieferant`, `Aktionen/KiAktionsTexte`, `Dialoge/KiDialoge`, `Dialoge/KiDialogTexte`. **Seit Auftrag #199 (Stufe S1 „Der Hilfe-Assistent im Dialog") drei Dateien mehr:** `KiAufrufkontext` (Bereich, Dialogname, Hilfeschlüssel, Frage, Kennung — das eine Argument von `Dienste.Navigation.OeffneMaske(Masken.KiAssistent, …)`), `KiVerfuegbarkeit` (die EINE Auskunft „darf diese Installation den Assistenten anbieten?", ohne Netz-, Schlüssel- und Einwilligungsfrage — Anwenderentscheid KI‑D‑Q1) und `KiMeldungskennung` (die 15 Kennungen der erklärbaren Meldungen samt der vorbelegten Frage `KI_FRAGE_<Kennung>`). `KiChatKontext` trägt dazu die Tabelle Hilfeschlüssel-Präfix → Bereich und den gemeldeten Aufruf, `HilfeWissen` das **Aktionswissen** je Kennung. **Seit Auftrag #200 (Stufe S2) eine Datei mehr:** `KiMaskenbruecke` — der Weg vom offenen Razor-Dialog zum Assistenten. Der Dialog meldet je Katalogfeld einen Getter (und, für S3, einen Setzer) an; `Lesen` liefert Anzeigename, Art, Einheit, Rohwert UND Text, `Dialogdaten` den wörtlichen Feldblock, den Anfrage und Vorschau gleichermaßen mitnehmen, und `Vermerken` die eine Protokollzeile je Übertragung (`KiProtokoll.Zeile`, angehängt über die Senke der Plattformhülle). Ein Eintrag je Maske, die jüngste Anmeldung gilt, `Abmelden` trägt die Marke ihrer Anmeldung. **Der Dialogkatalog nennt seither Eigenschaften statt Controls** (`HeizkesselKatalogDaten.Ptherm`, Regel `KiEigenschaftspfad` in KiKern) und führt mit `StromspeicherAuslegung` eine fünfte Maske; `KiEinwilligung` trägt neben der allgemeinen die **zweite Stufe „Dialogdaten"** (KI‑D‑Q2: eigener Merker, eigene Fassung, eigenes Datum, eigener Rückweg — ohne eingehängten Haken kein Weg zu ihr), und `KiChatService` nimmt den Feldblock als EINEN optionalen Parameter entgegen, ohne Schalter oder Einwilligung nachzuprüfen. Was er **bedient**, bleibt bei der Oberfläche |
| `Allgemein/Hilfe/` (1) | seit iU5-U5: `DokuUebersetzung` (Wiki-URL durch den Übersetzungs-Proxy) |
| `Controller/` (127) | 127 Controller ohne Oberflächenbezug — 50 aus iU4, 29 aus iU5-U4, `EnergietraegerVarianteCtrl` aus iU8-8b (die Datenseite des ersten Blazor-Dialogs), `KostenfaktorCtrl` aus iU9-W1.5, `KostenSummenCtrl` aus iU9-W0.1 und `EnergietraegerPreisCtrl` aus iU9-W4.4 (die neun SQL-Anweisungen der Trägerkarte). **Mit iU9-W6 hat die Erzeugerseite ihre Datenseite bekommen:** `EnergietraegerVarianteCtrl.Anlegen`/`VariantenDerGruppe`/`TraegerUmhaengen` (die 185 Zeilen `CreateNewEnergyCarrier`, die ZWEIMAL wortgleich in der Oberfläche standen), die Katalogfilter und Detailblöcke in `HeizkesselStammCtrl`/`HeizkesselCtrl`/`BHKWStammCtrl`/`BHKWCtrl`/`PhotovoltaikStammCtrl`/`PufferSpStammCtrl`/`PufferSpCtrl` sowie die beiden Schreibeinstiege `Ueberschreiben`/`Anlegen` je Katalogeditor. **Mit iU9‑W7** kommen `WPCtrl` (Umzug), `WaermepumpeGeraeteCtrl` (die zweistufige Geräteauskunft Ä22) und die Datenwege der acht Wärmepumpen- und Solarmasken dazu: `WPStammCtrl.KatalogZeilen`/`GesperrtDurchProjekt`/`Speichern`, `KenndatenCtrl.Reihen`/`LiesStamm`/`Abgleichen` (transaktional), `KenndatenKuehlungCtrl.Reihen`/`HatKenndaten`, `WErzeugerCtrl.AnlagenzeileNachziehen`, `KostenSummenCtrl.AnlagenSumme`, `Z_ProjektSolarganglinieCtrl.LiesProjekt` und `SolarkollektorenStammCtrl.IdZu`/`ReadById`. **Mit iU9‑W8** kommen die drei Bedarfsblätter dazu: `BedarfStammCtrl` und `TypProfilCtrl` (neu — EINE Schnittstelle für drei Tabellen mit zwei verschiedenen Schlüsselspalten), die Schreibwege `ProzesswaermeStammCtrl.Exists`/`SaveHead`/`TypIsReadOnly`/`TypNew`/`TypDelete` (sie standen inline in zwei Masken) und die vollständige Gebäudetyp-Verwaltung in `TagVCtrl`. **Mit iU9‑W10a** kommen `PufferSpStammCtrl.Katalogzeilen` (das inline-SQL auf `Tab_Pufferspeicher_STAMM`, das in der Maske stand, Befund W10‑B27) und die drei Serialisierungswege des Quellprofils dazu — `QuellprofilCtrl.MonatswerteParsen`, `MonatswerteText` und `WochenwerteParsen` (W10‑B21). **Mit iU9‑W10b** die fünf Abfragen, die als inline-SQL in der Anzeigeschicht standen (Befund W10‑B35): `WErzeugerCtrl.AnlagenNamen`/`Quellnutzer`/`AnlagenMitWp`, `ErgebnisCtrl.LetzteErgebnisId`, `KlimaregionCtrl.Aussentemperatur`/`KlimazoneJeProjekt`/`KlimazoneJeProjektSchreiben` und **`KonfigurationCtrl.LiesProjekt`**. **Mit iU9‑W11a** kommen vier Controller der Ergebnisseite dazu: `SimulationErgebnisCtrl` (sieben DTO je Erzeuger — die rund 600 Zeilen Fachrechnung, die in `Form_Simulation_Detail` standen), `SimulationLaufCtrl` (`Vorpruefen`/`Bedarf`/`Bestuecken`/`Laufen`/`Abbruchgrund`/`ErgebnisSpeichern` — der Lauf als Kernvorgang, Fehler als RÜCKGABE statt als Dialog), `SpeicherKennzahlenBlock` (die 39 Kennzahlzeilen des Stromspeichers samt `KennzahlStufe` statt vier `Color.FromArgb`) und `SpeicherAnzeigeCtrl` (`BetriebsartText`/`BerechnungsartText`/`AmortisationText` — sie standen dreifach im Oberflächencode). **Mit iU9‑W11b (Anwenderentscheid 04.09.2026 zu W11a‑O‑1)** führen die sechs Summen von `SimulationErgebnisCtrl.Uebersicht` die **DECKUNG** je Erzeuger statt der Produktion — Direktdeckung plus zugerechnete Speicherentladung, je Kanal —, und `RestwaermebedarfMwh` ist dieselbe Zahl wie `RestwaermeMwh`, nämlich `sim.Restwaerme`. Damit gilt „Bedarf − Summe Deckung = Restwärme ≥ 0" per Konstruktion; eine negative Restwärme zeigte eine falsche Zuordnung zu den Erzeugern und darf rechnerisch nicht entstehen. Der Referenzlauf ist unberührt: `BaueErgebnis` schreibt unverändert `sim.Restwaerme`. **`KonfigurationCtrl.LiesProjekt` haben W10b und W11a gleichzeitig gebraucht** — es gibt sie einmal, dazu `ProjektLesen` für Aufrufer, die ein Steuerobjekt füllen. Erweitert sind ausserdem `HeizkesselStammCtrl.BrennstoffartenJeProjekt`, `WErzeugerCtrl.AnlagenJeTyp`/`ModelleJeTyp`/`AnlagenBezeichner` und `StromspeicherStammCtrl.KapazitaetUndLeistung`/`KapazitaetJeProjekt`. **Mit iU9‑W12** kommen drei Dateien der Lastspitzenkappung dazu: `PeakShavingCtrl` (Umzug — er war vollständig oberflächenfrei, und beim Umzug fiel `catch (OleDbException)`, das seit der SQLite-Umstellung ins Leere lief, Befunde W12‑B23/B25), `PeakShavingKennzahlenBlock` (18 Kennzahl- und 12 Monatszeilen, Muster `SpeicherKennzahlenBlock`) und `PeakShavingEingaben` (die vier Prüfregeln und die vier Einheitenumrechnungen von `ParameterLesen` — Fachaussagen, die iOS sonst ein zweites Mal hätte). Dazu `Z_ProjektStromganglinieCtrl.LiesProjekt` und `StromganglinieStammCtrl.FindeStamm` für die drei konkatenierten Abfragen der Oberfläche (W12‑B4). **Mit iU9‑W14a** bekommt die KATALOGVERWALTUNG ihre Datenseite: `KatalogsatzAnzeige` in fünf Stamm-Controllern (die sieben inline-SQL-Stellen der Browser, Befund W14‑B12), `SolarkollektorenStammCtrl.KatalogZeilen` und `StromspeicherStammCtrl.KatalogZeilen`, die zwei Speicherwege `AnzeigefelderSchreiben` (Heizkessel mit Dublettenklammer, BHKW mit Schreibschutzfrage), `BHKWStammCtrl.IstSchreibgeschuetzt`, die drei Schreibeinstiege `PufferSpStammCtrl.Anlegen`/`Ueberschreiben`/`Loeschen` sowie `PhotovoltaikStammCtrl.SpeichernAus`/`Loeschen` und `StromspeicherStammCtrl.SpeichernAus`/`Loeschen` — alle mit `SpeicherErgebnis` statt einer `MessageBox` (Befunde W14‑B22/B33/B42/B47). **`PufferSpStammCtrl.SpeichertypAbbildung`** (W14a.0d) trägt die drei DB-Werte, die drei eingefrorenen englischen Altwerte des Befunds L0‑1 und die beiden Wege `SpeichertypIndex`/`SpeichertypDbWert`. **`HeizkesselStammCtrl.Filtern` ist mit W14a.0b berichtigt** (Befund W14‑B2): `Fernwärme=23`, `Sonstige Energieträger=24`, `Wasserstoff=25` statt des nie treffenden `"Sonstige"=23` — der Kommentar „W6‑O‑1" ist damit geschlossen, und die Heizkessel-Liste ändert sich auch im schon portierten `HeizkesselDialog`. **Mit iU9‑W14b** kommt `BedarfsVorschauCtrl` dazu — die Rechnung hinter dem Knopf „Grafik“ der drei Bedarfsverwaltungen, die dreimal im Formularcode stand und sich in genau vier Punkten unterschied (Simulationsklasse, Engine-Methode, Teiler, Nachlauf; alle vier hängen an `BedarfsArt`). Sie ist **bitgleich je Art** — einschließlich des fehlenden Teilers beim Brauchwasser (Befund W14‑B49): Der Wert liegt in kWh, und genau so nennt ihn die Ergebnishülle seit dem Entscheid W8‑O‑5. Erweitert sind `BedarfStammCtrl` (`Bezeichner`, `Kopf`, `Loeschen` mit dem Aufzählungstyp `BedarfLoeschErgebnis` — die drei `Delete` der Stammcontroller MELDEN ihre ReadOnly-Sperre, und das wäre in einer WebView ein modaler Kasten) und `SolarganglinieStammCtrl` (`Exists` statt der Präfixsuche `listBox.FindString`, Befund W14‑B70; `HatProjektzuordnung` statt des verketteten inline-SQL, W14‑B12). **Mit iU9‑W14c** kommen ZWEI Controller dazu: **`KlimaregionStammCtrl`** (Umzug — er zog über `FillComboBox(ComboBox)`/`FillListBox(ListBox)` `System.Windows.Forms` in die Controllerschicht, Befund W14c‑B33; an ihre Stelle tritt `Bezeichner()`, `ReadSingle(sql)` wird das parametrierte `ReadByName(name)`, und `Delete` löscht seither MIT Kaskade über `KatalogBereinigung.SatzLoeschen` — der Vorläufer liess 8 760 + 365 Zeilen als Waisen stehen, Befund W14c‑B23) und **`EinstellungenCtrl`** (neu — der ERSTE schreibende Weg zu den neun `Properties.Settings`-Schlüsseln ausserhalb einer Maske, Befund W14c‑B57; die vier Vorgabepfade laufen über `Dienste.Pfade` statt `Environment.GetFolderPath(SpecialFolder…)`, das hier verboten ist, W14c‑B55). Erweitert ist `GesetzKatalog` um `KlassenVorrat`/`Einheiten`/`Statuswerte`/`KlasseAnzeige`/`WertText` (die Steuerwertlisten standen als ZWEITE Quelle in der Maske, W14c‑B5), `Pruefe`/`Existiert` (dieselbe Prüfung stand zweimal, W14c‑B7; die Dublettenprüfung ist jetzt eine SQL-Zählung statt eines Katalognachladens, W14c‑B12) und `Zeilen(klasse)` mit `GesetzZeile`; `SolardatenCtrl.ReadAllStamm` ist auf `DbParam` umgestellt (W14c‑B18b). **Mit iU9‑W15a kommt `ProjektExportImportCtrl` dazu** (1 278 Z., Umzug — seine einzige Kante war die Zahl `SchemaMigration.ZIEL_VERSION`, siehe `SchemaStand.Zielversion`; damit ist der Projekttransfer auf iOS moeglich). Erweitert sind `ProjektCtrl` (`IdVonName`, `NamenListe`, `Kopf`, `LoeschenMitVorarbeiten` — die sechs Schritte des Loeschwegs aus `MenueCtrl` ohne die zwei Dialogaufrufe, Befund W15a‑B48/B50), `ProjektDuplizierenCtrl` (`PruefeNamen` als abfragbares Ergebnis statt der Praefixsuche der Maske, W15a‑B10; `VerwaltungsfelderSetzen` mit unveraenderter Fehlerpolitik, W15a‑B11/B47; `Duplizieren` mit `CancellationToken` und Rollback) und `KlimaregionStammCtrl` (`IdVonName`, `NameZuProjektregion` mit STAMM-Rueckfall — die drei verketteten `RecordSet`-Abfragen von `Wizard_Projekt`, W15a‑B32). **Mit iU9‑W9.8 kommt `GebaeudeBedarfCtrl` dazu** — der Wärmebedarf EINES Gebäudes hinter dem Knopf „Simulation…" des Gebäudedialogs (Anwenderwunsch W9‑E‑2, 05.09.2026). Er RUFT den Rechenweg des Laufs, statt ihn abzuschreiben: `SimulationWaermebedarf.KlimakalenderLesen` und `…HeizwaermeEinesGebaeudes` sind aus `Waermebedarf_berechnen` herausgezogen und werden von beiden Seiten gerufen — siehe die Hausregel weiter unten |
| `Model/` (54) | alle Modelle; seit iU9‑W8 der Aufzählungstyp `BedarfsArt` — er liegt hier und nicht in `EPOS.UI`, weil ihn BEIDE Seiten brauchen: Die Controller verteilen danach auf drei Tabellen, die Razor-Komponenten wählen danach ihre Beschriftungen. Seit iU9‑W10b dazu `AnlagenInfo` — die Zeile aus `Tab_Energieanlagen` samt ihrer Senkenkette, bis dahin eine PRIVATE Klasse in `Form_Simulation_Config`. **Seit iU9‑W15a** `ProjektAngaben.cs` mit `ProjektKopfZeile` (die EINE Projektliste — der Bestand fuehrte VIER, Befund W15a‑B52), `ProjektKopfDaten` (die neun Felder der ersten Assistentenseite statt zehn `Get*`-Methoden, W15a‑B42) und den drei Befundtypen `DuplizierBefund`, `VerwaltungsfelderBefund` und `LoeschBefund`/`LoeschStand` |
| `Controller/` — Nachtrag iU9‑W15c | **Drei Controller der Lizenzseite**: `LizenzCtrl` (das Lagebild der Lizenzverwaltung — sprachneutraler Zustandsname, Statustext, Detailtext, `HatToken`, `GeraetName`, `PortalUrl`, die WordPress-E-Mail-Regel `EmailGueltig` und die vier `await`-Wege als Tupel statt als Kern-Antworttyp), `LizenzTextCtrl` (woher der Vertragstext kommt: Dateisuche über `Dienste.Pfade`, Zwischenspeicher, Onlineabruf, `HtmlZuText`, `StandFormatieren` — **die Quelle ist EINE Zeile**, heute die AGB-Seite, siehe Befund W15c‑B27) und `ZustimmungCtrl` (die Zustimmung beim ersten Start über `Dienste.Einstellungen`, unter Windows derselbe Registry-Zweig wie vorher; **Fehlerpfad `catch → true`, wortgleich übernommen** — eine nicht lesbare Ablage darf den Start nicht blockieren) |
| `Controller/` — Nachtrag iU9‑W16b | **Zwei Controller der Startseite** und ein Zustand. **`ProjektKontextCtrl`** (K2, W16b.0 — das gerade geöffnete Projekt: Id, Name, Klimazone, `Setzen`/`Uebernehmen`, Ereignis `Gewechselt`, `Tab_Applikation`. Er ist seit W16b.3 `Dienste.Projekt`; bis dahin war es `FormStartProjektKontext`, eine Fassade auf ein FELD der Startmaske (Befund W16‑B6). `Setzen` und `Uebernehmen` sind getrennt, weil der Bestand sie unterscheidet: Die drei Projektkacheln merken sich das Projekt, der Variantenwechsel im Kopfband und die Menüwege „Neu"/„Bearbeiten" nicht. Nachweis **N7** in `EPOS.Kern.Tests/ProjektKontextCtrlTests.cs`, einschließlich des Projektwechsel-Falls zu Risiko R‑W16‑4. **Die Klimazone ist seit dem Anwenderentscheid W16b‑O‑3 vom 04.09.2026 auf BEIDEN Plattformen die PROJEKTKOPIE** — `StartseiteCtrl.ProjektKlimazone`; damit gibt es für „welches Projekt ist offen" **eine** Umsetzung, und `EPOS.iOS/Dienste/IosProjektKontext` ist nur noch eine dünne Weiterleitung auf DIESE Klasse. N7 zählt dafür **15 statt 12 Fälle**). **`StartseiteCtrl`** (K4, W16b.0 — Klimaregionen lesen und speichern, Variantengruppe, Projektname; die vier Abfragen von `Form_Start` (:356/369/382/390) standen hier parametriert, die verkettete bei :369 läuft über `KlimaregionStammCtrl.IdVonName`, Befund W16‑B11. **Seit W16b‑O‑3 sind es drei**: `:356` — der Regionsname zur STAMM-Id, zuletzt `KlimaregionName(int)` — ist **ersatzlos gefallen**. Der Entscheid lautete „nehme iOS-Lösung"; die Messung dazu zeigte, dass die iOS-Abfrage **den falschen Schlüsselraum las**: An `Tab_Projekt.ID_Klimaregion` steht die Id der PROJEKTKOPIE (`Tab_Klimaregion.ID`, Ids ab 1 006 017), die Abfrage hielt sie gegen `Tab_Klimaregion_STAMM.ID_Klimaregion` (Ids 1…50) — Überschneidung **0**, Antwort für jedes Projekt des Bestands leer. Es war kein zweiter Weg, sondern ein Fehler, und `KlimaregionName` hatte seit K6‑a ohnehin keinen Aufrufer (Befund W16b‑B3). Vereinheitlicht ist deshalb auf die Projektkopie: **`ProjektKlimazone`** (der frühere `ProjektKlimaregion`, umbenannt weil er jetzt die EINE Wahrheit beider Plattformen ist) liest `:382` und `:390` unverändert und **ohne Stamm-Rückfall**; der angezeigte Text ist derselbe wie vorher. Messung im W16b-Protokoll § 6). Dazu **`Allgemein/Simulation/BedarfsZustand`** (W16b.4, E‑5 — die zwei Bedarfsrechnungen eines Projekts. `Form_Start` besaß sie als zwei Felder und reichte sie an die Ergebnisansicht durch; genau das war der Grund für deren Modalität, Befund W11‑B3/W16‑B29. Sie gehören jetzt dem PROJEKT und werden bei einem Wechsel verworfen) |
| `Controller/` — Nachtrag iU9‑W16a | **Drei Controller des Projektassistenten.** `KomponentenBestandCtrl` (K1, W16a.0 — **unverändert verschoben** aus `Views/Wizard/KomponentenBestand.cs`: die dreizehn Bitwerte, `Bitmaske`, `NachSeite`, `Lesen`; sie war reine Datenlogik ohne eine Zeile WinForms. Nachweis N6 in `EPOS.Kern.Tests/KomponentenBestandTests.cs`: bitgleich zum eingefrorenen `Form_Start.status`-Wert für **alle dreizehn** Referenzprojekte — damit ist Entscheid E‑3 belegt, nicht nur behauptet). `WizardCtrl` (W16a.4, Umzug — siehe oben). **`AssistentCtrl`** (K3, W16a.4 — die sieben Zustandslisten, die sechs Ladewege mit ihren sechs Inline-SQL, die Seitenschaltung `NaechsteAktive`/`LetzteAktive`, die beiden Filter und `Speichern` mit **bitgleicher** Reihenfolge der Schreibschritte. Neu ist allein, dass ein Fehlschlag GEMELDET statt verschwiegen wird: `AssistentErgebnis` nennt den Schritt, und der Aufrufer zeigt EINE Meldung — der Vorläufer brach siebzehnmal kommentarlos ab, Befund W16‑B16, Entscheid E‑4. **Seit W16a‑O‑1 ist auch die TRANSAKTION da**: `Speichern` öffnet EINEN `DbVorgang`, reicht ihn in alle 23 Schreibmethoden von `WizardCtrl` hinein — jede meldet ihn über `Vorgangsklammer` am Faden an, damit auch die Katalogcontroller darunter darin arbeiten — und schreibt ihn erst fest, wenn alle Schritte gelungen sind; jeder Fehlschlag rollt den ganzen Lauf zurück. Die Reihenfolge der Schreibschritte ist dabei Zeichen für Zeichen dieselbe geblieben. **Risiko R‑W16‑6 bleibt offen**: Den Feld-für-Feld-Vergleich am Windows-Gerät (`Referenzlauf.exe projekt`) kann nur der Anwender fahren; auf Linux belegen zwei Kern-Prüffälle in `AssistentCtrlTests` den Rückzug und den unveränderten Erfolgsfall) |
| `Controller/` — Nachtrag Stromspeicher (11.09.2026) | **Acht Dateien der Speicherflotte**, die Naht zwischen `SpeicherEngine`/`SpeicherPlanung` und dem Projektlauf. **`SpeicherFlottenStudieCtrl`** (der Kandidatenlauf: Projektquellen aufbereiten, Preise **genau einmal** ct → EUR wandeln, die Studie rechnen und als `SpeicherFlottenErgebnis` freigeben — im Kandidatenlauf ohne Datenbank). **`SpeicherFlottenProjektCtrl`** (der freigegebene Kandidat als reservierter Stand `@Projektflotte` im gewöhnlichen Projektlauf, mit `IstAktiv`, der Zulässigkeitsprüfung und der Fabrik **`PlanerFactory`**; sie ist die EINZIGE Stelle, an der ein `IFlottenPlaner` in den Kern kommt — siehe die OR-Tools-Regel in der [`CLAUDE.md` der Repo-Wurzel](../CLAUDE.md)). **`SpeicherAuslegungCtrl`** mit der zweiten Hälfte **`SpeicherAuslegungCtrl.Rechnung`** (die Auslegungsprofile in `Tab_SpeicherAuslegung` samt der DDL des Schemaschritts 73 — und `Vorbereiten`, das Zeitreihen, Kosten und Speicherparameter **vor** dem Hintergrundlauf zu EINEM unabhängigen Eingabestand einfriert) und **`SpeicherAuslegungModel`** (die Verträge dazu ohne jeden Datenbankzugriff: `SpeicherAuslegungQuelle`, `SpeicherKostenQuelle`, `SpeicherKostensaetze` — Kapazität und entladene Energie bleiben **getrennte Mengenbasen**). **`SpeicherZeitreihenImport`** (CSV-Zeitreihen je Rolle Last/PV/Bezug mit Einheiten- und Kodierungswahl) und **`SpeicherFlottenCsvImport`** (ganze Projektjahre und nachweislich bekannte Prognose-Snapshots mit ausdrücklicher Spalten-, Zeit- und Einheitenzuordnung). **`SpeicherFlottenAnzeigeCtrl`** liest **ausschließlich** den fertig gerechneten Lauf und baut daraus Bilder und CSV — er rechnet nichts nach. Erweitert sind `StromspeicherSimCtrl` (`AlsErgebnismodell`, `ProjektNetzbilanz`), `SpeicherOptimierungCtrl` und `SpeicherParameterPruefung`. Fachgrundlage: [`Doku_Mehrspeicher_Konzept_und_Umsetzung.md`](../Doku_Mehrspeicher_Konzept_und_Umsetzung.md) |
| `MyResource/` | `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` — der Anzeigetext-Katalog beider Sprachen |
| `Properties/` | `Settings.settings`, `Settings.Designer.cs`, `Settings.cs` |

`RecordSet` ist seit iU6-T1 ein reiner vorwärtslaufender Zeilenzeiger: `DBCommand`, `_cmd`,
`MerkeSql()` und `Parameter()` sind ersatzlos gestrichen (iR8 — repositoryweit gab es **0**
externe Nutzer). Wer parametrisiert arbeiten will, nimmt `DataRepository` oder `DbVorgang`.

Verlinkt statt verschoben ist genau eine Datei: `../sql/schema/SchemaTypKatalog.g.cs` — ihre
Quelle ist `sql/tools/Erzeuge-Schema.ps1`, nicht dieses Projekt.

## Was mit Absicht NICHT hier liegt

Nach dem zweiten Umzug (iU5-U1…U5) sind es noch **62 Dateien** unter
`../WindowsFormsApplication1/Allgemein/` (42) und `../WindowsFormsApplication1/Controller/` (20).
Jede steht auf dieser Liste, weil der Kernbau sie ablehnt — nicht, weil sie übersehen worden wäre:

| Was | Warum |
|---|---|
| `BaseForm`, `FensterEinpassung`, `GrafikTools/*`, `Hilfe/HilfeAutomatik`, `Hilfe/InfoKnopf`, `Hilfe/HelpCatalog` (mit `HelpExtender`), `Views/Help/Form_HelpPopup` | Oberflächenbausteine — WinForms und GDI+. **`Form_Hinweis` ist mit iU9‑W16b.3 GELÖSCHT** (Entscheid W15b‑E‑1b eingelöst): Sein Nachfolger `Warnbanner.Verfaellt` war seit W15b.1 gebaut und geprüft, seine drei Aufrufer lagen sämtlich in `Form_Start` — und die ist mit derselben Teilwelle gefallen. `SpeichernLeiste` fiel mit W14a. **`Form_HelpPopup` bleibt bis iU11** (Entscheid E‑2): Sein Ersatz ist nicht eine Razor-Fassung, sondern `IHilfeDienst` mit Windows- und iOS-Fassung — beide gebaut; die Maske fällt mit `HelpCatalog`/`HelpExtender`. Die Zeichenrechnung `BeschreibungUmbrechen` ist mit W15b.0e als `Allgemein/Hilfe/Kurzbeschreibung` in den Kern gezogen |
| `Blazor/BlazorDialogForm`, `Blazor/BlazorDienste`, `Hilfe/WindowsHilfeDienst` | die Blazor-Hülle selbst (iU8-6/iU8-7): ein modales `Form` mit `BlazorWebView`, sein Dienstverzeichnis und die Windows-Fassung von `EPOS.UI.Dienste.IHilfeDienst`. Sie **sind** die Oberfläche und können nie in den Kern |
| `Update/SchemaMigration`, `GeraeteWaisen`, `SchemaVersionAccess`, `DbParamOleDb` | der eingefrorene Access-Zweig — `System.Data.OleDb`. **`ErststartMigration` ist mit W3 (#157‑E‑1, 09.09.2026) GELÖSCHT**: Der Übernahme-Assistent im Programmstart ist gefallen, der Rest ist Hauswerkzeug (`EposSqliteMigrator`, `SchemaMigration.HebeAltbestand`) |
| `Bericht/BerichtsDatenSammler` | `EnergieMengen` aus `Views/Varianten/` |
| `KI/KiDialogZugriff`, `KiAusfuehrer`, `HilfeKontext` | greifen auf lebende `Control`/`Form` zu. `KiAufrufKnopf` ist mit iU9‑W14a gefallen und mit iU9‑W15b.5 durch den Baustein `KiKnopf` ersetzt |
| `KI/KiAktionen` (trägt `KiHilfe`), `KiAktionenDialog`, `-Energie`, `-Lastgang`, `-Projekt`, `-Schreiben`, `-Sitzung`, `-Uebernahme`, `-Wirtschaft` | hängen an den obigen, an `HelpEntry` oder an `OleDbException`. **`KiChatService` steht seit iU9‑W15b.0a HIER im Kern** (Befund W15b‑B1: 1 751 Zeilen ohne einen einzigen WinForms-, `Program.`-, `Registry`-, DPAPI- oder `SpecialFolder`-Bezug); die Naht zur Ausführungsschicht ist `IKiAusfuehrung`/`KiAusfuehrungsweg` |
| ~~`StromTestClass`~~ | **mit iU9‑W16b.1 GELÖSCHT** (Anwenderentscheid E‑7, K6‑a): Ihr einziger Nutzer war `Form_StromTest`, ein Prüfstand im Auslieferungsstand (Befund W16‑B31). **`IAssistentRahmen` gibt es seit iU9‑W16a.5 nicht mehr**: Der Assistentenrahmen ist eine Razor-Seite, und sie reicht ihren Zustand als Delegat herein, statt dass die Seiten ihn sich über einen statischen Halter holen |
| ~~die 12 `*KontextMenuCtrl`~~ | **mit iU9‑W16b.1 GELÖSCHT** (E‑7, K6‑a, 2 381 Zeilen): Ihr einziger Erzeuger war das Detailformular `FormMain` (Befund W16‑B28); mit ihm fallen `Gewerke`, `INavigation.OeffneGewerk` in allen drei Fassungen und `Masken.ProjektDetail` |
| `KlimaregionStammCtrl` | `ComboBox`/`ListBox` in `FillComboBox`/`FillListBox` |
| `MenueCtrl` | die Windows-Navigation (`Dienste.Navigation`, `Program.rahmen` — bis Entscheid E‑10 vom 04.09.2026 `Program.mdifrm`) |
| `EnergietraegerKatalogCtrl` | `EnergyCarrier`, deklariert in `Views/Kosten/Form_Kosten.cs` |
| ~~`WizardCtrl`~~ | **ist seit iU9‑W16a.4 HIER.** Seine einzige WinForms-Kante war das Feld `public WizardParent parentform` mit genau EINEM Schreiber (`WinFormsNavigation:258`) und KEINEM Leser im ganzen Bestand (Befund W16a‑B2); ohne es enthält die Klasse keine Zeile Oberfläche. Der Aufräumlauf `GeraeteWaisen.Aufraeumen` läuft seither über den Haken `WErzeugerCtrl.GeraetewaisenAufraeumen` — dieselbe Brücke, die iU4‑2 für `WErzeugerCtrl.Delete` angelegt hat. Erst danach konnte **`AssistentCtrl`** (K3) überhaupt entstehen: Der Assistent RUFT diesen Schreibweg |
| ~~`PeakShavingCtrl`~~, ~~`ProjektExportImportCtrl`~~ | **beide sind inzwischen HIER**: `PeakShavingCtrl` mit iU9‑W12 (sein `catch (OleDbException)` lief seit der SQLite-Umstellung ins Leere), `ProjektExportImportCtrl` mit **iU9‑W15a** — seine einzige Kante war die Zahl `SchemaMigration.ZIEL_VERSION`, und die steht seither als `SchemaStand.Zielversion` im Kern (Befund W15a‑B30). Damit ist der **Projekttransfer auf iOS** ueberhaupt erst moeglich |

Ebenfalls dort, aber keine Quelldatei: `Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx`. Sie
wird über `<None Update … CopyToOutputDirectory>` neben die EXE gelegt, und genau dort sucht sie
`WordBerichtGenerator.FindeVorlage()` (`AppDomain.CurrentDomain.BaseDirectory`).

**Die `partial`-Falle.** Vor jedem weiteren Umzug prüfen, ob die Klasse noch eine zweite Hälfte
in der Anwendung hat. `SimulationControl` liegt mit beiden Hälften hier; `WPCtrl` lag mit beiden
dort, bis iU9‑W7.0a seine WinForms-Hälfte STRICH — `WPCtrl.WinForms.cs` trug genau eine Methode
(`FillListBox(ListBox)`), und die hatte im ganzen Bestand keinen Aufrufer. Erst danach konnte
die Klasse hierher; dazwischen gibt es nichts.

## Regeln für Änderungen hier

**Kein WinForms-Code, kein `System.Data.OleDb`.** `EnableWindowsTargeting=false` ist der
Wächter: Jede WinForms-Berührung bricht den Build sofort, nicht erst zur Laufzeit auf dem
iPad. `System.Data.OleDb` ist seit **iU6** ganz weg — kein `using`, kein Typ, keine
`PackageReference`. **CA1416 steht bei 0** (Verlauf 87 → 78 → 0);
**kein `NoWarn`**, damit eine neu hereingetragene Windows-API sofort als Warnung auffällt.

**Die Pakete des Kerns** — alle plattformfrei, Fassungen zentral in `Directory.Packages.props`:

| Paket | Wofür | Seit |
|---|---|---|
| `Microsoft.Data.Sqlite` | Zugriffsschicht | iU4 |
| `System.Configuration.ConfigurationManager` | `Properties\Settings` erbt von `ApplicationSettingsBase` | iU4 |
| `SkiaSharp` (+ die bedingten Nativen) | `ChartRenderer` | iU7-5 |
| `BouncyCastle.Cryptography` | Ed25519-Prüfung in `LizenzToken` | iU5-U1 |
| `ClosedXML` | Excel — lesend in `GanglinienDatei`, schreibend im `ExcelBerichtGenerator` | iU5-U1 |
| `Microsoft.ML.OnnxRuntime`, `Microsoft.ML.Tokenizers` | `SemantikModell` | iU5-U2 |
| `DocumentFormat.OpenXml` | `WordBerichtGenerator` | iU5-U3 |
| `SixLabors.Fonts` | Spaltenbreiten für ClosedXML; **auf 1.0.1 gepinnt** (ab 2.x gilt die Six-Labors-Split-Lizenz) | iU5-U3 |

Dazu zwei `ProjectReference`: `SpeicherEngine` (iU4) und `KiKern` (iU5-U2, UI- und DB-frei,
ohne eigene Pakete).

**Kein iOS-Sonderpaket mehr (iU10-1).** Bis iU10 stand hier eine bedingte `PackageReference` auf
`SQLitePCLRaw.bundle_green` für die Ziele `net10.0-ios`/`net10.0-maccatalyst`. Sie ist gestrichen:
Die Fassung 2.1.12 gibt es nicht (`bundle_green` endet bei 2.1.11, NU1102), `bundle_e_sqlite3`
lädt auf iOS ohnehin nichts dynamisch (`provider.internal`, statisch gelinkte `e_sqlite3.a`), und
die System-SQLite des Geräts wäre für die **118 STRICT-Tabellen** der Datenbank nicht steuerbar
(114 zur Zeit des Entscheids; 117 nach den Schritten 65/66, 118 seit Schritt 74 vom 11.09.2026).
Der Kern bekommt auch kein zweites `TargetFramework` — die iOS-Hülle `EPOS.iOS` referenziert ihn
als `net10.0`-Bibliothek und zieht ihre Nativen selbst.

**Datenzugriff ausschließlich über `DataRepository` mit `new DbParam(…)`.** `DataRepository`
ist seit iU6-T4 eine **Fassade**: Die Arbeit macht `SqliteDatenzugriff` hinter
`IDatenzugriff` (sechs Ausführungs-, fünf Schemamethoden, `DatenbankVorhanden`,
`DatenbankPfad`). Für die rund 160 Aufruferdateien ändert das nichts — Signaturen,
Fehlerwortlaute und Rückgabewerte im Fehlerfall sind dieselben. Auf der Fassade bleiben mit
Absicht: der Engine-Modus (`FehlerMelden`, `EngineModus`, `StilleFehlerAbholen` — eine
Meldeentscheidung für das ganze Programm), die Pfadauflösung (`PfadUeberschreibung`,
`GetDBPath` — bekommt in iU5 ihr `IPfade`; **`PfadUeberschreibung` schlägt alles**, der
Referenzlauf hängt daran) und die vier Bequemlichkeiten (`GetMaxID`,
`DeleteWithDependencies`, `GetIdByName`, `GetValueById`).

**Die Brücke nach OleDb steht in der ANWENDUNG**, nicht hier:
`WindowsFormsApplication1/Allgemein/DbParamOleDb.cs` (`Aus`, `Von`, `Nach`,
`[SupportedOSPlatform("windows")]`). Getragen wird sie nur noch vom eingefrorenen
Access-Zweig der Schemapflege — `SchemaMigration.HebeAltbestand`, `GeraeteWaisen` und
`SchemaVersionAccess` (die aus `ApplikationCtrl` ausgelagerten Schemamarker-Methoden).
**Seit W3 (#157‑E‑1, 09.09.2026) ist das ein HAUSWERKZEUG, kein Kundenweg**: Der
Erststart-Assistent samt `ErststartMigration` ist gelöscht, die Anwendung übernimmt
keinen Access-Altbestand mehr. Wer hier eine neue Zugriffsstelle schreibt, nimmt
`DbParam` — sonst nichts.

**Die Datenbank einer Neuinstallation entsteht nur über
`Allgemein/Datenbank/Erstbereitstellung.Sicherstellen`** (Anwenderentscheid `#157‑E‑1`,
Weg W3, 09.09.2026). Sie kopiert die ausgelieferte Vorlage
(`Dienste.Pfade.Auslieferungsvorlage` → `{app}\Vorlage\Kenndaten.sqlite`) in den
Datenordner, prüft danach `PRAGMA integrity_check` und `Tab_Applikation.SchemaVersion`
und **überschreibt nie** eine vorhandene Datei; bei jedem Fehler räumt sie die halb
angelegte Zieldatei wieder weg. Gerufen wird sie aus `Program.DatenbankBereitstellen()`
vor `DataRepository.DatenbankVorhanden()`; die iOS-Schale fährt denselben Gedanken für
ihr Anwendungspaket (`EPOS.iOS/Datenbankbereitstellung.cs`). Wer eine zweite Stelle
schreibt, an der eine Datenbank entsteht, baut den zweiten Auslieferungsweg.

**Sicherungskopien nur über `Allgemein/Datenbank/Datenbanksicherung.KopieAnlegen`**
(Auftrag #158). Sie zieht die Kopie über eine geöffnete SQLite-Verbindung (`VACUUM INTO`,
`BETRIEB_SQLITE.md` § 3.2) statt über `File.Copy` — unter WAL fehlen einer reinen Dateikopie
der Hauptdatei die noch nicht eingecheckpointeten Änderungen aus der `-wal`. Beide Aufrufer
(`KiSicherungspunkt`, `MenueCtrl.DatenbankKopieAnlegen`) nutzen sie; eine neue Sicherungsstelle
schreibt kein zweites `File.Copy`, sondern ruft diesen Helfer.

**Die Umgebung ausschließlich über `Dienste.*` — nie über `Program.*`.** Seit iU5 (03.09.2026)
liegen neun Umgebungsdienste in `Allgemein/Dienste/`. Neuer Kerncode, der eine Meldung absetzt,
einen Ablageort braucht, eine Einstellung liest, die Sprache kennen will oder eine Maske öffnen
soll, ruft `Dienste.Dialog`, `Dienste.Datei`, `Dienste.Pfade`, `Dienste.Einstellungen`,
`Dienste.Lizenzablage`, `Dienste.GeraeteId`, `Dienste.Sprache`, `Dienste.Navigation` bzw.
`Dienste.Projekt`. **`Program.*` ist im Kern und in allen Kernkandidaten verboten** — der Wächter
steht unten unter „Nachweis".

| Dienst | Wofür | Vorbelegung ohne Oberfläche |
|---|---|---|
| `Dialog` | Meldung, Warnung, Fehler, Rückfrage, Dreifachwahl, Wartekurve | `StilleDialoge` — Konsole; Rückfrage = nein |
| `Datei` | Datei-/Ordnerwahl, Öffnen mit der Systemanwendung | `KeineDateiwahl` — `""` bzw. `false` |
| ↳ *wartbare Zwillinge* | `DateiOeffnenAsync`, `DateiSpeichernAsync`, `OrdnerWaehlenAsync`, `MeldungAsync`, `WarnungAsync`, `FrageAsync` — **für Aufrufe aus einem Blazor-Ereignis** | Standardfassung in der Schnittstelle: fällt auf die synchrone Form zurück |
| `Pfade` | `%APPDATA%\wp-plan`, `%APPDATA%\<Produkt>`, `LocalApplicationData[\WP-Plan]`, `CommonApplicationData\WP-Plan`, Dokumente, **`Herstellerdaten`** (`VDI-3805-Daten` neben dem Programm) und **`Auslieferungsvorlage`** (`Vorlage\Kenndaten.sqlite` neben dem Programm, W3) | `StandardPfade` — `Environment.SpecialFolder`; die zwei Auslieferungspfade über einen Aufstieg von `AppContext.BaseDirectory` (installiert Stufe 1, im Entwicklungsstand `Setup\Vorlage\`) — **keine Windows- und keine iOS-Sonderfassung nötig** |
| `Einstellungen` | Schlüssel-Wert-Ablage, dazu ein maschinenweiter Leser | `FluechtigeEinstellungen` — Wörterbuch im Speicher |
| `Lizenzablage` | Geheimnisse; Geltungsbereich Gerät **oder** Benutzer als Parameter | `KeineAblage` — merkt nichts |
| `GeraeteId` | Gerätemerkmale für die Lizenzbindung | `KeineGeraeteId` — leer |
| `Sprache` | Kürzel, `IstEnglisch`, Umschalten | `StandardSprache` — hält `Sprache.Nummer` |
| `Navigation` | Gewerksliste auffrischen, Maske öffnen, Ansicht auffrischen | `KeineNavigation` — Leerlauf, `OeffneMaske` = `false` |
| `Projekt` | das offene Projekt (Id, Name, Klimazone, Wechsel) | `LeererProjektKontext` — `Vorhanden` = `false` |

Belegt werden alle neun an genau EINER Stelle: `Program.Main`, vor
`DataRepository.DatenbankVorhanden()`. Die Windows-Fassungen liegen in
`../WindowsFormsApplication1/Dienste/`. Ein Prüfstand tauscht ein Feld, fährt seinen Fall und legt
die Standardfassung zurück (`EPOS.Kern.Tests/DiensteTests.cs`).

**Maskennamen und Gewerke sind sprachneutrale ASCII-Schlüssel** (`Gewerke.Bhkw`,
`Masken.PufferSpAdmin`, `Ansichten.Varianten`) nach der Drei-Schichten-Regel — nie ein
Anzeigetext.

**Meldungen und Oberflächenaufgaben über Haken.** Das ältere Muster, das weiterhin gilt: ein
`static Action<…>`-Feld hier, belegt von `Program.Main` in der Anwendung, mit einer folgenlosen
oder auf die Konsole schreibenden Vorbelegung.

| Haken | Wofür | Vorbelegung |
|---|---|---|
| `Meldung.Zeigen` / `.Hinweis` / `.Warnung` / `.Warten` | Dialog statt `MessageBox.Show` bzw. Sanduhr | **seit iU5 `Dienste.Dialog`** — ohne Oberfläche damit Konsole, `Warten` folgenlos. `Program.Main` belegt diese vier Haken **nicht mehr** |
| `SimulationControl.Speicherlauf` | der Stromspeicherzweig (K8) | wird von `SimulationControl.StromspeicherzweigEinhaengen` in `SimulationControl.Stromspeicher.cs` gesetzt — ausdrücklich, als erste Anweisung von `Do_Simulation`. Bis zum Stromspeicher-Sync hing die Belegung an einem `[ModuleInitializer]`; in einer Bibliothek ist das die Bauart, vor der CA2255 warnt, und unter AOT (iOS) weder vorhersagbar noch beweisbar |
| `SimulationRunner.Speicherergebnismodell` | dasselbe für das Ergebnismodell | wie oben |
| `WErzeugerCtrl.GeraetewaisenAufraeumen` | Aufräumlauf nach dem Löschen eines Projekts | `null` = kein Lauf; zulässig, weil er ohnehin nach dem erfolgreichen DELETE läuft und der Migrationsschritt nachholt |
| `DataRepository.Zugriff` | die Umsetzung hinter `IDatenzugriff` (iU6-T4) | `new SqliteDatenzugriff()`; wird in iU5 an `Dienste.Daten` gehängt |

**ResX und Settings pflegen.** Der Anzeigetext-Katalog liegt jetzt hier; `Resource.Designer.cs`
ist **eingecheckter, erzeugter Quelltext**. Der `LogicalName` beider `.resx` ist im `.csproj`
festgeschrieben (`WindowsFormsApplication1.MyResource.Resource[.en-US].resources`), damit der
Ressourcenname nicht am Ordnerpfad hängt — der Basisname in `Resource.Designer.cs` bleibt dadurch
gültig. **Keine der zwei `.resx` trägt einen Code-Generator** (abgeklemmt am 06.09.2026): Visual
Studio schrieb die Designer-Datei bei jeder `.resx`-Änderung neu und wich damit vom eingecheckten
Stand ab — beim Anwender blockierte die „lokal geänderte" Datei jeden `git pull`. **Die Datei wird
nie von Hand ergänzt und nie von Visual Studio erzeugt, sondern nur neu geschrieben:**
`python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben` erzeugt sie vollständig aus der
neutralen `.resx` im Format des StronglyTypedResourceBuilder (alphabetisch, XML-Escapes, BOM/LF);
ohne Argument prüft es nur („abweichend 0" ist die Abnahme). Am 06.09.2026 hingen 314 Schlüssel
ohne Eigenschaft nach — seither ist das Werkzeug der einzige Weg; wer parallel von Hand ergänzt,
baut Duplikate (CS0102).

**`InternalsVisibleTo`.** Etliche Typen sind ohne Zugriffsangabe deklariert und damit `internal`
(`ProjektCtrl`, `KlimaregionCtrl`, `WPStammCtrl`, `Properties.Settings`, `Init` …). Das `.csproj`
gibt sie für `EPOS_Plan` und `EPOS.Kern.Tests` frei. Neue Typen brauchen deshalb **keine**
Sichtbarkeitsanhebung, nur weil die Anwendung sie sieht.

**Namespace bleibt `WindowsFormsApplication1`** — die Umbenennung ist eine eigene Entscheidung
(iF13), nicht Teil dieser Etappe. Bezeichner und Kommentare deutsch.

**Die Feldgrößen sind fest verdrahtet:** 8760 Stunden, 168 Wochenwerte, 365 Tage, 12 Monate, 24
Tagesstunden; Vektoren `float` mit Zwischenrechnung in `double`; Arrays werden **in-place**
überschrieben, der Rückgabewert fast überall ignoriert. Diese Konventionen beim Erweitern
beibehalten.

## Bericht: alles hier bis auf den GDI+-Stand

**Der Diagramm-Renderer liegt seit iU7-5 hier** — `Allgemein/Bericht/ChartRenderer.cs`,
SkiaSharp statt GDI+ (iU7-2), ohne eine einzige Windows-API. Er ist die Vorlage für iF16
(`EPOS.UI/Standards/ChartBild`): Der Kern liefert PNG-Bytes, die Oberfläche zeigt sie an —
ein Chart-Stack für Bericht *und* Bildschirm.

**Er zeichnet seit iU9-W3.4 auch für EINGABEMASKEN.** `ChartRenderer.Kostenprofil` (samt der
Palettenfarbe `C_PROFIL`) ist die erste neue Methode seit der SkiaSharp-Portierung: das aus
zwölf Monatsniveaus und 168 Wochenwerten konstruierte Jahresprofil (8 760 Stunden) über einer
Monatsachse 0…12, Bildmaß **1296 × 780** — die doppelte Zielauflösung des abgelösten
WinForms-Chart aus `Form_Kostenprofil` (648 × 390). Die y-Achse ist **vorzeichenfähig** wie
beim Kapitalwert-Verlauf und aus demselben Grund: Ein Wochenwert ist eine *Abweichung* und
darf den Monatswert unter null ziehen; die Nulllinie wird dann gestrichelt hervorgehoben. Der
Dialog dazu ist `EPOS.UI/Dialoge/Kosten/KostenprofilDialog.razor`, gerechnet wird in
`Views/Kosten/KostenprofilHuelle.cs` (`PreisModell.AusMonatsUndWochenwerten` + Renderer, beides
in `Task.Run`). Damit trägt der Weg „Diagramm im Kern zeichnen, in der Oberfläche nur das PNG
zeigen" auch außerhalb des Berichts.

**Seit iU9‑W7.0c zeichnet er die WÄRMEPUMPEN-KENNLINIEN.** `ChartRenderer.Kennlinien`
ist die zweite Methode für eine EINGABEMASKE: COP bzw. Leistung über der
Außentemperatur, eine Linie je Vorlauftemperatur, Bildmaß **968 × 520** (die doppelte
Zielauflösung des breitesten der vier abgelösten WinForms-Charts, 484 × 195, plus
130 px für die Legende — sie steht hier UNTER dem Diagramm statt darin, weil sie bei
acht Reihen die Linien verdeckte). Punktmarken wie im Vorläufer: Kreis für den COP,
Kreuz für die Leistung. Die x-Achse trägt echte Temperaturen statt
Stützstellennummern — zwei Vorlauf-Kennlinien müssen nicht dieselben
Außentemperaturen haben; die „schöne" Achsenstufung ist dafür aus
`KapitalwertVerlauf` als `Stufe(ref min, ref max)` herausgezogen. Die Datenseite
liefern `KenndatenCtrl.Reihen` und `KenndatenKuehlungCtrl.Reihen` als **ein**
`KennlinienSatz` mit beiden Reihenlisten. Die Dialoge dazu sind
`EPOS.UI/Dialoge/Waermepumpe/WaermepumpeStammDialog.razor` und
`…/WaermepumpeAnlageDialog.razor`, gezeichnet wird in den Hüllen
`Views/Wärmepumpe/WaermepumpeStammHuelle.cs` (`BilderZu`).

**Seit iU9‑W8.0c zeichnet er die BEDARFSBILDER.** Drei Methoden lösen die neun
`Chart`-Steuerelemente der zehn Bedarfsmasken ab (Bausteinlücke 12):
`ChartRenderer.MonatsSaeulen` (978 × 542 — die doppelte Zielauflösung des
größten Vorläufers 489 × 271; x-Achse starr 1…12, y ab 0),
`ChartRenderer.Stundenprofil` (1244 × 464 — EIN Bild für 168 Wochenstunden UND
24 Tagesstunden; der Unterschied Fläche/Linie der beiden Vorläufer war keine
Entscheidung, sondern die Voreinstellung zweier Diagrammverwalter) und
`ChartRenderer.Jahresverlauf` (978 × 542, 8 760 Stunden über Monatsgrenzen —
OHNE den Mausrad-Zoom des Vorläufers, weil ein PNG nicht spreizen kann).
Die „schönen Schritte" der y-Achse sind wörtlich aus `SkaliereYAchse`
übernommen und eine ANDERE Reihe als beim Kapitalwert-Verlauf
(0,1/0,2/0,25/0,5/1/2/2,5/5/10 statt 1/2/2,5/5/10): Bedarfswerte brauchen auch
Zehntel. Die Dialoge dazu stehen in `EPOS.UI/Dialoge/Bedarf/`, gezeichnet wird
in den drei Hüllen unter `Views/Bedarf/`.

**Seit iU9‑W11a.6 zeichnet er die ERGEBNISBILDER der Simulation.** Sieben Methoden
lösen die **17 Zeichenflächen** der sechs Ergebnismasken ab:
`ChartRenderer.GanglinieNormiert` (1240 × 560 — ein bis vier Linien, alle auf DENSELBEN
Höchstwert normiert, y-Achse 0…100,2 % wie `init_Chart`, x wahlweise Monatsgrenzen oder
die vier Stundenmarken 2000/4000/6000/8000),
`ErzeugerStapel` (1240 × 560 — **das Arbeitspferd**: es trägt SECHS der siebzehn Flächen.
Zwei Stapelgruppen wie `StackedGroupName` im Vorläufer, Linien darüber in
Zeichenreihenfolge, die Konturlinie „Gesamt" UNTER dem Stapel, sortiert ohne Stapel, eine
Reihe auf einer zweiten y-Achse),
`Streuwolke` (1240 × 560 — halbtransparente XY-Punkte über einer vorzeichenfähigen
x-Achse), `Ring` (720 × 560 — Kuchen mit Innenloch, Zahl in der Mitte und einer Legende,
die nur Segmente > 0 nennt), `MonatsStapel` (978 × 542) und `Temperaturverlauf`
(1240 × 560 — gestrichelte Zwillingsreihe je Speicher, y-Achse OHNE Nullpunkt mit einer
Mindestspanne von 5 K). `Reihe` trägt dafür seit W11a.6 `Stapelgruppe`, `Gestrichelt` und
`Breite`; der alte Konstruktor ist unverändert.

**Seit iU9‑W11b zeichnet er sie AUCH für die Ergebnisseite** — die sieben Methoden
aus W11a.6 bedienen dort alle 17 Zeichenflächen der sechs abgelösten Masken. Neu ist
daran nichts; die Welle 11b fasst den Renderer nicht an (ChartProben unverändert 30).

**Seit W11b‑B‑5 zeichnet er die AUSLEGUNGSOPTIMIERUNG — und löst damit die letzte
fremde Zeichenbibliothek ab.** `ChartRenderer.Optimierungsraster` (860 × 560 — eine
Zelle je Rasterpunkt in der Dreifarbskala Rot/Gold/Grün, das Optimum als offenes
schwarzes Quadrat, ein senkrechter Farbbalken rechts) und `ChartRenderer.Schnittkurve`
(720 × 460 — ΔJ über der Kapazität bei der besten C-Rate, y-Achse mit Vorzeichen und
gestrichelter Nulllinie, das Optimum als roter Kreis). Bis dahin zeichnete das
`ScottPlot.WinForms` in `Form_SpeicherOptimierung`, dem einzigen Ort des Programms mit
einer zweiten Zeichenbibliothek — **und genau dort stürzte der Dialog ab**: Jeder Lauf
hängte über `Plot.Add.ColorBar` eine weitere Farbskala an denselben Plot, `Plot.Clear()`
räumt aber nur Plottables und keine Panels; die Zeichenfläche schrumpfte je Lauf um rund
78 Bildpunkte und war ab dem achten Lauf null. Ein Renderer ohne Zustand kennt das
Problem nicht. **Nicht endliche Werte fallen hier weg** statt das Bild zu Fall zu
bringen — ein einziges ±∞ in der Matrix beendete ScottPlot beim RENDERN („min must be a
real number"), also im Anstrich des Steuerelements und damit unfangbar. Der Aufrufer ist
`Controller/SpeicherOptimierungCtrl`; die Proben stehen in `ChartProben` (38 Bilder,
sechs Gegenproben) und in `EPOS.Kern.Tests/SpeicherOptimierungCtrlTests`.

**Die vier BERICHTSBILDER bleiben unangetastet.** `JahresverlaufWaerme` und
`DauerlinieWaerme` sind zwei feste Ausprägungen von `ErzeugerStapel`,
`StrombilanzMonate`/`MonatsSaeulen` zwei von `MonatsStapel`, `Speichertemperaturen` eine
von `Temperaturverlauf` — sie nehmen aber einen `ZeitreihenSatz` und tragen feste deutsche
Titel im Quelltext. Ihre Zusammenführung mit den neuen ist ein eigener Schritt mit eigenem
Nachweis (offener Punkt W11a‑O‑3), keine Nebenarbeit.

**Die AUSGABE liegt seit iU5-U3 ebenfalls hier:** `WordBerichtGenerator` (OpenXML),
`ExcelBerichtGenerator` (ClosedXML), `IBerichtsBaustein`, `BerichtsKonfiguration`,
`ZeitreihenExtraktor` und `Bausteine/`. Word und Excel sind Dateiformate, keine Windows-APIs —
der Bericht entsteht damit auch auf dem iPad. In der Anwendung blieb nur
`BerichtsDatenSammler`, weil er `EnergieMengen` aus `Views/Varianten/` ruft. Der eingefrorene
GDI+-Stand `ChartRendererGdi` und der Modus `bildvergleich` der Referenzlauf-Suite sind mit
Entscheid **iF23** am 03.09.2026 gelöscht — der Anwender hat die Löschung ohne den
Windows-Bildvergleich angeordnet; die Berichtskette hat keine GDI+-Stelle mehr.

**Die Fußzeilen-Fassung des Word-Berichts.** `Bausteine/BausteineStandard.cs` las die
Programmfassung bis iU5-U3 über `System.Windows.Forms.Application.ProductVersion`. An ihrer
Stelle steht jetzt `DeckblattBaustein.ProduktFassung()` mit derselben Reihenfolge wie WinForms:
`AssemblyInformationalVersionAttribute` des **Einstiegs**-Assemblies, sonst
`FileVersionInfo(...).ProductVersion` derselben Datei, sonst `"1.0.0.0"`. Der Bestand nimmt den
zweiten Zweig — die Anwendung setzt `GenerateAssemblyInfo=false` und deklariert nur
`AssemblyVersion`/`AssemblyFileVersion` `1.1.0.0`; das Deckblatt zeigt unter Windows deshalb
unverändert `1.1.0.0`.

**Die Vorlage bleibt neben der EXE.** `WordBerichtGenerator.FindeVorlage()` sucht
`Vorlagen\Berichtsvorlage.docx` über `AppDomain.CurrentDomain.BaseDirectory` — die `.docx`
selbst liegt weiterhin im Anwendungsprojekt und wird von dort ins Ausgabeverzeichnis kopiert.

**Die Dateiwahl der Berichtsansicht läuft seit iU7-9 über `Dienste.Datei`** —
`OrdnerWaehlen`, `DateiSpeichern` und `MitSystemOeffnen` statt `FolderBrowserDialog`,
`SaveFileDialog` und `Process.Start` (`Views/Bericht/UcBericht.cs`,
`Views/Varianten/Form_Variantentest.cs`).

**Schriftregel iF19 — Systemschrift, flexibel.** Der Renderer bindet keine Schrift ein,
sondern fragt `SKFontManager` eine Rückfallkette ab: Calibri (Windows) → Carlito/Liberation
Sans/DejaVu Sans (Linux) → Helvetica/Arial (macOS/iOS). Das Layout ist **metrikgetrieben**:
Umbrüche und Legendenbreiten folgen den gemessenen Textmaßen, nicht festen Pixelwerten.
Folge, und das ist Absicht: **Textbreiten dürfen je Plattform abweichen.** Ein Vergleich
Windows↔Linux ist deshalb ein Struktur- und Histogrammvergleich, kein Pixelvergleich; ein
Pixelvergleich wäre nur *innerhalb* einer Plattform sinnvoll (das tat der Modus
`bildvergleich` gegen den GDI+-Stand — beide mit iF23 gelöscht).

**Nachweis in drei Stufen.** `EPOS.Kern.Tests/ChartRendererTests.cs` (iU7-8) prüft die
Verdichtungen exakt und dass gezeichnet wird — seit iU9-W3.4 fünf Tests (die zwei neuen
sichern Maß und Determinismus des Kostenprofils), in jedem Kern-Lauf dabei.
`Proben/ChartProben` (eigene `.sln`, referenziert dieses Projekt) zeichnet seit iU9‑W11a.6 **dreißig** Bilder und
prüft Maße, Farbvorkommen und Determinismus; seit iU7-7 läuft die Probe in
`.github/workflows/kern.yml` auf ubuntu **und** macos, die PNG gehen als Artefakt mit. Der
Pixelvergleich gegen GDI+ läuft unter Windows.

**Die nativen SkiaSharp-Bibliotheken sind bedingt** — `Condition="$([MSBuild]::IsOSPlatform(…))"`
in `EPOS.Kern.csproj` und in `EPOS.Kern.Tests.csproj`. Welche Native passt, entscheidet die
Bauumgebung und nicht das TargetFramework; jede Umgebung zieht genau ihre eigene statt aller
drei. Win32 steht mit dabei, weil `windows.yml` `dotnet test WP-Plan.Kern.slnf` fährt.

## Eine Emissionsquelle für alle Erzeuger

**Hausregel seit dem Anwenderentscheid W14a‑E‑8‑B1 vom 07.09.2026.** Wer einen Emissionsfaktor
braucht — gleich ob im Rechenlauf, in der Wirtschaftlichkeit, im Bericht oder auf einer
Kachel —, holt ihn über `Allgemein/Wirtschaftlichkeit/Emissionsquelle.cs`. Es gibt keine
zweite Stelle mehr.

Bis zu diesem Entscheid gab es drei: Die Wirtschaftlichkeit las den Emissionskatalog über
`EmissionsFaktorLader`, der **Heizkessel** las `Tab_Brennstoff_Stamm` unmittelbar über die
Brennstoff-ID des Geräts, und das **BHKW** las die fünf Gerätespalten seines Katalogs
(`Tab_BHKW.CO2/SO2/NOX/CO/Staub`, Einheit g/MWh). Dasselbe Modul trug damit im Rechenlauf und
in der Emissionsbilanz verschiedene Zahlen.

| Was | Aufruf |
|---|---|
| Faktorsatz eines Erzeugers | `Emissionsquelle.Fuer(idProjekt, carrierId, idBrennstoffRueckfall, modus)` |
| Berechnungsmodus des Laufs (F7) | `Emissionsquelle.Modus(idProjekt)` — **einmal je Lauf**, nicht je Erzeuger |
| Netzstrom (Autarkie-Kachel, Kennzahlen) | `Emissionsquelle.Netzstrom(idProjekt, modus)`, Rückfall `NETZSTROM_RUECKFALL_G_JE_KWH` = 435 |
| verdrängte Wärme (Autarkie-Kachel) | `Emissionsquelle.Waerme(idProjekt, modus)`, Rückfall `WAERME_RUECKFALL_G_JE_KWH` = 200 |
| Stromträger des Projekts | `Emissionsquelle.StromTraeger(idProjekt)` |

**Die Lesekette bleibt die des Konzepts** (`Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md`
§ 3, unverändert in `EmissionsFaktorLader`): Projektwert → aktive `emissionswert`-Zeile →
`Tab_Brennstoff_Stamm` → Altspalte `energy_carrier`. `Emissionsquelle` legt zwei Dinge darum:
den **Modus** (im Modus `CO2E` das Äquivalent nach F6) und ein **fünftes Glied** für Anlagen
ohne `Tab_Energieanlagen.ID_Carrier` — dann gilt der Brennstoff des Geräts gegen dieselbe
`Tab_Brennstoff_Stamm`, in der die Kette ohnehin endet (Ebene `BRENNSTOFF`), und der Lauf sagt
es im Protokoll an.

**Einheiten, und warum sie nicht dieselben sind wie am Gerät:** Der Katalog führt CO₂ in
**g/kWh** und SO₂/NOₓ/Staub in **mg/kWh** (Konzept F4); die Gerätespalten standen in **g/MWh**.
Beide Simulationsstufen teilen ihre Summe wie bisher durch 1 000 — dadurch stehen `Em_CO2_*`
seither in **t/a** und `Em_SO2_*`/`Em_NOX_*`/`Em_Staub_*` in **kg/a**, deckungsgleich mit
`EmissionsBilanzRechner`. Vorher führte die BHKW-Stufe ihr CO₂ in kg/a, die Kesselstufe in t/a.

**Die zehn Gerätespalten von Kessel und BHKW sind „nur Anzeige"** (`ParameterVerwendung`,
Stufe `Dialog`); beide Katalogeditoren tragen darüber eine Herleitungszeile
(`HZKK_EMISSION_INFO`). Sie bleiben pflegbar — als Herstellerangabe.

**Nachweis:** `EPOS.Kern.Tests/EmissionsquelleTests.cs`. Der Referenzlauf taugt hier **nicht**
als Wächter: Keine Referenz-CSV führt eine Emissionsgröße (`Referenzlauf/Ergebnisexport.cs`
schreibt keine, `Tab_Ergebnis*` hat keine Emissionsspalte), die zwölf Projekte sind nach dem
Umbau byte-gleich. Wer eine Emissionsgröße ändert, misst sie an dieser Probe.

## Die Anzeigeeinheit einer Energiemenge

`Allgemein/Energieeinheit.cs` (öffentlich, ohne Datenbank, ohne Oberfläche) trägt seit dem
Anwenderentscheid W8‑O‑5 / W9‑O‑3 vom **04.09.2026** die beiden Einheiten **MWh (Vorgabe)** und
**kWh**: `Text`, `Format` (`F2` bzw. `F0`), `Alle` als Auswahlliste, `AusKWh`/`AusMWh`/`Aus` für
die Anzeige und `NachKWh`/`NachMWh` für den Rückweg einer Eingabe. **Die Identität ist bitgleich** —
`AusMWh` auf `MWh` gibt den Wert unverändert zurück, statt ihn über `× 1000 × 0,001` zu schicken;
ohne diese Fallunterscheidung wäre eine Anzeige bei der Vorgabe nicht mehr zeichengleich zum
Bestand.

**Der Rechenkern bleibt unberührt.** Die Klasse rechnet ANZEIGEN um, nicht Simulationen:
`SimulationWaermebedarf` und `SimulationStrombedarf` teilen weiter selbst durch 1 000 bzw. 4 000,
und der Referenzlauf bleibt byte-gleich. Wozu sie da ist: Der Bestand trug die Einheit als
Zeichenkette **neben** der Zahl und dazwischen ein nacktes `/ 1000`, das nur in EINEM der beiden
Wege stand (Befund W8‑B4). Jetzt sagt die Hülle, **in welcher Einheit ihre Zahl vorliegt**, und die
Anzeige rechnet um.

`BedarfEinheitWahl` merkt die Wahl über `Dienste.Einstellungen` unter dem Schlüssel
`BedarfEinheit` (ohne Eintrag MWh), damit der Bedarfsprofildialog (W9) und der daraus geöffnete
Ergebnisdialog (W8) dieselbe Einheit zeigen.

**Hausregel: eine Energiemenge wird GENAU EINMAL umgerechnet, an der Anzeigekante.** Im Kern und
in den Hüllen bleibt die Zahl in ihrer Quelleneinheit stehen; erst die Anzeige rechnet um, und sie
tut es über `Energieeinheit`, nie über einen nackten Teiler. Eine Hülle, die eine Zahl weitergibt,
nennt deshalb die **Einheit am Wert** (`ErgebnisKennzahl.QuelleEinheit`, `Monatssicht.QuelleEinheit`)
statt sie vorher passend zu machen. Wer einen zweiten Teiler einbaut, verschiebt eine bereits
umgerechnete Zahl um Faktor 1 000 — das war Befund W8‑B4 und der Nachtrag W9‑O‑3.

## Einheiten: die Regel des Rechenkerns

**Anwenderentscheid W8‑O‑5c vom 07.09.2026, Frage Q1: „Regel festschreiben".** Der Kern
rechnete die Regel seit jeher zu rund 90 %, aber sie stand nirgends — und genau dort, wo es
darauf ankam, fehlte die Einheit am Namen: Die sechs Erzeuger- und Speicherklassen führten
**31 Jahressummen** als `…_gesamt` oder `…_summe`, keine einzige mit Einheit, obwohl einige
kWh und andere MWh trugen. Herleitung, Inventar (242 Fundstellen), die Gleitkommaprobe und der
verworfene Vollumbau stehen in
[`Konzept_Einheiten_EPOS-Plan.md`](../Konzept_Einheiten_EPOS-Plan.md).

1. **Zeitreihen führen kWh**, Viertelstundenreihen kW — immer, ohne Ausnahme. Das ist die
   Einheit der 300 Vektordateien der Referenzbasis.
2. **Jahres- und Monatssummen, die den Kern VERLASSEN, führen MWh** — an Anzeige, Bericht,
   Datenbank und CSV. 15 der 17 `Tab_Ergebnis*` speichern MWh, die zwei Speichertabellen kWh.
3. **Jede Größe, die den Kern verlässt, trägt ihre Einheit im NAMEN** — `…Kwh`, `…Mwh`, `…Kw` —
   oder sie führt sie im DTO (`Energieeinheit` am Wert). Ein Kommentar hält die Einheit nicht:
   Der Kommentar an `SimulationStrombedarf.Strombedarf_Max` nannte seit dem Bestand „kWh" für
   eine Leistung in kW (Befund U7).
4. **Umgerechnet wird an genau ZWEI Nähten:** `Controller/SimulationErgebnisCtrl` (Anzeige) und
   `Allgemein/Simulation/SimulationRunner` (Datenbank). Außerhalb dieser beiden Dateien und der
   `Energieeinheit` steht in `EPOS.UI` und in den Windows-Hüllen **kein** Faktor 1 000 auf einer
   Energiemenge.
5. **Kein Wechsel der Recheneinheit.** Wo heute MWh in einem Rechenobjekt steht
   (`SWaermeSpkMwh`, `Waermeproduktion_BHKW_MWh`, `RestwaermeMwh`, die 19 Brennstoffzähler),
   bleibt es MWh und heißt so. Eine Vereinheitlichung auf EINE Recheneinheit ist geprüft und
   **abgelehnt** (Q1): Sie behebt die Ursache nicht — die war die ungenannte Einheit, nicht die
   zweite —, sie bringt keine Genauigkeit (die Messung steht in Kapitel 4.3 des Konzepts) und
   sie kostet eine neue Referenzbasis. (Der Klammersatz nannte bis W8‑O‑5d `float`; die
   Typfrage ist seither anders entschieden — siehe unten —, die Ablehnung des
   EINHEITENumbaus bleibt davon unberührt.)

**Zwei Wächter halten die Regel** (`EPOS.Kern.Tests/EinheitenWacheTests.cs`, Stufe S1.4):

| Wächter | Was er prüft | Ausnahmen |
|---|---|---|
| `In_Anzeige_und_Huellen_steht_kein_Faktor_1000_auf_einer_Energiemenge` | In `EPOS.UI/**` und `WindowsFormsApplication1/Views/**` steht keine der sieben Schreibweisen `/ 1000`, `* 1000`, `/= 1000`, `*= 1000`, `/ 4000`, `0.001`, `1e-3` auf einer Energiemenge. Meldet Datei:Zeile | **fünf**, alle LEISTUNG (W → kWp) bzw. eine kWp-Untergrenze, je mit Grund in `AusnahmenAnzeige` |
| `Jede_Jahressumme_der_Simulationsklassen_traegt_ihre_Einheit_im_Namen` | Jedes `public`/`internal` Skalarfeld und jede Property vom Typ `double`/`float` in den sieben Simulationsklassen, deren Name `Bedarf`, `Verbrauch`, `Produktion`, `Ertrag`, `Summe`, `gesamt` oder `Rest` enthält, endet auf `Kwh`/`Mwh` | **13**: vier Leistungsspitzen [kW], eine Vollbenutzungsstundenzahl [h] und die acht `…_gesamt` des Pufferspeichers, deren Namen als CSV-Schlüssel in der eingefrorenen Basis stehen |

Eine Suche nach `/ 1000` allein genügt **nicht**: `BhkwPlan.VectorSumme` und
`BhkwPlan.MonatsSumme` — die Routinen, aus denen jede Monatsreihe und jede Jahressumme des
Bestands entsteht — schreiben `0.001`, und die Viertelstundenreihen teilen durch 4 000.

**Was die CSV-Schlüssel angeht (Q7):** `Sim.Restwaerme` und `Sim.Reststrom` in
`Referenzlauf/Ergebnisexport.cs` bleiben hart verdrahtet, obwohl die Felder `RestwaermeMwh` und
`ReststromMwh` heißen. Der Schlüssel steht in 312 Dateien der Basis `2026-09-07_R5_Zahlenrand`;
wandert er mit, ist kein Vergleich gegen eine ältere Basis mehr möglich. Dasselbe gilt für
`Puffer.Ladung_gesamt`, `Puffer.Entladung_gesamt` und `Puffer.Verluste_gesamt`.

## Typen: der Rechenkern rechnet in `double`

**Anwenderentscheid W8‑O‑5d vom 07.09.2026:** „alles in double, ist kein Nachteil und
systematisch. Summenfunktionen aus Original BHKW-Plan ebenfalls double." Damit ist die
Empfehlung **Q6** des Einheitenkonzepts („die 59 Stundenreihen bleiben `float`") überholt.

Der ganze Rechenweg — Stundenreihen (8 760 / 35 040 / 365 / 168 / 12), Akkumulatoren,
Zwischenwerte, Felder, Eigenschaften, Parameter und Rückgaben — führt `double`, und zwar
auch dort, wo bis dahin in `double` GERECHNET und in `float` GESPEICHERT wurde. Das war die
bewusste Nachbildung des FPU-Verhaltens der alten `BHKWPLAN.DLL` (80-Bit-Zwischenwert,
32-Bit-Speicherzelle); sie ist aufgegeben. `BhkwPlan.VectorSumme` und `BhkwPlan.MonatsSumme`
akkumulieren seither in `double`. Die Datenbankgrenze passt damit: SQLite `REAL` **ist**
`double`, und der Lesepfad verliert keine Stellen mehr.

**`float` steht nur noch an drei Grenzen:**

| Grenze | Wo | Warum |
|---|---|---|
| Bildpunkte | `Allgemein/Bericht/ChartRenderer.cs` (120 Stellen), die Strichstärke in `SimulationErgebnisHuelle.Bilder` | SkiaSharp rechnet in `float`. Die **Datenreihen**, die der Renderer annimmt, waren immer `double` und bleiben es — `ChartRenderer` führt kein einziges `float[]` |
| Einbettungen | `Allgemein/KI/SemantikIndex.cs`, `SemantikModell.cs` | Vektoren des KI-Wissens, kein Rechenweg (ONNX liefert `Tensor<float>`) |
| Typprüfungen | `v is float`, `typeof(float)` in `ProjektExportImportCtrl`, `KomponentenUebernahmeCtrl`, `MerkmalUebernahmeCtrl`, `ParameterUebersichtCtrl`, `AnlagenEindeutigkeit`, `DublettenPruefung`, `PufferSpStammCtrl`, `Referenzlauf/Ergebnisexport` | Absicherung gegen einen boxed Wert aus einer Fremdquelle; kein Rechenweg |

Damit fällt die Zahl der `float`-Fundstellen im Kern von **788 in 42 Dateien** auf
**147 in 14**: 120 Bildpunkte, 12 Einbettungen, 10 Typprüfungen und 5 Kommentare, die die
Geschichte erzählen. Im Ordner `Allgemein/Simulation/` steht **keine einzige** Stelle mehr, in
`BhkwPlan.cs` nur noch zwei Kommentarzeilen — genau das prüft der Wächter.

**Ein dritter Wächter hält die Regel** (`EPOS.Kern.Tests/DoubleWacheTests.cs`):

| Wächter | Was er prüft | Ausnahmen |
|---|---|---|
| `Im_Rechenweg_steht_kein_float_mehr` | In `EPOS.Kern/Allgemein/Simulation/**` und `BhkwPlan.cs` steht keine der Schreibweisen `float` (Typname, `(float)`, `float.Parse`, `float.Epsilon`), `Convert.ToSingle`, `MathF.` und kein Zahlenliteral mit `f`-Suffix. Meldet Datei:Zeile | **keine** — die Liste ist leer, und eine Gegenprobe hält fest, dass jede eingetragene Ausnahme wirklich existieren müsste |

**Was der Umbau am Ergebnis geändert hat** (Basis `2026-09-07_R4_Double` — die damalige; seit den Entscheiden Q1/Q2 ist es `2026-09-07_R5_Zahlenrand`, Begründung mit
Zahlen im `protokoll.txt` dort): Die Jahressummen bleiben in allen zwölf Projekten innerhalb
3e‑5 relativ, die erste Differenz einer Stundenreihe liegt bei einer `float`-Stufe (rund
1e‑7). **Elf der zwölf Projekte reißen trotzdem die Toleranz iF15**, weil drei Schwellen des
Modells am letzten Bit entschieden und ihr Ergebnis über Stunden weitertrugen: die
Speicherhysterese `SOC >= Q_max · SchwelleAus` (`SimulationPufferspeicher`, bistabil), die
Volllast/Modulations-Grenze `bhkwWaermeLeistung[motor] < restWaerme + restSpeicher`
(`SimulationBHKW.Motorlauf_Waermegefuehrt`) und die drei `int`-Rückgaben von `BhkwPlan`
(`TaeglHeizlastWG`, `SolareGewinneC`, `SpezWaermeverlusteC` — Borland `_ftol`, Abschneiden).
**Wer eine solche Schwelle anfasst, ändert den Rechenweg fachlich** und braucht dafür einen
eigenen Entscheid; der Typumbau hat keine davon berührt. **Zwei Entscheide vom 07.09.2026
haben genau das dann getan** — siehe die nächsten zwei Abschnitte; die Basis dazu ist
`2026-09-07_R5_Zahlenrand`.

## Vergleiche an Betriebsschwellen tragen den Zahlenrand

**Anwenderentscheid W8‑O‑5d‑Q1 vom 07.09.2026 („Empfehlung").** Ein Vergleich, der eine
BETRIEBSSCHWELLE entscheidet, darf nicht am letzten Bit kippen. Der Rand steht **einmal** in
`Allgemein/Simulation/Rechenrand.cs`:

```csharp
Rand    = Rechenrand.ABSOLUT (1e-9) + Rechenrand.RELATIV (1e-12) · |Schwelle|
erreicht = Rechenrand.SchwelleErreicht(wert, schwelle)   //  wert >= schwelle - Rand
```

Er ist **absolut UND relativ**, weil die Schwellen Energien in kWh tragen und mehrere
Größenordnungen spannen: ein rein absoluter Rand wäre bei 100 000 kWh zu knapp, ein rein
relativer verschwände an einer Schwelle von 0. Beide Zahlen sind so gewählt, dass der Rand
**vier Größenordnungen unter der schärfsten Vergleichstoleranz der Referenzsuite** (rel. 1e‑4)
bleibt — er kann keine Abweichung erzeugen, die ein Referenzvergleich noch sähe.

**Angewandt an den zwei Schwellen des Befunds:**

| Stelle | alte Bauart | warum sie am letzten Bit entschied |
|---|---|---|
| `SimulationPufferspeicher.HystereseFortschreiben` | `SOC >= Q_max · SchwelleAus` | `Ladefaehigkeit` fährt den Speicher auf **genau** `Q_max · grenze`, und `a + (b − a)` ist nicht bitgleich `b`. Bistabil — der Fehltritt trug über Stunden |
| `SimulationBHKW.Motorlauf_Waermegefuehrt` (beide Stufen) | `P_th < restWaerme + restSpeicher` bzw. `P_th · x_min <= …` | Wärmeraum und Nennleistung liegen an der Kante gleichauf; kippt der Vergleich, springt die Stundenproduktion |

**Und seit W8‑O‑5d‑Q3/Q4 vom selben Tag („Empfehlung", das Nachziehen) an ALLEN
Betriebsschwellen des Kerns** — nicht nur an den zwei, die der Befund erzwungen hatte:

| Stelle | wie viele Vergleiche | Schwelle / Wert |
|---|---|---|
| `SimulationBHKW.Motorlauf_Waermegefuehrt` (Q1) | 2 | Nennleistung bzw. Modulationsgrenze / Wärmeraum |
| `SimulationBHKW.Motorlauf_Stromgefuehrt` | 2 | Nennleistung bzw. Modulationsgrenze / Reststrom |
| `SimulationBHKW.Motorlauf_OhneEinspeisung` (W1, W2, S1…S3) | 12 | dieselben Größen, dazu die anteilige Ausbeute des geregelten Laufs |
| `SimulationPufferspeicher.HystereseFortschreiben` (Q1) | 1 | `Q_max · SchwelleAus` / `SOC` |
| `SimulationPufferspeicher.EntnahmeObergrenze` | 1 | Reservemarke `Q_max · SchwelleReserve` / `SOC` — eine UNTERgrenze, deshalb mit vertauschten Rollen |

**Die Leserichtung ist überall dieselbe:** SCHWELLE ist die Maschinengröße (Nennleistung,
Modulationsgrenze, die aus ihr abgeleitete Ausbeute), WERT der Rest, der ihr gegenübersteht
(Wärmeraum, Reststrom). Damit entscheiden die drei Fahrweisen nach EINEM Maß. Wo der Bestand
`<` statt `<=` schrieb — zweimal in `Motorlauf_OhneEinspeisung` —, fällt die Gleichheit damit
auf die Seite, auf der sie an den vier übrigen Modulationsgrenzen ohnehin liegt; die Änderung
bleibt im Band der Breite `Rand` und ist die einzige, die der Rand am Gleichheitspunkt bewegt.

Die **Einschaltschwelle** des Speichers bleibt bewusst ohne Rand — und der Grund benennt zugleich
die Regel, nach der eine Schwelle einen braucht: **Auf sie steuert keine Rechnung zu.** Ein Rand
gehört an jede Marke, die ein Rechenweg ansteuert (die Ladung fährt auf `Q_max · SchwelleAus`, die
Entladung auf `Q_max · SchwelleReserve`) und an jeden Vergleich, dessen Operanden aus getrennten
Rechenketten stammen und an der Kante gleichauf liegen. **Wer eine neue Betriebsschwelle einführt,
nimmt `Rechenrand.SchwelleErreicht` — nicht `>=`.** Nachweis: `EPOS.Kern.Tests/RechenrandTests`
(6 Fälle) und `EPOS.Kern.Tests/RechenrandFahrweisenTests` (13 Fälle) — je Schwelle ein Stand genau
auf der Grenze und einer ein ulp darunter, dazu die zwei Bistabilitätsproben und je eine Gegenprobe
mit dem blanken Vergleich. Die zwölf Referenzprojekte fahren **alle wärmegeführt**
(`Tab_Einstellungen.Betriebsart` 0 bzw. `NULL`), deshalb sind die Proben der zwei anderen
Fahrweisen synthetisch — und deshalb bleibt der Referenzlauf zu Q3/Q4 byte-gleich.

## Keine `(int)`-Abschneidung auf einer Rechengröße

**Anwenderentscheid W8‑O‑5d‑Q2 vom 07.09.2026: „keine Treue zur alten DLL".** Die drei
Physik-Funktionen des BHKW-Plan-Ports — `BhkwPlan.TaeglHeizlastWG`, `SolareGewinneC`,
`SpezWaermeverlusteC` — gaben `int` zurück, weil die native `BHKWPLAN.DLL` das tat (Borland
`_ftol`). Seither geben sie `double` zurück und schneiden nicht mehr ab; die Aufrufer in
`SimulationWaermebedarf` folgen mit (`/ 100` war dort eine GANZZAHLIGE Division und heißt
jetzt `/ 100.0` — es waren zwei Abschneidungen hintereinander). Der Faktor 100 selbst bleibt:
Er gehört zur Schnittstelle der Funktion, nicht zur Physik.

**Die Regel daraus:** Auf einer Rechengröße steht keine `(int)`-Wandlung. Wer eine Zahl
ganzzahlig braucht (Indizes, Zähler, Stundennummern), wandelt sie dort, wo sie ein Index
wird — nicht auf dem Weg dorthin. Nachweis: `EPOS.Kern.Tests/BhkwPlanRueckgabeTests`.

## Vorschau und Lauf lesen dieselben Tabellen

**Hausregel (seit Befund W9‑B‑4/B‑5 der Windows-Abnahme vom 05.09.2026): Ob eine Profilrechnung
den KATALOG oder die PROJEKTKOPIEN liest, hängt am PROJEKT — nicht daran, ob eine Namensliste
mitkommt.** Die Regel steht einmal in `ProfilBedarf.Vorschaumodus(namen, idProjekt)`:

| Aufruf | Modus | wer |
|---|---|---|
| ohne Namensliste | `Projektrechnung` | der Simulationslauf (Referenzlauf) |
| mit Liste, **ohne** Projekt | `Katalogvorschau` | die drei Katalogverwaltungen |
| mit Liste **und** Projekt | `Projektvorschau` | der Bedarfsprofil-Dialog (Kopie zuerst, W9‑O‑3c) |

**Zweite Hausregel (Anwenderentscheid W9‑O‑3c vom 05.09.2026, „Empfehlung"): Die Projektvorschau
liest die KOPIE zuerst.** `Projektvorschau` rechnet auf denselben Tabellen und mit demselben
Projektfilter wie der Lauf und fällt erst für einen Namen, den das PROJEKT nicht kennt, auf den
`_STAMM`-Katalog zurück (`ProfilQuelle.Rueckfall`). Beide Quellen sind nötig, weil die Liste des
Dialogs gemischt ist: eine gespeicherte Zuordnung trägt den Namen ihrer Projektkopie
(`Z_Projekt*Ctrl.LiesProjekt` liest `Tab_*.Bezeichner`, und eine Kopie heißt vielfach
„‹Name› (P‹Projekt›)"), eine eben aufgenommene Zeile den ihres Katalogeintrags — deren Kopie
entsteht erst beim Speichern, und genau für sie greift der Rückfall. Wird er gezogen, liefert er
**Kopf UND Typprofil** — ihre Vermischung war Befund V0‑4.

Damit zeigt die Vorschau überall dieselben Zahlen wie der Lauf. Die erste Fassung (Behebung
W9‑B‑4/B‑5) las den Katalog zuerst, damit jede damals richtige Zahl zeichengleich blieb; eine im
Projekt GEÄNDERTE Kopie erschien dadurch mit der Katalogverteilung — Brauchwasser 1007: Januar
1,900 statt 0,552 MWh bei gleicher Jahressumme. Der Entscheid hat das gedreht. **Wer eine Zahl der
Vorschau ändert, hat sie am Lauf zu messen, nicht am Katalog.**

Die alte Ableitung `list == null ? Projektrechnung : Katalogvorschau` stand in allen drei
Bedarfszweigen und ließ den Dialog Projektnamen im `_STAMM`-Katalog suchen; er fand nichts, übersprang
still und zeigte zwölf Nullmonate samt leerem Bild. **Wer eine vierte Bedarfsart anlegt, nimmt
`Vorschaumodus` — nicht `list == null`.**

## Eine Auskunft ruft den Rechenweg des Laufs — sie schreibt ihn nicht ab

**Hausregel seit dem Anwenderwunsch W9‑E‑2 vom 05.09.2026 (iU9‑W9.8).** Der
Gebäudedialog zeigt seit diesem Wunsch den Wärmebedarf EINES Gebäudes. Diese Zahl legt
der Anwender neben die Kennzahl der Ergebnisseite — sie muss dieselbe sein. Also darf sie
nicht ein zweites Mal gerechnet, sondern nur ein zweites Mal **gerufen** werden.

Wo der Weg in einer Schleife steckt, wird der **Schleifenrumpf ausgelagert** und von
beiden Seiten gerufen. `SimulationWaermebedarf` führt dafür seit W9.8 zwei `internal`
Methoden, Anweisung für Anweisung aus `Waermebedarf_berechnen` herausgezogen:
`KlimakalenderLesen(idKlimaregion)` (die 365 Tagessätze, die 8 760 Stundentemperaturen,
`WochentagJan1`) und `HeizwaermeEinesGebaeudes(item, index, ziel)` (der Rumpf der
Gebäudeschleife bis einschließlich `StdWerte`). Der Lauf ruft sie in seiner Schleife,
`Controller/GebaeudeBedarfCtrl` für sein eines Gebäude — der Referenzlauf bleibt
byte-gleich, weil nichts umgeschrieben wurde.

**Die Probe ist dann kein eingefrorener Wert, sondern der Vergleich gegen den LAUF
selbst**: bei einem Projekt mit genau einem Gebäude (1007, 1017) ist die Zahl des
Dialogs **bitgleich** zu `Waermebedarf_Gebaeude_Gesamt`, bei mehreren (1008, 1039) ist
es die Summe der Einzelrechnungen. Nachweis: `EPOS.Kern.Tests/GebaeudeBedarfCtrlTests`.

**Und die Kleinigkeit, an der es hängt:** Die Jahressumme steht dort als
`werte.Sum() / 1000` — eine `float`-Division wie im Lauf, nicht `/ 1000.0`. Eine
`double`-Division ergäbe eine andere neunte Stelle, und genau die sieht der Anwender,
wenn er die zwei Zahlen nebeneinanderlegt.

## Nachweis

Jede Änderung hier wird gegen die eingefrorene Windows-Basis geprüft:

```bash
dotnet build EPOS.Referenzlauf/EPOS.Referenzlauf.csproj -c Release
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/neu
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/neu     # GESAMT: PASS
```

**Seit iU9-W6 prüft `EPOS.Kern.Tests` auch SCHREIBENDE Wege — mit Datenbank.** Bis dahin
galt dort ausschließlich, was ohne Datenbank entscheidbar ist. Mit Welle 6 sind jedoch
Schreibwege aus der Oberfläche hierher gewandert, deren Ausgang darüber entscheidet, ob
ein Erzeuger aufgenommen wird (`EnergietraegerVarianteCtrl.Anlegen`, vier Ausgänge); der
Referenzlauf sieht davon nichts, weil er einen BESTEHENDEN Projektstand nachrechnet.
`EPOS.Kern.Tests/TestDatenbank.cs` legt je Testklasse eine Arbeitskopie von
`Referenzlaeufe/Kenndaten_Test.sqlite` an und biegt `DataRepository.PfadUeberschreibung`
darauf um — dasselbe Vorgehen wie `EPOS.Referenzlauf`, damit die Vergleichsbasis
unberührt bleibt. Fehlt die Datei, schweigen die Fälle statt rot zu werden. Alle Klassen
dieser Art tragen `[Collection("Testdatenbank")]`: `PfadUeberschreibung` ist statisch, und
xunit fährt Testklassen sonst nebeneinander.

**`[Collection("Testdatenbank")]` ist seit dem 06.09.2026 (Befund iU5‑O‑1) die EINE serielle
Sammlung — nicht nur die der Datenbank.** Wer in `EPOS.Kern.Tests` ein `Dienste.*` tauscht,
gehört in `[Collection("Testdatenbank")]` — der Wächter
`EPOS.Kern.Tests/DiensteSammlungTests` prüft es über die Quelldateien. Grund: `Dienste.*` ist
prozessweiter Zustand, und xunit trennt nur INNERHALB einer Sammlung — zwei VERSCHIEDENE
Sammlungen laufen immer nebeneinander. Eine eigene Sammlung „Dienste" half deshalb nichts:
Während ihr Tausch stand, meldete ein Datenbanktest über `DataRepository.FehlerMelden` in
denselben `Dienste.Dialog` und schrieb in die fremde Mitschrift (Windows-CI, Lauf 34018913888
auf `002c937`). Der Störer muss selbst kein Tauscher sein — darum reicht es nicht, sich nur
von den anderen Tauschern abzugrenzen.

**Der iU5-Wächter — muss leer bleiben:**

```bash
git grep -nE '\bProgram\.[A-Za-z]' -- 'EPOS.Kern/*.cs' \
    '../WindowsFormsApplication1/Allgemein/*.cs' \
    '../WindowsFormsApplication1/Controller/*.cs' \
    '../WindowsFormsApplication1/Model/*.cs' | grep -vP ':\s*(///|//|\*)'
```

Dieselben drei Projekte rechnet die CI (`.github/workflows/kern.yml`) auf `ubuntu-latest` und
`macos-latest`. 1007 und 1017 führen aktive Stromspeicher-Varianten und decken damit den
K8-Haken ab; ohne sie fiele ein stillgelegter Haken nicht auf.

**Der Plattform-Wächter — muss ebenfalls leer bleiben:**

```bash
git grep -nE 'System\.Windows\.Forms|System\.Drawing|MessageBox\.|\bProgram\.|\bRegistry\.|ProtectedData|OleDb' \
    -- 'EPOS.Kern/*.cs' | grep -vP ':\s*(///|//|\*)'
```

`\bRegistry\.` mit Wortgrenze — ohne sie trifft das Muster `speicherRegistry.` in
`SimulationControl.cs` und meldet zwölf falsche Treffer.
