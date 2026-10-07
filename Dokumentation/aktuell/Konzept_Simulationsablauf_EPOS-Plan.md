# Konzept: Simulationsablauf ohne Dialog — eine Ansicht, ein Rückweg

Stand 11.09.2026, gemessen am Commit `1e71d30` des Zweigs `ios_migration_september` — einer
**alten Kennung** von vor dem Umschreiben der Git-Geschichte am 12.09.2026 (AUF‑Q1, #244);
übersetzt wird sie über `Dokumentation/ueberholt/Geschichte/commit-map_2026-09-12.txt`. Anlass ist die
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
│ ① Konfiguration                                                               │
│  Komponenten der Simulation        │  Speicher im Projekt                     │
│                                    │  [Pufferspeicher anlegen / verwalten…]   │
│                                    │  [Stromspeicher auslegen…]               │  → Ansicht STROMSPEICHER_AUSLEGUNG
│                                    │  Mehrspeicherbetrieb aktiviert · …       │     „← zurück" → hierher, Schritt ①
├──────────────────────────────────────────────────────────────────────────────┤
│ ③ Ergebnis                                                                    │
│  Übersicht | Bedarf | Wärmepumpe | … | Stromspeicher                          │  ← die neun Reiter
│  Gerechnet mit Speicherflotte: Lastspitzenkappung · 2 Einheiten  [Konfiguration ändern → ①]
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
  Verschachtelte Ablaufleisten wären eine zweite Sorte „Fenster im Fenster". Der Weg hinein ist
  der Knopf **„Stromspeicher auslegen…"** in Schritt ①, Spalte „Speicher im Projekt" (Abschnitt 11);
  ihr Rückweg führt in die Simulationsansicht, Schritt ①. Der Nachzug markiert das Ergebnis als
  veraltet, und der Reiter „Stromspeicher" zeigt das Banner „Flotte geändert — Simulation neu
  starten".
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

---

## 11. #274 (14.09.2026) — der Einstieg in die Auslegung wandert nach ①

### 11.1 Die Rückmeldung

Der Anwender schickte zwei Bildschirmfotos und einen Satz: **„Der Dialog Stromspeicher soll in
den Dialog Konfiguration verschoben werden. Ähnlich zu ‚Pufferspeicher anlegen/verwalten' einen
Konfigurationsbutton ‚Stromspeicher auslegen' (anstelle Aufruf aus ‚Speicherflotte und
Auslegung')."**

Das erste Bild zeigte den Ergebnisreiter **Stromspeicher** mit dem Block *Speicherflotte und
Auslegung*: Mehrspeicherbetrieb, der Editor „Betrieb und Leistungsverteilung" (Betriebsziel,
Erzeuger-Reihenfolge, Peak-Ziel, Startwert, Netzladung, Export), die Speichertabelle und der
Knopf *Speicherflotte & Auslegung öffnen*. Das zweite zeigte **① Konfiguration**, Spalte
*Speicher im Projekt*, mit dem Knopf *Pufferspeicher anlegen / verwalten…*.

### 11.2 Der Befund dahinter

Der Block war eine **Eingabemaske mitten im Ergebnis**. Wer die Betriebsführung einstellen
wollte, musste erst rechnen — und sah danach Zahlen, die noch zum vorigen Stand gehörten. Die
Betriebsführung ist aber eine **Eingabe**: Sie entscheidet, WAS gerechnet wird, genau wie die
Kaskade und die Pufferzuordnung daneben in ①. Dazu kam eine zweite Pflegestelle: Betriebsziel,
Verteilung und Peak-Ziel standen im Reiter UND in Station 3 der Auslegungsansicht.

### 11.3 Der Entscheid

| | vorher | jetzt |
|---|---|---|
| Einstieg in die Auslegung | Knopf im Ergebnisreiter „Stromspeicher" | Knopf **„Stromspeicher auslegen…"** in ①, unter „Pufferspeicher anlegen / verwalten…" |
| Betriebsführung pflegen | Editor im Reiter **und** Station 3 | allein Station 3 der Auslegungsansicht |
| Reiter „Stromspeicher" | Eingabe und Ergebnis gemischt | reines Ergebnis plus **Herkunftszeile** |
| Rückweg aus der Auslegung | ③, Blatt „Stromspeicher" | ①, Konfiguration |

- **Der Knopf steht nur, wenn es etwas auszulegen gibt** — das Projekt führt einen Stromspeicher
  oder einen Flottenstand. Sonst steht dort dieselbe Art Erklärzeile wie beim fehlenden
  Pufferspeicher. Darunter eine **Kurzzeile zum Stand**: Mehrspeicherbetrieb aktiviert bzw.
  deaktiviert, Betriebsziel, Einheitenzahl.
- **Die Herkunftszeile** im Reiter sagt, WOMIT der angezeigte Lauf gerechnet wurde —
  Betriebsziel, Einheitenzahl und, wo es eines gibt, das Peak-Ziel samt seinem Modus
  („adaptiv (kausal)" bzw. „fest", Abschnitt 5.1.1 der Mehrspeicher-Spezifikation). Daneben der
  Verweis **„Konfiguration ändern → ①"**. Anzeige, kein Editor.
- **Zwei Wirte, ein Verweis:** In der Ansicht SIMULATION blättert er auf Schritt ①, aus dem
  Startseiten-Reiter „Simulation" heraus öffnet er die Ansicht dort. Beides über denselben
  Maskenschlüssel `SIMULATION_KONFIGURATION`, den die Wurzel seit #207 als Einstiegsmarke führt.

### 11.4 Wie es gebaut ist

