# Konzept: Stromspeicherimport EPOS-Plan — Quellenprüfung, Abbildung und Stufenplan

**Rev. 2 — 07.09.2026 — Prüfbericht, Entscheide und Umsetzungsbericht der Stufe S1.
Der Kern-Zerleger ist gebaut (Kapitel 9), und seit dem Entscheid W13‑E‑2 (Kapitel 10,
Q1…Q8 = Empfehlung) auch die MASKE, der Menüpunkt und der Netzabruf.**

Auftrag (Anwenderwunsch **W13‑E‑2**, 07.09.2026, im Wortlaut):

> „Es gibt keinen Datenimport für Stromspeicher. Dieser muss noch hinzugefügt werden
> (Administration → Datenimport). Folgende Quellen sollen auf Verfügbarkeit und
> Importmöglichkeiten geprüft werden: HTW Berlin `bslib`, NREL SAM Battery Library,
> TUM `simses`, CEC Energy Storage Systems List."

Anlass ist eine Lücke im Menü **Administration → Datenimport**: Es führt sechs Punkte
(Heizkessel, Pufferspeicher, Wärmepumpe, PV-Module, Wechselrichter, Solarkollektoren) —
und keinen für Stromspeicher. Der Katalog `Tab_Stromspeicher_STAMM` wird deshalb bis
heute ausschließlich von Hand gepflegt; die Testdatenbank führt fünf Sätze.

**Dieses Papier war ein Prüfbericht und ist seit Rev. 2 auch ein Umsetzungsbericht.**
Die Kapitel 1 bis 9 sagen unverändert, was die vier Quellen wirklich hergeben — mit
Beleg, nicht aus dem Gedächtnis —, wie sich das auf `Tab_Stromspeicher_STAMM` abbildet
und in welchen drei Stufen der Import entstehen sollte. **Kapitel 10 trägt die acht
Entscheide und das, was Stufe S1 daraus geworden ist:** Maske, Menüpunkt, Netzabruf und
Auslieferungsdatei. Einen Migrationsschritt gab es nicht — der Katalog hat alle Spalten
seit Schritt 11a. Die Stufen S2 (Kapitel 6) und S3 bleiben, wie sie beschrieben sind;
S3 ist mit Q7 ausdrücklich vertagt.

---

## 1. Prüfbericht der vier Quellen

Alle vier Quellen wurden am **07.09.2026** wirklich abgerufen (nicht aus dem Gedächtnis
beschrieben). Die Zugriffsbelege stehen je Quelle bei der Bewertung; blieb etwas
unerreichbar, steht der Grund dabei.

### 1.0 Das Ergebnis in einem Satz

**Nur EINE der vier Quellen ist ein Geräteverzeichnis kommerzieller Speicher — die
CEC-Liste.** `bslib` ist ein kleiner, sehr genau vermessener Satz von vier Systemen,
und die beiden übrigen (NREL SAM, TUM `simses`) sind ZELLPHYSIK: Spannungskurven,
Innenwiderstände, Degradationsmatrizen. Für eine Jahressimulation auf Stundenbasis
gibt es dort nichts abzubilden, weil `Tab_Stromspeicher_STAMM` keine einzige Spalte
dafür hat.

| Quelle | erreichbar | Lizenz | Datensätze | Format | für EPOS-Plan |
|---|---|---|---|---|---|
| **CEC Energy Storage System List** | **ja** (1 338 305 Byte) | Public Records Act, **Namensnennung**, kommerzielle Nutzung eingeschränkt (1.4) | **6 654** Geräte, 130 Hersteller | XLSX | **die Hauptquelle** |
| **`bslib`** (HTW Berlin / FZ Jülich) | **ja** (PyPI und zwei Repositorys) | MIT (Code), **CC BY 4.0** (Datenbank) | **7** Zeilen, davon **4** Speicher | CSV | **zweite Quelle**, einzige mit Standby |
| **NREL SAM Battery Library** | **ja**, aber anderes Ding als erwartet | BSD‑3‑Clause | **4** generische Zellchemien | JSON | **nein** (keine Geräte) |
| **TUM `simses`** | **ja** | BSD‑3‑Clause | **2** Zellmodelle (Quelltext) | Python + 5 CSV | **nein** (Zellphysik) |

---

### 1.1 `bslib` — Battery Storage Library (HTW Berlin, gepflegt vom FZ Jülich)

**Zugriffsbeleg.**

| Weg | Ergebnis |
|---|---|
| `https://pypi.org/pypi/bslib/json` | HTTP 200, 15 696 Byte |
| `bslib-0.7.tar.gz` (files.pythonhosted.org) | HTTP 200, **30 287 Byte**, hochgeladen 18.01.2023 |
| `git clone --depth 1 https://github.com/FZJ-IEK3-VSA/bslib.git` | erfolgreich |
| `git clone --depth 1 https://github.com/RE-Lab-Projects/bslib.git` | erfolgreich (HTW-Ursprungsadresse; **derselbe Stand**, HEAD `0f4e822`, 18.01.2023) |

**Was drin ist.** Das Paket führt genau **eine** Datendatei: `bslib/bslib_database.csv`,
**2 729 Byte, 8 Zeilen** (Kopfzeile und 7 Datensätze), 64 Spalten. Dazu im Repository
`input/PerModPAR.xlsx` (270 802 Byte) — die Vorlage, aus der die CSV erzeugt wird; sie
führt im Blatt `Data` **acht** Beispielsysteme, also ebenfalls keine Geräteliste.

Die sieben Zeilen zerfallen in drei Gruppen (so auch das LIESMICH): **2 AC-gekoppelte**
Systeme, **3 DC-gekoppelte** und **2 reine PV-Wechselrichter**. Davon tragen **vier**
eine nutzbare Kapazität und sind damit Speicher im Sinne von
`Tab_Stromspeicher_STAMM`:

| Kennung | Hersteller / Modell | Kopplung | E_BAT_usable | P_BAT2AC_out | eta_BAT |
|---|---|---|---|---|---|
| SG1 | Generic / AC-System | AC | 1,00 kWh | 1 000 W | 95,00 % |
| S2 | Siemens / Junelight Smart Battery 9,9 | AC | 8,85 kWh | 3 507 W | 96,87 % |
| S3 | KOSTAL PLENTICORE plus 5.5 + BYD Battery-Box H6.4 | DC | 5,68 kWh | 3 157 W | 94,82 % |
| S4 | KOSTAL PLENTICORE plus 10 + BYD Battery-Box H11.5 | DC | 10,51 kWh | 5 776 W | 95,28 % |
| ~~S5~~ | Fronius Symo GEN24 10.0 Plus + BYD H11.5 | DC | *leer* | 9 823 W | *leer* |
| ~~INV1/INV2~~ | SMA Sunny Boy 5.0 / Sunny Tripower 10.0 | PVINV | — | — | — |

**Die Herkunft der Zahlen ist das Wertvolle.** Sie stammen aus der PerMod-Datenbank der
**HTW Berlin** und der **Stromspeicher-Inspektion** — also aus einer Prüfstandsmessung
nach dem Effizienzleitfaden, nicht aus einem Datenblatt. Deshalb führt `bslib` als
**einzige der vier Quellen** die Größe, die `Tab_Stromspeicher_STAMM` unter
`Standby_Verbrauch` erwartet: den Eigenverbrauch, und zwar viermal — je Betriebszustand
(voll / leer) und je Seite (AC / DC).

