# Etappe D4 — Ansicht „Schema"

Stand 16.08.2026 · Umsetzung zu [`Konzept_KonfigUI_Hydraulik.md`](Konzept_KonfigUI_Hydraulik.md),
Etappe **D4**: die vier Restpunkte aus
[`D5b_DialogFreischaltung_Protokoll.md`](D5b_DialogFreischaltung_Protokoll.md), Abschnitt 8.

D2/D3 haben die Konfigurationsseite auf Karten umgestellt, D5a den Rechenkern von
Kombispeicher und Kessel-Kaskade gebaut, D5b die Kaskade bedienbar gemacht. Was fehlte, war
die zweite Sicht: **„Liste" zum Arbeiten, „Schema" zum Verstehen** (Konzept Abschnitt 3).
D4 liefert sie — und macht zugleich die Kaskade im Ergebnis sichtbar, statt sie nur am
gesunkenen Brennstoffverbrauch erschließen zu lassen.

**Codestand:** Haupt-Checkout `C:\Waermeplan\WP_Plan`, HEAD **ca57ccb**, Working Tree = D4.
**Nichts committet.** Build: 0 Fehler, exakt die sechs Bestandswarnungen
(`WErzeugerModel` CS0108, `StromverbraucherStammCtrl` CS0108, `KlimaregionStammCtrl`
CS0109 ×2, `MDIMainForm` CS4014 + CS1998).

---

## 1. Was umgesetzt ist

| # | Aufgabe | Ergebnis |
|---|---|---|
| 1 | Ansicht „Schema" mit Umschalter | GDI+-Fläche `SchemaAnsicht` über dem Kartenbereich; vier Spalten Quelle → Erzeuger → Speicher → Abnehmer, Farbsprache des Entwurfs, Kombi-Puffer als EIN Knoten mit beiden Badges, Kaskade blau gestrichelt |
| 2 | Auswahl-Synchronisation | ein gemeinsamer Auswahlschlüssel für beide Ansichten; Klick hier hebt dort hervor, Doppelklick öffnet denselben Editor, Tooltip = Kurzinfo der Karte |
| 3 | Kaskadenband und Temperatur-Warnregel im Schema | Pillen-Band unter dem Schema aus der automatisch abgeleiteten Kette; amber Band am Erzeugerkasten mit demselben Text wie der Kartenchip |
| 4 | Ergebnisspalte Quellwärme des Kessels | Migrationsschritt **10** (`Tab_ErgebnisHeizkessel.Quellwaerme`), Schreiben im Runner, eigene Zeile auf der Heizkessel-Ergebnisseite |
| 5 | Vollzyklen des Kombispeichers | in der Ergebnisansicht markiert und erklärt — **ohne** Schemaänderung und **ohne** Änderung am gespeicherten Wert (Abschnitt 6) |

### 1a. Eine Ableitung, keine dritte

Die Verschaltung „welche Anlage lädt welchen Puffer, welche Anlage bezieht aus welchem
Puffer" stand bis D5b als **lokale Rechnung IN** `WaermesenkeClass.RingMeldung`. Genau das
hält der D5b-Restpunkt 2 fest: Sie „lässt sich für die Kettenbildung wiederverwenden, statt
sie ein drittes Mal zu schreiben."

Sie ist deshalb nach `Hydraulikbild` gewandert (neue Datei,
`Allgemein/Simulation/Hydraulikbild.cs`). `RingMeldung` ruft sie jetzt auf und rechnet
nichts mehr selbst; das Schema benutzt dieselbe Abbildung. **Verschoben, nicht geändert:**
Abfrage, Zeilenordnung, die Bedingung „lädt" (Puffer-ID auf einem Senkenfeld UND ein
Puffer-Ziel dazu), die Einschränkung auf Wärmepumpe und Heizkessel (Befund E-K2-2) und die
Ebenen-Relaxation sind Zeile für Zeile die aus D5b. Die Abfrage holt nur **zusätzliche
Spalten** — dieselben Zeilen in derselben Ordnung.

**Zwei Auflösungen der Quellidentität, mit Absicht:**

* `QuelleJeAnlage` — ausschließlich der Fremdschlüssel `WQ_ID_Puffer`. Das ist die
  ENGINE-Wahrheit (`QuellbezuegeAufbauen` verlangt ihn > 0). Ring- und Ebenenrechnung
  laufen darüber, damit der Dialog exakt das prüft, woran die Engine scheitern würde.
* `QuellpufferAnzeige` — Fremdschlüssel, sonst der Alt-Bezeichner gegen die
  Projekt-Pufferliste (kleinste ID, wie `WaermesenkeClass.QuellPufferDerAnlage`). Das ist
  die ANZEIGE-Wahrheit der Erzeugerkarte; Liste und Schema müssen dasselbe zeigen.
  `NurBezeichner` macht den Unterschied sichtbar.

