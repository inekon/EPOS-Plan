# CLAUDE.md — `EPOS.Kern`, der Rechenkern

Der plattformfreie Kern: `net10.0` **ohne** `-windows`, AnyCPU, Namensraum
`WindowsFormsApplication1`, Bezeichner und Kommentare deutsch. Repositoriumsweite Regeln und
Wegweiser zu den Schalen: [Wurzel-`CLAUDE.md`](../CLAUDE.md).

**Die eine Regel: Eine Fachänderung am Rechenkern wird EINMAL gemacht — hier.** Die Schalen
referenzieren dieses Projekt, sie übersetzen seine Dateien nicht mit.

## Was hier liegt

| Ordner | Rolle |
|---|---|
| `Allgemein/` | `BhkwPlan` (die Physik), Zugriffsschicht (`DataRepository`, `DbParam`), `Meldung`, `Sprache`, `Energieeinheit` |
| `Allgemein/Simulation/` | die Engine: `SimulationControl`, `SimulationRunner`, je ein Modul für Bedarf und Erzeuger, `Rechenrand`, `ProfilBedarf` |
| `Allgemein/Wirtschaftlichkeit/` | Kapitalwert, KWKG, EEG, Steuer, Stromtarife, Emissionsbilanz, `Emissionsquelle` |
| `Allgemein/Bericht/` | `ChartRenderer` (SkiaSharp), Word- und Excel-Erzeuger, `Bausteine/`, Kennzahlen |
| `Allgemein/Dienste/`, `Datenbank/`, `Update/` | die neun Umgebungsschnittstellen; `Erstbereitstellung`, `Datenbanksicherung`, Schemapflege, `GeraeteWaisen` (der Aufräumlauf der verwaisten Gerätezeilen, gerufen aus `WErzeugerCtrl.Delete` und `WizardCtrl`) |
| `Allgemein/Import/`, `Katalog/`, `Export/` | Hersteller- und Klimadaten, Ganglinien; Register, Filter, Dubletten; CSV |
| `Allgemein/Lizenz/`, `KI/`, `Hilfe/` | Lizenzlage und Token; KI-Zugang, Aufgabenregister, Semantik; Berechnungshilfe |
| `Controller/`, `Model/`, `Properties/` | die Fachcontroller, ihre Datenklassen, `Settings` |
| `MyResource/` | die beiden `.resx` und die erzeugte `Resource.Designer.cs` |

Einzige verlinkte Datei: `../sql/schema/SchemaTypKatalog.g.cs` (Quelle `sql/tools/Erzeuge-Schema.ps1`).

## Was mit Absicht NICHT hier liegt

| Was | Warum |
|---|---|
| Oberflächenbausteine (`BaseForm`, `GrafikTools/*`, Hilfefenster, Blazor-Hülle) | WinForms und GDI+ — sie **sind** die Oberfläche |
| `SchemaMigration` der Schale | Schemapflege des Windows-Programmstarts; Access-Altbestände werden nicht übernommen |
| Belegung der `Dienste.*`, Datenbankbereitstellung der Schale | jede Schale beantwortet sie selbst |
| `HilfeKontext`, `KiAusfuehrungWindows`, `KlimaregionStammCtrl`, `MenueCtrl` | hängen an einer WinForms-Ansicht, am aktiven Fenster, an `ComboBox`/`ListBox` oder der Navigation |
| `Bericht/Vorlagen/Berichtsvorlage.docx`, `Berichtsvorlage_Standard.docx`, `Berichtsvorlage_Kurzbericht(_en).docx` | keine Quelldateien: Sie liegen neben der EXE bzw. im iOS-Bundle, der Kern findet sie über `Dienste.Pfade.Berichtsvorlagen` (`BerichtsvorlagenCtrl.DATEI_STANDARD`/`DATEI_RUECKFALL`) — die Standardvorlage für den Vorlagenweg, die Stilvorlage als Rückfall, der Kurzbericht je Sprache als Muster von „Neue Vorlage…“ (`DATEI_KURZBERICHT`, `Musterpfad`) |
| `EPOS.UI.Daten` (die Hüllen) | referenzieren den Kern — nicht umgekehrt |

