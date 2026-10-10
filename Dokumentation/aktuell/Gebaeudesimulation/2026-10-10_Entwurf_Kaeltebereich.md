# Entwurf KB — Kältebereich: Kälte-Kachel als Auswahldialog, Konfiguration in der Simulationskonfiguration

**Stand 10.10.2026 · Entwurf; Wellen KB-A, KB-B und KB-D gebaut (Abschnitt 7; KB-D2: Pfeile der Folge an der Seite), KB-C offen.** Gelesen auf `8100e397` (Zweig `ios_migration_september`, Basis R50, R51 für
FK angemeldet). Auftrag der Sitzung Gebäudesimulation; die Katalogauswahl der Kältemaschine ist Stufe 5 des
[Konzepts Projektdialoge Katalogauswahl](../Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md), das der Sitzung „Dialoge und
Korrekturen“ gehört — Abschnitt 4 schlägt den Schnitt zwischen beiden Sitzungen vor.

**Anforderungen des Anwenders (Wortlaut, 10.10.2026):**

- **(a)** „Checkbox ‚Kühlung rechnen‘ sollte in einem eigenen Bereich mit Konfiguration und Schema analog Wärme erscheinen.“
- **(b)** zum Dialog der Kachel „Kühlung und Kälteanlagen“: „Es sollten nur die Kälteanlagen ausgewählt werden mit Möglichkeit
  der Konfiguration. Dieser Dialog soll ähnlich zu den anderen sein (Wärmepumpen Verwaltung: oben ‚Im Projekt‘ mit ‚Aus dem
  Projekt entfernen‘, darunter ‚Katalog (Datenbank)‘ mit Suche, Spaltenwahl, Vergleichen, ‚In das Projekt übernehmen‘,
  ‚Markierte auf dieses Gerät umstellen‘, unten ‚Gewählter Satz‘ mit Details, OK/Abbrechen). Die Konfiguration soll dann unter
  Simulation-Konfiguration erfolgen.“

## 0 Das Ergebnis in Punkten

1. Der Kältedialog wird ein **Auswahldialog** auf dem Baustein `Zweispaltenauswahl` (V1): Kältemaschinen im Projekt, Katalog
   `Tab_Kaeltemaschine_STAMM`, gewählter Satz. Die Betriebseingaben und die Schalter der Wärmepumpen verlassen ihn.
2. Die Simulationskonfiguration bekommt unter dem Wärmeteil einen **Bereich „Kälte“**: Schalter „Kühlung rechnen“ im
   Bereichskopf, links die Kälteerzeuger in Rechenfolge als Kacheln (Kältemaschinen, Wärmepumpen im Kühlbetrieb), rechts die
   Kältespeicher. Bei „aus“ ist er eingeklappt und sagt das.
3. Die **Anlagenkonfiguration der Kältemaschine** (Name, Anzahl, Kaltwasservorlauf, Hilfsstrom, Kühlträger, eigener Zähler)
   wird eine eigene Komponente. Sie öffnet aus der Kachel des Kältebereichs und als Überlagerung „Anlage…“ aus dem Dialog,
   wie die Wärmepumpe.
4. **Befund:** Die Kältefolge ist heute nicht pflegbar. Erst kommt die freie Kühlung, dann die Kältespeicher, dann die
   Wärmepumpen in Modulfolge, danach die Kältemaschinen nach Anlagen-ID. Empfohlen wird, sie zuerst nur sichtbar zu machen.
   Pflegbar würde sie mit einer optionalen Welle (Schemaschritt, Kern, Pfeile).
5. Der **Rechenweg bleibt unberührt**, die Basis auch (Abschnitt 5.8). Eine Ausnahme ist nur die optionale Welle KB-D. Selbst
   dort bleibt die Basis byte-gleich, solange die neue Spalte leer ist.
6. Aufwand: Sitzung Gebäudesimulation **3,5–4,25 PT** (KB-A bis KB-C), dazu optional **1,5–2 PT** (KB-D). Sitzung Dialoge
   **2,5–3,5 PT** (Stufe 5).

## 1 Bestand

### 1.1 Der Dialog „Kältemaschinen im Projekt“ (`EPOS.UI/Dialoge/Erzeuger/KaeltemaschineAnlageDialog.razor`, 728 Zeilen)

| Teil | Ort | Was er kann und schreibt |
|---|---|---|
| Aufbau | `:1–23` (Kopfkommentar), `:33` | Eigener Dialog `epos-kaeltemaschine-anlage`, **nicht** auf `Zweispaltenauswahl`; Titel `KMA_TITEL` „Kältemaschinen im Projekt“ |
| Anlagenliste | `:53–80` | Tabelle Name, Anzahl, Nennkälteleistung der Anlagenzeilen (Typ 13); Klick wählt |
| Wärmepumpen im Kühlbetrieb | `:84–106` | Je Wärmepumpe mit Kühlfunktion ein `Schalter` mit Sperrgrund. Darunter steht der Hinweis `KMA_WP_HINWEIS`. Geschrieben wird beim OK über `WaermepumpenSchreiben` (`:479`) → `KuehlungKachelBau.KuehlbetriebSchreiben` → `WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten` |
| Gerät (nur lesen) | `:112–141` | Bezeichner, Firma, Typ, Nennkälteleistung, Nenn-EER, Rückkühlart, Mindestteillast, kleinster Kaltwasservorlauf, Teillastweg, Verdichterregelung, Taktverlust, Kennfeldrand |
| Betrieb (Eingaben) | `:143–172` | Name, **Anzahl** (mit Herleitung „Folgeschaltung“), **Kaltwasservorlauf**, **Hilfsstromanteil** %, **Kühlträger**, **eigener Zähler**. Dazu der Hinweis `KMA_HINWEIS_REIHENFOLGE` „Die Kältemaschinen rechnen nach den Wärmepumpen im Kühlbetrieb.“ |
| Fuß | `:176–182` | OK/Abbrechen; „Hinzufügen…“, „Löschen“ (Rückfrage, merkt vor) |
| Katalogwahl | `:185–218` | Überlagerung mit `Katalogliste` (`ZeileIstWahl`), „Typkennfelder laden…“, „Katalogverwaltung…“ (`Dienste.Navigation` → `Seitenschluessel.KaeltemaschineKatalog`), „Übernehmen“ |
| Speichern | `:631–676` | Nichts ohne OK: prüft jede Anlage (`KaeltemaschineAnlageCtrl.Pruefen`), löscht, legt an (`Anlegen` → Projektkopie und Anlagenzeile), schreibt geänderte Anlagen (`KaeltemaschineAnlageCtrl.Speichern`), dann die Wärmepumpen |

