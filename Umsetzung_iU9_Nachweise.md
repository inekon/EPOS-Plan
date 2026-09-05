# Nachweisliste iU9 — die Maskenwellen W0 bis W10a, Abnahme auf Windows

**Stand 03.09.2026 · Branch `ios_migration` · W0…W4 `aef9509`..`740c73e` ·
W5 `d95283c`..`f39b4a3` (Basis `740c73e`) · W6 bis W10a `740c73e`..`427fd59`**

> **Diese Liste ist bis einschließlich Welle 10a fortgeschrieben.** Bis Welle 5
> stand sie still; die Wellen 6 (`198506f`), 7 (`e5114e1`), 8 (`8995d3e`),
> 9 (`04fc474`) und 10a (`427fd59`) sind seither nachgezogen — je auf ihrem
> zusammengeführten Stand.

Paket **iU9** des [`Umsetzungskonzept_iOS_EPOS-Plan.md`](Umsetzungskonzept_iOS_EPOS-Plan.md)
stellt die Masken des Bestands in **Wellen** auf Razor-Komponenten um; jede
WinForms-Fassung wird im selben Schritt gelöscht (Regel M1). Nach zehn Wellen
sind **70 Masken** umgestellt und **neun stillgelegt**; der Stapellauf der
Formularkarte zählt noch **50** von ursprünglich 118 Designer-Masken — zwei
Masken der Welle 10a (`Form_Quellprofil`, `Form_Waermesenke`) hatten nie einen
Designer und standen deshalb nie in dieser Zählung.

**Alle Nachweise dieser Wellen sind auf Linux geführt** — SDK 10.0.400, kein
Visual Studio, keine WebView2. Auf Linux lässt sich beweisen, dass alles
übersetzt, dass die Komponenten sich richtig verhalten (bunit), dass der
Rechenweg unberührt ist (Referenzlauf) und dass die Veröffentlichung die
richtigen Dateien enthält. **Nicht** beweisen lässt sich, wie die Masken
aussehen und sich anfühlen — genau das steht hier als abhakbare Liste, Welle
für Welle.

> **Ohne WebView2-Laufzeit ist nichts davon prüfbar.** Auf Windows 11 ist sie
> da. Auf einem Windows-10- oder LTSC-Rechner zuerst prüfen:
> `reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" /v pv`
> — steht dort nichts oder `0.0.0.0`, fehlt sie.

Die vollständigen Protokolle je Welle (Feldkartenabgleich, Abweichungsliste
A‑n, offene Punkte) stehen unter
`WindowsFormsApplication1/Allgemein/Reporting/iU9_W1…W10a_Blazor_Port_Protokoll.md`;
diese Liste bündelt nur, **was auf Windows nachzusehen ist**.

---

## 0. Was auf Linux schon steht (Stand nach W10a)

| Nachweis | Ergebnis |
|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` | **0 Fehler, 17 Warnungen** (20 von W5 bis W9; WFO1000 14 → 11 mit den Designern der Welle 10a) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | **2 284 grün** (450 + 337 + 1 269 + 228), **identisch unter `LC_ALL=en_US.UTF-8`** — der zweite Lauf ist Regel seit Welle 8, weil der Windows-Läufer en-US ist |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | **123 grün** |
| Stapellauf `--alle … --erreichbarkeit` | 51 Designer-Dateien, davon **50 Masken**, 29 lokalisiert, 49 erreichbar, **0 × „nein", 0 × „verwaist"**, 1 × „unklar" |
| `Werkzeuge/SqlDialektPruefer` | 1 240 SQL-Texte, **0 Fundstellen**; Selbsttest 32 Anweisungen / 0 Abweichungen |
| `Proben/ChartProben` | **16 Bilder**, 0 Verstöße (10 nach W5, 12 nach W7, 15 nach W8, 16 nach W10a) |
| `EPOS.Referenzlauf` 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` | **PASS/PASS/PASS**, 815 043 Werte, alle drei **byte-gleich** |
| `dotnet publish … -r win-x64 --self-contained` | `wwwroot` vollständig (`index.html`, `_framework/blazor.webview.js`, `_content/EPOS.UI/epos-ui.css`, QuickGrid) samt dem Kartenbild `bilder/Zonenkarte_Klimazonen.png` aus W10a |

**Der Referenzlauf ist das Gate.** Jede Welle, die eine SQL-Anweisung
verschiebt oder einen Kern-Controller anfasst, wird gegen dieselbe eingefrorene
Basis gehalten. Bisher hat keine Welle ihn bewegt — auch die Wellen 6 bis 10a
nicht, in denen zusammen über dreißig Kern-Controller angefasst wurden: alle
drei Projekte sind in jeder Welle byte-gleich geblieben.

---

## 1. Vorbedingungen, bevor die Liste beginnt

1. **WebView2** vorhanden (s. o.). Fehlt sie, bleiben alle Blazor-Flächen leer;
   die Anwendung startet trotzdem.
2. Ein Projekt mit **Vergleichsgruppe** (Stamm + mindestens zwei Varianten),
   mindestens einer simulierten Version und gepflegten Kosten — sonst sind die
   halben Listen leer und die Abnahme sagt nichts.
3. Beide Sprachen durchspielen: `HKCU\Software\wp-plan\Language` = 0 (deutsch)
   und 1 (englisch), Neustart dazwischen.
4. Die Anzeige einmal auf **125 %** und einmal auf **150 %** stellen (das ist
   der Punkt, an dem die modalen Dialoge und die eingebetteten Seiten sich
   unterscheiden — siehe § 12).
5. **Für die Wellen 6 bis 10a muss das Projekt bestückt sein:** je mindestens
   ein Heizkessel, ein BHKW, eine Wärmepumpe (mit gepflegten Kennlinien und
   gepflegter Regelung), eine PV-Anlage, ein Strom- und ein Pufferspeicher,
   Solarkollektoren und eine Solarganglinie, Gebäude, externer Wärmebedarf
   sowie Prozesswärme-, Stromverbraucher- und Brauchwasserzuordnungen. Ohne
   Bestückung bleiben die Projektlisten leer, und die halbe Liste sagt nichts.
6. **Für die Welle 10a zusätzlich** eine Simulationskonfiguration mit einer
   Wärmepumpe (Quelle Erdreich **und** Quelle Pufferspeicher), einem Heizkessel
   als zweitem Quellfall, mindestens zwei Wärmesenken und einem Projektpuffer —
   dazu **ein großes Projekt** für die Laufzeit- und Speichermessung des
   asynchronen Simulationslaufs (Punkt 10a.5).
7. Den **Assistenten** einmal von der ersten bis zur letzten Seite durchgehen:
   zehn der dreizehn Seiten sind seit den Wellen 6, 7 und 9 Razor-Komponenten,
   und der Speicherbedarf der zehn WebViews ist nur am Gerät messbar
   (Punkte 6.7, 7.9, 9.9).
