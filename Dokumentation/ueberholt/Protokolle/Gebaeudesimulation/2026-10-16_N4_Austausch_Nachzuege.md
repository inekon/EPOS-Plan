# N4 — Nachzüge des Datenaustauschs: Feiertagsverweis und Gebäude-Höchsttemperatur

Sitzung Gebäudesimulation, 16.10.2026, Zweig `n4` (von `ios_migration_september` `3a8f1bcb0`). Kein Schemaschritt, keine
neue SQL-Anweisung, kein Eingriff in den Rechenweg, Referenzprojekte unberührt.

## Befund

Nach Nachzug N2 (#1092) blieben laut Übergabepapier (Abschnitt 4, Punkte 3 und 7) zwei Verluste:

1. **Feiertagsperioden ohne Wochenverweis.** `GebaeudeRestdaten.AusZone` übersprang jede Feiertagsperiode
   (`p.IstFeiertag`), weil der Schlüssel `Periodenwoche_<n>` die Periode über Beginn und Ende findet — eine Feiertagsperiode
   hat beide 0 und wird über ihre Feiertagsregel bestimmt. Der Verweis `ID_Woche` ging in gbXML und IFC verloren; die
   Feiertagsperiode kam mit eingebetteter Woche zurück.
2. **Gebäudefeld `Maximaleraumtemperatur`.** B10 („Höchsttemperatur“) meint zwei Felder: `Tab_Zone.Maximaleraumtemperatur`
   (in N2 gebaut, reist je Zone) und `Tab_Gebaeude.Maximaleraumtemperatur` (die obere Raumtemperatur des Gebäudes, Grenze der
   Überhitzungskennzahl im Klassenweg). Die Testtafel führte beide unter B10, die Verlustliste stufte das Gebäudefeld als
   benannten Verlust ein, die Rundlaufwache hielt die Abweichung als Abstimmungseintrag. **Es sind also nicht dieselben
   Felder:** N2 hat die Zone geschlossen, offen war allein das Gebäudefeld.

## Lösung

- **Feiertagsverweis:** neuer Restdatenschlüssel `Feiertagswoche_<n>=<GRÖSSE>;<Feiertagsregel>;<Wochennummer>;<Bezeichner>`
  (eigene Zählung neben `Periodenwoche_`, Bezeichner zuletzt, darf `;` tragen) — in gbXML im Block `EPOS:` der
  `Zone/Description`, in IFC als `IfcText` in `EPOS_Zone`. `AbbildPeriodenwoche` trägt dafür die optionale `Feiertagsregel`.
  Der Leser prüft die Regel gegen die neun bundeseinheitlichen (`Feiertage.Bekannt`) und übergeht einen falschen Wert
  benannt. `ZonenplanCtrl.RestUebernehmen` findet die Feiertagsperiode über die Regel — zuerst mit gleichem Bezeichner,
  sonst die Regel allein (gbXML führt die Feiertage als Tagtyp `Holiday`, der Import benennt sie nach der Saat) — und setzt
  `Kalenderangabe.AusBenannterWoche`.
- **Gebäude-Höchsttemperatur:** `AbbildErdreich` (der Gebäuderest) trägt `MaximaleraumtemperaturC`, Schlüssel
  `Gebaeude_Maximaleraumtemperatur` — gbXML im Block `EPOS:` der `Building/Description`, IFC in `EPOS_Gebaeude`; der
  Vorgabewert 0 der Modellzeile reist nicht. Der Import setzt das neue Zielfeld `MAX_RAUMTEMPERATUR` (Gruppe Sollwerte, °C,
  Ressource `GIMP_FELD_MAX_RAUMTEMPERATUR` deutsch und englisch), die Hülle `GebaeudeImportHuelle.NachKatalogdaten` legt es auf
  `GebaeudeKatalogDaten.MaxTemperatur`. Ohne Angabe bleibt die Zeile leer und ohne Haken.
- **Tafeln:** Kerntafel `DatenaustauschInventar` B10 trägt zusätzlich `Gebaeude_Maximaleraumtemperatur`, C9 zusätzlich
  `Feiertagswoche_` (gbXML und IFC). `GebaeudeExportVerluste.Gebaeude["Maximaleraumtemperatur"]` rundläufig, `Getragen` nennt
  das Feld nicht mehr. Testtafel `Datenaustauschinventar`: B10 bleibt Welle N2 (die Zeile führte das Gebäudefeld schon),
  Abstimmungseintrag `Gebaeude.Maximaleraumtemperatur` entfällt; der Vergleicher B10 misst zusätzlich das Gebäudefeld.
- **Rückwärtsverträglich:** Dateien ohne die neuen Schlüssel lesen wie bisher; ältere Leser übergehen die neuen Schlüssel
  still (kein Zonen- bzw. Erdreichschlüssel).

**Dateien.** `EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeRestdaten.cs`, `GebaeudeAggregation.cs`, `GebaeudeZielfelder.cs`,
`EPOS.Kern/Allgemein/Import/Ifc/IfcAbbildBauer.Standardsaetze.cs`, `EPOS.Kern/Controller/ZonenplanCtrl.cs`,
`EPOS.Kern/Allgemein/Export/Gebaeude/DatenaustauschInventar.cs`, `GebaeudeExportVerluste.cs`,
`EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs`, `EPOS.Kern/MyResource/Resource(.en-US).resx` samt Designer; Tests
`RestdatenRundlaufTests.cs`, `DatenaustauschRundlaufWacheTests.cs`, `Datenaustauschinventar.cs`,
`IfcQuelldateienDiagnoseTests.cs`.

## Tests

- `RestdatenRundlaufTests`: Probenabbild um die Gebäude-Höchsttemperatur und einen Feiertagsverweis erweitert; gbXML- und
  IFC-Rundlauf je 414 Felder (vorher 408), 0 Abweichungen. Neu:
  - `Eine_Feiertagsperiode_mit_Verweis_auf_eine_benannte_Woche_verweist_nach_dem_Import_wieder_auf_sie` (Projekt 1045,
    `ifc4_haus.ifc`, Zonenplan): Neujahr verweist nach dem Import auf die angelegte Woche, gefunden über die Regel trotz
    anderem Bezeichner; Gegenprobe 1. Mai bleibt eingebettet; der Wiederexport schreibt genau `Feiertagswoche_1`.
  - `Die_obere_Raumtemperatur_des_Gebaeudes_reist_in_beiden_Formaten_und_kommt_an_das_Zielfeld`: gbXML und IFC bis zur
    Zielfeldzeile (Wert, Haken), Hülle bis `MaxTemperatur`; Gegenproben Vorgabewert 0 und Datei ohne Angabe.
  - `Ein_Block_ohne_die_Angaben_des_Nachzugs_N4_liest_wie_bisher`: alter Block ohne Meldung; Gegenprobe unbekannte Regel
    benannt übergangen.
- `DatenaustauschRundlaufWacheTests`: Felder je Format 70 663 → 70 665 (das Gebäudefeld in B10), Ausgänge unverändert
  (gbXML gleich 52, abweichend 5; IFC gleich 54, abweichend 3); Abstimmung ohne den Gebäudeeintrag.
- `IfcQuelldateienDiagnoseTests`: Kopfzählung der sechs Anwenderdateien 21/14/6 → 21/14/7 (die neue Zielfeldzeile bleibt leer).
- Abnahmefilter Kern (`Wiederimport|Mehrzonen|Zonenplan|Konditionierung|ZonenNutzung|Restdaten|Datenaustausch|Rundlauf|Gbxml|
  Ifc|Ids|Export|GebaeudeImport|GebaeudeZuordnung|Modultrennung|Dokumentation|Repository|Resource`): 1639 Tests, 1628 grün,
  5 übersprungen (Bestand), 6 rot in `IfcQuelldateienDiagnoseTests` (Zählung, nachgeführt, danach 30/30 grün). Dazu
  Importtests mit Zeilenbezug (`GebaeudeBauteilvorschlag|Welle2|Sqproj|BaujahrImport|IfcCadBauteile|Sollwertvorgaben|
  IfcImport|Zielfeld|GebaeudeNeulesen|KiDialog`) 474/474, UI-Filter (`GebaeudeImport|Gebaeudeimport|Zuordnung|Ressource|
  Resource`) 186/186.
- Bau `WP-Plan.Kern.slnf` Release 0 Fehler; Windows-Schale nicht berührt. Designer: ein neuer Eintrag, zweiter Lauf +0.
  SQL-Dialekt-Prüfer nicht nötig (keine SQL-Anweisung berührt). Referenzlauf nicht gefahren (kein Rechenweg berührt).

## Offen

- Ein Bezeichner mit Zeilenumbruch bricht den gbXML-Block (Bestand der Blockform).
- gbXML trägt die Angabe der Feiertage weiter nur als einen Tagtyp `Holiday`; der Verweis setzt sich nur an der Feiertagsperiode
  seiner Regel, die übrigen acht Feiertage kommen mit dem Holiday-Bild zurück (Bestand).
- Anwenderprobe unter Windows: Export eines Gebäudes mit Feiertagsverweis und oberer Raumtemperatur, Wiedereinlesen im
  Importdialog (Zeile „Maximale Raumtemperatur“ der Gruppe Sollwerte).

## Logbuch-Vorschlag

„Der Gebäudeexport in gbXML und IFC nimmt die obere Raumtemperatur des Gebäudes und die Zuordnung von Feiertagen zu benannten
Wochen mit, der Import liest sie wieder ein.“