**Die `partial`-Falle.** Vor jedem Umzug einer Klasse hierher prüfen, ob sie in der Anwendung
eine zweite Hälfte hat — `partial` trägt nur innerhalb EINER Assembly.

## Regeln für Änderungen hier

- **Kein WinForms-Code, kein `System.Data.OleDb`.** `EnableWindowsTargeting=false` bricht den
  Build sofort statt erst auf dem iPad; CA1416 steht bei 0, **ohne `NoWarn`**.
- **Alle Pakete plattformfrei**, Fassungen in `Directory.Packages.props`; `SixLabors.Fonts` **auf
  1.0.1 gepinnt** (Lizenzwechsel ab 2.x). **Kein zweites `TargetFramework`** — die iOS-Hülle
  nimmt den Kern als `net10.0`-Bibliothek.
- **Datenzugriff nur über `DataRepository` mit `new DbParam(…)`** — kein zusammengesetzter
  SQL-Text, kein `RecordSet` dafür. **`PfadUeberschreibung` schlägt alles**; Referenzlauf und
  Tests hängen daran. Dialekt und Umlautregel:
  [`BETRIEB_SQLITE.md`](../Dokumentation/aktuell/BETRIEB_SQLITE.md) § 6.
- **Eine Datenbank entsteht nur über `Erstbereitstellung.Sicherstellen`** (Vorlage kopieren,
  `integrity_check` und `SchemaVersion` prüfen, **nie** überschreiben), **eine Sicherungskopie nur
  über `Datenbanksicherung.KopieAnlegen`** (`VACUUM INTO` statt `File.Copy`, dem unter WAL
  Änderungen fehlen).
- **Die Umgebung nur über `Dienste.*` — nie über `Program.*`.** Die neun Schnittstellen haben je
  eine oberflächenlose Standardfassung, damit Kern, Tests und Referenzlauf ohne Schale laufen;
  belegt an EINER Stelle in `Program.Main`.
- **Maskennamen und Gewerke sind sprachneutrale ASCII-Schlüssel** (`Gewerke.Bhkw`), nie ein
  Anzeigetext.
- **Oberflächenaufgaben über Haken:** ein `static Action<…>`-Feld mit folgenloser Vorbelegung,
  von der Schale **ausdrücklich** belegt — nie über `[ModuleInitializer]`, unter AOT nicht
  beweisbar.
- **`Resource.Designer.cs` ist erzeugter, eingecheckter Quelltext** und wird nur neu geschrieben
  (`Werkzeuge/ResourceDesigner/designer_neu.py schreiben`); von Hand ergänzt gibt es Duplikate.
- **`InternalsVisibleTo` statt Sichtbarkeitsanhebung:** Ein Typ braucht kein `public`, nur weil
  ihn eine Schale sieht.
- **Die Feldgrößen sind fest verdrahtet:** 8 760 Stunden, 35 040 Viertelstunden, 168
  Wochenwerte, 365 Tage, 12 Monate, kein Schaltjahr; Arrays werden **in-place** beschrieben.
- **Gebäudebedarf: eine Weiche, getrennte Module.** `SimulationWaermebedarf.HeizwaermeEinesGebaeudes`
  ist die Fassade (Vorbereitung `GebaeudeVorbereitung`, Weiche `RechenwegWaehlen`, Naht
  `IGebaeudeRechenweg`); der Tagesbilanz-Weg liegt eingefroren in `Allgemein/Simulation/Altweg/`
  und bekommt keine neue Funktion. Außer der Weiche nennt keine Kerndatei `Altweg/`, und
  `Gebaeude/` und `Altweg/` nennen einander nicht — Wächter `ModultrennungswacheTests`.
