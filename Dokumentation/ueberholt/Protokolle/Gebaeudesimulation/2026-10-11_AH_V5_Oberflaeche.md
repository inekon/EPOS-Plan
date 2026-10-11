# Protokoll AH-V5 — Oberfläche der Aufheizoptimierung Fassung 2

**11.10.2026 · Sitzung Gebäudesimulation · Welle V5 der Aufheizoptimierung Fassung 2.** Grundlage:
[Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md), Abschnitte 2.9 und 5.1–5.3
(E122, E124, E125); aufgesetzt auf [AH-V4](2026-10-11_AH_V4_Schema215.md) (Schema 215). Oberfläche, Hülle, Texte beider
Sprachen, KI-Felder und der Entscheid der Orchestrierung zum Rückfall je Zone; kein Bericht und kein Export (V6), kein
Referenzprojekt (V7).

## Felder je Ort

| Ort | Feld | Sichtbar bei | Speicherweg |
|---|---|---|---|
| Simulationskonfiguration, Gruppe „Aufheizen vor Nutzungsbeginn“ | Aufheizen rechnen (Schalter) | immer | `AufheizvorgabeSchreiben` → `KonfigurationCtrl.AufheizvorgabeSetzen` (V4, ein `UPDATE`) |
| | Verfahren: Sollwertrampe nach Aufheizleistung / Vorheizzeit vorgeben / Vorheizzeit berechnen | Schalter an | dito |
| | Bemessung, ΔT_K, Art der Aufheizzeit, Aufschlag (h, %) — Bestand | Verfahren Sollwertrampe | dito |
| | Vorheizzeit (h), 1–47, leer = Gebäude und Zonen | Verfahren Vorgabe | dito |
| | Zulässiger Sprung zum Nutzungsbeginn (Art % / kW) und Wert, leer = „Vorgabe 20 %“ | Vorgabe, Berechnet | dito |
| | Regelgenauigkeit (K): 0,5 K / 1 K (Vorgabe, gespeichert leer) / 2 K / Eingabe (Zahlenfeld 0,1–5 K) | Vorgabe, Berechnet | dito |
| | Vorheizzeit gilt: je Gebäude / je Zone | Vorgabe, Berechnet | dito |
| | Gebäude mit Heizkreis einbeziehen (Schalter, Vorgabe an) | Vorgabe, Berechnet | dito |
| | Leistungsreserve (%) — Bestand | Schalter an | dito |
| Gebäudedialog, Reiter „Konditionierung“ | Aufheizzeit bzw. Vorheizzeit (h) — dieselbe Spalte `Aufheizzeit_Manuell_H`, neue Zeile zur Bedeutung je Verfahren | wie bisher | OK-Weg des Gebäudedialogs |
| Zonendialog (beheizte Zone) | Vorheizzeit (h), 1–47, leer = wie Gebäude, Zeile zur Wirkung (Vorgabe mit Geltung je Zone) | beheizte Zone | Arbeitsstand → OK-Weg des Gebäudedialogs → `GebaeudeZonenCtrl` (V4) |
| Programmeinstellungen, Rubrik „Anwendung“, Gruppe „Neue Projekte“ | Zulässiger Sprung beim Vorheizen (Art % / kW), Wert (leer = Vorgabe 20 %) | immer | OK → `EinstellungenCtrl.Speichern` → `VorheiztoleranzSchreiben` (`Dienste.Einstellungen`, V4-Schlüssel) |

Herleitungszeilen der Gruppe nach 2.9: „Aus“, Sollwertrampe (neu gefasst, Schlüssel `SIMKONF_AUFH_HRL_AN`), Vorgabe,
Berechnet, Deckel (bei Vorgabe und Berechnet), verfügbare Heizleistung (immer bei an), Geltung (immer). Die drei
`VORHEIZART`-Schlüssel entfallen (E124). Englisch heißt die Vorheizzeit „preheat lead time“, damit sie sich von der
Aufheizzeit („preheat time“) unterscheidet.

## Kern: Zonenwerte genügen (Entscheid der Orchestrierung)

Mit „Vorheizzeit vorgeben“ fiel ein Gebäude ohne Vorheizzeit des Projekts ganz auf die Sollwertrampe zurück. Jetzt:

- `Vorheizplanung.Anwendbar` verlangt beim Verfahren Vorgabe keinen Projektwert mehr.
- `Vorheizplanung.Rueckfall(…, vorheizzeitGebaeudeH, vorheizzeitenZonenH)`: `OhneVorheizzeit` nur, wenn weder Projekt noch
  Gebäude noch — bei Geltung Zone — eine Zone einen Wert trägt; die Aufrufer in `Vdi6007Rechenweg` reichen
  `Aufheizzeit_Manuell_H` und die Zonenwerte.
