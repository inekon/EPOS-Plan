# Referenzlauf-Suite (Paket B1)

Regressionsbasis für den Simulationskern von EPOS-Plan.

Vor jedem Umbau an der Engine wird der aktuelle Stand als CSV eingefroren; nach dem Umbau
läuft derselbe Satz Projekte erneut und wird mit Toleranz gegen den eingefrorenen Stand
verglichen. Was sich dabei ändert, ist entweder gewollt — dann wird die Referenz neu
gesetzt — oder ein Fehler.

Grundlage: `WindowsFormsApplication1/Allgemein/Simulation/Konzept_Simulation_QuellenSenken.md`,
Paket B1, Kapitel 9.

## Aktuelle Basis

**`2026-08-16_B4/`** — seit dem 16.08.2026, 03:43 Uhr die gültige Referenz,
**acht Projekte** (1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024), **190 CSV,
2 094 451 Werte**. Jeder neue Vergleich läuft gegen diesen Ordner.

> **Die Basis ist mit Feature-Flag `Kaskade_Zweikanalig` = AUS gerechnet** und bildet damit
> weiter den einkanaligen Altpfad ab. Das bleibt so, bis die Bestandsprojekte projektweise
> auf die zweikanalige Kaskade umgestellt werden. Ein Lauf mit gesetztem Flag ist **kein**
> Regressionsfall gegen diese Basis — er wird gegen den Flag-aus-Lauf desselben Codes
> verglichen (Umsetzungsprotokoll Paket 4).

### Warum die Basis auf B4 gewechselt wurde

**Ein Anlass: die neue Ergebnisspalte aus Etappe D4.** Vollständige Zuordnung je Projekt im
[Laufprotokoll der Basis](2026-08-16_B4/lauf_protokoll.md).

Etappe **D4** hat `Tab_ErgebnisHeizkessel.Quellwaerme` eingeführt — **Migrationsschritt 10**,
rein additives DDL, Schema-Zielstand **9 → 10**. Weil der Export `SELECT * FROM Tab_Ergebnis*`
liest, führt `aggregate.csv` je Projekt mit Heizkessel-Ergebniszeile einen Schlüssel mehr.
Gegen B3 meldete der Vergleich das als „Eintrag nur im Vergleichslauf" — fachlich richtig,
aber dauerhaft erklärungsbedürftig. **B4 friert den D4-Stand einschließlich der neuen Spalte
ein; künftige Vergleiche laufen wieder ohne `--ohne`.**

**Codestand:** `3fd2787`, unverändert, gebaut aus einem `git archive`-Export außerhalb des
Repos (0 Fehler, 6 Bestandswarnungen). **Datenquelle:** produktive `Kenndaten.accdb`,
Zeitstempel **15.08.2026 22:50** (Datei 23:22), Schemastand **9**, nur gelesen (keine
`Kenndaten.laccdb`). Schritt 10 lief ausschließlich auf der Arbeitskopie — die produktive
Datei steht nachweislich weiter auf Schemastand 9.

**Zuordnung B3 → B4, Projekt für Projekt:**

| Projekt | Abweichung zu B3 | Ursache |
|---|---|---|
| 1007, 1008, 1011, 1021 | **keine — byte-/MD5-gleich** | kein Heizkessel-Ergebnisdatensatz |
| 1017, 1018, 1023, 1024 | **je ein neuer Schlüssel** in `aggregate.csv` (`Heizkessel.Quellwaerme;0`) | Migrationsschritt 10 / Etappe D4 |

Byte-Vergleich: **186 von 190 gleich**, die vier Abweichungen sind ausschließlich die
`aggregate.csv` der vier Heizkessel-Projekte, Zeilendiff je genau eine eingefügte Zeile. Alle
Ganglinien sind in allen acht Projekten byte-gleich.

```
vergleich 2026-08-15_B3 2026-08-16_B4 --ohne Heizkessel.Quellwaerme
  → 8/8 PASS (2 094 451 Werte)
```

