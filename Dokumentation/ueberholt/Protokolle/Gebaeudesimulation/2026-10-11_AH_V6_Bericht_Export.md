# Protokoll AH-V6 — Bericht, Kennzahlen, Export und Datenwege des Vorheizens

**11.10.2026 · Sitzung Gebäudesimulation · Welle V6 der Aufheizoptimierung Fassung 2.** Grundlage:
[Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md), Abschnitte 2.9 und 4;
aufgesetzt auf [AH-V4](2026-10-11_AH_V4_Schema215.md) (Schritt 215, `Vorheizkennzahlen` je Gebäude und Zone). Keine
Oberfläche (V5), kein Referenzprojekt (V7).

## Datenwege (Teil 3)

Alle vier Wege kopieren **schemagetrieben** — keine explizite Spaltenliste war nachzuziehen:

- **Duplizieren und Variante** (`ProjektDuplizierenCtrl`, `VariantenCtrl.AnlegenAusStamm` über dasselbe Duplizieren): Spalten
  aus der Tabelle, `Tab_Einstellungen` und `Tab_Zone` reisen vollständig.
- **Projektpaket** (`ProjektExportImportCtrl`, Export und Import): ebenso.
- **Zonenkatalog** (`Zonenkopie`, Katalog → Projekt und Projekt → Katalog): Schnittmenge beider Zonentabellen; `Vorheizzeit_H`
  steht an `Tab_Zone` und `Tab_Zone_STAMM`.

Belegt mit `VorheizDatenwegeTests` (4 Fälle): Projekt 1052 der Arbeitskopie trägt alle sieben `Aufheiz_*`-Spalten und je Zone
eine eigene Vorheizzeit; Duplikat, Variante und Rundreise über das Paket tragen sie unverändert, der Zonenkatalog reicht
`Vorheizzeit_H` (gesetzt und NULL) hin und zurück.

## Bericht (Teil 1)

- **Gebäudetafel** (`Aufheizbericht.Zeilen` → Kapitel „Projektbeschreibung“ und `tabelle.gebaeude.ergebnis`): nach den
  Aufheizzeilen die Vorheizzeilen `Aufheizbericht.Vorheizzeilen` — Verfahren, Vorheizzeit t_V (vorgegeben bzw. berechnet,
  Quantil), größter Bedarf, Bezugsleistung Φ_K,max, Deckel, Vorheizleistung, Sprung im Vorlauf und im Lauf, Tage ohne Ankunft
  und mit Sprung über t_V, größte Unterschreitung, Stunden unter dem Sollwert, am Deckel und an der Untergrenze, Nächte ohne
  Absenkung, unerreichbare und gesperrte Tage, Mehrwärme (MWh/a) — jede nur mit Wert. Bei Sollwertrampe ist
  `Vorheizen` `null`: keine Zeile, die Tafel bleibt, wie sie war.
- **Tafel „Vorheizen je Zone“** (`Aufheizbericht.Zonentafel`) unter der Gebäudetafel des Kapitels, nur wenn eine Zone
  Vorheizwerte trägt: Zone, t_V, P_V, Tage ohne Ankunft, Unterschreitung max., Mehrwärme.
- **Vorlagenfelder, Katalog v19** (`FASSUNG_VORHEIZEN`): 18 Felder `gebaeude.ergebnis.vorheizverfahren`, `…vorheizzeit`,
  `…vorheizzeit_bedarf_max`, `…vorheiz_bezug`, `…vorheiz_deckel`, `…vorheizleistung`, `…vorheiz_sprung_vorlauf`,
  `…vorheiz_sprung_lauf`, `…vorheiz_tage_verfehlt`, `…vorheiz_tage_ueber`, `…vorheiz_unterschreitung_max`,
  `…vorheiz_unterschreitungsstunden`, `…vorheiz_deckelstunden`, `…vorheiz_floorstunden`, `…vorheiz_naechte_ohne_absenkung`,
  `…vorheiz_tage_unerreichbar`, `…vorheiz_tage_sperrzeit`, `…vorheiz_mehrwaerme`, dazu der Schalter `hat.vorheizen` und die
  Kennzahlfelder der vier neuen Kennzahlen (je Stand und Paar). Messlatte `Vorlagenfeldkatalog_v19.txt`: 79 Schlüssel mehr
  als v18, keiner entfallen.
