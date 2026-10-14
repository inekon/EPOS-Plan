# SK-3 — Heizgrenze der Kesselbereitschaft, Schemaschritt 154, Basis R24 (#595)

Stand: 29.09.2026 · Zweig `ios_migration_september` · Commits `393ebe0f` Schemaschritt (gebaut als 152),
`6cfe3fbb` Kern, `fb2426e8` Oberfläche, `cb821307` Basis R24/R23 archiviert, `8ad7e2ba`
KI-Abdeckung, `14973783` Papiere und Wiki-Quellen (Opus-Agent im Worktree); Merge `36e6ee67`.
Schemaschritt 154 (beim Zusammenführen hinter KP1b und die Bezugsart Zimmer umnummeriert). Anwenderentscheid 27.09.2026 („Tagesmittel < 15 °C und individuelle Vorgabe“)
zu Punkt (d) aus „Nach #568“ in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).
Statuszeile „#595“ in derselben Datei.

## 1 Anlass

SK-2 (#568, [`SK2_Kessel_Bereitschaft_Stunden_R23_Protokoll.md`](SK2_Kessel_Bereitschaft_Stunden_R23_Protokoll.md))
ließ den Kessel nur in betriebsbereiten Stunden den Bereitschaftsverlust tragen, nahm als Heiztag
aber jeden Tag mit „Tagessumme Raumwärmebedarf > 0“. Bei den VDI-6007-Gebäuden trifft das im
Mittel auf **326 von 365 Tagen** zu, beim Tagesbilanz-Weg (Projekt 1040) ebenso auf 326: Der Kessel
war fast ganzjährig betriebsbereit, das Kriterium unterschied kaum. Punkt 4 der offenen Punkte
von SK-2 hatte das benannt; der Anwender entschied am 27.09.2026 für das **Tagesmittel der
Außentemperatur unter einer Heizgrenze**, Vorgabe 15 °C, je Projekt vorgebbar.

Messung aus den R23-Reihen (Skript im Scratchpad der Sitzung, nicht im Repository): Für die
Kandidaten Tagesmittel < 15 °C und < 12 °C liegt der Anteil des Raumwärmebedarfs, der auf
Nicht-Heiztage fiele, bei **2,4 %** bzw. **7,6 %**. Eine Regel genügt für alle
Gebäudemodelle.

## 2 Umsetzung

- **Regel** (`6cfe3fbb`, `SimulationSPK.IstBetriebsbereit`, `HeiztageAus`): Ein stillstehender
  Kessel ist betriebsbereit, wenn der Tag ein Heiztag ist oder er in den 24 Stunden davor gelaufen
  ist. **Heiztag** ist ein Tag, dessen Mittel der Außentemperatur des Laufs über die 24 Stunden
  **unter der Heizgrenze** liegt — strikt, genau auf der Grenze ist kein Heiztag, der Vergleich
  trägt den Rechenrand. Vorgabe `HEIZGRENZE_VORGABE_C` = 15 °C; ohne Temperaturreihe ist jeder Tag
  Heiztag. Nachlauf von 24 Stunden und Deckel `Kessel_Betriebsbereitschaft` bleiben unverändert;
  der Raumwärmebedarf vor der Kaskade ist keine Quelle mehr. Beide Kesselwege (Speicherstufe,
  Vektorstufe) nehmen Reihe und Grenze an der Stelle der Betriebsbereitschaft.
- **Schemaschritt 154** (gebaut als 152 in `393ebe0f`, beim Zusammenführen umnummeriert; `KesselHeizgrenzeSchema`, Eintrag in `Paketanhebung.STUFEN`,
  Art `Ddl`): `Tab_Einstellungen.Kessel_Heizgrenze` REAL, nullbar, ohne `DEFAULT` und ohne
  `CHECK`, NULL = Vorgabe; reines DDL, wiederholbar, ergebnisneutral (die Wirkung kommt mit der
  Regel, nicht mit der Spalte). Eine Quelle für Migration der Schale, Werkzeug
  `Testdatenbankschema` und Testvorrichtung; die Nummer war gegen `origin` gemessen (Zielversion
  151 → 152); vor dem Push belegten KP1b der Gebäudesimulation die 152 und die Bezugsart Zimmer des
  Zapfprofilgenerators die 153; die Heizgrenze folgt als 154
  (`KesselHeizgrenzeSchema.SCHRITT = TwwBezugsartSchema.SCHRITT + 1`). Die Gleichheitsproben „Zielversion == 151“ in `KonditionierungCtrlTests` und
  `SolarkollektorTemperaturenTests` stehen jetzt auf `>=`.
- **Konfiguration und Ergebnis**: `KonfigurationModel`/`KonfigurationCtrl` lesen und schreiben die
  Spalte (leer = NULL); die Oberfläche nimmt 0 bis 30 °C an. Das Laufprotokoll nennt Heizgrenze und
  Zahl der Heiztage, das Kesselergebnis trägt beide.
- **Oberfläche** (`fb2426e8`): Feld „Heizgrenze der Kesselbereitschaft“ im Konfigurationsdialog der
  Komponenten, Hinweis im Heizkessel-Reiter mit der wirksamen Heizgrenze. Der Assistent kennt das
  Feld `kessel_heizgrenze`; die Feldzahl der Simulationsmaske steht bei 48 statt 47, und
  `KiMaskenabdeckungWacheTests` zählt am Konfigurationsdialog eine vierte Eingabestelle
  (`8ad7e2ba`).
- **Tests**: `KesselBereitschaftTests` (Regel, Grenzfall, Vorgabe, eigene Grenze im Lauf —
  1007 bei 12 °C: 202 Heiztage, 3 488 Bereitschaftsstunden; bei 15 °C: 254 Heiztage, 4 335 statt
  5 931 —, fehlende Reihe, Plausibilität 0 bis 30, Rundreise über `KonfigurationCtrl`),
  `KesselHeizgrenzeSchemaTests`, `KomponentenKonfigurationDialogTests`.
- **Testdatenbank** `Referenzlaeufe/Kenndaten_Test.sqlite`: gebaut auf Schemastand 152 (LFS-SHA-256
  `6f0d5458…`, 71 577 600 Byte); nach dem Zusammenführen aus der Fassung `621cf64a…` (Schemastand 153) neu
  nachgezogen auf Schemastand **154** (LFS-SHA-256
  `2e417b36e68147fd4c90fea562df64c0c2dad815142780df83f2004163c6d0b2`, 71 622 656 Byte): einzige
  Zelländerungen SchemaVersion 153 → 154 und die neue Spalte (alle 25 Zeilen NULL); die fünfzehn
  Projekte rechnen darauf byte-gleich zu R24 (460/460 CSV).

## 3 Gegenprobe gegen R23 (15 Projekte)

A/B R23 → R24, **beide auf Linux und allein der Rechenweg**: **448/460 CSV byte-gleich**, alle
Zeitreihen byte-gleich — verschieden sind nur zwölf `aggregate.csv`.

| Projekt | Bereitschaftsstunden R23 → R24 | Kessel-Verbrauch [MWh] | Jahresnutzungsgrad [%] |
|---|---|---|---|
| 1007, 1046 (Gas) | 5 931 → 4 335 | 10,63 → 10,55 | 85,16 → 85,80 |
| 1008 (Gas) | 5 260 → 3 793 | 23,79 → 23,72 | 86,63 → 86,90 |
| 1017 (Elektrokessel) | 4 534 → 4 482 | — | 98,24 → 98,26 |
| 1047 (Elektrokessel) | 7 401 → 6 111 | — | 70,05 → 73,86 |

Bei 1024, 1030, 1039, 1040, 1042, 1045 und 1049 wandern nur die Stunden (Bereitschaftsleistung
0 kW, kein Verlust in kWh). Unverändert bleiben 1018, 1023 (3 702 Stunden) und 1041. Der zweite
Lauf ist mit dem ersten 460/460 CSV byte-gleich (Determinismus).

## 4 Neue Basis R24

`2026-09-27_R24_Heizgrenze` (Ordnername nach Auftrag, eingefroren am 29.09.2026) — fünfzehn
Projekte (1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047,
1049), 460 CSV, 2 685 Skalare, gerechnet gegen `Kenndaten_Test.sqlite` (Schemastand 152, `6f0d5458…`;
nachgerechnet byte-gleich auf Schemastand 154, siehe Abschnitt 2), **auf Linux eingefroren** (R23 war auf Windows eingefroren). R23
(`2026-09-26_R23_KesselBereitschaft`) archiviert unter
[`Dokumentation/ueberholt/Referenzbasen/`](../../Referenzbasen/2026-09-26_R23_KesselBereitschaft/protokoll.txt).
Herleitung, Aufbau und Wegweiser: [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md).

**Plattformbefund.** Der unveränderte Rechenweg R23 rechnet auf Linux in 19 Dateien anders als auf
Windows: bei 1008, 1023 und 1042 jenseits der Toleranz (eine Schwelle kippt am letzten Bit), bei
1007/1046 (`heizstab.csv`) und 1024 innerhalb. Keines der sieben CI-Projekte liegt jenseits; die CI
rechnet auf ubuntu und ist davon nicht berührt. Folge: **Ein Windows-Lauf gegen R24 zeigt bei 1008, 1023 und 1042 FAIL**,
bis die Schwelle robust gemacht ist (Abschnitt 6, Punkt 1).

**Einfrierregel.** Neu in [`CLAUDE.md`](../../../../CLAUDE.md) („Regressionsnetz“) und
`Referenzlaeufe/LIESMICH.md`: „gesäte Kesseldaten“ — `Tab_Einstellungen.Kessel_Heizgrenze` und
`Kessel_Betriebsbereitschaft` eines Referenzprojekts und die Bereitschaftsleistung seines Kessels.

```bash
dotnet build EPOS.Referenzlauf/EPOS.Referenzlauf.csproj -c Release
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf \
  --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
  --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047,1049 \
  --ziel Referenzlaeufe/2026-09-27_R24_Heizgrenze
```

## 5 Papiere

- Konzept Simulationsablauf § 12 (Heiztag, Heizgrenze, Laufprotokoll, Basis R24).
- `Referenzlaeufe/LIESMICH.md` und `CLAUDE.md` „Regressionsnetz“ (Basisname, Einfrierregel).
- Vier Papiere mit „aktuelle Basis“ auf R24: Konzept Gebäudesimulation VDI 6007, Systementwurf
  Gebäudesimulation, Umsetzungskonzept Zapfprofilgenerator, Konzept Wirtschaftlichkeit
  (konsolidiert).
- Wiki-Quellen, **kein Upload**: `Projekte/Wiki/Programm Dokumentation - Simulation.wiki`
  (Abschnitt „Betriebsbereitschaft des Heizkessels“, Anker `kesselbereitschaft`; Laufparameter
  hinter „Konfiguration…“ mit OK), `Programm Dokumentation - Gerätekataloge.wiki` (Bereitschaftsverlust
  nur in betriebsbereiten Stunden), `Programm Dokumentation - Hilfe-Assistent.wiki` (Heizgrenze
  unter den Laufparametern).
- Logbuch-Satz (ersetzt den #568-Satz unter 1.2.0.5): „Die Bereitschaftsverluste des Heizkessels
  fallen nur an, solange er betriebsbereit ist – an Tagen unter der je Projekt einstellbaren
  Heizgrenze (Vorgabe 15 °C Tagesmittel) oder bis 24 Stunden nach dem letzten Lauf.“

## Gate

Gate auf `4a8e8bff` (Heizgrenze samt origin bis `c7b9dd70`, Schritt noch 152): Kern-Filter 0 Fehler; Tests 16 717 grün, 0 rot (2 übersprungen); Werkzeugtests grün; SQL-Prüfer 0 Fundstellen; ChartProben 221 Bilder, 0 Verstöße; Referenzlauf der sieben CI-Projekte gegen R24 PASS; Windows-Schale 0 Fehler. Nach dem Merge mit origin `9166c630` (KP1b, Schritt 153; Merge `9030d3e8`) volles Gate erneut grün (Tests 16 745, SQL 2 077 Texte, ChartProben, R24 PASS, Windows-Schale). Nach dem Merge mit origin `ea8d050d` (Bezugsart Zimmer, Schritt 154; Merge `36e6ee67`): fünfzehn Projekte byte-gleich zu R24 (460/460 CSV), 1 128 gezielte Kern-Tests (Schema, Wachen, Kessel, Paketanhebung, Konfiguration, Tww, Transfer), `EPOS.UI.Tests` 6 887, Auslieferungsvorlage 39 grün, SQL-Prüfer 0 Fundstellen (2 097 Texte), Windows-Schale 0 Fehler`.

## 6 Offene Punkte

Siehe „Nach #568“ in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md):

1. Ein Windows-Lauf gegen R24 zeigt bei 1008, 1023 und 1042 FAIL. Die Schwelle, die am letzten Bit
   kippt, ist zu finden und robust zu machen — eigener Auftrag (Rechenweg, neue Basis).
2. ~~Kollision mit KP1b der Gebäudesimulation~~ gelöst beim Zusammenführen
   ([`Entwurf_KP1b_Konditionierungsprofile.md`](../../Entwurf_KP1b_Konditionierungsprofile.md)):
   KP1b ist Schritt 152, die Bezugsart Zimmer 153, die Heizgrenze 154.
3. [`Konzept_Kessel_Kennlinie_EPOS-Plan.md`](../../Konzept_Kessel_Kennlinie_EPOS-Plan.md)
   (#569, zurückgestellt) plant noch mit „Neueinfrierung R23“ und Schritt 151 — bei Wiederaufnahme
   neu messen.
4. Sichtabnahme des Feldes „Heizgrenze der Kesselbereitschaft“ in der Windows-Anwendung.
