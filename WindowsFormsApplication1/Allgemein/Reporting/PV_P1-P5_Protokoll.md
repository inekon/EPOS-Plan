# PV-Wirtschaftlichkeit — Etappen P1–P5 (Protokoll)

Umsetzung 26.08.2026 auf Branch `kostenformulare`, Spezifikation:
`Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md` Rev. 1 + Nachtrag 1
(pv@now-Abgleich, Korrekturen N1–N5). Commits: P1 9ace8af · P2 19d66ce ·
P3 310c48c · P4 c0f6012 · P5 im Anschlusscommit.

## P1 — Befunde V1–V3

- **V1:** `SimulationPV` klemmt den übergebenen Restbedarf auf 0; der negative
  Anteil (BHKW-Überschuss, `SubVectors(…, false)`-Kette) läuft getrennt als
  `BhkwUeberschuss` (Zeitreihe `BHKW_Ueberschuss`, Protokollhinweis) und nicht
  mehr als PV-Einspeisung. Erhaltungsbeweis im Smoke: alt ≡ neu +
  BhkwUeberschuss je Stunde (Abweichung 0) — Projekte ohne BHKW rechnen exakt
  unverändert. Die DB-Kopie führt kein PV+BHKW-Projekt; der Positivfall ist
  synthetisch belegt, die Realabnahme an 1018 gehört zur P6-Runde auf der
  Produktiv-DB.
- **V2:** Einspeisung = max(0, Überschuss − `SpeicherErgebnis.LadungAcKwh`) je
  Viertelstunde — wirksam im `ErgebnisModel` (SimulationRunner) und in der
  Stundenreihe `PV_UEBERSCHUSS` (Extraktor). Realbeleg 1007: 1,773 MWh
  Überschuss, 1,014 MWh Ladung ⇒ 0,759 MWh Einspeisung (Doppelbegünstigung
  behoben).
- **V3:** `PhotovoltaikCtrl.KwpDesProjekts` — Σ `Tab_PV.Leistung` [W] ×
  Modulanzahl / 1000 (`PV_Leistung` ist die MODULANZAHL).
- Smoke `pv1` 11/11 PASS.

## P2 — EegSatzRechner + Katalogklasse EEG

- Saat-Generation 5 (33 Schlüssel): AW-Basen/Voll-Zuschläge der 5 Klassen,
  Degression, EV-/Ausfall-Abschläge, Grenzen (EV 100 · unentgeltlich 200 (N4) ·
  Ausschreibung 1000 · § 51 100), § 51a-Faktor + 12 Monatskontingente, Kappung,
  Dauer, Solarpaket-Aufschlag VORLAEUFIG (nicht anwenden, F8).
- Rechner: Degressionskette UNRUNDET (N1, Basis × 0,99ⁿ, Stichtage 1.2./1.8.
  ab 01.02.2024); der ANZUWENDENDE Klassenwert ist der gerundete
  BNetzA-Tabellenwert; leistungsanteiliger Mix (§ 23c) auf den Tabellenwerten;
  feste EV = AW − 0,40 nur ≤ 100 kW; Ausfallvergütung 80 % nur > 100 kW (N3);
  Ausschreibungsanteil > 1 MW ⇒ Unvollständig + Override.
- Smoke `pv2` 37/37 PASS: alle 16 BNetzA-Werte 08/2026 exakt; unrundete Werte
  02–07/2026 (8,17851 / 6,73243 / 5,49614 / 12,34327); Mischsätze 300→6,04,
  100→6,43, 30→7,41; Saat idempotent (Zweitlauf 0/0).

## P3 — Datenmodell

- Migrationsschritt 41 (Zielstand 41): `Tab_ProjektPhotovoltaik` (UNIQUE
  ID_Projekt, `Aktiv=false` = Bestand; kein DDL-DEFAULT) + Saat der
  Marktwert-Solar-Stammreihen 2024/2025/2026 (Jan–Jul) in `Tab_Preisreihe`
  (Auflösung Monat, ct/kWh). Idempotent (Zweitlauf 0 neu).
- `ProjektPhotovoltaikCtrl`: Lies/LiesOderVorbelegt (DV 0,40 — N5; Ausfall
  20 % — F5; schreibt nicht), Upsert, `MarktwertMonatCt`, `Jahresmarktwert`
  (Projekt-Override vor Katalogwert des EXAKTEN Jahres — keine stille
  Vorjahresübernahme; Fortschreibung ist Sache der Erlösbildung).
