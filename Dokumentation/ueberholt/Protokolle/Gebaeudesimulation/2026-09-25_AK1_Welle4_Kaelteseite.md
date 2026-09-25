# Protokoll: AK1 Welle 4 — die Kälteseite der Anlagenkopplung (24./25.09.2026)

**Auftrag** (vom Anwender am 24.09.2026 beauftragt): vierte Welle der Stufe AK1 nach **E37** — die
Kälteseite der Kopplung mit eigenen Spalten der Kühlübergabe am Gebäude, gebaut in acht Schritten
(W4-0 bis W4-7). Die Anwenderentscheide vom 24.09.2026 gehen dem Auftragsanhang vor: **A1** eigener
Schalter `Kuehluebergabe_Aktiv` (abweichend von der Empfehlung, behandelt wie `Heizkreis_Aktiv`),
**A2** Nennleistung bei leerem Feld aus einem Auslegungstag, **A3** keine Kühlkurve, fester Vorlauf,
**A4** EPOS-Vorgaben je Art wie vorgeschlagen. Maßgeblich:
[Anlagenkopplung](../../../aktuell/Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 7, 8, 9.1,
9.5, 10.5 und 11.1, [Konzept Gebäudesimulation](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
N1.42.

## 1 Umsetzung

| Schritt | Commit | Stand |
|---|---|---|
| W4-0 Papiere | `810ccfa1` | E37 als Nachtrag N1.42, Kopf und Status (Zeile E37), Register (H9), Anlagenkopplung Kopf, 2.1, 2.2, 7.1, 7.2, 7.4, 8, 8.1, 8.3, 8.4, 9.1, 9.5, 10.5, 11.1, 13; Kühlkonzept 3.2 und 7.1; Mehrzonenkonzept 4.2; Indexzeilen |
| W4-1 Umbau ohne neue Funktion | `ecc2500d` | Schritt H wird `SchrittUebergabe` mit Seite und Spiegel (x bzw. −x, ohne Multiplikation), zwei Zeitfelder je Seite, `GRUENDE = 8`; gemeinsamer Kern `Kreisergebnis`/`Kreisprojekt`; Heizseite byte-gleich |
| W4-2 Schritt K im Löser | `f5f9cf93` | Kühlzweig über `SchrittUebergabe`, Verletzungsmaß der gekoppelten Kühlung, Verteilung a·wAW, a·wIW, 1−a auch im geregelten Kühlfall, Gründe `KeineKaelte`, `KuehlUebergabe`, `KuehlleistungMax`, `VorlaufgrenzeKuehlung`; Klasse `Kuehluebergabe` (Vorgaben je Art, Wirksamkeit, Spiegel) |
| W4-3 Schema | `30ae47af` | Schritte **135** (`KAK-S1`, sechzehn Spalteneinträge, vierter Sichtneubau `Abfrage_Projektgebaeude` 90 → 98 Spalten), **136** (`KAK-S3`, drei plus fünf Ergebnisspalten) und **137** (drei Zonenspalten, Wertliste samt `IDEAL`); Testdatenbank 134 → 137 |
| W4-4 Eingang bis Export | `8f8e5d33` | Eingang mit Kaltwasser-Vorlauf max(Anlage, Grenze) und Auslegungstag (wärmster Tag, periodisch eingeschwungen, höchstens 30 Wiederholungen, 1e-6 relativ), H7 für die Kälteseite, Fassade und `WPCtrl.KuehlVorlaufDesKaeltekanals`, Kältekreis je Gebäude und Projekt, Meldungen (`SIMENG_AK_KAELTESEITE_IDEAL` statt der Vertagung), Ergebnisspalten mit Wächter, Export `kuehlvorlauf_`/`kuehlruecklauf_`/`kuehluebergabe_<n>.csv` |
| W4-5 Oberfläche | `e53a0836` | Baustein `GebaeudeKuehluebergabeFelder` in beiden Wirten, Arbeitsstand (Prüfregeln, Abweichungen, KI-Wege), Hülle und Herleitung samt Wirksamkeit im Projekt, acht KI-Felder; Kacheln und Bild im Bedarfsdialog (`ChartRenderer.VorlaufRuecklauf` ohne neuen Parameter); Bericht „Kältekreis und Kühlübergabe", Produktausweis, Rechenweg-Ausweis; Variantenvergleich; ChartProben der Kälteseite |
| W4-6 Zone | `36b453f9` | `GebaeudeZonenCtrl` liest und schreibt die drei Zonenspalten neben `ZonenSchema.Zonenspalten` (unverändert); Projektduplikat und Projekttransfer tragen sie über die Spaltenliste der Tabelle |
| W4-7 Papiere | dieser Commit | Statuszeile AK1, dieses Protokoll, Nummern 135 bis 137 in den Papieren, Wiki-Quellen, Update-Papier samt Logbuch-Entwurf |

Vergebene Schemanummern: **135**, **136**, **137** (unmittelbar vor dem Schemacommit gegen `origin`
abgelesen; G3 stand mit 132 bis 134, G4 brachte danach S-F als 138).

**Zählung im Commit W4-3.** Der Rumpf von `30ae47af` nennt 19 Schematests; die Klasse
`KuehluebergabeSchemaTests` führt 20 Fälle. Eine Zählung im Committext, keine fehlende Probe; der
Commit ist gepusht und bleibt, wie er ist.

**Gepusht.** W4-0 bis W4-3 hat die Orchestrierung als `e84242b6` gepusht (Konflikt in
`ProjektGebaeudeModel` beim Merge dort aufgelöst: erst die Kühlfelder, dann die Zonen von G3); CI grün
im Lauf `36078014135` auf `45935351`.

## 2 Rechenzeit (E36)

Messprobe `AnlagenkopplungEingangTests.E36_Rechenzeit_ohne_heiz_kuehl_und_beidseitig_gekoppelt`, je
Gebäude und Jahr (in Klammern der Eingangsbau): ohne Kopplung 19,0 ms (6,1), heizgekoppelt 40,0 ms (4,0),
kühlgekoppelt 22,1 ms (4,4), beidseitig 40,8 ms (4,2) — samt Auslegungstag unter der Grenze von 100 ms.

## 3 Proben

**Probegebäude ohne Datenbank** (Kühldecke mit Vorgaben): Auslegungstag 17. Juli (22 °C), Kühllast
2 997 W gegen die ideale Spitze 2 244 W; Kältebedarf 0,385 → 0,277 MWh/a, Spitze 2,24 → 1,57 kW,
Kühlvorlauf 18,0 °C, Kühlrücklauf 18,66 °C, Überhitzungsstunden 297 → 347. Eine kleine Decke
(0,5 kW, 7 °C Anlage): 28,9 h begrenzt, alle an der Vorlaufgrenze, größte Überschreitung 1,09 K.

**1017 an einer Arbeitskopie** (Stufe AK1, Kühldecke mit Vorgaben, Wärmepumpe im Kühlbetrieb mit
18 °C): Kältebedarf ideal 2,522 → gekoppelt 2,362 MWh/a; Spitze 14,99 → 14,99 kW (die Kühlleistungsgrenze
15 kW bindet, 21,5 h); Kühlvorlauf 18,00 °C, Kühlrücklauf 18,81 °C; begrenzte Stunden 0,0, davon an der
Vorlaufgrenze 0,0; Überhitzungsstunden 306 → 330; Nennleistung 20,40 kW aus dem Auslegungstag 19. Juni.
H7 der Kälteseite: hergeleitet 11,31 kW bei Verbrauchsangabe, eine feste Nennleistung von 12 kW genau mit
zwei Läufen (Verbrauch) bzw. einem Lauf (Fläche).

**Charakterisierung der Heizseite** (1045 und 1017 heizgekoppelt, Radiator mit Heizkurve): 70 Dateien
samt `vorlauf_`/`ruecklauf_`/`uebergabe_` byte-gleich nach W4-1, W4-2 und W4-4.

## 4 Zusammenführungen

| Merge | Inhalt | Konflikt |
|---|---|---|
| `0c5248e6` | origin `97a4ade0` (G3 Schritte 132 bis 134) vor dem Schemaschritt | keiner |
| `e84242b6` | Orchestrierung: W4-0 bis W4-3 mit G4c W2 | `ProjektGebaeudeModel` (Kühlfelder vor den Zonenfeldern), dort gelöst |
| `d651e581` | origin G4a W1 (IFC-Leser) nach W4-4 | `Resource.resx`/`.en-US.resx`: Anhänge beider Seiten behalten, Designer unverändert |
| `48b5fbc8` | origin G4c W3 (Schritt 138, S-F) | keiner; Testdatenbank in der origin-Fassung (LFS `3f5c892d`), kein eigener Schritt neu anzuwenden |

Nach jedem Merge: keine Konfliktmarker, Designer unverändert, KI-Zahlen nachgezählt, Schema- und
Kopplungstests grün, Referenzlauf 13/13 und 394 Dateien byte-gleich.

## 5 Abnahme (nach der letzten Änderung)

| Prüfung | Ergebnis |
|---|---|
| Kern-Filter, Windows-Schale (Linux-Weg), EPOS.Referenzlauf | 0 Fehler |
| Test-Gate W4-5 | Kern 6 603 (+1 übersprungen, `IfcProbenTests.Erzeuger_schreibt_die_Proben` von G4), UI 6 139, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 |
| Kern-Tests W4-6 | 6 607 (+1 übersprungen) |
| Auslieferungsvorlage.Tests | 34/34 |
| ChartProben | 174 Bilder, 0 Verstöße; vier Proben der Kälteseite neu, kein Bild des Bestands geändert |
| SqlDialektPruefer | 1 906 Texte, 0 Fundstellen |
| Designer | unverändert, zweiter Lauf +0 |
| Referenzlauf | 13/13 PASS gegen R14 (4 207 049 Werte), 394 Dateien byte-gleich, nur `protokoll.txt` anders |
| Wächter `EPOS.Kern/CLAUDE.md` | beide leer |

Neue Proben: `AnlagenkopplungKaelteseiteTests` (19, W4-2), `KuehluebergabeSchemaTests` (20, W4-3),
`AnlagenkopplungKaelteEingangTests` (15 samt Symmetrieprobe) und `AnlagenkopplungKaelteDatenbankTests`
(6, W4-4), `AnlagenkopplungKaelteOberflaecheTests` (8) und `GebaeudeKuehluebergabeTests` (18 bunit,
W4-5), `KuehluebergabeZoneTests` (4, W4-6); nachgezählt `KiDialogkatalogTests` (72 → 80 Felder) und
`KiMaskenabdeckungWacheTests` (Baustein mit acht Eingabestellen).

## 6 Offen

- **Welle 5:** Referenzprojekt mit Kopplung, Einfrierregel „Auslegungsdaten der Wärme- und
  Kühlübergabe" samt `Kuehluebergabe_Aktiv` und Basis R15.
- **Benannt vertagt** (Anlagenkopplung 7.4): Kühlkurve und Kennlinienwahl je Stunde (A3), eigenes
  Sollwertprofil der Kühlung (KU3), Strahlungsanteil ohne Spalte, Übergabe je Zone (G6), Komfortspalten
  der Kälteseite (AK2).
- **Wiki:** „Gebäudemodell VDI 6007" (Abschnitt „Kühlübergabe", Ergebnisse, Grenzen), „Kühlung"
  (Verweis) und „Simulation" (Anlagenkopplung) fortgeschrieben und gegengelesen, nicht hochgeladen; der
  Logbuch-Satz steht als Entwurf im [Update-Papier](../../../aktuell/Wiki_Update_2026-09-26.md), die
  Version fragt die Orchestrierung beim Anwender ab.
- Die iOS-Zeilen je Maske (N-A5) ohne eigenen Lauf; kein Push, kein CI-Lauf aus dieser Welle.
