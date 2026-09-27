# E21 — Bericht Phase 0 (Opus, 25.09.2026 ca. 09:40, Worktree e21 ab cbed6dba, nur gelesen/gemessen; Referenzlauf-Proben auf Kopien im Scratchpad)

## Nr. 23 resx-Sammelnachtrag — längst gelaufen, § 6.3 Nr. 23 veraltet
Commits aeece2feb (sechs Schlüssel B3a/B3b), 2cfb871dc (zehn PREIS_ST_* B4, zwei SIM_KARTE_* F2), 2a8b95335 (sechs KOH_* FX5), 8f07cce93
(Designer). Messung: de 11.011, en 11.011, Designer 11.008 (Differenz Bitmap1/Color1/Icon1); Dubletten 0; de/en gleich; Designer wiederholbar.
26 Schlüssel aus B3a/B3b/B4/F2/FX1–FX5 vorhanden mit Leser, zwei Ausnahmen: `PREIS_ST_GRUND_EINHEIT` (de :10825, en :10818, Designer :56644)
ohne Leser seit iU9-W4.4 (02efc0336) → streichen; `STEUER_ENERGIEST_54_BEMESSUNG` (de :8289, en :8282, Designer :73522) seit B3a nicht mehr
erzeugt, Kommentar `SteuerGutschriftRechner.cs:691-695` → streichen + Kommentar. `WIRT_CO2_STROMMIX_RUECKFALL`, `KOH_FALL5_MISCHLAGE` sauber
gestrichen (Nachfolger vorhanden). Rückfalltexte: 21/24 zeichengenau; drei Literale mit geradem `"` statt `“`: `EnergietraegerHuelle.cs:2531`
(PREIS_ST_QUELLE_RUECKFALL), :2534 (PREIS_ST_GRUND_KEIN_JAHR), :2542 (PREIS_ST_EMPFOHLEN). Nebenbefund: 45 „fehlende" Code-Schlüssel sind
Präfixe/Prüfmuster/`WIRT_SZEN_HINWEIS` (Test prüft Fehlen) — keiner echt. Designer nach dem Merge mit e20 erneut laufen lassen.

