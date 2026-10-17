# N8 — Einzonenkalender, Regel X1 der eigenen gbXML-Datei, Einzonenwarnung in der Meldungsliste

Sitzung Gebäudesimulation, 17.10.2026, Zweig `n8` (von `ios_migration_september` `56eddca88`, Testdatenbank 238). Kein
Schemaschritt, keine neue SQL-Anweisung im Produktcode, kein Eingriff in den Rechenweg, Referenzprojekte unberührt.

## Befund (Windows-Probe N4b nach N7)

Mit mehreren Zonen (gbXML X3, IFC Z1) kamen die Zonenkalender an. Offen blieben:

1. **Einzonenweg** (gbXML X4, IFC Z5): Die eine Zone bekam keine Kalender der Datei, nur die Warnung
   `IMP_BAUTEIL_PROT_EINZONE_ZEITPLAENE`.
2. **Regel X1 fehlte** für eine von EPOS-Plan geschriebene gbXML-Datei (3 `Zone`, 3 `Space` mit `zoneIdRef`); angeboten waren
   nur X3 und X4.
3. **Die Einzonenwarnung** stand nur als schwerste Meldung im Zonierungsstand und bei den Bauteilen, nicht in der allgemeinen
   Meldungsliste des Imports.

## Entscheide

- **E160 (Anwender, 16.10.2026):** Tragen alle Räume der Datei denselben Kalender, übernimmt die eine Zone ihn; weichen sie ab,
  bleibt es bei der Warnung, deren Text auf eine Mehrzonenregel verweist. Festgelegt in N8: **Räume ohne Kalender werden
  übergangen, mindestens ein Raum muss einen tragen.** Begründung: Ein Raum ohne Kalender sagt nichts über den Kalender aus;
  auch der Mehrzonenweg gibt einer Zone die Konditionierung des ersten Raums, der eine trägt (`Dateikonditionierung`), und
  übergeht die übrigen.
- **E161 (N8):** X1 steht zur Wahl, sobald ein Raum der Datei auf eine Zone zeigt — auch bei je einem Raum je Zone. Die
  **Vorgabe bleibt** X1 nur, wenn eine Zone mehrere Räume zusammenfasst (oder die Datei eine Projektdatei ist), sonst X2 bzw.
  X4. Begründung: Angleich an IFC — Z1 wird angeboten, sobald Räume Zonen tragen, die IFC-Vorgabe (Z4/Z5) folgt den Zonen nie;
  die Regel D13 (1:1 ist oft ein Begleitobjekt des CAD-Programms) bleibt für die Vorgabe gültig.

## Ursachen

1. `GebaeudeBauteilvorschlag.ZoneBilden` (Einzonenweg) setzte nie eine Konditionierung der Datei an die Zone
   (`Zonenkonditionierungen` blieb leer) und warnte, sobald ein beheizter Raum Kalender trug — ohne Vergleich der Räume.
2. `GebaeudeZonierung.Waehlbar` bot X1 nur an, wenn die Räume auf **weniger** Zonen zeigten, als es Räume sind (D13). Der Export
   schreibt je Zone einen `Space` mit eigener `Zone` (1:1), also fiel X1 heraus.
3. Die Hülle `GebaeudeImportHuelle.Zuordnen` baute die allgemeine Meldungsliste nur aus den Meldungen des Zuordnungssatzes; die
   Meldungen des Bauteilvorschlags erreichten nur Bauteil- und Zonierungsabschnitt.

## Lösung

- `GebaeudeZonierung.Kalenderschluessel(AbbildRaum)`: Vergleichsschlüssel der Konditionierung eines Raums, sprach- und
  kulturfrei — Nutzung, DIN-Nummer, je Größe Herkunft, Matrixzellen und Kalender (Grundangabe, Perioden mit Rang, Art,
  Bezeichner, Beginn, Ende, Feiertagsregel, Angabe), aus dem Rest benannte Wochen, Periodenverweise (auch Feiertagsverweise) und
  Zeilenbild. Nicht im Schlüssel: Nennwerte (wachsen mit der Raumfläche, die Zone summiert sie) und Bemerkungen. Grundlage ist
  dieselbe Abbildung wie beim Schreiben (`AbbildKonditionierung.AlsZonenkonditionierung` bzw. `GbxmlKonditionierung.Bilden`
  je Raum), also formatfrei.
- `GebaeudeZonierung.Einzonenkonditionierung`: gleiche Schlüssel → `Dateikonditionierung` über alle beheizten Räume; abweichend
  → `null` und Warnung. `ZoneBilden` legt das Ergebnis in `Zonenkonditionierungen`; `GebaeudeZonenCtrl.VorschlagSchreiben`
  schreibt es wie im Mehrzonenweg an die Zone, danach den Rest (Feiertagsverweis auf die benannte Woche). Bei Abweichung reist
  der Rest ohne Wochen, Periodenverweise und Zeilenbild (`GebaeudeRestdaten.OhneKonditionierung`) — sonst kämen die Wochen eines
  einzelnen Raums an. gbXML-Meldungen der Konditionierung (Zähler, Verdichtungen) stehen wie im Mehrzonenweg im Vorschlag.
