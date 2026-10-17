# Protokoll MA-2 — Blatt „Nutzung und Kalender“

Welle MA-2 aus dem [Entwurf Geführter Modellaufbau](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md)
(Zielbild Abschnitt 2, Wellenplan Abschnitt 4 Zeile MA-2) auf der Modellprüfung aus
[MA-1a](2026-10-13_MA-1a_Modellpruefung_Kern.md) und [MA-1b](2026-10-13_MA-1b_Modellpruefung_Oberflaeche.md). Grundlage ist
Entscheid E137: F5 (a) Ferien und Heizperiode je Projekt, je Zone nur bei Abweichung; F6 (a) Zone ohne eigenes Profil rechnet
mit dem Profil der Regel. Leitziel: alle Zonen auf einem Blatt, eine Handlung für n Zonen, Einzelheiten danach ohne Wiederimport.

## Ergebnis

- **Kern** `EPOS.Kern/Controller/ZonenNutzungCtrl.cs`: liest je Zone eines Projektgebäudes das Nutzungsprofil (Name, Kategorie
  aus `Tab_Raumnutzungskatalog`, Herkunft Profil/Datei/Vorgabe, bei „Vorgabe“ der Vorschlag der Profilregel aus
  `Modellvorschlagsregeln.Profil`), je Größe die Kalenderkennung (eigen, vom Profil erzeugt, geerbt vom Gebäude, fehlt, entfällt
  bei unbeheizt — „ALLE“ gilt für jede Größe), die Sollwerte Tag und Nacht (`Tab_Zone`), Ferien und Heizperiode
  (Zeilen `FERIEN`/`SAISON` der Vorgabetabelle: an der Zone = Abweichung, am Gebäude oder `Tab_Gebaeude.Ferien` = Projekt) und
  Ampel samt Texten der Befunde der Prüfgruppe 3 (`Modellpruefung`). Der Stand nennt, ob das Projekt Ferien und Heizperiode
  vorgibt (am Gebäude oder an einem anderen Gebäude des Projekts), und die Befunde ohne Zone.
- **Schreibwege** — je einer für n gewählte Zonen in EINEM Vorgang samt `Vorgangsklammer`:
  - `ProfilZuweisen(zonen, idProfil)`: je Zone `ZonenplanCtrl.NutzungUebernehmen` (Generator, Fläche und lichte Höhe der Zone);
    `idProfil = null` nimmt je Zone den Vorschlag der Profilregel („Vorschlag übernehmen“, Stufen bis zum EPOS-Muster).
  - `ProjektvorgabeUebernehmen(zonen)`: die Zeilen `FERIEN`/`SAISON` der Zonen fallen; fehlt die Zeile am Gebäude, kommt sie
    zuvor von einem anderen Gebäude des Projekts (derselbe Weg wie der Vorschlag F5 der Modellprüfung).
  - `KalenderKopieren(quelle, zonen)`: `Konditionierungskopie.Kopieren` Zone → Zone, ersetzend (Kalender, Perioden, Wochen,
    Vorgabezeilen), dazu der Profilname an der Zone.
  - `SollwerteSetzen(zonen, tag, nacht)`: Bereich 5 bis 30 °C, die Nacht nicht über dem Tag; `null` lässt den Wert.
  - `Zuruecknehmen()`: Vor jeder Mehrfachänderung merkt sich die Instanz das Abbild der betroffenen Zonen (drei Spalten an
    `Tab_Zone`, Kalender, Perioden, Wochen, Vorgabezeilen; beim Projektweg dazu die Vorgabezeilen des Gebäudes) und stellt es mit
    den alten Ids wieder her — Sitzungsgedächtnis wie MA-1a, keine Tabelle.
- **Hülle** `EPOS.UI.Daten/Bedarf/ZonenNutzungHuelle.cs` baut den `ZonenNutzungWeg` (`EPOS.UI/Dialoge/Bedarf/ZonenNutzungDaten.cs`);
  Ausnahmen werden zu Meldungen mit neu gelesenem Stand. Plattformfrei, kein SQL.
