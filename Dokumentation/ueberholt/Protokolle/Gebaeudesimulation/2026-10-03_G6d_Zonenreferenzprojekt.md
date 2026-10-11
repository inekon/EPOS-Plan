# Protokoll G6d — Referenzprojekt 1052 mit Zonen (03.10.2026)

**Auftrag.** Stufe G6d der Gebäudesimulation: ein Referenzprojekt mit mehreren Zonen in der Testdatenbank,
gesät über die Programmwege (Mehrzonenkonzept 9). Entschieden ist die Reihenfolge **D2 → E59 → R5 → G6d →
RP1 → RP2**; G6d friert nicht ein, die Basis **R34** friert RP2. Die Basis R33 bleibt unverändert. Kein
Schemaschritt; die Testdatenbank steht auf Fassung 175. Nicht in G6d: Projekt 1051 (RP1), die Einfrierregel
„gesäte Zonendaten“ in der Wurzel-`CLAUDE.md` und die CI-Auswahl (RP2).

Dieses Protokoll gilt für die Welle G6d als Ganzes; **Stand: umgesetzt, Gate im Hauptbaum siehe Abschnitt 7.**

## 1 Commits

Zweig `g6d`, ab `f5c8bb42`:

| Commit | Inhalt |
|---|---|
| `f4ad8fa12` | Saatskript `referenzprojekt_1052_zonen.cs` samt Bauplan `referenzprojekt_1052_bauplan.cs` |
| `1fd9dbdc7` | Testdatenbank: 1052 gesät (Schemastand 173, durch den Merge überholt) |
| `0358ab1e3` | Skript läuft unter Kultur de-DE; Schlüsselspalten im Abdruck nach Namensteil |
| `bd9589a83` | Wache `ZonenReferenzprojektWacheTests`, Bauplan im Testprojekt verlinkt |
| `2873fe719` | N-AH8 mit Zonen und N-AH9 rechnen 1052 aus der Datenbank |
| `4b4b54076` | Zählnachzüge in 16 Testklassen |
| `89019ebf3` | Kopfkommentar zu M11 berichtigt |
| `9a43ea1ab` | `Referenzlaeufe/LIESMICH.md` und Mehrzonenkonzept Abschnitt 9 (G6d-Zeile) |
| `4d964a02b` | Merge `ios_migration_september` (Schemaschritt 175, Testdatenbank von dort, LIESMICH-Konflikt zusammengeführt, 0 Konfliktmarker) |
| `2fd619ed5` | Testdatenbank: 1052 mit Zonen gesät (Schemastand 175), 82 329 600 Byte, LFS-OID `d33766677c…`, `integrity_check` ok, `foreign_key_check` leer |
| `f9af6d957` | LIESMICH-Nachtrag auf Fassung 175 |

Nachzug „Stammverweis einer Variante beim Duplizieren nicht versetzt“ (Befund 1): ``2a207ce7e``.

## 2 Zonenschnitt

Kopie 1018 → 1052 über `ProjektDuplizierenCtrl` (auf Schema 175 kommen die 25 Projektbrennstoffe von 1018
mit). Hülle „Gebäude als eine Zone übernehmen“ (`GebaeudeZonenCtrl.Uebernahme`), Faktor 500 / 1 975,34 =
0,25312; danach schreibt der OK-Weg des Zonendialogs (`GebaeudeZonenCtrl.Schreiben`) drei Zonen:

| Zone | Beheizung | Fläche | Bauteile | Besonderheiten |
|---|---|---|---|---|
| Gästezimmer | beheizt | 60 % = 300 m² | 15 | 60 % jeder Außenfläche, ψ·L anteilig; Kellerdecke 318,93 m², U 0,55, zur Zone Keller; Trennwand 30 m², U 0,6, zur Gastronomie (Gruppe nach der 4-K-Regel) |
| Gastronomie und Verwaltung | beheizt | 40 % = 200 m² | 14 | 40 % derselben Außenflächen; Kellerdecke 212,62 m² |
| Keller | unbeheizt | 531,55 m² | 2 | Boden 531,55 m² und Wände 230,6 m² (Umfang des flächengleichen Quadrats × 2,5 m), beide am Erdreich, U 0,55; innere Gewinne 0, Bewohner 0, Infiltration 0,2 1/h, Nutzerluftwechsel 0 |

Luftstrom 150 m³/h zwischen Gästezimmern und Gastronomie.

## 3 Gesäte Zellen

- **Heizkalender** je beheizter Zone über `KonditionierungsvorlageCtrl.Uebernehmen`: Gästezimmer Vorlage
  „Wohnen“, 20 °C, 18 °C von 22 bis 6 Uhr; Gastronomie Vorlage „Büro“, 20 °C, 16 °C von 18 bis 7 Uhr,
  Wochenende und neun Feiertage 16 °C; Sprünge um 6 bzw. 7 Uhr.
