# Konzept: Stromspeicher-Dialoge — eine Ansicht statt Fenster in Fenster

Stand 11.09.2026 · Zweig `ios_migration_september` · Aufgabe #179 · Ablage `Projekte/` (Anwenderhinweis 11.09.2026, Heimat der Stromspeicherunterlagen) · Status: **entschieden am 11.09.2026, SD‑Q1 bis SD‑Q7 nach Empfehlung** (Anwender: „SD-Q1 bis SD-Q7: Empfehlung"). Umsetzung nach dem Stufenplan in Abschnitt 5: P1 = Aufgabe #183 und P2 = #184 parallel, danach P3 und P4; dazu der Befund #185 (Projektlauf bricht mit „Im Dialog fehlen die Investitionskoeffizienten" ab).

Anlass ist die Anwenderrückmeldung vom 11.09.2026 zum Dialog „Auslegung optimieren" der
Speicherflotte, mit zwei Bildschirmfotos:

1. „Die tabellarische Darstellung ist unübersichtlich im Dialog und sollte separat sein."
2. „Die Grafiken sollten auswählbare Graphen haben sowie als sortierte Dauerlinie darstellbar sein."
3. „Unklar: Wie wird die Lastspitzenkappung gestartet? Die Darstellung der Ergebnisse in
   Abhängigkeit der Speichergröße (Kapazität, Leistung, C) ist nicht da."
4. „Die Dialogdarstellung für Stromspeicher ist generell verbesserungsfähig."
5. „Fenster in Fenster ist nicht gut."

Das Mockup der Zielansicht liegt unter
[`Projekte/Mockup_Stromspeicher_Ansicht_2026-09-11.html`](Mockup_Stromspeicher_Ansicht_2026-09-11.html).
Die Codebelege stammen aus der Analyse vom 11.09.2026 (Explore-Agent, nur lesend); die Zeilen
gelten für den Stand `230616c`.

---

## 1. Befund — was der Code heute tut

### 1.1 Wie die Lastspitzenkappung gestartet wird (Antwort auf Punkt 3)

Es gibt **genau einen Startknopf**: „Speichervergleich berechnen" in der Fußleiste des Dialogs
(`EPOS.UI/Dialoge/Strom/SpeicherFlottenDialog.razor:62` → `Starten()` :175–190 → Delegat
`Rechnen` → Windows-Hülle `SimulationErgebnisHuelle.Flotte.cs:44–77` → `SpeicherFlottenStudieCtrl.Rechnen`).
Ein Reiterwechsel rechnet nichts; jede Eingabeänderung setzt nur die Marke „Eingaben geändert".
„Einstellungen speichern" schreibt den Stand `@Aktuell` in `Tab_SpeicherAuslegung` und rechnet
ebenfalls nichts. „Diese Flotte für die Projektsimulation aktivieren" schreibt den Stand
`@Projektflotte`; erst dann fährt die Flotte im gewöhnlichen Projektlauf
(`SimulationControl.Stromspeicher.cs:97–104`). Das Betriebsziel „Lastspitzenkappung" und der
Peak-Zielwert stehen im Baustein `SpeicherFlottenBetriebEditor` (Reiter 1 und noch einmal im
Ergebnisreiter).

Der Ablauf ist also: Speicher anlegen → Daten und Kosten → Betriebsziel und Peak-Ziel → **Berechnen**
→ Ergebnis lesen → optional Aktivieren. Er ist nirgends als Ablauf sichtbar, der Dialogtitel
„Auslegung optimieren" verspricht eine Optimierung, und ohne den Schalter „Speicheranzahl und
Größenbereiche optimieren" rechnet der Knopf eine **einzige** Konfiguration
(`SpeicherFlottenStudieCtrl.cs:195–201`). Die Verwirrung ist berechtigt.

### 1.2 Warum die Flotte im Foto nichts tat — ein Befund, drei Sperren

„Mit Flotte" byte-gleich „Ohne Speicher", „Speicher gesamt" flach auf 0 und die Jahresprojektion mit
Netto-Cashflow = −Betrieb sind **ein** Befund: Die Flotte hat im ganzen Jahr weder geladen noch
entladen. Kein Anzeigefehler — Variante und Referenz werden getrennt gerechnet
(`SpeicherEngine/FlottenSimulator.cs:54–55`), sie sind nur zahlengleich. Drei Sperren, jede reicht allein:

