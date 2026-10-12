# Protokoll GD-4 — Als Mehrzonenmodell übernehmen

Welle GD-4 aus dem [Entwurf Gebäudedialog](../../../aktuell/Gebaeudesimulation/2026-10-11_Entwurf_Gebaeudedialog_Mehrzonen.md)
(Abschnitte 1.4, 2.1 Punkt 4, 2.4, 4.1, 4.2, Frage F7, Wellenplan Zeile GD-4). Grundlage ist Entscheid E130 F7: „Als
Mehrzonenmodell übernehmen…“ zuerst aus der gespeicherten Importquelle; eine andere Datei ist ein neuer Import. Dazu der
übernommene Sprung „Kühlung“ aus dem Konzept Kältebedarf, Abschnitt 3.5.

## Befund

Für ein schon importiertes Projektgebäude gab es keinen Dialogweg zu mehreren Zonen. „Datei erneut lesen“ (HC-4) zeigte nur
die Ansicht, `VorschlagSchreiben` lehnte ein Gebäude mit Zonen ab, und `ZonenplanCtrl.Gespeichert` wurde nur in Tests gerufen.
Die Kachel „Gebäudekühlung“ öffnete den Gebäudedialog ohne Ziel.

## Ergebnis

- **Kern** (`EPOS.Kern/Controller/MehrzonenuebernahmeCtrl.cs`): `QuellePruefen` hält die gewählte Datei gegen die jüngste
  `Tab_Importquelle` des Gebäudes. Format über die Endung, SHA-256 über den ganzen Inhalt. Die Zustände `KeineQuelle`,
  `DateiFehlt`, `FormatAbweichend` und `PruefsummeAbweichend` sind benannt und tragen eine Meldung; es gibt keinen stillen
  Rückfall. `Ersetzen` schreibt in einem `DbVorgang` mit Vorgangsklammer: Bauteile, dann Zonen des Gebäudes (Luftströme,
  Zonenkalender, -wochen, -vorgaben und Paarungen über die Kaskade), die alten Quellen samt Grundrissen, danach derselbe
  Schreibweg wie beim Anlegen (Baustoffzuordnungen, `VorschlagSchreiben` mit Profil und Kalendern je Zone,
  Gebäudekonditionierung, `SchreibeHerkunft`). Scheitert ein Schritt, rollt der Vorgang zurück. Gebäudefelder und die
  Kalender des Gebäudes ohne Zone bleiben.
- **Hülle** (`EPOS.UI.Daten/Bedarf/GebaeudeMehrzonenHuelle.cs`): Dateiwahl mit dem Namen der Quelle (wie HC-4), Prüfsumme
  im Arbeitsfaden, dann der Parametersatz einer frischen `GebaeudeImportHuelle`. Damit gelten derselbe Zonenbaum, die
  Raumplatzierung (GD-3) und die Vorbesetzung (GD-5). Ihr `Uebernehmen` prüft die Prüfsumme des Laufs ein zweites Mal,
  verlangt „als Zonen mit Bauteilen“ und merkt die Herkunft nur vor. `GebaeudeKatalogHuelle` hängt den Weg an den
  Zonenweg des Projektgebäudes; ein Katalogsatz bekommt keinen.
- **Oberfläche**: Im Übernahmeblock des Gebäudeeditors steht neben „Gebäude als eine Zone übernehmen“ der Knopf „Als
  Mehrzonenmodell übernehmen…“. Er ist weich gesperrt mit Grund, wenn keine Quelle gespeichert ist, am Katalogsatz, bei
  geänderten Zonen und während des Lesens. Eine Ablehnung der Prüfung steht als Meldung. Sonst tauscht ein `Blattwechsel`
  den Editor gegen den `GebaeudeImportDialog`. Der Dialog ist neu mit `TitelAnzeigen` (Titel und Kreuz trägt das Blatt) und
  `Vorgelesen` (liest die geprüfte Quelle ohne Dateiwahl). Abbrechen, Rückknopf und Esc merken nichts vor. Nach dem OK des
  Blatts nennt eine leise Zeile das vorgemerkte Modell. Erst das OK des Gebäudeeditors ersetzt in einem Vorgang, statt des
  Zonenabgleichs, und liest die Zonen danach neu.
