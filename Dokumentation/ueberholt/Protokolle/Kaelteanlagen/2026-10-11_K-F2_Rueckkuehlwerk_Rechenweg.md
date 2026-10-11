# Protokoll K‑F2 — Rechenweg des Rückkühlwerks (ohne Schemaschritt)

**11.10.2026** · Sitzung Kälteanlagen · Grundlage: [Entwurf Split, VRF und Rückkühlwerk](../../../aktuell/Kälteanlagen/2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md),
Abschnitte 5.1 und 10 (Zeile K‑F2); Vorgänger [K‑F1](2026-10-10_K-F1_Rueckkuehlwerk_Glied.md).

## Was gebaut ist

- **Annäherung `LASTABHAENGIG`:** ein Schätzschritt, ΔT = ΔT_Nenn · x / L; Überlast bis 30 K mit Meldung.
- **Bezugstemperatur je Bauart:** Kühlturm mit Feuchtkugeltemperatur nach Stull (`Kaeltemaschine.Feuchtkugeltemperatur`);
  adiabat und hybrid oberhalb `Befeuchtung_Ab_C` mit T − η(T − Tf); ohne Feuchte ein benannter Hinweis und Ausweichweg.
- **Ventilator:** `EIN_AUS`, `STUFEN`, `DREHZAHL`; die Energie folgt dem Luftanteil der Annäherung. `EIN_AUS` unter `FEST`
  taktet P · min(x, 1), unter `LASTABHAENGIG` läuft er voll im Laufanteil der Maschine (neu
  `Kaeltemaschine.LaufanteilDerStunde`). Ein gesetzter `Ventilator_Nenn_kW` ersetzt `Hilfsstrom_Rueckkuehlung_kW`.
- **Wasser:** Verdunstung, Abflutung, Drift; Kosten nur als Hinweis (Betriebskostenzeile offen für K‑F5); Hinweis zur
  42. BImSchV.
- **Ergebnisse und Kennzahlen:** Spalten `Ventilatorstrom_MWh`, `Wasser_m3`, `Stunden_Nass`, `Rueckkuehltemperatur_Mittel`
  (nur mit Rückkühlwerk, sonst NULL); Kennzahlen `kaelte.rk.ventilator`, `kaelte.rk.wasser_m3`.
- **Bericht:** Berichtskatalogfassung 18 → 19, Messlatte `Vorlagenfeldkatalog_v19`, zehn Berichtsvorlagen neu gebaut.
- **Vorgaben:** `KaelteFestwerte` mit „Quelle nicht am Wortlaut geprüft (vorläufig)“; Annahmen begründet.

## Nachweise

Gleichwertigkeit 1055/1063 byte-gleich; Referenzlauf der Kälteprojekte PASS. Gate der Zusammenführung mit KM4‑E1
(`gate_rest_linux.sh` auf `d74640010`): Referenzlauf 29/29 PASS gegen R51, CSV byte-gleich 943/943, EPOS.UI.Tests 8651,
Dokumentationswachen 35, Windows-Schale 0 Fehler, SQL-Dialekt 0 Fundstellen. Basis unverändert.

## Offen

K‑F3 Teil-Freikühlung `REIHE`; K‑F4 Referenzprojekt mit Feuchtesaat und neue Basis; K‑F5 Oberfläche, Bericht, Wasserkosten
als Kostenkomponente.
