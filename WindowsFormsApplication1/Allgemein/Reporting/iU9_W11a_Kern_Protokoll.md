# iU9 Welle 11a — Simulationsergebnis I: Kern, Renderer, Fortschritt

> Umsetzung 04.09.2026 auf `ios_migration` (Basis `427fd59`, danach `cd849f8`
> und **`a398c9a` mit der ganzen Welle 10b** eingemergt — siehe § 11).
> Vermessung: `iU9_W11_Vermessung.md` (Stand `b6a72b0`, 50 Befunde). **Keine Maske gelöscht** — W11a verlegt, was ohne Oberfläche geht,
> in den Kern und hängt die sechs WinForms-Masken schon daran; W11b baut danach
> die Ergebnisseite und löscht die Masken.
>
> **Das Gate dieser Welle ist der Referenzlauf: byte-gleich für 1030/1007/1017.**
> Es wurde nach **jedem** Teilschritt gefahren und war jedes Mal grün.

---

## 1. Was wo hingezogen ist

| Teil | Von | Nach |
|---|---|---|
| **W11a.1** | `Views/Simulation/ErgebnisPraesenz.cs` (182 Z., `internal`) | `EPOS.Kern/Allgemein/Simulation/ErgebnisPraesenz.cs`, **`public`** |
| | `GanglinienDarstellung.Dauerlinie/Anzeigewerte` (:29–48) | `EPOS.Kern/Allgemein/Simulation/Ganglinie.cs` |
| **W11a.2** | 8 × `"select * from Tab_Einstellungen where ID_Projekt=" + id` | `KonfigurationCtrl.LiesProjekt(int)` / `ProjektLesen(int)` |
| | `SELECT DISTINCT k.Brennstoff …` (`Detail:1194–1221`) | `HeizkesselStammCtrl.BrennstoffartenJeProjekt(int)` |
| | `SELECT ID, Bezeichner … ORDER BY ID` + `SELECT COUNT(*)` | `WErzeugerCtrl.AnlagenJeTyp(int,int)` |
| | 2 × `ReadAllFilter("ID_Projekt=… and ID_Type=…")` | `WErzeugerCtrl.ModelleJeTyp` / `LesenJeTyp` |
| | `SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?` (`:6414`) | `WErzeugerCtrl.AnlagenBezeichner(int)` |
| | `SpGeraetedaten` (`:6446–6510`, Abfrage **und** Rückfall) | `StromspeicherStammCtrl.KapazitaetUndLeistung(int,int)` |
| | `RecordSet`-Schleife (`TabNavigationManager:142–154`) | `StromspeicherStammCtrl.KapazitaetJeProjekt(int)` |
| **W11a.3** | die fünf Eigenanteil-Rechnungen (`:4217`, `:4392`, `:4483`, `:4635` und der Runner) | `SimulationRunner.EigenanteilWpMwh/…KesselMwh/…SolarKwh/…BhkwMwh` + `RestNachEigenanteil` + `DeckungProzent` |
| | `Endergebniss_Simulation` (:4158–4742), `FuelleUebersicht` (:3764–3801), `NavigatorUebersicht.SetControl` (:260–278), `Energiebedarf`-Felder, `BedarfKanalzeilenFuellen`, `PufferspeicherErgebnisAnzeigen`, `WarmwasserAnteil` | `EPOS.Kern/Controller/SimulationErgebnisCtrl.cs` (7 DTO + 4 Helfer) |
| | `SpKennzahlenFuellen` (:7287–7400) mit `Vgl`, `SpVerkaufKwh`, `SpBudgetzeilen`, `SpBudgetfarbe`, `SpZeile` ×2, `SpZeileText`, `SpAmpelfarbe` | `EPOS.Kern/Controller/SpeicherKennzahlenBlock.cs` + `KennzahlStufe` |
| **W11a.4** | `btn_Simulation_Click` (:3419–3531), `LaufAbgebrochen` (:3551–3578), `SpeichereErgebnis` (:3712–3760), `Energiebedarf` (:3948–3990) | `EPOS.Kern/Controller/SimulationLaufCtrl.cs` |
| | — | `SimulationControl.Do_Simulation(int, IProgress<LaufFortschritt>?, CancellationToken)` und `Allgemein/Simulation/LaufFortschritt.cs` |
| **W11a.5** | `BetriebsartText`/`BerechnungsartText`/`AmortisationText` (3 Masken) | `EPOS.Kern/Controller/SpeicherAnzeigeCtrl.cs` |
| | `co2Saved = … * 0,42 + … * 0,20` (`DashboardForm:355`) | `EmissionsVorgaben.CO2_NETZSTROM_KG_JE_KWH` / `CO2_WAERME_KG_JE_KWH` / `Co2ErsparnisKg` |
| **W11a.6** | — | `ChartRenderer.GanglinieNormiert` · `ErzeugerStapel` · `Streuwolke` · `Ring` · `MonatsStapel` · `Temperaturverlauf`; `Reihe` um `Stapelgruppe`/`Gestrichelt`/`Breite` |
| **W11a.7** | — | `EPOS.UI/Bausteine/Fortschritt.razor` |

**Aufruferlos geworden und gelöscht** (alle in `Form_Simulation_Detail`):
`Vgl`, `SpVerkaufKwh`, `SpBudgetzeilen`, `SpBudgetfarbe`, `SpZeile` (zwei
Überladungen), `SpZeileText`, `SpAmpelfarbe`, die Konstante `SP_ERG_UNBESTIMMT`.

---

## 2. Der Zahlenabzug (W11a.3)

