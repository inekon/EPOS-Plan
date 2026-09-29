# P556 — Leistungspreis nur bei stromverwendendem Erzeuger (Protokoll, 29.09.2026)

Statuszeile folgt (#610 vorläufig) in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Auftrag
[`P556_Auftrag_2026-09-29.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P556_Auftrag_2026-09-29.md) der Sitzung „EPOS Plan
Wirtschaftlichkeit". Vorgänger: [`P555_Gruppenregel_Konzeptnachlese_Protokoll.md`](P555_Gruppenregel_Konzeptnachlese_Protokoll.md)
(#603). Zweig `p556` ab `ee1f9323c`.

## Anlass und Entscheid

Konzept § 6.3 Nr. 38 (Statusdatei Nach #555 (c), Nach #603 (d)) fragte, ob ein Stand ohne eigene Stromverwendung, dessen
Netzbezug die Gruppenregel im Vergleich bepreist, den Leistungspreis seines Trägers trägt. Anwenderentscheid 29.09.2026
(**EZ‑17**, Wortlaut im Register): „Der Leistungspreis bei Netzbezug eines Standes ohne stromverwendenden Erzeuger ist nur
für die Lastoptimierung relevant. Der Leistungspreis wird ansonsten nur für stromverwendende Erzeuger verwendet, sofern
angegeben." — und: „Falls der Leistungspreis ein Teil des Preises darstellt, sollte es einen Hinweis geben bzw. in den
Ergebnissen dargestellt sein." Er nimmt den Nebenbefund aus P555 zurück, nach dem der Rückfallträger der Gruppenregel in
der Szenarioabdeckung als Stromträger mit Leistungspreis zählte.

## Code (Commit `bf81729a1`)

- `EPOS.Kern/Allgemein/Bericht/KostenEmissionRechner.cs`: Trägt der Stand `StromImVergleichBepreisen` (die Kopie der
  Gruppenregel, nur an Ständen ohne stromverwendenden Erzeuger), bepreist der Netzbezugsblock Arbeits- und Grundpreis und
  setzt den Leistungspreis (Staffel, Saisonreihe, Satz) nicht an — zugeordneter wie Auslieferungsträger, auch bei
  bekannter Bezugsspitze. Führt der Träger einen, bildet `LeistungspreisNichtAngesetztVermerken` den Hinweis mit Satz
  (`LeistungspreisSatz`: Staffel „60,00 €/(kW·a) bis 1.500 kW, darüber 90,00 €/(kW·a)", Saisonreihe als Summe der zwölf
  Monatssätze, Satz je Monat oder je Jahr) und Träger. Für solche Stände entfallen der Vermerk „Szenario-Leistungspreis
  ohne Wirkung" und die Warnung „Leistungspreis ohne Bezugsspitze".
- `BerichtsDaten.cs`: neues Feld `VariantenDaten.LeistungspreisNichtAngesetzt` (der Hinweis; `null` = kein Fall).
- `WirtschaftlichkeitCtrl.RechneProjekt`: die Zeile hängt an den Hinweisen des Standes (Warnband, Vergleichstabelle,
  Wort- und Excelbericht, Hinweisliste des Berichtslaufs).
- `BerichtsDatenSammler.StromGruppenregelAnwenden`: Meldung als Hinweis an der Stelle des Leistungspreises ohne
  Bezugsspitze.
- `SzenarioAbdeckung.cs`: `SzenarioAbdeckungTraeger.LeistungspreisAusgesetzt`; der Stromträger eines Standes ohne
  stromverwendenden Erzeuger (zugeordnet oder Rückfall) zählt Arbeits- und Grundpreis, den Leistungspreis nie.
- Ressourcen de/en: `WIRT_HINWEIS_LEISTUNGSPREIS_NICHT_ANGESETZT`, `WIRT_LP_SATZ_STAFFEL`, `WIRT_LP_SATZ_SAISON`,
  `WIRT_LP_SATZ_MONAT`; `Resource.Designer.cs` neu erzeugt (13 144 → 13 148 Einträge, zweiter Lauf +0).
- Unverändert: Stände mit Stromverwendung, die Einzelbetrachtung, der Rollentarif, die Speicherauslegung
  (`SpeicherAuslegungVorgabenCtrl`, SpeicherEngine, SpeicherPlanung) und `StromLeistungspreisGepflegt`. Kein Rechenweg der
  Simulation, kein Schemaschritt, keine Basis.

## Tests

- `StromGruppenregelTests` 12 → 20: `Im_Vergleich_setzt_der_Stand_ohne_Verwendung_keinen_Leistungspreis_an` (Katalog
  60 €/(kW·a) und 120 €/a Grundpreis, 40 kW Spitze an beiden Ständen: Stamm 50 + 5.642 + 120 = 5.812,00 €/a ohne
  2.400 €/a Leistungsanteil, Hinweis in allen drei Szenarien; Variante mit Wärmepumpe 4.712,00 €/a mit Leistungsanteil,
  ohne Hinweis), `Im_Bericht_meldet_die_Gruppenregel_den_Leistungspreis_als_Hinweis` (Stufe Hinweis in der Hinweisliste,
  keine Warnung „Bezugsspitze"), `Der_zugeordnete_Stromtraeger_setzt_seine_Staffel_ebenso_nicht_an` (Staffel
  1.500/60/90 bei 2.000 kW — 135.000 €/a nicht angesetzt), `Der_Hinweis_nennt_den_Satz_je_Monat_und_ohne_Leistungspreis_keinen`,
  Theorie `Der_Leistungspreishinweis_kommt_aus_der_Ressource` (4 Schlüssel); die P555-Zählproben auf Arbeits- und
  Grundpreis (22 → 21 Parameter; Seite `ohne + Erdwärme + 2 × 2`), dazu mit gepflegtem Leistungspreis: die Variante mit
  Wärmepumpe zählt ihn (+1), der Stamm nicht.
- `SzenarioAbdeckungTests` 54 → 55: `Der_Leistungspreis_eines_Standes_ohne_Stromverwendung_zaehlt_nie`.
- Gegenprobe mit ausgehängter Regel (Kosten und Zählung): sechs Fälle rot, danach zurückgebaut.
- Läufe im Worktree (Debug, `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`): Kern-Filter 0 Fehler;
  gefiltert (StromGruppenregel, SzenarioAbdeckung, KostenEmission, WirtschaftlichkeitAnker, KapitalwertAnker,
  BerichtVorlagenMesslatte, LokalisierungWirtschaftlichkeitWache, HuellenTextschluesselWache, DokumentationLinkWache,
  RepositoryOrdnungWache) 120/120 — `BerichtVorlagenMesslatteTests` 7/7 (sechs Bericht-Messlatten byte-gleich),
  `WirtschaftlichkeitAnkerTests` 10/10, `KapitalwertAnkerZerlegungTests` 2/2; nach den Papieren
  `DokumentationLinkWache`, `RepositoryOrdnungWache` und `WikiProduktdatenWache` 35/35; `EPOS.Kern.Tests` voll 9 100
  bestanden, 1 übersprungen, 0 rot; `EPOS.UI.Tests` gefiltert (Wirtschaftlichkeit, AnhangE, Szenario, Ergebnisansicht)
  311/311; Windows-Schale (x64, Debug) 0 Fehler, keine Warnung in den geänderten Dateien.

## Papiere

- Konzept: § 3.5 (Gruppenregel mit Arbeits- und Grundpreis, Leistungspreis nur bei Stromverwendung, Hinweis), § 2.11.5
  (Zählregel), § 6.1 (Zeile P556), § 6.2 (Wache 20 Fälle), § 6.3 Nr. 38 geschlossen, Kopf; § 2.13 (5) und § 6.5 nennen den
  Leistungspreis nicht und bleiben.
- Register: EZ‑17 (beide Sätze im Wortlaut), EZ‑14 (Verweis), Quellenabsatz, Familientafel (R‑EZ 17), Kopf.
- `Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`: Verweissatz auf § 3.5 (EZ‑13 bis EZ‑17).
- Statusdatei: Nach #603 (d) — Nr. 38 erledigt mit P556; keine Statuszeile.
- Wiki-Quelle nicht angefasst (die Berichterstellung schreibt sie mit P555‑B um). Vorschlag für die Seite
  „Programm Dokumentation - Wirtschaftlichkeit", Anker `bericht-gruppenregel`, nach „…gilt der Stromträger des Katalogs.":
  „Bepreist wird dieser Netzbezug mit Arbeits- und Grundpreis; einen Leistungspreis des Stromträgers setzt der Vergleich
  bei einer Version ohne Erzeuger, der Strom verwendet, nicht an – er dient dort nur der Lastoptimierung –, und ein Hinweis
  nennt ihn mit Satz und Träger." Im Anker `szenarioabdeckung` „…zählt auch eine Version ohne Erzeuger, der Strom
  verwendet, ihren Stromträger mit Arbeits- und Grundpreis mit…" und „der Leistungspreis beim Stromträger einer Version mit
  stromverwendendem Erzeuger immer".
- Logbuch-Vorschlag (Version beim Anwender): „Im Variantenvergleich der Wirtschaftlichkeit trägt der Netzbezug eines
  Standes ohne stromverwendenden Erzeuger Arbeits- und Grundpreis, aber keinen Leistungspreis; ein Hinweis nennt den
  Leistungspreis des Stromträgers."

## Offen

- **Rollentarif:** Ist die Tarifstruktur aktiv, ersetzt ihr Reststromtarif den Stromanteil samt eigenem
  Leistungspreismodell auch an einem Stand ohne stromverwendenden Erzeuger — nicht angefasst (Konzept § 3.5: „Rollentarif
  und § 9b-Menge wie jeder Stand mit Stromverwendung"); ob EZ‑17 auch dort gilt, entscheidet der Anwender.
- **Zählregel an Ständen mit Stromverwendung ohne zugeordneten Träger** (Nach #603 (c)): Ihr Stromträger zählt über die
  Verwendung der Wärmepumpe Arbeits- und Grundpreis, den Leistungspreis aber nur, wo er gepflegt ist — nicht „immer";
  unverändert.
- **Kostenkapitel des Berichts** (P555‑B): Die Fußzeile der Gruppenregel könnte den nicht angesetzten Leistungspreis
  mitnennen — Sache der Berichterstellung.
- Ein Stand ohne stromverwendenden Erzeuger und ohne Netzbezug unter der Gruppenregel trägt den Grundpreis und den
  Rückfallvermerk seines Trägers (Bestand seit #555); der Leistungspreishinweis steht dann ebenfalls.

## Merge mit #609 (P555‑B) und Abnahme

- Merge `33c9dfa55` mit origin `33f487058` (Welle #609 der Berichterstellung): Die Gruppenzahl des Berichts entsteht dort
  auf einer Kopie (`BerichtsDatenSammler.StromGruppenzahlErmitteln`, `VariantenDaten.Gruppenzahl`); die EZ‑17-Meldung
  hängt jetzt an dieser Kopie (`kopie.LeistungspreisNichtAngesetzt`, Stufe Hinweis), die Gruppenzahl der Fußzeile enthält
  keinen Leistungspreis. Die zwei Berichtsfälle der P556-Tests sind auf den neuen Weg umgestellt (der Stand bleibt
  Einzelzahl). Konflikte: vier Dateien, je beide Seiten übernommen.
- Nach dem Merge: `EPOS.Kern.Tests` gefiltert (Auftrag D, dazu Bericht, Excel, Kennzahlen, Emission, BerichtSzenario,
  WikiProduktdatenWache) 688/688 — `StromGruppenregelTests` 22 (14 aus #609, 8 aus P556), `BerichtSzenarioTests` 6,
  `BerichtVorlagenMesslatteTests` 7/7 byte-gleich; Kern-Filter und Windows-Schale 0 Fehler; Designer unverändert.
- **Abnahme P555‑B:** Tafeln Kosten und Emissionen mit der Einzelzahl, je eine Fußzeile mit Gruppenzahl, Menge und
  Stromverwender (Word unter ihrer Tafel, Excel als Anmerkungszeile), Kapitel Wirtschaftlichkeit mit der Gruppenzahl, App
  unverändert (keine Datei unter `EPOS.UI*`), Messlatten byte-gleich — belegt durch die Fälle aus #609 im lokalen Lauf;
  der Word-Text der Probe gesichtet (Emissionen 7,3 t/a, Energiekosten 5.692 €/a). Benannt, nicht behoben: Die Rücknahme
  gilt für den ganzen Berichtsbaum, auch Übersicht und Kennzahl-Platzhalter der Vorlage lesen die Einzelzahl ohne
  Fußzeile — die Fachvorgabe nannte nur die zwei Tafeln.

## Gate

offen (Orchestrierung).

## Commit

Code `bf81729a1`, Papiere `00e8a229d`, Merge `33c9dfa55`; Abnahme und Nummer #610 im folgenden Papiercommit; Push offen
(Orchestrierung).