- **Projekteinstellung:** Aufheizoptimierung an über `KonfigurationCtrl.AufheizvorgabeSetzen`, Bemessung (a),
  ρ 0,2 ausdrücklich, täglich, kein Aufschlag; keine manuelle Aufheizzeit am Gebäude.
- **Direkt geschrieben** werden nur die Kopfzellen in `Tab_Projekt`.

## 4 Skriptverhalten

- Wiederholbar: `--trocken`, scharf, zweiter Lauf „nichts zu tun“, Rückgabe 0.
- Schreibt in eine Arbeitsdatei und prüft dort Vorlage, Zielzellen, Unversehrtheit von 1018 (Abdruck samt
  Schlüsseln) und der Konditionierungsvorlagen sowie `integrity_check`/`foreign_key_check`; erst dann ersetzt
  es die Datenbank.
- Bricht bei liegenden `-wal`/`-shm` ab.
- Zwei Läufe ergeben inhaltsgleiche Tabellen (2 Byte im freien Seitenraum verschieden).

## 5 Wache und Tests

**`ZonenReferenzprojektWacheTests`, 6 Fälle grün:**

1. Genau 1052 trägt Zonen, Trennflächen, Luftströme und Zonenkalender.
2. Jede gesäte Zelle wie im Bauplan, samt Kennzahlen des Schnitts.
3. Dieselben Programmwege auf einer Arbeitskopie ergeben einen bitgleichen Abdruck (9 321 Zeilen).
4. Die Kalender springen bei 6/22 bzw. 7/18 Uhr.
5. Die Zonenschleife rechnet 1052 in zwei Läufen bitgleich, mit einem Aufheizplan je Zone.
6. Ein Lauf schreibt drei Zeilen nach `Tab_ErgebnisZone`.

**KP3-Zonentests:** N-AH8 mit Zonen rechnet jetzt 1 Mehrzonengebäude aus der Datenbank (Pflicht, sobald 1052
steht). Neu: N-AH9 an 1052 bei konstant −15 °C, 562 Rampenfenster im Band 1,01·P_auf, größtes 100,37 %.

**Zählnachzüge** (16 Testklassen): Der neue Helfer `Zonenbestand` zählt ohne die Zeilen von 1052; „Alles aus“
gilt außer für 1052.

| Zählwert | Neu |
|---|---|
| Projektgebäude / Verweis-Nachtrag | 31 / 27 |
| Baualtersklasse | H4 |
| Kessel (aus Katalog / gesetzt) | 23 / 24 |
| Preisbasis „Nm³“ | 22 |
| Investitionspositionen | 146 / 139, 36 / 43 / 11 |

## 6 Zahlen des Laufs 1052

Zwei Läufe byte-gleich, Vergleich PASS; auf Stand 175 gleich wie auf 173; rund 2 s.

| Größe | Gebäude | Gästezimmer | Gastronomie |
|---|---|---|---|
| Heizwärme | 57,10 MWh (1018: 68,25) | 37,21 MWh | 19,89 MWh |
| Spitze | 30,10 kW | 18,40 kW | 12,51 kW |
| Rampentage | 217 | 52 | 201 |
| t_auf,max | 15 h | 6 h | 15 h |
| Aufheizstunden | 672 | 80 | 645 |

Keller im Mittel der Heizzeit 13,3 °C. `heizsollwert_0.csv` mit 672 Rampenstunden, Sollwert
flächengewichtet 17,2 bis 20 °C. Zonenwerte stehen in `aggregate.csv`; eine eigene Zonen-CSV erzeugt
`EPOS.Referenzlauf` nicht.

## 7 Gates

**Gate 1** (Stand 173, vor den Zählnachzügen): 29 Kern-Tests rot — Anlass für die Zählnachzüge.

**Gate 2** (`9a43ea1ab`, Stand 173):

| Prüfung | Ergebnis |
|---|---|
| Kern-Filter | 0 Fehler |
| ChartProben | JA (200 Hashes) |
| KiKern | 549 grün |
| SpeicherEngine | 397 grün |
| SpeicherPlanung | 27 grün, 1 übersprungen |
| EPOS.UI | 7 334 grün |
| EPOS.Kern | 10 386 grün, 1 rot (Befund 1) |
| Dokumentationswachen | 35 grün |
| Referenzlauf | 16/16 PASS, 487/487 CSV byte-gleich gegen R33; gestörter Lauf PASS |

**Nach dem Merge auf 175** (kein drittes volles Gate): Kern-Filter 0 Fehler; gezielter Lauf über 24 Klassen
(310 Tests) 309 grün, 1 rot (dieselbe, Befund 1); Referenzlauf 16 Projekte gegen R33 PASS, 487/487
byte-gleich; Auslieferungsvorlage.Tests 47/47 grün (die Vorlage liefert 1052 nicht aus, sie löscht alle
Projektdaten, `Tab_Zone` dort 0); SqlDialektPruefer 0 Fundstellen; Windows-Schale 0 Fehler.

