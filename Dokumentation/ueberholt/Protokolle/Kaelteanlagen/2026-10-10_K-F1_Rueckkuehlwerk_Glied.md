# Protokoll K‑F1 — Rückkühlwerk als eigenes Glied (Schemaschritt 213, angemeldet als 214)

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

## Schema und Arbeitskette

`RueckkuehlwerkSchema` hängt an `KaelteRangSchema.SCHRITT + 1`; angemeldet als 214, beim Push hängt es an 213 um
(die Kette hängt immer über `+ 1` an der Vorgängerklasse). Angelegt sind `Tab_Rueckkuehlwerk(_STAMM)` (17 Fachspalten,
Katalogregister Stufe 4, Kürzel `RKW`), an `Tab_Energieanlagen` die Spalten `ID_Rueckkuehlwerk` und
`Wasserpreis_EUR_m3` und an `Tab_ErgebnisKaeltemaschine` sechs Ergebnisspalten. Die Testdatenbank ist angehoben, nur
leere Tabellen und Spalten; die 183 Bestandstabellen sind inhaltsgleich.

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
- Gate nach dem Merge: siehe Bericht der Orchestrierung.

## Offen

- K‑F2 (lastabhängige Annäherung, Ventilator, Nassbetrieb, Wasserbilanz) und K‑F3 (Teil-Freikühlung `REIHE`): Die
  sechs Ergebnisspalten bleiben bis dahin leer.
- Die Testdatenbank trägt keine Feuchte; die Feuchtesaat gehört zu K‑F4 (Referenzprojekt, neue Basis).
- K‑F5 Oberfläche, Bericht, Wiki.