- **Der Weg gehört der Ergebnishülle, der Knopf der Konfiguration.** Die Auslegung zieht ihre
  EPOS-Zeitreihen aus einem abgeschlossenen Lauf, und den hält `SimulationErgebnisHuelle`;
  `SimulationAnsichtQuelle` — die Stelle, die beide Hüllen kennt — legt deren `AuslegungOeffnen`
  in `SimulationKonfigDienste` ein. Ohne eingelegten Weg gibt es den Knopf nicht (Hausregel
  „kein Delegat, kein Knopf"); auf iOS ist er da, weil beide Plattformen dieselbe Quelle nehmen.
- **Der Kurzstand wird einmal je Besuch gelesen.** `Laden` läuft nach jeder Bedienung der Seite,
  der Flottenstand ändert sich dabei nicht — geschrieben wird er allein in der Auslegungsansicht.
  Der Merker fällt genau dann, wenn der Einstieg benutzt wird, und beim Projektwechsel; der
  Rückweg liest die Speicherspalte damit frisch.
- **Keine toten Gaben.** Aus `SimulationErgebnisDienste` fallen `OptimierungVorgaben`,
  `OptimierungFlottenRechnen`, `OptimierungEinstellungenSpeichern` und `AuslegungOeffnen`; neu
  ist `KonfigurationOeffnen`. Geblieben ist `OptimierungCsv` — die Flottenansicht des Reiters
  schreibt ihre CSV weiter.
- **Nebenbefund:** Kacheln, Betriebsbild und die 39 Kennzahlen hingen zusätzlich an der Frage,
  ob die Plattform eine Flotte rechnen KANN. Unter Windows und iOS ist das immer der Fall — der
  Block war dort unerreichbar. Er hängt jetzt am Ergebnis: Gibt es ein Flottenergebnis, zeigt
  die Flottenansicht die Zahlen je Speicher; gibt es keines, sind das die Zahlen des Laufs.

### 11.5 Die Prüfmuster

| Wo | Was |
|---|---|
| `EPOS.UI.Tests/Seiten/SimulationKonfigSeiteTests` | Knopf mit Standzeile unter der Pufferverwaltung, er ruft den Öffner; ohne Stromspeicher die Erklärzeile; ohne Weg kein Knopf |
| `EPOS.UI.Tests/Seiten/StromspeicherReiterTests` | Herkunftszeile statt Editor, Peak-Modus in der Zeile, Verweis meldet seinen Klick, kein Einstieg mehr |
| `EPOS.UI.Tests/Seiten/SimulationErgebnisSeiteTests` | der Reiter führt über den Verweis in die Konfiguration |
| `EPOS.UI.Tests/Seiten/AppWurzelTests` | der Rückweg aus der Auslegung landet in ① (Marke `schritt=1`) |
| `EPOS.Kern.Tests/SimulationUebersichtZustandTests` | der Zustand „veraltet" entsteht über den neuen Weg: aus ① öffnen, in der Auslegung speichern |

**Am Rechenweg ändert sich nichts** — kein Referenzlauf nötig.

## 12. Heizkessel: Bereitschaftsverlust, Betriebsbereitschaft und Elektrokessel (#568)

Dieser Abschnitt beschreibt den gültigen Rechenweg der Kesselstufe, soweit er Brennstoffeinsatz,
Bereitschaft und Emission betrifft; er ergänzt den Ablauf oben um den Rechenweg, weil die Vorgaben
„Betriebsbereitschaft“ und „Heizgrenze der Kesselbereitschaft“ in ① in der Konfiguration des
Heizkessels stehen (Abschnitt 7.3).

**Stundenbilanz je Kessel** (`SimulationSPK.Stunde_Abschluss`, einmal je Stunde und Kessel nach
Bedarfsdeckung, Ladephase und Nachentladung):

| Zustand der Stunde | Brennstoffeinsatz |
|---|---|
| läuft (Abgabe ab dem Zahlenrand von 10⁻⁹ kWh, `SimulationSPK.KesselLaeuft`: Bedarfsdeckung, Speicherladung oder Anhub aus dem Quellpuffer; ein Rest darunter aus einer Vorstufe zählt nicht als Lauf) | Nutzwärme ÷ Wirkungsgrad (Öl oder Gas) |
| steht still, ist aber **betriebsbereit** | Bereitschaftsleistung [kW] × 1 h (`Tab_Heizkessel.Betriebsbereitschaftverlust` in der Einheit `Bereitschaft_Einheit`, siehe unten) |
| steht still und ist abgeschaltet | 0 |

**Die Bereitschaftsleistung in ihrer Einheit.** Katalog und Projektkopie führen den Wert
`Betriebsbereitschaftverlust` und seine Einheit `Bereitschaft_Einheit` (Schemaschritt 162,
`KesselBereitschaftEinheitSchema`): `kW` — Vorgabe jeder Bestandszeile und die Einheit des Imports
aus VDI 3805 — oder `%` der Nennleistung. Der Lauf rechnet in kW; die Umrechnung steht einmal in
`KesselBereitschaft.LeistungKw` (bei `%`: Wert × `Ptherm` / 100, ohne Nennleistung 0), gerufen beim
Einlesen des Kessels (`SimulationSPK.BereitschaftsleistungKw`). Die Prüfgrenzen je Einheit
(`KesselBereitschaft.Verstoss`: kW 0 … Nennleistung, % 0 … 100) halten Katalogeditor und
Katalogbrowser beim Speichern. Ein Import überschreibt mit seinem kW-Wert auch die Einheit.

**Betriebsbereit** ist ein stillstehender Kessel (`SimulationSPK.IstBetriebsbereit`), wenn

1. der Tag der Stunde ein **Heiztag** ist — das Mittel der Außentemperatur des Laufs über die 24
   Stunden des Tages liegt **unter der Heizgrenze** (`HeiztageAus`; genau auf der Grenze ist kein
   Heiztag, der Vergleich trägt den Rechenrand; Tage statt Stunden, weil ein Kessel zwischen einer
   kalten Nacht und einem warmen Mittag nicht abkühlt) — oder
2. er in den **24 Stunden** davor gelaufen ist (Nachlauf; ein Kessel, der im Sommer Warmwasser oder
   Prozesswärme bereitet, wird zwischen seinen Laufstunden warm gehalten).

**Die Heizgrenze** steht je Projekt in `Tab_Einstellungen.Kessel_Heizgrenze` [°C]; leer gilt die
Vorgabe **15 °C** (`SimulationSPK.HEIZGRENZE_VORGABE_C`). Die Oberfläche nimmt 0 bis 30 °C an. Die
Außentemperatur ist dieselbe Stundenreihe, mit der der Lauf rechnet (Klimaregion des Projekts in
Ortszeit); die Regel gilt für jedes Gebäudemodell und für Projekte ohne Gebäude. Ohne
Temperaturreihe gilt jeder Tag als Heiztag. Das Laufprotokoll nennt die wirksame Heizgrenze und
die Zahl der Heiztage.

**Die Vorgabe `Tab_Einstellungen.Kessel_Betriebsbereitschaft` [h/a]** zählt die Stunden, in denen ein
Kessel warm gehalten wird, Laufstunden eingeschlossen. Größer 0 deckelt sie die Bereitschaftsstunden
je Kessel auf `Vorgabe − Laufstunden` (nicht unter 0); den Überhang nimmt der Lauf samt Verbrauch
zurück und meldet ihn im Laufprotokoll. 0 heißt „kein Deckel“, dann gilt allein die Stundenregel.

**Laufprotokoll und Ergebnis.** Je Kessel nennt das Laufprotokoll Laufstunden, Starts (Laufphasen im
Stundenraster), betriebsbereite Stillstandsstunden und den Bereitschaftsverlust [kWh/a]. Der
Heizkessel-Reiter zeigt dieselben Zahlen als Gruppe „Betrieb“ (Summe über die Kessel), ihr Hinweis
nennt die wirksame Heizgrenze; der
Jahresnutzungsgrad ist die Nutzwärme geteilt durch den gesamten Brennstoffeinsatz (Laufstunden und Bereitschaft).

**Teillastkennlinie** ([Konzept Kesselkennlinie](../ueberholt/Konzept_Kessel_Kennlinie_EPOS-Plan.md) 4.1). Ein
laufender Brennstoffkessel rechnet je Stunde mit dem Wirkungsgrad seiner Laststufe β = brennstoffbasierte
Wärme / Nennleistung: zwischen β = 0,3 und 1 linear von η₃₀ (`Wirkungsgrad_Teillast30`) nach η₁₀₀
(`Wirkungsgrad_Gas`/`_Öl`), darunter η₃₀ (`Kesselkennlinie.Eta`, gerufen in `SimulationSPK.Stunde_Abschluss`).
Ein leeres η₃₀ nimmt die Normvorgabe nach Bauart: Brennwertkessel (`Brennwert` = 1) η₁₀₀ + 0,06, höchstens
Hs/Hi des Brennstoffs; Standardkessel (Beschreibung „Standard…“) η₁₀₀ − 0,03; sonst η₁₀₀ — der
Niedertemperaturkessel rechnet damit Stunde für Stunde wie mit festem Wirkungsgrad. Die Kurve ist stetig und
hat keine Betriebsschwelle. Der Reiter zeigt in der Gruppe „Betrieb“ den mittleren Wirkungsgrad im Betrieb
(Wärme der Laufstunden durch ihren Brennstoff) und den Brennstoff aus Teillast gegenüber Nennlast, die
Kesseltabelle je Kessel η₃₀ (mit „(Vorgabe)“), den Wirkungsgrad im Betrieb und die mittlere Laststufe; der
CSV-Export führt je Brennstoffkessel die Stundenreihe des Wirkungsgrads, das Laufprotokoll η₁₀₀ und η₃₀ samt
Herkunft. Das Kennzeichen `Brennwert` der Projektkopie folgt dem Katalogsatz (Schemaschritt 158,
`KesselBrennwertNachzug`).

**Brennwertkennlinie** ([Konzept Kesselkennlinie](../ueberholt/Konzept_Kessel_Kennlinie_EPOS-Plan.md) 4.1 Punkte 3 bis 5). Ein
Brennwertkessel mit `Kennlinie_Brennwert` = 1 rechnet je Laufstunde η_eff = η_tr(β) + Δ₃₀ · g(T_RL): die trockene
Kurve aus η₃₀ − Δ₃₀, der Kondensationsanteil g = (T_Tau − T_RL)/(T_Tau − 30) auf [0; 1,2], höchstens Hs/Hi
(`Kesselkennlinie.EtaBrennwert`; Taupunkt und Δ₃₀ je Brennstoffgruppe: Gas 57 °C/0,08, Heizöl 47 °C/0,04, Holz und
Pellets 50 °C/0,05). Der Rücklauf der Stunde kommt aus dem Heizkreis der Anlagenkopplung
(`HeizkreisProjekt.RuecklaufC`, NaN fällt durch), sonst aus dem ersten Pufferspeicher der Senkenliste (`RL_eff`,
geschichtet die unterste Schicht, einmal je Stunde in `Stunde_Start` gelesen), sonst aus dem gepflegten Paar an
Anlage und Kessel, sonst 50 °C. „Brennwertbetrieb“ ist eine Stunde mit Rücklauf unter dem Taupunkt
(`Rechenrand.SchwelleErreicht`). Der Reiter zeigt mittleren Rücklauf, Anteil der Stunden und der Wärme im
Brennwertbetrieb und den Brennwertbrennstoff; das Laufprotokoll nennt die Stufen der Rücklaufkette und meldet
einen Kessel, dessen Rücklauf in mindestens der Hälfte der Betriebsstunden über dem Taupunkt lag.

**Takten** (Konzept Kesselkennlinie 4.2). Liegt die brennstoffbasierte Wärme Q eines Brennstoffkessels in einer
Laufstunde unter seiner Mindestleistung P_min (`Kesselkennlinie.Taktet`, beide Enden am Zahlenrand), zählt die Stunde
min(⌊60/t⌋, ⌈Q/(P_min · t/60)⌉) Starts — so viele Mindestläufe der Mindestlaufzeit t, wie die Wärme braucht
(`Kesselkennlinie.StartsImTakt`, Zahlenrand an den Vielfachen eines Mindestlaufs); jede andere Laufstunde zählt einen
Start, wenn der Kessel in der Vorstunde stand. Jeder Start kostet den Anfahrverlust als Brennstoff; er steht im
Kesselverbrauch, im Jahresnutzungsgrad, in den Emissionen und in der Gasspitze der Stunde, nicht im mittleren
Wirkungsgrad im Betrieb. Leere Felder nehmen die Normvorgaben: Mindestleistung 30 % der Nennleistung beim
Gas-Brennwertkessel, sonst 60 %, Anfahrverlust 0,002 h × Nennleistung, Mindestlaufzeit 10 min. Der Elektrokessel
taktet nicht; seine Starts sind seine Laufphasen. Der Reiter zeigt Taktstunden, Anfahrverlust und je Kessel die
Starts; das Laufprotokoll nennt die Taktwerte samt Herkunft (gepflegt oder Normvorgabe) und Starts, Laufphasen und
Taktstunden des Jahres.

**Elektrokessel** (`Tab_Heizkessel.Brennstoff` = 13). Sein Strom steht über den Stromverbrauch der
Stufe im Reststrombedarf und damit im Netzbezug, den Kostenrechnung und Emissionsbilanz bewerten.
Seine Modulzeile führt deshalb keinen Brennstoffverbrauch, und er trägt **keine Kesselemission**
(`Em.Kessel.*` = 0) — derselbe Strom wird einmal gezählt, als Netzbezug. Hilfsenergie trägt der
Elektrokessel nicht. Der Stromeinsatz ist seine Nutzwärme (Nutzungsgrad 1); ein Bereitschaftsverlust
senkt nur seinen Jahresnutzungsgrad und steht weder im Netzbezug noch in einer Emission.

**Die Tafel und das Bild des Heizkessel-Reiters** teilen den Stufeneingang gleich auf: Kesselwärme
(Direktdeckung plus die dem Kessel zugerechnete Speicherentladung), aus Puffer (Entladung der Ladung
anderer Erzeuger), übrige Erzeuger / ungedeckt (`SimulationErgebnisCtrl.KesselbildReihen`). Die Zeile
„Restwärmebedarf nach Kessel“ ist Stufeneingang minus Kesselwärme, „davon aus Puffer (andere
Erzeuger)“ ihr Anteil aus fremder Ladung. „Maximale Brennstoffleistung Gas (Hu)“ ist je Gaskessel der
höchste Stundenwert des Brennstoffs (Wärmeabgabe ÷ Wirkungsgrad plus Anfahrverlust der Starts), bei mehreren Kesseln
die Summe dieser Höchstwerte. Ein Wirkungsgrad von genau 1,0 bei einem Brennstoffkessel ist ein Platzhalter; der Reiter
meldet ihn mit „Katalogwert pflegen“.

Gehalten von `EPOS.Kern.Tests/KesselBereitschaftTests`, `EPOS.Kern.Tests/KesselBereitschaftEinheitTests`,
`EPOS.Kern.Tests/KesselKennlinieTests`,
`EPOS.Kern.Tests/KesselBrennwertNachzugTests` und der Referenzbasis `2026-10-03_R34_Erdreich` (Größen `Kessel[i].*` in `aggregate.csv`).

## 13. Kaskade: Vorwahl in der Folge der Ladeprioritäten

Die vier Wärmeplätze `Tab_Einstellungen.Tool_1` bis `Tool_4` ordnen die Direktdeckung einer Stunde;
welcher Erzeuger einen Pufferspeicher zuerst lädt, entscheidet die Ladepriorität der Wärmesenke
(Vorgaben Solarthermie 10, Wärmepumpe 20, BHKW 30, Heizkessel 40, `Ladeordnung.VorgabeLadeprio`).

**Vorwahl.** Solange eine Kaskade nicht von Hand gepflegt ist (`Tab_Einstellungen.Kaskade_Gepflegt`
= 0), wählt die Simulationskonfiguration beim Öffnen die verbauten Wärmeerzeuger des Projekts vor
(`SimulationKonfigHuelle.VerbauteAnlagenVorwaehlen`) — in der Folge der Vorgabe-Ladeprioritäten
Solarthermie, Wärmepumpe, BHKW, Heizkessel (`ErzeugerKatalog.WAERMEERZEUGER`). Jeder noch fehlende
Erzeuger kommt über `Kaskade.Vorwaehlen` vor den ersten belegten Platz, dessen Erzeuger eine
schlechtere Vorgabe-Ladepriorität hat; die Plätze ab dort rücken in den nächsten freien nach. Hat
keiner eine schlechtere, gilt `Kaskade.Aufnehmen` (erster freier Platz hinter dem letzten belegten).
Die schon belegten Plätze behalten ihre Reihenfolge untereinander. Ein Heizkessel, den
`KonfigurationCtrl.HeizkesselNachziehen` in eine gespeicherte, ungepflegte Kaskade an das Ende gesetzt
hat, steht damit auch nach der Vorwahl einer Wärmepumpe hinter ihr. Geschrieben wird die Vorwahl
erst mit „Konfiguration speichern“.

**Wahl des Anwenders.** Die Erzeugerkarten tragen ihren Rang; die Pfeile der Karte ordnen um
(`Kaskade.Verschieben`), „+ aufnehmen“ hängt hinten an (`Kaskade.Aufnehmen`), „×“ nimmt heraus. Jeder
dieser Handgriffe setzt `Kaskade_Gepflegt`; eine gepflegte Kaskade wird weder vorgewählt noch
nachgezogen. Gespeicherte Kaskaden — auch die der Referenzprojekte — rechnet der Lauf unverändert.

Gehalten von `EPOS.Kern.Tests/KaskadeTests` (`Vorwaehlen_*`) und
`EPOS.Kern.Tests/KuehlbetriebProgrammeinstellungTests.Die_Vorwahl_folgt_den_Ladeprioritaeten`.

## 14. Solarthermie-Ganglinie als Rechenweg

Die Solarthermie eines Projekts rechnet entweder über das **Kollektorfeld** (Klimadaten,
Kollektorkennwerte, Ausrichtung — die Vorgabe) oder über eine zugeordnete **Solarthermieganglinie**
mit 8 760 Stundenwerten (`Allgemein/Simulation/SolarganglinieWeiche.cs`, Rechenweg in
`SimulationSolarthermie.GanglinieEinsetzen`).

**Weiche.** Die Auswahl „Profil“/„Ganglinie“ der Startseiten-Kachel wählt nur den Dialog; sie wird
nicht gespeichert. Maßgeblich ist der Datenstand: Die Weiche steht auf Ganglinie genau dann, wenn dem
Projekt über `Z_ProjektSolarganglinie` eine Ganglinie zugeordnet ist, deren Projektkopie
(`Tab_SolarganglinieDaten`, gelesen nach `ID`) **genau 8 760 endliche, nicht negative Werte** führt.
Sonst rechnet das Kollektorfeld. Bei mehreren Zuordnungen rechnet die mit der kleinsten
Zuordnungs-ID; die übrigen werden als Warnung gemeldet. Einen Schemaschritt braucht die Weiche nicht.

**Einheit.** Ein Wert ist die Wärmeleistung der Stunde in kW und damit die Wärmemenge der Stunde in
kWh — absolut, ohne Bezug auf eine Fläche. Die Ganglinie ist das stündliche Potenzial EINES Felds;
was davon den Bedarf deckt, den Puffer lädt oder als Überschuss verfällt, entscheidet die Stunde.

**Senken, Puffer, Kaskade.** Führt das Projekt eine Solarthermie-Anlagenzeile, ist die mit der
kleinsten `Tab_Energieanlagen.ID` der Träger der Ganglinie: Die Ganglinie rechnet unter ihrer ID —
mit ihren Senken (`Z_AnlageSenke`, sonst der Vorbelegung Heizkreis/Beides), ihrer Pufferladung samt
Nachrang-Schwelle und an ihrem Kaskadenplatz. Weitere Kollektorfelder rechnen dann nicht (Hinweis im
Protokoll). Ohne Anlagenzeile deckt die Ganglinie alle Wärmekanäle — Heizung, Brauchwasser,
Prozesswärme — direkt und ohne Puffer (Hinweis im Protokoll). In beiden Fällen rechnet sie nur, wenn
die Solarthermie einen Kaskadenplatz hat; die Vorwahl der Simulationskonfiguration zählt eine
vollständige Ganglinie wie ein Kollektorfeld, und ohne Platz meldet der Lauf
`SIM_W_SOLARGANGLINIE_OHNE_KASKADENPLATZ` (`SimulationLaufCtrl.ErzeugerOhneKaskadenplatz`).

**Rückfälle.** Eine zugeordnete, aber unvollständige Ganglinie (zu wenige oder zu viele Werte, leere,
negative oder nicht endliche Werte) ist eine Warnung mit dem benannten Mangel; der Lauf rechnet mit
dem Kollektorfeld, ohne Kollektorfeld liefert die Solarthermie nichts. Der Statuspunkt der Kachel
(`KomponentenBestandCtrl`) ist ohne Anlagenzeile nur mit vollständiger Ganglinie an.

**Ergebnis und Bericht.** Die Ganglinie erscheint in `Kollektor_Ergebnisse` und damit in
`Tab_ErgebnisSolarthermieModul`, im Ergebnisreiter und in den Erzeugertabellen des Berichts als eine
Zeile „Solarthermie-Ganglinie ‚Bezeichner‘“ mit Jahresertrag (genutzt plus Überschuss), genutzter
Wärme und Überschuss; Fläche und Anzahl stehen auf 0 und werden im Ergebnisreiter als „–“ gezeigt
(`SolarKollektorErgebnis.IstGanglinie`, `JahresertragKwh`, `NutzanteilProzent`). Die
Wirtschaftlichkeit kennt für die Ganglinie keine eigene Investition; ihre Wärmemenge steht in der
Wärmemenge der Komponente Solarthermie. Eine Kostenposition, die an eine einzelne Anlage gebunden ist,
findet die Ganglinienzeile nicht unter dem Anlagennamen und behält ihre gespeicherte Menge.

Gehalten von `EPOS.Kern.Tests/SolarganglinieRechenwegTests` und
`EPOS.UI.Tests/Seiten/ErzeugerReiterTests.Solarthermie_Ganglinienzeile_zeigt_keine_Flaeche_und_keine_Anzahl`.
Kein Referenzprojekt führt eine Solarthermieganglinie.

## 15. Prozesswärme: Temperaturniveau und Betriebsweisen

Ein Prozesswärmesatz trägt neben Monatswerten und Wochenprofil ein **Temperaturpaar**: `Vorlauf`
und `Ruecklauf` [°C] an `Tab_Prozesswaerme_STAMM` und an der Projektkopie `Tab_Prozesswaerme`
(Schemaschritt `ProzesswaermeTemperaturSchema`; REAL, nullbar, 0 … 250 °C, beide oder keiner,
Vorlauf nicht unter dem Rücklauf). Leer heißt „ohne Temperaturniveau“ — der Prozess ist dann
eine reine Wärmemenge, und der Lauf rechnet Zeichen für Zeichen wie ohne die Spalten.

**Das Niveau des Kanals.** Die Profilroutine meldet jedes gerechnete Profil mit Kopfsatz und
Jahresreihe (`ProfilLaufInfo.JeProfil`); `Prozesstemperatur` bildet daraus je Stunde

- den **höchsten geforderten Vorlauf** der Prozesse, die in der Stunde Wärme verlangen und ein
  Paar tragen, und
- ihren **Rücklauf, mengengewichtet** über dieselben Prozesse.

In Stunden ohne einen solchen Prozess steht NaN; ohne ein einziges Paar entsteht kein Niveau
(`SimulationWaermebedarf.ProzessTemperatur` bleibt `null`). Lastgänge mit Kanal Prozesswärme
tragen kein Temperaturniveau.

**Wirkung im Lauf** — nur in Stunden mit Prozessbedarf und gefordertem Vorlauf:

| | Erzeuger bzw. Speicher | Regel |
|---|---|---|
| a | Wärmepumpe mit Direktsenke Prozesswärme | Liegt der Prozessvorlauf über der Kennlinie der Stunde, rechnet die Stunde mit der **untersten Kennlinie, die ihn erreicht** (`ProzessKennlinieWaehlen`) — für die ganze Abgabe der Stunde, die höchste geforderte Temperatur bestimmt den Betriebspunkt. Über der obersten Stützstelle gilt die Extrapolationsregel des Projekts (erlaubt: oberste Kennlinie; verboten: nicht erreicht). Unter der untersten Quelltemperatur dieser Kennlinie gibt es keinen Betriebspunkt — nicht erreicht, weder Extrapolation noch Abbruch. |
| b | jeder Erzeuger mit Direktsenke Prozesswärme | Erreicht er den Prozessvorlauf nicht, ist der Prozesskanal für ihn in dieser Stunde gesperrt (Muster der Heizkanalsperre im Kühlbetrieb). Die Wärmepumpe misst am Kennfeld (a), der Heizkessel an seinem **gepflegten** Vorlauf (Kette Anlage → Heizkessel; ohne Paar keine Sperre). Das BHKW deckt Prozesswärme nur über einen Puffer. Das Warnkriterium W3 meldet einen Erzeuger mit Direktsenke Prozesswärme, dessen gepflegter Vorlauf unter dem höchsten Prozessvorlauf des Projekts liegt (Wärmepumpe ausgenommen). |
| c | Brennwertkessel mit Kennlinie | Der Anteil seiner Stundenabgabe, der in den Prozesskanal ging, sieht den Prozessrücklauf: `T_RL = a · T_RL,Prozess + (1 − a) · T_RL,Kette`, `a` = Prozessabgabe ÷ Abgabe der Stunde. |
| d | Pufferspeicher, Entnahme in den Prozesskanal | Geschichtet: Die Mindest-Nutztemperatur des Prozesskanals steigt für diese Entnahme auf den Prozessvorlauf — entnommen wird nur aus Schichten, die ihn halten. Ungeschichtet mit gepflegtem Paar: Hält `VL_eff` den Prozessvorlauf nicht, entnimmt der Prozess nichts; ohne gepflegtes Paar keine Sperre. |

Das Laufprotokoll nennt je Erzeuger und Speicher die Stunden mit Kennlinie am Prozessvorlauf, ohne
Prozessdeckung, mit Prozessrücklauf und mit begrenzter Entnahme. Der Bericht führt in der
Bedarfstafel die Zeile „Temperaturniveau Prozesswärme“ (höchster Vorlauf / tiefster Rücklauf), nur
wenn ein Prozess ein Paar trägt.

**Stufe 2, nicht gebaut.** Die Wärmepumpe teilt eine Stunde nicht zeitlich in Prozess- und
Heizanteil, sie rechnet die ganze Stunde am höchsten geforderten Vorlauf; auch ihre Pufferladung
in einer solchen Stunde. Die Solarthermie wertet den Prozessvorlauf nicht aus (eigene
Arbeitstemperatur, Abschnitt 16). Eine zweikanalige Ganglinie mit
Vorlauftemperatur je Stunde (PW1 Stufe 2) gibt es nicht.

**Bedienung.** Der Stammkopf der Prozesswärme führt Vorlauf und Rücklauf (Prüfung
`Prozesstemperatur.Paarpruefung`, dieselben Grenzen wie die Prüfklauseln); die Projektkopie
übernimmt das Paar des Katalogs. Im Projektdialog zeigt der Infoblock das Temperaturniveau, und
„Temperaturen übernehmen“ setzt das Paar der gewählten Projektzeile in den Arbeitsstand —
geschrieben wird mit OK (`WizardCtrl.Add_Projekt_Prozess`, nur bei geänderter Zeile).

**Katalog typischer Betriebsweisen.** Derselbe Schemaschritt sät acht Sätze in
`Tab_Prozesswaerme_STAMM` und `Tab_Prozesstyp_STAMM` (`ProzesstypSaat`, `ReadOnly = 1`, wiederholbar,
nie überschreibend; ein eigener gleichnamiger Satz bleibt): Jahresmenge 100 MWh, Monatswerte nach
Monatsfaktor × Kalendertagen, Wochenprofil als relative Last, Temperaturpaar als Vorbelegung,
Beschreibung mit dem Vermerk „Schichtmodell, keine Messung“.

| Satz | Wochenprofil | Monatsfaktoren | Vorlauf/Rücklauf |
|---|---|---|---|
| Einschicht 5 Tage | Mo–Fr 6–14 Uhr 1,0, 5 Uhr 0,5 | August 0,4, Dezember 0,8 | 60/40 °C |
| Zweischicht 5 Tage | Mo–Fr 6–22 Uhr 1,0 | August 0,4, Dezember 0,8 | 70/50 °C |
| Dreischicht 5 Tage | Mo 6 Uhr bis Sa 6 Uhr 1,0 | August 0,4, Dezember 0,8 | 80/60 °C |
| Durchlaufbetrieb 7 Tage | täglich 6–22 Uhr 1,0, 22–6 Uhr 0,9 | August 0,7 (Revision) | 90/70 °C |
| Reinigung/Spülen (CIP) | Mo–Fr 14 und 22 Uhr je 1,0 | 1,0 | 75/40 °C |
| Trocknung/Lackierung | Mo–Fr 6 Uhr 1,5, 7–22 Uhr 1,0 | 1,0 | 120/90 °C |
| Waschen/Bäder | Mo–Fr 6–18 Uhr 1,0, Montag 6 Uhr 2,0 | 1,0 | 60/45 °C |
| Raumlufttechnik Halle | Mo–Fr 5–20 Uhr 1,0 | Oktober–April 1,0, Mai–September 0,1 | 50/30 °C |

Kein Referenzprojekt ordnet einen dieser Sätze zu und keines trägt ein Temperaturpaar; die Basis
bleibt unberührt. Gehalten von `EPOS.Kern.Tests/ProzesswaermeTemperaturSchemaTests`,
`ProzesstemperaturRechenwegTests` (Läufe auf Kopien von 1041 und 1050), `ProzesswaermeTemperaturWegeTests`,
`ProzesstypSaatWacheTests` und `EPOS.UI.Tests/Dialoge/ProzessTemperaturDialogTests`.

## 16. Solarthermie: Arbeitstemperatur, Diffus-IAM, Solarkreis

Das Kollektorfeld rechnet je Stunde die Leistung je Quadratmeter Bezugsfläche nach EN ISO 9806
(`Allgemein/Simulation/Solarkreis.cs`, Rechenweg in `SimulationSolarthermie`). Die Eingaben stehen an
der Anlagenzeile des Felds in `Tab_Energieanlagen` (neben Modulanzahl, Neigung und Azimut), die
Bezugsfläche am Kollektorsatz (`Tab_Solarkollektoren(_STAMM).Bezugsflaeche`); Schemaschritt
`SolarthermieFelderSchema`, alle Spalten mit `CHECK`.

| Spalte | Bedeutung | leer |
|---|---|---|
| `Pumpenleistung_W` | elektrische Leistung der Solarkreispumpe | `Hilfsenergie_Anteil` der Zeile auf die genutzte Wärme, sonst kein Pumpenstrom |
| `Solarkreisverluste_Prozent` | Verluste von Leitung und Übertrager, 0 … 50 % | 8 % |
| `Arbeitstemperatur_Weg` | `fest` oder `speicher` | `fest` |
| `Uebertrager_Graedigkeit_K` | Grädigkeit des Wärmeübertragers (nur `speicher`) | 5 K |
| `Kollektor_Spreizung_K` | Spreizung des Kollektorkreises (nur `speicher`) | 10 K |
| `Bezugsflaeche` (Katalog) | `apertur` oder `brutto` | `apertur` |

**Arbeitstemperatur.** `fest` rechnet gegen 50 °C. `speicher` rechnet die mittlere Fluidtemperatur
ϑ_m = ϑ_Speicher,unten + Grädigkeit + Spreizung/2 in jeder Stunde neu, mit dem Speicherstand vom
Beginn der Stunde (`Stunde_Start`, vor Bedarf und Ladung): ϑ_Speicher,unten ist die unterste Zone des
Puffers, den das Feld lädt (`T_unten`, bei einer Zone Rücklauf + SOC/Q_max · (Vorlauf − Rücklauf)).
Das Feld nimmt den ersten Puffer seiner Ladefolge aus dem Speicherregister
(`SolarTemperaturSpeicherSetzen` nach dem Rücklaufspeicher des Kessels); das Protokoll nennt ihn.
Ohne Puffer gilt der Heizkreisrücklauf der Stunde, wenn die Anlagenkopplung ihn rechnet, sonst 50 °C
mit einem Hinweis. Die Leistung des Kollektors bestimmt so den Speicher und der Speicher die
Leistung der nächsten Stunde; eine Iteration innerhalb der Stunde gibt es nicht. Das Ergebnis führt
die mittlere Arbeitstemperatur je Feld (`ArbeitstemperaturMittelC`, Referenzskalar nur bei
`speicher`).

**Einfallswinkel.** Die Strahlung auf die geneigte Fläche teilt sich in den Direktanteil
G_b = DNI · cos θ und den Rest G_dr = G_t − G_b (Diffus und Reflexion). Die Leistung ist
q = η_0 · (K_b(θ) · G_b + K_d · G_dr) − a_1 · Δϑ − a_2 · Δϑ², Δϑ = ϑ_m − ϑ_a. K_d ist der gepflegte
Katalogwert `Kdfu`; ohne ihn rechnet die Diffusstrahlung mit K_b(θ) wie die Direktstrahlung — der
alte Rechenweg, bitgleich.

**Bezugsfläche.** Die Kennwerte η_0, a_1, a_2 gelten für die Fläche, auf die das Datenblatt sie
bezieht: `apertur` multipliziert mit der Aperturfläche, `brutto` mit der Modulfläche. Fehlt bei
`brutto` die Modulfläche, rechnet das Feld mit der Aperturfläche und meldet es. Der VDI-3805-Import
setzt `brutto` nur, wenn die Bezugsfläche der Datei der Bruttofläche gleicht und von der
Aperturfläche abweicht; Absorberflächen bleiben `apertur`.

**Solarkreis.** Die Verluste kürzen die abgegebene Wärme um den Faktor (100 − p)/100 — bei 8 %
bitgleich 0,92. Die Pumpe läuft in jeder Stunde, in der das Feld Wärme abgibt (Senke oder Puffer),
und geht mit Leistung · 1 h (ohne Leistung: `Hilfsenergie_Anteil` × genutzte Wärme der Stunde)
in den Rest-Strombedarf (`Rest_Strombedarf_viertelstuendlich`, je
Viertelstunde gleich verteilt); das Ergebnis zeigt den Pumpenstrom im Reiter Solarthermie und als
Referenzskalar `Solarthermie.PumpenstromMwh`, beides nur, wenn er größer als null ist.

Gehalten von `EPOS.Kern.Tests/SolarkreisTests`, `SolarthermieModellgrenzenTests`,
`SolarthermieFelderSchemaTests` und `SolarWaermeMonateTests` (Referenzprojekt 1049 mit `speicher`,
Grädigkeit 5 K, Spreizung 10 K, ohne Pumpe und mit der Vorgabe der Verluste).

## 17. Bedarf: Netzverluste je Kanal, Zirkulation, Betriebskalender

Drei Optionen der Bedarfsrechnung, alle mit der Vorgabe „leer = wie ohne sie“ (Schemaschritt
`BedarfNetzKalenderSchema`; Punkte BW4, PW2 und BW2 der Entscheidungsvorlage Modellgrenzen). Kein
Referenzprojekt setzt eine davon; die Basis bleibt unberührt.

**Netzverluste je Kanal.** `Tab_Einstellungen` führt je Wärmekanal Wert und Einheit:
`Netzverluste_Heizung`, `Netzverluste_Brauchwasser`, `Netzverluste_Prozess` (REAL ≥ 0) mit
`…_Einheit` (`%` oder `kWh/a`, paarweise, ein Prozentwert höchstens 100). Die Regel
(`SimulationWaermebedarf`, `Netzverlustvorgabe`):

| Stand | Wirkung |
|---|---|
| alle drei Kanalwerte leer | der Projektwert `Netzverluste`/`NetzverlusteEinheit` als konstanter Stundenbetrag, je Stunde anteilig auf Heizung, Brauchwasser und Prozess verteilt (`Kanalsatz.NetzverlusteVerteilen`) |
| mindestens ein Kanalwert gesetzt | je Kanal sein eigener Wert als fester Stundenbetrag auf genau diesen Kanal, ein leerer Kanal trägt 0; der Projektwert gilt nicht, und es wird nichts zwischen den Kanälen verteilt |

Ein Kanalwert in % bezieht sich auf den Jahresbedarf des Kanals vor dem Aufschlag
(`Q_k · p_k / 100 / 8760` je Stunde; beim Brauchwasser samt Zirkulation), ein Wert in kWh/a gilt
fest (`W_k / 8760`). Die Kanäle sind die Wärmekanäle; der Kühlkanal trägt keinen Netzverlust.
`Waermebedarf_Netzverluste` weist die Summe der drei Jahresmengen aus, das Laufprotokoll die drei
Posten. Bedient im Abschnitt „Wärmebedarf“ der Simulationskonfiguration; sobald ein Kanalwert
steht, sagt die Zeile unter den Netzverlusten, dass der Projektwert nicht gilt.

**Zirkulation im Bestandsweg.** `Zirkulation_Leistung_kW` (0 … 100) und `Zirkulation_Laufzeit_h_d`
(0 … 24) an `Tab_Einstellungen`. Rechnet das Projekt sein Brauchwasser aus den Bestandsprofilen,
kommt eine Zirkulation als eigene Teilreihe in den Brauchwasserkanal — dieselbe Formel wie die
Methode „manuell“ des Zapfprofilgenerators (Umsetzungskonzept Zapfprofilgenerator 4.3):

```
q_zirk,h = P in den Laufzeitstunden, sonst 0;   Q_zirk = P · t_Lauf · 365
```

Die Laufstunden liegen zusammenhängend um die Tagesmitte der Brauchwasserreihe
(`Zirkulationskanal.Laufzeitfenster`), eine gebrochene Laufzeit belegt die letzte Stunde anteilig.
Ohne Leistung oder ohne Laufzeit gibt es keine Zirkulation. Auf dem Generatorweg gilt dessen
Zirkulation, die Projekteinstellung wirkt dort nicht; die Vorschau mit Namensliste rechnet keine.
Ausgewiesen wird sie wie beim Generator: `Brauchwasser_Zirkulation_Mwh`, getrennte Monatsschichten
Zapfung und Zirkulation, der Posten „davon Zirkulation“ und das gestapelte Monatsbild im Reiter
Wärmebedarf. Normbezug: Verteilverluste der Trinkwassererwärmung nach DIN EN 15316-3 und
DIN V 18599-8, Betrieb der Zirkulation nach DVGW W 551.

**Betriebskalender.** `Tab_Betriebskalender` (STRICT) hält Kalender projektübergreifend wie einen
Katalog: Bezeichnung, Bundesland (leer = nur die neun bundeseinheitlichen Feiertage), bis vier
Betriebsferien als Tag im Jahr (Beginn nach Ende = über den Jahreswechsel), Ferienfaktor
f (0 … 1), `Feiertag_wie_Sonntag` und `Ferien_kuerzen` (0/1). Je Zuordnungszeile
(`Z_Projekt_Brauchwasser`, `Z_Projekt_Prozesswaerme`, `Z_Projekt_Stromverbraucher`) zeigt die
nullbare Spalte `ID_Betriebskalender` auf einen Kalender (`ON DELETE SET NULL`); leer = das
Wochenprofil gilt für alle Wochen.

Die Kalenderschicht (`Betriebskalenderschicht`) sitzt in `ProfilBedarf.Rechnen` zwischen der
Kachelung des Wochenprofils und der Monatsnormierung — dieselbe Routine für alle drei Profilarten:

```
t(h) = w((24 · w₀ + h) mod 168)                      Kachelung ab dem Wochentag des 1. Januar
a(h) = w(144 + s) an einem Feiertag, sonst t(h)       Sonntag des Wochenprofils
b(h) = f · m_s an einem Ferientag, sonst a(h)         m_s = (1/7) · Σ_d w(24 d + s)
q(h) = b(h) / Σ_M b · M_m · 1000                     Vorgabe: Ferien verteilen die Monatsmenge um
q(h) = b(h) / Σ_M a · M_m · 1000                     „Ferien kürzen die Monatsmenge“
```

h = 24 d + s, M die Stunden des Monats m, M_m seine Menge [MWh]. Ferien gehen Feiertagen vor;
Feiertage verteilen stets nur um. Die Feiertage kommen aus `Feiertage` (die neun
bundeseinheitlichen, Ostern als Rechenvorschrift) und `Landesfeiertage` (die landesweiten
Feiertage des gewählten Landes), aufgelöst gegen das Referenzjahr des Projekts
(`SolardatenCtrl.Referenzjahr`) und als Tag im Gemeinjahr abgebildet. Hat ein Monat ohne Kürzen
keine Stunde mit Bedarf mehr, rechnet er ohne Ferien und das Laufprotokoll nennt es. Ohne Kalender
ruft die Routine die Kachelung `BhkwPlan.StromWocheToJahr` wie zuvor.

Der Lauf liest den Kalender je Zuordnungszeile, die Projektvorschau über die ID des Kopfsatzes;
Zuordnungsdialog, Assistent und Speichern tragen ihn mit (`LiesProjekt`, `WizardCtrl.Add_*`),
Duplizieren behält die ID, Export und Import finden den Kalender über seinen Bezeichner. Bedient
wird er in der Verwaltung „Betriebskalender“ (Administration → Wärmebedarf & Heizung → Profile &
Lastgänge) und je Zuordnung in den Bedarfsprofil-Dialogen von Brauchwasser, Prozesswärme und
Strom.

Gehalten von `EPOS.Kern.Tests/BedarfNetzKalenderSchemaTests`, `NetzverlusteJeKanalTests`,
`ZirkulationBestandswegTests` (Läufe auf Kopien von 1041 und 1045), `BetriebskalenderTests` und
`EPOS.UI.Tests/Dialoge/BetriebskalenderDialogTests`.

## 18. Erzeuger in Teillast: Wärmepumpe und BHKW

Wärmepumpe und BHKW rechnen ihr Verhalten unter der Volllast aus Katalogfeldern; leer heißt
„nicht gepflegt", und ohne gepflegten Wert rechnet ein Gerät bitgleich ohne diesen
Abschnitt. Schemaschritt `ErzeugerTeillastSchema` (167), alle Spalten nullbar mit `CHECK`,
in Katalog und Projektkopie gleich; die Projektkopie entsteht beim Übernehmen aus dem Katalog
(`ErzeugerTeillastWerte`).

| Tabelle | Spalte | Bedeutung | leer |
|---|---|---|---|
| `Tab_WP(_STAMM)` | `Mindestleistung_kW` (0 … 1000) | kleinste Modulationsleistung P_min | kein Takten |
| `Tab_WP(_STAMM)` | `Taktverlustfaktor_Cd` (0 … 1) | Teillastkoeffizient C_d nach EN 14825 | 0,9 |
| `Tab_BHKW(_STAMM)` | `Wirkungsgrad_el_Teillast50` (0 … 1) | η_el bei 50 % elektrischer Last, Faktor | wie Volllast |
| `Tab_BHKW(_STAMM)` | `Wirkungsgrad_th_Teillast50` (0 … 1) | η_th bei 50 % elektrischer Last, Faktor | wie Volllast |
| `Tab_BHKW(_STAMM)` | `Anfahrverlust_kWh` (0 … 100) | Brennstoff je Start | 0 |
| `Tab_BHKW(_STAMM)` | `Mindestlaufzeit_min` (0 … 60) | Mindestlaufzeit je Start | 10 min |

**Wärmepumpe: Taktverlust (`Waermepumpentakt`).** Am Ende jeder Stunde
(`Zweikanalig_StundeEnde`) sammelt der Lauf je Modul die Verdichterwärme Q und den
Verdichterstrom P der Stunde aus Bedarfsdeckung und Ladung. Bei 0 < Q < P_min · 1 h taktet das
Gerät: CR = Q / P_min, f = CR / (C_d · CR + 1 − C_d), COP_takt = f · COP; die Wärme bleibt, der
Strom steigt um P · (1/f − 1) — in die Stundenreihe, die Modulsumme und den Jahresstrom, damit
auch in die JAZ. Die Starts folgen der Kesselregel (`Kesselkennlinie.StartsImTakt`) mit fest
10 min; außerhalb des Takts ist ein Start der Übergang aus einer Stillstandsstunde. Die
Quellentnahme der Stunde wird nicht nachgezogen. Im Kühlbetrieb rechnet die Kältekaskade
dasselbe mit der Mindestkühlleistung P_min / P_nenn · P_kühl(t) (`Kaeltekaskade.Mindestanteil`).

**BHKW: Teillastkennlinie (`BhkwTeillast`).** η_el,100 und η_th,100 teilen den
Gesamtwirkungsgrad im Verhältnis P_el : P_th, so dass Volllast unverändert bleibt. Zwischen
β = 0,5 und 1 verlaufen beide Wirkungsgrade linear, darunter gilt der Wert bei 0,5. Die
Motorläufe rechnen Wärme aus Strom über η_th(β)/η_el(β) mit β = P/P_el, Strom aus Wärme durch
Intervallhalbierung über β; ohne Kennlinie bleibt es der Dreisatz des Bestands, bitgleich. Der
Brennstoff einer Laufstunde ist P / η_el(β); die Abweichung gegen (Q + P)/η geht als
Teillast-Mehrbrennstoff in den Brennstoffverbrauch und die Emissionen.

**BHKW: Takten (`BhkwTeillast`, `SimulationBHKW.TeillastStundeAbschliessen`).** Nur mit
Anfahrverlust oder Mindestlaufzeit und einer Untergrenze x_min > 0. Unter der Untergrenze bleibt
das Modul nicht aus, sondern liefert in allen drei Fahrweisen den Wärmeraum, den Reststrom oder
— ohne Einspeisung — das Kleinere von beiden mit der Stromkennzahl an x_min. Die Starts zählt
die Kesselregel gegen die Wärme der Untergrenze Q_min = x_min · P_el · η_th(x_min)/η_el(x_min);
je Start kommt der Anfahrverlust auf den Brennstoff. Der Brennstoff einer Taktstunde rechnet mit
η_el(x_min). Getaktet wird nur, wenn der Wärmeraum der Stunde (offener Bedarf plus freier
Pufferraum) mindestens einen Mindestlauf Q_min · t_min / 60 aufnimmt (t_min leer = 10 min;
`BhkwTeillast.NimmtMindestlaufWaerme`); stromgeführt muss der Reststrom den Strom eines
Mindestlaufs tragen, ohne Einspeisung beides — sonst bleibt das Modul aus. Der Heizkessel zählt
sein Takten (`Kesselkennlinie.Taktet`) dagegen schon ab jeder Wärme über dem Zahlenrand, denn er
deckt als letzter Erzeuger auch einen kleinen Rest.

**Ergebnis.** Die Reiter Wärmepumpe und BHKW zeigen Starts, Mehrstrom, Anfahrverlust und
Teillast-Mehrbrennstoff nur, wenn ein Modul sie rechnet; ebenso die Referenzskalare
`Takt.Waermepumpe[i].*`, `Takt.Kaelte[k].*`, `Takt.Bhkw[i].*` und
`Teillast.Bhkw[i].MehrbrennstoffKwh`, nur bei Werten größer null — die Basis bleibt damit
unberührt, solange kein Referenzprojekt die Felder pflegt.

**Pflege.** Der BHKW-Katalogeditor führt die Gruppe „Teillast und Takten" mit kleiner
Kennlinie, die BHKW-Verwaltung dieselben vier Felder (leer schreibt NULL); der
Wärmepumpenkatalog führt Mindestleistung und C_d mit dem Hinweis „Vorgabe 0,9 nach EN 14825",
die Projektdialoge zeigen die Werte lesend. Der VDI-3805-Import (Blatt 22) setzt keine der
Spalten: Die Lastangaben der Datei nennen einen Modulationsbereich, aber keine Mindestleistung
in kW, und C_d steht nicht in der Datei; das BHKW hat keinen VDI-Import.

Gehalten von `EPOS.Kern.Tests/ErzeugerTeillastTests` (Formeln ohne Datenbank, Rechnungen auf
Kopien der Projekte 1039, 1017, 1018 und 1024), `ErzeugerTeillastSchemaTests`,
`KatalogAufklapperTests` und den bunit-Fällen `BhkwKatalogDialogTests`,
`WaermepumpeStammFelderTests`.
## 19. Strom in Viertelstunden: PV-Bilanz, Einspeisegrenze, Standby

Die Strombilanz der Photovoltaik und der Stromspeicher laufen auf den 35 040 Viertelstunden des
Jahres; die Klimadaten bleiben stündlich. Schemaschritt `StromViertelstundenSchema`, alle neuen
Spalten mit `CHECK`.

| Tabelle | Spalte | Bedeutung | leer |
|---|---|---|---|
| `Tab_Einstellungen` | `Einspeisegrenze_Wert` | höchste PV-Einspeisung am Netzanschluss, ≥ 0 | keine Grenze |
| `Tab_Einstellungen` | `Einspeisegrenze_Einheit` | `kW` oder `%` der installierten PV-Leistung | `kW` |
| `Tab_Stromspeicher(_STAMM)` | `Standby_Verbrauch` | Standby des Speichersystems in W, 0 … 1 000 (im Code geprüft) | 0 |
| `Tab_Stromspeicher(_STAMM)` | `Selbstentladung_Prozent_Monat` | Selbstentladung in %/Monat, 0 … 20 | 0 |

**PV-Bilanz.** `SimulationPV.Bilanzieren` verteilt die Stundenerzeugung aller Anlagen auf die vier
Viertel nach dem Kosinus des Zenitwinkels in der Mitte jeder Viertelstunde
(`SolarPVGISCalculator.KosinusZenitwinkel`, Zeitachse der UTC-Stunde der Klimazeile):
P_q = 4 · P_h · cos θ_z,q / Σ cos θ_z. Das Mittel der vier Viertel ist der Stundenwert; ohne Sonne
in allen vier Vierteln tragen alle den Stundenwert. Direktverbrauch, Überschuss und Reststrom
entstehen je Viertelstunde gegen `Rest_Strombedarf_viertelstuendlich`; die Stundenreihen sind die
Mittel ihrer Viertel. Der Reiter „Photovoltaik" zeigt den Überschuss vor dem Speicher.

**BHKW-Einspeisung.** Die Kaskade zieht den BHKW-Strom je Viertelstunde stundenkonstant und ungeklemmt vom
Strombedarf ab; was danach negativ steht, nimmt kein Verbraucher des Anschlusses ab. Ohne Speicherflotte ist die
BHKW-Einspeisung je Viertelstunde dieser negative Rest, max(0, −Rest_q), gebildet direkt nach der Kaskade, mit und
ohne Photovoltaik (`SimulationControl.BhkwEinspeisung_viertelstuendlich` [kW], Stundenmittel
`BhkwEinspeisungDesLaufs` [kWh/h]; `SimulationPV.BhkwUeberschuss` ist dieselbe Größe, eine Formel
`BhkwUeberschussKw`). BHKW-Reiter (Linie und Kennzahl), Zeitreihensatz `BHKW_UEBERSCHUSS` (Berichtsbild,
Excel-Monatsblock), der KWK-Split der Strommatrix und über ihn die Wirtschaftlichkeit lesen diese eine Reihe; die
Energiebilanz Netzbezug + PV-Eigenverbrauch + BHKW-Strom − Einspeisung = Strombedarf aller Verbraucher schließt je
Viertelstunde. Mit Speicherflotte ist die BHKW-Netzeinspeisung der Flottenbilanz die Quelle. Netzbezug, Reststrom und
der Reststrombedarf der BHKW-Zeile bleiben davon unberührt.

**Einspeisegrenze.** P_grenz in kW, oder in % als Anteil der installierten Leistung (kWp). Je
Viertelstunde: E_ein = min(Ü − Ladung − Standby aus PV, P_grenz), Abregelung = Rest. Der Speicher
lädt vor dem Abregeln (`SimulationControl.PvEinspeisungAufteilen` nach der Speicherphase). Ohne
Grenze ist die Abregelung null.
Ausweis: Reiter „Photovoltaik" (Abregelung kWh/a und % der Erzeugung, Grenze kW), Zeitreihe
`PV_ABREGELUNG`, Monatstafel des Berichts, Referenzskalar `Photovoltaik.AbregelungMwh` nur bei
> 0, Kennzahl `pv_eigen` ohne Abregelung. Eine aktive Speicherflotte liest die Projekteinstellung
als weiche Grenze (`PvEinspeisegrenzeWeichKw`): Sie lädt zuerst, darüber wird abgeregelt, die
Variante bleibt zulässig. Eine neue Flotte belegt ihre harte Netzeinspeisegrenze mit dem Wert vor;
der Netzblock nennt ihn.

**Standby und Selbstentladung.** `SpeicherEngine.Speichersystem` zieht zu Beginn jedes Intervalls
die Selbstentladung SoC · s/100 · Δt/730 h ab, höchstens bis SoC_min, und teilt den Standby
(`StandbyBilanz`): aus dem PV-Überschuss nach der Ladung, sonst aus dem Netz, nie aus der Batterie.
Der Netzanteil geht in den Rest-Strombedarf, der PV-Anteil fehlt in der Einspeisung; der Fahrplan
bleibt unberührt. In der Flotte ist der Standby der Hilfsverbrauch der Einheit (Standortlast am
Netzanschluss), die Selbstentladung ein Parameter der Einheit; beide übernimmt sie aus dem Katalog.
Ausweis: Reiter „Stromspeicher" (Eigenverbrauch Speichersystem, Netzanteil, Selbstentladung),
Zeitreihe `SPEICHER_EIGENVERBRAUCH`, Monatstafel des Berichts, Referenzskalar
`Stromspeicher.EigenverbrauchSystemMwh` nur bei > 0.

Gehalten von `EPOS.Kern.Tests/StromViertelstundenTests`, `StromViertelstundenSchemaTests`,
`PvAusweisStromMatrixTests`, `PvPreisProjektTests` und `SpeicherEngine.Tests/SpeichersystemTests`;
Basis `Referenzlaeufe/2026-10-03_R34_Erdreich`.

## 20. Katalogabgleich mit Katalogfassung; Erdreichprüfung im Ergebnis

Welle M6 der [Entscheidungsvorlage Modellgrenzen](Entscheidungsvorlage_Modellgrenzen_Rechenwege.md)
(KU1 Stufe 1 und 2, EQ1). **Kein Rechenweg ist betroffen:** Der Lauf liest Projektkopien, und die
fasst der Abgleich nie an; die Erdreichprüfung wird nur gespeichert, nicht anders gerechnet.

**Schemaschritt `KatalogfassungSchema`** (DDL und Saat, wiederholbar):

| Ort | Spalte bzw. Tabelle | Bedeutung |
|---|---|---|
| acht Kataloge der Stufe 1 | `Katalog_Schluessel` TEXT, eindeutig (Teilindex `WHERE … IS NOT NULL`) | stabile Kennung des Auslieferungssatzes, über Fassungen gleich; Anwendersatz leer |
| dieselben | `Katalog_Pruefsumme` TEXT (64 Hexzeichen) | SHA-256 des ausgelieferten Stands |
| dieselben | `Katalog_Ausgelaufen` INTEGER 0/1 | 1 = in einer späteren Auslieferung entfallen, bleibt stehen |
| `Tab_Applikation` | `Katalogfassung` INTEGER | Fassung des letzten Abgleichs; leer = noch nie |
| `Tab_Katalogabgleich` | STRICT | Protokoll: Zeitpunkt, Fassung, Tabelle, Schlüssel, Aktion, Hinweis |
| `Tab_ErgebnisErdreich` | STRICT, an Projekt und Anlage (`ON DELETE CASCADE`) | Prüfzeilen der Erdreichprüfung je Lauf |

**Die Stufe 1** (`Katalogfassung.Stufe1`) sind die laufend gepflegten Kataloge:
`Tab_WP_STAMM` (Kürzel WP, mit den Kindtabellen `Tab_Kenndaten_STAMM` und
`Tab_Kenndaten_Kuehlung_STAMM`, diese nur mit `ID_Projekt` 0 oder leer), `Tab_Heizkessel_STAMM`
(KES), `Tab_BHKW_STAMM` (BHKW), `Tab_PV_STAMM` (PV), `Tab_Brauchwasser_STAMM` (BW),
`Tab_Brauchwassertyp_STAMM` (BWT), `Tab_Prozesswaerme_STAMM` (PW), `Tab_Prozesstyp_STAMM` (PWT).

**Die Stufe 2** (`Katalogfassung.Stufe2`, Schemaschritt `KatalogfassungStufe2Schema`: dieselben
drei Spalten samt Teilindex an den sechzehn Kopftabellen, Saat wie oben) sind die übrigen Kataloge.
Kindtabellen tragen keine Katalogspalte, ihre Zeilen gehören über den Fremdschlüssel zum Kopf:

| Katalog | Kopftabelle (Kürzel) | Name für Schlüssel und Namensprüfung | Kind- und Enkeltabellen |
|---|---|---|---|
| Baustoffe | `Tab_Baustoff_STAMM` (BST) | Bezeichner | `Tab_Baustoffsynonym_STAMM` (`ID_Baustoff`, eingefügt gesperrt) |
| Bauteilaufbauten | `Tab_Bauteilaufbau_STAMM` (BTA) | Bezeichner | `Tab_Bauteilschicht_STAMM` (`ID_Aufbau`); `ID_Baustoff` ist ein Verweis über den Schlüssel des Baustoffs |
| Brennstoffe | `Tab_Brennstoff_Stamm` (BRS) | Bezeichner | —; `ID_Kategorie` ist ein Verweis über `Tab_BrennstoffKategorien.Gruppe` |
| Tagesverteilungen | `Tab_DBTagV_STAMM` (TAGV) | Bezeichner | `Tab_DBTagVDaten_STAMM` (`ID_TagV`, Reihe) |
| Gebäude | `Tab_Gebaeude_STAMM` (GEB) | Bezeichner | `Tab_Konditionierungsvorgabe`, `Tab_Konditionierungskalender` (`ID_Gebaeude_Stamm`), Enkel `Tab_Konditionierungsperiode` (`ID_Kalender`) |
| Konditionierungsvorlagen | `Tab_Konditionierungsvorlage_STAMM` (KV) | Größe und Bezeichner | wie Gebäude, über `ID_Vorlage` |
| Pufferspeicher | `Tab_Pufferspeicher_STAMM` (PS) | Bezeichner | — |
| Vorgaben der Pufferauslegung | `Tab_PufferAuslegungParameter_STAMM` (PAP) | Schluessel | — |
| Solarkollektoren | `Tab_Solarkollektoren_STAMM` (SK) | Bezeichner | — |
| Solarganglinien | `Tab_Solarganglinie_STAMM` (SOLGL) | Bezeichner | `Tab_SolarganglinieDaten_STAMM` (`ID_Ganglinie`, Reihe) |
| Stromspeicher | `Tab_Stromspeicher_STAMM` (SSP) | Bezeichner | — |
| Stromverbraucher-Wochenprofile | `Tab_Stromverbrauchertyp_STAMM` (SVT) | Typname | — |
| Stromverbraucherprofile | `Tab_Stromverbraucher_STAMM` (SV) | Bezeichner | — |
| Stromganglinien | `Tab_Stromganglinie_STAMM` (STRGL) | Bezeichner | `Tab_StromganglinieDaten_STAMM` (`ID_Ganglinie`, Reihe) |
| Wärmebedarfsganglinien | `Tab_Waermebedarf_STAMM` (WBGL) | Bezeichner | `Tab_WaermebedarfDaten_STAMM` (`ID_Ganglinie`, Reihe) |
| Wechselrichter | `Tab_Wechselrichter_STAMM` (WR) | Bezeichner | — |

**Benannt ausgenommen** (`Katalogfassung.Ausgenommen`, im Bericht des Werkzeugs genannt):

- **Klimakatalog** (`Tab_Klimaregion_STAMM`, `Tab_Klimadaten_STAMM`, `Tab_Solar_STAMM`): je Region
  eine Stundenreihe mit 13 Spalten und eine Tagesreihe — das Paket würde dreistellige Megabyte groß.
  Der Pflegeweg ist der Klimaimport mit Quelle, Importdatum, Szenario und Bezugsjahr; der
  Primärschlüssel heißt `ID_Klimaregion`.
- **Zapfprofilkatalog** (`Tab_Tww*_STAMM`, acht Tabellen): Er führt eine eigene Katalogversion und
  einen eigenen Paketweg (`TwwPaketteilCtrl`, Katalogimport mit Konfliktregeln und Herkunft je
  Zeile), und seine Tabellen verweisen über IDs aufeinander. Ein zweiter Weg daneben ergäbe zwei
  Wahrheiten über denselben Stand; das Update dieses Katalogs geht über sein Paket.

Die Wache `KatalogabgleichTests` hält das Register gegen das Schema: Jede Spalte einer
Registertabelle ist Fach- oder Metaspalte, jede Spalte einer Kind- oder Enkeltabelle Fachspalte,
Fremdschlüssel, Projekt- oder Nebenspalte (ein anderer Eigentümer derselben Tabelle), jedes
Verweisziel steht vor seinem Verweiser, und jede `_STAMM`-Tabelle der Testdatenbank steht im
Register oder in den Ausnahmen.

**Was ein Abgleich der Stufe 2 bewirkt.** Brennstoffe, Konditionierungsvorlagen und die Vorgaben
der Pufferauslegung haben wie die Gerätekataloge eine Projektkopie (Abschnitt 22): Der Abgleich
ändert nur den Katalog, ein Projekt rechnet danach wie vorher. Vor dem ersten Schreiben legt er die
fehlenden Kopien der Brennstoffe und der Pufferauslegungs-Vorgaben wertgleich zum alten Stand an.
Ein vom Anwender angepasster oder entsperrter Satz bleibt wie in Stufe 1 stehen. Die Testdatenbank
wird nie abgeglichen; die Saat setzt dort nur Schlüssel und Prüfsumme.

**Prüfsumme.** SHA-256 über die Fachspalten in der festen Folge der Liste, je Spalte
„Name=Wert"; Zahlen invariant und rundlauffest (eine ganzzahlige Gleitkommazahl wie die Ganzzahl,
Wahrheitswerte als 1/0), Text unverändert, leer trägt nichts bei — eine neu und leer angelegte
Spalte verschiebt keine Prüfsumme. Die Kindzeilen gehen sortiert hinter dem Kopf ein, eine
**Reihe** (Ganglinie, Tagesverteilung) in ihrer Folge mit Position, die Enkel einer Kindzeile
sortiert in deren Zeile. Ein **Verweis** geht mit dem Namen seines Ziels ein (Schlüssel des
Baustoffs, Gruppe der Brennstoffkategorie), nie mit dessen ID, und wird beim Schreiben wieder zur
ID des Ziels in dieser Datenbank; fehlt das Ziel, bleibt die Spalte leer.
**Schlüssel:** Kürzel und bereinigter Name (Umlaute ausgeschrieben, alles außer A–Z und
0–9 als „_", groß), bei Dopplung mit Zähler `_2`, `_3`; der Name ist der Bezeichner oder die
Namensspalten der Tabelle (Konditionierungsvorlage: „HEIZSOLL / Büro" → `KV:HEIZSOLL_BUERO`). Die
**Saat** des Schritts belegt Schlüssel und Prüfsumme jedes gesperrten Satzes (`ReadOnly = 1`) ohne
Schlüssel — kein Fachwert ändert sich. Ob ein Name belegt ist, prüft der Abgleich ohne Unterschied
von Groß- und Kleinschreibung.

**Katalogpaket.** `Werkzeuge/Auslieferungsvorlage` schreibt den Auslieferungsstand in der
Vorlage fest (`Katalogpaket.Festschreiben`: Saat, Prüfsummen auf den heutigen Stand,
`Katalogfassung`) und legt `Katalogpaket.json` neben die Vorlage: alle gesperrten Sätze des
Registers mit Schlüssel, Prüfsumme, Werten und Kindzeilen, dazu die Fassung (`--katalogfassung`,
Vorgabe das Datum als JJJJMMTT; die Setup-Kette gibt sie als JJJJMMTTnn aus dem Freigaberegister
`Setup/Katalogfassungen.txt` mit, Setup-Konzept Abschnitt 6.5). Eine JSON-Datei statt des Formats des Katalogimports: Jener liest
Herstellerformate ohne Schlüssel und Prüfsumme, und das CSV-Paket des Zapfprofilgenerators trennt
Zahl und Text nicht — die Prüfsumme braucht beides. Das Paket ist deterministisch (Tabellen in der
Folge des Registers, Sätze nach Schlüssel, Werte in Spaltenfolge, ASCII mit LF) und trägt keinen
Schemastand. **Formatversion 2** liest auch Fassung 1; eine Reihe steht darin als bloße Werteliste
in ihrer Folge, die Enkel einer Kindzeile unter `"Kinder"`. **Größe:** Die Reihen machen den
Hauptteil (eine Stundenreihe rund 0,2 MB, eine Viertelstundenreihe rund 0,8 MB); das Paket der
Testdatenbank misst knapp 1 MB (417 Sätze, drei Wärmebedarfsganglinien). Das Werkzeug nennt die
Größe im Bericht und warnt ab 20 MB — dann wären die Reihen benannt auszunehmen. In der Auslieferung liegt es unter `{app}\Vorlage\Katalogpaket.json`
(`Katalogpaket.Pfad(Dienste.Pfade.Auslieferungsvorlage)`).

**Abgleich** (`Katalogabgleich`), je Satz des Pakets:

| Lage in der Datenbank | Aktion |
|---|---|
| Schlüssel fehlt | **eingefügt** (`ReadOnly = 1`, Schlüssel, Prüfsumme); trägt ein eigener Satz den Namen: behalten mit Hinweis |
| Werte = Paket, Prüfsumme = Paket | nichts (unverändert) |
| gesperrt, Prüfsumme der Zeile = gespeicherte Prüfsumme | **aktualisiert** (Werte, Kindzeilen, Prüfsumme, nicht ausgelaufen) |
| geändert (Prüfsumme weicht ab) oder entsperrt (`ReadOnly = 0`) | **behalten** — „Ihre Anpassung bleibt; der neue Auslieferungsstand liegt als Vergleich vor" |
| Satz mit Schlüssel, den das Paket nicht mehr führt | **ausgelaufen** (`Katalog_Ausgelaufen = 1`), nie gelöscht |

Anwenderzeilen (ohne Schlüssel) und jede Projektkopie bleiben unberührt. Alles läuft in EINEM
Vorgang, samt Protokoll und neuer `Katalogfassung`; ein Lauf mit derselben Fassung tut nichts, und
auch ein erzwungener Lauf schreibt keine Protokollzeile doppelt. Ohne lesbares Paket steht
`KEIN_PAKET` im Protokoll.

**Beim Start** (`Katalogabgleich.BeimStart`, gerufen von `Program.Main` der Windows-Schale nach
erfolgreicher Schemamigration): nur wenn das Paket eine NEUERE Fassung trägt als die Datenbank, nach
einer Sicherung per `VACUUM INTO` in `DB-Backup` (`Katalogabgleich.SicherungAnlegen` über
`Datenbanksicherung.KopieAnlegen`). Den Bericht („n neu, m aktualisiert, k behalten,
a ausgelaufen") zeigt das Hauptfenster einmal als Überlagerung. iOS gleicht nicht ab; dort liegt
kein Paket.

**Bedienung.** Administration → Daten & Import → „Katalog aktualisieren…"
(`KatalogabgleichDialog`, Hülle `KatalogabgleichHuelle`, Fenster `KatalogabgleichFenster`): Fassung
der Datenbank und des Pakets, „Nur prüfen", „Abgleichen…" (Rückfrage, Sicherung) und je behaltenem
Satz „Auslieferungsstand wiederherstellen…" (Werte des Pakets, gesperrt, Aktion
`WIEDERHERGESTELLT`). Eine Katalogkopie (`Katalogkopie.Duplizieren`) übernimmt die Katalogspalten
nicht, die Dublettenprüfung vergleicht sie nicht.

**Erdreichprüfung im Ergebnis (EQ1).** `SimulationRunner.BaueErgebnis` legt die Prüfung des Laufs
(`ErdreichAuswertung.FuerProjekt`) ins Modell, `ErgebnisCtrl.Save` schreibt sie im Vorgang des
Ergebnisses nach `Tab_ErgebnisErdreich` (die alten Zeilen des Projekts weg, die neuen hinein);
`ErgebnisCtrl.Delete` nimmt sie mit. Je Anlage die Prüfzeilen `ENTZUGSLEISTUNG` (W; Hinweis = Text
anstelle der Prüfung), `JAHRESENTZUG` (kWh/a; Hinweis = Vorbehalt), `VOLLLASTSTUNDEN` (h/a),
`FROST` (h, Grenzwert 5 % der Betriebsstunden; Hinweis = Frostmeldung) und `VDI4640:<Zeile>` je
Zeile der Auslegungsprüfung, alle mit Grundlage und Laufstempel. Der Erdreich-Dialog nimmt den Lauf
der Sitzung, sonst das gespeicherte Ergebnis (`ErdreichErgebnisSpeicher.Gespeichert`) und zeigt
darunter „Stand des Laufs vom …". Der Bericht führt die Prüfung nicht als Baustein; ein späterer
Baustein liest sie über `ErdreichErgebnisSpeicher.Lesen`.

Gehalten von `EPOS.Kern.Tests/KatalogabgleichTests` (Prüfsumme, Schlüssel, Wachen des Registers,
Saat, Abgleich mit der Probe `Referenzlaeufe/Importproben/Katalogpaket_Probe.json` — Prozesswärme
aus Stufe 1, Konditionierungsvorlagen und Wechselrichter aus Stufe 2 —, Reihen, Enkel und Verweise,
Formatversion, Wiederherstellen, Start, kein Paket), `ErdreichErgebnisSpeicherTests`, `KatalogduplizierenTests`, den Werkzeugtests
`KatalogpaketVorlageTests` und den bunit-Fällen `KatalogabgleichDialogTests` und
`QuelleErdreichDialogTests`.

## 21. Pufferspeicher: Bereitschaft, Zonenanteile, Frischwassermodul, Desinfektion

Vier Optionen des Speichers und des Brauchwassers (Entscheidungsvorlage Modellgrenzen PS1 (c),
PS1 (a), PS5 (a), BW5). Schemaschritt `PufferOptionenSchema`, alle Spalten nullbar mit `CHECK`;
leer rechnet Anweisung für Anweisung wie zuvor. Die Rechenregeln stehen in
`Allgemein/Simulation/PufferOptionen.cs` und `Desinfektion.cs`, ohne Datenbank.

| Tabelle | Spalte | Bedeutung | leer |
|---|---|---|---|
| `Tab_Pufferspeicher` | `Bereitschaft_Weg` | `tag` oder `temperatur` | `tag` |
| `Tab_Pufferspeicher` | `Aufstellraum_Temperatur_C` | Raumtemperatur am Speicher, 0 … 35 °C | 20 °C |
| `Tab_Pufferspeicher` | `Schicht_Anteile` | Volumenanteile der Zonen von oben, „0,10;0,16;0,37;0,37" | gleich große Zonen |
| `Tab_Pufferspeicher` | `Frischwassermodul` | 0/1 | aus |
| `Tab_Pufferspeicher` | `FWM_Graedigkeit_K` | Grädigkeit des Moduls, 0 … 20 K | 5 K |
| `Tab_Einstellungen` | `Desinfektion_Aktiv` | 0/1 | aus |
| `Tab_Einstellungen` | `Desinfektion_Intervall_Tage` | 1 … 31 | 7 |
| `Tab_Einstellungen` | `Desinfektion_Stunde` | 0 … 23 | 2 |
| `Tab_Einstellungen` | `Desinfektion_Zieltemperatur_C` | 55 … 90 °C | 70 °C |
| `Tab_Einstellungen` | `Desinfektion_Volumen_l` | 0 … 100 000 l | Volumen der Speicher mit Brauchwasser |

Die Pufferfelder stehen an der Projektkopie (wie Schichtzahl und Entnahmehöhen): Sie beschreiben
die Anlage, in der der Speicher steht, nicht das Gerät. Der Katalog bleibt bei seinen Gerätewerten.

**Bereitschaft nach Temperatur (PS1 (c)).** `H = Q_B · 1000 / (24 · 45 K)` [W/K] aus dem
Katalogwert Q_B [kWh/24 h] (Prüfwert nach EN 12897/EN 15332 bei 45 K). In Phase G verliert jede
Zone `H · a_i · max(ϑ_i − ϑ_Raum, 0) / 1000` [kWh], höchstens ihren Inhalt über dem Rücklauf; die
Summe geht vom Füllstand ab wie der Tageswert. Ein leerer Speicher verliert nichts, ein voller
`H · (ϑ_VL − ϑ_Raum)` — mehr als der Tageswert ab 45 K Übertemperatur. Quellspeicher und der
Durchfluss einer Stunde rechnen mit dem Tageswert bzw. ohne Verlust. Das Protokoll nennt H.

**Zonenanteile (PS1 (a)).** Mit gültigen Anteilen (Summe 1 ± 0,001, Anzahl = Zonenzahl, je Anteil
> 0; `PufferOptionen.AnteilePruefen`, benannte Ablehnung im Dialog, im Lauf Warnung und gleich große
Zonen) ist die Zone i `Q_max · a_i` groß: Füllen, Leeren, Klemmen, Temperatur, Mantelfläche
`π · D · H · a_i` (plus Deckel), Leitwert je Paar `λ · A / (H · (a_i + a_i+1)/2)`, Kappung an der
kleineren Kapazität des Paars, Inversionsmischung auf dem Füllgrad mit Volumengewicht und die Zone
zu einer Anschlusshöhe (kumuliertes Band). Ohne Anteile laufen die Zweige der gleich großen Zonen
unverändert. Vorschlag im Dialog: „Vorschlag Kombispeicher" = vier Zonen 0,10/0,16/0,37/0,37, nach
der Gliederung von prEN 15316-5 Anhang B (Planungsvorschlag, kein Normwert).

**Frischwassermodul (PS5 (a)).** Nur an einem Speicher mit Brauchwasser im Klassen-Set (sonst
Warnung, ohne Wirkung). Mindesttemperatur oben `ϑ_FWM = ϑ_Zapf + ΔT_FWM`; ϑ_Zapf ist die höchste
Zapftemperatur der Zonen des Zapfprofilgenerators (Zone, sonst Bezugswert der Nutzungsart), ohne
Generator 60 °C mit Hinweis. In der Entladung des Brauchwasserkanals (Phasen A und E,
`Kaskadenschleife.EntladeKanal`) klemmt `SimulationPufferspeicher.FrischwasserEntnahmefaehigkeit`
den Bedarf: Hält die oberste Zone ϑ_FWM nicht, nur der Durchfluss; sonst geschichtet die Zonen mit
ϑ_i ≥ ϑ_FWM ab der Brauchwasser-Entnahmehöhe, mit einer Zone der Inhalt über ϑ_FWM. Der Rest bleibt
offen für die nächste Stufe der Kaskade (Muster `TNutz[PROZESS]`, Abschnitt 15 (d)). Das Protokoll
und die Pufferrubrik nennen die Stunden mit Begrenzung (je Stunde einmal gezählt).

**Thermische Desinfektion (BW5).** Zusatzbedarf je Ereignis
`Q_D = V · 1,163 kWh/(m³·K) · (ϑ_Ziel − ϑ_Soll) / 1000` (V in l), am Tag d (0 … 364) in der Stunde
s, wenn (d + 1) mod Intervall = 0 — bei 7 Tagen 52 Ereignisse. ϑ_Soll ist die Speichertemperatur
des Zapfprofilgenerators (`Tab_TwwProjekt.Speicher_C`), ohne sie 60 °C mit Hinweis; V das gepflegte
Volumen, sonst die Summe der Speicher mit Brauchwasser; ohne V oder mit ϑ_Soll ≥ ϑ_Ziel kein Posten
(Warnung). `SimulationWaermebedarf.BrauchwasserDesinfektion` addiert die Reihe NACH den
Netzverlusten in den Brauchwasserkanal und führt sie als `Brauchwasser_Desinfektion_Mwh`.

Deckung (`Desinfektionsdeckung`): Vor der Kaskade wird der Zusatzbedarf aus dem Kanal genommen
und nur vor einer **fähigen** Stufe freigegeben; was die Stufe danach im Kanal deckt, deckt zuerst
ihn, der Rest steht wieder zurück. Fähig sind Heizkessel und BHKW mit gepflegtem Vorlauf ≥ ϑ_Ziel
oder ohne gepflegten Vorlauf (Regel 15 (b)), die Wärmepumpe, wenn eine projektierte Kennlinie den
Vorlauf erreicht, ihr Heizstab (Phase F) und nach Phase E ein Brauchwasserspeicher mit gepflegtem
Vorlauf ≥ ϑ_Ziel, den eine fähige Anlage lädt. Solarthermie und Pufferentladung sonst nie. In der
Speicherstufe gilt das je Erzeugerart, bei Vektorstufen je Stufe. Was offen bleibt, deckt ein
benannter **Zusatzstrom** (elektrisch, Wirkungsgrad 1, in `Rest_Strombedarf_viertelstuendlich`,
Warnung). Ausweis: Reiter Wärmebedarf „davon thermische Desinfektion", Protokoll je Stufe, Bericht
„Thermische Desinfektion".

**Hinweis HK4.** Der Pufferdialog trägt an der Entladegrenze „Übertrager: Leistung als
Entladegrenze eintragen"; ein Übertragermodell gibt es nicht.

Kein Referenzprojekt setzt eines der Felder; die Basis R33 bleibt byte-gleich. Gehalten von
`EPOS.Kern.Tests/PufferOptionenSchemaTests`, `PufferOptionenTests`, `PufferOptionenLaufTests`
(Läufe auf Kopien von 1049 und 1045), `DesinfektionTests` und den bunit-Fällen in
`EPOS.UI.Tests/Dialoge/PufferSpProjektDialogTests` und `Seiten/SimulationKonfigSeiteTests`.

## 22. Projektkopien der Brennstoffe, Konditionierungsvorlagen und Pufferauslegungs-Vorgaben

Jedes Projekt rechnet mit eigenen Kopien der drei Kataloge, die der Katalogabgleich (Abschnitt 20)
aktualisiert. Der Abgleich fasst nur den Stamm an; ein Projekt rechnet nach einem Update wie vorher,
bis der Anwender eine Kopie bewusst auf den Katalog zurücksetzt.

| Katalog | Projektkopie | Entsteht | Gelesen über |
|---|---|---|---|
| `Tab_Brennstoff_Stamm` | `Tab_Brennstoff` (STRICT, je Projekt und Brennstoffart) | Schemaschritt (je Projekt jede Brennstoffart), Vorstufe des Abgleichs, „Aus Katalog übernehmen" | `ProjektBrennstoffe.Sicht(idProjekt)` |
| `Tab_Konditionierungsvorlage_STAMM` samt Vorgaben, Kalender, Perioden | Matrix und Kalender des Projektgebäudes bzw. der Zone (`ID_Gebaeude`, `ID_Zone`) | „Vorlage übernehmen" kopiert den Inhalt | `Konditionierungdatenweg` (nur Projektzeilen) |
| `Tab_PufferAuslegungParameter_STAMM` | `Tab_PufferAuslegungParameter` (STRICT, je Projekt und Schlüssel) | erste gespeicherte Auslegung, Vorstufe des Abgleichs | `PufferAuslegungParameter.Lesen(idProjekt)` |

**Brennstoffe.** Die Brennstoffart bleibt die ID des Stammsatzes: Gerät (`Tab_Heizkessel.Brennstoff`,
`Tab_BHKW.Brennstoff`), Träger (`energy_carrier.ID_Brennstoff`), Umrechnung und Referenzkessel
(`Tab_ProjektWirtschaftlichkeit.RefKessel_ID_Brennstoff`) führen sie, und der Kern verzweigt über ihre
Nummernbereiche (Gas, Öl, Strom). Die Kopie hängt über `(ID_Projekt, ID_Brennstoff)` am Projekt und
trägt alle Fachspalten des Stamms — Kategorie, Name, Einheiten, Heizwerte, Emissionsfaktoren
(CO₂, SO₂, NOₓ, Staub), Primärenergiefaktor, Preisvorgaben — und `Katalogfassung_Herkunft`. Wer im
Projekt einen Wert des Brennstoffs liest, nimmt `ProjektBrennstoffe.Sicht`: die Kopie des Projekts,
für eine dem Projekt noch unbekannte Brennstoffart der Stamm. So lesen die Ebene STAMM der
Emissionskette (`EmissionsFaktorLader`, Gerätebrennstoff in `Emissionsquelle`), die
Emissionsbilanz (biogene Einstufung, Referenzkessel), die BEHG-Einstufung des Berichts
(`KostenEmissionRechner`), die Wirtschaftlichkeit (Kategorie je Anlage, Heizöl-BHKW,
Referenzkessel), die KWKG-Anlagenliste, die Vorgabepreise eines neuen Trägers (Assistent,
Trägervariante) und die Pufferauslegung (Brennstoff des Kessels). Ohne Projekt — Katalogpflege,
Energieträgerkatalog — gilt der Stamm. Die Kopie ist vollständig (je Projekt jede Brennstoffart),
weil sich die benutzte Brennstoffart eines Projekts über die Rückfallketten (Stromträger,
Referenzkessel-Vorgabe, Gerätebrennstoff ohne Träger) nicht abschließend bestimmen lässt.

**Verwaltung:** Administration → Kosten → „Brennstoffe des Projekts…" (`ProjektBrennstoffeDialog`,
Hülle `ProjektBrennstoffeHuelle`): Liste mit Abweichung vom heutigen Katalog je Feld, „Bearbeiten"
(Heizwerte, Emissionsfaktoren, Primärenergiefaktor, Preisvorgaben; Kategorie, Name und Einheiten
bleiben), „Auf Katalog zurücksetzen" und „Aus Katalog übernehmen" für eine Brennstoffart, die das
Projekt noch nicht führt. Jede Handlung schreibt sofort und je Satz.

**Konditionierungsvorlagen.** Kein Projektgebäude und keine Zone verweist über eine ID auf eine
Vorlage. „Vorlage übernehmen" kopiert ihren Inhalt in die Matrix und den Kalender des Ziels; die
Herkunft steht nur als Text in `Bemerkung`. Der Lauf liest ausschließlich diese Projektzeilen. Die
Projektkopie besteht damit schon, eine eigene Tabelle braucht es nicht; das Kennzeichen `ReadOnly`
bleibt am Stamm.

**Die Kopie trägt die Nutzung.** „Vorlage übernehmen" schreibt die `Nutzung` der Vorlage (`WOHNEN`,
`BUERO`, `SCHULE`, `SONSTIGE`) in die Spalte `Nutzung` des Kalenders am Gebäude bzw. an der Zone;
Bearbeiten behält sie, solange die Herkunft dieselbe Vorlage nennt, ein Kalender ohne Herkunft trägt
keine. Die Vorbelegung des Nutzungsprofils der Pufferauslegung liest allein diese Spalte der
Projektkalender, nie den Vorlagenkatalog: Umbenennen, Löschen oder Katalogabgleich einer Vorlage
ändern sie nicht. Duplizieren, Projektpaket und Katalogbau-Übernahme tragen die Spalte mit.
Schemaschritt `KonditionierungNutzungSchema` (176) legt sie an und füllt bestehende Kalender einmalig
aus der Herkunft in `Bemerkung` (Vorlage gleichen Namens und gleicher Größe; ohne Treffer leer).