- **Parallelität nur über `SpeicherEngine/Kulturweitergabe`**; ein nacktes `Parallel.*`,
  `Task.Run`, `new Thread` oder `.AsParallel()` fällt im Wächter `ParallelitaetWacheTests` auf —
  sonst lesen Aufrufer und Arbeitsfaden `DefaultThreadCurrent(UI)Culture` je für sich.

## Bericht

**Der Diagramm-Renderer liegt hier** (`Allgemein/Bericht/ChartRenderer.cs`, SkiaSharp, ohne
Windows-API): Der Kern liefert PNG-Bytes, die Oberfläche zeigt sie an. Ein neues Bild entsteht
deshalb hier, nicht in der Oberfläche und nicht mit einer zweiten Bibliothek.

Drei Regeln: Ein **neuer Parameter bekommt eine Vorgabe, die das Bild byte-gleich lässt** (die
ChartProben vergleichen Bilder). **Nicht endliche Werte fallen weg** statt das Bild zu Fall zu
bringen — ein Loch im Raster bekommt `C_RASTER_LOCH`, nicht die Minimumfarbe. Und die
**Schriftregel**: Rückfallkette über `SKFontManager`, Layout metrikgetrieben — **Textbreiten
dürfen je Plattform abweichen**, verglichen wird über Struktur und Histogramm, nicht Pixel.

**Die Berichtsschreiber (Bausteine, Anhang E, Excel-Generator, Formelmappe, Vorlagenfüller) lesen
nur `BerichtsDaten`** — alles aus der Datenbank sammelt `BerichtsDatenSammler.SammleFuerBericht`
einmal in `BerichtsDaten.Wirtschaft`; Wache `EPOS.Kern.Tests/BerichtSchreiberOhneDatenbankWacheTests`.

## Eine Emissionsquelle für alle Erzeuger

Jeder Emissionsfaktor kommt über `Allgemein/Wirtschaftlichkeit/Emissionsquelle.cs` — keine zweite
Stelle; sonst trägt ein Modul im Lauf und in der Bilanz verschiedene Zahlen.

| Was | Aufruf |
|---|---|
| Faktorsatz eines Erzeugers | `Fuer(idProjekt, carrierId, idBrennstoffRueckfall, modus)` |
| Berechnungsmodus des Laufs | `Modus(idProjekt)` — **einmal je Lauf**, nicht je Erzeuger |
| Netzstrom / verdrängte Wärme | `Netzstrom(…)`, `Waerme(…)`; Rückfall 435 bzw. 200 g/kWh |
| Stromträger des Projekts | `StromTraeger(idProjekt)` |

Lesekette und Modus stehen im
[Konzept](../Dokumentation/aktuell/Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md) § 3; der
Katalog führt CO₂ in **g/kWh**, SO₂/NOₓ/Staub in **mg/kWh**, die Summen in **t/a** bzw. **kg/a**.
Die Emissionsspalten der Kessel- und BHKW-Kataloge sind **„nur Anzeige"**
(`ParameterVerwendung`, Stufe `Dialog`). **Nachweis: `EmissionsquelleTests`** — keine
Referenz-CSV führt eine Emissionsgröße.

## Ein geliehener Wert wird nie still gesetzt

Fehlt einer Größe ihr Wert — ein CO₂-Faktor, ein Arbeits- oder Leistungspreis —, ist das eine
**Datenlücke** und keine Aufforderung an den Rechenweg, sich bei einem Nachbarn zu bedienen.
`Emissionsquelle` liefert dann 0 mit `Co2Gepflegt = false`, `KostenEmissionRechner` einen
benannten Fehlgrund; beides bleibt so.

