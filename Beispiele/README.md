# Projektbeispiele

Quellordner der Anwendungsbeispiele, die unter
`https://epos-plan.de/epos-plan/epos-plan-schulung/beispiele/` veröffentlicht werden.

**Konzept:** `claude/Konzept_Projektbeispiele_Dokumentation.md` — dieses README ist die
Kurzfassung für den Alltag, das Konzept begründet die Festlegungen.

> **Stand: Gerüst.** Angelegt sind Ordnerstruktur, Vorlagen, Schemata und der Rumpf des
> Bauvorgangs. Die Schritte 2 bis 7 von `build-beispiele.ps1` sind noch nicht umgesetzt
> (Pakete B-1 bis B-4 des Konzepts). Das Skript läuft, meldet aber je Schritt, was fehlt.

---

## Der Grundsatz

> **Geschrieben wird nur der Erzähltext. Zahlen, Diagramme und Bildschirmfotos entstehen
> bei jedem Bauvorgang neu.**

Wer eine Jahresarbeitszahl von Hand in `text.md` tippt, hat ein Beispiel gebaut, das mit
der nächsten Programmversion falsch wird. Deshalb: Zahlen ausschließlich als Platzhalter,
Bilder ausschließlich über das Drehbuch.

---

## Ordner

```
Beispiele/
  README.md                     diese Datei
  build-beispiele.ps1           Bauvorgang (Rumpf)
  .gitignore
  schema/                       JSON-Schemata zur Prüfung der Beschreibungsdateien
  werkzeuge/
    neues-beispiel.ps1          legt ein Beispiel aus der Vorlage an
  _vorlage/                     Muster — nicht verändern, sondern kopieren
  10-einfamilienhaus-waermepumpe/
  20-mehrfamilienhaus/
  …
```

Die Zahl im Ordnernamen bestimmt die Reihenfolge auf der Übersichtsseite. Zehnerschritte,
damit sich später etwas dazwischenschieben lässt, ohne alles umzubenennen.

### Innerhalb eines Beispiels

| Datei | Herkunft | Versioniert |
|---|---|---|
| `beispiel.yaml` | von Hand | ja |
| `text.md` | von Hand | ja |
| `schritte.yaml` | von Hand (Entwurf aus dem Mitschnitt) | ja |
| `projekt.epx` | Export aus der Beispieldatenbank | ja |
| `ergebnis.json` | **erzeugt** — Kennzahlen des Referenzlaufs | ja (für den Vergleich) |
| `veroeffentlichung.json` | **erzeugt** — Seiten-ID, Medien-IDs, Prüfsummen | ja (sonst entstehen Dubletten) |
| `bilder/<sprache>/` | **erzeugt** | ja |
| `ausgabe/` | **erzeugt** | nein |
| `mitschnitt/` | Aufzeichnungsmodus, Wegwerfmaterial | nein |

`ergebnis.json` und `veroeffentlichung.json` sehen aus wie Zwischenstände, gehören aber
zwingend ins Repository: Ohne das erste gibt es keinen Kennzahlvergleich, ohne das zweite
legt jede Veröffentlichung neue Seiten und Bilddubletten an.

---

## Ein neues Beispiel anlegen

```powershell
cd C:\Waermeplan\WP_Plan\Beispiele
.\werkzeuge\neues-beispiel.ps1 -Nummer 40 -Slug schwimmbad -Titel "Schwimmbad"
```

Danach in dieser Reihenfolge arbeiten:

1. **Projekt bauen** — in EPOS-Plan gegen `Beispiele.accdb`, mit erfundenen, aber
   plausiblen Randbedingungen. Nie ein Kundenprojekt, auch nicht anonymisiert.
2. **Durchklicken und mitschneiden** (sobald der Aufzeichnungsmodus steht, Paket B-8) —
   liefert den Entwurf von `schritte.yaml`.
3. **`beispiel.yaml` ausfüllen**, besonders die Bezugsliste: Ohne `bereiche` und
   `code_anker` meldet sich das Beispiel später nicht, wenn der Code darunter sich ändert.
4. **`text.md` schreiben** — Gliederung ist verbindlich, siehe unten.
5. **`.\build-beispiele.ps1 -Beispiel <slug>`** und die Meldungen abarbeiten.
6. **`-Veroeffentlichen`** legt die Seite als Entwurf an. Erst `-Freigeben` schaltet live.

---

## Gliederung von `text.md`

Vier Abschnitte, in dieser Reihenfolge, keine anderen Überschriften der ersten Ebene.
Das Muster stammt von der bestehenden Beispielseite und hat sich bewährt:

