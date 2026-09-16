# Umsetzungskonzept: Gebäudesimulation VDI 6007 in EPOS-Plan — Einbindung, Gebäudedialog, IFC-Import

**Rev. 3 — 16.09.2026 — Umsetzungsentwurf, zur Abnahme durch Philipp**

> **Rev. 3 — Entscheid E20 eingearbeitet (Trennung der Rechenwege, 16.09.2026), dazu der Zusatz
> E21 (Kältebedarf) und E19 (Nutzfläche). Rev. 2 — Korrekturen des Gegenlesens vom 15.09.2026,
> Protokoll: [Gegenlesen](Gebaeudesimulation/2026-09-15_Gegenlesen_Umsetzungskonzept.md)**

Auftrag (Anwender, 15.09.2026, im Wortlaut):

> „Prüfe die Einbindung der VDI 6007 Gebäudesimulation in EPOS-Plan. Nutze vorhandene Daten und
> Möglichkeiten. Prüfe den vorhandenen Gebäudeeditor/Dialog auf Änderungen für die Parameter und
> Eingaben der VDI 6007. Prüfe auch den Import von Daten mit einer IFC-Datei. Erstelle ein Konzept,
> wie mit einem Gebäudeimport umgegangen werden kann."

Grundlage ist das Konzept
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Rev. 1 mit Nachtrag 1) und die dort getroffenen Entscheide **E1** (Stundenmodell ist die Vorgabe
für alle Gebäude), **E2** (Bestandsgewichte im Stundenmodell gestrichen, Dialog auf U·A),
**E3** (xBIM als unverändertes NuGet-Paket), **E4** (GB als eigener Einfrierschritt vor G1),
**E5** (Klimabasis sind die vorliegenden PVGIS-TMY-Reihen), **E7/E8** (Einzonenmodell zuerst, Skalierung bleibt), **E9** (Import **und**
Export von gbXML und IFC: der gbXML-Import wird Pflicht in G4, die beiden Exporte werden Stufe G7;
Konzept N1.13) und **E10** (Druckrundung als Toleranz der Normprüfregel; Konzept N1.15).

Dazu die Entscheide vom 16.09.2026: **E19** (die Bezugsfläche heißt `Nutzflaeche`; Konzept N1.24),
**E20** (die beiden Rechenwege werden vollständig getrennt, VDI 6007 ist die Vorgabe; Konzept N1.25,
[`ADR-006`](ADR-006_Trennung_Altweg_VDI6007.md)), **E23** (der Tagesbilanz-Weg **bleibt dauerhaft**
als eingefrorener **Bestandsweg** im Produkt; die mit E20 vorgesehene Stufe **GA — Altweg
entfernen — entfällt**) und der Zusatz **E21** (die Kältebedarfsrechnung
wird der Wärmebedarfsrechnung nachgebildet; 1.1, 1.4).

Die Befunde, auf denen jede Codeaussage dieses Papiers steht:

| Befund | Gegenstand |
|---|---|
| [`Gebaeudesimulation/2026-09-15_Befund_L_Einbindung_Kern.md`](Gebaeudesimulation/2026-09-15_Befund_L_Einbindung_Kern.md) | Einbindung in den Rechenkern: Naht, Klima, Persistenz, Migration, Referenzlauf, Tests, Merge-Folge |
| [`Gebaeudesimulation/2026-09-15_Befund_M_Gebaeudedialog.md`](Gebaeudesimulation/2026-09-15_Befund_M_Gebaeudedialog.md) | Gebäudeeditor und Bedarfsdialog: Ist-Inventar, Soll-Entwurf, Hülle, Texte, bunit-Fälle |
| [`Gebaeudesimulation/2026-09-15_Befund_N_IFC-Import_Entwurf.md`](Gebaeudesimulation/2026-09-15_Befund_N_IFC-Import_Entwurf.md) | IFC-Import: Importmuster, xBIM-Paket und Lizenz, Klassen, Abbildungsregeln, Sonderfälle, Plattform |
| [`Gebaeudesimulation/2026-09-15_Befund_R_gbXML_Schema_Werkzeuge.md`](Gebaeudesimulation/2026-09-15_Befund_R_gbXML_Schema_Werkzeuge.md) | gbXML: Schema und Versionswert, was die Autorensysteme liefern, LINQ to XML statt `XmlSerializer`, Aufwand von Import und Export (E9) |
| [`Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md`](Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md) | IFC-Export ohne Geometriekernel, Rückgabe angereicherter Dateien — und die daraus folgende Auflage an **diesen** Import (3.2) |
| [`Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md`](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) | Feldzuordnung je Spalte (nur Altweg / beide / nur VDI), Aufrufstellen des Tagesbilanz-Wegs, was der Vorbereitungsschritt liefern muss, Umfang der Verschiebung (E20); der dort noch beschriebene Inhalt einer Stufe „Altweg entfernen" ist mit **E23** gegenstandslos |

Dieses Papier sagt, **was gebaut wird, in welcher Reihenfolge und woran es abgenommen ist**. Es
erfindet nichts: Jede Aussage über den Quelltext trägt Datei und Zeile aus den drei Befunden; was
dort nicht belegt ist, steht hier ausdrücklich als offen. Was das Konzept sagt und dieses Papier
korrigiert, ist je Stelle benannt (1.10).

---

## 0. Das Ergebnis in sechs Punkten

1. **Die Einbindung hat genau eine Weiche, und sie sitzt am Eingang** (E20, 16.09.2026).
   `SimulationWaermebedarf.HeizwaermeEinesGebaeudes`
   (`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:566`) ist der einzige Ort, an dem
   die Wärme eines Gebäudes entsteht; `GebaeudeBedarfCtrl.Rechnen`
   (`EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs:94`) ruft dieselben zwei Methoden (`:112`, `:117`)
   und trägt die Weiche damit automatisch mit. Vor der Weiche steht ein **modellfreier
   Vorbereitungsschritt** (Klimakalender, Bewohner aus der Nutzfläche, `VerbrauchNeu`, die beiden
   Flächen der Skalierung nach E8); hinter ihr stehen **zwei getrennte Module**:
   `Simulation/Altweg/` (die Tagesbilanz, Zeichen für Zeichen verschoben, ohne neue Funktion) und
   `Simulation/Gebaeude/` (VDI 6007), und keines ruft das andere. Die
   Verbrauchs-Rückrechnung ruft **dasselbe** Modul ein zweites Mal, statt an einer zweiten Stelle
   zu verzweigen — damit ist der frühere zweite Verzweigungspunkt gegenstandslos (A16). Der Puffer
   (`:188`, Watt) und die eine Umrechnung nach kW (`:222`) bleiben unberührt. Der Altweg ist ein
   **eingefrorener Bestandsweg** (E23, 16.09.2026): Er bleibt **dauerhaft** neben dem VDI-Weg
   stehen, samt Weiche, Schalter und Spalten, und bekommt keine neue Funktion.

2. **Zwei stille Fallen entscheiden über die Reihenfolge der Merges.** Das Modellfeld
   `Fensterflaeche_Ost` trägt heute die Spalte `Fensterflaeche_Ost_West`
   (`EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:56`, `GebaeudeCtrl.cs:66`); wer die neue Spalte
   `Fensterflaeche_Ost` anlegt, bevor das Feld auf `Fensterflaeche_OstWest` umbenannt ist, verliert
   die Ost-/Westfenster des Tagesmodells ohne jede Meldung. Und
   `GebaeudeStammCtrl.CopyFromStamm` (`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:439`) bildet jedes
   `DBNull` auf `0.0` bzw. `""` ab — das zerstört „NULL = Vorgabe" für die elf nullbaren der zwölf
   neuen Spalten. Beides
   ist behebbar, aber nur in dieser Reihenfolge: **Umbenennung vor Schema, Schema vor Modell.**

3. **Der Gebäudedialog ist fünf Masken, und der Editor, den E2 trifft, ist nicht der, den das
   Konzept nennt.** `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor` (983 Z.) führt alle Größen
   der U·A-Tabelle bereits — verteilt auf vier Gruppen und zwei Reiter; `GebaeudeDialog.razor`
   (828 Z.) ist reiner Wirt ohne ein einziges Fachfeld. Der Umbau ist deshalb eine **Umordnung mit
   zwei neuen Feldern** (Fenster Ost, Fenster West) und einer neuen Gruppe „Modellparameter
   (VDI 6007)" — keine
   neue Maske. Nach E20 folgen **alle** Gebäudemasken der VDI-6007-Struktur; die Felder, die nur
   der Altweg liest, stehen in einem eingeklappten Abschnitt **„Tagesbilanz (Bestandsweg)"** — nach
   [Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) sind das
   **vier Spalten**, davon zwei sichtbare. Gemessener Aufwand: **9,0 PT** allein für die
   Oberfläche (8,0 PT Umbau + 1,0 PT Bestandswegabschnitt), mehr als die Hälfte des im Konzept für
   ganz G1 veranschlagten Rahmens.

4. **Der IFC-Import fügt sich ohne Architekturbruch ein, aber das Paket ist enger zu fassen als
   E3 es beschreibt.** `Xbim.Essentials` zieht `Xbim.Ifc` und damit `Xbim.IO.Esent` (Windows,
   ManagedEsent) nach; in den Kern gehört **allein `Xbim.IO.MemoryModel` 6.1.605**, das
   `Xbim.Common`, `Xbim.Ifc2x3`, `Xbim.Ifc4` und `Xbim.Ifc4x3` von selbst mitbringt. Ein Leser
   bedient alle drei Schemata über `Xbim.Ifc4.Interfaces.IIfc*`; `IfcStore` wird nie gerufen.

5. **Drei Dinge fehlen im Bestand und müssen mitgebaut werden, sonst ist die Stufe nicht
   abnehmbar:** eine Lizenzhinweisseite im Installationspaket (CDDL § 3.1 verlangt den
   Quellenverweis an den Empfänger; `Setup/EPOS-Plan.iss:164`, `:330` kennt nur `Lizenz.rtf`), ein
   Größenlimit für Importdateien (im ganzen Importbestand gibt es keins) und eine Vorrichtung für
   **nicht ausgelieferte** Normprüfdaten — mit der Folge, dass der Nachweis der zwölf Normtestfälle
   ein **lokaler** ist, kein CI-Nachweis.

6. **Reihenfolge:** G0 (Löser, keine Wirkung) → GB (Bestandsbefunde) → M2 Umbenennung → M3 Schema
   → M4 Klimaspalten → G1 + G2 gemeinsam → G3 → G4 → G5. **Eine Stufe „Altweg entfernen" gibt es
   nicht** (E23, 16.09.2026): Der Tagesbilanz-Weg bleibt dauerhaft als eingefrorener Bestandsweg
   neben dem VDI-Weg stehen; die Konzeptfrage Q24 ist damit beantwortet (nie), Q25 gegenstandslos.
   **G1 beginnt mit der Verschiebung des Altwegs** in sein Modul und einem **byte-gleichen**
   Referenzlauf — erst danach wird der VDI-Weg angebunden. **G4 trägt nach E9 zwei Importe:** den
   IFC-Import (G4a/G4b, Kapitel 3) und den **gbXML-Import als Pflichtteil G4c**; die beiden
   Exporte nach gbXML und IFC sind Stufe **G7** und Gegenstand des
   [Datenaustauschkonzepts](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md). G5 bleibt die
   Geometrieableitung, nur bei Bedarf aus der Praxis. **Die Basis wird in diesem Papier zweimal neu
   eingefroren: mit GB und mit G1 + G2**; der dritte Anlass der Einfrierkette, **G6d**, gehört zum
   [Mehrzonenmodell](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md). Alles dazwischen muss
   `GESAMT: PASS` melden — sonst ist hinterher nicht
   mehr zu sagen, ob eine Abweichung vom Schema oder vom Modell kommt.

---

## 1. Einbindung in den Kern

### 1.1 Der Weg durch den Bestand und die eine Verzweigung

`SimulationWaermebedarf.Waermebedarf_berechnen(int ID_Projekt, int ID_Klimaregion)`
(`SimulationWaermebedarf.cs:128`) ist der Einstieg; gerufen wird er aus
`SimulationRunner.Simuliere_Intern` (`EPOS.Kern/Allgemein/Simulation/SimulationRunner.cs:184`) und
aus `SimulationLaufCtrl.Bedarf` (`EPOS.Kern/Controller/SimulationLaufCtrl.cs:114-120`). Die
Klimaregion kommt aus `ProjektCtrl.ReadSingle`, eine 0 bricht benannt ab
(`SimulationRunner.cs:163-170`); davor steht die Schemasperre `SchemaStand.SimulationGesperrt`
(`SimulationRunner.cs:137`).

Die Gebäudeschleife und ihre Einheiten:

| Zeile | Was geschieht | Einheit |
|---|---|---|
| `:175`, `:177-178` | `KlimakalenderLesen(ID_Klimaregion)`, `ProjektGebaeudeCtrl.ReadAll(ID_Projekt)` | — |
| `:188` | `double[] Waermebedarf_EinGebaeude = new double[8760]` — **ein** Puffer je Durchlauf | **W** |
| `:197` | `HeizwaermeEinesGebaeudes(ctrl.items[i], i, Waermebedarf_EinGebaeude)`; `false` bricht den Lauf ab | — |
| `:201`, `:204`, `:208` | Addition in `kanalHeizung`, in die unabhängige Energieprobe (`:172`) und in `Waermebedarf_Gebaeude` | W |
| `:212` | `MaxP[i] = Maximaler_Waermebedarf(…)` | W |
| `:222` | `BhkwPlan.WattToKw(kanalHeizung)` — **die eine Umrechnung** | kW |
| `:228`, `:321` | `Waermebedarf_Gebaeude_Gesamt = kanalHeizung.Sum() / 1000`, `BhkwPlan.MonatsSumme(…)` | MWh |
| `:401` | `Waermebedarf_Max` — das `Waermelast_Max` des Projekts | kW |

`HeizwaermeEinesGebaeudes` selbst (`:566`) läuft heute in fünf Schritten: Einheitenzweig
(`:569-576`), **Tagesmodell `Berechnung_Gebaeude_Tageswerte(item, index)` (`:581`)**,
Tagesverteilung lesen (`:584`), Abbruch bei fehlender Verteilung (`:590-595`), `VectorInit(ziel)`
und `BhkwPlan.StdWerte(…)` (`:598-608`). Genau dieser Rumpf wird aufgeteilt.

**Die Weiche sitzt am Eingang** (E20, 16.09.2026). `HeizwaermeEinesGebaeudes` wird zur **Fassade**
und schrumpft auf rund zwanzig Zeilen:

1. **Der modellfreie Vorbereitungsschritt** liefert, was beide Wege brauchen — den
   **Klimakalender** (`Sol_*`, `A_Temp`, `WE`, `TagTyp_W/NW`, `Stundentemperatur`, `WochentagJan1`;
   heute `KlimakalenderLesen:513-537` und `Stundentemperatur_aus_DB:912-920`, Anweisung für
   Anweisung), die **Bewohnerzahl aus der Nutzfläche** (`:571`, `:641`, `:653`), **`VerbrauchNeu`
   je Einheit** (`:617-636`, reine Einheitenumrechnung) und die **beiden Flächen der Skalierung
   nach E8** — `Z_AuswahlWohnflaeche` und `Nutzflaeche`. Den Faktor selbst wendet er **nicht** an:
   im Altweg steckt er im Rückgabewert der Tagesrechnung (`BhkwPlan.cs:435`), im VDI-Weg ist er
   eine Nachmultiplikation (1.5).
2. **Die Weiche** liest den Rechenweg des Gebäudes (`Tab_Gebaeude.Gebaeude_Modell`; NULL = VDI 6007
   nach E1) und ruft **genau ein Modul**.
3. **Zwei getrennte Module.** `EPOS.Kern/Allgemein/Simulation/Altweg/` trägt den Tagesbilanz-Weg —
   Zeichen für Zeichen verschoben, ohne neue Funktion —, `Simulation/Gebaeude/` den VDI-Weg
   (1.4). **Der VDI-Weg ruft nichts aus dem Altweg**, und ein Wächter hält das fest (1.9).

**Weiche und Altwegmodul bleiben dauerhaft** (E23, 16.09.2026). Der Tagesbilanz-Weg ist kein
Zwischenzustand, sondern der **eingefrorene Bestandsweg** des Produkts: VDI 6007 ist die Vorgabe
(E1), die Tagesbilanz bleibt ausdrücklich wählbar. Damit sind Fassade, Vorbereitungsschritt,
`IGebaeudeRechenweg` und die Trennungswache Dauereinrichtungen, kein Provisorium auf Zeit.

**Die Verbrauchs-Rückrechnung ist kein zweiter Verzweigungspunkt mehr.** Sie braucht
`VerbrauchAlt` aus **demselben** Modell, das anschließend den Bedarf rechnet
(heute `Bewohner_und_Flaeche_berechnen:613` → `:647` → `:650`). Die Fassade ruft dafür **dasselbe
Modul ein zweites Mal** — in der Reihenfolge des Bestands (`:645` → Lauf 1 → `:652` → Lauf 2) —
statt an einer zweiten Stelle zu verzweigen. Damit ist Architekturfrage **A16** (Zuschnitt des
zweiten Verzweigungspunkts) gegenstandslos; die Verhältnisrechnung kann zwei Modelle nicht mehr
mischen, weil es nur noch eine Stelle gibt, an der das Modell gewählt wird.

Alles nach der Weiche (`:201-212`, Kanal, Summen, Dauerlinie, Energieprobe) bleibt Zeichen für
Zeichen — genau wie Konzept 4.1 es zusagt. Weil `GebaeudeBedarfCtrl.Rechnen`
(`GebaeudeBedarfCtrl.cs:94`) dieselben zwei Methoden ruft (`:112` `KlimakalenderLesen`, `:117`
`HeizwaermeEinesGebaeudes`, danach `:121` `WattToKw`, `:126-133` Summe/Höchstwert/Monate), trägt
die eine Weiche den Bedarfsdialog automatisch mit. Das ist die Hausregel „Eine Auskunft ruft den
Rechenweg des Laufs — sie schreibt ihn nicht ab"
([`EPOS.Kern/CLAUDE.md`](../../EPOS.Kern/CLAUDE.md)). Die drei öffentlichen Signaturen
(`Waermebedarf_berechnen`, `KlimakalenderLesen`, `HeizwaermeEinesGebaeudes`) bleiben deshalb
unverändert.

**Was verschoben wird, und was es kostet.** Nach
[Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) wandern drei
Methoden aus `SimulationWaermebedarf.cs` (`Berechnung_Gebaeude_Tageswerte:684-888`,
`DBTagesVeteilung:658-682`, der `StdWerte`-Zweig `:597-608`, rund 250 Zeilen) und **vier
Physikfunktionen** aus `BhkwPlan.cs` (`StdWerte:259`, `SolareGewinneC:311`,
`SpezWaermeverlusteC:342`, `TaeglHeizlastWG:364` samt der statischen Vortemperatur `:51`/`:54`,
rund 185 Zeilen). **`BhkwPlan.cs` wandert nicht als Ganzes:** dieselbe Datei trägt die neun
Vektorhelfer, die jeder Bedarfs- und Erzeugerzweig ruft. Aufwand der Trennung: **3–5 PT**
zusätzlich in G1, abgenommen an einem **byte-gleichen** Referenzlauf (1.8, Kapitel 4).

**Die Kältebedarfsrechnung folgt derselben Bauform** (E21, 16.09.2026). Neben
`SimulationWaermebedarf` steht eine Fassade **`SimulationKaeltebedarf`**, die **denselben**
Vorbereitungsschritt liest und die Kühllast je Stunde aus **demselben Lauf** des VDI-Moduls
`Gebaeude/` entgegennimmt (`GebaeudeModellErgebnis.KuehlbedarfKwh`, 1.4) — **keine zweite
Gebäuderechnung**. Einen Altweg gibt es auf der Kälteseite nicht: Ein Gebäude auf dem
Tagesbilanz-Weg liefert Kältebedarf **0** mit benanntem Hinweis, weil die Tagesbilanz keine
Raumtemperatur führt. Kanal, Senken, Erzeuger und Dialoge der Kälteseite stehen im
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) (Stufe KU1), nicht hier.

**Zwei Bestandsgrenzen fallen dabei auf.** `HeizwaermebedarfGeb` (`:31`) und `MaxP` (`:56`) sind
`double[100]`; ein Projekt mit mehr als 100 Gebäuden wirft eine `IndexOutOfRangeException` an
`:816`. `MaxP` wird an `:212` geschrieben und **nirgends gelesen** — toter Bestand; dasselbe gilt
für `Anzahl_Bewohner` (`:11`, gesetzt `:577`) und `Wohnflaeche` (`:12`, gesetzt `:578`), die im
ganzen Bestand keinen Leser haben (Befund X 2.3). Die drei Spitzenkennzahlen des Stundenmodells
(Konzept 4.5) gehören deshalb nicht dorthin, sondern in ein eigenes, benanntes Ergebnisobjekt
(1.4, `GebaeudeModellErgebnis`). Frage U9.

### 1.2 Der Datenfluss vom Klima bis zum Kanal

| # | Stufe | Fundstelle | Was das Stundenmodell davon braucht |
|---|---|---|---|
| 1 | `KlimakalenderLesen(int)` füllt sieben Tagesreihen `Sol_N/_O/_S/_w`, `A_Temp`, `WE`, `TagTyp_W`, `TagTyp_NW` | `SimulationWaermebedarf.cs:513`, `:519-526` | **nur `WE[365]`** (`:524`) als Wochenendmaske; die Tagesverteilung entfällt |
| 2 | `Stundentemperatur_aus_DB(int)` ruft `ReadOrtszeit` und nimmt **nur** `Außen_Temp` | `:912`, `:915`, `:919` | die ganze Zeilenliste ist zu behalten statt zu verwerfen (`:914-920`) — ein Feld `_solarOrtszeit` |
| 3 | `SolardatenCtrl.ReadOrtszeit(idKlimaregion, idProjekt)` liest 8 760 Zeilen, merkt die UTC-Herkunft je Zeile und sortiert auf Ortszeit um | `EPOS.Kern/Controller/SolardatenCtrl.cs:156`, `:160-162`, `:174-175`, `:181`, `:195-199` | `Außen_Temp`, `Globalstrahlung`, `Direktstrahlung`, `Diffusstrahlung`, `TagUtc`, `StundeUtc` |
| 4 | `SolarZeitbasis` — datenbankfrei, EU-Regel fest verdrahtet, kein `TimeZoneInfo` | `EPOS.Kern/Allgemein/Simulation/SolarZeitbasis.cs:36`, `:135` | nichts Neues: Sonnenstand auf UTC, Bilanz auf Ortszeit — wie `SimulationPV` und `SimulationSolarthermie` |
| 5 | `SolarCalculator.CalculateHourlyHayDavies(…)` — anisotrop, **ohne statische Seitenwirkung** | `EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:455`, Kopf `:446-452` | **die Funktion je Fensterorientierung**; `Tilt = 90`, `Azimuth ∈ {0, −90, 180, 90}` |
| 6 | Azimutkonvention: Grad gegen **Süd**, Ost −90, Nord 180, West 90; Fassadenneigung 90 | `EPOS.Kern/Allgemein/Import/KlimaImportAblauf.cs:135`, `:132` | die Konvention des Eingangsbauers |
| 7 | Erdreichtemperatur nach Kusuda — **liegt fertig im Kern** | `EPOS.Kern/Allgemein/Simulation/ErdreichTemperatur.cs:411`, Vorgabeboden `:47` (`BODENTYP_DEFAULT`), Katalogzeile `:188` | Dämpfung 0,69 und Phasenverzug 21,4 Tage, also die Konzeptzahlen 0,68/22 auf zwei Stellen. **Kein neuer Kusuda-Code** |
| 8 | `Zonenmodell2K` rechnet 8 760 Blockstunden, liefert `HeizlastW[8760]` | neu (1.3) | schreibt in den vorhandenen Puffer `ziel[]` in Watt |

**Was nicht benutzt werden darf:** `CalculateHourly` (`SolarPVGISCalculator.cs:389`) — die isotrope
Bestandsfunktion schreibt drei statische Felder (`:302-304`, gesetzt `:395`, `:399`, `:401`), die
`SimulationSolarthermie` unmittelbar nach dem Aufruf liest. Das ist prozessweiter Zustand und
verstößt gegen „Zustand je Instanz, nichts Statisches" (Konzept 4.1).

**Der Vergleich in 8.2 trägt deshalb zwei Anteile, nicht einen.** Der Bestandsweg nimmt `Sol_*` aus
`Tab_Klimadaten` — gerechnet mit der **isotropen** `CalculateHourly` beim Klimaimport
(`KlimaImportAblauf.cs:318-322`, Leser `SimulationWaermebedarf.cs:513-526`) —, das Stundenmodell
rechnet anisotrop nach Hay-Davies. Wer die Differenz der Jahressummen allein dem Modellwechsel
zuschreibt, schreibt den Himmelsmodellwechsel mit hinein. Die Aufteilung wird in G1 **einmal
gemessen** (ein Lauf mit `CalculateHourlyHayDavies` gegen einen mit den `Sol_*`-Reihen) und im
Protokoll ausgewiesen.

**Ein dritter Nachweis gehört in G1** (E5 / Konzept N1.10): Blatt 3 (Gl. 29–50, Aydinli/Krochmann)
wird **neben** Hay-Davies implementiert und je Orientierung und Neigung auf den dreizehn
Klimaregionen gegengehalten; berichtet werden Jahressumme, Stundenabweichung und der Anteil
innerhalb ± 5 W/m². Das trägt Q20.

