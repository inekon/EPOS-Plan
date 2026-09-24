# Konzept: Administrationsdialoge neu ordnen — ein Rollbereich je Spalte, Stammblatt, ein Schema für die Verwaltungen

**Stand:** AD-Q1 bis AD-Q9 sind entschieden, Variante B ist die Grundlage (6.1); mit AD-Q11 ist am
23.09.2026 auch die letzte offene Frage entschieden (6.2). Mit AD-Q12 bis AD-Q14 hat der Anwender am
22.09.2026 den Geltungsbereich auf die Verwaltungsdialoge der fünf Menügruppen Gebäude, Klimadaten,
Wärmebedarf & Heizung, Strombedarf & Speicher und Energiesysteme eingegrenzt (achtzehn Menüpunkte, elf
Komponenten, 1.4), das Kennzeichen auf das Schloss beschränkt und den Mockup-Reiter „Schmal"
gestrichen. **Stufe 1 ist umgesetzt** (23.09.2026, Commits `c8e5f775`, `d9ef80b8`): V1, V2 und V7 im
`Katalograhmen` und in der `Katalogliste`, dazu AD-Q6 im `KatalogBrowserDialog`. **Stufe 2 ist
umgesetzt** (23.09.2026, Commits `5767e273`, `e2fbb829`, Merge `f6290028`): V4, V10, V11 und V15 in
`Katalogliste`, `KatalogBrowserDialog` und `ModulKatalogDialog`, dazu AD-Q11 mit dem Kern-Baustein
`Katalogkopie.Duplizieren`. **Stufe 3 ist umgesetzt** (23.09.2026, Commits `d0660247`, `d8660fde`): V8,
V9, V12 und V13 als Bausteine (`Auswahlleiste`, `Stammblatt`, `Vergleichstabelle`), V6 (Kästchenspalte)
und V3 (Stammblatt neben der Liste) in A1 bis A3 und A5 — Heizkessel als Pilot, dieselbe Komponente für
BHKW, Solarkollektoren, Pufferspeicher, Wärmepumpe (mit Gruppe Kennlinie), PV-Module, Wechselrichter,
Stromspeicher und Bedarfsprofile. **Stufe 4 ist umgesetzt** (23.09.2026, Commits `5a46b0b1`,
`b1569752`, Merge `df78272c`): V14 an A4 und A6 bis A8 — Klimadaten und die drei Zeitreihen im
Stammblatt mit Einlesen als Überlagerung —, dazu die Reste aus Stufe 3 bis auf den Zweitweg von V14 bei
A1 bis A3. **Stufe 5 ist umgesetzt** (23.09.2026, Commits `9cb41354`, `7e7ecb9c`, `c938ed32`, Merge
`b3fa8658`): V16 an den Sonderlisten (A9 bis A11) — Gebäude, Gebäudetypen und Lastspitzenkappung im
Gerüst mit Liste und Stammblatt —, dazu der Zweitweg von V14 bei A1 bis A3 (7.1 d, „Import…" in den
Gerätekatalogen). **Damit sind alle fünf Stufen umgesetzt**; einzelne Reste stehen in 7.1 („Offen nach
Stufe 5"), die Import-Reste (d) sind am 24.09.2026 erledigt (Welle #466), ebenso die Frage (c) der
Lastspitzenkappung: Entscheid „Werkzeugleiste", umgesetzt mit Welle #467; mit #483 stehen die beiden
Knöpfe im schmalen Fenster zusätzlich im Kopf des Stammblatts. Die Auswahlleiste steht im breiten Fenster über dem Stammblatt statt über der Liste —
Abweichung vom Schema, Begründung in 3.6 Punkt 7. Das Schema (Abschnitt 3) gilt als umgesetzter Stand
für alle elf Komponenten des Geltungsbereichs, mit den in 3.6 genannten Abweichungen. Der Ablauf aller
fünf Stufen steht in Abschnitt 7; was nicht mehr Gegenstand ist, fasst Abschnitt 8 zusammen.
**Mit AD-Q15 (23.09.2026) ist AD-Q11 abgelöst:** Das Schloss eines Auslieferungssatzes lässt sich in
allen zehn Verwaltungen über die Auswahlleiste aufheben und wieder setzen, nach Rückfrage — umgesetzt
am 23.09.2026 (Welle #459; Kern `Auslieferungskennzeichen`, Baustein `Schlossumschaltung`; 3.4, 6.2).
**Mit #465 (24.09.2026) ist die Gebäudeverwaltung (A9) nachgezogen:** Ihr Stammblatt führt jedes Feld
des Katalogeditors auf demselben Arbeitsstand, und beim Hilfe-Assistenten ist sie eine eigene Maske
(7.1 a und e erledigt). **Mit #468 (24.09.2026) erkennt die Löschsperre ein benutztes Gebäude über
den Katalogverweis** `Tab_Gebaeude.ID_Gebaeude_Stamm` (Schemaschritt 121) und erst ohne ihn über den
Namen (7.1 a); **mit #473 (24.09.2026)** legt auch das Neuschreiben der Gebäudeliste eines Projekts die
Kopien über diesen Verweis an (7.1 a); **mit #475 (24.09.2026)** behält das Speichern der Gebäudeliste
jede unveränderte Projektkopie samt Feld-Übernahmen, und die Startseite schreibt in einem Vorgang
(7.1 a); **mit #485 (24.09.2026)** berichtigt Schemaschritt 126 die vorgelegten Gebäude-Katalogsätze
(7.1 a); **mit #487 (24.09.2026)** tragen gespeicherte Zeilen der Gebäudeliste ihre echte Id, das
Änderungsdatum folgt nur einer echten Änderung, und „Gebäude in DB löschen" des Projektdialogs hält
die Löschsperre der Verwaltung (7.1 a); **mit #490 (24.09.2026)** gleicht auch der Bearbeiten-Zweig
des Assistenten seine übrigen Gewerke ab — ein Speichern ohne Eingabe schreibt nichts (7.1 a).
**Anlass:** Anwender, 22.09.2026, mit zwei Screenshots (Dialog „Administration Heizkessel", Menü
„Administration"): „Die Administrationsdialoge haben ein benutzerunfreundliches Schema und Bedienung
(Beispiel Heizkessel). Insbesondere die verschachtelten Scrollbars sind nicht gut passend. Die Auswahl
an Anlagen/Komponenten in das Projekt ist umständlich. Erstelle ein Beispiel-Mockup und Vorschläge."
Dazu am selben Abend: „Mockup: Administration_Heizkessel_Neuordnung.html — Variante B
(Mehrfachauswahl). Weitere Vorschläge auf dieser Basis. Ziel soll sein: einheitliches und
übersichtliches Design auch für die anderen Administrationsdialoge." Und zum Schema-Mockup, mit einem
Screenshot des Menüs „Administration": „Hinweis ‚Auslieferung' zu groß, evtl. nur das
Schloss-Symbol." — „Wozu ist Dialog ‚Schmal · Wärmepumpe'?" — „Die Dialoge im Mockup sollen nur auf
die Auswahlen des Menüs beschränkt sein; die anderen Dialoge eignen sich nicht für das vorgeschlagene
Schema. Stelle im Mockup alle Dialoge dieser Auswahl dar (auch Solarthermie, Wärmepumpe …)."
**Mockups:** [`Mockups/Administration_Heizkessel_Neuordnung.html`](Mockups/Administration_Heizkessel_Neuordnung.html)
— Reiter „Heute" (Nachbau mit echten Rollbereichen), A Katalog und Stammblatt, B Mehrfachauswahl,
C schmales Fenster, D Im Projekt (D ist nicht mehr Gegenstand, Abschnitt 8).
[`Mockups/Administrationsdialoge_Schema.html`](Mockups/Administrationsdialoge_Schema.html) — Reiter
„Gerüst" (die sechs Zonen, das Kennzeichen, die Übersicht der achtzehn Menüpunkte) und je Menüpunkt
ein Reiter, geordnet wie das Menü nach den fünf Gruppen; jeder Dialog mit einem Kasten „gleich /
dialogspezifisch", jeder Rahmen in der Fenstergröße des Anwenders (1 088 × 624 CSS-Pixel).
**Geltungsbereich:** Anordnung und Bedienung der Verwaltungsdialoge, die das Menü Administration unter
Gebäude, Klimadaten, Wärmebedarf & Heizung, Strombedarf & Speicher und Energiesysteme öffnet (Liste
in 1.4). Nicht Gegenstand sind die Projektdialoge (Erzeugerkacheln, Assistent), Kosten, Daten & Import
und Einstellungen — Entscheid AD-Q12, Abschnitt 8. Kein Rechenweg, kein Datenbankschema, keine
Testdatenbank — kein Referenzlauf.
**Es gilt weiter:** [Konzept Katalogfilter](Konzept_Katalogfilter_EPOS-Plan.md) (Spaltenmodell,
Kapitel 5.6) und [Konzept Knopfleisten](Konzept_Knopfleisten_Administration_EPOS-Plan.md) (ein
primärer Schlussknopf zuletzt; die Belegung fasst V15 neu). Die Vorschläge 1 und 3 ändern
Anordnungsregeln aus [`EPOS.UI/CLAUDE.md`](../../EPOS.UI/CLAUDE.md); 3 ist dafür mit AD-Q1 entschieden.


## 1 Befund

### 1.1 Zwei Dialoge hinter einem Gerät

- **Verwaltung** (Menü Administration → Wärmebedarf & Heizung → Heizkessel, die Screenshots):
  `KatalogBrowserDialog`, Ausprägung Heizkessel (`KatalogBrowserProfil`), Hülle `HeizkesselAdminHuelle`
  → `KatalogBrowserHuelle`, eigenes Fenster `BlazorDialogForm`. Sie kennt kein Projekt.
- **Projektdialog** „Verwaltung Heizkessel" (Erzeugerkachel, Assistent): `HeizkesselDialog`, Hülle
  `HeizkesselHuelle.Oeffnen`. Nur hier kommt ein Kessel ins Projekt. Beide nutzen die `Katalogliste`.

Gegenstand dieses Konzepts ist die Verwaltung; der Projektdialog steht in Abschnitt 8. Was an der
`Katalogliste` geändert wird, erbt er trotzdem mit.

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

### 1.3 Was unter der Liste liegt

Gruppenkopf „Info markierter Kessel" mit dem Weg „Berechnungsweg Heizkessel", darunter `Katalogfelder`
mit **21 Feldern** (bearbeitbar; Technik, Kosten, Emissionen) und die aufklappbare
`Parameteruebersicht`. Fußleiste: Speichern · Füller · Neu… · Bearbeiten… · Löschen · OK — die vier
Knöpfe rechts vom Füller sind die im Screenshot angeschnittenen. Die übrigen Verwaltungen tragen dort
ihren eigenen Block: Einleseblock (Klimadaten, Zeitreihen), lesendes `Formularraster` (Bedarfsprofile,
Gebäude), Stundenfelder (Gebäudetypen), Parameterblock (Lastspitzenkappung) — Bestand in 3.5.

### 1.4 Die Dialoge des Geltungsbereichs

Quelle ist `EPOS.UI/Bausteine/Menuetabelle.cs`, Kopf „Administration": achtzehn Menüpunkte, elf
Komponenten. Die Spalte **Bestand** verweist auf die Zeile in 3.5.

| Menügruppe | Menüpunkt | Komponente (Ausprägung) | Bestand |
|---|---|---|---|
| Gebäude | Bearbeiten | `GebaeudeDialog`, Betriebsart Verwaltung | A9 |
| Gebäude | Gebäudetypen | `GebaeudetypDialog` | A10 |
| Klimadaten | Klimadaten (Punkt ohne Untermenü) | `KlimadatenDialog` | A4 |
| Wärmebedarf & Heizung | Brauchwasser | `BedarfAdminDialog` (Brauchwasser) | A5 |
| Wärmebedarf & Heizung › Profile & Lastgänge | Wärmebedarf Lastgang | `WaermebedarfAdminDialog` | A6 |
| Wärmebedarf & Heizung › Profile & Lastgänge | Prozesswärme | `BedarfAdminDialog` (Prozesswärme) | A5 |
| Wärmebedarf & Heizung › Profile & Lastgänge | Solarthermieganglinie | `SolarganglinieAdminDialog` | A7 |
| Wärmebedarf & Heizung | Heizkessel | `KatalogBrowserDialog` (Heizkessel) | A1 |
| Wärmebedarf & Heizung | BHKW | `KatalogBrowserDialog` (BHKW) | A1 |
| Wärmebedarf & Heizung | Wärmepumpen | `WaermepumpeStammDialog` | A3 |
| Wärmebedarf & Heizung | Solarkollektoren | `KatalogBrowserDialog` (Solarkollektoren) | A1 |
| Strombedarf & Speicher | Stromverbraucher | `BedarfAdminDialog` (Stromverbraucher) | A5 |
| Strombedarf & Speicher | Stromganglinie | `StromganglinieAdminDialog` | A8 |
| Strombedarf & Speicher | Stromspeicher | `ModulKatalogDialog` (Stromspeicher) | A2 |
| Strombedarf & Speicher | Lastspitzenkappung (Peak-Shaving) | `PeakShavingDialog` | A11 |
| Energiesysteme › Photovoltaik | PV Module | `ModulKatalogDialog` (PV) | A2 |
| Energiesysteme › Photovoltaik | Wechselrichter | `ModulKatalogDialog` (Wechselrichter) | A2 |
| Energiesysteme | Pufferspeicher | `KatalogBrowserDialog` (Pufferspeicher) | A1 |

Zwei Punkte pflegen keinen Gerätekatalog: die **Gebäudetypen** (Kopf und Detail — ein Typ führt fünf
oder acht Tageskurven zu 24 Stunden) und die **Lastspitzenkappung** (ein Rechenwerkzeug über einem
Lastgang, das nichts ablegt). Beide stehen im Menü und damit im Schema; wie sie sich einfügen, sagt 3.6.

Nicht in diesem Schema (AD-Q12): die Projektdialoge, die Menügruppen Kosten (darin auch der
Kostenfaktorenkatalog), Daten & Import und die Einstellungen — Abschnitt 8. Die Katalogimporte und die
Projektdialoge nutzen dieselbe `Katalogliste` und erben, was V2 und V4 an ihr ändern, ohne selbst
Gegenstand zu sein.


## 2 Zielbild in sieben Vorschlägen

Aufwand: S bis ein Tag, M wenige Tage, L eine Woche und mehr. Abschnitt 4 führt V1 bis V7 mit V8
bis V17 zu einem Schema weiter; regeln beide dieselbe Stelle, gilt die spätere Nummer. Die Nummern
bleiben stehen: V5 und V17 betreffen nur die Projektdialoge und entfallen (Abschnitt 8).

**V1 — Das Gerüst: Kopf und Fuß stehen, die Arbeitsfläche füllt die Höhe.** *Was:* Der Dialog füllt das
Fenster; Kopf (Titel, Hilfe, ✕), Werkzeugzeile (Suche, Trefferzahl) und Fußleiste stehen fest,
dazwischen eine Arbeitsfläche mit höchstens einem Rollbereich je Spalte, nie einer im anderen. Die
Liste nimmt die Resthöhe (`flex: 1; min-height: 0`) statt einer Höchsthöhe; ① und ② entfallen, ①
bleibt nur als Notnagel unter dem Kleinstmaß. *Warum:* Die drei Rollbereiche ineinander sind der Kern
der Beschwerde; die Fußleiste ist immer sichtbar. *Betroffen:* Baustein `Katalograhmen` und damit die
acht Komponenten, die ihn tragen (A1 bis A8, seit Stufe 1); A9 bis A11 seit Stufe 5 (V16).
*Aufwand:* M. *Abhängig:* Katalogprobe und Rasterprobe (Virtualisierung braucht weiter einen Behälter
fester Höhe — den gibt die Resthöhe), die Regeln „LISTE in festem Rahmen" und „KATALOGDIALOG nutzt die
Höhe" in `EPOS.UI/CLAUDE.md`. **Umgesetzt (23.09.2026).**

**V2 — Keine waagerechte Rollleiste: Spalten mit Rang.** *Was:* Jede `Katalogspalte` bekommt einen Rang
(1 immer, 2 bei Platz, 3 nur im Stammblatt); eine Containerabfrage der Komponente blendet nach Breite
aus. Der Bezeichner ist die elastische Spalte mit Auslassung („…") und vollem Namen im Kurztext, Zahlen
stehen rechtsbündig mit fester Breite. Eine Spalte mit gesetztem Filter wird nie ausgeblendet; die
Suche findet weiter in allen Spalten. *Warum:* Querrollen versteckt genau die Kennwerte, nach denen
man wählt. *Betroffen:* `Katalogfilterprofil` (Kern) und `Katalogliste` — die Verwaltungen des
Geltungsbereichs; weil der Baustein geteilt ist, erben die übrigen Wirte (Projektdialoge, Importe) die
Ränge mit. *Aufwand:* M. *Abhängig:* Rasterprobe (Zeilen dürfen nicht umbrechen, das Zeilenmaß bleibt
gesetzt), Konzept Katalogfilter 5.6.5 (die fachliche Spaltenwahl bleibt, nur ihr Rang ist neu).
**Umgesetzt (23.09.2026).**

**V3 — Stammblatt neben der Liste statt Detailblock darunter.** *Was:* Ab 900 CSS-Pixeln Dialogbreite
steht rechts das Stammblatt (`clamp(340px, 36 %, 440px)`): Name, Herkunft, drei Kennzahlen, die
wichtigen Felder im `Formularraster` (einspaltig), „Alle Daten" als Aufklapper. Darunter steht die
Liste allein, eine Auswahlleiste nennt die gewählte Zeile, und das Stammblatt schiebt sich als Blatt
über die Liste („‹ Liste" und Esc führen zurück). *Warum:* Der Detailblock liegt heute außerhalb des
Fensters. Die Liste braucht die ganze Breite nur, weil sie alle Spalten zeigt — mit V2 genügen ihr bei
1 088 px Fensterbreite rund 665 px; im Schema-Mockup lässt jeder der achtzehn Reiter dem Bezeichner
mindestens 150 px. *Betroffen:* `Katalograhmen` einmal, damit die Verwaltungen; die iOS-Hülle erbt die
schmale Anordnung. *Aufwand:* M. *Abhängig:* Katalogprobe mit den Fällen 1 088 × 624 und 400 × 624.
*Entschieden:* AD-Q1 (ersetzt „Liste über die ganze Breite, Eingabe darunter").
**Umgesetzt (23.09.2026)** in A1 bis A3 und A5 (Heizkessel, BHKW, Solarkollektoren, Pufferspeicher,
Wärmepumpe, PV-Module, Wechselrichter, Stromspeicher, Bedarfsprofile) mit Stufe 3, in A4 und A6 bis A8
mit Stufe 4 (V14) und in A9 bis A11 mit Stufe 5 (V16). Die Auswahlleiste steht dabei breit über dem
Stammblatt statt über der Liste (Abweichung, 3.6 Punkt 7).

**V4 — Die Zeile ist die Wahl.** *Was:* Klick oder Berührung auf die Zeile wählt, ↑ ↓ Pos1 Ende bewegen
die Wahl (ein Tabulatorhalt für die Liste), die gewählte Zeile trägt Fläche und linken Balken und
`aria-selected`. Die Wahlspalte mit dem runden Knopf entfällt; das Kästchen der Mehrfachwahl (V6) bleibt
eigene Spalte. Die Zeile wird 46 statt 53 px hoch (Berührungsziel 44 px). *Warum:* Der kleine runde
Knopf ist das einzige Klickziel einer 1 000 px breiten Zeile; Strg-Klick zum Markieren ist unsichtbar.
*Betroffen:* `Katalogliste`, `Zeilenwahl`, `Raster` — alle Wirte. *Aufwand:* M. *Abhängig:* QuickGrid
kennt keinen Zeilenklick (Klickfläche in jeder Zelle, kein `display: flex` auf `<td>`), neues Zeilenmaß
als `ItemSize` und `--epos-rasterzeile` samt Rasterprobe, Fokusführung mit bunit.
**Umgesetzt (23.09.2026)** in den acht Verwaltungen; Projektdialoge und Importe behalten die
Wahlspalte und 53 px.

**V5 — Übernahme aus der Zeile, Umschalter „Katalog | Im Projekt (n)".** Entfällt: betrifft nur die
Projektdialoge (AD-Q2, Abschnitt 8).

**V6 — Mehrfachwahl und Vergleich im Stammblatt.** *Was:* Das Kästchen markiert (Kopfkästchen = alle
sichtbaren); die Auswahlleiste über der Liste (V8) nennt dann „n gewählt" und bietet „Vergleichen ·
Löschen… · Auswahl aufheben". „Vergleichen" zeigt zwei bis drei Sätze im Stammblatt statt in einer
Überlagerung, ab vier die breite Überlagerung von heute. Löschen fragt immer zurück; ob es auch mehrere
Zeilen auf einmal löscht, ist AD-Q9. *Warum:* Strg-Klick zum Markieren ist unsichtbar, und nach einem
Herstellerimport mit Hunderten Sätzen ist Aufräumen Zeile für Zeile keine Bedienung. *Betroffen:*
`Katalogliste` (Mehrfachmodus und Vergleich sind da), alle Verwaltungen. *Aufwand:* M. *Abhängig:* V4,
Baustein `Zeilenmarkierung`. *Entschieden:* mit Variante B (6.1); die Sammelübernahme ins Projekt aus
AD-Q4 ist nicht mehr Gegenstand (Abschnitt 8).
**Umgesetzt (23.09.2026)** in A1 bis A3 und A5: Kästchenspalte mit Kopfkästchen, Leertaste und
Strg-Klick, ohne Obergrenze; Vergleichen zeigt zwei bis drei Sätze im Stammblatt (V12), ab vier die
breite Überlagerung.

**V7 — Suche und Filter: Bestand, eine Ergänzung.** *Was:* Suchfeld über alle Spalten, Trefferzahl,
Trichter im Spaltenkopf und „Filter zurücksetzen" bleiben, wie im Konzept Katalogfilter entschieden;
Filterchips über der Liste kommen nicht zurück. Neu ist nur die Regel aus V2: Eine gefilterte Spalte
bleibt sichtbar, damit ihr gefüllter Trichter den Filter anzeigt. „Vergleichen" steht in der
Auswahlleiste (V8). *Warum:* Die Beschwerde betrifft das Rollen, nicht den Filter. *Betroffen:*
`Katalogliste`. *Aufwand:* S. *Abhängig:* V2. **Umgesetzt (23.09.2026).**


## 3 Einheitliches Schema auf der Grundlage von Variante B

Ein Gerüst mit sechs festen Zonen gilt für die elf Komponenten aus 1.4. Was ein Dialog Eigenes braucht,
steckt er als Gruppe ins Stammblatt oder als Schalter in die Werkzeugleiste; die Zonen selbst, ihre
Reihenfolge und ihr Verhalten sind überall gleich. Mockup:
[`Mockups/Administrationsdialoge_Schema.html`](Mockups/Administrationsdialoge_Schema.html), Reiter
„Gerüst"; je Menüpunkt ein Reiter im selben Gerüst.

### 3.1 Die sechs Zonen

| Zone | Inhalt | gleich in allen Dialogen | je Dialog verschieden |
|---|---|---|---|
| **1 Titelzeile** | Titel, Info, ✕ | Titel links, Info (Wiki) daneben, `Schliesskreuz` rechts außen. Das Kreuz steht beim Titel: Trägt eine `Ueberlagerung` den Titel, trägt sie auch das Kreuz, die eingebettete Komponente dann keins | nur der Titeltext |
| **2 Werkzeugleiste** | Suche, Trefferzahl | Suche über alle Spalten (`*` und `?`), Trefferzahl, „Filter zurücksetzen". Sortierpfeil und Trichter bleiben im Spaltenkopf (Konzept Katalogfilter 5.6) | ein Schalter, der einen Trichter setzt (Wärmepumpen: „nur mit Kühlfunktion" legt „>0" in die Spalte Kühlleistung) — nie ein zweiter Filterweg; beim Rechenwerkzeug die Handlungen am Ergebnis (Lastspitzenkappung: CSV-Export, In Variante übernehmen; 7.1 c) |
| **3 Liste** | ein Rollbereich | Kästchenspalte für die Mehrfachwahl; die Zeile ist die Wahl, die Fokuszeile zeigt das Stammblatt (V4); Spalten nach Rang, nie waagerecht rollend (V2); das Schloss hinter dem Bezeichner (3.4) | die Spalten und ihr Rang (Profil im Kern) |
| **4 Auswahlleiste** | Handlungen an Zeilen | steht, sobald eine Zeile gewählt ist, im breiten Fenster über dem Stammblatt, im schmalen über der Liste (3.6 Punkt 7), und nennt zuerst, worauf sie wirkt: die Fokuszeile beim Namen oder „n gewählt". Ein Knopf, der gerade nicht geht, ist weich gesperrt (`aria-disabled`) und nennt den Grund im Kurztext; „Auswahl aufheben" nur bei gesetzten Kästchen | welche Handlungen: Vergleichen, Duplizieren, Löschen… |
| **5 Stammblatt** | Felder der Fokuszeile | ab 900 px Dialogbreite rechts (`clamp(340px, 36 %, 440px)`), darunter als Blatt über Werkzeugleiste und Liste (AD-Q1). Kopf mit Name, Herkunft (Auslieferungssatz mit Schloss oder eigener Satz) und drei Kennzahlen; Gruppen „Kenndaten", „Kosten", „Alle Daten" (Aufklapper); Fuß „n Felder geändert · Verwerfen · Speichern" (AD-Q6). Ab zwei gewählten Zeilen zeigt es die Vergleichstabelle (V12). Auslieferungssätze nur lesbar, mit Hinweis im Fuß (V13). Rollt allein, falls nötig | eine bis vier steckbare Gruppen: Kennlinie, Jahresverlauf, Wochenprofil, Tagesprofil, Ganglinie, Hülle, Herkunft; bei der Lastspitzenkappung Speicher, Schwelle, Ergebnis |
| **6 Fußleiste** | Handlungen am Dialog | links Neu… und Import…, dann die Statuszeile, Füller, zuletzt der eine primäre Knopf „Beenden" (Konzept Knopfleisten, V15); sonst nichts — Handlungen am Ergebnis stehen in der Werkzeugleiste (3.2) | ob es Neu… und Import… gibt und was Import… öffnet |

### 3.2 Drei Orte für Handlungen

- **An Zeilen** — die Auswahlleiste (Zone 4).
- **An Feldern** — der Fuß des Stammblatts (Zone 5): Verwerfen · Speichern. Eine Knopfzeile im Blatt,
  die weder primär ist noch schließt, gilt nach dem Konzept Knopfleisten nicht als zweite Fußleiste.
- **Am Dialog** — die Fußleiste (Zone 6): Neu…, Import…, Schlussknopf.

Ein Rechenwerkzeug (die Lastspitzenkappung, 3.6 Punkt 6) hat dazu **Handlungen am Ergebnis** — die
Ausgabe als CSV und die Übernahme in die Speichervariante. Sie wirken weder auf Zeilen noch auf Felder
noch auf den Dialog und stehen in der **Werkzeugleiste** (Zone 2) nach der Suche, nicht in der
Fußleiste (7.1 c). Im schmalen Fenster (unter 900 px Rahmenbreite) verdeckt das Stammblatt die
Werkzeugleiste; dort stehen dieselben Knöpfe zusätzlich im Kopf des Stammblatts, in einer Zeile mit
„‹ Liste" (`Stammblatt.Kopfhandlungen`, dasselbe Fragment wie im Schlitz `Werkzeug`).

Keine Handlung steht an zwei Orten, die zugleich sichtbar sind — die Kopfhandlungen des schmalen
Stammblatts erscheinen nur, solange es die Werkzeugleiste verdeckt. Aus der heutigen Fußleiste wandern dafür Speichern ins Stammblatt,
Löschen und Duplizieren in die Auswahlleiste; „Bearbeiten…" entfällt (AD-Q6). Knöpfe, die eine Gruppe
vertiefen (Kennliniendaten…, Stundenwerte…, Wochenprofil bearbeiten…, Monatsverlauf…,
Originaldatei…, Gebäudetypen…), stehen im Kopf ihrer Gruppe und öffnen eine Überlagerung im selben
Fenster.

### 3.3 Verhalten

- **Tastatur:** ↑ ↓ Pos1 Ende bewegen die Fokuszeile, das Stammblatt folgt; die Leertaste setzt das
  Kästchen der Fokuszeile; die Liste ist ein Tabulatorhalt. Esc wirkt wie ✕ und schließt stufenweise:
  erst die oberste Überlagerung, im schmalen Fenster dann das Blatt, zuletzt den Dialog. Enter bleibt
  unbelegt, weil Knöpfe der Auswahlleiste und des Stammblatts sofort schreiben.
- **Maus und Berührung:** Klick auf die Zeile wählt; das Kästchen wählt mehrere, das Kopfkästchen alle
  sichtbaren. Ein Doppelklick tut nichts, die Zeilenwahl zeigt schon das Stammblatt (AD-Q3).
- **Worauf eine Handlung wirkt:** Sind Kästchen gesetzt, auf die gewählten Zeilen, sonst auf die
  Fokuszeile. Das erste Wort der Auswahlleiste sagt es („Kessel 7" bzw. „3 gewählt").
- **Beim Öffnen** ist die erste — oder die zuletzt gewählte — Zeile Fokuszeile; das Stammblatt ist nie
  leer, solange die Liste Treffer hat. Ohne Treffer steht am Platz der Auswahlleiste eine leise Zeile,
  damit die Liste nicht springt.
- **Rückmeldung:** Gelungenes meldet die Statuszeile der Fußleiste („Profil Wohnen 2 dupliziert als
  …", „12 Sätze übernommen und gewählt"). Ein Warnband erst nach einem gescheiterten Versuch, mit `Verfaellt`
  (Hausregel „Zustand, Meldung, Leerzustand"). Eine **Rückfrage** gibt es vor dem Löschen — nur, wenn
  gelöscht werden kann — und vor dem Umschalten des Schlosses in beiden Richtungen, dort mit „Nein"
  als Vorgabe (AD-Q15).
- **Ungespeicherte Felder:** Solange das Stammblatt geänderte Felder trägt, hält ein Zeilenwechsel die
  Wahl fest, und sein Fuß sagt „Speichern oder Verwerfen" (Muster: `WaermepumpenDialog` prüft vor dem
  Zeilenwechsel und hält die Wahl). Beenden meldet dasselbe im Warnband und bleibt offen.
- **Schmal (unter 900 px):** Die Liste steht allein und ohne die Spalten mit Rang 2, und
  „Stammblatt ›" in der Auswahlleiste schiebt das Blatt über Werkzeugleiste und Liste, während Auswahl-
  und Fußleiste stehen bleiben — die Komponente fragt dafür ihre eigene Breite, die iOS-Hülle erbt die
  Anordnung (AD-Q14).

### 3.4 Kennzeichen: nur das Schloss

Ein Auslieferungssatz (`ReadOnly`, bei den Gebäudetypen „nicht veränderbar") trägt hinter dem
Bezeichner ein kleines Schloss ohne Wort (AD-Q13). Der Kurztext am Zeichen sagt „Auslieferungssatz –
nur lesen, Duplizieren oder Schloss aufheben erlaubt", in Klimadaten und den drei Zeitreihen, die kein
Duplizieren kennen, „Auslieferungssatz – nur lesen". Dasselbe Zeichen steht im Kopf des Stammblatts vor
der Herkunft; weil ein Kurztext auf Berührungsgeräten nicht erreichbar ist, sagt der Kopf des
Stammblatts dasselbe in Worten („… Zum Ändern in der Auswahlleiste duplizieren oder das Schloss
aufheben."). Die Legende des Mockups und die Hilfeseite erklären das Zeichen einmal; unter 900 px bleibt
es, wie es ist.

**Das Schloss ist umschaltbar (AD-Q15).** Die Auswahlleiste trägt in allen zehn Verwaltungen zwischen
„Duplizieren…" und „Löschen" die Handlung „Schloss aufheben…" bzw. „Schloss setzen…": Enthält die
Auswahl gesperrte Sätze, heißt sie „Schloss aufheben…" und wirkt nur auf diese, sonst „Schloss
setzen…"; der Knopf hält die Breite der längeren Beschriftung, damit die Leiste nicht anders umbricht.
Sie fragt in beiden Richtungen zurück (Vorgabe „Nein"), schaltet danach nur das Kennzeichen des
Kopfsatzes in einer Transaktion (`Auslieferungskennzeichen`, Tabelle aus der `KatalogRegistry`), liest
die Liste neu und meldet das Ergebnis in der Statuszeile. Kein Wert ändert sich, auch nicht beim
Setzen. Ein Satz, dessen Schloss in dieser Sitzung aufgehoben wurde, trägt am Platz des
Auslieferungshinweises das Band „Ausgelieferter Satz, Schloss aufgehoben – Ihre Änderungen gelten als
eigene Werte; ein Update stellt die ausgelieferten Werte nicht wieder her." (die Verwaltung merkt sich
die IDs, solange sie offen ist; kein Schemaschritt). Im Lesemodus der Lizenz und bei `NurLesen` ist die
Handlung hart gesperrt; einen Weg für den Hilfe-Assistenten gibt es nicht (wie beim Löschen).
Besonderheiten: Beim Gebäudetyp schaltet sie `ReadOnly` und `Veraenderbar`; das Schloss eines
Brauchwasser-, Prozess- oder Verbrauchertyps bleibt getrennt (die Rückfrage nennt es); die
Tww-Kataloge, deren Kennzeichen ihrem Freigabestatus folgt, und Tabellen ohne Kennzeichen lehnt der
Kern benannt ab. **Update-Verhalten:** Setup, Erstbereitstellung und Schemamigration überschreiben
Katalogsätze nie und säen die Kataloge der Verwaltungen nicht nach (ADR-001) — ein entsperrter,
geänderter Satz bleibt, wie er ist; erneutes Sperren ändert nur das Kennzeichen; die ausgelieferten Werte
gibt es danach nur noch in einer Datenbanksicherung. Nach dem Aufheben ist Löschen möglich und
endgültig (samt Kindzeilen); die Löschrückfrage bleibt, wie sie ist.

Weitere Kennzeichen führen die Verwaltungen nicht: „im Projekt" (✓) fällt mit den Projektdialogen,
„Hauptposition" mit dem Kostenfaktorenkatalog (Abschnitt 8). Dass ein Projekt einen Satz verwendet, ist
kein Kennzeichen an der Zeile, sondern der Sperrgrund von Löschen… — weich gesperrt, der Kurztext nennt
das Projekt.

### 3.5 Bestand der Verwaltungen

Quellen: die Komponenten unter `EPOS.UI/Dialoge/`, die Spaltenprofile in
`EPOS.Kern/Allgemein/Katalog/Katalogfilterprofil.cs`, die Satzzahlen der Testdatenbank. Die Spalte
**Stammblatt im Schema** nennt die Gruppen; **Abweichung vom Schema** nennt, was der Dialog anders
braucht oder was sich an ihm mehr ändert als am Vorbild Heizkessel. A1 bis A8 tragen heute den
`Katalograhmen`, A9 bis A11 nicht. Die Nummern A1 bis A8 sind die bisherigen; A9 bis A11 sind mit
AD-Q12 hinzugekommen (der frühere A9, der Kostenfaktorenkatalog, steht in Abschnitt 8).

| Nr | Dialog (Menüpunkte) | Listeninhalt | Spalten | Detail heute | Handlungen heute | Besonderheiten | Stammblatt im Schema | Abweichung vom Schema |
|---|---|---|---|---|---|---|---|---|
| A1 | `KatalogBrowserDialog` (Heizkessel, BHKW, Solarkollektoren, Pufferspeicher) | Geräte (63 / 79 / 7 / 13) | 6 / 8 / 6 / 5; Text, Zahl, Ja/Nein — z. B. Heizkessel: Bezeichner, Hersteller, Brennstoff, P_th, η, Brennwert | `Katalogfelder` mit 21 / 25 / 14 / 6 Feldern, bearbeitbar; `Parameteruebersicht` aufklappbar | Speichern · Füller · Neu… · Bearbeiten… · Löschen · OK; Vergleichen per Strg-Klick | Katalogeditor als Überlagerung; nur das BHKW fragt vor dem Überschreiben eines Auslieferungssatzes zurück | Kenndaten, Kosten, Alle Daten | keine — das Vorbild (Mockup-Reiter Heizkessel). „Bearbeiten…" entfällt (AD-Q6), OK heißt Beenden (V15), das Überschreiben beim BHKW klärt AD-Q11; Stufe 5 setzt „Import…" in der Fußleiste um (V14, Baustein `ImportUeberlagerung`) bei Heizkessel, Solarkollektoren und Pufferspeicher — das BHKW hat kein Import…, weil es keinen Herstellerimport gibt; der Pufferspeicher braucht mit sechs Feldern keinen Aufklapper „Alle Daten" |
| A2 | `ModulKatalogDialog` (PV Module, Wechselrichter, Stromspeicher) | Geräte (6 / 1 / 5; nach CEC-Import 20 743 / 2 343 / 6 658) | 7 / 7 / 8 — z. B. Stromspeicher: Bezeichner, Hersteller, Chemie, Energie, Leistung, C-Rate, η_RT, Zyklen | `Formularraster` in ein bis drei Gruppen, 13 / 21 / 13 Felder, direkt bearbeitbar | Speichern · Füller · Neu… · Löschen · Beenden | kein Schutz je Satz: die Tabellen führen `ReadOnly`, Dialog und Profil werten es nicht aus; Neu… belegt Vorgaben aus dem Profil | Kenndaten (die Gruppen des Profils: Moduldaten; Gerät, Eingang, Wirkungsgrad; Speicherdaten, Gerätetechnik), Kosten, Alle Daten | keine; Stufe 5 setzt den Herstellerimport (CEC, PAN/OND, Stromspeicherimport) als „Import…" in der Fußleiste um (V14, Baustein `ImportUeberlagerung`); das Schloss setzt voraus, dass `ReadOnly` ausgewertet wird (V13) |
| A3 | `WaermepumpeStammDialog` (Wärmepumpen) | Geräte (51, dazu 1 960 Kennlinienzeilen) | 9: Hersteller, Modell, Quelle, P_N, VL min, VL max, Zuheizung, Kühlleistung, COP | `WaermepumpeStammFelder` (11 Felder), zwei Reiter COP und Leistung (`DiagrammSvg`), Umschalter Wärme/Kühlung | Speichern · Kennliniendaten… · Füller · Neu · Löschen · Beenden | einzige Verwaltung mit `ReadOnly`-Sperre je Satz und Löschsperre, die das Projekt nennt; „nur mit Kühlfunktion" gibt es nur in der Katalogauswahl des Projektdialogs | Kennlinie, Kenndaten, Kosten, Alle Daten | Kennliniendaten… wird Knopf der Gruppe Kennlinie; der Kühlschalter kommt in die Werkzeugleiste; die Löschsperre wird weich gesperrt mit Grund; Stufe 5 setzt „Import…" in der Fußleiste um (V14, Baustein `ImportUeberlagerung`) |
| A4 | `KlimadatenDialog` (Klimadaten) | Regionen (32; TRY-Regionen, eingelesene Orte) | 7: Bezeichner, Quelle, Standort, Länge, Breite, eingelesen, Schreibschutz | zwei Reiter mit `DiagrammSvg` (Temperatur, Sonnenwinkel), Herkunftstext; darunter der Einleseblock (Quelle, Datei, Ort, Jahr, Fortschritt) | Daten einlesen · Füller · Löschen · Beenden | Rückfrage vor dem Löschen (Kaskade auf die Datenblöcke), Regionsvorschau der TRY-Regionaldaten, Einlesen abbrechbar | Jahresverlauf, Herkunft | keine — Stufe 4 setzt Einlesen als Überlagerung um (V14, Mockup-Reiter Klimadaten); keine Kenndaten, keine Kosten, kein Duplizieren; Schreibschutz-Spalte → Schloss (3.4); Vergleichen (V12) und Löschen mehrerer Sätze neu; kein Umbenennen, Vergleich nur Kennwerte (7.1) |
| A5 | `BedarfAdminDialog` (Brauchwasser, Prozesswärme, Stromverbraucher) | Profile (16 / 32 / 41) | 5: Bezeichner, Typ, Jahressumme, Beschreibung, Auslieferung | `Formularraster` lesend (Jahressumme, Name, Beschreibung, Typ) | Grafik… · Typ ändern… · Füller · Neu… · Ändern… · Löschen · Beenden | vier Überlagerungen (Stammkopf, Wochenprofil-Editor mit 168 Stunden, Ergebnisgrafik, Namensabfrage); Löschsperre für Auslieferungssätze | Wochenprofil, Kenndaten | Felder direkt bedienbar statt „Ändern…"; Grafik… und Typ ändern… werden Knöpfe der Gruppe Wochenprofil; die Gruppe sagt, dass das Wochenprofil zum Typ gehört; Auslieferungsspalte → Schloss (Mockup-Reiter Brauchwasser, Prozesswärme, Stromverbraucher) |
| A6 | `WaermebedarfAdminDialog` (Wärmebedarf Lastgang) | Zeitreihen (4) | 3: Bezeichner, Jahresarbeit, Spitze | kein Diagramm; Einleseblock (Ordner, Datei, Formathinweis, Fortschritt) | Anzeigen · Einlesen · Löschen · Füller · Beenden | `GanglinienImportLauf` (CSV/Text, Excel; 8 760 oder 35 040 Werte); Löschsperre bei Projektzuordnung und Auslieferung | Ganglinie, Herkunft | keine — Stufe 4 setzt Einlesen als Überlagerung um (V14); die Gruppe Ganglinie ist neu (Baustein `GanglinienGrafik` gibt es); kein Neu…, kein Duplizieren, kein Umbenennen; „Anzeigen" bleibt statt „Originaldatei…", keine Spalte speichert den Quellpfad (7.1) |
| A7 | `SolarganglinieAdminDialog` (Solarthermieganglinie) | Zeitreihen (1) | 4: Bezeichner, Beschreibung, Jahresarbeit, Spitze | wie A6, Fortschritt mit Anteil | Anzeigen · Einlesen · Löschen · Füller · OK | eigener Einleseweg (Textdatei mit Kopfzeile, 8 760 Werte), nicht `GanglinienImportLauf` | Ganglinie, Herkunft | wie A6 — Stufe 4 setzt Einlesen als Überlagerung um; OK heißt Beenden; der eigene Einleseweg bleibt (anderes Format) |
| A8 | `StromganglinieAdminDialog` (Stromganglinie) | Zeitreihen (3) | 4: Bezeichner, Zeitintervall, Jahresarbeit, Spitze | kein Diagramm; Zeitintervall und Dateiwahl als Einleseblock | Löschen · Füller · OK; Einlesen über die Dateiwahl | `GanglinienImportLauf` mit drei Überlagerungen; `ReadOnly`-Sperre und Rückfrage | Ganglinie, Herkunft | trägt den `Katalograhmen` bereits seit Stufe 1 (vorgezogen); Stufe 4 setzt Einlesen als Überlagerung und OK/Beenden um; die Dateiwahl setzt nur den Pfad, „Datei einlesen…" startet den Import |
| A9 | `GebaeudeDialog`, Betriebsart Verwaltung (Gebäude → Bearbeiten) | Gebäude (277) | eigene Tabelle: Name, „Typ/Wohnfläche"; vier Vorfilter darüber (Verwendung, Gebäudeart, Baujahr, Suche) | Gruppe „Gebäude: Verbrauch", lesend (Name, Gebäudeart, Beschreibung, Wohnfläche, Art der Angabe) | Gebäude in DB neu… · ändern… · löschen · Gebäudetyp in DB ändern… · Füller · Beenden | Katalogeditor mit zwei Reitern (Kenngrößen; Flächen, U-Werte, Raumtemperaturen, Ferien, Anschlussmaße) und Gebäudetypen-Verwaltung als Überlagerungen; dieselbe Komponente dient Projekt und Assistent | Kenndaten, Hülle, Alle Daten | **größte Abweichung**: Stufe 5 setzt V16 um — eigene Tabelle und Vorfilter → `Katalogliste` mit Profil `FuerGebaeude`, Name, Gebäudeart, Verwendung und Baujahr als Trichter; der Katalogeditor ist in Stammblattgruppen (Kenndaten, Hülle, Alle Daten) zerfallen; „Gebäudetyp in DB ändern…" ist „Gebäudetypen…" im Kopf der Gruppe Kenndaten; keine Kosten, kein Import; Projekt und Assistent behalten ihre Anordnung (3.6); seit #465 jedes Feld des Editors im Stammblatt bedienbar, der Editor nur noch für „Neu…", eigene KI-Maske `Form_Gebaeude_Admin` (7.1 a, e) |
| A10 | `GebaeudetypDialog` (Gebäudetypen) | Typen (12) mit je fünf oder acht Tageskurven zu 24 Stunden | eine Spalte (Name) mit rundem Wahlknopf; daneben die Kurvenliste als zweite Liste | Beschreibung lesend, 24 Stundenfelder und das Tagesbild (`DiagrammSvg`) unter den Listen | Typ speichern · Füller · Typ hinzufügen · Typ löschen · Beenden | Kopf-Detail-Modell; der Kurvenwechsel überträgt die 24 Felder still; ein Typ, der nicht „Veränderbar" ist, sperrt „Typ speichern" und sagt es in einer Herleitungszeile; Löschen fragt zurück | Tagesprofil, Kenndaten | **kein Gerätekatalog**: Stufe 5 setzt V16 um — die Kurvenliste ist die Klappliste „Kurve" der Gruppe Tagesprofil geworden, die 24 Felder öffnen als Überlagerung „Stundenwerte…", ein Kurvenwechsel mit Änderungen hält an; Schloss statt Herleitungszeile; Neu… fragt Name, Beschreibung und Kurvenzahl; keine Kosten, kein Import, kein „Alle Daten"; Reste in 7.1 |
| A11 | `PeakShavingDialog` (Lastspitzenkappung) | Lastgänge: Stromganglinien aus Stamm und Projekt oder eine Datei, die nicht abgelegt wird | keine Liste: Optionsgruppe „Vorhandene Ganglinie \| Datei importieren", Klappliste, Dateiwahl | Parameterblock (Speicher, Zielschwelle mit „Minimale haltbare Schwelle ermitteln", Leistungs- und Bezugspreis, Kompatibilitätsmodus, fünf Wirtschaftlichkeitsfelder), darunter „Berechnen" und das Ergebnis in drei Reitern (Kennzahlen, Lastgang vorher/nachher, Monatsspitzen) | CSV-Export · In Variante übernehmen · Füller · Beenden | Rechenwerkzeug, legt nichts ab, braucht kein offenes Projekt; der Rechenlauf läuft nebenher mit Fortschritt; die Importkette zeigt Optionen und Protokoll als Überlagerungen | Speicher, Schwelle, Kosten, Ergebnis | **Rechenwerkzeug statt Katalog**: Stufe 5 setzt V16 um — die Liste zeigt die Lastgänge mit Quelle, Intervall und Jahresmaximum; die Datei kommt über „Lastgang aus Datei…" an der Stelle von Import…; „Berechnen" bleibt im Blatt zwischen Parametern und Ergebnis, der Stammblattfuß trägt keinen Knopf; Vergleichen rechnet über zwei bis drei Lastgänge; kein Neu…, Duplizieren, Löschen, kein Schloss; CSV-Export und „In Variante übernehmen" stehen in der Werkzeugleiste, im schmalen Fenster zusätzlich im Stammblattkopf, die Fußleiste trägt „Lastgang aus Datei…", Statuszeile und Beenden (7.1 c); Reste in 7.1 |

### 3.6 Wo das Schema nicht passt — und wie es sich hilft

1. **Einlesen ist kein Feld.** Klimadaten und Zeitreihen lesen über mehrere Schritte ein (Quelle,
   Datei, Vorschau, Entscheidungen, Fortschritt). Das gehört weder ins Stammblatt noch in die Liste:
   Import… in der Fußleiste öffnet eine Überlagerung (V14); danach ist die neue Zeile gewählt. Die
   Lastspitzenkappung liest auf demselben Weg ein, legt die Datei aber nicht ab: Sie erscheint als Zeile
   mit der Quelle „Datei".
2. **Klimadaten haben weder Kenndaten noch Kosten.** Ihr Stammblatt besteht aus Kopf, Jahresverlauf
   und Herkunft; es ist bei Auslieferungsregionen ganz, bei eingelesenen bis auf die Bezeichnung nur
   lesbar. Duplizieren entfällt.
3. **Gebäudetypen sind Kopf und Detail.** Eine Zeile ist ein Typ mit fünf oder acht Tageskurven. Die
   Kurvenliste wird die Klappliste „Kurve" der Gruppe Tagesprofil; ein Kurvenwechsel mit geänderten
   Werten hält an wie ein Zeilenwechsel.
4. **Editoren bleiben Überlagerungen.** Wochenprofil (168 Stunden), Stundenwerte einer Tageskurve (24),
   Kennlinienpunkte und Katalogimport passen nicht in 340 bis 440 px; ihre Gruppe trägt den Knopf, der
   sie öffnet.
5. **Die Verwaltung Gebäude** hatte eine eigene Tabelle, vier Vorfilter, und ihre Komponente hat drei
   Betriebsarten. Das Gerüst gilt für die Betriebsart Verwaltung; Projekt und Assistent derselben
   Komponente behalten ihre Anordnung (Abschnitt 8). Seit Stufe 5 trägt die Verwaltung ein Profil
   für die `Katalogliste` (V16); der Katalogeditor mit zwei Reitern ist in die Gruppen Kenndaten,
   Hülle (Flächen und U-Werte) und Alle Daten zerfallen. Mit #465 (24.09.2026) sind alle Felder des
   Editors im Stammblatt bedienbar — Kenndaten samt Wohnfläche und Bauart, Hülle (Hüll-Raster aus
   Bauteil, Kennwert und Größe in drei Spalten, dazu Randbedingung und Wärmeleitwerte), Fenster nach
   Orientierung, Kenngrößen und aufklappbar Alle Daten (Raumtemperaturen, Ferien, Modellparameter,
   Rechenweg, Kühlung); Editor und Stammblatt teilen EINEN Arbeitsstand (`GebaeudeArbeitsstand`:
   Prüfung, Ableitungen, Hüllrechnung) und den Schreibweg `GebaeudeKatalogHuelle.Schreiben`. Der
   Editor erscheint nur noch für „Neu…" (wie beim Heizkessel, AD-Q6); Punkt 4 gilt für ihn nicht mehr.
6. **Die Lastspitzenkappung ist ein Rechenwerkzeug.** Ihre Zeile ist ein Lastgang, ihr Stammblatt trägt
   Parameter und Ergebnis. „Berechnen" steht im Blatt zwischen den Parametern und dem Ergebnis — der
   Lesefluss Parameter → Rechnen → Ergebnis aus dem Entscheid DL-Q2 zu den Knopfleisten bleibt —, und
   der Fuß des Blatts trägt keinen Knopf, weil nichts abgelegt wird. Kein Neu…, kein Duplizieren, kein
   Löschen, kein Schloss; Stromganglinien pflegt die Verwaltung Stromganglinie. Die Handlungen am
   Ergebnis — CSV-Export und „In Variante übernehmen" — stehen in der Werkzeugleiste nach der Suche, im
   schmalen Fenster zusätzlich im Kopf des Stammblatts neben „‹ Liste"; die Fußleiste trägt nur „Lastgang aus Datei…" (an der Stelle von Import…), Statuszeile und Beenden (7.1 c).
7. **Die Auswahlleiste steht im breiten Fenster über dem Stammblatt, nicht über der Liste**
   (Abweichung von 3.1 Zone 4, gültiger Stand seit der Umsetzung 23.09.2026). Grund: Die Liste soll bei
   1 088 × 624 CSS-Pixeln ihre acht Zeilen behalten (Suchzeile 44 px, Spaltenkopf 53 px); eine
   Auswahlleiste zusätzlich über der Liste kostete davon eine Zeile. Im schmalen Fenster (unter 900 px)
   bleibt es bei 3.3: die Auswahlleiste steht über der Liste, das Stammblatt schiebt sich als Blatt
   darüber. Die Überschrift über der Liste entfällt dafür; bei der Wärmepumpe trägt der Titel den
   Kurztext, den sie sonst getragen hätte.


## 4 Vorschläge V8 bis V17 — das Schema als Bausteine

Aufwand wie in Abschnitt 2. Jeder Baustein wird einmal gebaut und von allen Verwaltungen genommen
(Hausregel „ein Dialog baut kein Hausmuster selbst nach").

**V8 — Auswahlleiste als gemeinsamer Baustein.** *Was:* Baustein `Auswahlleiste` für Zone 4: erstes
Wort die Fokuszeile oder „n gewählt", dahinter die Handlungen als Daten (Text, Rückruf, kleinste und
größte Zeilenzahl, Sperrgrund je Zeile), „Auswahl aufheben" nur bei gesetzten Kästchen. Kein Rückruf,
kein Knopf; eine Handlung außerhalb ihrer Zeilenzahl ist weich gesperrt und nennt den Grund.
Löschen fragt zurück und nennt die Zeilen, die bleiben (Schloss, in Verwendung). *Warum:* Eine Stelle
für alle Zeilenhandlungen, gleich benannt in allen Dialogen; die Sammelleiste der Variante B und
die Auswahlleiste des schmalen Fensters (V3) werden eins. *Betroffen:* alle elf Komponenten.
*Aufwand:* M. *Abhängig:* V4, V6, `Zeilenmarkierung`; AD-Q9.
**Umgesetzt (23.09.2026)** in A1 bis A3 und A5; steht dort breit über dem Stammblatt statt über der
Liste (Abweichung, 3.6 Punkt 7). A4, A6 bis A8 und A9 bis A11 stehen noch aus.
**Fortgeschrieben mit AD-Q15 (23.09.2026):** In allen zehn Verwaltungen kommt zwischen Duplizieren…
und Löschen die Handlung „Schloss aufheben…"/„Schloss setzen…" hinzu (Baustein `Schlossumschaltung`);
die `Auswahlhandlung` trägt dafür einen Kurztext (`title`) und eine Breitenvorlage (3.4).

**V9 — Stammblatt mit steckbaren Gruppen.** *Was:* Baustein `Stammblatt` mit Kopf (Name, Herkunft samt
Schloss, drei Kennzahlen), festen Gruppen „Kenndaten" (`Formularraster`), „Kosten", „Alle Daten"
(`Katalogfelder` im Aufklapper; entfällt, wo die festen Gruppen schon alle Felder zeigen) und
Steckplätzen für dialogspezifische Gruppen (`Stammblattgruppe`: Kennlinie, Jahresverlauf,
Wochenprofil, Tagesprofil, Ganglinie, Hülle, Herkunft, Speicher, Schwelle, Ergebnis). Fuß mit
Änderungszahl, Verwerfen, Speichern. Breit neben der Liste, schmal als Blatt mit „‹ Liste". Ab zwei
gewählten Zeilen tritt die Vergleichstabelle (V12) an seine Stelle. *Warum:* Heute baut jeder Dialog
seinen Detailblock selbst — `Katalogfelder`-Block, Einleseblock, lesendes `Formularraster`,
Stundenfelder, Parameterblock. *Betroffen:* alle elf. *Aufwand:* L. *Abhängig:* V3, AD-Q6; Diagramme
als `DiagrammSvg` mit Modell aus dem Kern.
**Umgesetzt (23.09.2026)** in A1 bis A3 und A5, mit den Gruppen Kenndaten, Kosten, Alle Daten sowie
dialogspezifisch Kennlinie (Wärmepumpe) und Wochenprofil (Bedarfsprofile); A4, A6 bis A8 und A9 bis
A11 stehen noch aus.

**V10 — Kennzeichen-Baustein: das Schloss.** *Was:* Baustein `Kennzeichen` mit genau einer Form: das
Schloss ohne Wort für einen Auslieferungssatz, klein hinter dem Bezeichner und im Kopf des
Stammblatts, der volle Satz im Kurztext (3.4), Farbe auch mit `forced-colors`. *Warum:* Ersetzt die
Spalten „Schreibschutz" (Klimadaten) und „Auslieferung" (Bedarfsprofile), die Listen die Breite nehmen;
ein Wort an jeder Zeile war zu groß (AD-Q13). *Betroffen:* die `Katalogliste`-Wirte des
Geltungsbereichs, Profile `FuerBedarf` und Klimadaten; Gebäude und Gebäudetypen mit V16. *Aufwand:* S.
*Abhängig:* V2. **Umgesetzt (23.09.2026).**

**V11 — Gemeinsame Tastaturführung und Rückmeldung.** *Was:* Das Verhalten aus 3.3 einmal im Rahmen:
Fokuszeile und Leertaste in der `Katalogliste`, Esc-Stufen im Gerüst (Überlagerung → Blatt → Dialog),
Statuszeile mit `role="status"`, Warnband nach dem Versuch, das Festhalten der Wahl bei
ungespeicherten Feldern. *Warum:* Heute trägt jede der elf Komponenten einen eigenen Tastenhandler
(Esc); weder sie noch `Katalogliste` und `Zeilenwahl` kennen die Pfeiltasten. *Betroffen:* alle elf.
*Aufwand:* M. *Abhängig:* V4, V9; bunit je Verhalten. **Umgesetzt (23.09.2026)** in den acht
Verwaltungen (Pfeiltasten, Esc, Statuszeile, Halten bei ungespeicherten Feldern); die Leertaste der
Mehrfachwahl steht mit V6 noch aus.

**V12 — Vergleichstabelle.** *Was:* Baustein `Vergleichstabelle` für zwei bis drei Zeilen im
Stammblatt: abweichende Werte fett und mit Wort („≠ abweichend"), gleiche leise, Schalter „nur
abweichende Werte", „‹ Stammblatt von …" zurück; ab vier Zeilen die breite Überlagerung von heute.
Parameter aus demselben Profil wie „Alle Daten". Klimadaten und Zeitreihen vergleichen ihre
Kennzahlen und legen die Verläufe übereinander, die Gebäudetypen dieselbe Tageskurve; die
Lastspitzenkappung rechnet dieselben Parameter über die gewählten Lastgänge. *Warum:* Der Vergleich
existiert als Überlagerung, die das Bild verdeckt, nach dem man wählt. *Betroffen:* alle
`Katalogliste`-Wirte; A4, A6 bis A8, A10 und A11 neu. *Aufwand:* M. *Abhängig:* V6, V9.
**Umgesetzt (23.09.2026)** in A1 bis A3 und A5, für zwei bis drei Sätze im Stammblatt; A4, A6 bis A8,
A10 und A11 stehen noch aus.

**V13 — Auslieferungssätze: lesen, duplizieren, nicht überschreiben.** *Was:* Ein Satz mit `ReadOnly`
trägt das Schloss an der Zeile; sein Stammblatt zeigt die Felder als Text, nicht als gesperrte
Eingaben, und der Fuß sagt „Auslieferungssatz — nur lesen. Zum Ändern in der Auswahlleiste
duplizieren." Duplizieren legt den eigenen Satz an, wählt ihn und nennt im Kopf, woher er kommt.
Löschen ist weich gesperrt. *Warum:* Heute gibt es drei Antworten auf dieselbe Frage: gesperrt
(Wärmepumpe, Gebäudetypen), Überschreiben nach Rückfrage (BHKW), gar kein Schutz (der Modulkatalog
wertet `ReadOnly` nicht aus). *Betroffen:* A1 bis A10 (die Lastspitzenkappung pflegt keinen Katalog).
*Aufwand:* S bis M. *Abhängig:* V9, V10; AD-Q11.
**Umgesetzt (23.09.2026)** in A1 bis A3 und A5: Duplizieren, weich gesperrtes Löschen mit Rückfrage,
Hinweistext im Stammblattkopf. Offen bleibt, die Felder eines Auslieferungssatzes als Text statt als
gesperrte Eingaben zu zeigen (Nach #447).
**Fortgeschrieben mit AD-Q15 (23.09.2026):** „Nicht überschreiben" gilt, solange das Schloss steht —
der Anwender kann es in allen zehn Verwaltungen aufheben und wieder setzen (3.4); Duplizieren bleibt
der Weg zu einer Kopie neben dem Auslieferungssatz.

**V14 — Import als Handlung im Schema.** *Was:* „Import…" bzw. „Daten einlesen…" steht links in der
Fußleiste und öffnet die vorhandene Einlesekette als Überlagerung — `GanglinienImportLauf` für
Zeitreihen, der eigene Einleseweg der Solarthermieganglinie, der Klimaimport, `KatalogImportDialog`
und `ModulImportDialog` für Herstellerdaten, die Importkette der Lastspitzenkappung („Lastgang aus
Datei…", legt nichts ab). Nach dem Einlesen stehen die neuen Zeilen gewählt in der Liste, die
Statuszeile nennt ihre Zahl. Die Menüpunkte unter „Daten & Import" bleiben. *Warum:* Heute steht das
Einlesen in vier Verwaltungen als Block im Detailbereich und nimmt dem Stammblatt den Platz; bei den
Gerätekatalogen ist es nur über das Menü erreichbar. *Betroffen:* A4, A6 bis A8, A11; als Zweitweg A1
bis A3 (ohne BHKW, für das es keinen Import gibt). *Aufwand:* M. *Abhängig:* V9.
**Umgesetzt (23.09.2026)** in A4, A6 bis A8: Der Klimaimport, `GanglinienImportLauf` und der eigene
Einleseweg der Solarthermieganglinie öffnen als Überlagerung mit Kreuz beim Titel hinter „Import…“ bzw.
„Daten einlesen…“; nach dem Einlesen ist die neue Zeile gewählt. A8 (Stromganglinie): Die Dateiwahl
setzt nur den Pfad, „Datei einlesen…“ startet den Import. Offen bleiben A11 (Lastspitzenkappung, Stufe
5) und der Zweitweg bei A1 bis A3 (7.1).

**V15 — Schlanke Fußleiste.** *Was:* Neu… · Import… · Status · Füller · **Beenden**. Speichern wandert
ins Stammblatt (V9), Löschen und Duplizieren in die Auswahlleiste (V8), „Bearbeiten…" entfällt
(AD-Q6); die Schlussknöpfe „OK" des `KatalogBrowserDialog`, des `SolarganglinieAdminDialog` und des
`StromganglinieAdminDialog` heißen „Beenden" (Konzept Knopfleisten, Satz 3: ein Katalogdialog ohne
Arbeitsstand schließt mit Beenden). *Warum:* Eine Leiste, die in allen Verwaltungen gleich aussieht;
die Regeln der `KnopfleistenWacheTests` (ein primärer Knopf, zuletzt) bleiben unverändert.
*Betroffen:* alle elf. *Aufwand:* S je Dialog, mit V8 und V9 zusammen gebaut. *Abhängig:* V8, V9.
**Umgesetzt (23.09.2026)** in `KatalogBrowserDialog`, `ModulKatalogDialog` und den beiden
Ganglinien-Verwaltungen: „OK" heißt „Beenden", Reihenfolge Speichern · Verwerfen · Füller ·
Neu… · Duplizieren… · Löschen · Beenden; Speichern ins Stammblatt und Löschen/Duplizieren in
die Auswahlleiste stehen mit V8 und V9 noch aus.

**V16 — Die Sonderlisten in die Katalogliste.** *Was:* Die Verwaltung Gebäude bekommt ein Profil (Name,
Gebäudeart, Verwendung, Baujahr, Wohnfläche), ihre vier Vorfilter werden Trichter und die Suche der
Werkzeugleiste. Die Gebäudetypen bekommen statt der Typliste mit rundem Wahlknopf ein Profil (Name,
Tageskurven, Beschreibung). Die Lastspitzenkappung bekommt statt Optionsgruppe, Klappliste und
Dateiwahl eine Liste der Lastgänge (Lastgang, Quelle, Intervall, Jahresmaximum). *Warum:* Drei Listen
ohne Suche und Filter sind im Geltungsbereich die letzten Ausnahmen vom Spaltenmodell. *Betroffen:*
A9 bis A11. *Aufwand:* M. *Abhängig:* V2, V10; die Liste der Lastgänge liefert der Kern aus den
Ganglinien von Stamm und Projekt. **Umgesetzt (23.09.2026)** in `GebaeudeAdminDialog` (Profil
`FuerGebaeude`, Trichter Name, Gebäudeart, Verwendung, Baujahr), `GebaeudetypDialog` (Klappliste statt
Typliste mit Wahlknopf) und `PeakShavingDialog` (Liste der Lastgänge mit Quelle, Intervall,
Jahresmaximum).

**V17 — Die zwölf Projektdialoge weg von `Zweispaltenauswahl`.** Entfällt: betrifft nur die
Projektdialoge (Abschnitt 8).

| Nr | Titel | Aufwand | betroffen |
|---|---|---|---|
| V8 | Auswahlleiste als gemeinsamer Baustein | M | alle elf |
| V9 | Stammblatt mit steckbaren Gruppen | L | alle elf |
| V10 | Kennzeichen-Baustein: das Schloss | S | `Katalogliste`-Wirte, Bedarf, Klimadaten, Gebäude, Gebäudetypen |
| V11 | Gemeinsame Tastaturführung und Rückmeldung | M | alle elf |
| V12 | Vergleichstabelle | M | `Katalogliste`-Wirte; A4, A6 bis A8, A10, A11 neu |
| V13 | Auslieferungssätze: lesen, duplizieren, nicht überschreiben | S–M | A1 bis A10 |
| V14 | Import als Handlung im Schema | M | A4, A6 bis A8, A11; Zweitweg A1 bis A3 |
| V15 | Schlanke Fußleiste | S je Dialog | alle elf |
| V16 | Die Sonderlisten in die Katalogliste | M | A9 bis A11 |
| V17 | entfällt (Abschnitt 8) | — | — |


## 5 Was bleibt

Die Regeln der Fußleiste nach dem Konzept Knopfleisten (genau ein primärer Schlussknopf, zuletzt;
✕ und Esc wie Beenden — die Belegung fasst V15 neu), Berührungsziele ab 44 px, immer sichtbare
Zeilenknöpfe, Enter unbelegt, wo ein Knopf sofort schreibt, Filter vor dem Raster, Markierung am
Bezeichner, gesetztes Zeilenmaß — und die Schreibwege: Katalogfelder über ihren eigenen Speicherweg,
Einlesen über die vorhandenen Einleseketten.


## 6 Entscheide

Die Fragen AD-Q1 bis AD-Q8 sind am **22.09.2026** entschieden: nach Empfehlung, mit einer Abweichung
bei AD-Q5. AD-Q12 bis AD-Q14 sind die Rückmeldung des Anwenders zum Schema-Mockup vom selben Tag.
Hier stehen die Entscheide, die für die Verwaltungen gelten; die nur für die Projektdialoge gefallenen
(AD-Q2 bis AD-Q5, AD-Q7) und die dort offene AD-Q10 stehen in Abschnitt 8. Den Grundsatzentscheid für
Variante B nennt 6.1, AD-Q9 (entschieden: Ja), AD-Q11 (abgelöst) und AD-Q15 stehen in 6.2.

| Kennung | Frage | Empfehlung | Entscheid |
|---|---|---|---|
| **AD-Q1** | Stammblatt **neben** der Liste ab 900 px Dialogbreite, darunter als Blatt über der Liste (V3)? Das ersetzt „Liste über die ganze Breite, Eingabe darunter". | Ja — mit den Spaltenrängen aus V2 reicht die Breite, und der Detailblock ist ohne Rollen sichtbar | **22.09.2026: Ja** — Stammblatt neben der Liste ab 900 px Dialogbreite, darunter schmal als Blatt über der Liste (V3) |
| **AD-Q6** | Verwaltung: Braucht es „Bearbeiten…" (Katalogeditor mit Überschreiben und Speichern unter) neben dem bearbeitbaren Stammblatt noch? | Nein — „Überschreiben" leistet schon „Speichern" in der Fußleiste, das das Stammblatt schreibt; „Speichern unter" wird „Kopieren…" neben „Neu…" | **22.09.2026: Entfällt** — die Felder sind direkt bedienbar mit Speichern/Verwerfen. **Umgesetzt (23.09.2026)** — im `KatalogBrowserDialog` ohne „Bearbeiten…" |
| **AD-Q8** | Reihenfolge der Umsetzung (Abschnitt 7)? | wie vorgeschlagen | **22.09.2026: wie vorgeschlagen** — Stufe 1 alle Verwaltungen über den gemeinsamen Rahmen (V1, V2, V7), Stufe 2 Zeilenwahl (V4), Stufe 3 Heizkessel-Projektdialog als Pilot (V3, V5, V6), Stufe 4 übrige Erzeuger, Stufe 5 Bedarf und Zeitreihen. Mit AD-Q12 gelten die Stufen 1 und 2 unverändert; an die Stelle der Stufen 3 bis 5 treten die des Abschnitts 7 |
| **AD-Q12** | Welche Dialoge umfasst das Schema? | Stand bis dahin: neunzehn Komponenten — sieben Verwaltungen im `Katalograhmen`, zwölf Projektdialoge — und zwei Nachbarn (Stromganglinie, Kostenfaktoren) | **22.09.2026:** „Die Dialoge im Mockup sollen nur auf die Auswahlen des Menüs beschränkt sein; die anderen Dialoge eignen sich nicht für das vorgeschlagene Schema." — Gegenstand sind die Verwaltungsdialoge der fünf Menügruppen Gebäude, Klimadaten, Wärmebedarf & Heizung, Strombedarf & Speicher und Energiesysteme (1.4); Projektdialoge, Kosten, Daten & Import und Einstellungen nicht (Abschnitt 8) |
| **AD-Q13** | Wie zeigt die Zeile einen Auslieferungssatz? | Schloss mit dem Wort „Auslieferung" (V10) | **22.09.2026:** „Hinweis ‚Auslieferung' zu groß, evtl. nur das Schloss-Symbol." — nur das Schloss; das Wort steht im Kurztext und in der Legende (3.4) |
| **AD-Q14** | Braucht das Mockup einen eigenen Reiter für das schmale Fenster? | Reiter „Schmal · Wärmepumpe" bei 400 px | **22.09.2026:** „Wozu ist Dialog ‚Schmal · Wärmepumpe'?" — der Reiter entfällt; das Verhalten unter 900 px bleibt ein Satz im Schema (3.3) |

### 6.1 Grundsatzentscheid: Variante B ist die Basis

**22.09.2026 abends:** Der Anwender wählt im Mockup
[`Mockups/Administration_Heizkessel_Neuordnung.html`](Mockups/Administration_Heizkessel_Neuordnung.html)
die **Variante B (Mehrfachauswahl)** als Grundlage: „Weitere Vorschläge auf dieser Basis. Ziel soll
sein: einheitliches und übersichtliches Design auch für die anderen Administrationsdialoge." Daraus
folgen das Schema (Abschnitt 3) und die Vorschläge V8 bis V16 (Abschnitt 4): Kästchen und
Auswahlleiste stehen in jeder Verwaltung, obwohl sie kein Projekt kennt; die Sammelleiste aus B heißt
Auswahlleiste und trägt alle Zeilenhandlungen; der Vergleich im Stammblatt wird ein Baustein.

### 6.2 Offene Fragen

| Kennung | Frage | Variante (a) | Variante (b) | Empfehlung |
|---|---|---|---|---|
| **AD-Q9** | Löscht „Löschen…" in der Auswahlleiste auch mehrere gewählte Zeilen auf einmal? | **Ja:** eine Rückfrage nennt alle gewählten Zeilen und die, die stehen bleiben (Schloss, in Verwendung); gelöscht wird der Rest, die Statuszeile nennt beide Zahlen | **Nein:** Löschen nur bei genau einer Zeile, sonst weich gesperrt mit Grund | **(a)** — nach einem Herstellerimport mit Hunderten Sätzen ist Aufräumen Zeile für Zeile keine Bedienung; die Rückfrage bleibt die einzige des Schemas — **Entscheid 22.09.2026: Ja, Variante (a).** |
| **AD-Q11** | Darf ein Auslieferungssatz (`ReadOnly`) in der Verwaltung überschrieben werden? | **Nein:** nur lesbar; „Duplizieren" legt den eigenen Satz an (so hält es die Wärmepumpe) | **Ja, nach Rückfrage** (so hält es das BHKW) | **(a)** — eine Regel für alle, eine Rückfrage weniger, und die Auslieferung bleibt Bezug. Folge: Beim BHKW (in der Testdatenbank 79 von 79 Sätzen `ReadOnly`) beginnt jede Änderung mit Duplizieren. **Entscheid 23.09.2026: (a).** **Umgesetzt (23.09.2026)** — Kern `Katalogkopie.Duplizieren` in allen acht Stamm-Controllern, die BHKW-Rückfrage „Trotzdem überschreiben?" entfällt. **Abgelöst durch AD-Q15 (23.09.2026):** Der Anwender kann das Schloss selbst aufheben |
| **AD-Q15** | Darf der Anwender das Auslieferungskennzeichen selbst umschalten — einen Auslieferungssatz änderbar machen und einen Satz zum Auslieferungssatz erklären? | **Ja:** ein Kennzeichen, in beide Richtungen, über die Auswahlleiste für die gewählten Sätze, nach Rückfrage, in allen zehn Verwaltungen | **Nein:** weiter nur Duplizieren (AD-Q11) | **(a)** — Anwender 23.09.2026 zur Administration Brauchwasser: „Die Auslieferungssätze sollten auch auf änderbar (vom Nutzer) gesetzt werden können (ohne Schloss)", „Es sollen Datensätze als Auslieferungssätze gesetzt werden können.", „Beim Ändern eines Auslieferungsdatensatzes sollte ein Hinweis erscheinen". **Entscheid 23.09.2026: (a), löst AD-Q11 ab** — Beschriftung nach der Auswahl, Rückfrage in beiden Richtungen (Vorgabe Nein), Band im Stammblatt für in dieser Sitzung entsperrte Sätze, Werte bleiben beim Setzen, Gebäudetyp schaltet `ReadOnly` und `Veraenderbar`, Tww und Tabellen ohne Kennzeichen benannt abgelehnt, Typ-Schloss der Bedarfsprofile getrennt, kein KI-Weg (3.4). **Umgesetzt (23.09.2026, Welle #459)** |


## 7 Reihenfolge und Abnahme

Die Stufen 1 und 2 sind die aus AD-Q8. Die Stufen 3 bis 5 ersetzen mit AD-Q12 die früheren Stufen 3
bis 5 (Projektdialoge, Abschnitt 8); die früheren Stufen 2a und 2b sind in ihnen aufgegangen. Pilot
bleibt die Verwaltung Heizkessel: Sie ist das Vorbild des Mockups, ihre Komponente trägt vier
Menüpunkte, und Schloss, Vergleich und Import lassen sich an ihr zuerst zeigen.

| Stufe | Inhalt | Aufwand | Voraussetzung |
|---|---|---|---|
| 1 | ✔ **umgesetzt 23.09.2026** (Commits `c8e5f775`, `d9ef80b8`) — V1 + V2 + V7 im `Katalograhmen` und in der `Katalogliste`: die acht Komponenten im Rahmen (A1 bis A8) auf einmal, Pilot „Heizkessel"; AD-Q6 im `KatalogBrowserDialog` | M + M + S | AD-Q6 |
| 2 | ✔ **umgesetzt 23.09.2026** (Commits `5767e273`, `e2fbb829`, Merge `f6290028`) — V4 + V11 in `Katalogliste`, `Zeilenwahl`, `Raster` in den acht Verwaltungen; dazu V10 (Schloss) und V15 (Fußleiste); AD-Q11 mit `Katalogkopie.Duplizieren` | M + M + S + S | Stufe 1 |
| 3 | ✔ **umgesetzt 23.09.2026** (Commits `d0660247`, `d8660fde`, Merge `f3957840`) — V8 + V9 + V12 + V13 als Bausteine; Pilot Heizkessel nach dem Mockup-Reiter, dieselbe Komponente für BHKW, Wärmepumpen, Solarkollektoren, Pufferspeicher (A1, A3), PV-Module, Wechselrichter, Stromspeicher (A2) und die Profile Brauchwasser, Prozesswärme, Stromverbraucher (A5); dazu aus Stufe 2 zurückgestellt: V6 (Leertaste und Kästchen der Mehrfachwahl), das weiche Sperren beim Löschen eines Auslieferungssatzes, Duplizieren für die Bedarfsprofile und die Änderungserkennung der Wärmepumpenverwaltung | M + L + M + S | Stufe 2, AD-Q9, AD-Q11 |
| 4 | ✔ **umgesetzt 23.09.2026** (Commits `5a46b0b1`, `b1569752`, Merge `df78272c`) — V14 an den einlesenden Verwaltungen: Klimadaten (A4) und die drei Zeitreihen (A6 bis A8; A8 trägt den `Katalograhmen` schon seit Stufe 1); dazu aus Stufe 3 zurückgestellt und erledigt: Schalter „nur mit Kühlfunktion" in der Werkzeugleiste der Wärmepumpenverwaltung, direkt bedienbare Bedarfsfelder statt „Ändern…" (A5), eigene Kostengruppe bei der Wärmepumpe, die Felder eines Auslieferungssatzes als Text statt als gesperrte Eingaben (V13); offen bleibt „Import…" in der Fußleiste der Gerätekataloge (Zweitweg zu V14 bei A1 bis A3, siehe 7.1) | M | Stufe 3 |
| 5 | ✔ **umgesetzt 23.09.2026** (Commits `9cb41354`, `7e7ecb9c`, `c938ed32`, Merge `b3fa8658`) — V16 an den Sonderlisten: Gebäude in der Betriebsart Verwaltung (A9), Gebäudetypen (A10), Lastspitzenkappung (A11) im Gerüst mit Liste und Stammblatt; dazu 7.1 d erledigt — „Import…" als Zweitweg zu V14 in den Gerätekatalogen (A1 bis A3, nicht beim BHKW) über den neuen Baustein `ImportUeberlagerung`; **Nachtrag #465 (24.09.2026):** 7.1 (a) und (e) erledigt — das Stammblatt der Gebäudeverwaltung führt jedes Feld des Katalogeditors auf demselben Arbeitsstand und Schreibweg, die Verwaltung ist beim Assistenten eine eigene Maske; **Nachtrag #467 (24.09.2026):** 7.1 (c) erledigt — CSV-Export und „In Variante übernehmen" der Lastspitzenkappung stehen in der Werkzeugleiste; **Nachtrag #483 (24.09.2026):** im schmalen Fenster zusätzlich im Stammblattkopf | M + M | Stufe 3; für A11 auch Stufe 4 (Importkette als Überlagerung) |

**Warum diese Folge:** Die Gerätekataloge teilen sich zwei Komponenten (A1, A2) und gewinnen am
meisten durch die Spaltenränge; die Profile folgen, weil ihr Stammblatt nur eine Gruppe hinzubekommt;
die einlesenden Verwaltungen brauchen die Überlagerung aus V14; die Sonderlisten brauchen zuvor ein
Profil im Kern und kommen zuletzt.

**Abnahme je Stufe:** Katalogprobe und Rasterprobe mit den Fällen 1 088 × 624 und 400 × 624 (kein
Rollbereich in einem Rollbereich, keine waagerechte Überbreite der Liste, Fußleiste im Fenster, Zeilenmaß
gleich `ItemSize`); bunit je Komponente (Wahl per Zeile und Tastatur, Rückgabe bei Beenden, Fall ohne
Gaben); `KnopfleistenWacheTests` und `SchliesskreuzWacheTests` grün; die Windows-Schale auf Linux
gebaut, wo eine Hülle angefasst wird. Ab Stufe 3 dazu bunit für die Bausteine — Auswahlleiste
(Fokuszeile gegen „n gewählt", weiche Sperre mit Grund, Löschen mit Rückfrage), Stammblatt (Speichern,
Verwerfen, Zeilenwechsel hält die Wahl, Vergleich ab zwei Zeilen), Kennzeichen (Schloss mit Kurztext,
ohne Wort) — und die Katalogprobe mit gesetzten Kästchen (Auswahlleiste im Fenster, die Liste springt
nicht).

**Papiere im selben Schritt:** die zwei Anordnungsregeln in `EPOS.UI/CLAUDE.md` („LISTE in festem
Rahmen", „KATALOGDIALOG nutzt die Höhe") und neue Regeln für Auswahlleiste, Stammblatt und Kennzeichen;
im Konzept Knopfleisten die Leseregel der Fußleiste (V15); ein Nachtrag im Konzept Katalogfilter 5.6.1;
die Bedienungsseiten unter `Projekte/Wiki/` mit je einem Logbuch-Satz.

### 7.1 Offen nach Stufe 5

(a) Gebäude (A9): ✔ **erledigt mit #465 (24.09.2026)** — Hülle und Wohnfläche sind im Stammblatt
bedienbar, dazu jedes übrige Feld des Katalogeditors (3.6 Punkt 5); Speichern/Verwerfen in der
Fußleiste, Prüfung, Ableitungen und Schreibweg sind die des Editors (`GebaeudeArbeitsstand`,
`GebaeudeKatalogHuelle.Schreiben`), der Lesemodus eines Auslieferungssatzes zeigt Hülle und Alle Daten
als Text; der Editor steht nur noch hinter „Neu…", „Bearbeiten…" entfällt. **Keine Gruppe
Wärmebedarf — begründet:** Der Wärmebedarf eines Gebäudes ist keine Eigenschaft des Katalogsatzes. Der
Kern rechnet ihn je Projektzuordnung (`GebaeudeBedarfCtrl.Rechnen(Projekt, Klimaregion, Zuordnung)`):
mit der Klimaregion und dem Kalender des Projekts, der Angabe der Zuordnung (Wohnfläche oder
Verbrauchsrückrechnung, Jahresnutzungsgrad) und dem Kühlbetrieb des Projekts; ein Kennwert „je
Klimaregion-Vorgabe" bräuchte einen zweiten, projektfreien Rechenweg mit erfundenen Annahmen und ein
Stundenmodell über 8 760 Stunden je Zeilenwahl — eine Zahl, die kein Projekt wiedergäbe. Die
klimafreien Kennzahlen stehen im Blatt (H_ges im Kopf, H_T, H_ve und H_ges unter dem Hüll-Raster); den
Wärmebedarf zeigt der Gebäudedialog des Projekts („Simulation…"). ✔ **Die Löschsperre erkennt eine
Nutzung über die ID — erledigt mit #468 (24.09.2026), Schemaschritt 121:** Die Projektkopie trägt den
Katalogverweis `Tab_Gebaeude.ID_Gebaeude_Stamm` (`INTEGER`, Verweis auf `Tab_Gebaeude_STAMM(ID)`,
`ON DELETE SET NULL`, Index; Quelle `GebaeudeKatalogverweis`). Gefüllt wird er beim Übernehmen
(`GebaeudeStammCtrl.CopyFromStamm` — der einzige Weg Katalog → Projekt, für Assistent und
Projekt-Gebäudedialog), Duplizieren und Varianten kopieren ihn unversetzt, der Projekttransfer nimmt ihn
nicht über die Paketgrenze mit und trägt ihn am Ziel über den Namen nach; der Schritt hat ihn einmalig
über den eindeutigen Namen nachgetragen. `Projektverwendung` führt jede Kopie unter dem Namen ihres
Katalogsatzes (über den Verweis) und nur eine Kopie ohne Verweis unter ihrem eigenen Namen;
`Loeschsperre` nennt die Projekte, und `Loeschen` lehnt einen benutzten Satz auch im Kern ab. Damit hält
die Sperre, wenn der Katalogsatz oder die Kopie umbenannt wird. ✔ **Neuanlage über den Verweis —
erledigt mit #473 (24.09.2026):** Jede Kopie, die beim Speichern der Gebäudeliste eines Projekts neu
entsteht, entsteht über den Verweis: Die Liste führt ihn (`Z_ProjGebModel.ID_Gebaeude_Stamm`,
`GebaeudeProjektZeile.IdKatalog`, gelesen von `Z_ProjGebCtrl.LiesProjekt`, gesetzt beim Übernehmen aus
dem Katalog), und `GebaeudeStammCtrl.CopyFromStamm(Id, Name, …)` sucht den Katalogsatz zuerst über die
Id; der Name ist nur der Rückfall für eine Zeile ohne Verweis oder mit einem Verweis ins Leere.
✔ **Abgleich statt Neuaufbau — erledigt mit #475 (24.09.2026):** Assistent (Betriebsart Bearbeiten) und
Startseite schreiben die Gebäudeliste über dieselbe Kernmethode `WizardCtrl.Schreibe_Projekt_ZuordungGebäude`.
Eine Zeile, deren Zuordnungs-Id (`ID_Z`) eine Zuordnung dieses Projekts mit genau einer Kopie trifft
und deren Katalogverweis unverändert ist (dieselbe Id; ohne Verweis derselbe Name), **behält ihre
Projektkopie** — mit allem, was per Feld-Übernahme (`MerkmalUebernahmeCtrl`) nur dort geändert wurde,
ihrer Tagesverteilung und den Verweisen darauf (Trinkwarmwasserzonen, Gebäudeergebnis); nur ihre
Zuordnungswerte (Fläche oder Verbrauch, Einheit, Jahresnutzungsgrad, dezentrales Warmwasser) werden
fortgeschrieben. Eine Zuordnung, die in der Liste fehlt, wird samt Kopie gelöscht; jede übrige Zeile
(neu übernommen oder mit geändertem Verweis) entsteht neu aus dem Katalog. Eine Umbenennung im Katalog
berührt eine bestehende Kopie damit nicht mehr. Das Änderungsdatum des Projekts — und damit die
Veraltung des letzten Ergebnisses — wird nur gesetzt, wenn der Abgleich tatsächlich geschrieben hat
(eine Zuordnung entfernt oder neu angelegt, Zuordnungswerte einer bleibenden geändert); eine bleibende
Zeile mit unveränderten Werten wird nicht geschrieben (#487). Nach dem Festschreiben trägt jede neu
angelegte Zeile der Liste ihre echte Zuordnungs-Id statt der vorläufigen ab 100000
(`WizardCtrl.EchteIdsUebernehmen`, im Assistenten nach dem Festschreiben seines Vorgangs; die Hülle
`GebaeudeHuelle` zieht sie in die Anzeigezeile nach) — ein zweites Speichern derselben Liste legt
nichts neu an, und eine Feld-Übernahme dazwischen bleibt stehen (#487; Nachweis
`GebaeudelisteAbgleichTests`). ✔ **Die übrigen Gewerke des Assistenten — erledigt mit #490
(24.09.2026):** Der Bearbeiten-Zweig (`AssistentCtrl.Fortschreiben`) gleicht auch Erzeuger,
Prozesswärme, Stromganglinie, externen Wärmebedarf und Stromverbraucher ab: `AssistentAbgleich` nimmt
nach den Ladewegen und nach jedem gelungenen Speichern einen Abdruck je Gewerk — genau das, was der
Schreibweg aus der Liste in die Datenbank trägt (Erzeuger über Reflexion ohne `ID`/`ID_Projekt`, samt
Strangliste des PV-Dialogs; die Zuordnungen über Bezeichner, Summe bzw. Kanal, ohne Ids und
Projektverweise, die der Add-Weg aus dem Bezeichner neu ableitet) —, und nur ein geändertes Gewerk wird
gelöscht und neu angelegt. Ein unveränderter Erzeuger lässt Anlagenzeilen, Pufferzeilen, Senken,
Stränge, Kostenanker, Projektgeräte und Trägersätze stehen; `NeueAnlagenSenkenNachziehen` läuft nur
nach einem Neuschreiben der Anlagen. Der Projektsatz (`Update_Projekt`) wird nur geschrieben, wenn der
Kopf von der Datenbank abweicht (`AssistentAbgleich.KopfGleichGespeichert`). Damit setzt ein Speichern
ohne Eingabe das Änderungsdatum nicht, und das letzte Simulationsergebnis bleibt aktuell; eine Eingabe
in einem Gewerk schreibt genau dieses und setzt das Datum. Ohne Vergleichsstand (Ladekennzeichen
zurückgesetzt) schreibt der Zweig jedes Gewerk. Die Ladewege füllen dafür, was die Seiten beim Aufbau
nachtragen — die Stammfelder der Wärmepumpen-Projektkopie und den Kanal des Wärmebedarfs —, sodass
schon das Betreten einer Seite keine Änderung ist und ein Lauf, der die Seite nie zeigt, weder leere
Stammfelder in die Projektkopie noch jeden Kanal als Heizung zurückschreibt. Nachweis:
`AssistentAbgleichTests`. Die Startseite schreibt in **einem**
Datenbankvorgang (`WizardCtrl.Speichere_Projekt_Gebaeudeliste`): Scheitert ein Schritt — etwa ein
Gebäude ohne Verweis, dessen Name im Katalog fehlt —, rollt alles zurück, das Projekt behält seine
Gebäude, und die Seite zeigt den Grund als Fehlerbanner (`GEB_MSG_LISTE_KATALOGSATZ_FEHLT` bzw.
`GEB_MSG_LISTE_NICHT_GESPEICHERT`); der Assistent läuft ohnehin in seinem Vorgang. Nachweis:
`GebaeudelisteAbgleichTests`, `StartseiteTests.Der_Fehlerhinweis_steht_als_Fehlerbanner_ohne_Verfall`.
Dieselbe Transferregel gilt für den Verweis der
Wärmepumpen-Projektkopie `Tab_WP.ID_Stamm` (Schritt 80): Er reist nicht, der Wärmepumpenkatalog wird am
Ziel nicht unter der Original-Id aufgefüllt, und der Import trägt den Verweis über den eindeutigen
Bezeichner nach (sonst NULL). **`SET NULL` statt `RESTRICT`:** Die
Kopie trägt alle Werte selbst und rechnet ohne den Katalogsatz; die Sperre ist die weiche der
Verwaltung, und ein harter Datenbankfehler träfe jeden anderen Löschweg (Dublettenbereinigung,
„Gebäude in DB löschen" des Projektdialogs, Auslieferungsvorlage). ✔ **Die Löschsperre gilt auf
beiden Wegen (#487):** „Gebäude in DB löschen" im Projekt-Gebäudedialog fragt vor der Rückfrage
`GebaeudeStammCtrl.Loeschsperrgrund` — dieselbe Wahrheit wie die Verwaltung (`Loeschsperre` über
Verweis und Namen, dazu `ReadOnly`) — und nennt einen gesperrten Satz benannt als Warnung
(`ADM_AW_LOESCHEN_VERWENDET` mit den Projekten bzw. `BADM_MSG_SCHREIBGESCHUETZT` für einen
Auslieferungssatz), ohne nachzufragen; gelöscht wird über `GebaeudeStammCtrl.Loeschen`, das die Sperre
im Kern noch einmal hält, eine Ablehnung steht als Absage im Dialog. Mit demselben Schritt tragen vier
Katalogsätze, deren „Sonstige Fläche" keinen U-Wert hatte, die Fläche 0 (`H_T` unverändert). ✔ **Die
dem Anwender vorgelegten Sätze sind erledigt** (Anwenderentscheid „Empfehlung übernehmen", Welle #485,
Schemaschritt 126, `GebaeudeKatalogReparatur`): Der Krankenhaussatz trägt U-Wert Fenster 1,3 statt 0,09
und 250 statt 10 000 m² Nordfenster (gesamte Fensterfläche neu gebildet); die vier Sätze ohne „Fläche
je Nutzer" (`EFH-BZ2`, `KrankenH-F-U-400`, `KMEH-M-U-54`, `Z-EFH-A-S-126`) tragen Wohnfläche /
Bewohner; die acht Testreste (`Z2-EFH-A-S*`, `EFH-BZ2 XXX`) sind gelöscht. Der Schritt trifft je Satz
nur Bezeichner UND Schadensbild — ein schon berichtigter oder anderer Satz einer Kundendatenbank bleibt,
ein benutzter Testrest ebenso (Protokoll). Alle 269 Katalogsätze bestehen jetzt die Prüfung des
Editors (Wächter `GebaeudeKatalogverweisTests`, Nachweis `GebaeudeKatalogReparaturTests`). (b)
Gebäudetypen (A10): Die Klappliste der Kurven kommt aus `TagVCtrl.Typen`; die Löschsperre über ein
Stamm-Gebäude ist neu; ein Kurvenwechsel bei ungespeicherten Änderungen ist gesperrt. (c)
Lastspitzenkappung (A11): Die Parameter stehen in drei Gruppen; die Auswahlleiste steht nur im
schmalen Fenster, kein Vergleich. ✔ **Entschieden und umgesetzt 24.09.2026** (Anwenderentscheid
„Lastspitzenkappung: Werkzeugleiste", Welle #467, Commit `f5366a4a`): CSV-Export und „In Variante
übernehmen" stehen in der Werkzeugleiste — im Schlitz `Werkzeug` der `Katalogliste` nach dem Suchfeld,
Klasse `.epos-werkzeughandlungen` (beieinander, ohne Textumbruch) —, die Fußleiste trägt nur „Lastgang
aus Datei…", Statuszeile und „Beenden" (3.2, 3.6 Punkt 6). Beschriftung und Sperre bleiben: gesperrt,
solange gerechnet wird; ohne Ergebnis meldet jeder Knopf „Bitte zuerst rechnen."; ohne Delegat fehlt er.
Tabulatorfolge Suche → CSV-Export → In Variante übernehmen → Liste. *Gemessen* im Wirt der Rasterprobe:
bei 1 088 × 624 steht die Werkzeugleiste in einer Zeile — das Suchfeld der Lastspitzenkappung beginnt
dafür mit 16rem statt 22rem (Eingabe 124 statt 309 px breit) —, die Liste behält 426 px und acht Zeilen,
die Fußleiste eine Zeile; bei 400 × 624 läuft die Suchzeile in drei statt zwei Zeilen (Suche ·
Handlungen · Trefferzahl), die Fußleiste in einer statt zwei, die Liste hat 283 statt 281 px;
Katalogprobe 0 (60 Fälle). *Schmales Fenster:* ✔ **entschieden und umgesetzt 24.09.2026** (Welle
#483): Das Stammblatt liegt dort als Blatt über Werkzeugleiste und Liste; damit, wer im Blatt rechnet,
die beiden Knöpfe nicht erst über „‹ Liste" erreicht, stehen sie zusätzlich im Kopf des Stammblatts,
rechts in der Zeile von „‹ Liste" (`Stammblatt.Kopfhandlungen`, Klasse `.epos-stammblatt-kopfzeile`
mit `epos-nur-schmal`). Eine Wahrheit: Der Dialog reicht dasselbe Fragment wie in den Schlitz
`Werkzeug` — dieselben Rückrufe, dieselben Sperren („Bitte zuerst rechnen.", gesperrt während der
Rechnung). Ab 900 px Rahmenbreite weicht die Kopfzeile, die Knöpfe stehen nur in der Werkzeugleiste.
*Gemessen* im Wirt der Rasterprobe: bei 400 × 624 nach „Stammblatt ›" steht die Kopfzeile in einer
Zeile (44 px; „‹ Liste" links, CSV-Export und „In Variante übernehmen" rechts), der Stammblattkopf ist
mit 158 px so hoch wie mit „‹ Liste" allein, Blatt, Inhalt und Seite rollen nicht quer (0 px), sichtbar
ist jeder Knopf genau einmal (im Kopf); bei 1 088 × 624 steht keine Kopfzeile, die beiden Knöpfe stehen
genau einmal in der Werkzeugleiste, der Stammblattkopf bleibt 112 px hoch; Katalogprobe 0 (62 Fälle).
Ist ein Spaltenfilter gesetzt, rückt „Filter zurücksetzen" bei 1 088 px in eine
zweite Zeile der Werkzeugleiste (sie wird dann 54 px höher). (d) Import: ✔ **erledigt 24.09.2026**
(Welle #466, Commits `d515b7fe`, `d614c3e7`). *Wahl nach der Übernahme:* Nach „Import…" in A1 bis A3
sind die neuen Sätze die Auswahl (`Zeilenauswahl.Uebernommen`) — ab zweien stehen ihre Kästchen, die
Auswahlleiste sagt „n gewählt", Vergleichen, Schloss und Löschen wirken auf genau sie; der erste neue
Satz ist Fokuszeile und steht im Stammblatt; ein einzelner ist wie nach dem Einlesen in A4 und A6 bis A8
als Fokuszeile allein die Wahl. Kästchen von vorher fallen, ein Vergleich endet; die Statuszeile meldet
„n Sätze übernommen und gewählt". *Breite des Stromspeicherimports:* Kein Fehler im Stilblatt — jede
Import-Überlagerung ist die breite (`min(96vw, 1400px)`), sie kann aber nicht breiter werden als ihr
Fenster. Der Modulkatalog wünschte für alle drei Ausprägungen 860 × 780, der Stromspeicherimport als
eigenes Fenster 1 180 × 700, der Modulimport (PV-Module, Wechselrichter) 1 240 × 800; bei 860 px blieben
826 px, und der Import rollte senkrecht. Nun wünscht ein Fenster, das einen Import als Überlagerung
trägt, mindestens dessen Maß (`Fenstermass.MitUeberlagerung`): Stromspeicher 1 180 × 780, PV-Module und
Wechselrichter 1 240 × 800 (gemessen im Wirt der Rasterprobe: bei 1 180 × 780 eine Überlagerung von
1 133 × 711 px, der Import passt ohne Rollen). Auf dem 1920er Schirm bei 150 % ändert das nichts — dort
nimmt jedes Fenster ohnehin 85 % des Arbeitsbereichs (1 088 CSS-Pixel); es wirkt auf kleinen Schirmen.
(e) Hilfe-Assistent (Entscheid KI‑D‑Q11, #456): Die
Verwaltungen sind steuerbar wie die Projektdialoge — erledigt für A1 (die vier Erzeugerkataloge: Satz
wählen, jedes editierbare Feld des Stammblatts setzen, Speichern über den Weg des Knopfes) und A5 (Typ,
Beschreibung und Monatswerte); A2, A3, A4, A8, A10 und A11 waren es schon. Auslieferungssätze bleiben
geschützt, die Absage nennt „Duplizieren…"; Neu…, Duplizieren…, Löschen und Import… gehen nie über den
Assistenten, A6 und A7 führen keine Einstellwerte. ✔ **Die Gebäude-Verwaltung (A9) mit #465
(24.09.2026):** eigene Maske `Form_Gebaeude_Admin` — ihr Schlüssel ist ihr Navigationsschlüssel
(`Masken.GebaeudeAdmin`, bis dahin `Form_Gebaeude` und damit gleich der Projektmaske); Wahlfeld `satz`
und die Feldliste des Katalogeditors aus derselben Methode (`KiDialoge.GebaeudeKatalogFelder`) an
derselben Sichtklasse, der Name nur lesbar, ohne die Betriebsart des Editors; Prüfen und Speichern auf
dem Weg des Knopfes, ein Auslieferungssatz schreibgeschützt mit „Duplizieren…" oder „Schloss
aufheben…" als Weg. Die Projektmaske `Form_Gebaeude` gehört allein dem Projektdialog; ihr Ziel ist die
Startseite.
(f) Schloss (AD-Q15): Das Band „Schloss aufgehoben" kennt nur die Verwaltung, in
der das Schloss aufgehoben wurde, solange sie offen ist (die Datenbank führt nur das Kennzeichen); im
schmalen Fenster (400 × 624) bricht die Auswahlleiste mit vier Handlungen in drei statt zwei Zeilen um —
die Liste verliert 50 px (Katalogprobe); breit rückt das Stammblatt beim ersten Kästchen um eine Zeile,
wenn der Löschknopf einen langen Text trägt (Stromverbraucher).


## 8 Außerhalb des Geltungsbereichs (Entscheid 22.09.2026)

Mit AD-Q12 sind die folgenden Dialoge nicht mehr Gegenstand dieses Konzepts — „die anderen Dialoge
eignen sich nicht für das vorgeschlagene Schema". Was für sie erarbeitet und entschieden war, steht
hier, damit es nicht verloren geht; als Ziel gilt es nicht. Die volle Bestandstabelle der
Projektdialoge (P1 bis P12) und die ausführlichen Fassungen der Vorschläge stehen in diesem Papier im
Stand vor der Eingrenzung (Commit `58119cc8`).

**Nicht Gegenstand:**

- **Projektdialoge** (Erzeugerkacheln, Assistent), zwölf Komponenten: `HeizkesselDialog`, `BhkwDialog`,
  `WaermepumpenDialog`, `SolarkollektorenDialog`, `PufferspeicherDialog`, `PhotovoltaikDialog`,
  `StromspeicherDialog`, `GebaeudeDialog` in den Betriebsarten Projekt und Assistent,
  `WaermebedarfExternDialog`, `BedarfsProfileDialog`, `SolarganglinieDialog`, `StromganglinieDialog`.
- **Kosten:** Kostenvorlagen, Energieträger, Nutzungsdauer (AfA), gesetzliche Parameter; darin der
  Kostenfaktorenkatalog (`KostenfaktorKatalogDialog`, bisher A9 und Mockup-Reiter „Kostenfaktoren").
- **Daten & Import:** die Katalogimporte und die Katalogdubletten. Die Importe erben mit der
  `Katalogliste`, was V2 und V4 ändern; hinter Import… (V14) erscheinen sie in den Verwaltungen als
  Überlagerung, bleiben aber eigene Dialoge.
- **Einstellungen.**

**Befund zu den Projektdialogen, kurz:** Heute kommt ein Gerät je Durchgang ins Projekt, in sechs
Schritten — Wahlknopf in der Katalogliste treffen, zur Übernahmeleiste über der Liste zurückrollen,
„▲ In das Projekt übernehmen", Trägerwahl als Überlagerung, Vorlauf/Rücklauf und Kosten im Block
„Modul" unter der Katalogliste, OK. Der Projektdialog trägt keine Höhengrenze und rollt als ganze
Seite; ob ein Satz schon im Projekt steht, sagt eine weitere Spalte „im Projekt verwendet".

**Frühere Vorschläge, die nicht mehr Ziel sind:**

- **V5** Übernahme aus der Zeile: Umschalter „Katalog | Im Projekt (n)" statt der gestapelten Listen,
  „＋" je Zeile, „✓" bzw. „✓ 2×" statt der Spalte „im Projekt verwendet", Stammblatt der Projektzeile
  mit dem Inhalt des Blocks „Modul".
- **V6**, Teil Sammelübernahme: „＋ n ins Projekt", Trägerwahl einmal je Brennstoff.
- **V16**, Teil Kostenfaktoren: `Katalogliste` statt `Raster`, die Verwendung (Positionen, Projekte,
  Vorlagen) als Spalte, Schloss „Hauptposition" für die geschützten Hauptpositionen.
- **V17** Baustein `Projektkatalog` für die zwölf Projektdialoge in der Folge Heizkessel (Pilot), BHKW,
  Pufferspeicher, Stromspeicher, Solarkollektoren, Wärmepumpe, Photovoltaik, Bedarfsprofile, die drei
  Zeitreihen, Gebäude; danach entfiele `Zweispaltenauswahl`.
- Schemateile nur für den Projektdialog: Doppelklick wie „＋", Fußleiste „Katalog verwalten… ·
  Abbrechen · OK", Kennzeichen „✓ im Projekt", Gruppe „Im Projekt" im Stammblatt, die Strangtabelle
  der Photovoltaik als breite Überlagerung, der eingebettete `WaermepumpeAnlageDialog` als
  Stammblattgruppen, der Assistentenbetrieb ohne Zone 6.
- Mockups: Reiter „D Im Projekt" in `Mockups/Administration_Heizkessel_Neuordnung.html`; die Reiter
  „Wärmepumpe (Projekt)", „Kostenfaktoren" und „Schmal · Wärmepumpe" des Schema-Mockups sind entfernt.

**Entscheide zu den Projektdialogen (22.09.2026) — sie ruhen:**

| Kennung | Inhalt | Entscheid |
|---|---|---|
| **AD-Q2** | Umschalter „Katalog \| Im Projekt (n)" mit „＋" je Zeile statt `Zweispaltenauswahl` (V5) | Ja, Heizkessel als Pilot |
| **AD-Q3** | Doppelklick auf eine Katalogzeile | Projektdialog: wie „＋" (Übernahme, danach die Trägerwahl); Verwaltung: nichts — dieser Teil gilt weiter (3.3) |
| **AD-Q4** | Mehrfachwahl mit Sammelübernahme, Trägerwahl einmal je Brennstoff (V6) | Ja zu beidem; die Mehrfachwahl gilt in den Verwaltungen mit Variante B weiter (6.1) |
| **AD-Q5** | Katalogfelder im Stammblatt des Projektdialogs | bearbeitbar — abweichend von der Empfehlung schreibt „Ändern" nur die Projektkopie; die Übernahme in den Katalog ist eine eigene Funktion |
| **AD-Q7** | „＋ Ins Projekt" aus der Verwaltung, wenn ein Projekt offen ist | Nein — Katalog und Projekt bleiben getrennt; als Folge gilt weiter: Die Verwaltungen kennen kein Projekt |
| **AD-Q10** | Bleibt die Katalogpflege (Neu, Duplizieren, Löschen) im Projektdialog? | nicht entschieden; die Empfehlung war (a): „Katalog verwalten…" öffnet die Verwaltung als Überlagerung, der Projektdialog pflegt nur Projektzeilen |

Die Stufen 3 bis 5 aus AD-Q8 (Heizkessel-Projektdialog als Pilot, übrige Erzeuger, Bedarf und
Zeitreihen samt Gebäude) ruhen mit diesen Entscheiden.