Aus demselben Grund ist auch der **Quellentext** umgezogen: `WaermequelleAnzeige` und
`ErdreichAnzeige` waren private Methoden der Konfigurationsseite und stehen jetzt als
`WaermequelleClass.QuelleAnzeige`/`ErdreichAnzeige` an EINER Stelle. Zwei Fassungen
desselben Textes wären zwei Wahrheiten über die Quelle.

### 1b. Drei Schichten statt einer Zeichnung

* **`SchemaModell`** (`Allgemein/Simulation/`) — WAS gezeichnet wird: Knoten, Kanten,
  Kaskadenketten. Ohne `System.Windows.Forms` und ohne `System.Drawing`. Damit ist die
  Aussage des Schemas **headless prüfbar** (Knoten- und Kantenliste gegen die Datenbank),
  und die Verifikation prüft das Modell statt Pixel.
* **`SchemaAnsicht`** (`Views/Simulation/`) — WIE gezeichnet wird: Spaltenlayout,
  Bézier-Kanten mit Pfeilspitze, Prioritätskreise, Pillen-Band, Legende, Trefferflächen.
  Freies GDI+ nach dem Muster von `SpeicherKarte`, doppelgepuffert
  (`OptimizedDoubleBuffer`); Farben und Maße aus `KartenStil`, damit Liste und Schema nicht
  zwei Farbtabellen führen.
* **`Form_Simulation_Config.Schema.cs`** — die Verdrahtung: Umschalter, Auffrischung,
  Auswahl, Zuordnung Schema-Element → Editor.

### 1c. Anordnung ohne Kantenoptimierung

Erst die Erzeugerspalte von oben nach unten (sie gibt die Kaskadenreihenfolge vor), dann die
Quellen auf die Höhe ihres Erzeugers, dann Speicher und Abnehmer auf die MITTLERE Höhe ihrer
Zuflüsse; Überschneidungen werden anschließend nach unten aufgelöst. Damit laufen die
Leitungen weitgehend waagerecht, ohne dass ein Kantenverfahren nötig wäre.

**Rückwärtskanten** (Kaskade: Speicher → Erzeuger, also von rechts nach links) laufen
bewusst UNTER den Kästen herum — sonst zöge die einzige Linie, die im Schema rückwärts
zeigt, quer durch die Erzeugerspalte.

**Nur beteiligte Speicher** bekommen einen Kasten: geladen, als Quelle genutzt oder
Zweitsenke. Der Filter ist nicht kosmetisch — Projekt 1023 der Arbeitskopie führt **79
Puffer-Zeilen**, von denen genau EINE an der Hydraulik teilnimmt. 79 Kästen wären unlesbar
und behaupteten 78 Speicher, die kein Erzeuger bedient.

### 1d. Invariante S-1 im Bild

Konzept Abschnitt 5: „In Schema und Kaskadenkette steht zwischen zwei Speicher-Knoten immer
ein Erzeuger-Knoten; ein direkter Pfeil Speicher → Speicher darf nie gezeichnet werden."

Strukturell ist das garantiert — eine Kante entsteht nur aus einem Quell- oder Senkenbezug,
und die gibt es ausschließlich an `Tab_Energieanlagen`. `SchemaModell.Pruefen()` prüft es
zusätzlich nach (Kanten UND Kettenglieder), damit eine künftige Erweiterung nicht still
dagegen verstößt; das Prüfprogramm ruft die Methode in jedem Szenario auf.

### 1e. Kaskadenkette

Eine Kette beginnt bei einem Erzeuger OHNE Quellpuffer, der einen Speicher lädt, aus dem ein
anderer Erzeuger seine Quellwärme bezieht; sie folgt dem Weg Erzeuger → Speicher → Erzeuger
→ … bis zum letzten Speicher und endet beim Abnehmer. Verzweigt ein Speicher auf mehrere
Nutzer, bekommt jeder Zweig eine eigene Kette (höchstens sechs, sonst wäre es kein Band
mehr, sondern eine Liste). Ein Ringschutz begrenzt jede Kette auf einen Besuch je Speicher —
die Engine bricht bei einem Ring ab und der Dialog verhindert ihn seit D5b, hier geht es
allein darum, dass die ANZEIGE eines Altbestands nicht endlos läuft.

Gemessen am Booster-Beispiel des Konzepts (Abschnitt 5) liefert die Ableitung genau die dort
beschriebene Kette — siehe Abschnitt 5, Block T7.

**Abweichung vom Mockup, bewusst:** Der Abzweig „Puffer 1 → Heizkreis" steht im Band NICHT
als eigenes Kettenglied. Er ist im Schema als grüne Versorgungskante gezeichnet; ihn
zusätzlich als Kettenzweig zu führen, machte aus einer Kette einen Baum und aus dem Band
eine zweite Zeichnung.

