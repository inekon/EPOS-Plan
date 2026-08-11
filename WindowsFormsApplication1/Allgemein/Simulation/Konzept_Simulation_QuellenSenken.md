# Konzept: Quellen und Senken von Wärmeerzeugern, Pufferspeicher und Brauchwasser-Stundenbilanz

**Fassung 1** · Stand 11.08.2026 · Status: abgestimmt (Grundsatzentscheidungen E1–E6), Entwurf zur Umsetzung
Bezug: `Konzept_TWW-Zapfprofile_WP-Plan_1.md` (Ausbaupfad Brauchwasser),
`Konzept_Variantenbericht.md`, `Konzept_Wirtschaftlichkeit.md`
Codebasis: Analyse vom 11.08.2026 (`Allgemein/Simulation/*`, `Form_Simulation_Config`,
`WaermequelleClass`, `Z_ProjektPufferSp`, Brauchwasser-Modul)

> **Einordnung.** Dieses Konzept ist Bestandteil von EPOS-Plan und beschreibt die
> Erweiterung der Simulationskonfiguration um eine explizite **Quellen-/Senken-Zuordnung
> je Wärmeerzeuger-Anlage**, die Verbesserung der **Pufferspeicher-Auswahl**
> (Trennung Heizung/Brauchwasser, Pflicht zur vorherigen Anlage im Projekt) sowie die
> **stundenwertbasierte Führung des Brauchwasserbedarfs** als eigenen Bedarfskanal
> neben dem Heizwärmebedarf. Es geht den pragmatischen Weg: Die vorhandene Mechanik
> (`WQ_*`/`WS_*`-Spalten, `WaermequelleClass`, `SimulationPufferspeicher`,
> Restwärme-Kaskade in `SimulationControl`) wird verallgemeinert, nicht ersetzt.

---

## 1. Getroffene Entscheidungen (abgestimmt am 11.08.2026)

| # | Punkt | Entscheidung |
|---|---|---|
| E1 | Zuordnungsebene | Quelle und Senke werden **je einzelner Anlage** zugeordnet (je Zeile in `Tab_Energieanlagen`), nicht je Erzeugertyp |
| E2 | Anzahl Senken | **Hauptsenke + optionale Zweitsenke** je Anlage. Die Hauptsenke ist Pflicht; die Zweitsenke dient ausschließlich der Überschussverwertung (z. B. Solarthermie: primär Puffer Brauchwasser, Überschuss in Puffer Heizung) |
| E3 | Pufferspeicher-Pflicht | Ein Pufferspeicher ist **nur dann Pflicht, wenn eine Quelle oder Senke vom Typ Pufferspeicher gewählt wurde** — dann muss er zuvor **im Projekt angelegt** sein (kein implizites Kopieren aus den Stammdaten mehr). Ohne Puffer-Quelle/-Senke ist kein Pufferspeicher erforderlich |
| E4 | Quellentyp Erdreich | **Vereinfachtes Erdreichmodell**: Erdreichtemperatur wird aus den Klimadaten der Projekt-Klimaregion abgeleitet (gedämpfter, phasenverschobener Jahresgang, Parameter Tiefe/Bodentyp); kein manuelles Eintippen nötig. Monatsprofil/CSV bleiben als Alternativen erhalten |
| E5 | Umsetzungsumfang | **Stufenkonzept**: Stufe 0/1 = Datenmodell + Konfiguration vollständig, Engine wertet Senken für Wärmepumpe + Solarthermie aus; Stufe 2 = Heizkessel/BHKW an Puffer; Stufe 3 = TWW-Zapfprofile (eigenes Konzept) |
| E6 | Brauchwasser-Bilanz | Der Brauchwasserbedarf wird als **eigener Stundenwerte-Kanal** (`float[8760]`) geführt und zusammen mit dem Heizwärmebedarf zum Gesamtwärmebedarf zusammengeführt. Die gesamte Erzeuger-Kaskade rechnet **zweikanalig** (Heizung / Brauchwasser), nicht mehr auf einem Summenvektor |

---

## 2. Ausgangslage im Code (Ist-Analyse)

### 2.1 Was bereits existiert

Die Grundmechanik für Quelle/Senke ist für **Wärmepumpen** bereits gebaut und dient
als Vorlage für die Verallgemeinerung:

- **`Tab_Energieanlagen`** trägt (per `WaermequelleClass.SchemaSicherstellen()`
  zur Laufzeit angelegte) Spalten: `Prioritaet`, `WQ_Typ`, `WQ_Temp`,
  `WQ_Monatswerte`, `WQ_Wochenwerte`, `WQ_CSV`, `WQ_Puffer`, `WQ_Spreizung`,
  `WQ_Regeneration`, `WQ_Unbegrenzt`, `WS_Typ`, `BM_Typ`.
- **Quellentypen** (`WaermequelleClass.TypWerte`): `Aussenluft`, `Konstant`,
  `Pufferspeicher`, `Profil` (12 Monatswerte + 168 Wochenwerte),
  `CSV` (8760 Stundenwerte). Eingabedialoge `Form_Quellprofil` und
  `Form_QuellePufferspeicher` existieren.
- **`WS_Typ`** (Werte `Beides`/`Warmwasser`/`Heizung`) ist eine **Bedarfsart**,
  keine hydraulische Senke; ausgewertet nur in `SimulationWaermepumpe`
  (`SenkeAbziehen`, mit Warmwasservorrang bei `Beides`).
