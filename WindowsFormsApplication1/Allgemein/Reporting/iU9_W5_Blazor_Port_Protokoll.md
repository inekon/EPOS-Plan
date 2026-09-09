# iU9 Welle 5 — Port der Seiten „Berichte & Kosten" (Umsetzungsprotokoll)

> Muster: [`iU9_W4_Blazor_Port_Protokoll.md`](iU9_W4_Blazor_Port_Protokoll.md),
> [`iU9_W3_Blazor_Port_Protokoll.md`](iU9_W3_Blazor_Port_Protokoll.md),
> [`iU9_W2_Blazor_Port_Protokoll.md`](iU9_W2_Blazor_Port_Protokoll.md) und
> [`iU9_W1_Blazor_Port_Protokoll.md`](iU9_W1_Blazor_Port_Protokoll.md) —
> Feldkarten-Abgleich je Maske, Abweichungsliste A‑n, Entscheidungen,
> Windows-Abnahmepunkte.
>
> Basis `740c73e` (Branch `ios_migration`), Arbeitsstand 03.09.2026.
> Plan: Wellenplan iU9, Abschnitt C Zeile W5 und „Hüllentypen", E Priorität
> 9–11, F, G (R4/R5/R6).

---

## 1. Auftrag und Ergebnis

**Sechs WinForms-Masken → sechs Razor-Komponenten**, jede WinForms-Fassung
gelöscht (Regel M1). Es ist die erste Welle mit **Seiten** statt Dialogen: Der
ganze Reiter „Berichte & Kosten" der Startmaske ist jetzt Blazor, in **einer**
WebView.

| # | Maske (Zeilen) | Komponente | Datenseite | Aufrufer nach dem Umbau |
|---|---|---|---|---|
| W5.1 | `Form_BkUebernahme` (180) | `EPOS.UI/Dialoge/Berichte/BkUebernahmeDialog.razor` | inline im Aufrufer, ab W5.6 `UebersichtSeiteGaben` | `UebersichtSeite` (Überlagerung) |
| W5.2 | `UcBericht` (508) | `EPOS.UI/Seiten/Berichte/BerichtSeite.razor` | `Views/Bericht/BerichtSeiteGaben.cs` | `BerichteKostenSeite` |
| W5.3 | `UcWirtschaftlichkeit` (831) | `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor` | `Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs` | `BerichteKostenSeite` |
| W5.4 | `UcBkKosten` (1 311, K4) | `EPOS.UI/Seiten/Berichte/KostenSeite.razor` | `Views/BerichteKosten/KostenSeiteGaben.cs` | `BerichteKostenSeite` |
| W5.5 | `UcBkUebersicht` (1 552, K4) | `EPOS.UI/Seiten/Berichte/UebersichtSeite.razor` | `Views/BerichteKosten/UebersichtSeiteGaben.cs` | `BerichteKostenSeite` |
| W5.6 | `UcBerichteKosten` (810, K4) | `EPOS.UI/Seiten/Berichte/BerichteKostenSeite.razor` | `Views/BerichteKosten/BerichteKostenHuelle.cs` | `Form_Start.tabPage6` (`BlazorSeite<T>`) |

Zusammen **5 192 Zeilen WinForms**. Dazu die **nicht-modale Hülle**
`BlazorSeite<T>`, drei neue Bausteine und der Nachzug von **A‑17** (Welle 3)
und **A‑2** (Welle 4).

**Commits** (ein Commit je Nummer, Reihenfolge des Plans):

```
d95283c  iU9-W5.0   Seiten-Huelle BlazorSeite, Bausteine Reiter und Kachelraster
a39fe13  iU9-W5.1   BkUebernahmeDialog statt Form_BkUebernahme
cd4213d  iU9-W5.2   BerichtSeite als Razor-Komponente
bf38fa6  iU9-W5.3   WirtschaftlichkeitSeite als Razor-Komponente
47ea9e3  iU9-W5.4   KostenSeite als Razor-Komponente
8ea1e2e  iU9-W5.5   UebersichtSeite als Razor-Komponente
f59aed1  iU9-W5.6a  Sieben Huellen liefern ihren Parametersatz (Gaben)
ff4e6f7  iU9-W5.6   BerichteKostenSeite in Form_Start.tabPage6; sechs Masken geloescht
f5d660f  iU9-W5.7   Ressourcen-Sammelnachtrag (34 Schluessel de + en), Hilfekatalog
f39b4a3  iU9-W5.8   Formularkarte — neue Zaehler und das achte Pruefmuster
```

---

## 2. Bauweise

### 2.1 Die Seiten-Hülle (W5.0) — `Allgemein/Blazor/BlazorSeite.cs`

`BlazorDialogForm<T>` ist ein eigenes modales Fenster: Es kommt, zeigt,
liefert ein `DialogResult` und geht wieder. Eine **Seite** sitzt in einer
vorhandenen Maske und bleibt dort, solange die Maske offen ist. Die Hülle ist
deshalb ein `UserControl` und kein `Form`.

Sie trägt dieselben `CreationProperties` wie die Dialoghülle — insbesondere
denselben `UserDataFolder`. Das ist keine Verdopplung, sondern der Zweck: **ein
gemeinsamer Browserprozess** für Dialoge und Seiten, sonst laufen zwei
nebeneinander.

**EINE WebView je Fenster** (Risiko R5). Eine `BlazorWebView` kostet 60–120 MB
und 100–300 ms Aufbau. Die vier Seiten laufen deshalb in **einer** Hülle mit
**einer** WebView; das Umschalten ist Sache der Komponente.

### 2.2 Der geteilte Zustand — `EPOS.UI/Dienste/SeitenZustand.cs`

Eine `BlazorDialogForm` setzt ihre Parameter **einmal**, beim Aufbau — ein
Dialog lebt kurz. Eine Seite lebt so lange wie ihre Maske, und unter ihr
wechselt das Projekt: Wer im Kopfband der Startmaske auf eine andere Version
derselben Gruppe umschaltet, erwartet, dass die Seite folgt. Die WebView
deswegen wegzuwerfen wäre jedes Mal ein Aufblitzen und eine Drittelsekunde
Wartezeit.

`SeitenZustand` ist ein gewöhnliches Objekt mit einem Ereignis: Die Hülle
schreibt (`ProjektSetzen`, `Auffrischen`), die Komponente hängt sich an
`Geaendert` und zeichnet neu. Geschrieben wird aus dem Oberflächenfaden von
WinForms, gezeichnet im Blazor-Verteiler — die Komponente ruft deshalb
`InvokeAsync`, bevor sie zeichnet (Muster `AppWurzel.OeffneMaske`).

### 2.3 DPI — der offene Punkt (Risiko R4)

Die Anwendung läuft DpiUnaware (`app.manifest`, `Program.SetHighDpiMode`). Die
Dialoghülle umgeht das mit der `DpiInsel`: Sie stellt den Faden für die Dauer
des modalen Laufs auf „Per Monitor V2", und weil dabei sowohl das Fenster als
auch das Fenster der WebView2 entsteht, ist der Dialoginhalt scharf.

**Für eine eingebettete Seite geht das nicht.** Sie hat kein eigenes Fenster;
sie sitzt im Fenster der DpiUnaware-`Form_Start`, und Windows skaliert dieses
Fenster als Bitmap — bei 125–200 % also unscharf. Ein Fenster kann seinen
DPI-Kontext nachträglich nicht wechseln.

**`BlazorSeite` versucht es deshalb gar nicht erst.** Sie dokumentiert den
Befund im Kopfkommentar und setzt `DefaultBackgroundColor` gegen das weiße
Aufblitzen. Der Weg zur scharfen Seite ist, die Anwendung insgesamt DPI-fähig
zu machen — **offener Entscheid iF21**, ein eigenes Paket, das die fest
gerechneten Pixelkoordinaten der gewachsenen WinForms-Masken betrifft. Bis
dahin ist die Schärfe der Seite ein **Windows-Abnahmepunkt** (§ 9, F‑1) und
keine Zusage.

### 2.4 Die drei neuen Bausteine (W5.0)

**`Bausteine/Reiter.razor` + `Reiterblatt.razor`** (Bausteinlücke 10) — der
Ersatz für `TabControl`/`TabPage`. Die Blätter melden sich **selbst** an
(`CascadingValue`): Der Aufrufer schreibt nur seine Inhalte hin und pflegt
keine zweite Liste der Reitertitel — zwei Listen wären zwei Wahrheiten. Die
Leiste trägt `role="tablist"`, jeder Knopf `role="tab"` samt `aria-selected`
und `aria-controls`, das Blatt `role="tabpanel"`. Pfeil links/rechts wandern
und wählen sofort aus (ARIA „automatic activation" — dasselbe Verhalten wie
Strg+Tab im TabControl), Pos1 und Ende springen an die Enden; nur der aktive
Knopf steht im Tabulatorzyklus. Ein **nicht gewähltes Blatt wird gar nicht
gezeichnet** — verzögerter Aufbau, und kein Feld eines verborgenen Reiters im
Tabzyklus.

Das **Betreten** meldet der Reiter, nicht das Blatt: Er weiß als einziger
sicher, welches Blatt vorn steht, und er zeichnet bei jedem Wechsel ohnehin
neu. Ein Blatt, das sein Betreten selbst meldete, hinge davon ab, ob der
Verteiler es diesmal überhaupt neu gezeichnet hat (im Prüfstand nachgewiesen:
es tat es nicht).

**`Bausteine/Kachelraster.razor`** — `auto-fit`/`minmax` statt gerechneter
Prozentspalten. Der Bestand baute dafür zweimal ein `TableLayoutPanel`
(`UcBkKosten.pnlKacheln`, `UcWirtschaftlichkeit.KachelnBauen`). Auf einem
schmalen Fenster stehen die drei Karten untereinander statt auf ein Drittel
gequetscht.

**`Bausteine/Kennzahlkachel.razor`** — die Karte aus `UcBkKosten.Kachel`, die
seit KD6a auf beiden Seiten steht (dort über ein `internal`, damit es nur EINE
Gestaltung gibt). Genau diese eine Gestaltung ist jetzt der Baustein. Sie ist
bewusst kein `<button>`: Ein Klick tut nichts, und eine Sprachausgabe soll sie
nicht als Schaltfläche melden.

### 2.5 Der Nachzug aus den Wellen 3 und 4

Drei Dialoge bekommen ihre **Reiterform zurück**, die sie mangels Baustein als
Abschnitte untereinander stellten:

| Dialog | vorher | jetzt | erledigt |
|---|---|---|---|
| `KostenprofilDialog` | drei `Gruppenkopf` untereinander | drei Reiter (Monat, Woche, Grafik); das Betreten von „Grafik" zeichnet die Vorschau neu — wie der Vorläufer bei jedem Reiterwechsel | **W3‑O3 / A‑17** |
| `KostenKomponenteDialog` | Ertragsabschnitt unter dem Raster | zwei Reiter; der zweite **fehlt**, wenn das Gewerk keinen Ertrag kennt — der Vorläufer entfernte die Reiterseite zur Laufzeit (`ErtragReiterSteuern`) | **W4‑O1 / A‑2** |
| `EnergietraegerEinstellungen` | vier `Gruppenkopf` untereinander | zwei Reiter („Preise & Umrechnung", „Emissionen") wie im Vorläufer; **Historie und Speichern stehen UNTER der Leiste**, weil der Speichern-Knopf für die ganze Karte gilt | **W4‑O1 / A‑2** |

### 2.6 Sieben Hüllen liefern ihren Parametersatz (W5.6a)

Sobald der **Wirt** selbst eine Razor-Komponente ist, wäre ein zweites Fenster
eine zweite WebView über der ersten (Risiko R2). Sieben Hüllen bekommen
deshalb dasselbe `Gaben`-Muster, das die Wellen 1 bis 3 mit W4.4 schon
bekommen haben:

```
TarifstrukturHuelle.Gaben(idStamm, sicht)
KapitalwertVerlaufHuelle.Gaben(idStamm, name, varianten, out neuGesammelt)
WirtschaftlichkeitParameterHuelle.Gaben(idStamm, sprung)
PhotovoltaikVerguetungHuelle.Gaben(idStamm, besitzerHalter)
BhkwWirtschaftlichkeitHuelle.Gaben(idStamm, ergebnisse, out titel)
KostenKomponenteHuelle.GabenProjekt(idProjekt, name, komponente, betrieb, anlage)
EnergietraegerHuelle.Gaben(projektId, traegerId)
```

Drei Besonderheiten: Der Verlauf meldet über einen `out`-Delegaten, ob neu
simuliert wurde (Review Phase 11) — das konnte vorher der Rückgabewert von
`Oeffnen`. Der PV-Dialog braucht ein Fenster für seinen Dateiwähler; es kommt
jetzt als `Func<Form>` herein. Die beiden Kosten-Hüllen sind Instanzklassen
und halten den Bearbeitungsstand; ihre Instanz lebt über die Rückrufe des
Satzes so lange wie der Bereich.

**Damit stehen sieben weitere Unterdialoge im selben Fenster wie ihr Wirt** —
insgesamt sind es nach dieser Welle sechzehn.

### 2.7 Die vier Datenseiten

| Datei | Zeilen | Lädt / rechnet |
|---|---|---|
| `Views/BerichteKosten/UebersichtSeiteGaben.cs` | 780 | `VariantenCtrl`, `ProjektDetails`, `AbweichungsErmittler`, `SimulationRunner`, `MerkmalUebernahmeCtrl`, `KomponentenUebernahmeCtrl`, Registry-Ablage der letzten Gruppe |
| `Views/BerichteKosten/KostenSeiteGaben.cs` | 660 | `WirtschaftlichkeitCtrl` (dieselbe Leselogik wie die Kapitalwertrechnung), `KostenSummenCtrl`, `ProjektEnergietraegerCtrl`, `EmissionsFaktorLader`, `EmissionenCtrl` |
| `Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs` | 490 | `WirtschaftlichkeitCtrl`, `BerichtsDatenSammler`, `WirtschaftlichkeitZeilen` (EINE Zeilendefinition für Seite, Word und Excel), `EmissionsBilanzRechner`, die fünf Unterdialoge |
| `Views/Bericht/BerichtSeiteGaben.cs` | 340 | `BerichtCtrl`, `BerichtsDatenSammler`, `ProjektvergleichBericht`, `Dienste.Datei` |
| `Views/BerichteKosten/BerichteKostenHuelle.cs` | 210 | der geteilte Zustand (Stamm, Markierung, Verwerfen der Gruppenseiten) und der Parametersatz je Seite |

**Kein neuer Kern-Controller.** Alle vier Seiten riefen schon vorher
ausschließlich Kern-Controller (Hausmuster Ä9). Die vier SQL-Anweisungen der
Kostenseite (Trägerliste, Leistungspreis, zwei Preisabfragen) sind **wortgleich**
mitgewandert — dieselben Spalten, dieselbe Vorrangkette, dieselben Parameter.

---

## 3. Feldkarten-Abgleich

Der Abgleich ist **als Test ausgeführt** (`EPOS.UI.Tests/Seiten/*Tests.cs`,
`EPOS.UI.Tests/Dialoge/BkUebernahmeDialogTests.cs`), nicht als einmalige
Messung. Die Karten der drei Designer-Masken wurden vor Beginn frisch gezogen
(`Werkzeuge/Formularkarte`, Stand `740c73e`); für die drei K4-Masken ist der
Feldbestand aus der `.cs` erhoben (Regel F1).

| Maske | Soll (Feldkarte) | Ist (Komponente) | Deckung |
|---|---|---|---|
| `Form_BkUebernahme` | 13 Zeilen: Gegenstand, Quellenauswahl, 2 Wertpaare (Titel + Wert), Ziel, Komponentenzeile, Klartextfeld, Grundzeile, OK, Abbrechen | Kontextzeile, `Auswahlfeld`, 3 `Kohaerenzzeile` bzw. `Textfeld` (Klartextmodus), `Herleitungszeile`, `Warnbanner`, 2 Knöpfe | **13/13** |
| `UcBericht` | 15 Zeilen: Variantenliste (4 Spalten), Bausteinliste, Hinweiszeile, „Ausgabe:", Alle/Keine, 3 Optionen, Durchsuchen, Zielfeld, Erstellen, Fortschritt, Vergleich (alt), Abbrechen | `Raster` mit Wahlspalte + 4 Spalten, `Mehrfachauswahl`, `Herleitungszeile`, `Optionsgruppe` (3), `Dateiwahl`, `<progress>`, 5 Knöpfe | **15/15** |
| `UcWirtschaftlichkeit` | 11 Zeilen + 3 Laufzeitknöpfe: Infoknopf, Variantenliste, Szenario, Raster, Parameterzeile, Tarif (unsichtbar), Parameter, Verlauf, Berechnen, Schließen, Fortschritt; dazu Photovoltaik, BHKW, Strombezug | `InfoKnopf`, `Raster`, `Auswahlfeld`, Matrix, `Herleitungszeile`, 6 Knöpfe (Tarif entfällt, A‑4), `<progress>`, 4 `Kennzahlkachel` | **13/14** (A‑4) |
| `UcBkKosten` (K4) | Projektzeile, 2 Knöpfe, 3 Kacheln, 2 Tabellen (3 bzw. 10 Spalten), Statuszeile | Seitentitel, 2 Knöpfe, `Kachelraster` mit 3 `Kennzahlkachel`, 2 Tabellen, Statuszeile | **vollständig** |
| `UcBkUebersicht` (K4) | Stammwahl, Filter, Liste (4 Spalten), Bezeichnerfeld, 3 Knöpfe, Komponentenbereich, Statuszeile | `Auswahlfeld`, `Schalter`, Tabelle mit `Zeilenwahl` + 4 Spalten, `Textfeld`, 3 Knöpfe, Vergleichstabelle, Statuszeile | **vollständig** |
| `UcBerichteKosten` (K4) | 4 Navigationszeilen mit Sinnbild, Kopfzeile, Inhaltsfläche, Infoknopf | 4 `role="tab"`-Knöpfe mit Zeichen, Kopfzeile mit `InfoKnopf`, Inhaltsfläche | **vollständig** |

**133 neue bunit-Tests** prüfen Feldbestand, Beschriftungen, Vorbelegung,
Zeilenfarben, Kurztexte, Tastatur und jeden Rückruf.

---

## 4. Abweichungen (mit Begründung)

