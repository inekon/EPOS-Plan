# Z5 — Zapfprofilgenerator: Kalibrierung und Validierung (Protokoll, 25.09.2026)

Statuszeile #495 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Zeile Z5
in Kapitel 7 und der Nachtrag N15 im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md); Abschnitt 13
der [Übergabe](../../../aktuell/Zapfprofilgenerator/2026-09-23_Uebergabe_Zapfprofilgenerator.md);
Vorstufe im Protokoll [Z4b](2026-09-24_Z4b_Typtage.md). Zweig `z5` von `d6020c05`, 37
eigene Commits bis `a87b783b`, Papiere `b28b133f`, Merge `a0bbc633`, gepusht `a0bbc633`, Kern-Lauf 36099321791 grün; Merges von `origin` (`45935351` in `07cff70b`, `ac8a1762` in `4c692ff8`,
`6c5aa139` in `c82884d4`). Alle Gates im Worktree, ohne CI-Lauf bis zum Push.

## Auftrag

Stufe Z5 nach Kapitel 7: Messdatenimport, Vergleichsbericht, Validierung gegen freie Messreihen
(INEKON-Reihen lagen nicht vor, K5), Kalibrierung der Nichtwohn-Parameter, Katalogausbau; Abnahme
nach Kapitel 7 (Messspitze im P85–P95-Band der synthetischen Dauerlinie, √N-Skalierung,
Formabgleich mit Schwelle, Energie nach Kalibrierung exakt). Drei Gruppen (Kern · Katalog ·
Oberfläche) durch Agenten mit `model: opus` im Worktree `z5`, je Gruppe eine Gegenprüfung und eine
Nachbesserung; ohne Push und ohne CI-Lauf bis zum Abschluss. Der Anwender bestellte die Stufe
unmittelbar nach Z4b („dont stop").

## Gruppen und Commits

| Gruppe | Inhalt | Commits |
|---|---|---|
| 1 | Schritt T4 `Tab_TwwMessreihe` (zunächst 132, dann 135), `Messreihenleser`, `TwwMessreihenCtrl`, `Messvergleich` (fünf Kennzahlen), `Messkalibrierung` mit `Nichtwohnparameter`, fünf Parameter im Paketteil, Wachen nachgezogen | `7e2d6d2f`, `391abda5`, `9c7b33b0`, `96abc961`, `60add862`, `8f74e298`, `c6756421`, `c256d23d` |
| 2 | Katalog: Ein- und Zweifamilienhaus (ZU19), Nichtwohn-Vorgabesatz mit Steuerspalte `Gruppe`, `TwwNutzungsartCtrl.VorschlagUebernehmen` | `21865489`, `74b49e0f`, `67f40e57` |
| 1 Nachbesserung | Merge `45935351` mit Schritt → 140; Band aus der Dauerlinie, `Spitzenstreuung`; Sommerzeit (Ortszeit/Normalzeit); Wache „Schrittnummern lückenlos"; Teiljahr, kleinste Quadrate, Hochrechnung mit Jahresgang, Feiertage; Nullläufe; vorbereitetes Kommando und Reader | `07cff70b`, `95ebb3e0`, `87ec36b2`, `ffa096ce`, `8ab29bba`, `5546af26`, `68fdf76b` |
| 3 (mit Nachbesserung 2) | Katalogimport je Gruppe, Provenienz „Modellannahme", Kalibrierquelle 80 Zeichen, Formprobe; Messdaten-Dialog mit Hülle und KiSicht; Vergleichsbericht im Reiter Kennzahlen (nebenläufig), Kalibrierknöpfe, Validierungshinweise; Tests; Wiki `#messdaten`/`#vergleich` | `805b0cc8`, `9ea7e2bf`, `1ea6c135`, `2eb146af`, `2529b9ff`, `301981f7`, `d1bcd8e1` |
| 3 Nachbesserung | Kalibrierung aus der Messreihe nebenläufig und nur bei Teiljahr gerechnet; Messdaten-Prüfung nebenläufig, nur bei Größe, Schwelle, Zeitstempel oder Datei; Wahlknopf-Text; Vorschlagshinweise in der Warnliste; Kreuz und Esc der Überlagerungen lesen neu; mengengewichtete Spreizung aller Zonen; Kalibrierquelle nach Textelementen; Pflege (Ordner merken, Doc-Verweise, en-Anführungszeichen, Kalibriersperre nennt Stufe, Paket ohne Vorgabesatz benannt) | `67ad20ab`, `30a3f4b4`, `d14d04c8`, `063917ec`, `1e782975`, `16acb9ab`, `844db9cd`, `c9a7e0ed` |
| Abschluss | Merge `origin` (6c5aa139), Testdatenbank, Papiere (N15, Protokoll, Statuszeile, Übergabe, Wiki-Quelle), Gate | `4c692ff8`, `0bfc0bd1`, `c82884d4`, `a87b783b`; Papiere im Folgecommit |

## Gates

| Stand | Kern-Build | Tests | Weiteres |
|---|---|---|---|
| Gruppe 1 (`c256d23d`) | 0 Fehler | 13 350 grün (1 übersprungen) | SqlDialektPruefer 1 842/0; Vorlage 31/31; ChartProben 165; Windows-Schale 0 Fehler; Referenzlauf 5/5 byte-gleich gegen R14 |
| Gruppe 2 (`67f40e57`) | 0 Fehler | 13 354 grün | SqlDialektPruefer 1 847/0; Vorlage 31/31; Windows-Schale 0 Fehler; Referenzlauf 5/5 byte-gleich |
| Nachbesserung 1 (`68fdf76b`) | 0 Fehler | 13 707 grün (1 übersprungen): Kern 6 624, UI 6 121, KiKern 549, Engine 386, Planung 27 | SqlDialektPruefer 1 905/0; Vorlage 32/32 (142 STRICT); ChartProben 170; Windows-Schale 0 Fehler; Referenzlauf 5/5, 160/160 CSV byte-gleich |
| Gruppe 3 (`d1bcd8e1`) | 0 Fehler | 13 749 grün (2 übersprungen) | SqlDialektPruefer 1 905/0; Vorlage 32; ChartProben 170; Windows-Schale 0 Fehler; Referenzlauf 5/5 |
| Abschluss (a87b783b) | 0 Fehler | 13 938 grün (2 übersprungen): Kern 6 778, UI 6 197, KiKern 549, Engine 386, Planung 28 | SqlDialektPruefer 1 920/0; Vorlage 34/34 (144 STRICT); ChartProben 174; Windows-Schale 0 Fehler; Referenzlauf 5/5 gegen R14 (1 805 429 Werte) |

## Merge und Nachzug

- Schrittnummer: Gruppe 1 maß 132 als frei; bis zur Abnahme belegte die Gebäudesimulation (G3 Welle B)
  132–134, Gruppe 1 wich auf 135 aus; bis zur Nachbesserung belegte die Anlagenkopplung (W4) 135–137, bis zum
  Abschluss die Importzuordnung (G4c W3) 138 und das Baujahr (G4a W3) 139;
  T4 steht seither als **140** symbolisch (`BaujahrSchema.SCHRITT + 1`) mit einer
  Wache gegen Lücken. Regel: wer zuerst pusht, hat die Nummer.
- `07cff70b`: Merge `45935351` nach `z5` — sieben Konflikte (SchemaStand, TwwSchema, SchemaMigration,
  Testdatenbankschema, beide resx, Vorlagenprobe, Testdatenbank, LIESMICH), beide Seiten; Testdatenbank
  aus der `origin`-Fassung auf 140 (oid `6bf9bfbc…`, 142 STRICT).
- `4c692ff8`: Merge `ac8a1762` nach `z5` — sieben Konflikte, beide Seiten; T4 auf 139, Testdatenbank
  `0bfc0bd1` (oid `cc21e971…`, 144 STRICT).
- `c82884d4`: Merge `6c5aa139` nach `z5` vor dem Gate, vier Konflikte (SchemaStand, SchemaMigration, LIESMICH der Referenzläufe, Testdatenbank), beide Seiten; T4 auf
  140 mit Testdatenbank `a87b783b` (oid `5de448e8…`, 144 STRICT), die verschluckte
  Methodengrenze wieder eingezogen.
- `a0bbc633`: Merge `b5a2e389` (G3 Wellen D1 und C) nach `z5`, weil der Push abgewiesen war; Gate wiederholt:
  Kern-Filter 0 Fehler, 14 014 Tests grün, Windows-Schale 0 Fehler, SqlDialektPruefer 1 921/0, Referenzlauf 5/5 gegen R14 (Merge berührt die Gebäudesimulation), ChartProben entbehrlich.
- Statusnummer: #494 war mit den Nachbarsitzungen abgestimmt, wurde aber vor dem Push von der
  Berichtsvorlagen-Sitzung belegt; Z5 trägt #495.

## Gegenprüfungen

- **Gruppe 1** (zehn Befunde, zwei hohe): das P85–P95-Band aus Realisierungsspitzen statt aus der
  Dauerlinie (Leitkennzahl unerfüllbar, Test verdeckte es) — behoben; Ablehnung der Herbstumstellung
  — behoben; Schrittliste mit Lücke — Merge und Wache; Teiljahr, Ausgleichsrechnung, Hochrechnung,
  Lückenzahl, Ablage, Feiertage, Kleinigkeiten — behoben. Bestätigt: DDL, Leser-Formate, Controller,
  Kalibrierung exakt, K5, Formales.
- **Gruppe 2** (zehn Befunde, ein mittlerer): Katalogimport ohne Gruppenbindung — behoben (Gruppe 3);
  Provenienztexte, Skriptkopf, Doc-Kommentare, Formprobe, Quellenlänge — behoben; „25–27 Typen"
  als Anwenderfrage ZU24. Bestätigt: Ableitung byte-gleich, Testdatenbank-Zählungen, Gruppenregel,
  Auslieferung, `VorschlagUebernehmen`, Formales.
- **Gruppe 3**: (vierzehn Befunde, ein hoher): „Aus Messreihe kalibrieren“ rechnete die Jahresreihe synchron im
  Blazor-Verteiler — behoben (nur bei Teiljahr, nebenläufig mit Fortschritt und Abbruch); Prüfung je
  Tastendruck, Wahlknopf mit Spaltenüberschrift, Vorschlagshinweise ohne Warnliste, Kreuz ohne Neulesen
  (auch Typtage), Spreizung nur der ersten Zone, Kalibrierquelle mit Ersatzzeichenpaaren, Pflege — behoben.
  Bestätigt: Dialogaufbau, KiSicht, Vergleichsbericht nur mit Verhältnissen, Wiki-Quelle.

## Abweichungen vom Papier

Stehen im Nachtrag N15: Band als Quantil der Dauerlinie (Deutung von 3.6/Lehre 1), Spitzenstreuung
getrennt; Messreihen als Projektdaten mit eigener Tabelle; Sommerzeitregel; Hochrechnung mit
Jahresgang; kleinste Quadrate als Vorschlag; Katalogziel 25–27 nicht aus VDI 6002 erreichbar (ZU24);
Steuerspalte `Gruppe` im Paketteil; kein Vergleichsbild (5.6); Spitzenstreuung in der Oberfläche unbestimmt; Vergleich nur ab Erweitert.

## Offene Punkte

- Sichtabnahme unter Windows (Übergabe, Abschnitt 13) — auch die der Stufen Z1–Z4b.
- ZU24 (Katalogtypen aus A100), K5 (INEKON-Messreihen), ZU7 (Referenzprojekt auf dem Generator mit
  neuer Basis), ZU20–ZU22, K8/ZU15.
- Redundanter Index auf `Tab_TwwMessreihe.ID_Projekt`; Bedeckungsgrad-Referenzfall (TRY-Import).
- Wiki-Upload und Logbuch-Sätze mit Versionsnummer.
