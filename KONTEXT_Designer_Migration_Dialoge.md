# Bestandsaufnahme: Dialoge ohne WinForms-Designer und Bewertung der Migration

Stand: 19.08.2026. Untersucht wurde der gesamte Bestand unter `WindowsFormsApplication1`
(ohne die Altkopien und Worktrees). Ziel: Welche Dialoge wurden ohne den Designer gebaut,
und für welche lohnt eine Umstellung auf das Designer-Muster
(`partial class` + `X.Designer.cs` + `X.resx`, `InitializeComponent` designer-generiert)?

## 1. Gesamtbild

139 Form-/UserControl-Klassen im Projekt, davon **90 mit Designer-Datei** und **49 ohne**.
Die 49 zerfallen in:

| Gruppe | Anzahl | Ergebnis |
|---|---|---|
| Kategorie **A** — gut migrierbar (statisches Layout, Migration mechanisch) | 19 | migrieren, Reihenfolge s. Abschnitt 7 |
| Kategorie **B** — teilmigrierbar (statischer Rahmen designerfähig, Substanz bleibt Code) | 12 | nur bei konkretem Anlass |
| Kategorie **C** — Migration nicht sinnvoll (Hüllen, Owner-Draw, toter Code) | 7 | nicht migrieren |
| Sonderfall `Controller\*KontextMenuCtrl` — erben grundlos von `Form` | 10 | **keine** Designer-Migration; `: Form` streichen |
| `Allgemein\BaseForm.cs` — Infrastruktur-Basisklasse | 1 | bleibt |

Dazu kommen **7 Inline-Wegwerf-Dialoge** (`new Form()` im Methodenrumpf) in `Form_KiChat`
und `Form_Simulation_Config` — Bewertung in Abschnitt 6.

**Randbedingungen, die die Migration leicht machen:**
- Alle 49 Kandidaten-Dateien sind gültiges UTF-8 — der CLAUDE.md-Fallstrick „93 Dateien
  nicht UTF-8" trifft **keinen** Kandidaten. (Restpunkt: einige sind UTF-8 *ohne* BOM;
  Visual Studio schreibt beim Speichern BOM zurück → 3-Byte-Diff auf Zeile 1.)
- Die `.csproj` ist SDK-Style mit Standard-Globbing: neue `X.Designer.cs`/`X.resx`
  binden sich ohne Projektdatei-Eingriff ein.
- Die Lokalisierung ist bei der Mehrheit der Kandidaten bereits sauber über
  `MyResource.Resource.*` — genau das diktiert aber die wichtigste Migrationsregel (s. u.).

## 2. Projektweite Migrationsregeln (das Rezept)

Diese Regeln gelten für **jede** einzelne Migration; sie stammen aus gemessenen Befunden
im Bestand, nicht aus allgemeiner Vorsicht.

1. **`Localizable = false` halten, Texte per Code setzen.** Rund 60 der 90 Bestands-
   Designer-Forms sind resx-lokalisiert (`ApplyResources`) — das ist das *alte* Muster.
   Für Neues gilt die Drei-Schichten-Regel: Der Designer bekommt neutrale Platzhalter
   (z. B. den Feldnamen — ein vergessenes Nachsetzen fällt dann sofort auf), die echten
   Texte setzt eine `TexteSetzen()`-Methode **nach** `InitializeComponent` aus
   `MyResource.Resource.*`. So bleiben `.resx` der Form praktisch leer und harmlos.
2. **AutoScale ist das größte Einzelrisiko.** Die Kandidaten setzen höchstens
   `AutoScaleMode = Font` **ohne** `AutoScaleDimensions` — Skalierfaktor bleibt (1,1),
   es wird faktisch nie skaliert. Der Designer schreibt beim ersten Speichern
   `AutoScaleDimensions` dazu und **schaltet die Skalierung erstmals scharf**.
   Gemessener Präzedenzfall `Form_Simulation_Config`: Entwurf 7;17 gegen Laufzeit 7;15
   → Höhe 502 statt 552, verkleinerte Schriften. Pflicht nach jedem Designer-Speichern:
   `AutoScaleDimensions` exakt auf das Laufzeitmaß prüfen (7F;15F bei Segoe UI 9, 96 dpi)
   oder `AutoScaleMode = None` erzwingen. Die App ist faktisch DpiUnaware.