## Nr. 24 und Datenlücken — an Referenzprojekten nicht byte-gleich haltbar oder gewollte Prüffälle
(Herkunft: 1030/1026 aus E10 `e10_berichte.md:66-67, 158-165`, nicht e9_fakten; PV-Preislücke aus E9a `e9a_berichte.md:163, 169`.)
| Projekt | Lücke | Wert/Quelle | Einfrierregel | Wirkung |
| 1018 (Ref.) | `Tab_Energieanlagen.ID_Carrier` NULL Anlage 10369 (Kessel 1018251, Brennstoff 3) | 63 Erdgas E (wie BHKW 11327, Kessel in 1026/1030) | nicht in der Liste; Em.Kessel gleich (eps 10063 co2 = 240) | gemessen FAIL: 1 Abweichung `HeizkesselModul[0].carrier_id` leer → 63 |
| 1018 (Ref.) | `Tab_Pufferspeicher.Vorlauf/Ruecklauf` NULL 1054173/1054175 (+ Verwendung NULL) | keine Quelle; Probe 70/50 | keine Regel, aber Simulation | 12 Dateien anders, Puffer Q_max 11,19 → 22,39 kWh, Ladung 36,7 → 64,5 MWh, Em.Bhkw.Co2T 22,339 → 22,344 |
| 1024 (Ref.) | `Tab_Kenndaten` ID_WP 1034317 Quelltemperaturen nur bis 20 °C | Herstellerkatalog (Stamm 59) endet bei 20 °C, keine Quelle; Kern kappt, extrapoliert nicht (`SimulationWaermepumpe.cs:437`), Warnung richtig | WP-Kennlinie nicht in der Liste | nicht gemessen (Probe vom Klassifizierer abgelehnt); erfundene Daten nicht empfohlen |
| 1023 (Ref.) | `ID_Carrier` NULL Kessel 11205 (1018254, Brennstoff 3); keine eps-Zeile Erdgas → kein Kapitalwert | 63 + eps-Zeile (Preis ~0,84 €/Nm³ aus 1030) | eps-Zeile mit co2 fällt unter die Regel; CO₂ 240 → 201 g/kWh | gemessen FAIL: Em.Kessel.Co2T 22,44 → 18,79 t/a + carrier_id; Tests nutzen 1023 als „ohne Nachweis" (`ErgebnisansichtEntscheideTests.cs:352`, `ErgebnisansichtTests.cs:60`) |
| 1030 (Ref., Anker) | Sammelposten 101600097 (18.000 € Wartung BHKW), 101600098 (2.000 € Wartung Kessel) neben VDI-2067-Nullzeilen; Investition 295.000 € nur an 14920 | keine Quelle für Aufteilung | keine | Referenzlauf byte-gleich (keine Kosten), aber Anker −31.141.242,71 / −21.895.377,28 (164 Testdateien nennen 1030) bewegen sich; Doppelzählung nur latent (wirkt erst über „Sätze vorbelegen" E10) |
| 1026 (kein Ref.) | kein Stromträger in eps; Investitionen 101600083/101600082 = 0 | Stromträger 60 wie 1030 | keine | gewollter Prüffall: `Co2StromtraegerRueckfallTests.cs:53` (PROJEKT_OHNE_STROM = 1026), `BetriebskostenBemessungsmatrixTests.cs:357-373`, `InvestKaskadeTests.cs:356` |
Fazit: keine Änderung an einem Referenzprojekt bleibt byte-gleich → in E21 nur benennen. 1018-Träger und 1023 = Kandidaten für eine spätere
Neueinfrierung (R15 läuft parallel mit E22, dort nicht mehr hineinnehmen).

## Kommentar `KiDialoge.cs:1777`
Dialog 35 Felder (Zeile 1772 stimmt); Teilzahlen falsch: Parametersatz **siebzehn** (fünf Allgemein, acht Strom/Brennstoff/Bilanzierung inkl.
`unternehmensart`, vier Risiko), Szenariosätze **achtzehn** (vierzehn wirksam, vier E9b gepflegt). Vorschlag ASCII: „Siebzehn Felder gehoeren
dem Parametersatz des Projekts, achtzehn den zwei SZENARIOSAETZEN Best und Worst – je Groesse eines; vierzehn davon zeigen den WIRKSAMEN Wert …".

## Fragen (Entscheid Orchestrator 25.09.2026 nach Empfehlung, alle a)
Q1 beide Schlüssel streichen + Kommentar SteuerGutschriftRechner; Q2 drei Literale angleichen; Q3 1018 Träger benennen; Q4 1018 Puffer
benennen; Q5 1024 „kein Datenfehler"; Q6 1023 benennen; Q7 1030 nicht anfassen; Q8 1026 Prüffall; Q9 PV-Projekt mit vollständigen Preisen
später als eigene Welle (neues Projekt außerhalb der Referenzliste). Aufwand Phase 1 ~1 h + Gate; kein Schema, keine DB, kein LFS.
Bau freigegeben 25.09.2026 09:45: E21/1 Ressourcen + Designer, E21/2 Kommentare + Literale.
Skripte/Proben: scratchpad/resx_scan.py, fallback.py, q.py, probe/p0…p3, ohne_leser.txt.

# E21 — Bericht Phase 1 (Opus, 25.09.2026 ca. 09:10, e21 = 02ea7650 ab cbed6dba; Merge #501 = 36a3fc89 auf pm21 ab b5cc1a61)

Commits: 25984cbd E21/1 zwei Schlüssel (`PREIS_ST_GRUND_EINHEIT`, `STEUER_ENERGIEST_54_BEMESSUNG`) aus de/en/Designer gestrichen, Designer
11.008 → 11.006 wiederholbar (Rumpf mit Schreibfehler „Werkzeugs/", belassen); 02ea7650 E21/2 Kommentare `KiDialoge.cs` ~1777 (siebzehn
Parametersatz, achtzehn Szenariosätze, vierzehn wirksam, vier E9b gepflegt) und `SteuerGutschriftRechner.cs` ~691 (Streichung vermerkt), drei
Rückfalltexte `EnergietraegerHuelle.cs:2531/2534/2542` auf „…“ (alle 22 Code-Rückfälle B3/B4/F-Serie zeichengenau = de-resx, Prüfskript 0
Abweichungen). Kein neuer/geänderter Schlüssel. Prüfungen: Build 0 Fehler; SQL-Prüfer 1.921/0; gefiltert 729/0 (Kern 313, UI 340, KiKern 76);
voller Lauf 14.027 / 0 / 2 (Kern 6.822+1, UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1); Testhost-Regel eingehalten
(vor gefiltertem Lauf zwei fremde testhosts abgewartet); Referenzlauf 13/13 gegen R14 PASS, 4.207.049 Werte, 394/394 byte-gleich; Testdatenbank
unverändert (a427aa72). Erledigt-Gründe: Nr. 23 erledigt (Sammelnachtrag aeece2feb/2cfb871dc/2a8b95335/8f07cce93 + E21-Streichungen,
de/en deckungsgleich, keine Dubletten); Nr. 24/Datenlücken nur benannt (Tafel Phase 0; 1018-Träger und 1023 Kandidaten für die nächste
Neueinfrierung; 1024 kein Datenfehler; 1030 Anker; 1026 Prüffall; PV-Projekt Q9 eigene Welle); kein BETRIEB_SQLITE-Hinweis; kein Logbuchsatz;
A‑E21‑1 entfällt. Merge #501 = 36a3fc89 (ort ohne Konflikt; Designer +0 wiederholbar) auf b5cc1a61 (Cloud-Sitzung „claude/exciting-jennings"
nach #497). Logs: scratchpad/e21_test_gefiltert.txt, e21_test_voll.txt, rl_e21, rl_e21_lauf.log.
