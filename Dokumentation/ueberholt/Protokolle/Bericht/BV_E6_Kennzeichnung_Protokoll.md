# BV-E6 — Kennzeichnung in der App (Protokoll)

Etappe BV-E6 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitte 9 und 13). Auftrag #544, Anwenderauftrag vom 26.09.2026: „starte BV-E6 und dann BV-E7“. Der gültige Stand
steht im Konzept (Rev. 8) und in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md); hier steht, wie es
geworden ist. Vorgänger: [`BV_E5_Tabellen_Bilder_Protokoll.md`](BV_E5_Tabellen_Bilder_Protokoll.md). Zweig
`claude/intelligent-bohr-hthrk8`, beide Agentenzweige ab `fce6060f`, umgesetzt am 26.09.2026; Opus 5.5 hat
orchestriert, zwei Agenten (Opus 5.5) haben in eigenen Worktrees die Infrastruktur der Marke (W1) und die Marken an den
Seiten samt Ortstabelle und Wachen (W2) gebaut, ein Abschluss-Agent hat beide zusammengeführt, die Nahtstellen
behoben, das Gate gezogen und die Papiere geschrieben — 7 Commits ohne Merges, 57 Dateien, +6.040/−312 Zeilen. Kein
Schemaschritt, kein Rechenweg berührt (Referenzlauf GESAMT: PASS), keine Referenzbasis neu eingefroren. `EPOS.iOS/`
ist berührt (`IosZwischenablage`, `MauiProgram`).

| Agent | Gegenstand | Commits | Zusammenführung |
|---|---|---|---|
| W1 Marke, Ansicht, Zwischenablage | `Vorlagenfeldknopf`, `Vorlagenfeldansicht`, Anzeige-DTO und Halter, `IZwischenablage` mit Windows- und iOS-Adapter, Umschalter und leise Zeile, „In der App zeigen“ im Platzhalterkatalog, Stilblock, Markenprobe | `a23f5747`, `f9525a9c`, `f6e2fe74` | `f9fe6a64` |
| W2 Marken am Ort, Orte, Wachen | Parameter an `Kennzahlkachel`, `DiagrammSvg`, `Vergleichstabelle`, `Textfeld`; Schlüssel aus den Hüllen; `Vorlagenfeldorte`; Abdeckungs-, Orts- und Anzeigewertwache | `ab5ccd1c`, `81e2e054` | `26f2a118` |
| Abschluss | Nahtstellen, `origin/ios_migration_september`, Gate, Papiere | `9a9b16ad`, `04f0e73e` | `aa198657` |

---

## 1 Marke und Anzeige (W1)

### 1.1 Die Marke

`EPOS.UI/Bausteine/Vorlagenfeldknopf.razor` ist ein voller 44-px-Knopf mit dem Symbol `{ }` in der Titelzeile bzw.
Randspur seines Wirtes. Er nennt genau einen Katalogschlüssel und nimmt Angaben und Texte aus dem Halter, nie aus
einer Fachklasse. Drei Stellungen: **Aus** — nur ein leerer, verborgener Anker (`hidden`, Klasse
`epos-vorlagenfeld-anker`, `display: none`) mit `data-vorlagenfeld` und `data-vorlagenfeldstufe`, damit Wachen und
Proben den Ort finden; **Marken** — das Symbol, Überfahren oder Fokus öffnet die Aufklappung (reine Stilregel), Klick
heftet an, Esc, Schließfläche oder zweiter Klick lösen; **Schlüssel** — der Schlüssel als Chip, ein Klick kopiert. Die
Aufklappung nennt Schlüssel, Beschreibung, Art und Kontext, Beispiel samt Einheit, Leerwert, Excel-Name (bzw. „in Excel
nur als Listenzeile“ / „als Diagramm auf dem Tabellenbereich“), Stufe mit Hinweis, bei Tabellen den Zusatz, den
Hinweis je Art sowie „Kopieren“ und „Platzhalter ausblenden“. `aria-label` in „Marken“: „Angaben zeigen und
kopieren“; „kopiert“ steht 1,5 s, das Aufleuchten nach „In der App zeigen“ 2,5 s.

