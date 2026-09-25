# E22 — Rechenweg-Sortierung nach der Regel „99“ des Hydraulikbilds, Referenzbasis R16 (Protokoll, 25.09.2026)

Statuszeile #503 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Anwender am
25.09.2026, „Nr. 18: so umsetzen, Umbau: Das Hydraulikbild wurde im August (HB1) auf die richtige Regel umgestellt:
ungepflegte Anlagen nach hinten, Regel ‚99‘“, nach der Messwelle `mess18` vom selben Tag. Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 6.3 Nr. 18;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E22 (neu), R‑Rest Nr. 18 und R‑E18 (E18‑Q3); Herkunft des Punkts:
[`HB1_Hydraulikbild_Sortierung_Protokoll.md`](HB1_Hydraulikbild_Sortierung_Protokoll.md) (offener Punkt HB1-O1) und
[`E18_Restpunkte_Stromsteuer_Protokoll.md`](E18_Restpunkte_Stromsteuer_Protokoll.md) (Nachmessung, E18‑Q3); die
Messwelle in [`E19_Unternehmensart_ohne_BHKW_Protokoll.md`](E19_Unternehmensart_ohne_BHKW_Protokoll.md), Befund 7;
Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; die Basis in [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md), Abschnitt „Aktuelle Basis“,
und im [Archiv der Referenzbasen](../../Referenzbasen/LIESMICH.md). Vorgänger:
[`E20_Waermepumpe_kW_elektrisch_Protokoll.md`](E20_Waermepumpe_kW_elektrisch_Protokoll.md). Zweig `e22` von
`cbed6dba`; Opus 5.5 im Worktree `.claude/worktrees/e22`: `0e1c0938` (E22/1), `53394b73` (E22/2), E22/3 nicht vergeben
(der Determinismus ohne Datei), `0dd97e05` (E22/4, Basis `2026-09-25_R15_Anlagenprio`, verworfen — Abschnitt
„Die Basis-Kollision“), `548d5983` (E22/5), `8cb5c69b` (Zusammenführung mit `origin` = `f83ce27d`), `c6ee0961`
(E22/6, Basis R16). Merge `76f8661d` („Merge e22: Rechenweg-Sortierung nach der HB1-Regel 99, Referenzbasis R16
(#503)“) auf `pm24` über `origin` = `f83ce27d` (#502 samt seinen Papieren, zusammengeführt mit AK1 Welle 5 und #507).
Testdatenbank Schemastand 142, LFS-SHA-256 `1360e2be…` — unverändert. **Kein Schemaschritt, keine Rechenwirkung** —
alle Werte und Zeitreihen gleich; allein der Modulindex der Wärmepumpen in 1042 wechselt, deshalb die neue Basis
`2026-09-25_R16_Anlagenprio`.

## Befund vor der Welle (Messwelle `mess18`)

1. **Zwei Regeln für dieselbe Frage.** Die fünf Rechenweg-Leser `SimulationControl.WP_Liste_Laden`,
   `…QuellbezuegeAufbauen`, `…SenkenPufferDerAnlagen`, `WaermesenkeClass.SenkenLaden` und `…SenkenlistenLaden`
   sortierten `ORDER BY Prioritaet, ID` — NULL vorn, die ungepflegte Anlage vor der gepflegten. Hydraulikbild und
   Erzeugerkarten folgen seit HB1 der Regel `Ladeordnung.SqlAnlagenprio` (Regel „99“: gepflegte Priorität zuerst,
   NULL oder 0 hinten, bei Gleichstand die ID); der offene Punkt HB1-O1 hielt den Rechenweg fest, E18 hat die fünf
   Stellen im Code mit „HB1-O1, offen“ vermerkt. 48 von 60 Wärmeerzeugern der Testdatenbank tragen keine Priorität.
2. **Probeumbau der fünf Leser** (Worktree `mess18`, Commit `6de80d66`, nicht gemergt): 12 von 13 Referenzprojekten
   byte-gleich; 1042 FAIL allein mit 10 Werten in `aggregate.csv` — die beiden Wärmepumpenmodule tauschen die Plätze,
   Werte gleich, alle Zeitreihen byte-gleich. Die Reihenfolge der Listen ändert sich außerdem in 1030, 1040, 1041 und
   1045, ohne Wirkung auf eine Zahl. Die Kennzahlen aus je fünf frischen Läufen (Deckungsanteile, Endenergie, CO₂, in
   1030 dazu der Kapitalwert) sind vorher gleich nachher; kein Test rot.
3. **Nebenbefund:** `SPK_Liste_Laden`, `Solar_Liste_Laden` und `BHKW_Liste_Laden` laden ohne `ORDER BY`, in der
   Zeilenfolge der Datenbank — für Kessel-, Solar- und BHKW-Module galt `Prioritaet` gar nicht.
4. **Empfehlung der Messwelle:** den Umbau mit der nächsten ohnehin fälligen Neueinfrierung bündeln. Der Anwender hat
   den Umbau sofort beauftragt; E22 friert deshalb eine eigene Basis ein.

## Gebaut

- **E22/1 — die fünf Rechenweg-Leser** (`0e1c0938`): `ORDER BY " + Ladeordnung.SqlAnlagenprio(null) + ", ID` in
  `SimulationControl` (`WP_Liste_Laden`, `QuellbezuegeAufbauen`, `SenkenPufferDerAnlagen`) und `WaermesenkeClass`
  (`SenkenLaden`, `SenkenlistenLaden`) wie im Probeumbau `6de80d66`; die fünf Kommentare „HB1-O1, offen“ entfernt;
  `Ladeordnung.SqlAnlagenprio` mit dem Absatz „gilt für Anzeige und Rechenweg“ statt des Hinweises auf die
  Anzeige-Leser.
- **E22/2 — die drei Modul-Lader** (`53394b73`): `SPK_Liste_Laden`, `Solar_Liste_Laden` und `BHKW_Liste_Laden` nach
  derselben Regel; der Hinweis „Ohne ORDER BY“ beim BHKW ersetzt (E22‑Q1 a, nach der A/B-Messung).
- **E22/5 — Tests** (`548d5983`): neu `EPOS.Kern.Tests/AnlagenprioRechenwegTests.cs` mit vier Fällen — die
  Quelltext-Wache (die acht Leser tragen die Regel, keiner mehr `ORDER BY Prioritaet`), ihre Gegenprobe, der Fakt an
  1042 (Anlage 14817 mit Priorität 1 vor 14818 ohne Priorität in `SenkenLaden` und `SenkenlistenLaden`) und ein
  synthetischer Fall (Priorität 0 gilt als ungepflegt; Kessel 14854 mit Priorität 1 vor beiden Wärmepumpen).
- **E22/4 und E22/6 — die Basis:** Abschnitt „Die neue Basis“. Keine `.resx`, keine Daten, kein Schema.

## Die neue Basis

`Referenzlaeufe/2026-09-25_R16_Anlagenprio`, **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039,
1040, 1041, 1042, 1045, 1046, 1047), 432 CSV und `protokoll.txt` — 433 Dateien, 60 332 709 Byte, 2 447 Skalare; in Git
431 CSV als Umbenennung R100, `Projekt_1042/aggregate.csv` R094. Gerechnet auf der Testdatenbank mit Schemastand 142
(`1360e2be…`), derselben Datei, auf der R15 zuletzt gehalten wurde. Gegen `2026-09-25_R15_Anlagenkopplung` bewegt sich
allein 1042, und nur im Index der Module:

| 1042, `aggregate.csv` | R15 `WaermepumpeModul[0]` | R15 `[1]` | R16 `WaermepumpeModul[0]` | R16 `[1]` |
|---|---|---|---|---|
| `.Modul` (Anlage, Priorität) | CS7800iLW 16 (14818, keine) | CS6800iAW (14817, 1) | CS6800iAW (14817, 1) | CS7800iLW 16 (14818, keine) |
| `.Leistung` [kW] | 15 | 11 | 11 | 15 |
| `.Waermeproduktion` [MWh/a] | 20,84 | 71,45 | 71,45 | 20,84 |
| `.Stromverbrauch` [MWh/a] | 7,06 | 26,29 | 26,29 | 7,06 |
| `.Betriebsstunden` [h] | 2 073,4 | 5 995,29 | 5 995,29 | 2 073,4 |

**A/B der beiden Teile:**

| Stand | Vergleich | Ergebnis |
|---|---|---|
| Teil 1 (fünf Leser) allein | gegen R14, dreizehn Projekte | 12/13 PASS und byte-gleich; 1042 FAIL mit den **10 Werten** oben, die übrigen 35 Dateien byte-gleich |
| Teil 1 + 2 (dazu drei Lader) | gegen Teil 1 allein, dreizehn Projekte | **394/394 CSV byte-gleich** — Teil 2 ändert in keinem Projekt etwas, nicht einmal einen Index |
| Teil 1 + 2 | gegen R15, vierzehn Projekte, Schemastand 142 | 13/14 PASS, 431/432 CSV byte-gleich; allein 1042 `aggregate.csv` mit denselben **10 Werten**; 1047 PASS und byte-gleich (38 Dateien, 403 158 Werte) |

**Warum ohne Rechenwirkung.** Die Deckungsreihenfolge legt die Kaskade über den Typ fest (`Tool_1..4`), Anlagen
finden ihre Senken über die Anlagen-ID und Puffer über `Z_AnlageSenke.Ladeprio`; die Rechenfolge der Wärmepumpen von
1042 steht in `ModulEbenen` (getrennte Senken: 14817 Heizkreis und Puffer 1054196, 14818 nur der Brauchwasserpuffer
1054202, Quellwärme aus 1054196). Teil 2 greift in keinem Projekt: Keines führt zwei Kessel oder zwei Kollektorfelder,
und die beiden BHKW von 1030 stehen nach der Regel wie nach der Zeilenfolge (14920 mit Priorität 1 vor 14921 ohne).
Rechnerisch wirkt die Regel erst bei zwei Anlagen gleichen Typs auf derselben Rechenebene und Senke, deren Priorität
von der ID abweicht — kein Referenzprojekt hat diese Lage.

**Determinismus:** zwei Läufe des Endstands nacheinander 14/14 byte-gleich (432/432 CSV), untereinander GESAMT: PASS
(4 610 207 Werte); der Einfrierlauf ist mit beiden byte-gleich. Auf dem Stand vor der Zusammenführung waren es 13/13
(4 207 049 Werte). **Einfrierregeln** nicht berührt — keine gesäten Daten geändert, die Testdatenbank ist byte-gleich.
Einfrierbefehl: `dotnet run --project EPOS.Referenzlauf -c Release -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite
--projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 --ziel
Referenzlaeufe/2026-09-25_R16_Anlagenprio`.

**Archiv.** Das `protokoll.txt` von R15 ist per `git mv` nach
`Dokumentation/ueberholt/Referenzbasen/2026-09-25_R15_Anlagenkopplung/` gewandert, der Rest per `git rm -r` entfernt.
Das Archiv führt 33 Basen und 34 Dateien, die Tabellenzeile R15 („abgelöst durch R16 am 25.09.2026“) und den Abschnitt
„Die Basis R15 im Einzelnen“ — der ganze bisherige Abschnitt „Aktuelle Basis“ samt dem Nachtrag #505
(Schemastand 142, Zusammenführung mit AK1 Welle 5) und der Notiz zu R14. Die Nachträge wandern damit nach dem Muster,
das `origin` bei R14 gesetzt hat, mit der archivierten Basis; **R16 trägt keine Nachträge** und steht auf genau der
Testdatenbank, die der Nachtrag #505 herleitet. `Referenzlaeufe/LIESMICH.md`: „Aktuelle Basis“ R16 (Anlass,
Modultafel R15/R16, A/B-Tafel mit drei Zeilen, Begründung, Determinismus, Einfrierbefehl, Vorgängernotiz R15),
„Entfernte Basen“ mit 33 Basen und neun gesicherten Protokollen, Behalteliste, Weg A. **Basisname**
`2026-09-25_R15_Anlagenkopplung` → `2026-09-25_R16_Anlagenprio` in `CLAUDE.md` (Regressionsnetz), `kern.yml` (7
Stellen; die sechs CI-Projekte 1030, 1007, 1017, 1045, 1046, 1047 von `origin` bleiben), `ios.yml` (2),
Konzept Gebäudesimulation (Z. 1909), Systementwurf Gebäudesimulation (Z. 179), Konzept Wirtschaftlichkeit (Kopf,
Tafel der Regressionsanker, § 6.3 Nr. 21 — dort stand auf `origin` noch R14), Basenhistorie und Wegweiser des
Archivs. Geschichte bleibt unverändert: Konzept Anlagenkopplung, Status der Gebäudesimulation, das Protokoll der AK1
Welle 5, Konzept Kühlung und Umsetzungskonzept Zapfprofilgenerator (mit R14) und die Statuszeilen.

