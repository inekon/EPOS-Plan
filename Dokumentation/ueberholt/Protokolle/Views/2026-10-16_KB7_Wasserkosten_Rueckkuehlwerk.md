# KB-7 — Wasserkosten des Rückkühlwerks in der Kostenbasis

Sitzung Dialoge und Korrekturen (Kostenbasis), 16.10.2026, Zweig `kb7m`. Entwurf:
[`2026-10-16_Entwurf_KB7_Wasserkosten_Rueckkuehlwerk.md`](../../2026-10-16_Entwurf_KB7_Wasserkosten_Rueckkuehlwerk.md).
Anwenderentscheid 16.10.2026: Die Kostenbasis übernimmt den Wasserpreis (F1 m³ nur am Wasserträger, F2 ein Preis je Projekt,
F3 Betriebskosten p_B, F4 eigene Kennzahl, F5 ein Arbeitspreis inklusive Abwasser, kein Grundpreis).

## Stand

| Welle | Statuszeile | Inhalt | Nachweis |
|---|---|---|---|
| KB-7a | #1121 | Schemaschritt 235 `WasserpreisTraegerkarteSchema` (hängt an 234): Träger „Wasser“ (`pricing_model` WATER, m³, ID 78), Preise je Projekt aus `Tab_Energieanlagen.Wasserpreis_EUR_m3` nach `energy_project_settings.custom_price_work` (kleinste Anlagen-ID gewinnt, mit Hinweis), Spalte entfällt, Katalogstempel bleibt, Paketanhebung `SchrittWasserpreis` | Kern-Tests, Testdatenbank 235 |
| KB-7b | #1121 | `Wasserkosten.cs`: Σ `Wasser_m3` × Arbeitspreis in den Betriebs-Topf (p_B), Nachweiszeile „Wasser Rückkühlwerk“, ohne Preis Hinweis mit Menge; nicht in Energiekosten, CO₂, BEHG, Zulässigkeit, `Verwendete`; Lauf liest keinen Preis, Hinweis `SIMENG_KAELTE_RKW_WASSER` | Kern-Tests |
| KB-7c1 | #1121 | Trägerkarte €/m³ ohne Grundpreis, Heizwert, Emission; Sichtbarkeit bei Kältemaschine mit Rückkühlwerk (`ProjektEnergietraegerCtrl.WasserTraegerInKarte`); Unterzeile `BETRIEBSKOSTEN_WASSER_RKW` | bUnit |
| KB-7c2 | #1121 | Kennzahl `kaelte.rk.wasser_kosten` [€/a], Fassung 24 des Vorlagenfeldkatalogs (`FASSUNG_WASSERKOSTEN_RUECKKUEHLWERK`), zehn Vorlagen neu | Kern-Tests |
| KB-7e | #1121 | Kenndatentafel der Kältemaschine als Platzhalter (`tabelle.komponenten.kenndaten.kaeltemaschine`, `FASSUNG_KAELTEMASCHINE_GEWERK` = 24); erledigt den Platzhalterpunkt aus #1035; Messlatte `Vorlagenfeldkatalog_v24.txt` = v23 + 17 | Kern-Tests, Messlatte |
| KB-7d | #1121 | Wiki-Quellen Kosten, Wirtschaftlichkeit, Kälteerzeugung, Hilfe Kühlung; Konzept Wirtschaftlichkeit § 3.4; Logbuch-Zeile | Dokumentationswachen |

Commits der Welle: Merge `fa67c4e86`, Testdatenbank `f4952d170`, R56 `ba986af15`.

## Testdatenbank 235

108 744 704 Byte, LFS-SHA-256 `3478881f3c518127442c49a09b928edeba9b83f520ef100ba08c908ed61d15dc`.

## Referenzbasis R56

`2026-10-16_R56_Wasserkosten` eingefroren (Anwenderentscheid 16.10.2026): 37 Projekte, 1 205 CSV. Gegen R55 37/37 PASS
(12 736 541 Werte, mit `--ohne` der zwei Kälteschlüssel), byte-verschieden nur `Projekt_1072/aggregate.csv`. R56 enthält die
Hauptzahl der Kälte mit Split-Zeile (#1105); `--ohne` ist aus den Gate-Skripten entfernt; Projekt 1072 gegen R56 ohne `--ohne`
PASS; R55 archiviert.

## Gate (lokal, Linux)

Kern-Filter und Windows-Schale 0 Fehler; SQL-Dialekt 0 Fundstellen bei 3 034 Texten; Designer 0 Abweichungen; Kern-Tests
gefiltert 2 549/2 549 und 165/165, UI-Tests gefiltert 1 909/1 909. Voller Kern-Lauf über die CI ausstehend.

## Offen

Wiki-Upload der vier Repo-Quellen (Sammel-Upload); CI-Kern-Lauf-Vermerk.
