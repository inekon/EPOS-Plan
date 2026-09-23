# Protokoll: Schlusswelle KU1 der Kühlung (23.09.2026)

**Auftrag** (vom Anwender am 23.09.2026 beauftragt): vierte und letzte Welle der Stufe KU1 — Entscheid
E32 (Gebäude ohne wirksame Kühlung laufen frei), ein Referenzprojekt mit Kühlung samt Einfrierregel
„gesäte Kältedaten", die Kühldecke als eigener Knoten für Normfall 11 (nur, wenn es sauber gelingt)
und das Einfrieren einer neuen Basis. Maßgeblich:
[Kühlkonzept](../../../aktuell/Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 10.4, 10.5 und 11.1,
[Konzept Gebäudesimulation](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.37,
[Rechenschritte](../../../aktuell/Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 7.1, 8.2 und 10.3.

## 1 Umsetzung

| Teil | Stand |
|---|---|
| E32 im Rechenkern | `GebaeudeModellEingang` setzt die obere Regelgrenze ohne wirksame Kühlung auf +∞ (`Stundenrand` ohne Kühlung, `MitKappung` heißt `MitKuehlung`); das Gebäude läuft frei. `GebaeudeModellErgebnis` führt Kühlreihe, `KuehlenergieMwh` und `StundenMitKuehlbedarf` nur bei wirksamer Kühlung, sonst `null`; kühlt der Löser ohne wirksame Kühlung, ist das ein benannter Fehler (`ErgebnisUnplausibel`). Die Überhitzungsstunden zählen weiter gegen `Maximaleraumtemperatur`. `GebaeudeErgebnisexport` schreibt `kuehlbedarf_<n>.csv` und die zwei Kühlskalare nur mit Kühlreihe |
| E32 an der Oberfläche | Bedarfsdialog: Abschnitt „Kältebedarf" für jedes VDI-Gebäude, ohne Kühlung mit „—", ohne Kältebild und Kühlspalte, mit den Überhitzungsstunden und dem Satz, dass das Gebäude frei läuft; Gebäudedialog, Hilfe-Assistent und Herleitung in beiden Sprachen ohne „informativ"; Bericht „—" |
| E32 in den Papieren | Konzept Kopf, 4.5, 4.6 und N1.37; Status E32; Kühlkonzept Kopf, 3.1, 3.2, 3.4, 3.7, 4.7, 6.4, 7.1, 8.1, 8.4; Rechenschritte Kopf, 7.1, 7.3, 8.1, 8.2, 9; Umsetzungskonzept 1.4; Register F-S4; Wiki-Quellen „Kühlung" und „Gebäudemodell VDI 6007" (Gegenlese-Regex ohne Treffer, nicht hochgeladen) |
| Referenzprojekt 1017 | vier Zellen über `Referenzlaeufe/Skripte/kuehlung_1017_referenzprojekt.py` (bricht bei unerwarteten Werten vor dem Schreiben ab, zweiter Lauf ohne Änderung): `Tab_Einstellungen.Kuehlbetrieb` 0 → 1, Gebäude 10599 `Kuehlung_Aktiv` 0 → 1, `Kuehl_Sollwert` NULL → 24,0 °C, `Kuehlleistung_Max` NULL → 15,0 kW; Sicherung vorher außerhalb des Repositoriums; Zellvergleich aller 130 Tabellen (10 496 533 Zellen) genau diese vier Zellen, `integrity_check` ok, `foreign_key_check` leer, Schemastand 113, 67 751 936 Byte, LFS-Zeiger geprüft (SHA-256 `715759e7…`; nach dem Zusammenführen mit der Katalog-Generation 9 aus Auftrag #452 `769143e4…`, 67 751 936 Byte, gegen die Fassung von origin genau diese vier Zellen) |
| Einfrierregel „gesäte Kältedaten" | in `Referenzlaeufe/LIESMICH.md` und im Abschnitt „Regressionsnetz" der `CLAUDE.md` |
| Kühldecke (Normfall 11) | geprüft und verworfen (Abschnitt 4); Fall 11 bleibt dokumentierte Abweichung |
| Neue Basis | `Referenzlaeufe/2026-09-23_R13_Kuehlung`, 387 CSV, 2 207 Skalare; R12 mit Protokoll nach `Dokumentation/ueberholt/Referenzbasen/`; Basisname in `CLAUDE.md`, `kern.yml`, `ios.yml`, Systementwurf B14, Konzept Q14, Konzept Wirtschaftlichkeit |
| Tests nachgezogen | freier Lauf und Parität (unerreichbarer Kühlsollwert), Export ohne Kühlreihe, Bedarfsdialog ohne Kühlnullen, gekühlte Proben mit Kühlsollwert = θ_max (Energiebilanz, Kennzahlen, Skalierung, Sommerlüftung), F-K4 in zwei Läufen; die Schemaproben erwarten die gesäten Kältedaten von 1017 |

**Vor dem Einfrieren `git fetch origin`**, zweimal. Beim ersten Mal hatte origin die Testdatenbank
(Zapfprofil-Testkatalog Z2), `Referenzlaeufe/LIESMICH.md` und die Ressourcen geändert: angehalten,
Testdatenbank nicht angefasst, der Orchestrator hat zusammengeführt. Beim zweiten Mal, unmittelbar vor
dem Einfrieren, kein neuer Commit. Das Skript ist erst auf die zusammengeführte Testdatenbank
angewandt worden.

## 2 Wirkung von E32 (gemessen vor dem Einfrieren)

Referenzlauf aller dreizehn Projekte gegen die Basis R12, Testdatenbank unverändert:

| Gebäude (Projekte) | Heizwärme R12 → E32 [MWh/a] | relativ | Überhitzungsstunden R12 → E32 [h] | höchste Raumluft E32 [°C] |
|---|---:|---:|---:|---:|
| 10614, 10577, 10652 (1007, 1008, 1046) | 69,07 → 68,97; 15,03 → 15,01 | −0,145 % | 469 → 600 | 33,2 |
| 10576 (1008) | 89,17 → 89,13 | −0,044 % | 295 → 440 | 30,4 |
| 10599 (1017) | 90,19 → 90,15 | −0,045 % | 305 → 455 | 30,3 |
| 10632 (1018) | 68,27 → 68,25 | −0,032 % | 154 → 233 | 29,1 |
| 10628, 10629, 10644 (1023, 1024, 1039) | 450,56 → 449,90 | −0,145 % | 628 → 840 | 33,4 |
| 10642 (1039) | 48,02 → 48,02 | −0,009 % | 72 → 102 | 27,5 |
| 10643 (1039) | 99,14 → 99,10 | −0,041 % | 264 → 308 | 32,2 |
| 10646, 10647, 10651 (1041, 1042, 1045) | 75,98 → 75,94 | −0,050 % | 277 → 331 | 32,0 |

Die Heizwärme sinkt um höchstens 0,15 % — die Grenze des Auftrags (3 %) ist weit unterschritten; die
Wärmelast bleibt gleich. Die Überhitzungsstunden steigen um 17 bis 51 %. 1030 und 1040 bleiben
byte-gleich. Die Normfälle gehen nicht über den Eingangsbauer und sind unberührt.

## 3 Das Referenzprojekt mit Kühlung

**Wahl:** 1017 — Einzelgebäude auf dem VDI-Weg (Gebäude 10599 „GMH-D-S-118", 744,4 m², Skalierung
744/744,4), genau eine Wärmepumpe (sie bekommt mit KU2 die Kühlfunktion), PV und Stromspeicher, eines
der fünf CI-Projekte. Nicht 1040 (Tagesbilanz-Weg, A15), 1045 (die Kühltests schalten die Kühlung an
dessen Gebäude auf Arbeitskopien), 1046 (Flottenstand eingefroren), 1007 (Gebäude wie 1046, Skalierung
4,6). **Werte:** Kühlsollwert 24 °C = Maximaleraumtemperatur des Gebäudes (Prüfregel +1 K über dem
höchsten Heizsollwert 20 °C erfüllt), Kühlleistungsgrenze 15 kW (Spitze ohne Grenze rund 21 kW).
**Abweichend von Kühlkonzept 10.4 alter Fassung** steht der Projektschalter auf 1: Nach K10 (E27)
entscheidet er, ob ein Projekt Kälte rechnet, und nach E32 gäbe es ohne ihn keine Kühlreihe.

**Ergebnis in der Basis:** Kältebedarf 2,52 MWh/a in 402 Stunden, Kältelast 14,99 kW (die Grenze des
Katalogbaus mal der Skalierung), 25 Stunden an der Grenze, in denen die Raumluft bis 25,0 °C steigt;
ungedeckt (`Kaelterestbedarf` 2,52 MWh/a, Warnung „Kältebedarf ohne Kälteerzeuger" gewollt).
Heizwärme 90,1943 MWh/a (R12: 90,1944 — gekühlt auf 24 °C ist die frühere Kappung bis auf die
Grenzstunden), Überhitzungsstunden 306. Im Export: `kuehlbedarf_0.csv` bleibt,
`waermebedarf_kuehlung.csv` kommt dazu, und sechs der neun Kältespalten erscheinen
(`Waermebedarf_Kuehlung`, `Kaeltebedarf_Gesamt`, `Kaeltelast_Max`, `Kaelterestbedarf`,
`Deckung_Kuehlung` von BHKW und Heizkessel); Wärmepumpe, Solarthermie und Pufferspeicher schreiben in
1017 keine Zeile bzw. bleiben nach K7 leer.

## 4 Die Kühldecke als eigener Knoten — verworfen

Außerhalb des Produkts (eine nicht eingecheckte Messklasse, lokal gegen die AixLib-Daten) ist
Normfall 11 mit einem eigenen, masselosen Deckenknoten nachgerechnet worden: Deckenfläche aus dem
Unterschied der gemischten Innenbeiwerte der Fälle 1 bis 9 und 11 abgeleitet (rund 17,4 m² von
75,5 m², α_kon 5,0 statt 1,7), Anschluss an die Innenmasse, Strahlungsleitwert zur Außenwand und
innere Strahlungslasten flächenanteilig, die Kühlleistung allein am Deckenknoten. Ohne Teilung trifft
die Messklasse den Löser des Kerns auf 0,000 W — sie ist damit geprüft.

| Fassung | Stunden im Band | Umschaltstunden (Tag 10 und 60, Stunde 10) gegen den Kernlöser |
|---|---:|---:|
| ein Innenknoten (Kernlöser) | 70 von 72 | ±0 |
| Deckenknoten, Fläche abgeleitet | 56 von 72 | rund −27 W |
| dazu Strahlungsaustausch Decke ↔ Innenflächen, 5 W/(m²K) | 57 von 72 | rund −23 W |
| dazu Strahlungsaustausch Decke ↔ Innenflächen, 50 W/(m²K) | 58 von 72 | rund −9 W |

Der eigene Deckenknoten **verschlechtert** den Fall: Die Umschaltstunden rücken weiter vom Band ab, und
weitere Stunden der Kühlphase fallen heraus (bis rund 59 W). Mit enger Kopplung der Decke an die
übrigen Innenflächen nähert sich das Ergebnis wieder dem Ein-Knoten-Modell, ohne es zu übertreffen.
Die Kühldecke ist als Ursache der zwei Umschaltstunden damit widerlegt; wahrscheinlicher ist die
Messkette der Referenz (Ersatzspalte in den ersten 120 s nach jedem Umschalten, Befund E 5.3). Ein
Deckenknoten bräuchte außerdem Parameter, die der Datensatz nicht führt — ein Hilfskonstrukt. Der
Löser bleibt unverändert, die Messklasse ist nicht eingecheckt.

## 5 Die neue Basis

`Referenzlaeufe/2026-09-23_R13_Kuehlung`, dreizehn Projekte, 387 CSV, 2 207 Skalare; gegen R12 (399
CSV, 2 239 Skalare):

| Projekt | Gebäudewärme R12 → R13 [MWh/a] | relativ | `Waermebedarf_Gesamt` R12 → R13 [MWh/a] | Überhitzungsstunden je Gebäude [h] |
|---|---:|---:|---:|---|
| 1007, 1046 | 69,07 → 68,97 | −0,145 % | 73,13 → 73,03 | 469 → 600 |
| 1008 | 104,20 → 104,14 | −0,058 % | 104,20 → 104,14 | 295 → 440; 469 → 600 |
| 1017 (gekühlt) | 90,19 → 90,19 | −0,0001 % | 90,19 → 90,19 | 305 → 306 |
| 1018 | 68,27 → 68,25 | −0,032 % | 68,27 → 68,25 | 154 → 233 |
| 1023, 1024 | 450,56 → 449,90 | −0,145 % | 510,56 → 509,90 | 628 → 840 |
| 1039 | 597,72 → 597,02 | −0,117 % | 618,72 → 618,02 | 72 → 102; 264 → 308; 628 → 840 |
| 1041, 1042, 1045 | 75,98 → 75,94 | −0,050 % | 176,41 → 176,37; 105,98 → 105,94; 80,98 → 80,94 | 277 → 331 |
| 1040 | 59,35 → 59,35 | ±0 | unverändert | — |
| 1030 | — | — | unverändert | — |

Die Wärmelast bleibt überall gleich, die ungedeckte Restwärme sinkt leicht (1023, 1024, 1039).
Dateien und Schlüssel: 13 Gebäude ohne wirksame Kühlung verlieren `kuehlbedarf_<n>.csv` und je drei
Skalare, 1017 bekommt eine Datei und sieben Skalare dazu. Kein Fehlschlag, kein NaN, keine Ablehnung.
**Determinismus:** zweiter Lauf 387/387 CSV byte-gleich, ebenso ein Lauf auf der Testdatenbank vor dem
Zusammenführen — der Zapfprofil-Testkatalog Z2 bewegt kein Referenzprojekt. Vor dem Einfrieren lagen
neben der Testdatenbank eine leere `-wal`- und eine `-shm`-Datei aus der Zellprobe; sie sind entfernt
und die Basis ist danach eingefroren, ihr Protokoll ohne Warnung.

## 6 Abnahme (nach der letzten Änderung)

| Prüfung | Ergebnis |
|---|---|
| Bau des Kern-Filters | 0 Fehler |
| Windows-Schale und Windows-Referenzlauf | in einen Ordner außerhalb des Repositoriums gebaut, 0 Fehler |
| Test-Gate | Kern 5 258, UI 5 557, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), alle grün |
| ChartProben | 145 Bilder, 0 Verstöße; kein neues Bild |
| Referenzlauf gegen R13 | fünf CI-Projekte GESAMT: PASS (1 744 067 Werte), alle dreizehn GESAMT: PASS (4 145 687 Werte); jeder Lauf 387/387 CSV byte-gleich zur Basis |
| SqlDialektPruefer | 1 726 SQL-Texte, 0 Fundstellen |
| Normfälle (lokal, AixLib) | 11 von 12 im Band, Fall 11 unverändert (3,4 W bzw. 2,8 W) |

## 7 Offen

- **Für KU2:** Kühlbetrieb, `Kuehl_Vorlauf` und gesäte Kühlkenndaten der Wärmepumpe 1017033
  (`KU-S3`), eigener Einfrierschritt (K19); Prüfaufgabe K22; die Fragen K8, K9, K21 und K23.
- **Wiki:** die Seiten „Gebäude", „Simulation" und „Simulationsergebnisse"; Upload der Seiten
  „Kühlung" und „Gebäudemodell VDI 6007" und der Logbuch-Satz mit der Versionsnummer beim Anwender.
- **Löschliste GA:** unverändert — E32 und das Referenzprojekt fügen keinen Bestandsweg-Sonderfall
  hinzu.
