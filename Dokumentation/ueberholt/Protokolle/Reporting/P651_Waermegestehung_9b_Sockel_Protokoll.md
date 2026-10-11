# P651 — § 9b-Abzug der Wärmegestehung mit Sockelbetrag und Deckelung (Protokoll, 02.10.2026)

Statuszeile vorläufig #653 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md) (die Orchestrierung prüft
die Nummer beim Push; mit dieser Welle keine Statuszeile); Auftrag
[`P651_Auftrag_2026-10-02.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P651_Auftrag_2026-10-02.md) der Sitzung „EPOS Plan
Wirtschaftlichkeit". Vorgänger: [`P646_Waermegestehung_Nachlese_Protokoll.md`](P646_Waermegestehung_Nachlese_Protokoll.md)
(#650). Zweig `p651` ab `c6ddc51e5` (Auftrag auf `1d61cd773`, origin/ios_migration_september nach #650).

## Anlass und Entscheid

Konzept § 6.3 Nr. 40 und 41 nannten zwei Grenzen des § 9b-Abzugs der Wärmegestehung (#642): kein Sockelbetrag, keine
Deckelung auf den Stromsteueranteil des Arbeitspreises der Gutschrift. Anwenderentscheid 02.10.2026 (**EZ‑22**): „§ 6.3
Nr. 40 und 41: Empfehlung umsetzen" — der Abzug folgt denselben Regeln wie die Entlastung nach § 9b StromStG im Projekt
(§ 3.8). Kein Schemaschritt (Zielversion 158), keine neue Basis (R30; die Wärmegestehung steht in keiner CSV der Basis).

## Regeln

1. **Sockelbetrag (Nr. 40).** `E_t = max(0, (N + E) × s − S_t) − max(0, N × s − S_t)`; N = Netzbezug nach dem Lauf
   (`SteuerEingabe.NetzbezugMWh` = `NetzbezugFuerStromsteuer`, die Menge der Entlastung des Projekts), E = BHKW-Eigenstrom
   (`KwkEigenGesamtMWh`), s = § 9b-Satz, S_t = `STROMST_SOCKELBETRAG_9B` des Kalenderjahres Förderbeginn + t − 1. Eine
   Funktion `SteuerGutschriftRechner.Entgangene9bEur`, darin `Entlastung9bEur` = max(0, s × N − S) — jetzt auch der Weg der
   Entlastung des Projekts (`StromsteuerEntlastung`, bitgleich). Trägt N den Sockel (N × s > S) oder fehlt er, rechnet sie
   Zeichen für Zeichen E × s. Die § 9b-Korrektur des Ausweises (`VermiedenEntlastung9bJahr`, § 2.6, § 3.6) ruft dieselbe
   Funktion mit der vermiedenen Menge und dem Sockel des Förderbeginns.
2. **Deckel (Nr. 41).** `s_eff = min(s_t, a)` (`SteuerGutschriftRechner.Satz9bWirksam`, nie unter 0). a = Stromsteueranteil
   des Trägers, dessen Arbeitspreis die Gutschrift trägt (Netzeintrag der Aufstellung je Träger, sonst
   `Kaeltestromabrechnung.Projekttraeger` samt Rückfall), aus „Strompreis Details" (`StrompreisZerlegungCtrl.StromsteuerRoh`,
   ct/kWh × 10); abgeschaltete Komponente → a = 0, kein Abzug; ohne gepflegten Anteil (Rückfallträger, keine Zeile, kein
   Wert) a = `STROMST_REGELSATZ` des Jahres (20,50 €/MWh, beim Satz 20,00 kein Deckel). Gelesen nur, wenn der Abzug greifen
   kann (produzierendes Gewerbe, Eigenstrom, Gutschrift); ein Lesefehler wird als Rechenstufe „Energieträger" benannt.
3. **Nachweis.** `Waermegestehung.Nachweis9b` — je wirkender Regel des ersten Jahres ein Text am Laufhinweis der
   Ergebniszeile: `WIRT_GESTEHUNG_9B_DECKEL` (Anteil unter dem Satz), `WIRT_GESTEHUNG_9B_REGELSATZ` (kein Anteil gepflegt,
   auch wenn die Obergrenze nicht greift), `WIRT_GESTEHUNG_9B_SOCKEL` (Abzug statt E × s_eff). Umschlag bleibt Fassung 13.

## Code

- `6f44f146f` Punkt 1: `SteuerGutschriftRechner.Entlastung9bEur` (um Zeile 1396), `Entgangene9bEur` (um 1430),
  `StromsteuerEntlastung` über die Funktion (1372); `Waermegestehung.Entgangene9bEntlastungEur` (um 357) und
  `Entgangene9bReihe` (um 397) mit Netzbezug und Sockel (optionale Parameter); `WirtschaftlichkeitCtrl` Ausweis (um 6689)
  und `BaueWaermeEingabe` (um 7020).
- `2d489b42e` Punkt 2: `Satz9bWirksam` (um 1451), Deckel-Parameter durch die Kette; `WirtschaftlichkeitCtrl.StromsteueranteilNetzEurJeMWh`
  (neu, um 7063; SQL über die bestehenden Lesewege `StromsteuerRoh` und `StrompreisZerlegungCtrl.Read`).
- `a0a8e55aa` Punkt 3: `Waermegestehung.Nachweis9b` (um 443), `BaueWaermeEingabe` mit `out nachweis9b`, angehängt an
  `erg.Hinweis` (um 6918); Ressourcen (de, en); Designer neu (13 563 Einträge, +3, zweiter Lauf +0).

## Tests

- `WaermegestehungTests` 46 → 52: Sockel wirkt bei kleinem Netzbezug ((5 + 50) × 20 − 250 = 850 statt 1.000 €/a; Schwelle
  12,5 MWh; Gleichheit mit der Differenz `Entlastung9bEur` über 18 Paare), getragener Sockel und ohne Sockel bitgleich
  (ohne Toleranz), Reihe mit Sockel je Kalenderjahr; Deckel 10,00 €/MWh → 500 statt 1.000 €/a, Deckel 0 → keine Reihe,
  Sockel und Deckel zusammen 300 €/a; Rückfall Regelsatz 20,50 → bitgleich; Nachweis je Regel.
- `WaermegestehungAnkerTests` 8 → 9: Anker 1024 (0,065409) und 1030 (0,008251) als produzierendes Gewerbe **unverändert**
  — der Netzbezug trägt den Sockel (Entlastung des Projekts 7.492,40 = 387,12 MWh × 20,00 − 250 > 0 bzw. > 0 an 1030), der
  Netzträger 60 führt in 1024 2,05 ct/kWh = 20,50 €/MWh, in 1030 keinen Anteil (Obergrenze Regelsatz 20,50) — beide
  ≥ 20,00; Nachweis 1024 leer, 1030 nennt die Obergrenze. **Neu** 1024 mit Anteil 1,00 ct/kWh: 73,91 MWh × min(20,00;
  10,00) = 739,10 statt 1.478,20 €/a, Gestehung 0,0616162 + 739,10 ÷ Wärmebedarf (12 Stellen); Komponente abgeschaltet: kein
  Abzug, 0,0616162 €/kWh; Kapitalwert −2.784.891,14 und Entlastung 7.492,40 unverändert.
- `VermiedeneMengeOhneEigenerzeugungTests` 7 (`4b314b9cb`): die Nachbildung der § 9b-Korrektur ruft die Kernfunktion
  (Restbezug 250,0 MWh × 20 = 5.000 € > 250 €) — 23.594,0 €/a ohne Toleranz gleich Satz × Menge, Anker 316.159,6 unverändert.
- Gegenproben: Sockel ausgehängt → 2 Einheitstests rot; Deckel ausgehängt → Einheitstest und Anker 1024-Deckel rot;
  Nachweis ausgehängt → Anker 1024-Deckel und 1030 rot; je zurückgebaut.
- Läufe im Worktree (Debug, `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`, kein fremder testhost):
  Kern-Filter 0 Fehler; Auftragsfilter samt Ergebnisansicht, Erlösrubrik, vermiedene Menge, Kapitalwert-Anker und
  Nachweisumschlag `EPOS.Kern.Tests` 410 → 417, `EPOS.UI.Tests` 281, `SpeicherEngine.Tests` 49, alle grün — darin die
  sechs Bericht-Messlatten byte-gleich (kein Projekt der Testdatenbank ist produzierendes Gewerbe), die
  Dokumentationswachen mit diesem Protokoll grün; `SqlDialektPruefer` 2 132 SQL-Texte, 0 Fundstellen; Windows-Schale
  (x64, Debug, `EnableWindowsTargeting`) 0 Fehler.

## Papiere

- Konzept (`70801ee3c`): Kopf, § 2.6, § 3.1 (Kennzahltafel), § 3.6 (Sockel am Ausweis, ohne Deckel), § 3.8 (Formel, eine
  Funktion, Nachweis), § 6.1 (P651), § 6.2 (Absatz, zwei Ankerzeilen), § 6.3 Nr. 40, 41 durchgestrichen. Rechenweg 08
  (Formel). Register EZ‑22 (Wortlaut und Empfehlung), Quellenabsatz, Familientafel (22), Kopf. Wiki-Quelle
  Wirtschaftlichkeit, Anker `waermegestehung` (Gegenlese mit dem Muster aus `CLAUDE.md` ohne Treffer); Logbuch 1.2.0.6:
  der Satz des Auftrags. Index Reporting 152 → 153 mit diesem Protokoll.

## Abweichungen und Lesart

- Punkt 3 nennt eine „Herleitungszeile der Gestehung (Nachweisumschlag, Zeile „§ 9b")" — die gibt es nicht: Die Zerlegung
  der Gestehung wird weder gespeichert noch angezeigt. Der Nachweis steht deshalb als Text am Laufhinweis (`erg.Hinweis`),
  der gespeichert wird und im Bericht unter den Hinweisen steht; Umschlag Fassung 13, kein neues Feld; drei neue
  Ressourcen, weil kein bestehender Text passte.
- Der Ausweis der vermiedenen Stromkosten bekommt den Sockel (Auftrag), keinen Deckel: Er bewertet über die Rollenkosten
  von Bezugs- und Reststromtarif, nicht über den Arbeitspreis eines Trägers; die Hilfsfunktion trägt den Deckel als
  optionalen Parameter.
- Eine abgeschaltete Komponente Stromsteuer gilt als gepflegter Anteil 0 (der Preis enthält keine Stromsteuer, wie
  Kohärenz-Fall 2), nicht als „nicht gepflegt".

## Offen

- Deckel am Ausweis (§ 3.6) — nur mit Anwenderentscheid.
- Wiki-Upload der Quelle Wirtschaftlichkeit mit dem nächsten Sammel-Upload; Logbuch-Version beim Anwender.

## Gate

Gate #651 auf `06d2c4b1a` (Linux, `Werkzeuge/Gate/gate_linux.sh`): Kern-Filter Release 0 Fehler; ChartProben 200 Hashes, alle grün und
gleich der Messlatte `Proben/ChartProben/Messlatte_2026-09-30.sha256`; Tests 18 026 grün, 2 übersprungen, 0 rot (Kern 9 831, UI 7 233,
KiKern 549, SpeicherEngine 386, SpeicherPlanung 27); Dokumentationswachen 35 grün; Referenzlauf 16/16 PASS gegen
`2026-09-30_R30_Stromverbraucher` (5 180 240 Werte, 487/487 CSV byte-gleich); Störlauf `--stoerung ulp` PASS. Nachtest auf dem
Merge `448b1f0eb` (#651, #652): Build 0 Fehler, Testklassen Wärmegestehung, Wirtschaftlichkeit, Stromsteuer, Kohärenz, Bericht,
Messlatten, Absenkung, Kühlung und Wachen 1 450 grün, fremde Klassen Konditionierung, Vorlagenkopierregel, KI-Maskenabdeckung 426 grün.
Windows-Schale 0 Fehler (Agent, x64 Debug, `EnableWindowsTargeting`).

## Commit

Sieben Commits auf `p651` hinter dem Auftrag `c6ddc51e5` (`6f44f146f`, `2d489b42e`, `a0a8e55aa`, `e7db6242b`, `4b314b9cb`,
`70801ee3c`, `868556386`); Merges `06d2c4b1a` mit origin (#650 Nachträge, KP3-Entwurf; eigene Nummer #651) und `448b1f0eb`
(#651 Absenksollwert, #652 E58; eigene Nummer #653). Statuszeile #653 im Folgecommit; Push nach Freigabe des Anwenders;
CI-Vermerk in Nach #653 (e).
