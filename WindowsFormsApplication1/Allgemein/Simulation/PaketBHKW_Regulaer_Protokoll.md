# Paket BHKW-Regulär (Umsetzungsprotokoll)

Stand: 17.08.2026 · Grundlage: Entscheidungen des Anwenders vom 17.08.2026 (vier Punkte,
Kapitel 1) · Vorarbeit: [`Paket6_BHKW_Protokoll.md`](Paket6_BHKW_Protokoll.md) — dort sind
die Nutzerentscheidungen **6-1** (Altpfad-Fix zurückgestellt) und **6-2** (Sommerbetrieb
nicht reaktivieren) festgehalten; **beide sind mit diesem Paket revidiert**.

**Nicht committet.** Der Anwender synct selbst.

**Kernaussage in einem Satz.** Das BHKW rechnet ab jetzt wie jeder andere Wärmeerzeuger —
über die Speicherstufe, mit seinem Pufferspeicher im Bilanzraum —, und der einkanalige
BHKW-Altpfad, der den Speicher am Bedarf vorbeigerechnet hat, ist ersatzlos entfallen.

**Projekte OHNE BHKW rechnen unverändert.** Nachgewiesen, nicht behauptet: Projekt 1021
byte-identisch über alle 227.840 Werte, Projekt 1023 mit genau einer Abweichung (ein
Energieträger-Datenstand, keine Rechengröße), 1007 und 1011 in allen Wärmereihen
byte-identisch (Kapitel 7.3).

---

## 1. Entscheidungen des Anwenders (verbindlich)

| Nr. | Entscheidung | Umsetzung |
|---|---|---|
| 1 | „Zweikanalig (mit Warmwasser) muss nicht sein, es soll analog anderen Wärmeerzeugern funktionieren. Der Altpfad wird nicht benötigt." ⇒ BHKW-Projekte rechnen IMMER über die Speicherstufen-Mechanik; kein Flag-Zwang, keine Kaskade nötig | Etappe 1 |
| 2 | Leistungsuntergrenze: Migration setzt `Leistungsgrenze = 30`, wo heute **0 oder 1** steht; Engine-Fallback 50 % → 30 %; Label „[50%]" → „[30%]" | Etappen 2 + 3 |
| 3 | Neuer Pufferspeicher-Parameter „Mindestfüllstand/Notreserve [%]", Vorbelegung 10 (Bestand UND neu); wirkt **nur** auf die BHKW-Entladung, andere Erzeuger entladen unverändert bis 0 | Etappen 2 + 4 |
| 4 | Feld „Volumen Pendelspeicher [l]" auf der BHKW-Parameterseite **ausbauen**; bestehende „BHKW-Pendelspeicher"-Puffer bleiben normale Projektpuffer | Etappe 5 |

Damit sind die beiden Paket-6-Entscheidungen **revidiert**: Der Altpfad wird nicht
gefixt, sondern entfernt (6-1), und die toten Sommer-/Notschaltungszweige werden nicht
konserviert, sondern gelöscht (6-2).

---

## 2. Etappe 1 — Weiche und Altpfad-Rückbau

### 2.1 Die Weiche

Geändert wird **das Feld `KaskadeZweikanalig` selbst**, nicht nur die Verzweigung in
`Do_Simulation`:

| Datei:Zeile | Änderung |
|---|---|
| `SimulationControl.cs:104-136` | Kopfkommentar des Feldes neu (zwei Quellen); neues privates Feld `_bhkwErzwingtSpeicherstufe` |
| `SimulationControl.cs:392-396` | `_bhkwErzwingtSpeicherstufe = KaskadeEnthaelt(ERZEUGER_BHKW)`; `KaskadeZweikanalig = Flag \|\| _bhkwErzwingtSpeicherstufe` |
| `SimulationControl.cs:425-436` | Protokollhinweis differenziert: BHKW-Fall nennt seinen Grund, Flag-Fall bleibt wortgleich |
| `SimulationControl.cs:455-461` | Kommentar an der Verzweigung nachgezogen |

**Warum das Feld und nicht nur die Verzweigung.** Der Rechenweg ist an fünf weiteren
Stellen abgefragt: `AlleSpeicher()` (`:2159`), der Registry-Aufbau (`:2332`) und der
`SimulationRunner` an vier Stellen (`:308`, `:343`, `:381`, `:434` — Restbedarf und
Deckungsgrade von Wärmepumpe und BHKW). Hätte nur die Weiche den BHKW-Fall gekannt, hätte
ein BHKW-Projekt zweikanalig gerechnet, während seine Ergebnisbildung noch die
Altpfad-Formeln genommen hätte. Ein Feld, eine Antwort.

`tool[]` steht zum Auswertungszeitpunkt fest: `SimulationRunner:169` setzt `sim.tool` vor
`sim.Do_Simulation` (`:179`).

**`_bhkwInSchleife` ist unverändert** (`SimulationControl.cs:737-748`, Kommentar
nachgezogen). Die Weiche entscheidet nur, dass ein BHKW-Projekt in die Speicherstufe
kommt; ob das BHKW dort Schleifenmitglied oder Vektorstufe ist, bleibt eine Frage seines
Speichers (Puffer-Senke ODER Pendelspeicher). Ein BHKW ohne jeden Puffer rechnet als
Vektorstufe — belegt an Projekt 1017 (Kapitel 7.2).

### 2.2 Was aus dem Altpfad entfernt wurde

