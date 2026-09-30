# SK8 — Kessel-Kennlinie, Abschluss (E5): kleine Kurve im Kesseleditor, Kesseltafel im Vorlagenfeldkatalog v11

Stand: 30.09.2026 · Zweig `ios_migration_september` · Opus-Agent im Worktree, Zweig `kessel-abschluss` ab `4c8ff7c1`
(`kessel-e4` mit E1–E4, Schemastand 158, Basis R29). **Kein Schemaschritt, keine Änderung der Testdatenbank, kein
Rechenweg geändert (Basis R29 byte-gleich).** Konzept
[`Konzept_Kessel_Kennlinie_EPOS-Plan.md`](../../Konzept_Kessel_Kennlinie_EPOS-Plan.md) (4.1, 5, 7; mit diesem Schritt
vollständig umgesetzt und nach `ueberholt/` verschoben); Vorgänger
[`SK7_Kessel_Takten_E4_R29_Protokoll.md`](SK7_Kessel_Takten_E4_R29_Protokoll.md).

Commits:
- `98d9f6c9` Merge `origin/ios_migration_september` (`243400f4`: #629 Legendenfarbe über die Rolle der Reihe), vor der
  Arbeit, konfliktfrei
- `7339b540` Kurve im Kesseleditor: Kern, Renderer, Hülle, Dialog, ChartProben samt Messlatte, Tests
- `85637ef6` Kesseltafel im Katalog v11, Sammler, Marke im Kessel-Reiter, Vorlagen neu erzeugt, Tests
- Papiere: Konzept nach `ueberholt/`, Index, Verweise, Wiki-Quellen, dieses Protokoll (Commit danach)
- `c5b5f9e3` Merge `origin/ios_migration_september` (`edc2f463`: Statuszeilen #629, #630), konfliktfrei
- Gate-Zahlen in diesem Protokoll im Commit danach

## 1 Auftrag

Anwenderauftrag 30.09.2026: „führe aus: Konzept Kessel-Kennlinie: Aus Abschnitt 5 sind noch zwei Punkte offen“ —

1. **Katalogeditor:** Die Gruppe „Kennlinie“ des Kesseleditors bekommt eine kleine Kurve η(β) über der Last bei 30, 50
   und 60 °C Rücklauf, gerechnet aus derselben Kernfunktion wie der Lauf (Teillast E2, Brennwert E3, Vorgaben nach 7.1
   für leere Felder); ohne Brennwertkennlinie eine Kurve; sie folgt dem Arbeitsstand, auch ungespeicherten Eingaben;
   ein neues Modell im `ChartRenderer`, ChartProben ergänzt, bestehende Bilder byte-gleich.
2. **Bericht:** η_eff (Jahresnutzungsgrad), Brennwertanteil (Stunden und Wärme) und Starts je Kessel in den
   Vorlagenfeldkatalog, gefüllt aus `BerichtsDaten` über `BerichtsDatenSammler`; die Wache
   `BerichtSchreiberOhneDatenbankWacheTests` bleibt grün; Word-Vorlagen und `Werkzeuge/Berichtsvorlage` nur nachziehen,
   wenn ein ausgeliefertes Muster die Felder zeigen soll.

## 2 Die Kurve im Kesseleditor

- **Kern** (`EPOS.Kern/Allgemein/Simulation/Kesselkennlinie.Kurve.cs`): `Kesselkennlinie.Kurven(wirkungsgradGas,
  wirkungsgradOel, brennstoff, brennwert, beschreibung, eta30, kennlinieBrennwert)` liefert je Kurve zehn Punkte
  (β = 0,1 … 1,0; der Knick bei 0,3 ist eine Stützstelle) aus denselben Funktionen wie der Lauf: `WirkungsgradAlsFaktor`
  (Prozentregel über 1,5), `Nennwirkungsgrad` (Ölfeld beim Heizöl, 0,90 für einen fehlenden Wert), `Eta30Wirksam` mit
  der Bauart aus Schalter und Beschreibung, dann `Eta` bzw. `EtaBrennwert`. Mit Brennwertkennlinie (nur beim
  Brennwertkessel und einem kondensierenden Brennstoff, `RechnetMitBrennwertkennlinie`) je Rücklauf 30, 50 und 60 °C eine
  Kurve, sonst eine; der Elektrokessel flach bei η₁₀₀. **Eine Auskunft ruft den Rechenweg:** `SimulationSPK` ruft
  `WirkungsgradAlsFaktor` beim Einlesen und `Nennwirkungsgrad` in `Stunde_Abschluss` und `Nennwirkungsgrad(index)` jetzt
  selbst, statt die Regeln inline zu führen — arithmetisch gleich (Referenzlauf byte-gleich, Abschnitt 5).
- **Renderer** (`ChartRenderer.Kesselkennlinie.cs`): `KesselkennlinieModell(titel, xTitel, yTitel, reihen)`, 720 × 430,
  x die Last in Prozent (0 … 100), y der Wirkungsgrad als Faktor mit Luft 0,01 und der Stufung der Kennlinien
  (`Skala.Stufe`), zwei oder drei Nachkommastellen je nach Stufe. Ein reines Pixelbild ohne Zeichenfläche wie die
  Kennlinien der Wärmepumpe: Linie und Punktmarken je Reihe unter der Marke der Reihe, jede Marke mit ihrem Wert
  („Last 30 % · Rücklauf 30 °C: 1,070“), je Linie eine `Datenreihe` mit der Last als x-Stelle; Linien und
  Legendenfelder in den Serienrollen (Regel „Die Farbe einer Reihe ist ihre Rolle“, #629). Nicht endliche Werte fallen
  weg, ohne Linie der Leerhinweis. `KesselkennlinieBild` gibt dasselbe als PNG für die Proben.
- **Hülle** (`EPOS.UI.Daten/Erzeuger/HeizkesselKennlinienbild.cs`, plattformfrei): baut das Modell aus dem
  `HeizkesselKatalogDaten` des Dialogs — ohne Energieträger gilt die 1 wie beim Speichern, der Schalter
  Brennwertkennlinie nur beim Brennwertkessel wie im Controller; Texte `HZKK_BILD_*` de/en. Die Windows-Hülle
  `HeizkesselHuelle.Gaben` reicht den Delegaten `Kennlinienbild` herein — damit tragen alle drei Wege zum Editor die
  Kurve (Katalogbrowser, Projektdialog, eigenes Fenster).
- **Dialog** (`HeizkesselKatalogDialog.razor`): unter den Feldern der Gruppe „Kennlinie“ ein `DiagrammSvg` (Kennung
  `hzkk-kennlinie`). Das Modell wird zwischengespeichert und nur neu geholt, wenn sich Wirkungsgrade, Energieträger,
  Brennwertschalter, Beschreibung (Bauart), η₃₀ oder der Schalter Brennwertkennlinie ändern — eine Eingabe in ein
  fremdes Feld lässt Referenz, Knotenbaum und abgewählte Legendeneinträge stehen. Ohne Delegat keine Kurve.
- **ChartProben** (`Program.Kesselkennlinie.cs`): Maßproben `kesselkennlinie_brennwert` (drei Rückläufe, `SERIE_1` bis
  `SERIE_3`) und `kesselkennlinie_teillast`, Gegenproben `kesselkennlinie_brennwert_wirkt` und
  `kesselkennlinie_eta30_wirkt`, SVG-Proben `svg_kesselkennlinie_brennwert` (Pixelbild der Gruppe (b)) und
  `svg_kesselkennlinie_werte` (30 Werte, die Prüfpunkte wörtlich). **Messlatte** `Messlatte_2026-09-30.sha256`: die 194
  Zeilen unverändert, **6 Bilder neu**, keines geändert; LIESMICH-Abschnitt „Kesselkennlinie“. Die Windows-Messliste des
  Gates ist auf Windows nachzuziehen (sechs Zeilen neu).
- **Tests:** `KesselkennlinieKurveTests` (Prüfpunkte, jeder Punkt gleich `Eta`/`EtaBrennwert`, Normvorgabe nach Bauart,
  Prozentregel, Ölfeld und Rückfall, Elektrokessel, Bild: Datenreihen, Rollen, Werte, Leerhinweis);
  `HeizkesselKatalogDialogTests`: die Kurve folgt dem Arbeitsstand (η₃₀ und Schalter holen neu, ein fremdes Feld nicht),
  ohne Delegat keine Kurve, das Bild der Hülle aus der Kernfunktion.

## 3 Die Kesseltafel im Vorlagenfeldkatalog

- **Daten:** Brennwertstunden, Brennwertwärme und Starts stehen nicht im gespeicherten Ergebnis (`Tab_ErgebnisHeizkessel*`
  führt je Kessel nur den Jahresnutzungsgrad). Sie reisen deshalb wie die Bezugsspitze als Werte des Laufs im
  Zeitreihensatz: `ZeitreihenSatz.Kessel` (Liste `Kesselbetrieb`: Name, Jahresnutzungsgrad, Brennwertkennlinie ja/nein,
  Brennwertanteil nach Stunden und nach Wärme, Starts), gefüllt in `ZeitreihenExtraktor` über
  `SimulationErgebnisCtrl.Heizkessel` — dieselbe Stelle, aus der der Kessel-Reiter liest. Dafür trägt
  `KesselModulZeile` zusätzlich den Wärmeanteil des Brennwertbetriebs je Kessel (dieselbe Teilung wie der Anteil über
  alle Kessel). Kein Schemaschritt.
- **Katalog v11** (`Vorlagenfeldkatalog.Tabellen.cs`, `FASSUNG_KESSEL`): `stand.tabelle.heizkessel` (Tabelle je Stand,
  Word und Excel, Bedarf Zeitreihen — der Sammler rechnet frisch, sobald eine Vorlage sie nutzt) und der Schalter
  `hat.tabelle.heizkessel`. Der Schalter einer Tabelle trägt jetzt die Fassung seiner Tabelle statt fest 4 — für alle
  bisherigen Tabellen dieselbe 4, für die Kesseltafel 11; sonst stünde er in der ausgelieferten Liste der Fassung 4.
  `KATALOGFASSUNG` = `KatalogfassungWord` = 11, eingefrorene Liste `Vorlagenfeldkatalog_v11.txt` (gegen v10 genau die
  zwei Schlüssel neu).
- **Tabellenbau** (`Berichtstabellen.Heizkessel`): Spalten Heizkessel · Jahresnutzungsgrad [%] (N1) · Brennwertbetrieb
  Stunden [%] · Brennwertbetrieb Wärme [%] (N0, Strich ohne Brennwertkennlinie wie im Reiter) · Starts [1/a]; leer mit
  Grund ohne Zeitreihensatz (`BV_GRUND_KEINE_ZEITREIHEN`) oder ohne Kessel (`BV_GRUND_KEIN_HEIZKESSEL`, neu, de/en);
  Kopftexte im Wörterbuch `BerichtTexte`. Er liest nur den Wertesatz — `BerichtSchreiberOhneDatenbankWacheTests` grün.
- **Marke in der App:** Die Modultabelle des Kessel-Reiters trägt `stand.tabelle.heizkessel` (Stufe „ähnlich“, Hinweis
  `VF_ORT_KESSELTAFEL`); Ortstabelle `Vorlagenfeldorte` und Direkttabellen der Abdeckungswache nachgezogen.
- **Vorlagen:** Die Word-Fassung steigt auf 11, die ausgelieferten Vorlagen tragen sie in `custom.xml` — deshalb mit
  `dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- alle WindowsFormsApplication1/Allgemein/Bericht/Vorlagen`
  neu erzeugt, zweimal, der zweite Lauf byte-gleich, `OpenXmlValidator` in jeder Fassung ohne Fehler. Die **ausführliche
  Vorlage** (de/en) zeigt die Tafel im Block je Stand unter `{{#wenn hat.tabelle.heizkessel}}` (Werkzeug
  `Ausfuehrlich.cs`), die **Bausteinvorlage** den Schlüssel als Schnellbaustein; Beispiel-, Standard-, Kurzbericht- und
  Excel-Vorlagen tragen nur die neue Fassung (die Standardvorlage bleibt kapitelgleich — die Tafel gehört zu keinem
  Kapitel). Die Stilvorlage bleibt byte-gleich.
- **Tests:** `VorlagenfeldKesseltafelTests` (Katalogeintrag und Schalter in Fassung 11, alle übrigen Tabellenschalter in
  Fassung 4; Zellen der Tafel mit Strich ohne Brennwertkennlinie; Gründe; der Sammler füllt die Tafel aus dem Lauf von
  1050 — Jahresnutzungsgrad gleich dem gespeicherten auf zwei Stellen, Brennwertbetrieb 100 % der Stunden und der Wärme
  am Rückfall-Rücklauf 50 °C, Starts des Laufs); Fassungsasserts in `VorlagenfeldkatalogWacheTests` und
  `VorlagenfeldErgebnisstellenTests`.

## 4 Papiere

Konzept auf „vollständig umgesetzt“ gezogen (Kopf, Abschnitte 5 und 6, „Stand E5“ in 7) und per `git mv` nach
`Dokumentation/ueberholt/`; Index (Zeile unter `ueberholt`, Ordnerzähler Protokolle/Simulation 76), Verweise in
`Konzept_Simulationsablauf_EPOS-Plan.md`, `Status_iOS_Migration.md`, dem Übergabepapier der Sitzung „Dialoge und
Korrekturen“, SK3–SK7 und den Codekommentaren von `KesselKennlinieSchema` und `KesselBrennwertNachzug` nachgezogen.
`Proben/ChartProben/LIESMICH.md` und `Werkzeuge/Berichtsvorlage/LIESMICH.md` fortgeschrieben.

## 5 Gate

Gate auf dem Merge-Stand `c5b5f9e3` (enthält origin `edc2f463`: Statuszeilen #629 und #630; Testdatenbank unverändert
`5d59041f…`), Linux, `TMPDIR=/dev/shm`, `Werkzeuge/Gate/gate_linux.sh` samt Werkzeugtests, SQL-Prüfer und Schale:

- Kern-Filter Release: 0 Fehler.
- Tests mit den xUnit-Schaltern: KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 (1 übersprungen),
  EPOS.UI.Tests 7 124, EPOS.Kern.Tests 9 535 (1 übersprungen) — zusammen **17 621 grün, 0 rot**; Dokumentationswachen 35
  grün.
- Werkzeugtests mit normaler Build-Ausgabe: Formularkarte 124, Auslieferungsvorlage 44, Gebaeudevergleich 24,
  ZapfprofilValidierung 39 — grün; `Werkzeuge/Berichtsvorlage` führt kein eigenes Testprojekt, seine Wachen
  (`BerichtsvorlageDateiWacheTests`, `AuslieferungsvorlagenWacheTests`, `WordBausteinvorlageTests`,
  `AusfuehrlichRundlaufTests`, `KurzberichtRundlaufTests`, `VorlagenprueferTests`) laufen in EPOS.Kern.Tests grün mit.
- SQL-Prüfer: Selbsttest 35/0; 2 145 Texte, 0 Fundstellen.
- ChartProben: 237 Bilder, 0 Verstöße; 200 Hashes gleich der Linux-Messlatte `Messlatte_2026-09-30.sha256`.
- `EPOS.Referenzlauf` über alle sechzehn Projekte gegen R29: **GESAMT PASS, 487/487 CSV byte-gleich**; gestört
  (`--stoerung ulp`) gegen ungestört 16/16 PASS.
- Windows-Schale (`EnableWindowsTargeting=true`, Debug x64): 0 Fehler.

## 6 Wiki und Logbuch

Wiki-Quellen: `Programm Dokumentation - Heizkessel.wiki` (Absatz zum Diagramm „Wirkungsgrad über der Last“ in der Gruppe
„Kennlinie“), `Programm Dokumentation - Berichtsvorlagen.wiki` (Marke und Inhalt der Tabelle
`{{stand.tabelle.heizkessel}}`, Schalter, ausführliche Vorlage). Tabu-Grep über die neuen Zeilen leer, keine
Produktdaten. Upload mit dem nächsten Sammel-Upload.

Logbuch unter **1.2.0.6** (Vorschlag):
- „Der Kesseleditor zeigt in der Gruppe ‚Kennlinie‘ den Wirkungsgrad über der Last als Diagramm, beim Brennwertkessel mit
  Brennwertkennlinie für 30, 50 und 60 °C Rücklauf.“
- „Berichtsvorlagen können je Variante die Tabelle der Heizkessel mit Jahresnutzungsgrad, Brennwertanteil und Starts
  zeigen.“

## 7 Offene Punkte

1. Statuszeile in `Status_iOS_Migration.md` beim Zusammenführen.
2. Windows: die Messliste des Gates um die sechs neuen Bilder ergänzen; ein Windows-Lauf gegen R29 steht wie nach SK7
   aus.
3. Aus Abschnitt 5 des Konzepts nicht Gegenstand des Auftrags und nicht umgesetzt: Zeitreihen η_eff und T_RL im Export
   hat E2/E3 gebracht; eine eigene Spalte „Wärme im Brennwertbetrieb“ je Kessel im Reiter gibt es nicht (der Reiter zeigt
   den Wärmeanteil über alle Kessel, der Bericht je Kessel).
