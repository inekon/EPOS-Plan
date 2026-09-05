# Paket A — Zeitbasis (B1) und Modulstammdaten (Stufe E1)

**Umsetzungsprotokoll, 02.09.2026, Branch `ios_migration`**

Grundlage: `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` (Repo-Wurzel) — Befund **B1**
(Abschnitt 4 und Nachtrag 1), Stufe **E1** (Abschnitt 3), Beauftragung **Nachtrag 2**
(Entscheidungen Q1–Q3, Reihenfolge N2.6).

| | |
|---|---|
| **Commits** | `36c5401` PA1a (Datenmodell) · `aced014` PA1b (Rechenweg) · `7c622b1` PA1c (Oberfläche) · dieser Commit PA1 (Referenzlauf + Protokoll) |
| **Referenzbasis vorher** | `Referenzlaeufe/2026-09-02_PA0_vor-PaketA/` (Codestand `d46e200`, Ablage `df90063`) |
| **Referenzbasis nachher** | `Referenzlaeufe/2026-09-02_PA1_nach-PaketA/` (Codestand `7c622b1`) |
| **Schemastand** | 61 → **62** (Migrationsschritt 62, erster Schritt des SQLite-Zweigs) |
| **Prüfbuild** | MSBuild x64 Debug, **0 Fehler**; Warnungsprofil **unverändert** zum Bestand (CS0108 2, CS0109 2, NU1510 2, WFO0003 1, WFO1000 30 — in beiden Ständen identisch) |

---

## 1. Auftrag

Zwei Dinge in einem Paket, weil beide dieselben Dateien berühren:

* **B1 — Zeitbasis.** `Tab_Solar(_STAMM)` liegt im **UTC**-Raster (PVGIS `time(UTC)`, Ablage in
  Empfangsreihenfolge, keine Zeitspalte), Lastgänge und Bedarfsprofile laufen in **Ortszeit**.
  Erzeugung und Bedarf standen sich damit 1 h (Winter) bzw. 2 h (Sommer) zu früh gegenüber:
  Jahressummen richtig, Eigenverbrauchsquote, Autarkie, Speicherfahrweise, Solarthermie-Deckung
  und die § 51-Zuordnung zur Spotreihe systematisch verschoben.
* **E1 — „Eine Wahrheit".** P_STC statt Fläche × Wirkungsgrad (E1.1), `T_NOCT` statt fest
  verdrahteter 45 °C (E1.2), Wechselrichter und Systemverluste als Anlagenparameter (E1.3),
  1-basierter Tagindex (E1.4), γ-Plausibilität und vollständiges `Init()` (E1.5).

---

## 2. Änderungen

### 2.1 B1 — der Ortszeit-Lesepfad

| Datei | Was |
|---|---|
| `Allgemein/Simulation/SolarZeitbasis.cs` (**neu**, 182 Zeilen) | Die Verschiebungsregel, **ohne Datenbank testbar**. `UtcIndex(L, jahr) = L − Offset(L)`, Offset 1 (MEZ) / 2 (MESZ), Jahresumlauf für `L < Offset`. `Zuordnung(jahr)` liefert das ganze Feld, `TagSommerzeitBeginn/Ende` die Umstelltage, `UmstelltageText` den Protokolltext. |
| `Controller/SolardatenCtrl.cs:145-289` | `ReadOrtszeit(idKlimaregion, idProjekt, stamm = false)` — der EINE Lesepfad; dazu `Referenzjahr(idProjekt)`, `Leeren()`, `Uebernehmen()`. |
| `Model/KlimadatenModel.cs:47-70` | `SolardatenModel.TagUtc` (1-basiert) und `.StundeUtc` — die UTC-Herkunft reist an der Zeile mit. |
| `Allgemein/DbWerte.cs:1974-1993` | `SOLAR_REFERENZJAHR_STANDARD = 2025`. |

**Die vier stundenscharfen Leser** (alle vorher mit eigenem SQL im UTC-Raster):

| Leser | vorher | nachher |
|---|---|---|
| `Allgemein/Simulation/SimulationPV.cs:190` | `ReadAll("select * from Tab_Solar where ID_Klimaregion=…")` | `ReadOrtszeit(nID_Klimaregion, ID_Projekt)` |
| `Allgemein/Simulation/SimulationSolarthermie.cs:239-240` | dito | `ReadOrtszeit((int)nID_Klimaregion, m_ID_Projekt)` |
| `Allgemein/Simulation/SimulationWaermebedarf.cs:841-853` | eigenes `RecordSet`-SQL | `ReadOrtszeit(ID_Klimaregion, m_ID_Projekt)` |
| `Views/Simulation/Form_Simulation_Config.Uebersicht.cs:521-556` | eigenes `GetDataTable`-SQL | `ReadOrtszeit(idRegion, m_ID_Projekt)` |

`Views/Klimadaten/Form_Klimadaten.cs:97` bleibt **bewusst UTC** — die Maske zeigt den
Importbestand einer Stammregion und ist die Kontrollansicht gegen die PVGIS-Quelle, kein
Rechenweg. Der Grund steht als Kommentar an der Stelle.