**Kein Altwert weicht ab** — D4 hat keinen Rechenweg verändert.

> **Auffällig:** `Heizkessel.Quellwaerme` steht in allen vier Projekten auf **0** — kein Kessel
> der Referenzmenge hängt an einem Quellpuffer. Die Spalte ist damit im Vergleich enthalten,
> aber noch nicht mit einem Wert ungleich null abgedeckt (wie `Erdreich[i].*` seit Paket 7).
> Für einen belastbaren Regressionstest dieses Pfades fehlt ein Referenzprojekt mit Kessel an
> einem Quellpuffer.

**Selbstvergleich der neuen Basis:** Ein zweiter Lauf desselben Codes auf derselben Quelle
ergibt **8/8 PASS (2 094 451 Werte)** und **190/190 byte-/MD5-gleich** — die Basis ist
reproduzierbar.

## Frühere Stände

`2026-08-15_B3/` bleibt als **vorheriger Stand** liegen (Codestand `a0a623a` + K-3,
Schemastand 9, acht Projekte, 190 CSV) — für alle Werte außer der neuen Spalte
byte-gleich mit B4. Warum B3 seinerzeit gesetzt wurde:

**Zwei Anlässe — beide getrennt nachgewiesen.** Vollständige Zuordnung je Projekt im
[Laufprotokoll der Basis](2026-08-15_B3/lauf_protokoll.md).

**(1) Ergebnisänderung K-3.** Die Bivalenz-Umschaltung des bivalent-alternativen
Wärmepumpenbetriebs schaltet ab jetzt an der **Bivalenztemperatur**
(`Tab_Energieanlagen.Abschaltpunkt`) statt stundenweise nach Leistungsunterdeckung — in
beiden Rechenwegen. Umsetzung, Datenbefund, Regelentscheidung und alle Zahlen:
[`../WindowsFormsApplication1/Allgemein/Simulation/K3_BivalenzTemperatur_Protokoll.md`](../WindowsFormsApplication1/Allgemein/Simulation/K3_BivalenzTemperatur_Protokoll.md).

**Davon betroffene Referenzprojekte: keines.** Der Datenbefund vor dem Lauf zeigt, dass im
gesamten Bestand **keine einzige** Anlage `Bivalenter_Betrieb = TRUE` **und**
`Betriebsart = "Alternativbetrieb"` führt — der geänderte Zweig ist in keinem gespeicherten
Projekt aktiv. (Die eine `Alternativbetrieb`-Zeile, Anlage 10132 in Projekt 1008, trägt
`Bivalenter_Betrieb = False`; die Bedingung ist eine Und-Verknüpfung.) Dementsprechend:

```
A/B gegen a0a623a, Flag AUS : 9/9 PASS (2 295 987 Werte), 208/208 byte-/MD5-gleich
A/B gegen a0a623a, Flag AN  : 9/9 PASS (2 295 998 Werte), 208/208 byte-/MD5-gleich
```

Der A/B-Lauf umfasst noch **neun** Projekte: Er lief auf einer gemeinsamen Datenbankkopie
vom 22:26 Uhr — also vor der Löschung unten — und deckt Projekt 1010 damit mit ab.

Wirksam ist K-3 sehr wohl — nachgewiesen an eigens präparierten Kopien der Projekte **1026**
(WP + Kessel + Puffer, auf `Alternativbetrieb` gestellt: WP-Produktion 28,3 → 40,2 MWh,
Kessel 36,4 → 24,6 MWh, WP-Ein/Aus-Wechsel einkanalig 2 962 → 2 524 und zweikanalig
**1 126 → 140**, Frostbetrieb der WP 330 h → 0 h) und **1024** (Sommer-Warmwassermuster:
**714 Sommerstunden**, in denen die WP bisher an Warmwasserspitzen ausfiel, laufen wieder mit
der Wärmepumpe). Stundengenaue Bilanzproben schließen in allen Varianten (max. Abweichung
7·10⁻⁶ kWh, 0 Stunden über 0,01 kWh).

