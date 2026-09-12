# B5 — Dialog „BHKW-Wirtschaftlichkeit" (Umsetzungsprotokoll)

Etappe B5 des Konzepts `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` (§ 6.1, Leitentscheidung
BW9: „Ein eigener Dialog *BHKW-Wirtschaftlichkeit* … Die beiden BHKW-Gruppen verlassen
`Form_WirtschaftlichkeitParameter`"). Stand 03.09.2026, Branch `lokal_dirk`, Basis `c781ee5`.
Grundlage der Umsetzung ist die Feldkarte B5 (Mockup-Vorlage) mit den Anwenderentscheiden vom
03.09.2026. **Kein neuer Schemaschritt** — `ZIEL_VERSION` bleibt 61, neue Schritte weiter ab 62.

## 1. Auftrag und feste Entscheide

| # | Entscheid | Umsetzung |
|---|---|---|
| K1 | KEIN Feld „Deckung je Modul" | umgesetzt; Negativbeleg gemessen (0 Treffer) |
| K2 | Hilfsenergie-Basis klar benennen, keine vierte Spalte | Beschriftung „Hilfsenergieanteil [% des Endenergiebedarfs]" + eigene Erläuterungszeile in Gruppe 1b und Gruppe 5 |
| K3 = a | Modusfeld § 9 Abs. 1 Nr. 3 anzeigen, aber ausgegraut, Vermerk „ab B6", KEINE Persistenz | umgesetzt; ComboBox `Enabled = false`, Vorgabe „Ausweis", Vermerkzeile darunter; `ProjektwerteSpeichern` schreibt den Wert ausdrücklich nicht |
| K4 | kleiner Leser CarrierId→Name für die Spalte „Brennstoff" | in `KwkgAnlagenCtrl` (dort liegt schon der Anlagen-Leseweg des Dialogs), mit `DbParam`; Rangfolge Träger vor Gerät wie `WirtschaftlichkeitCtrl.BrennstoffId` |
| K5 | Jahresnutzungsgrad als PROJEKTfeld | Gruppe 3, Feld 3.3 |
| K6 | Anteilsfeld nur bei BHKW; Kessel-Hinweis; WP später | `AnteilPflegbar(idType)` / `AnteilHinweis(idType)` + `HilfsenergieSichtbarkeit()`; Kessel-Hinweis in Gruppe 5 nur bei Kessel in der Gruppe |
| K7 | `KwkgAnlagenCtrl.Speichere` von 8 auf 11 Spalten | Überladung `Speichere(g, mitSteuerangaben)`; die alte Signatur bleibt bitgenau bei acht Spalten |
| K8 = c | `btnBhkwTarif` öffnet den neuen Dialog, alter Dialog nicht mehr angeboten | Handler umgeleitet, Knopftext im Code gesetzt; `Form_KwkgModule` bleibt als Klasse, ist aber über keine Oberfläche mehr erreichbar |
| K9 | nichts zu tun | — |
| K10 | NICHT in B5 (auf B6 verschoben) | nicht angefasst |
| K11 | neue Texte als `BHW_*` im GetString-Rückfallmuster, keine `.resx` | 95 Schlüssel, Tabelle in § 8 |

## 2. Der neue Dialog — `Views/Wirtschaftlichkeit/Form_BhkwWirtschaftlichkeit.cs`

**Code-only**, ohne `.Designer.cs` und ohne eigene `.resx` (Bauvorbild `ucBrennstoffBestandteile`
aus B2 und `Form_WirtschaftlichkeitParameter`, der ebenfalls vollständig im Code entsteht). Damit
entfällt die Designer-Pflegefalle vollständig.

**Aufbau** (Hausmaße § 5 der Feldkarte): Kopfband `Dock.Top` 48 px `#0F1F3D` mit weißem
Segoe-UI-12-pt-Titel und Infoknopf · Inhaltsfläche `Dock.Fill` mit `AutoScroll` · Fußleiste
`Dock.Bottom` mit `SpeichernLeiste` (nicht schließender Speichern-Knopf + Statuszeile GrayText)
und „Schließen" (110 × 30). `ClientSize` 914 breit, Höhe auf `WorkingArea − 90` gedeckelt
(gemessen 914 × 662). `AutoScaleMode = None` wie bei `Form_KwkgModule`/`UcWirtschaftlichkeit` —
das ist zugleich der Schutz gegen die AutoScroll-Verdeckung aus verzögertem Font-Skalieren.

### Feldkarten-Abgleich je Gruppe (headless gemessen)

| Gruppe | Feldkarte | gebaut | Abgleich |
|---|---|---|---|
| 1 Anlagen | Tabelle 6 Spalten (1.1–1.6) + 3 Warnzeilen | ListView mit **7** Spalten, 1 Warnlabel | **+1 Spalte „Projekt"** (siehe § 7, A-1); alle drei Warnzeilen gemessen |
| 1b Angaben der Anlage | 11 Felder (1.7–1.17) | Num 5 · Combo 4 · Datum 2 = **11** | vollständig, Reihenfolge wie Feldkarte |
| 2 KWK-Zuschlag | 11 Felder (2.1–2.11) + Herleitungslabel + Knopf 240×30 | Num 6 · Combo 2 · Datum 2 · Check 1 = **11**, Herleitungslabel, Knopf 240×30 | vollständig |
| 3 Energiesteuer | 3 Felder (3.1–3.3) + Herleitung + Kohärenzzeile | Num 1 · Combo 2 = **3**, Herleitung aus `SteuerHerkunft` | Kohärenzzeile in eigenem Block (A-2) |
| 4 Stromsteuer | 4 Felder (4.1–4.4) + Sprungknopf (4.5) | Combo 2 · Check 2 = **4**, **2** Sprungknöpfe | „BHKW-Tarif…" zusätzlich (A-3) |
| — Kohärenzprüfung | (in 3 und 4) | 1 Label, Firebrick | eigener Block (A-2) |
| 5 Hilfsstrom | Anteil (=1.17) · KEINE Deckung · Mengenkette · Doppelpflegewarnung | Basis-Erläuterung, Mengenkette, Doppelpflege, Kessel-Hinweis | K1 belegt, kein zweites Anteilsfeld |
| 6 Vorschau | Kopfstreifen `#1A3261` + 5 Zeilen | Kopfstreifen 882×28 `#1A3261`, Titel Segoe UI 9,75 bold weiß, 5 Zeilen + Stand | vollständig |

**Der Dialog rechnet nichts.** Sätze und Herleitung kommen aus `KwkgSatzRechner.Vorschlag`,
Vorschauzahlen und Satzherkunft aus dem gebuchten Ergebnis (`WirtschaftlichkeitCtrl.LadeErgebnisse`)
bzw. aus dem durchgereichten Lauf, die Mengenkette aus `KwkgModulNachweis` (B3b), die
Kohärenzzeilen aus `KohaerenzPruefung`. Die Grenzwerte der Warnzeilen liest er aus demselben
Gesetzeskatalog wie der Rechenweg (`KWKG_AUSSCHREIBUNG_GRENZE_KW`,
`STROMST_GRENZE_BEFREIUNG_9_1_3_KW`) mit denselben Rückfallwerten.

**Durchreichung statt Zweitrechnung:** Zwei Bestandteile des Laufs sind nicht persistiert — die
Kohärenzhinweise (B2-O4) und die KWKG-Modulnachweise mit der Mengenkette (E7/B3b). Der zweite
Konstruktor nimmt deshalb die Ergebnisliste des letzten Laufs entgegen; `UcWirtschaftlichkeit`
reicht `_ergebnisse` durch. Ohne Lauf fällt der Dialog auf den gebuchten Stand zurück und sagt das
in beiden Feldern ausdrücklich.

**Erstauswahl deterministisch:** `ErsteZeileWaehlen` ruft `Liste_Wechsel` notfalls selbst auf.
Ohne Fensterhandle feuert `ListView.SelectedIndexChanged` nicht — die Felder blieben sonst leer
und aktiv, ohne dass eine Zeile gewählt wäre. `GewaehlteZeile()` liest den Index notfalls aus dem
gemerkten Zustand der Einträge. Der Befund stammt aus der Headless-Probe; er hätte im Betrieb
nicht gestört, macht den Zustand aber unabhängig von der Ereignisreihenfolge.

## 3. K7 — der Schreibweg der elf Spalten

`Allgemein/Wirtschaftlichkeit/KwkgAnlagenCtrl.cs`:

- **`Speichere(KwkgAnlagenAngabe g)`** — unverändert **acht** Spalten (E6, Schritt 22); die drei
  B3a-Spalten bleiben unangetastet. Ruft intern `Speichere(g, false)`.
- **`Speichere(KwkgAnlagenAngabe g, bool mitSteuerangaben)`** — mit `true` **elf** Spalten:
  zusätzlich `Energiesteuer_Wahl` (TEXT 20), `Aufteilung_Methode` (TEXT 30), `Hilfsenergie_Anteil`
  (DOUBLE), Breiten wie in `SchemaKatalog.Schritt61_SteuerJeAnlage`. Spaltennamen ausschließlich
  über `SchemaKatalog`, Parameter ausschließlich `DbParam`.
- **Warum ein Schalter:** `Form_KwkgModule` (E6) kennt die drei Felder nicht und hat sie auch nicht
  im Bildschirmzustand — ohne Schalter setzte jedes Speichern dort gepflegte Werte still auf NULL.
- **Leseweg** ergänzt: Fähigkeitstreppe „mit B3a / ohne" nach dem Muster
  `WirtschaftlichkeitCtrl.LiesAnlagen` (erkannt an der Spaltenliste, nicht an einer Ausnahme),
  dazu `ID_Carrier` und der K4-Brennstoffname.
- **K4-Leser:** drei Nachschlagelisten je Controllerinstanz (`Tab_Brennstoff_Stamm` mit Kategorie,
  `energy_carrier`), Rangfolge **Träger vor Gerät** — wortgleich zu
  `WirtschaftlichkeitCtrl.BrennstoffId`. Liefert Anzeigename und das Kennzeichen „Öl"
  (Kategorie 2) für die Heizöl-Warnzeile. Reine Anzeige, wird nie geschrieben.

## 4. BW9 — der Auszug aus `Form_WirtschaftlichkeitParameter`

Der Dialog ist Code-only; einen Designer gibt es dort nicht. Die beiden BHKW-Gruppen werden
**unverändert weiter aufgebaut** und unmittelbar danach ausgeblendet (`Bw9Ausblenden`), das
Layout-`y` wird auf den Stand davor zurückgesetzt, und an ihre Stelle tritt eine Gruppe
„BHKW — KWKG, Energie- und Stromsteuer" mit Verweiszeile und Sprungknopf (`Bw9Verweis`).

**Warum ausblenden und nicht weglassen:** `btnOk_Click` liest dieselben Steuerelemente weiter aus,
und sie tragen unverändert die geladenen Datenbankwerte — der Dialog schreibt die BHKW-Angaben
damit **wertgleich** zurück, genau wie er es für jede andere ausgeblendete Gruppe tut
(Vorgabe 12.08.2026: „Werte ausgeblendeter Gruppen bleiben beim Speichern unverändert erhalten").
Ein Weglassen hätte den Speicherweg auf Null-Prüfungen umgebaut und ein Verhalten geändert, das
zu B5 nicht gehört.

Der Modul-Knopf „⚙ Werte je BHKW-Modul …" ist Teil der ausgezogenen Gruppe und damit ebenfalls
unsichtbar — `Form_KwkgModule` ist über die Oberfläche nicht mehr erreichbar. Die Klasse bleibt
(Auftrag), der Handler `btnModule_Click` ebenfalls.

## 5. K8 = c — die Andockung

`UcWirtschaftlichkeit.BauePhotovoltaikKnopf()`: Der vorhandene Knopf `btnBhkwTarif` (x = 182,
y = 494, 110 × 30) trägt jetzt den Text „BHKW-Wirtschaftlichkeit…" (Schlüssel `BHW_KNOPF` mit
deutschem Rückfall, im Code gesetzt — Designer und `.resx` unberührt) und ruft
`btnBhkwWirtschaftlichkeit_Click`. Sichtbarkeit unverändert an `_erzeuger.Bhkw`.

**Kein Funktionsverlust:** Die BHKW-Sicht der Tarifstruktur, die dieser Knopf bis B5 öffnete, ist
über den Sprungknopf „BHKW-Tarif…" in der Stromsteuergruppe des neuen Dialogs erreichbar (neben
dem in der Feldkarte vorgesehenen „Strombezug…").

## 6. Nachweise

Harness `..\dev\b5\` (gitignored, Vorbild `dev\fx5`): ProjectReference, DLL-Tausch für den
Vorher/Nachher-Vergleich, Schutzriegel gegen die Produktivdatenbank, je Lauf eine frische Kopie
im Scratchpad. Vorher-Stand = `c781ee5` in einem Wegwerf-Worktree gebaut (39 Warnungen, 0 Fehler —
dieselbe Zahl wie der Nachher-Stand). **Die Produktivdatenbank wurde nur gelesen; Zeitstempel
unverändert (03.09.2026 02:03:32, 77.000.704 Bytes).**

### 6.1 K7-Schreibweg (Projekt 1030, Anlage 14920)

| Probe | Ergebnis |
|---|---|
| Leseweg + K4 | 2 Anlagen: `BHKW EW M 50 S [K] Erdgas` (Pel 50, Carrier 63, Brennstoff **Erdgas E**, Heizöl false) · `EC-POWER XRGI 9` (Pel 9, Erdgas E) |
| Ausgangsstand | alle elf Spalten NULL |
| `Speichere(g, true)` | `2026-03-17 \| 2027-05-04 \| MODERNISIERT \| NR2_KUNDENANLAGE \| 5.57 \| 3.25 \| 30000 \| 3500 \| PARAGRAF_53A \| ENERGETISCH \| 3.5` — **alle elf Spalten kommen an** |
| Idempotenz | zweites Speichern: Rücklesung **bitgleich**, keine Drift |
| **Bestandsaufrufer** `Speichere(g)` | Objekt trug `KEINE` / `VOLLER_BRENNSTOFF` / `99`; in der Datenbank steht danach `NEUANLAGE \| … \| 4.44 \| … \| **PARAGRAF_53A \| ENERGETISCH \| 3.5**` — die drei B3a-Spalten **überleben unangetastet** |
| Rückweg | leer/leer/0 geschrieben → `NULL \| NULL \| 0` |
| Leseweg zurück | `Wahl='' Methode='' Anteil=0 Stichtag=2026-03-17` |

### 6.2 Rechenwerk unberührt — A/B über alle Projekte × Szenarien

Vorher (`c781ee5`) gegen Nachher, dieselbe frische Kopie, Schritt `ab`: **16 Projekte** mit
Simulationslauf, je drei Szenarien (Erwartet/Best/Worst) — 48 `BETRIEB`-, 48 `TOPF`-, 66 `KENN`-,
6 `MOD`- und 8 `SENS`-Zeilen, dazu die Kohärenztexte.

```
diff vorher/nachher (183 Zeilen je Seite): LEER
```

Regressionsanker (Schritt `anker`, beide Stände zeichengleich):

```
Betrieb 1024 = 99,0000                     (Soll 99,00)
Invest  1018 = 45.312,5000                 (Soll 45.312,50)
Invest  1024 = 12.001,0000                 (Soll 12.001,00)
Invest  1042 = 13.000,0000                 (Soll 13.000,00)
KW 1024      = -2219863.761540025          (Soll -2.219.863,7615)
KW 1030      = -21875243.675724894         (Soll -21.875.243,6757)
```

### 6.3 Formular headless (Schritt `form` / `formlauf` / `warn`)

- **Instanziierung ohne Ausnahme**, Titel „BHKW-Wirtschaftlichkeit", `ClientSize 914 × 662`,
  `FixedDialog`, 111 Steuerelemente.
- **Feldbestand je Gruppe** wie in der Tabelle § 2 gemessen (11 / 11 / 3 / 4 Eingabefelder).
- **K3**: ComboBox „Modus § 9 Abs. 1 Nr. 3:" vorhanden, `Enabled = False`, Auswahl
  „Ausweis (nicht im Kapitalwert)", 2 Einträge; Vermerk gemessen:
  `ab B6 — bis dahin gilt fest „Ausweis" (nicht im Kapitalwert).`
- **K1**: Treffer auf „Deckung" in Text oder Name: **0**.
- **K6**: `AnteilPflegbar(11) = True`, `(10) = False`, `(1) = False`;
  `AnteilHinweis(11) = False`, `(10) = True`, `(1) = False`; Feld im Dialog `sichtbar = True`
  (Messung über `Control.GetState(States)` — `Control.Visible` ist an einem nie angezeigten
  Formular für jedes Kind false).
- **Herleitung Gruppe 2** (Bestand `KwkgSatzRechner`), 50-kW-Anlage:
  `Einspeisung 16,00 ct/kWh — 50,0 kW und damit bis 50 kW, neue Anlage → 16,00 ct/kWh
  (§ 7 Abs. 3a KWKG 2025, Stand 2027) …` / `Eigenstrom 8,00 ct/kWh — …`;
  bei 600 kW: `Einspeisung 4,98 ct/kWh — 600,0 kW nach Leistungsanteilen: 50,0 kW × 8,00 +
  50,0 kW × 6,00 + 150,0 kW × 5,00 + 350,0 kW × 4,40 → Mischsatz 4,98 ct/kWh`.
- **Mengenkette Gruppe 5** (durchgereichter Lauf):
  `Stromerzeugung brutto 373,780 MWh/a − Hilfsstrom 0,000 MWh/a = Nettostromerzeugung 373,780 MWh/a`
  / `davon Eigenverbrauch 373,780 MWh/a, Einspeisung 0,000 MWh/a`.
- **Vorschau Gruppe 6**: `KWK-Zuschlag p. a.: 7.316 €` (unpräpariert) bzw. im präparierten Fall
  `Energiesteuer p. a.: 5.119 € · Stromsteuer p. a.: 86.906 €`.
- **Präparierter Fall** (`warn`, nur auf der Kopie: Pel 600 / 2500 kW, Heizölträger, IBN 2025,
  Anteil 3,5 % + aktive Kostenposition, Projektwahl § 53a / prod. Gewerbe / Hocheffizienz):
  - Warnzeile Gruppe 1 (Firebrick): `Ausschreibung nach § 8a KWKG: BHKW EW M 50 S [K] Erdgas,
    EC-POWER XRGI 9 über 500 kW. · Stromsteuerbefreiung § 9 Abs. 1 Nr. 3 entfällt:
    EC-POWER XRGI 9 über 2.000 kW. · Heizöl-Ausschluss ab Inbetriebnahme 2025: EC-POWER XRGI 9.`
  - Kohärenzblock (Firebrick): Energiesteuer **5.118,75 €/a** („kein Anteil erfasst") und
    Stromsteuer § 9b **86.905,60 €/a** („Schalter Aufschläge ist aus") — zeichengleich zu den
    Zeilen, die derselbe Lauf im Reiter zeigt.
  - Gruppe 5 (Firebrick): die B3b-Doppelpflegewarnung, **genau einmal** (sie kommt aus zwei
    Quellen und wird über den Text entdoppelt).
  - K4 in der Tabelle: `Heizöl Bio 10` für die umgestellte Anlage.

### 6.4 BW9 — Sichtbarkeit vorher/nachher (`Form_WirtschaftlichkeitParameter`, Projekt 1030)

| Größe | vorher (`c781ee5`) | nachher |
|---|---|---|
| Steuerelemente | 99, davon **99 sichtbar / 0 unsichtbar** | 102, davon **68 sichtbar / 34 unsichtbar** |
| „BHKW — KWKG 2025" (y = 285) | sichtbar | **AUSGEBLENDET** |
| „BHKW — Energie- und Stromsteuer" (y = 676) | sichtbar | **AUSGEBLENDET** |
| Ersatzgruppe | — | „BHKW — KWKG, Energie- und Stromsteuer" bei y = 285, sichtbar |
| „Brennstoff — BEHG …" | y = 881 | y = **402** |
| „Bilanzierung …" | y = 1070 | y = **591** |
| Knopf „⚙ Werte je BHKW-Modul …" | sichtbar | **AUSGEBLENDET** |
| Knopf „⚙ BHKW-Wirtschaftlichkeit (KWKG, Steuern, Module)…" | — | sichtbar |
| Verweiszeile | — | „Diese Angaben stehen seit Etappe B5 im eigenen Dialog *BHKW-Wirtschaftlichkeit* — dort zusammen mit den Werten je BHKW-Modul, den Herleitungen und der Vorschau." |

Die 34 ausgeblendeten Steuerelemente sind exakt die beiden Gruppen (23 + 11). Der Dialog wird um
479 px kürzer.

### 6.5 K8 — Andockung

```
Feld btnBhkwTarif: Text='BHKW-Wirtschaftlichkeit…', sichtbar, x=182 y=494
Handler: btnBhkwWirtschaftlichkeit_Click(Object, EventArgs)
Form_BhkwWirtschaftlichkeit: ctor(Int32) und ctor(Int32, List<WirtschaftlichkeitErgebnis>)
KwkgAnlagenCtrl: Speichere(KwkgAnlagenAngabe) und Speichere(KwkgAnlagenAngabe, Boolean)
```

**Aufrufersuche `Form_KwkgModule` repoweit** (ohne `dev/`, `obj/` und die eigene Datei):

| Fundstelle | Art |
|---|---|
| `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs:706` | einziger echter Aufruf — in `btnModule_Click`, dessen Knopf seit BW9 unsichtbar ist |
| `Allgemein/Hilfe/help_mapping.txt:266` | Hilfezuordnung des Infoknopfs (bleibt) |
| `Allgemein/KI/HilfeKontext.cs:245` | Hilfekontext (bleibt) |
| `Allgemein/Wirtschaftlichkeit/KwkgAnlagenCtrl.cs:111/238`, `Form_BhkwWirtschaftlichkeit.cs:14/165/179/863` | Kommentare |

Über die Oberfläche ist der Dialog damit nicht mehr erreichbar; die Klasse bleibt bestehen.

### 6.6 Build und Sweep

| Probe | Ergebnis |
|---|---|
| `dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64 -t:Rebuild` | **Exit 0, 39 Warnungen, 0 Fehler** — dieselbe Zahl wie im Vorher-Stand |
| Warnungen aus `Form_BhkwWirtschaftlichkeit.cs` | **0** (keine neue WFO1000; der Dialog hat keine serialisierten Eigenschaften) |
| `git grep "^<<<<<<<" -- "*.cs"` | 0 Treffer |
| Produktivdatenbank | nur gelesen, Zeitstempel unverändert |

## 7. Abweichungen von Feldkarte und Bauplan (mit Begründung)

| # | Abweichung | Begründung |
|---|---|---|
| **A-1** | Die Anlagentabelle hat **7** statt 6 Spalten — „Projekt" kommt vor „Anlage" | Der Dialog führt die ganze Vergleichsgruppe (Stamm **und** Varianten, `KwkgAnlagenCtrl.LadeGruppe`). Varianten tragen regelmäßig gleichnamige Anlagen; ohne die Projektspalte wären die Zeilen nicht unterscheidbar. `Form_KwkgModule` löste dasselbe Problem im Listentext („Projekt · Anlage (kW)") |
| **A-2** | Die steuerlichen Kohärenzzeilen stehen in **einem** Block „Kohärenzprüfung (Energie- und Stromsteuer)" statt getrennt in Gruppe 3 und 4 | `KohaerenzHinweis` trägt Schwere, Text und Betrag, aber **kein Artmerkmal**. Eine Aufteilung ginge nur über den Anzeigetext (sprachabhängiges Textraten) oder über eine Änderung an `KohaerenzPruefung` — die gehört zum gesperrten Rechenwerk und nicht zu B5. Der gemeinsame Block zeigt dieselben Zeilen wie der Reiter, in derselben Reihenfolge, aus derselben Quelle. **Designfrage für B6** (dort entsteht ohnehin die Herleitungstafel): Soll `KohaerenzHinweis` ein ASCII-Artmerkmal bekommen? |
| **A-3** | Die Stromsteuergruppe hat **zwei** Sprungknöpfe: „Strombezug…" (Feldkarte 4.5) und zusätzlich „BHKW-Tarif…" | K8 = c leitet den Knopf um, der bis B5 die **BHKW-Sicht der Tarifstruktur** öffnete (`Form_Tarifstruktur`, `TarifSicht.Bhkw`) — nicht die KWKG-Modulpflege. Ohne diesen Sprung wäre die Differenzmethoden-Sicht über keine Oberfläche mehr erreichbar. **Designfrage an den Anwender:** Ist der Sprung an dieser Stelle richtig, oder soll die BHKW-Tarifsicht anderswo andocken? |
| **A-4** | Die Vorschau heißt „Vorschau — zuletzt gebuchter Lauf" und ist **nicht live** | Die Feldkarte nennt „Live, ein Rechenweg". Ein Live-Wert wäre eine Rechnung im Dialog — genau die Zweitrechnung, die BW8 ausschließt. Der Dialog zeigt die gebuchten Jahr-1-Werte des einen Rechenwegs und sagt, dass nach dem Speichern neu zu rechnen ist. Live-Vorschau bliebe möglich, wenn B6 den Rechenweg headless anstoßbar macht |
| **A-5** | Der Erläuterungsabsatz am Fuß von `Form_WirtschaftlichkeitParameter` nennt weiterhin KWKG- und Steuerregeln | Er ist ein einziger zusammengesetzter Text; ihn zu zerlegen wäre eine Textänderung ohne Auftrag. **Offener Punkt B5-O2** |

## 8. Neue Textschlüssel (`BHW_*`, 95 Stück)

Alle über das Rückfallmuster `T("BHW_…", "deutscher Text")`; **keine `.resx` angefasst** — der
Sammelnachtrag trägt sie gebündelt nach (zusammen mit den offenen B3a-/B3b-/FX-Schlüsseln).

| Schlüssel | de | en (Vorschlag) |
|---|---|---|
| `BHW_TITEL` | BHKW-Wirtschaftlichkeit | CHP economics |
| `BHW_KNOPF` | BHKW-Wirtschaftlichkeit… | CHP economics… |
| `BHW_MELD_GESPEICHERT` | BHKW-Wirtschaftlichkeit gespeichert — bitte neu berechnen. | CHP economics saved — please recalculate. |
| `BHW_G1` | Anlagen | Units |
| `BHW_G1B` | Angaben der gewählten Anlage — leer bzw. 0 = Projektvorgabe | Settings of the selected unit — empty or 0 = project default |
| `BHW_G2` | KWK-Zuschlag (Projektvorgabe) | CHP surcharge (project default) |
| `BHW_G3` | Energiesteuer (Projektvorgabe) | Energy tax (project default) |
| `BHW_G4` | Stromsteuer (Projektvorgabe) | Electricity tax (project default) |
| `BHW_G5` | Hilfsstrom | Auxiliary power |
| `BHW_G6` | Vorschau — zuletzt gebuchter Lauf | Preview — last recorded run |
| `BHW_G_KOHAERENZ` | Kohärenzprüfung (Energie- und Stromsteuer) | Consistency check (energy and electricity tax) |
| `BHW_SP_PROJEKT` | Projekt | Project |
| `BHW_SP_ANLAGE` | Anlage | Unit |
| `BHW_SP_PEL` | P_el [kW] | P_el [kW] |
| `BHW_SP_BRENNSTOFF` | Brennstoff | Fuel |
| `BHW_SP_STICHTAG` | Stichtag | Cut-off date |
| `BHW_SP_IBN` | Inbetriebnahme | Commissioning |
| `BHW_SP_ANLAGENART` | Anlagenart | Unit category |
| `BHW_A_STICHTAG` | Stichtag (Bestellung/Genehmigung): | Cut-off date (order/permit): |
| `BHW_A_IBN` | Inbetriebnahme: | Commissioning: |
| `BHW_A_ANLAGENART` | Anlagenart: | Unit category: |
| `BHW_A_EIGENFALL` | Eigenstrom nach § 6 Abs. 3: | Self-consumption under sec. 6 (3): |
| `BHW_A_SATZ_EINSP` | Satz Einspeisung [ct/kWh] (0 = Projektsatz): | Feed-in rate [ct/kWh] (0 = project rate): |
| `BHW_A_SATZ_EIGEN` | Satz Eigenstrom [ct/kWh] (0 = Projektsatz): | Self-consumption rate [ct/kWh] (0 = project rate): |
| `BHW_A_KONTINGENT` | Vbh-Kontingent [h] (0 = Projektwert): | Full-load hour quota [h] (0 = project value): |
| `BHW_A_DECKEL` | Vbh-Jahresdeckel [h/a] (0 = Staffel): | Annual full-load hour cap [h/a] (0 = schedule): |
| `BHW_A_ENERGIESTEUER` | Energiesteuerentlastung (Anlage): | Energy tax relief (unit): |
| `BHW_A_AUFTEILUNG` | Brennstoff auf Strom/Wärme (Anlage): | Fuel split power/heat (unit): |
| `BHW_A_HILFSANTEIL` | Hilfsenergieanteil [% des Endenergiebedarfs] (0 = keine): | Auxiliary power share [% of final energy demand] (0 = none): |
| `BHW_A_HILFS_BASIS` | Vorschlag BHKW 2–4 %. Bemessen wird am Endenergiebedarf (Brennstoff) dieser Anlage — nicht an den Kosten. | Suggested 2–4 % for CHP. Measured against this unit's final energy demand (fuel) — not against cost. |
| `BHW_P_BONUS_EIGEN` | Bonus Eigenstrom [ct/kWh] (0 = aus): | Self-consumption bonus [ct/kWh] (0 = off): |
| `BHW_P_BONUS_EINSP` | Bonus Einspeisung [ct/kWh]: | Feed-in bonus [ct/kWh]: |
| `BHW_P_DECKEL` | Vbh-Deckel-Override [h/a]: | Full-load hour cap override [h/a]: |
| `BHW_P_KONTINGENT` | Vbh-Kontingent gesamt [h] (0 = automatisch): | Total full-load hour quota [h] (0 = automatic): |
| `BHW_P_ABSCHLAG` | Abschlag Negativstunden [%]: | Negative-price hour deduction [%]: |
| `BHW_P_TATBESTAND` | Eigenstrom-Tatbestand (§ 6 Abs. 3): | Self-consumption case (sec. 6 (3)): |
| `BHW_P_ANLAGENART` | Anlagenart (§ 8): | Unit category (sec. 8): |
| `BHW_P_KOSTENANTEIL` | Anteil Neuherstellungskosten [%]: | Share of new-build cost [%]: |
| `BHW_P_PAUSCHAL` | Pauschale § 9 KWKG (nur bis 2 kWel, einmalig) | Lump sum under sec. 9 KWKG (up to 2 kWel, one-off) |
| `BHW_P_STICHTAG` | Stichtag, Vorgabe je Anlage: | Cut-off date, default per unit: |
| `BHW_P_IBN` | Inbetriebnahme, Vorgabe je Anlage: | Commissioning, default per unit: |
| `BHW_E_WAHL` | Energiesteuerentlastung: | Energy tax relief: |
| `BHW_E_AUFTEILUNG` | Brennstoff auf Strom/Wärme: | Fuel split power/heat: |
| `BHW_E_NUTZUNGSGRAD` | Jahresnutzungsgrad [%] (0 = nicht erfasst): | Annual efficiency [%] (0 = not recorded): |
| `BHW_E_OHNE_HERKUNFT` | Keine Gutschrift im zuletzt gebuchten Lauf — es wurde kein Satz verwendet. | No credit in the last recorded run — no rate was applied. |
| `BHW_S_UNTERNEHMENSART` | Unternehmensart: | Type of business: |
| `BHW_S_RAEUMLICH` | Räumlicher Zusammenhang (4,5 km) gegeben | Spatial connection (4.5 km) given |
| `BHW_S_HOCHEFFIZIENZ` | Hocheffizienz nachgewiesen | High efficiency demonstrated |
| `BHW_S_MODUS` | Modus § 9 Abs. 1 Nr. 3: | Mode under sec. 9 (1) no. 3: |
| `BHW_S_MODUS_B6` | ab B6 — bis dahin gilt fest „Ausweis" (nicht im Kapitalwert). | from stage B6 — until then fixed to "disclosure" (not in the net present value). |
| `BHW_H_BASIS` | Der Anteil wird je Anlage oben gepflegt und am ENDENERGIEBEDARF (Brennstoff) der Anlage bemessen — nicht an den Kosten. Die Menge mindert die zuschlagsfähige Nettostromerzeugung. | The share is maintained per unit above and measured against the unit's FINAL ENERGY DEMAND (fuel) — not against cost. The quantity reduces the eligible net power generation. |
| `BHW_H_KETTE1` | Stromerzeugung brutto {0} MWh/a − Hilfsstrom {1} MWh/a = Nettostromerzeugung {2} MWh/a | Gross power generation {0} MWh/a − auxiliary power {1} MWh/a = net power generation {2} MWh/a |
| `BHW_H_KETTE2` | davon Eigenverbrauch {0} MWh/a, Einspeisung {1} MWh/a | of which self-consumption {0} MWh/a, feed-in {1} MWh/a |
| `BHW_H_OHNE_LAUF` | Mengenkette: noch kein gebuchtes Ergebnis — bitte in der Wirtschaftlichkeit „Berechnen". | Quantity chain: no recorded result yet — please use "Calculate" in the economics view. |
| `BHW_H_KESSEL` | Heizkessel der Gruppe: Der Hilfsenergieanteil wird für Kessel mitgerechnet, aber nicht hier gepflegt. | Boilers in the group: the auxiliary power share is included for boilers but not maintained here. |
| `BHW_K_LEER` | Keine Auffälligkeit im zuletzt gebuchten Lauf. | No findings in the last recorded run. |
| `BHW_V_ZUSCHLAG` | KWK-Zuschlag p. a. | CHP surcharge p.a. |
| `BHW_V_ENERGIESTEUER` | Energiesteuer p. a. | Energy tax p.a. |
| `BHW_V_STROMSTEUER` | Stromsteuer p. a. | Electricity tax p.a. |
| `BHW_V_EINSPEISUNG` | Einspeiseerlös KWK p. a. | CHP feed-in revenue p.a. |
| `BHW_V_VERMIEDEN` | Vermiedene Stromkosten p. a. (Ausweis) | Avoided electricity cost p.a. (disclosure) |
| `BHW_V_STAND` | Stand: {0} — nach dem Speichern neu berechnen. | As of {0} — recalculate after saving. |
| `BHW_V_OHNE_LAUF` | Noch kein gebuchtes Ergebnis — die Vorschau erscheint nach „Berechnen" in der Wirtschaftlichkeit. | No recorded result yet — the preview appears after "Calculate" in the economics view. |
| `BHW_HERLEITUNG_EINSP` | Einspeisung {0} ct/kWh — {1} | Feed-in {0} ct/kWh — {1} |
| `BHW_HERLEITUNG_EIGEN` | Eigenstrom {0} ct/kWh — {1} | Self-consumption {0} ct/kWh — {1} |
| `BHW_W_AUSSCHREIBUNG` | Ausschreibung nach § 8a KWKG: {0} über {1} kW. | Tendering under sec. 8a KWKG: {0} above {1} kW. |
| `BHW_W_STROMSTEUER` | Stromsteuerbefreiung § 9 Abs. 1 Nr. 3 entfällt: {0} über {1} kW. | Electricity tax exemption under sec. 9 (1) no. 3 does not apply: {0} above {1} kW. |
| `BHW_W_HEIZOEL` | Heizöl-Ausschluss ab Inbetriebnahme 2025: {0}. | Fuel oil exclusion from commissioning 2025: {0}. |
| `BHW_W_OFFEN` | (nicht angegeben) | (not specified) |
| `BHW_W_PROJEKTWERT` | (Projektwert) | (project value) |
| `BHW_W_ART_LEER` | (nicht erfasst — gilt als Neuanlage) | (not recorded — treated as a new unit) |
| `BHW_W_ART_NEU` | neue Anlage (§ 8 Abs. 1) | new unit (sec. 8 (1)) |
| `BHW_W_ART_MOD` | modernisiert (§ 8 Abs. 2) | modernised (sec. 8 (2)) |
| `BHW_W_ART_NACH` | nachgerüstet (§ 8 Abs. 3) | retrofitted (sec. 8 (3)) |
| `BHW_W_FALL_KEINER` | kein Tatbestand (kein Eigenstromzuschlag) | no qualifying case (no self-consumption surcharge) |
| `BHW_W_FALL_NR1` | Nr. 1 — Anlage bis 100 kW | no. 1 — unit up to 100 kW |
| `BHW_W_FALL_NR2` | Nr. 2 — Kundenanlage / geschl. Netz | no. 2 — customer installation / closed network |
| `BHW_W_FALL_NR3` | Nr. 3 — stromkostenintensiv | no. 3 — electricity-intensive |
| `BHW_W_ES_KEINE` | keine | none |
| `BHW_W_ES_53` | § 53 EnergieStG (Formular 1131) | sec. 53 EnergieStG (form 1131) |
| `BHW_W_ES_53A` | § 53a Abs. 5 EnergieStG (1135) | sec. 53a (5) EnergieStG (1135) |
| `BHW_W_ES_54` | § 54 EnergieStG (Formular 1450) | sec. 54 EnergieStG (form 1450) |
| `BHW_W_AUF_VOLL` | voller BHKW-Brennstoff (§ 53 Abs. 2) | full CHP fuel (sec. 53 (2)) |
| `BHW_W_AUF_ENERGETISCH` | energetisch (konservativ) | energy-based (conservative) |
| `BHW_W_UA_KEIN` | kein produzierendes Gewerbe | not a manufacturing business |
| `BHW_W_UA_PROD` | produzierendes Gewerbe | manufacturing business |
| `BHW_W_UA_LAND` | Land- und Forstwirtschaft | agriculture and forestry |
| `BHW_W_MODUS_AUSWEIS` | Ausweis (nicht im Kapitalwert) | disclosure (not in the net present value) |
| `BHW_W_MODUS_ERLOES` | Erlös (im Kapitalwert) | revenue (in the net present value) |
| `BHW_BTN_VORSCHLAG` | Vorschlag in die Satzfelder übernehmen | Apply suggestion to the rate fields |
| `BHW_BTN_STROMBEZUG` | Strombezug… | Electricity purchase… |
| `BHW_BTN_BHKW_TARIF` | BHKW-Tarif… | CHP tariff… |
| `BHW_BTN_SCHLIESSEN` | Schließen | Close |
| `BHW_MSG_FEHLER` | {0} Angabe(n) konnten nicht gespeichert werden. | {0} entr(y/ies) could not be saved. |
| `BHW_MSG_FEHLER_TITEL` | Fehler | Error |

## 9. Offene Punkte

| Nr. | Punkt |
|---|---|
| **B5-O1** | **Sichtabnahme des Anwenders steht aus** — Maße, Gruppenreihenfolge und die Höhendeckelung (914 × 662 auf dem Prüfrechner, AutoScroll darunter) sind nur headless belegt |
| B5-O2 | Der Erläuterungsabsatz am Fuß von `Form_WirtschaftlichkeitParameter` nennt weiterhin KWKG- und Steuerregeln, deren Felder umgezogen sind (A-5) |
| B5-O3 | `KohaerenzHinweis` ohne Artmerkmal — die Trennung der Kohärenzzeilen auf Energie- und Stromsteuergruppe braucht ein ASCII-Merkmal in `KohaerenzPruefung` (Designfrage, Kandidat für B6) |
| B5-O4 | Die Vorschau ist der gebuchte Stand, nicht live (A-4) |
| B5-O5 | Die 63 unlokalisierten Bestandsliterale in `Views\Wirtschaftlichkeit` bleiben Altlast B6; der `resx`-Sammelnachtrag für die 95 `BHW_`-Schlüssel steht aus |
| B5-O6 | K10 (Altkatalog `PROZENT_BRENNSTOFFKOSTEN` gegen Seed `PROZENT_ENDENERGIEKOSTEN`) bewusst auf B6 verschoben — wäre ein Migrationsschritt und gehört nicht ins Dialogpaket |
| B5-O7 | `ucErtragBonus` (KD5) als zweiter Einstieg in den Dialog (Konzept § 6.1, „zwei Wege auf dasselbe Formular") ist nicht Teil von B5 |

## 10. Geänderte und neue Dateien

```
Views/Wirtschaftlichkeit/Form_BhkwWirtschaftlichkeit.cs   NEU — der Dialog (Code-only, 1.369 Zeilen)
Allgemein/Wirtschaftlichkeit/KwkgAnlagenCtrl.cs           K7 (11 Spalten, Überladung), B3a-Leseweg, K4-Leser
Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs          K8 = c: Knopftext + Handler, Ergebnisdurchreichung
Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs  BW9: Bw9Ausblenden + Bw9Verweis + Sprungknopf
Allgemein/Reporting/B5_BhkwWirtschaftlichkeit_Dialog_Protokoll.md   dieses Protokoll
```

Keine `.Designer.cs`, keine `.resx`, kein Schemaschritt, kein Eingriff in
`Allgemein/Wirtschaftlichkeit/*` außer `KwkgAnlagenCtrl.cs` (dort liegt `KwkgAnlagenCtrl` —
der Auftrag nannte den Pfad `Controller/`, die Klasse steht seit E6 unter
`Allgemein/Wirtschaftlichkeit/`). Harness (gitignored): `..\dev\b5\`.
