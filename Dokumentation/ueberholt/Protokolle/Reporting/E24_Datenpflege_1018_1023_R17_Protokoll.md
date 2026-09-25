# E24 — Datenpflege 1018/1023: Kessel-Träger, Erdgaspreis 1023, Referenzbasis R17 (Protokoll, 25.09.2026)

Statuszeile #514 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Anwender am
25.09.2026, „nehme die Empfehlungen vor: für Später“, zu Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 6.3 Nr. 24;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E24 (neu), R‑Rest Nr. 24 und R‑E21 (E21‑Q3, Q4, Q6); Herkunft des Punkts:
[`E21_Pflege_Ressourcen_Testdaten_Protokoll.md`](E21_Pflege_Ressourcen_Testdaten_Protokoll.md) (die Datenlücken
gemessen und benannt, 1018-Träger und 1023 als Kandidaten für die nächste Neueinfrierung); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; die Basis in [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md), Abschnitt „Aktuelle Basis“,
und im [Archiv der Referenzbasen](../../Referenzbasen/LIESMICH.md); der Hinweis für die Live-Datenbank in
[`BETRIEB_SQLITE.md`](../../../aktuell/BETRIEB_SQLITE.md) § 7.1. Vorgänger:
[`E23_Waermepumpe_Betrieb_ohne_kWh_Protokoll.md`](E23_Waermepumpe_Betrieb_ohne_kWh_Protokoll.md). Zweig `e24` von
`822ba803` (#513); Opus 5.5 im Worktree `.claude/worktrees/e24`: `3e20336e` (E24/1), `717de7d9` (E24/2), `63667c9b`
(E24/3), `7da6bb81` (E24/4). Merge `edf89ae8` („Merge e24: Datenpflege 1018/1023 (Kessel-Traeger, Erdgaspreis 1023),
Referenzbasis R17 (#514)“) auf `pm26` über `origin` = `822ba803`. **Kein Schemaschritt** — Schemastand 143 bleibt;
geändert sind allein Werte der Testdatenbank (LFS-SHA-256 `76dd9e48…` → `0c2fe21a69a8…`). Die Simulation bewegt
allein die Trägerkennung der Kessel in zwei `aggregate.csv`, deshalb die neue Basis `2026-09-25_R17_Datenpflege`.

## Befund vor der Welle (Phase 0, Datenbank nur als Kopie gelesen)

1. **Zwei Kessel ohne Energieträger.** In 1018 trägt die Anlage 10369 (Kessel 1018251, Brennstoff 3)
   `ID_Carrier` NULL, obwohl das BHKW 11327 desselben Projekts und die Kessel von 1026–1030 und 1039–1045 den Träger
   63 „Erdgas E“ führen; 1018 hat die Projektzeile 10063 (Preis 0) und den Preisstand 10126. In 1023 trägt der
   Kessel 11205 (1018254, Brennstoff 3) ebenfalls `ID_Carrier` NULL, und es gibt keine Projektzeile für Erdgas.
2. **Die Emissionskette** (`EmissionsFaktorLader.Lade`): Projektwert (`energy_project_settings.co2` > 0) → aktive
   `emissionswert`-Zeile → Stamm → Trägerspalte. Heute greift an beiden Kesseln der Rückfall auf den Stamm 3 mit
   240 g/kWh; mit Träger 63 ohne Projektwert gälte die Katalogzeile 176 (BAFA_EEW, 201 g/kWh); eine Projektzeile mit
   co2 240 hält 240.
3. **Proben gegen R16** (Kopien; die Ausgangskopie von 1018/1023 PASS byte-gleich): v1 — 1018 mit Träger 63: eine
   Abweichung, `HeizkesselModul[0].carrier_id` leer → 63, `Em.Kessel.Co2T` 2,66228 gleich; v2a — 1023 mit Träger 63
   und einer Kopie der Zeile 10067 (co2 240): nur `carrier_id`, `Em.Kessel.Co2T` 22,4351687 gleich; v2b — dieselbe
   Zeile mit co2 201: zwei Abweichungen (`carrier_id`, `Em.Kessel.Co2T` → 18,7894538); v2c — nur der Träger: wie v2b.
   Betroffen ist jeweils nur `aggregate.csv`, alle Zeitreihen und die übrigen zwölf Projekte sind byte-gleich.
4. **„Ohne Nachweis“ hängt nicht an den Daten.** Die Rolle meint gespeicherte Ergebniszeilen ohne `Nachweis_Json`
   (`WirtschaftlichkeitCtrl.cs:8619`); 1018, 1019, 1023 und 1024 führen je drei solcher Zeilen. Die Pflege ändert
   `Tab_ErgebnisWirtschaftlichkeit` und `…WirtSensitivitaet` nicht — `ErgebnisansichtEntscheideTests.cs:352` ist
   synthetisch, `ErgebnisansichtTests.cs:60` (Gruppe Wöhler, 131.844,18 €) liest die gespeicherten Zeilen. „Kein
   Kapitalwert“ an 1023 gilt nur für eine frische Rechnung (`GRUND_VERBRAUCH_OHNE_TRAEGER`,
   `KostenEmissionRechner.cs`).
5. **Weitere Referenzkessel ohne Träger:** 1007, 1008, 1017 (Kessel und BHKW), 1046, 1047 (Kessel und BHKW); der
   Kessel von 1024 trägt 0 — außerhalb des Auftrags.

## Gebaut

- **E24/1 — Datenpflege** (`3e20336e`): einmaliges dotnet-Dateiskript `e24_pflege.cs` (im Scratchpad der Welle, nicht
  im Repository) auf `Referenzlaeufe/Kenndaten_Test.sqlite` — Vorzustand geprüft, eine Transaktion, wiederholbar,
  Trockenlauf 0; der LFS-Zeiger committet. Tafel im Abschnitt „Die Datenpflege“.
- **E24/2 — Fakten-Tests** (`717de7d9`): neu `EPOS.Kern.Tests/DatenpflegeKesseltraegerTests.cs` mit vier Fällen — die
  Kessel 10369 und 11205 tragen Träger 63 und CO₂ 240 auf Ebene PROJEKT; der Gaspreis von 1023 ist 0,84 ÷ 10,5 =
  0,08 €/kWh, 1018 hat keinen; die Gas-Anschlussleistung von 1023 (19,3/0,874); die Rolle „ohne Nachweis“ ist
  unverändert (E24‑Q5).
- **E24/3 — die Basis** (`63667c9b`): Neueinfrierung R17, R16 ins Archiv, Basisname ersetzt (Abschnitt „Die neue
  Basis“).
- **E24/4 — Nachzug eines Tests** (`7da6bb81`): `PreisbasisSchrittTests.cs:53` zählt 18 statt 17 Zeilen „Nm³“ — die
  Erdgaszeile 10130 von 1023 kommt hinzu (Befund 4).

## Die Datenpflege

| Tabelle, Zeile | Projekt | vorher | nachher | Quelle |
|---|---|---|---|---|
| `Tab_Energieanlagen` 10369 (Kessel 1018251, Brennstoff 3) | 1018 | `ID_Carrier` NULL | **63** „Erdgas E“ | wie das BHKW 11327 desselben Projekts und die Kessel von 1026–1030, 1039–1045 |
| `Tab_Energieanlagen` 11205 (Kessel 1018254, Brennstoff 3) | 1023 | `ID_Carrier` NULL | **63** | dieselbe |
| `energy_project_settings` **10130** (neu) | 1023 | — | Träger 63, `ID_Umrechnung` 40, Hi 10,5, Hs 11,6, 0,84 €/Nm³, Grundpreis 1.200 €/a, power 0, CO₂ **240**, SO₂ 0,3, NOx 110, Anteil-Modus Gesamtwert, Preisbasis Nm³, alle `_Aktiv` 0 | Kopie der Zeile 10067 von 1030 (E24‑Q1 a) |
| `energy_price` **10185** (neu) | 1023 | — | Träger 63, 0,84 / 1.200 / Nm³ / Heizwert 10,5, `valid_from` wie 1030 | Kopie der Zeile 10130 von 1030 (E24‑Q2) |

**Zellvergleich** aller 145 Tabellen samt `sqlite_sequence` gegen die Fassung mit Schemastand 143 (10.645.701
Zellen): allein die zwei `ID_Carrier`-Zellen, die zwei neuen Zeilen und die zwei Zähler (`energy_project_settings`
10129 → 10130, `energy_price` 10184 → 10185); Schema gleich. `integrity_check` ok, `foreign_key_check` leer,
144 Tabellen (alle STRICT), 14 Sichten, 219 Indizes. 68.714.496 Byte, LFS-SHA-256 `0c2fe21a69a8…`. Ein zweiter Lauf
des Skripts findet nichts offen.

**Wirkung je Projekt.** 1018: allein `carrier_id`; die Projektzeile 10063 hat den Preis 0, der Grund „Preis fehlt“
bleibt (E24‑Q4). 1023: eine frische Wirtschaftlichkeitsrechnung hat erstmals Energiekosten für Erdgas (rund
8.903 Nm³ × 0,84 € ≈ 7.480 €/a zuzüglich 1.200 € Grundpreis, Schätzung aus Phase 0) und damit einen Kapitalwert; die
gebuchten Ergebnisse der Gruppe „Wöhler“ bleiben unverändert und ohne Nachweisumschlag.

## Die neue Basis

`Referenzlaeufe/2026-09-25_R17_Datenpflege`, **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039,
1040, 1041, 1042, 1045, 1046, 1047), 432 CSV und `protokoll.txt` — 433 Dateien, 60.332.713 Byte, 2.447 Skalare; in Git
431 Dateien als Umbenennung R100, zwei `aggregate.csv` geändert. Gerechnet auf der gepflegten Testdatenbank
(Schemastand 143, `0c2fe21a…`).

**A/B gegen R16** (vierzehn Projekte): **12/14 PASS und byte-gleich**, 430/432 CSV byte-gleich; 1018 und 1023 FAIL
mit je einem Wert in `aggregate.csv`, alle Zeitreihen byte-gleich:

| Projekt, `aggregate.csv` | R16 | R17 |
|---|---|---|
| 1018 `HeizkesselModul[0].carrier_id` | leer | 63 |
| 1023 `HeizkesselModul[0].carrier_id` | leer | 63 |

`Em.Kessel.Co2T` bleibt gleich (1018 2,66228 t/a; 1023 22,4351687 t/a).

**Einfrierregel „Emissionsfaktoren“ berührt** — die neue Projektzeile führt `energy_project_settings.co2` an einem
Referenzprojekt; darum die Neueinfrierung. Die Emissionen bewegen sich trotzdem nicht: Der Projektwert 240 g/kWh
steht in der Lesekette vor der aktiven Katalogzeile (201 g/kWh) und ist derselbe Wert, den vorher der Rückfall auf
den Stamm 3 lieferte (E24‑Q1 a).

**Determinismus:** zwei Läufe desselben Standes 432/432 CSV byte-gleich, untereinander GESAMT: PASS (4.610.207
Werte); der Einfrierlauf ist mit beiden byte-gleich. Einfrierbefehl: `dotnet run --project EPOS.Referenzlauf -c
Release -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte
1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 --ziel Referenzlaeufe/2026-09-25_R17_Datenpflege`.

**Archiv.** Das `protokoll.txt` von R16 ist per `git mv` nach
`Dokumentation/ueberholt/Referenzbasen/2026-09-25_R16_Anlagenprio/` gewandert, der Rest per `git rm` entfernt. Das
Archiv führt 34 Basen und 35 Dateien, die Tabellenzeile R16 („abgelöst durch R17 am 25.09.2026“) und den Abschnitt
„Die Basis R16 im Einzelnen“ samt den Nachträgen #504 und Schemastand 143. `Referenzlaeufe/LIESMICH.md`: „Aktuelle
Basis“ R17 (Anlass § 6.3 Nr. 24, Pflegetafel, Zellvergleich, Einfrierregel, A/B-Tafel, benannte Kessel ohne Träger,
Determinismus, Einfrierbefehl, Vorgängernotiz R16), „Entfernte Basen“ mit 34 Basen und zehn gesicherten Protokollen,
Behalteliste, Weg A. **Basisname** `2026-09-25_R16_Anlagenprio` → `2026-09-25_R17_Datenpflege` in `CLAUDE.md`
(Z. 146), `kern.yml` (7 Stellen), `ios.yml` (2), `Dokumentation/LIESMICH.md` (Z. 251), Konzept Gebäudesimulation
(Z. 1931), Systementwurf Gebäudesimulation (Z. 179), Konzept Wirtschaftlichkeit (Kopf, Tafel der Regressionsanker,
§ 6.3 Nr. 21), Analysepapier (Kopf, Legende von § 5), Basenhistorie (Z. 7). Geschichte bleibt unverändert: die
Protokolle, die Statuszeilen, Register R‑Rest Nr. 18, Konzept § 6.1 und Anhang (Zeilen E22), Analysepapier (Nachtrag
und § 5 Zeile E22), Umsetzungskonzept Zapfprofilgenerator und die Archivliste.

## Fragen aus der Welle

Die Fragen stellt der Phase‑0-Bericht; entschieden hat sie der Orchestrator am 25.09.2026 (~15:10) mit der
Baufreigabe, nach Empfehlung; gebaut ist jeweils der Entscheid (→ Register R‑E24).

| Frage | Lesarten | Entscheid |
|---|---|---|
| **E24‑Q1** CO₂ der neuen Erdgas-Projektzeile von 1023 | (a) 240 g/kWh wie das Muster 10067 von 1030; (b) 201 g/kWh aus dem Katalog (BAFA_EEW); (c) 0 | a — die Emissionen bleiben unverändert |
| **E24‑Q2** Preisstand zur Projektzeile | eine `energy_price`-Zeile anlegen: ja / nein | ja — 10185, Kopie der Zeile 10130 von 1030 |
| **E24‑Q3** Umfang | nur 1018 und 1023 pflegen, die übrigen Kessel ohne Träger benennen: ja / nein | ja |
| **E24‑Q4** Gaspreis für 1018 | ja / nein | nein — 1018 bleibt Prüffall ohne Arbeitspreis |
| **E24‑Q5** Tests | nur neue Fakten-Tests, die Rolle „ohne Nachweis“ unverändert: ja / nein | ja |
| **E24‑Q6** Ordnername | `2026-09-25_R17_Datenpflege`: ja / nein | ja |

## Anwenderentscheid vom 25.09.2026 zu Konzept § 6.3 Nr. 24

(→ Register R‑Rest Nr. 24; der Weg im Protokoll der Entscheidwege § 8.48.)

| Nr. | Entscheid | Stand |
|---|---|---|
| **24** Datenpflege (1018 Kessel ohne Energieträger, Puffer ohne Temperaturpaar; WP-Kennlinie 1024) | „nehme die Empfehlungen vor: für Später“ | erledigt mit E24 (#514): 1018 und 1023 gepflegt, neue Basis R17; benannt bleiben der 1018-Puffer, 1024 (kein Datenfehler), 1030 (Anker) und 1026 (Prüffall) |

## Abweichungen und Befunde

1. **Die Rolle „ohne Nachweis“ bleibt** — sie hängt an den gespeicherten Ergebniszeilen, nicht an den Daten
   (Befund 4 vor der Welle); die Tests, die 1023 so nutzen, sind unverändert.
2. **`Em.Kessel.Co2T` unverändert**, weil der Projektwert 240 greift (E24‑Q1 a); mit 201 wäre 1023 auf 18,79 t/a
   gefallen (Probe v2b, nicht gewählt).
3. **Benannt, nicht gepflegt (E24‑Q3):** ohne Energieträger bleiben die Kessel von 1007, 1008, 1017 (dazu das BHKW),
   1046 und 1047 (dazu das BHKW); der Kessel von 1024 trägt `ID_Carrier` = 0; 1018 bleibt ohne Gaspreis als Prüffall
   (E24‑Q4; `ProjektkostenArtenTests.cs:101-121` bleibt wahr).
4. **E24/4:** `PreisbasisSchrittTests.cs:53` erwartete 17 Zeilen „Nm³“ — in Phase 0 übersehen, im vollen Lauf rot,
   mit der Erdgaszeile von 1023 sind es 18.
5. **Die CI bemerkt die Pflege nicht:** 1018 und 1023 gehören nicht zu den sechs CI-Projekten (1030, 1007, 1017,
   1045, 1046, 1047); nur der Lauf der vierzehn Projekte zeigt sie. Die CI vergleicht gegen R17.
6. **Fremder sporadischer Test:** Die Windows-CI des Vorgängers #513 lief rot mit
   `GebaeudeHochrechnungTests.Eine_Zone_ohne_Bezugsflaeche_ist_ein_benannter_Fehler` (Kulturleck zwischen Tests,
   Test der Gebäudesimulation, CI-Vermerk der Statuszeile #513) — kein Bezug zu E24, lokal im Gate #514 grün.

## Nachweis

- **Datenbank:** Zellvergleich und Prüfungen im Abschnitt „Die Datenpflege“.
- **Referenzlauf:** A/B gegen R16 und Determinismus im Abschnitt „Die neue Basis“; Referenzlauf 14/14 gegen R17.
- **Tests** (Worktree `e24` nach E24/4): EPOS.Kern 7.273 und 1 übersprungen, EPOS.UI 6.375, KiKern 549,
  SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen — 14.610 bestanden / 0 Fehler / 2 übersprungen (vor
  E24/4 ein Test rot, `PreisbasisSchrittTests`); Wachen 38/38; `AuslieferungsvorlagenWacheTests` 9/9;
  SqlDialektPruefer 1.920/0; Testhost-Regel eingehalten. Kein Test nennt den alten Basisnamen.
- **Merge** `edf89ae8` auf `pm26` über `origin` = `822ba803`.
- **Gate:** Gate #514 auf `edf89ae8` (25.09.2026 19:00–19:06): Kern-Filter 0 Fehler, ChartProben 0 Fehler, 161 Bild-Hashes gleich mit der Messlatte, Tests Kern 7.273 (1 übersprungen), UI 6.375, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), Dokumentationswachen 29/29. Nachtest nach dem Merge von `origin` `fc0b7e5c` (`868afc57`): Kern-Filter der Gebäudeklassen 36/36.
- **CI:** steht aus (Beobachtung nach dem Push) — die CI muss gegen R17 laufen (`kern.yml` und `ios.yml` mit dem Basispfad R17).

## Abnahme am Gerät (A‑E24‑1, Windows)

1. **1023:** das Projekt „Wöhler - Test1“ neu rechnen — die Wirtschaftlichkeit zeigt Energiekosten für Erdgas
   (0,84 €/Nm³) und einen Kapitalwert; der Kessel ecoVIT zeigt den Energieträger „Erdgas E“.
2. **1018:** der Kessel Vitocrossal zeigt „Erdgas E“; die Emissionen sind unverändert.

Belegt in `DatenpflegeKesseltraegerTests` und in `aggregate.csv` von R17; die Sichtprüfung liegt beim Anwender.

## Logbuch

Kein Eintrag. E24 pflegt Werte der Testdatenbank, die nur Gate, CI und Referenzlauf lesen; die Anwendung, ihre Masken
und ihre Rechenregeln sind unverändert, und die Live-Datenbank des Anwenders ist nicht berührt. Ein Satz im Logbuch
hätte keine Änderung, die ein Anwender sieht. Für die Live-Datenbank steht der Hinweis „Trägerzuordnung der Kessel
prüfen“ in `BETRIEB_SQLITE.md` § 7.1.

## Papiere mit der Statuszeile

Mit dem Merge (E24/3): der Basisname und die Referenzbasen-Papiere (Abschnitt „Die neue Basis“). Mit dieser
Statuszeile: Konzept (Kopf mit Codestand `edf89ae8`, Zielversion 143 berichtigt, „E24 ohne Schritt“; Schrittabsatz;
§ 6.3 Nr. 24 als Einzeiler erledigt; § 7), Register (Kopf, Familientafel, R‑Rest Nr. 24 gebaut, neue Familie R‑E24,
R‑E21 Q3, Q4 und Q6 nachgezogen), Analysepapier (Kopf, Nachtrag, § 5 Zeile E24, Basisvermerk der Legende),
Protokoll der Entscheidwege (Kopf von § 8, § 8.48 mit dem Wortlaut von Nr. 24 vor #514, § 8.49),
`BETRIEB_SQLITE.md` (§ 7.1), Dokumentations-Index (Reporting 138 → 139, Referenzbasen 34 Basen und 35 Dateien),
Statusdatei (#514, Nach #514; in #513 der CI-Vermerk). Das Archiv der Referenzbasen ist geprüft: Tabellenzeile und
Abschnitt R16 nennen Datum und Grund der Ablösung. Kein Mockup (kein Dialog), kein Wiki, kein Logbuch.

## Offen

- Die **Abnahme am Gerät** A‑E24‑1 (zwei Schritte oben).
- Benannt bleiben der **1018-Puffer** ohne Temperaturpaar, **1024** (kein Datenfehler), **1030** (Anker) und **1026**
  (Prüffall); die übrigen Referenzkessel ohne Träger (Befund 3).
- Die **Live-Datenbank:** Trägerzuordnung der Kessel prüfen, gepflegt wird nur nach Entscheid des Anwenders
  (`BETRIEB_SQLITE.md` § 7.1).
- **Gate** und **CI** (Nachweis oben).