**Lizenz.** `LICENSE`: MIT, „Copyright (c) 2023 FZ Jülich - IEK 3, Tjarko Tjaden, Kai
Rösken, Hauke Hoops". Das LIESMICH sagt zur Datei ausdrücklich: *„All resulting database
CSV file are under CC BY 4.0."* → **Weitergabe als Auslieferungsdatei ist erlaubt**,
solange Herkunft und Lizenz danebenstehen (dieselbe Handhabung wie bei
`VDI-3805-Daten/PV/LIESMICH_CEC_Inverters.md`). DOI der Fassung:
`10.5281/zenodo.6514527`.

**Ohne Python lesbar?** **Ja.** Die Datei ist eine gewöhnliche CSV mit Komma und Punkt;
das Python-Paket ist nur ihr Transportweg. Der Kern liest sie direkt — ein Exportskript
(Auftragspunkt 5) ist nicht nötig.

**Pflegerhythmus.** Fassung 0.7 vom **18.01.2023**, seither unverändert. Die Quelle ist
also **eingefroren**; wer sie mitliefert, liefert einen Stand aus, keinen Dienst.

**Eignung.** Klein, aber inhaltlich genau richtig: gemessene Round-Trip-Wirkungsgrade
und Standby-Verbräuche für einen Speicherbetrieb, den EPOS-Plan auf Stundenbasis rechnet.
Vier Sätze ersetzen keinen Katalog — sie sind ein **Referenz- und Vorbelegungssatz**,
insbesondere die generische Zeile SG1.

---

### 1.2 CEC Energy Storage System List (California Energy Commission)

**Zugriffsbeleg.**

| Weg | Ergebnis |
|---|---|
| `https://solarequipment.energy.ca.gov/Home/PropertyDetails/EnergyStorage` | HTTP 200, 558 722 Byte |
| `https://solarequipment.energy.ca.gov/Home/DownloadtoExcel?filename=EnergyStorage` | HTTP 200, **1 338 305 Byte**, `Content-Disposition: attachment; filename="Energy_Storage_System_List_Data_ADA.xlsx"` |
| `…?filename=Battery` (Namensraten) | HTTP 200, **0 Byte** — den Namen liefert nur der Verweis auf der Seite |

Die Datei liegt im Scratchpad unter
`…/scratchpad/w127/cec_ess.xlsx` (1 338 305 Byte). **Sie ist NICHT im Repository** — im
Repository steht nur die 23-Zeilen-Probe (Kapitel 9.3).

**Was drin ist.** Ein Blatt `Energy Storage System` mit **6 671 Zeilen**: 15
Erläuterungszeilen, **zwei** Kopfzeilen (Zeile 16 die Beschriftung, Zeile 17 die
Einheiten) und **6 654 Gerätezeilen** von **130 Herstellern**. Der Stand steht in
Zeile 2: *„Data has not changed since August 21, 2026"*.

Die 45 Spalten sind zum größten Teil Zulassungsverwaltung (UL 9540, UL 1741 SA/SB,
CSIP, Anti-Islanding, Zertifikatsdaten). **Sieben** davon sind fachlich:

| Spalte | Inhalt | belegt |
|---|---|---|
| `Manufacturer Name` | Hersteller | 100 % |
| `Model Number` | Modell (Spannungsvariante in `{…}`) | 100 % |
| `Technology` | Zellchemie | 100 % |
| `Nameplate Energy Capacity` [kWh] | Nennkapazität | **100 %** (6 654 von 6 654) |
| `Nameplate Power` [kW] | Nennleistung | **100 %** |
| `Maximum Continuous Discharge Rate4` [kW] | Dauer-Entladeleistung | meist gleich der Nennleistung |
| `Manufacturer Declared Roundtrip Efficiency` [%, AC‑AC] | Round-Trip-Wirkungsgrad | **1 103 (16,6 %)** — die übrigen 5 540 tragen „No Information Submitted" |

Zellchemien (gemessen, nicht geschätzt): Lithium-Eisen-Phosphat 4 238, Lithium-Ionen
2 386, „Lithium Iron" 23, Lithium-Titanat 4, Eisen-Redox-Flow 2, NMC 1.
Kapazitäten von 1 bis 10 032 kWh, Leistungen von 0,432 bis 4 904 kW — die Liste
reicht vom Heimspeicher bis zum Großspeicher.

**Eine Falle steckt in der Wirkungsgradspalte.** Sie ist mit „%" beschriftet, führt
aber neben 85, 93 und 97,5 auch **0,88** — dieselbe Größe, einmal in Prozent und einmal
als Bruch. Kleinstwert 0,88, Größtwert 97,5. Ein Import, der stur durch 100 teilt,
schriebe 0,0088 in den Katalog; einer, der stur übernimmt, schriebe 88. Beides weist die
Engine zurück (`SpeicherParameter` verlangt η_RT in (0…1]). Die Weiche muss deshalb
**wertabhängig** sein (Kapitel 2.2, geprüft in `StromspeicherImportTests`).

**Format.** Nur XLSX. Es gibt auf der Seite **keinen** CSV-Ausgang — der einzige
Verweis ist „Download Excel file". Damit unterscheidet sich diese Liste von den
CEC-Modul- und -Wechselrichterlisten, die EPOS-Plan schon liest: Jene holt EPOS-Plan aus
dem SAM-Bibliotheksverzeichnis des NREL als CSV, und **dort gibt es keine
Speicherliste** (Kapitel 1.3).

**Pflegerhythmus.** Fortlaufend; die Liste nennt ihren Stand selbst und führt je Gerät
`CEC Listing Date` und `Last Update`. Der Abruf ist ein einfaches GET ohne Anmeldung
und ohne Sitzungsschlüssel — ein automatischer Abruf wie bei
`CecWechselrichterDienst` (30-Tage-Zwischenspeicher) wäre technisch möglich.

**Eignung.** **Die einzige echte Geräteliste.** Sie füllt genau die drei Spalten, ohne
die ein Speicher nicht rechenbar ist (Bezeichner, Energie, Leistung) und bei jedem
sechsten Gerät auch den Wirkungsgrad. Sie führt dagegen **keine** Kosten, **keine**
Degradation, **keine** Zyklenzusage und **keinen** Standby-Verbrauch — das ist eine
Netzanschluss-Zulassungsliste, kein Datenblattarchiv.

---

### 1.3 NREL SAM Battery Library — **die Annahme des Auftrags hält nicht**

**Zugriffsbeleg.**

| Weg | Ergebnis |
|---|---|
| `https://api.github.com/repos/NREL/SAM/contents/deploy/libraries` | **HTTP 403** — „GitHub access to this repository is not enabled for this session" (Egress-Regel der Sitzung, keine Aussage über die Quelle) |
| `git clone --depth 1 --filter=blob:none --no-checkout https://github.com/NREL/SAM.git` | erfolgreich — das Verzeichnis ließ sich damit vollständig auflisten |
| `raw.githubusercontent.com/NREL/SAM/develop/deploy/libraries/BatteryStateful/*.json` | HTTP 200 je Datei |

**Der Befund.** Im Bibliotheksverzeichnis `deploy/libraries` liegen 13 CSV-Dateien und
zwei Unterverzeichnisse. Eine Speicherliste ist **nicht** darunter:

```
CEC Inverters.csv          389 761   ← liest EPOS-Plan bereits
CEC Modules.csv          6 274 198   ← liest EPOS-Plan bereits
Sandia Modules.csv         194 726
Wind Turbines.csv          164 527
SRCC Collectors.csv         42 778
… (Trog, Gezeiten, Wellen, TOD-Fahrpläne)
BatteryStateful/           ← 4 JSON-Dateien
```

