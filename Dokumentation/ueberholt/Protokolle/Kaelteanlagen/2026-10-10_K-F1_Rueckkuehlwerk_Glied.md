# Protokoll K‑F1 — Rückkühlwerk als eigenes Glied (Schemaschritt 214, Status #945)

**10.10.2026** · Sitzung Kälteanlagen · Grundlage: [Entwurf Split, VRF und Rückkühlwerk](../../../aktuell/Kälteanlagen/2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md),
Abschnitte 5.1 und 10 (Zeile K‑F1).

## Auftrag

Anwender 10.10.2026: „zuerst deine KM4-Entscheidungen, dann K‑F1 bauen“; Entscheid E120 (KD‑Q10): Reihenfolge K‑F1
(Glied mit Festwerten, byte-gleich) gleich nach K‑A und FK. K8 aus E33 ist mit E118/E120 fortgeschrieben: Die
Rückkühlung ist ein eigenes Glied, kein Erzeuger, kein Platz in der Kältefolge.

## Wellen

| Welle | Commits | Inhalt |
|---|---|---|
| K‑F1‑a | `85b11f168`, `d2fd944cd` | Schema, Katalog, Projektkopie, Modell, Controller, Anlagenzeile, Ressourcen, Testdatenbank |
| K‑F1‑b | `cdbef92b8`, `e32e26502` | Rechenklasse `Rueckkuehlwerk`, Einbindung, Tests, Gleichwertigkeitsprobe |
| K‑F1‑c | Doku-Commit dieser Welle | Register E33/K8, Kühlkonzept 5.4, Protokoll, Index, Referenzlaeufe/LIESMICH |

## Schema und Kette

`RueckkuehlwerkSchema` hängt an `KaeltebedarfSchema.SCHRITT + 1` = **214** (an 213 K1 Kältebedarf; gebaut zunächst als
Arbeitskette an 212, umgehängt mit dem Merge `87aaf83a4` von `origin/ios_migration_september` und dem Commit
`04db11692`). Angelegt sind `Tab_Rueckkuehlwerk(_STAMM)` (17 Fachspalten, Katalogregister **Stufe 5** hinter den
Kältebedarfskatalogen der Stufe 4, Kürzel `RKW`), an `Tab_Energieanlagen` die Spalten `ID_Rueckkuehlwerk` und
`Wasserpreis_EUR_m3` und an `Tab_ErgebnisKaeltemaschine` sechs Ergebnisspalten. Die Testdatenbank ist aus der Fassung 213
von origin auf 214 angehoben (95 711 232 Byte): nur zwei leere Tabellen und acht leere Spalten, die 189 Bestandstabellen
sind inhaltsgleich, `integrity_check` ok, `foreign_key_check` leer. Die Auslieferungsvorlage-Wache zählt 190 STRICT-Tabellen
(K1 fünf, K‑F1 zwei; `3a9447ba1`).

## Bau

- Modell und Controller: `RueckkuehlwerkModel`, `RueckkuehlwerkStammCtrl`, `RueckkuehlwerkCtrl`; Anlagenzeile in
  `KaeltemaschineAnlageCtrl` (`RueckkuehlwerkWaehlen`, `RueckkuehlwerkPruefen`); 25 Ressourcen.
- Rechenklasse `Rueckkuehlwerk`: Weg `FEST`, Vorgaben gleich `KaelteFestwerte`; `TROCKEN` Außenluft + 10 K,
  `KUEHLTURM_*` wie Nasskühler, `ADIABAT`/`HYBRID` der trockene Ast. Nicht gerechnet und als Hinweis
  `kuehl-rkw-nicht-gerechnet-<Id>` benannt: `LASTABHAENGIG`, Ventilator, Befeuchtung, Wasserbilanz, `REIHE`.
- Eingebunden in `Kaeltemaschine.cs` und `SimulationControl.Kaelte.cs` (alle Leser der Rückkühltemperatur, auch die
  freie Kühlung).

## Prüfungen

- `RueckkuehlwerkTests` (22), `RueckkuehlwerkGleichwertigkeitTests`: 1055 und 1063 je 32 Referenzlauf-Dateien
  byte-gleich, Gegenprobe +2 K, Hinweisfall.
- Referenzlauf der Welle: 14 Projekte PASS, 478 von 478 CSV byte-gleich. Basis R51 unberührt.
- Gate nach dem Merge (`gate_rest_linux.sh` auf `04db11692`): Kern-Filter gebaut, EPOS.UI.Tests 8600, KiKern.Tests 553, SpeicherEngine.Tests 397, SpeicherPlanung.Tests 27 (+1 übersprungen), Dokumentationswachen 35, Windows-Schale 0 Fehler, Designer unverändert, SQL-Dialekt 0 Fundstellen, Werkzeugtests grün (Formularkarte 124, Auslieferungsvorlage 61 nach `3a9447ba1`, Gebäudevergleich 24, Zapfprofil 39), BOM und Konfliktmarker keine, Referenzlauf 29/29 PASS gegen R51, CSV byte-gleich 943/943; gefilterte Kern-Tests 4787 grün (EPOS.Kern.Tests 3040). Statuszeile #945.

## Offen

- K‑F2 (lastabhängige Annäherung, Ventilator, Nassbetrieb, Wasserbilanz) und K‑F3 (Teil-Freikühlung `REIHE`): Die
  sechs Ergebnisspalten bleiben bis dahin leer.
- Die Testdatenbank trägt keine Feuchte; die Feuchtesaat gehört zu K‑F4 (Referenzprojekt, neue Basis).
- K‑F5 Oberfläche, Bericht, Wiki.
