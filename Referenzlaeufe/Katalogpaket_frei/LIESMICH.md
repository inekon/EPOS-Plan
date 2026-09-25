# Freier Paketteil des Zapfprofilgenerators

Die freien Katalogdaten des Zapfprofilgenerators — Daten, die im Repositorium stehen dürfen
(Umsetzungskonzept Zapfprofilgenerator, Kapitel 6, Zeile „frei") und ohne die eine Auslieferung
weder stochastisch rechnet noch die Bedarfstag-Quelle (5) anbietet, weder Liter noch Stunden über
der Schwelle anzeigt noch eine große Zirkulation nennt. Sie stehen **einmal** hier:

- [`Werkzeuge/Auslieferungsvorlage`](../../Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs) spielt den
  Ordner in **jede** Vorlage ein (`PaketteilEinspielen`), nach dem externen `--katalogpaket`;
- [`Skripte/tww_testkatalog_fiktiv.py`](../Skripte/tww_testkatalog_fiktiv.py) schreibt dieselben
  Zeilen in die Testdatenbank;
- die Wache `EPOS.Kern.Tests/TwwKatalogWacheTests.Die_freien_Zeilen_der_Testdatenbank_gleichen_dem_Paketteil`
  hält Testdatenbank und Dateien gleich.

## Format

Paketformat N2 des Umsetzungskonzepts (Kapitel 6 (b)): je Tabelle `<Tabelle>.csv`, UTF-8,
Kopfzeile mit den Spaltennamen der Tabelle, Trenner `;`, Zahlen mit Punkt, leeres Feld = NULL.
Dazu drei Regeln des Paketteils:

1. **Jede Zeile** trägt Herkunftsart `FREI`, Status `AUSLIEFERUNG` und `ReadOnly` 1.
2. **Keine Katalogversion.** Die Zeilen treten der Katalogversion des Katalogs bei, in den sie
   kommen — in der Vorlage der des zuletzt angelegten Parameters (sonst `FREI-1`), in der
   Testdatenbank `TEST-1`. Sonst sähe der Parametersatz, der nur eine Katalogversion liest, die
   Parameter der Stochastik nicht. `Version` (Provenienz) nennt den Stand des Paketteils.
3. **Schlüssel des Pakets.** Die `ID` eines Bedarfstags verknüpft ihn mit seinen Ereignissen
   (`ID_Bedarfstag`); die Datenbank vergibt die echte ID. Die Zapfkategorien führen **keine**
   `ID_Nutzungsart`: Sie sind Vorgabesätze, von denen jede Nutzungsart mit Status `AUSLIEFERUNG`
   ohne eigene Kategorien **den Satz ihrer Gruppe** bekommt (in der Testdatenbank jede Nutzungsart
   des Testkatalogs).
4. **Die Steuerspalte `Gruppe`** (nur `Tab_TwwZapfkategorie_STAMM.csv`) ist die **einzige** Spalte
   des Paketteils, die keine Spalte ihrer Tabelle ist; sie wird nie geschrieben. Sie trägt
   `Wohnen` oder `Nichtwohnen` und sagt, an welche Nutzungsarten der Satz bindet: Die
   **Kalenderart** entscheidet — `Wohnen` (1) heißt Wohnnutzung, jede andere (Arbeitstage,
   Schulferien, Betrieb, Auslastungsgang) Nichtwohnen (`TwwSchema.Kategoriengruppe`, dieselbe
   Regel in Auslieferungsvorlage, Einspielskript und Kern; die Bezugsart trennt nicht, weil
   Personen Wohn- wie Nichtwohnnutzungen tragen). Leer bleibt sie nur in einem älteren Paketteil
   ohne Gruppen — dann bindet der eine Satz an jede Nutzungsart. Fehlt der Satz einer Gruppe,
   bleiben ihre Nutzungsarten ohne Kategorien und rechnen nicht stochastisch; der Prüfbericht
   meldet das.

Führt das externe Katalogpaket dieselbe Zeile (Parameter: Schlüssel; Bedarfstag: Bezeichner —
jeweils in derselben Katalogversion) oder für eine Nutzungsart eigene Kategorien, gilt das
Katalogpaket; der Prüfbericht meldet die Schlüsselgleichheit.

## Inhalt und Quellen

| Datei | Zeilen | Inhalt | Quelle |
|---|---|---|---|
| `Tab_TwwBedarfstag_STAMM.csv` | 1 | Ecodesign-Zapfprofil L, Bedarfstag der Art 5, Bezugsart Wohneinheiten (2, ab Schemastand 124: das Lastprofil beschreibt einen Haushalt), ohne Bezugsmenge (nicht skaliert) | Verordnung (EU) Nr. 814/2013 der Kommission, Anhang III, Tabelle 1, Lastprofil L (ABl. L 239 vom 6.9.2013) — EU-Recht |
| `Tab_TwwBedarfstagEreignis_STAMM.csv` | 24 | die 24 Zapfungen: Beginn, Dauer, Energie Q_tap; Tagessumme = Q_ref | wie oben; die Dauer ist eine Setzung der Umsetzung (siehe unten) |
| `Tab_TwwParameter_STAMM.csv` | 13 | `Zapfprofil.Stochastik.*`: Urlaubsversatz, Vielfaches der Mindestzahl, Konsistenzschwelle, Quantile P95 und P99; `Zapfprofil.Zirkulation.Hinweisverhaeltnis` (Hinweis, wenn die Zirkulation mehr als das 1,5-Fache der Zapfung verliert); `Zapfprofil.Anzeigetemperatur` (45 °C, Literanzeige) und `Zapfprofil.Stundenschwelle` (0,1 kW, Stunden über der Schwelle) — die Vorgaben der Anzeige, wenn weder Dialog noch Einstellung eine nennen; `Zapfprofil.Validierung.*`: die fünf Setzungen der Validierung gegen eine Messreihe — Bandgrenzen der synthetischen Spitze (0,85 und 0,95), Formschwelle des Tagesgangs (0,01), höchster Lückenanteil einer Messreihe (0,05) und kürzeste Reihe für einen Kalibriervorschlag (30 d) | Quantile: Standardnormalverteilung; die übrigen: Setzungen des Zapfprofilgenerators (Umsetzungskonzept 4.4, 4.0, 4.6 und Warnlogik der Stufe Z4; Konsistenzschwelle nach der Warnlogik des Konzepts TWW-Zapfprofile) |
| `Tab_TwwZapfkategorie_STAMM.csv` | 6 | **zwei Vorgabesätze** (Steuerspalte `Gruppe`): Wohnen mit vier Kategorien (Kurzzapfung, mittlere Zapfung, Wannenbad, Dusche), Nichtwohnen mit zwei (Kurzzapfung, Duschzapfung) — je Kategorie mittlerer Volumenstrom, Dauer, Anteil, Streuung, Kappung | Wohnen: Jordan/Vajen, IEA SHC Task 26 — die Parametrik des Einfamilienhauses, wie sie das Protokoll der DHWcalc-Referenzdatei [im Testordner](../../EPOS.Kern.Tests/Proben/Zapfprofil/OpenDHW/LIESMICH.md) ausweist; Nichtwohnen: **Modellannahme** nach dem OpenDHW-Muster (zwei Kategorien statt vier) — beide **Modellannahme** |

**Dauer der Ecodesign-Zapfungen.** Die Tabelle der Verordnung nennt Energie, Volumenstrom und
Temperaturen, keine Dauer. Setzung: Dauer = Volumen / Volumenstrom, Volumen = Q_tap / (c_w ·
(Nutztemperatur − 10 °C)), Nutztemperatur = Spitzentemperatur, wo angegeben, sonst die
Mindesttemperatur; ganze Minuten kaufmännisch, mindestens 1. Die Energie jeder Zapfung ist Q_tap
unverändert.

**Zapfkategorien, Gruppe Wohnen.** Vier Kategorien ohne obere Kappung (`Kappung_l_min` leer); die
Streuung je Kategorie wie im DHWcalc-Protokoll.

**Zapfkategorien, Gruppe Nichtwohnen** (Konzept 4.4: „für Nichtwohnen zwei Kategorien nach dem
OpenDHW-Muster"). Das Muster ist die **Zahl und Art der Kategorien** — eine Kurzzapfung am
Waschtisch und eine Zapfung in Höhe einer Dusche —, nicht ein übernommener Zahlenwert. Volumenstrom,
Dauer, Anteil, Streuung und Kappung sind **freie Modellannahmen** von INEKON mit runden Werten:
Kurzzapfung 2 l/min über 1 Minute mit Anteil 0,6, Duschzapfung 8 l/min über 5 Minuten mit Anteil
0,4, Streuung je 1 l/min, Kappung 6 bzw. 20 l/min (was eine Armatur bzw. eine Brause höchstens
gibt). Die Anteile summieren wie im Wohnsatz auf 1; die Kappung begrenzt den gezogenen Volumenstrom
nach oben und geht in das doppelt gestutzte Mittel der λ-Kalibrierung ein. Gemessene Reihen ändern
daran nichts: Die Kalibrierung einer Nichtwohn-Zone (Stufe Z5) schreibt Tagesbedarf, Wochenfaktoren
und Tagesgänge in eine Anwenderkopie und nimmt die Kategorien unverändert mit.

**Setzungen zur Bestätigung (ZU21).** Urlaubsversatz, Vielfaches, Konsistenzschwelle, das
Hinweisverhältnis der Zirkulation, die Anzeigetemperatur, die Stundenschwelle, die fünf
Setzungen der Validierung (`Zapfprofil.Validierung.*`) und die **fünf Werte je Kategorie des
Nichtwohnsatzes** samt der Gruppenregel (Kalenderart) sind Setzungen
von INEKON; der Anwender bestätigt oder ändert sie vor der ersten Auslieferung. Ohne
Hinweisverhältnis entfällt der Hinweis zur Zirkulation; ohne Anzeigetemperatur bzw. Stundenschwelle
entfällt die Literanzeige bzw. die Zählung, sofern weder der Dialog noch die Einstellung einen Wert
nennt.

Keine Normzahl, kein Hersteller- oder Produktwert.