- Katalog-Generation 6: amtliche Jahresmarktwerte 2024 = 4,624 / 2025 = 4,508
  (N2). CSV-Import netztransparenz + Reihenverifikation = P6-Prüfschritt.
- Smoke `pv3` 20/20 PASS.

## P4 — Erlösbildung

- `PvErloesRechner` (UI-frei): jahresscharfe Reihe 1…T; EV / Marktprämie
  (N2: MP = max(0, AW_mix − Jahresmarktwert), Fortschreibung über
  `MarktwertEntwicklung`, AW szenariofest) / PPA fest oder Spot+Aufschlag /
  unentgeltlich; Vergütungsdauer 20 a + IBN-Restmonate, danach Marktwert bzw.
  0; § 51 AUTO je Betrachtungsjahr (Stichtag 25.02.2025; < 100 kW erst ab dem
  Folgejahr des iMSys-Einbaus) — Stufe-1-Pauschale bzw. Stufe-2-MESSUNG aus
  der Spotreihe (Negativstunden; Abregelung: kein Spoterlös); § 51a-Gutschrift
  0,5 × AW × Ausfallarbeit im letzten Vergütungsjahr (vereinfachte
  Barwert-Abbildung, Monatskontingente sind Katalogwissen — hier ohne Deckel,
  ausgewiesen); 60-%-Kappung (AUTO: feste EV ohne iMSys) aus der Stundenreihe
  (dokumentierte Näherung der Viertelstundenregel).
- Einbettung: `RechnePvVerguetung` NACH allen drei Erlöspfaden — der jeweils
  geführte PV-Anteil (`e.ErloesPv`) verlässt den konstanten Erlös, die Reihe
  `ErloesReihe.PV_VERGUETUNG` übernimmt; inaktiv ändert sich NICHTS.
- V4/F7: `StromPreisCtrl` bezieht `v_pv` bei aktivem Dialog aus
  `PvErloesRechner.VpvCtKwh` — eine Vergütungswahrheit.
- Smoke `pv4` 24/24 PASS gegen Handrechnung: 300 kWp MP → Jahr 1 13.536,00 €
  (Spot 10.819,20 + MP 3.676,80 − DV 960), § 51a 1.812,00 € im Jahr 20;
  30 kWp EV 7,01 ct, Dauerende Jahr 21, iMSys-Staffel; Stufe 2: gemessene
  50 %, Kappung 200 kWh; V4-Sätze 7,01/5,64/5,50/0/null.

## P5 — Dialog (Teilumsetzung)

- `Form_PhotovoltaikVerguetung` (Designer-fähig — bewusste Abweichung vom
  § 7-Wortlaut „programmatisch" nach der jüngeren FK1/Ä6-Entscheidung des
  Kostendialoge-Konzepts; App-Design): 7 Gruppen mit Live-Herleitung des
  AW_mix, N3/N4-Sperrlogik (EV > 100 kW gesperrt, unentgeltlich ≥ 200 kW
  gesperrt), § 51-/Kappungs-Statuszeilen, Vorschau aus demselben
  `PvErloesRechner` (keine Zweitrechnung; ohne Simulationsergebnis nur Sätze).
- Andockknopf „Photovoltaik…" in `UcWirtschaftlichkeit` (programmatisch neben
  „Tarifstruktur…", sichtbar nur bei `ErzeugerDerGruppe().Photovoltaik`),
  `Gespeichert`-Flag ⇒ „bitte neu berechnen".
- 52 PVW_*-Schlüssel de + en; `HilfeKontext` → B_KOSTEN.
- Smoke `pv5` 14/14 PASS; Layout-Sweep 120 Formulare, 0 Befunde.

## Bewusst offen (P6-Runde)

- Ergebnisspalten 6.4 (`Pv*` über SpalteSicher) + Kennzahlzeilen (N.3: LCOE₀,
  EV-Quote+Autarkie als Paar, „Vorteil durch PV je Jahr") + Word/Excel-Bausteine
  — gebündelt mit der Gesamtabnahme (INEKON Schulung 01: Kapitalwert
  +92.568 € / −22.979 €, `kennzahlen_modell.py`-Prüfstand ±1 %).
- Realabnahme V1 an Projekt 1018 (Produktiv-DB), Marktwert-CSV-Verifikation,
  Spot-CSV-Import für Monatsmarktwerte.
- Sichtprüfung Philipp (Dialog, Knopf, Statuszeilen).
