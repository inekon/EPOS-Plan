# Protokoll SA3a — Oberfläche der Schnellaufheizung

**11.10.2026 · Sitzung Gebäudesimulation · Welle SA3a der Schnellaufheizung (E128).** Grundlage:
[SA1](2026-10-11_SA1_Schnellaufheizung_Kern.md), [SA1b](2026-10-11_SA1b_Heizkurve_Vorheizfenster.md),
[SA2](2026-10-11_SA2_Schema217.md) (Datenweg `KonfigurationCtrl.VorheizvorgabeLesen`/`AufheizvorgabeSchreiben`), Aufbau der
Gruppe nach [AH-V5](2026-10-11_AH_V5_Oberflaeche.md). Bericht, Kennzahlen und Export sind Welle SA3b.

## Was gebaut ist

- **Gruppe „Aufheizen vor Nutzungsbeginn“** (`EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor`): bei „Vorheizzeit
  vorgeben“ und „Vorheizzeit berechnen“ unter der Leistungsreserve der Schalter „Schnellaufheizung (Heizkurve im Vorheizfenster
  anheben)“ (Vorgabe aus), bei Schalter an das Feld „Raumsollwert-Überhöhung (K)“ (Einheit K, Bereich über 0 bis 10 K, leer =
  Vorgabe, Platzhalter „Vorgabe 2 K“), darunter immer die Herleitungszeile: wirkt nur bei Gebäuden mit Heizkreis, endet mit der
  Ankunft, begrenzt durch den höchsten Vorlauf der Anlage. Bei der Sollwertrampe steht nichts davon.
- **Speichern** über den vorhandenen Weg `VorheizenSchreiben` → `AufheizvorgabeSchreiben` (sofort, ganze Einstellung). „Aus“
  behält eine gepflegte Überhöhung; über 10 K färbt das Feld und schreibt nicht; 0 K lehnt der Kern ab (`ArgumentOutOfRange`), der
  Abschnitt kehrt zurück und die Fußzeile meldet.
- **Hüllen:** `SimulationErgebnisHuelle` (Naht `AufheizvorgabeSchreiben` = `AufheizvorgabeSetzen`) und `SimulationKonfigHuelle`
  (Kaskaden-Speichern mit Delete + Insert, Nachreichen über `AufheizvorgabeSchreiben`) reichen die ganze `Aufheizvorgabe` durch
  und brauchten keine Codeänderung; ein Test hält beide Wege mit Datenbank.
- **Texte** in beiden Sprachen: `SIMKONF_AUFH_LBL_SCHNELL`, `SIMKONF_AUFH_LBL_UEBERHOEHUNG`, `SIMKONF_AUFH_HRL_SCHNELL`,
  `KI_DLG_SIM_AUFH_SCHNELL_ERL`, `KI_DLG_SIM_AUFH_UEBERHOEHUNG_ERL`, `KI_DLG_SIM_AUFH_SCHNELL_AUS`; Designer neu erzeugt (+6).
- **KI-Felder** `aufheiz_schnell` (Wahrheitswert) und `aufheiz_ueberhoehung` (Zahl, K, leer erlaubt) in `KiDialoge` und
  `SimulationKiSicht` (`AufheizSchnell`, `AufheizUeberhoehungK`): Schalter nur bei „Vorgabe“/„Berechnet“, Überhöhung nur bei
  Schalter an und im Bereich über 0 bis 10 K, sonst benannt abgelehnt.
- **Aufräumen im Kern:** `SimulationWaermebedarf.SchnellaufheizungTestnaht` entfernt; die Projektläufe der Schnellaufheizung
  (`SchnellaufheizungTests.Projektlauf`, Messung und AK3-Fall) schreiben Schalter und Überhöhung in die Spalten des Schritts 217
  der Testkopie über `VorheizAk3Tests.Projektlauf` und setzen danach zurück. Der interne Schalter
  `Aufheizoptimierung.RampeVorlaufNachfuehren` (SA1b) ist samt Schaltertheorie und Messung `Messung_SA1b_Rampe` entfernt; der Fall
  „Sollwertrampe lässt den Vorlauf einer gekoppelten Zone unberührt“ und der Nachbildungsfall der Kurve bleiben ohne Schalter.

## Wachen

- `KiSimulationMaskeTests`: 86 statt 84 Felder, Liste der setzbaren Felder um die zwei ergänzt, neuer Fall Lesen/Setzen/Absagen.
- `KiMaskenabdeckungWacheTests`: Eingabebilanz `SimulationKonfigSeite` 35 → 37 mit Vermerk.
- `KiDialogkatalogTests`, Lokalisierungs-, Textschlüssel- und Infoknopf-Wachen: ohne Änderung grün.

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler; Windows-Schale (`EnableWindowsTargeting=true`, Debug x64): 0 Fehler.
- `EPOS.UI.Tests` vollständig: 8 949 bestanden, 0 rot (darin drei neue bunit-Fälle der Gruppe und der KI-Fall).
- `EPOS.Kern.Tests` mit Filter (Schnellauf, Vorheiz, HeizkurveVorheiz, Aufheiz, Konfiguration, SimulationWaermebedarf, Ki, Wache):
  1 324 bestanden, 1 übersprungen, 0 rot.
- Referenzlauf aller dreißig Projekte gegen `2026-10-11_R52_Vorheizen`: GESAMT PASS, 979 von 979 CSV byte-gleich.

## Logbuch-Vorschlag

In den Simulationseinstellungen lässt sich beim Vorheizen eine Schnellaufheizung zuschalten, die die Heizkurve im
Vorheizfenster um eine einstellbare Raumsollwert-Überhöhung anhebt.

## Offen

- Bericht, Kennzahlen und Export der Schnellaufheizung (SA3b); Wiki-Seite der Gruppe nachziehen.
