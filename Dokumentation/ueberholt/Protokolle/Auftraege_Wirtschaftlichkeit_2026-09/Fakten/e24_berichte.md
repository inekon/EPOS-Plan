# E24 — Bericht Phase 0 (Opus, 25.09.2026 ca. 15:05, Worktree e24 ab 4434983b; Datenbank nur als Kopie gelesen)

Stand Testdatenbank: SHA 76dd9e48, 68.714.496 Byte, **Schemastand 143** (Nachtrag 143 in Referenzlaeufe/LIESMICH; R16 auf 1360e2be = 142 eingefroren).
„Ohne Nachweis" hängt nicht an den Daten: die Rolle meint gespeicherte Ergebniszeilen ohne `Nachweis_Json` (`WirtschaftlichkeitCtrl.cs:8619`);
1018/1019/1023/1024 je 3 Zeilen ohne Umschlag; Pflege ändert `Tab_ErgebnisWirtschaftlichkeit`/`…WirtSensitivitaet` nicht →
`ErgebnisansichtEntscheideTests.cs:352` synthetisch, `ErgebnisansichtTests.cs:60` (Gruppe Wöhler, 131.844,18 €) liest gespeicherte Zeilen. „Kein
Kapitalwert" an 1023 gilt nur für frische Rechnung (`GRUND_VERBRAUCH_OHNE_TRAEGER`, `KostenEmissionRechner.cs:~904`). Emissionskette
(`EmissionsFaktorLader.Lade`): Projektwert (eps.co2 > 0) → aktive `emissionswert`-Zeile → Stamm → Trägerspalte; heute Rückfall Stamm 3 = 240;
mit Träger 63 ohne Projektwert Katalogzeile 176 BAFA_EEW 201; eps-Zeile co2 240 hält 240. Weitere Referenzkessel ohne Träger: 1007, 1008, 1017
(Kessel+BHKW), 1046, 1047 (Kessel+BHKW); 1024 Kessel = 0 (außerhalb des Auftrags).
Proben gegen R16 (Kopien; Ausgangskopie 1018/1023 PASS byte-gleich): v1 1018 ID_Carrier 63 → 1 Abweichung `HeizkesselModul[0].carrier_id` leer → 63,
`Em.Kessel.Co2T` 2,66228 gleich; v2a 1023 Träger 63 + eps-Kopie 10067 (co2 240) → nur carrier_id, Co2T 22,4351687 gleich; v2b co2 201 → 2
Abweichungen (carrier_id, Co2T → 18,7894538); v2c nur Träger → wie v2b. Nur aggregate.csv betroffen, Zeitreihen byte-gleich, übrige 12 Projekte
unberührt. Laufdaten scratchpad/e24/ (lauf_*, vgl_*.log, var.py, vorher.sqlite).
Pflegewerte: 1018 `Tab_Energieanlagen` 10369 ID_Carrier NULL → 63 (Kessel 1018251 Brennstoff 3; BHKW 11327 und Kessel 1026–1030/1039–1045 tragen
63; eps 10063 Preis 0, energy_price 10126 vorhanden). 1023: 11205 NULL → 63 (Kessel 1018254); neue eps-Zeile nach Muster 10067 (1030):
ID_Energieträger 63, ID_Umrechnung 40, custom_hi 10,5, custom_hs 11,6, custom_price_work 0,84 €/Nm³, custom_price_base 1.200 €/a, power 0, so2 0,3,
nox 110, Anteil_Modus Gesamtwert, Preisbasis Nm³, alle _Aktiv 0, co2 nach Q1 (neue ID 10130); energy_price-Zeile (wie 1030 id 10130: 0,84 / 1.200 /
Nm³ / Heizwert 10,5, valid_from wie 1030; neue ID 10185). Wirkung: 1018 nur carrier_id, Preis 0 → Grund „Preis fehlt" bleibt; 1023 frische
Wirtschaftlichkeit erstmals Energiekosten (~8.903 Nm³ × 0,84 ≈ 7.480 €/a + 1.200 €) und Kapitalwert; gespeicherte Wöhler-Ergebnisse unverändert.
Tests: kein sicher betroffener; möglich `ProjektkostenArtenTests.cs:101-121` (1018 ohne Arbeitspreis bleibt wahr), `HilfsstromStrompreisJeAnlageTests`
(1023, prüft nur WPs); nicht betroffen: Ergebnisansicht*, BetriebskostenBasis, NutzungsdauerS3, ValeriLuecken, KostenInvestitionsfuss,
InvestKaskade (1018 45.312,5 / 1023 7.001), KomponentenBestand (2313/6155), ZeitzonentarifAbloesung, StromsteuerErfasst, Struktur/Transfer/Gebäude/UI,
ProjektEnergietraegerEindeutig; kein Test nennt „R16_Anlagenprio". Rolle „ohne Nachweis" bleibt → E24/2 = neue Fakten-Tests.
Aufwand R17: Ordner `2026-09-25_R17_Datenpflege` (14 Projekte, 432 CSV; 430 byte-gleich, 1018/1023 aggregate.csv anders); R16 archivieren (34 Basen,
zehn Protokolle, 35 Dateien); LIESMICH; Namensersetzung nur gültiger Stand: CLAUDE.md:146, kern.yml (7), ios.yml (2),
Konzept_Gebaeudesimulation:1931, Systementwurf_Gebaeudesimulation:179, Umsetzungskonzept_Zapfprofilgenerator:3172, Konzept_Wirtschaftlichkeit Z. 3,
2882, 3007, § 6.3 Nr. 24, Referenzbasen/LIESMICH:19, Basenhistorie:7; Geschichte unverändert (Statuszeilen, Protokolle, Analyse 546/572, Register
731). Zeit 2–2,5 h + Gate.
Fragen (Entscheid Orchestrator 25.09.2026 ~15:10 nach Empfehlung): **Q1 a** co2 240 wie Muster (b 201 Katalog, c 0); **Q2 ja** energy_price-Zeile;
**Q3 ja** nur 1018/1023, übrige Kessel benennen; **Q4 nein** kein Gaspreis für 1018 (Prüffall); **Q5 ja** nur Fakten-Tests; **Q6 ja** Ordnername.
Hinweis BETRIEB_SQLITE: Live-DB dieselbe Pflege nur empfehlen (sonst Rückfall-Hinweis EMISSION_OHNE_TRAEGER_KESSEL_*). Bau freigegeben (zuerst Merge
origin 822ba803, Testdatenbank 143).

