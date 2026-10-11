# Protokoll K-C — Importvarianten der Kältemaschine (Nennwerte, Ökodesign A–D) und Menüpunkt

**10.10.2026** · Sitzung Gebäudesimulation · Zweig `gs-kc` (K-A und VDI-K/VDI-K2 zusammengeführt) · Grundlage: Stufe K-C
der [Konzeptprüfung Kälteanlagen](../../../aktuell/Kälteanlagen/2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md),
Abschnitt Teillastpunkte A–D und Datenwege im Entwurf K-D (Zweig `gs-kd`), Anwenderentscheid E118. Kein Schemaschritt,
keine Basiswirkung.

## Formen der CSV-Vorlage

Die Importart `KatalogImportArt.Kaeltemaschine` liest die CSV-Vorlage in drei Formen. Die Kopfzeile `Form`
(`KENNFELD`, `NENNWERTE`, `OEKODESIGN`) entscheidet; ohne sie erkennt die Weiche die Form am Inhalt: Kennfeldzeilen →
Kennfeld, Punktzeilen ohne Kennfeld → Ökodesign, sonst Nennwerte. Ein Gerät ohne Kennfeld, Nennwerte und Punkte wird
benannt übergangen (vorher lief es mit leerer Kennlinie durch). Die Form steht als Quelle in der Liste: `CSV`,
`CSV Nennwerte`, `CSV Ökodesign A–D`.

| Form | Pflicht | Kennfeld | Teillast |
|---|---|---|---|
| Kennfeld (KM1) | Rasterzeilen | aus der Datei | Teillastzeilen; Punkte A–D, wenn vorhanden, gegen dieses Kennfeld |
| Nennwerte | Nennkälteleistung, Nenn-EER | Typkennfeld am Eurovent-Nennpunkt (Kaltwasser 7 °C; Luft 35 °C + 5 K bzw. Kühlwasser 30 °C) auf die Nennwerte skaliert | vom Typkennfeld, wenn die Datei nichts angibt |
| Ökodesign A–D | Punkte, Kaltwassertemperatur der Prüfung, bei Wasserkühlung je Punkt die Rückkühltemperatur | Typkennfeld am Punkt A auf dessen P_dc und EER_d skaliert; Nennwerte danach am Nennpunkt abgelesen | aus den Punkten |

Neue Kopfschlüssel: `Form`, `Pdesignc`, `Verdichter` (`SCROLL`, `SCHRAUBE`, `TURBO`, `HUBKOLBEN`),
`Kaltwassertemperatur`, `Cdc` (Alias des Taktverlustfaktors). Punktzeile:
`Punkt; Name; Tj; Pdc; EERd; Teillastverhältnis %; Rückkühltemperatur` (die letzten beiden optional; luftgekühlt
gilt ohne Angabe Tj + 5 K, der Bezug des Kennfelds aus KM1). Fehler stehen je Zeile („Zeile n: Teillastpunkt B
unvollständig …“) bzw. je Gerät („Bezeichner: Form OEKODESIGN braucht die Kaltwassertemperatur der Prüfung“).

## Wahl und Skalierung des Typkennfelds

`KaeltemaschineImportVarianten.TypkennfeldWaehlen`, Rangfolge: gleiche Rückkühlart (sonst gleiche Klasse Luft/Wasser —
eine Luftkurve passt nie an eine Wasserachse; ohne passende Klasse wird das Gerät übergangen), gleicher Verdichter
(falls angegeben), gleiche Drehzahlart (`DREHZAHL` ↔ drehzahlgeregelt; ohne Angabe die ungeregelte), nächstliegende
Leistungsklasse im logarithmischen Abstand, Folge der Ressource. Begründung: Die Form der Kennlinie hängt an
Rückkühlart und Verdichter, kaum an der Größe. Skaliert werden Kälteleistung und EER mit je einem Faktor, gemessen
bilinear wie in der Simulation am Bezugspunkt; Hinweis und — wenn leer — Beschreibung nennen Typkennfeld und Faktoren.

## Abbildung A–D (KM3)

