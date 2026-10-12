# Protokoll GD-2 — Gebäudewerte der Projektdatei, Name = Dateiname

Welle GD-2 aus dem [Entwurf Gebäudedialog](../../../aktuell/Gebaeudesimulation/2026-10-11_Entwurf_Gebaeudedialog_Mehrzonen.md)
(Abschnitte 1.3, 2.2, 4.1, 5.1), Entscheide F4 und F8 des Anwenders vom 12.10.2026 nach Empfehlung.

## Umgesetzt

- **F4, Leser:** `SqprojLeser` liest an `BmBuilding` `BuildingType`, `BuildingHeatInsulationStandardType`,
  `BuildingEnergyEfficientType` und `Cwirk`, an `BmBuildingData` (optional, über die Kennung des Gebäudes)
  `BuildingUsageType`. Das Baujahr wird wie bisher gelesen. Die Projektdatei hat keine `HSETU_*`-Sätze. Diese stehen nur
  im IFC-Export.
- **F4, Abbildung:** `SqprojGebaeudecodes` hält die festen Tabellen im Kern (Muster `Din18599Nutzung`): Gebäudetyp → Gebäudeart,
  Gebäudetyp und Nutzung → Verwendung, Effizienz- bzw. Wärmeschutzstandard → `Energiestandard.CODES`. Die Tabellen enthalten nur
  belegte Codes. Code 0 heißt „keine Angabe“ und erzeugt keine Meldung. Jeder andere Code erscheint benannt
  (`IMP_GEB_PROT_GEBAEUDECODE_OHNE_ABBILDUNG`). Die Datei trägt keine Klartexte zu ihren Codes (`XmFields` ist leer).
  Belegt ist nur Gebäudetyp 3 → Nichtwohngebäude: An der Belegdatei ergeben die Zonen allein nach der Fläche ebenfalls
  „Nichtwohngebäude“. Für Gebäudeart und Energiestandard ist kein Code belegt.
- **Vorrang:** Die Verwendung der Datei geht der Ableitung aus Gebäudeart, Zonen und Räumen vor
  (`GebaeudeImportAblauf.Verwendung`, Herleitung `IMP_GEB_PROT_VERWENDUNG_PROJEKTDATEI`). Gebäudeart und Energiestandard der
  Datei gehen der Vorgabe des neuen Satzes vor (`GebaeudeImportHuelle.GebaeudewerteSetzen`).
- **Cwirk → Bauweise:** Einheit Wh/(m²K), flächenbezogen nach DIN V 18599-2. Die Belegdatei trägt einen der drei Vorgabewerte
  der Norm, die Zonenzeilen tragen den Platzhalter „nicht gesetzt“. Die Bauweise ergibt sich als Cwirk × Nutzfläche in Wh/K.
  Herkunft „Projektdatei“, Haken gesetzt, Beleg `GIMP_BELEG_BAUWEISE_CWIRK`. Cwirk geht der Ableitung aus den Schichten vor.
  Die Bauart folgt nur als nächstliegende Klasse nach `Gebaeudebauweise` (Schwellen 30 und 75 Wh/(m²K)). Das Band 5 bis
  200 Wh/(m²K) warnt. Der Gebäudeeditor führt eine Bauweise aus der Datei im Modus Neu nicht der Bauart nach
  (`GebaeudeKatalogDaten.BauweiseAusDatei`, nicht gespeichert). Eine Änderung von Bauart oder Nutzfläche führt sie wieder nach.
- **Eine Quelle der Baualtersklasse:** `Baujahrregel.KlasseWirksam` gibt die ausdrückliche Wahl zurück, sonst die Klasse des
  Baujahrs, sonst die Vorgabe. Diese Regel nehmen die Zuordnung (`GebaeudeAggregation.Bilden`, Kopf des Imports) und die
  Vorbelegung des Editors (`NachKatalogdaten`, mit dem übernommenen Baujahr). Bisher setzte die Vorbelegung die Klasse aus der
  Zeile und das Baujahr getrennt. Ein im Import von Hand geändertes Baujahr ergab deshalb Klasse H neben dem Vorschlag
  „1995 bis 2001“ im Editor. An der Belegdatei liegt das Baujahr in Klasse H. Kopf und Editor zeigen jetzt beide H.
- **F8, Name:** Der Name des neuen Gebäudes ist bei IFC, gbXML und Projektdatei der Dateiname ohne Endung
  (`GebaeudeZuordnungsModell.Vorschlagsname`). Den Gebäudenamen der Datei liefert `NameDerDatei`, außer er ist leer, ein
  Platzhalter oder gleich dem Dateinamen. Der Importdialog zeigt ihn als Herleitungszeile „Name in der Datei“ mit dem Knopf
  „Übernehmen“.
- **Schema:** Es gibt keinen Schemaschritt. Die Herkunft der Gebäudefelder wird nicht neu gespeichert. Sie steht in der
  Feldzeile des Imports und im Protokoll. Das genügt für diese Welle (Entwurf 5.1, Weg offen für eine spätere Welle).

## Zählungen an der Belegdatei (Projektdatei des Anwenders, nicht im Repositorium)

| Größe | Befund |
|---|---|
| Gebäude in `BmBuilding` | 1 |
| belegte Gebäudefelder | Gebäudetyp, Nutzung, Wärmeschutzstandard, Effizienzstandard, Cwirk, Baujahr: je 1 von 1 |
| Codes Typ / Nutzung / Wärmeschutz / Effizienz | 3 / 0 / 0 / 0 |
| abgebildet | Verwendung 1 (aus Typ 3), Gebäudeart 0, Energiestandard 0 |
| Codes ohne Abbildung | 0 |
| Cwirk | im Band, Bauart „schwer“, Bauweise-Zeile mit Herkunft Projektdatei und Haken |
| Zonen mit gesetztem `Cwirk` | 0 von 34 |
| Baualtersklasse | aus dem Baujahr der Datei, Klasse H |
| Name | Dateiname, kein abweichender Gebäudename als Vorschlag |

## Nachweis

- Kern-Filter und Windows-Schale (`-p:EnableWindowsTargeting=true`) bauen mit 0 Fehlern.
- `SqprojGebaeudewerteTests` (neu) mit Proben aus `SqprojProbenErzeuger`, das um die Gebäudecodes und `BmBuildingData` erweitert
  ist. Die Tests decken ab: eine Probe mit Codes, eine Probe ohne, unbelegte Codes, eingesetzte Tabellen, Cwirk mit den Bauarten
  leicht, schwer und sehr schwer, den Vorrang der Verwendung, die Klassenregel in Zuordnung und Vorbelegung, Name = Dateiname bei
  gbXML, IFC und Projektdatei sowie den Editor, der die Bauweise hält. Dazu eine Diagnose an der Belegdatei, die nur Zählungen
  ausgibt.
- Die Erwartungen in `IfcImportTests`, `IfcReferenzenTests` und `GebaeudeImportHuelleTests` sind auf F8 nachgezogen.
- Der `SqlDialektPruefer` ist ohne Fundstelle.
- Kein Referenzlauf: Kein Referenzprojekt ist importiert, der Rechenweg ist unberührt.
