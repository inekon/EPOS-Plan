# Protokoll DA-4 — Restdaten beider Formate: Zone, Luftströme, Erdreich, Zeilenbild und Wochen

Welle DA-4 aus dem [Konzept Datenaustausch der Simulationsdaten](../../../aktuell/Gebaeudesimulation/2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md)
(Abschnitte 2, 3.1–3.3, 4.4, 7). Grundlage ist Entscheid E138 (F2 Datei vor Profil, F5 Rundlauf je Welle, F6 (a) Zeitpläne in
`EPOS_*`). Vorgänger: [Protokoll DA-2](2026-10-13_DA-2_gbXML_Export_Rundlauf_Schema225.md),
[Protokoll DA-3](2026-10-13_DA-3_IFC_Standardsaetze.md). **Kein Schemaschritt** (alle Felder bestehen), keine Leseregel des
Rechenwegs, kein Referenzprojekt beschrieben.

## Befund

- Weder gbXML 6.01 noch IFC4 hat ein Standardelement für Vorheizzeit (B20), Höchstleistungen (B11), Übergabe und Kühlübergabe je
  Zone (B21, B22), Luftströme zwischen Zonen (G1; `AirLoop` ist Anlagentechnik), Umfang P, wirksamen U-Wert am Erdreich und
  Kellertemperatur (G3, G4), Zeilenbild (C8) und benannte Wochen (C9). Alle gehen damit über den Rückfall nach Konzept 3.3:
  gbXML **Weg 2** (Block `EPOS:` mit Schlüssel=Wert in `Description`; der Weg 1 über einen Erweiterungspunkt bleibt ungeprüft,
  die Schemakopie liegt nicht im Repositorium), IFC die eigenen Sätze `EPOS_Zone` und `EPOS_Gebaeude`.
- Der gbXML-Leser las `Description` bisher nicht; er liest jetzt nur den Block, der Text davor bleibt unbeachtet (Importproben
  unverändert).
- Die Heizsollwerte der Nacht- und die Kühlsollwerte der Tageszeile stehen im Stand der Konditionierung in den Bestandsspalten der
  Zone (`Raumsolltemperatur_Nachtabsenkung`, `Kuehl_Sollwert`); die Zelle trägt dann die Zeiten. Das Zeilenbild reist trotzdem
  vollständig (Wert, „aus“, Zeiten, ΔT); verglichen wird der Wert an der gebildeten Matrix.
- Die Vorgabematrix lehnt Zeiten außerhalb der Nacht- (Stunden 0–23) und Saisonzeile (Tage 1–365) und ΔT außerhalb der Nachtzeile
  der Lüftung ab und bräche damit den ganzen Import. Der Leser übergeht solche Zeilen benannt.
- **Befund DA-7, IFC-Azimut:** Im gemeinsamen Rundlauf kam die Außenwand Ost mit 270° zurück. Ursache im IFC-Leser
  (`IfcAbbildBauer.Koerperflaechen`): Die Außenseite eines Wandkörpers war die vom **Gebäudeschwerpunkt** abgewandte Seite. Ein
  angebauter Raum ohne Hüllfläche (Zone 2 der Probe liegt in der schematischen Anordnung östlich) verschiebt den Schwerpunkt über
  die Ostwand hinaus — die Seite kippt. Behebung: Eine Außenwand mit genau einem Raum mit Körper nimmt die Mitte **ihres** Raums
  (`Wandbezug`), sonst wie bisher den Gebäudeschwerpunkt. Schreiber und schematische Geometrie sind richtig (Normalen aller vier
  Wände geprüft).

## Abbildung je Feld und Format

