# E8c — Bemessungstexte aller Bemessungsarten, Gliederungsprobe mit den Positionen des ersten Jahres, U42-Kommentar (Protokoll, 24.09.2026)

Statuszeile #460 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Mini-Welle E8c nach E8b des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.6 (Stufe 3) und § 3.4 (Betriebskosten); Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`,
Kategorie 8 (Zone „Bericht und Ausgabe", Kasten „Der Nachweisumschlag und seine Grenzen", Ressourcentafel) und
Anhangzeile U42. Anlass: der Anwender am 23.09.2026, „Fragen aus E8b: Empfehlung" — E8b‑Q2 (Bemessungstexte) und
E8b‑Q3 (Warnung bei Startjahr-Positionen, Lesart b) als eigene kleine Aufträge, dazu der überholte Kommentar zu U42
(Nach #454 (d)). Die beiden Fragen stehen im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
unter R‑E8b, die zwei neuen Fragen dieser Welle unter R‑E8c. Vorgänger:
[`E8b_Formelmappe_AnhangE_D_Protokoll.md`](E8b_Formelmappe_AnhangE_D_Protokoll.md). Zweig `e8c` von `fbe93de6`
(`origin`, Schemastand 113, Basis R13), zwei Phasen; Phase 1: `e91617da` (E8c/1), `7c901689` (E8c/2), `b7dd5e1b`
(E8c/3); Phase 2: Merge `b18237c0` (`origin` = `e513f05e`, 30 Commits, ohne Konflikt). Merge `9ab55946` auf dem
Hilfszweig `pm7` über `e513f05e` (16 Dateien, +854/−135; der Baum gleicht `b18237c0`). Opus 5.5 im Worktree
`.claude/worktrees/e8c`. **Keine Rechenwirkung, kein Schemaschritt** — `SchemaStand.Zielversion` = 114, gekommen mit
dem Nachzug (Kühlung KU2).

## Befund vor der Welle

- **`WirtschaftlichkeitZeilen.BemessungText` führte eine eigene Liste** mit vier Arten (`PROZENT_INVESTITION`,
  `PROZENT_BRENNSTOFFKOSTEN`, `EUR_PRO_H`, `EUR_PRO_KWH`, je eine Ressource `BEMESSUNG_*`) und nannte jede andere
  „fester Betrag" — in der Spalte „Bemessung" der Tabelle „Betriebskosten nach Kostenarten" beider Berichte, seit #455
  auch neben einer Menge-×-Satz-Formel der Formelmappe (E8b‑Q2). Kostendialog und Kostenseite lesen ihre Texte aus dem
  `BemessungKatalog` (Ressourcen `BM_*` für alle 18 Steuerwerte, mit gewerkeigener Beschriftung).
- **Die Herleitung** ließ nur einen leeren Steuerwert und `BETRAG` als fest gelten; ein fester Jahresbetrag mit
  gepflegter Menge bekam eine Herleitung Menge × Satz, nach der nicht gerechnet wird.
- **Die Probe der Gliederung** hielt in Wort- und Tabellenbericht — je in eigener Kopie — die Summe **aller**
  Positionen gegen die angesetzten Betriebskosten p. a. (`BetriebskostenJahr`, die Jahr‑1-Zahl der Rechnung). Eine
  Position mit Startjahr ≥ 2 (KD6) steht in der Summenschleife im Topf „ab Jahr", nicht in dieser Zahl; die Prüfgruppe
  hybtest (Wartung BHKW 1.800 €, Wartung Kessel 600 € ab Jahr 6) warnte deshalb 2.400 gegen 1.800 €, obwohl nichts
  fehlte (E8b‑Q3). Der Nachweisumschlag (Fassung 8) trug kein Startjahr je Position.
- **Der Kopfkommentar** der Darstellung „ValERI-Bewertung" in `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor`
  sagte „Das Zahlungsstrombild (U42) ist nicht gebaut — Konzeptentscheid offen"; das Bild steht seit #454 unter der
  Jahrestafel von Block 2.

## Gebaut — Phase 1 (E8c/1 bis E8c/3)

- **Bemessungstexte aus dem Katalog (E8c/1, `e91617da`, E8b‑Q2).** `BemessungText(steuerwert, komponente)` hat keine
  eigene Liste mehr: Der Text kommt aus dem `BemessungKatalog` — derselben Quelle wie Kostendialog und Kostenseite,
  in beiden Sprachen, samt der gewerkeigenen Beschriftung („je Liter" am Pufferspeicher, „je kW elektr. Leistung" am
  BHKW); Wort- und Tabellenbericht geben dafür die Komponente der Position mit. „fester Betrag" steht nur noch bei
  `BETRAG` und bei leerem oder unbekanntem Steuerwert — so rechnet auch der Rechenweg (`BetriebskostenCtrl.Betrag`:
  „nie stillschweigend 0"). Neu ist `BetriebskostenCtrl.Bemessungsfaktor`: 1 für eine Art „Satz je Einheit", 0,01 für
  eine Prozentart, `null` für eine feste Art — die Antwort des einen Rechenwegs für Menge 1, Satz 1 und Betrag 0.
  Herleitung und Formelmappe (Stufe 3) fragen beide dort; eine neue Art im Rechenweg kann nicht an ihnen
  vorbeilaufen. Die Formelmappe rechnete Menge × Satz schon für alle 16 bemessenen Arten; sie ist nur auf den
  gemeinsamen Faktor umgestellt, das Verhalten ist gleich. Die fünf Ressourcen `BEMESSUNG_*` entfallen, der Designer
  ist neu erzeugt.
- **Gliederungsprobe mit den Positionen des ersten Jahres (E8c/2, `7c901689`, E8b‑Q3, Lesart b).** Gewählt ist die
  erste der beiden Varianten der Lesart: Die Probe vergleicht nur die Positionen, die im ersten Jahr zahlen — die
  angesetzten Betriebskosten p. a. sind die Jahr‑1-Zahl der Rechnung. **Warum nicht die zweite** („die Warnung nennt
  Startjahr und Differenz"): Sie warnte weiter, wo nichts fehlt, und eine echte Lücke ginge in ihrer Aufzählung unter.
  Gebaut: `KostenPositionNachweis.StartJahr` (≥ 2, sonst leer), gelesen wie in der Summenschleife;
  `WirtschaftlichkeitZeilen.LaeuftImErstenJahr` (dieselbe Grenze `start > 1`), `GliederungAbweichung` — **eine** Probe
  für Wort- und Tabellenbericht, Toleranz weiter 0,50 € — und `HerleitungZeile`: Die Herleitungsspalte nennt hinter
  Herleitung oder Szenariokennzeichen „ab Jahr X", der Hinweistext über der Tabelle erklärt das in einem Satz. Der
  Nachweisumschlag geht auf **Fassung 9**: Das Startjahr steht nur, wo es ≥ 2 ist — eine Position ohne Startjahr
  schreibt sich Zeichen für Zeichen wie in Fassung 8; ein älterer Umschlag liest seine Positionen als „ab dem ersten
  Jahr" und vergleicht dort wie zuvor alle Positionen. Der Bericht rechnet frisch; ein gespeicherter Stand greift nur
  im Rückfall, wenn die Rechnung des Berichtslaufs scheitert.
- **U42-Kommentar (E8c/3, `b7dd5e1b`).** Der Blockkommentar sagt jetzt, dass unter der Jahrestafel das
  Zahlungsstrombild desselben Standes im selben Szenario steht (Zahlungsreihenteil); sonst keine Codeänderung.
- **Ressourcen** (de und en): **fünf entfallen** (`BEMESSUNG_BETRAG`, `BEMESSUNG_PROZENT_INVESTITION`,
  `BEMESSUNG_EUR_PRO_H`, `BEMESSUNG_EUR_PRO_KWH`, `BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN`), **einer neu**
  (`WIRT_BK_AB_JAHR` „ab Jahr {0}" / „from year {0}"), **zwei neu gefasst** (`WIRT_BK_ABWEICHUNG` nennt „die Summe der
  Positionen des ersten Jahres", `WIRT_BK_HINWEIS` erklärt das Startjahr); für Punkt 1 keine neuen Schlüssel — die
  Katalogtexte `BM_*` gelten. Je Sprache 8.612 Einträge auf `b7dd5e1b`, **8.709** nach dem Nachzug (8.713 − 5 + 1).

## Phase 2 und der Nachzug

- **Merge `b18237c0`** holt `origin` = `e513f05e` — 30 Commits der Nachbarsitzungen: Dialog Design #458 (Wächter der
  Maskenabdeckung) und #459 (Schloss der Auslieferungssätze), Kühlung KU2 Wellen 1 und 2 mit Schemaschritt 114
  (`SCHRITT_114_KUEHLUNG_ERZEUGER`, KU‑S3), die Entscheid-Papiere zu E8b‑Q1 bis Q6 (`46023235`). Ohne Konflikt; beide
  `.resx` mit beiden Seiten (die fünf `BEMESSUNG_*` bleiben gestrichen, `WIRT_BK_AB_JAHR` bleibt; gleiche
  Schlüsselmengen de/en, keine Doppel, die Nähte `</data>` geprüft), `Resource.Designer.cs` aus den zusammengeführten
  `.resx` neu erzeugt (unverändert gegen den Mergestand, ein zweiter Lauf ändert nichts); in
  `WirtschaftlichkeitSeite.razor` stehen der berichtigte Kommentar und die Änderung der Nachbarsitzung beide; die
  Testdatenbank ist die Fassung von `origin` (LFS `8a3bebaf…`, Schemastand 114).
- **Merge `9ab55946`** auf dem Hilfszweig `pm7` über `e513f05e` (16 Dateien, +854/−135 — genau der Umfang von E8c/1 bis
  E8c/3; der Baum gleicht `b18237c0`).

## Fragen aus der Welle

Der Phase‑1-Bericht nennt zwei Fragen. **Beide sind beim Anwender offen**; gebaut ist jeweils Lesart a. Sie stehen im
Entscheidungsregister als **R‑E8c**.

| Frage | Lesarten | Empfehlung | Stand |
|---|---|---|---|
| **E8c‑Q1** „je Stunde" statt „je Betriebsstunde" | Die Art `EUR_PRO_H` heißt in der Betriebskostentabelle jetzt „je Stunde" — der Katalogtext `BM_STUNDE`, wie im Kostendialog und auf der Kostenseite; der gestrichene eigene Text sagte „je Betriebsstunde" — (a) „je Stunde" bleibt; (b) `BM_STUNDE` wird „je Betriebsstunde", dann überall | a | offen; gebaut ist a |
| **E8c‑Q2** Mengeneinheit „kWh/a" und „m²/a" | Befund: In der Herleitung an Betriebszeilen heißen die Mengen der Arten „je kWh Kapazität" und „je m² Kollektorfläche" „kWh/a" bzw. „m²/a"; beide Arten sind im Betriebsraster nicht wählbar, betroffen sind nur Fremd- oder Altdaten — (a) lassen; (b) umbenennen | a | offen; gebaut ist a (unverändert) |

**Erledigt mit dieser Welle** (Register R‑E8b): **E8b‑Q2** mit `e91617da` — die Texte kommen aus dem Katalog,
Herleitung und Formelmappe nutzen denselben Faktor, ein Wächter hält die Konstanten; **E8b‑Q3** mit `7c901689` —
Lesart b, nur die Positionen des ersten Jahres; hybtest warnt nicht mehr, eine echte Lücke warnt weiter.

## Nachweis „keine Rechenwirkung" und Zellvergleich

- **Rechenwerte:** Geändert sind Texte der Berichte, die Herleitungsspalte und die Probe — keine Zahl der Rechnung.
  Die Ankertests (`WirtschaftlichkeitAnkerTests`) laufen unverändert grün; in `ErgebnisansichtTests` ist allein der
  Fassungsanker 8 → 9 nachgezogen. Referenzlauf 13/13 PASS gegen `2026-09-23_R13_Kuehlung`, 4.145.687 Werte in der
  Toleranz; keine Simulationsgröße berührt, kein Schemaschritt.
- **Zellvergleich** (Messprogramm `vergleich.py` des E8b-Berichts, Wort- und Tabellenbericht je Prüfgruppe) über
  **15 Prüfgruppen** — die 13 aus E8b, dazu hybarten (je eine Position jeder Art) und hybluecke (eine echte Lücke:
  eine Position der Kostenart ZUSCHUSS außerhalb der Blöcke der Tabelle). **E8c/1 gegen `fbe93de6`:** geändert nur
  54 Zellen der Spalte „Bemessung"; Beträge, Mengen, Sätze und Formeln unverändert, die Formelzahl gleich, die
  ClosedXML-Nachrechnung „abweichend 0" (hybarten: 465 Formeln, 457 gleich, 8 Fehlerwerte aus `NPV`/`IRR` wie
  bekannt), OpenXML-Prüfung 0 Fehler. **E8c/2 gegen E8c/1:** hybtest und hybbk ohne Warnzeile, hybluecke warnt mit
  1.800 gegen 2.300 €, sonst nur der Hinweistext. **Nach dem Nachzug** (gemergter Stand gegen E8c/2): alle 15
  Prüfgruppen gleich.

## Abweichungen vom Auftrag

- **Für die Bemessungstexte keine neuen Schlüssel** — die Katalogtexte `BM_*` gelten. Sichtbar ändern sich dadurch
  „je Betriebsstunde" → „je Stunde" (E8c‑Q1), „% of fuel cost" → „% of fuel costs" und beim festen Jahresbetrag
  „fester Jahresbetrag" statt „fester Betrag".
- **Ein fester Jahresbetrag mit Menge und Satz** bekommt keine Herleitung mehr; in der Testdatenbank kommt das nicht vor.
- **Nachweisumschlag Fassung 8 → 9**; der Fassungsanker in `ErgebnisansichtTests` ist angepasst. Ältere gespeicherte
  Läufe lesen sich als „ab Jahr 1" und warnen bis zum Neurechnen wie zuvor.
- **Die Herleitungsspalte** nennt zusätzlich „ab Jahr X".
- **Der Phase‑1-Bericht nannte 16 Prüfgruppen** — es sind 15 (die 13 aus E8b und die zwei neuen), berichtigt im
  Phase‑2-Bericht.

## Zahlen und Abnahme

- **Phase 1** (Worktree `e8c`): Kern-Filter und Windows-Schale 0 Fehler, keine neuen Warnungen in den geänderten
  Dateien; Designer wiederholbar; SQL-Prüfer 1.737 Texte, 0 Fundstellen — kein `dotnet test`, kein Referenzlauf.
- **Phase 2** (auf `b18237c0`): Kern-Filter und Windows-Schale 0 Fehler; voller Lauf `WP-Plan.Kern.slnf` 0 Fehler,
  **12.123 bestanden, 1 übersprungen** (EPOS.Kern 5.481, EPOS.UI 5.705, KiKern 524, SpeicherEngine 386,
  SpeicherPlanung 27 und 1 übersprungen); die neuen Tests 33/33; Referenzlauf 13/13 PASS gegen
  `2026-09-23_R13_Kuehlung`, 4.145.687 Werte in der Toleranz; SQL-Prüfer 1.750 Texte, 0 Fundstellen; Zellvergleich
  auf dem gemergten Stand 15 Prüfgruppen gleich.
- **Gate auf `9ab55946`** (Worktree `pm7`, 01:51–02:00), grün: Kern-Filter (Release) 0 Fehler; ChartProben 146 Bilder,
  alle grün, Hashes gleich der Windows-Messlatte (146/146; E8c bringt kein Bild); Tests Kern-Filter 0 Fehler,
  **12.123 bestanden, 1 übersprungen** (dieselbe Aufteilung); Dokumentationswachen 26/26; Referenzlauf und SQL-Prüfer
  aus Phase 2 auf dem byte-gleichen Baum `b18237c0`. Der Testschritt wartete viermal 60 Sekunden auf einen fremden
  Testprozess (Testregel).
- **Tests** (neu): `BemessungstexteAlleArtenTests` (21 Fälle: je Steuerwert einer — Text de/en, Herleitung, Zeile der
  Formelmappe, unveränderter Betrag —, dazu die Beschriftung im Gewerk, leerer und unbekannter Steuerwert und ein
  Wächter, der jede Konstante `DbWerte.BEMESSUNG_*` gegen ihren Fall und den Katalog hält),
  `BetriebskostenStartjahrGliederungTests` (12 Fälle: die Probe, die echte Lücke, die Grenze des ersten Jahres in fünf
  Fällen, die Herleitungsspalte, der Umschlag, die Nachweisliste gegen die Summenschleife, hybtest ohne Hinweis und die
  echte Lücke in beiden Berichten); `ErgebnisansichtTests` (Fassungsanker).
- **Kein Schemaschritt;** `SchemaStand.Zielversion` = 114 aus dem Nachzug; die Testdatenbank ist die Fassung von
  `origin` (LFS `8a3bebaf…`).

## Abnahme am Gerät (A‑E8c‑1, Windows und iPad)

(1) Wortbericht einer Vergleichsgruppe mit bemessenen Betriebskosten öffnen: Die Tabelle „Betriebskosten nach
Kostenarten" nennt je Position die Bemessungsart mit dem Text der Kostenverwaltung (etwa „% der Erzeugerkosten",
„je kW Heizleistung", am Pufferspeicher „je Liter", am BHKW „je kW elektr. Leistung"), „fester Betrag" nur bei festen
Beträgen, „fester Jahresbetrag" beim festen Jahresbetrag; Menge × Satz nur an bemessenen Positionen. (2) Eine
Position mit späterem Startjahr (etwa eine Wartung ab Jahr 6) trägt in der Herleitungsspalte „ab Jahr 6", der
Hinweistext über der Tabelle erklärt es, und der Hinweis „Gliederung unvollständig" erscheint nicht. (3) Ein Projekt
mit echter Lücke zeigt den Hinweis weiterhin; er nennt die Positionen des ersten Jahres. (4) Derselbe Stand im
Tabellenbericht — Spalte „Bemessung", Menge × Satz der Formelmappe unverändert, die rote Warnzeile nur bei echter
Lücke. (5) Englisch („per hour", „% of fuel costs", „from year …").

## Befunde nebenbei

- **Linux-Messlatte der ChartProben:** `Proben/ChartProben/Messlatte_2026-09-20.sha256` im Repository hat 91 Zeilen
  und trägt die 14 Bilder aus #454 (Brücke, Zahlungsstrom) nicht — ebenso wenig die übrigen seither hinzugekommenen;
  die Windows-Messlatte des Gates führt lokal 146 Zeilen. Nachtrag beim nächsten Linux-Lauf.
- **Rechenweg 08:** Die Gap-Kurztafel stand in den Zeilen V‑G6, V‑G8, V‑G9 und V‑G11 auf dem Stand vor #434 — mit den
  Papieren zu #460 nachgezogen.
- **Nachweisumschlag im Mockup:** Der Kasten „Der Nachweisumschlag und seine Grenzen" nannte Fassung 3 mit vier Listen
  und sieben Skalaren — mit den Papieren zu #460 auf Fassung 9 berichtigt. Die Wiki-Quelle nennt keine Fassung des
  Umschlags, dort war nichts zu berichtigen; die Betriebskostentabelle beschreibt dort der neue Punkt
  `bericht-betriebskosten`.
- **CI-Nachweise** der Pushes vor dieser Welle: zu `fbe93de6` Kern-Lauf `main` 35923242428 und Windows-Lauf `main`
  35923242569 grün, der Kern-Lauf `ios_migration_september` 35923217641 abgebrochen — überholt durch den Push
  `c65aefe4` (Merge der Kühlungssitzung), dessen Lauf 35923955544 grün ist und `fbe93de6` einschließt; zu `46023235`
  Kern-Lauf `ios_migration_september` 35932714594, Kern-Lauf `main` 35932719275 und Windows-Lauf `main` 35932719228
  grün; zu `e513f05e` Kern-Lauf `ios_migration_september` 35934414433 grün.
- **Schemaschritt-Vergabe 24.09.2026:** 114 Kühlung KU2 (KU‑S3, Commit `24074b3a`), 115 Zapfprofil T2 (zugesagt),
  116 bis 118 die Schritte B, C und D der Etappe E9a. Die Papiere vom 23.09. nannten „114 Zapfprofil, 115 Dialog
  Design" — mit den Papieren zu #460 berichtigt.
- **Zählweise der Ressourcen:** 8.709 sind die echten Einträge je Sprache (`xml:space="preserve"`). Die Zahl 8 620 der
  Statuszeile #455 zählte die vier Beispielzeilen im Kopf der `.resx` mit (echt 8.616), der Phase‑1-Bericht nannte für
  `b7dd5e1b` 8.613 (echt 8.612).

## Offen

- **Die zwei Fragen** E8c‑Q1 und E8c‑Q2 beim Anwender (Empfehlung jeweils a).
- **Abnahme am Gerät** A‑E8c‑1 (fünf Punkte oben).
- **Nächste Etappe: E9** in zwei Wellen — E9a läuft (die Schemaschritte B, C und D als 116, 117 und 118, der Kern liest
  die Paare; voraussichtlich #461), danach E9b (die Dialoge, der Hinweistext entfällt; voraussichtlich #462).
- **Push** nach der Regel des Anwenders ohne Rückfrage aus dem Hilfszweig `pm7` (`9ab55946` und die Papiere) auf
  `ios_migration_september` und `main`; der CI-Nachweis kommt mit der nächsten Papierwelle.
- **Papiere mit der Statuszeile:** Register (Kopf, Familientafel, R‑E8b Q2 und Q3 erledigt, neue Familie R‑E8c, E7c1‑Q6
  mit Fassung 9, EZ‑9, EZ‑10), Konzept (Kopf, § 2.11.4 Fußnote, § 2.11.6, § 3.4 „Die Betriebskostentabelle der
  Berichte", § 3.6 Fassung 9, § 6.1, § 6.2, § 7 und Anhang), Protokoll der Entscheidwege (§ 8.17, § 8.18, Kopf von
  § 8), Analysepapier (Kopf, Nachtrag, § 0 Punkt 5, § 5 mit der Zeile E8c, § 6), Rechenweg 08 (Gap-Kurztafel), Mockup
  (Anhang U42, Zone „Bericht und Ausgabe", Kasten zum Nachweisumschlag, Ressourcentafel der Kategorie 8,
  Stand-Absatz), Logbuch-Sätze und die Wiki-Quelle der Seite Wirtschaftlichkeit, Index Reporting.
