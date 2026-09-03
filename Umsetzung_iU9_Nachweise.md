# Nachweisliste iU9 — die Maskenwellen W0 bis W5, Abnahme auf Windows

**Stand 03.09.2026 · Branch `ios_migration` · W0…W4 `aef9509`..`740c73e` ·
W5 `d95283c`..`f39b4a3` (Basis `740c73e`)**

Paket **iU9** des [`Umsetzungskonzept_iOS_EPOS-Plan.md`](Umsetzungskonzept_iOS_EPOS-Plan.md)
stellt die Masken des Bestands in **Wellen** auf Razor-Komponenten um; jede
WinForms-Fassung wird im selben Schritt gelöscht (Regel M1). Nach fünf Wellen
sind **30 Masken** umgestellt und **neun stillgelegt**; der Stapellauf der
Formularkarte zählt noch **88** von ursprünglich 118 Designer-Masken.

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
`WindowsFormsApplication1/Allgemein/Reporting/iU9_W1…W5_Blazor_Port_Protokoll.md`;
diese Liste bündelt nur, **was auf Windows nachzusehen ist**.

---

## 0. Was auf Linux schon steht (Stand nach W5)

| Nachweis | Ergebnis |
|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` | **0 Fehler, 20 Warnungen** (26 vor W4, 22 vor W5; WFO1000 30 → 14) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | **1 485 grün** (37 + 450 + 337 + 661) |
| `dotnet test Werkzeuge/Formularkarte/Formularkarte.sln -c Release` | **123 grün** |
| Stapellauf `--alle … --erreichbarkeit` | **88 Masken**, 86 × „ja", **0 × „nein", 0 × „verwaist"**, 2 × „unklar" |
| `Werkzeuge/SqlDialektPruefer` | 1 301 SQL-Texte, **0 Fundstellen** |
| `Proben/ChartProben` | 10 Bilder, **0 Verstöße** |
| `EPOS.Referenzlauf` 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` | **PASS/PASS/PASS**, `diff -rq` ohne Unterschied |
| `dotnet publish … -r win-x64 --self-contained` | `wwwroot` vollständig (`index.html`, `_framework/blazor.webview.js`, `_content/EPOS.UI/epos-ui.css`, QuickGrid) |

**Der Referenzlauf ist das Gate.** Jede Welle, die eine SQL-Anweisung
verschiebt oder einen Kern-Controller anfasst, wird gegen dieselbe eingefrorene
Basis gehalten. Bisher hat keine Welle ihn bewegt.

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
   der Punkt, an dem die Wellen 1–4 und die Welle 5 sich unterscheiden — siehe
   § 7).

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
| 5.1 | **DPI (§ 7)** — der eigentliche Entscheid der Welle |
| 5.2 | Projektwechsel im Kopfband: Alle vier Seiten folgen, **ohne** dass die Seite neu aufblitzt (die WebView bleibt) |
| 5.3 | Die Navigation schaltet um; genau eine Seite ist sichtbar; Tab kommt hinein, ↑/↓ wandern, Tab verlässt sie nach EINEM Druck |
| 5.4 | **Fokusfalle (W4‑O4/W5‑O4):** In jeder der sechzehn Überlagerungen mit Tab im Kreis laufen |
| 5.5 | Der lange Lauf: „Berechnen" und „Erstellen" mit Fortschritt und Abbrechen; die Meldungen stehen im Fenster statt in einer MessageBox |

---

## 7. Der offene Entscheid: DPI (Risiko R4, iF21)

**Das ist der Punkt, an dem die Welle 5 sich von allen vorherigen
unterscheidet.**

Die Anwendung läuft DPI-unbewusst (`app.manifest` `dpiAware=false`,
`Application.SetHighDpiMode(DpiUnaware)`). Für die gewachsenen WinForms-Masken
mit ihren fest gerechneten Pixelkoordinaten ist das die einzige Fassung, die
überall gleich aussieht.

**Die Dialoge der Wellen 1 bis 4 sind trotzdem scharf.** `BlazorDialogForm`
stellt den Faden für die Dauer des modalen Laufs auf „Per Monitor V2"
(`DpiInsel`); weil dabei sowohl das Fenster als auch das Fenster der WebView2
entsteht, ist der Inhalt scharf. Die Masken dahinter bleiben unberührt.

