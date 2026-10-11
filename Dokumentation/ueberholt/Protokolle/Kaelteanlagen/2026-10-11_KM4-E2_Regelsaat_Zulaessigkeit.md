# Protokoll KM4‑E2 — Regelsaat `FGAS`, Zulässigkeitsprüfung nach Anhang IV, Warnkriterium, Kältebahn, Berichtsspalte

**11.10.2026 · Sitzung Kälteanlagen · Wellen E2‑a und E2‑b des Kältemittels als Stammdatum.** Grundlage:
[Fachkonzept](../../../aktuell/Kälteanlagen/Konzept_Kaeltemittel_Stammdatum_EPOS-Plan.md) 3.1, 3.3, 4.5, 5.1, 9 (E123,
E126); [Umsetzungskonzept](../../../aktuell/Kälteanlagen/Umsetzungskonzept_Kaeltemittel_Stammdatum_EPOS-Plan.md) 3.2. Ohne
Schemaschritt; Basis unverändert.

## Was gebaut ist

- **E2‑a — Regelsaat und Prüfung:** Gesetzeskatalog Klasse `FGAS`, **Generation 10**, dreizehn Zeilen nach Anhang IV der
  VO (EU) 2024/573 (`GesetzKatalog.FgasRegeln`), am amtlichen deutschen Wortlaut geprüft, Fundstelle je Zeile in `Quelle`;
  Klassenvorrat, Anzeige und Ressource `GESETZ_KLASSE_ANZ_FGAS` (de/en). Reine Klassen
  `Kaeltemittelregeln` (Leser des Vokabulars `FGAS_IV_<GERAETEKLASSE>_<BAND>_<KRITERIUM>`, unbekannte Schlüssel benannt
  verworfen; Geräteklasse aus `Geraeteart` der Kältemaschine bzw. `Bauart`/`Typ` der Luft-Wasser-Wärmepumpe) und
  `Kaeltemittelzulaessigkeit` (Fachkonzept 5.1: ohne Jahr kein Ergebnis, Absorption kein Ergebnis, fehlende Größe →
  Klasse 4, Treffer → Klasse 2 mit der Regel des frühesten Stichtags, sonst Klasse 1; Stichtag einer Zeitreihe = Beginn
  der ununterbrochenen Trefferreihe).
- **E2‑b — Projekt, Lauf, Bericht:** `KaeltemittelProjektpruefung` liest `Tab_Projekt.Inbetriebnahmejahr`, die
  Kältemaschinen (Projektkopie, Verweis `ID_Kaeltemittel` vor Text) und Wärmepumpen mit Code. Warnkriterium
  **`KAELTEMITTEL_STICHTAG`** (`Warnkriterien.KaeltemittelPruefen`, weicher Befund je Erzeuger der Klasse 2, nach
  `KaeltespeicherPruefen`), elf Ressourcen `SIMWARN_KAELTEMITTEL_*` (de/en). Laufhinweis
  `kuehl-km-kaeltemittel-<Anlage>` für Klasse 4 (`SimulationControl.KaeltemittelHinweiseMelden`, nur mit Jahr). Warntext
  am Knoten der Kältemaschine in der Kältebahn (`SchemaModell.KaelteBahnAnlegen`). Bericht: `VariantenDaten.Kaeltemittel`
  vom Sammler, Spalte „Kältemittel“ in `Berichtstabellen.Kaelteerzeuger`, sobald ein Erzeuger einen Code trägt, Stern und
  Fußnote bei Klasse 2. **Kein neues Vorlagenfeld; die Berichtskatalogfassung bleibt 20.**
- **Warnung, nie Sperre:** Anhang IV untersagt das Inverkehrbringen (Art. 11 Abs. 1), nicht den Betrieb; das
  Inbetriebnahmejahr ist ein konservativer Stellvertreter, die Ausnahmen kennt die Prüfung nicht.

## Regeltabelle (Generation 10)

Band: `BIS12` = (0, 12] kW, `UEBER12` = (12, ∞), `UEBER12BIS50` = (12, 50], `ALLE` = jede Nennleistung. Grenze = GWP
„von … oder mehr“, 0 = jedes F-Gas. Alle Zeilen `GESICHERT`.

