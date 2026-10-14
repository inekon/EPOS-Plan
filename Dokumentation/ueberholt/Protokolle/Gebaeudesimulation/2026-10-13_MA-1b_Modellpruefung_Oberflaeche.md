# Protokoll MA-1b — Blatt „Modell prüfen“ (Oberfläche)

Welle MA-1b aus dem [Entwurf Geführter Modellaufbau](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md)
(Abschnitte 0 und 2, Wellenplan Zeile MA-1, Oberflächenhälfte) auf der Kern-Schnittstelle aus
[MA-1a](2026-10-13_MA-1a_Modellpruefung_Kern.md). Grundlage ist Entscheid E137: F1 Ampel je Prüfgruppe mit Zählung und
Rechenstatus, F2 „Alle Vorschläge übernehmen“ und je Gruppe mit Rücknahme. Leitziel: Import → ein Klick → rechenfähig, ohne
Pflichteingabe.

## Ergebnis

- **Hülle** `EPOS.UI.Daten/Bedarf/ModellpruefungHuelle.cs` auf `ModellpruefungCtrl`: baut den `ModellpruefungWeg` des Blatts
  (Prüfen, Übernehmen je Gruppe, Alle übernehmen, Zurücknehmen, Nordrichtung bestätigen). Der Stand trägt Rechenstatus samt Text
  (`Statusname`), fünf Gruppenzeilen (Name aus `Gruppenname`, Ampel, Zählung, Vorschlagszahl, Sprungziel, `KannZuruecknehmen`)
  und je Befund das benannte Ziel „Zone — Bauteil“ (Namen aus `GebaeudeZonenCtrl.LesenJeGebaeude`). Ausnahmen des Kerns werden zu
  benannten Meldungen. Plattformfrei, kein SQL.
- **Daten** `EPOS.UI/Dialoge/Bedarf/ModellpruefungDaten.cs`: `ModellpruefungStand`, `ModellpruefungGruppenzeile`,
  `ModellpruefungBefundzeile`, `ModellpruefungAusgang`, `ModellpruefungWeg` (Delegaten, keine Datenbank).
- **Blatt** `EPOS.UI/Dialoge/Bedarf/Modellpruefungsblatt.razor` als Überlagerung im Gebäudedialog: Kopf mit Gebäudename,
  Rechenstatus (Klasse `epos-ampel-gruen|gelb|rot`) und Hilfeknopf; je Prüfgruppe eine Zeile mit Ampel, Name (klappt die Befunde
  auf — erst auf Klick, ohne Befund gesperrt), Zählung „n Befunde, m Vorschläge“, „Vorschlag übernehmen“ (gesperrt ohne Vorschlag),
  „Zurücknehmen“ (nur wenn `KannZuruecknehmen`), „Ansicht…“; Gruppe 4 dazu „Klimaregion wählen…“ und „Nordrichtung bestätigen“.
  Fuß: „Alle Vorschläge übernehmen“ (Hauptknopf, gesperrt ohne Vorschlag) und „Simulation…“. Nach jeder Übernahme oder Rücknahme
  zeigt das Blatt den neu geprüften Stand und die Meldung („n Vorschläge übernommen, m Befunde brauchen eine Eingabe.“).
- **Sprung in die Ansicht:** „Ansicht…“ öffnet die Gebäudeansicht des Gebäudedialogs — ihr einziger Wirt mit Dateikörper ist dort
  „Datei erneut lesen“ (Dateiwahl der gespeicherten Importquelle, Prüfsumme) — und setzt über den neuen Parameter
  `GebaeudeAnsicht.FarbmodusVorgabe` den Farbmodus des Sprungziels: Gruppe 1 → „Randbedingung“, 2 → „Aufbau“, 3 → „Zonen“, 4 und 5
  → „Befund“. Ein nicht wählbarer Modus fällt wie bei der Hand auf „Zonen“ zurück. Ohne Importquelle ist der Knopf weich gesperrt
  mit Grund („Die Gebäudeansicht braucht die Importdatei …“). „Datei erneut lesen“ am Satz öffnet weiter ohne Vorgabe.
