# Protokoll KP3 — Aufheizoptimierung, Ergebnisse, Referenzprojekt und neue Basis der Konditionierungsprofile

**Stand 02.10.2026 · in Umsetzung.** Grundlage: [Entwurf KP3](../../../aktuell/Gebaeudesimulation/2026-10-02_Entwurf_KP3.md)
(zwölf Wellen in vier Spuren, Festlegungen nach der Umsetzung als N1.68), Entscheid E58 (Leitkonzept N1.67, Teilkonzept 9.8),
[Protokoll KP2](2026-09-30_KP2_Konditionierung_Oberflaeche.md). Je Welle ein Agent im eigenen Worktree mit eigenem Gate
(`Werkzeuge/Gate/gate_linux.sh`), Merge durch die Orchestrierung, Gate über den gemeinsamen Stand, Statuszeile, Push.

## 1. Verfahren

Wie bei KP2: Auftrag als Datei, Agent im eigenen Worktree (`model: opus`), Abnahme mit vollem Gate im Worktree, danach
Merge → Gate im Hauptbaum → Statuszeile und Protokoll → Push → CI-Nachweis. Bis RP1 gilt die Basis R30 (16 Projekte,
487 CSV byte-gleich), ab RP2 die Basis R31 mit 17 Projekten.

## 2. Was gebaut ist

### R1 — Aufheizantwort, Stufenformel, Kappungsanteil

- **`Zonenmodell2K.Aufheizantwort(strahlungsanteil, zusatzleitwertWK)`**: zustandsfrei, eigenes geregeltes Fallsystem,
  berührt den Puffer `_heizen` nicht; ΔB und G_0 exakt aus dem linearen Term von `Konstante`; Speicher mit vier Plätzen
  (`AUFHEIZANTWORT_PLAETZE`) und bitgenauem Schlüssel (Anteil, Zusatzleitwert); `StationaererZustand(…)` als Startzustand
  der Nachweise. Typ `Aufheizantwort` (A, z, ΔB, v, G_0, H_s, τ₁/τ₂, r_k, C_k, C_w, `Bei`, `Stunde`, `Ausgang(M) = z·M·v`).
- **`Aufheizstufen`** mit `Aufheizform` und `Aufheizwahl`: `StundenmittelW`, `AugenblickW`, `AbsenkformW`,
  `AbsenkformGeschlossen`, `ErsteOrdnungMittel`, `ErsteOrdnungAugenblick`, `FormZurQuelle(bool quelleGrenze)` nach E58 F1 (b),
  `Waehlen(…)` (kleinstes n mit Φ − P_auf ≤ `Rechenrand.Zu(P_auf)`, n ≤ n_max, sonst „unerreichbar“ mit N = 0); jede
  Exponentialfunktion über `Uebergangsrechner.Bei`; kein Aufrufer im Lauf, keine Ressourcenschlüssel.
- **Kappungsanteil** (Befund B1, Festlegung 20): `Schritt` und `SchrittMitMuster` zählen die Zeit im Betriebsfall
  `Heizgrenze` in einem eigenen Akkumulator und schreiben ihn als `heizleistungMaxAnteil` auch in den idealen Rückweg; mit
  Übergabe bleibt der Ausdruck aus den Begrenzungsgründen; gelesen wird der Anteil im Lauf weiterhin nur mit Kopplung —
  keine Zahl ändert sich.
- **Tests:** `KappungsanteilTests` (4), `AufheizantwortTests` (N-AH0, innere Identitäten, Speicher; 7),
  `AufheizStufenformelTests` (N-AH1, N-AH2, N-AH4, N-AH5).

### D1 — Schemaschritte 160 und 161, Datenweg

- **Schritt 160 `AufheizvorgabeSchema`** (fünf Spalten an `Tab_Einstellungen`, 32 → 37) und **Schritt 161
  `AufheizErgebnisSchema`** (14 Spalten an `Tab_ErgebnisGebaeude`, 26 → 40, und `Tab_ErgebnisZone`, 15 → 29, dort dazu
  `Sommerlueftungsstunden_H`); wiederholbar je Spalte, CHECKs beim `ADD COLUMN`, kein DML, keine neue Tabelle (153 STRICT),
  die Stempeltrigger aus Schritt 159 unberührt; Wertlisten `DbWerte.AUFHEIZ_*`; eingehängt in `Paketanhebung` (zwei
  DDL-Stufen), `SchemaMigration` der Schale, `Werkzeuge/Testdatenbankschema`, `TestDatenbank.cs`. `SchemaStand.Zielversion`
  = `AufheizErgebnisSchema.SCHRITT`. Nummern gegen origin geprüft (02.10.2026, 08:21 UTC und nach dem Gate: dort 159).