### 1.2 Kopieren und Zwischenablage

`Vorlagenfeldkopie.Fuer` bildet den Text je Art (`{{schlüssel}}`; Tabelle, Liste, Kapitel mit „eigener Absatz“; Bild
mit Alternativtext; `stand.*` samt Blockrahmen `{{#je stand}}` … `{{/je}}`). `IZwischenablage` (`EPOS.UI/Dienste`)
hat zwei Adapter: `WindowsFormsApplication1/Allgemein/Blazor/WindowsZwischenablage.cs` (registriert in
`BlazorDienste.cs`) und `EPOS.iOS/Dienste/IosZwischenablage.cs` (registriert in `MauiProgram.cs`). Ohne Adapter oder
wenn er scheitert, steht der Text markiert in einem nur lesbaren Feld der Aufklappung.

### 1.3 Zustand, Umschalter, leise Zeile

`Vorlagenfeldansicht` ist ein Singleton im Dienstverzeichnis (unter Windows teilen alle WebView-Wurzeln eines, auf
iOS die MAUI-App); jede Marke meldet sich an und zeichnet über `InvokeAsync`. `Vorlagenfeldumschalter` sitzt in
`BerichteKostenSeite.razor` und `SimulationSeite.razor`; `Vorlagenfeldzeile` nennt in „Schlüssel“ die Zahl der
Platzhalter = verschiedene Schlüssel der gezeichneten Marken (unter Windows über alle WebViews) und öffnet
„Katalog…“ — ohne Baukasten. Das DTO `Vorlagenfeldanzeige` samt `Vorlagenfeldhalter` füllt die Hülle
`EPOS.UI.Daten/Bericht/VorlagenfeldanzeigeHuelle.cs` aus dem Katalog.

### 1.4 „In der App zeigen“

`Vorlagenfeldzeige.Waehle` nimmt den ersten lesend erreichbaren Ort, der kein Assistent ist, sonst den schwersten
Grund (Assistent vor „nur in einer Eingabemaske“ vor „kein Ort“); `Zeigen` schaltet „Marken“ ein, setzt den
Leuchtschlüssel und öffnet die Ansicht über `Dienste.Navigation` mit dem Reiter als erstem Argument. Ohne Ort ist der
Knopf im Platzhalterkatalog weich gesperrt und nennt den Grund.

### 1.5 Texte, Stil, Probe

43 Ressourcen `VF_KNOPF_*`, `VF_ANZEIGE_*`, `VF_KATALOG_*` de/en; Stilblock „PLATZHALTER IN DER APP“ in
`epos-ui.css` vor dem Block „Formularraster“ (`FormularrasterTests` liest ab dessen Überschrift bis Dateiende);
Anmeldung der Anzeigestufe in `KiMaskenabdeckungWacheTests`. Browserprobe `/vorlagenfeldprobe` mit
`vorlagenfeldprobe.mjs`: zehn Varianten mit echten Katalogschlüsseln in drei Stellungen bei 1.280 × 900.

## 2 Marken am Ort (W2)

### 2.1 Parameter und Hüllen

`Kennzahlkachel`, `DiagrammSvg`, `Vergleichstabelle` und `Textfeld` tragen optional `Vorlagenfeld`,
`VorlagenfeldStufe`, `VorlagenfeldHinweis` (Tabelle: `VorlagenfeldZusatz`); ohne Parameter bleibt das Markup
unverändert. Die Marke steht in einer eigenen Spur oben rechts (Stilblock „PLATZHALTERMARKEN AM ORT“, nur
Anordnung). Welcher Schlüssel an einem Wert steht, entscheiden die Hüllen (`KostenSeiteGaben`,
`WirtschaftlichkeitSeiteGaben`, `SimulationErgebnisHuelle`): Stamm oder Variante, Szenario, beste Variante. Elf
Hinweise `VF_ORT_*` de/en.