**Vorgaben der Pufferauslegung.** Die Kopie entsteht mit der ersten gespeicherten Auslegung eines
Projekts (`PufferAuslegungCtrl.Speichern`). `PufferAuslegungParameter.Lesen(idProjekt)` legt die
eingebauten Vorgaben, darüber den Stamm und darüber die Kopie; die Herkunftszeile der Vorbelegung
nennt die Projektkopie.

**Schema und Migration.** Schemaschritt `ProjektkopienKatalogeSchema` (175): beide Tabellen
(`ON DELETE CASCADE` mit dem Projekt, eindeutig je Projekt und Brennstoffart bzw. Schlüssel), dann
die Saat — wertgleich, typgleich, wiederholbar. Duplizieren und Projektpaket tragen die Kopien über
den generischen Plan (`ID_Brennstoff` zeigt in beiden Wegen auf die Brennstoffart, im Paket über
den Namen); ein älteres Paket bringt keine Kopien mit, das Projekt liest dann den Katalog des Ziels,
bis die Vorstufe des Abgleichs oder der Projektdialog die Kopie anlegt (Paketanhebung `Ddl`).

**Referenzlauf.** Die Kopien tragen die Werte des Stamms; die sechzehn Projekte rechnen gegen R33
byte-gleich. Gehalten von `EPOS.Kern.Tests/ProjektkopienKatalogeTests` (Saat je Referenzprojekt,
zweiter Lauf, Sicht, Emissionsquelle, Abgleich mit Paket, Jahressummen von 1030 und 1017 vor und nach
einer Katalogänderung, Übernehmen und Zurücksetzen, Duplizieren und Projektpaket) und
`EPOS.UI.Tests/Dialoge/ProjektBrennstoffeDialogTests`.