- **Gruppe 4:** „Nordrichtung bestätigen“ schreibt die gespeicherte Richtung unverändert über den Bestandsweg
  `GebaeudeImportCtrl.AusrichtungAendern` (Drehung 0, Herkunft „Eingabe“ statt „Annahme“) und prüft neu; ohne Importquelle nennt es
  den Grund. „Klimaregion wählen…“ hat im Gebäudedialog keine bestehende Auswahl (die Klimaregion gilt je Projekt) und nennt den
  Hinweis auf die Projektdaten; der Parameter `Klimaregion` des Blatts nimmt einen Wirt auf, sobald es einen gibt.

## Einstiege

- **Kopfleiste IM PROJEKT** (`GebaeudeDialog`): „Modell prüfen…“ neben „Simulation…“ für die gewählte Projektzeile, ohne Wahl die
  erste; ohne Projektzeile weich gesperrt mit Grund; eine Zeile ohne Projektkopie wird zuvor still gespeichert (Weg
  `VorherSpeichern` wie „Bearbeiten…“). Nicht im Assistenten, ohne Delegat kein Knopf; die Katalogverwaltung kennt ihn nicht.
- **Nach dem Import:** Nach jedem erfolgreichen Gebäudeimport (Editor gespeichert, Zeile aufgenommen, Projektkopie angelegt) öffnet
  der Gebäudedialog das Blatt für das neue Gebäude von selbst; ohne Weg (Zeile ohne Projektgebäude) bleibt es still zu.
- **Nach „Als Mehrzonenmodell übernehmen…“:** Der Editor meldet über den neuen Parameter `GebaeudeKatalogDialog.MehrzonenGeschrieben`,
  dass der OK-Weg das Mehrzonenmodell geschrieben hat; nach dem Schließen öffnet der Gebäudedialog das Blatt für dieses Gebäude.
- **Quellhinweise:** Die Hülle des Imports und die Mehrzonenhülle lesen aus dem Zonenplan der Übernahme je Planzone DIN-Nummer,
  Raumtyp oder Nutzungsklasse (Art der `Profilquelle`) und merken sie an der Zeile bzw. am Gebäude; das nächste Öffnen des Blatts
  setzt sie mit `QuelleSetzen` je Zone gleichen Namens und verbraucht sie.
- **Hülle des Gebäudedialogs:** neuer Parametersatz `ModellpruefungGaben` (`GebaeudeHuelle`).
- **Hilfe:** `help_mapping.txt` Zeile `Modellpruefung.btn_Help = Gebäude` (Seite als Ganzes, ohne Anker).

## Dateien

- neu: `EPOS.UI/Dialoge/Bedarf/ModellpruefungDaten.cs`, `Modellpruefungsblatt.razor`; `EPOS.UI.Daten/Bedarf/ModellpruefungHuelle.cs`;
  Tests `EPOS.UI.Tests/Dialoge/ModellpruefungsblattTests.cs`, `GebaeudeDialogTests.Modellpruefung.cs`,
  `EPOS.Kern.Tests/ModellpruefungHuelleTests.cs`
- geändert: `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor`, `GebaeudeKatalogDialog.razor`, `EPOS.UI/Bausteine/GebaeudeAnsicht.razor`,
  `EPOS.UI/wwwroot/epos-ui.css` (Regeln `epos-ampel*`, `epos-modellpruefung-*`), `EPOS.UI.Daten/Bedarf/GebaeudeHuelle.cs`,
  `GebaeudeMehrzonenHuelle.cs`, `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt`, `EPOS.UI.Tests/Dialoge/GebaeudeDialogImportTests.cs`,
  `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx` (je 32 Schlüssel `MPB_*`), `Resource.Designer.cs` (Designer, wiederholbar)