- **Blatt** `EPOS.UI/Dialoge/Bedarf/ZonenNutzungsblatt.razor`: Kopf mit Gebäudename, Zählung „n von m Zonen gewählt“ und
  Hilfeknopf; Werkzeugleiste „Profil zuweisen…“ (Auswahl Kategorie — Profil), „Vorschlag übernehmen“, „Projektvorgabe
  Ferien/Heizperiode übernehmen“, „Kalender kopieren von…“ (Quellzone), „Sollwerte setzen…“ (Tag, Nacht), „Zurücknehmen“ (nur
  wenn möglich). Ohne Wahl sind die Werkzeuge gesperrt mit Grund. Tabelle mit Kästchen je Zeile und Kopfkästchen, Spalten Zone,
  Fläche, Nutzungsprofil (Auswahl je Zeile weist der einen Zone das Profil zu; Herkunftsmarke, Vorschlag), fünf Kalendermarken
  H/K/L/G/P (Klasse je Kennung, Titel nennt Größe und Kennung), Soll Tag/Nacht, Marken F und HP (Projekt, Abweichung, fehlt),
  Ampel mit den Befundtexten; mit Wirt dazu „Zone…“ in den Zonendialog. Eigener CSS-Abschnitt `epos-zonennutzung-*` vor dem
  Formularraster.

## Einstiege

- **Reiter „Zonen“ des Gebäudeeditors** (Projekt): „Nutzung und Kalender…“ neben „Baustoff-Zuordnungen…“, nur mit Zonen und mit
  dem vom Gebäudedialog durchgereichten Weg (`GebaeudeKatalogDialog.ZonenNutzungGaben`); mit ungespeicherten Zonen weich gesperrt
  („Erst die geänderten Zonen mit OK speichern.“), damit Blatt und Arbeitsstand nicht gegeneinander schreiben. Hat das Blatt
  geschrieben, liest der Reiter beim Schließen die Zonen über `Zonen.Neulesen` neu. „Zone…“ schließt das Blatt und öffnet die
  Zone im Zonendialog.
- **Ampelblatt der Modellprüfung:** Gruppe 3 trägt „Bearbeiten…“ (`Modellpruefungsblatt.Bearbeiten`); der Gebäudedialog öffnet
  das Blatt als Überlagerung über dem Ampelblatt für das geprüfte Gebäude; beim Schließen prüft das Ampelblatt neu
  (`Modellpruefungsblatt.Auffrischen`). Nicht im Assistenten, ohne Delegat kein Knopf.
- **Hülle des Gebäudedialogs:** neuer Parametersatz `ZonenNutzungGaben` (`GebaeudeHuelle`), Projektgebäude wie bei MA-1b.
- **Hilfe:** `help_mapping.txt` Zeile `ZonenNutzung.btn_Help = Gebäude`; Präfix `ZonenNutzung` → Bereich Gebäude in
  `KiChatKontext.BEREICH_JE_HILFEPRAEFIX`. Das Blatt steht nur in Überlagerungen, sein Hilfeknopf setzt `MitAssistent="false"` und
  steht mit Begründung in `KiKnopfEinmalWacheTests.OHNE_ASSISTENT`.

## Dateien

- neu: `EPOS.Kern/Controller/ZonenNutzungCtrl.cs`, `EPOS.UI.Daten/Bedarf/ZonenNutzungHuelle.cs`,
  `EPOS.UI/Dialoge/Bedarf/ZonenNutzungDaten.cs`, `ZonenNutzungsblatt.razor`; Tests `EPOS.Kern.Tests/ZonenNutzungCtrlTests.cs`,
  `EPOS.UI.Tests/Dialoge/ZonenNutzungsblattTests.cs`, `GebaeudeDialogTests.ZonenNutzung.cs`
- geändert: `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor`, `GebaeudeKatalogDialog.razor`, `Modellpruefungsblatt.razor`,
  `EPOS.UI.Daten/Bedarf/GebaeudeHuelle.cs`, `EPOS.UI/wwwroot/epos-ui.css`, `EPOS.Kern/Allgemein/KI/KiChatKontext.cs`,
  `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt`, `EPOS.UI.Tests/KiKnopfEinmalWacheTests.cs`,
  `EPOS.UI.Tests/Dialoge/GebaeudeKuehluebergabeTests.cs` (Editortest), `EPOS.Kern/MyResource/Resource.resx`,
  `Resource.en-US.resx` (je 81 Schlüssel `ZN_*`, `ZNB_*`, `GEB_*ZONENNUTZUNG*`, `MPB_BTN_BEARBEITEN*`), `Resource.Designer.cs`