- **Sprung Kühlung**: `Sprungziele.Kuehlung` im Kern (`EPOS.Kern/Allgemein/Dienste/Sprungziele.cs`). `GebaeudeDialog`
  nimmt den Parameter `Sprungziel`, wählt das erste Projektgebäude und öffnet seinen Editor mit dem Ziel. Der Editor stellt
  den Reiter „Gebäude und Hülle“ ein, und `epos-sprung.js` rollt die Gruppe „Kühlung“ in Sicht und fokussiert ihr erstes
  Feld. Ohne Projektgebäude steht der Hinweis „Noch kein Gebäude im Projekt.“. Die Kachel „Gebäudekühlung“ reicht das Ziel
  über `GebaeudeFenster.Oeffnen` durch. Die Zustandszeile der Kachel bleibt unverändert.
- 16 Ressourcen in beiden Sprachen (`GEBMZ_*`, `GEB_SPRUNG_KEIN_GEBAEUDE`), `Resource.Designer.cs` neu erzeugt.

Kein Schemaschritt, Rechenweg unberührt (kein Referenzlauf nötig). Die Zeile „Jahreswärmebedarf“ der Gebäudezusammenfassung
ist nicht Teil dieser Welle.

## Dateien

- Kern: `EPOS.Kern/Controller/MehrzonenuebernahmeCtrl.cs` (neu), `EPOS.Kern/Allgemein/Dienste/Sprungziele.cs` (neu), beide
  `.resx`, `Resource.Designer.cs`.
- Hülle: `EPOS.UI.Daten/Bedarf/GebaeudeMehrzonenHuelle.cs` (neu), `GebaeudeKatalogHuelle.cs`.
- Oberfläche: `EPOS.UI/Dialoge/Bedarf/GebaeudeZonenDaten.cs` (`GebaeudeMehrzonenweg`, `GebaeudeMehrzonenstart`),
  `GebaeudeKatalogDialog.razor`, `GebaeudeDialog.razor`, `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor`,
  `EPOS.UI/wwwroot/epos-sprung.js` (neu).
- Windows-Schale: `WindowsFormsApplication1/Views/Gebäude/GebaeudeFenster.cs`, `Views/Hauptformular/StartseiteHuelle.cs`.
- Tests: `EPOS.Kern.Tests/MehrzonenuebernahmeTests.cs` (3 Fälle), `EPOS.UI.Tests/Dialoge/GebaeudeMehrzonenDialogTests.cs`
  (6 Fälle).

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale mit `EnableWindowsTargeting=true`: 0 Fehler.
- Kern-Test: Prüfsumme passend, abweichend (ein Byte mehr), Datei fehlt, anderes Format, keine Quelle. Die Hülle lehnt eine
  abweichende Prüfsumme ohne Blatt ab. Ersetzen in einem Vorgang an der Projektkopie von 1045 in der Testdatenbank: Vormerken
  schreibt nichts, das Schreiben legt drei Zonen samt Bauteilen und eine Quelle an. Ein zweiter Lauf ersetzt sie (neue Ids,
  gleiche Bauteilzahl, eine Quelle, alle Spalten von `Tab_Gebaeude` gleich). Der Rückweg: Die Herkunft scheitert nach Löschen
  und Schreiben der Zonen, der Vorgang rollt zurück, und Zonen, Bauteile und Quelle bleiben. Abgelehnt ohne Vorschlag und an
  einer fremden Id.
