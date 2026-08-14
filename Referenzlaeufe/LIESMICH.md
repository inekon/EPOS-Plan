# Referenzlauf-Suite (Paket B1)

Regressionsbasis für den Simulationskern von EPOS-Plan.

Vor jedem Umbau an der Engine wird der aktuelle Stand als CSV eingefroren; nach dem Umbau
läuft derselbe Satz Projekte erneut und wird mit Toleranz gegen den eingefrorenen Stand
verglichen. Was sich dabei ändert, ist entweder gewollt — dann wird die Referenz neu
gesetzt — oder ein Fehler.

Grundlage: `WindowsFormsApplication1/Allgemein/Simulation/Konzept_Simulation_QuellenSenken.md`,
Paket B1, Kapitel 9.

## Aktuelle Basis

**`2026-08-14_Paket4/`** — seit der Abnahme der Paket-4-Review-Nacharbeit (14.08.2026) die
gültige Referenz, **neun Projekte** (1007, 1008, 1010, 1011, 1017, 1018, 1021, 1023, 1024).
Jeder neue Vergleich läuft gegen diesen Ordner.

> **Die Basis ist mit Feature-Flag `Kaskade_Zweikanalig` = AUS gerechnet** und bildet damit
> weiter den einkanaligen Altpfad ab. Das bleibt so, bis die Bestandsprojekte projektweise
> auf die zweikanalige Kaskade umgestellt werden. Ein Lauf mit gesetztem Flag ist **kein**
> Regressionsfall gegen diese Basis — er wird gegen den Flag-aus-Lauf desselben Codes
> verglichen (Umsetzungsprotokoll Paket 4).

Gegenüber `2026-08-14_Paket7` sind genau **drei** Werte neu, alle in Projekt 1021 und alle
begründet in `2026-08-14_Paket4/lauf_protokoll.md`: die ID-Semantik des Quellspeichers
(`Pufferspeicher[0].ID_Pufferspeicher` 8 → 1018014) und die beiden laufzeitbasierten
Skalare aus dem Bestandsfehler **B0-13** (`WaermepumpeModul[0].Betriebsstunden`
6692,41 → 4,41; `Waermepumpe.Vollbenutzungsstunden` 3846,66 → 502,66). Alle übrigen
2.260.920 Werte sind byte-genau gleich.

`2026-08-14_Paket7/` bleibt als **vorheriger Stand** liegen, `2026-08-14_B0/` als
**historischer Stand** (Zustand vor Paket 1/3/7, acht Projekte). Ein Vergleich gegen B0
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
```

Exit-Code 0 = alles PASS, 1 = mindestens ein FAIL. Je Projekt werden die zehn größten
Abweichungen ausgegeben, sortiert nach dem Vielfachen der erlaubten Toleranz.

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
3. **Neu rechnen und vergleichen** — Referenz ist die aktuelle Basis, seit der
   Paket-4-Abnahme also `2026-08-14_Paket4`:
   ```powershell
   & $exe lauf --ziel C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-20_Paket5
   & $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket4 `
                    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-20_Paket5
   ```
   `lauf` kopiert **und migriert** die Arbeitskopie selbst.

### Weg B — eigene Kopie außerhalb des Repos (`migration` + `projekt`)

```powershell
# 1. Eigene, vollständig migrierte Kopie anlegen (schreibt NIE in die produktive DB)
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\MeinTest\DB

# 2. Auswahl kontrollieren (rein lesend, kopiert nichts)
& $exe liste C:\Waermeplan\MeinTest\DB

# 3. Die neun Referenzprojekte einzeln rechnen
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\MeinTest\Lauf\Projekt_$id" C:\Waermeplan\MeinTest\DB
}

# 4. Gegen die aktuelle Basis vergleichen und plausibilisieren
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket4 C:\Waermeplan\MeinTest\Lauf
& $exe pruefen   C:\Waermeplan\MeinTest\Lauf
```

Der Modus `projekt` migriert **nicht** — er erwartet eine fertige Kopie aus Schritt 1.
Ohne Schritt 1 rechnet er auf einem unvollständigen Schema.

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
& $exe lauf --projekte 1007,1008,1010,1011,1017,1018,1021,1023,1024
```

## Dialoge der Engine

Engine und `DataRepository` zeigen im Fehler- und Grenzfall MessageBoxen (Konzept
Kapitel 13.4). Ein headless-Lauf würde daran hängen bleiben, deshalb läuft ein
Dialogwächter mit: er findet die Dialogfenster des eigenen Prozesses und drückt den
bejahenden Knopf (Ja vor OK vor Ignorieren).

Das ist bewusst kein blindes Wegklicken. Die häufigste Rückfrage lautet „Temperatur
unterschreitet Kennlinien-Untergrenze, soll extrapoliert werden? Bei nein wird Simulation
abgebrochen!" — mit „Nein" würde die Wärmepumpe für diese Stunden schlicht null liefern.
Der Referenzlauf muss denselben Weg gehen wie ein Anwender, und der antwortet „Ja".

Jede beantwortete Rückfrage steht mit Titel, Text und gedrücktem Knopf im
`lauf_protokoll.md`. Taucht dort eine neue Meldung auf, lohnt der Blick: sie zeigt einen
Grenzfall in den Projektdaten.

Bleibt ein Projekt trotzdem hängen — etwa an einem Dialog, den der Wächter nicht bedienen
kann — greift das Zeitlimit. Jedes Projekt läuft in einem eigenen Kindprozess, der nach
Ablauf abgeräumt wird; die halbfertige Ausgabe wird gelöscht, das Projekt im Protokoll als
übersprungen vermerkt, und die übrigen Projekte laufen weiter.

## Aufräumen

Ein Lauf belegt rund 30 MB (neun Projekte). Die CSVs gehören ins Git — sie sind die Referenz —, alte
Laufordner dagegen nicht auf Dauer. Nicht mehr benötigte Ordner löschen, statt sie
anzusammeln. `Arbeitskopie/` bleibt ohnehin außen vor: `Kenndaten.accdb` steht in
`.gitignore`.