| Datei:Zeile (Ersatzkommentar) | Entfernt |
|---|---|
| `SimulationControl.cs:541-547` | der `else if (tool[i] == ERZEUGER_BHKW)`-Zweig der Altpfad-Schleife |
| `SimulationControl.cs:3095-3119` | `Simulation_BHKW_Ctrl(...)` — RecordSet-Abfrage, Vektordifferenz `Bedarf − Produktion`, Befüllung von `kapazitaetPendelspeicher` aus `VolumenPendelspeicherBHKW` |
| `SimulationBHKW.cs:136-158` | `Berechnung(int)` — der Einstieg des Altpfads |
| `SimulationBHKW.cs:351-359` | `BhkwSimulationWaermegefuehrt(...)` — Jahresschleife wärmegeführt |
| `SimulationBHKW.cs:439-452` | `Speicherabrechnung(...)` und `SimulationStromgefuehrt(...)` |
| `SimulationBHKW.cs:502-506` | `SimulationOhneEinspeisung(...)` |
| `SimulationBHKW.cs:670-673` | `SolareErzeugung(int)` — VBA-Platzhalter, gab immer `0f` zurück |
| `SimulationBHKW.cs:53-58` | die drei Solar-Felder `solarVorhanden` / `solarSpeicher` / `solarWaerme` |

Die drei Fahrweisen-Jahresschleifen **mussten** mit, weil `Speicherabrechnung` sonst ohne
Aufrufer bzw. der Aufruf ohne Methode gestanden hätte. `SolareErzeugung` und die
Solar-Felder verwaisten damit ebenfalls; die Felder hätten als „zugewiesen, aber nie
verwendet" eine neue Compilerwarnung ergeben.

**Was an Physik erhalten blieb:** die drei Motorläufe `Motorlauf_Waermegefuehrt`,
`Motorlauf_Stromgefuehrt`, `Motorlauf_OhneEinspeisung`. `Fahrweise_Stunde` ruft genau
dieselben Methoden — nur gegen einen Speicherspiegel statt gegen den skalaren
Pendelspeicher (Paket-5-Lehre N6: die Physik steht einmal).

**`kapazitaetPendelspeicher` bleibt** (`SimulationBHKW.cs:59-70`, Kommentar neu): Es ist
der generische Speicherraum-Skalar der Motorläufe, über den der zweikanalige Weg seinen
Stufenspeicher spiegelt. Nur die Altpfad-Befüllung ist entfallen.

### 2.3 Tote Zweige in `Motorlauf_Waermegefuehrt`

`SimulationBHKW.cs:361-473` (Doku `:361-396`, Methode `:397-473`).

Die Methode war in eine Betriebsartenweiche gehüllt, deren erste Bedingung
`if (stunde < 8760)` für **jede** Stunde des Jahres wahr ist (die Jahresschleife läuft
0…8759). Unerreichbar waren damit:

- der **Sommerbetrieb** `else if (stdTag > 10 && stdTag < 22)` mit vier stromseitigen
  Zuschaltfällen,
- die darin liegende **30-%-Schwelle** und die **10-%-Notschaltung**,
- die **20-%-Notschaltung** als letzter `else if`-Zweig.

**Ergebnisneutral durch Lesen nachweisbar**, nicht erst durch Messen: Kein Aufruf konnte
sie erreichen. Die auskommentierte Altbedingung `//if (stunde < 3600 || stunde > 5760)`
steht weiterhin als Spur im Kommentar — sie zurückzuholen wäre eine fachliche Änderung
und gehört nicht in dieses Paket (Vorbefund `Paket6_BHKW_Protokoll.md:74-94`).

Mit den Zweigen entfielen ihre Parameter: `stdTag`, `strombedarf` (nur Sommerbetrieb),
`bhkwGrenzleistung` (nur 10-%-Notschaltung) und `solarSpeicher`. Letzterer trug seit dem
Altpfad-Rückbau konstant `0f`, sodass die Terme
`kapazitaetPendelspeicher - solarSpeicher - speicher` **bitgleich** zu
`kapazitaetPendelspeicher - speicher` sind. Die lokale `stdTag`-Berechnung in
`Fahrweise_Stunde` ist mit entfallen — sie hätte sonst eine CS0219-Warnung ergeben.

Umfang: `SimulationBHKW.cs` von **1707 auf 1394 Zeilen** (−313), `git diff` 687 Zeilen
angefasst.

### 2.4 Kommentare, die Falsches behaupteten

| Datei:Zeile | Nachgezogen |
|---|---|
| `SimulationControl.cs:2884-2891` | `AltpfadHinweiseD5a` — Geltungsbereich: erreicht nur noch Projekte ohne BHKW; ein BHKW-Hinweis dort wäre unerreichbar |
| `SimulationControl.cs:1423-1436` | `BHKW_Liste_Laden` — ist jetzt der EINZIGE Ladeweg; `see cref` auf die entfallene Methode aufgelöst; Katalog-/100-Fix vermerkt |
| `SimulationBHKW.cs:18` | `bhkw_anlagen_ids` „Gefüllt von … `Simulation_BHKW_Ctrl`" → `BHKW_Liste_Laden` |
| `SimulationBHKW.cs:176`, `:222`, `:261` | „Schritt 0/1/2 aus `Berechnung`" → „des Laufs (vormals aus `Berechnung`)" |
| `SimulationBHKW.cs:434`, `:511`, `:824` | `see cref` auf die drei entfallenen Fahrweisen aufgelöst |
| `SimulationBHKW.cs:1196-1200` | `Fahrweise_Stunde` — „dieselben Methoden wie im Altpfad" → „die auch der entfallene Altpfad benutzt hat" |

`KonfigurationCtrl.KaskadeNotwendig` (`:229-278`) blieb **unangetastet** — die Logik wurde
nicht erweitert, weil der BHKW-Fall dort nach dem Umbau irrelevant ist.

---

## 3. Etappe 2 — Migration Schritt 13

`ZIEL_VERSION` 12 → **13** (`SchemaMigration.cs:61`).

