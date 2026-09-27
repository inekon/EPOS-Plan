

===== Bericht 1 (2026-09-23T05:09:18.257Z) =====

## Zwischenbericht E7b, Phase 1

Phase 1 ist fertig. Die Kern-Projektmappe und die Windows-Schale bauen mit 0 Fehlern. `dotnet test` und der Referenzlauf sind nicht gelaufen, sie gehören zu Phase 2. Den Tarifstrukturdialog habe ich nicht entfernt, sondern auf das Rollenmodell reduziert. Das ist die wichtigste Abweichung, dazu steht unten eine offene Frage.

### Commits auf `e7b`
Basis ist c4ef252d. Nichts ist gepusht.

| Commit | Inhalt |
|---|---|
| 1b3797a3 E7b/1 | Die Strommatrix führt keine Tarifzonen mehr. Ein aktiver Zonensatz rechnet nicht mehr und bekommt einen Hinweis am Ergebnis. Pro Projekt wird eine Jahreszeile gespeichert. Word- und Excel-Tafel nachgezogen. |
| 23fd7c7e E7b/2 | Schemaschritt 103: drei Staffelspalten am Stromträger, dazu die Datenübernahme. Zielversion ist 103. Migrationstest. |
| 4d0004a1 E7b/3 | Die Staffel rechnet im Kern: `KostenEmissionRechner`, Speicherauslegung, Variantenkopie. |
| d263b1cc E7b/4 | Die Kostenverwaltung pflegt die Staffel (Energieträger-Karte, Hülle, KI-Felder). |
| beeb3f47 E7b/5 | Der Tarifdialog kennt nur noch das Rollenmodell. Der Einstieg „Strombezug…“ entfällt auf der Seite und im BHKW-Dialog. Ressourcen und Tests nachgezogen. |
| 0780c8a9 E7b/6 | Neue `StromMatrixOhneZonenTests`. In den Anker-Tests steht als Kommentar: alt = neu. |
| 9a89aa28 E7b/7 | Nur ein Kommentar in einem Test. |

### Was je Punkt gebaut ist
1. **HT/NT gestrichen:**
   - Die Matrix führt nur noch Jahressummen und Lastbilder. Vom Tarif nimmt sie nur die Winterspanne.
   - `LadeTarif` und `SpeichereTarif` lesen und schreiben die 13 Zonenspalten nicht mehr.
   - Ein alter Stand mit Zonenzeilen wird vom Leser summiert. Ein neuer Lauf ersetzt ihn, Schritt 103 fasst ihn zusammen.
2. **Staffel verlegt:**
   - Neue Gruppe „Leistungspreis-Staffel“ beim Stromträger des Projekts. Geschrieben wird über `EnergietraegerPreisCtrl`.
   - Bemessen wird an der Viertelstundenspitze. Eine gepflegte Staffel hat Vorrang vor dem Leistungspreis und der Saisonreihe des Stromträgers.
3. **Dialog abgekündigt, nur teilweise:** Zonen, HT-Fenster, Modellwahl, Staffel und die Sichten „Strombezug“ und „Komplett“ sind weg. Übrig bleibt das Rollenmodell, erreichbar über die Sprünge aus dem BHKW- und dem PV-Dialog. Eine Zeile in `Menuetabelle.cs` gab es nicht.
4. **Anker:** unverändert, siehe A/B.
5. **Ressourcen:** Den Designer habe ich schon geschrieben, weil der Build die neuen Schlüssel braucht. SQL-Prüfer und Designer-Prüflauf folgen in Phase 2.
6. **Testdatenbank:** nicht migriert, nicht committet.

### A/B
Gemessen auf Kopien im Scratchpad: vorher mit dem Kern c4ef252d auf Schemastand 101, nachher mit HEAD auf Kopien, die auf 103 migriert sind.

**Die 13 Basisprojekte**