### 2.2 Die Ortstabelle

`EPOS.UI/Dienste/Vorlagenfeldorte.cs`: 53 Zeilen — Berichte & Kosten › Wirtschaftlichkeit 18, › Kosten 6 (drei
Kacheln je Stamm und Variante), › Übersicht 1; Simulation › Ergebnis › Übersicht 20 (neun Kennzahlen je Stamm und
Variante, zwei Ringe), › Wärmepumpe 1, › Stromspeicher 1; Projektassistent › Projektkopf 6 (nicht lesend, nie ein
Sprungziel).

### 2.3 Wachen

- `VorlagenfeldAbdeckungWacheTests` (UI): jede `Kennzahlkachel`, jedes `DiagrammSvg`, jede `Vergleichstabelle` unter
  `Seiten/Berichte` und `Seiten/Simulation` trägt ein `Vorlagenfeld` oder steht als benannte Ausnahme mit Grund; eine
  veraltete Ausnahme macht die Wache rot. Ausnahmen: Leerzustand ohne Lauf (Wärmebedarf aus der Bedarfsrechnung),
  Deckung Wärme/Strom über alle Erzeuger im Dashboard, Kältering, 13 Erzeuger- und Bedarfsbilder (`bild.ergebnis.*`
  vorgemerkt, nicht in Fassung 4), drei Kacheln der Autarkieanalyse und die Kacheln des Speicherlaufs ohne Schlüssel.
- `VorlagenfeldorteWacheTests` (UI): jede Zeile ein Katalogschlüssel mit `Seit` ≤ Fassung, jeder Ort eine bekannte
  Ansicht mit bekanntem Blatt, jede literale Marke der Seiten eine Zeile, jeder Schlüssel als Parameter in Seite oder
  Hülle.
- `VorlagenfeldAnzeigewertWacheTests` (Kern): für 1030, 1019 und 1017 steht jede gesetzte Marke der Hüllen in der
  Tabelle, und ihr Wert ist der aufgelöste Katalogwert; eine Formatabweichung N2/N0 hält die Wache mit `|stellen 2`.
- bunit `VorlagenfeldmarkenTests`: die vier Bausteine mit und ohne Marke.

## 3 Zusammenführung und Nahtstellen

- **Merge W1** (`f9fe6a64`) konfliktfrei. **Merge W2** (`26f2a118`): add/add in `Vorlagenfeldknopf.razor` und
  `Vorlagenfeldstufe.cs` → Fassung W1 (die Stubs von W2 zeichneten nur `data-vorlagenfeld`/`data-vorlagenfeldstufe`,
  beides trägt W1s Marke in jeder Stellung); `Vorlagenfeldorte.cs` → Fassung W2 (die Form `Vorlagenfeldort(Ansicht,
  Reiter, Element, NurLesend)` ist auf beiden Seiten gleich); beide `.resx` als Vereinigung (keine Duplikate, BOM und
  LF erhalten), `Resource.Designer.cs` mit `designer_neu.py schreiben` unverändert (12.398 Einträge); CSS beider Blöcke
  automatisch zusammengeführt, W1-Block vor dem Formularraster; Zähler der KI- und Textbündel-Wachen grün ohne Eingriff.