**(2) Projektlöschung durch den Anwender.** Am 15.08.2026 gegen 22:50 Uhr hat der Anwender
die Projekte **1010, 1016, 1020 und 1025** aus der produktiven Datenbank gelöscht. Von der
Referenzmenge trifft das **1010 „Kurs EE"** — es existiert nicht mehr. **B3 umfasst deshalb
acht Projekte, B2 hatte neun.**

> **Folgebedarf:** 1010 war in der Referenzmenge die Kategorie **„Wärmepumpe ohne weitere
> Erzeuger"** (`Anlagen: WP`). Fällt sie dauerhaft weg, sollte ein Ersatzprojekt derselben
> Kategorie nachrücken (`Projektauswahl.MAX_PROJEKTE` steht auf 9).

**Zuordnung B2 → B3, Projekt für Projekt:**

| Projekt | Abweichung zu B2 | Ursache |
|---|---|---|
| 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024 | **keine — alle 190 Dateien byte-/MD5-gleich** | — |
| 1010 | **Ordner entfällt** (18 Dateien) | **Projektlöschung**, kein Codeeffekt |

Für die acht verbliebenen Projekte ist B3 also wertgleich mit B2 bis auf das Byte; **kein
einziger Wert weicht durch K-3 ab**. Der Basiswechsel erfolgt damit aus zwei Gründen, von
denen keiner „geänderte Zahlen" heißt: die geschrumpfte Projektmenge und die **Zuordnung** —
ab hier ist die gültige Basis mit dem K-3-Code gerechnet, und eine spätere Abweichung lässt
sich zweifelsfrei einer Folgeänderung zuschreiben statt K-3.

**Selbstvergleich der neuen Basis:** Ein zweiter Lauf desselben Codes auf derselben Quelle
ergibt **8/8 PASS (2 094 447 Werte)** und **190/190 byte-/MD5-gleich** — die Basis ist
reproduzierbar.

**Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **15.08.2026 22:50**, nur gelesen
(keine `Kenndaten.laccdb`).

`2026-08-15_B2/` bleibt als **vorvorheriger Stand** liegen (Codestand `925c37f`, Datenstand
15.08.2026 11:58, **neun** Projekte) — für die acht gemeinsamen Projekte byte-gleich mit B3
und die einzige verbliebene Quelle für die Ganglinien des gelöschten Projekts 1010. Warum B2
seinerzeit gesetzt wurde:

Gerechnet auf Codestand **`925c37f`** (Paket 9, Etappe 2) und auf der produktiven
`Kenndaten.accdb` mit Zeitstempel **15.08.2026 11:58**. Ein Codeeffekt liegt dem Wechsel
**nicht** zugrunde — die Ursache sind **geänderte Projektdaten**:

Der Anwender hat am 15.08.2026 um 11:58 in **Projekt 1024** das **zweite Wärmepumpenmodul**
(`CS7800iLW 12`) entfernt. Damit fehlt im `aggregate.csv` der komplette Block
`WaermepumpeModul[1]`, und die davon abhängigen Ganglinien (BHKW, Kessel, WP, Heizstab,
Restwärme, Reststrom) verschieben sich. Der Vergleich der alten gegen die neue Basis zeigt
das sauber abgegrenzt:

```
2026-08-14_B1-Fixes vs 2026-08-15_B2 : 193 byte-/MD5-gleich, 15 abweichend
                                       (alle 15 in Projekt_1024)
Toleranzvergleich                    : 8 x PASS, Projekt_1024 FAIL (75.575 Abweichungen)
```

Der Nachweis, dass das **nicht** vom Code kommt, steht in
`../WindowsFormsApplication1/Allgemein/Simulation/Paket9_Lokalisierung_Protokoll.md`,
Abschnitt 12.2: Ein Baselinelauf aus einem eigenen git-Arbeitsbaum auf `d49075e` — also
**ohne** die Änderungen der Etappe 2 — zeigt gegen `B1-Fixes` **dieselben 15 Dateien**;
gegen den Etappe-2-Lauf auf demselben Datenstand sind alle 208 Dateien byte-gleich.