**Hülle:** `EPOS.UI.Daten/Erzeuger/KaeltemaschineAnlageHuelle.cs:22–47`. **Kern:** `EPOS.Kern/Controller/KaeltemaschineAnlageCtrl.cs`
mit `Liste` (`:57`, `ORDER BY a.ID` `:60`), `Anlegen` (`:97`), `Pruefen` (`:124`) und `Speichern` (`:137–170`). `Speichern`
schreibt `Bezeichner`, `Kaeltemaschine_Anzahl`, `Kuehl_ID_Carrier` und `Kuehl_EigenerZaehler` an die Anlagenzeile, `Kuehl_Vorlauf`
und `Kuehl_Hilfsstromanteil` an die **Projektkopie**. Dazu kommt `Loeschen` (`:172`).

**Befund B1 — geteilte Projektkopie.** `KaeltemaschineCtrl.AusKatalogUebernehmen`
(`EPOS.Kern/Controller/KaeltemaschineStammCtrl.cs:559–570`) gibt eine vorhandene Kopie desselben Katalogsatzes zurück. Zwei
Anlagen desselben Geräts teilen damit Kaltwasservorlauf und Hilfsstromanteil, obwohl der Dialog sie je Anlage zeigt. Die
letzte gespeicherte Anlage gewinnt.

**Befund B2 — Katalog- und Rückweglücken.** Für die Kältemaschine gibt es kein `Rueckweggewerk`, keinen
`KatalogsatzLoeschen` mit Satzvorlage und kein Umstellen. Auch einen öffentlichen Schreibweg der Projektkopie (Kopf, Kennlinie,
Teillast) gibt es nicht; `KopfSchreiben` und `KennlinieSchreiben` sind `internal` (`KaeltemaschineStammCtrl.cs:424`, `:474`).
Vorhanden sind `ID_Stamm` an der Kopie und `ID_KostenVorlage`/`ID_KostenVorlageInvestition` am Katalog (Schritte 208/209).

### 1.2 Die Kachel „Kühlung und Kälteanlagen“

`EPOS.UI/Seiten/Start/ErzeugerReiter.razor:92–104` zeigt Titel, Beschreibung und Statuspunkt. Der Punkt ist grün bei einer
Kältemaschine oder einer Wärmepumpe im Kühlbetrieb (`:180–183`). Ein Klick öffnet den Dialog über
`Dienste.Navigation.OeffneMaske(Seitenschluessel.KaeltemaschineAnlage)` (`:207–216`); eine Ablehnung steht im Banner (KT-2). Die
Daten liefert `EPOS.UI.Daten/Erzeuger/KuehlungKachelBau.cs:25–51`.

### 1.3 Die Simulationskonfiguration zur Kälte

| Teil | Ort | Stand |
|---|---|---|
| Liste, Erzeugerspalte | `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor:78–175`; Gruppen `EPOS.UI.Daten/Simulation/SimulationKonfigHuelle.cs:560–568` (Wärmeerzeuger, Stromerzeuger, Energiespeicher) | **Keine Kältemaschine.** Eine Wärmepumpe steht als Wärmeerzeuger; ihr Kühlbetrieb liegt in der Komponentenkonfiguration (`WaermepumpeKuehlGaben`, Seite `:2550–2566`) mit Kühlbetrieb, Kühlvorlauf, Kühlträger, Zähler und freier Kühlung |
| Liste, Speicherspalte | Seite `:177–265`, Hülle `SpeicherSpalte` `:1367` | Kältespeicher stehen zwischen den Wärmepuffern; die Kachel nennt die Verwendung (`:1524–1582`) |
| Schalter „Kühlung rechnen“ | Seite `:405–424` im Block „Weitere Einstellungen“ (`:342`); `KuehlbetriebGesetzt` `:1258–1268`; Weg `SimulationAnsichtDaten.KuehlbetriebSchreiben` (`EPOS.UI/Seiten/Simulation/SimulationAnsichtDaten.cs:334`) → `KonfigurationCtrl.KuehlbetriebSetzen` (`EPOS.UI.Daten/Simulation/SimulationErgebnisHuelle.cs:843`; Kern `KonfigurationCtrl.cs:789`, `:1153–1181`) | Schreibt sofort; scheitert das Schreiben, setzt `@key` den Schalter zurück |
| Schema | `Schema` `:65–72`; Kältebahn `EPOS.Kern/Allgemein/Simulation/SchemaModell.cs:663–760`, DTO `SimulationKonfigHuelle.cs:1844–1952` | Kältebahn seit #892: Rückkühlung bzw. Quelle, Kälteerzeuger, Kältespeicher, Kältekreis, Satz „Kälte-Kaskade: …“. Bei Schalter aus trägt der Kältekreis „Projekt rechnet keine Kälte“. Doppelklick seit #902: Kältemaschine und Rückkühlung öffnen den Anlagendialog über die Navigation, die Wärmepumpe ihren Senkendialog, ihre Quelle die Quellenwahl, der Kältespeicher die Pufferverwaltung |

### 1.4 Kälteerzeuger im Kern: wo Folge, Träger und Zähler stehen

- **Folge** (`EPOS.Kern/Allgemein/Simulation/SimulationControl.Kaelte.cs`): Die Wärmepumpen im Kühlbetrieb rechnen in der Folge
  ihrer Module (`:104–116`). Damit gilt die Wärmekaskade `Tool_1…4`, und eine Wärmepumpe ohne Wärmeplatz kühlt nicht.
  Danach kommen die Kältemaschinen als Typ, untereinander nach Anlagen-ID (`:372–382`). In Stunden freier Kühlung deckt eine
  Maschine mit Trocken- oder Nasskühler vor allen anderen (`Kaeltekaskade.cs:548–552`, `Kaeltemaschine.cs:303`). Kältespeicher
  entladen nach der freien Kühlung und vor den Erzeugern, geordnet nach `Entladeprio` (`:740–775`). **Diese Folge ist nirgends
  pflegbar.** Nur die Wärmepumpen lassen sich mittelbar über die Wärmekaskade umordnen.
- **Kühlträger und Zähler:** Bei der Kältemaschine stehen sie an der Anlagenzeile und werden im Dialog gepflegt (1.1). Bei der
  Wärmepumpe stehen sie an deren Anlagenzeile und werden in der Komponentenkonfiguration der Simulationskonfiguration gepflegt.
- **Freie Kühlung:** Bei der Kältemaschine hängt sie allein an der Rückkühlart, einem Gerätemerkmal ohne Schalter. Bei der
  Wärmepumpe ist sie ein Schalter in ihrer Kühlkonfiguration (KU3-6). An ihr arbeitet die laufende Welle FK (Referenzprojekt
  1064, R51).

