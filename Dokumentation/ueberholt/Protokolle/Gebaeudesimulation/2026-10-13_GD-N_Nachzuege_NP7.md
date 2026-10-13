# Protokoll GD-N — Nachzüge aus GD-4 und NP7

Welle GD-N der Sitzung „Gebäudesimulation“: die offenen Punkte der Welle GD-4 (Statuszeile #1026, [Protokoll
GD-4](2026-10-12_GD-4_Mehrzonenuebernahme.md)) und der Nachzug aus NP7 (Statuszeile #1022, [Protokoll
NP7](2026-10-12_NP7_Normwerte_einlesen.md)), dazu der Jahreswärmebedarf in der Zusammenfassung des Gebäudedialogs aus dem
[Konzept Projektdialoge](../../../aktuell/Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md). Grundlage sind die
Anwenderentscheide vom 13.10.2026 („nach Empfehlung“).

## Befund

- „Als Mehrzonenmodell übernehmen…“ startete den Zonenbaum mit der Vorgabe der Datei. Der gespeicherte Plan eines früheren
  Imports (`ZonenplanCtrl.Gespeichert`) wurde nur in Tests gerufen.
- Beim Weg „Gebäudedatei + Projektdatei“ wurde nur die Gebäudedatei geprüft. Die Prüfsumme der Projektdatei speichert der
  Import nicht (`Tab_Importquelle` trägt beim Weg „IFC + Projektdatei“ nur die IFC-Datei).
- Der Sprung „Kühlung“ wählte das erste Projektgebäude. Für die Projektgebäude gab es keinen Maskenschlüssel; der Sprung lief
  allein über die Kachel der Windows-Startseite.
- Die Zusammenfassung des Gebäudedialogs nannte keinen Jahreswärmebedarf. `Tab_ErgebnisGebaeude` führt `Heizwaerme_Mwh` je
  Gebäude, aber nur für Gebäude mit Gebäudemodell. Je Projekt steht `Tab_ErgebnisEnergiebedarf.Waermebedarf_Heizung`.
- Beim Einlesen der Normwerte aus der Projektdatei blieben die Nummern 70 und 71 unbekannt. Die Wohnprofile trafen nur über
  den Namen.

## Ergebnis

1. **Gespeicherter Zonenplan**: `ZonenplanCtrl.GespeichertJeGebaeude` nimmt die jüngste Importquelle des Gebäudes, deren
   Raumpaarungen auf Zonen zeigen. Daraus baut es mit den Zonen des Gebäudes in ihrer Reihenfolge, Bezeichner und Nutzung
   den Plan (`Zonenplan.AusGespeichert`). Weicht die Prüfsumme der gewählten Datei ab, wird der Plan trotzdem über die
   Raumkennungen vorgelegt, die die Datei noch führt, und die Abweichung wird benannt. Die Hülle des Imports nimmt den Plan
   über `GebaeudeImportHuelle.Planvorlage` statt des Regelvorschlags. Das gilt, solange keine andere Regel gewählt und nichts
   von Hand umgehängt ist; die Schritte des Zonenbaums setzen darauf auf. Der Dialog zeigt über dem Zonenbaum die Zeile der
   Vorbelegung (Datei, Zeitpunkt, Räume, Zonen, verlorene Paarungen), bei abweichender Prüfsumme dazu eine Warnung.
   `GebaeudeMehrzonenHuelle` setzt die Vorlage.
2. **Projektdatei-Befund**: Mit `GebaeudeImportHuelle.ProjektdateiPruefen` (gesetzt von der Mehrzonenhülle) trägt der Stand
   der Projektdatei einen Prüfbefund mit vier Zeilen: vorhanden bzw. nicht zu öffnen, lesbar mit Fassung bzw. Grund,
   Zuordnung zur Gebäudedatei (zugeordnete Räume, ohne Treffer, ohne Gegenstück; keiner = gehört nicht dazu) und die
   Prüfsumme (SHA-256, kurz). Weil der Import sie nicht speichert, nennt die Zeile das ausdrücklich. Der gewöhnliche Import
   prüft nicht.
