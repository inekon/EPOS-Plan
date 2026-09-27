

===== Bericht 1 (2026-09-23T15:33:16.739Z) =====

**Zwischenbericht E7c3 (nach den Punkten 5, 3, 6, 7 und 2)**

Zweig `e7c3`, Worktree `.claude/worktrees/e7c3`, Basis c0131b2b. Commits: ba9d0b13 E7c3/5 · 3e7bc8b4 E7c3/3 · 92aa069a E7c3/6 · f114499b E7c3/7 · 46291409 E7c3/2. Kern-Filter und Windows-Schale bauen mit 0 Fehlern, Designer ist gezogen. Es gibt keinen Schemaschritt; `Zielversion` bleibt 113. Die Testdatenbank ist nicht committet. `dotnet test`, Referenzlauf und SQL-Prüfer laufen erst in Phase 2.

**A/B** (Messprogramm auf Scratchpad-Kopien, Schemastand 113; verglichen wird jeweils mit dem vorigen Commit): Die 13 Basisprojekte haben nach jedem Punkt 9.519 von 9.519 Werten gleich.

| Punkt | Probe | vorher | nachher |
|---|---|---|---|
| 5 | 1030 σ 0,5: Vbh / KWKG Jahr 1 / Kapitalwert | 6.055,2 h/a / 7.316,03 / −21.895.376,67 | **7.475,69 / 6.137,94 / −21.904.948,06** (Stand vor E7c2/7 bestätigt; frisch gerechnet 6.138,92) |
| 5 | σ berechnet + 100 MWh Überschuss · Ersatzweg · ohne Deckel (KW) | 7.316,03 · 7.316,00 · −21.890.762,63 | 6.448,21 · 6.395,35 · −21.900.597,13 |
| 5 | Rückfall ohne gespeicherte Modul-Vbh, 5 % Hilfsstrom | Vbh 6.613,42 (netto), Jahr 1 7.316,00 | Vbh 7.475,6 (brutto), Jahr 1 6.600,94 |
| 3 | `VpvCtKwh` bei EV, 100 kWp / 30 kWp / 750 kWp | 6,03 / 7,01 / 5,52 | 6,032 / 7,006667 / 5,518933 (jetzt gleich dem EvCt der Erlösreihe) |
| 6 | Testdatenbank, Brennstoff 24 Hi/Hs; Marker | 0/0; 8 | 1,0/1,0; 9; 227 Zeilen unverändert, zweiter Lauf ändert nichts |
| 7 | `KWKG_REALISIERUNGSFRIST`, `KWKG_STICHTAG_DAUERBETRIEB` | GESICHERT | ABGEKUENDIGT; Werte 4 und 2026 bleiben |

