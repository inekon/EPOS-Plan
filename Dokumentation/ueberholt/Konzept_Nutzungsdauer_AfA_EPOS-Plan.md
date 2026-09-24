# Konzept: Nutzungsdauer je Technik und Positionsart aus einer AfA-Tabelle

> **Überholt seit #474, S1–S3 gebaut (#269, #357, #463), A8-Halbsatz #474.** Dieses Konzept liegt als Geschichte
> unter `ueberholt/` (Regel der Statusdatei, Nach #264: „Konzept wandert nach `ueberholt/`, sobald S3 abgeschlossen
> ist"). Was gilt, steht im
> [Konzept Wirtschaftlichkeit](../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
> (§ 2.13 (3), § 3.4, § 6.5) und im Register (R‑ND, R‑A A8, R‑E10); der Text unten ist der Stand vom 24.09.2026 vor der
> Verschiebung, nachgezogen allein um diesen Kopf, die Verweise und die Zeilen zu E10 und A8.

Stand 24.09.2026 — Anwenderentscheid 14.09.2026: ND-Q1 bis ND-Q8 nach Empfehlung. **Die Stufen S1, S2 und
S3 sind umgesetzt** — S3 (Instandsetzung, Wartung, Gerätekataloge, dazu Speicherflotte und Kennzeichnung der
Gerätespalten) mit **#463**, der Etappe **E10** des Etappenplans E0–E12 im Analysepapier
[`Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md) § 5.
Den eigenen Entscheid zu S3 vertreten die Fragen E10‑Q1 bis E10‑Q7 im
[Entscheidungsregister](../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md) (R‑E10): gebaut
ist jeweils die Empfehlung, **entschieden am 24.09.2026 nach Empfehlung** (E10‑Q5 erledigt). Der Halbsatz aus A8 — eine
neue Speichervariante nimmt die Nutzungsdauer der Standardzeile „Stromspeicher · Batterie" — ist mit **#474** (E13)
gebaut, nur als Vorgabe neuer Einträge. Codestand `4b50b77b`, `SchemaStand.Zielversion` = **123** (Schritt 120 sät die
Instandsetzungssätze der Tabelle; 121 bis 123 gehören anderen Feldern).

**Anlass (Anwenderwunsch 14.09.2026, Bildschirmfoto der Kostenverwaltung):** „In allen Kostendialogen
soll die Nutzungsdauer nach Technik/Kategorie standardmäßig vorbelegt werden können. Grundlage ist eine
eigene AfA-Tabelle (Absetzung für Abnutzung / Nutzungsdauer), die auch editierbar sein soll, unter
Administration an geeigneter Stelle."

## 0. Das Ergebnis in fünf Sätzen

1. Die Nutzungsdauer ist heute ein freies Feld je Kostenposition (Vorlage und Projekt); die Auslieferungs-
   vorlagen lassen es leer, und ein leeres Feld rechnet still „wie Betrachtungszeitraum" — ohne Ersatz-
   beschaffung und ohne Restwert.
2. Vorgeschlagen wird eine editierbare Tabelle **Nutzungsdauern** (im Programm „Nutzungsdauern (AfA)")
   je Technik (die zehn Kostenkomponenten) und Positionsart (Wärmeerzeuger, Abgasanlage, MSR, …) mit einer
   Standardzeile je Technik, gesät mit Richtwerten nach VDI 2067 Blatt 1 und mit Quellenangabe.
3. Jede Position kennt ihre Positionsart (neue Spalte mit Rückfall auf den Technik-Standard); neue
   Positionen werden damit vorbelegt, bestehende auf Knopfdruck („Nutzungsdauern vorbelegen") — nie
   still, damit sich keine gerechnete Wirtschaftlichkeit unbemerkt ändert.
4. Die Tabelle bekommt einen eigenen Menüpunkt unter Administration → Kostenverwaltung neben Kosten-
   vorlagen und Energieträgern; Auslieferungszeilen sind im Wert editierbar, aber nicht löschbar.
5. Referenzlauf und Bestandsprojekte bleiben unverändert, weil kein gespeicherter Wert automatisch
   ersetzt wird; drei Stufen (Tabelle und Dialog → Vorbelegung in den Kostendialogen → Instandsetzungs-
   und Wartungssätze) mit Fragen ND-Q1 bis ND-Q8 in Abschnitt 4.

## 1. Ist-Stand

### 1.1 Wo die Nutzungsdauer liegt

| Ort | Spalte | Bedeutung |
|---|---|---|
| `Tab_KostenVorlagePosition` | `Nutzungsdauer` (REAL, NULL erlaubt) | Vorlagenzeile je Komponente und Kategorie; alle 120 Auslieferungszeilen tragen NULL |
| `Tab_ProjektWerte` | `Nutzungsdauer`, `Worstcase_Nutzungsdauer`, `Bestcase_Nutzungsdauer` | Projektposition mit Szenarien; in der Testdatenbank 166 von 175 Zeilen mit 0 |
| Gerätekataloge (`Tab_BHKW`, `Tab_Heizkessel`, `Tab_StromspeicherVariante`) | eigene Nutzungsdauer-Spalten | technische Gerätedaten, ohne Bezug zur Kostenrechnung |

Die Vorlagenzeile ist als „VDI-2067-Nutzungsdauer [a] als Vorbelegung" gedacht
(`EPOS.Kern/Allgemein/Update/SchemaKatalog.cs`); die Saat lässt sie bewusst leer: „Normwerte werden
nicht erfunden." Eine Vorgabe von 15 Jahren, wie sie das Bildschirmfoto zeigt, hat im Quelltext und in
der Saat keinen Fundort — sie stammt aus einer vom Anwender gepflegten Vorlage (Prüfbitte, ND-Q8).

### 1.2 Rechenwirkung

`EPOS.Kern/Allgemein/Wirtschaftlichkeit/KapitalwertRechner.cs`: Eine Nutzungsdauer unter einem Jahr gilt
als „wie Betrachtungszeitraum T" — keine Ersatzbeschaffung, kein Restwert. Ab einem Jahr löst sie
Ersatzbeschaffungen bei n, 2n, … aus und einen linearen Restwert am Ende von T. Worst- und Best-Fall
rechnen mit ihrer eigenen Nutzungsdauer (Vorgabe ± 2 Jahre). Der Betrachtungszeitraum (Vorgabe
20 Jahre) ist eine Projektgröße, keine Positionsgröße.

### 1.3 Dialoge, die die Nutzungsdauer zeigen

- Kostenverwaltung (Administration, Vorlage) und Kostenverwaltung im Projekt: Spalte „Nutzungsdauer [a]"
  im Zeilenraster, nur bei Investitionskosten (`VorlagenZeile.razor`, `KostenPositionZeile`).
- Zeileneditor „Position bearbeiten": bewusst ohne Nutzungsdauer (sie bleibt im Raster).
- Worst/Best (`CaseEingabeDialog`): Nutzungsdauer je Szenario.
- Berichte & Kosten › Übersicht und Bericht: Anzeige, keine Eingabe.

### 1.4 Technik und Kategorie im Datenmodell

- `Tab_KostenKomponente`: zehn Techniken — Wärmepumpe, Heizkessel, Photovoltaik, Solarthermie,
  Stromspeicher, Pufferspeicher, BHKW, Wärmezentrale, Bauliche Anlagen, Stromeinspeisung.
- `Tab_KostenVorlage`: je Technik eine Vorlage für Investition (Kategorie 1) und Betrieb (Kategorie 2).
- `Tab_KostenVorlagePosition`: die Positionen einer Vorlage, überwiegend Freitext — 102 von 120 ohne
  Bezug zum flachen Positionslexikon `Tab_Kostenfaktor`, das selbst keine Technik kennt.
- Eine **Positionsart** („Wärmeerzeuger", „Abgasanlage", „MSR", „Montage", „Planung", „Bauliche
  Anlagen") gibt es nicht als Feld; sie steckt nur im Namen der Position. Beispiel Heizkessel/Investition:
  Wärmeerzeuger (Kessel), Zubehör, MSR-Technik / Automation, Abgasanlage / Schornstein, Montage und
  Installation, Bauliche Anlagen, Planung / Baunebenkosten.

### 1.5 Administration und Wächter

Administration → Kostenverwaltung führt „Kostenvorlagen" und „Energieträger" (`Menuetabelle.cs`). Die
Menüwache zählt 59 Punkte, 8 Trenner, 13 klappbare und 46 nicht klappbare Knoten; ein neuer Punkt
verschiebt diese Zahlen. Schemastand: **Zielversion 120** (Stand 24.09.2026), Schritte 90–120
vergeben (120 = die Sätze der Nutzungsdauertabelle, S3), **neue ab 121**; neue Fachtabellen sind STRICT und laufen
als nummerierter Schritt über `SchemaMigration`.

### 1.6 VDI 2067 im Bestand

Die Empfehlungsbereiche nach VDI 2067 stehen an der **Position einer Kostenvorlage**
(`Tab_KostenVorlagePosition`, Spalten `Empfehlung_von`/`Empfehlung_bis`, gesät aus
`SchemaKatalog.Schritt39_Vorlagen`): über die Vorlagenpflege änderbar und als Hinweis am Satzfeld des
Komponenten-Kostendialogs sichtbar. Instandsetzungs- und Wartungssätze je Technik (VDI 2067 Blatt 1,
Tabelle A2) gab es im Datenmodell nicht; **mit S3 (#463)** führt sie die Tabelle 2.2, gesät aus eben diesen
Empfehlungsbereichen (Abschnitt 3).

## 2. Zielbild

### 2.1 Begriffe

Die steuerliche **AfA** (Tabelle des Bundesfinanzministeriums für allgemein verwendbare Anlagegüter)
und die **rechnerische Nutzungsdauer** nach VDI 2067 Blatt 1 sind zwei Größen. Der Rechenweg von
EPOS-Plan (Kapitalwert mit Ersatzbeschaffung und Restwert) braucht die rechnerische Nutzungsdauer. Die
Tabelle heißt im Programm deshalb „Nutzungsdauern (AfA)", trägt je Zeile ihre Quelle und kann eine
steuerliche Spalte zusätzlich führen (ND-Q1).

### 2.2 Die Tabelle `Tab_Nutzungsdauer`

| Spalte | Typ | Bedeutung |
|---|---|---|
| `ID` | INTEGER PRIMARY KEY | Schlüssel |
| `KomponentenID` | INTEGER, FK `Tab_KostenKomponente`, NULL erlaubt | Technik; NULL = technikübergreifend (Bauliche Anlagen, Planung) |
| `Positionsart` | TEXT | Wärmeerzeuger, Brenner, Abgasanlage, MSR, Hydraulik/Zubehör, Montage, Planung, … |
| `IstStandard` | INTEGER CHECK (0,1) | genau eine Standardzeile je Technik — Rückfall, wenn eine Position keine Positionsart trägt |
| `Nutzungsdauer_a` | REAL | rechnerische Nutzungsdauer in Jahren |
| `AfA_steuerlich_a` | REAL, NULL erlaubt | steuerliche Nutzungsdauer, nur Anzeige (ND-Q1) |
| `Instandsetzung_Prozent`, `Wartung_Prozent` | REAL, NULL erlaubt | VDI 2067 Blatt 1, Tabelle A2 — Stufe S3 (ND-Q6): % der Investition je Jahr, im Dialog sichtbar und gepflegt, an den Standardzeilen gesät (Schritt 120, #463); NULL = kein Satz |
| `Quelle` | TEXT | „VDI 2067 Blatt 1, Tab. A2", „AfA-Tabelle AV", „eigener Wert" |
| `ReadOnly` | INTEGER CHECK (0,1) | Auslieferungszeile: Wert editierbar, Zeile nicht löschbar (ND-Q5) |
| `Sortierung` | INTEGER | Reihenfolge im Dialog |

Eindeutig ist (`KomponentenID`, `Positionsart`). STRICT, Schema-Schritt mit Saat, Testdatenbank auf die
neue Zielversion, Auslieferungsvorlage behält die `ReadOnly`-Zeilen.

### 2.3 Zuordnung Position → Zeile

`Tab_KostenVorlagePosition` und `Tab_ProjektWerte` bekommen die Spalte `NutzungsdauerID` (FK,
NULL erlaubt). Fehlt sie, gilt die Standardzeile der Technik der Position (über `VorlageID` bzw.
`KomponentenID`). Die Saat ordnet den 120 Auslieferungspositionen ihre Zeile über den Positionsnamen
zu (Abbildungstabelle im Schema-Schritt, z. B. „Wärmeerzeuger (Kessel)" → Wärmeerzeuger; „Montage und
Installation" → Montage). Der Vorlagenübernahme-Weg kopiert `NutzungsdauerID` in die Projektposition.

### 2.4 Vorbelegung in den Kostendialogen

1. **Neue Position** (Kostenverwaltung in Administration und Projekt, Neuzeile und Fußknopf): die
   Nutzungsdauer wird aus der Zeile der Positionsart, sonst aus dem Technik-Standard vorbelegt; eine
   Herleitungszeile unter dem Raster nennt die Quelle („Nutzungsdauer aus Nutzungsdauern (AfA): Heizkessel ·
   Wärmeerzeuger 20 a").
2. **Bestehende Positionen**: Knopf „Nutzungsdauern vorbelegen…" in der Leiste unter dem Raster, neben
   „+ Position hinzufügen" — er füllt die LEEREN Felder und nennt die Zahl in der Statuszeile; tragen
   danach noch Positionen einen Wert, der von der Vorgabe abweicht, folgt die Rückfrage mit ihrer Anzahl.
   Ohne Bestätigung bleibt jeder gepflegte Wert stehen. Nichts geschieht still; geschrieben wird erst mit
   „Speichern"/„OK" wie bei jeder anderen Eingabe.
3. **Positionsart je Zeile**: eine Klappliste im Zeileneditor „Position bearbeiten" (Technik ist durch die
   Vorlage bekannt, die Liste zeigt die Zeilen dieser Technik plus die technikübergreifenden, dazu eine
   leere Zeile „keine — Standardzeile der Technik"). Eine Änderung der Positionsart setzt die
   Nutzungsdauer neu — als einmalige Kopie, wie bei der Katalogübernahme der Energieträger. Ohne die
   Tabelle steht das Feld gar nicht erst da.
3a. **Herkunft je Zeile**: Unter dem Nutzungsdauerfeld des Rasters steht eine leise Zeile
   „15 a · Vorgabe der Technik" — sonst „… · AfA-Tabelle: ‹Positionsart›" oder „… · eigener Wert".
   Gemessen wird am WERT, nicht an einer Merkspalte: Der gepflegte Wert gilt als übernommen, solange er
   der Vorgabe entspricht. Ohne gepflegte Dauer bleibt die Zeile weg; den Grund nennt die Tafel
   „Ersatz und Restwert" (Abschnitt 2.4a).
4. **Vorlagenübernahme ins Projekt**: Vorlagenwert, sonst Wert der Tabelle; Worst/Best unverändert
   (± 2 Jahre um den Wert).
5. Die Gerätekataloge (BHKW, Heizkessel, Stromspeicher) behalten ihre eigenen Nutzungsdauer-Spalten;
   eine Vorbelegung dort ist Stufe S3 (ND-Q7). **Mit S3 (#463):** Die Spalten von BHKW und Heizkessel heißen
   „Nutzungsdauer (Gerätedaten)" und rechnen nicht (A8); ein neuer Kesseleintrag mit der Wartungseinheit „%/a" und
   ohne Betrag übernimmt den Wartungssatz der Standardzeile Heizkessel als einmalige Kopie, bestehende Einträge und das
   BHKW (€/kWh el) bleiben (E10‑Q2, E10‑Q6).

### 2.4a Tafel „Ersatz und Restwert" im Kostendialog

Unter dem Raster der Investitionskosten steht im Projektmodus eine Gruppe „Ersatz und Restwert": je
Position Betrag, Nutzungsdauer n, die Ersatzjahre innerhalb T mit dem Barwert der Ersatzbeschaffungen,
der Restwert im Jahr T nominal und als Barwert und die Herkunft der Dauer; zuletzt eine Summenzeile der
Komponente („3 von 4" Positionen mit Dauer). Erlös- und Zuschusszeilen bleiben außen vor — sie
bekommen im Rechenkern weder Ersatz noch Restwert (K5).

**Gerechnet wird nichts Neues.** Ersatzkette und linearer Restwert stehen als eigene Funktion im
`KapitalwertRechner` (`Ersatz`, `Barwert`), die die Kapitalwertrechnung selbst durchläuft; die Tafel
ruft dieselbe Funktion. Parameter sind i, T und das wirksame p_I der Gruppe. Die Tafel ist reine
Anzeige — der Referenzlauf bleibt byte-gleich.

**Über der Tafel** steht der Hinweis nach Konzept Wirtschaftlichkeit § 2.13 (3):
„Betrachtungszeitraum {T} a über der Vorgabe {n} a der Technik {Technik}; {k} von {m}
Investitionspositionen ohne Nutzungsdauer ({Liste}): kein Ersatz, kein Restwert gerechnet. Ist ein
Ersatz fällig, ist der Kapitalwert zu günstig ausgewiesen." Er ist ein Prüfauftrag, keine
Fehlermeldung, und bleibt weg, wo es nichts zu prüfen gibt.

### 2.5 Administrationsdialog „Nutzungsdauern (AfA)"

Menüpunkt unter Administration → Kostenverwaltung als dritter Eintrag. Razor-Dialog nach dem Muster der
Katalogverwaltungen: Zeilenraster, gruppiert nach Technik (Standardzeile zuerst), Spalten Technik ·
Positionsart · Nutzungsdauer [a] · Quelle · (AfA steuerlich [a]) · Aktionen, mit S3 (#463) dazu Instandsetzung [%/a]
und Wartung [%/a] in Tabelle und Neuzeile samt einer leisen Zeile zur Quelle; Suchfeld über alle Felder;
Knöpfe „Neu", „Löschen" (nur Zeilen ohne `ReadOnly`), „Auslieferungswerte wiederherstellen" (setzt die
Werte der `ReadOnly`-Zeilen auf die Saat zurück, mit Rückfrage), „Speichern"/„OK". Ein Kern-Controller
`NutzungsdauerCtrl` (Laden, Speichern, `Vorgabe(komponentenId, nutzungsdauerId)`) trägt die Datenbank-
seite; die Hülle bleibt plattformfrei in `EPOS.UI.Daten`.

### 2.6 Startwerte der Saat

Richtwerte in Jahren, gerundet, als **Vorschlag zur Bestätigung durch den Anwender** (ND-Q8): die
Spalte „VDI 2067" folgt Blatt 1, Tabelle A2 (rechnerische Nutzungsdauer), die Spalte „AfA" der
steuerlichen AfA-Tabelle für allgemein verwendbare Anlagegüter, wo diese eine Position kennt.

| Technik | Positionsart | VDI 2067 [a] | AfA [a] |
|---|---|---|---|
| Heizkessel | Wärmeerzeuger (Standard) | 20 | — |
| Heizkessel | Brenner | 12 | — |
| Heizkessel | Abgasanlage / Schornstein | 25 | — |
| Heizkessel | MSR / Automation | 12 | — |
| Heizkessel | Hydraulik / Zubehör | 20 | — |
| BHKW | Modul (Standard) | 15 | 10 |
| BHKW | Abgasanlage, Hydraulik, MSR | 20 / 20 / 12 | — |
| Wärmepumpe | Gerät (Standard) | 18 | — |
| Wärmepumpe | Erdsonden / Erdkollektor | 40 | — |
| Wärmepumpe | MSR / Hydraulik | 12 / 20 | — |
| Solarthermie | Kollektoren (Standard) | 20 | 10 |
| Solarthermie | Speicher, Hydraulik | 20 / 20 | — |
| Photovoltaik | Module (Standard) | 25 | 20 |
| Photovoltaik | Wechselrichter | 12 | — |
| Photovoltaik | Unterkonstruktion | 25 | — |
| Stromspeicher | Batterie (Standard) | 10 | 10 |
| Stromspeicher | Wechselrichter / Leistungselektronik | 12 | — |
| Pufferspeicher | Speicher (Standard) | 20 | — |
| Wärmezentrale | Rohrleitungen (Standard) | 40 | — |
| Wärmezentrale | Pumpen, Armaturen | 15 | — |
| Stromeinspeisung | Netzanschluss (Standard) | 30 | — |
| Bauliche Anlagen | Bauliche Anlagen (Standard) | 40 | — |
| technikübergreifend | Montage, Planung / Baunebenkosten | Wert der Hauptposition | — |

„Wert der Hauptposition" bedeutet: Montage- und Planungspositionen tragen keinen eigenen Ersatz; die
Vorbelegung nimmt die Nutzungsdauer der Standardzeile derselben Technik (die Position wird mit ihr
ersetzt). Die Gerätekataloge (Tabelle 1.1) bleiben davon unberührt.

### 2.7 Rechenwirkung und Referenzlauf

Kein gespeicherter Wert ändert sich von selbst; die 13 Referenzprojekte behalten ihre Positionen, der
Referenzlauf bleibt byte-gleich. Wer eine leere Nutzungsdauer über den Knopf oder eine neue Position
füllt, ändert den Kapitalwert bewusst: statt „wie Betrachtungszeitraum" rechnen Ersatzbeschaffung und
Restwert nach VDI 2067. Die Herleitungszeile und der Bericht nennen die Quelle des Werts.

## 3. Stufenplan

| Stufe | Umfang | Nachweis |
|---|---|---|
| **S1 Tabelle und Verwaltung** — **umgesetzt** | Schema-Schritt (Tabelle, Saat, Spalten `NutzungsdauerID` mit Saat-Zuordnung, Zielversion +1, Testdatenbank), `NutzungsdauerCtrl`, Razor-Dialog mit plattformfreier Hülle, Menüpunkt (Wächter 59 → 60, 46 → 47), Ressourcen de/en, Auslieferungsvorlage | Kern-/bunit-Tests, SqlDialektPruefer, Referenzlauf 13/13 byte-gleich |
| **S2 Vorbelegung** — **umgesetzt** | Neue Position, Knopf „Nutzungsdauern vorbelegen…", Positionsart im Zeileneditor, Vorlagenübernahme, Herleitung je Zeile, Tafel „Ersatz und Restwert" samt Hinweis; Wiki „Programm Dokumentation/Kosten" | bunit-Fälle je Weg, Kern-Fall gegen die Kapitalwertrechnung, Referenzlauf byte-gleich |
| **S3 Instandsetzung und Wartung** — **umgesetzt #463** | Spalten in der Tabelle sichtbar, gesät mit Schritt 120 und gelesen von der Vorbelegung („Sätze vorbelegen…", Vorlagenübernahme) — der Rechenweg liest den Satz der Position; Vorbelegung neuer Kesseleinträge in %/a; dazu die Speicherflotte an der Tabelle und die Kennzeichnung der Gerätespalten (A7, A8) | A/B („Sätze vorbelegen…" auf einer Arbeitskopie, Speicherflotte 1046), Anker unverändert, Referenzlauf 13/13 byte-gleich — keine neue Basis |

S3 ist die Etappe **E10** des Etappenplans E0–E12, gebaut mit **#463** (Merge `94521f2e`); ihre Fragen E10‑Q1 bis E10‑Q7
stehen im Entscheidungsregister (R‑E10), gebaut ist jeweils die Empfehlung. **So gebaut:**

- **Sätze:** Der Dialog „Nutzungsdauern (AfA)" zeigt „Instandsetzung [%/a]" und „Wartung [%/a]". Schritt 120 sät an
  den Standardzeilen die Mitte des Empfehlungsbereichs der Betriebsvorlagen (Abschnitt 1.6) — Heizkessel 2,0, BHKW 6,0,
  Wärmezentrale 2,0, Stromeinspeisung 2,0, Bauliche Anlagen 1,25 %; Wärmepumpe, Photovoltaik, Solarthermie, Strom- und
  Pufferspeicher bleiben leer, Wartung überall (E10‑Q7).
- **Die Tabelle rechnet nicht selbst** (E10‑Q1, Lesart a; ND‑Q4): Der Rechenweg nimmt den Satz der Position, wie er
  gepflegt ist. In eine Position „Instandhaltung …"/„Wartung …" mit „% der Investition" kommt der Satz der Tabelle
  ausdrücklich — mit der vom Anwender ausgelösten Übernahme einer Kostenvorlage oder mit dem Knopf „Sätze vorbelegen…"
  der Betriebsseite (Muster des Knopfs „Nutzungsdauern vorbelegen…": leere füllen, belegte nach Rückfrage, geschrieben
  mit „Speichern"); die automatische Anlage der Pflichtpositionen schreibt keinen. Zugeordnet wird über den Namen der
  Position (Technik der Position, Zeile der Positionsart, sonst Standardzeile).
- **Herkunft:** Gleicht ein Satz dem der Tabelle, steht unter dem Satzfeld „2 % · Satz aus Nutzungsdauertabelle:
  Heizkessel · Wärmeerzeuger (Instandsetzung)"; Herleitung der Berichte und Formelmappe nennen „Satz aus
  Nutzungsdauertabelle" (Nachweisfassung 10).
- **Gerätekataloge:** Abschnitt 2.4, Punkt 5.
- **Speicherflotte** (A7): Die Flottenstudie rechnet den Restwert je Einheit linear aus ihrer Nutzungsdauer (dem
  Ersatzintervall); eine Einheit ohne eigenes Intervall nimmt die Standardzeile „Stromspeicher · Batterie" (10 a); der
  feste Restwert der Einheit ist ein Altfeld.

**Offen aus S2 — umgesetzt #431 (Merge `2cfee66b`):** Die Hülle der Kostenverwaltung liegt seit E3
Schritt 5 plattformfrei in `EPOS.UI.Daten/Kosten/KostenKomponenteHuelle.cs`; die Windows-Schale behält
den Fenster-Adapter `KostenKomponenteFenster` (Muster aus #428/KI‑F8: Hülle in `EPOS.UI.Daten`,
Fenster-Adapter in der Schale, Nähte in `IProjektQuelle`). Damit ist **E3 Schritt 5**
(„`KostenKomponenteHuelle` mit Fenster-Adapter", entschieden mit **A1** und **Q14** am 20.09.2026)
gebaut.

Ebenso offen: die geräteeigenen Dauerspalten und der Anschluss der Speicherflotte (Mockup-Anhang U39).
Dazu sind zwei Entscheide gefallen
(**20.09.2026, nach Empfehlung**): **A7** — die Speicherflotte wird **mit ND‑S3** an
`Tab_Nutzungsdauer` angeschlossen, als eigener Auftrag mit Neueinfrieren der Referenzbasis (Projekt
1046); **A8** — die geräteeigenen Nutzungsdauer-Spalten (`Tab_BHKW`, `Tab_Heizkessel`) werden **nicht
jetzt** abgekündigt, sondern nur **gekennzeichnet**; die Speichervariante sollte die Positionsarten
20/21 lesen. Ein Schemaschritt dafür (vormals „104") bekommt seine Nummer erst bei der Umsetzung. **Umgesetzt mit
S3 (#463):** A7 — die Speicherflotte hängt an der Tabelle (Abschnitt 3), ohne Neueinfrieren, weil der Referenzlauf
keine Flottenwirtschaftlichkeit führt und byte-gleich blieb; A8 — die Spalten von BHKW und Heizkessel sind als
„Nutzungsdauer (Gerätedaten)" gekennzeichnet, kein Schemaschritt. **Der Halbsatz zur Speichervariante ist erledigt
mit #474** (E13/4): Eine neu angelegte Speichervariante — eine neue Speicheranlage im Anlagendialog oder „Speichervariante
anlegen" des Hilfe-Assistenten — nimmt die Nutzungsdauer der Standardzeile „Stromspeicher · Batterie", ohne Tabelle,
Zeile oder brauchbaren Wert (< 1 a) 20 a; bestehende Varianten und der Rückfall des Rechenwegs (20) bleiben.
Die Entkopplung von Ersatz und Restwert ist mit **A6** entschieden (Kennzeichen je **Position**,
nullbar, NULL = wie bisher) und **umgesetzt #446** (E7c2, Schemaschritt 111): `ErsatzFuehren` und
`RestwertAnsetzen` an `Tab_ProjektWerte` und `Tab_KostenVorlagePosition`, gepflegt im Zeileneditor
„Position bearbeiten" als Klapplisten „Ersatzbeschaffung führen:" und „Restwert ansetzen:"; „nein"
schaltet das eine ohne das andere ab, die Tafel „Ersatz und Restwert" nennt die Abwahl (Konzept
Wirtschaftlichkeit § 2.13 (3), § 3.1).

## 4. Fragen mit Empfehlung

**Entscheid 14.09.2026:** alle acht Fragen nach Empfehlung. Zu ND-Q8 gilt: Die Werte der Tabelle 2.6 werden als
Saat mit Quellenangabe „Richtwert" ausgeliefert, sind im Dialog editierbar und lassen sich mit „Auslieferungswerte
wiederherstellen" zurücksetzen; der Abgleich mit dem Wortlaut der Norm bleibt Sache des Anwenders.

| Frage | Optionen | Empfehlung |
|---|---|---|
| **ND-Q1** Quelle der Vorbelegung | (a) VDI 2067 Blatt 1, (b) steuerliche AfA, (c) beide Spalten, gerechnet wird mit (a) | (c): VDI 2067 rechnet, die AfA-Spalte informiert |
| **ND-Q2** Schlüssel der Zuordnung | (a) Positionsart je Position mit Technik-Standard als Rückfall, (b) nur Technik | (a) — nur so bekommen Abgasanlage und MSR andere Werte als der Kessel |
| **ND-Q3** Ort in der Administration | Administration → Kostenverwaltung → „Nutzungsdauern (AfA)…" | ja, dritter Eintrag neben Kostenvorlagen und Energieträgern |
| **ND-Q4** Bestehende Positionen | (a) still beim Öffnen füllen, (b) nur auf Knopfdruck | (b) — nichts ändert eine gerechnete Wirtschaftlichkeit ohne Zutun |
| **ND-Q5** Auslieferungszeilen | (a) unveränderlich, Kopie über „Speichern unter", (b) Wert editierbar, Zeile nicht löschbar, „Auslieferungswerte wiederherstellen" | (b) — die Tabelle ist die Tabelle des Anwenders |
| **ND-Q6** Spalten Instandsetzung/Wartung | (a) mit S1 anlegen, Anzeige ab S3, (b) erst mit S3 | (a) — ein Schema-Schritt statt zwei |
| **ND-Q7** Gerätekataloge (BHKW, Kessel, Stromspeicher) | (a) unberührt, (b) Vorbelegung aus der Tabelle | (a) in S1/S2; (b) mit S3 prüfen |
| **ND-Q8** Startwerte | Tabelle 2.6 bestätigen oder Werte nennen | vom Anwender zu bestätigen — Normwerte werden nicht erfunden |

## 5. Nicht enthalten

Zinssatz, Preissteigerung und Betrachtungszeitraum bleiben Projektgrößen im Dialog
Wirtschaftlichkeitsparameter. Die Betriebskostenprozentsätze nach VDI 2067 stehen mit S3 in der Tabelle (#463);
gerechnet wird mit dem Satz der Position.

## 6. Umsetzung

| Auftrag | Inhalt | Voraussetzung |
|---|---|---|
| A (Stufe S1) — **umgesetzt** | Schema-Schritt mit `Tab_Nutzungsdauer`, Saat nach Tabelle 2.6, Spalten `NutzungsdauerID` in Vorlagen- und Projektposition mit Saat-Zuordnung, `NutzungsdauerCtrl`, Vorbelegung beim Anlegen und Übernehmen im Kern, Administrationsdialog mit plattformfreier Hülle, Menüpunkt, Ressourcen, Testdatenbank, Auslieferungsvorlage | nächster freier Schema-Schritt |
| B (Stufe S2) — **umgesetzt** | Kostenverwaltung: Knopf „Nutzungsdauern vorbelegen…", Positionsart im Zeileneditor, Herleitung je Zeile, Tafel „Ersatz und Restwert" mit Hinweis; Wiki „Programm Dokumentation/Kosten". Die Hülle der Kostenverwaltung liegt seit **E3 (#431)** plattformfrei in `EPOS.UI.Daten/Kosten/KostenKomponenteHuelle.cs` mit Fenster-Adapter `KostenKomponenteFenster` | nach A |
| S3 (Etappe E10) — **umgesetzt #463** | Instandsetzung/Wartung (Satzspalten im Dialog, Schemaschritt 120, Vorbelegung über Kostenvorlage und „Sätze vorbelegen…", Herkunft am Satz), Gerätekataloge (neue Kesseleinträge in %/a, Kennzeichnung A8), Speicherflotte (A7) | eigener Entscheid — vertreten durch E10‑Q1 bis E10‑Q7 (R‑E10; gebaut ist jeweils die Empfehlung, entschieden 24.09.2026 nach Empfehlung); der Halbsatz aus A8 zur Speichervariante mit #474 |