- **Sprungziel der Ergebnisansicht** (`9a9b16ad`): Unter Windows leitet `WinFormsNavigation` `SIMULATION_ERGEBNIS`
  nicht weiter. Die Orte der Ergebnisansicht tragen deshalb die Ansicht `SIMULATION` und als Reiter die Marke
  `schritt=3;blatt=<Blatt>` (`Vorlagenfeldorte.Ergebnisblatt`, über `SimulationMarke.Schreiben`), „Berichte & Kosten“
  die Seite (`WIRTSCHAFT`, `KOSTEN`, `UEBERSICHT`); beide Wege nehmen `AppWurzel` (iOS) und `WinFormsNavigation`
  (Windows) an. Die Ortswache prüft die Marke; neuer bunit-Fall
  `PlatzhalterkatalogZeigenTests.Ein_Ort_jeder_Ansicht_der_Tabelle_springt_auf_beiden_Schalen` (sechs Orte der echten
  Tabelle, Fake-Navigation).
- **W2-bunit gegen W1s Marke:** `VorlagenfeldmarkenTests` grün mit den Attributen des echten Ankers.
- **Kopfzeilen und Tabellenzeilen im Browser** (`04f0e73e`): neue Seite `/vorlagenfeldwirte` (die drei Bausteine je
  ohne und mit Vorlagenfeld), gemessen von `vorlagenfeldprobe.mjs`. In „Aus“ keine Verschiebung (Kachel 80,97 px,
  Diagramm 262,83 px, Titel, Wert, Leiste, Bild und Tabelle auf gleicher Höhe); Tabellenzeilen in allen drei Stellungen
  26,69 / 42,64 px wie ohne Marke; in „Marken“ und „Schlüssel“ wächst die Kachel auf 106,78 px, das Diagramm auf
  280,83 px (44-px-Spur), die Marke überdeckt weder Kacheltitel noch Zoomleiste.
