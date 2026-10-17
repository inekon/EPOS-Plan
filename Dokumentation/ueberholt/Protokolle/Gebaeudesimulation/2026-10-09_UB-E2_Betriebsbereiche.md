# Protokoll UB-E2 — Betriebsbereiche B0–B4, Vorwärmbetrieb, Schemaschritt 205, Referenzprojekt 1060, Basis R45 (09.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#853** (die Nummer #838 war auf origin bereits an die Welle A der Sitzung Dialoge vergeben).
Commits E2-a `c1bac61d7` … `8b3bc57f9`, E2-b `368a237c7` … `014279a96`, E2-c `9b76fd0e4` … `f9a30b9d4` (Nachzug `f2e383612`),
E2-d `3f3b5fe36` … `283d18c94`; Merge `b49d3ab48`; Nachzug der Orchestrierung `b2fd1d4db` (`ParameteruebersichtTests` 12 → 20).
**Entscheid:** E109. Konzepte: Fachkonzept Übergabegrenze/Bivalenz (Fassung 2) und Umsetzungskonzept Abschnitt 10.

## 1 Auftrag und Entscheidlage

- **E109** (Anwender, 08.10.2026): Reihenfolge UB-E1 → E2 → E3 → E4 → E5, Bedienung nach dem Mockup
  `Mockups/Waermepumpe_Bivalenz_Uebergabe.html`; Wellen und Pushes nach grünem Gate.
- E2 bringt den Schemaschritt 205 `UebergabegrenzeSchema` (angemeldet mit E1 als 203, gegen origin auf 205 umgehÃ¤ngt), die Betriebsbereiche B0–B4 samt Vorwärmbetrieb im
  Rechenweg, die Gruppe „Bivalenz und Übergabe“ in der Wärmepumpenmaske, das Referenzprojekt 1060 und die Basis R45.
- Entscheid der Orchestrierung zur Sperre „Rücklauf nie“: Vergleich mit dem Rücklauf an der Übergabegrenze θ_R,UE statt mit dem
  Auslegungsrücklauf.

## 2 Wellen

| Welle | Commits | Ergebnis |
|---|---|---|
| E2-a | `c1bac61d7`, `d7093a171`, `d4471d0d0`, `8b3bc57f9` | Schemaschritt 205 (gegen origin umgehängt: 203 ist KM2 `KaeltemaschinenTypkennfelderSchema`, 204 ist ZK `ZonenKatalogSchema`) mit 43 nullbaren Spalten: je acht Gerätespalten an `Tab_WP`/`Tab_WP_STAMM` samt `Kaeltemittel`, `Ruecklauf_Max` an `Tab_BHKW(_STAMM)`, `Einbindung` und `Vorwaermbetrieb` an `Tab_Energieanlagen`, 13 Ergebnisspalten am Modul, 10 am Projektergebnis; vier Leser, Testdatenbank 202 → 203, `Katalogfassung.STUFE1` um neun Katalogspalten, `ParameterVerwendung`, `UebergabegrenzeSchemaTests` (6) |
| E2-b | `368a237c7` … `014279a96` | `Bivalenz/Betriebsbereich.cs` (`Bivalenzrechner.Bereich`, Laufobjekt `Bivalenzmodul`, Opt-in über `Einbindung`), `SimulationWaermepumpe` mit `Heizkreisruecklauf` und `Vorwaermvorlauf`, Kennfeld bei min(θ_V, θ_WP,max) mit Quelltemperatur der Stunde, Bereichswert statt Kennfeldleistung, AK3-Angebot mit optionalem Bivalenzobjekt, vier Verfügbarkeitsgründe (`UEBERGABE_HOECHSTVORLAUF`, `SPREIZUNG_MAX`, `SPREIZUNG_MIN`, `RUECKLAUF_MAX`), Rücklaufstufe `Vorwaermer` der Kesselkennlinie mit Platzhalter im Brennwert-Hinweis, Ergebnisspalten über `ErgebnisCtrl`, Referenzlauf-Export nur bei gesetzter Einbindung; `BetriebsbereichTests` (10) |
| E2-c | `9b76fd0e4` … `f9a30b9d4`, Nachzug `f2e383612` | Gruppe „Bivalenz und Übergabe“ ersetzt „Betrieb“ in `WaermepumpeKonfiguration.razor` (Einbindung mit Vorbelegung PUFFER/DIREKT bei Neuanlage, Vorwärmbetrieb, Lesewerte mit Herkunft, Herleitungszeile des Abschaltpunkts, fünf weiche Sperren und zwei Hinweise als `Warnbanner`), Prüfregeln im Kern `Bivalenz/Bivalenzpruefung.cs`, Schreibwege (`AnlagenSql` mit festen Spalten, `WErzeugerCtrl`, Kältemittel in der Projektkopie `Tab_WP`), KI-Sicht und Feldkarte `KiDialoge`, 44 Ressourcenschlüssel je Sprache, bunit |
| E2-d | `3f3b5fe36`, `b164c05cd`, `ffc847236`, `02bd61d37`, `a6e47462b`, `283d18c94` | Fehlerbehebung `BivalenzAufbauen`; Saatskript `referenzprojekt_1060_uebergabegrenze.cs` und Wache `UebergabegrenzeReferenzprojektWacheTests` (3); Testdatenbank (93 782 016 Byte, SHA-256 `cd50d465…`, Schemastand 203); Basis **R45** `2026-10-09_R45_Uebergabegrenze` (27 Projekte, 879 CSV, 6 071 Skalare; R44 nach `ueberholt/Referenzbasen`); `CLAUDE.md` (Projekt 1060, zehn CI-Projekte, Einfrierregel „gesäte Übergabegrenzdaten“); `kern.yml`/`ios.yml`; Zähltests |
| Merge | `b49d3ab48` | `ub-e2d` in den Arbeitszweig |
| Nachzug | `b2fd1d4db` | `ParameteruebersichtTests`: Spalten ohne Verwendung 12 → 20 (acht Gerätespalten) |

