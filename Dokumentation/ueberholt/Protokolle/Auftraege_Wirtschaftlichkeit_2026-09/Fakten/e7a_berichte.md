

===== Bericht 1 (2026-09-23T02:41:57.349Z) =====

**Zwischenbericht E7a, Phase 1.** Kein `dotnet test` und kein Referenzlauf.

Gebaut sind Nr. 29, Nr. 32, der DML-Teil von Nr. 30 und der Dialogtext „(bitte wählen)". Die Kern-Regel und die Kohärenzzeile zu Nr. 30, K‑1 und A20 sind zurückgestellt, weil ihr Wortlaut nicht eindeutig ist (Fragen unten). Eine Blockade für Phase 2: Mit Zielversion 101 werden drei Tests rot, solange die Repo-Testdatenbank nicht nachgezogen und committet ist (Frage 1).

## Commits
Zweig `e7` im Worktree `.claude/worktrees/e7`, Basis b4d468a4:

| Commit | Inhalt |
|---|---|
| `57ba5097` E7/1 | Nr. 29: CO₂-Grenzwert brennwertbezogen |
| `e4ce4f35` E7/2 | Nr. 30: Schemaschritt 101, leere `KWKG_Anlagenart` wird NULL; Zielversion 101 |
| `d1cef396` E7/3 | Nr. 30: Dialog zeigt „(bitte wählen)" |
| `212cb5b5` E7/4 | Nr. 32: vermiedene Menge ohne jede Eigenerzeugung |
| `8c834746` E7/5 | Anker-Tests: Kommentar „alt = neu" mit Grund je Anker |

Kern-Filter, Windows-Schale und Testdatenbankschema bauen mit 0 Fehlern. Designer-Prüfung ohne Abweichung, SQL-Prüfer 0 Fundstellen, Wächter leer.

## Was je Punkt gebaut ist
- **Nr. 29:** `SteuerGutschriftRechner.Co2JeEnergieertrag` liest bei Erdgas den Ho-Katalogfaktor 181,4 g/kWh. Die übrigen Brennstoffe rechnet er über H_i/H_s des Trägers um, wie die Energiesteuer. Ohne gepflegten Brennwert bleibt der Hi-Faktor (konservativ) mit einer Begründung. Begründung und Herkunft nennen jetzt den Wert je Anlage.
- **Nr. 30:** Schritt 101 in Migration, Werkzeug und Testvorrichtung, mit Migrationstest. Die sieben betroffenen Anlagen: 12310 (Projekt 1032, Wärmepumpe), 14819 (1043, Kessel), 14842, 14843, 14844 (1043, Puffer), 14851, 14852 (1043, Wärmepumpen). Keine davon ist ein BHKW. Ihr `KWKG_Eigenstromfall = ''` bleibt stehen, weil der Entscheid nur die Anlagenart nennt.
- **Nr. 32:**
  - `StromMatrix`: Bedarf und Lastbild ab jetzt vor Abzug der PV-Eigennutzung; neue Größe `PvEigenGesamtMWh`.
  - Der KWK-Eigenanteil bleibt min(BHKW, Bedarf nach PV).
  - Der Verteilschlüssel nimmt BHKW und PV auf.
  - Neuer Test: Anker 293.245,6 + 22.914,0 = 316.159,6 €/a über Matrix, Tarifrechner und Verteilschlüssel.

## A/B-Nachweis
Gemessen mit einem eigenen Messprogramm im Scratchpad an Kopien der Testdatenbank, je Stand zwei Wege: gebuchtes Ergebnis (wie die Anker-Tests) und frischer Lauf mit Stundenreihen.

**Die dreizehn Basisprojekte, Wirtschaftlichkeit:** alle Größen vorher = nachher, auf beiden Wegen und in allen Szenarien.
- Kapitalwert 1024: −2.896.359,13 €, 1030: −21.895.377,28 €; 1023: −639.584,90 €. Die übrigen zehn haben keinen Kapitalwert.
- Gründe:
  - Nr. 29: Kein Projekt erreicht die CO₂-Prüfung, weil Hocheffizienz und räumlicher Zusammenhang überall 0 sind.
  - Nr. 30: Schritt 101 trifft nur Nicht-BHKW-Anlagen.
  - Nr. 32: `Tab_ProjektTarif` ist leer, kein Projekt rechnet im Rollentarif. `VermiedenArbeit`, `VermiedenLeistung`, `VermiedenGesamt` und `VermiedenEntlastung9b` bleiben deshalb überall 0 → 0.