## 23. Erdsonde: Sondenfeld mit Entzugsrückwirkung

Die Wärmequelle „Erdreich" mit dem Quellsystem Sonde (`Tab_Energieanlagen.WQ_Typ` = Erdreich,
`WQ_Quellsystem` = Sonde, oder ein Kollektor mit `WQ_Tiefe` über 10 m) rechnet die mittlere
Soletemperatur je Stunde aus dem Entzug des Laufs. Der Erdkollektor bleibt beim Jahresgang nach
Kusuda (Abschnitt Erdreichmodell, `ErdreichTemperatur.JahresprofilKollektor`); ein Entzugsabschlag
für ihn entfällt, weil seine Tabellenwerte nach VDI 4640 Blatt 2 Anhang A Leistung **und** Arbeit
begrenzen und der Kusuda-Gang die Jahresdynamik der oberflächennahen Schicht schon trägt — ein
Rückwirkungsmodell für die Fläche bräuchte Rohrabstand und Verlegeschema, die nicht erfasst sind.

### 23.1 Modell

Mittlere Fluidtemperatur zu Beginn der Stunde *t* (Lasten je Sondenmeter *q* in W/m, Entzug positiv):

(1) T_f(t) = T_u − ΔT_V(t) − Σ_{i<t} q_i · [G(t−i) − G(t−i−1)] − R_b · q_{t−1}

