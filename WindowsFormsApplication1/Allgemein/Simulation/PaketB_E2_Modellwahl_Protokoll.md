# Paket B — Stufe E2 als WÄHLBARES Rechenmodell

**Umsetzungsprotokoll, 03.09.2026, Branch `ios_migration`**

Grundlage: `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` (Repo-Wurzel) — Stufe **E2**
(Abschnitt 3) und die Beauftragung in **Nachtrag 2** (N2.1–N2.6). Vorgänger:
[`PaketA_Zeitbasis_E1_Protokoll.md`](PaketA_Zeitbasis_E1_Protokoll.md).

| | |
|---|---|
| **Commits** | `f1d16e3` PB1a (Migration 63 + Datenmodell) · `4bd8752` PB1b (Rechenmodell ERWEITERT) · `74f9acf` PB1c (Degradation + Dialogfeld) · `36acbf1` PB1d (Oberfläche, Importe, Ressourcen) · dieser Commit PB1 (Referenzlauf + Protokoll) |
| **Bitgleichheits-Basis** | `Referenzlaeufe/2026-09-02_PA1_nach-PaketA/` (Codestand `7c622b1`, Schemastand 62) |
| **Neue Basis** | `Referenzlaeufe/2026-09-03_PB1_nach-PaketB/` (Codestand `36acbf1`, Schemastand 63) |
| **Schemastand** | 62 → **63** (Migrationsschritt 63, zweiter Schritt des SQLite-Zweigs) |
| **Prüfbuild** | MSBuild x64 Debug aus `git archive`-Export, **0 Fehler**; Warnungsprofil zum PA1-Export **identisch** (CS0108 2, CS0109 2, NU1510 4, WFO0003 1, WFO1000 30 — beide Exporte dafür mit demselben Befehl neu gebaut) |
| **Zentrales Abnahmekriterium** | **355/355 CSV byte-/MD5-gleich zu PA1** — erfüllt |

---

## 1. Die Leitentscheidung und was aus ihr folgt

Philipps Entscheidung vom 02.09.2026 lautet: **E2 wird umgesetzt, aber das vereinfachte
Modell bleibt vollwertig wählbar.** E2 ist damit kein Ersatz, sondern eine zweite
Rechentiefe — und daraus folgt die ganze Bauform dieses Pakets:

* Der Schalter sitzt **je Anlage** (`Tab_Energieanlagen.PV_Modell`), nicht je Projekt.
  Die Wechselrichterdaten gelten je Anlage, und ein Projekt darf gemischt sein: ein Feld
  mit bekanntem Wechselrichter, eines ohne.
* **NULL heißt EINFACH.** Nicht „unbestimmt", nicht „bitte nachpflegen" — sondern der
  Rechenweg aus Paket A, Zeichen für Zeichen. Jede Bestandsanlage steht nach der
  Migration auf NULL.
* Der EINFACH-Zweig wurde deshalb **nicht umgebaut, sondern umschlossen**: Er steht in
  `SimulationPV.Berechnung` als eigene Stundenschleife, unverändert. Eine gemeinsame
  Schleife mit Verzweigungen im Rumpf hätte dieselbe Zusage nur schwerer nachweisbar
  gemacht — und der Nachweis ist hier die Hauptsache.
* Die Abnahme ist deshalb ein **Nicht-Ereignis**: 355 byte-gleiche CSV. Die Gegenprobe,
  dass das neue Modell überhaupt rechnet, liefern zwei Smoke-Läufe (Abschnitt 5).

---

## 2. Änderungen

### 2.1 Datenmodell und Migrationsschritt 63 (PB1a, `f1d16e3`)

**Acht Spalten, kein DML, kein DDL-DEFAULT.**

| Tabelle | Spalte | Typ | NULL bedeutet |
|---|---|---|---|
| `Tab_Energieanlagen` | `PV_Modell` | TEXT(20) | **EINFACH** — der Paket-A-Rechenweg |
| | `PV_WrNennleistungKw` | DOUBLE | kein Clipping; Auslastungsbezug ist die DC-Nennleistung |
| | `PV_WrEta10` / `-50` / `-100` | DOUBLE | Vorbelegung 0,94 / 0,975 / 0,97 |
| `Tab_PV`, `Tab_PV_STAMM` | `Technologie` | TEXT(30) | unbekannt → Rückfall auf die EINFACH-Modulformel |
| `Tab_ProjektPhotovoltaik` | `Degradation` | DOUBLE | 0 %/a — ergebnisneutral |

`SchemaMigration.SCHRITT_63_PV_MODELLWAHL`, `Schritt_63_PvModellwahl`, Eintrag in
`SCHRITTE_SQLITE`, `ZIEL_VERSION` 62 → **63**. Der Schrittkörper benutzt ausschließlich
`SqliteSpalteAnlegen`; anders als Schritt 62, der `"REAL"` ausgeschrieben hat, geht er
über **`StilleDb.SqliteSpaltenTyp`** — er führt neben DOUBLE auch zwei `TEXT(n)`-Spalten,
und die Übersetzung nach `TEXT CHECK (length(…) <= n)` ist genau dieselbe, die die
Rückfallebene benutzt. Zwei Schreibweisen derselben Spalte wären zwei Spaltendefinitionen.