---

## 2. Änderungen je Datei

Zeilennummern nach dem Endstand dieser Etappe.

### Neue Dateien

| Datei | Zeile | Inhalt |
|---|---|---|
| `Allgemein/Simulation/Hydraulikbild.cs` | 49 | `AnlagenEintrag` — eine Wärmeerzeuger-Anlage mit Quell- und Senkenfeldern |
| " | 123 | `Lesen` — EINE Abfrage je Projekt (Spalten und Sortierung wie `AnlagenImProjekt`) |
| " | 230 | `QuellpufferAnzeige` — Anzeige-Auflösung ohne zusätzliche Abfrage |
| " | 252 | `NurBezeichner` — nicht aufgelöster Altbestand (E0) |
| " | 277 | `Ebenen` — die Relaxation aus D5b, unverändert |
| " | 324 | `RingBeteiligte` — Aufzählung der Ringteilnehmer, unverändert |
| `Allgemein/Simulation/SchemaModell.cs` | 183 | `Aufbauen` — Modell aus `Hydraulikbild`, Pufferliste und Kaskadenbelegung |
| " | 260 | `SpeicherKnotenAnlegen` (Filter „beteiligt") und die beiden Abnehmerknoten |
| " | 390 | `ErzeugerKnotenAnlegen` — Zeilen, Kaskadenmarke, Temperatur-Warnregel |
| " | 465 | `Quelltext` — Quellkasten je Erzeugerart |
| " | 497 | `KantenAnlegen` — Quelle, Kaskade, Ladung (mit Priorität), Versorgung |
| " | 627 | `KettenAbleiten` / `KetteFortsetzen` — das Kaskadenband |
| " | 789 | `Pruefen` — Selbstprüfung, u. a. Invariante S-1 |
| `Views/Simulation/SchemaAnsicht.cs` | 137 | `Auswahl` (setzt ohne Rückmeldung — sonst schaukeln sich Liste und Schema auf) |
| " | 151/162 | `FlaecheVon` / `Treffer` — Trefferflächen, auch ohne Maus prüfbar |
| " | 203 | `Neuordnen` — das Spaltenlayout (Abschnitt 1c) |
| " | 321 | `BandAnordnen` — Pillen des Kaskadenbands |
| " | 477 | `KanteZeichnen` — Bézier, Pfeilspitze, Rückwärtsführung, Prioritätskreis |
| " | 540 | `KnotenZeichnen` — Kästen, Badges, amber Warnband |
| " | 652/711 | `BandZeichnen` / `LegendeZeichnen` |
| `Views/Simulation/Form_Simulation_Config.Schema.cs` | 62 | `SchemaAufbauen` — Umschalter und Schemafläche |
| " | 121 | `UmschalterPlatzieren` — oben rechts, wie im Entwurf |
| " | 157 | `AnsichtAnwenden` — Umschalten, Auswahl überlebt |
| " | 202 | `AktualisiereSchema` — rechnet nur bei sichtbarem Schema |
| " | 222 | `SchemaHinweiseSetzen` — Tooltip = Kurzinfo der Karte |
| " | 269/281 | `SchemaAuswahl` / `SchemaBearbeiten` — Klick und Doppelklick |
| " | 386 | `AuswahlInKartenZeigen` — Hervorhebung in beiden Kartenspalten |

### Geänderte Dateien