(2) G(τ) = 1/(4πλ) · (1/N) · Σ_j Σ_k h(r_jk, τ),  r_jj = r_b

(3) h(r, τ) = ∫_{s₀}^{∞} e^{−r²s²} · Y(Hs, Ds) / (H s²) ds,  s₀ = 1/√(4aτ)

(4) Y(x, d) = 2·ierf(x) + 2·ierf(x+2d) − ierf(2x+2d) − ierf(2d),  ierf(X) = X·erf(X) − (1 − e^{−X²})/√π

(5) T_u = T_m + 1,5 K + 0,03 K/m · max(0, H/2 − 20 m)

- **g-Funktion:** mittlere Wandtemperatur der endlichen Linienquelle mit Spiegelquelle nach
  Claesson und Javed (Gl. 3, 4), über das Feld gemittelt bei gleicher Last je Sonde (Gl. 2). Für
  H → ∞ geht (3) in die unendliche Linienquelle E₁(r²/4aτ) über. Gewählt, weil sie Kurz- und
  Langzeitverhalten in **einer** Formel trägt, Sondenzahl und Abstand über die Paarabstände
  r_jk direkt abbildet und keine Tabellen der Eskilson-Funktionen braucht; das Monatsverfahren nach
  VDI 4640 Blatt 2 liefert nur Monatswerte und taugt nicht für die Stundenkopplung an die
  Kennlinie. Die Auslegungsprüfung nach Tabelle B2 bleibt unverändert daneben stehen.
