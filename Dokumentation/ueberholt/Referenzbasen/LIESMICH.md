# Die Protokolle der 24 entfernten Referenzbasen

**Was hier liegt.** Für jede der **24 historischen Referenzbasen** unter `Referenzlaeufe/` das
Protokoll ihrer Entstehung — `lauf_protokoll.md` beziehungsweise `protokoll.txt`, byte-gleich
aus dem Stand `b02f986^` (= dem letzten Commit vor der Löschung) gesichert. **25 Dateien,
603 913 Byte.** Nur Text: was gemessen wurde, gegen welche Vorgängerbasis, mit welchem
Ergebnis, welche Abweichung gewollt war und welche Gegenprobe sie belegt.

**Warum hier.** Die Ordner der Basen sind am 11.09.2026 mit dem Sync-Commit `b02f986` aus dem
Arbeitsbaum gefallen (Anwenderentscheid **SYNC‑Q1**: „entfernt lassen"); abrufbar blieben sie
über die Git-Geschichte. Mit dem Anwenderentscheid **AUF‑Q1** vom **12.09.2026** („ausführen",
Auftrag #244) ist die Geschichte umgeschrieben worden — **die Basen sind seither auch dort
nicht mehr enthalten.** Damit wäre die Begründung jeder einzelnen Basiswahl verloren gegangen.
Deshalb sind die Protokolle **vor** dem Umschreiben hierher gesichert worden.

> **Die Messdaten selbst sind endgültig weg.** Die rund **7 700 CSV-Dateien** der 24 Basen
> (etwa 1 016,7 MB Ganglinien und Kennzahlen) sind weder im Arbeitsbaum noch in der
> Git-Geschichte. Wer eine alte Zahl braucht, findet sie **nur noch im Protokoll** — oder
> rechnet sie neu. Die einzige lauffähige Basis ist
> [`Referenzlaeufe/2026-09-11_R7_Speicherflotte`](../../../Referenzlaeufe/2026-09-11_R7_Speicherflotte/);
> gegen sie prüfen Gate und CI.

Die Übersicht der Basen mit Datum und Zweck steht — samt der Begründung der Löschung — im
Abschnitt „Entfernte Basen" von
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md); die Tabelle unten ist von
dort übernommen und um die Spalte des gesicherten Protokolls ergänzt.

## Basis → Datum → Zweck → Protokoll

| Ordner | Datum | Zweck | Protokoll |
|---|---|---|---|
| `2026-08-27_V0` | 27.08.2026 | Stand nach den V0-Bestandsfehler-Fixes (Mehrgebäude-Doppelzählung, summierte statt überschriebene Stromprofile) und vor der Dreikanal-Umstellung; neun Projekte, 216 CSV | [`2026-08-27_V0/lauf_protokoll.md`](2026-08-27_V0/lauf_protokoll.md) |
| `2026-08-27_K1` | 27.08.2026 | Basis der Pakete K1 bis S2 nach der Dreikanal-Umstellung (anteilige Netzverlustverteilung F2, Wochenprofile am Klimadaten-Kalender F3); neun Projekte, 216 CSV | [`2026-08-27_K1/lauf_protokoll.md`](2026-08-27_K1/lauf_protokoll.md) |
| `2026-08-27_A1` | 27.08.2026 | erste Basis mit den vier Konzept-11.1-Projekten nach dem Altpfad-Abriss, Meilenstein „ein Rechenweg"; dreizehn Projekte, 329 CSV | [`2026-08-27_A1/lauf_protokoll.md`](2026-08-27_A1/lauf_protokoll.md) |
| `2026-08-27_E1` | 27.08.2026 | Basis des Meilensteins Z3 (Paket E1, Ergebnis je Kanal, Migrationsschritt 52); dreizehn Projekte, 329 CSV | [`2026-08-27_E1/lauf_protokoll.md`](2026-08-27_E1/lauf_protokoll.md) |
| `2026-08-28_P1` | 28.08.2026 | Stand nach Paket P1 (Schichtspeichermodell, Migrationsschritt 53), konstruktiv byte-gleich zu E1; dreizehn Projekte, 329 CSV | [`2026-08-28_P1/lauf_protokoll.md`](2026-08-28_P1/lauf_protokoll.md) |
| `2026-08-28_B2` | 28.08.2026 | Stand nach Paket B2 (Kessel-Temperaturmodus, wählbarer Booster-Lesepunkt, Schema 55) plus Datenänderung an 1042; gemeinsamer Ausgangspunkt beider Basen vom 29.08.2026; dreizehn Projekte, 332 CSV | [`2026-08-28_B2/lauf_protokoll.md`](2026-08-28_B2/lauf_protokoll.md) |
| `2026-08-28_E2` | 28.08.2026 | erste Basis mit scharfer Booster-Temperaturkopplung in Projekt 1042 (Codestand E2/D-Check); dreizehn Projekte, 332 CSV | [`2026-08-28_E2/lauf_protokoll.md`](2026-08-28_E2/lauf_protokoll.md) |
| `2026-08-29_Booster` | 29.08.2026 | Referenz des 13-Projekte-Zweitstands mit erstmals scharfer Booster-Temperaturkopplung (Anlage 14818, `WQ_Unbegrenzt = False`); dreizehn Projekte, 332 CSV | [`2026-08-29_Booster/lauf_protokoll.md`](2026-08-29_Booster/lauf_protokoll.md) |
| `2026-08-29_E1E2` | 29.08.2026 | Referenz des 10-Projekte-Bestands dieses Rechners nach den Emissions-Etappen E1/E2 (CO2-Saat, Emissionsarten-Katalog) samt Umbau von 1039 und Löschung von 1040–1042; zehn Projekte, 234 CSV | [`2026-08-29_E1E2/lauf_protokoll.md`](2026-08-29_E1E2/lauf_protokoll.md) |
| `2026-08-30_B3-Kaskade` | 30.08.2026 | Stand nach Wiederherstellung der 1030-BHKW-Kaskade und Neuaufbau von 1042, Zusammenführung der beiden Datenbestände vom 29.08.2026; dreizehn Projekte, 332 CSV | [`2026-08-30_B3-Kaskade/lauf_protokoll.md`](2026-08-30_B3-Kaskade/lauf_protokoll.md) |
| `2026-09-02_PA0_vor-PaketA` | 02.09.2026 | Ausgangsbasis vor Paket A, einzige Quelle der Ganglinien im UTC-Raster, nach dem Wechsel auf SQLite und dem Projektwechsel (1040–1042 gelöscht, 1026/1028/1029/1043 neu); vierzehn Projekte, 355 CSV | [`2026-09-02_PA0_vor-PaketA/lauf_protokoll.md`](2026-09-02_PA0_vor-PaketA/lauf_protokoll.md) |
| `2026-09-02_PA1_nach-PaketA` | 02.09.2026 | Stand nach Paket A des PV-Ertragsmodell-Konzepts (Zeitbasis UTC → Ortszeit, Stufe E1 „Eine Wahrheit"), byte-gleich zu PB1; vierzehn Projekte, 355 CSV | [`2026-09-02_PA1_nach-PaketA/lauf_protokoll.md`](2026-09-02_PA1_nach-PaketA/lauf_protokoll.md) |
| `2026-09-03_M1_nach-Merge` | 03.09.2026 | Stand nach dem Merge von `origin/ios_migration` (Umzug des Rechenkerns nach `EPOS.Kern`/`EPOS.UI`), byte-gleich zu PB1; vierzehn Projekte, 355 CSV | [`2026-09-03_M1_nach-Merge/lauf_protokoll.md`](2026-09-03_M1_nach-Merge/lauf_protokoll.md) |
| `2026-09-03_M2_nach-Merge2` | 03.09.2026 | Stand nach dem zweiten Merge (Blazor-Dialoge iU9, iOS-Hülle iU10, SQL-Dialekt-Audit, Wirtschaftlichkeitspakete FX2–FX5/B5), byte-gleich zu M1; vierzehn Projekte, 355 CSV | [`2026-09-03_M2_nach-Merge2/lauf_protokoll.md`](2026-09-03_M2_nach-Merge2/lauf_protokoll.md) |
| `2026-09-03_M3_nach-Merge3` | 03.09.2026 | Stand nach dem dritten Merge (iU9 Welle 1: sieben WinForms-Masken der Kosten-/Wirtschaftlichkeitsseite durch Razor-Komponenten ersetzt), byte-gleich zu M2; vierzehn Projekte, 355 CSV | [`2026-09-03_M3_nach-Merge3/lauf_protokoll.md`](2026-09-03_M3_nach-Merge3/lauf_protokoll.md) |
| `2026-09-03_M4_nach-Merge4` | 03.09.2026 | Stand nach dem vierten Merge (iU9 Welle 0: Stilllegung von neun Altmasken), byte-gleich zu M3; vierzehn Projekte, 355 CSV | [`2026-09-03_M4_nach-Merge4/lauf_protokoll.md`](2026-09-03_M4_nach-Merge4/lauf_protokoll.md) |
| `2026-09-03_PB1_nach-PaketB` | 03.09.2026 | Stand nach Paket B (PV-Modellwahl je Anlage: Hay-Davies, Huld-Schwachlichtmodell, Wechselrichter-Teillastkennlinie, Degradation), byte-gleich zu PA1; vierzehn Projekte, 355 CSV | [`2026-09-03_PB1_nach-PaketB/lauf_protokoll.md`](2026-09-03_PB1_nach-PaketB/lauf_protokoll.md) |
| `2026-09-05_M5_nach-Merge5` | 05.09.2026 | Stand nach dem fünften Merge (iU9-Wellen W2–W16c, Umzug auf die Razor-Struktur, zwölf stillgelegte WinForms-Masken), byte-gleich zu M4; vierzehn Projekte, 355 CSV | [`2026-09-05_M5_nach-Merge5/lauf_protokoll.md`](2026-09-05_M5_nach-Merge5/lauf_protokoll.md) |
| `2026-09-05_R2_Zeitbasis` | 05.09.2026 | plattformfreie CI-Basis nach Zusammenführung der Rechner-2-Linie (Paket A: Solar-Zeitbasis UTC → Ortszeit); elf Projekte, 282 CSV | [`2026-09-05_R2_Zeitbasis/protokoll.txt`](2026-09-05_R2_Zeitbasis/protokoll.txt) |
| `2026-09-06_R3_Straenge` | 06.09.2026 | CI-Basis mit dem ersten Strang-Projekt 1045 „Prüfprojekt Ost/West Stränge" (Wechselrichterkonzept, Vorrangregel Kapitel 3.5); zwölf Projekte, 312 CSV | [`2026-09-06_R3_Straenge/protokoll.txt`](2026-09-06_R3_Straenge/protokoll.txt) |
| `2026-09-07_M7_nach-Merge7` | 07.09.2026 | Stand nach dem siebten Merge (PV-Strangtabelle W6‑B‑4, Schemaschritt 69 samt Linux-Basis R6) — die Windows-Reihen-Basis „Aktuelle Basis" bis zur Ablösung; vierzehn Projekte, 355 CSV | [`2026-09-07_M7_nach-Merge7/lauf_protokoll.md`](2026-09-07_M7_nach-Merge7/lauf_protokoll.md), dazu [`vergleich_M5_zu_M7.txt`](2026-09-07_M7_nach-Merge7/vergleich_M5_zu_M7.txt) |
| `2026-09-07_R4_Double` | 07.09.2026 | CI-Basis nach Anwenderentscheid W8‑O‑5d („alles in double" statt `float`); zwölf Projekte, 312 CSV | [`2026-09-07_R4_Double/protokoll.txt`](2026-09-07_R4_Double/protokoll.txt) |
| `2026-09-07_R5_Zahlenrand` | 07.09.2026 | CI-Basis nach den Entscheiden W8‑O‑5d‑Q1 (Zahlenrand), W8‑O‑5d‑Q2 (keine `int`-Abschneidung im BHKW-Plan-Port) und Em‑9.8 (zehn Emissionsskalare); zwölf Projekte, 312 CSV, 1 792 Skalare | [`2026-09-07_R5_Zahlenrand/protokoll.txt`](2026-09-07_R5_Zahlenrand/protokoll.txt) |
| `2026-09-07_R6_PvKoeffizienten` | 07.09.2026 | CI-Basis nach Befund W6‑B‑5 (Reparatur der PV-Modulkoeffizienten aus der CEC-Liste, Schemaschritt 69); zwölf Projekte, 312 CSV, 1 792 Skalare — abgelöst durch R7 am 11.09.2026 | [`2026-09-07_R6_PvKoeffizienten/protokoll.txt`](2026-09-07_R6_PvKoeffizienten/protokoll.txt) |

## Was in diesen Protokollen steht — und was nicht

- **Steht darin:** der Codestand, der Schemastand, die feste Projektliste, der Selbstvergleich,
  der Vergleich gegen die Vorgängerbasis Datei für Datei, jede gewollte Abweichung mit ihrer
  Ursache und die Gegenprobe, die sie belegt. Die drei Einfrierregeln der Testdatenbank
  (Em‑9.8‑Q4 Emissionsfaktoren, W6‑B‑5‑Q3 PV-Modulkoeffizienten, SP‑O‑8 Flottenparameter)
  stützen sich auf die Herleitungen in `2026-09-07_R5_Zahlenrand/protokoll.txt` und
  `2026-09-07_R6_PvKoeffizienten/protokoll.txt`.
- **Steht nicht darin:** die Ganglinien und Kennzahlen selbst. Ein Vergleich gegen eine dieser
  Basen ist nicht mehr möglich.

**Diese Protokolle sind Geschichte, keine Regelquelle.** Sie erklären, wie die heutige Basis
geworden ist; was gilt, steht in [`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md)
und in der Wurzel-[`CLAUDE.md`](../../../CLAUDE.md). Die sechs Verweise, die aus fünf dieser
Protokolle auf ihren damaligen Ablageort zeigen (viermal
`WindowsFormsApplication1/Allgemein/Simulation/…`, zweimal ein Konzept in der
Repository-Wurzel), sind **bewusst nicht nachgezogen** — ein Protokoll darf nennen, worauf es
sich damals bezogen hat. Einer davon trifft nach dem Umzug #241 zufällig wieder; die anderen
fünf führt die Wache `EPOS.Kern.Tests/DokumentationLinkWacheTests` als vorbestehende Lücken.
