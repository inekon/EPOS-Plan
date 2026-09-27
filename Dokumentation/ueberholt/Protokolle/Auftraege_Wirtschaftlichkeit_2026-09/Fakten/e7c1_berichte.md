

===== Bericht 1 (2026-09-23T06:38:42.133Z) =====

Phase 1 von E7c1 ist fertig: gebaut, Tests geschrieben (nicht ausgeführt), in 7 Commits auf `e7c1` festgehalten. An den 13 Basisprojekten ändert sich kein einziger Wert; alle Anker bleiben. `dotnet test` und der Referenzlauf sind noch nicht gelaufen, beide warten auf Phase 2.

Kern-Filter und Windows-Schale bauen mit 0 Fehlern; alle Warnungen stammen aus Dateien, die ich nicht angefasst habe. Der SQL-Prüfer auf der nach 105 migrierten Kopie findet nichts (1.579 SQL-Texte, 0 Fundstellen). Der Designer ist stimmig (7.625 Einträge, wiederholbar). BOM und CRLF stimmen in 28 von 29 Quelldateien; `Resource.Designer.cs` schreibt der Designer bewusst mit LF.

## Commits (Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e7c1`, auf e06d7eae)

| Commit | Inhalt |
|---|---|
| e531f0bf | E7c1/1: Schemaschritt 105 |
| 152827a3 | E7c1/2: K‑1 Fall 2 |
| e8a6a773 | E7c1/3: A20 Fristende 2030 |
| d17be688 | E7c1/4: Nr. 30 Kohärenzzeile |
| f1014e66 | E7c1/5: Überlagerung „Sätze und Herkunft" |
| 45553be3 | E7c1/6: Ankertests alt = neu |
| 8ca5bcbe | E7c1/7: zwei Doku-Kommentare zur alten Vier-Jahres-Frist nachgezogen |

Kein Push, kein Merge, kein Stash. Testdatenbank, Papiere, Mockups und Wiki sind unverändert.

## Was je Punkt gebaut ist

1. **Schemaschritt 105** (auf Anweisung 105 statt 104), `Zielversion` = 105.
   - Neue Spalte `KWKG_Abwaermeabfuhr`: Ganzzahl, Vorgabe 0, nur 0 oder 1 erlaubt.
   - Neue Spalte `KWKG_Stromkennzahl`: Kommazahl, darf leer sein. Die Tabelle bleibt STRICT.
   - Überall eingetragen, wo auch Schritt 103 steht. Ein Migrationstest ist geschrieben.
   - Bis zum Nachzug fehlt in diesem Zweig Schritt 104. Die Migration 103 → 105 läuft über diese Lücke sauber durch (an der Kopie gemessen).
2. **K‑1, Rechenweg** (neuer reiner Rechner `KwkStromRechner`, eingebaut in `ReiheJeAnlage` und den Ersatzweg).
   - Mit Kennzeichen gilt: KWK-Strom = `min(Netto, Nutzwärme × σ)`.
   - Nutzwärme = Wärme des Moduls minus sein Anteil am Wärmeüberschuss; der Überschuss wird nur nach P_el verteilt.
   - σ ist der gepflegte Wert, sonst P_el ÷ P_th der Gerätezeile.
   - Ist σ nicht bestimmbar: Zuschlag der Anlage 0, dazu eine Herleitungszeile und die Kohärenzzeile „Stromkennzahl fehlt".
   - Die Kürzung geht zuerst von der Einspeisung ab, dann vom Eigenverbrauch.
   - Ersatzweg: Wärme und Überschuss des Projekts nach P_el verteilt, eigene Zeile „Ersatzweg".
   - Die Herleitungszeile je Anlage nennt Fall, σ mit Herkunft, Nutzwärme, KWK-Strom und Kürzung.
   - Der Nachweis bekommt 7 neue Felder, die leer bleiben dürfen; die Nachweisfassung bleibt 7.
3. **Dialog.**
   - Gruppe 1b zeigt eine Zeile zum KWK-Strom und einen Knopf „Sätze und Herkunft…".
   - Die Überlagerung (Mockup U22, auf die zwei Felder beschränkt) enthält:
     - die Wahl Fall 1 / Fall 2;
     - eine Zeile „Stromkennzahl σ" mit Vorschlag P_el ÷ P_th, Herkunft, eigenem Wert, „Vorschlag übernehmen" und einer Spalte „gilt";
     - die Knöpfe Abbrechen und Übernehmen.
   - Geschrieben wird erst beim OK des Dialogs, über `KwkgAnlagenCtrl.Speichere` im Kern. Die Hülle musste im Code nicht angepasst werden.
