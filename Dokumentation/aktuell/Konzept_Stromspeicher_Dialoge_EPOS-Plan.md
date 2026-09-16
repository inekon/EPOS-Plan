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
> gilt seither für jede Einheitenzahl. Der Einzelspeicher-**Optimierer** ist inzwischen ganz
> gefallen; das Betriebsbild des Berichts (`SpeicherBetriebsbild`) steht eigenständig und wird
> vom Stromspeicher-Reiter der Ergebnisseite gezeichnet. Einzelheiten in 4.1.

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
   mit den Gründen und je einem Knopf zur Abhilfe („Peak-Ziel bestimmen…", „Netzladung
   erlauben", „Zu Schritt 3 Betriebsführung"). Ein vierter Knopf — „Informationsstand auf
   Idealwissen setzen" — erscheint nur, wenn die Hinweisliste die Kennung `PrognoseFehlt`
   trägt (2.4 Punkt 7).
3. **Vergleichstabelle** mit **drei** Spalten „Ohne Speicher · Mit Flotte · Δ", Δ farbig
   (grün = besser), Einheiten im Spaltenkopf, Nullzeilen (Einspeisung 0/0/0) einklappbar.
4. **Jahresprojektion als Bild** (Balken Netto-Cashflow je Jahr, Linie kumuliert, Ersatzjahre
   markiert), die Tabelle darunter aufklappbar; CSV-Export wie bisher.
   **Seit dem 13.09.2026 (#249) steht über dem Bild die HERLEITUNG der Investition** — je
   Einheit eine `Herleitungszeile` `fest + kWh × €/kWh + kW × €/kW = Summe`, bei mehreren
   Einheiten darunter die Summenzeile und einmal der Satz, dass die größere der beiden
   Richtungsleistungen zählt; Anlass war die Anwenderfrage vom 12.09.2026, warum 150 €/kWh
   mal 1 395 kWh als 284 250 € erscheinen. Gerechnet wird dafür nichts nach:
   `SpeicherFlottenAnzeigeCtrl.Investitionsherleitung` liest die Anteile der Einheiten und holt
   die Summe aus `FlottenWirtschaftlichkeit.Investition` — derselben Funktion, deren Summe der
   CAPEX des Kapitalwerts ist.
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
7. **Prognosepflicht der planenden Betriebsziele** (`FlottenPlausibilitaet.Prognosepflicht`,
   Kennung `PrognoseFehlt`, Stufe **Problem**): `PvPlanung`, `Arbitrage` und `MultiUse` planen
   je Schritt aus einem Prognose-Snapshot. Für den vorbelegten Informationsstand „Archivierte
   Prognose-Snapshots" nimmt der Kern ausschließlich geladene Snapshots — die Istreihe wird nie
   still als Prognose verwendet. Ist keiner geladen, meldet die Vorprüfung es **vor** dem Lauf,
   der Rechenknopf ist gesperrt und nennt den Grund, und Aktivierung wie Projektlauf scheitern
   benannt statt in der Engine. Der Wortlaut nennt beide Auswege und steht an drei Stellen
   derselben Ressource: als Hinweiszeile über der Ablaufleiste, als Zeile unter dem Auswahlfeld
   „Informationsstand" in Schritt 3 und — nach einem Lauf, der ihn trägt — in der Liste des
   Diagnosebanners samt viertem Abhilfeknopf. **Die Vorbelegung bleibt „Archivierte
   Prognose-Snapshots"**: Der Informationsstand ist eine bewusste Wahl und wird nicht still
   umgestellt; die Abhilfe ist ein Knopf.
8. **Gültigkeit der Lebensdauerkurve** (`FlottenPlausibilitaet.Lebensdauerkurve`, Kennung
   `LebensdauerkurveUngueltig`, Stufe **Problem**): Die Bedingung steht in der Engine
   (`FlottenRainflow.PruefeKurve`) — genau die, an der die Rainflow-Auswertung abbricht:
   Entladetiefe über 0 bis 100 %, Zyklen über 0, jede Entladetiefe nur einmal; eine **leere
   Kurve ist zulässig**, dann zählt der Lauf nur die Zyklen. Ein im Einheiteneditor angelegter
   Punkt steht auf 0 %/0 Zyklen und ist damit „nicht ausgefüllt" — die Vorprüfung sagt es
   sofort und nennt Einheit, Punktnummer und Grund. Der Wortlaut steht an zwei Stellen derselben
   Ressource: als Hinweiszeile über der Ablaufleiste und als Zeile unter der beanstandeten
   Punktzeile im Block „Alterung und Grenzkosten", die dafür die Fehlerfarbe des Hauses und
   `aria-invalid` trägt. Der Rechenknopf ist gesperrt und nennt den Grund; Aktivierung und
   Projektlauf scheitern benannt statt in der Engine. Der Satz nennt beide Auswege: den Punkt
   ausfüllen oder ihn mit „Punkt entfernen" löschen.

Keiner dieser Punkte ändert einen Rechenwert eines gespeicherten Standes: Vorbelegungen greifen
nur bei neuen Studien, die Vorgabe „Netzladung" nur, wenn der Stand die Eigenschaft nicht trägt —
das ist vor dem Merge gegen Projekt 1046 (R7) zu prüfen (Stufenplan P1).

### 2.5 Ergebnisse über der Speichergröße (Punkt 3b)

Mit dem Schalter „Größen optimieren" zeigt Schritt 5 vor der Kandidatentabelle
(seit SD‑E‑8 für jede Einheitenzahl — die Größen-Sicht einer Flotte mit einer Einheit ist die
Größen-Sicht des Einzelspeichers):

- **Rasterkarte** Kapazität × Leistung (Farbe = Kapitalwert gegenüber „ohne Speicher", unzulässige
  Kandidaten schraffiert, Optimum markiert) — je Einheit wählbar, bei Anzahl > 1 die Summe;
- **Schnittkurve** Kapitalwert über der Kapazität bei fester Entladeleistung (Wahl als Schieber),
  daneben dieselbe Kurve über der Leistung;
- **Ausschnitt um das Optimum** — die Kurve über der Größenachse; sie erscheint nur, wenn ein
  Lauf Punkte außerhalb des Grobgitters geführt hat. Die Gerätesuche tut das nicht (siehe 8.9);
- **Kandidatentabelle** um Durchsatz (Vollzyklen/a), Bezugsspitze, Ersparnis, **Abweichung [%]**
  und das Kennzeichen **neutrale Kennwerte** erweitert, sortierbar, mit Spaltenfilter
  (Katalogfilter-Muster), „Kandidat übernehmen" je Zeile.

**Die Gesamtdarstellungen zeigen das Grobraster, der Ausschnitt das Feinraster.** Rasterkarte und
die zwei Schnitte tragen genau die eingegebenen Stützstellen; die Punkte der zweiten Phase liegen
dazwischen und bekämen dort eigene Zeilen, in denen jede andere Spalte leer bliebe. Alle Bilder
liefern `ChartRenderer.Optimierungsraster`/`.Schnittkurve`; die Flotte füttert sie aus
`FlottenKandidatZusammenfassung`. SP‑O‑4 („endliches Raster ist nicht global optimal") wird im Bild
benannt — und wenn ein Feinpunkt den Lauf gewonnen hat, nennt dieselbe Fußzeile Wert und Fundort
des besten Ergebnisses, weil die Karte dann das Grob-Optimum markiert.

**Die Achsen sind Kapazität [kWh] und Entladeleistung [kW]** — die einzige Größenkopplung, die
eine Suche erzeugt (siehe 8.9). `FlottenAuslegungErgebnis.Achsenmodus` trägt sie weiter, damit ein
aufbewahrtes Ergebnis seine eigene Auskunft behält; ein Ergebnis, das noch eine C-Rate-Marke trägt,
liest die Sicht wie bisher (Zeilen Kapazität, Spalten C-Rate beziehungsweise Zeilen Leistung,
Spalten C-Rate). **Die Karte ist dünn besetzt**, weil Geräte kein Gitter bilden: Zeilen sind die
verschiedenen Kapazitäten der gefundenen Geräte, Spalten ihre verschiedenen Entladeleistungen, und
nur besetzte Zellen tragen einen Wert. Jede leere Zelle ist eine Größenkombination, die niemand
baut — das ist die wahre Auskunft, ein lückenloses Gitter wäre eine erfundene. Titel, Achsen-, Schieber- und Bildbeschreibungstexte liefert je Kopplung
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
  (`EinzelVorbereiten`, `EinzelRechnen`, `Betriebsbild`, `RasterCsv`). `SpeicherOptimierungCtrl`
  und `SpeicherOptimierer` blieben damals stehen; sie sind inzwischen ebenfalls gefallen. Vom
  alten Umfeld bleiben der gespeicherte Stand und die Leistungspreis-Quellen
  (`SpeicherAuslegungVorgabenCtrl`) sowie das eigenständige Betriebsbild des Berichts
  (`SpeicherBetriebsbild`).

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
> (Darstellung). Was von diesem Kapitel abweicht, steht in 7.9. **Die Lage der Blöcke ist mit
> Auftrag #273 neu geordnet — es gilt 7.10.**

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
  6. **Ergebnis der Suche** — ~~direkt darunter~~ **seit 7.10 in Station 5**: Kasten
     **„Bestes Ergebnis"** (Kapitalwert, Kapazität, C-Rate, Leistung, Anzahl; geprüfte / zulässige
     Kandidaten; Rechendauer), die **Rasterkarte** und die **Kurve Kapitalwert über der Größe**
     (Grob- und Feinpunkte unterscheidbar), darunter die **Kandidatentabelle**.
     „Kandidat übernehmen" schreibt die Größe in Schritt 1 und markiert das Ergebnis als veraltet.
- **Schritt 5 Ergebnis:** die Bewertung der GEWÄHLTEN Flotte (Kacheln, Δ-Tabelle, Diagramme,
  Jahresprojektion) — **und seit 7.10 davor die Ergebnisse der Optimierung**.

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
| Größen-Sicht von 5 nach 4 | **Mit #273 zurückgenommen (7.10):** `SpeicherFlottenGroessenAnsicht` steht wieder in Schritt 5, jetzt in der Rubrik „Ergebnisse der Optimierung" (`OptimierungsergebnisBlock`); die Hinweiszeile, welcher Kandidat bewertet ist, bleibt |
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
4. **Das Feinraster steht im AUSSCHNITT um das Optimum, nicht in den Gesamtdarstellungen.**
   Rasterkarte und die zwei Gesamtschnitte führen allein die Kandidaten der Phase `Grob`; der
   Ausschnitt (`SpeicherFlottenAnzeigeCtrl.Ausschnittdaten`/`.Ausschnittbild`) ist eine Kurve über
   der Größenachse, begrenzt auf das Fenster des Feinrasters, und `ChartRenderer.Schnittkurve`
   zeichnet einen Feinpunkt darin in `C_FEINRASTER` und kleiner (ChartProbe
   `flottenschnitt_feinraster` samt Gegenprobe). In der Karte wäre eine zweite Farbe neben
   Dreifarbskala und Schraffur eine dritte Aussage in derselben Fläche; hat ein Feinpunkt gewonnen,
   markiert sie das Grob-Optimum und sagt es in der Fußzeile.
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

### 7.10 Anwenderentscheid SD‑E‑11 (14.09.2026): Ergebnisse gebündelt in Station 5, Bedienblock unter der Suche

> **Umgesetzt mit Auftrag #273.** Er nimmt den Teil von #224 zurück, der die Größen-Sicht nach
> Station 4 gezogen hat (7.4 Punkt 6, 7.9 Zeile „Größen-Sicht von 5 nach 4").

**Die Rückmeldung** (drei Bildschirmfotos der Station 4): *„Die Ergebnisse aus Tab Optimierung
(Größenabhängigkeit: Karte, Schnitte, Ausschnitt um das Optimum, Kacheln jährliche
Ersparnis/Einheiten/zulässig·Rechendauer) sollen in den ‚Ergebnis' Tab verschoben werden. Die
Ergebnisse sollen im Ergebnis-Tab strukturiert angezeigt werden: 1. Ergebnisse aus der
Optimierung, 2. die Ergebnisse nach Simulation. Der rechte Bereich ‚Optimierung' (Ziel,
Kandidatenzeile, Phase 2 Feinraster, Knopf Optimieren) soll auf die linke Seite unter die
‚Suche'-Auswahl."* Dazu am selben Tag: *„Bei der Suchart ‚Größe suchen' ist eine Speicherauswahl
nicht sinnvoll."*

**Das Zielbild.**

| Ort | Was dort steht |
|---|---|
| **Station 4 „Optimierung"** — reine EINGABE | 1. die **Suche** (drei Optionen mit Erklärsatz); 2. darunter, in **voller Breite**, der **Bedienblock** (Ziel, Kandidatenzeile, Schalter „Phase 2: Feinraster", Knopf „Optimieren" mit Fortschritt und Abbrechen); 3. der **Suchraum je Einheit**. Kein Ergebnis mehr |
| **Station 5 „Ergebnis"**, Rubrik 1 | **„Ergebnisse der Optimierung"**: Kasten „Bestes Ergebnis" (jährliche Ersparnis, Einheiten, zulässige Kandidaten · Rechendauer) und die **Größenabhängigkeit** (Rasterkarte bzw. Stückzahlkurve, Schnitte mit Schiebern, Ausschnitt um das Optimum, Kandidatentabelle mit „übernehmen" und CSV). Nur nach einem **Suchlauf**; nach „Nur bewerten" steht statt der Rubrik **eine Erklärzeile** |
| **Station 5**, Rubrik 2 | **„Ergebnisse der Simulation"**: Diagnosebanner, Hinweiszeile „welcher Kandidat steht hier", Ergebnisansicht (Kacheln, Δ-Tabelle, Diagramme, Jahresprojektion), Fußleiste (Projektflotte aktivieren, beste Flotte übernehmen, Größe in die Projektanlage) |