- **Rechenzeit:** G wird beim Aufbau einmal auf einem logarithmischen Zeitraster (1 h bis
  Betrachtungsjahr · 8760 h, 25 Punkte je Dekade, Simpson in ln s) berechnet und daraus für jede
  ganze Stunde 0…8760 tabelliert. Die Faltung (1) läuft damit über ganze Stundenabstände als
  Tabellenzugriff: 8760 · 8759 / 2 ≈ 38 Mio. Multiplikationen je Feld und Jahr, ohne
  Aggregationsfehler. Eine Lastaggregation (Tages- und Monatsblöcke) ist nur nötig, wenn die
  Messung die Grenze von 200 ms je Feld überschreitet.
- **Vorjahre:** Das Rechenjahr ist das Betrachtungsjahr *n* (Vorgabe 10). Die n − 1 Vorjahre
  tragen die Stundenlast des Feldes aus einem ersten Feldlauf (Abschnitt 23.4, Gl. 6 und 7).

### 23.2 Kopplung im Stundenschritt

Der Entzug der Stunde ist Wärme minus Strom der Module an der Anlage (dieselbe Größe wie in der
Erdreichprüfung); mehrere Module derselben Anlage teilen ein Feld. Die Quelltemperatur der Stunde
*t* entsteht am Ende der Stunde *t − 1* (`Zweikanalig_StundeEnde`) aus den Lasten bis *t − 1*
(**Vorstunde**). Ein Fixpunkt in der Stunde hieße, die Kaskade der Stunde mehrfach zu rechnen —
Speicher, Ebenen und Takt ändern dabei ihren Zustand. Die thermische Zeitkonstante des Bohrlochs
liegt bei Stunden, die Vorstunde verfehlt nur den Widerstandsanteil R_b · q in der ersten Stunde nach
einem Einschalten. Die Reihe steht in `SimulationWaermepumpe.Quelltemperaturen` und damit in
`wp_quellentemperatur.csv`, in der Kälteseite und in der Erdreichprüfung.

