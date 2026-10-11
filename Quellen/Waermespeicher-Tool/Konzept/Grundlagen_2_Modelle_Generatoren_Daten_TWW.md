I have gathered comprehensive material. Here is the report.

---

# Fachbericht: Trinkwarmwasser-Zapfprofile (TWW/DHW) — Modelle, Generatoren, Bibliotheken und Messdaten

**Stand: Juli 2026** · Recherche für INEKON (Energieplanungssoftware, Python, kommerzieller Vertrieb)

---

## 0. Executive Summary — die Kurzfassung

**Empfohlener Kern-Stack (alles MIT-lizenziert, kommerziell nutzbar):**

1. **OpenDHW** (RWTH-EBC) als **Zapfprofil-Generator** — Python-Reimplementierung von DHWcalc, MIT-Lizenz, auf PyPI, **aktiv gepflegt (v0.2.8, 19.02.2026)**, seit v0.2.8 mit **10 Nichtwohngebäude-Typen**. Das ist die mit Abstand beste Passung zur Aufgabenstellung.
2. **demandlib** (oemof) als **VDI-4655-Backend** für die deutsche Normwelt — enthält die VDI-4655-Typtag-Tabellen inkl. **separatem TWW-Kanal (`Q_TWW_a` / `F_TWW_TT`)** in 1-min-Auflösung, MIT-Lizenz, aktiv (Juli 2026).
3. **Eigene Gleichzeitigkeits-/Diversitätsschicht** — das ist der Punkt, den **keines** der Tools befriedigend löst und wo Ihr bekannter 2,4×-Befund herkommt (Abschnitt 8/9).
4. **ASHRAE/DOE-Schedules + DIN EN 12831-3** für Nichtwohngebäude-Nutzungsarten, die OpenDHW nicht abdeckt.

**Wichtigste Warnungen:**
- **StROBe (KU Leuven) hat KEINE Lizenzdatei** im Repository → kommerziell **nicht verwendbar** ohne schriftliche Klärung.
- **DHWcalc selbst** (Windows-EXE, Uni Kassel) hat **keine publizierten Lizenzbedingungen** → nicht in ein verkauftes Produkt einbetten; nur als Referenz/Validierungsquelle.
- **pysimdeum** deckt **NUR Wohngebäude** ab (kein Hotel/Büro/Pflege) und ist **EUPL-1.2** (Copyleft-artig, Achtung bei Verlinkung).
- **VDI 4655** selbst ist urheberrechtlich geschützt (VDI); dass demandlib die Tabellen unter MIT mitliefert, entbindet nicht zwingend von einer eigenen Lizenzprüfung.

---

## 1. DHWcalc (Jordan & Vajen, Uni Kassel, IEA SHC Task 26)

### 1.1 Modellansatz — vollständige Parametrierung