Der Wert eines fachlich verwandten Datensatzes darf nur über einen **Bedienweg** einfließen:
`Controller/EnergietraegerRueckfall.cs` legt die Träger derselben Kategorie
(`energy_carrier.pricing_model`) mit ihrem Wert und dessen Einheit vor, und **erst die
Bestätigung des Anwenders schreibt** — auch dann, wenn es nur einen Kandidaten gibt. Geschrieben
wird in die **Projektübersteuerung**, nie in den Katalog, und die Oberfläche trägt eine
Herleitungszeile, an der ein geliehener Wert als geliehen erkennbar bleibt.

**Nachweis: `EnergietraegerRueckfallTests`** samt dem Fall, dass die Emissionsquelle weiterhin
nicht auf die Kategorie zurückfällt.

## Die Anzeigeeinheit einer Energiemenge

`Allgemein/Energieeinheit.cs` (ohne Datenbank, ohne Oberfläche) trägt **MWh (Vorgabe)** und
**kWh**; die Identität ist **bitgleich** statt über `× 1000 × 0,001` gerechnet.

**Hausregel: eine Energiemenge wird GENAU EINMAL umgerechnet, an der Anzeigekante.** Im Kern und
in den Hüllen bleibt die Zahl in ihrer Quelleneinheit; erst die Anzeige rechnet um, über
`Energieeinheit` statt über einen nackten Teiler. Eine Hülle nennt die **Einheit am Wert**
(`QuelleEinheit`) — ein zweiter Teiler verschiebt sie um Faktor 1 000.

## Einheiten: die Regel des Rechenkerns

Herleitung: [Einheitenkonzept](../Dokumentation/aktuell/Konzept_Einheiten_EPOS-Plan.md).

1. **Zeitreihen führen kWh**, Viertelstundenreihen kW — so stehen sie in den Vektordateien der
   Referenzbasis.
2. **Jahres- und Monatssummen, die den Kern VERLASSEN, führen MWh** — an Anzeige, Bericht,
   Datenbank und CSV.
3. **Jede Größe, die den Kern verlässt, trägt ihre Einheit im NAMEN** (`…Kwh`, `…Mwh`, `…Kw`)
   oder im DTO; ein Kommentar hält die Einheit nicht.
4. **Umgerechnet wird an genau ZWEI Nähten:** `SimulationErgebnisCtrl` und `SimulationRunner`;
   sonst steht in `EPOS.UI` und den Windows-Hüllen **kein** Faktor 1 000 auf einer Energiemenge.
5. **Kein Wechsel der Recheneinheit:** Wo ein Rechenobjekt MWh führt, bleibt es MWh.

**Zwei Wächter halten die Regel** (`EinheitenWacheTests`):
`In_Anzeige_und_Huellen_steht_kein_Faktor_1000_auf_einer_Energiemenge` (fünf Ausnahmen, alle
LEISTUNG) und `Jede_Jahressumme_der_Simulationsklassen_traegt_ihre_Einheit_im_Namen` (13
Ausnahmen). Eine Suche nach `/ 1000` genügt **nicht**: `BhkwPlan.VectorSumme`/`MonatsSumme`
schreiben `0.001`, die Viertelstundenreihen teilen durch 4 000. **Die CSV-Schlüssel der
Referenzbasis bleiben, wie sie sind** (`Sim.Restwaerme`, `Puffer.*_gesamt`), auch wo das Feld
`…Mwh` heißt — sonst ist kein Vergleich gegen die Basis mehr möglich.

## Typen: der Rechenkern rechnet in `double`

Der ganze Rechenweg führt `double` — Reihen, Akkumulatoren, Felder, Parameter, Rückgaben und die
Summenfunktionen `BhkwPlan.VectorSumme`/`MonatsSumme`; die Datenbankgrenze passt, SQLite `REAL`
**ist** `double`. **`float` steht nur an drei Grenzen:** Bildpunkte im `ChartRenderer` (SkiaSharp
rechnet so, die Datenreihen bleiben `double`), die Einbettungen des KI-Wissens und Typprüfungen
gegen einen boxed Fremdwert. **Der Wächter `DoubleWacheTests`** hält `Allgemein/Simulation/**`
und `BhkwPlan.cs` frei von `float`, `Convert.ToSingle`, `MathF.` und `f`-Suffix; seine
Ausnahmeliste ist **leer**.

