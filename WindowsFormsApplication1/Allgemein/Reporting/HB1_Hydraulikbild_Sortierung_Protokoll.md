# HB1 — Anzeige-Sortierung ungepflegter Priorität, Hydraulikbild auf Z_AnlageSenke (Fixprotokoll)

Anlass: Diagnose des Anwenderbefunds „zweites BHKW wird nicht angezeigt" (30.08.2026,
Projekt 1030): Das neue BHKW war angezeigt, stand aber wegen ungepflegter `Prioritaet`
(NULL sortiert in ACE vorn) VOR der konfigurierten Anlage und erbte deren
▲▼/×-Bedienelemente (F1); zusätzlich fand sich ein vergessener Altspalten-Leser:
`Hydraulikbild.Lesen` speiste Ring-/Ebenenprüfung aus den stillgelegten `WS_*`-Spalten,
während der Senkendialog seit Paket A1 nur `Z_AnlageSenke` schreibt (F3). Die
Rangziffer je Erzeugertyp (beide BHKW zeigen „1") ist dokumentierte Absicht und blieb
unangetastet (F2 = offene Konzeptfrage).

## Umsetzung

- **F1**: `Ladeordnung.SqlAnlagenprio(alias)` — die vorhandene 99er-Regel
  (`ANLAGENPRIO_UNGEPFLEGT`) als EIN ACE-Ausdruck; angewandt in den vier
  **Anzeige**-Lesern (`Form_Simulation_Config.Uebersicht.AnlagenImProjekt`,
  `Hydraulikbild`, `Karten.AnlagenNamen`, `Karten.QuellnutzerSammeln`). Die fünf
  `ORDER BY Prioritaet`-Stellen des **Rechenwegs** (SimulationControl/
  WaermesenkeClass) tragen dieselbe Schieflage, sind aber Kaskadenreihenfolge des
  Laufs — bewusst nicht angefasst (eigene Entscheidung mit vollem Referenzlauf).
- **F3**: `Hydraulikbild.Lesen` liest Senken/Lader jetzt über
  `Z_AnlageSenkeCtrl.LesenJeProjekt` (derselbe Weg wie SchemaModell/Warnkriterien);
  Lader über **alle** Ränge; führende Senke = Rang 1, zweiter Platz = nächste
  Puffersenke ab Rang 2 (exakte Umkehrung der Migration 48–54); Anlage ohne Zeile →
  Rang-1-Vorbelegung Heizkreis/Beides. `WS_*` wird im Hydraulikbild nicht mehr
  gelesen; `Warnkriterien.AusAltspalten` → `AusBildSenke` (nur Umbenennung/Doku).

## Nachweise (Harness `..\dev\hb1\`, frische Kopien; Produktiv nur gelesen)

- Build Exit 0 (nur bekannte Altwarnungen).
- F1: 1030 vorher `14921(–) → 14920(1)`, nachher `14920(1) → 14921(–)`; Projekte mit
  durchgängig ungepflegten oder gepflegten Prioritäten unverändert.
- F3-Wirkprobe: Senkenzeile für 14921 über den Dialogweg → vorher Lader
  `11334,14920` (Ladebezug unsichtbar), nachher `11334,14920,14921`.
- **Alle 30 Projekte** Graph-gedumpt und gedifft: 27 knoten-/kantengleich (nur
  F1-Reihenfolge); **1042/1043/1044 ändern Kanten gewollt** — dort laufen `WS_*` und
  `Z_AnlageSenke` in den Echtdaten auseinander, das neue Bild deckt sich Zeile für
  Zeile mit der Dialogwahrheit (u. a. 1043: Rang-3-Senke jetzt sichtbar).
- `Warnkriterien.PruefeProjekt` über alle 30 Projekte **identisch** (12 Projekte mit
  Befunden, jeder Text gleich); `RingMeldung` überall null; Ebenen: nur die drei neu
  gesehenen Ladebezüge (1042/1043/1044) steigen 0→1, `Ring=False` überall.
- **Referenzlauf-A/B** (1030/1042/1043, je eigene DB-Kopie, DLL-Tausch): 90
  Ergebnisdateien, **SHA256 0 Unterschiede**, Laufprotokolle identisch.

## Offene Punkte

| Nr. | Punkt |
|---|---|
| HB1-O1 | Engine-Sortierung (5 `ORDER BY Prioritaet`-Stellen im Rechenweg) — eigene Entscheidung mit vollem Referenzlauf |
| HB1-O2 | Verbleibende WS_*-Leser sind dokumentiertes Schutznetz (A1-O4: `SenkenPufferDerAnlagen`, Wizard-Sicherung, INSERT) |
| HB1-O3 | `Warnkriterien.Projektbild.LaderErgaenzen` ist beweisbar redundant — Kandidat fürs Aufräumpaket |
| HB1-O4 | Datenbefund: In 1042/1043/1044 hängen die Altspalten an anderen Anlagen als die Dialogwahrheit — ein späteres Räumen der WS_* beendet die Drift |
| HB1-O5 | F2 (Rangziffer je Typ / „Modul n von m"-Badge) — Konzeptfrage, nicht umgesetzt |

Geänderte Dateien: `Allgemein/Simulation/Ladeordnung.cs`, `Hydraulikbild.cs`,
`Warnkriterien.cs`, `WaermesenkeClass.cs` (Doku), `Views/Simulation/
Form_Simulation_Config.Uebersicht.cs`, `Form_Simulation_Config.Karten.cs`.
Harness (gitignored): `..\dev\hb1\`.