## 3 Dateien

- Kern: `EPOS.Kern/Allgemein/Simulation/Bivalenz/` (`Betriebsbereich.cs`, `Bivalenzpruefung.cs`), `SimulationWaermepumpe`, AK3-Angebot, Kesselkennlinie, `ErgebnisCtrl`, `SchemaMigration` (`UebergabegrenzeSchema`), `Katalogfassung`, `ParameterVerwendung`.
- Oberfläche: `WaermepumpeKonfiguration.razor`, `AnlagenSql`, `WErzeugerCtrl`, KI-Sicht und Feldkarte `KiDialoge`, Ressourcen in beiden Sprachen.
- Referenz: `Referenzlaeufe/Kenndaten_Test.sqlite`, Basis R45, Saatskript `referenzprojekt_1060_uebergabegrenze.cs`.
- Tests: `UebergabegrenzeSchemaTests` (6), `BetriebsbereichTests` (10), `UebergabegrenzeReferenzprojektWacheTests` (3), bunit der Maske.

## 4 Proben

| Probe | Ergebnis |
|---|---|
| Vorwärmen bei 0 °C | 4,40 kW, Anteil 0,704 |
| Referenzlauf CI-Auswahl nach E2-b | 9/9 PASS, 303/303 CSV byte-gleich |
| Sperre „Rücklauf nie“ | R744 40 °C gegen 46,5 °C schlägt an, R410A nicht |
| Projekt 1060, Bereiche | B1 3 987 h / 39,61 MWh, B3 1 308 h / 13,64 MWh, B2 und B4 0 h |
| Projekt 1060, Bivalenzpunkte | +1,83 / −3,55 °C |
| Projekt 1060, Übergabegrenze | 22,00 kW bei Heizlast 38,73 kW |
| Projekt 1060, Erzeuger | Wärmepumpe 53,25 MWh, Kessel 15,56 MWh (1 308 Laufstunden, 952 h Stufe Vorwärmer) |
| Projekt 1060, Komfort | 993 Unterschreitungsstunden / 1 484 Kh |
| Vergleich gegen R44 | 26/26 Bestandsprojekte PASS, 841/841 byte-gleich, 1060 neu (38 CSV) |
| gestörter Lauf | GESAMT PASS, 835/879 byte-gleich |
| Laufzeit Projekt 1058 | 3,26 → 3,30 s |

## 5 Festlegungen

**E2-a:** Hilfsfunktionen `Stunden`/`NichtNegativ` aus `AnlagenfahrplanSchema` auf `internal`; `ParameterVerwendung` führt neun Spalten
als „keine Verwendung, noch ohne Leser“ bis E3; `WPS_LBL_*`/`BHKWK_LBL_RUECKLAUF_MAX` vorab für das Stammblatt; vier Spaltenfolge-Proben nachgezogen.