4. **A20.**
   - Die Konstante `KWKG_REALISIERUNG_JAHRE` ist gestrichen.
   - Neu ist der Katalogschlüssel `KWKG_INBETRIEBNAHME_FRISTENDE`: 31.12.2030, mit Herkunftsangabe, Katalog-Generation 8 (bestehende Datenbanken bekommen die Zeile per Nachsaat).
   - Gelesen wird er mit dem Jahr der Inbetriebnahme und darf leer sein. Fehlt er, erscheint eine Herleitungszeile statt einer stillen Vorgabe.
   - Inbetriebnahme nach dem Fristende: kein Zuschlag, mit Hinweis.
   - Die Reihe läuft bis das Kontingent verbraucht ist (Test: Inbetriebnahme 01.10.2026 ergibt Jahr 12 = 2037).
5. **Nr. 30.** Die Kohärenzzeile „Anlagenart fehlt" (Schwere Hinweis) erscheint nur, wenn das Kontingent aus der Anlagenart abgeleitet werden muss. Ein gepflegtes Kontingent erzeugt keine Zeile. Das gilt auf dem Regelweg und auf dem Ersatzweg.
6. **Tests (geschrieben, nicht gelaufen).**
   - Neu: `KwkgAbwaermeabfuhrSchrittTests`, `KwkStromRechnerTests`, `KwkgFall2Tests`, `KwkgFoerderendeTests`, `KwkgAnlagenartFehltTests` und der bunit-Test `BhkwSaetzeHerkunftTests`.
   - Drei Katalogtests an die Generation 8 angepasst.
   - In den Ankertests stehen Kommentare „alt = neu".

## A/B an den Basisprojekten
Vorher = Kopie mit Schemastand 103 auf e06d7eae, nachher = Kopie mit Schemastand 105.

| Projekt | Größe | vorher | nachher | Grund |
|---|---|---|---|---|
| alle 13 | Anker-Weg, frische Simulation, KWKG-Reihe (8.011 Werte) | – | 0 Abweichungen | kein Projekt trägt das Kennzeichen |
| 1024 | Kapitalwert / Energiekosten / Betriebskosten | −2.896.359,13 € / 188.167,18 €/a / 99,00 €/a | gleich | kein KWKG-Modul |
| 1030 | Kapitalwert / KWKG Jahr 1 | −21.895.377,28 € / 7.315,96 € | gleich | Inbetriebnahme 2027 liegt vor jeder der beiden Fristen; Kontingent gepflegt |
| 1030 | frisch gerechnet: Kapitalwert / Jahr 1 | −31.141.242,71 € / 7.322,63 € | gleich | ebenso |
| 1030 | Energiekosten / Reihe | 1.176.906,60 €/a / Jahre 1–12 | gleich | |
| 1042 | Kaskade | 13.000,00 € | gleich | |

## Proben an 1030 (Anker-Weg, Kapitalwert / KWKG Jahr 1)

| Probe | vorher | nachher | Grund |
|---|---|---|---|
| σ gepflegt 0,5 | −21.895.377,28 / 7.315,96 | −21.904.948,06 / 6.137,94 | 605,52 MWh × 0,5 = 302,76 MWh, Kürzung 71,02 MWh (frisch: 6.138,92) |
| σ berechnet 50 ÷ 81 | ebenso | −21.895.377,57 / 7.315,92 | Kürzung 0,002 MWh, entsteht aus der Rundung auf 0,01 MWh (Frage 1) |
| σ berechnet + 100 MWh Überschuss | 7.315,96 | −21.902.427,26 / 6.448,21 | Nutzwärme 520,77, KWK-Strom 321,47 MWh |
| ohne σ (P_th leer) | 7.315,96 | −21.945.748,56 / 1.116,03 | Zuschlag der Anlage 0, Zeile „Stromkennzahl fehlt" |
| Ersatzweg, σ 0,5 | −21.895.377,34 / 7.315,95 | −21.902.856,75 / 6.395,35 | Kürzung zusammen 54,40 MWh |
| Einspeisung zuerst (künstlicher Bedarf) | 10.184,46 | 7.828,43 | Einspeisung 146,55 → 75,53 MWh, Eigenverbrauch 227,23 MWh unverändert |
| Stichtag 2025, Inbetriebnahme 06/2030 (je Anlage oder am Projekt) | −21.954.815,75 / 0 | −21.896.087,48 / 5.899,97 | alte Regel: mehr als 4 Jahre nach Stichtag; neue: vor dem 31.12.2030 |
| Inbetriebnahme 31.12.2030 oder 03/2031 | 5.899,97 bzw. 0 | gleich | nur der Hinweistext ändert sich |
| Inbetriebnahme 03/2031 ohne Stichtag | 5.899,97 | −21.954.815,75 / 0 | das Fristende gilt jetzt auch ohne Stichtag (Frage 3) |
| Testdaten Anlagenart NEUANLAGE | Anker | gleich (1.948 von 1.948 Werten) | Kontingent gepflegt |
| ohne Kontingent und ohne Anlagenart | 1.116,03 | gleich, dazu die neue Zeile | Nr. 30 |

