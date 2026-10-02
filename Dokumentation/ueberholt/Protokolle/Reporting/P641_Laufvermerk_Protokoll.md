# P641 — Laufvermerk je Ergebnis, Bezugsrolle bestätigt, Kohärenz am Rückfallträger als benannte Grenze (Protokoll, 02.10.2026)

Statuszeile #644 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Auftrag
[`P641_Auftrag_2026-10-02.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P641_Auftrag_2026-10-02.md) der Sitzung „EPOS Plan
Wirtschaftlichkeit". Vorgänger: [`P630_Rollentarif_Rueckfalltraeger_Haken_Protokoll.md`](P630_Rollentarif_Rueckfalltraeger_Haken_Protokoll.md)
(#633). Zweig `p641` ab `d14b5bbf3` (Auftrag auf `4cfbf0171`, origin/ios_migration_september mit #640).

## Anlass und Entscheid

Offen waren die drei Punkte aus Nach #633: (a) die Bezugsrolle ohne Leistungspreis an der Gruppenregel-Kopie zur
Bestätigung, (b) der Lauf gespeicherter Ergebnisse, den die Seite nur kannte, solange sie ihn selbst gerechnet hatte,
(c) die Kohärenz am Rückfallträger, die nie „stimmig" wird. Anwenderentscheid 02.10.2026 (**EZ‑19**, Wortlaut im
Register): „Nach #633 (a): Empfehlung, (b): Empfehlung, (c): Empfehlung". Kein nummerierter Schemaschritt (Zielversion
158), kein Rechenweg der Simulation, keine neue Basis (R30).

## Analyse — was war, was sich ändert

1. **Bezugsrolle bestätigt.** Kein Code. Vermerk „bestätigt 02.10.2026" an EZ‑18, ein Satz in Konzept § 3.5.
2. **Laufvermerk.** Die gespeicherten Ergebnisse trugen ihren Lauf nicht; die Hülle nahm beim ersten Laden die Wahl
   als Lauf (`_ergebnisLauf`). War die Wahl vorher auf „Übersicht" oder „Kosten" geändert, oder stammte das Ergebnis
   eines neu angehakten Standes aus einem älteren Lauf, schwieg das Band `WIRT_BAND_GRUPPENREGEL_VERALTET`. Jetzt trägt
   jede Zeile von `Tab_ErgebnisWirtschaftlichkeit` die Stände ihres Laufs in der nullbaren Spalte `Lauf_Staende`
   (TEXT, aufsteigend mit Komma, `1026,1027,1029`; Stamm, angehakte Varianten, Referenz = `daten.Varianten`). Die
   Seite prüft je Ansicht die Läufe der gewählten Stände gegen die Wahl: Tragen sie verschiedene Vermerke, wird jeder
   dieser Läufe mit `StromGruppenregelGeaendert` geprüft, und das Band steht, sobald einer die Gruppenregel ändert —
   kein Band bei jedem Haken, kein automatisches Rechnen. Eine Zeile ohne Vermerk (Altbestand, NULL) zählt zum Lauf,
   den die Seite selbst kennt (`_ergebnisLauf`: nach „Berechnen" die gerechneten Stände, sonst die Wahl beim ersten
   Laden) — für reinen Altbestand also das Verhalten von #633. Der Kosten-Reiter rechnet über
   `BerechnenMitReferenz` → `Berechnen` → `Berechne`/`Persistiere` und schreibt denselben Vermerk.
3. **Kohärenz am Rückfallträger.** Kein Code: bleibt benannte Grenze, ein Satz in Konzept § 3.9.

**Lesart des Auftrags.** „Tragen die gewählten Stände verschiedene Vermerke, gilt der Lauf als geändert" ist als Prüfung
jedes dieser Läufe gegen die Wahl gebaut, nicht als Band ohne Prüfung: Unter derselben Gruppenregel rechnen zwei Läufe
dieselben Zahlen, ein Band wäre dort ein falscher Alarm; der Auftrag verlangt „nur bei geänderter Gruppenregel wie
bisher". Der Testfall „gewählter Stand mit anderem Vermerk → Band" nimmt deshalb einen Lauf mit anderer Gruppenregel,
die Gegenprobe „anderer Vermerk ohne Wirkung auf die Regel → kein Band" steht daneben.

## Code

- `0e5fa2c3d` Kern: `WirtschaftlichkeitCtrl.SPALTE_LAUF_STAENDE`, `StelleTabellenSicher` (vierte Ergebnisspalte über
  `SpalteSicher`, TEXT), `Berechne` setzt `WirtschaftlichkeitErgebnis.LaufStaende` an jedem Ergebnis des Laufs,
  `Persistiere` schreibt ihn (54 Platzhalter, leer → NULL), `LadeErgebnisse` liest ihn (`Text`, leer bei NULL und ohne
  Spalte); `Laufvermerk` (neu: `Schreiben`, `Lesen`, `Laeufe`, `GruppenregelVeraltet`).
- `f9b94c9f1` Hülle: `WirtschaftlichkeitSeiteGaben.GruppenregelVeraltet` fragt `Laufvermerk.GruppenregelVeraltet` mit
  den geladenen Ergebnissen, den Ständen der Wahl und `_ergebnisLauf` als Lauf der Zeilen ohne Vermerk.
- `9b022053c` Testdatenbank: `Werkzeuge/Testdatenbankschema` legt `Lauf_Staende` über `SpalteSicherstellen` an (kein
  eigener Schritt, Muster `Nachweis_Json`); `Kenndaten_Test.sqlite` nachgezogen — 1 Spalte, `--trocken` danach 0 offen,
  Marker 158, `integrity_check` ok, 48 Bestandszeilen NULL, 81 137 664 Byte, LFS-SHA-256
  `c1a153dc227c6f7b91a71657eb9705ba7056116551f900259fad5d3cfc543eed` (vorher `5d59041f…`), als LFS-Zeiger committet.
- Kein neuer Ressourcenschlüssel (das Band nutzt `WIRT_BAND_GRUPPENREGEL_VERALTET`), Designer unverändert. Der
  Referenzlauf liest `Tab_ErgebnisWirtschaftlichkeit` nicht (geprüft: keine Fundstelle in `EPOS.Referenzlauf` und im
  Simulationsweg), die Basis R30 bleibt.

## Tests

- `StromGruppenregelTests` 27 → 31 (der Auftrag nannte 23; gezählt am Ausgangsstand 27): Vermerk schreiben und lesen
  und die Läufe der Wahl ohne Datenbank; Rundreise `Persistiere` → `LadeErgebnisse`, eine Zeile mit NULL liest leer;
  `Laufvermerk.GruppenregelVeraltet` an Gruppe 1026 (gleicher Vermerk kein Band, Stand aus einem Lauf mit anderer
  Gruppenregel Band, Altbestand kein Band, anderer Vermerk ohne Wirkung kein Band); die Seite nach dem Seitenwechsel
  (geteilte Vergleichswahl, neue Hülle je Laden): Altbestand ohne Band, nach „Berechnen" der Gruppe und „Erdwärme" ab
  Band, gleiche Wahl ohne, Stand aus älterem Lauf wieder angehakt Band, „Berechnen" schreibt an alle Zeilen den neuen
  Vermerk und räumt das Band.
- `WirtschaftlichkeitCtrlTabellenTests` 4 → 5: `StelleTabellenSicher` legt `Lauf_Staende` (TEXT, nullbar) an, ein zweiter
  Lauf ändert das Schema nicht, keine Vorsorgewarnung; die bestehende Vorsorgeprobe nennt die vierte Spalte.
- bUnit `WirtschaftlichkeitErgebnisansichtTests` 29 → 30: neu geladen mit einer Wahl, die vom gespeicherten Lauf
  abweicht, zeigt die Seite das Band sofort; mit der Wahl des Laufs nicht; kein Lauf.
- Gegenproben (je Regel ausgehängt, danach zurückgebaut): Hülle mit der Prüfung von #633 — Seitenwechsel rot;
  `Persistiere` ohne Vermerk — Rundreise und Seitenwechsel rot; `StelleTabellenSicher` ohne die Spalte — beide
  Vorsorgeproben, Rundreise und Seitenwechsel rot; `Laufvermerk.Laeufe` ohne Vermerk — beide Vermerkproben und
  Seitenwechsel rot, im bUnit-Test die neue Probe rot.
- Läufe im Worktree (Debug, `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`, kein fremder testhost):
  Kern-Filter 0 Fehler; der Auftragsfilter (StromGruppenregel, WirtschaftlichkeitCtrlTabellen, SzenarioAbdeckung,
  Wirtschaftlichkeit, BerichtVorlagenMesslatte, LokalisierungWirtschaftlichkeitWache, HuellenTextschluesselWache,
  DokumentationLinkWache, RepositoryOrdnungWache, WikiProduktdatenWache, KapitalwertAnker, Ergebnisansicht) über den
  Kern-Filter: `EPOS.Kern.Tests` 315/315 — darin `StromGruppenregelTests` 31, `WirtschaftlichkeitCtrlTabellenTests` 5,
  `SzenarioAbdeckungTests` 56, `BerichtVorlagenMesslatteTests` 7/7 (sechs Bericht-Messlatten byte-gleich),
  `WirtschaftlichkeitAnkerTests` 10/10, `KapitalwertAnkerZerlegungTests` 2/2, `DokumentationLinkWacheTests` 9,
  `RepositoryOrdnungWacheTests` 15, `WikiProduktdatenWacheTests` 11 —, `EPOS.UI.Tests` 268/268 (darin
  `WirtschaftlichkeitErgebnisansichtTests` 30), `SpeicherEngine.Tests` 48/48; `Werkzeuge/Testdatenbankschema --trocken`
  0 offen; `SqlDialektPruefer` 2 147 SQL-Texte, 0 Fundstellen; Windows-Schale (x64, Debug, `EnableWindowsTargeting`)
  0 Fehler.

## Papiere

- Konzept: Kopf (Stand 02.10.2026, Codestand `54f8aaa9a`, Zielversion 158, P641 ohne Schritt, Spalte `Lauf_Staende`
  nachgezogen, Basis R30), § 2.15 (Laufvermerk, Altbestand ohne Vermerk), § 3.5 (Bezugsrolle bestätigt), § 3.9
  (Grenze am Rückfallträger), § 6.1 (Zeile P641; P630 auf #633), § 6.2 (Wachen), § 6.5 (vier Ergebnisspalten).
- Register: EZ‑19 mit Wortlaut und den drei Punkten, Vermerk „bestätigt 02.10.2026" an EZ‑18 (dort #633 statt
  vorläufig), Quellenabsatz, Familientafel (R‑EZ 19), Kopf.
- Statusdatei: Nach #633 (a), (b), (c) als erledigt mit #644; keine Statuszeile.
- `Referenzlaeufe/LIESMICH.md`: nachgezogene Spalte, neue LFS-SHA und Bytezahl, Basis unverändert.
- Wiki-Quelle `Programm Dokumentation - Wirtschaftlichkeit.wiki`, Anker `hinweisband`: das Band erscheint auch nach
  einem Wechsel der Seite und für eine Version aus einem älteren Lauf; Gegenlese mit dem Muster aus `CLAUDE.md` ohne
  Treffer.
- Logbuch `Wiki_Update_2026-09-26.md`, Version 1.2.0.6: der Satz des Auftrags (Version bestätigt der Anwender beim
  Upload).
- Index `Dokumentation/LIESMICH.md`: Reporting 149 → 150 mit diesem Protokoll.

## Offen

- Wiki-Upload der Quelle Wirtschaftlichkeit mit dem nächsten Sammel-Upload (wie Nach #633 (d)).
- Anwenderprobe unter Windows: Wahl auf „Übersicht" ändern, zur Wirtschaftlichkeit wechseln — Band ohne Haken.

## Gate

Gate #644 auf `54f8aaa9a` (Linux, `Werkzeuge/Gate/gate_linux.sh`): Kern-Filter Release 0 Fehler; ChartProben 200 Hashes, alle grün
und gleich der Messlatte `Proben/ChartProben/Messlatte_2026-09-30.sha256`; Tests 17 878 grün, 2 übersprungen, 0 rot (Kern 9 708,
UI 7 208, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27); Dokumentationswachen 35 grün; Referenzlauf 16/16 PASS gegen
`2026-09-30_R30_Stromverbraucher` (5 180 240 Werte innerhalb der Toleranz, 487/487 CSV byte-gleich); Störlauf `--stoerung ulp`
gegen den ungestörten Lauf PASS. Windows-Schale 0 Fehler (Agent, x64 Debug, `EnableWindowsTargeting`).

## Commit

Sechs Commits auf `p641` (`0e5fa2c3d` Kern, `f9b94c9f1` Hülle, `5a4fab7be` Tests, `9b022053c` Testdatenbank, `84b315ede` Papiere,
`dfaa071f3` Protokoll) hinter dem Auftrag `d14b5bbf3`; Merge `f9aa90012` mit origin (#641 SV1, Basis R30; eigene Fundstellen von
#641 auf #644 und von R29 auf R30 nachgezogen); zweiter Merge `54f8aaa9a` mit origin (#642 Wärmegestehung, #643 SV2), eigene Nummer #644. Statuszeile #644 im Folgecommit; Push nach Zuruf, Nummer beim Push gegen origin
geprüft; CI-Vermerk in Nach #644 (c).