3. **Parameterloser Konstruktor nötig.** Der VS-Designer öffnet die Entwurfsfläche sonst
   nicht. Präzedenz im Projekt: `Form_Variantentest.cs:36` — `public Form_Variantentest() : this(-1) { }`.
4. **`Dispose`-Kollision prüfen.** Eine generierte `.Designer.cs` bringt ihr eigenes
   `protected override void Dispose(bool)` mit. `Form_SpeicherVariantenVergleich.cs:283`
   hat bereits ein eigenes (gibt `m_SchriftAktiv` frei) → CS0111; die Font-Freigabe muss
   in die Designer-Fassung umziehen. Vor jeder Migration auf eigenes `Dispose` greppen.
5. **Lambdas raus aus dem UI-Aufbau.** Der Designer-Parser bricht bei Lambdas in
   `InitializeComponent` ab („Der Designer kann den Code nicht verarbeiten").
   Closure-freie Lambdas mechanisch zu Methodenreferenzen machen; echte Closures
   (z. B. `Form_Betriebskosten`: pro Zeile ein `Steuerung`-Objekt) sind ein Grund,
   den betroffenen Teil im Code zu belassen.
6. **Lokale Control-Variablen werden Felder.** Viele Kandidaten halten Labels nur als
   lokale Variablen — der Designer braucht Felder.
7. **Persistenz-/Steuerwerte niemals im Designer-Code lassen.** Der Serializer schreibt
   sie beim nächsten Speichern als Literale bzw. (bei `Localizable=true`) in die resx,
   wo sie „übersetzt" würden. Konkreter Fund: `UcWirtschaftlichkeit.InitializeComponent`
   füllt `cbSzenario` mit `WirtschaftlichkeitSzenario.ERWARTET/BEST/WORST` — das sind
   DB-Persistenzwerte (`Tab_ErgebnisWirtschaftlichkeit.Szenario`). Solche Zeilen vor der
   Migration hinter `InitializeComponent` verschieben.
8. **Charts und ScottPlot nicht auf die Entwurfsfläche.** `Chart`-Serialisierung
   (portiertes `WinForms.DataVisualization`) ist unter VS 2022/.NET 8 unzuverlässig;
   `ScottPlot.FormsPlot` (SkiaSharp) wird zur Entwurfszeit instanziiert — Ladefehler-
   Risiko, zumal x86. Muster von `Form_PeakShaving` übernehmen: leere Chart-Hülle bzw.
   Plots per Code hinter `InitializeComponent` einhängen.
9. **AutoScroll-Masken: d49075e-Verdeckungsmuster nicht nachbauen.** Wer bedingten
   Aufbau durch „alles designen + `Visible=false`" ersetzt, erzeugt genau die bekannte
   Konstellation (handle-loses direktes Kind eines scrollenden Containers mit
   Default-Anker). Betroffen wären: `Form_WirtschaftlichkeitParameter`,
   `Form_Tarifstruktur` (warnt im Klassenkommentar selbst davor und schaltet `Enabled`
   statt `Visible`), `Form_KwkgModule`, `Form_Betriebskosten` (`_liste`-Panel).
10. **Kommentarverlust einpreisen.** Designer-Code kennt keine Kommentare. Mehrere
    Kandidaten dokumentieren Pixelentscheidungen aus Abnahmebefunden im Aufbaucode
    (`Form_QuelleErdreich`, `Form_Waermesenke` I-K1-1, `Form_PufferSp_Projekt`,
    beide Karten). Bei diesen Forms den Begründungstext in die verbleibende Code-Datei
    retten (z. B. über `TexteSetzen()`/Nachpositionierungs-Methode).

**Alternative Zwischenform (Grundsatzentscheidung vor Beginn):** Das Projekt enthält mit
`Form_Variantentest.Designer.cs` bereits eine **von Hand geschriebene** designer-förmige
Partialdatei (ohne `AutoScaleDimensions`, ohne `#region`). Reines Aufteilen in
`X.cs` + `X.Designer.cs` ohne den Designer je zu öffnen vermeidet die Risiken 2 und 7
fast vollständig, bringt aber nur Dateihygiene — keine WYSIWYG-Pflege. Wenn das Ziel
„im Designer pflegbar" ist, führt an Regeln 1–10 kein Weg vorbei.

## 3. Kategorie A — gut migrierbar (19)

Aufwand: S < 2 h, M = 2–8 h je Form (rein mechanisch; Lokalisierungs-Nacharbeit separat vermerkt).

| Datei | Klasse | Aufwand | Kernrisiko / Vorarbeit |
|---|---|---|---|
| `Views\Stromspeicher\Form_SpeicherVariantenVergleich.cs` | `Form_SpeicherVariantenVergleich` | **S** | eigenes `Dispose(bool)` → CS0111 (Regel 4); sonst 9 Controls, voll verankert — **Pilot** |
| `Views\Kosten\Form_SpotpreisImport.cs` | `Form_SpotpreisImport` | **S** | keins von Gewicht: statisch, lambda-frei, voll lokalisiert — **Pilot** |
| `Views\Kosten\Form_PlanwertUebernahme.cs` | `Form_PlanwertUebernahme` | **S** | Panels/Grid-Spalten nur lokal; `fuss.Resize`-Workaround entfällt durch echte Anker — **Pilot** |
| `Views\BerichteKosten\Form_BkUebernahme.cs` | `Form_BkUebernahme` | S | konstruktorabhängige `Visible`/`ClientSize`-Zweige hinter `InitializeComponent`; TLP-RowStyles im Editor fragil |
| `Views\Stromverbraucher\Form_GanglinieProtokoll.cs` | `Form_GanglinieProtokoll` | S | parameterabhängige Texte/Zustände trennen; zweite Klasse (`GanglinienProtokollText`) in eigene Datei |
| `Views\Simulation\Form_QuellePufferspeicher.cs` | `Form_QuellePufferspeicher` | S | gemessene Eingabespalte (`Math.Max(l1.Right,…)`) bleibt Code; zwei deckungsgleiche Container (`Visible`-Tausch) |
| `Views\Wirtschaftlichkeit\Form_WirtschaftlichkeitVerlauf.cs` | `Form_WirtschaftlichkeitVerlauf` | S | 4 Lambdas → Methoden; DB-Aufruf/`Screen`-Deckelung/`FensterEinpassung` aus dem Aufbau lösen; 7 harte Texte |
| `Views\Admin\Form_Gesetzesparameter.cs` | `Form_GesetzparameterZeile` | S | 707-Zeilen-Datei muss in zwei Dateisätze zerlegt werden; `ReadOnly`/`Enabled` parameterabhängig |
| `Views\Admin\Form_Gesetzesparameter.cs` | `Form_Gesetzesparameter` | S–M | ListView-Spaltentexte nach `InitializeComponent`; `Name`-Werte sind Testanker des Reflection-Harness (W4-E1) |
| `Views\Admin\Form_LizenzVerwaltung.cs` | `Form_LizenzVerwaltung` | S (+Lok.) | strukturell ideal; ~28 Texte müssen erst als Resource-Schlüssel angelegt werden; `this.Font`-Zeile gegen AutoScale |
| `Views\Varianten\Form_AlsVariante.cs` | `Form_AlsVariante` | S (+Lok.) | **Klasse ist aktuell unerreichbar** (kein Aufrufer von `Zeige`) — vor dem Umbau klären, ob sie bleibt |
| `Views\Pufferspeicher\Form_PufferSp_Projekt.cs` | `Form_PufferSp_Projekt` | M | drei Aufrufer → größte Regressionsfläche; keine Lambdas, kein Chart — mechanisch |
| `Views\Simulation\Form_QuelleErdreich.cs` | `Form_QuelleErdreich` | M | `Chart` (2 Series) → Regel 8; 11 Labels nur lokal; ~60 Kommentarzeilen retten |
| `Views\Stromspeicher\Form_PeakShaving.cs` | `Form_PeakShaving` | M | ListView-Spalten + Chart-Hülle nach `InitializeComponent`; `Anchor`-Layout beim Nachbau exakt treffen |
| `Views\Wirtschaftlichkeit\Form_KwkgModule.cs` | `Form_KwkgModule` | M | ListBox-Füllung + `MeasureText`-Höhe aus dem Aufbau; `ClientSize` hängt am aufgelaufenen `y`; AutoScroll (Regel 9) |
| `Views\Wirtschaftlichkeit\UcWirtschaftlichkeit.cs` | `UcWirtschaftlichkeit` | M | Lambda in `InitializeComponent` (Parser); **Szenario-Persistenzwerte raus** (Regel 7); 12 harte Texte |
| `Views\Kosten\ucStromAufschlaege.cs` | `ucStromAufschlaege` | M | Closure über `wert` in `SchnellwahlKnopf`; öffentliche `BREITE`/`HOEHE` steuern Layout des Wirts `ucFuelSettings` |
| `Views\Bericht\UcBericht.cs` | `UcBericht` | M | 13 harte deutsche Texte zuerst nach `MyResource` (+ Katalog-Doku); 2 Lambdas; `InitializeComponent` schon designer-nah |
| `Views\Stromverbraucher\Form_GanglinieImportOptionen.cs` | `Form_GanglinieImportOptionen` | M | Fabrikmethoden nicht serialisierbar → Layout im Designer neu auslegen (24 Controls, 7 neue Felder) |

## 4. Kategorie B — teilmigrierbar, nur bei Anlass (12)

Gemeinsames Muster: Der statische Rahmen wäre designerfähig, aber die Substanz
(Rasterzeilen, datengetriebene Gruppen, gemessene Layouts) bliebe ohnehin Code —
der Gewinn ist klein, die Risiken (AutoScroll-Muster, Closures, Kulturlabels) real.

| Datei | Klasse | Aufwand | Warum nur teilweise |
|---|---|---|---|
| `Views\Simulation\Form_Quellprofil.cs` | `Form_Quellprofil` | M | 84 der ~100 Controls aus festen 12er/24er-Schleifen in `_tbMonat[]`/`_tbStunde[]` — Control-Arrays kennt der Designer nicht |
| `Views\Simulation\Form_Waermesenke.cs` | `Form_Waermesenke` | M | `TextRenderer.MeasureText` bestimmt Trenner/Knöpfe/`ClientSize` — feste Designer-Werte holen Befund I-K1-1 zurück |
| `Views\Stromspeicher\Form_SpeicherOptimierung.cs` | `Form_SpeicherOptimierung` | M | 2× `ScottPlot.FormsPlot` bleiben Code (Regel 8); Rest wäre machbar |
| `Views\Kosten\Form_Betriebskosten.cs` | `Form_Betriebskosten` | M | ~84 der ~98 Controls sind datengetriebene Zeilen mit echtem Closure-State (`Steuerung` je Zeile); scrollendes `_liste`-Panel |
| `Views\Kosten\Form_Kostenprofil.cs` | `Form_Kostenprofil` | L | 12er/24er-Raster; Monats-/Wochentagslabels aus `CurrentUICulture` (Designer fröre Deutsch ein); Chart |
| `Views\Wirtschaftlichkeit\Form_WirtschaftlichkeitParameter.cs` | `Form_WirtschaftlichkeitParameter` | L | Gruppen entstehen bedingt je `_erzeuger`; DB-Zugriff in `InitializeComponent`; AutoScroll + `Visible`-Nachbau = d49075e |
| `Views\Wirtschaftlichkeit\Form_Tarifstruktur.cs` | `Form_Tarifstruktur` | L | ~105 absolute Positionen; laufzeitberechnete GroupBox-Höhen; Klassenkommentar warnt selbst vor dem Verdeckungsmuster |
| `Views\BerichteKosten\UcBkKosten.cs` | `UcBkKosten` | M | Grid-Spalten laufzeiterzeugt; private nested `Kachel`-Klasse müsste erst public + parameterlos werden |
| `Views\BerichteKosten\UcBkUebersicht.cs` | `UcBkUebersicht` | M | Grid + Zellentausch bleibt Code; Prozentraster im TLP-Editor fragil; Prüfhilfen hängen am Feldnamen `gridKomp` |
| `Views\Help\Form_KiChat.cs` | `Form_KiChat` | M | Rahmen rein Dock/Flow (designerfähig), aber ~30 harte Literale zuerst nach `MyResource`; Z-Order „Fill zuerst" kritisch |
| `Views\Help\Form_KiHinweis.cs` | `Form_KiHinweis` | S | technisch problemlos, aber bereits sauber + voll lokalisiert — **Nutzen gering** |
| `Views\Help\Form_Lizenz.cs` | `Form_Lizenz` | L | ~60 Texte + ~15 Absätze Rechtsprosa hart im Code (Z. 359–463) — **erst Textauslagerung**, dann ist der Rest Kategorie A. Nebenbefund: `ZustimmungSicherstellen()` wird nirgends aufgerufen (toter Erststart-Pfad) |

## 5. Kategorie C — nicht migrieren (7)

| Datei | Klasse | Grund |
|---|---|---|
| `Views\Wirtschaftlichkeit\Form_Wirtschaftlichkeit.cs` | `Form_Wirtschaftlichkeit` | Hülle um 1 gedocktes Kind; Kind hat keinen parameterlosen Ctor → Entwurfsfläche bliebe leer |
| `Views\Bericht\Form_Bericht.cs` | `Form_Bericht` | dito (Hülle um `UcBericht`) |
| `Views\BerichteKosten\UcBerichteKosten.cs` | `UcBerichteKosten` | nur 3 Controls; OwnerDraw/GDI+/Reflection (`DoubleBuffered`) ginge beim Designer-Speichern verloren |
| `Views\Simulation\ErzeugerKarte.cs` | `ErzeugerKarte` | datengetrieben, `Neuordnen()` ersetzt Layoutmanager, `OnPaint`/`SetStyle(UserPaint)` — Ertrag null |
| `Views\Simulation\SpeicherKarte.cs` | `SpeicherKarte` (+ `SchwellenBand`) | dito; `SchwellenBand` verträgt kein transparentes `BackColor` (dokumentierte Ausnahme) |
| `Allgemein\Form3Src.cs` | `Form3Src` | **toter Code** (keine Aufrufer, Inhalt: 1 Testfeld) — Löschkandidat, wird noch mitkompiliert |
| `Allgemein\GrafikTools\Form_ChartZoom.cs` | `Form_ChartZoom` | **toter Code** (keine Aufrufer); daneben verwaiste, leere `Form_ChartZoom.resx` |

## 6. Sonderfälle

### 6.1 `Controller\*KontextMenuCtrl` (10 Klassen): `: Form` streichen statt migrieren

Alle zehn erben von `Form`, benutzen davon aber **nachweislich nichts**: kein `this`
(weder als `IWin32Window` noch für `Invoke`), kein `Show`/`ShowDialog`, kein `Dispose`,
kein `InitializeComponent`. Sie sind unsichtbare Halter für einen `ContextMenuStrip`,
der an ein fremdes `ListView` gehängt wird. Der Beweis liegt im selben Ordner:
`ProzesswaermeKontextMenuCtrl` und `StrombedarfKontextMenuCtrl` sind funktional identisch
und erben **nicht** von `Form`.

**Empfehlung:** in 10 Dateien `: Form` ersatzlos streichen (~15 Min., kein
Verhaltensrisiko), die 4 byte-identischen, leeren Designer-Abfall-resx löschen
(`GebäudeKontextMenuCtrl.resx`, `HeizkesselKontextMenuCtrl.resx`,
`StromganglinieKontextMenuCtrl.resx`, `WaermebedarfExternKontextMenuCtrl.resx` —
werden heute sinnlos als EmbeddedResource eingebettet).
`SpKontextMenuCtrl` (651 Zeilen, AP9-Variantenverwaltung) technisch gleich behandeln,
Fachlogik nicht anfassen.

### 6.2 Inline-Dialoge (`new Form()` im Methodenrumpf)

| Fundstelle | Empfehlung |
|---|---|
| `Form_KiChat.cs:1081` (`ProtokollZeigen`) + `:1256` (`VorschauZeigen`) | fast wortgleiche Monospace-Viewer → zu **einem** `Form_TextAnzeige(titel, text, kopf = null)` zusammenlegen (~1 h) |
| `Form_KiChat.cs:1133` (`EinstellungenOeffnen`) | als eigenständigen Designer-Dialog extrahieren (Regel 1: Texte über `MyResource`, nicht Form-resx). Behebt zugleich: kein `using` (Form wird nie disposed) und hart deutsche Texte (~2 h) |
| `Form_KiChat.cs:820` (`WerkzeugeOeffnen`) | als `Form_KiWerkzeuge` extrahieren, aber als Code-Konstruktor-Klasse — die Parametermaske ist dynamisch (~2–3 h) |
| `Form_Simulation_Config.Uebersicht.cs:661` (`BetriebsmodusBearbeiten`) | **bester Designer-Kandidat der Inline-Gruppe**: 3 RadioButtons + fixe Erläuterungslabels, Texte bereits lokalisiert; entschärft zugleich das Übersetzungs-Layoutrisiko der auf 460 px fixierten Labels (~1–2 h) |
| `Form_Simulation_Config.cs:277` (`SpeicherregelungBearbeiten`) | **nicht anfassen** — laut Methodenkommentar „D1: derzeit ohne Aufrufer", steht nur bis zur Abnahme; danach komplett entfernen |
| `Form_Simulation_Config.Uebersicht.cs:1239` (`EingabeDialog`) | inline belassen (21 Zeilen), nur `using` ergänzen; optional später mit `Form_Sp_ItemNeu` zu einem projektweiten `Form_TextEingabe` vereinen |

## 7. Empfohlene Reihenfolge

- **Stufe 0 — Aufräumen (unabhängig von jeder Migration, ~1 h):**
  `: Form` aus den 10 KontextMenuCtrl streichen; 4 leere Controller-resx löschen;
  tote Klassen entscheiden (`Form3Src` löschen; `Form_ChartZoom` löschen oder anbinden;
  `Form_AlsVariante` anbinden oder entfernen); resx-Leichen `Form_Quellprofil.resx`
  und `Form_ChartZoom.resx` löschen.
  **Stand 20.08.2026: umgesetzt** — 10× `: Form` gestrichen, alle 7 leeren resx
  (4 Controller + `Form_Quellprofil` + `Form_ChartZoom` + `Form3Src`) und `Form3Src.cs`
  gelöscht, Build Debug/x86 grün (VS-MSBuild; `dotnet build` scheitert wegen der
  COM-Referenzen an MSB4803). **Offen:** Entscheidung `Form_ChartZoom` (Klasse) und
  `Form_AlsVariante` — löschen oder anbinden.
- **Stufe 1 — Piloten (A/S, ~1 Arbeitstag):** `Form_SpeicherVariantenVergleich`
  (zeigt Dispose-Konflikt, AutoScale-Verhalten und `TexteSetzen`-Mechanik an einer
  überschaubaren Form), dann `Form_SpotpreisImport` und `Form_PlanwertUebernahme`
  (beide voll lokalisiert und logikfrei). Ergebnis: validiertes Rezept.
- **Stufe 2 — restliche A/S (~2–3 Tage):** `Form_BkUebernahme`, `Form_GanglinieProtokoll`,
  `Form_QuellePufferspeicher`, `Form_WirtschaftlichkeitVerlauf`,
  `Form_GesetzparameterZeile` + `Form_Gesetzesparameter` (Dateizerlegung),
  `Form_LizenzVerwaltung` (Lokalisierung zuerst); dazu die Inline-Extraktionen
  `BetriebsmodusBearbeiten` und `Form_TextAnzeige`.
- **Stufe 3 — A/M (~1 Woche):** `Form_PufferSp_Projekt`, `Form_QuelleErdreich`,
  `Form_PeakShaving`, `Form_KwkgModule`, `UcWirtschaftlichkeit`, `ucStromAufschlaege`,
  `UcBericht` (Lokalisierung zuerst), `Form_GanglinieImportOptionen`,
  KiChat-Einstellungsdialog.
- **Stufe 4 — Kategorie B:** nur bei konkretem Anlass (z. B. wenn eine Maske ohnehin
  umgebaut wird). `Form_Lizenz` erst nach Auslagerung der Rechtsprosa.
- **Nie:** Kategorie C und die beiden Karten-Controls.

## 8. Nebenbefunde außerhalb des Auftrags (bei der Analyse gefunden, real)

1. **Bug — BHKW-Kontextmenü:** `FormMain.cs:439–443` `Add_BHKWKontext()` erzeugt per
   Copy-Paste-Fehler einen `WPKontextMenuCtrl` auf `listView_WP` statt eines
   `BHKWKontextMenuCtrl` auf `listView_BHKW`. Die BHKW-Liste bekommt ihr
   Rechtsklick-Menü nur zufällig über den Drag&Drop-Pfad (`FormMain.cs:754`).
2. **Bug — Doppelverdrahtung:** `MenueCtrl.cs:118/119` und `:165/166` rufen
   `Add_WPKontext()` **und** `Add_BHKWKontext()` → wegen (1) hängen zwei
   `WPKontextMenuCtrl` an `listView_WP`; `MenueCtrl.cs:168/174` rufen zweimal
   `Add_SpKontext()` → dasselbe für `listView_SP`.
3. **Toter Code:** `Form3Src`, `Form_ChartZoom`, `Form_AlsVariante.Zeige` (nie verdrahtet),
   `Form_Lizenz.ZustimmungSicherstellen()` (Erststart-Pfad tot).
4. **Ressourcen-Leichen als EmbeddedResource:** 4 leere Controller-resx, leere
   `Form_Quellprofil.resx` (kam am 11.08.2026 über `GitHub_Sync.bat` herein), verwaiste
   `Form_ChartZoom.resx`.
5. **Dispose-Lücken:** Inline-Forms ohne `using` in `Form_KiChat.cs:1133` und
   `Form_Simulation_Config.Uebersicht.cs:661/:1239`.
6. **Lokalisierungslücken** (unabhängig vom Designer-Thema): `Form_Lizenz` (~60 Texte +
   Rechtsprosa), `Form_LizenzVerwaltung` (~28), `UcBericht` (13), `Form_AlsVariante` (6),
   `Form_KiChat` (~30), Teile von `Form_WirtschaftlichkeitParameter`, `Form_Tarifstruktur`,
   `Form_KwkgModule`, `UcWirtschaftlichkeit`, `Form_WirtschaftlichkeitVerlauf`.