| # | Abweichung | Begründung |
|---|---|---|
| **A‑1** | Der Übernahmedialog trägt einen **Infoknopf**; die WinForms-Fassung hatte keinen. | Jede Razor-Komponente des Hauses trägt ihn (H7). Die Zeile im Hilfekatalog ist nachgetragen. |
| **A‑2** | Die vier Wertzeilen des Übernahmedialogs sind `Kohaerenzzeile`n statt Label-Paare; ein leerer Wert erscheint als „—". | Ein leeres Label sagt nicht, ob der Wert fehlt oder leer ist. |
| **A‑3** | Ä21 löschte die losen Positionen per **Doppelklick** auf die gelbe Zeile; jetzt steht ein **Knopf** in der Zeile. | Ein Doppelklick ist auf einem Berührungsbildschirm kein Ziel (iL4) und in einer Tabelle nicht auffindbar. Die MessageBox-Rückfrage wird der Baustein `Rueckfrage`. **Erledigt 04.09.2026 (W5‑O3):** Der Doppelklick ist als ZWEITER Weg zurück, der Knopf bleibt der erste — beide gehen durch dieselbe Rückfrage. |
| **A‑4** | Der Sammel-Einstieg „Tarifstruktur…" fehlt jetzt **ganz**. | Ä16 hatte ihn schon unsichtbar gesetzt; der Vorläufer trug ihn nur noch im Designer. Ein unsichtbarer Knopf ist kein Feld. |
| **A‑5** | Die Vergleichstabelle der Wirtschaftlichkeit ist eine **Matrix**, kein `Raster`. | Ihre Spalten entstehen zur Laufzeit (eine je Version); ein `QuickGrid` braucht sie zur Übersetzungszeit. Sie trägt die Hausklasse `epos-raster` — dieselbe Optik, ohne dem Baustein eine Fähigkeit anzudichten. Dasselbe gilt für die vier Tabellen der Kosten- und Übersichtsseite: Sie tragen Zeilenfarben, Kurztexte je Zelle und Summenzeilen. |
| **A‑6** | Die Stammzeile der Variantenlisten ist **gesperrt** statt „Abwählen wird zurückgedreht". | Der Vorläufer ließ den Haken zu und drehte ihn im `ItemCheck` zurück — sichtbar als Flackern. Die Meldung bleibt bereit, falls der Weg doch erreicht wird. |
| **A‑7** | Die Szenariowahl zeigt jetzt eine **Übersetzung** (`WIRT_SZEN_*`) statt des Persistenzwerts. | Heilt W1‑O6 für diese Seite: Gespeichert wird weiter „Erwartet"/„Best"/„Worst"; die Hülle bildet Nummer auf Wert ab, die Komponente kennt nur die Nummer. |
| **A‑8** | Die fünf Unterdialoge der Wirtschaftlichkeit und die zwei Einstiege der Kostenseite stehen in einer **Überlagerung** statt in einem zweiten Fenster. | Risiko R2 — erledigt damit W4‑O3. |
| **A‑9** | Der Sprung „Parameter → BHKW-Wirtschaftlichkeit" bleibt **im selben Fenster**: Der Parameterbereich schließt, der BHKW-Bereich öffnet, danach steht der Parameterbereich wieder da. | Der Vorläufer schloss dafür ein Fenster und öffnete es neu (`WirtschaftlichkeitParameterHuelle`, Schleife). Das Ergebnis ist dasselbe, der Fensterwechsel entfällt. |
| **A‑10** | Der Berichtslauf fragt vorher über den Baustein `Rueckfrage` und meldet sein Ergebnis als **Meldung im Fenster** samt zweiter Rückfrage („öffnen?"). | Fünf MessageBox werden zwei Rückfragen, zwei Warnbanner und eine Meldung. |
| **A‑11** | Das Simulationsprotokoll der Übersichtsseite steht als **Meldung im Fenster** statt in einer MessageBox. | Dasselbe Muster; der Text bleibt wortgleich. |
| **A‑12** | Der Fortschritt ist das HTML-Element `<progress>`, kein Baustein. | Der Baustein `Fortschritt` kommt mit Welle 11 (Bausteinlücke 13). `<progress>` deckt die `ProgressBar` der Feldkarte ab und braucht keine Bibliothek. |
| **A‑13** | Die vier Vektor-Sinnbilder der Navigation (GDI: Liste, Euro, Säulen, Dokument) sind **vier Zeichen** (☰ € ▤ ▦). | Kein Renderer, kein Bild — und im Hochkontrastmodus sichtbar. |
| **A‑14** | **H11 entfällt**: Die 110 Zeilen Messcode, mit denen der Vorläufer den Infoknopf jeder Seite von der Kopfzeile abrückte, sind ersatzlos weg. | Die Kopfzeile trägt den Knopf des Behälters, jede Seite ihren eigenen im FLUSS ihres Inhalts; sie können sich nicht mehr überdecken. |
| **A‑15** | Die Kostenseite frischt nach **jedem** der zwei Einstiege auf, auch nach einem Abbruch. | Unverändert zum Vorläufer: `Aktualisiere()` stand dort hinter dem `ShowDialog`, nicht im OK-Zweig — die Unterdialoge schreiben selbst. |
| **A‑16** | Die Trägertabelle bricht ihre Spaltenköpfe um; feste Spaltengewichte gibt es nicht mehr. | Zehn Spalten passen bei keiner Fensterbreite in ihre Mindestbreiten — genau das stellte der Vorläufer mit `WrapMode = True` und `ColumnHeadersHeightSizeMode = AutoSize` fest. Die gerechneten `FillWeight` (135/125/90/95/92/85/105/62/70/70) entfallen; der Browser verteilt. |
| **A‑17** | Die Wirtschaftlichkeitsseite ist **zweisprachig**; der Vorläufer trug seine Texte als deutsche Literale im Code (`TexteSetzen`, `LadeDaten`, `ZeigeParameterzeile`). | 22 neue Schlüssel (§ 5). |
| **A‑18** | Die Registry-Ablage der zuletzt gewählten Gruppe (`Software\EPOS_PLAN\Variantentest`) bleibt in der **Hülle**. | `EPOS.UI` kennt keine Registry; auf iOS gäbe es sie nicht. Der Pfad ist unverändert, damit ein Bestandsstand seine Gruppe behält. |

---

## 5. Texte

**34 neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx` und
`Resource.en-US.resx`, als zusammenhängender Block am Ende beider Dateien
(Kommentar `iU9-W5`):

| Präfix | Zahl | Inhalt |
|---|---|---|
| `BKS_*` | 10 | die gemeinsamen Texte der vier Seiten: Fortschritt, Ja/Nein, Stammwahl, Filter, Bezeichner, die drei Pflegeknöpfe, die zwei Wahl-Kurztexte |
| `WIRT_*` | 22 | die Texte der Wirtschaftlichkeitsseite, bisher deutsche Literale im Code — Titel, Beschriftungen, Spaltenkopf, Statuszeilen, Meldungen, die drei Szenarien |
| übrige | 2 | `PVW_MELD_GESPEICHERT`, `BK_KOSTEN_ANLAGE_OHNE_POSITIONEN` (bisher nur Rückfall im Code) |

Der **Designer bleibt unberührt**: Jeder neue Schlüssel wird über
`ResourceManager.GetString` mit deutschem Rückfall gelesen (B5b‑O4) — genau der
Weg der Wellen 1 bis 4.

**Wiederverwendet** sind die vorhandenen `BK_*` (Reiter, Spalten, Meldungen,
Übernahme), `BK_KOSTEN_*`, `BK_BER_*`, `BK_UEB_*`, `WIRT_KACHEL_*`,
`BHW_*`, `PVW_KNOPF`, `KOH_ZEILE_TITEL` — zusammen über 90 Schlüssel.

`help_mapping.txt`: Der Übernahmedialog bekommt seine Zeile
(`Form_BkUebernahme.btn_Help = Varianten`). Die Kopfkommentare zu
`UcWirtschaftlichkeit`, `UcBericht` und `UcBkKosten` sagen jetzt, dass die
Masken gelöscht sind und wer ihre Schlüssel trägt; die vier Feldzeilen des
Berichts bleiben stehen, weil sie die Wikiabschnitte benennen.

---

## 6. WinForms-Seite

**Gelöscht** (11 Dateien):

```
Views/BerichteKosten/Form_BkUebernahme.{cs,Designer.cs,resx}     180 Z.
Views/BerichteKosten/UcBerichteKosten.cs                         810 Z.
Views/BerichteKosten/UcBkKosten.cs                             1 311 Z.
Views/BerichteKosten/UcBkUebersicht.{cs,resx}                  1 552 Z.
Views/Bericht/UcBericht.{cs,Designer.cs}                         508 Z.
Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.{cs,Designer.cs}   831 Z.
```

**Kopiert** (2 Dateien) — `UcBericht.{cs,Designer.cs}` nach
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Bericht/`: Die Maske ist der
**einzige Beleg für die `CheckedListBox`**, und mit ihrem Löschen fällt der Typ
aus der Typtabelle des Stapellaufs (§ 7.3).

**Neu** auf der Windows-Seite: `Allgemein/Blazor/BlazorSeite.cs`,
`Views/BerichteKosten/BerichteKostenHuelle.cs`,
`Views/BerichteKosten/UebersichtSeiteGaben.cs`,
`Views/BerichteKosten/KostenSeiteGaben.cs`,
`Views/Bericht/BerichtSeiteGaben.cs`,
`Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs`.

**Umgebaut** (Fenster **und** Parametersatz): `TarifstrukturHuelle`,
`KapitalwertVerlaufHuelle`, `WirtschaftlichkeitParameterHuelle`,
`PhotovoltaikVerguetungHuelle`, `BhkwWirtschaftlichkeitHuelle`,
`KostenKomponenteHuelle`, `EnergietraegerHuelle`.

**Aufrufer umgestellt:** `Form_Start.BaueBerichteKostenSeite` (nur dort — der
Reiter ist die einzige Stelle, an der die Seite hängt),
`Form_Start.ZeigeBerichteKosten`, `Form_Start.VariantenAnzeigeAktualisieren`,
`MDIMainForm:564`. Fünf tote Einträge aus `HilfeKontext.cs` entfernt.

**Keine Typverwendung ist übrig:**

```
git grep -nE "UcBerichteKosten|UcBkKosten|UcBkUebersicht|UcBericht\b|UcWirtschaftlichkeit|Form_BkUebernahme" \
    -- '*.cs' '*.razor' '*.resx' | grep -vP ':\s*(///|//|\*)' | grep -v Pruefmuster
→ 24 Zeilen, ausschliesslich:
   (a) HilfeSchluessel-Zeichenketten ("UcBericht.btn_Help" &c.) in den
       Komponenten und ihren Tests — Schluessel des Hilfekatalogs, § 5
   (b) Herkunftszeilen in den Kopfkommentaren der Razor-Dateien
       (@* … *@ — Fliesstext, den der Kommentarfilter nicht erkennt)
```

---

## 7. Nachweise

### 7.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental
→ 0 Fehler, 20 Warnungen
```

Basis (`740c73e`): 22. **WFO1000 sinkt von 16 auf 14** — die beiden
Eigenschaften der gelöschten `UcBkKosten.Kachel` sind weg; der Rest ist
unverändert (2 × CS0108, 2 × CS0109, 1 × WFO0003, 1 × CA2255).

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ EPOS.Kern.Tests       37 grün
  KiKern.Tests         450 grün
  SpeicherEngine.Tests 337 grün
  EPOS.UI.Tests        661 grün   (528 vorher, 133 neu)
  ────────────────────────────────
  1 485 grün, 0 rot    (1 352 vorher)
```

Die 133 neuen bunit-Tests:

| Datei | Tests | Prüft |
|---|---|---|
| `Bausteine/ReiterTests.cs` | 13 | Selbstanmeldung, Vorgabe und Rückfall, Klick, zweiter Klick ohne Meldung, Rollen, Tabulatorzyklus, ←/→/Pos1/Ende, gesperrtes Blatt, Betreten |
| `Bausteine/KachelrasterTests.cs` | 7 | Mindestbreite als Stilvorgabe, Vorgabe 220, Kacheln im Raster, Titel/Wert/Herkunft, „—" statt leer, keine Herkunftszeile, kein Knopf |
| `Dialoge/BkUebernahmeDialogTests.cs` | 17 | Feldbestand, Quellenreihenfolge, Vorgabe und Sofortladen, Quellenwechsel, Wertgegenüberstellung, „—", Komponentenzeile, Klartextmodus, Sperre samt Grund, ohne Lader, ohne Quelle, OK, Abbrechen/Esc, Enter unbelegt, Hilfeschlüssel |
| `Seiten/BerichtSeiteTests.cs` | 24 | Feldbestand, Spaltenköpfe, Hinweiszeile, veralteter Stand, Ordnerwähler (mit und ohne), Erstbefüllung, gesperrte Stammzeile, Ab-/Anwählen, Alle/Keine, Bausteinhaken, Wirtschaftlichkeitshinweis, Startrückfrage samt Anzahl, „Nein", Auftragsinhalt, Fortschritt, Auffrischen, Öffnen-Rückfrage, Fehler, Abbruch, Bestandsweg (mit und ohne Delegat), Hilfeschlüssel |
| `Seiten/WirtschaftlichkeitSeiteTests.cs` | 21 | Feldbestand, Matrixspalten, ohne Ergebnisse, drei Sichtknöpfe je Ausstattung, ohne Delegat, fehlender Sammel-Einstieg, Erstbefüllung, gesperrte Stammzeile, Szenariowechsel ohne Neuladen, fünf Bereiche (Theory), fehlender Parametersatz, Nachlauf, Berechnen samt Varianten, Fortschritt, Abbruch, Fehler, Hilfeschlüssel |
| `Seiten/KostenSeiteTests.cs` | 18 | Feldbestand, zehn Spaltenköpfe, vier Zeilenfarben, Kurztexte, Emissionskurztexte, Summenzeile ohne Wahlknopf, gesperrte Einstiege, ohne Delegat, Trägermarkierung (mit und ohne Träger), Anlagenvorwahl, Trägerverwaltung, Auffrischen, fehlender Parametersatz, Löschknopf nur gelb, Rückfrage Ja/Nein, leere Frage, Hilfeschlüssel |
| `Seiten/UebersichtSeiteTests.cs` | 19 | Feldbestand, Gegenüberstellung ohne Aktionsspalte, Unterschiede mit, gesperrte Zeile mit Strich und Grund, Gewerk nur in der ersten Zeile, Kurztexte je Zelle, veralteter Stand, markierte Zeile, Stammwechsel, Filter, Markierung, Anlegen, gesperrtes Löschen, Löschrückfrage Ja/Nein, Simulationsprotokoll, Übernahme, Abbruch, ohne Delegat, Hilfeschlüssel |
| `Seiten/BerichteKostenSeiteTests.cs` | 14 | vier Navigationseinträge, Rollen, Startseite und Rückfall, Kopfzeile, Klick samt Gaben, genau eine gezeichnete Seite, ↑/↓/Ende, Tabulatorzyklus, Hinweis ohne Stamm, Projektwechsel, Seitenwunsch (einmalig), Abmelden beim Entsorgen |

Dazu die angepassten Tests der drei nachgezogenen Dialoge (Reiter statt
Abschnitte) — 23 in `KostenprofilDialogTests`, 4 in `EnergietraegerDialogTests`,
1 in `KostenKomponenteDialogTests`.

### 7.3 Formularkarte

```
dotnet build Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 0 Fehler, 0 Warnungen
dotnet test  Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 123 grün (122 vorher)
```

**Kein Testanker** hing an den drei gelöschten Designer-Masken (die Ankerliste
F11 des Wellenplans nennt sie nicht); umzuhängen war nichts.

**Das achte Prüfmuster** ist `UcBericht`: Sie ist der einzige Beleg für die
`CheckedListBox` — mit ihrem Löschen fällt der Typ aus der Typtabelle des
Stapellaufs. Sie wandert wie `ucVorlagenZeile` mit **zwei** Dateien (ein
`UserControl`, dessen Texte im Code stehen, führt keine `.resx`).

### 7.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -c Release -- --alle WindowsFormsApplication1 --erreichbarkeit
```

| Kennzahl | nach W1 | nach W0 | nach W2 | nach W3 | nach W4 | **nach W5** |
|---|---:|---:|---:|---:|---:|---:|
| Designer-Dateien (Repo) | 114 | 108 | 105 | 101 | 92 | **89** |
| davon Masken | 111 | 105 | 102 | 98 | 91 | **88** |
| lokalisiert | 62 | 61 | 59 | 59 | 59 | **59** |
| Kartenzeilen | 2 322 | 2 231 | 2 188 | 2 128 | 1 994 | **1 955** |
| Felder ohne Beschriftung | 172 | 168 | 168 | 165 | 151 | **147** |
| Öffner erreichbar („ja") | 104 | 103 | 100 | 96 | 89 | **86** |
| unerreichbar / verwaist / unklar | 4/1/2 | 0/0/2 | 0/0/2 | 0/0/2 | 0/0/2 | **0/0/2** |

Die drei K4-Masken (`UcBerichteKosten`, `UcBkKosten`, `UcBkUebersicht`)
zählten hier nie mit — sie führten keine Designer-Datei.

### 7.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 301 SQL-Texte geprüft: 0 Fundstellen, 149 dynamisch, 1 152 in Ordnung
```

Unverändert zu W4: Die vier Anweisungen der Kostenseite sind wortgleich von
`UcBkKosten` nach `KostenSeiteGaben` gewandert.

### 7.6 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 10 Bilder geprueft, 0 Verstoesse.  ERGEBNIS: alle gruen.
```

Unverändert — die Welle hat den Renderer nicht angefasst.

### 7.7 Referenzlauf

```
dotnet run --project EPOS.Referenzlauf -c Release -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
    --projekte 1030,1007,1017 --ziel <ordner>
dotnet run --project EPOS.Referenzlauf -c Release -- vergleich Referenzlaeufe/2026-08-30_B3-Kaskade <ordner>
```

| Projekt | Ergebnis |
|---|---|
| 1007 | **PASS** (29 Dateien, 324 219 Werte) |
| 1017 | **PASS** (21 Dateien, 254 154 Werte) |
| 1030 | **PASS** (22 Dateien, 236 670 Werte) |

`diff -rq` gegen die Basis meldet für diese drei Ordner **keinen** Unterschied.
Der Lauf ist Pflicht, obwohl kein Kern-Controller entstanden ist: Die Seiten
lesen über `WirtschaftlichkeitCtrl`, `KostenSummenCtrl` und
`BerichtsDatenSammler` genau die Wege, die auch der Rechenweg nimmt, und die
Übersichtsseite **schreibt** über `MerkmalUebernahmeCtrl` und
`KomponentenUebernahmeCtrl`.

### 7.8 Veröffentlichung

```
dotnet publish WindowsFormsApplication1 -c Release -r win-x64 --self-contained -p:Platform=x64 -o <ordner>
```

`wwwroot` vollständig: `index.html`, `_framework/blazor.webview.js`,
`_framework/blazor.modules.json`, `_content/EPOS.UI/{epos-ui.css,help_icon.png}`
(samt `.br`/`.gz`),
`_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js`. Die 32
neuen CSS-Klassen (`epos-reiter*`, `epos-kachelraster`, `epos-kennzahlkachel*`,
`epos-navigation*`, `epos-seite-spalten`, `epos-seite-spalte`,
`epos-seite-zeile`, `epos-veraltet`, `epos-fortschritt*`, `epos-matrix*`,
`epos-kostentabelle`, `epos-traegertabelle`, `epos-zahl`,
`epos-zellenaktionen`, `epos-zeile--*`, `epos-kostenkopf`,
`epos-variantentabelle`, `epos-vergleichstabelle`, `epos-vergleich-gewerk`,
`epos-variantenpflege`, `epos-gesperrt`) sind in der ausgelieferten
`epos-ui.css` enthalten.

---

## 8. Grenzen

* **Keine Windows-Sicht.** Alles hier ist auf Linux gemessen. Ob die vier
  Seiten in der WebView2 richtig aussehen — und vor allem, **wie unscharf sie
  bei 125 % sind** —, sagt erst die Abnahme (§ 9).
* **Die DPI-Frage ist offen** (R4, Entscheid iF21). Sie ist nicht durch
  Nacharbeit an dieser Welle zu lösen: Ein Fenster kann seinen DPI-Kontext
  nachträglich nicht wechseln.
* **Der Fortschritt ist ein HTML-Element**, kein Baustein (A‑12) — bis Welle 11.
* **Die Fokusfalle der Überlagerung** trägt jetzt sieben weitere Unterdialoge
  und ist weiterhin ungeprüft (W4‑O4, hier F‑8).
* **Die Trägertabelle hat zehn Spalten** und keine gerechneten Breiten mehr
  (A‑16). Ob der Browser sie besser verteilt als der `DataGridView`, ist eine
  Sichtfrage.

---

## 9. Abnahmeliste Windows (iZ5) für diese sechs Masken

| # | Punkt |
|---|---|
| **F‑1** | **DPI (R4, Entscheid iF21):** Reiter „Berichte & Kosten" bei 100 %, 125 %, 150 % und 200 % ansehen. Erwartet wird ein **bitmapskalierter, also unscharfer** Inhalt ab 125 % — das ist der Befund, kein Fehler. Zu entscheiden: Reicht das bis zur DPI-Umstellung der ganzen Anwendung, oder muss iF21 vorgezogen werden? |
| **F‑2** | Der Reiter öffnet ohne weißes Aufblitzen; die Themafläche steht, bevor die WebView2 da ist |
| **F‑3** | Die vier Navigationseinträge schalten um; genau eine Seite ist sichtbar; die Kopfzeile nennt Seite und Stammnamen |
| **F‑4** | Tastatur: Tab kommt in die Navigation, ↑/↓ wandern, Tab verlässt sie nach EINEM Druck in die Seite; Pos1/Ende springen |
| **F‑5** | Projektwechsel im Kopfband der Startmaske: Alle vier Seiten folgen, **ohne** dass die Seite neu aufblitzt (die WebView bleibt) |
| **F‑6** | Menü „Projekte › Varianten und Bericht…" landet auf der Übersicht |
| **F‑7** | Ohne Stammprojekt zeigen Wirtschaftlichkeit und Bericht den Hinweis statt der Seite |
| **F‑8** | **Fokusfalle (W4‑O4):** In jeder der sechzehn Überlagerungen mit Tab im Kreis laufen — der Fokus darf den Bereich nicht verlassen |
| **F‑9** | Übersicht: Stammwahl und Filter „nur Stammprojekte"; die zuletzt gewählte Gruppe steht beim nächsten Start wieder da (Registry) |
| **F‑10** | Übersicht: Variante anlegen (der Name erscheint auch in der Klappliste des Kopfbands, Ä19), löschen mit Rückfrage, Simulation starten — das Protokoll steht im Fenster |
| **F‑11** | Übersicht, Stammzeile: die Gegenüberstellung zeigt je Variante eine Spalte, ab neun Varianten mit dem Kappungshinweis; die Merkmale einer Komponente erscheinen als Kurztext an der Zelle |
| **F‑12** | Übersicht, Variantenzeile: die Unterschiede samt Aktionsspalte; ein Knopf, wo die Übernahme trägt, ein grauer Strich mit Begründung, wo nicht |
| **F‑13** | Übernahme: Quellenwahl, Wertgegenüberstellung bzw. Klartext, „OK" schreibt, danach steht die Meldung und die Zeile bleibt markiert |
| **F‑14** | Kosten: die drei Karten zeigen „—" statt 0,00, wo nichts erfasst ist; die Fußzeile nennt alle Befunde |
| **F‑15** | Kosten: die Aktionsspalte steht mit beschriftetem Kopf („Aktionen“, W5‑B‑1) und trägt Zeilenwahl und Papierkorb ohne Hover; die gelbe Zeile löscht über den Papierkorb (A‑3) **und** über den Doppelklick (W5‑O3) — beide Wege stellen dieselbe Rückfrage, sie nennt die Komponente, „Ja" löscht |
| **F‑16** | Kosten: die Wahl einer Anlage kennzeichnet rechts ihren Energieträger (Ä19); „Kostenverwaltung öffnen…" startet mit genau dieser Komponente |
| **F‑17** | Kosten: die Trägertabelle mit zehn Spalten — Köpfe umgebrochen, Werte lesbar, rote Fehlzeile mit Kurztext (A‑16) |
| **F‑18** | Wirtschaftlichkeit: vier Karten, die Vergleichstabelle mit je einer Spalte pro Version, der Parameternachweis in einer Zeile |
| **F‑19** | Wirtschaftlichkeit: „Berechnen" mit Fortschritt und Abbrechen; nach dem Lauf stehen die Zahlen und die Statuszeile nennt den Zeitpunkt |
| **F‑20** | Wirtschaftlichkeit: die fünf Bereiche öffnen **im selben Fenster** (A‑8); der Sprung Parameter → BHKW und zurück (A‑9) |
| **F‑21** | Bericht: „Erstellen" fragt mit der Anzahl, zeigt Fortschritt, meldet die Pfade und fragt nach dem Öffnen; „Projektvergleich (alt)" ebenso |
| **F‑22** | Bericht: „Durchsuchen…" öffnet die Ordnerwahl; der Zielordner lässt sich auch tippen |
| **F‑23** | Beide Sprachen (`HKCU\Software\wp-plan\Language`): Die Wirtschaftlichkeitsseite ist erstmals englisch (A‑17) |
| **F‑24** | Maus **und** Finger (44 px), Hochkontrast, Tabellen ohne Umbruch bei üblicher Fensterbreite |
| **F‑25** | Die drei nachgezogenen Dialoge: Kostenprofil (drei Reiter), Kostenverwaltung (zwei, der zweite fehlt ohne Ertrag), Trägerkarte (zwei, Speichern unter der Leiste) |

---

## 10. Offene Punkte

| # | Punkt |
|---|---|
| **W5‑O1** | **DPI (R4) dem Anwender vorlegen** — der eigentliche Entscheid der Welle. Die Seiten sind ab 125 % bitmapskaliert, und das lässt sich nur durch iF21 (Anwendung insgesamt DPI-fähig) heilen. Bis dahin gilt: Dialoge scharf, Seiten unscharf. |
| **W5‑O2** | **A‑16 sichtprüfen:** Die Trägertabelle hat ihre gerechneten Spaltenbreiten verloren. Verteilt der Browser sie brauchbar, oder braucht sie `min-width` je Spalte? |
| **W5‑O3** | ~~**A‑3 dem Anwender vorlegen:** Der Doppelklick auf die gelbe Zeile ist ein Knopf geworden.~~ **Entschieden 04.09.2026 (Windows-Abnahme): Doppelklick als zweiter Weg nachgerüstet, Knopf bleibt; Sichtbarkeitsbefund W5‑B‑1 (`display:flex` auf dem `<td>` der Aktionsspalte, dazu ein leerer Spaltenkopf) behoben** (Commit `acc19a3`, § 12). |
| **W5‑O4** | **Die Fokusfalle bleibt ungeprüft** (W4‑O4, hier F‑8). Sie trägt jetzt sechzehn Unterdialoge; fällt sie durch, braucht `EPOS.UI` doch eine JS-Schicht — dieselbe, die W1‑O4 für `SelectAll()` und W3‑O2 für das Zoomen erwägt. |
| **W5‑O5** | **A‑12:** Der Fortschritt ist `<progress>`. Ob das reicht, bis Welle 11 den Baustein bringt, sagt die Abnahme — der Berichtslauf dauert bei fünf Varianten spürbar. |
| **W5‑O6** | Die **Kopfzeile des Reiters** trägt Titel und Stammnamen; der Vorläufer setzte sie in `lblKopf`. Ob sie an dieser Stelle noch gebraucht wird, wo jede Seite ihren eigenen Titel führt, entscheidet die Sichtabnahme. |
| **W5‑O7** | Die **Übersichtsseite lädt bei jeder Aktion neu** (`Auffrischen` nach Stammwechsel, Markierung, Anlegen, Löschen, Übernahme). Der Vorläufer tat dasselbe, hatte aber einen Detailpuffer je Gruppe — den hat die Hülle ebenfalls. Bei sehr großen Gruppen ist zu messen, ob das reicht. |
| **W5‑O8** | `BerichteKostenHuelle.SetzeProjekt` ruft `Auffrischen` **immer**, auch wenn sich die Id nicht geändert hat (der Reiterwechsel löst es aus). Das ist gewollt (die Daten können sich unter der Seite geändert haben), kostet aber je Betreten eine Ladung. |

---

## 11. Geänderte und neue Dateien

```
NEU
  WindowsFormsApplication1/Allgemein/Blazor/BlazorSeite.cs             150 Zeilen
  EPOS.UI/Dienste/SeitenZustand.cs                                      75
  EPOS.UI/Bausteine/Reiter.razor                                       160
  EPOS.UI/Bausteine/Reiterblatt.razor                                   80
  EPOS.UI/Bausteine/Kachelraster.razor                                  35
  EPOS.UI/Bausteine/Kennzahlkachel.razor                                40
  EPOS.UI/Dialoge/Berichte/BkUebernahmeDaten.cs                         65
  EPOS.UI/Dialoge/Berichte/BkUebernahmeDialog.razor                    200
  EPOS.UI/Seiten/Berichte/BerichtDaten.cs                              150
  EPOS.UI/Seiten/Berichte/BerichtSeite.razor                           430
  EPOS.UI/Seiten/Berichte/WirtschaftlichkeitDaten.cs                   110
  EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor                480
  EPOS.UI/Seiten/Berichte/KostenDaten.cs                               115
  EPOS.UI/Seiten/Berichte/KostenSeite.razor                            380
  EPOS.UI/Seiten/Berichte/UebersichtDaten.cs                           105
  EPOS.UI/Seiten/Berichte/UebersichtSeite.razor                        420
  EPOS.UI/Seiten/Berichte/BerichteKostenSeite.razor                    260
  WindowsFormsApplication1/Views/BerichteKosten/BerichteKostenHuelle.cs 210
  WindowsFormsApplication1/Views/BerichteKosten/UebersichtSeiteGaben.cs 780
  WindowsFormsApplication1/Views/BerichteKosten/KostenSeiteGaben.cs     660
  WindowsFormsApplication1/Views/Bericht/BerichtSeiteGaben.cs           340
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs 490
  EPOS.UI.Tests/Bausteine/ReiterTests.cs                               220
  EPOS.UI.Tests/Bausteine/KachelrasterTests.cs                          95
  EPOS.UI.Tests/Dialoge/BkUebernahmeDialogTests.cs                     270
  EPOS.UI.Tests/Seiten/BerichtSeiteTests.cs                            405
  EPOS.UI.Tests/Seiten/WirtschaftlichkeitSeiteTests.cs                 340
  EPOS.UI.Tests/Seiten/KostenSeiteTests.cs                             335
  EPOS.UI.Tests/Seiten/UebersichtSeiteTests.cs                         375
  EPOS.UI.Tests/Seiten/BerichteKostenSeiteTests.cs                     260
  Werkzeuge/Formularkarte.Tests/Pruefmuster/Bericht/UcBericht.cs        (Kopie)
  Werkzeuge/Formularkarte.Tests/Pruefmuster/Bericht/UcBericht.Designer.cs (Kopie)

GEAENDERT
  EPOS.UI/wwwroot/epos-ui.css                          32 neue Klassen
  EPOS.UI/Dialoge/Kosten/KostenprofilDialog.razor      A-17 nachgezogen
  EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor  A-2 nachgezogen
  EPOS.UI/Dialoge/Kosten/EnergietraegerEinstellungen.razor  A-2 nachgezogen
  EPOS.UI.Tests/Dialoge/{Kostenprofil,KostenKomponente,Energietraeger}DialogTests.cs
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/*Huelle.cs   Gaben (5)
  WindowsFormsApplication1/Views/Kosten/{KostenKomponente,Energietraeger}Huelle.cs
  WindowsFormsApplication1/Views/Hauptformular/Form_Start.cs     tabPage6
  WindowsFormsApplication1/MDIMainForm.cs                        Seitenschluessel
  WindowsFormsApplication1/Allgemein/KI/HilfeKontext.cs          5 tote Eintraege
  WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt      1 Zeile, 3 Kommentare
  EPOS.Kern/MyResource/Resource.resx, Resource.en-US.resx        34 Schluessel
  Werkzeuge/Formularkarte.Tests/{Stapel,Erreichbarkeit,Pruefmuster}Tests.cs
  Werkzeuge/Formularkarte/LIESMICH.md                            achtes Muster

GELOESCHT
  WindowsFormsApplication1/Views/BerichteKosten/Form_BkUebernahme.{cs,Designer.cs,resx}
  WindowsFormsApplication1/Views/BerichteKosten/UcBerichteKosten.cs
  WindowsFormsApplication1/Views/BerichteKosten/UcBkKosten.cs
  WindowsFormsApplication1/Views/BerichteKosten/UcBkUebersicht.{cs,resx}
  WindowsFormsApplication1/Views/Bericht/UcBericht.{cs,Designer.cs}
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.{cs,Designer.cs}
```

---

## 12. Windows-Abnahme 04.09.2026 — Befunde

| # | Befund | Ursache | Behebung |
|---|---|---|---|
| **W5‑B‑1** | Kosten: In der Tabelle „Anlagenkomponenten" ist **keine Aktionsspalte** zu sehen — weder die `Zeilenwahl` noch der Papierkorb. Der Anwender sah damit keinen Weg, eine lose Position zu löschen. | **Das Stilblatt, nicht das Markup.** `.epos-zellenaktionen` setzte `display: flex` auf das `<td>` selbst. Damit ist die Zelle keine Tabellenzelle mehr (CSS 2.1, 17.2.1 „Anonymous table objects"): Der Browser schiebt eine **anonyme** `table-cell` darunter, die Spaltenbreite hängt nicht mehr an diesem `<td>`, und jede Zellenregel des Hausblatts (`.epos-raster td`: Polsterung, Trennlinie) wie die Zeilenfarbe (`.epos-zeile--lose > td`) trifft einen Kasten, der die Zelle nicht mehr ist. Dazu war der **erste `<th>` leer** — die Spalte hatte nichts, woraus sie ihre Breite nehmen konnte, während alle anderen Spalten mit `white-space: nowrap` ihre volle Breite forderten. Die Übersichtsseite trägt dieselbe Klasse, aber einen **beschrifteten** Spaltenkopf (F‑12, abgenommen) — dort fiel es nicht auf. | `.epos-zellenaktionen` ist wieder eine gewöhnliche Zelle (`width: 1%`, `white-space: nowrap`); der Flexkasten steht **darin** als `.epos-zellenaktionen-inhalt`. Der Spaltenkopf ist beschriftet (`SpalteAktionen`, Schlüssel `BK_KOSTEN_SP_AKTIONEN`, de „Aktionen" / en „Actions"). Die Knöpfe sind **ohne Hover** sichtbar (iL4). Wache: `KostenSeiteTests.Die_Aktionszelle_traegt_im_Stilblatt_kein_display_flex` liest das Stilblatt selbst — eine bunit-Probe hätte den Fehler nie gesehen, denn das Markup war richtig. |
| **W5‑B‑2** | Kosten: „Heizkessel — ohne Anlage (gelb) wird mit **Doppelklick** nicht gelöscht." | Die Angleichung A‑3 hatte den Doppelklick des Vorläufers (Ä21) durch den Papierkorb-Knopf ersetzt; W5‑O3 hielt das zum Entscheid offen. | **W5‑O3 entschieden:** `@ondblclick` hängt wieder an jeder Zeile der Anlagentabelle und endet in derselben `LoeschenFragen(zeile)` wie der Knopf — eine Rückfrage, eine `Loeschen`-Gabe. Die eine Bedingung beider Wege steht **einmal** in `LoeschenFragen` (`!z.Loeschbar || LoeschFrage is null`), nicht zweimal im Markup; auf gebundenen Zeilen passiert nichts. Die gelbe Zeile trägt `user-select: none`, damit der Doppelklick keinen Text markiert. |

**Grenze des Nachweises.** Beide Befunde sind auf Linux geprüft — sechs neue
bunit-Proben (2 205 → **2 211**, auch unter `LANG=en_US.UTF-8`). Die
**Sichtprüfung in WebView2 steht beim Anwender**: In der Arbeitsumgebung ist
kein Browser erreichbar, der Befund W5‑B‑1 ist deshalb aus Markup, Stilblatt
und Tabellenboxmodell hergeleitet, nicht am Bild gemessen.

---

## 13. Anwenderwunsch 05.09.2026 (W5‑E‑1) — Variantenwahl als Auswahlfeld

> **Wortlaut des Anwenders:** „Variantenprojekte-Auswahl als Dropdown, damit
> weniger Platz verwendet wird."

Gemeint ist die Seite **Berichte & Kosten → Übersicht — Stammprojekt und
Varianten** (`EPOS.UI/Seiten/Berichte/UebersichtSeite.razor`).

### 13.1 Vorher / Nachher

| | vorher (W5.5) | nachher (W5‑E‑1) |
|---|---|---|
| Wahl der Version | **Tabelle** `epos-variantentabelle`: Wahlknopf (`Zeilenwahl`), Art, Bezeichner, Projektname, Simulation — ein Spaltenkopf und je Version eine Zeile | **Auswahlfeld** „Variante:" (Baustein `Auswahlfeld`) — der Stamm als erster Eintrag, dann die Varianten; Eintragstext „Bezeichner — Projektname", Id = `Tab_Projekt.ID` |
| Verwaltung | zweite Spalte `epos-variantenpflege` (max. 260 px) mit Bezeichnerfeld und den drei Knöpfen **untereinander** | **eine Zeile** `epos-variantenzeile`: Auswahlfeld, Bezeichnerfeld, „Variante anlegen", „Variante löschen", „Simulation starten" |
| Simulationsstand | Spalte „Simulation" je Zeile, `— (fehlt) ⚠` bzw. `05.09.26 16:23 ⚠`, **ohne Kurztext** | leise Zeile `epos-simstand` unter der Zeile: „Simulation: 05.09.26 16:23" bzw. „noch nicht simuliert", das „⚠" als eigenes Element **mit Grund im Kurztext**, `aria-live="polite"` |
| Unterschiedstabelle | `epos-raster-huelle` (Höchsthöhe 22 rem) unterhalb des zweispaltigen Blocks | rückt hoch und steht in `epos-raster-huelle--vergleich` — **35,2 rem** (`calc(var(--epos-listenhoehe) * 1.6)`), innerer Rollbalken und stehender Spaltenkopf wie gehabt |
| Höhe über der Tabelle | vier bis fünf Zeilen (Tabellenkopf + je Version eine Zeile) | **zwei Zeilen** (Auswahlfeldzeile + Statuszeile), unabhängig von der Zahl der Varianten |

**Was das „⚠" bedeutet — nachgesehen, nicht geraten.**
`BerichtsDatenSammler.ErmittleStatus` setzt es in **zwei** Fällen:
`SimStand` ist `null` (es liegt kein Ergebnis vor) **oder** `Veraltet` ist
gesetzt, das heißt der Zeitstempel des Ergebnisses ist **älter als
`Tab_Projekt.Aenderungsdatum`**. Bis hierher sagte das Zeichen nicht, welcher
der beiden Fälle gerade gilt; jetzt sagt es der Kurztext
(`BKS_SIM_GRUND_FEHLT` / `BKS_SIM_GRUND_VERALTET`).

### 13.2 Was dafür nötig war

- **`VarianteZeile.SimZeitpunkt`** (neu, `EPOS.UI/Seiten/Berichte/BerichtDaten.cs`):
  der **reine** Zeitpunkt, leer = nie simuliert. `SimStand` bleibt unverändert
  der fertige Zellentext der Tabellen von Bericht und Wirtschaftlichkeit — er
  trägt das „⚠" und im Fehlfall den Wortlaut „— (fehlt) ⚠" in sich. Aus ihm
  ließe sich der Wert nur durch Raten zurückgewinnen; deshalb ein eigenes Feld
  und keine Zerlegung. Gefüllt wird es in `UebersichtSeiteGaben.Laden()` aus
  demselben `VariantenStatus`, aus dem auch `SimStandText` kommt.
- **`Auswahlfeld.Kurzname`** (neu, `EPOS.UI/Standards/Auswahlfeld.razor`):
  optionales `aria-label`. Die sichtbare Beschriftung ist aus Platzgründen
  „Variante:", die Sprachausgabe hört „Version wählen" (`BKS_WAHL_VERSION` —
  derselbe Text, der bis hierher am Wahlknopf der Zeile hing). Leer = kein
  `aria-label`, dann benennt wie bisher das umschließende `<label>` das Feld.
- **Vier neue Schlüssel** in `Resource.resx` und `Resource.en-US.resx`
  (Block `BKS_*`, gelesen über `ResourceManager.GetString` mit deutschem
  Rückfall — Weg B5b‑O4): `BKS_LBL_VARIANTE`, `BKS_SIM_NIE`,
  `BKS_SIM_GRUND_FEHLT`, `BKS_SIM_GRUND_VERALTET`.
- **Drei Parameter entfallen** an der Seite und in ihrem Parametersatz:
  `SpalteArt`, `SpalteBezeichner`, `SpalteProjektname` — die Spalten gibt es
  nicht mehr. `SpalteSimulation` (`BK_BER_SP_SIMULATION`, de/en „Simulation")
  **bleibt** und beschriftet jetzt die Statuszeile: dieselbe Ressource,
  dieselbe Aussage, eine Zeile statt einer Spalte. Die Ressourcenschlüssel
  `BK_SP_ART`/`BK_SP_BEZEICHNER`/`BK_SP_PROJEKTNAME` bleiben im Bestand — die
  Berichts- und die Wirtschaftlichkeitsseite führen ihre Tabellen weiter.
- **Stilblatt** (`epos-ui.css`, ein Block im Abschnitt „Seiten des Reiters
  Berichte & Kosten"): `.epos-variantenzeile` (das Auswahlfeld darf wachsen,
  das Bezeichnerfeld bleibt schmal, Umbruch über das `flex-wrap` von
  `.epos-seite-zeile`), `.epos-simstand`, `.epos-raster-huelle--vergleich`.
  Gelöscht: `.epos-variantenpflege` und der Selektor `.epos-variantentabelle`.

**Die Hausregel W9‑B‑2 bleibt.** Die Unterschiedstabelle steht weiter in
`.epos-raster-huelle` — fester Rahmen, `overflow: auto`, stehender
Spaltenkopf. Geändert ist allein die **Höchsthöhe**, und sie ist an
`--epos-listenhoehe` gerechnet, damit sie mit der Schrift und mit der
Hausregel mitwächst. Der Grund für die Ausnahme: Auf dieser Seite ist die
Tabelle der **Inhalt** und nicht eine Liste neben ihm.

### 13.3 Abnahmepunkte

| # | Was der Anwender sehen muss |
|---|---|
| **A‑1** | Statt der Variantentabelle steht ein Auswahlfeld „Variante:". Es führt **den Stamm als ersten Eintrag** („(Stammprojekt) — Beispiel WP WG 1") und darunter je Variante „Bezeichner — Projektname". |
| **A‑2** | Ein Wechsel im Auswahlfeld tut, was vorher die Zeilenwahl tat: Die Überschrift wechselt zwischen „Komponenten der Gruppe im Vergleich" und „Unterschiede der Variante ‚…'", die Tabelle darunter wechselt mit, „Variante löschen" ist auf dem Stamm gesperrt und auf einer Variante frei. |
| **A‑3** | Unter der Zeile steht leise „Simulation: 05.09.26 16:23". Fehlt das Ergebnis, steht dort „noch nicht simuliert". Ist es veraltet oder fehlt es, steht ein „⚠" daneben, und der **Mauszeiger darauf** nennt den Grund. |
| **A‑4** | Bezeichnerfeld und die drei Knöpfe stehen **in einer Zeile** mit dem Auswahlfeld; auf schmalem Fenster rutschen die Knöpfe darunter. Der Schalter „nur Stammprojekte" steht unverändert oben beim Stammfeld. |
| **A‑5** | Die Unterschiedstabelle beginnt **deutlich weiter oben** und ist höher; ihr eigener Rollbalken und der stehende Spaltenkopf sind geblieben. Die Seite selbst rollt nicht mehr, um an die Tabelle zu kommen. |
| **A‑6** | Tastatur: Tabulator führt Stammfeld → Filter → Variante → Bezeichner → die drei Knöpfe. Im Auswahlfeld wählen ↑/↓ die Version. |

### 13.4 Nachweise

- `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → **0 Fehler**,
  6 Warnungen (die fünf Altwarnungen von `EPOS.Kern` und `WFO0003`).
- `dotnet test EPOS.UI.Tests -c Release` → **2 400** grün (vorher 2 392;
  zehn neue Fälle, zwei durch sie ersetzte entfallen), ebenso unter
  `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8`. Darin `StilblattTests`
  (Klammerbilanz, kein Nesting) und `ListenrahmenTests` (Hausregel W9‑B‑2).
- `dotnet test Werkzeuge/Formularkarte/Formularkarte.sln -c Release` → **122** grün.
- Die neuen Fälle in `EPOS.UI.Tests/Seiten/UebersichtSeiteTests.cs`:
  Einträge und Reihenfolge des Auswahlfelds, die gewählte Version, der leere
  Stand, das `aria-label`, die Statuszeile mit und ohne „⚠" samt beiden
  Gründen, der Wechsel treibt `MarkierteId`/Unterschiede/Knöpfe, die
  Variantentabelle ist weg, die Unterschiedstabelle steht im höheren Rahmen
  (Markup **und** Stilblatt — eine bunit-Probe sieht eine Stilregel nicht,
  Lehre W6‑B‑1).

**Grenze des Nachweises.** Geprüft auf Linux. Die **Sichtprüfung in WebView2
steht beim Anwender**: Ob die eine Zeile auf seinem Fenster wirklich ohne
Umbruch steht und wie viel Höhe die Unterschiedstabelle gewinnt, ist aus
Markup und Stilblatt hergeleitet, nicht am Bild gemessen.


## Windows-Abnahme 05.09.2026 — Formularraster, Paket P2 (iU8‑E‑2)

**Der Wortlaut.** „Darstellung der Dialoge kompakter und übersichtlicher — Parameterblöcke
rechts. Genauso für andere Dialoge prüfen." Die hausweite Regel dazu steht seit Aufgabe #90 in
`EPOS.UI/Bausteine/Formularraster.razor` und `Formulargruppe.razor`; sie ist in
[`iU9_W14a_Blazor_Port_Protokoll.md`](iU9_W14a_Blazor_Port_Protokoll.md), Abschnitt
„Kompaktes Formularraster", hergeleitet und gemessen. Paket **P2** (Kosten und
Wirtschaftlichkeit, Wellen 1–5) hängt die Parameterblöcke dieser Welle ein — **kein Feld
umbenannt, kein Text geändert, keine Regel je Dialog, kein neues CSS**.

**Die Arbeitsregel des Pakets.** Der Raster umschließt den **Feldlauf**. Eine
`Herleitungszeile`, eine `Kohaerenzzeile`, ein `Warnbanner` oder ein Knopf am **Ende** des
Blocks bleibt außerhalb — sie sind Sätze, keine Felder, und fielen in einer Rasterzelle auf
halbe Breite. Steht so eine Zeile **mitten** im Feldlauf, wird der Raster dort **geteilt**;
zwei Raster derselben Breite tragen dieselbe Beschriftungsspalte, die Kante bleibt also
durchgehend. `Einspaltig` bekommt, wer eine Reihenfolge trägt oder ein breites Pfadfeld führt.

**Umgestellt und bewusst nicht umgestellt.** Die Welle bringt **Seiten**, keine Dialoge —
und Seiten tragen ihre Felder überwiegend in Werkzeugzeilen und Tabellenspalten, nicht in
Parameterblöcken. Umgestellt ist deshalb genau eine Stelle.

| Datei | Felder | Raster | Gruppen | Einspaltig | Klasse‑B‑Entscheid |
|---|---|---|---|---|---|
| `Seiten/Berichte/BerichtSeite.razor` | 2 | 1 | – | **ja** | **B, geteilt.** Das freistehende Pfadfeld des Zielordners ist ein Formularfeld → Raster, einspaltig (ein Pfad braucht die Breite). Der Varianten‑Schalter steht in einer **Tabellenspalte**, die Ausgabewahl in einer `epos-seite-zeile` — beide bleiben |
| `Seiten/Berichte/UebersichtSeite.razor` | 4 | – | – | – | **B, bleibt.** Stamm‑, Filter‑, Varianten‑ und Bezeichnerfeld stehen in zwei `epos-seite-zeile` **neben** den Knöpfen „Anlegen / Löschen / Öffnen". Das ist eine Werkzeugzeile; ein Raster brächte die Knöpfe aus der Zeile |
| `Seiten/Berichte/WirtschaftlichkeitSeite.razor` | 2 | – | – | – | **B, bleibt.** Ein Schalter in einer Tabellenspalte, eine Szenariowahl in der Werkzeugzeile — kein Parameterblock |

Damit ist auch die Gegenprobe gezogen, die die Regel überhaupt erst trägt: Der Raster greift
**nur** innerhalb von `.epos-formularraster`. Die Felder der beiden nicht umgestellten Seiten
sehen unverändert aus, obwohl im selben Programm zwölf Blöcke umgezogen sind.

**Nachweis.** `EPOS.UI.Tests` **2 562** grün (2 546 + 16 neue Fälle des Pakets), unter `de-DE`
**und** `LANG=en_US.UTF-8`; `Werkzeuge/Formularkarte` **122** grün. `FormularrasterTests`,
`StilblattTests`, `ParametersatzTests`, `KatalograhmenTests` und `KatalogdialogTests`
unverändert grün — das Stilblatt ist nicht angefasst worden, die Klammerbilanz also
unberührt.


## Windows-Abnahme 05.09.2026 — Übersicht: nur verwendete Erzeugerkomponenten (W5‑E‑2)

> **Wortlaut des Anwenders:** „Gewerk Anlage gibt es nicht. Dort stehen Parameter.
> Dargestellt werden nur die Erzeugerkomponenten, die verwendet werden, keine Parameter
> (wie unter WinForms)."

Gemeint ist die Tabelle **„Komponenten im Vergleich — Stammprojekt und Varianten"** der Seite
**Berichte & Kosten → Übersicht** (`EPOS.UI/Seiten/Berichte/UebersichtSeite.razor`), und zwar
die **Gegenüberstellung** — die Ansicht, die steht, wenn im Auswahlfeld der Stamm gewählt ist.
Das Bildschirmfoto zeigt sie für Projekt **1042 „Booster-Kette mit Kombi-Speicher"** mit seiner
Variante **1044 „Schichtspeicher"**: vier Spalten (Gewerk · Merkmal · Stamm · Schichtspeicher),
beginnend mit dem Gewerk „Anlage" und darunter Betriebsart, Vorlauf 45 °C, Rücklauf 35 °C,
bivalenter Betrieb, Abschaltpunkt, Heizstab, Grenzleistung, PV-Leistung, Neigung, Azimut,
Kollektormodulanzahl, Solaranteil, Speichervolumen (Anlage) …

### 1. Das Vorbild — nachgesehen, nicht geraten

| Fundstelle | Was dort steht |
|---|---|
| `Views/BerichteKosten/UcBkUebersicht.cs` im Stand vor der Löschung (Commit `ff4e6f7`, Vorgängerfassung, 1 604 Z.), Methode `FuelleVergleich` ab Z. 1 025 | Die WinForms-Maske lief über `GewerkeInReihenfolge()` — die Gewerke in der Reihenfolge von `AbweichungsErmittler.Felder`, also **einschließlich „Anlage" und „Gebäude"**. Der Anlagenblock war nur durch den Artefakt-Guard `AnlagenEinheitlich(versionen)` gedeckelt (Nutzerbefund 28.08.2026, Commit `a533b20`): Er entfiel, wenn die Versionen VERSCHIEDENE Anlagengewerke führen. Bei 1042/1044 führen beide dasselbe → er stand da. **Die Maske hat den Block also gezeigt**; die Erinnerung „wie unter WinForms" trifft auf diese Maske nicht zu |
| dieselbe Datei, Z. 1 036 f. | Der Quelltext benennt den Sachverhalt selbst: *„«Anlage» und «Gebäude» sind Konfigurationsblöcke ohne Komponentenbestand; nur die echten Gewerke der `GewerkTabellen` führen eine Stückzahl."* |
| ältere Fassung derselben Maske (Commit `3ffb179`), `ZeigeStammKomponenten` ab Z. 665 | Die **erste** Fassung (Gewerk · Merkmal · Wert, nur der Stamm) zeigte den Anlagenblock ebenfalls — der Befund ist so alt wie die Maske |
| `Views/Varianten/Form_Variantentest.cs` im Stand vor der Stilllegung (Commit `16b106a`, Vorgängerfassung, 473 Z.) | Der mit iU9‑W0 stillgelegte Altdialog „Projektvarianten" führte **überhaupt keine** Komponententabelle (nur Projektliste, Simulationslauf, Wirtschaftlichkeit, Bericht) — er kommt als Vorbild nicht in Frage |
| **`EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineProjekt.cs:218` und `:237`** | **Das Vorbild, das es wirklich gibt.** Der Berichtsbaustein „Komponentenübersicht" zählt seit jeher **allein über `ProjektDetails.GewerkTabellen`** — Wärmepumpe, BHKW, Spitzenkessel, Solarthermie, Photovoltaik, Pufferspeicher, Stromspeicher. Kein „Anlage", kein „Gebäude". Die Kenndatentabellen darunter überspringen ein Gewerk, das keine Version führt (`HatGewerk`). Dieselbe Zählung nimmt `ExcelBerichtGenerator.cs:239` |

**Der Befund gilt trotzdem, und er ist ein Fachbefund, kein Erinnerungsfehler.** Die
Gegenüberstellung heißt „**Komponenten** im Vergleich"; ein Block ohne Komponentenbestand
gehört nicht hinein. Gemessen am Projekt des Bildschirmfotos standen dort **21 belegte
Anlagenmerkmale und 4 Gebäudemerkmale** — 25 Parameterzeilen über 10 Komponentenzeilen
(Gegenprobe, siehe § 4).

### 2. Umsetzung

**Die Zeilenbildung ist in den Kern gezogen**, weil sie eine Fachaussage ist und beide
Schalen sie brauchen (iOS hätte sie sonst ein zweites Mal):

- **Neu: `EPOS.Kern/Allgemein/Bericht/KomponentenVergleich.cs`** mit dem anzeigefreien
  Zeilentyp `KomponentenVergleichZeile` (Gewerk · Merkmal · Zellen · Kurztexte) und
  `Gegenueberstellung(versionen, kurztextTrenner)`. Sie läuft über
  **`ProjektDetails.GewerkTabellen`** statt über `AbweichungsErmittler.Felder`, überspringt
  jedes Gewerk mit Stückzahl 0 in **allen** Versionen und liefert je verwendetem Gewerk eine
  Kopfzeile „Anzahl Komponenten" und darunter eine Zeile je Komponente mit ihrem Bezeichner;
  die Merkmale der Komponente stehen wie bisher im Kurztext der Zelle.
- **`WindowsFormsApplication1/Views/BerichteKosten/UebersichtSeiteGaben.cs`**: `FuelleVergleich`
  schrumpft von 88 auf 10 Zeilen und bildet nur noch die Kernzeilen auf `VergleichZeile` ab.
  Ersatzlos gefallen sind der Zweig für die nicht zählbaren Gewerke, der Artefakt-Guard
  `AnlagenEinheitlich` (er hatte hier keinen Gegenstand mehr), `GewerkeInReihenfolge()` und
  die Konstante `OHNE_WERT` (sie steht jetzt im Kern).
- **Die Merkmalzeilen entfallen NUR in dieser einen Ansicht.** `AbweichungsErmittler.Felder`,
  `AbweichungsErmittler.Vergleiche` und `AnlagenEinheitlich`/`AnlagenVergleichbar` sind
  **unverändert**: Die Unterschiedsansicht einer Variante zeigt weiter jede geänderte
  Betriebsart, Temperatur, Neigung … — dort ist eine Zeile eine **Änderung** und trägt den
  Übernahmeknopf (`MerkmalUebernahmeCtrl`). Ebenso unverändert bleibt der Bericht.
- **Ein Text ist nachgezogen** (`Resource.resx` / `Resource.en-US.resx`):
  `BK_MSG_VERGLEICH_UMFANG` heißt jetzt „**{0} Komponentenzeile(n)** für das Stammprojekt und
  {1} Variante(n)." (en: „{0} component row(s) …") statt „{0} Merkmalszeile(n) …" — gezählt
  werden keine Merkmale mehr. Kein neuer Schlüssel, kein toter Schlüssel; `Resource.Designer.cs`
  ist unberührt, weil sich nur der WERT geändert hat.
- **Der Titel bleibt** `BK_LBL_KOMPONENTEN_VERGLEICH` = „Komponenten im Vergleich —
  Stammprojekt und Varianten": Er stimmt jetzt erst recht.
- **Kein CSS, kein SQL.** Die Tabelle, ihre Spalten, die Kappung auf acht Variantenspalten
  (`MAX_VARIANTENSPALTEN`) und der Rahmen der Hausregel W9‑B‑2 sind unangetastet.

### 3. Was der Anwender jetzt sieht (Projekt 1042 mit Variante „Schichtspeicher")

| Gewerk | Merkmal | Stamm | Schichtspeicher |
|---|---|---|---|
| **Wärmepumpe** | Anzahl Komponenten | 2 | 2 |
| | Komponente 1 | CS6800iAW MB + AW 10 OR-T | CS6800iAW MB + AW 10 OR-T |
| | Komponente 2 | CS7800iLW 16 | CS7800iLW 16 |
| **Spitzenkessel** | Anzahl Komponenten | 1 | 1 |
| | Komponente | ecoTEC plus VC 1206/5-5 | ecoTEC plus VC 1206/5-5 |
| **Pufferspeicher** | Anzahl Komponenten | 4 | 4 |
| | Komponente 1 … 4 | Puffer 3000Ltr … | Puffer 3000Ltr … |

**Zehn Zeilen statt fünfunddreißig.** BHKW, Solarthermie, Photovoltaik und Stromspeicher
erscheinen gar nicht — das Projekt führt sie nicht.

### 4. Nachweise

- **`EPOS.Kern.Tests/KomponentenVergleichTests.cs` (neu, 7 Fälle)** — gegen die
  **Testdatenbank** (`Referenzlaeufe/Kenndaten_Test.sqlite`, Arbeitskopie über die Vorrichtung
  `TestDatenbank`) und gegen synthetische Bestände:
  **V1** keine Zeile trägt „Anlage"/„Gebäude" oder eines der vierzehn Anlagenmerkmale ·
  **V2** die Gewerkspalte führt genau `Wärmepumpe, Spitzenkessel, Pufferspeicher` in der
  Reihenfolge der `GewerkTabellen` · **V3** zehn Zeilen, Kopfzeile mit Stückzahl, je Version
  eine Zelle, die Bezeichner der verbauten Geräte · **V4** der Kurztext nennt die Merkmale
  der Komponente ohne ihren Bezeichner · **V5** ein Gewerk, das NUR die Variante führt,
  erscheint mit „nicht vorhanden" beim Stamm und dem Strich in der Komponentenzeile ·
  **V6** ohne Erzeugerkomponenten bleibt die Tabelle leer (leere Liste, `null`, leeres
  Projekt) · **V7** die Beschriftung „Komponente"/„Komponente n" kommt aus dem
  Ressourcenkatalog und wechselt mit der Sprache (de/en).
- **Gegenprobe zum Befund** (einmalig gefahren, nicht eingecheckt): Für 1042/1044 ist
  `AnlagenEinheitlich` **wahr**, es sind **21** Anlagen- und **4** Gebäudemerkmale belegt —
  die Änderung entfernt also wirklich die 25 Zeilen des Bildschirmfotos und nicht etwas
  anderes.
- **`EPOS.UI.Tests/Seiten/UebersichtSeiteTests.cs`**, neuer Fall
  `W5E2_Die_Gegenueberstellung_zeigt_nur_Erzeugerkomponenten`: Die SEITE zeigt die sechs
  Zeilen des Stands, die Gewerkspalte führt die drei Erzeugergewerke, kein Anlagenparameter
  steht im Tabellentext, und die Gegenüberstellung trägt vier Spalten **ohne** Aktionsspalte.
- `dotnet build WindowsFormsApplication1 -c Release` → **0 Fehler, 6 Warnungen** (die
  bekannten: 2 × CS0108, 2 × CS0109, WFO0003, CA2255) — unverändert zum Ausgangsstand.
- `dotnet test EPOS.Kern.Tests -c Release` → **1 197** grün (1 190 auf dem Zweigstand,
  dazu die sieben neuen Fälle), unter
  `LANG=de_DE.UTF-8` **und** `LANG=en_US.UTF-8`.
- `dotnet test EPOS.UI.Tests -c Release` → **2 649** grün (2 648 auf dem Zweigstand, dazu
  der neue Fall), beide Kulturen.
- `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite`
  → **1 207 SQL-Texte, 0 Fundstellen**.
- Die zwei Kern-Wächter (`Program.*` und Plattform) melden **nichts**.
- **Referenzlauf unberührt**: Der Rechenweg ist nicht angefasst; geändert ist eine
  Anzeigetabelle und ein Anzeigetext.

**Grenze des Nachweises.** Geprüft auf Linux, ohne Oberfläche. Wie die Tabelle in WebView2
aussieht — insbesondere, ob die gewonnene Höhe die Pufferspeicherzeilen ohne Rollen zeigt —,
sieht der Anwender am Gerät.

### 5. Abnahmepunkte A‑W5‑E‑2

| # | Was der Anwender sehen muss |
|---|---|
| **A‑W5‑E‑2‑1** | Berichte & Kosten → Übersicht, Projekt „Booster-Kette mit Kombi-Speicher", im Auswahlfeld **den Stamm** wählen: Die Tabelle beginnt mit dem Gewerk **„Wärmepumpe"**. Ein Gewerk „Anlage" gibt es **nicht mehr**. |
| **A‑W5‑E‑2‑2** | Keine Parameterzeile mehr: Betriebsart, Vorlauf-/Rücklauftemperatur, bivalenter Betrieb, Abschaltpunkt, Heizstab, Grenzleistung, PV-Leistung, Neigung, Azimut, Kollektormodulanzahl, Solaranteil, Speichervolumen (Anlage) und Wärmequelle stehen dort nicht. Ebenso wenig der Block **„Gebäude"** (Wärmebedarf, Wohn-/Nutzfläche, Warmwasserbedarf, Luftwechselrate). |
| **A‑W5‑E‑2‑3** | Je Gewerk steht eine fette Kopfzeile mit **„Anzahl Komponenten"** und der Stückzahl je Spalte, darunter je Komponente eine Zeile: „Komponente 1", „Komponente 2" … Führt ein Gewerk genau eine, heißt die Zeile nur **„Komponente"**. |
| **A‑W5‑E‑2‑4** | **Nicht verwendete Gewerke fehlen ganz.** Im Beispielprojekt erscheinen nur Wärmepumpe (2), Spitzenkessel (1) und Pufferspeicher (4) — kein BHKW, keine Solarthermie, keine Photovoltaik, kein Stromspeicher. |
| **A‑W5‑E‑2‑5** | Der **Mauszeiger auf einer Komponentenzelle** zeigt weiter deren Merkmale (Hersteller, Typ, Nennleistung …) als Kurztext — das ist der Platz, an dem die Einzelwerte jetzt stehen. |
| **A‑W5‑E‑2‑6** | Die Statuszeile unter der Tabelle nennt jetzt **„… Komponentenzeile(n) für das Stammprojekt und n Variante(n)."** (vorher „Merkmalszeile(n)"). |
| **A‑W5‑E‑2‑7** | **Gegenprobe — die Unterschiede bleiben vollständig.** Im Auswahlfeld die Variante „Schichtspeicher" wählen: Dort erscheinen weiterhin ALLE Abweichungen einschließlich der Anlagen- und Gebäudeparameter, samt Spalte „Aktion" und dem Knopf „Übernehmen aus…". Nur die Gegenüberstellung ist gekürzt worden, nicht der Vergleich. |
| **A‑W5‑E‑2‑8** | Der Bericht ist unverändert: „Varianten und Bericht…" → Bericht erzeugen — die Komponententabellen und die Kenndaten je Gewerk sehen aus wie vorher. |

### 6. Offener Punkt

**W5‑E‑2‑O‑1 — soll auch die Unterschiedsansicht die Parameter weglassen?** Sie ist hier
bewusst unangetastet geblieben: Dort zeigt eine Zeile eine tatsächliche ÄNDERUNG zwischen
Stamm und Variante, und genau an dieser Zeile hängt die Merkmalsübernahme
(`MerkmalUebernahmeCtrl`, Stufe 3). Ein Wegfall der Parameter dort nähme dem Anwender die
Möglichkeit, eine geänderte Vorlauftemperatur zurückzuholen. Der Befund vom 05.09.2026 betraf
das Bild der Gegenüberstellung; wenn der Anwender die Unterschiedsansicht ebenfalls auf
Komponenten verkürzt haben will, ist das ein eigener Entscheid mit eigener Folge für die
Übernahme.

---

## Windows-Abnahme 08.09.2026 — W5‑B‑3 Umbenennen in der Übersicht, W5‑B‑4 doppelte Titel

**W5‑B‑3 (mit W16b‑B‑3).** In der Variantenzeile der Übersicht steht als vierter Knopf
**„Umbenennen"**: Der Bezeichner im Feld wird der neue Name der gewählten Variante
(`UebersichtSeite.VarianteUmbenennen`, Gaben in `UebersichtSeiteGaben.VarianteUmbenennen`,
gemeinsamer Weg `StartseiteHuelle.ProjektUmbenennen` → `VariantenCtrl.Umbenennen`, danach
`VerwirfDetails()` und die Anzeige der Startseite). Frei unter derselben Bedingung wie Löschen
(eine Variante ist gewählt). Der Knopf steht HINTER Anlegen, Löschen, Simulieren, damit deren
Reihenfolge für Anwender und Proben bleibt. Nachweis: `UebersichtSeiteTests` **+1**
(Bezeichner geht durch, Meldung kommt zurück).

**W5‑B‑4 — „Übersicht — Stammprojekt und Varianten" stand zweimal untereinander** (Bildschirmfoto:
einmal im Rahmen mit „· Beispiel WP WG 1", einmal als Seitenkopf mit Hilfeknopf). Der Rahmen
`BerichteKostenSeite` trägt jetzt allein Titel und Hilfeknopf, und zwar den **Schlüssel der
gezeigten Seite** (`HilfeJeSeite`: UEBERSICHT → `UcBkUebersicht.btn_Help`, KOSTEN →
`UcBkKosten.btn_Help`, WIRTSCHAFT → `UcWirtschaftlichkeit.btn_Help`, BERICHT →
`UcBericht.btn_Help`). Die Seitenköpfe von Übersicht, Wirtschaftlichkeit und Bericht sind weg;
die Kostenseite behält ihre Projektzeile („Projekt: …" — sie nennt das Projekt, nicht den
Stamm) ohne zweiten Hilfeknopf. `BerichtSeiteTests` prüft den Titel jetzt am Parameter und
das Fehlen des zweiten Kopfs. Sandbox, Kern- und UI-Tests: s. W6‑B‑7 (**2 046/2 046 (+13: StrangAuslegungTests 9, StrangPlausibilitaetTests 3, ProjektpflegeTests 1)**, **3 248/3 248 (+4: StartseiteTests 3, UebersichtSeiteTests 1; BerichtSeiteTests angepasst)**).

**Offen — Rückfrage an den Anwender (W5‑B‑5):** „Im Bereich Berichte & Kosten sollen die
Varianten mit den Vergleichen/Daten jeweils angezeigt werden (wie auch schon in der Version
branch version_august_2026)." Im August stand in der Übersicht die **Versionstabelle** (Art,
Bezeichner, Projektname, Simulationsstand je Zeile), die W5‑E‑1 am 05.09.2026 auf Anwenderwunsch
(„als Dropdown, damit weniger Platz verwendet wird") durch das Auswahlfeld ersetzt hat; die
Unterschiedstabelle zeigt seither die Abweichungen der GEWÄHLTEN Variante. Zu klären: (a) die
Versionstabelle zurück (Übersicht aller Varianten mit Simulationsstand, zusätzlich zum
Auswahlfeld), (b) die Unterschiede ALLER Varianten nebeneinander (eine Spalte je Variante), oder
(c) die Wirtschaftlichkeits-/Kostenwerte je Variante in der Übersicht. Bis zur Antwort bleibt
W5‑E‑1 stehen.

---

## Anwenderwunsch 08.09.2026 — W5‑B‑5 umgesetzt: EINE Vergleichswahl für Übersicht, Kosten und Wirtschaftlichkeit

**Antwort des Anwenders auf die Rückfrage oben:** „Es geht dabei nur um die Darstellung in der
Übersicht, Kosten, Wirtschaftlichkeit (Beispiel Wirtschaftlichkeit → sollten alle ausgewählten
Varianten im Vergleich stehen). Es soll ausgewählt werden können, welche Varianten dargestellt
werden."

### 1. Befund (Bildschirmfoto Wirtschaftlichkeit: Spalten „Stamm" und „Erdwärme", „Andere WP" fehlt)

Die Kennzahltabelle der Wirtschaftlichkeit baute ihre Spalten aus den GESPEICHERTEN Ergebnissen
(`LadeErgebnisse` über alle Gruppenmitglieder) — nicht aus den Haken der Liste darüber. Die Haken
wirkten erst beim nächsten „Berechnen". Eine Variante ohne gespeichertes Ergebnis („Andere WP",
nie gerechnet) fehlte deshalb stumm; die Übersicht stellte immer ALLE Varianten gegenüber, und
die Kostenseite zeigte genau ein Projekt. Im August (`UcWirtschaftlichkeit`) gab es die Liste mit
Haken nur auf der Wirtschaftlichkeitsseite (Stamm fest, Vorgabe alle).

### 2. Was jetzt gilt

**EINE Vergleichswahl für die drei Seiten** — `Vergleichsauswahl` (Kern,
`EPOS.Kern/Allgemein/Bericht/Vergleichsauswahl.cs`), gehalten in der Rahmenhülle
`BerichteKostenHuelle` und an die Gaben von Übersicht, Kosten und Wirtschaftlichkeit gereicht.
Gemerkt wird das ABGEWÄHLTE (Vorbild `AktualisiereListe`): Vorgabe alle Versionen, der Stamm
immer, eine neue Variante ist von selbst dabei, ein Stammwechsel braucht kein Zurücksetzen.

| Seite | Bedienelement | Wirkung |
|---|---|---|
| **Übersicht** | neue Zeile „Im Vergleich:" (Baustein `Vergleichswahl`: je Version ein Schalter, der Stamm gesetzt und gesperrt mit Werkzeugtipp) unter der Simulationszeile | die Gegenüberstellung „Komponenten im Vergleich" führt nur noch die gewählten Varianten als Spalten (`UebersichtStand.GewaehlteVarianten`) |
| **Kosten** | dieselbe Zeile „Im Vergleich:" und darunter NEU **„Kosten im Vergleich — Stammprojekt und Varianten"**: Investition [€], Betrieb [€/a], Energie [€/a] je gewählter Version (`KostenStand.Versionen/GewaehlteVarianten/VergleichSpalten/Vergleich`, Lesung `Kostenwerte` = dieselbe Leselogik wie die drei Karten) | Karten und Detailtabellen bleiben dem Projekt der Projektzeile; die Rahmenhülle reicht der Kostenseite die Gruppe (`SetzeGruppe`) |
| **Wirtschaftlichkeit** | die vorhandenen Haken der Liste | wirken SOFORT auf Kacheln und Tabelle (`VergleichGewaehlt` → `Anzeigen`); die Tabelle führt **eine Spalte je gewählter Version, auch ohne Ergebnis** („—" und Hinweiszeile „⚠ nicht berechnet — bitte „Berechnen“"); die Statuszeile nennt die Zahl der gewählten Versionen ohne gespeichertes Ergebnis; geladen werden die Ergebnisse aller Gruppenmitglieder, die Wahl filtert in `Ansicht()` |

Der Bericht behält seine eigene, persistierte Auswahl (`Berichtskonfiguration.VariantenIds`) —
er ist nicht Teil des Wunsches.

### 3. Was wo liegt

| Schicht | Datei | Änderung |
|---|---|---|
| Kern | `EPOS.Kern/Allgemein/Bericht/Vergleichsauswahl.cs` | neu: `IstGewaehlt`, `Gewaehlte(gruppe, idStamm)`, `Setzen(gewaehlt, gruppe, idStamm)`, `Geaendert` |
| Baustein | `EPOS.UI/Bausteine/Vergleichswahl.razor`, Stil `.epos-vergleichswahl*` | neu |
| Übersicht | `UebersichtDaten.cs`, `UebersichtSeite.razor`, `UebersichtSeiteGaben.cs` | `GewaehlteVarianten`, Parameter `VergleichGewaehlt`, Filter in `Gegenueberstellung` |
| Kosten | `KostenDaten.cs`, `KostenSeite.razor`, `KostenSeiteGaben.cs` | Versionen, Gegenüberstellung, `Kostenwerte`, `SetzeGruppe` |
| Wirtschaftlichkeit | `WirtschaftlichkeitSeite.razor`, `WirtschaftlichkeitSeiteGaben.cs` | `VergleichGewaehlt`, Spalten je gewählter Version, fehlende Ergebnisse sichtbar |
| Rahmen | `BerichteKostenHuelle.cs` | die eine `Vergleichsauswahl` für alle drei Gaben |
| Texte | `Resource.resx`/`.en-US.resx`/Designer | `BK_LBL_VERGLEICHSWAHL`, `BK_KOSTEN_LBL_VERGLEICH`, `WIRT_STATUS_FEHLEND`, `WIRT_MSG_NICHT_BERECHNET` |

### 4. Nachweise

| Was | Wo | Ergebnis |
|---|---|---|
| Vergleichsauswahl: Vorgabe alle, Stamm immer, Abgewähltes bleibt, Neues dabei, Ereignis nur bei echter Änderung, andere Gruppen unberührt | `EPOS.Kern.Tests/VergleichsauswahlTests.cs` | **5** neue Fälle |
| Baustein: je Version ein Schalter, Stamm gesetzt und gesperrt, Ab-/Anwahl meldet die vollständige Liste in Reihenfolge, Werkzeugtipp, `Aktiv=false` | `EPOS.UI.Tests/Bausteine/VergleichswahlTests.cs` | **4** neue Fälle |
| Übersicht: ohne Delegat keine Zeile; Abwahl meldet `[1030]` und liest den Stand neu | `UebersichtSeiteTests` | **2** neue Fälle |
| Kosten: Gegenüberstellung mit Köpfen/Zeilen, Abwahl meldet und liest neu, ohne Gruppe kein Block | `KostenSeiteTests` | **3** neue Fälle |
| Wirtschaftlichkeit: der Haken meldet die Wahl und zeigt die Tabelle neu | `WirtschaftlichkeitSeiteTests` | **1** neuer Fall |
| Sandbox-Bau `WP-Plan.sln` x64 Debug | `K:\imp\src` | **0 Fehler** |
| `EPOS.Kern.Tests` / `EPOS.UI.Tests` | `dotnet test --no-build` | **2 051/2 051** / **3 258/3 258** |
| Referenzlauf | — | unberührt (Anzeige und Auswahl, kein Rechenweg) |

### 5. Abnahmepunkte — A‑W5‑B‑5

1. Übersicht, Stamm „Beispiel WP WG 1" gewählt: Zeile „Im Vergleich: ☑ Stamm ☑ Andere WP ☑ Erdwärme"; „Erdwärme" abhaken → die Spalte verschwindet aus „Komponenten im Vergleich".
2. Auf „Kosten" wechseln: dieselbe Zeile zeigt „Erdwärme" abgewählt; „Kosten im Vergleich" führt Stamm und „Andere WP" mit Investition/Betrieb/Energie.
3. Auf „Wirtschaftlichkeit" wechseln: der Haken von „Erdwärme" ist aus; „Andere WP" steht als Spalte mit „—" und „⚠ nicht berechnet — bitte „Berechnen“", die Statuszeile nennt „Für 1 gewählte Version(en) liegt kein gespeichertes Ergebnis vor". „Erdwärme" wieder anhaken → die Spalte erscheint sofort, ohne Rechenlauf.
4. „Berechnen" rechnet die gehakten Varianten; danach sind alle gewählten Spalten gefüllt.

---

## Windows-Abnahme 08.09.2026 — W5‑B‑6: „das Löschen der gelb hinterlegten Anlage ohne Zuordnung funktioniert nicht"

**Befund an der produktiven Datenbank (Projekt 1026, Wärmepumpe):** vier verwaiste Positionen
(`ID_Anlage` NULL, Geräteanker 1672017 = die getauschte Wärmepumpe CS6800iAW), drei davon
**Pflichtpositionen** (Betrieb, H3). Der Anwender hat die Wärmepumpe getauscht (jetzt CS3400i,
Anlagenzeile 14987); der Del+Add-Speicherweg legte die Zeile neu an, die Heilung über den
Geräteanker fand kein Ziel (anderes Gerät), `PflichtpositionenSicherstellen` versorgte die neue
Anlage mit eigenen Pflichtzeilen — und die alten blieben als gelbe Zeile „Wärmepumpe — ohne
Anlagenzuordnung" stehen. `LoseLoeschen` rief `Loeschen(id)`, das Pflichtzeilen SCHÜTZT:
0 von 3 gelöscht, die Fußzeile sagte es nur leise.

**Änderung:** `KostenProjektPositionenCtrl.LoseLoeschen` löscht verwaiste Zeilen ohne den
Pflichtschutz (`VerwaisteLoeschen`) — `Lies(…, 0)` liefert nur Zeilen ohne (gültige) Anlage,
der Schutz der lebenden Anlagen bleibt. Nachweis: `KostenProjektPositionenCtrlTests` (neu, 2:
alle verwaisten Wärmepumpen-Positionen von 1026 gehen weg; eine Pflichtzeile einer lebenden
Anlage bleibt geschützt). Sandbox: Kern **2062/2062**, UI **3278/3278**.

---

## Anwenderbefund 08.09.2026 — W5‑B‑7: Investitionskaskade in Dialog und Kostenseite

**Wortlaut des Anwenders:** „% der Investitionskosten ist immer 0. Investitionskosten müssten
berechnet werden aus allen nicht-%-Anteilen. — Wie werden die Kosten aus dem Dialog
Kostenverwaltung (Photovoltaik) in die Kostenberechnung genommen? Sollten auf der Seite Kosten
nicht die Kosten aus dem Kostendialog stehen?"

### 1. Befund — DREI Lesewege für dieselben Zeilen

Dieselben Investitionspositionen (`Tab_ProjektWerte`, `KategorieID = 1`) wurden an drei Stellen
auf drei Arten gelesen. Für die Photovoltaik-Anlage des Anwenderprojekts kamen deshalb drei
verschiedene Zahlen heraus:

| Stelle | Rechenweg | PV-Anlage des Anwenders |
|---|---|---|
| Kapitalwertrechnung (`WirtschaftlichkeitCtrl.LiesInvestitionen`) | volle ETAPPE‑H4b‑Kaskade nach Konzept Kostendialoge § 5.3 | **6.961,80 €** (5.660 + 3 % + 10 % + 10 %) — richtig |
| Dialog Kostenverwaltung (`KostenProjektPositionenCtrl.Lies`, Summenfuß `KostenKomponenteHuelle`) | je Zeile nur die **gespeicherte** `Menge` | 0,00 € in jeder Prozentzeile, Summenfuß **5.660,00 €** |
| Seite „Berichte & Kosten", Spalte Investition je Anlage (`KostenSummenCtrl`) | rohes `SUM(EingegebenerWert)` | **1.500,00 €** (nur der feste Wechselrichterbetrag) |

**Warum der Dialog 0 zeigte.** Für „% der Investition" wird in Kategorie 1 **nie** eine Menge
gespeichert: `WirtschaftlichkeitCtrl.MengeAusweisen` schließt genau diese Kombination
ausdrücklich aus (die Basis ist Kaskadenmaterie der Runde 3, keine Einzelzeilen-Größe).
`BetriebskostenCtrl.Betrag` fällt ohne Menge nach Anwenderentscheid I‑2 auf den *erfassten* Wert
zurück — und der ist bei einer Prozentzeile 0. Der Dialog rechnete also nicht falsch, er las nur
etwas anderes als der Rechenkern.

**Warum die Kostenseite noch weniger zeigte.** `LiesAnlagenSummen` / `LiesKomponentenSummen` /
`AnlagenSumme` summierten die Spalte `EingegebenerWert` roh — also ohne Menge × Satz und ohne
jede Prozentzeile. Betroffen war nicht nur die Anlagentabelle der Kostenseite, sondern auch die
Kostenzeile der Wärmepumpenmaske („Invest … €"), die Photovoltaik-Vergütung und der
Rückfallweg der Kostenseite.

### 2. Änderung — EIN Rechenweg

1. **`EPOS.Kern/Controller/InvestKaskade.cs` (neu).** Die H4b-Kaskade ist Wort für Wort aus
   `LiesInvestitionen` **herausgelöst** (nicht nachgebaut): Runde 1 direkte Zeilen, Runde 2
   „% der Erzeugerkosten", Runde 3 „% der Investition" mit eingefrorenen Basiszeilen
   (Anwenderentscheid I‑3). Rückgabe ist die Zeilenliste je `Tab_ProjektWerte.ID` mit Betrag,
   **Basis**, Zuschuss-Kennzeichen, Komponente, Anlage, Nutzungsdauer und Startjahr;
   `NachId(...)` und `Summen(...)` sind die beiden Sichten für Dialog und Kostenseite.
   Die Basis ist neu und reine Auskunft: `InvestBetrag` gibt die tatsächlich angesetzte
   Bezugsgröße heraus, die Rechnung selbst ist unverändert.
2. **`LiesInvestitionen`** ist nur noch Verbraucher — geblieben ist der K5-Zuschussabzug und die
   Übersetzung in `KapitalwertRechner.InvestPosition`. Die geteilten Zeilenleser
   (`Szenariowert`, `StartJahrDerZeile`, `IstZuschuss`, `KomponenteUndAnlage`, `IstProzentInvest`,
   `D`/`B`/`Text`, die beiden Spaltenproben) sind dafür von `private` auf `internal` gestellt;
   `InvestZeile`, `IstProzentErzeuger` und `InvestBetrag` sind mit der Kaskade umgezogen.
3. **Dialog.** `KostenProjektPositionenCtrl.Lies` fragt für Kategorie 1 die Kaskade, für
   Kategorie 2 die Nachweisliste `WirtschaftlichkeitCtrl.BetriebNachId`
   (= `LiesBetriebskostenPositionen`, um `Id`/`Komponente`/`Anlage` erweitert). Die Zeile trägt
   jetzt `Basis`; `Speichern` zieht Betrag und Basis nach dem Schreiben aus demselben Rechenweg
   nach. Der Summenfuß enthält die Prozentzeilen damit von selbst.
4. **Werkzeugtipp.** `KostenKomponenteHuelle.BasisKurztext` nennt im Projektmodus die
   Bezugsgröße: „Aus Satz und Bezugsgröße des Projekts berechnet: 3 % von 5.660,00 €" bzw.
   „… 1,50 €/kW × 30,00 kW" (zwei neue Ressourcenschlüssel `KDLG_TT_BETRAG_BASIS_PROZENT` /
   `KDLG_TT_BETRAG_BASIS_MENGE`, de + en). `Nachziehen` rechnet den Betrag im Projektmodus bei
   jeder Satzänderung sofort mit, statt ihn bis zum Speichern zu leeren.
5. **Kostenseite.** `KostenSummenCtrl.LiesKomponentenSummen` / `LiesAnlagenSummen` /
   `AnlagenSumme` liefern für Kategorie 1 die Kaskadenbeträge, für Kategorie 2 die Beträge des
   Betriebskosten-Auflösers. Zuschusszeilen bleiben wie in `LiesInvestitionen` außen vor, ihre
   GRUPPE aber bestehen — sonst verlöre eine Komponente mit reiner Zuschusszeile ihr
   „hat Positionen" und stünde rot. Auf einer Datenbank ohne die Spalten aus Schritt 19 fällt
   alles auf das alte SQL zurück.

### 3. Nachweis

Die Testdatenbank führt keine Photovoltaik-Kostenzeilen; das Beispiel des Anwenders steht
deshalb an der Wärmepumpe des Projekts 1040 (Anlage 14728): eine direkte Zeile über
**5.660,00 €** und drei Prozentzeilen mit **3 %, 10 %, 10 %**.

| Zeile | vorher (Dialog / Kostenseite) | nachher |
|---|---|---|
| direkte Zeile | 5.660,00 € | 5.660,00 € |
| 3 % der Investition | 0,00 € | **169,80 €** (Basis 5.660,00 €) |
| 10 % der Investition | 0,00 € | **566,00 €** (Basis 5.660,00 €) |
| 10 % der Investition | 0,00 € | **566,00 €** (Basis 5.660,00 €) |
| Summenfuß des Dialogs | 5.660,00 € | **6.961,80 €** |
| `AnlagenSumme` / Anlagenzeile der Kostenseite | 5.660,00 € | **6.961,80 €** |
| Kachel „Investition" (`LiesInvestitionen`) | 6.961,80 € | 6.961,80 € (unverändert) |

Die beiden 10‑%‑Zeilen bekommen denselben Betrag — %-Zeilen zählen einander nach I‑3 nie mit,
das Ergebnis hängt nicht an der Lesereihenfolge (die Abfrage trägt kein `ORDER BY`).

| Prüfung | Ort | Ergebnis |
|---|---|---|
| Kaskadenregeln (Basis × Satz, Rückfall Anlage → Komponente, I‑3, Zuschuss außen vor) | `InvestKaskadeTests` (neu) | **7** Fälle |
| `LiesInvestitionen` vorher/nachher zahlengleich, 16 Projekte × 3 Szenarien | `InvestKaskadeTests.LiesInvestitionen_bleibt_zahlengleich` | **16** Fälle, Vergleichszahlen am 08.09.2026 an der unberührten `Kenndaten_Test.sqlite` gemessen |
| Sandbox-Bau `WP-Plan.sln` x64 Debug | `K:\imp2\src` | **0 Fehler** |
| `EPOS.Kern.Tests` / `EPOS.UI.Tests` | `dotnet test --no-build` | **2 096/2 096** / **3 301/3 301** |
| Referenzlauf | `Referenzlaeufe/2026-09-07_M7_nach-Merge7` | **gibt keine Kosten- oder Wirtschaftlichkeitswerte aus** — 0 von 298 Kennzahlen der `aggregate.csv` tragen kost/invest/kapital/wirt/barwert/annuität/amortisation; er kann zu dieser Änderung nichts beweisen und ist von ihr auch nicht berührt (der Rechenweg der Kapitalwertrechnung ist unverändert) |

### 4. Offene Punkte

- ~~**Kategorie 2, Basis „% der Investition".** `BetriebskostenCtrl.InvestSummeFuer` (H4a) summiert
  weiterhin `SUM(EingegebenerWert)` — die Betriebszeile „x % der Investitionssumme" bemisst sich
  also an der ROHEN Spaltensumme, nicht an der Kaskade. Das ist ein VIERTER Leseweg derselben
  Zahlen und damit derselbe Befund auf der Betriebsseite. Er ist hier bewusst nicht angefasst:
  Er würde die Kapitalwertrechnung selbst verändern (Betriebskosten p. a., Kapitalwert,
  Sensitivität FX5‑a) und gehört deshalb vor eine Anwenderentscheidung.~~ →
  **entschieden und umgesetzt am 09.09.2026, siehe W5‑B‑8 unten.**
- **Sichtabnahme** des Werkzeugtipps und des Summenfußes im Projektmodus (Dialog
  Kostenverwaltung, Reiter Investition und Betrieb).


## Anwenderentscheid 09.09.2026 — W5‑B‑8: Betriebskosten-Basis aus der Kaskade

**Wortlaut des Anwenders:** Die Betriebskosten-Bemessung „x % der Investitionssumme"
(Kategorie 2) rechnet auf die **Kaskade**.

### 1. Der vierte Leseweg — was W5‑B‑7 offen gelassen hatte

W5‑B‑7 machte die H4b-Kaskade (`EPOS.Kern/Controller/InvestKaskade.cs`) zur einen Wahrheit der
Kategorie 1: Kapitalwertrechnung, Dialog Kostenverwaltung und Seite „Berichte & Kosten" lesen
seither dieselben Beträge. `BetriebskostenCtrl.InvestSummeFuer` (H4a) blieb dabei ausdrücklich
unberührt — es war der **vierte** Leseweg derselben Zeilen und hätte die Kapitalwertrechnung
selbst verändert.

Er summierte roh `SUM(EingegebenerWert)` der Kategorie-1-Zeilen. Eine Betriebszeile
„x % der Investitionssumme" (Wartung, Instandhaltung, Versicherung, Verwaltung — im Bestand die
häufigste Kategorie-2-Bemessung) bemaß sich damit an einer Zahl, die

- **satzbasierte Investitionszeilen** (Menge × Satz, z. B. „200 €/kW el × 14,5 kW el") und
- **alle Prozentzeilen der Investseite** („% der Erzeugerkosten", „% der Investition")

gar nicht enthielt. Dieselbe Anlage konnte in der Kachel „Investition" 58.049,84 € zeigen und
ihre Instandhaltung mit 45.312,50 € bemessen.

### 2. Änderung — dieselbe Stufung, vollständige Basis

1. **`EPOS.Kern/Controller/BetriebskostenCtrl.cs`.**
   `InvestSumme(projektID, komponentenID, idAnlage, kaskade)` staffelt jetzt die Summen aus
   `InvestKaskade.Summen(projektID, ERWARTET)` — Anlage → Komponente → Projekt, genau die
   Stufung der Runde 3 der Investseite. `InvestSummeFuer` ist unverändert in Ablauf und
   Reihenfolge; nur die Zahl, auf die es staffelt, ist jetzt die vollständige.
   `LiesBezugsgroessen` (`InvestGesamt`, `InvestBhkw`, `InvestKessel`) liest dieselbe Kaskade.
   Zuschusszeilen bleiben außen vor — **K5 gilt Wort für Wort weiter** („% der Investitionssumme
   rechnet VOR Zuschussabzug"): `InvestKaskade.Summen` trägt eine Zuschusszeile mit Beitrag 0.
2. **Der Rückfall bleibt.** `InvestSummeSql` ist der alte Weg unter neuem Namen und gilt nur
   noch, wenn die Kaskade nichts anzubieten hat — also auf einer Datenbank ohne die Spalten aus
   Schritt 19. Dort rechnet die Kaskade ohnehin Zeile für Zeile `EingegebenerWert` (es gibt weder
   Bemessung noch Satz noch Kostenart); die beiden Wege fallen dann zusammen. Dasselbe Muster wie
   `KostenSummenCtrl.Rechenwegsummen`.
3. **`WirtschaftlichkeitCtrl`.** `RueckfallMenge` führt die Kaskade als Merker der laufenden
   Leseschleife (`ref Dictionary<KeyValuePair<int,int>, double> investSummen`, null = noch nicht
   gelesen) — dasselbe Muster wie der Endenergie-Auflöser. Ohne ihn liefe der ganze Rechenweg der
   Kategorie 1 **je Betriebskostenzeile** erneut. Die drei Aufrufer sind
   `LiesBetriebskostenTopfe` (Summenschleife), `LiesBetriebskostenPositionen` (Nachweisliste, E7)
   und `MengeAusweisen` (Ausweis nach `Tab_ProjektWerte.Menge`).
4. **Kein fünfter Weg.** Nach der Änderung verwendet keine Lesestelle mehr
   `SUM(EingegebenerWert)` als Investitionssumme außer den dokumentierten Rückfällen
   (`BetriebskostenCtrl.InvestSummeSql`, die drei Rückfälle in `KostenSummenCtrl`).

### 3. Was sich dadurch ändert

| Anzeige / Rechnung | vorher | nachher |
|---|---|---|
| Dialog Kostenverwaltung, Reiter Betrieb, Zeile „% der Investition" | Betrag auf der rohen Spaltensumme | Betrag auf der Kaskadensumme |
| Werkzeugtipp / Herleitung derselben Zeile (`Menge`) | rohe Spaltensumme | Kaskadensumme |
| Kachel „Betrieb" und Anlagentabelle der Kostenseite (Kategorie 2) | ” | ” |
| Betriebskosten p. a., Kapitalwert, Annuität, Amortisation | ” | ” |
| Sensitivität „Investition Variante ±10 %" (FX5‑a, investgekoppelter Anteil) | ” | ” |
| Investseite (Kachel, Dialog, Anlagentabelle, I₀, Zuschuss) | — | **unverändert** |

### 4. Nachweis

**Das Beispiel** steht am BHKW des Projekts 1018 (Komponente 7, Anlage 11327) — der einzigen
Anlage der Testdatenbank, die sowohl die Kategorie-1-Kaskade als auch Kategorie-2-Zeilen
„% der Investition" führt. `Tab_BHKW.Pel` = 14,5 kW el liefert die Baugröße der satzbasierten
Zeile, damit nichts geraten ist.

| Kategorie-1-Zeile | Bemessung | Betrag |
|---|---|---|
| BHKW (Hauptposition) | Betrag | 45.312,50 € |
| BHKW-Modul | 200,00 €/kW el × 14,5 kW el | 2.900,00 € |
| MSR-Technik | 5 % der Erzeugerkosten (Runde 2) | 2.265,625 € |
| **Basis der Runde 3** | | **50.478,125 €** |
| Montage und Einbringung | 10 % der Investition | 5.047,8125 € |
| Planung / Baunebenkosten | 5 % der Investition | 2.523,90625 € |
| **Kaskadensumme der Anlage** | | **58.049,84375 €** |

| Kategorie-2-Zeile | vorher | nachher |
|---|---|---|
| „Instandhaltung BHKW", 2 % der Investitionssumme | Basis 45.312,50 € → **906,25 €/a** | Basis 58.049,84375 € → **1.160,996875 €/a** |
| `LiesBetriebskosten(1018)` | 906,25 €/a | **1.160,996875 €/a** |
| `AnlagenSumme(1018, Kategorie 2, 11327)` | 906,25 €/a | **1.160,996875 €/a** |
| investgekoppelter Ausweis (FX5‑a) | 906,25 €/a | **1.160,996875 €/a** |

**Regressionsprobe über alle Projekte der Testdatenbank**
(`Betriebskosten_je_Projekt_vorher_und_nachher`, Ausgabe des Testlaufs):

```
Projekt | Betriebskosten p. a. vorher | nachher | Abweichung
--------|-----------------------------|---------|-----------
   1007 |                        0,00 |    0,00 |      0,00
   1018 |                        0,00 |    0,00 |      0,00
   1019 |                       99,00 |   99,00 |      0,00
   1023 |                       99,00 |   99,00 |      0,00
   1024 |                       99,00 |   99,00 |      0,00
   1026 |                        0,00 |    0,00 |      0,00
   1028 |                        0,00 |    0,00 |      0,00
   1029 |                        0,00 |    0,00 |      0,00
   1030 |                   20.000,00 | 20.000,00 |      0,00
   1031 |                        0,00 |    0,00 |      0,00
   1032 |                        0,00 |    0,00 |      0,00
   1040 |                        0,00 |    0,00 |      0,00
   1041 |                        0,00 |    0,00 |      0,00
   1042 |                        0,00 |    0,00 |      0,00
   1043 |                        0,00 |    0,00 |      0,00
   1044 |                        0,00 |    0,00 |      0,00
```

**Keine Abweichung — und zwar nachweisbar, nicht zufällig.** Keine einzige Kostenzeile der
unberührten `Kenndaten_Test.sqlite` trägt einen Satz: `Einheitpreis` ist in **allen 175**
Kategorie-1/2-Zeilen NULL, `Menge` ebenso. Ohne Satz ist die Ableitung nach Anwenderentscheid
I‑2 gar nicht rechenbar — es gilt der erfasste Wert (0,00 bzw. 99,00 bzw. 20.000,00 €),
unabhängig von der Basis. Die Umstellung wird erst sichtbar, sobald ein Satz gepflegt ist; genau
das zeigen die sechs Fälle oben. **Bestandsprojekte des Anwenders mit gepflegten Sätzen ändern
sich dagegen** — die Änderung ist ausdrücklich akzeptiert.

Wie „vorher" in der Liste entsteht: Nur die Bemessungsart „% der Investition" der Kategorie 2
ändert ihre Bezugsgröße, alles andere ist unberührt. Der Fall bildet die ALTE Regel (rohe
Spaltensumme, stufig, ohne Zuschuss) noch einmal nach, rechnet jede solche Zeile mit beiden Basen
über denselben `BetriebskostenCtrl.Betrag` und zieht die Differenz vom heutigen Ergebnis ab. Das
ist exakt, weil der Betrag linear in der Basis ist.

| Prüfung | Ort | Ergebnis |
|---|---|---|
| Kaskadenbasis (satzbasierte Zeile + Prozentzeilen), Betrag, Jahressumme, Anlagensumme | `BetriebskostenBasisTests` (neu) | **7** Fälle |
| Zuschuss außen vor (K5), Stufung Anlage → Komponente, Mengenausweis | dieselbe Datei | in den 7 enthalten |
| Regressionsliste vorher/nachher, 16 Projekte | `BetriebskostenBasisTests.Betriebskosten_je_Projekt_vorher_und_nachher` | **0,00 Abweichung** je Projekt |
| Sandbox-Bau `WP-Plan.sln` x64 Debug | `K:\imp2\src` | **0 Fehler** |
| `EPOS.Kern.Tests` / `EPOS.UI.Tests` | `dotnet test --no-build` | **2 103/2 103** / **3 301/3 301** |

**Bestehende Erwartungswerte mussten NICHT angepasst werden.** Kein Fall der beiden Testprojekte
prüfte einen Betriebskostenwert, der sich durch die neue Basis verschiebt — die 2 096 Fälle vor
der Änderung laufen unverändert grün, die 7 neuen kommen hinzu.

### 5. Offene Punkte

- **Sichtabnahme** im Dialog Kostenverwaltung, Reiter Betrieb: Betrag und Herleitungstext einer
  Zeile „% der Investitionssumme" an einer Komponente mit satzbasierten oder prozentualen
  Investitionszeilen.
- **Nachbar, bewusst nicht angefasst:** `TechnikPlanwertCtrl` (Kessel-/WP-Wartung, Einheit „%/a")
  bemisst sich auf die **Hauptposition** der Komponente (`KostenPositionCtrl.LiesBetrag` einer
  EINZELNEN Zeile), nicht auf eine Summe. Das ist eine andere Größe mit eigener Herleitungszeile
  im Gerätedialog und keine `SUM(EingegebenerWert)` — eine Umstellung auf die Kaskade wäre eine
  eigene Fachentscheidung.
---

## Anwenderentscheid 09.09.2026 — W5‑B‑9: Szenarioparameter

### 1. Befund

Die Seite „Wirtschaftlichkeit“ bot die drei Szenarien **Erwartet / Best / Worst**
(`WirtschaftlichkeitSzenario`) an und zeigte in allen dreien **dieselben Zahlen**.

Zwei Ursachen:

1. Sie unterschieden sich ausschließlich über die **Zeilenwerte**
   `Tab_ProjektWerte.BestCase`/`WorstCase` (und `…_Nutzungsdauer`) — und die stehen im
   Bestand bei nahezu jeder Position auf 0. `WirtschaftlichkeitCtrl.Szenariowert` fällt
   dann nach dem VALERI-Muster („0/leer = kein Szenariowert gepflegt“) auf den
   Erwartungswert zurück.
2. Es gab **keine Szenarioparameter auf Projektebene** — Zins, Preissteigerungen,
   Investitions- und Ertragsunsicherheit galten für alle drei Szenarien gleich. Genau
   das ist aber die Ebene, auf der DIN EN 17463 (VALERI) die Bandbreite aufspannt.

Der dritte Teil des Befunds — „Energiekosten nicht bestimmbar“ — ist keine Szenariofrage,
sondern ein Datenmangel der Kostenmaske; die Fehlgrundzeile sagt das bereits
(`„Energiekosten nicht bestimmbar — Arbeitspreise/Träger in der Kostenmaske prüfen“`).

**Befund 1029** (`ID_Ergebnis 167` / `IstStamm 1` eines alten Laufs) ist nachgeprüft und
liegt **nicht am Schreibweg**: `RechneProjekt` setzt `IdErgebnis = LiesErgebnisId(v.IdProjekt)`
und `IstStamm = v.IstStamm` je Lauf frisch, `Persistiere` löscht vorher alle Zeilen des
Projekts (`DELETE … WHERE ID_Projekt = ?`) und schreibt sie neu. Eine Neuberechnung heilt
den Stand also vollständig, wie der Anwender beobachtet hat; übrig war ein Datenstand aus
einer früheren Gruppenzuordnung. **Neu ist trotzdem etwas an dieser Stelle:** Bis zu dieser
Etappe schrieb `Persistiere` in alle drei Szenariozeilen dreimal denselben Projektwert für
`Zinssatz`, `Preissteigerung_Energie` und `Preissteigerung_Betrieb`. Mit Szenarioparametern
wäre das eine Annahme, mit der gar nicht gerechnet wurde — seit W5‑B‑9 steht je Zeile der
**wirksame** Satz.

### 2. Der Parametersatz

Sechs Größen je Szenario (`SzenarioSatz`), abgelegt an `Tab_ProjektWirtschaftlichkeit`:

| Größe | Einheit | Wirkung | Vorgabe Best | Vorgabe Worst |
|---|---|---|---|---|
| Kalkulationszins | % | Diskontierung, Annuität, Gestehungskosten | i − 1 %‑Pkt (nie < 0) | i + 1 %‑Pkt |
| Preissteigerung Energie | %/a | p_E: Energie, CO₂, Endenergie-Topf | p_E − 1 %‑Pkt | p_E + 1 %‑Pkt |
| Preissteigerung Betrieb | %/a | p_B: Betriebs-Topf | p_B − 1 %‑Pkt | p_B + 1 %‑Pkt |
| Investitionsänderung | % | Investitionspositionen **ohne** gepflegten Szenariowert | − 10 % | + 10 % |
| Ertragsänderung | % | Einspeiseerlös und PV-Vergütungsreihe | + 10 % | − 10 % |
| Nutzungsdaueränderung | a | Nutzungsdauer **ohne** gepflegten Szenariowert | + 2 a | − 2 a |

**Vorzeichen einheitlich: `+` heißt mehr bzw. länger.**

Drei Regeln tragen die Etappe:

- **`null` heißt Vorgabe, nicht 0.** Ein Feld, das nie gepflegt wurde, zieht bei einer
  geänderten Projektangabe mit (i wechselt von 3 auf 4 % → die Best-Vorgabe folgt auf 3 %).
- **ERWARTET bekommt keinen Satz.** `WirtschaftlichkeitParameter.FuerSzenario` gibt für ihn
  `this` zurück — **dieselbe Referenz**, nicht eine wertgleiche Kopie. Damit ist die
  Zahlengleichheit des Erwartungsfalls eine Eigenschaft des Codes, keine Behauptung.
- **Gepflegte Zeilenwerte haben Vorrang** vor dem pauschalen Ausschlag — sonst zählte er
  doppelt. `InvestKaskade.Zeile` merkt sich seither in `WertGepflegt` und `DauerGepflegt`,
  ob der Szenariowert aus der Best-/Worst-Spalte kam; `LiesInvestitionen` skaliert nur die
  zurückgefallenen Zeilen. Zuschusszeilen (K5) und die gesetzlichen Erlösreihen (KWKG,
  Energie-/Stromsteuer) bleiben grundsätzlich außen vor.

Der **`KapitalwertRechner` ist unverändert**. Zins und Preissteigerungen wirken über den
Szenario-Parametersatz, Investition, Ertrag und Nutzungsdauer in der Eingabe
(`BaueEingabe` → `LiesInvestitionen`, `SkaliereErtraege`).

### 3. Ablage — Migrationsschritt 71

Zwölf **nullbare** `DOUBLE`-Spalten an `Tab_ProjektWirtschaftlichkeit`
(`SchemaKatalog.Schritt71_SzenarioBest` und `…Worst`):

```
Szen_Best_Zins    Szen_Best_Preis_E    Szen_Best_Preis_B
Szen_Best_Invest  Szen_Best_Ertrag     Szen_Best_Dauer
Szen_Worst_Zins   Szen_Worst_Preis_E   Szen_Worst_Preis_B
Szen_Worst_Invest Szen_Worst_Ertrag    Szen_Worst_Dauer
```

**Kein DML, kein DDL-DEFAULT** — dasselbe Muster wie Schritt 70. Der Schritt steht **nicht**
in `SchemaKatalog.Alle`: Kein Rechenkern der Simulation liest eine der Spalten, und die
tolerante Vorsorge steht unmittelbar vor dem Zugriff in
`WirtschaftlichkeitCtrl.StelleTabellenSicher` — wortgleiche Begründung wie bei den
übrigen `Tab_ProjektWirtschaftlichkeit`-Schritten (20, 21, 28). Zielstand **71**.

**Wirkung, ausdrücklich:** Erwartet bleibt zahlengleich; **Best und Worst ändern sich**,
sobald der Schritt gelaufen ist — sie rechnen dann mit den Vorgaben statt mit dem
Erwartungswert. Genau das ist der Zweck des Entscheids. Der Referenzlauf ist nicht berührt
(er rechnet Simulationen, keine Wirtschaftlichkeit).

### 4. Dialog „Parameter…“ — Abschnitt „Szenarien“

Drei Wertspalten × sechs Zeilen, als Tabelle (`epos-raster epos-matrix
epos-raster--bearbeitbar`) statt als Formularraster: Eine Beschriftung gehört hier zu DREI
Werten, und der Vergleich zwischen den Spalten ist der Zweck der Anzeige.

- Die **Erwartet-Spalte ist Anzeige** — sie wiederholt, was oben unter „Allgemein“ gepflegt
  wird („Kein Delegat ist kein Knopf“). Investition, Erträge und Nutzungsdauer stehen dort
  auf 0.
- Die **Felder zeigen den wirksamen Wert**, nicht den gepflegten — ein leeres Feld gäbe es
  sonst für jede Vorgabe, und niemand sähe, womit gerechnet wird.
- Der Knopf **„Vorgaben“** setzt alle zwölf Felder auf `null` zurück. Das ist NICHT dasselbe
  wie „auf die heutigen Vorgabezahlen setzen“.
- Zwei **Herleitungszeilen** nennen je Satz Herkunft („Vorgaben“ / „gepflegte Werte“) und die
  wirksamen Zahlen.

### 5. Seite „Wirtschaftlichkeit“

Die Szenariowahl bleibt. Neu ist die **Statuszeile** unmittelbar darunter
(`ErgebnisAnsicht.Szenariozeile`): Sie nennt für das gewählte Szenario den wirksamen Satz
und seine Herkunft; für Erwartet den Satz „die Projektparameter unverändert“. Sie hängt an
der **Ansicht**, nicht am Stand — der Szenariowechsel tauscht genau dieses Objekt aus, und die
Zeile zieht von selbst mit. Eine leere Zeile wird gar nicht erst gezeichnet.

---

## Anwenderentscheid 09.09.2026 — W5‑B‑10: VALERI-Abgleich (DIN EN 17463)

### 1. Was EPOS-Plan bereits abbildet

| VALERI-Anforderung | In EPOS-Plan |
|---|---|
| Nettobarwert | `KapitalwertRechner.Rechne` — KW = −I₀ + Σ (E_t − A_t)/(1+i)^t + RW_T/(1+i)^T |
| Zahlungsströme je Jahr | `Zahlungsbild.NominalReihe`/`BarwertReihe` plus die Einzelreihen (Betrieb, Endenergie-Anteil, Energie, CO₂, Ersatz, Einspeiseerlös, benannte Erlösreihen) seit E7; Mehrjahrestabelle im Bericht |
| Betrachtungszeitraum T | Projektparameter 1…50 a; der Verlaufsdialog rechnet freie Horizonte |
| Diskontierungszins | Projektparameter, seit W5‑B‑9 je Szenario |
| Restwert am Ende von T | linear je Position, abgezinst |
| Ersatzinvestition bei n < T | in t = n, 2n, … (`ErsatzJeJahr`) |
| Preisentwicklung | p_E und p_B; CO₂-Preispfad und PV-Vergütung jahresscharf |
| Förderungen | Investitionszuschuss (K5, I₀-mindernd), KWKG-Zuschlag und drei Steuergutschriften als jahresscharfe Erlösreihen |
| Sensitivitäten | Zins, Energiepreissteigerung, Investition, Energiekosten, „KWKG-Bonus entfällt“ |
| Bandbreite W/E/B | Zeilenwerte **und** Parametersatz (W5‑B‑9) |
| Weitere Kennzahlen | Annuität, dynamische Amortisation, interner Zinsfuß, Wärmegestehungskosten |
| Referenzfall | Stammprojekt; Varianten als **Differenz** — die VALERI-Sicht „Maßnahme gegen Weiterbetrieb“ |
| Offenlegung der Annahmen | Parameternachweis, Herkunft der Steuersätze, Hinweiszeile bei jeder Vereinfachung |

### 2. Umgesetzt (ohne neues Datenmodell)

- **V1 — Ersatzbeschaffungen als eigener Ausweis.** Sie wurden seit W1 gerechnet, aber nie
  ausgewiesen: Sie steckten stumm in `BarwertAusgaben`. Neu ist der abgeleitete Barwert
  (`WirtschaftlichkeitErgebnis.ErsatzBarwert`, Spalte
  `Tab_ErgebnisWirtschaftlichkeit.ErsatzBarwert` über `SpalteSicher`) und die Zeile
  „Ersatzbeschaffungen, Barwert“ in `WirtschaftlichkeitZeilen.Kennzahlen` — also in Seite,
  Word und Excel zugleich, unmittelbar **vor** dem Restwert. Sie erscheint nur, wo es Ersatz
  gibt. **Reiner Ausweis:** Der Kapitalwert ist unverändert, die Zahl wird aus dem fertigen
  Zahlungsbild abgelesen und nirgends aufsummiert.
- **V2 — die Annahmen der Bandbreite stehen in der Nachweiszeile** (Dialog und Seite,
  siehe W5‑B‑9 § 4 und § 5).
- **V3 — je Ergebniszeile der wirksame Satz** statt dreimal des Erwartungswerts.

### 3. Offen — Entscheidungsbedarf des Anwenders

| Nr. | Lücke | Was VALERI verlangt | Aufwand |
|---|---|---|---|
| **G1** | **Endjahr je Position.** EPOS kennt seit KD6 ein Startjahr, aber kein Endjahr. | Start- **und** Endjahr je Faktor (`99` = ganze Betriebszeit). | Spalte + Rechenweg; Datenmodell |
| **G2** | **Preisänderung je Kostenart.** Heute zwei Töpfe (p_B, p_E) plus CO₂-Pfad. | eine eigene Preisänderung je Faktor | Spalte je Zeile + Rechenkern |
| **G3** | **Degradation je Faktor.** Nur die PV-Ertragsdegradation ist modelliert. | Degradation je Nutzen-/Lastenfaktor | Spalte je Zeile |
| **G4** | **Preisindizierung der Ersatzbeschaffung.** Ersatz nominal unverändert (Vereinfachung W1). | VDI 2067/VALERI setzen Ersatz üblicherweise preisindiziert an | Rechenkern; **fachlicher Entscheid** |
| **G5** | **Startjahr für die Energiekosten.** Die Simulation kennt keine Startjahre je Komponente (dokumentierte Vereinfachung FK10). | jeder Faktor ab seinem Betriebsjahr | Simulation; groß |
| **G6** | **Nicht monetisierbare Wirkungen.** Kein Freitextfeld. | qualitative Beschreibung im Bewertungsbericht | Feld + Berichtsbaustein |
| **G7** | **Betrachtungszeitraum aus der Nutzungsdauer.** T wird nicht gegen die längste Nutzungsdauer geprüft. | Begründung des Zeitraums | Prüfzeile; klein |
| **G8** | **Berichtsausgabe der Bandbreite.** Word/Excel führen den Erwartungsfall. | alle drei Szenarien nebeneinander | Berichtsbaustein; **kein klarer Anker — offen gelassen** |
| **G9** | **Entscheidungsempfehlung als Text** („Vorschlag zur Entscheidung“). | Empfehlung im Bericht | Textbaustein; klein |
| **G10** | **Aufteilung Eigennutzung/Einspeisung.** EPOS leitet sie aus der Simulation ab — fachlich besser als VALERI, aber die Herleitung steht nicht im Bericht. | als Annahme ausweisen | Ausweis; klein |
| **G11** | **Investitionsgekoppelte Betriebskosten im Szenario.** „x % der Investitionssumme“ folgt dem Szenario-Investitionsausschlag **nicht** (anders als in der Sensitivität, FX5‑a). | Konsequenz wäre, den Ausschlag auch dort mitzuziehen | klein; **fachlicher Entscheid** |

**Bewusst nicht übernommen:** Die VALERI-Vorlage rechnet ihre Worst-/Best-Spalten über feste
Formelfaktoren im Tabellenblatt (z. B. `=E44*1,3`). EPOS-Plan trennt statt dessen **gepflegter
Zeilenwert** von **pauschalem Parametersatz** — dieselbe Wirkung, aber nachvollziehbar, wo die
Zahl herkommt.

---

## Nachweise W5‑B‑9 und W5‑B‑10

| Prüfung | Ort | Ergebnis |
|---|---|---|
| Vorgaben und Vorzeichen, Nullsemantik, Zinsklemme, Nutzungsdauerklemme, Nachweiszeile | `SzenarioParameterTests` (neu) | 6 Fälle |
| `Szenariowert` meldet die Herkunft | `SzenarioParameterTests.Szenariowert_meldet_die_Herkunft` | grün |
| Ohne Satz bleibt die Investitionsliste **bitgleich** | `…Ohne_Satz_bleibt_die_Investitionsliste_unveraendert` | grün |
| Ausschlag auf nicht gepflegte Zeilen (× 1,1 / × 0,9 der Erwartungssumme) | `…Der_Ausschlag_greift_auf_nicht_gepflegte_Zeilen` | grün |
| **Vorrangregel**: gepflegte 7.000 € bleiben 7.000 €, nicht 7.700 € | `…Ein_gepflegter_Zeilenwert_schlaegt_den_pauschalen_Ausschlag` | grün |
| Zuschuss wird nicht skaliert (K5) | `…Der_Zuschuss_wird_vom_Ausschlag_nicht_skaliert` | grün |
| Nutzungsdauer folgt Satz und Vorrangregel | `…Die_Nutzungsdauer_folgt_dem_Satz_und_der_Vorrangregel` | grün |
| **KW_Best > KW_Erwartet > KW_Worst** über den echten Rechenkern | `…Best_Erwartet_und_Worst_liegen_auseinander` | grün |
| **Erwartet bleibt zahlengleich** — auch mit von Hand gepflegten Best-/Worst-Sätzen | `…Erwartet_bleibt_zahlengleich` | grün (12 Dezimalstellen) |
| Migrationsschritt 71: zwölf `DOUBLE`-Spalten, Zielstand 71 | `…Der_Migrationsschritt_71_fuehrt_zwoelf_Spalten` | grün |
| Speichern/Laden: gepflegt bleibt Zahl, leer bleibt leer | `…Der_Satz_ueberlebt_Speichern_und_Laden` | grün |
| Ersatzzeile erscheint nur mit Ersatz, steht vor dem Restwert | `…Die_Ersatzzeile_erscheint_nur_mit_Ersatzbeschaffungen` | grün |
| Szenariotabelle: 3 Spalten × 6 Zeilen, je 2 Felder | `WirtschaftlichkeitParameterDialogTests.Die_Szenariotabelle_zeigt_drei_Spalten_und_sechs_Groessen` | grün |
| Felder zeigen die wirksamen Vorgaben | `…Die_Szenariofelder_zeigen_die_wirksamen_Vorgaben` | grün |
| „Vorgaben“ setzt auf `null` zurück, Herleitungszeile wechselt | `…Vorgaben_setzt_die_zwoelf_Felder_zurueck` | grün |
| Herleitungszeilen nennen beide Sätze | `…Die_Herleitungszeilen_nennen_beide_Saetze` | grün |
| Statuszeile über dem Parameternachweis; leer = keine Zeile | `WirtschaftlichkeitSeiteTests.Die_Szenariozeile_*` | 2 Fälle grün |
| Schemawerkzeug `Testdatenbankschema` auf einer Kopie der Referenzdatenbank | 12 Spalten angelegt, Schemastand nachher **71** | grün |
| Sandbox-Bau `WP-Plan.sln` x64 Debug | `K:\imp2\src` | **0 Fehler** |
| `EPOS.Kern.Tests` / `EPOS.UI.Tests` | `dotnet test --no-build` | **2 154/2 154** / **3 343/3 343** |

**Ein Referenzlauf war nicht nötig:** Diese Etappe fasst keinen Simulationswert an. An seine
Stelle tritt die Regressionsprobe der Kapitalwerte
(`Erwartet_bleibt_zahlengleich`, 12 Dezimalstellen) und der bitgleiche Vergleich der
Investitionslisten.

**Bestehende Erwartungswerte angepasst:** Der Szenarioblock steht im Parameterdialog zwischen
„Allgemein“ und „Strom“ und bringt zwölf Zahlenfelder mit; sechs Fälle in
`WirtschaftlichkeitParameterDialogTests` griffen Felder über ihren Index. Die Indizes stehen
dort jetzt als Konstanten (`SZENARIO_FELDER`, `EINSPEISUNG_PV`, …), damit der nächste Umbau
eine Zeile trifft statt sechs.

### Offene Punkte

- **Sichtabnahme** des Abschnitts „Szenarien“ im Parameterdialog (Spaltenbreiten der
  Tabelle auf schmalen Fenstern) und der Statuszeile auf der Seite.
- **Best/Worst ändern sich** gegenüber dem Stand vor dieser Etappe — das ist gewollt, sollte
  dem Anwender aber bei der ersten Neuberechnung bewusst sein.
- Die **elf VALERI-Lücken G1…G11** oben warten auf Entscheidungen; G4, G8 und G11 sind
  fachliche Entscheide, die übrigen Aufwandsfragen.

---

## Anwenderentscheid 09.09.2026 — W5‑B‑11: VALERI-Gaps ohne Datenmodell

Der VALERI-Abgleich der Etappe W5‑B‑10 hatte elf Lücken benannt (G1…G11). Der Anwender hat
sie am 09.09.2026 entschieden. Diese Etappe setzt alles um, was **ohne neue Spalte**
auskommt; G4, G2 und G6 gehören zu W5‑B‑12 (Migrationsschritt 72), G1, G3 und G5 werden
nicht umgesetzt, aber **offengelegt**.

### 1. Die Entscheidungstabelle

| Nr. | Lücke | Entscheid 09.09.2026 | Wo umgesetzt |
|---|---|---|---|
| **G1** | Endjahr je Kostenposition | **nicht** — offenlegen | Hinweiszeile „Vereinfachungen (VALERI)" |
| **G2** | Preisänderung je Kostenart | **W5‑B‑12** — nur als dritter Satz p_I | Etappe W5‑B‑12 |
| **G3** | Degradation je Faktor | **nicht** — offenlegen | Hinweiszeile „Vereinfachungen (VALERI)" |
| **G4** | Preisindizierung der Ersatzbeschaffung | **W5‑B‑12** — p_I | Etappe W5‑B‑12 |
| **G5** | Startjahr der Energiekosten | **nicht** — offenlegen (FK10) | Hinweiszeile „Vereinfachungen (VALERI)" |
| **G6** | Nicht monetisierbare Wirkungen | **W5‑B‑12** — Freitextfeld | Etappe W5‑B‑12 |
| **G7** | Betrachtungszeitraum gegen die Nutzungsdauern | **jetzt** | `NutzungsdauerAbgleich` |
| **G8** | Berichtsausgabe der Bandbreite | **jetzt** | `BausteineWirtschaftlichkeit`, `ExcelBerichtGenerator` |
| **G9** | Entscheidungsempfehlung als Text | **jetzt** | `WirtschaftlichkeitEmpfehlung` |
| **G10** | Eigennutzung/Einspeisung als Herleitung | **jetzt** | `ValeriAusweis.EigennutzungHerleitung` |
| **G11** | Investitionsgekoppelte Betriebskosten im Szenario | **jetzt** | `InvestKaskade`, `BetriebskostenCtrl`, `WirtschaftlichkeitCtrl` |

### 2. G11 — die Prozentzeilen folgen dem Szenario-Investitionsausschlag

**Der Befund.** Eine Betriebskostenzeile „x % der Investitionssumme"
(`DbWerte.BEMESSUNG_PROZENT_INVESTITION`, im Bestand die häufigste Kategorie‑2‑Bemessung:
Wartung, Versicherung, Verwaltung) bemaß sich bis hierher IMMER an der Investition des
**Erwartungsfalls**: `BetriebskostenCtrl.Kaskadensummen` stand fest auf
`WirtschaftlichkeitSzenario.ERWARTET`. Kostet die Anlage im Worst-Fall 10 % mehr, kostete ihre
Wartung trotzdem unverändert — während die **Sensitivität** denselben Ausschlag längst
mitzieht (PAKET FX5‑a, additive Korrektur über `BetriebsTopfe.InvestGekoppelt`). Zwei Wege,
zwei Antworten auf dieselbe Frage.

**Der gewählte Weg — der bevorzugte, nicht der Notweg.** Die BASISBERECHNUNG bekommt den
`SzenarioSatz`; skaliert wird **je Zeile** und nur dort, wo kein Best-/Worst-Wert gepflegt
ist. Die Regel steht seither an EINER Stelle:

* `InvestKaskade.BetragImSzenario(Zeile z, SzenarioSatz satz)` — `satz == null` oder
  `z.WertGepflegt` → `z.Betrag` unverändert, sonst `z.Betrag × satz.InvestFaktor`.
  `WirtschaftlichkeitCtrl.LiesInvestitionen` fragt seither dieselbe Methode; ihr Rechenweg
  ist unverändert.
* `InvestKaskade.Summen(idProjekt, szenario, satz)` — die Bemessungsbasis je
  (Komponente, Anlage) mit dieser Regel.
* `BetriebskostenCtrl.Kaskadensummen(projektID, satz)` — der Satz trägt sein Szenario selbst
  (`SzenarioSatz.Szenario`); `null` heißt Erwartungslauf.
* `WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(…, satz)` und
  `LiesBetriebskostenPositionen(…, satz)` — beide bekommen denselben Satz, damit die
  E7‑Probe „Summe der Nachweisliste = Summe der Rechnung" auch im Szenario hält.
  `BaueEingabe` reicht ihn durch.

**Die Vorrangregel gilt auch für die Basis.** Eine Investitionszeile mit gepflegtem
Worst-Wert von 50.000 € trägt genau 50.000 € zur Bemessungsbasis bei, nicht 55.000 €. Der
Notweg „Basis pauschal × InvestFaktor" wurde damit NICHT genommen — er hätte die
Doppelzählung erzeugt, die § 2.2 des Konzepts ausschließt.

**Bitgleich ohne Satz.** `satz == null` (Szenario ERWARTET, jede Anzeige, Dialog
Kostenverwaltung, Kostenseite) betritt den neuen Zweig gar nicht erst; die
Zweiargument-Überladungen bleiben Zeichen für Zeichen die von vorher.

**Die Sensitivität bleibt, wo sie war.** Sie rechnet auf ERWARTET — dort ist der Satz null —
und korrigiert ihren eigenen Ausschlag weiterhin additiv in `RechneBild`. Beides zusammen
wäre Doppelzählung; beides trifft aber nie zusammen. Wartungszeilen aus dem Gerätedialog
(`TechnikPlanwertCtrl`, Hauptposition) sind unberührt.

**Zahlenbeleg** (Projekt 1018, BHKW-Anlage 11327, Vorgabesatz Worst = + 10 %):

| Größe | vor W5‑B‑11 | nach W5‑B‑11 |
|---|---|---|
| Bemessungsbasis der Betriebszeile | 58.049,84375 € | 63.854,828125 € |
| Betriebskosten p. a. (2 %) | 1.160,996875 €/a | 1.277,096563 €/a |
| Kapitalwert Worst (i = 4 %, T = 20 a) | − 125.315,69 € | **− 127.030,57 €** (Δ − 1.714,87 €) |

Mit einer gepflegten Worst-Zeile (Hauptposition 50.000 €) ergibt die Vorrangregel eine Basis
von 65.081,00 € und 1.301,62 €/a — der Notweg käme auf 70.081,00 € und 1.401,62 €/a.

### 3. G8 — die Bandbreite im Bericht

- Der Hinweistext über der Szenarientabelle nannte seit W5‑B‑9 nur die **Zeilenwerte**. Er
  nennt jetzt **beide Quellen** (`WIRT_SZ_QUELLEN`): gepflegte Best-/Worst-Felder mit
  Vorrang, sonst der pauschale Parametersatz — und dass die investitionsgekoppelten
  Betriebskosten dem Investitionsausschlag folgen (G11).
- Die drei Wertspalten führten schon immer `KapitalwertDiff`, hießen aber „KW Worst" — der
  Name einer **anderen** Größe. Die Köpfe heißen jetzt **„ΔKW Worst/Erwartet/Best [€]"**,
  stehen als Ressourcen (`WIRT_SZ_SP_*`, Kopfzellen ohne zweite Übersetzung) und eine
  Fußzeile sagt, was Δ heißt und wann „—" erscheint (`WIRT_SZ_DELTA_FUSS`).
- **Neue Spalte „Einstufung"** je Variante (G9). Sechs Spalten statt fünf; die Summe bleibt
  `WordBerichtGenerator.INHALT_B` = 9 355 dxa (Beschriftung 2 455 + 5 × 1 380).
- **Annahmenzeile je Szenario** unter der Tabelle: `SzenarioSatz.Nachweis(p, kultur)` mit
  Herkunft („Vorgaben" / „gepflegte Werte", `WPAR_SZ_HERKUNFT_*` — dieselben Ressourcen wie
  Dialog und Seite). Erwartet bekommt keine: Es IST der Projektparametersatz.
- **Excel** zeigt dieselbe Annahmenzeile unmittelbar unter jeder Blocküberschrift
  „Szenario: …" und den Vorschlag unter den drei Blöcken.

### 4. G9 — die Entscheidungsempfehlung

Neu: `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitEmpfehlung.cs` mit
`WirtschaftlichkeitEmpfehlung`, `VariantenEmpfehlung`, `EmpfehlungStufe` — **im Kern**, damit
Seite, Word und Excel denselben Satz zeigen (Lehre aus E7, Divergenzen D1…D5).

Maßstab ist die **Kapitalwertdifferenz zum Stamm**, nicht der absolute Kapitalwert: Der Stamm
IST die Unterlassensalternative.

| Stufe | Bedingung |
|---|---|
| **empfohlen** | ΔKW > 0 in Worst, Erwartet und Best |
| **bedingt empfohlen** | ΔKW > 0 in Erwartet, aber ≤ 0 in Worst (oder in Best) |
| **nicht empfohlen** | ΔKW ≤ 0 in Erwartet |
| Zusatz „Bandbreite nicht berechnet" | Best oder Worst fehlt → Urteil allein nach Erwartet |

**Gesamtvorschlag:** die höchste Erwartet-Differenz unter den empfohlenen, sonst unter den
bedingt empfohlenen, sonst „Keine Variante ist gegenüber dem Stammprojekt wirtschaftlich;
Weiterbetrieb (Referenzfall)." **Ohne Variante mit Erwartet-Ergebnis bleibt der Text leer** —
Seite und Bericht zeichnen die Zeile dann gar nicht erst.

Ausgabe: Seite (`ErgebnisAnsicht.Empfehlungszeile`, Herleitungszeile unter der
Vergleichstabelle), Word (Absatz nach der Szenarientabelle), Excel (Zelle unter den
Szenarioblöcken). Auf der Seite hängt die Zeile an der **Ansicht** und nicht am Stand: Sie
folgt der Vergleichswahl, und die tauscht — wie der Szenariowechsel — genau dieses Objekt aus.

### 5. G7 — Betrachtungszeitraum gegen die Nutzungsdauern

`NutzungsdauerAbgleich.Hinweis(T, positionen, kultur)` bildet aus T und den
Investitionspositionen des ERWARTET-Laufs eine Zeile: kürzeste und längste **gepflegte**
Nutzungsdauer (n < 1 heißt im Rechenkern „wie T" und zählt nicht), dazu

- T < längste → „Restwert am Ende angesetzt",
- T > kürzeste → „Ersatzbeschaffung im Jahr n" (dieselbe Rundung wie der Rechenkern:
  `tj = round(start + n)`, nur innerhalb 1 ≤ tj < T),
- weder noch → „deckungsgleich",
- keine gepflegte Dauer → „kein Ersatz, kein Restwert".

Kein Blocker, reiner Ausweis: Der `KapitalwertRechner` ist unberührt. Ausgabe im
Parameternachweis der Seite (`WirtschaftlichkeitStand.Zeitraumzeile`) und im Word-Bericht
unter „Parameter dieses Rechenlaufs".

### 6. G10 und die Vereinfachungen G1/G3/G5

`ValeriAusweis.EigennutzungHerleitung()` — nur mit Photovoltaik in der Gruppe
(`ErzeugerDerGruppe`) — sagt, dass Eigenverbrauchsquote und Einspeiseanteil **aus der
Stundensimulation abgeleitet** und nicht als Annahme gesetzt sind. Das ist fachlich besser
als die VALERI-Vorlage, die sie erfragt; genau deshalb muss die Herleitung dastehen, sonst
hält der Leser die Zahl für eine Schätzung. Kein neuer Rechenweg.

`ValeriAusweis.Vereinfachungen()` benennt die drei nicht umgesetzten Lücken: kein Endjahr je
Kostenposition (G1), keine Degradation außer beim PV-Ertrag (G3), Energiekosten als
Gesamtrechnung des Simulationslaufs ab Jahr 1 (G5/FK10). Beides steht auf der Seite
(`WirtschaftlichkeitStand.Vereinfachungszeile`) und im Word-Bericht bei den Parametern.

### 7. Nachweise W5‑B‑11

| Prüfung | Ort | Ergebnis |
|---|---|---|
| Ohne Satz bleiben die Betriebskosten **bitgleich** | `ValeriLueckenTests.Ohne_Satz_bleiben_die_Betriebskosten_unveraendert` | grün |
| Prozentzeile folgt dem Ausschlag (× 1,1 / × 0,9), Erwartet bleibt Erwartet | `…Die_Prozentzeile_folgt_dem_Investitionsausschlag_des_Szenarios` | grün |
| **Vorrangregel in der Basis**: gepflegte 50.000 € → Basis 65.081,00 €, nicht 70.081,00 € | `…Ein_gepflegter_Zeilenwert_bleibt_auch_in_der_Bemessungsbasis_stehen` | grün |
| E7‑Probe: Nachweisliste trifft die Summe auch im Szenario | `…Die_Nachweisliste_trifft_die_Summe_auch_im_Szenario` | grün |
| **Regressionsbeleg**: KW Worst − 125.315,69 € → − 127.030,57 € | `…Der_Kapitalwert_im_Worst_Fall_wird_durch_G11_unguenstiger` | grün |
| G9: drei positive Szenarien → „empfohlen", Satz nennt die Bandbreite | `…Drei_positive_Szenarien_ergeben_empfohlen` | grün |
| G9: negativer Worst-Fall → „bedingt empfohlen" | `…Ein_negativer_Worst_Fall_ergibt_bedingt_empfohlen` | grün |
| G9: Erwartet ≤ 0 → „nicht empfohlen", Satz nennt den Weiterbetrieb | `…Ein_nicht_positiver_Erwartungsfall_ergibt_nicht_empfohlen` | grün |
| G9: ohne Bandbreite urteilt nur Erwartet, mit Zusatz | `…Ohne_Bandbreite_urteilt_nur_der_Erwartungsfall` | grün |
| G9: höchste Differenz der **empfohlenen** gewinnt, sonst der bedingten | `…Der_Vorschlag_nimmt_die_hoechste_Differenz_der_empfohlenen` | grün |
| G9: ohne Stamm / ohne Variante kein Satz | `…Ohne_Stamm_und_ohne_Variante_gibt_es_keinen_Satz` | grün |
| G7: T < n → Restwert · T > n → Ersatz im Jahr n · T = n → deckungsgleich · keine Dauer | `…T_unter_…`, `…T_ueber_…`, `…T_gleich_…`, `…Ohne_gepflegte_Nutzungsdauer_…` | 4 Fälle grün |
| Seite: Zeitraum- und Vereinfachungszeile unter dem Parameternachweis | `WirtschaftlichkeitSeiteTests.Der_Nachweisblock_zeigt_Zeitraum_und_Vereinfachungen` | grün |
| Seite: Empfehlungszeile UNTER der Vergleichstabelle | `…Die_Empfehlungszeile_steht_unter_der_Vergleichstabelle` | grün |
| Seite: leere Empfehlungszeile wird nicht gezeichnet | `…Eine_leere_Empfehlungszeile_wird_nicht_gezeichnet` | grün |
| Sandbox-Bau `WP-Plan.sln` x64 Debug | `K:\imp\src` (Stand `7ade6e7f` + diese Etappe) | **0 Fehler** |
| `EPOS.Kern.Tests` / `EPOS.UI.Tests` | `dotnet test --no-build` | **2 185/2 185** / **3 359/3 359** |

Ausgangsstand derselben Sandbox ohne diese Etappe: 2 170 / 3 356 — die 15 neuen Kern- und
3 neuen UI-Fälle sind vollständig zugeordnet.

**Ein Referenzlauf war nicht nötig:** Diese Etappe fasst keinen Simulationswert an. An seine
Stelle tritt die Bitgleichheitsprobe des Erwartungsfalls
(`Ohne_Satz_bleiben_die_Betriebskosten_unveraendert`).

**Bestehende Erwartungswerte angepasst:** keine. Kein vorhandener Fall greift die
Szenario-Betriebskosten, die Spaltenköpfe der Szenarientabelle oder den Hinweistext darüber;
die Word-/Excel-Bausteine haben keinen eigenen Prüffall (`grep -rn "BausteineWirtschaftlichkeit"
EPOS.Kern.Tests` ist leer).

### Offene Punkte

- **Sichtabnahme**: Szenarientabelle im Word-Bericht (sechs Spalten, Umbruch der Köpfe
  „ΔKW Erwartet [€]" und „bedingt empfohlen" auf schmalem Satzspiegel), die zwei
  Annahmenzeilen darunter, der Vorschlagssatz; im Excel-Blatt die Annahmenzeile je Block und
  die Vorschlagszelle; auf der Seite die drei neuen Herleitungszeilen (Zeitraum,
  Vereinfachungen, Vorschlag).
- **Best- und Worst-Kapitalwerte ändern sich** für jedes Projekt mit Betriebskostenzeilen
  „x % der Investitionssumme" (G11) — Worst wird ungünstiger, Best günstiger. Das ist
  gewollt, sollte dem Anwender aber bei der ersten Neuberechnung bewusst sein. Der
  Erwartungsfall ist unberührt.
- **W5‑B‑12** (p_I, Freitextfeld) bringt zwei weitere Zeilen in denselben Nachweisblock; die
  Annahmenzeile des Szenarios wächst dann um p_I. Die Stellen sind dieselben
  (`SzenarioSatz.Nachweis`, `SchreibeSzenarioAnnahmen`, `WIRT_SZ_QUELLEN`).

---

## Anwenderentscheid 09.09.2026 — W5‑B‑12 Teil a: Preisindizierung der Ersatzbeschaffung (p_I) und nicht monetäre Wirkungen — Rechenkern und Schritt 72

Zwei der elf VALERI-Lücken aus W5‑B‑10 sind entschieden: **G4** (Ersatzbeschaffungen
werden preisindiziert, VDI 2067 Blatt 1) und **G6** (Freitextfeld „Nicht monetäre
Wirkungen“). **G2** („Preisänderung je Kostenart“) wird ausdrücklich nur so weit
umgesetzt, wie G4 reicht — als dritter Topf p_I neben p_B und p_E, nicht als Spalte je
Zeile.

**Teil a (dieser Eintrag)** legt den Rechenweg und die Ablage an. **Teil b** — Parametersatz,
Dialog, Bericht — folgt; bis dahin steht p_I im Rechenkern auf 0 und **keine Zahl ändert sich**.

### Was der Rechenkern jetzt tut

`KapitalwertRechner.Rechne` nimmt am ENDE seiner Signatur einen optionalen
`double preisstInvestProzent = 0` entgegen (jeder bestehende Aufrufer bleibt unverändert
gültig — es gibt genau einen, `WirtschaftlichkeitCtrl.RechneBild`).

* **Ersatzbeschaffung** im Jahr tj: `A(tj) = A₀ · (1 + p_I)^tj`. Der Exponent ist das
  ABSOLUTE Jahr, nicht der Abstand zum Startjahr — Preisstand des Rechenkerns ist immer t = 0.
* **Erstbeschaffung nominal**, auch die nach KD6 verschobene: Der eingegebene Betrag ist
  der Betrag zum Zahlungszeitpunkt und kein auf heute zurückgerechneter Preisstand.
* **Restwert** auf der Preisbasis der LETZTEN Beschaffung (Faktor 1 bei Erst-/verschobener
  Beschaffung, sonst `(1 + p_I)^tj`), linear × rest/n und abgezinst wie bisher.
* **p_I = 0 rechnet bitgleich.** Der Indexfaktor wird dann gar nicht erst gebildet —
  dieselbe IEEE‑754-Vorsicht wie bei FX4‑c/FX5‑a.

### Nachweise W5‑B‑12 Teil a

| Prüfung | Ort | Ergebnis |
|---|---|---|
| p_I = 0 ist **bitgleich** zum Aufruf ohne den Parameter (KW, Restwert, Ersatzreihe, Barwert-/Nominalreihe, 21 Glieder) | `KapitalwertRechnerPreisindexTests.Ohne_Preisaenderungssatz_bleibt_das_Zahlungsbild_bitgleich` | grün, **exakt** (kein Toleranzfenster) |
| bekannte Beträge ohne Satz: Ersatz 10.000 in t = 8/16, Restwert 5.000 | `…Ohne_Preisaenderungssatz_stehen_die_bekannten_Betraege` | grün |
| p_I = 2 %, n = 8, T = 20: Ersatz **11.716,59 €** (1,02⁸) und **13.727,86 €** (1,02¹⁶), I₀ nominal | `…Die_Ersatzbeschaffungen_werden_auf_ihr_Zahlungsjahr_indiziert` | grün |
| Restwert = 10.000 · 1,02¹⁶ · (8−4)/8 = **6.863,93 €**, abgezinst | `…Der_Restwert_steht_auf_der_Preisbasis_der_letzten_Beschaffung` | grün |
| KD6: StartJahr 5, n = 10 → Zahlung in t = 5 **nominal**, Ersatz t = 15 = **13.458,68 €** (1,02¹⁵), Restwert **6.729,34 €** | `…Die_verschobene_Erstbeschaffung_bleibt_nominal` | grün |
| KW sinkt mit p_I > 0, wo ersetzt wird | `…Mit_Ersatzbeschaffung_sinkt_der_Kapitalwert` | grün |
| n ≥ T: kein Ersatz, Restwert 2.000, KW **exakt gleich** mit und ohne Satz | `…Ohne_Ersatzbeschaffung_aendert_der_Satz_nichts` | grün |
| Migrationsschritt 72: vier Spalten, Zielstand 72 | `Migration72Tests.Der_Migrationsschritt_72_fuehrt_vier_Spalten` | grün |
| Typen: 3 × `DOUBLE`→`REAL`, `MEMO`→`TEXT` ohne Längenprüfung | `…Die_Spaltentypen_sind_REAL_und_TEXT` | grün |
| Sammel-Enumerator in Anlegereihenfolge (EINE Quelle) | `…Der_Sammel_Enumerator_fuehrt_beide_Bloecke_in_Anlegereihenfolge` | grün |
| Spalten stehen in der Testdatenbank | `…Die_vier_Spalten_stehen_in_der_Testdatenbank` | grün |
| Zahl, Fließtext und NULL gehen hin und zurück | `…Die_Spalten_nehmen_Zahl_Text_und_NULL` | grün |
| Sandbox-Bau `WP-Plan.sln` x64 Debug | `K:\imp2\src` (= HEAD + 8 Dateien, Datei für Datei geprüft) | **0 Fehler** |
| `EPOS.Kern.Tests` / `EPOS.UI.Tests` | `dotnet test --no-build` | **2 170/2 170** / **3 356/3 356** |

**Ein Referenzlauf war nicht nötig:** Die Etappe fasst keinen Simulationswert an. An seine
Stelle tritt die Bitgleichheitsprobe bei p_I = 0 — sie vergleicht ohne Toleranzfenster.

### Offene Punkte

- **Teil b** (Parametersatz, Dialog, Bericht): p_I in `SzenarioSatz`/`WirtschaftlichkeitParameter`,
  Lade-/Speicherweg, `StelleTabellenSicher` um `Schritt72_ValeriErgaenzung` ergänzen,
  Einspeisung in `RechneBild`, Freitextfeld im Dialog und Berichtsbaustein für G6.
- **Wirkung nach Teil b:** Bestandsprojekte MIT Ersatzbeschaffung rechnen dann mit
  p_I = p_B; ihre Kapitalwerte sinken leicht. Das ist gewollt — der bisherige Ausweis war
  der zu günstige. Projekte ohne Ersatz bleiben zahlengleich.
- **Sichtabnahme** steht aus (Teil b bringt erst die Oberfläche).
- Die Doku-Aufzählung in `SchemaMigration.SCHRITTE_SQLITE` listet die Schritte nur bis 69
  auf — 70, 71 und 72 fehlen dort; Nachzug bei Gelegenheit.

---

## Anwenderentscheid 09.09.2026 — W5‑B‑12 Teil b: p_I im Parametersatz, Dialog, Bericht und Freitext „Nicht monetäre Wirkungen"

Teil a hatte den Rechenweg gelegt (`KapitalwertRechner.Rechne` nimmt p_I entgegen) und die vier
Spalten des Migrationsschritts **72** angelegt — mehr nicht: p_I stand auf 0, und **keine Zahl
änderte sich**. Teil b schließt die Kette. Er füllt den Parametersatz, reicht ihn in den
Rechenkern, macht ihn im Dialog pflegbar und weist ihn in Bericht und Seite aus; dazu kommt das
Freitextfeld der VALERI-Lücke **G6**.

### 1. Der Parametersatz

- `WirtschaftlichkeitParameter.PreissteigerungInvestition` (`double?`) und
  `NichtMonetaer` (`string`). Die Nullsemantik steht an **einer** Stelle:
  `PreisInvestWirksam => PreissteigerungInvestition ?? PreissteigerungBetrieb`. NULL heißt
  „wie p_B", nicht „0 %" — eine 0 als Vorbelegung hätte behauptet, Investitionsgüter würden nie
  teurer.
- `SzenarioSatz.PreissteigerungInvestition` (`double?`) ist die **siebte** Größe des Satzes;
  `NurVorgaben`, `Kopie`, `Vorgabe(szenario)` und `Nachweis(p, kultur)` führen sie mit.

> **Die Vorgaberegel je Szenario.** wirksames p_I(Szenario) =
> `satz.PreissteigerungInvestition ?? (p_I_erwartet ± 1 %‑Punkt)` mit
> `p_I_erwartet = p.PreissteigerungInvestition ?? p.PreissteigerungBetrieb`
> (Best −, Worst +). Der Bezug ist also das **Erwartet‑p_I**, nicht das p_B des Szenarios —
> dieselbe ∓1‑%‑Punkt-Regel wie bei p_E und p_B, angewandt auf den Erwartungswert der eigenen
> Größe. Im Regelfall (p_I ungepflegt, Szenario‑p_B ungepflegt) ist das **genau das wirksame p_B
> des Szenarios**; ist Erwartet‑p_I gepflegt, spannt sich die Bandbreite um diesen Wert; und ist
> nur das Szenario‑p_B gepflegt, folgt p_I ihm **nicht** — sonst zöge eine Betriebskostenannahme
> still die Ersatzbeschaffung mit.

- `FuerSzenario` ersetzt p_I wie Zins, p_E und p_B — und zwar als **gepflegten** Wert in der
  Kopie. Bliebe das Feld dort `null`, fiele die Kopie über `PreisInvestWirksam` auf ihr eigenes
  (bereits ersetztes) p_B zurück und das Szenario rechnete an seinem Satz vorbei.
- `RechneBild` übergibt `p.PreisInvestWirksam` als letzten Parameter an `Rechne`. Das ist die
  **einzige** Aufrufstelle des Rechenkerns; Hauptlauf, Verlaufsdialog und Sensitivität gehen alle
  dort durch, es fehlt also keine Stelle.
- Lade-/Speicherweg: `LadeParameter` (ohne `?? 0`), `LiesSatz` um die siebte Spalte, UPDATE und
  INSERT um `Preissteigerung_Investition`, `Szen_Best_Preis_I`, `Szen_Worst_Preis_I` und
  `Nicht_Monetaer` (Freitext als `LongVarWChar` wie `WQ_Wochenwerte` — VarWChar schnitte ihn auf
  dem Access-Rückweg ab). `StelleTabellenSicher` bekommt die Schleife über
  `SchemaKatalog.Schritt72_ValeriErgaenzung` neben der für Schritt 71 **und** die vier Spalten im
  `CREATE TABLE` von `Tab_ProjektWirtschaftlichkeit` (Muster K6).
- **Bewusst nicht:** p_I je Ergebniszeile in `Tab_ErgebnisWirtschaftlichkeit`. Die Annahmenzeile
  des Berichts entsteht aus dem **Parametersatz**, nicht aus der Ergebniszeile; eine fünfte
  Ergebnisspalte wäre eine zweite Wahrheit für dieselbe Zahl. Der `CREATE TABLE`-Text der
  Ergebnistabelle bleibt deshalb unangetastet.
- `KiAktionenWirtschaft.ParameterLesen` meldet `preissteigerung_investition_prozent` (den
  **wirksamen** Wert), `preissteigerung_investition_herkunft` (`gepflegt` / `wie_betrieb`) und
  `nicht_monetaere_wirkungen`.

### 2. Dialog „Parameter…"

- **Allgemein**: das Zahlenfeld „Preissteigerung Investition/Ersatz p_I [%/a] (leer = wie
  Betrieb)" neben p_B — als einziges Feld des Blocks **ohne** Rückfall auf den alten Wert, denn
  leer ist hier eine Aussage. Darunter eine Herleitungszeile mit dem wirksamen Wert und seiner
  Herkunft (`WPAR_PREIS_I_ZEILE`).
- **Szenarien**: eine siebte Zeile „Preissteigerung Investition", unmittelbar hinter den beiden
  anderen Preissteigerungen (dieselbe Reihenfolge wie in `Nachweis`: i · p_E · p_B · p_I ·
  Investition · Erträge · Nutzungsdauer). Erwartet = Anzeige des wirksamen Werts, Best/Worst =
  Felder mit der wirksamen Vorgabe. „Vorgaben" setzt jetzt **vierzehn** Felder zurück (der Knopf
  legt beide Sätze neu an und trifft das neue Feld dadurch von selbst).
- **Neuer Abschnitt „Bewertung nach DIN EN 17463"** mit dem mehrzeiligen Freitextfeld „Nicht
  monetäre Wirkungen" (Hausbaustein `Textfeld Mehrzeilig`, kein neuer Baustein und **kein neuer
  CSS-Block**). Ein eigener Abschnitt, weil unter „Allgemein" Rechengrößen stehen; dieser Text
  rechnet nichts, sondern steht neben der Zahl, so wie die Norm es verlangt.
- Die Hülle `WirtschaftlichkeitParameterHuelle` blieb **unverändert**: Der Parametersatz kommt
  vollständig herein und geht vollständig zurück, jeder neue Gabenschlüssel wäre überflüssig
  gewesen.

### 3. Bericht und Seite

- `WIRT_SZ_QUELLEN` (Hinweis über der Szenarientabelle) nennt jetzt als dritte Quelle p_I mit
  seiner Nullsemantik — deutsch und englisch.
- `p.Nachweis(kultur)` führt „Investition/Ersatz x,x %/a (gepflegt | wie Betrieb)";
  `SzenarioSatz.Nachweis` führt „· p_I = x,x %/a". Damit wachsen die Annahmenzeilen von Word,
  Excel, Dialog und Seite aus **einer** Quelle mit.
- Der Satz „Ersatzbeschaffungen nominal konstant" im Parameternachweis des Word-Berichts war bis
  hierher richtig und ist es jetzt nur noch bei p_I = 0. Er heißt deshalb „Ersatzbeschaffungen
  preisindiziert mit p_I (VDI 2067)" bzw. „… nominal konstant (p_I = 0)".
- **G6** — der Freitext erscheint dreimal, jedes Mal **nur wenn gepflegt**: im Word-Bericht als
  Überschrift 2 + Absatz unmittelbar nach dem Vorschlagssatz (`WIRT_NM_TITEL`), in Excel als
  Zelle unter der Vorschlagszelle (`WIRT_NM_ZEILE`) und auf der Seite als eigene
  Herleitungszeile im Nachweisblock (`WirtschaftlichkeitStand.Wirkungszeile`, Muster
  `Vereinfachungszeile`). Eine leere Überschrift wäre keine Aussage, sondern eine Lücke mit
  Titel. Die Zeile hängt am **Stand** und nicht an der Ansicht: Sie folgt weder der Szenario-
  noch der Vergleichswahl.

### 4. Nachweise W5‑B‑12 Teil b

| Prüfung | Ort | Ergebnis |
|---|---|---|
| p_I ungepflegt → wirksam = p_B; das leere Feld zieht bei geändertem p_B mit, ein gepflegtes nicht | `PreisInvestitionTests.Ohne_eigenen_Satz_gilt_die_Preissteigerung_Betrieb` | grün (1,5 → 2,75 → 3,0) |
| Szenariovorgabe = Erwartet‑p_I ∓ 1 %‑Pkt — ohne gepflegtes p_I (p_B = 2 → Best 1,0 / Worst 3,0, **gleich dem wirksamen p_B**) und mit (p_I = 5 → Best 4,0 / Worst 6,0) | `…Die_Szenariovorgabe_spannt_sich_um_das_wirksame_Erwartet_p_I` | grün |
| gepflegter Szenariowert hat Vorrang und bleibt bei geändertem p_B stehen (7,5 %) | `…Ein_gepflegter_Szenariowert_schlaegt_die_Vorgabe` | grün |
| `NurVorgaben` kennt die siebte Größe; `Kopie` trägt sie mit | `…NurVorgaben_beruecksichtigt_das_neue_Feld` | grün |
| Nachweiszeilen: „p_I = 2,0 %/a" im Satz, „Investition/Ersatz 1,0 %/a (wie Betrieb)" bzw. „4,0 %/a (gepflegt)" im Parametersatz | `…Die_Nachweiszeilen_nennen_p_I` | grün |
| Speichern/Laden der vier Spalten über den echten Weg: 2,5 · „Versorgungssicherheit, Arbeitsschutz, Komfort" · Worst 4,25 · Best bleibt NULL — und zurück ins Leere | `…Die_vier_Spalten_ueberleben_Speichern_und_Laden` | grün |
| `StelleTabellenSicher` legt die vier Spalten nach einem `DROP COLUMN` wieder an | `…StelleTabellenSicher_legt_die_Spalten_des_Schritts_72_an` | grün |
| **Ende zu Ende über `Berechne`** (10.000 €, n = 8 a, T = 20 a, i = 3 %, p_B = 2 %): KW Erwartet **− 85.965,31 €** bei p_I = 0 gegen **− 88.611,47 €** bei p_I = p_B = 2 % — Δ **− 2.646,16 €** aus zwei indizierten Ersatzbeschaffungen (t = 8/16) und der höheren Restwert-Preisbasis | `…Mit_Ersatzbeschaffung_senkt_p_I_den_Erwartet_Kapitalwert` | grün |
| ohne Ersatzbeschaffung (n < 1) ändert p_I nichts: **− 74.607,92458136652 €** beidseitig, **bitgleich** | `…Ohne_Ersatzbeschaffung_aendert_p_I_nichts` | grün |
| **Regression**: p_B = 0 und p_I NULL → wirksames p_I = 0 → **− 85.965,30754577533 €** beidseitig, bitgleich zum Stand vor W5‑B‑12 (mit Ersatz!) | `…Ohne_Preissteigerung_Betrieb_bleibt_der_Erwartungsfall_bitgleich` | grün |
| Dialog: sieben Zeilen × drei Spalten, 14 Eingabefelder | `WirtschaftlichkeitParameterDialogTests.Die_Szenariotabelle_zeigt_drei_Spalten_und_sieben_Groessen` | grün |
| Dialog: p_I-Vorgaben Best 0,50 / Worst 2,50 bei p_B = 1,5 | `…Die_Szenariofelder_zeigen_die_wirksamen_Vorgaben` | grün |
| Dialog: leeres p_I zeigt „1,50 %/a (wie Betrieb)", gepflegtes „3,00 %/a (gepflegt)", Leeren führt zurück | `…Ein_leeres_p_I_rechnet_wie_die_Preissteigerung_Betrieb` | grün |
| Dialog: gepflegtes p_I = 4,0 verschiebt die Szenariofelder auf 3,00 / 5,00 | `…Ein_gepflegtes_p_I_verschiebt_die_Szenariovorgaben` | grün |
| Dialog: „Vorgaben" setzt vierzehn Felder zurück, p_I eingeschlossen | `…Vorgaben_setzt_die_vierzehn_Felder_zurueck` | grün |
| Dialog: Herleitungszeilen nennen p_I (Best 0,5 / Worst 2,5 %/a) | `…Die_Herleitungszeilen_nennen_beide_Saetze` | grün |
| Dialog: der Freitext landet im Parametersatz | `…Der_Freitext_der_nicht_monetaeren_Wirkungen_wird_uebernommen` | grün |
| Sandbox-Bau `WP-Plan.sln` x64 Debug | `K:\imp2\src` (= HEAD `4a1216b8` + 16 Dateien) | **0 Fehler** |
| `EPOS.Kern.Tests` / `EPOS.UI.Tests` | `dotnet test --no-build` | **2 195/2 195** / **3 362/3 362** |

Ausgangsstand derselben Sandbox ohne diese Etappe: 2 185 / 3 359 — die 10 neuen Kern- und
3 neuen UI-Fälle sind vollständig zugeordnet.

**Ein Referenzlauf war nicht nötig:** Die Etappe fasst keinen Simulationswert an. An seine Stelle
tritt die Bitgleichheitsprobe des Erwartungsfalls bei p_B = 0 (12 gültige Stellen, ohne
Toleranzfenster) und die zweite ohne Ersatzbeschaffung.

**Bestehende Erwartungswerte angepasst:** keine Zahl. Angepasst wurden ausschließlich
**Feldzählungen und Indizes** im Parameterdialog (`WirtschaftlichkeitParameterDialogTests`):
`SZENARIO_FELDER` 12 → 14, dazu die neue Konstante `ALLGEMEIN_FELDER = 4`, die Gruppenliste um
„Bewertung nach DIN EN 17463" und die Zeilenindizes der Szenariotabelle um eins nach unten
(Investition 3 → 4, Erträge 4 → 5, Nutzungsdauer 5 → 6). Der Fall
`Die_Szenariotabelle_zeigt_drei_Spalten_und_sechs_Groessen` heißt jetzt
`…_und_sieben_Groessen`, `Vorgaben_setzt_die_zwoelf_Felder_zurueck` heißt
`Vorgaben_setzt_die_vierzehn_Felder_zurueck`.

**`SzenarioParameterTests` blieb unberührt** — insbesondere
`Erwartet_bleibt_zahlengleich` und `Best_Erwartet_und_Worst_liegen_auseinander`. Beide rufen
`KapitalwertRechner.Rechne` unmittelbar auf (ohne den p_I-Parameter) und vergleichen zwei Läufe
DERSELBEN Fassung; p_I kann dort weder wirken noch die Erwartung verschieben.

### Offene Punkte

- **Sichtabnahme**: im Parameterdialog das p_I-Feld samt Herleitungszeile unter „Allgemein", die
  siebte Zeile der Szenariotabelle (Spaltenbreiten auf schmalen Fenstern) und der neue Abschnitt
  „Bewertung nach DIN EN 17463" mit dem mehrzeiligen Feld; im Word-Bericht der Block „Nicht
  monetäre Wirkungen" nach dem Vorschlagssatz und der geänderte Satz im Parameternachweis; im
  Excel-Blatt die Zelle unter der Vorschlagszelle; auf der Seite die neue Herleitungszeile.
- **Bestandsprojekte MIT Ersatzbeschaffung und p_B ≠ 0 rechnen ab jetzt mit p_I = p_B**; ihre
  Kapitalwerte sinken leicht (im Nachweis oben − 2.646,16 € bei 10.000 € und zwei Ersätzen). Das
  ist gewollt — der bisherige Ausweis war der zu günstige (Vereinfachung W1, Lücke G4). Projekte
  **ohne** Ersatzbeschaffung und alle Projekte mit p_B = 0 bleiben zahlengleich. Wer den alten
  Ausweis will, trägt p_I ausdrücklich mit 0 ein.
- Die Doku-Aufzählung in `SchemaMigration.SCHRITTE_SQLITE` listet die Schritte weiterhin nur bis
  69 — 70, 71 und 72 fehlen dort; Nachzug bei Gelegenheit (schon in Teil a vermerkt).
