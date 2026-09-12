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
[`Projekte/Mockup_Stromspeicher_Ansicht_2026-09-11.html`](../../Projekte/Mockup_Stromspeicher_Ansicht_2026-09-11.html).
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

**Anwenderentscheid #210‑O‑1 (11.09.2026): „gleichzeitig".** Mehrere Stromspeicher-Anlagen eines Projekts
sind gleichzeitig betriebene Einheiten, keine Vergleichsalternativen. Das Datenmodell braucht kein
Unterscheidungsmerkmal; die Vorbelegung aus #210 (je `SP_TYP`-Anlage eine Einheit) ist damit die Regel.
Wer eine Anlage nur zum Vergleich führt, nimmt sie in Schritt 1 der Auslegung aus der Flotte.

### 1.8 „Speicher hinzufügen" ohne Quelle — Befund #239

**Die Rückmeldung (Anwender, 12.09.2026, Bildschirmfoto „Stromspeicher-Auslegung › 1 Speicher"),
wörtlich:** „Stromspeicher hinzufügen geht nicht für Speicher aus der Datenbank - nur für
duplizierung des vorhandenen (optional kann vorhandener kopiert werden). Bei mehreren angelegten
Stromspeichern wird nur einer angezeigt, nach löschen steht er nicht mehr zur Auswahl."

**Drei Befunde, eine Wurzel.**

1. **Der Knopf kannte keine Quelle.** `EPOS.UI/Dialoge/Strom/SpeicherFlottenEditor.razor`
   `EinheitHinzufuegen` legte `NeueEinheit(n)` an — generisch „Speicher n", 100 kWh, 50/50 kW,
   95 %, SoC 10/90/50 %. Daneben gab es nur `EinheitKopieren` (dieselbe Einheit noch einmal,
   ohne Anlagenbezug) und `EinheitEntfernen`. Weder der Speicherkatalog
   (`Tab_Stromspeicher_STAMM` über `StromspeicherStammCtrl.Katalogfilterzeilen`) noch die
   Speicheranlagen des Projekts (`StromspeicherSimCtrl.Speicheranlagen`) waren von hier aus
   erreichbar — obwohl beide Wege im Kern längst standen. Die Wiki-Bedienungsseite behauptete
   in Schritt 1 sogar, „als Vorlage dient ein Satz aus dem Speicherkatalog": Das gab es im
   Programm nicht.
2. **„Nur einer angezeigt."** Die Vorbelegung aus #210 (§ 1.7) sät je Speicheranlage eine
   Einheit — aber **nur beim Anlegen**: `SpeicherAuslegungCtrl.Vorbelegung` kehrt um, sobald ein
   Stand `@Aktuell` für (Projekt, aktive Anlage) vorliegt, und den schreibt
   `StromspeicherAuslegungCtrl` bei jedem Speichern, jedem Flottenlauf und jedem Rückruf.
   Danach ist die Einheitenliste eingefroren: Eine später angelegte Speicheranlage erscheint
   nie, eine entfernte kommt nie zurück.
3. **„Nach Löschen nicht mehr zur Auswahl."** Folge von 1 + 2. § 1.7 sagt „eine Einheit zu viel
   nimmt der Anwender heraus" — der Weg zurück fehlte.

**Die Behebung (#239).**

- **A — Kern, die Abbildung an EINER Stelle** (`SpeicherFlottenStudieCtrl`):
  `Projektanlagenkandidaten(projektId)` nennt je `SP_TYP`-Anlage Kennung, Name, Kapazität und
  Leistung; `EinheitAusProjektanlage(projektId, anlageId)` baut daraus eine Einheit über
  **denselben** Weg wie die Vorbelegung (`LeseParameter(projektId, anlageId)` und die private
  `Einheit(…)`) — es gibt keine zweite Abbildung, die auseinanderlaufen könnte;
  `EinheitAusKatalog(katalogId)` bildet einen Katalogsatz ab (Tabelle unten), gelesen über den
  neuen `StromspeicherStammCtrl.Katalogsatz(id)`. Alle drei LESEN nur.
- **B — Dienste und Hülle:** `StromspeicherAuslegungDienste` führt `Projektanlagen`,
  `EinheitAusProjektanlage`, `Katalogzeilen`, `Katalogprofil` und `EinheitAusKatalog`;
  `EPOS.UI.Daten/Stromspeicher/StromspeicherAuslegungHuelle.DiensteSatz()` verdrahtet sie auf
  den Kern. Das Katalogprofil ist **dasselbe** wie im Projekt-Stromspeicherdialog
  (`Katalogfilterprofil.MitVerwendung(Anlagenart.Stromspeicher, …)`) — acht Spalten, derselbe
  Filter, dieselbe Sortierung.
- **C — Editor:** „+ Speicher hinzufügen" öffnet eine `Ueberlagerung` mit drei Quellen
  (Speicheranlage des Projekts · Speicherkatalog · leere Einheit). Eine bereits vertretene
  Anlage steht in der Liste, ist aber gesperrt und trägt den Vermerk „bereits in der Flotte" —
  sie zu verbergen ließe genau die Frage offen, die der Anwender gestellt hat. Der Doppelklick
  in der Katalogliste übernimmt sofort (Hausmuster). **Ohne die neuen Parameter legt der Knopf
  wie bisher sofort eine leere Einheit an** — der zweite Wirt des Editors (Reiter
  „Stromspeicher") kennt kein Projekt und keinen Katalog.
- **D — Nachzug:** Über der Einheitenliste steht eine leise Zeile, sobald eine Speicheranlage
  des Projekts keine Einheit hat: „n Speicheranlagen des Projekts sind nicht in der Flotte: A,
  B" mit dem Knopf „Aufnehmen". Sie erscheint auch **nach dem Entfernen** einer
  Anlageneinheit — damit ist „nach Löschen nicht mehr zur Auswahl" behoben.

**Die Abbildung Katalog → Einheit** (sie folgt Feld für Feld den Regeln, mit denen
`StromspeicherSimCtrl.LeseParameter` eine Projektanlage liest — sonst rechnete dieselbe Zeile je
nach Herkunft verschieden):

| `Tab_Stromspeicher_STAMM` | `FlottenEinheit` | Regel |
|---|---|---|
| `Bezeichner` | `Name` | unverändert |
| `Energie` [kWh] | `KapazitaetKWh` | unverändert |
| `Leistung` [kW] | `LadeleistungKw` = `EntladeleistungKw` | fehlt sie, gilt **1 C** (Leistung = Kapazität) — wörtlich die Regel des Laufs |
| `Wirkungsgrad_RT` | `Ladewirkungsgrad` = `Entladewirkungsgrad` | `sqrt(eta_RT)` je Richtung (`SpeicherParameter.EtaCh`/`EtaDis`); außerhalb (0…1] gilt `ETA_RT_STANDARD` = 0,90 |
| — | `SocMin` / `SocMax` | 10 / 90 % aus einer **leeren** `StromspeicherVarianteModel` — ein Katalogsatz trägt keine Betriebsführung |
| `Ladezustand` [%] | `SocStart` | in das SoC-Band geklemmt; 0 heißt „nicht gepflegt" und wird SoC_min (AP0, Frage 8) |
| `Modulkosten` [€/kWh] | `InvestitionEuroProKWh` | nur mit `EigeneKosten` |
| `Leistungskosten` [€/kW] | `InvestitionEuroProKw` | nur mit `EigeneKosten` |
| `Investition_Fix` [€] | `InvestitionEuro` | nur mit `EigeneKosten` |
| `Standby_Verbrauch` [W] | `HilfsverbrauchKw` | W / 1000 |

`EigeneKosten` geht **nur** an, wenn der Satz wenigstens einen der drei Investitionswerte trägt;
sonst überschrieben lauter Nullen die gemeinsamen Kostensätze aus Schritt 2.

**Was bewusst NICHT abgebildet wird — und warum.** `Verschleisskosten` steht im Katalog in
€/(kWh·Zyklus) **bezogen auf die Nennkapazität**; die zwei Kostenfelder der Flotte
(`GrenzverschleissEuroProKWhEntladung`, `DurchsatzkostenEuroProKWhEntladung`) rechnen je
abgegebener AC-kWh. Die Umrechnung hängt am nutzbaren Band und am Entladewirkungsgrad
(`SpeicherEngine/ArbitrageOptionen`) und ist damit keine Zuordnung, sondern eine Annahme — sie
bleibt dem Anwender. `Zyklen_Zugesichert` ist eine Zahl ohne Entladetiefe und damit keine
Rainflow-Stützstelle; `Degradation`, `Typ` und `Firma` haben in `FlottenEinheit` kein
Gegenstück.

**Die Kante, die bleibt.** Der **gespeicherte Stand wird nicht von selbst angefasst**
(SP‑O‑8): Die Vorbelegung ist unverändert, die neuen Wege lesen nur, und der Nachzug ist eine
Anwenderhandlung. **Der Referenzlauf ist unberührt** — 1046 rechnet seinen Stand
`@Projektflotte`.

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

**Die Achsen folgen der GRÖSSENKOPPLUNG der Suchachse** (Auftrag #226, Anwenderbefund vom
11.09.2026). Bis dahin trug die Spaltenachse immer die C-Rate — auch bei einer Suche über
Kapazität UND Leistung, wo P/C gar kein Gitter bildet: Aus 13 × 13 = 169 Kandidaten (20…500 kWh
und kW, Schritt 40) wurde eine Karte mit 137 krummen C-Raten-Spalten, in der über neun Zehntel
der Zellen leer blieben — und ein Loch zeichnete der Renderer in der MINIMUMFARBE, also rot.
Seither trägt `FlottenAuslegungErgebnis.Achsenmodus` die Kopplung der ersten aktiven Suchachse,
und die Sicht liest sie: `KapazitaetUndLeistung` → Zeilen Kapazität [kWh], Spalten
Entladeleistung [kW], Schnitte über Kapazität und Leistung; `KapazitaetUndCRate` → wie bisher;
`LeistungUndCRate` → Zeilen Leistung, Spalten C-Rate, Schnitte über Leistung und über der
Kapazität `E = P / C`. Titel, Achsen-, Schieber- und Bildbeschreibungstexte liefert je Kopplung
der Kern (`SpeicherFlottenAnzeigeCtrl.Rastertitel`/`Achsentext`/`Schiebertext`/`Werttext`/
`Schnitttitel`/`Schnittbeschreibung`) — eine Quelle für Bild und Markup. **Ein Loch ist seither
hellgrau** (`ChartRenderer.C_RASTER_LOCH`): „nicht gerechnet" ist keine Aussage über einen
schlechten Kandidaten; unzulässig bleibt schraffiert.

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
  Baustein `SpeicherFlottenBetriebEditor` (`VerteilungZeigen`, Vorgabe `true`). **Offen: zweiter
  Wirt → erledigt (#213, Anwenderentscheid 11.09.2026 „Empfehlung"):** Bis dahin setzte nur die
  Auslegungsansicht den Schalter (`Flotte.Einheiten.Count >= 2`, über `PeakZielBlock`), und der
  Stromspeicher-Reiter der Ergebnisseite zeigte die Klappliste weiter unbedingt (die Vorgabe des
  Editors). Seit #213 wertet auch der Reiter dieselbe Bedingung an seinem eigenen Eingabestand
  (`flotte.Einheiten.Count >= 2`) — keine neue Logik, derselbe Ausdruck an beiden Wirten, denn
  der geteilte Baustein selbst kennt die Einheitenzahl nicht (sein `Wert` ist
  `FlottenSimulationOptionen`, ohne Einheitenliste).
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

### 4.2 Anwenderbefund 11.09.2026: Lastspitzenkappung entlädt zu früh (Ratsche)

Bei festem Peak-Ziel entlädt die Flotte auch dann bei jeder kleineren Spitze weiter, wenn die Jahresspitze
schon verfehlt ist — der Speicher ist dann leer, wenn die große Spitze kommt (Bildschirmfoto: Ziel 200 kW,
Januarwoche auf 200 kW gekappt, Spitze 523 kW ungekappt). Der Anwender hat das kausale Gegenmodell als
Excel-Makro vorgelegt (Schwelle steigt, sobald `N − D > H`; Laden bis H; Jahresspitze 738,4 → 569,6 kW mit
400 kW / 400 kWh). Die Regel, die geprüften Alternativen S‑A…S‑F, der Vergleich mit dem Vorausschau-Optimum
der Bisektion und die Fragen PS‑Q1…PS‑Q4 stehen in `Spezifikation_Stromspeicher_Optimierung.md` 5.1.1
(Fassung 1.4); die Umsetzung ist Paket P7 des Stufenplans.

## 5. Stufenplan (nach dem Entscheid; je Paket ein Opus-Agent im eigenen Worktree)

| Paket | Inhalt | Regressionsbedingung |
|---|---|---|
| **P1 Kern/Engine** — **umgesetzt #183** (`710b4c3`, Merge `14ecdde`, Referenzlauf 13/13 byte-gleich gegen R7) | Diagnosezähler im `FlottenSimulator` und Hinweise im `SpeicherFlottenErgebnis`; Vorbelegung Peak-Ziel aus der Referenz; „Peak-Ziel bestimmen" (Bisektion mit `IProgress`/`CancellationToken`); Vorprüfungen; Vorgabe Netzladung je Ziel; Betriebskosten-Hinweis. Tests in `SpeicherEngine.Tests` und `EPOS.Kern.Tests` | Referenzlauf 13/13 byte-gleich gegen R7 — insbesondere 1046 (Stand `@Projektflotte` muss `NetzladungErlaubt` ausdrücklich tragen, sonst Vorgabenwechsel sichtbar machen und anhalten) |
| **P2 Ergebnis und Diagramme** — **umgesetzt #184** (`2a77bd4`, Merge `12db7da`) | `SpeicherFlottenErgebnisAnsicht` nach 2.2/2.3; `SpeicherFlottenAnzeigeCtrl.Bilder` mit `sortiert`/`fenster`/`ladezustand`, Reihen je Einheit; Jahresprojektionsbild im `ChartRenderer` (ChartProben 44 → 46 Proben, 39 Bilder und 7 Gegenproben); Ressourcen `FLOTTE_*` de/en für alle Flottentexte und die drei Textbündel; bunit-Tests, Hausmuster W16b‑O‑2. **Ohne** Diagnosebanner — die Zähler liefert P1, die Verdrahtung P3 | ChartProben 46/46 grün, EPOS.UI.Tests 3 503/3 503, EPOS.Kern.Tests 2 463/2 463, Referenzlauf 1030/1046 byte-gleich zu R7 |
| **P3 Ansicht** — **umgesetzt #192** (`0088941`, Merge `ef55097`) | Seitenschlüssel `STROMSPEICHER_AUSLEGUNG`, Ablaufleiste mit fünf Stationen, Modus Flotte/Einzelspeicher (SD‑Q1), Rückkehrweg über `AppWurzel`/`Dienste.Navigation`, Rückfrage beim Verlassen (62b‑E‑1), Überlagerungen nur für CSV/Prognosen; Diagnosebanner (2.2 Punkt 2) und Peak-Ziel-Vorschlag/-Bestimmung samt Vorprüfung und Netzladung je Ziel (SD‑Q3/SD‑Q5); Hüllen-Delegaten aus `SimulationErgebnisHuelle.Flotte.cs`/`.Optimierung.cs` im Kern-Controller `StromspeicherAuslegungCtrl` (Regel: Datenbankseite in den Kern); `SpeicherFlottenDialog` und `SpeicherOptimierungDialog` sind gelöscht (Regel iZ5, nie zwei Fassungen). **Kein** neuer Menüpunkt in diesem Paket — die `Menuetabelle` bleibt bei 58 Punkten; der Weg führt über den Stromspeicher-Reiter der Ergebnisseite | EPOS.UI.Tests 3 530/3 530, EPOS.Kern.Tests 2 515/2 515, ChartProben 46/46, SQL-Prüfer 0, Referenzlauf 1030/1046 byte-gleich zu R7; Windows-Abnahme durch den Anwender steht aus |
| **P4 Größen-Sicht** — **umgesetzt #193** (`e0c81b0`, Merge `8b2bfc8`) | Rasterkarte und Schnittkurve für die Flotte aus `FlottenKandidatZusammenfassung` (+ Durchsatz, Vollzyklen, Spitze, Ersparnis, „arbeitslos“, C-Rate und Rasterindex), `SpeicherFlottenAnzeigeCtrl.Rasterdaten`/`Schnittdaten`/`SchnittdatenLeistung` samt den drei Bildern, `ChartRenderer.Optimierungsraster` mit optionaler **Schraffur** und **SP‑O‑4-Fußzeile**, Baustein `SpeicherFlottenGroessenAnsicht` mit Kandidatentabelle (Filter, Sortierung, „übernehmen“). **Einbindung #196 (`47bdc8a`, Merge `bd9dbac`)** (11.09.2026): Schritt 5 der Ansicht `STROMSPEICHER_AUSLEGUNG` zeigt den Baustein, sobald ein Rastersuchergebnis vorliegt — VOR der Ergebnisansicht (Konzept 2.5); die einfache Kandidatentabelle der `SpeicherFlottenErgebnisAnsicht` ist damit gefallen (keine zwei Tabellen), Empfehlung und CSV-Export sind mitgewandert. „Kandidat übernehmen“ macht die Variante über `SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration` zur Flotte in Schritt 1 — für den besten Kandidaten ist das die Konfiguration des Optimierers, für jeden anderen eine Rückabbildung aus `FlottenKandidatEinheit` auf den Arbeitsstand —, markiert Schritt 5 als veraltet und führt nach Schritt 1 mit Hinweisbanner; ungespeicherte Eingaben werden vorher abgefragt (Muster 62b‑E‑1). Dazu bindet `EPOS.iOS/wwwroot/index.html` seither `epos-flotte.css` ein | Referenzlauf 1030/1046 byte-gleich gegen R7; ChartProben 49 (41 Bilder, 8 Gegenproben), die 39 vorhandenen Bilder byte-gleich. #196: EPOS.UI.Tests 3 569, EPOS.Kern.Tests 2 546, SQL-Prüfer 0 |
| **P4a Achsen nach der Größenkopplung (Anwenderbefund 11.09.2026)** — **umgesetzt #226** | Der Befund: Eine Einheit mit Kopplung „Kapazität und Leistung", beide 20…500 in Schritten von 40 (13 × 13 = 169 Kandidaten), zeigte in Schritt 5 eine krumme C-Raten-Achse (0,04 · 0,16 · … · 17) und fast nur rote Zellen. Ursache: `Rasterdaten` baute die Spaltenachse IMMER aus `p.CRate` — in diesem Modus sind das 137 verschiedene Quotienten statt eines Gitters, über neun Zehntel der Zellen blieben leer, und `ChartRenderer.Rasterfarbe` gab jedem `NaN` die MINIMUMFARBE. Behoben: `FlottenAuslegungErgebnis.Achsenmodus` (vom `FlottenOptimierer` aus der ersten aktiven Suchachse gesetzt) ist die EINE Quelle; `FlottenRasterdaten` führt `Modus`, `Zeilenwerte`, `Spaltenwerte` samt `Zeilengroesse`/`Spaltengroesse`, die zwei Schnitte heißen `SchnittdatenBeiSpalte`/`SchnittdatenBeiZeile` (Bilder entsprechend) und legen ihre Achse je Kopplung (Spalten direkt, `P = E · C`, `E = P / C` — letztere aufsteigend umgelegt); Titel, Achsen-, Schieber- und `alt`-Texte kommen je Kopplung aus dem Kern (zehn neue Ressourcen de/en). `ChartRenderer.C_RASTER_LOCH` (0xF2F2F2) zeichnet ein Loch hellgrau — Schraffur und SP‑O‑4-Fußzeile unverändert. **Kein Rechenweg berührt** (`FlottenOptimierer` nur um das Merkfeld ergänzt) | Build 0 Fehler / 4 eindeutige Warnungen; EPOS.Kern.Tests 2 682, EPOS.UI.Tests 3 816, SpeicherEngine.Tests 412, KiKern.Tests 488, SpeicherPlanung.Tests 27; ChartProben 55 (0 Verstöße, +1 Bild „Kapazität × Leistung", +1 Gegenprobe „Loch ≠ Minimumfarbe"); SQL-Prüfer 0 von 1 343; Referenzlauf 1030/1046 byte-gleich gegen R7 |
| **P5 Ein Weg statt zwei Modi (SD‑E‑8)** — **umgesetzt #206 (`8a382b6`+`7cecc6e`, Merge `4fcb6a1`)** (Wiki hochgeladen 11.09.2026: Stromspeicher Rev. 540, Berechnung/Stromspeicher Fassung 5 Rev. 541) | `AuslegungModus` und der Modus-Umschalter fallen; die Ansicht rechnet immer die Flotte, ein Einzelspeicher ist eine Flotte mit EINER Einheit (die Vorbelegung stand im Kern schon und legt seit #210 je Speicheranlage des Projekts eine Einheit an). Fünf Betriebsziele für jede Einheitenzahl, die **Verteilung erst ab zwei Einheiten** (`SpeicherFlottenBetriebEditor.VerteilungZeigen`, ausgeblendet statt gesperrt, mit Erklärzeile), Schritt 4 heißt „Bewerten" bzw. „Größen optimieren". **Was der Einzelweg hierließ, bleibt:** das Rückschreiben in die Projektanlage in Schritt 5 (Knopf mit Rückfrage, für die eine Einheit mit Anlagenbezug — ohne ihn käme die ausgelegte Größe nie beim klassischen Projektlauf an, SD‑Q2) und der Leistungspreis als EINE Eingabe in Schritt 2 (`LeistungspreisBlock` → Suchraum, `FlottenTarif.LeistungspreisEuroProKw` und Projektvariante). **Gelöscht:** `EinzelspeicherSuchraum/-Betrieb/-Ergebnis`, der Suchraum-Teil des `SpeicherAuslegungEditor` samt `NurQuellenKostenProfile`, der `Modusknopf` der `Ablaufleiste`, der Einzelweg in `StromspeicherAuslegungCtrl` (`EinzelVorbereiten`, `EinzelRechnen`, `Betriebsbild`, `RasterCsv`) und **38 Ressourcenschlüssel** (17 der Ansicht, 21 verwaiste `OPT_*` der drei gefallenen Blätter; die Rückfrage vor dem Schreiben nimmt den vorhandenen Text `OPT_MSG_UEBERNAHME_FRAGE` statt eines zweiten mit derselben Aussage). **Nicht gelöscht:** `SpeicherOptimierungCtrl` und `SpeicherOptimierer` — sie tragen Bericht, Vorbelegung und die KI-Aktion `speicher_optimieren` | EPOS.UI.Tests 3 634, EPOS.Kern.Tests 2 609, SpeicherEngine.Tests 394, KiKern.Tests 474, ChartProben 49, SQL-Prüfer 0, Referenzlauf 1030/1046 byte-gleich zu R7 (kein Rechenwert geändert) |
| **P6 Feinraster** — **umgesetzt #224** (im selben Auftrag wie P8, Entscheid SD‑E‑9 Option A) | Zweite Phase im `FlottenOptimierer`: nach dem Grobraster ein engeres Raster um das Grob-Optimum, NUR auf der Größenachse (SD‑Q10), Fenster `[max(von, E*−Δ), min(bis, E*+Δ)]` mit Mindestbreite 1 kWh, Schrittweite `Δ/9`; zweite Achse, Stückzahl, übrige Achsen und Betriebsziel bleiben beim Grob-Optimum. **Das Feinraster gewinnt nur bei STRIKT besserem Kapitalwert** (Phase 2 läuft nach Phase 1, der Vergleich ist `>`); jeder Kandidat trägt seine `Phase`, `FlottenOptimierer.Kandidatenzahl` ist die EINE Zählregel für Lauf und Kandidatenzeile, `MaximaleKandidaten` zählt beide Phasen, `IProgress` und Abbruch laufen über beide. `FlottenAuslegungErgebnis` führt dazu `FeinrasterGerechnet` und `Rechendauer` | 13 neue Fälle in `SpeicherEngine.Tests/FlottenFeinrasterTests` (Bereich, Randlage, Mindestbreite, Zählregel, Grenzfall, Phasenmarke, Gleichstand, Abbruch in Phase 2, Fortschritt); Referenzlauf unberührt — der Projektlauf betritt die Rastersuche nicht |
| **P8 Station „4 Optimierung" (SD‑E‑9 Option A)** — **umgesetzt #224** | Zielbild 7.4 vollständig: `AuslegungSchritt.Optimierung` als fünftes BLATT (die Ablaufleiste verliert ihren Aktionsplatz, `MitAktion="false"`), `Seiten/Strom/OptimierungBlock` mit Ziel, Suchwahl, Suchraumtabelle je Einheit, LIVE-Kandidatenzeile, Feinraster-Schalter, Rechenknopf und dem Kasten „Bestes Ergebnis"; die Größen-Sicht zieht von Schritt 5 nach 4. Der Einheiteneditor trägt nur noch die Einheiten — „Netz und Planung" wird `SpeicherFlottenNetzBlock` (Schritt 3), die Jahresprojektion `SpeicherFlottenWirtschaftBlock` (Schritt 2, SD‑Q12) mit den drei Erklärzeilen aus 7.3, der Kostenblock bekommt die eigene Überschrift „Kosten dieser Einheit". Dazu 7.8: Stufenleiste mit nummerierten Kreisen, `Zahlenfeld` höchstens vier Nachkommastellen (hausweit), `Seiten/Strom/Hinweiszeilen` für die Vorprüfung, Herleitungszeile und neutrale Pille im Editorkopf, Kartenkopf ohne Zahlenzusatz | Build 0 Fehler / 4 eindeutige Warnungen; EPOS.Kern.Tests 2 682, EPOS.UI.Tests 3 887, SpeicherEngine.Tests 425, KiKern.Tests 488, SpeicherPlanung.Tests 27; ChartProben 57; SQL-Prüfer 0 von 1 343; Referenzlauf 1030/1007/1017/1045/1046 byte-gleich gegen R7 |
| **P7 Adaptive Lastspitzenkappung (Ratsche, S‑A)** — Anwenderbefund 11.09.2026, Spezifikation 5.1.1 (Fassung 1.4), PS‑Q1…PS‑Q4 entschieden 11.09.2026 (Empfehlung) — **umgesetzt #215** (`4bd5c8c`, Merge `74a3bb1`; Wiki hochgeladen 11.09.2026: Berechnung/Stromspeicher Fassung 6 Rev. 543, Stromspeicher Rev. 544) | `FlottenSimulationOptionen`: Peak-Ziel „adaptiv (kausal) | fest", Startwert H0; Ratsche im `FlottenSimulator` (H als Zustand, D_t aus `Grenzen()`), Ganglinie von H, Diagnose „Nachzüge"; Schritt 3 der Ansicht, Ergebnisansicht „kausal erreicht" neben „mit Vorausschau erreichbar" (Bisektion bleibt); Prüfstand mit dem Excel-Makro als Referenzrechnung auf synthetischem Lastgang | Opus; 1046 (festes Ziel) byte-gleich; Wiki Rechenweg Fassung 6 |
| **P9 Quellen für „Speicher hinzufügen" (Befund #239)** — Anwenderrückmeldung 12.09.2026, Abschnitt 1.8 — **umgesetzt #239** | Kern: `SpeicherFlottenStudieCtrl.Projektanlagenkandidaten`, `.EinheitAusProjektanlage` (DERSELBE Weg wie die Vorbelegung) und `.EinheitAusKatalog` samt `StromspeicherStammCtrl.Katalogsatz(id)`; Dienste und Hülle um fünf Wege ergänzt; Editor mit `Ueberlagerung` und drei Quellen (Projektanlage — vertretene gesperrt —, Speicherkatalog mit dem Profil des Projektdialogs, leere Einheit) und der Nachzugszeile „n Speicheranlagen des Projekts sind nicht in der Flotte …" mit Knopf „Aufnehmen"; 19 Ressourcen de/en. **Ohne die neuen Parameter bleibt der Knopf, was er war**; Vorbelegung und gespeicherter Stand unverändert (SP‑O‑8) | EPOS.Kern.Tests 2 722 (+10), EPOS.UI.Tests 3 956 (+11), beide neuen Klassen auch unter `LANG=en_US.UTF-8` grün; SQL-Prüfer 0 von 1 344; Referenzlauf unberührt — kein Rechenweg berührt |

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
- **SP‑O‑13 Ablaufleiste mit Lücke:** Bildschirmfoto 11.09.2026 — „1 2 3", eine breite Lücke,
  dann „4 5" (die breite Fassung schiebt Rechenknopf und Schritt 5 mit `margin-inline-start:auto`
  nach rechts); Behebung Auftrag **#225** — Modifikator `Ablaufleiste.Buendig` /
  `.epos-ablaufleiste--buendig`, fünf Stationen linksbündig ohne Lücke, die Simulationsseite
  (`Kompakt`) bleibt unverändert.
- **SP‑O‑14 Achsen der Größen-Sicht** (Anwenderbefund 11.09.2026, **behoben mit #226**): Die
  Rasterkarte trug die C-Rate auch dort auf der Spaltenachse, wo die Suche Kapazität UND
  Leistung rasterte — dort ist P/C kein Gitter, die Karte zerfiel in Löcher, und ein Loch
  zeichnete der Renderer in der Minimumfarbe. Seither folgen Achsen, Schnitte, Schieber und
  Texte dem `Achsenmodus` des Ergebnisses, und ein Loch ist hellgrau (2.5, Paket P4a).
  **Offen bleibt daran nichts**; der Punkt steht hier als Beleg, dass der Befund nicht die
  Rastersuche selbst betraf (`FlottenOptimierer` unverändert, Referenzlauf byte-gleich),
  sondern nur ihre Anzeige.
- **SP‑O‑15 Die Optimierung war versteckt** (Anwenderrückmeldung 11.09.2026, **behoben mit
  #224**): Die Rastersuche gab es seit P4/P5 vollständig — erreichbar nur über DREI Schalter
  an DREI Orten, und das Wort „Optimierung" kam in der Ablaufleiste nicht vor (Station 4 hieß
  „Bewerten"). Seither ist sie die STATION 4 mit Suchraum, Kandidatenzeile, Feinraster und
  dem Kasten „Bestes Ergebnis" (Kapitel 7). **Offen bleibt daran nichts.**
- **SP‑O‑16 Feinraster** (P6, **geschlossen mit #224**): Der `FlottenOptimierer` rechnete nur
  das Grobraster; die zweite Phase der Mappe V7 fehlte als einziger fachlicher Rest. Sie ist
  portiert — Regel, Gewinnbedingung und Zählregel stehen in 7.7 und im Rechenweg-Wiki.

---

## 7. Optimierung als eigener Bereich — Anwenderrückmeldung 11.09.2026 (SD‑E‑9)

> **Umgesetzt mit Auftrag #224.** Option A vollständig, samt P6 (Feinraster) und 7.8
> (Darstellung). Was von diesem Kapitel abweicht, steht in 7.9.

### 7.1 Rückmeldung

Drei Bildschirmfotos (Einheiteneditor mit „Größenbereich dieser Einheit" und „Ersatz und Restwert",
Block „Wirtschaftliche Jahresprojektion") und die Excel-Mappe V7 (Blatt „Optimierung Speicher
(Ziel: max N13)", Tab „Daten für Auswertung P_Sim"): *„verbessere die Struktur der Dialogseite. Es soll
ein Bereich Stromspeicheroptimierung geben, der die wirtschaftlich beste ‚Größe' des Stromspeichers
berechnet und auch grafisch darstellt. Dies war schon einmal vorhanden."* Dazu die Bitte, drei Felder
zu erläutern: Energie-Ausgleichswert, Zusätzlicher Restwert der Studie, Maximale Auslegungskandidaten.

### 7.2 Befund — die Optimierung gibt es, sie ist nur versteckt

- **Die Rastersuche liefert heute genau das, was die Mappe zeigt.** Seit P4 (#193) und P5 (#206)
  zeichnet Schritt 5 die **Rasterkarte** „Kapitalwert über Kapazität und C-Rate" (Optimum markiert,
  unzulässige Punkte schraffiert), zwei **Schnittkurven** (über der Kapazität bei fester C-Rate, über
  der Leistung bei fester Kapazität) und die **Kandidatentabelle** mit „übernehmen"
  (`SpeicherFlottenGroessenAnsicht`, 561 Zeilen; Bilder aus `ChartRenderer.Optimierungsraster`/
  `.Schnittkurve`, dieselben Renderer wie der gefallene Einzelspeicher-Dialog aus #137).
- **Erreichbar ist sie nur über drei Schalter an drei Orten.** (1) In Schritt 1 ganz unten, UNTER dem
  Block „Wirtschaftliche Jahresprojektion", der Schalter „Speicheranzahl und Größenbereiche optimieren"
  (`FLOTTE_DLG_CHK_OPTIMIEREN` → `Auslegung.FlottenGroessenOptimieren`); (2) je Einheit im Klappblock
  „Größenbereich dieser Einheit" der Schalter „In der Auslegung variieren" samt Größenkopplung und drei
  Bereichen; (3) erst dann heißt Station 4 „Größen optimieren" statt „Bewerten", und Schritt 5 zeigt die
  Größen-Sicht. **Im Foto steht Schalter (1) aus** — deshalb sieht der Anwender „4 Bewerten" und keine
  Optimierung. Das Wort „Optimierung" kommt in der Ablaufleiste nicht vor (1 Speicher · 2 Daten & Kosten ·
  3 Betriebsführung · 4 Bewerten · 5 Ergebnis).
- **Der Einheiteneditor ordnet falsch.** Die sieben Kostenfelder (Feste Investition, Investition
  Kapazität/Leistung, Betriebskosten fix/Kapazität/Leistung, Kosten je Entladung) stehen unter der
  Überschrift **„Ersatz und Restwert"**, weil der Schalter „Eigene Kosten für diese Einheit verwenden"
  dort eingehängt wurde (`SpeicherFlottenEditor.razor:193-228`). Der **Größenbereich** (der SUCHRAUM der
  Optimierung) steht zwischen „Alterung" und „Ersatz", obwohl er nicht die Einheit beschreibt, sondern
  die Suche. Der Block „Wirtschaftliche Jahresprojektion" (Zins, Laufzeit, Restwert, Ausgleichswert)
  steht in Schritt 1 unter der Einheitenliste — die Wiki-Seite ordnet ihn Schritt 2 „Daten & Kosten"
  zu („Für die Flotte kommen hier die finanzielle Projektlaufzeit und der Energie-Ausgleichswert
  dazu"); Programm und Doku widersprechen sich.
- **Was mit dem alten Dialog verloren ging.** `SpeicherOptimierungDialog` (#137) baute die Mappe
  wörtlich nach: **Phase 1 Grobraster**, **Phase 2 Feinraster** um das Grob-Optimum
  (`SpeicherOptimierer.FeinrasterBereich`, V7-Regel Schrittweite/9), Kasten **„Bestes Ergebnis"**
  (Zielwert, Größe, C-Rate, Leistung, Rechendauer), Heatmap, Schnittkurve. Er fiel mit P3/P5, weil
  die Flotte den Weg übernahm. Der `FlottenOptimierer` rechnet heute NUR das Grobraster (Kapitel 5,
  P6 „Feinraster — später"); den Kasten „Bestes Ergebnis" gibt es in der Größen-Sicht nicht — das
  Optimum steht nur als Marke im Bild und als hervorgehobene Tabellenzeile.
- **Zwei Anzeigebefunde am Rand.** Das Feld „Energie-Ausgleichswert" zeigt `0,31746000000002055`
  (Gleitkommarest des Vorschlags aus dem mittleren Bezugspreis; das `Zahlenfeld` rundet nicht,
  `SpeicherFlottenEditor.razor:280`). Die zwei Expertenfelder „Zusätzlicher Restwert der Studie" und
  „Maximale Auslegungskandidaten" stehen ohne Erklärzeile neben Zins und Laufzeit.

### 7.3 Was die drei Felder bedeuten (Antwort, gehört in Erklärzeilen und Wiki)

| Feld | Bedeutung | Quelle |
|---|---|---|
| **Energie-Ausgleichswert** [€/kWh gespeichert] | Am Ende des Simulationsjahrs steht der Speicher selten auf demselben Ladezustand wie am Anfang. Ein Speicher, der voll startet und leer endet, hätte „gratis" Energie verkauft; einer, der leer startet und voll endet, hätte Energie bezahlt, die noch drin ist. Der Ausgleichswert bewertet die Differenz Endenergie − Startenergie mit einem Preis und bucht sie als Korrektur in die Jahresbilanz (positiv, wenn der Speicher voller endet). Der Vorschlag ist der mittlere Bezugspreis der Datenquelle; ein von Hand geänderter Wert bleibt („gespeichert") und wird nicht wieder überschrieben. Ohne Endenergiegleichheit ist er Pflicht. | `FlottenSimulationOptionen.EnergieAusgleichEuroProKWh`, `SpeicherFlottenErgebnis.EndenergieAusgleichEuro` |
| **Zusätzlicher Restwert der Studie** [€] | Ein Betrag, der am ENDE der Projektlaufzeit als Einnahme angesetzt und auf heute abgezinst in den Kapitalwert eingeht — ZUSÄTZLICH zu den Restwerten der einzelnen Einheiten („Restwert der Einheit" im Editor). Gedacht für alles, was zur Studie und nicht zu einer Einheit gehört: Netzanschluss, Gebäude, Fläche, Weiterverkauf im Paket. Vorgabe 0. | `FlottenWirtschaftlichkeitEingang.RestwertEuro`; `FlottenWirtschaftlichkeit.cs:140` addiert Studie + Einheiten |
| **Maximale Auslegungskandidaten** | Obergrenze für die Zahl der Rasterpunkte, die die Optimierung rechnet. Die Suche multipliziert je variierter Einheit die Schritte von Anzahl × Kapazität × Leistung (bzw. C-Rate); jeder Kandidat ist ein vollständiger Jahreslauf über alle Projektjahre. Überschreitet das Raster die Grenze, wird der Lauf **abgewiesen, nicht gekürzt** — man verkleinert dann Bereich oder Schrittweite. Vorgabe 10 000. Im Foto: Kapazität 20…500/20 und Leistung 20…500/20 = 25 × 25 = 625 Kandidaten. | `FlottenAuslegungEingang.MaximaleKandidaten`, `FlottenOptimierer.cs:57` |

### 7.4 Zielbild — Station „Optimierung" statt eines Schalters (Vorschlag)

Die Ablaufleiste bekommt die Optimierung als eigene, benannte Station; Station 4 ist dann eine SEITE,
nicht nur ein Rechenknopf:

**1 Speicher · 2 Daten & Kosten · 3 Betriebsführung · 4 Optimierung · 5 Ergebnis**

- **Schritt 1 Speicher:** nur noch die Einheiten (Technik, Alterung & Grenzkosten, Kosten dieser
  Einheit, Ersatz & Restwert). Der Größenbereich und der Schalter „optimieren" wandern nach 4; der
  Block „Wirtschaftliche Jahresprojektion" wandert nach 2 (dort, wo die Wiki-Seite ihn beschreibt).
- **Schritt 2 Daten & Kosten:** Zeitreihen, Kostensätze, dazu Zins, Jahresprojektion, Projektlaufzeit,
  Restwert der Studie, Energie-Ausgleichswert (jedes Expertenfeld mit Erklärzeile aus 7.3).
- **Schritt 4 Optimierung** (Aufbau wie das Mappenblatt; das hier ursprünglich genannte Mockup `stromspeicher-optimierung.html` lag nie im Repository — Befund 12.09.2026; das Mockup zu Kapitel 8 ist `Mockups/stromspeicher-optimierung-v2.html`):
  1. Kopf: **Ziel** (Kapitalwert gegenüber „ohne Speicher" — die Mappe kannte drei Zielgrößen, das
     Programm legt seit P1 eine fest) und die Wahl **„Nur die eingestellte Flotte bewerten"** oder
     **„Wirtschaftlich beste Größe suchen"**.
  2. **Suchraum je Einheit** (heute „Größenbereich dieser Einheit"): Größenkopplung, Von/Bis/Schritt für
     Anzahl, Kapazität, Leistung bzw. C-Rate — als Tabelle, eine Zeile je Einheit, Schalter „variieren".
  3. **Kandidatenzeile, live:** „25 × 25 × 1 = 625 Kandidaten, geschätzt 1–2 min" gegen „Maximale
     Auslegungskandidaten"; rot mit Hinweis, sobald das Raster die Grenze reißt (heute erst beim Start).
  4. **Phase 2 Feinraster** (Schalter, Vorgabe an): nach dem Grobraster ein zweites Raster um das
     Grob-Optimum auf der Größenachse, Schrittweite/9 wie in der Mappe und im alten Einzelspeicher
     (`SpeicherOptimierer.FeinrasterBereich`) — das ist Paket P6, vorgezogen.
  5. **Rechenknopf** „Optimieren" (bzw. „Bewerten"), Fortschritt und Abbrechen wie heute.
  6. **Ergebnis der Suche**, direkt darunter: Kasten **„Bestes Ergebnis"** (Kapitalwert, Kapazität,
     C-Rate, Leistung, Anzahl; geprüfte / zulässige Kandidaten; Rechendauer), daneben die
     **Rasterkarte** und die **Kurve Kapitalwert über der Größe** (Grob- und Feinpunkte unterscheidbar),
     darunter die **Kandidatentabelle** — das ist die heutige Größen-Sicht, sie zieht von 5 nach 4.
     „Kandidat übernehmen" schreibt die Größe in Schritt 1 und markiert das Ergebnis in 5 als veraltet.
- **Schritt 5 Ergebnis:** nur noch die Bewertung der GEWÄHLTEN Flotte (Kacheln, Δ-Tabelle, Diagramme,
  Jahresprojektion) — ohne Größen-Sicht.

**Abbildung Mappe ↔ Programm**

| Mappe V7, Blatt „Optimierung Speicher" | Programm nach 7.4 |
|---|---|
| Ziel: max N13 (Jahresüberschuss nach Kapitaldienst) | Kapitalwert über die Projektlaufzeit (gleiche Rangfolge bei gleicher Laufzeit und gleichem Zins; Entscheid P1) |
| Bestes Ergebnis: Wert, Größe, C-Rate, Leistung, Rechendauer | Kasten „Bestes Ergebnis" (neu) |
| Phase 1 Grobraster Größe × C-Rate, farbige Matrix | Rasterkarte (vorhanden), Kopplung „Kapazität und C-Rate" |
| Phase 2 Feinraster 4 500…5 000 | Feinraster im `FlottenOptimierer` (P6, neu) |
| Punktdiagramm Ergebnis über Größe | Schnittkurve (vorhanden), Grob- und Feinpunkte (neu) |

### 7.5 Optionen und Empfehlung

| | Inhalt | Aufwand |
|---|---|---|
| **A — Station „Optimierung"** (Empfehlung) | 7.4 vollständig: Station 4 als Seite, Suchraum und Schalter aus 1 nach 4, Jahresprojektion nach 2, Kandidatenzeile live, Feinraster (P6), Kasten „Bestes Ergebnis", Größen-Sicht von 5 nach 4, Editorblöcke neu geordnet, Erklärzeilen, Rundung des Ausgleichswerts; Wiki-Seite Stromspeicher (Schritte) nachgezogen | ein Opus-Agent, Seite + Editor + Optimierer; Referenzlauf unberührt (Projektlauf kennt keine Rastersuche) |
| **B — nur Sichtbarkeit** | Struktur bleibt; Schalter nach OBEN in Schritt 1 mit dem Namen „Wirtschaftlich beste Größe suchen", Station 4 heißt immer „Optimieren / Bewerten", Kasten „Bestes Ergebnis" in Schritt 5, Kostenblock im Editor umbenannt, Rundung | klein, ein Sonnet-Agent; das Versteck bleibt, die Mappen-Analogie nicht |
| **C — A ohne Feinraster** | wie A, P6 bleibt „später" | wie A minus Optimierer |

**Empfehlung A.** Die Rückmeldung zielt auf einen BEREICH, der den Namen trägt und wie das Mappenblatt
aufgebaut ist; das Feinraster ist der einzige fachliche Rest aus der Mappe, der noch fehlt, und der alte
Einzelspeicher-Code zeigt die Regel (Schrittweite/9, Mindestbreite 1 kWh) — die Portierung in den
`FlottenOptimierer` ist überschaubar und bekommt ein eigenes Prüfmuster (Grob-Optimum ⊂ Feinraster,
Feinraster gewinnt nur bei strikt besserem Wert, wie `SpeicherOptimierer.cs:143`).

### 7.6 Fragen (SD‑Q9 … SD‑Q12)

| Frage | Empfehlung |
|---|---|
| **SD‑Q9** Option A, B oder C? | A |
| **SD‑Q10** Feinraster wie in der Mappe (Schrittweite/9 um das Grob-Optimum, nur auf der Größenachse) oder auf beiden Achsen? | wie Mappe: nur Größenachse, C-Rate bleibt Grobraster (die Mappe zeigt, dass die C-Rate ab 1,0 C nichts mehr ändert) |
| **SD‑Q11** Zielgröße bleibt der Kapitalwert (nicht der Jahresüberschuss der Mappe)? | ja — der Kasten „Bestes Ergebnis" zeigt zusätzlich die jährliche Ersparnis, damit der Vergleich mit der Mappe möglich bleibt |
| **SD‑Q12** Block „Wirtschaftliche Jahresprojektion" nach Schritt 2 (wie die Wiki-Seite sagt)? | ja |

**Stufenplan:** **P8 Struktur** (A oder C, ein Opus-Agent) und **P6 Feinraster** (bei A im selben
Auftrag, sonst später). Start nach dem Merge von #220 (Startseiten-Reiter Simulation), weil beide
`AppWurzel.razor` anfassen.

### 7.7 Anwenderentscheid SD‑E‑9 (11.09.2026): „Empfehlung"

**Option A** (Station „4 Optimierung" als Seite, Feinraster P6 im selben Auftrag), **SD‑Q10** Feinraster nur auf der
Größenachse, **SD‑Q11** Zielgröße bleibt der Kapitalwert und der Kasten „Bestes Ergebnis" nennt die jährliche Ersparnis
dazu, **SD‑Q12** der Block „Wirtschaftliche Jahresprojektion" wandert nach Schritt 2. Umsetzung als **Auftrag #224**
(Opus, eigener Worktree) nach den Merges von #220 (AppWurzel), #225 (Ablaufleiste bündig, Anzahl-Schritt fällt) und #226
(Rasterkarte folgt der Größenkopplung), weil alle drei die Auslegungsseite, den Editor oder die Größen-Sicht anfassen.

### 7.8 Darstellung der Ansicht (Anwenderwunsch 11.09.2026, Bildschirmfoto Schritt 1)

*„optimiere die Darstellung der Seite z. B. etwas deutlich 1 Speicher, 2 Daten & Kosten …"* — Befund am Foto und Regeln für #224:

| Befund | Regel |
|---|---|
| Die Stationen der Ablaufleiste sind kleine Textreiter; nur „4 Größen optimieren" ist ein dunkler Knopf, „5 Ergebnis" trägt daneben die Marke „veraltet". Was Schritt ist und was Knopf, sieht man nicht. | **Stufenleiste** im Baustein `Ablaufleiste`: je Station ein nummerierter Kreis (28 px) plus Titel; die aktive Station ausgefüllt in der Primärfarbe mit Unterstrich, erledigte Stationen mit Haken, kommende grau; alle fünf gleich gebaut (kein Knopf unter den Stationen — der Rechenknopf steht in der Seite von Schritt 4, 7.4). Die Marke „veraltet" bleibt als kleine Pille am Kreis von 5. Eine Reihe, bündig links (#225). Die Simulationsseite nutzt denselben Baustein und bekommt dieselben Kreise, behält aber Lage und Reihenfolge ihrer Werkzeugleiste (#216, `Kompakt`). |
| Zahlen mit Gleitkommarest: Ladewirkungsgrad `94,86832980505137 %`, SoC-Obergrenze `89,99999999999999 %`, Energie-Ausgleichswert `0,31746000000002055`. | Der Baustein `Zahlenfeld` zeigt Werte mit höchstens vier Nachkommastellen (`0.####`, Kultur des Anwenders), ein Parameter `Nachkommastellen` erlaubt weniger (Prozente 2); der gespeicherte Wert bleibt unverändert, erst eine Eingabe ändert ihn. Gilt hausweit für alle Dialoge; bunit-Fall am Baustein. |
| Zwei blaue Hinweisbänder (Betriebsaufwand, Start-Ladezustand) nehmen ein Drittel des Kopfes ein, jedes mit eigenem Link „erklären lassen". | Diagnosehinweise **kompakt**: eine Zeile je Hinweis mit Symbol, Text und dem Link inline; ab zwei Hinweisen ein aufklappbarer Block „2 Hinweise" (offen beim ersten Erscheinen, Zustand je Sitzung). Diagnosebanner-Konzept (2.4) bleibt, nur die Form ändert sich. |
| Kopf „Speicherflotte — Physische Einheiten …" mit der roten Pille „1 Speicher" rechts und dem Satz über die Studie. | Der Erklärsatz wird Herleitungszeile (leise), die Pille zeigt die Einheitenzahl neutral (kein Rot ohne Fehler). |
| Kartenkopf der Einheit `[100kW, 129.0kWh]` mit Punkt als Dezimaltrenner und ohne Leerzeichen. | Beschriftung nach Hauskultur: „129 kWh · 100/100 kW" (steht rechts schon so) — den Namen ohne den Klammerzusatz zeigen. |

### 7.9 Was #224 umgesetzt hat — und wo es vom Vorschlag abweicht

**Umgesetzt** (Zielbild 7.4 und 7.8, Anwenderentscheid SD‑E‑9 Option A):

| Gegenstand | Wo es jetzt steht |
|---|---|
| Station „4 Optimierung" als BLATT | `AuslegungSchritt.Optimierung`, `EPOS.UI/Seiten/Strom/OptimierungBlock.razor` (397 Z.); die Ablaufleiste bekommt `MitAktion="false"` und führt fünf gleich gebaute Stationen |
| Kopf: Ziel und die Wahl „bewerten" / „beste Größe suchen" | `OptimierungBlock`, `Optionsgruppe` → `Auslegung.FlottenGroessenOptimieren`; der Schalter `FLOTTE_DLG_CHK_OPTIMIEREN` in Schritt 1 ist gefallen |
| Suchraum je Einheit als Tabelle | `table.epos-flotte-suchraum`, eine Zeile je `FlottenAuslegungsAchse`; dieselben Modellfelder wie bis #224 im Einheiteneditor |
| Kandidatenzeile LIVE | `FlottenOptimierer.Kandidatenzahl` — dieselbe Zählregel, mit der der Lauf annimmt oder abweist; rot mit Wortlaut, wenn das Raster die Grenze reißt, und der Rechenknopf ist dann über `FlotteEingabenPruefen` gesperrt |
| Phase 2 Feinraster | `FlottenAuslegungEingang.Feinraster` (Vorgabe an, serialisiert), `FlottenOptimierer.Feinrasterwerte`; Regel und Gewinnbedingung im Rechenweg-Wiki, Abschnitt „Rastersuche" |
| Rechenknopf „Optimieren"/„Bewerten" | in der Seite von Schritt 4; `FLOTTE_SEITE_BTN_GROESSEN`/`_FLOTTE` tragen keine Ziffer mehr (die Stufenleiste zählt) |
| Kasten „Bestes Ergebnis" | drei Karten: Kapitalwert + jährliche Ersparnis (SD‑Q11), Kapazität · C-Rate · Leistung + Einheitenzahl, geprüfte/zulässige Kandidaten + Rechendauer; daneben die Marke Grob/Fein |
| Größen-Sicht von 5 nach 4 | `SpeicherFlottenGroessenAnsicht` steht im `OptimierungBlock`; Schritt 5 trägt stattdessen eine Hinweiszeile, welcher Kandidat dort bewertet ist |
| Schritt 1 nur Einheiten | Größenbereich → Station 4, „Netz und Planung" → `SpeicherFlottenNetzBlock` (Schritt 3), Jahresprojektion → `SpeicherFlottenWirtschaftBlock` (Schritt 2, SD‑Q12); Kostenblock unter eigener Überschrift „Kosten dieser Einheit" |
| Drei Erklärzeilen (7.3) | `FLOTTE_ED_AUSGLEICH_ERL`, `FLOTTE_ED_RESTWERT_ERL`, `FLOTTE_ED_KANDIDATEN_ERL` de/en |
| Stufenleiste, Zahlenrundung, kompakte Hinweise, Herleitungszeile, neutrale Pille, Kartenkopf | 7.8 vollständig; `Zahlen.HOECHSTE_NACHKOMMASTELLEN` gilt hausweit, `Seiten/Strom/Hinweiszeilen.razor` trägt die Vorprüfung |

**Abweichungen vom Vorschlag und vom Mockup — mit Grund:**

1. **Das Feinraster verfeinert nur die ERSTE aktive Suchachse**, die übrigen bleiben beim Wert des
   Grob-Optimums. Der Vorschlag sagte „auf der Größenachse" und meinte den Regelfall EINER Achse
   (nur dann ist das Raster zweidimensional und als Karte zeichenbar — Auflage aus #193). Mit zwei
   Achsen wäre „die Größenachse" mehrdeutig, und das volle Produkt aus feiner erster und ganzer
   zweiter Achse vervielfachte die Kandidatenzahl.
2. **Die Kandidatenzeile nennt für das Feinraster eine OBERGRENZE („bis zu n"), keine feste Zahl.**
   Wie breit das Fenster wird, hängt an der Lage des Grob-Optimums — am Rand wird es einseitig
   gekappt. Vor dem Lauf steht nur der größtmögliche Fall fest; gegen ihn wird die Grenze geprüft,
   damit ein zu großes Raster nicht erst auffliegt, nachdem Phase 1 verrechnet ist.
3. **Das Mockup zeigt 60 + 54 Kandidaten**, weil es das Feinraster über ALLE sechs C-Raten
   wiederholt. Das widerspricht SD‑Q10 („nur auf der Größenachse"): Hier bleibt die zweite Achse
   beim Wert des Grob-Optimums, das Feinraster hat deshalb so viele Punkte wie Stützstellen im
   Fenster.
4. **Grob- und Feinpunkte sind in der SCHNITTKURVE unterscheidbar, nicht in der Rasterkarte.**
   `ChartRenderer.Schnittkurve` zeichnet einen Feinpunkt in `C_FEINRASTER` und kleiner (ChartProbe
   `flottenschnitt_feinraster` samt Gegenprobe). Die Karte zeigt die Feinpunkte als eigene Zeilen —
   dort wäre eine zweite Farbe neben Dreifarbskala und Schraffur eine dritte Aussage in derselben
   Fläche.
5. **Die Stationstitel geben ihre ZIFFER an den Kreis ab** („Speicher" statt „1 Speicher"), auch die
   der Simulationsansicht. Sonst stünde die Nummer zweimal nebeneinander. Lage und Reihenfolge der
   Simulationsleiste bleiben unverändert (`Kompakt`, #216).
6. **Der Kasten „Bestes Ergebnis" nennt keine geschätzte Rechendauer VOR dem Lauf** (das Mockup
   schreibt „geschätzt unter 1 min"). Eine Schätzung bräuchte ein Maß für die Laufzeit eines
   Kandidaten, und das hängt an Jahreszahl, Betriebsziel und Solver — geraten wäre sie eine Zusage,
   die niemand einlöst. Die GEMESSENE Dauer steht nach dem Lauf im Kasten.
7. **„Netz und Planung" steht in Schritt 3 und nicht in Schritt 1** — der Vorschlag ließ das offen
   („prüfen, heute steht es im Flotteneditor"). Bezugs- und Einspeisegrenze sind harte Grenzen des
   Anschlusses, die Prognoseplanung sagt, mit welchem Wissen ein Fahrplan entsteht: Beides
   beschreibt den BETRIEB und nicht eine Einheit.

## 8. Zwei Suchmethoden — Anwenderrückmeldung 12.09.2026 (SD‑E‑10)

### 8.1 Rückmeldung

Zur Station „4 Optimierung" (Stand #224/#226, Bildschirmfoto mit dem Projekt „Stromspeicher Optimierung", eine Einheit
„Shenzhen Growatt WIT‑M+APX ESS [100 kW, 129,0 kWh]" aus dem Katalog, Suchraum 1–1 Stück, Kapazität 40–300/20, Leistung 40–400/20):

1. „Es ergibt keinen Sinn, die Variation der Leistung, Kapazität … bei einem vorgegebenen Speicher vorzunehmen. Die Variation ergibt
   nur Sinn für einen ohne Vorgabe des Speichertyps."
2. „Für mehrere Speicher ist die Variante die Anzahl der Speicher die Variation und nicht die Variation der Leistung, Kapazität …
   Das wäre eine zweite Methode."
3. „Stelle den Dialog für die Variation übersichtlicher dar und insgesamt ein verbessertes Design des Dialogs."

Dazu der Fehler, dass jedes Zahlenfeld der Suchraum-Tabelle bei jedem Tastendruck den Fokus verliert — der geht als **#245** getrennt
und vor diesem Kapitel (Ursache: die Seite ersetzt die Flottenkonfiguration je Änderung durch eine JSON-Tiefenkopie, und die Zeilen
hängen per `@key` an den Objektreferenzen).

### 8.2 Befund — was die Suche heute tut

- Es gibt **eine** Suchart. Jede Einheit der Flotte bekommt eine Suchachse (`FlottenAuslegungsAchse`) mit **Stückzahl von–bis UND**
  einem Größenraster aus zwei der drei Größen Kapazität, Leistung, C-Rate (Kopplung, die dritte folgt). Die Rastersuche
  (`FlottenOptimierer.BildeAchse`) läuft heute schon Stückzahl × erste Größe × zweite Größe; ein Kandidat ist eine vollständige
  Flotte je Betriebsziel.
- Die **Vorlage** der Achse ist die konkrete Einheit. Beim Katalogspeicher aus dem Bildschirmfoto erzeugt das Raster damit
  Fantasiegeräte von 40 bis 300 kWh mit den Kostensätzen des 129‑kWh‑Geräts — genau der Einwand aus 8.1 Punkt 1: Ein Katalog- oder
  Projektspeicher HAT eine Größe; variieren lässt sich bei ihm nur, wie viele davon stehen.
- Die **Herkunft** einer Einheit ist im Modell nur halb bekannt: `FlottenEinheit.AnlageId` unterscheidet Projektanlage von
  „keine Projektanlage"; Katalogeinheit und freie Einheit (#239, dritte Quelle „leer") sind ununterscheidbar. Station 4 liest die
  Herkunft nirgends — sie behandelt jede Einheit wie eine freie.
- Die Suchraum-Tabelle hat neun Spalten mit sieben kleinen Zahlenfeldern je Zeile, alle gleich gewichtet, ohne sichtbaren Bezug
  dazu, was die Einheit ist. Das ist Punkt 3.

### 8.3 Zielbild — zwei Suchmethoden, gewählt nach dem, was die Flotte enthält

**Methode G „Größe suchen"** (freie Auslegung). Sie gilt für Einheiten **ohne** Speichertyp — die freie Einheit aus #239 („leer")
oder eine dafür angelegte Auslegungseinheit. Variiert werden zwei der drei Größen nach Kopplung, genau wie heute; die Stückzahl
steht fest auf 1 (SD‑Q14). Die Kosten kommen aus den spezifischen Koeffizienten (€/kWh, €/kW, Betrieb, Ersatz) wie heute bei
`EigeneKosten`. Feinraster erlaubt. Ergebnis wie heute: Rasterkarte, Schnittkurve, Kandidatentabelle; „Kandidat übernehmen" setzt
der freien Einheit die gefundene Größe.

**Methode S „Stückzahl suchen"** (Bestückung mit konkreten Speichern). Sie gilt für Einheiten aus **Projektanlage oder Katalog**.
Die Größe ist die des Geräts und bleibt fest; variiert wird die Stückzahl von–bis je Einheit (0 = Einheit entfällt). Ein Kandidat ist
eine Kombination der Stückzahlen aller variierten Einheiten je Betriebsziel: Kandidatenzahl = Π (Stückzahlen) × Betriebsziele. Kein
Feinraster (ganze Zahlen). Kosten = Katalogkosten des Geräts × Stückzahl (Investition, Betrieb, Ersatz und Restwert je Gerät).
Ergebnis: bei EINER variierten Einheit die Kurve „Kapitalwert über Stückzahl" (Balken je Stückzahl), bei ZWEI die Rasterkarte
n₁ × n₂ (dieselbe Karte wie heute, Achsen ganzzahlig), darüber hinaus die Kandidatentabelle; „Kandidat übernehmen" setzt die
Stückzahlen in Schritt 1.

**Wahl in „Suche"**: drei Optionen — *Nur die eingestellte Flotte bewerten* · *Größe suchen (freie Einheit)* · *Stückzahl suchen
(Speicher aus Projekt oder Katalog)*. Eine Option ist nur wählbar, wenn die Flotte eine passende Einheit enthält; sonst steht sie
gedimmt mit Abhilfe („In Schritt 1 eine freie Einheit hinzufügen" bzw. „… einen Speicher aus Projekt oder Katalog aufnehmen").
**Mischflotte:** G variiert nur die freien Einheiten, die konkreten stehen fest mit ihrer Stückzahl; S variiert nur die konkreten,
die freien stehen fest mit ihrer Größe. Beides zugleich ist nicht Teil der ersten Stufe (SD‑Q16).

**Herkunft als Feld.** `FlottenEinheit.Herkunft` = *Projektanlage* | *Katalog* | *Frei*, gesetzt an den drei Anlegewegen aus #239
(`EinheitAusProjektanlage`, `EinheitAusKatalog`, leere Einheit), serialisiert im Stand (`Tab_SpeicherAuslegung`). Altbestand ohne
Feld: `AnlageId` gesetzt → Projektanlage, sonst Katalog — freie Einheiten gibt es erst seit #239, und wer eine hat, kann sie in
Schritt 1 als „freie Auslegungseinheit" kennzeichnen (SD‑Q15). Schritt 1 zeigt die Herkunft als Pille an jeder Einheit.

**Engine.** `FlottenAuslegungsAchse.Suchart` = *Groesse* | *Stueckzahl*. Bei *Stueckzahl* sind erste und zweite Größe je ein
Stützpunkt (der Gerätewert), `Kandidatenzahl` rechnet Π (Stückzahlen) × Ziele, `Feinrasterwerte` liefert nichts. Vorprüfung:
Suchart passt zur Herkunft (frei ↔ Größe, konkret ↔ Stückzahl), sonst benannte Ablehnung vor dem Lauf. Die Rastersuche selbst bleibt
— `BildeAchse` läuft heute schon Stückzahl × Größen; neu ist nur die Einschränkung der Achse und die Ergebnissicht über der
Stückzahl. Der Projektlauf (Stand `@Projektflotte`, Referenzprojekt 1046) ist nicht berührt; die Referenzbasis R7 bleibt.

### 8.4 Darstellung — Karten je Einheit statt einer Neun-Spalten-Tabelle (Punkt 3)

- **Kopfblock zweispaltig:** links „Suche" als drei untereinanderstehende Optionen mit je einem Erklärsatz; rechts das Ziel
  („Kapitalwert gegenüber ‚ohne Speicher' [€]"), die Kandidatenzeile („266 Kandidaten von höchstens 10 000 · Raster zulässig"), der
  Feinraster-Schalter (nur bei G sichtbar) und der Rechenknopf.
- **Suchraum als eine Karte je Einheit.** Kopfzeile: Name · Herkunftspille (*Projektanlage* / *Katalog* / *frei*) · feste
  Kenndaten („129 kWh · 100 kW") · Schalter „variieren". Rumpf je Methode: bei G die Kopplung und zwei Zeilen „Kapazität 40 – 300
  kWh, Schritt 20" / „Leistung 40 – 400 kW, Schritt 20" mit der Einheit IM Feld (Zeilenraster-Regel W6‑B‑4); bei S eine Zeile
  „Stückzahl 1 – 4" und darunter die Herleitungszeile „= 129 – 516 kWh · 100 – 400 kW · Investition 45 000 – 180 000 €". Fußzeile
  „Kandidaten dieser Einheit: 14". Einheiten, die die gewählte Methode nicht betrifft, stehen gedimmt mit „fest: 1 × 129 kWh".
- **Bestes Ergebnis:** die drei Karten von 7.9 bleiben; bei S nennt die zweite Karte die Stückzahl je Einheit statt
  Kapazität · C-Rate. Rasterkarte/Schnittkurve/Kandidatentabelle wie 8.3.
- Formularraster-Regeln (iU8‑E‑2), Bedienblock fester Breite wie #233, Ablaufleiste unverändert. Mockup
  `Mockups/stromspeicher-optimierung-v2.html` mit beiden Methoden am Beispiel des Bildschirmfotos (S: Growatt 1–4 Stück) und einer
  freien Einheit (G: 40–300 kWh × 40–400 kW).

### 8.5 Fragen (SD‑Q13 … SD‑Q18) mit Empfehlung

| Kennung | Frage | Empfehlung |
|---|---|---|
| SD‑Q13 | Methode ausdrücklich wählen (drei Optionen) oder automatisch aus der Herkunft ableiten? | **Ausdrücklich wählen.** Bei einer Mischflotte ist sonst nicht eindeutig, was variiert wird; die nicht passende Option bleibt gedimmt mit Abhilfe. |
| SD‑Q14 | Freie Einheit: Stückzahl fest 1 oder auch variierbar? | **Fest 1.** n gleiche freie Einheiten sind dieselbe Flotte wie eine n-fach größere; wer zwei verschieden große will, legt zwei freie Einheiten an. |
| SD‑Q15 | Herkunftsfeld und Regel für Altbestände? | **Feld `Herkunft` mit drei Werten;** alt ohne Feld: `AnlageId` → Projektanlage, sonst Katalog; in Schritt 1 umschaltbar auf „frei". |
| SD‑Q16 | Größe und Stückzahl in EINEM Lauf kombinieren? | **Nicht in S1.** Kandidatenzahl multipliziert sich; als spätere Option C, wenn gebraucht. |
| SD‑Q17 | Ergebnisdarstellung bei Stückzahl? | **Kurve/Balken über Stückzahl** (eine Einheit), **Rasterkarte n₁ × n₂** (zwei), Kandidatentabelle darüber hinaus; ChartProben +2 Bilder +1 Gegenprobe. |
| SD‑Q18 | Karten je Einheit statt Tabelle? | **Ja,** nach 8.4 und Mockup; die Tabelle bleibt nur in der Kandidatenliste (Ergebnis). |

### 8.6 Stufenplan

- **S1 (nach Entscheid SD‑E‑10):** Herkunftsfeld + Pille in Schritt 1; `Suchart` an der Achse, Vorprüfung, Kandidatenzahl und
  Feinraster je Suchart; Methodenwahl mit Abhilfen; Karten je Einheit; Ergebnissicht Stückzahl (Kurve, n₁ × n₂-Karte,
  Kandidatentabelle, „Kandidat übernehmen" setzt Stückzahlen); Ressourcen de/en; SpeicherEngine-, Kern- und bunit-Tests;
  ChartProben; Wiki-Bedienungsseite Schritt 4 und Rechenweg-Absatz „Rastersuche"; Referenzlauf 13/13 byte-gleich.
- **S2 (optional):** Kombination G + S (SD‑Q16, Option C) mit Kandidatengrenze.
- **Unabhängig davor:** #245 (Fokusverlust).

### 8.7 Anwenderentscheid SD‑E‑10

Offen (Stand 12.09.2026).
