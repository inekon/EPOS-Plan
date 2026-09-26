# E30 — Hilfsenergiekosten aus dem Anlagenanteil, BHKW-Stromdeckung am Gesamtbedarf, Datenpflege 1030/1026, Kapitalwert-Anker 1030, Referenzbasis R21 (Protokoll, 26.09.2026)

Statuszeile #548 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: die Sichtprüfung der
Betriebskosten von 1030 (P1030, Befunde B3, B4, B5 und B8; Statusdatei, Nach #535 (h); Konzept § 6.3 Nr. 21) und der
Befund N10 aus E29 (Nach #536 (e); § 6.3 Nr. 36) mit den Anwenderentscheiden vom 26.09.2026 (~09:50); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 3.4 (Absatz „Hilfsenergiekosten aus dem Anlagenanteil“), § 3.6, § 6.1, § 6.2 und § 6.3 Nr. 21 und 36;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E30 (neu), R‑Rest (Nr. 21) und R‑E29 (E29‑Q12); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; Referenzbasis [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md), Abschnitt „Aktuelle Basis“.
Vorgänger: [`E29_Stromausweis_Anzeige_Protokoll.md`](E29_Stromausweis_Anzeige_Protokoll.md). Zweig `e30` von `ebf01a90`;
Opus 5.5 im Worktree `.claude/worktrees/e30`: `4dc4a662` (E30/1), `cefe1b54` (Kommentare), `b9e35a3e` (E30/2),
`44e52518` (E30/4), `2299b8da` (E30/5 Tests), `bd338f1f` (Merge `origin` `50fa9e5a`), `fb936e4c` (E30/3), `9597762f`
(E30/5 Messlatten), `fb2dcc3f` (E30/6); 34 Dateien ohne die Basisordner, +1.799/−149. Merge `7d1b6d28` („Merge e30:
Hilfsenergiekosten aus dem Anlagenanteil, BHKW-Deckungsgrad, Datenpflege 1030/1026, Anker 1030, Referenzbasis R21
(#542)“) auf `pm26` über `origin` = `003bc8a3` (#542 Dialogdarstellung, #543 Zapfprofil; Zwischenmerge `778755f9`),
Folgecommit `593e9048` (#548 statt #542 in Kommentaren und Papieren). **Kein Schemaschritt; neue Referenzbasis
`2026-09-26_R21_BhkwDeckung`** (R20 archiviert); Testdatenbank `22e67400` → `40df1bf2` (LFS-oid
`40df1bf2a9e0544368322e25e9651ee7ac751204773d177d51a4f1a12ef475ee`, 70.680.576 Byte, Schemastand 148).

**Nummernkreuzung:** Die Welle lief als #541; BV‑E5 hat #541 gepusht, Dialogdarstellung #542 — E30 ist **#548**.

## Befund vor der Welle (Phase 0, Worktree `e30` = `ebf01a90`, nur Kopien der Testdatenbank)

1. **B4 — Hilfsenergie hat nur einen Mengenweg.** `Tab_Energieanlagen.Hilfsenergie_Anteil` (% des Brennstoffs der
   Anlage, Schritt 61) ergibt `HilfsstromRechner.MengeMWh` und mindert allein die KWKG-Nettomenge; gepflegt wird er nur
   im BHKW-Wirtschaftlichkeitsdialog. Kosten entstehen nirgends. Der Kostenweg — die Betriebskostenposition
   „Hilfsenergiekosten“ mit Weg A (`PROZENT_ENDENERGIEKOSTEN`), Weg B (`PROZENT_ENDENERGIEBEDARF`) oder Jahresbetrag —
   rechnet nur mit einem gepflegten Satz. In der ganzen Testdatenbank trägt keine Anlage einen Anteil und keine
   Hilfsenergieposition einen Satz oder Betrag.
2. **Strommatrix:** `STROMBEDARF_GESAMT` enthält Gebäude, Wärmepumpe, Heizstab, Elektrokessel und Kälte samt
   Kälte-Hilfsstrom, **nicht** den Hilfsstrom von BHKW und Brennstoffkessel (die Strommatrix bleibt brutto).
   Deshalb fehlen die Kosten genau dort, wo ein Anteil gepflegt ist, und eine Bepreisung des Anteils zählt gegen die
   Energiekosten nicht doppelt; der Kälte-Hilfsstrom (`Tab_WP.Kuehl_Hilfsstromanteil`) steckt schon im Netzbezug.
3. **N10:** Stromring, Stromtabelle und Word-Torte zählen beim BHKW die ganze Produktion samt Einspeisung;
   `BHKW.Strombedarfsdeckung` teilt durch den Projekt-Strombedarf. Neu gerechnet: 1017 5,48 → 5,31, 1024 26,22 → 20,94,
   1047 5,34 → 5,30, 1030 9,02 (9,025 → 9,017), 1018 0 — drei `aggregate.csv` wandern, `kern.yml` hält 1017 und 1047.
4. **B3/B5:** Altzeilen `101600097` (18.000 €, Anlage 14920) und `101600098` (2.000 €, 11334) neben den leeren
   Pflichtzeilen `101600588` und `101600585`; fünf Hilfsenergie-Pflichtzeilen mit Weg A (1030: `101600587`, `590`, `593`;
   1026: `101600570`, `576`), deren Vorlagen seit Schritt 94 Weg B führen. Die Probe der Datenpflege auf einer Kopie:
   Betriebskosten und Kapitalwerte in drei Szenarien bitgleich.
5. **B8:** Der Kernanker −21.895.377,28 € rechnet mit dem gebuchten Lauf 212 vom 30.08.2026 — vor B‑1 ohne
   Kesselbrennstoff (`Verbrauch = 0`, `KesselVerbrauchFehlt`), BHKW-Brennstoff vor R10 (1.048,27 statt 1.241,55 MWh).
   Der Berichtsweg rechnet frisch −31.142.971,06 €. **Lauf 212 ist veraltet; richtig ist −31.142.971,06 €.**

## Messung

**B8 — Zerlegung der Ankerdifferenz** (Kopie der Testdatenbank, beide Wege):

| Ursache | Lauf 212 | frisch | Energiekosten |
|---|---|---|---|
| Kessel ohne Brennstoff (vor B‑1) | 0 MWh | 5.403,1 MWh | +432.248 €/a (× 0,08 €/kWh) |
| BHKW-Brennstoff vor R10 | 1.048,27 MWh | 1.241,55 MWh | +15.462 €/a |
| E27-Klemme Netzbezug | 4.357,78 MWh | 4.358,17 MWh | +97,50 €/a |
| **Summe Energiekosten** | 1.176.906,60 €/a | 1.624.713,70 €/a | **+447.807,10 €/a** |
| CO₂-Abgabe (55 €/t × +1.343,1 t) | 13.837,16 €/a | 87.709,25 €/a | +73.872,08 €/a |

(447.807,10 + 73.872,08) × 17,7267 ≈ 9,2476 Mio. € Barwert der Ausgaben; KWKG +54,25 € → **ΔKW −9.247.593,78 €** =
Differenz der Anker (bis auf < 5 €, KWKG-Split). Betriebskosten 20.000 in beiden gleich. Probe: Lauf 212 auf einer Kopie
neu gebucht → Kernweg −31.143.024,30 € (Rest −53,24 € = KWKG-Split ohne Zeitreihen, 7.316,08 gegen 7.322,63 €). Für 1024
gilt dasselbe (Lauf 199 vom 26.08.2026: −2.896.359,13 gegen −2.772.642,27 €).

**B4 — Hilfsenergiekosten an einer Kopie von 1030:**

| Fall | Menge | Betrag |
|---|---|---|
| 2 % an beiden BHKW, gespeicherter Altlauf 212 | 1.048,27 MWh × 2 % = 20,97 MWh | 5.241,35 €/a (× 0,25 €/kWh) |
| 2 % an beiden BHKW, frischer Lauf | 1.241,55 MWh × 2 % = 24,83 MWh | ≈ 6.207,75 €/a (ΔKapitalwert ≈ −110.043 €, Barwertfaktor p_E 17,7267) |
| Gaskessel 0,5 % | — | 6.753,88 €/a |

**N10 — Stromdeckung des BHKW** (R20-Werte, MWh/a):

| Projekt | Produktion | Einspeisung | Eigenverbrauch | Bedarf aller Verbraucher | R20 | R21 |
|---|---|---|---|---|---|---|
| 1017 | 36,805 | 0 | 36,805 | 692,681 | 5,48 | **5,31** |
| 1018 | 27,458 | 27,458 | 0 | 0 | 0 | 0 |
| 1024 | 95,701 | 0 | 95,701 | 456,982 | 26,22 | **20,94** |
| 1030 | 432,305 | 0,392 | 431,913 | 4.790,086 | 9,02 | 9,02 (9,025 → 9,017) |
| 1047 | 35,867 | 0 | 35,867 | 677,043 | 5,34 | **5,30** |

## Fragen aus der Welle

Den Anlass hat der Anwender am 26.09.2026 (~09:50) entschieden: B3 „bereinigen“, B5 „Prozent des Endenergiebedarfs“,
B4 „Änderung vornehmen: Wenn Hilfsenergie angegeben ist, müssen die Hilfsenergiekosten daraus ermittelt werden“, B8
„nach Empfehlung: klären“, N10 „nach Empfehlung: korrigieren“. Die Fragen E30‑Q1 bis Q12 stellt der Phase‑0-Bericht,
entschieden hat sie der Orchestrator am 26.09.2026 (10:05) mit der Baufreigabe, nach Empfehlung; gebaut ist jeweils
der Entscheid (→ Register R‑E30).

| Frage | Gegenstand | Entscheid |
|---|---|---|
| **E30‑Q1** Ort (B4) | (a) Pflichtzeile ohne Satz/Betrag trägt den Betrag aus dem Anlagenanteil (Weg B), fehlt sie → abgeleitete Zeile; (b) immer eigene abgeleitete Zeile; (c) in die Energiekosten | a |
| **E30‑Q2** Doppelpflege | (a) gepflegte Position gilt, Anteil nur KWKG, Hinweis neu formuliert; (b) Anteil gilt, Position ruht; (c) beide addieren | a |
| **E30‑Q3** Anlagen | (a) jede Anlage mit Anteil > 0 und Brennstoffmenge (BHKW, Brennstoffkessel), Wärmepumpe/Elektrokessel/Kälte ausgenommen; (b) nur BHKW | a |
| **E30‑Q4** Preis | (a) Arbeitspreis des Projekt-Stromträgers ohne Grund-/Leistungspreis, Szenariopreis folgt, Einspeisefall benannte Näherung; (b) Mischpreis Eigen/Bezug | a |
| **E30‑Q5** Emission/Steuer des Hilfsstroms | nur benennen | so |
| **E30‑Q6** Referenzdaten | (a) keinen Anteil an 1030 pflegen, B4 auf Kopien testen; (b) 2 % an beiden BHKW | a |
| **E30‑Q7** N10 | (a) Eigenverbrauch/Gesamtbedarf an allen fünf Stellen samt Persistenz, Ring und Tabelle mit dem Eigenanteil; (b) nur die persistierte Größe | a (ohne Tooltip der Einspeisung in der Stromtabelle) |
| **E30‑Q8** B3 Form | (a) Pflichtzeilen auf `JAHRESBETRAG` 18.000/2.000; (b) Bemessung lassen, Betrag über I‑2 | a |
| **E30‑Q9** B3 Kaskade | (a) 18.000 bleibt an 14920; (b) teilen 14920/14921 | a |
| **E30‑Q10** B5 Umfang | (a) nur die fünf Zeilen 1030/1026; (b) auch 1019 | a |
| **E30‑Q11** B8 | (a) Läufe 212/199 neu buchen; (b) Anker benennen, Erklärtest, § 6.2 beide Anker mit Weg; (c) Kerntest auf den Berichtsweg | b |
| **E30‑Q12** Katalog | Empfehlung „Hilfsenergiekosten (Strom)“ 4–8 % am Kessel stammt aus Weg A, in Weg B zu hoch — nur benennen | so (eigener Katalogauftrag) |

## Gebaut

- **E30/1 — Datenpflege 1030/1026** (`4dc4a662`; Q8 a, Q9 a, Q10 a): Skript
  `Referenzlaeufe/Skripte/datenpflege_1030_1026_betriebskosten.cs` (Vorzustand zellgenau, eine Transaktion in einer
  Arbeitsdatei, `integrity_check`, `foreign_key_check`, zweiter Lauf 0; Abweichung → 2, Datei unverändert); 9 Zeilen:
  `101600588` → `JAHRESBETRAG` 18.000, `101600585` → `JAHRESBETRAG` 2.000, `DELETE` `101600097`/`101600098`, fünf
  Hilfsenergiezeilen → `PROZENT_ENDENERGIEBEDARF`; Wache `DatenpflegeBetriebskosten1030Tests`; Zeilen-IDs in
  `BetriebskostenBemessungsmatrixTests` und `HilfsstromBemessungVorlageTests` auf die Pflichtzeilen.
- **E30/2 — Hilfsenergiekosten aus dem Anlagenanteil** (`b9e35a3e`; Q1 a…Q4 a): neue Klasse
  `EPOS.Kern/Allgemein/Wirtschaftlichkeit/HilfsenergieAusAnteil.cs` (`Plane` `:94`, `IstHilfsenergiePosition` `:149`);
  `LiesBetriebskostenTopfe` (`WirtschaftlichkeitCtrl.cs:7185` ff.) und `LiesBetriebskostenPositionen` (`:7459` ff.)
  nutzen denselben Plan, Betrag über `HilfsenergieBetrag` (`:7790`); abgeleitete Zeile `HILFS_ANTEIL_POSITION`, Herkunft
  `HILFS_ANTEIL_HERKUNFT` in der Herleitung (`WirtschaftlichkeitZeilen.cs`); Kohärenzhinweis `KOH_HILFSENERGIE_DOPPELT`
  neu formuliert (`KohaerenzPruefung.cs`); Tests `HilfsenergiekostenAusAnteilTests`.
- **E30/4 — Kapitalwert-Anker** (`44e52518`; Q11 b): Kernanker `Kapitalwert_des_gespeicherten_Altlaufs_212_ist_absolut_gepinnt`
  (`WirtschaftlichkeitAnkerTests.cs:323`, Wert unverändert −21.895.377,28 €); neu `KapitalwertAnkerZerlegungTests`
  (`Der_Kernanker_rechnet_mit_dem_Altlauf_212_ohne_Kesselbrennstoff`,
  `Die_Differenz_der_Anker_zerlegt_sich_in_Kesselbrennstoff_BHKW_Netzbezug_und_CO2`).
- **E30/5 — Tests** (`2299b8da`): `KostenVorlagenhinweisTests` nach B3/B5 (die Abweichung der Vorlage jetzt an der
  Kesselwartung), B4 am frischen Lauf.
- **Merge `origin` `50fa9e5a`** (`bd338f1f`): LFS-Konflikt der Testdatenbank — die `origin`-Fassung `22e67400`
  (Speicherauslegung #543) genommen und das Skript darauf gezogen (Vorzustand passte, 9 Zeilen, `integrity_check` ok,
  zweiter Lauf ohne Änderung) → `40df1bf2`; danach `ZapfprofilReferenzprojektWacheTests`,
  `DatenpflegeBetriebskosten1030Tests` und die Zapfprofil-/Speicherauslegungstests 307/307 grün.
- **E30/3 — N10** (`fb936e4c`; Q7 a): `SimulationErgebnisCtrl.BhkwStromdeckungProzent` (`:847`), `BhkwEigenverbrauchMwh`
  (`:822`), `StrombedarfVerbraucherMwh` (`:834`); Lauf `SimulationRunner.cs:628` (persistiert, `aggregate.csv`, Word-Torte
  über den persistierten Wert), BHKW-Reiter (`SimulationErgebnisCtrl.cs:794`), Übersicht mit
  `UebersichtKennzahlen.BhkwStromEigenverbrauchMwh` in Stromring (`SimulationErgebnisHuelle.Bilder.cs:337`) und
  Stromtabelle (`…Anzeige.cs:383`); Tests `BhkwStromdeckungTests`, `SimulationErgebnisCtrlTests` nachgezogen.
- **E30/5 — Messlatten** (`9597762f`): Word/Excel 1030 begründet neu (unten).
- **E30/6 — Referenzbasis R21** (`fb2dcc3f`): `Referenzlaeufe/2026-09-26_R21_BhkwDeckung/` (432 CSV und `protokoll.txt`);
  R20 `protokoll.txt` per `git mv` nach `Dokumentation/ueberholt/Referenzbasen/2026-09-26_R20_Zapfprofil/`, der Rest
  entfernt; Archiv-LIESMICH (38 Basen, Abschnitt „Die Basis R20 im Einzelnen“); `Referenzlaeufe/LIESMICH.md` „Aktuelle
  Basis“ mit A/B-Tafel und Nachtrag Datenpflege; Basisname in `CLAUDE.md`, `kern.yml` (7), `ios.yml` (2),
  `Dokumentation/LIESMICH.md` und Basenhistorie; der Verweis im Umsetzungskonzept Zapfprofilgenerator (3.4 d) wegen der
  Linkwache auf das archivierte Protokoll.

**Texte:**

| Schlüssel | de | en |
|---|---|---|
| `HILFS_ANTEIL_POSITION` | Hilfsenergiekosten (aus dem Anlagenanteil) | Auxiliary energy costs (from the unit's share) |
| `HILFS_ANTEIL_HERKUNFT` | Satz aus dem Hilfsenergieanteil der Anlage | rate from the unit's auxiliary energy share |
| `KOH_HILFSENERGIE_DOPPELT` (neu formuliert, zweiter Satz) | Die Kosten rechnet die Kostenposition; der Anteil an der Anlage mindert nur den KWK-Zuschlag und wird nicht zusätzlich bepreist. | The cost item carries the costs; the share at the unit only reduces the CHP bonus and is not priced a second time. |

**Messlatten** (Grund B3, zwei Positionen weniger; N10 ändert keine Zeile):

| Datei | alt | neu |
|---|---|---|
| `Bericht_Word_1030.txt` Z. 71 | Tabelle 5 Sp. × 18 Z. | 16 Z. |
| `Bericht_Word_1030_Vorlage.txt` Z. 68 | Tabelle 5 Sp. × 18 Z. | 16 Z. |
| `Bericht_Excel_1030.txt`, Blatt 3 (8-Spalten-Fassung aus E29) | 171 Z. | 169 Z. |
| `Bericht_Excel_1030.txt`, Summe der Positionen | Z165 `=SUM(E149:E164)` | Z163 `=SUM(E149:E162)` |
| `Bericht_Excel_1030.txt`, „Bewertung nach DIN EN 17463“ | Z167 | Z165 |

**A/B gegen R20** (14 Projekte): 11/14 PASS, 429/432 CSV byte-gleich; FAIL allein je ein Skalar
`BHKW.Strombedarfsdeckung` in `aggregate.csv` von 1017 (5,48 → 5,31), 1024 (26,22 → 20,94) und 1047 (5,34 → 5,30); alle
Zeitreihen byte-gleich; der Einfrierlauf ist mit dem A/B-Lauf 432/432 byte-gleich.

## Nachweise

- **Build:** Release `WP-Plan.Kern.slnf` 0 Fehler; Windows-Schale Debug x64 0 Fehler.
- **Voller Lauf** mit den xUnit-Schaltern (Testhost-Regel eingehalten): Kern 8.267/8.268 (+1 übersprungen), UI 6.564,
  KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (+1 übersprungen) — 0 rot.
- **Referenzlauf** 14/14 gegen R21: GESAMT PASS, 4.610.207 Werte, 432/432 CSV byte-gleich; 1048 (Prüfprojekt ohne
  Referenzrolle) rechnet: PV 13,43 MWh, Deckung 20,21 %.
- **ChartProben:** 183 Hashes, gleich der Windows-Messlatte `messlatte_windows.sha256`.
- **SqlDialektPruefer:** nicht gezogen (Python auf der Testdatenbank gesperrt); die neuen SQL-Texte sind einfache
  SELECTs, `kern.yml` prüft sie.
- **Wirkung:** kein Kapitalwert-Anker bewegt (kein Referenzprojekt trägt einen Anteil, die Datenpflege ist
  ergebnisneutral); drei `aggregate.csv` (N10) → R21; Testdatenbank `40df1bf2`, kein Schemaschritt.
- **Merge** `7d1b6d28` auf `pm26` über `003bc8a3`, Folgecommit `593e9048`.
- **Gate:** Gate #548 auf `593e9048` (26.09.2026 11:55–12:19, Kern-Filter Release 0 Fehler, ChartProben 183 Bild-Hashes gleich mit der Windows-Messlatte, Tests mit Schaltern: KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (+1 übersprungen), EPOS.UI.Tests 6.574, EPOS.Kern.Tests 8.267 (+1 übersprungen); Dokumentationswachen 32/32 auf dem Papierstand `ce890772`)
- **CI:** steht aus (Beobachtung nach dem Push — Kern-Lauf gegen R21 und Windows-Lauf auf `main`)

## Abweichungen und Befunde

1. **Nummernkreuzung** #541 → #542 → #548 (BV‑E5 #541, Dialogdarstellung #542); die Kommentare trugen #542 und sind mit
   `593e9048` auf #548 gestellt.
2. **Testdatenbank beim Merge:** LFS-Konflikt, gelöst durch das wiederholbare Skript auf der `origin`-Fassung.
3. **Q7 a ohne Tooltip:** Die Stromtabelle trägt keinen Tooltip mit der Einspeisung; die Einspeisung zeigen BHKW-Reiter
   und Excel (E29).
4. **Aufwand:** ~5 h (davon ~1,5 h Läufe; Schätzung der Phase 0 ~10,5 h + Gate).

## Abnahme am Gerät (A‑E30‑1, Windows, Testdatenbank)

1. 1030, Kostenverwaltung Betriebskosten: „Wartung BHKW“ 18.000 €/a und „Vollwartung / Wartung Kessel“ 2.000 €/a als
   fester Jahresbetrag; die Altzeilen „BHKW“ und „Heizkessel“ sind weg; Summe 20.000 €/a; die Hilfsenergiezeilen stehen
   auf „% des Endenergiebedarfs“.
2. BHKW-Wirtschaftlichkeit 1030: Hilfsenergieanteil 2 % an beiden BHKW setzen und rechnen — rund +6.208 €/a Betriebskosten
   in den Hilfsenergiezeilen, Herleitung „Satz aus dem Hilfsenergieanteil der Anlage“; danach den Anteil wieder auf 0.
3. Simulation 1024: BHKW-Reiter „Strombedarfsdeckung“ 20,94 %; Stromring und Stromtabelle der Übersicht zeigen beim BHKW
   den Eigenverbrauch; 1018 zeigt keine Deckung über 100 %.

## Konzeptvermerk (E30‑Q1…Q12)

In Konzept § 3.4 der Absatz „Hilfsenergiekosten aus dem Anlagenanteil“ (Regel, Vorrang, Ausnahmen, Doppelzählungsschutz);
§ 3.6 der Satz zur Stromdeckung des BHKW (Absatz „BHKW-Einspeisung und Gesamtbedarf im Ausweis“); § 6.1 Zeile E30; § 6.2
der Kernanker als „gespeicherter Altlauf 212“ mit Verweis auf die Zerlegung und die Basis R21; § 6.3 Nr. 21 endgültig
geschlossen, Nr. 36 N10 erledigt (N11 bleibt offen).

## Logbuch

Logbuchsatz im [Wiki-Upload-Papier](../../../aktuell/Wiki_Update_2026-09-26.md) unter Version 1.2.0.4: „Ist an einer
Anlage ein Hilfsenergieanteil angegeben, ermittelt EPOS-Plan daraus die Hilfsenergiekosten (Menge × Strompreis) in der
Position „Hilfsenergiekosten“, sofern dort kein eigener Satz gepflegt ist. Der Deckungsgrad des BHKW zählt nur den selbst
verbrauchten BHKW-Strom und bezieht ihn auf den gesamten Strombedarf.“ Wiki-Quelle „Kosten“: Absatz
„Hilfsenergiekosten aus dem Anlagenanteil“ (Anker `hilfsenergie-anteil`).

## Papiere mit der Statuszeile

Statusdatei (#548, Nach #548); dieses Protokoll und der Eintrag im Dokumentations-Index (Reporting 144 → 145); Register
(Kopf, Familientafel, neue Familie R‑E30, R‑Rest Nr. 21 erledigt, R‑E29 Q12 N10 korrigiert); Konzept (§ 3.4, § 3.6,
§ 6.1 Zeile E30, § 6.2, § 6.3 Nr. 21 und Nr. 36, Basis R21); Analysepapier § 5 (Kopf, Zeile E30); die Basisnennung
R20 → R21 in Konzept Gebäudesimulation, Systementwurf und Umsetzungskonzept Zapfprofilgenerator; Wiki-Quelle „Kosten“ und
Wiki-Upload-Papier (Upload-Liste, Logbuchsatz). Im selben Papierschritt, aber nicht Teil von E30: der CI-Vermerk #536
(Statusdatei, Protokoll E29).

## Offen

- **Kostenraster-Satzfeld:** Eine Zeile mit Satz aus dem Anteil zeigt Betrag und Basis, aber ein leeres Satzfeld —
  Anzeige-Folgepunkt.
- **Katalogempfehlung** „Hilfsenergiekosten (Strom)“ 4–8 % am Kessel (`DbWerte.cs:617`, aus Weg A) ist in Weg B fachlich
  zu hoch — eigener Katalogauftrag (E30‑Q12).
- **Emission und Stromsteuer des Hilfsstroms** (E30‑Q5) — nicht angesetzt, Folgepunkt.
- **N11** — Flotten-Netzeinspeisung im Stapel der Strombilanz, offen, Anwenderentscheid.
- **Wiki-Fachtext** „Simulationsergebnisse“ (Definition der Stromdeckung des BHKW) — nicht nachgezogen.
- Die **Abnahme am Gerät** A‑E30‑1.
- **Gate** und **CI** (Nachweis oben).