## Die Basis-Kollision (Befund mit Lehre)

**Was geschah.** E22 fror um 09:52 auf R14 `2026-09-25_R15_Anlagenprio` ein (`0dd97e05`: dreizehn Projekte, 394 CSV,
R14 ins Archiv, der Basisname an allen Stellen ersetzt, die Nachträge 119–141 in der LIESMICH belassen — vom
Orchestrator bestätigt). Zur selben Zeit fror die Cloud-Sitzung der Anlagenkopplung, AK1 Welle 5, die Basis
`2026-09-25_R15_Anlagenkopplung` ein (vierzehn Projekte, neu das Referenzprojekt 1047, die Einfrierregel „gesäte
Auslegungsdaten der Übergabe“, R14 entfernt) und stand zuerst auf `origin` (`bcd61fe2`). Die laufende Nummer R15 war
damit doppelt vergeben, R14 auf beiden Seiten archiviert, die Projektliste verschieden.

**Wie es aufgelöst ist.** Zusammenführung `8cb5c69b` mit `origin` = `f83ce27d`: 1 182 rename/rename-Konflikte der
R14-CSV (hier nach R15_Anlagenprio, dort nach R15_Anlagenkopplung umbenannt) zugunsten von `origin`, der eigene
Ordner per `git rm -r` verworfen; Inhaltskonflikte in neun Dateien (`Referenzlaeufe/LIESMICH.md`, `CLAUDE.md`,
`kern.yml`, `ios.yml`, Konzept Gebäudesimulation, Systementwurf Gebäudesimulation, Konzept Wirtschaftlichkeit,
Archiv-LIESMICH, Basenhistorie) in der Fassung von `origin`; das R14-Protokoll ohne Dublette. Danach E22/6
(`c6ee0961`): A/B gegen R15 auf vierzehn Projekten, Determinismus, Einfrierung als R16, R15 archiviert. E22/4 bleibt
in der Geschichte des Zweigs, ist aber inhaltlich ganz ersetzt; gegen `origin` trägt der Merge nur E22/1, E22/2, E22/5
und E22/6. Kein Rechenfehler in einer der Basen; der Preis waren ein zweites A/B, ein zweiter Determinismuslauf und
die Nachpflege der Namen.