**Wie er entstanden ist.** Ein Wegwerf-Harnesch unter `dev/Zahlenabzug/`
(gitignored) rechnet `SimulationRunner.Simuliere(projekt)` gegen
`Referenzlaeufe/Kenndaten_Test.sqlite` und gibt **95 Kennzahlen** aus. Modus
`alt` rechnet **wörtlich so, wie die Maske es tat** (abgeschrieben aus
`Endergebniss_Simulation`, `FuelleUebersicht`, `NavigatorUebersicht.SetControl`);
Modus `neu` fragt den `SimulationErgebnisCtrl`. Beide Abzüge wurden für **drei**
Projekte gezogen und verglichen.

**Ergebnis: von 95 Kennzahlen sind 92 unverändert.** Die drei Abweichungen:

| # | Kennzahl | 1030 alt → neu | 1007 alt → neu | 1017 alt → neu | Grund |
|---|---|---|---|---|---|
| **A‑1** | `gesamt_waerme` (Ergebnisblock) | 5 403,10 → **6 139,32** | 50,85 → 50,85 | 8,89 → **62,91** | W11‑B35 |
| **A‑2** | `restwaermebedarf` (Ergebnisblock) | 734,46 → **−1,76** | 6,04 → 6,04 | 54,02 → **0,00** | W11‑B35 |
| **A‑3** | PV-Deckungsgrad | NaN → **0,00** | 9,28 → 9,28 | NaN → **0,00** | W11‑B22 |

### A‑1/A‑2 — W11‑B35, die sechs Summen standen zweimal

Die Vermessung nennt **zwei** Unterschiede zwischen `Form_Simulation_Detail`
:4720–4734 und `NavigatorUebersicht.SetControl` :266–275. Nachgemessen ist nur
**einer** davon einer:

1. **Die Kesselwärme ist KEINE Abweichung.** Die Detailansicht summierte
   `s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i]` über die Kesselliste, der Navigator
   nahm `S_Waerme_spk`. Letzteres entsteht in
   `SimulationSPK.Bilanz_und_Nutzungsgrad` :201–202 aus **genau dieser Summe über
   genau diese Liste** — zwei Wege, ein Wert. Ein Testfall hält das fest.
2. **Das BHKW ist die Abweichung.** Der Navigator zählt `waerme_bhkw` mit, die
   Detailansicht nicht.

**Genommen ist die Navigator-Fassung (mit BHKW).** Begründung: Das BHKW ist eine
Kaskadenstufe wie die anderen, und `SimulationControl.Restwaerme` — die Wahrheit
des Referenzlaufs, gespeichert als `Tab_Ergebnis.Waermerestbedarf` — zieht seine
Lieferung ebenfalls ab. Die Zahlen belegen es:

| Projekt | Lauf (`sim.Restwaerme`) | alt (ohne BHKW) | neu (mit BHKW) |
|---|---|---|---|
| 1017 | **0,00** | 54,02 | **0,00** |
| 1030 | **0,00** | 734,46 | **−1,76** |
| 1007 | 6,04 | 6,04 | 6,04 (kein BHKW) |

Für 1017 trifft die neue Fassung den Lauf **exakt**. Für 1030 bleibt eine
Restdifferenz von **−1,76 MWh** — sie ist die Kehrseite der Größe selbst:
„Produktion" ist nicht „Deckung". Geladene Speicherwärme steht in der Produktion
und deckt trotzdem keinen Bedarf; bei 2 947 MWh Ladung und 2 946 MWh Entladung
über den Senkenspeicher summiert sich das auf gut anderthalb MWh.

→ **Offener Punkt W11a‑O‑1** (unten).

### A‑3 — W11‑B22, PV-Deckungsgrad ohne Nullprüfung

`Stromproduktion.Sum() * 100 / Strombedarf_stuendlich.Sum()` teilte ungeprüft.
In **zwei von drei** Referenzprojekten steht im Bestand deshalb `NaN` im Feld —
das ist kein Randfall, das ist der Fall. Die Nachbarzeilen der Maske prüfen alle
(`:4678`, `:4688`, `:4401`, `:4493`). Neu: 0,00.

### Nicht sichtbar in den drei Projekten, aber mitbehoben

| Befund | Was war | Was ist |
|---|---|---|
| **W11‑B15** | `WP_Laufzeit / wp_list.Count` ohne Nullprüfung → `∞` bei leerer Liste | Nullprüfung wie im Runner; eigener Testfall mit leerer Liste |
| **W11‑B16** | `for (i = 0; i < 8750; …)` — die letzten **zehn** Jahresstunden fehlten bei der Mindest-Spitzenkesselleistung | über die ganze Ganglinie (`Length`), wie `SimulationRunner:295–298`. In 1007 liegt das Maximum nicht in den letzten zehn Stunden, der Wert bleibt 20,22 kW |
| **W11‑B19** | `tb_Koks.Text` zweimal mit demselben Wert gesetzt (:4418, :4425) | ein DTO-Feld gibt es einmal |
| **W11‑B20** | die Solarthermie-Felder blieben nach einem Folgelauf ohne Solarthermie stehen | `Solarthermie(...)` ist dann `null` — die Rubrik kann nichts Altes zeigen |

### Berichtigung zur Vermessung

Der Kennzahlenblock des Stromspeichers hat **39 Zeilen (17 Energie, 8 Speicher,
14 Wirtschaft)**, nicht 40 (18/8/14). Die Eigenverbrauchsquote steht in einer
`if`/`else`-Verzweigung und ist in der Vermessung offenbar doppelt gezählt worden.

---

## 3. Die Fadenprüfung (W11a.4)

**Frage:** Liest `Do_Simulation` intern die Datenbank, und trifft `Task.Run` sie
damit aus einem fremden Faden?

**Antwort: ja, es liest — und das ist unschädlich. Vorgezogen wurde deshalb KEIN
Lesevorgang.**