- **Merge `origin/ios_migration_september`** (`aa198657`, 21 Commits, darunter E29 #536): konfliktfrei; Suche nach
  Konfliktmarkern leer.

## 4 Entscheidungen

### 4.1 Entscheide der Orchestrierung (nach Empfehlung der Agenten)

- Die Kostenkacheln tragen „ähnlich im Bericht“: die Kostenerfassung zeigt Cent, der Bericht rechnet aus der
  Wirtschaftlichkeit in ganzen Euro (`VF_ORT_KOSTEN`).
- Die Komponententabelle der Übersicht trägt `tabelle.komponenten.matrix` (ähnlich) statt `tabelle.vergleich`.
- Deckungsgrad und JAZ Kälte tragen jetzt „ähnlich“ mit dem Hinweis `VF_ORT_KENNZAHL_RECHENWEG`; die Angleichung ist
  ein eigener Auftrag.
- Der verborgene Anker in „Aus“ bleibt (Festlegung für die Wachen).
- Die Zahl der Platzhalter zählt wie gebaut (verschiedene Schlüssel der gezeichneten Marken).

### 4.2 Abweichungen vom Plan mit Grund

- **Befund Rechenweg:** Deckungsgrad und JAZ Kälte des Projekts 1017 weichen zwischen Dashboard und Katalog ab
  (98,4 gegen 98,5 %, 4,52 gegen 4,51) — das Dashboard rechnet aus anderen Summen als der Wertesatz.
- Der Strombedarf im Dashboard (mit Eigenverbrauch) trägt keine Marke: kein Katalogschlüssel mit derselben Abgrenzung.
- Die Klimaregion trägt keine Marke.
- Konzept 9.7 nannte als `aria-label` „Platzhalter … kopieren“; in „Marken“ lautet es „Angaben zeigen und kopieren“,
  weil der Klick dort die Aufklappung öffnet.
- Konzept 9.4 sah in „Aus“ keine Spur vor; es steht ein verborgener Anker ohne Layout (siehe 4.1).
- „Katalog…“ der leisen Zeile öffnet den Katalog ohne Baukasten.
- Aufleuchten 2,5 s.

## 5 Abnahme

**Tests der Etappe:** `VorlagenfeldTests`, `VorlagenfeldanzeigeHuelleTests`, `VorlagenfeldmarkenTests`,
`PlatzhalterkatalogZeigenTests`, `VorlagenfeldAbdeckungWacheTests`, `VorlagenfeldorteWacheTests`,
`VorlagenfeldAnzeigewertWacheTests`, `KiMaskenabdeckungWacheTests`, `BerichtsvorlagenTextbuendelWacheTests`.

**Gate auf `aa198657`** (nach allen Merges): Kern-Filter Release und Windows-Schale (Linux, `EnableWindowsTargeting`) je 0 Fehler; Tests EPOS.Kern 8.244 bestanden / 1 übersprungen / 1 rot (`TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar` — Umgebung, Container mit Python 3.11, wie in #532 und #541; auf der CI grün), EPOS.UI 6.651, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 / 1 übersprungen; Designer wiederholbar (12.398 Einträge); SQL-Prüfer 1.991 Texte / 0 Fundstellen; ChartProben 218 Bilder / 0 Verstöße; Referenzlauf 6 Projekte (1030, 1007, 1017, 1045, 1046, 1047) gegen R20 GESAMT: PASS, 2.208.587 Werte.

**Browser:** Markenprobe `vorlagenfeldprobe.mjs` (zehn Varianten und die drei Wirte, drei Stellungen, 1.280 × 900)
kein Verstoß. Rasterprobe 26 von 27 Fällen erfüllt; Z6 (Kategorien in der Überlagerung des TWW-Katalogs, Hülle rollt
quer 47 px) verfehlt identisch auf dem Stand vor BV-E6 (`f46ce962`, Nebenordner gemessen) — Bestand, nicht BV-E6.

## 6 Commitfolge

| Commit | Inhalt |
|---|---|
| `a23f5747` | W1: Platzhaltermarke, Anzeige, Zwischenablage, In der App zeigen |
| `f9525a9c` | W1: Stilblock vor das Formularraster |
| `f6e2fe74` | W1: Anker mit `data-vorlagenfeld`, Markenprobe im Browser |
| `ab5ccd1c` | W2: Marken an Bausteinen und Seiten, Ortstabelle `Vorlagenfeldorte` |
| `81e2e054` | W2: Wachen Abdeckung, Orte, Anzeigewert = Katalogwert |
| `f9fe6a64` | Merge W1 (konfliktfrei) |
| `26f2a118` | Merge W2 (Stubs → W1, Ortstabelle → W2, `.resx` Vereinigung) |
| `9a9b16ad` | Ergebnisorte springen über `SIMULATION` mit `schritt=3;blatt` |
| `04f0e73e` | Markenprobe misst die echten Wirte in drei Stellungen |
| `aa198657` | Merge `origin/ios_migration_september` vor der Abnahme (konfliktfrei) |
| Papier-Commit | „Papiere #544: BV-E6 Kennzeichnung in der App“ — dieses Protokoll, Konzept Rev. 8, Statusdatei, Index, Wiki-Quelle |

## 7 Offen

- **(a) Anwenderabnahme** unter Windows (1.280 px, zehn Varianten) und auf dem iPad (Konzept 13).
- **(b) iOS-Lauf** nach Rückfrage (`IosZwischenablage`, `MauiProgram`).
- **(c) Befund Rechenweg Kälte:** Deckungsgrad und JAZ Kälte Dashboard gegen Katalog (1017) angleichen — eigener
  Auftrag mit Referenzlauf.
- **(d)** `bild.ergebnis.*` für die Erzeugerbilder (Fassung 5) und Schlüssel für die Kacheln der Autarkieanalyse und
  des Speicherlaufs.
- **(e)** „Katalog…“ der leisen Zeile mit Baukasten.
- **(f) Wiki-Upload** der Seite „Berichtsvorlagen“ und Logbuch.
- **(g)** Rasterprobe Z6 (Bestand, siehe 5).

**Logbuch-Vorschlag** (für den nächsten Wiki-Upload, Version beim Anwender zu erfragen, Stichwort `bericht`):

- „In ‚Berichte & Kosten‘ und im Simulationsergebnis zeigt ein Umschalter ‚{ }‘ an Kacheln, Diagrammen und Tabellen
  die zugehörigen Berichtsplatzhalter zum Kopieren.“

## 8 Dateien

7 Commits ohne Merges (Tafel in Abschnitt 6): 57 Dateien, +6.040/−312 Zeilen.

| Bereich | Dateien |
|---|---|
| Bausteine | neu `EPOS.UI/Bausteine/Vorlagenfeldknopf.razor`, `Vorlagenfeldstufe.cs`, `Vorlagenfeldkopie.cs`, `VorlagenfeldTexte.cs`, `Vorlagenfeldumschalter.razor`, `Vorlagenfeldzeile.razor`; geändert `Kennzahlkachel.razor`, `DiagrammSvg.razor`, `Vergleichstabelle.razor`, `EPOS.UI/Standards/Textfeld.razor` |
| Dienste | neu `EPOS.UI/Dienste/Vorlagenfeldansicht.cs`, `Vorlagenfeldanzeige.cs`, `IZwischenablage.cs`, `Vorlagenfeldzeige.cs`, `Vorlagenfeldorte.cs` |
| Seiten und Dialog | `EPOS.UI/Seiten/Berichte/BerichteKostenSeite.razor`, `KostenSeite.razor`, `UebersichtSeite.razor`, `WirtschaftlichkeitSeite.razor`, `WirtschaftlichkeitDaten.cs`, `KapitalwertVerlaufAbschnitt.razor`, `AnhangEChecklisteKnopf.razor`; `Seiten/Simulation/SimulationSeite.razor`, `SimulationErgebnisSeite.razor`, `SimulationErgebnisDaten.cs`, `UebersichtReiter.razor`, `WaermepumpeReiter.razor`, `StromspeicherReiter.razor`; `Seiten/Assistent/ProjektKopfSeite.razor`; `Dialoge/Berichte/PlatzhalterkatalogDialog.razor`, `PlatzhalterkatalogTexte.cs`; `wwwroot/epos-ui.css` |
| Hüllen | neu `EPOS.UI.Daten/Bericht/VorlagenfeldanzeigeHuelle.cs`; geändert `Kosten/KostenSeiteGaben.cs`, `Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs`, `Simulation/SimulationErgebnisHuelle.cs` |
| Schalen | neu `WindowsFormsApplication1/Allgemein/Blazor/WindowsZwischenablage.cs`, geändert `BlazorDienste.cs`; neu `EPOS.iOS/Dienste/IosZwischenablage.cs`, geändert `EPOS.iOS/MauiProgram.cs` |
| Ressourcen | `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |
| Tests | neu `EPOS.UI.Tests/Bausteine/VorlagenfeldTests.cs`, `VorlagenfeldanzeigeHuelleTests.cs`, `VorlagenfeldmarkenTests.cs`, `Dialoge/PlatzhalterkatalogZeigenTests.cs`, `VorlagenfeldAbdeckungWacheTests.cs`, `VorlagenfeldorteWacheTests.cs`, `EPOS.Kern.Tests/VorlagenfeldAnzeigewertWacheTests.cs`; geändert `Dialoge/BerichtsvorlagenTextbuendelWacheTests.cs`, `Dialoge/Hilfe/KiMaskenabdeckungWacheTests.cs` |
| Proben | `Proben/Rasterprobe/vorlagenfeldprobe.mjs`, `Wirt/Seiten/Vorlagenfeldprobe.razor`, `Wirt/Seiten/Vorlagenfeldwirte.razor`, `Wirt/Program.cs`, `LIESMICH.md` |
| Papiere und Wiki (dieser Auftrag) | dieses Protokoll, Konzept Rev. 8, `Dokumentation/aktuell/Status_iOS_Migration.md`, `Dokumentation/LIESMICH.md`, `Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki` |