### 23.3 Eingaben

| Größe | Quelle |
|---|---|
| λ, ρ·c_p (a = λ/ρc_p) | Bodentyp `WQ_Bodentyp` aus dem Katalog nach VDI 4640 Blatt 1, Tabelle 1 (`ErdreichTemperatur.Katalog`) |
| T_m | Jahresmittel der Außentemperatur (wie bisher) |
| H, N | `WQ_Tiefe` (Länge je Sonde), `WQ_Anzahl` (mindestens 1) |
| Abstand B | `WQ_Sondenabstand`, Vorgabe 6 m (Bezug der Tabelle B2) |
| Anordnung | `WQ_Sondenanordnung`: `Quadratisch` (Vorgabe, möglichst quadratisches Raster, zeilenweise gefüllt) oder `Reihe` (alle Sonden in einer Linie) |
| r_b | `WQ_Bohrlochdurchmesser` / 2, Vorgabe 150 mm (r_b = 0,075 m, Bezug der Tabelle B2) |
| R_b | `WQ_Bohrlochwiderstand`, Vorgabe 0,10 m·K/W (Doppel-U 32 × 3,0, Verfüllung λ = 0,8 W/(m·K), turbulent) |
| D | `WQ_Kopfueberdeckung`, Vorgabe 2 m |
| n | `WQ_Betrachtungsjahr`, Vorgabe 10 |