Kein Schemaschritt, kein Rechenwegbaustein, kein SQL angefasst; Basis R52 unberührt. Die Regeln `.epos-raster*` sind unberührt
(das Blatt ist eine Liste, kein Raster); Kopf und Schlussleiste der Dialoge sind unverändert.

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale mit `EnableWindowsTargeting=true`: 0 Fehler.
- `EPOS.UI.Tests` mit Filter `Gebaeude|Modellpruefung`: 905 bestanden, 0 rot (18 neue Fälle: Blatt 7, Gebäudedialog 9, Import 2).
- `EPOS.Kern.Tests` mit Filter `Modellpruefung`: 18 bestanden (5 neue Hüllentests gegen die Arbeitskopie: Stand mit fünf Gruppen
  und benannten Zielen an 1054, Alle übernehmen mit Rücknahme, kein Weg ohne Gebäude, Quellhinweise, Nordrichtung ohne Quelle).
- `designer_neu.py`: wiederholbar (zweiter Lauf +0).

## Nachzug Wachen

Im Nachgate des Integrationsstands waren zwei UI-Wachen rot:

- `FormularrasterTests.Ausserhalb_des_Rasters_bleibt_ein_Feld_unveraendert` liest `epos-ui.css` vom Kommentar „FORMULARRASTER“
  bis zum Dateiende; die angehängten Regeln `epos-ampel*` und `epos-modellpruefung-*` standen darin. Sie stehen jetzt als eigener
  Abschnitt mit Kommentarkopf vor dem Formularraster, inhaltlich unverändert.
- `InfoknopfSchluesselWacheTests.Es_gibt_keine_fuenfte_Ausnahme`: Der Hilfeknopf des Blatts setzt `MitAssistent="false"`. Das Blatt
  steht nur als Überlagerung im Gebäudedialog, dessen Kopfpille den Assistenten führt — das ist der vorgesehene Fall einer
  eingebetteten Seite, keine fünfte Ausnahme. Das Blatt steht deshalb mit Begründung in `KiKnopfEinmalWacheTests.OHNE_ASSISTENT`;
  der Hilfeknopf (Seite „Gebäude“) bleibt, der Assistent bleibt über die Kopfpille erreichbar und erscheint nicht zweimal.

Nachweis: Kern-Filter 0 Fehler; `EPOS.UI.Tests` mit Filter `Formularraster|InfoknopfSchluessel|Modellpruefung|GebaeudeDialog|KiKnopfEinmal`
250 bestanden, 0 rot.

## Offen

- Windows-Sichtabnahme: Breite der Kopfleiste mit „Simulation…“ und „Modell prüfen…“, Zeilenumbruch der Gruppenzeilen, Ampel-
  und Statusfarben in beiden Farbschemata.
- „Ansicht…“ geht über „Datei erneut lesen“ und verlangt die Dateiwahl; eine Ansicht des gespeicherten Modells ohne Datei (das
  Exportmodell aus den Bauteilen) wäre der Weg ohne Importquelle — eigener Auftrag.
- „Klimaregion wählen…“: keine Auswahl im Gebäudedialog; ein Sprung in die Projektdaten braucht eine Naht der Navigation.
- Ampelleiste im Importdialog vor dem OK (Prüfung des Importvorschlags statt des gespeicherten Gebäudes) und im Reiter „Zonen“
  des Editors — nach dem Entwurf spätere Stufe.
- Quellhinweise werden über den Zonennamen zugeordnet; eine nach dem Import umbenannte Zone bekommt keinen Hinweis.
- Hilfeseite „Gebäude“: Abschnitt „Modell prüfen“ mit Anker, dann Zeile in `help_mapping.txt` auf den Anker setzen.

## Logbuch-Vorschlag

„Im Gebäudedialog prüft ‚Modell prüfen…‘ ein Gebäude auf Rechenfähigkeit, zeigt je Prüfgruppe eine Ampel und übernimmt die
Vorschläge mit einem Klick — nach einem Import öffnet die Prüfung von selbst.“

## Nachzug N1 (14.10.2026)

- **Befund:** Die Ampelleiste im Reiter „Zonen“ des Editors stand aus (Entwurf 2.2, Offen oben); die Prüfung war nur über
  „Modell prüfen…“ der Kopfleiste zu erreichen.
