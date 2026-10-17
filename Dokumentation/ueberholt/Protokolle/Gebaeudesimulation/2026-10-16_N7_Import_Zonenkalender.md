# N7 — Zonenkalender auf dem Importweg des Dialogs und Feiertage in gbXML

Sitzung Gebäudesimulation, 16.10.2026, Zweig `n7` (von `ios_migration_september` `eab1ee134`, Testdatenbank 237). Kein
Schemaschritt, keine neue SQL-Anweisung im Produktcode, kein Eingriff in den Rechenweg, Referenzprojekte unberührt.

## Befund (Windows-Probe N4)

Probe unter Windows (EPOS Lokal, Kopie der Testdatenbank, Projekt 1045): Export über `GebaeudeExportHuelle` (gbXML und IFC),
Import über die Hülle des Importdialogs (`GebaeudeImportweg`: Lesen, Zuordnen, Prüfen, Übernehmen, Editor speichern,
Aufnehmen, Zeile anlegen).

1. **Zonenkalender gingen still verloren.** Obere Raumtemperatur (26 °C) und benannte Woche kamen an, die Zonenperioden nicht:
   0 Perioden an den neuen Zonen, keine Verlustmeldung. Das gelesene Abbild trug die Kalender.
2. **gbXML schrieb als `Holiday` nur einen Feiertag;** der eingebettete „Tag der Arbeit“ fehlte, IFC führte beide.

## Ursachen

1. `GebaeudeImportHuelle.VorschlagBilden` nimmt die Zonierung des Zonenplans nur mit gespeicherter Planvorlage oder mit
   Planschritten; sonst gilt die Regelzonierung `GebaeudeZonierung.Bilden` ohne Plan. Allein `Zonenplan.AusRegelBilden` setzte
   die Konditionierung der Datei an die Zone (`Planzone.Projektdatei`: EPOS_*-Sätze der IFC, sonst gbXML-/IFC-Zeitpläne über
   `GbxmlKonditionierung.Bilden`); die Regelzonierung trug sie nicht. `GebaeudeZonenCtrl.VorschlagSchreiben` schrieb deshalb das
   Profil statt der Kalender der Datei, die Zonenreste kamen trotzdem an — der Feiertagsverweis fand keine Periode.
   Derselbe Fehler steckte im zweiten Weg: `Zonenplan.AusGespeichert` (Planvorlage des Wiederimports) baute die Zonen aus
   Namen, Nutzung und Paarungen ohne Konditionierung der Datei; der Wiederimport löschte an einer Zone ohne Handmarke ihre
   Kalender und schrieb keine neuen (gefunden mit der Gegenprobe des neuen Tests).
   Der Einzonenweg nimmt die Zeitpläne der Datei nach Entwurf nicht; er verwarf sie aber ohne Meldung.
2. **Formatgrenze, kein Schreibfehler.** gbXML kennt je Wochenplan nur den einen Tagtyp `Holiday`, ohne Datum und ohne Regel des
   einzelnen Feiertags. `GbxmlZeitplanschreiber.Plan` schreibt das Bild des ranghöchsten Feiertags; der Leser setzt es an alle
   neun bundeseinheitlichen Feiertage. Ein Feiertag mit Wochenverweis kommt über den Restdatenschlüssel `Feiertagswoche_<n>`
   (N4) zurück. Verschiedene Bilder einzelner Feiertage trägt gbXML nicht — die Probe sah das Bild eines Feiertags, das
   zweite fehlte zu Recht. Unbenannt war die Grenze: Die Wiki-Seite versprach die vollständige Rückkehr, der Export meldete nichts.

## Lösung

- **Wahl:** Die Regelzonierung trägt die Konditionierung selbst (`GebaeudeZonierung.Dateikonditionierung`, gesetzt am Ende von
  `Bilden` ohne Plan, nur wo noch keine steht), statt `VorschlagBilden` immer über den Zonenplan zu führen. Begründung: Die
  Regelzonierung bleibt die Zonierung des Dialogs ohne Schritte (Namen, Zuschlag nach M8, Vorgaberegel unverändert), und jeder
  Aufrufer von `Bilden` ohne Plan — auch eine Datei mit nur einer wählbaren Regel, die gar keinen Plan bildet — bekommt die
  Kalender. `Zonenplan.AusRegelBilden` nutzt dieselbe Funktion (eine Regel für beide Wege).