**Der Katalog ist geteilt, und das Kriterium ist der LESER:**

* `SchemaKatalog.Schritt63_PvModellwahl` (fünf Anlagenspalten + `Tab_PV.Technologie`)
  steht **in `Alle`** — der Rechenkern liest sie. Die Einträge beginnen mit
  `Tab_Energieanlagen` und schließen damit unmittelbar an Schritt 62 an; die
  Rückfallebene liest das Tabellenschema neu, sobald der Tabellenname wechselt.
* `SchemaKatalog.Schritt63_PvStammUndDegradation` (`Tab_PV_STAMM.Technologie`,
  `Tab_ProjektPhotovoltaik.Degradation`) steht **nicht in `Alle`** — wortgleiche
  Begründung wie bei Schritt 61: Der Rechenkern liest sie nirgends, und die
  Rückfallebene läuft bei jedem Simulationsstart.

**Persistenz an allen Paket-A-Stellen**, namensbasiert und mit ausdrücklichem Typ
(`ProjektPuffer.Par`), weil NULL hier der Normalfall ist: `WErzeugerModel`,
`WErzeugerCtrl.AusZeile`, `WizardCtrl.SQL_ANLAGE_INSERT` + `AnlagenParameter`,
`PhotovoltaikModel`/`-Ctrl`/`-StammCtrl` (einschließlich `CopyFromStamm` und
`UpdateImport`), `ProjektPhotovoltaikCtrl`, `KatalogRegistry`, `AbweichungsErmittler`.

> **Die neuen Anlagenspalten gehören ins Modell, nicht in die FS1-Rettung.** Der Block
> FS1 einer parallelen Sitzung definiert die Fachspalten als **Komplement** von
> `SQL_ANLAGE_INSERT` — es gibt keine zweite Liste. Mit der Aufnahme in die
> Einfügeanweisung verlassen die fünf Spalten die Rettungsmenge von selbst. Genau so war
> es gemeint: Der Rechenkern liest sie, die PV-Anlagenmaske schreibt sie — sie sind
> Modellspalten.

`ProjektPhotovoltaikCtrl.LiesOderVorbelegt` belegt `Degradation` mit **0,5 %/a** vor —
aber **nur beim Anlegen einer neuen Zeile** (Muster N5/F5). Eine bestehende Zeile behält
ihr NULL, sonst änderte allein die Migration die Erlösreihe jedes Bestandsprojekts.

### 2.2 Rechenkern ERWEITERT (PB1b, `4bd8752`)

**Die Modellweiche.** `SimulationPV.IstErweitert(anlage)` schaltet nur beim
ausdrücklichen Persistenzwert `PV_MODELL_ERWEITERT` um. NULL, Leerstring,
`PV_MODELL_EINFACH` und **jeder unbekannte Text** bedeuten EINFACH — eine Textmüll-Zeile
in der Datenbank darf kein anderes Rechenmodell aktivieren.

**Neue Datei `Allgemein/Simulation/PvErweitertesModell.cs`** (249 Zeilen, ohne Datenbank
prüfbar — Muster `SolarZeitbasis` aus Paket A): Huld-Koeffizienten, `EtaRelativ`,
`LeistungHuld`, `EtaWechselrichter` und die Zuordnung der Importtexte.

**Transposition (E2.5).** `SolarCalculator.CalculateHourlyHayDavies` — neue Methode neben
der Bestandsfunktion:

```
I_0n = 1367 · (1 + 0,033 · cos(360° · n / 365))
A_i  = DNI / I_0n                                  geklemmt 0…1
R_b  = cosθ / max(cosθ_z, cos 85°)                 Horizontklemme
G_t  = DNI·cosθ + DHI·[A_i·R_b + (1 − A_i)·(1 + cosβ)/2] + GHI·0,2·(1 − cosβ)/2
```

Die **Sonnengeometrie** ist in eine gemeinsame private Hilfsmethode `Sonnengeometrie`
gezogen — Rechenschritt für Rechenschritt, Klammerung für Klammerung die des Bestands.
Die drei statischen Seitenwirkungen (`sonnenwinkel`, `sonnen_azimut`, `lastCosTheta`)
bleiben **draußen**: Sie gehören zum Vertrag von `CalculateHourly`, den
`SimulationSolarthermie` unmittelbar nach dem Aufruf ausliest, und dürfen von einem
zweiten Modell nicht mitgeschrieben werden.

**Modul (E2.3).** `T_Zelle` nach demselben NOCT-Modell wie EINFACH, dann
`P_DC = P_STC · G' · η_rel` mit den PVGIS-Koeffizienten für C_SI, CIS und CDTE.
Bei `G' < 0,001` ist das Ergebnis 0 (ohne die Klemme liefe `0 · ln 0` in NaN); ein
rechnerisch negatives η_rel wird auf 0 geklemmt. **In ERWEITERT ersetzt Huld den linearen
γ-Gang vollständig** — die Temperaturabhängigkeit steckt in k3…k6.