Solange `B1-Fixes` die Basis bliebe, schleppte jede Folgeprüfung diese eine
erklärungsbedürftige Abweichung mit und Projekt 1024 wäre dauerhaft FAIL — der Regressionstest
verlöre für dieses Projekt seine Aussagekraft. Deshalb der Basiswechsel.

**Selbstvergleich der neuen Basis:** Ein zweiter Lauf desselben Codes auf derselben Quelle
ergibt **9 von 9 PASS (2.295.987 Werte)** und **208 von 208 Dateien byte-/MD5-gleich** — die
Basis ist reproduzierbar.

Die Anwendung des Anwenders lief während des Laufs, hatte die Datenbank aber **nicht**
geöffnet (keine `Kenndaten.laccdb`). Die produktive Datei wurde ausschließlich gelesen.

`2026-08-14_B1-Fixes/` bleibt als **älterer Stand** liegen (Datenstand vom 14.08.2026,
neun Projekte). Gegenüber `2026-08-14_Paket4` weichen dort **drei Projekte** ab, vollständig
zugeordnet in
`2026-08-14_B1-Fixes/vergleich_protokoll.md`: **1008** und **1011** durch die
Bestandsfehler-Fixes **B1-F1/B1-F2** (Stromganglinien fließen erstmals in den Strombedarf
ein; Prozesswärme war still 0 — B0-Protokoll, Nachtrag B1-F1/B1-F2), **1024** durch
**geänderte Projektdaten** (Heizkessel nach dem Paket4-Snapshot in die Kaskade
aufgenommen; Alt- vs. Neu-Code auf identischer DB ist für 1024 vollständig PASS —
kein Code-Effekt). Die übrigen sechs Projekte: PASS.

`2026-08-14_Paket4/` bleibt als **älterer Stand** liegen. Gegenüber
`2026-08-14_Paket7` waren dort genau **drei** Werte neu, alle in Projekt 1021 und alle
begründet in `2026-08-14_Paket4/lauf_protokoll.md`: die ID-Semantik des Quellspeichers
(`Pufferspeicher[0].ID_Pufferspeicher` 8 → 1018014) und die beiden laufzeitbasierten
Skalare aus dem Bestandsfehler **B0-13** (`WaermepumpeModul[0].Betriebsstunden`
6692,41 → 4,41; `Waermepumpe.Vollbenutzungsstunden` 3846,66 → 502,66). Alle übrigen
2.260.920 Werte sind byte-genau gleich.

`2026-08-14_Paket7/` und `2026-08-14_B0/` bleiben als **historische Stände** liegen
(Paket7: vor Paket 1/2/4, B0-12/13 und B1-Fixes; B0: vor Paket 1/3/7, acht Projekte).
Ein Vergleich gegen B0
meldet zwangsläufig FAIL — der Basiswechsel ist gewollt und in
`2026-08-14_Paket7/vergleich_protokoll.md` sowie in
`../WindowsFormsApplication1/Allgemein/Simulation/Paket7_Ergebnis_Anzeigen_Protokoll.md`
begründet:

| Was | Alt (B0) | Neu (Paket 7) |
|---|---|---|
| Projektmenge | acht | neun — **1021** kommt hinzu und deckt als einziges den Quellspeicher-Pfad ab |
| `Waermepumpe.Kapazitaet_Pufferspeicher` | `Volumen · 1,16` aus dem WP-Datensatz (in allen Projekten 11,6) | `SimulationPufferspeicher.Q_max` des zugeordneten Puffers; 0 ohne Puffer |
| Pufferspeicher-Persistenz | gab es nicht | `Pufferspeicher[i].*` je Speicher in `aggregate.csv` (aus `Tab_ErgebnisPufferspeicher`) |
| Speicher-Kennzahlen | gab es nicht | `Puffer.SOC_Mittel`, `Puffer.SOC_Max`, `Puffer.Vollzyklen`, `Sim.Speicher_Anzahl` |
| Quellspeicher-Ganglinien | gab es nicht | `quellspeicher_<AnlagenID>_{soc,ladung,entladung}.csv` (nur in 1021) |
| Erdreich-Auslegungsprüfung | gab es nicht | `Erdreich[i].*` in `aggregate.csv` — **nur** bei Projekten mit `WQ_Typ = 'Erdreich'`, in der Referenzmenge also nirgends |