- **Ergebnis:** Neuer Baustein `EPOS.UI/Dialoge/Bedarf/Modellpruefungsleiste.razor` — Rechenstatus (Klasse
  `epos-ampel-gruen|gelb|rot`) und je Prüfgruppe ein Knopf mit Ampel, Name und Zahl der Befunde, Titel mit Befunden und
  Vorschlägen. Der Gebäudedialog reicht dem Editor der Projektzeile `ModellpruefungLesen` (Weg `ModellpruefungWeg.Pruefen` der
  Hülle `ModellpruefungHuelle`, einmal je Öffnen gebaut) und `PruefgruppeOeffnen` durch; ein Klick öffnet das Ampelblatt
  über dem Editor auf dieser Gruppe (`Modellpruefungsblatt.StartGruppe`: ihre Befunde stehen offen). Jede Übernahme oder
  Rücknahme im Ampelblatt, jede Änderung im Blatt „Nutzung und Kalender“ und jede Auswahl an der Legende der Ansicht zählt
  `PruefFassung` hoch — die Leiste prüft neu, ohne ungespeicherte Zonen liest der Reiter die Zonen neu (das OK des Editors
  schreibt so keinen veralteten Stand zurück). Mit ungespeicherten Zonen ist die Leiste weich gesperrt (Grund wie „Nutzung und
  Kalender…“); im Assistenten, im Katalog und ohne Delegat keine Leiste. CSS `epos-pruefleiste*` vor dem Formularraster.
- **Dateien:** `Modellpruefungsleiste.razor` (neu), `Modellpruefungsblatt.razor`, `GebaeudeKatalogDialog.razor`,
  `GebaeudeDialog.razor`, `EPOS.UI/wwwroot/epos-ui.css`, `Resource(.en-US).resx` (`MPB_LEISTE_TITEL`,
  `MPB_LEISTE_GRUPPE_HINWEIS`), `Resource.Designer.cs`.
- **Tests:** `GebaeudeKatalogPruefleisteTests` (Leiste mit Status, fünf Gruppen, Klick; ohne Weg keine Leiste; neue Prüffassung
  liest Leiste und Zonen neu; Startzone), `GebaeudeDialogTests.N1_Ampelleiste_im_Editor_oeffnet_das_Ampelblatt_auf_der_Gruppe`,
  `…N1_Ampelleiste_nicht_im_Assistenten`.
- **Offen:** ~~Eine Übernahme der Gruppe 4 (Nordrichtung) schreibt am Gebäude; der Editor liest nach einer Prüffassung nur die
  Zonen neu, nicht die Felder der Hülle — ein OK danach schreibt die Hüllfelder des Editors.~~ Erledigt mit Nachzug N3. Windows-Sichtabnahme der Leiste.
- **Logbuch:** „Im Reiter „Zonen“ des Gebäudeeditors zeigt eine Ampelleiste den Stand der Modellprüfung; ein Klick öffnet die
  Prüfung auf der Gruppe.“

## Nachzug N3 (14.10.2026)

- **Befund:** Nach einer Übernahme, Rücknahme oder Nordrichtungs-Bestätigung im Ampelblatt las der Editor der Projektzeile nur
  die Zonen neu. Ein OK danach schrieb die Gebäudefelder aus dem Stand beim Öffnen zurück — zum Beispiel das alte Feiertagsland
  über die Übernahme der Gruppe 4.
- **Ort der Leiste:** Die Leiste sitzt im Reiter „Zonen“ von `GebaeudeKatalogDialog.razor`, und zwar nur in der Betriebsart
  Projekt als Editor der Projektzeile, die `GebaeudeDialog.razor` öffnet. Im Katalog und im Assistenten gibt es keine Leiste.
  Der Gebäudedialog zählt `PruefFassung` hoch, der Editor zieht nach. Der Katalogdialog ist damit der richtige Ort; im
  Gebäudedialog selbst gibt es keine Hüllfelder.
