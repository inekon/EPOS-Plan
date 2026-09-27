

===== Bericht 1 (2026-09-23T23:38:19.016Z) =====

## Phase-1-Bericht E8c

Phase 1 ist fertig. Alle drei Punkte sind gebaut und je als eigener Commit abgelegt, der Arbeitsbaum ist sauber. Getestet ist noch nichts: `dotnet test` und der Referenzlauf warten auf „Tests freigegeben“. Zweig `e8c`, Basis fbe93de6, Stand b7dd5e1b. Kein Push, kein Merge, kein CI-Lauf.

| Commit | Punkt |
|---|---|
| e91617da | E8c/1 Bemessungstexte aller Bemessungsarten (E8b‑Q2) |
| 7c901689 | E8c/2 Gliederungsprobe nur mit Positionen des ersten Jahres (E8b‑Q3) |
| b7dd5e1b | E8c/3 U42-Kommentar in der Wirtschaftlichkeitsseite berichtigt |

**Punkt 1 (Q2)**
- **Bau:** `BemessungText` hat keine eigene Liste mehr, sondern liest den `BemessungKatalog`. Dort stehen die Texte für alle 18 Steuerwerte schon in beiden Sprachen (Ressourcen `BM_*`), am BHKW und Pufferspeicher mit eigener Beschriftung („je Liter“). Wort- und Tabellenbericht geben dafür die Komponente mit. „fester Betrag“ steht nur noch bei BETRAG sowie bei leerem oder unbekanntem Steuerwert, denn so rechnet auch der Rechenweg.
- Neu ist `BetriebskostenCtrl.Bemessungsfaktor`. Herleitung und Formelmappe Stufe 3 fragen jetzt beide dort, ob eine Art bemessen ist.
- **Formelmappe:** Menge × Satz hatte sie schon für alle 16 bemessenen Arten; sie ist nur auf den gemeinsamen Faktor umgestellt, das Verhalten ist gleich.
- **Test** `BemessungstexteAlleArtenTests`: je Art ein Fall (Text de/en, Herleitung, Formel, unveränderter Betrag), dazu Gewerk, leer/unbekannt und ein Wächter gegen alle Konstanten `DbWerte.BEMESSUNG_*`.
- **Zellvergleich** mit `vergleich.py` gegen die Basis, 16 Prüfgruppen (zwei neue: `hybarten` mit je einer Art, `hybluecke` mit einer echten Lücke): Geändert sind nur 54 Zellen der Spalte „Bemessung“. Beträge, Mengen, Sätze und Formeln sind unverändert, die Formelzahl ist gleich, ClosedXML meldet „abweichend 0“.

**Punkt 2 (Q3)**
- **Gewählt:** Die Probe vergleicht nur die Positionen des ersten Jahres. Die angesetzten Betriebskosten p. a. sind die Jahr‑1‑Zahl der Rechnung.
- **Begründung:** Die andere Variante (die Warnung nennt Startjahr und Differenz) würde weiter warnen, wo nichts fehlt. Eine echte Lücke ginge in der Aufzählung unter.
- **Bau:**
  - `KostenPositionNachweis.StartJahr` wird so gelesen wie in der Summenschleife.
  - Eine gemeinsame Probe gilt für Wort- und Tabellenbericht, Toleranz weiter 0,50 €.
  - Die Herleitungsspalte zeigt „ab Jahr X“, der Hinweistext über der Tabelle erklärt das in einem Satz.
- **Test** `BetriebskostenStartjahrGliederungTests`: Probe, Grenze, Herleitung und Umschlag, Nachweisliste gegen Summenschleife. In beiden Berichten warnt hybtest nicht mehr, die echte Lücke (Kostenart ZUSCHUSS) warnt weiter.
- **Zellvergleich:** hybtest und hybbk haben keine Warnzeile mehr. hybluecke warnt mit 1.800 gegen 2.300 €. Sonst hat sich nur der Hinweistext geändert.

**Punkt 3:** Nur der Kommentar ist berichtigt, sonst nichts.