**Befund Kapitalwert 1024:** Die −676.036,81 € sind nachgerechnet. Es ist kein Codefehler, beide Anteile kommen aus dem Datenstand.
- **(1) −676.495,37 €** kommen aus Schemaschritt 83 (#313, 17.09.). Er hat die aktiven Strompreisanteile des Trägers 60 in den Arbeitspreis gefaltet: 11,746 ct/kWh im Modus „aufgeschlüsselt", 35,000 → 46,746 ct/kWh. Vorher rechnete die Wirtschaftlichkeit diese Anteile nicht mit. Rechnung: 387,12 MWh × 0,11746 € = 45.471,12 €/a, mal Faktor 14,877 (3 %, 20 Jahre). Die Sicherungen des Anwenders zeigen 0,35 am 02.09. und 14.09. und 0,46746 ab 17.09.
- **(2) +458,56 €** entstanden bei der Übernahme von Access nach SQLite am 02.09. (FX1: „Anker-Aktualisierung −2.219.863,76, Datenstand"). Genauer aufteilen lässt sich dieser Rest ohne den Code aus der Access-Zeit nicht.
- Die drei Kandidaten aus dem Konzept (B‑1/#331, #365/#366, Schritte 93–96) tragen 0 € bei. Mit 35,000 ct rechnet der heutige Kern bitgleich −2.219.863,761540025 €, also den Wert aus B5 und FX1–FX4. Das hält der neue Ankertest fest.
- **Vorschlag für die Konzepttafel:** Anker −2.896.359,13 € bleibt, mit Herkunft „Schritt 83 (+ Datenstand 02.09.)". Der Konzeptwert wird ersetzt.

**Offene Fragen** (gebaut ist jeweils a):
1. **Kapitalwert 1024.** (a) Der gemessene Wert ist richtig: Es gilt ein einziger Strompreis, und die Anteile waren am Träger aktiv. (b) Der Projektschalter war aus; dann müsste 1024 in der Testdatenbank auf 35 ct zurück (Datenkorrektur, Anker −2.219.863,76). Empfehlung: a.
2. **Punkt 7.** (a) Neuer vierter Status ABGEKUENDIGT. (b) Stattdessen ein Vermerk in der Quelle. Empfehlung: a.
3. **Punkte 6 und 7.** (a) Generation 9 als reine Nachpflege beim Start. (b) Stattdessen Schritt 114. Empfehlung: a. Die Testdatenbank im Repo steht auf Marker 8 und kann mit `Werkzeuge/Testdatenbankschema` auf 9 gebracht werden; die Tests gehen in beiden Fällen.
4. **Punkt 5, Erweiterung.** Der Rückfall `VbhDerAnlage` nimmt jetzt die Bruttoerzeugung. Das greift nur ohne gespeicherte Modul-Vbh und mit Hilfsstrom; kein Basisprojekt ist betroffen. Empfehlung: so lassen.

**Für die Papierwelle:** Die Wiki-Quelle `Programm Dokumentation - Wirtschaftlichkeit.wiki` (Anker kwk-abwaermeabfuhr) beschreibt noch den Stand von E7c2/7. Der KI-Feldtext ist schon angepasst.

Als Nächstes folgen ohne Pause B‑6 (in Gruppen), Q8 b, U22 und 9h.

===== Bericht 2 (2026-09-23T16:45:48.387Z) =====

Phase 1 von E7c3 ist fertig. Alle neun Punkte sind gebaut oder gemessen und committet, zusammen 13 Commits auf Zweig `e7c3`. Nichts ist gepusht, der Arbeitsbaum ist sauber, und es gibt keinen Schemaschritt (Zielversion bleibt 113, nächste freie Nummer laut origin 114). Die Testdatenbank ist nicht committet. Tests, Referenzlauf, Designer-Prüfung und SQL-Prüfer habe ich nicht gestartet, sie warten auf „Tests freigegeben“.

Einen wichtigen Befund gab es bei B‑6: Die Datenbankschicht (`DataRepository`) wirft bei einem Abfragefehler nie. Sie zeigt einen Dialog oder sammelt den Fehler still und liefert eine leere Tabelle. Die benannten Fänge der ersten B‑6-Commits (1a–1c) hätten deshalb nur Umwandlungsfehler erwischt. Das habe ich mit einem strengen Leseweg nachgebaut (Commit 1d, Einzelheiten unter Punkt 1).

## Commits
| SHA | Punkt |
|---|---|
| ba9d0b13 | 5 Vbh nach Definition, E7c2/7 zurückgebaut |
| 3e7bc8b4 | 3 Q5 b ungerundeter EV-Mix |
| 92aa069a | 6 Brennstoff 24 mit H_i = H_s = 1,0 |
| f114499b | 7 zwei KWKG-Katalogzeilen „abgekündigt“ |
| 46291409 | 2 Kapitalwert 1024 nachgerechnet |
| d5f2d00a, a2863b31, 8820ec1e, 27e4cd7b, 38ca5bc5 | 1 B‑6 in fünf Schritten (1a–1e) |
| 18d148a4 | 4 Energiesteuer-Vorschau je Wahl |
| 5f7711a7 | 8 Anzeigezeilen in der Überlagerung |
| cabd55a9 | 10 Ankertests „alt = neu“ |

## Die Punkte

**5 Vbh:** Vbh = erzeugte Arbeit ÷ P_Nenn, brutto, in Fall 1 und Fall 2 gleich. Kontingent und Deckel zählen wieder nach dem Modulstrom, der KWK-Strom bestimmt nur die bezahlte Menge. Die Herleitungszeile nennt die Formel mit Zahlen. Probe 1030 mit σ 0,5 ist zurück auf den Werten vor E7c2/7:
| Größe | vorher | nachher |
|---|---|---|
| Vbh | 6.055,2 h/a | 7.475,69 h/a |
| KWKG Jahr 1 | 7.316,03 € | 6.137,94 € |
| Kapitalwert | −21.895.376,67 € | −21.904.948,06 € |

**3 Q5 b:** Der Satz der Speicherbewertung nimmt jetzt den ungerundeten EV-Mix, z. B. 100 kWp 6,03 → 6,032 ct/kWh, 750 kWp 5,52 → 5,518933 ct/kWh.

**6 und 7:** Beides läuft über eine neue „Katalog-Generation 9“, die bestehende Zeilen nachpflegt statt neue zu säen, ohne Schemaschritt. Brennstoff 24 bekommt H_i = H_s = 1 (0/0 → 1/1). Die zwei Katalogzeilen ohne Leser tragen den neuen Status ABGEKUENDIGT; gelöscht wird nichts.

**2 Kapitalwert 1024:** Die Abweichung von −676.036,81 € ist kein Codefehler, sondern Datenstand:
- −676.495,37 € kommen aus Schemaschritt 83: Er hat 11,746 ct/kWh Strompreisanteile in den Arbeitspreis gefaltet (35,000 → 46,746 ct/kWh).
- +458,56 € kommen aus der Übernahme Access → SQLite am 02.09.
- Mit 35 ct/kWh rechnet der heutige Kern bitgleich −2.219.863,76 €.

Der Anker bleibt. Vorschlag: In die Konzepttafel kommt der gemessene Wert mit diesem Befund.

**1 B‑6:**
- **Leere Fänge:** In den fünf Prioritätsdateien sind alle 100 bisher leeren `catch` benannt; übrig ist keiner.
- **Strenger Leseweg:** 26 Lesestellen lesen über neue strenge Lesemethoden (`StilleDb.TabelleStreng`/`ScalarStreng`), die den Fehler weiterreichen.
- **Sichtbarkeit:** Ein Fehler steht als Warnzeile „Prüfung/Rechenstufe „X“ nicht ausführbar: <Grund>“ an jeder Ergebniszeile oder als Grund in einer Eigenschaft. Das sind `Lesefehler`, `LetzterFehler` und `Katalogfehler` sowie `Ladefehler`, `Speicherfehler` und `Vorsorgewarnung`.
- **Nachgemessen:** Neun Fehlerlagen habe ich auf Datenbankkopien mit dem Messprogramm im Scratchpad nachgestellt, z. B. eine umbenannte Anlagentabelle, ein kaputtes Datum im Parametersatz, ein Speichern mit Abbruch-Trigger. Jede ergab die erwartete Zeile, ohne Fehler steht keine. Das war kein `dotnet test`.
- **Rest außerhalb der Priorität:** 29 leere Fänge in 13 Dateien, dazu 5 stille Lesestellen im Engine-Modus.

**4 Q8 b:** Der Lauf rechnet für jede Anlage mit Brennstoff jede Wahl vor: keine, § 53 voll und energetisch, § 53a, § 54. Es ist dieselbe Steuerrechnung auf einer Kopie der Eingabe. Die Wirkung ist der Unterschied zu „keine“ für diese Anlage, Sockel und Mischlage wirken also wie im Lauf und haben ihren Grund an der Zeile. Die Vorschau wird mitgespeichert, d. h. auch ein geladener Stand trägt sie. Handprobe Rechenweg 05:
| Wahl | Betrag Jahr 1 |
|---|---|
| § 53 voll | 26.383,46 € |
| § 53 energetisch (Anteil 0,458) | 12.079,17 € |
| § 53a | 21.202,71 € |
| § 54 (nach 250 € Sockel) | 6.369,85 € |

**8 U22:** Jede Wahl der Überlagerung steht jetzt als eine Zeile: Wahlknopf, Text und dahinter die Wirkung, z. B. „→ 5,50 €/MWh · 26.383,5 €/a“. Die Klapplisten im Formular bleiben. Die Zeilen habe ich mit Edge als statisches Bild geprüft; sie sehen aus wie im Mockup.

**9 Nr. 9h (nur gemessen):**
| Tabelle | Nutzungsdauer gepflegt (Testdatenbank) | Wer liest sie |
|---|---|---|
| `Tab_BHKW` | 3 von 6 (10 a); Stamm 44 von 79 | kein Leser in der Wirtschaftlichkeit |
| `Tab_Heizkessel` | 1 von 22 (20 a); Stamm 0 von 63 | kein Leser in der Wirtschaftlichkeit |
| `Tab_StromspeicherVariante` | 13 von 13 (20 a) | rechnet in der Speicherwirtschaftlichkeit und im Peak-Shaving |

- **Nutzungsdauer-Tabelle:** Sie führt abweichende Werte: BHKW-Modul 15 a, Batterie 10 a.
- **Speicherflotte 1046:** zwei Einheiten mit Ersatzintervall 10 Jahre und Restwert 500 bzw. 300 €.
- **Vorschlag für ND‑S3:**
  - A8: Die Gerätespalten nur als „Gerätedaten“ kennzeichnen.
  - Speichervariante und Flottenintervall: Die Tabelle nur als Vorgabe für neue Einträge nehmen; das bewegt nichts.
  - Restwert: Einen linearen Restwert aus der Nutzungsdauer gibt es erst mit ND‑S3 und neu eingefrorener Basis für 1046.

## A/B
Alle 13 Basisprojekte: 9.519 von 9.519 Werten gleich, gemessen nach jedem Punkt. Punkt 4 fügt nur die neuen Vorschauzeilen hinzu. Die 18 Proben sind außerhalb der gewollten Änderungen aus Punkt 5 gleich, die Katalogmessung auch. Keiner der vier Anker bewegt sich: 1024 −2.896.359,13 €, 1030 −21.895.377,28 €, 99,00 €, 13.000,00 €.

**Schlüssel:** 43 neu (de/en). Geändert: `WIRT_KWKG_FALL2_VBH`, `WIRT_KWKG_FALL2_VBH_ERSATZ`, `KI_DLG_BHW_ABWAERME_ERL`. Keiner entfallen.

## Abweichungen
- Ein Lesefehler in den fünf Dateien erscheint nicht mehr als Dialog, sondern als Zeile am Ergebnis.
- Eine fehlende Spalte `Hilfsenergie_Anteil` ist jetzt ein Lesefehler statt „kein Anteil“. Das betrifft nur Datenbanken vor Schritt 61, die den Kern nicht mehr erreichen.
- Ein nicht lesbarer Tarif gilt als nicht aktiv.
- Der gespeicherte Nachweis springt auf Fassung 8. Ein Test pinnt die Fassung und ist von 7 auf 8 umgestellt.
- Ohne gespeicherte Modul-Vbh rechnet der Rückfall jetzt mit dem Bruttostrom statt dem Nettostrom.

## Offene Fragen (gebaut ist jeweils a)
1. **Kapitalwert 1024:** a Konzepttafel auf den gemessenen Wert, b Strompreis 35 ct zurück. Empfehlung a.
2. **Katalog-Generation 9:** a als Nachpflege, b als Schemaschritt 114. Empfehlung a.
3. **Status ABGEKUENDIGT:** a als vierter Status, b nur als Vermerk in der Quelle. Empfehlung a.
4. **Strenger Leseweg (B‑6):** a gebaut, b bei `DataRepository` bleiben und nur leere Tabellen erkennen. Bei b geht der Grund verloren und der Dialog bleibt. Empfehlung a.
5. **B‑6-Rest außerhalb der Priorität:** a eigene kleine Etappe, b so lassen. Empfehlung a.
6. **`Ladefehler`, `Speicherfehler`, `Vorsorgewarnung` in der Oberfläche:** Heute zeigt die Oberfläche sie nicht. a in Statuszeile und BHKW-Dialog zeigen, b nur im Kern lassen. Empfehlung a in der nächsten Welle.
7. **U22:** a Zeilen in der Überlagerung, Klapplisten im Formular bleiben; b wie die ursprüngliche U22-Zeichnung, die Formular-Klapplisten werden Anzeigezeilen. Empfehlung a.
8. **Vorschau bei „Projektvorgabe“ mit mehreren Anlagen:** a je Anlage, b zusätzlich eine Projektvorschau. Empfehlung a.

## Erledigt-Gründe (Vorschlag)
- R‑E7c1 Q8: /7
- R‑E7c2 Q5 b: /3
- R‑E7c2 Q8 b: /4 und /8
- R‑E7c2 Q7 und R‑E7c1 Q2 b präzisiert: /5
- Brennstoff 24: /6
- Konzept § 4 B‑6: für die fünf Prioritätsdateien erledigt, Rest offen (Frage 5)
- Konzept § 6.2 Kapitalwert 1024: /2
- Mockup U22: /8; U39 bleibt teilweise offen (Nr. 9h mit ND‑S3)

## Logbuch (Version bitte bestätigen)
- „Die Überlagerung „Sätze und Herkunft…“ nennt für jede Energiesteuerentlastung Satz und Betrag im ersten Jahr und zeigt die Wirkung jeder Wahl in ihrer Zeile.“
- „Lässt sich eine Rechenstufe der Wirtschaftlichkeit nicht ausführen, steht der Grund als Warnung an der Ergebniszeile.“
- Den E7c2-Satz „Vollbenutzungsstunden aus dem KWK-Strom“ streichen, falls er noch nicht veröffentlicht ist, sonst einen Gegensatz aufnehmen.

## Für die Papierwelle
Papiere, Mockup und Wiki habe ich nicht angefasst. Die Wiki-Seite Wirtschaftlichkeit (Anker `kwk-abwaermeabfuhr`) beschreibt noch E7c2/7 und muss berichtigt werden; ebenso Konzept § 3.6, § 4, § 6.2, § 6.3, Register, Rechenweg 05 und Mockup U22.

## Stand der Abnahme
- **Gebaut:** Kern-Filter und Windows-Schale je 0 Fehler.
- **Merge:** Probemerge mit origin `8c94ee3a` ergibt Konflikte nur in den zwei `.resx` (beide Seiten hängen am Ende an): beide Blöcke behalten, Designer neu erzeugen.
- **Phase 2 nach „Tests freigegeben“:** erst die tasklist-Regel, dann gefilterte Tests der geänderten Klassen, voller Lauf, Referenzlauf 13/13 gegen R12, Designer, SQL-Prüfer.
- **Nutzung:** Wochenbudget 21 %, 5‑Stunden-Fenster 81 % (frei ab 19:40).

Worktree: `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e7c3`

===== Bericht 3 (2026-09-23T17:16:23.892Z) =====

Phase 2 ist abgeschlossen und grün: Nachzug gemergt, voller Lauf ohne Fehler, Referenzlauf 13/13, Designer und SQL-Prüfer sauber. Die Testdatenbank im Repo ist unverändert; eine auf Katalog-Generation 9 gezogene Kopie liegt zum Committen bereit. Zwei Fehler, die erst der Test zeigte, habe ich behoben (1f, 1g). Nichts gepusht, Arbeitsbaum sauber.

## Commits (Zweig `e7c3`, HEAD `77618ce2`)
- **`91315089`** Merge „E7c3: Arbeitszweig 8c94ee3a in e7c3 zusammengeführt“ (Eltern `cabd55a9`, `8c94ee3a`).
- **`29fbbc77`** E7c3/1f: Ein Zeilenumbruch in einer Fehlermeldung (`\r\n`) wurde zu zwei Leerzeichen statt einem. Das fiel im gefilterten Lauf auf.
- **`77618ce2`** E7c3/1g: Die Blattstruktur-Wache rechnet ihre Prüfgruppe jetzt ohne Speichern.
  - Die Gruppe trägt erfundene Projekt-Ids (9001/9002), die es in `Tab_Projekt` nicht gibt; ihr Speichern scheiterte schon immer, nur still.
  - Seit B‑6 steht das als Warnzeile an jedem Ergebnis und verschob im Excel-Blatt die Ankerzeilen um drei.
  - Die Zahlen sind dieselben; gespeichert wurde auch vorher nichts.

## Nachzug und Konflikte
- **Konflikte** nur in `Resource.resx` und `Resource.en-US.resx`. Beide Blöcke sind behalten: erst E7c3 mit 43 Schlüsseln, dann origin.
- **Stolperstelle:** Git hatte das gemeinsame Ende `</data></root>` aus beiden Konfliktseiten herausgezogen. Beim bloßen Zusammenfügen fehlte an der Naht ein `</data>`, und der Designer verlor dadurch `ADM_AW_GEWAEHLT`. Ich habe das `</data>` ergänzt und beide Dateien als XML geprüft: je 8.091 Einträge, keiner doppelt.
- **Schlüsselzahl von origin:** 8.048 Einträge (44 neu, 3 gestrichen, die gestrichen bleiben). Die 8.043 aus deiner Nachricht treffe ich nicht. Meine Merge-Nachricht nennt „41 neue“; das ist die Nettozahl.
- **Designer** neu erzeugt: 8.092 Blöcke, wiederholbar, gleiche Eigenschaften wie der automatisch zusammengeführte Stand.
- **Automatisch zusammengeführt:** `DbWerte.cs`, `Resource.Designer.cs`, `epos-ui.css`, `SchemaMigration.cs`.
- **Schemastand** bleibt 113, kein Schemaschritt.

## Tests
| Lauf | Ergebnis |
|---|---|
| Gefiltert Kern (35 Klassen) | 680 von 681; rot war der Umbruch-Fall, nach 1f RobustheitB6 17/17 |
| Gefiltert UI (Überlagerung, Optionsgruppe, BHKW-Dialog u. a.) | 359/359 |
| Voller Lauf 1 | 2 rot (Blattstruktur-Wache), nach 1g 22/22 |
| **Voller Lauf 2 (19:11–19:14)** | **0 Fehler:** Kern 5.175, UI 5.471, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 + 1 übersprungen |

**Parallellauf:** Mein erster voller Lauf (18:53) startete, während ein Testlauf aus dem Worktree `z2` lief. Meine tasklist-Prüfung zeigte ihn zwar an, brach aber nicht ab. Ich habe nur meinen eigenen Prozessbaum beendet und danach jeweils gewartet, bis kein fremder Testprozess mehr lief. Zählen tun nur die Läufe danach.

## Referenzlauf, Designer, SQL-Prüfer, Build
- **Referenzlauf** 13/13 PASS gegen `2026-09-23_R12_Gebaeudemodell`: 4.250.839 Werte in der Toleranz, 399 von 399 CSV byte-gleich. Das gilt für die Repo-Datenbank und ebenso für die auf Generation 9 gezogene Kopie.
- **Designer-Prüflauf:** unverändert, wiederholbar.
- **SQL-Prüfer:** 1.714 Texte, 0 Fundstellen.
- **Build:** Kern-Filter 0 Fehler; Windows-Schale 0 Fehler (4 Warnungen aus dem Bestand).

## Testdatenbank
- **Repo-Datei:** Sie bleibt unverändert (LFS-Kennung `689a0755…`), weil Tests und Referenzlauf nur auf Kopien arbeiten. Sie steht aber auf Katalog-Generation 8. Beim ersten Katalogzugriff (Programmstart, `Testdatenbankschema`) pflegt der Kern sie auf 9 nach; im Referenzlauf ist das schon so gerechnet.
- **Auf einer Kopie gemessen** mit `Werkzeuge/Testdatenbankschema`, Zellvergleich über alle 130 Tabellen: genau fünf Zellen ändern sich.
  - Brennstoff 24: Hi 0 → 1 und Hs 0 → 1.
  - `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB`: Status GESICHERT → ABGEKUENDIGT.
  - Katalog-Marker 8 → 9.
  - Schema gleich, Integritätsprüfung ok; ein zweiter Lauf ändert nichts.
- **Datei zum Committen:** `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e7c3\testdb_gen9\Kenndaten_Test.sqlite` (SHA-256 `db143098ca0d88233676eecb2be0e65965db5933deb75a4c7f7d32bba4d18f5c`, 67.743.744 Byte). Die `vorher.sqlite` im selben Ordner ist nur die Ausgangskopie.
- **Einfrierregeln:** keine berührt, also keine neue Basis. Die Tests laufen mit beiden Ständen, weil sie Marker und Werte selbst setzen. `Referenzlaeufe/LIESMICH.md` braucht beim Commit einen Nachtrag zu diesem Datenwechsel.

## Nebenbefund
In `%TEMP%` liegen 161 Kopieordner `epos-kerntest-*`, zusammen rund 11 GB, aus 18:02 bis 19:01. Sie tragen keine Besitzmarke, stammen also aus einem Codestand vor dem Leck-Fix, vermutlich den Läufen aus `z2`; von meinen Läufen ist keiner übrig. Der nächste Testlauf räumt sie erst nach zwei Stunden Schonfrist weg. Ich habe nichts gelöscht.

===== Bericht 4 (2026-09-23T17:34:08.533Z) =====

`e7c3` steht auf dem aktuellen origin-Stand `ffc27d18` (Z2, #451), und die Testdatenbank ist als E7c3/12 committet. Kern-Filter und Windows-Schale bauen mit 0 Fehlern, die gefilterten Tests sind grün. Nichts gepusht, Arbeitsbaum sauber.

## Spitze: `6ebb26b6`
| SHA | Commit |
|---|---|
| `716b6449` | Merge `39c63361`, ohne Konflikte |
| `c3246cb6` | Merge `ffc27d18`: Z2 kam um 19:27:54 kurz nach dem ersten Merge, deshalb ein zweiter statt einem |
| `6ebb26b6` | E7c3/12: Testdatenbank der Z2-Fassung auf Katalog-Generation 9 |

## Konflikte (zweiter Merge)
- **`Resource.resx` und `Resource.en-US.resx`:** beide Blöcke behalten, 43 Schlüssel aus E7c3 und 234 aus Z2. An der Naht fehlte wie beim ersten Nachzug ein `</data>`; ergänzt und geprüft.
- **`Referenzlaeufe/Kenndaten_Test.sqlite`:** die origin-Fassung übernommen (LFS `fc13e3a7…`), dann E7c3/12 darauf.

## Schlüsselzahl
- **Je Sprache 8.391 Einträge:** 8.114 aus `39c63361` + 43 aus E7c3 + 234 aus Z2. Keiner doppelt, jedes `<data>` mit genau einem `<value>`.
- **Zählweise:** Die 8.118 der Nachbarsitzung sind die Zählung mit `grep '<data name='`. Sie enthält vier Beispiele aus dem Dateikopf; nach dieser Zählung wären es jetzt 8.395.
- **Designer:** neu erzeugt und geprüft, 8.392 Blöcke, unverändert und wiederholbar.
- **Schemastand** bleibt 113.

## Testdatenbank
- **Neue Fassung:** LFS `9df1b7a5f04a4d45eb8b8a1ad62cdc7e0bf5b0a7c5500d7ee10fd531b960552b`, 67.751.936 Byte. Committet ist der LFS-Zeiger, das Objekt liegt lokal vor.
- **Zellvergleich gegen die origin-Fassung** (130 Tabellen, 10.496.533 Zellen): genau die fünf erwarteten Zellen.
  - Brennstoff 24 „Sonstige“: Hi 0 → 1 und Hs 0 → 1.
  - `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB`: Status GESICHERT → ABGEKUENDIGT.
  - Katalog-Marker 8 → 9.
- **Sonst unverändert:** Schema gleich, Integritätsprüfung ok, der Z2-Testkatalog ist unberührt. Ein zweiter Lauf des Werkzeugs pflegt nichts mehr.
- **Referenzlauf** gegen R12: 13/13 PASS, 399 von 399 CSV byte-gleich.

## Tests
Gefahren nach der tasklist-Regel; beim Start lief kein fremder Testprozess.

| Lauf | Ergebnis |
|---|---|
| Kern: E7c3-Klassen, alle Wachen, Migrations- und Schemastand-Tests, dazu Zapfprofil- und TWW-Tests wegen der neuen Datenbank | 840/840 |
| UI: Überlagerung, Optionsgruppe, BHKW-Dialog, Wachen, Zapfprofil | 213/213 |

Die Zapfprofil- und TWW-Fälle habe ich zusätzlich aufgenommen, weil sich die Datenbank geändert hat. Den vollen Lauf habe ich wie beauftragt nicht gefahren.

Statusnummer für E7c3 ist #452.