**Die Regel im Klartext.** Ortszeit-Index `L` (0…8759) → UTC-Index `U = L − Offset(L)`; MESZ gilt
nach EU-Regel vom letzten Märzsonntag 02:00 Ortszeit (= 01:00 UTC) bis zum letzten
Oktobersonntag 03:00 Ortszeit (= 01:00 UTC). Die Zeitzonentabelle des Rechners wird **nicht**
befragt (`TimeZoneInfo` kommt nicht vor) — dieselbe Entscheidung und dieselbe Begründung wie in
`SpeicherEngine/GanglinienPruefung.cs:253-263`: Das Ergebnis eines Laufs darf nicht davon
abhängen, auf welchem Rechner er läuft.

**Die zwei Umstellstunden.** Das Raster hat feste 8.760 Fächer und eine glatte Ortszeitachse.
Daraus folgt zwangsläufig: im **Frühjahr** wird eine UTC-Stunde **doppelt** gelesen (die
Ortsstunde 02:00 gibt es nicht), im **Herbst** **entfällt** eine (die Ortsstunde 02:00 gibt es
zweimal, das Raster führt sie einmal). Für das Referenzjahr 2025 sind das die UTC-Indizes
**2112** (Tag 89, Stunde 0) und **7153** (Tag 299, Stunde 1) — beides **Nachtstunden**. Deshalb
bleibt die Jahressumme der Globalstrahlung exakt erhalten (im Harness auf 1e-6 nachgerechnet);
die Jahressumme der Außentemperatur verschiebt sich um genau diese zwei Werte
(Stuttgart −8,85 K auf 86 502 K, München −4,48 K auf 83 874 K — das sind 0,010 % bzw. 0,005 %).

**Das Referenzjahr** bestimmt ausschließlich die beiden Umstelltage. Erste Wahl ist das Jahr der
Spotpreisreihe des Projekts (aktive Speichervariante → `Tab_StromspeicherVariante.ID_Preisreihe`
→ `Tab_Preisreihe.Jahr`) — dann liegen Erzeugung und Preisreihe auf denselben Umstelltagen.
Ohne Reihe gilt `DbWerte.SOLAR_REFERENZJAHR_STANDARD = 2025`. **Kein `DateTime.Today`:** Sonst
rechnete derselbe Referenzlauf am Jahreswechsel andere Zahlen. `StromPreisCtrl.Stichtag` schied
aus demselben Grund aus — es liefert praktisch immer das heutige Jahr.

**Protokollmeldungen** (Schlüssel ASCII, global eindeutig, einmal je Lauf):

* `solar-zeitbasis-<Region>` — *„Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025,
  Umstellung 30.03./26.10."* (Hinweis; in allen 14 Referenzprojekten sichtbar).
* `solar-zeitraster-<Region>` — Warnung, wenn die Reihe **nicht** 8.760 Zeilen führt. Dann
  bleibt die Verschiebung **aus** und der Lauf rechnet mit der rohen Reihe weiter. Das ist
  zugleich die Zeilenzahlprüfung, die Befund **B4** verlangt hat (bis dahin gab es gar keine).

### 2.2 E1 — Modulstammdaten und Anlagenparameter

| Nr. | Was | Fundstelle |
|---|---|---|
| **E1.1** | `P_DC = P_STC[kW] · (G·cosθ / 1000) · (1 + γ(T_Zelle − 25))`; Rückfall auf die Flächenformel bei `Leistung ≤ 0` (+ Hinweis je Modul); Konsistenzhinweis, wenn `|P_STC − L·B·η·1000| / P_STC > 3 %` | `SimulationPV.BerechnePV`, `PStcDerAnlage` |
| **E1.2** | `T_Zelle = T_amb + (G/800)·(T_NOCT − 20)`; Rückfall 45 °C außerhalb **20…60 °C** (+ Hinweis je Modul) | `SimulationPV.BerechnePV`, `NoctDesModuls` |
| **E1.3** | `PV_WrWirkungsgrad` (NULL → 0,95) ersetzt die Konstante 0,95; danach `· (1 − PV_Systemverluste/100)` (NULL → 0) | `SimulationPV.Berechnung`, Migrationsschritt 62 |
| **E1.4** | `CalculateHourly` bekommt `TagUtc` (1-basiert) und `StundeUtc` statt `i/24` und `i%24` | `SimulationPV`, `SimulationSolarthermie` |
| **E1.5** | γ-Plausibilität als Hinweis/Warnung **ohne Rechenänderung**; `Init()` setzt zusätzlich `Stromproduktion_Max`, `MaxPSolar`, beide `*_gesamt` und die drei `*_viertelstunde`-Reihen zurück | `SimulationPV.GammaPruefen`, `SimulationPV.Init` |

**Warum ein NOCT-FENSTER und nicht „> 0".** Der Katalog ist an dieser Stelle vergiftet: In
**allen sechs** Modulen der Referenzmenge steht in `T_NOCT` (wie in `alpha_SC` und `beta_OC`) der
Wert von `I_Kurzschluss` — 9,34 / 9,42 / 9,014. Der ist **positiv** und liefe mit dem Kriterium
„> 0" glatt in die Formel: `(9,014 − 20)/800` ist negativ und ergäbe eine Zelltemperatur
**unter** der Außentemperatur bei Einstrahlung, also Mehrertrag statt der erwarteten ±0,5 %.
Mit dem Fenster 20…60 °C greift überall der Rückfall 45 °C — **E1.2 ist in dieser Referenzmenge
deshalb rechnerisch wirkungslos** und meldet je Modul einen Hinweis.

