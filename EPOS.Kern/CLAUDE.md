# CLAUDE.md — `EPOS.Kern`, der Rechenkern

Der plattformfreie Kern von EPOS-Plan: **334 `.cs`-Dateien** (168 aus iU4, dazu
`IDatenzugriff`/`SqliteDatenzugriff` aus iU6, `ChartRenderer` aus iU7-5, die 22 Dienste-Dateien
aus iU5, `EnergietraegerVarianteCtrl` aus iU8-8b, die **74 Dateien des zweiten Umzugs**
iU5-U1…U5, die sechs Dateien der Ergebnisseite aus iU9‑W11a, die **acht Dateien der
Importkette und der Lastspitzenkappung** aus iU9‑W12 und die **sechs Dateien des
KI-Assistenten** aus iU9‑W15b), `net10.0` **ohne** `-windows`, AnyCPU.

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
| `Allgemein/Simulation/` (33) | die vollständige Engine — `SimulationControl` (beide `partial`-Hälften), `Kaskadenschleife`, `SimulationKanaele`, `Init`, `SimulationRunner`, die Module je Erzeuger/Bedarf, `WaermequelleClass`/`WaermesenkeClass`, `Warnkriterien`, `ProfilBedarf`, `StilleDb`. **Mit iU9‑W10a** kommen die Rechen- und Anzeigewege der sieben Simulationsdialoge dazu: `WaermesenkeClass.SenkeAnzeige`/`SENKE_LEER` (sie war eine STATISCHE Methode auf `Form_Waermesenke` mit drei fremden Aufrufern, Befund W10‑B22), `VDI4640Pruefung.Sondenmeter`/`Volllaststunden`, `ErdreichAuswertung.ErdreichLaufErgebnis`/`ErgebnisZuordnen` (die Zuordnung stand doppelt in Maske und Aufrufer, W10‑B8) und die **erzeugte** Datei `KlimazonenPfade.cs` — 15 Zonen als SVG-Pfade, gebaut von `../Werkzeuge/KlimazonenPfade/erzeugen.py`, weil der Vorläufer die Karte zur Laufzeit mit einem Regex aus einer eingebetteten SVG las (W10‑B5). **Mit iU9‑W10b** kommt der Rest der Simulationskonfiguration dazu: `SchemaModell.cs` (unverändert verschoben — die letzte Datei, die noch in der Anwendung lag), `SchemaLayout.cs` (die ANORDNUNG des Schemas, bis dahin GDI+ in `SchemaAnsicht`: Spaltenbreiten, Knotenhöhen, Bézierbögen, Kaskadenband, Legende — headless prüfbar), `Kaskade.cs` (die vier Plätze `Tab_Einstellungen.Tool_1..4` samt den beiden Stromplätzen, bis dahin sechs unsichtbare Steuerelemente) und `WaermequelleClass.QuelleSchreiben` mit dem Satz `QuelleErgebnis` (die sechs Zweige der Quellenwahl als EIN Schreibweg). **Mit iU9‑W11a** kommen `ErgebnisPraesenz` (war `internal` in `Views/Simulation/` und steuert fünf der sechs Ergebnismasken), `Ganglinie` (`Dauerlinie`/`Anzeigewerte` aus `GanglinienDarstellung`; `Stapeltyp`/`StapelEinstellen` arbeiten auf einer WinForms-`Series` und bleiben) und `LaufFortschritt` dazu. **`SimulationControl.Do_Simulation` nimmt seither `IProgress<LaufFortschritt>` und `CancellationToken` entgegen** — ohne die beiden Zusatzangaben unverändert; der Abbruch wird ZWISCHEN den fünf Phasen geprüft (Start, Kaskade, Photovoltaik, Stromspeicher, Abschluss). Eine Meldung je Erzeuger gibt der Rechenweg nicht her: Die Kaskade läuft stundenweise und bedient in jeder Stunde alle Erzeuger nacheinander. **Die vier EIGENANTEILE** (`SimulationRunner.EigenanteilWpMwh`/`…KesselMwh`/`…SolarKwh`/`…BhkwMwh`) und die zwei Ableitungen `RestNachEigenanteil`/`DeckungProzent` sind aus `BaueErgebnis` herausgezogen: Dieselben Ausdrücke standen wortgleich in `Form_Simulation_Detail` |
| `Allgemein/Wirtschaftlichkeit/` (20) | alle 20 Dateien — `KapitalwertRechner` (DIN EN 17463), `EmissionsBilanzRechner`, `StromMatrix`, `WirtschaftlichkeitCtrl`, die KWKG-/EEG-/Steuer-Rechner |
| `Allgemein/Bericht/` (14 + 4) | die **DATEN**-Hälfte: `BerichtTexte`, `BerichtsDaten`, `EmissionsAusweis`, `KostenEmissionRechner`, `ProjektDetails`, `KennzahlenKatalog`, `AbweichungsErmittler`; seit iU7-5 der **Renderer** `ChartRenderer` (seit iU9‑W10a mit `Jahresgang` — 1 304 × 440, zwei Reihen, Monatsachse 0…12, vorzeichenfähige y-Achse, für das Quelltemperaturbild des Erdreichdialogs); seit iU5-U3 die **AUSGABE** `WordBerichtGenerator`, `ExcelBerichtGenerator`, `IBerichtsBaustein`, `BerichtsKonfiguration`, `ZeitreihenExtraktor` und `Bausteine/` (4 Dateien); seit iU9‑W12 `PeakShavingBild` — die drei Reihen und Farben des Vorher/Nachher-Bildes der Lastspitzenkappung. Es ist **kein neuer Renderer**: `ChartRenderer.ErzeugerStapel` trägt seit iU9‑W11a eine Sekundärachse, und genau die braucht der Ladezustand (kWh und kW teilen keine Skala) |
| `Allgemein/Dienste/` (22) | die **neun Umgebungsdienste** (iU5): `Dienste` (Halter), `IDialogDienst`, `IDateiDienst`, `IPfade`, `IEinstellungen`, `ILizenzAblage`, `IGeraeteId`, `ISprache`, `INavigation`, `IProjektKontext`, ihre Standardfassungen (`StilleDialoge`, `KeineDateiwahl`, `StandardPfade`, `FluechtigeEinstellungen`, `KeineAblage`, `KeineGeraeteId`, `StandardSprache`, `KeineNavigation`, `LeererProjektKontext`) und die sprachneutralen Schlüssel `Masken`, `Ansichten`, `Projektwahl`. **Die Konstantenklasse `Gewerke` und `INavigation.OeffneGewerk` sind mit iU9‑W16b.1 entfallen** — sie existierten ausschließlich für `FormMain` (Befunde W16‑B27/B28) |
| `Allgemein/Update/` (5) | `Anlagenzeilen`, `ProjektPuffer`, `SchemaKatalog`, `SchemaStand` (Ergebniszustand der Migration und die DDL-Konstanten, die Controller zur Selbstanlage brauchen) — seit iU5-U5 dazu `AnlagenEindeutigkeit`, seit iU9‑W10a `ProjektPuffer.NutzbareKapazitaetKWh` (Volumen × 1,16 × Spreizung ÷ 1000 — die Formel stand in ZWEI Masken, Befund W10‑B12; die Leerregeln bleiben je Maske) |
| `Allgemein/Lizenz/` (4) | seit iU5-U1: `LizenzManager`, `LizenzToken` (Ed25519 über BouncyCastle), `LizenzServerClient`, `GeraeteId` — die Umgebung kommt über `Dienste.Lizenzablage`, `Dienste.Pfade`, `Dienste.Einstellungen` und `Dienste.GeraeteId`. **Mit iU9‑W15c ist die reine Zustandsrechnung herausgezogen**: `LizenzManager.Bewerten(token, geraeteId, heute, anker)` beantwortet die sechs Zustände OHNE Ablage und mit vorgegebenem Datum, `Pruefe()` bleibt die Fassade (Token laden, Anker lesen, bewerten, Anker fortschreiben) — Verhalten unverändert, nur verschoben. Ohne diese Trennung liefe jeder Test gegen den echten Zeitanker und setzte ihn fort (Risiko R‑W15c‑3). `StatusText()` und `TypText()` lesen ihre neun Sätze seither aus `MyResource.Resource.LIZ_ST_*`/`LIZ_TYP_*` statt aus dem Quelltext — es war der letzte unlokalisierte Anwendertext des Lizenzwegs |
| `Allgemein/Import/` (17) | seit iU5-U1: `AnsiEncoding`, `CsvReader` (NReco, MIT), `GanglinienDatei` (CSV/TXT und Excel über ClosedXML), `CEC/` (3), `Pan/` (2), `VDI 3805/` (5 — Heizkessel, Pufferspeicher, Solarkollektoren, Wärmepumpen, `VdiAuswahlFilter`). **Mit iU9‑W12** kommt die AP5-Kette selbst dazu: `GanglinienImportAblauf` (sie stand ZWEIMAL wörtlich im Oberflächencode — mit Ablage in der Stammdatenverwaltung, ohne in der Lastspitzenkappung; die drei Entscheidungen kommen als Rückrufe herein, angezeigt wird nichts), `GanglinienOptionenModell` (die acht Steuerwertlisten des Importdialogs — Blazor und iOS brauchen dieselben Plätze in derselben Reihenfolge) und `GanglinienProtokollText` (Schlüssel → Text; die Farbe ist eine Stufe geworden, `System.Drawing` gibt es hier nicht) . **Mit iU9-W13** kommt der KATALOGIMPORT dazu: `KatalogImportProfil` (die Auspraegung der vier VDI-3805-Importe als DATEN samt dem Aufzaehlungstyp `KatalogImportArt` — Katalogschluessel, Unterordner, Dateifilter, Filtergroesse mit Vorbelegung, Detailfeldliste; der Bauplan stand viermal wortgleich im Formularcode, Befund W13-B3), `KatalogImportAblauf` (Lesen, Filtern, Vorpruefen, Ausfuehren mit `IProgress` und `CancellationToken`; der Konfliktdialog ist kein Rueckruf, sondern eine ZAESUR zwischen zwei Aufrufen), `KatalogImportSatz` mit vier Auspraegungen (die vier `FuelleModellwerte` — beim Heizkessel die einzige echte Rechnung der Vierlinge mit Brennstoffdeckel aus `Tab_Brennstoff_Stamm`, Oel-/Gas-Weiche und Platzhalter 1, bei der Waermepumpe die vier Regelungstexte als benannte Persistenzwerte) und `GanglinienTextDatei` (`ToolsClass.OpenText` ohne Dialog IM Parser, mit Kopfzeilenschalter fuer W14b). `WaermepumpenImport` liefert seine Kennlinien jetzt TYPISIERT (`KennlinienZu`) statt als `';'`-Ketten, die das Formular ein zweites Mal zerlegte (B34), und meldet einen unbekannten Aufstellungsindex, statt den ganzen Dateiimport mitzureissen (B35). In `CEC/` und `Pan/` fallen die deutschen Anzeigetexte weg (`Bifacial` ist ein Wahrheitswert, B50), die PAN-Sitzungsliste ist ein Instanzfeld statt `static` (B46), die PTC-Naeherung steht als `PanModule.PtcGeschaetzt` im Modell (B43), und `CECDataService.Filter`/`BuildWildcardMatcher` sind geloescht — die dritte Platzhaltersuche des Bestands, ohne Aufrufer (B41). **Mit iU9‑W14c** kommt `KlimaImportAblauf` dazu — Geokodierung, PVGIS-Abruf, Sonnenstandsrechnung und EINE Transaktion (Kopf, 8 760 Stunden-, 365 Tageswerte), bis dahin ein 177-Zeilen-Handler in der Oberfläche. **Der einzige Netzzugriff des Programms hängt an einem DELEGATEN** (`ITmyQuelle`): Unter Windows ist es `PVGIS_EPW_Downloader.GetTMY`, in der Probe eine eingefrorene Datei — der Ablauf ist damit ohne Internet nachweisbar (Risiko R‑W14c‑5). Angeglichen sind dabei EIN statt VIER PVGIS-Abrufen (drei wurden geholt und weggeworfen, W14c‑B28), der Sonnenwinkel als Wert statt aus einem statischen Feld (W14c‑B29), der `Listbezeichner` in den Tageswerten (W14c‑B31) und eine Dublettenprüfung, die die DATENBANK fragt und MELDET (W14c‑B26) |
| `Allgemein/Katalog/` (9) | seit iU5-U1: `DublettenPruefung`, `KatalogBereinigung`, `KatalogRegistry`; **seit iU9‑W14c** `DublettenBefundText` (Blatt- und Gruppentext als Spalten-/Wertepaare statt `DataRow` — ohne ihn zöge `EPOS.UI` `System.Data` herein, Befund W14c‑B42) und `DublettenBaum` (die vier Ebenen des Befunds als anzeigefreie Knotenliste mit SCHLÜSSEL statt Index; Wurzel und Ast von vorn offen, die Gruppe zu — bitgleich zu `BaumFuellen`). `KatalogRegistry.Anzeige` löst dabei die neunzehn Anzeigenamen ab, die als neunzehn `case` ein zweites Mal in `Form_KatalogDubletten` standen (Befund W14c‑B40); `KatalogBereinigung` bekommt `SatzUmbenennen` (der letzte verkettete `UPDATE` einer Maske, W14c‑B45) und `VerwendungZaehlen` MIT Grund — ein Fehlschlag der Prüfung ist nicht „nicht verwendet" (W14c‑B44); **seit iU9‑W14a.0a** `KatalogBrowserProfil` (die Ausprägung der vier Erzeuger-Katalogbrowser als DATEN — Stammtabelle, ein- oder zweispaltige Liste samt Textbauplan, Filterart, Detailfeldliste, Speicherweg, Schreibschutzanzeige, Meldung ohne Auswahl, Hilfeziel; Aufzählungstypen `KatalogBrowserArt`, `KatalogFilterArt`, `BrowserFeldArt`) und `ModulKatalogProfil` (dasselbe für die zwei Modulkataloge, mit der `leerErlaubt`-Regel und den dreizehn Vorbelegungen je Feld). Zwillinge zu `KatalogImportProfil`, Muster `BedarfsArt`; seit iU9‑W12 `ImportKonfliktModell` — `KonfliktAktion`, `KonfliktEntscheidung`, `ErlaubteAktionen`, `BefundText`, `NamensVorschlag` und die OK-Prüfung. Seit iU9-W13 fuehrt `KatalogRegistry` fuer `WAERMEBEDARF` ein LEERES `ImportSpalten`-Array: `null` hiess „kein Dateiimport“, und genau daran lag es, dass die Waermebedarfsverwaltung als einzige Importmaske ohne Dublettenpruefung auskam (B2). Sie lagen in `Views/Import/Form_ImportKonflikte.cs`, also in einer WinForms-Datei, die FÜNF Importmasken benutzen; solange sie dort lagen, zog jede Razor-Komponente eine WinForms-Abhängigkeit nach `EPOS.UI` (Befund W12‑B18). Die Aktion ist hier ein **Wert**, nicht der Anzeigetext einer Zelle (W12‑B19); **seit W14c‑E‑6** `KlimaWaisenBereinigung` — die zwei Löschanweisungen des Schema-Schritts 62 als EINE Wahrheit für `SchemaMigration` (Windows-App) und den Kern-Test |
| `Allgemein/Export/` (1) | seit iU5-U1: `CsvExportClass` |
| `Allgemein/KI/` (11) | seit iU5-U2 das, was der Assistent **weiß**: `HilfeWissen` (`WissensAbschnitt`), `WikiWissen`, `SemantikIndex`, `SemantikModell` (ONNX), `KiSchreibschutz`, `KiSicherungspunkt`, `KiEinwilligung`, `KiTextlieferant`, `Aktionen/KiAktionsTexte`, `Dialoge/KiDialoge`, `Dialoge/KiDialogTexte`. Was er **bedient**, bleibt bei der Oberfläche |
| `Allgemein/Hilfe/` (1) | seit iU5-U5: `DokuUebersetzung` (Wiki-URL durch den Übersetzungs-Proxy) |
| `Controller/` (106) | 106 Controller ohne Oberflächenbezug — 50 aus iU4, 29 aus iU5-U4, `EnergietraegerVarianteCtrl` aus iU8-8b (die Datenseite des ersten Blazor-Dialogs), `KostenfaktorCtrl` aus iU9-W1.5, `KostenSummenCtrl` aus iU9-W0.1 und `EnergietraegerPreisCtrl` aus iU9-W4.4 (die neun SQL-Anweisungen der Trägerkarte). **Mit iU9-W6 hat die Erzeugerseite ihre Datenseite bekommen:** `EnergietraegerVarianteCtrl.Anlegen`/`VariantenDerGruppe`/`TraegerUmhaengen` (die 185 Zeilen `CreateNewEnergyCarrier`, die ZWEIMAL wortgleich in der Oberfläche standen), die Katalogfilter und Detailblöcke in `HeizkesselStammCtrl`/`HeizkesselCtrl`/`BHKWStammCtrl`/`BHKWCtrl`/`PhotovoltaikStammCtrl`/`PufferSpStammCtrl`/`PufferSpCtrl` sowie die beiden Schreibeinstiege `Ueberschreiben`/`Anlegen` je Katalogeditor. **Mit iU9‑W7** kommen `WPCtrl` (Umzug), `WaermepumpeGeraeteCtrl` (die zweistufige Geräteauskunft Ä22) und die Datenwege der acht Wärmepumpen- und Solarmasken dazu: `WPStammCtrl.KatalogZeilen`/`GesperrtDurchProjekt`/`Speichern`, `KenndatenCtrl.Reihen`/`LiesStamm`/`Abgleichen` (transaktional), `KenndatenKuehlungCtrl.Reihen`/`HatKenndaten`, `WErzeugerCtrl.AnlagenzeileNachziehen`, `KostenSummenCtrl.AnlagenSumme`, `Z_ProjektSolarganglinieCtrl.LiesProjekt` und `SolarkollektorenStammCtrl.IdZu`/`ReadById`. **Mit iU9‑W8** kommen die drei Bedarfsblätter dazu: `BedarfStammCtrl` und `TypProfilCtrl` (neu — EINE Schnittstelle für drei Tabellen mit zwei verschiedenen Schlüsselspalten), die Schreibwege `ProzesswaermeStammCtrl.Exists`/`SaveHead`/`TypIsReadOnly`/`TypNew`/`TypDelete` (sie standen inline in zwei Masken) und die vollständige Gebäudetyp-Verwaltung in `TagVCtrl`. **Mit iU9‑W10a** kommen `PufferSpStammCtrl.Katalogzeilen` (das inline-SQL auf `Tab_Pufferspeicher_STAMM`, das in der Maske stand, Befund W10‑B27) und die drei Serialisierungswege des Quellprofils dazu — `QuellprofilCtrl.MonatswerteParsen`, `MonatswerteText` und `WochenwerteParsen` (W10‑B21). **Mit iU9‑W10b** die fünf Abfragen, die als inline-SQL in der Anzeigeschicht standen (Befund W10‑B35): `WErzeugerCtrl.AnlagenNamen`/`Quellnutzer`/`AnlagenMitWp`, `ErgebnisCtrl.LetzteErgebnisId`, `KlimaregionCtrl.Aussentemperatur`/`KlimazoneJeProjekt`/`KlimazoneJeProjektSchreiben` und **`KonfigurationCtrl.LiesProjekt`**. **Mit iU9‑W11a** kommen vier Controller der Ergebnisseite dazu: `SimulationErgebnisCtrl` (sieben DTO je Erzeuger — die rund 600 Zeilen Fachrechnung, die in `Form_Simulation_Detail` standen), `SimulationLaufCtrl` (`Vorpruefen`/`Bedarf`/`Bestuecken`/`Laufen`/`Abbruchgrund`/`ErgebnisSpeichern` — der Lauf als Kernvorgang, Fehler als RÜCKGABE statt als Dialog), `SpeicherKennzahlenBlock` (die 39 Kennzahlzeilen des Stromspeichers samt `KennzahlStufe` statt vier `Color.FromArgb`) und `SpeicherAnzeigeCtrl` (`BetriebsartText`/`BerechnungsartText`/`AmortisationText` — sie standen dreifach im Oberflächencode). **Mit iU9‑W11b (Anwenderentscheid 04.09.2026 zu W11a‑O‑1)** führen die sechs Summen von `SimulationErgebnisCtrl.Uebersicht` die **DECKUNG** je Erzeuger statt der Produktion — Direktdeckung plus zugerechnete Speicherentladung, je Kanal —, und `RestwaermebedarfMwh` ist dieselbe Zahl wie `RestwaermeMwh`, nämlich `sim.Restwaerme`. Damit gilt „Bedarf − Summe Deckung = Restwärme ≥ 0" per Konstruktion; eine negative Restwärme zeigte eine falsche Zuordnung zu den Erzeugern und darf rechnerisch nicht entstehen. Der Referenzlauf ist unberührt: `BaueErgebnis` schreibt unverändert `sim.Restwaerme`. **`KonfigurationCtrl.LiesProjekt` haben W10b und W11a gleichzeitig gebraucht** — es gibt sie einmal, dazu `ProjektLesen` für Aufrufer, die ein Steuerobjekt füllen. Erweitert sind ausserdem `HeizkesselStammCtrl.BrennstoffartenJeProjekt`, `WErzeugerCtrl.AnlagenJeTyp`/`ModelleJeTyp`/`AnlagenBezeichner` und `StromspeicherStammCtrl.KapazitaetUndLeistung`/`KapazitaetJeProjekt`. **Mit iU9‑W12** kommen drei Dateien der Lastspitzenkappung dazu: `PeakShavingCtrl` (Umzug — er war vollständig oberflächenfrei, und beim Umzug fiel `catch (OleDbException)`, das seit der SQLite-Umstellung ins Leere lief, Befunde W12‑B23/B25), `PeakShavingKennzahlenBlock` (18 Kennzahl- und 12 Monatszeilen, Muster `SpeicherKennzahlenBlock`) und `PeakShavingEingaben` (die vier Prüfregeln und die vier Einheitenumrechnungen von `ParameterLesen` — Fachaussagen, die iOS sonst ein zweites Mal hätte). Dazu `Z_ProjektStromganglinieCtrl.LiesProjekt` und `StromganglinieStammCtrl.FindeStamm` für die drei konkatenierten Abfragen der Oberfläche (W12‑B4). **Mit iU9‑W14a** bekommt die KATALOGVERWALTUNG ihre Datenseite: `KatalogsatzAnzeige` in fünf Stamm-Controllern (die sieben inline-SQL-Stellen der Browser, Befund W14‑B12), `SolarkollektorenStammCtrl.KatalogZeilen` und `StromspeicherStammCtrl.KatalogZeilen`, die zwei Speicherwege `AnzeigefelderSchreiben` (Heizkessel mit Dublettenklammer, BHKW mit Schreibschutzfrage), `BHKWStammCtrl.IstSchreibgeschuetzt`, die drei Schreibeinstiege `PufferSpStammCtrl.Anlegen`/`Ueberschreiben`/`Loeschen` sowie `PhotovoltaikStammCtrl.SpeichernAus`/`Loeschen` und `StromspeicherStammCtrl.SpeichernAus`/`Loeschen` — alle mit `SpeicherErgebnis` statt einer `MessageBox` (Befunde W14‑B22/B33/B42/B47). **`PufferSpStammCtrl.SpeichertypAbbildung`** (W14a.0d) trägt die drei DB-Werte, die drei eingefrorenen englischen Altwerte des Befunds L0‑1 und die beiden Wege `SpeichertypIndex`/`SpeichertypDbWert`. **`HeizkesselStammCtrl.Filtern` ist mit W14a.0b berichtigt** (Befund W14‑B2): `Fernwärme=23`, `Sonstige Energieträger=24`, `Wasserstoff=25` statt des nie treffenden `"Sonstige"=23` — der Kommentar „W6‑O‑1" ist damit geschlossen, und die Heizkessel-Liste ändert sich auch im schon portierten `HeizkesselDialog`. **Mit iU9‑W14b** kommt `BedarfsVorschauCtrl` dazu — die Rechnung hinter dem Knopf „Grafik“ der drei Bedarfsverwaltungen, die dreimal im Formularcode stand und sich in genau vier Punkten unterschied (Simulationsklasse, Engine-Methode, Teiler, Nachlauf; alle vier hängen an `BedarfsArt`). Sie ist **bitgleich je Art** — einschließlich des fehlenden Teilers beim Brauchwasser (Befund W14‑B49): Der Wert liegt in kWh, und genau so nennt ihn die Ergebnishülle seit dem Entscheid W8‑O‑5. Erweitert sind `BedarfStammCtrl` (`Bezeichner`, `Kopf`, `Loeschen` mit dem Aufzählungstyp `BedarfLoeschErgebnis` — die drei `Delete` der Stammcontroller MELDEN ihre ReadOnly-Sperre, und das wäre in einer WebView ein modaler Kasten) und `SolarganglinieStammCtrl` (`Exists` statt der Präfixsuche `listBox.FindString`, Befund W14‑B70; `HatProjektzuordnung` statt des verketteten inline-SQL, W14‑B12). **Mit iU9‑W14c** kommen ZWEI Controller dazu: **`KlimaregionStammCtrl`** (Umzug — er zog über `FillComboBox(ComboBox)`/`FillListBox(ListBox)` `System.Windows.Forms` in die Controllerschicht, Befund W14c‑B33; an ihre Stelle tritt `Bezeichner()`, `ReadSingle(sql)` wird das parametrierte `ReadByName(name)`, und `Delete` löscht seither MIT Kaskade über `KatalogBereinigung.SatzLoeschen` — der Vorläufer liess 8 760 + 365 Zeilen als Waisen stehen, Befund W14c‑B23) und **`EinstellungenCtrl`** (neu — der ERSTE schreibende Weg zu den neun `Properties.Settings`-Schlüsseln ausserhalb einer Maske, Befund W14c‑B57; die vier Vorgabepfade laufen über `Dienste.Pfade` statt `Environment.GetFolderPath(SpecialFolder…)`, das hier verboten ist, W14c‑B55). Erweitert ist `GesetzKatalog` um `KlassenVorrat`/`Einheiten`/`Statuswerte`/`KlasseAnzeige`/`WertText` (die Steuerwertlisten standen als ZWEITE Quelle in der Maske, W14c‑B5), `Pruefe`/`Existiert` (dieselbe Prüfung stand zweimal, W14c‑B7; die Dublettenprüfung ist jetzt eine SQL-Zählung statt eines Katalognachladens, W14c‑B12) und `Zeilen(klasse)` mit `GesetzZeile`; `SolardatenCtrl.ReadAllStamm` ist auf `DbParam` umgestellt (W14c‑B18b). **Mit iU9‑W15a kommt `ProjektExportImportCtrl` dazu** (1 278 Z., Umzug — seine einzige Kante war die Zahl `SchemaMigration.ZIEL_VERSION`, siehe `SchemaStand.Zielversion`; damit ist der Projekttransfer auf iOS moeglich). Erweitert sind `ProjektCtrl` (`IdVonName`, `NamenListe`, `Kopf`, `LoeschenMitVorarbeiten` — die sechs Schritte des Loeschwegs aus `MenueCtrl` ohne die zwei Dialogaufrufe, Befund W15a‑B48/B50), `ProjektDuplizierenCtrl` (`PruefeNamen` als abfragbares Ergebnis statt der Praefixsuche der Maske, W15a‑B10; `VerwaltungsfelderSetzen` mit unveraenderter Fehlerpolitik, W15a‑B11/B47; `Duplizieren` mit `CancellationToken` und Rollback) und `KlimaregionStammCtrl` (`IdVonName`, `NameZuProjektregion` mit STAMM-Rueckfall — die drei verketteten `RecordSet`-Abfragen von `Wizard_Projekt`, W15a‑B32). **Mit iU9‑W9.8 kommt `GebaeudeBedarfCtrl` dazu** — der Wärmebedarf EINES Gebäudes hinter dem Knopf „Simulation…" des Gebäudedialogs (Anwenderwunsch W9‑E‑2, 05.09.2026). Er RUFT den Rechenweg des Laufs, statt ihn abzuschreiben: `SimulationWaermebedarf.KlimakalenderLesen` und `…HeizwaermeEinesGebaeudes` sind aus `Waermebedarf_berechnen` herausgezogen und werden von beiden Seiten gerufen — siehe die Hausregel weiter unten |
| `Model/` (52) | alle Modelle; seit iU9‑W8 der Aufzählungstyp `BedarfsArt` — er liegt hier und nicht in `EPOS.UI`, weil ihn BEIDE Seiten brauchen: Die Controller verteilen danach auf drei Tabellen, die Razor-Komponenten wählen danach ihre Beschriftungen. Seit iU9‑W10b dazu `AnlagenInfo` — die Zeile aus `Tab_Energieanlagen` samt ihrer Senkenkette, bis dahin eine PRIVATE Klasse in `Form_Simulation_Config`. **Seit iU9‑W15a** `ProjektAngaben.cs` mit `ProjektKopfZeile` (die EINE Projektliste — der Bestand fuehrte VIER, Befund W15a‑B52), `ProjektKopfDaten` (die neun Felder der ersten Assistentenseite statt zehn `Get*`-Methoden, W15a‑B42) und den drei Befundtypen `DuplizierBefund`, `VerwaltungsfelderBefund` und `LoeschBefund`/`LoeschStand` |
| `Controller/` — Nachtrag iU9‑W15c | **Drei Controller der Lizenzseite**: `LizenzCtrl` (das Lagebild der Lizenzverwaltung — sprachneutraler Zustandsname, Statustext, Detailtext, `HatToken`, `GeraetName`, `PortalUrl`, die WordPress-E-Mail-Regel `EmailGueltig` und die vier `await`-Wege als Tupel statt als Kern-Antworttyp), `LizenzTextCtrl` (woher der Vertragstext kommt: Dateisuche über `Dienste.Pfade`, Zwischenspeicher, Onlineabruf, `HtmlZuText`, `StandFormatieren` — **die Quelle ist EINE Zeile**, heute die AGB-Seite, siehe Befund W15c‑B27) und `ZustimmungCtrl` (die Zustimmung beim ersten Start über `Dienste.Einstellungen`, unter Windows derselbe Registry-Zweig wie vorher; **Fehlerpfad `catch → true`, wortgleich übernommen** — eine nicht lesbare Ablage darf den Start nicht blockieren) |
| `Controller/` — Nachtrag iU9‑W16b | **Zwei Controller der Startseite** und ein Zustand. **`ProjektKontextCtrl`** (K2, W16b.0 — das gerade geöffnete Projekt: Id, Name, Klimazone, `Setzen`/`Uebernehmen`, Ereignis `Gewechselt`, `Tab_Applikation`. Er ist seit W16b.3 `Dienste.Projekt`; bis dahin war es `FormStartProjektKontext`, eine Fassade auf ein FELD der Startmaske (Befund W16‑B6). `Setzen` und `Uebernehmen` sind getrennt, weil der Bestand sie unterscheidet: Die drei Projektkacheln merken sich das Projekt, der Variantenwechsel im Kopfband und die Menüwege „Neu"/„Bearbeiten" nicht. Nachweis **N7** in `EPOS.Kern.Tests/ProjektKontextCtrlTests.cs`, einschließlich des Projektwechsel-Falls zu Risiko R‑W16‑4. **Die Klimazone ist seit dem Anwenderentscheid W16b‑O‑3 vom 04.09.2026 auf BEIDEN Plattformen die PROJEKTKOPIE** — `StartseiteCtrl.ProjektKlimazone`; damit gibt es für „welches Projekt ist offen" **eine** Umsetzung, und `EPOS.iOS/Dienste/IosProjektKontext` ist nur noch eine dünne Weiterleitung auf DIESE Klasse. N7 zählt dafür **15 statt 12 Fälle**). **`StartseiteCtrl`** (K4, W16b.0 — Klimaregionen lesen und speichern, Variantengruppe, Projektname; die vier Abfragen von `Form_Start` (:356/369/382/390) standen hier parametriert, die verkettete bei :369 läuft über `KlimaregionStammCtrl.IdVonName`, Befund W16‑B11. **Seit W16b‑O‑3 sind es drei**: `:356` — der Regionsname zur STAMM-Id, zuletzt `KlimaregionName(int)` — ist **ersatzlos gefallen**. Der Entscheid lautete „nehme iOS-Lösung"; die Messung dazu zeigte, dass die iOS-Abfrage **den falschen Schlüsselraum las**: An `Tab_Projekt.ID_Klimaregion` steht die Id der PROJEKTKOPIE (`Tab_Klimaregion.ID`, Ids ab 1 006 017), die Abfrage hielt sie gegen `Tab_Klimaregion_STAMM.ID_Klimaregion` (Ids 1…50) — Überschneidung **0**, Antwort für jedes Projekt des Bestands leer. Es war kein zweiter Weg, sondern ein Fehler, und `KlimaregionName` hatte seit K6‑a ohnehin keinen Aufrufer (Befund W16b‑B3). Vereinheitlicht ist deshalb auf die Projektkopie: **`ProjektKlimazone`** (der frühere `ProjektKlimaregion`, umbenannt weil er jetzt die EINE Wahrheit beider Plattformen ist) liest `:382` und `:390` unverändert und **ohne Stamm-Rückfall**; der angezeigte Text ist derselbe wie vorher. Messung im W16b-Protokoll § 6). Dazu **`Allgemein/Simulation/BedarfsZustand`** (W16b.4, E‑5 — die zwei Bedarfsrechnungen eines Projekts. `Form_Start` besaß sie als zwei Felder und reichte sie an die Ergebnisansicht durch; genau das war der Grund für deren Modalität, Befund W11‑B3/W16‑B29. Sie gehören jetzt dem PROJEKT und werden bei einem Wechsel verworfen) |
| `Controller/` — Nachtrag iU9‑W16a | **Drei Controller des Projektassistenten.** `KomponentenBestandCtrl` (K1, W16a.0 — **unverändert verschoben** aus `Views/Wizard/KomponentenBestand.cs`: die dreizehn Bitwerte, `Bitmaske`, `NachSeite`, `Lesen`; sie war reine Datenlogik ohne eine Zeile WinForms. Nachweis N6 in `EPOS.Kern.Tests/KomponentenBestandTests.cs`: bitgleich zum eingefrorenen `Form_Start.status`-Wert für **alle dreizehn** Referenzprojekte — damit ist Entscheid E‑3 belegt, nicht nur behauptet). `WizardCtrl` (W16a.4, Umzug — siehe oben). **`AssistentCtrl`** (K3, W16a.4 — die sieben Zustandslisten, die sechs Ladewege mit ihren sechs Inline-SQL, die Seitenschaltung `NaechsteAktive`/`LetzteAktive`, die beiden Filter und `Speichern` mit **bitgleicher** Reihenfolge der Schreibschritte. Neu ist allein, dass ein Fehlschlag GEMELDET statt verschwiegen wird: `AssistentErgebnis` nennt den Schritt, und der Aufrufer zeigt EINE Meldung — der Vorläufer brach siebzehnmal kommentarlos ab, Befund W16‑B16, Entscheid E‑4. **Offen bleibt die TRANSAKTION**: ein `DbVorgang` über den ganzen Speicherlauf setzte voraus, dass alle 23 Schreibmethoden von `WizardCtrl` ihn hereingereicht bekommen — ein Umbau des Schreibwegs, den Risiko R‑W16‑6 ohne Windows-Feldvergleich untersagt) |
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
| `Update/SchemaMigration`, `GeraeteWaisen`, `ErststartMigration`, `SchemaVersionAccess`, `DbParamOleDb` | der eingefrorene Access-Zweig — `System.Data.OleDb` |
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
die System-SQLite des Geräts wäre für die **114 STRICT-Tabellen** der Datenbank nicht steuerbar.
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
Access-Zweig der Erststart-Migration — `SchemaMigration`, `GeraeteWaisen` und
`SchemaVersionAccess` (die aus `ApplikationCtrl` ausgelagerten Schemamarker-Methoden).
Wer hier eine neue Zugriffsstelle schreibt, nimmt `DbParam` — sonst nichts.

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
| `Pfade` | `%APPDATA%\wp-plan`, `%APPDATA%\<Produkt>`, `LocalApplicationData[\WP-Plan]`, `CommonApplicationData\WP-Plan`, Dokumente | `StandardPfade` — `Environment.SpecialFolder` |
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
| `SimulationControl.Speicherlauf` | der Stromspeicherzweig (K8) | wird vom `[ModuleInitializer]` in `SimulationControl.Stromspeicher.cs` gesetzt, sobald diese Assembly lädt |
| `SimulationRunner.Speicherergebnismodell` | dasselbe für das Ergebnismodell | wie oben |
| `WErzeugerCtrl.GeraetewaisenAufraeumen` | Aufräumlauf nach dem Löschen eines Projekts | `null` = kein Lauf; zulässig, weil er ohnehin nach dem erfolgreichen DELETE läuft und der Migrationsschritt nachholt |
| `DataRepository.Zugriff` | die Umsetzung hinter `IDatenzugriff` (iU6-T4) | `new SqliteDatenzugriff()`; wird in iU5 an `Dienste.Daten` gehängt |

**ResX und Settings pflegen.** Der Anzeigetext-Katalog liegt jetzt hier; `Resource.Designer.cs`
ist **eingecheckter Quelltext** und muss beim Ergänzen von Schlüsseln mitgepflegt werden. Nur die
neutrale `Resource.resx` trägt den Code-Generator, die Satellitendatei nicht. Der `LogicalName`
beider Dateien ist im `.csproj` festgeschrieben
(`WindowsFormsApplication1.MyResource.Resource[.en-US].resources`), damit der Ressourcenname nicht
am Ordnerpfad hängt — der Basisname in `Resource.Designer.cs` bleibt dadurch gültig. Visual Studio
regeneriert die Designer-Datei bei jeder `.resx`-Änderung selbst; wer parallel von Hand ergänzt
hat, baut Duplikate (CS0102).

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
