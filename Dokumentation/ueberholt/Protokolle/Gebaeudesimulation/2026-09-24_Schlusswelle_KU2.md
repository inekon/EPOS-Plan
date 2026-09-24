# Protokoll: Schlusswelle KU2 der Kühlung (24.09.2026)

**Auftrag** (vom Anwender am 24.09.2026 beauftragt, samt dem Einfrieren): vierte und letzte Welle der
Stufe KU2 — Entscheid E35 (ein eigener Zähler trägt Grund- und Leistungspreis seines Kühlträgers), die
Reste von E34, das Referenzprojekt mit Kälteerzeuger samt Rechenprobe je Vorlauf und die neue Basis.
Maßgeblich: [Kühlkonzept](../../../aktuell/Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 6.1–6.3,
10.3–10.5 und 11.1, [Konzept Gebäudesimulation](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
N1.39 und N1.40.

## 1 Umsetzung

| Teil | Stand |
|---|---|
| E35 in den Papieren | Konzept N1.40 und Kopf; Status Zeile E35; Register (Vermerk unter K9, Kopf); Kühlkonzept Kopf, 6.1, 6.2, 6.3, 12; Indexzeilen |
| E35 im Kern | `Kaeltestromabrechnung.EigeneZaehler` (je Anlage ein Zähler, auch ohne Kältestrom); `KostenEmissionRechner` setzt je Zähler den Grundpreis des Kühlträgers an und dessen Leistungspreis auf die eigene Spitze — dieselbe Regel wie beim Projektträger, herausgezogen als `LeistungsanteilStrom` (Staffel vor Saisonreihe vor Satz je Monat oder Jahr); die eigene Spitze bildet `ZeitreihenExtraktor` aus der Stundenreihe der Anlage (`ZeitreihenSatz.Kaeltestromspitzen`, Schlüssel Modulplatz); `StromLeistungspreisGepflegt` fragt den Kühlträger eines eigenen Zählers mit; ohne Zeitreihen steht der Grundpreis, der Leistungspreis wird benannt; Szenariopreise (E9a) wie beim Arbeitspreis |
| E35 im Ausweis | Energiekosten je Anlage: Zeilen „Grundpreis Kältestromzähler …" (1 a × Grundpreis) und „Leistungspreis Kältestromzähler …" (Spitze × Satz), beide Sprachen; Kosten des Kältestroms und Leistungsanteil der Energiekosten enthalten sie; der Rollentarif lässt sie stehen |
| Reste von E34 | Preis des vermiedenen Bezugs und des PV-Mehrbezugs aus dem Projektträger und der Vorrangkette der Energiekosten statt einer eigenen `LIMIT 1`-Abfrage (`StromArbeitspreisEurJeKwh`); Bemessungsmenge nach § 9b StromStG samt dem Kältestrom eigener Zähler, genau einmal (`NetzbezugFuerStromsteuer`); `SzenarioMengen` skaliert die Kälteseite der Wärmepumpe samt Kühlträger und Abrechnungsart |
| Referenzprojekt 1017 | vier Zellen und zehn Zeilen über `Referenzlaeufe/Skripte/kaelteerzeuger_1017_referenzprojekt.py` (Abschnitt 3) |
| Einfrierregel „gesäte Kältedaten" | um die Kälteerzeugung erweitert, in `Referenzlaeufe/LIESMICH.md` und im Abschnitt „Regressionsnetz" der `CLAUDE.md` |
| Tests | `KaeltestromAbrechnungTests` +5 (E35 mit Zahlen, zwei Zähler, Preis des vermiedenen Bezugs samt Gegenprobe, § 9b-Menge, Mengenszenario); `ReferenzprojektKaelteerzeugerTests` neu (Saat, Rechenprobe je Vorlauf, Kosten und CO₂); nachgezogen auf die Saat: `SimulationLaufCtrlTests` (1017 im Modell ohne Platz), `KuehlungErzeugerSchemaTests` (zwei Fälle) |
| Neue Basis | `Referenzlaeufe/2026-09-24_R14_Kaelteerzeuger` (Abschnitt 5) |
| Wiki | „Kühlung" (eigener Zähler, Zeilen der Wirtschaftlichkeit) und „Simulationsergebnisse" (Block und Ring „Kältedeckung", Block „Kälte") — Repo-Quellen, gegen das Verbotsmuster gegengelesen, nicht hochgeladen; kein eigener Logbuch-Satz (Abschnitt 7) |

## 2 Entscheid E35 — die Probe

An einer Arbeitskopie von 1045 (reversible Wärmepumpe, Phantasiewerte: Kühlträger 0,20 €/kWh,
Grundpreis 120 €/a, Leistungspreis 100 €/(kW·a)): 0,23 MWh/a Kältestrom über den eigenen Zähler, eigene
Spitze 2,711 kW (die höchste Stunde der Kältestromreihe, von Hand nachgerechnet samt den zwölf
Monatsspitzen). Kosten des Kühlträgers **46,00 + 120,00 + 271,06 = 437,06 €/a**; anteilig am Netzbezug
22,00 €/a (kein Grund- und kein Leistungspreis); ohne Zeitreihen 166,00 €/a, der Leistungspreis benannt;
mit einer Staffel (1 kW zu 50, darüber 200 €/(kW·a)) 392,12 €/a. Die Energiekosten wachsen genau um
Grund- und Leistungspreis, die Stromkosten des Anschlusses bleiben. Zwei Anlagen mit demselben
Kühlträger und eigenem Zähler sind zwei Zähler — zwei Grundpreise, einer davon auch ohne Kältestrom.

**Die Frage „ein oder zwei Zähler"** ist so beantwortet: Die Abrechnungsart steht je Anlage (E34), und
das Datenmodell kennt keinen Zähler, den mehrere Anlagen teilen. Ein gemeinsamer Zähler bräuchte eine
eigene Zählerzuordnung an der Anlagenzeile — benannt in N1.40, nicht gebaut.

## 3 Teil B — die Reste von E34

- **Preis des vermiedenen Bezugs.** `WirtschaftlichkeitCtrl.StromArbeitspreisEurJeKwh` las den
  Arbeitspreis über eine eigene Abfrage — irgendeinen dem Projekt zugeordneten Stromträger (`LIMIT 1`,
  über den Index auf `ID_Projekt` die zuerst zugeordnete Zeile) und ohne Preisstand. Mit einem
  Kühlträger im Projekt konnte das dessen Preis sein. **Gegenprobe:** An 1045 mit der Zuordnung des
  Kühlträgers vorn lieferte die alte Abfrage 0,20 statt 0,30 €/kWh; jetzt der Projektträger
  (`Kaeltestromabrechnung.Projekttraeger`) mit `KostenEmissionRechner.ArbeitspreisJeKwh` — im Szenario
  mit dem wirksamen Szenariopreis. Die Photovoltaik deckt den Kältestrom eines eigenen Zählers nie; ihr
  vermiedener Bezug zählt ihn damit nicht.
- **§ 9b StromStG.** Die Bemessungsmenge war der Netzbezug des Anschlusses; der Kältestrom eines eigenen
  Zählers — versteuerter Netzstrom neben dem Anschluss — fehlte. Er zählt jetzt genau einmal hinzu,
  nicht als Netzbezug des Projektträgers und nicht als vermiedener Bezug (die vermiedene Menge ist
  Bedarf minus Restbezug des Anschlusses).
- **Mengenszenario (E9a).** `SzenarioMengen.Ergebnis` kopierte die Kälteseite der Wärmepumpe nicht mit:
  Im Szenario verschwand der Kühlträger samt Menge und Kosten. Jetzt skaliert sie wie die Wärmeseite;
  Kühlträger und Abrechnungsart bleiben, die eigenen Spitzen folgen dem Faktor.
- **`Emissionsquelle.StromTraeger`, geprüft, nicht geändert.** Wählt keine Anlage ihren Stromträger,
  nimmt Stufe 2 per `LIMIT 1` einen der zugeordneten — über den Index auf (`ID_Projekt`,
  `ID_Energieträger`) den mit der kleinsten Kennung, ohne dass die Abfrage eine Reihenfolge verlangt.
  **Die Kühlseite ist betroffen:** Ein Kühlträger muss dem Projekt zugeordnet sein (E33); ist er der
  Träger, den Stufe 2 greift, wird er zum Projektträger, der ganze Netzbezug trägt seinen Preis und
  Faktor, und die Kühlwahl wirkt nicht. Abhilfe für den Anwender: den Stromträger des Heizbetriebs an
  der Wärmepumpe wählen. 1017 führt zwei Stromträger ohne Anlagenwahl; sein Kältestrom trägt keinen
  Kühlträger und rechnet mit dem Träger aus Stufe 2. Benannt in Kühlkonzept 6.3; die Regel gehört
  nicht zur Kühlung.

## 4 Das Referenzprojekt mit Kälteerzeuger

| Zelle bzw. Zeile | vorher | nachher | Begründung |
|---|---|---|---|
| `Tab_Einstellungen.Tool_3` (1017) | leer | „Wärmepumpe" | Kaskadenplatz 3 hinter BHKW und Elektrokessel: ohne Platz rechnet die Maschine nicht, auf Platz 3 bleibt die Wärmeseite fast unverändert — der Platz der Proben der Wellen 2 und 3 |
| `Tab_WP.Kuehlbetrieb` (1017033) | 0 | 1 | die Maschine kühlt |
| `Tab_WP.Kuehl_Vorlauf` | NULL | 18 | Stützstelle (K21), Flächenkühlung über dem Taupunkt — sensible Kälte (K5) |
| `Tab_WP.Kuehl_Hilfsstromanteil` | NULL | 0,05 | die Basis trägt den Zuschlag; NULL ließe den Zweig unbewacht; ein runder Beispielwert |
| `Tab_Kenndaten_Kuehlung` | leer | zehn Zeilen | gesät, weil der Katalogsatz 33 des Geräts keine Kühlkennlinie trägt: Vorlauf 7 und 18 °C × 20 bis 40 °C, Laststufe 100, EER in `COP` (K22), Kaltwasserlage, keine Dubletten — die Phantasie-Kennlinie der Proben |
| Kühlträger, Abrechnungsart, Nennkühlleistung | NULL | NULL | wie Heizbetrieb (der Referenzfall ohne E34-Sonderweg); die Nennkühlleistung ist Berichtsgröße |

Sicherung vorher außerhalb des Repositoriums; Zellvergleich aller 132 Tabellen (10 498 993 Zellen)
gegen die Fassung 119 von origin (LFS-SHA-256 `6259b348…`): genau diese vier Zellen, zehn Zeilen und die
Zeile `Tab_Kenndaten_Kuehlung` in `sqlite_sequence`; 14 Sichten und 208 Indizes gleich;
`integrity_check` ok, `foreign_key_check` leer, 67 784 704 Byte, LFS-SHA-256 `63cc2d64…`; ein zweiter
Skriptlauf ändert nichts.

**Rechenprobe gegen die Handrechnung je Vorlauf** (`ReferenzprojektKaelteerzeugerTests`): je Stunde
Kälte = min(Bedarf, Zeitanteil × Pkühl(Außentemperatur)) und Kältestrom = Kälte / EER × 1,05, EER und
Pkühl von Hand aus den Stützstellen; dazu Jahressummen, Hilfsstrom, Stunden und die gespeicherten
Modulspalten.

| Kühl-Vorlauf | gedeckt [MWh/a] | Anteil | Kältestrom [MWh/a] | Hilfsstrom [MWh/a] | EER-Jahreswert |
|---|---:|---:|---:|---:|---:|
| 18 °C (gesät) | 2,483 von 2,522 | 98,4 % | 0,549 | 0,026 | 4,524 |
| 7 °C (Arbeitskopie) | 2,318 von 2,522 | 91,9 % | 0,713 | 0,034 | 3,253 |

43 Kühltage, 396 Stunden mit Kälte; ungedeckt bleibt die Kälte an Heiztagen und in den Stunden über der
Kälteleistung der Kennlinie.

## 5 Die neue Basis

`Referenzlaeufe/2026-09-24_R14_Kaelteerzeuger`, dreizehn Projekte, 394 CSV, 2 249 Skalare; gegen R13
(387 CSV, 2 207 Skalare) bewegt sich allein 1017, zwölf Projekte sind in allen Dateien byte-gleich:

| 1017 | R13 | R14 |
|---|---:|---:|
| Kältedeckung durch die Wärmepumpe [MWh/a] | — | 2,48 (98,4 %) |
| ungedeckte Kälte [MWh/a] | 2,52 | 0,04 |
| Kältestrom, davon Hilfsstrom [MWh/a] | — | 0,55; 0,03 |
| Jahresarbeitszahl Kälte | — | 4,52 |
| Wärme der Wärmepumpe [MWh/a] | — | 0,04 |
| Restwärme [MWh/a] | 0,14 | 0,10 |
| Netzbezug [MWh/a] | 655,31 | 655,88 |
| Stromkosten des Anschlusses [€/a] (gerechnet, nicht im Export) | 326 040,51 | 326 324,06 |
| Kosten und CO₂ des Kältestroms (gerechnet) | — | 273,60 €/a; 0,24 t/a |

Stromspeicher, BHKW und Elektrokessel bleiben Zeichen für Zeichen; 1017 führt keine Photovoltaik. Die
Energiekosten und die CO₂-Summe des ganzen Projekts bleiben in 1017 wie in R13 aus — ein Brennstoff ohne
Energieträger (116,7 MWh/a), ein benannter Grund der Testdatenbank. Dateien und Schlüssel: sieben
Dateien der Wärmepumpe und 42 Skalare kommen dazu; 1 689 Werte außerhalb der Toleranz gegen R13, alle in
1017. Kein Fehlschlag, kein NaN. **Determinismus:** zweiter Lauf 394/394 CSV byte-gleich. R13 ist mit
Protokoll nach `Dokumentation/ueberholt/Referenzbasen/` gewandert, der Ordner entfernt; der Basisname
steht in `CLAUDE.md`, `kern.yml`, `ios.yml` und den Papieren mit „aktuell"-Verweis.

**Zusammenführung mit den Schemaschritten 120 und 121.** Nach dem Einfrieren ist origin zusammengeführt
worden (E10 #463: Sätze der Nutzungsdauertabelle; #468: `Tab_Gebaeude.ID_Gebaeude_Stamm`). Die
Testdatenbank ist die Fassung von origin (Schemastand 121, LFS-SHA-256 `00fbbb8b…`), auf die
`kaelteerzeuger_1017_referenzprojekt.py` erneut angewandt ist; die Saaten von KU1 und die Korrektur von
10612/233 standen schon darin (die Skripte finden nichts zu tun). Zellvergleich aller 132 Tabellen gegen die
Fassung von origin (10 499 019 Zellen): genau die vier Zellen, zehn Zeilen und die Sequenzzeile der
vierten Welle; 14 Sichten und 209 Indizes gleich, `integrity_check` ok, `foreign_key_check` leer,
67 792 896 Byte, LFS-SHA-256 `9acda529…`. **R14 rechnet darauf unverändert:** 13/13 Projekte, 394/394
CSV byte-gleich zur Basis — die Basis bleibt. Die Nachträge von origin zum R13-Abschnitt (Schritte 116
bis 118, 120, 121) stehen wortgleich im archivierten R13-Abschnitt.

## 6 Abnahme (nach der letzten Änderung)

Die Arbeit ist nach dem Einfrieren kurz pausiert worden; danach ist origin mit den Schemaschritten 120
(E10, #463) und 121 (#468) zusammengeführt worden (Abschnitt 5). Die Abnahme lief auf diesem Stand:

| Prüfung | Ergebnis |
|---|---|
| Bau des Kern-Filters | 0 Fehler |
| Windows-Schale | in einen Ordner außerhalb des Repositoriums gebaut, 0 Fehler |
| Test-Gate | Kern 5 778, UI 5 906, KiKern 542, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), alle grün — darin die Doku-Wächter, `GebaeudeRueckwegTests` und die Normfälle (lokal, AixLib; 11 von 12 im Band, Fall 11 unverändert) |
| `Auslieferungsvorlage.Tests` | 30 grün |
| ChartProben | 161 Bilder, 0 Verstöße; kein neues Bild |
| Referenzlauf gegen R14 | fünf CI-Projekte GESAMT: PASS (1 805 429 Werte), alle dreizehn GESAMT: PASS (4 207 049 Werte); je zweiter Lauf byte-gleich |
| SqlDialektPruefer | 1 772 SQL-Texte, 0 Fundstellen |

Vor dem Einfrieren (auf dem Stand vor dem ersten Zusammenführen) lief das Gate ebenfalls grün (Kern
5 650, UI 5 827), dazu `Auslieferungsvorlage.Tests` 30, SqlDialektPruefer 1 765 Texte und ChartProben.

## 7 Offen

- **Für KU3:** Kältemaschine als eigener Erzeugertyp samt Rückkühlung, freie Kühlung, Kältespeicher (K7),
  Kühlung je Zone (G6), Kühlsollwert Nacht, Export nach IFC und gbXML (Kühlkonzept 11.1); dazu die
  Erdreichregeneration durch Rückkühlung (K8c) und ein gemeinsamer Zähler mehrerer Anlagen, falls er
  gebraucht wird (N1.40).
- **Außerhalb der Kühlung, benannt:** die Stufe 2 von `Emissionsquelle.StromTraeger` (feste Reihenfolge
  oder Ausschluss eines reinen Kühlträgers; Abschnitt 3).
- **Wiki:** Upload der Seiten „Kühlung", „Simulationsergebnisse", „Simulation", „Gerätekataloge" und
  „Gebäudemodell VDI 6007" mit dem Sammel-Upload; die Logbuch-Sätze stehen unter Version 1.2.0.4 — E35
  bekommt keinen eigenen Satz (die Kühlung erscheint mit dieser Version zum ersten Mal, der Satz zu KU2
  nennt die Kosten des Kältestroms). Die Seite „Gebäude" steht weiter aus.