Abstand, r_b, R_b, D, n und die Anordnung stehen im Parameterobjekt `Sondenfeldgeometrie` mit den
Normwerten als Vorgabe; `WaermequelleClass.SondenfeldgeometrieDerAnlage` liest sie je Anlage aus den
Spalten von `Tab_Energieanlagen` (Schemaschritt 195, `ErdsondenfeldSchema`):

| Spalte | Typ und Prüfung | Vorgabe |
|---|---|---|
| `WQ_Sondenabstand` | REAL, m, `CHECK` > 0 | 6,0 |
| `WQ_Bohrlochdurchmesser` | REAL, mm, `CHECK` > 0 | 150 |
| `WQ_Bohrlochwiderstand` | REAL, m·K/W, `CHECK` > 0 | 0,10 |
| `WQ_Kopfueberdeckung` | REAL, m, `CHECK` ≥ 0 | 2,0 |
| `WQ_Betrachtungsjahr` | INTEGER, `CHECK` ≥ 1 | 10 |
| `WQ_Sondenanordnung` | TEXT, `CHECK` in (`Quadratisch`, `Reihe`) | Quadratisch |

NULL heißt Vorgabe; mit allen Spalten leer rechnet das Feld bitgleich mit der Norm. Ein unbrauchbarer Wert
fällt auf die Norm (`Sondenfeldgeometrie.Bereinigt`). Gepflegt werden die Werte im Erdreichdialog (Zweig
Erdsonde, leeres Feld = Vorgabe, der Platzhalter nennt sie) über `ErdsondenfeldCtrl`; die Spalten sind
Fachspalten und überstehen den Speicherweg des Assistenten über dessen Rettung.

### 23.4 Zweiter Feldlauf: Vorjahre aus der eigenen Last

Die Last der Vorjahre ist unbekannt, bevor der Lauf rechnet. Deshalb rechnet ein Projekt mit
Sondenfeld den Simulationsdurchgang (`SimulationControl.Do_Simulation_Intern`) zweimal:

1. **Erster Lauf** mit einer Startschätzung der Vorjahre: aus der VDI-4640-Vorprüfung der Anlage
   (Jahresentzug = Σ Q_N · (1 − 1/COP) · Volllaststunden der Klimazone), nach Heizgradstunden der
   Außentemperatur (Heizgrenze 15 °C) auf zwölf Monatsblöcke verteilt; ohne Vorprüfung (kein
   Normpunkt, keine Klimazone) ohne Vorjahre. Der Lauf sammelt je Feld die stündliche Nettolast
   q_V,i = Entzug minus Rückspeisung (Abschnitt 23.5).
2. **Zweiter Lauf** über denselben Durchgang: Die n − 1 Vorjahre tragen genau diese Stundenlast,
   das Rechenjahr ist Jahr n. Das gilt auch für Projekte ohne Klimazone.

Weil alle Vorjahre dieselbe Reihe tragen, fassen sich ihre Pulsantworten zu einem Kern über
Stundenabstände m = −8760 … 8758 zusammen, und der Beitrag der Vorjahre ist eine einzige Faltung
über ein Jahr:

(6) S(m) = Σ_{y=1}^{n−1} [G(m + 1 + 8760y) − G(m + 8760y)]

(7) ΔT_V(t) = Σ_{i=0}^{8759} q_V,i · S(t − 1 − i)

- **Keine Aggregation:** (7) ist stundengenau und kostet 8760² ≈ 77 Mio. Multiplikationen je Feld,
  gemessen rund 50 ms. Monats- oder Tagesblöcke der Vorjahre sparen dagegen nichts, was zählt, und
  verschmieren die Stundenspitzen der letzten Vorjahreswochen vor dem Rechenjahr.
- **Wo der zweite Lauf ansetzt:** am ganzen Durchgang nach Wärme- und Strombedarf, die unverändert
  bleiben. Die Wärmepumpe rechnet in der Stundenschleife der Speicherstufe zusammen mit Speichern,
  Kessel und BHKW; ein Durchgang nur der Wärmepumpe hätte keinen eigenen Zustand. Der Durchgang ist
  wiederholbar: Mit erzwungenem zweitem Lauf rechnen alle Projekte der Referenzbasis byte-gleich.
- **Protokoll:** Die Meldungen des ersten Laufs verwirft der Lauf (`SimulationProtokoll.Merken`,
  `ZuruecksetzenAuf`); stehen bleiben die des zweiten. Die Zeile je Feld nennt Betrachtungsjahr,
  Zahl der Vorjahre, Jahresentzug und Rückspeisung der Vorjahreslast.
- **Rechenzeit Projekt 1029:** 511 ms mit einem, 705 ms mit zwei Feldläufen (warmer Prozess, ganzer
  `Simuliere`-Aufruf samt Bedarf); im kalten Prozess 1045 ms gegen 1310 ms.

### 23.5 Regeneration

Kühlwärme, die eine Wärmepumpe mit Erdreichquelle im Kühlbetrieb abgibt, geht als negative Last in
das Feld ihrer Anlage:

(8) Q_R,h = Σ_e [Q_K,e,h + P_e,h / (1 + h_e)]

mit der gedeckten Kälte Q_K (Verdichter und freie Kühlung über die Sole), dem Kältestrom P samt
Hilfsstromzuschlag und dem Hilfsstromanteil h (`Kuehl_Hilfsstromanteil`); P / (1 + h) ist die
Verdichterarbeit, bei freier Kühlung die Pumpenarbeit. Gezählt werden die Kälteerzeuger e, deren
Modul am Feld hängt (`Tab_WP.Kuehlbetrieb` gesetzt, Kühlkennlinie im Projekt). Weil die
Kältekaskade nach der Wärmekaskade rechnet, liefert der erste Lauf Q_R; der zweite meldet sie in
jeder Stunde mit dem Entzug, q_i = (Entzug_i − Q_R,i) / (N·H), und die Vorjahre tragen die
Nettolast (Gl. 7). Die Kälteseite des zweiten Laufs liest ihre Quelltemperatur aus diesem Feld.

Kältemaschinen speisen nicht ins Erdreich: Ihre Rückkühlung (Luft, Trocken-, Nass- oder
Wasserkühlwerk) arbeitet gegen die Umgebung. Ein Projekt ohne Kühlbetrieb rechnet bitgleich wie
ohne Regeneration.

### 23.6 Vorschau im Erdreichdialog

Die Vorschau zeigt die ungestörte Erdreichtemperatur T_u (Gl. 5) als Linie; der Hinweis darunter sagt,
dass die Soletemperatur im Lauf mit dem Entzug sinkt und ihr Verlauf im Ergebnis steht. Den Verlauf
des letzten Laufs zeigt der Dialog nicht.

### 23.7 Erdreichquellen der Referenzprojekte

- Die Sole-Wärmepumpen der Referenzprojekte 1008, 1017, 1023, 1039, 1047, 1050, 1055 und 1056 (dazu
  die Beispielprojekte 1019 und 1027) führen `WQ_Typ` = Erdreich mit einer Sonde in Mergel/Lehm,
  ausgelegt nach VDI 4640 Blatt 2: spezifische Entzugsleistung nach Tabelle B2 (λ des Bodens) mit
  Energiegrenze, Länge = Entzugsleistung / (q_spez · N), N so, dass H zwischen 60 und 120 m liegt;
  ihre Klimaregion liegt in Klimazone 6. Die Saat steht in
  `Referenzlaeufe/Skripte/erdreichquellen_referenzprojekte.py`.
- Das Sonden-Referenzprojekt 1057 ist eine Kopie von 1029 (4 Sonden zu 90 m, Mergel/Lehm,
  Klimazone 6), gehalten von `EPOS.Kern.Tests/ErdsondeReferenzprojektWacheTests`.
- Einfrierregel „gesäte Erdreichquellen der Referenzprojekte“: die Quellfelder und Sondenfeldspalten
  der Referenzanlagen, die Klimazone, der Bodenkatalog, die Normgeometrie und die Festwerte der
  Klasse `Erdsondenfeld` sowie das Anlegen oder Entfernen eines Referenzprojekts mit Erdreichquelle.
- Wirkung in der Basis `2026-10-07_R40_Erdreichquellen`: Bei den Kühlprojekten 1047 und 1056 steigt
  die JAZ (3,86 → 4,45 bzw. 3,73 → 4,33), die Kälte-EER mit ihr (4,6 → 5,2). Bei den
  Grundlastanlagen 1008 und 1039 fällt sie (4,15 → 3,94 bzw. 3,25 → 3,06), weil das Erdreich unter
  der Last der Sonde auskühlt (Sole im Mittel 4,5 bzw. 1,0 °C gegen 9,9 °C Außenluft). Bei 1023 und
  1050 bleibt sie praktisch gleich (2,38). 1057 rechnet JAZ 3,07 bei 22,12 MWh Strom. Die
  Erdreichprüfung führt je Projekt einen Block `Erdreich[n].*`; die Kältemaschine von 1055 bleibt
  unberührt.
