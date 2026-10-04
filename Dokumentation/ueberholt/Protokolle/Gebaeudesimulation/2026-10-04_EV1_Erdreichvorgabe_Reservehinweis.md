# Protokoll EV1 — Erdreichvorgabe und Reservehinweis (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Zweig `ev1` (Worktree), Fast-Forward-Merge auf `7c0a92b`.
**Anlass:** Übergabe [`2026-10-03_Uebergabe_Gebaeudesimulation_Kontowechsel.md`](../../../aktuell/Gebaeudesimulation/2026-10-03_Uebergabe_Gebaeudesimulation_Kontowechsel.md), Abschnitt 3.1; Entscheide E64 und E65. Statuszeile #705.

## 1 Auftrag

E64: keine pauschale Reserve, die Aufheizreserve ist Nutzereingabe; leer gilt 20 % mit Laufhinweis. E65: die Erdreichkorrektur nach DIN EN ISO 13370 ist Standard, optional gibt der Anwender je Gebäude einen wirksamen U-Wert der Bodenplatte vor. Zwei Opus-Agenten, Basis R34 unverändert, kein Basiswechsel.

## 2 Umsetzung Teil A (Schema, Kern, Tests)

- `1e2f755` Schema 180 (`ErdreichVorgabeSchema`, `SCHRITT = PufferAuslegungErgaenzungSchema.SCHRITT + 1`): Spalte `Erdreich_U_Wirksam` REAL, CHECK NULL oder > 0, an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`; neunter Neubau der Sicht `Abfrage_Projektgebaeude` (104 Spalten); `SchemaStand.Zielversion` 180; `Paketanhebung`, `SchemaMigration` der Schale, `Werkzeuge/Testdatenbankschema`; Testdatenbank 180: 83 169 280 Byte, LFS-OID `0aa88998…`, 163 STRICT-Tabellen, `integrity_check` ok, `foreign_key_check` leer.
- `a0f5482` Kern: `Erdreichwiderstand.Bauteilsatz(…, uVorgabe)` — die Vorgabe gilt als U_g jedes Bodens am Erdreich, kein B′, Quelle `Erdreichumfangsquelle.Vorgabe`, Export `Geb[n].Erdreich_Umfangsquelle = Vorgabe`; `Geb[n].Erdreich_B` und der Hinweis `SIMENG_ERDREICH_UMFANG` entfallen bei Vorgabe. Modelle, Leser und Schreiber NULL-erhaltend (`ProjektGebaeudeModel`, `GebaeudeModel`, `ProjektGebaeudeCtrl`, `GebaeudeStammCtrl`, `Katalogfassung`); Projektduplikat, Variante und Projektpaket generisch.
- `fa944eb` Hinweis `SIMENG_AUFH_RESERVE_VORGABE` (I, einmal je Lauf, nur bei eingeschalteter Aufheizoptimierung und leerer `Aufheizvorgabe.Reserve`): „Aufheizreserve nicht vorgegeben; es gelten 20 %.“; Herleitungszeile „· Reserve 20 % (Vorgabe)“ bzw. „· Reserve 15 %“; Felder `ReserveAnteil`, `ReserveVorgabe` in der Aufheizauskunft; Ressourcen de/en.
- `f5d5b2d` LIESMICH-Nachtrag Schritt 180, Kopfzeile auf „181 — frei“; `b79e6d9` DTO `GebaeudeKatalogDaten.ErdreichUWirksam`, `GebaeudeExportVerluste`.
- `635e464` Tests: `ErdreichVorgabeTests` 15 Fälle; Sicht-Assertions in vier Schematests und weiteren Klassen; Fachspaltenzahl 94 → 95.

## 3 Umsetzung Teil B (Oberfläche)

- `7d37263` Exportwert „Vorgabe“ (eine Stelle `Erdreichkennwerte.QUELLE_VORGABE`).
- `b5b2de5` Gebäudedialog: Feld „Wirksamer U-Wert Bodenplatte“ in `GebaeudeKatalogDialog` (Katalog- und Projektmodus) und `GebaeudeStammblattFelder`; Platzhalter „leer = Erdreichkorrektur nach DIN EN ISO 13370“; bei Keller oder Außenluft gesperrt mit Grund, Wert bleibt; Auskunftszeile über `GebaeudeKatalogHuelle.Erdreichweg()` und `ErdreichAuskunftDaten`; Prüfung > 0 (`GEBK_MSG_ERDREICH_U`); sieben Ressourcen de/en; KI-Sicht und Katalogfeld `erdreich_u_wirksam`.
- `0df8837` Projekteinstellung: Platzhalter „Vorgabe 20 %“, Hinweistext zur Reserve.
- `0f7eee0` Tests: `GebaeudeErdreichVorgabeTests` 9 Fälle, `SimulationKonfigSeiteTests` +1, `ErdreichVorgabeTests` +2 (17).
- `7c0a92b` Papiere: Mehrzonenkonzept 2.5, Wiki „Gebäude“ und „Simulation“, Gegenlese 0 Treffer.

## 4 Nachweise

| Prüfung | Teil A | Teil B |
|---|---|---|
| Kern-Filter | 0 Fehler | 0 Fehler |
| Tests (gefiltert) | Kern 1 680 (1 übersprungen), UI 1 183, KiKern 549 | UI 486/486, Kern 48/48 (breiter UI 1 114, Kern 1 545) |
| SQL-Dialekt-Prüfer | 2 299 Texte, 0 Fundstellen | — |
| ResourceDesigner | ohne Abweichung | 14 612 Blöcke ohne Abweichung |
| Referenzlauf gegen R34 | 18/18 PASS, 548/548 CSV byte-gleich | 1045 und 1051 byte-gleich (32/32, 36/36) |
| Windows-Schale auf Linux | 0 Fehler | 0 Fehler |

Gate 705 im Hauptbaum: Kern-Filter 0 Fehler, ChartProben JA (208, Messlatte 2026-10-03), Kern 10 644 (2 übersprungen), UI 7 413, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), Wachen 35/35, Referenzlauf **18/18 PASS gegen `2026-10-03_R34_Erdreich`, 548/548 CSV byte-gleich**, gestörter Lauf 18/18 PASS, Windows-Schale auf Linux 0 Fehler, Designer 14 612 Blöcke ohne Abweichung, SqlDialektPruefer 2 299 Texte 0 Fundstellen, Werkzeugtests Formularkarte 124, Auslieferungsvorlage 47, Gebäudevergleich 24, ZapfprofilValidierung 39, geänderte Markdown-Dateien ohne BOM (Bestandsbefund: `Werkzeuge/Formularkarte/LIESMICH.md` und `EPOS.iOS/CLAUDE.md` tragen ein BOM), keine Konfliktmarker (auf `7c0a92b`, Gate 705). CI-Vermerk folgt.

## 5 Befunde und Festlegungen

1. Im Katalogeditor steht das Feld nur in der freien Hüll-Tabelle; bei Gebäuden mit Zonen zeigt der Dialog die Zonensummen ohne Bodenplattenfeld.
2. Im Lesemodus der Verwaltung erscheint der Wert nicht eigens in der Hüll-Liste.
3. Sperre über `disabled` mit sichtbarer Grundzeile.
4. Der Ressourcenschlüssel `SIMKONF_AUFH_HRL_RESERVE` war belegt, daher die Zeilenschlüssel `…_ZEILE_RESERVE`.
5. Die Herleitungszeile wird in der Hülle `SimulationErgebnisHuelle` gebaut.
6. Die Aufheizoptimierung hat keine eigene Wiki-Seite; der Punkt steht auf der Seite „Simulation“.

## 6 Offen

Windows-Prüfsummen `GebaeudeEinzonennetzTests`; Sichtproben der EV1-Dialoge unter Windows; Logbuch-Version; Wiki-Upload gebündelt (Gebäude, Simulation); Entscheid zur Ampelregel des Gebäudevergleichs und zu den Ferienperioden des Heizsoll-Kalenders 1051; CI-Kennung. Nächste Welle: AK1z (E63).