| Datei:Zeile | Inhalt |
|---|---|
| `SchemaKatalog.cs:289` | `SPALTE_SCHWELLE_RESERVE = "Schwelle_Reserve"` |
| `SchemaKatalog.cs:291-319` | `Schritt13_Mindestfuellstand` — `Tab_Pufferspeicher.Schwelle_Reserve DOUBLE` |
| `SchemaKatalog.cs:550-569` | Eintrag in `Alle` samt Begründung |
| `SchemaMigration.cs:161-187` | `SCHRITT_13_BHKW_REGULAER = 13` mit Doku (zwei Teile 13a/13b) |
| `SchemaMigration.cs:396-402` | Registrierung im `SCHRITTE`-Array |
| `SchemaMigration.cs:1481-1527` | `Schritt_13_BhkwRegulaer(Lauf)` |
| `SchemaMigration.cs:1529-1560` | `ReserveVorbelegen` — `UPDATE … SET Schwelle_Reserve = 10 WHERE Schwelle_Reserve IS NULL` |
| `SchemaMigration.cs:1562-1585` | `LeistungsgrenzeAnheben` — `UPDATE … SET Leistungsgrenze = 30 WHERE Leistungsgrenze = 0 OR Leistungsgrenze = 1` |
| `SchemaMigration.cs:248-251` | Zählwerk `DatenReserveVorbelegt`, `DatenLeistungsgrenzeAngehoben` |
| `SchemaMigration.cs:434-435`, `:605-609` | Rücksetzung und Berichtszeile |
| `SchemaMigration.cs:45-52` | Klassenkommentar um Schritt 13 erweitert (vierter DML-Schritt neben 5, 7, 9) |

**`Schwelle_Reserve` gehört in `SchemaKatalog.Alle`, und zwar zwingend.** Sie ist eine
EINGABEspalte und wird in der ausgeschriebenen SELECT-Liste von
`WaermesenkeClass.PufferLaden` gelesen; fehlt sie, scheitert die Abfrage und mit ihr der
ganze Lauf. Die Rückfallebene `WaermequelleClass.SchemaSicherstellen` läuft bei jedem
Simulationsstart und schließt genau diese Lücke — belegt an der Suite-Arbeitskopie
(Kapitel 7.4).

**Warum die Reserve vorbelegt wird (DML statt nur DDL).** `ADD COLUMN … DOUBLE` lässt
bestehende Zeilen auf NULL, und NULL hieße für den Rechenkern „keine Reserve" — eine
fachliche Aussage über jeden Bestandsspeicher, die niemand getroffen hat. Anders als bei
`Quellwaerme` (Schritt 10, eine Ergebnisgröße ohne Backfill) ist das hier ein PARAMETER.

**Idempotenz.** Beide `UPDATE` tragen ihre Einschränkung im `WHERE`; ein zweiter Lauf
findet keine Zeile mehr. Gemessen: Kapitel 7.1.

---

## 4. Etappe 3 — Leistungsgrenze und Einheiten-Fix

### 4.1 Fallback 50 % → 30 %

`SimulationBHKW.cs:233` — `if (bhkwGrenzleistungAllgemein == 0) … = 0.3f;` (vorher `0.5f`).

Der Fallback bleibt bestehen, obwohl Migrationsschritt 13 dieselben Sätze auf 30 hebt:
Eine Datenbank ohne gelaufene Migration soll nicht anders rechnen als eine mit. Migration
und Fallback nennen jetzt denselben Wert.

### 4.2 Einheiten-Bruch der Katalog-Grenzleistung — MITGEFIXT

`SimulationBHKW.cs:243-269`.

**Der Befund.** `bhkwGrenzL` ist ein FAKTOR (0,3 = 30 % Teillast) — so wird das Feld in
allen drei Motorläufen benutzt (`bhkwWaermeLeistung[motor] * bhkwGrenzL[motor]`). Der
ANLAGENwert aus `Tab_Energieanlagen.Grenzleistung` wird deshalb beim Laden durch 100
geteilt (`SimulationControl.BHKW_Liste_Laden:1462`), die projektweite Grenze ebenso
(`SimulationBHKW.cs:227`). Der KATALOGwert aus `Tab_BHKW.Grenzleistung` wurde es **nicht** —
er trägt aber dieselbe Einheit, nämlich Prozent.

**Die Folge** war ein Faktor 100 zu viel: Ein Katalog-BHKW mit 50 % Modulationsgrenze
rechnete mit `bhkwGrenzL = 50`. Da der Katalogwert den Anlagenwert überschreibt, sobald er
ungleich 0 ist, betraf das jedes Modul mit gepflegter Katalog-Grenzleistung. Die Bedingung
„`bhkwWaermeLeistung * bhkwGrenzL <= Wärmeraum`" war damit praktisch nie erfüllt — der
Teillastzweig fiel aus, das Modul lief nur Volllast oder gar nicht.

**Der Fix:** `bhkwGrenzL[i] = (float)(ctrl.m_Grenzleistung / 100.0)`.

### 4.3 Label

`Form_Simulation_Detail.resx:4596` — `label54.Text`
„Untere Leistungsgrenze der Module **[30%]**". `numericUpDown_UnteresteLG` (Minimum,
Maximum) blieb unangetastet.

---

## 5. Etappe 4 — `Schwelle_Reserve` durchgezogen

### 5.1 Die Kette

| Schicht | Datei:Zeile |
|---|---|
| Vorbelegungs-Konstante (Engine) | `Ladeordnung.cs:63-72` — `SCHWELLE_RESERVE_DEFAULT = 10.0` |
| Vorbelegungs-Konstante (Neuanlage) | `ProjektPuffer.cs:47-53` — dito, damit neu = migriert |
| DB → `PufferInfo` | `WaermesenkeClass.cs:154-160` (Feld), `:432/:470` (SELECT-Listen), `:480-490` (Mapping) |
| `PufferInfo` → Speicherobjekt | `SimulationControl.cs:1616` (Ersatz-Pendelspeicher) und `:2422` (Registry-Aufbau) — beide Stellen, Prozent → Anteil |
| Speicherobjekt | `SimulationPufferspeicher.cs:126-168` (`SchwelleReserve`, `BhkwReserveGilt`), `:687-723` (`EntnahmeObergrenze()`) |
| Wirkung | `Kaskadenschleife.cs:1155-1191` (Klemmung), `:670-698` (Scharfstellen) |
| Persistenz | `ProjektPuffer.cs:236-243` (SQL), `:256-318` (Parameter), `PufferSpCtrl.cs:349-405` |
| Oberfläche | `Form_PufferSp_Projekt.cs:63-69`, `:262-271`, `:518`, `:558`, `:1183-1195`, `:1204-1226` |
| Texte | `Resource.resx` + `Resource.Designer.cs`: `PSP_LABEL_MINDESTFUELLSTAND`, `PSP_NAME_MINDESTFUELLSTAND`, `PSP_FEHLER_RESERVE_UEBER_AUS` |