| Inventar | EPOS | gbXML (Weg 2) | IFC | Import |
|---|---|---|---|---|
| B20 | `Tab_Zone.Vorheizzeit_H` | `Zone/Description`, `Vorheizzeit_H=` | `EPOS_Zone.Vorheizzeit_H` | Zonenzeile (0 … 47 h) |
| B11 | `Heizleistung_Max`, `Kuehlleistung_Max` | dto. | dto. | Zonenzeile (≥ 0) |
| B21 | `Uebergabe_Art`, `_Exponent`, `_Leistung_Nenn`, `Auslegung_Vorlauf`, `_Ruecklauf`, `_Raumtemperatur`, `Regler_Proportionalband` | dto. | dto. | Zonenzeile; Art nur aus der Liste des Kerns |
| B22 | `Kuehl_Uebergabe_Art`, `_Exponent`, `_Leistung_Nenn` | dto. | dto. | Zonenzeile; Art nur aus `Waermeuebergabevorgaben.KuehlArten` |
| G1 | `Tab_Zonenluftstrom` (Zone A trägt den Strom) | `Luftstrom_<n>=<Partnerzone>;<Volumenstrom>` | `EPOS_Zone.Luftstrom_<n>` | `Tab_Zonenluftstrom`, Partner über die Kennung `Zone=` der Datei, A < B |
| C8 | `Tab_Konditionierungsvorgabe` (eigene Zeilen der Zone) | `Zeile_<GRÖSSE>_<ZEILE>=wert=…;aus;von=…;bis=…;bedingt=…` | `EPOS_Zone.Zeile_*` | über `Konditionierungsarbeit.ZelleSetzen` |
| C9 | `Tab_Konditionierungswoche` (Name, Größe, 168 Werte) | `Woche_<n>=<GRÖSSE>;<168 Werte, „aus“>;<Name>` | `EPOS_Zone.Woche_*` | über `Konditionierungsstand.MitWochen`; gleicher Name und gleiche Größe ersetzt |
| G3 | `Abmessung_Anschluß_Außenwand_Kellerdecke` | `Building/Description`, `Erdreich_Umfang=` | `EPOS_Gebaeude.Erdreich_Umfang` | Zielfeld `LAENGE_AUSSENWAND_KELLER` |
| G4 | `Erdreich_U_Wirksam`, `Kellertemperatur` | `Erdreich_U_Wirksam=`, `Kellertemperatur=` | `EPOS_Gebaeude.*` | neue Zielfelder `ERDREICH_U_WIRKSAM`, `KELLERTEMPERATUR` (Gruppe Grund; Hülle setzt `ErdreichUWirksam`, `Kellertemperatur`) |

Zahlen invariant und rundlaufsicher (`R`); in IFC als `IfcText`. Ein unlesbarer oder unzulässiger Wert wird übergangen und gemeldet
(`IMP_GBXML_PROT_RESTDATEN_UEBERSPRUNGEN`, `IMP_IFC_PROT_RESTDATEN_UEBERSPRUNGEN`), ein Luftstrom zu einer Zone, die die Datei nicht
führt, zur Zone selbst oder doppelt ebenso (`IMP_BAUTEIL_PROT_LUFTSTROM_PARTNER`). Ohne Restdaten schreiben beide Schreiber nichts
Neues (Dateien bytegleich zum Stand vor DA-4). Die Kennungen der Datei sind stabil (zweimal geschrieben bytegleich).

**Inventar nach DA-4** (Fortschreibung der Tafeln der Protokolle DA-2 und DA-3; die Tafel der Wache führt DA-7):

| Datum | gbXML | IFC |
|---|---|---|
| B11, B20, B21, B22, G1, G3, G4, C8, C9 | ohne Element → **Rückfall Weg 2** (beide Richtungen) | ohne Element → **nur `EPOS_*`** (beide Richtungen) |

Damit gbXML: 42 beide Richtungen (Standard) + 9 über den Rückfall, 3 nur gelesen, 17 ohne Element (A2, A3, A5, A7, A8, A9, B10,
B19, C10, D8, E6, E7, F3, F4, F5, F7, F10). IFC: 31 Standard + 25 `EPOS_*`, 1 nur gelesen, 14 ohne Element (A2, A3, A7, A8, A9,
B10, B19, C10, E6, E7, F4, F6, F7, F10).

## Dateien

- neu: `EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeRestdaten.cs` (`AbbildZonenrest`, `AbbildErdreich`, Schlüssel, Lesen und
  Schreiben, Block der Beschreibung, Luftströme), `EPOS.Kern.Tests/RestdatenRundlaufTests.cs`