**Wechselrichter (E2.1/E2.2).**

```
P_DC,sys = P_DC · (1 − PV_Systemverluste/100)
x        = P_DC,sys / P_AC,nenn            ohne Nennleistung: / P_STC,gesamt
η_WR(x)  = linear über (0,1; η10), (0,5; η50), (1,0; η100)
           unter 0,1: linear auf 0   ·   über 1,0: konstant η100
P_AC     = min(P_DC,sys · η_WR, P_AC,nenn)
```

**Kennzahlen und Rückfallebenen.** `PVModulErgebnis` trägt `Erweitert`,
`DcAcVerhaeltnis`, `ClippingVerlust` und `WechselrichterVerlust` — als **Ausweis**; die
Ergebnistabellen bleiben unverändert (Q-Reserve des Konzepts). Je Anlage meldet das
Protokoll die Kennzahlen, und **jede Rückfallebene meldet sich einzeln**:

| Fall | Meldung |
|---|---|
| `Technologie` leer | „…führt keine Zelltechnologie. Ohne sie gibt es keine Schwachlicht-Koeffizienten; gerechnet wird die Modulformel des einfachen Modells auf der Hay-Davies-Einstrahlung." |
| `A_SI`/`SONSTIGE` | „…gibt es keinen Huld-Koeffizientensatz (nur C_SI, CIS und CDTE sind veröffentlicht)." |
| `Leistung` = 0 | „…braucht die Nennleistung des Moduls; der Katalog führt keine." |
| Kennlinie unvollständig | „…Gerechnet wird mit 0,940 / 0,975 / 0,970…" |
| keine AC-Nennleistung | „…Gerechnet wird OHNE Clipping; die Auslastung bezieht sich ersatzweise auf die DC-Nennleistung (x kWp)." |

### 2.3 Degradation (PB1c, `74f9acf`)

`PvErloesRechner.DegradationsFaktor(d, t) = (1 − d/100)^(t−1)`. **Jahr 1 ist immer 1** —
die Stundensimulation rechnet das Basisjahr und kennt kein Alter; erst die Erlösreihe
altert. **d = 0 liefert exakt 1,0 über einen eigenen Zweig** statt über `Math.Pow(1,0; n)`;
das ist keine Kosmetik, sondern die Bedingung dafür, dass die Reihe bitgleich zum
P6-Stand bleibt. Ein negativer Wert wird wie 0 behandelt.

Der Faktor trifft in jedem Jahr t die vergütungsfähige Arbeit (und damit Einspeiseerlös,
Marktprämie, Spoterlös und § 51-Ausfall gleichermaßen — sie hängen alle an `basisKwh`),
die § 51a-Gutschrift mit dem Faktor ihres Gutschriftjahres, und den **vermiedenen Bezug**.

> **Zum vermiedenen Bezug — die einzige Stelle, an der das Konzept eine Auslegung
> brauchte.** Die Stundenreihe und damit der Reststrom des Kapitalwerts sind über alle
> Jahre konstant. Altert die Anlage, deckt sie weniger Eigenverbrauch, der Netzbezug
> steigt um `EV_Basisjahr · (1 − Faktor(t))`. Dieser Mehrbezug steht als **negativer
> Beitrag in der Reihe `PV_VERGUETUNG`**; die Kostenseite bleibt unberührt. Kein
> Doppelansatz: Die Kostenseite rechnet die Ersparnis des Basisjahres, dieser Posten nur
> ihren jährlichen Schwund. Die beiden Größen (Eigenverbrauch, Arbeitspreis) reicht
> `WirtschaftlichkeitCtrl` aus denselben Quellen herein, aus denen der Ausweis
> `PvVermiedenerBezugAusweis` rechnet — eine Wahrheit, zwei Verwendungen. Ohne
> Degradation ist der Posten exakt 0.

`Form_PhotovoltaikVerguetung` bekommt das Feld „Degradation [%/a]" in der Gruppe
„Anlage", rechts unten — programmatisch, weil Designer- und `.resx`-Dateien der Formulare
nicht von Hand editiert werden. 0 heißt NULL; die Vorschau rechnet den Effekt mit.

### 2.4 Oberfläche, Importe, Ressourcen (PB1d, `36acbf1`)

**`Form_PV` — das Panel ist umgebaut, nicht erweitert.** Paket A hatte eine dritte Spalte
bei x = 252 angehängt; ihre Beschriftung kollidierte dort mit dem AutoSize-Label
„Anzahl Module:" (x 177…282). Für Modellwahl **und** Wechselrichterknopf ist in zwei
Zeilen zu 418 px ohnehin kein Platz — horizontal blockiert die Modulliste ab x = 449,
vertikal der Modulblock. Das Panel geht deshalb auf **zwei Spalten und vier Zeilen**
(420 × 71 → 420 × **128**), alles darunter rückt **57 px** nach unten, die Maske wächst
entsprechend:

| | Spalte A | Spalte B |
|---|---|---|
| Zeile 1 | Neigung [°] | **Rechenmodell** (Auswahlliste) |
| Zeile 2 | Azimut [°] | WR-Wirkungsgrad *(nur EINFACH aktiv)* |
| Zeile 3 | Anzahl Module | Systemverluste [%] |
| Zeile 4 | — | **Knopf „Wechselrichter…"** *(nur ERWEITERT aktiv)* |

Die Designer-Datei bleibt unberührt; Lage und Größe der sechs Bestandscontrols setzt der
Code, `AutoSize` der Beschriftungen wird abgeschaltet (genau daran lag die Überlappung).
Im Assistenten passt sich der Rahmen selbst an (`WizardParent.LoadNewForm` rechnet mit
`PreferredSize` und schaltet AutoScroll ein); der gestrichelte Rahmen in `Form_PV_Paint`
liest Lage und Größe zur Zeichenzeit und folgt automatisch.

**`Form_PVModell` (neu, programmatisch, ohne Designer- und `.resx`-Datei):**
AC-Nennleistung und die drei Kennlinienpunkte mit **Live-Kennzahl DC/AC**. 0 heißt in
jedem Feld NULL. Im Modell EINFACH sind alle vier Felder **gesperrt** (Enabled, nicht
Visible — Vorgabe N2.4) und der Kopf sagt warum. HilfeKontext-Eintrag `Form_PVModell` →
Photovoltaik.

**`Form_AdminPV`:** Auswahlliste „Zelltechnologie" unter dem NOCT-Feld. Erster Eintrag
„(nicht gepflegt)" schreibt NULL — bewusst kein sechster Fachwert: „unbekannt" und
„SONSTIGE" führen zwar zur selben Rückfallebene, sagen dem Anwender aber Verschiedenes,
und der Simulationshinweis nennt beide Fälle getrennt. Anzeigetexte aus MyResource,
Persistenzwerte aus `DbWerte`, verbunden nur über den Index (Drei-Schichten-Regel).

**Importe:** CEC `Technology` und PAN `Technol` werden über
`PvErweitertesModell.TechnologieAusCec`/`-AusPan` auf die fünf Persistenzwerte abgebildet
und mitgeschrieben; `KatalogRegistry` führt die Spalte in der Import-Schnittmenge, damit
die Dublettenprüfung sie vergleicht. Verglichen wird auf **Teilzeichenketten** in fester
Reihenfolge (`a-Si` **vor** `c-Si`, sonst finge die Silizium-Regel die Dünnschichtmodule
mit ein), weil die CEC-Texte keine geschlossene Werteliste sind.

> **Ergänzung gegenüber dem Konzepttext:** `HIT`, `PERC` und `TOPCon` fallen ebenfalls auf
> `C_SI`. Es sind kristalline Siliziumzellen; sie unter „SONSTIGE" zu führen hätte ihnen
> ohne Not den Koeffizientensatz genommen.

**PV-Karte** (`Form_Simulation_Config.Karten.PvDetailchips`): Chip
„Modell erweitert · DC/AC 1,25" — **nur bei Abweichung vom Bestand**; das vereinfachte
Modell ist der Regelfall und braucht keinen Chip.

**Ressourcen:** 31 Schlüssel in **beiden** Sprachen (29 × `PVM_*`, 2 × `SIM_KARTE_PV_MODELL_*`),
dazu die Properties in `Resource.Designer.cs`, alphabetisch zwischen `PV_MODUL_TIP_TNOCT`
und `PVW_51_ALTANLAGE` eingeordnet. **Keine CS0102-Duplikate** — nachgezählt: 2 769
Designer-Schlüssel, 2 793 resx-Einträge je Sprache, Duplikate 0.

---

## 3. Verifikation

### 3.1 Prüfbuild

`git archive HEAD` (`36acbf1`) nach `P:\pb1\src`, MSBuild x64 Debug — **0 Fehler**. Für
den Warnungsvergleich wurde der PA1-Export mit demselben Befehl neu gebaut:

| Code | PA1-Export | PB1-Export |
|---|---:|---:|
| CS0108 | 2 | 2 |
| CS0109 | 2 | 2 |
| NU1510 | 4 | 4 |
| WFO0003 | 1 | 1 |
| WFO1000 | 30 | 30 |

*(Die NU1510-Zahl ist beim Solution-Build 4, beim Einzelprojekt-Build 2 — ein
Zählunterschied der Bauart, kein Unterschied der Stände.)*

### 3.2 Prüfstand `P:\pb1\harness` — die reinen Rechenvorschriften, **58 PASS / 0 FAIL**