## Schlüssel
- **Neu:** 43 Ressourcen, jeweils deutsch und englisch:
  - 11 für K‑1 (`WIRT_KWKG_SIGMA_*`, `WIRT_KWKG_FALL2_*`, `KOH_KWKG_STROMKENNZAHL_FEHLT`);
  - 2 für A20 (`WIRT_KWKG_NACH_FRISTENDE`, `WIRT_KWKG_FRISTENDE_FEHLT`);
  - 2 für Nr. 30 (`WIRT_KWKG_KONTINGENT_ANLAGE_OHNE_ART`, `KOH_KWKG_ANLAGENART_FEHLT`);
  - 28 `BHW_*` für die Überlagerung.
- **Neu außerdem:** der Katalogschlüssel `KWKG_INBETRIEBNAHME_FRISTENDE` und die zwei Spalten aus Schritt 105.
- **Geändert:** `WIRT_KWKG_ANLAGE_FRIST` nennt jetzt Fristende und Herkunft.
- **Gestrichen:** keine Ressource, nur die Code-Konstante `KWKG_REALISIERUNG_JAHRE`.

## Abweichungen vom Auftrag
- Schritt 105 statt 104. Die Lücke schließt erst der Nachzug.
- Das Mockup nennt den Schlüssel `WIRT_KWKG_MENGE_FALL2`; gebaut ist die Zeile unter `WIRT_KWKG_FALL2_ANLAGE`.
- Die Überlagerung trägt nur die zwei neuen Felder. Offen bleiben dort: Anlagenart, Tatbestand, Satztafel, Energie- und Stromsteuer, „Wirkung Jahr 1" und „Wahl und Herkunft…".
- Der KI-Feldkatalog kennt die zwei neuen Felder noch nicht, und die Word/Excel-Modultafeln haben keine Spalten für Fall 2.

## Offene Fragen an dich (Lesarten, Empfehlung)
1. **Winzige Kürzungen bei berechnetem σ.**
   - a (gebaut): Formel ohne Toleranz, die Kürzung ist in der Herleitungszeile sichtbar.
   - b: Kürzungen unter 0,01 MWh als 0 werten.
   - Empfehlung: a.
2. **Kontingentverbrauch in Fall 2.**
   - a (gebaut): Vbh und Kontingent zählen weiter nach dem Bruttostrom des Moduls; nur die bezahlte Menge sinkt.
   - b: Vbh aus dem KWK-Strom; das Kontingent reicht dann länger und die Reihe wird länger.
   - Empfehlung: vorerst a. Vorher gegen den Gesetzestext prüfen, ob die Vbh-Definition sich auf KWK-Strom bezieht; falls ja, b in E7c2.
3. **Fristende auch ohne Stichtag.**
   - a (gebaut): ja, das Datum ist absolut.
   - b: nur prüfen, wenn ein Stichtag vorliegt.
   - Empfehlung: a.
4. **Fristende fehlt im Katalog.**
   - a (gebaut): Zuschlag bleibt, mit einer Zeile „ungeprüft".
   - b: dann kein Zuschlag.
   - Empfehlung: a.
5. **Schwere der zwei Kohärenzzeilen.**
   - a (gebaut): Hinweis.
   - b: Warnung.
   - Empfehlung: a, denn die Folge steht bereits in der Herleitungszeile.