`BatteryStateful/` enthält **vier** Dateien — `BatteryStateful_LFPGraphite.json`
(21 756 Byte), `_LeadAcid.json` (10 044), `_NMCGraphite.json` (2 192),
`_LMOLTO.json` (2 237). Ihr Inhalt ist **Zellphysik einer generischen Chemie**, kein
Gerät:

```
"ParamsCell": { "Qfull": 3.2 (Ah), "Qnom": 3.126, "Vnom": 3.342 (V),
                "Vfull": 4.2, "Vcut": 2.772, "resistance": 0.001155 (Ohm),
                "calendar_a": 0.00266, "calendar_b": -7280.0, …
                "cycling_matrix": [[DOD %, Zyklen, Restkapazität %], …] }
```

Es gibt dort **keinen Herstellernamen, kein Modell, keine Kilowattstunde und kein
Kilowatt** — die Systemgröße gibt in SAM der Anwender vor, die Bibliothek liefert nur
die Chemie darunter. **Die Formulierung des Auftrags („das CEC-Pendant … CSV-Dateien im
`Libraries`-Ordner") trifft für Batterien also nicht zu**; sie gilt für Module und
Wechselrichter, und nur für die.

**Lizenz.** BSD‑3‑Clause („Copyright (c) 2017, 2022 Alliance for Energy Innovation, LLC")
— Weitergabe wäre erlaubt.

**Eignung.** **Keine für den Katalogimport.** Ein einziger Wert wäre theoretisch
ableitbar: aus `cycling_matrix` ließe sich je Chemie die Zyklenzahl bis 80 %
Restkapazität lesen und als Vorbelegung für `Zyklen_Zugesichert` verwenden. Das ist
eine **Vorbelegung nach Chemie**, kein Import eines Geräts — es gehört, wenn überhaupt,
in Stufe S3 (Kapitel 6, Frage **Q6**).

---

### 1.4 TUM `simses` — Simulation of Scalable Energy Storage Systems

**Zugriffsbeleg.**

| Weg | Ergebnis |
|---|---|
| `https://pypi.org/pypi/simses/json` | HTTP 200, 34 530 Byte |
| `simses-2.1.1.tar.gz` | HTTP 200, **136 975 Byte**, hochgeladen **17.05.2026** |

**Was drin ist.** Ein aktiv gepflegtes Paket (34 Fassungen, zuletzt vor knapp vier
Monaten), BSD‑3‑Clause, „Chair of Electrical Energy Storage Technology | TUM School of
Engineering and Design". **Datendateien: fünf.**

```
src/simses/model/cell/data/CLFP_Sony_US26650_OCV.csv
src/simses/model/cell/data/CLFP_Sony_US26650_Rint.csv
src/simses/model/cell/data/CLFP_Sony_US26650_HystV.csv
src/simses/model/cell/data/CLFP_Sony_US26650_entropy.csv
src/simses/model/converter/data/sinamics_S120_efficiency.csv
```

Also die Messreihen **einer einzigen Zelle** (Sony US26650, LFP) und die Kennlinie
**eines** Umrichters. Die übrigen Zelltypen stehen als Python-Klassen im Quelltext —
zwei an der Zahl: `Samsung94AhNMC` und `SonyLFP`. Beispiel:

```python
electrical=ElectricalCellProperties(nominal_capacity=94.0,  # Ah
                                    nominal_voltage=3.68,   # V
                                    max_charge_rate=2.0)    # 1/h
def internal_resistance(self, state): return 0.819e-3       # Ohm
```

**Eignung.** **Keine für den Katalogimport.** `simses` ist ein Modellbaukasten für
Zell- und Systemphysik im Sekunden- bis Minutentakt; EPOS-Plan rechnet eine
Jahressimulation auf Stundenbasis mit einem einzigen Round-Trip-Wirkungsgrad. Es gibt
kein Gerät zu importieren. (Ein Exportskript nach Auftragspunkt 5 wäre technisch
machbar — es exportierte zwei Zellen. Deshalb wurde keines gebaut.)

---

## 2. Abbildung auf das Datenmodell

### 2.1 Die Zielspalten und was sie WIRKLICH bedeuten

Vor jeder Abbildung steht die Frage, in welcher Einheit der Rechenweg die Spalten liest.
Die Antworten stehen in `StromspeicherModel`, `StromspeicherSimCtrl.LeseParameter`,
`SpeicherEngine/SpeicherParameter` und `Allgemein/Hilfe/Berechnung/Stromspeicher.wiki` —
und sie sind an mehreren Stellen anders, als der Spaltenname vermuten lässt:

| Spalte | Einheit | Bedeutung im Rechenweg | Rückfall bei 0 |
|---|---|---|---|
| `Bezeichner` | Text | Schlüssel des Katalogs (`WHERE Bezeichner = ?`) | — |
| `Typ` | Text | Zellchemie, **Freitext**; Bestand: „Lithium-Ionen", „Lithium-Ionen-Akkus", „Lithium-Eisen-Phosphat" | — |
| `Leistung` | **kW** | gemeinsame Lade-/Entladeleistungsgrenze | **1 C** (`Leistung := Energie`) + Protokollhinweis |
| `Energie` | **kWh** | Nennkapazität; auf sie bezieht sich das SoC-Band der Variante | — |
| `Wirkungsgrad_RT` | **Bruch 0…1** | Round-Trip; die Engine setzt je Richtung **√η_RT** | **0,90** (`WIRKUNGSGRAD_RT_VORGABE`), ebenso bei > 1 |
| `Degradation` | **% je JAHR** | wird durch 100 geteilt → `DegradationProA`; geht in den degradierten Rentenbarwertfaktor | 0 (keine Degradation) |
| `Ladezustand` | **% der Nennkapazität** | **Start-SoC**, danach ins SoC-Band geklemmt (Entscheid AP0, 16.08.2026) | → SoC_min |
| `Zyklen_Zugesichert` | — | Zyklenbudget für Ampel und Hochrechnung | keine Ampel |
| `Modulkosten` | **EUR/kWh** | c_cap, **ohne** Umrechnung | 0 |
| `Leistungskosten` | **EUR/kW** | c_pow (leistungsgewichtet gemittelt) | 0 |
| `Investition_Fix` | **EUR** | I_fix (summiert) | 0 |
| `Verschleisskosten` | **EUR/(kWh·Zyklus)** | c_ver | **0,025** (`C_VER_VORGABE`) |
| `Standby_Verbrauch` | **W** | **Geräteparameter, KEIN Term des Dispatchs** — die Wiki sagt es wörtlich; wird nur summiert und im `StromspeicherLaufKontext` mitgeführt | 0 |

Zwei Punkte sind für den Import entscheidend:

* **`Wirkungsgrad_RT` ist ein Bruch, kein Prozentwert.** Die Engine wirft
  `ArgumentOutOfRangeException`, sobald er außerhalb (0…1] liegt.
* **0 heißt „nicht gepflegt", nicht „null".** Die Migration hat die Gerätespalten
  bewusst ohne Datenbank-Vorgabewert angelegt; die Leseseite ersetzt 0 durch die
  Fachvorgabe. Ein Import darf deshalb Felder, die seine Quelle nicht führt, **leer
  lassen** — genau das ist das vorgesehene Verhalten, und es ist besser als eine
  erfundene Zahl.

### 2.2 CEC Energy Storage System List → `Tab_Stromspeicher_STAMM`

| Quellspalte | EPOS-Feld | Umrechnung |
|---|---|---|
| `Manufacturer Name` + `Model Number` | `Bezeichner` | `Hersteller + ": " + Modell` — die Schreibweise der CEC-Modul- und -Wechselrichterlisten, aus der der Modulimport den Hersteller zurückgewinnt |
| `Technology` | `Typ` | Übersetzungstabelle (unten); Unbekanntes bleibt **im Original** stehen |
| `Nameplate Power` [kW] | `Leistung` | 1:1; ist sie 0, tritt `Maximum Continuous Discharge Rate` ein |
| `Nameplate Energy Capacity` [kWh] | `Energie` | 1:1 |
| `Manufacturer Declared Roundtrip Efficiency` | `Wirkungsgrad_RT` | **wertabhängige Weiche**: > 1 → ÷ 100; (0…1] → unverändert; sonst 0 |
| — | `Degradation`, `Ladezustand`, `Zyklen_Zugesichert`, `Standby_Verbrauch` | **bleiben 0** (führt die Liste nicht) |
| — | `Modulkosten`, `Leistungskosten`, `Investition_Fix`, `Verschleisskosten` | **bleiben 0** — und sind im Katalog ohnehin `AusschlussSpalten`, zählen also beim Dublettenvergleich nicht mit |

Zellchemie-Übersetzung (die fünf Texte, die die Liste am 07.09.2026 führt):

| CEC | EPOS `Typ` | Sätze der Liste |
|---|---|---|
| `Lithium Iron Phosphate`, `Lithium Iron` | `Lithium-Eisen-Phosphat` | 4 261 |
| `Lithium Ion` | `Lithium-Ionen` | 2 386 |
| `Lithium Titanate Oxide` | `Lithium-Titanat` | 4 |
| `Lithium Nickel Manganese Cobalt` | `Lithium-Nickel-Mangan-Kobalt` | 1 |
| `Iron Flow Battery` | `Eisen-Redox-Flow` | 2 |

**Was die Quelle sonst noch bietet und der Rechenweg heute nicht nutzt:** die
UL-Zertifizierungen, das CSIP-Profil, die Nennspannung [Vac], `PV DC Input Capability`
(ob das Gerät einen PV-Eingang hat — die AC/DC-Kopplung!) und die Anti-Islanding-Gruppen.
Von alledem hätte **eine** Größe fachlichen Wert: die **Kopplung AC/DC**, weil sie
darüber entscheidet, ob die Wandlerverluste doppelt gezählt werden. Der heutige
Rechenweg kennt den Unterschied nicht (er hat einen einzigen η_RT). Das wäre S3 —
und nur, wenn der Anwender es will (Frage **Q7**).

### 2.3 `bslib` → `Tab_Stromspeicher_STAMM`

| Quellspalte | EPOS-Feld | Umrechnung |
|---|---|---|
| `Manufacturer (PE)` | Hersteller im `Bezeichner` | — |
| `Model (PE)` (+ `Manufacturer (BAT)` `Model (BAT)`) | Modell im `Bezeichner` | Bei einem DC-System ist das Gerät das **Paar**: `PLENTICORE plus 5.5 / BYD Battery-Box H6.4` |
| `E_BAT_usable [kWh]` | `Energie` | 1:1 (bereits die **nutzbare** Kapazität) |
| `P_BAT2AC_out [W]` | `Leistung` | **÷ 1000** |
| `eta_BAT` [%] | `Wirkungsgrad_RT` | **÷ 100**. Dieselbe Konvention wie in EPOS: `bslib` rechnet `e_b0 + p·√η` beim Laden und `e_b0 + p/√η` beim Entladen — Zeile für Zeile das, was `SpeicherParameter.EtaCh`/`EtaDis` tun |
| `P_SYS_SOC0/1_AC/DC [W]` (vier Werte) | `Standby_Verbrauch` | **max(AC+DC voll, AC+DC leer)** — der ungünstigere Betriebszustand (2.4) |
| `Type [-coupled]` = `PVINV` | — | **Zeile wird übergangen**: ein PV-Wechselrichter ist kein Speicher |
| — | `Typ` | **bleibt leer** — `bslib` führt keine Zellchemie, weder in der CSV noch in der PerMod-Vorlage |
| — | Degradation, Ladezustand, Zyklen, alle vier Kostenfelder | bleiben 0 |

**Was `bslib` sonst noch bietet und der Rechenweg heute nicht nutzt:** die
**Wirkungsgradkennlinie der Leistungselektronik** als quadratisches Polynom je Pfad
(`AC2BAT_a/b/c_in|out`, `BAT2AC_…`, `PV2BAT_…`, `PV2AC_…`), die Totzeit `t_DEAD` und
die Einschwingzeit `t_SETTLING` der Regelung sowie die Regelabweichungen
(`P_*_DEV_IMPORT/EXPORT`). Das ist genau der Apparat, den `Konzept_Wechselrichter…`
für die PV-Seite bereits eingeführt hat (Stützstellen `Eta05…Eta100`). Für die
Speicherseite wäre es **S3** — es setzt einen teillastabhängigen Wirkungsgrad im
Dispatch voraus, den es heute nicht gibt (Frage **Q7**).

### 2.4 Der Standby-Verbrauch — warum das Maximum und nicht der Mittelwert

`bslib` misst je Zustand und Seite; `Tab_Stromspeicher_STAMM` führt **eine** Zahl.
Genommen wird die Summe beider Seiten im **ungünstigeren** Zustand:

| System | voll (SOC1 AC + DC) | leer (SOC0 AC + DC) | → `Standby_Verbrauch` |
|---|---|---|---|
| SG1 generisch | 0,0 | 0,0 | **0,0 W** |
| S2 Siemens (AC) | 14,9 + 0,1 = 15,0 | 12,1 + 0,0 = 12,1 | **15,0 W** |
| S3 KOSTAL 5.5 (DC) | 0,0 + 0,15 = 0,15 | 4,47 + 4,56 = 9,03 | **9,03 W** |
| S4 KOSTAL 10 (DC) | 0,0 + 0,15 = 0,15 | 4,47 + 4,71 = 9,18 | **9,18 W** |

Bei AC-Systemen ist der volle Zustand der teurere, bei DC-Systemen der leere — ein
Mittelwert verschwiege gerade den Fall, den eine Wirtschaftlichkeitsrechnung sehen muss.
Das ist eine **Setzung des Imports**, keine Aussage der Quelle; sie steht deshalb als
Frage **Q4** im Kapitel 7.

---

## 3. Bewertung und Empfehlung

**Empfehlung: beide brauchbaren Quellen, in dieser Reihenfolge — CEC zuerst, `bslib`
gleich mit.**

1. **CEC Energy Storage System List als Hauptquelle.** Sie ist der einzige Weg, den
   Speicherkatalog von fünf handgepflegten Sätzen auf einen echten Gerätebestand zu
   bringen. Sie füllt genau die Spalten, ohne die nicht gerechnet werden kann, sie ist
   frei abrufbar, sie wird laufend gepflegt, und EPOS-Plan liest bereits zwei
   Schwesterlisten derselben Behörde — Anwender, Bedienweg und Prüfmuster sind
   eingeführt. Ihre 6 654 Zeilen sind zugleich ihre Schwäche: **ohne Filter ist die
   Liste unbenutzbar** (Kapitel 5, Mockup).
2. **`bslib` als zweite Quelle im selben Dialog.** Vier Sätze rechtfertigen keinen
   eigenen Menüpunkt, aber sie sind die einzigen mit gemessenem Wirkungsgrad UND
   Standby, und ihre Lizenz (CC BY 4.0) erlaubt die Mitlieferung. Sie sind damit der
   natürliche Inhalt einer **Auslieferungsdatei** (S2) und der Bezugspunkt, an dem sich
   ein CEC-Satz messen lässt.
3. **NREL SAM und TUM `simses` NICHT importieren.** Beide sind Zellphysik; es gibt in
   `Tab_Stromspeicher_STAMM` keine Spalte, die sie füllen könnten. Aus SAM ließe sich
   allenfalls eine Zyklenzahl je Chemie ableiten (Frage **Q6**) — das ist eine
   Vorbelegung, kein Import.

**Die unangenehme Nebenwirkung, die der Anwender kennen muss:** Ein CEC-Import bringt
Geräte **ohne Kosten**. Ein Speicher ohne `Modulkosten` rechnet in der
Wirtschaftlichkeit mit 0 EUR/kWh — er ist dann gratis und damit immer die beste Lösung.
Das ist der einzige echte Fallstrick dieses Imports und der Grund für Frage **Q3**
(Vorbelegung der Kosten) und für die Ampel in Stufe S1.

---

## 4. Wo der Import in der Oberfläche sitzt

**Administration → Daten & Import → „Stromspeicher (CEC, bslib)…"** — als **siebter**
Punkt neben den sechs vorhandenen (`EPOS.UI/Bausteine/Menuetabelle.cs`, Kopf
`MenuItem_DatImport`, Kennung `MenuItem_SP_Import`).

> **Er ist NACH dem Umbau eingehängt worden.** Der Anwenderentscheid **W16c‑E‑7** vom
> selben Tag ordnet die Rubrik neu: Modul- und Wechselrichterimport stehen seither unter
> einem Zwischenknoten „Photovoltaik". Der neue Punkt steht **hinter** diesem Knoten und
> **vor** „Import Solarkollektoren" — Stromspeicher und Photovoltaik gehören zur selben
> Anlage. Von den sechs Punkten sind damit vier auf der ersten Ebene, zwei unter dem
> Knoten; der siebte ist der Speicher.

Wirt ist der vorhandene **`KatalogImportDialog.razor`** mit einer neuen Ausprägung
`KatalogImportArt.Stromspeicher` in `KatalogImportProfil`. Damit erbt der Speicherimport
ohne eine Zeile Neuschrift: Dateiwähler, Fortschritt mit Abbruch, Suchfilter über
Hersteller und Zahlenwert, Detailfeldleiste, **Dublettenvorprüfung mit Konfliktdialog**
(anlegen / überschreiben / umbenennen / überspringen) und die Sammelmeldung mit den
fünf Zählern. Der Katalogschlüssel `STROMSPEICHER` ist in `KatalogRegistry` **bereits
eingetragen** (mit den vier Kostenspalten als `AusschlussSpalten`) — die Vorprüfung
funktioniert also sofort.

---

## 5. Mockup der Maske (Textskizze)

Die Maske ist der `KatalogImportDialog` in seiner Speicher-Ausprägung. Zwei Dinge
unterscheiden sie von den sechs vorhandenen: die **Quellenwahl** oben (zwei Formate
statt eines) und der **Zahlenfilter auf zwei Größen** (kWh und kW), weil eine Liste
von 1 bis 10 032 kWh sonst nicht bedienbar ist.

```
┌───────────────────────────────────────────────────────────────────────────────────┐
│  Stromspeicher einlesen                                                      [×]  │
├───────────────────────────────────────────────────────────────────────────────────┤
│  Quelle   ( ) CEC Energy Storage System List (*.xlsx, *.csv)                       │
│           ( ) bslib – Battery Storage Library (bslib_database.csv)                 │
│                                                                                    │
│  Datei    [ …\Energy_Storage_System_List_Data_ADA.xlsx        ] [ Durchsuchen… ]   │
│           Stand der Liste: 21.08.2026 · 6 654 Geräte · 130 Hersteller              │
│                                                                                    │
│  Filter   Hersteller [ alle              ▾]   Zellchemie [ alle          ▾]        │
│           Kapazität  [    5 ] … [    30 ] kWh   Leistung [   0 ] … [  20 ] kW      │
│                                                          [ Filter anwenden ]       │
├───────────────────────────────────────────────────────────────────────────────────┤
│ ☐ │ Bezeichner                                │ Chemie │  kWh │   kW │ η_RT │ !   │
│───┼───────────────────────────────────────────┼────────┼──────┼──────┼──────┼─────│
│ ☑ │ Alpha ESS Co., Ltd.: SMILE-SP7.6 {8.2kWh} │ LFP    │  8,2 │  7,6 │  —   │ ●   │
│ ☑ │ BYD: Battery-Box Premium HVS 10.2         │ LFP    │ 10,2 │  9,0 │  —   │ ●   │
│ ☐ │ Dyness …: AMOR-9.6/9.9                    │ Li-Ion │  9,9 │  9,6 │ 0,93 │ ●   │
│ ☐ │ Villara …: VES20BC22S10P-S12K             │ LTO    │ 11,5 │  8,0 │ 0,92 │ ●   │
│   │                                       … 6 650 weitere (gefiltert: 38)          │
├───────────────────────────────────────────────────────────────────────────────────┤
│  Ausgewählt: „Dyness Digital Energy Technology Co., LTD.: AMOR-9.6/9.9"            │
│                                                                                    │
│  Bezeichner        [ Dyness: AMOR-9.6/9.9                       ]  ← änderbar      │
│  Hersteller          Dyness Digital Energy Technology Co., LTD.                    │
│  Zellchemie          Lithium-Ionen            Kapazität     9,9 kWh                │
│  Leistung            9,6 kW                   Round-Trip    0,93                   │
│  Standby             — (führt die Liste nicht)                                     │
│                                                                                    │
│  ⚠ Die CEC-Liste führt keine Kosten, keine Degradation und keine Zyklenzusage.     │
│    Diese Felder bleiben leer und sind vor der Wirtschaftlichkeitsrechnung           │
│    in der Verwaltung „Stromspeicher" zu ergänzen.                                   │
├───────────────────────────────────────────────────────────────────────────────────┤
│  4 von 6 654 markiert            [ Alle markieren ] [ Abbrechen ] [ Übernehmen ]   │
└───────────────────────────────────────────────────────────────────────────────────┘
```

Die Ampel `!` je Zeile ist dieselbe wie beim Wechselrichterimport:

| Ampel | Bedeutung |
|---|---|
| ● grün | Energie > 0, Leistung > 0, η_RT in (0…1] — vollständig rechenbar |
| ● gelb | η_RT fehlt (fällt auf 0,90) **oder** Leistung fehlt (fällt auf 1 C) **oder** Kosten fehlen |
| ● rot | Energie ≤ 0 — der Satz wird gar nicht erst angeboten |

Nach der Übernahme kommt die bekannte Sammelmeldung des `KatalogImportAblauf`:
„*n* gespeichert, *n* überschrieben, *n* umbenannt, *n* Dubletten, *n* Fehler".

---

## 6. Vorschlag in drei Stufen

### Stufe S1 — Import als siebte Ausprägung (ohne jede Rechenwirkung)

1. `KatalogImportArt.Stromspeicher` in `KatalogImportProfil` mit den Detailfeldern
   Bezeichner, Hersteller, Zellchemie, Kapazität, Leistung, Round-Trip, Standby.
2. `StromspeicherImportSatz` (Kapitel 9) von `KatalogImportSatz` ableiten und um
   `Anlegen`/`Ueberschreiben` ergänzen; dafür bekommt `StromspeicherStammCtrl` die
   beiden Methoden `ImportUebernehmen` und `UpdateImport` — wortgleich zu
   `HeizkesselStammCtrl`.
3. Die zwei Zerleger an `KatalogImportAblauf.Lesen` hängen (heute liest der Ablauf nur
   VDI-3805-Dateien; er bekommt eine zweite Lesart für die tabellarischen Quellen).
4. Der Dialog: Quellenwahl, zweiter Zahlenfilter, Ampel, Kostenhinweis (Kapitel 5).
5. Menüpunkt „Stromspeicher (CEC, bslib)…" **nach** dem Umbau W16c‑E‑7.
6. Texte in `MyResource.Resource.*` (deutsch und englisch), Designer über
   `Werkzeuge/ResourceDesigner`.

**Kein Migrationsschritt.** Der Katalog hat alle Spalten seit Schritt 11a; der Import
schreibt nur in vorhandene.

### Stufe S2 — Auslieferungsdatei (optional, Lizenzentscheid nötig)

`VDI-3805-Daten/Stromspeicher/bslib_database.csv` samt
`LIESMICH_bslib_Stromspeicher.md` nach dem Muster von `LIESMICH_CEC_Inverters.md`
(Herkunft, DOI, Lizenz CC BY 4.0, Abrufdatum, Zeilenzahl, was EPOS-Plan daraus macht).
Aufnahme in die Setup-Komponente „Herstellerdaten (VDI 3805, CEC)".

**Die CEC-Liste gehört dort NICHT hin — jedenfalls nicht ohne Prüfung.** Die
Nutzungsbedingungen der Energy Commission (abgerufen am 07.09.2026,
`https://www.energy.ca.gov/conditions-of-use`) sagen wörtlich:

> „*Most of these materials and information were generated, compiled, or assembled at
> public expense and are free for public use consistent with the Public Records Act …
> provided the Energy Commission is credited …* **Use or modification of these
> materials or information for commercial or profit-making purposes is prohibited** …"

Die Modul- und Wechselrichterlisten, die EPOS-Plan heute mitliefert, kommen **nicht**
von dort, sondern aus dem NREL-SAM-Bestand unter BSD‑3‑Clause — ein anderer Rechtsweg.
Für die Speicherliste gibt es diesen Weg nicht (Kapitel 1.3). **Empfehlung: nicht
mitliefern**, sondern im Dialog den Abrufknopf anbieten (Frage **Q2**).

### Stufe S3 — Erweiterte Parameter im Rechenweg (optional, später)

Was die Quellen bieten und der Rechenweg heute nicht kennt, in der Reihenfolge des
Nutzens:

1. **Teillastabhängiger Wirkungsgrad** statt eines festen η_RT — `bslib` liefert die
   Polynome, die CEC-Liste nicht. Wirkt auf jede Stunde der Simulation und ist damit
   ein Eingriff in den Rechenweg mit neuer Referenzbasis.
2. **AC/DC-Kopplung** (`PV DC Input Capability` der CEC-Liste, `Type [-coupled]` bei
   `bslib`). Bei einem DC-System entfällt eine Wandlung zwischen PV und Speicher —
   heute rechnet EPOS-Plan beide gleich.
3. **Standby im Dispatch.** Der Wert steht im Katalog und wird gelesen, ist aber
   ausdrücklich „kein Term des Dispatchs" (Wiki). Ein System mit 15 W Dauerlast
   verbraucht über ein Jahr **131 kWh** (15 W · 8 760 h) — bei einem 8,85-kWh-Speicher
   sind das rund 15 volle Ladungen, die heute in keiner Bilanz auftauchen.
4. **Zyklenzahl je Zellchemie** aus der SAM-`cycling_matrix` als Vorbelegung von
   `Zyklen_Zugesichert`.

**Alle vier sind Rechenwegänderungen und brauchen eine neue Referenzbasis.** Keine
davon ist Voraussetzung für S1.

### Reihenfolge

S1 zuerst und allein. S2 erst nach dem Lizenzentscheid (**Q2**). S3 nur auf
ausdrücklichen Wunsch, einzeln, je mit eigener Referenzbasis.

---

## 7. Fragen an den Anwender

**Q1 — Welche Quellen sollen in Stufe S1 in den Dialog?**
*Empfehlung: beide (CEC und `bslib`) in EINEM Dialog mit Quellenwahl.* Die CEC-Liste
bringt den Bestand, `bslib` die gemessenen Wirkungsgrade und den einzigen
Standby-Verbrauch. Zwei Menüpunkte für vier Datensätze wären unverhältnismäßig; NREL SAM
und `simses` bleiben außen vor, weil sie keine Geräte führen.

**Q2 — Soll die CEC-Speicherliste mitgeliefert werden?**
*Empfehlung: nein.* Die Nutzungsbedingungen der Energy Commission untersagen die
kommerzielle Nutzung ihrer Materialien ausdrücklich (Kapitel 6, S2), und anders als bei
Modulen und Wechselrichtern gibt es keinen NREL-Umweg unter BSD‑3‑Clause. Stattdessen:
`bslib` mitliefern (CC BY 4.0, unbedenklich) und für die CEC-Liste im Dialog einen
Abrufknopf anbieten, der sie beim Anwender ablegt — der Anwender lädt sie damit selbst,
so wie er es im Browser auch täte.

**Q3 — Womit werden die Kosten vorbelegt, die keine Quelle liefert?**
*Empfehlung: mit NICHTS — die Felder bleiben leer (0), und die Maske sagt es.* Das ist
das eingeführte Verhalten (`WIRKUNGSGRAD_RT_VORGABE`, `C_VER_VORGABE`, der
Heizkesselimport lässt Wartungskosten ebenso stehen), und eine erfundene Zahl in einer
Wirtschaftlichkeitsrechnung ist schlimmer als eine fehlende. **Wenn** eine Vorbelegung
gewünscht ist, dann sichtbar in der Maske als Eingabefeld „Modulkosten für alle
markierten Sätze: ___ EUR/kWh" — nicht still im Zerleger.

**Q4 — Welcher der vier `bslib`-Standby-Werte wird `Standby_Verbrauch`?**
*Empfehlung: das Maximum aus „voll" und „leer", je AC + DC* (Kapitel 2.4). Es ist der
Wert, den ein Datenblatt „Standby" nennen würde, und der einzige, der bei AC- und
DC-Systemen gleichermaßen den ungünstigen Fall trifft.

**Q5 — Wie soll der Bezeichner aussehen?**
*Empfehlung: `Hersteller: Modell`* — die Schreibweise der CEC-Listen, aus der der
Modulimport den Hersteller zurückgewinnt. Nachteil: Manche CEC-Herstellernamen sind
lang („Dyness Digital Energy Technology Co., LTD."). Alternative wäre ein gekürzter
Firmenname; das erforderte eine Kürzungstabelle, die gepflegt werden muss. Der
Bezeichner bleibt in der Maske ohnehin änderbar.

**Q6 — Soll `Zyklen_Zugesichert` nach Zellchemie vorbelegt werden (aus SAM)?**
*Empfehlung: nein, nicht in S1.* Die Zyklenzusage ist eine **Garantieaussage des
Herstellers**, keine Eigenschaft der Chemie; sie aus einer generischen Alterungsmatrix
abzuleiten und im Katalog als „zugesichert" zu führen, wäre irreführend — gerade weil
die Ampel und die Zyklenhochrechnung daran hängen.

**Q7 — Soll Stufe S3 überhaupt verfolgt werden?**
*Empfehlung: nur Punkt 3 (Standby im Dispatch), und auch der später.* Er ist der
einzige der vier, für den die Daten schon im Katalog stehen; die anderen drei
verlangen zusätzlich neue Spalten UND eine neue Referenzbasis. Vor allem aber:
Solange die Kosten von Hand kommen (Q3), ist der Wirkungsgrad nicht die größte
Unsicherheit der Rechnung.

**Q8 — Soll der Dialog die CEC-Liste selbst abrufen können (wie `CecWechselrichterDienst`)?**
*Empfehlung: ja, in S1.5 — nach dem Import selbst, nicht davor.* Der Abruf ist ein
einfaches GET ohne Anmeldung (Kapitel 1.2, belegt), und der Apparat steht bereits:
Rückfallkette, 45-Sekunden-Grenze, 30-Tage-Zwischenspeicher, Fortschritt mit Abbruch.
Er wäre eine Abschrift von `CecWechselrichterDienst`, kein Neubau. Erst das macht
Frage Q2 wirklich unschädlich.

---

## 8. Größenordnungen

| | |
|---|---|
| Katalogsätze heute (Testdatenbank) | **5** |
| CEC-Liste | **6 654** Geräte, 130 Hersteller, 1,3 MB XLSX |
| davon mit Round-Trip-Wirkungsgrad | 1 103 (16,6 %) |
| davon mit Kosten | **0** |
| `bslib` | **4** Speicher (+ 3 übergangene Zeilen), 2,7 kB CSV |
| Kapazitätsspanne der CEC-Liste | 1 … 10 032 kWh |
| Leistungsspanne der CEC-Liste | 0,432 … 4 904 kW |

Zum Vergleich: Die Modulliste, die EPOS-Plan bereits einliest, hat 20 746 Zeilen, die
Wechselrichterliste 2 343. Ein virtualisiertes Raster ist also vorhanden und erprobt;
**6 654 Zeilen sind kein neues Problem**, wohl aber ohne Filter unbedienbar.

---

## 9. Was in diesem Schritt GEBAUT ist

Gebaut ist der **Rechenkern-Zerleger** — Lesen, Erkennen, Umrechnen — samt Proben und
Prüfung. **Nicht gebaut** sind Maske, Menüpunkt und Migrationsschritt; sie kommen mit
Stufe S1 nach dem Entscheid.

### 9.1 Die drei Kerndateien

| Datei | Inhalt |
|---|---|
| `EPOS.Kern/Allgemein/Import/Stromspeicher/StromspeicherImportSatz.cs` | Der gelesene Satz in EPOS-Einheiten, `NachModell()` → `StromspeicherModel`, `Vergleichswerte()` für die Dublettenvorprüfung; dazu die geteilten Umrechnungen: Zellchemietabelle, Wirkungsgradweiche, kulturunabhängiges Zahlenlesen, Kopfzeilennormalisierung, UTF‑8/ANSI-Weiche, CSV-Zerlegung |
| `…/CecSpeicherImport.cs` | Die CEC-Liste — **XLSX über ClosedXML** (die Bibliothek, mit der `GanglinienDatei` schon Excel liest) **und** CSV; Kopfzeilensuche über die Pflichtspalten, Stand der Liste, Herstellerliste |
| `…/BslibImport.cs` | Die `bslib_database.csv`; übergangene Zeilen werden **benannt** (`Uebergangen`), nicht stillschweigend verloren |

Dazu vier neue Persistenzwerte in `DbWerte` (`SP_TYP_LITHIUM_EISEN_PHOSPHAT`,
`SP_TYP_LITHIUM_TITANAT`, `SP_TYP_LITHIUM_NMC`, `SP_TYP_EISEN_REDOX_FLOW`) — der erste
davon steht im Bestand längst in der Datenbank, stand aber nirgends als Konstante.

**Der Kern kennt keine Anzeigetexte.** Jede Rückmeldung ist ein Schlüssel mit
Platzhalterwerten (`SpeicherImportMeldung`): `SPIMP_MSG_GELADEN`,
`SPIMP_MSG_KOPFZEILE`, `SPIMP_MSG_DATEI_FEHLT`, `SPIMP_MSG_LEER`,
`SPIMP_MSG_KEINE_SAETZE`, `SPIMP_MSG_FORMAT_ALT`, `SPIMP_MSG_FEHLER` — die
Ressourcentexte legt Stufe S1 an, wenn es eine Maske gibt, die sie zeigt.

### 9.2 Warum kein `KatalogImportSatz`, sondern ein eigener Satz

`KatalogImportSatz` verlangt `Anlegen` und `Ueberschreiben` und damit
`ImportUebernehmen`/`UpdateImport` am `StromspeicherStammCtrl` — also Schreibzugriff auf
den Katalog. Das ist Stufe S1 und gehört zur Maske. Der Zerleger dieses Schritts liest
und rechnet um; er schreibt nichts. `NachModell()` und `Vergleichswerte()` sind bereits
die Hälften, die S1 nur noch einhängen muss.

### 9.3 Proben und Prüfung

| Probe | Inhalt |
|---|---|
| `Referenzlaeufe/Importproben/stromspeicher_cec_ess_23.csv` | **Echte Zeilen der CEC-Liste**, unverändert: Titel- und Standzeile, die zwei Kopfzeilen und **23 Geräte** — bewusst über die ganze Liste gestreut, damit alle fünf Zellchemien, beide Wirkungsgrad-Schreibweisen (85 % und 0,88) und Geräte ohne Wirkungsgrad vorkommen (11 902 Byte) |
| `Referenzlaeufe/Importproben/stromspeicher_bslib_7.csv` | Die **vollständige** `bslib_database.csv`, unverändert (2 729 Byte) |
| Gegenprobe | `cec_wechselrichter_21.csv` — die vorhandene Wechselrichterprobe, über Kreuz gelesen |

`EPOS.Kern.Tests/StromspeicherImportTests.cs` — **28 Prüffälle**, Kultur auf `de-DE`
gepinnt: Zahl der Sätze, ein Satz je Quelle feldgenau, beide Wirkungsgrad-Schreibweisen,
die Zellchemieübersetzung, die drei `bslib`-Umrechnungen, das DC-Paar,
Mappe-gegen-CSV (dieselbe Probe als echte Arbeitsmappe geschrieben und zurückgelesen)
und die Gegenproben über Kreuz.

**Der Nachweis an der VOLLEN Datei.** Die echte Mappe (1 338 305 Byte) wurde einmal
durch den Zerleger geschickt:

```
Erfolg:     True / SPIMP_MSG_GELADEN (6654)
Kopfzeile:  16            Stand: Data has not changed since August 21, 2026
Sätze:      6654          Hersteller: 130
mit η_RT:   1103          ohne Leistung: 0     η außerhalb (0…1]: 0
Typen:      Lithium-Eisen-Phosphat 4261 · Lithium-Ionen 2386 · Lithium-Titanat 4
            · Eisen-Redox-Flow 2 · Lithium-Nickel-Mangan-Kobalt 1
Kapazität:  1 … 10 032 kWh        Leistung: 0,432 … 4 904 kW
```

Die Originaldatei bleibt im Scratchpad
(`…/scratchpad/w127/cec_ess.xlsx`, 1 338 305 Byte) und **nicht** im Repository — bis
Frage Q2 entschieden ist, gehört sie dort auch nicht hin.

### 9.4 Was der Rechenweg NICHT merkt

Nichts. Es gibt keinen Aufrufer: `CecSpeicherImport` und `BslibImport` werden von
keiner Simulations-, Berichts- oder Wirtschaftlichkeitsstelle berührt. Der Referenzlauf
1030/1007/1017/1045 gegen `Referenzlaeufe/2026-09-06_R3_Straenge` ist **byte-gleich**.

---

## 10. Entscheide

**Der Anwender hat am 07.09.2026 auf alle acht Fragen mit „Empfehlung" geantwortet.**
Damit gilt je Frage der Vorschlag aus Kapitel 7 im Wortlaut; die Zeile „Umgesetzt in"
nennt die Stelle, an der er steht.

| Kennung | Frage | Entscheid (07.09.2026) | Umgesetzt in |
|---|---|---|---|
| W13‑E‑2‑Q1 | Welche Quellen in S1? | **beide, in EINEM Dialog** — CEC und `bslib` als fünfte Ausprägung `KatalogImportArt.Stromspeicher` des `KatalogImportDialog`, nicht als zwei Menüpunkte | `KatalogImportProfil.Finde`, `KatalogImportAblauf.LiesStromspeicher` |
| W13‑E‑2‑Q2 | CEC-Liste mitliefern? | **nein** — die Nutzungsbedingungen der Energy Commission untersagen die kommerzielle Nutzung, und es gibt keinen NREL-Umweg unter BSD‑3‑Clause. Statt dessen ein **Abrufknopf** im Dialog. **`bslib` wird mitgeliefert** (CC BY 4.0, 2 729 Byte) samt `LIESMICH_bslib.md` | `VDI-3805-Daten/Stromspeicher/`, `CecSpeicherDienst` |
| W13‑E‑2‑Q3 | Kostenvorbelegung? | **keine** — `Modulkosten`, `Leistungskosten`, `Investition_Fix` und `Verschleisskosten` bleiben 0, ebenso Degradation und Ladezustand. Die Maske sagt es in einer Herleitungszeile | `KatalogImportProfil.Hinweis` (`IMP_KAT_HINWEIS_KOSTEN`), `StromspeicherImportSatz.NachModell` |
| W13‑E‑2‑Q4 | Welcher Standby-Wert? | **max(voll, leer), je AC + DC** — der ungünstigere Betriebszustand (Kapitel 2.4) | `BslibImport.Standby` |
| W13‑E‑2‑Q5 | Schreibweise des Bezeichners? | **`Hersteller: Modell`** — die Schreibweise der CEC-Listen; in der Maske änderbar wie bei den vier VDI-Ausprägungen | `StromspeicherImportSatz.Bezeichner` |
| W13‑E‑2‑Q6 | Zyklen nach Chemie vorbelegen? | **nein** — eine Zyklenzusage ist eine Garantieaussage des Herstellers, keine Eigenschaft der Chemie | — (nichts gebaut, mit Absicht) |
| W13‑E‑2‑Q7 | Stufe S3 verfolgen? | **nicht jetzt** — sie bleibt als spätere Stufe im Konzept stehen (Kapitel 6, S3); von den vier Punkten hat nur „Standby im Dispatch" seine Daten bereits im Katalog | — (Kapitel 6 unverändert) |
| W13‑E‑2‑Q8 | Abrufknopf für die CEC-Liste? | **ja, in S1** — er macht Q2 erst unschädlich: ein GET ohne Anmeldung, 45 Sekunden Grenze, 30-Tage-Zwischenspeicher, Fortschritt mit Abbruch | `CecSpeicherDienst` |

### Was Stufe S1 wirklich geworden ist

Umgesetzt am **07.09.2026**, Präfix `W13-E-2:`. Kein Migrationsschritt — der Katalog hat
alle Spalten seit Schritt 11a; der Import schreibt nur in vorhandene.

| Teil | Wo |
|---|---|
| Die fünfte Ausprägung | `KatalogImportArt.Stromspeicher` mit `Quellen`, `Listenspalten`, `Zweitfilter`, `HerstellerFilter` und `Hinweis` — fünf Profilteile, die bei den vier VDI-Ausprägungen leer bleiben |
| Der Lesezweig | `KatalogImportAblauf.Lesen(pfad, melder, abbruch, **quelle**)` → `CecSpeicherImport` bzw. `BslibImport` → `StromspeicherKatalogSatz`. **Der Quellschlüssel wählt den Zerleger, nicht die Dateiendung** — beide Quellen kommen als CSV |
| Der Schreibweg | `StromspeicherStammCtrl.ImportUebernehmen` (transaktional, mit Dublettensperre) und `.UpdateImport` (frischt Typ, Leistung, Energie, η_RT und Standby auf; Bezeichner, Kosten, Degradation und Zyklen bleiben stehen) |
| Der Netzabruf | `EPOS.Kern/Allgemein/Import/CEC/CecSpeicherDienst.cs` nach dem Muster von `CecWechselrichterDienst`, mit einem einlegbaren `HttpMessageHandler` — deshalb **ohne Netz prüfbar** |
| Die Maske | `KatalogImportDialog` mit drei Quellknöpfen statt des Dateiwählers, Herstellerklappliste, zwei Zahlenbereichen (kWh und kW), sieben Listenspalten und der Herleitungszeile zu den Kosten. **Mehrfachwahl, Doppelklick und Konfliktdialog kommen aus dem Wirt** (W6‑E‑5) — dafür war nichts zu bauen |
| Die Hülle | `WindowsFormsApplication1/Views/Import/KatalogImportHuelle.cs` — sie beschafft die Datei für die zwei Quellen ohne Wähler (Netzabruf; Auslieferungsdatei mit Rückfall auf den Wähler) und öffnet das Fenster in 1 180 × 700 statt 900 × 640 |
| Der Menüpunkt | `MenuItem_SP_Import` („Stromspeicher (CEC, bslib)…" / „Battery storage (CEC, bslib)…") in Administration ▸ Daten & Import, **hinter** dem Knoten „Photovoltaik" und **vor** „Import Solarkollektoren" |
| Die Auslieferung | `VDI-3805-Daten/Stromspeicher/bslib_database.csv` (2 729 Byte, byte-gleich zur Importprobe) + `LIESMICH_bslib.md`. **`Setup/EPOS-Plan.iss` bleibt unverändert** — die Komponente `herstellerdaten` liefert `VDI-3805-Daten\*` mit `recursesubdirs createallsubdirs`, ein neuer Unterordner reist von selbst mit |
| Der Nachweis | `EPOS.Kern.Tests/StromspeicherUebernahmeTests` (23 Fälle: Profil, beide Lesezweige über Kreuz, der Netzabruf mit gestelltem Handler in fünf Lagen, Anlegen/Überschreiben/Dublette/Schreibschutz gegen die Testdatenbank, die Auslieferungsdatei) und 11 neue bunit-Fälle in `EPOS.UI.Tests/Dialoge/KatalogImportDialogTests` |

**Der Rechenweg ist unberührt.** Es gibt keinen Aufrufer der Zerleger in Simulation,
Bericht oder Wirtschaftlichkeit; der Referenzlauf 1030/1007/1017/1045 gegen
`Referenzlaeufe/2026-09-06_R3_Straenge` ist byte-gleich.

---

## Anhang A — Was dieses Papier nicht behandelt

* **Die Verwaltung „Stromspeicher"** (`Form_AdminStromspeicher` bzw. ihre
  Razor-Nachfolge). Sie ist vorhanden und bleibt der Ort, an dem Kosten, Degradation
  und Zyklenzusage von Hand ergänzt werden.
* **Die Speichervariante** (`Tab_StromspeicherVariante`) mit SoC-Band, Betriebsart,
  Berechnungsart, Zins und Nutzungsdauer. Sie hängt am Projekt, nicht am Gerät; kein
  Import berührt sie.
* **Den Rechenweg selbst.** Er steht in
  `EPOS.Kern/Allgemein/Hilfe/Berechnung/Stromspeicher.wiki` und im
  `Umsetzungskonzept_Stromspeicher_EPOS-Plan.md` und bleibt in diesem Schritt
  unangetastet.
* **Die Dublettenprüfung.** Sie ist in `Konzept_Dublettenpruefung_Import_EPOS-Plan.md`
  beschrieben und über `KatalogRegistry` für `STROMSPEICHER` bereits parametriert.
