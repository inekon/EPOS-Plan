# P630 — Rollentarif unter EZ‑17, Rückfallträger im Ausweis, Stromsteuer-Kohärenz, Veraltet-Hinweis am Haken (Protokoll, 30.09.2026)

Statuszeile folgt (#633 vorläufig) in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Auftrag
[`P630_Auftrag_2026-09-30.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P630_Auftrag_2026-09-30.md) der Sitzung „EPOS Plan
Wirtschaftlichkeit". Vorgänger: [`P556_Leistungspreis_Gruppenregel_Protokoll.md`](P556_Leistungspreis_Gruppenregel_Protokoll.md)
(#612) und [`P555_Gruppenregel_Konzeptnachlese_Protokoll.md`](P555_Gruppenregel_Konzeptnachlese_Protokoll.md) (#603). Zweig
`p630` ab `243400f40` (origin/ios_migration_september mit #629).

## Anlass und Entscheid

Offen waren die Blöcke Nach #612 (a) (Rollentarif), Nach #603 (b) (Seite nach einem Haken) und (c) (Zählregel E9b) sowie
Konzept § 6.3 Nr. 39 (Stromsteuer-Kohärenz). Anwenderentscheid 30.09.2026 (**EZ‑18**, Wortlaut im Register): „Führe nächste
Welle aus, #630 nach fetch: Punkte 1, 3 und 4 zusammen, dazu Punkt 2" — alle vier nach der Empfehlung der Wirtschaftlichkeit.

## Analyse — was war, was sich ändert

1. **Rollentarif.** Die Gruppenregel-Kopie eines Standes ohne stromverwendenden Erzeuger rechnete bei wirksamer
   Tarifstruktur `RechneRollentarif` wie jeder Stand: Der Reststrombetrag samt Leistungsanteil seines Modells ersetzte den
   Stromanteil, und ein Hinweis nannte den Leistungspreis des Stromträgers, dessen Preise der Tarif gerade ersetzt hatte.
   Jetzt rechnen beide Rollen der Kopie ohne Leistungspreis — auch die Bezugsrolle, damit die vermiedenen Kosten keinen
   Leistungsanteil ausweisen, den keine Anlage vermeidet (Folge der Umsetzung, im Auftrag nicht genannt) —, und
   `WIRT_HINWEIS_LEISTUNGSPREIS_TARIF_NICHT_ANGESETZT` nennt das Modell (`TARIF_LM_*`) an der Stelle des Trägerhinweises.
2. **Veraltet am Haken.** Ein Haken tauschte die Ansicht, die gespeicherten Ergebnisse blieben die des letzten Laufs; das
   Warnband kannte nur „älterer Lauf" und „Simulation veraltet". Die gespeicherten Ergebnisse tragen ihren Lauf nicht —
   die Hülle merkt ihn sich deshalb selbst (`_ergebnisLauf`: nach „Berechnen" die gerechneten Stände, vorher einmal beim
   ersten Laden die Stände der Wahl) und fragt je Ansicht die Regel des Kerns
   (`WirtschaftlichkeitCtrl.StromGruppenregelGeaendert`: andere Menge der Stromverwender, ein Stand ohne Stromverwendung
   aus dem gerechneten Lauf noch gewählt); das Band `WIRT_BAND_GRUPPENREGEL_VERALTET` steht im vorhandenen Warnband über
   „Neu berechnen".
3. **Rückfallträger im Ausweis.** `SzenarioAbdeckung.TraegerMitVerbrauch` nahm an einem Stand mit Stromverwendung nur den
   zugeordneten Stromträger. Ohne Zuordnung fehlte er am BHKW-Stand 1018 ganz (die Verwendungsliste führt dort nur Erdgas),
   an Ständen mit Wärmepumpe zählte er über deren Verwendung mit Arbeits- und Grundpreis, den Leistungspreis nur gepflegt.
   Jetzt zählt der Rückfallträger der Kostenrechnung (`Emissionsquelle.KatalogStromTraeger`) als Stromträger. Betroffen
   sind in der Testdatenbank nicht nur 1018, 1026 und 1029, sondern alle zwanzig Stände mit Stromverwendung ohne
   Stromträger (1006–1009, 1018, 1026–1029, 1031, 1032, 1039–1046, 1049); die Bericht-Messlatten nicht (1030 führt 60, die
   synthetische Gruppe keine Anlagen).
4. **Stromsteuer-Kohärenz.** Die Stromseite schwieg nur an Ständen ohne eigene Stromverwendung (also unter der
   Gruppenregel). An einem Stand mit Stromverwendung ohne Zuordnung stand bei gebuchtem § 9b schon Fall 2, aber mit dem
   Grund „dem Projekt ist kein Strom-Energieträger zugeordnet", obwohl der Auslieferungsträger den Netzbezug bepreist.
   Jetzt prüft sie gegen den Rückfallträger (`KostenEmissionRechner.StromTraegerRueckfall`, dieselbe Wahl wie die Kosten;
   unter der Gruppenregel über `KohaerenzLauf.StromImVergleichBepreist`). Einen Stromsteueranteil führt nur eine
   Projektzeile eines zugeordneten Trägers — am Rückfallträger steht deshalb stets Fall 2 mit „Stromsteueranteil des
   Auslieferungsträgers „…" nicht gepflegt" (`KOH_GRUND_RUECKFALL_STROMSTEUER`).

## Code

- `fe05913cb` Rollentarif: `StromTarifRechner.LeistungspreisGepflegt`, `.OhneLeistungspreis`; `WirtschaftlichkeitCtrl`
  (`RechneRollentarif`, `ProjektEingabe.LeistungspreisTarifNichtAngesetzt`, `HINWEIS_LEISTUNGSPREIS_TARIF_NICHT_ANGESETZT`,
  `Leistungsmodelltext`, Hinweiswahl in `RechneProjekt`).
- `aa688d81c` Ausweis: `SzenarioAbdeckung.TraegerMitVerbrauch` mit `stromRueckfall`, `IstStrom` für ihn.
- `34c499fdd` Kohärenz: `KostenEmissionRechner.StromTraegerRueckfall` (die Wahl einmal, Kostenrechnung unverändert),
  `KohaerenzLauf.StromImVergleichBepreist`, `KohaerenzPruefung.Stromseite`.
- `562dd61e9` Veraltet-Band: `WirtschaftlichkeitCtrl.StromGruppenregelGeaendert`, `WirtschaftlichkeitSeiteGaben`
  (`_ergebnisLauf`, `GruppenregelVeraltet`), `ErgebnisAnsicht.GruppenregelVeraltet`, `WirtschaftlichkeitSeite`
  (Band, `GruppenregelVeraltetText`, Prüfhilfe).
- Drei Ressourcen de/en, `Resource.Designer.cs` neu erzeugt (13 437 → 13 440 Einträge, zweiter Lauf +0). Kein Rechenweg
  der Simulation, kein Schemaschritt, keine Basis, kein Anker.

## Tests

- `StromGruppenregelTests` 22 → 27: Rollentarif am Prüfstand (Stamm 5.308,40 €/a ohne 220,82 €/a Leistungsanteil,
  vermiedene Leistung 0; Variante mit Leistungsanteil 2.192,24 €/a), Klartext der Staffel und kein Hinweis ohne
  Leistungspreis, Ressourcenfall; § 9b am Stamm mit Kohärenzzeile am Rückfallträger; Veraltet-Band der Seite (Gruppe
  1026: „Andere WP" ohne, „Erdwärme" mit Band, Referenzwahl hin und zurück, „Berechnen" räumt, Gegenrichtung). Angepasst:
  die Zählprobe erwartet `nurVariante` statt `nurVariante + 1` — der Rückfallträger der Variante zählt seinen
  Leistungspreis immer; hergeleitet Stamm 11 + 2 = 13, Variante 11 + 2 + 3 + 2 = 18.
- `SzenarioAbdeckungTests` 55 → 56: 1018 zählt m = 11 + 2 + 3 = 16 (vorher 13), gleich dem Ausweis mit zugeordnetem Träger.
- `EnergiekostenGrundTests` 35 → 36: die Gruppenregel gegen den Rückfallträger (WARNUNG mit Betrag, Ressource de/en);
  angepasst: die Probe mit Wärmepumpe erwartet den neuen Grund statt „kein Strom-Energieträger zugeordnet".
- bUnit `WirtschaftlichkeitErgebnisansichtTests` 28 → 29: Band am Haken der einzigen Stromvariante, kein Band am Haken
  ohne Wirkung, kein Lauf ohne Zuruf, „Neu berechnen" räumt.
- Gegenproben mit ausgehängter Regel: Rollentarif 2 rot, Ausweis 2 rot, Kohärenz 3 rot, Veraltet 1 rot im Kern und 1 im
  bUnit-Test; danach zurückgebaut.
- Läufe im Worktree (Debug, `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`): Kern-Filter 0 Fehler;
  `EPOS.Kern.Tests` gefiltert (StromGruppenregel, SzenarioAbdeckung, Kohaerenz, StromTarif, Wirtschaftlichkeit,
  BerichtVorlagenMesslatte, LokalisierungWirtschaftlichkeitWache, HuellenTextschluesselWache, KapitalwertAnker,
  EnergiekostenGrund) 209/209 — `BerichtVorlagenMesslatteTests` 7/7 (sechs Bericht-Messlatten byte-gleich),
  `WirtschaftlichkeitAnkerTests` 10/10, `KapitalwertAnkerZerlegungTests` 2/2; nach den Papieren `DokumentationLinkWache`,
  `RepositoryOrdnungWache` und `WikiProduktdatenWache` 35/35; `EPOS.UI.Tests` gefiltert (Wirtschaftlichkeit,
  Ergebnisansicht, AnhangE, Szenario, KiMaskenabdeckung) 333/333; Windows-Schale (x64, Debug) 0 Fehler.

## Papiere

- Konzept: Kopf (Stand 30.09.2026, Zielversion 158, Schritte 155–158, Basis R28 — der Auftrag nannte R27, am
  Ausgangsstand gilt mit #627 R28), § 2.11.5 (Zählregel: Rückfallträger), § 2.15 (Veraltet-Hinweis am Haken), § 3.5
  (Rollentarif unter der Leistungspreisregel, EZ‑13 bis EZ‑18), § 3.6 (vermiedene Leistung 0), § 3.9 (Stromseite am
  Rückfallträger), § 6.1 (Zeile P630, P556 auf #612), § 6.2 (Wachen), § 6.3 Nr. 39 geschlossen.
- Register: EZ‑18 mit dem Wortlaut und den vier Punkten, EZ‑17 auf #612 mit Verweis, Quellenabsatz, Familientafel
  (R‑EZ 18), Kopf. `Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`: Verweis auf EZ‑13 bis EZ‑18.
- Statusdatei: Nach #612 (a), Nach #603 (b), (c) und die Nr. 39 in (d), Nach #555 (d) als erledigt mit #633; keine
  Statuszeile.
- Wiki-Quelle `Programm Dokumentation - Wirtschaftlichkeit.wiki`: Anker `vergleichswahl` (Band am Haken), `hinweisband`
  und `neu-berechnen` (Anlass des Bandes), `szenarioabdeckung` (Rückfallträger), `kohaerenz` (§ 9b am Katalogträger),
  `bericht-gruppenregel` (Leistungspreis des Reststromtarifs); Gegenlese mit dem Muster aus `CLAUDE.md` ohne Treffer.
- Logbuch `Wiki_Update_2026-09-26.md`, Version 1.2.0.6: die zwei Sätze des Auftrags (Version bestätigt der Anwender
  beim Upload).

## Offen

- Die Hülle nimmt beim ersten Laden die Wahl als Lauf der gespeicherten Ergebnisse. Wurde die Wahl vorher auf
  „Übersicht" oder „Kosten" geändert oder stammt das Ergebnis eines neu angehakten Standes aus einem älteren Lauf, meldet
  die Seite nichts — ohne einen gespeicherten Laufvermerk (Schemaschritt) nicht bestimmbar.
- Am Rückfallträger kann die Kohärenz nie „stimmig" werden: Den Stromsteueranteil pflegt „Strompreis Details" nur an
  einem zugeordneten Träger. Der Ausweg ist die Zuordnung; der Dialog „BHKW-Wirtschaftlichkeit" zeigt unter der
  Unternehmensart weiter „kein Strom-Energieträger zugeordnet".
- Die Bezugsrolle ohne Leistungspreis an der Kopie (Punkt 1) zur Bestätigung.

## Gate

Gate #633 auf `acaf32a4a` (Windows, Worktree `p630`, 30.09.2026 12:42–12:56): Kern-Filter Release 0 Fehler; ChartProben 194
Bilder, alle grün — die lokale Windows-Messlatte wurde von 185 auf 194 Hashes nachgezogen (neun Kalenderbilder aus KP2 K4,
zwölf Stapelbilder aus der Stufenregel des Stapels, alle aus Wellen der Nachbarn; Sicherung der alten Liste liegt neben ihr);
Tests 17 611 grün, 2 übersprungen, 0 rot (Kern 9 526, UI 7 123, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27);
Dokumentationswachen 35 grün; Referenzlauf der sieben CI-Projekte gegen `2026-09-30_R29_Kesseltakten`: 7/7 PASS,
2 497 978 Werte; Windows-Schale 0 Fehler (Agent).

## Commit

Code `fe05913cb` (Rollentarif), `aa688d81c` (Rückfallträger im Ausweis), `34c499fdd` (Stromsteuer-Kohärenz), `562dd61e9`
(Veraltet-Band), Papiere `bd19c43b0`, Merge `acaf32a4a` mit origin (#629-Nachträge, Basis R29; Konflikte im Index-Zähler und
im Logbuch 1.2.0.6 beidseitig übernommen); Statuszeile #633 und das Gate-Feld im Papier-Commit der Orchestrierung (Zweig `p630`,
Push auf `ios_migration_september`).

## Nachtrag (02.10.2026)

Die Hinweiszeile nennt im Rollentarif den Leistungspreis des Trägers wieder immer und den des Reststromtarifs nur
zusätzlich, wenn er sich unterscheidet — nicht mehr an seiner Stelle (Anwenderentscheid 02.10.2026, Variante 1;
dieselbe Regel wie die Fußzeile der Kostentafel). Umsetzung und Prüfungen: Protokoll
[P555B § 11](../Bericht/P555B_Kostenkapitel_Fusszeile_Protokoll.md#11-nachtrag-hinweiszeile-der-wirtschaftlichkeit-nach-derselben-regel-02102026).