- geändert: `GebaeudeAbbild.cs` (`AbbildRaum.Rest`, `AbbildGebaeude.Erdreich`), `GebaeudeExportSatz.cs` (Stand jeder Zone,
  Luftströme), `GebaeudeExportAblauf.cs`, `GbxmlSchreiber.cs`, `GbxmlLeser.cs`, `IfcSchreiber.cs`, `IfcAbbildBauer.cs`
  (Restdaten, `Wandbezug`), `IfcAbbildBauer.Standardsaetze.cs`, `GebaeudeBauteilvorschlag.cs` (`Zonenreste`, `Luftstroeme`),
  `GebaeudeAggregation.cs`, `GebaeudeZielfelder.cs`, `GebaeudeZonenCtrl.cs` (Schritt 3b Luftströme, Restdaten nach der
  Konditionierung), `ZonenplanCtrl.cs` (`RestUebernehmen`), `EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs` (zwei Zielfelder),
  `GebaeudeExportVerluste.cs`, `EPOS.Kern.Tests/GebaeudeExportAblaufTests.cs`, beide `Resource*.resx`, `Resource.Designer.cs`
- Ressourcen: 6 neue Schlüssel je Sprache (`GIMP_FELD_ERDREICH_U_WIRKSAM`, `GIMP_FELD_KELLERTEMPERATUR`,
  `GIMP_BELEG_EPOS_RESTDATEN`, `IMP_GBXML_PROT_RESTDATEN_UEBERSPRUNGEN`, `IMP_IFC_PROT_RESTDATEN_UEBERSPRUNGEN`,
  `IMP_BAUTEIL_PROT_LUFTSTROM_PARTNER`); Designer zweiter Lauf +0.
- SQL: eine neue Anweisung (`INSERT INTO Tab_Zonenluftstrom`, wie im Bestand), Prüfer siehe Nachweis.

## Verlustliste (`GebaeudeExportVerluste`)

- Zone auf **rundläufig**: `Vorheizzeit_H` (doppelte Zeile entfernt), `Heizleistung_Max`, `Kuehlleistung_Max`, die sieben
  Übergabe- und Auslegungsspalten, die drei Kühlübergabespalten; dazu nach Befund DA-7 `Luftwechsel_Nutzer`,
  `Interne_Waermegewinne`, `Bewohner`, `Kuehl_Sollwert`, `Kuehl_Sollwert_Nacht`, `Kuehlung_Aktiv`.
- Gebäude auf **rundläufig**: `Abmessung_Anschluß_Außenwand_Kellerdecke`, `Erdreich_U_Wirksam`, `Kellertemperatur`; nach Befund
  DA-7 `Gebaeudeart`.
- `Getragen` nennt diese Zonenfelder und `Erdreich_U_Wirksam` nicht mehr. Benannt bleiben u. a. `Maximaleraumtemperatur`,
  `Heizung_Strahlungsanteil`, die Sollwerte Wochenende und Ferien, `Grundflaeche_Randbedingung` (wird aus den Bauteilen gebildet),
  die Gebäudefelder der Übergabe und Heiz-/Kühlkurve, und — gebäudeseitig — `Heizleistung_Max`, `Kuehlleistung_Max`,
  `Luftwechsel_Nutzer`.
- Die Wache `DatenaustauschRundlaufWacheTests` (DA-7) gleicht Verlustliste und Inventartafel ab; ihre Liste `Abstimmung` zieht die
  Orchestrierung beim Merge nach.

## Tests

`EPOS.Kern.Tests/RestdatenRundlaufTests` (8 Tests):

- gbXML und IFC: Export → eigener Leser → Vergleich Feld für Feld, je **393 Felder, 0 Abweichungen** (Erdreich 3; Zone 1: 14
  Felder, 1 Luftstrom 2, 4 Zeilen 28, 2 Wochen 2 × 170; Zone 2: 14); zweimal geschrieben bytegleich.
- Gegenprobe ohne Restdaten: kein Block, keine neue Eigenschaft, Leser liefert nichts.
- Gegenprobe unlesbar: Vorheizzeit 99 h, Übergabeart „KAMIN“, Kellertemperatur „warm“ werden übergangen und in zwei Meldungen
  genannt, die übrigen Werte bleiben.
- Import: Zonenfelder aus dem Rest; Luftströme über die Kennung, Gegenprobe fremder Partner, Zone selbst, doppeltes Paar (drei
  Meldungen).