| Weg | Größe | vorher | nachher | Grund |
|---|---|---|---|---|
| Ankerweg | alle 13 Projekte | 1023 −639.584,90 €; 1024 −2.896.359,13 €; 1030 −21.895.377,28 €; 99,00 €/a; Kaskade 13.000,00 € | bitgleich, 0 Differenzen | kein Tarifsatz, keine Staffel, keine Stundenreihen |
| Frischer Lauf | Kapitalwert 1024 | −2.796.650,73 € | gleich | – |
| Frischer Lauf | Kapitalwert 1030 | −31.141.242,71 € | Differenz 1·10⁻⁸ € | Matrixsumme in einem Durchlauf statt aus vier Teilsummen |
| Frischer Lauf | Matrixsummen, alle 13 Projekte | – | Abweichung nur in den letzten Nachkommastellen (relativ ≤ 2·10⁻¹³) | Matrixsumme in einem Durchlauf statt aus vier Teilsummen |
| Gespeicherte Matrix | Zeilen je Projekt | 4 Zonenzeilen | 1 Jahreszeile | keine Zonen mehr |
| Gespeicherte Matrix | geladene Werte | z. B. 1040 PV 2,272 / 1018 −14,733 / 1017 Bedarf 672,001 MWh | 2,273 / −14,732 / 672,000 MWh | ±0,001 MWh: eine gerundete Summe statt vier gerundeter |
| Altbestand 1018/1031 | Zeilen | 4 Zonenzeilen | 1 Zeile, gleiche Summe (−14,471 / −14,723 MWh) | Schritt 103 fasst zusammen |

**Proben mit gebauten Tarifsätzen** (die Testdatenbank selbst hat keinen Tarifsatz)

1030 mit aktivem Zonensatz (Staffel 1.500 kW / 60 / 90 €), Werte „Erwartet“:

| Größe | vorher | nachher | Grund |
|---|---|---|---|
| Energiekosten | 1.832.155,35 €/a | 1.760.606,20 €/a | Arbeitspreis und Grundpreis des Stromträgers (1.091.845 €) statt der Zonenpreise (1.163.394,15 €). Die Staffel bleibt gleich (135.990 €), steht jetzt aber als Leistungsanteil des Stromträgers. |
| KWK-Einspeiseerlös | 28,56 €/a | 0 | Die Zonen-Einspeisepreise entfallen, der KWK-Satz ist in 1030 nicht gepflegt. |
| Stromkosten Tarif | 1.299.384,15 € | leer | Kein Zonenpfad mehr. |
| Kapitalwert | −34.819.801,17 € | −33.551.896,03 € | +1.267.905,14 €, Folge der zwei Zeilen darüber |
| Leistungspreisquelle | Wert 90 €/(kW·a), Quelle „Tarifstruktur“ | 90 €/(kW·a), Quelle „Staffel des Stromträgers“ | Die Staffel ist umgezogen. |

- **1024 im Rollenmodell:** Die Werte bleiben gleich, bis auf die letzten Nachkommastellen: Kapitalwert −2.136.393,15 €, vermiedene Kosten −12.941,70 €. Der Satz bleibt aktiv, seine Staffel wird nicht übernommen.
- **Kein stiller Rückfall:** Auf einer nicht migrierten Kopie (Stand 101) steht am Ergebnis der Hinweis „Zeitzonentarif (HT/NT) entfällt …“. Gerechnet wird dann mit dem Stromträger ohne Staffel: Energiekosten 1.624.616,20 €, Kapitalwert −31.141.242,71 €.

### Schemaschritt 103
- **DDL:** drei DOUBLE-Spalten an `energy_project_settings` (`Leistungspreis_Staffelgrenze`, `Leistungspreis_Staffel1`, `Leistungspreis_Staffel2`).
- **DML in einer Transaktion, wiederholbar:**
  - Die Staffel eines aktiven Zonensatzes geht an den Stromträger jeder Version der Gruppe, nur in leere Spalten.
  - Aktive Zonensätze bekommen `Aktiv = 0`.
  - Die Zonenzeilen der Matrix werden zu einer Jahreszeile zusammengefasst.