Gerechnet wurde die neue Basis auf einer **eigenen, vollständig migrierten Kopie außerhalb
des Repos** im Modus `projekt` (siehe `2026-08-14_Paket7/lauf_protokoll.md`).

## Was hier liegt

| Pfad | Inhalt |
|---|---|
| `<yyyy-MM-dd>_<Marke>/` | Ein eingefrorener Lauf: je Projekt ein Unterordner `Projekt_<ID>/`, dazu `lauf_protokoll.md` |
| `<...>/Projekt_<ID>/aggregate.csv` | Alle Skalare des Laufs: `Tab_Ergebnis*`-Zeilen, Restgrößen aus `SimulationControl`, Jahressumme jedes Vektors |
| `<...>/Projekt_<ID>/*.csv` | Die Ganglinien: 8760 Stundenwerte bzw. 35040 Viertelstundenwerte, `Index;Wert` |
| `Arbeitskopie/` | Die Kopie der Datenbank, auf der gerechnet wird. Wird bei jedem `lauf` neu angelegt. Nicht im Git (`Kenndaten.accdb` ist in `.gitignore`) |

Der Werkzeugcode liegt in `../Referenzlauf/`.

## Die wichtigste Regel

**Die produktive `Kenndaten.accdb` wird nie beschrieben.**

Die Suite kopiert sie nach `Referenzlaeufe/Arbeitskopie/`, biegt den DB-Pfad der Anwendung
per Reflection auf diesen Ordner um und prüft anschließend über
`DataRepository.GetDBPath()` nach, dass die Anwendung wirklich auf der Kopie arbeitet.
Zeigt der Pfad woanders hin — oder auf eine der bekannten produktiven Ablagen — bricht der
Lauf sofort ab. Auch jeder Kindprozess prüft das für sich noch einmal.

Liegt neben der Quelle eine `Kenndaten.laccdb`, ist die Datenbank gerade geöffnet. Kopiert
wird trotzdem (lesend), aber das Protokoll weist darauf hin: die Kopie kann dann Änderungen
der laufenden Sitzung noch nicht enthalten. Für einen belastbaren Referenzlauf die
Anwendung vorher schließen.

## Bauen

Nur über das MSBuild von Visual Studio — `dotnet build` scheitert an MSB4803
(COM-Referenzen des App-Projekts).

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj `
    -p:Configuration=Debug -p:Platform=x86
```

Beim allerersten Mal davor einmal `-t:Restore` mit denselben Parametern. Das Projekt ist
bewusst **nicht** Teil von `WP-Plan.sln`.

Ergebnis: `Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe`

## Bedienung

```powershell
$exe = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
```

### `lauf` — Stand einfrieren

```powershell
& $exe lauf                                  # Ziel: Referenzlaeufe\<heute>_B0
& $exe lauf --ziel D:\Temp\NachUmbau         # anderer Zielordner
& $exe lauf --projekte 1010,1023             # feste Projektliste statt Automatik
& $exe lauf --timeout 600                    # Zeitlimit je Projekt in Sekunden (Standard 300)
```

Kopiert die Datenbank, **migriert sie auf den Zielstand des Schemas**, wählt die Projekte,
rechnet und schreibt CSVs plus `lauf_protokoll.md`. Exit-Code 0, wenn alle Projekte
durchgelaufen sind.

Die Migration (Schritt 2b) gehört seit der Paket-7-Nacharbeit dazu. Vorher rechnete `lauf`
auf einer Kopie im Stand der Quelldatenbank: fehlende Spalten und eine fehlende
`Tab_ErgebnisPufferspeicher` wurden nur von den Rückfallebenen im Anwendungscode
notdürftig ausgeglichen, und das Ergebnis war mit einem Lauf auf einer migrierten
Datenbank nicht vergleichbar. Die Migration ist idempotent — auf einer aktuellen Kopie
ist sie ein No-op.