- **`SimulationPufferspeicher`** ist ein generisches Energiebilanzmodell
  (Q_max = Volumen·1,16·ΔT/1000, Hysterese `SchwelleEin`/`SchwelleAus`,
  füllstandsanteilige Verluste, Regeneration) und bereits mehrfach instanzierbar.
- **`Form_Simulation_Config`**: Übersichts-ListView mit Spalten
  `Prio | Wärmeerzeuger | Anlage(n) im Projekt | WP-Prio | Wärmequelle | Wärmesenke | Betriebsmodus | Pufferspeicher`;
  Bearbeitung per Doppelklick — heute nur für WP-Zeilen aktiv (`istWP`).

### 2.2 Heutige Grenzen, die dieses Konzept aufhebt

1. **Senke nur als Bedarfsart:** „Heizkreis / Pufferspeicher Heizung /
   Pufferspeicher Brauchwasser" existiert nicht; kein Bezug auf einen konkreten
   Speicher.
2. **Puffer-Zuordnung je Erzeuger-Typ statt je Anlage:** `Z_ProjektPufferSp.Erzeuger`
   ist ein Textwert („Wärmepumpe", „Heizkessel" …); `SimulationControl` wertet nur
   den **ersten** Eintrag mit `Erzeuger == "Wärmepumpe"` aus (`break` nach Treffer) —
   Zuordnungen zu Kessel/BHKW/Solarthermie sind heute **wirkungslos**.
3. **Genau ein Speicher projektweit**, keine Trennung Heizung/Brauchwasser;
   `Tab_Pufferspeicher` hat keine Eigenschaft „Verwendung".
4. **Puffer-Auswahl aus den Stammdaten:** `Form_QuellePufferspeicher` listet
   `Tab_Pufferspeicher_STAMM` direkt; `Form_KonfigPufferspeicher` erhält seine
   Liste von `Form_Simulation_Config`, das sie ebenfalls aus
   `Tab_Pufferspeicher_STAMM` lädt;
   `Z_ProjektPufferSpCtrl.Insert()` kopiert beim Speichern implizit ins Projekt
   (`CopyFromStamm`). Referenzierung per **Bezeichner-String**, nicht per ID.
5. **Quellentyp „Erdreich" fehlt** (nur über Konstant/Profil/CSV nachbildbar);
   Luft-Wasser-WP wird über Literalvergleich `Tab_WP.Typ == "Luft-Wasser"` erkannt.
6. **Ein Bedarfsvektor:** `Do_Simulation` reicht `Eingang`/`Ausgang` (`float[8760]`)
   seriell durch die Kaskade. Der Brauchwasseranteil (`brauchwasserwerte[8760]`,
   berechnet in `SimulationWaermebedarf.Brauchwasserwaerme_berechnen` aus
   Monatswerten × 168-h-Wochen-Zapfprofil) wird zwar stündlich ermittelt, aber
   sofort in den Gesamtvektor addiert — nur die WP kennt ihn danach noch
   (`Warmwasserbedarf_stuendlich`, ungekürzt statt Rest). Kessel, BHKW und
   Solarthermie sehen den WW-Anteil überhaupt nicht.
7. **Solarthermie-Überschuss wird verworfen** (`SimulationSolarthermie`:
   Kappung am Momentanbedarf, `Ueberschuss[]` nur gezählt) — größter fachlicher
   Hebel einer Speicheranbindung.
8. **BHKW-Pendelspeicher** ist ein Skalar mit hartkodierten Regelschwellen —
   kein `SimulationPufferspeicher`.

---

## 3. Zielbild: das Quellen-/Senken-Modell

### 3.1 Begriffe

Jede Wärmeerzeuger-**Anlage** (Zeile in `Tab_Energieanlagen`: WP, Heizkessel,
BHKW, Solarthermie) erhält:

- **genau eine Hauptsenke** (Pflicht, E1/E2):
  - `HEIZKREIS` — direkte Deckung des Momentanbedarfs (Verhalten wie heute),
  - `PUFFER_HEIZUNG` — Anlage lädt einen Projekt-Pufferspeicher mit Verwendung „Heizung",
  - `PUFFER_BRAUCHWASSER` — Anlage lädt einen Projekt-Pufferspeicher mit Verwendung „Brauchwasser";
- **optional eine Zweitsenke** (E2), nur zur Verwertung von Überschuss/Ladepotenzial,
  wenn die Hauptsenke gedeckt bzw. der Hauptpuffer voll ist. Zulässig ist jede
  Senke ≠ Hauptsenke; typische Fälle: Solar → primär `PUFFER_BRAUCHWASSER`,
  Zweitsenke `PUFFER_HEIZUNG`; WP im PV-Modus → primär `HEIZKREIS`,
  Zweitsenke `PUFFER_HEIZUNG`;
- **Wärmepumpen zusätzlich genau eine Wärmequelle**:
  - Typ **Luft-Wasser**: fest `Luft` (Außentemperatur der Klimaregion; nicht änderbar, wie heute),
  - Typ **Sole/Wasser**: wählbar `Erdreich` *(neu, E4)*, `Konstante Temperatur`,
    `Temperaturprofil` (Monat/Tag → Jahr, vorhandener Mechanismus),
    `Stundenprofil CSV` (8760 Werte, vorhanden), `Pufferspeicher`
    (Heizung **oder** Brauchwasser — z. B. Abwärmenutzung, kaskadierte WP).