- `SchemaStand.Zielversion` ist 103.
- **Merge-Reihenfolge: zuerst z0 (Schritt 102), dann e7b.** Sonst springt eine Datenbank von 101 auf 103 und lässt 102 später aus.
- **Live-DB (nur gelesen):** Stand 100, keine Tarifsätze. Projekt 1062 hat vier Zonenzeilen, die Schritt 103 zu einer Zeile mit 3,709 MWh zusammenfasst.

### Schlüssel (de und en gleich)
- **Neu (21):** `ETV_STAFFEL_*` (6), `KI_DLG_ET_STAFFEL_*_ERL` (3), `OPT_QUELLE_STAFFEL*` (4), `WIRT_MATRIX_TITEL/HERKUNFT/ZEITRAUM/JAHR/STUNDENSPITZE/STUNDENLAST`, `WIRT_HINWEIS_ZEITZONENTARIF`, `WIRT_TARIF_NACHWEIS_ZONEN`.
- **Geändert (5):** `WIRT_MATRIX_BEDARF_HINWEIS`, `TARIF_G_ZEITZONEN` (heißt jetzt „Winterspanne …“), `KI_DLG_TAR_WINTERVON_ERL`, `KI_DLG_TAR_WINTERBIS_ERL`, `KDLG_ERTRAG_FK7` (verwies auf die Einsp_*-Zonenpreise).
- **Gestrichen (38):**
  - Tarif-Zonen, HT-Fenster, Modellwahl, Staffel: `TARIF_*` (22)
  - KI-Erklärtexte dazu: `KI_DLG_TAR_*_ERL` (8)
  - alte Speicherauslegungs-Quellen: `OPT_QUELLE_TARIF*` (4)
  - Knöpfe „Strombezug…“: `WIRT_BTN_STROM_TARIF`, `BHW_BTN_STROMBEZUG`
  - dazu die drei alten Waisen `TARIF_BTN_SPEICHERN`, `KDLG_LP_STROM_TARIF`, `KDLG_LP_STROM_TARIF_BTN`

### Abweichungen vom Auftrag
- **Punkt 3 nur teilweise:** Dialog, Hülle und KI-Sicht bleiben verkleinert bestehen. Der Kern liest `Tab_ProjektTarif` weiter, aber nur die Spalten des Rollenmodells.
- **Kein Menüpunkt vorhanden:** Entfernt habe ich stattdessen den Knopf „Strombezug…“ auf der Wirtschaftlichkeitsseite und den Sprung im BHKW-Dialog.
- **Neue Spalten für die Staffel:** Keine bestehende Spalte kann eine zweistufige Staffel tragen.
- **Schritt 103 ohne 102 auf dem Zweig:** siehe Merge-Reihenfolge oben.

### Offene Fragen
- **E7b‑Q1 – was wird aus dem Tarifstrukturdialog?**
  - (a) Wörtlich ganz entfernen. Dann fällt auch das Rollenmodell (Differenzmethode, Etappe E5) weg, samt der Sprünge „BHKW-Tarif…“ und „Tarif…“.
  - (b) So wie jetzt gebaut: Nur das Zonenmodell fällt, das Rollenmodell bleibt.
  - (c) Das Rollenmodell in einem eigenen Auftrag verlegen und den Dialog danach entfernen.
  - **Empfehlung b.** Q11 hat nur „kein HT/NT“ entschieden. Zu a gehört ein eigener Entscheid, weil ein ganzer Rechenweg wegfiele (heute betrifft das allerdings keinen einzigen gespeicherten Tarifsatz).