Kein Schemaschritt, kein Rechenwegbaustein; Basis R52 unberührt. Die Regeln `.epos-raster*` sind unberührt (das Blatt nimmt eine
eigene Tabelle), Kopf und Schlussleiste der Dialoge sind unverändert.

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale mit `EnableWindowsTargeting=true`: 0 Fehler.
- `EPOS.Kern.Tests` mit Filter `ZonenNutzung|Modellpruefung|Zonenplan|Modultrennungswache|KiDialogaufruf|ReferenzprojektWache`:
  178 bestanden, 0 rot (6 neue Fälle gegen die Arbeitskopie, Projekt 1054: Liste je Zone, Profil für zwei Zonen in einem Vorgang
  mit Rücknahme, Ferien/Heizperiode mit Abweichung je Zone und Rücknahme, Kalender kopieren mit Rücknahme, Sollwerte mit Bereich,
  Hülle).
- `EPOS.UI.Tests` mit Filter `ZonenNutzung|Gebaeude|Modellpruefung|KiKnopfEinmal|Formularraster|InfoknopfSchluessel`: 989 bestanden,
  0 rot (8 neue Fälle: Blatt 6, Ampelblatt-Einstieg 1, Editor-Einstieg 1).
- `SqlDialektPruefer`: 0 Fundstellen. `designer_neu.py`: wiederholbar (zweiter Lauf +0).

## Offene Punkte

- **Ferienwerte und Ferienzeitraum:** Die Marke liest die Vorgabezeilen `FERIEN` (Werte an Ferientagen) und `SAISON`; die
  Ferienperioden der Kalender (Art `FERIEN` in `Tab_Konditionierungsperiode`) zählen nicht. „Projektvorgabe übernehmen“ löscht
  die Zonenzeilen `FERIEN` — auch die, die der Generator eines Profils je Größe schreibt. Ob Ferienwerte des Profils als
  Abweichung gelten sollen, ist zu entscheiden (Rückfrage zu F5).
- **Sollwerte bei Kalender:** „Sollwerte setzen…“ schreibt die Spalten an `Tab_Zone`; trägt die Zone einen Heizkalender, gilt
  dieser. Ein Weg, der die Zeilen `TAG`/`NACHT` des Kalenders mitsetzt, steht aus.
- **Kurzbild der Woche je Größe** (Entwurf 2.4) und die Bausteine der Kalenderkarte im Blatt stehen aus.
- Der Sprung „Zone…“ steht nur im Editor; aus dem Ampelblatt fehlt der Zonendialog als Wirt.

## Logbuch-Vorschlag

„Das neue Blatt „Nutzung und Kalender“ zeigt Nutzungsprofil, Kalender, Sollwerte, Ferien und Heizperiode aller Zonen eines
Gebäudes und weist sie mehreren Zonen in einem Schritt zu.“

## Nachzug E139 (13.10.2026)

Entscheid E139 (Anwender, nach Empfehlung): Die Ferienwerte, die der Profilgenerator je Zone schreibt, gelten als Abweichung
von der Projektvorgabe und bleiben erhalten; „Projektvorgabe übernehmen“ fragt vorher und ersetzt sie nur mit Zustimmung.

- **Befund:** Die Marke F las nur die Vorgabezeilen `FERIEN` an der Zone; die Ferienperioden der Zonenkalender (Art `FERIEN`)
  zählten nicht, und „Projektvorgabe übernehmen“ löschte die vom Generator geschriebenen Zeilen ohne Rückfrage.
- **Regel Lesen:** Je Zone `Zonenferien` (Ferienwerte = Zahl der Zeilen `FERIEN` der Zone, Ferienzeiträume = verschiedene Paare
  Beginn–Ende der Ferienperioden ihrer Kalender, abweichend, wenn diese Menge nicht die der Gebäudekalender ist; eine Zone ohne
  Ferienperioden weicht darin nicht ab). Die Marke heißt „Abweichung“ bei eigenen Ferienwerten oder abweichenden Zeiträumen,
  sonst „Projekt“ (Gebäudezeile, `Tab_Gebaeude.Ferien` oder Ferienperioden der Gebäudekalender) oder „fehlt“. Herkunft
  „Profilgenerator“, wenn ein Kalender der Zone die Nutzung eines Profils trägt, sonst „eigen“ (Hand und Datei sind nicht zu
  unterscheiden). Der Titel der Marke nennt Herkunft, Zeiträume und Ferienwerte. Die Heizperiode liest unverändert `SAISON`.