Grundlage ist Probe **R‑W10a‑2** (`EPOS.Kern.Tests/SimulationslaufAusFremdemFadenTests`,
W10a-Protokoll § 2): `SqliteDatenzugriff` öffnet je Aufruf eine eigene Verbindung,
hält nichts `[ThreadStatic]` und nichts statisch Offenes;
`DataRepository.EngineModus` ist ein Zähler auf einem statischen Feld, kein
Fadenzustand. Zwei neue Fälle belegen dasselbe für den Weg über
`SimulationLaufCtrl.Laufen` (`Laufen_im_Task_liefert_dasselbe_Ergebnis`,
`Laufen_im_Task_laesst_sich_abbrechen`).

Die Aufteilung folgt trotzdem `Form_SpeicherOptimierung` (Klassenkopf :29–46),
weil sie den Ablauf lesbar hält:

| Faden | Was |
|---|---|
| **UI** | `Vorpruefen` (Konfiguration, Netzverluste, Klimaregion), `Bedarf` (Wärme- und Stromrechnung), `Bestuecken` (samt `PufferSpCtrl.PendelspeicherVolumenLiter` — der letzte Datenbankzugriff vor dem Lauf) |
| **Hintergrund** | ausschließlich `SimulationLaufCtrl.Laufen` |
| **Marshalling** | `Progress<T>`, auf dem UI-Faden erzeugt |

**Zwei GLEICHZEITIGE Läufe bleiben ausgeschlossen** — `EngineModus` ist
prozessweit. Der Knopf ist für die Dauer des Laufs gesperrt.

### Die Phasen — fünf, nicht „je Erzeuger"

Die Arbeitsanweisung nennt „je Erzeuger der Kaskade". Das gibt der Rechenweg
nicht her: `Kaskade_Zweikanalig` läuft **stundenweise** über das Jahr und bedient
in jeder Stunde alle Erzeuger nacheinander (Phasen A–G der `Kaskadenschleife`).
Es gibt keinen Zeitpunkt, ab dem „die Wärmepumpe fertig" wäre. Gemeldet werden
deshalb `Start` (0,00), `Kaskade` (0,10), `Photovoltaik` (0,60),
`Stromspeicher` (0,75), `Abschluss` (0,90); geprüft wird der Abbruch **zwischen**
ihnen. Bis zur ersten Meldung läuft der Balken unbestimmt.

### Eine sichtbare Folge, ausdrücklich abgefangen

Der Automatikstart aus `Form_Simulation_Detail_Load` (:3334) lief bis hierher
**synchron** und setzte an seinem Ende den Reiter „Simulation" — danach holte die
Zeile darunter die „Übersicht" nach vorn. Asynchron käme der Lauf **nach** ihr an,
und der Anwender landete nach dem Öffnen auf einem anderen Reiter als bisher. Ein
Merker (`_laufAusLoad`) lässt die Reiterwahl beim Automatikstart deshalb aus; die
sichtbare Endlage bleibt die „Übersicht". Der Aufruf selbst steht **wörtlich** wie
zuvor.

---

## 4. Die vierzehn neuen Proben (W11a.6)

`Proben/ChartProben`: **16 → 30 Bilder, 0 Verstöße.** Je Bild ein „voller" und
ein „magerer" Fall; der magere ist der wichtigere — er trifft die
Präsenzfilterung, und genau dort brachen die Vorläufer (drei parallele Listen,
die gemeinsam gefiltert werden mussten, `NavigatorUebersicht` :304–306).

| Bild | Maß | Proben | Deckt |
|---|---|---|---|
| **B1** `GanglinieNormiert` | 1240 × 560 | `ganglinie_normiert_chronologisch` (4 Reihen, Monatsachse), `ganglinie_normiert_sortiert` (1 Reihe, Stundenachse) | `chart1`, `chart2` |
| **B2** `ErzeugerStapel` | 1240 × 560 | `erzeugerstapel_waerme` (5 Stapel + Kontur + zweite Achse), `erzeugerstapel_strom_viertelstunden` (4 Stapel + 2 Linien, 35 040 Werte), `erzeugerstapel_kessel_sortiert`, `erzeugerstapel_solar_zwei_linien` | `chart3`, `chart_Kessel`, `chart8`, `chart_BHKW_Waerme`, `chart_Waerme`, `chart7` |
| **B3** (Option von B2) | — | in `erzeugerstapel_waerme` enthalten | `chart_PV`, Bedarfslinie in `chart_Waerme` |
| **B4** `Streuwolke` | 1240 × 560 | `streuwolke_drei_reihen`, `streuwolke_eine_reihe` | `chart4` |
| **B5** `Ring` | 720 × 560 | `ring_waermedeckung` (5 Segmente), `ring_stromdeckung` (4 Segmente, eines mit Wert 0) | die zwei GDI-Donuts |
| **B6** `MonatsStapel` | 978 × 542 | `monatsstapel_drei_reihen`, `monatsstapel_eine_reihe` | `chartSolar` |
| **B7** `Temperaturverlauf` | 1240 × 560 | `temperaturverlauf_zwei_speicher` (2 Speicher + 1 Quelle), `temperaturverlauf_ein_speicher` (Mindestspanne greift) | `chart_Speichertemperatur` |

**Nachtrag Windows-Abnahme 05.09.2026.** Zwei der sieben Bilder haben seither
Zuwachs, die Maße aller sieben sind unverändert:

* **B1 und B2** kennen einen wahlfreien `Achsenfenster`-Parameter — den DATENZOOM
  (Befund A‑1, § 10a des W11b-Protokolls). Ohne ihn zeichnen sie Bildpunkt für
  Bildpunkt dasselbe wie vorher; zwei neue Proben
  (`ganglinie_normiert_fenster`, `erzeugerstapel_fenster`) und zwei Gegenproben
  halten beides fest.