- Der Gebäudekalender aus gbXML-Lasten im Einzonenweg (DA-1, `GebaeudeImportAblauf.Gebaeudekonditionierung`) bleibt
  unverändert; der Kalender der Zone geht ihm vor.
- `Waehlbar`: X1 bei `zonen >= 1`; Vorgabe wie bisher.
- Hülle: `Uebernahmemeldungen(_vorschlag)` hängt `IMP_BAUTEIL_PROT_EINZONE_ZEITPLAENE` an die allgemeine Meldungsliste.
- Text der Warnung (de/en): „Die Räume der Datei tragen verschiedene Zeitpläne bzw. Kalender; … Wählen Sie eine Zonenregel für
  mehrere Zonen, dann kommen sie je Zone an.“ Designer neu geschrieben.
- Wiki `Programm Dokumentation - Gebäudeimport.wiki`: X1-Bedingung, Vorbelegung bei gbXML, Absatz zum Einzonenweg.
  `Datenexport.wiki` unverändert (keine Aussage betroffen).

**Dateien.** `EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeZonierung.cs`, `GebaeudeBauteilvorschlag.cs`, `GebaeudeRestdaten.cs`,
`EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs`, `EPOS.Kern/MyResource/Resource(.en-US).resx` samt Designer, Test
`EPOS.Kern.Tests/GebaeudeImportZonenkalenderDialogwegTests.cs`.

## Tests

- `GebaeudeImportZonenkalenderDialogwegTests` von 4 auf 9 Fälle (Projekt 1045, Export über die Exporthülle, Import über die
  Hüllenfolge des Dialogs):
  - `Der_Einzonenweg_nimmt_den_gemeinsamen_Kalender_der_Raeume` (gbXML X4, IFC Z5): eine Zone, Perioden vorhanden, Neujahr
    verweist auf „Betriebsruhe“, Tag der Arbeit eingebettet, keine Warnung.
  - `Der_Einzonenweg_warnt_bei_abweichenden_Kalendern_und_nimmt_keinen` (beide Formate; eine Quellzone ohne Perioden): Warnung im
    Vorschlag und genau einmal in der allgemeinen Meldungsliste, keine Feiertagsperiode und keine Woche „Betriebsruhe“ an der Zone.
  - `Die_eigene_gbXML_Datei_bietet_X1_an_und_X1_bildet_die_Zonen_der_Datei`: X1 angeboten und gewählt, Zonenzahl wie im
    Ausgangsgebäude, Kalender je Zone angekommen.
  - `Der_Einzonenweg_benennt_die_nicht_uebernommenen_Zeitplaene` auf E160 umgestellt: gleiche Sätze → keine Warnung, Konditionierung
    an der Zone; ein abweichender Raum → Warnung, keine.
- Gegenprobe ohne Fix: alle fünf neuen Fälle rot (Warnung statt Kalender; Meldung fehlt in der Liste; „Regel X1 ergibt X4 von
  X3, X4“).
- Gefiltert (GebaeudeImport, GebaeudeExport, Zonenplan, Zonierung, RestdatenRundlauf, Wiederimport, Gbxml, Ifc,
  OhneAnwenderdatenbank, DokumentationLinkWache, WikiProduktdatenWache, KiMasken, Bauteilvorschlag, Mehrzonen, Datenaustausch):
  EPOS.Kern 1 311/0 (5 übersprungen); Sqproj, Konditionierung, Gebaeudedialog, GebaeudeHuelle, Ressourcen, Projektdatei 658/0;
  EPOS.UI GebaeudeImport 121/0.
- Kern-Filter und Windows-Schale (`EnableWindowsTargeting=true`) gebaut, 0 Fehler. Kein SQL geändert, daher kein
  SQL-Dialekt-Lauf nötig. Kein Referenzlauf: Importweg, nicht der Rechenweg; kein Referenzprojekt berührt.

## Offen

- Anwenderprobe unter Windows wiederholen (Einzonenweg beider Formate, X1 der eigenen gbXML-Datei, Meldungsliste).

## Logbuch-Vorschlag

„Beim Gebäudeimport als eine Zone übernimmt die Zone den Kalender der Datei, wenn alle Räume denselben tragen; für eine mit
EPOS-Plan geschriebene gbXML-Datei steht die Zonenregel ‚eine Zone je Zone der Datei‘ zur Wahl.“
