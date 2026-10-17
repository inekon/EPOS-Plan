# Protokoll AK2 Teil 2 — Oberfläche, Bericht, Wiki, Referenzprojekt 1056, Basis R37 (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Wellen AK2-3 und AK2-4 (E67, E83, E85), Commits AK2-3 `57b5a1f`, `fdb3a3d`, `1778f85`, `9559052`, `c23b511`, `80e733b`, Merge `5872a3c`, Nachzug `3c5d6c8`; AK2-4 `bffc903`, `328a0dc`, `2e58f60`, `ea33867`, `b80c8ad`, `ead2d52`, Merge `e1487e7`, `c5e405c`, `8a2adbe`
**Entscheid:** E83 (Bild Woche), E85 (Komfortspalten je gekoppeltem Projekt), E84 (Nennleistung erst mit AK3). Schemaschritt 186 aus Teil 1; Basis R37.

## 1 Auftrag

Zweiter Teil der Stufe AK2 (Arbeitsplan [`2026-10-05_Plan_AK2.md`](2026-10-05_Plan_AK2.md), Teil 1: [`AK2 Teil 1`](2026-10-05_AK2_Teil1_Fahrplan_Komfort.md)): Oberfläche, Bericht und Wiki zum Anlagenfahrplan (AK2-3) sowie Referenzprojekt, Komfortspalten und Einfrieren der Basis (AK2-4).

## 2 Vorgehen

Zwei Wellen, je ein Worktree, Commits sofort, kein Push. Gate 731 auf dem Merge von AK2-3, Gate 734 auf `2e4814c` für die Gesamtwelle (Zahlen trägt die Orchestrierung nach).

## 3 Ergebnis

**AK2-3 (Oberfläche, Bericht, Wiki).**
- Gruppe „Betriebszeiten“ im Baustein `WaermepumpeKonfiguration` (Wärmepumpen-Anlagendialog und Simulation › Konfiguration): Wochenraster für das Zeitprogramm (Faktoren 0…1, leer = immer verfügbar), Feld „Höchster Vorlauf“ (Vorgabe `Vorlauf`, Grenzen 20…120 °C), Vorrang der Sperrzeit, Info-Zeile zu überschneidenden Wochenstunden.
- `BetriebszeitenAbbildung` in `EPOS.UI.Daten`; Schreibweg `WErzeugerCtrl.KonfigurationFelder` mit Schalter `Betriebszeiten`.
- Bedarfsdialog: Komfortkacheln (Unterschreitungsstunden, Kelvinstunden, längste Strecke; Kälteseite) neben dem Restbedarf, Bedarfsbegriff und Fahrplanstunden; Bild „Raumtemperatur und Sollwert“ als Woche mit der größten Unterschreitung (`Komfortwoche`, `ChartRenderer.KomfortwocheModell`); „—“ ohne Kopplung.
- Bericht: Tafel „Komfort und Restbedarf“ nur bei greifendem Fahrplan (Bedarfsbegriff je Gebäude, Zahl der festen Lasten, Hinweis Profilweg).
- 34 Ressourcenschlüssel; ChartProben-Messlatte `Messlatte_2026-10-05.sha256` (211 Zeilen, drei neue Bilder); Wiki Energieerzeuger („Betriebszeiten“) und Gebäudemodell VDI 6007 („Komfortstunden“); Logbuch-Entwurf Konzept 12.4.

**AK2-4 (Referenzprojekt 1056, Komfortspalten, Basis R37).**
- F12/E85 in `SimulationRunner.AnlagenfahrplanSpaltenSetzen`: Komfortspalten, sobald der Fahrplan lief; `Fahrplan_Begrenzt_Stunden` 0 statt NULL.
- Referenzprojekt **1056 „Referenz Kopplung mit Fahrplan“**, Kopie von 1047 auf dem Kopierweg des Programms (Gebäude 10662, Wärmepumpe 1672052, Kessel 1018353): Wärmepumpe `Sperrung` 1, 0–6 Uhr (Altfenster ohne Mitternachtsübertrag), `Vorlauf_Max` 50 (Vorlauf 55); Kessel und BHKW Zeitprogramm 0 von 0 bis 6 Uhr; kein Puffer. Skript `Referenzlaeufe/Skripte/referenzprojekt_1056_fahrplan.py`.
- Wache `FahrplanReferenzprojektWacheTests` (3), elf Zähltests um 1056.
- Basis **R37 `2026-10-05_R37_Fahrplan`**: 21 Projekte, 646 CSV, 4 265 Skalare, 78 MB; 18 Projekte byte-gleich mit R36; 1047 und 1054 nur neue Zeilen in `aggregate.csv` (1047: Fahrplanstunden 0, Unterschreitung 875 h, 1 281,04 Kh, längste Strecke 11 h, Überschreitung 32 h / 43 Kh; 1054: 0, 2 162 h, 2 138,59 Kh, 16 h); 1056 neu (Fahrplanstunden 1 249, Unterschreitung 1 854 h, 3 883,19 Kh, längste Strecke 16 h, Restbedarf 0).
- Einfrierregel „gesäte Auslegungsdaten der Übergabe“ um Sperrzeit samt `Tab_Sperrfenster`, `Zeitprogramm`, `Vorlauf_Max` erweitert (CLAUDE.md, LIESMICH); `kern.yml`/`ios.yml` auf R37, 1056 nicht in der CI-Auswahl.
- Testdatenbank 87 764 992 Byte, OID 63b0bdae….

## 4 Festlegungen

- E84: Nennleistung der Erzeuger nicht als Schranke in AK2, erst mit AK3.
- E85: Komfortspalten für jedes gekoppelte Projekt, auch ohne kappende Stunde.
- Betriebszeiten nur an der Wärmepumpen-Maske; Kessel und BHKW tragen den Wert aus der Datenbank ohne Eingabe.

## 5 Nachweise

- **Gate 731** (`5872a3c`, Worktree): Kern 11 022 grün + 1 rot, UI 7 535 + 1 rot (zwei Zählwachen: Bildmethoden 35, Eingabestellen 19, Nachzug `3c5d6c8`), ChartProben 211/211 gleich, Dokumentationswachen 35, Referenzlauf 20/20 PASS gegen R36 und 608/608 byte-gleich, Plattformnachweis PASS, Schale, Designer, SQL 2 351/0, Werkzeugtests grün.
- **AK2-3:** 11 neue UI-, 6 neue Kern-Tests; UI 188/188, Kern 628/628, Wachen 38/38; SQL 2 351/0; acht CI-Projekte byte-gleich.
- **AK2-4:** Kern-Volllauf 11 029 mit 17 rot (16 Zähler behoben, 1 fremd = Bildmethoden, im Hauptzweig behoben), Filterlauf 249/249, SQL 2 351/0.
- **Gate 734** auf `2e4814c`: Zahlen folgen.

## 6 Offenes

- Sichtabnahme unter Windows (Betriebszeiten, Komfortkacheln, Komfortwoche); Windows-Messliste des Gates (drei neue Zeilen).
- Logbuch-Version, Wiki-Upload.
- Komfortwerte je Gebäude ohne Datenbankspalte (Bericht zeigt „—“ je Gebäude aus der Datenbank).
- Upload der Datenbankobjekte 186 und 1056 beim Anwender; CI-Kennung nach dem Push.
- Nächste Stufen: KU3-6, AK3 (Rückfrage H6), GA (Rückfrage Ablösekriterium).