| Datei | Stelle | Inhalt |
|---|---|---|
| `Allgemein/Simulation/WaermesenkeClass.cs` | 905 | `RingMeldung` ruft `Hydraulikbild` — 105 Zeilen lokale Rechnung entfallen |
| `Allgemein/Simulation/WaermequelleClass.cs` | 181/222 | `QuelleAnzeige` / `ErdreichAnzeige` (aus der Konfigurationsseite hierher) |
| `Views/Simulation/Form_Simulation_Config.Uebersicht.cs` | 397–406 | `WaermequelleAnzeige` reicht nur noch durch |
| `Views/Simulation/Form_Simulation_Config.Karten.cs` | 145 | `SchemaAufbauen()` im Layoutaufbau |
| " | 542 | `AktualisiereSchema()` + `AuswahlInKartenZeigen()` in der zentralen Auffrischung |
| " | 836 | Klick auf eine Erzeugerkarte meldet die Auswahl |
| " | 1170 | Klick auf eine Speicherkarte meldet die Auswahl |
| `Views/Simulation/ErzeugerKarte.cs` | 290 | `Hervorgehoben` (kräftigerer Rahmen in der Quellfarbe) |
| " | 328/424/433/513 | Ereignis `Ausgewaehlt`, Klick-Durchgriff auf Karte und Chips |
| " | 651 | `OnPaint` zeichnet den Hervorhebungsrahmen |
| `Views/Simulation/SpeicherKarte.cs` | 160 | `Hervorgehoben`, unabhängig von `Aufgeklappt` |
| " | 495 | `OnPaint` wie oben |
| `Allgemein/Update/SchemaKatalog.cs` | 55 | `TAB_ERGEBNISHEIZKESSEL` |
| " | 242/269 | `SPALTE_KESSEL_QUELLWAERME`, `Schritt10_KesselQuellwaerme` |
| " | 297 | Begründung, warum Schritt 10 NICHT in `Alle` steht |
| `Allgemein/Update/SchemaMigration.cs` | 49 | `ZIEL_VERSION` 9 → **10** |
| " | 103/253/646 | `SCHRITT_10_KESSEL_QUELLWAERME`, Registereintrag, Schrittmethode |
| `Model/ErgebnisModel.cs` | 142 | `ErgebnisHeizkesselModel.Quellwaerme` [MWh/a] |
| `Allgemein/Simulation/SimulationRunner.cs` | 568 | `h.Quellwaerme = spk.Quellwaerme_gesamt / 1000.0` |
| `Controller/ErgebnisCtrl.cs` | 318/338 | INSERT um die neue Spalte erweitert (letzte Position) |
| " | 665 | Leseseite (`D()` liefert 0 bei fehlender Spalte oder NULL) |
| " | 89/889 | `StelleKesselSpaltenSicher` — tolerante Rückfallebene wie beim BHKW |
| `Views/Simulation/Form_Simulation_Detail.cs` | 470/502 | `InitKesselQuellwaerme` — Ergebniszeile unter „Gasspitze" |
| " | 577 | `NachbarZeile` — Beschriftung und Einheit werden GEMESSEN, nicht über Namen gesucht |
| " | 2725 | Wert der Zeile aus `SimulationSPK.Quellwaerme_gesamt` |
| " | 604 | Präsenzregel: Zeile nur mit Kessel im Ergebnis |
| " | 1021/1085 | Vollzyklen des Kombispeichers: Marke `*` und Zeilenhinweis |
| `Referenzlauf/Vergleich.cs` | 41–75 | `--ohne`: benannte Schlüssel vom Vergleich ausnehmen (Abschnitt 4) |
| `Referenzlauf/Program.cs` | 55–62 | Argument `--ohne` durchgereicht, Hilfetext |
| `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` | — | **23 neue Schlüssel** (Nachtrag in [`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md)); Bestand jetzt **640** `<data>`-Einträge je Datei |

---

## 3. Abweichung von der Aufgabenstellung: Schritt **10** statt 11

Die Aufgabe nennt „aktueller Schemastand 10 … führe Schritt 11 ein". An der Datenbank und
im Code steht es anders, und der Code entscheidet:

* `SchemaMigration.ZIEL_VERSION` stand vor dieser Etappe auf **9** (letzter Schritt: Regel
  R7 der Etappe E0). D5b hält das ausdrücklich fest: „D5b legt ebenfalls keine neue Spalte
  an (Schemastand bleibt 9)".
* Die Wegwerf-Kopie der produktiven `Kenndaten.accdb` meldete vor dem Lauf
  `SchemaVersion = 9`.

Die neue Spalte ist deshalb **Schritt 10**, und der Zielstand ist **10**. Ein Schritt 11 auf
einem Stand 9 wäre nie ausgeführt worden: Das Register arbeitet alle Schritte mit Nummer
> Marker ab und hebt den Marker je Schritt um genau eins.

---

## 4. Erklärte Abweichung: die CSV-Spaltenliste wächst um EINEN Schlüssel

**Rücksprache-Notiz — das ist die einzige beabsichtigte Abweichung dieser Etappe.**

`Referenzlauf/Ergebnisexport.cs` schreibt `aggregate.csv` aus
`SELECT * FROM Tab_Ergebnis*`. Die neue Spalte `Tab_ErgebnisHeizkessel.Quellwaerme`
erscheint dort deshalb zwangsläufig als zusätzliche Zeile `Heizkessel.Quellwaerme;0` — in
genau den Projekten, die einen Heizkessel rechnen (1017, 1018, 1023, 1024). Gegen die
eingefrorene Basis `2026-08-15_B3` meldet der Vergleich das als „Eintrag nur im
Vergleichslauf".

Das ist **kein stiller Bruch, sondern eine erklärte, kontrollierte Erweiterung**:

1. Der Wert ist in allen vier Fällen **0** — ohne Quellbezug rechnet der Kern die Größe
   nicht, und keines der acht Referenzprojekte führt eine Kessel-Kaskade.
2. **Alle Altzeilen sind Wert für Wert und in derselben Reihenfolge identisch** (Abschnitt 5,
   Zeilendiff der vier Dateien).
3. 186 der 190 CSV-Dateien sind **byte-/MD5-gleich**; die vier Ausnahmen sind genau die vier
   `aggregate.csv`, und ihr Unterschied ist genau die eine hinzugekommene Zeile.

Damit der Nachweis „alle Altwerte unverändert" auch als PASS zu führen ist, hat der
Vergleichsmodus die Option `--ohne <schluessel,…>` bekommen. Sie ist ein Werkzeug für einen
**benannten** Unterschied, kein Weg, Abweichungen wegzuschalten: Sie wirkt nur auf
ausdrücklich aufgezählte Schlüssel, und die Ausgabe nennt sie in einer eigenen Kopfzeile
(`AUSGENOMMEN (--ohne): Heizkessel.Quellwaerme`).

**Empfehlung für die Abnahme:** Nach der Freigabe eine neue Basis einfrieren (`2026-08-16_B4`);
danach ist der Vergleich wieder ohne Ausschluss zu führen.

---

## 5. Verifikation

### 5.1 Regression, Flag AUS

Alle Läufe auf **einer** vollständig migrierten Kopie der produktiven `Kenndaten.accdb`
(`Referenzlauf.exe migration`, Quelle **nur gelesen**, `Kenndaten.laccdb` vorher geprüft und
nicht vorhanden). Projektmenge 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024.

| Prüfung | Vergleich | Ergebnis |
|---|---|---|
| Toleranzvergleich **ohne** Ausschluss | Basis `2026-08-15_B3` gegen D4 | 4× PASS, 4× FAIL — **je genau eine** Abweichung, immer `aggregate.csv [Heizkessel.Quellwaerme]: Eintrag nur im Vergleichslauf (neu=0)` |
| Toleranzvergleich **mit** `--ohne Heizkessel.Quellwaerme` | dito | **8/8 PASS, 2 094 451 Werte** |
| Rekursiver MD5-Vergleich | dito | **186/190 Dateien byte-gleich**; die vier Ausnahmen sind die `aggregate.csv` von 1017, 1018, 1023, 1024 |
| Zeilendiff dieser vier Dateien | dito | genau **eine** hinzugekommene Zeile `Heizkessel.Quellwaerme;0`; alle übrigen Zeilen **wert- UND reihenfolgegleich** |
| Dateibestand | dito | 190 CSV in beiden Läufen, keine fehlt, keine kommt hinzu |

Die Wertezahl 2 094 451 statt der 2 094 447 aus D5b ist der Zähler `max(Ref, Neu)` je Datei:
vier Dateien führen einen Schlüssel mehr.

### 5.2 Migration Schritt 10

Eigene Wegwerf-Kopie der produktiven Datenbank (Stand **9**), zweimal migriert.

| Prüfung | Ergebnis |
|---|---|
| Stand vorher | `SchemaVersion = 9` |
| Lauf 1 | „Schritt 10 Ergebnisspalte Quellwaerme in Tab_ErgebnisHeizkessel (Etappe D4): **OK**", Stand nachher **10** |
| Lauf 2 (Doppelstart) | „Schritt 10 …: **bereits erledigt**", Stand bleibt **10** — idempotent |
| Spalte | `Quellwaerme` an Ordinalposition **21** (angehängt), Typ DOUBLE, NULL-fähig |
| Bestandswerte | drei vorhandene Ergebniszeilen: **alle 20 Altspalten wert- und reihenfolgegleich**, `Quellwaerme` bleibt NULL (kein Backfill) |
| Leseseite | `D(rh, …)` liefert für NULL und für die fehlende Spalte 0 — Altergebnisse zeigen 0,00 |

Nach den acht Referenzläufen trägt die Spalte in den vier neu geschriebenen Kesselzeilen
den Wert 0 und in den beiden Altzeilen weiterhin NULL — genau die beabsichtigte
Unterscheidung „nicht erhoben" gegen „erhoben und null".

### 5.3 Prüfprogramm (Schema-Modell, Trefferflächen, Umschalter)

Wegwerf-Harness nach dem bewährten Muster (net8-x86-Konsole, Projektverweis auf die
Anwendung, `Properties.Settings.DBPath` per Reflection auf eine Wegwerf-Kopie umgebogen und
hart nachgeprüft; internes `SchemaAnsicht` und die privaten Mitglieder der
Konfigurationsseite über Reflection). **107 Proben, 0 Fehler.**

| Block | Proben | Kernaussagen |
|---|---|---|
| **T1** Katalog | 23 | alle 23 neuen Schlüssel in de **und** en vorhanden, ohne `U+FFFD` und ohne `??`; verschieden — bis auf `MWh` und `Schema`/`Schematic`… (die Einheit ist bewusst gleich) |
| **T2** Projekt **1023** (WP + WP + Kessel, ein Heizungspuffer) | 14 | 3 Erzeugerknoten, **1** Speicherknoten von 79 Puffer-Zeilen, 3 Quellknoten; Ladekanten 11203/11204 → 1018023 mit **Priorität 1 und 2**; Versorgung Puffer → Heizkreis; Kessel deckt direkt; **Temperatur-Warnung an 11203** (Vorlauf 45 °C < Puffer 65 °C), **keine** an 11204 (65 = 65); keine Kette; Selbstprüfung 0 |
| **T3** Projekt **1011** (Solarthermie + WP, PV/Batterie) | 9 | 5 Erzeugerknoten (3 WP + 2 Solarthermie), **0** Speicherknoten (der Projekt-Puffer nimmt an nichts teil); die beiden Heizkessel des Projekts sind **nicht** gezeichnet (nicht in der Kaskade); Quelle Solarthermie = „Solarstrahlung", Luft-Wasser-WP = „Außenluft"; Rang 1 = Solarthermie, Rang 2 = Wärmepumpe; keine Kette; Selbstprüfung 0 |
| **T4** Projekt **1021** (Quellpuffer ohne Lader) | 8 | Quellpuffer 1018014 wird zum Speicherknoten, Kaskadenkante Puffer → WP 10361, **kein** Quellkasten für 10361, Quellkasten für 10360 vorhanden, Knoten als Kaskade markiert; **keine Kette** — der Puffer hat keinen Lader (der „freistehende Quellpuffer" aus D5a/K3-5, hier korrekt als Nicht-Kette abgebildet); Selbstprüfung 0 |
| **T5** Trefferflächen (1023) | 22 | jeder der 9 Knoten hat ein Rechteck > 40×20 px, **keine Überlappung**; `Treffer(Mitte)` liefert je Knoten genau seinen Schlüssel; weit außerhalb liefert „"; `Auswahl` nimmt einen bekannten Schlüssel an und **verwirft** einen unbekannten |
| **T6** Umschalter und Auswahl (1023, echtes Formular) | 16 | Start = Liste; Klick auf eine Erzeugerkarte setzt `ERZEUGER_11203` und hebt **genau eine** Karte hervor; Umschalten auf Schema baut das Modell und **erhält die Auswahl**; der Tooltip trägt die **Kartenchips**; Klick im Schema setzt `SPEICHER_1018023`; Rückschalten erhält die Auswahl und hebt **genau eine** Speicherkarte hervor; die Schalter tragen die Steuerwerte `LISTE`/`SCHEMA` und lokalisierte Beschriftungen |
| **T7** Booster-Konstellation (präparierte Kopie) | 15 | 2 Speicherknoten; Kaskadenkante Heizungspuffer → Booster; Booster lädt den Warmwasserpuffer; Warmwasserpuffer versorgt Warmwasser; Heizungspuffer versorgt weiter den Heizkreis; kein Quellkasten mehr für den Booster; **genau eine Kette**, Verlauf exakt `QUELLE_11203 → ERZEUGER_11203 → SPEICHER_1018023 → ERZEUGER_11204 → SPEICHER_1018024 → ABNEHMER_WARMWASSER`; Invariante S-1 in der Kette; Ebenenrechnung zyklenfrei mit Ebene 0 (WP 1) und **1** (Booster); `RingMeldung` schweigt — und meldet in der Gegenprobe (WP 1 zöge aus dem Warmwasserpuffer) den **Ring** |

Die Kette als Klartext, wie sie im Band steht:

```
Außenluft → CS6800iAW MB + AW 10 OR-T → Vitocell 140-E 600 Ltr
          → CS7800iLW 12 → allSTOR exclusiv VPS 800/3-7 → Warmwasser
```

Das ist Glied für Glied die Booster-Kette aus Konzept Abschnitt 5.

**T7 ist zugleich die Regressionsprobe der verschobenen Ableitung:** `RingMeldung` liefert
in derselben Konstellation weiterhin „kein Ring" und in der Gegenprobe eine Ringmeldung —
die D5b-Prüfungen T4/T5 bleiben also gültig, obwohl ihre Rechnung jetzt in
`Hydraulikbild` steht.

### 5.4 Build, Kodierung, Zeilenenden

| Prüfung | Ergebnis |
|---|---|
| Build (Lösung, Debug/x86, eigener `OutDir`) | **0 Fehler, exakt 6 Warnungen** — die sechs Bestandswarnungen |
| Kodierung | alle 17 geänderten **und** die 4 neuen Code-/Ressourcendateien: **UTF-8 mit BOM**; die Markdown-Dateien ohne BOM (jeweilige Bestandskonvention) |
| Zeilenenden | **durchgehend CRLF** (CR-Zahl = LF-Zahl in jeder Datei); `git diff` meldet keine Zeilenendenwarnung |
| `U+FFFD` | 0 in jeder geänderten Datei |
| `git diff --check` | nur `Resource.Designer.cs` mit den 23 Trennzeilen aus 8 Leerzeichen — das ist die Eigenkonvention dieser generierten Datei (618 solche Zeilen im Bestand) |
| `bin\` | **nicht beschrieben** — jeder Bau mit eigenem `OutDir` |

`git status` zeigt genau die beabsichtigten Dateien:

```
 M Referenzlauf/Program.cs
 M Referenzlauf/Vergleich.cs
 M WindowsFormsApplication1/Allgemein/Simulation/SimulationRunner.cs
 M WindowsFormsApplication1/Allgemein/Simulation/WaermequelleClass.cs
 M WindowsFormsApplication1/Allgemein/Simulation/WaermesenkeClass.cs
 M WindowsFormsApplication1/Allgemein/Simulation/Lokalisierung_Katalog.md   (Nachtrag D4)
 M WindowsFormsApplication1/Allgemein/Simulation/D5b_DialogFreischaltung_Protokoll.md  (Verweis in 8)
 M WindowsFormsApplication1/Allgemein/Update/SchemaKatalog.cs
 M WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
 M WindowsFormsApplication1/Controller/ErgebnisCtrl.cs
 M WindowsFormsApplication1/Model/ErgebnisModel.cs
 M WindowsFormsApplication1/MyResource/Resource.Designer.cs · Resource.resx · Resource.en-US.resx
 M WindowsFormsApplication1/Views/Simulation/ErzeugerKarte.cs
 M WindowsFormsApplication1/Views/Simulation/Form_Simulation_Config.Karten.cs
 M WindowsFormsApplication1/Views/Simulation/Form_Simulation_Config.Uebersicht.cs
 M WindowsFormsApplication1/Views/Simulation/Form_Simulation_Detail.cs
 M WindowsFormsApplication1/Views/Simulation/SpeicherKarte.cs
?? WindowsFormsApplication1/Allgemein/Simulation/Hydraulikbild.cs
?? WindowsFormsApplication1/Allgemein/Simulation/SchemaModell.cs
?? WindowsFormsApplication1/Allgemein/Simulation/D4_SchemaAnsicht_Protokoll.md
?? WindowsFormsApplication1/Views/Simulation/Form_Simulation_Config.Schema.cs
?? WindowsFormsApplication1/Views/Simulation/SchemaAnsicht.cs
```

---

## 6. Vollzyklen des Kombispeichers (D5b-Restpunkt 4)

**Was NICHT gemacht wurde und warum.** Der gespeicherte Wert in
`Tab_ErgebnisPufferspeicher.Vollzyklen` bleibt Bit für Bit der bisherige. Eine andere Formel
wäre eine **Ergebnisänderung** — sie schlüge in `aggregate.csv` durch, bräuchte einen
eigenen Referenznachweis und gehört damit in eine Etappe mit eigenem Beleg, nicht in eine
Anzeigeaufgabe. Die Aufgabenstellung schließt das ausdrücklich ein („nur wenn ohne
Schema-Änderung an Ergebnistabellen darstellbar").

**Was gemacht wurde.** Die Kennzahl wird in der Ergebnisansicht **markiert und erklärt**:
Bei einem Kombispeicher steht hinter dem Wert ein `*`, und die Zeile trägt einen
Mouseover-Hinweis (`PSP_VOLLZYKLEN_KOMBI_TIP`): Heizung und Warmwasser werden aus EINEM
Wärmevorrat gedeckt, die Zahl ist der **Jahresdurchsatz bezogen auf die Kapazität** und
nicht ein Maß für die Alterung des Speichers. Damit ist der Wert aus dem D5b-Szenario
(6627 an einem 13,9-kWh-Puffer) dort, wo er gelesen wird, eingeordnet — ohne eine einzige
Zahl zu verändern.

Der Rest des Punktes — die **Berichtsanzeige** (`Allgemein/Bericht`) — bleibt offen und
steht in Abschnitt 7.

---

## 7. Restpunkte des KonfigUI-Konzepts

**Aus der D5b-Liste erledigt:** 1 (Ansicht „Schema"), 2 (Kaskadenband), 3 (Ergebnisspalte
Quellwärme), 4 teilweise (Vollzyklen — Ergebnisansicht ja, Bericht offen).

**Neu bzw. weiter offen:**

1. **Neue Regressionsbasis einfrieren.** Nach der Abnahme `Referenzlaeufe/2026-08-16_B4`
   anlegen; danach entfällt der Ausschluss `--ohne Heizkessel.Quellwaerme` (Abschnitt 4).
2. **Vollzyklen im BERICHT.** Die Markierung aus Abschnitt 6 wirkt nur in der
   Ergebnisansicht. `Allgemein/Bericht` gibt die Kennzahl unkommentiert aus; dort fehlt
   dieselbe Einordnung.
3. **Kaskaden-Abzweige im Band.** Der Zweig „Puffer → Heizkreis" neben einer weiterlaufenden
   Kette steht nur als Kante im Schema, nicht als Kettenglied (Abschnitt 1e). Sichtbar wird
   er erst in einer Konstellation mit Booster UND Heizkreisversorgung aus demselben Puffer.
4. **Ergebnisspalte `Speicherladung`** (5-1d) — dieselbe Familie wie die jetzt angelegte
   Quellwärme, weiterhin vorgemerkt.
5. **Schema als Bildexport / Berichtsbaustein.** Die Zeichnung liegt als reines GDI+ vor und
   ließe sich ohne Formular in eine PNG rendern (Muster `ChartRenderer`). Nicht Teil von D4.

**Weiter offen aus den D5a-/D5b-Listen, bewusst nicht angefasst:**

* `SimulationBHKW.ZweitsenkenRaum` behandelt den Kombispeicher als „eigenen Kanal" und lässt
  den Durchsatzterm weg (Review 1, K3-4) — als Modellentscheidung dokumentiert.
* Freistehender Quellpuffer, `Anteil_Umbuchen` ohne bekannte Herkunft, kesselseitige
  Quelltemperatur als Puffer-Vorlauf (Review 1, K3-5/6/7). Der freistehende Quellpuffer ist
  seit D4 wenigstens **sichtbar** — Projekt 1021 zeigt ihn als Speicherknoten mit
  Kaskadenkante und ohne Lader (Abschnitt 5.3, T4).
* Stufe 3 der Quellspeicher-Auflösung (`WQ_Puffer` gegen den **Katalog** `_STAMM`,
  `WaermequelleClass.QuellspeicherZeile`) baut einen Speicher auf, der zu keinem Projekt
  gehört. Unverändert offen; das Schema zeigt einen solchen Bezug bewusst NICHT als Kante,
  weil es nur Projekt-Puffer als Knoten führt.
* **Release-Notiz aus D5a 9.15 gilt unverändert:** Ein Projekt mit Kombispeicher darf nicht
  mit einer Fassung vor D5a geöffnet werden. D4 hebt den Schemastand auf **10**; eine
  ältere Fassung liest die Zusatzspalte schlicht nicht (namensbasierter Zugriff), der
  Schemastand-Riegel selbst bleibt vorgemerkt.

---

## 8. Reproduktion

```powershell
$msb = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# 1. Anwendung und Referenzlauf bauen (Ausgabe NIE nach bin\)
& $msb C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86 -p:OutDir=<scratch>\app\
& $msb C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj -t:Restore,Build `
       -p:Configuration=Debug -p:Platform=x86 -p:OutDir=<scratch>\ref\

# 2. Migrierte Wegwerf-Kopie (Produktiv-DB nur LESEN, vorher Kenndaten.laccdb pruefen)
<scratch>\ref\Referenzlauf.exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb <scratch>\DB

# 3. Regression Flag AUS gegen die eingefrorene Basis
foreach ($id in 1007,1008,1011,1017,1018,1021,1023,1024) {
    <scratch>\ref\Referenzlauf.exe projekt $id <scratch>\Lauf\Projekt_$id <scratch>\DB
}
<scratch>\ref\Referenzlauf.exe vergleich `
    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-15_B3 <scratch>\Lauf
#    -> 4x FAIL mit je EINER Abweichung (Heizkessel.Quellwaerme)
<scratch>\ref\Referenzlauf.exe vergleich `
    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-15_B3 <scratch>\Lauf --ohne Heizkessel.Quellwaerme
#    -> 8/8 PASS; zusaetzlich rekursiver MD5-Vergleich und Zeilendiff der vier aggregate.csv

# 4. Migrationsprobe: eigene Kopie auf Stand 9, zweimal migrieren
<scratch>\ref\Referenzlauf.exe migration <scratch>\MigTest\Kenndaten.accdb <scratch>\MigTest --nokopie
<scratch>\ref\Referenzlauf.exe migration <scratch>\MigTest\Kenndaten.accdb <scratch>\MigTest --nokopie
#    Spalten und Werte dazwischen per 32-bit-PowerShell + ACE gelesen

# 5. Prueflauf: eigenes net8-x86-Konsolenprojekt mit Projektverweis auf die Anwendung,
#    Properties.Settings.Default.DBPath per Reflection auf eine EIGENE Wegwerf-Kopie;
#    T7 praepariert die Booster-Konstellation per SQL auf dieser Kopie.
```

Arbeitskopien, Datenbankkopien, Vergleichsbauten und Prüfprogramm sind Wegwerf-Material und
nach der Abnahme gelöscht; die Zahlen dieses Protokolls sind der Beleg.