| Sperre | Beleg | Wirkung |
|---|---|---|
| **Peak-Ziel fest 50 kW** als Vorbelegung, unabhängig von der Last (Grundlast 70–80 kW, Spitze 789 kW) | `SpeicherFlottenStudieCtrl.cs:49` und `:64` | Die Anforderung `n − H` ist in jedem Intervall positiv (`FlottenSimulator.cs:427–441`): es wird nie eine Ladung angefordert, und der Ladedeckel `max(0, H − n)` (`:336–344`, Regel „Wiederaufladung erzeugt keinen neuen Peak über H", Spezifikation 5.1) ist dauerhaft 0 |
| **Netzladung verboten** (Vorgabe `NetzladungErlaubt = false`, `FlottenModel.cs:521`) | Standort ohne Überschuss (Einspeisung 0 in beiden Spalten) | zweiter Ladedeckel `max(0, −n) = 0`: Laden ist unabhängig vom Peak-Ziel unmöglich |
| **Start-SoC = SoC-Minimum** (`StartSoCEffektivKwh => StartSoCKwh ?? SoCMinKwh`, `SpeicherParameter.cs:170`; Produktivstandard AP0, `StromspeicherSimCtrl.cs:1389`) | `SpeicherFlottenStudieCtrl.cs:62` | nichts zu entladen: `discharge = max(0, e − floor) = 0` schon im ersten Intervall, die Einheit fällt aus der aktiven Menge (`:468`) |

Folge exakt wie beobachtet: `soll = ist = 0`, Netzleistung gleich Referenz, `CF = 0 − OPEX = −1,00 €/a`,
`NPV = −15 000 − Σ 1/(1,03)^a = −15 014,88 €` (das bestätigt nebenbei Zins 3 % und 20 Projektjahre).

**Was fehlt:** eine Diagnose. Der Simulator zählt Lade- und Entladeenergie je Einheit
(`FlottenSpeicherKennzahlen.LadeenergieAcKWh/EntladeenergieAcKWh`, angezeigt in
`SpeicherFlottenErgebnisAnsicht.razor:63–66`), aber niemand wertet „beides 0" als Warnung, und die
Vorprüfung `EingabenPruefen` (`SpeicherFlottenDialog.razor:191–227`) hält das Peak-Ziel nicht gegen
die Last. Die Spezifikation 5.1 behandelt den Fall „N liegt dauerhaft über H" nicht; sie sagt nur
„Restpeak protokollieren". Dazu SP‑O‑5: Der Leistungspreis wird einmal auf den Jahrespeak
angesetzt (`FlottenSimulator.cs:121–122`) — ein Peak-Ziel von 50 kW vor 789 kW ist wirtschaftlich
nie erreichbar, und die Warnung „Das wirtschaftliche Peak-Ziel wurde nicht erreicht" sagt dem
Anwender nicht, warum.

**Ergänzung „Befund #185" (11.09.2026).** Derselbe Projektlauf brach danach vollständig ab —
`SpeicherAuslegungCtrl.KostenAufloesen` verlangte spezifische Kostensätze bedingungslos und warf
„Im Dialog fehlen die Investitionskoeffizienten", sobald der Dialogstand `@Aktuell` die Quelle
„Dialog" trug, aber keine gepflegten Sätze. Gebraucht werden sie dort nicht: Die Sätze erreichen
ausschließlich `FlottenWirtschaftlichkeit`, während Netzleistung, Ladezustand und Energie des
Projektlaufs keinen einzigen von ihnen lesen — und trägt jede Einheit `EigeneKosten`, überschreibt
`SpeicherFlottenStudieCtrl.Konfiguration` mit ihnen ohnehin nichts. Seither verlangt der
**Studienlauf** sie weiterhin (mit dem Ausweg im Meldungstext), der **Projektlauf** rechnet ohne
sie weiter und kennzeichnet Kapitalwert und Jahreskonten als „nicht bewertbar"
(`KostenPflicht`, `SpeicherFlottenProjektCtrl.Pruefe`, `SpeicherFlottenProjektLauf.KostenBewertbar`).

### 1.3 Jahresprojektion „Betrieb 1,00 €/a" — ein Eingabewert, kein Rechenfehler

`FlottenWirtschaftlichkeit.cs:49–58`: `Betrieb = fix + €/(kWh·a)·C + €/(kW·a)·P`,
`Durchsatz = Entladeenergie·€/kWh`, `Ersatz` nur im Ersatzjahr, `Energieausgleich = ΔEndenergie·€/kWh`,
`Cashflow = (Referenzrechnung − Variantenrechnung) − Betrieb − Durchsatz − Ersatz + Ausgleich`.
Alle Kostensätze sind absolut (`SpeicherAuslegungModel.cs:12–23`); eine prozentuale Kostenposition
des Kostenmoduls wird ausdrücklich ausgelassen und als solche angezeigt
(`SpeicherAuslegungCtrl.cs:155–161`, `SpeicherAuslegungEditor.razor:127–130`). Bei 15 000 € Investition
ist „Betrieb 1,00 €" also der eingegebene Betriebskostensatz mal Größe. **Lücke:** Die Größenordnung
wird nirgends beanstandet — geprüft wird nur „Satz vorhanden ja/nein" (`SpeicherAuslegungEditor.razor:116–123`).

### 1.4 Diagramm — der Renderer kann mehr, als gerufen wird

`SpeicherFlottenAnzeigeCtrl.Bilder` (`:13–43`) baut vier Reihen und ruft
`ChartRenderer.Speicherbetrieb(titel, reihen, "kW")`. Die Signatur (`ChartRenderer.cs:2318–2322`) kennt
längst `ladezustand` (zweite Achse), `sortiert` (Dauerlinie) und `fenster` (Datenzoom) — **alle drei
werden nicht übergeben**. Die Hausregel steht in `Doku_Simulationsergebnis_Darstellung.md` (Wurzel) § 5:
„Über jedem Bild dieselbe Steuerung: 1. `sortiert`, 2. je Reihe ein Schalter mit derselben Ressource
wie die Legende, 3. das Bild mit Datenzoom." Die Flottenansicht folgt ihr als einzige nicht. Die
Bausteine dafür existieren: Reihen-Umschalter im `SpeicherOptimierungDialog.razor:240–259`,
„sortiert" + Reihenwahl + Datenzoom im `StromspeicherReiter.razor:142–163` (Logik :432–490),
sprachneutrale Reihenschlüssel in `EPOS.Kern/Allgemein/Bericht/SpeicherBetriebsbild.cs`, der
Bildauftrag mit Zwischenspeicher in `SimulationErgebnisDaten.cs:483–492`, der Zoomrahmen
`Bausteine/Diagramm.razor` + `Standards/ChartBild.razor`, die Neuzeichnung ohne Datenbank
`SimulationErgebnisHuelle.Optimierung.cs:284–307`.

### 1.5 Abhängigkeit vom Speichermaß — die Rastersuche gibt es, die Sicht nicht

`SpeicherEngine/FlottenOptimierer.cs` (346 Zeilen) rastert Anzahl × Kapazität × Leistung bzw. C-Rate
(drei Achsenmodi, `FlottenModel.cs:55`) und liefert `FlottenAuslegungErgebnis` mit `Kandidaten`
(`FlottenKandidatZusammenfassung`: Kapazität, Lade-/Entladeleistung, Kapitalwert, Zulässigkeit,
Grund). Angezeigt wird davon eine **Texttabelle mit höchstens 50 Zeilen**
(`SpeicherFlottenErgebnisAnsicht.razor:13–18`). Für den Einzelspeicher gibt es seit W11b‑B‑5
Rasterkarte und Schnittkurve (`ChartRenderer.Optimierungsraster` :2577, `.Schnittkurve` :2757,
`SpeicherOptimierungDialog.razor:207–210`) — für die Flotte nicht. Die Kandidaten tragen weder
Durchsatz noch Vollzyklen: ein arbeitsloser Kandidat ist von einem arbeitenden nicht zu
unterscheiden. Die Verfeinerung um interessante Kandidaten (Spezifikation 12.2) fehlt im
Flottenoptimierer; der Einzelspeicher hat dafür den Schalter „Feinraster".

### 1.6 Fenster in Fenster — der Bestand

Der Flottendialog öffnet sich als modale Überlagerung aus dem Stromspeicher-Reiter der
Ergebnisseite; sein CSV-Unterdialog ist eine zweite Überlagerung darin
(`SpeicherFlottenDialog.razor:72–82`), der Einzelspeicher-Optimierungsdialog ebenso. Das
Haus kennt seit W16b/#62b den anderen Weg: **Simulationskonfiguration** und **Projektassistent**
sind freie Ansichten der `AppWurzel` (Entscheid E‑5, W16a‑E‑1), Überlagerungen bleiben für
Rückfragen und kleine Unterdialoge. Vier Reiter, eine Fußleiste mit drei Knöpfen, ein
Warnbanner, ein Fortschrittsbalken und die Ergebnistabelle in einem Fenster mit eigenem
Rollbalken — das ist der Zustand aus Foto 1.

**Lokalisierung:** Die fünf Flottenkomponenten führen außer den vier `FLOTTE_PLANER_*`-Schlüsseln
(#170c) keine Ressourcen — deutsche Literale und Textbündel (Übergangsmuster aus `EPOS.UI/CLAUDE.md`).

### 1.7 Zwei Speicher, eine Einheit — die Vorbelegung verlor jede Anlage außer der aktiven (#210)

**Der Befund (Anwender, 11.09.2026, zwei Bildschirmfotos).** Der Stromspeicher-Reiter der
detaillierten Simulation zeigte im Abschnitt „Kennzahlen je Speicher" EINE Zeile
(„Shenzhen Growatt New Energy Co., Ltd.: WIT‑M+APX ESS", 1 060,67 / 856,70 kWh, 7,40 Vollzyklen),
und auch die Reihenwahl der zwei Diagramme führte nur diese eine. Dasselbe im Foto darüber: Die
Einheitentabelle des **Eingabestands `@Aktuell`** führte ebenfalls nur eine Einheit (129,00 kWh,
100/100 kW). Das Projekt hat **zwei** Speicher.

**Die Ursache steht in der Vorbelegung, nicht in der Anzeige** —
`SpeicherFlottenStudieCtrl.Vorbelegung` (`EPOS.Kern/Controller/SpeicherFlottenStudieCtrl.cs:61`):
Eine neu angelegte Flotte entstand aus `StromspeicherSimCtrl.LeseParameter(projektId)` und daraus
wurde **genau eine** `FlottenEinheit`. Jener Aufruf liefert EINEN Parametersatz — die Anlagenzeile
der AKTIVEN Variante (AP9b, Fachkonzept 7.3), im Rückfall die kapazitätsgewichtete Summe über alle
`SP_TYP`-Anlagen. Für den Einzelspeicherlauf ist das richtig; für die Flotte war es ein stiller
Verlust. Und weil der Projektlauf den GESPEICHERTEN Stand rechnet
(`SpeicherFlottenProjektCtrl.Rechnen` → `@Projektflotte`), tauchte die zweite Anlage danach
nirgends mehr auf — weder in den Kennzahlen je Speicher noch in der Reihenwahl.

**Zwei geprüfte Gegenhypothesen, beide falsch.** (b) *Zusammenfallen gleicher Einheiten:* Weder
Engine noch Anzeige verdichten — `FlottenSimulator` legt je Einheit der Konfiguration eine
`FlottenSpeicherKennzahlen`-Zeile an und verlangt nur eindeutige `Id`
(`SpeicherEngine/FlottenSimulator.cs:738`), die Ansicht zählt diese Zeilen. Zwei Einheiten mit
demselben Namen UND demselben Anlagenbezug ergeben zwei Zeilen (Wachen in
`EPOS.Kern.Tests/SpeicherFlottenAnlagenEinheitenTests` und
`EPOS.UI.Tests/Dialoge/SpeicherFlottenErgebnisBetriebTests`). (c) *Veraltetes Ergebnis:* Das Banner
„Flotte geändert" gibt es und es greift (`StromspeicherReiter.razor`,
`Daten.FlottenAenderungOhneNeuenLauf`); der Eingabestand im zweiten Foto war ohnehin schon
einheitig.

**Die Behebung (#210).** `StromspeicherSimCtrl.Speicheranlagen(projektId)` liefert die
`SP_TYP`-Anlagenzeilen in Anlagenreihenfolge; `SpeicherFlottenStudieCtrl.Vorbelegung` liest je
Zeile über `LeseParameter(projektId, anlageId)` ihren **eigenen** Satz — Gerätedaten aus der
Anlage, SoC-Band aus DEREN Variantenzeile — und macht daraus je eine Einheit mit Anlagennamen und
`AnlageId`. Liefert keine Anlage einen Satz, bleibt es beim bisherigen Sammelsatz; ein
GESPEICHERTER Stand wird wie bisher nie überschrieben, die Vorbelegung greift nur beim Anlegen. Im
Reiter nennt die Einheitentabelle seither die **Herkunft** je Zeile („Projektanlage ‹Id›" bzw.
„nur im Eingabestand").

**Die fachliche Kante, die dabei bleibt.** Das Schema unterscheidet eine gleichzeitig betriebene
Anlage nicht von einer bloßen Vergleichs-Alternative — beides ist eine `SP_TYP`-Zeile in
`Tab_Energieanlagen` (Konzept Stromspeicher 7.3; die Spezifikation sagt in Kapitel 11 nur „eine
vorhandene Einzelanlage → genau eine Einheit"). Von den zwei möglichen Fehlern ist deshalb der
SICHTBARE gewählt: Eine Einheit zu viel sieht der Anwender im Flotteneditor und nimmt sie heraus;
eine Einheit zu wenig erfährt er nirgends. Die Referenzliste `REF_SP_TYP` bleibt ausdrücklich
draußen. **Der Referenzlauf ist unberührt** — 1046 rechnet seinen gespeicherten Stand
`@Projektflotte`, nicht die Vorbelegung; 13/13 byte-gleich gegen R7.

---

## 2. Zielbild

### 2.1 Eine Ansicht statt Fenster: „Stromspeicher-Auslegung" als freie Ansicht der `AppWurzel`

Neuer Seitenschlüssel `STROMSPEICHER_AUSLEGUNG` (`EPOS.UI/Seiten/Seitenschluessel.cs`), erreichbar
aus dem Stromspeicher-Reiter der Ergebnisseite („Auslegung…"), aus der Startseite (Kachel
Stromspeicher) und aus dem Menü; die Rückkehr geht dorthin, woher man kam (Muster #62b:
`Dienste.Navigation`). Die Ansicht füllt die WebView, kein Rahmen im Rahmen, kein zweiter
Rollbalken. Oben eine **Ablaufleiste** mit fünf Schritten, die den Weg aus 1.1 sichtbar macht:

```
 1 Speicher  ·  2 Daten & Kosten  ·  3 Betriebsführung  ·  4 Berechnen  ·  5 Ergebnis
```

Schritt 4 ist ein Knopf, kein Blatt: Er heißt „Bewerten" (Einzelkonfiguration) bzw.
„Größen optimieren" (Rastersuche an), je nach dem Schalter in Schritt 1. Schritt 5 ist das
Ergebnis (2.2) und erst nach einem Lauf betretbar; „Eingaben geändert" markiert ihn als veraltet.
Überlagerungen bleiben nur für die CSV-Spaltenzuordnung, die Rückfrage „Speichern / Verwerfen /
Bleiben" beim Verlassen mit ungespeicherten Eingaben (wie 62b‑E‑1) und die Prognosen-Tabelle.

> **Fortschreibung SD‑E‑8 (11.09.2026, umgesetzt #206).** Der hier beschriebene
> **Modus-Umschalter Flotte / Einzelspeicher ist gefallen** — die Ansicht rechnet immer die
> Flotte, und ein Einzelspeicher ist eine Flotte mit genau einer Einheit. Die Absätze unten,
> die einen Modus nennen, sind in diesem Sinn zu lesen; was sie dem Modus „Flotte" zuschreiben,
> gilt seither für jede Einheitenzahl. Der Einzelspeicher-**Optimierer** bleibt: Er trägt weiter
> das Betriebsbild des Berichts und die KI-Aktion `speicher_optimieren`. Einzelheiten in 4.1.

Dasselbe Muster bekam bis #206 die **Einzelspeicher-Optimierung** (`SpeicherOptimierungDialog`,
2 Bilder mit Reihenwahl schon vorhanden): Sie wurde Modus „Einzelspeicher" derselben Ansicht. Ob
Einzel- und Flottenpfad rechnerisch zusammengeführt werden, ist eine spätere Frage (SD‑Q2, **sie
bleibt auch nach SD‑E‑8 offen**): Die ANSICHT ist seit #206 eine, der PROJEKTLAUF führt weiter
zwei Pfade — der Einzelpfad ist der regressionsgeprüfte (zwölf Referenzprojekte), der Flottenpfad
seit #174 mit Projekt 1046.

**Befund 11.09.2026 (Anwender: „springt bei ‚Zurück' auf das Hauptfenster und geht nicht
zurück"):** Der Rückweg ist gebaut (`AppWurzel._auslegungRueckweg`), aber unter Windows steht
das Simulationsergebnis nicht als Ansicht der Wurzel, sondern als `Ueberlagerung` in der
Startseite — die Wurzel merkt sich deshalb `STARTSEITE`, und beim Zurück ist das Ergebnis weg.
Ursache, Zielbild (Simulation als EINE freie Ansicht mit Ablaufleiste, Rückwegstapel mit Marke)
und Stufenplan stehen in
[`Konzept_Simulationsablauf_EPOS-Plan.md`](Konzept_Simulationsablauf_EPOS-Plan.md).

### 2.2 Ergebnis als eigener Schritt (Punkt 1)

Nach der Hausregel `Doku_Simulationsergebnis_Darstellung.md` § 1–4 und § 6:

1. **Kennzahlkacheln** oben: Kapitalwert gegenüber „ohne Speicher", Bezugsspitze vorher → nachher
   (mit Peak-Ziel), Ersparnis Stromrechnung, Vollzyklen je Einheit — vier Kacheln, keine Tabelle.
2. **Diagnosebanner** (2.4): „Die Flotte hat im gesamten Zeitraum weder geladen noch entladen"
   mit den Gründen und je einem Knopf zur Abhilfe („Netzladung erlauben", „Peak-Ziel bestimmen").
3. **Vergleichstabelle** mit **drei** Spalten „Ohne Speicher · Mit Flotte · Δ", Δ farbig
   (grün = besser), Einheiten im Spaltenkopf, Nullzeilen (Einspeisung 0/0/0) einklappbar.
4. **Jahresprojektion als Bild** (Balken Netto-Cashflow je Jahr, Linie kumuliert, Ersatzjahre
   markiert), die Tabelle darunter aufklappbar; CSV-Export wie bisher.
5. **Kandidaten** (nur Modus „Größen optimieren"): Rasterkarte und Schnittkurve (2.5) vor der Tabelle.
6. **Diagramme** (2.3) als eigener Abschnitt am Ende, mit Zeitraumwahl.

Der Betriebseditor steht **nicht** mehr im Ergebnis (heute doppelt); das Ergebnis nennt die
berechnete Betriebsführung als Text und verlinkt auf Schritt 3.

### 2.3 Diagramme nach Hausregel § 5 (Punkt 2)

Über jedem Bild dieselbe Steuerung: Schalter **„sortiert"** (Dauerlinie: jede Reihe für sich
absteigend sortiert, x-Achse Stunden des Jahres), **ein Schalter je Reihe** mit derselben Ressource
wie die Legende (`OPT_BETRIEB_R_OHNE/_MIT/_SCHWELLE/_LEISTUNG` wiederverwenden, neu
`FLOTTE_R_SOC_<n>` je Einheit), **Datenzoom** durch Ziehen (Muster W8‑E‑2, `Diagrammbereich`) und
eine **Zeitraumwahl** Jahr / Woche / Tag mit Navigator (heute fest sieben Tage ab dem 1. Januar).
`SpeicherFlottenAnzeigeCtrl.Bilder` reicht `sortiert`, `fenster` und `ladezustand` an den vorhandenen
Renderer durch; der Ladezustand je Einheit kommt als zweite Achse ins Netzbild (§ 5.3) statt in ein
zweites Bild, das zweite Bild bleibt wählbar. Reihen je Einheit („Speicher A", „Speicher B") statt nur
„Speicher gesamt". Neuzeichnen ohne Datenbank aus dem gehaltenen Lauf (Muster
`SimulationErgebnisHuelle.Optimierung.cs:284–307`), mit Drosselung wie W11b‑B‑25.

### 2.4 Lastspitzenkappung: Start, Vorbelegung, Plausibilität, Diagnose (Punkt 3a)

1. **Vorbelegung des Peak-Ziels aus der Referenz** statt fest 50 kW: `H₀ = Referenzspitze −
   Σ Entladeleistung der Flotte`, mindestens das Maximum der Tagesminima der Last (sonst ist
   Wiederaufladung ohne Netzladung ausgeschlossen); die Herleitung steht als Zeile unter dem Feld.
2. **Knopf „Peak-Ziel bestimmen"**: Bisektion über den Simulator zwischen Grundlast und Spitze,
   Ziel = kleinstes H, bei dem die verbleibende Bezugsspitze ≤ H bleibt (höchstens ~12 Jahresläufe,
   mit Fortschritt und Abbruch). Das ist die Frage, die der Anwender an eine „Lastspitzenkappung"
   stellt: Wie tief komme ich mit dieser Flotte?
3. **Vorprüfung vor dem Lauf** (`EingabenPruefen`): Peak-Ziel unter dem Tagesminimum der Last →
   Warnung „Unter H fällt die Last nie; ohne Netzladung lädt die Flotte nicht"; Peak-Ziel über der
   Referenzspitze → Hinweis „Kappung wirkungslos"; Betriebskosten unter 0,1 % der Investition →
   Hinweis auf den Satz (1.3).
4. **Vorgabe „Netzladung erlaubt" je Betriebsziel**: PeakShaving → erlaubt (Spezifikation 5.1:
   „Die Wiederaufladung nutzt freie Anschlussleistung unter H" — das IST Netzladung),
   PvGreedy → nicht erlaubt; sichtbar im Betriebseditor, änderbar.
5. **Diagnose „arbeitslose Flotte"** im Engine-Ergebnis: Zähler je Einheit (Intervalle mit
   `n > H`, Intervalle mit Ladedeckel 0 durch Netzladeverbot, Intervalle mit leerem Speicher bei
   Entladeanforderung); daraus im Ergebnis das Banner aus 2.2 mit Gründen — die Zahlen liegen im
   Simulator schon vor, sie müssen nur ausgewiesen werden.
6. **Start-SoC**: Produktivstandard SoC-Minimum (AP0) bleibt für den Projektlauf; die Studie zeigt
   das Feld „Start-Ladezustand" mit dem Hinweis, dass ein leerer Speicher vor einer Spitze am
   1. Januar nichts kappen kann. Keine stille Änderung des Standards (SD‑Q4).

Keiner dieser Punkte ändert einen Rechenwert eines gespeicherten Standes: Vorbelegungen greifen
nur bei neuen Studien, die Vorgabe „Netzladung" nur, wenn der Stand die Eigenschaft nicht trägt —
das ist vor dem Merge gegen Projekt 1046 (R7) zu prüfen (Stufenplan P1).

### 2.5 Ergebnisse über der Speichergröße (Punkt 3b)

Mit dem Schalter „Größen optimieren" zeigt Schritt 5 vor der Kandidatentabelle
(seit SD‑E‑8 für jede Einheitenzahl — die Größen-Sicht einer Flotte mit einer Einheit ist die
Größen-Sicht des Einzelspeichers):

- **Rasterkarte** Kapazität × Leistung (Farbe = Kapitalwert gegenüber „ohne Speicher", unzulässige
  Kandidaten schraffiert, Optimum markiert) — je Einheit wählbar, bei Anzahl > 1 die Summe;
- **Schnittkurve** Kapitalwert über der Kapazität bei fester C-Rate (Wahl der C-Rate als Schieber),
  daneben dieselbe Kurve über der Leistung;
- **Kandidatentabelle** um Durchsatz (Vollzyklen/a), Bezugsspitze und Ersparnis erweitert, sortierbar,
  mit Spaltenfilter (Katalogfilter-Muster), „Kandidat übernehmen" je Zeile.

Beide Bilder liefern `ChartRenderer.Optimierungsraster`/`.Schnittkurve` schon für den
Einzelspeicher; die Flotte füttert sie aus `FlottenKandidatZusammenfassung`. Die Verfeinerung
(Spezifikation 12.2, „Feinraster") kommt als eigener Schritt danach; SP‑O‑4 („endliches Raster ist
nicht global optimal") wird im Bild benannt.

---

## 3. Was bewusst NICHT Teil dieses Konzepts ist

- Kein neuer Rechenweg, keine Änderung an Verteilung, Reserve, Wirkungsgraden, MILP-Planer.
- Keine Zusammenführung von Einzel- und Flottenpfad (SD‑Q2, später).
- Keine Monatspeaks/Tarifstaffeln (SP‑O‑5), keine Alterungswirkung (SP‑O‑2/6).
- Kein iOS-Lauf für diese Welle: Sie trifft Kern, Oberfläche und Tests, nicht die Hülle
  (Regel vom 09.09.2026); der Gerätebeleg für die gesperrten planenden Ziele (SP‑O‑3) ist am 11.09.2026 auf dem iPad geführt (Anwender).

---

## 4. Entscheide des Anwenders (SD‑Q1 … SD‑Q7)

| Kennung | Frage | Empfehlung |
|---|---|---|
| **SD‑Q1** | Umfang „freie Ansicht": nur die Speicherflotte, oder auch die Einzelspeicher-Optimierung, der Peak-Shaving-Dialog (W12) und der Stromganglinien-Dialog? | **Flotte und Einzelspeicher-Optimierung** als zwei Modi EINER Ansicht `STROMSPEICHER_AUSLEGUNG`; Peak-Shaving- und Stromganglinien-Dialog bleiben Überlagerungen (kurze Arbeitsschritte mit eigener Rückkehr) |
| **SD‑Q2** | Einzel- und Flottenpfad rechnerisch zusammenführen (Flotte mit einer Einheit = Einzelspeicher)? | **Nicht jetzt.** Erst wenn die Flotte alle Kennzahlen des Einzelpfads liefert und ein Referenzprojekt beide Wege byte-gleich zeigt; als SP‑O‑12 ins Register |
| **SD‑Q3** | Vorbelegung des Peak-Ziels: aus der Referenz herleiten (2.4 Punkt 1) und Knopf „Peak-Ziel bestimmen" (Bisektion)? | **Beides.** Die feste 50 kW fällt |
| **SD‑Q4** | Start-SoC der Studie: beim Produktivstandard SoC-Minimum bleiben (AP0) und nur den Hinweis zeigen, oder für Studien mit vollem Speicher starten? | **AP0 beibehalten**, Feld und Hinweis zeigen; ein voller Start würde den Januar-Peak schöner rechnen, als er im Betrieb wäre |
| **SD‑Q5** | Vorgabe „Netzladung erlaubt" je Betriebsziel (PeakShaving ja, PvGreedy nein)? | **Ja**, mit Anzeige der Vorgabe; gespeicherte Stände unverändert |
| **SD‑Q6** | Ergebnis: Kacheln + Δ-Spalte + Jahresprojektion als Bild (2.2) — oder nur die Tabelle aus dem Dialog herauslösen? | **Vollfassung 2.2**; sie folgt der Regel, die alle anderen Ergebnisreiter schon haben |
| **SD‑Q7** | Diagramm-Zeitraum: Jahr / Woche / Tag mit Navigator (Muster Jahresverlauf W8‑E‑2) zusätzlich zum Datenzoom? | **Ja**; heute ist der Ausschnitt fest sieben Tage ab dem 1. Januar |

**Entscheid 11.09.2026: alle sieben nach Empfehlung.**

---

### 4.1 Nachträglicher Entscheid SD‑E‑8 (11.09.2026): ein Modus statt zwei

Anwender, zum Bildschirmfoto der Ansicht mit dem Modus-Umschalter „Einzelspeicher": **„Es ist
nicht sinnvoll, einen Unterschied zwischen Einzelspeicher und Flotte zu machen. Für
Einzelspeicher sollen auch die Betriebsziele wählbar sein — die bisherigen Berechnungsarten
für Einzelspeicher sind nicht mehr nötig."**

Damit ist **SD‑Q1 revidiert**: Die Ansicht `STROMSPEICHER_AUSLEGUNG` kennt nur noch EINEN Weg —
die Flottenrechnung; ein Einzelspeicher ist eine Flotte mit genau einer Einheit (so bildet die
Spezifikation 1.2, Kapitel 11, eine vorhandene Einzelanlage ohnehin ab). Die fünf Betriebsziele,
Peak-Ziel, Diagnose und Größen-Sicht gelten für jede Einheitenzahl; die Verteilung wird erst ab
zwei Einheiten gezeigt. Die drei Einzelspeicher-Schritte (Suchraum, Betrieb mit den
Berechnungsarten Dauernutzung/Nachtnutzung/Lastspitzenkappung, Ergebnis) und der
Modus-Umschalter fallen aus der Ansicht. **SD‑Q2 bleibt:** Der Projektlauf führt weiter zwei
Pfade — die Einzelanlage der zwölf Referenzprojekte rechnet unverändert, nur wer die
Projektflotte aktiviert, geht den Flottenpfad (SP‑O‑12 offen). Umsetzung als **Paket P5**
(Aufgabe #206), parallel zu S3 des Assistenten (#201).

**Umgesetzt #206** (11.09.2026). Was dabei blieb und warum:

- **Die Vorbelegung macht der Kern schon** (`SpeicherFlottenStudieCtrl.Vorbelegung`): Ohne
  gespeicherten Flottenstand entsteht je Speicheranlage des Projekts eine Einheit (seit **#210**,
  Abschnitt 1.7) — Kapazität, Lade- und Entladeleistung, beide Wirkungsgrade, SoC-Band, Name und
  Anlagenbezug. Ein Projekt mit EINER Speicheranlage bekommt damit genau eine Einheit; das ist der
  Fall, den SD‑E‑8 „Einzelspeicher" nennt. Es war nichts zu ergänzen, nur nachzuweisen
  (`StromspeicherAuslegungCtrlTests.Ohne_Flottenstand_steht_die_aktive_Variante_als_EINE_Einheit_da`).
- **Die Verteilung erscheint erst ab zwei Einheiten** — ausgeblendet, nicht gesperrt, mit einer
  Zeile Erklärung (`FLOTTE_BETRIEB_VERTEILUNG_EINE`). Der Schalter dafür sitzt am geteilten
  Baustein `SpeicherFlottenBetriebEditor` (`VerteilungZeigen`, Vorgabe `true`), damit der zweite
  Wirt — der Stromspeicher-Reiter der Ergebnisseite — unverändert bleibt.
- **Das Rückschreiben in die Projektanlage bleibt** (Schritt 5, Knopf mit Rückfrage): Es greift
  für die eine Einheit mit Anlagenbezug; bei zweien gibt es den Knopf nicht, weil die Anlage eine
  Kapazität und eine Leistung trägt. Geschrieben wird der Arbeitsstand aus Schritt 1 — dorthin
  legen „Kandidat übernehmen" und „Beste Flotte übernehmen" ihr Ergebnis.
- **Der Leistungspreis ist EINE Eingabe** und steht in Schritt 2 (`LeistungspreisBlock`): Er
  schreibt in den Suchraum, in `FlottenTarif.LeistungspreisEuroProKw` und sofort in die
  Projektvariante (W11b‑E‑3).
- **Gelöscht** sind `EinzelspeicherSuchraum/-Betrieb/-Ergebnis.razor`, der Suchraum-Teil des
  `SpeicherAuslegungEditor` samt `NurQuellenKostenProfile`, der `Modusknopf` der `Ablaufleiste`,
  `AuslegungModus` und im Kern der Einzelweg des `StromspeicherAuslegungCtrl`
  (`EinzelVorbereiten`, `EinzelRechnen`, `Betriebsbild`, `RasterCsv`). **Nicht gelöscht** sind
  `SpeicherOptimierungCtrl` und `SpeicherOptimierer`: Sie haben weitere Aufrufer (Bericht
  `SpeicherBetriebsbild`, `SpeicherAuslegungCtrl.Vorbelegung`, KI-Aktion `speicher_optimieren`
  über `StromspeicherSimCtrl`).

## 5. Stufenplan (nach dem Entscheid; je Paket ein Opus-Agent im eigenen Worktree)

| Paket | Inhalt | Regressionsbedingung |
|---|---|---|
| **P1 Kern/Engine** — **umgesetzt #183** (`710b4c3`, Merge `14ecdde`, Referenzlauf 13/13 byte-gleich gegen R7) | Diagnosezähler im `FlottenSimulator` und Hinweise im `SpeicherFlottenErgebnis`; Vorbelegung Peak-Ziel aus der Referenz; „Peak-Ziel bestimmen" (Bisektion mit `IProgress`/`CancellationToken`); Vorprüfungen; Vorgabe Netzladung je Ziel; Betriebskosten-Hinweis. Tests in `SpeicherEngine.Tests` und `EPOS.Kern.Tests` | Referenzlauf 13/13 byte-gleich gegen R7 — insbesondere 1046 (Stand `@Projektflotte` muss `NetzladungErlaubt` ausdrücklich tragen, sonst Vorgabenwechsel sichtbar machen und anhalten) |
| **P2 Ergebnis und Diagramme** — **umgesetzt #184** (`2a77bd4`, Merge `12db7da`) | `SpeicherFlottenErgebnisAnsicht` nach 2.2/2.3; `SpeicherFlottenAnzeigeCtrl.Bilder` mit `sortiert`/`fenster`/`ladezustand`, Reihen je Einheit; Jahresprojektionsbild im `ChartRenderer` (ChartProben 44 → 46 Proben, 39 Bilder und 7 Gegenproben); Ressourcen `FLOTTE_*` de/en für alle Flottentexte und die drei Textbündel; bunit-Tests, Hausmuster W16b‑O‑2. **Ohne** Diagnosebanner — die Zähler liefert P1, die Verdrahtung P3 | ChartProben 46/46 grün, EPOS.UI.Tests 3 503/3 503, EPOS.Kern.Tests 2 463/2 463, Referenzlauf 1030/1046 byte-gleich zu R7 |
| **P3 Ansicht** — **umgesetzt #192** (`0088941`, Merge `ef55097`) | Seitenschlüssel `STROMSPEICHER_AUSLEGUNG`, Ablaufleiste mit fünf Stationen, Modus Flotte/Einzelspeicher (SD‑Q1), Rückkehrweg über `AppWurzel`/`Dienste.Navigation`, Rückfrage beim Verlassen (62b‑E‑1), Überlagerungen nur für CSV/Prognosen; Diagnosebanner (2.2 Punkt 2) und Peak-Ziel-Vorschlag/-Bestimmung samt Vorprüfung und Netzladung je Ziel (SD‑Q3/SD‑Q5); Hüllen-Delegaten aus `SimulationErgebnisHuelle.Flotte.cs`/`.Optimierung.cs` im Kern-Controller `StromspeicherAuslegungCtrl` (Regel: Datenbankseite in den Kern); `SpeicherFlottenDialog` und `SpeicherOptimierungDialog` sind gelöscht (Regel iZ5, nie zwei Fassungen). **Kein** neuer Menüpunkt in diesem Paket — die `Menuetabelle` bleibt bei 58 Punkten; der Weg führt über den Stromspeicher-Reiter der Ergebnisseite | EPOS.UI.Tests 3 530/3 530, EPOS.Kern.Tests 2 515/2 515, ChartProben 46/46, SQL-Prüfer 0, Referenzlauf 1030/1046 byte-gleich zu R7; Windows-Abnahme durch den Anwender steht aus |
| **P4 Größen-Sicht** — **umgesetzt #193** (`e0c81b0`, Merge `8b2bfc8`) | Rasterkarte und Schnittkurve für die Flotte aus `FlottenKandidatZusammenfassung` (+ Durchsatz, Vollzyklen, Spitze, Ersparnis, „arbeitslos“, C-Rate und Rasterindex), `SpeicherFlottenAnzeigeCtrl.Rasterdaten`/`Schnittdaten`/`SchnittdatenLeistung` samt den drei Bildern, `ChartRenderer.Optimierungsraster` mit optionaler **Schraffur** und **SP‑O‑4-Fußzeile**, Baustein `SpeicherFlottenGroessenAnsicht` mit Kandidatentabelle (Filter, Sortierung, „übernehmen“). **Einbindung #196 (`47bdc8a`, Merge `bd9dbac`)** (11.09.2026): Schritt 5 der Ansicht `STROMSPEICHER_AUSLEGUNG` zeigt den Baustein, sobald ein Rastersuchergebnis vorliegt — VOR der Ergebnisansicht (Konzept 2.5); die einfache Kandidatentabelle der `SpeicherFlottenErgebnisAnsicht` ist damit gefallen (keine zwei Tabellen), Empfehlung und CSV-Export sind mitgewandert. „Kandidat übernehmen“ macht die Variante über `SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration` zur Flotte in Schritt 1 — für den besten Kandidaten ist das die Konfiguration des Optimierers, für jeden anderen eine Rückabbildung aus `FlottenKandidatEinheit` auf den Arbeitsstand —, markiert Schritt 5 als veraltet und führt nach Schritt 1 mit Hinweisbanner; ungespeicherte Eingaben werden vorher abgefragt (Muster 62b‑E‑1). Dazu bindet `EPOS.iOS/wwwroot/index.html` seither `epos-flotte.css` ein | Referenzlauf 1030/1046 byte-gleich gegen R7; ChartProben 49 (41 Bilder, 8 Gegenproben), die 39 vorhandenen Bilder byte-gleich. #196: EPOS.UI.Tests 3 569, EPOS.Kern.Tests 2 546, SQL-Prüfer 0 |
| **P5 Ein Weg statt zwei Modi (SD‑E‑8)** — **umgesetzt #206** (Zweig `w206-ein-modus`; SHA beim Merge nachtragen) | `AuslegungModus` und der Modus-Umschalter fallen; die Ansicht rechnet immer die Flotte, ein Einzelspeicher ist eine Flotte mit EINER Einheit (die Vorbelegung stand im Kern schon und legt seit #210 je Speicheranlage des Projekts eine Einheit an). Fünf Betriebsziele für jede Einheitenzahl, die **Verteilung erst ab zwei Einheiten** (`SpeicherFlottenBetriebEditor.VerteilungZeigen`, ausgeblendet statt gesperrt, mit Erklärzeile), Schritt 4 heißt „Bewerten" bzw. „Größen optimieren". **Was der Einzelweg hierließ, bleibt:** das Rückschreiben in die Projektanlage in Schritt 5 (Knopf mit Rückfrage, für die eine Einheit mit Anlagenbezug — ohne ihn käme die ausgelegte Größe nie beim klassischen Projektlauf an, SD‑Q2) und der Leistungspreis als EINE Eingabe in Schritt 2 (`LeistungspreisBlock` → Suchraum, `FlottenTarif.LeistungspreisEuroProKw` und Projektvariante). **Gelöscht:** `EinzelspeicherSuchraum/-Betrieb/-Ergebnis`, der Suchraum-Teil des `SpeicherAuslegungEditor` samt `NurQuellenKostenProfile`, der `Modusknopf` der `Ablaufleiste`, der Einzelweg in `StromspeicherAuslegungCtrl` (`EinzelVorbereiten`, `EinzelRechnen`, `Betriebsbild`, `RasterCsv`) und **38 Ressourcenschlüssel** (17 der Ansicht, 21 verwaiste `OPT_*` der drei gefallenen Blätter; die Rückfrage vor dem Schreiben nimmt den vorhandenen Text `OPT_MSG_UEBERNAHME_FRAGE` statt eines zweiten mit derselben Aussage). **Nicht gelöscht:** `SpeicherOptimierungCtrl` und `SpeicherOptimierer` — sie tragen Bericht, Vorbelegung und die KI-Aktion `speicher_optimieren` | EPOS.UI.Tests 3 634, EPOS.Kern.Tests 2 609, SpeicherEngine.Tests 394, KiKern.Tests 474, ChartProben 49, SQL-Prüfer 0, Referenzlauf 1030/1046 byte-gleich zu R7 (kein Rechenwert geändert) |
| **P6 Feinraster** (später) | Verfeinerung um interessante Kandidaten (Spezifikation 12.2) im `FlottenOptimierer` | eigener Entscheid, eigenes Prüfmuster |

Reihenfolge P1 → P2 → P3 → P4 → P5; P1 und P2 können parallel laufen (P2 zeigt die Diagnose aus P1,
Schnittstelle = die Felder im `SpeicherFlottenErgebnis`, vorab vereinbart). Nach jedem Paket:
Gate, Statusblock, Push; iOS-Lauf keiner.

---

## 6. Register

Ins Register § 8 des Umsetzungskonzepts (Block „Offene Punkte des Mehrspeicherkonzepts"):

- **SP‑O‑10 Arbeitslose Flotte:** Peak-Ziel fest 50 kW, Netzladung verboten, Start-SoC = Minimum → die
  Flotte rechnet nichts und niemand sagt es (1.2); Behebung P1.
- **SP‑O‑11 Darstellung:** Flottendialog als Überlagerung mit vier Reitern und Ergebnistabelle
  („Fenster in Fenster"), Diagramm ohne Reihenwahl/Dauerlinie/Datenzoom entgegen Hausregel § 5,
  keine Größen-Sicht trotz Rastersuche; Behebung P2–P4.
- **SP‑O‑12 Zwei Speicherpfade:** Einzelspeicher (regressionsgeprüft, 12 Projekte) und Flotte
  (1046) rechnen getrennt; Zusammenführung erst nach SD‑Q2.