3. **Sprung Kühlung**: Der Gebäudedialog merkt das gewählte Projektgebäude über die Gaben `ZuletztGewaehlt` und
   `GewaehltMerken`. Die Hülle hält es je Projekt in `Dienste.Einstellungen` (`Gebaeudedialog_Zuletzt_<Projekt>`). Der Sprung
   nimmt das zuletzt gewählte Gebäude, sonst das erste. Neuer Maskenschlüssel `Masken.Projektgebaeude` mit dem Sprungziel als
   Argument; `Seitenschluessel.Projektgebaeude` verweist darauf. Unter Windows öffnet `WinFormsNavigation` ihn über
   `GebaeudeFenster.ProjektOeffnen`. Das ist zugleich der Weg der Kacheln „Gebäude“ und „Gebäudekühlung“ (Fenster, danach
   Abgleich in einem Vorgang). Die Wurzel führt den Schlüssel und lehnt ihn benannt ab, weil auf iOS der Assistent die
   Gebäude pflegt. **Die iOS-Hülle ist berührt**: `EPOS.iOS/Dienste/IosNavigation.cs`, nur der Kommentar zum unübersetzt
   durchgereichten Schlüssel; nicht gebaut.
4. **Jahreswärmebedarf**: `GebaeudeJahresbedarfCtrl.Letzter` liest den letzten Lauf des Projekts (`Tab_Ergebnis`, höchste
   ID) und darin `Tab_ErgebnisGebaeude.Heizwaerme_Mwh` des Gebäudes. Ohne Zeile je Gebäude nimmt es den Projektwert
   (`Waermebedarf_Heizung`, sonst `Waermebedarf_Gesamt`) mit dem Hinweis „Projekt gesamt“. Die Hülle reicht ihn als Gabe
   `Jahreswaermebedarf`. Die Zusammenfassung des Projektsatzes zeigt „12,3 MWh/a (Lauf vom 30.08.2026)“ bzw. „noch nicht
   gerechnet“.
5. **NP7-Nachzug**: `RaumnutzungNormwerte.Zuordnen` bildet beim Einlesen (CSV wie Projektdatei) eine Nummer ohne
   DIN-Profil über `WohnprofilAusNummer` ab: 70 auf „Wohngebäude – Einfamilienhaus“, 71 auf „Wohngebäude –
   Mehrfamilienhaus“. Die Raumzuordnung auf das Muster Wohnen (`Din18599Nutzung`, `Gebaeudeverwendung.WohnenAusDinNummer`)
   bleibt unverändert.

15 Ressourcen in beiden Sprachen (`GEBMZ_PLAN_*`, `GEBMZ_SQ_*`, `GEB_ZF_JAHRESWAERME`, `GEB_ZF_JWB_*`,
`WURZEL_PROJEKTGEBAEUDE_NICHT`), `Resource.Designer.cs` neu erzeugt. Kein Schemaschritt, Rechenweg unberührt.

## Dateien

- Kern: `EPOS.Kern/Controller/ZonenplanCtrl.cs`, `GebaeudeJahresbedarfCtrl.cs` (neu), `RaumnutzungNormwerte.cs`,
  `EPOS.Kern/Allgemein/Dienste/Masken.cs`, `Sprungziele.cs`, beide `.resx`, `Resource.Designer.cs`.
- Hüllen: `EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs`, `GebaeudeMehrzonenHuelle.cs`, `GebaeudeHuelle.cs`.
- Oberfläche: `EPOS.UI/Dialoge/Import/GebaeudeImportDaten.cs`, `GebaeudeImportDialog.razor`,
  `EPOS.UI/Dialoge/Bedarf/GebaeudeDaten.cs`, `GebaeudeDialog.razor`, `EPOS.UI/Seiten/Seitenschluessel.cs`, `AppWurzel.razor`.
- Windows-Schale: `WindowsFormsApplication1/Dienste/WinFormsNavigation.cs`, `Views/Gebäude/GebaeudeFenster.cs`,
  `Views/Hauptformular/StartseiteHuelle.cs`.
