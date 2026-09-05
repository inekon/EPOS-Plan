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