- `Vorheizplanung.VorheizzeitVorgabe`: Zone (bei Geltung Zone) vor Gebäude vor Projekt. Eine Zone ohne jeden Wert bekommt in
  `AnwendenZonen` den Plan der Sollwertrampe (`Aufheizoptimierung.Planen` mit derselben Vorgabe, Vorheizen =
  Sollwertrampe) und den Laufhinweis `SIMENG_VORH_RUECKFALL_ZONE` mit ihrem Namen; die übrigen Zonen rechnen Option 1.
- Bitgleich bleiben die Sollwertrampe und Option 1 mit Projektwert (der Zweig ist unverändert); neu rechnen nur Projekte
  mit „Vorgabe“ ohne Projektwert, die bisher ganz zurückfielen — keines davon steht in der Basis.
- `SIMENG_VORH_UNERREICHBAR` nennt keine Nächte ohne Absenkung mehr (E125 F18 (b)); der Text sagt, dass die Tage nicht in
  die Bemessung eingehen. `SIMENG_VORH_RUECKFALL_OHNE_ZEIT` nennt Projekt, Gebäude und Zone.
- Abwandlungen für Oberfläche und Assistent: `Vorheizvorgabe.MitVerfahren/-Vorheizzeit/-Toleranz/-Genauigkeit/-Geltung/
  -Heizkreis` (Ankunftsbezug reist mit), `Aufheizvorgabe.MitVorheizen`.
- Behoben nebenbei: Seite und KI-Sicht bauten die `Aufheizvorgabe` je Feld neu und verloren dabei das Vorheizen; jede
  Setzung trägt es jetzt mit.

## Umsetzungsentscheide

- Die Felder der Sollwertrampe bleiben in ihrer Reihenfolge, die Reserve steht bei der Rampe an ihrem Platz und bei den
  neuen Verfahren am Ende; die Verfahrenswahl steht über allen.
- Regelgenauigkeit 1 K wird leer gespeichert (Vorgabe); „Eingabe“ schreibt erst mit dem getippten Wert.
- Der Zonendialog zeigt die Vorheizzeit an jeder beheizten Zone und sagt in einer Zeile, wann sie wirkt; die Geltung des
  Projekts reist nicht in den Gebäudedialog. Die Prüfung meldet einen gespeicherten Wert außerhalb 1–47 mit
  `ZONDLG_MSG_VORHEIZZEIT`, ein getippter färbt das Feld und hält OK an.
- KI-Felder: sieben in `SimulationKiSicht` (Vorheizzeit nur bei Vorgabe, die übrigen nur bei Vorgabe oder Berechnet, sonst
  benannt abgelehnt mit `KI_DLG_SIM_AUFH_NICHT_VERFAHREN`), `vorheizzeit` am Zonendialog, zwei an den
  Programmeinstellungen. Kein neuer Infoknopf — die Gruppe behält ihre eine Hilfepille.

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler; Windows-Schale (`EnableWindowsTargeting=true`): 0 Fehler.
- `EPOS.UI.Tests` vollständig: 8 610 grün, 0 rot (darin die neuen Fälle der Gruppe, `ZonenVorheizzeitTests` und der
  Programmeinstellung; nachgezogen sind `KiSimulationMaskeTests` 77 → 84 Felder, die Eingabebilanz von
  `KiMaskenabdeckungWacheTests` — Simulationskonfiguration 26 → 35, Zonendialog 22 → 23, Programmeinstellungen 12 → 14 —,
  `KiDialogkatalogTests` und die geänderten Texte).
- `EPOS.Kern.Tests` mit Filter (Vorheiz, Aufheiz, Konfiguration, Einstellungen, Zone, Ressourcen, Designer, Text,
  Lokalisierung, Kodierung, Double, Link, Ordnung, alle Wachen, KI-Dialoge): 2 628 grün, 0 rot, 1 übersprungen; darin
  zwei neue Fälle in `VorheizVorgabeTests` zum Rückfall je Zone.
- Referenzlauf aller 29 Projekte gegen `2026-10-10_R51_FreieKuehlung`: **GESAMT PASS** (10 036 768 Werte), alle 943 CSV
  byte-gleich.
- Kein SQL geändert, Schema bleibt 215.

## Logbuch-Vorschläge

- „Die Gruppe „Aufheizen vor Nutzungsbeginn“ in der Simulationskonfiguration bietet drei Verfahren: Sollwertrampe nach
  Aufheizleistung, Vorheizzeit vorgeben und Vorheizzeit berechnen.“
- „Für das Vorheizen lassen sich der zulässige Sprung zum Nutzungsbeginn (in % oder kW), die Regelgenauigkeit und die
  Geltung je Gebäude oder je Zone einstellen.“
- „Im Zonendialog kann jede Zone eine eigene Vorheizzeit haben.“
- „In den Programmeinstellungen lässt sich der zulässige Sprung beim Vorheizen für neue Projekte vorbelegen.“

## Offen

- Sichtabnahme der Gruppe, des Zonenfelds und der Programmeinstellung unter Windows.
- V6: Bericht, Kennzahlen, Export; V7: Referenzprojekt.
