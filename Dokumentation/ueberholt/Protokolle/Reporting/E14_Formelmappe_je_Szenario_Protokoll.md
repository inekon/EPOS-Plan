# E14 — Formelmappe je Szenario: Stufen 1 und 2 für Günstig und Ungünstig (Protokoll, 24.09.2026)

Statuszeile #477 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Auftrag nach dem Befund 1 aus
E9a (Protokoll [`E9a_Szenarioabdeckung_Kern_Protokoll.md`](E9a_Szenarioabdeckung_Kern_Protokoll.md): „Formelmappe
Stufe 1 und 2 rechnen nur Erwartet"). Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.6 (Stufenplan, „Dauerhaft Werte bleiben", „EPOS trägt die Werte ein, Excel rechnet neu") und § 2.11.4 (V‑D);
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E8b (E8b‑Q1, abgelöst) und R‑E14 (neu); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5 (E8 Teil b); Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Anhangzeilen U12 und U43, Zone
„Bericht und Ausgabe" und Ressourcentafel der Kategorie 8. Anlass: der Anwender am 24.09.2026, 18:35, „Formelmappe und
Befund aus E9a: Auftrag". Vorgänger: [`E13_Checkliste_Fehlergruende_Protokoll.md`](E13_Checkliste_Fehlergruende_Protokoll.md);
Muster der Formelmappe: [`E8b_Formelmappe_AnhangE_D_Protokoll.md`](E8b_Formelmappe_AnhangE_D_Protokoll.md). Zweig `e14`
von `105cdb31` (`origin` nach dem Push von #474), Opus 5.5 im Worktree `.claude/worktrees/e14`, zwei Phasen: `dd0ec440`
(E14/1), `d29f6496` (E14/2), `7935316d` (E14/3), `172971e4` (E14/4) in Phase 1; in Phase 2 die Zusammenführung
`f6727fdd` mit `origin` = `61efa054` (Anwender-Merge der Anlagenkopplung AK1, Welle 2, mit #476; Schemastand 124).
Erster Merge `f3f071d2` („Merge e14: Formelmappe je Szenario - Stufen 1 und 2 fuer Guenstig und Unguenstig (#477)")
über `61efa054`, der Baum byte-gleich mit `f6727fdd`; End-Merge `b9c660b9` über `origin` = `83b10810` (#480
Hilfe der Nutzflächenangabe auf `Gebäude#verbrauch`, CI-Nachzug des Zapfprofils Z4; ohne Schemaschritt, Testdatenbank
unverändert). Basis `2026-09-24_R14_Kaelteerzeuger`. **Kein Schemaschritt, keine
Rechenwirkung im Kern, keine neue Basis** — nur der Tabellenbericht ändert sich; die Kernwerte bleiben die Wahrheit.

## Befund vor der Welle

Die Formelmappe (E8b, #455) rechnete die Stufen 1 (Mehrjahrestabellen) und 2 (Kennzahlen, Differenzreihe, Zinsfuß,
Amortisation) nur für das Szenario Erwartet. Die Blöcke Günstig und Ungünstig standen als Werte (E8b‑Q1 a); der
Betrachtungszeitraum je Szenario (E9a, #461) wirkte in der Mappe nur im Parameterblock (Stufe 0) und im Verlaufsblock.

## Gebaut

- **E14/1 — Stufe 1 je Szenario:** Unter den Tabellen des Szenarios Erwartet je ein Block „Mehrjahresübersicht der
  Zahlungsströme — Szenario „Günstig"/„Ungünstig" (T = n a)" mit Hinweiszeile und je Stand einer Tabelle. Die
  Eingangswerte kommen aus dem Lauf des Szenarios (`BerechneVerlaufSzenarienJeZeitraum`), die Formeln rechnen mit
  seiner Spalte im Parameterblock (`p.FuerSzenario(s)`; Namen `Zins_i_Guenstig`, `p_E_Unguenstig` …). Die Tabellen
  reichen bis zum längsten Zeitraum der drei Szenarien; jenseits von T_s hält eine Schutzformel
  `IF(A<=Zeitraum_T_s,…,"")` die Zeile leer, der Restwert steht am Ende von T_s. Der Nachweisblock „vermiedene Kosten"
  steht nur bei Erwartet.
- **E14/2 — Stufe 2 je Szenario:** Kennzahlblöcke Günstig und Ungünstig mit dem Nettobarwert über `NPV` + Jahr 0 +
  Restwert-Barwert, der Annuität über `PMT`, der Differenzreihe Variante − Referenz und der Amortisation. Zwei neue
  Hilfsspalten je Differenzreihe: das Vorzeichen (über Nullwerte ≤ 1E‑6 € fortgeschrieben) und die Zahl der Wechsel
  bis zu diesem Jahr — dieselbe Regel wie `KapitalwertRechner.Vorzeichenwechsel`. Der Zinsfuß: 0 Wechsel „kein Zinsfuß
  bestimmbar", mehr als ein Wechsel „nicht eindeutig", sonst `ROUND(IRR(…)*100,2)`. Die Bandbreitentafel führt die
  Kapitalwertdifferenz der drei Szenarien als Zellbezug, die Spanne als `MAX-MIN`. Jede Formel ist im `Formelregister`
  gegengerechnet. **Entscheidnachtrag:** E14‑Q2 a ersetzt E8b‑Q1 a (die Kennzahlen Günstig und Ungünstig bleiben nicht
  mehr Werte).
- **E14/3 — Wertfassung = Formelfassung, Wachfälle:** `Excel_E14_Kennzahlen_Guenstig_und_Unguenstig_rechnen_in_Formeln`
  und `Excel_E14_Zeitraum_je_Szenario_mit_Schutzformel` (Zeiträume 25/20/15 a) in `BerichtBlattstrukturWacheTests`; die
  Prüfgruppe `GruppeMitSaetzen` trägt dafür T_Günstig und T_Ungünstig.
- **E14/4 — Wachen und Ausweis:** Die Ankerzeilen der neuen Blöcke (Titel Günstig P+154 … Abschluss P+261; alles bis
  P+151 unverändert); Punkt 11 der Anhang-E-Checkliste „die Mappe rechnet alle drei Szenarien formelbasiert" (de/en,
  Test `Punkt_11_nennt_alle_drei_Szenarien_formelbasiert` in `AnhangEChecklisteTests`); die Parameterblock-Zeile
  „Betrachtungszeitraum T_s je Szenario [a]"; der Hinweis `WIRT_FM_PARAM_HINWEIS` nennt die Namen je Szenario und die
  Länge der Tabellen.

## Schlüssel

Je Sprache 9.927 → 9.932 Einträge. **Neu (5):** `WIRT_FM_MJ_SZENARIO_TITEL` („Mehrjahresübersicht der Zahlungsströme —
Szenario „{0}" (T = {1} a)"), `WIRT_FM_MJ_SZENARIO_HINWEIS`, `WIRT_FM_MJ_VORZEICHEN` („Vorzeichen Δ nominal (|Δ| ≤ 1E-6 €
zählt nicht)"), `WIRT_FM_MJ_WECHSEL` („Vorzeichenwechsel bis hier"), `WIRT_FM_IZF_NICHT_EINDEUTIG` („nicht eindeutig —
die Differenzreihe wechselt mehrfach das Vorzeichen (Anhang C)"). **Neu gefasst (3):** `WIRT_FM_PARAM_ZEITRAUM`
(„Betrachtungszeitraum T_s je Szenario [a]"), `WIRT_FM_PARAM_HINWEIS` (die Formeln rechnen je Szenario mit seiner
Spalte; die Jahreszeilen reichen bis zum längsten Zeitraum, Jahre nach dem Zeitraum eines Szenarios bleiben leer — ein
längerer Zeitraum verlangt einen neuen Bericht) und `WIRT_AE_11_STAND` (Punkt 11: „die Mappe rechnet alle drei
Szenarien formelbasiert — mit sichtbaren Formeln auf ihre Spalte im Parameterblock"). Designer wiederholbar.

## Fragen aus der Welle

Gebaut ist jeweils Lesart a (die Empfehlung); offen beim Anwender (→ Register R‑E14).

| Frage | Lesarten | Empfehlung |
|---|---|---|
| **E14‑Q1** Länge der Tabellen je Szenario | (a) eine Tabelle je Szenario bis zum längsten Zeitraum mit Schutzformeln jenseits von T_s; (b) drei Blätter, je Szenario eines | a |
| **E14‑Q2** Kennzahltafel Günstig/Ungünstig | (a) Formeln statt Werte — ersetzt E8b‑Q1 a; (b) Werte bleiben, die Formeln nur als Nebenblock | a |
| **E14‑Q3** Zinsfuß bei mehreren Vorzeichenwechseln | (a) Text „nicht eindeutig"; (b) `IRR` mit Schätzwert | a |

## Abweichungen und Befunde

1. **Zinsfuß-Text auch bei Erwartet:** E14‑Q3 a gilt für alle drei Szenarien — ein mehrdeutiger Zinsfuß steht auch im
   Block Erwartet als Text statt als Zahl (nur Ausweis); keine Prüfgruppe ist betroffen.
2. **Formeltexte im Block Erwartet:** die neue Zinsfußformel und die zwei Hilfsspalten; bei ungleichem Zeitraum stehen
   Schutzzeilen in der Tabelle, und die Abschlusszeile rückt nach unten.
3. **„Menge × Preis je Träger" nicht zerlegt:** Die Energiespalte der Szenariotabellen ist der Jahr‑1-Kernwert des
   Szenariolaufs, fortgeschrieben mit p_E des Szenarios; Grund- und Leistungspreise bleiben Werte (§ 2.11.6). Stufe 3
   (Betriebskosten Menge × Satz) bleibt beim Szenario Erwartet.
4. **Designer-Lücke fremd:** Der Designer-Lauf nahm den fehlenden Eintrag `ZPG_EINGABE_STOCHASTIK_EINHEITSTAGE` mit auf —
   ein Schlüssel des Zapfprofilgenerators, nicht dieser Welle.
5. **Wortbericht:** nur der neue Text von Punkt 11 der Checkliste; die Gliederung ist unverändert.
6. **Regelverstoß in Phase 2:** Ein voller Testlauf startete um 18:39:23 für Sekunden parallel zu Testprozessen des
   Zweigs `z4` und wurde abgebrochen, er zählt nicht; die Sitzung Zapfprofil ist informiert.

## Nachweis

- **Kein Kernwert ändert sich:** `WirtschaftlichkeitAnkerTests` unverändert grün, kein Anker neu gesetzt; Referenzlauf
  13/13 gegen `2026-09-24_R14_Kaelteerzeuger` PASS, 394/394 CSV byte-gleich, 4.207.049 Werte; SQL-Prüfer 1.807 Texte,
  0 Fundstellen.
- **Zellvergleich** (Messprogramm aus E8b, erweitert; Stand vorher gegen nachher, 16 Prüfgruppen mit der neuen Gruppe
  `hybzeit` — Zeiträume je Szenario verschieden): Excel 16 nach voller Neuberechnung abweichend 0, ClosedXML-Nachrechnung
  abweichend 0 (Fehlerwerte nur bei `NPV`/`IRR`, wie bekannt), OpenXML-Prüfung 0 Fehler. Je Gruppe genau drei gewollte
  Textzellen geändert: Parameterblock Zeile 5 „Betrachtungszeitraum T_s je Szenario [a]", der Hinweis in Zeile 15 und
  Punkt 11 der Checkliste. Wortbericht: Gliederung unverändert.

| Prüfgruppe | Formeln vorher | Formeln nachher |
|---|---|---|
| synth | 256 | 895 |
| 1019/1023/1024 | 320 | 1.039 |
| 1030 | 125 | 373 |
| prep1030 | 134 | 382 |
| hyb1040 | 427 | 1.535 |
| hyb1042 | 429 | 1.541 |
| hybbk | 449 | 1.597 |
| hybleer | 429 | 1.541 |
| hybtest | 448 | 1.596 |
| hybarten | 465 | 1.617 |
| hybluecke | 448 | 1.596 |
| hybzeit (neu) | 429 | 1.766 |
| 1018/1031 | 10 | 10 |
| 1026/1027/1029 | 29 | 29 |
| 1046 | 0 | 0 (keine Wirtschaftlichkeit) |

  Die sechzehnte Gruppe `synthk` nennt der Bericht nicht mit eigener Zahl; ihre Zellen sind im Vergleich enthalten
  (abweichend 0).
- **Phase 1** (Worktree `e14`): Build 0 Fehler, Designer wiederholbar; kein `dotnet test`.
- **Phase 2** (auf `f6727fdd`, derselbe Baum wie `f3f071d2`): Kern-Filter 0 Fehler; gefiltert 87/87; voller Lauf
  `WP-Plan.Kern.slnf` **12.967 bestanden / 0 Fehler / 1 übersprungen** (EPOS.Kern.Tests 6.009, EPOS.UI.Tests 5.996,
  KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 und 1 übersprungen); Referenzlauf 13/13 (oben);
  SQL-Prüfer 1.807/0; Designer wiederholbar.
- **Gate auf `f3f071d2`** (Worktree `pm14`, Log `GATE477.log`): Kern-Filter 0 Fehler; ChartProben 151/151 gleich der
  Windows-Messlatte (E14 bringt kein Bild); voller Lauf 12.967 / 0 / 1 (Kern 6.009, UI 5.996, KiKern 549,
  SpeicherEngine 386, SpeicherPlanung 27/1); Dokumentationswachen 29/29.
- **Gate auf dem End-Merge `b9c660b9`:** reduziertes Gate auf dem End-Merge b9c660b9 (Nachzug 83b10810): Build 0 Fehler, E14-Testklassen, Maskenwache, Anker-Wache und Dokumentationswachen Kern 141/141, UI 26/26.
- **CI:** steht aus (Push nach dem Gate).

## Abnahme am Gerät (A‑E14‑1, Windows)

1. **Projekt 1030 berechnen, Tabellenbericht in Excel öffnen:** unter den Tabellen von Erwartet die Blöcke „Szenario
   Günstig" und „Szenario Ungünstig"; ihre Kennzahlen sind Formeln und zeigen dieselben Zahlen wie die Seite.
2. **`Zins_i_Guenstig` im Parameterblock ändern:** Tabellen und Kennzahlen von Günstig ziehen mit, Erwartet und
   Ungünstig bleiben.
3. **Zeiträume 25/15 a für Günstig/Ungünstig pflegen, neu rechnen, Bericht erzeugen:** Die Tabellen reichen bis Jahr 25,
   die Zeilen von Ungünstig bleiben ab Jahr 16 leer.
4. **Blatt „Checkliste Anhang E":** Punkt 11 nennt „alle drei Szenarien formelbasiert".

## Logbuch

Im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026), Stichwort `bericht`: „Die Formelmappe des
Tabellenberichts rechnet Mehrjahrestabellen, Kennzahlen und Bandbreite für alle drei Szenarien mit sichtbaren Formeln
auf den Parametersatz des jeweiligen Szenarios."

## Papiere mit der Statuszeile

Register (Kopf, Familientafel mit R‑E8b und der neuen Familie R‑E14, R‑V zu V‑G10, R‑E8b mit E8b‑Q1 „abgelöst durch
E14‑Q2 a (#477)", R‑E9a mit dem Befund 1, R‑E14 mit E14‑Q1…Q3, EZ‑9, EZ‑10), Konzept (Kopf, § 2.11.4 V‑D „ergänzt #477",
§ 2.11.6 Stufenplan und „Was die Mappe trägt", § 6.1, § 6.2, § 7 und Anhang), Analysepapier (Kopf, Nachtrag, § 5 E8
Teil b „ergänzt #477" und Stand der Etappen), Protokoll der Entscheidwege (Kopf, Kopf von § 8, § 8.27, § 8.28), Mockup
(U12, U43 mit Punkt 11, Zone „Bericht und Ausgabe", Ressourcentafel der Kategorie 8, Stand-Absatz), Update-Papier und die
Wiki-Quelle Wirtschaftlichkeit (Abschnitt `formelmappe`), Index (Reporting 128 → 129).

## Offen

- **Fragen E14‑Q1…Q3** beim Anwender (gebaut jeweils a).
- **Abnahme am Gerät** A‑E14‑1 (vier Schritte oben).
- **Gate auf dem End-Merge** und **CI** (Nachtrag).
- Der **Wiki-Sammel-Upload** am 26.09.2026 (Version 1.2.0.4, freigegeben).