### 5.2 Die gewählte Wirkungsstelle — und warum

Der Auftrag ließ die Stelle offen („konzipiere die minimal-invasive Stelle selbst und
begründe sie"). Gewählt ist **eine** Stelle: `Kaskadenschleife.EntladeKanal`
(`Kaskadenschleife.cs:1175-1191`), eine Klemmung des ANGEFORDERTEN Bedarfs auf
`sp.EntnahmeObergrenze()`.

**Warum dort.** `EntladeKanal` ist die einzige Stelle, an der ein Speicher bedarfsdeckend
aus seinem VORRAT entlädt — Phase A (Vorabentladung) und Phase E (Nachentladung) laufen
beide durch sie.

**Warum nicht in `SimulationPufferspeicher.Entladen`** (`:534-558`): Das ist die
Speicherphysik aller vier Erzeugerarten und aller Phasen. Eine Untergrenze dort wäre die
globale Verhaltensänderung, die Entscheidung 3 ausdrücklich ausschließt. `Entladen` bleibt
Zeile für Zeile unverändert und kennt weiterhin nur die Grenze „Speicher leer".

**Warum am SPEICHER und nicht an der Entladung.** Die Entladung eines Speichers ist nicht
erzeugerbezogen: Ein Puffer wird entladen, weil Bedarf offen ist, nicht weil ein
bestimmter Erzeuger ihn geladen hat. Es gibt keinen „BHKW-Entladevorgang", den man einzeln
begrenzen könnte. Die Notreserve ist deshalb als das ausgedrückt, was sie fachlich ist:
eine Eigenschaft DES SPEICHERS — er soll nicht leerlaufen —, scharfgestellt genau an den
Speichern, auf die ein BHKW angewiesen ist (`BhkwAuftraegeZuordnen` →
`ReserveScharfstellen`, `Kaskadenschleife.cs:670-698`).

**Warum `Q_max` als Bezug und nicht `SOC`.** Die Reserve ist ein Anteil der NUTZBAREN
KAPAZITÄT — dieselbe Bezugsgröße wie Ein- und Abschaltschwelle. Ein Anteil des momentanen
Füllstands wäre keine feste Marke, sondern eine, die mit dem Leerlaufen mitwandert.

**Warum die Ladeseite unangetastet blieb.** Der Bilanzraum des BHKW
(`SimulationBHKW.ZweitsenkenRaum`, `Zweikanalig_Laden`) hat zwei Summanden: Ladefähigkeit
(Zielfüllstand) und Durchsatz (hydraulische Weiche). Die Reserve begrenzt den VORRAT, nicht
den Durchsatz: Steht der Füllstand über `Q_max` (Durchleitung derselben Stunde, Befund N6),
ist der Überhang vollständig entnehmbar, weil `SOC − Q_max · Reserve` dann größer ist als
der Überhang. `DurchsatzEntladen` (`Kaskadenschleife.cs:1073-1103`) entnimmt ausschließlich
diesen Überhang und ist deshalb bewusst **nicht** geklemmt. Messbeleg: Kapitel 7.5 — BHKW-
Produktion und Betriebsstunden bleiben bei geänderter Reserve exakt gleich.

**Verhaltensneutral ohne BHKW.** `EntnahmeObergrenze()` liefert `double.MaxValue`, solange
`BhkwReserveGilt` nicht gesetzt oder die Reserve 0 ist; `Math.Min` mit `MaxValue` gibt den
Bedarf unverändert zurück. Der Zweig `if (bedarf <= 0) { … }` ist nur bei aktiver Reserve
erreichbar, weil `bedarf > 0` oben bereits geprüft ist. Nachgewiesen in Kapitel 7.3.

**Nachladebetrieb.** Erreicht der Speicher die Reservemarke, setzt die Klemmung
`sp.LaedtGerade = true` (nur in Phase A) — derselbe Weg, den ein nicht ausreichender
Speicher am Schleifenende nimmt. Der Bedarf bleibt offen und wird von der nächsten
Kaskadenstufe bzw. vom Heizstab gedeckt, während das BHKW seinen Vorrat aufbaut.

### 5.3 Dokumentierte Abgrenzung: geteilter Speicher

Lädt außer dem BHKW noch ein anderer Erzeuger denselben Puffer, wirkt die Reserve auch auf
dessen Entladung — ein Speicher hat einen Füllstand, nicht zwei, und eine getrennte
Untergrenze je Herkunftsanteil wäre neue Physik. Der Regelfall ist der eigene BHKW-Puffer
(Migrationsregel R6 und `ProjektPuffer.SQL_BHKW_AUF_PUFFER` legen ihn genau so an). Steht
als **offener Punkt** in Kapitel 9.

### 5.4 Eingabeprüfung

`SchwelleLesen` hat einen Parameter `nullErlaubt` bekommen
(`Form_PufferSp_Projekt.cs:1204-1226`): Anders als die drei Schaltschwellen ist **0** bei
der Reserve eine gültige Angabe und bedeutet „dieser Speicher darf leergefahren werden".
Für die drei Schaltschwellen bleibt es beim Bereich „größer 0 bis 100" — der
Bedingungsausdruck ist so geschrieben, dass ihr Verhalten unverändert ist.

Plausibilität: `Reserve >= Schwelle_Aus` wird abgelehnt (`PSP_FEHLER_RESERVE_UEBER_AUS`) —
läge die Marke auf oder über dem Ladeziel, könnte der Speicher nie bedarfsdeckend entladen.

Dieselbe Trennung im Lesepfad (`WaermesenkeClass.cs:480-490`): Die drei Schwellen werden
mit `<= 0 → Default` behandelt, die Reserve **nicht** — nur ein FEHLENDER Wert (NULL) wird
vorbelegt, eine ausdrückliche 0 bleibt stehen.

### 5.5 Layout

Die Schwellenzeile der Gruppe „Eigenschaften" war über die volle Breite belegt, deshalb
eine vierte Zeile: `gbDaten` +32 px (200 → 232), `ClientSize` +32 px (616 → 648), alle
Elemente darunter um 32 px nachgerückt (`gbLaden`, Entladepriorität, Statuszeile, die zwei
Schaltflächen). Jede verschobene Zahl trägt den Vermerk `// +32`.

Das Feld steht an **jedem** Puffer, nicht nur an BHKW-Puffern: Der Anwender weiß beim
Anlegen nicht, welcher Erzeuger den Speicher später bedient, und eine Sichtbarkeitsregel
nach Erzeugerart hätte den Wert bei einer späteren Zuordnung stillschweigend entwertet.

---

## 6. Etappe 5 — Pendelspeicher-Feld ausgebaut

| Datei:Zeile | Änderung |
|---|---|
| `Form_Simulation_Detail.cs:1781-1786` | Aufruf `PendelspeicherFeldEinrichten()` entfernt |
| `Form_Simulation_Detail.cs:3604-3626` | Methode `PendelspeicherFeldEinrichten()` entfernt, Begründung an ihrer Stelle |
| `Form_Simulation_Detail.cs:3652-3656` | Leave-Handler `numericUpDown_Volumen_Leave` entfernt (der Schreibweg) |
| `Form_Simulation_Detail.Designer.cs:198`, `:364`, `:1525-1529`, `:1541`, `:2769`, `:2989` | `label56` und `numericUpDown_Volumen` aus Deklaration, `BeginInit`/`EndInit`, `Controls.Add`, Konfigurationsblock und Feldliste entfernt |

**Warum.** Ein Pendelspeicher ist ein Pufferspeicher. Ihn an zwei Stellen zu pflegen — hier
als bloße Literzahl, in der Pufferverwaltung als vollständiger Speicher mit Verwendung,
Temperaturpaar, Schaltschwellen und jetzt Notreserve — hieß, dass dieselbe Anlage zwei
Wahrheiten hatte.

**Was bleibt.** `PufferSpCtrl.PendelspeicherVolumenLiter` (`:707`) und
`SetPendelspeicherVolumenLiter` (`:737`) bleiben bestehen: Die Lesefunktion speist
`SimulationControl.VolumenPendelspeicherBHKW` (`SimulationRunner.cs:175`), und dieses Feld
entscheidet weiterhin über die Speicherbeteiligung eines BHKW ohne Puffer-Senke
(Altbestände aus Migrationsregel R6). Die Schreibfunktion nutzt die Migration. Nur der
Schreibweg über die Oberfläche ist entfallen.

---

## 7. Verifikation

### 7.1 Build, Tests, Migration

| Nachweis | Ergebnis |
|---|---|
| Prüfbuild `WindowsFormsApplication1` (Debug/x86), nach **jeder** Etappe | **0 Fehler / 6 Warnungen** — Baseline gehalten (CS0108×2, CS0109×2, CS4014, CS1998) |
| `dotnet test SpeicherEngine.Tests` | **337 von 337 grün**, 0 Fehler — Batterie-Engine unberührt |
| Referenzlauf-Werkzeug (Debug/x86) | 0 Fehler / 0 Warnungen (nach dem Nachzug aus Kapitel 8.1) |
| Migration Modus `migration`, DB-Kopie `%TEMP%\wpk6` | Exit **1**, Schemastand **12 → 13**, Schritt 13 „OK" |
| Migration, zweiter Lauf (Idempotenz) | Exit 1, Schritt 13 „**bereits erledigt**", keine Doppelvorbelegung |

Exit 1 beim Migrationsmodus ist der **bekannte Vorbefund** dieses Datenstands: zwei
Datenstand-Nachweise schlagen an (`PufferHeizung ohne WS_ID_Puffer: 2`,
`Anlagen ohne Ladeprio-Vorgabe: 1`). Beide sind Bestand, kein Regressionszeichen; die
Migration selbst meldet `ERFOLG   MigrationOk=True`.

**Migrationsmesswerte (Protokollzeilen):**

```
Schritt 13  BHKW-Regulär: Spalte Schwelle_Reserve, Vorbelegung 10 %, Leistungsgrenze 30 %: OK
        - Tab_Pufferspeicher: 1 Spalten angelegt, 0 bereits vorhanden
        - Schwelle_Reserve: 119 Pufferspeicher auf 10 % vorbelegt
        - Leistungsgrenze: 8 Einstellungssätze von 0 bzw. 1 auf 30 % angehoben
Schemastand nachher: 13   (Zielstand 13)
```

**Belege direkt aus der migrierten Kopie** (OLEDB, x86):

| Abfrage | Ergebnis |
|---|---|
| `Tab_Pufferspeicher` | 119 Zeilen, `Schwelle_Reserve = 10` in **119**, NULL in **0**, Min = Max = **10** |
| `Tab_Einstellungen` | 15 Zeilen, `Leistungsgrenze = 30` in **8**, `= 0 oder 1` in **0**, Min 10 / Max 30 |
| Projekt 1018 | `Leistungsgrenze = 30`, `Kaskade_Zweikanalig = **False**` |
| Puffer 1018007 | `Schwelle_Reserve = 10`, `Schwelle_Ein`/`Schwelle_Aus` leer (Rückfall 10/95) |

Die Produktiv-DB `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` wurde ausschließlich **gelesen
und kopiert**.

### 7.2 Referenzläufe der BHKW-Projekte

Beide mit **Exit 0**. Der neue Protokollhinweis erscheint in beiden Läufen:

> Das Projekt enthält ein BHKW — dieser Lauf rechnet deshalb IMMER über die Speicherstufe
> mit herausgelöster Ladephase (Konzept 6.3), unabhängig von der Projekteinstellung
> `Kaskade_Zweikanalig`. Der einkanalige BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).