**Zwei Zeitfragen bleiben offen und gehören in G1 gemessen, nicht geraten:**

- **Stundenanfang oder Stundenmitte.** `KlimaImportAblauf.Rechnen` übergibt `dt.Hour` aus der
  TMY-Zeitmarke (`KlimaImportAblauf.cs:318-322`), PVGIS liefert `20200101:0000`, `:0100`, … — der
  Zeitbezug ist also der **Stundenanfang**. Konzept N1.10 verlangt für Blatt 3 die Stundenmitte;
  der Unterschied ist eine halbe Stunde Stundenwinkel = 7,5° und verschiebt genau die Ost- und
  Westflächen, deren Trennung G1 neu einführt. Entschieden wird an **einer** Stelle, im
  Eingangsbauer; `Tab_Solar.Sol_*` bleibt unberührt (Referenzbasis). Frage U6.
- **Wochenendkalender.** Konzept 4.4 leitet den Wochentag des 1. Januar aus
  `SolardatenCtrl.Referenzjahr(idProjekt)` (`SolardatenCtrl.cs:222`) ab. Das Referenzjahr kommt aus
  der aktiven Spotpreisreihe (`:228-232`, sonst `DbWerte.SOLAR_REFERENZJAHR_STANDARD`, `:242`) und
  entscheidet heute **ausschließlich** über die zwei Sommerzeit-Umstelltage. `Tab_Klimadaten.WE`
  dagegen entsteht im Import aus `datum.DayOfWeek` der TMY-Zeitmarke
  (`KlimaImportAblauf.cs:354`) — also aus dem PVGIS-Jahr 2020. Der in Konzept 4.4 verlangte Test
  (Maske gegen `Tab_Klimadaten.WE`) ist damit **nicht führbar**, sobald ein Projekt eine Preisreihe
  ≠ 2020 führt. Das Stundenmodell nimmt deshalb `WE[365]` aus `KlimakalenderLesen:524` — dieselbe
  Quelle wie der Bestand; dann rechnen beide Wege denselben Kalender und der Vergleich im
  Bedarfsdialog (Konzept 8.2) bleibt gültig. Frage U7, Konzeptkorrektur 1.9.

### 1.3 Stufe G0 — der Löser im Kern, ohne jede Wirkung

Ort: `EPOS.Kern/Allgemein/Simulation/Gebaeude/`. Alle Dateien ohne `DataRepository`, ohne
`SimulationProtokoll`, ohne Statik, durchgehend `double` (Vorbild
`EPOS.Kern/Allgemein/Simulation/PvErweitertesModell.cs`).

**`ErsatzparameterRC.cs`** — ein `record` mit den reduzierten RC-Größen eines Gebäudes: die beiden
Kapazitäten `C_AW_Jk`/`C_IW_Jk` [J/K], die Widerstände `R_Rest_AW_KW`, `R_1_AW_KW`, `R_1_IW_KW`,
`R_conv_AW_KW`, `R_conv_IW_KW`, `R_rad_KW` und `R_ext_KW` (= H_ve + Σψ·L) [K/W], die
Bezugsflächen `A_AW_opak_M2`/`A_IW_M2` und `SummeUA_opak_WK` für θ_eq; dazu
`static ErsatzparameterRC AusKlassenweg(GebaeudeModellEingang e)`. Der Erbauer trägt die harten
Prüfungen aus Konzept 4.8 — `R_Rest_AW > 0` mit benanntem Fehler (kein stiller Rückfall),
`5 ≤ Bauweise/Wohnflaeche ≤ 200 Wh/(m²K)`, U-Werte 0,1…6 W/(m²K), `0 < g ≤ 1`.

**E14 (16.09.2026): die Fenster liegen im AW-Zweig, nicht im Lüftungszweig.** Der Satz führt
dafür `R_1_AF_KW` (= R_AF/6, nach den Wänden parallel geschaltet, Gl. (25)–(28)) und
`R_Rest_AF_KW`; `R_ext_KW` trägt allein Lüftung und Wärmebrücken. Der Fensterpfad des Prototyps
entfällt damit für das Produkt (Konzept N1.19, Rechenschritte A7a).

**`Zonenmodell2K.cs`** — der Löser. **Der Name folgt der Norm:** die Richtlinie sagt „2-K-Modell",
„7R2C" ist nur Kurzform (Konzept N1.3); Konzept 4.1 und 11 nennen noch `Zonenmodell7R2C` und sind
an dieser Stelle zu korrigieren (1.10).

```csharp
internal sealed class Zonenmodell2K
{
    internal Zonenmodell2K(ErsatzparameterRC p);   // baut A, b-Struktur, Phi, Gamma, Psi einmal
    internal double[] Eigenwerte { get; }          // beide reell und negativ (Probe Konzept 10.2)
    internal void Zuruecksetzen(double thetaStart);
    internal Stundenergebnis Schritt(in Stundenrand r);  // eine Blockstunde
}
```

`Stundenrand` (readonly struct) trägt `ThetaOut`, `ThetaEq`, `ThetaSoll`, `ThetaMax`, die drei
Lasten `PhiRadAW`/`PhiRadIW`/`PhiConv` [W], `HeizleistungMaxW` (`NaN` = unbegrenzt) und
`HeizungStrahlungsanteil`; `Stundenergebnis` trägt `HeizleistungW` und `KuehlleistungW` (Blockmittel,
beide ≥ 0), `ThetaAirMittel`, `ThetaOpMittel` sowie die beiden Endzustände `ThetaMAwEnde`,
`ThetaMIwEnde`. Zustand je Instanz sind zwei `double` — mehr nicht. `Zuruecksetzen` ist keine
Bequemlichkeit, sondern Bedingung: Verbrauchsgebäude werden **zweimal** gerechnet (1.5).

**`Bauteilreduktion.cs`** (Kettenmatrix, `System.Numerics.Complex`) ist hier nur als leerer Platz
vorgemerkt; sie kommt mit G3.

**Die Prüfgröße ist das Blockmittel der Stunde**, nicht der Momentanwert und nicht ein gleitendes
Mittel (Konzept N1.2); die Prüfregel ist nach **E10** das Band zwischen den beiden Programmspalten
**± 0,15 K bzw. ± 1,5 W** — Toleranz der Richtlinie plus eine halbe Druckstelle, weil die Tabellen
gerundet gedruckt sind (Konzept N1.15) —, Heizlast **positiv**. Offen aus dem Konzept und in G0 zu
lösen: Testfall 11
(Kühldecke, α_kon 5,0 je Bauteil), Testfall 6 (Vorzeichen), α_kon je Bauteil statt global 2,7,
die Grenze `E = 0 ab Z > 170` aus Blatt 1, 6.8.

**G0 ändert keine einzige Zeile des Bestandswegs.** Es gibt keinen Aufrufer; der Referenzlauf kann
sich nicht bewegen. Abnahme: Kern-Filter grün; **elf der zwölf Normtestfälle im Band nach E10**,
Testfall 11 in zwei Umschaltstunden um 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung), bis der eigene Knoten der Kühldecke ihn löst
— mit der Einschränkung aus 1.8.

### 1.4 Stufe G1 — die vier neuen Kernklassen

**`GebaeudeModellEingang.cs`** baut aus `ProjektGebaeudeModel` und den Klimareihen die 8 760
Randbedingungen:

```csharp
internal static GebaeudeModellEingang Bauen(
    ProjektGebaeudeModel gebaeude,
    IReadOnlyList<SolardatenModel> solarOrtszeit,  // aus ReadOrtszeit, 8760 Zeilen
    bool[] wochenende,                             // WE[365] aus KlimakalenderLesen:524
    double laengengrad, double breitengrad);
```

Ergebnis sind acht Reihen zu 8 760 Werten — `ThetaOut`, `ThetaEq` (U·A-gewichtet), `PhiSolarAW`,
`PhiSolarIW`, `PhiSolarLuft` (der a_kon-Anteil), `PhiIntern`, `ThetaSoll`, `ThetaMax` — und die
`ErsatzparameterRC`.

Hier — und nur hier — fällt die Entscheidung über Stundenanfang/Stundenmitte (1.2), hier läuft die
Erdreichtemperatur über
`ErdreichTemperatur.JahresprofilKollektor(thetaOut, 1.0, DbWerte.BODENTYP_SAND_FEUCHT)`
(`ErdreichTemperatur.cs:411`), und hier steht die Azimutzuordnung der vier Fensterrichtungen.

**Wichtig für die Lesereihenfolge:** `item.Bewohner` und `item.Z_AuswahlWohnflaeche` sind
**Ausgabefelder** (`SimulationWaermebedarf.cs:571`, `:641`, `:645`, `:652`, `:653`, Doku-Kopf
`:558-560`). Seit E20 füllt sie der **modellfreie Vorbereitungsschritt** vor der Weiche (1.1); der
Eingangsbauer liest also **nach** ihm, nicht davor. Die Klimareihen bekommt er ebenfalls von dort —
er liest `Tab_Klimadaten` und `Tab_Solar` nicht selbst, und er ruft **nichts** aus `Altweg/`.

**Die Kälteseite hängt an derselben Klasse** (E21, 16.09.2026). `GebaeudeModellErgebnis`
(unten) führt `KuehlbedarfKwh` bereits je Stunde; die Fassade **`SimulationKaeltebedarf`** nimmt
diese Reihe aus **demselben** Lauf entgegen, den die Wärmeseite auslöst — der Löser wird kein
zweites Mal gerufen, und der Vorbereitungsschritt wird kein zweites Mal gelesen. Ein Gebäude auf
dem Altweg liefert Kältebedarf **0** mit benanntem Hinweis (kein Altweg auf der Kälteseite).
Kanal, Senken und Erzeuger regelt das
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) in Stufe KU1.

**`GebaeudeModellErgebnis.cs`** — vier Reihen (`HeizlastW[8760]` in Watt, weil sie in den
vorhandenen Watt-Puffer `ziel[]` geschrieben wird und den Kern nicht verlässt; `Raumtemperatur`,
`OperativeTemperatur`, `KuehlbedarfKwh` — Zeitreihen führen kWh, Einheitenregel 1) und sieben
Kennzahlen (`JahresheizwaermeMwh`, `SpitzeKw`, `SpitzeTagesmittelKw` — Mittel über 24
**Blockstunden** —, `Spitze95Kw`, `KuehlenergieMwh`, `StundenMitKuehlbedarf`,
`MittlereRaumtemperaturHeizzeit`). **Umgerechnet wird im Kern** (`GebaeudeBedarfCtrl` bzw.
`SimulationErgebnisCtrl`), nie in der Hülle — sonst reißt
`In_Anzeige_und_Huellen_steht_kein_Faktor_1000_auf_einer_Energiemenge`; und eine
`…Kwh`-Jahressumme, die den Kern verlässt, verstößt gegen Einheitenregel 2
([`EPOS.Kern/CLAUDE.md`](../../EPOS.Kern/CLAUDE.md)).

Die **Einheit steht im Namen** — sonst reißt der Namenswächter, sobald die Klasse in
`EinheitenWacheTests.Simulationsklassen` (`EPOS.Kern.Tests/EinheitenWacheTests.cs:201-206`)
aufgenommen ist. Sie **muss** dort aufgenommen werden: die Liste ist fest, eine neue Datei unter
`Simulation/Gebaeude/` sieht der Wächter sonst nicht (1.9). Der Eintrag lautet
`Gebaeude/GebaeudeModellErgebnis.cs` — **mit Unterordner**: `Simulationsdateien()` (`:447-455`)
setzt den Pfad aus `Simulation` + Listeneintrag zusammen und prüft ihn mit
`Assert.True(File.Exists(...))`; ein Eintrag ohne Unterordner macht den Wächter rot.

