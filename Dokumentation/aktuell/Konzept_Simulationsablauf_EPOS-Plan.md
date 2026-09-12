# Konzept: Simulationsablauf ohne Dialog — eine Ansicht, ein Rückweg

Stand 11.09.2026, gemessen am Stand `1e71d30` (`ios_migration_september`). Anlass ist die
Anwenderrückmeldung vom 11.09.2026 zum Bildschirmfoto des Stromspeicher-Reiters der
Simulationsergebnisse:

1. „Der Simulationsdialog im Fenster ist nicht gut. Evtl. wie bei ‚Speicherflotte Auslegung'."
2. „Bei Dialog ‚Speicherflotte Auslegung' springt bei ‚Zurück' auf das Hauptfenster und geht nicht zurück."
3. „Harmonisiere den Ablauf bei Simulation — am besten ohne Dialog. Erstelle Vorschlag."

Das Konzept schließt an [`Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`](Konzept_Stromspeicher_Dialoge_EPOS-Plan.md)
an (Abschnitt 2.1: „eine Ansicht statt Fenster in Fenster", Ablaufleiste) und an die
Entscheide **E‑5** (04.09.2026: Simulationskonfiguration als freie Ansicht, Ergebnis als
`Ueberlagerung`) und **W16c‑E‑3** („Ansicht wechseln statt Überlagerung") im
[`Umsetzungskonzept_iOS_EPOS-Plan.md`](Umsetzungskonzept_iOS_EPOS-Plan.md). Es beschreibt
nur den **Ablauf** — Konfiguration, Lauf, Ergebnis, Auslegung und den Weg zurück. Die Inhalte
der Reiter und der Rechenweg bleiben unberührt.

---

## 1. Ist-Stand (gemessen) — **Stand VOR #207**

> **Dieser Abschnitt beschreibt den Zustand vor Stufe S1.** Er ist mit Auftrag
> **#207** (11.09.2026, `4da303d`, Merge `e608457`) behoben und bleibt als
> Befund stehen: Er nennt die drei Wirte, die tote Stelle in der `AppWurzel` und
> die Ursache des gemeldeten Rückweg-Fehlers. Was seither gilt, steht in
> Abschnitt 2 und in der Zeile **S1** des Stufenplans.


### 1.1 Drei Wirte für zwei Seiten

Die zwei Simulationsseiten `SimulationKonfigSeite` (1 229 Z.) und `SimulationErgebnisSeite`
(653 Z.) sind seit W10b/W11b fertige Razor-Komponenten. Sie werden heute an **drei** Stellen
eingebettet, und nur eine davon ist eine Ansicht der `AppWurzel`:

| Wirt | Konfiguration | Ergebnis | Plattform |
|---|---|---|---|
| `Startseite.razor` (883 Z.) | löst die Reiter der Startseite **innerhalb der Startseiten-Komponente** ab (Z. 73–81, `_konfig`); „Beenden" führt zu den Reitern zurück | steht in einer **`Ueberlagerung`** der Startseite (Z. 285–295, Klasse `epos-ueberlagerung--breit`: `min(96vw, 1400px)`, `max-height 94vh`) — das „Dialog im Fenster" | Windows (Startansicht `STARTSEITE`) |
| `SimulationErgebnisSeite.razor` | öffnet die Konfiguration als **zweite Überlagerung in der Ergebnis-Überlagerung** (Z. 240–248, Knopf „Konfiguration ...") | — | Windows |
| `AppWurzel.razor` (1 067 Z.) | Ansicht `SIMULATION_KONFIGURATION` (Z. 120–124) | Ansicht `SIMULATION_ERGEBNIS` (Z. 125–129) | **heute tot**: Unter Windows liefert `HauptfensterHuelle` keine Simulationsgaben (nur `StromspeicherAuslegungGaben`, Z. 93), die Wurzel fällt auf `IProjektQuelle` zurück, und deren Standard ist `null` → „Seite geht nicht auf". Auf iOS setzt `IosProjektQuelle` (Projekte, Energieträger, BHKW, Lizenzlage) ebenfalls **keine** der drei Simulationsgaben um |

Kurz: **E‑5 wurde in der Startseite umgesetzt, nicht in der Wurzel.** Die Wurzel kennt die zwei
Schlüssel, aber kein Wirt füllt sie. Die Simulation ist auf iOS heute **nicht erreichbar**
(die Projektliste bietet nur Energieträger und BHKW-Wirtschaftlichkeit an), und unter Windows
nur über den Reiter „Simulation" der Startseite — die `Menuetabelle` (58 Punkte) führt keinen
Simulationspunkt.

### 1.2 Der Ablauf heute (Windows)

```
Startseite › Reiter „Simulation"
 ├─ Knopf „Simulation Konfiguration..."  → Konfiguration löst die Startseite ab (in der Startseiten-Komponente)
 │                                          „Beenden" → Startseite
 └─ Kachel „Simulation"                   → Ergebnis als ÜBERLAGERUNG (96 vw × 94 vh)
      Fußleiste: „Simulation starten ▶" · „Konfiguration ..." · „Ergebnis speichern" · „Beenden"
        ├─ „Simulation starten ▶"  → Fortschritt, dann zehn Reiter
        ├─ „Konfiguration ..."     → Konfiguration als ZWEITE Überlagerung darin (Fenster im Fenster im Fenster)
        └─ Reiter „Stromspeicher" › „Speicherflotte & Auslegung öffnen"
                                   → Ansicht STROMSPEICHER_AUSLEGUNG (Wurzel wechselt; Startseite samt Überlagerung wird entsorgt)
                                      „← zurück" → STARTSEITE mit Reitern — das Ergebnis ist weg
```

### 1.3 Ursache des gemeldeten Rückweg-Fehlers

`AppWurzel.Zeige(STROMSPEICHER_AUSLEGUNG)` merkt sich als Rückweg die **Ansicht**, in der
man stand: `_auslegungRueckweg = _ansicht` (Z. 700). Unter Windows ist das `STARTSEITE`, weil
das Ergebnis keine Ansicht der Wurzel ist, sondern eine Überlagerung **in** der Startseite.
`ZurueckZumAufrufer` (Z. 837–848) ruft dann `Zeige(STARTSEITE)`; die Startseiten-Komponente
wird neu aufgebaut, ihr Feld `_ergebnis` ist leer, die Überlagerung ist zu. Der Nachzug, den
`SimulationErgebnisHuelle.AuslegungOeffnen` (Z. 137–147) an die Auslegung übergibt
(`_flotteProjektGeaendert = true; _ergebnisGueltig = false`), trifft eine Hülleninstanz, deren
Seite nicht mehr steht. **Der Rückweg ist richtig gebaut, aber es gibt kein Ziel, zu dem er
zurückkönnte** — genau die Lücke, die das Stromspeicher-Konzept 2.1 mit „die Rückkehr geht
dorthin, woher man kam" versprochen hatte.

Die Wurzel führt heute **drei getrennte Rückwegfelder** (`_auslegungRueckweg`, `_kiRueckweg`,
der Assistent kehrt immer zur Liste zurück) und sagt es selbst: „eine Ansicht zur Zeit, kein
Ansichtenstapel" (Z. 441). Jede neue freie Ansicht bringt ein viertes Feld mit.

### 1.4 Was die Windows-Hülle heute hält

`SimulationKonfigHuelle.cs` (1 915 Z.) und `SimulationErgebnisHuelle.*` (fünf Dateien, 3 523 Z.)
bauen je ein Parameterwörterbuch. Beide werden aus `StartseiteHuelle` gerufen (Z. 107–113),
nicht aus `HauptfensterHuelle`. Der gerechnete Lauf (`SimulationControl`), die Bilder und die
Gültigkeitsmarke des Ergebnisses leben in der `SimulationErgebnisHuelle`-Instanz — sie
überleben einen Ansichtswechsel, die Razor-Seite darüber nicht.

---

## 2. Zielbild

**Die Simulation ist EINE freie Ansicht der `AppWurzel` mit Ablaufleiste, wie die
Stromspeicher-Auslegung.** Keine Überlagerung für das Ergebnis, keine Konfiguration in der
Startseite, keine Konfiguration in der Ergebnisseite. Wer die Auslegung verlässt, steht wieder
im Ergebnis, auf demselben Reiter.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Simulation · Projekt „Stromspeicher mit Wärmepumpe"                [← zurück] │
│  ① Konfiguration   ·   ② Simulation starten ▶   ·   ③ Ergebnis               │  ← Ablaufleiste
├──────────────────────────────────────────────────────────────────────────────┤
│ ③ Ergebnis                                                                    │
│  Parameter | Übersicht | Bedarf | Wärmepumpe | … | Stromspeicher              │  ← die zehn Reiter, unverändert
│  ┌ Speicherflotte und Auslegung ───────────────────────────────────────────┐  │
│  │ Datenstand … [Speicherflotte & Auslegung öffnen]                        │  │  → Ansicht STROMSPEICHER_AUSLEGUNG
│  └─────────────────────────────────────────────────────────────────────────┘  │     „← zurück" → hierher, Reiter Stromspeicher
│  Fußleiste: [Ergebnis speichern]                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

- **Schritt ①** ist die heutige `SimulationKonfigSeite` — unverändert, nur ohne eigenen
  „Beenden"-Knopf; „Speichern" bleibt.
- **Schritt ②** ist ein **Knopf, kein Blatt** (Muster Auslegung Schritt 4): Er startet den
  nebenläufigen Lauf mit `Fortschritt` und Abbruch und landet in ③. Er ist gesperrt, solange
  die Konfiguration ungespeichert ist oder die Vorprüfung (#190) rot ist; der Grund steht als
  Titel am Knopf.
- **Schritt ③** ist die heutige `SimulationErgebnisSeite` ohne die Fußknöpfe „Konfiguration ..."
  und „Beenden" — die Ablaufleiste ersetzt sie. „Ergebnis speichern" bleibt. Ohne gerechneten
  oder gespeicherten Lauf ist ③ gesperrt und sagt, warum.
- **Einstiegsmarke:** Der Knopf „Simulation Konfiguration..." der Startseite öffnet die Ansicht in
  ①, die Kachel „Simulation" in ③ (fällt auf ① zurück, wenn es kein Ergebnis gibt — mit
  Hinweis). Beides läuft über `Dienste.Navigation.OeffneMaske(Seitenschluessel.Simulation, marke)`,
  auf beiden Plattformen derselbe Weg.
- **„← zurück"** oben rechts (Text `FLOTTE_SEITE_ZURUECK`, wie die Auslegung) führt dorthin, woher
  man kam. Eine ungespeicherte Konfiguration löst die Rückfrage nach 62b‑E‑1 aus
  (`DarfVerlassen`, dieselbe Überlagerung wie beim Assistenten).
- **Die Auslegung bleibt eine eigene Ansicht** mit ihrer eigenen fünfschrittigen Ablaufleiste.
  Verschachtelte Ablaufleisten wären eine zweite Sorte „Fenster im Fenster". Ihr Rückweg führt
  in die Simulationsansicht, Schritt ③, Reiter „Stromspeicher"; der Nachzug markiert das Ergebnis
  als veraltet, und der Reiter zeigt das Banner „Flotte geändert — Simulation neu starten".
- **Kurze Unterdialoge bleiben Überlagerungen** (Regel SD‑Q1): Bedarfs-Detail, Wärmepumpen-Detail,
  Variantenvergleich, die sieben Quell-/Senken-/Pufferdialoge der Konfiguration. Sie haben eine
  eigene Rückkehr und keinen Zustand, den ein Ansichtswechsel verlieren könnte.

### 2.1 Ein Rückweg für alle Ansichten: der Rückwegstapel

Die drei Rückwegfelder der Wurzel werden **ein** Stapel aus Einträgen `(Schlüssel, Marke)`:

- `Zeige(ziel, marke)` legt die stehende Ansicht samt ihrer Marke auf den Stapel, wenn `ziel`
  eine Ansicht mit Rückkehr ist (Auslegung, KI-Assistent, Simulation aus der Startseite).
- `Zurueck()` nimmt den obersten Eintrag und ruft `Zeige(schlüssel, marke)`; lässt sich die
  Ansicht nicht mehr aufbauen, gilt wie heute die Startansicht.
- Die **Marke** ist ein kurzer Text (`schritt=3;blatt=Stromspeicher`), den die Ansicht als
  `[Parameter] string Marke` bekommt und beim Aufbau anwendet: Schritt der Ablaufleiste, aktives
  Reiterblatt. Mehr stellt die Wurzel nicht wieder her — die Regel aus #199 („die Wurzel stellt
  die Ansicht wieder her, nicht den inneren Zustand einer Komponente") bleibt; der Datenstand
  kommt aus der Hülle bzw. dem Kern-Controller, der den Ansichtswechsel überlebt.
- Der Stapel ist bewusst flach (höchstens drei Einträge: Startseite → Simulation → Auslegung →
  KI-Assistent) und wird beim Wechsel auf die Startansicht geleert. Ein Router ist das nicht
  und soll es nicht werden.

### 2.2 Die Windows-Hülle

Eine `SimulationHuelle` je Projekt fasst `SimulationKonfigHuelle` und `SimulationErgebnisHuelle`
zu **einem** Parameterwörterbuch der Ansicht zusammen und wird — wie `StromspeicherAuslegungGaben`
— als Delegat in `HauptfensterHuelle.Gaben()` eingelegt (`SimulationGaben`). Die Startseiten-Hülle
gibt ihre zwei Simulationsgaben ab. Der gerechnete Lauf, die Bilder und die Gültigkeitsmarke
bleiben, wo sie sind: in der Hülleninstanz, die zwischen zwei Besuchen steht. Der Fensterbesitzer
(`Func<Form>`) bleibt für die Dateiwähler der CSV-Ausgaben.

### 2.3 iOS

Die Ansicht ist plattformfrei. Damit iOS sie zeigt, muss `IosProjektQuelle.SimulationGaben`
dasselbe Wörterbuch liefern. Dafür ist **zu messen**, wie viel von den 5 438 Zeilen der zwei
Windows-Hüllen Datenweg ist, der nach Regel iU5 in einen Kern-Controller gehört, und wie viel
Plattform (Dateiwähler, Fensterbesitz). Das ist ein eigener Schritt (S2) und der Grund, warum
die Simulation heute auf iOS fehlt — nicht die Oberfläche.

---

## 3. Was sich für den Anwender ändert

| Heute | Danach |
|---|---|
| Ergebnis in einem Rahmen im Rahmen mit eigenem Rollbalken, 96 % Breite | Ergebnis füllt die Ansicht, ein Rollbalken |
| „Konfiguration ..." öffnet ein drittes Fenster | Ablaufleiste ① — ein Klick, kein Fenster |
| „Beenden" (Ergebnis), „Beenden" (Konfiguration) | ein „← zurück" oben rechts, mit Rückfrage bei ungespeicherter Konfiguration |
| Auslegung „← zurück" landet auf der Startseite | landet im Ergebnis auf dem Reiter Stromspeicher, Ergebnis als veraltet markiert |
| Simulation nur über die Startseite | zusätzlich über einen Menüpunkt (SIM‑Q3) und auf iOS (S2) |
| Kachel „Simulation" öffnet immer das Ergebnis | öffnet ③, ohne Ergebnis ① mit Hinweis |

Was **nicht** anders wird: die zehn Reiter, die Konfigurationskarten, das Schema, der Lauf, der
Rechenweg, der Referenzlauf.

---

## 4. Stufenplan

| Stufe | Inhalt | Prüfmuster |
|---|---|---|
| **S1** — Ansicht und Rückweg (Windows) — **umgesetzt #207** (11.09.2026, `4da303d`, Merge `e608457`) | `Seitenschluessel.Simulation` (`SIMULATION`), Seite `EPOS.UI/Seiten/Simulation/SimulationSeite.razor` mit `Ablaufleiste` (Vorne ①, Rechnen ②, Hinten ③) und `Marke`; ① und ③ betten die zwei bestehenden Seiten ein; Fußknöpfe „Konfiguration ..."/„Beenden" und die Konfig-Überlagerung fallen aus der Ergebnisseite, `_konfig`/`_ergebnis`/Überlagerung fallen aus der Startseite; Rückwegstapel in `AppWurzel` ersetzt `_auslegungRueckweg`/`_kiRueckweg`; Auslegung kehrt mit Marke zurück; `SimulationHuelle` und `SimulationGaben` in `HauptfensterHuelle`; die zwei alten Schlüssel `SIMULATION_KONFIGURATION`/`SIMULATION_ERGEBNIS` bleiben als Einstiegsmarken (①/③) gültig; Menüpunkt nach SIM‑Q3 | bunit: Ablaufleiste schaltet, ② gesperrt bei ungespeicherter Konfiguration, ③ gesperrt ohne Ergebnis, Rückweg aus der Auslegung landet in ③ auf „Stromspeicher", Rückfrage bei ungespeicherter Konfiguration; **Wache:** `SimulationErgebnisSeite` und `SimulationKonfigSeite` stehen in keiner `Ueberlagerung` mehr (Muster `UeberlagerungstitelTests`); Referenzlauf 5/5 byte-gleich (kein Rechenweg) |
| **S2** — iOS erreicht die Simulation — **umgesetzt #208** (11.09.2026, `2f62ca4` + `81d8f2f`; Anwenderentscheid #208‑E‑1 = A: Datenseite `EPOS.UI.Daten`; Merge `c989745`, Gate sept28) | Messung der zwei Hüllen (Datenweg → plattformfreies Projekt `EPOS.UI.Daten`, Plattform bleibt), `IosProjektQuelle.SimulationGaben`, Knopf „Simulation…" in der Projektliste; iOS-Lauf 43 **gebündelt mit #202** (trifft die Hülle) | iOS-CI: Ansicht baut, Prüfmodus unverändert; Kern-Tests für den verlegten Datenweg |

S1 ist ohne S2 abnehmbar. S2 setzt S1 voraus, weil es dasselbe Wörterbuch liefern muss.

### Was **#207** umgesetzt hat

- **`Seitenschluessel.Simulation` (`SIMULATION`)** mit `Masken.Simulation`-Zwilling im Kern
  (Muster `Masken.KiAssistent`, #199): EIN Weg für Menüpunkt, Startseitenknopf und Kachel,
  auf beiden Plattformen über `Dienste.Navigation.OeffneMaske(Masken.Simulation, marke)`.
  Die zwei alten Schlüssel `SIMULATION_KONFIGURATION`/`SIMULATION_ERGEBNIS` bleiben als
  **Einstiegsmarken** (①/③) gültig.
- **`EPOS.UI/Seiten/Simulation/SimulationSeite.razor`** mit `Ablaufleiste` (① Konfiguration ·
  ② „2 Simulation starten ▶" als Knopf · ③ Ergebnis), `Marke`-Parameter
  (`schritt=1|3;blatt=<Reiterschlüssel>`), Kopf mit Titel, Projektzeile, Infoknopf und
  „← zurück", Rückfrage beim Verlassen (62b‑E‑1). **Beide Blätter bleiben montiert**, sobald
  sie einmal standen — ein `@if` würde den gerechneten Lauf, den Bilderspeicher und das
  offene Reiterblatt entsorgen.
- **Die zwei Seiten bleiben**, sie verlieren nur ihre eigenen Ausgänge: die Ergebnisseite die
  Fußknöpfe „Konfiguration …" und „Beenden" samt der Konfigurations-Überlagerung, die
  Konfigurationsseite ihren „Beenden"-Knopf. Der Fußknopf „Simulation starten ▶" der
  Ergebnisseite BLEIBT — er ist der Zwilling von ② und trägt die Seite auch ohne Leiste
  darüber (iOS, Stufe S2). Der **Automatikstart ist in der Ansicht abgeschaltet**: Der Lauf ist
  Schritt ② und damit ein bewusster Klick.
- **Aus der Startseite fallen** `_konfig`, `_ergebnis`, die Ergebnis-`Ueberlagerung`, die
  Konfig-Einbettung und die zwei Parameter `SimulationKonfigGaben`/`SimulationErgebnisGaben`.
- **Rückwegstapel in `AppWurzel`**: `List<(Schluessel, Marke)>`, höchstens drei Einträge,
  geleert beim Wechsel auf die Startansicht. Er löst `_auslegungRueckweg` (#192) und
  `_kiRueckweg` (#199) ab; `ZurueckZumAufrufer` und `ZurueckVomAssistenten` holen daraus.
- **`WindowsFormsApplication1/Views/Simulation/SimulationHuelle.cs`** hält je Projekt die zwei
  Hülleninstanzen und legt `["SimulationGaben"]` in `HauptfensterHuelle.Gaben()`; die
  Startseiten-Hülle hat ihre zwei Simulationsgaben abgegeben und reicht nur noch ihren
  `BedarfsZustand` weiter. Damit trifft der Nachzug aus `SimulationErgebnisHuelle.AuslegungOeffnen`
  (`_flotteProjektGeaendert`, `_ergebnisGueltig = false`) die Hülle, die ③ danach zeigt.
- **Menüpunkt „Simulation…"** (`MENU_SIMULATION`) im Kopf „Projekt", unmittelbar hinter
  „Varianten und Bericht…" — die Menütabelle steht damit bei **59 Punkten, 46 handelnd**.

### Was **#208** umgesetzt hat (Stufe S2)

**Die Messung.** Die zwei Datenhüllen führen **5 490 Zeilen**, und davon waren genau **sechs**
Windows: ein `Func<Form>` als Fensterbesitzer und die eine Stelle, die ihn braucht — der
Wärmepumpen-Assistent (`WaermepumpenHuelle.Gaben(IWin32Window, …)`). Alles andere geht seit
Paket iU5 über `Dienste.*` und ist auf jeder Plattform derselbe Weg. Der Grund, warum die
Simulation auf iOS fehlte, war also tatsächlich nicht die Oberfläche — es war der ORT der
Datenseite.

**Der Ort: ein neues Projekt `EPOS.UI.Daten`.** Es sieht den Kern UND die Oberfläche und kennt
keine Plattform (`EnableWindowsTargeting=false` wie `EPOS.Kern` und `EPOS.UI`). In den Kern
selbst konnte die Datenseite **nicht** ziehen: Eine Hülle baut genau die DTO der Razor-Seiten
(`SimulationKonfigDaten`, `ChipDaten`, `SchemaBild`, `Rueckmeldung`), und `EPOS.UI` kennt
`EPOS.Kern`, nicht umgekehrt — dieselbe Begründung, die schon `KiMaskenbruecke` im Kopf trägt.
In `EPOS.UI` konnte sie ebenso wenig bleiben: Dort gilt „Keine Datenbank". Verlegt sind
**18 Dateien / 8 115 Zeilen**: die zwei Simulationshüllen samt Teildateien, die sieben
Unterdialoghüllen der Konfiguration, `PufferSpProjektHuelle`, `BedarfErgebnisHuelle` (ihre
tote `Zeigen`/`Oeffnen`-Hälfte ist dabei gefallen — sie hatte im ganzen Bestand keinen
Aufrufer mehr) und `StromspeicherAuslegungHuelle`.

**Die Windows-Hülle bleibt — als ADAPTER.** `Views/Simulation/SimulationHuelle.cs` fällt von
122 auf **64 Zeilen** und trägt nur noch den Fensterbesitzer, den sie als benannten Weg in die
Quelle legt. **Windows verhält sich unverändert**;
`HauptfensterHuelle.Gaben()["SimulationGaben"]` kommt jetzt aus
`EPOS.UI.Daten/Simulation/SimulationAnsichtQuelle.cs` (116 Z.).

**Zwei Nähte statt einer Plattformbindung** — beide benannt, beide mit Standardfassung:
`SimulationPlattformwege` (der Wärmepumpen-Assistent; ohne Weg lehnt die Hülle **benannt** ab
und fällt nicht still aus) und `Katalogwege` (der Auslieferungskatalog der Pufferspeicher, der
noch in `KatalogBrowserHuelle` steckt — ohne eingehängten Haken zeigt die Pufferverwaltung den
Knopf „Katalog ansehen" gar nicht erst).

**Drei Stellen sind dabei in den Kern gezogen**, weil sie dort hingehören:
`SchemaMigration.SimulationGesperrt` war nur eine Weiterleitung auf
`SchemaStand.SimulationGesperrt` (die Hüllen rufen jetzt den Kern unmittelbar),
`KartenStil.Kreisziffer` ist reine Zeichenarbeit an einer Ladeposition und heißt seither
`Ladeordnung.Kreisziffer`, und `HilfeKontext.SetzeBereich` erreicht die Datenseite über den
neuen Haken `KiChatKontext.BereichMelder` (Windows hängt ihn in `Program.Main` ein — der
Gegenweg zum vorhandenen `AktiverBereich`).

**iOS.** `IosProjektQuelle.SimulationGaben` hält je Sitzung EINE `SimulationAnsichtQuelle` —
der gerechnete Lauf und die Bilder überleben damit einen Ansichtswechsel, genau wie unter
Windows —, und die **Projektliste** führt je Zeile einen dritten Knopf „Simulation…"
(`Seitenschluessel.Simulation`). Der Rückweg läuft über den Stapel aus #207 und landet wieder
in der Liste.

Offen bleibt auf iOS genau das, was ein Fenster braucht: der Wärmepumpen-Assistent (benannt
abgelehnt) und der Katalogbrowser der Pufferverwaltung (kein Delegat, kein Knopf). Beide
fallen mit dem Umzug der Katalogmasken (iU11).

---

## 5. Fragen mit Empfehlung

**Entscheid 11.09.2026: alle sechs nach Empfehlung** (Anwender: „SIM‑Q1 bis Q6: Empfehlung"). S1 = Aufgabe #207, S2 = Aufgabe #208.

| Frage | Empfehlung | Stand |
|---|---|---|
| **SIM‑Q1** Schrittfolge: drei Schritte mit ② als Knopf, oder vier Blätter? | Drei; ② ist ein Knopf wie Schritt 4 der Auslegung — ein Lauf ist kein Blatt, das man ansieht | **entschieden 11.09.2026 (Empfehlung)** |
| **SIM‑Q2** Auslegung als Schritt ④ derselben Ansicht oder eigene Ansicht mit Rückkehr? | Eigene Ansicht; Rückkehr mit Marke in ③/„Stromspeicher". Zwei Ablaufleisten ineinander wären das nächste Fenster im Fenster | **entschieden 11.09.2026 (Empfehlung)** |
| **SIM‑Q3** Menüpunkt „Simulation…" im Kopf „Projekt" neben „Varianten und Bericht…"? | Ja — die Startseite ist heute der einzige Weg; ein Punkt, Ziel `SIMULATION` ①, kein Untermenü (Regel W16c‑E‑6) | **entschieden 11.09.2026 (Empfehlung)** |
| **SIM‑Q4** Rückwegstapel mit Marke für alle Ansichten (Auslegung, KI-Assistent, Simulation)? | Ja; drei Felder werden ein Mechanismus, flach, kein Router | **entschieden 11.09.2026 (Empfehlung)** |
| **SIM‑Q5** Bedarfs-Detail, Wärmepumpen-Detail, Variantenvergleich bleiben Überlagerungen? | Ja (Regel SD‑Q1: kurze Unterdialoge mit eigener Rückkehr) | **entschieden 11.09.2026 (Empfehlung)** |
| **SIM‑Q6** S2 (iOS) direkt nach S1 starten, iOS-Lauf mit #202 bündeln? | Ja — die Simulation ist die erste Fachseite der iOS-Migration und heute dort nicht erreichbar | **entschieden 11.09.2026 (Empfehlung)** |
| **SIM‑E‑1** Die Kachel „Simulation" der Startseite: öffnen oder rechnen? | **RECHNEN** — sie heißt „Simulation starten" und löst Schritt ② aus (Anwenderwort, Windows-Abnahme #216) | **entschieden 11.09.2026 (Anwender), umgesetzt #216** (`5ef1433`, Merge `bcd3725`; Gate sept25) |
| **SIM‑E‑2** Der Startseiten-Reiter „Simulation" (drei Bildschirmfotos 11.09.2026): rechts leer, Kachel wechselt in die Ansicht — was soll rechts stehen, was tut die Kachel? | **Option 1: Die Kachel „Simulation starten" rechnet AN ORT UND STELLE** (Fortschrittsbalken, Abbrechen, Sperrgründe wie an Schritt ②); rechts im Reiter steht danach dieselbe Ergebniskomponente wie Schritt ③ (Übersicht zuerst, Reiter darüber, „Ergebnis speichern" im Kopf), ohne Ergebnis ein Hinweis. Die Ansicht bleibt für Konfiguration (①) und Vollbild; die Dienste kommen aus derselben Quelle wie die Ansicht (#208, damit auch iOS); der Rückweg aus der Auslegung führt in den Reiter zurück. Folgen nach Empfehlung angenommen. | **entschieden 11.09.2026 (Anwender), umgesetzt #220** (Abschnitt 9) |
| **SIM‑E‑3** Die Übersicht des Ergebnisses: zweimal dieselben Zahlen, Zahlenspalte weit vom Kopf, leerer Ring bei 0 % | **a)** EINE Übersicht (Dashboard Wärme \| Strom), **b)** Ringvariante A (grauer Vollring, Rest immer als Segment), **c)** keine Zoomleiste an Ringen | **entschieden 11.09.2026 (Anwender: „Empfehlung"), umgesetzt #222** |

---

## 6. Abgrenzung

- Keine Änderung an Rechenweg, Reiterinhalten, Diagrammen, `SimulationLaufCtrl`, `SimulationErgebnisCtrl`.
- Keine Änderung an der Auslegungsansicht selbst außer dem Rückweg über den Stapel.
- Kein allgemeiner Router in der Wurzel; der Stapel trägt höchstens drei Einträge.
- Die Startseite behält ihren Reiter „Simulation" mit Knopf und Kachel — nur die Ziele wechseln.

---

## 7. Windows-Abnahme 11.09.2026 (#216) — die Ansicht nachgeschärft

S1 stand, und der Anwender hat sie an drei Bildschirmfotos abgenommen. Vier Sätze kamen
zurück, und alle vier betreffen die ANSICHT, nicht den Ablauf:

1. „Belege den Button (Kachel ‚Simulation') mit ‚Simulation starten'."
2. „Bringe die Elemente aus dem Dialog auf die rechte Seite mit besserem Design."
3. „Nimm Parameter heraus — die Netzverluste können an eine andere Stelle (werden diese
   überhaupt verwendet?)."
4. „Stelle die Übersicht als erstes dar."

### 7.1 SIM‑E‑1 — die Kachel rechnet

**Anwenderentscheid SIM‑E‑1 (11.09.2026):** Die Kachel auf dem Startseiten-Reiter
„Simulation" heißt **„Simulation starten"** (`START_K_DETAILSIM_T`) und **löst Schritt ②
aus** — Marke `schritt=2`, derselbe Weg wie der Rechenknopf der Ablaufleiste: Sperrgrund
prüfen, Fortschritt, Abbrechen. Ist der Lauf gesperrt, bleibt die Ansicht bei ① und nennt
den Grund; eine Marke ist ein Wunsch, kein Befehl.

Das **revidiert für diese Kachel** den #207-Entscheid „die Kachel öffnet ③ und startet
nicht von selbst". Der Grund dafür war der Automatikstart des alten Ergebnisfensters, der
bei jeder Rückkehr aus der Auslegung erneut rechnete — ein Klick auf eine Kachel, die
ausdrücklich „starten" heißt, ist etwas anderes. Der Knopf „Simulation Konfiguration…"
bleibt und öffnet ① (`schritt=1`).

### 7.2 EINE rechtsbündige Werkzeugleiste

Der Kopf der Ansicht trägt jetzt alles, was sie bedient, in EINER Zeile, rechtsbündig:

```
Simulation · Projekt „…"        [1 Konfiguration|2 Simulation starten ▶|3 Ergebnis]  [Ergebnis speichern]  [i] [KI] [← zurück]
```

- Die **Schrittgruppe** ist die `Ablaufleiste` in ihrer neuen kompakten Form
  (`Kompakt="true"` → `.epos-ablaufleiste--kompakt`): ohne die Zäsur vor dem Rechenknopf
  (`margin-inline-start:auto` — bei fünf Schritten die Grenze zwischen „eingeben" und
  „rechnen", bei drei eine Lücke mitten in der Gruppe), ohne die untere Trennlinie und mit
  einem Rahmen um die drei Knöpfe. Der aktive Schritt bleibt hervorgehoben, ② bleibt der
  Handlungsknopf.
- **„Ergebnis speichern"** steht daneben und ist **nur in ③** frei — und dort nur nach
  einem vollständigen Lauf: Die Zustandsmaschine dahinter (Nacharbeit Paket 8, Befund N1)
  liegt unverändert in der Ergebnisseite und heißt dort `SpeichernMoeglich`.
- Dann das **EINE Paar [i] [KI]** und das **EINE „← zurück"**.

Dafür fallen an der eingebetteten Ergebnisseite zwei Dinge, die seit #207 doppelt standen:
ihre **Fußleiste** mit „Simulation starten ▶" und „Ergebnis speichern" (Restpunkt #207 —
sie war der Zwilling der Ablaufleiste) und ihr **eigener Kopf mit [i] [KI]** (Bild 1 der
Abnahme: zwei Paare untereinander). Ein Paar je Ansicht — sinngemäß die Hausregel
W11b‑B‑9 „Seite ohne eigenen Kopf". Die Seite verliert dabei keine Fähigkeit:
`LaufStarten()` und `ErgebnisSpeichern()` sind öffentlich, und auf iOS trägt dieselbe
Werkzeugleiste (S2). Das Hinweisband „*n* Hinweise zum Lauf" bleibt, wo es war — unter der
Leiste.

Auf schmalem Schirm bricht die Leiste um; die Schrittgruppe ist EIN Flexelement und bleibt
dabei als Ganzes zusammen. Alles im Stilblatt, kein Inline-Stil.

### 7.3 Der Reiter „Parameter" fällt — wohin die fünf Werte gehen

**„Werden diese überhaupt verwendet?" — ja.** Der Leseweg der Netzverluste, gemessen:

| Schritt | Ort |
|---|---|
| gelesen aus `Tab_Einstellungen` | `EPOS.Kern/Controller/KonfigurationCtrl.cs:127-128` → `KonfigurationModel.m_Netzverluste` |
| geprüft (> 100 % nur bei Einheit „%") | `EPOS.Kern/Controller/SimulationLaufCtrl.cs:72` |
| übergeben an den Wärmebedarf | `EPOS.Kern/Controller/SimulationLaufCtrl.cs:118-119` → `SimulationWaermebedarf.Netzverluste` |
| stündlich umgerechnet | `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:344-366` |
| anteilig auf die Bedarfskanäle verteilt | `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:374` → `SimulationKanaele.NetzverlusteVerteilen` |

Sie parametrieren also den **Lauf**, nicht sein Ergebnis — und stehen deshalb seit #216
dort, wo man den Lauf einstellt:

| Wert | Neuer Ort |
|---|---|
| **Netzverluste** (Wert + Einheit) | Schritt ①, Abschnitt **„Wärmebedarf"** über den zwei Spalten, mit Herleitungszeile „wirkt nur bei vorhandenem Wärmebedarf …", Vorgabe 0 % |
| **BHKW-Betriebsart** (0/1/2) und **untere Leistungsgrenze** [%] | Schritt ①, Parameterbereich der **BHKW-Karte** |
| **Heizstab** | Schritt ①, Parameterbereich der **Wärmepumpen-Karte** |
| **Betriebsbereitschaft** [h/a] | Schritt ①, Parameterbereich der **Heizkessel-Karte** |
| **Speicher-Parameterblock** (SoC-Band, Gerätegröße, Ladeschwelle, Betriebsart, Berechnungsart, Kompatibilität, Quellen, Wirtschaft, Preisquelle) | ③, Reiter **„Stromspeicher"** unter der Überschrift **„Einzelanlage (klassischer Projektlauf)"** — dort stand er seit W11b‑B‑28 schon, aber hinter `FlottenEinstiegMoeglich`; er hängt jetzt am STAND |

**Der Parameterbereich steht nur an der ERSTEN Karte seiner Art.** Ein Projekt kann
mehrere Module derselben Erzeugerart führen (zwei BHKW-Karten); die fünf Werte gelten
projektweit, und an jeder Karte stünde dieselbe Betriebsart mehrfach — dieselbe Regel,
nach der auch ▲▼ und × nur an der ersten Karte stehen.

**Der Speicherblock hing an der falschen Frage.** `FlottenEinstiegMoeglich` sagt, ob die
PLATTFORM eine Flotte rechnen kann — unter Windows immer; der Block war damit unerreichbar
geworden. Er hängt jetzt an `Daten.FlotteImProjektAktiv`: Solange kein Flottenstand
aktiviert ist, fährt der Projektlauf die Einzelanlage, und dann sind das ihre Parameter.
Mit aktivierter Flotte folgt die Betriebsart dem Häkchen „Netzladung" des Flotteneditors
(`StromspeicherSimCtrl`), und zwei Pflegestellen desselben Werts wären eine zuviel.

**Alle Schreibwege bleiben.** Die fünf Delegaten sind DIESELBEN, die bis #216 der Reiter
„Parameter" von `SimulationErgebnisDienste` bekam; sie stehen jetzt als
`SimulationParameterDienste` an `SimulationAnsichtDienste.Parameter` und kommen aus
**derselben Hülle** (`SimulationErgebnisHuelle.ParameterGaben()` →
`KonfigSchreiben`/`BetriebsartSchreiben`). Das ist kein Formalismus: Die Hülle hält
`_bhkwBetriebsart` und `_grenzleistungBhkw`, und mit genau diesen zwei Feldern bestückt
`SimulationLaufCtrl.Bestuecken` den Lauf. Ein zweiter Weg über die Konfigurationshülle
schriebe zwar dieselben Spalten, ließe die Felder aber stehen — der nächste Lauf rechnete
mit der alten Betriebsart. Die Werte landen unverändert in denselben Spalten von
`Tab_Einstellungen`; der Projektlauf der zwölf Einzelanlagen-Referenzprojekte ändert sich
nicht (Referenzlauf 1030 und 1046 byte-gleich).

**Ein Nebenbefund, der dabei behoben ist.** `SimulationKonfigHuelle.Speichern()`
schreibt die GANZE Zeile aus seinem Arbeitsstand `_konfiguration` weg (Delete + Insert,
wörtlich `btn_Speichern_Click`). Der Arbeitsstand entsteht beim Anlegen der Hülle — die
fünf Laufparameter darin sind ab dann alt. Vor #216 fiel das kaum auf (man musste aus ③
nach ① zurückgehen und speichern), seit #216 stünden Feld und Knopf nebeneinander: Der
Anwender änderte die Netzverluste, drückte „Konfiguration speichern" und sähe seine
Eingabe verschwinden. `Speichern()` liest die fünf Werte deshalb unmittelbar davor frisch
nach (`LaufparameterNachlesen`) — nur diese fünf, denn alles Übrige der Zeile ist der
Arbeitsstand, den der Knopf gerade schreiben soll.

Mit dem Reiter fallen `ParameterReiter.razor` (231 Z.), `ParameterReiterTests`, die
Schlüsselklasse `ParameterBlatt`, `ParameterDaten.Unterblaetter`, `BlattZuTool` der Hülle
und der verwaiste Ressourcenschlüssel `SIMERG_TAB_PARAMETER`.

### 7.4 Die Übersicht zuerst

Aus zehn Blättern werden neun, und das erste ist die **Übersicht**: Übersicht ·
Wärme-/Strombedarf · Erzeuger … · Stromspeicher · Ergebnis. `StartBlatt` bleibt der Weg
der MARKE — `blatt=STROMSPEICHER` aus der Auslegung trifft unverändert.

### 7.5 Was #216 NICHT anfasst

Rechenweg, Reiterinhalte, Diagramme, die Controller des Kerns, der Rückwegstapel, die
Auslegungsansicht und der Menüpunkt „Simulation…". Die Hüllen nur an den Schreibnähten.

---

## 8. SIM‑E‑3 (11.09.2026) — die Übersicht des Ergebnisses neu

Nach #216 hat der Anwender das Ergebnis am Bildschirmfoto „Heinestr 15" angesehen. Drei Sätze
kamen zurück, und alle drei betreffen die ANSICHT der Übersicht:

> „Zahlenspalte unter der Überschrift ist ungünstig, Strombedarfsdeckung bei 0 ist das Diagramm
> nicht gut … Design optimieren auf gute Übersicht und praktische Nutzbarkeit."

Der Entwurf dazu lag als Mockup vor; der Anwender hat alle drei Fragen mit **„Empfehlung"**
entschieden: **a)** eine Übersicht, **b)** Ringvariante A, **c)** keine Zoomleiste an Ringen.
Umgesetzt mit **Auftrag #222**.

### 8.1 Befund B‑1 — dieselben Zahlen zweimal

`UebersichtReiter.razor` hatte ZWEI Rollen: den Hauptreiter „Übersicht" (13 Kennzahlen in zwei
Gruppen) UND — mit `NurNavigator` — das erste Blatt des Reiters „Ergebnis" (zwei Ringe, zwei
Rest-Kacheln, das Eigenanteilsraster). Beide standen auf DERSELBEN Seite, nur zwei Reiter
auseinander; der Anwender sah Restwärme und Restspitze je zweimal.

**Fix.** Die Rolle `NurNavigator` entfällt. Der Hauptreiter wird das Dashboard, das Blatt
„Übersicht" im Ergebnisreiter fällt — der behält seine DREI eigenen Blätter (Autarkie-Analyse,
Wärme- und Stromproduktion) und macht seither mit der Autarkie auf. Startblatt der Seite bleibt
die „Übersicht" (#216). Aus neun Hauptreitern werden keine acht: Der Reiter „Ergebnis" bleibt,
es fällt ein Blatt IN ihm.

### 8.2 Das Dashboard — zwei Spalten Wärme | Strom

Die Reihenfolge bleibt die gewohnte (W11b‑B‑15 „nach Kategorie gruppiert", W11b‑B‑12 „Restwärme
unter dem Wärmering"): links Wärme, rechts Strom. Jede Spalte trägt fünf Bänder.

| Band | Inhalt |
|---|---|
| Kopf | „Wärme" bzw. „Strom", rechts ein Abzeichen: die KASKADE des Laufs (`tool[0…3]`) bzw. „kein Stromerzeuger im Projekt" |
| Kennzahlen | Bedarf · Deckung durch Erzeuger · Rest — DREI Zahlen statt dreizehn, die letzte betont |
| Ring | links das Bild, rechts die Legende als **HTML** (je Segment MWh und %, der Rest abgesetzt, darunter die Summe = Bedarf mit 100 %) |
| Tabelle | je Erzeuger seine Zahlen, Summe der Erzeuger und Restzeile |
| Fuß | der Schalter für die Zeilen ohne Beitrag und „Wärmebedarf Übersicht…" bzw. „Strombedarf Übersicht…" |

Unter 960 px stehen die zwei Spalten untereinander.

**Keine Zahl geht verloren.** Die 13 Kennzahlen des Vorläufers stehen sämtlich in den
Erzeugerreitern wieder — bis auf DREI: den Stromverbrauch von Wärmepumpe, Heizstab und
Spitzenkessel, aus dem sich der Nenner des Stromrings zusammensetzt. Sie stehen als eine leise
Zeile unter den Kennzahlen der Stromspalte.

### 8.3 Befund B‑2 — der leere Kreis bei 0 % (Ringvariante A)

Der Anwender sah bei 0 % Stromdeckung einen **leeren Kreis**. Die Ursache liegt tiefer als in der
Farbwahl und ist mit #222 gefunden: `SKPath.ArcTo` zieht bei einem Winkel von **360°** nichts —
Anfangs- und Endpunkt fallen zusammen, der geschlossene Pfad ist die leere Strecke vom
Mittelpunkt zum Rand und zurück. Ein Ring mit EINEM Segment (alles Netzbezug) blieb deshalb
weiß. Dieselbe Falle traf den Kuchen mit nur einem Segment.

**Fix (drei Teile).**

1. `ChartRenderer.Kreissegment` zeichnet ab 360° einen KREIS statt eines Bogens. Wächter:
   `ErgebnisbilderTests.Ein_einziges_Segment_fuellt_den_Ring_ganz` (prüft die Farbe im Bild)
   und die ChartProbe `ring_null_prozent`.
2. Der ungedeckte Rest ist in BEIDEN Ringen dasselbe **Grau** (`#D9DEE5`, im Stilblatt das Token
   `--epos-ring-rest`) — vorher Blau im Wärmering, Gelb im Stromring; ein voller gelber Kreis
   sah aus wie eine Leistung.
3. Die Mitte trägt eine **Unterzeile**: „gedeckt" bzw. — bei 0 % Stromdeckung — „Netzbezug
   100 %". Darunter steht im HTML der Weg: „Kein Stromerzeuger in der Kaskade. Photovoltaik,
   BHKW oder Speicher unter ① Konfiguration aufnehmen …"

### 8.4 Die Legende verlässt das Bild

`ChartRenderer.Ring` bekommt eine Überladung mit `mitteUnterzeile` und `mitLegende`. Ohne Legende
wird das Bild **quadratisch (420 × 420)**; die 720 × 560 des Vorläufers waren zu zwei Fünfteln
Legendenfläche. Die Legende steht als HTML neben dem Ring — kopierbar, mitwachsend und nicht
abschneidbar. Sie kommt aus DERSELBEN Segmentliste wie das Bild
(`SimulationErgebnisHuelle.SegmenteWaerme` / `.SegmenteStrom`); zwei Wege wären zwei Wahrheiten
über denselben Kreis. Die Aufrufe mit vier Parametern bleiben unverändert — Bericht und
ChartProben zeichnen ihre Ringe wie bisher.

### 8.5 Befund B‑3 — die Zahl weit weg von ihrem Kopf

Das Eigenanteilsraster setzte seine Köpfe LINKSbündig über RECHTSbündige Zahlen in gleich breiten
Spalten: zwischen „Deckung Brauchwasser [MWh/a]" und der 0,00 darunter lagen dreihundert
Bildpunkte. Die neue Erzeugertabelle (`.epos-simueb-tabelle`) stellt Kopf UND Zelle rechts, führt
die Einheit einmal als kleine zweite Kopfzeile, setzt `font-variant-numeric: tabular-nums` und
lässt die Namensspalte den Rest der Breite nehmen.

**Zeilen ohne Beitrag** stehen gedimmt und lassen sich mit „n Zeilen ohne Beitrag ausblenden"
wegklappen; Vorgabe ist EINBLENDEN, der Schalter merkt sich den Stand je Spalte. Sie
verschwinden nicht von selbst — eine angelegte Anlage mit 0,00 ist eine Aussage (#190, Zusatz
„(nicht in der Kaskade)").

### 8.6 Die Zoom-Ausnahme (Entscheid c)

Die Hausregel **W8‑E‑2** („jedes Renderer-Bild steht im Baustein `Diagramm` und ist zoombar")
bekommt ihre eine Ausnahme: **Ring und Kuchen**. `ChartBild Rund="true"` setzt seither
`Diagramm.OhneZoom` — keine Leiste „×1 · 1:1", kein JavaScript-Modul, kein Greifzeiger. Ein Ring
trägt eine Handvoll Segmente statt 8 760 Stützstellen; ein Achsenausschnitt ist dort undenkbar,
und auf ×1,2 schnitt `.epos-diagramm-flaeche { overflow: hidden }` den Kreis an (rechte Grafik
des Fotos). **Der Rahmen bleibt** — die Regel „jedes Bild durch `ChartBild`" gilt unverändert,
und ihr Wächter `ChartBildTests.Jedes_Bild_steht_im_Baustein_Diagramm` ist unberührt.

### 8.7 Das Hinweisband

„n Hinweise zum Lauf" war ein Knopf in voller Breite mit zentriertem Text auf weißem Grund — eine
leere Zeile, die aussah wie eine Schaltfläche für etwas Wichtiges. Jetzt ein Band in der
Warnfarbe des Hauses: links die Zahl als Abzeichen, in der Mitte der Kurztext, rechts
„anzeigen ▾". Der Klick öffnet den Volltext wie bisher.

### 8.8 Was #222 NICHT anfasst

Rechenweg, Kern-Controller, die übrigen Reiterinhalte, `SimulationSeite.razor` (außer dem
Hinweisband), `InfoKnopf`, `Hauptfenster`, `AppWurzel` und die Auslegungsansicht. Der
Referenzlauf 1030 und 1046 bleibt **byte-gleich** gegen R7.

### 8.9 Offen

- **Der Link „① Konfiguration"** im 0‑%-Hinweis ist TEXT, kein Sprung. Den Schritt wechselt die
  `SimulationSeite`, und die war von #222 ausgenommen; die Ablaufleiste mit „① Konfiguration"
  steht unmittelbar über der Ansicht. Ein Rückruf durch die Ergebnisseite wäre nachzurüsten.
- **Eigenverbrauch und Einspeisung je Stromerzeuger** führt kein DTO des Laufs (der Lauf bucht
  sie als Projektsummen). Die Stromtabelle führt deshalb „Erzeugung" und „Anteil" statt der vier
  Spalten des Mockups.
- **Die Restzeile der Wärmetabelle** trägt in den drei Kanalspalten „—": Der Restwärmebedarf ist
  eine Bilanzgröße des Laufs und nicht nach Kanälen aufgeteilt.

---

## 9. SIM‑E‑2 (11.09.2026) — der Startseiten-Reiter „Simulation" rechnet

**Anwenderentscheid SIM‑E‑2, Option 1**, umgesetzt mit **Auftrag #220** (nach #221 und #222).

### 9.1 Befund — eine leere halbe Seite und ein Ansichtswechsel

Der Anwender hat den Reiter an drei Bildschirmfotos zurückgegeben. Gemessen am Stand nach
#216:

* **Links** standen Projektzusammenfassung, der Knopf „Simulation Konfiguration…" und die
  eine Bildkachel „Simulation starten" — zusammen etwa ein Drittel der Breite.
* **Rechts stand nichts.** Zwei Drittel des Reiters waren leer.
* Die **Kachel wechselte die Ansicht**: Sie trug seit SIM‑E‑1 die Marke `schritt=2`, und die
  Ansicht `SIMULATION` rechnete dort. Wer nur rechnen und das Ergebnis ansehen wollte,
  verließ dafür die Startseite — obwohl der Reiter genau dafür gebaut ist.

### 9.2 Zielbild — zwei Spalten, ein Lauf

| | vorher | seit #220 |
|---|---|---|
| Kachel „Simulation starten" | Marke `schritt=2`, **Ansichtswechsel** | **rechnet an Ort und Stelle**, kein Wechsel |
| Fortschritt und Abbrechen | in der Ansicht | **unter der Kachel**, in der linken Spalte |
| Sperrgründe | am Rechenknopf der Ansicht | **an der Kachel** (Statuszeile, grauer Punkt) **und als Hinweis darunter** |
| rechte Spalte | leer | **dieselbe `SimulationErgebnisSeite` wie Schritt ③**, Startblatt „Übersicht" |
| „Ergebnis speichern" | Werkzeugleiste der Ansicht | **im Kopf der rechten Spalte** (dieselbe Bedingung) |
| ohne gerechneten Lauf | — | Hinweis **„Noch kein Ergebnis — Simulation starten."** |

Die Aufteilung ist ein CSS-Raster `minmax(0, 1fr) minmax(0, 2fr)` (`.epos-simreiter`);
unter 1100 px stehen die zwei Spalten untereinander. Die Schwelle ist eine andere als die
900 px von `Zweispaltenauswahl` und `Katalograhmen`: Dort stehen zwei Eingabeblöcke
nebeneinander, hier eine Eingabespalte neben einer ganzen Ergebnisseite mit neun
Reiterblättern.

**Die Ansicht `SIMULATION` bleibt unverändert** — sie trägt die Konfiguration ①, das
Vollbild ③, den Menüpunkt „Projekt → Simulation…" und die Werkzeugleiste aus #216.

### 9.3 Eine Wahrheit: `SimulationLaufsteuerung`

Zwei Wirte beantworten seither dieselben drei Fragen — „warum ist der Lauf gesperrt?",
„darf er starten?" und „wie startet er?". Kopiert wäre das zweimal derselbe
Zustandsautomat. Sie stehen deshalb EINMAL in
`EPOS.UI/Seiten/Simulation/SimulationLaufsteuerung.cs`:

* **`SimulationLaufsteuerung`** — `Sperrgrund` (in der Reihenfolge: fremder Lauf → rote
  Vorprüfung → ungespeicherte Konfiguration), `Frei`, `Laeuft`, `Anteil`,
  `Fortschrittstext`, `AbbruchMoeglich`, `Abbrechen()` und `Starten()`. Sie rechnet nichts:
  Der Lauf gehört unverändert der `SimulationErgebnisSeite` (Fortschritt, Abbruch,
  Neuladen danach). `SimulationSeite` benutzt sie seit #220 für Schritt ②,
  `SimulationReiter` für die Kachel.
* **`SimulationLaufsperre`** — „ein Lauf zur Zeit" (Punkt 5 des Entscheids). Sie lebt in der
  QUELLE (`SimulationAnsichtQuelle`, EINE je Projekt) und geht über
  `SimulationAnsichtDienste.Laufsperre` in JEDEN Parametersatz; Ansicht und Reiter sperren
  sich damit gegenseitig, ohne voneinander zu wissen. Eine Komponente könnte sie nicht
  halten: Es sind zwei Komponenten mit zwei Lebensdauern, und die Wurzel verwirft die eine,
  wenn sie die andere zeigt.

Die `SimulationErgebnisSeite` hat dafür drei Zusätze bekommen, alle mit dem heutigen
Verhalten als Vorgabe: `FortschrittZeigen` (Vorgabe `true` — der Reiter schaltet ihren
eigenen Balken ab und zeichnet ihn unter der Kachel), die Leseeigenschaften `Anteil` /
`Fortschrittstext` / `AbbruchMoeglich` und `LaufAbbrechen()`. **Es bleibt EIN Lauf mit
EINEM Fortschritt** — nur an einer anderen Stelle gezeichnet.

### 9.4 Dienste aus EINER Quelle (Punkt 3)

Der Reiter bekommt seine Dienste über den vorhandenen Weg: `AppWurzel.SimulationGabenHolen()`
(`SimulationGaben?.Invoke() ?? Quelle.SimulationGaben(projekt)`) — derselbe Aufruf, aus dem
sich die Ansicht bedient, gebündelt in EINER privaten Methode. Die `Startseite` bekommt ihn
als `[Parameter] Func<IReadOnlyDictionary<string, object>?>? SimulationGaben` und holt den
Satz beim **Betreten** des Reiters (`BeiSimulationBetreten`), weil er den Stand der zwei
Hüllen mitbringt; `SimulationAnsichtDienste.Aus(gaben)` liest das Bündel heraus, damit der
Schlüsselname an einer Stelle steht.

**Keine zweite Hülle, kein zweiter Datenweg** — und damit rechnet der Reiter auf iOS
genauso: Dort fehlt der Hüllen-Delegat, und `IosProjektQuelle.SimulationGaben` liefert
denselben Satz aus derselben `SimulationAnsichtQuelle`.

### 9.5 Rückwege (Punkt 4): die Marke bekommt eine Wirtkennung

Derselbe Schritt ③ steht seither an ZWEI Stellen. Der Rückwegstapel muss sie
auseinanderhalten: Wer die Stromspeicher-Auslegung aus dem REITER heraus geöffnet hat, will
in den Reiter zurück und nicht in die Ansicht. Die Marke trägt dafür ein drittes Stück:

```
wirt=START;schritt=3;blatt=STROMSPEICHER
```

* `SimulationMarke.WIRT_START`, `WirtLesen(marke)` und die Überladung
  `Schreiben(wirt, schritt, blatt)` — eine Marke OHNE Wirtkennung meint wie bisher die
  Ansicht, und die Startseite lässt sie liegen.
* `Startseite.AktuelleMarke` liefert sie, solange der Reiter „Simulation" vorn steht;
  `AppWurzel.StehendeMarke()` fragt neben der Simulationsansicht jetzt auch die Startseite.
* `Startseite.Marke` wendet sie an, wenn sie sich ÄNDERT (Muster `SimulationSeite`): Reiter
  „Simulation" nach vorn, Blatt als `StartBlatt` an die rechte Spalte.

Der Assistent folgt demselben Gedanken wie #221: Der Reiter **meldet seinen Hilfekontext**
über den `Hilfekontextmelder` („Startseite · Simulation · &lt;Blatt&gt;") und zeichnet unter
Windows KEINE eigene Pille — die eine Pille des Kopfbands trägt seinen Schlüssel. Auf iOS
gibt es kein Kopfband, und der Reiter behält seine.

### 9.6 Der Rückfall

Ohne Parametersatz — kein Projekt offen, ein Prüfstand, eine Plattform ohne Simulation —
gibt es keine rechte Spalte (`.epos-simreiter--allein`), und die Kachel meldet ihren
Schlüssel wie vor #220; die Startseite wechselt dann über `Dienste.Navigation` in die
Ansicht. Dieselbe Hausregel wie überall: kein Delegat, keine Bedienung.

### 9.7 Was #220 NICHT anfasst

Rechenweg, Kern-Controller, die Reiterinhalte des Ergebnisses, die Ansicht `SIMULATION`
selbst (außer der gemeinsamen Laufsteuerung), die Pille aus #221 (außer der Kontextmeldung)
und die Auslegungsansicht. Der Referenzlauf 1030 und 1046 bleibt **byte-gleich** gegen R7.

### 9.8 Darstellung nach Anwenderrückmeldung 12.09.2026 (Auftrag #233)

Der Anwender hat den Reiter nach #220 ein zweites Mal zurückgegeben, diesmal zur
DARSTELLUNG: „das Layout ist nicht gut/stimmt nicht — Größe, Lesbarkeit." Auf dem
Bildschirmfoto standen links **drei Elemente in drei Breiten** — der Zusammenfassungskasten
rund 600 px (mit blauen Werten), darunter der graue Knopf „Simulation Konfiguration…"
355 px, darunter die 190 px schmale Startseiten-KACHEL „Simulation starten" mit
84-px-Sinnbild und dreizeilig umgebrochenem Untertitel —, rechts eine leere Fläche mit
einem „Ergebnis speichern", das bedienbar aussah, und einer einsamen Hinweiszeile.

**Die Ursache ist das Kachelmuster am falschen Ort.** Das feste Raster aus W16b‑E‑7 ist auf
drei Spalten zu 404 px gebaut; in einer schmalen Spalte hat es keine zweite Spalte, an der es
sich ausrichten könnte. Daraus wurde die Hausregel (`EPOS.UI/CLAUDE.md`): **Kachelraster nur
in einem Reiter mit drei Spalten; ein Zweispalten-Reiter bekommt einen Bedienblock.**

| | seit #220 | seit #233 |
|---|---|---|
| linke Spalte | `1fr` — wächst mit dem Fenster | **Bedienblock fester Breite** `minmax(320px, 360px)`, alles darin von Rand zu Rand |
| „Simulation starten" | Bildkachel im Kachelraster | **Hauptknopf** in Blockbreite, 44 px, `epos-knopf--primaer` wie der Rechenknopf der Ansicht (#216), mit ▶ |
| Kacheluntertitel | dreizeilig in der Kachel | **eine leise Zeile** unter dem Knopf |
| Sperrgrund | Statuszeile an der Kachel **und** Warnbanner am Fuß | **eine Zeile unter dem Hauptknopf** und dessen `title` — einmal statt zweimal |
| „Simulation Konfiguration…" | 355 px breiter Knopf mit Sinnbild | derselbe Knopf, **in Blockbreite** (das Sinnbild bleibt, W16b‑E‑3) |
| Werte der Zusammenfassung | Markenton `--epos-marke` (blau) | **`--epos-text`** — blau ist in dieser Oberfläche die Verweisfarbe, und die Zusammenfassung verweist nirgendwohin |
| „Ergebnis speichern" ohne Ergebnis | `disabled`, sah aber bedienbar aus | `disabled` **auf der leisen Hausfläche**, `title` nennt den Grund |
| Leerzustand rechts | eine Textzeile quer durch die Fläche | **ruhige Karte** mittig: kleines Sinnbild, ein Satz |
| Umbruch | unter 1100 px untereinander | **unter 900 px** — dieselbe Schwelle wie `Zweispaltenauswahl` und `Katalograhmen` |

**Was NICHT fällt:** kein Kachelschlüssel und kein Kachelbild. Beschriftung und Erläuterung
des Hauptknopfes kommen weiter aus dem Kachelregister der Hülle (`START_K_DETAILSIM_T` /
`_B` über `StartseiteHuelle`), das Bild `PDetailSim.jpg` trägt jetzt die Leerzustandskarte,
und Hilfeschlüssel, Startfragen und `KiDialogKatalog` bleiben unberührt. Es ändert sich die
BAUFORM, nicht der Weg: derselbe Schlüssel, dieselbe Sperrprüfung, derselbe Lauf, dieselbe
Marke, derselbe Rückfall ohne Dienste. Der Reiter läuft unverändert auch in der iOS-Wurzel;
eine Hüllenänderung war nicht nötig.

Berührt sind `EPOS.UI/Seiten/Start/SimulationReiter.razor` und die Klassen `epos-simreiter*`
in `EPOS.UI/wwwroot/epos-ui.css`; Wachen sind
`EPOS.UI.Tests/Seiten/StartreiterSimulationTests` (Bedienung) und
`…/StartseiteAnmutungTests` (Stilblatt).

## 10. Befund #236 (12.09.2026) — die Übersicht zeichnete ein Nullobjekt

### 10.1 Die Rückmeldung

Bildschirmfoto vom 12.09.2026, Startseiten-Reiter „Simulation", Projekt „Stromspeicher
Optimierung - ein Speicher" (Technologie Stromspeicher, Wärmebedarf 0, Strombedarf aus einer
eingelesenen Stromganglinie). **Links** in der Projektzusammenfassung steht „Strombedarf:
2850,20 MWh/a", **rechts** im Blatt „Übersicht" derselben Ansicht „Strombedarf 0,00 MWh/a",
„Deckung durch Erzeuger 0,0 %", die Marke „kein Stromerzeuger im Projekt" und der Satz „Ohne
Bedarf lässt sich keine Deckung ausweisen."; „Ergebnis speichern" ist gesperrt. Wörtlich: „Der
Strombedarf wird in der Übersicht (Simulation) nicht korrekt dargestellt."

### 10.2 Was nicht die Ursache war

**Der Rechenweg ist in Ordnung.** `SimulationStrombedarf.Berechnung` addiert die Ganglinien aus
`Z_ProjektStromganglinie` auch dann, wenn das Projekt keine Stromverbraucher-Profile führt —
`Stromprofil_Strombedarf_berechnen` liefert dafür eine Nullreihe, nicht `null`. Beleg ist das
Referenzprojekt **1030** der Testdatenbank: eine Ganglinie, kein Verbraucherprofil, und die
Basis R7 führt `Energiebedarf.Strombedarf_Gesamt;4790.09`. Die linke Spalte des Startreiters
rechnet über **genau diese** Methode.

Ebenso unbeteiligt: `SimulationLaufCtrl.Bedarf` (füllt die zwei Bedarfsobjekte an Ort und
Stelle), `SimulationErgebnisCtrl.Uebersicht` (liest `sb.StrombedarfGesamtMwh` daraus) und der
Eigenverbrauchszuschlag aus W8‑O‑5c. Bei einem gültigen Lauf stünde rechts dieselbe Zahl wie
links.

### 10.3 Die Ursache: ein vorbelegtes DTO, das wie ein Ergebnis aussah

`EPOS.UI.Daten/Simulation/SimulationErgebnisHuelle.Zusammentragen` stieg bei ungültigem
Ergebnis aus, **bevor** `d.Kennzahlen` und `d.Uebersicht` gebaut waren:

```csharp
d.Bedarf = BedarfDaten(bedarf);
if (!_ergebnisGueltig) return d;          //  <- hier
d.Kennzahlen = SimulationErgebnisCtrl.Uebersicht(…);
d.Uebersicht = UebersichtDaten(…);
```

`SimulationErgebnisDaten.Uebersicht` war dabei mit `= new UebersichtDaten()` **vorbelegt**, und
`UebersichtReiter.razor` zeichnete diese Vorbelegung wie ein Ergebnis: `StrombedarfMwh` 0,
`StrombedarfVorhanden` false → „Ohne Bedarf …", `StromerzeugerVorhanden` false → die Marke
„kein Stromerzeuger im Projekt", Deckung 0,0 %. Das ist Zeile für Zeile das Bildschirmfoto.

### 10.4 Warum das Ergebnis so leicht ungültig wird

Der Startreiter montiert seine rechte Spalte, sobald `Dienste.ErgebnisVorhanden()` wahr ist —
und das ist `LaufGerechnet`, eine Marke, die **nie** zurückfällt („ein Lauf, der gelaufen ist,
ist gelaufen"). Die Gültigkeit dagegen fiel an **drei** Stellen in
`SimulationErgebnisHuelle.Optimierung.cs`:

| Stelle | Anlass |
|---|---|
| `OptimierungEinstellungenSpeichern` | der Reiter „Stromspeicher" speichert die Betriebsoptionen |
| `OptimierungFlottenRechnen` | eine Flottenstudie wird gerechnet |
| Rückruf aus `AuslegungOeffnen` | die Ansicht `STROMSPEICHER_AUSLEGUNG` meldet eine Änderung zurück |

Jeder Besuch der Stromspeicher-Auslegung, der etwas speichert oder rechnet, machte das
Ergebnis damit „veraltet" — und der Anwender kam genau von dort. Dazu kommen zwei weitere
Zustände mit demselben Bild: **vor dem ersten Lauf** (die Ansicht schaltet den Automatikstart
ab) und **nach einem abgebrochenen Lauf**.

### 10.5 Der Entscheid: Zustand statt Schalter, Bedarfszahlen aus der Bedarfsrechnung

1. **`ErgebnisZustand` statt `bool`.** `SimulationErgebnisDaten` trägt seither
   `Zustand` (`NichtGerechnet` · `Gueltig` · `Veraltet` · `Abgebrochen`) und `Zustandsgrund`
   (den ANLASS in Anwendersprache). `ErgebnisGueltig` bleibt als Ableitung stehen, damit
   `SpeichernMoeglich` und die vorhandenen Prüfstände unberührt sind. Die Hülle setzt den
   Zustand an den Stellen, an denen vorher die Marke fiel — Laufbeginn, jeder Frühausstieg des
   Laufs (`Abbruch(grund)`), Laufende, die drei Setzer der Auslegung.
2. **Kein Nullobjekt mehr.** `SimulationErgebnisDaten.Uebersicht` ist **nullbar** und wird bei
   ungültigem Zustand gar nicht gebaut. `UebersichtReiter` zeichnet dann kein Ergebnis: kein
   Ring, keine Deckung, keine Erzeugertabelle, keine Marke „kein Stromerzeuger", nicht den Satz
   „Ohne Bedarf …".
3. **Die Bedarfszahlen kommen aus der BEDARFSRECHNUNG, nicht aus dem Lauf.** Sie hängen am
   Projekt und stehen in jedem Zustand: Wärmebedarf gesamt und Strombedarf gesamt aus
   `d.Bedarf` — dieselben Zahlen, die die Projektzusammenfassung links nennt. Damit die
   Bedarfsobjekte auch dann gefüllt sind, wenn niemand vorher den Startreiter betreten hat
   (die Ansicht `SIMULATION`, und auf iOS jeder Weg), rechnet die Hülle sie beim ERSTEN Laden
   einmal selbst (`BedarfSicherstellen`, derselbe Weg wie im Lauf, danach nie wieder).
4. **Ein ruhiger Leerzustand mit Grund** an der Stelle des Ergebnisses — Bauform wie die
   Leerzustandskarte aus #233, kein Warnbanner (Regel W16b‑E‑6, dritte Stufe): „Noch nicht
   gerechnet — …", „Das Ergebnis ist veraltet — &lt;Anlass&gt;. Bitte Simulation erneut
   starten." oder „Der Lauf wurde abgebrochen — &lt;Grund&gt;.". Steht derselbe Abbruchgrund
   schon als Warnbanner über dem Reiterstapel, sagt die Karte „…, siehe Meldung oben" statt
   denselben Text ein zweites Mal. Den Satz baut die SEITE, nicht die Hülle — nur sie kennt
   das Banner.
5. **Der Startreiter zeigt den Zustand sichtbar.** Die rechte Spalte darf bei
   `LaufGerechnet && !Gueltig` weiter stehen (so war es gemeint), trägt aber im Kopf eine leise
   Zustandszeile, und „Ergebnis speichern" bleibt gesperrt — sein `title` nennt seither den
   Zustand statt nur „Noch kein Ergebnis". Damit der Wirt den ersten Stand überhaupt erfährt,
   meldet `SimulationErgebnisSeite` nach ihrem ersten Zeichenlauf einmal `StandGeaendert`; ohne
   diese Meldung blieben Knopf und Zeile auf dem Stand „es gibt nichts", bis den Wirt etwas
   anderes neu zeichnet.

**Am Rechenweg ändert sich nichts.** `SimulationStrombedarf`, `SimulationControl` und
`SimulationErgebnisCtrl.Uebersicht` bleiben unangetastet; der Referenzlauf ist byte-gleich zur
Basis `2026-09-11_R7_Speicherflotte` (5/5 Projekte).

### 10.6 Die Reproduktion

Drei Fälle in `EPOS.Kern.Tests/SimulationUebersichtZustandTests` halten den Befund fest — alle
über die Hülle, headless, gegen eine Arbeitskopie der Testdatenbank:

| Fall | vorher | nachher |
|---|---|---|
| Projekt 1030 laden, ohne Lauf | Bedarf 0,00 **und** Übersicht 0,00 | Bedarf **4790,09**, Übersicht `null`, Zustand `NichtGerechnet` |
| danach rechnen | Übersicht 4790,09 | unverändert, Zustand `Gueltig` |
| danach in der Auslegung speichern | Übersicht wieder 0,00, Marke „kein Stromerzeuger" | Übersicht `null`, Zustand `Veraltet` samt Anlass, Bedarf weiter 4790,09 |

Ein vierter Fall baut in der Arbeitskopie das Projekt des Anwenders nach — aus 1030 abgeleitet,
Kaskadenplätze leer, Ganglinie behalten, eine Speicheranlage und ein Flottenstand mit EINER
Einheit: **der Lauf geht durch**, danach nennt die Übersicht 4790,09 MWh/a bei einem
Wärmebedarf von 0. Der Fehler lag also nicht am Rechenweg, sondern an der Anzeige.

Auf der Oberflächenseite prüfen `EPOS.UI.Tests/Seiten/UebersichtReiterTests` den Leerzustand
(darunter die Wache, die ein vorbelegtes `UebersichtDaten` durch die Komponente schickt und
„0,00" an der Stelle des Strombedarfs nicht mehr findet) und
`…/StartreiterSimulationTests` die drei Zustände der rechten Spalte.