**Nr. 32, Strommatrix (frischer Lauf):** nur die Projekte mit PV ändern sich.

| Projekt | Bedarf ohne Anlage [MWh] | PV-Eigennutzung [MWh] | Spitze des Bedarfs [kW] |
|---|---|---|---|
| 1007, 1046 | 19,10 → 24,00 | 4,90 | 6,60 → 6,62 |
| 1040 | 5,40 → 8,00 | 2,60 | 3,72 (gleich) |
| 1045 | 5,94 → 8,00 | 2,07 | 3,7235 → 3,7215 |

Betroffen ist damit auch die gespeicherte Spalte `Tab_ErgebnisStromMatrix.Bedarf` und die Matrixtafel in Wort- und Excelbericht. Bei 1045 sinkt die Bedarfsspitze leicht, weil die Nachtaufnahme des Wechselrichters (negative PV-Eigennutzung, 9,3 kWh/a) nicht mehr im Bedarf steckt. Alle übrigen Projekte: unverändert.

**Vorher/Nachher-Fälle je Änderung:**

| Punkt | Fall | Vorher | Nachher | Grund |
|---|---|---|---|---|
| Nr. 29 | Probe: 1024 mit bestätigter Hocheffizienz und räumlichem Zusammenhang | Befreiung 0 € (etwa 278,6 g/kWh, über 270) | 1.680,07 €/a (262,2 g/kWh, brennwertbezogen) | Ho statt Hi; Kapitalwert gleich, weil Modus Ausweis |
| Nr. 29 | Probe: 1018 und 1030 | Befreiung wie vorher | Befreiung wie vorher, neue Herleitungszeile (196,9 / 189,1 / 211,3 g/kWh) | beide lagen schon unter 270 |
| Nr. 29 | Test-Grenzfall Erdgas | 0,00 €/a | 8.200,00 €/a | Ho-Faktor statt Hi-Faktor |
| Nr. 29 | Test-Grenzfall Heizöl | 0,00 €/a | 8.200,00 €/a | Umrechnung über H_i/H_s |
| Nr. 32 | Mockup-Beispiel, gemessen am alten und neuen Code | Menge 1.094,2 MWh, Arbeit 315.129,6 €, § 9b 21.884,0 €, allein BHKW 293.245,6 € | Menge 1.179,7 MWh, Arbeit 339.753,6 €, § 9b 23.594,0 €, BHKW 293.245,6 € + PV 22.914,0 € = 316.159,6 € | PV-Eigenverbrauch wandert in die Menge; KWK-Eigen 1.094,2 MWh unverändert |
| Nr. 30 | Schritt 101 | — | alle Werte gleich | kein Rechenweg unterscheidet '' von NULL |

## Schemaschritt, Testdatenbank, Referenzlauf
- Schritt 101 ist reines DML, Zielversion 101. An einer Kopie geprüft: sieben Zeilen werden NULL, Schemastand danach 101.
- Die Repo-Testdatenbank ist **nicht** nachgezogen, wie beauftragt. Die Annahme „sie migriert beim Öffnen" gilt aber nur für die Arbeitskopien der Tests.
- Referenzlauf ist nicht gelaufen (Phase 2). Erwartet wird Byte-Gleichheit, weil kein Punkt die Simulation berührt; dann bleibt die Basis R11.

## Ressourcenschlüssel
- Neu: `STEUER_STROMST_CO2_FAKTOR_HO`, `STEUER_STROMST_CO2_FAKTOR_UMGERECHNET`, `STEUER_STROMST_CO2_FAKTOR_HI`, `STEUER_STROMST_CO2_HERLEITUNG`, `STEUER_STROMST_CO2_HEIZWERT`.
- Geändert: `STEUER_STROMST_CO2` (nennt „brennwertbezogen"), `BHW_W_ART_LEER` („(bitte wählen)" statt „(nicht erfasst — gilt als Neuanlage)"), `WIRT_MATRIX_BEDARF_HINWEIS` („ohne jede Eigenerzeugung").