| Prüfung | Ergebnis |
|---|---|
| **η_rel(G' = 1, T' = 0) == 1 EXAKT** | für alle drei Koeffizientensätze |
| Koeffizienten k1…k6 | zeichengleich mit `pvlib._infer_k_huld` (PVGIS 5), am 03.09.2026 gegen die Quelle geprüft |
| `P_DC(1000 W/m², 25 °C) == P_STC` | exakt |
| Klemme G' < 0,001 | 0; 1…1400 W/m² durchgehend endlich und ≥ 0 |
| A_SI / SONSTIGE / NULL | kein Koeffizientensatz (Rückfall greift) |
| **Hay-Davies == isotrop bei DNI = 0** | **45 792 Fälle** (6 Neigungen × 6 Azimute × 53 Tage × 24 h), davon 22 716 mit Sonne — **maximale Abweichung 0**, nicht 1e-9 |
| Nachtstunde | beidseitig 0 |
| Sommermittag Süd mit DNI | 891,46 → **909,28 W/m²** (circumsolarer Zuschlag) |
| 8 760 h ohne Horizontexplosion | Maximum 1 433,3 W/m², kein NaN/Inf |
| **Kennlinie an den Stützstellen** | 10 % / 50 % / 100 % **exakt** die Eingabewerte |
| Ränder | x = 0 → 0; x = 0,05 → η10/2; x > 1 → konstant η100; 0,3 → Mitte 10/50 |
| **Clipping-Verlust == Σ max(0, P_DC,sys·η − P_AC,nenn)** | 100 000 Zufallsstunden, Abweichung < 1e-9 |
| Energiebilanz `P_DC,sys = P_AC + Clipping + WR-Verlust` | 698 868,458 gegen 698 868,458 |
| Degradation d = 0 | Faktor in Jahr 1, 20 und 40 **exakt 1,0** |
| Technologie-Zuordnung CEC/PAN | 15 Fälle einschließlich `a-Si/nc` vor `c-Si` |
| Modellweiche | NULL/leer/Müll = EINFACH; nur `PV_MODELL_ERWEITERT` schaltet um |

> **Ein Konzeptwert stimmt nicht: N2.5 nennt für d = 0,5 %/a in Jahr 20 den Faktor
> 0,9088.** Nachgerechnet ist `(1 − 0,005)^19 = 0,909156`, also **0,9092**. Die Formel des
> Konzepts ist richtig, die dort genannte Zahl leicht daneben; der Prüfstand prüft gegen
> die Formel und meldet den gerechneten Wert. **Die Zahl 0,9088 im Konzept sollte auf
> 0,9092 berichtigt werden.**

### 3.3 Migrationsschritt 63 an einer DB-Kopie — **24 PASS / 0 FAIL**

Lauf 1: Stand 61 → **63** (Schritt 62 und 63 hintereinander), alle acht Spalten angelegt,
**0 belegte Zeilen** in allen vier Tabellen — kein DML. Lauf 2: „bereits erledigt", kein
erneutes DDL, Stand bleibt 63, jede Spalte genau einmal. Zusätzlich geprüft: Die
Rückfallebene `SchemaKatalog.Alle` führt **genau die sechs Rechenkern-Spalten**, und
weder `Tab_PV_STAMM` noch `Degradation` stehen darin.

### 3.4 Referenzlauf PB1 — der Bitgleichheitsnachweis

```powershell
& $exe lauf --ziel <Referenzlaeufe\2026-09-03_PB1_nach-PaketB> `
            --quelle P:\pa0\Quelle\Kenndaten.sqlite `
            --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1026,1028,1029,1030,1039,1043
```

**14 von 14 erfolgreich**, 355 CSV, 24 Warnungen (alle Bestand, alle aus 1043 — identische
Menge wie in PA0/PA1), 0 Fehler, Gesamtdauer 10 s. Migration der Arbeitskopie **61 → 63**.

```
vergleich 2026-09-02_PA1_nach-PaketA 2026-09-03_PB1_nach-PaketB
  → 14/14 PASS (3 882 476 Werte)  ·  GESAMT: PASS
Byte-/MD5-Vergleich: 355 von 355 CSV identisch, 0 abweichend,
                     keine Datei nur auf einer Seite
pruefen 2026-09-03_PB1_nach-PaketB  → GESAMT: plausibel
```