- `Zonenplan.AusGespeichert` setzt je Zone die Konditionierung der Datei aus ihren Räumen. Zonen mit Herkunft `MANUELL`
  bewahrt die Zusammenführung des Wiederimports wie bisher (N6).
- Einzonenweg: Warnung `IMP_BAUTEIL_PROT_EINZONE_ZEITPLAENE` an der einen Zone, wenn beheizte Räume EPOS_*-Kalender oder
  gbXML-Zeitpläne tragen (beide Sprachen).
- gbXML: `GbxmlZeitplanschreiber.FeiertageVerschieden`; die Exportvorschau meldet `GEXP_PROT_FEIERTAGE_EIN_BILD` (Info, Zahl
  und Beispiele), wenn Feiertage eines Kalenders verschiedene Tagesbilder tragen. Die Meldung steht in der Vorschau, weil der
  Dialog Meldungen des Schreiblaufs nach erfolgreichem Speichern nicht zeigt.
- Wiki `Projekte/Wiki/Datenexport.wiki` (Kalender, Formatwahl, Rundlauf): ein Tagesverlauf aller Feiertage in gbXML, Hinweis
  der Vorschau, IFC trägt jeden Feiertag einzeln.

## Tests

- Neu `EPOS.Kern.Tests/GebaeudeImportZonenkalenderDialogwegTests` (4): Projekt 1045, Zonen des Probenhauses mit Kalender
  (Neujahr mit Verweis auf „Betriebsruhe“, Tag der Arbeit eingebettet und ranghöher), obere Raumtemperatur 26 °C; Export über
  die Exporthülle, Import über die Hüllenfolge des Dialogs ohne Planvorlage und Planschritte (IFC Regel Z1, gbXML Regel X3) —
  je beheizte Zone Perioden vorhanden, Verweis auf die benannte Woche, Tag der Arbeit eingebettet, 26 °C; gbXML-Vorschau nennt
  die Feiertagsgrenze, IFC nicht. Wiederimport (Mehrzonenhülle, IFC) mit einer Zone `MANUELL`: deren Kalender- und Periodenzeilen
  samt Kennungen unverändert, die Zone ohne Marke nimmt die Kalender der Datei neu. Einzonenweg: Warnung mit Kalender, keine ohne.
- Gegenprobe ohne die Zeile in `GebaeudeZonierung.Bilden`: alle drei Dialogwegfälle rot („Zone ohne Heizkalender“), wie in der Probe.
- Gefiltert (GebaeudeImport, GebaeudeExport, RestdatenRundlauf, Wiederimport, Zonenplan, Gbxml, Ifc, Zonierung,
  Bauteilvorschlag, Mehrzonen, Sqproj, DokumentationLinkWache, WikiProduktdatenWache, ReferenzprojektWache): EPOS.Kern 1 454/0
  (5 übersprungen); dazu Gebaeudedialog, GebaeudeHuelle, Konditionierung, Ressourcen 465/0; EPOS.UI GebaeudeImport/-Export 175/0.
- Kern-Filter und Windows-Schale gebaut (0 Fehler), Designer neu geschrieben, SQL-Dialekt-Prüfer 3 045 Texte, 0 Fundstellen.
- Kein Referenzlauf: Import und Export, nicht der Rechenweg.

## Offen

- Eine mit EPOS-Plan geschriebene gbXML-Datei bot im Dialog die Regel X1 („nach Zone“) nicht an (nur X3, X4); der Test nimmt X3.
  Ob der Export die Zonenzuordnung der Räume für X1 trägt, ist nicht geprüft.
- Anwenderprobe unter Windows wiederholen (Dialogweg, beide Formate, Wiederimport).

## Logbuch-Vorschlag

„Beim Gebäudeimport kommen die Kalender der Zonen aus einer gbXML- oder IFC-Datei auch ohne gespeicherten Zonenplan an.“