- **Testdatenbank** von 159 auf 161 gehoben (33 Spalten): 81 170 432 Byte, LFS-SHA-256
  `117f44f96b4530540348202ecb9cbd123128953ff0d7af0e77abb8cded9fe3dd`; einziger Wertunterschied `Tab_Applikation.SchemaVersion`
  159 → 161; `integrity_check` ok; Nachtrag in `Referenzlaeufe/LIESMICH.md`. Die Basis R30 bleibt — der Referenzlauf liest
  die Ergebnistabellen nicht, der Schalter steht überall auf 0.
- **Datenweg:** Record `Aufheizvorgabe` (`EPOS.Kern/Model/`, Normalisierung nach Festlegung 24, wirksame Werte (a), 2 K,
  ρ 0,2, täglich), `KonfigurationCtrl.AufheizvorgabeLesen` (fehlende Zeile oder Spalte = aus) und `-Schreiben` (ein
  `UPDATE`), Naht `SimulationWaermebedarf.AufheizvorgabeProjekt` (in `KlimakalenderLesen` zurückgesetzt, ohne Aufrufer im
  Rechenweg); je 14 Felder in `ErgebnisGebaeudeModel`/`ErgebnisZoneModel`, `ErgebnisCtrl` schreibt und liest nach
  Vorhandensein, die Zonenzeile aus einer gemeinsamen Spaltenliste (B23); `SimulationKonfigHuelle.Speichern` reicht die
  Aufheizvorgabe beim Neuanlegen der Einstellungszeile nach (wie Kühlbetrieb und Anlagenkopplung).
- **Tests:** `AufheizSchemaTests` (9), `AufheizvorgabeTests` (10), `ErgebnisAufheizTests` (6); Spaltenzahl-Konstanten
  (B24) auf `AufheizErgebnisSchema.SPALTENZAHL_*`; `Auslieferungsvorlage.Tests` 44/44; `SqlDialektPruefer` 2 135 Texte,
  0 Fundstellen.

## 3. Schemaschritte

| Schritt | Klasse | Inhalt | Testdatenbank |
|---|---|---|---|
| 160 | `AufheizvorgabeSchema` | `Aufheizoptimierung`, `Aufheiz_Bemessung`, `Aufheiz_Abzug_K`, `Aufheiz_Reserve`, `Aufheiz_Art` an `Tab_Einstellungen` | 161 (`117f44f9`) |
| 161 | `AufheizErgebnisSchema` | 14 Ergebnisspalten je Gebäude und Zone, `Sommerlueftungsstunden_H` an der Zone | 161 |

## 4. Befunde der Umsetzung

- **Druckstellen N-AH0 treffen alle** (Prüfsatz, Haus aus 1045): H_s 971,76 W/K, G_0 2 425,12 W/K, τ 1,0168/4,5395 h,
  C_w 5,781/6,931 kWh/K, Φ_stat 32,07 kW, Spitze ohne Rampe 37,04 kW; bei P = 35,3 kW n = 5 im Mittel (35 217 W; Stundenbeginn
  35 662 W), n = 7 im Augenblick (n = 6: 35 335 W); a = 0,3: Φ_stat 34,42 kW, n = 32 im Mittel. C₁/C₂ stehen im Teilkonzept nur
  auf zwei Stellen und werden auf die halbe letzte Stelle geprüft.
- **Neue Zahl:** mit a = 0,3 braucht die Augenblicksform bei 35,3 kW **n = 37** (35 286 W; H_s 1 042,9 W/K, G_0 3 155,1 W/K,
  τ 0,982/3,878 h) — mit Quelle Grenze (F1 (b)) läge der Prüfsatz über dem bisher genannten n = 32; nachzutragen in
  Teilkonzept 4.3 mit dem Abschluss.
- Bei a > 0 koppelt nicht nur G_rad die Massen, sondern auch die auf beide Oberflächen verteilte Leistung; zusammenfallende
  Eigenwerte entstehen nur bei G_rad = a·w_AW·G_c,IW/(1 − a) mit angeglichener Diagonale — so sind die synthetischen Sätze gebaut.
- **Variante und Duplikat nehmen keine Ergebnisse mit** (Bestand, `ProjektDuplizierenCtrl.IstErgebnisTabelle`, Entscheid
  23.09.2026): die Projekteinstellung wandert mit Duplikat, Variante und Transfer, die Ergebnisse bleiben beim Quellprojekt;
  nur der `.wpx`-Transfer trägt die Ergebnisspalten.
- Eine Stunde nur mit Kühlübergabe trägt den Kappungsanteil aus dem eigenen Akkumulator (bisher 0, dort nicht gelesen).

## 5. Festlegungen der Wellen (für N1.68)

- R1: Index 1 der schnelle, Index 2 der langsame Modus (Teilkonzept 4.2); im zusammenfallenden Zweig r_k, C_k NaN, τ₁ = τ₂ =
  −1/μ, C_w endlich; erste Ordnung als `double` (mindestens 1, +∞ bei P ≤ Φ_stat); unerreichbar = N = 0 mit der Leistung bei
  n_max; `AbsenkformGeschlossen` vergleicht Φ_stat(θ_T) − G_0·ΔT > `Rechenrand.Zu(0)`; `FormZurQuelle(bool)` statt eines
  Quellen-Enums; Nachweise mit dem Sprung 17 → 21 °C.