**Das ist der Kernnachweis der Weiche:** Bei beiden Projekten steht
`Kaskade_Zweikanalig = False` in der Datenbank, und beide rechnen dennoch über die
Speicherstufe.

**Projekt 1018 — der Puffer rechnet regulär mit** (BHKW mit Puffer-Senke,
`WS_Ziel = PufferHeizung`, `WS_ID_Puffer = 1018007`):

| Größe | Wert |
|---|---|
| `Puffer.Ladung_gesamt` | **31.357,70 kWh** (> 0 — der geforderte Beleg) |
| `Puffer.Entladung_gesamt` | 30.619,69 kWh |
| `Puffer.Q_max` / `SOC_Mittel` / `Vollzyklen` | 10,44 kWh / 9,83 kWh / 3.003,61 |
| `Vektor.bhkw_waerme.Summe` | 25.334,30 kWh |
| `BHKW.Waermeproduktion` / `Waermebedarfsdeckung` | 25,33 MWh / 53,76 % |
| `BHKW.Waermebedarf` / `Restwaermebedarf` | 46,87 / 21,67 MWh |
| `BHKW.Betriebsstunden_Gesamt` | 654,63 h |
| `Sim.Speicher_Anzahl` | 1 |

`Tab_ErgebnisPufferspeicher` (Ergebnis-Kopf 170) führt den Puffer mit
`Ladung_gesamt = 31357.7`, `Entladung_gesamt = 30619.69`, `SOC_Mittel = 9.83`.