- **Regel Schreiben:** `ZonenNutzungCtrl.FerienRueckfrage(zonen)` nennt die gewählten Zonen mit eigenen Ferien samt Herkunft und
  Zahlen (`null` ohne solche Zone); das Blatt fragt über `Dienste.Dialog.FrageAsync` (warnend, Vorgabe „Nein“).
  `ProjektvorgabeUebernehmen(zonen, ferienErsetzen)`: ohne Zustimmung bleiben diese Zonen ganz unverändert (Meldung nennt ihre
  Zahl), die übrigen übernehmen; mit Zustimmung fallen dazu die Ferienperioden der Zone — je Kalender treten die Ferienperioden
  des Gebäudekalenders derselben Größe an ihre Stelle, ohne solchen Kalender bleiben nur die Perioden mit Zeiträumen des
  Projekts. Rücknahme über das gemerkte Abbild (Kalender, Perioden, Vorgabezeilen).
- **Dateien:** `EPOS.Kern/Controller/ZonenNutzungCtrl.cs` (`Ferienherkunft`, `Zonenferien`, Feld `FerienAngaben` der Zeile,
  `FerienRueckfrage`, `Herkunftstext`), `EPOS.UI/Dialoge/Bedarf/ZonenNutzungDaten.cs` (`ProjektvorgabeUebernehmen` mit Zustimmung,
  `FerienRueckfrage`, `Fragen`), `ZonenNutzungsblatt.razor`, `EPOS.UI.Daten/Bedarf/ZonenNutzungHuelle.cs`, `Resource.resx` und
  `Resource.en-US.resx` (je 7 Schlüssel `ZN_FERIEN_HERKUNFT_*`, `ZN_FRAGE_FERIEN*`, `ZN_MSG_FERIEN_BEHALTEN`,
  `ZNB_FERIEN_ANGABE`), `Resource.Designer.cs`; Tests `EPOS.Kern.Tests/ZonenNutzungCtrlTests.cs` (+2),
  `EPOS.UI.Tests/Dialoge/ZonenNutzungsblattTests.cs` (+3 Fälle). Keine neue Eingabestelle (Eingabebilanz 7 unverändert), kein
  CSS, kein Schemaschritt, keine Leseregel des Rechenwegs; Referenzprojekte unberührt, Basis R52 unberührt.
- **Tests:** `dotnet build WP-Plan.Kern.slnf -c Release` 0 Fehler; Windows-Schale mit `EnableWindowsTargeting=true` 0 Fehler.
  `EPOS.Kern.Tests` Filter `ZonenNutzung|Konditionierung|Dokumentation|Repository|Resource`: 391 bestanden, 0 rot (neu: Marke
  „Abweichung“ bei Generator-Ferien mit Gegenprobe „Projekt“; Übernahme ohne und mit Zustimmung samt Rücknahme).
  `EPOS.UI.Tests` Filter `ZonenNutzung|MA2|KiMaskenabdeckung|VorlagenfeldStilblatt`: 39 bestanden, 0 rot. `SqlDialektPruefer`:
  0 Fundstellen. `designer_neu.py`: zweiter Lauf +0.
- **Logbuch-Satz:** „„Projektvorgabe übernehmen“ im Blatt „Nutzung und Kalender“ fragt nach, bevor eigene Ferien einer Zone —
  auch die eines Nutzungsprofils — ersetzt werden.“

## Nachzug N1 (14.10.2026)

- **Befund:** (a) „Zone…“ fehlte im Blatt, wenn es über dem Ampelblatt stand (kein Wirt). (b) „Sollwerte setzen…“ schrieb nur
  die Spalten an `Tab_Zone`; trägt die Zone einen eigenen Heizkalender, ist dieser die erste Quelle
  (`Konditionierungseingang.ErsteQuelle`) und die Spalten blieben wirkungslos. (c) Das Kurzbild der Woche je Größe stand aus.
