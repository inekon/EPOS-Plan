# Konzept: Navigation der Seite „Berichte & Kosten“ — Vorschlag mit drei Varianten

**Umgesetzt — Variante A (Anwenderentscheide BN-Q1 bis BN-Q3 vom 27.09.2026), Etappen A1 bis A4
(#590); Protokoll: [`BN_A_Navigation_Protokoll.md`](Protokolle/Bericht/BN_A_Navigation_Protokoll.md).
Der gültige Stand steht im Code (`EPOS.UI/Seiten/Berichte/BerichteKostenSeite.razor`,
`EPOS.UI/Bausteine/Reiter.razor`) und in den Wiki-Quellen. Das Papier ist Geschichte.**

**Rev. 1 — 26.09.2026 — Vorschlag.** Anlass ist der Befund des
Anwenders (PDF vom 26.09.2026, Seite 7): „Vorschlag für besseres Design des Dialogs —
insbesondere die linke Spalte mit Übersicht, Kosten, Wirtschaftlichkeit, Bericht.“ Dieses
Papier ändert nichts am Code; es stellt drei Mockups nebeneinander, empfiehlt eines und
nennt die Etappen.

Mockups (statisch, 1 280 px, Token aus dem Stilblatt epos-ui.css des Hauses):
[A — Reiterzeile](../aktuell/Mockups/BerichteKosten_Navigation_A.html) ·
[B — schlanke Seitenleiste](../aktuell/Mockups/BerichteKosten_Navigation_B.html) ·
[C — Kachelstart](../aktuell/Mockups/BerichteKosten_Navigation_C.html);
Bilder: [A](../aktuell/Mockups/BerichteKosten_Navigation_A.png), [B](../aktuell/Mockups/BerichteKosten_Navigation_B.png),
[C](../aktuell/Mockups/BerichteKosten_Navigation_C.png).

Bild 3 jedes Mockups zeigt den Bereich *Wirtschaftlichkeit* mit dem Knopf **„Zum Bericht ›“**
anstelle von „Bericht erzeugen“ (entschieden am 26.09.2026, Umsetzung in eigenem Auftrag):
Er wechselt in den Bereich *Bericht*, hakt dort Wirtschaftlichkeit an und übergibt
Vergleichsgruppe und Szenario; erzeugt wird erst dort. In A und B ist das ein gewöhnlicher
Reiter- bzw. Punktwechsel, in C führt der Rückweg danach zum Kachelstart statt zurück.


## 1. Ist-Stand

- **Datei:** `EPOS.UI/Seiten/Berichte/BerichteKostenSeite.razor` — Rahmen mit Navigation
  links, Kopfzeile (Rückweg, Titel mit Stammname, Platzhalter-Umschalter, Hilfe) und der
  gewählten Seite rechts. Er steht an zwei Orten: als sechstes Reiterblatt der Startseite
  und als eigene Ansicht der `AppWurzel` (Menü „Varianten und Bericht…“, mit Rückweg).
  Die Gaben baut `EPOS.UI.Daten/Bericht/BerichteKostenHuelle.cs` je Seitenschlüssel.
- **Navigation:** eigene Stilfamilie `.epos-navigation*` in `epos-ui.css`, **208 px** breit,
  Fläche `#23282d`, aktiver Punkt `#0073aa`, Hover `#00b9eb` — Farben der alten
  Detailsimulation, **nicht als Token** geführt. Sinnbilder sind Textzeichen (☰ € ▤ ▦).
  Bedienung als `tablist` senkrecht (↑ ↓ Pos1 Ende); die Seiten entstehen erst beim ersten
  Aufruf.
- **Die vier Seiten:** *Übersicht* (Vergleichsgruppe: Stamm, Varianten anlegen, umbenennen,
  simulieren; Unterschiede zum Stamm), *Kosten* (Kategoriekarten Investition, Betrieb,
  Energie; Anlagen- und Energieträgertabelle mit zehn Spalten; Befunde), *Wirtschaftlichkeit*
  (Kennzahlen und ValERI-Sicht, Vergleichsmatrix, Verlauf, Sensitivität — die breiteste
  Seite), *Bericht* (Varianten, Vorlagenblock, Erstellen).
- **Hausmuster für Unterbereiche:** der Baustein `Reiter`/`Reiterblatt` — Hauptleiste der
  Startseite, Reiter der Simulationsergebnisse (Übersicht · Bedarf · je Erzeuger · Ergebnis),
  15 Dialoge; die senkrechte Form `.epos-reiter--senkrecht` nur im Einstellungsdialog;
  Kachelraster mit drei Spalten auf Startseite und Simulationsreiter; Bedienblock im
  Zweispalten-Reiter. Der Baustein `Reiter` ist in Welle 5 **für genau diese Seite** entstanden
  und wird von ihr selbst nicht benutzt.

**Warum die Spalte fremd wirkt:**

1. Sie ist die **einzige dunkle Seitennavigation** der Anwendung; direkt daneben zeigt die
   Simulationsseite dieselbe Aufgabe als Reiterzeile.
2. Sie nimmt **208 px Breite über die volle Höhe** für vier Einträge — genau auf den
   Seiten mit den breitesten Tabellen; die Varianten- und die Energieträgertabelle rollen
   dadurch quer.
3. Farben und Sinnbilder haben **keinen Hausbezug** (keine Token, Zeichen ohne Bedeutung
   für den Anwender).
4. Die Einträge sagen **nichts über den Stand**: Warnungen der Kosten, bester Kapitalwert
   oder der letzte Bericht erscheinen erst nach dem Klick.
5. Kopfzeile und Navigation wiederholen den Seitennamen — eine Zeile Höhe ohne neue Auskunft.


## 2. Die drei Varianten

| | A — Reiterzeile | B — schlanke Seitenleiste | C — Kachelstart |
|---|---|---|---|
| Aufbau | zweite Reiterebene unter der Hauptleiste; je Reiter Titel und Statuszeile; Stamm, Umschalter und Hilfe rechts in derselben Zeile | helle Leiste 200 px (`--epos-flaeche`), Marke `--epos-marke` am aktiven Punkt, Status je Punkt, einklappbar auf 60 px | Start mit vier Kennzahlkacheln; Klick öffnet den Bereich in voller Breite, Rückweg „‹ Berichte & Kosten“ |
| Platz | volle Breite (+208 px), eine Zeile weniger | 200 px bzw. 60 px | volle Breite im Bereich |
| Konsistenz | gleich Simulationsseite und Dialogen | nahe Einstellungsdialog, sonst Einzelfall | Muster Startseite; vier Kacheln passen nicht ins Dreierraster (3 + 1) |
| iPad hochkant (< 900 px) | passt; Status in Kurzform (`▲ 2`, `26.09. 18:12`) | nur eingeklappt sinnvoll, Warnpunkt am Sinnbild | passt; Kacheln zweispaltig bzw. einspaltig |
| Tastatur, Sprachausgabe | wie jeder Reiter: ← → Pos1 Ende, `tablist` | ↑ ↓ wie heute; eingeklappt nur Sinnbild mit `aria-label` | Kacheln als Knöpfe; zwei Rückwege |
| Wechsel zwischen Bereichen | ein Klick | ein Klick | zwei Klicks (über den Start) |
| Neue Bausteine | keiner — `Reiterblatt` bekommt `Status`, `Reiter` ein Leistenende | Klappzustand, Sinnbilder, Stilfamilie neu | Kachelstart, Kennzahlhülle, Rückwegsteuerung |
| Aufwand | **klein bis mittel, 1–1,5 Tage** | mittel, 1,5–2 Tage | mittel bis groß, rund 3 Tage |

**A — Reiterzeile.** Löst alle fünf Punkte: Hausbaustein statt Sonderweg, Breite zurück,
Kopfzeile geht in der Reiterzeile auf, Stand je Reiter sichtbar. Zwei Reiterebenen
übereinander bleiben unterscheidbar (Hauptleiste gefüllt, zweite Ebene unterstrichen), das
zeigt die Simulationsseite schon heute. Offen: Der Wert „zuletzt erstellt“ existiert noch
nicht (siehe Etappe A3).

**B — schlanke Seitenleiste.** Die kleinste Umgewöhnung: Die Punkte bleiben, wo sie sind,
nur hell, in Hausfarben und mit Stand. Die Breite bleibt aber verbraucht, solange die Leiste
offen ist, und die Seite bleibt die einzige Hauptseite mit Seitennavigation. Das Einklappen
braucht einen gemerkten Zustand je Anwender.

**C — Kachelstart.** Stärkster Überblick vor dem ersten Klick. Dafür wird der häufigste Weg
(Kosten → Wirtschaftlichkeit → Bericht) um je einen Klick länger, und die Kennzahlen zwingen
die Hülle, Kosten und Wirtschaftlichkeit schon beim Öffnen zu rechnen — heute entstehen die
schweren Seiten erst beim ersten Aufruf. Der Menüsprung „Varianten und Bericht…“ muss den
Start überspringen.


## 3. Empfehlung: Variante A

- **Konsistenz:** dieselbe Reiterzeile wie Simulationsseite und Dialoge; kein neuer
  Baustein, keine eigene Farbe, die Stilfamilie `.epos-navigation*` mit ihren sechs
  Festfarben fällt weg.
- **Breite:** Energieträgertabelle, Vergleichsmatrix und Diagramme der Wirtschaftlichkeit
  bekommen 208 px zurück — auch auf dem iPad hochkant.
- **Stand ohne Klick:** Die Statuszeile trägt, was heute erst die Seite zeigt.
- **Geringes Risiko:** `Reiterblatt` zeichnet ein nicht gewähltes Blatt gar nicht — der
  verzögerte Aufbau der schweren Seiten bleibt ohne eigenen Code; Seitenschlüssel,
  `Seitenwunsch` und Menüsprünge bleiben unverändert.

B ist die Rückfallwahl, falls der Anwender die Punkte links behalten will; C ist als
eigener Überblick besser in der Übersichtsseite aufgehoben (Kennzahlkacheln oben) als in
der Navigation.


## 4. Etappen für A

| Etappe | Inhalt | Dateien | Aufwand |
|---|---|---|---|
| A1 | `BerichteKostenSeite` auf `Reiter`/`Reiterblatt` umstellen (Schlüssel bleiben, `@bind-Aktiv` statt `_seite`, `Seitenwunsch` setzt `Aktiv`); Kopfzeile vorerst über dem Reiter; `.epos-navigation*` bis auf Kopf und Rückweg entfernen; IDs der Knöpfe eindeutig halten (Präfix, weil die Startseite selbst einen `Reiter` trägt) | `EPOS.UI/Seiten/Berichte/BerichteKostenSeite.razor`, `EPOS.UI/wwwroot/epos-ui.css`, `EPOS.UI.Tests/Seiten/BerichteKostenSeiteTests.cs`, `AppWurzelTests.cs`, `HauptfensterTests.cs` | 0,5 Tag |
| A2 | Statuszeile je Reiter: `Reiterblatt.Status` (Text) und `Statusstufe` (Normal/Warnung, Warnung in `--epos-warn-text` mit ▲, `forced-colors` abgesichert), Kurzform unter 900 px; die Hülle liefert die Kurzstände (`Func<string,string>` je Seitenschlüssel: Zahl der Versionen, Warnungen der Kosten, bester Kapitalwert); Ressourcentexte beider Sprachen; Leistenende im `Reiter` (RenderFragment) für Stamm, Umschalter und Hilfe — die eigene Kopfzeile entfällt | `EPOS.UI/Bausteine/Reiter.razor`, `Reiterblatt.razor`, `epos-ui.css` (`StilblattTests`), `EPOS.UI.Daten/Bericht/BerichteKostenHuelle.cs`, `MyResource`, Bausteintests | 0,5–0,75 Tag |
| A3 | „zuletzt erstellt“ für den Bericht: Zeitpunkt beim erfolgreichen Erstellen je Projekt ablegen (Einstellung, kein Schemaschritt nötig, falls `Tab_Einstellungen` genügt) und in der Statuszeile zeigen; entfällt ohne Entscheid | `BerichtCtrl` (Kern), Hülle | 0,25 Tag |
| A4 | Prüfen: Windows-Anwendung und Rasterprobe-Wirt im Browser bei 1 280 und 820 px, Wiki-Seite „Berichte & Kosten“ nachziehen, Logbuch-Satz vorschlagen | `Projekte/Wiki/*.wiki` | 0,25 Tag |

Kein Eingriff in Rechenweg, Schema oder Referenzbasis.


## 5. Fragen an den Anwender

- **BN-Q1:** Variante A übernehmen? (Empfehlung: ja; sonst B.)
- **BN-Q2:** Statuszeile je Reiter gewünscht, und wenn ja, mit „zuletzt erstellt“ (Etappe A3)?
- **BN-Q3:** Stammname, Platzhalter-Umschalter und Hilfe rechts in die Reiterzeile (A2) oder
  als eigene Zeile darüber lassen (nur A1)?