**Zwei Kernänderungen gehören in denselben Merge.** Erstens: `GebaeudeBedarfCtrl.Rechnen`
(`GebaeudeBedarfCtrl.cs:94`) bekommt einen vierten Parameter `string? modellErzwungen = null`
(null = der Spaltenwert) und `GebaeudeBedarfErgebnis` die sechs neuen Kennzahlen — nur so ist der
Vergleich zweier Wege im Bedarfsdialog (2.7) überhaupt zu holen. Der Parameter wirkt allein auf der
**gelesenen Modellinstanz**, schreibt nichts und ruft dieselbe Weiche wie der Lauf (Hausregel
„Eine Auskunft ruft den Rechenweg des Laufs", `EPOS.Kern/CLAUDE.md:186`); er **bleibt dauerhaft**,
weil der Vergleich alt/neu dauerhaft bleibt (E23, 16.09.2026; 2.7). Zweitens:
`GebaeudeModellEingang` bekommt einen **`Pruefmodus`**-Schalter (UTC-Reihenfolge, isotrope `Sol_*`,
θ_eq ohne Absorptionsterm); ein Test hält den Kern damit gegen die Prototypzahlen aus Konzept 5 auf
1e-6 relativ, und der Unterschied zum Auslieferungsweg wird je Referenzprojekt ausgewiesen
(Konzept 10.4 (2)).

`SimulationWaermebedarf` bekommt ein Feld `List<GebaeudeModellErgebnis> GebaeudeErgebnisse` —
**an der Stelle, an der `MaxP` (`:56`) steht**, das tot ist.

### 1.5 Stufe G1 — die Verzweigung, der Vorlauf und die Skalierung

**Die Weiche** in der Fassade `HeizwaermeEinesGebaeudes` (E20, 16.09.2026) — eine Stelle, zwei
Module, kein Modellparameter in einer Bestandsmethode:

```
IGebaeudeRechenweg weg = (Modell(item) == DbWerte.GEBAEUDE_MODELL_TAGESBILANZ)
                       ? _altweg      // Altweg/TagesbilanzRechenweg   - Uebergang, ohne neue Funktion
                       : _vdi6007;    // Gebaeude/Vdi6007Rechenweg     - NULL faellt hierher (E1)
return weg.Rechnen(item, index, ziel, kalender);
```

Der Vorbereitungsschritt läuft **davor** und ist modellfrei (1.1); die Verbrauchs-Rückrechnung ruft
`weg` ein zweites Mal, statt an einer zweiten Stelle zu verzweigen. Der Rückgabewert bleibt `bool`
— nur der Altweg gibt `false` zurück (fehlende Tagesverteilung, `:590-595`); die Reihen und
Kennzahlen reisen über `GebaeudeErgebnisse`. **Der Tagesbilanz-Zweig bekommt dabei keine Zeile
neuen Codes:** die Bestandsmethoden wandern Zeichen für Zeichen nach `Altweg/`, und **kein
Parameter „Modellwahl" erreicht sie** — damit ist A16 gegenstandslos.

**Der Vorlauf.** Der Bestand rechnet **15 Tage** (`Tag = 350…364`, `:748-814`, Ergebnis verworfen).
Konzept 4.6 setzt für das Stundenmodell **30 Tage** — die gemessenen Zeitkonstanten der
Referenzgebäude liegen bei 7,5–24,8 h, die langsameren Normtesträume bei bis zu 264 h; der Nachweis
„Vorlauf konvergiert auf unter 0,1 K" gehört als Rechenprobe in G0. Der Bestandsvorlauf bleibt bei
15 Tagen, weil daran die Basis hängt.

**Die Skalierung (E8) bleibt in der Sache, wird aber im VDI-Modul nachgebaut.** Die Rechnung läuft
mit den Katalogdaten des Gebäudes; im Altweg steckt der Faktor im Rückgabewert von
`BhkwPlan.TaeglHeizlastWG` (`BhkwPlan.cs:435`: `return acc * gesamtflaeche / wohnflaeche;`), der
`Z_AuswahlWohnflaeche` und `Nutzflaeche` als 14. und 15. Argument bekommt
(`SimulationWaermebedarf.cs:812-813`) — **keine Nachmultiplikation des Ergebnisvektors, sondern
Teil der Tagesrechnung**. **Das VDI-Modul führt die Verhältnisrechnung selbst**, als
Nachmultiplikation in seinem eigenen Ergebnisschritt
([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Schritt G/8.3); es ruft
dafür **nichts** aus `Altweg/`, und der Vorbereitungsschritt liefert ihm nur die **beiden Flächen**,
nicht den fertigen Faktor — sonst würde der Altweg zweimal skaliert (1.1).

Die Verbrauchs-Rückrechnung ist eine Verhältnisrechnung: Der Vorbereitungsschritt bildet
`VerbrauchNeu` je Einheit (heute `:617-636`, Ergebnis in kWh, modellfrei) und setzt
`Z_AuswahlWohnflaeche = Nutzflaeche des Katalogbaus`; die Fassade ruft **das gewählte Modul ein
erstes Mal** und nimmt `VerbrauchAlt` aus dessen Jahreswert (heute
`HeizwaermebedarfGeb[index] / 1000`, `:650`), rechnet `FlaecheNeu = VerbrauchNeu / VerbrauchAlt ×
FlaecheAlt` (`:651`) und ruft dasselbe Modul ein zweites Mal.

Daraus zwei harte Folgen:

1. **Der Löser muss zustandsfrei zweimal hintereinander laufen dürfen** — einmal für
   `VerbrauchAlt`, einmal für die Reihe. Weil beide Aufrufe **dasselbe Modul** treffen, kann
   `VerbrauchAlt` nicht mehr aus dem einen und der Bedarf aus dem anderen Weg stammen (1.1). Bei
   rund 5 ms je Lauf (Konzept 4.8/5.13) ist das belanglos, aber `Zonenmodell2K.Zuruecksetzen` ist
   deshalb Pflicht und die Rechenprobe „zwei Läufe byte-gleich" die Gegenprobe; ein Datenbankfall
   hält beide Zahlen gegeneinander. **Im Altweg gilt dasselbe für die statische Vortemperatur**
   `BhkwPlan._prevRoomTemp` (`:51`), die über beide Aufrufe **und alle Gebäude** trägt — die
   Verschiebung darf die Zahl der Aufrufe nicht um einen verändern, sonst ist der Lauf nicht
   byte-gleich (Befund X 3.4).
2. **`VerbrauchAlt = 0` ist heute ungeschützt** (`:650` → Division durch null → `Infinity`).
   Konzept 4.8 verlangt hier eine benannte Prüfung; sie gehört in das **VDI-Modul** (im Altweg
   bleibt es bei einer Warnung, damit die Basis unberührt bleibt — Q18).

### 1.6 Stufe G1 — Schemaschritt 77, vollständig

> **Hinweis zu den Schrittnummern (16.09.2026):** „77" und „78" sind in diesem Papier Arbeitsnummern aus der Entwurfszeit. Der Bestand steht inzwischen auf `SchemaStand.Zielversion = 81` (`EPOS.Kern/Allgemein/Update/SchemaStand.cs:127`): 77 und 78 vom 15.09.2026, 79 bis 81 vom 16.09.2026 (Heizstab je Wärmepumpe, Katalogverweis der Wärmepumpen-Projektkopie, Löschschutz der Projektkosten; Aufträge #299 und #302); die tatsächliche Nummer vergibt der Schritt bei seiner Beauftragung, die nächste freie ist 82 — beim Beauftragen am `SchemaStand` nachprüfen. **E19 (16.09.2026):** derselbe Schritt benennt `Wohnflaeche` in beiden Gebäudetabellen in `Nutzflaeche` um, und der Sichtneubau liefert die Spalte unter dem neuen Namen (Konzept N1.24). Die Softwarearchitektur führt denselben Schritt unter dem Papiernamen M3.

`SchemaMigration` liegt in der **Schale**
(`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`); der Kern kennt nur die Zielzahl
`SchemaStand.Zielversion = 76` (`EPOS.Kern/Allgemein/Update/SchemaStand.cs:93`). Das Rezept steht
im Quelltext selbst (`SchemaMigration.cs:3499-3514`): Nummer ab 62 lückenlos aufsteigend; der
Schrittkörper benutzt **ausschließlich** `SqliteDdl` (`:3923`), `SqliteSpalteAnlegen` (`:4062`),
`SqliteSpalteVorhanden` (`:4039`) und `SqliteTabelleVorhanden` (`:4022`) — nie `Ddl`/`NonQuery`, die
auf `Lauf.Conn` arbeiten, und die ist im SQLite-Zweig `null`; **erst** Konstante, Methode und
`SCHRITTE_SQLITE`-Eintrag, **dann** die Zielversion.

**Eine neue Kernklasse trägt die Definitionen**, nach dem Muster
`EPOS.Kern/Allgemein/Update/ProjektEnergietraegerEindeutig.cs:65` („EINE Quelle für Migration,
Testdatenbank und Nachweis"): **`EPOS.Kern/Allgemein/Update/GebaeudeSchema.cs`** mit
`Schritt77_Gebaeudemodell` (**24** `SchemaSpalte`-Einträge — die **zwölf** Spalten der Tabelle
unten je zweimal, für `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`; nach U5 kommen die drei G2-Spalten
hinzu (1.7), dann sind es **fünfzehn** Spalten und **30** Einträge), `SQL_VIEW_DROP` und
`SQL_VIEW_NEU`.

Die Typangaben stehen in **Access**-Schreibweise und werden erst beim Anlegen übersetzt
(`StilleDb.SqliteSpaltenTyp`, `EPOS.Kern/Allgemein/Simulation/StilleDb.cs:234`):

| Spalte (beide Tabellen) | Typangabe | SQLite daraus | NULL bedeutet |
|---|---|---|---|
| `Gebaeude_Modell` | `TEXT(20)` | `TEXT` | **`VDI6007`** (E1 kehrt die Semantik von Konzept 6.1 um) |
| `Fensterflaeche_Ost` | `DOUBLE` | `REAL` | ½ `Fensterflaeche_Ost_West` |
| `Fensterflaeche_West` | `DOUBLE` | `REAL` | ½ `Fensterflaeche_Ost_West` |
| `Rahmenanteil` | `DOUBLE` | `REAL` | 0,3 |
| `Verschattungsfaktor` | `DOUBLE` | `REAL` | 0,9 |
| `Grundflaeche_Randbedingung` | `TEXT(20)` | `TEXT` | `ERDREICH` |
| `Kellertemperatur` | `DOUBLE` | `REAL` | 10 °C (N1.3) |
| `Masseanteil_Aussen` | `DOUBLE` | `REAL` | 0,3 |
| `Innenflaechenfaktor` | `DOUBLE` | `REAL` | 2,5 |
| `Heizung_Strahlungsanteil` | `DOUBLE` | `REAL` | 0,3 |
| `Heizleistung_Max` | `DOUBLE` | `REAL` (kW) | unbegrenzt |
| `Aussenbauteile_Strahlung` | **`YESNO`** | `INTEGER NOT NULL DEFAULT 0 CHECK ("…" IN (0,1))` | — (Schalter, kein Fachwert) |

Die Boolean-Übersetzung kommt **gratis** aus `StilleDb.cs:239-246` — kein handgeschriebenes
`CHECK`. Und es gibt **keinen DDL-DEFAULT auf einem Fachwert**: NULL ist die Vorgabe
(`WechselrichterSchema.cs:33-38`). `sql/schema/001_grundschema.sql`
bleibt unberührt — sie ist der eingefrorene Zielstand 61 und führt weder `PV_Modell` (Schritt 64)
noch `I_sc_max` (Schritt 70) noch `Tab_Nutzungsdauer` (Schritt 75).

**Der Sichtteil ist das Neue.** In `SchemaMigration.cs` kommt `CREATE VIEW` heute **nur in
Kommentaren** vor (`:1075`, `:1079`, `:1309`, alle zum eingefrorenen Access-Zweig); `DROP VIEW`
kommt repoweit in keiner `.cs`- und keiner `.sql`-Datei vor, und `sql/schema/002_views.sql` wird
von der **Anwendung** nicht ausgeführt — gelesen wird sie allein von `sql/tools/baue_leere_db.py:71`
und `sql/tools/Reduziere-Testdatenbank.probe.py:55`, die den eingefrorenen Stand 61 aufbauen;
danach läuft die Migration und baut die Sicht über `GebaeudeSchema` neu. **Schritt 77 ist damit der
erste Sichtneubau des SQLite-Zweigs.**

```csharp
private static bool Schritt_77_Gebaeudemodell(Lauf l)
{
    foreach (SchemaSpalte s in GebaeudeSchema.Schritt77_Gebaeudemodell)
        if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                 StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

    if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_DROP, "Sicht " + GebaeudeSchema.VIEW)) return false;
    if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_NEU,  "Sicht " + GebaeudeSchema.VIEW)) return false;

    l.Notiz("77: ... KEIN Rechenergebnis aendert sich durch diesen Schritt.");
    return true;
}
```

`SQL_VIEW_DROP` ist `DROP VIEW IF EXISTS "Abfrage_Projektgebaeude"` — damit ist der Schritt
wiederholbar. `SQL_VIEW_NEU` ist wörtlich die Definition aus `sql/schema/002_views.sql:90-91`,
ergänzt um die zwölf `Tab_Gebaeude.<Spalte>` — nach U5 fünfzehn — **hinter** `Tab_Gebaeude.ID`. Das ist kein
Schönheitsfehler, sondern die Bedingung dafür, dass `row[57] = ID`
(`ProjektGebaeudeCtrl.cs:99`) gültig bleibt und der Indexleser den Schritt überlebt — auch wenn er
im selben Merge auf Namen umgestellt wird.

Der `SCHRITTE_SQLITE`-Eintrag (vor der schließenden Klammer `:3731`, Muster `:3720-3730`) trägt
vier Stücke: Nummer, was der Schritt tut, **was ohne ihn schiefginge**, und die Methode. Das dritte
Stück lautet: *Die neuen Spalten erreichten den Leser nicht — die Sicht hat eine feste Spaltenliste,
und SQLite kennt kein `ALTER VIEW`. Das Gebäudemodell liefe für jedes Gebäude auf die Vorgabewerte,
ohne dass eine Eingabe des Anwenders je ankäme.*

**Die Katalogkopie gehört in denselben Merge.** `GebaeudeStammCtrl` führt **vier fest verdrahtete
Spaltenlisten**: `BuildValueParams` (`:345`, 54 Parameter in Positionsreihenfolge), `Insert`
(`:406`/`:409`), `Overwrite` (`:418`/`:426`) und `CopyFromStamm` (`:439`/`:449`, 55 Spalten). Eine
neue Spalte muss in **allen vieren** mitlaufen. Und `CopyFromStamm` bildet jeden Wert als
`r["X"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["X"])` ab (`:458-470`), bei Texten `""` —
**das macht aus „NULL = Vorgabe" ein hartes 0 im Projekt**; für `Rahmenanteil`,
`Verschattungsfaktor`, `Masseanteil_Aussen`, `Innenflaechenfaktor` und `Heizung_Strahlungsanteil`
ist 0 kein neutraler Wert, und `""` ist bei `Gebaeude_Modell` nicht dasselbe wie NULL. **Die elf
nullbaren der zwölf neuen Spalten** werden deshalb **NULL-erhaltend** gebunden;
`Aussenbauteile_Strahlung` ist ein Schalter (`NOT NULL DEFAULT 0`, `StilleDb.cs:239-246`) und wird
als 0/1 kopiert — für sie gibt es kein NULL. Der Eingangsbauer leitet die Vorgabe aus
NULL **oder** leerem Text ab. Ein Datenbankfall „Katalogkopie hält NULL" gehört dazu (1.9).

**`Zielversion` zuletzt** — 76 → 77 (bzw. 78, siehe 1.7). **Ohne Handgriff mitläuft** die
Auslieferungsvorlage: sie kennt keine Spaltenlisten, sondern fragt
`DataRepository.SpaltenVonTabelle` (`Werkzeuge/Auslieferungsvorlage/Projektsicht.cs:95-103`) und
`pragma_table_info` (`…/Prueflauf.cs:193-201`); `Tab_Solar_STAMM` führt keine Spalte `ReadOnly` und
bleibt vollständig (`…/Vorlagenbau.cs:113-116`).

### 1.7 Stufe G1 — die drei G2-Spalten, der Tab_Solar-Schritt und `DbWerte`

**Schritt 78 (G2-Spalten) sollte mit 77 verschmelzen.** Er bringt `Luftwechsel_Infiltration`
(`DOUBLE`, NULL = 0,3 1/h), `Luftwechsel_Nutzer` (`DOUBLE`, NULL = 0,4) und `Sommerlueftung`
(`YESNO`) — je beide Tabellen, also sechs Einträge. Die Sicht müsste dafür ein **zweites Mal** neu
gebaut werden. Da G1 und G2 nach E1 gemeinsam ausgeliefert werden, sind zwei Sichtneubauten
hintereinander nur zwei Gelegenheiten, die Definitionen auseinanderlaufen zu lassen. Konzept 6.1
hat die Trennung vorgesehen, solange G2 eine eigene Auslieferung war. **Empfehlung: verschmelzen**
(15 Spalten je Tabelle, ein Neuaufbau). Frage U5.

**Der Tab_Solar-Schritt bleibt dagegen ein eigener Schritt.** Drei `REAL`-Spalten in `Tab_Solar`
und `Tab_Solar_STAMM` (`sql/schema/001_grundschema.sql:2153` bzw. `:2169`, beide `STRICT`):

| Spalte | Quelle | Einheit | NULL bedeutet |
|---|---|---|---|
| `Gegenstrahlung` | PVGIS `IR(h)` | W/m² | Schätzung nach Blatt 3 Gl. (84)–(88) aus der Sonnenwahrscheinlichkeit |
| `Windgeschwindigkeit` | PVGIS `WS10m` | m/s | nicht verfügbar (h_a bleibt 25 W/(m²K)) |
| `Luftfeuchte` | PVGIS `RH` | % | nicht verfügbar |

**Die Werte liegen bereits in der Antwort und werden weggeworfen.** Die eingefrorene Probe
`Referenzlaeufe/Importproben/pvgis_tmy_stuttgart_72h.json` führt je Stunde `time(UTC)`, `T2m`,
`RH`, `G(h)`, `Gb(n)`, `Gd(h)`, **`IR(h)`**, `WS10m`, `WD10m`, `SP`. `TmyHourlyData`
(`SolarPVGISCalculator.cs:57-89`) liest davon nur `RH` und `WS10m` — `IR(h)` hat kein Feld —, und
`SaveTmyData` (`:586`, Spaltenlisten `:602-603`, Bindung `:614-635`) schreibt auch diese beiden
**nicht**. Vier kleine Stellen genügen: eine Eigenschaft `[JsonPropertyName("IR(h)")]` in
`TmyHourlyData`; drei Spalten und drei `DbParam` in `SaveTmyData`; drei Zeilen im
`SolardatenCtrl.MapDataRowToModel` (`:29-42`) samt drei Feldern in `SolardatenModel`; der
Migrationsschritt. `SolardatenCtrl.Insert` (`:267-322`) und `WriteDataTable` (`:325-365`) schreiben
nur `(ID, ID_Klimaregion, Temperatur)` (`:293`, `:350`), haben **beide keinen Aufrufer** und würden
an `Tab_Solar.ID_Projekt INTEGER NOT NULL` (`sql/schema/001_grundschema.sql:2155`) scheitern. **M4
löscht sie** — toter Bestand, den der Auftrag ohnehin überschreitet (Aufräumregel der
Wurzel-[`CLAUDE.md`](../../CLAUDE.md)).

Vier Gründe für den eigenen Schritt: **andere Wirkung** (die Klimaspalten bleiben in allen
Bestandsregionen NULL, bis der Anwender die Region neu importiert — eine Zusage, die im
Schrittbericht eigens stehen muss), **anderer Mitläufercode**, **anderes Risiko** (77 baut eine
Sicht neu; die Klimaspalten daranzubinden koppelt ihren Rücklauf an dieses Risiko ohne Gegenwert)
und **keine technische Notwendigkeit dagegen** (`ALTER TABLE … ADD COLUMN` ist in SQLite eine reine
Metadatenänderung; die rund 280 000 Zeilen von `Tab_Solar_STAMM` werden nicht angefasst).

**`DbWerte`** bekommt fünf Persistenzwerte nach dem Muster `PV_MODELL_*`
(`EPOS.Kern/Allgemein/DbWerte.cs:2158-2199`, Konstanten `:2173`/`:2180`): ein Abschnittskopf mit
Konzeptverweis, dann je Wert eine `public const string` mit XML-Doku, die sagt, was NULL bedeutet.

```csharp
public const string GEBAEUDE_MODELL_TAGESBILANZ = "TAGESBILANZ";
public const string GEBAEUDE_MODELL_VDI6007     = "VDI6007";
public const string GRUND_ERDREICH              = "ERDREICH";
public const string GRUND_KELLER                = "KELLER";
public const string GRUND_AUSSENLUFT            = "AUSSENLUFT";
```

**Die XML-Doku muss die durch E1 umgedrehte Semantik tragen: NULL = `VDI6007`.** Konzept 6.1
(Rev. 1) sagt noch das Gegenteil; Nachtrag N1.1 hebt es auf. Persistenzwerte sind eingefroren und
ASCII — wie `KANAL_HEIZUNG = "Heizung"` (`:1260`) und `KANAL_PROZESS = "Prozesswaerme"` (`:1271`,
bewusst ohne Umlaut, weil in SQL verglichen).

### 1.8 Stufe G1 — Ergebnisreihen, Referenzlauf-Export und die Einfrierschritte

**Die Kennzahlen gehen nicht in die Datenbank.** `Tab_ErgebnisEnergiebedarf`
(`sql/schema/001_grundschema.sql:850-862`) trägt elf Spalten und ist **einzeilig je Lauf**; die
Gebäudekennzahlen sind je Gebäude. Der Weg ist derselbe wie bei den Emissionsgrößen („Weg A",
`Referenzlauf/Ergebnisexport.cs:284-287`): **Skalare in `aggregate.csv`**, keine Spalte. Die
„einmalige, tolerante Migration" in `ErgebnisCtrl` (`EPOS.Kern/Controller/ErgebnisCtrl.cs:1117`)
ist ausdrücklich **kein** Muster zum Nachbauen ([`ADR-001_Schema-Ausrollung.md`](ADR-001_Schema-Ausrollung.md)).

**Der Export.** `Ergebnisexport.ProjektAusfuehren` (`Referenzlauf/Ergebnisexport.cs:34`) schreibt
heute unter anderem `waermebedarf.csv` (`:58`), `waermebedarf_gebaeude.csv` (`:59`) und
`stundentemperatur.csv` (`:64`); `Vektor` (`:573-592`) legt je Datei die Summe in `summen` ab.
**Achtung, eine Einheiteninkonsistenz im Bestand:** `wb.Waermebedarf_Gebaeude` steht in **Watt**
(die Addition `SimulationWaermebedarf.cs:208` geschieht vor `WattToKw:222`), während
`waermebedarf.csv` kWh führt. Das ist eingefroren und wird nicht angefasst.

Die drei neuen Reihen kommen unmittelbar nach `:64`, in einem Block, der **nur** läuft, wenn das
Projekt mindestens ein VDI-6007-Gebäude führt — Muster ist der Erdreichblock, der ohne Erdreich
**keinen einzigen Eintrag** erzeugt (`:247-253`, Begründung `:249-251`):

```csharp
// nur fuer VDI-6007-Gebaeude, sonst bleibt der Bestandsordner byte-gleich
dateien += Vektor(zielOrdner, "raumtemperatur_"       + n + ".csv", geb.Raumtemperatur,      summen);
dateien += Vektor(zielOrdner, "operative_temperatur_" + n + ".csv", geb.OperativeTemperatur, summen);
dateien += Vektor(zielOrdner, "kuehlbedarf_"          + n + ".csv", geb.KuehlbedarfKwh,      summen);
```

**Die drei neuen Reihen werden in kWh exportiert** (Einheitenregel 1 des Kerns: Zeitreihen führen
kWh); die Watt-Form bleibt intern. Nur die eingefrorenen Bestandsdateien behalten ihre Einheit —
siehe die Inkonsistenz oben.

Der Index `n` ist der Schleifenindex des Gebäudes — Vorbild `"quellspeicher_" + kennung + "_soc.csv"`
(`:99`). Die Skalare folgen dem Muster `Erdreich[i].…` (`:256-271`) mit Präfix `Geb[i].`:
`ID_Gebaeude`, `Modell`, `SpitzeKw`, `SpitzeTagesmittelKw`, `Spitze95Kw`, `KuehlenergieMwh`,
`StundenMitKuehlbedarf`, `MittlereRaumtemperaturHeizzeit`, `HeizwaermeMwh` — Einheit im Namen,
Jahressummen in MWh (Einheitenregel 2).

**Warum der bedingte Block Bedingung ist und nicht Bequemlichkeit.** `Vergleich`
(`Referenzlauf/Vergleich.cs:41`) kennt nur einen **Schlüssel**-Ausschluss innerhalb einer Datei
(`--ohne`, `_ausgenommen` `:61-62`, gefüllt `:76-79`). Eine **Datei**, die nur im neuen Lauf liegt,
bekommt `Schwere = double.MaxValue` (`:183-190`) — also FAIL, ohne Schalter dagegen. Der in N1.1
verlangte Rückweg-Regressionstest funktioniert deshalb **nur**, wenn die drei Reihen für
Tagesbilanz-Gebäude **gar nicht entstehen** — nicht „mit Nullen gefüllt"; Konzept 10.4 ist hier zu
schärfen (1.10). `EPOS.Referenzlauf` und das Windows-Werkzeug teilen sich dabei **eine** Fassung
von `Ergebnisexport.cs` und `Vergleich.cs` (`EPOS.Referenzlauf.csproj:42-51`, `<Compile Include…
Link…>`): eine Änderung wirkt auf beiden Wegen.

**Die Einfrierschritte** (E23, 16.09.2026: **GB** und **G1 + G2** in diesem Papier, **G6d** später
im Mehrzonenmodell — eine Stufe „Altweg entfernen" gibt es nicht).

- **GB** (Stufe vor G1, E4): `_prevRoomTemp` wird von `static` (`EPOS.Kern/Allgemein/BhkwPlan.cs:51`,
  gesetzt `:433`, zurückgesetzt nur über `ResetState()` `:54`) auf Instanzzustand umgestellt — heute
  startet Gebäude 2 mit der Raumtemperatur, die Gebäude 1 am 31.12. hinterlassen hat, und damit
  hängt das Ergebnis an der Zeilenreihenfolge (1008, 1039). Dazu die Warnungen statt stiller
  Fehlgriffe in der Ferienmaske (`SimulationWaermebedarf.cs:689-733`: Zeitraum 1 läuft ohne `-1`
  (`:703`, `:707`), die Zeiträume 2–4 mit `-1` (`:714`, `:721`, `:728`); Zeitraum 1 ist zudem als
  Jahreswechsel gelesen und senkt bei `Ferienbeginn_1 < Ferienende_1` den ganzen Rest des Jahres
  ab) **und die nicht nachgeführte Ferienabsenkung der Jahresschleife** (`:845-850` bestimmen nur
  `WE_Absenkung` neu; `Ferien_Absenkung` behält den Wert des letzten Vorlauftags aus `:781` und geht
  so in alle 365 Aufrufe von `TaeglHeizlastWG` ein, `:872` — die Ferienabsenkung wirkt damit
  ganzjährig oder gar nicht; Befund X 3.4 Punkt 8), die Korrektur `Bauweise` von Gebäude 10576 auf
  15 200 Wh/K in
  `Referenzlaeufe/Kenndaten_Test.sqlite` und die **vierte Einfrierregel „gesäte Gebäudedaten"**
  (`Tab_Gebaeude(_STAMM)`: `Bauweise`, U-Werte, Flächen, Sollwerte, `Luftwechselrate`,
  `Fensterdurchlassgrad` sowie ab G1 `Gebaeude_Modell`, `Fensterflaeche_Ost/West`, `Rahmenanteil`,
  `Verschattungsfaktor`, `Grundflaeche_Randbedingung`, `Kellertemperatur`, `Masseanteil_Aussen`,
  `Innenflaechenfaktor`, `Heizung_Strahlungsanteil`, `Heizleistung_Max`,
  `Aussenbauteile_Strahlung` und die drei G2-Spalten) in
  [`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md) (nach `:100-121`) und im
  Abschnitt „Regressionsnetz" der [`CLAUDE.md`](../../CLAUDE.md).
- **G1 + G2:** alle dreizehn Referenzprojekte rechnen stündlich (+7 bis +33 % Jahresheizwärme,
  Konzept 5.5), dazu drei neue CSV je VDI-Gebäude. Basis vollständig neu; die GB-Basis bleibt als
  **letzte reine Bestandsbasis**, gegen die der ausdrücklich gewählte Tagesbilanz-Weg
  **dauerhaft** regressionsgeprüft wird (E23).
- **G6d** (Mehrzonenmodell, [`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 9):
  das Zonenprojekt der Testdatenbank und die Regel „gesäte Zonendaten"; außerhalb dieses Papiers,
  hier nur genannt, damit die Einfrierkette vollständig ist.

**Eine Stufe „Altweg entfernen" friert nichts ein, weil es sie nicht gibt** (E23, 16.09.2026). Das
Referenzprojekt auf dem Altweg (A15) bleibt dauerhaft auf dem Altweg, der Rückweg-Test bleibt
dauerhaft in Betrieb, die vier Altweg-Spalten bleiben im Schema
([`ADR-006`](ADR-006_Trennung_Altweg_VDI6007.md)).

**Der erste Schritt von G1 friert nichts ein: er muss byte-gleich sein.** Die Verschiebung des
Altwegs nach `Simulation/Altweg/` samt Fassade und Vorbereitungsschritt ist ergebnisneutral und
wird **vor** der Anbindung des VDI-Wegs mit einem **byte-gleichen** Referenzlauf gegen die GB-Basis
abgenommen (E20; Kapitel 4). Ein Unterschied an dieser Stelle ist ein Fehler der Verschiebung, kein
Einfrieranlass — und er wäre nach der Anbindung nicht mehr von der Modellwirkung zu trennen.

### 1.9 Stufe G1 — Tests, Wächter und die nicht ausgelieferten Normprüfdaten

**Reine Rechenproben** (Vorbild `EPOS.Kern.Tests/PvKoeffizientenTests.cs:15-36`, `:43-44`: keine
Sammlung, keine Datenbank, ein privater Erbauer, `Assert.Equal(erwartet, ist, 9)`): Grenzfälle,
Skalierung, Determinismus, Eigenwerte reell und negativ für alle Klassenparameter der
Testdatenbank, Lastbestimmung hält den Sollwert auf 1e-9 K, Vorlauf konvergiert,
Plausibilitätsprüfungen werfen benannte Fehler.

**Datenbankfälle** mit `[Collection("Testdatenbank")]` und `IClassFixture<TestDatenbank>`
(`EPOS.Kern.Tests/GebaeudeBedarfCtrlTests.cs:30-35`; die Sammlung ist die **eine** serielle,
definiert in `EPOS.Kern.Tests/TestDatenbank.cs:29`, Wächter `DiensteSammlungTests`; Kulturpinnung
über `Kulturvorrichtung.cs:29`, alle vier Werte auf `de-DE`, `:38-50`): der Bedarfsdialog liefert
für ein umgestelltes Gebäude dieselbe Reihe wie der Lauf; Schemaschritt 77 auf der Arbeitskopie
(Muster `EPOS.Kern.Tests/Migration74Tests.cs:34-40` — Teil 1 prüft Texte ohne Datenbank, Teil 2 den
Umbau); der Namensleser hält alle 58 Bestandsfelder; **die Katalogkopie hält NULL**; die
Ost/West-Summe ist konsistent; der Mischfall Gebäude plus Ganglinie (Projekt 1041) summiert richtig.
Dazu **der Rückweg-Test** (E1/N1.1): Er läuft auf einer Arbeitskopie mit
`Gebaeude_Modell = 'TAGESBILANZ'` gegen die GB-Basis und **bleibt dauerhaft** (E23, 16.09.2026),
zusammen mit dem eigenen Modus des Referenzlaufs, der ihn trägt — er ist der laufende Nachweis,
dass der Bestandsweg unverändert rechnet.

**`TestDatenbank` zieht das Schema selbst nach** (`:197-241`, Spalten über `SpalteSicherstellen`,
Tabellen über die Kern-Schemaklasse `:224-239`, zuletzt
`UPDATE Tab_Applikation SET SchemaVersion = …` `:241`). **Schritt 77 und der Tab_Solar-Schritt
müssen hier eingetragen werden**, sonst laufen die Datenbankfälle auf einer Kopie ohne die neuen
Spalten; die Sicht wird über `DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP)` **und
danach** `…SQL_VIEW_NEU` nachgezogen — dieselbe Zweierfolge wie in Schritt 77, nach dem Muster der
`NutzungsdauerSchema.Anweisungen`-Schleife (`:224-225`). Ein einzelnes `CREATE VIEW` scheitert auf
der Kopie, weil die Sicht dort besteht, und der Fehler verschwindet im `catch`
(`TestDatenbank.cs:243-246`, `Console.WriteLine`).

**Die vier vorhandenen Wächter und was sie sehen:**

| Wächter | Umfang | Wirkung auf `Simulation/Gebaeude/` |
|---|---|---|
| `DoubleWacheTests` (`EPOS.Kern.Tests/DoubleWacheTests.cs:39`) | alle `.cs` unter `Simulation/` **mit Unterordnern** (`SearchOption.AllDirectories`, `:206`) plus `BhkwPlan.cs` (`:203`) | **greift.** Kein `float`, kein `Convert.ToSingle`, kein `MathF.`, kein `f`-Suffix; Ausnahmeliste leer (`:63-64`) und bleibt es. `System.Numerics.Complex` (G3) ist `double`-basiert |
| `EinheitenWacheTests` | Wächter 2 prüft nur die **namentlich gelisteten** Simulationsklassen (`:201-206`); `Simulationsdateien()` (`:447-455`) baut daraus `Simulation` + Eintrag und prüft `File.Exists` mit `Assert`, die Gegenprobe zählt Liste gegen Dateien (`:361`) | **greift nur halb.** Einzutragen ist `Gebaeude/GebaeudeModellErgebnis.cs` — **mit Unterordner**, sonst ist der Wächter rot — im selben Merge, in dem die Klasse entsteht; ohne Eintrag bleibt eine stille Lücke |
| `RechenrandTests` (`EPOS.Kern.Tests/RechenrandTests.cs:29`) | prüft `Rechenrand` selbst (`EPOS.Kern/Allgemein/Simulation/Rechenrand.cs:78`, `:95`) | **greift nicht automatisch**, aber die Regel gilt: Kappung an `Maximaleraumtemperatur`, Grenze `Heizleistung_Max` und die Bisektion des Umschaltzeitpunkts sind Betriebsschwellen und nehmen `SchwelleErreicht`, nicht `>=` |
| `ParallelitaetWacheTests` | vier plattformfreie Projekte, sieben Muster (`:75-84`) | **greift.** Der Löser ist einfädig; ein `Parallel.For` über die Gebäude widerspräche dem Determinismusversprechen. Nicht tun |

**Ein fünfter Wächter kommt hinzu: die Trennung der Module** (E20, 16.09.2026). Vorschlag:
`EPOS.Kern.Tests/RechenwegTrennungWacheTests.cs` nach dem Muster von
`DoubleWacheTests.Der_Waechter_sieht_den_Bestand_und_jede_Ausnahme_existiert` (`:137`) — er liest
die `.cs`-Dateien unter `EPOS.Kern/Allgemein/Simulation/Gebaeude/` und hält drei Sätze fest:

1. **Kein Bezeichner aus `Altweg/`** erscheint darin (weder der Namensraum noch eine der
   verschobenen Methoden `Berechnung_Gebaeude_Tageswerte`, `DBTagesVeteilung`, `StdWerte`,
   `SolareGewinneC`, `SpezWaermeverlusteC`, `TaeglHeizlastWG`).
2. **Der Ordner `Altweg/` existiert und ist nicht leer** — sonst prüft der Wächter nichts und ist
   still grün (dieselbe Gegenprobe, die `DoubleWacheTests` für seine Ausnahmeliste führt).
3. **Kein Bezeichner aus `Gebaeude/`** erscheint in `Altweg/` — der Altweg bekommt keine neue
   Funktion (E20).

**Der Wächter bleibt dauerhaft** — mit dem Ordner `Altweg/`, den es dauerhaft gibt (E23,
16.09.2026). Dazu
`DokumentationLinkWacheTests` (`:133` Verweise, `:170` „Der_Index_nennt_jedes_Papier") und
`HuellenwegTests` beim Hüllenumzug (2.8).

**Die Normprüfdaten dürfen nicht ausgeliefert werden** (Konzept N1.2: das Ausliefern der Normzahlen
in Testdateien ist eine Vervielfältigung). **Ein Muster für lokal beizustellende, gitignorierte
Prüfdaten gibt es im Repositorium heute nicht** — `.gitignore` kennt `dev/` (`:383`), `.work/`
(`:376`) und `Referenzlaeufe/Arbeitskopie/` (`:377`), aber keinen Ort für Prüfdaten;
`Referenzlaeufe/Importproben/` ist das Gegenteil (eingefroren, versioniert,
`Referenzlaeufe/LIESMICH.md:127-130`), und LFS ist keine Zugriffsbeschränkung. Die Bauform ist
`TestDatenbank` (`EPOS.Kern.Tests/TestDatenbank.cs:98-152`, Suche aufwärts vom Laufordner
`Quelle()` `:280-286`, Kennzeichen `Vorhanden`, jeder Fall beginnt mit
`if (!_db.Vorhanden) return;`). Vorschlag:

eine Vorrichtung `Normzahlen` mit `Vorhanden`, die
`Referenzlaeufe/Normzahlen/vdi6007_blatt1_anhang_a1.csv` sucht (ohne Datei **schweigt** jeder Fall);
ein `.gitignore`-Eintrag `Referenzlaeufe/Normzahlen/` mit begründendem Kommentar; ein
**versioniertes** `Referenzlaeufe/Normzahlen/LIESMICH.md`, das sagt, woher die Zahlen kommen
(VDI 6007 Blatt 1:2015-06, Tabellen A1.3…A12.3, Seiten 41–63) und wie sie einzutragen sind —
**ohne eine einzige Zahl**; und ein **versionierter Gegenwächter** für das, was ohne die Zahlen
prüfbar ist (Prüfregel Band ± 0,15 K / ± 1,5 W nach E10, Vorzeichenkonvention, und dass die Vorrichtung
wirklich sucht — Vorbild
`DoubleWacheTests.Der_Waechter_sieht_den_Bestand_und_jede_Ausnahme_existiert`, `:137`).

**Die Folge ist auszusprechen: der Nachweis der zwölf Normtestfälle ist ein lokaler Nachweis, kein
CI-Nachweis.** In `kern.yml` laufen die Normfälle schweigend durch. Das ist eine bewusste Lücke im
Gate und gehört ins Protokoll; der Auszug des Laufs (Abweichung je Fall, ohne Absolutwerte) gehört
als Tabelle in die Dokumentation. Frage U8.

### 1.10 Was dieses Papier am Konzept korrigiert

| Stelle im Konzept | Korrektur | Beleg |
|---|---|---|
| 4.1 / 11, Klassenname | `Zonenmodell2K` statt `Zonenmodell7R2C` — die Richtlinie sagt „2-K-Modell" | Konzept N1.3 |
| 4.4, Wochenendkalender | aus `Tab_Klimadaten.WE` (`SimulationWaermebedarf.cs:524`), **nicht** aus `SolardatenCtrl.Referenzjahr` — sonst ist der dort verlangte Test nicht führbar | `KlimaImportAblauf.cs:354`, `SolardatenCtrl.cs:222` |
| 4.4, Erdreich | der Kusuda-Ansatz liegt fertig im Kern; die Zahlen 0,68/22 Tage fallen aus dem Vorgabeboden. Kein neuer Code | `ErdreichTemperatur.cs:411`, `:47`/`:188` |
| 4.6, Vorlauf | der Bestandsvorlauf sind **15** Tage — die Zahl ist belegt | `SimulationWaermebedarf.cs:748` |
| 6.1, NULL-Semantik | `Gebaeude_Modell` NULL = **`VDI6007`**; die Tabelle in 6.1 sagt noch das Gegenteil | Konzept N1.1 |
| 6.1, Schritt 78 | mit 77 verschmelzen, weil G1 und G2 gemeinsam ausgeliefert werden | 1.7, Frage U5 |
| 6.2, Sichtdefinition | „das Schemaskript und der Migrationsschritt führen dieselbe Definition" trifft nicht zu: `sql/schema/002_views.sql` wird zur Laufzeit **nicht ausgeführt** und ist eingefroren. Die Definition gehört in `GebaeudeSchema` | von der Anwendung nicht ausgeführt; gelesen allein von `sql/tools/baue_leere_db.py:71` und `sql/tools/Reduziere-Testdatenbank.probe.py:55`; `WechselrichterSchema.cs:33-38` |
| 8.1, Randbedingung | die Randbedingung der Grundfläche steht als **Spalte der U·A-Tabelle**, nicht als zwölftes Feld der Modellgruppe | E2/N1.6, Befund M 5.3 |
| 10.4, neue Reihen | sie dürfen für Tagesbilanz-Gebäude **gar nicht entstehen** — `Vergleich` kennt keinen Dateiausschluss | `Vergleich.cs:183-190` |
| 10.5, Wächter | `EinheitenWacheTests` greift auf einer neuen Datei nur halb; die Liste `Simulationsklassen` ist zu erweitern — mit Unterordner | `EinheitenWacheTests.cs:201-206`, `:447-455` |
| N1.6, Gruppenname | drei Gruppen statt einer „Hülle und Rechenmodell": die U·A-Tabelle trägt die Hülle, die Summen stehen als eigene Gruppe, „Rechenmodell" bleibt für die sieben Parameter | 2.3, 2.5 |
| N1.25, Vorbereitungsschritt | „modellfrei" gilt für Klimakalender, Bewohner, `VerbrauchNeu` und die beiden Flächen — **nicht für die Rückrechnung selbst**: `VerbrauchAlt` kommt aus dem gewählten Modul, das die Fassade hinter der Weiche ein zweites Mal ruft. E20 ist damit erfüllt (kein Weg ruft den anderen), die Rechenfolge des Bestands bleibt | 1.1, 1.5; Befund X 2.4 |
| 6.1, Spaltenliste | die Tabelle nennt elf Spalten und **lässt `Kellertemperatur` aus**, obwohl Rechenschritte 1.1, 1.8 und 2.4 sie führen | [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 1.1; Befund X 5.4 |
| 6.4, Leser von `Gebaeude_Modell` | nicht mehr „in `HeizwaermeEinesGebaeudes`", sondern **in der Weiche der Fassade** und in der Anzeige; der Eingangsbauer liest die Spalte nicht | E20, 1.1 |

---

## 2. Der Gebäudedialog

### 2.1 Der Ist-Stand in fünf Sätzen

Der „Gebäudedialog" ist **fünf Masken**, nicht eine:

| Datei | Zeilen | Rolle |
|---|---|---|
| `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor` | 828 | **Wirt**: Zweispaltenauswahl (`:62-149`), Detailblock (`:154-195`), vier Überlagerungen (`:202-248`), `SpeichernLeiste` (`:199`) — **kein einziges Fachfeld** |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor` | 983 | **der Editor**, den E2 trifft: ein Katalogsatz auf zwei Reitern |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeBedarfDialog.razor` | 324 | **Auskunft**: drei Kennzahlen (`:60-84`), Ganglinie über `ChartBild` (`:91-93`), Monatsübersicht (`:96-117`) |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeWohnflaecheDialog.razor` | 284 | **Zuordnung**: Bedarfsart, Verbrauch/Wohnfläche, Jahresnutzungsgrad — der Ort der Skalierung (E8) |
| `EPOS.UI/Dialoge/Bedarf/GebaeudetypDialog.razor` | 432 | Tagesverteilungen eines Gebäudetyps |

**Konzept 8.1 nennt `GebaeudeDialog.razor` und `GebaeudeKatalogDialog.razor` gleichrangig — das
trifft nicht zu.** Die neuen Gruppen entstehen **einmal**, im Editor; der Wirt bekommt nur eine
Spalte und zwei leise Kennzahlen (2.7).

**Alle Größen der U·A-Tabelle sind bereits da**, nur verstreut: Reiter 1 führt „Kenngrößen"
(`:90-143`), „Flächen [m²]" (`:145-177`) und „U-Werte [W/m²K]" (`:179-203`); Reiter 2 führt
Raumtemperaturen (`:212-236`), „Wärmebrückenverlustkoeffizienten" (`:238-253`), „Abmessung Anschluß"
(`:255-272`), Ferien (`:274-314`) und „Sonstiges" mit der Luftwechselrate (`:316-335`). Der Umbau
ist damit eine **Umordnung**, keine Neuentwicklung — zwei Felder kommen hinzu (Fenster Ost, Fenster
West), sieben Modellparameter und ein Schalter.

Der Speicherweg ist dreistufig: der Dialog ruft den Delegaten
`Speichern(GebaeudeKatalogDaten, istNeu, bezeichner)` (`GebaeudeKatalogDialog.razor:398-399`,
gerufen `:918-935`), die Hülle
`WindowsFormsApplication1/Views/Gebäude/GebaeudeKatalogHuelle.cs` führt ihn in `Schreiben(...)`
(`:318-337`) aus — die ReadOnly-Sperre sitzt **in der Hülle** (`:313-317`, `:323-326`) — und ruft
`GebaeudeStammCtrl.Insert` bzw. `Overwrite` (`:334`); die Feldabbildung steht in `NachModell`
(`:416-505`). **`EPOS.UI.Daten` führt für Gebäude heute nichts**: der Ordner `Bedarf/` enthält
allein `BedarfErgebnisHuelle.cs`, alle drei Gebäudehüllen liegen in der Windows-Schale.

### 2.2 Sechs Befunde am Bestand, die der Umbau mit erledigt

| # | Befund | Stelle | Folge |
|---|---|---|---|
| M-1 | Der Editor ist der **einzige** Dialog des Hauses ohne `SpeichernLeiste`: drei eigene Knöpfe „Überschreiben", „Speichern unter"/„Speichern", „Beenden" (`:360-369`) und zusätzlich „Werte übernehmen" am Fuß des zweiten Reiters (`:337-340`); geschrieben wird **sofort** (`:894-935`), „Beenden" verwirft nichts und meldet immer `true` (`:950-956`) | gegen `EPOS.UI/CLAUDE.md:48-56` | die zehn neuen Prüfregeln aus Konzept 4.8 hingen sonst an **drei** Schreibstellen |
| M-2 | Reiter 2 führt einen **zweiten, eigenen Stand** (`:549-558`), der erst mit „Übernehmen" in den Satz wandert (`:788-829`) | dito | U und A stehen heute auf Reiter 1, ψ und L auf Reiter 2 — die U·A-Summe wäre bis zum „Übernehmen" falsch |
| M-3 | **Kein einziges Zahlenfeld des Editors hat `Min`/`Max`** (`:123-199`, `:218-232`); keine Prüfung auf `0 < g ≤ 1`, keine auf U-Werte | — | Konzept 4.8 verlangt Bereiche für g, U, Bauweise, Ferientage |
| M-4 | ψ- und L-Gruppe tragen **dieselben drei Beschriftungen** (`:244-249` gegen `:261-268`, beide `LabelWbvk*`) in **verschiedener Reihenfolge** (Fenster/Keller/Dach gegen Fenster/Dach/Keller) | — | E2 führt ψ und L in **einer** Zeile zusammen und räumt beides auf |
| M-5 | Der englische Wert `GEBK_GRP_UWERTE` trägt zwei unsichtbare Steuerzeichen | `EPOS.Kern/MyResource/Resource.en-US.resx:13294` | beim Anfassen der Gruppe bereinigen |
| M-6 | `Fensterflaeche_Ost` heißt im Modell Ost, meint aber Ost **und** West | `GebaeudeKatalogHuelle.cs:364`, `:443`, `:453` | die Umbenennung auf `Fensterflaeche_OstWest` gehört zu G1, Merge M2 (1.6) |

**Die Validierung heute:** `PflichtzahlenStehen()` (`:859-892`) prüft 17 Zahlen des ersten Reiters
und meldet den **ersten** fehlenden mit seinem *Feldnamen* (`:963-979`, Sprung auf Reiter 1 `:887`);
`BeiUebernehmen()` (`:788-829`) prüft auf Reiter 2 allein die vier Ferienregeln über
`Ferienzeit.Pruefen` (`:798-799`) und leitet vier Werte ab (`:804-826`). `BauweiseNachfuehren()`
(`:713-715`) bestimmt die `Bauweise` aus der Bauart, der Rückweg beim Laden läuft über
`BauartAusBauweise` (`:743-744`).

### 2.3 Das Soll — Reiter 1 als Textskizze

```
+-------------------------------------------------------------------------------------------+
|  Gebäude im Katalog bearbeiten: "Mehrfamilienhaus 1969-1978"                          [x]  |
+-------------------------------------------------------------------------------------------+
|  [ Gebäude und Hülle ]   [ Temperaturen und Ferien ]                                       |
+-------------------------------------------------------------------------------------------+
|  Kenngrößen                                                                                |
|    Name             [ Mehrfamilienhaus 1969-1978 ]   Gebäudetyp  [ Wohngebäude       v]    |
|    Beschreibung     [                            ]   Gebäudeart  [ Mehrfamilienhaus  v]    |
|    Baujahr (Klasse) [ E  1969-1978              v]   Verwendung  [ Wohngebäude       v]    |
|    Bauart           [ schwer                    v]   Wohn-/Nutzfläche  [  850 ] m²         |
|    Fläche / Nutzer  [   35 ] m²                      Interne Gewinne   [ 4250 ] W          |
|    Fensterdurchlaßgrad  [ 0,60 ]                     Raumhöhe          [  2,5 ] m          |
|    Luftwechselrate      [ 0,70 ] 1/h                                                       |
+-------------------------------------------------------------------------------------------+
|  Hülle: Transmission je Bauteil                                                            |
|  +--------------------------+----------+----------+---------------+----------+             |
|  | Bauteil                  | U bzw. p | A bzw. L | Randbedingung |    U*A   |             |
|  +--------------------------+----------+----------+---------------+----------+             |
|  | Außenwand                | [ 1,20 ] | [  520 ] | Außenluft     |   624,0  |             |
|  | Fenster                  | [ 2,80 ] |    128   | Außenluft     |   358,4  |  A gerechnet|
|  | Dach                     | [ 0,60 ] | [  310 ] | Außenluft     |   186,0  |             |
|  | Bodenplatte              | [ 1,00 ] | [  310 ] | [ Erdreich v] |   310,0  |  nur hier   |
|  | Sonstiges                | [ 2,00 ] | [    8 ] | Außenluft     |    16,0  |             |
|  | Wärmebrücke Fenster-Wand | [ 0,10 ] | [  240 ] |      -        |    24,0  |             |
|  | Wärmebrücke AW-Keller    | [ 0,15 ] | [   88 ] |      -        |    13,2  |             |
|  | Wärmebrücke Wand-Dach    | [ 0,10 ] | [   88 ] |      -        |     8,8  |             |
|  +--------------------------+----------+----------+---------------+----------+             |
+-------------------------------------------------------------------------------------------+
|  Wärmeleitwerte                                                                            |
|    H_T Transmission :  1 540,4 W/K       H_ve Lüftung :    505,8 W/K                       |
|    H_ges gesamt     :  2 046,2 W/K                                                         |
+-------------------------------------------------------------------------------------------+
|  Fenster nach Orientierung                                                                 |
|    Fensterfläche Nord [   24 ] m²      Fensterfläche Süd  [   52 ] m²                      |
|    Fensterfläche Ost  [   26 ] m²      Fensterfläche West [   26 ] m²                      |
|    Summe Ost + West :    52 m²         gesamte Fensterfläche :  128 m²                     |
+-------------------------------------------------------------------------------------------+
|  Modellparameter (VDI 6007)                                                                |
|    Rahmenanteil         [ Vorgabe 0,3 ]   Verschattungsfaktor  [ Vorgabe 0,9 ]              |
|    Masseanteil außen    [ Vorgabe 0,3 ]   Innenflächenfaktor   [ Vorgabe 2,5 ]              |
|    Strahlungsanteil Hz. [ Vorgabe 0,3 ]   Heizleistungsgrenze  [ unbegrenzt ] kW            |
|    [ ] Außenbauteile mit Strahlung                                                          |
+-------------------------------------------------------------------------------------------+
|  Rechenweg         [ VDI 6007                        v]                                    |
|    VDI 6007: Raumtemperatur, Kühlbedarf und Spitzenlast je Stunde.                          |
|  > Tagesbilanz (Bestandsweg)                                    (eingeklappt, nur Altweg)  |
+-------------------------------------------------------------------------------------------+
|                                         [ Speichern unter... ]  [ Abbrechen ]  [ OK ]      |
+-------------------------------------------------------------------------------------------+
```

**Die Maske folgt allein der VDI-6007-Struktur** (E20, 16.09.2026). Die **Modellparameter stehen
immer** — auch bei einem Gebäude auf dem Altweg —, sind immer bearbeitbar und gelten dort nach der
Umstellung; sie sind der Parametersatz des Gebäudes, nicht der Rechenweg. Der Schalter heißt
**„Rechenweg"** und trägt **dauerhaft** zwei Werte (E23, 16.09.2026): **„VDI 6007"** (Vorgabe,
Spaltenwert NULL) und **„Tagesbilanz"**; darunter steht eine Herleitungszeile, die in beiden
Stellungen sagt, was gilt — bei „Tagesbilanz" zusätzlich, dass dieser Weg der eingefrorene
Bestandsweg ist und weder Kühllast noch Anlagenkopplung kennt.

Steht der Rechenweg auf **Tagesbilanz**, klappt darunter der Abschnitt
**„Tagesbilanz (Bestandsweg)"** auf. Er trägt allein die Felder, die **nur** der Altweg liest — nach
[Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) 1.5 sind das
vier Spalten, davon zwei sichtbare: **Gebäudetyp** (die Tagesverteilung hängt daran) und
**Fensterfläche Ost/West** (schreibgesperrt, gerechnete Summe aus Ost und West); `Wochenende` und
`Ferien` sind abgeleitete Flags, die die Hülle setzt und die der Dialog nicht zeigt (Befund X 5.1).
Dazu tritt unter „Wärmeleitwerte" die vierte Zeile mit dem gewichteten Wert (2.5). **Der Abschnitt
bleibt dauerhaft** (E23); bei einem Gebäude auf VDI 6007 erscheint er nicht.

**Reiter 2 „Temperaturen und Ferien"** bleibt, verliert aber die Gruppen
„Wärmebrückenverlustkoeffizienten" und „Abmessung Anschluß" (ihre sechs Werte stehen jetzt in der
U·A-Tabelle), den eigenen Stand (M-2) und den Knopf „Werte übernehmen".

### 2.4 Die Feldtabelle

**Die Spalte „Weg"** sagt, welcher Rechenweg die Größe liest — `beide`, `nur VDI`, `nur Altweg`
oder `—` (kein Rechenleser). Sie ist die Regel für den Dialog nach E20: **nur Altweg** gehört in
den eingeklappten Abschnitt „Tagesbilanz (Bestandsweg)" (2.3), alles
andere steht in der Hauptstruktur — die Modellparameter immer sichtbar und bearbeitbar. Die
Zuordnung je Spalte samt Zählung steht in
[Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) 1.2 bis 1.5.

*Gruppe „Kenngrößen"* — unverändert bis auf einen Zugang: die **Luftwechselrate wandert von Reiter 2
nach vorn** (heute `GebaeudeKatalogDialog.razor:322-324`), weil H_ve aus ihr entsteht und in der
Summenzeile derselben Ansicht steht.

| Feld | Einheit | Bindung | Vorgabe | Pflicht | Weg | Prüfregel (Konzept 4.8) |
|---|---|---|---|---|---|---|
| Name | — | `Daten.Name` (Admin: Klappliste) | — | beim Anlegen | — | nicht leer |
| Gebäudeart / Baujahr / Verwendung | — | `Gebaeudeart`, `Baualtersklasse`, `Verwendung` (**Steuerwert**, `GebaeudeKatalogDaten.cs:45-49`) | — | nein | — | — |
| **Gebäudetyp** | — | `Typ` — Schlüssel der Tagesverteilung (`SimulationWaermebedarf.cs:584`, `:601`) | — | nein | **nur Altweg** | Abschnitt „Tagesbilanz (Bestandsweg)" |
| Bauart | — | `Bauart` → `Bauweise` (`:713-715`) | schwer | **ja** (Fußnote) | beide | 5 ≤ Bauweise/Nutzfläche ≤ 200 Wh/(m²K) |
| Nutzfläche (E13) | m² | `WohnflaecheGesamt` (Editorfeld des Bestands; Spalte `Nutzflaeche` ab M3, E19) | — | **ja** | beide | > 0 |
| Fläche / Nutzer | m² | `FlaecheNutzer` | 35 (Hülle, `GebaeudeKatalogHuelle.cs:429-431`) | **ja** | beide | > 0 |
| Interne Wärmegewinne | W | `Waermegewinne` | — | **ja** | beide | ≥ 0 |
| Fensterdurchlaßgrad | — | `Fensterdurchlassgrad` | — | **ja** | beide | 0 < g ≤ 1 |
| Raumhöhe | m | `Raumhoehe` | — | **ja** | beide | > 0 |
| Luftwechselrate | 1/h | `Luftwechselrate` | — | **ja** | beide | > 0 |

*Gruppe „Tagesbilanz (Bestandsweg)"* — eingeklappt, **nur bei einem Gebäude auf dem Altweg**,
dauerhaft (E23):

| Feld | Einheit | Bindung | Weg | Bemerkung |
|---|---|---|---|---|
| Gebäudetyp | — | `Typ` | nur Altweg | wählt die Tagesverteilung aus `Abfrage_Tagverteilung` |
| Fensterfläche Ost/West | m² | `Fensterflaeche_Ost_West` | nur Altweg | **schreibgesperrt**: gerechnete Summe aus Ost und West (2.6) |
| (`Wochenende`, `Ferien`) | 0/1 | abgeleitete Flags der Hülle (`GebaeudeKatalogDialog.razor:809`) | nur Altweg | **nicht gezeigt** — Ableitungen, keine Eingaben |

**Fußnote zur Bauart.** Bei leerer Auswahl liefert `BauweiseAusBauart` ein **absolutes** 50
(`Gebaeudebauweise.cs:66`), nicht `Wohnfläche × 50`; die neue Prüfregel fiele damit für jedes
Gebäude über 10 m² auf einen unerklärlichen Fehler. Die Bauart ist deshalb Pflichtfeld — oder der
Rückfall wird in GB auf `Wohnfläche × 50` gezogen (ergebnisneutral, weil heute kein
Referenzgebäude ihn trifft).

*Gruppe „Modellparameter (VDI 6007)" und der Schalter „Rechenweg"* — die neuen Felder; **alle
nullbar, der Dialog schreibt `null`, nicht die Vorgabe** (Vorbild
`EPOS.UI/Dialoge/Erzeuger/PvModellFelder.razor:93-97`). **Alle sind immer sichtbar und immer
bearbeitbar** (E20) — auch bei einem Gebäude auf dem Altweg, wo sie nach der Umstellung gelten:

| Feld | Einheit | Bindung (neue Spalte, 1.6) | Vorgabe-Anzeige | Weg | Prüfregel | Sichtbar |
|---|---|---|---|---|---|---|
| **Rechenweg** | — | `Modell` (`Gebaeude_Modell`); **NULL = „VDI 6007"**, zweiter Wert „Tagesbilanz" (E1, E20, E23) | — | Weiche | Wert aus `DbWerte.GEBAEUDE_MODELL_*` | immer, **dauerhaft** |
| Rahmenanteil | — | `Rahmenanteil` | Vorgabe 0,3 | nur VDI | 0 ≤ x < 1 | **immer** |
| Verschattungsfaktor | — | `Verschattungsfaktor` | Vorgabe 0,9 | nur VDI | 0 < x ≤ 1 | **immer** |
| Masseanteil außen | — | `Masseanteil_Aussen` | Vorgabe 0,3 | nur VDI | 0,05 ≤ x ≤ 0,95 | **immer** |
| Innenflächenfaktor | — | `Innenflaechenfaktor` | Vorgabe 2,5 | nur VDI | 0,5 ≤ x ≤ 10 | **immer** |
| Strahlungsanteil Heizung | — | `Heizung_Strahlungsanteil` | Vorgabe 0,3 | nur VDI | 0 ≤ x ≤ 1 | **immer** |
| Heizleistungsgrenze | kW | `Heizleistung_Max` | Vorgabe: unbegrenzt | nur VDI | > 0 | **immer** |
| Außenbauteile mit Strahlung | — | `Aussenbauteile_Strahlung` (0/1, `NOT NULL DEFAULT 0`) | aus | nur VDI | — | **immer** |

Die DTO-Erweiterung in `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDaten.cs` sind zwölf Felder —
`string? Modell` (null = VDI6007), `string? GrundflaecheRandbedingung` (null = ERDREICH),
`bool AussenbauteileStrahlung` und neun `double?` — die fünf Zahlenparameter der Gruppe
„Modellparameter" (`Rahmenanteil`, `Verschattungsfaktor`, `MasseanteilAussen`,
`Innenflaechenfaktor`, `HeizungStrahlungsanteil`), die Heizleistungsgrenze, die `Kellertemperatur`
sowie `FensterflaecheOst` und `FensterflaecheWest` (2.9). Alle Zahlen des DTO sind dort ohnehin
`double?`, „weil leer etwas anderes ist als 0" (`:13-17`) — die Semantik passt also schon.

**Hilfe und KI-Anmeldung gehören dazu.** Die neue Gruppe trägt
`<InfoKnopf Schluessel=… Dialogname=… />` — „Jeder Dialog bietet den Hilfe-Assistenten an"
([`EPOS.UI/CLAUDE.md`](../../EPOS.UI/CLAUDE.md), Abschnitt `Dialoge/`) —, und der Wirt meldet die
Feldliste über `KiMaskenanmeldung.Fuer(name, () => Daten, KiHaken())` an; `Pruefen` ist dieselbe
Prüfung wie am OK-Weg.

**Weder verstecken noch sperren — die Modellparameter stehen immer** (E20, 16.09.2026). Der
Bestandsentwurf wollte die sieben Parameterfelder im Tagesbilanz-Weg ausblenden oder sperren und
ließ die Wahl zwischen beidem offen (Frage U2). E20 hebt die Frage auf: Die Felder sind der
**Parametersatz des Gebäudes**, nicht der Rechenweg, sie gelten nach der Umstellung auf VDI 6007,
und ein Dialog, der je nach Wahl Felder verschwinden lässt, ist für den Anwender ein Rätsel und
für die Tests ein Zustandsraum. Bedingt ist allein der **Zugang**: der eingeklappte Abschnitt
„Tagesbilanz (Bestandsweg)" mit den vier Feldern, die nur der Altweg liest (2.3) — dort über `@if`,
weil ein Abschnitt, den es für dieses Gebäude nicht gibt, auch nicht grau dastehen soll. Die
Herleitungszeile unter dem Schalter „Rechenweg" sagt in beiden Stellungen, was gilt.

### 2.5 Die U·A-Tabelle und die Wärmeleitwerte

Eine `<table class="epos-raster">` in einer `.epos-raster-huelle` — dieselbe Bauform, die die
Gebäudedialoge für ihre Listen schon benutzen (`GebaeudeDialog.razor:69-92`, `:110-133`), **nicht**
die virtualisierte `Katalogliste`.

| Spalte | Inhalt | Bauform |
|---|---|---|
| Bauteil | fester Zeilentext | `<th scope="row">` |
| U bzw. ψ | W/(m²K) bzw. W/(mK) | `Zahlenfeld`, `Min="0.1" Max="6"` bei U, `Min="0" Max="2"` bei ψ |
| A bzw. L | m² bzw. m | `Zahlenfeld`, `Min="0"` |
| Randbedingung | Außenluft \| Erdreich \| Keller | `Auswahlfeld`, **nur** in der Zeile „Bodenplatte" wählbar, sonst fester Text; bei „Keller" steht daneben das Feld `Kellertemperatur` (Vorgabe 10 °C) — der Eingangsbauer setzt daraus θ_NR,eq statt eines Reduktionsfaktors (Konzept N1.3) |
| U·A | W/K, **gerechnet, nur Anzeige** | `<td class="epos-zahl">` |
| Herkunft | manuell \| Katalog \| IFC | **ab G4**, vorher nicht gezeichnet |

Die acht Zeilen und ihre Bindungen: Außenwand (`UWertAussenwand`/`FlaecheAussenwand`), Fenster
(`UWertFenster`/**Summe der vier Orientierungen, nur lesbar**), Dach
(`UWertDachflaeche`/`Dachflaeche`), Bodenplatte (`UWertGrundflaeche`/`Grundflaeche`/**Randbedingung**),
Sonstiges (`UWertSonstiges`/`SonstigeFlaechen`) und die drei Wärmebrücken
(`WbvkFensterWand`/`AnschlussFensterWand`, `WbvkAussenwandKeller`/`AnschlussAussenwandKeller`,
`WbvkWandDach`/`AnschlussWandDach`).

**Die Randbedingung der Grundfläche steht genau hier und nirgends sonst** — sie ist eine Spalte,
kein zwölftes Feld der Modellgruppe (Konzept 8.1 zählt sie dort auf; dieses Papier folgt E2/N1.6,
Korrektur in 1.10). **Die Tabelle ist in beiden Rechenmodellen sichtbar**: sie ist die gemeinsame
Zielstruktur von Klassenweg (Konzept 4.3), Bauteilweg (G3) und IFC-Import (G4), und auch im
Tagesbilanz-Weg sind U, A, ψ und L die Eingaben.

**Die Summen** stehen darunter in einer eigenen Gruppe „Wärmeleitwerte" (oder als `<tfoot>`):

| Größe | Rechnung | Einheit |
|---|---|---|
| **H_T** | Σ (U·A)_Bauteile + Σ (ψ·L)_Wärmebrücken | W/K |
| **H_ve** | `Luftwechselrate` · `WohnflaecheGesamt` · `Raumhoehe` · 0,34 Wh/(m³K) | W/K |
| **H_ges** | H_T + H_ve | W/K |
| **H_T gewichtet (Tagesbilanz)** — *nur im Tagesbilanz-Weg* | 0,83·U_w·A_w + U_f·A_f + 0,95·U_d·A_d + 0,45·U_g·A_g + U_s·A_s + 0,83·Σψ·L | W/K |

Der Faktor 0,34 ist der des Stundenmodells (Konzept 4.4); der Bestand rechnet mit
`1,2 · 0,2777…` = 0,3333 Wh/(m³K) (`EPOS.Kern/Allgemein/BhkwPlan.cs:357`, `:359`), die Gewichte
stehen in `SpezWaermeverlusteC` (`BhkwPlan.cs:347-353`). Darunter eine `Herleitungszeile`: „Der
Tagesbilanz-Weg wichtet Außenwand und Wärmebrücken mit 0,83, das Dach mit 0,95 und die Bodenplatte
mit 0,45. Das Stundenmodell rechnet ungewichtet." Damit ist der Unterschied je Gebäude erklärbar,
wie E2 es verlangt.

**Die Rechnung liegt im Kern, nicht im Dialog.** Eine reine Hilfsklasse
`EPOS.Kern/Allgemein/Gebaeudehuellbilanz.cs` nach dem Vorbild
`EPOS.Kern/Allgemein/Gebaeudebauweise.cs` und `Ferienzeit` liefert Zeilen und Summen — **eine
Wahrheit für Dialog und Eingangsbauer** (1.4); der Dialog zeigt sie nur an. Dieselbe Maske hat das
schon einmal begründet: „Die Rechnung selbst steht — wie Ferienzeit und Suchmuster — als reine
Hilfsklasse im Kern; ein Controller wird von dieser Komponente nicht angefasst"
(`GebaeudeKatalogDialog.razor:43-45`). **Die Kernprobe hält Transmission + Wärmebrücken** des
gewichteten Zweigs auf 1e-12 gegen `SpezWaermeverlusteC` (`BhkwPlan.cs:347-353`), mit
`aussenTemp = 0` und nach Division durch 100 (die Rückgabe `:362` trägt den Faktor 100, der
Aufrufer teilt ihn wieder heraus, `SimulationWaermebedarf.cs:774`). Der Lüftungsanteil des Bestands
hängt an der Außentemperatur (`:356-357`) und ist keine Dialogkennzahl — er bleibt außen vor.
Bezugsfläche ist im Lauf `Tab_Gebaeude.Wohnflaeche` (`SimulationWaermebedarf.cs:773`), im Editor
`Wohnflaeche_gesamt`; beim Schreiben werden sie gleichgesetzt (`GebaeudeKatalogHuelle.cs:427`,
`:458`), bei Altzeilen nicht — der Dialog nennt das in der Herleitungszeile. Die Kennzahl **H_T
wandert zusätzlich in `KennzahlenKatalog.cs` und den Bericht** (Konzept N1.6).

**E13 (16.09.2026): die Bezugsfläche heißt Nutzfläche.** Feldbeschriftung, Herleitungszeile,
Prüfregeln und Meldungstexte nennen sie **Nutzfläche** (beheizte Netto-Grundfläche), nicht mehr
Wohnfläche; Pflichtfelder des Imports sind Nutzfläche und Raumhöhe (Konzept N1.17). **E19 (16.09.2026,
Q11a):** die Spalte heißt künftig auch im Schema so — `Wohnflaeche` wird in `Tab_Gebaeude` und
`Tab_Gebaeude_STAMM` im Gebäudespalten-Schritt (1.6, mit dem Sichtneubau) zu **`Nutzflaeche`**
umbenannt, Werte 1:1, alle Leser und Schreiber auf den neuen Namen; `Wohnflaeche_gesamt` und die
Skalierungsspalten der Projektzuordnung (E8) bleiben; der Referenzlauf bleibt byte-gleich (Konzept N1.24).

**Zwei Prüfregeln hängen an der Tabelle** (Konzept 4.8): U-Werte zwischen 0,1 und 6 W/(m²K) — als
`Min`/`Max` am Feld **und** als Meldung beim OK — und `R_Rest,AW > 0`, also mittleres U der opaken
Bauteile unter 4,17 W/(m²K); die zweite kann erst die Hilfsklasse rechnen und meldet benannt beim
Speichern.

### 2.6 Fenster nach Orientierung

| Feld | Einheit | Bindung | Vorgabe | Pflicht | Regel |
|---|---|---|---|---|---|
| Fensterfläche Nord | m² | `FensterflaecheNord` | — | **ja** | ≥ 0 |
| Fensterfläche Süd | m² | `FensterflaecheSued` | — | **ja** | ≥ 0 |
| Fensterfläche Ost | m² | **`FensterflaecheOst` (neu)** | ½ der Summe Ost+West | nein | ≥ 0 |
| Fensterfläche West | m² | **`FensterflaecheWest` (neu)** | ½ der Summe Ost+West | nein | ≥ 0 |
| Summe Ost + West | m² | **gerechnet**, nur Anzeige | — | — | wird nach `Fensterflaeche_OstWest` mitgeschrieben |
| gesamte Fensterfläche | m² | **gerechnet**, nur Anzeige | — | — | = Summe der vier; geht in die U·A-Zeile „Fenster" |

**Eine Wahrheit im Dialog, zwei Leser im Kern** (Konzept 6.1): Der Dialog pflegt Ost und West,
schreibt aber die **Summe** nach `Fensterflaeche_OstWest` mit, damit `SolareGewinneC` auf dem
Tagesbilanz-Weg unverändert rechnet. Sind beide Felder leer, gilt „je die Hälfte der Summe" — das
ist die Vorgabe des **Kerns**, nicht des Dialogs; der Dialog schreibt `null`.

Damit wird die Plausibilitätsregel aus Konzept 4.8 („Summe der Fensterflächen =
`gesamte_Fensterflaeche`") **erfüllbar statt prüfbar**: Die Gesamtfläche ist gerechnet, nicht
eingegeben. Die Prüfung bleibt trotzdem als Wache im Kern — für Bestandsdatensätze und für den
IFC-Import. Heute leitet die Hülle sie aus drei Feldern ab
(`gesamte_Fensterflaeche = Süd + Ost + Nord`, `GebaeudeKatalogHuelle.cs:453-454`).

### 2.7 Der Wirt und der Bedarfsdialog

**`GebaeudeDialog.razor` bekommt drei Dinge:**

1. eine **Spalte „Rechenweg"** in der Projektliste (heute Wahl + Name, `:75-89`) — ohne sie ist E1
   für den Anwender unsichtbar; sie **bleibt dauerhaft**, weil es dauerhaft zwei Wege gibt (E23);
2. **zwei leise Kennzahlen** im Detailblock „Gebäude: Verbrauch" (`:154-195`): H_ges [W/K] und den
   Rechenweg als Text — bei einem Gebäude auf dem Altweg **„Tagesbilanz (Bestandsweg)"** statt des
   Produktausweises nach E10 (E20, E23) —, beide nur lesend wie die fünf vorhandenen Felder;
3. den Knopf **„Aus IFC-Datei übernehmen …"** in der Katalogleiste (`:136-148`) — **ab G4**, und
   nur mit Delegat: „Kein Delegat, kein Knopf" (`EPOS.UI/CLAUDE.md:47`).

**`GebaeudeBedarfDialog.razor` bekommt den Vergleich alt/neu** (Konzept 8.2; E20) — eine
Tabelle mit vier Spalten (Kennzahl | Tagesbilanz | VDI 6007 | Abweichung) über fünf Zeilen:
Wärmebedarf Heizung (MWh/kWh nach Einheitenwahl), Spitzenlast (Stunde), Spitzenlast (Tagesmittel),
95-%-Quantil der Stundenlast, Vollbenutzungsstunden. **Beide Spalten sind Auskünfte über
`GebaeudeBedarfCtrl`** — zwei Aufrufe desselben Controllers, keine zweite Rechnung. Dafür braucht
der Controller den vierten Parameter `modellErzwungen` und sein Ergebnis die sechs neuen
Kennzahlen; beides steht in 1.4 und gehört in den Merge G1+G2. **Der Vergleich bleibt dauerhaft**
(E23, 16.09.2026) — mit ihm `modellErzwungen` und das Feld `Vergleich` des DTO: Solange beide
Rechenwege wählbar sind, ist die Gegenüberstellung die Entscheidungshilfe des Anwenders. Ein
Gebäude auf dem Altweg trägt in diesem Dialog — wie im Bericht — die Zeile
**„Tagesbilanz (Bestandsweg)"**.

Dazu ein **zweites Bild „Raumtemperatur"** (Jahresverlauf Luft und operativ mit Sollwertband),
gezeichnet im Kern über `ChartRenderer` und hereingereicht als zweiter Delegat
`BildauftragRaumtemperatur` = `Func<Diagrammbereich?, byte[]?>` nach dem Muster von `Bildauftrag`
(`:126-134`), in einem **eigenen** `ChartBild` mit eigenem `BereichGewaehlt`/`Zurueckgesetzt` —
jedes Bild führt seinen eigenen Ausschnitt, und
[`Doku_Simulationsergebnis_Darstellung.md`](Doku_Simulationsergebnis_Darstellung.md) § 5.1 verlangt
Steuerzeile und Datenzoom an **jeder** Jahresganglinie; die Hülle reicht `Fenster(a, 8760)` durch,
der Kern schneidet über `Zugeschnitten`/`XAchseFenster` zu. Nur bei VDI 6007. Und drei neue Kennzahlen im Block „Kennzahlen"
(`:60-84`): Kühlbedarf (informativ), Stunden mit Kühlbedarf, mittlere Raumtemperatur in der Heizzeit.

```csharp
// GebaeudeBedarfDaten.cs — Erweiterung; KEINE 8 760 Werte im DTO (:15-17)
public string Modelltext { get; init; } = "";
public double? SpitzeStundeKw { get; init; }
public double? SpitzeTagesmittelKw { get; init; }
public double? SpitzeQuantil95Kw { get; init; }
public double? KuehlenergieMwh { get; init; }
public int?    KuehlstundenH { get; init; }
public double? MittlereRaumtemperaturC { get; init; }
public GebaeudeBedarfDaten? Vergleich { get; init; }   // der jeweils andere Weg; null = keiner
```

Alle neuen Felder sind **nullbar**: „Ein Reiter zeichnet nie ein vorbelegtes DTO als Ergebnis"
(`EPOS.UI/CLAUDE.md:82-84`); ohne Wert steht „—" (`GebaeudeBedarfDialog.razor:173`). Umgerechnet
wird weiterhin nur an der Anzeigekante über `Energieeinheit` (`:246-251`), die Leistung bleibt kW
(`:253-258`).

**`GebaeudeWohnflaecheDialog` wird zum Skalierungsdialog in VDI-Struktur** (E20, E19). Er bleibt der
Ort der Skalierung nach E8 — Projektfläche, Art der Angabe, Jahresnutzungsgrad und der Schalter für
die dezentrale Warmwasserbereitung —, aber er trägt die Begriffe des VDI-Wegs statt der alten:
Beschriftung und Hilfetext sprechen von der **Nutzfläche** des Projekts (E19, Spalte
`Nutzflaeche`; die Skalierungsspalten `Z_ProjektGebaeude.Wohnflaeche_Waermebedarf` und
`Einheit_Waermebedarf_Wohnflaeche` behalten dagegen Namen und Bedeutung, Konzept N1.24), und die
Herleitungszeile sagt, dass aus Fläche und Verbrauch der **Skalierungsfaktor** der Gebäuderechnung
entsteht. Die vier Größen dieses Dialogs liest **jeder** Rechenweg (Befund X 1.3); ein
Bestandswegabschnitt gehört hier deshalb nicht hinein. Der Dialog heißt in diesem Papier fortan
**Skalierungsdialog**; der Klassenname folgt, wenn die Hülle umzieht (2.8). `GebaeudetypDialog` ist
von E1/E2 nicht betroffen — seine Tagesverteilungen behalten ihren Rechenleser im Bestandsweg
dauerhaft (E23).

### 2.8 Die Hülle nach `EPOS.UI.Daten`

Konzept 8.3 verlangt den Umzug; er ist ohnehin für iOS fällig. Vorbild ist die Aufteilung von
`SimulationErgebnisHuelle` in fünf Dateien unter `EPOS.UI.Daten/Simulation/`. Aus den beiden
Windows-Hüllen (`GebaeudeHuelle.cs` 483 Z., `GebaeudeKatalogHuelle.cs` 554 Z.) werden je zwei
Hälften: nach `EPOS.UI.Daten/Bedarf/` wandern `GebaeudeHuelle.cs`, `GebaeudeKatalogHuelle.cs`,
`GebaeudeBedarfHuelle.cs` und die Naht `Gebaeudewege.cs`; in der Schale bleiben zwei
Fensterdateien. **Dort bleibt allein das Fenster**: `BlazorDialogForm<T>`, `ShowDialog`, `Size MASS`
und der `Geschlossen`-Rückruf (`GebaeudeHuelle.cs:51-100`, `GebaeudeKatalogHuelle.cs:43-86`) — genau
die Hälfte, die `EnableWindowsTargeting=false` nicht erlaubt.

**Ohne eine benannte Naht wandert die Hülle nicht.** `GebaeudeKatalogHuelle.Gaben(...)` trägt heute
einen `IWin32Window besitzer` (`:88-89`) und reicht ihn an `BrauchwasserGaben(...)` (`:123`, `:253`)
durch, das `BedarfsProfileHuelle.Gaben(besitzer, …)` ruft (`:279`); dasselbe gilt für
`GebaeudetypHuelle.Gaben()` (`GebaeudeHuelle.cs:154-155`). Beide Zielhüllen liegen in der Schale.
Die Bauform steht bereit — `EPOS.UI.Daten/Katalogwege.cs:23-31`: ein `static Func<…>`-Haken mit
folgenloser Vorbelegung, den Windows in `Program.Main` einhängt und iOS leer lässt („Kein Delegat
ist kein Knopf", `Katalogwege.cs:18-21`):

```csharp
internal static class Gebaeudewege
{
    /// Parametersatz der Brauchwasser-Profilliste des laufenden Projekts; null = kein Knopf.
    internal static Func<List<Z_ProjektBrauchwasserModel>,
                         IReadOnlyDictionary<string, object>> BrauchwasserGaben;

    /// Parametersatz der Gebaeudetypen-Verwaltung; null = kein Knopf.
    internal static Func<IReadOnlyDictionary<string, object>> GebaeudetypGaben;
}
```

Der `IWin32Window` fällt damit aus allen `Gaben`-Signaturen — er wurde ohnehin nur weitergereicht,
nie selbst benutzt (`GebaeudeKatalogHuelle.cs:88-123`). Zwei Wachen prüfen den Umzug:
`EPOS.UI.Tests/ParametersatzTests.cs` (jeder Gaben-Schlüssel trifft ein `[Parameter]`, `:12-38`,
Gegenwache am Gerät `WindowsFormsApplication1/Allgemein/Blazor/Parametersatzwache.cs`) und
`EPOS.UI.Tests/HuellenwegTests.cs` (kein modales Systemfenster im Blazor-Ereignis, `:10-57`). Dazu
die Regel der Wurzel-[`CLAUDE.md`](../../CLAUDE.md): Wer eine Hülle oder Naht der Schale anfasst,
prüft sie mit `-p:EnableWindowsTargeting=true` kompiliert. Der Kern bleibt unberührt —
`GebaeudeStammCtrl`, `ProjektGebaeudeCtrl` und `GebaeudeBedarfCtrl` liegen schon in
`EPOS.Kern/Controller/`.

### 2.9 Ressourcenschlüssel und Glossar

**63 neue Schlüssel** — 46 `GEBK_` (Editor), 4 `GEB_` (Wirt), 13 `GEBB_` (Bedarfsdialog); die
vollständige Liste mit deutschem und englischem Wert steht in Befund M, Abschnitt 5.10. Vier
Bestandsschlüssel werden **frei** und werden gelöscht, nicht umgewidmet: `GEBK_GRP_FLAECHEN`,
`GEBK_GRP_UWERTE`, `GEBK_GRP_WAERMEBRUECKEN`, `GEBK_GRP_ANSCHLUSS`.

Die Regeln dazu: beide `.resx` sind Pflicht, UTF-8 **mit** BOM und CRLF, Einträge alphabetisch;
danach **immer** `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben`. Die Namensordnung
ist streng (`GRP_*`, `LBL_*` mit Doppelpunkt, `FELD_*` ohne — für die Pflichtmeldung —, `SP_*`,
`BTN_*`, `MSG_*`, `HINWEIS_*`); `LBL_` und `FELD_` sind **bewusst zwei Schlüssel für dasselbe Feld**
(`GebaeudeKatalogDialog.razor:958-961`). Ab etwa zehn Texten ein **Bündel** statt einzelner
Parameter (`EPOS.UI/CLAUDE.md:22-24`) — die Schwelle ist klar überschritten.

**Das Glossar kennt die Gebäudehülle nicht.** In
[`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md) fehlen unter anderem Wärmebrücke,
Verschattung, Rahmenanteil, Bauteil, Bodenplatte, Keller, Randbedingung, Transmission,
Lüftungsleitwert, operative Temperatur, Kühlbedarf, Bauweise, Rechenmodell und Tagesbilanz.
**Ein Abschnitt „13. Gebäudehülle und Gebäudemodell" muss vor den en-US-Werten stehen** — sonst
entstehen zwei Übersetzungen desselben Begriffs, genau die Lage, die § 12 für die
`KONFIG_*`-Schlüssel eigens einfrieren musste. Vorschlagsliste: Befund M, Abschnitt 3.3. Frage U4.

**Eine Lücke im Standardbaustein:** `EPOS.UI/Standards/Zahlenfeld.razor` kennt `Wert`, `Einheit`,
`Min`/`Max`, `Nachkommastellen`, `Aktiv`, `Feldname`, `FehlerZustand` (`:42-93`), aber **keinen
`Platzhalter`** — nur `Textfeld` hat einen. Entweder eine `Herleitungszeile` je Feld (Muster
`BhkwDialog.razor:397-398`, „0 = Projektvorgabe ({0} %)", eingesetzt `:670-688`) oder ein
`Platzhalter`-Parameter am `Zahlenfeld`; der zweite Weg ist sauberer, rein additiv, zieht aber
`StilblattTests` nach sich. Frage U3.

### 2.10 Tests der Oberfläche

Heute prüfen **119 bunit-Fälle in fünf Klassen** (2 384 Zeilen) die Gebäudedialoge:
`GebaeudeDialogTests.cs` (40 Fälle), `GebaeudeKatalogDialogTests.cs` (33),
`GebaeudetypDialogTests.cs` (17), `GebaeudeBedarfDialogTests.cs` (14),
`GebaeudeWohnflaecheDialogTests.cs` (15). Das Muster: Klasse erbt `EposBunitContext`,
`JSInterop.Mode = Loose`, `IHilfeDienst` als Attrappe (`GebaeudeKatalogDialogTests.cs:33-37`),
**Kultur auf de-DE gepinnt** (`:17-19`), ein belegter Satz als statische Fabrik `Satz(name)`
(`:39-72`), eine `Aufbauen(...)`-Methode (`:74-93`), Hilfsgriffe `Knopf` (`:95-96`) und
`ReiterWaehlen` über `button[role=tab]` (`:98-99`).

**Neu: 34 Fälle** (24 Editor, 3 Wirt, 7 Bedarf); die 33 aus Befund M, Abschnitt 5.11, und ein
Zugang aus E20. Die tragenden: die Hülltabelle führt acht Zeilen und jede zeigt U·A; die
Fensterzeile ist nur lesbar; nur die Bodenplatte hat eine Randbedingung; H_T ist die Summe der acht
Zeilen; H_ve kommt aus Luftwechsel, Nutzfläche und Raumhöhe; **der gewichtete Wert trifft
`SpezWaermeverlusteC`** (gegen `BhkwPlan.cs:347-353`); **die Vorgabe ist VDI 6007** (leeres `Modell`
→ der Schalter „Rechenweg" steht auf „VDI 6007", E1); ein leeres Parameterfeld **speichert NULL,
nicht 0,3**; die Summe Ost+West wird gerechnet und mitgeschrieben; OK prüft, speichert und
schließt; Abbrechen schreibt nichts.

**Zwei Fälle ersetzen den Modellzustand des Dialogs** (E20). Der Bestandsentwurf wollte prüfen, dass
die sieben Parameterfelder im Tagesbilanz-Weg verschwinden; die Modellparameter stehen jetzt in
beiden Stellungen, es gibt also **keinen Modellzustand mehr zu prüfen**. An seine Stelle tritt der
**Bestandswegabschnitt**: (1) Bei `Modell = 'TAGESBILANZ'` erscheint der eingeklappte Abschnitt
„Tagesbilanz (Bestandsweg)" mit Gebäudetyp und der schreibgesperrten Fensterfläche Ost/West; bei leerem
`Modell` erscheint er **gar nicht** (kein graues Feld, kein leerer Abschnitt). (2) Beim Umschalten
des Rechenwegs bleiben **alle Modellparameterwerte stehen** und werden auch im Tagesbilanz-Weg
gespeichert — sie sind der Parametersatz, nicht der Rechenweg (2.4).

**Sieben bestehende Fälle sind anzupassen**: die beiden Feldbestandsfälle je Reiter (`:105-125`,
`:138-158`), `Ohne_Uebernehmen_bleibt_der_Satz_unberuehrt` (`:486`) und die Fälle um
`Beenden`/`Ueberschreiben` (`:174-217`, `:388-421`). Dazu die Fälle des Skalierungsdialogs
(`GebaeudeWohnflaecheDialogTests.cs`, 15 Fälle), soweit sie die Beschriftungen prüfen, die auf
„Nutzfläche" ziehen (2.7, E19).

**Die Rasterprobe gilt hier nicht — mit einer Einschränkung.** `Proben/Rasterprobe` misst **allein**
die virtualisierte `Katalogliste` (QuickGrid `Virtualize`) im echten Browser
([`Proben/Rasterprobe/LIESMICH.md`](../../Proben/Rasterprobe/LIESMICH.md)); die Gebäudedialoge
zeichnen schlichte `table.epos-raster`. Wer beim Umbau eine `.epos-raster*`-Stilregel anfasst — etwa
für die rechtsbündige Zahlenspalte oder die Summenzeile —, **zieht sie**; wer nur Markup hinzufügt,
nicht. **`Proben/ChartProben` ist dagegen Pflicht**, sobald das Bild „Raumtemperatur" entsteht
(Konzept 9; Regel in [`EPOS.Kern/CLAUDE.md`](../../EPOS.Kern/CLAUDE.md), Abschnitt „Bericht": ein
neuer Parameter bekommt eine Vorgabe, die das Bild byte-gleich lässt).

Ohne Zutun greifen außerdem `ParametersatzTests` (neue Gaben-Schlüssel), `SchliesskreuzWacheTests`,
`UeberlagerungstitelTests`, `StilblattTests` und `HuellenwegTests`.

### 2.11 Aufwand der Oberfläche

| Teil | Inhalt | Aufwand |
|---|---|---|
| **M-a** | `EPOS.Kern/Allgemein/Gebaeudehuellbilanz.cs` (Zeilen, H_T, H_ve, H_ges, gewichteter Zweig) samt Kernprobe gegen `SpezWaermeverlusteC` | 0,5 PT |
| **M-b** | 12 Felder in `GebaeudeKatalogDaten` (2.4), `AusModell`/`NachModell`, Ost/West-Summenschreibung, Umbenennung `Fensterflaeche_OstWest` | 0,5 PT |
| **M-c** | U·A-Tabelle und Summen; Wegfall der drei alten Gruppen und der zwei Reiter-2-Gruppen | 1,0 PT |
| **M-d** | Fenster Ost/West samt Summenanzeigen | 0,3 PT |
| **M-e** | Gruppe „Modellparameter (VDI 6007)": sieben immer sichtbare Felder, Schalter „Rechenweg", `Kellertemperatur`, Herleitungszeile, `Platzhalter` am `Zahlenfeld`; dazu der **freie Zahlenweg für `Bauweise`** — heute überschreibt `BauweiseNachfuehren()` (`GebaeudeKatalogDialog.razor:713-715`) vor jedem Speichern jeden freien Wert mit `Nutzfläche × 20/50/100` | 0,7 PT |
| **M-f** | **Ein Schreibweg**: `SpeichernLeiste` statt drei Knöpfen, Reiter-2-Stand auflösen, zehn Prüfregeln an einer Stelle | 1,0 PT |
| **M-g** | Wirt: Spalte „Rechenweg", zwei Kennzahlen im Detailblock | 0,3 PT |
| **M-h** | Bedarfsdialog: Vergleichstabelle, sechs Kennzahlen, zweites Bild, DTO | 1,0 PT |
| **M-i** | 63 Schlüssel in zwei `.resx`, Glossarabschnitt 13, `ResourceDesigner` | 0,7 PT |
| **M-j** | 34 neue bunit-Fälle, Anpassung von sieben bestehenden | 1,0 PT |
| **M-k** | Hülle nach `EPOS.UI.Daten`: vier Dateien, `Gebaeudewege`-Naht, `IWin32Window` heraus, Linux-Bau der Schale | 1,0 PT |
| **M-l** | **Bestandswegabschnitt „Tagesbilanz (Bestandsweg)" (E20, E23)**: eingeklappter Block mit zwei sichtbaren Feldern und dem gewichteten Wert der Wärmeleitwerte, Herleitungszeile am Schalter „Rechenweg", Ausweis „Tagesbilanz (Bestandsweg)" in Wirt und Bedarfsdialog, Skalierungsdialog in VDI-Struktur (2.7), die beiden Ersatzfälle aus 2.10 | **1,0 PT** |
| | **Summe** | **9,0 PT** |

**Das ist der Grund, warum der G1-Rahmen des Konzepts (6–10 PT für Schemaschritt, Namensleser,
Eingangsbauer, Verzweigung, Dialog, Hülle und Texte zusammen) nicht trägt.** Die Oberfläche allein
ist 9,0 PT; Kapitel 4 rechnet neu. **Was der Bestandswegabschnitt kostet, spart die entfallene
Modellzustandslogik zum Teil wieder ein** — die sieben bedingten Felder aus dem Bestandsentwurf
sind sieben unbedingte geworden (2.4).

---

## 3. Der IFC-Gebäudeimport (Stufe G4a)

### 3.1 Ziel und Grenzen

**Ziel:** aus einer IFC-Datei die Felder des Gebäudekatalogs vorbelegen — dieselben Zeilen, die E2
im Dialog sichtbar macht (U, A, U·A je Bauteilgruppe), jede mit **Herkunftskennzeichen** und
**Beleg**. Geschrieben wird in die **Projektzeile `Tab_Gebaeude`** über einen eigenen Delegaten
`UebernehmenInsProjekt(daten, idGebaeude)` des Gebäudedialogs. Der `Speichern`-Delegat des
Katalogeditors (`GebaeudeKatalogDialog.razor:399`) trifft dagegen `Tab_Gebaeude_STAMM`
(`GebaeudeKatalogHuelle.cs:318-337` ruft `GebaeudeStammCtrl.Insert`/`Overwrite`, `TABLE` `:13`,
`TABLE_PROJ` `:14`) und ist damit nur der zweite, **ausdrücklich zu wählende** Weg „als Katalogsatz
ablegen". Die Zielfelder liegen fertig in
`EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDaten.cs:29-122` (7 Flächen, 5 U-Werte, Bauweise,
3 ψ + 3 Längen, Luftwechsel).

**Grenzen, die in G4a bewusst nicht überschritten werden:**

- **Keine Geometrieableitung.** Fehlen die Quantity-Sets, bliebe nur `IfcExtrudedAreaSolid`. Für
  eine prismatische Wand wäre das rechenbar (`SweptArea` × `Depth`, Polygonfläche über die
  Trapezformel), es scheitert aber an `IfcBooleanClippingResult` (Giebelwände), an nicht
  prismatischen Wänden und an `IfcMappedItem` — und genau diese drei kommen in Bestandsmodellen
  regelmäßig vor. **In G4a nicht bauen**: der Leser meldet `IMP_IFC_PROT_KEINE_MENGEN` und lässt
  das Feld leer; die Ableitung bleibt G5.
- **Kein Mehrzonenmodell.** E7 vergibt es an ein eigenes Papier
  (`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`, in Arbeit); G4a schreibt in `Tab_Gebaeude`, nicht in
  `Tab_Bauteil`. Zonen werden gar nicht gelesen (3.5, Nr. 8).
- **Kein Ort, kein Klima.** `IIfcSite.RefLatitude/RefLongitude` sind
  `IfcCompoundPlaneAngleMeasure` (`LIST [3:4] OF INTEGER`) und von `IfcUnitAssignment` **nicht**
  betroffen; die Klimaregion wählt der Anwender im Projekt.
- **Kein Menüpunkt** (Konzept 8.4): der Import ist projektbezogen, kein Katalogimport, und
  erscheint als **Überlagerung** im Gebäudedialog — dasselbe Muster wie die Brauchwasserliste im
  Katalogeditor (`GebaeudeKatalogDialog.razor:401-405`). Einen **Hilfeschlüssel** bekommt der
  Dialog sehr wohl, über `IfcImportProfil.HilfeSchluessel` (3.3) — nur keinen Eintrag in
  `Menuetabelle.cs`.
- **Vorbedingung:** G1 und G2 müssen fertig sein, sonst importiert man in ein Tagesmodell, das die
  Daten nicht nutzt. G3 muss **nicht** fertig sein.

### 3.2 Der Ablauf

Das Muster steht dreifach im Bestand: `KatalogImportAblauf` (Lesen `:119` / Vorprüfen `:259` /
Ausführen `:321`, Klasse `EPOS.Kern/Allgemein/Import/KatalogImportAblauf.cs:81`),
`KatalogImportProfil.cs:196` (was den Lauf unterscheidet, steht als **Daten**),
`KatalogImportSatz.cs:28`, dazu `KlimaImportAblauf.cs:126` und `GanglinienImportAblauf.cs:173`.
Drei Regeln daraus gelten wörtlich: **der Ablauf zeigt nichts an** („Der Konfliktdialog ist kein
Rückruf, sondern eine Zäsur", `KatalogImportAblauf.cs:69-74`), **ein fehlerhafter Eintrag bricht
den Lauf nicht ab** (`:376-381`; nur `OperationCanceledException` beendet ihn), und **der Zustand
lebt im Ablauf, nicht in der Komponente** (`KatalogImportHuelle.cs:111-142`, `:210-233`).

| # | Schritt | Wer | Meldung / Abbruch |
|---|---|---|---|
| 1 | Datei wählen über `Dienste.Datei.DateiOeffnenAsync` | Hülle | `""` = abgebrochen, nichts geschieht |
| 2 | Größe gegen `IfcImportProfil.MaxBytes`: bei `.ifc`/`.ifcxml` die Dateigröße, bei `.ifczip` die **entpackte** Größe aus dem Zip-Verzeichnis (`ZipArchiveEntry.Length`); fehlt sie, gilt das Zehnfache der Dateigröße | Ablauf | `IMP_IFC_PROT_ZU_GROSS` (Fehler), Lauf endet |
| 3 | `MemoryModel.OpenRead(pfad, fortschritt)` — der zweite Parameter ist ein `ReportProgressDelegate` und optional; die Protokollsenke wird einmalig über `XbimServices.Current.ConfigureServices` belegt, die Meldungen des Laufs bleiben `PruefMeldung` | Ablauf, im Arbeitsfaden | `IMP_IFC_PROT_LESEFEHLER` mit `ex.Message` |
| 4 | `MemoryModel.GetSchemaVersion(pfad)` → `XbimSchemaVersion` | Ablauf | `Unsupported`, `Cobie2X4` **und `Ifc4x1`** → `IMP_IFC_PROT_SCHEMA_UNBEKANNT`; angenommen werden genau `Ifc2X3`, `Ifc4`, `Ifc4x3` — die drei Schemata, die `Xbim.IO.MemoryModel` mitbringt |
| 5 | Einheiten aus `IIfcProject.UnitsInContext.Units` | Ablauf | fehlende Längeneinheit → Warnung, Annahme Meter |
| 6 | Gebäude, Geschosse, Räume (`IIfcBuilding`, `IIfcBuildingStorey`, `IIfcSpace`) | Ablauf | 0 Gebäude → Fehler; 0 Räume → Warnung |
| 7 | Bauteile sammeln und gruppieren; außen/innen entscheiden | Ablauf | je verworfenem Bauteil eine Info mit Grund |
| 8 | **U·A-Zeilen bilden** (E2): A = Σ Bruttoflächen, U flächengewichtet | Ablauf | U außerhalb 0,1…6 → Warnung, Wert bleibt, Herkunft `Ifc` |
| 9 | Fenster in Sektoren N/O/S/W über den Azimut des Wirtsbauteils | Ablauf | Fenster ohne Azimut → Sammelposten, Warnung |
| 10 | Vorgaben je Baualtersklasse füllen | Ablauf | jede Vorgabe trägt Herkunft `Vorgabe` |
| 11 | `IfcImportSatz` bilden: Zielfeld, Wert, Herkunft, Beleg | Ablauf | — |
| 12 | **Zuordnungsdialog**: Tabelle, Haken je Zeile, OK/Abbrechen | Komponente | Abbrechen → `null`, **nichts wird geschrieben** |
| 13 | Plausibilität (Konzept 4.8) auf dem übernommenen Satz | Ablauf | benannte Fehler, Rückkehr in den Dialog |
| 14 | Schreiben über `UebernehmenInsProjekt` in `Tab_Gebaeude` (3.1) und Ablegen der Herkunftsdaten | Hülle | `IfcImportBilanz` mit den Zählern |

Die Schritte 2–11 laufen in **einem** Aufruf (`Lesen`), 13–14 in einem zweiten (`Uebernehmen`) —
dieselbe Zäsur wie `Vorpruefen`/`Ausfuehren`.

**Schritt 14 persistiert die Herkunft — sonst ist der Rückweg später verbaut.** Übernommen wird
nicht nur der Satz, sondern auch die Zuordnung **EPOS-Gebäude ↔ `IfcBuilding.GlobalId`** (später je
Zone ↔ `IfcSpace.GlobalId`) samt Name, SHA-256 und Zeitpunkt der Quelldatei, als Herkunftsdaten in
einer eigenen Tabelle. Ohne diese Zeilen ist die **Rückgabe angereicherter Dateien** — der
IFC-Export in seiner Stufe S2 — nicht mehr möglich, weil sich beim zweiten Lauf nicht mehr sagen
lässt, welche EPOS-Zeile zu welcher IFC-Entität gehört; Befund S nennt das ausdrücklich als
Anforderung an **diesen** Import
([`Befund S`](Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md),
Abschnitt 6). Der Spaltenvorschlag steht im
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md); hier wird kein Datenmodell
ausgeführt.

**Fehlerbilder sind Meldungen, keine Ausnahmen.** Im Importbestand gibt es **keine benannte
Ausnahmeklasse**; das Hausmuster ist `PruefStufe` (`SpeicherEngine/GanglinienPruefung.cs:55`:
Info / Warnung / Fehler) und `PruefMeldung` (`:74`: **Schlüssel + invariant formatierte Werte**, nie
Text — den Text holt erst die Oberfläche). Auch ein Lesefehler wird gefangen und gelegt, nicht
geworfen (`KatalogImportAblauf.cs:185-187`). Die dreizehn Schlüssel `IMP_IFC_PROT_*` samt deutschem
und englischem Wortlaut stehen in Befund N, Abschnitt 4.5; dieses Papier fügt vier hinzu —
`…_SEITE_UNBESTIMMT`, `…_PLATZIERUNGSART`, `…_EIGENSCHAFTSART` und `…_FLAECHENART_GEMISCHT` (3.4),
also **siebzehn**.

### 3.3 Die Klassen

Ort: `EPOS.Kern/Allgemein/Import/Ifc/`, Namensraum wie überall `WindowsFormsApplication1`.

| Datei | Inhalt |
|---|---|
| `IfcSchema.cs`, `IfcHerkunft.cs`, `IfcBauteilart.cs`, `IfcHimmelsrichtung.cs` | `enum IfcSchemaStand { Unbekannt, Ifc2x3, Ifc4, Ifc4x3 }`, `enum IfcHerkunft { Leer, Ifc, Vorgabe, Manuell }`, `enum IfcBauteilart { … }`, `enum IfcSektor { Nord, Ost, Sued, West, Ohne }` — **Werte, keine Anzeigetexte** (Regel `EPOS.Kern/Allgemein/Katalog/ImportKonfliktModell.cs:44-46`) |
| `IfcGebaeudeAbbild.cs` | das Zwischenmodell: was in der Datei steht, noch ohne EPOS-Semantik — `IfcBauteilAbbild` (Kennung, Art, `FlaecheBruttoM2`, `AzimutGrad?`, `NeigungGrad?`, `UWert?`, `GWert?`, `IstAussen?`, Geschoss, Schichten, **`Herkunftsbeleg`**), `IfcSchichtAbbild` (Baustoff, `DickeM`, λ, ρ, c_p), `IfcRaumAbbild`, `IfcGebaeudeAbbild` (mit `TrueNorthGrad?`, `MapConversionVorhanden`, `LaengenFaktorNachMeter`, `FlaechenFaktorNachM2`) |
| `IfcImportProfil.cs` | was den Lauf einstellt (Muster `KatalogImportProfil.cs:196`): `Dateifilter`, **`MaxBytes`**, `SektorBreiteGrad` (90), `UWertMin` (0,1), `UWertMax` (6,0), `HilfeSchluessel` |
| `IfcImportSatz.cs` | je **Zielfeld** eine `IfcFeldzeile` (Zielfeld, Gruppe, `Wert?`, Einheit, `Herkunft`, `Beleg`, `Uebernehmen`); der Satz trägt Schema, Gebäudename, `Baujahr?`, `BaualtersklassenIndex`, Zeilen, Meldungen und `NachKatalogdaten(GebaeudeKatalogDaten grundlage)` |
| `IfcImportAblauf.cs` | `Lesen(pfad, melder, abbruch) → int`, `Zuordnen(gebaeudeIndex, baualtersklasseVorgabe) → IfcImportSatz`, `static Pruefen(satz) → IReadOnlyList<PruefMeldung>` |
| `IfcZuordnungsModell.cs` | die Regeln des Dialogs, **oberflächenfrei** (Muster `ImportKonfliktModell.cs:45`): `HerkunftText`, `KopfText`, `ZeilenText`, `Pruefe` |

**Oberfläche:** `EPOS.UI/Dialoge/Bedarf/IfcZuordnungDialog.razor` mit `IfcZuordnungDaten.cs`
(`IfcZuordnungZeile`: Gruppe, Feld, `IfcWert` als formatierter Text, Beleg, `VorgabeWert`,
`Herkunft` als Anzeigetext aus `IfcZuordnungsModell`, `Uebernehmen`). Die Parameter folgen
`KatalogImportDialog.razor:241-260` (`DateiWaehlen`, `Lesen`, `GebaeudeWechseln`, `Uebernehmen`,
`Meldungstext`, `Fortschrittstext`, `Geschlossen` — `false` = Abbrechen); Fortschritt und Meldungen
über die vorhandenen Bausteine `<Fortschritt …>` (`:121`) und `<Warnbanner …>` (`:99`).

**Die Hülle liegt plattformfrei** in `EPOS.UI.Daten/Bedarf/IfcImportHuelle.cs` (Muster
`EPOS.UI.Daten/Kosten/SpotpreisImportHuelle.cs:32`), nicht in der Windows-Schale — so bekommt iOS
sie ohne zweite Fassung. **Vorbedingung:** Der Schreibweg des Gebäudedialogs muss dann schon
plattformfrei sein. Heute liegt er in der Schale
(`WindowsFormsApplication1/Views/Gebäude/GebaeudeKatalogHuelle.cs:318`), und `EPOS.UI.Daten/Bedarf/`
führt allein `BedarfErgebnisHuelle.cs`; der Umzug ist Teil **M-k** des Dialogumbaus (2.8, 2.11) und
damit mit G1+G2 erledigt, bevor G4a beginnt. Wird G4a wider Erwarten vor dem Umzug gebaut, ist der
IFC-Import auf iOS **benannt abzulehnen** (Muster `IFlottenPlaner`) — nicht still zu übergehen.

**Hilfe und KI-Anmeldung** gelten auch hier: Der Zuordnungsdialog trägt
`<InfoKnopf Schluessel=… Dialogname=… />` (Schlüssel aus `IfcImportProfil.HilfeSchluessel`), und der
Wirt meldet die Feldliste über `KiMaskenanmeldung.Fuer(name, () => Daten, KiHaken())` an; `Pruefen`
ist dieselbe Prüfung wie am OK-Weg. **Der Arbeitsfaden läuft dort über `Kulturweitergabe.Starten`**
(`SpotpreisImportHuelle.cs:61`, `:71`; API `Kulturweitergabe.cs:170`), **nicht** über `Task.Run`
wie in der Windows-Hülle (`KatalogImportHuelle.cs:229`): der Wächter prüft `EPOS.UI.Daten` mit
(`EPOS.Kern.Tests/ParallelitaetWacheTests.cs:52`), die Schale nicht.

### 3.4 Abbildungsregeln

Alle Typnamen sind Schnittstellen aus `Xbim.Ifc4.Interfaces`. Pset- und Quantity-Zugriff wird
**selbst geschrieben** über `IIfcObject.IsDefinedBy` →
`IIfcRelDefinesByProperties.RelatingPropertyDefinition.PropertySetDefinitions` (ein SELECT, das
eine **Menge** sein kann — die direkte Wandlung nach `IIfcPropertySet` verlöre ein
`IfcPropertySetDefinitionSet` und fiele bei Typmengen auf `IIfcElementQuantity` herein) → daraus
`IIfcPropertySet.HasProperties` bzw. `IIfcElementQuantity.Quantities`. Findet sich die Eigenschaft
dort nicht, wird der **TYP** gelesen (`IsTypedBy` → `RelatingType.HasPropertySets`) — dort tragen
Autorensysteme regelmäßig `Pset_WallCommon.ThermalTransmittance` und `Pset_DoorWindowGlazingType`.
Vorrang hat immer das Vorkommnis; der Beleg nennt, welche Quelle gegriffen hat. Die bequemen
`GetPropertySingleValue`/`GetElementQuantity` hängen an der IFC4-**Klasse**
`Xbim.Ifc4.Kernel.IfcObject` und wären schemagebunden.

**Der Mengensatz heißt in der Datei meist anders als in der Vorlage.** `Qto_…` ist der Name der
*Vorlage*; als Instanz trägt `IfcElementQuantity.Name` nach bSI-Festlegung `BaseQuantities`. Gesucht
wird deshalb über `IIfcElementQuantity.Name` ∈ { `BaseQuantities`, `Qto_<Klasse>BaseQuantities` }
(Groß-/Kleinschreibung egal), die Größe über `IIfcPhysicalSimpleQuantity.Name`. Welche Schreibweise
die KIT-Datei führt, wird an der Importprobe gemessen (3.7). Die Spalte „IFC-Quelle" unten nennt
jeweils die Vorlage.

| Zielfeld | IFC-Quelle | Regel | Rückfall | Herkunft |
|---|---|---|---|---|
| `WohnflaecheGesamt` | `IIfcSpace` + `Qto_SpaceBaseQuantities.NetFloorArea` | Σ über beheizte Räume | `GrossFloorArea` **für alle Räume gemeinsam oder für keinen** — eine gemischte Netto-/Bruttosumme wird nicht gebildet, sondern gemeldet (`IMP_IFC_PROT_FLAECHENART_GEMISCHT`); sonst leer → **Pflichtfeld** | Ifc / leer |
| `Raumhoehe` | `Qto_SpaceBaseQuantities.Height` | flächengewichtetes Mittel | `NetVolume / NetFloorArea`; sonst 2,5 m | Ifc / Vorgabe |
| (Volumen, nur Prüfgröße) | `…NetVolume` | gegen `Wohnfläche · Raumhöhe` halten | Abweichung > 20 % → Warnung | — |
| `FlaecheAussenwand` | `IIfcWall` + `Qto_WallBaseQuantities.GrossSideArea` | Σ über Wände mit `IsExternal = true`; **`GrossSideArea`, nicht `NetSideArea`** (Bruttomaß außen) | `Length × Height`; sonst leer | Ifc / leer |
| gesamte Fensterfläche | `IIfcWindow` + `Qto_WindowBaseQuantities.Area` | Σ über Fenster in Außenwänden | `Width × Height` | Ifc |
| `FensterflaecheNord/Sued/Ost/West` | Azimut des **Wirtsbauteils** | Sektor mit der nächsten Mitte in **IFC-Konvention** (N = 0°, O = 90°, S = 180°, W = 270°, im Uhrzeigersinn), Breite 90°; vor der Übergabe an den Eingangsbauer wird auf die **EPOS-Konvention gegen Süd** umgerechnet (`az_EPOS = az_IFC − 180`, auf (−180, 180] normiert; `KlimaImportAblauf.cs:135`) — ein Unit-Test hält beide Richtungen gegeneinander | ohne Azimut: gleichmäßig auf vier Sektoren, Warnung | Ifc / Vorgabe |
| `Dachflaeche` | `IIfcRoof` + `Qto_RoofBaseQuantities.GrossArea`; `IIfcSlab` mit `PredefinedType = ROOF` + `Qto_SlabBaseQuantities.GrossArea` | Σ; ist das Dach über `IIfcRelAggregates` aus Slabs zusammengesetzt, zählt **entweder** das Dach **oder** die Slabs | Grundfläche des obersten Geschosses | Ifc / Vorgabe |
| `Grundflaeche` | `IIfcSlab` (`BASESLAB`/`FLOOR`) im untersten Geschoss + `…GrossArea` | Σ | Wohnfläche / Geschosszahl | Ifc / Vorgabe |
| `SonstigeFlaechen` | `IIfcDoor` außen, `IIfcCurtainWall`, `IIfcPlate` außen | Σ | 0 | Ifc / Vorgabe |
| die fünf U-Werte | `Pset_WallCommon.ThermalTransmittance` usw.; gelesen wird `IIfcPropertySingleValue.NominalValue`, ein `IIfcPropertyBoundedValue` (so führt IFC4 `Pset_SpaceThermalLoad.AirExchangeRate`) über `SetPointValue`, sonst `UpperBoundValue`/`LowerBoundValue`, jede andere Eigenschaftsart benannt übergangen (`IMP_IFC_PROT_EIGENSCHAFTSART`) | **flächengewichtet je Gruppe:** `U = Σ(Uᵢ·Aᵢ)/ΣAᵢ`; fehlt er bei > 30 % der Gruppenfläche, gilt die Gruppe als „nicht aus IFC" | Vorgabe je Baualtersklasse | Ifc / Vorgabe |
| `Fensterdurchlassgrad` | `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` | flächengewichtet | Vorgabe je Klasse (0,75 alt / 0,6 / 0,5 neu) | Ifc / Vorgabe |
| `Bauweise` | `IIfcMaterialLayerSet` über `IIfcRelAssociatesMaterial` + `Pset_MaterialThermal`/`…Common` | raumseitige Schichten bis 10 cm: `C" = Σ ρᵢ·cpᵢ·dᵢ` [J/(m²K)], `/3600` → Wh/(m²K), `Bauweise = C"·Wohnfläche`; die Anzeige rastet über `Gebaeudebauweise.BauartAusBauweise` (`EPOS.Kern/Allgemein/Gebaeudebauweise.cs:42`) an den Schwellen **30 und 75** Wh/(m²K) ein (`:46-47`) — die Stufen 20/50/100 gehören zum Hinweg `BauweiseAusBauart` (`:63-65`). **Achtung:** `BauweiseNachfuehren()` (`GebaeudeKatalogDialog.razor:713-715`) schreibt vor jedem Speichern `Wohnfläche × 20/50/100` zurück und löscht jeden freien Wert; der Import setzt `Bauweise` deshalb erst, wenn der Editor den freien Zahlenweg hat (M-e, 2.11) — sonst nur die Bauart, Herkunft `Vorgabe`, und die Prüfregel „5 ≤ Bauweise/Wohnfläche ≤ 200" (Konzept 4.8) sähe über den Dialog nur 20, 50 oder 100 | ohne Schichten: Bauart „schwer" setzen und `BauweiseNachfuehren()` die Größe rechnen lassen (`Wohnfläche × 50`) — **nie** ein absoluter Wert (`Gebaeudebauweise.cs:66`) | Ifc / Vorgabe |
| `Baualtersklasse` | `Pset_BuildingCommon.YearOfConstruction` (**`IfcLabel`, also Text**) | erste vierstellige Zahl 1500…2100 aus dem Text („ca. 1965" → 1965), dann **A** vor 1919, **B** 1919–1948, **C** 1949–1957, **D** 1958–1968, **E** 1969–1978, **F** 1979–1983, **G** 1984–1994, **H** 1995–2000 (`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:112-119`) | ab 2001 ist die Klasse ein **Standard** (I = Niedrigenergie … U = BEG 40), kein Jahr — Feld bleibt, der Anwender wählt | Ifc / manuell |
| `Baujahr` (neue Spalte) | dieselbe Quelle | die gezogene Jahreszahl, `INTEGER`, NULL = unbekannt | — | Ifc |
| `Grundflaeche_Randbedingung` | Geschoss bzw. Raum unter der Bodenplatte | `IIfcBuildingStorey` mit `Elevation < 0` **oder** ein `IIfcSpace`, dessen Name auf Keller deutet → `KELLER` | `ERDREICH` | Ifc / Vorgabe |
| drei ψ und drei Anschlusslängen | **nicht in IFC** | ψ steht in keinem Standard-Pset; Längen ohne Geometriekernel nicht ableitbar | Vorgabe je Klasse **oder** leer — Frage U15 | Vorgabe / leer |
| `Luftwechselrate` | `Pset_SpaceThermalLoad.AirExchangeRate` | **nicht benutzen** — im Schema als `IfcPowerMeasure` typisiert (Schemafehler); der Zahlenwert ist nicht verlässlich zu deuten | Vorgabe 0,7 1/h (G2: 0,3 + 0,4) | Vorgabe |
| `Waermegewinne` | `Pset_SpaceOccupancyRequirements` | nur als **Vorschlag** angezeigt, nicht übernommen | Vorgabe je Gebäudeart (Wohnbau 5 W/m²) | Vorgabe |
| Sollwerte | `Pset_SpaceThermalRequirements` (in IFC 4.3 entfallen) | nur lesen, wenn vorhanden | Vorgaben des Grundlagensatzes | Ifc / Vorgabe |

**Einheiten.** Vor jeder Zahl steht der Faktor aus `IIfcUnitAssignment.Units`: bei `IIfcSIUnit` mit
`UnitType = LENGTHUNIT` entscheidet `Prefix` (`MILLI` → 0,001, `CENTI` → 0,01, ohne → 1,0). Für
`AREAUNIT` und `VOLUMEUNIT` gilt die **dort erklärte Einheit mit ihrem eigenen Prefix** —
unabhängig von `LENGTHUNIT`; ein fehlender Prefix heißt „ohne Prefix", nicht „wie die Länge". Nur
wenn der Typ ganz **fehlt**, wird er aus der Längeneinheit abgeleitet (Quadrat bzw. dritte Potenz),
und das wird gemeldet. Der Grund ist Praxis: Revit und Archicad erklären regelmäßig
`LENGTHUNIT = MILLI METRE` **und** `AREAUNIT = SQUARE_METRE` ohne Prefix — wer den Längenfaktor
quadriert, rechnet die Flächen um 10⁻⁶ falsch. `IIfcConversionBasedUnit` (Zoll, Fuß) trägt den
Faktor in `ConversionFactor` — im DACH-Raum selten, aber **benannt gemeldet** statt stillschweigend
als 1,0 genommen.

**Die Bemaßungsregel** (Außenbauteile nach Bruttomaß) gilt nach **E6 als Arbeitsannahme ohne
Normzitat**, bis sie in G1 aus VDI 2078 Abschnitt 6 oder DIN EN ISO 13789 belegt ist; aus
VDI 6020:2022 geht nichts in Code, Tests, Wiki oder Auslieferung.

**Azimut ohne Geometriekernel** ist reine Matrixmultiplikation: `IIfcProduct.ObjectPlacement` →
`IIfcLocalPlacement.RelativePlacement` (`IIfcAxis2Placement3D.RefDirection`, `Axis`) → über
`PlacementRelTo` aufwärts bis zum Weltsystem — sofern das Placement ein `IIfcLocalPlacement` ist;
`IIfcGridPlacement` und `IIfcLinearPlacement` werden benannt übergangen
(`IMP_IFC_PROT_PLATZIERUNGSART`), das Bauteil kommt in den Sammelposten ohne Azimut.

Die lokale x-Achse der Wand liegt nach Spezifikation in der Wandachse; senkrecht dazu in der
xy-Ebene liegen aber **zwei** Richtungen, und die Spezifikation legt nicht fest, welche außen ist
(`LayerSetDirection = AXIS2` ordnet nur die Schichten). Ein Vorzeichenfehler vertauscht N↔S. **Die
Seite bestimmt die Raumgrenze:** Die Außennormale zeigt von `IIfcRelSpaceBoundary.RelatingSpace`
weg, geprüft am Vorzeichen des Abstands zwischen Raum- und Wandplatzierung. Gibt es keine
Raumgrenze, bleibt der Azimut unbestimmt: der Leser meldet `IMP_IFC_PROT_SEITE_UNBESTIMMT` und
verteilt die Fensterfläche gleichmäßig.

Zum Schluss dreht `IIfcGeometricRepresentationContext.TrueNorth` (`IIfcDirection`, Vorgabe `[0,1]`)
das Ergebnis — **außer** wenn der Kontext eine `IIfcMapConversion` trägt
(`HasCoordinateOperation`): dann tritt deren Drehung `atan2(XAxisOrdinate, XAxisAbscissa)` an die
Stelle von `TrueNorth`, und `TrueNorth` bleibt unbeachtet (es wird **nicht** addiert). Gelesen wird
der Kontext mit `ContextType = 'Model'`.

### 3.5 Sonderfälle

| # | Fall | Regel in G4a |
|---|---|---|
| 1 | **Mehrere `IfcBuilding`** | ein EPOS-Gebäude je `IfcBuilding`; der Dialog zeigt eine Klappliste, übernommen wird je Lauf **eines**. Zuordnung über `IIfcRelAggregates` bzw. `IIfcRelContainedInSpatialStructure`; was sich keinem zuordnen lässt, kommt in einen Sammelposten mit Warnung. Frage U13 |
| 2 | **Geschosse** | `IIfcBuildingStorey.Elevation` liefert nur Summenbildung und Reihenfolge — **relativ** gelesen, nie als absoluter Wert (drei Höhenbezüge sind optional) |
| 3 | **Unbeheizte Räume** | `Pset_SpaceCommon.IsExternal = true` schließt aus; sonst entscheidet der Name (Treffer auf Keller, Garage, Carport, Dachboden, Speicher, Abstellraum, Technik, Schacht, Aufzug — englisch Basement, Garage, Attic, Shaft, Plant; Groß-/Kleinschreibung egal). **Der Dialog zeigt die Raumliste mit dem Haken**, damit die Regel sichtbar und korrigierbar ist |
| 4 | **Archicad-Raumgrenzen** | die Dateien schreiben trotz IFC4 die **Basisklasse** `IfcRelSpaceBoundary` und tragen das Merkmal nur in `Name='2ndLevel'` / `Description='2a'` — der Leser prüft **beides**. Fehlt `IsExternal`, entscheidet `InternalOrExternalBoundary` der Raumgrenze (`EXTERNAL*` = außen, `EXTERNAL_EARTH` zugleich `Grundflaeche_Randbedingung = ERDREICH` ohne Namensraten); steht dort `NOTDEFINED`, gilt die Zählregel: außen, wenn **genau eine** Raumgrenze mit `PhysicalOrVirtualBoundary = PHYSICAL` auf sie zeigt |
| 5 | **Fehlende Quantities** | Meldung statt Rückfall auf Geometrie (3.1) |
| 6 | **Gedrehte Gebäude** | ohne `TrueNorth` gilt `[0,1]` (Norden = +y) und **der Dialog sagt das** (`IMP_IFC_PROT_KEIN_NORDEN`) — ein falsch genordetes Modell vertauscht die Fenstersektoren, und niemand sieht es an den Zahlen |
| 7 | **IFC2x3 gegen IFC4** | der Leser arbeitet über die `IIfc*`-Schnittstellen und verzweigt an genau **zwei** Stellen: 2nd-Level-Erkennung (Nr. 4) und das in 4.3 entfallene `Pset_SpaceThermalRequirements` |
| 8 | **`IfcZone`** | `RelatedObjects` erlaubt `IfcZone` und `IfcSpatialZone`; wer nicht entschachtelt, zählt Flächen doppelt. In G4a werden Zonen **gar nicht** gelesen — die Räume hängen am Geschoss, das genügt für eine Einzonenrechnung |
| 9 | **Mehrschalige Wände** | zwei Wände mit derselben Achslage und derselben Raumgrenze sind **eine** Wand; ohne Achsauswertung Gruppierung über die gemeinsame Raumgrenze, sonst Warnung (`IMP_IFC_PROT_MEHRSCHALIG`) |
| 10 | **Fensterabzug** | hier kreuzen sich Bruttomaß-Grundsatz und EPOS-Feldstruktur: `A_Wand = Σ GrossSideArea − Σ A_Fenster − Σ A_Außentür`; wird das negativ, greift `NetSideArea`, sonst Warnung und `A_Wand = 0`. Frage U14 |

### 3.6 Plattform, Paket und Lizenz

**Das Paket.** Gemessen an den nuspec-Dateien (Abruf 15.09.2026):

| Paket 6.1.605 | Abhängigkeiten | Lizenz |
|---|---|---|
| `Xbim.IO.MemoryModel` | `Xbim.Common`, `Xbim.Ifc2x3`, `Xbim.Ifc4`, `Xbim.Ifc4x3` | CDDL-1.0 |
| `Xbim.Ifc` | zusätzlich **`Xbim.IO.Esent`** | CDDL-1.0 |
| `Xbim.Essentials` (Metapaket) | `Xbim.Common`, **`Xbim.Ifc`**, die drei Schemata, **`Xbim.IO.Esent`**, `Xbim.IO.MemoryModel` | CDDL-1.0 |

Alle tragen net10.0, net8.0 und netstandard2.0/2.1. **Der Kern nimmt genau eine Zeile** in
`Directory.Packages.props` (`<PackageVersion Include="Xbim.IO.MemoryModel" Version="6.1.605" />`)
und in `EPOS.Kern.csproj` nur `<PackageReference Include="Xbim.IO.MemoryModel" />`; die drei
Schemapakete kommen transitiv mit (`CentralPackageTransitivePinningEnabled` steht auf `false`,
`Directory.Packages.props:15`), `Microsoft.Extensions.Logging` ist bereits zentral geführt (`:30`)
und deckt den `ILoggerFactory`, den `MemoryModel` über `XbimServices` zieht. **`Xbim.Essentials` ist
damit nicht die richtige Zeile** — das ist die Präzisierung zu E3; die Klammer des Entscheids
(„`Xbim.Ifc2x3`, `Xbim.Ifc4`, `Xbim.Ifc4x3`, `Xbim.IO.MemoryModel`") trifft genau zu.

**Die Lizenzhinweisseite fehlt und muss entstehen.** `Setup/` führt keine Seite für
Fremdbibliotheken: `Setup/EPOS-Plan.iss:164-165` setzt `LicenseFile={#SetupDir}Lizenz.rtf`, `:330-331`
kopiert dieselbe Datei nach `{app}`. Für Datenlizenzen gibt es das Muster „Beipackzettel neben den
Daten" (`VDI-3805-Daten/Stromspeicher/LIESMICH_bslib.md`,
`VDI-3805-Daten/PV/LIESMICH_CEC_Inverters.md`). Vorschlag: eine neue
`Setup/Vorlage/Lizenzhinweise.txt` mit je Fremdbibliothek Name, Version, Lizenz, Copyright-Vermerk
und **dauerhaftem Verweis auf den Quelltext** — erster Eintrag xBIM (CDDL-1.0,
`github.com/xBimTeam/XbimEssentials` bzw. die NuGet-Quellpakete; **CDDL § 3.1 verlangt, dass dieser
Verweis dem Empfänger mitgeteilt wird**), dazu die schon ausgelieferten Fremdanteile; eine
`Source:`-Zeile in `EPOS-Plan.iss` nach dem Muster `:330-331`; als Pflegeweg eine Zeile je
ausgelieferter `PackageVersion`. **Ohne diese Seite ist der IFC-Import nicht auslieferbar.**
Frage U10. Und: **nie forken, nie patchen** (E3) — dann gibt es nichts offenzulegen.

**iOS — drei Punkte.**

- **Dateifilter:** Nachzutragen sind `[".ifcxml"] = "public.xml"` und
  `[".ifczip"] = "public.zip-archive"` in `EPOS.iOS/Dienste/Dateifilter.cs:25-44` — beide Kennungen
  sind dort schon für `.xml` (`:27`) und `.zip` (`:30`) geführt. Für `.ifc` gibt es keine
  registrierte Typkennung; dort bleibt `public.data` (`:22`) die richtige Antwort — mit Kommentar
  wie im `.lic`-Fall (`:37-40`). Gewählt
  wird über `DateiOeffnenAsync` (`IDateiDienst.cs:107`, iOS-Fassung `IosDateiDienst.cs:146`, ohne
  Oberfläche `KeineDateiwahl.cs:17-20` → `""`); dass der Wähler **asynchron** sein muss, begründet
  `IDateiDienst.cs:74-105` (ein synchroner Wähler pumpt unter Windows eine verschachtelte
  Nachrichtenschleife, und auf iOS geht er vom Hauptfaden gar nicht erst auf).
- **Speicher:** `MemoryModel` hält das ganze Modell im Arbeitsspeicher; Faustzahl 10–20 MB je MB
  STEP-Text, 50 MB Datei wären also 0,5–1 GB — auf einem iPad zu viel. Vorschlag **50 MB Windows /
  20 MB iOS** über eine Eigenschaft von `IfcImportProfil`, die die Hülle je Plattform belegt. **Die
  iOS-Zahl ist geschätzt und in G4-8 zu messen.** Frage U11.
- **Trimming:** `EPOS.iOS.csproj` setzt weder `TrimMode` noch `PublishTrimmed`, und es gibt
  **keinen `TrimmerRootDescriptor`** im Repositorium. Der CI-Lauf baut für den **Simulator**
  (`.github/workflows/ios.yml:123-127`, Begründung `:122`: „Fuer den Rauchtest reicht JIT ohne
  Linker"), und dort wird nie getrimmt; **Gerätebauten** trimmen dagegen immer mit
  `TrimMode=partial`, unabhängig von der Konfiguration, und dabei bleiben die vier xBIM-Assemblies
  unangetastet, weil sie nicht als trimmbar markiert sind. Das Risiko ist benannt:
  `ExpressMetaData` ruft `module.GetTypes()`. Es wird deshalb **nichts** voreingestellt: **G4-8
  misst einen Gerätebau**; erst wenn `ExpressMetaData` dort Typen vermisst, kommt
  `EPOS.iOS/Pruefung/XbimRoots.xml` mit `preserve="all"` für die vier Assemblies dazu, eingebunden
  über `<TrimmerRootDescriptor …>`. Der Nachweis kostet einen macOS-Lauf (**zehnfaches
  Kontingent — beim Anwender zu erfragen**). Frage U16.

### 3.7 Tests und Importprobe

**Wie Importproben organisiert sind:** `Referenzlaeufe/Importproben/` (27 Dateien, rund 300 KB,
gewöhnliche Blobs, **kein** LFS); der Ordner „gehört zum Testbestand und wird nie gelöscht"
(`Referenzlaeufe/LIESMICH.md:128-129`). Eine LIESMICH im Ordner selbst gibt es **nicht** — die
Quellenvermerke stehen in den Fachkonzepten. Tests suchen den Ordner **aufwärts vom Laufordner**
(`EPOS.Kern.Tests/KatalogImportTests.cs:50-68`).

**Vorschlag:** `Referenzlaeufe/Importproben/AC20-FZK-Haus.ifc` (KIT/IAI, 2,5 MB, IFC4/Archicad 20)
aufnehmen — der Ordner wächst um den Faktor 9, bleibt unter 3 MB und braucht kein LFS — und eine
**neue** `LIESMICH_Importproben.md` mit den Quellenvermerken aller Proben anlegen; für die KIT-Datei
im vorgegebenen Wortlaut „Institut für Automation und angewandte Informatik (IAI) / Karlsruher
Institut für Technologie (KIT)" (Nutzung uneingeschränkt, Namensnennung für Veröffentlichungen
vorgeschrieben). RWTH- und bim2sim-Dateien **nicht** aufnehmen (Konzept Q12).

**Erwartete Werte der Importprobe** (beim Bau des Tests an der Datei nachzumessen; hier stehen die
belegten): Schema `XbimSchemaVersion.Ifc4`; **1** `IIfcBuilding`; **8** `IIfcSpace`; **81**
Raumgrenzen, sämtlich als **Basisklasse** mit `Name='2ndLevel'` — der Test prüft ausdrücklich, dass
der Leser sie **nicht** über den Entity-Typ sucht; **33** `ThermalTransmittance`; 7 Vorkommen
`Pset_SpaceThermalRequirements`; Längeneinheit aufgelöst; Herkunft je Zeile (Wohnfläche, Raumhöhe,
Außenwandfläche und U-Werte `Ifc`, Wärmebrücken und Luftwechsel `Vorgabe`).

**Vierzehn Unit-Tests ohne Datei** (`EPOS.Kern.Tests`, keine Testdatenbank) — Liste in Befund N,
Abschnitt 4.6; die tragenden: die Sektorzuordnung trifft die vier Mitten (Grenze festgelegt:
**aufsteigend zum größeren Sektor**), der Azimut dreht mit `TrueNorth`, bei `MapConversion` wird
`TrueNorth` **nicht** addiert, der U-Wert einer Gruppe ist flächengewichtet (10 m²/0,5 und 30 m²/1,5
ergeben 1,25, nicht 1,0), das Baujahr wird aus Text gelesen („ca. 1965" → 1965, „Altbau" → keins),
die Baualtersklasse folgt dem Baujahr, die Fensterfläche wird von der Wandfläche abgezogen
(100 − 15 − 2 = 83), das Größenlimit greift, ein Abbruch wirft `OperationCanceled`. Dazu die
bunit-Fälle in `IfcZuordnungDialogTests.cs`: Abbrechen liefert `false` und ruft `Uebernehmen` nie;
ein abgehakter Haken hält die Zeile aus dem Satz; die Herkunftstexte kommen aus
`IfcZuordnungsModell`, nicht aus der Komponente.

**Abnahme G4a:** Kern-Filter grün und Importprobe bestanden; **Referenzlauf unverändert** (der
Import schreibt nur auf Zuruf des Anwenders — und der Schemaschritt für `Baujahr` ist
ergebnisneutral); Windows-Sichtabnahme (Datei wählen, Zuordnung prüfen, OK, Werte im Katalogeditor);
iOS-Lauf **nach Rückfrage** mit Release-Bau, Trimming-Nachweis und der KIT-Datei im Prüfmodus;
`SqlDialektPruefer` nur, wenn der Schemaschritt dazukommt.

### 3.8 Aufwand und Reihenfolge innerhalb G4

| Teil | Inhalt | Aufwand |
|---|---|---|
| **G4-1** | Paketzeile, `EPOS.Kern.csproj`, Lizenzhinweisseite im Setup, Bau auf ubuntu und Windows grün | 0,5–1 PT |
| **G4-2** | Abbild, Profil, Einheitenauflösung, Schemaerkennung, Öffnen, Räume und Bauteile sammeln — **ohne** Zuordnung | 3–4 PT |
| **G4-3** | Azimut aus der Placement-Kette, `TrueNorth`, `MapConversion`; Tests ohne Datei | 2–3 PT |
| **G4-4** | Zuordnung nach E2: U·A-Zeilen, Sektoren, Fensterabzug, Bauweise aus Schichten, Baujahr → Klasse; Satz, Modell, Plausibilität | 3–4 PT |
| **G4-5** | Vorgabetabelle je Baualtersklasse (21 Klassen × 5 U-Werte + g), Quelle und Lizenz geklärt (U12) | 1–2 PT |
| **G4-6** | Dialog + DTO + `IfcImportHuelle`, Knopf in beiden Gebäudedialogen, Texte in beiden `.resx`, `ResourceDesigner` | 2–3 PT |
| **G4-7** | Importprobe, Quellenvermerk, Unit-Tests, Dialogtests | 1–2 PT |
| **G4-8** | iOS: Dateifilter, Größenlimit **gemessen**, **Gerätebau** zum Trimming-Nachweis (erst danach ein `TrimmerRootDescriptor`, wenn er gebraucht wird), Lauf nach Rückfrage | 1–2 PT |
| | **Summe G4a** | **13,5–21 PT** |
| **G4b** | Anbindung an `Tab_Bauteil`/`Tab_Bauteilschicht` aus G3: je Bauteil eine Zeile mit Schichten, Azimut, Neigung, Herkunft `Ifc` — statt Nachmultiplikation | +4–6 PT |
| **G4c** | **gbXML-Import** (Pflicht nach E9): Lesemodell und Einheiten, Aggregation auf das Zonenmodell, Zuordnungsdialog, Beispieldateien — LINQ to XML, Versionswert `6.01` (Befund R) | **17–28 PT** — die Zahl führt das [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md), Kapitel 10, seit die Persistenz der Zuordnung (Schemaschritt S-F) dorthin vorgezogen ist |

Reihenfolge G4-1 → G4-2 → G4-3 → G4-4 → G4-5 → G4-6 → G4-7 → G4-8; G4-3 **vor** G4-4, weil die
Sektorzuordnung am Azimut hängt. G4-5 kann parallel laufen, sobald die Zielfelder stehen. G4b erst
nach G3 und erst, wenn G4a im Feld war. Der Schemaschritt für `Baujahr` bekommt die nächste freie
Nummer nach G1/G2 (`SchemaStand.cs:93` steht auf 76).

**Vorbedingung von außen:** Der Schreibweg des Gebäudedialogs liegt zu Beginn von G4a schon
plattformfrei in `EPOS.UI.Daten` — das erledigt M-k im Dialogumbau (2.8, 2.11). Ohne diesen Umzug
ist der IFC-Import auf iOS benannt abzulehnen (3.3).

**Zur Reihenfolge von G4a und G4c ist nichts entschieden.** Befund R empfiehlt, den gbXML-Import
**vor** dem IFC-Import zu bauen (einfacheres Format, geringerer Aufwand, dieselbe Zielstruktur);
das ist die offene Anwenderfrage **D1** des
[Datenaustauschkonzepts](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) und wird hier nur genannt,
nicht entschieden. Die beiden **Exporte** nach gbXML und IFC sind nach E9 Stufe **G7** und stehen
ebenfalls in jenem Papier.

---

## 4. Reihenfolge und Abnahme je Stufe

| Stufe | Inhalt | Abnahme | Aufwand |
|---|---|---|---|
| **G0 — Löser** | `Zonenmodell2K`, `ErsatzparameterRC`, Diskretisierung, ideale Regelung; Normfälle und Rechenproben (1.3, 1.9); Testfall 11 lösen, Testfall 6 klären, α_kon je Bauteil, Band-Prüfregel, Vorlaufkonvergenz. **Keine Datenbank, kein Aufrufer** | Kern-Filter grün; **elf der zwölf Normtestfälle** im Band ± 0,15 K / ± 1,5 W nach E10 (Druckrundung), Testfall 11 in zwei Umschaltstunden um 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung) — **lokal**, in der CI schweigend (1.9). Referenzlauf **unberührt**, weil keine Zeile des Bestandswegs angefasst wird | 2–4 PT |
| **GB — Bestandsbefunde** | `_prevRoomTemp` als Instanzzustand (`BhkwPlan.cs:51`, `:433`), Warnungen statt stiller Fehlgriffe in der Ferienmaske (`SimulationWaermebedarf.cs:689-733`) **und die nicht nachgeführte `Ferien_Absenkung` der Jahresschleife** (`:845-850`, Befund X 3.4), `Bauweise` 10576 auf 15 200 Wh/K, **vierte Einfrierregel** in `Referenzlaeufe/LIESMICH.md` und `CLAUDE.md`; dazu die 100-Gebäude-Grenze (U9). **GB läuft vor der Verschiebung** (E20): Das verschobene Modul soll der geprüfte Stand sein | **1008 und 1039 ändern sich** — eigener Einfrierschritt: Lauf, Vergleich, Begründung, grüner CI-Lauf. Diese Basis ist die **letzte reine Bestandsbasis** | 1–2 PT |
| **M2 — Umbenennung** | `Fensterflaeche_Ost` → `Fensterflaeche_OstWest`, 15 Stellen (`GebaeudeModel.cs:20`/`:76`, `ProjektGebaeudeModel.cs:26`/`:88`, `GebaeudeCtrl.cs:66`, `GebaeudeStammCtrl.cs:291`/`:358`, `ProjektGebaeudeCtrl.cs:56`, `SimulationWaermebedarf.cs:752`/`:756`/`:822`/`:826`, `GebaeudeKatalogHuelle.cs:364`/`:443`/`:453`) | gegen die GB-Basis **byte-gleich**. Eigener Merge, weil jede dieser Zeilen in `SolareGewinneC` mündet | 0,5 PT |
| **M3 — Schema** | Schritt 77 (+78 verschmolzen, U5), `GebaeudeSchema.cs`, Sichtneubau, Namensleser statt `row[0…57]` — der Leser und `SQL_VIEW_NEU` schreiben die acht nicht-ASCII-Bezeichner **buchstabengetreu** (`k_Wert_Außenwand`, `Flaeche_Außenwand`, `WBVK_Anschluß_*`, `Abmessung_Anschluß_*`), Umlautregel [`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md) § 6.1, und der Feldbestandstest prüft sie namentlich; `DbWerte`, Katalogkopie NULL-erhaltend, `TestDatenbank` nachziehen | gegen die GB-Basis **byte-gleich** (Spalten bleiben NULL, kein Leser rechnet damit). Die Probe ist der Sichtneubau: `ProjektGebaeudeCtrl` liefert alle 58 Bestandsfelder unverändert; dazu `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite` grün | 1,5–2 PT |
| **M4 — Klimaspalten** | eigener Schritt: `Gegenstrahlung`, `Windgeschwindigkeit`, `Luftfeuchte` in `Tab_Solar(_STAMM)`; `TmyHourlyData`, `SaveTmyData`, `SolardatenCtrl` | gegen die GB-Basis **byte-gleich**; die Importprobe gegen `pvgis_tmy_stuttgart_72h.json` liefert dieselben `Sol_*` wie bisher; `SqlDialektPruefer` grün | 0,5–1 PT |
| **G1.0 — Trennung der Rechenwege** (erster Schritt von G1, E20) | Modul `Simulation/Altweg/` anlegen und den Tagesbilanz-Weg **Zeichen für Zeichen** hineinschieben (drei Methoden aus `SimulationWaermebedarf.cs`, vier Physikfunktionen aus `BhkwPlan.cs` samt Vortemperatur; `BhkwPlan.cs` bleibt als Datei, seine neun Vektorhelfer wandern nicht); Fassade und Weiche in `HeizwaermeEinesGebaeudes`, **modellfreier Vorbereitungsschritt**, `IGebaeudeRechenweg`, Wache „der VDI-Weg referenziert nichts aus `Altweg/`" (1.9); `BhkwPlanRueckgabeTests` nachziehen | gegen die GB-Basis **byte-gleich** — die Verschiebung ist ergebnisneutral, **kein Einfrieranlass**. Eine Abweichung ist ein Fehler der Verschiebung; nach der Anbindung des VDI-Wegs wäre sie nicht mehr von der Modellwirkung zu trennen. Kern-Filter grün, `SqlDialektPruefer` unberührt | **3–5 PT** |
| **G1 + G2 — das Modell und seine Darstellung** | `GebaeudeModellEingang`, `GebaeudeModellErgebnis`, das Modul `Simulation/Gebaeude/` hinter der Weiche (G1.0), Vorlauf 30 Tage, Plausibilitätsprüfungen, Ergebnisexport; Dialogumbau in VDI-Struktur (Kapitel 2, **9,0 PT** einschließlich Bestandswegabschnitt), Hülle nach `EPOS.UI.Daten`, 63 Texte; Raumtemperatur-Bild, Kühlbedarf, drei Spitzenwerte, Vergleich im Bedarfsdialog, Sommerlüftung und Infiltration/Nutzerlüftung, Wiki-Seite | **alle dreizehn Referenzprojekte ändern sich** (+7 bis +33 %), drei neue CSV je VDI-Gebäude — **Basis vollständig neu einfrieren** (E1/Q14). Dazu der **Rückweg-Test**: er läuft auf einer **Arbeitskopie** (`Referenzlaeufe/Arbeitskopie/`, gitignoriert), die `Gebaeude_Modell = 'TAGESBILANZ'` setzt, und hält jedes Referenzprojekt gegen die GB-Basis; die eingefrorene Testdatenbank bleibt unberührt, der Schalter dafür ist ein eigener Modus des Referenzlaufs. **Er bleibt dauerhaft** (E23). `ChartProben` grün; Sichtabnahme Windows; Kriterien Konzept 10.4 (2)–(4); **Strahlungsweg: VDI 2078 Testbeispiel 7.1/7.2 (Typ 2, ± 0,2 °C / ± 5 W), aus den gedruckten Klimaparametern Anhang A1/B1 ohne TRY** (Konzept N1.9); **Logbuch-Eintrag entworfen, Versionsnummer beim Anwender erfragt**, Wiki-Seite „Gebäudemodell VDI 6007" und die geänderte Seite „Gebäude" im nächsten gebündelten Upload | 16–22 PT |
| **G3 — Bauteilkatalog** | `Tab_Baustoff_STAMM`, `Tab_Bauteil`, `Tab_Bauteilschicht`, Baustoffdialog samt **Zeile in `EPOS.UI/Bausteine/Menuetabelle.cs`** unter Administration (das Menü ist Daten; kein Untermenü mit nur einem Punkt), Bauteilweg mit Kettenmatrix-Reduktion, geneigte Fenster, echte Hülle statt Nachmultiplikation | Reduktion trifft die Normwerte der Testräume; Bauteilweg = Klassenweg im Grenzfall gleicher U und C | 8–12 PT |
| **G4a — IFC-Import** | Kapitel 3: Paket und Lizenzseite, Leser, Azimut, Zuordnung, Vorgabetabelle, Dialog und Hülle, Importprobe, iOS | Importprobe bestanden; **Referenzlauf unverändert**; Windows-Sichtabnahme; iOS-Lauf nach Rückfrage | 13,5–21 PT |
| **G4b — IFC auf Bauteilebene** | je Bauteil eine Zeile mit Schichten, Azimut, Neigung, Herkunft `Ifc` in `Tab_Bauteil` | nach G3 und erst, wenn G4a im Feld war | 4–6 PT |
| **G4c — gbXML-Import** | **Pflicht nach E9** (Konzept N1.13): Lesemodell und Einheiten, Aggregation auf das Zonenmodell, Zuordnungsdialog mit Herkunft je Feld, Beispieldateien; LINQ to XML statt `XmlSerializer`, Versionswert `6.01` | Importprobe bestanden; **Referenzlauf unverändert**; Reihenfolge zu G4a offen (D1, 3.8) | **17–28 PT** nach [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md), Kapitel 10 (einschließlich der dorthin vorgezogenen Persistenz der Zuordnung) |
| **G5 — Geometrieableitung** | eigene Auswertung von `IfcExtrudedAreaSolid` und Placement-Kette, Öffnungsabzug | nur bei Bedarf aus der Praxis | 30–60 PT |
| **G7 — Exporte** | gbXML- und IFC-Export (E9) — eigenes Papier: [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md). Dort steht auch der **Gebäudebetrachter (E11)**: ein Zonengeometrie-Modell im Kern, 2D-Grundriss je Geschoss mit G6c, schematische Körper mit G7b (Nachtrag 1; Konzept N1.16) | dort | dort |
| **~~GA — Altweg entfernen~~** | **Entfällt (E23, 16.09.2026).** Der Tagesbilanz-Weg bleibt **dauerhaft** als eingefrorener Bestandsweg: Modul `Simulation/Altweg/`, Weiche, `IGebaeudeRechenweg`, Trennungswache, Schalter „Rechenweg", Abschnitt „Tagesbilanz (Bestandsweg)", Spalte „Rechenweg" im Wirt, Vergleich alt/neu samt `modellErzwungen`, Ausweis „Tagesbilanz (Bestandsweg)", die vier Altweg-Spalten in beiden Gebäudetabellen, das Referenzprojekt auf dem Altweg (A15) und der Rückweg-Test — alles bleibt. Es gibt keinen Schemaschritt mit `DROP COLUMN` und keinen weiteren Einfrierschritt. Offen bleibt allein ein **Aufräumpunkt ohne Stufe**: die leserlosen Spalten `WW_Bedarf` und `Waermebedarf` | — | **0 PT** (5–8 PT fallen weg) |

**Summen:** G0 + GB 3–6 PT; **G1.0 (Trennung) 3–5 PT**; bis einschließlich G1+G2
**24,5–36,5 PT**; bis G3 32,5–48,5 PT; bis G4a 46–69,5 PT. **Die 5–8 PT der früher vorgesehenen
Stufe GA fallen weg** (E23, 16.09.2026); keine Summe oben enthielt sie. Der gbXML-Import (G4c) kommt
mit **17–28 PT** hinzu — **er steckt in keiner der Summen oben**. Diese Zahl und die Exporte (G7)
rechnet das [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) in Kapitel 10;
sie schließt die Persistenz der Zuordnung ein (Schemaschritt S-F, zwei Tabellen), die dort zu G4c
gehört.

**Warum das über dem Konzeptrahmen liegt.** Konzept 11 veranschlagt G1 mit 6–10 PT und G2 mit
3–5 PT. Allein die Oberfläche ist gemessen 9,0 PT (2.11), und dazu kommen vier Dinge, die das
Konzept in dieser Form nicht kannte: die **Trennung der Rechenwege samt byte-gleichem Nachweis**
(G1.0, E20, 3–5 PT), der erste Sichtneubau des SQLite-Zweigs samt Namensleser (1.6), die
NULL-erhaltende Katalogkopie (1.6) und die Vorrichtung für nicht ausgelieferte Normzahlen (1.9).
Die Aufwände sind Größenordnungen für Entwicklung **und Nachweis**; Agentenarbeit verkürzt die
Kalenderzeit, nicht die Prüfzeit.

**Was zwischen GB und G1+G2 gilt:** jeder Merge läuft gegen die GB-Basis und muss `GESAMT: PASS`
melden — **G1.0 ausdrücklich byte-gleich**. **Nur GB und G1+G2 frieren neu ein** (E23). Wer M3 und G1
zusammenlegt, kann hinterher nicht mehr sagen, ob eine Abweichung vom Schema oder vom Modell kommt —
genau die Trennung, die E4 für GB begründet; dasselbe gilt für G1.0 und die Anbindung des VDI-Wegs.

**Drei Handgriffe, die zur Papierführung gehören:** **Dieses Papier und jeder Befund** brauchen je
eine Indexzeile in [`Dokumentation/LIESMICH.md`](../LIESMICH.md), sonst ist
`DokumentationLinkWacheTests.Der_Index_nennt_jedes_Papier` (`:170`) rot — dieses Papier steht dort
auf `:55`, die Befunde A–S auf `:56-74`; jedes weitere Papier zieht seine Zeile nach — **Befund X
(E20) ebenso wie Befund W**. **Je Stufe** kommt
eine Zeile in [`Status_Gebaeudesimulation_VDI6007.md`](Status_Gebaeudesimulation_VDI6007.md) dazu,
der ausführliche Block dazu als Protokoll unter `Dokumentation/ueberholt/Protokolle/`. Und
`EinheitenWacheTests.Simulationsklassen` (`:201-206`) bekommt den Eintrag
`Gebaeude/GebaeudeModellErgebnis.cs` — **mit Unterordner** (1.4, 1.9) — in demselben Merge, in dem
die Klasse entsteht.

---

## 5. Fragen mit Empfehlung

Nur **neue** Fragen; Q1–Q23 des Konzepts sind dort beantwortet oder durch **E1–E21** entschieden
(E9 Austauschformate, E10 Druckrundung, E19 Nutzfläche, E20 Trennung der Rechenwege, E21
Kältebedarf). **Q24** — der Zeitpunkt der Stufe GA — ist mit **E23** beantwortet: **nie**, die
Stufe entfällt; **Q25** (ihr Schemaschritt) ist damit gegenstandslos. Beide stehen im Konzept,
nicht hier.

| Nr. | Frage | Empfehlung |
|---|---|---|
| **U1** | Der Katalogeditor bekommt **einen** Schreibweg (OK/Abbrechen statt „Überschreiben", „Speichern"/„Speichern unter", „Beenden" und „Werte übernehmen", `GebaeudeKatalogDialog.razor:337-340`, `:360-369`). Das ist eine für den Anwender **sichtbare** Änderung | **Ja** — sonst hängen die zehn Prüfregeln aus Konzept 4.8 an drei Schreibstellen und der Reiter-2-Stand macht die U·A-Summe zeitweise falsch. „Speichern unter…" bleibt als nicht schließender Zweitknopf mit `MitSpeichern="true"` (`EPOS.UI/CLAUDE.md:53-56`) |
| **U2** | ~~Die sieben Modellparameterfelder im Tagesbilanz-Weg **verstecken** oder **gesperrt zeigen**?~~ | **Durch E20 überholt (16.09.2026).** Die Modellparameter stehen **immer** sichtbar und bearbeitbar — sie sind der Parametersatz des Gebäudes, nicht der Rechenweg, und gelten nach der Umstellung. Bedingt ist allein der eingeklappte Abschnitt „Tagesbilanz (Bestandsweg)" mit den vier Feldern, die nur der Altweg liest (2.3, 2.4). Die Frage entfällt, die Nummer bleibt vergeben |
| **U3** | `Platzhalter` am `Zahlenfeld` ergänzen (für „Vorgabe 0,3" im leeren Feld) — ein Eingriff in einen Standardbaustein, den alle Dialoge benutzen | **Ja**, rein additiv (ein `[Parameter] string`, ein `placeholder`-Attribut); zieht `StilblattTests` nach sich. Sonst je Feld eine `Herleitungszeile` — zehn Zeilen statt zehn Platzhalter |
| **U4** | Ein Abschnitt „13. Gebäudehülle und Gebäudemodell" im [`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md), **bevor** die 63 en-US-Werte geschrieben werden | **Ja** — das Glossar kennt heute weder Wärmebrücke noch Verschattung, Rahmenanteil, Bodenplatte, Randbedingung oder operative Temperatur. Ohne den Abschnitt entstehen zwei Übersetzungen desselben Begriffs |
| **U5** | Schemaschritt 77 und 78 zu **einem** Schritt verschmelzen (15 Spalten je Tabelle, ein Sichtneubau)? | **Ja** — E1 liefert G1 und G2 gemeinsam aus; zwei Sichtneubauten hintereinander sind zwei Gelegenheiten, die Definitionen auseinanderlaufen zu lassen. Der Tab_Solar-Schritt bleibt **getrennt** (andere Wirkung, anderer Mitläufercode, anderes Risiko) |
| **U6** | Zeitbezug der Sonnengeometrie im Gebäudemodell: **Stundenanfang** wie im Bestand (`KlimaImportAblauf.cs:318-322`) oder **Stundenmitte** wie Blatt 3 (Konzept N1.10)? | **In G1 messen und dann entscheiden** — an der einen Stelle im Eingangsbauer. Der Unterschied sind 7,5° Stundenwinkel und trifft genau Ost und West. `Tab_Solar.Sol_*` bleibt in jedem Fall unberührt (Referenzbasis) |
| **U7** | Wochenendkalender des Stundenmodells aus `Tab_Klimadaten.WE` (wie der Bestand) statt aus `SolardatenCtrl.Referenzjahr` (wie Konzept 4.4)? | **Ja, aus `WE`.** `WE` stammt aus dem PVGIS-Jahr 2020 (`KlimaImportAblauf.cs:354`); der in 4.4 verlangte Test wäre mit einer Preisreihe ≠ 2020 nicht führbar, und beide Wege rechnen sonst verschiedene Kalender |
| **U8** | Normzahlen als **gitignorierte, lokal beizustellende** Datei (`Referenzlaeufe/Normzahlen/`) mit schweigenden Testfällen — Folge: der Normfallnachweis ist **lokal**, nicht CI | **Ja** — das Ausliefern der Normzahlen wäre eine Vervielfältigung (Konzept N1.2), und LFS ist keine Zugriffsbeschränkung. Die Lücke im Gate gehört ins Protokoll, der Laufauszug (Abweichung je Fall, ohne Absolutwerte) in die Dokumentation |
| **U9** | Die Grenze von 100 Gebäuden beheben (`HeizwaermebedarfGeb[100]`, `:31`; `MaxP[100]`, `:56`; `IndexOutOfRangeException` an `:816`)? | **Ja, in GB**, wo die Schleife ohnehin angefasst wird: `MaxP` **löschen** (wird nirgends gelesen — wie `Anzahl_Bewohner` `:11` und `Wohnflaeche` `:12`, Befund X 2.3), `HeizwaermebedarfGeb` auf `ctrl.rows` dimensionieren. Ergebnisneutral — und **vor** der Verschiebung nach `Altweg/`, damit das verschobene Modul der geprüfte Stand ist (E20) |
| **U10** | Eine Lizenzhinweisseite im Installationspaket — und dann gleich für **alle** ausgelieferten Fremdanteile, nicht nur xBIM? | **Ja, mit G4-1 und für alle.** CDDL § 3.1 verlangt den Quellenverweis an den Empfänger; ohne die Seite ist der IFC-Import nicht auslieferbar. Die übrigen Fremdanteile sind ohnehin fällig — darunter **three.js (MIT)** des Gebäudebetrachters (E11, Konzept N1.16) |
| **U11** | Größenlimit für IFC-Dateien: 50 MB Windows / 20 MB iOS — oder es versuchen und bei Speichermangel abbrechen? | **Benannt ablehnen**, nicht versuchen: `MemoryModel` hält das Modell im Arbeitsspeicher (10–20 MB je MB STEP-Text), ein Speicherabbruch auf dem iPad ist kein Fehlerbild, das man erklären kann. **Die iOS-Zahl ist in G4-8 zu messen**, nicht zu schätzen — **mit E18 (16.09.2026) nach Empfehlung entschieden** |
| **U12** | Woher die Vorgaben je Baualtersklasse? TABULA/IWU hat weder DOI noch Datensatzlizenz | **Eigene Werte aus dem EPOS-Gebäudekatalog ableiten** — die Testdatenbank führt Gebäude je Klasse. Lizenzfrei, hausgemacht, passt zu den übrigen EPOS-Vorgaben. Sonst nur A–H vorbelegen und den Rest leer lassen |
| **U13** | Mehrere `IfcBuilding` in einer Datei: Klappliste und **ein** Gebäude je Lauf — oder alle auf einmal anlegen? | **Eines je Lauf.** „Alle auf einmal" erzeugt Katalognamen automatisch und zöge die Dublettenlogik des Katalogimports nach; das ist ein eigener Schritt, nicht G4a |
| **U14** | Fensterabzug: Wandfläche = `GrossSideArea` **minus** Fenster und Außentüren — oder die Öffnungen in der Wandfläche belassen, wie `GrossSideArea` sie liefert? | **Abziehen.** Das Modell führt Wand und Fenster getrennt mit je eigenem U-Wert; ohne Abzug zählt die Öffnung zweimal. Wird die Differenz negativ: `NetSideArea`, sonst Warnung und 0 |
| **U15** | Die drei ψ-Werte und die drei Anschlusslängen beim IFC-Import als **Vorgabe je Baualtersklasse** setzen oder **leer** lassen? | **ψ als Vorgabe, Längen leer.** IFC liefert weder das eine noch das andere; eine geratene Anschlusslänge sähe aus wie eine gemessene, ein ψ-Vorgabewert ist als Klassenwert erkennbar. Beide tragen Herkunft `Vorgabe` bzw. `Leer` |
| **U16** | Soll `ios.yml` um einen **Gerätebau** ergänzt werden (heute baut der Lauf für den Simulator, `:123-127`, und dort wird nie getrimmt) — oder genügt ein einmaliger Nachweis von Hand? | **Einmaliger Nachweis in G4-8**, nach Rückfrage. Ein dauerhafter Release-Zweig verdoppelt die Laufzeit eines Workflows, der zehnfach zählt; erst wenn xBIM im Feld ist, lohnt die Dauerprüfung — **mit E18 (16.09.2026) nach Empfehlung entschieden** |

---

## 6. Abgrenzung

**Dieses Papier behandelt nicht:**

- **Die Physik selbst.** Rechenweg, Knotenbilanzen, Diskretisierung, Randbedingungen, Validierung
  und die gemessenen Vergleichszahlen stehen im
  [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Kapitel 4, 5 und 10; hier steht nur,
  **wo** sie im Quelltext andockt. Ebenso die Einzelheiten der Stufe G3 (Konzept 6.3) und von G5
  (Begründung in 3.1).
- **Das Mehrzonenmodell.** E7 vergibt es an ein eigenes Papier
  (`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`, in Arbeit): Zonenkopplung über θ_NR,eq, Datenmodell
  Zone → Bauteil → Aufbau → Schicht → Baustoff, Zoneneingabe und Zuordnung auf Zonenebene.
- **Bericht und Wiki im Wortlaut.** Was in `KennzahlenKatalog.cs`, `AbweichungsErmittler.cs` und in
  die Wiki-Seite „Gebäudemodell VDI 6007" kommt, steht in Konzept 9; der Logbuch-Eintrag entsteht
  mit dem Upload und trägt die Version, die der Anwender nennt
  ([`Konzept_Hilfesystem_Wikidokumentation.md`](Konzept_Hilfesystem_Wikidokumentation.md), 13.3).
- **Alles, was Konzept 15 ausschließt:** Feuchtebilanz, Kühlung als vierter Kanal (**E12 vom
  16.09.2026 nimmt ihn auf** — die Folgen regelt das Kühlkonzept, Konzept N1.18),
  Bauteilaktivierung, Kopplung von Vorlauftemperatur und Erzeugerfahrplan an die
  Raumtemperatur (seit E22 eigenes Papier [Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md), Stufen AK1 nach G2, AK2 und AK3
  danach — **an keine Stufe „Altweg entfernen" gebunden** (E23): Sie wirken auf VDI-Gebäude, ein
  Gebäude auf dem Bestandsweg geht als feste Last ein), sommerlicher Wärmeschutz nach DIN 4108-2, Nachweise nach GEG/DIN V 18599,
  Verschattung durch Nachbarbebauung, Wärmerückgewinnung, Nutzungsprofile für Nichtwohngebäude,
  Scan-to-BIM, die Validierung an gemessenen Verbräuchen.
- **Normnachweise, die ohne Datenträger nicht führbar sind.** Nach E5 werden weder die Datenträger
  der Richtlinien noch DWD-Testreferenzjahre beschafft; die Testbeispiele 8–16 der VDI 6020 und
  VDI 2078 sind damit nicht nachrechenbar. EPOS-Plan weist nach **E10** aus: **„Rechenkern nach
  VDI 6007 Blatt 1; elf der zwölf Testbeispiele im Normband einschließlich Druckrundung,
  Testbeispiel 11 in zwei Umschaltstunden um 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung)"** — nicht „validiert nach
  VDI 6020/2078". Der Ausweis wechselt auf **„zwölf von zwölf"**, sobald G0 den Fall 11 löst (der
  eigene Knoten der Kühldecke, 1.3). Nach E6 geht aus VDI 6020:2022 nichts in Code, Tests,
  Testdaten, Wiki, Bericht oder Auslieferung.
- **Der Datenaustausch mit gbXML und IFC in beide Richtungen.** E9 macht den gbXML-Import zur
  Pflicht (G4c) und die beiden Exporte zur Stufe G7; Schema, Abbildung, Herkunftsdaten und
  Reihenfolge stehen im
  [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) und in den Befunden R
  und S. Hier steht allein der **IFC-Import** (Kapitel 3) samt der Auflage, seine Zuordnung zu
  persistieren (3.2). Ebenso wenig behandelt dieses Papier den **Gebäudebetrachter** nach **E11**
  (2D-Grundriss aus den Raumgrenzen, schematische Körper aus dem Zonengeometrie-Modell): Konzept
  N1.16, Datenaustauschkonzept Nachtrag 1, Mehrzonenkonzept 6.7.
- **Die Auslegungsheizlast im Stundenmodell** (Heating Design Period) ist Gegenstand eines späteren
  Papiers. `Waermelast_Max` bleibt unverändert das Maximum des Kanalsummenvektors
  (`SimulationWaermebedarf.cs:401`), die drei Spitzenkennzahlen stehen **zusätzlich** je Gebäude.
- **Jede Weiterentwicklung des Tagesbilanz-Wegs.** Nach **E20** und **E23** ist der Altweg der
  **eingefrorene Bestandsweg**: Er wird nach `Simulation/Altweg/` verschoben und bekommt danach
  **dauerhaft keine Änderung außer Fehlerbehebung** — keine neue Größe, keinen neuen Ausweis, keine
  neue Eingabe, keine Kühllast (0 mit benanntem Hinweis), keine Anlagenkopplung, keine Zonen. Was an
  ihm zu beheben ist, gehört in die Stufe **GB**, also **vor** die Verschiebung (Kapitel 4). Eine
  Verbesserung, die beide Wege beträfe, wird allein im VDI-Weg gebaut.
- **Eine Stufe „Altweg entfernen" gibt es nicht** (**E23**, 16.09.2026). Der Bestandsweg bleibt
  dauerhaft; die Konzeptfrage Q24 ist beantwortet (nie), Q25 gegenstandslos. Was Kapitel 4 der
  [Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) dazu ausbreitet,
  ist damit überholt. Ohne Stufe bleibt ein **Aufräumpunkt**: die beiden leserlosen Spalten
  `WW_Bedarf` und `Waermebedarf`; sie bekommen bei Gelegenheit einen eigenen Auftrag, aber keinen
  Platz in der Stufenfolge.
- **Die Kälteseite.** E21 legt nur die **Bauform** fest (Fassade `SimulationKaeltebedarf`,
  derselbe Vorbereitungsschritt, dieselbe Gebäuderechnung, kein Altweg auf der Kälteseite; 1.1 und
  1.4). Kanal, Senkenzuordnung, Kälteerzeuger, Dialoge, Schema und Bericht der Kühlung stehen im
  [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) (Stufen KU0–KU3, E12) und in
  [Befund W](Gebaeudesimulation/2026-09-16_Befund_W_Kuehlung_Bestand.md).