Die bestehende Bedarfsart `WS_Typ` (`Beides`/`Warmwasser`/`Heizung`) bleibt als
**Feinsteuerung für die Hauptsenke `HEIZKREIS`** erhalten (welchen Kanal die Anlage
direkt bedient, Default `Beides` mit WW-Vorrang). Bei Puffer-Senken ist der Kanal
durch die **Verwendung des Puffers** eindeutig bestimmt; `WS_Typ` wird dort ignoriert.

### 3.2 Kanalmodell (E6)

Die Simulation führt künftig **zwei Bedarfskanäle** parallel durch die Kaskade:

```
Kanal HEIZUNG      [8760] = Gebäude-Heizwärme + externe Lastgänge
                            + Prozesswärme + Netzverluste (anteilig, 7.4)
Kanal BRAUCHWASSER [8760] = brauchwasserwerte (heute schon stündlich berechnet)

Gesamtwärmebedarf  [8760] = HEIZUNG + BRAUCHWASSER   (Ausweisung wie bisher)
```

Jede Anlage bedient gemäß Senke den passenden Kanal; Pufferspeicher entladen
in „ihren" Kanal. Die Ergebnisgrößen (`Waermebedarf_Brauchwasser`,
Deckungsgrade usw.) bleiben kompatibel, werden aber erstmals **exakt**, weil der
WW-Restbedarf durch die Kaskade mitgeführt wird statt nur pauschal am
WP-Modul gekappt (heutige Ungenauigkeit: `Warmwasserbedarf_stuendlich` =
voller WW-Bedarf statt Rest nach vorgeschalteten Erzeugern).

### 3.3 Pufferspeicher im Projekt (E3)

Ein Pufferspeicher, der als Quelle **oder** Senke dienen soll, muss zuvor als
**Projekt-Pufferspeicher** angelegt worden sein (Gewerk Pufferspeicher / Wizard
bzw. Absprung aus der Konfiguration, 4.3). Jeder Projekt-Puffer erhält neu die
Eigenschaft **Verwendung** = `Heizung` | `Brauchwasser` sowie seine Betriebs­parameter
(Vor-/Rücklauf → Q_max, Ein-/Abschaltschwelle). Mehrere Anlagen dürfen denselben
Puffer laden (n:1 über FK). Die Auswahl­listen in der Konfiguration zeigen
**ausschließlich Projekt-Puffer**, gefiltert nach passender Verwendung; das
implizite `CopyFromStamm` beim Speichern entfällt.

Ist keine Puffer-Quelle/-Senke gewählt, ist **kein** Pufferspeicher erforderlich —
Projekte ohne Speicher bleiben uneingeschränkt simulierbar.

---

## 4. Anwendersicht (Konfiguration unter „Simulation")

### 4.1 Erweiterte Erzeuger-Übersicht in `Form_Simulation_Config`

Die vorhandene `listView_Uebersicht` wird für **alle** Erzeuger-Zeilen aktiv
(heute nur WP): jede Anlagen-Zeile erhält `Tag = AnlagenInfo` und die Spalten
werden umgebaut — links die Quelle, rechts die Senke, wie gefordert:

```
┌─ Übersicht Wärmeerzeuger ────────────────────────────────────────────────────┐
│ Prio │ Erzeuger     │ Anlage        │ Wärmequelle (*)   │ Wärmesenke (*)     │ Zweitsenke │ Modus │
├──────┼──────────────┼───────────────┼───────────────────┼────────────────────┼────────────┼───────┤
│ 1    │ Wärmepumpe   │ Vitocal 300   │ Erdreich (1,5 m)  │ Puffer Heizung     │ –          │ Lauf. │
│ 1    │ Wärmepumpe   │ Vitocal 350   │ Konstant 10 °C    │ Puffer Brauchw.    │ –          │ PV    │
│ 2    │ Heizkessel   │ Vitola 200    │ –                 │ Heizkreis          │ –          │ –     │
│ 3    │ Solarthermie │ Vitosol 200-F │ –                 │ Puffer Brauchw.    │ Puffer Hzg.│ –     │
├──────┴──────────────┴───────────────┴───────────────────┴────────────────────┴────────────┴───────┤
│ Pufferspeicher im Projekt:  PS 800 (Heizung, 800 l) · WW 500 (Brauchwasser, 500 l)                │
│ [Pufferspeicher anlegen…]  [Pufferspeicher bearbeiten…]                                           │
└───────────────────────────────────────────────────────────────────────────────────────────────────┘
```

- Doppelklick **Wärmequelle** (nur WP-Zeilen): Dropdown mit den je WP-Typ
  zulässigen Typen (3.1). Luft-Wasser → gesperrt mit Hinweis (wie heute).
- Doppelklick **Wärmesenke** / **Zweitsenke** (alle Zeilen): Auswahldialog 4.2.
- Die bisherige Rubrik „Pufferspeicher:" mit Filter-Checkboxen und die
  `listView1`-Zuordnungstabelle entfallen; an ihre Stelle tritt die Liste der
  **Projekt-Puffer** mit Anlegen/Bearbeiten (4.3). Die Speicherregelung
  (Schwellen) wird je Puffer gepflegt, nicht mehr je Zuordnung.

### 4.2 Senkendialog `Form_Waermesenke` (neu)

