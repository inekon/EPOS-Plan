# Konzept: Administrationsdialoge neu ordnen — ein Rollbereich je Spalte, Stammblatt, Übernahme aus der Zeile

**Stand:** Alle acht Fragen sind entschieden (Abschnitt 4); umgesetzt ist noch nichts, die
Reihenfolge steht in Abschnitt 5.
**Anlass:** Anwender, 22.09.2026, mit zwei Screenshots (Dialog „Administration Heizkessel", Menü
„Administration"): „Die Administrationsdialoge haben ein benutzerunfreundliches Schema und Bedienung
(Beispiel Heizkessel). Insbesondere die verschachtelten Scrollbars sind nicht gut passend. Die Auswahl
an Anlagen/Komponenten in das Projekt ist umständlich. Erstelle ein Beispiel-Mockup und Vorschläge."
**Mockup:** [`Mockups/Administration_Heizkessel_Neuordnung.html`](Mockups/Administration_Heizkessel_Neuordnung.html)
— Reiter „Heute" (Nachbau mit echten Rollbereichen), A Katalog und Stammblatt, B Mehrfachauswahl,
C schmales Fenster, D Im Projekt; jeder Rahmen in der Fenstergröße des Anwenders.
**Geltungsbereich:** Anordnung und Bedienung der Katalog- und Projektdialoge. Kein Rechenweg, kein
Schema, keine Testdatenbank — kein Referenzlauf.
**Es gilt weiter:** [Konzept Katalogfilter](Konzept_Katalogfilter_EPOS-Plan.md) (Spaltenmodell,
Kapitel 5.6) und [Konzept Knopfleisten](Konzept_Knopfleisten_Administration_EPOS-Plan.md). Die
Vorschläge 1, 3 und 5 ändern Anordnungsregeln aus [`EPOS.UI/CLAUDE.md`](../../EPOS.UI/CLAUDE.md);
3 und 5 sind dafür mit AD-Q1 und AD-Q2 entschieden (Abschnitt 4).


## 1 Befund

### 1.1 Zwei Dialoge hinter einem Gerät

- **Verwaltung** (Menü Administration → Wärmebedarf & Heizung → Heizkessel, die Screenshots):
  `KatalogBrowserDialog`, Ausprägung Heizkessel (`KatalogBrowserProfil`), Hülle `HeizkesselAdminHuelle`
  → `KatalogBrowserHuelle`, eigenes Fenster `BlazorDialogForm`. Sie kennt kein Projekt.
- **Projektdialog** „Verwaltung Heizkessel" (Erzeugerkachel, Assistent): `HeizkesselDialog`, Hülle
  `HeizkesselHuelle.Oeffnen`. Nur hier kommt ein Kessel ins Projekt. Beide nutzen die `Katalogliste`.

### 1.2 Drei Rollbereiche ineinander

`Fenstermass` öffnet eine Fachmaske auf 85 % × 90 % des Arbeitsbereichs; bei 1 920 × 1 040
Gerätepixeln und 150 % Skalierung sind das **1 088 × 624 CSS-Pixel**. Fundstellen in
`EPOS.UI/wwwroot/epos-ui.css`:

| Nr | Rollbereich | Regel | Warum er rollt |
|---|---|---|---|
| ① | Dialog | `.epos-katalog-dialog { height: 100dvh; overflow: auto }` | Notnagel, sobald Kopf, Mindestliste (9 rem) und Fußleiste nicht ins Fenster passen |
| ② | Rahmen | `.epos-katalog-paar.epos-katalog-fuellend { overflow: auto }` (Baustein `Katalograhmen`) | Liste und Eingabeblock stehen bei jeder Breite untereinander. Die Liste ist bis 457,6 px hoch, der Rahmen hat im Fenster rund 470 px — der Block „Info markierter Kessel" liegt ganz darunter. Gemessene Dialoghöhe 1 152 bis 1 228 px (Konzept Katalogfilter 5.6.5) gegen 624 px Fenster |
| ③ | Liste | `.epos-raster-huelle { overflow: auto }`, im Rahmen gedeckelt auf `calc(var(--epos-listenhoehe) * 1.3)`; `.epos-katalog-liste { min-width: 0 }` lässt sie waagerecht rollen | senkrecht: sieben Zeilen zu 53 px (`Raster.ZEILENHOEHE`: 44-px-Wahlknopf plus Polsterung). Waagerecht: Wahlspalte und sechs Parameterspalten des Heizkesselprofils (`Katalogfilterprofil`: Bezeichner, Hersteller, Brennstoff, P_th, η, Brennwert) mit Sortierknopf und Trichter im Kopf passen nicht in rund 1 040 px. „Sechs bis neun Spalten ohne Querrollen" ist bei 1 366 px Fensterbreite gemessen |

Die Höhengrenze von ③ ist keine Zierde: QuickGrid virtualisiert nur in einem Behälter fester Höhe
(`EPOS.UI/CLAUDE.md`, „Fallstricke der Virtualisierung"). Die Fußleiste steht nach dem Stilblatt unter
dem Rahmen; im Screenshot ist sie angeschnitten. Ob dort ① greift, klärt die Katalogprobe
(`Proben/Rasterprobe`) mit einem Fall 1 088 × 624, bevor Vorschlag 1 gebaut wird.

Der **Projektdialog** trägt keine Höhengrenze (`.epos-dialog` ohne `epos-katalog-dialog`) und rollt als
ganze Seite. Über der Katalogliste (22 rem = 352 px) stehen die Projektliste (12 rem = 192 px, Baustein
`Zweispaltenauswahl`) und die Übernahmeleiste; aus den Stilregeln gerechnet endet das Fenster in der
Katalogliste, der Block „Modul" beginnt erst darunter.

### 1.3 Was unter der Liste liegt

- **Verwaltung:** Gruppenkopf „Info markierter Kessel" mit dem Weg „Berechnungsweg Heizkessel",
  darunter `Katalogfelder` mit **21 Feldern** (bearbeitbar; Technik, Kosten, Emissionen) und die
  aufklappbare `Parameteruebersicht`. Fußleiste: Speichern · Füller · Neu… · Bearbeiten… · Löschen ·
  OK — die vier Knöpfe rechts vom Füller sind die im Screenshot angeschnittenen.
- **Projektdialog:** unter der Katalogliste „Löschen", dann Gruppenkopf „Modul" mit der Knopfzeile
  Investitionskosten… · Betriebskosten… · Energiekosten… · Füller · Bearbeiten…, der Berechnungshilfe,
  einem `Formularraster` (Name, Detailfelder lesend, Beschreibung, Brennwert; bei einer gewählten
  Projektzeile Trägerwahl, Vorlauf, Rücklauf), dem Aufklapper „Alle Daten" (`Katalogfelder`,
  bearbeitbar, eigener Knopf „Felder speichern") und der `SpeichernLeiste` Abbrechen · OK.

### 1.4 Der Übernahmeweg heute

Im Projektdialog, ein Gerät je Durchgang:

1. In der Katalogliste den runden Wahlknopf treffen (`Zeilenwahl` in der Wahlspalte) — die Zeile
   selbst wählt nicht.
2. Zurückrollen: „▲ In das Projekt übernehmen" steht **über** der Katalogliste.
3. Knopf drücken (`BeiHinzu`); es erscheint die Trägerwahl als Überlagerung
   (`EnergietraegerVarianteDialog`). Abbruch heißt: keine Aufnahme.
4. `Aufnehmen` legt Trägervariante und Projektkopie sofort an; die Zeile erscheint oben in
   „ausgewählt im Projekt".
5. Vorlauf, Rücklauf und Kosten stehen im Block „Modul" unter der Katalogliste — wieder rollen.
6. OK; die Hülle schreibt die Projektliste.

Ob ein Katalogsatz schon im Projekt steht, sagt die Spalte „im Projekt verwendet" des Profils
`MitVerwendung` — eine weitere Spalte in einer Liste, der es schon an Breite fehlt.

### 1.5 Dialoge mit demselben Schema

| Familie | Komponente | Menüpunkte bzw. Einstieg |
|---|---|---|
| Verwaltung, `Katalograhmen` (sieben Komponenten, vierzehn Menüpunkte) | `KatalogBrowserDialog` | Heizkessel, BHKW, Solarkollektoren, Pufferspeicher |
| | `ModulKatalogDialog` | PV-Module, Wechselrichter, Stromspeicher |
| | `WaermepumpeStammDialog`, `KlimadatenDialog` | Wärmepumpen, Klimadaten |
| | `BedarfAdminDialog`, `WaermebedarfAdminDialog`, `SolarganglinieAdminDialog` | Brauchwasser, Prozesswärme, Stromverbraucher; Wärmebedarf extern; Solarthermie-Ganglinie |
| Projekt, `Zweispaltenauswahl` + `Katalogliste` (zwölf Komponenten) | Erzeuger | `HeizkesselDialog`, `BhkwDialog`, `WaermepumpenDialog`, `SolarkollektorenDialog`, `PufferspeicherDialog`, `PhotovoltaikDialog`, `StromspeicherDialog` |
| | Bedarf und Zeitreihen | `GebaeudeDialog`, `WaermebedarfExternDialog`, `BedarfsProfileDialog`, `SolarganglinieDialog`, `StromganglinieDialog` |

Nicht in diesem Schema: Kostenvorlagen, Energieträger, Nutzungsdauer, Gesetzesparameter,
Einstellungen, Gebäudetypen. Die Katalogimporte nutzen die `Katalogliste` und gewinnen durch die
Vorschläge 2 und 4 mit.


## 2 Zielbild in sieben Vorschlägen

Aufwand: S bis ein Tag, M wenige Tage, L eine Woche und mehr.

**V1 — Das Gerüst: Kopf und Fuß stehen, die Arbeitsfläche füllt die Höhe.** *Was:* Der Dialog füllt das
Fenster; Kopf (Titel, Hilfe, ✕), Werkzeugzeile (Umschalter, Suche, Trefferzahl) und Fußleiste stehen
fest, dazwischen eine Arbeitsfläche mit höchstens einem Rollbereich je Spalte, nie einer im anderen.
Die Liste nimmt die Resthöhe (`flex: 1; min-height: 0`) statt einer Höchsthöhe; ① und ② entfallen,
① bleibt nur als Notnagel unter dem Kleinstmaß. *Warum:* Die drei Rollbereiche ineinander sind der
Kern der Beschwerde; die Fußleiste ist immer sichtbar. *Betroffen:* Baustein `Katalograhmen` und
damit alle sieben Verwaltungskomponenten; die Projektdialoge über V5. *Aufwand:* M.
*Abhängig:* Katalogprobe und Rasterprobe (Virtualisierung braucht weiter einen Behälter fester
Höhe — den gibt die Resthöhe), die Regeln „LISTE in festem Rahmen" und „KATALOGDIALOG nutzt die Höhe"
in `EPOS.UI/CLAUDE.md`.

**V2 — Keine waagerechte Rollleiste: Spalten mit Rang.** *Was:* Jede `Katalogspalte` bekommt einen Rang
(1 immer, 2 bei Platz, 3 nur im Stammblatt); eine Containerabfrage der Komponente blendet nach Breite
aus. Der Bezeichner ist die elastische Spalte mit Auslassung („…") und vollem Namen im Kurztext, Zahlen
stehen rechtsbündig mit fester Breite. Eine Spalte mit gesetztem Filter wird nie ausgeblendet; die
Suche findet weiter in allen Spalten. *Warum:* Querrollen versteckt genau die Kennwerte, nach denen
man wählt. *Betroffen:* `Katalogfilterprofil` (Kern) und `Katalogliste` — alle 25 Wirte der Liste.
*Aufwand:* M. *Abhängig:* Rasterprobe (Zeilen dürfen nicht umbrechen, das Zeilenmaß bleibt gesetzt),
Konzept Katalogfilter 5.6.5 (die fachliche Spaltenwahl bleibt, nur ihr Rang ist neu).

**V3 — Stammblatt neben der Liste statt Detailblock darunter.** *Was:* Ab 900 CSS-Pixeln Dialogbreite
steht rechts das Stammblatt (`clamp(340px, 36 %, 440px)`): Name, Hersteller, drei Kennzahlen, die Knöpfe
zum Gerät, die wichtigen Felder im `Formularraster` (einspaltig), „Alle Daten" als Aufklapper. Darunter
steht die Liste allein, eine Auswahlleiste nennt das gewählte Gerät, und das Stammblatt schiebt sich
als Blatt über die Liste („‹ Liste" und Esc führen zurück). *Warum:* Der Detailblock liegt heute
außerhalb des Fensters. Die Liste braucht die ganze Breite nur, weil sie alle Spalten zeigt — mit V2
genügen ihr bei 1 088 px Fensterbreite rund 665 px für Bezeichner, Hersteller, Brennstoff,
Leistung und die Projektspalte (im Mockup gemessen). *Betroffen:* `Katalograhmen` einmal, damit die
Verwaltungen; die Projektdialoge über V5; die iOS-Hülle erbt die schmale Anordnung. *Aufwand:* M.
*Abhängig:* Katalogprobe mit den Fällen 1 088 × 624 und 400 × 624. *Entschieden:* AD-Q1 (ersetzt
„Liste über die ganze Breite, Eingabe darunter").

**V4 — Die Zeile ist die Wahl.** *Was:* Klick oder Berührung auf die Zeile wählt, ↑ ↓ Pos1 Ende bewegen
die Wahl (ein Tabulatorhalt für die Liste), die gewählte Zeile trägt Fläche und linken Balken und
`aria-selected`. Die Wahlspalte mit dem runden Knopf entfällt; das Kästchen der Mehrfachwahl (V6) bleibt
eigene Spalte. Die Zeile wird 46 statt 53 px hoch (Berührungsziel 44 px). *Warum:* Der kleine runde
Knopf ist das einzige Klickziel einer 1 000 px breiten Zeile; Strg-Klick zum Markieren ist unsichtbar.
*Betroffen:* `Katalogliste`, `Zeilenwahl`, `Raster` — alle Wirte. *Aufwand:* M. *Abhängig:* QuickGrid
kennt keinen Zeilenklick (Klickfläche in jeder Zelle, kein `display: flex` auf `<td>`), neues Zeilenmaß
als `ItemSize` und `--epos-rasterzeile` samt Rasterprobe, Fokusführung mit bunit.

**V5 — Übernahme aus der Zeile, Umschalter „Katalog | Im Projekt (n)".** *Was:* Im Projektdialog ersetzt
ein Umschalter die gestapelten Listen samt Übernahmeleiste. Im Katalog trägt jede Zeile am Ende einen
immer sichtbaren Knopf „＋" (Aktionsspalte mit Kopf „Im Projekt"), dieselbe Handlung steht im Stammblatt;
„✓" bzw. „✓ 2×" ersetzt die Spalte „im Projekt verwendet". Die Ansicht „Im Projekt" zeigt die
Projektzeilen mit Energieträger, Vorlauf/Rücklauf und „Entfernen" je Zeile; ihr Stammblatt trägt, was
heute im Block „Modul" steht (Kostenknöpfe, Trägerwahl, Vorlauf, Rücklauf). Die Zahl am Umschalter und
die Statuszeile der Fußleiste melden jede Übernahme. Die Trägerwahl bleibt vor der Aufnahme; geschrieben
wird wie heute. *Warum:* Wählen, Übernehmen und die Projektfelder liegen dann dort, wo man hinsieht —
vier statt sechs Schritte, kein Rollen. *Betroffen:* die sieben Erzeuger-Projektdialoge, danach die fünf
aus Bedarf und Zeitreihen; Assistentenbetrieb dieselben Komponenten. *Aufwand:* L. *Abhängig:*
neuer Baustein statt `Zweispaltenauswahl`, Hausregel „Aktionsknöpfe einer Tabellenzeile immer
sichtbar", V1 bis V4. *Entschieden:* AD-Q2 (ersetzt „Projekt ↔ Datenbank immer über
`Zweispaltenauswahl`").

**V6 — Mehrfachwahl, Sammelübernahme, Vergleich im Stammblatt.** *Was:* Das Kästchen markiert
(Kopfkästchen = alle sichtbaren); solange etwas markiert ist, steht über der Liste eine Sammelleiste
„n markiert · ＋ n Geräte ins Projekt · Vergleichen · Markierung aufheben". Die Trägerwahl erscheint bei
der Sammelübernahme einmal je Brennstoff. „Vergleichen" zeigt zwei bis drei Geräte im Stammblatt statt
in einer Überlagerung, ab vier Geräten die breite Überlagerung von heute. Löschen bleibt
Einzelhandlung mit Rückfrage. *Warum:* Ein Projekt mit mehreren Kesseln braucht heute je Gerät den
ganzen Weg aus 1.4. *Betroffen:* `Katalogliste` (Mehrfachmodus und Vergleich sind da), Projektdialoge.
*Aufwand:* M. *Abhängig:* V4, V5, Baustein `Zeilenmarkierung`. *Entschieden:* AD-Q4.

**V7 — Suche und Filter: Bestand, eine Ergänzung.** *Was:* Suchfeld über alle Spalten, Trefferzahl,
Trichter im Spaltenkopf und „Filter zurücksetzen" bleiben, wie im Konzept Katalogfilter entschieden;
Filterchips über der Liste kommen nicht zurück. Neu ist nur die Regel aus V2: Eine gefilterte Spalte
bleibt sichtbar, damit ihr gefüllter Trichter den Filter anzeigt. „Vergleichen" bleibt Nebenaktion in
der Werkzeugzeile. *Warum:* Die Beschwerde betrifft Rollen und Übernahme, nicht den Filter.
*Betroffen:* `Katalogliste`. *Aufwand:* S. *Abhängig:* V2.


## 3 Was bleibt

Die Fußleisten nach dem Konzept Knopfleisten (Verwaltung: Speichern · Füller · Neu… · Bearbeiten… ·
Löschen · OK; Projektdialog: Status · Füller · Abbrechen · OK; ✕ und Esc wie Abbrechen), Berührungsziele
ab 44 px, immer sichtbare Zeilenknöpfe, Enter unbelegt, wo ein Knopf sofort schreibt, Filter vor dem
Raster, Markierung am Bezeichner, gesetztes Zeilenmaß — und die Schreibwege: Aufnahme und Trägervariante
beim Übernehmen, Projektliste beim OK, Katalogfelder über ihren eigenen Speicherweg.


## 4 Entscheide

Alle acht Fragen sind am **22.09.2026** entschieden: nach Empfehlung, mit einer Abweichung bei
AD-Q5 — Katalogfelder ändern dort nur die Projektkopie, nicht den Katalogsatz.

| Kennung | Frage | Empfehlung | Entscheid |
|---|---|---|---|
| **AD-Q1** | Stammblatt **neben** der Liste ab 900 px Dialogbreite, darunter als Blatt über der Liste (V3)? Das ersetzt „Liste über die ganze Breite, Eingabe darunter". | Ja — mit den Spaltenrängen aus V2 reicht die Breite, und der Detailblock ist ohne Rollen sichtbar | **22.09.2026: Ja** — Stammblatt neben der Liste ab 900 px Dialogbreite, darunter schmal als Blatt über der Liste (V3) |
| **AD-Q2** | Projektdialoge: Umschalter „Katalog \| Im Projekt (n)" mit „＋" je Zeile statt `Zweispaltenauswahl` (V5)? | Ja, zuerst im Heizkesseldialog als Pilot | **22.09.2026: Ja** (V5) |
| **AD-Q3** | Doppelklick auf eine Katalogzeile: Übernehmen (Projektdialog), Bearbeiten (Verwaltung) oder nichts? | Projektdialog: Übernehmen; Verwaltung: nichts, das Stammblatt zeigt schon alles. Enter bleibt unbelegt | **22.09.2026:** Im Projektdialog wirkt der Doppelklick wie „＋" (Übernahme, danach die Trägerwahl); in der Verwaltung tut er nichts, dort zeigt die Zeilenwahl (V4) schon das Stammblatt |
| **AD-Q4** | Mehrfachwahl mit Sammelübernahme (V6) — und die Trägerwahl einmal je Brennstoff statt je Gerät? | Ja zu beidem | **22.09.2026: Ja** zu beidem (V6) |
| **AD-Q5** | Stammblatt im Projektdialog: Katalogfelder unter „Alle Daten" weiter bearbeitbar oder nur zeigen? | Bearbeitbar lassen (so gilt es heute), mit der Zeile „Ändern schreibt den Katalogsatz für alle Projekte" | **22.09.2026: Ja**, die Katalogfelder der Projektkopie bleiben bearbeitbar — abweichend von der Empfehlung schreibt „Ändern" aber nicht den Katalogsatz: Stammfelder ändern nur das Projekt (Regel seit der Projektkopie der Wärmepumpe), Übernahme in den Katalog ist eine eigene Funktion |
| **AD-Q6** | Verwaltung: Braucht es „Bearbeiten…" (Katalogeditor mit Überschreiben und Speichern unter) neben dem bearbeitbaren Stammblatt noch? | Nein — „Überschreiben" leistet schon „Speichern" in der Fußleiste, das das Stammblatt schreibt; „Speichern unter" wird „Kopieren…" neben „Neu…" | **22.09.2026: Entfällt** — die Felder sind direkt bedienbar mit Speichern/Verwerfen |
| **AD-Q7** | Darf die Verwaltung aus dem Menü „＋ Ins Projekt" anbieten, wenn ein Projekt offen ist? | Nein — ein Weg je Ziel; die Verwaltung bleibt Katalogpflege | **22.09.2026: Nein** — Katalog und Projekt bleiben getrennt, Übernahme nur im Projektdialog |
| **AD-Q8** | Reihenfolge der Umsetzung (Abschnitt 5)? | wie vorgeschlagen | **22.09.2026: wie vorgeschlagen** — Stufe 1 alle Verwaltungen über den gemeinsamen Rahmen (V1, V2, V7), Stufe 2 Zeilenwahl (V4), Stufe 3 Heizkessel-Projektdialog als Pilot (V3, V5, V6), Stufe 4 übrige Erzeuger, Stufe 5 Bedarf und Zeitreihen (Abschnitt 5) |


## 5 Reihenfolge und Abnahme

| Stufe | Inhalt | Aufwand | Voraussetzung |
|---|---|---|---|
| 1 | V1 + V2 + V7 im `Katalograhmen` und in der `Katalogliste`: alle sieben Verwaltungskomponenten auf einmal, Pilot „Administration Heizkessel"; AD-Q6 im `KatalogBrowserDialog` | M + M + S | AD-Q6 |
| 2 | V4 in `Katalogliste`, `Zeilenwahl`, `Raster` — alle Wirte | M | Stufe 1 |
| 3 | V3 + V5 + V6 im `HeizkesselDialog` (Pilot) | M + L + M | AD-Q1 bis AD-Q5, Stufe 2 |
| 4 | die übrigen sechs Erzeuger-Projektdialoge | L | Stufe 3 |
| 5 | Bedarf und Zeitreihen (fünf Projektdialoge) | M | Stufe 4 |

**Abnahme je Stufe:** Katalogprobe und Rasterprobe mit den Fällen 1 088 × 624 und 400 × 624 (kein
Rollbereich in einem Rollbereich, keine waagerechte Überbreite der Liste, Fußleiste im Fenster, Zeilenmaß
gleich `ItemSize`); bunit je Komponente (Wahl per Zeile und Tastatur, Übernahme, Ergebnis und `null`
bei Abbruch, Fall ohne Gaben); `KnopfleistenWacheTests` und `SchliesskreuzWacheTests` grün; die
Windows-Schale auf Linux gebaut, wo eine Hülle angefasst wird.

**Papiere im selben Schritt:** die drei Anordnungsregeln in `EPOS.UI/CLAUDE.md` („LISTE in festem
Rahmen", „Projekt ↔ Datenbank immer über `Zweispaltenauswahl`", „KATALOGDIALOG nutzt die Höhe"), ein
Nachtrag im Konzept Katalogfilter 5.6.1, die Bedienungsseiten unter `Projekte/Wiki/` mit je einem
Logbuch-Satz.
