# Befund N — Entwurf des IFC-Gebäudeimports (15.09.2026)

**Protokoll.** Befund N eines Erkundungs-Agenten (Modell Opus, nur lesend) im Auftrag des Umsetzungskonzepts [`../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026.

---

## 0. Das Ergebnis in sieben Sätzen

1. **Der Import braucht keine neue Architektur.** `EPOS.Kern/Allgemein/Import/` trägt das Muster
   Ablauf/Profil/Satz bereits dreifach ausgeprägt; `IfcImportAblauf` + `IfcImportSatz` fügen sich
   ohne Bruch ein (`KatalogImportAblauf.cs:81`, `KatalogImportProfil.cs:196`, `KatalogImportSatz.cs:28`).
2. **Die Bibliothekswahl ist enger als E3 sie beschreibt:** Das Metapaket `Xbim.Essentials` zieht
   `Xbim.Ifc` und damit `Xbim.IO.Esent` nach; in den Kern gehört deshalb **allein**
   `Xbim.IO.MemoryModel` 6.1.605 — es referenziert `Xbim.Common`, `Xbim.Ifc2x3`, `Xbim.Ifc4` und
   `Xbim.Ifc4x3` von selbst und kommt ohne Esent aus (Belege in 3.1).
3. **Ein Leser genügt für alle drei Schemata:** `Xbim.Ifc2x3` und `Xbim.Ifc4x3` tragen je einen
   Ordner `Interfaces/IFC4` und bedienen damit dieselben `Xbim.Ifc4.Interfaces.IIfc*`-Schnittstellen
   wie IFC4 — der Leser wird gegen die Schnittstellen geschrieben, nie gegen eine Schemaklasse.
4. **Die Zielstruktur des Dialogs steht schon fest:** Entscheid E2 verlangt Zeilen `U | A | U·A` je
   Bauteilgruppe; die Felder dazu liegen fertig in `GebaeudeKatalogDaten` (`GebaeudeKatalogDaten.cs:85-96`),
   der Import füllt genau sie — mit Herkunftskennzeichen daneben.
5. **Die Baualtersklasse ist kein A–H, sondern A–U mit 21 Einträgen**
   (`GebaeudeStammCtrl.cs:112-119`); A–H sind die Baujahrspannen bis 2000, I–U die Standards
   (Niedrigenergie … BEG 40). Damit gibt es die im Konzept 6.1 vermisste Zuordnung
   Baujahr → Klasse doch — für A–H eindeutig, ab I nur über eine Anwenderangabe.
6. **Die Plattformnaht ist vorhanden, aber unvollständig:** `IDateiDienst.DateiOeffnenAsync`
   (`IDateiDienst.cs:107`) trägt die Dateiwahl auf beiden Schalen, `KeineDateiwahl` lehnt benannt ab
   (`KeineDateiwahl.cs:14`) — die iOS-Filterkarte kennt `.ifc` aber nicht
   (`EPOS.iOS/Dienste/Dateifilter.cs:25-44`), und ein Größenlimit gibt es im ganzen Bestand nicht.
7. **Aufwand:** 13–21 PT für G4a (Konzept 11 nennt 10–20), davon rund ein Drittel Zuordnungsdialog
   und Vorgabetabelle, nicht Parsen.

---

## 1. Was der Bestand für einen Import schon hat

### 1.1 Das Dreigespann Ablauf / Profil / Satz

| Baustein | Fundstelle | Rolle |
|---|---|---|
| `KatalogImportAblauf` | `EPOS.Kern/Allgemein/Import/KatalogImportAblauf.cs:81` | **Lesen → Vorprüfen → Ausführen**, drei getrennte Methoden, je mit Melder und Abbruchzeichen |
| `KatalogImportProfil` | `KatalogImportProfil.cs:196`, Auswahl über `Finde(art, text)` `:315` | Was den Lauf unterscheidet, steht als **Daten**: Dateifilter, Unterordner, Detailfelder, Listenspalten, Katalogschlüssel |
| `KatalogImportSatz` | `KatalogImportSatz.cs:28` | abstrakt; je Ausprägung ein `Anlegen(bezeichner)` `:65` und ein `Ueberschreiben(bestandsId)` `:68`, dazu `Vergleichswerte(bezeichner)` `:62` für die Dublettenprüfung |
| `KlimaImportAblauf` | `KlimaImportAblauf.cs:126`, `Laufen(...)` `:149` | statischer Ablauf mit **Auftrag → Ergebnis** (`KlimaImportAuftrag` `:65`, `KlimaImportErgebnis` `:29`, `KlimaImportAusgang` `:10`) |
| `GanglinienImportAblauf` | `GanglinienImportAblauf.cs:173` | Ablauf mit **Rückrufen** für Optionen, Protokoll und Konflikte (`GanglinienImportRueckrufe:62`) und einem **Ziel**-Objekt mit `Anlegen`/`Ersetzen`-Delegaten (`GanglinienZiel:101`) |

Drei Regeln sind für den IFC-Weg verbindlich und stehen wörtlich im Bestand:

- **Der Ablauf zeigt nichts an.** „Der Konfliktdialog ist kein Rückruf, sondern eine Zäsur"
  (`KatalogImportAblauf.cs:69-74`): der Wirt ruft `Vorpruefen`, zeigt seine Überlagerung und ruft
  dann `Ausfuehren` mit den Entscheidungen.
- **Ein fehlerhafter Eintrag bricht den Lauf nicht ab** (`KatalogImportAblauf.cs:376-381`) — er
  zählt als Fehler, die Schleife läuft weiter; nur `OperationCanceledException` beendet sie.
- **Der Zustand lebt im Ablauf, nicht in der Komponente.** Die Hülle legt den Ablauf einmal an und
  reicht der Razor-Komponente nur Delegaten (`KatalogImportHuelle.cs:111-142`) — die Komponente
  sieht nie einen Satz, der schreiben könnte (`KatalogImportHuelle.cs:210-233`).

### 1.2 Fehlerbilder und Meldungen

Es gibt **keine benannten Ausnahmeklassen** im Importbestand. Das Hausmuster ist die
sprachneutrale Meldungszeile:

- `PruefStufe` (`SpeicherEngine/GanglinienPruefung.cs:55`): `Info` / `Warnung` / `Fehler` —
  „Auffälligkeit; der Import darf trotzdem laufen" gegen „Abbruchgrund".
- `PruefMeldung` (`GanglinienPruefung.cs:74`): **Schlüssel + invariant formatierte Werte**, nie Text.
  Den Text holt erst die Oberfläche aus `MyResource.Resource` (Drei-Schichten-Regel).
- Ein Lesefehler wird gefangen und als Meldung gelegt, nicht geworfen:
  `_meldungen.Add(new PruefMeldung(PruefStufe.Fehler, "IMP_KAT_PROT_LESEFEHLER", ex.Message))`
  (`KatalogImportAblauf.cs:185-187`).
- Die Schlüsselnamen folgen `IMP_<BEREICH>_PROT_<FALL>` bzw. `IMPORT_PROT_*`
  (`KatalogImportAblauf.cs:128,190`).

**Folge für den Entwurf:** `IfcImportAblauf` wirft nichts nach außen; jeder Befund ist eine
`PruefMeldung` mit Schlüssel `IMP_IFC_PROT_*`. Nur der Abbruch des Anwenders reist als
`OperationCanceledException`.

### 1.3 Fortschritt, Abbruch, Fadenwechsel

- `ImportFortschritt` (`KatalogImportAblauf.cs:9`): `Anteil` (0…1 oder `null` = unbestimmt),
  `Schluessel`, `Werte`.
- `ImportBilanz` (`:31`): die fünf Zähler der Sammelmeldung.
- Die Komponente zeigt `<Fortschritt Sichtbar Anteil Text>` (`KatalogImportDialog.razor:121`) und
  `<Warnbanner Stufe Text>` (`:99`); beides sind vorhandene Bausteine.
- **Der Fadenwechsel gehört in die Hülle, nicht in den Ablauf.** In der Windows-Hülle steht
  `await Task.Run(...)` (`KatalogImportHuelle.cs:229`). Das ist dort erlaubt, weil der Wächter
  `ParallelitaetWacheTests` nur `EPOS.Kern`, `SpeicherEngine`, `KiKern`, `EPOS.UI.Daten` und
  `EPOS.UI` prüft (`EPOS.Kern.Tests/ParallelitaetWacheTests.cs:52`). Eine **plattformfreie** Hülle
  muss `Kulturweitergabe.Starten` nehmen — so macht es `SpotpreisImportHuelle`
  (`EPOS.UI.Daten/Kosten/SpotpreisImportHuelle.cs:61,71`; API `Kulturweitergabe.cs:152/170`).

### 1.4 Wie Dialoge die Importe rufen

| Dialog | Fundstelle | Muster |
|---|---|---|
| `KatalogImportDialog.razor` | `EPOS.UI/Dialoge/Import/` (941 Z.) | fünf Ausprägungen, alle Datenwege als Delegat-Parameter (`:241` `Lesen`, `:244` `Vorpruefen`, `:251` `Ausfuehren`) |
| `ImportKonflikteDialog.razor` | `EPOS.UI/Dialoge/Import/` (281 Z.) | Zeilen `Name | Befund | Aktion`; `Geschlossen` liefert `List<KonfliktEntscheidung>?`, **`null` = Abbrechen, es wird nichts geschrieben** (`:96`, `:270`); Esc = Abbrechen, Enter nicht belegt (`:23`) |
| `GebaeudeKatalogDialog.razor` | `EPOS.UI/Dialoge/Bedarf/` (983 Z.) | Feldsatz als `Daten`-Parameter (`:374`), Lesen über `Lies` (`:392`), Schreiben über `Speichern(daten, istNeu, bezeichner)` (`:399`) |
| Windows-Hüllen | `Views/Import/KatalogImportHuelle.cs:81`, `Views/Gebäude/GebaeudeKatalogHuelle.cs:33` | `BlazorDialogForm<T>` mit `Gaben(...)`-Wörterbuch; `Gaben` ist **ohne** `Geschlossen` gebaut, damit dieselbe Gabe auch eine Überlagerung im selben Fenster speisen kann (`GebaeudeKatalogHuelle.cs:85-88`) |

**Die Hausregel OK/Abbrechen:** nichts wird ohne OK geschrieben; der Abbruch liefert `null` bzw.
`false` und der Wirt verwirft. Der Konfliktdialog gibt **alle** Zeilen zurück, auch die
konfliktfreien, damit der Wirt „übersprungen" zählen kann (`ImportKonflikteDialog.razor:18-21`).

### 1.5 Das Konfliktmodell als Vorbild für einen Zuordnungsdialog

`EPOS.Kern/Allgemein/Katalog/ImportKonfliktModell.cs` ist das nächstliegende Vorbild:

- `KonfliktAktion` (`:7`) ist ein **Wert, kein Anzeigetext** — „Kein Anzeigetext darf Steuerwert
  sein" (`:44-46`); `AktionText(a)` (`:48`) liefert nur die Beschriftung.
- `ErlaubteAktionen(pruefung, out vorbelegung)` (`:63`) — welche Wahl je Befund erlaubt ist und wie
  sie vorbelegt wird, entscheidet der **Kern**.
- `BefundText` (`:106`), `KopfText(gesamt, konflikte)` (`:155`), `NamensVorschlag` (`:162`),
  `Pruefe(entscheidungen, …) → Beanstandung` (`:180,191`), `BeanstandungsText` (`:218`).

Für den IFC-Import ist die Entsprechung: **`IfcZuordnungsModell`** mit `HerkunftText`,
`ErlaubteHerkuenfte` und `Pruefe` — dieselbe Arbeitsteilung, nur mit `IfcHerkunft` statt
`KonfliktAktion`.

### 1.6 `IDateiDienst` und die Adapter

```csharp
public interface IDateiDienst                                  // IDateiDienst.cs:12
{
    string DateiOeffnen(string titel, string filter, string startOrdner);          // :19  "" = abgebrochen
    string DateiSpeichern(string titel, string filter, string vorschlag);          // :25
    string OrdnerWaehlen(string titel, string startOrdner);                        // :28
    bool   MitSystemOeffnen(string pfad);                                          // :34
    bool   AdresseOeffnen(string adresse) => false;                                // :51
    string[] DateienOeffnen(string titel, string filter, string startOrdner);      // :63
    Task<string> DateiOeffnenAsync(string titel, string filter, string startOrdner); // :107
}
```

- **Die Async-Zwillinge sind Pflicht, nicht Geschmack** (`IDateiDienst.cs:74-105`): ein synchroner
  Wähler aus einem Blazor-Ereignis pumpt unter Windows eine verschachtelte Nachrichtenschleife im
  `WebMessageReceived`-Rückruf; auf iOS liefert `IosDateiDienst.AufDemHauptfaden` vom Hauptfaden aus
  `default` und der Wähler geht gar nicht erst auf. Der IFC-Wähler nimmt also `DateiOeffnenAsync`.
- **Windows:** `WindowsDateiDienst` über `Blazornachlauf`; der Startordner kommt beim Katalogimport
  aus `EinstellungenCtrl.HerstellerdatenpfadOderVorgabe()` (`KatalogImportHuelle.cs:188-201`).
  Für IFC gibt es keinen sinnvollen Auslieferungsordner — Startordner bleibt leer.
- **iOS:** `IosDateiDienst` (`EPOS.iOS/Dienste/IosDateiDienst.cs:36`) über `FilePicker.Default.PickAsync`,
  `DateiOeffnenAsync` `:146`. Der Windows-Filter wird in UTI übersetzt
  (`EPOS.iOS/Dienste/Dateifilter.cs:51`); **`.ifc`, `.ifcxml` und `.ifczip` fehlen in der Tabelle
  `:25-44`** und fallen auf `public.data` (`:22`) zurück. Das funktioniert („lieber ein Wähler, der
  zu viel anbietet"), zeigt dem Anwender aber jede Datei. **Ein Eintrag ist mit G4 nachzutragen** —
  eine registrierte UTI für IFC gibt es nicht, deshalb bleibt es bei `public.data`, aber der
  Kommentar dort soll den Fall benennen wie beim `.lic`-Fall (`Dateifilter.cs:37-40`).
- **Ohne Oberfläche:** `KeineDateiwahl` (`KeineDateiwahl.cs:14`) liefert `""` — der Aufrufer prüft
  auf leer und tut nichts. Kein Sonderfall für IFC nötig.

### 1.7 Größenlimits: es gibt keine

Eine Suche über `EPOS.Kern/Allgemein/Import/` und `ProjektExportImportCtrl.cs` findet **keine
einzige Datei- oder Speichergrenze**. Der Bestand begründet die Fadenauslagerung stattdessen mit
der Größe der Probendatei („92 376 Zeilen und 8,3 MB", `KatalogImportAblauf.cs:75-79`). Das
Größenlimit aus Konzept 7.6 Nr. 2 ist damit **neu** und muss samt Meldung und Test entstehen.

---

## 2. Paketverwaltung, Lizenzhinweise, Trimming

### 2.1 Welches xBIM-Paket in den Kern gehört

Gemessen an den nuspec-Dateien auf nuget.org (Abruf 15.09.2026):

| Paket 6.1.605 | Zielrahmen | Abhängigkeiten | Lizenz |
|---|---|---|---|
| `Xbim.IO.MemoryModel` | net10.0, net8.0, netstandard2.0/2.1 | `Xbim.Common`, `Xbim.Ifc2x3`, `Xbim.Ifc4`, `Xbim.Ifc4x3` | CDDL-1.0 |
| `Xbim.Ifc` | dieselben | zusätzlich **`Xbim.IO.Esent`** | CDDL-1.0 |
| `Xbim.Essentials` (Metapaket) | dieselben | `Xbim.Common`, **`Xbim.Ifc`**, `Xbim.Ifc4`, `Xbim.Ifc2x3`, `Xbim.Ifc4x3`, **`Xbim.IO.Esent`**, `Xbim.IO.MemoryModel` | CDDL-1.0 |

**Befund gegen E3:** Der Entscheid nennt „`Xbim.Essentials` 6.1.605 (`Xbim.Ifc2x3`, `Xbim.Ifc4`,
`Xbim.Ifc4x3`, `Xbim.IO.MemoryModel`)" — die Klammer ist richtig, das Metapaket davor ist es nicht:
es zieht `Xbim.Ifc` und `Xbim.IO.Esent` mit. `Xbim.IO.Esent` bindet ManagedEsent (Windows-Datenbank)
und ist auf iOS nicht tragbar; `IfcStore` aus dem Schnellstart der xBIM-Dokumentation lebt genau
dort. **Der Kern nimmt deshalb genau eine Zeile:**

```xml
<!-- Directory.Packages.props, neue ItemGroup "IFC-Import (G4)" -->
<PackageVersion Include="Xbim.IO.MemoryModel" Version="6.1.605" />
```

Die drei Schemapakete kommen als transitive Abhängigkeit mit; `CentralPackageTransitivePinningEnabled`
steht auf `false` (`Directory.Packages.props:15`), sie brauchen also keine eigene Zeile. In
`EPOS.Kern.csproj` steht dann nur `<PackageReference Include="Xbim.IO.MemoryModel" />` — die Regel
„in den .csproj steht nur noch `Include`, die Version ausschließlich hier"
(`Directory.Packages.props:6-8`). `Microsoft.Extensions.Logging` ist bereits zentral geführt
(`Directory.Packages.props:30`) und deckt den `ILoggerFactory`, den `MemoryModel` über
`XbimServices` zieht.

**Das wird nicht gerufen:** `IfcStore.Open` (Namensraum `Xbim.Ifc`) — es ist der
Dokumentations-Schnellstart, aber es ist der Esent-Weg.

### 2.2 Lizenzhinweise im Setup

`Setup/` führt **keine** Seite für Fremdbibliotheken. Vorhanden sind:

- `Setup/EPOS-Plan.iss:164-165` — `LicenseFile={#SetupDir}Lizenz.rtf`, nur wenn die Datei existiert;
- `Setup/EPOS-Plan.iss:330-331` — dieselbe Datei wird nach `{app}` kopiert;
- für Datenlizenzen gibt es das Muster **Beipackzettel neben den Daten**:
  `VDI-3805-Daten/Stromspeicher/LIESMICH_bslib.md` und `VDI-3805-Daten/PV/LIESMICH_CEC_Inverters.md`
  (CC BY 4.0, Namensnennung), die über die Komponente `herstellerdaten` mit
  `recursesubdirs createallsubdirs` von selbst mitreisen.

**Es fehlt also die Stelle, an der CDDL-1.0 landen kann.** Vorschlag (Aufwand 0,5 PT, Teil von G4):

1. Neue Datei `Setup/Vorlage/Lizenzhinweise.txt` (bzw. `.rtf`) mit je Fremdbibliothek: Name,
   Version, Lizenz, Copyright-Vermerk, dauerhafter Verweis auf den Quelltext. Erster Eintrag
   `Xbim.IO.MemoryModel 6.1.605 / Xbim.Common / Xbim.Ifc2x3 / Xbim.Ifc4 / Xbim.Ifc4x3` — CDDL-1.0,
   Quelltext `github.com/xBimTeam/XbimEssentials` bzw. die NuGet-Quellpakete, **der Verweis ist dem
   Empfänger mitzuteilen** (CDDL § 3.1, siehe Gegenlesen IFC, Befund 1). Zugleich werden die schon
   ausgelieferten Fremdanteile (SkiaSharp, ClosedXML, DocumentFormat.OpenXml, BouncyCastle,
   SixLabors.Fonts 1.0.1, Microsoft.Data.Sqlite/SQLitePCLRaw, MathNet.Numerics, Humanizer) dort
   nachgeführt — das ist ohnehin fällig und nicht Sache dieses Befundes, aber die Seite entsteht hier.
2. `EPOS-Plan.iss`: eine `Source:`-Zeile nach dem Muster `:330-331`, Komponente `programm`.
3. Der Pflegeweg: **eine Zeile je `PackageVersion` in `Directory.Packages.props`, die ausgeliefert
   wird.** Ein Wächtertest in `EPOS.Kern.Tests` kann die beiden Listen gegeneinander halten —
   Vorschlag, kein Muss.
4. **Nie forken** (E3): weder Quelltext übernehmen noch patchen, sonst greift CDDL § 3.1 auf die
   geänderten Dateien.

### 2.3 iOS: Trimming

`EPOS.iOS/EPOS.iOS.csproj` setzt **weder `TrimMode` noch `PublishTrimmed`**, und es gibt
**keinen `TrimmerRootDescriptor`** im Repositorium. `Directory.Build.props` setzt ebenfalls nichts.
Der CI-Lauf baut `-c Debug` (`.github/workflows/ios.yml:125`) — in Debug trimmt das iOS-SDK nicht,
der Lauf beweist also **nichts** über das Trimming. Die Vorlage für eine bedingte Ausnahme steht
schon da: `Microsoft.ML.OnnxRuntime` wird mit `ExcludeAssets="native;build;buildTransitive"`
entschärft (`EPOS.iOS.csproj:113`).

Das Risiko ist benannt: `ExpressMetaData` ruft `module.GetTypes()` (Befund C, Abschnitt 4;
im Gegenlesen IFC unter „Geprüft und bestätigt" bestätigt). Der Entwurf:

```xml
<!-- EPOS.iOS.csproj, neue ItemGroup -->
<ItemGroup>
  <TrimmerRootDescriptor Include="Pruefung\XbimRoots.xml" />
</ItemGroup>
```

```xml
<!-- EPOS.iOS/Pruefung/XbimRoots.xml -->
<linker>
  <assembly fullname="Xbim.Ifc4"   preserve="all" />
  <assembly fullname="Xbim.Ifc2x3" preserve="all" />
  <assembly fullname="Xbim.Ifc4x3" preserve="all" />
  <assembly fullname="Xbim.Common" preserve="all" />
</linker>
```

`preserve="all"` ist grob und kostet Paketgröße (rund 10 MB Assemblies, Konzept 12); feiner ginge
es über `<type>`-Listen, das ist aber erst zu schneiden, wenn ein Release-Bau die tatsächlich
gebrauchten Typen zeigt. **Der Nachweis gehört in G4**: ein Release-Bau für `ios-arm64` und der
Prüfmodus liest die KIT-Datei; erst dann ist die Aussage belastbar. Er kostet einen macOS-Lauf
(zehnfaches Kontingent) und ist **beim Anwender zu erfragen** (Wurzel-`CLAUDE.md`, Abschnitt
„CI und Läufer-Kontingent").

---

## 3. Menü, Seiten, Registrierung

- **Das Menü ist Daten:** `EPOS.UI/Bausteine/Menuetabelle.cs`. Die Importrubrik ist
  `MenuItem_DatImport` (`:235`), die Gebäuderubrik `MenuItem_Gebaeude` (`:274`) mit
  `MenuItem_GebBearbeiten → Seitenschluessel.GebaeudeAdmin` (`:276`).
- **Ein Seitenschlüssel ist ein Maskenname:** `Seitenschluessel.GebaeudeAdmin` (`:216`) verweist auf
  `Masken.GebaeudeAdmin = "Form_Gebaeude"` (`EPOS.Kern/Allgemein/Dienste/Masken.cs:20`); die
  Sammelliste am Ende (`Seitenschluessel.cs:397-403`) führt jeden Razor-Schlüssel.
- **Der IFC-Import bekommt keinen Menüpunkt** (Konzept 8.4): er ist projektbezogen, kein
  Katalogimport, und startet aus `GebaeudeDialog.razor` bzw. `GebaeudeKatalogDialog.razor`. Damit
  entfällt auch ein neuer Maskenschlüssel; der Dialog erscheint als **Überlagerung im selben
  Fenster** — dasselbe Muster wie die Brauchwasserliste im Katalogeditor
  (`GebaeudeKatalogDialog.razor:401-405`, „Sie erscheint als ÜBERLAGERUNG im selben Fenster
  (Risiko R2)") und wie `SpotpreisImportHuelle` in der Energieträgerverwaltung
  (`SpotpreisImportHuelle.cs:34-39`).

---

## 4. Der Entwurf

### 4.1 Der Ablauf in Schritten

| # | Schritt | Wer | Meldungen / Abbruch |
|---|---|---|---|
| 1 | **Datei wählen** — `Dienste.Datei.DateiOeffnenAsync(titel, "IFC (*.ifc;*.ifcxml;*.ifczip)\|*.ifc;*.ifcxml;*.ifczip", "")` | Hülle (`EPOS.UI.Daten`) | `""` = abgebrochen, nichts geschieht |
| 2 | **Größe prüfen** — `new FileInfo(pfad).Length` gegen `IfcImportProfil.MaxBytes` (Vorschlag 50 MB) | Ablauf | `IMP_IFC_PROT_ZU_GROSS` (Fehler), Lauf endet |
| 3 | **Öffnen** — `MemoryModel.OpenRead(pfad, melder)` bzw. `OpenReadStep21(stream, …)` | Ablauf, im Arbeitsfaden | `IMP_IFC_PROT_LESEFEHLER` mit `ex.Message` |
| 4 | **Schema erkennen** — `MemoryModel.GetSchemaVersion(pfad)` → `XbimSchemaVersion` | Ablauf | `Unsupported`/`Cobie2X4` → `IMP_IFC_PROT_SCHEMA_UNBEKANNT` |
| 5 | **Einheiten lesen** — `IIfcProject.UnitsInContext.Units` | Ablauf | fehlende Längeneinheit → Warnung, Annahme Meter |
| 6 | **Gebäude / Geschosse / Räume** — `IIfcBuilding`, `IIfcBuildingStorey`, `IIfcSpace` | Ablauf | 0 Gebäude → Fehler; 0 Räume → Warnung (Wohnfläche bleibt leer) |
| 7 | **Bauteile sammeln und gruppieren** — Wände, Fenster, Platten, Dächer; außen/innen entscheiden | Ablauf | je verworfenem Bauteil eine Info mit Grund |
| 8 | **U·A-Zeilen bilden** (E2) — je Gruppe A = Σ Bruttoflächen, U = flächengewichtet | Ablauf | U außerhalb 0,1…6 W/(m²K) → Warnung, Wert bleibt, Herkunft `IFC` |
| 9 | **Fenster in Sektoren** N/O/S/W über Azimut (Sektormitte, Breite 90°) | Ablauf | Fenster ohne Azimut → Sammelposten, Warnung |
| 10 | **Vorgaben füllen** je Baualtersklasse | Ablauf | jede Vorgabe trägt Herkunft `Vorgabe` |
| 11 | **Zuordnungssatz** `IfcImportSatz` bilden: Zielfeld, Wert, Herkunft, Beleg | Ablauf | — |
| 12 | **Dialog** `IfcZuordnungDialog.razor`: Tabelle, Haken je Zeile, OK/Abbrechen | Komponente | Abbrechen → `null`, **nichts wird geschrieben** |
| 13 | **Plausibilität** (Konzept 4.8) auf dem übernommenen Satz | Ablauf | benannte Fehler, Rückkehr in den Dialog |
| 14 | **Schreiben** über den vorhandenen `Speichern`-Delegaten des Katalogeditors | Hülle | `IfcImportBilanz` mit den Zählern |

Die Schritte 2–11 laufen in **einem** Aufruf (`Lesen`), die Schritte 13–14 in einem zweiten
(`Uebernehmen`) — dieselbe Zäsur wie `Vorpruefen`/`Ausfuehren` beim Katalogimport.

### 4.2 Die Klassen

Ort: `EPOS.Kern/Allgemein/Import/Ifc/`. Namensraum wie überall `WindowsFormsApplication1`.

```csharp
// IfcSchema.cs — die drei Schemata, die der Leser bedient
public enum IfcSchemaStand { Unbekannt, Ifc2x3, Ifc4, Ifc4x3 }

// IfcHerkunft.cs — Wert, kein Anzeigetext (Regel ImportKonfliktModell.cs:44-46)
public enum IfcHerkunft { Leer, Ifc, Vorgabe, Manuell }

// IfcBauteilart.cs
public enum IfcBauteilart { Aussenwand, Fenster, Dach, Bodenplatte, Innenwand, Decke, Sonstiges }

// IfcHimmelsrichtung.cs
public enum IfcSektor { Nord, Ost, Sued, West, Ohne }
```

```csharp
// IfcGebaeudeAbbild.cs — das Zwischenmodell: was in der Datei steht, noch ohne EPOS-Semantik
public sealed class IfcBauteilAbbild
{
    public string Kennung { get; set; }            // IfcGloballyUniqueId (GlobalId)
    public string Name { get; set; }
    public IfcBauteilart Art { get; set; }
    public double FlaecheBruttoM2 { get; set; }    // GrossSideArea / Area / GrossArea, Bruttomass aussen
    public double? AzimutGrad { get; set; }        // 0 = Nord, im Uhrzeigersinn; null = unbekannt
    public double? NeigungGrad { get; set; }       // 90 = senkrecht, 0 = waagerecht
    public double? UWert { get; set; }             // W/(m²K), aus Pset
    public double? GWert { get; set; }             // nur Fenster, SolarHeatGainTransmittance
    public bool?   IstAussen { get; set; }         // Pset .IsExternal, null = unbekannt
    public string  Geschoss { get; set; }          // IfcBuildingStorey.Name
    public IReadOnlyList<IfcSchichtAbbild> Schichten { get; set; }  // innen -> aussen
    public string  Herkunftsbeleg { get; set; }    // "IfcWall #1234 / Qto_WallBaseQuantities.GrossSideArea"
}

public sealed class IfcSchichtAbbild
{
    public string Baustoff { get; set; }
    public double DickeM { get; set; }
    public double? LambdaWmK { get; set; }         // Pset_MaterialThermal.ThermalConductivity
    public double? RhoKgM3 { get; set; }           // Pset_MaterialCommon.MassDensity
    public double? CpJkgK { get; set; }            // Pset_MaterialThermal.SpecificHeatCapacity
}

public sealed class IfcRaumAbbild
{
    public string Kennung { get; set; }
    public string Name { get; set; }
    public string Geschoss { get; set; }
    public double? NettoflaecheM2 { get; set; }    // Qto_SpaceBaseQuantities.NetFloorArea
    public double? HoeheM { get; set; }            // .Height
    public double? NettovolumenM3 { get; set; }    // .NetVolume
    public bool    IstBeheizt { get; set; }        // Vorgabe true; Regel siehe 4.4
}

public sealed class IfcGebaeudeAbbild
{
    public string Kennung { get; set; }            // IfcBuilding.GlobalId
    public string Name { get; set; }
    public string BaujahrText { get; set; }        // Pset_BuildingCommon.YearOfConstruction (IfcLabel!)
    public double? TrueNorthGrad { get; set; }     // aus IIfcGeometricRepresentationContext.TrueNorth
    public bool    MapConversionVorhanden { get; set; }
    public double  LaengenFaktorNachMeter { get; set; } = 1.0;
    public double  FlaechenFaktorNachM2 { get; set; } = 1.0;
    public IReadOnlyList<IfcRaumAbbild> Raeume { get; set; }
    public IReadOnlyList<IfcBauteilAbbild> Bauteile { get; set; }
}
```

```csharp
// IfcImportProfil.cs — was den Lauf einstellt (Muster KatalogImportProfil.cs:196)
public sealed class IfcImportProfil
{
    public static IfcImportProfil Vorgabe(Func<string,string> text = null);
    public string Dateifilter { get; }             // "(*.ifc;*.ifcxml;*.ifczip)|*.ifc;*.ifcxml;*.ifczip"
    public long   MaxBytes { get; }                // 50 * 1024 * 1024
    public double SektorBreiteGrad { get; }        // 90
    public double UWertMin { get; }                // 0,1
    public double UWertMax { get; }                // 6,0
    public string HilfeSchluessel { get; }
}
```

```csharp
// IfcImportSatz.cs — je ZIELFELD eine Zeile
public sealed class IfcFeldzeile
{
    public string Zielfeld { get; }                // "UWertAussenwand", "FlaecheAussenwand", ...
    public string Gruppe { get; }                  // "Aussenwand", "Fenster", "Dach", ...
    public double? Wert { get; set; }
    public string  Einheit { get; }                // "W/(m²K)", "m²", "m", "1/h"
    public IfcHerkunft Herkunft { get; set; }
    public string  Beleg { get; }                  // Entität, Pset/Qto, Zahl der Bauteile
    public bool    Uebernehmen { get; set; } = true;
}

public sealed class IfcImportSatz
{
    public IfcSchemaStand Schema { get; }
    public string GebaeudeName { get; }
    public int? Baujahr { get; }
    public int  BaualtersklassenIndex { get; }     // 0..20, GebaeudeStammCtrl.BAUALTERSKLASSEN_DE
    public IReadOnlyList<IfcFeldzeile> Zeilen { get; }
    public IReadOnlyList<PruefMeldung> Meldungen { get; }
    /// <summary>Bildet die uebernommenen Zeilen auf den Feldsatz des Katalogeditors ab.</summary>
    public GebaeudeKatalogDaten NachKatalogdaten(GebaeudeKatalogDaten grundlage);
}
```

```csharp
// IfcImportAblauf.cs — Lesen, Zuordnen, Uebernehmen (Muster KatalogImportAblauf.cs:81)
public sealed class IfcImportAblauf
{
    public IfcImportAblauf(IfcImportProfil profil);
    public IfcImportProfil Profil { get; }
    public IReadOnlyList<IfcGebaeudeAbbild> Gebaeude { get; }
    public IReadOnlyList<PruefMeldung> Meldungen { get; }

    /// <summary>Liest die Datei; liefert die Zahl der gefundenen IfcBuilding. 0 = nichts.</summary>
    public int Lesen(string pfad, IProgress<ImportFortschritt> melder = null,
                     CancellationToken abbruch = default);

    /// <summary>Bildet den Zuordnungssatz eines gelesenen Gebaeudes (Index aus <see cref="Gebaeude"/>).</summary>
    public IfcImportSatz Zuordnen(int gebaeudeIndex, int baualtersklasseVorgabe = -1);

    /// <summary>Prueft den Satz nach Konzept 4.8; leere Liste = in Ordnung.</summary>
    public static IReadOnlyList<PruefMeldung> Pruefen(IfcImportSatz satz);
}
```

```csharp
// IfcZuordnungsModell.cs — die Regeln des Dialogs, oberflaechenfrei (Muster ImportKonfliktModell.cs:45)
public static class IfcZuordnungsModell
{
    public static string HerkunftText(IfcHerkunft h);
    public static string KopfText(int zeilen, int ausIfc, int ausVorgabe, int leer);
    public static string ZeilenText(IfcFeldzeile z);
    public static IReadOnlyList<PruefMeldung> Pruefe(IReadOnlyList<IfcFeldzeile> zeilen);
}
```

**Der Dialog.** `EPOS.UI/Dialoge/Bedarf/IfcZuordnungDialog.razor` mit
`EPOS.UI/Dialoge/Bedarf/IfcZuordnungDaten.cs`:

```csharp
// IfcZuordnungDaten.cs — das DTO der Komponente; kein Kerntyp, der schreiben koennte
public sealed class IfcZuordnungZeile
{
    public string Gruppe { get; set; } = "";
    public string Feld { get; set; } = "";
    public string IfcWert { get; set; } = "";      // formatiert, mit Einheit
    public string Beleg { get; set; } = "";
    public string VorgabeWert { get; set; } = "";
    public string Herkunft { get; set; } = "";     // Anzeigetext aus IfcZuordnungsModell
    public bool   Uebernehmen { get; set; } = true;
}

public sealed class IfcZuordnungDaten
{
    public string Dateiname { get; set; } = "";
    public string Schema { get; set; } = "";
    public string GebaeudeName { get; set; } = "";
    public IReadOnlyList<string> Gebaeudewahl { get; set; } = Array.Empty<string>();
    public int GewaehltesGebaeude { get; set; }
    public List<IfcZuordnungZeile> Zeilen { get; set; } = new();
    public List<string> Protokoll { get; set; } = new();
}
```

Parameter der Komponente (Muster `KatalogImportDialog.razor:241-260`):

```csharp
[Parameter] public Func<Task<string>>? DateiWaehlen { get; set; }
[Parameter] public Func<string, IProgress<ImportFortschritt>, CancellationToken,
                        Task<IfcZuordnungDaten>>? Lesen { get; set; }
[Parameter] public Func<int, Task<IfcZuordnungDaten>>? GebaeudeWechseln { get; set; }
[Parameter] public Func<IReadOnlyList<IfcZuordnungZeile>, Task<IfcUebernahmeErgebnis>>? Uebernehmen { get; set; }
[Parameter] public Func<PruefMeldung, string>? Meldungstext { get; set; }
[Parameter] public Func<ImportFortschritt, string>? Fortschrittstext { get; set; }
[Parameter] public EventCallback<bool> Geschlossen { get; set; }   // false = Abbrechen
```

**Die Hülle liegt plattformfrei** in `EPOS.UI.Daten/Bedarf/IfcImportHuelle.cs` (Muster
`SpotpreisImportHuelle.cs:32`), nicht in der Windows-Schale — so bekommt iOS sie ohne zweite
Fassung. Der Arbeitsfaden läuft dort über `Kulturweitergabe.Starten` (`Kulturweitergabe.cs:170`),
nicht `Task.Run` (Wächter, siehe 1.3).

### 4.3 Abbildungsregeln

Alle Typnamen sind Schnittstellen aus `Xbim.Ifc4.Interfaces`; sie werden von `Xbim.Ifc2x3` und
`Xbim.Ifc4x3` mitbedient (je ein Ordner `Interfaces/IFC4`). Der Zugriff auf Psets und Quantities
wird **selbst geschrieben** über `IIfcObject.IsDefinedBy` → `IIfcRelDefinesByProperties.RelatingPropertyDefinition`
→ `IIfcPropertySet.HasProperties` / `IIfcElementQuantity.Quantities`; die bequemen
`GetPropertySingleValue`/`GetElementQuantity` hängen an der IFC4-**Klasse** `Xbim.Ifc4.Kernel.IfcObject`
und wären schemagebunden.

| Zielfeld (`GebaeudeKatalogDaten`) | IFC-Quelle | Regel | Rückfall | Herkunft |
|---|---|---|---|---|
| `WohnflaecheGesamt` | `IIfcSpace` + `Qto_SpaceBaseQuantities.NetFloorArea` | Σ über beheizte Räume des Gebäudes | `GrossFloorArea`; sonst leer → **Pflichtfeld**, Anwender trägt ein | IFC / leer |
| `Raumhoehe` | `Qto_SpaceBaseQuantities.Height` | flächengewichtetes Mittel über die Räume | `NetVolume / NetFloorArea`; sonst 2,5 m | IFC / Vorgabe |
| (Volumen, nur Prüfgröße) | `Qto_SpaceBaseQuantities.NetVolume` | Σ; gegen `Wohnfläche · Raumhöhe` halten, Abweichung > 20 % → Warnung | — | — |
| `FlaecheAussenwand` | `IIfcWall` + `Qto_WallBaseQuantities.GrossSideArea` | Σ über Wände mit `IsExternal = true`; **`GrossSideArea`, nicht `NetSideArea`** — Bruttomaß außen (Konzept N1.9, Bemaßungsregel; als Arbeitsannahme geführt, Normzitat in G1 zu belegen). Fensterfläche wird **nicht** abgezogen, weil das Modell Fenster zusätzlich führt — der Doppelzählung begegnet Schritt „Fensterabzug" (4.4) | `Length × Height` aus demselben Qto; sonst leer | IFC / leer |
| `gesamte_Fensterflaeche` (abgeleitet) | `IIfcWindow` + `Qto_WindowBaseQuantities.Area` | Σ über Fenster in Außenwänden | `Width × Height` | IFC |
| `FensterflaecheNord/Sued`, `FensterflaecheOst`, `FensterflaecheWest` | Azimut des **Wirtsbauteils** (Wand) | Sektor = Sektor mit der nächsten Mitte; N = 0°, O = 90°, S = 180°, W = 270°, Breite 90° | Fenster ohne Azimut: gleichmäßig auf die vier Sektoren, Warnung | IFC / Vorgabe |
| `Dachflaeche` | `IIfcRoof`, `IIfcSlab` mit `PredefinedType = ROOF` + `Qto_SlabBaseQuantities.GrossArea` | Σ | Grundfläche des obersten Geschosses | IFC / Vorgabe |
| `Grundflaeche` | `IIfcSlab` mit `PredefinedType = BASESLAB`/`FLOOR` im untersten Geschoss + `Qto_SlabBaseQuantities.GrossArea` | Σ | Wohnfläche / Geschosszahl | IFC / Vorgabe |
| `SonstigeFlaechen` | `IIfcDoor` außen, `IIfcCurtainWall`, `IIfcPlate` außen | Σ | 0 | IFC / Vorgabe |
| `UWertAussenwand` u. a. (5 Kategorien) | `Pset_WallCommon.ThermalTransmittance` usw. (`IIfcPropertySingleValue.NominalValue`) | **flächengewichtet je Gruppe:** `U = Σ(Uᵢ·Aᵢ) / ΣAᵢ`, nur über Bauteile mit U-Wert; fehlt er bei > 30 % der Fläche der Gruppe, gilt die Gruppe als „nicht aus IFC" | Vorgabe je Baualtersklasse | IFC / Vorgabe |
| `Fensterdurchlassgrad` | `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` | flächengewichtet | Vorgabe je Baualtersklasse (0,75 alt / 0,6 / 0,5 neu) | IFC / Vorgabe |
| `Bauweise` | `IIfcMaterialLayerSet` über `IIfcRelAssociatesMaterial` + `Pset_MaterialThermal`/`Pset_MaterialCommon` | Raumseitige Schichten bis 10 cm Tiefe: `C" = Σ ρᵢ·cpᵢ·dᵢ` in J/(m²K), `/3600` → Wh/(m²K); dann `Bauweise = C"·Wohnfläche`. Die Anzeige rastet über `Gebaeudebauweise.BauartAusBauweise` (`Gebaeudebauweise.cs:42`) auf leicht/schwer/sehr schwer (20/50/100 Wh/(m²K), `:61-66`) ein | ohne Schichten: Vorgabe „schwer" = Wohnfläche × 50 | IFC / Vorgabe |
| `Baualtersklasse` | `Pset_BuildingCommon.YearOfConstruction` (**`IfcLabel`, also Text**) | erste vierstellige Zahl 1500…2100 aus dem Text ziehen („ca. 1965" → 1965), dann auf A…H abbilden: **A** vor 1919, **B** 1919–1948, **C** 1949–1957, **D** 1958–1968, **E** 1969–1978, **F** 1979–1983, **G** 1984–1994, **H** 1995–2000 (`GebaeudeStammCtrl.cs:112-119`). Ab 2001 ist die Klasse ein **Standard**, kein Jahr (I = Niedrigenergie … U = BEG 40) — dann bleibt das Feld auf Vorgabe und der Anwender wählt | ohne Jahr: Klasse bleibt, was der Grundlagensatz trägt | IFC / manuell |
| `Baujahr` (neue Spalte) | dieselbe Quelle | die gezogene Jahreszahl, `INTEGER`, `NULL` = unbekannt | — | IFC |
| `Grundflaeche_Randbedingung` (G1-Spalte) | Geschoss unter der Bodenplatte bzw. `IfcSpace` darunter | Liegt unter der Platte ein `IIfcBuildingStorey` mit `Elevation < 0` **oder** ein `IIfcSpace`, dessen Name auf Keller/Untergeschoss deutet → `KELLER`; sonst `ERDREICH` | `ERDREICH` (Vorgabe nach Konzept 6.1) | IFC / Vorgabe |
| `WbvkFensterWand`, `WbvkWandDach`, `WbvkAussenwandKeller` und die drei Anschlussmaße | **nicht in IFC** | ψ-Werte stehen in keinem Standard-Pset; Anschlusslängen ohne Geometriekernel nicht ableitbar | Vorgabe je Baualtersklasse; die **Längen** als Näherung aus Umfang und Geschosszahl **nur, wenn der Anwender es verlangt** — sonst leer | Vorgabe / leer |
| `Luftwechselrate` | `Pset_SpaceThermalLoad.AirExchangeRate` | **nicht benutzen** — das Feld ist im Schema als `IfcPowerMeasure` typisiert (Schemafehler, IFC 4.3.2 Abschn. 6.2.4.25); der Zahlenwert ist nicht verlässlich zu deuten | Vorgabe 0,7 1/h (bzw. G2: 0,3 + 0,4) | Vorgabe |
| `Waermegewinne` (innere Gewinne, W) | `Pset_SpaceOccupancyRequirements.OccupancyNumber` / `AreaPerOccupant` | nur als **Vorschlag** zur Anzeige, nicht übernommen | Vorgabe je Gebäudeart (Wohngebäude: 5 W/m²) | Vorgabe |
| `SollTag`, `NachtAbsenkung`, `MaxTemperatur` | `Pset_SpaceThermalRequirements` (in IFC 4.3 entfallen) | nur lesen, wenn vorhanden; sonst nicht | Vorgaben des Grundlagensatzes | IFC / Vorgabe |

**Einheiten.** Vor jeder Zahl steht der Faktor aus `IIfcProject.UnitsInContext`
(`IIfcUnitAssignment.Units`): für `IIfcSIUnit` mit `UnitType = LENGTHUNIT` entscheidet
`Prefix` (`IfcSIPrefix.MILLI` → 0,001, `CENTI` → 0,01, kein Prefix → 1,0); `AREAUNIT` und
`VOLUMEUNIT` bekommen das Quadrat bzw. die dritte Potenz desselben Faktors, es sei denn, sie sind
eigenständig als SI-Einheit mit eigenem Prefix erklärt. `IIfcConversionBasedUnit` (Zoll, Fuß) trägt
den Faktor in `ConversionFactor` — im DACH-Raum selten, aber der Leser meldet ihn benannt statt ihn
stillschweigend als 1,0 zu nehmen.

**Azimut ohne Geometriekernel.** Die Kette ist reine Matrixmultiplikation:
`IIfcProduct.ObjectPlacement` → `IIfcLocalPlacement.RelativePlacement`
(`IIfcAxis2Placement3D.RefDirection`, `Axis`) → über `PlacementRelTo` aufwärts bis zum Weltsystem.
Die lokale x-Achse der Wand liegt nach Spezifikation in der Wandachse (bei
`IIfcMaterialLayerSetUsage` ist die `'Axis'`-Repräsentation zwingend); die Außennormale ist die
dazu senkrechte Richtung in der xy-Ebene. Zum Schluss dreht `TrueNorth` aus
`IIfcGeometricRepresentationContext.TrueNorth` (`IIfcDirection`, Vorgabe `[0,1]`) das Ergebnis —
**außer** wenn der Kontext eine `IIfcMapConversion` trägt (`HasCoordinateOperation`), dann ist
`TrueNorth` laut Spezifikation nur informativ und wird **nicht** addiert.

### 4.4 Sonderfälle

1. **Mehrere `IfcBuilding`.** Ein EPOS-Gebäude je `IfcBuilding`. Der Dialog zeigt eine Klappliste
   (`IfcZuordnungDaten.Gebaeudewahl`); übernommen wird je Lauf **eines**. Räume und Bauteile werden
   über `IIfcRelAggregates` bzw. `IIfcRelContainedInSpatialStructure` dem Gebäude zugeordnet; was
   sich keinem zuordnen lässt, kommt in einen Sammelposten mit Warnung.
2. **Geschosse.** `IIfcBuildingStorey` liefert nur die Summenbildung und die Reihenfolge über
   `Elevation`. Drei Höhenbezüge sind optional (`IIfcSite.RefElevation`,
   `IIfcBuilding.ElevationOfRefHeight`, `IIfcBuildingStorey.Elevation`) — der Leser nimmt
   ausschließlich `Elevation` **relativ**, also die Sortierung, nie einen absoluten Wert.
3. **Unbeheizte Räume.** `Pset_SpaceCommon.IsExternal = true` schließt aus. Sonst entscheidet der
   Name: Treffer auf `Keller|Garage|Carport|Dachboden|Speicher|Abstellraum|Technik|Schacht|Aufzug`
   (Groß-/Kleinschreibung egal, auch englisch `Basement|Garage|Attic|Shaft|Plant`) → unbeheizt.
   **Der Dialog zeigt die Raumliste mit dem Haken**, damit die Regel sichtbar und korrigierbar ist.
   `Pset_SpaceThermalRequirements` wird genommen, wenn vorhanden — es ist in IFC 4.3 entfallen.
4. **Archicad-Raumgrenzen.** Die Dateien schreiben trotz IFC4 die Basisklasse `IfcRelSpaceBoundary`
   und tragen das Merkmal nur in `Name='2ndLevel'` / `Description='2a'`. Der Leser prüft deshalb
   **beides**: `is IIfcRelSpaceBoundary2ndLevel` **oder** `Name`/`Description` enthält `2nd`.
   *Für Stufe G4a werden Raumgrenzen ohnehin nur für eine Hilfsentscheidung gebraucht* —
   `IsExternal` fehlt oft, und der Rückfall lautet: eine Wand ist außen, wenn **genau eine**
   `IIfcRelSpaceBoundary` mit `PhysicalOrVirtualBoundary = PHYSICAL` auf sie zeigt.
5. **Fehlende Quantities.** Kein `Qto_*` → Rückfall `Length × Height` aus demselben Qto; fehlt auch
   das, bliebe nur `IfcExtrudedAreaSolid`. **Bewertung der Machbarkeit ohne Geometriekernel:** Für
   eine prismatische Wand ist es rechenbar — `SweptArea` ist ein `IIfcRectangleProfileDef` oder
   `IIfcArbitraryClosedProfileDef` mit `IIfcPolyline`, `Depth` ist die Extrusionslänge; Fläche =
   Profilbreite × `Depth`, mit Polygonfläche über die Trapezformel. Es scheitert an nicht
   prismatischen Wänden, an `IfcBooleanClippingResult` (Giebelwände!) und an `IfcMappedItem`
   (wiederverwendete Geometrie mit eigener Transformation) — und genau diese drei kommen in
   Bestandsmodellen regelmäßig vor. **Daher: in G4a nicht bauen.** Der Leser meldet
   `IMP_IFC_PROT_KEINE_MENGEN` und lässt das Feld leer; die Geometrieableitung bleibt G5
   (Konzept 11).
6. **Gedrehte Gebäude.** Siehe 4.3, Absatz „Azimut". Zusätzlich: Ist `TrueNorth` nicht gesetzt,
   gilt `[0,1]` (Norden = +y) und der Dialog **sagt das**, weil ein falsch genordetes Modell die
   Fenstersektoren vertauscht und niemand es an den Zahlen sieht.
7. **Einheiten.** Siehe 4.3. Merkposten: `IIfcSite.RefLatitude/RefLongitude` sind
   `IfcCompoundPlaneAngleMeasure` = `LIST [3:4] OF INTEGER` (Grad, Minuten, Sekunden, optional
   Millionstel-Sekunden, alle mit gleichem Vorzeichen) und **nicht** von `IfcUnitAssignment`
   betroffen. In G4a wird der Ort **nicht** übernommen — die Klimaregion wählt der Anwender im
   Projekt; er könnte später einen Vorschlag speisen.
8. **IFC2x3 gegen IFC4.** 2x3 kennt `IfcRelSpaceBoundary1stLevel`/`2ndLevel` nicht und hat für
   `RelatedBuildingElement` keine Pflicht. `Pset_DoorWindowGlazingType` gibt es in 2x3, die
   Quantity-Sets ebenfalls. Der Leser arbeitet ausschließlich über die `IIfc*`-Schnittstellen und
   verzweigt nur an zwei Stellen: 2nd-Level-Erkennung (siehe 4) und das in 4.3 entfallene
   `Pset_SpaceThermalRequirements`.
9. **`IfcZone` ist zu entschachteln.** Das Schema erlaubt in `RelatedObjects` auch `IfcZone` und
   `IfcSpatialZone`; wer nicht entschachtelt, zählt Flächen doppelt. In G4a werden Zonen gar nicht
   gelesen — die Räume hängen über `IIfcRelContainedInSpatialStructure` bzw. `IIfcRelAggregates` am
   Geschoss, und das genügt für eine Einzonenrechnung.
10. **Mehrschalige Wände als mehrere Elemente.** Zwei Wände mit derselben Achslage und derselben
    Raumgrenze sind **eine** Wand. In G4a ohne Achsauswertung: Gruppierung über die gemeinsame
    Raumgrenze; wo das nicht trägt, warnt der Leser (`IMP_IFC_PROT_MEHRSCHALIG`), weil sonst die
    Außenwandfläche doppelt zählt.
11. **Fensterabzug.** `GrossSideArea` enthält die Öffnungen. Das EPOS-Modell führt Wand und Fenster
    **getrennt** mit je eigenem U-Wert; die Wandfläche muss also um die Fensterfläche **vermindert**
    werden, sonst zählt die Öffnung zweimal. Regel: `A_Wand = Σ GrossSideArea − Σ A_Fenster − Σ A_Aussentuer`,
    und wird das negativ, greift `NetSideArea`, wenn vorhanden, sonst Warnung und `A_Wand = 0`.
    *Das ist der Punkt, an dem sich der Bruttomaß-Grundsatz (Konzept N1.9) und die
    EPOS-Feldstruktur kreuzen — er gehört in die Abnahme.*

### 4.5 Plattform, Fehlerbilder, Protokoll

**Windows.** `IfcImportHuelle.Gaben(idGebaeude)` in `EPOS.UI.Daten/Bedarf/`, gerufen aus
`GebaeudeKatalogHuelle` als zusätzliche Gabe; der Dialog erscheint als Überlagerung. Dateiwahl über
`Dienste.Datei.DateiOeffnenAsync`, Arbeitsfaden über `Kulturweitergabe.Starten`.

**iOS.** Dieselbe Hülle, derselbe Dialog. Drei Punkte sind iOS-eigen:

- **Dateifilter:** `.ifc` in `EPOS.iOS/Dienste/Dateifilter.cs:25-44` ergänzen (auf `public.data`,
  mit Kommentar wie beim `.lic`-Fall `:37-40`).
- **Speicher:** `MemoryModel` hält das ganze Modell im Arbeitsspeicher. Die KIT-Datei
  `AC20-FZK-Haus.ifc` misst 2,5 MB; als Faustzahl liegt der Speicherbedarf bei 10–20 MB je MB
  STEP-Text. 50 MB Datei sind damit 0,5–1 GB — auf einem iPad zu viel. **Vorschlag: MaxBytes auf
  20 MB für iOS, 50 MB für Windows**, gesetzt über eine Eigenschaft von `IfcImportProfil`, die die
  Hülle je Plattform belegt (Muster: das plattformbedingte Ablehnen bei `Katalogwege`/
  `SimulationPlattformwege`). Die Grenze gehört gemessen, nicht geschätzt — ein Punkt für G4.
- **Trimming:** siehe 2.3.

**Fehlerbilder** (alle als `PruefMeldung`, Schlüssel je in beiden `.resx`, danach
`Werkzeuge/ResourceDesigner`):

| Schlüssel | Stufe | Deutsch | Englisch |
|---|---|---|---|
| `IMP_IFC_PROT_ZU_GROSS` | Fehler | „Die Datei ist {0} MB groß; verarbeitet werden höchstens {1} MB." | „The file is {0} MB; at most {1} MB can be processed." |
| `IMP_IFC_PROT_LESEFEHLER` | Fehler | „Die IFC-Datei konnte nicht gelesen werden: {0}" | „The IFC file could not be read: {0}" |
| `IMP_IFC_PROT_SCHEMA_UNBEKANNT` | Fehler | „Das Schema ‚{0}' wird nicht unterstützt; gelesen werden IFC2x3, IFC4 und IFC4x3." | „Schema '{0}' is not supported; IFC2x3, IFC4 and IFC4x3 are read." |
| `IMP_IFC_PROT_KEIN_GEBAEUDE` | Fehler | „Die Datei enthält kein Gebäude (IfcBuilding)." | „The file contains no building (IfcBuilding)." |
| `IMP_IFC_PROT_KEINE_RAEUME` | Warnung | „Kein Raum mit Mengenangaben gefunden; Wohnfläche und Raumhöhe bleiben leer." | „No space with quantities found; floor area and room height remain empty." |
| `IMP_IFC_PROT_KEINE_MENGEN` | Warnung | „{0} Bauteile ohne Mengenangaben (Qto_…); ihre Flächen fehlen." | „{0} elements without base quantities (Qto_…); their areas are missing." |
| `IMP_IFC_PROT_KEIN_UWERT` | Warnung | „Gruppe {0}: für {1} % der Fläche fehlt der U-Wert; es gilt die Vorgabe." | „Group {0}: the U-value is missing for {1} % of the area; the default applies." |
| `IMP_IFC_PROT_KEIN_NORDEN` | Warnung | „Die Datei nennt keine Nordrichtung; angenommen wird die Schemavorgabe (+y = Nord)." | „The file states no true north; the schema default (+y = north) is assumed." |
| `IMP_IFC_PROT_MAPCONVERSION` | Info | „Die Datei trägt eine Koordinatenumrechnung; die Nordrichtung ist dort nur informativ." | „The file carries a map conversion; true north is informative there." |
| `IMP_IFC_PROT_EINHEIT` | Warnung | „Unerwartete Einheit ‚{0}'; gerechnet wird mit {1}." | „Unexpected unit '{0}'; {1} is used." |
| `IMP_IFC_PROT_MEHRSCHALIG` | Warnung | „{0} Wände konnten nicht zu einem Bauteil zusammengefasst werden; die Außenwandfläche kann zu groß sein." | „{0} walls could not be merged; the external wall area may be too large." |
| `IMP_IFC_PROT_BAUJAHR_TEXT` | Info | „Baujahr aus ‚{0}' gelesen: {1}." | „Year of construction read from '{0}': {1}." |
| `IMP_IFC_PROT_GELESEN` | Info | „{0} Gebäude, {1} Räume, {2} Bauteile gelesen." | „{0} buildings, {1} spaces, {2} elements read." |

**Das Protokoll des Imports** ist die Meldungsliste in Reihenfolge, wie beim Ganglinienimport
(`GanglinienImportErgebnis.Protokoll`, `GanglinienImportAblauf.cs:51`). Es erscheint im Dialog
unter der Tabelle (Baustein `Warnbanner` für die schwerste Stufe, ausklappbare Liste darunter) und
wird **nicht** in die Datenbank geschrieben. Je übernommener Zeile trägt der Satz seinen `Beleg`
(Entität und Pset/Qto) — das ist die Spur, mit der ein Anwender eine Zahl zurückverfolgt.

### 4.6 Tests und Abnahme

**Wie Importproben heute organisiert sind.** `Referenzlaeufe/Importproben/` (28 Dateien, 304 KB,
gewöhnliche Blobs, **kein** LFS — `Konzept_Repository_Aufraeumen_EPOS-Plan.md:132`). Der Ordner
„gehört zum Testbestand und wird nie gelöscht" (`Referenzlaeufe/LIESMICH.md:128-129`). Es gibt
**keine** LIESMICH im Ordner selbst; die Quellenvermerke stehen in den Fachkonzepten
(`Konzept_Stromspeicherimport_EPOS-Plan.md:667-668`) bzw. als `LIESMICH_*.md` neben den
ausgelieferten Daten. Tests suchen den Ordner **aufwärts vom Laufordner**
(`EPOS.Kern.Tests/KatalogImportTests.cs:50-68`, gleichlautend `KatalogImportAblaufTests.cs:36-44`,
`BedarfVerwaltungTests.cs:685-693`); fehlt er, schlägt der Test fehl bzw. schweigt.

**Vorschlag für G4:**

1. `Referenzlaeufe/Importproben/AC20-FZK-Haus.ifc` (KIT/IAI, 2,5 MB, IFC4/Archicad 20) aufnehmen —
   der Ordner wächst damit um den Faktor 9, bleibt aber unter 3 MB und braucht kein LFS.
2. **Neu:** `Referenzlaeufe/Importproben/LIESMICH_Importproben.md` mit den Quellenvermerken aller
   Proben. Für die KIT-Datei im vorgegebenen Wortlaut: „Institut für Automation und angewandte
   Informatik (IAI) / Karlsruher Institut für Technologie (KIT)"; Nutzung uneingeschränkt, die
   Namensnennung ist für Veröffentlichungen vorgeschrieben (Gegenlesen IFC, Befund 13).
3. **RWTH- und bim2sim-Dateien nicht aufnehmen** (Konzept Q12, Empfehlung).

**Importprobe — erwartete Werte** (aus Befund C, Abschnitt 6 und Konzept 7.8; die Zahlen sind beim
Bau des Tests an der Datei nachzumessen, hier stehen die belegten):

| Prüfung | Erwartung |
|---|---|
| Schema erkannt | `XbimSchemaVersion.Ifc4` → `IfcSchemaStand.Ifc4` |
| Gebäude | 1 × `IIfcBuilding` |
| Räume | **8** `IIfcSpace` |
| Raumgrenzen | **81**, sämtlich als **Basisklasse** mit `Name='2ndLevel'` / `Description='2a'` — der Test prüft ausdrücklich, dass der Leser sie **nicht** über den Entity-Typ sucht |
| U-Werte | **33** `ThermalTransmittance` in den Psets |
| `Pset_SpaceThermalRequirements` | 7 Vorkommen |
| Einheiten | Längeneinheit aufgelöst, Faktor nach Meter dokumentiert |
| Herkunft je Zeile | Wohnfläche, Raumhöhe, Außenwandfläche, U-Werte = `Ifc`; Wärmebrücken und Luftwechsel = `Vorgabe` |

**Unit-Tests ohne Datei** (`EPOS.Kern.Tests`, keine Testdatenbank, keine Sammlung nötig):

| Test | Inhalt |
|---|---|
| `Die_Sektorzuordnung_trifft_die_vier_Mitten` | 0°→N, 45°→N oder O (Grenze festgelegt: **aufsteigend zum größeren Sektor**), 90°→O, 180°→S, 270°→W, 359°→N; negative und > 360° Winkel werden normalisiert |
| `Der_Azimut_dreht_mit_TrueNorth` | Richtung `[1,0]` mit `TrueNorth = [0,1]` ergibt Ost; mit `TrueNorth = [1,0]` ergibt Süd |
| `Bei_MapConversion_wird_TrueNorth_nicht_addiert` | Gegenprobe |
| `Der_U_Wert_einer_Gruppe_ist_flaechengewichtet` | zwei Bauteile 10 m²/0,5 und 30 m²/1,5 ergeben 1,25 W/(m²K), nicht 1,0 |
| `Eine_Gruppe_ohne_genug_U_Werte_faellt_auf_die_Vorgabe` | Deckungsgrad 60 % < 70 % → Herkunft `Vorgabe` |
| `Das_Baujahr_wird_aus_Text_gelesen` | „1965", „ca. 1965", „erbaut 1965/66", „um 1965" → 1965; „Altbau" → keins |
| `Die_Baualtersklasse_folgt_dem_Baujahr` | 1918→A, 1919→B, 1948→B, 1949→C, 2000→H, 2005→unverändert |
| `Die_Bauweise_folgt_dem_Schichtaufbau` | 20 cm Beton (ρ 2300, cp 1000) raumseitig, bis 10 cm gezählt → 63,9 Wh/(m²K) → Bauart „sehr schwer" nach `Gebaeudebauweise.BauartAusBauweise` |
| `Die_Einheiten_werden_umgerechnet` | `IfcSIUnit` Länge mit `Prefix = MILLI` → Faktor 0,001; Fläche entsprechend 1e-6 |
| `Die_Fensterflaeche_wird_von_der_Wandflaeche_abgezogen` | 100 m² brutto, 15 m² Fenster, 2 m² Tür → 83 m²; bei Überzug → Warnung und 0 |
| `Die_Vorgaben_je_Baualtersklasse_sind_vollstaendig` | für jede der 21 Klassen ein U-Wert-Satz und ein g-Wert |
| `Das_Groessenlimit_greift` | Datei über `MaxBytes` → genau eine Meldung `IMP_IFC_PROT_ZU_GROSS`, Stufe Fehler, kein Öffnen |
| `Ein_Abbruch_wirft_OperationCanceled` | `CancellationToken` vor dem Öffnen gesetzt |

Dazu die Dialogtests in `EPOS.UI.Tests/Dialoge/IfcZuordnungDialogTests.cs` (bunit, Muster
`KatalogImportDialogTests`): Abbrechen liefert `false` und ruft `Uebernehmen` nie; ein abgehakter
Haken hält die Zeile aus dem Satz; die Herkunftstexte kommen aus `IfcZuordnungsModell`, nicht aus
der Komponente.

**Abnahme G4a:**

1. `dotnet test WP-Plan.Kern.slnf -c Release` grün, Importprobe bestanden.
2. **Referenzlauf unverändert** — der Import schreibt nur, wenn ein Anwender ihn ruft; kein
   Referenzprojekt ändert sich. Wird die neue Spalte `Baujahr` mit einem Schemaschritt eingeführt
   (der nächste freie ist **77+n**, `SchemaStand.Zielversion` steht auf 76,
   `EPOS.Kern/Allgemein/Update/SchemaStand.cs:93`; 77 und 78 sind für G1/G2 vergeben), ist der
   Schritt ergebnisneutral und die Basis bleibt.
3. Windows-Sichtabnahme: Datei wählen, Zuordnung prüfen, OK, Werte im Katalogeditor.
4. **iOS-Lauf nach Rückfrage beim Anwender** (Kontingent) mit Release-Bau, Trimming-Nachweis und
   der KIT-Datei im Prüfmodus.
5. `SqlDialektPruefer` nur, wenn ein Schemaschritt dazukommt.

### 4.7 Aufwand und Reihenfolge innerhalb G4

| Teil | Inhalt | Aufwand |
|---|---|---|
| **G4-1** | Paketzeile, `EPOS.Kern.csproj`, Lizenzhinweisseite im Setup, Bau auf ubuntu und Windows grün | 0,5–1 PT |
| **G4-2** | `IfcGebaeudeAbbild`, `IfcImportProfil`, Einheitenauflösung, Schemaerkennung, Öffnen, Räume und Bauteile sammeln — **ohne** Zuordnung; Test: Importprobe liest 8 Räume, 33 U-Werte | 3–4 PT |
| **G4-3** | Azimut aus der Placement-Kette, `TrueNorth`, `MapConversion`; Tests ohne Datei | 2–3 PT |
| **G4-4** | Zuordnung nach E2: U·A-Zeilen, Sektoren, Fensterabzug, Bauweise aus Schichten, Baujahr → Klasse; `IfcImportSatz`, `IfcZuordnungsModell`, Plausibilität | 3–4 PT |
| **G4-5** | Vorgabetabelle je Baualtersklasse (21 Klassen × 5 U-Werte + g), Quelle und Lizenz geklärt (siehe 5, Frage 3) | 1–2 PT |
| **G4-6** | `IfcZuordnungDialog.razor` + DTO + `IfcImportHuelle` in `EPOS.UI.Daten`, Knopf in beiden Gebäudedialogen, Texte in beiden `.resx`, `ResourceDesigner` | 2–3 PT |
| **G4-7** | Importprobe, Quellenvermerk, Unit-Tests, Dialogtests | 1–2 PT |
| **G4-8** | iOS: Dateifilter, Größenlimit gemessen, `TrimmerRootDescriptor`, Release-Bau und Lauf nach Rückfrage | 1–2 PT |
| | **Summe G4a** | **13–21 PT** |
| **G4b** | Anbindung an `Tab_Bauteil`/`Tab_Bauteilschicht` aus G3: je Bauteil eine Zeile mit Schichten, Azimut, Neigung, Herkunft `IFC` — statt Nachmultiplikation | +4–6 PT |

**Reihenfolge:** G4-1 → G4-2 → G4-3 → G4-4 → G4-5 → G4-6 → G4-7 → G4-8. G4-3 vor G4-4, weil die
Sektorzuordnung am Azimut hängt; G4-5 kann parallel laufen, sobald die Zielfelder stehen. G4b erst
nach G3 und erst, wenn G4a im Feld war.

**Vorbedingungen:** G3 muss **nicht** fertig sein (G4a schreibt in `Tab_Gebaeude`, nicht in
`Tab_Bauteil`). G1 und G2 müssen fertig sein — sonst importiert man in ein Tagesmodell, das die
Daten nicht nutzt (Konzept 7.9).

---

## 5. Offene Fragen für den Anwender

1. **Lizenzhinweise im Setup.** Es gibt heute keine Seite für Fremdbibliotheken (2.2). Soll sie mit
   G4 entstehen — und dann gleich für **alle** ausgelieferten Fremdanteile, nicht nur xBIM? Der
   CDDL-Verweis auf die xBIM-Quellen muss dem Empfänger mitgeteilt werden; ohne diese Seite ist der
   IFC-Import nicht auslieferbar.
2. **Größenlimit.** Vorschlag 50 MB Windows / 20 MB iOS (4.5). Die iOS-Zahl ist geschätzt und in
   G4-8 zu messen. Ist eine Datei, die größer ist, benannt abzulehnen — oder soll EPOS-Plan es
   versuchen und bei Speichermangel mit einer Meldung abbrechen?
3. **Woher die Vorgaben je Baualtersklasse?** Das Konzept nennt TABULA/IWU (Stein/Loga 2025,
   Zenodo); das Gegenlesen (Befund 12) hält fest, dass **Record und Datensatzlizenz fehlen** —
   ohne beides dürfen die Tabellenwerte nicht in ein verkauftes Produkt. Drei Wege: (a) DOI und
   Lizenz besorgen und die Bedingung im Setup nennen; (b) eigene Vorgabewerte aus dem
   EPOS-Gebäudekatalog ableiten (die Testdatenbank führt Gebäude je Klasse — das wäre lizenzfrei
   und hausgemacht); (c) nur die Klassen A–H vorbelegen und den Rest leer lassen. **Empfehlung:
   (b)**, weil es keine fremde Lizenz braucht und die Zahlen zu den übrigen EPOS-Vorgaben passen.
4. **Was passiert bei mehreren `IfcBuilding`?** Vorschlag: eine Klappliste, ein Gebäude je Lauf
   (4.4 Nr. 1). Alternative wäre „alle auf einmal anlegen" — das erzeugt Katalognamen automatisch
   und braucht dann die Dublettenlogik des Katalogimports. Soll das in G4a hinein?
5. **Bruttomaß und Fensterabzug.** Die Bemaßungsregel (Bruttomaß außen) und die EPOS-Feldstruktur
   (Wand und Fenster getrennt mit eigenem U-Wert) kreuzen sich; der Entwurf zieht die Fensterfläche
   von der Bruttowandfläche ab (4.4 Nr. 11). Ist das die gewünschte Lesart — oder soll die
   Wandfläche die Öffnungen enthalten, wie es `GrossSideArea` liefert?
6. **Wärmebrücken.** IFC liefert weder ψ noch Anschlusslängen (4.3). Sollen die drei ψ-Werte und
   die drei Längen als Vorgabe je Baualtersklasse gesetzt werden, oder bleiben sie leer und der
   Anwender trägt sie ein? (Der Bestand trägt sie in `GebaeudeKatalogDaten.cs:108-118`.)
7. **Die KIT-Datei im Repositorium.** 2,5 MB als gewöhnlicher Blob in
   `Referenzlaeufe/Importproben/`, Quellenvermerk in einer neuen `LIESMICH_Importproben.md` — in
   Ordnung? (Konzept Q12 empfiehlt ja.)
8. **iOS-Lauf.** Der Trimming-Nachweis braucht einen macOS-Lauf mit Release-Bau; der CI-Lauf baut
   heute Debug (`.github/workflows/ios.yml:125`). Soll der Workflow um einen Release-Zweig ergänzt
   werden, oder bleibt es bei einem einmaligen Nachweis von Hand?

---

## 6. Anhang: geprüfte API-Namen (xBIM, Abruf 15.09.2026)

| Was | Name | Namensraum | Fundstelle |
|---|---|---|---|
| Modell öffnen | `MemoryModel.OpenRead(string fileName, ReportProgressDelegate progressDel = null)` | `Xbim.IO.Memory` | `XbimEssentials/Xbim.IO.MemoryModel/MemoryModel.cs` |
| STEP-Text aus Strom | `MemoryModel.OpenReadStep21(Stream stream, ReportProgressDelegate, IEnumerable<string> ignoreTypes, bool allowMissingReferences, bool keepOrder)` | dito | dito |
| Schema erkennen | `MemoryModel.GetSchemaVersion(string fileName) → XbimSchemaVersion` | dito | dito |
| Schemaliste | `enum XbimSchemaVersion { Unsupported, Ifc4, Ifc4x1, Ifc2X3, Cobie2X4, Ifc4x3 }` | `Xbim.Common.Step21` | `Xbim.Common/Step21/XbimSchemaVersion.cs` |
| Fabrikwahl | `MemoryModel.GetFactory(XbimSchemaVersion) → IEntityFactory` (`EntityFactoryIfc4`, `EntityFactoryIfc2x3`, `EntityFactoryIfc4x3Add2`) | `Xbim.IO.Memory` | dito |
| Abfrage | `model.Instances.OfType<T>()`, `.FirstOrDefault<T>()`, `.Where<T>(…)` | `Xbim.Common` | docs.xbim.net, Quick Start |
| Psets | `IIfcObject.IsDefinedBy → IEnumerable<IIfcRelDefinesByProperties>` | `Xbim.Ifc4.Interfaces` | `Xbim.Ifc4/Kernel/IfcObject.cs` |
| | `IIfcRelDefinesByProperties.RelatingPropertyDefinition : IIfcPropertySetDefinitionSelect` | dito | `Xbim.Ifc4/Kernel/IfcRelDefinesByProperties.cs` |
| | `IIfcPropertySet.HasProperties : IItemSet<IIfcProperty>` | dito | `Xbim.Ifc4/Kernel/IfcPropertySet.cs` |
| | `IIfcPropertySingleValue.NominalValue : IIfcValue`, `.Unit : IIfcUnit` | dito | `Xbim.Ifc4/PropertyResource/IfcPropertySingleValue.cs` |
| Quantities | `IIfcElementQuantity.Quantities : IItemSet<IIfcPhysicalQuantity>`, `.MethodOfMeasurement` | dito | `Xbim.Ifc4/ProductExtension/IfcElementQuantity.cs` |
| | `IIfcQuantityArea.AreaValue : IfcAreaMeasure` (analog `IIfcQuantityLength.LengthValue`, `IIfcQuantityVolume.VolumeValue`) | dito | `Xbim.Ifc4/QuantityResource/IfcQuantityArea.cs` |
| Räume | `IIfcSpace : IIfcSpatialStructureElement, IfcSpaceBoundarySelect` mit `PredefinedType`, `ElevationWithFlooring`, inverse `BoundedBy : IEnumerable<IIfcRelSpaceBoundary>` | dito | `Xbim.Ifc4/ProductExtension/IfcSpace.cs` |
| Raumgrenzen | `IIfcRelSpaceBoundary : IIfcRelConnects` mit `RelatingSpace : IIfcSpaceBoundarySelect`, `RelatedBuildingElement : IIfcElement`, `ConnectionGeometry : IIfcConnectionGeometry`, `PhysicalOrVirtualBoundary : IfcPhysicalOrVirtualEnum`, `InternalOrExternalBoundary : IfcInternalOrExternalEnum` | dito | `Xbim.Ifc4/ProductExtension/IfcRelSpaceBoundary.cs` |
| | Subtypen `IIfcRelSpaceBoundary1stLevel`, `IIfcRelSpaceBoundary2ndLevel` (nur IFC4 aufwärts) | dito | `Xbim.Ifc4/ProductExtension/IfcRelSpaceBoundary{1st,2nd}Level.cs` |
| Bauteile | `IIfcWall`, `IIfcWindow`, `IIfcSlab`, `IIfcRoof`, `IIfcDoor`, `IIfcCurtainWall`, `IIfcPlate` | dito | `Xbim.Ifc4/SharedBldgElements/`, `…/ProductExtension/` |
| Räumliche Struktur | `IIfcSite`, `IIfcBuilding`, `IIfcBuildingStorey`, `IIfcZone`, `IIfcSpatialZone` | dito | `Xbim.Ifc4/ProductExtension/` |
| Kontext | `IIfcContext.RepresentationContexts : IItemSet<IIfcRepresentationContext>`, `.UnitsInContext : IIfcUnitAssignment` (`IIfcProject : IIfcContext`) | dito | `Xbim.Ifc4/Kernel/IfcContext.cs`, `…/IfcProject.cs` |
| Norden | `IIfcGeometricRepresentationContext.TrueNorth : IIfcDirection`, `.WorldCoordinateSystem : IIfcAxis2Placement`, `.HasCoordinateOperation : IEnumerable<IIfcCoordinateOperation>` | dito | `Xbim.Ifc4/RepresentationResource/IfcGeometricRepresentationContext.cs` |
| Einheiten | `IIfcUnitAssignment.Units : IItemSet<IIfcUnit>`; `IIfcSIUnit : IIfcNamedUnit` mit `Prefix : IfcSIPrefix?`, `Name : IfcSIUnitName` | dito | `Xbim.Ifc4/MeasureResource/IfcUnitAssignment.cs`, `…/IfcSIUnit.cs` |
| Schichten | `IIfcMaterialLayerSet.MaterialLayers : IItemSet<IIfcMaterialLayer>`, `.LayerSetName`, `.TotalThickness` | dito | `Xbim.Ifc4/MaterialResource/IfcMaterialLayerSet.cs` |
| | `IIfcMaterialLayer.Material : IIfcMaterial`, `.LayerThickness : IfcNonNegativeLengthMeasure`, `.IsVentilated : IfcLogical?`, `.Category`, `.Priority`, `.ToMaterialLayerSet` | dito | `Xbim.Ifc4/MaterialResource/IfcMaterialLayer.cs` |
| Materialbindung | `IIfcRelAssociatesMaterial` | dito | `Xbim.Ifc4/ProductExtension/IfcRelAssociatesMaterial.cs` |
| Schemabreite | `Xbim.Ifc2x3` und `Xbim.Ifc4x3` tragen je einen Ordner `Interfaces/IFC4` — dieselben `IIfc*`-Schnittstellen für alle drei Schemata | — | GitHub `xBimTeam/XbimEssentials` |
| **Nicht benutzen** | `IfcStore` (`Xbim.Ifc`) — Paket `Xbim.Ifc` hängt an `Xbim.IO.Esent` (Windows) | `Xbim.Ifc` | nuspec `Xbim.Ifc 6.1.605` |
