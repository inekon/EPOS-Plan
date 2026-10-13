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
