# Konzept: Erweiterte Berichtserstellung mit Variantenvergleich (konsolidiert)

**Anwendung:** EPOS-Plan / WP-Plan (`WindowsFormsApplication1`, C#, net8.0-windows, WinForms MDI)
**Fassung 3.1 (konsolidiert)** · Stand 11.08.2026 (abends) · abgestimmt mit P. Engelmann (Entscheidungsrunden 1–4)
**Status:** Konzept beschlossen · Code- und DB-Verifikation **abgeschlossen** (Kap. 11) · Umsetzung Phase 1–6 **unabhängig nachgeprüft** (Prüf-Session 11.08. abends; Chart-Technik und Tabellenanlage an den Code-Stand angepasst, Befundliste erweitert; W1-Detailprüfung im Begleitkonzept Kap. 8)
Begleitdokument: `Konzept_Wirtschaftlichkeit.md` (Kapitalwertmethode nach DIN EN 17463, UI-Reiter, Datenvertrag)

> **Konsolidierung.** Diese Fassung führt die beiden parallel entstandenen Konzepte
> zusammen: `Konzept_Variantenbericht.md` (10.08., pragmatischer Ausbau des
> lauffähigen `ProjektvergleichBericht`) und die Erstfassung dieses Dokuments
> (11.08., neue Berichtsarchitektur). Beschluss: **Kombination** — die
> Strukturideen der Berichtsarchitektur (DTO-Datensammler, `IBerichtsBaustein`,
> `KennzahlenKatalog`) werden übernommen; der vorhandene, funktionierende
> Vergleichs-/OpenXML-Code wird darin **weiterverwendet, nicht weggeworfen**.
> Die frühere Code-Analyse-Checkliste (ehem. Kap. 12) ist durch die
> durchgeführte Verifikation ersetzt (Kap. 11). `Konzept_Variantenbericht.md`
> ist in dieser Fassung aufgegangen.

---

## 1. Ziel und beschlossene Eckpunkte

Die begonnene Berichtserstellung wird zu einem vollwertigen Berichtsmodul ausgebaut.
Kern ist der **Vergleich beliebig vieler Projektvarianten** gegen das Basisprojekt
(„Stamm"). Der Bericht enthält Projektbeschreibung (Komponenten, Daten),
Berechnungsergebnisse mit Variantenvergleich sowie die Wirtschaftlichkeit
(eigenes Konzept, Kap. 7).

| # | Punkt | Entscheidung |
|---|-------|--------------|
| 1 | Variantenanzahl je Bericht | **unbegrenzt** (dynamisches Tabellen-/Diagramm-Layout, Kap. 5.1) |
| 2 | Architektur | **Kombination**: neue Struktur `Allgemein/Bericht/` (DTOs, Bausteine, Katalog) unter Weiterverwendung von `ProjektvergleichBericht`-Logik, `EnergieMengen`, `ErgebnisCtrl` |
| 3 | Berichtsumfang | **voller Umfang sofort**: 4 Kennzahlgruppen + Balkendiagramme + **4 Ganglinientypen**; Zeitreihen aus **In-Memory-Simulation bei Berichtserzeugung** (Kap. 6.2/8.2) |
| 4 | Word-Technik | **OpenXML SDK** (ohne installiertes Word) mit **neuer `Berichtsvorlage.docx`** (vollständiges Stylesystem; `Vorlage_Bericht3.dotx` dient nur als CI-Referenz für Logo/Fußzeile) |
| 5 | Excel-Technik | **ClosedXML** (MIT, ohne Office; `SixLabors.Fonts` auf 1.0.x pinnen) |
| 6 | Bedienung | **neuer Dialog `Form_Bericht`** — aus Menü „Projekt → Bericht erstellen…" und Button in `Form_Varianten` |
| 7 | Konfiguration | **je Stammprojekt in der DB** (neue Tabelle `Berichtskonfiguration`, JSON) — vorbelegt beim nächsten Öffnen |
| 8 | Ergebnisherkunft | gespeicherte Ergebnisse (`Tab_Ergebnis*`) für Dialog/Zeitstempel; Option „vor Ausgabe neu rechnen"; **für Ganglinien wird ohnehin je Variante headless simuliert** |
| 9 | Excel-Umfang | Übersicht + Vergleichstabelle + **Detailblatt je Variante** |
| 10 | Sprache | Bericht folgt der **UI-Sprache** (de/en, Satelliten-`.resx`); Zahlen-/Datumsformat je `CultureInfo` |
| 11 | Wirtschaftlichkeit | **Kapitalwertmethode nach DIN EN 17463 (ValERI)**, eigener UI-Reiter unter „Berichte & Kosten" — Details `Konzept_Wirtschaftlichkeit.md` |
| 12 | Variantenbezeichner im Stamm | neuer Menüweg „Als Variante speichern…" überführt den aktuellen Stand per Bezeichner in eine Variante (Kap. 3.3) |

---

## 2. Ausgangslage (verifiziert am Code und an `Kenndaten.accdb`)

- **Variantenmodell:** Eine Variante ist ein vollwertiges Kopie-Projekt
  (`ProjektDuplizierenCtrl`); die Seitentabelle **`Tab_Variante`**
  (`ID, ID_Projekt, ID_ProjektRef, Variantenname` — DB-verifiziert) verknüpft
  Variante ↔ Stamm. `Form_Variantentest` („Projektvarianten") kann Varianten
  anlegen/löschen, headless simulieren (`SimulationRunner`) und ruft heute den
  Vergleich für **Stamm + eine** Variante auf. Beim Anlegen werden
  `energy_project_settings`/`energy_price` mitkopiert — **derzeit doppelt**
  (generisch durch `ProjektDuplizierenCtrl` *und* explizit durch
  `VariantenCtrl.KopiereEnergieEinstellungen`; Befund B8).
- **Berichtsbestand:** `Views/Varianten/ProjektvergleichBericht.cs` erzeugt bereits
  ein Word-Dokument über OpenXML: Übersicht, Energiebedarf, Erzeuger-Vergleich
  (WP/BHKW/Kessel/Solar/PV) mit Δ-Spalte, Erzeuger-Einzellisten je Modul,
  Brennstoffmengen (`EnergieMengen`), Restbedarf, Deckungs-Kuchendiagramme
  (System.Drawing→PNG). **Wiederverwendbar:** Tabellen-/Absatz-Helfer,
  Vergleichstabellen-Logik, PNG-Einbettung, Kennzahl-Definitionen (`List<KZ>`).
- **Ergebnispersistenz:** `ErgebnisCtrl` speichert je Projekt **genau einen** Lauf
  (Vorab-DELETE) in 13 Tabellen `Tab_Ergebnis*` — **Summen- und Modulwerte, keine
  Zeitreihen** (DB-verifiziert). `Load(idProjekt)` liest den kompletten Baum
  **ohne das aktive Projekt umzuschalten** → der lesende Datensammler
  (Konzept-Variante A) ist bestätigt machbar; die Rückfallebene
  „Projekt umschalten" entfällt.
- **Kosten-/Preisdaten:** `Tab_ProjektWerte` (Betrag, Nutzungsdauer, Best-/
  Worst-Case je Position; Komponenten/Kategorien über `Tab_KostenKomponente`/
  `Tab_KostenKategorie`), Energieträger über `energy_carrier` /
  `energy_project_settings` (custom-Preise, Hi/Hs, CO₂/SO₂/NOx) /
  `energy_price` (Historie `valid_from`, `valid_to`) — alles DB-verifiziert.
  Kosten-Abfragen vorhanden (`Abfrage_Kosten_*`, `Abfrage_ProjektKostenEnergie`,
  `Abfrage_ProjektKostenInvestBetrieb`, `Abfrage_Energietraeger_Effektiv`).
- **Charts:** `WinForms.DataVisualization` (UI-gebunden) und ScottPlot (im
  Projekt vorhanden). **Umgesetzt ist der Druckpfad mit System.Drawing/GDI+**
  (`ChartRenderer` — bewusste Abweichung: kein API-Risiko, gleiches Muster wie
  der Bestandsbericht); ScottPlot bleibt als Option.
- **Randbedingung Statics:** `Program` hält globale Statics, die App arbeitet auf
  einem aktiven Projekt — der Berichtsweg liest deshalb ausschließlich über
  Repository/Controller in eigene DTOs (bestätigt, s. o.).

---

## 3. Bedienung

### 3.1 Dialog `Form_Bericht`

Erreichbar über Menü **„Projekt → Bericht erstellen…"** und Button **„Bericht…"**
in `Form_Varianten` (übernimmt deren Stammkontext). Aufruf immer vom Stamm aus;
ist eine Variante aktiv, wird intern ihr Stamm verwendet.

```
┌─ Bericht erstellen ─ Projekt: <Stammprojektname> ───────────────────────┐
│ Varianten (Referenz: Stamm, fest gewählt)          Berichtsbausteine    │
│ ┌─────────────────────────────────────────┐  ┌───────────────────────┐  │
│ │ [■] Stamm            Sim: 10.08.26 14:12│  │ [x] Deckblatt         │  │
│ │ [x] V1 Wärmepumpe    Sim: 10.08.26 14:20│  │ [x] Inhaltsverzeichnis│  │
│ │ [x] V2 WP + PV       Sim:  — (fehlt) ⚠  │  │ [x] Projektbeschreib. │  │
│ │ [ ] V3 BHKW          Sim: 03.08.26 09:41│  │ [x] Komponenten/Var.  │  │
│ └─────────────────────────────────────────┘  │ [x] Ergebnisse je Var.│  │
│ [Alle] [Keine]                               │ [x] Variantenvergleich│  │
│                                              │ [ ] Wirtschaftlichkeit│  │
│ Optionen                                     │ [x] Anhang            │  │
│ [x] Vor Ausgabe neu rechnen                  └───────────────────────┘  │
│ Ausgabe: (•) Word  ( ) Excel  ( ) Word + Excel                          │
│ Datei:   [C:\...\<Projekt>_Bericht_2026-08-11]        [Durchsuchen…]    │
│ ────────────────────────────────────────────────── [Erstellen] [Abbr.]  │
└─────────────────────────────────────────────────────────────────────────┘
```

- **Stamm immer enthalten** (Referenz, nicht abwählbar); beliebig viele Varianten.
- Je Zeile der **Zeitstempel der letzten Simulation** (`Tab_Ergebnis.Zeitstempel`);
  fehlend/veraltet (`Zeitstempel < Tab_Projekt.Aenderungsdatum`) → ⚠. Ohne
  Ergebnis wird „neu rechnen" erzwungen oder die Variante (mit Bestätigung)
  ausgeschlossen. Da Ganglinien-Bausteine ohnehin je Variante simulieren
  (Kap. 8.2), betrifft die Prüfung vor allem den schnellen Weg ohne Ganglinien.
- Baustein **Wirtschaftlichkeit** ist seit Phase 6 wählbar; er liest die im
  Reiter „Wirtschaftlichkeit" persistierten Ergebnisse (fehlen sie, erscheint im
  Bericht ein Hinweis mit dem Weg zum Reiter).
- **Fortschrittsanzeige** mit Abbruch (Hintergrund-Thread); Dateinamensvorschlag
  `<Projektname>_Bericht_<JJJJ-MM-TT>`; nach Erfolg „Bericht öffnen?".
- Beim Erstellen wird die Auswahl als **Berichtskonfiguration am Stamm gespeichert**
  (Kap. 8.4) und beim nächsten Öffnen vorbelegt.

### 3.2 Reiter „Wirtschaftlichkeit" (Berichte & Kosten)

Eigener Bereich neben [Kosten] und [Varianten]: zeigt die
Wirtschaftlichkeits-Kennzahlen der Vergleichsgruppe direkt in der App, mit
Prüfkette (Simulation aktuell? → anbieten; Wirtschaftlichkeit auf aktuellem Lauf?
→ automatisch nachrechnen) und Szenario-Umschalter. Vollständige Beschreibung:
`Konzept_Wirtschaftlichkeit.md` Kap. 6. Reiter, Word-Baustein und Excel-Blatt
lesen dieselben persistierten Ergebnisse.

### 3.3 Variantenbezeichner im Stammprojekt

Menüpunkt *„Als Variante speichern…"*: Bezeichner eingeben → aktueller Stand wird
per `ProjektDuplizierenCtrl` kopiert, in `Tab_Variante` verknüpft,
Energie-Einstellungen werden mitkopiert. Gemeinsame Logik wandert aus
`Form_Variantentest.btnAnlegen_Click` in einen **`VariantenCtrl`**
(`AnlegenAusStamm(idStamm, bezeichner)`), den Formular und Menüweg teilen —
inklusive Waisen-Prüfung für `Tab_Variante` (kein FK mit Löschweitergabe).

---

## 4. Berichtsstruktur Word (Bausteine)

Jeder Baustein einzeln an-/abwählbar (`IBerichtsBaustein`, Kap. 8.3):

1. **Deckblatt** — Projektname, Variantenliste, Datum, Bearbeiter, Logo, Version.
2. **Inhaltsverzeichnis** — `TOC`-Feld; `updateFields` in `settings.xml`, Word
   aktualisiert beim Öffnen.
3. **Projektbeschreibung** — Stammdaten (Objekt, Kunde, Bearbeiter, Beschreibung),
   Klimaregion/Wetterdatensatz, Gebäude je `ProjektGebaeudeModel` (Art,
   Baualtersklasse, Flächen, k-Werte, Bedarfe), Anlagenkonfiguration
   (`WErzeugerModel`: Betriebsart, Vor-/Rücklauf, Bivalenz, Sperrzeiten).
4. **Komponenten & Varianten** — Matrix Komponenten × Varianten; Kenndaten-Tabellen
   je Komponententyp (Auslegungsdaten je Variante, „—" wo nicht vorhanden);
   **je Variante eine Abweichungstabelle** „Merkmal · Stamm · Variante" (nur
   Unterschiede; dreistufige Erkennung: Gewerk vorhanden → andere Komponente →
   geänderte Auslegung/Betriebsparameter; deklarative Feldliste im Code).
5. **Berechnungsergebnisse je Variante** — Unterkapitel je Variante (Stamm zuerst):
   Energiebilanz, Kennzahlen, **Ganglinien** (Kap. 6.2), Simulationszeitstempel im
   Kapitelkopf.
6. **Variantenvergleich** — Kennzahlentabellen aller Varianten nebeneinander
   (Kap. 5), Δ-Darstellung zu Stamm (aus Rohwerten gerechnet, dann formatiert),
   Balkendiagramme je Kennzahlgruppe (Kap. 6.1); Erzeuger-Einzellisten und
   Brennstoffmengen je Projekt (aus dem Bestand übernommen).
7. **Wirtschaftlichkeit** — Kennzahlen nach `Konzept_Wirtschaftlichkeit.md`
   (Kapitalwert, Annuität des KW, Amortisation, Gestehungskosten je Szenario;
   Parameter-Nachweiszeile). **Umgesetzt in Phase 6 (Stufe W1)** —
   `BausteineWirtschaftlichkeit.cs`, liest `Tab_ErgebnisWirtschaftlichkeit`.
8. **Anhang** — Annahmen (Emissionsfaktoren, Energiepreise, Klimadatensatz),
   Datenquellen (VDI 3805/CEC/PAN), Simulationsstände je Variante, Verweis auf
   die begleitende Excel-Datei.

---

## 5. Kennzahlenkatalog Variantenvergleich

Zentral definiert in `KennzahlenKatalog` (je Kennzahl: Schlüssel, Bezeichnung
de/en, Einheit, Gruppe, Zahlenformat, Delta-Regel). Fehlende Komponenten → „—",
nie 0. Vier Gruppen:

- **Energiebilanz** — Wärmebedarf gesamt/je Zweck, Strombedarf inkl. WP-/Hilfsstrom,
  Erzeugung je Komponente + Deckungsanteil, Stromerzeugung PV/BHKW, Brennstoff je
  Träger, Netzbezug/-einspeisung, Speicherverluste. *(Quelle: `Tab_Ergebnis*` bzw.
  frischer Lauf.)*
- **Effizienz** — JAZ Wärmepumpe, Nutzungsgrade Kessel/BHKW, Volllaststunden je
  Erzeuger, PV-Eigenverbrauchsquote, Autarkiegrad. *(JAZ/Quoten werden im Sammler
  aus vorhandenen Größen abgeleitet.)*
- **Emissionen** — CO₂ gesamt [t/a], spezifisch [g/kWh], Δ zu Stamm; Faktoren je
  Träger aus `energy_project_settings`/`energy_carrier`, Verrechnung im Sammler
  (kleines Paket; Bewertung des Netzbezugs mit Strommix-Faktor als Parameter).
- **Kosten (einfach)** — Energiekosten p. a. je Träger (Menge aus `EnergieMengen`
  × Preis aus `energy_*`), Strom Bezug/Einspeisung, Summe, Δ zu Stamm — Vorstufe
  zur Wirtschaftlichkeit (Kap. 7). **Voraussetzung:** `carrier_id`-Spalte in den
  Ergebnis-Modultabellen (Kap. 11, Befund B1).

### 5.1 Tabellenlayout bei unbegrenzter Variantenanzahl

- A4 hoch: Spalte „Kennzahl (Einheit)", dann **Stamm**, dann bis zu **3 Varianten**
  je Tabellenblock; weitere Varianten in Folgeblöcken (V4–V6, …), **Stamm-Spalte in
  jedem Block wiederholt**.
- Bei genau **einer** Variante zusätzlich Δ-Spalte (wie im Bestandsbericht).
- Ergänzend eine kompakte **Delta-Tabelle** der Schlüsselkennzahlen (Zeile je
  Variante, Δ zu Stamm in %) — bleibt auch bei vielen Varianten einseitig lesbar.

---

## 6. Diagramme

Off-screen-Rendering als **PNG** (Zielbreite 16 cm, 150–200 dpi) über
**System.Drawing/GDI+** (`ChartRenderer`; bewusste Abweichung vom ursprünglich
geplanten ScottPlot — Umstieg bleibt möglich); die vorhandenen Kuchendiagramme
(Wärme-/Stromdeckung) werden aus dem Bestand übernommen. Feste Farbzuordnung je
Variante über alle Diagramme (Stamm neutral dunkel, Varianten aus Palette);
Beschriftung in Berichtssprache.

### 6.1 Balkendiagramme (Variantenvergleich)

Je Kennzahlgruppe 1–2 Diagramme (z. B. Wärmeerzeugung gestapelt, CO₂,
Energiekosten). **Horizontale Balken** (ein Balken je Variante, wächst nach
unten); ab ~15 Varianten Aufteilung auf mehrere Diagramme.

### 6.2 Ganglinien (Ergebnisse je Variante) — alle vier Typen

1. **Wärmeerzeugung im Jahresverlauf** — gestapelt nach Komponenten, Bedarf als
   Linie (Tages-/Wochenmittel).
2. **Jahresdauerlinie Wärme** — geordnete Dauerlinie mit Erzeugeraufteilung.
3. **Strombilanz** — PV-/BHKW-Erzeugung, Bedarf (inkl. WP), Eigenverbrauch,
   Netzbezug/Einspeisung.
4. **Speicherverlauf** — Puffer-/Stromspeicher über charakteristische Wochen
   (Winter/Übergang/Sommer).

**Datenquelle (beschlossen):** Zeitreihen werden **nicht** persistiert
(DB-verifiziert), sondern bei der Berichtserzeugung **je Variante headless neu
simuliert** (`SimulationRunner`, frische Instanz je Projekt — Muster aus
`Form_Variantentest.btnSimulieren_Click`); die Stundenreihen bleiben im Speicher
des `BerichtsDatenSammler`. Kennzahlen und Ganglinien stammen damit garantiert
aus **demselben Lauf**; das persistierte `Tab_Ergebnis*` wird im selben Zug
aktualisiert. Fortschritt „Variante i von n", Abbruch möglich.

---

## 7. Wirtschaftlichkeit (Verweis)

Methode, Datenvertrag, DB-Zusätze und UI sind vollständig in
**`Konzept_Wirtschaftlichkeit.md`** beschrieben: Kapitalwertmethode nach
DIN EN 17463 (ValERI) mit Szenarien Worst/Erwartet/Best, Zahlungsgerüst nach dem
Alt-Verfahren (BHKW-Plan), `WirtschaftlichkeitErgebnisModel` +
`IWirtschaftlichkeitProvider` (umgesetzt durch `WirtschaftlichkeitCtrl`, Phase 6),
Persistenz `Tab_ErgebnisWirtschaftlichkeit` mit FK auf den Simulationslauf,
Parameter in `Tab_ProjektWirtschaftlichkeit`, VALERI-Vorlage V7 als
Strukturvorbild. Der Berichtsbaustein und das Excel-Blatt lesen die persistierten
Ergebnisse — identische Zahlen in Reiter, Word und Excel.

---

## 8. Technische Architektur

### 8.1 Struktur

```
Allgemein/Bericht/
├─ BerichtsDaten.cs           # DTOs: BerichtsDaten, VariantenDaten, KomponentenInfo,
│                             #   KennzahlenSatz, Zeitreihen, Abweichung
├─ BerichtsKonfiguration.cs   # Varianten-/Baustein-Auswahl, Optionen; JSON-(De)Serialisierung
├─ BerichtsDatenSammler.cs    # lädt Stamm + Varianten lesend (ErgebnisCtrl, Projekt-/
│                             #   Komponenten-Controller, EnergieMengen); triggert Simulation;
│                             #   berechnet abgeleitete Kennzahlen/Emissionen/Deltas/Abweichungen
├─ KennzahlenKatalog.cs       # zentrale Kennzahl-Definitionen (de/en, Einheit, Format, Delta)
├─ ChartRenderer.cs           # Kuchen (Bestand) + Balken + 4 Ganglinientypen → PNG (System.Drawing/GDI+)
├─ WordBerichtGenerator.cs    # OpenXML; Styles aus Vorlagen-docx; übernimmt Tabellen-/
│                             #   Absatz-Helfer und Vergleichslogik aus ProjektvergleichBericht
├─ ExcelBerichtGenerator.cs   # ClosedXML (Kap. 9)
├─ Bausteine/                 # ein IBerichtsBaustein je Kapitel (inkl. Wirtschaftlichkeit)
└─ Vorlagen/Berichtsvorlage.docx
Controller/BerichtCtrl.cs     # Ablauf, Konfig-Persistenz, Fortschritt
Controller/VariantenCtrl.cs   # Varianten anlegen/löschen/auflisten, AnlegenAusStamm, Waisen-Prüfung
Views/Bericht/Form_Bericht.*  # Dialog (+ de-DE/en-US .resx)
```

`Views/Varianten/ProjektvergleichBericht.cs` wird schrittweise in
`WordBerichtGenerator` + Bausteine überführt (Phase 2) und danach stillgelegt;
`EnergieMengen` bleibt als Dienst bestehen. Neue Pakete: `DocumentFormat.OpenXml`
(bereits vorhanden), `ClosedXML` (+ `SixLabors.Fonts` 1.0.x gepinnt,
`dotnet list package --include-transitive` in die Release-Checkliste).

### 8.2 Ablauf bei „Erstellen"

1. Konfiguration validieren (fehlende Ergebnisse → rechnen/ausschließen).
2. Je Variante **headless simulieren** (immer, wenn Ganglinien-Bausteine gewählt;
   sonst nur bei fehlenden/veralteten Ergebnissen oder Option „neu rechnen") —
   Zeitreihen im Speicher, `Tab_Ergebnis*` wird aktualisiert.
3. Je Variante laden: Projektdaten, Komponenten + Kenndaten, Ergebnisse,
   Brennstoffmengen.
4. Kennzahlen/Emissionen/Kosten-einfach/Deltas/Abweichungen berechnen → `BerichtsDaten`.
5. Charts rendern (PNG, Temp-Ordner, Bereinigung im `finally`).
6. Word-/Excel-Generator ausführen; Konfiguration speichern; optional öffnen.

Fehler einer einzelnen Variante brechen den Bericht nicht ab — die Variante wird
mit Hinweis ausgewiesen, Sammelmeldung am Ende.

### 8.3 Baustein-Abstraktion

`IBerichtsBaustein` mit `SchreibeWord(WordKontext, BerichtsDaten)` und optional
`SchreibeExcel(ExcelKontext, BerichtsDaten)`; Reihenfolge/Aktivierung aus der
`BerichtsKonfiguration`. Die Wirtschaftlichkeit ist damit später ein reiner
Zusatzbaustein.

### 8.4 Persistenz

- **`Berichtskonfiguration`** (neu): `ID`, `ProjektID` (Stamm, UNIQUE),
  `KonfigJson` (Memo), `GeaendertAm`. JSON über `System.Text.Json`; Anlage
  **selbstanlegend zur Laufzeit** (`BerichtCtrl.StelleKonfigTabelleSicher()` —
  gleiches Muster wie `Tab_Variante` in `VariantenCtrl` und die
  Wirtschaftlichkeits-Tabellen). *Hinweis:* `UpdateDatabaseFromScript` kennt
  keine TABELLEN/SPALTEN-Abschnitte, sondern Zeilenpräfixe (`SQL=`,
  `BACKUP_REL:`, `CLEAN_COL:`, `RESTORE_REL:` — letztere hart auf
  `Tab_ProjektWerte` verdrahtet) und nutzt einen eigenen
  Registry-Connection-String; es bleibt das Werkzeug für Migrationsskripte,
  nicht für die Laufzeit-Anlage.
- **Simulationsergebnisse:** bleiben wie gehabt in `Tab_Ergebnis*` (ein Lauf je
  Projekt, `Zeitstempel` als Nachweis). Zeitreihen werden bewusst **nicht**
  persistiert — sie entstehen je Berichtslauf neu (6.2).
- **Wirtschaftlichkeit:** `Tab_ErgebnisWirtschaftlichkeit` +
  `Tab_ProjektWirtschaftlichkeit` (siehe Begleitkonzept).

### 8.5 Word-Vorlage und Lokalisierung

**Neue `Berichtsvorlage.docx`** mit vollständigem Stylesystem (Überschrift 1–3,
Standard, Tabellenstile, Beschriftung, Kopf-/Fußzeile mit Feldern
Seite/Datum, Logo-Platzhalter, Deckblattlayout). Der Generator kopiert die
Vorlage und schreibt Inhalte über Style-Namen — Layout/CI sind damit ohne
Codeänderung pflegbar. `Vorlage_Bericht3.dotx` dient als CI-Referenz (Logo,
Fußzeilenaufbau) beim Gestalten der neuen Vorlage. Berichtstexte in
`de-DE`/`en-US`-Ressourcen; Berichtssprache = Programmsprache; Formatierung über
`CultureInfo` der Berichtssprache (nie UI-vermischt).

---

## 9. Excel-Ausgabe (reduziert, ClosedXML)

- **Blatt „Übersicht"** — Projektstammdaten, Variantenliste (Bezeichner,
  Simulationszeitstempel), Komponenten-Matrix.
- **Blatt „Vergleich"** — komplette Kennzahlen-Vergleichstabelle: Kennzahlen als
  Zeilen (4 Gruppen), Varianten als Spalten (Stamm zuerst), **echte Zahlenwerte**
  mit Zellformat, fixierte Kopfzeile/erste Spalte, Autofilter; Δ-Spaltenblock (%)
  rechts. Fehlende Werte bleiben leer, nie 0.
- **Blatt je Variante** — Detailergebnisse: Energiebilanz, Kennzahlen,
  Erzeuger-Einzellisten (Module), Brennstoffmengen (Menge · Einheit),
  Monatswerte der wichtigsten Größen. Keine Diagramme (reduzierte Form).
- **Blatt „Wirtschaftlichkeit"** (Phase 6): Kennzahlen der Kapitalwertmethode
  in drei Szenarioblöcken (Erwartet/Best/Worst) aus den persistierten Ergebnissen.

---

## 10. Fehlerbehandlung und Randfälle

- Variante ohne Ergebnis → ⚠, „neu rechnen" erzwungen oder Ausschluss mit Bestätigung.
- Veraltete Ergebnisse (`Zeitstempel < Aenderungsdatum`) → Warnung in Dialog und Bericht.
- Komponente nur in Teilmenge der Varianten → „—"; Diagramme lassen die Reihe weg.
- Sehr viele Varianten → Blocksplitting (5.1), horizontale Balken, Umfangshinweis im Dialog.
- Lange Laufzeit → Hintergrund-Thread, Fortschritt je Variante/Baustein, Abbruch
  (Teil-Dateien werden verworfen).
- Zieldatei gesperrt/vorhanden → Alternativname (`…_2.docx`), kein stilles Überschreiben.
- Temp-PNGs → Bereinigung auch im Fehlerfall.

---

## 11. Ergebnisse der Code-/DB-Verifikation (ersetzt die frühere Checkliste)

Verifiziert am Quellstand `C:\Waermeplan\WP_Plan\WindowsFormsApplication1` und am
Schema von `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` (11.08.2026):

| Frage (ehem. Kap. 12) | Befund |
|---|---|
| Varianten-Datenhaltung | `Tab_Variante (ID, ID_Projekt, ID_ProjektRef, Variantenname)`; Varianten sind Kopie-Projekte; ComboBox in `Form_Variantentest` wählt den Stamm, Liste zeigt Stamm + Varianten. **Kein** Feld „Variantenbezeichner" in `Tab_Projekt` |
| Ergebnispersistenz | Summen-/Modulwerte in 13 `Tab_Ergebnis*`-Tabellen, ein Lauf je Projekt (`ErgebnisCtrl.Save` löscht Vorgänger); **keine Zeitreihen** → Ganglinien per In-Memory-Simulation (6.2) |
| Vorhandene Berichtserstellung | `ProjektvergleichBericht.cs` (OpenXML, lauffähig) — Helfer und Vergleichslogik werden übernommen (8.1) |
| Lesender Datenzugriff je Variante | bestätigt: `ErgebnisCtrl.Load`, `ProjektCtrl`, Stamm-Controller, `DataRepository` — ohne Umschalten des aktiven Projekts |
| Chart-Export | ScottPlot vorhanden (off-screen); Bestands-Kuchendiagramme via System.Drawing |
| Kosten-/Emissionsdaten | `energy_*`-Tabellen inkl. CO₂/SO₂/NOx je Träger und Preishistorie (`valid_from`, `valid_to`); `Tab_ProjektWerte` mit Nutzungsdauer + Best/Worst; Abfragen `Abfrage_Kosten_*` u. a. vorhanden |
| Bestehende Exporte | `CsvExportClass` (Allgemein/Export); Excel-Interop nur an anderer Stelle — kein Layout-Zwang für Kap. 9 |

**Codebefunde (Status nach Prüf-Session 11.08.2026 abends):**

| # | Befund | Status / Konsequenz |
|---|---|---|
| B1 | `Tab_ErgebnisBHKWModul.Brennstoff`/`…HeizkesselModul.Brennstoff` freie Strings; `EnergieMengen.CarrierFor()` rät ohne Projektbezug | **erledigt für die Ergebnistabellen** (`carrier_id` selbstanlegend, projektbezogen befüllt). **Rest offen:** `EnergieMengen.CarrierFor()` (Kapitel „Brennstoffmengen") weiterhin heuristisch + SQL-Konkatenation; Semantik `Tab_BHKW.Brennstoff` im Code widersprüchlich (FK auf `energy_carrier.id` vs. `id_brennstoff`) — an der DB klären |
| B2 | `ErgebnisCtrl.Delete()` funktionsunfähig | **erledigt** (`Delete(int)` mit Parameter/Commit; Alt-Überladung `[Obsolete]`) |
| B3 | Heizkessel-Modul-INSERT persistiert `Waermeproduktion` nicht | **erledigt** (inkl. Rundung `Verbrauch`) |
| B4 | `ProjektModel.m_nNetzverluste`/`m_szEinheit` ohne Spalte in `Tab_Projekt` (DB-verifiziert) | offen — Quelle klären (`KonfigurationModel`?), Modell bereinigen |
| B5 | `Tab_Variante` ohne Löschweitergabe → Waisen möglich | **teilweise:** `VariantenCtrl.EntferneWaisen()` existiert, wird aber noch von keinem Ablauf aufgerufen |
| B6 | Kostenmodul: `Form_KostenAdmin`-Insert defekt; `energy_price`-Ersteintrag ohne `leistungspreis`; Speichern nur über ucFuelSettings-Button | offen — Details/Zusatzbefunde im Begleitkonzept 3.5 (u. a. `valid_from` mit Uhrzeit beim Ersteintrag, `price_power` nie gelesen) |
| B7 | Δ-Spalte des Bestandsberichts rechnet auf formatierten Strings | **im Neumodul erledigt** (Δ aus Rohwerten); Altbericht unverändert, entfällt mit dessen Stilllegung |
| B8 | **neu:** Energie-Einstellungen werden beim Variantenanlegen **doppelt** kopiert (`ProjektDuplizierenCtrl` generisch + `VariantenCtrl.KopiereEnergieEinstellungen`) → doppelte `energy_project_settings`-Sätze möglich; `energy_price`-Zweitkopie läuft auf den Unique-Index und erzeugt eine MessageBox aus `DataRepository` | eine der beiden Kopien entfernen |
| B9 | **neu:** Projektduplizierung kopiert auch `Tab_Ergebnis*` mit → frische Variante zeigt den Simulationsstand des Stamms als eigenen (Aktualitätsprüfung meldet „vorhanden" statt „fehlt") | `Tab_Ergebnis` in die Ausnahmeliste des Duplizierers oder Zeitstempel/Ergebnis nach dem Anlegen löschen |
| B10 | **neu:** `KennzahlenKatalog` deckt Kap. 5 noch nicht voll ab (es fehlen u. a. Wärmebedarf je Verwendungszweck, Nutzungsgrade Kessel/BHKW, Speicherverluste, Einspeise-Kennzahl) | Katalog schrittweise auffüllen oder Kap.-5-Anspruch je Kennzahl als „geplant" kennzeichnen |
| B11 | **neu:** Anhang weist die tatsächlich verwendeten Emissionsfaktoren/Preise und den Strommix-Faktor noch nicht aus (Platzhaltertext aus Phase 2) | Anhang-Baustein um Faktoren-/Preistabelle ergänzen (Kap. 4 Baustein 8) |
| B12 | **neu:** Neue Formulare (`Form_Bericht`, `Form_AlsVariante`, `Form_Wirtschaftlichkeit*`) sind komplett im Code aufgebaut, deutsche Texte hart codiert, keine Satelliten-`.resx`; `BerichtTexte`-Wörterbuch fehlen die Wirtschaftlichkeits-Labels (K9) | Lokalisierungskonvention nachziehen (en-Bericht sonst gemischtsprachig) |
| B13 | **neu:** Menü-Verdrahtung offen — MDI-Menü hat weder „Bericht erstellen…" noch „Als Variante speichern…"; `[Wirtschaftlichkeit]`-Button in `Form_Start` fehlt (bewusst Designer-Handgriffe, `LIESMICH_Phase1`) | im Designer nachziehen |
| — | W1-Detailbefunde K1–K10 (Zuschüsse, Parameternachweis, veraltete Simulationen, verwaiste Ergebnisse …) | **Begleitkonzept `Konzept_Wirtschaftlichkeit.md` Kap. 8** |

---

**Nachverifikation (Kenndaten.accdb-Kopie, Chat-Upload 11.08.2026):**
`Tab_ProjektWerte` wie dokumentiert (Spalten `Worstcase`/`Bestcase` und
`…_Nutzungsdauer`; Access-Zugriffe sind case-insensitiv). Die Emissionsfaktoren
sind **in g/kWh** gepflegt (`Tab_Brennstoff_Stamm`: Erdgas 240, Heizöl 310,
Strom 560; zusätzlich Spalten `Staub`, `PE_Faktor`) — Einheiten-Annahme des
KostenEmissionRechners damit bestätigt. Wichtiger Befund: die Katalog-Kopien in
`energy_carrier` tragen fast durchweg `co2 = 0`; die Faktor-Kette
**Projektwert → Tab_Brennstoff_Stamm → energy_carrier** (Vorgabe 11.08.2026) ist
daher zwingend. `carrier_id` in den Ergebnis-Modultabellen sowie
`Berichtskonfiguration`, `Tab_ProjektWirtschaftlichkeit` und
`Tab_ErgebnisWirtschaftlichkeit` existieren in dieser Kopie noch nicht — sie
werden beim ersten Start der neuen Programmversion automatisch angelegt
(`StelleModulSpaltenSicher` / `StelleKonfigTabelleSicher` / `StelleTabellenSicher`).

## 12. Umsetzungsphasen (voller Umfang, Beschluss #3)

| Phase | Inhalt | Ergebnis |
|---|---|---|
| **1 — Fundament** | DTOs, `BerichtsDatenSammler` (lesend + Simulations-Trigger), `KennzahlenKatalog`, `VariantenCtrl` (inkl. „Als Variante speichern…"), `Form_Bericht`, DB-Tabelle `Berichtskonfiguration`, Befunde B1/B2 | Dialog zeigt korrekte Varianten-/Datenlage; Varianten headless simulierbar |
| **2 — Word-Kern** | `Berichtsvorlage.docx` (Styles, Deckblatt, Kopf-/Fußzeile), `WordBerichtGenerator` + Bausteine 1–4 und 6 (Übernahme der Bestandslogik, Blocksplitting, Abweichungstabellen) | erster vollwertiger Vergleichsbericht |
| **3 — Diagramme** | `ChartRenderer`: Balken + **4 Ganglinientypen** aus In-Memory-Simulation, Baustein 5 komplett | Vollbericht Word |
| **4 — Excel** | `ExcelBerichtGenerator` (Übersicht, Vergleich, Detailblätter) | beide Formate |
| **5 — Komfort** | Zeitstempel-/Warnlogik final, en-Lokalisierung, Feinschliff Vorlage/CI, Emissions-Verrechnung | produktionsreif |
| **6 — Wirtschaftlichkeit** | Reiter + Rechenmodul + Baustein nach `Konzept_Wirtschaftlichkeit.md` — **Stufe W1 umgesetzt (11.08.2026)**: `KapitalwertRechner`/`WirtschaftlichkeitCtrl`/`Form_Wirtschaftlichkeit` + Word-Baustein + Excel-Blatt; **Rechenkern unabhängig nachgerechnet und bestätigt**, Korrekturliste K1–K10 (Begleitkonzept Kap. 8) vor Produktivsetzung abarbeiten; W2/W3 offen | W1 fertig (mit Korrekturliste), W2/W3 eigenes Arbeitspaket |

Grobaufwand Phasen 1–5: **20–28 PT** (voller Umfang inkl. Ganglinien);
Wirtschaftlichkeit separat (Begleitkonzept).

---

*Konsolidierte Fassung 3.1: Fassung 3 (Cowork-Session 11.08.2026) + unabhängige
Code-Prüfung (zweite Cowork-Session, 11.08.2026 abends — Chart-Technik,
Tabellenanlage und Befundliste an den realen Code-Stand angepasst;
Prüfbericht: `Pruefbericht_Konsolidierung_2026-08-11.md`). Vorgänger:
`Konzept_Variantenbericht.md` (aufgegangen in dieser Fassung, Datei als Verweis
erhalten) und Erstfassung dieses Dokuments vom 11.08.2026. Historisches
Grundlagendokument: Reporting-Gerüst (`LIESMICH_Geruest.md`,
`Reporting_Geruest.zip`) — Ausbaupfad „format-neutrales Dokumentmodell + PDF",
derzeit zurückgestellt.*
