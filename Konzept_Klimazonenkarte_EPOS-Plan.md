# Klimazonen-Karte für die DIN-4710-Auswahl — Analyse und Vorgehensweise

Stand: 29.08.2026. Anlass ist der Anwenderwunsch aus dem Erdreich-Quellendialog
(`Form_QuelleErdreich`): Für die Klimazone könnte eine **Karte** angezeigt werden, auf der
die Zonen ersichtlich sind — evtl. mit **Auswahl direkt auf der Karte** statt über das
Dropdown.

Dieses Dokument ist reine Analyse und Planung — es wurde keine Zeile Code geändert.

---

## 1. Kurzfassung des Befunds

Der Wunsch ist gut umsetzbar, und die Datenseite ist bereits vollständig vorhanden: Die
Zone ist eine **projektweite Eigenschaft der Klimaregion**, alle Zahlenwerte je Zone
(Volllaststunden, Entzugsgrenzen) stehen im Code und taugen unmittelbar als
Karten-Beschriftung und Tooltip. Die Technik (WinForms-Control mit Polygonen, Hover und
Hit-Test) ist Standardkost und folgt hausüblichen Mustern.

Die **eigentliche Entscheidung ist das Kartenmaterial**: Die Zonenkarte der Norm
(DIN 4710 bzw. Bild A1 der VDI 4640 Blatt 2) ist als Abbildung urheberrechtlich
geschützt und darf nicht eingescannt oder eingebettet werden. Es braucht eine **eigene
Karte** — schematisch gezeichnet oder auf freien Verwaltungsgrenzen aufgebaut — plus eine
einmalig zu erstellende Zuordnung „Gegend → Zone" aus der Norm.

Wichtig zur Begriffshygiene: Die drei bestehenden „Karten"-Klassen im Code
(`ErzeugerKarte`, `SpeicherKarte`, `EinstiegsKarte`) sind Info-**Kacheln** (Cards) der
Simulationskonfiguration, keine Landkarten. Es gibt **keine** wiederverwendbare
Geografie-Infrastruktur — wohl aber deren bewährtes `OnPaint`-Muster.

---

## 2. Ist-Zustand

### 2.1 Der Dialogweg

| Baustein | Fundstelle | Rolle |
|---|---|---|
| ComboBox `_cbZone` | `Form_QuelleErdreich` | Index = Zone (0 = „nicht zugeordnet", 1…15); Anzeigetexte zur Laufzeit aus `VDI4640Pruefung` („n — x h/a", z. B. „8 — 2.000 h/a") |
| Vorbelegung | `KlimaregionCtrl.GetKlimazone(...)` | liest `Tab_Klimaregion.Klimazone_DIN4710` der Projekt-Klimaregion |
| Rückschreiben | `Form_Simulation_Config.Uebersicht.cs:573` → `KlimaregionCtrl.SetKlimazone(...)` | die im Erdreich-Dialog gewählte Zone landet an der **Klimaregion des Projekts** |
| Persistenz | `Tab_Klimaregion.Klimazone_DIN4710` (`LONG DEFAULT 0`, Migrationsschritt 2) | eine Zone je Klimaregion, projektweit — **nicht** je Anlage |

### 2.2 Verwender der Zone

- **`VDI4640Pruefung`** — Auslegungsprüfung nach VDI 4640 Blatt 2: Tabellen `A2_LEISTUNG`
  (15 Zonen × 4 Bodenarten, W/m² und kWh/(m²·a)) und `A2_VOLLLASTSTUNDEN` (h/a je Zone,
  `KLIMAZONEN = 15`). **Diese Tabellen sind die fertige Datengrundlage für Legende und
  Tooltips der Karte.**
- **`ErdreichAuswertung.KlimazoneDesProjekts(...)`** — holt die Zone über
  `Tab_Projekt.ID_Klimaregion` für die Frost-/Entzugsauswertung.

### 2.3 Vorhandener geografischer Anker

`KlimaregionModel` trägt je Klimaregion **Longitude/Latitude**. Die Wetterstationen des
Klimakatalogs lassen sich damit als Punkte auf jeder Deutschlandkarte verorten — nützlich
für eine spätere Ausbaustufe (Klimaregion-Wahl im Wizard über dieselbe Karte).

---

## 3. Kartenmaterial — die Kernentscheidung

1. **Norm-Abbildung übernehmen: scheidet aus.** Die Zonenkarte in DIN 4710 / VDI 4640
   Blatt 2 (Bild A1) ist geschützt; einscannen oder abfotografieren ist keine Option.
   Zulässig bleibt der textliche Verweis auf die Norm (wie heute im Klammerhinweis).
2. **Eigene schematische Karte (Empfehlung für Stufe 1).** Ein vereinfachter
   Deutschland-Umriss (gemeinfreie Basis, z. B. Natural Earth) mit **grob gezeichneten
   Zonenpolygonen** eigener Prägung und den Repräsentativstationen als Beschriftung. Die
   Zuordnung „Gegend → Zone" ist Faktenübernahme aus der Norm und wird einmalig als
   eigene Liste erstellt. Geringster Aufwand und geringstes Rechtsrisiko; die Genauigkeit
   ist „Übersichtskarte" — für den Zweck (die eigene Zone **finden**, nicht ein
   Grundstück vermessen) ausreichend, zumal die Combo für den exakten Wert bestehen
   bleibt.