```
┌─ Wärmesenke — Vitocal 300 ───────────────────────────────┐
│  Hauptsenke:                                             │
│   (•) Heizkreis (direkt)                                 │
│        Bedarfsart: [Beides ▾]  (nur bei Heizkreis)       │
│   ( ) Pufferspeicher Heizung      [PS 800 (800 l)  ▾]    │
│   ( ) Pufferspeicher Brauchwasser [WW 500 (500 l)  ▾]    │
│                                                          │
│  ☐ Zweitsenke (Überschussverwertung):                    │
│      [Pufferspeicher Heizung ▾]  [PS 800 (800 l) ▾]      │
│                                                          │
│  ⓘ Für Puffer-Senken muss der Speicher im Projekt        │
│    angelegt sein.        [Pufferspeicher anlegen…]       │
│                                     [Abbrechen] [OK]     │
└──────────────────────────────────────────────────────────┘
```

- Die Puffer-Dropdowns listen **nur Projekt-Puffer passender Verwendung**.
  Existiert keiner, ist die Option wählbar, aber OK blockiert mit Hinweis und
  Absprung `[Pufferspeicher anlegen…]` (öffnet 4.3 im Modus „Neu").
- Zweitsenke: nur aktivierbar, wenn ≠ Hauptsenke; `HEIZKREIS` als Zweitsenke
  ist zulässig (Puffer-Vorrang, Rest direkt).

### 4.3 Pufferspeicher anlegen/bearbeiten (Projektebene)

Einstieg aus der Konfiguration (`[Pufferspeicher anlegen…]`) und aus dem Gewerk
Pufferspeicher. Der Dialog kombiniert die vorhandenen Bausteine
(`Form_PufferSp_Admin` als Katalogbrowser, `PufferSpCtrl.CopyFromStamm` als
**explizite** Übernahme):

1. Katalogauswahl aus `Tab_Pufferspeicher_STAMM` (inkl. VDI-3805-Importe)
   **oder** freie Eingabe (Bezeichner, Volumen, Bereitschaftsverluste).
2. Pflichtfeld **Verwendung**: `Heizung` | `Brauchwasser`.
3. Betriebsparameter: Vorlauf/Rücklauf [°C] (Vorbelegung aus
   `Abfrage_Erzeuger_Vorlauftemperaturen`/`…Ruecklauftemperaturen` wie heute),
   Einschalt-/Abschaltschwelle [%] (Default 10/95).
4. Speichern → Zeile in `Tab_Pufferspeicher` (Projektkopie) mit neuen Spalten (5.1).

Damit ist „Pufferspeicher muss zuvor angelegt worden sein" (E3) erfüllt, ohne den
Anwender zu gängeln: der Weg vom Senkendialog zur Neuanlage ist ein Klick.

### 4.4 Quellendialog Erdreich (neu, E4)

```
┌─ Wärmequelle Erdreich — Vitocal 300 ─────────────────────┐
│  Quellsystem:  (•) Erdkollektor (Verlegetiefe [1,5] m)   │
│                ( ) Erdsonde     (mittl. Tiefe  [50 ] m)  │
│  Bodentyp:     [Lehm (feucht) ▾]                         │
│                                                          │
│  Vorschau: Jahresgang der Quelltemperatur (Chart)        │
│  min 4,2 °C (Feb) · max 14,8 °C (Aug) · Mittel 9,6 °C    │
│                                     [Abbrechen] [OK]     │
└──────────────────────────────────────────────────────────┘
```

Berechnung ohne manuelle Profileingabe aus den Klimadaten der Projekt-Klimaregion
(`Tab_Solar.Temperatur` je `ID_Klimaregion`, 8760 Werte — bereits geladen als
`SimulationWaermebedarf.Stundentemperatur`):

```
T_Boden(z, t) = T_m − A · e^(−z·√(π/(8760·α))) · cos( 2π·(t − t_min)/8760 − z·√(π/(8760·α)) )

T_m    Jahresmittel der Außentemperatur (aus Klimadaten)
A      halbe Jahresamplitude (aus Monatsmitteln der Klimadaten)
z      Tiefe [m] (Kollektor: Verlegetiefe, Default 1,5 m;
                  Sonde: halbe Sondenlänge als wirksame Tiefe, stark gedämpft)
α      Temperaturleitfähigkeit des Bodens [m²/h] je Bodentyp
       (Katalog: Sand trocken / Sand feucht / Lehm / Ton / Fels)
t_min  Stundenindex des Temperaturminimums (aus Klimadaten, ≈ Ende Januar)
```

Das ist die übliche gedämpft-phasenverschobene Bodentemperaturgleichung (Kusuda);
sie liefert je Anlage ein `float[8760]`-Quellprofil und fügt sich damit exakt in
den vorhandenen Rückgabeweg von `WaermequelleClass.Quelltemperatur()` ein.
Ab ca. 10 m Tiefe degeneriert der Jahresgang zur Konstanten ≈ T_m — die Sonde
liefert also automatisch nahezu konstante Quelltemperatur. Entzugsleistung/
Regeneration werden in Stufe 1 **nicht** modelliert (bewusste Vereinfachung,
dokumentiert im Ergebnis); wer Quellerschöpfung abbilden will, nutzt den
Quellentyp `Pufferspeicher` mit Regeneration.

### 4.5 Validierung beim Speichern (E3)

`btn_Speichern_Click` prüft vor dem Persistieren je Anlage:

| Prüfung | Verhalten bei Verstoß |
|---|---|
| Hauptsenke gesetzt (jede Anlage) | Default `HEIZKREIS` wird gesetzt (nie leer) — erfüllt „Wärmeerzeuger soll immer eine Wärmesenke haben" |
| Senke `PUFFER_*` → zugeordneter Projekt-Puffer existiert und hat passende Verwendung | Speichern blockiert; Meldung mit Anlagen-/Puffername + Absprung „Pufferspeicher anlegen…" |
| Quelle `Pufferspeicher` → dito | dito |
| Zweitsenke ≠ Hauptsenke | Speichern blockiert |
| Puffer wird von mind. einer Anlage geladen, aber sein Kanal hat keinen Bedarf (z. B. WW-Puffer ohne Brauchwasser im Projekt) | Warnung (kein Blocker) |
| Puffer als Quelle **und** Senke derselben Anlage | Speichern blockiert (Kurzschluss) |

---

## 5. Datenmodell

### 5.1 Erweiterung `Tab_Pufferspeicher` (Projektkopie)

Neue Spalten (additiv, über das vorhandene `SpalteSicherstellen`-Muster bzw.
`SQL=ALTER TABLE`-Skript):

| Spalte | Typ | Bedeutung |
|---|---|---|
| `Verwendung` | TEXT(50) | `Heizung` \| `Brauchwasser` (Pflicht bei Neuanlage; Migration 5.4) |
| `Vorlauf` | LONG | Bezugsvorlauf [°C] → Q_max |
| `Ruecklauf` | LONG | Bezugsrücklauf [°C] |
| `Schwelle_Ein` | DOUBLE | Einschaltschwelle Nachladung [%], Default 10 |
| `Schwelle_Aus` | DOUBLE | Abschaltschwelle [%], Default 95 |

Die Betriebsparameter wandern damit von der Zuordnung (`Z_ProjektPufferSp`) an den
**Speicher selbst** — ein Puffer hat genau einen Betriebszustand, egal wie viele
Anlagen ihn laden.

### 5.2 Erweiterung `Tab_Energieanlagen`

Neue Spalten über `WaermequelleClass.SchemaSicherstellen()` (bestehender
Mechanismus, nur um `SpalteSicherstellen(...)`-Aufrufe ergänzen):

| Spalte | Typ | Bedeutung |
|---|---|---|
| `WS_Ziel` | TEXT(50) | Hauptsenke: `Heizkreis` \| `PufferHeizung` \| `PufferBrauchwasser` (Default `Heizkreis`) |
| `WS_ID_Puffer` | LONG | FK → `Tab_Pufferspeicher.ID` (Projekt!), wenn `WS_Ziel = Puffer*` |
| `WS_Ziel2` | TEXT(50) | Zweitsenke (leer = keine) |
| `WS_ID_Puffer2` | LONG | FK für die Zweitsenke |
| `WQ_ID_Puffer` | LONG | FK → `Tab_Pufferspeicher.ID` für Quelle `Pufferspeicher` — **ersetzt** die Bezeichner-Referenz `WQ_Puffer` (bleibt als Altspalte lesbar) |
| `WQ_Tiefe` | DOUBLE | Erdreich: Tiefe z [m] |
| `WQ_Bodentyp` | TEXT(50) | Erdreich: Katalogschlüssel Bodentyp |
| `WQ_Quellsystem` | TEXT(50) | `Kollektor` \| `Sonde` |

`WaermequelleClass` erhält den neuen Typwert `TYP_ERDREICH = "Erdreich"`
(Erweiterung von `TypWerte`/`TypAnzeige` und des `switch` in `Quelltemperatur()`;
zusätzlich die UI-Dispatcher `WaermequelleAnzeige()` und
`WqCombo_SelectedIndexChanged()` in `Form_Simulation_Config.cs`).
`WS_Typ` bleibt bestehen (Bedarfsart bei `Heizkreis`, 3.1); `BM_Typ` unverändert.

### 5.3 `Z_ProjektPufferSp` wird abgelöst

Die Zuordnung Anlage↔Puffer liegt künftig **als FK an der Anlage** (5.2); die
Verwendung und die Betriebsparameter am Puffer (5.1). `Z_ProjektPufferSp` wird
nicht mehr geschrieben und nur noch von der Migration (5.4) gelesen; die Tabelle
bleibt für Alt-Datenbanken erhalten. Das beseitigt nebenbei drei Altlasten:
Text-Referenz über `Erzeuger`-Literale, `break` nach dem ersten Treffer,
`Schwelle_*` außerhalb des Models.

### 5.4 Migration (einmalig je Projekt, beim ersten Öffnen der Konfiguration)

| Altbestand | Übernahme |
|---|---|
| `Z_ProjektPufferSp` mit `Erzeuger='Wärmepumpe'` (erster Eintrag nach `Prioritaet` — heute gewinnt dieser auch dann, wenn sein Speicherdatensatz fehlt) | Projekt-Puffer erhält `Verwendung='Heizung'`, `Vorlauf`/`Ruecklauf`/`Schwelle_*` aus der Zuordnung; **alle** WP-Anlagen des Projekts: `WS_Ziel='PufferHeizung'`, `WS_ID_Puffer` = Puffer-ID (entspricht dem heutigen Verhalten: ein gemeinsamer WP-Puffer) |
| `Z_ProjektPufferSp` mit anderem `Erzeuger` | keine Übernahme (war schon heute wirkungslos); Protokollhinweis |
| `WS_Typ` vorhanden, kein Puffer | `WS_Ziel='Heizkreis'`, `WS_Typ` bleibt als Bedarfsart |
| `WQ_Typ='Pufferspeicher'` mit `WQ_Puffer` (Bezeichner, Stamm) | Existiert ein Projekt-Puffer gleichen Bezeichners → `WQ_ID_Puffer` setzen; sonst Hinweis „Quell-Puffer im Projekt anlegen" (E3 greift ab jetzt) |
| Alle übrigen Anlagen ohne `WS_Ziel` | `WS_Ziel='Heizkreis'` |

### 5.5 Schema-Ausrollung

Beide vorhandenen Mechanismen werden bedient: `SchemaSicherstellen()` für die
Laufzeit-Selbstmigration (Spalten) und das Update-Skript für
`UpdateDatabaseFromScript` (`SQL=ALTER TABLE …`-Zeilen) für die reguläre
Auslieferung. **Wichtig:** die neuen Spalten von `Tab_Pufferspeicher` müssen
auch in `ProjektDuplizierenCtrl` (Variantenkopie) und in der IMPORT-Sektion des
Update-Skripts ergänzt werden, sonst verlieren Varianten/Updates die Zuordnung.

---

## 6. Simulations-Engine

### 6.1 Speicher-Registry statt Einzelspeicher (Stufe 1)

`SimulationControl.Do_Simulation` baut statt `puffer_wp` eine Registry auf:

```csharp
// je Projekt-Puffer genau EINE Instanz, geteilt von allen Erzeugern
Dictionary<int /*ID Tab_Pufferspeicher*/, SimulationPufferspeicher> speicher;
// Zusatzfeld am Simulationsspeicher:
sp.Verwendung = "Heizung" | "Brauchwasser";
```

Init je Puffer aus `Tab_Pufferspeicher` (Volumen, Vorlauf, Rücklauf, Verluste,
Schwellen). Kompatibilität: `puffer_wp` zeigt weiterhin auf den ersten
Heizungs-Puffer (für `NavigatorWaerme`, `Form_Simulation_Detail`), bis die
Anzeigen auf die Registry umgestellt sind.

### 6.2 Zweikanalige Kaskade (Stufe 1)

```
Rest_Heiz[8760] = Heizkanal   (Gebäude + Extern + Prozess + Netzverluste)
Rest_WW  [8760] = brauchwasserwerte

je Stunde h, je Kaskadenstufe (tool[0..3], darin Anlagen nach Prioritaet):
  1. Puffer-Entladung:  jeder Speicher entlädt gemäß Hysterese in seinen Kanal
                        (WW-Puffer → Rest_WW, Heiz-Puffer → Rest_Heiz)
  2. Anlage produziert: verfügbarer Bedarf gemäß Senke
       HEIZKREIS            → Rest_WW/Rest_Heiz nach WS_Typ (WW-Vorrang bei Beides)
       PUFFER_HEIZUNG       → Ladefähigkeit des Speichers (bis Q_max·SchwelleAus)
       PUFFER_BRAUCHWASSER  → dito
  3. Zweitsenke:        verbleibendes Ladepotenzial/Überschuss → WS_Ziel2
  4. StundeAbschliessen() je Speicher (Verluste, SOC-Ganglinie)
```

Die bestehende WP-Logik (Hysterese `_speicherLaden`, Betriebsmodus
`Laufzeit`/`Leistung`/`PV` als Ladepotenzial-Begrenzung, Quellspeicher-Bilanz,
Heizstab) bleibt erhalten und wird lediglich auf „Speicher der Anlage" statt
„der eine WP-Puffer" umgestellt. **Solarthermie** erhält als zweites Modul die
Senkenauswertung: statt Kappung am Momentanbedarf lädt der Überschuss den
zugeordneten Puffer (`Ueberschuss[]` bleibt als „nicht verwertbar" nur für den
Rest). Dazu muss `solarthermie_list` von `ID_Solar` auf `Tab_Energieanlagen.ID`
umgestellt werden (gleiches gilt später für BHKW/Kessel-Listen) — sonst fehlt
der Bezug zu `WS_*`.

Kessel und BHKW behandeln in Stufe 1 beide Kanäle wie heute den Summenbedarf
(`HEIZKREIS`/`Beides`); ihre Senkenauswahl ist konfigurierbar, wird aber erst in
Stufe 2 ausgewertet (Anzeige „(wirksam ab Stufe 2)" im Dialog, damit keine
stillen Erwartungslücken entstehen).

### 6.3 Stufe 2: Heizkessel und BHKW an Puffer

- `SimulationSPK`: je Kessel Senkenauswertung analog WP (Puffer laden bis
  Abschaltschwelle statt reiner Momentandeckung); Betriebsbereitschaftsverluste
  unverändert.
- `SimulationBHKW`: der skalare `kapazitaetPendelspeicher` (Belegung in
  `SimulationControl.Simulation_BHKW_Ctrl`: `Volumen · 20000/860`; Regelschwellen
  30/10/20 % hartkodiert in `SimulationBHKW`) wird durch einen
  zugeordneten `SimulationPufferspeicher` ersetzt; die drei Fahrweisen
  (wärme-/stromgeführt/ohne Einspeisung) laden/entladen dann denselben
  Speichertyp wie alle anderen Erzeuger. Der bisherige Pendelspeicher-Parameter
  in `Tab_Einstellungen` wird zur Migrationsquelle (legt bei Bedarf einen
  Projekt-Puffer „BHKW-Pendelspeicher" an).

### 6.4 Stufe 3: Brauchwasser-Bedarf verfeinern (Anschluss TWW-Konzept)

Der WW-Kanal ist ab Stufe 1 ein sauberer Andockpunkt: heute wird er aus
Monatswerten × 168-h-Wochenprofil erzeugt (`Brauchwasserwaerme_berechnen`).
Das Konzept `Konzept_TWW-Zapfprofile_WP-Plan_1.md` ersetzt genau diese Quelle
durch die Zapfprofil-Engine (`zapfwerte[8760]` + getrennter Zirkulationskanal),
über die dort beschriebene Weiche in `SimulationWaermebedarf` mit Default „alt".
Für das vorliegende Konzept ändert sich dadurch **nichts an Kaskade, Senken oder
Speichern** — nur die Befüllung von `Rest_WW` wird besser. Die
Speicherauslegung (DIN EN 12831-3-Summenlinie) bleibt Bestandteil des
TWW-Konzepts und wird hier nicht dupliziert.

### 6.5 Ergebnis-Persistenz (Stufe 1, klein)

`Tab_ErgebnisWaermepumpe.Kapazitaet_Pufferspeicher` wird künftig aus
`SimulationPufferspeicher.Q_max` gefüllt (statt Legacy `Volumen · 1,16`).
Neu (kleine Tabelle, Muster `StelleEnergieSpaltenSicher()` /
`StelleBHKWSpaltenSicher()` in `ErgebnisCtrl`):
**`Tab_ErgebnisPufferspeicher`** — je Simulationslauf und Puffer:
`ID, ID_Ergebnis, ID_Pufferspeicher, Bezeichner, Verwendung, Q_max,
Ladung_gesamt, Entladung_gesamt, Verluste_gesamt, Vollzyklen`.
Damit bekommen Variantenbericht und Wirtschaftlichkeit erstmals belastbare
Speicherbilanzen (Bezug: `Konzept_Variantenbericht.md` Kap. 4,
Kostenkomponente Puffer = 6 in `Tab_ProjektWerte`).

---

## 7. Technische Struktur

### 7.1 Neue und geänderte Dateien

| Datei | Art | Inhalt |
|---|---|---|
| `Allgemein/Simulation/WaermequelleClass.cs` | ändern | `TYP_ERDREICH`, Spalten 5.2 in `SchemaSicherstellen`, Erdreich-Fall in `Quelltemperatur()` (Kusuda, Klimadaten), `WQ_ID_Puffer` statt Bezeichner in `Quellspeicher()` (Projekt- statt Stammtabelle!) |
| `Allgemein/Simulation/ErdreichTemperatur.cs` | neu | Kusuda-Berechnung + Bodentyp-Katalog (α je Typ), Ableitung T_m/A/t_min aus `Stundentemperatur` |
| `Allgemein/Simulation/SimulationControl.cs` | ändern | Speicher-Registry (6.1), zweikanalige Kaskade (6.2), Anlagen-Listen auf `Tab_Energieanlagen.ID` |
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | ändern | Feld `Verwendung`; sonst unverändert |
| `Allgemein/Simulation/SimulationWaermepumpe.cs` | ändern | Senke aus `WS_Ziel`/`WS_ID_Puffer` je Modul; `Warmwasserbedarf_stuendlich` = Restkanal statt Vollbedarf |
| `Allgemein/Simulation/SimulationSolarthermie.cs` | ändern | Überschuss → zugeordneter Puffer (Haupt-/Zweitsenke) |
| `Allgemein/Simulation/SimulationWaermebedarf.cs` | ändern | Heiz- und WW-Kanal getrennt bereitstellen (Summenfelder bleiben) |
| `Views/Simulation/Form_Simulation_Config.cs` | ändern | Übersicht für alle Erzeuger, Spalten 4.1, Validierung 4.5, Puffer-Rubrik → Projektliste |
| `Views/Simulation/Form_Waermesenke.cs` | neu | Senkendialog 4.2 (programmatisch, Muster `Form_QuellePufferspeicher`) |
| `Views/Simulation/Form_QuelleErdreich.cs` | neu | Erdreichdialog 4.4 mit Vorschau-Chart |
| `Views/Simulation/Form_QuellePufferspeicher.cs` | ändern | Liste aus `Tab_Pufferspeicher` (Projekt, Verwendungsfilter) statt STAMM; FK statt Bezeichner |
| `Views/Pufferspeicher/Form_PufferSp_Bearbeiten.cs` | ändern | Felder Verwendung, Vorlauf/Rücklauf, Schwellen; **Projektmodus ist neu** — das Formular arbeitet heute ausschließlich auf `Tab_Pufferspeicher_STAMM` |
| `Controller/PufferSpCtrl.cs` | ändern | neue Spalten, `ReadAllProjekt(idProjekt, verwendung)` |
| `Controller/WErzeugerCtrl.cs` | ändern | neue Spalten lesen; Bugfix Spaltenname `Rücklauf`→`Ruecklauf` in `ReadAllFilter` (9/O3) |
| `MigrationQuellenSenken.cs` (Allgemein/Update) | neu | Migration 5.4 inkl. Protokoll |
| Satelliten-`.resx` (de-DE/en-US) | ändern/neu | alle neuen sichtbaren Texte zweisprachig (Projektkonvention; die neuen Dialoge programmatisch + Ressourcenzugriff wie `MyResource.Resource.*`) |

### 7.2 Bewusst nicht verändert

`KonfigurationModel`/`Tab_Einstellungen` (positionsbasiertes `row[0..22]`-Lesen —
neue Konfigurationsdaten gehen bewusst **nicht** dorthin, sondern in
`Tab_Energieanlagen`/`Tab_Pufferspeicher` über den etablierten
`WertLesen`/`WertSchreiben`-Kanal); die Slot-Logik `Tool_1..4`
(Einsatzreihenfolge der Erzeugertypen) bleibt wie sie ist; die Stromseite
(PV, SSP) bleibt unberührt.

---

## 8. Umsetzungsschritte und Aufwand

| Schritt | Inhalt | Stufe | Aufwand |
|---|---|---|---|
| 1 | Schema (5.1/5.2), `WaermequelleClass`-Erweiterung, Migration 5.4 | 0 | 2–3 PT |
| 2 | Konfigurations-UI: Übersicht alle Erzeuger, `Form_Waermesenke`, Projekt-Puffer-Verwaltung, Validierung 4.5 | 0 | 3–4 PT |
| 3 | Erdreichmodell (`ErdreichTemperatur`, Dialog, Vorschau) | 0 | 1–2 PT |
| 4 | Engine: Speicher-Registry, zweikanalige Kaskade, WP-Umstellung | 1 | 3–4 PT |
| 5 | Solarthermie an Puffer (Haupt-/Zweitsenke) | 1 | 1–2 PT |
| 6 | Ergebnis-Persistenz `Tab_ErgebnisPufferspeicher` + Anzeige (Navigator/Detail auf Registry) | 1 | 1–2 PT |
| 7 | Test mit Realprojekten (nur Heizkreis / WP+WW-Puffer / Solar-Zweitsenke / Migration Altprojekte / Projekt ohne Puffer) | 1 | 2 PT |
| 8 | Heizkessel + BHKW an Puffer, Pendelspeicher-Ablösung | 2 | 3–4 PT |
| | **Summe Stufe 0+1** | | **13–17 PT** |
| | Stufe 2 zusätzlich | | 3–4 PT |
| | Stufe 3 (TWW-Zapfprofile) | | eigenes Konzept |

---

## 9. Risiken und offene Punkte

| # | Punkt | Status/Maßnahme |
|---|---|---|
| O1 | **Regelstrategie bei mehreren Ladern an einem Puffer** (WP + Solar auf denselben Heizpuffer): Reihenfolge = Kaskadenposition; reicht das, oder braucht Solar generellen Ladevorrang? | Vorschlag: Solar lädt vor allen anderen (kostenlose Wärme), sonst Kaskadenreihenfolge — im Test kalibrieren |
| O2 | **Netzverluste-Kanalzuordnung**: heute gleichverteilt auf den Gesamtbedarf; Vorschlag anteilig nach Kanalsumme, alternativ vollständig auf Heizung | Entscheidung bei Umsetzung Schritt 4 |
| O3 | `WErzeugerCtrl.ReadAllFilter` liest `Rücklauf` (Umlaut) statt `Ruecklauf` → Feld bleibt 0 | Bugfix in Schritt 1 mitnehmen |
| O4 | `MessageBox`-Aufrufe in der Engine (`SimulationWaermepumpe`, `SimulationWaermebedarf`, `Z_ProjektPufferSpCtrl`) blockieren Headless-Läufe (`SimulationRunner` aus Variantenbericht E10) | im Zuge von Schritt 4 durch Fehlerliste/Log ersetzen |
| O5 | `Heizstab_stuendlich[stunde]` wird je WP-Modul zugewiesen statt addiert | Bugfix in Schritt 4 |
| O6 | `WErzeugerModel.ID_PUFFER` (alt, ungenutzt; `Form_PufferSp` schreibt dort Stamm- statt Projekt-IDs) | wird durch `WS_ID_Puffer` abgelöst; Altspalte nicht weiterverwenden, Wizard-Gewerk Pufferspeicher auf Projektanlage (4.3) umstellen |
| O7 | **Quellspeicher heute aus STAMM-Daten** (`Quellspeicher()` liest `Tab_Pufferspeicher_STAMM` per Bezeichner, Start „voll") | Umstellung auf Projekt-Puffer-FK (7.1); Startzustand konfigurierbar prüfen |
| O8 | Erdreichmodell ohne Entzugsleistungsbegrenzung (E4-Vereinfachung) | im Ergebnis dokumentieren; Ausbau: Kopplung an `WQ_Regeneration`-Mechanik |
| O9 | Kompatibilität Anzeigen (`NavigatorWaerme.puffer_wp`, `Form_Simulation_Detail` Pufferfelder, CSV-Export) | Übergangsweise Alias auf ersten Heizpuffer, Umstellung in Schritt 6 |
| O10 | Zweitsenke bei WP: Interaktion mit Betriebsmodus `PV` (Ladepotenzial) definieren — Zweitsenke nur aus Ladepotenzial, nie aus Pflichtbedarf | Festlegung in Schritt 4, Testfall vorsehen |
| O11 | Varianten/`ProjektDuplizierenCtrl`: neue Spalten in Kopierlogik prüfen (analog O10 im Variantenbericht-Konzept) | Schritt 1 |
