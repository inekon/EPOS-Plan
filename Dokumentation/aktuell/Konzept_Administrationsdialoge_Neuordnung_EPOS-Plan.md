# Konzept: Administrationsdialoge neu ordnen — ein Rollbereich je Spalte, Stammblatt, Übernahme aus der Zeile, ein Schema für alle

**Stand:** AD-Q1 bis AD-Q8 sind entschieden, und Variante B ist die Grundlage für alle
Administrationsdialoge (Abschnitt 6). Das einheitliche Schema (Abschnitt 3) und die Vorschläge V8
bis V17 (Abschnitt 4) sind Vorschlag; drei Fragen sind offen (AD-Q9 bis AD-Q11, Abschnitt 6.2).
Umgesetzt ist noch nichts, die Reihenfolge steht in Abschnitt 7.
**Anlass:** Anwender, 22.09.2026, mit zwei Screenshots (Dialog „Administration Heizkessel", Menü
„Administration"): „Die Administrationsdialoge haben ein benutzerunfreundliches Schema und Bedienung
(Beispiel Heizkessel). Insbesondere die verschachtelten Scrollbars sind nicht gut passend. Die Auswahl
an Anlagen/Komponenten in das Projekt ist umständlich. Erstelle ein Beispiel-Mockup und Vorschläge."
Dazu am selben Abend: „Mockup: Administration_Heizkessel_Neuordnung.html — Variante B
(Mehrfachauswahl). Weitere Vorschläge auf dieser Basis. Ziel soll sein: einheitliches und
übersichtliches Design auch für die anderen Administrationsdialoge."
**Mockups:** [`Mockups/Administration_Heizkessel_Neuordnung.html`](Mockups/Administration_Heizkessel_Neuordnung.html)
— Reiter „Heute" (Nachbau mit echten Rollbereichen), A Katalog und Stammblatt, B Mehrfachauswahl,
C schmales Fenster, D Im Projekt. [`Mockups/Administrationsdialoge_Schema.html`](Mockups/Administrationsdialoge_Schema.html)
— Reiter „Gerüst" (die sechs Zonen mit Legende), 1 Heizkessel (Verwaltung, Variante B verfeinert),
2 Wärmepumpe (Projektdialog), 3 Klimadaten, 4 Kostenfaktoren, 5 Brauchwasser, „Schmal"; jeder Dialog
mit einem Kasten „gleich / dialogspezifisch". Jeder Rahmen in der Fenstergröße des Anwenders.
**Geltungsbereich:** Anordnung und Bedienung der Katalog- und Projektdialoge — die neunzehn
Komponenten aus 1.5 und zwei Nachbarn (Stromganglinien-Verwaltung, Kostenfaktoren). Kein Rechenweg,
kein Schema, keine Testdatenbank — kein Referenzlauf.
**Es gilt weiter:** [Konzept Katalogfilter](Konzept_Katalogfilter_EPOS-Plan.md) (Spaltenmodell,
Kapitel 5.6) und [Konzept Knopfleisten](Konzept_Knopfleisten_Administration_EPOS-Plan.md) (ein
primärer Schlussknopf zuletzt, Abbrechen unmittelbar davor; die Belegung fasst V15 neu). Die
Vorschläge 1, 3 und 5 ändern Anordnungsregeln aus [`EPOS.UI/CLAUDE.md`](../../EPOS.UI/CLAUDE.md);
3 und 5 sind dafür mit AD-Q1 und AD-Q2 entschieden (Abschnitt 6).


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

Zwei Nachbarn gehören der Sache nach dazu und stehen in der Bestandstabelle (3.4): die
**Stromganglinien-Verwaltung** (`Strom/StromganglinieAdminDialog`, eigener Menüpunkt, `Katalogliste`
ohne `Katalograhmen`) und der **Kostenfaktorenkatalog** (`Kosten/KostenfaktorKatalogDialog`, eine
Überlagerung der Kostenverwaltung, `Raster` statt `Katalogliste`).


## 2 Zielbild in sieben Vorschlägen

Aufwand: S bis ein Tag, M wenige Tage, L eine Woche und mehr. Abschnitt 4 führt V1 bis V7 mit V8
bis V17 zu einem Schema für alle Dialoge weiter; regeln beide dieselbe Stelle, gilt die spätere
Nummer.

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
immer sichtbaren Knopf „＋" (Aktionsspalte mit Kopf „Im Projekt"), dieselbe Handlung steht in der Auswahlleiste (V8);
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
(Kopfkästchen = alle sichtbaren); die Auswahlleiste über der Liste (V8) nennt dann „n gewählt" und
bietet „＋ n ins Projekt · Vergleichen · Auswahl aufheben". Die Trägerwahl erscheint bei
der Sammelübernahme einmal je Brennstoff. „Vergleichen" zeigt zwei bis drei Geräte im Stammblatt statt
in einer Überlagerung, ab vier Geräten die breite Überlagerung von heute. Löschen fragt immer zurück;
ob es auch mehrere Zeilen auf einmal löscht, ist AD-Q9. *Warum:* Ein Projekt mit mehreren Kesseln braucht heute je Gerät den
ganzen Weg aus 1.4. *Betroffen:* `Katalogliste` (Mehrfachmodus und Vergleich sind da), Projektdialoge.
*Aufwand:* M. *Abhängig:* V4, V5, Baustein `Zeilenmarkierung`. *Entschieden:* AD-Q4.

**V7 — Suche und Filter: Bestand, eine Ergänzung.** *Was:* Suchfeld über alle Spalten, Trefferzahl,
Trichter im Spaltenkopf und „Filter zurücksetzen" bleiben, wie im Konzept Katalogfilter entschieden;
Filterchips über der Liste kommen nicht zurück. Neu ist nur die Regel aus V2: Eine gefilterte Spalte
bleibt sichtbar, damit ihr gefüllter Trichter den Filter anzeigt. „Vergleichen" steht in der
Auswahlleiste (V8). *Warum:* Die Beschwerde betrifft Rollen und Übernahme, nicht den Filter.
*Betroffen:* `Katalogliste`. *Aufwand:* S. *Abhängig:* V2.


## 3 Einheitliches Schema auf der Grundlage von Variante B

Ein Gerüst mit sechs festen Zonen gilt für die neunzehn Komponenten aus 1.5 und die zwei Nachbarn.
Was ein Dialog Eigenes braucht, steckt er als Gruppe ins Stammblatt oder als Schalter in die
Werkzeugleiste; die Zonen selbst, ihre Reihenfolge und ihr Verhalten sind überall gleich. Mockup:
[`Mockups/Administrationsdialoge_Schema.html`](Mockups/Administrationsdialoge_Schema.html), Reiter
„Gerüst"; die Reiter 1 bis 5 zeigen Heizkessel, Wärmepumpe, Klimadaten, Kostenfaktoren und
Brauchwasser im selben Gerüst.

### 3.1 Die sechs Zonen