- **E7b‑Q2 – Bemessung der Staffel:** Umgesetzt ist die Viertelstundenspitze statt der höchsten Stundenlast (wie beim Leistungspreis des Stromträgers). Bei 1030 ist die Zahl gleich, 2.011 kW. **Empfehlung: so bestätigen.**
- **E7b‑Q3 – Vorrang der Staffel:** Eine gepflegte Staffel ersetzt Leistungspreis und Saisonreihe, statt sich zu addieren. Gepflegt wird sie nur am Projekt, nicht im Katalog. **Empfehlung: so bestätigen.**
- **E7b‑Q4 – alte gespeicherte Ergebnisse:** Ergebnisse, die noch mit dem Zonentarif gerechnet wurden, bleiben stehen, bis jemand „Berechnen“ drückt. Die Statuszeile meldet sie nicht als veraltet.
  - (a) So lassen.
  - (b) Schritt 103 löscht sie.
  - (c) Hinweis in der Statuszeile.
  - **Empfehlung a**, weil keine vorhandenen Daten betroffen sind.

### Nebenbefunde
- `Tab_ProjektTarif` behält die 13 Zonenspalten, der Kern liest sie nicht mehr. Kandidat für einen späteren Aufräumschritt (DDL).
- Die Spalte `Tab_ErgebnisStromMatrix.Zone` enthält jetzt nur noch „Jahr“.
- Die Altmatrix von 1018 und 1031 hat einen negativen Netzbezug, der als KWK-Einspeisung gespiegelt ist. Das stammt aus dem Bestand vom 21.08. und hat mit E7b nichts zu tun.

### Verweise auf den Dialog (nur aufgelistet, nicht geändert)
- **Wiki „Wirtschaftlichkeit“:**
  - Z. 12 (fussleiste): nennt „Strombezug…“
  - Z. 93 (strombezug): der ganze Absatz beschreibt den entfallenen Dialogteil
  - Z. 75 (gespeicherte-laeufe): „aktive Tarifstruktur“ heißt jetzt nur noch Rollentarif
  - Z. 57 (energiekosten): Staffel des Stromträgers ergänzen
- **Wiki „Kosten“:** Z. 95 (preiswirkung): neue Staffel-Gruppe beschreiben.
- **Wiki „Hilfe-Assistent“:** Z. 72 nennt die Tarifstruktur, das bleibt gültig.
- **`help_mapping.txt` Z. 265:** `Form_Tarifstruktur` zeigt auf den Anker `#strombezug`.

### Vorschlag für die erledigt-Gründe
- **Register Q11:** „erledigt mit #438 (E7b): Zeitzonentarif entfällt (Matrix ohne Zonen, Schritt 103 schaltet Zonensätze ab, Hinweis bei nicht migriertem Satz); zweistufige Staffel am Stromträger der Kostenverwaltung, bemessen an der Viertelstundenspitze; Tarifdialog auf das Rollenmodell reduziert, Einstieg ‚Strombezug…‘ entfällt (E7b‑Q1 offen).“
- **Analysepapier § 5, E7b:** „erledigt mit #438: Weg 3 und Weg 2 aus Nach #291, Schritt 103, Anker unverändert; Punkt 3 in Lesart b.“

### Logbuchsätze (Versionsnummer bitte nennen)
- Der Zeitzonentarif (Hoch- und Niedertarif, Winter und Sommer) ist nicht mehr vorhanden. Der Strombezug wird mit den Preisen des Stromträgers aus der Kostenverwaltung bewertet.
- Die zweistufige Leistungspreis-Staffel wird in der Kostenverwaltung beim Stromträger des Projekts gepflegt und an der Viertelstundenspitze des Netzbezugs bemessen.
- Der Knopf „Strombezug…“ auf der Wirtschaftlichkeitsseite und im Dialog BHKW-Wirtschaftlichkeit ist nicht mehr vorhanden.

### Für Phase 2
- Die Wache `TestdatenbankSchemastandWacheTests` bleibt rot, bis die Testdatenbank auf 103 migriert ist. Wahrscheinlich gilt dasselbe für die Tests der Auslieferungsvorlage.
- Den SQL-Prüfer lasse ich gegen eine migrierte Kopie laufen.
- Am Simulationsweg ist nichts geändert, deshalb erwarte ich beim Referenzlauf PASS ohne neues Einfrieren.
- Wochennutzung liegt bei 66 %, Fable bei 75 %.