**Lehre.**
1. Zwei Sitzungen, die zugleich eine Basis einfrieren, brauchen Abstimmung — wer einfriert und wer wartet, bevor der
   erste Lauf beginnt.
2. Der Basisname ist erst beim Fetch vor dem Push endgültig: Die Regel „wer zuerst pusht“ gilt für die laufende
   Nummer der Basis wie für Statusnummer und Schemaschritt — vor dem Einfrieren `origin` prüfen, vor dem Push noch
   einmal.
3. Die Nachträge der LIESMICH wandern mit der archivierten Basis (Muster von `origin` bei R14); die neue Basis beginnt
   ohne Nachträge. E22/4 hatte sie noch im Arbeitsbaum gelassen, E22/6 folgt dem Muster von `origin`.

## Fragen aus der Welle

Die Frage stellt der Auftrag samt der Regel, nach der sie im Bau entschieden wird; entschieden am 25.09.2026 nach der
A/B-Messung, nach Empfehlung — a; gebaut ist der Entscheid (→ Register R‑E22).

| Frage | Lesarten | Entscheid |
|---|---|---|
| **E22‑Q1** die drei Modul-Lader (Kessel, Solarthermie, BHKW) | (a) mitnehmen, wenn die A/B-Abweichungen nur Reihenfolgen oder Indizes sind; (b) nur die fünf Leser, falls die Lader Zeitreihen oder Kennzahlen ändern | a — Teil 1 + 2 gegen Teil 1: 394/394 CSV byte-gleich |