| Zone | Inhalt | gleich in allen Dialogen | je Dialog verschieden |
|---|---|---|---|
| **1 Titelzeile** | Titel, Info, ✕ | Titel links, Info (Wiki) daneben, `Schliesskreuz` rechts außen. Das Kreuz steht beim Titel: Trägt eine `Ueberlagerung` den Titel, trägt sie auch das Kreuz, die eingebettete Komponente dann keins | nur der Titeltext |
| **2 Werkzeugleiste** | Suche, Trefferzahl, Umschalter | Suche über alle Spalten (`*` und `?`), Trefferzahl, „Filter zurücksetzen"; im Projektdialog vorn der Umschalter „Katalog \| Im Projekt (n)". Sortierpfeil und Trichter bleiben im Spaltenkopf (Konzept Katalogfilter 5.6) | ein Schalter, der einen Trichter setzt (Wärmepumpe: „nur mit Kühlfunktion" legt „>0" in die Spalte Kühlleistung) — nie ein zweiter Filterweg |
| **3 Liste** | ein Rollbereich | Kästchenspalte für die Mehrfachwahl; die Zeile ist die Wahl, die Fokuszeile zeigt das Stammblatt (V4); Spalten nach Rang, nie waagerecht rollend (V2); Kennzeichen an der Zeile (V10); im Projektdialog „＋" je Zeile, Doppelklick wie „＋" (AD-Q3) | die Spalten und ihr Rang (Profil im Kern) |
| **4 Auswahlleiste** | Handlungen an Zeilen | steht über der Liste, sobald eine Zeile gewählt ist, und nennt zuerst, worauf sie wirkt: die Fokuszeile beim Namen oder „n gewählt". Ein Knopf, der gerade nicht geht, ist weich gesperrt (`aria-disabled`) und nennt den Grund im Kurztext; „Auswahl aufheben" nur bei gesetzten Kästchen | welche Handlungen: Ins Projekt, Vergleichen, Duplizieren, Löschen, Entfernen |
| **5 Stammblatt** | Felder der Fokuszeile | ab 900 px Dialogbreite rechts (`clamp(340px, 36 %, 440px)`), darunter als Blatt über Werkzeugleiste und Liste (AD-Q1). Kopf mit Name, Herkunft (Auslieferung, eigener Satz, Projektkopie) und drei Kennzahlen; Gruppen „Kenndaten", „Kosten", „Alle Daten" (Aufklapper); Fuß „n Felder geändert · Verwerfen · Speichern" (AD-Q5, AD-Q6). Ab zwei gewählten Zeilen zeigt es die Vergleichstabelle (V12). Auslieferungssätze nur lesbar, mit Hinweis im Fuß (V13). Rollt allein, falls nötig | eine oder zwei steckbare Gruppen: Im Projekt, Kennlinie, Jahresverlauf, Wochenprofil, Ganglinie, Positionen, Herkunft |
| **6 Fußleiste** | Handlungen am Dialog | links Neu… und Import…, dann die Statuszeile, Füller, zuletzt der eine primäre Knopf: in der Verwaltung „Beenden", im Projektdialog „Abbrechen · OK" (Konzept Knopfleisten, V15) | ob es Neu… und Import… gibt und was Import… öffnet |

### 3.2 Drei Orte für Handlungen

- **An Zeilen** — die Auswahlleiste (Zone 4); dazu „＋" bzw. „Entfernen" in der Zeile selbst, weil
  die Hausregel Zeilenknöpfe immer sichtbar verlangt.
- **An Feldern** — der Fuß des Stammblatts (Zone 5): Verwerfen · Speichern. Eine Knopfzeile im Blatt,
  die weder primär ist noch schließt, gilt nach dem Konzept Knopfleisten nicht als zweite Fußleiste.
- **Am Dialog** — die Fußleiste (Zone 6): Neu…, Import…, „Katalog verwalten…" (AD-Q10), Schlussknopf.

Keine Handlung steht an zwei Orten. Aus der heutigen Fußleiste wandern dafür Speichern ins Stammblatt,
Löschen und Duplizieren in die Auswahlleiste; „Bearbeiten…" entfällt (AD-Q6). Knöpfe, die eine Gruppe
vertiefen (Kennliniendaten…, Wochenprofil bearbeiten…, Monatsverlauf…, Stränge…), stehen im Kopf ihrer
Gruppe und öffnen eine Überlagerung im selben Fenster.

### 3.3 Verhalten

- **Tastatur:** ↑ ↓ Pos1 Ende bewegen die Fokuszeile, das Stammblatt folgt; die Leertaste setzt das
  Kästchen der Fokuszeile; die Liste ist ein Tabulatorhalt. Esc wirkt wie ✕ und schließt stufenweise:
  erst die oberste Überlagerung, im schmalen Fenster dann das Blatt, zuletzt den Dialog. Enter bleibt
  unbelegt, weil Knöpfe der Auswahlleiste und des Stammblatts sofort schreiben.
- **Maus und Berührung:** Klick auf die Zeile wählt; das Kästchen wählt mehrere, das Kopfkästchen alle
  sichtbaren. Doppelklick nur im Projektdialog, dort wie „＋"; in der Verwaltung tut er nichts (AD-Q3).
- **Worauf eine Handlung wirkt:** Sind Kästchen gesetzt, auf die gewählten Zeilen, sonst auf die
  Fokuszeile. Das erste Wort der Auswahlleiste sagt es („Kessel 7" bzw. „3 gewählt").
- **Beim Öffnen** ist die erste — oder die zuletzt gewählte — Zeile Fokuszeile; das Stammblatt ist nie
  leer, solange die Liste Treffer hat. Ohne Treffer steht am Platz der Auswahlleiste eine leise Zeile,
  damit die Liste nicht springt.
- **Rückmeldung:** Gelungenes meldet die Statuszeile der Fußleiste („Profil Wohnen 2 dupliziert als
  …", „3 Geräte übernommen"), im Projektdialog zusätzlich die Zahl am Umschalter. Ein Warnband erst
  nach einem gescheiterten Versuch, mit `Verfaellt` (Hausregel „Zustand, Meldung, Leerzustand"). Eine
  **Rückfrage** gibt es nur vor dem Löschen — und nur, wenn gelöscht werden kann.
- **Ungespeicherte Felder:** Solange das Stammblatt geänderte Felder trägt, hält ein Zeilenwechsel die
  Wahl fest, und sein Fuß sagt „Speichern oder Verwerfen" (Muster: `WaermepumpenDialog` prüft vor dem
  Zeilenwechsel und hält die Wahl). Beenden meldet dasselbe im Warnband und bleibt offen. Im
  Projektdialog gehören die Felder zum Arbeitsstand bis OK; dort trägt das Stammblatt kein Speichern.
- **Schmal (unter 900 px):** Die Liste steht allein; „Stammblatt ›" in der Auswahlleiste schiebt das
  Blatt über Werkzeugleiste und Liste, Auswahl- und Fußleiste bleiben stehen. Die Komponente fragt ihre
  eigene Breite (Containerabfrage); die iOS-Hülle erbt die Anordnung.

