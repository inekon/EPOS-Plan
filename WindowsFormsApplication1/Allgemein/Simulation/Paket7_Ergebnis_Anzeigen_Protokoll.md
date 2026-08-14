# Paket 7 — Ergebnis + Anzeigen (Umsetzungsprotokoll)

Stand: 14.08.2026 · Grundlage: [`Konzept_Simulation_QuellenSenken.md`](Konzept_Simulation_QuellenSenken.md),
Kapitel 6.6, 6.7, 9 und 13.3, dazu 13.1 (zweite Warnbedingung) ·
Vorarbeiten: [`Paket1_SchemaMigration_Protokoll.md`](Paket1_SchemaMigration_Protokoll.md)
(Tabelle `Tab_ErgebnisPufferspeicher` seit Schritt 3, offener Punkt
„`StellePufferTabelleSicher` → Paket 7") und
[`Paket3_Erdreichmodell_Protokoll.md`](Paket3_Erdreichmodell_Protokoll.md)
(offener Punkt 3: „Ergebnisanbindung der Auslegungsprüfung → Paket 7").

**Nicht committet.** Keine Schemaänderung: Die Ergebnistabelle legt weiterhin die
`SchemaMigration` an; `ErgebnisCtrl` bekommt nur die im Konzept 6.6 geforderte
Rückfallebene. Keine Designer- oder `.resx`-Datei angefasst.

---

## 1. Umfang

### Neue Dateien

| Datei | Inhalt |
|---|---|
| `Model/ErgebnisPufferspeicherModel.cs` | Detailmodell einer Pufferspeicher-Ergebniszeile, Spalten exakt nach Konzept 6.6 |
| `Allgemein/Simulation/ErdreichAuswertung.cs` | Ergebnisanbindung der VDI-4640-Prüfung: Jahresentzugsarbeit, Volllaststunden, maximale Entzugsleistung je Erdreich-Anlage; zweite Warnbedingung (Quelltemperatur − Spreizung < 0 °C); prozessweiter Zwischenspeicher je Projekt |

### Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | Rolle (`VERWENDUNG_HEIZUNG`/`VERWENDUNG_QUELLE`), `ID_Pufferspeicher`, `ID_Anlage`; Kennzahlen `SOC_Mittel`, `SOC_Max`, `Vollzyklen` + `KennzahlenBerechnen()`; `RolleAnzeige()`; `Schluessel(index)` als technischer Serienschlüssel `PUFFER_<ID>` / `QUELLE_<AnlagenID>` |
| `Allgemein/Simulation/SimulationControl.cs` | Rolle und Speicher-ID am `puffer_wp` gesetzt; neue Methode `AlleSpeicher()` (Senken-Puffer + alle Quellspeicher); Nachlauf am Ende von `Do_Simulation`: `KennzahlenBerechnen()` je Speicher + `ErdreichAuswertung.AusLauf(this)` |
| `Allgemein/Simulation/SimulationWaermepumpe.cs` | Lesezugriffe `Quellspeicher`, `Quelltemperaturen` und `WPTypen` (Wirksamkeitsregel); Quellspeicher merkt sich seine Anlagen-ID; `Init()` leert die quellenbezogenen Listen; `StundeAbschliessen` der Quellspeicher auch bei ausgesetzter Wärmepumpe |
| `Allgemein/Simulation/WaermequelleClass.cs` | `Quellspeicher()` liest zusätzlich die `ID` aus `Tab_Pufferspeicher_STAMM` und setzt Rolle „Quelle" |
| `Allgemein/Simulation/SimulationRunner.cs` | `Kapazitaet_Pufferspeicher` aus `puffer_wp.Q_max` (Fallback 0); `BaueErgebnis` füllt `ErgebnisModel.Pufferspeicher` aus `sim.AlleSpeicher()` |
| `Model/ErgebnisModel.cs` | `List<ErgebnisPufferspeicherModel> Pufferspeicher` |
| `Controller/ErgebnisCtrl.cs` | `TAB_PUFFER`; `StellePufferTabelleSicher()` (CREATE TABLE + Index + FK CASCADE, nach dem `Ddl()`-Vorbild aus `WirtschaftlichkeitCtrl`); defensives explizites `DELETE` in `Save`; Insert- und Load-Block für die Pufferzeilen |
| `Views/Simulation/NavigatorWaerme.cs` | Eine Chart-Serie je Speicher statt der einen „Speicherfüllstand"-Serie; Auswahlliste ab zwei Speichern; CSV-Spalte und Y-Skalierung ziehen mit |
| `Views/Simulation/Form_Simulation_Detail.cs` | CSV-Export mit drei Spalten je Speicher (Bezeichner im Kopf); `InitPufferspeicherRubrik()` + `PufferspeicherErgebnisAnzeigen()` (ListView statt `textBox_Pufferspeicher`); `ErdreichHinweisAnzeigen()` |
| `Views/Simulation/Form_QuelleErdreich.cs` | `HinweisErgebnis` / `HinweisVorbehalt` — der Dialog kann jetzt sagen, *warum* die Prüfung nicht möglich ist, statt pauschal „(noch kein Simulationslauf)" |
| `Views/Simulation/Form_Simulation_Config.cs` | Versorgt den Erdreichdialog aus `ErdreichAuswertung` (`ErgebnisseVorhanden`, `MaxEntzugW`, `JahresentzugKWh`, `VolllastStunden`, Hinweise) |
| `Allgemein/Simulation/SimulationPufferspeicher.cs` (Nacharbeit) | Vollzyklen rollenabhängig (Quelle: `Entladung_gesamt / Q_max`); `BezeichnerAnzeige()` und `Anzeige()` als einzige Stelle des „Speicher"-Ersatztexts |
| `Allgemein/Export/CsvExportClass.cs` | Spaltenköpfe: Trennzeichen ersetzen, Namensdubletten mit `_2`, `_3` … eindeutig machen |
| `../../Referenzlauf/Ergebnisexport.cs` | `Tab_ErgebnisPufferspeicher`-Zeilen im `aggregate.csv` (über den stillen Lesezugriff); Kennzahlen `Puffer.SOC_Mittel/SOC_Max/Vollzyklen`; `Sim.Speicher_Anzahl`; Ganglinien der Quellspeicher als eigene CSVs; `Erdreich[i].*` der Auslegungsprüfung |
| `../../Referenzlauf/Projektauswahl.cs` | Merkmal `QuellspeicherWP`, Pflichtkategorie „Wärmepumpe mit Quellspeicher", `MAX_PROJEKTE` 8 → 9 |
| `../../Referenzlauf/Program.cs` | `lauf` migriert die Arbeitskopie (Schritt 2b); `liste [<dbOrdner>]` liest eine vorhandene Kopie |

**Nicht angefasst** (Parallelarbeit/Feature): `WizardCtrl.cs`, `WErzeugerModel.cs`,
`Form_BHKWEing.cs`, `WizardParent.cs`, `Form_Heizkessel*.cs`, `SimulationSPK.cs`,
`RecordSet.cs` sowie sämtliche `.Designer.cs`- und `.resx`-Dateien.

---

## 2. Teil A — SOC-Kennzahlen

`SimulationPufferspeicher.KennzahlenBerechnen()` wertet nach dem Lauf die Ganglinie
`SOC_stuendlich` aus:

* `SOC_Mittel` = arithmetisches Mittel über 8760 Stunden [kWh]
* `SOC_Max` = Maximum der Ganglinie [kWh]
* `Vollzyklen` = `Ladung_gesamt / Q_max`, bei `Q_max <= 0` **0** (Division-durch-Null-Absicherung)

Die Methode ist idempotent — sie rechnet ausschließlich aus Ganglinie und Jahressummen,
nicht inkrementell. Aufgerufen wird sie zentral am Ende von
`SimulationControl.Do_Simulation` für jeden Speicher aus `AlleSpeicher()`; damit gilt sie
gleichermaßen für den headless-Lauf und für die Detailansicht.

`Reset()` setzt die drei Kennzahlen mit zurück.

---

## 3. Teil B — Persistenz

### 3.1 Was geschrieben wird

Je Lauf **eine Zeile je beteiligtem Speicher**, in stabiler Reihenfolge aus
`SimulationControl.AlleSpeicher()`:

1. der Senken-Puffer der Wärmepumpe (`sim.puffer_wp`) — `Verwendung = 'Heizung'`,
   `ID_Pufferspeicher` aus der **Projektkopie** `Tab_Pufferspeicher`
2. jeder Quellspeicher eines WP-Moduls (`SimulationWaermepumpe.Quellspeicher`) —
   `Verwendung = 'Quelle'`, `ID_Pufferspeicher` aus dem **Katalog**
   `Tab_Pufferspeicher_STAMM` (dort liest `WaermequelleClass.Quellspeicher()` den Speicher
   über den Bezeichner)

Der Bezeichner kommt in beiden Fällen aus dem Speicherobjekt. Die Spalte `Verwendung` ist
`TEXT(50)`; die beiden Werte stehen als Konstanten in `SimulationPufferspeicher`.

> **Anmerkung zur ID-Semantik.** `ID_Pufferspeicher` zeigt beim Senkenspeicher auf
> `Tab_Pufferspeicher`, beim Quellspeicher auf `Tab_Pufferspeicher_STAMM`. Das ist eine
> Folge des Bestands (Paket 3/4 lösen die Quellzuordnung über `WQ_ID_Puffer` auf die
> Projektkopie um) und in `WaermequelleClass.Quellspeicher()` als Kommentar vermerkt.
> Solange die Spalte keinen erzwungenen Fremdschlüssel trägt, ist das unkritisch;
> mit Paket 4 ist die Quelle auf die Projektkopie nachzuziehen. → offener Punkt 1.

### 3.2 ID-Vergabe

Wie bei allen Geschwistertabellen: `MAX(ID)+1` einmal je Block, danach hochzählend
(`NextId(conn, trans, TAB_PUFFER)` und `pufId++` je Zeile) — identisch zum Muster der
Modultabellen.

### 3.3 `StellePufferTabelleSicher()`

Rückfallebene für Datenbanken ohne gelaufene Migration, nach dem `Ddl()`-Vorbild aus
`WirtschaftlichkeitCtrl.cs:72-190`:

* Tabellenprüfung vorab über `GetOleDbSchemaTable(Tables)` — auf migrierten Datenbanken
  passiert nichts (Duplikat-tolerant, die Tabelle existiert dort bereits)
* `CREATE TABLE` mit demselben Spaltensatz wie `SchemaMigration.SQL_CREATE_ERGEBNISPUFFER`
* `CREATE INDEX idx_ErgPuffer`
* `ALTER TABLE … ADD CONSTRAINT FK_ErgPuffer … ON DELETE CASCADE`

Jeder Schritt einzeln abgesichert; schlägt das `CREATE TABLE` fehl, entfallen Index und
Constraint (sie wären sinnlos). Aufgerufen wird die Methode in `Save`, zusammen mit den
drei bestehenden `Stelle…SpaltenSicher()`.

### 3.4 Defensives Delete

`Save` löscht die Pufferzeilen des Projekts **vor** dem Kopf-Delete explizit:

```sql
DELETE FROM Tab_ErgebnisPufferspeicher
 WHERE ID_Ergebnis IN (SELECT ID FROM Tab_Ergebnis WHERE ID_Projekt = ?)
```

Auf migrierten Datenbanken ist das ein No-op (die Löschweitergabe hätte es ohnehin
erledigt); auf einer von `StellePufferTabelleSicher()` ohne Constraint entstandenen
Tabelle verhindert es Waisenzeilen, die wegen der `MAX(ID)+1`-Vergabe später auf fremde
Läufe zeigen würden (Konzept 6.6).

### 3.5 Load

`ErgebnisCtrl.Load` füllt `ErgebnisModel.Pufferspeicher` aus derselben Tabelle. Fehlt die
Tabelle, bleibt die Liste leer (der Block ist gekapselt).

---

## 4. Teil C — `Kapazitaet_Pufferspeicher` (dokumentierte Ergebnisänderung)

| | alt | neu |
|---|---|---|
| Quelle | `wp.Volumen_Pufferspeicher * 1.16` — Volumen aus dem **WP-Datensatz**, ohne ΔT (also implizit ΔT = 1 K) und ohne `/1000` | `sim.puffer_wp.Q_max` [kWh] des zugeordneten Puffers |
| ohne Puffer | derselbe Legacy-Ausdruck | **0** |

Der Altwert widersprach der Anzeige in der Detailansicht, die schon vorher `Q_max` zeigte
(Konzept 6.6). Die Änderung ist der Zweck des Teilpakets.

> **Zum Rückfallwert 0.** Konzept 6.6 schreibt `Q_max` als Quelle vor, sagt aber **nicht**,
> was ohne zugeordneten Speicher gelten soll. Die 0 ist eine **Festlegung dieses Pakets**,
> keine Konzeptvorgabe: Ohne Speicher gibt es keine Speicherkapazität, und der bisherige
> Ersatzwert 11,6 kWh war eine reine Scheingröße aus einem Feld des WP-Datensatzes. Wer
> das anders sieht, ändert genau eine Stelle in `SimulationRunner`.

**Wirkung auf die acht Referenzprojekte** (`Waermepumpe.Kapazitaet_Pufferspeicher`):

| Projekt | alt (B0) | neu | Grund |
|---|---|---|---|
| 1007 | 11,6 | **0** | kein Puffer zugeordnet |
| 1008 | 11,6 | **6,96** | Puffer 1008007 „Vitocell 140-E 600 Liter", 600 l · 1,16 · 10 K / 1000 |
| 1010 | 11,6 | **0** | kein Puffer zugeordnet |
| 1011 | 11,6 | **0** | Puffer vorhanden, aber anderem Erzeuger zugeordnet |
| 1017 | — | — | keine Wärmepumpe, keine `Tab_ErgebnisWaermepumpe`-Zeile |
| 1018 | — | — | keine Wärmepumpe |
| 1023 | 11,6 | **13,92** | Puffer 1018023 „Vitocell 140-E 600 Ltr", 1200 l · 1,16 · 10 K / 1000 |
| 1024 | 11,6 | **0** | kein Puffer zugeordnet |

> **Wichtig und über die Vorgabe hinaus:** Der Altwert war in **allen** sechs
> WP-Projekten identisch 11,6 kWh (`Volumen_Pufferspeicher = 10` aus dem WP-Datensatz,
> mal 1,16). Betroffen sind deshalb nicht nur die beiden Projekte **mit** Puffer-Zuordnung
> (1008, 1023), sondern auch die vier **ohne** — dort fällt der Wert von 11,6 auf 0. Das
> ist die unmittelbare Folge des im Konzept 6.6 verlangten Fallbacks „0 bei fehlendem
> Puffer" und fachlich richtig: ohne zugeordneten Speicher gibt es keine Speicherkapazität.
> Der bisherige 11,6-Wert war eine reine Scheingröße.

---

## 5. Teil D — Anzeigen (Konzept 13.3)

### 5.1 `NavigatorWaerme`

* `sim.AlleSpeicher()` liefert die Speicherliste; je Speicher wird **eine** Chart-Serie
  angelegt.
* `Series.Name` ist der **technische Schlüssel** `PUFFER_<ID>` bzw. `QUELLE_<AnlagenID>`
  (Fallback auf den Listenindex, falls keine ID vorliegt). Der Anzeigetext
  „Bezeichner (Senkenspeicher|Quellspeicher)" steht ausschließlich in `Series.LegendText` —
  genau die Trennung, die Konzept 13.3 wegen der Lokalisierung (Paket 9) verlangt.
* Sechs Farben rotieren über die Speicher.
* Die Checkbox „Speicherfüllstand" bleibt als Sammelschalter (aktiv, sobald mindestens
  ein Speicher existiert). Ab **zwei** Speichern erscheint zusätzlich eine ComboBox
  „Alle Speicher / <Speicher 1> / <Speicher 2> …" — die Auswahlliste aus 13.3. Sie
  schränkt ein, welche Serie bei gesetzter Checkbox sichtbar ist.
* Y-Skalierung (`:126` und der Wärmebedarf-Handler) rechnet jetzt mit dem Maximum **über
  alle** Speicher (`SpeicherMax()`).
* Der CSV-Export des Navigators (`:89-90`) schreibt eine Spalte je **sichtbarem**
  Speicher, mit Bezeichner und Rolle im Kopf.

### 5.2 CSV-Export `Form_Simulation_Detail` (~`:292-298`)

Statt drei fest verdrahteter Spalten für den einen `puffer_wp` jetzt **drei Spalten je
Speicher** — Ladung, Entladung, Speicherinhalt —, jeweils mit
„`<Bezeichner> (<Rolle>) <Größe> [kWh]`" im Kopf. Quellspeicher sind damit erstmals im
Export enthalten. Die Kopfzeile bleibt deutsch: sie ist Exportformat, nicht Oberfläche
(Konzept 13.6).

> **Der Kopf ist seit der Nacharbeit robust — vorher war er es nicht.** Mit dem Bezeichner
> wandert erstmals ein frei gepflegter Text in die Kopfzeile. Ein Semikolon darin hätte
> die Spalten gegen die Datenzeilen verschoben, zwei gleichnamige Speicher (Katalog und
> Projektkopie tragen oft denselben Namen) wären in der Auswertung nicht mehr zu trennen
> gewesen. `CsvExportClass` ersetzt das Trennzeichen jetzt zentral durch ein Komma und
> hängt bei Namensdubletten `_2`, `_3` … an — an **einer** Stelle für alle Exporte, also
> auch für den Navigator.

### 5.3 Ergebnistabelle statt `textBox_Pufferspeicher` (~`:1241-1244`)

`InitPufferspeicherRubrik()` legt im Konstruktor programmatisch an — Muster
`listView_SimSolar` / `listView_SimPV`, **kein** Designer- und **kein** `.resx`-Eingriff:

* `listView_SimPuffer` unmittelbar unter `listView_SimWP`, gleiche Breite, verankert
  Top|Left|Right. Damit dafür Platz ist, wird `listView_SimWP` zur Laufzeit von 244 auf
  134 px Höhe gesetzt (für die typischen ein bis drei Module reichlich).
  Spalten: Speicher · Rolle · Kapazität [kWh] · Ladung [kWh/a] · Entladung [kWh/a] ·
  Verluste [kWh/a] · Vollzyklen · Füllstand Ende [kWh].
* `label_Erdreich` darunter für die Warnungen der Auslegungsprüfung (5.4).

`PufferspeicherErgebnisAnzeigen()` füllt die Tabelle aus **denselben**
`SimulationPufferspeicher`-Objekten, die auch `Tab_ErgebnisPufferspeicher` speisen — eine
Quelle der Wahrheit.

> **Abweichung von Konzept 13.3 (bewusst, deklariert).** 13.3 verlangt, dass die Anzeige
> „identisch mit Bericht und Wirtschaftlichkeit" ist, und meint damit die Ergebnistabelle
> `Tab_ErgebnisPufferspeicher` als gemeinsame Quelle. Die Detailansicht speist sich
> stattdessen aus den **Simulationsobjekten des laufenden Laufs**. Die Werte sind
> zahlengleich — beide Wege lesen dieselben Felder desselben Objekts, die Persistenz
> rundet lediglich auf zwei Nachkommastellen (`ErgebnisCtrl.R`). Der Grund ist praktisch:
> Die Detailansicht zeigt das Ergebnis unmittelbar nach dem Lauf, auch wenn das Speichern
> fehlgeschlagen ist oder gar nicht stattgefunden hat. Auf die Ergebnistabelle umzustellen
> hieße, die Anzeige von einem erfolgreichen `Save` abhängig zu machen. Sobald Bericht und
> Wirtschaftlichkeit die Tabelle lesen (offener Punkt 5), ist die Gegenprobe
> „Anzeige == Tabelle" mit einem Blick zu führen.

Hat der Lauf mindestens einen Speicher, werden `textBox_Pufferspeicher` und die
Beschriftung `label38` ausgeblendet; ohne Speicher bleibt es beim bisherigen Textfeld mit
dem Legacy-Ausdruck, damit sich für Projekte ohne Speicher optisch nichts ändert.

Beide Anzeigemethoden werden seit der Nacharbeit **außerhalb** von
`if (sim.bSimulationWP)` gerufen: Wird die Wärmepumpe in einem Folgelauf abgewählt, muss
die Rubrik geleert werden, statt die Zahlen des Vorlaufs stehen zu lassen.

### 5.4 Übergangshinweis „Speicher 1 von n"

Entfällt (Konzept 6.7): Navigator, CSV-Export und Detailansicht zeigen jetzt **alle**
Speicher. Der Alias `sim.puffer_wp` bleibt unverändert bestehen — `ZeitreihenExtraktor`
(Berichtsmodul) und `Referenzlauf/Ergebnisexport` nutzen ihn weiter.

Alle sichtbaren Texte sind deutsch hartkodiert. Das entspricht dem Bestandsmuster des
Simulationsbereichs; die durchgängige Umstellung auf `MyResource` gehört zu **Paket 9**
und ist an jeder Stelle als solche kommentiert.

---

## 6. Teil E — Erdreich-Anbindung (Paket 3, offener Punkt 3)

### 6.1 Was geliefert wird

`ErdreichAuswertung.AusLauf(sim)` läuft am Ende jedes `Do_Simulation` und legt je
Energieanlage mit `WQ_Typ = 'Erdreich'` ab:

| Größe | Herleitung |
|---|---|
| Entzugsganglinie [kW] | `WP_Waermeproduktion_stuendlich[h] − WP_Strombedarf_stuendlich[h]`, auf ≥ 0 geklemmt |
| Jahresentzugsarbeit [kWh/a] | Summe der Entzugsganglinie |
| max. Entzugsleistung [W] | Maximum der Entzugsganglinie × 1000 |
| Volllaststunden [h/a] | Jahresentzugsarbeit / Spitzenentzugsleistung |
| Betriebsstunden [h/a] | Stunden mit `WP_Waermeproduktion_stuendlich[h] > 0` (Bezug der Frostprüfung) |

Daraus wird `VDI4640Pruefung.PruefeKollektor` bzw. `PruefeSonde` aufgerufen — mit
Klimazone aus `Tab_Klimaregion.Klimazone_DIN4710` des Projekts und den Anlagendaten
`WQ_Quellsystem`, `WQ_Bodentyp`, `WQ_Tiefe`, `WQ_Flaeche`, `WQ_Anzahl`.

> **Basiskorrektur (Nacharbeit).** Bis zur Nacharbeit kamen Jahresentzugsarbeit und
> Volllaststunden aus den **Modul-Jahressummen** (`Modul_WP_Waermeproduktion`,
> `Modul_WP_Strombedarf`, `Modul_WP_Laufzeit`), die Spitzenleistung dagegen aus der
> **globalen Stundenganglinie**. Das sind zwei verschiedene Zahlenwerke: Die Wärme, mit
> der die Wärmepumpe den Senkenspeicher lädt, wird in `SimulationWaermepumpe` nur auf die
> Ganglinie und die Gesamtsummen addiert, **nicht** auf die Modulsummen (Block
> „Pufferspeicher: Laden aus WP-Überschuss"). Die Prüfung setzte damit einen zu kleinen
> Jahresentzug gegen eine zu große Spitze — sie fiel systematisch zu milde aus.
>
> Gemessen an Projekt 1008 (ein Sole-Wasser-Modul mit Erdreichquelle, Senkenspeicher
> 6,96 kWh):
>
> | Größe | alte Basis (Modulsummen) | neue Basis (Ganglinie) |
> |---|---|---|
> | Wärmeproduktion | 53 510 kWh | 74 710 kWh |
> | Stromaufnahme | 10 530 kWh | 14 432 kWh |
> | **Jahresentzugsarbeit** | **42 980 kWh** | **60 279 kWh** (+40,2 %) |
> | **Volllaststunden** | **2 884 h** (Modul-Laufzeit) | **3 313 h** (Arbeit / Spitze) |
> | max. Entzugsleistung | 18 197 W | 18 197 W (unverändert) |
>
> Die Differenz der Wärmeproduktion beträgt 21 200 kWh und ist ziffernweise
> `Puffer.Ladung_gesamt` = 21 198 kWh — die Speicherladung, exakt wie beschrieben.
> Der alte Zahlensatz war in sich unstimmig: 42 980 kWh bei 18 197 W ergäben 2 362 h,
> ausgewiesen wurden 2 884 h.
>
> Bis Paket 4 die Ganglinie je Modul liefert, ist die globale Ganglinie die belastbarere
> der beiden Basen. Gibt es einen Senkenspeicher, enthält sie dessen Ladung; der Kurztext
> führt dann die Kennzeichnung **„inkl. Speicherladung"** und der Dialog denselben
> Hinweis unter der Prüfung.

### 6.2 Dokumentierte Grenze der Zuordnung

Wärmeproduktion und Strombedarf liegen als **Stundenganglinie nur global** vor (Summe
aller WP-Module). Die Zuordnung zum Modul ist deshalb gestuft:

| Fall | Verhalten |
|---|---|
| genau **ein** WP-Modul, wirksam Erdreich | exakt — die globale Ganglinie *ist* die des Moduls, keine Einschränkung |
| mehrere Module, **alle** wirksam Erdreich | die globale Ganglinie ist vollständig Erdreich-Entzug; der Modulanteil wird **proportional zur Modul-Jahresentzugsarbeit** verteilt und als Näherung gekennzeichnet („Spitze anteilig aus der Summenganglinie") |
| **gemischte** Quellen (mindestens ein Modul nicht wirksam Erdreich) | keine Prüfung; Text „maximale Entzugsleistung nicht je Modul trennbar (mehrere Wärmepumpen mit unterschiedlichen Quellen, Stundenganglinie liegt nur global vor)" |

Die Modul-Jahressummen dienen jetzt **nur noch als Verteilungsschlüssel**, nicht mehr als
Absolutwert — sonst käme der Basisbruch über die Hintertür zurück. Die saubere Auflösung
verlangt eine Ganglinie je Modul; die entsteht mit dem Engine-Umbau in Paket 4.
→ offener Punkt 2.

### 6.2a Wirksamkeitsregel: Luft-Wasser wird nicht geprüft

`WaermequelleClass.Quelltemperatur()` und `…Quellspeicher()` liefern für
`Tab_WP.Typ = 'Luft-Wasser'` (und für einen leeren Typ) **immer** die Außenluft bzw.
`null` — eine dort gepflegte `WQ_*`-Konfiguration wird von der Engine nie gerechnet.
Solche Anlagen werden deshalb **nicht** geprüft. Stattdessen sagen Kurztext und Dialog:

> „Die Wärmepumpe ist eine Luft-Wasser-Anlage — die Erdreich-Konfiguration bleibt in der
> Simulation unwirksam (gerechnet wird mit der Außenluft). Für eine Erdreich-Quelle eine
> Sole-Wasser- oder Wasser-Wasser-Wärmepumpe wählen."

Ohne diese Regel stand eine VDI-4640-Aussage über ein Erdreich im Ergebnis, das die
Simulation nie angefasst hat. Die Regel wirkt auch auf die Eindeutigkeit: Ein
Luft-Wasser-Modul speist die globale Ganglinie mit Luftwärme und macht den Fall damit
genauso „gemischt" wie eine andere Quelle. Die Bauart je Modul liefert die Engine selbst
(`SimulationWaermepumpe.WPTypen`) — dieselbe Quelle, aus der sie ihre eigene Entscheidung
zieht.

### 6.3 Zweite Warnbedingung (Konzept 13.1)

Gezählt werden die Stunden, in denen `Quelltemperatur[h] − WQ_Spreizung < 0 °C` gilt —
**ausschließlich innerhalb der Betriebsstunden**. In der Stillstandszeit entzieht niemand
Wärme, das Erdreich regeneriert; eine Frostmeldung daraus wäre gegenstandslos. Gewarnt
wird ab **5 % der Betriebsstunden** (`FROST_ANTEIL_MAX`). Zuvor war der Bezug die volle
Jahresstundenzahl: Schwelle 438 h, gezählt auch über Stunden ohne Entzug.

`WQ_Spreizung` ist seit der Nacharbeit auch im Erdreichdialog pflegbar — Eingabefeld
„Nutzbare Spreizung [K]", Vorgabe 5, geschrieben über `WertSchreiben` wie die übrigen
Felder. Vorher gab es das Feld nur im Pufferspeicher-Quellendialog, bei einer
Erdreichquelle rechnete die Prüfung also immer mit der Vorgabe.

Der Meldungstext nennt jetzt die **Normbasis**:

> „Hinweis: Quelltemperatur − Spreizung liegt in 1 165 von 5 199 Betriebsstunden unter
> 0 °C (VDI 4640 Bl. 2 bemisst gegen −5 °C Soleaustritt; die Auslegungsprüfung bleibt
> davon unberührt)."

Damit liest sich „VDI 4640: eingehalten" neben einer Frostmeldung nicht mehr wie ein
Widerspruch: Die Norm bemisst gegen −5 °C Soleaustritt, die Frostbedingung aus Konzept
13.1 ist eine **zusätzliche, strengere** Betrachtung.

### 6.4 Wo es sichtbar wird

* **Detailansicht** (`Form_Simulation_Detail`, WP-Ergebnisbereich): eine kompakte
  Textzeile je Erdreich-Anlage unter der Speicher-Tabelle, z. B.
  „Erdreich WP-1: Entzug 18.400 kWh/a, Spitze 6.480 W, 1.950 h/a. VDI 4640: Grenzwert
  überschritten — Quelle zu klein bemessen!" — rot bei Warnung. Damit erreicht die Prüfung
  den Anwender auch dann, wenn er den Quellendialog nie mehr öffnet (Konzept 4.5).
* **Erdreichdialog** (`Form_QuelleErdreich` über `Form_Simulation_Config`): der Dialog
  bekommt `ErgebnisseVorhanden`, `MaxEntzugW`, `JahresentzugKWh` und `VolllastStunden`
  gesetzt, sobald für die Anlage ein Lauf der laufenden Sitzung vorliegt. Ist der Fall
  nicht trennbar, steht der Grund im Dialog statt des pauschalen
  „(noch kein Simulationslauf)"; ist er nur genähert, steht der Vorbehalt unter der Prüfung.

### 6.5 Ablage

Die Werte liegen **prozessweit je Projekt** in einem statischen Dictionary (Muster der
übrigen Statics in `Program`) und werden **nicht** persistiert — sie gelten für den Lauf
der laufenden Sitzung. Das ist bewusst: Konzept 6.6 listet für
`Tab_ErgebnisPufferspeicher` keine Erdreich-Größen, und eine eigene Ergebnistabelle dafür
wäre eine Schemaänderung außerhalb dieses Pakets. Folge: Nach einem Programmneustart zeigt
der Erdreichdialog wieder „(noch kein Simulationslauf)", bis erneut gerechnet wurde.
→ offener Punkt 3.

---

## 7. Teil F — Verifikation

### F1 — Build

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    ..\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
```

**0 Fehler.** Keine neuen Warnungen — es bleiben dieselben sechs Bestandswarnungen
(`WErzeugerModel`, `StromverbraucherStammCtrl`, `KlimaregionStammCtrl` ×2, `MDIMainForm` ×2),
alle aus Bestand bzw. Parallelarbeit. `Referenzlauf.csproj` ebenfalls 0 Fehler.

### F2 — Regression gegen `2026-08-14_B0`

**Testumgebung.** Eigene Kopie **außerhalb des Repos**:
`C:\Waermeplan\Paket7_Nach\DB_Basis\Kenndaten.accdb`, angelegt und vollständig migriert mit

```powershell
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\Paket7_Nach\DB_Basis
```

→ **Schema-Nachweis 0 Abweichungen**, `Tab_ErgebnisPufferspeicher` 13/13 Spalten,
Index `idx_ErgPuffer` vorhanden, `FK_ErgPuffer` mit `DELETE=CASCADE`. Gerechnet im Modus
`projekt` je Projekt (nicht `lauf` — der legt `Referenzlaeufe\Arbeitskopie` **im Repo** neu
an; für diese Nacharbeit war eine Kopie außerhalb des Repos vorgegeben). Die produktive
`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` wurde ausschließlich **gelesen**.

Verglichen werden die **acht** Projekte, die es in beiden Ständen gibt; Projekt 1021 ist
mit der Nacharbeit neu und in B0 nicht enthalten.

**Erwartung war kein globales PASS.** Ergebnis:

```
Projekt_1007: FAIL (29 Dateien, 324210 Werte,  2 Abweichungen)
Projekt_1008: FAIL (21 Dateien, 227847 Werte, 16 Abweichungen)
Projekt_1010: FAIL (18 Dateien, 201540 Werte,  2 Abweichungen)
Projekt_1011: FAIL (29 Dateien, 324232 Werte,  2 Abweichungen)
Projekt_1017: FAIL (20 Dateien, 245378 Werte,  1 Abweichung)
Projekt_1018: FAIL (19 Dateien, 210343 Werte,  1 Abweichung)
Projekt_1023: FAIL (25 Dateien, 262917 Werte, 16 Abweichungen)
Projekt_1024: FAIL (22 Dateien, 236616 Werte,  2 Abweichungen)
```

**Vollständige Abweichungsliste — 42 Abweichungen, jede erklärbar:**

| Art | Anzahl | Wo | Erklärung |
|---|---|---|---|
| `Waermepumpe.Kapazitaet_Pufferspeicher` geändert | **6** | 1007, 1008, 1010, 1011, 1023, 1024 | Teil C: 11,6 → 0 / 6,96 / 13,92 (Tabelle in Abschnitt 4) |
| `Sim.Speicher_Anzahl` neu | **8** | alle acht | neuer Skalar des Exportwerkzeugs (0 bzw. 1) |
| `Puffer.SOC_Mittel`, `Puffer.SOC_Max`, `Puffer.Vollzyklen` neu | **6** | 1008, 1023 (je 3) | neue Kennzahlen aus Teil A |
| `Pufferspeicher[0].*` neu (11 Spalten je Zeile) | **22** | 1008, 1023 (je 11) | neue Persistenz aus Teil B, gelesen aus `Tab_ErgebnisPufferspeicher` |

Im Einzelnen für die beiden Speicherprojekte (Diff der `aggregate.csv`):

| Größe | Projekt 1008 | Projekt 1023 |
|---|---|---|
| `Pufferspeicher[0].ID_Pufferspeicher` | 1008007 | 1018023 |
| `Pufferspeicher[0].Bezeichner` | Vitocell 140-E 600 Liter | Vitocell 140-E 600 Ltr |
| `Pufferspeicher[0].Verwendung` | Heizung | Heizung |
| `Pufferspeicher[0].Q_max` | 6,96 | 13,92 |
| `Pufferspeicher[0].Ladung_gesamt` | 22 260,33 | 13 244,54 |
| `Pufferspeicher[0].Entladung_gesamt` | 21 837,93 | 12 997,32 |
| `Pufferspeicher[0].Verluste_gesamt` | 415,64 | 247,22 |
| `Pufferspeicher[0].SOC_Ende` | 6,76 | 0 |
| `Pufferspeicher[0].SOC_Mittel` | 3,73 | 4,46 |
| `Pufferspeicher[0].SOC_Max` | 6,87 | 13,83 |
| `Pufferspeicher[0].Vollzyklen` | 3 198,32 | 951,48 |
| `Waermepumpe.Kapazitaet_Pufferspeicher` | 11,6 → **6,96** | 11,6 → **13,92** |

`Ladung_gesamt`, `Entladung_gesamt`, `Verluste_gesamt` und `Q_max` stimmen ziffernweise mit
den bereits in B0 vorhandenen `Puffer.*`-Skalaren überein — die Persistenz gibt also exakt
das wieder, was die Engine gerechnet hat.

**Keine einzige Abweichung in einem Vektor.** Alle 2 033 047 Werte der B0-Basis liegen
unverändert innerhalb der Toleranz; die 2 033 083 Werte des neuen Laufs unterscheiden sich
davon nur um die 36 neu hinzugekommenen Einträge. Die Anzahl der CSV-Dateien je Projekt ist
identisch zu B0 (29/21/18/29/20/19/25/22) — keines der acht Projekte hat einen
Quellspeicher, deshalb entstehen keine `quellspeicher_*.csv`.

Rohausgabe: [`../../../Referenzlaeufe/2026-08-14_Paket7/vergleich_protokoll.md`](../../../Referenzlaeufe/2026-08-14_Paket7/vergleich_protokoll.md).

### F3 — Neue Referenzbasis (9 Projekte)

`Referenzlaeufe/2026-08-14_Paket7/` — **komplett neu eingefroren** nach allen Fixes:
1007, 1008, 1010, 1011, 1017, 1018, **1021**, 1023, 1024. 1021 kommt über die neue
Pflichtkategorie „Wärmepumpe mit Quellspeicher" hinein und ist das einzige Projekt, das
den `QUELLE_`-Pfad überhaupt berührt. Dazu `lauf_protokoll.md` und
`vergleich_protokoll.md`.

```
Referenzlauf.exe liste <dbOrdner>             -> Auswahl = die alten 8 + 1021
Referenzlauf.exe pruefen  2026-08-14_Paket7   -> GESAMT: plausibel (9/9)
Selbstvergleich (zweiter Lauf, gleiche DB)    -> GESAMT: PASS (2 260 923 Werte)
```

Die Auswahl der bisherigen acht ist **unverändert** — die neue Pflichtkategorie steht
bewusst hinter den fünf ursprünglichen, damit deren Wahlen und die Auffüllung gleich
bleiben.

`2026-08-14_B0/` bleibt als historischer Stand liegen. `Referenzlaeufe/LIESMICH.md` ist um
den Abschnitt „Aktuelle Basis" und um **zwei getrennte Reproduktionswege** ergänzt
(`lauf` bzw. `migration` + `projekt`) — der frühere Ablauf war irreführend, weil `lauf`
die Arbeitskopie nicht migriert hat. Inzwischen tut er es (Schritt 2b in `ModusLauf`).

### F4 — Persistenz-Nachweis auf der Testkopie

Nach dem Lauf von 1008 und 1023 (gelesen mit ACE OLEDB 32-bit direkt aus
`C:\Waermeplan\Paket7_Test\DB\Kenndaten.accdb`):

```
ID_Projekt=1008  ID=1  ID_Ergebnis=166  ID_Pufferspeicher=1008007
                 Bezeichner=Vitocell 140-E 600 Liter  Verwendung=Heizung
                 Q_max=6,96  Ladung=22260,33  Entladung=21837,93  Verluste=415,64
                 SOC_Ende=6,76  SOC_Mittel=3,73  SOC_Max=6,87  Vollzyklen=3198,32
ID_Projekt=1023  ID=2  ID_Ergebnis=171  ID_Pufferspeicher=1018023
                 Bezeichner=Vitocell 140-E 600 Ltr   Verwendung=Heizung
                 Q_max=13,92 Ladung=13244,54 Entladung=12997,32 Verluste=247,22
                 SOC_Ende=0  SOC_Mittel=4,46  SOC_Max=13,83  Vollzyklen=951,48
GESAMTZEILEN=2
```

Plausibilität der Vollzyklen: 22 260,33 / 6,96 = **3 198,32** ✔ · 13 244,54 / 13,92 =
**951,48** ✔. `SOC_Max ≤ Q_max` in beiden Zeilen ✔.

**Wiederholungslauf ersetzt statt zu duplizieren.** Beide Projekte ein zweites Mal
gerechnet:

```
GESAMTZEILEN=2   (IDs jetzt 3 und 4, ID_Ergebnis 173 und 174)
```

Die Löschweitergabe `FK_ErgPuffer` hat die Vorgängerzeilen mit dem Kopf abgeräumt.

**`StellePufferTabelleSicher()` auf einer Kopie OHNE Tabelle.** Zweite Kopie
`C:\Waermeplan\Paket7_Test\DB_ohneTabelle`, dort `DROP TABLE Tab_ErgebnisPufferspeicher`
(Nachweis: 0 Treffer im Schema), `SchemaVersion` bleibt bei 5 — die Migration läuft also
**nicht** noch einmal, nur die Rückfallebene kann greifen. Nach einem Lauf von Projekt 1008:

```
Tabelle vorhanden: 1
Spalten (13): Bezeichner, Entladung_gesamt, ID, ID_Ergebnis, ID_Pufferspeicher,
              Ladung_gesamt, Q_max, SOC_Ende, SOC_Max, SOC_Mittel, Verluste_gesamt,
              Verwendung, Vollzyklen
Indizes: <PK>, FK_ErgPuffer, idx_ErgPuffer
FK: Tab_ErgebnisPufferspeicher.ID_Ergebnis -> Tab_Ergebnis.ID  DELETE=CASCADE
Zeilen: 1   -> Vitocell 140-E 600 Liter | Q_max=6.96 | Ladung=22260.33 | Vollzyklen=3198.32
```

Die Rückfallebene legt Tabelle, Index **und** Löschweitergabe korrekt an.

### F5 — Kodierung und Diff

| Datei | Kodierung | Zeilenenden |
|---|---|---|
| `SimulationPufferspeicher.cs`, `WaermequelleClass.cs` | UTF-8 **ohne** BOM (wie vorgefunden) | LF (wie vorgefunden) |
| `SimulationRunner.cs`, `SimulationControl.cs`, `SimulationWaermepumpe.cs`, `ErgebnisCtrl.cs`, `ErgebnisModel.cs`, `NavigatorWaerme.cs`, `Form_Simulation_Detail.cs` | UTF-8 **mit** BOM (wie vorgefunden) | CRLF (wie vorgefunden) |
| `Form_QuelleErdreich.cs`, `Form_Simulation_Config.cs`, `Referenzlauf/Ergebnisexport.cs` | UTF-8 mit BOM (wie vorgefunden) | LF (wie vorgefunden) |
| **neu**: `ErdreichAuswertung.cs`, `ErgebnisPufferspeicherModel.cs` | UTF-8 mit BOM | CRLF |

Alle bearbeiteten Dateien sind gültiges UTF-8; keine Datei hat Kodierung oder
Zeilenendenstil gewechselt. Der `git diff` über die von Paket 7 berührten Dateien enthält
**null** Ersatzzeichen. Die Ersatzzeichen im Gesamt-Diff des Projekts stammen weiterhin aus
`Views/BHKW/Form_BHKWEing.cs` (Parallelarbeit, nicht angefasst).

---

## 8. Bewusste Abweichungen

1. **Lokalisierung hartkodiert.** Alle neuen sichtbaren Texte (Spaltenköpfe der
   Speichertabelle, Rollenbezeichnungen, Auswahlliste, Erdreich-Hinweiszeile,
   Dialogtexte) sind deutsch hartkodiert — Bestandsmuster des Simulationsbereichs, wie
   schon in Paket 3. Die Umstellung auf `MyResource` gehört zu **Paket 9** und ist an
   jeder Stelle als solche kommentiert. Der technische Serienschlüssel ist genau deshalb
   vom Anzeigetext getrennt (13.3).

2. **`listView_SimWP` wird zur Laufzeit niedriger.** Die WP-Modul-Liste ist im Designer
   244 px hoch; für die Speichertabelle darunter wird sie programmatisch auf 134 px
   gesetzt. Das ist kein Designer-Eingriff und für die typischen ein bis drei Module
   reichlich, ändert aber das gewohnte Layout des Wärmepumpen-Tabs.

3. **Auswahlliste statt Ersatz der Checkbox.** Konzept 13.3 formuliert „statt einer
   Checkbox eine kleine Auswahlliste". Umgesetzt ist beides nebeneinander: die Checkbox
   bleibt Sammelschalter (bei genau einem Speicher unverändertes Verhalten), die
   Auswahlliste erscheint erst ab zwei Speichern. Das hält den Einspeicherfall exakt wie
   bisher und vermeidet eine Layoutänderung, wo sie keinen Nutzen hat.

4. **`textBox_Pufferspeicher` bleibt bestehen.** Sie wird nur ausgeblendet, wenn es
   mindestens einen Speicher gibt; ohne Speicher zeigt sie weiter den Legacy-Ausdruck
   `Volumen · 1,16`. Damit ändert sich für Projekte ohne Speicher optisch nichts — und der
   Designer bleibt unberührt.

5. **Erdreich-Ergebnisse nicht persistiert.** Siehe 6.5. Konzept 6.6 listet dafür keine
   Spalten; eine eigene Tabelle wäre eine Schemaänderung außerhalb dieses Pakets.

6. **Maximale Entzugsleistung im Mehrmodulfall genähert.** Siehe 6.2. Die Alternative
   wäre gewesen, den Fall gar nicht zu beliefern; die anteilige Verteilung ist im Ergebnis
   sichtbar gekennzeichnet und in beiden Anzeigen mit Vorbehalt versehen.

7. **`Sim.Speicher_Anzahl` und die Quellspeicher-CSVs im Referenzlauf.** Das
   Exportwerkzeug wurde erweitert, damit die neue Persistenz überhaupt regressionsfähig
   ist. Der Preis: eine zusätzliche Abweichung je Projekt gegenüber B0
   (`Sim.Speicher_Anzahl`, auch dort, wo es gar keinen Speicher gibt). Das ist der
   Übersichtlichkeit halber bewusst so — der Skalar dokumentiert, dass ein Projekt
   *keinen* Speicher hat, statt das nur aus fehlenden Einträgen ableiten zu lassen.

---

## 9. Offene Punkte

1. **ID-Ziel der Quellspeicherzeile.** `ID_Pufferspeicher` zeigt beim Quellspeicher auf
   `Tab_Pufferspeicher_STAMM`, beim Senkenspeicher auf die Projektkopie
   `Tab_Pufferspeicher` (Abschnitt 3.1). Mit Paket 4, wenn die Quellzuordnung über
   `WQ_ID_Puffer` auf die Projektkopie läuft, ist das auf die Projektkopie zu vereinheitlichen.
   Solange die Spalte keinen erzwungenen Fremdschlüssel trägt, ist der Zustand unkritisch,
   aber er ist eine Falle für spätere Auswertungen.

2. **Entzugs-Ganglinie je WP-Modul.** Erst damit ist die maximale Entzugsleistung im
   Mehrmodulfall exakt (6.2). Sie entsteht mit dem Engine-Umbau in **Paket 4**; danach
   entfällt die anteilige Näherung und der Fall „gemischte Quellen" ebenso.

3. **Erdreich-Ergebnisse überdauern die Sitzung nicht** (6.5). Wer den Ausweis dauerhaft
   will, braucht entweder Spalten in `Tab_ErgebnisWaermepumpeModul` (Entzugsarbeit,
   Spitzenleistung) oder eine eigene Ergebnistabelle — beides Schemaänderungen, die in die
   Migration gehören.

4. **`Verwendung` kennt nur zwei Werte.** „Heizung" und „Quelle" decken den heutigen
   Bestand ab (Konzept 6.6 nennt die Spalte als `TEXT(50)`). Mit Paket 5/6 kommen
   Solarthermie- und BHKW-Speicher dazu; dann ist der Wertevorrat zu erweitern —
   die Konstanten liegen dafür an einer Stelle in `SimulationPufferspeicher`.

5. **`Tab_ErgebnisPufferspeicher` wird von Bericht und Wirtschaftlichkeit noch nicht
   gelesen.** `ErgebnisCtrl.Load` liefert die Liste bereits; die Auswertung dort ist
   Sache der Berichtsbausteine (Konzept 13.3 nennt sie als Ziel: „identisch mit Bericht
   und Wirtschaftlichkeit"). Erst damit lässt sich die in 5.3 deklarierte Abweichung
   (Anzeige aus den Simulationsobjekten) gegen die Tabelle gegenprüfen.

6. **ID-Semantik des Quellspeichers** — siehe Punkt 1; mit Projekt 1021 ist der Fall
   jetzt in der Referenzmenge (`Pufferspeicher[0].ID_Pufferspeicher = 8` zeigt auf
   `Tab_Pufferspeicher_STAMM`, nicht auf die Projektkopie). Die Umstellung in Paket 4
   ändert diesen Wert und ist damit eine bewusst zu erwartende Regressionsabweichung.

7. **`ZeitreihenExtraktor` kennt nur `puffer_wp`.** Der Bericht zeigt weiterhin nur die
   SOC-Ganglinie des Senkenspeichers (`Allgemein/Bericht/ZeitreihenExtraktor.cs:64-65`).
   Sobald das Berichtsmodul mehrere Speicher darstellen soll, ist dort auf
   `sim.AlleSpeicher()` umzustellen — dieselbe Quelle, die Navigator, CSV-Export,
   Detailansicht und Persistenz schon benutzen. Bewusst **nicht** in diesem Paket
   geändert: `ZeitreihenSatz` trägt genau einen Speicherkanal, mehrere Kanäle sind eine
   Schnittstellenänderung im Berichtsmodul.

6. **Kein UI-Test.** Die Anzeigen sind programmatisch aufgebaut und über den Build
   abgesichert, aber nicht in der laufenden Anwendung gesichtet — der Referenzlauf ist
   headless. Layout und Lesbarkeit von Speichertabelle, Auswahlliste, Erdreich-Zeile und
   des gewachsenen Erdreichdialogs (Höhe 690 → 718 px wegen des Spreizungsfelds) sind an
   einem echten Projekt nachzusehen.

7. **Bericht zeigt weiter nur den Senkenspeicher.** `Allgemein/Bericht/ZeitreihenExtraktor.cs`
   füllt `ZeitreihenSatz.PUFFER_SOC` ausschließlich aus `sim.puffer_wp`; Quellspeicher
   fehlen im Bericht. Anzeige, CSV-Export und Persistenz zeigen sie seit Paket 7, der
   Bericht nicht. → offener Punkt 7.

8. **Erdreich-Ergebnisse im Referenzlauf, nicht in der Datenbank.** Die Größen der
   Auslegungsprüfung stehen jetzt als `Erdreich[i].*` in `aggregate.csv`, damit sie
   regressionsfähig sind — persistiert werden sie weiterhin nicht (5. oben, 6.5). Ein
   Projekt ohne `WQ_Typ = 'Erdreich'` erzeugt **keinen** dieser Einträge; die
   Referenzmenge bleibt davon unberührt.

---

## 10. Reproduktion

```powershell
# 1. Build
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj -p:Configuration=Debug -p:Platform=x86

$exe = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"

# 2. Eigene, migrierte Kopie AUSSERHALB des Repos
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\Paket7_Nach\DB_Basis

# 3. Auswahl kontrollieren (liest die Kopie, kopiert nichts)
& $exe liste C:\Waermeplan\Paket7_Nach\DB_Basis

# 4. Die NEUN Referenzprojekte im Modus "projekt".
#    "projekt" migriert NICHT - Schritt 2 ist Voraussetzung.
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket7_Nach\Lauf9\Projekt_$id" C:\Waermeplan\Paket7_Nach\DB_Basis
}

# 5. Regression gegen B0 - nur die acht gemeinsamen Projekte, 1021 gibt es dort nicht
$acht = "C:\Waermeplan\Paket7_Nach\Lauf8"
New-Item -ItemType Directory -Force $acht | Out-Null
foreach ($id in 1007,1008,1010,1011,1017,1018,1023,1024) {
    Copy-Item -Recurse "C:\Waermeplan\Paket7_Nach\Lauf9\Projekt_$id" $acht
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_B0 $acht     # erwartet: 42 Abweichungen

# 6. Neue Basis pruefen und Selbstvergleich (zweiter Lauf derselben neun Projekte)
& $exe pruefen   C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket7
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket7  <zweiter_Lauf>
```

Alternativ der bequeme Weg, seit der Nacharbeit gleichwertig (er migriert selbst, benutzt
aber `Referenzlaeufe\Arbeitskopie` im Repo):

```powershell
& $exe lauf --ziel D:\Temp\NachUmbau --projekte 1007,1008,1010,1011,1017,1018,1021,1023,1024
```

---

## 11. Review-Nacharbeit (14.08.2026)

Zwei Reviews haben das nicht committete Paket geprüft. Die Befunde sind vollständig
eingearbeitet; jeder Punkt hat oben seine Stelle. Diese Übersicht dient dem Wiederfinden.

| # | Befund | Umsetzung | Nachweis |
|---|---|---|---|
| 1 | Waisen in `Tab_ErgebnisPufferspeicher` | `Delete(int)` löscht die Pufferzeilen defensiv mit; `Save` räumt zusätzlich **alle** Zeilen ohne gültigen Kopf ab | Reproduktion in 11.1 |
| 2 | `Load`-Guard lief ins Leere | `GetDataTable` wirft nicht, sondern zeigt eine MessageBox — ersetzt durch den stillen Direktzugriff `ErgebnisCtrl.PufferZeilenLesenStill`, auch im `Referenzlauf/Ergebnisexport` | Bau + headless-Lauf ohne Dialogmeldung |
| 3 | Veraltete Quellspeicher im Folgelauf | `SimulationWaermepumpe.Init()` leert `wp_quellspeicher`/`wp_quelltemp`/`wp_typ`; `Init()` läuft in `Do_Simulation` **unbedingt**. Zusätzlich werden Speichertabelle und Erdreichzeile der Detailansicht außerhalb von `if (bSimulationWP)` gefüllt | 5.3 |
| 4 | `StellePufferTabelleSicher` prüfte nur die Tabelle | Index `idx_ErgPuffer` und Beziehung über `ID_Ergebnis` werden auch auf vorhandener Tabelle nachgezogen; vor `ADD CONSTRAINT` werden Waisen entfernt (Access weist die Beziehung sonst zurück) | 11.1 |
| 5 | Quellspeicher-Ganglinie brach ab, wenn die WP wegen des Senkenspeichers aussetzte | `StundeAbschliessen` wird im `!wpEinsatz`-Zweig für alle Quellspeicher gerufen | 11.2 |
| 6 | Basisbruch Jahresentzug ↔ Spitze | alle Größen aus der globalen Entzugsganglinie; „inkl. Speicherladung" im Kurztext | 6.1, 11.3 |
| 7 | Erdreich-Prüfung bei Luft-Wasser | wird nicht mehr geprüft, stattdessen Unwirksamkeitshinweis | 6.2a, 11.3 |
| 8 | Frostbedingung / `WQ_Spreizung` | nur Betriebsstunden, Schwelle 5 % davon, Normbasis im Text; Eingabefeld im Erdreichdialog | 6.3, 11.3 |
| 9 | `comboBox_Puffer` außerhalb des Sichtbereichs, `SpeicherSichtbar` an `Visible` gekoppelt | Liste in die freie zweite Checkbox-Zeile; Kriterium ist `speicherListe.Count > 1` | 5.1 |
| 10 | `label_Erdreich` schnitt Warnungen ab | Höhe wird über `TextRenderer.MeasureText` an den Umbruch angepasst | 5.3 |
| 11 | Vollzyklen, CSV-Köpfe, dreifacher „Speicher"-Fallback | Vollzyklen rollenabhängig; Trennzeichen und Dubletten zentral in `CsvExportClass`; `SimulationPufferspeicher.BezeichnerAnzeige()`/`Anzeige()` | 5.2, `lauf_protokoll.md` |
| 12 | Referenzmenge ohne Quellspeicher, Reproduktionsweg falsch | 1021 über neue Pflichtkategorie, Basis mit 9 Projekten neu eingefroren, `lauf` migriert, `liste <dbOrdner>`, LIESMICH überarbeitet | F3 |
| 13 | Protokollaussagen | dieses Dokument | — |

### 11.1 Waisen-Reproduktion

Eigene Kopie `C:\Waermeplan\Paket7_Nach\DB_Waisen2`, Beziehung entfernt
(`ALTER TABLE Tab_ErgebnisPufferspeicher DROP CONSTRAINT FK_ErgPuffer`), dann eine Waise
angelegt, die auf die **nächste** Kopf-ID zeigt — genau der Wiederverwendungsfall:

```
vor dem Lauf:
  Waise  ID=9999  ID_Ergebnis=174  "WAISE aus geloeschtem Lauf 1008"  Q_max=6,96
  naechste Kopf-ID (MAX+1)          174

Lauf von Projekt 1010 (hat KEINEN Speicher):
  Kopf von 1010                     ID=174        <- ID wiederverwendet
  Speicherzeilen dieses Kopfes      (keine)       <- Waise nicht mehr zugeordnet
  Waisen gesamt                     0
  Beziehung                         FK_ErgPuffer, DELETE=CASCADE   <- nachgezogen
  uebrige Zeilen                    1008/166, 1021/171, 1023/172 unveraendert
```

Zweite Probe für den Index: `DROP INDEX idx_ErgPuffer`, ein Lauf — Index ist wieder da
(`FK_ErgPuffer, idx_ErgPuffer, <PK>`).

### 11.2 Ergebniswirkung von Fix 5

`SimulationWaermepumpe` verließ die Modulschleife per `break`, sobald der **Senken**speicher
den Stundenbedarf allein deckte. Damit unterblieb `StundeAbschliessen()` der
**Quell**speicher: Bereitschaftsverluste wurden nicht verrechnet und — schwerer — die
Ganglinie `SOC_stuendlich` blieb in diesen Stunden auf 0 stehen, obwohl der Speicher
gefüllt war. `SOC_Mittel` und `SOC_Max` fielen dadurch **systematisch zu niedrig** aus,
und zwar in Anzeige, CSV-Export und `Tab_ErgebnisPufferspeicher` gleichermaßen.

**Wirkungsbereich:** ausschließlich Projekte, die **beides** haben — einen Senkenspeicher
der Wärmepumpe *und* mindestens einen Quellspeicher. Ohne Senkenspeicher wird `wpEinsatz`
nie `false`. In der Referenzmenge trifft das auf **kein** Projekt zu: 1008 und 1023 haben
nur einen Senkenspeicher, 1021 nur einen Quellspeicher
(`Sim.PufferWP_vorhanden = False`). Die Regression gegen B0 ist deshalb unberührt — was
sie auch zeigt.

### 11.3 Erdreich-Funktionsnachweis

Die Referenzprojekte haben keine Erdreichquelle. Für den Nachweis wurden auf **eigenen
Kopien** (`DB_Erdreich`, `DB_ErdreichT3`) Erdreich-Konfigurationen gesetzt; die
Ergebnisgrößen stehen seit der Nacharbeit als `Erdreich[i].*` in `aggregate.csv`.

| Fall | Aufbau | Ergebnis |
|---|---|---|
| **1010** — Luft-Wasser | ein Modul, `Tab_WP.Typ = 'Luft-Wasser'`, Kollektor 250 m² | `Unwirksam = True`, `Pruefung_Moeglich = False`, keine Zahlen — Hinweis auf die wirkungslose Konfiguration |
| **1008** — zwei Module | 10132 Sole-Wasser + 10133 Luft-Wasser, beide auf Erdreich | 10132: wirksam, aber `MaxEntzugBelastbar = False` (gemischt, weil 10133 Luftwärme in die Ganglinie speist); 10133: `Unwirksam = True`. Betriebsstunden 5 188, Frost 1 161 h → Warnung |
| **1024** — gemischt | 11208 Sole-Wasser Sonde 2 × 90 m, 11207 Luft-Wasser ohne Erdreich | `MaxEntzugBelastbar = False` (nicht je Modul trennbar), Betriebsstunden 7 882, Frost 0 h |
| **1008 (T3)** — ein wirksames Modul + Senkenspeicher | wie 1008, Luft-Wasser-Modul entfernt | eindeutig und exakt: **Entzug 60 278,77 kWh/a**, **Spitze 18 196,57 W**, **Volllaststunden 3 312,65 h**, Betriebsstunden 5 199, Frost 1 165 h → Warnung, `InklSpeicherladung = True` |

Gegenrechnung zu T3 direkt aus den Ganglinien-CSVs des Laufs:

```
Summe(wp_produktion - wp_strom > 0)  =  60 278,7745 kWh   -> Erdreich[0].JahresentzugKWh
Max(wp_produktion - wp_strom)        =  18,196565 kW      -> Erdreich[0].MaxEntzugW
Stunden mit wp_produktion > 0        =   5 199            -> Erdreich[0].BetriebsStunden
Arbeit / Spitze                      =   3 312,646 h      -> Erdreich[0].VolllastStunden
```

Mit gesetzter Klimazone (Testwert 12 auf der Kopie) liefert die Prüfung ein Urteil:
`Pruefung_Moeglich = True`, `Pruefung_Warnung = True` — ein 300-m²-Kollektor trägt
60 MWh/a bei 18,2 kW Spitze nicht. Ohne Klimazone meldet `VDI4640Pruefung` wie bisher
„Klimazone nicht zugeordnet, Prüfung nicht möglich" (Datenlage, kein Programmfehler:
in den Projektdaten ist `Tab_Klimaregion.Klimazone_DIN4710` durchweg leer).

### 11.4 Kodierung und Diff

Alle bearbeiteten Dateien behalten Kodierung und Zeilenendenstil des Vorgefundenen:
UTF-8 mit BOM/CRLF bei `ErgebnisCtrl.cs`, `SimulationWaermepumpe.cs`,
`ErdreichAuswertung.cs`, `NavigatorWaerme.cs`, `Form_Simulation_Detail.cs`; UTF-8 ohne BOM
und mit LF bei `SimulationPufferspeicher.cs`, `CsvExportClass.cs`; UTF-8 mit BOM und LF bei
`Form_QuelleErdreich.cs`, `Form_Simulation_Config.cs`, `Referenzlauf/Ergebnisexport.cs`,
`Referenzlauf/Projektauswahl.cs`, `Referenzlauf/Program.cs`. Der `git diff` der von Paket 7
berührten Dateien enthält **null** Ersatzzeichen; die Ersatzzeichen im Gesamt-Diff stammen
weiterhin aus `Views/BHKW/Form_BHKWEing.cs` (Parallelarbeit, nicht angefasst).