6. **Nachweisfassung** bleibt 7, wie beim Vorgänger #351. Empfehlung: so lassen.
7. **Rest der Überlagerung, KI-Feldkatalog und Berichtsspalten.** Empfehlung: E7c2.
8. **Katalogzeilen ohne Leser.** `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB` werden weiter gesät, aber von keinem Code mehr gelesen. Empfehlung: in einer späteren Generation kennzeichnen oder entfernen.

## Vorschlag für die erledigt-Gründe
- **A2\* und EZ‑5 (K‑1):** gebaut E7c1 mit Schritt 105, Fall 2 auf Regel- und Ersatzweg und der Überlagerung; Basisprojekte gemessen unverändert.
- **A20:** der Teil Förderende ist gebaut. Mindestabstand und ETS 2 bleiben offen.
- **Nr. 30 und Konzept § 6.3 Nr. 30:** erledigt; die Testdaten spielt der Merge ein.
- **E7‑Q1, E7‑Q2 (mit der Auflage zu σ), E7‑Q3:** umgesetzt E7c1.
- **Konzept § 3.6 und die A20-Zeile im Rechenweg 05:** Vermerk „gebaut E7c1".

## Logbuchsätze (Versionsnummer musst du noch nennen)
- „Im BHKW-Dialog lassen sich unter „Sätze und Herkunft…" je Anlage die Vorrichtung zur Abwärmeabfuhr und die Stromkennzahl pflegen; der KWKG-Zuschlag rechnet dann mit Nutzwärme × Stromkennzahl."
- „Der KWKG-Zuschlag gilt für Anlagen mit Inbetriebnahme bis zum 31.12.2030 und läuft bis zum Ende des Vollbenutzungsstunden-Kontingents."

## Testdaten 1030
Die SQL-Datei: `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e7c1\testdaten_1030.sql`

Sie setzt bei den Anlagen 14920 und 14921 die Anlagenart auf `NEUANLAGE`, aber nur dort, wo sie noch leer ist. Die Begründung steht im Dateikommentar: Nur die Neuanlage erreicht 30.000 Vbh ohne Kostenanteil. Die Datei ist nicht im Repo.

## Für Phase 2
Ich warte auf „Tests freigegeben" und „Nachzug freigegeben".
- **Erwartet rot:** Schemastand-Wache und Auslieferungsvorlage, bis die Repo-Testdatenbank auf 105 steht.
- **Nachzug, Nummerierung:** `e7b` steht inzwischen bei bfbfbbb9 mit Schritt 104. Die Katalog-Generation 8 gibt es bisher nur hier, ein Zusammenstoß ist also nicht zu erwarten.
- **Nachzug, Konflikte:** Zu erwarten in `SchemaStand`, `SchemaMigration`, `TestDatenbank.SchemaNachziehen` und `Werkzeuge/Testdatenbankschema`.

===== Bericht 2 (2026-09-23T06:59:34.625Z) =====

Nachzug und Phase 2 sind fertig. Nach dem Merge war nur die Schemastand-Wache rot, wie erwartet. Auf einer Kopie, die auf 105 gebracht ist und die 1030-Testdaten trägt, sind alle Tests grün. Referenzlauf 13/13 PASS. Die Werte an 1030 sind nach dem Nachzug unverändert.

## SHAs (Zweig `e7c1`, Spitze 1599faf4, kein Push)
| SHA | Inhalt |
|---|---|
| e531f0bf … 8ca5bcbe | E7c1/1–7 wie im Zwischenbericht |
| **91dd77ba** | Merge „E7c1: Arbeitszweig 954d4dcc in e7c1 zusammengeführt" (Eltern 8ca5bcbe, 954d4dcc) |
| **1599faf4** | E7c1/8: zwei Katalogtests zählen nach der Nachsaat |

## Nachzug
- **Konflikte in `SchemaStand` und `SchemaMigration`** inhaltlich aufgelöst:
  - `SchemaStand`: Zielversion 105; der Hinweis „104 kommt mit dem Nachzug" ist gestrichen.
  - `SchemaMigration`: Schritt 104 in der Fassung des Arbeitszweigs, 105 dahinter.