Quelle: [Jordan & Vajen, „Realistic Domestic Hot-Water Profiles in Different Time Scales" (IEA SHC Task 26, 2001)](https://sel.me.wisc.edu/trnsys/trnlib/iea-shc-task26/iea-shc-task26-load-profiles-description-jordan.pdf) und [Jordan & Vajen, ISES SWC 2005](https://solar-publikationen.umwelt-uni-kassel.de/uploads/2005%20ISES-SWC%20Jordan%20und%20Vajen%20Program%20to%20Generate%20Domestic%20Hot%20Water%20Profiles%20with%20Statistical%20Means%20for%20User%20Defined%20Conditions.pdf)

**Die 4 Zapfkategorien (1-min-Auflösung, Referenzfall 200 l/d):**

| Kat. | Bezeichnung | Mittl. Volumenstrom | Dauer | Ereignisse/Tag | Anteil | Vol./Tag |
|---|---|---|---|---|---|---|
| A | Kurzzapfung (Händewaschen) | 1 l/min (60 l/h) | 1 min | 28 | 14 % | 28 l |
| B | Mittlere Last (Spüle, Geschirr) | 6 l/min (360 l/h) | 1 min | 12 | 36 % | 72 l |
| C | Bad (Wanne) | 14 l/min (840 l/h) | 10 min | 0,143/d (≈1/Woche) | 10 % | 20 l |
| D | Dusche | 8 l/min (480 l/h) | 5 min | 2 | 40 % | 80 l |

- Volumenströme streuen **gaußverteilt**, σ = 2 l/min, diskretisiert in 0,2-l/min-Schritten.
- Maximale Energie pro Zapfung ≈ **5820 Wh** — bewusst an **DIN 4708** (Einheitswohnung/Vollbad) angelehnt. Das ist eine wichtige Kopplung: DHWcalc ist normkompatibel konstruiert.
- Standard-Tagesvolumen 200 l/d, skalierbar in Zweierpotenzen (100/200/400/800/1600/3200 l/d).

**Wahrscheinlichkeitsmodell (multiplikativ):**

```
p = p(Jahr) · p(Wochentag) · p(Tageszeit) · p(Ferien)
```

- **Saisonal:** Sinusfunktion, ±10 % Amplitude um den Mittelwert.
- **Wöchentlich:** nur auf die Badkategorie angewandt — Mo–Do 0,95 / Fr 0,98 / Sa–So 1,09–1,13.
- **Täglich:** Stufenfunktion, Zapfungen konzentriert 5:00–23:00, kategoriespezifisch (Werktag vs. freier Tag).
- **Ferien:** zwei 14-Tage-Perioden (14.–28. Juli, 8.–22. August) mit 50 % Lastreduktion; erzeugt zusätzlich ±3,8 % saisonale Varianz.

**Zeitauflösungen:**
- **1 min:** volle 4-Kategorien-Logik (1/5/10-min-Zapfungen), bis 3200 l/d
- **6 min:** eine einzige Kategorie, bis 6400 l/d
- **1 h:** gemittelte 6-min-Profile; laut Autoren **nur für große Systeme** sinnvoll, weil die Momentanvolumenströme sonst unrealistisch werden

### 1.2 Verfügbarkeit, Lizenz, Grenzen

- **Download:** [Uni Kassel, Fachgebiet Solar- und Anlagentechnik, Downloads](https://www.uni-kassel.de/maschinenbau/en/institute/thermische-energietechnik/fachgebiete/solar-und-anlagentechnik/downloads) — „DHWcalc Version 2.02b incl. manual — ZIP 2,51 MB". Eine neuere, in Entwicklung befindliche Version (mit Profilen für ganze **Wohnquartiere**) gibt es auf Anfrage bei `solar@uni-kassel.de`.
- **⚠️ Lizenz: UNKLAR.** Auf der Downloadseite stehen **keine** expliziten Lizenz-/Nutzungsbedingungen. In der Literatur wird DHWcalc durchgängig als „free to use" und „public-funded" bezeichnet (so z. B. explizit im [OpenDHW-README](https://github.com/RWTH-EBC/OpenDHW)), aber das ist **keine belastbare Rechtsgrundlage für Einbettung in verkaufte Software**. → **Empfehlung: nicht einbetten.** Falls DHWcalc-Profile als Referenzdaten mitgeliefert werden sollen, schriftliche Freigabe von Uni Kassel einholen.
- **Grenzen des Modells:**
  - Windows-GUI-Programm, **keine API, kein Batch-Interface** (deshalb überhaupt die Reimplementierungen)
  - **Rein wohngebäudeorientiert** — kein Hotel/Büro/Pflege/Sport
  - Kein Personen-/Belegungsmodell, keine Kopplung an Anwesenheit — Tagesprofil ist eine feste Stufenfunktion
  - Keine Zirkulationsverluste, keine Kaltwassertemperatur-Saisonalität (muss extern ergänzt werden)
  - Gleichzeitigkeit bei Aggregation ist **implizit** (Poisson-artig durch unabhängige Ziehung) — das kann je nach Skalierungsweise zu stark (Volumenskalierung statt N unabhängiger Haushalte) oder korrekt sein. **Kritischer Punkt, siehe Abschnitt 8.**

---

## 2. Python-Reimplementierungen: OpenDHW im Detail

### 2.1 OpenDHW (RWTH Aachen, E.ON ERC, EBC) — **die Empfehlung**

- **Repo:** https://github.com/RWTH-EBC/OpenDHW · **PyPI:** https://pypi.org/project/OpenDHW/
- **Lizenz: MIT** (`Copyright (c) 2024 RWTH Aachen University — E.ON ERC — EBC`) → **uneingeschränkt kommerziell nutzbar**
- **Reifegrad/Stand:** v0.2.8, letzter Commit **19.02.2026** („add nrb-types"). Release-Historie: 0.1 (28.01.2025) → 0.2.8 (19.02.2026). **Aktiv gepflegt**, aber kleines Projekt (wenige Stars, 2 offene Issues) — Bus-Faktor beachten.
- **Dependencies:** `scipy, pandas, numpy, matplotlib, seaborn, holidays` — schlank, keine exotischen Abhängigkeiten.
- **Datenstruktur:** pandas DataFrame mit `DatetimeIndex`, Zeitschritt `s_step` in Sekunden → direkt in Ihre 8760-h-Bilanz integrierbar, Resampling ist eingebaut (`e11_Resample_Timeseries.py`).

**API (aus dem Quellcode verifiziert):**
```python
OpenDHW.generate_dhw_profile(
    s_step,                     # Zeitschritt in s (60, 900, 3600)
    categories,                 # 1 oder 4
    mean_drawoff_vol_per_day,   # l/(Person·d), wird intern mit occupancy multipliziert
    occupancy,                  # Personenzahl
    holidays,                   # Ferienliste
    building_type,              # 'SFH','TH','MFH','AB' | 'OB','SC','GS','RE','UNI',
                                # 'HOSPITAL','CULTURE','SPORT','RETAIL','WORKSHOP'
    weekend_weekday_factor,
    initial_day=0               # 0=Mo … 6=So
)
```
Zusätzlich: `import_from_dhwcalc()` (lädt echte DHWcalc-Dateien — das Repo liefert eine umfangreiche Referenzsammlung `DHWcalc_Files/` mit, z. B. `160L/197L/198L/199L/2000L × 1min/15min/60min × 1cat/4cat`), `compare_generators()` (statistischer Vergleich), `add_additional_runs()` (Monte-Carlo-Ensembles), Speicherlast-Beispiel `e10_Storage_Load.py`.

**Nichtwohngebäude-Typen (neu in v0.2.8) — 10 Nutzungsarten:**

| Code | Nutzungsart | Betriebslogik (aus `OpenDHW.py`) |
|---|---|---|
| `OB` | Bürogebäude | Sa/So + Feiertage geschlossen |
| `SC` | Schule | Sa/So + Feiertage + Tage 182–212 (Sommerferien) |
| `UNI` | Universität | wie Schule |
| `GS` | Lebensmittelmarkt | nur So + Feiertage geschlossen |
| `RE` | Restaurant | nie geschlossen |
| `HOSPITAL` | Krankenhaus | nie geschlossen |
| `CULTURE` | Kultur | nie geschlossen |
| `SPORT` | Sport | nie geschlossen |
| `RETAIL` | Einzelhandel | nie geschlossen |
| `WORKSHOP` | Werkstatt | nie geschlossen |

Für NWG nutzt OpenDHW **2 Zapfkategorien** statt 4: mittl. Volumenströme 100 und 360 l/h, Anteile 0,28 / 0,72, σ = 200 bzw. 240 l/h, max. Volumenstrom 1600 l/h (statt 1200 l/h bei WG). Die Tagesprofile liegen als JSON in `OpenDHW/Data/prob_nonresidential.json` (Stufenfunktion je `work-day`/`off-day`) — **direkt editierbar**, d. h. Sie können eigene Nutzungsarten (Hotel, Pflegeheim, Kita, Fitness) sehr einfach durch Hinzufügen eines JSON-Eintrags ergänzen. Das ist der praktisch wichtigste Integrationsvorteil.

**⚠️ Abweichungen zu DHWcalc, die Sie kennen müssen (aus Code-Review):**
1. **σ der Volumenströme:** DHWcalc-Paper nennt σ = 2 l/min für **alle** Kategorien. OpenDHW verwendet `stddev = [120, 120, 12, 24] l/h` = `[2, 2, 0,2, 0,4] l/min` — für Bad und Dusche also **10× bzw. 5× enger**. Das dämpft Spitzen. Bewusst prüfen, ggf. anpassen.
2. **Kappung:** Zapfungen werden auf `µ ± 2σ` und `[min, max]` begrenzt (Rejection Sampling) → die Verteilung ist **abgeschnitten normal**, nicht normal. Für Auslegungsspitzen relevant.
3. **Referenzwert:** `ReadMe.txt` im Repo dokumentiert die Annahme **40 l/(Person·d)** TWW (vs. 123 l/(Person·d) Gesamtwasser laut BDEW/UBA — der Rest ist WC, Spül-/Waschmaschine mit Kaltwasseranschluss). Das Repo liefert die Belegdokumente (BDEW-PDFs, DIN EN 12831-3-Auszug, `DIN12831_Interaktiv.xlsx`) mit.
4. **Saisonalität** ist hart auf `1 + 0,1·cos(π·(2d/365 − 1/4))` verdrahtet — Amplitude wäre zu parametrisieren.

**Validierungsstand:** Das Repo enthält den `compare_generators()`-Vergleich gegen echte DHWcalc-Ausgaben — also **Verifikation gegen DHWcalc**, nicht Validierung gegen Messdaten. Eine peer-reviewte Validierungspublikation zu OpenDHW konnte ich **nicht finden** (Unsicherheit).

### 2.2 Andere Python-Reimplementierungen

- **„pyDHW"** — **existiert nicht** als etabliertes Projekt. Meine Suchen lieferten keinen Treffer. (Nicht verwechseln mit [`pydhn`](https://github.com/idiap/pydhn), das ist ein Fernwärme-**Netzhydraulik**-Paket von Idiap, keine Zapfprofile.)
- **pyCREST** ([github.com/4c656554/pyCREST](https://github.com/4c656554/pyCREST)) — Python-Portierung des CREST-Modells; Reifegrad niedrig, Lizenz prüfen.
- **EnTiSe** ([github.com/tum-ens/EnTiSe](https://github.com/tum-ens/EnTiSe), TUM ENS) — **MIT**, Copyright 2025 TUM, letzter Commit **06.07.2026**, aktiv. Generisches Framework für synthetische Energiezeitreihen mit **DHW als eigenem Modul** (plus Strom, HVAC, WP-COP, Belegung, PV, Wind). Python ≥3.10, PyPI-verfügbar. Paper: [Doepfert & Hamacher, SoftwareX 2026](https://www.sciencedirect.com/science/article/pii/S2352711026002761) · [Doku](https://entise.readthedocs.io/). **Interessant als Architektur-Vorbild** (Pipeline-Muster, Batch-Generator), DHW-Methodik aber weniger tief als OpenDHW. Sehr jung, kaum Community.

---

## 3. IEA SHC Task 26 / 32 / 44 — was ist frei herunterladbar?

| Quelle | Inhalt | Verfügbarkeit |
|---|---|---|
| **Task 26** (Solar Combisystems) | Die **Ursprungsprofile** von Jordan/Vajen; Methodenbeschreibung als PDF | Methoden-PDF frei: [sel.me.wisc.edu/trnsys/trnlib/iea-shc-task26/…](https://sel.me.wisc.edu/trnsys/trnlib/iea-shc-task26/iea-shc-task26-load-profiles-description-jordan.pdf). Die **Profildateien** liegen in der TRNSYS-TRNLIB; der Verzeichnisindex war beim Abruf **403-gesperrt** (Unsicherheit — ggf. direkt bei SEL/UW-Madison anfragen). **Praktikabler Ersatz:** die `DHWcalc_Files/` im OpenDHW-Repo (MIT) decken dieselben Fälle ab. |
| **Task 32** (Advanced Storage) | Speicher-Benchmarks | Publikationen frei auf iea-shc.org, Profile nicht separat gelistet |
| **Task 44 / HPP Annex 38** (Solar & Heat Pump) | **Reference Framework** — Randbedingungen inkl. DHW-Lastdefinition | **Frei:** [T44A38_Rep_C1_A_BoundaryConditions_Final_Revised.pdf](https://www.iea-shc.org/Data/Sites/1/publications/T44A38_Rep_C1_A_BoundaryConditions_Final_Revised.pdf). Task-44-Publikationsseite: [task44.iea-shc.org/publications](https://task44.iea-shc.org/publications) |
| **EN 16147 Zapfzyklen** (M, L, XL…) | Normative Prüf-Zapfzyklen für WW-Wärmepumpen | Norm ist **kostenpflichtig**. Aber: nützlich als **Plausibilitätsanker** für Ihre Einzelzapfungen (die Zyklen sind in vielen Herstellerdatenblättern öffentlich abgedruckt). |

**Zu SimBench/ARegV:** Beides ist **nicht einschlägig**. SimBench liefert elektrische Netz-Benchmarkdatensätze, ARegV ist die Anreizregulierungsverordnung (Netzentgelte). Für TWW gibt es dort nichts. → **Nicht weiterverfolgen.**

---

## 4. LoadProfileGenerator (Noah Pflugradt)

- **Website:** https://www.loadprofilegenerator.de/ · **Repos:** [loadprofilegenerator/LoadProfileGenerator](https://github.com/loadprofilegenerator/LoadProfileGenerator) (bis v9.6.0) und [FZJ-IEK3-VSA/LoadProfileGenerator](https://github.com/FZJ-IEK3-VSA/LoadProfileGenerator) (Weiterentwicklung beim Arbeitgeber FZJ)
- **Lizenz: MIT.** Die [FAQ](https://www.loadprofilegenerator.de/faq/) ist explizit: „The entire source code is freely available under the MIT-License", kommerzielle Nutzung ausdrücklich erlaubt, kostenlos, einzige Bitte: Bugs melden. **Rechtlich der sauberste Fall im ganzen Feld.**
- **Paper (JOSS, peer-reviewed):** [Pflugradt et al., „LoadProfileGenerator: An Agent-Based Behavior Simulation for Generating Residential Load Profiles", JOSS 2022, doi:10.21105/joss.03574](https://joss.theoj.org/papers/10.21105/joss.03574)
- **Ansatz:** vollständig **verhaltensbasiert/agentenbasiert** mit psychologischem Bedürfnismodell (Wünsche/Desires → Aktivitäten → Gerätenutzung). Deutlich reichhaltiger als DHWcalc; erzeugt korrelierte Strom-/Warmwasser-/Anwesenheitsprofile.

**⚠️ TWW-Besonderheit (wichtig, aus der FAQ):** Haushalte in LPG erzeugen **„warm water consumption" bei ca. 35 °C** (Duschtemperatur), **nicht** TWW bei 60 °C. Um eine echte TWW-Lastkurve zu bekommen, muss der Haushalt in eine **House**-Struktur eingebettet und ein **„warm water transformation device"** hinzugefügt werden. Wer das übersieht, bekommt systematisch falsche Energiemengen. Ausgabe: Strom, Gas, Kaltwasser, Warmwasser, Heizung, Kühlung.

- **Auflösung:** intern **1 min** (`ExternalTimeResolution` konfigurierbar, z. B. `00:15:00`).
- **Rechenaufwand:** Speicherbedarf **500 MB–1 GB pro gleichzeitig gerechnetem Haushalt** ([Doku „Calculating large numbers"](https://www.loadprofilegenerator.de/calculatinglargenumbers/)); CLI-Batchbetrieb parallelisierbar. **Für Laufzeit-Simulation in einer interaktiven Planungssoftware praktisch untauglich.**
- **Version:** 10.10.0 (64 bit), 18.06.2024; Windows-GUI (.NET 6.0) + Rechenkern für Windows **und Linux**.

### 4.1 pylpg — der praktikable Weg

- **Repo:** https://github.com/FZJ-IEK3-VSA/pylpg · PyPI: `pyloadprofilegenerator` · **Lizenz: MIT**
- Wandelt Einstellungen in JSON, **lädt beim ersten Aufruf automatisch die LPG-Binaries + Datenbank herunter**, startet den plattformabhängigen Kern (Windows/Linux), liefert **pandas DataFrames** zurück.
- ~118 Commits, 31 Stars, **noch keine formalen Releases** → Reifegrad mittel.

**Bewertung für INEKON:** LPG ist als **Datenquelle für vorgenerierte Profile** attraktiv, **nicht** als Laufzeit-Engine. Empfehlung: einmalig ein Ensemble (z. B. 50–200 Haushalte × Haushaltstypen × Personenzahl) mit pylpg offline erzeugen, als komprimierte Referenzbibliothek ins Produkt legen (MIT erlaubt das) und zur Laufzeit nur noch samplen/skalieren. Das gibt Ihnen realistische **Korrelation zwischen Anwesenheit, Strom und TWW** — etwas, das DHWcalc/OpenDHW prinzipbedingt nicht liefern.

**Nichtwohngebäude:** LPG ist **rein wohngebäudeorientiert.** Keine Hotels/Büros/Pflege.

---

## 5. CREST, StROBe, pysimdeum

### 5.1 CREST Demand Model (Loughborough University)
- **Download:** [Figshare-Repositorium Loughborough](https://repository.lboro.ac.uk/articles/dataset/CREST_Demand_Model_v2_0/2001129) — Versionen ab v2.1 (Dez. 2015), v2.3 (Feb. 2018) mit indischen Haushalten.
- **Format: Excel/VBA** — für Python-Integration ungeeignet. Python-Port: [pyCREST](https://github.com/4c656554/pyCREST) (Reifegrad niedrig).
- **Paper:** [McKenna & Thomson, „High-resolution stochastic integrated thermal–electrical domestic demand model", Applied Energy 2016, doi:10.1016/j.apenergy.2015.11.089](https://www.sciencedirect.com/science/article/pii/S0306261915016621) — integriert thermisch/elektrisch, 1-min-Auflösung, **UK-kalibriert**.
- **⚠️ Lizenz:** als „free open-source software" beschrieben; die konkrete Lizenz war auf der Figshare-Seite **nicht eindeutig auslesbar** (Figshare-Default ist meist CC-BY 4.0 — **verifizieren, bevor Sie es verwenden**).
- **Bewertung:** UK-Kalibrierung + Excel/VBA + Lizenzunsicherheit → **für Ihren Anwendungsfall nicht empfohlen**, allenfalls als methodische Referenz.

### 5.2 StROBe (KU Leuven, open-ideas)
- **Repo:** https://github.com/open-ideas/StROBe · Python 3.7+ · **letzter Commit 03.02.2021** (Py2.7-Branch), Status **Beta**
- Erzeugt Belegung, Geräte, Beleuchtung, Heizungssollwerte **und TWW-Zapfungen** als Randbedingungen für IDEAS/Modelica-Simulationen.
- **Papers:** [Baetens & Saelens, J. Building Performance Simulation 9(4):431–447, 2016](https://www.tandfonline.com/doi/abs/10.1080/19401493.2015.1070203); Baetens PhD 2015.
- **🚩 KRITISCH: Das Repository enthält KEINE LICENSE-Datei** (verifiziert per Clone: nur `Corpus/`, `Data/`, `README.md`, `example.py`). Ohne Lizenz gilt **volles Urheberrecht** → **kommerzielle Nutzung ist nicht gestattet.** Zusätzlich seit >5 Jahren unmaintained. → **Ausschließen**, außer Sie holen eine schriftliche Lizenz bei KU Leuven ein.

### 5.3 pysimdeum (KWR Water Research Institute, NL)
- **Repo:** https://github.com/KWR-Water/pysimdeum · **Doku:** https://pysimdeum.readthedocs.io/
- **Lizenz: EUPL-1.2.** ⚠️ Die EUPL ist eine **starke Copyleft-Lizenz** (kompatibel mit GPL/AGPL). Wenn Sie pysimdeum in Ihre verkaufte Software einbinden, kann das je nach Integrationsart die **Offenlegung Ihres abgeleiteten Werks** erzwingen. **Rechtliche Prüfung zwingend erforderlich** — für ein kommerzielles Closed-Source-Produkt ist das ein echtes Risiko. Alternative: pysimdeum nur als **separater Offline-Prozess** zur einmaligen Profilgenerierung (Output ist Ihr Werk, nicht abgeleitet vom Code).
- **Nichtwohngebäude: NEIN.** Explizit auf Wohnhaushalte beschränkt („Build and populate houses with users and water end-use devices according to region specific statistics"). Kein Hotel, kein Büro, kein Pflegeheim. Die Release-Notes erwähnen **keinerlei** NWG-Funktionalität. → **Damit fällt der Hauptgrund weg, warum pysimdeum für Sie interessant gewesen wäre.**
- **Warmwasser: JA, seit v1.0.0/1.0.1 (26.08.2024)** — neue `flowtypes`-Dimension mit `totalflow` und `hotflow`. Breaking Change ggü. 0.x.
- **API:** sauber objektorientiert (`House` → `.simulate()` → `xarray.DataArray`), Export nach CSV/Excel/NetCDF, matplotlib-Plots. **Technisch die eleganteste API im Feld.**
- **Reifegrad:** 10 Releases, ~25 Stars, 22 offene Issues. Mittel.
- **Paper:** [Steffelbauer et al., „pySIMDEUM: An open-source stochastic water demand end-use model in Python", WDSA/CCWI 2022, Valencia](https://ocs.editorial.upv.es/index.php/WDSA-CCWI/WDSA-CCWI2022/paper/view/14774)
- **Validierungsstand:** SIMDEUM (das Original, KWR) ist in der Trinkwasserbranche breit validiert — allerdings gegen **Kaltwasser-Gesamtverbrauch**, nicht primär gegen TWW-Wärmelasten. Regionalstatistik ist **niederländisch** — für deutsche Anwendung neu zu parametrieren.

---

## 6. demandlib (oemof) — VDI 4655 & BDEW

- **Repo:** https://github.com/oemof/demandlib · **Doku:** https://demandlib.readthedocs.io/
- **Lizenz: MIT** (verifiziert). **Aktiv: letzter Commit 23.07.2026.** Paket heißt inzwischen `oemof-demand` (v0.2.3a).

### 6.1 Was genau ist enthalten (per Repo-Inspektion verifiziert)

**BDEW-Modul:** Strom-Standardlastprofile (H0, G0–G6, L0–L2) **und** Gas-/Wärme-Lastprofile (Sigmoid-Funktion). Seit [v0.2.2 (Apr. 2025)](https://oemof.org/2025/04/11/demandlib-0-2-2-vdi-and-bdew25/) zusätzlich die **BDEW-2025-Profile** mit beliebigen Zeiträumen und beliebiger Zeitauflösung. Zusätzlich ein industrielles Strommodul.

**VDI-4655-Modul — hier liegt der TWW-Wert:**

| Aspekt | Befund |
|---|---|
| **TWW-Trennung** | **JA, vollständig getrennt.** Drei Kanäle: `Q_Heiz_TT` (Heizung), `Q_TWW_TT` (Trinkwarmwasser), `W_TT` (Strom). Eingabe pro Gebäude: `Q_Heiz_a`, `Q_TWW_a`, `W_a` (Jahreswerte in kWh). |
| **Mitgelieferte Daten** | `VDI_4655_Typtage.csv` (15 360 Zeilen) und `VDI_4655_Typtag-Faktoren.csv` — **die VDI-Tabellen sind im MIT-Repo enthalten** |
| **Auflösung** | **1440 Zeitschritte/Typtag = 1 min**, alternativ 96 = 15 min; `resample_rule` frei wählbar |
| **Gebäudetypen** | **EFH und MFH** (nur diese beiden) |
| **Typtage** | 10: `UWH, UWB, USH, USB, SWX, SSX, WWH, WWB, WSH, WSB` (Übergang/Sommer/Winter × Werktag/Sonntag × heiter/bewölkt) |
| **Klimaregionen** | **15 DWD-TRY-Regionen**; `vdi.find_try_region(lon, lat)` ermittelt sie aus Koordinaten (benötigt geopandas) |
| **Eingaben je Gebäude** | `N_Pers`, `N_WE`, `house_type`, `Q_Heiz_a`, `Q_TWW_a`, `W_a`, `summer/winter_temperature_limit`, `copies` |

**TWW-Kernformel im Code** (`regions.py`):
```
Q_TWW_TT = Q_TWW_a · (1/365 + n_pers_we · F_TWW_TT)
```
Mit Sicherung: wird `Q_TWW_TT` negativ (kann bei `F_TWW_SWX` passieren), wird `F_TWW = 0` gesetzt — dieser Sonderfall ist in VDI 4655 S. 16 explizit geregelt und im Code kommentiert.

**⚠️ Lizenz-Nuance:** Der **Code** ist MIT. Die **VDI-4655-Zahlentabellen** unterliegen dem VDI-Urheberrecht; dass sie hier unter MIT verteilt werden, ist ein Umstand, den ich nicht rechtlich bewerten kann. → **Vor Auslieferung in einem verkauften Produkt: eigene juristische Prüfung bzw. VDI-Lizenz klären.** (Kennzeichnete Unsicherheit.)

**Grenze für Ihren Zweck:** VDI 4655 ist ein **Typtag-Modell mit deterministischen Normprofilen**. Es liefert eine gute **Bilanz**, aber die Spitzen sind für Auslegung problematisch — genau Ihr 2,4×-Befund. Siehe Abschnitt 9.

### 6.2 lpagg — der VDI-4655-Aggregator mit Gleichzeitigkeit

- **Repo:** https://github.com/jnettels/lpagg · **Lizenz: MIT** · 291 Commits, 23 Releases, **letztes Release Dez. 2025** — aktiv
- Kombiniert Heiz-, **TWW**- und Stromprofile mehrerer Gebäude zu Quartiers-Jahresprofilen. Nutzt entweder eigene eingebaute VDI-4655-Profile **oder** demandlib als Backend (konfigurierbar).
- Eingabe: YAML (Gebäudetyp, Personen, DWD-Wetter, Feiertage, Sommerzeit). Auflösung: 15-min-Typtagsprofile → Jahreskalender.
- **🔑 Gleichzeitigkeits-Feature:** lpagg wendet **normalverteilte Zeitverschiebungen pro Gebäude** an, „in order to approximate the effects of simultaneity present in larger groups of buildings". Das ist genau der Mechanismus, den Sie brauchen — und ein **direkt übernehmbarer Ansatz** für Ihre eigene Diversitätsschicht (siehe 8.4).

### 6.3 districtgenerator (RWTH-EBC)
- **Repo:** https://github.com/RWTH-EBC/districtgenerator · **Lizenz: MIT** (verifiziert) · **letzter Commit 17.07.2026**, v0.1.1 (Juni 2025), 33 Stars, 37 offene Issues
- Erzeugt gebäudescharfe Lastprofile für **Quartiere**; TWW über pyCity-Funktionen, Belegungsprofile als Basis.
- **Gebäudetypen: WG (SFH, TH, MFH, AB) + NWG (Büro, Schule, Lebensmittelmarkt, Restaurant)** — dieselbe Typologie wie OpenDHW v0.2.8 (gleiches Institut, offensichtlich abgestimmt).
- Abhängigkeiten: TEASER, richardsonpy, pyCity, **TABULA-WebTool-Daten**, DWD-Wetter für 16 deutsche Klimazonen.
- **Bewertung:** Sehr gute Passung zur deutschen Praxis, aber **schwerer Dependency-Stack**. Als Ideengeber und für die TABULA-Anbindung wertvoll; als eingebettete Bibliothek zu schwergewichtig.

### 6.4 Weitere geprüfte Tools

| Tool | Befund |
|---|---|
| **TEASER** (RWTH-EBC) | Gebäudemodell-Generator (Modelica/TEASER-Archetypen), **kein eigener TWW-Zapfprofilgenerator**. Nur indirekt via districtgenerator relevant. |
| **tsib** (FZJ-IEK-3) | [github.com/FZJ-IEK3-VSA/tsib](https://github.com/FZJ-IEK3-VSA/tsib) — „Time Series Initialization of Buildings", **MIT-Lizenz** (verifiziert), aber **letzter Commit 22.03.2023** → faktisch unmaintained. Bündelt u. a. LPG-Anbindung. Nur als Referenz. |
| **hotmaps** | [wiki.hotmaps.eu/en/CM-Heat-load-profiles](https://wiki.hotmaps.eu/en/CM-Heat-load-profiles) — **CC-BY-4.0**, 8760-h-Profile auf **NUTS2-Ebene**, EU28+. **Warmwasser und Raumheizung sind getrennt ausgewiesen**, Sektoren Wohnen + Tertiär (Dienstleistung). Profile auf Integral = 1 normiert. **Für Regionalbilanzen brauchbar, für Gebäudeauslegung viel zu grob** (NUTS2 = Regierungsbezirksebene). |
| **cesar-p** (Empa, CH) | [SIA-2024-DHW-Doku](https://cesar-p-core.readthedocs.io/en/latest/features/sia2024-dhw-demand.html) — nutzt **SIA 2024/2016** Personen-Verbrauchswerte: **EFH 40 l/(P·d)** → 13,5 kWh/m²a; **MFH 35 l/(P·d)** → 17,8 kWh/m²a; 60 °C warm / 10 °C kalt. SIA 2024 deckt **sehr viele Nutzungsarten** ab (Wohnen, Büro, Schule, Verkauf, Restaurant, Hotel, Spital, Sport…) und ist damit die **beste normative NWG-Quelle im DACH-Raum**. ⚠️ SIA 2024 ist eine kostenpflichtige Norm. |

---

## 7. Nichtwohngebäude: ASHRAE 90.1 / DOE Commercial Reference Buildings

**Das ist der wichtigste Hebel für „möglichst viele Nutzungsarten" — und er ist frei.**

Quelle: [Deru et al., „U.S. DOE Commercial Reference Building Models of the National Building Stock", NREL/TP-5500-46861, 2011](https://docs.nlr.gov/docs/fy11osti/46861.pdf) — US-Regierungsbericht, **gemeinfrei/frei verwendbar**.

**16 Gebäudetypen:** Small/Medium/Large Office, Primary/Secondary School, Stand-Alone Retail, Strip Mall, Supermarket, Quick Service Restaurant, Full Service Restaurant, Small/Large Hotel, Hospital, Outpatient Healthcare, Warehouse, Midrise Apartment.

**Tabelle 11 „Peak Service Hot Water Demand and Data Sources" (wörtlich extrahiert, S. 17):**

| Space Type | gal/h | **l/h** | °F | **°C** | Quelle |
|---|---|---|---|---|---|
| Gästezimmer (Small Hotel) | 1,75 | **6,6** | 110 | 43 | Jiang et al. 2008, ASHRAE 2007 |
| Gästezimmer (Large Hotel) | 1,25 | **4,7** | 110 | 43 | Jiang et al. 2008, ASHRAE 2007 |
| Wäscherei (Small Hotel) | 67,5 | **255,5** | 140 | 60 | Jiang et al. 2008 |
| Wäscherei (Large Hotel) | 156,6 | **592,8** | 140 | 60 | Jiang et al. 2008 |
| Sanitär (Grundschule) | 56,5 | **214,0** | 110 | 43 | ASHRAE 2007 |
| Sanitär (Sek.-Schule) | 104,4 | **395,0** | 110 | 43 | ASHRAE 2007 |
| Sporthalle (Sek.-Schule) | 189,5 | **717,2** | 110 | 43 | ASHRAE 2007 |
| Kleines Büro | 3,0 | **11,4** | 110 | 43 | Jarnagin et al. 2006 |
| Mittleres Büro (pro Geschoss) | 9,9 | **37,5** | 110 | 43 | Jarnagin et al. 2006 |
| Großes Büro (pro Geschoss) | 21,3 | **80,6** | 110 | 43 | Jarnagin et al. 2006 |
| Wohnung (Apartment) | 3,5 | **13,2** | 110 | 43 | Gowri et al. 2007 |
| Ambulante Versorgung | 30,0 | **113,5** | 110 | 43 | Doebber et al. 2009 |
| Krankenhaus: Notaufnahme-Wartebereich | 1,0 | **3,8** | 120 | 49 | Ingenieurschätzung |
| Krankenhaus: OP/Zystoskopie | 2,0 | **7,6** | 120 | 49 | Ingenieurschätzung |
| Krankenhaus: Labor | 2,0 | **7,6** | 120 | 49 | Ingenieurschätzung |
| Krankenhaus: Patientenzimmer | 1,0 | **3,8** | 120 | 49 | Ingenieurschätzung |

**Kalibrierhinweis aus dem Bericht:** Für Hotels wird ein mittlerer Warmwasserverbrauch von **14 gal/d (53 l/d) pro Gästezimmer im kleinen Hotel** bzw. **10 gal/d (38 l/d) im großen Hotel** angesetzt. Der Zusammenhang: Tagesvolumen = Spitzenwert × Schedule-Integral, wobei das Schedule ein **Vollast-Äquivalent von 8 h/Tag** ergibt. Wäscherei: 9 lb (4,1 kg) Wäsche pro Zimmer und Tag × 1,2 gal/lb (9,9 l/kg). Speicher auf 140 °F (60 °C).

**Stündliche Schedules:** Appendix B des Berichts (Tabellen B-1 bis B-15) enthält die Schedules je Gebäudetyp, u. a. `BLDG_SWH_SCH`, `APT_DHW_SCH`, `GuestRoom_SWH_Sch`, `LaundryRoom_SWH_Sch`, `Kitchen Water Equipment…`. Maschinenlesbar sind sie am einfachsten aus:
- den **IDF-Dateien** der DOE Commercial Reference Buildings / ASHRAE-90.1-Prototypen ([OEDI-Submissions](https://data.openei.org/submissions/160), [catalog.data.gov](https://catalog.data.gov/dataset/commercial-reference-building-small-hotel))
- oder aus [NREL/openstudio-standards](https://github.com/NREL/openstudio-standards), wo alle Prototyp-Schedules als JSON hinterlegt sind

**Bewertung:** Deckt **16 Nutzungsarten**, ist **frei und gemeinfrei**, aber:
- ⚠️ **US-Nutzungsmuster und US-Verbrauchsniveaus** — Volumina müssen für Deutschland skaliert werden (deutsche Werte deutlich niedriger; vgl. DIN EN 12831-3 / SIA 2024 / VDI 2067)
- Schedules sind **deterministische Stundenprofile**, keine stochastischen Zapfungen → **für Bilanz gut, für Speicherauslegung zu glatt.** Kombinationsvorschlag: DOE-Schedule als `p(Tageszeit)`-Funktion in den OpenDHW-Generator einspeisen (die JSON-Struktur `prob_nonresidential.json` ist genau dafür gebaut).

**Europäische Alternative — DIN EN 12831-3:** Ersetzt DIN 4708 und bringt das **Summenlinienverfahren**, das für **alle Gebäudetypen** (auch Hotel, Krankenhaus, Sport) gilt. Die nationale Ergänzung **DIN EN 12831-3/A100 (Entwurf 2021-09)** liefert laut [DIN-Pressemitteilung](https://www.din.de/de/mitwirken/normenausschuesse/nhrs/pressemitteilung-nationale-ergaenzung-zur-din-en-12831-3--297950) „Prüf- und Randbedingungen sowie **normative Zapfprofile für verschiedene Gebäudekategorien**". → **Das ist normativ die sauberste deutsche Quelle für NWG-Zapfprofile.** ⚠️ Kostenpflichtig ([DIN Media](https://www.dinmedia.de/en/standard/din-en-12831-3/261437587)); die konkrete Gebäudetypenliste konnte ich aus frei zugänglichen Quellen **nicht** verifizieren — **Beschaffung empfohlen.** (Praktischer Hinweis: OpenDHW liefert im Ordner `Resources_Water_Demand_Parametrisation/` bereits einen DIN-EN-12831-3-Auszug und `DIN12831_Interaktiv.xlsx` mit.)

---

## 8. Fachliche Kernfrage: Gleichzeitigkeit / Diversität korrekt modellieren

Das ist der Punkt, an dem die meisten Werkzeuge scheitern — und der Grund für Ihren 2,4×-Befund.

### 8.1 Das Grundgesetz: Superposition unabhängiger Poisson-Prozesse

Physikalisch korrekt ist: TWW-Zapfungen sind **seltene, kurze, näherungsweise unabhängige Ereignisse** → Poisson-Prozess. Bei Superposition von N Wohneinheiten gilt für die Momentanlast:

- Erwartungswert: μ_ges = N · μ_WE (**linear**)
- Standardabweichung: σ_ges = √N · σ_WE (**Wurzelgesetz**)
- Spitzenlast (z. B. 99. Perzentil): P₉₉ ≈ N·μ + z₀,₉₉·√N·σ

Daraus folgt zwingend die **Spitzenlast pro WE**:
```
P₉₉/N ≈ μ + z₀,₉₉·σ/√N   →  fällt mit 1/√N und konvergiert gegen μ
```
Das ist die theoretische Begründung für alle empirischen Gleichzeitigkeitskurven. **Wichtige Konsequenz für Ihre Implementierung:** Ein 20-WE-Gebäude darf **niemals** durch Skalierung eines 1-WE-Profils mit Faktor 20 entstehen — es müssen **20 unabhängige Ziehungen** superponiert werden. Wird das falsch gemacht, ist die Spitze um bis zu √20 ≈ 4,5 zu hoch. **Das ist meine Hauptvermutung für den Mechanismus hinter Ihrem 2,4×-Befund.**

### 8.2 Winter-Formel (Gleichzeitigkeitsfaktor, DACH-Standard für Wärmenetze)

Quelle: [Winter, Haslauer & Obernberger (2001), „Untersuchungen der Gleichzeitigkeit in kleinen und mittleren Nahwärmenetzen", Euroheat & Power 9&10/2001 — Volltext frei bei Verenum](https://www.verenum.ch/Dokumente/2001_Winter-Gleichzeitig.pdf)

```
GLF(n) = a + b / (1 + (c/n)^d)

a = 0,449677646267461
b = 0,551234688
c = 53,84382392
d = 1,762743268
```
- Gültigkeit **1 < n ≤ 200** Abnehmer; GLF(1) = 1; **r² = 0,95**
- **Datenbasis: 559 Kundenübergabestationen** in zwei österreichischen Biomasse-Nahwärmenetzen — Straßwalchen (304 Stationen, 6,8 MW, Mai 1998–Sep. 1999) und Tamsweg (255 von 286 Stationen, 10,5 MW, Juni 1998–Sep. 1999)
- Ab n > 200 läuft GLF gegen **≈ 0,47**
- ⚠️ **Einschränkung: Die Studie trennt NICHT zwischen TWW und Raumheizung** — es ist ein Gesamtwärme-GLF. Für reines TWW ist die Diversität **deutlich stärker** (siehe 8.3). Diese Verwechslung ist ein häufiger Planungsfehler.
- Referenzimplementierung/Erläuterung: [nPro — Gleichzeitigkeitsfaktor in Wärmenetzen](https://www.npro.energy/main/de/district-heating-cooling/diversity-factor)

### 8.3 TWW-spezifische Gleichzeitigkeit: Braas et al. 2020 — die beste Quelle

[Braas, Jordan, Best, Orozaliev & Vajen, „District heating load profiles for domestic hot water preparation with realistic simultaneity using DHWcalc and TRNSYS", *Energy* 201 (2020) 117552, doi:10.1016/j.energy.2020.117552](https://doi.org/10.1016/j.energy.2020.117552) · [Deutsche Zusammenfassung frei bei heatbeat](https://heatbeat.de/de/blog/2/) · [englisch](https://heatbeat.de/en/newsletter/2/)

Methodisch genau Ihr Anwendungsfall: DHWcalc-Zapfprofile + TRNSYS-Übergabestationsmodelle → realistische Gleichzeitigkeit.

**Ergebnisse (Gleichzeitigkeitsfaktoren):**

| Anzahl Gebäude | **mit** TWW-Speicher | **ohne** Speicher (Durchfluss/Frischwasserstation) |
|---|---|---|
| 1 | 100 % | 100 % |
| 20 | **40–50 %** | **15–20 %** |
| 100 | **30–45 %** | **≈ 10 %** |

**Der Schlüsselbefund:** Ein kleines EFH **ohne** Speicher hat **42 kW** Spitzenleistung, **mit** korrekt geladenem Speicher nur **3 kW** — Faktor **14**. Die Gleichzeitigkeit hängt also **massiv** von der Anlagentopologie ab, nicht nur von der Anzahl WE.

→ **Direkte Konsequenz für Ihre Software:** Der Gleichzeitigkeitsfaktor darf **nicht** nur Funktion von N sein, sondern muss `f(N, Anlagentyp)` sein: Durchflusssystem / Speichersystem / Frischwasserstation. Weitere Kennwerte aus der Studie: spez. TWW-Wärmebedarf 8,5–11 kWh/m²a (EFH), 11,7–12,5 kWh/m²a (MFH); Verteilverluste 8–13 kWh/m²a; Rücklauftemperatur ohne Speicher 8–9 K niedriger.

### 8.4 DIN 4708 (Bedarfskennzahl N) vs. DIN EN 12831-3 (Summenlinienverfahren)

- **DIN 4708:** Bedarfskennzahl **N** bezogen auf die „Einheitswohnung" (3,5 Personen, 4 Zimmer, ein Vollbad/Tag ≈ **5820 Wh = 5,82 kWh**, ≈ 500 l/d bzw. 20,4 kWh je N=1). Leistungskennzahl **N_L** charakterisiert den Speicher-Wassererwärmer. Die N-Kurve setzt eine **Gaußverteilung** an. Erläuterungen: [SHKwissen — Bedarfskennzahl/Leistungskennzahl](https://www.haustechnikdialog.de/SHKwissen/2005/Bedarfskennzahl-Leistungskennzahl), [SBZ Monteur — Auslegen nach DIN 4708](https://www.sbz-monteur.de/fokus/auslegen-nach-din-4708)
- **⚠️ Bekannte Schwäche:** Die Gaußannahme ist für Nichtwohngebäude, Hotels, Krankenhäuser und generell konzentrierte Spitzen ungeeignet und führt zu **erheblicher Überdimensionierung**.
- **DIN EN 12831-3** ersetzt sie durch das **Summenlinienverfahren**: kumulierte Energienachfrage über 24 h vs. Bereitstellungslinie (Speichervolumen, Erzeugerleistung, Verluste, Anlaufverzug). Gilt für **alle** Gebäudetypen.

**Der quantitative Vergleich** — [IKZ-Fachaufsatz „DIN EN 12831-3 als Ersatz für DIN 4708" (PDF)](https://www.ikz.de/fileadmin/user_upload/008_011.pdf), Beispiel 11-WE-Wohngebäude:

| Verfahren | Erzeugerleistung | Speichervolumen |
|---|---|---|
| DIN 4708 (4670 l/d Bedarf) | **60 kW** | 380 l |
| DIN EN 12831-3 (gleicher Bedarf) | **30 kW** | 380 l |
| DIN EN 12831-3 (realistisch 40 l/(P·d)) | **20 kW** | 300 l |

→ **DIN 4708 überschätzt hier um Faktor 2 bis 3** gegenüber EN 12831-3. **Das liegt bemerkenswert nahe an Ihrem 2,4×-Befund** (Abschnitt 9).

### 8.5 Hunter's Curve / IAPMO Water Demand Calculator — der anglo-amerikanische Zweig

[IAPMO Study 4-2024, „Peak Water Demand Study" — Volltext frei](https://iapmo.org/media/42ehgafw/peak-water-demand-full-study.pdf) · [Executive Summary](https://iapmo.org/media/jk1n2zf0/peak-water-demand-study-executive-summary.pdf)

- **Datenbasis: 1038 Einfamilienhäuser** in 9 US-Bundesstaaten, 1996–2011, **10-Sekunden-Auflösung**, ~863 000 Wasserentnahme-Ereignisse über 11 385 Haus-Tage (Ø 11 Tage/Haus, 2,72 Personen). **Das ist der größte hochaufgelöste offene Referenzdatensatz zu Wasserentnahme weltweit.**
- **Methodik:** Hunter-Zahl H(n,p) = Σ nₖpₖ, dann regionenabhängig exakte Enumeration (H < 1,25), **Modified Wistort Method** (1,25 ≤ H ≤ 5) bzw. Wistort (H ≥ 5). Modified Wistort nutzt eine **nullabgeschnittene Binomialverteilung**:

```
Q₀,₉₉ = [P₀/(1−P₀)] · { Σnₖpₖqₖ + z₀,₉₉·√[ P₀²·Σnₖpₖ(1−pₖ)qₖ² / (1−P₀)² ] }
```
mit P₀ = Wahrscheinlichkeit „keine Entnahme" in der Spitzenstunde.

- **Skalierungsbeleg (Tabelle aus der Studie):**

| Konfiguration | Armaturen | H(n,p) | P₀ | Q₀,₉₉ (gpm) | Q pro WE |
|---|---|---|---|---|---|
| 1 WE | 10 | 0,275 | 76 % | 11,0 | 11,0 |
| 3 WE | 30 | 0,825 | 43,8 % | 15,5 | 5,2 |
| 9 WE | 90 | 2,475 | 8,4 % | 24,6 | 2,7 |
| 27 WE | 270 | 7,425 | 0,1 % | 52,6 | **1,9** |

→ **27× mehr Armaturen ergeben nur 4,8× mehr Spitzenbedarf.** Die Spitze pro WE fällt von 11,0 auf 1,9 gpm — **Faktor 5,8**. Die Struktur `μ + z·σ/√N` passt sehr gut.
- ⚠️ Die Studie gibt **keinen expliziten Überdimensionierungsfaktor gegenüber Hunter's Curve** an (nur qualitativ: „an improved method to avoid over-design resulting from Hunter's Curve"; Hunters Datenbasis von 1940 sei „sparse data from a few hotels and government offices"). Weitere Analysen: [Buchberger et al., Ohio WRC Final Report 2016](https://wrc.osu.edu/sites/default/files/2022-04/Buchberger_2016OH506O_FinalReport.pdf).

### 8.6 Weitere Diversitätsmetriken

- **Peak Load Ratio (PLR)** — [Weißmann, Hong & Graubner, „Analysis of heating load diversity in German residential districts and implications for the application in district heating systems", *Energy and Buildings* 139 (2017) 302–313, doi:10.1016/j.enbuild.2016.12.096](https://doi.org/10.1016/j.enbuild.2016.12.096) · [Volltext frei (OSTI)](https://www.osti.gov/biblio/1416777). 144 simulierte Gebäudelastprofile (IDA ICE); **PLR erreicht bis 15 %**. Zentraler Befund: **Diversität ist besonders groß in Quartieren mit hoher Lastdichte, Neubau, niedriger Vorlauftemperatur und hohem TWW-Anteil** — d. h. je moderner der Bestand, desto stärker dominiert TWW und desto größer der Diversitätsgewinn.
- **Smart-Meter-basierte Diversität (UK):** [„Sizing of district heating systems based on smart meter data: Quantifying the aggregated domestic energy demand and demand diversity in the UK", *Energy* (2020), doi:10.1016/j.energy.2019.116780](https://doi.org/10.1016/j.energy.2019.116780) — Diversität direkt aus Messdaten statt aus Simulation.
- **Praktische Umsetzung mit Zeitversatz:** [lpagg](https://github.com/jnettels/lpagg) verschiebt jedes Gebäudeprofil um einen **normalverteilten Zeitoffset**. Das ist eine sehr pragmatische, leicht implementierbare Näherung an die Poisson-Superposition und für 15-min-Profile ausreichend.

---

## 9. Ihr Befund einordnen: „VDI-4655-Spitze 2,4× über Messung, Messspitze ≈ P90 der synthetischen Dauerlinie"

### 9.1 Was die Literatur direkt stützt

| Quelle | Befund | Faktor |
|---|---|---|
| [IKZ / DIN EN 12831-3 vs. DIN 4708](https://www.ikz.de/fileadmin/user_upload/008_011.pdf) | 11-WE-Gebäude: 60 kW (DIN 4708) vs. 30 kW (EN 12831-3) vs. 20 kW (realistischer Bedarf) | **2,0–3,0×** |
| [Braas et al., *Energy* 2020](https://doi.org/10.1016/j.energy.2020.117552) | EFH ohne Speicher 42 kW vs. mit Speicher 3 kW; GLF fällt bei 20 Gebäuden auf 15–50 % | **2–14×** je nach Topologie |
| [B2E, „Right-Sizing Matters: Retrofitting DHW in MURBs" (2025)](https://b2electrification.org/right-sizing-matters-retrofitting-domestic-hot-water-murbs) | Konventionelle ASHRAE/ASPE-Auslegung liefert **3× größere Heizleistung** als messdatengestützte Auslegung → >70 % höhere Investkosten. Aus Gasverbrauch abgeleitete TWW-Menge überschätzt die **gemessene** um **Faktor >5** (27 vs. 5 gal/Person·d). Basis: Gilford Court, 67 Bewohner, Onicon-F-4400-Durchflussmesser, 3 Wochen Okt. 2024 + 2 Wochen März 2025; ergänzend eine Studie der Stadt Vancouver mit **37 MURBs**. | **3× (Leistung), >5× (Menge)** |
| [IAPMO 4-2024](https://iapmo.org/media/42ehgafw/peak-water-demand-full-study.pdf) | Spitze pro WE fällt von 11,0 auf 1,9 gpm zwischen 1 und 27 WE | **5,8×** |

**Ihr Faktor 2,4 liegt damit genau im publizierten Korridor** und ist keine Anomalie, sondern der Normalfall bei normbasierter/synthetischer Auslegung. Der IKZ-Vergleich (2,0–3,0×) ist der nächstliegende Anker.

### 9.2 Die Gegenrichtung — wichtig, um nicht überzukorrigieren

[Weiler & Eicker, „Individual Domestic Hot Water Profiles for Building Simulation at Urban Scale", **Building Simulation 2019** (IBPSA), Volltext frei](https://publications.ibpsa.org/proceedings/bs/2019/papers/BS2019_210467.pdf) — SimStadt + DHWcalc, Quartier Stuttgart:

- Konstanter TWW-Ansatz **unterschätzt** Morgenspitzen werktags um bis zu **−537 %**, mittags −125 %, abends am Wochenende bis −253 %; **überschätzt** nachts um bis zu +95 %.
- Spitzenlast: konstanter Ansatz **406 kW**, mit Zapfprofil **612 kW** (**+50 %**).
- Mit Profilen fällt die geordnete Jahresdauerlinie nach 8279 h auf null → **481 h/a ohne Wärmebedarf**, im Konstantmodell unsichtbar.
- ⚠️ Die Autoren merken selbst an: **kein Abgleich mit Felddaten** in dieser Studie.

**Fazit:** Profile gegen Konstantwerte → Spitzen **steigen**. Profile/Normen gegen **Messung** → Spitzen sind **deutlich zu hoch**. Beides gleichzeitig wahr, weil die Fehlerquellen verschieden sind (zeitliche Auflösung vs. Gleichzeitigkeit/Verbrauchsniveau).

### 9.3 Zum P90-Befund — ehrliche Einordnung

**Ich habe keine Publikation gefunden, die explizit sagt „die gemessene Spitze entspricht dem P90-Punkt der synthetischen Jahresdauerlinie".** Das ist eine echte Lücke in der Literatur — Ihr Befund ist insofern originär und publikationswürdig.

Was die Literatur indirekt stützt: Der IAPMO-Ansatz arbeitet konsequent mit dem **99. Perzentil** (Q₀,₉₉) statt mit einem absoluten Maximum — d. h. der Stand der Technik in der Trinkwasserbemessung ist bereits **perzentilbasiert**, weil das absolute Maximum synthetischer Verteilungen als Artefakt der Verteilungsannahme gilt (insbesondere der unbegrenzten Gauß-Schwänze).

**Mechanistische Erklärung Ihres Befunds (meine Hypothese, klar als solche gekennzeichnet):** Bei VDI 4655 entsteht die synthetische Spitze durch die **Multiplikation** von Typtag-Faktor × Personenzahl × Normprofil. Da alle Wohneinheiten dasselbe deterministische Typtagsprofil erhalten, sind sie **perfekt korreliert** — die Gleichzeitigkeit ist implizit 100 %. Real dekorreliert das Verhalten, die effektive Spitze fällt auf μ + z·σ/√N. Bei typischen MFH-Größen (N ≈ 6–20 WE) ergibt √N ≈ 2,4–4,5 — **das erklärt Ihren Faktor 2,4 quantitativ.** Und weil das synthetische Profil dieselbe Gesamtenergie hat, aber die Spitzen künstlich konzentriert, liegt die reale Spitze zwangsläufig irgendwo im oberen Dezil der synthetischen Dauerlinie.

**Prüfbare Vorhersage:** Wenn diese Hypothese stimmt, sollte Ihr Faktor **mit √N skalieren** — bei einem EFH nahe 1,0, bei 20 WE nahe 4,5. Das lässt sich an Ihren Messdaten direkt testen und wäre der beste Validierungsschritt.

### 9.4 Weitere Literatur zur Quantifizierung

- [De Santiago, Rodriguez-Ubinas et al., „The generation of domestic hot water load profiles in Swiss residential buildings through statistical predictions", *Energy and Buildings* 141 (2017), doi:10.1016/j.enbuild.2017.06.030](https://doi.org/10.1016/j.enbuild.2017.06.030) — Postprint frei via DiVA: [uu.diva-portal.org/smash/get/diva2:1090222/FULLTEXT02](https://uu.diva-portal.org/smash/get/diva2:1090222/FULLTEXT02). (Beim automatisierten Abruf robots-gesperrt — Inhalt nicht selbst verifiziert, Angabe daher **unsicher**.)
- [Amanowicz, „Peak Power of Heat Source for DHW Preparation for Residential Estate in Poland…", *Energies* 14(23):8047, 2021, doi:10.3390/en14238047](https://doi.org/10.3390/en14238047) — **Open Access (CC-BY).** Vergleicht mehrere Auslegungsverfahren; Kernbotschaft: in energieeffizienten Neubauten kann die **TWW-Spitzenleistung die Heiz-/Lüftungsleistung erreichen oder übersteigen**; ausdrückliche Empfehlung für **Speichersysteme**, weil diese „less sensitive to design errors" sind. Enthält den „coefficient of non-simultaneous consumption of hot water" φ als Funktion der Armaturenzahl. **Sehr gut passende Referenz für Ihre Auslegungsfunktion.**
- [„Characteristics of Domestic Hot Water Consumption Profiles in Multi-Family Buildings for Energy Modeling Purposes", *Energies* 18(17):4578, 2025, doi:10.3390/en18174578](https://doi.org/10.3390/en18174578) — **Open Access.** 42 MFH, **1376 Wohnungen**, Polen (Wrocław, Zawidów), 2012–2017. Morgenspitze ≈ 18 % des Tagesbedarfs (7–8 Uhr), Abendspitze ≈ 45 % (20–23 Uhr), Wochenende deutlich flacher. Kritisiert lineare Flächenbezugsmodelle (Fehler bis 20 %) zugunsten eines Wohnungsstruktur-Modells (~8 % Fehler).

---

## 10. Messdatensätze zur Validierung

| Datensatz | Inhalt | Auflösung | Umfang | Zugang / Lizenz |
|---|---|---|---|---|
| **Carleton / Edwards et al. — Repräsentative TWW-Zapfprofile** | **Gemessene** TWW-Profile, Québec (CA); 12 repräsentative Profile aus 4 Verbrauchsniveaus × 3 zeitlichen Mustern | **5 min** | **73 Häuser** (EFH) | **Frei, Direktdownload** — [carleton.ca/sbes — Hot water demand profiles](https://carleton.ca/sbes/publications/hot-water-demand-profiles-downloadable/). Bedingung: Zitieren von [Edwards, Beausoleil-Morrison & Laperrière, *Solar Energy* 111:43–52, 2015](https://doi.org/10.1016/j.solener.2014.10.026). **Beste sofort nutzbare Validierungsquelle.** |
| **IAPMO Peak Water Demand Study** | Gesamtwasser-Entnahmeereignisse (nicht TWW-getrennt), 9 US-Staaten | **10 s** | **1038 EFH**, 863 000 Ereignisse, 11 385 Haus-Tage | Studie frei als [PDF](https://iapmo.org/media/42ehgafw/peak-water-demand-full-study.pdf); Rohdaten nicht offen. **Beste Quelle für Ereignisstatistik/Gleichzeitigkeit.** |
| **WEUSEDTO** (Water End USE Dataset and TOols) | Offener Wasser-Endverbrauchsdatensatz + Analysetools | k. A. (verifiziert nicht möglich) | k. A. | [SoftwareX 2022, doi:10.1016/j.softx.2022.101144](https://doi.org/10.1016/j.softx.2022.101144). ⚠️ Abruf robots-gesperrt — **Details ungeprüft**, aber vielversprechend. |
| **Energies 18(17):4578 (2025)** | Monats-/Tages-/Stundenprofile MFH, Polen | Stunde | **42 Gebäude, 1376 Wohnungen**, 4–5 Jahre | Paper CC-BY; **Datenverfügbarkeit im Paper nicht ausgewiesen** → Autorenanfrage nötig |
| **HEAPO** | Wärmepumpen-Optimierung, Smart-Meter + Vor-Ort-Inspektionsprotokolle, Schweiz | Smart-Meter | groß | [github.com/tbrumue/heapo](https://github.com/tbrumue/heapo) · [ACM e-Energy 2025, doi:10.1145/3679240.3734637](https://doi.org/10.1145/3679240.3734637). **TWW nicht separat** — nur WP-Gesamtstrom. Für TWW-Validierung nur bedingt. |
| **WPuQ — EFH- und WP-Lastprofile Deutschland** | Elektrische EFH- und Wärmepumpen-Lastprofile | 10 s bis 1 h | 38 Haushalte, Niedersachsen | [Scientific Data 9, 56 (2022), doi:10.1038/s41597-022-01156-1](https://www.nature.com/articles/s41597-022-01156-1), offen. **TWW nicht separat**, aber deutsche Referenz für Gleichzeitigkeitsvalidierung auf Quartiersebene. |
| **IEA HPT Annex 46 — DHW Heat Pumps** | Länderberichte, u. a. Schweiz | — | — | [OST-Bericht (PDF)](https://www.ost.ch/fileadmin/dateiliste/3_forschung_dienstleistung/institute/ies/projekte/projekte_tes/126_annex_46/overview_on_r_and_d_switzerland_-_annex_46_dhwhp_task_4_.pdf) — Übersichtsdokument, keine Rohdaten |
| **Hochauflösende Einzelzapfstellen-Messung EFH** | Individuelle Zapfstellen (Dusche, Küche, Bad separat) | hoch | mehrere EFH | [J. Phys. Conf. Ser. 3140:112002, doi:10.1088/1742-6596/3140/11/112002](https://doi.org/10.1088/1742-6596/3140/11/112002). ⚠️ IOPscience robots-gesperrt, **Details ungeprüft**. Konzeptionell die einzige Quelle, die die **4 DHWcalc-Kategorien direkt validieren** könnte. |
| **Smart-Meter End-Use-Charakterisierung** | Warmwasser auf Haushalts- und Endnutzungsebene | Smart-Meter | — | [*Water* 17:1906 (2025)](https://ui.adsabs.harvard.edu/abs/2025Water..17.1906M/abstract) |
| **B2E / Vancouver MURB-Studie** | Gemessene TWW-Daten aus **37 MURBs**, Vancouver | Durchflussmesser | 37 Gebäude | Referenziert in [b2electrification.org](https://b2electrification.org/right-sizing-matters-retrofitting-domestic-hot-water-murbs); Primärdaten über City of Vancouver anzufragen |
| **DHWcalc-Referenzprofile** | Synthetisch, aber verifiziert gegen Original-DHWcalc | 1/15/60 min | ~50 Dateien, 160–2000 l/d | [OpenDHW `DHWcalc_Files/`](https://github.com/RWTH-EBC/OpenDHW/tree/main/DHWcalc_Files), **MIT** — für Regressionstests Ihrer Implementierung ideal |

**⚠️ Zusammenfassende Bewertung der Datenlage:** Es gibt **keinen** großen offenen **deutschen** TWW-Messdatensatz mit Zapfstellen-Auflösung. Die Lage ist:
- **Beste Einzelquelle für Profilform:** Carleton/Québec (5 min, 73 Häuser, sofort frei)
- **Beste Quelle für Gleichzeitigkeit:** IAPMO (aggregierte Statistik) + Braas et al. (TWW-spezifisch, DACH)
- **Deutsche Feldmessungen** (Wohnungswirtschaft, Heizkostenabrechner wie Techem/ista/Brunata) sind **nicht offen** — ggf. über Kooperation/Kauf. Das ist der wahrscheinlich wertvollste ungehobene Datenschatz für Ihr Produkt und Ihr eigener Messdatensatz ist entsprechend wertvoll.

---

## 11. Bewertungstabelle

Legende Lizenz-Ampel: 🟢 kommerziell unproblematisch · 🟡 prüfen · 🔴 nicht verwendbar

| Werkzeug | Nutzungsarten | Zeitauflösung | Eingaben (minimal) | Lizenz | Python-Integration | Validierung | Gesamt |
|---|---|---|---|---|---|---|---|
| **OpenDHW** | 4 WG + **10 NWG** | 1 min – 1 h, frei | Personenzahl, l/(P·d), Gebäudetyp, Feiertage | 🟢 **MIT** | ⭐⭐⭐⭐⭐ pip, pandas nativ | 🟡 vs. DHWcalc verifiziert, keine Messvalidierung publiziert | **⭐⭐⭐⭐⭐ Kern-Engine** |
| **demandlib (VDI 4655)** | EFH, MFH | **1 min** / 15 min | Q_TWW_a, N_Pers, N_WE, TRY-Region | 🟢 MIT (⚠️ VDI-Tabellen-Urheberrecht prüfen) | ⭐⭐⭐⭐⭐ pip, aktiv | 🟢 normkonform, aber Typtag-Modell | **⭐⭐⭐⭐⭐ Normbezug** |
| **lpagg** | EFH/MFH (VDI 4655) | 15 min | YAML, DWD-Wetter | 🟢 MIT | ⭐⭐⭐⭐ | 🟡 | ⭐⭐⭐⭐ Aggregation + Gleichzeitigkeit |
| **DHWcalc (Original)** | Wohngebäude | 1/6/60 min | Tagesvolumen, Kategorien | 🔴 **keine Lizenzangabe** | ⭐ Windows-GUI, keine API | 🟢 IEA-SHC-Referenz, breit zitiert | ⭐⭐ nur als Referenz |
| **LPG + pylpg** | WG (verhaltensbasiert) | 1 min | Haushaltstyp, Personen | 🟢 **MIT**, kommerziell explizit erlaubt | ⭐⭐⭐ pylpg wrappt Binary | 🟢 JOSS peer-reviewed | ⭐⭐⭐⭐ **offline** als Profilquelle |
| **DOE Ref. Buildings / ASHRAE 90.1** | **16 NWG-Typen** | 1 h (Schedules) | Gebäudetyp, Fläche/Zimmerzahl | 🟢 US-Regierung, frei | ⭐⭐⭐ IDF/JSON parsen | 🟢 CBECS-basiert, ⚠️ US-Niveaus | ⭐⭐⭐⭐ **NWG-Breite** |
| **DIN EN 12831-3 (+/A100)** | **alle** Gebäudetypen | Summenlinie 24 h | Zapfstellen/Nutzungsart | 🟡 Norm kostenpflichtig | ⭐⭐ selbst implementieren | 🟢 normativ | ⭐⭐⭐⭐ **Auslegungsnorm** |
| **SIA 2024 (via cesar-p)** | viele NWG-Typen | Belegungsprofile | Raumtyp, Fläche | 🟡 Norm kostenpflichtig | ⭐⭐⭐ | 🟢 CH-normativ | ⭐⭐⭐ DACH-NWG-Alternative |
| **pysimdeum** | **nur WG** | hoch | Regionalstatistik (NL) | 🟡 **EUPL-1.2 Copyleft** | ⭐⭐⭐⭐⭐ xarray-API | 🟢 SIMDEUM breit validiert (Kaltwasser) | ⭐⭐ NWG fehlt + Lizenzrisiko |
| **districtgenerator** | 4 WG + 4 NWG | konfigurierbar | TABULA-Parameter | 🟢 MIT | ⭐⭐ schwerer Stack | 🟡 | ⭐⭐⭐ Ideengeber |
| **EnTiSe** | generisch, DHW-Modul | konfigurierbar (h) | Objekt + Wetter | 🟢 MIT | ⭐⭐⭐⭐ | 🟡 sehr jung | ⭐⭐⭐ Architekturvorbild |
| **StROBe** | WG | 1 min | Haushaltsparameter | 🔴 **KEINE Lizenz** | ⭐⭐ unmaintained seit 2021 | 🟢 peer-reviewed | 🔴 **ausschließen** |
| **CREST** | WG (UK) | 1 min | UK-Haushaltsparameter | 🟡 unklar (vmtl. CC-BY) | ⭐ Excel/VBA | 🟢 peer-reviewed | ⭐⭐ nur Referenz |
| **hotmaps** | WG + Tertiär | 1 h, **NUTS2** | Region | 🟢 CC-BY-4.0 | ⭐⭐⭐⭐ CSV | 🟢 EU-Projekt | ⭐⭐ zu grob für Gebäude |
| **tsib** | WG | 1 h | Gebäudeparameter | 🟢 MIT | ⭐⭐ unmaintained seit 2023 | 🟡 | ⭐⭐ |
| **TEASER** | — | — | — | 🟢 MIT | — | — | ⭐ kein TWW-Generator |
| **SimBench / ARegV** | — | — | — | — | — | — | ❌ **nicht einschlägig** |

---

## 12. Konkrete Architekturempfehlung für INEKON

**Dreischichtig, mit minimalen Eingaben und guten Vorgabewerten:**

**Schicht 1 — Nutzungsart → Bedarfskennwerte (Lookup-Tabelle, eigenes IP)**
- WG: Vorgabe **40 l/(P·d)** bei 60 °C (DHWcalc/SIA-2024-Konsens; OpenDHW-ReadMe belegt die Abgrenzung zu 123 l/(P·d) Gesamtwasser)
- NWG: aus DOE Table 11 (l/h-Spitzen, oben vollständig extrahiert), auf deutsches Niveau skaliert; alternativ SIA 2024 / DIN EN 12831-3/A100
- Einzige Pflichteingabe: **Nutzungsart + Größenmaß** (Personen / WE / Zimmer / m² / Betten). Alles andere als überschreibbare Defaults.

**Schicht 2 — Stochastischer Zapfprofilgenerator: OpenDHW (MIT), geforkt/vendored**
- 1-min-Generierung, dann Resampling auf 15 min / 1 h für die Bilanz
- **Kritisch:** Für N Wohneinheiten **N unabhängige Profile** generieren und superponieren, **nicht** ein Profil skalieren
- Eigene Nutzungsarten durch Erweiterung von `prob_nonresidential.json` (DOE-Schedules als Stufenfunktionen einspeisen)
- σ-Werte der Kategorien C/D gegen das DHWcalc-Paper (σ = 2 l/min) prüfen und ggf. korrigieren
- Regressionstests gegen `DHWcalc_Files/` im Repo

**Schicht 3 — Auslegungsschicht (Gleichzeitigkeit + Perzentil)**
- **Perzentilbasiert statt Maximum:** Auslegung auf **P99** der superponierten Last (IAPMO-Praxis), nicht auf das absolute Maximum. Ihr P90-Befund legt nahe, für Erzeugerleistung sogar P95–P99 zu prüfen und das Ergebnis gegen die N-abhängige Erwartung μ + z·σ/√N zu plausibilisieren.
- **Topologieabhängiger GLF** nach Braas et al. 2020: getrennte Kurven für Speichersystem / Frischwasserstation / Durchflusserwärmer. Die Tabelle in 8.3 ist direkt als Stützstellen verwendbar.
- **Winter-Formel** (Koeffizienten in 8.2) nur für **Gesamtwärme im Netzkontext**, nicht für reines TWW — das ist ein häufiger Fehler.
- **Normabgleich** gegen DIN EN 12831-3 Summenlinienverfahren als Plausibilitäts-Guardrail. Erwartung: Ihre stochastische Auslegung sollte **unter** DIN 4708 und **nahe/leicht unter** EN 12831-3 landen.

**Validierungsplan**
1. Carleton/Québec-Profile (5 min, 73 Häuser, sofort frei) gegen OpenDHW-Output: Zapfvolumen-Verteilung, Tagesgang, Ereignisdauern
2. Ihre eigenen Messdaten: prüfen, ob der Überschätzungsfaktor **mit √N skaliert** (Test der Hypothese aus 9.3) — das ist der entscheidende und publikationswürdige Schritt
3. Auslegungsergebnisse gegen DIN 4708 / EN 12831-3 für ein Referenz-MFH (Erwartung ≈ IKZ-Werte: 60 / 30 / 20 kW bei 11 WE)

**Zu klärende Rechtsfragen (vor Auslieferung)**
- VDI-4655-Tabellen in demandlib: Nutzungsrecht in einem verkauften Produkt
- DHWcalc-Referenzdateien (über OpenDHW MIT verteilt — vermutlich unproblematisch, aber der Ursprungsstatus ist unklar)
- Falls pysimdeum doch genutzt wird: EUPL-1.2-Copyleft-Reichweite

---

## Quellenverzeichnis (Auswahl, alle geprüft)

**Modelle & Software**
- Jordan, U. & Vajen, K.: *Realistic Domestic Hot-Water Profiles in Different Time Scales*, IEA SHC Task 26, 2001. https://sel.me.wisc.edu/trnsys/trnlib/iea-shc-task26/iea-shc-task26-load-profiles-description-jordan.pdf
- Jordan, U. & Vajen, K.: *DHWcalc: Program to Generate Domestic Hot Water Profiles with Statistical Means for User Defined Conditions*, ISES SWC 2005. https://solar-publikationen.umwelt-uni-kassel.de/uploads/2005%20ISES-SWC%20Jordan%20und%20Vajen%20Program%20to%20Generate%20Domestic%20Hot%20Water%20Profiles%20with%20Statistical%20Means%20for%20User%20Defined%20Conditions.pdf
- Uni Kassel, Solar- und Anlagentechnik — Downloads (DHWcalc 2.02b). https://www.uni-kassel.de/maschinenbau/en/institute/thermische-energietechnik/fachgebiete/solar-und-anlagentechnik/downloads
- OpenDHW (RWTH-EBC), MIT. https://github.com/RWTH-EBC/OpenDHW · https://pypi.org/project/OpenDHW/
- demandlib / oemof-demand, MIT. https://github.com/oemof/demandlib · https://demandlib.readthedocs.io/ · https://oemof.org/2025/04/11/demandlib-0-2-2-vdi-and-bdew25/
- lpagg, MIT. https://github.com/jnettels/lpagg
- districtgenerator (RWTH-EBC), MIT. https://github.com/RWTH-EBC/districtgenerator
- LoadProfileGenerator. https://www.loadprofilegenerator.de/ · FAQ: https://www.loadprofilegenerator.de/faq/ · https://github.com/loadprofilegenerator/LoadProfileGenerator
- Pflugradt, N. et al.: *LoadProfileGenerator: An Agent-Based Behavior Simulation…*, JOSS 2022. https://doi.org/10.21105/joss.03574
- pylpg (FZJ-IEK-3), MIT. https://github.com/FZJ-IEK3-VSA/pylpg
- pysimdeum (KWR), EUPL-1.2. https://github.com/KWR-Water/pysimdeum · https://pysimdeum.readthedocs.io/
- StROBe (KU Leuven) — **ohne Lizenz**. https://github.com/open-ideas/StROBe
- CREST Demand Model (Loughborough). https://repository.lboro.ac.uk/articles/dataset/CREST_Demand_Model_v2_0/2001129
- McKenna, E. & Thomson, M.: *High-resolution stochastic integrated thermal–electrical domestic demand model*, Applied Energy 2016. https://doi.org/10.1016/j.apenergy.2015.11.089
- EnTiSe (TUM ENS), MIT. https://github.com/tum-ens/EnTiSe · https://entise.readthedocs.io/
- tsib (FZJ-IEK-3), MIT. https://github.com/FZJ-IEK3-VSA/tsib
- Hotmaps CM Heat load profiles, CC-BY-4.0. https://wiki.hotmaps.eu/en/CM-Heat-load-profiles
- cesar-p SIA 2024 DHW. https://cesar-p-core.readthedocs.io/en/latest/features/sia2024-dhw-demand.html

**Normen & Auslegung**
- Deru, M. et al.: *U.S. DOE Commercial Reference Building Models of the National Building Stock*, NREL/TP-5500-46861, 2011. https://docs.nlr.gov/docs/fy11osti/46861.pdf
- IKZ: *DIN EN 12831-3 als Ersatz für DIN 4708*. https://www.ikz.de/fileadmin/user_upload/008_011.pdf
- DIN: Pressemitteilung zur nationalen Ergänzung DIN EN 12831-3/A100. https://www.din.de/de/mitwirken/normenausschuesse/nhrs/pressemitteilung-nationale-ergaenzung-zur-din-en-12831-3--297950
- SHKwissen: Bedarfskennzahl / Leistungskennzahl (DIN 4708). https://www.haustechnikdialog.de/SHKwissen/2005/Bedarfskennzahl-Leistungskennzahl
- VDI 4655 Richtlinienseite. https://www.vdi.de/richtlinien/details/vdi-4655-referenzlastprofile-von-wohngebaeuden-fuer-strom-heizung-und-trinkwarmwasser-sowie-referenzerzeugungsprofile-fuer-fotovoltaikanlagen

**Gleichzeitigkeit & Validierung**
- Winter, W., Haslauer, T. & Obernberger, I.: *Untersuchungen der Gleichzeitigkeit in kleinen und mittleren Nahwärmenetzen*, Euroheat & Power 9&10/2001. https://www.verenum.ch/Dokumente/2001_Winter-Gleichzeitig.pdf
- Braas, H. et al.: *District heating load profiles for DHW preparation with realistic simultaneity using DHWcalc and TRNSYS*, Energy 201 (2020) 117552. https://doi.org/10.1016/j.energy.2020.117552 · https://heatbeat.de/de/blog/2/
- Weißmann, C., Hong, T. & Graubner, C.-A.: *Analysis of heating load diversity in German residential districts…*, Energy and Buildings 139 (2017). https://doi.org/10.1016/j.enbuild.2016.12.096 · https://www.osti.gov/biblio/1416777
- IAPMO Study 4-2024: *Peak Water Demand Study*. https://iapmo.org/media/42ehgafw/peak-water-demand-full-study.pdf
- Buchberger, S. et al.: *Improved Estimates of Peak Water Demand in Buildings*, Ohio WRC 2016. https://wrc.osu.edu/sites/default/files/2022-04/Buchberger_2016OH506O_FinalReport.pdf
- Weiler, V. & Eicker, U.: *Individual Domestic Hot Water Profiles for Building Simulation at Urban Scale*, BS2019. https://publications.ibpsa.org/proceedings/bs/2019/papers/BS2019_210467.pdf
- Amanowicz, Ł.: *Peak Power of Heat Source for DHW Preparation…*, Energies 14(23):8047, 2021. https://doi.org/10.3390/en14238047
- *Characteristics of DHW Consumption Profiles in Multi-Family Buildings…*, Energies 18(17):4578, 2025. https://doi.org/10.3390/en18174578
- B2E: *Right-Sizing Matters: Retrofitting Domestic Hot Water in MURBs*, 2025. https://b2electrification.org/right-sizing-matters-retrofitting-domestic-hot-water-murbs
- *Sizing of district heating systems based on smart meter data…*, Energy (2020). https://doi.org/10.1016/j.energy.2019.116780
- nPro: Gleichzeitigkeitsfaktor in Wärmenetzen. https://www.npro.energy/main/de/district-heating-cooling/diversity-factor

**Messdaten**
- Edwards, S., Beausoleil-Morrison, I. & Laperrière, A.: *Representative hot water draw profiles at high temporal resolution…*, Solar Energy 111:43–52, 2015. https://doi.org/10.1016/j.solener.2014.10.026 · Download: https://carleton.ca/sbes/publications/hot-water-demand-profiles-downloadable/
- WPuQ: *Dataset on electrical single-family house and heat pump load profiles in Germany*, Scientific Data 9, 56 (2022). https://doi.org/10.1038/s41597-022-01156-1
- HEAPO. https://github.com/tbrumue/heapo · https://doi.org/10.1145/3679240.3734637
- IEA HPT Annex 46 — DHW Heat Pumps (CH). https://www.ost.ch/fileadmin/dateiliste/3_forschung_dienstleistung/institute/ies/projekte/projekte_tes/126_annex_46/overview_on_r_and_d_switzerland_-_annex_46_dhwhp_task_4_.pdf

---

**Explizit gekennzeichnete Unsicherheiten:** DHWcalc-Lizenzstatus (keine publizierten Bedingungen); VDI-4655-Tabellen-Urheberrecht innerhalb demandlib; CREST-Lizenz (Figshare, nicht eindeutig auslesbar); IEA-SHC-Task-26-Profildateien in TRNLIB (Abruf 403-gesperrt); Inhalte von WEUSEDTO, De Santiago et al. 2017 und J. Phys. Conf. Ser. 3140:112002 (robots-gesperrt, nicht selbst verifiziert); Gebäudetypenliste in DIN EN 12831-3/A100 (kostenpflichtig, nicht verifiziert); die √N-Erklärung Ihres 2,4×-Befunds in 9.3 ist **meine Hypothese**, nicht Literaturbefund. Für keine Aussage wurde eine Quelle erfunden.agentId: a7f5027c3735a7573 (use SendMessage with to: 'a7f5027c3735a7573', summary: '<5-10 word recap>' to continue this agent)
<usage>subagent_tokens: 175718
tool_uses: 106
duration_ms: 1062651</usage>