| Schlüssel | Stichtag | Grenze | Band | Kriterium | Fundstelle (Anh. IV) |
|---|---|---|---|---|---|
| `FGAS_IV_KUEHLER_ALLE_HFKW` | 2020 | 2 500 | ALLE | HFKW | Nr. 7 a, S. 71/86 |
| `FGAS_IV_KUEHLER_BIS12_GWP` | 2027 | 150 | BIS12 | GWP | Nr. 7 b, S. 72/86 |
| `FGAS_IV_KUEHLER_BIS12_FGAS` | 2032 | 0 | BIS12 | FGAS | Nr. 7 c, S. 72/86 |
| `FGAS_IV_KUEHLER_UEBER12_GWP` | 2027 | 750 | UEBER12 | GWP | Nr. 7 d, S. 72/86 (Lesart ≥ 750, E126) |
| `FGAS_IV_MONOBLOCK_BIS12_GWP` | 2027 | 150 | BIS12 | GWP | Nr. 8 b, S. 72/86 |
| `FGAS_IV_MONOBLOCK_BIS12_FGAS` | 2032 | 0 | BIS12 | FGAS | Nr. 8 c, S. 73/86 |
| `FGAS_IV_MONOBLOCK_UEBER12BIS50_GWP` | 2027 | 150 | UEBER12BIS50 | GWP | Nr. 8 d, S. 73/86 |
| `FGAS_IV_MONOBLOCK_ALLE_GWP` | 2030 | 150 | ALLE | GWP | Nr. 8 e, S. 73/86 |
| `FGAS_IV_SPLIT_LUFTWASSER_BIS12_GWP` | 2027 | 150 | BIS12 | GWP | Nr. 9 b, S. 74/86 |
| `FGAS_IV_SPLIT_LUFTLUFT_BIS12_GWP` | 2029 | 150 | BIS12 | GWP | Nr. 9 c, S. 74/86 |
| `FGAS_IV_SPLIT_BIS12_FGAS` | 2035 | 0 | BIS12 | FGAS | Nr. 9 d, S. 74/86 |
| `FGAS_IV_SPLIT_UEBER12_GWP` | 2029 | 750 | UEBER12 | GWP | Nr. 9 e, S. 74/86 |
| `FGAS_IV_SPLIT_UEBER12_GWP` | 2033 | 150 | UEBER12 | GWP | Nr. 9 f, S. 75/86 (Zeitreihe zu 9 e) |

Nicht gesät: Nr. 9 a (Füllmengenkriterium unter 3 kg kennt das Regelschema nicht), Nr. 8 a (steckerfertige
Raumklimageräte, keine Geräteklasse), Nr. 4 bis 6 (Kälteanlagen ohne Kühler, keine Geräteklasse), Art. 13 (Bestand,
KM4‑Q12 b). Split, Multisplit und VRF der Kältemaschine sind in KM4 „nicht prüfbar“.

## Stoffklasse: HFKW gegen F-Gas

- **Kriterium `GWP` und `FGAS`:** fluoriertes Treibhausgas nach Art. 2 Buchst. a (Anhänge I, II, III). Ein Stoff, den die
  Tabelle als Nicht-F-Gas führt, trifft nie; ohne Tabellentreffer (nur Geräte-GWP, KM4‑Q17) gilt ein GWP über der Grenze
  konservativ als F-Gas.
- **Kriterium `HFKW` (nur Nr. 7 a):** HFKW nach Art. 3 Nr. 4 (Anhang I Gruppe 1, keine Olefine). Gruppe `HFKW` → ja;
  `HFO`, `HFCKW` (Anhang II), `NATUERLICH` → nein; `GEMISCH` je Code über `GEMISCHE_MIT_HFKW` — alle acht Gemische der
  Auslieferung enthalten R32, R125, R134a oder R143a —, ein anderes Gemisch und leer/`SONSTIGE` → „Stoffklasse
  unbekannt“ (Klasse 4, sofern das GWP die Grenze erreicht).

## Abweichungen des Fachkonzepts vom Wortlaut (nachgezogen)

- Begriff F-Gas nach **Art. 2 Buchst. a**, nicht Art. 3 Nr. 1; HFKW nach **Art. 3 Nr. 4**.
- Nr. 7 a gilt nur für HFKW (Kriterium `HFKW`, Band `ALLE`), nicht für jedes F-Gas.
- **Nr. 7 d:** Der deutsche Wortlaut nennt „GWP von 750“ ohne „oder mehr“. Gesät war zuerst `VORLAEUFIG`; mit **E126**
  (Anwender, 11.10.2026) gilt die Lesart **≥ 750** wie in Nr. 9 e, die Zeile ist `GESICHERT`, `Quelle` trägt den Vermerk
  (115 Zeichen, CHECK ≤ 120). Die Saatwache verlangt alle Zeilen `GESICHERT`; eine Statusprobe vergleicht die
  FGAS-Zeilen der Testdatenbank mit der Saat.
- Ergänzt: Stichtag einer Zeitreihe, Stoffklasse der Gemische, Klasse 4 bei unentscheidbarer Regel.

