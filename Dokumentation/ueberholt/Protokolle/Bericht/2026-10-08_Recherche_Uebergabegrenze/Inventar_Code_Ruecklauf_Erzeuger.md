# Inventar: Rücklaufbezug der Wärmeerzeuger außer Wärmepumpe

## Datenfluss des Rücklaufs

`Anlagenkopplung.Kreis(...)` (EPOS.Kern/Allgemein/Simulation/Anlagenkopplung.cs:448) liefert
je Stunde `(VorlaufC, RuecklaufC)` des Heizkreises aus der Gebäuderechnung
(`HeizkreisErgebnis.RuecklaufC`, Zeile 55/427/460/463 — wärmemengengewichtetes Mittel über alle
Gebäude). Dieser Heizkreisrücklauf fließt in `SimulationSPK` über den Eingang
`RuecklaufPaarLesen`/`Heizkreisruecklauf` (Kommentar SimulationSPK.cs:345/353/2502) in
`RuecklaufDerStunde(i, stunde, out stufe)` (Zeile 2020), die ihn an
`Kesselkennlinie.Ruecklauf(heizkreisC, speicherC, paarC, out stufe)` weiterreicht
(Kesselkennlinie.cs:345–350). Diese Methode ist die **einzige** Stelle, die einen
projektweiten "Rücklauf" für Kesselzwecke ermittelt — in der festen Stufenfolge
Heizkreis → Speicher (`_speicherRuecklauf[i]`, aus `sp.Geschichtet ? sp.T_unten : sp.RL_eff`,
SimulationSPK.cs:1637) → Paar (`RuecklaufPaarLesen`) → Rückfall (`Ruecklaufstufe.Rueckfall`).
Verbraucht wird dieser Wert ausschließlich in `EtaBrennwert` für Kessel (SimulationSPK.cs:2011)
und als Statistik (`RuecklaufMittel`, `RuecklaufStufenstunden`, Zeilen 2046–2102).

## Heizkessel (Standard/NT/Brennwert, alle Brennstoffarten)

- **Rücklaufbezug heute:** ja — der einzige echte Rücklaufbezug unter den Nicht-WP-Erzeugern.
  `Kesselkennlinie.EtaBrennwert(laststufe, ruecklaufC)` erhöht den Wirkungsgrad bei Brennwert-
  Kesseln, wenn der Rücklauf (Stufenkette oben) niedrig genug ist (η_eff = η_tr(β) + Δ₃₀·g(T_RL)).
  Kein Mindest- oder Höchstrücklauf, keine Abschaltung — nur Wirkungsgradkennlinie.
- **Fundstellen:** `EPOS.Kern/Allgemein/Simulation/Kesselkennlinie.cs:48,345-350,512-514` (Ruecklauf,
  Ruecklaufstufe-Enum); `EPOS.Kern/Allgemein/Simulation/SimulationSPK.cs:2003-2024` (EtaBrennwert-Aufruf,
  RuecklaufDerStunde).
- **Felder:** `Tab_Heizkessel.Kennlinie_Brennwert`, `Brennwert` (Schalter), `Beschreibung` (Bauart),
  `Wirkungsgrad_Teillast30`, `Mindestleistung`, `Anfahrverlust_kWh`, `Mindestlaufzeit_min`. Kein
  eigenes Rücklauf-Feld am Kessel selbst — der Rücklaufwert kommt ausschließlich aus der
  Stufenkette (Heizkreis/Speicher/Paar/Rückfall).
- **Lücke:** kein Mindest- oder Höchstrücklauf am Kessel (etwa zum Schutz vor Kondensation bei
  Nicht-Brennwertkesseln oder vor Taupunktunterschreitung); nur Brennwertkessel nutzen den
  Rücklauf überhaupt, und nur über den Wirkungsgrad, nie als Grenze/Sperre.

## BHKW

- **Rücklaufbezug heute:** nein. `SimulationBHKW.cs` und `BhkwTeillast.cs` enthalten keinen
  Treffer auf `Ruecklauf`/`Vorlauf`/`Temperatur` als Rechengröße (reine Kommentartreffer zu
  "Vorlauf" im Sinn von Vorgängercode, keine Temperaturlogik). Die Teillastkennlinie rechnet
  rein über die elektrische/thermische Leistung, kein Temperatureinfluss.
- **Fundstellen:** `EPOS.Kern/Allgemein/Simulation/SimulationBHKW.cs` (kein Ruecklauf-Treffer),
  `EPOS.Kern/Allgemein/Simulation/BhkwTeillast.cs` (kein Treffer).
- **Felder:** `Tab_BHKW.Grenzleistung`, `Ptherm`, Anlagenfeld `Tab_Energieanlagen.Grenzleistung` —
  alles Leistungsgrenzen, kein Temperaturfeld.