## Abweichungen vom Auftrag
- K‑1: Schemaschritt 102 ist nicht vergeben, weil der Punkt zurückgestellt ist.
- A20 ist nicht gebaut.
- Bei Nr. 30 fehlen die Kern-Regel und die Kohärenzzeile.
- Die Nummer 101 widerspricht dem Konzept: § 3.6 hatte 101 für K‑1 genannt. Die Vergabe folgt dem Auftrag.

## Offene Fragen
1. **Testdatenbank.** Mit Zielversion 101 werden `TestdatenbankSchemastandWacheTests` sowie P6 und P7 der Auslieferungsvorlage-Tests rot, solange die Repo-Testdatenbank auf 100 steht (das Werkzeug migriert nicht). Soll ich sie in Phase 2 mit `Werkzeuge/Testdatenbankschema` nachziehen (sieben Zellen, Schemastand 101), mit LFS committen und einen Nachtrag in `Referenzlaeufe/LIESMICH.md` schreiben?
2. **Nr. 30, Kern-Regel.** Wörtlich heißt „NULL → kein Zuschlag", dass auch alle BHKW mit leerer Anlagenart ihren Zuschlag verlieren. Das trifft jedes BHKW der Testdatenbank, darunter 1030 mit gepflegtem Kontingent 30.000 h und Sätzen 8/4 ct. Gemessen: KWKG-Erlös im Jahr 1 fällt von 7.315,96 € auf 0; Kapitalwert −21.895.377,28 → −21.954.815,75 € (−59.438,48 €), und der Anker 1030 bewegt sich. Das Konzept nennt als betroffen aber nur 1032 und 1043.
   - Lesart (a): wörtlich, jedes BHKW ohne Anlagenart verliert den Zuschlag.
   - Lesart (b): nur wo die Anlagenart gebraucht wird, also wenn das Kontingent abgeleitet werden muss. Das rechnet der Kern heute schon so (0 mit Grund); neu käme nur die Kohärenzzeile.
   - Soll bei (a) die Anlagenart von 1030 in der Testdatenbank gepflegt werden?