## Anwenderentscheid vom 25.09.2026 zu Konzept § 6.3 Nr. 18

Nach der Messwelle (→ Register R‑Rest; der Weg im Protokoll der Entscheidwege § 8.39 und § 8.44):

| Nr. | Entscheid | Stand |
|---|---|---|
| **18** Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1) | „Nr. 18: so umsetzen, Umbau: Das Hydraulikbild wurde im August (HB1) auf die richtige Regel umgestellt: ungepflegte Anlagen nach hinten, Regel ‚99‘“ | erledigt mit E22 (#503): Rechenweg, Hydraulikbild und Erzeugerkarten folgen derselben Regel; neue Basis R16 |

## Abweichungen und Befunde

1. **HB1-O1 geschlossen** — Rechenweg, Hydraulikbild und Erzeugerkarten folgen derselben Regel
   `Ladeordnung.SqlAnlagenprio`; die fünf Vermerke im Code sind entfernt.
2. **Kennzahlen unverändert** — Deckung, Endenergie, CO₂ und Kapitalwert wie vorher (Messwelle an 1030, 1040, 1041,
   1042, 1045; der Bau byte-gleich in allen Werten und Zeitreihen). Gepflegte Priorität wirkt überall in der
   Reihenfolge, rechnerisch nirgends im Bestand.
3. **Die drei Modul-Lader** sortieren jetzt ebenfalls; `Prioritaet` gilt damit auch für Kessel-, Solar- und
   BHKW-Module — ohne Wirkung im Bestand (E22‑Q1 a).
4. **Die Basis-Kollision** mit AK1 Welle 5 — Abschnitt oben, mit Lehre.
5. **Die CI bemerkt den Umbau nicht:** 1042 gehört nicht zu den sechs CI-Projekten; nur der Lauf der vierzehn
   Projekte zeigt ihn. Die CI vergleicht gegen R16 und bleibt grün, auch wenn 1042 sich bewegte.
6. **Abweichung vom R13-Muster in E22/4** (die Nachträge 119–141 blieben im Arbeitsbaum) — mit E22/4 verworfen; R16
   folgt dem Muster von `origin`.

## Nachweis

- **Ohne Rechenwirkung:** Anker unberührt; alle Werte und Zeitreihen gleich; die A/B-Tafel oben; Testdatenbank
  unverändert (Schemastand 142, `1360e2be…`).
- **Erster Bau** (Worktree `e22` auf `548d5983`): voller Lauf EPOS.Kern 6 826 und 1 übersprungen, EPOS.UI 6 243,
  KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen — 14 031 bestanden / 0 Fehler / 2
  übersprungen; Referenzlauf 13/13 gegen die verworfene Basis R15_Anlagenprio, 394/394 CSV byte-gleich.
- **Endstand** (Worktree `e22` auf `c6ee0961`): voller Lauf EPOS.Kern 7 090 und 1 übersprungen, EPOS.UI 6 243, KiKern
  549, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen — 14 295 bestanden / 0 Fehler / 2 übersprungen
  (`AnlagenprioRechenwegTests` und `GebaeudeRueckwegTests` grün mit R16); SqlDialektPruefer 0 Fundstellen;
  Referenzlauf 14/14 gegen R16 PASS (4 610 207 Werte, 432/432 CSV byte-gleich); Testhost-Regel eingehalten.
- **Merge** `76f8661d` auf `pm24` über `origin` = `f83ce27d`.
- **Gate:** Kern-Filter 0 Fehler, ChartProben 161/161 gleich der Windows-Messlatte, voller Lauf 14.295 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern 7.090 und 1 übersprungen, EPOS.UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen), Dokumentationswachen 29/29 (`GATE503.log`, 25.09.2026 11:43–11:56 Uhr, auf `76f8661d` = Merge e22 über den Push #502 `f83ce27d`); auf dem End-Merge mit #504 (`7901dd4e`, Testdatenbank `22eeb75c…`) Build 0 Fehler, Schema- und Dokumentationswachen samt `AnlagenprioRechenwegTests` und `GebaeudeRueckwegTests` grün und Referenzlauf 14/14 gegen R16 PASS (4.610.207 Werte) — dazu der Referenzlauf aller vierzehn Projekte gegen R16 aus dem Bau: 14/14 PASS
  (4 610 207 Werte, 432/432 CSV byte-gleich).
- **CI:** steht aus (Beobachtung nach dem Push) — die CI muss gegen R16 laufen (`kern.yml` mit dem Basispfad R16 und den sechs Projekten).

## Abnahme am Gerät (A‑E22‑1, Windows)

1. **Ergebnis:** Projekt 1042 rechnen — Modul 1 der Wärmepumpen ist die Anlage 14817 (CS6800iAW, 11 kW, Priorität 1),
   Modul 2 die Anlage 14818 (CS7800iLW 16, 15 kW, ohne Priorität).
2. **Bericht:** dieselbe Reihenfolge der Wärmepumpenmodule.
3. **Hydraulikbild:** 14817 steht vor 14818 — Ergebnis, Bericht und Bild stimmen überein.

Belegt in `Projekt_1042/aggregate.csv` von R16; die Sichtprüfung liegt beim Anwender.

## Logbuch

Kein Eintrag. Keine Seite des Wikis beschreibt die Nummerierung der Module im Ergebnis; die Seite Simulation nennt die
Priorität nur als Deckungsreihenfolge der Kaskade, und die gilt unverändert über den Typ. Ein Satz im Logbuch hätte
keine Stelle im Wiki, auf die er sich stützt. Der Satz aus dem Bau bleibt als Vorschlag stehen, falls eine Seite die
Modulreihenfolge später beschreibt: „Die Nummerierung der Module im Ergebnis folgt der Anlagenpriorität; eine Anlage
ohne Priorität steht hinter den Anlagen mit Priorität — wie im Hydraulikbild.“

## Papiere mit der Statuszeile

Mit dem Merge (E22/6): der Basisname und die Referenzbasen-Papiere (Abschnitt „Die neue Basis“). Mit dieser
Statuszeile: Konzept (Kopf mit Codestand `76f8661d` und „E22 ohne Schritt“, Schrittabsatz, § 6.1 Zeile E22, § 6.3 Nr. 18
als Einzeiler erledigt, Nr. 24 „nach R16“, § 6.5 Zeile „Zwei Migrationsmechanismen“ aufgelöst mit #501, § 7, Anhang
mit Kürzel- und Etappenzeile E22), Register (Kopf, Familientafel, neue Familie R‑E22, R‑Rest Nr. 18 gebaut, R‑E18
mit E18‑Q3, R‑E21 Q3 und Q6 „nach R16“), Analysepapier (§ 5 Zeile E22, Basisvermerk der Legende), Protokoll der
Entscheidwege (Kopf von § 8, § 8.44 mit dem Wortlaut von Nr. 18 vor #503 und der Basis-Kollision, § 8.45), Index
(Reporting 136 → 137), Statusdatei (#503, Nach #503; in #506 und Nach #506 „nächste Neueinfrierung nach R16“). Das
Archiv der Referenzbasen ist geprüft: Tabellenzeile und Abschnitt R15 nennen Datum und Grund der Ablösung, R14 steht
unverändert („abgelöst durch R15 am 25.09.2026“). Kein Mockup (kein Dialog), kein Wiki, kein Logbuch.

## Offen

- Die **Abnahme am Gerät** A‑E22‑1 (drei Schritte oben).
- Die Kandidaten **1018-Träger und 1023** (E21‑Q3, E21‑Q6) bei der nächsten ohnehin fälligen Neueinfrierung nach R16 —
  E22 hat sie bewusst nicht mitgenommen.
- **Gate** und **CI** (Nachweis oben).