**Bitgleichheit der Schreibweise.** `T_Zelle` steht als `(G / 800) · (T_NOCT − 20)` da, nicht als
`(T_NOCT − 20)/800 · G`. Mit dem Rückfall 45 ist die erste Fassung **zeichengleich** zur alten
Zeile `(G/800)·25` und damit bitgleich; die algebraisch gleichwertige zweite wäre es im letzten
Bit nicht. Ebenso ist `erg · etaWr · systemFaktor` mit den Vorgaben 0,95 und 1,0 bitgleich zu
`erg · 0.95` (die Multiplikation mit 1,0 ist exakt).

### 2.3 Datenmodell und Persistenz (E1.3)

**Migrationsschritt 62** — der **erste Schritt des SQLite-Zweigs**:
`SchemaMigration.SCHRITT_62_PV_ANLAGENPARAMETER`, `Schritt_62_PvAnlagenparameter`, Eintrag in
`SCHRITTE_SQLITE`, `ZIEL_VERSION` 61 → 62. Der Schrittkörper benutzt ausschließlich
`SqliteSpalteAnlegen` mit Typ **`REAL`** (alle Tabellen des Zielschemas sind `STRICT` und lassen
bei `ADD COLUMN` nur INT/INTEGER/REAL/TEXT/BLOB/ANY zu). **Kein DML, kein DDL-DEFAULT auf einem
Fachwert** — beide Spalten bleiben NULL, und NULL heißt 0,95 bzw. 0 %.

> **Neue Konstante `SchemaMigration.FREEZE_VERSION_ACCESS = 61`.** Bis Paket A war der
> eingefrorene Access-Zweig-Endstand dieselbe Zahl wie das Ziel. Mit Schritt 62 laufen beide
> auseinander, und **zwei Stellen hätten sonst gebrochen**: (a) `DurchfuehrenAltbestand` prüft
> `StandNachher >= ZIEL_VERSION` — eine `.accdb` kann 62 nie erreichen, jede Alt-Hebung hätte
> Misserfolg gemeldet; (b) `SchritteAbarbeitenSqlite` weist `version < ZIEL_VERSION` als „nicht
> erstmigriert" ab — jede Datei auf Stand 61, also der Normalfall, wäre abgelehnt worden. Beide
> Stellen prüfen jetzt gegen den Freeze-Stand, das ERGEBNIS weiter gegen `ZIEL_VERSION`.

`SchemaKatalog.Schritt62_PvAnlagenparameter` steht — anders als Schritt 61 — **in
`SchemaKatalog.Alle`**. Das Kriterium ist der LESER, nicht die Tabelle: Der **Rechenkern liest
beide Spalten**, und genau dafür ist die Rückfallebene da (dieselbe Linie wie
`Schritt13_Mindestfuellstand`).

**Persistenz an allen vier Pflichtstellen, namensbasiert:**

| Stelle | Was |
|---|---|
| `Controller/WizardCtrl.cs:212-243` | `SQL_ANLAGE_INSERT` führt beide Spalten — **zwingend im INSERT**, weil der Speicherweg aller Erzeuger Löschen + Neuanlegen ist (`Form_Start`, `PVKontextMenuCtrl`); ein nachgelagertes UPDATE käme zu spät. |
| `Controller/WizardCtrl.cs:333-347` | `AnlagenParameter` — beide über `ProjektPuffer.Par` mit ausdrücklichem Typ, weil NULL hier der Normalfall ist. |
| `Controller/WErzeugerCtrl.cs:276-283` | `AusZeile` liest beide **ausdrücklich auch mit null**; eine fehlende Spalte gilt wie NULL, eine Datenbank vor Schritt 62 läuft unverändert weiter. |
| `Model/WErzeugerModel.cs:190-213` | `double? PV_WrWirkungsgrad`, `double? PV_Systemverluste` — nullable, weil „nie gepflegt" und „ausdrücklich 0" zwei Aussagen sind. |

Ergänzt: `Allgemein/Bericht/AbweichungsErmittler.cs:57-62` (Merkmalskatalog, beide Spalten).

### 2.4 Oberfläche

* **`Views/Photovoltaik/Form_PV.cs`** — dritte Spalte im Panel „PV Anlage Eigenschaften"
  (WR-Wirkungsgrad, Systemverluste [%]) mit Tooltips; Panel 308 → **420 px**. Der gestrichelte
  Rahmen in `Form_PV_Paint` liest Lage und Größe zur Zeichenzeit und folgt automatisch; rechts
  beginnt erst bei x = 449 die Herstellerspalte. **Leer = NULL = Vorgabewert.**