**Weitere Festlegungen.**

1. **Ein gelungener Lauf stellt Station 5 vorn** — dort stehen beide Ergebnisarten. Ein **Abbruch**
   und ein **Vorprüfungsbefund der Stufe „Problem"** lassen Station 4 mit ihrer Meldung stehen. Der
   Fortschritt bleibt während des Laufs unter dem Rechenknopf (Muster #220).
2. **Kein Baustein zweimal.** Die Größen-Sicht wird nur aus der Rubrik „Ergebnisse der Optimierung"
   gerufen; Station 4 kennt sie nicht mehr. „Kandidat übernehmen" führt weiter nach Schritt 1 und
   markiert das Ergebnis als veraltet.
3. **Die Rubrik beschreibt den GEFAHRENEN Lauf, nicht die gerade gewählte Suchart.** Die Ansicht
   hält die Suchmethode des Laufs fest; wer danach in Station 4 umstellt, ändert den nächsten Lauf.
   Davon hängt auch ab, ob „Kandidat übernehmen" eine Stückzahl oder eine Größe schreibt.
4. **Unter „Größe suchen" nennt der Suchraum kein Gerät.** Keine Kopfzeile mit Hersteller, Typ und
   festen Kenndaten, kein Schalter „variieren": Dort variiert der Lauf Kapazität, Leistung und
   C-Rate, und welches Produkt dahintersteht, beantwortet keine der gestellten Fragen. **Jede
   Einheit wird variiert** — die Station gleicht den Stand entsprechend an —, mehrere Einheiten
   heißen neutral „Einheit 1", „Einheit 2", und eine Erklärzeile sagt, warum das Gerät hier keine
   Rolle spielt. Die Größensuche ist damit nur noch gesperrt, wenn es gar keine Einheit gibt.
   **„Stückzahl suchen" und „Nur bewerten" bleiben unverändert** — dort zählt die konkrete Einheit.

> **Dieser Befund beschreibt einen Rechenweg, den es nicht mehr gibt — die Frage am Ende ist
> mit 8.9 beantwortet.** Der Größenlauf rastert seit dem Anwenderentscheid vom 15.09.2026
> keine freie Größe mehr, sondern wählt **Geräte**; welcher Kennwert vom Gerät kommt und
> welcher Eingabe des Anwenders bleibt, legt `FlottenGeraeteuebernahme` je Kennwert fest
> (Tabelle in 8.9). Das Verhältnis von Lade- zu Entladeleistung wirkt gar nicht mehr, weil
> beide Leistungen vom Gerät kommen. Der Absatz bleibt als Befundlage stehen.

**Was vom Gerät trotzdem in den Größenlauf eingeht** (Befund am Rechenweg, `FlottenOptimierer`
und `SpeicherFlottenStudieCtrl.Konfiguration`): Der Kandidat entsteht als **Kopie der
Achsenvorlage**; überschrieben werden nur Kapazität und die beiden Leistungen. Es bleiben
**Lade- und Entladewirkungsgrad**, das **SoC-Band**, **Peak-Reserve** und **Hilfsverbrauch** (beide
ungeskaliert), die **Grenzverschleißkosten je entladener kWh**, die **Rainflow-Kurve** der Alterung
und das **Verhältnis von Lade- zu Entladeleistung** (beide werden mit demselben Faktor skaliert, die
Asymmetrie des Geräts bleibt also an jedem Rasterpunkt). Die **Investitions- und Betriebskosten
kommen aus Schritt 2** (€/kW, €/kWh, Betrieb) und werden mit der gesuchten Größe skaliert —
**außer** die Einheit trägt „eigene Kosten"; dann gelten ihre Werte, und die Pauschalen
(Investition, fixer Betrieb, Ersatzkosten, Ersatzintervall, Restwert) gehen ungeskaliert in jeden
Kandidaten ein. Ein Größenlauf braucht außerdem eine Vorlage mit **positiver Richtungsleistung**,
sonst weist der Optimierer ihn benannt ab. Ob diese Größen neutralisiert werden sollen, ist eine
offene Frage an den Anwender; die Anzeige ist davon unberührt.

**Umsetzung:** Auftrag **#273** — `OptimierungBlock` (Kopf einspaltig, Suchraum ohne Produktzeile
unter G), neuer Baustein `OptimierungsergebnisBlock`, Station 5 mit zwei `Gruppenkopf`-Rubriken,
`.epos-flotte-optimierung-kopf` einspaltig, Ressourcen de/en, bunit-Fälle, Mockup und Wiki-Quelle
nachgezogen.

