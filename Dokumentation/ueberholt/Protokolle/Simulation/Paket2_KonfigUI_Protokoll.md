# Paket 2 — Konfigurations-UI (Quellen/Senken-Modell)

Umsetzungsprotokoll zu Kapitel 4 des
[Simulationskonzepts](Konzept_Simulation_QuellenSenken.md), Stand **14.08.2026**,
**Review-Nacharbeit eingearbeitet** (siehe [§14](#14-review-nacharbeit)).
Grundlage: 4.1 (Erzeuger-Übersicht, Layoutzwang, zwingende Vorarbeit), 4.2 (`Form_Waermesenke`),
4.3 (Projekt-Puffer-Verwaltung), 4.4 Etappe A (Rubrik-Entfall), 4.6 (Validierung),
5.2 (Dedup-Aufhebung) sowie 3.4/3.5/3.6 (Ladeprioritäten, PV-Sonderregel, Entladereihenfolge).

Voraussetzung ist [Paket 1](Paket1_SchemaMigration_Protokoll.md): die Spalten `WS_*`, `WQ_ID_Puffer`
und die sieben Puffer-Spalten existieren seit der Migration, die vier Anlagen-Referenzen auf
`Tab_Pufferspeicher.ID` sind erzwungene Beziehungen ohne Löschweitergabe.

**Nicht committet** — Abnahme steht aus.

---

## 1. Umfang

### 1.1 Neue Dateien

Zeilenzahlen nach der Review-Nacharbeit (Stand des Arbeitsverzeichnisses, `wc -l`).

| Datei | Zeilen | Inhalt |
|---|---:|---|
| `Allgemein/Simulation/StilleDb.cs` | 137 | Dialogfreier DB-Zugriff (Scalar/Tabelle/NonQuery) für den neuen Code (13.4) |
| `Allgemein/Simulation/Ladeordnung.cs` | 559 | Ladepriorität, Ladeobergrenzen, Entladereihenfolge (3.4/3.5/3.6) |
| `Allgemein/Simulation/WaermesenkeClass.cs` | 738 | Senkenfelder lesen/schreiben/prüfen (4.6), Anzeigetexte, Projekt-Puffer-Liste, Übergangsbrücke |
| `Views/Simulation/Form_Waermesenke.cs` | 788 | Senkendialog nach Mockup 4.2, programmatisch |
| `Views/Pufferspeicher/Form_PufferSp_Projekt.cs` | 1000 | Projekt-Puffer-Verwaltung nach Mockup 4.3, programmatisch |
| `Views/Simulation/Form_Simulation_Config.Uebersicht.cs` | 1256 | Ausgelagerter Übersichts-/Layoutcode inkl. Spaltenkonstanten und Spaltenbreiten |

### 1.2 Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Views/Simulation/Form_Simulation_Config.cs` | 2072 → 1256 Zeilen; Übersichtscode ausgelagert; Rubrik auf `Visible = false` (Etappe A); `ZuordnungenLaden()` aus `SetControls` herausgelöst; Fußzeilen-Aufruf |
| `Controller/PufferSpCtrl.cs` | `GetProjektId` auf `MIN(ID)` determiniert; neu: `CopyFromStammNeu`, `ProjektPufferAnlegen`, `ProjektPufferAendern`, `EindeutigerBezeichner`, `ReferenzenAufPuffer`, `ProjektPufferEntfernen` |
| `Allgemein/Update/ProjektPuffer.cs` | `VERWENDUNG_BRAUCHWASSER`, Schwellen-Defaults, `SQL_PUFFER_INSERT_VOLL`/`…UPDATE_VOLL` samt Parameterbauern |
| `Allgemein/Simulation/WaermequelleClass.cs` | `WertLesenStill(int,string)`, `TabelleStill(…)`; `Quelltemperatur` und `Quellspeicher` lesen im Engine-Pfad still (13.4) |

**Gesperrte Dateien unberührt.** `WizardCtrl.cs`, `WErzeugerModel.cs`, `Form_BHKWEing.cs`,
`WizardParent.cs`, `Form_Heizkessel*.cs` und `RecordSet.cs` stehen zwar im `git status`, ihre
Zeitstempel liegen aber sämtlich vor Beginn dieser Sitzung (09:20–14:05 gegenüber 16:0x) — die
Änderungen stammen aus der Parallelarbeit.

Keine `.Designer.cs` und keine `.resx` angefasst; alles Neue ist programmatisch nach dem
Bestandsmuster `Form_QuellePufferspeicher` / `Form_QuelleErdreich` aufgebaut.

---

## 2. Vorarbeit: Spaltenkonstanten und Whitelist (4.1)

Die Spaltenindizes der Übersicht standen an drei Stellen doppelt — `Columns.Add`, Tooltip-`switch`
und Doppelklick-Dispatcher. Mit zwei zusätzlichen Spalten wäre daraus eine stille Fehlbedienung
geworden. Jetzt gibt es die Wahrheit genau einmal, in
`Form_Simulation_Config.Uebersicht.cs`:

```
COL_PRIO = 0, COL_ERZEUGER = 1, COL_ANLAGE = 2, COL_WPPRIO = 3, COL_QUELLE = 4,
COL_SENKE = 5, COL_ZWEITSENKE = 6, COL_BETRIEBSMODUS = 7, COL_PUFFER = 8
```

Alle drei Stellen sind umgestellt. Das `else`-Fallback `int spalte = 4` ist durch die Whitelist
`SPALTEN_MIT_DIALOG` ersetzt: Ein Doppelklick öffnet nur noch, was dort steht — sonst passiert
nichts. Das war zwingend, weil seit 4.1 **jede** Erzeugerzeile ein `Tag` trägt; vorher hätte ein
Doppelklick auf die Bezeichnerspalte eines Heizkessels den Wärmequellen-Dialog geöffnet.

`COL_ZWEITSENKE` ist zwischen Senke und Betriebsmodus eingefügt, nicht angehängt — die
Reihenfolge folgt dem Mockup 4.1. Das ist gefahrlos, weil die Indizes ab sofort ausschließlich über
die Konstanten laufen.

---

## 3. Übersicht für alle Erzeuger (4.1)

- `istWP`-Filter entfallen; `zeile.Tag = anlagen[a]` für **alle** Erzeugerzeilen.
- `AnlagenImProjekt` liefert zusätzlich `ID_Type` und liest `WS_Ziel`, `WS_ID_Puffer`,
  `WS_Ladeprio`, `WS_Ladegrenze`, `WS_Ladeprio_PV`, `WS_Ziel2`, `WS_ID_Puffer2`, `WS_Ladeprio2`,
  `WS_Ladegrenze2` in **derselben** Abfrage mit (`WaermesenkeClass.AusDatenzeile`) — sonst käme je
  Zeile eine zweite Abfrage dazu.
- Neue Spalte **„Wärmesenke (*)"** ersetzt inhaltlich die alte, rein WP-spezifische Senkenspalte:
  sie zeigt `Heizkreis (beides)` bzw. `Puffer Heizung: PS 800`. Die alte Bedarfsart (`WS_Typ`)
  erscheint nur noch als Klammerzusatz beim Heizkreis — genau der Feinsteuerungs-Status, den 3.1
  ihr zuweist.
- Neue Spalte **„Zweitsenke (*)"**, „–" ohne Zweitsenke.
- WP-Prio, Wärmequelle und Betriebsmodus bleiben WP-spezifisch. Bei Nicht-WP-Zeilen erscheint ein
  Hinweis statt eines Dialogs (`BetriebsmodusBearbeiten`, `WaermequelleBearbeiten`,
  `WpPrioritaetBearbeiten`).

**Betriebsmodus gesperrt statt umgetextet.** Konzept 4.1 lässt beides offen. Gewählt wurde die
Sperre, weil die Engine `BM_Typ` ausschließlich in `SimulationWaermepumpe` auswertet — ein Modus,
den für Kessel und BHKW niemand liest, wäre eine Zusage ohne Wirkung.

---

## 4. Rubrik-Entfall, Etappe A (4.4)

`InitPufferspeicherRubrik` legt Label, `comboBox_Puffer1/2` und `checkBox_Puffer1/2` weiterhin an,
setzt sie aber zusammen mit `groupBox_PufferSp` (und damit `listView1`, `btn_Hinzu`,
`btn_Loeschen`) auf `Visible = false`. Gesteuert über die Konstante `RUBRIK_SICHTBAR = false` —
auf `true` gesetzt kommt die alte Bedienung unverändert zurück.

`AktualisierePufferSpSichtbarkeit()` steigt bei ausgeblendeter Rubrik sofort aus; sonst hätte die
Vorbelegung aus `SetControls` die Zuordnungstabelle wieder auf den Schirm geholt.

**`_zuordnungen` läuft unverändert weiter.** Der Bestand wird beim Laden wie bisher aus der
Datenbank gefüllt (jetzt in `ZuordnungenLaden()`), und `btn_Speichern_Click` schreibt ihn
unverändert über den bestehenden Delete/Insert-Zyklus zurück — **einschließlich** der
B0-1-Schwellensicherung, der B0-11-Mapping-Liste und der Etappe-4-Fixes (führende Puffer-Ablage,
`TryParse` statt `Int32.Parse`, `IstTemperaturpaar`, `TemperaturenLoeschen`). An dieser Kernlogik
wurde **nichts** geändert; ergänzt wurde nur der zweite Aufrufer von `ZuordnungenLaden()`.

### Layout

Die Höhe der Übersicht hängt laut 4.1 (Fassung 12) an `groupBox_PufferSp.Top`:

```csharp
groupBox_Uebersicht.Size = new Size(groupBox_PufferSp.Width, groupBox_PufferSp.Top - 109 - 10);
```

Diese Formel bleibt unangetastet. Stattdessen wird die unsichtbare Gruppe an den unteren Rand
geschoben:

```csharp
groupBox_PufferSp.Location = new Point(groupBox_PufferSp.Left, btn_Speichern.Top - PLATZ_FUSSZEILE);
```

Damit wächst die Übersicht von **118 px** (3–4 Zeilen, 8 Spalten) auf **309 px** — ohne eine zweite
Wahrheit über die Höhe. Der freiwerdende Streifen darunter (`PLATZ_FUSSZEILE = 62`) trägt die
Fußzeile aus dem Mockup: die Aufzählung der Projekt-Puffer und die Schaltfläche
**„Pufferspeicher anlegen / verwalten…"**. Ohne diesen Einstieg wäre nach dem Entfall der Rubrik
kein Pufferspeicher mehr anzulegen.

#### Breite (zweiter Layoutzwang aus 4.1)

Neun Spalten passten nicht in die 505 px breite Gruppe. Ursache war nicht die Menge allein,
sondern das Verfahren: `AutoResizeColumns(ColumnContent)` gefolgt von
`AutoResizeColumns(HeaderSize)` — die zweite Zeile überschreibt die erste vollständig, die
Breiten hingen also **allein an der Länge der Kopftexte**. „Wärmeerzeuger", „Anlage(n) im
Projekt" … „Zuordnung (Altmodell)" ergaben rund 910 px in einer 491 px breiten Liste: ein
waagerechter Rollbalken bei jedem Öffnen, und die inhaltlich wichtigen Spalten waren die
schmalsten.

Jetzt gilt:

- **Kompakte Kopftexte:** Prio · Erzeuger · Anlage · WP-Prio · Quelle · Senke · Zweitsenke ·
  Modus · Zuordnung (alt). Was die Spalte bedeutet und dass sie per Doppelklick bearbeitbar
  ist, trägt der Mouseover-Hinweis — der Kopf muss es nicht mehr mittragen. Das `(*)` entfällt.
- **Feste Breiten** in `SPALTEN_BREITEN` (40/84/140/62/100/120/100/92/112 = **850 px**), kein
  Autosize mehr.
- **Verbreiterung im Code-Behind** (`UebersichtBreiteAnpassen`): Clientbreite **791 → 1169 px**,
  `groupBox_PufferSp` (und damit die Übersicht) 505 → 883 px, die drei Fußzeilen-Steuerelemente
  ziehen mit. Gekappt am Arbeitsbereich des Schirms — passt das nicht, wird nur so weit
  verbreitert wie möglich und der Rollbalken kommt für die letzten Spalten zurück. Das
  Formular ist in der Größe veränderbar, der Anwender kann nachhelfen.
- Der Zuschlag für Rahmen und senkrechte Bildlaufleiste kommt aus
  `SystemInformation.VerticalScrollBarWidth`, nicht aus einer festgeschriebenen 17.

Gemessen (Probe i, §9.6): ListView 869 px, Spaltensumme 850 px, **6 px Rest — kein
waagerechter Rollbalken**. Kein Designer und keine `.resx` angefasst; verschoben wird
ausschließlich im Code-Behind, wie es `InitPufferspeicherRubrik` mit der Höhe schon tut.

Ehrlich dazu: Lange Anlagennamen (z. B. „CS3400i AWS 10 E + CS3400i AWS 4 OR-S") passen nicht
in 140 px und werden von der ListView mit „…" gekürzt. Das ist die bewusste Wahl —
Rollbalken weg, dafür Kürzung in der einen Spalte, deren Inhalt am wenigsten
entscheidungsrelevant ist.

Die Spalte `COL_PUFFER` bleibt erhalten, umbenannt in **„Zuordnung (alt)"**. Begründung: Sie
ist die einzige Anzeige dessen, was die Engine bis Paket 4 tatsächlich auswertet, und ihr
Doppelklick ist der einzige Weg zu `SpeicherregelungBearbeiten` (Schwelle_Ein/Aus der Zuordnung).
Beides ersatzlos zu streichen wäre in Etappe A ein Funktionsverlust. Mit Etappe B fällt die Spalte
mit dem übrigen Rubrik-Code weg.

---

## 5. Übergangsbrücke auf `Z_ProjektPufferSp` — **entfällt mit Paket 4**

### Warum sie nötig ist

`SimulationControl.Do_Simulation` (`:104-161`) holt den Wärmepumpen-Pufferspeicher aus
`Z_ProjektPufferSp`: erste Zeile mit `Erzeuger = 'Wärmepumpe'` nach `Prioritaet`. Die neuen
`WS_*`-Spalten liest die Engine noch nicht — das ist Paket 4. Ohne Brücke bliebe jede im
Senkendialog gesetzte Puffer-Senke **wirkungslos**, und der Anwender sähe eine Einstellung, die im
Ergebnis nicht ankommt.

### Regel

`WaermesenkeClass.WpSenkeSpiegeln(idProjekt)`:

| Zustand nach der Bedienung | Wirkung auf `Z_ProjektPufferSp` |
|---|---|
| Eine WP hat `WS_Ziel = 'PufferHeizung'` mit `WS_ID_Puffer` | genau **eine** Zeile `Erzeuger = 'Wärmepumpe'` auf diesen Puffer; alle WP-Zeilen auf andere Puffer entfallen |
| Zeile auf denselben Puffer existiert bereits | bleibt bestehen (samt `Schwelle_Ein`/`Schwelle_Aus`), nur `Pufferspeicher` wird nachgeführt — `Vorlauf`/`Ruecklauf` **nur, wenn der Puffer ein gültiges Temperaturpaar trägt** (siehe unten) |
| Eine WP hat `WS_Ziel = 'PufferBrauchwasser'` | wie „keine Puffer-Senke": **alle** WP-Zuordnungszeilen entfallen. Die Alt-Engine kennt nur den Heizungs-Puffer; ein Brauchwasserspeicher hat dort keine Entsprechung. Der Senkendialog sagt das beim OK ausdrücklich (§6) |
| Keine WP hat eine Puffer-Senke (z. B. zurück auf `Heizkreis`) | **alle** WP-Zuordnungszeilen des Projekts entfallen |

Auswahl der maßgeblichen Wärmepumpe: `ORDER BY Prioritaet, ID` — dieselbe Reihenfolge, mit der die
Engine die Zuordnung wählt und mit der Migrationsregel R1 gearbeitet hat.

### Temperaturnachführung nur bei gültigem Paar

Der Puffer ist seit Etappe 4 die **führende Ablage** der Betriebstemperaturen, die Zuordnung die
Rückfallstufe 2. `WaermesenkeClass.PufferInfo` liefert 0/0, wenn am Puffer kein brauchbares Paar
steht (beide Werte gesetzt, Rücklauf > 0, Vorlauf > Rücklauf). Diese 0/0 in die Zuordnung zu
schreiben **löschte die Rückfallstufe** — die Engine fiele auf ihre Vorgabespreizung von 10 K durch,
obwohl in der Zuordnung ein gepflegtes Paar stand. Der UPDATE-Zweig prüft deshalb
`ProjektPuffer.IstTemperaturpaar` und lässt die Zuordnungswerte sonst unangetastet; geführt wird nur
der Name. Beim INSERT gibt es keinen Bestand zu schonen — dort ist 0/0 die richtige Aussage
„hier steht nichts". Nachgewiesen in Probe f (§9.6).

### Was die Brücke NICHT kann (bekannt, bleibt bis Paket 4)

- **Mehrere Wärmepumpen mit verschiedenen Speichern.** Die Brücke bildet genau **eine** WP-Zeile ab,
  die der führenden Wärmepumpe. Setzt der Anwender an einer zweiten WP einen anderen Puffer als
  Senke, wird das gespeichert und angezeigt, kommt in der Alt-Zuordnung aber nicht an — die Engine
  kennt bis Paket 4 ohnehin nur einen WP-Speicher.
- **Schwellenverlust bei A → B → A.** Der Wechsel auf einen anderen Speicher löscht die Zeile von A
  samt ihren `Schwelle_Ein`/`Schwelle_Aus`. Kehrt der Anwender später zu A zurück, entsteht eine
  **neue** Zeile mit den Vorgaben 10/95 % — die früher an A gepflegten Schwellen sind weg. Innerhalb
  eines Wechsels ist das gewollt (die Schwellen gehören zur Paarung Erzeuger↔Speicher); über zwei
  Wechsel hinweg ist es ein stiller Verlust. Mit Etappe B fällt die Doppelpflege der Schwellen
  ohnehin weg (§12.5).
- **Kein gemeinsamer Schreibvorgang.** Die Brücke setzt bis zu drei Anweisungen ohne Transaktion ab
  (DELETE, COUNT, UPDATE/INSERT); `WaermesenkeClass.Schreiben` davor sind **zehn** einzelne
  `UPDATE`-Anweisungen auf je eigener Verbindung. Bricht etwas in der Mitte ab, steht ein halb
  geschriebener Datensatz. Das Muster ist Bestand im ganzen Projekt (`DataRepository` kennt keine
  Transaktionsklammer); es hier allein umzustellen hätte einen eigenen Baustein gebraucht.

Zeilen anderer Erzeuger bleiben unberührt. Die Engine überspringt sie ohnehin (`continue`), und
Konzept 5.5/R2 hält fest, dass wirkungslose Altzuordnungen wirkungslos bleiben.

Beim Wechsel auf einen **anderen** Puffer werden die Schwellen bewusst **nicht** mitgenommen: Sie
gehören zur Paarung Erzeuger↔Speicher; ein neuer Speicher startet auf den Vorgaben 10/95 %. Beim
erneuten Wählen desselben Speichers bleiben sie erhalten.

### Aufrufkreis

Ausschließlich aus Bedienhandlungen: `Form_Simulation_Config.WaermesenkeBearbeiten` ruft nach
erfolgreichem `WaermesenkeClass.Schreiben` die Brücke und lädt anschließend `_zuordnungen` neu.
Das Neuladen ist zwingend — sonst schriebe der Delete/Insert-Zyklus beim nächsten „Speichern" den
gerade erzeugten Stand wieder weg.

**Aus dem Rechenlauf heraus wird die Brücke nie gerufen.** Deshalb ist die Regression unberührt
(siehe 9.2).

**Entfall:** Mit Paket 4 liest die Engine `WS_Ziel`/`WS_ID_Puffer` direkt. Dann entfallen
`WpSenkeSpiegeln`, der Aufruf in `WaermesenkeBearbeiten` und — mit Etappe B — auch
`_zuordnungen` und `Z_ProjektPufferSp` als Schreibziel.

---

## 6. Senkendialog `Form_Waermesenke` (4.2)

Programmatisch, kein Designer, keine `.resx`; Klasse erbt direkt von `Form`, Datenübergabe über
öffentliche Felder, Validierung im OK-Klick mit `DialogResult.None` — Vorbild
`Form_QuellePufferspeicher.cs:18, 20, 251-285`.

- **Hauptsenke** als drei Radiobuttons: Heizkreis (mit `WS_Typ`-Dropdown Beides/Warmwasser/Heizung,
  nur hier aktiv), Pufferspeicher Heizung, Pufferspeicher Brauchwasser.
- **Puffer-Dropdowns** listen ausschließlich **Projekt-Puffer passender Verwendung**
  (`WaermesenkeClass.ProjektPufferListe`).
- **Ladepriorität**: „nach Vorgabe (20 – Wärmepumpe)" oder manuell 1–99, dazu die Anzeige
  **„Lädt als n. von m / bis x %"** aus `Ladeordnung.LadereihenfolgeVorschau` — sie rechnet mit der
  gerade gewählten Priorität, nicht mit dem zuletzt gespeicherten Stand.
- **Ladeobergrenze**: Checkbox + Prozentfeld; leer bedeutet „Puffer-Regel gilt".
- **„Bei PV-Überschuss"**: nur sichtbar bei `BM_Typ = "PV"` (3.5).
- **Zweitsenken-Block** optional, mit eigenem Ziel, Puffer, Ladepriorität und Ladeobergrenze.
- **Hinweiszeile + „Pufferspeicher anlegen…"**, das die Verwaltung öffnet und die Dropdowns danach
  neu aufbaut — **unabhängig vom DialogResult**, weil die Verwaltung sofort schreibt. Die
  Verwendung wird mitgegeben, die Verwaltung stellt sich darauf ein (§7).
- **Übergangshinweis bei Brauchwasser-Senke.** Ist Haupt- oder Zweitsenke ein Brauchwasserspeicher,
  meldet der OK-Klick sichtbar: *„Die Brauchwasser-Senke wird erst mit dem Engine-Umbau (Paket 4)
  wirksam. Sie wird gespeichert und angezeigt, geht in die Simulation aber noch nicht ein."* Ist es
  die **Hauptsenke einer Wärmepumpe**, kommt der zweite Satz dazu: *„Die bisherige
  Pufferspeicher-Zuordnung dieser Wärmepumpe wird dabei entfernt; bis Paket 4 rechnet die
  Simulation dann ohne Speicher."* — genau das tut die Brücke (§5). Für Kessel, BHKW und
  Solarthermie fasst sie die Zuordnung nicht an, dort entfällt der Satz. Kanalwarnung (4.6) und
  Übergangshinweis erscheinen zusammen in **einer** Meldung.

Geschrieben wird über `WaermesenkeClass.Schreiben` → `WaermequelleClass.WertSchreiben`.
**Die drei ID-Spalten bekommen `NULL` statt 0** — 0 ist keine gültige Puffer-ID und verletzt die in
Schritt 4 der SchemaMigration angelegte erzwungene Beziehung.

---

## 7. Projekt-Puffer-Verwaltung `Form_PufferSp_Projekt` (4.3)

**Neubau**, kein Feldzusatz an `Form_PufferSp_Bearbeiten`: jene Maske arbeitet ausschließlich gegen
`Tab_Pufferspeicher_STAMM` und liest positionsbasiert `row[2]…row[6]`. Der Projektmodus liest
durchgehend über Spaltennamen.

- **Bestandsliste** aller Projekt-Puffer, Schaltflächen *Neuer Pufferspeicher*, *Entfernen*,
  *Katalog ansehen…* (letztere öffnet `Form_PufferSp_Admin` schreibgeschützt, wie im Bestand).
- **Katalogauswahl** aus `Tab_Pufferspeicher_STAMM` **oder** „(freie Eingabe)" mit Bezeichner,
  Volumen und Bereitschaftsverlusten.
- **Verwendung** ist Pflichtfeld (Heizung | Brauchwasser).
- **Vorlauf/Rücklauf** vorbelegt aus den Systemvorgaben (`PufferSpCtrl.SystemVorlauf` /
  `SystemRuecklauf`) und geprüft über `ProjektPuffer.TemperaturenPruefen`. Ein **leeres Paar** ist
  ausdrücklich zulässig — dann greift der Engine-Rückfall; ein halbes oder vertauschtes Paar wird
  abgewiesen. Live-Anzeige `Q_max`.
- **Schwellen** Ein/Aus (Default 10/95) und **Abschaltschwelle nachrangig** (Default = Abschalt­schwelle,
  also keine Reservezone — verhaltensneutral nach 3.4).
- **Entladepriorität**: „automatisch (n)" mit dem nach 3.6 errechneten Wert, oder manuell 1–99.
- **„Ladereihenfolge dieses Speichers"** als Tabelle (Position, Anlage, Erzeuger, Haupt-/Zweitsenke,
  Ladeprio inkl. „(manuell)", Obergrenze inkl. „(eigene)") aus `Ladeordnung.Ladereihenfolge`.
- **„Wird als n. von m Heizungsspeichern entladen"** aus `Ladeordnung.Entladereihenfolge` —
  Singular/Plural und Kanalname ausgeschrieben (`KanalSpeicherWort`).
- **Vorbelegung aus dem Absprung.** `SetControls` wählt den ersten Speicher der **übergebenen
  Verwendung** aus; gibt es keinen, springt der Dialog direkt in die Neuanlage mit dieser Verwendung.
  Wer aus einer Brauchwasser-Senke kam und noch keinen Brauchwasserspeicher hat, landet damit
  unmittelbar im richtigen Formular statt im ersten Heizungsspeicher der Liste.
  Der Einstieg über die **Fußzeile der Übersicht** übergibt bewusst *keine* Verwendung — dort will
  der Anwender den Bestand sehen, nicht einen Kanal; dann bleibt es beim ersten Speicher der Liste.
- **Rückfrage beim Verwendungswechsel.** Wird die Verwendung eines Speichers geändert, den eine
  Anlage bereits als Haupt-, Zweit- oder Quellsenke führt, kommt eine Ja/Nein-Rückfrage **mit der
  Liste der betroffenen Anlagen**: Nach dem Wechsel passt die Zuordnung nicht mehr zur Verwendung,
  der Senkendialog blockiert beim nächsten Öffnen mit „falsche Verwendung" (4.6). Die Rückfrage
  sitzt im Dialog, nicht in `PufferSpCtrl.ProjektPufferAendern` — die Ctrl-Bausteine aus Paket 2
  sind durchgehend dialogfrei (13.4), damit die headless laufenden Proben und der Referenzlauf sie
  benutzen können.

**Bewusste Abweichung vom Mockup:** Der Dialog hat kein „Abbrechen". Anlegen, Ändern und Entfernen
wirken sofort auf die Datenbank (das verlangt der Absprung aus dem Senkendialog); ein Abbrechen,
das nichts zurücknähme, wäre eine Zusage, die der Dialog nicht halten kann. Stattdessen:
*Übernehmen* und *Schließen*.

---

## 8. Dedup-Aufhebung (5.2) — expliziter Pfad gegen Altpfad

### Das Problem

Konzept 5.2 verlangt, dass die Dedup-Prüfung in `PufferSpCtrl.GetProjektId(Bezeichner, Projekt)`
entfällt, damit mehrere baugleiche Puffer je Projekt möglich werden (E7). Paket 1 hat das
ausdrücklich **nicht** umgesetzt, mit gutem Grund: `Z_ProjektPufferSpCtrl.Insert` ruft
`CopyFromStamm` **implizit** auf, und `btn_Speichern_Click` schreibt die Zuordnungen bei jedem
Speichern neu. Ohne Dedup entstünde bei **jedem Speichern** ein weiterer Duplikat-Puffer.

### Die Lösung: Aufhebung nur im expliziten Pfad

| Pfad | Methode | Verhalten |
|---|---|---|
| **implizit** (Altpfad) | `CopyFromStamm(stammId, idProjekt)` | unverändert: vorhandene Zeile wird wiederverwendet, keine Neuanlage |
| **explizit** (Verwaltung 4.3) | `CopyFromStammNeu(…)` / `ProjektPufferAnlegen(…)` | legt **immer** eine neue Projektzeile an |

Der Anwender löst die Neuanlage ausdrücklich aus; ein automatischer Speichervorgang tut es nie.
Damit ist E7 erfüllt, ohne die Duplikatflut aus Paket 1 zurückzuholen.

### Namensgleichheit: Suffix „ (2)"

Mehrere Zeilen gleichen **Namens** wären trotzdem gefährlich, weil eine Reihe von Altpfaden
weiterhin über den Bezeichner auflöst:

| Altpfad | löst auf gegen | Suffix hilft? |
|---|---|---|
| `PufferSpCtrl.GetProjektId(Bezeichner, Projekt)` | `Tab_Pufferspeicher` (Projekt) | **ja** |
| `Z_ProjektPufferSp.Pufferspeicher` (Textreferenz) | `Tab_Pufferspeicher` (Projekt) | **ja** |
| `PufferSpCtrl.ProjektWaisenEntfernen` (Puffer ↔ Anlagenzeile) | `Tab_Energieanlagen` (Projekt) | **ja** |
| `WaermequelleClass.Quellspeicher` | `Tab_Pufferspeicher_STAMM` | **nein** |

`PufferSpCtrl.EindeutigerBezeichner(idProjekt, wunsch, idAusnahme)` hängt bei Kollision „ (2)",
„ (3)" … an. Für die drei projektbezogenen Pfade löst das die Mehrdeutigkeit vollständig.

**Ausnahme `Quellspeicher`:** Der Quellspeicher wird über `WQ_Puffer` gegen den **Katalog**
`Tab_Pufferspeicher_STAMM` aufgelöst, nicht gegen die Projekttabelle. Ein Suffix „ (2)" existiert
dort nicht — es wirkt hier also nicht, kann aber auch nicht schaden: Der Quellendialog
(`Form_QuellePufferspeicher`) bietet ausschließlich Katalognamen an, ein Projekt-Puffer mit Suffix
lässt sich als Quelle gar nicht auswählen. Die Umstellung des Quellenpfads auf den Projekt-Puffer
(`WQ_ID_Puffer`) gehört zum Engine-Umbau in Paket 4 (siehe §9.4, Zeile „Quelle Pufferspeicher").

### `GetProjektId` determiniert

`SELECT ID` → **`SELECT MIN(ID)`**. Gäbe es doch einmal zwei gleichnamige Zeilen (Altbestand,
Handarbeit in Access), entschied bisher die Datenbankreihenfolge. `MIN(ID)` trifft dieselbe Zeile
wie die übrigen Altpfade (`PendelspeicherId`: `TOP 1 … ORDER BY ID`, Migration R6). Das war der
offene Review-Punkt aus Paket 1.

### Konsistenzregel

- **Anlegen** schreibt Puffer **und** Anlagenzeile (`ID_Type = 12`) über die Paket-1-Bausteine
  `ProjektPuffer.SQL_ANLAGENZEILE_INSERT` / `AnlagenzeileParameter`.
- **Umbenennen** zieht **beide** Textreferenzen nach: die Anlagenzeile (sonst räumte
  `ProjektWaisenEntfernen` sie ab) **und** `Z_ProjektPufferSp.Pufferspeicher`. Letzteres war im
  Review als Duplikat-Erzeuger nachgewiesen: Blieb dort der alte Name stehen, fand
  `Z_ProjektPufferSpCtrl.Insert` beim nächsten „Speichern" über `GetProjektId` nichts mehr und rief
  `CopyFromStamm` — das legte eine **zweite** Projektkopie unter dem alten Namen an. Schlüssel des
  Nachziehens ist `ID_Pufferspeicher`, nicht der Name: gleichnamige Zeilen anderer Speicher bleiben
  unangetastet. Nachgewiesen samt Gegenprobe in Probe g (§9.6).
- **Entfernen** blockiert mit Liste, solange eine Anlage den Puffer als Haupt-, Zweit- oder
  Quellsenke referenziert (`ReferenzenAufPuffer`). Ist er frei, räumt `ProjektPufferEntfernen`
  Alt-Zuordnungen, Anlagenzeile und Pufferzeile ab, ruft `ReferenzenLoesen` (die Beziehungen aus
  Schritt 4 sind restriktiv) und anschließend `ProjektWaisenEntfernen`.
  **Bekannt:** Geprüft werden nur die vier ID-Spalten in `Tab_Energieanlagen`, **nicht** eine
  vorhandene Zeile in `Z_ProjektPufferSp`. Ein Speicher, den nur noch die Alt-Zuordnung führt, wird
  also ohne Rückfrage entfernt und die Zuordnung stillschweigend mitgelöscht. Das ist bis Paket 4
  hinnehmbar — die Zuordnung ohne Pufferzeile wäre ohnehin wirkungslos (die Beziehung auf
  `Tab_Pufferspeicher.ID` ist erzwungen) —, gehört aber in die Liste der bekannten Punkte.

---

## 9. Verifikation

### 9.1 Bau

```
MSBuild ..\WP-Plan.sln -t:Rebuild -p:Configuration=Debug -p:Platform=x86  ->  0 Fehler, 6 Warnungen
MSBuild ..\Referenzlauf\Referenzlauf.csproj -t:Rebuild                    ->  0 Fehler
```

Vollständiger `Rebuild`, nicht inkrementell — sonst zeigt der Lauf schlicht keine Warnungen an,
weil nichts neu übersetzt wird.

**Vor der Nacharbeit waren es 7 Warnungen**, darunter **CS0162 „Unerreichbarer Code"** in
`Form_Simulation_Config.cs:492`. Ursache: `RUBRIK_SICHTBAR` war eine `const bool = false`, der
Compiler faltete `if (!RUBRIK_SICHTBAR) return;` weg und meldete den Rest von
`AktualisierePufferSpSichtbarkeit` als unerreichbar. Der Code **soll** stehen bleiben — er ist der
Rückweg für Etappe B —, also wurde der Schalter auf `static readonly` umgestellt: gleiche Wirkung,
keine Konstantenfaltung, keine Warnung. Die frühere Protokollzeile „keine neuen Warnungen" war
damit unzutreffend und ist jetzt wieder wahr.

Die verbleibenden **sechs** sind Bestand bzw. Parallelarbeit und stehen sämtlich außerhalb von
Paket 2:

| Warnung | Ort |
|---|---|
| CS0108 ×2 | `StromverbraucherStammCtrl.items`, `WErzeugerModel.ID_Projekt` |
| CS0109 ×2 | `KlimaregionStammCtrl.rows`, `KlimaregionStammCtrl.items` |
| CS1998, CS4014 | `MDIMainForm.cs:264` / `:275` |

### 9.2 Regression — **PASS**

Eigene, migrierte Kopie außerhalb des Repos (`C:\Waermeplan\Paket2_Nach\DB_Basis`), Modus
`migration` mit 0 Abweichungen im Schema-Nachweis. Danach alle neun Referenzprojekte im Modus
`projekt` und Vergleich gegen `Referenzlaeufe/2026-08-14_Paket7`:

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)
Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1024: PASS (22 Dateien, 236616 Werte)

GESAMT: PASS (2260923 Werte innerhalb der Toleranz)
```

Erwartungsgemäß: Paket 2 fasst keinen Rechenpfad an. Die einzige schreibende Neuerung im
Engine-Umfeld — die Übergangsbrücke — läuft ausschließlich aus Bedienhandlungen.

Nach der Review-Nacharbeit **erneut gerechnet und erneut PASS**, mit identischen Wertzahlen.

### 9.3 Datenpfad-Proben (headless)

Werkzeug `Paket2Proben.exe` außerhalb des Repos; es ruft die **echten** Klassen- und
Ctrl-Methoden der Anwendung (die internen über Reflection, wie es `Referenzlauf/DbUmgebung` mit
`Properties.Settings` vormacht) und arbeitet je Probe auf einer eigenen Datenbankkopie. Zwei
Riegel gegen die produktive Datei: Soll-/Ist-Vergleich des DB-Pfads und ausdrückliche Sperre auf
`%ProgramData%\EPOS_PLAN\Kenndaten.accdb`.

#### (a) Wärmesenke einer Wärmepumpe — **PASS**

Projekt 1010 (Wärmepumpe vorhanden, kein Pufferspeicher):

```
OK   Ausgangslage: kein Projekt-Puffer / keine WP-Zuordnung
OK   Puffer angelegt (800 l, 55/35, Heizung)         -> ID 1054164
OK   Validierung 4.6 laesst die gueltige Senke durch
OK   WS_Ziel = PufferHeizung, WS_ID_Puffer = 1054164
OK   WS_ID_Puffer2 ist NULL (nicht 0!), WS_Ziel2 leer
OK   Zuordnungszeile erzeugt (Bruecke)
OK   Zuordnung fuehrt Vorlauf/Ruecklauf des Puffers (55/35)
```

Simulationslauf danach:

```
Sim.PufferWP_vorhanden;True
Puffer.Q_max;18.56                 (= 800 l x 1,16 Wh/(l K) x 20 K / 1000)
Puffer.Ladung_gesamt;8796.71
Pufferspeicher[0].Bezeichner;Probe-Heizungsspeicher
```

Rücknahme auf `Heizkreis`:

```
OK   WS_Ziel = Heizkreis, WS_ID_Puffer ist NULL (nicht 0!)
OK   Zuordnungszeile entfernt
Sim.PufferWP_vorhanden;False   Sim.Speicher_Anzahl;0
```

Der Lauf **nach** der Rücknahme wurde zusätzlich gegen `2026-08-14_Paket7/Projekt_1010` verglichen:
**PASS (201540 Werte)** — der Hin- und Rückweg hinterlässt keine Spur.

#### (b) Blockierfälle nach 4.6 — **PASS**

Siehe Validierungsmatrix in 9.4.

#### (c) Puffer-Verwaltung — **PASS**

Projekt 1011:

```
OK   c1 Puffer angelegt, Bezeichner unveraendert
OK   c1 Anlagenzeile ID_Type=12 vorhanden
OK   c1 Verwendung gesetzt, Schwellen 10/95/95
     zweiter Speicher heisst: Probe PS (2)
OK   c2 zweite Zeile angelegt (Dedup aufgehoben)
OK   c2 Namenskollision aufgeloest, eigene Anlagenzeile
OK   c3 GetProjektId liefert MIN(ID)
     Referenzen auf 1054164: CS6800iAW MB + AW 12 OR-T (Wärmepumpe) - Hauptsenke
OK   c4 Referenz wird gefunden
OK   c4b nach dem Loesen keine Referenz mehr
OK   c5 Entfernen: Pufferzeile weg, Anlagenzeile weg
OK   c5 Ausgangszustand wiederhergestellt
```

#### (d) Ladeprio-Auflösung 3.4/3.5/3.6 — **PASS**

Tabellen-Testfälle:

| Fall | Erwartet | Ergebnis |
|---|---|---|
| Vorgabe Solarthermie / WP / BHKW / Kessel / sonstige | 10 / 20 / 30 / 40 / 50 | OK |
| manuell 5 bei einer WP | 5 | OK |
| `WS_Ladeprio = 0` | Vorgabe 20 | OK |
| `WS_Ladeprio = 100` bzw. −3 (außerhalb 1…99) | Vorgabe | OK |
| PV-Prio 5, Modus PV, Überschuss | 5 | OK |
| PV-Prio 5, Modus PV, **kein** Überschuss | 20 | OK |
| PV-Prio 5, Modus Laufzeit, Überschuss | 20 | OK |

Echte Reihenfolge an einem Speicher (Projekt 1011: 3 WP, 2 Solarthermie, 2 Heizkessel, alle auf
denselben Puffer, `Schwelle_Aus = 95`, `Schwelle_Aus_Nachrang = 70`):

```
10  Solarthermie  test    bis 95 %   (Kaskade 2, Anlagenprio 99, ID 10644)   <- Vorrang
10  Solarthermie  test    bis 95 %   (Kaskade 2, Anlagenprio 99, ID 10645)   <- Vorrang
20  Wärmepumpe    …       bis 70 %   (Kaskade 4, …, ID 10635)
20  Wärmepumpe    …       bis 70 %   (Kaskade 4, …, ID 10642)
20  Wärmepumpe    …       bis 70 %   (Kaskade 4, …, ID 10643)
40  Heizkessel    …       bis 70 %   (Kaskade 99, …, ID 11218)
40  Heizkessel    …       bis 70 %   (Kaskade 99, …, ID 11219)
```

**Geändert in der Nacharbeit:** Beide Solarfelder sind vorrangig und laden bis `Schwelle_Aus`.
Vorher galt `Vorrangig = (i == 0)` — der Vorrang hing am **Listenplatz**, das zweite Solarfeld
bekam die Nachrangschwelle 70 %. Konzept 3.4 definiert den Vorrang über die **Zahl**: vorrangig ist
jede Anlage mit der kleinsten Ladepriorität, die am Speicher anliegt. Die weitere Sortierung
(Kaskade → Anlagenprio → ID) ordnet nur **innerhalb** desselben Rangs und darf keinen
Rangunterschied erfinden. Die Reservezone `Schwelle_Aus_Nachrang` bleibt damit den echten
Nachrangigen vorbehalten.

Geprüft wird jetzt: Anzahl der Vorrangigen = Anzahl der Anlagen mit der kleinsten Ladeprio; **alle**
Vorrangigen haben `Obergrenze = Schwelle_Aus`, **alle** Nachrangigen `Schwelle_Aus_Nachrang`.

Gleichstand (alle manuell auf 25): Reihenfolge folgt **Kaskadenposition → Anlagenprioritaet →
Anlagen-ID**, geprüft paarweise über die gesamte Liste — und **alle sieben** sind vorrangig, laden
also bis 95 %. Eine manuelle 1 zieht die betreffende Anlage an die Spitze und trägt sie als
`PrioManuell`; eine eigene `WS_Ladegrenze = 60` schlägt `Schwelle_Aus` (`Obergrenze = 60`,
`ObergrenzeEigen = true`). `EntladeprioAutomatik` liefert danach 1 — die beste Ladeprio am
Speicher (3.6).

#### (e) Zusatz: Variante duplizieren — **PASS**

Offener Punkt aus Paket 1 („Variantentest gehört in die Abnahme von Paket 2"):

```
Quellprojekt 1008 "Heinestr 15": Anlagen 10132/10133 -> Puffer 1008007
Variante 1026:                   Anlagen 11255/11256 -> Puffer 1054164
OK   WS_ID_Puffer zeigt auf die Puffer DER VARIANTE
OK   Quellprojekt unveraendert
```

Die `FK_MAP`-Einträge für `WS_ID_Puffer`, `WS_ID_Puffer2` und `WQ_ID_Puffer` stammen aus Paket 1;
der Versatz greift.

### 9.4 Validierungsmatrix 4.6 mit Testergebnis

| Prüfung (Konzept 4.6) | Umsetzung | Verhalten | Test |
|---|---|---|---|
| Hauptsenke gesetzt | `Normalisieren` setzt unbekannte/leere Ziele auf `Heizkreis` | still korrigiert | (a) |
| Senke `PUFFER_*` → Projekt-Puffer existiert, Verwendung passt | `PufferPasst` | **blockiert**, Meldung mit Anlagen-/Puffername, Absprung „Pufferspeicher anlegen…" | b1 (kein Puffer), b2 (falsche Verwendung) — beide OK, beide mit Absprung |
| Quelle `Pufferspeicher` → dito | **OFFEN — nicht umgesetzt** (siehe unten) | — | — |
| Zweitsenke ≠ Hauptsenke | `Pruefen`, Schritt 3 | **blockiert**, kein Absprung | b3 OK |
| Puffer als Quelle **und** Senke derselben Anlage | `QuellPufferDerAnlage` (neu `WQ_ID_Puffer`, alt Bezeichner `WQ_Puffer` → `MIN(ID)`) | **blockiert** (Kurzschluss) | b4 OK; b4b (anderer Speicher als Senke) erlaubt |
| Puffer wird geladen, aber sein Kanal hat keinen Bedarf | `KanalWarnung` über `Z_Projekt_Brauchwasser` | **Warnung**, kein Blocker | b5 OK — blockiert nicht, meldet aber |
| gültige Zweitsenke auf anderem Speicher | — | erlaubt | b6 OK |

Zusätzlich abgesichert (kein Konzeptpunkt, aber notwendig): Ladeobergrenzen müssen Zahlen zwischen
0 und 100 % sein; ist die Abfrage auf `Z_Projekt_Brauchwasser` nicht auswertbar, wird **nicht**
gewarnt — eine Warnung aus Unkenntnis wäre schlechter als keine.

**Offener Punkt — Zeile 3 der Tabelle.** Die Forderung aus 4.6, auch für die **Wärmequelle**
„Pufferspeicher" einen Projekt-Puffer mit passender Verwendung zu verlangen, ist in Paket 2
**nicht** umgesetzt. Der Quellendialog `Form_QuellePufferspeicher` listet unverändert
`Tab_Pufferspeicher_STAMM` und legt das Ergebnis als **Bezeichner** in `WQ_Puffer` ab;
`WaermequelleClass.Quellspeicher` löst ihn zur Laufzeit wieder gegen den Katalog auf (§8). Ein
Projekt-Puffer ist auf diesem Weg gar nicht wählbar, eine Verwendungsprüfung liefe also ins Leere.

Die frühere Protokollfassung wies diese Zeile mit „Quellendialog (Paket 3) unverändert; hier
gegengeprüft über `QuellPufferDerAnlage`" als abgedeckt aus — das war zu großzügig:
`QuellPufferDerAnlage` dient ausschließlich der **Kurzschlussprüfung** in Zeile 4 (derselbe Speicher
als Quelle **und** Senke) und prüft die Verwendung nicht.

Die Umstellung des Quellenpfads auf den Projekt-Puffer (`WQ_ID_Puffer` statt `WQ_Puffer`, Auswahl
aus `WaermesenkeClass.ProjektPufferListe`) gehört zum **Engine-Umbau in Paket 4** — dort wird
`Quellspeicher` ohnehin auf die Projektzeile umgezogen, und erst dann ist die Prüfung mehr als eine
Formalie.

### 9.5 Kodierung und Diff

Nachgemessen byteweise (BOM-Signatur, Zählung von `0x0A` mit und ohne vorangehendes `0x0D`,
Suche nach `U+FFFD`) — nicht mit `grep`, das unter Git Bash je nach Textmodus falsche Ergebnisse
liefert:

| Datei | Kodierung | Zeilenenden |
|---|---|---|
| `Form_Simulation_Config.cs` | UTF-8 **mit** BOM (wie vorgefunden) | LF (1256/1256) |
| `PufferSpCtrl.cs`, `ProjektPuffer.cs` | UTF-8 mit BOM | CRLF (976/976 bzw. 477/477) |
| `WaermequelleClass.cs` | UTF-8 ohne BOM | LF (742/742) |
| neue Dateien | UTF-8 ohne BOM | LF (wie die Nachbardateien `Form_QuellePufferspeicher.cs`, `WaermequelleClass.cs`) |

**Jede Datei ist in sich einheitlich** — keine gemischten Zeilenenden, auch nach der Nacharbeit
nicht. **Null Ersatzzeichen** in allen berührten Dateien. Die gesperrten Dateien (`WizardCtrl.cs`,
`WErzeugerModel.cs`, `Form_BHKWEing.cs`, `WizardParent.cs`, `Form_Heizkessel*.cs`, `RecordSet.cs`)
sind unverändert — ihre Zeitstempel liegen sämtlich vor Beginn der Nacharbeit. Keine `.Designer.cs`
und keine `.resx` angefasst.

### 9.6 Proben zur Review-Nacharbeit (headless)

Dieselbe Werkzeugkette wie 9.3, je Probe eine eigene Datenbankkopie außerhalb des Repos. Die
Proben f und g enthalten jeweils eine **Gegenprobe**, die den ursprünglichen Befund am selben
Datenstand reproduziert — sonst belegt ein „OK" nur, dass etwas funktioniert, nicht dass es vorher
kaputt war.

#### (f) Stiller Temperatur-Rückschreiber und Temperaturnachführung — **PASS**

```
OK   f0 Puffer angelegt (55/35)
OK   f0 Zuordnung fuehrt 55/35
OK   f1 Puffer traegt jetzt 60                                (Verwaltung aendert den Vorlauf)
OK   f2 BEFUND: ohne Bruecke schreibt Speichern die 55 zurueck  <- der Befund, reproduziert
OK   f3 Bruecke fuehrt die Zuordnung nach (60)                  <- Fix
OK   f3 nach dem Speichern steht am Puffer weiterhin 60
OK   f4 Puffer hat kein Paar mehr
OK   f4 Zuordnung behaelt 60/35 (kein 0/0-Ueberschreiben)       <- Fix 6
OK   f9 Ausgangszustand wiederhergestellt
```

Nachgebildet wird der Teil von `btn_Speichern_Click`, der die Temperaturen der führenden
WP-Zuordnung über `PufferSpCtrl.SetTemperaturen` an den Puffer überträgt (B4-1/Etappe 4) — die
Kernlogik selbst wurde nicht angefasst.

#### (g) Umbenennen erzeugt keinen Duplikat-Puffer — **PASS**

```
Katalogname fuer die Probe: allSTOR exclusiv VPS 800/3-7
OK   g1 Pufferzeile heisst neu
OK   g1 Anlagenzeile (ID_Type=12) zieht nach
OK   g2 FIX: Z_ProjektPufferSp.Pufferspeicher zieht ebenfalls nach
OK   g3 Speichern legt KEINEN Duplikat-Puffer an
OK   g3 Zuordnung zeigt weiterhin auf denselben Puffer
OK   g4 BEFUND: mit altem Namen entsteht ein Duplikat            <- Gegenprobe
OK   g9 Ausgangszustand wiederhergestellt
```

#### (h) Entladereihenfolge und Ladereihenfolge-Kriterium — **PASS**

Testfall ist der **Alt-Puffer 1011007** (Projekt 1011, `Verwendung` leer — entstanden über das
frühere implizite `CopyFromStamm`):

```
Puffer 1011007 Verwendung: ""
OK   h1 steht in ProjektPufferListe(Heizung)
OK   h1 FIX: steht in der Heizungs-Entladereihenfolge
OK   h1 wirksame Verwendung wird als Heizung ausgewiesen
OK   h2 taucht NICHT im Brauchwasserkanal auf
OK   h3 gueltige Senke zaehlt
OK   h3 FIX: Altdaten-Rest (ID gesetzt, Ziel Heizkreis) zaehlt NICHT
OK   h9 Ausgangszustand wiederhergestellt
```

#### (i) Layout der Übersicht — **PASS**

Baut `Form_Simulation_Config` wirklich auf (ohne es anzuzeigen) und misst nach:

```
Formular ClientSize : 1169 x 532
Gruppe Uebersicht   : 883 x 309 @ 267,109
ListView            : 869 x 282
Spalten             : 40 84 140 62 100 120 100 92 112   Summe 850
Platz in der Liste  : 856  (Rest 6)
OK   i3 FIX: kein waagerechter Rollbalken (Spalten passen in die Liste)
OK   i4 Uebersicht bleibt im Formular
OK   i5 Schaltflaechen bleiben im Formular
OK   i6 Rubrik weiterhin unsichtbar (Etappe A)
```

Mehr lässt sich ohne Sichttest nicht prüfen — Lesbarkeit, Tabulatorreihenfolge und das Verhalten
unter anderer Systemschriftgröße bleiben Handarbeit (§12.1).

---

## 10. Bewusste Abweichungen

1. **Zweitsenke nur auf Puffer-Ziele — und zwar konsequent.** Das Mockup 4.1 zeigt beim BHKW eine
   Zweitsenke „Heizkreis". Umgesetzt sind nach Auftragslage ausschließlich Puffer-Ziele — fachlich
   schlüssig, weil die Zweitsenke laut 3.1 „ausschließlich zur Verwertung von Überschuss bzw.
   verbleibendem Ladepotenzial, **nie** zur Deckung von Pflichtbedarf" dient und der Heizkreis genau
   das Gegenteil ist.

   **Korrektur gegenüber der ersten Fassung.** Dort stand, `Pruefen` „toleriere einen vorgefundenen
   Wert `Heizkreis` in `WS_Ziel2`". Das war falsch: `Normalisieren` **löscht** jede Zweitsenke, die
   kein Puffer-Ziel ist, bedingungslos — und `Pruefen` ruft `Normalisieren` als erstes. Ein
   vorgefundener `Heizkreis` in `WS_Ziel2` wird also nicht toleriert, sondern verworfen.

   Das ist die **beibehaltene** Entscheidung: eine Semantik, nicht zwei. Angeglichen wurden
   stattdessen die beiden Code-Stellen, die so taten, als gäbe es den anderen Fall:

   - `Pruefen`, Schritt 3: Der zweite Disjunkt („beide sind kein Puffer") war nach dem
     Normalisieren unerreichbar und ist entfallen. Übrig bleibt die eine Frage, auf die es
     ankommt — zeigen Haupt- und Zweitsenke auf denselben Speicher?
   - `ZweitsenkeAnzeige`: Der Rückfall auf `ZielAnzeige(d.Ziel2)` war ebenso unerreichbar und hätte
     für hand­gebaute, nicht normalisierte Daten „Heizkreis" als Zweitsenke ausgewiesen — genau das,
     was `Normalisieren` verwirft. Jetzt gilt dort dieselbe Regel: kein Puffer-Ziel = keine
     Zweitsenke („–").

   Erzeugen kann die Oberfläche einen Heizkreis in `WS_Ziel2` ohnehin nicht.
2. **Kein „Abbrechen" in der Puffer-Verwaltung** (siehe 7).
3. **Betriebsmodus für Nicht-WP gesperrt statt umgetextet** (siehe 3).
4. **Spalte „Zuordnung (Altmodell)" bleibt in der Übersicht** (siehe 4).
5. **Leere `Verwendung` gilt als „Heizung".** Puffer aus dem früheren impliziten `CopyFromStamm`
   haben kein `Verwendung`. Sie als „unbestimmt" auszublenden hieße, sie wären nicht mehr wählbar;
   „Heizung" ist zugleich die Vorbelegung von Migration (5.5) und
   `ProjektPuffer.PufferParameter`. In der Verwaltungsliste sind sie mit
   „(Verwendung nicht gepflegt)" gekennzeichnet.
6. **Texte deutsch hartkodiert.** Konzept 4.2 verlangt, die neuen Dialoge von Anfang an gegen den
   Ressourcenkatalog `MyResource.Resource.SIM_*` zu bauen. Dieser Katalog entsteht erst in
   **Paket 9** (13.6) und existiert heute nicht; die Texte stehen deutsch im Code, wie im übrigen
   Simulationsbereich (`Form_QuellePufferspeicher`, `Form_Quellprofil`, Übersichtsspalten und
   Tooltips). Die neuen Dateien sind bewusst so geschnitten, dass die Umstellung eine reine
   Textersetzung ist — jeder sichtbare Text steht genau einmal.
7. **Eigene Helferklasse `StilleDb` statt Umbau der vorhandenen privaten Helfer.**
   `PufferSpCtrl.StillScalar` und `WaermequelleClass.SkalarStill` bleiben unangetastet, damit
   Paket 2 keinen bestehenden Rechenpfad anfasst.

---

## 11. Dialogfreiheit im Engine-Pfad (Alt-Review-Punkt aus Paket 1)

`WaermequelleClass.WertLesen` geht über `DataRepository.ExecuteScalar` und kann im Fehlerfall eine
MessageBox mitten im Rechenlauf zeigen — Konzept 13.4 verlangt Dialogfreiheit. Umgestellt sind:

- **`Quellspeicher(…)`** (der ausdrücklich genannte Stufe-1-Zugriff): alle Feldzugriffe über
  `WertLesenStill`, der Katalogzugriff über die neue `TabelleStill`.
- **`Quelltemperatur(…)`** komplett: `WQ_Typ`, `WQ_Temp`, `WQ_ID_Puffer`, `WQ_Monatswerte`,
  `WQ_Wochenwerte`, `WQ_CSV`, `WQ_Quellsystem`, `WQ_Bodentyp`, `WQ_Tiefe` sowie die beiden
  Altdaten-Abfragen auf `Z_ProjektPufferSp.Vorlauf/Ruecklauf` (vorher `DataRepository.ExecuteScalar`).

Gelesen werden dieselben Werte mit derselben Null-Semantik; die Regression (9.2) belegt die
Ergebnisgleichheit. **`WertLesen` selbst bleibt unverändert** — sein Aufrufkreis in der Oberfläche
ist breit, und dort ist ein Fehlerdialog erwünscht.

---

## 12. Offene Punkte

1. **UI-Sichttest steht aus.** Sämtliche Nachweise sind headless geführt — auch das Layout, das
   Probe i (§9.6) am wirklich aufgebauten Formular vermisst.

   **Der waagerechte Rollbalken ist weg**, nachgemessen: Spaltensumme 850 px in 856 px Liste. Was
   Probe i **nicht** prüfen kann und daher offen bleibt:

   - Lesbarkeit der neun Spalten und die Frage, ob 140 px für die Anlagenspalte in der Praxis
     reichen (lange Herstellerbezeichner werden mit „…" gekürzt).
   - Tabulatorreihenfolge der neuen Dialoge.
   - Verhalten unter anderer Systemschriftgröße: Die Spaltenbreiten sind feste Pixelwerte, die
     Texte wachsen mit der Schrift — bei deutlich größerer Schrift kürzt die ListView mehr. Der
     Simulationsbereich ist faktisch DpiUnaware; die Pixelpositionen sind wie im Bestand fest.
   - Ob 1169 px Clientbreite auf allen Arbeitsplätzen bequem sind. Auf einem schmalen Schirm kappt
     `UebersichtBreiteAnpassen` die Verbreiterung, dann kommt der Rollbalken für die letzten
     Spalten zurück; das Formular ist in der Größe veränderbar.
2. **Etappe B (4.4)** — Entfernen von `_zuordnungen`, `listView1`, `AktivePufferSp`,
   `RefreshZuordnungAnzeige`, `ZugeordnetePufferSp`, `SpeicherregelungBearbeiten`, `btn_Hinzu`,
   `btn_Loeschen`, `Form_KonfigPufferspeicher` und der Spalte `COL_PUFFER`. Erst nach bestätigter
   Migration in Realprojekten und zusammen mit Paket 4.
3. **Übergangsbrücke entfällt mit Paket 4** (siehe 5). Sie deckt bewusst nur die Wärmepumpe ab —
   mehr wertet die Engine heute nicht aus. Senken von Kessel, BHKW und Solarthermie werden
   gespeichert und angezeigt, wirken aber erst mit Paket 4.
4. **Ladepriorität, Ladeobergrenze, PV-Sonderregel und Entladepriorität sind heute reine
   Datenpflege.** `Ladeordnung` rechnet sie vollständig aus und beide Dialoge zeigen sie an; die
   Engine liest sie erst in Paket 4/6. Das ist der Zweck der Anzeige („die maßgebliche
   Kontrollinstanz", 3.4) — der Anwender sieht die Reihenfolge, bevor sie wirkt.
5. **`Schwelle_Ein`/`Schwelle_Aus` doppelt pflegbar.** Bis Etappe B stehen sie am Puffer (4.3) und
   an der Alt-Zuordnung (`SpeicherregelungBearbeiten`). Ausgewertet wird bis Paket 4 die
   **Zuordnung**. Die Brücke fasst die Schwellen nicht an; wer sie am Puffer ändert, sieht die
   Wirkung erst mit Paket 4.
6. **`Form_KonfigPufferspeicher`** ist über `btn_Hinzu` weiterhin erreichbar, aber die Schaltfläche
   ist unsichtbar — der Dialog ist damit toter Code bis Etappe B.
7. **Erzeugertypen `REF_*` (5…9)** bekommen weiterhin keine Senke. In der Arbeitskopie existiert
   keine Zeile dieser Typen (Befund aus Paket 1); die Übersicht kennt sie nicht.

8. **`PRIO_SONSTIGE = 50` trägt zwei Bedeutungen.** Die Konstante steht in `Ladeordnung` für
   „Erzeugertyp, der keiner der vier bekannten ist — hinter dem Kessel" (`VorgabeLadeprio`,
   Konzept 3.4) **und** dient `EntladeprioAutomatik` als Rückfall für „diesen Speicher lädt
   niemand, er wird zuletzt entladen" (3.6). Beides ergibt zufällig dieselbe 50, meint aber
   Verschiedenes: einmal eine Ladepriorität, einmal eine Entladepriorität. Solange beide Skalen
   1…99 laufen und 50 in beiden „spät" heißt, ist das unauffällig; wer eine der beiden Vorgaben
   ändert, ändert unbeabsichtigt die andere mit. Zwei getrennte Konstanten wären sauberer — die
   Umbenennung wurde zurückgestellt, weil die Werte in Paket 4/6 ohnehin in die Engine wandern und
   dort ihre endgültige Form bekommen.

9. **Quell-Puffer-Prüfung aus 4.6 offen** — siehe §9.4. Der Quellendialog löst weiterhin gegen
   `Tab_Pufferspeicher_STAMM` auf; die Umstellung auf den Projekt-Puffer (`WQ_ID_Puffer`) gehört
   zum Engine-Umbau in Paket 4.

10. **Bekannte Punkte aus dem Review, bewusst offen gelassen** (Einzelheiten an der jeweiligen
    Stelle):

    | Punkt | steht in |
    |---|---|
    | Brücke bildet nur **eine** Wärmepumpe ab; Puffer-Senken weiterer WPs erreichen die Alt-Zuordnung nicht | §5 |
    | Schwellenverlust beim Speicherwechsel **A → B → A** | §5 |
    | Zehn `UPDATE`s in `WaermesenkeClass.Schreiben` plus bis zu drei in der Brücke, **ohne Transaktion** | §5 |
    | **Entfernen** prüft keine vorhandene `Z_ProjektPufferSp`-Zeile, sondern nur die vier ID-Spalten | §8 |
    | `PRIO_SONSTIGE` mit Doppelbedeutung | Punkt 8 |

---

## 13. Reproduktion

```powershell
# 1. Bau
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj -p:Configuration=Debug -p:Platform=x86

$exe   = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
$basis = "C:\Waermeplan\Paket2_Nach"

# 2. Eigene, migrierte Kopie AUSSERHALB des Repos
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb $basis\DB_Basis

# 3. Regression: neun Projekte im Modus "projekt" (migriert NICHT)
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "$basis\Lauf9\Projekt_$id" $basis\DB_Basis
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket7 $basis\Lauf9

# 4. Datenpfad-Proben (Wegwerf-Werkzeug AUSSERHALB des Repos, je Probe eigene Kopie)
#    Quelle: <Scratchpad>\Paket2Proben\  (Projektreferenz auf WindowsFormsApplication1.csproj,
#    Zugriff auf die internen Ctrl-Methoden per Reflection - Muster Referenzlauf\DbUmgebung.cs)
#    Aufruf: Paket2Proben.exe <a|ar|b|c|d|e|f|g|h|i> <dbOrdner>
$proben = "<Scratchpad>\Paket2Proben\bin\x86\Debug\net8.0-windows\Paket2Proben.exe"
Copy-Item -Recurse $basis\DB_Basis $basis\DB_a
& $proben a  $basis\DB_a
& $exe projekt 1010 "$basis\ProbeA_Lauf" $basis\DB_a          # Puffer.Q_max = 18,56
& $proben ar $basis\DB_a
& $exe projekt 1010 "$basis\ProbeA_zurueck\Projekt_1010" $basis\DB_a
& $exe vergleich $basis\Ref1010 $basis\ProbeA_zurueck         # PASS (201540 Werte)

# je eigene Kopie: b, c, d, e und die Nacharbeit-Proben f, g, h, i
foreach ($p in "b","c","d","e","f","g","h","i") {
    Copy-Item -Recurse $basis\DB_Basis "$basis\DB_$p"
    & $proben $p "$basis\DB_$p"
}
```

Probe `i` baut `Form_Simulation_Config` auf und braucht deshalb einen Desktop (STA); die übrigen
laufen rein auf den Daten.

---

## 14. Review-Nacharbeit

Zwölf Befunde aus dem Review am uncommitteten Paket 2, nach Dringlichkeit abgearbeitet. Jede Zeile
nennt die Stelle und den Nachweis; die ausführliche Begründung steht jeweils im Code-Kommentar und
im oben genannten Kapitel.

| # | Befund | Umsetzung | Nachweis |
|---|---|---|---|
| 1 | **Stiller Temperatur-Rückschreiber.** Nach dem Ändern der Betriebstemperaturen in der Verwaltung stand in der unsichtbaren Alt-Zuordnung noch der alte Wert; „Speichern" schrieb ihn über `SetTemperaturen` an den Puffer zurück. | `btn_PufferVerwalten_Click` ruft `ZuordnungBrueckeAnwenden()` **vor** `ZuordnungenLaden()` (`Form_Simulation_Config.Uebersicht.cs`) | Probe f, §9.6 — inkl. Gegenprobe |
| 2 | **Duplikat-Puffer beim Umbenennen.** `Z_ProjektPufferSp.Pufferspeicher` behielt den alten Namen; das nächste „Speichern" legte über `CopyFromStamm` eine zweite Projektkopie an. | `ProjektPufferAendern` zieht die Textreferenz nach (`UPDATE … WHERE ID_Pufferspeicher = ?`) | Probe g, §9.6 — inkl. Gegenprobe; §8 |
| 3 | **Entladereihenfolge übersah Puffer mit leerer `Verwendung`.** SQL-Gleichheitsvergleich statt `WirksameVerwendung`. | `Entladereihenfolge` liest über `WaermesenkeClass.ProjektPufferListe` und filtert im Code — dieselbe Regel wie `ProjektPufferListe`/`PufferPasst` | Probe h (Alt-Puffer 1011007), §9.6 |
| 4 | **Vorrang hing am Listenplatz**, nicht an der Zahl. | `Vorrangig = (Ladeprio == kleinste Ladeprio der Liste)` in `ObergrenzenAufloesen` | Probe d, §9.3(d) — zwei Solarfelder mit Rang 10 sind beide vorrangig |
| 5 | **Brauchwasser-Senke wirkte still nicht.** | Verhalten bleibt (die Alt-Engine kennt nur den Heizungs-Puffer), aber sichtbarer Hinweis beim OK des Senkendialogs; Brückentabelle um die Zeile ergänzt | §5, §6 |
| 6 | **Brücke überschrieb Zuordnungstemperaturen mit 0/0**, wenn der Puffer kein gültiges Paar trug — die Rückfallstufe „Zuordnung" ging verloren. | UPDATE-Zweig prüft `ProjektPuffer.IstTemperaturpaar`; sonst wird nur der Name geführt | Probe f4, §9.6 |
| 7 | **Übersicht mit waagerechtem Rollbalken.** | Kompakte Kopftexte + feste Spaltenbreiten statt `AutoResizeColumns(HeaderSize)`; Formular im Code-Behind auf 1169 px verbreitert, am Schirm gekappt | Probe i, §9.6; §4 „Breite" |
| 8 | **CS0162** (unerreichbarer Code) durch `const bool RUBRIK_SICHTBAR = false`. | `static readonly` mit Begründungskommentar | §9.1 — 7 → 6 Warnungen, keine neue |
| 9 | **Verwendungswechsel ohne Warnung**, obwohl der Speicher referenziert war. | Ja/Nein-Rückfrage mit Anlagenliste im Verwaltungsdialog (dialogfrei bleibt der Ctrl) | §7 |
| 10 | **Tote Zweige und ungetypter DBNull-Parameter.** | Zweiter Disjunkt in `Pruefen` Schritt 3 und der `ZielAnzeige`-Rückfall in `ZweitsenkeAnzeige` entfernt; `ERZEUGER_TYPEN` gelöscht; neue Überladung `WertSchreiben(int,string,OleDbType,object)` für die drei NULL-fähigen Spalten | §10.1; Bau ohne neue Warnung; Probe a (NULL statt 0) |
| 11 | **Ladereihenfolge zählte Altdaten-Reste** (ID gesetzt, Ziel Heizkreis). | `Ladereihenfolge` prüft zusätzlich `WS_Ziel` bzw. `WS_Ziel2` über `IstPufferZiel` | Probe h3, §9.6 |
| 12 | **Kleinteiliges.** | „…s-Speicher(n)" → „Heizungsspeichern"/„Brauchwasserspeichern" (`KanalSpeicherWort`); `SetControls` der Verwaltung berücksichtigt die übergebene Verwendung; `_cbLadeprio2` am Auswahl-Handler | §7, §6 |
| 13 | **Protokoll.** | Abweichung 1, §9.1, §9.4 Zeile 3, §8, §12.1, Zeilenzahlen, Kodierungsnachweis korrigiert; `PRIO_SONSTIGE` und die vier bekannten Punkte aus dem Review aufgenommen | dieses Kapitel, §12.8–12.10 |

### Was dabei bewusst NICHT geändert wurde

- **`btn_Speichern_Click`** — die Kernlogik des Delete/Insert-Zyklus bleibt unangetastet. Fix 1
  setzt davor an: Er sorgt dafür, dass `_zuordnungen` beim Speichern die Wahrheit enthält.
- **Gesperrte Dateien** (`WizardCtrl.cs`, `WErzeugerModel.cs`, `Form_BHKWEing.cs`, `WizardParent.cs`,
  `Form_Heizkessel*.cs`, `RecordSet.cs`) — nicht angefasst, Zeitstempel unverändert.
- **Designer und `.resx`** — nicht angefasst. Die Verbreiterung läuft ausschließlich im
  Code-Behind, wie die Höhenanpassung aus Etappe A.
- **`Normalisieren`** bleibt streng (löscht Nicht-Puffer-Zweitsenken bedingungslos). Angeglichen
  wurde der Code drumherum, nicht die Semantik — eine Regel, nicht zwei (§10.1).
- **Die Ctrl-Bausteine bleiben dialogfrei.** Die Rückfrage aus Fix 9 sitzt deshalb im Dialog und
  nicht in `PufferSpCtrl.ProjektPufferAendern`; eine MessageBox dort brächte den nächsten
  headless-Lauf zum Stehen (13.4).

### Stand nach der Nacharbeit

```
Bau (Rebuild, Debug/x86)                 0 Fehler, 6 Warnungen (vorher 7), keine neue
Regression 9 Projekte gegen Paket7       PASS (2 260 923 Werte)
Proben a/ar, b, c, d, e                  PASS
Proben f, g, h, i (Nacharbeit)           PASS
Hin- und Rückweg Projekt 1010            PASS (201 540 Werte) - keine Spur
Kodierung / Zeilenenden                  unverändert, null Ersatzzeichen
```

**Weiterhin nicht committet** — Abnahme steht aus.