3. **Präzise Variante auf Verwaltungsgrenzen (Option für Stufe 2).** Kreisgrenzen des
   BKG (VG2500/VG250, Datenlizenz Deutschland dl-de/by-2-0, **Quellenvermerk
   „© GeoBasis-DE / BKG" Pflicht**) plus Zuordnungstabelle Kreis → Zone. Deutlich mehr
   Datenpflege, dafür belastbare Grenzverläufe.
4. **Nicht vorgesehen:** Online-Kartendienste oder WebView — die Anwendung muss offline
   funktionieren, und eine Browser-Komponente wäre eine neue schwere Abhängigkeit.

Voraussetzung für 2. wie 3.: Die Norm (DIN 4710 oder VDI 4640 Blatt 2 mit Bild A1) muss
für die einmalige Zuordnungserstellung **im Haus vorliegen** — die Zahlenwerte der
Tabelle A2 stehen schon im Code, die geografische Abgrenzung der Zonen noch nicht.

---

## 4. UI-Konzept in zwei Stufen

### Stufe 1 — Karte ansehen

Kleiner Kartenknopf neben der Klimazonen-Zeile in `Form_QuelleErdreich` öffnet einen
modalen Dialog `Form_Klimazonenkarte`: Karte mit Zonenflächen und -nummern, die aktuell
gewählte Zone hervorgehoben, darunter eine Legende je Zone (Nummer, h/a aus
`A2_VOLLLASTSTUNDEN`, Repräsentativstation), als Tooltip je Fläche zusätzlich die
A2-Grenzwerte der gewählten Bodenart. Nur ansehen und schließen — die Auswahl bleibt in
der Combo. Kein Eingriff in Auswahllogik oder Persistenz; das Risiko ist praktisch null.

Platzhinweis: `_cbZone` endet bei x = 400, der Klammerhinweis beginnt bei 412 — der
Knopf kommt ans Zeilenende hinter den Hinweis oder der Hinweis rückt; Maße nach dem
Echttext-Messrezept (TextRenderer, beide Sprachen) bestimmen.

### Stufe 2 — Auswahl auf der Karte

Derselbe Dialog bekommt Hover-Hervorhebung und Klickauswahl; OK übernimmt die Zone in
`_cbZone` (und damit über den bestehenden Weg beim Dialog-OK in die Klimaregion). Die
**Combo bleibt bestehen** — für Tastaturbedienung, für „0 = nicht zugeordnet" und als
präzise Anzeige des gewählten Werts.

### Technische Leitplanken (beide Stufen)

- Neues Control `KlimazonenKarte`: `OnPaint` + `GraphicsPath`-Polygone aus einer
  eingebetteten Ressource (einmalig konvertierte Koordinatenliste), Hit-Test über
  `GraphicsPath.IsVisible`, Hover-Tooltip. DpiUnaware wie die ganze Anwendung — feste
  Pixelmaße.
- `Form_Klimazonenkarte` designer-konform nach dem Migrationsrezept
  (`KONTEXT_Designer_Migration_Dialoge.md`): Echttexte im Designer, `TexteSetzen()` aus
  `MyResource` (DE/EN), Fußknopfnorm 110×30, `FensterEinpassung.Einhaengen(this)`.
- Drei-Schichten-Regel: Steuer- und Persistenzwert bleibt die **Zonennummer (int)**;
  alle sichtbaren Texte sind Laufzeit-Ressourcen.
- Die heutige Semantik bleibt unangetastet: eine Zone je Klimaregion, projektweit —
  die Karte ist nur ein zweiter Eingabeweg für denselben Wert.

### Ausbaustufe (vorgemerkt, nicht Teil dieses Vorhabens)

Dieselbe Karte im Wizard bzw. der Klimaregion-Pflege: Stationen des Klimakatalogs als
Punkte (Lat/Lon aus `KlimaregionModel`), Zonenflächen als Orientierung darunter.

---

## 5. Aufwandsschätzung

| Paket | Inhalt | Schätzung |
|---|---|---|
| K1 | Zonenpolygone + Zuordnungsliste erstellen (schematische Variante) | ~1 PT (Hauptaufwand, braucht die Norm) |
| K2 | Control `KlimazonenKarte` + Dialog + Einbau + Lokalisierung | ~1 PT |
| K3 | Stufe 2: Hover/Klickauswahl/Übernahme | ~0,5 PT |
| K4 | Alternative BKG-Grenzen statt Schema (ersetzt K1) | +1–2 PT |

---

## 6. Entscheidungspunkte für den Anwender

1. **Stufe 1** (nur ansehen) oder gleich **Stufe 2** (Klickauswahl)?
2. **Kartenmaterial:** schematische eigene Karte (schnell, grob) oder BKG-Kreisgrenzen
   (präzise, Quellenvermerk, mehr Pflege)?
3. Liegt **DIN 4710 bzw. VDI 4640 Blatt 2 (Bild A1)** für die einmalige
   Zonen-Zuordnung vor?
4. Soll die Karte perspektivisch auch im **Wizard** (Klimaregion-Wahl) erscheinen —
   dann würde das Control von Anfang an dafür geschnitten?