### 3.4 Bestand der Dialogfamilie

Quellen: die Komponenten unter `EPOS.UI/Dialoge/`, die Spaltenprofile in
`EPOS.Kern/Allgemein/Katalog/Katalogfilterprofil.cs`, die Satzzahlen der Testdatenbank aus dem Konzept
Katalogfilter 1.2. Die Spalte **Stammblatt im Schema** nennt die Gruppen; **Abweichung vom Schema**
nennt, was der Dialog anders braucht oder was sich an ihm mehr ändert als am Vorbild Heizkessel.

**Verwaltungen** — die sieben Komponenten im `Katalograhmen` (A1 bis A7) und die zwei Nachbarn (A8, A9):

| Nr | Dialog (Menüpunkte) | Listeninhalt | Spalten | Detail heute | Handlungen heute | Besonderheiten | Stammblatt im Schema | Abweichung vom Schema |
|---|---|---|---|---|---|---|---|---|
| A1 | `KatalogBrowserDialog` (Heizkessel, BHKW, Solarkollektoren, Pufferspeicher) | Geräte (63 / 79 / 7 / 13) | 6 / 8 / 6 / 5; Text, Zahl, Ja/Nein — z. B. Heizkessel: Bezeichner, Hersteller, Brennstoff, P_th, η, Brennwert | `Katalogfelder` mit 21 / 25 / 14 / 6 Feldern, bearbeitbar; `Parameteruebersicht` aufklappbar | Speichern · Füller · Neu… · Bearbeiten… · Löschen · OK; Vergleichen per Strg-Klick | Katalogeditor als Überlagerung; nur das BHKW fragt vor dem Überschreiben eines Auslieferungssatzes zurück | Kenndaten, Kosten, Alle Daten | keine — das Vorbild (Mockup-Reiter 1). „Bearbeiten…" entfällt (AD-Q6), OK heißt Beenden (V15), das Überschreiben beim BHKW klärt AD-Q11 |
| A2 | `ModulKatalogDialog` (PV-Module, Wechselrichter, Stromspeicher) | Geräte (6 / 1 / 5; nach CEC-Import 20 743 / 2 343 / 6 658) | 7 / 7 / 8 — z. B. Stromspeicher: Bezeichner, Hersteller, Chemie, Energie, Leistung, C-Rate, η_RT, Zyklen | `Formularraster` in ein bis drei Gruppen, 13 / 21 / 13 Felder, direkt bearbeitbar | Speichern · Füller · Neu… · Löschen · Beenden | kein Schutz je Satz: die Tabellen führen `ReadOnly`, Dialog und Profil werten es nicht aus; Neu… belegt Vorgaben aus dem Profil | Kenndaten (die Gruppen des Profils), Kosten, Alle Daten | keine; der Herstellerimport (CEC, PAN/OND) kommt als „Import…" in die Fußleiste (V14); das Schloss setzt voraus, dass `ReadOnly` ausgewertet wird (V13) |
| A3 | `WaermepumpeStammDialog` (Wärmepumpen) | Geräte (51, dazu 1 960 Kennlinienzeilen) | 9: Hersteller, Modell, Quelle, P_N, VL min, VL max, Zuheizung, Kühlleistung, COP | `WaermepumpeStammFelder` (11 Felder), zwei Reiter COP und Leistung (`DiagrammSvg`), Umschalter Wärme/Kühlung | Speichern · Kennliniendaten… · Füller · Neu · Löschen · Beenden | einzige Verwaltung mit `ReadOnly`-Sperre je Satz und Löschsperre, die das Projekt nennt; „nur mit Kühlfunktion" gibt es nur in der Überlagerung `WaermepumpenKatalogDialog` | Kennlinie, Kenndaten, Kosten, Alle Daten | Kennliniendaten… wird Knopf der Gruppe Kennlinie; der Kühlschalter kommt in die Werkzeugleiste |
| A4 | `KlimadatenDialog` (Klimadaten) | Regionen (TRY-Regionen, eingelesene Orte) | 7: Bezeichner, Quelle, Standort, Länge, Breite, eingelesen, Schreibschutz | zwei Reiter mit `DiagrammSvg` (Temperatur, Sonnenwinkel), Herkunftstext; darunter der Einleseblock (Quelle, Datei, Ort, Jahr, Fortschritt) | Daten einlesen · Füller · Löschen · Beenden | Rückfrage vor dem Löschen (Kaskade auf die Datenblöcke), Regionsvorschau der TRY-Regionaldaten, Einlesen abbrechbar | Jahresverlauf, Herkunft | **Einlesen als Überlagerung** statt Block (V14, Mockup-Reiter 3); keine Kenndaten, keine Kosten, kein Duplizieren; Schreibschutz-Spalte → Schloss (V10); Vergleichen neu (V12) |
| A5 | `BedarfAdminDialog` (Brauchwasser, Prozesswärme, Stromverbraucher) | Profile (16 / 32 / 41) | 5: Bezeichner, Typ, Jahressumme, Beschreibung, Auslieferung | `Formularraster` lesend (Jahressumme, Name, Beschreibung, Typ) | Grafik… · Typ ändern… · Füller · Neu… · Ändern… · Löschen · Beenden | vier Überlagerungen (Stammkopf, Wochenprofil-Editor mit 168 Stunden, Ergebnisgrafik, Namensabfrage); Löschsperre für Auslieferungssätze | Wochenprofil, Kenndaten | Felder direkt bedienbar statt „Ändern…"; Grafik… und Typ ändern… werden Knöpfe der Gruppe Wochenprofil; die Gruppe sagt, dass das Wochenprofil zum Typ gehört (Mockup-Reiter 5) |
| A6 | `WaermebedarfAdminDialog` (Wärmebedarf extern) | Zeitreihen (4) | 3: Bezeichner, Jahresarbeit, Spitze | kein Diagramm; Einleseblock (Ordner, Datei, Formathinweis, Fortschritt) | Anzeigen · Einlesen · Löschen · Füller · Beenden | `GanglinienImportLauf` (CSV/Text, Excel; 8 760 oder 35 040 Werte); Löschsperre bei Projektzuordnung und Auslieferung | Ganglinie, Herkunft | Einlesen als Überlagerung (V14); „Anzeigen" (Originaldatei) wird Knopf der Gruppe Herkunft; die Gruppe Ganglinie ist neu (Baustein `GanglinienGrafik` gibt es) |
| A7 | `SolarganglinieAdminDialog` (Solarthermie-Ganglinie) | Zeitreihen (1) | 4: Bezeichner, Beschreibung, Jahresarbeit, Spitze | wie A6, Fortschritt mit Anteil | Anzeigen · Einlesen · Löschen · Füller · OK | eigener Einleseweg (Textdatei mit Kopfzeile, 8 760 Werte), nicht `GanglinienImportLauf` | Ganglinie, Herkunft | wie A6; OK heißt Beenden; der eigene Einleseweg bleibt (anderes Format) |
| A8 | `StromganglinieAdminDialog` (Stromganglinie; Nachbar) | Zeitreihen (3) | 4: Bezeichner, Zeitintervall, Jahresarbeit, Spitze | kein Diagramm; Zeitintervall und Dateiwahl als Einleseblock | Löschen · Füller · OK; Einlesen über die Dateiwahl | `GanglinienImportLauf` mit drei Überlagerungen; `ReadOnly`-Sperre und Rückfrage | Ganglinie, Herkunft | kein `Katalograhmen` — kommt ins Gerüst wie A6; OK heißt Beenden |
| A9 | `KostenfaktorKatalogDialog` (Überlagerung „Katalog…" der Kostenverwaltung; Nachbar) | Kostenfaktoren (65; die 10 Hauptpositionen blendet der Kern aus) | 1 Spalte und eine eigene Wahlspalte; `Raster` statt `Katalogliste` — keine Suche, kein Filter | keins; eine Neu-Zeile (Textfeld und Knopf) über der Liste | Neu · Löschen · OK | Löschen zählt die Verwendung (Projektpositionen, Projekte, Vorlagenpositionen) und lehnt benannt ab; kein Auslieferungskennzeichen, geschützt sind die Hauptpositionen | Positionen, Kenndaten | **Titel und Kreuz trägt die Überlagerung**; `Katalogliste` statt `Raster` (V16); die Verwendung wird Spalte; das Schloss heißt „Hauptposition"; Neu… legt einen Entwurf im Stammblatt an (Mockup-Reiter 4) |

**Projektdialoge** — alle zwölf stehen heute auf `Zweispaltenauswahl`: Projektliste oben mit
Höhengrenze, Übernahmeleiste ▲/▼, darunter die Katalogliste mit der Spalte „im Projekt verwendet";
Einfachwahl, „Vergleichen" per Strg-Klick; Fuß `SpeichernLeiste` Abbrechen · OK, im Assistenten ohne
Fuß und Kreuz. Im Schema werden alle zwölf zu „Katalog | Im Projekt (n)" (V5, V17); die Spalte
„Projektliste heute" nennt, was die Ansicht „Im Projekt" zeigen muss.

| Nr | Dialog | Projektliste heute | Katalog: Spalten | Detail heute | Handlungen heute | Besonderheiten | Stammblatt im Schema | Abweichung vom Schema |
|---|---|---|---|---|---|---|---|---|
| P1 | `HeizkesselDialog` | Bezeichner | 6 + verwendet | Block „Modul": Kostenknöpfe, Bearbeiten…, Name, Brennstoff mit Trägerwahl, Vorlauf/Rücklauf; „Alle Daten" bearbeitbar mit eigenem Speichern | ▲ mit Trägerwahl (`EnergietraegerVarianteDialog`); Löschen im Katalog | Projektkopie fällt erst, wenn keine zweite Zeile auf sie zeigt | Im Projekt (Träger, Vorlauf, Rücklauf, Kostenknöpfe), Kenndaten, Kosten, Alle Daten | keine — Pilot (V17); das Löschen im Katalog klärt AD-Q10 |
| P2 | `BhkwDialog` | Bezeichner, Summenzeile P_th | 8 + verwendet | wie P1, dazu Grenzleistung % mit Herleitungszeile | ▲ mit Trägerwahl; Neu… und Löschen im Katalog | Katalog durchgehend `ReadOnly`; Rückfrage vor dem Überschreiben | wie P1, dazu Grenzleistung | Summenzeile → Statuszeile der Ansicht „Im Projekt"; Neu und Löschen: AD-Q10; Überschreiben: AD-Q11 |
| P3 | `WaermepumpenDialog` mit eingebettetem `WaermepumpeAnlageDialog` | eigene Tabelle: Hersteller, Typ, Leistung, Vorlauf, Rücklauf, Betriebsart | 9 + verwendet | eingebettete Anlage: Stammfelder samt Kühlleistung, Kennlinien (zwei Diagramme), Kennlinieneditor, Kostenleiste, „In Stamm übernehmen…" | ▲ ohne Trägerwahl; „Markierte auf diese Wärmepumpe umstellen" | Zeilenwechsel prüft erst die Eingaben und hält die Wahl; im Assistenten schreibt das Verlassen die Zeile | Im Projekt (Betriebsart, Vorlauf, Rücklauf, Kühlleistung), Kennlinie, Kosten, Alle Daten | die eingebettete Komponente zerfällt in Stammblattgruppen; „Umstellen" wird „Gerät tauschen…" im Stammblatt der Projektzeile; Kühlschalter neu (Mockup-Reiter 2 und „Schmal") |
| P4 | `SolarkollektorenDialog` | Name | 6 + verwendet | Block „Modul" und Gruppe „Kollektor" (Anzahl, Fläche, Neigung, Azimut, Vorlauf/Rücklauf) mit eigenem „Übernehmen" | ▲ ohne Trägerwahl; neu… und löschen im Katalog | die Gruppe „Kollektor" schreibt erst auf Knopfdruck | Im Projekt (Kollektorfelder), Kenndaten, Kosten, Alle Daten | das eigene „Übernehmen" entfällt — die Felder gehören zum Arbeitsstand bis OK; Neu und Löschen: AD-Q10 |
| P5 | `PufferspeicherDialog` | Bezeichner | 5 + verwendet | Block „Modul" | ▲ mit Dublettenfrage; Löschen im Katalog; Verwaltung als Überlagerung | einzige Dublettenfrage | Im Projekt, Kenndaten, Kosten, Alle Daten | die Dublettenfrage entfällt: „✓" an der Zeile zeigt, dass der Speicher schon im Projekt steht |
| P6 | `PhotovoltaikDialog` | Bezeichner | Module: 7 + verwendet; Wechselrichter nur als Klappliste in der Strangtabelle | Block „Anlage" (Neigung, Azimut, Träger, Modulzahl, Rechenmodell, Stränge mit Ampel, Auslegungstemperaturen) und Block „Modul" | ▲; Modul löschen; Verwaltung als Überlagerung | größte Maske der Familie | Im Projekt (Ausrichtung, Träger, Zusammenfassung der Stränge mit Ampel), Kenndaten, Kosten, Alle Daten | **die Strangtabelle passt nicht in 340 bis 440 px**: „Stränge…" öffnet sie als breite Überlagerung, das Stammblatt zeigt die Zusammenfassung |
| P7 | `StromspeicherDialog` | Bezeichner | 8 + verwendet | Block „Modul" mit Trägerwahl der Projektzeile | ▲; kein Löschen; Verwaltung als Überlagerung | keine Projektkopie; mehrere Speicher führt die Stromspeicher-Auslegung, nicht dieser Dialog | Im Projekt (Träger), Kenndaten, Kosten, Alle Daten | keine |
| P8 | `GebaeudeDialog` (Projekt, Assistent, Verwaltung) | Name | eigene Tabelle (Name, Typ, Wohnfläche) mit vier Vorfiltern (Verwendung, Gebäudeart, Baujahr, Suche) | `Formularraster` lesend (5 Felder) | ▲; Gebäude neu, ändern, löschen; Gebäudetyp ändern…; Simulation… | drei Betriebsarten, vier Überlagerungen | Im Projekt, Kenndaten, Wärmebedarf (Ergebnis der Simulation) | **größte Abweichung**: eigene Tabelle und Vorfilter → `Katalogliste` mit Profil, Verwendung, Gebäudeart und Baujahr als Spalten mit Trichter (V16); Simulation… wird Knopf der Gruppe Wärmebedarf; die Verwaltung ist dasselbe Gerüst ohne Umschalter |
| P9 | `WaermebedarfExternDialog` | Name, dazu Kanal je Zeile (Heizung, Brauchwasser, Prozesswärme) | 3 + verwendet | `GanglinienGrafik` über die volle Breite (Kennzahlen, sortiert, Einheit, Jahresganglinie) | CSV-Datei importieren… · Speichern unter… · Löschen · Bearbeiten… | der Kanal ist die Trägerwahl dieser Maske; zwei Löschsperren | Im Projekt (Kanal), Ganglinie | die Grafik rückt ins Stammblatt (Kennzahlen und Skizze, „groß…" als Überlagerung); Import… in die Fußleiste (V14); der Kanal wird bei der Übernahme gefragt, bei der Sammelübernahme einmal |
| P10 | `BedarfsProfileDialog` (Brauchwasser, Prozesswärme, Stromverbraucher) | Name | 5 + verwendet | Block „Profil" (lesend) und „Jahresverbrauch ändern" (Einheit, Wert, Übernehmen); Monatsverlauf… | in DB neu, ändern, löschen; Typ in DB ändern; Simulation; monatlicher Verlauf | Wochenprofil-Editor und Stammkopf als Überlagerung | Im Projekt (Jahresverbrauch), Wochenprofil, Kenndaten | das eigene „Übernehmen" des Jahresverbrauchs entfällt (Arbeitsstand bis OK); die DB-Knöpfe: AD-Q10 |
| P11 | `SolarganglinieDialog` | Name | 4 + verwendet | Name und Beschreibung lesend; kein Diagramm | nur Bearbeiten… (Verwaltung als Überlagerung) | der Rechenweg liest diese Ganglinie nicht (Hinweis am Infoknopf) | Im Projekt, Ganglinie | kleinste Maske; die Gruppe Ganglinie ist neu |
| P12 | `StromganglinieDialog` | Name | 4 + verwendet | `GanglinienGrafik` wie P9 | wie P9 | keine Dublettenprüfung (gewollt) | Im Projekt, Ganglinie | wie P9, ohne Kanal |

### 3.5 Wo das Schema nicht passt — und wie es sich hilft

1. **Einlesen ist kein Feld.** Klimadaten und Zeitreihen lesen über mehrere Schritte ein (Quelle,
   Datei, Vorschau, Entscheidungen, Fortschritt). Das gehört weder ins Stammblatt noch in die Liste:
   Import… in der Fußleiste öffnet eine Überlagerung (V14); danach ist die neue Zeile gewählt.
2. **Klimadaten haben weder Kenndaten noch Kosten.** Ihr Stammblatt besteht aus Kopf, Jahresverlauf
   und Herkunft; es ist bei Auslieferungsregionen ganz, bei eingelesenen bis auf die Bezeichnung nur
   lesbar. Duplizieren entfällt.
3. **Kostenfaktoren kennen kein Auslieferungskennzeichen.** `Tab_Kostenfaktor` hat keine Spalte
   `ReadOnly`; geschützt sind die Hauptpositionen (`IsMainComponent`). Dasselbe Kennzeichen trägt dort
   das Wort „Hauptposition". Der Katalog ist eine Überlagerung der Kostenverwaltung — Zone 1 stellt
   die Überlagerung.
4. **Die Strangtabelle der Photovoltaik ist breiter als ein Stammblatt.** Sie öffnet als breite
   Überlagerung; das Stammblatt zeigt die Zusammenfassung mit Ampel.
5. **Editoren bleiben Überlagerungen.** Wochenprofil (168 Stunden), Kennlinienpunkte und Katalogimport
   passen nicht in 340 bis 440 px; ihre Gruppe trägt den Knopf, der sie öffnet.
6. **Der Gebäudedialog** hat eine eigene Tabelle, vier Vorfilter und drei Betriebsarten. Er kommt als
   letzter und bekommt zuvor ein Profil für die `Katalogliste` (V16).
7. **Im Assistenten** stellen Assistent und Seitenrahmen Titel, Kreuz und Weiter/Zurück; die Zonen 2
   bis 5 sind dieselben, Zone 6 entfällt wie heute.
8. **Der Wärmepumpen-Projektdialog** bettet heute eine ganze Komponente ein
   (`WaermepumpeAnlageDialog`); sie zerfällt in Stammblattgruppen der Projektzeile.


## 4 Vorschläge V8 bis V17 — das Schema als Bausteine

Aufwand wie in Abschnitt 2. Jeder Baustein wird einmal gebaut und von allen Dialogen genommen
(Hausregel „ein Dialog baut kein Hausmuster selbst nach").

**V8 — Auswahlleiste als gemeinsamer Baustein.** *Was:* Baustein `Auswahlleiste` für Zone 4: erstes
Wort die Fokuszeile oder „n gewählt", dahinter die Handlungen als Daten (Text, Rückruf, kleinste und
größte Zeilenzahl, Sperrgrund je Zeile), „Auswahl aufheben" nur bei gesetzten Kästchen. Kein Rückruf,
kein Knopf; eine Handlung außerhalb ihrer Zeilenzahl ist weich gesperrt und nennt den Grund.
Löschen fragt zurück und nennt die Zeilen, die bleiben (Schloss, in Verwendung). *Warum:* Eine Stelle
für alle Zeilenhandlungen, gleich benannt in allen Dialogen; die Sammelleiste der Variante B und
die Auswahlleiste des schmalen Fensters (V3) werden eins. *Betroffen:* alle 21. *Aufwand:* M.
*Abhängig:* V4, V6, `Zeilenmarkierung`; AD-Q9.

**V9 — Stammblatt mit steckbaren Gruppen.** *Was:* Baustein `Stammblatt` mit Kopf (Name, Herkunft,
drei Kennzahlen), festen Gruppen „Kenndaten" (`Formularraster`), „Kosten", „Alle Daten"
(`Katalogfelder` im Aufklapper) und Steckplätzen für dialogspezifische Gruppen (`Stammblattgruppe`:
Im Projekt, Kennlinie, Jahresverlauf, Wochenprofil, Ganglinie, Positionen, Herkunft). Fuß mit
Änderungszahl, Verwerfen, Speichern; im Projektdialog ohne Speichern (Arbeitsstand bis OK). Breit
neben der Liste, schmal als Blatt mit „‹ Liste". Ab zwei gewählten Zeilen tritt die Vergleichstabelle
(V12) an seine Stelle. *Warum:* Heute baut jeder Dialog seinen Detailblock selbst — Block „Modul",
Einleseblock, eingebettete Anlage, lesendes `Formularraster`. *Betroffen:* alle 21. *Aufwand:* L.
*Abhängig:* V3, AD-Q5, AD-Q6; Diagramme als `DiagrammSvg` mit Modell aus dem Kern.

**V10 — Kennzeichen-Baustein.** *Was:* Baustein `Kennzeichen` mit einer Form für alle Zustände einer
Zeile: Schloss mit „Auslieferung" (`ReadOnly`), Schloss mit „Hauptposition" (Kostenfaktoren), „✓" bzw.
„✓ 2×" (im Projekt), „in n Projekten verwendet". Er steht im Bezeichner bzw. in der Projektspalte und
im Kopf des Stammblatts; Kurztext mit dem vollen Satz, Farben auch mit `forced-colors`. Unter 900 px
fällt das Wort, das Zeichen bleibt. Je Dialog nur die passenden Kennzeichen: die Verwaltung zeigt die
Auslieferung, der Projektdialog „im Projekt". *Warum:* Ersetzt die Spalten „Schreibschutz",
„Auslieferung" und „im Projekt verwendet" — drei Spalten weniger in Listen, denen es an Breite fehlt.
*Betroffen:* alle `Katalogliste`-Wirte, Profile `FuerBedarf` und Klimadaten, A9. *Aufwand:* S.
*Abhängig:* V2.

**V11 — Gemeinsame Tastaturführung und Rückmeldung.** *Was:* Das Verhalten aus 3.3 einmal im Rahmen:
Fokuszeile und Leertaste in der `Katalogliste`, Esc-Stufen im Gerüst (Überlagerung → Blatt → Dialog),
Doppelklick nur mit gesetztem Übernahme-Rückruf, Statuszeile mit `role="status"`, Warnband nach dem
Versuch, das Festhalten der Wahl bei ungespeicherten Feldern. *Warum:* Heute trägt jeder der 21 Dialoge
einen eigenen Tastenhandler (Esc); weder sie noch `Katalogliste` und `Zeilenwahl` kennen die
Pfeiltasten. *Betroffen:* alle 21. *Aufwand:* M. *Abhängig:* V4,
V9; bunit je Verhalten.

**V12 — Vergleichstabelle.** *Was:* Baustein `Vergleichstabelle` für zwei bis drei Zeilen im
Stammblatt: abweichende Werte fett und mit Wort („≠ abweichend"), gleiche leise, Schalter „nur
abweichende Werte", „‹ Stammblatt von …" zurück; ab vier Zeilen die breite Überlagerung von heute.
Parameter aus demselben Profil wie „Alle Daten". Klimadaten und Zeitreihen vergleichen ihre
Kennzahlen und legen die Verläufe übereinander. *Warum:* Der Vergleich existiert als Überlagerung,
die das Bild verdeckt, nach dem man wählt. *Betroffen:* alle `Katalogliste`-Wirte; A4, A6 bis A8 neu.
*Aufwand:* M. *Abhängig:* V6, V9.

**V13 — Auslieferungssätze: lesen, duplizieren, nicht überschreiben.** *Was:* Ein Satz mit `ReadOnly`
trägt das Schloss an der Zeile; sein Stammblatt zeigt die Felder als Text, nicht als gesperrte
Eingaben, und der Fuß sagt „Auslieferung — nur lesbar. Zum Ändern duplizieren." Duplizieren legt den
eigenen Satz an, wählt ihn und nennt im Kopf, woher er kommt. Löschen ist weich gesperrt. *Warum:*
Heute gibt es drei Antworten auf dieselbe Frage: gesperrt (Wärmepumpe), Überschreiben nach Rückfrage
(BHKW), gar kein Schutz (der Modulkatalog wertet `ReadOnly` nicht aus). *Betroffen:* A1 bis A8; die
Projektdialoge zeigen es im Katalog nicht (V10). *Aufwand:* S bis M. *Abhängig:* V9, V10; AD-Q11.

**V14 — Import als Handlung im Schema.** *Was:* „Import…" bzw. „Daten einlesen…" steht links in der
Fußleiste und öffnet die vorhandene Einlesekette als Überlagerung — `GanglinienImportLauf` für
Zeitreihen, der Klimaimport, `KatalogImportDialog` und `ModulImportDialog` für Herstellerdaten. Nach
dem Einlesen stehen die neuen Zeilen gewählt in der Liste, die Statuszeile nennt ihre Zahl. Die
Menüpunkte unter „Daten & Import" bleiben. *Warum:* Heute steht das Einlesen in drei Dialogen als Block
im Detailbereich und nimmt dem Stammblatt den Platz; bei den Gerätekatalogen ist es nur über das Menü
erreichbar. *Betroffen:* A4, A6 bis A8, P9, P12; als Zweitweg A1 bis A3. *Aufwand:* M. *Abhängig:*
V9.

**V15 — Schlanke Fußleiste.** *Was:* Verwaltung: Neu… · Import… · Status · Füller · **Beenden**;
Projektdialog: „Katalog verwalten…" (AD-Q10) · Status · Füller · Abbrechen · **OK**. Speichern
wandert ins Stammblatt (V9), Löschen und Duplizieren in die Auswahlleiste (V8), „Bearbeiten…" entfällt
(AD-Q6); die Schlussknöpfe „OK" des `KatalogBrowserDialog`, des `SolarganglinieAdminDialog`, des
`StromganglinieAdminDialog` und des Kostenfaktorenkatalogs heißen „Beenden" (Konzept Knopfleisten,
Satz 3: ein Katalogdialog ohne Arbeitsstand schließt mit Beenden). *Warum:* Eine Leiste, die in allen
Verwaltungen gleich aussieht; die Regeln der `KnopfleistenWacheTests` (ein primärer Knopf, zuletzt;
Abbrechen unmittelbar davor) bleiben unverändert. *Betroffen:* alle 21. *Aufwand:* S je Dialog, mit
V8 und V9 zusammen gebaut. *Abhängig:* V8, V9.

**V16 — Die Sonderlisten in die Katalogliste.** *Was:* Der Kostenfaktorenkatalog bekommt die
`Katalogliste` mit Suche, Sortierung und Trichtern statt des `Raster` mit eigener Wahlspalte; die
Verwendung (Positionen, Projekte, Vorlagen) wird Spalte, die Hauptpositionen erscheinen mit Schloss.
Der Gebäudedialog bekommt ein Profil (Name, Gebäudeart, Verwendung, Baujahr, Wohnfläche), seine vier
Vorfilter werden Trichter. *Warum:* Zwei Listen ohne Suche und Filter sind die letzten Ausnahmen vom
Spaltenmodell. *Betroffen:* A9, P8. *Aufwand:* M. *Abhängig:* V2, V10; die Verwendungszählung liefert
der Kern (`KostenfaktorCtrl` zählt heute erst beim Löschen).

**V17 — Die zwölf Projektdialoge weg von `Zweispaltenauswahl`.** *Was:* Baustein `Projektkatalog` —
Umschalter „Katalog | Im Projekt (n)", in beiden Ansichten das Gerüst aus Abschnitt 3, „＋" bzw.
„Entfernen" je Zeile, Auswahlleiste mit „＋ n ins Projekt" bzw. „Entfernen", Stammblatt der
Projektzeile mit der Gruppe „Im Projekt". Die Übernahme fragt Träger bzw. Kanal einmal je Brennstoff
(AD-Q4). Reihenfolge: Heizkessel als Pilot, dann BHKW, Pufferspeicher, Stromspeicher,
Solarkollektoren, Wärmepumpe, Photovoltaik; danach Bedarfsprofile und die drei Zeitreihen; das
Gebäude zuletzt. Mit dem letzten Wirt entfällt `Zweispaltenauswahl`. *Warum:* V5 als Baustein statt
zwölfmal. *Betroffen:* P1 bis P12, dazu die Assistentenseiten derselben Komponenten. *Aufwand:* L.
*Abhängig:* V5, V8, V9; AD-Q10; eine Wache, dass kein Dialog der Familie Liste, Auswahlleiste oder
Stammblatt selbst baut.

| Nr | Titel | Aufwand | betroffen |
|---|---|---|---|
| V8 | Auswahlleiste als gemeinsamer Baustein | M | alle 21 |
| V9 | Stammblatt mit steckbaren Gruppen | L | alle 21 |
| V10 | Kennzeichen-Baustein | S | `Katalogliste`-Wirte, Bedarf, Klimadaten, Kostenfaktoren |
| V11 | Gemeinsame Tastaturführung und Rückmeldung | M | alle 21 |
| V12 | Vergleichstabelle | M | `Katalogliste`-Wirte; Klimadaten und Zeitreihen neu |
| V13 | Auslieferungssätze: lesen, duplizieren, nicht überschreiben | S–M | A1 bis A8 |
| V14 | Import als Handlung im Schema | M | A4, A6 bis A8, P9, P12; Zweitweg A1 bis A3 |
| V15 | Schlanke Fußleiste | S je Dialog | alle 21 |
| V16 | Die Sonderlisten in die Katalogliste | M | A9, P8 |
| V17 | Die zwölf Projektdialoge weg von `Zweispaltenauswahl` | L | P1 bis P12 |


## 5 Was bleibt

Die Regeln der Fußleiste nach dem Konzept Knopfleisten (genau ein primärer Schlussknopf, zuletzt;
Abbrechen unmittelbar davor; ✕ und Esc wie Abbrechen bzw. Beenden — die Belegung fasst V15 neu),
Berührungsziele ab 44 px, immer sichtbare Zeilenknöpfe, Enter unbelegt, wo ein Knopf sofort schreibt,
Filter vor dem Raster, Markierung am Bezeichner, gesetztes Zeilenmaß — und die Schreibwege: Aufnahme und
Trägervariante beim Übernehmen, Projektliste beim OK, Katalogfelder über ihren eigenen Speicherweg.


## 6 Entscheide

Die Fragen AD-Q1 bis AD-Q8 sind am **22.09.2026** entschieden: nach Empfehlung, mit einer Abweichung
bei AD-Q5 — Katalogfelder ändern dort nur die Projektkopie, nicht den Katalogsatz. Den Grundsatzentscheid
für Variante B nennt 6.1, die offenen Fragen AD-Q9 bis AD-Q11 stehen in 6.2.

| Kennung | Frage | Empfehlung | Entscheid |
|---|---|---|---|
| **AD-Q1** | Stammblatt **neben** der Liste ab 900 px Dialogbreite, darunter als Blatt über der Liste (V3)? Das ersetzt „Liste über die ganze Breite, Eingabe darunter". | Ja — mit den Spaltenrängen aus V2 reicht die Breite, und der Detailblock ist ohne Rollen sichtbar | **22.09.2026: Ja** — Stammblatt neben der Liste ab 900 px Dialogbreite, darunter schmal als Blatt über der Liste (V3) |
| **AD-Q2** | Projektdialoge: Umschalter „Katalog \| Im Projekt (n)" mit „＋" je Zeile statt `Zweispaltenauswahl` (V5)? | Ja, zuerst im Heizkesseldialog als Pilot | **22.09.2026: Ja** (V5) |
| **AD-Q3** | Doppelklick auf eine Katalogzeile: Übernehmen (Projektdialog), Bearbeiten (Verwaltung) oder nichts? | Projektdialog: Übernehmen; Verwaltung: nichts, das Stammblatt zeigt schon alles. Enter bleibt unbelegt | **22.09.2026:** Im Projektdialog wirkt der Doppelklick wie „＋" (Übernahme, danach die Trägerwahl); in der Verwaltung tut er nichts, dort zeigt die Zeilenwahl (V4) schon das Stammblatt |
| **AD-Q4** | Mehrfachwahl mit Sammelübernahme (V6) — und die Trägerwahl einmal je Brennstoff statt je Gerät? | Ja zu beidem | **22.09.2026: Ja** zu beidem (V6) |
| **AD-Q5** | Stammblatt im Projektdialog: Katalogfelder unter „Alle Daten" weiter bearbeitbar oder nur zeigen? | Bearbeitbar lassen (so gilt es heute), mit der Zeile „Ändern schreibt den Katalogsatz für alle Projekte" | **22.09.2026: Ja**, die Katalogfelder der Projektkopie bleiben bearbeitbar — abweichend von der Empfehlung schreibt „Ändern" aber nicht den Katalogsatz: Stammfelder ändern nur das Projekt (Regel seit der Projektkopie der Wärmepumpe), Übernahme in den Katalog ist eine eigene Funktion |
| **AD-Q6** | Verwaltung: Braucht es „Bearbeiten…" (Katalogeditor mit Überschreiben und Speichern unter) neben dem bearbeitbaren Stammblatt noch? | Nein — „Überschreiben" leistet schon „Speichern" in der Fußleiste, das das Stammblatt schreibt; „Speichern unter" wird „Kopieren…" neben „Neu…" | **22.09.2026: Entfällt** — die Felder sind direkt bedienbar mit Speichern/Verwerfen |
| **AD-Q7** | Darf die Verwaltung aus dem Menü „＋ Ins Projekt" anbieten, wenn ein Projekt offen ist? | Nein — ein Weg je Ziel; die Verwaltung bleibt Katalogpflege | **22.09.2026: Nein** — Katalog und Projekt bleiben getrennt, Übernahme nur im Projektdialog |
| **AD-Q8** | Reihenfolge der Umsetzung (Abschnitt 7)? | wie vorgeschlagen | **22.09.2026: wie vorgeschlagen** — Stufe 1 alle Verwaltungen über den gemeinsamen Rahmen (V1, V2, V7), Stufe 2 Zeilenwahl (V4), Stufe 3 Heizkessel-Projektdialog als Pilot (V3, V5, V6), Stufe 4 übrige Erzeuger, Stufe 5 Bedarf und Zeitreihen (Abschnitt 7) |

### 6.1 Grundsatzentscheid: Variante B ist die Basis

**22.09.2026 abends:** Der Anwender wählt im Mockup
[`Mockups/Administration_Heizkessel_Neuordnung.html`](Mockups/Administration_Heizkessel_Neuordnung.html)
die **Variante B (Mehrfachauswahl)** als Grundlage: „Weitere Vorschläge auf dieser Basis. Ziel soll
sein: einheitliches und übersichtliches Design auch für die anderen Administrationsdialoge." Daraus
folgen das Schema (Abschnitt 3) und die Vorschläge V8 bis V17 (Abschnitt 4): Kästchen und
Auswahlleiste stehen in jedem Dialog der Familie, auch in den Verwaltungen, die kein Projekt kennen;
die Sammelleiste aus B heißt Auswahlleiste und trägt alle Zeilenhandlungen; der Vergleich im
Stammblatt wird ein Baustein.

### 6.2 Offene Fragen

| Kennung | Frage | Variante (a) | Variante (b) | Empfehlung |
|---|---|---|---|---|
| **AD-Q9** | Löscht „Löschen…" in der Auswahlleiste auch mehrere gewählte Zeilen auf einmal? | **Ja:** eine Rückfrage nennt alle gewählten Zeilen und die, die stehen bleiben (Schloss, in Verwendung); gelöscht wird der Rest, die Statuszeile nennt beide Zahlen | **Nein:** Löschen nur bei genau einer Zeile, sonst weich gesperrt mit Grund | **(a)** — nach einem Herstellerimport mit Hunderten Sätzen ist Aufräumen Zeile für Zeile keine Bedienung; die Rückfrage bleibt die einzige des Schemas |
| **AD-Q10** | Bleibt die Katalogpflege (Neu, Duplizieren, Löschen, Bearbeiten) im Projektdialog? | **Nein:** Der Projektdialog übernimmt und pflegt die Projektzeilen; „Katalog verwalten…" links in der Fußleiste öffnet die Verwaltung als Überlagerung (Pufferspeicher, Photovoltaik und Stromspeicher tun das schon), danach ist der Katalog neu geladen | **Ja, einheitlich:** jeder Projektdialog bekommt Duplizieren und Löschen in der Auswahlleiste der Ansicht „Katalog" | **(a)** — ein Weg je Ziel, das Gegenstück zu AD-Q7; heute hält es jeder Erzeuger anders (Heizkessel: Löschen; BHKW: Neu und Löschen; Solarkollektoren: neu und löschen; Stromspeicher: nichts) |
| **AD-Q11** | Darf ein Auslieferungssatz (`ReadOnly`) in der Verwaltung überschrieben werden? | **Nein:** nur lesbar; „Duplizieren" legt den eigenen Satz an (so hält es die Wärmepumpe) | **Ja, nach Rückfrage** (so hält es das BHKW) | **(a)** — eine Regel für alle, eine Rückfrage weniger, und die Auslieferung bleibt Bezug. Folge: Beim BHKW (in der Testdatenbank 79 von 79 Sätzen `ReadOnly`) beginnt jede Änderung mit Duplizieren |


## 7 Reihenfolge und Abnahme

Die Reihenfolge aus AD-Q8 bleibt. Eingeschoben sind die Stufen 2a und 2b: Dort entstehen die Bausteine
des Schemas an den Verwaltungen, bevor der erste Projektdialog sie braucht.

| Stufe | Inhalt | Aufwand | Voraussetzung |
|---|---|---|---|
| 1 | V1 + V2 + V7 im `Katalograhmen` und in der `Katalogliste`, dazu V10 (Kennzeichen) und V15 (Fußleiste): alle sieben Verwaltungskomponenten auf einmal, Pilot „Administration Heizkessel"; AD-Q6 im `KatalogBrowserDialog` | M + M + S + S + S | AD-Q6 |
| 2 | V4 + V11 in `Katalogliste`, `Zeilenwahl`, `Raster` — alle Wirte | M + M | Stufe 1 |
| 2a | V8 + V9 + V12 + V13 als Bausteine; Pilot „Administration Heizkessel" nach Mockup-Reiter 1, danach A1 bis A3 und A5 | M + L + M + S | Stufe 2, AD-Q9, AD-Q11 |
| 2b | V14 + V16 an den übrigen Verwaltungen: Klimadaten (A4), die drei Zeitreihen (A6 bis A8), Kostenfaktoren (A9) | M + M | Stufe 2a |
| 3 | V3 + V5 + V6 + V17 im `HeizkesselDialog` (Pilot der Projektdialoge, Baustein `Projektkatalog`) | M + L + M | Stufe 2a, AD-Q1 bis AD-Q5, AD-Q10 |
| 4 | die übrigen sechs Erzeuger-Projektdialoge (P2 bis P7) | L | Stufe 3 |
| 5 | Bedarf und Zeitreihen (P9 bis P12), zuletzt das Gebäude (P8, mit V16); `Zweispaltenauswahl` entfällt | L | Stufe 4, Stufe 2b |

**Abnahme je Stufe:** Katalogprobe und Rasterprobe mit den Fällen 1 088 × 624 und 400 × 624 (kein
Rollbereich in einem Rollbereich, keine waagerechte Überbreite der Liste, Fußleiste im Fenster, Zeilenmaß
gleich `ItemSize`); bunit je Komponente (Wahl per Zeile und Tastatur, Übernahme, Ergebnis und `null`
bei Abbruch, Fall ohne Gaben); `KnopfleistenWacheTests` und `SchliesskreuzWacheTests` grün; die
Windows-Schale auf Linux gebaut, wo eine Hülle angefasst wird. Ab Stufe 2a dazu bunit für die
Bausteine — Auswahlleiste (Fokuszeile gegen „n gewählt", weiche Sperre mit Grund, Löschen mit
Rückfrage), Stammblatt (Speichern, Verwerfen, Zeilenwechsel hält die Wahl, Vergleich ab zwei Zeilen),
Kennzeichen — und die Katalogprobe mit gesetzten Kästchen (Auswahlleiste im Fenster, die Liste springt
nicht).

**Papiere im selben Schritt:** die drei Anordnungsregeln in `EPOS.UI/CLAUDE.md` („LISTE in festem
Rahmen", „Projekt ↔ Datenbank immer über `Zweispaltenauswahl`", „KATALOGDIALOG nutzt die Höhe") und
neue Regeln für Auswahlleiste, Stammblatt und Kennzeichen; im Konzept Knopfleisten die Leseregel der
Fußleiste (V15); ein Nachtrag im Konzept Katalogfilter 5.6.1; die Bedienungsseiten unter
`Projekte/Wiki/` mit je einem Logbuch-Satz.