- D1: `ErgebnisCtrl` schreibt das Modell unverändert, die NULL-Regeln der Festlegung 25 setzt D2 in `GebaeudeKennzahlen`;
  ungültige Werte lässt der Record stehen, `AufheizvorgabeSchreiben` scheitert dann an der Spalte (`false`, Zeile unverändert);
  keine Ressourcenschlüssel in D1.

## 6. Nachweise

| Nachweis | Ergebnis |
|---|---|
| N-AH1 (R1) | 420 Fälle × n = 1 … 24 (29 Projektgebäude der Testdatenbank, sechs synthetische Sätze), größte relative Abweichung 2,98e-15; Kappungsschärfe: Grenze Φ̂·(1 + 1e-9) kappt nicht, Φ̂·(1 − 1e-6) kappt die Sprungstunde; zwölf Fälle im zusammenfallenden Zweig |
| N-AH2 (R1) | 370 Fälle, 33 670 Rampen, Abweichung 2,87e-15; nie über der Gleichgewichtsform; 50 Fälle ohne Vorbedingung ausgelassen |
| N-AH4 (R1) | 15 120 Proben je Form, keine Ausnahme (9 386 mit n_F = n) |
| N-AH5 (R1) | 49 000 Gitterpunkte, keine Ausnahme; (b) ≥ (a) mit ΔT_K 0 … 10 K; „fest ≥ täglich“ folgt mit R2 |
| Kappungsanteil (R1) | ideal = gekoppelt im Grenzfall bitgleich; `SchrittMitMuster` = `Schritt` bitgleich; Anteil = Σ Abschnittsdauern Heizgrenze |
| Abnahme R1 im Worktree (`ead963fd`) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 9 839, UI 7 233, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 byte-gleich gegen R30**, gestörter Lauf PASS, Windows-Schale 0 Fehler |
| Abnahme D1 im Worktree (`36c26656`) | Kern-Filter 0 Fehler, ChartProben JA, Kern 9 850, UI 7 233, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 byte-gleich gegen R30**, gestörter Lauf PASS, Windows-Schale 0 Fehler, `Auslieferungsvorlage.Tests` 44/44, `SqlDialektPruefer` 0 Fundstellen |
| Gate im Hauptbaum nach den Merges R1 (`a3c3c655`) und D1 (`54bf19bb`) mit `origin` #654–#656 (`54bf19bb`), danach Merge `origin` #657 (`c457460b`, IFC-Import, Testdatenbank-Kopien) mit gezielter Nachprüfung | Gate auf `54bf19bb`: Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 9 905, UI 7 239, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen) — alle grün, Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R30**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer unverändert; gezielte Nachprüfung auf `c457460b` nach dem Merge #657: Kern-Filter 0 Fehler, 1 558 betroffene Kern-Tests (Aufheiz, Schema, Testdatenbank, Ifc, Zonenmodell, Ergebnis, Konditionierung, Wachen) und 7 239 UI-Tests grün, Referenzlauf 16/16 PASS, 487/487 byte-gleich, gestörter Lauf PASS, Windows-Schale 0 Fehler |

## 7. Offen

- **R2** (Aufheizplan Einzone): `_vdi6007.Aufheizvorgabe = AufheizvorgabeProjekt` neben `Kuehlbetrieb`; W1 und Deckel aus
  `Aufheizwahl` (N = 0); θ_N nach F2 (b); Φ_stat mit `AequivalentN` statt θ_eq = T_a; Antwort mit dem unbedingten
  Zusatzleitwert der Sprungstunde (`ZusatzleitwertWK(h_s, false, false)`); „fest ≥ täglich“ in N-AH5.
- **D2:** `GebaeudeKennzahlen` füllt die neuen Modellfelder (auch `SommerlueftungsstundenH` der Zone, `HeizleistungMaxStundenH`
  als Σ der Anteile aus R4), Festlegung 25/26, Export nach E32.
- **O1:** Schreibweg nach dem Muster `KuehlbetriebSetzen` mit Vormerksatz, wenn noch keine Einstellungszeile steht.
- **Abschluss A:** Teilkonzept 4.3 (n = 37 bei a = 0,3 in der Augenblicksform), 10.3 „elfte“ Einfrierregel, 5.3 `GEMISCHT`,
  4.7 Residuen je Luftwechsel, Glossar „Nachweisband“ je Quelle, Liste in `GebaeudeRueckwegTests.cs`; N1.68.
- Beim Anwender: P14 (ρ nach der Messung in RP1), SA1 der KP2-Oberfläche, SA-KP3 am Ende.