**Die Seiten der Welle 5 sind es nicht.** Eine eingebettete Seite hat kein
eigenes Fenster; sie sitzt im Fenster der DpiUnaware-`Form_Start`, und Windows
skaliert dieses Fenster als Bitmap. Ein Fenster kann seinen DPI-Kontext
nachträglich nicht wechseln — `BlazorSeite` versucht es deshalb gar nicht
erst.

**Abzunehmen ist:**

1. Reiter „Berichte & Kosten" bei **100 %** ansehen — erwartet: scharf.
2. Auf **125 %** stellen, Anwendung neu starten, denselben Reiter ansehen —
   erwartet: **sichtbar unscharf** (Bitmapskalierung).
3. Zum Vergleich einen **Dialog** der Wellen 1–4 bei 125 % öffnen — erwartet:
   scharf.
4. **Entscheiden:** Reicht der unscharfe Zustand, bis die Anwendung insgesamt
   DPI-fähig gemacht wird (eigenes Paket, **iF21**)? Oder muss iF21 vor der
   nächsten Seiten-Welle (W10 Simulationskonfiguration, W11
   Simulationsergebnis, W16 Startmaske) gezogen werden?

Die Entscheidung wirkt weit: **Alles Nicht-Modale** der kommenden Wellen hängt
daran.

---

**Entscheid des Anwenders (03.09.2026, iF21):** die Empfehlung ist angenommen — die
DPI-Insel bleibt für alle modalen Dialoge, die Anwendung wird **mit Welle 16** insgesamt
DPI-fähig geschaltet, wenn nur noch der Rahmen WinForms ist. Bis dahin sind die
eingebetteten Seiten (Berichte & Kosten aus W5, die Assistentenseiten aus W6 und W7, später
die Simulationsreiter) bei 125–200 % bitmapskaliert; auf 100 % und auf dem iPad sind sie
scharf. Der Gerätebefund der Insel bei 125 % und 150 % (Punkt 1 oben) bleibt zu führen.

## 8. Offene Punkte aller fünf Wellen (Kurzfassung)

| Welle | offen | Kurz |
|---|---|---|
| W1 | O1…O7 | Zuschuss-Schalter, Übernahme bleibt offen, kein Fortschritt, `SelectAll()` braucht JS, Löschung über `StammID`, Szenario-Persistenzwerte (**mit W5/A‑7 für die Wirtschaftlichkeitsseite geheilt**), Titel je Namensaufrufer |
| W2 | O1…O7 | Sprungbrücke am Gerät, Staffelform, Pflichtangabe Inbetriebnahme, Hausregel „leeres Feld behält Wert", tote Leerprüfungen, ungenutztes Sprungziel, Fenstermaß je Tarifsicht |
| W3 | O1…O7 | Dateiwähler ohne Besitzer, Zoomen im Kostenprofil, **O3 mit W5 erledigt**, „Abbrechen" im Katalog, drei tote Meldungen, zweispaltiger Katalog, `Zeilenwahl` nachziehen |
| W4 | O1…O8 | **O1 und O3 mit W5 erledigt**, `Rueckfrage` statt `Dienste.Dialog`, Fokusfalle, Modus-Schalter, englische Fassungen, Trägerwechsel ohne Rückfrage, Regelübernahme |
| W5 | O1…O8 | **DPI (§ 7)**, Spaltenbreiten der Trägertabelle, Knopf statt Doppelklick, Fokusfalle, `<progress>` statt Baustein, Kopfzeile des Reiters, Ladeverhalten der Übersicht |

---

## 9. Was diese Liste NICHT abdeckt

* **Den Rechenweg.** Er ist durch den Referenzlauf gedeckt und wurde von keiner
  der fünf Wellen bewegt.
* **Die Datenbank.** Kein Schema-Schritt, keine Migration.
* **iOS.** Die Komponenten laufen dort unverändert; nachgewiesen wird das im
  CI-Job `.github/workflows/ios.yml`
  ([`Umsetzung_iU10_Nachweise.md`](Umsetzung_iU10_Nachweise.md)).