* **`Views/Photovoltaik/Form_AdminPV.cs`** — Feld „Zelltemperatur NOCT [°C]" in der linken
  Spalte unter den Knöpfen (der einzige freie Bereich; die rechte Wertespalte ist von y = 132
  bis zu den Knöpfen bei y = 449 durchgehend belegt).
* **MyResource**: 7 Schlüssel de **und** en (`PV_ANLAGE_LABEL_*`, `PV_ANLAGE_TIP_*`,
  `PV_MODUL_*`) plus die Properties in `Resource.Designer.cs`, alphabetisch zwischen
  `PSP_VOLLZYKLEN_KOMBI_TIP` und `PVW_51_ALTANLAGE` eingeordnet. Keine CS0102-Duplikate.

Beide Formulare bekommen ihre Felder **programmatisch** — Designer- und `.resx`-Dateien der
Formulare werden nicht von Hand editiert (CLAUDE.md des Hauptprojekts).

**Bestandsfehler dabei geschlossen (`Form_AdminPV`).** `btn_Speichern_Click` füllte ein
**frisches** `PhotovoltaikModel` allein aus den Maskenfeldern; `alpha_SC`, `beta_OC` und
`T_NOCT` blieben auf 0 und wurden von `PhotovoltaikStammCtrl.Update` mitgeschrieben. **Jedes
Speichern eines CEC-Moduls löschte damit genau die drei Katalogwerte, die der Import geliefert
hatte.** `T_NOCT` ist jetzt editierbar; `alpha_SC` und `beta_OC` werden aus dem geladenen
Datensatz erhalten (`_alphaScGeladen`, `_betaOcGeladen`).

---

## 3. Verifikation

### 3.1 Prüfbuild

MSBuild x64 Debug aus dem `git archive`-Export `P:\pa1\src` (Commit `7c622b1`) — **0 Fehler**.
Warnungsprofil **identisch** zum Bestandsstand `P:\pa0\src`:

| Code | Bestand (PA0) | Paket A (PA1) |
|---|---:|---:|
| CS0108 | 2 | 2 |
| CS0109 | 2 | 2 |
| NU1510 | 2 | 2 |
| WFO0003 | 1 | 1 |
| WFO1000 | 30 | 30 |

### 3.2 Headless-Harness `P:\pa1\harness` (kd1runner-Muster)

Eigenes Konsolenprojekt gegen die frisch gebaute `EPOS_Plan.dll`, Auflösung von verwalteten und
nativen Bibliotheken (`e_sqlite3.dll`) über `AssemblyLoadContext`. **Die produktive Datenbank
wurde nie beschrieben** — gerechnet wurde auf Kopien des Snapshots
`P:\pa0\Quelle\Kenndaten.sqlite` (MD5 `47bcefaca0f18d2180ba37786c6cb6b3`).

**(a) `SolarZeitbasis` rein rechnerisch — 18 PASS / 0 FAIL**

| Prüfung | Ergebnis |
|---|---|
| Umstelltage 2025 | **30.03. / 26.10.** = Tag 89 / Tag 299 im 365-Tage-Raster |
| Frühjahr 01:00 MEZ / 02:00 MESZ | Offset 1 / Offset 2 |
| Herbst 02:00 MESZ / 03:00 MEZ | Offset 2 / Offset 1 |
| Jahresumlauf | `L = 0 → U = 8759`, `L = 1 → U = 0` |
| Zielindizes | alle in 0…8759, 8760 Einträge |
| doppelt genutzte UTC-Stunde | **genau eine**: U = 2112 (Tag 89, Stunde 0) |
| entfallende UTC-Stunde | **genau eine**: U = 7153 (Tag 299, Stunde 1) |
| Unstetigkeiten der Zuordnung | **genau zwei** (die beiden Umstellstunden) |
| Schaltjahresfalle | 2024: 31.03. → Tag **90** (nicht 91), 27.10. → Tag **300** — gerechnet über Monat/Tag, nicht über `DayOfYear` |

**(b) Migrationsschritt 62 an einer DB-Kopie — 12 PASS / 0 FAIL**

Lauf 1: Stand 61 → **62**, beide Spalten angelegt, **0 belegte Zeilen** (kein DML).
Lauf 2: „bereits erledigt", **kein** erneutes DDL, Stand bleibt 62, beide Spalten genau einmal.

**(c) `ReadOrtszeit` an der DB-Kopie — 115 PASS / 0 FAIL** (14 Klimaregionen der
Referenzprojekte)

Mittlere Stunde des Tagesmaximums der Globalstrahlung:

| Region | Lon | roh Winter | roh Sommer | Ortszeit Winter | Ortszeit Sommer |
|---|---:|---:|---:|---:|---:|
| Stuttgart | 9,18 | 11,00 | 11,20 | **12,00** | **13,20** |
| München | 11,58 | 11,02 | 11,25 | **12,02** | **13,25** |

Verschiebung Winter **exakt +1 h**, Sommer **exakt +2 h** — genau die Erwartung des
Konzept-Nachtrags 1 (roh ≈ 11 in allen deutschen Regionen). Weiter geprüft je Region:
Summe Globalstrahlung roh = Ortszeit auf 1e-6 (Stuttgart 1 220 829,000 W/m², München
1 201 872,000 W/m²); `TagUtc` in 1…365 und `StundeUtc` in 0…23 ohne Ausreißer;
`(TagUtc − 1)·24 + StundeUtc == Zuordnung[L]` für alle 8.760 Zeilen; `ReadAll` lässt die
UTC-Felder unbesetzt (der rohe Lesepfad bleibt roh).

