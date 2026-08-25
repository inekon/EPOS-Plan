# KD4 — Energieträgerverwaltung und Leistungspreis-Rechenwirkung (Protokoll)

Etappe KD4 des Konzepts Kostendialoge (Rev. 1.2, § 7, Entscheidungen FK6/FK6a),
umgesetzt 25.08.2026 auf Branch `kostenformulare`.

## Umgesetzt

**Migrationsschritt 40** (`SCHRITT_40_LEISTUNGSPREISREIHE`, Zielstand 40):
`Tab_Preisreihe.ID_Energietraeger` (LONG, nullable) — saisonale Leistungspreis-
Reihen je Träger nach dem Preisreihen-Muster (12 Monatswerte, Auflösung „Monat",
Einheit „EUR/kW/Monat"); NULL = Spotreihe (Bestand). Neue `DbWerte`:
`PREISREIHE_AUFLOESUNG_MONAT`, `PREISREIHE_EINHEIT_EUR_KW_MONAT`. Idempotent
(Zweitlauf „bereits erledigt"); `SQL_CREATE_PREISREIHE` bringt die Spalte bei
Neuanlagen gleich mit.

**Rechenwirkung (FK6)** in `KostenEmissionRechner`:

- `TraegerInfo.PreisLeistung`/`LeistungsModus`/`ReihenSummeJeKW`; Effektivregel
  Projekt (`custom_price_power`) vor Katalog (`price_power`), 0 = nicht gepflegt
  (Befund-D5-Regel); Modus aus `energy_carrier.price_power_modus`.
- Anteil je Nicht-Strom-Träger: JAHR = Satz × kW; MONAT = Satz × kW × 12;
  **Reihe (FK6a) gilt vor dem konstanten Satz**: Σ(12 Monatssätze) × kW
  (Projektreihe vor Stammreihe, jüngstes Jahr —
  `PreisreiheCtrl.ReadTraegerReihe`). Der Stromträger bleibt außen vor: sein
  Leistungspreis ist die Tarifstruktur (Schritt 21, keine zweite Wahrheit).
- Ausweis: `VariantenDaten.EnergieLeistungsanteil` (in den Energiekosten
  ENTHALTEN, getrennt ausgewiesen; null = kein Träger gepflegt).
- **Basis = vorgehaltene Anschlussleistung aus den GERÄTEDATEN**
  (`AnschlussleistungKW`: BHKW (Pel+Ptherm)/η, Kessel Ptherm/η;
  η-Normierung > 1,5 ⇒ ÷100, außerhalb (0;1,5] zählt die Anlage nicht) —
  dokumentierte Abweichung vom § 7.1-Wortlaut „Höchstlast aus dem
  Simulationslauf": Der Gas-Leistungspreis bepreist die vorgehaltene
  Anschlussleistung, und Ergebnis-Zeitreihen werden nicht persistiert.

**Spot-Schutz:** `PreisreiheCtrl.ReadVerfuegbare` filtert auf Spotreihen
(`ID_Energietraeger IS NULL`, Auflösung Stunde/Viertelstunde) — die
Stichtagsregel der Simulation kann keine Monats-/Trägerreihe küren.

**UI:**

- `ucFuelSettings`: Modus-Klappliste (Jahres-/Monatsleistungspreis; schreibt
  `price_power_modus` — der Modus ist Katalogsache je Träger, auch im
  Projektkontext; dokumentierte Zwischenlösung), Knopf „Saisonale Sätze…",
  Statuszeile „Saisonreihe X gepflegt — gilt vor dem Satz"; Einheitentext
  €/(kW·a) bzw. €/(kW·Monat) statt €/⟨Brennstoffeinheit⟩; Strom: Feld
  gesperrt/unsichtbar (has_powerprice=false im Bestand) + Tarifstruktur-Hinweis.
- `Form_LeistungspreisReihe` (NEU, Designer-fähig, App-Design): 12 Monatssätze +
  Jahr; je (Träger, Ebene, Jahr) eine Reihe, „Übernehmen" ersetzt das gleiche
  Jahr; Projektkontext erzeugt die vorgehende Projektreihe.
- `Form_Energietraeger` (NEU, Designer-fähig, App-Design): Trägerliste +
  `ucFuelSettings`; beim Stromträger die beiden K4-Karten „Kostenprofil"
  (nur Projektkontext) und „Spotmarktpreise"; speichert das offene Control bei
  Trägerwechsel/Schließen (Bestandsverhalten des Energie-Reiters).
- Menü Administration → Kosten → **„Energieträgerverwaltung…"** (Katalogkontext;
  Preisfelder dort nur lesend — Katalogpreis-Pflege folgt mit den
  Trägervarianten; Modus, Stammreihen und Spot-Stammimport sind pflegbar).
- **Ä1**: `Form_Kosten` führt nur noch die Reiter Investitions-/Betriebskosten —
  Energie- und Kostenprofil-Reiter programmatisch entfernt (Designer-Datei
  unberührt); Übergangsknopf „Energieträger…" unten rechts bis die
  KD6-Projekteinstiege (§ 3.2) stehen.

## Nachweise (kd4-Smoke, DB-Kopie, 21/21 PASS)

- A: `AnschlussleistungKW(1030, 63)` = 2967,27 kW gegen Handrechnung
  (50+81)/0,94 + (250+290)/0,86 + 2200/1; unbekannter Träger ⇒ 0.
- B: alles NULL ⇒ neutral (kein Anteil, `EnergieLeistungsanteil` null);
  Katalog 25 wirkt; MONAT wird gelesen; Projekt 30 vor Katalog 25; Projekt 0 ⇒
  Rückfall auf Katalog. Handrechnung: 25×2967,27 = 74 181,72 €/a (JAHR) bzw.
  ×12 = 890 180,60 €/a (MONAT).
- C: Stammreihe Σ36 wirkt; Projektreihe Σ48 geht vor (48×2967,27 =
  142 428,90 €/a).
- D: Träger-/Monatsreihen bleiben der Spot-Auswahl unsichtbar.
- E: Gas zeigt Modus+Saison-Knopf; Strom gesperrt mit Tarifstruktur-Hinweis;
  beide Dialoge bauen sich in beiden Kontexten auf; Reihendialog lädt die
  Projektreihe. Regressionen: kd2 23/23, kd3 20/20; Layout-Sweep 119 Formulare,
  0 Befunde. Alle Testdaten wurden zurückgesetzt.

## Bewusst offen

- **Emissionsquellen-Ausnahme je Träger (KL8, § 7.3)** — wartet auf E1/E2 des
  Emissionsfaktoren-Konzepts (zur Abnahme).
- **Trägervarianten „Speichern unter…" + Katalogpreis-Schreibweg** (§ 7.1)
  und die Träger-Übernahme Stamm→Projekt (§ 7.2/§ 8.5).
- Projekt-Einstiege § 3.2 (Anlagendialog „Energiekosten…", Berichte & Kosten)
  — Etappe KD6; bis dahin Übergangsknopf in `Form_Kosten`.
- E2E-Lauf über `KostenEmissionRechner.Berechne` mit Simulationsergebnis: der
  Anteilsblock (8 Zeilen) ist über die Bausteintests + Handrechnung belegt.