Die drei `pruefen`-Hinweise („Jahressumme 0 — Gewerk aktiviert, aber kein Modul") bei
1007, 1039 und 1043 sind unverändert der Bestand aus PA0/PA1.

**Produktive Datenbank unberührt:** `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`,
Zeitstempel **02.09.2026 22:07:36**, 67 706 880 Byte — derselbe Stand wie vor Paket A.

> **Was der eine Nachweis alles trägt.** Byte-Gleichheit über 355 Dateien und
> 3,88 Mio. Werte deckt sechs Umbauten auf einmal ab: Migrationsschritt 63, die
> Modellweiche in `SimulationPV`, die **Auslagerung der Sonnengeometrie** in
> `SolarCalculator` (der heikelste Eingriff des Pakets — er hätte im letzten Bit
> abweichen können), den Degradationsfaktor, fünf zusätzlich gelesene und geschriebene
> Anlagenspalten und die neue Katalogspalte `Technologie`.

### 3.5 Wirtschaftlichkeit: INEKON-Referenz unverändert

Prüfstand `kd1runner` (Vorlage von `K:` nach `P:\pb1\kd1runner` kopiert, `APP_BIN` auf den
Paket-B-Build gesetzt), Modus `pv6`:

```
PV6-SMOKE: 28 PASS, 0 FAIL
  I1/I2 KapitalwertRechner == Handrechnung (Überschuss / Volleinspeisung)
  I3 Überschuss ±1 %        (91867 gegen 92568: −0,76 %)
  I4 Volleinspeisung ±1 %   (−23087 gegen −22979: −0,47 %)
  K1a EV-Quote 83,37 %  ·  K2 LCOE0 = 14,657 ct/kWh  ·  K4 Vorteil/a = 1.600
```

Zahl für Zahl dieselben Werte wie im Protokoll der Etappe Ä24 — die Degradation steht auf
NULL, und NULL liefert den Faktor exakt 1,0.

---

## 4. ERWEITERT-Smoke — die Gegenprobe

Bitgleichheit allein bewiese nur, dass nichts passiert ist. Zwei Läufe auf **Kopien**
derselben Quelle zeigen die andere Hälfte. Projekt **1026**, PV-Anlage „Jinkosolar
JKM 260P-60", 20 Module × 260 W = **5,20 kWp**, Neigung 30°, Azimut 0.

* **Smoke A:** `PV_Modell = ERWEITERT`, `Technologie = C_SI`,
  `PV_WrNennleistungKw = 4,16` (≈ 0,8 · kWp).
* **Smoke B:** `PV_Modell = ERWEITERT`, **keine** Wechselrichterdaten,
  `Technologie = NULL`.

| Größe | EINFACH (= PA1/PB1) | Smoke A | Smoke B |
|---|---:|---:|---:|
| PV-Erzeugung [MWh/a] | 6,71 | **6,45 (−3,94 %)** | **6,94 (+3,37 %)** |
| davon genutzt [MWh/a] | 4,34 | 4,27 | 4,37 |
| **Eigenverbrauchsquote** | 64,68 % | **66,20 % (+1,52 pp)** | **62,97 % (−1,71 pp)** |
| Strombedarfsdeckung | 15,84 % | 15,60 % | 15,96 % |
| Netzrestbezug [MWh/a] | 21,95 | 22,01 | 21,88 |
| max. Einstrahlung [W/m²] | 1 058,93 | 1 080,46 | 1 080,46 |
| **DC/AC** | — | **1,25** | nicht bestimmbar |
| Volllaststunden | — | 1 240 | 1 335 |
| Wechselrichterverlust [kWh/a] | — | 292,3 | 350,1 |
| **Clipping-Verlust [kWh/a]** | — | **40,1** | 0,0 |

**Die Richtung stimmt in jeder Zeile.** Smoke A liegt mit **−3,94 %** im angesagten Band
−1 … −5 %: Das Schwachlichtmodell nimmt mehr weg, als Hay-Davies dazugibt, und das
Clipping bei DC/AC 1,25 kappt 40 kWh (0,62 % des Ertrags). Weil Clipping ausgerechnet die
Einspeisespitzen trifft, **steigt** die Eigenverbrauchsquote um 1,5 Punkte — genau die
Wirkung, die das Konzept für E2.1 angesagt hat („Clipping senkt nur Einspeisespitzen →
EVQ steigt leicht").

Smoke B zeigt die Gegenrichtung: Ohne Technologie fällt das Schwachlichtmodell weg, übrig
bleibt der reine Hay-Davies-Gewinn (**+3,37 %**), und die Eigenverbrauchsquote sinkt
entsprechend. Die maximale Einstrahlung steigt in **beiden** Läufen von 1 058,9 auf
1 080,5 W/m² — der circumsolare Anteil, den das isotrope Modell nicht kennt.

**Alle Rückfallebenen melden sich** (Konzept N2.5, Kriterium 2). Smoke B protokolliert
alle drei:

```
PV-Anlage "…" rechnet im erweiterten Modell, das Modul "…" fuehrt aber keine
  Zelltechnologie. Ohne sie gibt es keine Schwachlicht-Koeffizienten; gerechnet wird
  die Modulformel des einfachen Modells (Nennleistung, gamma_PMP, NOCT) auf der
  Hay-Davies-Einstrahlung. Die Technologie laesst sich im Modulkatalog pflegen.
PV-Anlage "…": Die Wechselrichter-Kennlinie ist nicht vollstaendig gepflegt. Gerechnet
  wird mit 0.940 / 0.975 / 0.970 bei 10 / 50 / 100 % Auslastung (Vorbelegung eines
  typischen String-Wechselrichters).
PV-Anlage "…": Es ist keine Wechselrichter-Nennleistung gepflegt. Gerechnet wird OHNE
  Clipping; die Auslastung der Kennlinie bezieht sich ersatzweise auf die
  DC-Nennleistung der Anlage (5.20 kWp).
PV-Anlage "…" (Modell erweitert): DC/AC ohne AC-Nennleistung nicht bestimmbar
  (5.20 kWp gegen keine AC-Nennleistung), Jahresertrag 6,939.7 kWh (1,335
  Volllaststunden), Wechselrichterverlust 350.1 kWh, Clipping-Verlust 0.0 kWh.
```

Beide Smoke-Ordner sind **bewusst nicht abgelegt**: Sie sind Wirkprobe auf präparierten
Kopien, keine Basis. Ihre Zahlen stehen hier und im Laufprotokoll der Basis.

> **Ein Nebenbefund zur Einordnung von Smoke A.** Das Jinkosolar-Modul führt
> `gamma_PMP = 0` (Paket-A-Befund). Im **einfachen** Modell rechnet die Anlage deshalb
> ohne jeden Temperaturgang; im **erweiterten** bringt Huld ihn über k3…k6 zurück. Ein
> Teil der −3,9 % ist also nicht Schwachlicht, sondern der erstmals wirksame
> Temperatureinfluss. Bei einem Modul mit gepflegtem γ fiele die Differenz kleiner aus.

---

## 5. Umgang mit den parallelen Sitzungen

Im Arbeitsbaum arbeiten **zwei fremde Sitzungen** mit uncommitteten Änderungen:
Block **FS1** (Fachspalten-Rettung, `Controller/WizardCtrl.cs`,
[`FS1_Fachspalten_Protokoll.md`](FS1_Fachspalten_Protokoll.md)) und die
**PV-Katalog-Absicherung** (`Allgemein/Import/PvModulPlausibilitaet.cs` samt Aufrufen in
`Form_AdminPV` und `Form_CECImport`, `CECDataService`, `PanModule`,
[`../Import/PvKatalog_Koeffizienten_Protokoll.md`](../Import/PvKatalog_Koeffizienten_Protokoll.md)),
dazu `KostenProjektPositionenCtrl`, `MenueCtrl`, `Views/Projekt/*`, `Views/Wizard/*`,
`Form_Klimadaten.Designer.cs` und beide `Resource*.resx`.

Gestaget wurden ausschließlich eigene Pfade; **kein `git add -A`**. Für **vier** gemischte
Dateien ist die Indexfassung gezielt gebaut worden (HEAD-Blob + ausschließlich die eigenen
Änderungen, dann `git hash-object -w` + `git update-index --cacheinfo`) — dasselbe
Verfahren, das Paket A für die `.resx` benutzt hat:

| Datei | im Commit | nicht im Commit |
|---|---|---|
| `MyResource/Resource.resx` | genau **29 `PVM_*` + 2 `SIM_KARTE_PV_MODELL_*`** | **null** der 20 fremden (`PDLG_*`, `PA_*`, `WZP_*`) |
| `MyResource/Resource.en-US.resx` | dito | dito |
| `Views/Photovoltaik/Form_AdminPV.cs` | Technologie-Auswahl und ihre vier Einhängepunkte | der Block `PvModulPlausibilitaet` |
| `Views/Photovoltaik/Form_CECImport.cs` | drei `Technologie`-Zeilen | Plausibilitätsprüfung, PAN-Umrechnung, Anzeigefix |

**Warum bei den beiden `.cs` gefiltert wurde und nicht pauschal gestaget:** Der fremde
Block ruft `PvModulPlausibilitaet` auf, dessen Datei noch **untracked** ist. Mitgenommen
wäre `HEAD` nicht übersetzbar gewesen. Nachgewiesen am gestageten Blob: 0 Treffer für
`PvModulPlausibilitaet`, und der `git archive`-Export von `36acbf1` baut mit 0 Fehlern.

**Eine Ausnahme, bewusst und benannt:** `Controller/WizardCtrl.cs` ist im Commit **PB1a
als Ganzes** enthalten und trägt damit den fremden FS1-Block mit. Die Datei ist auf
Hunk-Ebene verschränkt — der Kopfkommentar von `SQL_ANLAGE_INSERT` und der FS1-Block
enthalten beide eigene und fremde Zeilen —, und der eigene Text verweist inhaltlich auf
`Fachspalten()`. Betroffen sind die Hunks bei Zeile 59, 78, 1098–1402, 1574, 1663 und 1686.
Die fremden Änderungen stehen unverändert im Arbeitsbaum.

---

## 6. Nebenbefunde (protokolliert, NICHT behoben)

1. **Konzeptwert N2.5 „Jahr 20 Faktor 0,9088" ist leicht falsch** — gerechnet
   0,909156 (Abschnitt 3.2). Die Formel stimmt.
2. **`gamma_PMP = 0`** beim Jinkosolar-Modul wirkt sich im erweiterten Modell anders aus
   als im einfachen (Abschnitt 4, Nebenbefund): Huld bringt den Temperaturgang über
   k3…k6 zurück. Das ist fachlich richtig, macht den Vergleich der beiden Modelle an
   diesem Modul aber unschärfer.
3. **Die sechs Katalogmodule führen keine `Technologie`** — bis zur Katalogpflege fällt
   das erweiterte Modell überall auf die Modulformel zurück. Die Spalte lässt sich über
   `Form_AdminPV` pflegen oder durch einen Neuimport füllen.
4. **`Tab_ErgebnisPhotovoltaik` bleibt unverändert.** DC/AC, Clipping- und
   Wechselrichterverlust stehen im Protokoll und auf der PV-Karte, nicht in den
   Ergebnistabellen — Q-Reserve des Konzepts. Wer sie im Bericht braucht, braucht einen
   eigenen Migrationsschritt.
5. **Schrittnummer 63 ist jetzt belegt.** Wer als Nächstes einen Migrationsschritt
   braucht, nimmt **64**.
6. **`.wpx`-Pakete mit Schemastand 62 werden abgewiesen** — systemimmanent, wie bei jedem
   Migrationsschritt.
7. **Stufe E3 bleibt zurückgestellt** (Q6). Die Diodenparameter der CEC-CSV werden weiter
   verworfen; `N_s` und die fünf Referenzparameter haben nach wie vor keine Spalte.
8. **Die Windspalte in `Tab_Solar` (Q7) ist nicht nachgerüstet** — sie gehört zu Faiman
   (E3), nicht zu E2.

---

## 7. Offene Punkte

1. **Sichtabnahme durch Philipp** — drei Masken:
   * `Form_PV`: Panel jetzt zwei Spalten und vier Zeilen, Maske 57 px höher. **Die Maße
     sind gerechnet, nicht gesehen** — die Anwendung lief während der Umsetzung nicht.
     Mitzuprüfen ist der Assistentenmodus (der Rahmen soll mitwachsen).
   * `Form_PVModell` (neu): Feldbreiten, Live-Kennzahl, gesperrte Felder im Modell
     Einfach.
   * `Form_AdminPV`: Technologie-Auswahl unter dem NOCT-Feld.
2. **Fachliche Abnahme der Auslegung „vermiedener Bezug"** (Abschnitt 2.3): Der
   degradationsbedingte Mehrbezug steht als negativer Beitrag in der PV-Erlösreihe. Das
   Konzept nennt die Größe, aber nicht den Ort — die Entscheidung gehört bestätigt.
3. **Katalogpflege `Technologie`** für die sechs Referenzmodule; erst danach wird das
   Schwachlichtmodell in der Referenzmenge wirksam.
4. **Basiswechsel**, sobald der Anwender eine Anlage produktiv auf ERWEITERT stellt —
   `2026-09-03_PB1_nach-PaketB` gilt nur, solange alle Anlagen EINFACH rechnen.
5. **Cutover je Rechner:** Beim ersten Start dieses Codes läuft Schritt 63 nach.

---

## 8. Encoding-Nachweis

Alle berührten `.cs`-Dateien sind **UTF-8 mit BOM und CRLF** (bzw. behalten ihren
BOM-losen Zustand, wo der Bestand ihn hatte), vor dem ersten Edit je Datei gemessen und
danach nachgewiesen (CRLF-Zähler = LF-Zähler):

| Datei | BOM |
|---|---|
| `Allgemein/Simulation/PvErweitertesModell.cs` (neu) | ja |
| `Allgemein/Simulation/SimulationPV.cs` | ja |
| `Allgemein/SolarPVGISCalculator.cs` | ja |
| `Allgemein/Update/SchemaKatalog.cs`, `SchemaMigration.cs`, `Allgemein/DbWerte.cs` | ja |
| `Model/WErzeugerModel.cs`, `PhotovoltaikModel.cs`, `ProjektPhotovoltaikModel.cs` | ja |
| `Controller/WErzeugerCtrl.cs`, `WizardCtrl.cs`, `PhotovoltaikCtrl.cs`, `PhotovoltaikStammCtrl.cs`, `ProjektPhotovoltaikCtrl.cs` | ja |
| `Allgemein/Wirtschaftlichkeit/PvErloesRechner.cs` | ja |
| `Views/Photovoltaik/Form_PV.cs`, `Form_PVModell.cs` (neu), `Form_AdminPV.cs`, `Form_CECImport.cs` | ja |
| `Views/Wirtschaftlichkeit/Form_PhotovoltaikVerguetung.cs`, `Views/Simulation/Form_Simulation_Config.Karten.cs` | ja |
| `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` | ja |
| `Allgemein/Katalog/KatalogRegistry.cs`, `Allgemein/Bericht/AbweichungsErmittler.cs`, `Allgemein/KI/HilfeKontext.cs`, `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | **nein** (Bestandszustand, unverändert) |

Die beiden `.resx` enden weiterhin **ohne** abschließenden Zeilenumbruch (`</root>` als
letzte Bytes) — im Arbeitsbaum wie im Commit. Die Protokolldateien (`*.md`) folgen der
Hauskonvention CRLF.