- **Lücke:** vollständig — kein Rücklaufbezug, weder Grenze noch Wirkungsgrad.

## Solarthermie

- **Rücklaufbezug heute:** indirekt — die Kollektoreintrittstemperatur kommt nicht aus dem
  Heizkreisrücklauf, sondern aus der **untersten Zone des Senkenpuffers** der Vorstunde
  (`TemperaturSpeicherSetzen`/`TemperaturSpeicher`, SimulationSolarthermie.cs:932,966,978).
  Kein Treffer auf "Ruecklauf" in dieser Datei selbst — der Projektrücklauf des Heizkreises
  spielt für den Solarkreis keine Rolle, nur die Puffertemperatur.
- **Fundstellen:** `EPOS.Kern/Allgemein/Simulation/SimulationSolarthermie.cs:932,966,978`.
- **Felder:** `Tab_Solarkollektoren.Kdfu`, `Bezugsflaeche`; projektseitig `Arbeitstemperatur_Weg`,
  `Uebertrager_Graedigkeit_K`, `Kollektor_Spreizung_K`, `Solarkreisverluste_Prozent`,
  `Pumpenleistung_W` (laut CLAUDE.md-Einfrierregeln zu Projekt 1049, im Code nicht in dieser
  Datei als Rücklauf-Treffer sichtbar — vermutlich in der Puffer-/Anlagenkopplungsschicht).
- **Lücke:** kein Rücklaufbezug im engeren Sinn (Heizkreisrücklauf irrelevant); kein Höchst-
  oder Mindestrücklauf denkbar, da keine direkte Rücklaufeinspeisung modelliert ist.

## Pufferspeicher

- **Rücklaufbezug heute:** ja, aber als Auslegungsgröße, nicht als Betriebsgrenze. Das
  Temperaturpaar `Vorlauf`/`Ruecklauf` der Pufferzeile bestimmt die nutzbare Kapazität
  `Q_max = Volumen · 1,16 Wh/(l·K) · (Vorlauf − Ruecklauf) / 1000` (SimulationPufferspeicher.cs:10-14).
  Am Kältespeicher ist die Spreizung umgekehrt (Ruecklauf − Vorlauf, Zeilen 60-98). `RL_eff`
  ist die wirksame Rücklauftemperatur aus diesem Paar (Fallback bei fehlendem Paar, Zeile 448-466)
  und wird u. a. als Quelle für `Kesselkennlinie.Ruecklauf`-Stufe "Speicher" verwendet
  (SimulationSPK.cs:1637).
- **Fundstellen:** `EPOS.Kern/Allgemein/Simulation/SimulationPufferspeicher.cs:10-14,60-98,448-466,721`.
- **Felder:** Pufferzeile `Vorlauf`/`Ruecklauf` (Temperaturpaar), abgeleitet `RL_eff`, `VL_eff`,
  `Q_max`, `Geschichtet`/`T_unten` bei Schichtmodell.
- **Lücke:** kein Mindest-/Höchstrücklauf als Betriebsgrenze (z. B. Ladesperre bei zu hohem
  Rücklauf) — das Paar dient nur der Kapazitätsrechnung und als Rücklaufquelle für Kessel.

## Heizstab

- Nur als Zusatzheizer je Wärmepumpe modelliert (`HeizstabJeWaermepumpe.cs`, Schemaschritt),
  kein eigener Rücklauftreffer in dieser Datei — hängt an der WP-Rücklauflogik, kein
  eigenständiger Erzeuger mit Temperaturbezug.

## Fernwärme

- Keine eigene Erzeugerart im Kern gefunden (kein Treffer auf „Fernwärme"/„Fernwaerme" als
  Simulationsobjekt außerhalb von Wirtschaftlichkeits-/Preis-/Bericht-Dateien). Kein
  Rücklaufbezug, weil kein entsprechender Erzeugertyp existiert.

## Fazit zur Lücke gegenüber `Ruecklauf_Max` der Wärmepumpe

Von den gefragten Erzeugern hat **nur der Heizkessel** (Brennwertstufe) einen echten
Rücklaufbezug, und zwar ausschließlich als Wirkungsgradkennlinie ohne Grenze. **BHKW und
Fernwärme haben keinen Rücklaufbezug.** Solarthermie bezieht sich auf die Puffertemperatur,
nicht auf den Heizkreisrücklauf. Der Pufferspeicher nutzt das Temperaturpaar nur zur
Kapazitätsrechnung. Eine Mindest- oder Höchstrücklaufgrenze analog zum geplanten
`Ruecklauf_Max` der Wärmepumpe existiert bei **keinem** dieser Erzeuger.