`OekodesignPunkteLeser.Teillast`: je Punkt Q_VL und EER_VL des Kennfelds bei Tj (bzw. Rückkühltemperatur) und
Kaltwassertemperatur; Lastgrad x = min(1, P_dc / Q_VL), EER-Verhältnis g = EER_d / EER_VL. Ab drei verschiedenen
Lastgraden die Kurve nach kleinsten Quadraten (`KaeltemaschineTeillastkurve.AusEerVerhaeltnis`), normiert, auf Bereich
und Plausibilität geprüft, x_u = kleinster Lastgrad, Weg `KURVE`. **Takten:** P_dc um mehr als 2 % über der Last der
Prüfung (Teillastverhältnis × Pdesignc) → der Punkt taktet; er zählt mit seinem Dauerwert zur Kurve, die kleinste
solche Leistung (bezogen auf die Nennkälteleistung) wird die Mindestteillast, den Verlust trägt `Cdc`. Keine Kurve
ableitbar, aber taktende Punkte → Weg `LINEAR` mit Taktverlust.

**Grenzen.** Aus A–D lassen sich Temperatur- und Teillasteinfluss nicht trennen; die Temperaturabhängigkeit kommt aus
dem Typkennfeld, der Rest geht in die Teillastkurve. Abseits von Punkt A und dem Nennpunkt gilt die Form des Typs. SEER
und ηs,c bleiben Angabe. Ohne Pdesignc wird das Takten nicht beurteilt (Hinweis). **Keine Normwerte im Code:** Lage,
Teillastverhältnis und Bedingungen der Punkte kommen aus der Datei; einzige EPOS-Vorgaben sind die Taktschwelle 2 % und
die Grädigkeit 5 K des Bestands.

## Für K-D

`OekodesignPunkteLeser` ist geräteartneutral: Punkt-Record (Name, Tj, P_dc, EER_d, Teillastverhältnis, zweite
Bedingung, Zeile), `ZeileLesen`, `Pruefen`, `Bezugspunkt` und `Teillast` mit dem Volllastkennfeld als Delegat. K-D
nutzt ihn für Split und Multisplit unverändert, mit der Raumluft als zweiter Bedingung und dem Kennfeld Außen × Raum als
Delegat; für den Weg `OEKODESIGN` ohne Kennfeld genügt ein Delegat mit konstanter Volllast (Nennleistung).

## Oberfläche, Hilfe, Wiki

- Menüpunkt **Administration → Daten & Import → Import Kältemaschinen (CSV, Copper)…** (`MenuItem_Import_Kaeltemaschinen`,
  `MENU_IMPORT_KAELTEMASCHINEN` in beiden Sprachen) hinter „Import Kälteanlagen VDI 3805“; Ziel
  `Masken.KaeltemaschineImport = "Form_Kaeltemaschine_einlesen"`, Windows-Hülle
  `KatalogImportHuelle.Oeffnen(null, KatalogImportArt.Kaeltemaschine)`; der Knopf im Katalog bleibt. Menü 70 Punkte,
  56 handelnd.
- Hilfetext `IMP_KAT_HINWEIS_KAELTEMASCHINE` nennt beide neuen Vorlagen (de, en); `help_mapping.txt`
  `Form_Kaeltemaschine_einlesen.btn_Help` → `Gerätekataloge#import-kaeltemaschinen`.
- Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Gerätekataloge.wiki`: Absatz mit Anker `import-kaeltemaschinen`,
  ohne Hersteller- und Produktdaten.

## Nachweis

- Kern-Tests `KaeltemaschineImportVariantenTests` (Nennwerte, Wahl, A–D-Abbildung, Fehlerfälle, Weiche, Vorlagen),
  `KaeltemaschineImportTests`, `DiensteTests`, `HelpMappingAnkerWacheTests`; UI-Tests `MenuebandTests`,
  `KatalogImportDialogTests` (bunit, Hilfe beider Sprachen), Seitenschlüssel und Parametersatz.
- Referenzlauf 1055 und 1063 gegen `Referenzlaeufe/2026-10-10_R51_FreieKuehlung`: PASS (350 656 bzw. 350 665 Werte), je 32 CSV byte-gleich; Windows-Schale auf Linux 0 Fehler.