### `vergleich` — gegen die Referenz prüfen

```powershell
& $exe vergleich <refOrdner> <neuOrdner>
& $exe vergleich <refOrdner> <neuOrdner> --ohne Heizkessel.Quellwaerme,Weiterer.Schluessel
```

Exit-Code 0 = alles PASS, 1 = mindestens ein FAIL. Je Projekt werden die zehn größten
Abweichungen ausgegeben, sortiert nach dem Vielfachen der erlaubten Toleranz.

`--ohne` (seit Etappe D4) nimmt **ausdrücklich benannte** Schlüssel vom Vergleich aus und
nennt sie in der Ausgabe. Der Zweck ist eng: Führt eine Etappe eine neue **Ergebnisspalte**
ein, wächst `aggregate.csv` zwangsläufig um einen Schlüssel, und gegen die eingefrorene Basis
verdeckt diese Meldung die eigentliche Frage — *sind die Altwerte unverändert?* Genau dafür
ist die Option da, **nicht** um Abweichungen wegzuschalten. Sobald die Basis neu gesetzt ist
(hier: B4), laufen die Vergleiche wieder ohne Ausschluss.

### `pruefen` — Plausibilität eines Laufs

```powershell
& $exe pruefen <ordner>
```

Prüft Rasterlänge (8760 oder 35040 Zeilen), NaN/Inf und Jahressummen größer null dort, wo
dem Projekt ein Modul zugeordnet ist. Ein aktiviertes Gewerk ohne Modul ergibt zwangsläufig
null und wird nur als Hinweis gemeldet.

### `liste` — Projektlandschaft ansehen

```powershell
& $exe liste                                 # legt die Arbeitskopie neu an
& $exe liste C:\Waermeplan\Paket7_Nach\DB_Basis   # liest eine vorhandene Kopie
```

Zeigt alle Projekte mit Ausstattung und die automatische Auswahl samt Begründung, ohne zu
rechnen. Mit Ordnerargument wird **nichts kopiert** — so lässt sich die Auswahl auf einer
eigenen Kopie außerhalb des Repos nachprüfen, ohne die `Arbeitskopie` eines laufenden
Vergleichs zu überschreiben.

## Toleranzen

Für Skalare und für jedes einzelne Vektorelement gilt dieselbe Regel:

| Wertebereich | Toleranz |
|---|---|
| Betrag ≥ 1 | relative Abweichung bis **1e-4** |
| Betrag < 1 | absolute Abweichung bis **0,01** |

Nichtnumerische Werte (Modulnamen, Schalter wie `Sim_Waermepumpe`) müssen exakt
übereinstimmen. Fehlende oder zusätzliche Dateien und Einträge gelten als FAIL.

Volatile Größen sind bewusst nicht Teil des Vergleichs: die Autowert-IDs der
`Tab_Ergebnis*`-Zeilen und der Zeitstempel des Laufs.

## Ablauf vor einer Änderung an der Engine (Paket 1 ff.)

Zwei gleichwertige Wege. **Weg B** ist der, mit dem die aktuelle Basis entstanden ist; er
ist zwingend, wenn parallel gearbeitet wird oder die Kopie außerhalb des Repos liegen soll.

### Weg A — mit `lauf` (bequem, benutzt `Referenzlaeufe\Arbeitskopie`)

1. **Sauberen Ausgangszustand herstellen.** Anwendung schließen, Arbeitsverzeichnis auf dem
   Stand, gegen den verglichen werden soll.
2. **Änderung umsetzen** und die Anwendung neu bauen (`WP-Plan.sln` **und**
   `Referenzlauf.csproj`).
