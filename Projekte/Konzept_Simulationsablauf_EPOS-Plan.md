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
[`Umsetzungskonzept_iOS_EPOS-Plan.md`](../Umsetzungskonzept_iOS_EPOS-Plan.md). Es beschreibt
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
| **S2** — iOS erreicht die Simulation | Messung der zwei Hüllen (Datenweg → Kern-Controller, Plattform bleibt), `IosProjektQuelle.SimulationGaben`, Kachel „Simulation" in der Projektliste; iOS-Lauf 43 **gebündelt mit #202** (trifft die Hülle) | iOS-CI: Ansicht baut, Prüfmodus unverändert; Kern-Tests für den verlegten Datenweg |

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

Nicht umgesetzt und ausdrücklich offen: **S2 (iOS)** — `IosProjektQuelle.SimulationGaben` liefert
weiterhin `null`; die Ansicht ist dort erreichbar, sobald das Wörterbuch steht (Auftrag #208).

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
| **SIM‑E‑1** Die Kachel „Simulation" der Startseite: öffnen oder rechnen? | **RECHNEN** — sie heißt „Simulation starten" und löst Schritt ② aus (Anwenderwort, Windows-Abnahme #216) | **entschieden 11.09.2026 (Anwender), umgesetzt #216** |

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