**Drei Schwellen des Modells entscheiden am letzten Bit** und tragen das über Stunden weiter:
Speicherhysterese, Modulationsgrenze des BHKW, die Rückgaben der BHKW-Plan-Physik. Wer eine davon
anfasst, **ändert den Rechenweg fachlich** und braucht einen Entscheid samt neuer Referenzbasis.

## Vergleiche an Betriebsschwellen tragen den Zahlenrand

Ein Vergleich, der eine BETRIEBSSCHWELLE entscheidet, darf nicht am letzten Bit kippen. Der Rand
steht **einmal** in `Allgemein/Simulation/Rechenrand.cs`: `SchwelleErreicht(wert, schwelle)`
prüft `wert >= schwelle − Rand`, `Rand = ABSOLUT (1e-9) + RELATIV (1e-12) · |Schwelle|`. Er ist
**absolut UND relativ**, weil die Schwellen Energien über mehrere Größenordnungen tragen, und
bleibt vier Größenordnungen unter der Toleranz der Referenzsuite. **Die Leserichtung:** SCHWELLE
ist die Maschinengröße (Nennleistung, Modulationsgrenze), WERT der Rest, der ihr gegenübersteht;
bei der UNTERgrenze `EntnahmeObergrenze` vertauscht.

**Einen Rand braucht jede Marke, auf die eine Rechnung ZUSTEUERT** (die Ladung fährt auf
`Q_max · SchwelleAus`), **und jeder Vergleich, dessen Operanden aus getrennten Rechenketten
stammen.** Die Einschaltschwelle bleibt bewusst ohne — auf sie steuert keine Rechnung zu. **Wer
eine neue Betriebsschwelle einführt, nimmt `Rechenrand.SchwelleErreicht` — nicht `>=`.**
Nachweis: `RechenrandTests`, `RechenrandFahrweisenTests`.

## Keine `(int)`-Abschneidung auf einer Rechengröße

`TagesbilanzPhysik.TaeglHeizlastWG`, `SolareGewinneC` und `SpezWaermeverlusteC` (Modul
`Allgemein/Simulation/Altweg/`) geben `double` zurück und
schneiden nicht ab; ihre Aufrufer rechnen mit `/ 100.0` statt ganzzahlig. Der Faktor 100 bleibt —
er gehört zur Schnittstelle der Funktion, nicht zur Physik. **Die Regel daraus:** Auf einer
Rechengröße steht keine `(int)`-Wandlung; wer eine Zahl ganzzahlig braucht, wandelt sie erst
dort, wo sie ein Index wird. Nachweis: `BhkwPlanRueckgabeTests`.

## Vorschau und Lauf lesen dieselben Tabellen

**Ob eine Profilrechnung den KATALOG oder die PROJEKTKOPIEN liest, hängt am PROJEKT — nicht
daran, ob eine Namensliste mitkommt.** Die Regel steht einmal in `ProfilBedarf.Vorschaumodus`:
ohne Liste `Projektrechnung`, mit Liste **ohne** Projekt `Katalogvorschau`, mit Liste **und**
Projekt `Projektvorschau`.

**Die Projektvorschau liest die KOPIE zuerst** — dieselben Tabellen und derselbe Projektfilter
wie im Lauf — und fällt nur für einen dem PROJEKT unbekannten Namen auf den `_STAMM`-Katalog
zurück (`ProfilQuelle.Rueckfall`, mit **Kopf UND Typprofil**, sonst erscheint eine fremde
Monatsverteilung). **Eine Zahl der Vorschau wird am Lauf gemessen, nicht am Katalog.**

## Eine Auskunft ruft den Rechenweg des Laufs — sie schreibt ihn nicht ab

