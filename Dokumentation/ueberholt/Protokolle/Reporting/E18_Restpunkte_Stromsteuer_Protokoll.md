# E18 — Restpunkte der Wirtschaftlichkeit: Wache der Stromsteuer-Rückfallebene, erfasster Stromsteueranteil im Dialog „BHKW-Wirtschaftlichkeit", Nr. 18 nachgemessen (Protokoll, 24.09.2026)

Statuszeile #492 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Anwender am
24.09.2026, „fahre fort" auf die Empfehlung, als nächste kleine Welle die Wache Konstante gegen Katalog zusammen mit
Nr. 18 und Nr. 16 zu bauen, „da sie wenig Schema berühren". Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.2 (Gruppe 4 — Stromsteuer), § 6.3 Nr. 14, 16, 18 und der neue Restpunkt Nr. 33, § 6.5 (Zeile Stromsteuersatz);
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E18 (neu); Herkunft der Punkte: [`B4_Energieintensitaet_Protokoll.md`](B4_Energieintensitaet_Protokoll.md) § 4
(Grenzen 1 und 3) und [`HB1_Hydraulikbild_Sortierung_Protokoll.md`](HB1_Hydraulikbild_Sortierung_Protokoll.md)
(HB1-O1); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 5 (Zone „Dialog", Gruppe Stromsteuer;
Zone Überlagerung „Sätze und Herkunft"; Ressourcentafel) und Stand-Absatz des Anhangs. Vorgänger:
[`E16_Wiederholperiode_Protokoll.md`](E16_Wiederholperiode_Protokoll.md). Zweig `e18` von `fbc4e536` (`origin` nach
#491), Opus 5.5 im Worktree `.claude/worktrees/e18`, zwei Phasen: Phase 0 nur gelesen und gemessen, Phase 1 nach der
Freigabe (24.09.2026, ~23:35) `52e223e2` (E18/1), `52b94866` (E18/2), `50ef787d` (E18/3), `5c05ea6b` (E18/4),
`29fad34d` (E18/5). Merge `e79bffb1` („Merge e18: Wache Stromsteuer-Rueckfallebene gegen Katalog, Anzeige des erfassten
Stromsteueranteils, HB1-O1 vermerkt (#492)") auf `pm19` über `origin` = `247e2091` (#493 mit Schemaschritt 130, den
Anschlusslängen im Gebäudekatalog), ohne Konflikt. Basis `2026-09-24_R14_Kaelteerzeuger`. **Kein Schemaschritt, keine
Rechenwirkung** — Anker und Referenzlauf bitgleich; der Schemastand bleibt 130 (#493).

## Befund vor der Welle (Phase 0)

1. **Wache Konstante gegen Katalog (§ 6.3 Nr. 14, § 6.5).** `StrompreisZerlegungModel` führt `STROMSTEUER_REGELFALL`
   = 2,050 und `STROMSTEUER_REDUZIERT` = 0,050 ct/kWh (dazu die drei Umlagen); die Saat `GesetzKatalog.Vorbelegung`
   führt `STROMST_REGELSATZ` 2026 = 20,50 EUR/MWh und `STROMST_REDUZIERT_SATZ` 2026 = 0,50 EUR/MWh (Generation 7), die
   Testdatenbank (Generation 9) dieselben Werte, keine Zeile nach 2026. Die Konstanten greifen nur als Rückfallebene —
   für Jahre vor 2026 oder bei gelöschter Katalogzeile. **Die Aussage des § 6.5 stimmte nicht:** Die vorhandene Wache
   `StrompreisZerlegungTests.Die_Katalogwerte_und_die_Rueckfallebene_sind_wertgleich` hielt die Konstante schon gegen
   die Saat, aber nur für `JahrVon == 2026`; es fehlten die Wache gegen den Katalog der Testdatenbank und die
   Robustheit gegen eine spätere Jahreszeile. Ein vierter Ort: die Ressourcen `PREIS_STROMSTEUER_REGELFALL` und
   `PREIS_STROMSTEUER_REDUZIERT` („2,05"/„0,05") ohne Leser seit B4.
2. **Nr. 18 — Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1).** Ursprung ist HB1-O1 (30.08.2026): Die Stellen des
   Rechenwegs, die `ORDER BY Prioritaet, ID` sortieren, tragen ungepflegte Prioritäten (NULL, 0) vorn; HB1 stellte nur
   die vier Anzeige-Leser auf `Ladeordnung.SqlAnlagenprio` (99er-Regel) um. Heute unverändert an **fünf** Stellen:
   `SimulationControl.cs` (`WP_Liste_Laden`, `QuellbezuegeAufbauen`, `SenkenPufferDerAnlagen`) und
   `WaermesenkeClass.cs` (`SenkenLaden`, `SenkenlistenLaden`) — die „drei Stellen" des § 6.3 Nr. 18 waren die gemeinten,
   aber unvollständig. SQLite sortiert NULL wie ACE zuerst. **Messung:** 48 von 60 Wärmeerzeugern (Typ 1, 2, 10, 11)
   der Testdatenbank tragen `Prioritaet` NULL, keiner 0; die 99er-Regel änderte die Reihenfolge in **5 von 13**
   Referenzprojekten (1030, 1040, 1041, 1042, 1045), in 1042 dreht sich die Modulreihenfolge der Wärmepumpen. Ein Umbau
   ist ein Rechenweg-Umbau ohne bitgleichen Referenzlauf — nicht Teil von E18.
3. **Nr. 16 — Rückweg „Parameterdialog zeigt den erfassten Preisanteil".** Herkunft B4 § 4 Grenze 3: Der Hinweg
   (Unternehmensart → Hervorhebung der Schnellwahl in „Strompreis Details", `EnergietraegerHuelle.ReduzierterSatzEmpfohlen`)
   ist gebaut, der Rückweg fehlt. Der Preisanteil ist der erfasste Stromsteueranteil des Stromträgers
   (`energy_project_settings.Aufschlag_Stromsteuer` mit Aktiv-Schalter), gepflegt in „Strompreis Details". Die
   Unternehmensart wird seit dem Auszug der Steuerfelder nicht mehr im Parameterdialog gepflegt, sondern im Dialog
   „BHKW-Wirtschaftlichkeit" (Gruppe 4 „Stromsteuer" und die Überlagerung „Sätze und Herkunft"); der Rückweg gehört
   dorthin. Nebenbefund: Die Unternehmensart ist nur erreichbar, wenn das Projekt ein BHKW führt (→ E18‑Q7).

## Gebaut

- **E18/1 — Wache der Rückfallebene gegen den Katalog** (`52e223e2`): Der Saattest vergleicht die **älteste**
  Saatzeile je Schlüssel statt `JahrVon == 2026`. Neue Wache
  `StrompreisZerlegungTests.Die_Rueckfallebene_steht_wertgleich_im_Katalog_der_Testdatenbank` (Sammlung
  „Testdatenbank"): die älteste Zeile aus `Tab_Gesetzesparameter` gegen `StrompreisZerlegungModel` für
  `STROMST_REGELSATZ`, `STROMST_REDUZIERT_SATZ` und die drei Umlagen; Einheit umgerechnet (EUR/MWh ÷ 10, ct/kWh × 1,
  jede andere Einheit rot); die Meldung nennt die drei nachzuziehenden Orte — Modellkonstante,
  `GesetzKatalog.Vorbelegung` samt Generation, Testdatenbank. Eine Novelle ist eine spätere Jahreszeile und lässt die
  Rückfallebene stehen. Gestrichen: `PREIS_STROMSTEUER_REGELFALL` und `PREIS_STROMSTEUER_REDUZIERT` (de/en).
- **E18/2 — HB1-O1 im Code vermerkt** (`52b94866`): An den fünf Rechenweg-Sortierungen steht der Verweiskommentar
  „HB1-O1, offen: ungepflegt vor gepflegt" — nur Kommentare, keine Änderung der Abfragen.
- **E18/3 — Kern** (`50ef787d`): `StrompreisZerlegungCtrl.StromsteuerErfasst(idProjekt)` liefert
  `StromsteueranteilStand` (Träger-ID und -Name, roher Wert in ct/kWh oder `null`, Aktiv, Lesbar, Grund) und wirft nie;
  der rohe Leseweg `StromsteuerRoh` ist wortgleich aus `KohaerenzPruefung` nach `StrompreisZerlegungCtrl` verschoben
  (öffentlich, E18‑Q6 a), Fall 4 der Kohärenzprüfung ruft ihn dort.
- **E18/4 — Dialog** (`5c05ea6b`): neu `EPOS.UI/Dialoge/Wirtschaftlichkeit/StromsteueranteilAnzeige.cs`. Der Dialog
  „BHKW-Wirtschaftlichkeit" zeigt in Gruppe 4 „Stromsteuer" unter den Feldern der Unternehmensart und in der
  Überlagerung „Sätze und Herkunft" (Gruppe „Stromsteuer (StromStG) — Projekt") den erfassten Stromsteueranteil und
  folgt live der — auch ungespeicherten — Unternehmensart (E18‑Q4 a). **Zeile 1, Herleitung:** Träger, Wert in ct/kWh,
  aktiv oder abgeschaltet, dazu der Abgleich „Regelsatz", „reduzierter Satz" oder „weder … noch" des Jahres; die Sätze
  aus dem Katalog des Bilanzjahres (ohne Bilanzjahr die Bilanzkonvention 2026, ohne Katalogzeile die Rückfallebene),
  Toleranz 0,005 ct/kWh wie Fall 4 der Kohärenzprüfung. **Zeile 2, Kohärenz** (nur bei aktivem Anteil): „Passt zur
  Unternehmensart" (Ok) oder der Vorschlag des anderen Satzes mit dem Verweis auf „Strompreis Details" (Abweichend) —
  dieselbe Regel wie die Hervorhebung der Schnellwahl, **keine Sperre** (E18‑Q5 a). Ohne erfassten Anteil, ohne
  Stromträger oder bei nicht lesbarem Wert sagt die Zeile das, mit dem Pflegehinweis. Nur Anzeige, keine zweite
  Pflegestelle (§ 6.5). Die Gabe `Stromsteueranteil` läuft über `BhkwWirtschaftlichkeitHuelle`, den Record
  `BhkwDialogDaten` (optionaler Parameter), `AppWurzel.razor` und `EPOS.iOS/Dienste/IosProjektQuelle.cs` (iOS-Zeile
  lokal nicht kompiliert, kein iOS-Lauf). Die KI-Sicht bleibt unverändert: Anzeige- und Kohärenzzeilen sind nach der
  Regel der KI-Dialoge kein Feld.
- **E18/5 — Tests** (`29fad34d`): `StromsteuerErfasstTests` (Kern, vier Fälle: 1017 mit 2,05 aktiv und Träger; 1030
  mit `null`, nicht 2,05; 1018 und Projekt 0 ohne Stromträger; der rohe Leseweg); `BhkwWirtschaftlichkeitDialogTests`
  acht bUnit-Fälle mehr (ohne Gabe keine Zeile; Regelsatz Ok; Wechsel auf produzierendes Gewerbe live ohne Schreiben;
  Satzabgleich mit Bilanzjahr 2027; fremder Satz; abgeschaltet ohne Kohärenzzeile; nicht erfasst, kein Träger, nicht
  lesbar; Englisch).

## Schlüssel

**14 neu** (de/en), alle im Bündel `BhkwWirtschaftlichkeitTexte`: `BHW_S_STANTEIL_ERFASST` („Erfasster
Stromsteueranteil im Strompreis „{0}": {1} ct/kWh ({2})."), `BHW_S_STANTEIL_AKTIV`, `BHW_S_STANTEIL_INAKTIV`,
`BHW_S_STANTEIL_KEINER`, `BHW_S_STANTEIL_KEIN_TRAEGER`, `BHW_S_STANTEIL_NICHT_LESBAR`, `BHW_S_STANTEIL_REGEL`,
`BHW_S_STANTEIL_REDUZIERT`, `BHW_S_STANTEIL_ABWEICHEND`, `BHW_S_STANTEIL_SATZ_REGEL`, `BHW_S_STANTEIL_SATZ_REDUZIERT`,
`BHW_S_STANTEIL_PASST`, `BHW_S_STANTEIL_VORSCHLAG`, `BHW_S_STANTEIL_PFLEGE`. **2 gestrichen:**
`PREIS_STROMSTEUER_REGELFALL`, `PREIS_STROMSTEUER_REDUZIERT`. Je Sprache auf `e18` 10.009 → 10.021 Einträge, der
Designer 10.006 → 10.004 → 10.018 Eigenschaften, wiederholbar; auf dem Merge `e79bffb1` je Sprache 10.027 Einträge,
Designer 10.024 (+0 im Prüflauf).

## Fragen aus der Welle

Entschieden hat der Orchestrator am 24.09.2026 jeweils nach der Empfehlung a (mit der Baufreigabe); gebaut ist a
(→ Register R‑E18). E18‑Q7 ist eine Notiz und steht als neuer Restpunkt im Konzept (§ 6.3 Nr. 33).

| Frage | Lesarten | Entscheid |
|---|---|---|
| **E18‑Q1** Gegen welche Katalogzeile die Wache hält | (a) die älteste Zeile je Schlüssel — eine Novelle ist eine spätere Jahreszeile; (b) die jüngste | a |
| **E18‑Q2** Die zwei Ressourcen `PREIS_STROMSTEUER_REGELFALL/_REDUZIERT` ohne Leser | (a) streichen; (b) lassen | a |
| **E18‑Q3** Nr. 18 | (a) offen lassen mit Befundsatz, der Umbau nur als eigene Etappe mit Neueinfrierung; (b) in E18 umbauen; (c) als Grenze schließen | a |
| **E18‑Q4** Ort der Anzeige | (a) Dialog „BHKW-Wirtschaftlichkeit", Gruppe 4 und Überlagerung; (b) zusätzlich der Parameterdialog; (c) nur der Parameterdialog | a |
| **E18‑Q5** Umfang der Zeile | (a) Wert, Aktiv, Satzabgleich, Vorschlag und Kohärenz ohne Sperre; (b) nur der Wert | a |
| **E18‑Q6** Der rohe Leseweg | (a) aus `KohaerenzPruefung` nach `StrompreisZerlegungCtrl` verschoben; (b) als Kopie | a |
| **E18‑Q7** (Notiz) Die Unternehmensart ist nur mit BHKW pflegbar (`WirtschaftlichkeitSeite.razor`, `_stand.MitBhkw`) | — § 9b und der Rückweg sind für Projekte ohne BHKW unerreichbar | offen, Restpunkt § 6.3 Nr. 33 |

## Abweichungen und Befunde

1. **Die Aussage des § 6.5 war unzutreffend:** Die Wache hielt die Konstante schon gegen die Saat, nicht nur „Modell
   gegen Konstante"; es fehlten die Testdatenbank und die Robustheit gegen spätere Jahreszeilen (E18/1).
2. **Nr. 18 bleibt offen** — die fünf Rechenweg-Leser sortieren weiterhin ungepflegt vor gepflegt; der Umbau änderte
   die Reihenfolge in 5 von 13 Referenzprojekten und braucht eine eigene Etappe mit neuem Referenzlauf.
3. **Ort der Anzeige:** nicht der Parameterdialog, wie Nr. 16 sagte, sondern der Dialog „BHKW-Wirtschaftlichkeit" —
   dort wird die Unternehmensart gepflegt.
4. **iOS:** Die Gabe steht auch in `EPOS.iOS/Dienste/IosProjektQuelle.cs`; die Zeile ist lokal nicht kompiliert, ein
   iOS-Lauf ist nicht erfolgt (Rückfrage beim Anwender, CLAUDE.md).
5. **Testhost-Regel einmal verletzt:** Im UI-Filterlauf lief die Prüfung auf fremde Testhosts im selben Aufruf, ein
   fremder Testhost lief mit; das Ergebnis war grün.
6. **Unternehmensart nur mit BHKW** (→ E18‑Q7): Die Abnahme braucht ein Projekt mit BHKW; 1017 führt womöglich keines.

## Nachweis

- **Ohne Rechenwirkung:** Anker unberührt; Referenzlauf 13/13 gegen `2026-09-24_R14_Kaelteerzeuger` PASS, 4.207.049
  Werte, alle CSV byte-gleich. Die Anzeige rechnet nichts, was in den Kapitalwert ginge; die Kohärenzprüfung Fall 4
  liest denselben rohen Wert wie zuvor.
- **Gegenprobe der Wache:** eine Kopie der Testdatenbank mit `STROMST_REGELSATZ` 2026 = 21 EUR/MWh — die Wache wird rot
  mit „… 21 EUR/MWh = 2.1 ct/kWh, die Rückfallebene trägt 2.05 ct/kWh" und den drei Orten; die Kopie ist gelöscht.
- **Phase 1** (Worktree `e18`): Kern-Filter Release 0 Fehler, Windows-Schale Debug x64 0 Fehler; SQL-Prüfer 1.825
  Texte / 0 Fundstellen; gefiltert Kern 126/126, UI 94/94 bzw. 500/500 (nach der Korrektur am Englisch-Test); voller
  Lauf EPOS.Kern.Tests 6.124, EPOS.UI.Tests 6.028, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27
  und 1 übersprungen — 13.114 bestanden / 0 Fehler; Designer 10.018.
- **Merge** `e79bffb1` auf `pm19` über `247e2091` ohne Konflikt; Designer 10.024, wiederholbar (+0).
- **Gate:** NACHTRAG-492-GATE
- **CI:** NACHTRAG-492-CI

## Abnahme am Gerät (A‑E18‑1, Windows)

1. **Anteil passt:** Projekt 1024 oder ein anderes Projekt mit BHKW und einem Stromsteueranteil 2,05 ct/kWh aktiv —
   Dialog „BHKW-Wirtschaftlichkeit", Gruppe Stromsteuer: Die Zeile nennt den Träger, „2,050 ct/kWh (aktiv)", den
   Regelsatz 2026, darunter die grüne Kohärenzzeile „Passt zur Unternehmensart".
2. **Wechsel ohne Speichern:** Unternehmensart auf „produzierendes Gewerbe" — die Kohärenzzeile legt den reduzierten
   Satz nahe und verweist auf „Strompreis Details"; „Abbrechen" schreibt nichts.
3. **Nichts erfasst:** Projekt 1030 — „kein Stromsteueranteil erfasst" mit dem Pflegehinweis.
4. **Englische Oberfläche:** dieselben Zeilen auf Englisch.

1017 führt womöglich kein BHKW; dann ist der Dialog dort nicht erreichbar (E18‑Q7).

## Logbuch

Im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026), Stichwort `wirtschaftlichkeit`: „Der Dialog
‚BHKW-Wirtschaftlichkeit' zeigt unter der Unternehmensart den im Strompreis erfassten Stromsteueranteil und ob er zur
gewählten Unternehmensart passt."

## Papiere mit der Statuszeile

Konzept (Kopf mit Codestand `e79bffb1` und Zielversion 130 — Schritt 130 ist #493, nicht E18 —, § 2.2 Gruppe 4, § 6.1
Zeile E18, § 6.3 Nr. 14 und 16 erledigt, Nr. 18 neuer Wortlaut, neuer Restpunkt Nr. 33, § 6.5 Zeile Stromsteuersatz,
§ 7, Anhang), Register (Kopf, Familientafel, neue Familie R‑E18 mit E18‑Q1…Q7, EZ‑9, EZ‑10), Analysepapier (Kopf,
Nachtrag #492, § 5 Zeile E18 ohne Schemaschritt und Stand der Etappen, § 6 Nachtrag zur Vergabe von 130), Protokoll
der Entscheidwege (Kopf, § 0.5, Kopf von § 8, § 8.35, § 8.36), Mockup (Kategorie 5: Gruppe Stromsteuer und
Überlagerung mit der Anzeige- und der Kohärenzzeile, Ressourcentafel; Stand-Absatz), Update-Papier und die Wiki-Quelle
Wirtschaftlichkeit (Dialog „BHKW-Wirtschaftlichkeit", Punkt „Unternehmensart und erfasster Stromsteueranteil"), Index
(Reporting 132 → 133). `Referenzlaeufe/LIESMICH.md` bleibt ohne Nachtrag (kein Schemaschritt); „heute Schemastand 130"
von #493 stimmt.

## Offen

- **Frage E18‑Q7** als Restpunkt (§ 6.3 Nr. 33): die Unternehmensart für Projekte ohne BHKW.
- **Nr. 18** als eigene Etappe mit neuem Referenzlauf; dazu weiter Nr. 10, 11, 13 und 15 des § 6.3.
- **Abnahme am Gerät** A‑E18‑1 (vier Schritte oben); die iOS-Zeile ohne Lauf — Rückfrage beim Anwender.
- **Gate** und **CI** (Nachweis oben).
- Der **Wiki-Sammel-Upload** am 26.09.2026 (Version 1.2.0.4, freigegeben).