**Nachweis im Hauptbaum:** Merge ``2a207ce7e` (Fast-Forward)`; Gate `Kern-Filter 0 Fehler, ChartProben JA (200), Kern 10 469 (1 übersprungen), UI 7 367, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R33**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer ohne Abweichung, Markdown ohne BOM, keine Konfliktmarker (auf `3ee6cf57d`, Gate 690; ein erster Durchlauf brach an der vollen Platte ab)`.

## 8 Befunde

1. **Kopieren einer Variante versetzt ihren Stamm.** Beim Duplizieren wird `Tab_Variante.ID_ProjektRef` um
   denselben Abstand verschoben wie die Projekt-ID. Beispiel 1044 (Stamm 1042): die Kopie fällt auf 1053,
   ihr Stamm würde 1051; fehlt 1051, scheitert die Kopie mit „FOREIGN KEY constraint failed“, sonst zeigt
   sie still auf ein falsches Projekt. Derselbe Fehler steckt in der Testdatenbank: 1050 steht als „Variante
   Test1 von 1046“ statt von 1019. Behebung: Stammverweis beim Kopieren nicht versetzen (Festlegung der
   Orchestrierung, dem Anwender vorgelegt), Nachzug ``2a207ce7e``; die Zelle von 1050 wird in RP2 behandelt.
   Der Test `KostenProjektPositionenCtrlTests.Eine_Kopie_erbt_keinen_Geraeteanker_ohne_Anlage` bleibt bis
   zum Nachzug rot.
2. **Herkunftstext in Anzeigesprache.** `Tab_Konditionierungskalender.Bemerkung` speichert die Herkunft in
   der Sprache der Oberfläche („aus Vorlage Wohnen“ bzw. „from template Wohnen“); Skript und Nachbau laufen
   deshalb unter de-DE.
3. **Zeitstempel durch Trigger.** Der Kostenstempel-Trigger stempelt beim Kopieren die Uhrzeit in
   `Tab_Projekt.Kosten_Geaendert`; das Skript setzt NULL wie bei allen übrigen Projekten.
4. **N-AH9 an 1052 nach langer Absenkung.** Nach langer Absenkung (Wochenende 61 h, Feiertag 37 h,
   Weihnachten 109 h) liegt die Gastronomie bis 2,1 % über P_auf; vermutete Ursache: Die Planung rechnet den
   Keller fest beim Startwert, im Lauf kühlt er weiter aus. Bei −5 °C und wärmer füllen die Rampen der
   Gästezimmer die ganze Absenkung von 8 h (W2) und liegen 5–24 % über P_auf, darum rechnet der Fall bei
   −15 °C. Messlatte: lang ≤ 1,03·P_auf, begrenzt ≤ 1,06·P_auf.
5. **Leere `-wal`/`-shm` neben der Testdatenbank.** Kern-Tests (vermutlich Repo-Lesewege, etwa
   `AufheizSchemaTests`) und das Gate hinterlassen sie; ein folgender Lauf des Saatskripts bricht dann ab.

## 9 Festlegungen

- **Platzhalter für 1051:** Fehlt 1051, hält eine leere Zeile `Tab_Projekt.ID = 1051` die Nummer nur für die
  Dauer der Kopie frei und wird danach gelöscht; steht 1051, fällt die Kopie von selbst auf 1052.
- Der Bauplan ist eine eigene Datei, die das Skript per `#:include` zieht und die Wache verlinkt.
- Gewählte Werte: Anteile 60/40, Kellerdecke mit dem U-Wert der Bodenplatte, Trennwand 30 m² mit U 0,6,
  Keller mit expliziten Nullgewinnen, ρ 0,2 ausdrücklich, Kultur de-DE.
- Wiki: keine Änderung.

## 10 Offen

- **RP1:** Reihenfolge der Saat zuerst 1051, dann 1052 (eine Kopie für RP1 fiele bei stehendem 1052 auf
  1053); Zählnachzüge für 1051 wie hier; ρ_min-Messung samt 1052; vor RP2 entscheiden, ob die Abweichung
  in N-AH9 nach langer Absenkung als Grenze der Planung hingenommen wird.
- **RP2:** Regel „gesäte Zonendaten“ in die Wurzel-`CLAUDE.md` (Regeltext steht in der LIESMICH), Basis R34
  mit 18 Projekten, CI-Auswahl (1052 rechnet rund 2 s), Frage M11 und Statusdatei nachziehen; Zelle von
  1050 (Befund 1).
- **SA:** 1052 im Zonendialog öffnen (drei Zonen, Trennwand, Luftstrom, Kalenderkarten „Wohnen“ und „Büro“).
- **Befund 5:** `-wal`/`-shm` vor einem Lauf des Saatskripts entfernen.

Dateien: `Referenzlaeufe/Skripte/referenzprojekt_1052_zonen.cs`,
`Referenzlaeufe/Skripte/referenzprojekt_1052_bauplan.cs`,
`EPOS.Kern.Tests/ZonenReferenzprojektWacheTests.cs`, `Referenzlaeufe/LIESMICH.md`.