Eine Zahl, die der Anwender neben eine Kennzahl der Ergebnisseite legt, muss dieselbe sein — sie
darf also nicht ein zweites Mal gerechnet, sondern nur ein zweites Mal **gerufen** werden. Wo der
Weg in einer Schleife steckt, wird der **Schleifenrumpf ausgelagert**:
`SimulationWaermebedarf.KlimakalenderLesen` und `…HeizwaermeEinesGebaeudes` ruft der Lauf in
seiner Schleife, `GebaeudeBedarfCtrl` für sein eines Gebäude. **Die Probe ist dann der Vergleich
gegen den LAUF selbst** (`GebaeudeBedarfCtrlTests`); auch die Schreibweise der Division wird
übernommen.

## Nachweis

Die Befehle stehen in der Wurzel-`CLAUDE.md`. Nachgewiesen ist eine Kernänderung, wenn
`dotnet test WP-Plan.Kern.slnf` grün ist, der **Referenzlauf** gegen die Basis unter
`Referenzlaeufe/` `GESAMT: PASS` meldet und — bei betroffenem Bild — `ChartProben` grün ist. Für
eine Größe ohne Referenz-CSV kommt eine Probe in `EPOS.Kern.Tests` dazu.

**Zwei `git grep`-Wächter müssen leer bleiben** (Kommentarzeilen ausgenommen): `\bProgram\.` über
`EPOS.Kern/*.cs` und die Kernkandidaten unter `../WindowsFormsApplication1/{Allgemein,Controller,Model}`;
sowie `System.Windows.Forms`, `System.Drawing`, `MessageBox.`, `\bRegistry\.` (Wortgrenze, sonst
trifft `speicherRegistry.`), `ProtectedData`, `OleDb` in `EPOS.Kern/*.cs`.

- **Tests mit Datenbank:** `TestDatenbank.cs` legt je Testfall oder Testklasse eine Arbeitskopie
  von `Kenndaten_Test.sqlite` unter `%TEMP%\epos-kerntest-*` an und biegt `PfadUeberschreibung`
  darauf um; fehlt die Datei, schweigen die Fälle. **Jede Instanz wird entsorgt:** `using`,
  Klassenvorrichtung oder Feld einer Testklasse, die `IDisposable` trägt und das Feld in `Dispose`
  entsorgt — sonst bleibt je Testfall eine Kopie von rund 65 MB liegen. Wächter:
  `TestDatenbankEntsorgungWacheTests`; was trotzdem liegen bleibt, räumt der nächste Lauf über die
  freie Besitzmarke weg.
- **`[Collection("Testdatenbank")]` ist die EINE serielle Sammlung.** Wer die Testdatenbank
  benutzt **oder** ein `Dienste.*` tauscht, gehört hinein — beides ist prozessweiter Zustand,
  und xunit trennt nur INNERHALB einer Sammlung. Wächter: `DiensteSammlungTests`.
- **Kulturpinnung:** Jede Testklasse mit deutschen Ressourcentext-Asserts führt
  `Kulturvorrichtung.cs`, die alle VIER Kulturwerte auf `de-DE` pinnt. Nur `CurrentCulture`
  genügt nicht: `Resource.*` liest über `CurrentUICulture`, der Windows-Läufer steht auf
  **en-US** — auf Linux bleibt der Fehler unsichtbar.
- **Dateiformate:** `.cs`, `.csproj`, `.resx` UTF-8 **mit** BOM und CRLF, Markdown **ohne** BOM
  mit CRLF; ältere Dateien können Windows-1252 ohne BOM sein — vorher Bytes messen.

Die ausführliche Fassung mit Herleitungen und Umzugsgeschichte:
[`EPOS.Kern_CLAUDE_2026-09-12.md`](../Dokumentation/ueberholt/Protokolle/CLAUDE-Historie/EPOS.Kern_CLAUDE_2026-09-12.md).
