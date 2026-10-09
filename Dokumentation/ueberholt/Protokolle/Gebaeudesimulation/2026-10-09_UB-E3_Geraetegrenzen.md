# Protokoll UB-E3 — Gerätegrenzen, Hydraulik- und Rücklaufgrenze, Stammblätter, Basis R46 (09.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#851**. Commits E3-a `7b80d047c`, `2aa6b4b51`, `594a6316e`, `2a4ae9bd2`; E3-b `35647a763`, `11d50500d`,
`74f58fb8a`, `d19280cef`; E3-c `0c2ef0680`, `2e58321fc`; Merges `b09015337`, `6472043e7` (origin #842–#844, Schemaschritt 207); Umnummerierung
`4acd13c25`; Nachzug `731483e36`. **Entscheid:** E109. Konzepte: Fachkonzept Übergabegrenze/Bivalenz (Fassung 2) und Umsetzungskonzept Abschnitt 10.

## 1 Auftrag und Entscheidlage

- **E109** (Anwender, 08.10.2026): Reihenfolge UB-E1 → E2 → E3 → E4 → E5; E3 bringt die Gerätegrenzen der Wärmepumpe, die Hydraulik- und die Rücklaufgrenze,
  die BHKW-Rücklaufgrenze, die Stammblätter mit Kältemittel-Schnellwahl und die Basis R46.
- Entscheide der Orchestrierung: Die BHKW-Grenze wirkt bei „größer als“, wie die der Wärmepumpe. Die Vorgabe von 60 % Mindestvolumenstrom bleibt auch für Projekt 1060.

## 2 Wellen

| Welle | Commits | Ergebnis |
|---|---|---|
| E3-a | `7b80d047c`, `2aa6b4b51`, `594a6316e`, `2a4ae9bd2` | `Bivalenz/Geraetegrenzen.cs`: acht Gerätespalten der Projektkopie `Tab_WP`, NULL → Vorgabe der Kältemittelklasse, Herkunft Katalog / Vorgabe nach Kältemittel / Vorgabe / abgeleitet, θ_R,grenz = min(`Ruecklauf_Max`, θ_WP,max − σ_min). `Hydraulikgrenze.cs`: DIREKT mit Überströmventil ṁ_WP = max(ṁ_HK, ṁ_min), σ_max und Takten unter σ_min mit Zähler `Spreizung_Unterschritten_h`; PUFFER Ladeseite ṁ_WP·c_p·σ_max, nie über θ_WP,max, Entladeseite benannt nicht gerechnet; WEICHE Mischungsbilanz mit Übergabegrenze am gemischten Vorlauf (`Uebergabegrenze.ZoneGemischt`). `Ruecklaufgrenze.cs`: Rücklauf zur Wärmepumpe direkt / gemischt / unterste Pufferzone, über θ_R,grenz → 0 mit Grund `RUECKLAUF_MAX` und Zähler `Ruecklauf_Ueberschritten_h`; R744-Faktor auf Leistung und COP, Strom unverändert. BHKW-Rücklaufgrenze in `SimulationBHKW` (`Ruecklauf_Max` der Projektkopie, Grund `RUECKLAUF_MAX`). AK3-Angebot mit `KapazitaetBegrenzen`. `ParameterVerwendung`: neun Spalten auf „Simulation“. Proben `GeraetegrenzenTests`, `HydraulikgrenzeTests`, `RuecklaufgrenzeTests` |
| E3-b | `35647a763`, `11d50500d`, `74f58fb8a`, `d19280cef` | BHKW-Grenze wirkt über der Grenze. `EPOS.Kern/Controller/GeraetegrenzWerte.cs`: Lesen, Schreiben, Kopieren der neun Katalogspalten mit Bereichsprüfung (`WPStammCtrl`, `WPCtrl.CopyFromStamm`, `BHKWStammCtrl`, `BHKWCtrl`). Stammblatt Wärmepumpe: Gruppe „Gerätegrenzen“ (`WaermepumpeGeraetegrenzenFelder.razor`) mit Klappliste Kältemittel und sieben Zahlenfeldern, Schnellwahl füllt nur leere Felder. BHKW-Stammblatt „Höchster Rücklauf (Abschaltgrenze)“ mit Vorgabe 70 °C und Hinweis gegen den Auslegungsrücklauf. KI-Feldkarten 21/18 Felder; 19 Ressourcenschlüssel je Sprache; `KatalogabgleichTests` Prüfsummenprobe, `ProjektpaketAnhebungTests`, Lückenprobe wieder nur `Modulkosten` |
| E3-c | `0c2ef0680`, `2e58321fc` | Referenzlauf 27/27 gegen R45: 26 Bestandsprojekte byte-gleich, 1060 ändert sich. Basis **R46** `2026-10-09_R46_Geraetegrenzen` (27 Projekte, 879 CSV, 6 071 Skalare), R45 nach `ueberholt/Referenzbasen`, `CLAUDE.md`, `kern.yml`, `ios.yml` nachgezogen |
| Merges | `b09015337`, `6472043e7` | origin #842–#844 (Schemaschritt 207); Umnummerierung `4acd13c25` (#842/#843 → #850/#851) |
| Nachzug | `731483e36` | fremder Zähltest `StromspeicherUebernahmeTests` (KM1): auch die Kältemaschine führt einen Katalogimport-Hinweis |

## 3 Dateien

- Kern: `EPOS.Kern/Allgemein/Simulation/Bivalenz/` (`Geraetegrenzen.cs`, `Hydraulikgrenze.cs`, `Ruecklaufgrenze.cs`), `SimulationBHKW`, `SimulationWaermepumpe`, AK3-Angebot, `Uebergabegrenze`, `ParameterVerwendung`, `EPOS.Kern/Controller/GeraetegrenzWerte.cs`, `WPStammCtrl`, `WPCtrl`, `BHKWStammCtrl`, `BHKWCtrl`.
- Oberfläche: `WaermepumpeGeraetegrenzenFelder.razor`, Stammblätter Wärmepumpe und BHKW, KI-Feldkarten, Ressourcen in beiden Sprachen.
- Referenz: Basis R46 `Referenzlaeufe/2026-10-09_R46_Geraetegrenzen`.
- Tests: `GeraetegrenzenTests`, `HydraulikgrenzeTests`, `RuecklaufgrenzeTests`, `KatalogabgleichTests`, `ProjektpaketAnhebungTests`, `StromspeicherUebernahmeTests` (Nachzug).

## 4 Proben

| Probe | Ergebnis |
|---|---|
| direkt 90/70 | Leistung W_H · 10 K |
| Weiche, gemischter Vorlauf | 46,67 °C |
| Mindestvolumenstrom | wirkt als Höchstspreizung |
| Überströmventil | taktet unter 0,36 · Φ_N |
| R744 bei 34 °C | Faktor 0,90 |
| R744 bei 40,1 °C | Leistung 0 |
| BHKW bei 71 °C Rücklauf | aus |
| Vergleich gegen R45 | 26 Bestandsprojekte byte-gleich, 1060 geändert |
| Projekt 1060, Betriebsstunden B1 / B3 / nur Kessel | 3 987 / 1 308 / 0 h → 3 964 / 620 / 711 h |
| Projekt 1060, Zähler | `Ruecklauf_Ueberschritten_h` 711, `Spreizung_Unterschritten_h` 2 566 |
| Projekt 1060, Erzeuger | Wärmepumpe 53,25 → 49,60 MWh, Kessel 15,56 → 19,11 MWh |
| Projekt 1060, Bivalenzpunkt der Anlage | 1,82 → 2,34 °C |
| Projekt 1060, Deckung Heizung | 76,61 → 71,35 % |
| Projekt 1060, Komfort | unverändert |

Ursache der Änderung von 1060: Der Mindestvolumenstrom von 60 % der 35-kW-Wärmepumpe (4,2 kW/K) liegt über dem Heizkreisstrom 75/60 (2,6 kW/K); das Überströmventil hebt den Rücklauf der Wärmepumpe.

## 5 Festlegungen

**E3-a:** DIREKT heißt immer mit Überströmventil (kein eigenes Feld). Die Hydraulikgrenze ist unter σ_max,eff unbegrenzt und wird am Vorlauf min(θ_V,soll, θ_WP,max) gerechnet. Weiche ohne Nennleistung rechnet wie DIREKT.
Puffer: Die Rücklaufgrenze prüft bei jedem Kältemittel die unterste Pufferzone; „nie über θ_WP,max laden“ wirkt über B0/B4; die Entladeseite steht als Protokollhinweis. Gerätespalten: Der Lauf liest nur die Projektkopie `Tab_WP`, der Dialog die Projektkopie vor `Tab_WP_STAMM`.
Ein leeres Abwertungsfeld nimmt die Vorgabe der Klasse (nur R744), eine gepflegte 0 schaltet ab. AK3: `KapazitaetBegrenzen` mit Kreisrücklauf; R744-Faktor auf den COP und unterste Pufferzone sind dort benannt nicht gerechnet.

**E3-b:** Die Gerätegrenzen sind eine eigene Stammblattgruppe zwischen Kennlinie und Kenndaten. Die weiche Prüfung läuft im Feld, der Speicherweg weist außerhalb des Bereichs benannt ab. Die Schnellwahl füllt nur bei Klassen mit eigener Vorgabe (R410A, R32, R290, R1234ze(E), R744). KI `feld_setzen` arbeitet ohne Schnellwahl. BHKW-Labels ohne Doppelpunkt.

**E3-c:** Die Vorgabe 60 % Mindestvolumenstrom bleibt für 1060 (Entscheid der Orchestrierung). Ein gestörter Lauf wurde nicht gemessen.

## 6 Offen

- Projekt-BHKW: `BHKWCtrl.Update` schreibt `Ruecklauf_Max` nicht; die Übernahme Projekt-BHKW → Katalog läuft ohne die Spalte.
- Der Kommentar „ELF von achtzehn Fachspalten“ in `ParameterVerwendungTests` ist überholt.
- Komfort von 1060 (993 h) gegen Kessel 28 kW bei Heizlast 38,7 kW einordnen.
- Wiki und Logbuch in E5.

## 7 Gate und CI

Kern-Filter-Bau rc=0; EPOS.UI.Tests 7 921/7 921, KiKern.Tests 549/549, SpeicherEngine.Tests 397/397, SpeicherPlanung.Tests 27 grün (1 übersprungen); Dokumentationswachen 35/35; Windows-Schale auf Linux rc=0; Designer unverändert (16 335 Einträge); SqlDialektPruefer 2 539 Texte, 0 Fundstellen; Werkzeugtests Formularkarte 124, Auslieferungsvorlage 61, Gebäudevergleich 24, Zapfprofilvalidierung 39 grün; BOM und Konfliktmarker keine; Referenzlauf gegen R46 27/27 PASS, GESAMT PASS (9 335 471 Werte), 879/879 CSV byte-gleich.
CI: Kern-Lauf auf dem Sitzungszweig `claude/gebaeudesimulation-ub` auf `731483e36` — Vermerk folgt.
