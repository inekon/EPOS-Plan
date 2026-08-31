# Konzept: EPOS-Plan autonom auf dem iPad

**Rev. 1 — 30.08.2026 — zur Abnahme durch Philipp**

Auftrag: Die App soll unter iOS auf dem iPad **autonom** laufen — volle Funktion, Datenhaltung auf
dem Gerät, keine Serverpflicht. Dieses Dokument klärt, was das technisch bedeutet, was übernommen
werden kann, was neu entsteht, und in welcher Reihenfolge — **nur Konzept, keine Umsetzung**.

Vermessungsbasis: Repo-Stand `Pufferspeicher` vom 30.08.2026, Zählungen mit Fundstellen in § 2.

---

## 1 Die Kernaussage vorweg

**Eine Portierung im Wortsinn gibt es nicht.** WinForms und die Access-Engine existieren auf iOS
nicht und werden dort nie existieren. Was es gibt, ist ein realistischer Weg mit klarer
Arbeitsteilung:

| Schicht | Anteil am Bestand | Weg nach iOS |
|---|---|---|
| **Rechenkern + Rechner** (Simulation, Wirtschaftlichkeit, Modelle) | ~200 Dateien, größtenteils plattformfrei | **mitnehmen** — C# läuft unter .NET auf iOS |
| **Datenhaltung** (Access/OLE DB) | 179 Dateien mit DB-Zugriff, 145-MB-`Kenndaten.accdb`, 20 gespeicherte Access-Abfragen | **ersetzen** durch SQLite + Konverter |
| **Oberfläche** (WinForms/WPF) | 204 View-Dateien | **neu bauen** — und zwar für Touch, nicht als 1:1-Abbild |

Das Entscheidende: **Die ersten und wichtigsten Etappen finden auf Windows statt**, nicht auf dem
Mac. Die Trennung von Rechenkern und Oberfläche, die die Portierung erzwingt, ist genau die
Entkopplung, von der auch die Windows-Anwendung profitiert (Testbarkeit, Ende der doppelten
Schema-Wahrheiten). Das Risiko des Vorhabens sinkt damit nicht linear, sondern vorne.

---

## 2 Bestandsvermessung (30.08.2026)

| Größe | Zahl | Bedeutung für iOS |
|---|---|---|
| `.cs`-Dateien gesamt (ohne bin/obj) | 569 | — |
| View-Dateien (ohne Designer) | **204** | vollständiger UI-Neubau |
| Dateien mit `DataRepository`/`OleDb` | **179** | neue Datenschicht |
| `RecordSet`-Altbestand (String-SQL, Jet-Dialekt) | **61** | SQL-Dialektübersetzung oder Ablösung |
| gespeicherte Access-Abfragen, im Code referenziert | **20** | Fachlogik **in** der `.accdb` — muss extrahiert werden |
| `Kenndaten.accdb` | 145 MB | Konverter nach SQLite |
| Rechenkern `BhkwPlan.cs` | 410 Zeilen, **0** Windows-APIs | läuft unverändert |
| Wirtschaftlichkeitsrechner ohne DB-Zugriff | 12 von 20 Dateien | läuft unverändert |
| Simulationsmodule ohne DB-Zugriff | 11 von 25 Dateien | läuft unverändert |
| Registry-Zugriffe | 3 Dateien | → `Preferences` |
| DPAPI (Lizenz) | 2 Dateien | → iOS-Keychain |
| Excel-COM-Interop | 2 Dateien (`GanglinienDatei`, `ToolsClass`) | → ClosedXML-Lesen (läuft schon im Projekt) |
| `System.Drawing`/GDI+ im Bericht | `ChartRenderer.cs` | **blockiert**: `System.Drawing.Common` ist seit .NET 6 Windows-only → SkiaSharp/ScottPlot |
| `WinForms.DataVisualization`-Charts | 39 Dateien | Teil des UI-Neubaus |
| Nicht-UTF-8-kodierte Dateien | 93 | beim Verschieben in geteilte Projekte einmalig sauber konvertieren |

**Was ohne Änderung auf iOS läuft:** MathNet.Numerics, SkiaSharp, ScottPlot (Version 5),
ClosedXML, DocumentFormat.OpenXml, BouncyCastle, `Mscc.GenerativeAI` (REST). Die Berichtserzeugung
Word/Excel ist damit portabel — nur der Chart-Renderer nicht.

---

## 3 Leitentscheidungen (Vorschlag)