- **`TestDatenbank.SchemaNachziehen`** wurde automatisch zusammengeführt; die Reihenfolge habe ich geprüft.
- **`Werkzeuge/Testdatenbankschema`**: Platzhalterzeile zu 104 entfernt.
- **Kette 101 → 105** an allen vier Stellen geprüft: Gebäude, Anlagenart, Tww, Staffel, K‑1.
- **Ressourcen:** deutsch und englisch je 7.729 Einträge, keine Duplikate, gleiche Schlüssel. Der Designer-Prüflauf ist unverändert (7.726 Einträge, wiederholbar). Keine Konfliktmarker mehr.
- **Builds:** Kern-Filter und Windows-Schale je 0 Fehler.

## Tests
Vor jedem Lauf habe ich `tasklist | grep -i testhost` geprüft; es lief nie ein fremder Testprozess.

- **Migrationstest 105** (jetzt ab 104): 3/3 grün.
- **Gefilterte Klassen im Kern:** zunächst 238 von 240. Zwei Katalogpflege-Tests waren rot, ein Fehler aus meiner E7c1/3, den ich in Phase 1 übersehen hatte:
  - Mit Katalog-Generation 8 zählt die Datenbank nach der Nachsaat 227 Zeilen statt 226.
  - „Anlegen" zog die Nachsaat mitten im Test nach.
  - Behoben in E7c1/8; danach 132/132 grün.
- **Gefilterte Oberflächentests** (BHKW-Dialog, Wachen): 303/303.

**Voller Lauf auf der Repo-Datenbank (Schemastand 104):**

| Testprojekt | Ergebnis |
|---|---|
| EPOS.Kern.Tests | 4.811 von 4.812 grün; rot nur die Schemastand-Wache (104 statt 105) |
| EPOS.UI.Tests | 5.284/5.284 |
| KiKern.Tests | 524/524 |
| SpeicherEngine.Tests | 386/386 |
| SpeicherPlanung.Tests | 27 grün, 1 übersprungen |
| Auslieferungsvorlage | 14 von 26 rot, alle mit „Schemastand 104 (erwartet 105)" |

**Gegenprobe:** Die Repo-Datenbank im Worktree habe ich vorübergehend durch eine Kopie ersetzt, die außerhalb des Repos auf 105 gebracht ist und die 1030-Testdaten trägt. Ergebnis:
- Kern-Filter: 11.033 grün, 1 übersprungen, 0 rot.
- Auslieferungsvorlage: 26/26.
- Danach aus LFS zurückgeholt; SHA-256 wieder gleich der LFS-Kennung 044e44db…, Arbeitsbaum sauber.

## Referenzlauf, SQL-Prüfer
- **Referenzlauf** auf der 105-Kopie gegen R11: **13/13 PASS**, 3.882.737 Werte in der Toleranz, 357 von 357 Dateien byte-gleich. Mit den 1030-Testdaten ebenso.
- **SQL-Prüfer** auf der nachgezogenen 105-Kopie: 1.645 Texte, **0 Fundstellen**.

## 1030 nach 105
- **Basismessung, 13 Projekte:** 8.095 von 8.095 Werten gleich dem Stand vor dem Nachzug.
  - 1030 Anker: −21.895.377,28 €, KWKG Jahr 1 7.315,96 €, Energiekosten 1.176.906,60 €/a.
  - 1030 frisch gerechnet: −31.141.242,71 € / 7.322,63 €.
  - 1024: −2.896.359,13 €.
- **Mit dem Testdaten-UPDATE (NEUANLAGE):** 8.095 von 8.095 Werten gleich, kein Anker bewegt sich.
- **Alle 20 Proben** (K‑1, A20, Nr. 30) sind gleich dem Stand vor dem Nachzug, 0 Abweichungen. Die Tafeln des Zwischenberichts gelten damit unverändert, zum Beispiel σ 0,5: 6.137,94 € / −21.904.948,06 €.

## Für dein Einspielen
- **Datenbank auf 105 bringen:** `Werkzeuge/Testdatenbankschema` legt die zwei Spalten an und sät zugleich die Katalog-Generation 8 nach (1 Zeile, danach 227). Genau diesen Stand hat die Gegenprobe getestet.
- **Testdaten 1030:** Die SQL-Datei liegt unverändert unter `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e7c1\testdaten_1030.sql`.
- **Gegenprobe-Kopie** zum Nachsehen: `…\scratchpad\e7c1\merge105_1030.sqlite`.
- **`Referenzlaeufe/LIESMICH.md`** braucht einen Schemastand-Vermerk, sobald die Datenbank auf 105 committet ist. Das habe ich nicht angefasst (Papierregel).