Ich warte auf „Tests freigegeben“.

===== Bericht 2 (2026-09-23T05:20:41.332Z) =====

## Schlussbericht E7b, Phase 2

Phase 2 ist grün. Rot bleiben nur die zwei erwarteten Stellen, beide wegen der Repo-Testdatenbank auf Stand 101. Der Referenzlauf ist PASS und byte-gleich. Den Bau habe ich wie empfohlen gelassen (E7b‑Q1 b, Q2/Q3 bestätigt, Q4 a), dafür war keine Code-Änderung nötig. HEAD ist e06d7eae, nichts ist gepusht.

**Merge:** a9be7581 „E7b: Arbeitszweig 435b9810 in e7b zusammengeführt“ mit Opus-Trailer. Es kamen nur die zwei Papier-Commits herein, ohne Konflikt.

**Tests**
- Der gefilterte Lauf der neuen und geänderten Klassen war zuerst dreimal rot. Alle drei Fehler lagen in meinen eigenen Testerwartungen, nicht im Programm; berichtigt in E7b/8:
  - Der Zeitstempel wurde als Text verglichen und hing damit an der Kultur des Läufers. Jetzt wird er als Zeitpunkt verglichen.
  - Ein Test suchte das Wort „Staffelgrenze“. Das steht zu Recht im Hinweis zu den vier Leistungsstufen des Rollenmodells. Jetzt prüft der Test die alten Beschriftungen der zweistufigen Staffel.
- Danach: Kern 130/130, UI 387/387.
- Voller Lauf `WP-Plan.Kern.slnf`:

| Testprojekt | Ergebnis |
|---|---|
| KiKern | 524/524 |
| SpeicherEngine | 386/386 |
| SpeicherPlanung | 27/28, 1 übersprungen (Bestand) |
| EPOS.UI | 5.228/5.228 |
| EPOS.Kern | 4.630/4.631 |

- Der einzige rote Fall im Kern ist `TestdatenbankSchemastandWacheTests`, wie erwartet.
- Werkzeugtests aus der CI: Formularkarte 124/124. Auslieferungsvorlage 12 von 19 rot, alle mit „Schemastand 101 (erwartet 103)“.
- **Gegenprobe:** Ich habe die Repo-Testdatenbank im Worktree kurz durch eine auf 103 migrierte Kopie ersetzt. Damit waren die Auslieferungsvorlage 19/19 und die Schemastand-Wache 1/1 grün. Danach habe ich die Originalbytes zurückkopiert; die Prüfsumme stimmt, der Worktree ist sauber.

**Referenzlauf:** alle 13 Projekte auf einer außerhalb des Repos auf 103 migrierten Kopie, gegen `2026-09-22_R11_Bestandsbefunde`. Ergebnis `GESAMT: PASS` mit 3.882.737 Werten, 13/13 byte-gleich. Die Basis muss nicht neu eingefroren werden.

**Designer-Prüflauf:** unverändert, 7.582 Einträge ohne Abweichung, wiederholbar.

**SQL-Prüfer** gegen die auf 103 migrierte Kopie: Selbsttest 35 Anweisungen ohne Abweichung. 1.579 SQL-Texte, 0 Fundstellen (287 dynamisch, 1.292 in Ordnung).

**Bauten:** Kern-Projektmappe und Windows-Schale bauen mit 0 Fehlern.

**Commits auf `e7b`** (seit c4ef252d)

| Commit | Inhalt |
|---|---|
| 1b3797a3 | E7b/1 Strommatrix ohne Tarifzonen |
| 23fd7c7e | E7b/2 Schemaschritt 103 |
| 4d0004a1 | E7b/3 Staffel rechnet am Stromträger |
| d263b1cc | E7b/4 Staffel in der Kostenverwaltung |
| beeb3f47 | E7b/5 Tarifdialog nur noch Rollenmodell, „Strombezug…“ entfällt |
| 0780c8a9 | E7b/6 Tests Strommatrix ohne Zonen, Anker unverändert |
| 9a89aa28 | E7b/7 Kommentar in einem Test |
| a9be7581 | Merge des Arbeitszweigs |
| e06d7eae | E7b/8 zwei Testerwartungen berichtigt |