### 3.3 Referenzlauf PA1

```powershell
& $exe lauf --ziel <Referenzlaeufe\2026-09-02_PA1_nach-PaketA> `
            --quelle P:\pa0\Quelle\Kenndaten.sqlite `
            --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1026,1028,1029,1030,1039,1043
```

**14 von 14 erfolgreich**, 355 CSV, 24 Warnungen (alle Bestand, alle aus 1043 — identische
Menge wie in PA0), 0 Fehler, Gesamtdauer 21 s. Migration der Arbeitskopie **61 → 62**.

* **Selbstvergleich:** zweiter Lauf desselben Codes auf derselben Quelle →
  **14/14 PASS (3 882 476 Werte)**, **355/355 CSV byte-/MD5-gleich**. Die Basis ist
  reproduzierbar.
* **`pruefen`:** **GESAMT plausibel** (keine NaN/Inf, Rasterlängen 8760/35040 korrekt); die
  drei Hinweise „Jahressumme 0 — Gewerk aktiviert, aber kein Modul" bei 1007, 1039 und 1043
  sind der Bestand aus PA0.
* **Produktive Datenbank unberührt:** `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`,
  Zeitstempel **02.09.2026 22:07:36**, 67 706 880 Byte, **SchemaVersion 61**, in
  `Tab_Energieanlagen` weiterhin nur `PV_Leistung` als `PV_*`-Spalte.

---

## 4. Vergleich PA0 → PA1: jedes Delta erklärt

Der Toleranzvergleich meldet **FAIL in allen 14 Projekten** — und das ist die Ansage des
Konzepts: B1 verschiebt die Stundentemperatur in **jedem** Projekt, und die Toleranz der Suite
(1e-4 relativ) ist enger als jede der beabsichtigten Änderungen.