| Abschnitt | Beantwortet |
|---|---|
| `## Ausgangssituation` | Welches Objekt, welche Ausgangslage, welche Frage? |
| `## Vorgehen` | Welche Bausteine, welche Eingaben geben den Ausschlag? |
| `## Auslegung` | Wie werden die Erzeuger gewählt und eingestellt? |
| `## Worauf zu achten ist` | Welche Ergebnisgröße trägt die eigentliche Aussage? |

Sie schreiben in der Sie-Form, ohne Marketington, und verweisen auf die Grundlagenseiten,
statt Grundlagen neu zu erklären.

### Platzhalter

| Form | Wird ersetzt durch |
|---|---|
| `{{kz:JAZ}}` | Kennzahl aus `ergebnis.json`, formatiert nach `KennzahlenKatalog` |
| `{{kz:JAZ\|variante=V1}}` | dieselbe Kennzahl einer bestimmten Variante |
| `{{bild:wp-kennfeld}}` | Bild mit Unterschrift, `id` aus `schritte.yaml` |
| `{{tab:vergleich}}` | erzeugte Tabelle: `vergleich`, `kennzahlen`, `komponenten` |

Ein Platzhalter, der sich nicht auflösen lässt, bricht den Bauvorgang ab. Das ist
beabsichtigt: Wird eine Kennzahl umbenannt, meldet sich jedes Beispiel, das sie zitiert.

**Keine Zahl ohne Platzhalter.** Ausgenommen sind Angaben, die zur Aufgabenstellung
gehören und nicht gerechnet werden — Baujahr, Wohnfläche, Anzahl Wohneinheiten.

---

## Aktuell halten

```powershell
.\build-beispiele.ps1 -Pruefen
```

Vergleicht `stand.commit` jedes Beispiels mit den Änderungen an seinen `code_anker` und
meldet, welches Beispiel durchgesehen werden muss. Der zugehörige Auftrag an Claude Code
steht im Konzept, Kapitel 10.2.

**Takt:** ein vollständiger Lauf vor jeder Auslieferung, gemeinsam mit dem Setup-Bau. Ein
Beispiel, dessen Zahlen nicht zur ausgelieferten Version passen, schadet mehr als keins.

---

## Festlegungen, die leicht kippen

- **Zeichenkodierung.** `.ps1` als **UTF-8 mit BOM** — Windows PowerShell 5.1 verstümmelt
  sonst die Umlaute. `.md`, `.yaml`, `.json` als **UTF-8 ohne BOM**, weil YAML- und
  JSON-Leser mit einer BOM stolpern können. Das weicht bewusst von der Repo-Regel für
  `.cs`-Dateien ab.
- **Zeilenenden** CRLF für `.ps1`, LF für alles andere (siehe `.gitattributes` der Wurzel).
- **Bilder nie von Hand nachbearbeiten.** Rahmen, Pfeile und Beschriftungen gehören als
  `markierung` ins Drehbuch. Ein im Bildbearbeiter gesetzter Pfeil überlebt die nächste
  Programmversion nicht.
- **`veroeffentlichung.json` nicht von Hand ändern.** Die Seiten- und Medien-IDs darin
  sind der einzige Grund, warum eine Aktualisierung aktualisiert statt zu verdoppeln.
- **Keine Kundendaten**, auch nicht in Projektnamen, Ortsangaben oder Dateinamen.
  Die Aufnahme läuft gegen `Beispiele.accdb` und unter einer neutralen Demo-Lizenz.

---

## Was noch fehlt

| Paket | Inhalt | Konzept |
|---|---|---|
| B-1 | Platzhalterauflösung, Markdown → HTML, Schemaprüfung | Kap. 4, 7 |
| B-2 | `ergebnis.json` aus `SimulationRunner` und `KennzahlenKatalog` | Kap. 7 |
| B-3 | Diagramme über den ScottPlot-Bildpfad des Berichts | Kap. 5.1 |
| B-4 | Veröffentlichung nach WordPress | Kap. 8 |
| B-5 | `Beispiele.accdb` aufsetzen | Kap. 6.1 |
| B-7 | Aufnehmer für Programmmasken | Kap. 5.2 |
| B-8 | Aufzeichnungsmodus | Kap. 5.3 |
| B-11 | Projektexport `.epx` | Kap. 6.2 |

Voraussetzung für B-7 und B-8: `Program.Main` muss Argumente entgegennehmen — heute nicht
der Fall (Konzept Kap. 13, Punkt B1).