### 1.5 Tests und Prüfungen, die Halt geben

`EPOS.UI.Tests/Dialoge/KaeltemaschineAnlageDialogTests.cs` (21), `…/KaeltemaschineAnlageWaermepumpenTests.cs` (7),
`…/WaermepumpeKuehlbetriebTests.cs` (12), `EPOS.UI.Tests/Seiten/ErzeugerReiterKuehlungTests.cs` (6),
`…/SimulationKonfigKaeltebahnTests.cs` (8), `…/SimulationKonfigSeiteTests.cs` (11 Treffer `kuehlung`/`Kuehlbetrieb`),
`EPOS.UI.Tests/Bausteine/SchemaKaeltebahnTests.cs` (8), `EPOS.Kern.Tests/KaeltemaschineAnlageHuelleTests.cs` (3),
`…/KaelteschemaTests.cs` (6), `…/KaeltemaschineAnlageDatenbankTests.cs`. Zur KI-Sicht gehören
`EPOS.UI/Dialoge/Erzeuger/KaeltemaschineAnlageKiSicht.cs` mit der Feldkarte `KiDialoge.KaeltemaschineAnlage`
(`EPOS.Kern/Allgemein/KI/Dialoge/KiDialoge.cs:5597`) und `SimulationKiSicht.Kuehlbetrieb`
(`EPOS.UI/Seiten/Simulation/SimulationKiSicht.cs:349–370`, Test `KiSimulationMaskeTests`, 7 Treffer). Dazu kommen
`KiMaskenabdeckungWacheTests` und `KiDialogkatalogTests`. Proben: `Proben/Rasterprobe/rollbereichprobe.mjs:81–85,212` führt die
Kältemaschine noch als Fall **außerhalb** des Bausteins („Stufe 5“).

## 2 Zielbild Dialog (Kälte-Kachel)

### 2.1 Aufbau nach V1 (Muster Heizkessel/BHKW, Konzept 4.1–4.6, 5.3, 5.4)

```
┌ Kältemaschinen ·································· i  KI  ✕ ┐
│┌[IM PROJEKT] 2 Anlagen · 3 Maschinen · Summe 300 kW Kälte          ┐│
││  Anlage… · Bearbeiten… · In die Datenbank übernehmen… · ▼ Entfernen ││
││ ☐ Anlage │ Hersteller │ Typ │ Anzahl │ Nennkälte kW │ EER │ Rückk. ││
││ Kühlen außerdem: Wärmepumpe 1 (Kühlbetrieb an) → Simulation › Kälte ││
│└════════════════════ Trennlinie ════════════════════════════════════┘│
│┌[KATALOG (DATENBANK)] Suche · 34 von 34 · Filter zurücksetzen        ┐│
││                                         ▲ In das Projekt übernehmen ││
││ ☐ Katalogliste (Profil Kaeltemaschine, Herkunft, Typkennfelder aus) ││
││ Satz: Vergleichen… · Schloss… · Löschen · Bearbeiten… · Umstellen   ││
││                         Typkennfelder laden… · Neu…                 ││
│└─────────────────────────────────────────────────────────────────────┘│
│┌▸[PROJEKTSATZ] Kältemaschine 1 · 150 kW · EER 3,2 · Trockenkühler Details┐│
└ Abbrechen · OK ──────────────────────────────────────────────────────┘
```