**Abnahme Phase 1**
- Kern-Filter und Windows-Schale bauen mit 0 Fehlern, keine neuen Warnungen in den geänderten Dateien.
- Designer ist wiederholbar, SQL-Prüfer: 1.737 Texte, 0 Fundstellen.

**Schlüssel**
- Entfallen (de/en): die fünf `BEMESSUNG_*`, sie hatten nach Punkt 1 keine Aufgabe mehr.
- Neu: `WIRT_BK_AB_JAHR`.
- Text geändert: `WIRT_BK_ABWEICHUNG` („Positionen des ersten Jahres“) und `WIRT_BK_HINWEIS`.
- 8.613 Einträge.

**Abweichungen vom Auftrag**
- Für Punkt 1 gibt es keine neuen Schlüssel, weil die Katalogtexte (`BM_*`) benutzt werden. Sichtbar ändern sich dadurch „je Betriebsstunde“ zu „je Stunde“, „% of fuel cost“ zu „% of fuel costs“ und beim festen Jahresbetrag „fester Jahresbetrag“.
- Ein fester Jahresbetrag, an dem Menge und Satz eingetragen sind, bekommt keine Herleitung mehr. In der Testdatenbank kommt das nicht vor.
- Der Nachweisumschlag geht von Fassung 8 auf 9. Deshalb ist der Anker in `ErgebnisansichtTests` von 8 auf 9 geändert. Ältere gespeicherte Läufe lesen sich als „ab Jahr 1“ und warnen bis zum Neurechnen wie bisher.
- Die Herleitungsspalte nennt zusätzlich „ab Jahr X“.

**Offene Fragen**
1. Soll „je Stunde“ bleiben (Text des Katalogs, wie im Kostendialog)? Die Alternative wäre, `BM_STUNDE` in „je Betriebsstunde“ zu ändern. Empfehlung: bleiben lassen.
2. Befund: In der Herleitung heißen „je kWh Kapazität“ und „je m² Kollektorfläche“ an Betriebszeilen „kWh/a“ bzw. „m²/a“. Beide Arten sind im Betriebsraster nicht wählbar, betroffen sind nur Fremd- oder Altdaten. Empfehlung: lassen.

**Erledigt-Gründe (Register R‑E8b)**
- Q2 erledigt mit e91617da: Texte kommen aus dem Katalog, Herleitung und Formelmappe nutzen denselben Faktor, ein Wächter hält die Konstanten.
- Q3 erledigt mit 7c901689: Lesart b, nur Positionen des ersten Jahres. hybtest warnt nicht mehr, eine echte Lücke warnt weiter.

**Logbuchsätze** (Wiki `bericht`/`wirtschaftlichkeit`, Version beim Anwender erfragen)
- „Die Betriebskostentabelle des Wort- und Tabellenberichts nennt für jede Position ihre Bemessungsart.“
- „Positionen mit späterem Startjahr tragen dort ‚ab Jahr …‘ und lösen keinen Hinweis auf eine unvollständige Gliederung mehr aus.“

Phase 2 (voller Testlauf, Referenzlauf 13/13 gegen R13) wartet auf „Tests freigegeben“.

Dateien:
- C:\Waermeplan\EPOS-Plan\.claude\worktrees\e8c\EPOS.Kern.Tests\BemessungstexteAlleArtenTests.cs
- C:\Waermeplan\EPOS-Plan\.claude\worktrees\e8c\EPOS.Kern.Tests\BetriebskostenStartjahrGliederungTests.cs
- Messprogramm und Messstände (`basis`, `neu1`, `neu2`, `vergleiche_alle.sh`): C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e8c\

===== Bericht 2 (2026-09-23T23:49:44.697Z) =====

## Phase-2-Bericht E8c

Phase 2 ist grün: 0 Fehler im vollen Testlauf, Referenzlauf 13/13 gegen R13. Zweig `e8c` steht auf **b18237c0**, der Arbeitsbaum ist sauber. Kein Push, kein CI-Lauf.

