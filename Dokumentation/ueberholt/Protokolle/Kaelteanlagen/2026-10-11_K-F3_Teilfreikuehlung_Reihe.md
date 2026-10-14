# Protokoll K‑F3 — Teil‑Freikühlung `REIHE` am Rückkühlwerk (ohne Schemaschritt)

**11.10.2026** · Sitzung Kälteanlagen · Grundlage: [Entwurf Split, VRF und Rückkühlwerk](../../../aktuell/Kälteanlagen/2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md),
Abschnitte 5.5, 5.6 und 10 (Zeile K‑F3); Vorgänger [K‑F2](2026-10-11_K-F2_Rueckkuehlwerk_Rechenweg.md).

## Was gebaut ist

- **Schaltung `REIHE`:** Das Rückkühlwerk kühlt den Kaltwasserrücklauf vor, den Rest deckt der Verdichter an der
  Rückkühltemperatur der Stunde. Vorkühlung mit dem Massenstrom der Stunde (Spreizung konstant, der Kaltwasserstrom
  folgt der Last):
  `Q_FK = min(Last · clamp((θ_R − θ_FK) / (θ_R − θ_V), 0, 1), Rückkühlerleistung)`
  mit θ_R Kaltwasserrücklauf, θ_V Kaltwasservorlauf, θ_FK Austritt der freien Kühlung (Außenluft bzw. Feuchtkugel plus
  Annäherung). Der Nenn-Massenstrom hätte die Vorkühlung in Teillast überschätzt.
- **Rücklauf:** auf Stufe AK3 aus der Lösung des Kreises, sonst Vorlauf plus Auslegungsspreizung der Kühlübergabe; ohne
  gepflegte Kühlübergabe die Vorgabe `KaelteFestwerte.REIHE_SPREIZUNG_K` = 5 K (Normbedingung Kaltwasser 12/7 °C,
  DIN EN 14511‑2; Quelle nicht am Wortlaut geprüft, vorläufig). Spreizung der Stunde ≤ 0 über dem Rechenrand: keine
  Teil‑Freikühlung.
- **Abstand verfehlt:** reicht θ_FK nicht unter θ_R, rechnet `REIHE` bitgleich wie `PARALLEL`; deckt die freie Kühlung
  alles, steht der Verdichter.
- **Ventilatorstrom der freien Kühlung:** Mit `Ventilator_Nenn_kW` ersetzt der Ventilator den EER‑Ersatz der freien
  Kühlung — in `REIHE` und in `PARALLEL`.
- **Ergebnisse und Kennzahlen:** Ergebnisspalten `TeilFreikuehlung_MWh` und `TeilFreikuehlung_Stunden` (nur mit
  Schaltung `REIHE`, sonst NULL); Kennzahlen `kaelte.rk.teilfrei` [MWh/a] und `kaelte.rk.teilfrei_stunden` [h/a],
  summiert über die Maschinen mit Rückkühlwerk in Reihe, ohne sie null; Texte in beiden Sprachen.
- **Berichtskatalog Fassung 22** (`FASSUNG_TEILFREIKUEHLUNG`): Fassung 21 hatte origin inzwischen an die Schnellaufheizung
  (SA3b) vergeben; Messlatte `Vorlagenfeldkatalog_v22.txt` = v21 plus die 30 Schlüssel `kaelte.rk.teilfrei*`, zehn
  Berichtsvorlagen mit `Werkzeuge/Berichtsvorlage alle` neu gebaut, Validator 0 Fehler.

## Abweichungen

- Fassung 22 statt der im Zweig gebauten 21 (Zusammenführung mit SA3b).
- Massenstrom der Stunde statt Nenn-Massenstrom (Nachzug im Zweig; Entwurf 5.5 nennt die Annahme).

## Nachweise

- `RueckkuehlwerkReiheTests`: Mischstunde gegen Handrechnung, 40‑%‑Teillast, Volllast, freie Kühlung deckt alles,
  Abstand verfehlt (bitgleich `PARALLEL`), Schwelle mit Rand, Fluid am Vorlauf, Rückkühlerleistung, Rücklauf AK3 gegen
  Spreizungsweg, Ventilator statt EER‑Ersatz (`PARALLEL` und `REIHE`), Energieprobe je Stunde und über die Kaskade;
  Datenbanktest 1055 trocken `REIHE` gegen `PARALLEL`; K‑F2‑Tests auf den gerechneten Reihenweg nachgezogen.
- Gate (`gate_rest_linux.sh` auf `29b9c9cc2`, Integration mit K‑F3 und KWN‑0): Referenzlauf 30/30 `GESAMT: PASS` gegen `2026-10-11_R52_Vorheizen`, CSV byte-gleich 979/979, SQL-Dialekt 0 Fundstellen, EPOS.UI.Tests 9087 (1 Zeitprobe unter Last rot, allein grün), KiKern 553, SpeicherEngine 397, SpeicherPlanung 27, Dokumentationswachen 35, Kern-Filter und Windows-Schale 0 Fehler; EPOS.Kern.Tests voll 13 760 grün (7 übersprungen); nach der Zusammenführung mit origin EPOS.UI.Tests voll 9174 grün.

## Offen

- **K‑F4 Referenzprojekt:** 1055 erzeugt bei 6 °C Kaltwasser keine Mischstunde; ein Referenzprojekt mit Teil‑Freikühlung
  braucht eine eigene Saat (höherer Kaltwasservorlauf, Feuchtesaat) und eine neue Basis.
- **K‑F5:** Oberfläche, Bericht, Wiki, Wasserkosten.
- Der Rücklauf auf Stufe AK3 ist nur durch die Rechenprobe belegt, nicht durch einen Referenzlauf.