---

## 8. Zwei Suchmethoden — Anwenderrückmeldung 12.09.2026 (SD‑E‑10)

> **Umgesetzt mit Auftrag #247.** Zielbild 8.3 und Darstellung 8.4 vollständig, samt der
> Übernahme ins Projekt aus 8.3. Was davon abweicht, steht in 8.8.

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

### 8.3 Zielbild — zwei Suchmethoden je Lauf, Einheiten ohne Typ (Entscheide SD‑Q13 … SD‑Q16 vom 12.09.2026)

**Grundsatz (SD‑Q15):** Die Einheiten der Flotte tragen KEINE Herkunftsmarkierung. Sie sind Arbeitsstücke der Optimierung —
woher ihre Zahlen kommen (Projektanlage, Katalog, Handeingabe über die dritte Quelle „leer" aus #239), spielt für die Suche keine
Rolle. WELCHE Einheiten variiert werden, entscheidet der Anwender je Einheit mit dem Schalter „variieren" auf ihrer Karte; WAS an
den variierten Einheiten variiert wird, entscheidet die gewählte Methode. Der Bezug `FlottenEinheit.AnlageId` bleibt, was er ist:
die Verbindung zu einer Speicheranlage des Projekts (Badge „vertreten", Nachzugszeile in Schritt 1) — keine Typkennung.

**Wahl in „Suche" (SD‑Q13, ausdrücklich):** drei Optionen untereinander — *Nur die eingestellte Flotte bewerten* · *Größe
suchen* · *Stückzahl suchen*. Eine Suchoption ist wählbar, sobald mindestens eine Einheit „variieren" trägt; sonst steht sie gedimmt
mit der Abhilfe „Auf einer Einheitenkarte ‚variieren' einschalten".

**Methode G „Größe suchen".** Variiert an jeder eingeschalteten Einheit zwei der drei Größen nach Kopplung (Kapazität, Leistung,
C-Rate — wie heute); die Stückzahl der Einheit steht fest auf dem Wert ihrer Karte. Kosten: die spezifischen Koeffizienten der
Einheit (€/kWh, €/kW, fix, Betrieb, Ersatz) × Stückzahl. Feinraster erlaubt (erste eingeschaltete Einheit, wie heute). Ergebnis wie
heute: Rasterkarte, Schnittkurve, Kandidatentabelle; „Kandidat übernehmen" setzt die gefundene Größe in die Karte(n).

**Methode S „Stückzahl suchen" (SD‑Q14, SD‑Q16).** Variiert an jeder eingeschalteten Einheit die Stückzahl von–bis (0 = Einheit
entfällt); die Größe steht fest auf dem Wert der Karte. Das gilt für Katalog- und Projektspeicher ebenso wie für eine von Hand
eingegebene Einheit — SD‑Q14: „es kann auch mehrere Einheiten geben, die in der Summe etwas anderes sind als eine verfügbare
größere." Kandidatenzahl = Π (Stückzahlbereiche der eingeschalteten Einheiten) × Betriebsziele. Kein Feinraster (ganze Zahlen).
Kosten = Kosten einer Einheit × Stückzahl (Investition, Betrieb, Ersatz und Restwert je Stück). Ergebnis: bei EINER variierten
Einheit die Kurve „Kapitalwert über Stückzahl" (Balken je Stückzahl, Bestwert hervorgehoben), bei ZWEI die Rasterkarte n₁ × n₂
(dieselbe Karte wie heute, Achsen ganzzahlig), darüber hinaus die Kandidatentabelle; „Kandidat übernehmen" setzt die Stückzahlen in
die Karten.