**Für den Nachzug nach z0:** Sobald Schritt 102 im Arbeitszweig liegt, merge ich ihn erneut in e7b. Dann prüfe ich an vier Stellen, dass 102 vor 103 steht: `SchemaMigration` (Konstante und `Schritt`-Liste), `TestDatenbank.SchemaNachziehen`, `Werkzeuge/Testdatenbankschema` und die Kommentare in `SchemaStand`. Danach wiederhole ich Migrationstest und vollen Lauf.

Wochennutzung liegt bei 67 %, Fable bei 76 %.

===== Bericht 3 (2026-09-23T05:57:47.177Z) =====

## Kurzbericht E7b/9

E7b/9 ist committet als **6e722688**, nichts gepusht. Der Q4-Umbau steht: Schritt 103 löscht die Zonensätze und verwirft die mit ihnen gerechneten Ergebnisse. Zu Q3 war kein Umbau nötig. Kern-Filter und Windows-Schale bauen mit 0 Fehlern. Ein `dotnet test` ist nicht gelaufen; der Migrationstest ist umgeschrieben, aber noch nicht ausgeführt.

**Was Schritt 103 jetzt tut** (DDL, danach der Datenteil in einer Transaktion, wiederholbar):
1. **Staffel übernehmen**, wie bisher: aus jedem aktiven Zonensatz, der einen Zonen-Bezugspreis und einen Staffelpreis trägt. Sie geht an den Stromträger jeder Version der Gruppe, und nur in leere Spalten.
2. **Zonensätze löschen**: jeder Satz, dessen `Tarif_Modus` nicht `ROLLEN` heißt, aktiv oder nicht, fällt aus `Tab_ProjektTarif`. Das ist dieselbe Grenze, die der Kern zwischen Rollen- und Zonensatz zieht. Rollensätze bleiben.
3. **Ergebnisse verwerfen.** Erkannt werden sie so: eine gespeicherte Ergebniszeile mit gefüllter Spalte `StromkostenTarif`, deren Projekt zur Gruppe eines Zonensatzes gehört (Stamm und Varianten).
   - Die Spalte füllt nur der Tarifweg. Die Gruppenbedingung schließt deshalb Rollenergebnisse aus, und Ergebnisse mit Flat-Preisen haben die Spalte leer.
   - Verworfen wird der ganze gespeicherte Lauf des Projekts: alle drei Szenarien, Sensitivität und Strommatrix.
4. **Matrix zusammenfassen**: die übrigen Zonenzeilen werden je Projekt zu einer Jahreszeile.

Die Nachprüfung am Ende verlangt, dass kein Zonensatz und keine Zonenzeile mehr steht. Der Hinweis `WIRT_HINWEIS_ZEITZONENTARIF` bleibt für eine Datenbank, die noch nicht migriert ist.

**E7b‑Q3:** Satz, Saisonreihe und Staffel schlossen einander schon aus. Die Rangfolge ist Staffel, dann Saisonreihe, dann Satz, im Rechner für Strom wie für Brennstoff; nirgends wird addiert. Belegt ist das mit dem neuen Test `LeistungspreisStaffelTests.Satz_Saisonreihe_und_Staffel_schliessen_einander_aus`. Er prüft nacheinander Satz (100.550 €), Satz mit Reihe (72.000 €, nicht die Summe) und alles zusammen (nur die Staffel, 135.990 €). Die Netzkosten ohne Leistungsanteil sind in allen Fällen gleich.

**Tests umgestellt, nicht ausgeführt:**
- Der Migrationstest prüft jetzt das Löschen (vier Zonensätze, einer davon inaktiv; der Rollensatz bleibt) und die Wiederholbarkeit.
- Ein neuer Fall für die Ergebnisse erwartet 9 verworfene Zeilen in 1019, 1024 und 1026 samt Sensitivität und Matrix. Flat-Ergebnisse derselben Gruppen, der Stamm ohne Stromträger und das Rollenergebnis 1039 sollen bleiben.