**Projekt 1017 — BHKW als VEKTORSTUFE** (`WS_Ziel = Heizkreis`, kein `WS_ID_Puffer`, kein
Pendelspeicher; die Migration meldet „Puffer 'BHKW-Pendelspeicher': 0"):

| Größe | Wert |
|---|---|
| `Vektor.bhkw_waerme.Summe` | **54.015,20 kWh** (> 0 — der geforderte Beleg) |
| `BHKW.Waermeproduktion` / `Waermebedarfsdeckung` | 54,02 MWh / 85,86 % |
| `BHKW.Waermebedarf` / `Restwaermebedarf` | 62,91 / 8,89 MWh |
| `BHKW.Betriebsstunden_Gesamt` | 2.842,90 h |
| `Sim.Speicher_Anzahl` | 0 (kein Puffer im Projekt) |

Das ist konsistent mit `_bhkwInSchleife`: ohne Speicher keine Speicherstufen-Mitgliedschaft,
aber zweikanalige Rechnung an der Kaskadenposition.

Beide BHKW-Anlagen tragen `Tab_Energieanlagen.Grenzleistung = 30`, was
`BHKW_Liste_Laden` zu 0,3 macht — deckungsgleich mit dem neuen Fallback und dem neuen
Label.

### 7.3 Regressionsnachweis für Projekte OHNE BHKW

Volle Suite: **9 von 9 erfolgreich**, Exit 0, Gesamtdauer 1:06.
Vergleich gegen `Referenzlaeufe/2026-08-16_B4`:

| Projekt | Ergebnis | Bewertung |
|---|---|---|
| 1021 | **PASS** (21 Dateien, 227.840 Werte) | byte-identisch — der stärkste Nachweis |
| 1023 | FAIL mit **genau 1** Abweichung von 262.918 Werten | die eine ist `HeizkesselModul[0].carrier_id` (Energieträger-Datenstand, keine Rechengröße); **alle** Ganglinien byte-identisch, inklusive WP, Kessel und Puffer |
| 1007 | 22 Dateien byte-identisch | **alle** Wärmereihen gleich (`waermebedarf*`, `wp_*`, `solar_*`, `heizstab`, `restwaerme`) |
| 1011 | 25 Dateien byte-identisch | dito, zusätzlich alle PV-Reihen außer den zwei umgelenkten |
| 1017, 1018, 1024 | FAIL | **absichtlich** — die drei BHKW-Projekte |
| 1008 / 1012, 1026 | FAIL („nur in einem Lauf") | Projektbestand der DB hat sich seit B4 geändert |

**Die Abweichungen bei 1007 und 1011 liegen ausschließlich in den PV-/Stromspeicherreihen**
(`pv_speicherfuellstand.csv`, `ssp_gespeichert_viertelstunde.csv`,
`reststrom_viertelstunde.csv`, bei 1007 zusätzlich `pv_produktion/_reststrom/_ueberschuss`)
und in `aggregate.csv`. Sie sind **nicht** diesem Paket zuzurechnen:

- `git diff` dieses Pakets berührt in `SimulationControl.cs` keine Zeile mit
  `Stromspeicher`, `Speicherfuellstand`, `simulation_pv`, `bSimulationSSP` oder
  `Simulation_Photovoltaik` (verifiziert),
- `SimulationPV.cs`, `SimulationStrombedarf.cs`, `SpeicherEngine`, `StromspeicherSimCtrl`
  stehen **nicht** in der Liste der geänderten Dateien,
- die zwei Speicher-CSVs sind die in Kapitel 8.1 umgelenkten Werkzeugspalten.

Ursache ist das Arbeitspaket **AP2b/AP3 Stromspeicher**, das nach dem Einfrieren von B4
eingebaut wurde und im Arbeitsbaum ungestaged vorliegt.

### 7.4 Rückfallebene der neuen Spalte

Die Suite läuft auf ihrer eigenen Arbeitskopie
`Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`. Nach dem Lauf: `SchemaVersion = 13`,
`Tab_Pufferspeicher` 119 Zeilen, `Schwelle_Reserve` NULL in **0**, Min = Max = **10**. Der
Weg über `SchemaKatalog.Alle` und die Migration greift also auch dort.

### 7.5 Wirkungsnachweis der Notreserve (präpariert)

Mit der Vorbelegung 10 % ist die Reserve auf **keinem** der beiden BHKW-Projekte messbar:
Puffer 1018007 steht über alle 8760 Stunden konstant auf `SOC = 9,8349 kWh` (94 % von
`Q_max = 10,44`) und arbeitet mit 3.003 Vollzyklen als hydraulische Weiche, nicht als
Vorratsspeicher — die Marke von 1,044 kWh wird nie erreicht.

Deshalb ein präparierter Lauf auf der Arbeitskopie: `Schwelle_Reserve` des Puffers 1018007
auf **90 %** (Marke 9,396 kWh), danach zurück auf 10.

| Größe | Reserve 10 % | Reserve 90 % |
|---|---|---|
| `Puffer.Entladung_gesamt` | 30.619,69 kWh | **5.441,21 kWh** (−82 %) |
| `Puffer.Ladung_gesamt` | 31.357,70 kWh | 6.179,22 kWh |
| `Puffer.Vollzyklen` | 3.003,61 | 591,88 |
| `Puffer.SOC_Mittel` | 9,8349 | 9,8349 (Marke gehalten: 9,8349 > 9,396) |
| `BHKW.Waermeproduktion` | 25,33 MWh | **25,33 MWh** (unverändert) |
| `BHKW.Betriebsstunden_Gesamt` | 654,63 h | **654,63 h** (unverändert) |
| `Vektor.bhkw_waerme.Summe` | 25.334,3033 | **25.334,3033** (bitgleich) |
| `BHKW.Restwaermebedarf` | 21,67 MWh | 21,68 MWh |

Die Reserve wirkt also **messbar auf die Entladung** und **nachweislich nicht auf die
Fahrweise** — genau die Konstruktion aus Kapitel 5.2. Die kleinen Verschiebungen bei
Restbedarf und Deckung stammen aus der geänderten Entladungszurechnung
(Interimsregel „Vermischung im Speicher").

---

## 8. Nebenbefunde

### 8.1 Referenzlauf-Werkzeug war nicht mehr übersetzbar (fremde Ursache)

`Referenzlauf/Ergebnisexport.cs` greift auf zwei Größen zu, die das Stromspeicher-Paket
AP2b abgelöst hat: `SimulationPV.Speicherfuellstand` und
`SimulationControl.simulation_ssp`. Beide fehlen **bereits im HEAD-Stand** (verifiziert:
`git show HEAD:…SimulationControl.cs | grep -c simulation_ssp` → 0). Das Projekt liegt
nicht in der `.sln` und wurde deshalb nicht mitgezogen.

**Ohne Nachzug war kein Referenzlauf möglich** — also kein Verifikationsnachweis für
dieses Paket. Der Eingriff bleibt auf die Umlenkung der Spaltenquellen beschränkt
(`Ergebnisexport.cs:144-166`):

- `pv.Speicherfuellstand` → `sim.Speicherfuellstand_stuendlich`
- `sim.simulation_ssp.Stromgespeichert` → `sim.Speicherfuellstand_viertelstuendlich`

Die Dateinamen bleiben unverändert, damit der Vergleich mit älteren Referenzständen möglich
bleibt. **Fachlich zu prüfen:** Ob `ssp_gespeichert_viertelstunde.csv` künftig den
Füllstand oder eine Lademenge führen soll, ist eine Entscheidung des Stromspeicher-Pakets,
nicht dieses Pakets.

### 8.2 `Leistungsgrenze IS NULL` bleibt unangetastet

Projekt 1017 trägt `Leistungsgrenze = NULL`, nicht 0 oder 1 — die Migration hebt es
deshalb **nicht** an. Entscheidung 2 nennt ausdrücklich „0 ODER 1", und der Wortlaut wurde
nicht eigenmächtig erweitert. **Rechnerisch ist es folgenlos:**
`KonfigurationCtrl.ReadSingle:66` lässt `model.Leistungsgrenze` bei NULL auf 0, und der
Engine-Fallback macht daraus 30 % — denselben Wert. Ob NULL mit in die
`WHERE`-Bedingung soll, ist eine Anwenderentscheidung (Kapitel 9).

### 8.3 `bhkwGrenzleistungAllgemein` mutiert weiter ein öffentliches Feld

`SimulationBHKW.cs:227` teilt das public-Feld in place durch 100 (Befund B-2). Der Auftrag
erlaubte, es „bei Gelegenheit lokal zu machen, wenn risikofrei" — es ist **nicht**
risikofrei: `Fahrweise_Stunde` gibt das Feld an `Motorlauf_Stromgefuehrt` und
`Motorlauf_OhneEinspeisung` weiter. Eine Kumulation über mehrere Läufe ist ausgeschlossen,
weil `SimulationControl` das Feld vor jedem Lauf neu aus `GrenzleistungBHKW` setzt
(`:1188`, `:1483`). Bleibt offen.

### 8.4 Verwaiste `.resx`-Einträge

`Form_Simulation_Detail.resx` führt weiterhin die Layout-Einträge zu `label56` und
`numericUpDown_Volumen` (`:4403-4457`). Sie sind ohne Wirkung — `ApplyResources` wird für
sie nicht mehr gerufen. Bewusst **nicht** entfernt: Die `>>…ZOrder`-Einträge stehen in
einer Ordnungskette der Geschwisterelemente, und ein Eingriff in eine 9.259-zeilige
`.resx` hat mehr Risiko als Nutzen. Der Designer räumt sie beim nächsten Öffnen selbst auf.

### 8.5 Vorgefundener Arbeitsbaum

Der Arbeitsbaum war vor diesem Paket bereits ungestaged geändert (Stromspeicher AP2b/AP4,
Abnahme-Runde 4 „Kartenschalter", Konfigurations-UI-Karten). Von diesem Paket **nicht**
angefasst: `Umsetzungskonzept_Stromspeicher_EPOS-Plan.md`, `ErzeugerKarte.cs`,
`SpeicherKarte.cs`, `Form_Simulation_Config*.cs`, `Resource.en-US.resx` und die
Detailansicht-Änderungen jenseits der drei Pendelspeicher-Stellen.

---

## 9. Offene Punkte

| Nr. | Punkt | Bewertung |
|---|---|---|
| 1 | **Neue Referenzbasis einfrieren.** `2026-08-16_B4` ist für 1017, 1018 und 1024 absichtlich obsolet; zusätzlich hat sich der Projektbestand der DB geändert (1008 weg, 1012 und 1026 neu) und AP2b hat die PV-/Speicherreihen verschoben. Ein Einfrieren war aus dem Auftrag ausgeschlossen | offen, für den Anwender |
| 2 | **Anzeige-Altformeln** `Form_Simulation_Detail.cs:2953-2960` sind nicht nachgezogen — die Ergebnisseiten rechnen dort weiter mit den alten Ausdrücken. Bewusst außerhalb des Auftrags | offen |
| 3 | **Notreserve bei geteiltem Speicher** (Kapitel 5.3): Lädt ein zweiter Erzeuger denselben Puffer, wirkt die Reserve auch auf dessen Entladung. Eine Untergrenze je Herkunftsanteil wäre neue Physik | Anwenderentscheidung |
| 4 | **`Leistungsgrenze IS NULL`** in die Migrationsbedingung aufnehmen? (Kapitel 8.2) — rechnerisch folgenlos | Anwenderentscheidung |
| 5 | **`ssp_gespeichert_viertelstunde.csv`** führt nach dem Werkzeug-Nachzug den Füllstand statt einer Lademenge (Kapitel 8.1) | gehört ins Stromspeicher-Paket |
| 6 | **Sommerbetrieb/Notschaltungen** sind gelöscht, nicht reaktiviert. Wenn die Betriebsartenweiche fachlich gewollt ist, ist sie neu zu entwerfen — die Altbedingung `stunde < 3600 \|\| stunde > 5760` steht als Spur im Kommentar | offen |
| 7 | **`bhkwGrenzleistungAllgemein`**-Mutation (Kapitel 8.3) | offen |
| 8 | **Zwei Datenstand-Vorbefunde** der Produktiv-DB: `PufferHeizung ohne WS_ID_Puffer: 2`, `Anlagen ohne Ladeprio-Vorgabe: 1` — sie halten den Migrationsmodus auf Exit 1 | Bestand |

---

## 10. Geänderte Dateien

| Datei | Umfang |
|---|---|
| `Allgemein/Simulation/SimulationControl.cs` | Weiche, Altpfad-Rückbau, `SchwelleReserve` an zwei Übertragungsstellen, Kommentare |
| `Allgemein/Simulation/SimulationBHKW.cs` | Altpfad entfernt (−313 Zeilen), tote Zweige, Fallback 0,3, Katalog-/100-Fix |
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | `SchwelleReserve`, `BhkwReserveGilt`, `EntnahmeObergrenze()` |
| `Allgemein/Simulation/Kaskadenschleife.cs` | Klemmung in `EntladeKanal`, `ReserveScharfstellen` |
| `Allgemein/Simulation/Ladeordnung.cs` | `SCHWELLE_RESERVE_DEFAULT` |
| `Allgemein/Simulation/WaermesenkeClass.cs` | `PufferInfo.SchwelleReserve`, zwei SELECT-Listen, Mapping |
| `Allgemein/Update/SchemaKatalog.cs` | `SPALTE_SCHWELLE_RESERVE`, `Schritt13_Mindestfuellstand`, `Alle` |
| `Allgemein/Update/SchemaMigration.cs` | `ZIEL_VERSION = 13`, Schritt 13 mit zwei DML-Teilen, Zählwerk |
| `Allgemein/Update/ProjektPuffer.cs` | SQL und Parameter um `Schwelle_Reserve`, `SCHWELLE_RESERVE_DEFAULT` |
| `Controller/PufferSpCtrl.cs` | zwei Signaturen um `schwelleReserve` (mit Vorbelegung) |
| `Views/Pufferspeicher/Form_PufferSp_Projekt.cs` | viertes Schwellenfeld, Layout +32 px, Prüfung |
| `Views/Simulation/Form_Simulation_Detail.cs` | Pendelspeicher-Feld ausgebaut (drei Stellen) |
| `Views/Simulation/Form_Simulation_Detail.Designer.cs` | `label56` und `numericUpDown_Volumen` entfernt |
| `Views/Simulation/Form_Simulation_Detail.resx` | `label54.Text` → „[30%]" |
| `MyResource/Resource.resx` + `Resource.Designer.cs` | drei neue Einträge |
| `Referenzlauf/Ergebnisexport.cs` | Nachzug fremder Ursache (Kapitel 8.1) |

**Encoding je Datei erhalten** (vor und nach jedem Edit geprüft): alle `.cs` und
`Form_Simulation_Detail.resx` **UTF-8 mit BOM + CRLF**; `Resource.resx` und
`Resource.Designer.cs` **UTF-8 mit BOM + reine LF**. Bei einem zeilenbasierten Eingriff in
`SimulationBHKW.cs` fiel das BOM einmal weg und wurde sofort wieder eingesetzt; der
Endstand ist geprüft.
