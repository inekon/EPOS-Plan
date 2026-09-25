# E21 — Pflegewelle: resx-Sammelnachtrag erledigt, Datenlücken der Testdatenbank gemessen und benannt (Protokoll, 25.09.2026)

Statuszeile #506 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Anwender am
25.09.2026, „sonst nach Empfehlung“. Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 6.3 Nr. 23, 24;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E21 (neu). Herkunft der Punkte: Nr. 23 aus dem resx-Sammelnachtrag B3a, B3b, B4 und der F-Serie; Nr. 24 und die
Datenlücken 1018/1024 aus E10 (`e10_berichte.md`), 1023 aus E14/E15, 1030/1026 aus E9 (`e9_fakten.md`), die
PV-Preislücke aus E9a (`e9a_berichte.md`); der veraltete Kommentar `KiDialoge.cs` ~1777 ist ein Nebenbefund aus E19.
Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5. Vorgänger: [`E19_Unternehmensart_ohne_BHKW_Protokoll.md`](E19_Unternehmensart_ohne_BHKW_Protokoll.md). Zweig `e21`
von `cbed6dba` (`origin`, nach #498 und #499); Opus 5.5 im Worktree `.claude/worktrees/e21`, zwei Phasen: Phase 0 nur
gelesen und gemessen, Phase 1 nach der Freigabe (25.09.2026, 09:45) `25984cbd` (E21/1), `02ea7650` (E21/2). Merge
`e7c2f8f7` („Merge e21: Pflegewelle Ressourcen (Nr. 23 erledigt) und Datenluecken benannt (Nr. 24) (#506)“) auf `pm21`
über `origin` = `b5cc1a61` (Cloud-Sitzung „claude/exciting-jennings“ nach #497), ohne Konflikt. Basis
`2026-09-24_R14_Kaelteerzeuger`. **Kein Schemaschritt, keine Rechenwirkung** — Anker und Referenzlauf bitgleich; der
Schemastand bleibt 141.

## Befund vor der Welle (Phase 0)

1. **Nr. 23 — resx-Sammelnachtrag: längst gelaufen, der Konzeptpunkt war veraltet.** Der Sammelnachtrag selbst ist mit
   den Commits `aeece2feb` (sechs Schlüssel B3a/B3b), `2cfb871dc` (zehn `PREIS_ST_*` B4, zwei `SIM_KARTE_*` F2),
   `2a8b95335` (sechs `KOH_*` FX5) und `8f07cce93` (Designer) schon erledigt. Messung vor der Welle: de 11.011,
   en 11.011, Designer 11.008 (die Differenz zu de/en sind Bitmap1/Color1/Icon1); 0 Dubletten, de/en deckungsgleich,
   Designer wiederholbar. Von 26 geprüften Schlüsseln aus B3a/B3b/B4/F2/FX1–FX5 tragen 24 einen Leser; zwei
   Ausnahmen: `PREIS_ST_GRUND_EINHEIT` (de `:10825`, en `:10818`, Designer `:56644`) ohne Leser seit iU9-W4.4
   (`02efc0336`) → streichen; `STEUER_ENERGIEST_54_BEMESSUNG` (de `:8289`, en `:8282`, Designer `:73522`) seit B3a
   nicht mehr erzeugt, Kommentar `SteuerGutschriftRechner.cs:691-695` → streichen samt Kommentar.
   `WIRT_CO2_STROMMIX_RUECKFALL` und `KOH_FALL5_MISCHLAGE` sind sauber gestrichen (Nachfolger vorhanden). 45
   „fehlende“ Code-Schlüssel sind Präfixe, Prüfmuster oder `WIRT_SZEN_HINWEIS` (der Test prüft gerade das Fehlen) —
   keiner davon ist echt fehlend. Rückfalltexte: 21 von 24 zeichengenau gegen die de-resx; drei Literale schließen mit
   einem geraden `"` statt `“`: `EnergietraegerHuelle.cs:2531` (`PREIS_ST_QUELLE_RUECKFALL`), `:2534`
   (`PREIS_ST_GRUND_KEIN_JAHR`), `:2542` (`PREIS_ST_EMPFOHLEN`).
2. **Nr. 24 und die Datenlücken — an keinem Referenzprojekt byte-gleich haltbar, oder gewollte Prüffälle.** Fünf
   Projekte geprüft:

   | Projekt | Lücke | Wert/Quelle | Einfrierregel | Wirkung |
   |---|---|---|---|---|
   | 1018 (Ref.) | `Tab_Energieanlagen.ID_Carrier` NULL, Anlage 10369 (Kessel 1018251, Brennstoff 3) | 63 Erdgas E (wie BHKW 11327, Kessel in 1026/1030) | nicht in der Liste; Emissionsfaktor des Kessels gleich (eps 10063, CO₂ = 240) | gemessen FAIL: 1 Abweichung `HeizkesselModul[0].carrier_id` leer → 63 |
   | 1018 (Ref.) | `Tab_Pufferspeicher.Vorlauf`/`Ruecklauf` NULL, 1054173/1054175 (+ Verwendung NULL) | keine Quelle; Probe 70/50 | keine Regel, aber Simulation | 12 Dateien anders, Puffer Q_max 11,19 → 22,39 kWh, Ladung 36,7 → 64,5 MWh, Emissionsfaktor BHKW CO₂ 22,339 → 22,344 |
   | 1024 (Ref.) | `Tab_Kenndaten` ID_WP 1034317, Quelltemperaturen nur bis 20 °C | Herstellerkatalog (Stamm 59) endet bei 20 °C, keine Quelle; der Kern kappt, extrapoliert nicht (`SimulationWaermepumpe.cs:437`), die Warnung ist richtig | WP-Kennlinie nicht in der Liste | nicht gemessen (die Probe hat der Klassifizierer abgelehnt); erfundene Daten sind nicht empfohlen |
   | 1023 (Ref.) | `ID_Carrier` NULL, Kessel 11205 (1018254, Brennstoff 3); keine eps-Zeile Erdgas → kein Kapitalwert | 63 + eps-Zeile (Preis ~0,84 €/Nm³ aus 1030) | die eps-Zeile mit CO₂ fällt unter die Regel; CO₂ 240 → 201 g/kWh | gemessen FAIL: Emissionsfaktor Kessel CO₂ 22,44 → 18,79 t/a + `carrier_id`; Tests nutzen 1023 als „ohne Nachweis“ (`ErgebnisansichtEntscheideTests.cs:352`, `ErgebnisansichtTests.cs:60`) |
   | 1030 (Ref., Anker) | Sammelposten 101600097 (18.000 € Wartung BHKW), 101600098 (2.000 € Wartung Kessel) neben VDI-2067-Nullzeilen; Investition 295.000 € nur an 14920 | keine Quelle für die Aufteilung | keine | Referenzlauf byte-gleich (keine Kosten), aber die Ankerwerte bewegen sich (−31.141.242,71 / −21.895.377,28); 164 Testdateien nennen 1030 (die Doppelzählung wirkt erst über „Sätze vorbelegen“, E10) |
   | 1026 (kein Ref.) | kein Stromträger in eps; Investitionen 101600083/101600082 = 0 | Stromträger 60 wie 1030 | keine | gewollter Prüffall: `Co2StromtraegerRueckfallTests.cs:53` (`PROJEKT_OHNE_STROM` = 1026), `BetriebskostenBemessungsmatrixTests.cs:357-373`, `InvestKaskadeTests.cs:356` |

   Fazit: Keine Änderung an einem Referenzprojekt bleibt byte-gleich, deshalb nur benennen. 1018-Träger und 1023 sind
   Kandidaten für eine spätere Neueinfrierung (R15 läuft parallel mit E22, dort nicht mit hineinnehmen).
3. **Kommentar `KiDialoge.cs:1777` veraltet.** Der Dialog führt 35 Felder (Zeile 1772 stimmt), die Teilzahlen im
   Kommentar waren falsch: der Parametersatz führt **siebzehn** Felder (fünf Allgemein, acht Strom/Brennstoff/
   Bilanzierung inklusive `unternehmensart`, vier Risiko), die zwei Szenariosätze **achtzehn** (vierzehn wirksam,
   vier mit E9b gepflegt).

## Gebaut

- **E21/1 — Ressourcen und Designer** (`25984cbd`): `PREIS_ST_GRUND_EINHEIT` und `STEUER_ENERGIEST_54_BEMESSUNG` aus
  `Resource.resx`, `Resource.en-US.resx` und dem Designer gestrichen (E21‑Q1 a); `Werkzeuge/ResourceDesigner`
  neu geschrieben, 11.006 Einträge, zweiter Lauf +0.
- **E21/2 — Kommentare und Rückfalltexte** (`02ea7650`): `KiDialoge.cs` ~1777 berichtigt (siebzehn Felder des
  Parametersatzes, achtzehn Szenariosätze, vierzehn davon wirksam, vier mit E9b gepflegt); `SteuerGutschriftRechner.cs`
  ~691 vermerkt die Streichung aus E21/1; `EnergietraegerHuelle.cs:2531/2534/2542` (`PREIS_ST_QUELLE_RUECKFALL`,
  `PREIS_ST_GRUND_KEIN_JAHR`, `PREIS_ST_EMPFOHLEN`) schließen das Anführungszeichen jetzt wie die resx mit U+201C
  (E21‑Q2 a) — alle 22 Code-Rückfälle der B3/B4/F-Serie sind zeichengleich mit der deutschen resx, das Prüfskript
  meldet 0 Abweichungen. Kein neuer oder geänderter Ressourcenschlüssel in diesem Commit.

## Schlüssel

**2 gestrichen** (de/en): `PREIS_ST_GRUND_EINHEIT`, `STEUER_ENERGIEST_54_BEMESSUNG`. Je Sprache 11.011 → 11.009
Einträge; der Designer 11.008 → 11.006 Eigenschaften, wiederholbar (+0 im Prüflauf).

## Fragen aus der Welle

Entschieden hat der Orchestrator am 25.09.2026 (09:45) mit der Baufreigabe, nach Empfehlung — alle a; gebaut ist
jeweils der Entscheid (→ Register R‑E21).

| Frage | Lesarten | Entscheid |
|---|---|---|
| **E21‑Q1** beide verwaisten Schlüssel | (a) streichen, Kommentar `SteuerGutschriftRechner.cs` nachziehen; (b) belassen | a |
| **E21‑Q2** drei Rückfall-Literale | (a) an die resx angleichen; (b) belassen | a |
| **E21‑Q3** 1018, Kessel ohne Energieträger | (a) benennen, nicht pflegen; (b) mit 63 (Erdgas E) pflegen | a |
| **E21‑Q4** 1018, Puffer ohne Temperaturpaar | (a) benennen, nicht pflegen; (b) mit einer Probe (70/50) pflegen | a |
| **E21‑Q5** 1024, WP-Kennlinie bis 20 °C | (a) kein Datenfehler feststellen; (b) erfundene Stützstellen ergänzen | a |
| **E21‑Q6** 1023, Kessel ohne Energieträger | (a) benennen, nicht pflegen; (b) mit Träger und eps-Zeile pflegen | a |
| **E21‑Q7** 1030, zwei Sammelposten | (a) nicht anfassen (Anker); (b) aufteilen | a |
| **E21‑Q8** 1026, kein Stromträger | (a) gewollter Prüffall, belassen; (b) mit Stromträger 60 pflegen | a |
| **E21‑Q9** PV-Projekt mit vollständigen Preisen | (a) später als eigene Welle, neues Projekt außerhalb der Referenzliste; (b) an einem Referenzprojekt nachziehen | a |

## Abweichungen und Befunde

1. **Nr. 23 war schon erledigt** — der Sammelnachtrag lief mit vier früheren Commits; E21 hat nur die beiden
   verwaisten Reste gestrichen und drei Rückfall-Literale angeglichen.
2. **Kein Referenzprojekt bleibt bei einer Pflege der Nr.-24-Lücken byte-gleich** — deshalb ist mit dieser Welle
   nichts an der Testdatenbank geschrieben; die Befundtafel oben benennt Lücke, Grund und Wirkung je Projekt.
3. **1018-Träger und 1023 sind Kandidaten für die nächste Neueinfrierung** (R15, parallel zu E22, dort nicht mit
   hineinnehmen); 1024 ist kein Datenfehler; 1030 bleibt Anker; 1026 ist ein gewollter Prüffall.
4. **PV-Projekt mit vollständigen Preisen** (E21‑Q9): eine spätere, eigene Welle mit einem neuen Projekt außerhalb
   der Referenzliste — nicht Teil dieser Welle.
5. **Kommentar `KiDialoge.cs:1777`** war seit E19 als veraltet benannt (dort nicht beauftragt) — mit E21/2 berichtigt.

## Nachweis

- **Ohne Rechenwirkung:** Anker unberührt; Referenzlauf 13/13 gegen `2026-09-24_R14_Kaelteerzeuger` PASS, 4.207.049
  Werte, 394/394 CSV byte-gleich. Testdatenbank unverändert (`a427aa72`, keine Zeile geschrieben, kein Schemaschritt).
- **Phase 1** (Worktree `e21`): Build 0 Fehler; SqlDialektPruefer 1.921/0; gefiltert 729/0 (Kern 313, UI 340,
  KiKern 76); voller Lauf 14.027 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern 6.822 und 1 übersprungen,
  EPOS.UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen); Testhost-Regel eingehalten
  (vor dem gefilterten Lauf zwei fremde Testhosts abgewartet).
- **Merge** `e7c2f8f7` auf `pm21` über `b5cc1a61` ohne Konflikt; Designer 11.006, wiederholbar (+0).
- **Gate:** Kern-Filter 0 Fehler, ChartProben 161/161 gleich der Windows-Messlatte, voller Lauf 14.090 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern 6.885 und 1 übersprungen, EPOS.UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen), Dokumentationswachen 29/29 (`GATE506b.log`, 25.09.2026 10:28–10:56 Uhr, auf dem End-Merge `365143e1` mit `f52d38ec` = #505, Schemaschritt 142).
- **CI:** steht aus (Beobachtung nach dem Push).

## Logbuch

Kein Eintrag — keine sichtbare Änderung (Ressourcen- und Kommentarpflege, keine Datenpflege).

## Papiere mit der Statuszeile

Konzept (§ 6.3 Nr. 23 erledigt als Einzeiler, Nr. 24 neuer Wortlaut „gemessen 25.09.2026, benannt: …“ mit den fünf
Projekten und den Kandidaten für die nächste Neueinfrierung, Kopfzeile Codestand `e7c2f8f7`, „E19 und E21 ohne
Schritt“), Register (neue Familie R‑E21 mit E21‑Q1…Q9), Protokoll der Entscheidwege (§ 8.40, § 8.41), Analysepapier
(§ 5, Zeile E21, ohne Schemaschritt), Index (Reporting +1). Kein Mockup, kein Wiki, kein Logbuchsatz — E21 ändert
keine sichtbare Bedienung.

## Offen

- **Kandidaten 1018-Träger und 1023** bei der nächsten ohnehin fälligen Neueinfrierung der Referenzbasis (nach R15,
  parallel zu E22, dort nicht mit hineinnehmen).
- **PV-Projekt mit vollständigen Preisen** (E21‑Q9) als eigene, spätere Welle, außerhalb der Referenzliste.
- **Gate** und **CI** (Nachweis oben).
- Die offenen Abnahmen und Restpunkte aus früheren Statuszeilen (Nach #498 (g) und früher) bleiben unverändert offen.
