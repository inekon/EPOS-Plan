# Protokoll UB-E1 — Übergabegrenze der Wärmepumpe: Kernklassen, Herleitungszeile, Schnellwahl (09.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#837**. Commits `7cc917dcb` (E1-a), `55bddb995` (E1-b), Merge `d04af08f8`;
Schemaschritt 203 `UebergabegrenzeSchema` für E2 angemeldet (`a91e9603c`). **Entscheid:** E109. Keine Rechenwirkung, kein Schema.
Konzepte: Fachkonzept Übergabegrenze/Bivalenz (Fassung 2) und Umsetzungskonzept Abschnitt 10.

## 1 Auftrag und Entscheidlage

- **E109** (Anwender, 08.10.2026): Konzepte lesen und ab 09.10.2026 02:00 Uhr umsetzen; Reihenfolge UB-E1 → E2 → E3 → E4 → E5,
  Bedienung nach dem Mockup `Mockups/Waermepumpe_Bivalenz_Uebergabe.html`; Wellen und Pushes nach grünem Gate.
- Entscheide der Orchestrierung zu den Fragen von E1-a: (1) Kältemittel ohne Tafelwerte (R134a, R407C, R454C, R455A,
  R1233zd(E)) rechnen mit der allgemeinen Vorgabe — keine eigenen Werte ohne Quelle; (2) Hybrid-Mindestanteil bei teilparallel
  30 % wie parallel (FK 2.10); (3) „höchstens der eingegebene Abschaltpunkt“ (FK 4.5) ist als Deckel nach UB-Q4 a gebaut,
  maßgebend der wärmere Wert — der Satz im Fachkonzept wird mit UB-E5 („wie gebaut“) geglättet; (4) Hybrid-Anteil =
  Kennfeldleistung bei −7 °C bezogen auf die Kesselleistung (§ 43 GModG, Teillastpunkt A), nicht die übergabebegrenzte Leistung.

## 2 Wellen

| Welle | Commit | Ergebnis |
|---|---|---|
| E1-a | `7cc917dcb` | `EPOS.Kern/Allgemein/Simulation/Bivalenz/`: `Bivalenzvorgaben` (Tafel FK 6.3, elf Kältemittelcodes), `Uebergabegrenze` (Kalibrierung je Zone, Nullstelle bei Höchstvorlauf, Newton ≤ 8 Schritte mit Bisektionsrückfall, 0,27 µs je Aufruf), `Bivalenzrechner` (`Punkte`, `Massgebend`), `Bivalenzherleitung` (Datenobjekt, Diagrammmodell); `EPOS.Kern.Tests/UebergabegrenzeTests` 25 Proben |
| E1-b | `55bddb995` | `EPOS.Kern/Controller/BivalenzQuelle.cs`, `EPOS.UI.Daten/Erzeuger/BivalenzAbbildung.cs`, `EPOS.UI/Dialoge/Waermepumpe/WaermepumpeBivalenz.cs`, Herleitungszeile unter „Betrieb“ und Schnellwahl des Höchstvorlaufs in `WaermepumpeKonfiguration.razor`, KI-Sicht, 30 Ressourcenschlüssel je Sprache |
| Merge | `d04af08f8` | `ub-e1b` in den Arbeitszweig |

## 3 Dateien (E1-b)

- Kern: `BivalenzQuelle` (Übergabe der gekoppelten Gebäude über `SimulationWaermebedarf.UebergabeEingang`, Kesselleistung,
  Kennfeld am Höchstvorlauf); einzige Kernänderung außerhalb `Bivalenz/`: `SimulationWaermebedarf.Flaechenfaktor` (14 Zeilen).
- Oberfläche: Baustein `EPOS.UI/Bausteine/Herleitungszeile.razor` in `div[data-gruppe=bivalenz-herleitung]`; Schnellwahl
  (Klappliste Kältemittel, vier Knöpfe) als `epos-knopf` in `epos-zeilenknoepfe` mit `aria-pressed`; schreibt nur ein leeres `Vorlauf_Max`.
- Tests: `EPOS.UI.Tests/Dialoge/WaermepumpeBivalenzTests`, `BivalenzQuelleTests`; `KiMaskenabdeckungWacheTests` 22 → 23 Eingabestellen.

## 4 Proben

**E1-a** (`UebergabegrenzeTests`, 25 Proben):