* **B4** hat eine **runde Achsenteilung** und Ränder bekommen (Befund W11b‑B‑3):
  Die fünf gleichmäßig verteilten Marken ergaben krumme Temperaturen
  („−18,2 … −5,3 … 7,7"), rechts stand die letzte Marke auf der Bildkante, und
  Legende und y-Achsentitel berührten sich.

Dazu **19 Renderer-Tests** im Kern (`EPOS.Kern.Tests/ErgebnisbilderTests.cs`):
Maße, Determinismus, Leerfall, die dynamische Legende des Rings, die Mindestspanne
der Temperaturachse, der fehlende Stapel im sortierten Modus, die Wirkung von
`Gestrichelt` und der unveränderte alte `Reihe`-Konstruktor.

**Die bestehenden Berichtsbilder sind unangetastet** (`JahresverlaufWaerme`,
`DauerlinieWaerme`, `StrombilanzMonate`, `Speichertemperaturen`) — ChartProben
prüft sie weiter. Ihre Zusammenführung mit den neuen ist **W11a‑O‑3**.

---

## 5. Abweichungen

| # | Was | Warum |
|---|---|---|
| **A‑1** | Ergebnisblock der Detailansicht zählt das BHKW mit — `gesamt_waerme` und `restwaermebedarf` ändern sich (Zahlen § 2) | W11‑B35, eine Wahrheit statt zweier |
| **A‑2** | PV-Deckungsgrad `NaN` → `0,00` | W11‑B22 |
| **A‑3** | Mindest-Spitzenkesselleistung über 8 760 statt 8 750 Stunden | W11‑B16 |
| **A‑4** | Vollbenutzungsstunden der WP mit Nullprüfung | W11‑B15 |
| **A‑5** | `tb_Koks` wird einmal statt zweimal gesetzt | W11‑B19 |
| **A‑6** | Die Solarthermie-Rubrik ist nach einem Lauf ohne Solarthermie leer statt veraltet | W11‑B20 |
| **A‑7** | Der Simulationslauf läuft nebenläufig, mit Balken und Abbrechen | W11‑B48 |
| **A‑8** | Beim Automatikstart bleibt die Reiterwahl am Laufende aus | Folge von A‑7, siehe § 3 |
| **A‑9** | Die CO₂-Faktoren stehen im Kern statt in der Oberfläche — **Werte unverändert** | W11‑B31 |
| **A‑10** | `AmortisationText` formatiert einheitlich mit `"N1"` (vorher zusätzlich `"0.0"`) | W11‑B42; beide Formate sind gleich, solange die Amortisationszeit unter 1 000 Jahren bleibt |
| **A‑11** | `StromspeicherStammCtrl.KapazitaetJeProjekt` summiert über einen JOIN statt über eine Schleife mit Einzelabfragen | W11‑B45; gleiches Ergebnis, außer wenn `Tab_Stromspeicher.Energie` NULL ist — dann übergeht der JOIN die Zeile, während der Vorläufer mit einer Ausnahme abgebrochen wäre |
| **A‑12** | Die Berechnungsart „Arbitrage" erscheint überall als **„Preissteuerung / Arbitrage"** statt als Persistenzwert „Arbitrage" — im Variantenvergleich, in der Auslegungsoptimierung und auf der Ergebnisseite | W11a‑O‑4, erledigt beim Merge mit W10b: Deren Fassung kannte die Preissteuerung und ist die maßgebliche geworden |
| **A‑13** | Ein UNBEKANNTER Wert der Betriebs-/Berechnungsart erscheint auf den Kacheln der Simulationskonfiguration als Persistenzwert statt als „Grünstrom"/„Dauernutzung" | dieselbe Zusammenführung; unerreichbar, solange alle Schreiber `DbWerte.SP_*` setzen |

### Namensabweichungen zur Arbeitsanweisung

| Anweisung | Umgesetzt | Grund |
|---|---|---|
| `WErzeugerCtrl.Bezeichner(id)` | `AnlagenBezeichner(id)` | `WErzeugerCtrl` erbt von `WErzeugerModel`; `Bezeichner` ist dort ein **Feld** (CS0019 an jeder Lesestelle) |
| `SpeicherKennzahlen.Zeilen(…)` | `SpeicherKennzahlenBlock.Zeilen(…)` | `SpeicherEngine.SpeicherKennzahlen` ist der Kennzahlensatz der Engine (CS0723/CS0029) |
| `WarnStufe` | `KennzahlStufe` | `EPOS.UI.Bausteine.WarnStufe` gibt es seit iU9‑W2 mit anderen Ausprägungen (CS0104) |
| `Stapelgruppe` (Aufzählungstyp) | `Stapelart` | So heißt das **Feld** der `Reihe`; der Typ braucht einen eigenen Namen |
| `HeizkesselStammCtrl.BrennstoffartenJeProjekt → List<string>` | `→ HashSet<int>` | Der Vorläufer führt Brennstoff**nummern**, keine Namen |
| `StromspeicherStammCtrl.KapazitaetUndLeistung(idProjekt)` — „**eine** Fassung" | `(idProjekt, idAnlageAktiveVariante = 0)` — **beide** | Die „zwei Fassungen" sind Abfrage und Rückfall, nicht zwei Meinungen (§ 6) |

---

## 6. Was geprüft und anders befunden wurde als vermessen

**Die zwei Fassungen von `SpGeraetedaten` sind keine zwei Meinungen.** Die erste
engt auf die Anlagenzeile der **aktiven Variante** ein, die zweite summiert über
alle Speicheranlagen — und die zweite läuft nur, wenn die erste nichts findet.
Genau diese Reihenfolge nimmt auch `StromspeicherSimCtrl.LeseParameter(int)`
(Fachkonzept 7.3). Die Einengung ist die richtige: Seit AP9b rechnet die
Simulation die Anlagenzeile der aktiven Variante, nicht deren Summe; ohne sie
zeigte die Parameterseite bei mehreren Varianten eine Leistung, mit der nie jemand
gerechnet hat (Abnahmebefund 1, Projekt 1011: 43,9 kW statt 11,04 kW). **Beide
wandern deshalb in den Kern, keine wird gestrichen.**

**`ProjektPuffer.NutzbareKapazitaetKWh` passt NICHT** für die Pufferzeile ohne
Speicherliste. Die Kernformel aus iU9‑W10a lautet `Volumen · 1,16 · ΔT / 1000`
und braucht eine Spreizung; der Ausdruck der Maske (`Volumen · 1,16`, :2446–2448)
hat weder ΔT noch die Division — er ist keine Kapazität in kWh, sondern eine
Altzeile. Er steht deshalb **wörtlich** als
`SimulationErgebnisCtrl.PufferVolumenKwh`, mit einem Testfall, der beide
auseinanderhält.

**`EmissionsVorgaben` hatte kein Gegenstück für die CO₂-Faktoren.** Die dort
vorhandenen Zahlen sind CO₂-Frachten je **Brennstoffverbrauch** in g/MWh
(290 880 / 201 600 / 238 680) — eine andere Größe. Ein Substitutionsfaktor für
verdrängten Netzstrom bzw. verdrängte Wärme gab es im Kern nicht. Übernommen sind
**wörtlich** die Dashboard-Werte → **W11a‑O‑2**.

**Die drei Anzeigetexte waren nicht „wortgleich".** Drei Feststellungen:

1. **Zwei Ressourcenpaare** für denselben Text: `OPT_AMORT_NIE`/`OPT_AMORT_UEBER`
   (Optimierung, Variantenvergleich) und
   `SP_ERG_NICHT_AMORTISIERBAR`/`SP_ERG_UEBER_NUTZUNGSDAUER` (Ergebnisseite).
   Beide tragen in **beiden** Sprachen denselben Wortlaut — nachgeprüft und als
   Testfall festgehalten. Genommen ist `SP_ERG_*`.
2. **Zwei Formatangaben:** `"N1"` gegen `"0.0"` (A‑10).
3. **`BerechnungsartText` kennt `SP_BERECHNUNG_ARBITRAGE` nicht** und zeigt dort
   den Persistenzwert. Wörtlich übernommen, als Testfall festgehalten → Teil von
   **W11a‑O‑4**.

**`KonfigurationCtrl` erbt von `KonfigurationModel`, `ReadSingle` füllt aber nur
das Feld `model`.** `Energiebedarf` (`ctrl.m_Netzverluste`) und
`SimulationRunner` (:137) lesen die **geerbten** Felder — die Netzverluste sind
in beiden Wegen faktisch 0, unabhängig davon, was in `Tab_Einstellungen` steht.
Wörtlich übernommen (Bedingung des Referenzlaufs); `SimulationLaufCtrl.Vorpruefen`
bekommt deshalb `ctrl` selbst und nicht `ctrl.model` → **W11a‑O‑5**.

---

## 7. Offene Punkte

### W11a‑O‑1 — Restwärme mit oder ohne BHKW — **ENTSCHIEDEN am 04.09.2026**

Der Ergebnisblock zeigt seit W11a.3 `Projektwärmebedarf − Summe der
Erzeugerproduktion` **mit** dem BHKW-Term (§ 2). Für 1017 trifft das den Lauf
exakt (0,00 MWh), für 1030 ergibt es **−1,76 MWh**, weil „Produktion" nicht
„Deckung" ist.

**Zu entscheiden:** Soll der Wert auf `>= 0` geklemmt werden — wie es jede andere
Restgröße dieser Maske tut (`Rundungsschutz`)? Dann stünde für 1030 ebenfalls
0,00 und damit dieselbe Zahl wie in `sim.Restwaerme` eine Zeile darüber. Die
Klemmung wäre eine **dritte** Fassung und ist deshalb nicht eingebaut worden.

Zusatzfrage: Braucht die Übersicht überhaupt zwei Restwärmezahlen — die
Bilanzgröße `sim.Restwaerme` (Feld „Restwärmebedarf") **und** die Anzeigegröße
des Ergebnisblocks? W11b könnte auf die erste allein zurückfallen.

**Entscheid (Anwender, 04.09.2026):** Der Restwärmebedarf ist in **beiden** Ansichten derselbe
Wert, das BHKW wird berücksichtigt. Eine negative Restwärme darf **rechnerisch nicht entstehen** —
sie zeigt eine falsche Zuordnung zu den Erzeugern. Umsetzung in W11b: eine Restwärmezahl
(`sim.Restwaerme`, die Bilanzgröße des Laufs), „Wärme gesamt“ als Summe der **Deckung** je
Erzeuger statt der Produktion; Überschuss ist Überschuss, nicht negative Restwärme.

> **Entscheid des Anwenders vom 04.09.2026, umgesetzt in iU9‑W11b:**
> (1) Der Restwärmebedarf ist in **beiden** Ansichten derselbe Wert, und das BHKW
> zählt mit. (2) Eine **negative Restwärme darf rechnerisch nicht entstehen** — sie
> zeigt eine falsche Zuordnung zu den Erzeugern. Also nicht klemmen, sondern richtig
> rechnen: Die Übersicht führt **eine** Restwärmezahl `= sim.Restwaerme`, und
> „Wärme gesamt" ist die Summe der **DECKUNG** je Erzeuger (Direktdeckung +
> Speicherentladung je Kanal, wie `NavigatorUebersicht.FillTableWithData` sie über
> `SimulationRunner.Summiere` bildete) — nicht der Produktion. Damit gilt
> `Bedarf − Summe Deckung = Restwärme ≥ 0` per Konstruktion. Übersteigt die
> Produktion eines Erzeugers seine Deckung, ist das ein **Überschuss** (Feld
> `Wärmeüberschuss`, wie beim BHKW) und keine Restwärme.
>
> Gemessen nach der Umstellung (`EPOS.Kern.Tests/W11bZahlenabzug.cs`):
>
> | Projekt | Wärmebedarf | Summe Deckung | Restwärme |
> |---|---:|---:|---:|
> | 1030 | 6 137,56 | 6 137,56 | **0,00** (vorher −1,76) |
> | 1007 | 56,90 | 50,85 | **6,04** (unverändert) |
> | 1017 | 62,91 | 62,91 | **0,00** (unverändert) |
>
> Die Abweichung **A‑1** dieses Protokolls ist damit überholt: Nicht mehr die
> Produktion mit BHKW-Term, sondern die Deckung. **A‑2** (PV-Deckungsgrad) bleibt.
> Der Referenzlauf ist unberührt — `Tab_Ergebnis.Waermerestbedarf` schreibt
> unverändert `sim.Restwaerme`.

### W11a‑O‑2 — die zwei CO₂-Faktoren (Fachprüfung)

`CO2_NETZSTROM_KG_JE_KWH = 0,42` und `CO2_WAERME_KG_JE_KWH = 0,20` stehen jetzt
in `EmissionsVorgaben` — **wörtlich** aus `DashboardForm.cs:355`. Sie sind nie
begründet worden und stehen neben BEHG-Faktoren, die in einer anderen Einheit
geführt werden (g/MWh je Brennstoffverbrauch). **Zu prüfen:** Gilt 0,42 kg/kWh
noch für den deutschen Strommix, und ist 0,20 kg/kWh die richtige verdrängte
Wärme (Erdgas Hu ≈ 0,20; Heizöl ≈ 0,27)? Beide sind jetzt an einer Stelle
änderbar.

**Entscheid (Anwender, 04.09.2026):** Die CO₂-Faktoren sollen aus einem **Katalog je
Energieträger** wählbar, erweiterbar und änderbar sein; die heutigen Zahlenwerte sind nicht
relevant. Eigener Folgeschritt (Emissionskatalog aus W3 als Quelle prüfen), nach W11b.

### W11a‑O‑3 — Zusammenführung der Berichtsbilder

`JahresverlaufWaerme`/`DauerlinieWaerme` sind zwei feste Ausprägungen von **B2**,
`StrombilanzMonate` und `MonatsSaeulen` zwei von **B6**, `Speichertemperaturen`
eine von **B7**. Sie nehmen einen `ZeitreihenSatz` und tragen feste deutsche Titel
im Quelltext; die neuen nehmen freie Reihenlisten. Eine Zusammenführung spart rund
400 Zeilen im Renderer, ändert aber die **Berichtsbilder** — und die prüft
ChartProben byte-genau. Das gehört in einen eigenen Schritt mit eigenem Nachweis,
nicht in eine Welle mit Referenzlauf-Gate.

### W11a‑O‑4 — eine VIERTE Fassung der Anzeigetexte — **beim Merge erledigt**

`Form_Simulation_Config.Karten.cs:1076–1091` führte `BetriebsartAnzeige` und
`BerechnungsartAnzeige` — eine vierte Kopie, die die Vermessung nicht nennt. Mit
W10b ist die Datei gelöscht und die beiden Methoden nach
`Views/Simulation/SimulationKonfigHuelle.cs:1063–1077` gewandert.

**Erledigt beim Zusammenführen** (§ 11): Die vierte Fassung war zugleich die
**vollständigste** — nur sie kannte `SP_BERECHNUNG_ARBITRAGE`. Ihr Wissen steht
jetzt in `SpeicherAnzeigeCtrl.BerechnungsartText`, und die Hülle ruft den Kern.
Vier Kopien, eine Wahrheit.

**Ein Unterschied bleibt bewusst:** Ein UNBEKANNTER Wert kam in der vierten
Fassung als „Grünstrom" bzw. „Dauernutzung" zurück; der Kern gibt ihn unverändert
weiter. Das ist eine Behauptung weniger über Daten, die man nicht kennt — und
unerreichbar, solange alle Schreiber `DbWerte.SP_*` setzen. Zwei Testfälle halten
beides fest.

### W11a‑O‑5 — `KonfigurationCtrl` liest zwei Modelle

`ReadSingle`/`ProjektLesen` füllen `model`; `ctrl.m_Netzverluste` und
`ctrl.m_szNetzverlusteEinheit` sind die **geerbten** Felder und bleiben auf ihrer
Vorbelegung. Beide Leser der Netzverluste (`Energiebedarf`, `SimulationRunner`)
greifen auf die geerbten zu und rechnen deshalb immer mit 0 %. Das ist der
Referenzstand und wurde nicht angetastet. **Zu entscheiden:** Ist das die
gewollte Rechnung (dann sollte die Einstellung aus der Oberfläche verschwinden),
oder ein Fehler (dann ist es eine **Ergebnisänderung** und braucht eine eigene
Etappe mit neuem Referenzstand)?

---

## 8. Nachweis

> **Die Zahlen unten sind der Stand VOR dem Merge mit Welle 10b.** Der Nachweis auf
> dem zusammengeführten Stand steht in § 11.

| Prüfung | Sollwert | Ist |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, ≤ 17 Warnungen | **0 / 17** |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 2 284 + neue | **2 406** (KiKern 450 · SpeicherEngine 337 · EPOS.Kern 337 · EPOS.UI 1 282) |
| dieselben Tests unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` | grün | **grün** (Kultur nachgewiesen: `Culture=en-US`) |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | 123 | **123** |
| Formularkarte-Stapellauf | 50 Masken, 29 lokalisiert | **50 / 29** — unverändert, keine Maske gelöscht |
| `Werkzeuge/SqlDialektPruefer` | 0 Fundstellen | **0** (1 233 SQL-Texte) |
| `Proben/ChartProben` | 30 Bilder | **30, 0 Verstöße** |
| **Referenzlauf 1030/1007/1017** | **byte-gleich** | **byte-gleich** gegen `Referenzlaeufe/2026-08-30_B3-Kaskade` |
| `git grep` auf die elf inline-SQL-Zeichenketten der Welle | 0 | **0** in den sechs Masken (die übrigen `ReadAllFilter`-Stellen gehören anderen Wellen) |
| `git grep` auf die drei Textdreifachungen | 0 | **0** — die drei Methoden stehen nur noch in `SpeicherAnzeigeCtrl` (die vierte Fassung siehe W11a‑O‑4) |

### Neue Kern- und bunit-Tests

| Datei | Fälle | Was |
|---|---|---|
| `GanglinieTests.cs` | 7 | Dauerlinie monoton fallend über 8 760 und 35 040 Werte, Summenerhalt, Quellvektor unberührt |
| `ErgebnisPraesenzTests.cs` | 5 | Präsenz gegen einen Lauf von 1030 und gegen den Anlagenbestand ohne Lauf |
| `SimulationErgebnisSqlTests.cs` | 17 | je Controller-Methode eine Probe gegen die Testdatenbank |
| `SimulationErgebnisCtrlTests.cs` | 23 | die acht DTO, die drei behobenen Befunde, die geteilten Runner-Methoden |
| `SpeicherKennzahlenBlockTests.cs` | 16 | 39 Zeilen, drei Gruppen, Vergleichsspalte, unbestimmte Eigenverbrauchsquote, beide Warnstaffelungen |
| `SpeicherAnzeigeCtrlTests.cs` | 9 | die drei Anzeigetexte, die zwei Ressourcenpaare, die CO₂-Faktoren |
| `SimulationLaufCtrlTests.cs` | 13 | Vorprüfungen, Abbruchgrund, Phasenfolge, Abbruch, Lauf im fremden Faden |
| `ErgebnisbilderTests.cs` | 19 | die sieben Bilder: Maße, Determinismus, Leerfall, Eigenheiten |
| `EPOS.UI.Tests/Bausteine/FortschrittTests.cs` | 13 | bestimmt/unbestimmt, Klemmung, Knopf nur mit Rückruf, Klick, zweiter Klick |

### Nebenbefund: die Testdatenbank-Kopien

`TestDatenbank` legt die **77-MB-Arbeitskopie je TESTFALL** an. Mit den
31 neuen Datenbankfällen dieser Welle wären das rund **2,4 GB** Datei-Ein- und
-Ausgabe je Lauf; auf dem Baurechner mit knapp 8 GB freiem Platz fielen daraufhin
sporadisch **fremde** Fälle um (`BedarfProfilTests`). Die vier neuen
Datenbank-Testklassen **lesen nur** und teilen sich seither **eine Kopie je
Klasse** (`IClassFixture<TestDatenbank>`); `TestDatenbank` ist dafür öffentlich.
Danach sechs Läufe hintereinander grün. **Die schreibenden Bestandsklassen
bleiben unverändert** — sie brauchen je Fall einen unberührten Stand.

---

## 9. Abnahme am Windows-Gerät

Ungeprüft — alles hier ist ohne Windows entstanden. Die Liste:

1. **Detailansicht öffnen** (Startbild → Kachel „Detaillierte Simulation"): Der
   Lauf startet von selbst, der **Fortschrittsbalken** ist sichtbar, das Fenster
   bleibt bedienbar (verschieben, Größe ändern, Reiter wechseln), die Titelzeile
   meldet **kein** „Keine Rückmeldung".
2. **Abbrechen** während des Laufs: Der Knopf wirkt (spätestens an der nächsten
   Phasengrenze), „Ergebnis speichern" bleibt gesperrt, die Maske ist danach
   wieder bedienbar, ein zweiter Lauf geht durch.
3. **Nach dem Automatikstart steht die „Übersicht" im Vordergrund** (nicht
   „Simulation") — die Endlage wie vor W11a.
4. **Alle elf Reiter** zeigen dieselben Zahlen wie vor W11a, bis auf:
   - Ergebnisblock „Wärme gesamt" und „Restwärmebedarf" bei Projekten **mit
     BHKW** (W11a‑O‑1; für 1030 −1,76 statt 734,46),
   - PV-Deckungsgrad steht auf `0,00` statt `NaN`, wo das Projekt keinen
     Strombedarf führt,
   - Mindest-Spitzenkesselleistung (nur wenn das Jahresmaximum in den letzten
     zehn Stunden liegt).
5. **Stromspeicher-Reiter:** Der Kennzahlenblock zeigt **39** Zeilen in drei
   Gruppen, die Zyklenzeile ist gefärbt wie zuvor (grün ≤ 90 %, gelb darüber, rot
   bei Überschreitung, grau ohne gepflegte N_zyk), die Budgetzeilen erscheinen
   nur mit Preissteuerung.
6. **Variantenvergleich und Auslegungsoptimierung**: Betriebsart,
   Berechnungsart und Amortisationstext lesen sich wie zuvor (beide Sprachen).
7. **Autarkie-Kachel:** dieselbe CO₂-Zahl wie vor W11a; die Speicherkapazität
   fällt ohne Stromspeicher auf 5 kWh zurück.
8. **Deutsch und Englisch** (`HKCU\Software\wp-plan\Language`).

---

## 10. Was W11b vorfindet

- Sieben DTO und der Kennzahlenblock liefern **alle** Zahlen der elf Reiter; die
  Ergebnisseite muss nichts mehr rechnen.
- Sieben Renderer-Bilder decken **alle 17** Zeichenflächen der Welle.
- Der Lauf ist über `SimulationLaufCtrl` anstoßbar, meldet Fortschritt und lässt
  sich abbrechen; der Baustein `Fortschritt` steht.
- `ErgebnisPraesenz` und `Ganglinie` liegen im Kern und sind `public`.
- **Vor W11b:** je Chart ein Bildschirmfoto des Bestands ins Scratchpad (Risiko
  R‑W11a‑4 — die sieben neuen Bilder haben keinen Golden-Vergleich).

---

## 11. Nachtrag: der Merge mit Welle 10b (04.09.2026)

W10b lief gleichzeitig in einem eigenen Arbeitsbaum und ist mit `a91ba2a` auf
`ios_migration` gelandet (Statusblock `a398c9a`). Beide Wellen fassen dieselbe
Ecke des Programms an: W10b macht aus der **Simulationskonfiguration** eine Seite,
W11a bereitet die **Ergebnisseite** vor. Elf Dateien haben beide berührt, sieben
davon in Konflikt.

**Merge-Commit:** `origin/ios_migration` (`a398c9a`) in
`worktree-agent-afd9c90f84878dc00`.

### Die sieben Konflikte und wie sie gelöst sind

| Datei | Konflikt | Lösung |
|---|---|---|
| `EPOS.Kern/Controller/KonfigurationCtrl.cs` | **beide Wellen haben `LiesProjekt(int)` gebaut** — W10b über eine neue `ReadSingle`-Überladung mit `DbParam`, W11a über `TabelleJeProjekt` + `ZeileUebernehmen` | **eine Fassung**, Signatur `KonfigurationModel LiesProjekt(int idProjekt)`. Der Rumpf ist der von W11a (er füllt ein FRISCHES Modell und braucht dafür ein parametrisierbares Ziel); W10b's Überladung `ReadSingle(string, params DbParam[])` bleibt, weil sie eigene Aufrufer hat. Dazu unverändert `ProjektLesen(int)` für Aufrufer, die ein STEUEROBJEKT füllen (`SimulationControl.ctrl_konfig` hält es während des Laufs). W10b's `ReadZeile(DataTable)` ruft jetzt W11a's `ZeileUebernehmen` — die Ordinalkette steht einmal |
| `WindowsFormsApplication1/Views/Simulation/Form_Simulation_Detail.cs` | nur `btn_Konfiguration_Click`: W10b ersetzt den Block durch `SimulationKonfigHuelle.Oeffnen`, W11a hatte darin die Lesezeile parametrisiert | **W10b gewinnt in diesem Block** — die Lesestelle steckt jetzt in der Hülle und ruft dieselbe Kern-Methode. **Alle übrigen W11a-Änderungen der Datei sind unberührt** (sie lagen außerhalb des Konflikts) |
| `WindowsFormsApplication1/Views/Hauptformular/Form_Start.cs` | `btn_SimKonfig_Click`: derselbe Fall | **W10b gewinnt** — mit dem ganzen Block entfällt auch die Stelle, die W11a.2 parametrisiert hatte |
| `EPOS.UI/wwwroot/epos-ui.css` | beide haben am Dateiende angehängt | **beide Blöcke** (`epos-fortschritt` aus W11a.7, `epos-schema`/`epos-simkonfig` aus W10b) |
| `EPOS.UI/CLAUDE.md` | beide haben Bausteinzeilen angehängt | **alle vier Zeilen** (`Fortschritt`, `Schema`, `ErzeugerKachel`, `SpeicherKachel`) |
| `EPOS.Kern/CLAUDE.md` | dieselben zwei Tabellenzeilen (`Allgemein/Simulation/`, `Controller/`) von beiden erweitert | **beide Texte in je einer Zeile**, Zahlen nachgezählt: `Allgemein/` 21 · `Simulation/` 33 · `Controller/` 92 · `Model/` 51 |
| `WindowsFormsApplication1/CLAUDE.md` | Wellenabsatz und Protokollliste | **beide** — der W11a-Absatz bleibt, die `Allgemein/`-Zeile nimmt W10b's Zahl (42), die Protokollliste beide neuen Protokolle |

`ChartRenderer.cs` und `Proben/ChartProben/Program.cs` standen **nicht** in
Konflikt — W10b hat sie nicht angefasst.

### Was der Merge zusätzlich erledigt hat

**W11a‑O‑4 ist geschlossen** (oben). Die vierte Fassung der Anzeigetexte lebte
nach W10b in `SimulationKonfigHuelle`; ihr Wissen um die Preissteuerung ist nach
`SpeicherAnzeigeCtrl.BerechnungsartText` gezogen, die Hülle ruft den Kern.
Abweichungen **A‑12** und **A‑13**.

**W10b bestätigt den Nebenbefund zu den Testdatenbank-Kopien** unabhängig:
`d75908c` („Befund W10b‑B42 — `DatenzugriffTests` gehört in die
Testdatenbank-Sammlung") beschreibt dieselbe Ursache aus einer anderen Richtung.

### Gate auf dem zusammengeführten Stand

| Prüfung | Ist |
|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | **0 Fehler, 12 Warnungen** (17 vor dem Merge — W10b hat Masken mit WFO1000 gelöscht) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | **2 502** grün (KiKern 450 · SpeicherEngine 337 · EPOS.Kern 375 · EPOS.UI 1 340) |
| dieselben Tests unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` | **grün** |
| `dotnet test Werkzeuge/Formularkarte.Tests` | **123**, auch unter `en_US` |
| Formularkarte-Stapellauf | **49 Masken / 28 lokalisiert** — der W10b-Stand, W11a löscht keine |
| `Werkzeuge/SqlDialektPruefer` | **0 Fundstellen** (1 233 SQL-Texte) |
| `Proben/ChartProben` | **30 Bilder, 0 Verstöße** |
| **Referenzlauf 1030/1007/1017** | **byte-gleich** gegen `Referenzlaeufe/2026-08-30_B3-Kaskade` (PASS: 324 219 · 254 154 · 236 670 Werte) |
| iU5-Wächter (`Program.*`) | leer |
| Plattform-Wächter (WinForms/Drawing/OleDb im Kern) | leer |
