# Paket E1 — Ergebnis und Bericht je Kanal: Umsetzungsprotokoll

Stand: 27.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Kapitel 4.4 (Ergebnis je Kanal), 6.3 (`puffer_wp`-Ablösung), 10 (Oberfläche), Schritt 52,
Paketzeile E1. Build `WP-Plan.sln` + `Referenzlauf.csproj` x64 Debug: **0 Fehler**
(5 Bestandswarnungen). **Meilenstein Z3: Bedarf → Kaskade → Ergebnis → Bericht ist
durchgängig dreikanalig.**

## 1. Umfang

Die seit Paket K2 in der Engine geführte kanalindizierte Buchführung wird **persistiert,
angezeigt und berichtet**. Dazu kommen drei Nachzüge, die das Konzept ausdrücklich an E1
hängt: die Ablösung des Alias `puffer_wp` in der Ergebnisgröße `Kapazitaet_Pufferspeicher`
und in den Berichtszeitreihen (Befund S-1), die eigene Kantenfarbe des Prozess-Abnehmers
(Befund S2-O7) und die Vereinheitlichung des Solar-Deckungsnenners (Befund V0-O1).

E1 ist **rechnerisch neutral**: Kein Stundenwert und kein Bilanzskalar ändert sich. Die
einzigen geänderten Bestandswerte sind die vier `Kapazitaet_Pufferspeicher` der
`puffer_wp`-Ablösung (Abschnitt 5).

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Schritt 52** | Rein additives DDL, **kein DML**: `Tab_ErgebnisEnergiebedarf` + `Waermebedarf_Heizung/_Brauchwasser/_Prozess`; `Tab_ErgebnisWaermepumpe`, `-Heizkessel`, `-BHKW`, `-Solarthermie` je + `Deckung_Heizung/_Brauchwasser/_Prozess`; `Tab_ErgebnisPufferspeicher` + `Entladung_Heizung/_Brauchwasser/_Prozess`, `Durchsatz_Geladen`, `Durchsatz_Entladen`, `ID_Anlage` (LONG NULL), `T_oben_Mittel`, `T_oben_Min` (P1-VORGRIFF). **23 Spalten**, `ZIEL_VERSION` 51 → **52** | `SchemaKatalog.Schritt52_ErgebnisJeKanal` (Namenskonstanten + Begründung je Spalte), `SchemaMigration.SCHRITT_52_ERGEBNIS_JE_KANAL` / `Schritt_52_ErgebnisJeKanal` |
| **Kanalzeile am Speicher** | `SimulationPufferspeicher.Entladung_Kanal[3]`, gebucht in `Entladen(menge, stunde, kanal)` an genau der Stelle, an der auch `Entladung_gesamt` fortgeschrieben wird — aus derselben Größe `umsatz`, ohne zweiten Rundungsweg. Der Skalar bleibt getrennt akkumuliert | `SimulationPufferspeicher.cs:236-256, 497-560`, `Kaskadenschleife.cs` (`DurchsatzEntladen`, `EntladeKanal` geben den Kanal mit) |
| **Quellentnahme = Heizkanal** | Die Entnahme eines Moduls aus seinem Quellpuffer trägt keinen Bedarfskanal; sie wird auf HEIZUNG gebucht — dieselbe Konvention, die `Kaskadenschleife.Anteil_Entladen(sp, gedeckt)` seit K2 anwendet (altverhaltenserhaltende Vorbelegung, § 4.2/F18). Ohne sie wäre die Summenzusage für Quellspeicherzeilen nicht einlösbar | Vorbelegung in `Entladen`, Kommentar an den drei Modulstellen (`SimulationSPK.cs:476`, `SimulationWaermepumpe.cs:848, :1252`) |
| **Runner-Persistenz** | `BedarfJeKanal` (Bedarf aus `KanaeleDrei()`), `Summiere` (Eigenanteil je Kanal aus `Direktdeckung_Kanal` + `Speicherentladung_Kanal` + WP-`Heizstab_Kanal`), `DeckungJeKanal` (Bruch mit **demselben Nenner** wie der Skalar, danach auf ihn normiert). Alle drei `public static` — die Detailansicht rechnet damit dieselbe Formel (V0-7) | `SimulationRunner.cs` (Block „PAKET E1 — Ergebnis je Kanal") |
| **Modelle/Persistenz** | `ErgebnisEnergiebedarfModel.Waermebedarf_Kanal`, `Deckung_Kanal` in den vier Erzeugermodellen, `Entladung_Kanal`/`Durchsatz_*`/`ID_Anlage`/`T_oben_*` im Pufferspeichermodell; `ErgebnisCtrl` schreibt und liest sie (Helfer `KanalParameter`/`KanalLesen`/`DeckungLesen` — eine Reihenfolge statt achtzehn) + tolerante Rückfallebene `StelleKanalSpaltenSicher` nach dem Muster von `StelleKesselSpaltenSicher` | `Model/ErgebnisModel.cs`, `Model/ErgebnisPufferspeicherModel.cs`, `Controller/ErgebnisCtrl.cs` |
| **`puffer_wp`-Ablösung (S-1)** | `Kapazitaet_Pufferspeicher` = **Summe aller Senkenspeicher** des Laufs (`SimulationControl.SenkenspeicherKapazitaet()`, Quellspeicher ausgenommen, Verbund zählt einmal); `ZeitreihenExtraktor` liefert **je Speicher** eine SOC-Reihe unter `PUFFER_<ID>`/`QUELLE_<AnlagenID>`; `ChartRenderer.Speicherverlauf` zeichnet eine Linie je Speicher; `ErdreichAuswertung` fragt statt des Alias das WP-Modul | `SimulationControl.cs:2229-2263`, `SimulationRunner.cs:262-273`, `ZeitreihenExtraktor.cs:92-112`, `BerichtsDaten.cs` (`Speicherreihen`, `Beschriftungen`, Präfixe statt `PUFFER_SOC`), `ChartRenderer.cs`, `ErdreichAuswertung.cs:241` |
| **V0-O1 Solar-Nenner** | Der Solar-Deckungsgrad teilte als einziger durch den **Stufeneingang** statt durch den Projektbedarf. Jetzt derselbe Nenner wie bei WP, Kessel und BHKW — in Runner **und** Detailansicht. Der Solar-RESTbedarf bleibt bewusst auf dem Stufeneingang (Stufengröße, wie bei allen Erzeugern) | `SimulationRunner.cs` (Solarblock), `Form_Simulation_Detail.cs:3614-3650` |
| **Anzeige Bedarf** | Drei programmatische Kennzahlzeilen „Heizung / Brauchwasser / Prozesswärme" unter „Gesamter Wärmebedarf" auf der Bedarfsseite (Muster `BhkwKennzahlZeile`; Designer und `.resx` der Form unangetastet). Kein Nachrücken nötig — die linke Spalte endet bei y≈548, die Seite ist 721 hoch | `Form_Simulation_Detail.InitBedarfKanalzeilen` / `BedarfKanalzeilenFuellen` |
| **Anzeige Deckung** | `NavigatorUebersicht` (die Ergebnisanzeige des Reiters „Ergebnis") bekommt drei Spalten „Deckung *Kanal* [MWh/a]" — der **Eigenanteil** je Erzeuger und Kanal, gebildet mit `SimulationRunner.Summiere`. Der Heizstab hat wie bisher seine eigene Zeile und damit seine eigene Kanalzeile | `NavigatorUebersicht.cs` (Konstruktor, `FillTableWithData`, `Zeile`) |
| **Bericht** | `KennzahlenKatalog`: drei „davon"-Zeilen unter dem Wärmebedarf und drei **Deckungsgrade je Bedarfsart** (Erzeugeranteile × Gesamtbedarf / Kanalbedarf — die Umrechnung steht an genau EINER Stelle); `BausteineProjekt` weist beide Blöcke im Word-Bericht aus; 7 neue Wörterbucheinträge in `BerichtTexte` (de/en) | `KennzahlenKatalog.cs`, `Bausteine/BausteineProjekt.cs`, `BerichtTexte.cs` |
| **S2-O7 Kantenfarbe** | Fünfter Enum-Wert `SchemaModell.Kantenart.Prozess`, Farbe **#7E57A6** (Violett — der einzige freie Hue-Sektor mit Abstand zu Blau #378ADD, Koralle #D85A30 und Grün #1D9E75; Amber ist im Kartenstil mit „Warnung" belegt). Fünfter Legendeneintrag; die Strichelung hängt jetzt an einem eigenen Feld statt an der hart kodierten Position `i == 3`; Legendenhöhe von 2 auf 3 Zeilen | `SchemaModell.cs:76-93, 709, 746, 902`, `SchemaAnsicht.cs:466-495, 256, 713-760` |

**Neue Ressourcen (3, de + en + Designer):** `SIM_SCHEMA_LEGENDE_PROZESS`,
`SIM_LABEL_BEDARF_JE_KANAL`, `SIM_SPALTE_DECKUNG_KANAL`. Bestand danach **2538** Schlüssel,
DE/EN deckungsgleich, 2538 Designer-Eigenschaften. Die Kanalnamen selbst nutzen die
`KANAL_*_ANZEIGE` aus Paket K1 — kein vierter Katalogeintrag. Nachtrag in
[`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md).

## 3. Die Invariante — und warum sie exakt gilt

> **Summe der drei Kanalwerte == der jeweilige Bestandsskalar.**

Sie ist keine Zusage über zwei getrennte Rechnungen, sondern über **eine**:

- **Bedarf**: Die Kanäle sind seit K1 die führende Größe, `Waermebedarf` ihre Summe.
  `BedarfJeKanal` summiert dieselben Vektoren mit derselben Umrechnung. Rest: allein die
  float-Rundung, mit der `Kanalsatz.Summe()` je Stunde addiert.
- **Erzeuger-Deckung**: Zähler und Nenner sind dieselben wie beim Skalar; nur der Zähler ist
  kanalindiziert. `DeckungJeKanal` **normiert** die drei Rohwerte anschließend auf den
  führenden Skalar (Faktor im Normalfall 1 ± 1e-12) — damit gilt die Zusage auch im
  Klemmfall (Skalar 0..100 geklemmt, Kanalwerte nicht) und über die getrennten
  double-Akkumulatoren der Engine hinweg.
- **Puffer-Entladung**: Beide Größen entstehen in derselben Zeile aus derselben Variablen
  `umsatz`; der Skalar wird ausdrücklich **nicht** aus der Kanalzeile aufsummiert.

**Was NICHT gespeichert wird:** der Deckungsgrad EINES Kanals („die WP deckt 80 % des
Brauchwasserbedarfs"). Er ist aus `Deckung_<Kanal>` und `Waermebedarf_<Kanal>` ableitbar
(`Deckung_Kanal · Gesamtbedarf / Kanalbedarf`) und stünde als eigene Spalte als zweite
Wahrheit daneben. Die eine Umrechnung liegt im `KennzahlenKatalog`.

**Keine Deckungsspalten an den MODULtabellen:** Die Eigenanteils-Buchführung der Engine ist
je **Erzeugerart** gebildet (`Kaskadenschleife._entladungJeArtKanal`), nicht je Modul; eine
Modulspalte müsste den Anteil erfinden.

## 4. Verifikation

**Lauf:** `2026-08-27_E1_Probe`, dreizehn Projekte, 329 CSV, 13/13 erfolgreich.
**Datenquelle:** produktive `Kenndaten.accdb` (Zeitstempel 27.08.2026 **20:45**, keine
`Kenndaten.laccdb`, kein Access-/App-Prozess), nur gelesen; die Arbeitskopie wurde von
Schemastand 51 auf **52** migriert.

### 4.1 Byte-/MD5-Vergleich gegen die Basis `2026-08-27_A1`

| | Zahl | Zuordnung |
|---|---|---|
| CSV gesamt | 329 ↔ 329 | keine fehlende, keine zusätzliche Datei |
| **byte-/MD5-gleich** | **309** | **alle Ganglinien der zwölf unveränderten Projekte** |
| abweichend: `aggregate.csv` | 13 | zwangsläufig — jede trägt die neuen Schlüssel |
| abweichend: Ganglinien | 7 | **alle in Projekt 1042**, Ursache Datenänderung (4.3) |

Ganglinien insgesamt 316; **309 von 316 byte-gleich, die 7 Ausnahmen ausschließlich in
1042.** Für die zwölf übrigen Projekte ist E1 damit auf Stundenebene nachweislich
verhaltensneutral.

### 4.2 Toleranzvergleich (`vergleich … --ohne <39 neue Schlüssel>`)

```
Projekt_1007  PASS (29 Dateien, 324 219 Werte)     Projekt_1023  PASS (25, 262 935)
Projekt_1008  PASS (21 Dateien, 227 861 Werte)     Projekt_1030  PASS (22, 236 667)
Projekt_1011  PASS (29 Dateien, 324 241 Werte)     Projekt_1024  FAIL — 1 Abweichung
Projekt_1017  PASS (21 Dateien, 254 152 Werte)     Projekt_1039  FAIL — 1 Abweichung
Projekt_1018  PASS (22 Dateien, 236 659 Werte)     Projekt_1040  FAIL — 1 Abweichung
Projekt_1021  PASS (21 Dateien, 227 854 Werte)     Projekt_1041  FAIL — 1 Abweichung
                                                   Projekt_1042  FAIL — 78 983 (Daten)
```

**Die vier Einzelabweichungen sind ausnahmslos `Waermepumpe.Kapazitaet_Pufferspeicher`** —
die dokumentierte `puffer_wp`-Ablösung (Abschnitt 5). **Keine weitere Wertänderung im
gesamten Vergleich.**

**Die 39 neuen Schlüssel** (`--ohne`-Liste):

```
Energiebedarf.Waermebedarf_{Heizung,Brauchwasser,Prozess}
{Waermepumpe,Heizkessel,BHKW,Solarthermie}.Deckung_{Heizung,Brauchwasser,Prozess}
Pufferspeicher[i].{Entladung_Heizung,Entladung_Brauchwasser,Entladung_Prozess,
                   Durchsatz_Geladen,Durchsatz_Entladen,ID_Anlage,
                   T_oben_Mittel,T_oben_Min}          (i = 0…3 je Projekt)
```

### 4.3 Projekt 1042 — Datenänderung, kein Codeeffekt

1042 (Booster-Kette) ist der Fall, den **A1-O7 vorhergesagt hat**: „mit unkonfigurierter
Quelle eingefroren … nach Anwender-Konfiguration wird die Basis erneuert". Der Anwender hat
das Projekt zwischen dem A1-Einfrieren (Quelle 27.08. 18:33) und diesem Lauf (Quelle 20:45)
umgebaut. Der Beweis steht **in den Daten, nicht in den Zahlen**:

| Befund | A1-Basis | jetzt |
|---|---|---|
| WP-Module | 3 (`LS 16-B R`, `CS7800iLW 16`, `CS6800iAW…`) | **2** (`CS7800iLW 16`, `CS6800iAW…`) |
| `WaermepumpeModul[0].Leistung` | 10 | **15** |
| Speicher im Lauf | 4 | **3** |
| `Pufferspeicher[0].Bezeichner` | `allSTOR exclusiv VPS 800/3-7` | **`Stora B 1000-6 ER 1 B`** |

Kontrollabfrage auf der Arbeitskopie: `Tab_Pufferspeicher WHERE ID_Projekt=1042` liefert
**genau drei** Zeilen (1054196, 1054197, 1054198) — der Speicher **1054195**
(`allSTOR exclusiv VPS 800/3-7`), in der A1-Basis `Pufferspeicher[0]`, **existiert in der
Datenbank nicht mehr**. Modulnamen, Speichernamen und Gerätezahlen kann kein Codestand
verändern.

### 4.4 Kanal-Summenprobe

Skript über alle 13 `aggregate.csv`; Toleranz `max(0,02; 1e-4·|Skalar|)` — 0,02 ist das
Rundungsraster von `ErgebnisCtrl.R()` über drei Summanden (je ±0,005) plus den Skalar.

```
Geprüft: 54 Invarianten (13 × Bedarf, 24 × Erzeuger-Deckung, 17 × Puffer-Entladung)
FAIL: 0        größter Rest: 0,01   (= eine Stelle des 2-Dezimal-Rasters)
```

Belege aus der Probe:

| Projekt | Größe | H | BW | P | Σ | Bestandsskalar |
|---|---|---|---|---|---|---|
| 1011 | Wärmebedarf [MWh] | 4 736,19 | 4,06 | **365,00** | 5 105,25 | 5 105,25 |
| 1041 | Wärmebedarf [MWh] | 124,74 | 5,00 | **30,00** | 159,74 | 159,74 |
| 1024 | WP-Deckung [%] | 13,87 | 10,93 | 0,00 | 24,80 | 24,80 |
| 1024 | BHKW-Deckung [%] | 39,32 | 1,97 | 0,00 | 41,29 | 41,29 |
| 1041 | WP-Deckung [%] | 0,00 | 0,00 | **18,78** | 18,78 | 18,78 |
| 1041 | Puffer[0] „Kombi" Entladung [kWh] | 122 029,08 | 5 000,01 | 0,00 | 127 029,09 | 127 029,09 |
| 1023 | Puffer[0] Entladung [kWh] | 70 517,03 | 0,00 | 0,00 | 70 517,03 | 70 517,03 |
| 1024 | Puffer[0] „Brauchwasser" [kWh] | 0,00 | 7 647,05 | 0,00 | 7 647,05 | 7 647,05 |

Die Probe trifft die drei Fälle, auf die es ankommt: einen **echten Prozesskanal** (1011,
1041), einen **Kombispeicher mit zwei belegten Kanälen** (1041) und einen reinen
**Brauchwasserspeicher** (1024).

### 4.5 Die neuen Größen im Einzelnen

- **Durchsatzsummen** (bis E1 nur am Objekt, „NICHT PERSISTIERT … vorgemerkte Erweiterung"):
  erstmals sichtbar und bei den Puffer-Hauptsenken der Referenzmenge substanziell —
  1030: 3 191 964 kWh, 1039: 262 456 kWh, 1023: 39 121 kWh, 1018: 32 572 kWh,
  1008: 24 281 kWh, 1040: 3,09 kWh. Ohne Durchlass exakt 0 (1021, 1024, 1041, 1042).
- **`ID_Anlage`**: 15 Pufferzeilen, **genau eine belegt** — 1021, `Pufferspeicher[0]`,
  Anlage **10361** (die einzige Quellspeicherzeile der Referenzmenge). Senkenspeicher
  bleiben NULL. Genau die Zuordnung, die die Ganglinien `quellspeicher_10361_*.csv` tragen.
- **`T_oben_Mittel` / `T_oben_Min`**: **30 Einträge, 0 belegt** — der P1-Vorgriff schreibt
  wie zugesagt nichts.

### 4.6 V0-O1 — behoben, in dieser Referenzmenge ohne Wirkung

Der Befund bestand noch (`SimulationRunner`, Solarblock) und ist behoben. Er ändert in der
Referenzmenge **keinen Wert**, weil die Solarthermie dort überall an erster Kaskadenposition
steht und beide Nenner damit gleich sind:

| Projekt | Stufeneingang Solar [MWh] | Projektbedarf [MWh] | Deckung alt | Deckung neu |
|---|---|---|---|---|
| 1007 | 56,90 | 56,90 | 0,00 % | 0,00 % |
| 1011 | 5 105,25 | 5 105,25 | 0,01 % | 0,01 % |
| 1042 | 0,00 | 64,31 | 0,00 % | 0,00 % |

Die Wirkung tritt ein, sobald ein Projekt die Solarthermie **hinter** einem anderen Erzeuger
führt — dann wies sie bisher einen zu hohen Deckungsgrad aus, und die Summe der
Erzeugerdeckungen konnte über 100 % laufen. Ein solches Projekt fehlt der Referenzmenge
(offener Punkt E1-O2).

### 4.7 Weitere Nachweise

- **Plausibilität** (`pruefen`): 13/13 **plausibel**, keine NaN/Inf; die drei Hinweise
  („Gewerk aktiviert, aber kein Modul") sind Bestand.
- **Selbstvergleich**: zweiter Lauf desselben Codes auf derselben Quelle —
  **329/329 CSV byte-/MD5-gleich**. Reproduzierbar.
- **Migrations-Idempotenz**: Erstlauf 51 → 52 legt **23 Spalten** an
  (3+3+3+3+3+8, Protokoll der Arbeitskopie). Zweitlauf mit **hartem Marker-Rücksetzen auf
  51** auf derselben, bereits migrierten Kopie: **0 Spalten angelegt, 23 bereits
  vorhanden**, Stand danach 52.
- **Schema-Nachweis** des Migrationslaufs: eine Abweichung, „PufferHeizung ohne
  `WS_ID_Puffer`: 2 (erwartet 0)" — der Bestandsdatenbefund **V0-O6**, unverändert.
- **Access-Feldgrenze**: `Tab_ErgebnisPufferspeicher` wächst 13 → 21 Spalten, keine
  Erzeugertabelle über 26. Abstand zu 255 an keiner Stelle knapp.

## 5. Die eine gewollte Wertänderung: `Kapazitaet_Pufferspeicher`

`puffer_wp` ist der **erste Heizungspuffer in Aufnahmereihenfolge**. Als Bezugsgröße der
WP-Kennzahl war das in zwei Fällen schlicht falsch: bei mehreren Speichern zeigte sie einen
davon, und bei einem reinen Brauchwasser- oder Kombispeicher meldete sie 0, obwohl der Lauf
einen Speicher bewirtschaftet hat. Neu ist es die Summe aller **Senkenspeicher**
(Quellspeicher bleiben draußen — sie sind Wärmequelle, kein Vorrat für den Bedarf; ein
Parallelverbund zählt einmal, sein Leitspeicher trägt bereits die Summe).

| Projekt | vorher | nachher | Grund |
|---|---|---|---|
| 1024 | 0 | **10,44** | einziger Speicher ist ein **Brauchwasser**speicher — der Alias fand keinen |
| 1039 | 34,80 | **45,99** | Heizung 34,80 + Brauchwasser 11,19 |
| 1040 | 45,99 | **80,79** | Heizung (Verbund) 45,99 + Brauchwasser 34,80 |
| 1041 | 0 | **80,79** | zwei **Kombi**speicher — der Alias fand keinen |
| 1008, 1023 | unverändert | | genau ein Heizungspuffer |
| 1021 | 0 | 0 | nur ein Quellspeicher |
| 1007, 1011, 1017 | 0 | 0 | kein Speicher |
| 1042 | 0 → 80,79 | | überlagert von der Datenänderung (4.3) |

## 6. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| E1-O1 | **Die Referenzbasis ist neu zu setzen.** `2026-08-27_A1` trägt für 1042 einen Datenstand, den es nicht mehr gibt (4.3), und kennt die 39 neuen Schlüssel nicht. Die Neusetzung macht der Orchestrator; der Probeordner ist gelöscht | Orchestrator |
| E1-O2 | Der V0-O1-Fix ist in der Referenzmenge **nicht messbar** — es fehlt ein Projekt mit Solarthermie an nachgelagerter Kaskadenposition. Bis dahin ist die Korrektur begründet, aber nicht regressionsgesichert | Referenzprojekt |
| E1-O3 | Das **Referenzlauf-Werkzeug** benutzt `sim.puffer_wp` weiter (`puffer_soc/_ladung/_entladung.csv`, `Sim.PufferWP_vorhanden`, `Puffer.*`). Bewusst unangetastet: Es ist das Messinstrument, und geänderte Dateinamen machten jeden Vergleich mit älteren Ständen unmöglich. Umstellung nur zusammen mit einem Basiswechsel | Basiswechsel |
| E1-O4 | `SimulationControl.puffer_wp` bleibt als Engine-Verdrahtung (`simulation_wp.Pufferspeicher = puffer_wp`, `SimulationControl.cs:392`). Das ist **kein Anzeigeweg** und keine E1-Frage; welchen Speicher die Wärmepumpe lädt, entscheidet die Senkenliste. Der Alias selbst fällt mit dem Aufräumpaket | Paket L |
| E1-O5 | `T_oben_Mittel`/`T_oben_Min` sind angelegt und bleiben NULL — sie werden mit dem Schichtmodell gefüllt | P1 |
| E1-O6 | Die Kanalaufteilung einer **Quellentnahme** ist eine Näherung (Heizkanal, § 4.2/F18). Sie betrifft ausschließlich die kanalfeine Anzeige, nie eine Bilanzsumme; mit der einheitlichen `Stunde_*`-Schnittstelle könnte das Modul den Kanal mitmelden | B1/Aufräumen |
| E1-O7 | Der **Deckungsgrad je Kanal** wird im Bericht aus zwei gespeicherten Größen gerechnet (`KennzahlenKatalog.DeckungKanal`). Kommt eine dritte Anzeige dazu, gehört die Umrechnung in eine gemeinsame Hilfsklasse statt in den Katalog | bei Bedarf |
| E1-O8 | Die Legende der `SchemaAnsicht` reserviert jetzt **drei** Zeilen. Ein sechster Eintrag bräuchte entweder eine gerechnete Höhe oder eine vierte Zeile — die feste Zahl ist mit E1 an ihrer Grenze | P2 |
| E1-O9 | Bestandsbefund unverändert: „PufferHeizung ohne `WS_ID_Puffer`: 2" (V0-O6) — Datenlage, kein Codefehler | Anwender |