| Probe | Ergebnis |
|---|---|
| 75/60/20 °C, n 1,3, 10 kW bei 55 °C | 5,6796 kW / 46,481 °C / 8,519 K |
| gleiche Zone bei 50 / 60 / 70 °C | 4,6805 / 6,7144 / 8,8773 kW (Rückläufe 42,979 / 49,928 / 56,684 °C) |
| Exponent 1,0 | gegen geschlossene Form |
| Exponent 1,1 und 1,4 | gegen Bisektion ≤ 1e-9 |
| Höchstvorlauf 75/80 °C | keine Begrenzung |
| Höchstvorlauf → Raumtemperatur | Leistung → 0, kein NaN |
| Flächenheizung 35/28 °C, n 1,1 | keine Begrenzung |
| Bivalenzpunkte | +1,825 / −3,549 °C; Kennfeld allein −3,843 °C |
| ohne Vorwärmbetrieb | zweiter Punkt = erster |
| Abschaltpunkt | −10 °C → maßgebend −3,549 °C; +3 °C → +3 °C |
| zwei Zonen | Summe, Rücklauf massenstromgewichtet |
| ohne Zonen | 5,680 kW, Anteil 0,568 |
| Laufzeit | 0,27 µs je Nullstelle (200 000 Aufrufe) |

**E1-b:** Kern-Filter 0 Fehler; EPOS.UI.Tests 53/53 gefiltert und 522/522 erweitert; EPOS.Kern.Tests 52/52 und 338/338
gefiltert, BivalenzQuelle 2/2; SqlDialektPruefer 2 528 Texte, 0 Fundstellen; Windows-Schale auf Linux 0 Fehler; BOM vorhanden.
Probe an Projekt 1047: „Einbindung nicht gesetzt — Übergabegrenze ruht. Übergabe bei Höchstvorlauf 55 °C: 38,7 kW von
38,7 kW Heizlast (100 %), Rücklauf 45,0 °C, Spreizung 10,0 K · erster Bivalenzpunkt −4,4 °C …“.

## 5 Festlegungen

**E1-a:** eigener Löser `Uebergabegrenze.Nullstelle` mit Einschlussintervall und Bisektionsrückfall (gleiche Gleichung wie
`Waermeuebergabe.Loesen`); Heizkurve des zweiten Punkts über die vorhandene `Heizkurve` (Niveau 0, Steilheit 1), bei Zonen
die Gebäudeübergabe; Suchbereich der Bivalenzpunkte zwischen Auslegungs-Außentemperatur und Raumtemperatur, sonst NaN; zweiter
Punkt nie über dem ersten; Abschaltpunkt nur bei teilparallel und alternativ; Aufzählung `Kaeltemittelherkunft` (Name
`Vorgabeherkunft` war belegt); Leistungseinheit frei, `Uebergabezone.AusKennwerten` in W; alle Klassen `internal`, `#nullable enable`.

**E1-b:** `Einbindung` null bis Schema 203 → Kennzeichen NichtWirksam, Vorwärmbetrieb in E1 immer aus; OhneKopplung bei Stufe
aus oder ohne gekoppeltes Gebäude (`IDEAL`, `Heizkreis_Aktiv` aus); Unvollstaendig bei Zonen, Verbrauchsangabe oder fehlender
Klimaregion; OhneKennfeld; Befund; das DTO trägt Zahlen, die Oberfläche formatiert (Kultur); Kältemittelcodes über das DTO;
Hinweiszeile der Schnellwahl ohne „speichert das Kältemittel“; Schlüssel der Codes ohne Satzzeichen (`R1234ZEE`). Solange die
Spalte `Einbindung` fehlt (E2), zeigt die Zeile „Einbindung nicht gesetzt — Übergabegrenze ruht“ vor den gerechneten Werten.

## 6 Offen

- Zweiter Bivalenzpunkt −3,549 °C wird als −3,5 °C gerundet, Fachkonzept und Mockup nennen −3,6 °C (Darstellung, mit E5 glätten).
- `BivalenzHerleitung` noch nicht in der Feldkarte `KiDialoge` (E2-c).
- Herleitungszeile „eingegeben … maßgebend …“ des Abschaltpunkts in E2-c.
- Quelltemperatur des Kennfelds = Außentemperatur; für Sole-Wärmepumpen Näherung (E2-b/E3).

## 7 Gate und CI

Kern-Filter-Bau rc=0; EPOS.UI.Tests 7 875/7 875, KiKern.Tests 549/549, SpeicherEngine.Tests 397/397, SpeicherPlanung.Tests 27 grün (1 übersprungen); Dokumentationswachen 35/35; Windows-Schale auf Linux rc=0; Designer unverändert (16 206 Einträge); SqlDialektPruefer 2 528 Texte, 0 Fundstellen; Werkzeugtests Formularkarte 124, Auslieferungsvorlage 61, Gebäudevergleich 24, Zapfprofilvalidierung 39 grün; BOM in Markdown und Konfliktmarker keine; Referenzlauf gegen R44 26/26 PASS (8 932 241 Werte), 841/841 CSV byte-gleich. CI: Kern-Lauf 37865695440 auf `d04af08f8` (Sitzungszweig) — Vermerk folgt.

Kern-Lauf 37865695440 auf `d04af08f8` (Sitzungszweig `claude/gebaeudesimulation-ub`) — Vermerk folgt.
