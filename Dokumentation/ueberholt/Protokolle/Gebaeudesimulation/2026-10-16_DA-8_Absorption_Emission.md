# DA-8 — Absorptions- und Emissionsgrad je Bauteil

Sitzung Gebäudesimulation, 16.10.2026, Zweig `da8` (von `ios_migration_september` `f6280948f`). Anwenderentscheid 16.10.2026:
bauen. Grundlage: [Konzept Datenaustausch](../../../aktuell/Gebaeudesimulation/2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md)
Abschnitt 6 (Frage F8) und 7 (DA-8).

## Ausgang

Der Rechenweg bildete den kurzwelligen und den langwelligen Term der äquivalenten Außentemperatur (VDI 6007-1 Gl. (33), (38))
mit zwei Festwerten: `GebaeudeFestwerte.ABSORPTIONSGRAD_OPAK` (0,6) in `GebaeudeKlimaweg.DeltaThetaKurzwellig` und
`GebaeudeFestwerte.EMISSIONSGRAD_AUSSEN` (0,9) in `GebaeudeKlimaweg.DeltaThetaLangwellig`. Verwender der Festwerte darüber
hinaus: `GbxmlSchreiber.Aufbau` (schrieb sie als `Absorptance`), `GbxmlLeser.Absorptionsgrade` (meldete Abweichungen),
`IfcStandardsaetze` (`Pset_MaterialOptical`) und `IfcAbbildBauer.OptikPruefen`. Der Klassenweg (ohne Bauteile) bleibt beim
Festwert.

## Lösung

- **Schema** `BauteilStrahlungSchema`, angemeldet als Schritt 237: `Absorptionsgrad_Solar` und `Emissionsgrad`, `REAL` nullbar
  mit `CHECK (x IS NULL OR (x >= 0 AND x <= 1))`, an `Tab_Bauteilaufbau(_STAMM)` und `Tab_Bauteil(_STAMM)`; STRICT bleibt,
  wiederholbar, kein DML. **Vorläufige Kette:** `SCHRITT = BauteilUHerkunftSchema.SCHRITT + 1` (heute 234), nach Schritt 236
  auf dessen Klasse `+ 1` umhängen. Eingetragen in Schalen-Schrittliste, `Werkzeuge/Testdatenbankschema`, Testvorrichtung,
  `Paketanhebung` (Art Ddl), `SchemaStand.Zielversion`, `Katalogfassung` (leer ohne Beitrag zur Prüfsumme) und den
  Zusatzspalten des Aufbaukatalogs (`Bauteilkatalog.Spalten`).
- **Rechenweg:** `BauteilEingang` trägt beide Grade (NaN = Festwert); `GebaeudeZonenabbildung.AlsBauteil` setzt den Wert des
  Bauteils, sonst den des Aufbaus. Die Glieder der U·A-Gewichtung (`GebaeudeModellEingang.BauteilwegAussenseite`) führen die
  wirksamen Grade im Schlüssel; Bauteile mit dem Festwert fallen wie bisher in ein Glied, deshalb rechnet ein leeres Feld
  bitgleich. `DeltaThetaKurzwellig` und `DeltaThetaLangwellig` haben je eine Überladung mit dem Grad.
- **Oberfläche:** Bauteildialog und Aufbaueditor (Gruppe „Strahlung der Außenfläche" hinter den Schichten, auch in „Neu…")
  mit je zwei Zahlenfeldern 0 … 1, Vorgabe als Platzhalter; Ressourcen beider Sprachen; KI-Feldkarte beider Masken
  (`absorptionsgrad`, `emissionsgrad`) samt Sichten; Eingabebilanz 16 → 18 bzw. 12 → 14.
- **Austausch:** Das Abbild trägt die Grade je Bauteil. gbXML: `Construction/Absorptance` mit dem Wert des Bauteils, sonst der
  Vorgabe; ein abweichendes Wertepaar bekommt eine eigene `Construction` (Kennung mit Zusatz `-S`). Der Leser führt einen von der
  Vorgabe abweichenden Wert am Aufbau des Abbilds und gibt ihn an die Bauteile weiter; ein Wert außerhalb 0 … 1 wird verworfen
  und gemeldet. IFC: `EPOS_Bauteil.AbsorptionsgradSolar` und `EPOS_Bauteil.Emissionsgrad` in beiden Richtungen
  (`Pset_MaterialOptical` gilt je Baustoff und reicht die Vorgabe weiter durch). Der Bauteilvorschlag des Imports und die
  Zusammenführung des Wiederimports übernehmen die Werte.
- **Inventar:** F8 und F9 tragen Felder (`Tab_Bauteil`, `Tab_Bauteilaufbau`), Trägerwelle DA8; IFC von „beide Richtungen" nach
  „nur EPOS" (29/28 statt 31/26).
- **Wiki-Quellen:** Seiten „Gebäude" (Bauteildialog), „Baustoffe und Bauteilaufbauten" (neuer Abschnitt) und „Gebäudemodell
  VDI 6007" (Strahlung) knapp ergänzt. Logbuch-Vorschlag: „Absorptions- und Emissionsgrad lassen sich je Bauteilaufbau und
  Bauteil eintragen und reisen mit gbXML und IFC."

## Nachweis

- Kern-Filter und Windows-Schale (`-p:EnableWindowsTargeting=true`) 0 Fehler; SQL-Dialekt-Prüfer 0 Fundstellen.
- Tests mit `--filter` auf die berührten Klassen grün (Schema, Rechenweg, Datenaustausch, gbXML/IFC-Rundlauf, Bauteil- und
  Aufbaudialog, KI-Masken); `ModellpruefungTests.Nur_U_Bauteil_bekommt_Katalog_oder_Typaufbau_mit_U_Naehe_und_Ruecknahme_stellt_zurueck`
  ist fremd (Saat `U_Herkunft` von 1052).
- Referenzlauf der achtzehn Projekte der CI-Auswahl gegen `2026-10-14_R55_KaeltebedarfDezentral`: GESAMT PASS, 18 von 18
  byte-gleich. Kein Referenzprojekt füllt ein Feld; die Einfrierregeln bleiben unberührt.