**E2-b:** θ_WP,max = `Vorlauf_Max`, sonst `Vorlauf`; σ_min fest 3 K bis E3, Φ_Hydraulik unbegrenzt; θ_V,soll und θ_R aus dem gefahrenen
Projektheizkreis; die Bereichsregel wirkt in der Bedarfsphase auf das ganze Modul, Ladephasen bleiben unberührt; zweiter Erzeuger = Kessel in
`Tool_1..4` oder Heizstab; ohne zweiten Erzeuger bleibt es über θ_WP,max bei B1 mit Grund `UEBERGABE_HOECHSTVORLAUF`; „nur Kessel“ = B0 + B4;
Zähler `Spreizung_Unterschritten_h`/`Ruecklauf_Ueberschritten_h` sind 0 mit Bivalenzobjekt, sonst NULL; Raumtemperatur ohne Stundenwert =
Auslegungsraumtemperatur; AK3: fehlender Kreisrücklauf → Vorstunde.

**E2-c:** Einbindung und Vorwärmbetrieb sind feste Spalten der Anlagenanweisung (mit Variante ohne Übergabe); das Kältemittel wird beim
Speichern in die Projektkopie geschrieben, Abbrechen verwirft; Hinweis „Übergabe begrenzt“ nach Mockup; Eingabestellen 23 → 25; Sperre
„Rücklauf nie“ vergleicht mit θ_R,UE (Entscheid der Orchestrierung).

**E2-d:** Brennwertkessel statt Elektrokessel in 1060, damit die Stufe Vorwärmer zählt (Einfrierregel Kesseldaten); Kopie über den
Kopierweg auf MAX+1 und Umnummerierung auf 1060 mit `foreign_key_check`. Zähltests: `GebaeudeKatalogverweisTests` 41 → 42,
`BaualtersklassenSchemaTests` E8 → E9, `ReferenzprojektKaelteerzeugerTests` 80 → 90, `PreisbasisSchrittTests` 22 → 24 / 8 → 9;
Ausnahmelisten um 1060 in `KuehlungSchemaTests`, `KuehlbetriebProgrammeinstellungTests`, `KuehlungErzeugerSchemaTests`,
`KuehluebergabeSchemaTests`, `AnlagenkopplungSchemaTests`, `KesselKennlinieSchemaTests`, `KesselBrennwertNachzugTests`,
`AnlagenfahrplanSchemaTests`, `UebergabegrenzeSchemaTests`, `Ak3KReferenzprojektWacheTests`; Orchestrierung: `ParameteruebersichtTests` 12 → 20.

## 6 Offen (für E3)

- Puffer-Sperre in B0/B4 (Puffer nie wärmer als θ_WP,max).
- Hydraulik- und Rücklaufgrenze; Gerätespalten lesen; Herkunft „Katalog“; Bereichsregel je Kanal prüfen.
- Komfortstunden von 1060 (993 h) mit E3 gegen die Kesselleistung 28 kW bei Heizlast 38,7 kW einordnen.

## 7 Gate und CI

Kern-Filter rc=0; EPOS.UI.Tests zunächst 7 886/7 887 — der rote Zähltest `ParameteruebersichtTests.Nicht_verwendet_wird_benannt` (12 → 20) ist mit
dem Nachzug `b2fd1d4db` behoben, danach 18/18 in der Klasse; KiKern.Tests 549/549; SpeicherEngine.Tests 397/397; SpeicherPlanung.Tests 27 grün
(1 übersprungen); Dokumentationswachen 35/35; Windows-Schale auf Linux rc=0; Designer unverändert (16 266 Einträge); SqlDialektPruefer 2 534 Texte,
0 Fundstellen; Werkzeugtests Formularkarte 124, Auslieferungsvorlage 61, Gebäudevergleich 24, Zapfprofilvalidierung 39 grün; BOM und Konfliktmarker keine;
Referenzlauf gegen R45 27/27 PASS, GESAMT PASS (9 335 471 Werte), 879/879 CSV byte-gleich. CI: Kern-Lauf auf dem Sitzungszweig
`claude/gebaeudesimulation-ub` — Vermerk folgt.