## Testdatenbank

Nachsaat über `Werkzeuge/Testdatenbankschema` (`StelleKatalogSicher`): dreizehn FGAS-Zeilen (ID 229–241), Marker 9 → 10,
Zeilenzahl des Gesetzeskatalogs 227 → 240. Danach Zeile `FGAS_IV_KUEHLER_UEBER12_GWP` auf `GESICHERT` mit der Quelle der
Saat (E126). Der Kostenstempel `Tab_Applikation.Kostenkatalog_Geaendert`, den die Saat setzt, steht wieder auf NULL.
Zellvergleich sonst gleich; Schemastand 216 unverändert, `integrity_check` ok, `foreign_key_check` leer, SHA‑256
`9fc5048c…`. Origin hat die Testdatenbank seit `ae1aa1fa7` nicht geändert; die Zusammenführung übernimmt die Seite von E2.

## Befunde der Lauftests (`KaeltemittelZulaessigkeitLaufTests`, Arbeitskopie)

- **Ohne Inbetriebnahmejahr** kein Befund in 1059 und 1063.
- **1059:** Kältemaschine `KWS_WASSER`, 10 kW, **R513A** (GWP 629,5). 2026 zulässig; **ab 2027 Nr. 7 b** (Kühler bis
  12 kW, GWP ≥ 150), Nr. 7 a trifft nicht (kein HFKW-Kriterium ≥ 2 500). Lauf mit Protokollwarnung, Hinweis und
  `bericht.warnungen` samt Stern der Tafel; Warntext nennt „R513A“, „Nr. 7 Buchst. b“, „Nennleistung 10 kW“.
- **1063:** dieselbe Maschine mit 20 kW — Nr. 7 d (über 12 kW, GWP ≥ 750) trifft 629,5 nicht; **zulässig** 2027 und 2035.
- **Wärmepumpen:** Sole-Wasser ohne Code, nicht geprüft. Proben mit Code: Luft-Wasser-Monoblock 35 kW **R410A → Nr. 8 d**
  (über 12 bis 50 kW, GWP ≥ 150, 2027); Sole-Wasser **R290 → „nicht prüfbar“** (Geräteklasse).
- Kältebahn: Warntext am Knoten (`SchemaModellTests`); Bericht: Spalte, Stern, Fußnote de/en (`KaeltemaschineBerichtTests`).
- Referenzlauf: `protokoll.txt` ohne Kältemittelzeile (kein Referenzprojekt trägt ein Inbetriebnahmejahr).

## Gate

**Zusammenführung mit origin (`gate_rest_linux.sh` auf `b063ef47f`, Merge von `af75a10ea` mit `bd3f4a283`, ohne
Konflikt):** EPOS.UI.Tests 8753, KiKern.Tests 553, SpeicherEngine.Tests 397, SpeicherPlanung.Tests 27 (+1 übersprungen),
Dokumentationswachen 35, Windows-Schale 0 Fehler, Designer unverändert, SQL-Dialekt 0 Fundstellen, Werkzeugtests grün
(Formularkarte 124, Auslieferungsvorlage 62, Gebäudevergleich 24, Zapfprofil 39), BOM keine, Konfliktmarker keine,
Referenzlauf 29/29 PASS gegen R51 (10 036 768 Werte), CSV byte-gleich 943/943, `protokoll.txt` ohne Kältemittelzeile.
**EPOS.Kern.Tests voll: 13 551 grün, 7 übersprungen, 0 rot.**

**Zweite Zusammenführung (`eb4b773be`, origin `4e2942025`, ohne Konflikt):** Kern-Filter und Windows-Schale 0 Fehler,
Designer unverändert, Konfliktmarker keine; EPOS.UI.Tests 8812 grün; EPOS.Kern.Tests gefiltert (Kältemittel,
Warnkriterien, Bericht, Schemamodell, Kältemaschine, Gesetz, Vorlage, Katalog, Ressourcen, Dokumentations- und
Ordnungswachen) 2637 grün; SQL-Dialekt 0 Fundstellen. Basis unverändert.

## Offen

- **E2‑c:** Status der Kältemittelzeilen (`VORLAEUFIG` → gesichert, wo belegt) und Gruppe von R1233zd(E) von `HFO` auf
  `HFCKW` über Saat, Katalogpaket und Testdatenbank, ohne Schemaschritt.
- **E3:** Dialoge, Projektdaten (Inbetriebnahmejahr), Kachel, Verwaltung, KI‑Sichten (Umsetzungskonzept 3.3).
- **E4:** Wiki, Logbuch, Konzeptnachzug, Konzepte „wie gebaut“ (Umsetzungskonzept 3.4).