**Merge**
- Merge-Commit **b18237c0** führt `origin/ios_migration_september` (e513f05e) in e8c zusammen, mit Opus-Trailer. Es gab keinen Konflikt.
- **Ressourcen:** Beide Seiten sind erhalten. Die fünf `BEMESSUNG_*` bleiben gestrichen, `WIRT_BK_AB_JAHR` bleibt. Damit hat jede Sprache 8.709 Schlüssel (8.713 − 5 + 1).
  - Beide Dateien sind wohlgeformt, die Schlüsselmengen gleich, ohne Dubletten; die Nähte `</data>` sind in Ordnung.
  - Der neu erzeugte Designer ist gleich dem Mergestand, ein zweiter Lauf ändert nichts.
- **Seite:** In `WirtschaftlichkeitSeite.razor` stehen mein U42-Kommentar und die Assistenten-Wahlen der Nachbarsitzung nebeneinander.
- **Testdatenbank:** Jetzt 8a3bebaf, Schemastand 114.

**Prüfungen**

| Prüfung | Ergebnis |
|---|---|
| Kern-Filter | 0 Fehler |
| Windows-Schale | 0 Fehler |
| Die zwei neuen Testklassen | 33/33 (21 + 12) |
| Betroffene Klassen einzeln | 183/183 |
| Voller Lauf | 12.123 bestanden, 0 Fehler, 1 übersprungen |
| Referenzlauf gegen R13 | 13/13 PASS, 4.145.687 Werte innerhalb der Toleranz |
| SQL-Prüfer | 1.750 SQL-Texte, 0 Fundstellen |

Der volle Lauf nach Projekten:

| Projekt | bestanden | Fehler | übersprungen |
|---|---|---|---|
| EPOS.Kern.Tests | 5.481 | 0 | 0 |
| EPOS.UI.Tests | 5.705 | 0 | 0 |
| KiKern.Tests | 524 | 0 | 0 |
| SpeicherEngine.Tests | 386 | 0 | 0 |
| SpeicherPlanung.Tests | 27 | 0 | 1 |

- Vor jedem `dotnet test` habe ich `tasklist` geprüft. Vor dem Merge lief kurz ein fremder testhost; vor allen Testläufen lief keiner.
- Die Testdatenbank ist nach Referenzlauf und SQL-Prüfer unverändert, es liegen keine Nebendateien.

**Zellvergleich wiederholt**
- Gerechnet auf dem gemergten Stand und mit dem Stand vor dem Merge verglichen: alle 15 Prüfgruppen sind gleich, in den Zellen wie in der Gliederung des Wortberichts.
- Die Wirkung von E8c gegenüber der Basis fbe93de6 bleibt damit genau wie in Phase 1 berichtet:
  - 54 Zellen der Spalte „Bemessung“ geändert.
  - hybtest und hybbk haben keine Warnzeile mehr.
  - hybluecke warnt weiter, mit 1.800 gegen 2.300 €.
  - Beträge, Mengen, Sätze und Formeln sind unverändert.

**Abweichungen**
- Es kamen 30 statt 19 Commits herein; mitgezählt sind die Commits von KU2 Welle 2 und die Papiere zu E8b.
- Berichtigung zu Phase 1: Es sind 15 Prüfgruppen (13 aus E8b plus 2 neue), nicht 16. Die 54 Zellen stimmen.
- Weiter gilt aus Phase 1: Der Nachweisumschlag steht jetzt auf Fassung 9, deshalb ist der Anker in `ErgebnisansichtTests` von 8 auf 9 geändert.

e8c liegt 4 Commits vor origin (E8c/1–3 und der Merge). Die offenen Fragen aus Phase 1 bleiben: „je Stunde“ lassen (Empfehlung) und der Einheitenbefund an Alt-/Fremddaten.

Messstände und Protokolle: C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e8c\ (`voll_lauf.txt`, `ref_vergleich.txt`, `vergleich_neu2_neu3.txt`, Ordner `neu3`)