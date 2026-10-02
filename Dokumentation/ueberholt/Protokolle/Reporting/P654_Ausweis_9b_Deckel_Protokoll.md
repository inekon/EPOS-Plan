# P654 — Deckel an der § 9b-Korrektur des Ausweises der vermiedenen Stromkosten (Protokoll, 02.10.2026)

Statuszeile vorläufig #654 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md) (die Orchestrierung prüft
die Nummer beim Push; mit dieser Welle keine Statuszeile); Auftrag
[`P654_Auftrag_2026-10-02.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P654_Auftrag_2026-10-02.md) der Sitzung „EPOS Plan
Wirtschaftlichkeit". Vorgänger: [`P651_Waermegestehung_9b_Sockel_Protokoll.md`](P651_Waermegestehung_9b_Sockel_Protokoll.md)
(#653). Zweig `p654` ab `2ec61ab69` (Auftrag auf `d70c674dd`, origin/ios_migration_september mit #653).

## Anlass und Entscheid

P651 gab der § 9b-Korrektur des Ausweises der vermiedenen Stromkosten (Konzept § 3.6) den Sockel, keinen Deckel — benannte
Grenze, Statusdatei Nach #653 (c). Anwenderentscheid 02.10.2026 (**EZ‑23**): „(Deckel)" — der Ausweis bekommt denselben
Deckel wie der Abzug der Gestehung; der wirksame § 9b-Satz ist höchstens der Stromsteueranteil des Preises, mit dem der
Eigenstrom bewertet wird. Kein Schemaschritt (Zielversion 158), keine neue Basis (R30; der Ausweis steht in keiner CSV der
Basis).

## Regel

```
Korrektur = max(0, (N + M) × s_eff − S) − max(0, N × s_eff − S)
s_eff     = min(s, a)   a = Stromsteueranteil des Trägers der Bezugsrolle (= Netzträger) [€/MWh]
                            ohne gepflegten Anteil: Regelsatz der Stromsteuer des Jahres; abgeschaltet: 0
N = Netzbezug der Entlastung des Projekts, M = vermiedene Menge, s = § 9b-Satz, S = Sockelbetrag (Förderbeginn)
```

Eine Funktion (`SteuerGutschriftRechner.Entgangene9bEur`, Deckel-Parameter seit P651), eine Ermittlung des Anteils
(`WirtschaftlichkeitCtrl.StromsteueranteilNetzEurJeMWh`) für Ausweis und Gestehung, gelesen höchstens einmal je Ergebnis und
nur bei § 9b-Satz > 0. Ohne Deckel unter dem Satz Zeichen für Zeichen das Ergebnis von P651.

## Code

- `292f1b939` Punkt 1: `WirtschaftlichkeitCtrl.RechneProjekt` (um Zeile 6676–6725) — `Func<double?> stromsteueranteil9b`
  (eine Ermittlung), Ausweis ruft `Entgangene9bEur(…, anteil ?? regelsatz)`; `BaueWaermeEingabe` (um 6999) nimmt die
  Ermittlung als Parameter statt eines eigenen Lesens (um 7067).
- `09f0c97fd` Punkt 2: `WirtschaftlichkeitCtrl.Nachweis9bAusweis` (neu, um 7095) — Text nur, wenn der Deckel wirkt;
  angehängt an `erg.Hinweis` (um 6751). Ressourcen `WIRT_VERMIEDEN_9B_DECKEL`, `WIRT_VERMIEDEN_9B_REGELSATZ` (de, en);
  Designer neu (13 571 Einträge, +2, zweiter Lauf +0).

## Tests

- `VermiedenAusweis9bDeckelTests` (neu, 6): Kernfunktion — Anker 23.594,0 mit Deckel 20,50 bitgleich, 1,00 ct/kWh
  11.797,0, Anteil 0 → 0; Lauf 1030 (Rollentarif, Stundenreihen des Prüffalls B6, produzierendes Gewerbe, vermiedene Menge
  432,3 MWh): ohne gepflegten Anteil 8.646,00 €/a (Obergrenze Regelsatz 20,50, kein Deckel, kein Nachweis), Anteil
  1,00 ct/kWh 4.323,00 €/a (halbiert, effektiv + 4.323,00, Kapitalwert und Entlastung des Projekts unverändert, Nachweis
  „rechnet mit 10,00 €/MWh statt 20,00 €/MWh"), Komponente aus 0 (Nachweis 0,00), Anteil 2,05 bitgleich; Nachweis je Fall
  ohne Datenbank (auch Regelsatz unter dem Satz).
- `VermiedeneMengeOhneEigenerzeugungTests` 7: der Anker 23.594,0 / 316.159,6 €/a rechnet mit Deckel 20,50 — unverändert
  (Anteil 2,05 ≥ 2,00 ct/kWh).
- Gegenproben: Deckel ausgehängt → 2 rot; Regelsatz statt Anteil → 2 rot; Anteil ?? 0 statt Regelsatz → 3 rot; Nachweis
  ausgehängt → 2 rot; je zurückgebaut.
- Läufe im Worktree (Debug, `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`, kein fremder testhost):
  Kern-Filter 0 Fehler; Auftragsfilter vorher `EPOS.Kern.Tests` 262, `EPOS.UI.Tests` 269, `SpeicherEngine.Tests` 49 → nach
  Punkt 1 Kern 267; mit Kapitalwert-Anker, Ergebnisansicht, Nachweisumschlag und Zeilenformat Kern 406, UI 273,
  SpeicherEngine 49, alle grün. Unverändert: `WaermegestehungTests` 52, `WaermegestehungAnkerTests` 9,
  `WirtschaftlichkeitAnkerTests` 10, `KapitalwertAnkerZerlegungTests` 2, `StromGruppenregelTests` 34, `ErloesrubrikTests` 34,
  `BerichtVorlagenMesslatteTests` 7 (sechs Bericht-Messlatten byte-gleich), bUnit `WirtschaftlichkeitErgebnisansichtTests` 32.
  Windows-Schale (x64, Debug, `EnableWindowsTargeting`) 0 Fehler.

## Papiere

- Konzept: Kopf (Codestand folgt mit P654), § 2.6 (Formel mit Deckel), § 3.6 (Absatz „Sockel und Deckel an der
  § 9b-Korrektur", Grenze gestrichen), § 3.8, § 6.1 (P654), § 6.2 (Ankerzeile 1030, Vermerk am Anker 316.159,6), § 6.5.
  Register EZ‑23, Quellenabsatz, Vermerk an EZ‑22, Familientafel (23), Kopf. Rechenweg 07 (Formel). Wiki-Quelle
  Wirtschaftlichkeit, Anker `vermiedene-kosten`, ein Satz (Gegenlese mit dem Muster aus `CLAUDE.md` ohne Treffer); kein
  Logbuchsatz. Index Reporting 153 → 154 mit diesem Protokoll.

## Abweichungen und Lesart

- **Träger der Bezugsrolle.** Die Rollen des Tarifs führen keinen eigenen Träger (`Tab_ProjektTarif` kennt keinen, „Strompreis
  Details" gibt es nur je Träger); der Rollentarif ersetzt die Preise des Netzträgers (`RechneRollentarif`). Der Träger der
  Bezugsrolle ist deshalb der Netzträger — die Ermittlung ist für beide Zweige des Auftrags dieselbe. Ohne aktiven Rollentarif
  entsteht keine vermiedene Menge, der Zweig „sonst" trägt keinen Ausweis.
- **Nachweis.** Die Texte `WIRT_GESTEHUNG_9B_*` beginnen mit „Wärmegestehungskosten:" und sprechen vom Abzug; zwei neue
  Ressourcen statt Wiederverwendung. Ort wie in P651 der Laufhinweis (kein neues Feld, Umschlag Fassung 13); genannt nur, wenn
  der Deckel wirkt — die Obergrenze Regelsatz ohne Wirkung nennt allein die Gestehung (Nach #653 (d)).
- **Rechenweg.** 08 nennt den Ausweis nicht und bleibt; der Rechenweg des Ausweises ist 07 — dort die Formel ergänzt.
- § 6.3 führte die Grenze in keinem Punkt (sie stand in § 3.6 und im Protokoll P651) — nichts durchzustreichen.

## Offen

- Wiki-Upload der Quelle Wirtschaftlichkeit mit dem nächsten Sammel-Upload.

## Gate

offen

## Commit

offen