- Verlustliste: die neuen Felder rundläufig und nicht mehr getragen; Gegenprobe `Maximaleraumtemperatur` bleibt benannt.
- Datenbank (Testdatenbank, Projekt 1045, `ifc4_haus.ifc`, Mehrzonenvorschlag): Zonenfelder, ein Luftstrom (A < B, 100), vier
  Zeilen des Zeilenbilds und zwei benannte Wochen kommen in `Tab_Zone`, `Tab_Zonenluftstrom` und den Stand der Zone.
- IFC-Azimut (Befund DA-7): Azimut aller vier Himmelsrichtungen, Neigung der Außenbauteile und Nachbarräume jedes Bauteils;
  **Gegenprobe:** vor der Behebung rot („Bauteil 103 Azimut 90 ≠ 270“).

Nachgezogen: `GebaeudeExportAblaufTests.Verlustliste_jedes_Feld_eingestuft_per_Reflexion` (die nun rundläufigen Felder aus der
Mindestliste „benannt“).

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Die Windows-Schale ist nicht berührt (die Hülle
  `GebaeudeImportHuelle` liegt in `EPOS.UI.Daten`, im Kern-Filter).
- `dotnet test EPOS.Kern.Tests --filter "Gbxml|Ifc|Rundlauf|Export|Zonenluft|GebaeudeImport|Sqproj|Modultrennung|Dokumentation|Repository|Resource|Restdaten|Bauteilvorschlag|Mehrzonen|Datenaustausch"`:
  1408 Tests, 1396 grün, 5 übersprungen (Bestand), 7 rot — alle nachgezogen und in der Wiederholung grün (90 von 90 mit
  `IfcQuelldateienDiagnose|GebaeudeExportAblauf|GebaeudeImportHuelle|Zielfeld|Zuordnungsmodell`): sechs Anwenderdateien der
  IFC-Diagnose zählen im Kopf „Werte/Vorgaben/leer“ zwei leere Zeilen mehr (die neuen Zielfelder, 21/14/4 → 21/14/6; Hülle,
  Zonierung und Flächen unverändert — auch nach der Behebung des Azimuts), und die Verlustmeldung nennt den Nutzerluftwechsel
  der Zone nicht mehr. Zusätzlich `Editorabschluss|Baualter|Modellvorschlag|Gebaeudedialog|Zonenplan|Konditionierung|Vorheiz`:
  568 von 568 grün.
- Neu: 8 Tests (`RestdatenRundlaufTests`).
- SQL-Dialekt-Prüfer: 2968 SQL-Texte, 0 Fundstellen. Designer: zweiter Lauf +0.
- Referenzlauf: nicht gefahren (kein Rechenweg berührt; die Orchestrierung fährt ihn gegen R53).

## Offen

- **Anreicherung (G7d):** `IfcAnreicherung` schreibt die Restdaten noch nicht in eine Originaldatei.
- **Schemakopie gbXML 6.01:** ob ein Erweiterungspunkt (Weg 1) besteht, ist ungeprüft; der Block in `Description` ist schemagültig.
- **Bezug Periode → benannte Woche:** Die Wochen kommen mit Name und Werten zurück; eine Periode, die auf eine Woche verweist,
  kommt als eigene Woche zurück (der Verweis `ID_Woche` geht verloren).
- **Einzonenweg:** Die eine Zone trägt keinen Luftstrom; die Restdaten nimmt sie vom ersten beheizten Raum.
- **Innere Bauteile im IFC-Rundlauf:** Azimut und Neigung innerer Bauteile kommen nicht zurück (der Import nimmt die Vorgabe der
  Bauteilart); eine Innenwand im selben Raum kommt mit einem Nachbarn zurück.
- **Anwenderprobe unter Windows:** Export eines Mehrzonengebäudes mit Luftströmen und Wiedereinlesen im Importdialog.

## Logbuch-Vorschlag

Der Gebäudeexport in gbXML und IFC nimmt Vorheizzeit, Höchstleistungen, Wärmeübergabe je Zone, Luftströme zwischen Zonen, die
Erdreichangaben sowie Zeilenbild und benannte Wochen der Kalender mit, der Import liest sie wieder ein.
