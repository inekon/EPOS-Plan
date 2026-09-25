# E19 — Restpunkte der Wirtschaftlichkeit: Nr. 15 als überholt geschlossen, Unternehmensart ohne BHKW im Parameterdialog (Protokoll, 25.09.2026)

Statuszeile #498 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Anwender am
25.09.2026, „fahre fort“ nach dem Statusbericht, auf den Vorschlag des Orchestrators als nächste kleine Welle. Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.2 (Gruppe 4 — Stromsteuer), § 2.4 (Parameterdialog, Gruppe Strom), § 3.8 (§ 9b), § 6.3 Nr. 15 und 33, § 6.5;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E19 (neu) und R‑Rest (neu, die Anwenderentscheide vom 25.09.2026 zu § 6.3 Nr. 10, 11, 13, 18, 19); Herkunft der
Punkte: [`B4_Energieintensitaet_Protokoll.md`](B4_Energieintensitaet_Protokoll.md) § 4 Grenze 2 (Nr. 15) und
[`E18_Restpunkte_Stromsteuer_Protokoll.md`](E18_Restpunkte_Stromsteuer_Protokoll.md), Frage E18‑Q7 (Nr. 33);
Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 8 (Zone „Dialog — Parameter“, neue
Gruppe Strom; Ressourcentafel) und Stand-Absatz des Anhangs. Vorgänger:
[`E18_Restpunkte_Stromsteuer_Protokoll.md`](E18_Restpunkte_Stromsteuer_Protokoll.md). Zweig `e19` von `b5a2e389`
(`origin`, Zielversion 139), vor dem Bau vorgespult auf `a0bbc633` (Z5 #495 mit Schemaschritt 140); Opus 5.5 im
Worktree `.claude/worktrees/e19`, zwei Phasen: Phase 0 nur gelesen und gemessen, Phase 1 nach der Freigabe
(25.09.2026, 08:20) `a8832acd` (E19/1), `c87354a0` (E19/2), `14d55922` (E19/3), `bfa2c9e5` (E19/4). Merge `31a0b085`
(„Merge e19: Unternehmensart ohne BHKW im Parameterdialog (Nr. 33), Nr. 15 als ueberholt mit Wache (#498)“) auf `pm20`
über `origin` = `f55a4cd5` (#496 mit Schemaschritt 141, der Folgeberichtigung der Anschlusslängen im Gebäudekatalog;
Testdatenbank `a427aa72`), ohne Konflikt. Basis `2026-09-24_R14_Kaelteerzeuger`. **Kein Schemaschritt, keine
Rechenwirkung** — Anker und Referenzlauf bitgleich; der Schemastand bleibt 141 (#496).

## Befund vor der Welle (Phase 0)

1. **Nr. 15 — Bilanzjahr und Unternehmensart wirken erst beim nächsten Öffnen: durch die Schalentrennung überholt.**
   Herkunft ist die Grenze 2 des B4-Protokolls: Der WinForms-Trägerdialog las beide Werte beim Blockaufbau; der
   Konzeptpunkt trug nur den Titel. Heute liest die Hülle der Energieträgerverwaltung beide Werte bei jeder Öffnung
   (`EnergietraegerHuelle.Gaben` über `KatalogjahrErmitteln`; sie speisen Katalogsatz, Schnellwahl und Projekt-CO₂-Preis),
   und die Hülle entsteht bei jedem Öffnen neu (Windows: modales `EnergietraegerFenster`; Kostenseite:
   `KostenSeiteGaben.TraegerGaben`). Aus dem Trägerdialog führt kein Weg in den Parameter- oder den BHKW-Dialog — seine
   Unterdialoge sind Kostenprofil, Spotpreis, Saisonreihe und Emissionskatalog; `UnterdialogGeschlossen` liest den
   Träger neu, nicht das Jahr, folgenlos. Die Wirtschaftlichkeitsseite baut den Satz jedes Unterdialogs beim Öffnen neu
   und lädt nach dem Schließen (`Laden`: Emissionsbilanz, Parameterzeile); die Überlagerungen schließen sich aus;
   Parameter- und BHKW-Dialog schreiben den ganzen Satz. Die Anzeige des Stromsteueranteils aus E18 folgt im
   BHKW-Dialog live. **Bewusst erst nach einem Rechenlauf** gelten Kohärenzzeilen, Vorschau, Ergebnisansicht,
   § 9b-Betrag und Kohärenzfall 3; die Seite meldet dann „gespeichert — bitte neu berechnen“.
2. **Nr. 33 — Unternehmensart ohne BHKW nicht pflegbar.** Die Sperre ist `_stand.MitBhkw` in
   `WirtschaftlichkeitSeite.razor` (aus `WirtschaftlichkeitCtrl.ErzeugerDerGruppe`, zählt `Tab_BHKW` über Stamm und
   Varianten); sie verbirgt den ganzen Dialog „BHKW-Wirtschaftlichkeit“ — Gruppe 4 mit räumlichem Zusammenhang,
   Hocheffizienz, Modus § 9 Abs. 1 Nr. 3 und der E18-Anzeige, die Überlagerung „Sätze und Herkunft“ und den Sprung
   „BHKW-Tarif…“. Ohne BHKW gehören fachlich nur die Unternehmensart und die E18-Anzeige dazu. Der Parameterdialog
   führt in der Gruppe BHKW nur einen Verweis und in der Gruppe Strom keine Unternehmensart. **Der Rechenweg besteht:**
   Seit E5 rechnet der Kern § 9b auch ohne BHKW bei produzierendem Gewerbe oder Land- und Forstwirtschaft (sonst null)
   — über die Oberfläche war er unerreichbar. iOS hat dieselbe Lücke (`IosProjektQuelle.BhkwDaten` ist ohne Anlagen
   `null`). **Testdatenbank:** 15 von 19 Stämmen führen kein BHKW; Referenzprojekte mit BHKW sind 1017, 1018, 1024 und
   1030; `Tab_ProjektWirtschaftlichkeit` hat 5 Zeilen (1019 und 1030 `KEIN_PROD_GEWERBE`, 1017, 1023 und 1024 NULL;
   `Bilanz_Jahr` überall NULL); ohne Brennstofferzeuger sind die Stämme 19, 1006 und 1032 (kein Referenzprojekt).
3. **Nebenbefunde.** Das Bilanzjahr steht nur in der Brennstoffgruppe des Parameterdialogs (`HatBrennstoff`).
   **Projekt 1017 führt ein BHKW** — der Vorbehalt in A‑E18‑1 ist gegenstandslos. Der Kommentar „Zwoelf Felder
   gehoeren dem Parametersatz“ in `KiDialoge.cs` ist veraltet (17 Felder des Parametersatzes, 18 des Szenariosatzes).

## Gebaut

- **E19/1 — Wache Nr. 15** (`a8832acd`): neu `EPOS.Kern.Tests/KatalogjahrJeOeffnungTests.cs` — Projekt 1030,
  Stromträger 60: Die erste Öffnung empfiehlt den Regelsatz „(ab 2026“; nach dem Speichern von produzierendem Gewerbe
  und Bilanzjahr 2025 empfiehlt die nächste Öffnung (`EnergietraegerHuelle.Gaben` zweimal auf einer Kopie) den
  reduzierten Satz „… im Jahr 2025“. Kein Code am Trägerdialog (E19‑Q1 a).
- **E19/2 — Hülle, Dialog, Texte** (`c87354a0`): `WirtschaftlichkeitParameterHuelle` liefert zusätzlich
  `Stromsteueranteil` (`StrompreisZerlegungCtrl.StromsteuerErfasst`) und `Katalog` (`GesetzKatalog.WertMitHerkunft`).
  `WirtschaftlichkeitParameterDialog.razor`, Gruppe Strom, **nur ohne BHKW** (`!HatBhkw`): Auswahlfeld Unternehmensart
  (`BhkwWahlen.Unternehmensart()`, Beschriftung `BHW_S_UNTERNEHMENSART`) auf `Parameter.Unternehmensart`, darunter die
  Zeilen des erfassten Stromsteueranteils (`StromsteueranteilAnzeige.Zeilen`, derselbe Baustein wie in Gruppe 4 des
  BHKW-Dialogs) live mit der gewählten Unternehmensart und `Parameter.BilanzJahr` (ohne Bilanzjahr die
  Bilanzkonvention), dazu die Erklärzeile zu § 9b. Mit BHKW steht in der Gruppe Strom nichts davon; die Gruppe BHKW
  verweist mit `BHW_PARAM_VERWEIS` auf den Dialog „BHKW-Wirtschaftlichkeit“ — je Projektlage genau eine Pflegestelle
  derselben Spalte (E19‑Q2 a, Q3 a, Q6 a). Geschrieben wird über den vorhandenen Weg des Parametersatzes; Menüs und
  Hilfeanker (`Wirtschaftlichkeit#parameter`) unverändert.
- **E19/3 — KI** (`14d55922`): `KiDialoge.cs` Feld `unternehmensart` (Typ Wahl, Name und Erklärung aus den
  BHW-Texten), Maske `Form_WirtschaftlichkeitParameter` mit 35 Feldern; `WirtschaftlichkeitParameterKiSicht` mit
  `Unternehmensart`, `UnternehmensartWahl` und `UnternehmensartSetzen` — mit BHKW lehnt das Setzen benannt ab
  (`InvalidOperationException` mit dem Text von `BHW_PARAM_VERWEIS`); `KiMaskenabdeckungWacheTests` 34 → 35 (E19‑Q5 a).
- **E19/4 — Tests** (`bfa2c9e5`): `WirtschaftlichkeitParameterDialogTests` sechs bUnit-Fälle mehr (sichtbar ohne BHKW;
  mit BHKW nur der Verweis; Wahl und Bilanzjahr drehen die Anzeige live ohne Speichern; „Abbrechen“ schreibt nichts;
  „OK“ schreibt mit dem Parametersatz; die KI setzt nur ohne BHKW; Klapplisten 1 → 2 bzw. 4 → 5); neu
  `EPOS.Kern.Tests/UnternehmensartOhneBhkwTests.cs` (die Hülle liefert Anteil und Katalog, jeder Schlüssel trifft einen
  `[Parameter]`; § 9b ohne BHKW an Projekt 1041 — statt 1007, das kein gespeichertes Ergebnis führt —: mit
  produzierendem Gewerbe eine Entlastung > 0, mit `KEIN_PROD_GEWERBE` 0). iOS unberührt.

## Schlüssel

**1 neu** (de/en): `WPAR_UA_9B_HINWEIS` (nach `WPAR_TITEL`; Text `WirtschaftlichkeitParameterDaten.Unternehmensart9b`:
„Für ein Unternehmen des produzierenden Gewerbes oder der Land- und Forstwirtschaft rechnet der nächste Lauf die
Entlastung nach § 9b StromStG auf den Netzbezug. Führt das Projekt ein BHKW, wird die Unternehmensart im Dialog
‚BHKW-Wirtschaftlichkeit' gepflegt.“). Wiederverwendet: `BHW_S_UNTERNEHMENSART`, die 14 Schlüssel
`BHW_S_STANTEIL_*` aus E18 und `BHW_PARAM_VERWEIS`. Je Sprache auf `e19` und auf dem Merge 11.010 → 11.011 Einträge,
der Designer 11.007 → 11.008 Eigenschaften, wiederholbar (+0 im Prüflauf auf dem Merge).

## Fragen aus der Welle

Entschieden hat der Orchestrator am 25.09.2026 (08:20) mit der Baufreigabe, nach Empfehlung — E19‑Q4 = b, die übrigen
a; gebaut ist jeweils der Entscheid (→ Register R‑E19).

| Frage | Lesarten | Entscheid |
|---|---|---|
| **E19‑Q1** Nr. 15 | (a) als überholt schließen mit Befund und Wache-Test; (b) defensiv `KatalogjahrErmitteln` in `UnterdialogGeschlossen` | a |
| **E19‑Q2** Ort der Pflege ohne BHKW | (a) Parameterdialog, Gruppe Strom, nur ohne BHKW; (b) der Knopf „BHKW-Wirtschaftlichkeit…“ immer, der Dialog ohne BHKW reduziert; (c) immer im Parameterdialog, Gruppe 4 des BHKW-Dialogs nur Anzeige | a |
| **E19‑Q3** Anzeige des erfassten Stromsteueranteils im Parameterdialog | (a) ja, live; (b) nein | a |
| **E19‑Q4** Bilanzjahr ohne Brennstofferzeuger | (a) immer zeigen; (b) so lassen, der Rückfall 2026 als benannte Grenze | **b** |
| **E19‑Q5** KI-Feld `unternehmensart` | (a) anmelden, mit BHKW gesperrt samt Hinweis; (b) ohne Sperre; (c) kein Feld | a |
| **E19‑Q6** Texte | (a) die vorhandenen BHW-Schlüssel und ein neuer Schlüssel für die § 9b-Erklärzeile; (b) eigene WPAR-Schlüssel | a |

## Anwenderentscheide vom 25.09.2026 zu Konzept § 6.3

Mit dieser Welle nachgetragen (→ Register R‑Rest; der Weg im Protokoll der Entscheidwege § 8.39):

| Nr. | Entscheid | Stand |
|---|---|---|
| **10** Bezugsgrößen der übrigen KD1-Bemessungsarten | „Wärmepumpe beides“ nur bei den Investitionskosten nach kW elektrisch und kW thermisch — die kWh-Bemessung der Wärmepumpe bleibt thermisch, Strom-kWh sind Energiekosten | offen mit dieser Präzisierung; kleine Bauwelle folgt |
| **11** Nachzieh-Migration für Bestandsprojekte | nicht nachziehen | geschlossen |
| **13** Pufferkapazität bleibt null | nur Volumen — die Grenze ist bestätigt | geschlossen |
| **18** Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1) | nach der Empfehlung: zuerst eine Messwelle, danach der Entscheid | offen — gemessen 25.09.2026 (Probeumbau der fünf Rechenweg-Sortierungen auf die 99er-Regel, Worktree `mess18`, nicht gemergt): ohne Rechenwirkung — 12 von 13 Referenzprojekten byte-gleich, nur 1042 tauscht in `aggregate.csv` die Modulreihenfolge der beiden Wärmepumpen (10 Werte, Werte gleich, Index anders); Deckung, Endenergie, CO₂, Kapitalwert unverändert; kein Test rot. Nebenbefund: Die Modul-Lader für Kessel, Solarthermie und BHKW sortieren gar nicht. Empfehlung: den Umbau mit der nächsten ohnehin fälligen Neueinfrierung der Referenzbasis bündeln und dann über die drei unsortierten Lader mitentscheiden; der Anwenderentscheid steht aus |
| **19** Asymmetrie „Wartung BHKW“ gegen „Vollwartung / Wartung Kessel“ | belassen — E10‑Q6 a bestätigt; €/kWh el. ist beim BHKW die übliche Vertragsform | dokumentiert, bestätigt |

## Abweichungen und Befunde

1. **Nr. 15 ist überholt**, nicht gebaut: Seit der Schalentrennung liest jede Öffnung des Trägerdialogs Bilanzjahr und
   Unternehmensart neu; die Wache hält das fest.
2. **Projekt 1017 führt ein BHKW** — der Vorbehalt in A‑E18‑1 („1017 führt womöglich kein BHKW“) ist gegenstandslos.
3. **Bilanzjahr nur mit Brennstofferzeuger** (E19‑Q4 b, benannte Grenze): Ohne Kessel und BHKW zeigt der
   Parameterdialog kein Bilanzjahr; Katalogjahr und Anzeige nehmen dann den Rückfall 2026 der Bilanzkonvention, die
   Rechnung ist davon nicht berührt.
4. **Nachweis an 1041 statt 1007:** 1007 führt kein gespeichertes Ergebnis; der § 9b-Test rechnet deshalb an 1041.
   Für die Abnahme taugen beide.
5. **iOS unberührt** — die iOS-Schale hat dieselbe Lücke; nicht beauftragt.
6. **Veralteter Kommentar** in `KiDialoge.cs` („Zwoelf Felder …“) — offen, nicht beauftragt.
7. **Messwelle zu Nr. 18** (Worktree `mess18`, Commit `6de80d66` „nicht mergen“): Der Probeumbau der fünf
   Rechenweg-Sortierungen auf die 99er-Regel ist ohne Rechenwirkung — 12 von 13 Referenzprojekten byte-gleich, in 1042
   tauschen in `aggregate.csv` nur die zwei Wärmepumpenmodule ihre Plätze (10 Werte, Werte gleich), alle Zeitreihen
   byte-gleich; Deckung, Endenergie, CO₂ und Kapitalwert unverändert; kein Test rot. Nebenbefund: Die Modul-Lader für
   Kessel, Solarthermie und BHKW sortieren gar nicht. Empfehlung: den Umbau mit der nächsten ohnehin fälligen
   Neueinfrierung der Referenzbasis bündeln und dann über die drei unsortierten Lader mitentscheiden; der
   Anwenderentscheid steht aus.

## Nachweis

- **Ohne Rechenwirkung:** Anker unberührt; Referenzlauf 13/13 gegen `2026-09-24_R14_Kaelteerzeuger` PASS, 4.207.049
  Werte, 394/394 CSV byte-gleich. Die neue Pflegestelle schreibt dieselbe Spalte wie der BHKW-Dialog; der Kapitalwert
  ändert sich nur, wenn eine Unternehmensart gewählt wird, und dann gewollt.
- **§ 9b ohne BHKW** (`UnternehmensartOhneBhkwTests`, Kopie der Testdatenbank): Projekt 1041 mit produzierendem Gewerbe
  — Entlastung > 0; mit `KEIN_PROD_GEWERBE` — 0.
- **Phase 1** (Worktree `e19`): Kern-Filter Release 0 Fehler, Windows-Schale x64 0 Fehler; kein SQL berührt, keine
  Tabellenänderung; gefiltert Kern 155/155 (`UnternehmensartOhneBhkwTests` 2/2), UI 425/425 (sechs E19-Fälle); voller
  Lauf EPOS.Kern.Tests 6.818 und 1 übersprungen, EPOS.UI.Tests 6.243, KiKern.Tests 549, SpeicherEngine.Tests 386,
  SpeicherPlanung.Tests 27 und 1 übersprungen — 14.023 bestanden / 0 Fehler / 2 übersprungen; Testhost-Regel
  eingehalten (zweimal gewartet); im Worktree die Dateien `-shm`/`-wal` gelöscht.
- **Merge** `31a0b085` auf `pm20` über `f55a4cd5` ohne Konflikt; Designer 11.008, wiederholbar (+0).
- **Gate:** Kern-Filter 0 Fehler, ChartProben 161/161 gleich der Windows-Messlatte (die Messlatte am 25.09.2026 um die zehn Bilder der AK1-Wellen W3/W4-5 erweitert, alle 151 bisherigen Hashes unverändert), voller Lauf 14.027 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern 6.822 und 1 übersprungen, EPOS.UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen), Dokumentationswachen 29/29 (`GATE498.log`, 25.09.2026 08:10–08:17 Uhr, auf `31a0b085`)
- **CI:** steht aus (Beobachtung nach dem Push)

## Abnahme am Gerät (A‑E19‑1, Windows)

1. **Ohne BHKW:** Projekt 1041 oder 1007 → „Parameter…“: Die Gruppe Strom zeigt die Unternehmensart, die Zeilen des
   erfassten Stromsteueranteils und die § 9b-Erklärzeile.
2. **Wechsel ohne Speichern:** „produzierendes Gewerbe“ wählen — die Kohärenzzeile wechselt sofort; „Abbrechen“
   schreibt nichts.
3. **Wirkung:** speichern, dann „Berechnen“ — die § 9b-Entlastung steht im Ergebnis.
4. **Mit BHKW:** Projekt 1030 — keine Unternehmensart in der Gruppe Strom; die Gruppe BHKW verweist auf den Dialog
   „BHKW-Wirtschaftlichkeit“.
5. **Englische Oberfläche:** dieselben Zeilen auf Englisch.

## Logbuch

Im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026), Stichwort `wirtschaftlichkeit`: „Ohne BHKW wird die
Unternehmensart nach Stromsteuergesetz in den Wirtschaftlichkeits-Parametern (Gruppe Strom) gepflegt; darunter steht
der im Strompreis erfasste Stromsteueranteil.“ Nr. 15 bekommt keinen Satz (keine sichtbare Änderung).

## Papiere mit der Statuszeile

Konzept (Kopf mit Codestand `31a0b085` und Zielversion 141 — Schritt 141 ist #496, 140 Z5 (#495), nicht E19 —,
§ 2.2 Gruppe 4, § 2.4 Gruppe Strom, § 3.8 § 9b ohne BHKW, § 6.1 Zeile E19, § 6.3 Nr. 10, 11, 13, 15, 18, 19 und 33,
§ 6.5 Zeile „Energieintensiv“, § 7, Anhang), Register (Kopf, Familientafel, R‑E18 mit E18‑Q7, R‑E10 mit E10‑Q6, neue
Familien R‑E19 und R‑Rest, EZ‑9, EZ‑10), Analysepapier (Kopf, Nachtrag #498, § 5 Zeile E19 ohne Schemaschritt und
Stand der Etappen, § 6 Nachtrag zur Vergabe von 131–141), Protokoll der Entscheidwege (Kopf, § 0.5, Kopf von § 8,
§ 8.37, § 8.38, § 8.39), Mockup (Kategorie 8: Dialog „Parameter“, neue Gruppe Strom mit Unternehmensart,
Anzeigezeilen, § 9b-Erklärzeile und Sichtbarkeitsregel; Ressourcentafel; Stand-Absatz), Update-Papier und die
Wiki-Quelle Wirtschaftlichkeit (Anker `parameter`, Abschnitt „Strom“, Punkt „Unternehmensart“; Halbsatz unter
`bhkw-wirtschaftlichkeit`), Index (Reporting 133 → 134). `Referenzlaeufe/LIESMICH.md` bleibt ohne Nachtrag (kein
Schemaschritt); „heute Schemastand 141“ von #496 stimmt.

## Offen

- **Nr. 10** („Wärmepumpe beides“ bei den Investitionskosten) — kleine Bauwelle folgt; **Nr. 18** — gemessen ohne
  Rechenwirkung; Entscheid des Anwenders, ob der Umbau mit der nächsten Neueinfrierung gebündelt wird oder eine
  eigene Basis R15 bekommt, dabei über die drei unsortierten Modul-Lader.
- **Abnahme am Gerät** A‑E19‑1 (fünf Schritte oben); A‑E18‑1 geht jetzt auch an 1017.
- **Gate** und **CI** (Nachweis oben).
- Der **Wiki-Sammel-Upload** am 26.09.2026 (Version 1.2.0.4, freigegeben).
- Nicht beauftragt: die iOS-Lücke, der veraltete Kommentar in `KiDialoge.cs`.