3. **K‑1.** Messung nach A2: Die Wärmeproduktion liegt je Modul vor, der Wärmeüberschuss aber nur als Projektsumme. In allen BHKW-Basisprojekten ist er 0. Also greift „Aufteilung nach P_el". Offen sind:
   - **P_el-Aufteilung:** Wird die ganze Nutzwärme nach P_el aufgeteilt, oder nur der Überschuss (die Wärme bleibt je Modul)?
   - **Kennzeichen ohne σ:** Gilt dann der Vorschlag P_el/P_th (Mockup „leer = 0,845") oder kein Zuschlag, weil das Feld der Wert ist?
   - **Wirkung auf die Mengen:** `min(Netto, Nutzwärme × σ)` an die Stelle von `stromNettoJeAnlage` gesetzt ändert nur den Anteil; bei einer einzigen Anlage bliebe der Zuschlag gleich. Werden Eigen- und Einspeisemenge proportional gekürzt, oder zuerst die Einspeisung?
   - **Ersatzweg:** Wie rechnet Fall 2, wenn sich Anlagen und Module nicht zuordnen lassen?
   - **Ort im Dialog:** Gruppe 1b (Analysepapier) oder die Überlagerung „Sätze und Herkunft" (Mockup)?
4. **A20 Förderende.**
   - Lesart (a), wie Analysepapier und Auftrag: kein Zuschlag in Kalenderjahren nach 2030. Das widerspricht den Grundlagen („Eine zeitliche Höchstdauer in Jahren gibt es nicht") und dem Rechenweg 05, dessen Beispielreihe bis 2037 zahlt.
   - Lesart (b), wie die Grundlagen: 2030 als Katalogdatum für das Ende der Frist zur Inbetriebnahme, statt der festen `KWKG_REALISIERUNG_JAHRE = 4`.
5. **Nr. 32, Schlüssel des BHKW.** Er ist wie bisher netto (Modulnachweis, nach Abzug des Hilfsstroms), der PV-Schlüssel ist brutto aus der Matrix. Das Mockup sagt „der Hilfsstrom berührt diese Menge nicht"; mit Brutto für beide wäre die Aufteilung ohne Speicher exakt. Umstellen? Dann passt auch `WIRT_ERL_B1_NAEHERUNG` („Netto-Stromanteil") nicht mehr. Ohne Hilfsstrom ist beides gleich; im Bestand gibt es keinen Rollentarif, also heute keine Wirkung.
6. **Nr. 32, doppelte PV-Zeile.** Die Zeile „PV: vermiedener Bezug" (Flat-Preis) steht im PV-Block neben dem neuen PV-Anteil. Das Mockup zeigt nur den Anteil. Soll die Zeile im Rollentarif entfallen?
7. **Nr. 29, Umrechnung.** Ich habe H_i/H_s des Trägers genommen, weil der Katalogfaktor `EBEV_UMRECHNUNG_HO` (3,2508) nur für Erdgas gilt. Ohne gepflegten Brennwert bleibt der Hi-Faktor. Bestätigen? `UMRECHNUNG_HO` hat damit weiter keinen Leser.

Nebenbei gesehen, nicht Teil von E7a: Der Strombedarf der Simulation enthält den Wärmepumpenstrom nicht (1039: Bedarf 60,0 MWh, Netzbezug 119,2 MWh). Im Rollentarif würde die vermiedene Menge dort negativ.

## Vorschläge für die Erledigt-Vermerke (§ 6.3)
- **Nr. 29:** erledigt mit E7/1 (Ho-Faktor, Umrechnung über H_i/H_s, Herleitung; Pinnung umgestellt 0,00 → 8.200,00 €/a).
- **Nr. 30:** teilweise, E7/2 und E7/3; offen sind Kern-Regel und Kohärenzzeile (Frage 2).
- **Nr. 32:** erledigt mit E7/4 (Anker 316.159,6 €/a über den Kernweg).
- **K‑1:** nicht gebaut, Messung nach A2 erfolgt (Frage 3).
- **A20:** nicht gebaut (Frage 4).

Papiere, die nachgezogen werden müssen: Rechenweg 05 (CO₂ 242,1 → 218,6 g/kWh, Ho-Faktor), Rechenweg 04 (R11), Rechenweg 07 und Konzept § 3.6 („ohne jede Eigenerzeugung"), Register R‑NR. Außerdem ist der Code-Kommentar an `SPALTE_EA_KWKG_ANLAGENART` („ohne Rechenwirkung") seit BK1 veraltet.

## Logbuchsätze
- „Die Stromsteuerbefreiung nach § 9 Abs. 1 Nr. 3 StromStG prüft den CO₂-Grenzwert von 270 g/kWh brennwertbezogen; die Herleitung nennt den Wert je Anlage."
- „Die vermiedenen Stromkosten im Rollentarif beziehen sich auf den Strombedarf ohne jede Eigenerzeugung; Blockheizkraftwerk und Photovoltaik erhalten je ihren Anteil."
- Der Dialogtext „(bitte wählen)" ist eine Kleinigkeit und bekommt keinen eigenen Satz.

## Phase 2 nach „Tests freigegeben"
Je nach Antwort auf Frage 1 die Testdatenbank nachziehen und committen, dann die gefilterten Tests, den vollen Lauf, den Referenzlauf über alle dreizehn Projekte gegen R11, Designer und SQL-Prüfer.

===== Bericht 2 (2026-09-23T02:55:56.212Z) =====

Phase 2 is done for everything that was built, except the test-database commit E7/6. The permission check refused the tool writing the repo file `Referenzlaeufe/Kenndaten_Test.sqlite` ("Modify Shared Resources"), so I did not retry it another way. As long as that commit is missing, one Kern test and twelve Auslieferungsvorlage tests stay red; everything else is green.

## Commits on `e7` (worktree `.claude/worktrees/e7`)

| Commit | Content |
|---|---|
| `57ba5097` E7/1 | Nr. 29: CO₂ limit checked on the Brennwert (gross calorific value) basis |
| `e4ce4f35` E7/2 | Nr. 30: schema step 101, Zielversion 101 |
| `d1cef396` E7/3 | Nr. 30: dialog shows „(bitte wählen)" |
| `212cb5b5` E7/4 | Nr. 32: avoided quantity without any own generation |
| `8c834746` E7/5 | Anchor tests: comment "old = new" with reason per anchor |
| `8fce3cb2` E7/7 | Answer 5: both keys gross from the Strommatrix; `WIRT_ERL_B1_NAEHERUNG` de „Näherung: verteilt nach dem Eigenverbrauch je Anlage", en „approximation: allocated by self-consumption per unit" |
| `61ebd027` E7/8 | Answer 6: in the Rollentarif the PV share replaces the line „PV: vermiedener Bezug"; in the Flat tariff it stays. The value is still computed and stored |

I kept the number E7/6 free for the test-database commit, as you named it.

## Test DB step: what is needed
- The migrated copy is ready at `scratchpad/e7a/tdbs101.sqlite` (SHA-256 `ec2820fc…`). The repo file is unchanged (`d2415fcd…`, matches the LFS pointer).
- A cell comparison of all 119 tables shows exactly seven rows in `Tab_Energieanlagen` plus the schema marker in `Tab_Applikation`. Integrity check is ok; size is unchanged.
- LFS is in place: `git check-attr` gives `filter: lfs`, and the filters are configured.
- If the user approves, in the worktree:
  1. `dotnet run --project Werkzeuge/Testdatenbankschema -c Release --no-build -- Referenzlaeufe/Kenndaten_Test.sqlite`
  2. `git add Referenzlaeufe/Kenndaten_Test.sqlite`
  3. Check `git show :Referenzlaeufe/Kenndaten_Test.sqlite | head -3` shows the LFS pointer.
  4. Commit „E7/6: Testdatenbank auf Schemastand 101 (Schritt 101)".
- The addendum for `Referenzlaeufe/LIESMICH.md` is prepared in `scratchpad/e7a/liesmich_nachtrag_101.md`. It goes after line 286 and becomes E7/9. I did not commit it yet, because without the database it would describe a state the repo doesn't have.

## Tests
- **Filtered, touched classes:** Kern 170 of 171 pass; the one failure is `TestdatenbankSchemastandWacheTests`. BHKW-dialog UI tests 75/75, Erlösrubrik and related 45/45.
- **Full run of the Kern filter:** 10,777 passed, 1 failed (the same Wache), 1 skipped (already skipped before, SpeicherPlanung).
- **Tool tests:** Formularkarte 124/124. Auslieferungsvorlage 7/19; all 12 failures come from the tool aborting on "Schemastand 100 (erwartet 101)". Run against the migrated copy, all its checks report "ok".

## Referenzlauf
13/13 PASS against R11, and 357/357 CSV byte-identical. I ran it both on the repo database (schema 100) and on the migrated copy (schema 101). The Referenzlauf export does not read `Tab_ErgebnisStromMatrix`, so the matrix change below never reaches the Referenzlauf. The basis stays R11.

## Designer and SQL checker
Designer unchanged. SQL checker: 0 findings (self-test 35 statements, 0 deviations).

## A/B for Nr. 32 after the gross switch
- **Economic results of all thirteen base projects:** unchanged from the start state. No project runs in the Rollentarif (`Tab_ProjektTarif` is empty), so the four stored avoided-cost columns stay 0 → 0. The anchors are unchanged too (1024 −2,896,359.13 €, 1030 −21,895,377.28 €).
- **Strommatrix, only projects with PV:**

| Project | Demand without plant [MWh] | PV self-use [MWh] | Demand peak [kW] |
|---|---|---|---|
| 1007, 1046 | 19.10 → 24.00 | 4.90 | 6.60 → 6.62 |
| 1040 | 5.40 → 8.00 | 2.60 | 3.72 (unchanged) |
| 1045 | 5.94 → 8.00 | 2.07 | 3.7235 → 3.7215 |

  In 1045 the peak drops slightly because the inverter's night consumption (9.3 kWh/a) no longer counts as demand.
- **Mockup example through the Kern path** (with 10 €/kW·month to show the capacity share):

| Quantity | Before | After |
|---|---|---|
| Avoided quantity | 1,094.2 MWh | 1,179.7 MWh |
| Energy share | 315,129.6 € | 339,753.6 € |
| § 9b correction | 21,884.0 € | 23,594.0 € |
| Capacity share | 14,989.0 € | 16,160.3 € |
| Split | BHKW 293,245.6 € only | BHKW 293,245.6 + PV 22,914.0 = 316,159.6 € |

  The KWK self-consumption stays 1,094.2 MWh.
- **Gross vs. net key, with auxiliary power (unit test):** before, net 1,000.0 : 85.5 gave BHKW 1,086.8 / PV 92.9 MWh; now gross 1,094.2 : 85.5 gives exactly 1,094.2 / 85.5 MWh.

## Other points
- **Answer 7:** implemented as confirmed — conversion over H_i/H_s of the carrier; without a maintained Brennwert the Hi factor stays and the reason text says so. `EF_BILANZ_EBEV_UMRECHNUNG_HO` still has no reader.
- **Still deferred:** K‑1, A20 and the Kern rule plus coherence line for Nr. 30, as you instructed. Nothing of those was built.
- **Suggested "erledigt" notes (§ 6.3):**
  - Nr. 29: done with E7/1.
  - Nr. 32: done with E7/4, E7/7, E7/8.
  - Nr. 30: partly done (E7/2, E7/3; E7/6 open); the Kern rule stays with question 2.
  - K‑1 and A20: not built (questions 3 and 4).
- **Log sentences:**
  - „Die Stromsteuerbefreiung nach § 9 Abs. 1 Nr. 3 StromStG prüft den CO₂-Grenzwert von 270 g/kWh brennwertbezogen; die Herleitung nennt den Wert je Anlage."
  - „Die vermiedenen Stromkosten im Rollentarif beziehen sich auf den Strombedarf ohne jede Eigenerzeugung; Blockheizkraftwerk und Photovoltaik erhalten je ihren Anteil nach dem Eigenverbrauch."

The measurement data are in `scratchpad/e7a` (`vorher.txt`, `nach29.txt`, `nach30.txt`, `nach32.txt`, `final.txt`, `probe_*.txt`, `probe32_*.txt`).

===== Bericht 3 (2026-09-23T03:05:13.166Z) =====

Die Punkte (1) bis (3) sind erledigt. E7/9 ist committet, und nach dem Commit der Testdatenbank läuft alles ohne Fehler.

**Stand:** Branch `e7` im Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e7` auf Basis b4d468a4. Der Arbeitsbaum ist sauber. Ich habe nichts gepusht, keinen CI-Lauf gestartet, nichts gemergt und keinen Stash benutzt.

**Commits E7/1 bis E7/9**

| Nr. | Commit | Betreff |
|---|---|---|
| E7/1 | 57ba5097 | CO₂-Grenzwert des § 9 Nr. 3 brennwertbezogen (Nr. 29) |
| E7/2 | e4ce4f35 | Schemaschritt 101 – leere KWKG_Anlagenart wird NULL (Nr. 30) |
| E7/3 | d1cef396 | BHKW-Dialog zeigt „(bitte wählen)" ohne Anlagenart (Nr. 30) |
| E7/4 | 212cb5b5 | Vermiedene Bezugsmenge ohne jede Eigenerzeugung (Nr. 32) |
| E7/5 | 8c834746 | Ankertests – E7a gemessen, kein Anker bewegt sich |
| E7/6 | 1d98c8b1 | Testdatenbank auf Schemastand 101 (dein Commit, zeitlich nach E7/8) |
| E7/7 | 8fce3cb2 | Verteilschlüssel der vermiedenen Kosten brutto aus der Matrix |
| E7/8 | 61ebd027 | Rubrik – PV-Anteil ersetzt „vermiedener Bezug" im Rollentarif |
| E7/9 | ff8a17f9 | LIESMICH – Nachtrag Schemastand 101 (Schritt 101) |

Insgesamt sind es 24 Dateien mit +1494 und −120 Zeilen.

**E7/9:** Der Nachtrag steht in `Referenzlaeufe/LIESMICH.md` nach Zeile 286, als Leerzeile plus zwölf Zeilen, unverändert aus dem vorbereiteten Text. Die Datei hat keinen BOM, CR = LF = 606, also durchgehend CRLF.

**Tests nach dem Commit der Testdatenbank** (nacheinander; nebenher liefen nur ruhende MSBuild-Knoten, kein anderer Testprozess):
- `TestdatenbankSchemastandWacheTests`: 1/1 grün, vorher 1 rot.
- `Auslieferungsvorlage.Tests`: 19/19 grün, vorher 7/19. Die zwölf roten kamen alle aus „Schemastand 100 (erwartet 101)".
- Voller Lauf `WP-Plan.Kern.slnf`, wiederholt: 0 Fehler, 10.778 bestanden, 1 übersprungen, zusammen 10.779.

  | Testprojekt | Ergebnis |
  |---|---|
  | EPOS.Kern | 4.611/4.611 |
  | EPOS.UI | 5.230/5.230 |
  | KiKern | 524/524 |
  | SpeicherEngine | 386/386 |
  | SpeicherPlanung | 27 bestanden, 1 übersprungen (der Überspringer bestand schon vorher) |

  Die gefiltert gelaufenen Klassen stecken alle in diesem Lauf. Die Formularkarte-Tests (124/124) stammen aus Phase 2 und sind seitdem nicht berührt.

**Referenzlauf, Designer und SQL-Prüfer gelten unverändert.** Seit ihren Läufen kamen nur E7/6 (die Datenbank, byte-gleich zu meiner geprüften Kopie) und E7/9 (die LIESMICH) dazu, kein Code und keine Ressource.
- **Referenzlauf:** 13/13 PASS gegen R11. Er lief um 04:52 und 04:53 auf dem Endstand nach E7/8, einmal mit Schemastand 100 und einmal mit der migrierten Kopie. Diese Kopie hat dieselbe SHA-256 ec2820fc… wie die committete Datenbank. Eben habe ich beide Läufe noch einmal mit `cmp` geprüft: je 357/357 CSV byte-gleich zur Basis. Ich habe nichts neu eingefroren.
- **Designer:** unverändert, 7.599 Einträge.
- **SQL-Prüfer:** 1.567 Texte, 0 Fundstellen; Selbsttest 35 bestanden, 0 Fehler.

**Die wichtigsten Zahlen:**
- **Nr. 29:** Der Grenzfall-Test aus R11 bekommt 8.200,00 statt 0,00 €/a. Die Basisprojekte ändern sich nicht. `EBEV_UMRECHNUNG_HO` bleibt wie beschlossen ohne Leser.
- **Nr. 30:** Schritt 101 setzt genau sieben Zellen auf NULL: Anlage 12310 (Projekt 1032) sowie 14819, 14842, 14843, 14844, 14851 und 14852 (Projekt 1043). Darunter ist kein BHKW und kein Referenzprojekt.
- **Nr. 32:** Der Anker geht von 293.245,6 auf 293.245,6 + 22.914,0 = 316.159,6 €/a. Mit dem Brutto-Schlüssel verteilt sich die vermiedene Menge neu:
  - vorher BHKW 1.086,8 MWh und PV 92,9 MWh,
  - jetzt BHKW 1.094,2 MWh und PV 85,5 MWh (Summe gleich).

  Den Matrix-Bedarf exportiert der Referenzlauf nicht: 1007 und 1046 steigen von 19,10 auf 24,00 MWh, 1040 von 5,40 auf 8,00, 1045 von 5,94 auf 8,00.

**Weiter zurückgestellt und nicht gebaut** (Fragen 2, 3 und 4 beim Anwender): K‑1 mit Schritt 102, A20 (Förderende) und die Kern-Regel zu Nr. 30 samt Kohärenzzeile „Anlagenart fehlt".

**Für den Merge:** Ich habe `git fetch` gemacht. `origin/ios_migration_september` hat seit b4d468a4 nur drei Papier-Commits (4929d432, 90225f59, c4a6ca0a) und steht weiter auf Zielversion 100. Es gibt also keinen konkurrierenden Schritt 101. Die geänderten Dateien überschneiden sich nicht mit E7, und eine Probe mit `git merge-tree` ist konfliktfrei.

Die Protokolle liegen in `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e7a\`: `wache_nach_db.log`, `auslieferung_nach_db.log` und `voller_lauf_nach_db.log`.