- iOS-Schale: `EPOS.iOS/Dienste/IosNavigation.cs` (Kommentar).
- Tests: `EPOS.Kern.Tests/ZonenplanVorlageTests.cs` (neu, 2 Fälle), `RaumnutzungNormwerteTests.cs` (1 Fall neu, 1 angepasst),
  `DiensteTests.cs` (Schlüsselliste), `EPOS.UI.Tests/Dialoge/GebaeudeNachzugDialogTests.cs` (neu, 3 Fälle),
  `GebaeudeImportZonenDialogTests.Nachzug.cs` (neu, 2 Fälle).

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale mit `EnableWindowsTargeting=true`: 0 Übersetzungsfehler.
  `EPOS_Plan.dll` ist neu übersetzt; das Kopieren der Laufzeitdateien nach `bin` scheiterte am vollen Datenträger des
  Prüfrechners (MSB3021).
- `EPOS.Kern.Tests`, Filter Zonenplan, Raumnutzung, Normwerte, DiensteTests, Mehrzonen: 246 bestanden, 0 rot. Dazu gehören
  der Plan aus dem ersten Import, die Vorlage im zweiten Lauf mit den gespeicherten Zonennamen, die Abweichung der
  Prüfsumme, der Prüfbefund mit fehlender und vorhandener Datei sowie 70/71 über CSV und Projektdatei.
- `EPOS.UI.Tests`, Filter Gebaeude, Sprung, Menue, Seitenschluessel, Katalogeinstieg: 1 015 bestanden, 0 rot. Dazu gehören
  die Zeile der Vorbelegung mit und ohne Abweichung, der Prüfbefund im Block der Projektdatei, der Sprung auf das gemerkte
  Gebäude (ein unbekanntes ergibt das erste), Wert und Datum bzw. „noch nicht gerechnet“ sowie Seitenschlüssel und Wurzel.
  `GebaeudeImportDialogTests.AB3_Die_Wahl_an_der_Schicht…` war in einem ersten Lauf einmal rot und danach zweimal grün
  (nicht berührt).
- `SqlDialektPruefer`: 2 893 SQL-Texte, 0 Fundstellen.
- `designer_neu.py`: wiederholbar, zweiter Lauf ohne Abweichung.

## Windows-Sichtabnahme

1. Ein Projektgebäude mit Mehrzonenimport: „Als Mehrzonenmodell übernehmen…“ mit derselben Datei. Über dem Zonenbaum steht
   „Vorbelegt mit dem gespeicherten Zonenplan aus …“, und die Zonen tragen die gespeicherten Namen.
2. Weg „IFC + Projektdatei“: Nach „Projektdatei dazuladen“ steht im Block der Projektdatei der Prüfbefund mit vier Zeilen.
3. Gebäudedialog mit zwei Gebäuden, das zweite wählen und schließen. Die Kachel „Gebäudekühlung“ öffnet den Editor des
   zweiten Gebäudes an der Gruppe „Kühlung“.
4. Zusammenfassung eines Projektsatzes: Nach einem Rechenlauf steht „Jahreswärmebedarf … MWh/a (Lauf vom …)“, ohne Lauf
   „noch nicht gerechnet“.
5. „Normwerte einlesen…“ mit einer Projektdatei, deren Wohnzeilen 70/71 Werte tragen: Beide Wohnprofile stehen unter
   „Treffer“.

## Offen

- Die Prüfsumme der Projektdatei wird beim Import nicht gespeichert. Ein Vergleich mit der Datei des ersten Imports bräuchte
  eine Spalte (Schemaschritt) und ist nicht Teil dieser Welle.
- Auf iOS öffnet der Maskenschlüssel `Masken.Projektgebaeude` keinen Gebäudedialog (benannte Ablehnung). Eine freie Ansicht
  der Wurzel wäre ein eigener Auftrag.
- Der Projektwert des Jahreswärmebedarfs gilt für alle Gebäude des Projekts zusammen; der Hinweis nennt das.
- iOS nicht gebaut und nicht im Gerät abgenommen; Windows-Sichtabnahme.