3. **Neu rechnen und vergleichen** — Referenz ist die aktuelle Basis, seit dem
   16.08.2026 also `2026-08-16_B4`:
   ```powershell
   & $exe lauf --ziel C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-20_Paket10 `
               --projekte 1007,1008,1011,1017,1018,1021,1023,1024
   & $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-16_B4 `
                    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-20_Paket10
   ```
   `lauf` kopiert **und migriert** die Arbeitskopie selbst.

### Weg B — eigene Kopie außerhalb des Repos (`migration` + `projekt`)

```powershell
# 1. Eigene, vollständig migrierte Kopie anlegen (schreibt NIE in die produktive DB)
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\MeinTest\DB

# 2. Auswahl kontrollieren (rein lesend, kopiert nichts)
& $exe liste C:\Waermeplan\MeinTest\DB

# 3. Die acht Referenzprojekte einzeln rechnen
foreach ($id in 1007,1008,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\MeinTest\Lauf\Projekt_$id" C:\Waermeplan\MeinTest\DB
}

# 4. Gegen die aktuelle Basis vergleichen und plausibilisieren
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-16_B4 C:\Waermeplan\MeinTest\Lauf
& $exe pruefen   C:\Waermeplan\MeinTest\Lauf
```

Der Modus `projekt` migriert **nicht** — er erwartet eine fertige Kopie aus Schritt 1.
Ohne Schritt 1 rechnet er auf einem unvollständigen Schema.

> **Schritt 1 ist keine Bequemlichkeit.** Er ist der Grund, warum die Anwendung auf der Kopie
> dieselben Werte rechnet wie auf der gepflegten Datenbank. Eine Datenlücke, die dabei besonders
> leicht zuschlägt, ist die Projekteinstellung `Extrapolation_erlaubt` (Paket 8): Die **Spalte**
> entsteht schon in Migrationsschritt 2 und wird von der stillen Rückfallebene
> `WaermequelleClass.SchemaSicherstellen` ebenfalls angelegt — Access belegt sie dabei in allen
> bestehenden Zeilen mit `False`, also „Extrapolation verboten". Ihre **Vorbelegung auf WAHR** setzt
> erst Schritt 7. Auf einer Kopie ohne Schritt 1 stünde die Einstellung damit überall auf „verboten",
> und jeder Lauf mit einer unterschrittenen Wärmepumpen-Kennlinie bräche ab.
>
> Seit der Paket-8-Nacharbeit (Befund N8) fängt der Leser das ab: Solange
> `Tab_Applikation.SchemaVersion` **unter 7** steht, gilt ein `False` in dieser Spalte als
> Datenlücke und nicht als Anwenderentscheidung — es wird als „erlaubt" gelesen. Ab Schemastand 7
> zählt der gespeicherte Wert. Ein Lauf im Modus `projekt` auf einer nicht migrierten Kopie bricht
> also nicht mehr fälschlich ab; wer die Einstellung wirklich prüfen will, braucht eine migrierte
> Kopie (Schritt 1).

### Danach

**Abweichungen bewerten.** Jede gemeldete Abweichung ist entweder gewollt — dann im
Umsetzungsprotokoll begründen und den neuen Ordner zur Referenz erklären — oder ein
Fehler.

Wichtig: Beide Läufe müssen von derselben Quelldatenbank ausgehen. Ändern sich zwischendurch
die Projektdaten, vergleicht man Äpfel mit Birnen. Die Quelle steht im Kopf von
`lauf_protokoll.md`.

## Die Projektauswahl

Ohne `--projekte` wählt die Suite selbst, deterministisch und aus der Arbeitskopie heraus.
Sie deckt zuerst **sechs** Pflichtkategorien ab — Wärmepumpe mit Pufferspeicher,
Heizkessel, BHKW, Solarthermie, den Minimalfall „nur Wärmepumpe" und (seit Paket 7)
Wärmepumpe mit **Quellspeicher** — und füllt dann auf neun Projekte auf: erst mit neuen
Erzeugerkombinationen, danach mit abweichender Anlagenausstattung. Übergangen werden
Projekte ohne Eintrag in `Tab_Einstellungen` und ohne Klimaregion; die stehen mit
Begründung im Protokoll.