**Je Lauf EINE Variationsart (SD‑Q16 „nur Stückzahl variieren").** Größe und Stückzahl werden nie im selben Lauf variiert: unter G
ist die Stückzahl fest, unter S die Größe. Wer beides prüfen will, fährt zwei Läufe. So bleibt die Kandidatenzahl beherrschbar
(266 Größen ODER 4 Stückzahlen je Einheit, nicht 1 064), und Karte und Kurve bleiben zweidimensional. Das ursprüngliche Zielbild
„die Herkunft entscheidet die Methode" ist damit gegenstandslos.

**Übernahme ins Projekt (SD‑Q15).** Neu in Schritt 1: je Einheitenkarte ein Auswahlkästchen und der Knopf „Ausgewählte Einheiten
in Projekt übernehmen". Er macht aus den gewählten Einheiten Speicheranlagen des Projekts — **je Stück eine Anlage** (die
Projekttabelle `Tab_Stromspeicher` kennt keine Stückzahl), benannt nach der Einheit, bei mehreren Stück mit laufender Nummer; die
Werte gehen über die Umkehrung derselben Kern-Abbildung, mit der eine Projektanlage heute gelesen wird
(`StromspeicherSimCtrl.LeseParameter` bzw. `SpeicherFlottenStudieCtrl.EinheitAusKatalog`: Energie, Leistung, Wirkungsgrad,
Ladezustand, Modul-/Leistungskosten, Investition fix, Standby). Eine Einheit, die bereits eine Projektanlage vertritt (`AnlageId`
gesetzt), schreibt ihre Werte in DIESE Anlage zurück statt eine zweite anzulegen; bei Stückzahl > 1 entstehen die weiteren als neue
Anlagen. Die Übernahme fragt vorher nach („n Speicheranlagen werden angelegt, m geändert — fortfahren?") und läuft in einer
Transaktion; danach tragen die Einheiten ihren Bezug, Badge und Nachzugszeile stimmen. Der gespeicherte Auslegungsstand bleibt davon
unberührt — der Projektlauf rechnet weiterhin den aktivierten Stand `@Projektflotte` (SP‑O‑8), und die Referenzprojekte kennen den
Knopf nicht.

**Engine.** `FlottenAuslegungEingang.Suchmethode` = *Bewerten* | *Groesse* | *Stueckzahl* (serialisiert im Stand; alte Stände ohne
Feld laden als *Groesse*, und ihre Stückzahlbereiche gelten als fest auf dem Von-Wert — ein alter Stand rechnet damit wie ein neuer
unter G, das heutige Mischraster Stückzahl × Größe gibt es nicht mehr). `FlottenOptimierer.Kandidatenzahl`, `Achsengroesse`,
`BildeAchse` und `Feinrasterwerte` lesen die Methode: unter *Groesse* zählt die Stückzahl als ein Stützpunkt (AnzahlVon), unter
*Stueckzahl* zählen erste und zweite Größe je einen Stützpunkt (die Vorlage). Vorprüfung: eine Suchmethode ohne eingeschaltete
Einheit lehnt benannt ab, ein Stückzahlbereich mit Bis < Von ebenso. Der Projektlauf (Stand `@Projektflotte`, Referenzprojekt 1046)
ist nicht berührt; die Referenzbasis R7 bleibt.

### 8.4 Darstellung — Karten je Einheit statt einer Neun-Spalten-Tabelle (SD‑Q18)

> Die Lage der Blöcke ist mit **7.10** neu geordnet: Kopfblock **einspaltig**, Ergebnis in Station 5.
> Was hier über die Bestandteile steht, gilt unverändert.

- **Kopfblock:** oben „Suche" als drei untereinanderstehende Optionen mit je einem Erklärsatz; **darunter in voller Breite** das Ziel
  („Kapitalwert gegenüber ‚ohne Speicher' [€]"), die Kandidatenzeile („266 Kandidaten von höchstens 10 000 · Raster zulässig", rot
  mit Abhilfesatz bei Überschreitung), der Feinraster-Schalter (nur bei G sichtbar) und der Rechenknopf.
- **Suchraum als eine Karte je Einheit.** Bei **S** trägt die Kopfzeile Name · feste Kenndaten („129 kWh · 100 kW · 1 Stück") ·
  Schalter „variieren"; bei **G** gibt es sie nicht (7.10). Rumpf je Methode: bei G die Kopplung und zwei Zeilen
  „Kapazität 40 – 300 kWh, Schritt 20" / „Leistung 40 – 400 kW, Schritt 20" mit der Einheit hinter dem kurzen Zahlenfeld
  (Zeilenraster-Regel W6‑B‑4); bei S eine Zeile „Stückzahl 1 – 4" und darunter die Herleitungszeile
  „= 129 – 516 kWh · 100 – 400 kW · Investition 45 000 – 180 000 €". Fußzeile „Kandidaten dieser Einheit: 14". Einheiten ohne
  „variieren" stehen unter S gedimmt mit „fest: 1 × 129 kWh · 100 kW". **Keine Herkunftspille** (SD‑Q15).
- **Bestes Ergebnis** (in Station 5, 7.10): die drei Karten von 7.9 bleiben; bei S nennt die zweite Karte die Stückzahl je Einheit
  („2 × … = 258 kWh · 200 kW") statt Kapazität · C-Rate. Rasterkarte/Kurve/Kandidatentabelle wie 8.3.
- **Schritt 1:** Auswahlkästchen je Einheitenkarte, Knopf „Ausgewählte Einheiten in Projekt übernehmen" neben „Speicher hinzufügen",
  Rückfrage mit Anzahl, Erfolgsmeldung mit den angelegten Anlagen.
- Formularraster-Regeln (iU8‑E‑2), Bedienblock fester Breite wie #233, Ablaufleiste unverändert. Das Mockup
  `Mockups/stromspeicher-optimierung-v2.html` (Fassung vor dem Entscheid: Herkunftspillen, Methode an die Herkunft gebunden) wird auf
  diesen Stand nachgezogen — Pillen fallen, jede Karte trägt den Schalter „variieren", unter S zeigt jede eingeschaltete Karte die
  Stückzahlzeile, unter G die Größenzeilen; Schritt 1 wird als zweite Ansicht mit Auswahl und Übernahmeknopf skizziert.

### 8.5 Fragen (SD‑Q13 … SD‑Q18) mit Empfehlung und Entscheid

| Kennung | Frage | Empfehlung | Entscheid (Anwender, 12.09.2026) |
|---|---|---|---|
| SD‑Q13 | Methode ausdrücklich wählen (drei Optionen) oder automatisch aus der Herkunft ableiten? | Ausdrücklich wählen | **Empfehlung** — drei Optionen; die Bindung an eine Herkunft entfällt mit SD‑Q15 |
| SD‑Q14 | Freie Einheit: Stückzahl fest 1 oder auch variierbar? | Fest 1 | **Variierbar** — „es kann auch mehrere Einheiten geben, die unterschiedlich in der Summe sind als eine verfügbare größere"; unter S variiert die Stückzahl JEDER eingeschalteten Einheit |
| SD‑Q15 | Herkunftsfeld und Regel für Altbestände? | Feld mit drei Werten, Altbestandsregel, Pille | **Keine Markierung** — „die Einheiten werden nur temporär für die Optimierung benötigt"; stattdessen der Knopf „Ausgewählte Einheiten in Projekt übernehmen" in Schritt 1 |
| SD‑Q16 | Größe und Stückzahl in EINEM Lauf kombinieren? | Nicht in S1 | **Nur Stückzahl variieren** — je Lauf eine Variationsart: G variiert die Größe bei fester Stückzahl, S die Stückzahl bei fester Größe |
| SD‑Q17 | Ergebnisdarstellung bei Stückzahl? | Kurve/Balken über Stückzahl (eine Einheit), Rasterkarte n₁ × n₂ (zwei), Kandidatentabelle darüber hinaus | **Empfehlung** |
| SD‑Q18 | Karten je Einheit statt Tabelle? | Ja, nach 8.4 und Mockup | **Empfehlung** |

Zwei Folgefragen hat die Orchestrierung ohne Rückfrage entschieden, weil sie den Entscheid nur ausführen: **Übernahme je Stück eine
Anlage** (die Projekttabelle kennt keine Stückzahl; eine n-fach große Anlage wäre eine andere Aussage als n Geräte) und
**Rückschreiben statt Dublette**, wenn die Einheit schon eine Projektanlage vertritt. Beides steht in 8.3 und lässt sich in der
Abnahme umkehren.

### 8.6 Stufenplan

- **#247 (Umsetzung, nach Entscheid):** Engine — `Suchmethode` mit Kandidatenzahl, Feinraster, `BildeAchse` und Vorprüfung je
  Methode, Laden alter Stände; Kern — Ergebnisdaten „Kapitalwert über Stückzahl" und n₁ × n₂-Karte in `SpeicherFlottenAnzeigeCtrl`,
  Übernahme ausgewählter Einheiten als Projektanlagen (Transaktion, Rückschreiben bei `AnlageId`, je Stück eine Anlage); ChartProben
  +2 Bilder +1 Gegenprobe; Oberfläche — Methodenwahl mit Abhilfe, Karten je Einheit mit „variieren", Kandidatenzeile und Feinraster je
  Methode, Ergebnissicht Stückzahl, „Kandidat übernehmen" für beide Methoden, Schritt 1 mit Auswahl und Übernahmeknopf; Ressourcen
  de/en; SpeicherEngine-, Kern- und bunit-Tests; Mockup nachgezogen; Wiki-Bedienungsseite (Schritt 1 und 4) und Rechenweg-Absatz
  „Rastersuche"; Referenzlauf 13/13 byte-gleich.
- **Später, nur auf Anwenderwunsch:** Größe und Stückzahl im selben Lauf mit Kandidatengrenze.
- **Erledigt davor:** #245 (Fokusverlust), Mockup v2 (#246).

### 8.7 Anwenderentscheid SD‑E‑10

**Entschieden am 12.09.2026** (Wortlaut in 8.5): SD‑Q13 Empfehlung · SD‑Q14 variierbar · SD‑Q15 keine Markierung, Übernahmeknopf ·
SD‑Q16 nur Stückzahl variieren (je Lauf eine Variationsart) · SD‑Q17 Empfehlung · SD‑Q18 Empfehlung. Umsetzung als #247.

### 8.8 Was #247 umgesetzt hat — und wo es vom Zielbild abweicht

**Umgesetzt** (Zielbild 8.3, Darstellung 8.4, Anwenderentscheid SD‑E‑10):

| Gegenstand | Wo es jetzt steht |
|---|---|
| Die drei Suchmethoden | `SpeicherEngine/FlottenModel.cs`: `FlottenSuchmethode` { `Bewerten`, `Groesse`, `Stueckzahl` }, `FlottenAuslegungEingang.Suchmethode` mit dem Eigenschaftsinitialisierer `Groesse` — ein Stand ohne das Feld lädt damit als Größensuche und rechnet weiter wie bisher (dieselbe Bauart wie `Feinraster` seit #224) |
| Was die Methode am Raster ändert | `FlottenOptimierer`: `Kandidatenzahl`, `Achsengroesse`, `FeinrasterHoechstzahl`, `Feinrasterwerte`, `BildeAchse`, `BildeFeinachse`, `BaueEinheiten` lesen sie. Unter `Groesse` zählt die Stückzahl als EIN Stützpunkt (`AnzahlVon`, 0 gilt als 1), unter `Stueckzahl` zählen erste und zweite Größe je einen (die Vorlage), unter `Bewerten` gibt es keine Suchachse. **Das Mischraster Stückzahl × Größe existiert nicht mehr** |
| Kein Feinraster unter S | `Kandidatenzahl` liefert `FeinHoechstens = 0`, `Feinrasterwerte` eine leere Liste, und `Rechne` startet Phase 2 nur unter `Groesse` — zwischen zwei ganzen Zahlen gibt es nichts zu verfeinern |
| Die Vorprüfung mit benannter Ablehnung | `FlottenSuchbefund` { `Inordnung`, `KeineAktiveAchse`, `StueckzahlbereichLeer`, `GroessenbereichUnbrauchbar` } als fünfter Teil von `FlottenKandidatenzahl`; `Rechne` wirft für jeden der drei Befunde eine eigene Meldung, die Ansicht sperrt den Rechenknopf und zeigt die Abhilfe |
| Die Stückzahl je Kandidat | `FlottenKandidatZusammenfassung.Stueckzahlen` — je aktiver Achse eine Zahl, vom Optimierer mitgeschrieben. Ohne sie ließe sich die Achse der Ergebnissicht nur aus Einheitenkennungen raten |
| Ergebnisdaten und Bilder der Stückzahlsuche | `EPOS.Kern/Controller/SpeicherFlottenAnzeigeCtrl.Stueckzahl.cs`: `VariierteEinheiten`, `Stueckzahlkurve` (+ `KandidatZuStueckzahl`), `Stueckzahlraster`, `Stueckzahlbild`, `Stueckzahlrasterbild`. Die Kurve zeichnet der neue Renderer `ChartRenderer.Stueckzahlkurve` (Balken, vorzeichenfähige Achse, Bestwert in `C_RASTER_GUT` samt Optimum-Marke, Schraffur für unzulässige Stückzahlen); die Karte n₁ × n₂ ist **dieselbe** `Optimierungsraster`-Zeichnung mit ganzzahligen Achsen |
| Übernahme ins Projekt | `SpeicherFlottenStudieCtrl.Uebernahme.cs`: `Vorschau` (die zwei Zahlen der Rückfrage, ohne Schreibzugriff) und `EinheitenInProjektUebernehmen` — je Stück eine Anlage über `Tab_Stromspeicher` **und** `AnlagenSql.SQL_ANLAGE_INSERT`, alles in EINEM `DbVorgang`, Bezeichner im Vorgang auf Eindeutigkeit geprüft, Rückgabe je Anlage samt Einheitenkennung |
| Station 4 nach 8.4 | `EPOS.UI/Seiten/Strom/OptimierungBlock.razor`: Kopfblock zweispaltig (drei Optionen mit Erklärsatz links, Bedienblock fester Breite rechts), Suchraum als **eine Karte je Einheit** mit Kopfzeile, methodenabhängigem Rumpf und Fußzeile „Kandidaten dieser Einheit"; die Neun-Spalten-Tabelle ist gefallen |
| Gedimmte Suchoption mit Abhilfe | `Optionsgruppe.WeichGesperrt` (aria‑disabled statt `disabled`, damit der Grund ankommt — W16b‑E‑6) plus die Zeile `.epos-flotte-abhilfe` unter der Gruppe |
| Bestes Ergebnis unter S | Die zweite Karte nennt die **Bestückung** („2 × Growatt … = 258 kWh · 200 kW") statt Kapazität · C‑Rate; die Phasenmarke entfällt (es gibt keine zweite Phase) |
| Ergebnissicht unter S | `SpeicherFlottenGroessenAnsicht.Methode`: bei einer variierten Einheit die Stückzahlkurve, bei zwei die Karte n₁ × n₂, darüber hinaus nur die Kandidatentabelle — Rasterkarte, Schnitte, der Ausschnitt um das Optimum und die zwei Schieber entstehen dann gar nicht |
| „Kandidat übernehmen" | Unter G wie bisher (der Kandidat wird die Flotte, die Achsen fallen); unter S setzt er `AnzahlVon = AnzahlBis = n` in die Karten der variierten Einheiten und lässt die Einheiten stehen |
| Schritt 1 mit Auswahl und Übernahmeknopf | `SpeicherFlottenEditor`: Auswahlkästchen je Einheitenkarte, Knopf neben „Speicher hinzufügen", Rückfrage mit den zwei Zahlen aus `Vorschau`, danach steht die `AnlageId` an der Einheit; ohne Delegat gibt es weder Kästchen noch Knopf, im Lesemodus ist er weich gesperrt |
| Ressourcen | 37 neue `FLOTTE_*`- und `KI_DLG_*`-Schlüssel de/en (`Werkzeuge/ResourceDesigner`), kein deutscher Literaltext in den zwei Razor-Dateien |
| Nachweise | `SpeicherEngine.Tests/FlottenSuchmethodeTests` (12), `EPOS.Kern.Tests/SpeicherFlottenStueckzahlCtrlTests` (8) und `…/SpeicherFlottenUebernahmeTests` (8, gegen eine Kopie der Testdatenbank), `EPOS.UI.Tests/StueckzahlsucheTests` (7) und der umgeschriebene `OptimierungStationTests`; `Proben/ChartProben` +2 Bilder +1 Gegenprobe (die Bestwertmarke der Stückzahlkurve ändert das Bild) |

**Abweichungen vom Zielbild und vom Mockup — mit Grund:**

1. **Die Karte zeigt ihre Bereiche im `Formularraster` mit je EIGENER Beschriftung** („Kapazität
   von", „Kapazität bis", „Schritt") und nicht als eine Zeile „Kapazität 40 – 300 kWh, Schritt 20".
   Das Mockup zeichnet die kompakte Zeile von Hand; im Haus gibt es dafür keinen Baustein — drei
   Eingaben in EINER `epos-feld`-Zeile müsste jeder Dialog selbst bauen, und genau das verbietet
   die Formularrasterregel (iU8‑E‑2). Die Beschriftungsschlüssel sind die des Einheiteneditors,
   also unverändert; die Kompaktheit kommt aus der Anordnung des Rasters.
2. **Die Abhilfe steht EINMAL unter der Optionsgruppe**, nicht je gesperrter Option. Beide
   Suchoptionen haben denselben einen Grund — es trägt keine Einheit „variieren" —, und zwei
   gleichlautende Warnzeilen untereinander sind eine Wiederholung ohne Gewinn. Am Bedienelement
   selbst steht der Grund zusätzlich als `title`/`aria-disabled` (weiche Sperre).
3. **Die Suchmethode steht an ZWEI Feldern desselben Standes.** `FlottenAuslegungEingang.Suchmethode`
   sagt, WAS variiert wird; `SpeicherAuslegungKonfiguration.FlottenGroessenOptimieren` bleibt der
   Schalter, an dem der Kern entscheidet, ob überhaupt eine Rastersuche läuft
   (`SpeicherFlottenStudieCtrl.Rechnen`). Die Ansicht zieht beide in EINEM Schreibweg gleich
   (`SuchmethodeSetzen`), und die Leserichtung (`Suchmethode`) kehrt einen alten Stand
   „optimieren = an, Methode = Bewerten" auf `Groesse` um. Ein einziges Feld hätte den
   Gate-Ausdruck des Kerns und damit den Projektlauf berührt — dafür gab es keinen Anlass.
4. **`Rechne` lehnt einen Lauf ohne aktive Achse ab, statt ihn als „ein Kandidat" zu rechnen.**
   Das Zielbild spricht nur von der gedimmten Option; ohne die Ablehnung im Kern hinge die Regel
   allein an der Oberfläche, und ein gespeicherter Stand mit abgeschalteten Achsen rechnete
   klaglos etwas anderes als der Anwender gewählt hat.
5. **Der LESEMODUS kommt als Delegat aus der Hülle** (`StromspeicherAuslegungDienste.Schreibgeschuetzt`),
   nicht aus einem `Schreibnaht`-Aufruf in der Razor-Komponente: Der Lesemodus ist eine
   Lizenzaussage, und eine Komponente stellt keine Lizenzfragen (Hausregel S‑2).
6. **Beim Rückschreiben in eine vertretene Anlage bleibt der BEZEICHNER stehen.** Geschrieben
   werden die acht Gerätewerte; ein Umbenennen zöge `Tab_Energieanlagen.Bezeichner` und die
   Zuordnung der Betriebsführung (`Tab_StromspeicherVariante` über die Anlage) nach sich und wäre
   eine zweite Aussage, die niemand verlangt hat.
7. **`Typ`, `Firma`, `Degradation`, `Zyklen_Zugesichert` und `Verschleisskosten` schreibt die
   Übernahme nicht** — sie sind genau die Spalten, die `EinheitAusKatalog` auch nicht LIEST
   (Konzept 1.8). Beim Anlegen bleiben sie leer, beim Rückschreiben unverändert stehen.
8. **Der Referenzlauf ist nicht berührt.** Der Projektlauf rechnet den aktivierten Stand
   `@Projektflotte` über `SpeicherFlottenProjektCtrl`, und der ruft die Rastersuche nicht
   (`FlottenGroessenOptimieren` wird dort ausdrücklich auf `false` gesetzt). Die Basis
   `2026-09-11_R7_Speicherflotte` und die Einfrierregel SP‑O‑8 bleiben unangetastet.

#### #248 — die Tiefenkopie je Tastendruck ist einer Fassungsnummer gewichen (13.09.2026)

**Was der Zustand war.** Seit #224 schreiben vier Blätter an derselben
`FlottenStudieKonfiguration`. Drei davon halten eine eigene Arbeitskopie und frischten sie nur
auf, wenn sich die **Referenz** änderte (`SpeicherFlottenEditor`,
`SpeicherFlottenBetriebEditor`, `SpeicherAuslegungEditor` — je ein `ReferenceEquals` in
`OnParametersSet`). Damit das zutraf, setzte `StromspeicherAuslegungSeite.FlotteGeschrieben()`
nach **jeder gemeldeten Eingabe** eine JSON-Tiefenkopie der ganzen Konfiguration ein
(`SpeicherAuslegungKopie.Von` = `JsonSerializer.Serialize`/`Deserialize`). Der Fokusverlust, den
das bis **#245** auslöste, war dort behoben (Zeilenschlüssel auf Wertidentität); geblieben war
der Aufwand.

**Gemessen, bevor gebaut wurde** (Linux, .NET 10, Release, Median über 1 000 Aufrufe):

| Kopie | Median | Mittel | p95 | Zuteilung |
|---|---|---|---|---|
| Flotte, typisch — 2 Einheiten, je 5 Rainflow-Punkte, 2 Achsen | **102–116 µs** | 113–136 µs | 155–216 µs | 14,7 kB |
| Flotte, groß — 10 Einheiten, je 20 Punkte, 2 Achsen | **527–783 µs** | 525–760 µs | 665–875 µs | 56,3 kB |
| Flotte, leer — 0 Einheiten | 9–27 µs | | | 3,8 kB |
| `SpeicherOptimierungEingaben.Kopie()` ohne Zeitreihe | **163 µs** | 179 µs | 228 µs | 17,8 kB |
| dieselbe Kopie **mit** eingelesener 8 760er Lastreihe | **3 305 µs** | 3 593 µs | 4 900 µs | **1,63 MB** |

(Zwei Läufe desselben Programms, daher die Spannen; die Zahlen entscheiden nichts — der
Anwender hatte den Umbau schon entschieden —, sie benennen den Preis.)

**Wozu die drei Editoren ihre Arbeitskopie halten** (Analyse): Sie **normalisieren** den
eingehenden Stand (`SpeicherFlottenEditor.Normalisieren` füllt fehlende Listen), sie **schreiben
in ihre eigene Kopie** und melden erst danach nach oben — eine halbfertige Eingabe erreicht den
Wirt also nie —, und sie führen Bedienzustand daneben (aufgeklappte Einheiten, Auswahl der
Übernahme). Rücknahme („Abbrechen") ist ausdrücklich **nicht** der Grund: Es gibt sie nicht.

**Was an der neuen Referenz hing — und was nicht.** Am Auffrischen der drei Arbeitskopien: ja,
das war ihr einziger Zweck. **Nicht** daran hingen: das „Veraltet"-Signal und der Merker
„ungespeichert" (beide setzt `Geaendert()`), der Kandidatenzähler und die Vorprüfung der
Station 4 (sie lesen die Konfiguration unmittelbar, es genügt der Zeichenlauf), die Entkopplung
des Ergebnisses (die macht `FlotteStarten()` mit `_eingaben.Kopie()` **zum Rechenzeitpunkt**,
und `SpeicherFlottenErgebnis.Konfiguration` ist deshalb der Stand des Laufs), das Speichern
(`EinstellungenSichern` reicht `e.Kopie()` weiter), der Blattwechsel (das alte Blatt wird
abgebaut, das neue liest beim ersten `OnParametersSet` ohnehin) und „Kandidat übernehmen"
(`FlotteSetzen` kopiert die Konfiguration des Ergebnisses — sie gehört dem Lauf und darf von
späteren Eingaben nicht verändert werden). Die Projektübernahme aus #247 meldet über denselben
Weg wie jede andere Eingabe.

**Der Umbau.** `StromspeicherAuslegungSeite` führt ein `int _fassung`, das **allein**
`Geaendert()` hochzählt — der eine Trichter, durch den jede gemeldete Änderung läuft. Die drei
Blätter bekommen es als Parameter `Fassung` (der `PeakZielBlock` reicht es durch) und frischen
auf, wenn sich **Referenz oder Fassung** geändert hat; ein Wirt ohne Fassung — der
`StromspeicherReiter` der Ergebnisseite — bleibt bei der reinen Referenzprüfung. Damit fällt in
`FlotteGeschrieben()` die Tiefenkopie ersatzlos weg, und zwei weitere Kopien fallen mit ihr,
weil sie dasselbe doppelt taten: `FlotteGeaendert` und `BetriebsoptionenGeaendert` bekamen den
Stand vom Editor bereits **als dessen eigene Kopie** herein und kopierten ihn ein zweites Mal;
ebenso `EingabenGeaendert`. Nebenbefund und mitgenommen: `SpeicherAuslegungEditor.OnParametersSet`
verglich `Wert` mit `_wert` — und weil `_wert` immer eine Kopie **von** `Wert` ist, traf das bei
**jedem** Zeichenlauf zu; der Editor kopierte den ganzen Arbeitsstand also auch dann, wenn sich
nichts geändert hatte (mit Zeitreihe: 3,3 ms je Zeichenlauf). Er vergleicht jetzt die Referenz
des Eingangs und die Fassung.

**Was bleibt.** Je Tastendruck bleibt genau die Kopie, die das meldende Blatt selbst herausgibt
— sie ist die Grenze zwischen „halbfertig im Editor" und „gilt" — und die Auffrischung der
Arbeitskopie des Blattes, das gerade liest. Beide sind handgeschrieben
(`SpeicherFlottenEditor.Kopie`), nicht JSON. Die drei fachlich nötigen Tiefenkopien (Lauf,
Speichern, Kandidatenübernahme) sind unberührt.

**Nachweise.** `EPOS.UI.Tests` 3 976 grün (vorher 3 973): drei neue Fälle in
`StromspeicherAuslegungFlotteTests` — eine Eingabe in der Betriebsführung erzeugt **keine** neue
Flotteninstanz (`Assert.Same`), Netzblock und Betriebseditor schreiben denselben Stand, und ein
Name aus Schritt 1 steht in der Suchraumkarte der Station 4. Die drei #245-Wachen sind
unverändert grün. **Der Referenzlauf ist nicht berührt** — der Umbau betrifft nur die
Oberfläche, kein Rechenweg des Kerns.

#### #253 — das Schrittfeld ließ sich nicht leeren, und der Balken stand außer Sicht (13.09.2026)

**Der Befund der Windows-Abnahme.** Im Suchraum der Station 4 (eine Einheit 4180 kWh / 125 kW,
Kopplung *Kapazität und Leistung*, Kapazität 50…5000 kWh Schritt 10, Leistung 50…5000 kW
Schritt 20, Feinraster an): „Fehler in Eingabefeld ‚Kapazität Schritt': 1 bleibt stehen,
Eingabe nicht korrekt möglich." Dazu die Ablehnung „123504 Kandidaten im Grobraster, bis zu 19
im Feinraster — zusammen 123523 von höchstens 10000" und, getrennt gemeldet: „Progress bar bei
Berechnung nicht mehr vorhanden."

**Ursache 1 — der Rückweg des Zahlenfeldes war zu.** Wer „10" rückwärts löscht, meldet erst
„1" — `OptimierungBlock.ZahlSetzen` schrieb sie in den Suchraum — und dann die **leere**
Eingabe. Ein leeres Feld meldet `null`; `ZahlSetzen` verwarf `null`, der Suchraum behielt seine
1, und `Zahlenfeld.OnParametersSet` sah einen Text, der den Wert nicht mehr meinte, und schrieb
die 1 in die Anzeige zurück. Damit ließ sich das Feld nicht leeren, jedes weitere Zeichen landete
hinter der 1 (im Browser steht die Schreibmarke nach dem Rückschreiben am Textende), und die
Schrittweite blieb bei 10 — woraus der zweite Befund folgt: Der Anwender **konnte** das Raster
gar nicht verkleinern.

**Zwei Hypothesen ausgeschlossen, mit Messwerten.** Der Live-Kandidatenzähler baut *kein* Raster
auf: `FlottenOptimierer.Kandidatenzahl` multipliziert Stützstellen (je Achse
`floor((bis − von)/schritt) + 1`, Obergrenze eingeschlossen). Ein ganzer Tastendruck der Ansicht
— Zählung, Vorprüfung des Suchraums, Neuzeichnen der Seite — kostet im bunit-Prüfstand
**0,4 bis 1 ms**, bei Schrittweite 1 kWh (1 232 799 Kandidaten) so viel wie bei 1000 kWh. Die
Zählung selbst nennt sogar ein Raster von fünf Milliarden Stützstellen ohne messbare Pause; ein
aufgebautes Raster gäbe es nie her. Auch die Fassungsnummer aus #248 setzt den Text nicht
zurück: Bei jeder **gültigen** Eingabe zeigte das Feld stets das Getippte.

**Die Behebung.** `Zahlenfeld` und `Ganzzahlfeld` merken sich, dass der Anwender das Feld
**geleert** hat, und schreiben bis zum nächsten Tastendruck nichts mehr in die Anzeige — dieselbe
Regel, nach der eine laufende Fehleingabe stehen bleibt: Der Anwender sieht, was er getippt hat,
auch das Nichts. Im Suchraum der Station 4 ist die leere Eingabe **die 0**, also ein benannt
ungültiges Raster: Die Kandidatenzeile sagt „Raster ungültig", die Vorprüfung sperrt den
Rechenknopf und nennt das Feld. Der alte Wert bleibt damit nicht unsichtbar stehen, und ein Lauf
mit einer Zahl, die niemand mehr sieht, ist ausgeschlossen.

**Die Kandidatenzahl der Abnahme ist richtig.** 496 Kapazitätsstufen (4950 kWh / 10 glatt, plus
Anfangswert) × 249 Leistungsstufen (4950 / 20 = 247,5 → 248 im Schrittmaß, die Obergrenze dazu)
= 123 504; das Feinraster zählt zwei Grobschrittweiten in Neunteln, also 19. Anzeige,
Fußzeile der Karte und Vorprüfung fragen dieselbe Regel. Der Sekundärfehler war die **Folge** des
hängenden Feldes, kein eigener Fehler.

**Ursache 2 — der Fortschrittsbalken stand außer Sicht.** Er sitzt seit jeher über der
Ablaufleiste, am Kopf der Ansicht; der Rechenknopf steht seit #224 in Station 4, am Ende einer
langen Seite. Wer ihn dort drückt, hat den Kopf nicht im Bild und sieht nur eine Seite, die
erstarrt. Station 4 zeichnet den Balken jetzt unter ihrem Rechenknopf, und die Ansicht lässt
ihren eigenen weg, solange diese Station vorn steht — EIN Lauf, EIN Fortschritt aus denselben
Werten, nur an der Stelle, an der geklickt wurde (Muster #220). Der Simulationslauf (Schritt ②
der Ansicht SIMULATION und der Startseiten-Reiter) war nicht betroffen; beide führen den Balken
an der Ergebnisseite und sind jetzt mit einer Wache belegt.

**Nachweise.** `EPOS.UI.Tests` und `SpeicherEngine.Tests` grün; neu sind fünf Fälle in
`EPOS.UI.Tests/Seiten/Strom/SchrittfeldUndFortschrittTests` (Eingabefolge, geleertes Feld sperrt
den Lauf, die Zahlen der Abnahme, der Balken in Station 4, der Balken auf den anderen Blättern),
je ein Fall in `ZahlenfeldTests` und `GanzzahlfeldTests` (ein geleertes Feld bleibt leer — beide
rot ohne die Behebung), acht Fälle in `SpeicherEngine.Tests/FlottenKandidatenzaehlungTests`
(Zählung ohne Rasteraufbau, Schrittweite ≤ 0) und eine Wache in `SimulationSeiteTests`.
**Der Referenzlauf ist nicht berührt** — die Zählregel und der Rechenweg sind unverändert.

#### #254 — die Vorprüfung läuft in zwei Stufen (13.09.2026)

**Der Befund, gemessen.** Jede Änderung eines Suchraumfelds in Station 4 rief über
`Geaendert()` die **volle** Vorprüfung: `Dienste.Vorpruefen` →
`StromspeicherAuslegungCtrl.Vorpruefen` → `SpeicherAuslegungCtrl.Vorbereiten` (Datenbank:
Modulkosten, Strompreisprofil, Vergütungen; dazu die Standortzeitreihen aus dem Lauf) →
`SpeicherFlottenStudieCtrl.Eingang` → `FlottenPlausibilitaet.Pruefe`. Am Prüfprojekt 1046 im
EPOS-Weg kostete das **im Median 69,7 ms und sechs Datenbankvorgänge je Tastendruck**
(100 Aufrufe, je ein geändertes Suchraumfeld). Die Aufschlüsselung nennt den Hauptposten: die
Vorbereitung 3,9 ms, die Prüfung selbst 0,8 ms — und `Eingang` samt `Konfiguration` **64,7 ms**.
Davon entfallen rund 2,7 ms auf den Aufbau der 35 040 Intervalle; der Rest sind die **zwei
SHA-256-Kennungen** (`DatenId`, `KonfigurationId`), die `Eingang` über den JSON-Text der ganzen
Reihe bildet. Der Kandidatenzähler des Blattes ist dagegen unter 1 ms (#253) und war nie das
Problem.

**Was von den Eingaben abhängt — und was nicht.** `Vorbereiten` liest aus dem Arbeitsstand die
Quellenwahl, die Kostenquellen, den übernommenen Projektflottenstand, die **Einheiten** der
Flotte (aus ihnen entsteht der aggregierte Parametersatz) und die Länge der Lastdatei. Den
**Suchraum** — Achsen, Schrittweiten, Stückzahlen, Feinraster, Suchmethode — liest sie
**nicht**. Genau daran setzt die Trennung an, und sie ist an EINER Stelle im Kern gezogen:
`SpeicherAuslegungCtrl.QuellenBeschaffen` holt alles aus Datenbank und Lauf,
`VorbereitenAusQuellen` setzt daraus den eingabenabhängigen Rest zusammen, und `Vorbereiten`
ruft beides nacheinander. Studien- und Projektlauf gehen unverändert über `Vorbereiten`.

**Der Zwischenspeicher.** `StromspeicherAuslegungCtrl` hält die beschafften Quellen samt
Istreihe unter einem Schlüssel, in dem alles steht, was die Beschaffung liest — Lauf-Fassung,
Projekt, Quellen- und Kostenwahl, Einheiten, Betriebsoptionen, Tarif, die Kennungen der
Dateireihen —, aber nichts vom Suchraum. Betriebsoptionen und Tarif stehen **vollständig**
darin, obwohl die Beschaffung nur einzelne Felder davon liest: Ein später hinzukommendes Feld
verwirft den Speicher dann von selbst. Verworfen wird er bei neuem Lauf (`LaufUebernehmen`),
bei neuen Vorgaben (`Vorgaben`) und in jedem Weg, der in die Projektdaten schreibt
(Projektflotte aktivieren, Einheiten übernehmen, Größe und Leistungspreis schreiben). Die Wache
ist der interne Zähler `Beschaffungen`, keine Zeitmessung: Vier Vorprüfungen mit geändertem
Suchraum ergeben **eine** Beschaffung, eine geänderte Einheit eine zweite.

**Die Istreihe ohne Kennungen.** `SpeicherFlottenStudieCtrl.Istwerte` baut die Standortreihe
ohne die zwei Kennungen; `Eingang` setzt sie darauf und hängt die Kennungen, die Prognosen und
die Projektjahre an. `FlottenPlausibilitaet.Pruefe` nimmt seither auch die Istreihe allein
entgegen — von einem `FlottenEingang` liest die Prüfung ohnehin nur sie.

**Zwei Stufen in der Ansicht.** Je Tastendruck läuft die **schnelle** Stufe
(`VorpruefenSchnell`): dieselben Regeln, aber ohne Standortreihe — also ohne Datenbank und ohne
Zeitreihen. Ohne Reihe entfallen genau die beiden Peak-Ziel-Prüfungen; der Betriebsaufwand und
der Start-Ladezustand erscheinen sofort. Die Kostensätze nimmt sie aus der letzten vollen
Prüfung, sonst aus dem gespeicherten Stand. Die **volle** Stufe läuft entprellt, 400 ms nach dem
letzten Zeichen (`CancellationTokenSource` + `Task.Delay` + `InvokeAsync`, kein `Task.Run`), und
außerdem sofort beim Öffnen, beim Stationswechsel, nach neuen Vorgaben und **vor dem Lauf** —
so begleitet keine veraltete Hinweisliste einen Start. Die Entprellzeit ist der Seitenparameter
`EntprellungMs` (Vorgabe 400, 0 = sofort), damit bunit sie abschalten kann; `Dispose` bricht eine
offene Entprellung ab. Die Hinweisliste bleibt **eine** Liste mit stabiler Reihenfolge: Die volle
Stufe ersetzt sie, sie ergänzt sie nicht.

**Die Sperre des Rechenknopfs hängt an keiner der beiden Stufen.** `Eingabefehler` rechnet bei
jedem Zeichenlauf neu aus der Konfiguration allein — Raster, Kandidatenzahl gegen die Grenze,
SoC-Band, Projektlaufzeit — und sperrt unverzüglich (#224, #253).

**Messung nach dem Umbau** (derselbe Messlauf): volle Stufe **Median 2,5 ms**, Mittel 3,0 ms,
**null Datenbankvorgänge** und **eine einzige Beschaffung** statt einer je Aufruf; die schnelle
Stufe liegt im Median bei **0,22 ms**.

**Nachweise.** Der Messlauf ist `EPOS.Kern.Tests/VorpruefungMessungTests` mit
`[Trait("Kategorie","Messung")]` — er prüft **keine** Zeitschranke, die Zahlen stehen im
Prüfbericht. Dazu vier Fälle in `StromspeicherAuslegungCtrlTests` (Zähler bei geändertem
Suchraum, Gleichheit der Hinweise gegenüber der frischen Beschaffung, Verwerfen bei neuem Lauf
und neuen Vorgaben, schnelle Stufe ohne Datenbankvorgang), zwei in `FlottenPlausibilitaetTests`
(ohne Reihe fehlen genau die Peak-Ziel-Hinweise; Eingang und Istreihe liefern dasselbe) und acht
in `EPOS.UI.Tests/Seiten/Strom/VorpruefungEntprelltTests`. **Referenzlauf Projekt 1046 gegen
`2026-09-11_R7_Speicherflotte`: PASS und byte-gleich** — der Rechenweg ist unverändert.

### 8.9 Die Größensuche wählt GERÄTE — Anwenderentscheid vom 15.09.2026

> „Größensuch Stromspeicher nur über Kapazität und Leistung im Katalog des Projektes oder
> wahlweise aus Stammdaten. Bei Such aus Stammdaten mit Möglichkeit der Übernahme aus Stammdaten
> in Projekt."

**Dieser Abschnitt gilt vor 8.2 und 8.3, wo diese vom Rastern einer freien Größe sprechen.**

**Was gesucht wird.** Der Anwender gibt je einen Bereich für Kapazität [kWh] und Leistung [kW] vor
und wählt die Quelle — **Projektkatalog** (`Tab_Stromspeicher` des offenen Projekts) oder
**Stammdaten** (`Tab_Stromspeicher_STAMM`). Gerechnet werden die Speicher, die es wirklich gibt;
es wird nichts skaliert und nichts gerastert. Es kommt heraus, was man kaufen kann.

**Die C-Rate ist keine Achse mehr.** Sie ist die abgeleitete Kennzahl `C = P / E` jedes Geräts und
steht in der Geräteliste und in der Kandidatentabelle, aber in keinem Eingabefeld.
`FlottenAuslegungsmodus.KapazitaetUndLeistung` ist die einzige gültige Kopplung; die zwei
C-Rate-Kopplungen sind Lesewerte älterer Stände. `FlottenAltstand.Normalisiere` setzt sie benannt
um: Der Leistungsbereich entsteht aus den Ecken `P = E · C`, der Kapazitätsbereich als `E = P / C`
(die schnellste C-Rate ergibt die kleinste Kapazität). Die Absicht des Anwenders bleibt damit
erhalten, statt still verworfen zu werden; ohne brauchbare C-Raten bleibt der gespeicherte Bereich
stehen, und die Vorprüfung sagt, was fehlt.

**Die Auswahlregel** (`FlottenGeraetewahl.Waehle`, eine Stelle für Kandidatenzeile, Geräteliste,
Kandidatentabelle und Lauf):

1. Ein Gerät ist ein **Treffer**, wenn seine Kapazität im Kapazitätsbereich **und** seine
   Entladeleistung im Leistungsbereich liegt (Grenzen eingeschlossen). Sein Abstand ist 0.
2. **Gibt es Treffer, gibt es nur Treffer** — wer einen Bereich vorgibt und Geräte darin findet,
   will nicht daneben rechnen.
3. Sonst kommen die **nächstliegenden**, höchstens fünf (`NAECHSTLIEGENDE_HOECHSTENS`).

Der Abstand je Größe ist der Überstand über den Bereich, bezogen auf die **Bereichsmitte**; beide
Anteile werden addiert:

```text
ueberstand(x) = x < von ? von − x : x > bis ? x − bis : 0
bezug         = (von + bis) / 2
Abstand       = ueberstand(E)/bezug_E + ueberstand(P)/bezug_P
```

Normiert wird, weil kWh und kW sonst nicht vergleichbar wären; die **Mitte** statt der Breite, weil
ein Bereich zu einem Punkt zusammenfallen darf (`von == bis`) und die Breite dort 0 wäre; die
**Summe** statt des Euklid, weil ein Gerät, das in beiden Größen danebenliegt, schlechter passt als
eines, das nur in einer danebenliegt — und weil sie ohne Wurzel auskommt. Sortiert wird nach
Abstand, Kapazität, Leistung und zuletzt der je Quelle eindeutigen Quellkennung; zwei Läufe auf
derselben Datenbank liefern dieselbe Reihenfolge. **Die Abweichung steht in der Kandidatentabelle**
(Spalte in Prozent), damit ein naheliegendes Gerät nicht für einen Treffer gehalten wird.

**Kandidatenzahl, Schranke, Feinraster.** Die Kandidatenzahl ist die Zahl der gefundenen Geräte mal
der Zahl der Betriebsziele — nicht mehr das Produkt zweier Rasterachsen. Sie kann nicht mehr
explodieren; die Schranke „Maximale Auslegungskandidaten" bleibt als Fangnetz für einen sehr großen
Bestand stehen. **Die zweite Suchphase entfällt**: Sie verfeinerte die Größenachse zwischen ihren
Stützstellen, was eine frei skalierbare Größe voraussetzt. Zwischen zwei Geräten liegt kein
drittes; eine zwischengerechnete Größe wäre ein Speicher, den es nicht gibt — dieselbe Begründung
wie bei „Stückzahl suchen" (SD‑Q16). `FlottenAuslegungEingang.Feinraster` bleibt als Lesefeld
älterer Stände erhalten und wirkt nicht mehr.

**Gerät ODER eigene Parameter — die EINE Regel** (Anwenderentscheid 15.09.2026,
`FlottenGeraeteuebernahme`): **Was der Gerätesatz führt, kommt vom Gerät; was der Anwender in
Schritt 1 gesetzt hat, bleibt seins.** Ein Kandidat entsteht deshalb aus der **Vorlage der
Suchachse**, und das Gerät überschreibt daraus genau das, was sein Satz wirklich trägt:

| Größe | Herkunft |
|---|---|
| Kapazität, Lade- und Entladeleistung | **immer vom Gerät** — sie sind der Gegenstand der Suche |
| Wirkungsgrade, SoC-Band, Hilfsverbrauch, Investitionssätze | vom Gerät, **wenn sein Satz sie führt**; sonst vom Anwender |
| Peak-Reserve, Grenzverschleiß, Betriebs- und Durchsatzkosten, Ersatz, Restwert, Alterungskurve | **immer vom Anwender** — ein Gerätesatz führt sie nie |

Bis dahin ersetzte `FlottenOptimierer.BaueGeraeteeinheiten` die Vorlage **vollständig** durch das
Gerät; die Eingaben aus Schritt 1 fielen bei jeder Gerätewahl weg, während sie unter „Stückzahl
suchen" stehen blieben — zwei Suchmethoden, die verschieden rechnen.

**Was „geführt" heißt, entscheidet der Kern**, nicht die Engine: Nur beim Lesen der Quelle ist zu
sehen, ob ein Wert aus dem Satz oder aus einer neutralen Vorgabe stammt
(`FlottenGeraeteuebernahme.Gefuehrt`, gesetzt in `SpeicherFlottenStudieCtrl.Geraetekandidaten`).
Fehlt danach noch etwas, greifen die neutralen Vorgaben aus `FlottenGeraetevorgaben` an EINER
Stelle: Lade- und Entladewirkungsgrad je 95 %, SoC-Fenster 10 bis 90 %, keine Alterung. Der
Kandidat wird dabei gekennzeichnet. Ein Katalogsatz führt keine Betriebsführung (das SoC-Fenster
steht nirgends in `Tab_Stromspeicher_STAMM`) und trägt die Kennzeichnung deshalb immer.

**Die Herleitung steht an EINER Stelle sichtbar**: Die Kandidatentabelle trägt die Spalte
„Herleitung" (`SpeicherFlottenAnzeigeCtrl.Herleitung`), eine Zeile je Kandidat — „Gerät: … ·
eigene Eingabe: …". Sie bleibt leer, wo es kein Gerät gibt („Stückzahl suchen") oder wo mehrere
Suchachsen mehrere Geräte liefern; eine gemeinsame Zeile wäre dort eine Behauptung über zwei Sätze.

**Die Übernahme ins Projekt** nimmt den Weg aus #247
(`SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen`, SD‑Q15): Aus den gewählten Einheiten
werden Speicheranlagen des Projekts, je Stück eine, alles in einer Transaktion. Ein zweiter Weg
wird nicht gebaut — er wäre eine zweite Wahrheit darüber, was „in das Projekt übernehmen" heißt.
Die Stammdaten bleiben dabei unberührt.

**„Stückzahl suchen" bleibt unverändert** (#246/#247): Dort ist das Gerät fest und die Stückzahl die
Variable. Die beiden Methoden ergeben das Paar **welches** Gerät und **wie viele** davon.
