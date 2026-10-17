# Protokoll KWN‑1a — Rechenklasse der Kälte-Wärme-Kopplung (ohne Schemaschritt)

**17.10.2026** · Sitzung Kälteanlagen · Grundlage: [Kühlkonzept](../../../aktuell/Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md),
Abschnitte 5.6.3, 5.6.4, 5.6.8 und 5.6.13 (Entscheid E127); Vorgänger [KWN‑0](2026-10-11_KWN-0_Prozesskaelte.md).
Statuszeile #1190.

## Ziel

Erste Teilstufe von KWN‑1 (Kopplungsvorstufe an der Kältemaschine): die Rechnung der gleichzeitigen Kälte- und
Wärmenutzung als reine, datenbankfreie Klasse mit Rechenproben — ohne Schema und ohne Einbau in den Lauf. Schema (KWN‑1b,
Schemaschritt 242 angemeldet) und Verdrahtung im Lauf (KWN‑1c) folgen.

## Umsetzung

- **`EPOS.Kern/Allgemein/Simulation/Kaelte/KaelteWaermeKopplung.cs`** (neu): Schnittmenge aus Kälte- und Wärmebedarf,
  kälte- und wärmegeführter Betrieb, Hubgrenze, Gerätegrenze, Restbetrieb, Stromaufteilung nach Nutzenergie (Strom einmal
  gebucht), Gesamtleistungszahl, Hilfe für die Jahresreihe.
- **`Kaeltemaschine.cs`:** `PunktBei` und `VerdichterstundeBei` verwenden die vorhandene Kennlinien-, Teillast- und
  Gütegradrechnung wieder.
- **`KaelteFestwerte.cs`:** Vorgabe-Grädigkeit der Kopplung und `KOPPLUNG_KONDENSATOR_SPREIZUNG_K` (Normpunkt 30/35 °C,
  vorläufig).
- **Lesepunkt am Kondensatoreintritt:** Die Rückkühlachse der Kennlinie wird am Kühlwassereintritt
  max(Rücklauf; Vorlauf − 5 K) + Grädigkeit gelesen; Hub und Einsatzgrenze bleiben am Austritt Vorlauf + Grädigkeit;
  Kennlinienrand ohne Grenze am Eintritt.
- **Kühlkonzept** 5.6.3, 5.6.4 und 5.6.13 um die Festlegungen der Kopplung ergänzt.
- **`EPOS.Kern.Tests/KaelteWaermeKopplungTests.cs`** (neu, 22 Proben): Handrechnung je Führung, Schnittmenge an beiden
  Grenzen, Hub- und Gerätegrenze, Teillast und Takt, kein Bedarf, Luftgerät, Energie- und Stromprobe,
  Gesamtleistungszahl, Jahresreihe, Determinismus.

## Prüfungen

Zusammengeführt mit K‑F5 auf `origin/ios_migration_september` `c9df9630c`; Zweig `ec3d1af85`, Zusammenführung
`d4ef736d3` (ohne Konflikte).

- Kern-Filter (Release) und Windows-Schale auf Linux: 0 Fehler.
- EPOS.Kern.Tests voll: 14 832 Tests, 14 824 grün, 7 übersprungen, 1 rot — `ZahlungsgliederungTests.Die_Bruecke_stapelt_die_Differenzspalte` (Modellbreite 611 statt 610), auf unberührtem origin ebenso rot (#1181; dieselbe Ursache wie die ChartProben unten): fremd. Die vier übrigen Projekte im selben Lauf: EPOS.UI.Tests 9 752, KiKern.Tests 553, SpeicherEngine.Tests 397, SpeicherPlanung.Tests 27 (1 übersprungen), alle grün.
- Referenzlauf: 38 von 38 Projekten gegen `2026-10-17_R59_Ergebnisreihen` `GESAMT: PASS`, CSV byte-gleich 1 233/1 233 —
  der Lauf ist unberührt, die Klasse ist nicht verdrahtet. Übrige Gate-Zahlen im Protokoll
  [K‑F5](2026-10-17_K-F5_Rueckkuehlwerk_Oberflaeche.md).

## Abweichungen

- Die Kondensatorspreizung von 5 K ist ein vorläufiger Festwert (Normpunkt 30/35 °C); die Gerätespalte dafür kommt mit dem
  Schema.

## Offen

- **KWN‑1b** Schema (Schritt 242 angemeldet): `Kopplung_*` an `Tab_Energieanlagen`, Kälteziele in `Z_AnlageSenke`; wartet,
  bis die Schritte 240 und 241 auf `origin` liegen (239 liegt dort).
- **KWN‑1c** Verdrahtung im Lauf (Kopplungsbuchung, Kopplungsprobe, Temperaturpaar der Prozesskälte für gekoppelte Geräte),
  danach ein Referenzprojekt mit Kopplung und neue Basis.