| Nr. | Entscheidung | Begründung |
|---|---|---|
| **iL1** | **Ein geteilter Kern, zwei Oberflächen.** Es entsteht eine Bibliothek `EPOS.Kern` (netstandard-frei, reines `net8.0`) mit Modellen, Rechenkern, Simulation, Wirtschaftlichkeit und `DbWerte` — referenziert von der bestehenden Windows-App **und** der iOS-App. Kein Fork: Eine Fachänderung wird einmal gemacht und wirkt auf beiden Plattformen. | Ein Fork stürbe binnen eines Jahres — die Wirtschaftlichkeit allein hat 2026 über zwanzig Etappen gesehen. |
| **iL2** | **Datenzugriff hinter einer Schnittstelle** (`IDatenzugriff`): Windows implementiert sie mit ACE/OLE DB wie heute, iOS mit **SQLite**. Der Konverter `.accdb` → `.sqlite` ist ein Windows-Werkzeug (dort gibt es ACE) und wandelt Kataloge **und** Projektdaten. | SQLite ist auf iOS Systembestandteil, dateibasiert wie Access, transaktional, und trägt 145 MB mühelos. |
| **iL3** | **Die 20 gespeicherten Access-Abfragen werden in Code geholt** — als SQL-Konstanten oder C#-Logik in `EPOS.Kern`, mit Einzelnachweis gegen die Access-Fassung. Danach ist die `.accdb` reine Datendatei. | Fachlogik in der Datenbankdatei ist heute schon eine bekannte Schwäche (Bestandsaufnahme § 8.1); iOS erzwingt nur, was ohnehin ansteht. |
| **iL4** | **Oberfläche als .NET MAUI mit Blazor Hybrid** — eine native App-Hülle, die Masken als Blazor-Komponenten. Voll offline, voller Gerätezugriff (Dateien, Share-Sheet, Keychain). | Gegenüber MAUI-XAML: 204 Masken sind als Web-Komponenten schneller und wartbarer zu bauen, das Layout-Know-how ist breiter verfügbar, und dieselben Komponenten taugen später für eine Browser-Fassung. Gegenüber Avalonia: Microsoft-gestützter, breiterer iOS-Pfad. |
| **iL5** | **Touch-first, nicht Fenster-Nachbau.** Kein MDI, keine modalen Ketten, keine 26-Felder-Dialoge. Der Wizard-Workflow (Projekt → Bedarf → Erzeuger → Simulation → Bericht) wird zur Navigationsstruktur; die in 2026 entstandenen Konzepte (Kostendialoge, B5-Feldkarte, Erlösrubrik) sind die Fachvorlage, ihre Feldlisten gelten 1:1. | Ein 1:1-Nachbau der Desktop-Dialoge wäre auf Touch unbenutzbar und doppelt so teuer. |
| **iL6** | **Charts einheitlich auf ScottPlot 5 / SkiaSharp** — auf iOS zwingend (GDI+ fehlt), auf Windows als spätere Ablösung der 39 `DataVisualization`-Stellen. Der Berichts-`ChartRenderer` wird zuerst umgestellt, weil er ohne UI testbar ist. | Ein Chart-Stack statt heute drei. |
| **iL7** | **Referenzläufe sind das Abnahmeinstrument der Portierung.** Der Kern gilt als portiert, wenn die iOS-Fassung (Simulator genügt) die Referenzprojekte rechnet und die Ergebnis-CSV **wertgleich** zur Windows-Basis sind. | Das Regressionsnetz existiert (`Referenzlaeufe\`, aktuelle Basis `2026-08-30_B3-Kaskade`) — es ist der einzige Beweis, der zählt. |
| **iL8** | **Lizenz über Keychain + signiertes Token** — `LizenzManager`-Logik bleibt (BouncyCastle läuft), nur die DPAPI-Ablage wird durch die iOS-Keychain ersetzt; `GeraeteId` über `identifierForVendor`. App-Store-Regeln zum Verkauf außerhalb des Stores sind zu prüfen (§ 7). | Kleinster Eingriff in ein funktionierendes System. |

---

## 4 Was „autonom" konkret heißt — und wo die Grenzen liegen

| Funktion | auf dem iPad | Anmerkung |
|---|---|---|
| Projekte anlegen, Bedarf, Erzeuger, Kataloge | ✅ lokal | Kataloge kommen als SQLite-Seed mit der App |
| Simulation (8760 h, Kaskade, Speicher) | ✅ lokal | Apple-Silicon rechnet das schneller als die meisten Bürorechner |
| Wirtschaftlichkeit (Kapitalwert, KWKG, Steuern) | ✅ lokal | Gesetzeskatalog liegt in der Projektdatenbank |
| Word-/Excel-Bericht | ✅ lokal | OpenXML/ClosedXML; Ausgabe über das Share-Sheet (Mail, Dateien, AirDrop) |
| VDI-3805-/CEC-/PAN-Import | ✅ lokal | über den iOS-Dateidialog statt fester Pfade |
| Excel-Ganglinien-Import | ✅ lokal | die 2 Interop-Stellen auf ClosedXML umgestellt |
| KI-Chat, Wiki-Hilfe | 🌐 online | wie heute auch auf Windows — „autonom" heißt ohne eigenen Server, nicht ohne Internet |
| Projektaustausch Windows ↔ iPad | 📦 Dateiaustausch | die SQLite-Datei bzw. der vorhandene Projekttransfer; **kein automatischer Abgleich** — zwei Geräte, zwei Stände. Ein Sync wäre ein eigenes Vorhaben |
| Drucken | ✅ AirPrint | über die erzeugten Berichte |

Die letzte Zeile der Tabelle ist die wichtigste Erwartungsklärung: Autonom heißt auch **getrennt**.
Wer auf dem iPad plant und am Schreibtisch weiterarbeiten will, transportiert eine Datei — wie
heute zwischen zwei Windows-Rechnern.

---

## 5 Etappen

| # | Inhalt | Ort | Nachweis |
|---|---|---|---|
| **S0** | **Machbarkeits-Spike:** Konverter-Rohfassung `.accdb` → SQLite; `BhkwPlan` + `SimulationControl` headless im iOS-Simulator gegen Projekt 1030 rechnen | Windows + Mac | Ergebnis-CSV **wertgleich** zur Referenzbasis — oder begründeter Abbruch des Vorhabens für kleines Geld |
| **S1** | **`EPOS.Kern` herauslösen:** Modelle, Rechenkern, Simulation, Wirtschaftlichkeit, `DbWerte`, Berichtslogik; `IDatenzugriff`-Schnittstelle; die 93 Nicht-UTF-8-Dateien dabei einmalig sauber konvertieren; `Program.*`-Statics kappen | **Windows** | Windows-App baut und rechnet **byte-gleich** (Referenzläufe) — reine Umbau-Etappe ohne Ergebniswirkung |
| **S2** | **Datenschicht:** SQLite-Implementierung, Konverter fertigstellen, die 20 Access-Abfragen nach iL3 extrahieren, Jet-Dialektstellen der 61 `RecordSet`-Dateien behandeln | Windows | Testprojekte auf SQLite rechnen wertgleich zu Access |
| **S3** | **UI-Gerüst iOS:** MAUI-Hülle, Navigation nach iL5, Projektliste, Wizard, Simulationsstart, Ergebnisansicht | Mac | Ein Projekt vollständig auf dem iPad durchgeplant |
| **S4** | **Berichte + Charts:** `ChartRenderer` auf SkiaSharp, Word/Excel-Ausgabe, Share-Sheet | Mac | Bericht vom iPad == Bericht von Windows (Zeilenvergleich) |
| **S5** | **Kataloge, Importe, Lizenz, Feinschliff**; TestFlight | Mac | Sichtabnahme, Feldtest |
| **S6** | App-Store-Einreichung | — | Review bestanden |

**S1 und S2 sind für die Windows-App eigenständig wertvoll** — sie lösen die doppelten
Schema-Wahrheiten, machen die Rechner erstmals testbar (offener Befund A1/E8) und entkoppeln vom
ACE-Provider. Selbst wenn nach S2 abgebrochen würde, wäre nichts davon verloren.

**Voraussetzungen:** Mac für Build und Signierung (oder Cloud-Build), Apple-Developer-Konto
(99 €/Jahr), Test-iPads. Ohne Mac geht ab S3 nichts.

---

## 6 Aufwandsehrlichkeit

Keine Schönrechnung: **S1–S2 sind Monate, S3–S5 sind viele Monate.** Der Gesamtrahmen ist ein
Mehrpersonen- oder Mehrjahresvorhaben neben dem laufenden Ausbau — die Wirtschaftlichkeit allein
hat in diesem Jahr über zwanzig Etappen gesehen, und jede davon müsste künftig in `EPOS.Kern`
landen statt in der Windows-App.

Der größte Einzelposten ist nicht die Technik, sondern die **Neugestaltung von 204 Masken für
Touch**. Realistisch braucht die iPad-App davon 40–60 als eigenständige Screens; der Rest sind
Admin- und Katalogpflegedialoge, deren Reihenfolge eine Scope-Frage ist (iF2).

Was das Risiko vorne klein hält, ist S0: **Für den Preis von wenigen Tagen liegt der Beweis auf dem
Tisch, dass der Rechenkern auf dem iPad wertgleich rechnet** — oder die belastbare Begründung,
warum nicht.

### 6.1 Wie weit sich der Formularteil automatisiert umwandeln lässt

**Ein fertiges Werkzeug gibt es nicht.** Der .NET Upgrade Assistant hebt Frameworks an, wandelt
aber ausdrücklich keine WinForms-Oberfläche in MAUI; kommerzielle Migratoren (Wisej, Mobilize)
zielen auf Web, nicht auf MAUI. Was es gibt, ist die Möglichkeit eines **eigenen Generators** —
und dessen Reichweite lässt sich am Bestand messen (Zählung 30.08.2026):

| Messgröße | Zahl | Bedeutung |
|---|---|---|
| Views mit `Designer.cs` (maschinenlesbare Struktur) | **118 von 161** (73 %) | strukturell vollständig extrahierbar |
| Views mit rein programmatischer UI | 43 | Handarbeit — darunter ausgerechnet die jüngsten Dialoge (`Form_WirtschaftlichkeitParameter`, `Form_Tarifstruktur`) |
| einfache Steuerelement-Instanzen (Label 1.545 · TextBox 730 · Button 497 · ComboBox 107 · NumericUpDown 57 · CheckBox 58 · Radio 45 · ListBox 50) | **≈ 3.100** | über 90 % der Feldmasse — der gut generierbare Teil |
| Formulare mit `DataGridView` | 16 | je Raster Handarbeit (Komponente einmal, Spalten je Maske) |
| Formulare mit `DataVisualization`-Charts | 16 | Neubau auf ScottPlot/SkiaSharp (iL6) |
| OwnerDraw-Zeichnung | 10 | Handarbeit |
| `SetControls`-Hausmuster (Füllen vor Anzeige, Rücklesen nach OK) | 74 Formulare | **regelmäßig genug für generierte Bindungs-Stubs** |
| `MessageBox.Show` / `DialogResult` / `ShowDialog` | 99 / 131 / 74 Dateien | mechanisch über Dienst-Shims ersetzbar |
| `BindingSource`-Datenbindung | 1 Formular | nichts zu konvertieren — der Datenfluss ist imperativ und einheitlich |

**Was ein Generator sicher leistet** (Roslyn über die `Designer.cs`, Ausgabe je Formular):

1. **Feldinventar** — Name, Typ, Beschriftung (Label-Zuordnung über das feste Raster
   Label x28/Control x270), Wertebereiche aus `NumericUpDown.Minimum/Maximum/DecimalPlaces`,
   ComboBox-Einträge, Tab-Reihenfolge, Ereignisliste, `resx`-Schlüssel beider Sprachen. Genau das
   hat die B5-Feldkarte bereits maschinell bewiesen.
2. **Komponenten-Skelette** — je `GroupBox` (83) eine Blazor-/XAML-Sektion, je `TabPage` (74) ein
   Navigationsziel, Felder mit Bindungs-Stubs im `SetControls`-Muster.
3. **`resx`-Übernahme 1:1** — MAUI liest dieselben Ressourcen, die Drei-Schichten-Regel bleibt
   unangetastet.

**Was kein Generator leistet:** das Layout. Die Designer tragen absolute Pixelkoordinaten; eine
1:1-Übertragung ergäbe eine Maus-UI auf einem Touch-Gerät und widerspräche iL5. Zielbild des
Generators sind deshalb **nicht fertige Seiten**, sondern Feldkarten plus Sektions-Skelette, die
von Hand zu Touch-Seiten komponiert werden. Ebenfalls Handarbeit: die 43 programmatischen Views,
die 16 Grid- und 16 Chart-Masken, die Kachel-Navigation der Startseite und als größtes Einzelstück
`Form_Simulation_Detail` (6.200 Zeilen, 11 Reiter).

**Nettoeinschätzung:** Automatisierbar sind rund **drei Viertel der Masken strukturell und über
90 % der Feldmasse** — das erspart etwa ein Drittel der S3/S5-Arbeit. Der größte Wert des
Generators ist aber nicht Tempo, sondern **Vollständigkeit**: Bei 730 Textfeldern ist das
vergessene Feld der typische Migrationsfehler, und ein generiertes Inventar mit Abgleichliste
schließt ihn aus.

---

## 6a Gemeinsame Weiterentwicklung: ein Kern, eine UI-Bibliothek, zwei Hüllen

*Ergänzt 30.08.2026 auf die Frage, wie beide Plattformen dauerhaft gemeinsam bedient werden und
der plattformspezifische Änderungsaufwand klein bleibt.*

### 6a.1 Drei Modelle im Vergleich

| Modell | Fachänderung landet in | UI-Änderung landet in | Bewertung |
|---|---|---|---|
| **A — geteilter Kern, zwei UIs** (iL1/iL4 in der Urfassung) | 1× `EPOS.Kern` | **2×** — WinForms und Blazor getrennt | Der UI-Doppelaufwand bleibt für immer. Bei über zwanzig Wirtschaftlichkeits-Etappen allein in 2026 ist das auf Dauer nicht tragbar |
| **B — MAUI überall** (auch Windows als WinUI-App) | 1× | 1× | verlangt die **Big-Bang-Ablösung** der gewachsenen Windows-App — unvereinbar mit der laufenden Entwicklung |
| **C — Blazor-Komponenten überall, Strangler-Muster** | 1× | **1×** — dieselbe Komponente | **Empfehlung.** Möglich durch `BlazorWebView`: WinForms kann Blazor-Komponenten **einbetten** (`Microsoft.AspNetCore.Components.WebView.WindowsForms`, WebView2). Die bestehende App bleibt die Hülle und schrumpft schrittweise |

### 6a.2 Zielarchitektur (Modell C)

```
EPOS.Kern        Modelle · Rechenkern · Simulation · Wirtschaftlichkeit · DbWerte
                 IDatenzugriff · Dienstschnittstellen (Dialog, Datei, Lizenz, …)
       ▲                              ▲
EPOS.UI          EINE Blazor-Komponentenbibliothek: Dialoge, Raster, Charts,
                 Herleitungszeilen — adaptiv für Maus UND Touch entworfen
       ▲                              ▲
Windows-Hülle                    iOS-Hülle
bestehende WinForms-App          MAUI-App (BlazorWebView)
bettet je Dialog eine            Navigation nach iL5
BlazorWebView ein;               
Alt-Dialoge laufen weiter        
```

**So bleibt der plattformspezifische Aufwand dauerhaft klein:** Eine neue Fachfunktion ist danach
eine Kern-Änderung plus **eine** Blazor-Komponente. Plattformspezifisch bleiben nur die Adapter —
eine einstellige Zahl kleiner Schnittstellen:

| Adapter | Windows | iOS |
|---|---|---|
| Datenzugriff | ACE (Übergang) → SQLite | SQLite |
| Ablage Lizenz/Schlüssel | DPAPI | Keychain |
| Geräte-Identität | `GeraeteId` heute | `identifierForVendor` |
| Dateiwahl/Export | Dateidialog, Explorer | Document-Picker, Share-Sheet |
| Drucken | Windows-Druck | AirPrint |
| Einstellungen | Registry → `Preferences` | `Preferences` |

**Der Weg dorthin ist kein Umbauprojekt, sondern eine Arbeitsregel** (Strangler-Muster): Ab einem
Stichtag wird jeder **neue** Dialog und jeder ohnehin **anzufassende** Dialog als Blazor-Komponente
gebaut und in die WinForms-App eingebettet — nie mehr doppelt. Die Windows-Anwender profitieren
sofort, die iOS-App erntet später dieselben Komponenten. Alt-Dialoge, die niemand anfasst, bleiben
unverändert WinForms, bis ihre Reihe kommt.

### 6a.3 Was bei der Migration in diesem Modell zu beachten ist

| # | Regel | Warum |
|---|---|---|
| **M1** | **Kein Dialog existiert doppelt.** Wird eine Maske nach Blazor gebaut, wird die WinForms-Fassung im selben Schritt stillgelegt | zwei lebende Fassungen derselben Maske sind die Doppelpflege, die das Modell gerade abschaffen soll |
| **M2** | **Komponenten von Anfang an adaptiv** entwerfen — ein Layoutsystem mit Breakpoints, Bedienbarkeit mit Maus **und** Finger je Komponente abgenommen | sonst entsteht die zweite UI durch die Hintertür („Desktop-Variante" und „Touch-Variante" derselben Komponente) |
| **M3** | **Datenhaltung mittelfristig auf beiden Plattformen SQLite.** Übergangsweise trägt `IDatenzugriff` zwei Dialekte — dann muss aber **jeder Migrationsschritt doppelt** geschrieben und geprüft werden (Jet-SQL ≠ SQLite). Ein Stichtag für den Windows-Umstieg gehört ins Konzept | die doppelte Schemapflege wäre der teuerste Dauerposten des ganzen Modells |
| **M3a** | Der Windows-Umstieg auf SQLite kostet den **Access-Direktzugriff**: keine gespeicherten Abfragen mehr in der Datei, kein „Komprimieren und reparieren", keine Sichtprüfung in Access. Diese Arbeitsweise ist heute Teil des Betriebs (Anhang-B-Checklisten) und braucht Ersatz (SQLite-Browser, eingebaute Wartungsfunktionen) | ehrliche Betriebsfolge — sonst wird der Stichtag am Widerstand der Praxis scheitern |
| **M4** | **`ShowDialog`/`DialogResult`/`MessageBox` einmalig durch Dienste ersetzen** (74/131/99 Fundstellen) und die `Program.*`-Statics kappen | Blazor kennt keine modalen Fensterketten; die Dienste sind zugleich die Naht, an der die Hüllen sich unterscheiden dürfen |
| **M5** | **Ein Chart-Stack**: ScottPlot 5/SkiaSharp inklusive dessen Blazor-Anbindung; der Berichts-`ChartRenderer` zuerst (ohne UI testbar), die 39 `DataVisualization`-Stellen folgen mit ihren Masken | drei Chart-Techniken auf zwei Plattformen wären sechs Pflegefälle |
| **M6** | **Raster-Standard vor der ersten Tabelle festlegen** (Kandidat: QuickGrid) — 16 Grid-Masken warten darauf | nachträglicher Rasterwechsel hieße 16 Masken zweimal bauen |
| **M7** | **Drei-Schichten-Regel unverändert**: `MyResource.Resource.*` ist eine normale Klasse und läuft in Blazor auf beiden Plattformen; `DbWerte` bleibt eingefroren | die Lokalisierungsinvestition überlebt die Migration vollständig |
| **M8** | **Referenzläufe je Plattform als Pflicht**: Kern-Wertgleichheit Windows ↔ iOS ist die Definition von „fertig", Mac-Build in der CI | ohne den Beweis je Plattform driftet der Kern unbemerkt |
| **M9** | **Übergangszeit benennen**: zwei Optiken in einer App (WinForms-Altdialoge neben Blazor-Seiten) sind gewollt und enden erst mit der letzten Maske; Windows braucht die WebView2-Laufzeit (auf Windows 11 vorhanden) | wer die Mischphase nicht ausdrücklich beschließt, bricht sie beim ersten Optik-Einwand ab |
| **M10** | Die 93 cp1252-Dateien beim Umzug nach `EPOS.Kern`/`EPOS.UI` **einmalig** auf UTF-8 normalisieren | die Kodierungsfalle darf nicht in die neuen Projekte wandern |

### 6a.4 Das komplette Arbeitsprogramm des Vollausbaus

*Ergänzt 30.08.2026 auf die Frage, was insgesamt zu tun ist, bis **alles** auf Modell C steht.
Fünf Blöcke; die Mengen sind gemessen (§ 2 und Zählung 30.08.). Grobe Verteilung des Aufwands:
Fundament ~15 % · Datenschicht ~15 % · Masken ~50 % · Module ~10 % · Absicherung ~10 %.*

**Block A — Fundament (einmalig, vor der ersten Komponente)**

| # | Arbeit | Menge | „Fertig" heißt |
|---|---|---|---|
| A1 | Projektstruktur: `EPOS.Kern` + `EPOS.UI` (Razor-Klassenbibliothek) + zwei Hüllen in `WP-Plan.sln`; beim Dateiumzug die cp1252-Dateien einmalig auf UTF-8 | 93 Dateien Kodierung | Solution baut, Windows-App unverändert lauffähig |
| A2 | **`Program.*`-Statics kappen** (`mdifrm`, `mainfrm`, `wizardctrl`, `HelpCatalog`, Pfade, Sprache) → Dienste `INavigation`, `IProjektKontext`, `ISprache`, `IPfade` | **40 Dateien** hängen daran | kein View-fremder Code greift mehr auf `Program.*` |
| A3 | Dienstschnittstellen des Irreduziblen: `IDialogDienst` (ersetzt `ShowDialog`/`DialogResult`/`MessageBox`), `IDateiDienst`, `ILizenzAblage`, `IGeraeteId`, `IEinstellungen`, `IDrucken`/`ITeilen` | 74 / 131 / 99 Fundstellen · Registry 3 · DPAPI 2 | je Hülle eine Implementierung, Aufrufer plattformfrei |
| A4 | Zahlen-/Validierungsdienst: `Program.ZahlParsen/ZahlPruefen/ZahlFaerben` als Blazor-Eingabekomponenten (komma-/punkttolerant, Färben als Komponentenzustand) | **44 Dateien** nutzen `Program.Zahl*` | eine Eingabekomponente je Zahlentyp, de/en |
| A5 | Haus-Bausteinsatz in `EPOS.UI`: SpeichernLeiste, InfoKnopf (an `help_mapping`), Kachel, EinstiegsKarte, Gruppenkopf, Herleitungszeile, Kohärenzzeile, Warnbanner + Farb-/Typografie-Thema | ~10–12 Bausteine | jede spätere Maske komponiert nur noch |
| A6 | Standards festlegen: Raster (QuickGrid-Wrapper), Charts (ScottPlot 5 Blazor), Datums-/Auswahlfelder | einmalig | **vor** der ersten Grid-/Chart-Maske (M6) |
| A7 | Formular-Generator (§ 6.1): `Designer.cs` → Feldkarte + Razor-Skelett | 118 Quell-Designer | Inventar = Abnahmecheckliste je Maske |

**Block B — Datenschicht**

| # | Arbeit | Menge | „Fertig" heißt |
|---|---|---|---|
| B1 | `IDatenzugriff` + SQLite-Provider. **`DataRepository` behält seine öffentliche Oberfläche** und wird zur Fassade — die 179 Aufruferdateien bleiben dadurch unangetastet; `RecordSet` (61 Dateien) ebenso fassadiert oder abgelöst | 179 + 61 Dateien | beide Provider hinter derselben API |
| B2 | Jet-Dialekt-Audit: `IIf`/`Nz`/`TOP`/`#Datum#`-Stellen finden und dialektfest machen | **17 Dateien** mit Jet-Eigenheiten (Erstzählung) | SQL läuft auf ACE **und** SQLite |
| B3 | Die 20 gespeicherten Access-Abfragen extrahieren (iL3), mit Einzelnachweis | 20 Abfragen | `.accdb` ist reine Datendatei |
| B4 | Konverter `.accdb` → `.sqlite` (Kataloge + Projekte, 145 MB); `SchemaMigration` dialektfähig **oder** Stichtag nach iF9; dabei stirbt die Selbst-DDL in `WirtschaftlichkeitCtrl` (bekannte Doppelwahrheit) | 61 Schritte + Selbst-DDL | Testprojekte rechnen auf SQLite wertgleich |
| B5 | Betriebsersatz: Backup, „Komprimieren" (VACUUM), Sichtwerkzeug statt Access (M3a); `%APPDATA%`-Caches über `IPfade` | 12 Dateien Pfad-Fundstellen | Anhang-B-Arbeitsweise hat ein Gegenstück |

**Block C — Masken-Umwandlung (die Masse: 161 View-Einheiten)**

| Klasse | Menge | Weg |
|---|---|---|
| K1 Formularmasse (Designer, einfach) | **~90 Masken** | Generator-Skelett + Hand-Layout nach iL5; Feldkarte als Abnahme |
| K2 Grid-Masken (`DataGridView`) | 16 | QuickGrid-Wrapper aus A6, Spalten je Maske von Hand |
| K3 Chart-Masken (`DataVisualization`, 39 Dateien) | 16 | ScottPlot-Komponenten; Serien-Schlüssel bleiben (Drei-Schichten-Regel) |
| K4 Programmatische Views | 43 | ohne Generator, dafür liegen für die jüngsten bereits maschinelle Feldkarten vor (B5) |
| K5 Sonderstücke | 6–8 | `Form_Start` (Kacheln → Startnavigation), `MDIMainForm` (Menü), `WizardParent` (Seitenfolge → Nav-Stack), `TabNavigationManager`/Navigatoren, Dashboard — und als größtes Einzelstück **`Form_Simulation_Detail`** (6.200 Zeilen, 11 Reiter): wird nicht konvertiert, sondern in Komponenten **zerlegt** |
| K6 Nicht konvertieren — stilllegen | ~10–15 | die dokumentierten toten Enden (`FormMain`-Altzweig, `Form_Wirtschaftlichkeit`-Hülle, `Form_AlsVariante`, „- Kopie"-Dateien …): die Umwandlung ist der Moment, sie zu begraben statt mitzuschleppen |

Reihenfolge nach Anfasswahrscheinlichkeit (Strangler): zuerst die aktiven Baustellen
(Wirtschaftlichkeit, Kosten), zuletzt ruhende Admin-Kataloge.

**Block D — Module**

| # | Arbeit | Menge |
|---|---|---|
| D1 | `ChartRenderer` GDI+ → SkiaSharp — **erste** Chart-Arbeit, weil ohne UI testbar (Berichtsbilder vergleichbar) | 1 Klasse |
| D2 | Excel-COM-Import → ClosedXML; Berichts-Export über `IDateiDienst`/Share-Sheet | 2 Dateien |
| D3 | KI/Hilfe: REST bleibt; DPAPI-Schlüssel → `ILizenzAblage`; Wiki-Caches → `IPfade`; InfoKnopf-Komponente aus A5 | 3–4 Stellen |
| D4 | Lizenz: Ablage je Plattform (DPAPI/Keychain), `GeraeteId` je Plattform | 2 Dateien + Neuimpl. iOS |
| D5 | Importe VDI 3805 / CEC / PAN / CSV: Logik unverändert, Dateiwahl über Dienst | Aufrufstellen |

**Block E — Absicherung**

| # | Arbeit |
|---|---|
| E1 | Referenzläufe als CI **beider** Plattformen; der vorhandene Wirtschaftlichkeits-Treiber (`run-wirtschaftlichkeit`-Skill) ist das Muster für den Kern-Runner |
| E2 | Abnahme je Maske: Generator-Inventar = Feldcheckliste, beide Sprachen, Maus **und** Touch (M2) |
| E3 | Mischphasen-Betrieb (M9): WebView2-Voraussetzung, Installer, Doku der zwei Optiken |

**Was ausdrücklich nicht angefasst wird:** Rechenkern und Rechner (nur Umzug nach `EPOS.Kern`),
`DbWerte`, sämtliche `resx`-Inhalte, das Referenzlauf-Werkzeug. Die Fachlichkeit wandert, sie
ändert sich nicht — jede Etappe beweist das per Wertgleichheit.

### 6a.5 Folgen für die Etappen

S1/S2 bleiben unverändert (Kern und Datenschicht). **S3 ändert die Richtung:** Statt „UI-Gerüst
auf dem Mac" entsteht zuerst `EPOS.UI` samt `BlazorWebView`-Einbettung **in der bestehenden
Windows-App** — der erste Blazor-Dialog läuft also unter Windows im Produktivbetrieb, lange bevor
es eine iOS-Hülle gibt. Die iOS-Hülle (bisheriges S3) wird zur eigenen, kleineren Etappe danach.
Damit verschiebt sich der iPad-Ersttermin nach hinten — dafür trägt jede UI-Investition ab dem
Stichtag doppelt.

## 7 Entscheidungsfragen an Philipp

| Nr. | Frage | Empfehlung |
|---|---|---|
| **iF1** | S0-Spike beauftragen? (Konverter-Rohfassung + Kernrechnung im Simulator gegen Projekt 1030) | **ja** — kleinster Einsatz, größter Erkenntnisgewinn |
| **iF2** | Voller Funktionsumfang von Anfang an — oder erste Auslieferung mit pflegbaren Projekten, aber **Katalogpflege/Herstellerimporte zunächst am Windows-Platz**? | erste Auslieferung ohne Katalog-Admin; „autonom rechnen" ja, „autonom Kataloge importieren" als Ausbaustufe |
| **iF3** | UI-Technologie: Blazor Hybrid (iL4) oder MAUI-XAML? | Blazor Hybrid — Begründung in iL4; Nebeneffekt Browser-Fassung |
| **iF4** | Soll S1/S2 (Kern-Herauslösung, SQLite-Fähigkeit) **unabhängig vom iOS-Ziel** eingeplant werden, als Gesundung der Windows-App? | ja — es adressiert vier dokumentierte Altlasten (A1-Tests, doppelte DDL, Abfragen in der `.accdb`, ACE-Bindung) |
| **iF5** | Vertriebsweg: App Store (Review, Provisionsregeln beim Lizenzverkauf) oder nur TestFlight/Unternehmensverteilung für bekannte Kunden? | zunächst TestFlight; App-Store-Frage erst mit S5 |
| **iF6** | Windows-Charts mittelfristig ebenfalls auf ScottPlot 5 (ein Stack), oder `DataVisualization` dort belassen? | mittelfristig vereinheitlichen, aber **nicht** als Teil dieses Vorhabens |
| **iF7** | Formular-Generator (§ 6.1) als eigenes Werkzeug in S3 einplanen — Feldinventar + Sektions-Skelette aus den 118 `Designer.cs`? | **ja** — geringe Kosten, und das Inventar ist zugleich die Abnahmecheckliste gegen vergessene Felder |
| **iF8** | **Modell C beschließen** (§ 6a): ab Stichtag jeder neue bzw. angefasste Dialog als Blazor-Komponente, eingebettet in die bestehende Windows-App — Strangler-Regel M1? | **ja** — es ist der einzige der drei Wege, der den UI-Doppelaufwand dauerhaft beseitigt, ohne die Windows-App abzulösen |
| **iF9** | **SQLite auch auf Windows** mit Stichtag (M3) — und damit Verzicht auf den Access-Direktzugriff (M3a)? | ja, aber erst nach S2 und mit Ersatz für die Access-Arbeitsweise; bis dahin zwei Dialekte bewusst in Kauf nehmen |

---

## 8 Was dieses Konzept ausdrücklich nicht ist

- **Kein Ersatz für die Windows-App.** Die bleibt das Hauptwerkzeug; das iPad kommt dazu.
- **Kein Cloud-/Sync-Konzept.** Autonom heißt getrennt (§ 4); ein Abgleich zwischen Geräten wäre
  ein eigenes Konzept mit eigener Datenmodell-Diskussion.
- **Keine Zusage zu Terminen.** Vor S0 gibt es keine belastbare Aufwandszahl — genau deshalb steht
  S0 vorn.