**1030-Probe mit gebautem Zonensatz** (Kopie des Vorher-Stands, mit dem neuen Werkzeug migriert):

| | vorher (Stand 101) | nach Schritt 103 |
|---|---|---|
| Tarifsatz 1030 (Zonen, aktiv) | vorhanden | gelöscht |
| Tarifsatz 1024 (Rollen) | vorhanden | bleibt |
| Staffel am Stromträger 1030/60 | – | 1.500 kW / 60 / 90 € |
| Gespeicherte Ergebnisse 1030 (StromkostenTarif 1.299.384,15 €) | 3 Zeilen, 4 Matrixzeilen | verworfen, keine Matrix |
| Gespeicherte Ergebnisse 1024 (Rollen, 128.541,70 €) | 3 Zeilen | bleiben, Matrix wird Jahreszeile |
| Energiekosten „Erwartet“ | 1.832.155,35 €/a | 1.760.606,20 €/a |
| Leistungsanteil (Staffel) | 135.990 € im Tarifweg | 135.990 € am Stromträger |
| KWK-Einspeiseerlös | 28,56 €/a | 0 |
| Kapitalwert „Erwartet“ | −34.819.801,17 € | −33.551.896,03 € |
| Kapitalwert 1024 (Rollen) | −2.136.393,15 € | unverändert |

Die Zahlen nach dem Schritt stimmen mit der früheren Messung überein, als der Zonensatz nur abgeschaltet wurde. Nur der Zustand der Datenbank ist jetzt ein anderer.

**Außerdem:** Der SQL-Prüfer ist gegen eine auf 103 migrierte Kopie gelaufen: 1.583 SQL-Texte, 0 Fundstellen. Er ist kein `dotnet test`.

**Für den Nachzug** („Nachzug freigegeben“): `git merge ios_migration_september`, und im selben Zug wird der Staffel-Schritt von 103 zu 104. Das betrifft Konstante, Methode, Eintrag in der Schrittliste, den Absatz in `SchemaStand`, `TestDatenbank.SchemaNachziehen`, `Werkzeuge/Testdatenbankschema`, Kommentare, Tests und die Zielversion 104. Danach prüfe ich die Reihenfolge 101 → 102 → 103 → 104 an den vier Stellen und lasse Migrationstest und vollen Lauf laufen.

Wochennutzung liegt bei 69 %, Fable bei 78 %.

===== Bericht 4 (2026-09-23T06:29:20.899Z) =====

## Schlussbericht E7b nach dem Nachzug

Der Nachzug ist fertig: Merge des Arbeitszweigs 89bf314c in e7b, danach ist der Staffel-Schritt 104. Die einzigen roten Stellen sind die zwei erwarteten; beide werden auf einer außerhalb des Repos auf 104 migrierten Kopie grün. Referenzlauf 13/13 PASS, byte-gleich. HEAD ist **9b9eaaa5**, nichts gepusht, der Worktree ist sauber.

**Commits** (seit c4ef252d; die Papier-Commits des Arbeitszweigs nicht mitgezählt)

| Commit | Inhalt |
|---|---|
| 1b3797a3 … 9a89aa28 | E7b/1 bis /7 |
| a9be7581 | Merge 435b9810 |
| e06d7eae | E7b/8, zwei Testerwartungen berichtigt |
| 6e722688 | E7b/9, Q4-Umbau und Q3-Nachweis |
| **d93488ef** | Merge „E7b: Arbeitszweig 89bf314c in e7b zusammengeführt“ |
| **9b9eaaa5** | E7b/10, Schritt 103 → 104 |