**Vollständiger Skalarvergleich** (jeder Schlüssel der 14 `aggregate.csv`, ohne die volatilen
Autowert-IDs): **391 geänderte Skalare in 14 Projekten** (1007: 36 · 1008: 41 · 1011: 21 ·
1017: 1 · 1018: 1 · 1021: 9 · 1023: 50 · 1024: 61 · 1026: 51 · 1028: 51 · 1029: 33 · 1030: 1 ·
1039: 2 · 1043: 33), jeder einer der vier Familien unten zuzuordnen. **Kein unerklärtes Delta,
und kein Schlüssel nur auf einer Seite** (der Vergleich meldet weder „nur in PA0" noch „nur in
PA1").

### 4.1 Familie 1 — die Stundentemperatur selbst (B1, 14 von 14 Projekten)

`Vektor.stundentemperatur.Summe` und `Vektor.wp_quellentemperatur.Summe` ändern sich um **genau
die zwei Umstellstunden**:

| Region | PA0 | PA1 | Δ | Δ relativ |
|---|---:|---:|---:|---:|
| Stuttgart (11 Projekte) | 86 502,49 | 86 493,64 | **−8,85 K** | −0,0102 % |
| München (1018, 1030) | 83 874,19 | 83 869,71 | **−4,48 K** | −0,0053 % |

**Das ist der unabhängige Gegenbeweis:** Der Harness misst dieselbe Differenz an derselben
Reihe (Abschnitt 3.2 c). Reihe und Rechenkern sind also nachweislich dasselbe verschoben worden.

### 4.2 Familie 2 — PV-Jahreserzeugung (E1.1 + E1.4)

| Projekt | Modul | Katalogfaktor `P_STC` vs. `L·B·η·1000` | PV theor. PA0 [kWh] | PA1 [kWh] | Δ |
|---|---|---:|---:|---:|---:|
| 1007 | Ablytek 6MN6A270 (Neigung 0°) | **+0,0332 %** | 6 053,139 | 6 055,971 | **+0,0468 %** |
| 1011 | Jinkosolar JKM 260P-60 (Neigung 0°, 2 Felder) | **0,0000 %** | 17 986,629 | 17 988,696 | **+0,0115 %** |
| 1026 / 1028 / 1029 | Jinkosolar JKM 260P-60 (Neigung 30°) | **0,0000 %** | 6 713,372 | 6 713,459 | **+0,0013 %** |
| 1043 | — (Gewerk ohne Modul) | — | 0,00 | 0,00 | **0** |

**Zuordnung.** Die Jinkosolar-Projekte belegen den **reinen E1.4-Anteil** (1-basierter
Tagindex): Ihr Katalog ist auf sieben Stellen konsistent, γ = 0 (kein Temperaturgang) und
`T_NOCT` fällt auf 45 °C zurück — E1.1 und E1.2 sind dort rechnerisch wirkungslos. Übrig bleibt
der Tagindex, und der wirkt geometrieabhängig: **+0,0115 %** bei Neigung 0° (1011),
**+0,0013 %** bei Neigung 30° (1026/1028/1029). Projekt 1007 ist die Summe aus dem
**Katalogfaktor +0,0332 %** und demselben Geometrieanteil (Neigung 0°, dazu ein kleiner
Beitrag des Temperaturgangs γ = −0,4509 %/K, der mit B1 andere Stundenpaare sieht):
0,0332 + 0,0115 + 0,002 ≈ **0,0468 %**. ✔

**Die Vorgabe des Konzepts ist damit eingehalten:** Die PV-Jahreserzeugung ändert sich um
höchstens die Katalogfaktoren (≤ 0,05 %). Ein T_NOCT-Effekt tritt **nirgends** auf — der
Rückfall greift in allen sechs Modulen (Abschnitt 2.2).

### 4.3 Familie 3 — die Paarung Erzeugung ↔ Bedarf (B1, der eigentliche Zweck)

| Projekt | Größe | PA0 | PA1 | Δ |
|---|---|---:|---:|---:|
| **1007** | PV genutzt [kWh] | 5 202,64 | 5 147,70 | **−1,06 %** |
| | PV Überschuss [kWh] | 850,50 | 908,28 | **+6,79 %** |
| | Netzbezug [kWh] | 50 834,71 | 50 924,47 | +0,18 % |
| | **Eigenverbrauchsquote** | 85,95 % | 85,00 % | **−0,95 pp** |
| | **Autarkiegrad** | 9,28 % | 9,18 % | **−0,10 pp** |
| **1026 / 1028** | PV genutzt [kWh] | 4 334,47 | 4 338,23 | +0,09 % |
| | PV Überschuss [kWh] | 2 378,91 | 2 375,23 | −0,15 % |
| | **Eigenverbrauchsquote** | 64,56 % | 64,62 % | **+0,06 pp** |
| | Speicherfüllstand (Σ) [kWh] | 18 993,35 | 18 572,54 | **−2,22 %** |
| | Puffer-Durchsatz PV-Ladung [kWh] | 14,26 | 12,99 | **−8,91 %** |
| **1029** | PV genutzt [kWh] | 4 155,01 | 4 163,43 | +0,20 % |
| | PV Überschuss [kWh] | 2 558,37 | 2 550,03 | −0,33 % |
| | **Eigenverbrauchsquote** | 61,89 % | 62,02 % | **+0,12 pp** |
| | **Autarkiegrad** | 18,63 % | 18,66 % | **+0,04 pp** |
| | Speicherfüllstand (Σ) [kWh] | 19 765,17 | 19 305,33 | **−2,33 %** |
| **1011** | Solarthermie-Ertrag [kWh] | 643,58 | 643,71 | +0,02 % |
| | Eigenverbrauchsquote | 100,00 % | 100,00 % | 0 |

**Einordnung.** Die Richtung stimmt, die Größenordnung ist **kleiner als die Konzeptschätzung
(„mehrere Prozentpunkte")**: −0,95 pp bis +0,12 pp statt mehrerer Punkte. Der Grund liegt in den
Daten, nicht im Code: Die Referenzprojekte fahren **synthetische Wochenprofile** aus der
Wochentag-/Uhrzeit-Tabelle des Anwenders, und die sind über den Tag vergleichsweise flach — eine
Verschiebung um 1 bis 2 Stunden bewegt dort wenig. Bei einem gemessenen Haushaltslastgang mit
ausgeprägter Abendspitze fällt die Wirkung deutlich größer aus. Die **Speichergrößen** zeigen
den Effekt am klarsten (−2,2 bis −2,3 % Füllstandssumme, −8,9 % PV-Ladung des Pufferspeichers):
Der Speicher reagiert auf die Stundenzuordnung unmittelbar.

Projekt **1011** ist wie in PA0 als B1-Nachweis untauglich (Bedarf ≫ Erzeugung, EVQ 100 %),
**1043** führt beide Gewerke ohne Modul — beide bestätigen nur, dass Paket A diese Randfälle
nicht bricht.

### 4.4 Familie 4 — temperaturabhängige Größen der Wärmeseite (B1)

Die Stundentemperatur speist den COP der Wärmepumpe, den Bivalenzpunkt, den Heizstab, die
Erdreichrechnung und die Kaskade. Alle Änderungen sind klein und einseitig erklärbar:

| Projekt | Auffälligste Größe | PA0 | PA1 | Δ |
|---|---|---:|---:|---:|
| 1008 | Bivalenzpunkt [°C] | 6,11 | 6,48 | +6,06 % |
| 1008 | WP-Modul 1 Betriebsstunden | 435,78 | 442,99 | +1,65 % |
| 1024 | Bivalenzpunkt [°C] | 20,73 | 21,20 | +2,27 % |
| 1024 | Puffer Entladung Brauchwasser [kWh] | 7 647,05 | 7 512,52 | −1,76 % |
| 1023 | Heizkessel Wärmeproduktion [MWh] | 66,49 | 66,55 | +0,09 % |
| 1026 / 1028 | Heizkessel Wärmeproduktion [MWh] | 13,66 | 13,69 | +0,22 % |
| 1043 | Heizkessel Wärmeproduktion [MWh] | 13,71 | 13,75 | +0,29 % |
| 1043 | WP Wärmeproduktion [MWh] | 54,14 | 54,09 | −0,09 % |
| 1029 | Erdreich Jahresentzug [kWh] | 48 312,09 | 48 311,57 | −0,001 % |

**Was sich NICHT geändert hat** — der Gegenbeweis, dass Paket A außerhalb von PV, Solarthermie
und der Stundentemperatur nichts bewegt:

| Projekt | geänderte Skalare | was |
|---|---:|---|
| **1017** | **1** | nur `Vektor.stundentemperatur.Summe` |
| **1018** | **1** | nur `Vektor.stundentemperatur.Summe` |
| **1030** | **1** | nur `Vektor.stundentemperatur.Summe` |
| **1039** | **2** | nur die beiden Temperaturreihen |

1018 und 1030 sind die BHKW-/Kesselprojekte: Ihr Wärmebedarf hängt am Tagesmittel aus
`Tab_Klimadaten`, nicht an der Stundenreihe — **BHKW-Stromproduktion, Kesselwärme,
Vollbenutzungsstunden und die ganze KWKG-Kette bleiben auf die letzte Stelle gleich.** Kein
einziger Ergebnisschlüssel ist in PA1 neu oder entfallen (`aggregate.csv` führt in beiden
Ständen dieselben Schlüsselmengen).

---

## 5. Nebenbefunde (protokolliert, NICHT behoben)

1. **`SQL_ANLAGE_INSERT` verliert die KWKG- und die B3-Spalten.** Die Anweisung führt weder die
   acht KWKG-Spalten (Migrationsschritt 22) noch `Energiesteuer_Wahl`, `Aufteilung_Methode` und
   `Hilfsenergie_Anteil` (Schritt 61). Weil der Speicherweg aller Erzeuger **Löschen +
   Neuanlegen** ist, gehen sie bei **jedem** Speichern über Wizard, Karten oder Kontextmenüs
   still verloren — dieselbe Fehlerklasse, die Paket 1 für die Quellen-/Senken-Konfiguration
   geschlossen hat. Das zu beheben verlangt eine eigene Abnahme (dieselbe Anweisung wird an
   fünf Stellen benutzt) und gehört ins Wirtschaftlichkeitsmodul, nicht in Paket A. Ein Hinweis
   steht jetzt im Kopfkommentar der Anweisung.
   > **Nachtrag, noch am selben Abend:** Eine **parallele Sitzung** greift diesen Befund
   > bereits auf und baut auf dem Paket-A-Stand einen Block **FS1** („Rettung der
   > Fachspalten", `WizardCtrl.FachspaltenSichern`/`FachspaltenWiederherstellen`,
   > `FS1_Fachspalten_Protokoll.md`). Er war zum Zeitpunkt dieses Commits noch uncommittet
   > im Arbeitsbaum und ist **nicht** Teil von Paket A.
2. **`.wpx`-Pakete mit Schemastand 61 werden abgewiesen.** `ProjektExportImportCtrl` vergleicht
   den Paketstand gegen `ZIEL_VERSION`; mit dem Sprung auf 62 passen ältere Pakete nicht mehr.
   Das ist die eingebaute Zusage des Formats und gilt für jeden Migrationsschritt gleichermaßen
   — **systemimmanent, keine Regression.**
3. **Katalog vergiftet.** In allen sechs Modulen der Referenzmenge tragen `T_NOCT`, `alpha_SC`
   und `beta_OC` den Wert von `I_Kurzschluss`. Ursache ist der `Form_AdminPV`-Bestandsfehler
   (Abschnitt 2.4) in Verbindung mit dem PAN-Import, der die drei Felder mit 0 schreibt. Der
   Schreibweg ist repariert, **die vorhandenen Daten sind es nicht** — sie lassen sich nur durch
   Neuimport oder Handpflege heilen. Die Simulation meldet das je Modul im Protokoll.
4. **`gamma_PMP = 0`** beim Jinkosolar-Modul: 1011, 1026, 1028 und 1029 rechnen **ohne jeden
   Temperaturgang**. Reale Module verlieren 0,3 bis 0,45 %/K. E1.5 meldet das als Hinweis, ändert
   aber nichts am Rechenweg.
5. **Schrittnummer 62 ist jetzt belegt.** Andere Konzeptpapiere hatten sie auf dem Papier
   reserviert; wer als Nächstes einen Migrationsschritt braucht, nimmt **63**.
6. **`SolarCalculator.CalculateTimeOffset`** (`SolarPVGISCalculator.cs:397-427`) bleibt toter
   Code — nicht verwendet, nicht gelöscht (Auftragsvorgabe).
7. **Befund B3 (Laufzeit) unangetastet.** `SimulationPV` liest die Wetterreihe weiterhin je
   Modulfeld neu aus der Datenbank (jetzt über `ReadOrtszeit`). Das ist eine Laufzeitfrage, keine
   Ergebnisfrage; die Gesamtdauer des Referenzlaufs blieb bei 21 s (PA0: 19 s).

---

## 6. Offene Punkte

1. **Sichtabnahme der beiden Masken** durch Philipp: `Form_PV` (dritte Spalte, verbreitertes
   Panel, gestrichelter Rahmen) und `Form_AdminPV` (NOCT-Feld links unten). Die Maße sind
   gerechnet, nicht gesehen — die Anwendung lief während der Umsetzung nicht.
2. **Katalogpflege `T_NOCT`** für die sechs Referenzmodule (Abschnitt 5.3). Erst danach wird
   E1.2 rechnerisch wirksam; erwartet werden dann ±0,5 % Jahresertrag.
3. **Paket B (Stufe E2)** setzt auf diesem Stand auf. `2026-09-02_PA1_nach-PaketA` ist die
   Bitgleichheits-Basis für das Modell **EINFACH** (Konzept N2.5, Kriterium 1).
4. **Cutover je Rechner:** Der Schemastand der produktiven Datenbank steht weiter auf 61. Beim
   ersten Start dieses Codes läuft Schritt 62 nach; ab dann sind ältere `.wpx`-Pakete
   inkompatibel (Abschnitt 5.2).

---

## 7. Encoding-Nachweis

Alle berührten `.cs`-Dateien sind **UTF-8 mit BOM und CRLF** (Vorgabe `.editorconfig`,
`charset = utf-8-bom`) — vor dem ersten Edit je Datei gemessen und danach nachgewiesen
(`perl -e ... "<:raw"`, BOM-Test auf `EF BB BF`, CRLF-Zähler = LF-Zähler):

| Datei | BOM | CRLF = LF |
|---|---|---|
| `Allgemein/Simulation/SolarZeitbasis.cs` (neu) | ja | 182 |
| `Allgemein/Simulation/SimulationPV.cs` | ja | 471 |
| `Allgemein/Simulation/SimulationSolarthermie.cs` | ja | 673 |
| `Allgemein/Simulation/SimulationWaermebedarf.cs` | ja | 917 |
| `Controller/SolardatenCtrl.cs` | ja | 359 |
| `Model/KlimadatenModel.cs` | ja | 89 |
| `Allgemein/DbWerte.cs` | ja | 2582 |
| `Views/Simulation/Form_Simulation_Config.Uebersicht.cs` | ja | 1200 |
| `Views/Klimadaten/Form_Klimadaten.cs` | ja | 425 |
| `Views/Photovoltaik/Form_PV.cs` | ja | 443 |
| `Views/Photovoltaik/Form_AdminPV.cs` | ja | 401 |
| `MyResource/Resource.resx` | ja | 8633 |
| `MyResource/Resource.en-US.resx` | ja | 8626 |
| `MyResource/Resource.Designer.cs` | ja | 24931 |

Die Protokolldateien (`*.md`) folgen der Hauskonvention **ohne BOM, CRLF**.

---

## 8. Umgang mit der fremden Sitzung

Im Arbeitsbaum arbeitete parallel eine **fremde Sitzung** mit uncommitteten Änderungen an
`Controller/MenueCtrl.cs`, `MyResource/Resource.resx`, `MyResource/Resource.en-US.resx`,
`Views/Klimadaten/Form_Klimadaten.Designer.cs`, `Views/Projekt/*`, `Views/Wizard/*` und
`Allgemein/Reporting/KD6_Protokoll.md`. Gestaget wurden ausschließlich eigene Pfade; **kein
`git add -A`**.

Die beiden `.resx`-Dateien tragen beide Seiten. Für sie wurde die **Indexfassung gezielt gebaut**
(HEAD-Blob + eigener Block vor `</root>`, dann `git hash-object -w` + `git update-index`) — der
Commit `7c622b1` enthält je Datei **genau die eigenen 24 Zeilen** und **null** der 20 fremden
Schlüssel (`PDLG_*`, `WZP_*`, `PA_AUSGEWAEHLT`, `PA_VARIANTE_VON`); nachgezählt am
gestageten Blob. Die fremden Änderungen stehen unverändert im Arbeitsbaum.

**Nach `7c622b1`** hat eine parallele Sitzung zusätzlich `Controller/WizardCtrl.cs` (Block FS1,
Rettung der Fachspalten — siehe Nebenbefund 1), `Views/Photovoltaik/Form_AdminPV.cs`
(Plausibilitätsprüfung vor dem Schreiben; sie setzt auf `_alphaScGeladen`/`_betaOcGeladen` und
dem neuen `T_NOCT`-Feld auf), `Allgemein/Import/CEC/CECDataService.cs`,
`Allgemein/Import/Pan/PanModule.cs`, `Views/Photovoltaik/Form_CECImport.cs` sowie neue Dateien
(`Allgemein/Import/PvModulPlausibilitaet.cs`, `FS1_Fachspalten_Protokoll.md`, `sql/pv_katalog/`)
im Arbeitsbaum verändert. **Diese Änderungen sind in keinem der vier Paket-A-Commits enthalten**
— der Abschlusscommit stagt ausschließlich das Konzeptdokument, `Referenzlaeufe/LIESMICH.md`,
den Basisordner und dieses Protokoll. Der Referenzlauf ist davon unberührt: Er wurde aus dem
`git archive`-Export von `7c622b1` gerechnet, also ohne die fremden Arbeitsbaum-Änderungen.