- bunit: Knopf gesperrt ohne Quelle (Titel, Meldung beim Versuch, Katalogsatz, ohne Weg); Prüfsummenmeldung ohne Blatt;
  Blatt mit Importdialog ohne Titel, Abbrechen merkt nichts vor und schreibt nichts; OK des Gebäudes schreibt das vorgemerkte
  Modell statt des Zonenabgleichs; Sprung Kühlung (Reiter, Gruppe, Editor des ersten Gebäudes, Hinweis ohne Gebäude).
- Tests mit Filter (Gebaeude, Zonen, Zonenplan, Sqproj, Ifc, Gbxml, Kuehl, Sprung, Ki, Wache, Ressource/Resource,
  Mehrzonen, Schliesskreuz, Fokus) mit den xUnit-Schaltern: `EPOS.Kern.Tests` 3 889 bestanden, 6 übersprungen, 0 rot;
  `EPOS.UI.Tests` 1 881 bestanden, 0 rot.
- `SqlDialektPruefer`: 2 863 SQL-Texte, 0 Fundstellen.
- `fensterprobe.mjs` (Wirt Release, Chromium headless): „FENSTERPROBE ERFUELLT (Gegenprobe rot)“, Rückgabe 0. Der Kopf
  des Importdialogs bleibt ohne `TitelAnzeigen=false` unverändert.

## Windows-Sichtabnahme

1. Projektgebäude aus einer Projektdatei, Editor „Hülle und Zonen…“: Der Knopf „Als Mehrzonenmodell übernehmen…“ steht neben
   „Gebäude als eine Zone übernehmen“ (Reiter „Gebäude und Hülle“ unter dem Rechenweg und im leeren Reiter „Zonen“).
2. Ein Gebäude ohne Import: Knopf grau, der Tooltip nennt „Kein Import gespeichert …“; der Klick meldet den Grund.
3. Dateiwahl: Der Titel nennt den gespeicherten Dateinamen. Eine geänderte Datei oder eine andere Datei gleicher Endung
   ergibt die Prüfsummenmeldung, kein Blatt.
4. Passende Datei: Das Blatt „Mehrzonenmodell aus der Importquelle“ zeigt den Zonenbaum sofort gelesen, ohne zweiten Titel
   und ohne zweites Kreuz. Rückknopf und Esc führen zum Editor zurück, ohne Vormerkung.
5. OK im Blatt („als Zonen mit Bauteilen“): Der Editor nennt in einer leisen Zeile das bereitstehende Modell. Abbrechen des
   Editors schreibt nichts; OK ersetzt Zonen, Bauteile und Zonenkalender, die Zonenliste zeigt danach die neuen Zonen.
6. Kachel „Gebäudekühlung“: Der Gebäudedialog öffnet den Editor des ersten Gebäudes, die Gruppe „Kühlung“ steht oben im
   Bild, ihr Schalter hat den Fokus. In einem Projekt ohne Gebäude steht der Hinweis „Noch kein Gebäude im Projekt.“.

## Offen

- Der Zonenbaum startet mit der Vorgabe der Datei (Regel der Quelle, Zonen der Projektdatei, GD-3, GD-5). Der gespeicherte Plan
  eines früheren Imports (`ZonenplanCtrl.Gespeichert`) wird noch nicht vorgelegt.
- Beim Weg „Gebäudedatei + Projektdatei“ wird nur die Gebäudedatei geprüft und vorgelesen. Die Projektdatei wählt der
  Anwender im Blatt wie beim Import.
- Der Sprung wählt das erste Projektgebäude; das zuletzt gewählte merkt sich der Dialog über das Öffnen hinaus nicht.
- Ein Maskenschlüssel für die Projektgebäude (`OeffneMaske(Masken.…, Sprungziele.Kuehlung)`) fehlt. Er zöge Seitenschlüssel,
  `AppWurzel` und beide Navigationsadapter nach. Der Sprung läuft über die Kachel der Windows-Startseite.
- Auf iOS ist der Weg derselbe (Dateiwahl der Schale), aber nicht im Gerät abgenommen.