- **Merge-Konflikte:** vier Dateien (`SchemaMigration`, `SchemaStand`, `TestDatenbank`, `Testdatenbankschema`). Aufgelöst jeweils als Tww-Teil (103) plus unser Teil. Ressourcen und Designer hat git selbst zusammengeführt; der Designer-Prüflauf bestätigt sie.
- **E7b/10:** Konstante `SCHRITT_104_ZEITZONENTARIF_ABLOESUNG = 104`, Methode, Listeneintrag nach 103, Notiz „104:“, `SchemaKatalog.Schritt104_LeistungspreisStaffel`, `SchemaStand.Zielversion = 104` samt eigenem Absatz, Kommentare und Tests. Die Kette 101 Gebäudespalten → 102 KWKG → 103 Tww → 104 Staffel steht an allen vier Stellen in dieser Reihenfolge: Migration, SchemaStand, TestDatenbank, Werkzeug.

**Tests** (kein anderer Testprozess lief)

| Lauf | Ergebnis |
|---|---|
| Migrationstest `ZeitzonentarifAbloesungTests` | 4/4 |
| Gefilterte Klassen (E7b-Klassen sowie Tww-, Gebäude- und KWKG-Schema) | Kern 151/151, UI 389/389 |
| Voller Lauf: KiKern | 524/524 |
| Voller Lauf: SpeicherEngine | 386/386 |
| Voller Lauf: SpeicherPlanung | 27/28, 1 übersprungen |
| Voller Lauf: EPOS.UI | 5.278/5.278 |
| Voller Lauf: EPOS.Kern | 4.780/4.781, rot nur `TestdatenbankSchemastandWacheTests` |
| Formularkarte | 124/124 |
| Auslieferungsvorlage (Repo-Datenbank auf 103) | 14 von 26 rot, alle „Schemastand 103 (erwartet 104)“ |
| Gegenprobe, Repo-Datenbank kurz durch 104-Kopie ersetzt | Auslieferungsvorlage 26/26, Schemastand-Wache 1/1 |

Nach der Gegenprobe sind die Originalbytes zurück; die Prüfsumme stimmt.

**Referenzlauf:** alle 13 Projekte auf der 104-Kopie gegen `2026-09-22_R11_Bestandsbefunde`: `GESAMT: PASS` mit 3.882.737 Werten, 13/13 byte-gleich. Die Basis muss nicht neu eingefroren werden.

**Designer:** unverändert, 7.683 Einträge ohne Abweichung.

**SQL-Prüfer** (104-Kopie): Selbsttest 35/0. 1.645 SQL-Texte, 0 Fundstellen.

**1030-Probe nach Schritt 104** (Probe von Stand 101 mit gebautem Zonensatz, durch 101 bis 104 migriert):
- Die Staffel 1.500 kW / 60 / 90 € geht an den Stromträger 1030/60.
- Der Zonensatz 1030 ist gelöscht; der Rollensatz 1024 bleibt aktiv.
- Die gespeicherten Ergebnisse von 1030 sind verworfen: 3 Zeilen (StromkostenTarif 1.299.384,15 €) samt Matrix. Die Ergebnisse von 1024 bleiben, seine Matrix wird eine Jahreszeile.
- Neu gerechnet, Szenario „Erwartet“:
  - 1030: Energiekosten 1.760.606,20 €/a, davon Leistungsanteil aus der Staffel 135.990 €. Einspeiseerlös 0, StromkostenTarif leer, Kapitalwert **−33.551.896,03 €** (vorher mit Zonentarif −34.819.801,17 €).
  - 1024 (Rollen): Kapitalwert −2.136.393,15 €, vermiedene Kosten −12.941,70 € (unverändert).
- Die Werte sind identisch mit dem Stand vor dem Nachzug (E7b/9); nur die Schemanummer ist jetzt 104.

**Für die Orchestrierung:** Die Repo-Testdatenbank steht auf 103. Nach der Migration auf 104 beim Merge in den Arbeitszweig werden Schemastand-Wache und Auslieferungsvorlage grün; die Gegenprobe oben hat das bereits gezeigt.

Wochennutzung liegt bei 72 %, Fable bei 81 %.