# E24 — Bericht Phase 1 (Opus, 25.09.2026 ca. 16:05, e24 = 7da6bb81 auf 822ba803 [#513]; Merge #514 = edf89ae8 auf pm26 ab origin 822ba803)

Commits: 3e20336e E24/1 Datenpflege (Skript scratchpad/e24/pflege/e24_pflege.cs, dotnet, Vorzustand geprüft, eine Transaktion, wiederholbar,
Trockenlauf 0; LFS-Zeiger); 717de7d9 E24/2 `EPOS.Kern.Tests/DatenpflegeKesseltraegerTests.cs` 4 Fälle (Kessel 10369/11205 Träger 63, CO₂ 240 Ebene
PROJEKT; 1023 Gaspreis 0,84/10,5 = 0,08 €/kWh, 1018 keiner; 1023 Gas-Anschlussleistung 19,3/0,874; Rolle „ohne Nachweis" unverändert); 63667c9b
E24/3 Neueinfrierung R17, R16 archiviert, Basisname ersetzt; 7da6bb81 E24/4 `PreisbasisSchrittTests.cs:53` 17 → 18 Nm³ (in Phase 0 übersehen, im
vollen Lauf rot). Testdatenbank: Schemastand 143 (Zielversion = BaustoffQuellenBerichtigung.SCHRITT), SHA 76dd9e48 → **0c2fe21a69a8**, 68.714.496 B;
neue IDs energy_project_settings **10130** (1023, Träger 63, Kopie 10067 aus 1030: co2 240, 0,84 €/Nm³, Grund 1.200, Hi 10,5, Hs 11,6, Umrechnung 40,
Nm³), energy_price **10185** (Kopie 10130 aus 1030); geänderte Zellen Tab_Energieanlagen 10369 (1018) und 11205 (1023) ID_Carrier NULL → 63;
Zellvergleich 145 Tabellen 10.645.701 Zellen: nur diese 2 Zellen + 2 neue Zeilen + 2 Zähler (10129→10130, 10184→10185); Schema gleich; integrity ok,
foreign_key leer; 144 Tabellen STRICT, 14 Sichten, 219 Indizes. A/B R17 gegen R16: 12/14 PASS byte-gleich, 430/432 CSV; 1018 und 1023 aggregate.csv
`HeizkesselModul[0].carrier_id` leer → 63, sonst gleich; Zeitreihen byte-gleich, Em.Kessel.Co2T unverändert (1018 2,66228; 1023 22,4351687);
Determinismus A/B 432/432, GESAMT PASS 4.610.207 Werte; Einfrierlauf byte-gleich. R17 `Referenzlaeufe/2026-09-25_R17_Datenpflege/` 432 CSV +
protokoll.txt (433 Dateien, 60.332.713 B, 2.447 Skalare; Git 431 R100, 2 aggregate.csv geändert); R16 protokoll.txt per git mv nach
`Dokumentation/ueberholt/Referenzbasen/2026-09-25_R16_Anlagenprio/`, Rest git rm; Archiv-LIESMICH 34 Basen/35 Dateien, Abschnitt „Die Basis R16 im
Einzelnen" samt Nachträgen #504 und 143; Referenzlaeufe/LIESMICH: Aktuelle Basis R17 (Anlass § 6.3 Nr. 24, Pflegetafel, Zellvergleich, Einfrierregel
Emissionsfaktoren berührt aber Projektwert 240 = keine Wirkung, A/B, benannte Kessel ohne Träger, Determinismus, Einfrierbefehl, Vorgängernotiz R16),
Entfernte Basen 34/zehn Protokolle, Weg A. Basisname R16 → R17: CLAUDE.md:146, kern.yml (7), ios.yml (2), Dokumentation/LIESMICH.md:251,
Konzept_Gebaeudesimulation:1931, Systementwurf:179, Konzept_Wirtschaftlichkeit Kopf Z. 3/Tabelle ~2882/Nr. 21 ~3007 (§ 6.3 Nr. 24 nicht angefasst),
Analysepapier Kopf Z. 8/Legende ~565, Basenhistorie:7; Geschichte unverändert (Protokolle, Statuszeilen, Register 731, Konzept 2819/3179, Analyse
152/591, Zapfprofil-Konzept 3172). Tests: Kern 7.273+1 (nach E24/4; vorher 1 rot PreisbasisSchritt), UI 6.375, KiKern 549, SpeicherEngine 386,
SpeicherPlanung 27+1; Wachen 38/38; AuslieferungsvorlagenWacheTests 9/9; SqlDialektPruefer 1.920/0; Testhost-Regel eingehalten. Für § 6.3 Nr. 24:
1018/1023 gepflegt (Träger 63; 1023 eps 10130 + energy_price 10185), R17 einzige Wirkung carrier_id in zwei aggregate.csv, 1023 frisch mit
Energiekosten/Kapitalwert (Wöhler-Ergebnisse bleiben „ohne Nachweis"); weiter benannt: 1018-Puffer, 1024 (kein Datenfehler), 1030 (Anker), 1026
(Prüffall); Referenzkessel ohne Träger 1007, 1008, 1017 (auch BHKW), 1046, 1047 (auch BHKW), 1024 ID_Carrier 0; 1018 ohne Gaspreis (Prüffall). Hinweis
BETRIEB_SQLITE: Live-DB Trägerzuordnung prüfen (Rückfall EMISSION_OHNE_TRAEGER_KESSEL_*; ohne Erdgaszeile kein Kapitalwert), Entscheid beim Anwender.
Abnahme A‑E24‑1: 1023 „Wöhler - Test1" neu rechnen → Energiekosten Erdgas 0,84 €/Nm³ und Kapitalwert, Kessel ecoVIT mit „Erdgas E"; 1018 Kessel Vitocrossal
zeigt „Erdgas E", Emissionen unverändert.