- **Geschriebene Spalten je Gruppe** (`ModellpruefungCtrl.Anwenden`, Rücknahme spiegelbildlich):
  Gruppe 1 (Hüllbauteil) legt eine Zeile in `Tab_Bauteil` an.
  Gruppe 2 (Aufbau) schreibt `Tab_Bauteil.ID_Aufbau` und bei einem Typaufbau auch `U_Wert`. Ein Katalogaufbau legt dazu eine
  Projektkopie in `Tab_Bauteilaufbau` an.
  Gruppe 3 (Profil) schreibt `Tab_Zone.Nutzungsprofil` sowie Kalender, Vorgaben und Wochen der Zone. Dabei wird auch der Stand
  der Konditionierung auf Gebäudeebene neu geschrieben (`RaumnutzungCtrl.ProfilUebernehmen`).
  Gruppe 3 (Ferien/Saison) fügt `Tab_Konditionierungsvorgabe`-Zeilen auf Gebäudeebene ein (`ID_Zone` leer).
  Gruppe 4 (Feiertagsland) schreibt `Tab_Gebaeude.Feiertagsland`; das ist die einzige Spalte von `Tab_Gebaeude`, die eine
  Übernahme schreibt.
  Gruppe 4 (Nordrichtung bestätigen) schreibt `Tab_Bauteil.Azimut` und den Nordwinkel samt Herkunft an der Importquelle.
  Gruppe 4 (Klimaregion wählen) ist im Gebäudedialog nicht verdrahtet und zeigt nur einen Hinweis; die Klimaregion gehört
  ohnehin zum Projekt (`Tab_Projekt.ID_Klimaregion`).
  Die Hüllspalten von `Tab_Gebaeude` (Flächen, U-Werte) schreibt keine Übernahme. Was an den Bauteilen steht, kommt mit dem
  Neulesen der Zonen.
- **Lösung:** Die Hülle `GebaeudeKatalogHuelle.ProjektNeulesen(idGebaeude)` liest die Projektkopie samt Konditionierung der
  Gebäudeebene. `ProjektGaben` reicht sie als Parameter `GebaeudeNeulesen` an den Editor. Bei jeder neuen Prüffassung übernimmt
  der Editor daraus das Feiertagsland und die Konditionierung der Gebäudeebene, danach wie bisher die Zonen.
  Ungespeicherte Änderungen an diesen Feldern (Feiertagsland abweichend vom zuletzt gelesenen Wert oder Fassung der
  Konditionierung > 0) werden nicht überschrieben. In diesem Fall ist die Leiste weich gesperrt, Grund
  `GEB_SPERRE_PRUEFFELDER` (de/en). Ungespeicherte Zonen sperren wie bisher mit `GEB_SPERRE_ZONENNUTZUNG`.
  `ModellpruefungCtrl` bleibt unverändert, neue SQL gibt es nicht.
- **Dateien:** `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor`, `EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs`,
  `Resource(.en-US).resx`, `Resource.Designer.cs`.
- **Tests:** In `GebaeudeKatalogPruefleisteTests` vier Fälle:
  - Nach der Übernahme der Gruppe 4 zeigt der Editor das neue Feiertagsland, und OK schreibt den neuen Wert.
  - Gegenprobe: Ein ungespeichertes Feiertagsland sperrt die Leiste mit Grund und bleibt bei einer neuen Prüffassung stehen.
  - Gegenprobe: Eine geänderte Konditionierung sperrt die Leiste.
  - Ein unveränderter Dialog lässt die Leiste frei.

  Dazu `ModellpruefungDatenbankTests.Gebaeudeeditor_liest_das_Feiertagsland_nach_einer_Uebernahme_neu` (Weg der Hülle gegen
  die Testdatenbank).
  Ergebnis: UI-Filter 346/346 grün, Kern-Filter „Modellpruefung“ 31/31 grün. Build des Kern-Filters und der Windows-Schale mit
  0 Fehlern; Designer im zweiten Lauf +0.
- **Offen:** Die Wochenvorschau der Konditionierung rechnet nach dem Neulesen erst bei der nächsten Handlung neu, weil ihr
  Schlüssel an der Fassung hängt und das Neulesen die Fassung 0 behält. Ferien und Saison ändern die Woche nicht, ein
  übernommenes Profil ändert die Zonen. Windows-Sichtabnahme.
- **Logbuch:** kein eigener Eintrag, weil der Editor nur einen Stand zeigt, den er ohnehin hätte zeigen sollen.