- **(a) Ergebnis:** Der Gebäudedialog gibt dem Blatt über dem Ampelblatt `ZoneOeffnen`. Steht der Editor schon (Ampelblatt aus
  seiner Leiste), schließen Blatt und Ampelblatt und der Editor öffnet die Zone (`GebaeudeKatalogDialog.ZoneVonAussenOeffnen`);
  sonst geht der Editor als Überlagerung 6 ÜBER dem Ampelblatt auf (zweite Stelle im Stapel, `_editorOben`) mit
  `StartZone` (Reiter „Zonen“, Zone im Zonendialog). Schließt er, prüft das Ampelblatt neu. Nicht im Assistenten, ohne Editor
  kein Knopf.
- **(b) Ergebnis:** `ZonenNutzungCtrl.SollwerteImKalender` (vor den Spalten, im selben Vorgang): alte Werte Tag/Nacht aus den
  Zeilen `TAG`/`NACHT` (Größe Heizsoll) der Zone mit Wert, sonst aus den Spalten, sonst höchster/tiefster Wert der Woche; in
  jedem Heizkalender der Zone werden Woche, Grundangabe und Perioden mit dem alten Tageswert auf den neuen gesetzt, ebenso der
  Nachtwert (`Kalenderwoche.Schreiben`); „aus“ und andere Werte bleiben. Zeilen `TAG`/`NACHT` mit Wert bekommen den neuen
  Wert, ihre Zeiten bleiben; eine Zeile nur mit Zeiten bleibt. Die Rücknahme stellt Kalender, Perioden und Zeilen über das
  Abbild her.
- **(c) Ergebnis:** `ZonenNutzungszeile.Wochen` je Größe die Standardwoche des geltenden Kalenders (eigen, sonst vom Gebäude
  geerbt; Woche über `Kalenderwoche.Lesen`, sonst Grundangabe in 168 Stunden; „ALLE“ und unlesbare Wochen fehlen). Baustein
  `Wochenkurzbild.razor`: Inline-SVG, je Größe eine Spur mit sieben Tagesblöcken, Balkenhöhe nach Wert, gleiche Stunden eines
  Tages in einem Balken mit Titel „Größe · Tag hh–hh Uhr: Wert Einheit“; „aus“ ohne Balken; keine Farbwerte, Tokens in
  `epos-wochenbild-*`; kein `DiagrammSvg`. Steht in der Kalenderspalte unter den Marken.
- **Dateien:** `EPOS.Kern/Controller/ZonenNutzungCtrl.cs`, `EPOS.UI/Dialoge/Bedarf/ZonenNutzungsblatt.razor`,
  `Wochenkurzbild.razor` (neu), `GebaeudeDialog.razor`, `GebaeudeKatalogDialog.razor`, `epos-ui.css`, `Resource(.en-US).resx`
  (`ZNB_WOCHE_BILD`, `ZNB_WOCHE_TITEL`), `Resource.Designer.cs`.
- **Tests:** `ZonenNutzungCtrlTests.Sollwerte_setzen_traegt_die_Werte_in_den_Heizkalender_der_Zone_mit_Ruecknahme` (1054,
  Gästezimmer: Woche 18/20 → 16,5/21,5, Nachtzeile mit Zeiten bleibt, Rücknahme bitgleich),
  `…Kurzbild_der_Woche_je_Groesse_eigen_und_ohne_Kalender`, `GebaeudeKatalogPruefleisteTests.Kurzbild_der_Woche_…`,
  `GebaeudeDialogTests.N1_Zone_aus_dem_Blatt_ueber_dem_Ampelblatt_oeffnet_den_Editor_darueber`. Die Referenzprojekte werden
  nicht beschrieben (Tests auf der Kopie der Testdatenbank).
- **Offen:** Das Kurzbild zeigt die Standardwoche ohne Perioden (Ferien, Feiertage) und ohne die aus der Matrix abgeleitete
  Woche einer Zone ohne Kalender. Windows-Sichtabnahme der Kalenderspalte mit Kurzbild.
- **Logbuch:** „Das Blatt „Nutzung und Kalender“ zeigt je Zone ein Kurzbild der Woche; „Sollwerte setzen…“ wirkt auch auf den
  Heizkalender einer Zone.“