| Funktion | Ort (Konzept 4.2) | Kernweg |
|---|---|---|
| Projektliste: **eine Zeile je Anlage**; Anzahl als Lesespalte; Summe der Kälteleistung (Anzahl × Nennkälteleistung) | P | `KaeltemaschineAnlageCtrl.Liste` (vorhanden) |
| ▲ In das Projekt übernehmen (Doppelklick, Enter, Mehrfachwahl) | K | `Anlegen` (vorhanden) |
| ▼ Aus dem Projekt entfernen | P | `Loeschen` (vorhanden) |
| **Markierte auf dieses Gerät umstellen** (genau eine Projekt- und eine Katalogzeile). Anzahl, Kühlträger, Zähler, Name, Kosten und die Kühleingaben bleiben | K-Fuß wie die Wärmepumpe (`WaermepumpenDialog.razor:116`) | **neu:** `KaeltemaschineAnlageCtrl.Umstellen(anlagenId, stammId)` |
| Suche, Spaltenwahl, Verwendungsmarke, Herkunft, „Typkennfelder ausblenden“ | K | vorhanden (`Katalogfilterprofil`, #881) |
| Vergleichen… | K | `KaeltemaschineParameteruebersicht` (vorhanden, 14 Zeilen) |
| Bearbeiten… (Katalogsatz), Neu… | K → Ü | `KaeltemaschineKatalogDialog` als Überlagerung (KA‑E‑13) |
| Schloss…, Löschen | K | `SchlossSetzen` (vorhanden); `KatalogsatzLoeschen` mit Satzvorlage **neu** (KA‑E‑16) |
| Typkennfelder laden…, Katalogverwaltung… | K-Fuß rechts | vorhanden |
| Bearbeiten… (Projektkopie, KA‑E‑8) | P → Ü | **neu:** öffentlicher Schreibweg der Kopie (Kopf, Kennlinie, Teillast) |
| In die Datenbank übernehmen… | P | **neu:** `Rueckweggewerk` Kältemaschine — erstes Gewerk **mit Kindzeilen** (`Tab_Kenndaten_Kaeltemaschine`); Teillastspalten in der Schnittmenge; kein Schemaschritt |
| **Anlage…** (Konfiguration der Anlage) | S → Ü (KA‑E‑11) | dieselbe Komponente wie im Kältebereich (3.3) |
| Kenndaten zugeklappt; aufgeklappt „Alle Daten“; beim Projektsatz Kosten… | S | Kostenweg der Komponente `ERZEUGER_KAELTEMASCHINE` (`TechnikPlanwertCtrl.cs:168`) |

### 2.2 Was herausfällt und wohin es wandert

| Heute im Dialog | Ziel |
|---|---|
| Schalter „Wärmepumpen im Kühlbetrieb“ (`:84–106`) | Kältebereich der Simulationskonfiguration (3.2). Im Dialog steht nur eine **Lesezeile** unter der Projektliste („Kühlen außerdem: …“, ohne Schalter), damit die Kachel weiter alle Kälteerzeuger nennt (Frage F3) |
| Name, Anzahl, Kaltwasservorlauf, Hilfsstromanteil, Kühlträger, eigener Zähler (`:143–172`) | Komponente `KaeltemaschineKonfiguration` (3.3): Hauptort ist der Kältebereich, Nebenort die Überlagerung „Anlage…“ (F2). Die Anzahl bleibt als Lesespalte in der Projektliste |
| Gerätefelder nur lesen (`:112–141`) | Detailzeile „Gewählter Satz“ (zugeklappt die Kenndaten, aufgeklappt alle Daten) |
| Hinweis „rechnen nach den Wärmepumpen“ | Kältebereich, Herleitungszeile der Folge (3.4) |
| Arbeitsstand bis OK | bleibt für Übernehmen, Entfernen und Umstellen; die Überlagerung „Anlage…“ schreibt mit ihrem eigenen OK |

**Festlegung KB-1 (gegen B1):** Neu angelegte Anlagen bekommen je **eine eigene Projektkopie**: `Anlegen` verwendet keine Kopie
mehr wieder. Bestehende geteilte Kopien bleiben stehen. Die Konfiguration zeigt dann den Hinweis „Vorlauf und Hilfsstrom gelten
für n Anlagen dieses Geräts“. Die Rechnung der Bestandsprojekte ändert sich nicht.

## 3 Zielbild Simulationskonfiguration „Kälte“

### 3.1 Ort und Aufbau

In der Ansicht LISTE steht **unter** den Spalten Erzeuger/Speicher, im selben `fieldset.epos-simkonfig-bereich` und damit unter
derselben Sperre, ein Bereich `section.epos-simkonfig-kaelte`:

```
┌ Kälte ············· [■ Kühlung rechnen]  (i) ┐
│┌ Kälteerzeuger (Rechenfolge) ─────┐┌ Kältespeicher ──────┐│
││ ① freie Kühlung: KM 1 (Trockenk.)││ Kältespeicher 1     ││
││ ② Wärmepumpe 1   Kühlbetrieb [an]││ 2 m³ · 6/12 °C      ││
││    KV 18 °C · Träger Projekt     ││ Entladeprio 1       ││
││ ③ Kältemaschine 1  2 × 150 kW    │└─────────────────────┘│
││    KW 6 °C · Hilfsstrom 5 % ·    │                       │
││    Kühlträger Strom 2 · eig. Zähler [Konfigurieren…]    │
│└──────────────────────────────────┘                       │
│ Folge: freie Kühlung, Kältespeicher, Wärmepumpen in der   │
│ Folge der Wärmekaskade, Kältemaschinen in Anlagenfolge.   │
└───────────────────────────────────────────────────────────┘
```

- **Bereichskopf:** `Gruppenkopf` „Kälte“ mit dem Schalter „Kühlung rechnen“ und dem InfoKnopf
  `Form_Simulation_Config_Kuehlung.btn_Help`. Der Schalter zieht aus „Weitere Einstellungen“ (`:405–424`) hierher und behält
  denselben Weg (`KuehlbetriebSchreiben`), sofortiges Schreiben, `@key`-Rückfall und `SimulationKiSicht.Kuehlbetrieb`. Der
  Wärmeteil darüber bekommt zur Symmetrie den Kopf „Wärme“ (Ressource, beide Sprachen).
- **Kälteerzeuger** als `ErzeugerKachel` je Kälteerzeuger, aus einer neuen `KachelGruppe` „Kälteerzeuger“
  (`SimulationKonfigHuelle.KaelteGruppe`). Chips zeigen die Kälteleistung (Anzahl × Nennleistung), den Kaltwasser- bzw.
  Kühlvorlauf, den Kühlträger, den eigenen Zähler und die freie Kühlung.
- **Kältespeicher** als `SpeicherKachel` der Puffer mit Verwendung Kälte. Sie wechseln aus der Speicherspalte der Wärme hierher
  (F4). „Bearbeiten“ öffnet wie im Schema die Pufferverwaltung.

### 3.2 Die Kälteerzeuger

| Erzeuger | Kachel | Aufnehmen/Entfernen | Bearbeiten |
|---|---|---|---|
| Kältemaschine (Anlage Typ 13) | immer aufgenommen: Sie rechnet, solange sie im Projekt ist (1.4); kein Entfernen hier, das Entfernen gehört in den Dialog | — | „Konfigurieren…“ → Überlagerung `KaeltemaschineKonfiguration` (3.3) |
| Wärmepumpe mit Kühlfunktion | **Aufnehmen = Kühlbetrieb an**, Entfernen = aus; Weg `WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten`. Ein Sperrgrund steht am Element, auch „nicht in der Wärmekaskade“ mit dem Handgriff „in die Wärme aufnehmen“ | wie die Wärmeerzeuger (verfügbar ↔ aufgenommen) | vorhandene Komponentenkonfiguration der Wärmepumpe mit `WaermepumpeKuehlGaben`, geöffnet am Abschnitt Kühlung |
| freie Kühlung | Lesezeile: Kältemaschine mit Trocken- oder Nasskühler (aus der Rückkühlart), Wärmepumpe mit Schalter „freie Kühlung“ (ihre Konfiguration). Was FK (R51) neu an Eingaben bringt, kommt in dieselbe Konfiguration | — | über den Erzeuger |

Ob der Kühlbetrieb einer Wärmepumpe mit „Konfiguration speichern“ (Arbeitsstand) oder sofort schreibt, ist eine Frage der
Einheitlichkeit. **Empfehlung:** sofort, wie heute an der Wärmepumpe und am Projektschalter. Der Kältebereich hat dann keinen
Arbeitsstand, solange die Folge fest ist (3.4).

### 3.3 Komponente `KaeltemaschineKonfiguration`

Die Komponente wird aus `KaeltemaschineAnlageDialog.razor:143–172` herausgelöst: Name, Anzahl mit Folgeschaltungshinweis,
Kaltwasservorlauf (Platzhalter und Untergrenze), Hilfsstromanteil, Kühlträger, eigener Zähler und die Gerätekenndaten als
Lesezeile. OK schreibt über `KaeltemaschineAnlageCtrl.Pruefen` und `Speichern`, Abbrechen verwirft. Eingebunden wird sie wie
`KomponentenKonfigurationDialog` (Seite `:640–655`) als Überlagerung mit `TitelAnzeigen="false"`. Daten und Texte kommen als
`KaeltemaschineKonfigurationDaten` bzw. `…Texte`, die KI-Sicht als eigene Feldkarte. Sie ersetzt die Betriebsfelder der heutigen
Karte `KaeltemaschineAnlage`.

### 3.4 Folge der Kälteerzeuger

**Befund:** nicht pflegbar (1.4). **Vorschlag in zwei Schritten:**

1. **KB-B (Pflicht):** Die Kacheln stehen in der Rechenfolge des Kerns; ohne Pfeile, die Nummer ① ② ③ ist Anzeige. Eine
   Herleitungszeile nennt die Regel. Für die Reihenfolge braucht es einen **Leser im Kern**,
   `Kaelteerzeugerfolge.Lesen(idProjekt)`, der dieselbe Regel wie `KaelteerzeugerVorbereiten` und `KaeltemaschinenVorbereiten`
   anwendet, damit Seite und Schema nicht selbst sortieren. Eine Probe hält die Liste gegen `Kaeltekaskade.Erzeuger` eines Laufs.
2. **KB-D (optional, Frage F1):** Die Folge wird pflegbar. Ein Schemaschritt legt `Tab_Energieanlagen.Kaelte_Rang`
   (INTEGER, nullbar) an, für Kältemaschinen und Wärmepumpen. Der Kern sortiert nach Rang; ist er leer, gilt die heutige Regel.
   Ein Rang kann eine Wärmepumpe nur innerhalb der Kälte vor oder hinter eine Kältemaschine stellen; die Wärmekaskade bleibt,
   wie sie ist. Die freie Kühlung bleibt vorn. Die Pfeile der Kachel schreiben in den Arbeitsstand von „Konfiguration
   speichern“. In den Referenzprojekten bleibt der Rang leer, die Basis damit byte-gleich. Die Einfrierregel zur Kälte bekommt
   einen Satz: Ein Rang in einem Referenzprojekt friert neu ein.

### 3.5 Verhalten bei „Kühlung rechnen“ aus

Der Bereich klappt auf den Kopf zusammen und zeigt eine Hinweiszeile: „Das Projekt rechnet keine Kälte. Angelegt: n
Kältemaschinen, m Wärmepumpen mit Kühlfunktion, k Kältespeicher — sie bleiben gespeichert und wirken nicht.“ Das entspricht dem
Satz im Schema („Projekt rechnet keine Kälte“). Ohne Projekt oder ohne Schreibweg (`KuehlbetriebSchreiben` null) steht der
Schalter nicht da, wie heute auch. Hat das Projekt weder Kälteerzeuger noch Kältespeicher, sagt eine Leerzeile, wo man sie
anlegt: Kachel „Kühlung und Kälteanlagen“, Wärmepumpe, Pufferspeicher mit Nutzung Kälte.

### 3.6 Schema

Die Kältebahn bleibt (#892), ebenso die Farben, die Legende und der Satz „Kälte-Kaskade“. Er liest künftig denselben
Folge-Leser (3.4). Der Doppelklick auf eine Kältemaschine oder ihre Rückkühlung öffnet in der Simulationskonfiguration die
Überlagerung `KaeltemaschineKonfiguration` und nicht mehr den Dialog über die Navigation (#902). So verlässt man die Seite nicht,
und auf dem iPad entfällt der Rückweg zur Liste, der in #902 offen blieb. Die Wärmepumpe im Kühlbetrieb öffnet ihre
Komponentenkonfiguration am Abschnitt Kühlung statt des Senkendialogs (F5).

### 3.7 Kleine Fenster

Der Bereich nutzt das Raster `epos-simkonfig-spalten` (61,5/38,5; unter 900 px einspaltig, `epos-ui.css:5395–5404`) und die
Kompaktstufe des Hauses (#886/#897: `max-width: 1279.98px` oder `max-height: 799.98px`). Er bekommt keinen eigenen Rollbereich,
es rollt die Seite. Die Überlagerung `KaeltemaschineKonfiguration` folgt der Haftregel „Kopf und Fuß im eigenen Fenster“
(`fensterprobe.mjs`).

## 4 Abgrenzung und Abstimmung mit der Sitzung „Dialoge und Korrekturen“

### 4.1 Wer baut was

| Teil | Sitzung | Begründung |
|---|---|---|
| KB-A Kernleser der Kältefolge, Hülle `KaelteGruppe`, DTO, Festlegung KB-1 | Gebäudesimulation | Rechenweg Kälte, Kältebahn (#892) |
| KB-B Kältebereich der Seite, Schalterumzug, Komponente `KaeltemaschineKonfiguration`, Doppelklickziele im Schema | Gebäudesimulation | Seite und Hülle der Simulationskonfiguration |
| KB-C Proben, Wiki Simulation/Kühlung | Gebäudesimulation | — |
| KB-D pflegbare Folge (optional) | Gebäudesimulation | Schemaschritt und Kern |
| **Stufe 5** Dialog auf `Zweispaltenauswahl`, Umstellen, Rückweggewerk mit Kindzeilen, Löschweg, Projektkopie bearbeiten, Rollbereichprobe ohne Ausnahme, Konzept nach `ueberholt/` | Dialoge und Korrekturen | Konzeptbesitzer, Muster 5.3/5.4 |

### 4.2 Gemeinsam berührte Dateien

| Datei | Gebäudesimulation | Dialoge |
|---|---|---|
| `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor` | Bereich Kälte, Schalterumzug, `SchemaBearbeiten` (Zweige `KERZEUGER_`/`KQUELLE_`) | keine Änderung |
| `EPOS.UI.Daten/Simulation/SimulationKonfigHuelle.cs`, `SimulationErgebnisHuelle.cs`, `SimulationAnsichtDaten.cs` | neue Gruppe und Wege | keine |
| `EPOS.UI/Bausteine/Schema.razor`, `SchemaModell.cs` | Folge-Leser in `KaelteBahnAnlegen` | keine |
| `EPOS.UI/Dialoge/Erzeuger/KaeltemaschineAnlage*.{razor,cs}`, `KaeltemaschineAnlageHuelle.cs` | **nur** Herauslösen der Komponente (KB-B; der Dialog bindet sie einstweilen ein) | Umbau Stufe 5 |
| `EPOS.Kern/Controller/KaeltemaschineAnlageCtrl.cs`, `KaeltemaschineStammCtrl.cs` | `Anlegen` ohne Wiederverwendung (KB-1) | `Umstellen`, `Rueckweg()`, `KatalogsatzLoeschen`, Kopie schreiben |
| `EPOS.Kern/Allgemein/KI/Dialoge/KiDialoge.cs` | neue Karte `KaeltemaschineKonfiguration` | Karte `KaeltemaschineAnlage` umbauen |
| `EPOS.Kern/MyResource/Resource*.resx`, `Resource.Designer.cs` | eigene Schlüssel `SIMKONF_KAELTE_*`, `KMK_*` | eigene Schlüssel `KMA_*` |
| `Projekte/Wiki/Programm Dokumentation - Kühlung.wiki` | Abschnitte „Kühlung einschalten“, „Deckung“ | Abschnitt „Kältemaschine“ (Dialog) |

### 4.3 Merge-Reihenfolge

1. **KB-A, KB-B** (Gebäudesimulation). Der Kältebereich steht; der alte Dialog bindet die herausgelöste Komponente ein, und
   nichts geht verloren.
2. **Stufe 5** (Dialoge) auf dem Stand von 1: Die Betriebsfelder und Wärmepumpenschalter verlassen den Dialog, „Anlage…“
   öffnet die Komponente. Das Doppelklickziel aus #902 hat bis dahin KB-B umgestellt.
3. **KB-C**, danach Wiki-Sammelupload. KB-D jederzeit nach 1, unabhängig von Stufe 5.

Laufen beide Sitzungen gleichzeitig, rebased Stufe 5 vor dem Merge auf KB-B. Konflikte erwartet nur `KiDialoge.cs` (verschiedene
Methoden) und `Resource.resx` (nur angehängt; der Designer wird nach dem Merge neu geschrieben).

### 4.4 Abstimmungsnotiz an die Sitzung „Dialoge und Korrekturen“

> **Von:** Sitzung Gebäudesimulation · **Betreff:** Stufe 5 der Katalogauswahl (Kältemaschine) und Kältebereich der
> Simulationskonfiguration · **Grundlage:** dieser Entwurf, Abschnitte 2 und 4.
>
> Der Anwender wünscht (10.10.2026), dass der Kältedialog „nur die Kälteanlagen auswählt“, nach dem Muster der Wärmepumpe mit
> Umstellen, und dass die Konfiguration in die Simulationskonfiguration wandert. Die Gebäudesimulation baut den Kältebereich
> (KB-A, KB-B) und löst die Betriebsfelder des Dialogs als Komponente `KaeltemaschineKonfiguration` heraus. Die Stufe 5 bleibt
> bei euch. Wir bitten um:
>
> 1. **Stufe 5 nach Abschnitt 2.1** bauen: Projektliste je Anlage (Anzahl nur lesen), Umstellen, Rückweggewerk mit den Kindzeilen
>    `Tab_Kenndaten_Kaeltemaschine` und den Teillastspalten, `KatalogsatzLoeschen` mit Satzvorlage, Bearbeiten der Projektkopie,
>    „Anlage…“ als Überlagerung mit unserer Komponente, Lesezeile der Wärmepumpen im Kühlbetrieb ohne Schalter.
> 2. **Erst nach unserem Merge KB-B** die Betriebsfelder und Schalter aus dem Dialog nehmen (4.3).
> 3. Festlegung **KB-1** (eigene Projektkopie je neuer Anlage) mittragen: `Umstellen` legt ebenfalls eine eigene Kopie an.
> 4. `SimulationKonfigSeite.razor`, `SimulationKonfigHuelle.cs` und die Kältebahn nicht anfassen; das Doppelklickziel aus #902
>    stellen wir in KB-B um.
>
> **Antwortspalte der Sitzung Dialoge:** _(Zustimmung, Abweichung, geplanter Zweig, Merge-Termin)_

## 5 Folgen

### 5.1 Kern

**Leser:**

- `Kaelteerzeugerfolge.Lesen` (KB-A); `SchemaModell.KaelteBahnAnlegen` liest künftig daraus.

**Schreiber:**

- `KaeltemaschineAnlageCtrl.Anlegen` mit eigener Kopie (KB-1).
- `Umstellen`, `KaeltemaschineStammCtrl.Rueckweg()`, `RueckwegVorschau`, `AusProjektUebernehmen`, `RueckwegNameBelegt`,
  `KatalogsatzLoeschen` und ein öffentliches `KaeltemaschineCtrl.Speichern` der Kopie (Stufe 5).
- Optional `Kaelte_Rang` mit Schemaschritt (KB-D); die Nummer meldet die Umsetzung vor dem Bau an.

Nach jeder neuen SQL-Anweisung läuft der `SqlDialektPruefer`.

### 5.2 `EPOS.UI.Daten`

- `SimulationKonfigHuelle`: `KaelteGruppe`, Kältespeicher aus `SpeicherSpalte` herausgenommen, Gaben der Konfiguration.
- `SimulationAnsichtDaten`: Wege `KaeltemaschineKonfigurationLaden`, `…Speichern` und `KuehlbetriebWpSchreiben`; die Wege bleiben
  benannt, ohne Plattformnaht.
- `KaeltemaschineAnlageHuelle` wird in Stufe 5 auf die Gaben der `Zweispaltenauswahl` umgebaut; `KuehlungKachelBau` bleibt für
  Statuspunkt und Lesezeile.

### 5.3 Oberfläche

- Seite: Bereich Kälte, Schalterumzug, Kopf „Wärme“.
- Neue Komponente `EPOS.UI/Dialoge/Erzeuger/KaeltemaschineKonfiguration.razor` samt Daten und Texten.
- Stilblatt: `.epos-simkonfig-kaelte` mit Kennfarbe der Kältebahn.
- Dialog: Stufe 5.

### 5.4 KI-Sicht und Feldkarten

- `SimulationKiSicht`: die Kälteerzeuger als Liste mit Wahl, Kühlbetrieb der Wärmepumpe; `Kuehlbetrieb` bleibt.
- Neue Karte `KaeltemaschineKonfiguration`; die Karte `KaeltemaschineAnlage` verliert die Betriebsfelder und bekommt die Felder
  der Zweispaltenauswahl wie BHKW und Heizkessel.
- `KiMaskenabdeckungWacheTests` und `KiDialogkatalogTests` laufen nach.

### 5.5 Ressourcen

Beide Sprachen, nach jedem Schlüssel `designer_neu.py schreiben`. Vorgesehen sind etwa 20 Schlüssel:

- `SIMKONF_GRP_KAELTE`, `SIMKONF_GRP_WAERME`, `SIMKONF_KAELTE_AUS`, `SIMKONF_KAELTE_LEER`, `SIMKONF_KAELTE_FOLGE`,
  `SIMKONF_KAELTE_KONFIG`;
- `KMK_*` aus den heutigen `KMA_LBL_*` (umbenannt, nicht doppelt);
- `KMA_WP_LESEZEILE`, `KMA_UMSTELLEN`, `KMA_TIP_UMSTELLEN`.

`SIMKONF_GRP_KUEHLUNG` und `SIMKONF_HRL_KUEHLBETRIEB` bleiben.

### 5.6 Tests

- **bunit:**
  - `SimulationKonfigKaeltebereichTests` (neu, etwa 14 Fälle): Bereich, Schalter, eingeklappt, Folge, WP an/aus, Sperrgrund,
    Konfiguration, Kältespeicher, Doppelklick;
  - `KaeltemaschineKonfigurationTests` (neu, etwa 10 Fälle; Fälle aus `KaeltemaschineAnlageDialogTests` übernommen);
  - Anpassungen in `SimulationKonfigSeiteTests` (Selektor `.epos-simkonfig-kuehlung`), `SimulationKonfigKaeltebahnTests` und
    `KiSimulationMaskeTests`.
- **Kern:**
  - `KaelteerzeugerfolgeTests` gegen die Folge eines Laufs;
  - `KaeltemaschineAnlageDatenbankTests` um KB-1 erweitern;
  - Stufe 5: Rückweg der Kältemaschine (neu, überschreiben, Kindzeilen, Teillast, Kosten) und Umstellen.

### 5.7 Proben

- `rollbereichprobe.mjs`: Der Fall `kaeltemaschine` verlässt die Ausnahme „außerhalb des Bausteins“ (Stufe 5).
- `fensterprobe.mjs`: die Überlagerung der Konfiguration.
- Rasterprobe: Pflicht in Stufe 5 (`Katalogliste`).
- Für die Simulationskonfiguration: ein Fall `simkonfig-kaelte` in 1 280 × 720 und 1 210 × 834 ohne Rollbereich im
  Rollbereich (KB-C).

### 5.8 Wiki-Quellen

- `Projekte/Wiki/Programm Dokumentation - Kühlung.wiki`: Zeile 18 und Abschnitt „Kühlung einschalten“ (`:157`) nennen „Weitere
  Einstellungen“, künftig „Bereich Kälte“; dazu die Abschnitte „Kältemaschine“, „Stromträger und Abrechnung“ und
  „Kühlbetrieb der Wärmepumpe“.
- `Programm Dokumentation - Simulation.wiki`, `Simulation konfigurieren und starten.wiki`: Bereich Kälte und Schema-Doppelklick.
- Logbuch-Entwurf: ein Satz je sichtbarer Änderung.

### 5.9 Rechenweg und Basis

Geprüft: KB-A bis KB-C und Stufe 5 ändern keinen Leser des Laufs. `KaelteerzeugerVorbereiten`, `KaeltemaschinenVorbereiten`,
`KaeltespeicherLesen` und `Kaeltekaskade` bleiben unberührt. Geschrieben werden dieselben Spalten über dieselben Kernwege
(`KaeltemaschineAnlageCtrl.Speichern`, `KuehlbetriebUmschalten`, `KuehlbetriebSetzen`). KB-1 ändert nur, ob eine **neue** Anlage
eine eigene Kopie bekommt; die Referenzprojekte werden nicht neu angelegt. **Die Basis R50 (bzw. R51 nach FK) bleibt.**

**Ausnahme KB-D:** Ein Schemaschritt und ein neuer Sortierschlüssel im Lauf. Bei leerem Rang bleibt alles byte-gleich, das zeigt
der Referenzlauf. Eine neue Basis braucht es erst, wenn ein Referenzprojekt einen Rang bekommt.

## 6 Fragen an den Anwender

1. **F1 Folge der Kälteerzeuger:** zunächst fest und sichtbar oder gleich pflegbar mit Pfeilen (KB-D, +1,5–2 PT, Schemaschritt)?
   **Empfehlung:** fest und sichtbar (KB-B), KB-D erst auf Wunsch. Heute rechnet die freie Kühlung zuerst, dann der
   Kältespeicher, dann die Wärmepumpe, dann die Kältemaschine — das ist für die Fälle im Bestand die wirtschaftliche Folge.
   **entschieden (E117): konfigurierbar, KB-D wird gebaut (Schemaschritt 212); Vorgabe freie Kühlung, Kältespeicher, Wärmepumpen, Kältemaschinen.**
2. **F2 Ort der Anlagenkonfiguration der Kältemaschine:** nur in der Simulationskonfiguration oder zusätzlich als Überlagerung
   „Anlage…“ im Dialog („mit Möglichkeit der Konfiguration“)? **Empfehlung:** beide Orte mit **derselben** Komponente, Hauptort
   Simulationskonfiguration — wie bei der Wärmepumpe.
   **entschieden (E117): beide Orte mit derselben Komponente.**
3. **F3 Wärmepumpen im Kühlbetrieb im Dialog:** als Lesezeile mit Verweis auf „Simulation › Kälte“ oder gar nicht? **Empfehlung:**
   Lesezeile ohne Schalter. Die Kachel heißt „Kühlung und Kälteanlagen“ und soll alle Kälteerzeuger nennen.
   **entschieden (E117): konfigurierbar an beiden Orten, nicht nur Lesezeile.**
4. **F4 Kältespeicher:** nur im Kältebereich oder auch weiter in der Speicherspalte der Wärme? **Empfehlung:** nur im
   Kältebereich, weil ein Kältespeicher keine Wärme puffert.
   **entschieden (E117): nur im Kältebereich.**
5. **F5 Doppelklick im Schema auf Kälteerzeuger:** in der Seite die Konfiguration öffnen statt in den Dialog zu springen
   (#902)? **Empfehlung:** ja, Konfiguration in der Seite. Den Katalogdialog erreicht man weiter über die Kachel.
   **entschieden (E117): Doppelklick öffnet die Konfiguration in der Seite.**
6. **F6 Zuständigkeit:** Stufe 5 baut die Sitzung „Dialoge und Korrekturen“ nach KB-B (Abschnitt 4), oder übernimmt die
   Gebäudesimulation alles? **Empfehlung:** Teilung nach Abschnitt 4.1. Das Konzept, der Baustein und das Muster 5.3/5.4 liegen
   dort.
   **entschieden (E117): Stufe 5 baut die Sitzung „Dialoge und Korrekturen“.**
   KB-1 (eigene Projektkopie je neuer Anlage): **entschieden (E117): nach Empfehlung.**

**Folgen der Entscheide.** KB-D wird gebaut (Schemaschritt 212). Die Gruppe Kühlbetrieb der Wärmepumpe erscheint im Kältebereich und im Kältedialog als konfigurierbare Zeile, nicht mehr als Lesezeile.

## 7 Wellenplan

| Welle | Sitzung | Inhalt | Aufwand | Abnahme |
|---|---|---|---|---|
| **KB-A** (gebaut) | Gebäudesimulation | `Kaelteerzeugerfolge.Lesen`, Schema liest daraus; KB-1 in `Anlegen`; `KaelteGruppe` und DTO in der Hülle; Kältespeicher aus der Wärmespalte | 1 PT | Kern-Filter grün; `dotnet test` mit `--filter "FullyQualifiedName~Kaelte"` (Kern) und `~Schema`; Schale auf Linux (`dotnet build WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Debug -p:Platform=x64 -p:EnableWindowsTargeting=true`, 0 Fehler); `SqlDialektPruefer`; Referenzlauf der sieben Projekte gegen die Basis PASS |
| **KB-B** (gebaut) | Gebäudesimulation | Bereich Kälte der Seite, Schalterumzug, Kopf „Wärme“, Kacheln mit Aufnehmen = Kühlbetrieb, Komponente `KaeltemaschineKonfiguration` (aus dem Dialog herausgelöst und dort eingebunden), Doppelklickziele, KI-Sicht und Feldkarte, Ressourcen beider Sprachen | 2–2,5 PT | bunit `--filter "FullyQualifiedName~SimulationKonfig\|FullyQualifiedName~Kaeltemaschine\|FullyQualifiedName~Schema\|FullyQualifiedName~KiSimulation\|FullyQualifiedName~KiMaskenabdeckung"`; Schale auf Linux; `designer_neu.py` ohne Befund; Dokumentationswachen |
| **KB-C** | Gebäudesimulation | Probenfall `simkonfig-kaelte` (Rollbereich, Fenstergrößen 1 280 × 800, 1 280 × 720, 1 210 × 834), `fensterprobe.mjs` für die Überlagerung, Wiki-Quellen und Logbuch-Entwurf | 0,5–0,75 PT | Proben grün mit Gegenprobe; Wiki-Gegenlese mit dem Suchmuster der `CLAUDE.md`; `WikiProduktdatenWacheTests` |
| **Stufe 5** | Dialoge und Korrekturen | Dialog auf `Zweispaltenauswahl`, Umstellen, Rückweggewerk mit Kindzeilen, Löschweg, Kopie bearbeiten, Felder heraus (nach KB-B) | 2,5–3,5 PT | nach Konzept Abschnitt 8: Kern-Tests des Rückwegs, Dialogtests, SQL-Dialekt, Rasterprobe, `fensterprobe.mjs`, `rollbereichprobe.mjs` ohne Ausnahme |
| **KB-D** (gebaut; Pfeile an der Seite: KB-D2) | Gebäudesimulation | Schemaschritt `Kaelte_Rang`, Kern sortiert, Pfeile, Arbeitsstand, Einfrierregel-Satz | 1,5–2 PT | wie KB-A, dazu Schematests, Referenzlauf byte-gleich, `Werkzeuge/Testdatenbankschema` |

**KB-A gebaut (10.10.2026, Zweig `gs-kba`):** Die Kältefolge hat eine Quelle, `Kaeltefolge` im Kern (Stufen fest;
Ordnungsregeln `ErzeugerOrdnen`, `KaeltemaschinenOrdnen`, `KaeltespeicherOrdnen`). Lauf und Schema ordnen darüber, der Leser
`Kaeltefolge.Lesen` liefert dieselbe Folge für die Anzeige — Probe gegen `Kaeltekaskade.Erzeuger` und `.Speicher` eines Laufs,
Referenzlauf gegen R51 byte-gleich. Der Leser heißt `Kaeltefolge.Lesen` statt `Kaelteerzeugerfolge.Lesen` (er liest auch
Kältespeicher und freie Kühlung). DTO `KaeltebereichDaten` in `SimulationKonfigDaten.Kaeltebereich`, Schreibweg
`SimulationKonfigDienste.KuehlbetriebWpSchreiben`, Hülle `KaeltebereichBau`. KB-1: `Anlegen` legt über
`KaeltemaschineCtrl.EigeneKopieAnlegen` je Anlage eine eigene Kopie an; die Testdatenbank führt keine geteilte Kopie. Die
Kältespeicher bleiben bis KB-B zusätzlich in der Wärmespalte (die Seite zeigt den Bereich noch nicht). Protokoll:
[`2026-10-10_KB-A_Kaeltefolge_Projektkopie.md`](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_KB-A_Kaeltefolge_Projektkopie.md).

**KB-D gebaut (10.10.2026, Zweig `gs-kbd`; Kern, Schema, Dienste; Pfeile an der Seite mit KB-D2):** Entscheid E117 F1 — die Folge der
Kälteerzeuger ist pflegbar. Schemaschritt 212 `KaelteRangSchema` legt `Tab_Energieanlagen.Kaelte_Rang` an (INTEGER ≥ 1,
NULL = Vorgabefolge); die Stufen bleiben fest (freie Kühlung vorn, Kältespeicher nach Entladepriorität). `Kaeltefolge`
ordnet Erzeuger mit Rang vorn, ohne Rang in der Vorgabefolge; Lauf, Schema und Leser ordnen darüber. Schreibweg
`KaeltefolgeCtrl` (`FolgeSetzen`, `Verschieben`, `VorgabeSetzen`, Prüfregeln). Die Pfeile schreiben **sofort** wie der
Kühlbetrieb im selben Bereich, nicht in den Arbeitsstand (Abweichung von 3.4). Dienste `KaelteVerschieben`,
`KaelteVorgabefolge`; DTO `FolgeGepflegt`, `FolgeHinweis`, je Zeile `KaelteRang`, `NachVornMoeglich`, `NachHintenMoeglich`.
Referenzprojekte ohne Rang, Basis R51 byte-gleich; der Einfrierregel-Satz bleibt offen, bis ein Referenzprojekt einen Rang
bekommt. **KB-D2:** ▲/▼ je Erzeugerkachel im Bereich „Kälte“ (nur mit Schreibweg und ab zwei Erzeugern, am
Rand ausgegraut), Knopf „Vorgabefolge“ (weich gesperrt, solange die Vorgabe gilt), Herleitungszeile aus `FolgeHinweis`.
Protokoll:
[`2026-10-10_KB-D_Kaeltefolge_pflegbar.md`](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_KB-D_Kaeltefolge_pflegbar.md).

**KB-B gebaut (10.10.2026, Zweig `gs-kbb`):** Bereich „Kälte“ der Seite mit Kopfschalter, Kälteerzeugern in Rechenfolge, Kühlbetrieb der Wärmepumpe an der Kachel, Kältespeichern nur hier und Hinweiszeile bei „aus“; Komponenten `KaeltemaschineKonfiguration` (aus dem Dialog herausgelöst, dort eingebunden) und `WaermepumpeKuehlbetriebGruppe` (aus der Wärmepumpen-Konfiguration herausgelöst); Schema-Doppelklick auf die Kältemaschine öffnet die Konfiguration in der Seite; KI-Sicht um drei Felder. Kopf „Wärme“ und der Doppelklick der Wärmepumpe im Kühlbetrieb (3.6) bleiben offen. Protokoll: [`2026-10-10_KB-B_Kaeltebereich.md`](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_KB-B_Kaeltebereich.md).

**Summe:** Gebäudesimulation 3,5–4,25 PT, mit KB-D 5–6,25 PT; Dialoge 2,5–3,5 PT. **Voraussetzung:** Welle FK (R51) ist
gemergt, damit KB-B ihre Eingaben der freien Kühlung übernimmt.