Die Kategorie „Quellspeicher" steht bewusst **hinter** den fünf ursprünglichen: so bleiben
deren Wahlen unverändert und es kommt nur ein Projekt hinzu (1021).

Ändert sich die Projektlandschaft, ändert sich womöglich auch die Auswahl — und damit
lassen sich die Ordner nicht mehr vergleichen. Wer über längere Zeit dieselbe Basis braucht,
gibt die Projekte fest vor:

```powershell
& $exe lauf --projekte 1007,1008,1011,1017,1018,1021,1023,1024
```

> **Seit dem 15.08.2026 sind es acht statt neun IDs.** Projekt **1010 „Kurs EE"** hat der
> Anwender gelöscht; es war die Kategorie **„nur Wärmepumpe"**. Bis ein Ersatzprojekt dieser
> Kategorie nachrückt, ist die Pflichtkategorie unbesetzt — bei einer Auswahl ohne
> `--projekte` füllt die Suite stattdessen mit einer weiteren Erzeugerkombination auf.

## Dialoge der Engine

**Seit Paket 8 zeigt die Engine keine MessageBoxen mehr** (Konzept Kapitel 13.4). Grenz- und
Fehlerfälle laufen über den Protokollkanal `SimulationProtokoll`; jeder Eintrag geht zusätzlich auf
die Konsole und steht damit im `lauf_protokoll.md`:

```
Simulation Hinweis:  vollwertig gerechnet, Randbedingung erwähnenswert
Simulation Warnung:  gerechnet, aber mit einer Ersatzannahme
Simulation FEHLER:   Lauf abgebrochen, es wird kein Ergebnis gespeichert
```

Die frühere Rückfrage „Temperatur unterschreitet Kennlinien-Untergrenze, soll extrapoliert werden?"
ist zur **Projekteinstellung** `Extrapolation_erlaubt` geworden — Vorbelegung WAHR, also genau die
Antwort, die in jedem dokumentierten Lauf gegeben wurde. Statt eines weggeklickten Dialogs steht
jetzt eine `Simulation Hinweis:`-Zeile im Protokoll: derselbe Rechenweg, nur sichtbar.

Der **Dialogwächter läuft trotzdem weiter mit**: Er findet Dialogfenster des eigenen Prozesses und
drückt den bejahenden Knopf (Ja vor OK vor Ignorieren). Er hat nach Paket 8 nichts mehr zu drücken —
und ist genau deshalb wertvoll: Er ist die Messsonde, mit der sich jede künftig neu eingeschleppte
MessageBox im Rechenpfad sofort im Lauf-Protokoll zeigt. Taucht dort ein Eintrag auf, ist das ein
Befund.

Der Zähler des Protokolls wertet die Konsolenausgabe der Kindprozesse aus und kennt beide
Schreibweisen — `WARNUNG:` (Suite) und `Simulation Warnung:` (Engine, seit der Paket-8-Nacharbeit,
Befund N13b). Hinweise werden bewusst nicht mitgezählt: Sie melden einen vollwertig gerechneten
Grenzfall, und den gab es in jedem bisherigen Referenzlauf.

Bleibt ein Projekt trotzdem hängen — etwa an einem Dialog, den der Wächter nicht bedienen
kann — greift das Zeitlimit. Jedes Projekt läuft in einem eigenen Kindprozess, der nach
Ablauf abgeräumt wird; die halbfertige Ausgabe wird gelöscht, das Projekt im Protokoll als
übersprungen vermerkt, und die übrigen Projekte laufen weiter.

## Aufräumen

Ein Lauf belegt rund 30 MB (neun Projekte). Die CSVs gehören ins Git — sie sind die Referenz —, alte
Laufordner dagegen nicht auf Dauer. Nicht mehr benötigte Ordner löschen, statt sie
anzusammeln. `Arbeitskopie/` bleibt ohnehin außen vor: `Kenndaten.accdb` steht in
`.gitignore`.