8. **Die Grundprobe je Dialog** ist in allen Wellen dieselbe und wird in den
   Abschnitten unten nicht wiederholt: öffnet mittig, kein weißes Aufblitzen,
   ziehbar **und** maximierbar, Tabellen ohne Umbruch, deutsch **und** englisch,
   Hochkontrast, 125 % und 150 % scharf, Maus **und** Finger (44 px),
   Tab-Zyklus bleibt im Dialog, Esc schließt, Enter schließt NICHT (Ausnahme:
   der reine Entscheidungsdialog „Betriebsmodus", Punkt 10a.1), Infoknopf zeigt
   die Wikiseite.

---

## 2. Welle 1 — Kostenvorlagen-Kleindialoge und Kapitalwertverlauf

Sieben Masken → sechs Komponenten; Einstieg: **Menü Administration › Kosten ›
Kostenverwaltung…** bzw. **Berichte & Kosten › Kosten › „Kostenverwaltung
öffnen…"**.

| # | Punkt |
|---|---|
| 1.1 | Zeileneditor (✏️) — Feldbestand, Vorbelegung, OK ohne Bezeichnung meldet im Dialog |
| 1.2 | Namensabfrage („Neue Variante", „Speichern unter…") — Vorbelegung sichtbar, leer meldet, Enter = OK, Esc = Abbruch |
| 1.3 | Worst/Best („+/−") — vier Felder, „in %" nur mit Betrag ≠ 0, Startjahr 1 → 0 |
| 1.4 | Übernahme — Quelle umschalten sperrt die Listen, Vorschau nennt Quell- und Zielzahl; nach „Übernehmen" bleibt der Bereich offen (W1‑O2) |
| 1.5 | Kostenfaktor-Katalog — „Neu" mit leerem Feld tut nichts; „Löschen" fragt mit Namen; Hauptkomponente nicht löschbar |
| 1.6 | Kapitalwert-Verlauf — rechnet beim Öffnen, „Aktualisieren" zeichnet neu, „Abbrechen" hält an; **kein Fortschrittsbalken** (W1‑O3) |
| 1.7 | **W1‑O1 vorlegen:** Der Zuschuss-Schalter ist im Projektraster ersatzlos weg (A‑6) — soll die Größe künftig geführt werden? |
| 1.8 | **W1‑O5 vorlegen:** Gleichnamige Kostenfaktoren sind jetzt einzeln löschbar (Löschung über `StammID`) — gewollt? |

## 3. Welle 2 — Namensdialoge und Wirtschaftlichkeitsparameter

Sechs Masken; Einstieg: die 28 Aufrufer der Namensabfrage (Erzeuger,
Speicher, Katalogpflege) und **Berichte & Kosten › Wirtschaftlichkeit**.

| # | Punkt |
|---|---|
| 2.1 | Namensabfrage an je einem Aufrufer aus BHKW, Brauchwasser, Pufferspeicher, Solar |
| 2.2 | **Sprungbrücke (W2‑O1):** Parameter → „Gesetzeskatalog…" öffnet das WinForms-Fenster **modal über** dem Dialog und kehrt zurück. Trägt die verschachtelte Nachrichtenschleife? |
| 2.3 | Tarifstruktur in den Sichten „BHKW" und „Strombezug"; der gesperrte Block bleibt lesbar |
| 2.4 | PV-Vergütung — Marktwert-Import (Dateiwähler), Pflichtangabe „Inbetriebnahme" (W2‑O3) |
| 2.5 | **W2‑O2 sichtprüfen:** Die Staffel als 24 einzeln beschriftete Felder je Rolle — oder doch eine Tabelle? |

## 4. Welle 3 — Energieträger-Kleindialoge

Vier Masken; Einstieg: **Energieträgerverwaltung › Trägerkarte**.

| # | Punkt |
|---|---|
| 3.1 | Leistungspreisreihe — zwölf Monatssätze, Summe stimmt |
| 3.2 | Spotpreis-Import — **W3‑O1:** Erscheint der Dateiwähler VOR dem Dialog oder dahinter? (`IDateiDienst` ohne Besitzerfenster) |
| 3.3 | Emissionskatalog — beide Untereditoren in der Überlagerung; **W3‑O4:** „Abbrechen" gibt die Änderungsmerker trotzdem zurück |
| 3.4 | Kostenprofil — **seit W5 wieder drei Reiter** (W3‑O3 erledigt); das Betreten von „Grafik" zeichnet neu; **W3‑O2:** nicht zoombar |

## 5. Welle 4 — Kostenverwaltung und Energieträgerkatalog

Sieben Masken, 5 216 Zeilen; Einstieg: **Menü Administration › Kosten** und
**Berichte & Kosten › Kosten**.

| # | Punkt |
|---|---|
| 4.1 | Kostenverwaltung — Positionsraster mit sieben Spalten, Summenfuß, Abschlusszeile; **seit W5 zwei Reiter** (W4‑O1 erledigt), der zweite fehlt ohne Ertrag |
| 4.2 | Die **fünf Unterdialoge** stehen im selben Fenster (Überlagerung) |
| 4.3 | Trägerkarte — **seit W5 zwei Reiter**, Historie und Speichern unter der Leiste |
| 4.4 | „Aus Katalog übernehmen…" mit Haken samt „Alle"/„Keine" |
| 4.5 | **W4‑O5 vorlegen:** Der Modus-Schalter der Emissionen wirkt erst mit „Speichern" |
| 4.6 | **W4‑O6 vorlegen:** Die 27 englischen Fassungen — passt „Species" für Emissionsart? |
| 4.7 | **W4‑O7 vorlegen:** Ein Trägerwechsel verwirft ungespeicherte Änderungen ohne Rückfrage |

## 6. Welle 5 — die Seiten „Berichte & Kosten"

Sechs Masken, 5 192 Zeilen; Einstieg: **Startmaske › Reiter „Berichte &
Kosten"** und **Menü Projekte › Varianten und Bericht…**.

Die vollständige Liste mit 25 Punkten steht im Protokoll
(`iU9_W5_Blazor_Port_Protokoll.md`, § 9). Die **fünf**, an denen die Welle
hängt:

| # | Punkt |
|---|---|
| 5.1 | **DPI (§ 12)** — der eigentliche Entscheid der Welle |
| 5.2 | Projektwechsel im Kopfband: Alle vier Seiten folgen, **ohne** dass die Seite neu aufblitzt (die WebView bleibt) |
| 5.3 | Die Navigation schaltet um; genau eine Seite ist sichtbar; Tab kommt hinein, ↑/↓ wandern, Tab verlässt sie nach EINEM Druck |
| 5.4 | **Fokusfalle (W4‑O4/W5‑O4):** In jeder der sechzehn Überlagerungen mit Tab im Kreis laufen |
| 5.5 | Der lange Lauf: „Berechnen" und „Erstellen" mit Fortschritt und Abbrechen; die Meldungen stehen im Fenster statt in einer MessageBox |

## 7. Welle 6 — Erzeuger-Eingabemasken I

Sieben Masken → sieben Komponenten, 4 202 Zeilen, 55 `MessageBox`, 214
Kartenzeilen. **Vier davon sind zugleich Assistentenseiten** (PV,
Stromspeicher, Heizkessel, BHKW) — die ersten Razor-Komponenten im
Assistentenrahmen. Einstieg: die fünf Erzeugerkacheln des Startbilds, die
Kontextmenüs der Übersichtslisten, die Assistentenseiten 9–12 und die beiden
Katalogverwaltungen im Menü.

**Auf Linux steht** (Stand `198506f`): Build 0 Fehler / 20 Warnungen · Tests
**1 636** grün (+91 bunit, +27 Kern) · Formularkarte 123 grün · Stapellauf
**81** Masken (88 − 7), 79 erreichbar, 0 × „nein" · SQL-Prüfer 1 283 Texte,
0 Fundstellen · ChartProben 10 Bilder · Referenzlauf 1030/1007/1017 **PASS,
byte-gleich**.

| # | Punkt |
|---|---|
| 6.1 | **Startbild → Kachel Heizkessel** — Filter (10 Gruppen × 6 Stufen); ◀ öffnet die Trägerwahl in der Überlagerung, Abbruch fügt nichts hinzu; ▶ auf zwei Zeilen mit demselben Kessel entfernt die Projektkopie erst beim zweiten Mal; Trägerwechsel schreibt sofort; „Bearbeiten…" zeigt den Katalogeditor IM Fenster, „Administration…" öffnet die WinForms-Maske DARÜBER |
| 6.2 | **Startbild → Kachel BHKW** — wie 6.1, dazu: zweite Spalte „Eigenschaften" vierzeilig, Summe der Ptherm nach jedem ◀/▶, „Neu…" fragt erst den Namen und zeigt dann den Editor in derselben Überlagerung, „Löschen" auf einem Auslieferungssatz nennt den Grund |
| 6.3 | **Startbild → Kachel Photovoltaik** — Anlagenblock erscheint nur bei Projektzeile, gestrichelter Rahmen, Gesamtleistung folgt der Modulzahl, „Modul Bearbeiten…" springt in `Form_AdminPV` |
| 6.4 | **Startbild → Kachel Stromspeicher** — zweimal derselbe Speicher = zwei Zeilen, ▶ trifft die markierte, „Energie (Kapazität) [kWh]" und „Modulkosten [€/kWh]" stehen richtig da |
| 6.5 | **Startbild → Kachel Pufferspeicher** — ◀ auf ein bereits gewähltes Gerät fragt nach, „Nein" fügt nichts hinzu, „Ja" legt eine zweite Zeile an und der Speicherweg fragt NICHT erneut; die Projektzeile zeigt die Kopie (ggf. anderer Name) |
| 6.6 | **Übersichtslisten → Kontextmenü „Hinzufügen/Bearbeiten"** — fünfmal derselbe Dialog; beim Heizkessel auch auf der **REF-Liste** (dort war die linke Liste bisher leer, A‑14 / W6‑O‑3) |
| 6.7 | **Assistent → Seiten 9–12** (PV, Speicher, Kessel, BHKW) — Seitenwechsel unter 1 s, kein Aufblitzen, Rückkehr zeigt den aktuellen Listenstand, keine OK/Abbrechen-Leiste, keine Kostenleiste; Speicher der Browserprozesse im Auge behalten (R‑W6‑1) |
| 6.8 | **Menü Kataloge → Heizkessel-Admin → Bearbeiten** — Katalogeditor als eigenes Fenster, Mehrdeutigkeitshinweis bei doppeltem Namen, „Speichern unter" fragt den Namen in der Überlagerung |
| 6.9 | **Menü Kataloge → BHKW-Admin → Bearbeiten / Neu** — Summe der fünf Posten rechnet live, Investition folgt, „unbestimmt" bei Pel = 0, Rückfrage beim Auslieferungssatz |
| 6.10 | **In 6.1, 6.2, 6.8 und 6.9: die Kostenleiste** — „Investitionskosten…", „Betriebskosten…", „Energiekosten…" öffnen ein **zweites Fenster** und kehren sauber zurück (A‑1, R‑W6‑3, W6‑O‑5) |
| 6.11 | **W6‑O‑1 vorlegen:** Die Gruppen→`Brennstoff`-Ketten von Heizkessel und BHKW sind uneinheitlich — „Sonstige" trifft beim Heizkessel nie und ist auf `Brennstoff = 23` (im Katalog **Fernwärme**) abgebildet, die Gruppen „Fernwärme" und „Wasserstoff" fehlen der Kesselkette ganz, das BHKW nennt „Tierische Fette" zweimal. Künftig über `Tab_Brennstoff_Stamm.ID_Kategorie`? |
| 6.12 | **W6‑O‑2 vorlegen:** Die Filterstufe „Alle" heißt `Ptherm Like '%'` und lässt einen Katalogsatz **ohne** Ptherm herausfallen (in der Testdatenbank genau ein Kessel). Soll die Absicherung des Pufferspeichers nachgezogen werden? |

## 8. Welle 7 — Wärmepumpe und Solarthermie

Acht Masken (Wärmepumpe 5, Solarthermie 3) → acht Komponenten, 3 065 Zeilen,
43 `MessageBox`, 178 Kartenzeilen. **Zwei davon sind die Assistentenseiten 7
und 8.** Neu im Kern: das Renderer-Bild `ChartRenderer.Kennlinien`. Einstieg:
**Startbild › Kacheln Wärmepumpe und Solarthermie**, **Menü ›
Wärmepumpen-Datenbank**, **Menü Kataloge › Solarkollektoren** und
**Simulation-Detail**.

**Auf Linux steht** (Stand `e5114e1`): Build 0 Fehler / 20 Warnungen · Tests
**1 820** grün (+155 bunit, +29 Kern) · Formularkarte 123 grün · Stapellauf
**73** Masken (81 − 8), 71 erreichbar, 0 × „nein" · SQL-Prüfer 1 272 Texte,
0 Fundstellen · ChartProben **12** Bilder (`kennlinien_cop` und
`kennlinien_leistung` neu) · Referenzlauf **PASS, byte-gleich**.

| # | Punkt |
|---|---|
| 7.1 | **Startbild → Kachel Wärmepumpe** — Zeilen nur `WP_TYP`; „Neu.." zeigt erst den Katalog, dann die Detailansicht, und erst deren OK hängt die Zeile an; „Ändern.." ersetzt die Zeile; „Löschen" trifft die markierte, nicht den Index; „Ansicht" ist NUR LESEND und hat einen einzigen Knopf |
| 7.2 | **In 7.1 → Detailansicht** — zwei Bilder mit Kreis- und Kreuzmarken, eine Reihe je Vorlauf, Legende „35°C"; Bivalent an → Betriebsart; Alternativ/Teilparallel → Bivalenztemperatur; Vorlauf ≤ Rücklauf meldet aus `TemperaturenPruefen`; leere Pflichtzahl nennt ihr Feld; die Pufferspeichergruppe ist NICHT da. **Hier zugleich der Bildvergleich von Hand** gegen die abgelösten WinForms-Charts (W7‑O‑7; `bildvergleich` ist mit iF23 gelöscht) |
| 7.3 | **In 7.2 → „Kosten bearbeiten…"** — gesperrt bei ungespeicherter Anlage (der Grund steht als Zeile lesbar da), sonst „Invest … · Betrieb …"; öffnet ein **zweites Fenster** und kehrt sauber zurück (A‑1, R‑W6‑3) |
| 7.4 | **In 7.2 → „Parameter Bearbeiten…" → „Kennliniendaten…"** — **vier Ebenen** in einem Fenster: Verwaltung → Anlage → Stammdialog → Editor. Esc schließt nur die oberste; der Tabulator bleibt je Ebene gefangen; nach dem Editor sind die Bilder neu gezeichnet |
| 7.5 | **Menü → Wärmepumpen-Datenbank** — die Liste zeigt Auslieferungssätze gedimmt; Wahl füllt Felder und Bilder; der Umschalter Wärme/Kühlung steht nur mit Kühl-Kenndaten zur Verfügung und fällt bei jedem Zeilenwechsel auf Wärme zurück; Speichern/Löschen auf einem ReadOnly-Satz nennt den Grund; Löschen mit Projektzuordnung nennt das Projekt |
| 7.6 | **In 7.5 → „Modul-Katalog…"** — sieben Filter greifen einzeln und zusammen; `CS*7*` und `CS-0?0`; Klartext ist Teilsuche; Reset stellt VLT Max und Leist. Max auf die Höchstwerte der Daten zurück; die Trefferzahl steht über dem Raster |
| 7.7 | **Übersichtsliste → Kontextmenü** (Anzeigen / Bearbeiten / Neu) — dreimal dieselbe Detailansicht; **„Bearbeiten" LÖSCHT die Leistungsstufen nicht mehr** (W7‑O‑4, A‑21) — auf einer Anlage mit gepflegter Regelung nachsehen; auch auf der REF-Liste |
| 7.8 | **Simulation-Detail → Doppelklick auf die WP-Liste** — dieselbe Verwaltung wie 7.1; die Datei ist sonst unberührt |
| 7.9 | **Assistent → Seiten 7 und 8** — Wechsel unter 1 s, kein Aufblitzen, Rückkehr zeigt den aktuellen Listenstand, keine OK/Abbrechen-Leiste; „Löschen" trifft im Assistenten die richtige Zeile (A‑20); jetzt **sechs WebViews** im Assistenten (R‑W6‑1) |
| 7.10 | **Startbild → Kachel Solarthermie, Zweig Kollektorprofil** — ◀ ohne Katalogwahl gesperrt; ◀ legt eine Zeile mit Anzahl 1 an; ▶ entfernt die Projektkopie erst ohne zweite Referenz; Katalogzeile → Detail OHNE Kollektorgruppe, Projektzeile MIT; Anzahl ändern → Aperturfläche live; „Übernehmen" meldet statt zu blinken |
| 7.11 | **In 7.10 → „Kollektor in DB ändern/neu/löschen"** — der Editor steht IM Fenster; „neu" fragt erst den Namen; „löschen" fragt nach; nach jedem Editorlauf ist die Katalogliste neu gezogen |
| 7.12 | **Startbild → Kachel Solarthermie, Zweig Ganglinie** — ◀/▶ auf zwei Zuordnungen DERSELBEN Ganglinie treffen die markierte; Name UND Beschreibung stehen da (A‑27); „Bearbeiten…" öffnet die WinForms-Verwaltung DARÜBER und die Liste ist danach neu |
| 7.13 | **Menü Kataloge → Solarkollektoren → ändern / neu** — derselbe Katalogeditor als eigenes Fenster (die WinForms-Verwaltung bleibt); „k2" und „Investitionskosten" stehen an den richtigen Feldern (A‑12) |
| 7.14 | **W7‑O‑1 vorlegen:** Der Auffangzweig „ungültiges Suchmuster = kein Filter" im Katalogfilter ist **nicht erreichbar** — eine offene Klammer wird wörtlich gesucht und trifft nichts. Stehen lassen oder streichen? |
| 7.15 | **W7‑O‑2 vorlegen:** Die Baujahrliste des Stammdialogs trug „2024" **zweimal** und „2022" nie; mit A‑15 ist die Reihe lückenlos. War die Lücke Absicht? |
| 7.16 | **W7‑O‑5 vorlegen:** Die Vorlauf-/Rücklaufprüfung der Solarkollektoren prüfte `ID_Type == BHKW_TYP` und traf in dieser Maske **nie** zu; geschrieben hat faktisch immer nur „Übernehmen". Sollen die beiden Felder auch beim Verlassen schreiben? |

## 9. Welle 8 — die Bedarfsblätter (W8a + W8b)

Zehn Masken der drei Bedarfsblätter (Stromverbraucher, Prozesswärme,
Brauchwasser) und die Gebäudetypen-Verwaltung → **vier** Komponenten, 2 569
Zeilen, 41 `MessageBox`, 369 Kartenzeilen. Die drei Blätter sind Drillinge
desselben Blatts und unterscheiden sich nur in der Ausprägung (`BedarfsArt`).
Neu im Kern: drei Renderer-Bilder. Einstieg: die drei Bedarfskacheln des
Startbilds, **Simulation-Detail**, **Navigator Übersicht** und die drei
Admin-Masken.

**Auf Linux steht** (Stand `8995d3e`): Build 0 Fehler / 20 Warnungen · Tests
**1 906** grün (+66 bunit, +20 Kern) · Formularkarte 123 grün · Stapellauf
**63** Masken (73 − 10), 61 erreichbar, 0 × „nein" · SQL-Prüfer 1 254 Texte,
0 Fundstellen · ChartProben **15** Bilder (`monatssaeulen`,
`stundenprofil_woche`, `jahresverlauf_bedarf` neu) · Referenzlauf **PASS,
byte-gleich**.

| # | Punkt |
|---|---|
| 8.1 | **Startbild → Stromverbraucher → „Simulation"** — Ergebnis mit Startreiter „monatlich"; vier Kennzahlen; EINE Monatsreihe, also KEINE Optionsgruppe; die Säulen sind gelbgrün |
| 8.2 | **Startbild → Prozesswärme → „Simulation"** — sieben Kennzahlen, „davon Brauchwasser" OHNE Teiler 1000; zwei Optionsgruppen, die sich NICHT gegenseitig umschalten; Säulen rot bzw. blau |
| 8.3 | **Startbild → Brauchwasser → „Berechnen"** — der Titel trägt „ - ‹Name›"; Startreiter „Grafik" UND Brauchwassersicht; „Wärmebedarf Brauchwasser" MIT Teiler 1000; der Schalter „Jahresverlauf" erscheint nur hier und zeigt 8 760 Stunden über Monatsgrenzen. **Hier zugleich der Bildvergleich von Hand** gegen die abgelösten Charts (W8‑O‑7) |
| 8.4 | **In 8.1–8.3 → „DB ändern"** — Name gesperrt, Typliste gefüllt, zwölf Monatswerte mit vier Nachkommastellen; „Speichern" gesperrt; ein leeres Monatsfeld meldet den Monatsnamen (Strom) bzw. „Monat n" (Prozess/Brauchwasser) |
| 8.5 | **In 8.4 → „Speichern unter"** — Namensabfrage IM Fenster mit Vorbelegung; ein belegter Name meldet; danach zeigt das Namensfeld den neuen Namen, „Überschreiben" trifft aber weiterhin den **alten** Satz |
| 8.6 | **In 8.1–8.3 → „DB neu"** — erst der Name, dann der Dialog im Modus Neu: zwölf LEERE Felder, nur „Speichern" frei; ein leerer Typ meldet vor den Zahlen |
| 8.7 | **In 8.1–8.3 → „Typ ändern"** — Typliste links, 24 Stundenfelder, sieben Wochentage; ein Tagwechsel verwirft nicht übernommene Eingaben; „Änderungen Übernehmen" meldet und zeichnet das 168‑h‑Bild neu; „Tag kopieren"/„Tag einfügen" wirken (A‑6) |
| 8.8 | **In 8.7 → „Speichern in DB" / „Neu" / „Speichern unter" / „Löschen"** — ein Auslieferungstyp meldet und schreibt nicht; „Löschen" fragt nach; nach „Neu" steht der neue Typ gewählt mit 168 Nullen; „Speichern unter" nimmt die aktuellen Werte mit |
| 8.9 | **Startbild → Kachel „eigenes Stromprofil"** — derselbe Profildialog in der Ausprägung Stromverbraucher |
| 8.10 | **Menü → Gebäudetypen** und **Gebäude → „Gebäudetyp ändern"** — fünf bzw. acht Kurvennamen je nach KURVENZAHL; ein Kurvenwechsel überträgt still; ein Katalogtyp sperrt „Typ Speichern" und nennt den Grund als Zeile; „Typ hinzufügen" fragt Name UND Beschreibung; „Typ Löschen" fragt nach (A‑8) |
| 8.11 | **Simulation-Detail → „Strom-Details" und „Wärmebedarf-Details"** — Strom ohne Startreiter (Kennzahlen vorn), Wärme mit Reiter „monatlich"; die Datei ist sonst unberührt |
| 8.12 | **Navigator Übersicht → „Wärmebedarf"** — Startreiter „Grafik" und Brauchwassersicht |
| 8.13 | **Die drei Admin-Masken (W14b) → alle Knöpfe** — besonders `Form_Brauchwasser_Admin` → „Ergebnisse Verbrauch": dort öffnet sich wie bisher die **Prozess**-Ansicht ohne Brauchwassersicht (Befund W8‑B3) |
| 8.14 | **W8‑O‑1 vorlegen:** Die Prüfmeldungen heißen je Ausprägung anders — „Monatswert Januar" gegen „Monat 1", „Stundenwert 7" gegen „Stunde 7". Sollen alle drei Blätter denselben Wortlaut bekommen? |
| 8.15 | **W8‑O‑2 vorlegen:** „Speichern in DB" der drei Typprofile ruft **kein** `VerteilungUebernehmen` — was im Feld steht und nicht festgeschrieben wurde, geht nicht mit; der Gebäudetyp macht es umgekehrt. Soll „Speichern" die Felder mitnehmen? |
| 8.16 | **W8‑B3 / W8‑O‑3 vorlegen:** `Form_Brauchwasser_Admin` → „Ergebnisse Verbrauch" öffnet den PROZESS-Ergebnisdialog aus der BRAUCHWASSER-Verwaltung — also ohne Brauchwassersicht und ohne den Teiler 1000. War das Absicht? |
| 8.17 | **W8‑O‑5 vorlegen:** Der Brauchwasserwert wird **nur** in der Brauchwassermaske durch 1000 geteilt — dieselbe Größe, zwei Anzeigen; eine davon ist um den Faktor 1000 daneben. Welche ist richtig? |

## 10. Welle 9 — die Bedarfsmasken vom Startbild

Acht Masken der vier Bedarfskacheln → **fünf** Komponenten, 3 289 Zeilen, 42
`MessageBox`, 229 Kartenzeilen. Nach dieser Welle sind **alle elf Kacheln des
Startbilds** Blazor, und **zehn der dreizehn Assistentenseiten** laufen als
Razor-Komponente (die Seiten 2 bis 5 kommen hier dazu). Einstieg:
**Startbild**, **Menü › Gebäudeverwaltung**, der **Assistent** und die
Kontextmenüs der vier Übersichtslisten.

**Auf Linux steht** (Stand `04fc474`): Build 0 Fehler / 20 Warnungen · Tests
**2 066** grün (+98 bunit, +62 Kern), **identisch unter `LC_ALL=en_US.UTF-8`**
· Formularkarte 123 grün · Stapellauf **55** Masken (63 − 8), 54 erreichbar,
0 × „nein" · SQL-Prüfer 1 241 Texte, 0 Fundstellen · ChartProben 15 Bilder ·
Referenzlauf **PASS, byte-gleich**.

| # | Punkt |
|---|---|
| 9.1 | **Startbild → Gebäude** — der Umschalter „Wohngebäude"/„Gewerbe+Sonstige" lädt Katalog UND Artenliste neu; die vier Filterkombinationen; Wildcard-Suche „Haus*_1990*"; ◀ legt eine Zeile mit „Wohnfläche [m²]" und Nutzungsgrad 1 an; ▶ trifft die markierte Zeile |
| 9.2 | **In 9.1 → „Ändern"** — der Kopf ist nur lesbar; die Bedarfsart wechselt die Einheitsanzeige; ein leeres Feld meldet; OK schreibt **vier** Werte zurück — auch den Schalter „Dezentrale Warmwasserbereitung" (A‑2; vorher stiller Datenverlust) |
| 9.3 | **In 9.1 → „Gebäude in DB ändern…"** — zwei Reiter; Baujahr ↔ Buchstabe; Bauart aus Bauweise; 17 Pflichtzahlen melden ihren Feldnamen; „Überschreiben" trifft den Ursprungsnamen |
| 9.4 | **In 9.3 → Reiter „Gebäudedaten – Raumtemperaturen …"** — „Werte übernehmen" leitet ab und prüft die vier Ferienregeln; ohne den Knopf bleibt der Satz unberührt; Winter über die Jahresgrenze |
| 9.5 | **In 9.4 → „Brauchwasser…"** — die Brauchwasserliste des laufenden Projekts als Überlagerung; OK schreibt die Zuordnung |
| 9.6 | **In 9.1 → „Gebäude in DB neu…" / „…löschen" / „Gebäudetyp in DB ändern…"** — Neu ohne Markierung; Löschen fragt nach und meldet „Gebäude gelöscht!"; der Gebäudetyp ist die W8.4-Komponente |
| 9.7 | **Menü → Gebäudeverwaltung** — kein Projektteil, keine Pfeile, kein „Ändern" — nur der Katalog |
| 9.8 | **Startbild → Wärmebedarf extern** — die Kanalwahl wirkt auf die markierte Zeile; eine neue Zeile steht auf Heizung; „DB Ganglinie löschen" fragt nach (A‑8) und meldet bei Projektzuordnung; „Einlesen/Bearbeiten.." öffnet die Verwaltung über der Komponente (Sprungbrücke) |
| 9.9 | **Assistent → Seiten 2 bis 5** — jetzt **zehn WebViews** im Assistenten (R‑W6‑1 / R‑W9‑5): Speicher am Gerät messen; Vor/Zurück behält den Listenstand |
| 9.10 | **Startbild → Prozesswärme / Stromverbraucher / Brauchwasser** — je Ausprägung: die Katalogzeile zeigt Σ Monate, die Projektzeile die Summe der Zeile; „Übernehmen" ohne Zeile oder mit negativem Wert meldet — **beim Strom in kWh, sonst in MWh** (Befund W9‑B7) |
| 9.11 | **In 9.10 → „Simulation"** — Prozess und Strom rechnen ALLE Zuordnungen, Brauchwasser NUR die gewählte; danach ist „monatlicher Verlauf" frei und zeigt denselben Stand |
| 9.12 | **In 9.10 im Assistenten ohne gespeichertes Projekt** — der Hinweis „Vorschau ohne Projektwerte" kommt genau EINMAL — und beim Brauchwasser gar nicht |
| 9.13 | **In 9.10 → „DB ändern" / „DB neu" / „Typ in DB ändern"** — die drei W8-Komponenten als Überlagerung; „DB neu" fragt erst den Namen |
| 9.14 | **Kontextmenüs der vier Übersichtslisten** — Bearbeiten, Neu und Löschen führen zu denselben Dialogen und schreiben danach `OeffneGewerk` |
| 9.15 | **W9‑B1 / W9‑O‑1 vorlegen:** Der Katalogfilter „Gebäudeart gewählt, Baujahr Alle" filtert im Gebäudeart-Handler **ohne**, im Baujahr-Handler **mit** der Verwendung — welche Liste erscheint, hängt davon ab, welche Klappliste zuletzt angefasst wurde. Soll der Zweig die Verwendung immer mitfiltern? |
| 9.16 | **W9‑B6 / W9‑O‑2 vorlegen:** Die gespeicherte **Bauweise** hängt am Index der **Gebäudeart**-Klappliste, nicht an der Bauart-Klappliste — obwohl die Bauart aus derselben Größe abgeleitet angezeigt wird. Soll die Bauart-Klappliste die Bauweise bestimmen? |
| 9.17 | **W9‑B7 / W9‑O‑3 vorlegen:** Die Meldung bei ungültigem Jahresverbrauch nennt beim Stromverbraucher **kWh**, bei Prozess und Brauchwasser **MWh** — für dieselbe Größe, die überall in MWh angezeigt und gespeichert wird. Welche Einheit ist richtig? |
| 9.18 | **W9‑B9 / W9‑O‑4 vorlegen:** „Überschreiben" im Katalogeditor trifft den **Ursprungsnamen** — der Vorläufer schrieb nach einem Umbenennen 0 Zeilen mit stiller Erfolgsmeldung; das ist behoben, soweit es ohne Fachentscheid ging. Soll „Überschreiben" umbenennen dürfen, oder soll das Namensfeld im Modus Bearbeiten gesperrt sein? |
| 9.19 | **W9‑B10 / W9‑O‑5 vorlegen:** Der **Admin-Modus** des Katalogeditors hat im ganzen Bestand keinen Aufrufer; er ist übernommen, weil er vollständig ausformuliert dastand. Soll er über einen Menüpunkt erreichbar werden, oder fällt er ersatzlos weg? |

## 11. Welle 10a — Simulationskonfiguration I: die sieben Dialoge

Sieben Masken → sieben Komponenten, 7 803 Zeilen, 30 `MessageBox`, 69
Kartenzeilen; das Steuerelement `KlimazonenKarte` (326 Z.) fällt mit seiner
einzigen Maske. **`Form_Simulation_Config` selbst bleibt bis W10b WinForms**
und ruft die sieben Dialoge über Hüllen — der Einstieg ist deshalb durchweg
die **Simulationskonfiguration**.

**Auf Linux steht** (Stand `427fd59`): Build 0 Fehler / **17** Warnungen (20
nach W9) · Tests **2 284** grün (+165 bunit, +53 Kern), **identisch unter
`LC_ALL=en_US.UTF-8`** · Formularkarte 123 grün · Stapellauf **50** Masken
(55 − 5; `Form_Quellprofil` und `Form_Waermesenke` hatten nie einen Designer),
49 erreichbar, 0 × „nein" · SQL-Prüfer 1 240 Texte, 0 Fundstellen ·
ChartProben **16** Bilder (`jahresgang_erdreich` neu) · Referenzlauf **PASS,
byte-gleich** · `dotnet publish` mit `wwwroot` **samt Kartenbild**.

Zwei Vorabproben haben die Bauweise bestimmt: `SimulationRunner.Simuliere`
läuft in `Task.Run` fehlerfrei gegen die Testdatenbank (R‑W10a‑2 — deshalb
rechnet der Erdreichdialog asynchron mit Wartezustand), und das Kartenbild
misst 1,29 MiB und bleibt deshalb unverkleinert (R‑W10a‑3).

| # | Punkt |
|---|---|
| 10a.1 | **Erzeugerkarte → Betriebsmodus** — nur für Wärmepumpen (die Vorprüfung bleibt beim Aufrufer); ein leerer oder unbekannter `BM_Typ` steht auf „laufzeitoptimiert"; unter jedem Wahlknopf steht seine Erläuterung; **Enter bestätigt** (reiner Entscheidungsdialog); der PV-Hinweis kommt weiterhin vom Aufrufer, wenn keine PV-Anlage in der Simulation steht |
| 10a.2 | **Wärmequelle Erdreich** — Bodentyp und Klimazone laufen über den **Schlüssel** (A‑3): einen Katalogeintrag in der Mitte einfügen und prüfen, dass die Anzeige stimmt; zwischen Erdkollektor und Erdsonde hin- und herschalten — **die Eingaben des anderen Zweigs bleiben stehen** (A‑4) |
| 10a.3 | **In 10a.2 → „…" neben der Klimazone** — die Karte erscheint als **Überlagerung**, nicht als zweites Fenster; das Kartenbild ist da (sonst fehlt `wwwroot/bilder/…` in der Veröffentlichung); Zeigen färbt, Klicken wählt, Doppelklick übernimmt; **Tab und Enter** erreichen jede der 15 Zonen (A‑15); „nicht zugeordnet" ist auf der Karte nicht wählbar, in der Liste dahinter schon (W10‑B4); OK ohne Auswahl ändert nichts; bei **150 %** die Fenstergröße prüfen (W10a‑O‑7) |
| 10a.4 | **In 10a.2 → „Simulation"** — der Knopf sperrt, die Wartefläche erscheint, das Fenster bleibt bedienbar und **friert nicht ein** (A‑5); danach stehen Prüfergebnis, Vorbehalt und Frosthinweis; das Jahresgangbild zeigt **zwei** Reihen (Quell- und Außentemperatur) |
| 10a.5 | **Wie 10a.4, aber mit einem großen Projekt** — Laufzeit und Speicher messen: der Lauf ist ein vollständiger Jahresgang in einem fremden Faden gegen die **geöffnete** Anwenderdatenbank. Fällt das durch, wäre der Rückweg der synchrone Lauf nach `await Task.Yield()` |
| 10a.6 | **Pufferspeicher (Verwaltung)** — **kein Abbrechen** (W10‑B29): Anlegen, Ändern und Entfernen wirken sofort, Esc nimmt nichts zurück; das **Klassen-Set** ist die Pflichtangabe, die abgeleitete Verwendung steht als leise Zeile darunter; die Wechsel-Rückfrage kommt beim Übernehmen, nicht beim Klicken; die grünen Statuszeilen sind jetzt Erfolgsbanner **mit Zeichen** (A‑11) |
| 10a.7 | **In 10a.6 → „Katalog ansehen"** — springt in die Pufferspeicher-Verwaltung **nur zum Ansehen** (A‑13); dort darf nichts schreibbar sein; zurück steht der Projektdialog unverändert |
| 10a.8 | **In 10a.6 → Schichtung ab zwei Schichten** — die Schichtfelder erscheinen erst dann (die Sichtbarkeitsregel bleibt, die 20 Laufzeitfelder sind weg); die Kapazität rechnet nach `V × 1,16 × ΔT / 1000` und bleibt leer, wenn Volumen ≤ 0 oder Vorlauf ≤ Rücklauf |
| 10a.9 | **Wärmequelle Pufferspeicher (Wärmepumpe)** — Parameterblock sichtbar; der Haken „unbegrenzt verfügbar" **und** ein gewählter Puffer ergeben eine Warnung samt der Temperatur, die dann gälte — der Dialog **verwirft nichts**; die Pufferliste ist ungefiltert |
| 10a.10 | **Dasselbe für einen Heizkessel** — kein Parameterblock, dafür Kaskadenhinweis und Temperaturbezug; **Quelltemperatur, Spreizung, Regeneration und „unbegrenzt" bleiben unangetastet** (W10‑B15) — nach dem Speichern prüfen, dass die WP-Vorgaben noch dastehen |
| 10a.11 | **In 10a.9/10a.10 → „Pufferspeicher verwalten"** — erscheint als Überlagerung (A‑12); nach dem Schließen steht die Liste neu, die Auswahl bleibt |
| 10a.12 | **In 10a.9 ohne Puffer im Projekt bzw. mit Altbezeichner** — **zwei** verschiedene Banner mit eigener Stufe (A‑6); beide dürfen gleichzeitig stehen |
| 10a.13 | **Quellprofil** — die Betriebsart wechseln: der Reiter wechselt mit, und **das vorderste Blatt ist das der neuen Betriebsart**; „alle Monate" und „alle Werte" fragen beide über dieselbe Abfrage und melden beide (A‑7) |
| 10a.14 | **In 10a.13 → Betriebsart Stunde, CSV einlesen** — 8 760 Zeilen im virtualisierten Raster, flüssig scrollen; eine unlesbare Zelle **färbt** (A‑8); Min/Max/Mittel stimmen |
| 10a.15 | **In 10a.13 mit einem Wochengang an der Anlage** — der Altweg-Reiter „Wochenwerte" erscheint, ist **nur lesend**, und die Herleitungszeile sagt, dass er nicht mehr wirkt (W10‑B17) |
| 10a.16 | **Wärmesenken** — die Rangfolge tauschen: die **PV-Sonderpriorität wandert nicht mit** (nur Rang 1 kennt sie); ein Feld, das nicht zum Ziel passt, wird beim Zielwechsel **gelöscht** und wirkt nicht heimlich weiter |
| 10a.17 | **In 10a.16 → Ladegrenze / Anschlusshöhe ungültig** — der Fehler steht im **Formular**, nicht im Modell (A‑9): Meldung beim OK, mit dem alten Wortlaut und der Nennung des Rangs; nach dem Korrigieren speichert OK ohne Rest |
| 10a.18 | **In 10a.16 → Parallelverbund** — zur Wahl stehen nur Speicher **derselben Verwendung**, das Speicher-Dropdown darüber zeigt **alle**; die beiden Herleitungszeilen erklären den Unterschied (W10‑B26); der Verbund hängt weiter an Rang 1 (W10‑B25) |
| 10a.19 | **Schema-Ansicht und Erzeugerkarten der Simulationskonfiguration** — sie zeigen die Senken über `WaermesenkeClass.SenkeAnzeige`; nach jeder Senkenänderung prüfen, dass Karte und Schema denselben Text zeigen wie der Dialog |
| 10a.20 | **Sprache auf en umstellen** und 10a.1–10a.19 stichprobenartig wiederholen — alle 266 Textschlüssel liegen in beiden Sprachen; die Steuerwerte (Modus, Kanal, Boden, Zone, Verwendung) dürfen sich **nicht** mit übersetzen |
| 10a.21 | **W10‑B10 / W10a‑O‑1 vorlegen:** Die Auslegungsprüfung rechnet mit den **angezeigten** Eingaben, der Simulationslauf mit dem **Datenbankstand** — wer etwas ändert und sofort „Simulation" drückt, sieht eine Prüfung zu den neuen und ein Ergebnis zu den alten Werten. Soll der Knopf vorher speichern, oder soll die Prüfung auf den Datenbankstand umgestellt werden? |
| 10a.22 | **W10‑B25 / W10a‑O‑2 vorlegen:** Der Parallelverbund hängt **konstruktiv an Rang 1** — `Z_AnlagePufferVerbund` führt keine `ID_Senke`, ein Verbund auf Rang 2 ist im Schema nicht abbildbar. Soll die Tabelle eine `ID_Senke` bekommen? Das wäre ein Migrationsschritt, keine Maskenfrage |
| 10a.23 | **W10‑B26 / W10a‑O‑3 vorlegen:** Das Speicher-Dropdown zeigt **alle** Projektpuffer, die Verbundliste filtert nach Verwendung — zwei Listen desselben Dialogs mit verschiedenen Regeln. Soll das Dropdown ebenfalls filtern? |
| 10a.24 | **W10‑B29 / W10a‑O‑4 vorlegen:** Die Pufferverwaltung hat **kein Abbrechen** — jede Handlung wirkt sofort, die Leiste trägt nur „Schließen". Soll der Dialog auf „Sammeln und beim Schließen schreiben" umgebaut werden? |

---

## 12. Der DPI-Entscheid (Risiko R4, iF21)

**Das ist der Punkt, an dem die eingebetteten Seiten sich von allen modalen
Dialogen unterscheiden.**

Die Anwendung läuft DPI-unbewusst (`app.manifest` `dpiAware=false`,
`Application.SetHighDpiMode(DpiUnaware)`). Für die gewachsenen WinForms-Masken
mit ihren fest gerechneten Pixelkoordinaten ist das die einzige Fassung, die
überall gleich aussieht.

**Die modalen Dialoge sind trotzdem scharf.** `BlazorDialogForm` stellt den
Faden für die Dauer des modalen Laufs auf „Per Monitor V2" (`DpiInsel`); weil
dabei sowohl das Fenster als auch das Fenster der WebView2 entsteht, ist der
Inhalt scharf. Die Masken dahinter bleiben unberührt. Das gilt für die Dialoge
aller Wellen 1 bis 10a.

**Die eingebetteten Seiten sind es nicht.** Eine eingebettete Seite hat kein
eigenes Fenster; sie sitzt im Fenster der DpiUnaware-`Form_Start`, und Windows
skaliert dieses Fenster als Bitmap. Ein Fenster kann seinen DPI-Kontext
nachträglich nicht wechseln — `BlazorSeite` versucht es deshalb gar nicht
erst. Dasselbe gilt für die Assistentenseiten (`BlazorAssistentSeite`,
`TopLevel = false`) der Wellen 6, 7 und 9.

**Entschieden am 03.09.2026 — iF21, Umsetzungskonzept § 8.2: „Insel jetzt,
Anwendung DPI-fähig mit W16."** Die Empfehlung ist angenommen: Die `DpiInsel`
aus iU8-6 deckt alle modalen Dialoge und damit auch die Masken der Wellen 6 bis
10a; die Anwendung wird **mit Welle 16** insgesamt DPI-fähig geschaltet, wenn
nur noch der Rahmen WinForms ist. Bis dahin bleiben die eingebetteten Flächen
— „Berichte & Kosten" aus W5, die Assistentenseiten der Wellen 6, 7 und 9,
später die Simulationsreiter — bei 125–200 % bitmapskaliert; auf 100 % und auf
dem iPad sind sie scharf.

**Der Gerätebefund steht weiterhin aus** — die Entscheidung ersetzt ihn nicht:

1. Reiter „Berichte & Kosten" bei **100 %** ansehen — erwartet: scharf.
2. Auf **125 %** stellen, Anwendung neu starten, denselben Reiter ansehen —
   erwartet: **sichtbar unscharf** (Bitmapskalierung). Dasselbe bei **150 %**.
3. Zum Vergleich einen **modalen Dialog** bei 125 % und 150 % öffnen —
   erwartet: scharf. Die Grundprobe in § 1 Punkt 8 führt das ohnehin je Maske.
4. Eine **Assistentenseite** der Wellen 6, 7 oder 9 bei 125 % ansehen —
   erwartet wie 2: bitmapskaliert.

Die Entscheidung wirkt weit: **Alles Nicht-Modale** der kommenden Wellen hängt
daran — W10b und W11 (Simulationskonfiguration und Simulationsergebnis) ebenso
wie W16 (Startmaske), mit der die Bitmapskalierung fällt.

### 12.1 iF21 ist eingelöst — Per Monitor V2 seit iU9‑W16c.4 (04.09.2026)

**Der Entscheid E‑6 der Welle 16 ist umgesetzt.** `app.manifest` trägt
`dpiAware=true/pm` und `dpiAwareness=PerMonitorV2`, `Program.Main` setzt
`HighDpiMode.PerMonitorV2`. Die `DpiInsel` in `BlazorDialogForm` ist im selben
Schritt **gelöscht** — samt den zwei `ShowDialog`-Überladungen, die sie
umschlossen: Ein Fenster, das ohnehin im richtigen Kontext entsteht, braucht
keine Insel.

**Warum es jetzt geht.** Der Grund für `DpiUnaware` waren die fest gerechneten
Pixelkoordinaten der gewachsenen WinForms-Masken. Nach Welle 16 gibt es sie
nicht mehr: Die Oberfläche ist eine Razor-Seite in einer WebView und rechnet in
relativen Einheiten; es bleiben `Form_HelpPopup` (eine Sprechblase ohne feste
Größe, bis iU11) und die DPI-freie Hülle `MDIMainForm` (129 Zeilen).

**Der Gerätebefund steht aus** — auf Linux ist nur der Bau prüfbar. Abzunehmen
bei **100 / 125 / 150 %**, jeweils nach einem Neustart:

1. **Menüband und Kopfband** — erwartet: scharf, keine Bitmapskalierung.
2. **Startseite mit ihren 21 Kacheln** und den sechs Reitern — erwartet:
   scharf; die Kacheln brechen um, statt zu skalieren (das Raster ist
   `auto-fit`/`minmax`, es gibt keine feste Fläche mehr).
3. **Ein modaler Blazor-Dialog** (z. B. Menü → Administration → Kosten →
   Kostenverwaltung…) — erwartet: scharf wie vorher; die Insel fehlt, der
   Prozesskontext ersetzt sie.
4. **`Form_HelpPopup`** (die letzte Designer-Maske: Hilfe-Sprechblase an einem
   `InfoKnopf`) — erwartet: scharf und richtig platziert. **Das ist der
   Punkt, an dem eine echte Abweichung auftreten könnte**: Sie ist die einzige
   Maske, die von der Umstellung betroffen ist.
5. **`Form_SpeicherOptimierung`** (iF22, ScottPlot, über die `Sprungbruecke`
   aus der Ergebnisseite) — erwartet: scharf; die ScottPlot-Fläche rechnet in
   Gerätepunkten.
6. **Bildschirmwechsel im Betrieb** (zwei Monitore mit verschiedener
   Skalierung, Fenster hinüberziehen) — das ist der eigentliche Gewinn von
   „Per Monitor V2" und war unter `DpiUnaware` gar nicht möglich.

## 13. Offene Punkte aller zehn Wellen (Kurzfassung)

| Welle | offen | Kurz |
|---|---|---|
| W1 | O1…O7 | Zuschuss-Schalter, Übernahme bleibt offen, kein Fortschritt, `SelectAll()` braucht JS, Löschung über `StammID`, Szenario-Persistenzwerte (**mit W5/A‑7 für die Wirtschaftlichkeitsseite geheilt**), Titel je Namensaufrufer |
| W2 | O1…O7 | Sprungbrücke am Gerät, Staffelform, Pflichtangabe Inbetriebnahme, Hausregel „leeres Feld behält Wert", tote Leerprüfungen, ungenutztes Sprungziel, Fenstermaß je Tarifsicht |
| W3 | O1…O7 | Dateiwähler ohne Besitzer, Zoomen im Kostenprofil, **O3 mit W5 erledigt**, „Abbrechen" im Katalog, drei tote Meldungen, zweispaltiger Katalog, `Zeilenwahl` nachziehen |
| W4 | O1…O8 | **O1 und O3 mit W5 erledigt**, `Rueckfrage` statt `Dienste.Dialog`, Fokusfalle, Modus-Schalter, englische Fassungen, Trägerwechsel ohne Rückfrage, Regelübernahme |
| W5 | O1…O8 | **DPI (§ 12)**, Spaltenbreiten der Trägertabelle, Knopf statt Doppelklick, Fokusfalle, `<progress>` statt Baustein, Kopfzeile des Reiters, Ladeverhalten der Übersicht |
| W6 | O‑1…O‑6 | **O‑1** uneinheitliche Brennstoff-Ketten · **O‑2** Filterstufe „Alle" lässt Sätze ohne Ptherm fallen · **O‑3** REF-Liste des Heizkessels blieb leer (**mit A‑14 behoben**) · **O‑4** SQL-Prüfer löste lokale Variablen gegen fremde Konstanten auf (**im Prüfer behoben**) · **O‑5** Kostenleiste als zweites Fenster (**Verschmelzung nach W16**) · **O‑6** KI-Aufrufknopf fehlt (**mit W15b**) |
| W7 | O‑1…O‑7 | **O‑1** unerreichbarer Auffangzweig des Katalogfilters · **O‑2** Baujahrliste 2024 doppelt / 2022 fehlt (**mit A‑15 lückenlos**) · **O‑3** verwaister `btn_Ansicht`-Handler (**ersatzlos entfallen**) · **O‑4** Kontextmenü löschte die Leistungsstufen (**mit A‑21 behoben**) · **O‑5** nie greifende Vorlauf-/Rücklaufprüfung der Solarkollektoren · **O‑6** KI-Knopf (**W15b**) · **O‑7** Kennlinienbilder nur von Hand vergleichbar (`bildvergleich` mit iF23 gelöscht) |
| W8 | O‑1…O‑7 | **O‑1** Wortlaut der Prüfmeldungen je Ausprägung · **O‑2** „Speichern in DB" ohne `VerteilungUebernehmen` · **O‑3** (B3) Brauchwasser-Admin öffnet die Prozessansicht · **O‑4** `GetMaxID + 1` auf `Tab_DBTagV_STAMM` (**Schemafrage, eigenes Paket**) · **O‑5** Teiler 1000 nur im Brauchwasser · **O‑6** KI-Knopf (**W15b**) · **O‑7** die drei Bedarfsbilder nur von Hand vergleichbar |
| W9 | O‑1…O‑7 | **O‑1** (B1) Filterzweig ohne Verwendung · **O‑2** (B6) Bauweise am Index der Gebäudeart · **O‑3** (B7) kWh gegen MWh in derselben Meldung · **O‑4** (B9) „Überschreiben" nach Umbenennen (**soweit ohne Fachentscheid behoben**) · **O‑5** (B10) Admin-Modus ohne Aufrufer · **O‑6** KI-Knopf (**W15b**) · **O‑7** Speicherbedarf von zehn WebViews im Assistenten |
| W10a | O‑1…O‑8 | **O‑1** (B10) Prüfung gegen Anzeige, Lauf gegen Datenbankstand · **O‑2** (B25) Parallelverbund konstruktiv an Rang 1 (**Migrationsschritt**) · **O‑3** (B26) Speicherliste gegen Verbundliste · **O‑4** (B29) Pufferverwaltung ohne Abbrechen · **O‑5** (B39) fehlende `de-DE.resx` von `Form_Simulation_Config` (**erledigt sich mit W10b**) · **O‑6** KI-Knopf (**W15b**) · **O‑7** Kartengröße bei 150 % · **O‑8** **ein nicht reproduzierbarer Testausfall unter `en_US`** (einer von dreizehn Läufen; Vorschlag: ausdrückliche Frist für die drei `WaitForAssertion` aus Welle 1 statt der Vorgabefrist) |
| W15c | O‑1…O‑5 | **iF30** Lesemodus-Durchsetzung (nach W16; heute genau EIN Leser, B7) · **O‑1** Vertragsendpunkt `epos/v1/vertrag` statt AGB-Seite (E‑17, B27 — die Quelle ist EINE Zeile) · **O‑2** ein `LizenzTexte`-Bündel für die zwei großen Komponenten · **O‑3** Textsuche im Vertragstext entfallen (E‑12) · **O‑4** Lizenzeinstieg auf iOS (E‑4, iU11) · **O‑5** `Form_HelpPopup` bleibt bis iU11 |

**Die Windows-Abnahme der Welle 15c steht in ihrem Protokoll**
([`WindowsFormsApplication1/Allgemein/Reporting/iU9_W15c_Blazor_Port_Protokoll.md`](WindowsFormsApplication1/Allgemein/Reporting/iU9_W15c_Blazor_Port_Protokoll.md),
§ 11) und trägt **zwei Punkte, die es vorher nicht gab**: den Erststart auf einem
echten `.accdb`-Bestand (mit und ohne Fehlschlag) und die **Wiederholung des
Sandbox-Nachweises ohne WebView2** — seit W15c startet die Anwendung ohne die
Laufzeit nicht mehr, sondern meldet und endet (Befund W15c‑B10,
[`Umsetzung_iU8_Nachweise.md`](Umsetzung_iU8_Nachweise.md)).

**Neu hergestellt statt nachgebaut:** Die WinForms-Klimazonenkarte konnte ihre
ausgelieferte SVG **nie** lesen (W10a‑B41: der Parser erwartete den Pfadbefehl
getrennt von der ersten Koordinate, `float.Parse("M315.30")` warf, ein leerer
`catch` verschluckte es) — die Maske zeigte immer nur ihre Ladefehlerzeile. Die
Blazor-Fassung ist damit kein Nachbau, sondern die erste funktionierende
Ausgabe dieser Maske; Punkt 10a.3 nimmt sie zum ersten Mal ab.

---

## 14. Was diese Liste NICHT abdeckt

* **Den Rechenweg.** Er ist durch den Referenzlauf gedeckt und wurde von keiner
  der zehn Wellen bewegt — alle drei Projekte sind in jeder Welle byte-gleich
  geblieben.
* **Die Datenbank.** Kein Schema-Schritt, keine Migration; die beiden
  Schemafragen (W8‑O‑4, W10a‑O‑2) gehören in eigene Pakete.
* **iOS.** Die Komponenten laufen dort unverändert; nachgewiesen wird das im
  CI-Job `.github/workflows/ios.yml`
  ([`Umsetzung_iU10_Nachweise.md`](Umsetzung_iU10_Nachweise.md)).