- **Mitgelieferte Vorlagen:** Die Word-Fassung steigt auf 19; `Werkzeuge/Berichtsvorlage … alle` hat alle zehn Vorlagen neu
  geschrieben (Standard, Beispiel, Kurzbericht und Ausführlich je Sprache, Bausteine je Sprache, Excel ausführlich je
  Sprache), Validator ohne Fehler, ein zweiter Lauf schreibt nichts. Der **Kurzbericht** zeigte die Aufheizung im Bestand
  (Absatz 7a je Gebäude); er bekommt den Absatz 7b „{{#wenn hat.vorheizen}}{{#je gebaeude}} Name – Verfahren: Vorheizzeit,
  Vorheizleistung, Tage ohne Ankunft, Mehrwärme {{/je}}{{/wenn}}“ in beiden Sprachen, als eigener Block hinter 7a (Blöcke
  höchstens zwei Ebenen tief); Kommentar 9 nennt ihn. Die Standardvorlage zeigt die Aufheizung über die Gebäudetafel des
  Kapitels und bekommt keinen Platzhalter.

## Gebäudeexport

`ZoneModel.Vorheizzeit_H` (AH-V4) war in der Verlustliste des Gebäudeexports nicht eingestuft (`GebaeudeExportAblaufTests`
rot seit V4): jetzt benannter Verlust — die Datei trägt sie nicht, der Import schreibt NULL (erbt); der Ablauf nennt den
Verlust, wenn eine Zone sie trägt.

## Kennzahlen, Export, Variantenvergleich (Teil 2)

- **Kennzahlen** (Gruppe „Gebäude“, mit Δ, ab Fassung 19): `geb.vorheizzeit` (Höchstwert der Gebäude, h),
  `geb.vorheizleistung` (Summe, kW), `geb.vorheiz_tage_verfehlt` (Höchstwert, d/a), `geb.vorheiz_mehrwaerme` (Summe, MWh/a);
  bei Sollwertrampe ohne Wert, die Zeilen entfallen wie die Aufheizwerte ohne Aufheizrechnung. Sie gehen mit dem Katalog in
  Kennzahlentabelle, Excel und Variantenvergleich.
- **Export** (`GebaeudeErgebnisexport`, Muster E32 — nur mit Wirkung): `Geb[n].Vorheizverfahren` (Text) und 17 Zahlen
  `Geb[n].VorheizzeitH`, `…VorheizzeitBedarfMaxH`, `…VorheizBezugKw`, `…VorheizDeckelKw`, `…VorheizLeistungKw`,
  `…VorheizSprungVorlaufMaxKw`, `…VorheizSprungLaufMaxKw`, `…VorheizTageVerfehlt`, `…VorheizTageUeberVorheizzeit`,
  `…VorheizUnterschreitungMaxK`, `…VorheizUnterschreitungH`, `…VorheizDeckelstundenH`, `…VorheizFloorstundenH`,
  `…VorheizNaechteOhneAbsenkung`, `…VorheizUnerreichbarTage`, `…VorheizSperrzeitTage`, `…VorheizMehrwaermeMwh`; je Zone
  ebenso unter `Geb[n].Zone[k].`. Bei Sollwertrampe kein Schlüssel — die Referenz-CSV bleiben byte-gleich.
- **Abweichungsmerkmale** (Gewerk „Gebäude“): Verfahren, Vorheizzeit, Art der Toleranz, Toleranz, Regelgenauigkeit ε und
  Geltung; NULL heißt die Vorgabe (Sollwertrampe, Prozent, Gebäude, ε 1 K) und ist keine Abweichung.

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Kerntests, Filter Bericht, Vorlage, Platzhalter, Kennzahl, Export, Variante, Projekttransfer, Paket, ZonenKatalog, Vorheiz,
  Aufheiz, Abweichung, Vergleich, Kurzbericht, Ausführlich, Word, Excel, GebäudeErgebnis und die Wachen (Ressourcen, Text,
  Kodierung, Double, Link, Ordnung, Einheiten, Modultrennung): **3 407 grün, 1 rot, 1 übersprungen** — rot war die
  Verlustliste des Gebäudeexports (behoben, siehe oben). Nachlauf nach Kurzbericht und Verlustliste (Kurzbericht,
  Berichtsvorlagen, Berichtssprache, AufheizBericht, Vorlagenwachen, Vorlagenprüfer, Bausteinvorlage, Auslieferungsvorlagen,
  GebaeudeExportAblauf, VorheizBericht): **374 grün, 0 rot**. Darin `VorheizDatenwegeTests` 4 und `VorheizBerichtTests` 8.
- UI-Tests, Filter Bericht, Kennzahl, Vergleich, Abweichung, Ressourcen, Wache, Vorlage, Ergebnis, Gebäude: **1 703 grün**.
- `Werkzeuge/Auslieferungsvorlage`: **61 grün**. `Werkzeuge/Berichtsvorlage … alle`: zehn Vorlagen, Validator 0 Fehler,
  wiederholbar.
- Nachgezogene Pins: Katalogfassung 19 (`VorlagenfeldkatalogWacheTests`), `KaeltemaschineTeillastBerichtTests` (≥ 18),
  Fassung der Gebäudekennzahlen (`AufheizVergleichTests`: Vorheizkennzahlen 19, übrige 16), Rumpf des Kurzberichts
  (`BerichtsvorlageDateiWacheTests`).
- ChartProben: 254 Bilder, 0 Verstöße (kein Bild geändert).
- Referenzlauf aller 29 Projekte gegen `2026-10-10_R51_FreieKuehlung`: **GESAMT PASS** (10 036 768 Werte), alle 943 CSV
  byte-gleich (abweichend nur `protokoll.txt`).
- Ressourcen: 58 Schlüssel je Sprache (`BV_VORH_*`, `VF_GEBAEUDE__ERGEBNIS__VORHEIZ*`, `VF_HAT__VORHEIZEN`,
  `ABW_MERKMAL_VORH_*`), `Resource.Designer.cs` neu geschrieben.

## Offen

- Der Schalter `Aufheiz_Heizkreis_Einbeziehen` ist kein Abweichungsmerkmal: NULL heißt „an“, der Ja/Nein-Vergleich des
  Ermittlers liest NULL als „nein“ — ein eigener Anzeigeweg wäre nötig.
- Bedarfsdialog und Ergebnisseite der Oberfläche zeigen die Vorheizgrößen noch nicht (V5 oder später).
- Mit V7 (Referenzprojekt mit Verfahren „Berechnet“) erscheinen die Exportschlüssel erstmals in einer Referenz-CSV.
