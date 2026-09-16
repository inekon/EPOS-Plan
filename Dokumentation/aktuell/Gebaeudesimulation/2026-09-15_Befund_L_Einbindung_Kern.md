# Befund L — Einbindung der VDI-6007-Gebäudesimulation in den Rechenkern (15.09.2026)

**Protokoll.** Befund L eines Erkundungs-Agenten (Modell Opus, nur lesend) im Auftrag des Umsetzungskonzepts [`../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026.

Gelesen wurden die Wurzel-`CLAUDE.md`, [`EPOS.Kern/CLAUDE.md`](../../../EPOS.Kern/CLAUDE.md), das Konzept
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Kapitel 4, 6, 7, 8, 10, 11 und Nachtrag 1 vollständig) und der Quelltext. Jede Aussage über den
Code trägt Datei und Zeile; gemessen am Stand des Zweigs `ios_migration_september` vom 15.09.2026.

---

## 0. Die Lage in acht Sätzen

1. **Die Verzweigung hat genau eine Stelle.** `SimulationWaermebedarf.HeizwaermeEinesGebaeudes`
   (`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:566`) ist der einzige Ort, an dem
   Lauf **und** Dialog die Wärme eines Gebäudes rechnen. Wer dort verzweigt, hat beide Wege.
2. **Der Puffer passt schon.** Der Zielvektor ist ein Einzelgebäude-Puffer in **Watt**, der
   Aufrufer nullt ihn (`:598`) und rechnet erst danach einmal nach kW (`:222`). Ein Stundenmodell
   füllt denselben Vektor mit denselben Einheiten.
3. **Der Klimazugriff ist vorbereitet.** `SolardatenCtrl.ReadOrtszeit`
   (`EPOS.Kern/Controller/SolardatenCtrl.cs:156`) liefert die 8 760 Zeilen in Ortszeit samt
   `Globalstrahlung`, `Direktstrahlung`, `Diffusstrahlung`, `Temperatur` und UTC-Herkunft je Zeile;
   `SolarCalculator.CalculateHourlyHayDavies` (`EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:455`)
   rechnet daraus jede Orientierung.
4. **Die Erdreich-Randbedingung existiert bereits fertig.**
   `ErdreichTemperatur.JahresprofilKollektor` (`EPOS.Kern/Allgemein/Simulation/ErdreichTemperatur.cs:411`)
   ist genau der Kusuda-Ansatz aus Konzept 4.4 — mit Vorgabeboden und 1 m Tiefe reproduziert er
   Dämpfung 0,68 und Phasenverzug 22 Tage auf zwei Stellen. Nichts davon ist neu zu schreiben.
5. **PVGIS liefert die Gegenstrahlung, EPOS wirft sie weg.** Die eingefrorene Antwort
   `Referenzlaeufe/Importproben/pvgis_tmy_stuttgart_72h.json` führt `IR(h)`, `WS10m`, `WD10m`, `RH`
   und `SP`; `TmyHourlyData` (`SolarPVGISCalculator.cs:57-89`) liest nur `RH` und `WS10m`, und
   `AccessRepository.SaveTmyData` (`:602-603`) schreibt auch die nicht.
6. **Eine Namensfalle steht scharf im Weg.** Das Modellfeld `Fensterflaeche_Ost` trägt heute den
   Wert der Spalte `Fensterflaeche_Ost_West` (`ProjektGebaeudeCtrl.cs:56`, `GebaeudeCtrl.cs:66`).
   Wer die neue Spalte `Fensterflaeche_Ost` anlegt, **bevor** das Feld umbenannt ist, verliert die
   Ost-/Westfenster des Tagesmodells still.
7. **Neue CSV-Dateien brechen den Referenzvergleich.** `Vergleich` kennt nur einen
   Schlüssel-Ausschluss (`Referenzlauf/Vergleich.cs:61-79`); eine Datei, die nur im neuen Lauf
   liegt, ist `Schwere = double.MaxValue` (`:183-190`) — also FAIL, nicht ausschaltbar.
8. **Der Kopiervorgang aus dem Katalog zerstört „NULL = Vorgabe".**
   `GebaeudeStammCtrl.CopyFromStamm` (`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:439`) bildet jedes
   `DBNull` auf `0.0` bzw. `""` ab. Eine neue Spalte, die dort mitläuft, kommt im Projekt als 0 an —
   und 0 ist bei `Rahmenanteil`, `Verschattungsfaktor` und `Innenflaechenfaktor` keine Vorgabe,
   sondern ein anderes Gebäude.

---

## 1. Der Weg je Gebäude im Bestand

### 1.1 Die Schleife und ihre Puffer

`SimulationWaermebedarf.Waermebedarf_berechnen(int ID_Projekt, int ID_Klimaregion)`
(`SimulationWaermebedarf.cs:128`) ist der Einstieg. Ablauf, soweit er das Gebäude betrifft:

| Zeile | Was geschieht | Einheit |
|---|---|---|
| `:146-153` | `Dauerlinie`, `Waermebedarf`, `Waermebedarf_Gebaeude`, … auf 0 | — |
| `:165-166` | `_kanaele = new Kanalsatz()`, `kanalHeizung = _kanaele.Heizung` | W (noch) |
| `:172` | `double[] probe = new double[8760]` — unabhängige Energieprobe | W |
| `:175` | `KlimakalenderLesen(ID_Klimaregion)` | — |
| `:177-178` | `ProjektGebaeudeCtrl.ReadAll(ID_Projekt)` | — |
| `:188` | `double[] Waermebedarf_EinGebaeude = new double[8760]` — **ein** Puffer, je Durchlauf genullt | **W** |
| `:190-214` | Schleife `for (i = 0; i < ctrl.rows; i++)` | |
| `:197` | `if (!HeizwaermeEinesGebaeudes(ctrl.items[i], i, Waermebedarf_EinGebaeude)) return;` | |
| `:201` | `BhkwPlan.VectorenAddieren(Waermebedarf_EinGebaeude, kanalHeizung)` | W |
| `:204` | `probe[h] += Waermebedarf_EinGebaeude[h]` | W |
| `:208` | `VectorenAddieren(…, Waermebedarf_Gebaeude)` — Summe aller Gebäude | W |
| `:212` | `MaxP[i] = Maximaler_Waermebedarf(Waermebedarf_EinGebaeude)` | W |
| `:222` | `BhkwPlan.WattToKw(kanalHeizung)` — **die eine Umrechnung W → kW** | kW |
| `:223` | `probe[h] *= 0.001` | kW |
| `:228` | `Waermebedarf_Gebaeude_Gesamt = kanalHeizung.Sum() / 1000` | MWh |
| `:321` | `BhkwPlan.MonatsSumme(kanalHeizung, Waermebedarf_Gebaeude_Monat, …)` | MWh |
| `:385-390` | `SummenvektorAusKanaelen()`, `Waermebedarf_Gesamt = Waermebedarf.Sum() / 1000`, `Energieprobe(probe)` | MWh |
| `:401` | `Waermebedarf_Max = Maximaler_Waermebedarf(Waermebedarf)` — **`Waermelast_Max` des Projekts** | kW |
| `:405-413` | Normierung, Heapsort, `Array.Reverse(Dauerlinie)` | — |

**`Waermebedarf_Gebaeude` bleibt in Watt** (`:208` addiert vor `:222`). Das ist eine Falle für den
Ergebnisexport (Abschnitt 4.2), keine für das neue Modell.

**Zwei feste Grenzen von 100.** `HeizwaermebedarfGeb` (`:31`) und `MaxP` (`:56`) sind
`double[100]`; ein Projekt mit mehr als 100 Gebäuden wirft eine `IndexOutOfRangeException` in
`Berechnung_Gebaeude_Tageswerte` (`:816`). `MaxP` wird geschrieben (`:212`) und **nirgends
gelesen** — repoweit gibt es keinen zweiten Treffer. Es ist toter Bestand; das Stundenmodell sollte
seine drei Spitzenkennzahlen (Konzept 4.5) nicht dort ablegen, sondern in einem eigenen,
benannten Ergebnisobjekt.

### 1.2 `HeizwaermeEinesGebaeudes` — die Naht

```csharp
internal bool HeizwaermeEinesGebaeudes(ProjektGebaeudeModel item, int index, double[] ziel)
```
(`SimulationWaermebedarf.cs:566`). Der Rumpf in fünf Schritten:

1. `:569-576` Einheit: bei `"Wohnfläche [m²]"` nur `item.Bewohner = Z_AuswahlWohnflaeche / Flaeche_Nutzer`,
   sonst `Bewohner_und_Flaeche_berechnen(item, index)`.
2. `:581` `Berechnung_Gebaeude_Tageswerte(item, index)` — füllt `Heizlast[365]` und
   `HeizwaermebedarfGeb[index]`.
3. `:584` `TagesVerteilung = DBTagesVeteilung(item.Typ, item.ID_Gebaeude, ref tagv_found)` —
   `Abfrage_Tagverteilung`, 192 Werte (8 Tagtypen × 24 h).
4. `:590-595` fehlt die Verteilung: Warnung `SIMENG_TAGESVERTEILUNG_FEHLT` und `return false` —
   der Lauf bricht ab (`:197`).
5. `:598-608` `VectorInit(ziel)`, dann `BhkwPlan.StdWerte(ziel, TagTyp_W | TagTyp_NW, TagesVerteilung, Heizlast)`.
   Die Wahl hängt am Text `item.Typ == "Wohngebaeude  VDI 2067"` (`:601`, zwei Leerzeichen).

**Das ist die Verzweigungsstelle.** Alles davor (Flächen- und Bewohnerrechnung) und alles danach
(`:201-212`, Kanal, Summen, Dauerlinie, Energieprobe) bleibt unberührt — genau wie Konzept 4.1 es
zusagt.

### 1.3 `Berechnung_Gebaeude_Tageswerte` — das Tagesmodell

`private void Berechnung_Gebaeude_Tageswerte(ProjektGebaeudeModel item, int GebaeudeNr)`
(`:684`). Aufbau:

- `:689-733` Ferienmaske `F_Absenkung[365]`. **Ein Bestandsfehler steht hier offen:**
  Ferienzeitraum 1 läuft ohne `-1` (`:703`, `:707`), die Zeiträume 2–4 mit `-1`
  (`:714`, `:721`, `:728`). Zeitraum 1 ist außerdem als Jahreswechsel gelesen (`Beginn…365` **und**
  `0…Ende`), was bei `Ferienbeginn_1 < Ferienende_1` den ganzen Rest des Jahres absenkt.
- `:748-814` **Vorlauf**: dieselbe Rechnung für `Tag = 350…364`, Ergebnis verworfen — **15 Tage**.
  Konzept 4.6 setzt dagegen 30 Tage.
- `:818-886` der Jahreslauf `Tag = 0…364` mit
  `Solare_Gewinne[Tag] = BhkwPlan.SolareGewinneC(…) / 100.0` (`:825`),
  `SpezWaermeverluste[Tag] = BhkwPlan.SpezWaermeverlusteC(…) / 100.0` (`:837`) und
  `Heizlast[Tag] = BhkwPlan.TaeglHeizlastWG(…)` (`:869`).
- `:885` `HeizwaermebedarfGeb[GebaeudeNr] += Heizlast[Tag]`.

**Die Gewichte** stehen in `BhkwPlan.SpezWaermeverlusteC`
(`EPOS.Kern/Allgemein/BhkwPlan.cs:347-353`): 0,83 · U_AW·A_AW, 1,0 · U_F·A_F, 0,95 · U_D·A_D,
0,45 · U_G·A_G, 1,0 · U_S·A_S, Wärmebrücken × 0,83. Die Lüftung (`:356-359`) rechnet mit
`1.2 · 0.2777777777777778` = 0,3333 Wh/(m³K) und unter 0 °C mit dem Temperaturfaktor
`(θ_a·0,025 + 1)`. Rückgabe ist das **Hundertfache** (`:361`).

**Der statische Zustand (Q23)** ist `BhkwPlan._prevRoomTemp` (`BhkwPlan.cs:51`), gesetzt in
`TaeglHeizlastWG` (`:433`), zurückgesetzt nur über `ResetState()` (`:54`). Er trägt über
Gebäudegrenzen hinweg: Gebäude 2 startet mit der Raumtemperatur, die Gebäude 1 am 31.12.
hinterlassen hat. Deshalb hängt das Ergebnis von 1008 und 1039 an der Zeilenreihenfolge.

### 1.4 `Bewohner_und_Flaeche_berechnen` — die Verbrauchsrückrechnung

`private void Bewohner_und_Flaeche_berechnen(ProjektGebaeudeModel item, int index)` (`:613`):

- `:617-636` `VerbrauchNeu` je Einheit: Öl `× η × 10,08`, Gas m³ `× η × 11,48`,
  Gas MWh (Ho) `× η / 1,1 × 1000`, Brennstoff MWh `× η × 1000`, Verbrauch MWh `× 1000` — Ergebnis in **kWh**.
- `:643-655` der Rückrechnungszweig: `Z_AuswahlWohnflaeche = Wohnflaeche_gesamt`, dann ein
  **erster** Aufruf von `Berechnung_Gebaeude_Tageswerte` (`:647`), danach
  `VerbrauchAlt = HeizwaermebedarfGeb[index] / 1000` (`:650`) und
  `FlaecheNeu = VerbrauchNeu / VerbrauchAlt × FlaecheAlt` (`:651`).

Das Gebäude wird also **zweimal** gerechnet: einmal für `VerbrauchAlt`, danach noch einmal aus
`HeizwaermeEinesGebaeudes:581`. Für das Stundenmodell heißt das: die Verhältnisrechnung aus
Konzept 4.7 braucht **zwei Läufe des Lösers** je Verbrauchsgebäude — bei 5 ms je Lauf (Konzept
4.8/5.13) belanglos, aber der Löser muss ohne Restzustand zweimal hintereinander laufen dürfen.
Der Rückfall `VerbrauchAlt = 0` ist ungeschützt (Division durch null → `Infinity`); Konzept 4.8
verlangt hier eine benannte Prüfung.

**Geschrieben wird am Modell.** `item.Bewohner` und `item.Z_AuswahlWohnflaeche` sind
Ausgabefelder (`:571`, `:641`, `:645`, `:652`, `:653`); der Doku-Kopf `:558-560` sagt es
ausdrücklich. Ein Eingangsbauer für das Stundenmodell muss deshalb **nach** diesem Schritt lesen.

### 1.5 Welche Felder von `ProjektGebaeudeModel` der Bestandsweg liest

Aus `EPOS.Kern/Model/ProjektGebaeudeModel.cs:11-68` (58 Felder, alle über
`ProjektGebaeudeCtrl.ReadAll:42-99` gefüllt):

| Gruppe | Felder | gelesen in |
|---|---|---|
| Zuordnung | `ID_Projekt`, `ID_Gebaeude`, `Z_AuswahlWohnflaeche`, `Einheit`, `Jahresnutzungsgrad`, `DezentralWarmwasser` | `:569`, `:584`, `:617-636` |
| Geometrie | `Wohnflaeche_gesamt`, `Wohnflaeche`, `Raumhoehe`, `Flaeche_Nutzer` | `:641`, `:768-774`, `:812-813` |
| Hülle U | `k_Wert_Außenwand`, `k_Wert_Fenster`, `k_Wert_Dachflaeche`, `k_Wert_Grundflaeche`, `k_Wert_Sonstiges` | `:837-843` |
| Hülle A | `Flaeche_Außenwand`, `gesamte_Fensterflaeche`, `Dachflaeche`, `Grundflaeche`, `Sonstige_Flaechen` | `:837-843` |
| Wärmebrücken | drei `Waermebrueckenverlustkoeffizient_*` + drei `Abmessung_*` | `:840-842` |
| Fenster | `Fensterflaeche_Sued`, **`Fensterflaeche_Ost`** (= Spalte `Ost_West`), `Fensterflaeche_Nord`, `Fensterdurchlassgrad` | `:825-827` |
| Betrieb | `Luftwechselrate`, `Interne_Waermegewinne`, `Bauweise`, `Maximaleraumtemperatur` | `:843`, `:876-881` |
| Sollwerte | `Raumsolltemperatur_Tag/_Nachtabsenkung/_Wochenende/_Ferien` | `:871-875` |
| Kalender | `Wochenende`, `Ferien`, `Ferienbeginn_1…4`, `Ferienende_1…4` | `:689-733`, `:846` |
| Art | `Typ` | `:584`, `:601` |
| **ungelesen** | `Beschreibung`, `Bewohner` (nur Ausgabe), `WW_Bedarf`, `spez_Waermeverbrauch`, `Waermebedarf`, `Baualtersklasse`, `Gebaeudeart`, `Wohngebaeude_Nicht_Wohngebaeude` | — |

Das Stundenmodell braucht **denselben** Satz plus die elf neuen Spalten aus Konzept 6.1 — kein
Feld mehr, kein Feld weniger. `spez_Waermeverbrauch` ist reine Katalogkennzahl und geht nur in das
Abnahmekriterium 10.4 (4).

### 1.6 `KlimakalenderLesen` und die Stundentemperatur

`internal void KlimakalenderLesen(int ID_Klimaregion)` (`:513`) füllt aus `KlimadatenCtrl`
(365 Tageszeilen, `EPOS.Kern/Controller/KlimadatenCtrl.cs:35-37`) die sieben Tagesreihen
`Sol_N/_O/_S/_w`, `A_Temp`, `WE`, `TagTyp_W`, `TagTyp_NW` (`:519-526`), ruft dann
`Stundentemperatur_aus_DB(ID_Klimaregion)` (`:528`) und setzt
`WochentagJan1 = ProfilBedarf.WochentagJan1AusWE(WE)` (`:534`).

`Stundentemperatur_aus_DB` (`:912`) geht **über den Ortszeit-Lesepfad**:
`ctrldat.ReadOrtszeit(ID_Klimaregion, m_ID_Projekt)` (`:915`), danach
`Stundentemperatur[i] = ctrldat.items[i].Außen_Temp` (`:919`) — 8 760 Werte in °C.

**Für das Stundenmodell reicht das nicht:** es braucht aus derselben `SolardatenCtrl`-Instanz
auch `Globalstrahlung`, `Direktstrahlung`, `Diffusstrahlung` und die UTC-Herkunft je Zeile
(`TagUtc`, `StundeUtc`, `SolardatenCtrl.cs:174-175`). Die Reihe wird heute gelesen und sofort
verworfen (`:914-920`). Der saubere Griff ist, `KlimakalenderLesen` die ganze Zeilenliste behalten
zu lassen (ein Feld `_solarOrtszeit`), statt ein zweites Mal zu lesen.

### 1.7 `GebaeudeBedarfCtrl.Rechnen` — die Auskunft

`internal static GebaeudeBedarfErgebnis Rechnen(int idProjekt, int idKlimaregion, int idZ)`
(`EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs:94`) ruft **dieselben zwei Methoden** wie der Lauf:

- `:99` `TabGebaeudeId(idZ)` — `SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?` (`:149`)
- `:102-107` `ProjektGebaeudeCtrl.ReadAll(idProjekt)`, dann Suche über `ID_Gebaeude`
- `:111-112` `new SimulationWaermebedarf { m_ID_Projekt = idProjekt }` + `sim.KlimakalenderLesen(idKlimaregion)`
- `:117` `sim.HeizwaermeEinesGebaeudes(gebaeude, 0, werte)` — **Merkplatz 0**
- `:121` `BhkwPlan.WattToKw(werte)`
- `:130-133` `werte.Sum() / 1000` (MWh), `Hoechstwert` (kW), `MonatsSumme` (MWh)

**Folge für die Einbindung:** die Verzweigung in `HeizwaermeEinesGebaeudes` trägt den Dialog
automatisch mit. `GebaeudeBedarfErgebnis` (`:21-47`) braucht aber neue Felder für Raumtemperatur,
operative Temperatur, Kühlbedarf und die drei Spitzenkennzahlen — und `Rechnen` braucht einen
Rückgabeweg dafür, weil `HeizwaermeEinesGebaeudes` heute nur `bool` liefert.

### 1.8 Aufrufkette, Klimaregion, Referenzjahr

| Weg | Stelle |
|---|---|
| Lauf (Referenzlauf, Startseite) | `SimulationRunner.Simuliere_Intern` → `SimulationWaermebedarf.Waermebedarf_berechnen(idProjekt, nKlimaregion)` (`EPOS.Kern/Allgemein/Simulation/SimulationRunner.cs:184`) |
| Klimaregion | `ProjektCtrl.ReadSingle(idProjekt)` → `m_ID_Klimaregion`; 0 bricht ab (`SimulationRunner.cs:163-170`) |
| Maskenweg | `SimulationLaufCtrl.Bedarf(idProjekt, idKlimaregion, …)` → `waerme.Waermebedarf_berechnen(idProjekt, idKlimaregion)` (`EPOS.Kern/Controller/SimulationLaufCtrl.cs:114-120`) |
| Vorprüfung | `SimulationLaufCtrl.Vorpruefen` (`:60`), `if (idKlimaregion == 0) return SIM_MSG_KLIMAREGION_WAEHLEN` (`:75`) |
| Schemasperre | `SchemaStand.SimulationGesperrt(out sperrgrund)` vor allem anderen (`SimulationRunner.cs:137`) |
| Wochentagskalender | `simulation_Strombedarf.WochentagJan1 = simulation_Waermebedarf.WochentagJan1` (`SimulationRunner.cs:190`, `SimulationLaufCtrl.cs:123`) |

**Referenzjahr.** `SolardatenCtrl.Referenzjahr(int idProjekt)` (`SolardatenCtrl.cs:222`) nimmt das
Jahr der aktiven Spotpreisreihe (`ReadAktiveVariante` → `ID_Preisreihe` → `Tab_Preisreihe.Jahr`,
`:228-232`), sonst `DbWerte.SOLAR_REFERENZJAHR_STANDARD` (`:242`). Es entscheidet **ausschließlich**
über die zwei Sommerzeit-Umstelltage. Konzept 4.4 stützt darauf den Wochentag des 1. Januar — das
ist eine **neue** Verwendung: heute kommt der Wochentag aus `Tab_Klimadaten.WE`
(`SimulationWaermebedarf.cs:534`), und `Tab_Klimadaten.WE` entsteht im Import aus
`datum.DayOfWeek` der TMY-Zeitmarke (`EPOS.Kern/Allgemein/Import/KlimaImportAblauf.cs:354`) —
also aus dem PVGIS-Jahr 2020, nicht aus dem Referenzjahr. Der in Konzept 4.4 verlangte Test
(Maske gegen `Tab_Klimadaten.WE`) wird damit **fehlschlagen**, sobald das Referenzjahr ≠ 2020 ist.
Empfehlung: das Stundenmodell nimmt den Wochenendkalender aus derselben Quelle wie der Bestand
(`WE[365]` aus `KlimakalenderLesen:524`), nicht aus dem Referenzjahr; dann rechnen beide Wege
denselben Kalender, und die Vergleichbarkeit im Bedarfsdialog (Konzept 8.2) bleibt.

---

## 2. Klimazugriff

### 2.1 `SolardatenCtrl.ReadOrtszeit`

```csharp
public void ReadOrtszeit(int idKlimaregion, int idProjekt, bool stamm = false)
```
(`SolardatenCtrl.cs:156`). Ablauf: `SELECT * FROM [Tab_Solar|Tab_Solar_STAMM] WHERE ID_Klimaregion = ? ORDER BY ID`
(`:160-162`), Zeilen in UTC-Reihenfolge mit `TagUtc = pos/24 + 1` und `StundeUtc = pos%24`
(`:174-175`), Prüfung auf 8 760 Zeilen (`:181`; sonst Warnung und **keine** Verschiebung, `:183-191`),
dann Umsortierung über `SolarZeitbasis.Zuordnung(Referenzjahr(idProjekt))` (`:195-199`) und ein
Protokollhinweis mit Referenzjahr und Umstelltagen (`:201-205`).

Gefüllt werden `items` (`SolardatenModel`), `list_Temperatur`, `list_Sonnenwinkel`, `list_Tag`
(`:255-265`). Je Zeile abgebildet werden (`MapDataRowToModel:29-42`): `ID`, `ID_Klimaregion`,
`Temperatur` → `Außen_Temp`, `Sol_Nord/Ost/Sued/West`, `Globalstrahlung`, `Direktstrahlung`,
`Diffusstrahlung`, `Sonnenwinkel`.

`SolarZeitbasis` (`EPOS.Kern/Allgemein/Simulation/SolarZeitbasis.cs:36`) ist datenbankfrei:
`STUNDEN_JAHR = 8760` (`:39`), `TAGE_JAHR = 365` (`:42`), `OFFSET_MEZ = 1` (`:45`),
`OFFSET_MESZ = 2` (`:48`), `TagSommerzeitBeginn/Ende(int)` (`:54`, `:63`), `IstSommerzeit` (`:77`),
`Offset` (`:96`), `UtcIndex` (`:124`), `Zuordnung(int) → int[8760]` (`:135`), `UmstelltageText` (`:146`).
Die EU-Regel ist fest verdrahtet, **kein** `TimeZoneInfo` (Klassenkopf `:26-29`).

**Wichtig für das Stundenmodell:** `Zuordnung` verschiebt ganze Zeilen; die UTC-Herkunft bleibt am
Objekt. Der Sonnenstand ist also weiter auf `TagUtc`/`StundeUtc` zu rechnen, die Bilanz auf dem
Ortszeitindex — genau so, wie es `SimulationPV` und `SimulationSolarthermie` tun.

### 2.2 `KlimadatenCtrl` — die Tagesebene

`KlimadatenCtrl` (`EPOS.Kern/Controller/KlimadatenCtrl.cs:9`) liest `Tab_Klimadaten` (365 Zeilen je
Region) mit `ReadAll(int ID_Klimaregion)` (`:35-37`, String-Interpolation statt Parameter — ein
Altbestand). Er trägt `Sol_Nord/Ost/Sued/West` (Tagesmittel), `Temperatur`, `WE`, `TagTyp_W`,
`TagTyp_NW`. **Das Stundenmodell braucht davon nur `WE`** (Wochenendmaske) und die beiden Tagtypen
gar nicht — die Tagesverteilung entfällt. Konzept 4.4 sagt „braucht `Tab_Klimadaten` nicht mehr";
das stimmt für die Physik, nicht für den Kalender, solange der Wochenendkalender von dort kommt
(Abschnitt 1.8).

### 2.3 `SolarPVGISCalculator` — Geometrie und Transposition

**`Sonnengeometrie`** (`EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:353`, privat):

```csharp
private static Sonnenstand Sonnengeometrie(double Lon, double Lat, int Tilt, int Azimuth,
                                           int dayOfYear, double hour)
```

- `:359-362` Zeitgleichung `eot`, wahre Sonnenzeit `solarHour = hour + (eot + 4·Lon)/60`,
  Stundenwinkel `omega = (solarHour − 12)·15`.
- `:365-368` Deklination `delta = 23,45·sin(360·(284+n)/365)`, Sonnenhöhe `alpha`.
- `:371` `alpha ≤ 0` ⇒ `Nacht = true`.
- `:373-376` Azimut `gammaS`, **vormittags negativ** (`if (omega < 0) gammaS = -gammaS`).
- `:380-382` `cosTheta = sin α·cos β + cos α·sin β·cos(γ_S − γ_Fläche)`, auf 0 geklemmt.

**Azimutkonvention:** `Azimuth` ist Grad gegen **Süd**, Ost negativ, West positiv — belegt durch
`KlimaImportAblauf.cs:135`: `AZ_SUED = 0, AZ_OST = -90, AZ_NORD = 180, AZ_WEST = 90`,
`NEIGUNG_FASSADE = 90` (`:132`). Das ist die Konvention, in der `GebaeudeModellEingang` die vier
Fensterorientierungen anzugeben hat.

**Zeitbezug:** `hour` ist der **Stundenanfang** — `KlimaImportAblauf.Rechnen` übergibt
`dt.Hour` aus der TMY-Zeitmarke `"yyyyMMdd:HHmm"` (`:318-322`), und PVGIS gibt `20200101:0000`,
`:0100`, … Konzept N1.10 verlangt für Blatt 3 die **Stundenmitte**. Der Unterschied ist eine halbe
Stunde Stundenwinkel = 7,5°; auf Ost- und Westflächen verschiebt das den Tagesgang sichtbar.
**Das ist in G1 zu entscheiden und zu messen** — und zwar für das Gebäudemodell allein, weil
`CalculateHourly` mit `dt.Hour` in `Tab_Solar.Sol_*` eingefroren ist und dort nicht angefasst
werden darf (Referenzbasis).

**`CalculateHourly`** (`:389`) — isotrop, der Bestand:

```csharp
public static double CalculateHourly(double Lon, double Lat, int Tilt, int Azimuth,
                                     double ghi, double dni, double dhi, double t2m,
                                     int dayOfYear, double hour)
```
Rückgabe `gTotal` in **W/m²** (`:405-417`): `dni·cosθ + dhi·skyView + ghi·0,2·groundView`.
Sie schreibt drei statische Felder (`sonnenwinkel:395`, `sonnen_azimut:399`, `lastCosTheta:401`) —
**Vertragsbestandteil** für `SimulationSolarthermie`, aber prozessweiter Zustand. Ein
Gebäudemodell darf sie deshalb **nicht** benutzen (Konzept 4.1 „Zustand je Instanz, nichts
Statisches").

**`CalculateHourlyHayDavies`** (`:455`) — anisotrop, ohne statische Seitenwirkung (`:446-452`):

```csharp
public static double CalculateHourlyHayDavies(double Lon, double Lat, int Tilt, int Azimuth,
                                              double ghi, double dni, double dhi,
                                              int dayOfYear, double hour)
```
`:466-481`: `cosZenit = sin α`, `rB = cosθ / max(cosZenit, cos 85°)` (`COS_85`, `:318`),
`i0n = 1367·(1 + 0,033·cos(360·n/365))`, `ai = clamp(dni/i0n, 0, 1)`,
`G_t = dni·cosθ + dhi·(ai·rB + (1−ai)·skyView) + ghi·ALBEDO_BODEN·groundView`, `ALBEDO_BODEN = 0,2`
(`:311`). Einheit **W/m²**, `dayOfYear` 1-basiert (`:454`). **Das ist die Funktion, die
`GebaeudeModellEingang` je Orientierung ruft** — genau wie Konzept 4.4 es vorsieht, und ohne
Nebenwirkung.

### 2.4 `TmyHourlyData` und was persistiert wird

`TmyHourlyData` (`SolarPVGISCalculator.cs:57-89`):

| Feld | JSON | Einheit |
|---|---|---|
| `TimeString` | `time(UTC)` | `"yyyyMMdd:HHmm"` |
| `Temperature` | `T2m` | °C |
| `Humidity` | `RH` | % |
| `GlobalIrradiance` | `G(h)` | W/m² |
| `DirectIrradiance` | `Gb(n)` | W/m² (DNI) |
| `DiffuseIrradiance` | `Gd(h)` | W/m² |
| `WindSpeed` | `WS10m` | m/s |
| `Sol_sued/_ost/_nord/_west`, `WE`, `TagTyp_W`, `TagTyp_NW`, `Sonnenwinkel` | — (gerechnet) | W/m², —, ° |

**Die PVGIS-Antwort führt mehr.** `Referenzlaeufe/Importproben/pvgis_tmy_stuttgart_72h.json`
(eingefroren, offline lesbar über `PVGIS_EPW_Downloader.AusJson`, `:191`) enthält je Stunde:
`time(UTC)`, `T2m`, `RH`, `G(h)`, `Gb(n)`, `Gd(h)`, **`IR(h)`**, `WS10m`, `WD10m`, `SP`.
`IR(h)` ist die **atmosphärische Gegenstrahlung in W/m²** (in der Probe 250,0 in der Januarnacht) —
also genau die Größe, die Blatt 1 Gl. (33)–(37) für den langwelligen Austausch braucht (Konzept
N1.10). Sie wird **nicht gelesen**: `TmyHourlyData` hat kein Feld dafür, `WD10m` und `SP` ebenso wenig.

**Was persistiert wird**, entscheidet `AccessRepository.SaveTmyData` (`:586`):

- `:602` für `Tab_Klimadaten*`: `ID_Klimaregion, Temperatur, Sol_Nord, Sol_Sued, Sol_Ost, Sol_West,
  Globalstrahlung, Direktstrahlung, Diffusstrahlung, WE, TagTyp_W, TagTyp_NW, Sonnenwinkel`
- `:603` für `Tab_Solar*`: dieselbe Liste **ohne** `WE`, `TagTyp_W`, `TagTyp_NW`

**Weder `Humidity` noch `WindSpeed` werden geschrieben** (`:614-635` bindet sie nicht) — obwohl
beide bereits gelesen werden. Das ist die kleinste denkbare Erweiterung: drei Parameter mehr.

`KlimaImportAblauf.Laufen` (`:149`) ruft `Rechnen(stunden, lon, lat, abbruch)` (`:233`, Definition
`:305`), legt die Region an (`:249`), schreibt die 8 760 Stundenzeilen
(`repo.SaveTmyData(stunden, bezeichner, "Tab_Solar_STAMM", id, v)`, `:263`), bildet die
Tagesmittel (`SolarCalculator.GetDailyAverages`, `:266`, Definition `:484`), setzt die Tagtypen
(`Tagtypen(tage)`, `:267`, Definition `:345`) und schreibt sie nach `Tab_Klimadaten_STAMM`
(`:270`) — **eine** Transaktion (`:244-282`).

**Schema `Tab_Solar` / `Tab_Solar_STAMM`** (`sql/schema/001_grundschema.sql:2153` bzw. `:2169`):
`ID`, `ID_Projekt` (nur Projekttabelle), `ID_Klimaregion`, `Temperatur`, `Sol_Nord`, `Sol_Ost`,
`Sol_Sued`, `Sol_West`, `Globalstrahlung`, `Direktstrahlung`, `Diffusstrahlung`, `Sonnenwinkel` —
alles `REAL`, Tabelle `STRICT`.

**Wo die Persistierung anzuhängen wäre** (vier Stellen, alle klein):

1. `TmyHourlyData`: drei Eigenschaften `[JsonPropertyName("IR(h)")] Gegenstrahlung`,
   `WD10m`, `SP` — oder nur `IR(h)`, wenn Windrichtung und Luftdruck nicht gebraucht werden.
2. `SaveTmyData:602-603`: drei Spalten je Anweisung, drei `DbParam` je Zeile (`:614-635`).
3. `SolardatenCtrl.MapDataRowToModel:29-42`: drei Zeilen nach demselben Muster
   (`dt.Columns.Contains(...)`), `SolardatenModel` bekommt drei Felder.
4. Migrationsschritt (Abschnitt 6.5): `Tab_Solar` **und** `Tab_Solar_STAMM` je drei `REAL`-Spalten.

**Auslieferungsvorlage:** keine Anpassung nötig. `Werkzeuge/Auslieferungsvorlage/Projektsicht.cs:95-103`
liest die Spalten über `DataRepository.SpaltenVonTabelle(tabelle)` aus der Datei selbst; es gibt
keine fest verdrahtete Spaltenliste. `Prueflauf.Textspalten` fragt `pragma_table_info`
(`Werkzeuge/Auslieferungsvorlage/Prueflauf.cs:193-201`). `Tab_Solar_STAMM` führt keine Spalte
`ReadOnly` und bleibt deshalb vollständig (`Vorlagenbau.cs:113-116`).

**`SolardatenCtrl.Insert` und `WriteDataTable` sind Altlasten**: beide schreiben nur
`(ID, ID_Klimaregion, Temperatur)` (`SolardatenCtrl.cs:293`, `:350`); `Insert` wird laut Kommentar
`:291-292` gar nicht gerufen. Sie sind für die neuen Spalten **nicht** anzufassen.

---

## 3. Persistenz und Migration

### 3.1 `DbWerte` — das Muster `PV_MODELL_*`

`EPOS.Kern/Allgemein/DbWerte.cs:2158-2199`: ein Abschnittskopf mit Konzeptverweis, dann je Wert
eine `public const string` mit XML-Doku, die sagt, **was NULL bedeutet**:

```csharp
public const string PV_MODELL_EINFACH   = "PV_MODELL_EINFACH";   // :2173
public const string PV_MODELL_ERWEITERT = "PV_MODELL_ERWEITERT"; // :2180
```
Die Kanalwerte daneben (`KANAL_HEIZUNG = "Heizung"`, `:1260`; `KANAL_PROZESS = "Prozesswaerme"`,
`:1271` — bewusst ohne Umlaut, weil in SQL verglichen) zeigen die zweite Regel: **Persistenzwerte
sind eingefroren und ASCII.**

Für G1 entstehen hier:
`GEBAEUDE_MODELL_TAGESBILANZ = "TAGESBILANZ"`, `GEBAEUDE_MODELL_VDI6007 = "VDI6007"`,
`GRUND_ERDREICH = "ERDREICH"`, `GRUND_KELLER = "KELLER"`, `GRUND_AUSSENLUFT = "AUSSENLUFT"`.
**Die NULL-Bedeutung ist durch E1 umgedreht:** NULL = `VDI6007`, der Bestandsweg ist der
ausdrücklich gesetzte Wert `TAGESBILANZ`. Das muss in der XML-Doku beider Konstanten stehen —
Konzept 6.1 (Rev. 1) sagt noch das Gegenteil.

### 3.2 `SchemaKatalog` — wie Spalten definiert werden

`EPOS.Kern/Allgemein/Update/SchemaKatalog.cs:9-26`: `SchemaSpalte(Tabelle, Name, TypDefinition)`,
drei `readonly`-Felder. Das Muster Schritt 70 (`:1543-1547`, `:1559-1563`):

```csharp
public static readonly SchemaSpalte[] Schritt70_Auslegungstemperaturen =
{
    new SchemaSpalte(TAB_EINSTELLUNGEN, SPALTE_AUSLEG_T_KALT,  "DOUBLE"),  // nur anhängen!
    new SchemaSpalte(TAB_EINSTELLUNGEN, SPALTE_AUSLEG_T_HEISS, "DOUBLE"),
};
```

Die Typangabe ist **Access-Schreibweise** und wird erst beim Anlegen übersetzt:
`StilleDb.SqliteSpaltenTyp(spalte, accessTyp)` (`EPOS.Kern/Allgemein/Simulation/StilleDb.cs:234`)
bildet `DOUBLE|SINGLE|FLOAT|REAL|CURRENCY|DECIMAL|NUMERIC → REAL` (`:248-257`),
`LONG|INTEGER|INT|BYTE|COUNTER → INTEGER` (`:259-268`) und — **wichtig** —
`YESNO|BIT|BOOLEAN → INTEGER NOT NULL DEFAULT 0 CHECK ("spalte" IN (0,1))` (`:239-246`).
Der Boolean-Schalter `Aussenbauteile_Strahlung` bekommt also mit der Typangabe `"YESNO"` genau die
DDL, die Konzept 6.1 verlangt — **ohne** handgeschriebenes CHECK.

Zwei weitere Regeln aus dem Klassenkopf (`:40-44`) und `WechselrichterSchema.cs:33-38`:
**kein DDL-DEFAULT auf Fachwerten** (NULL ist die Vorgabe), und
**`sql/schema/001_grundschema.sql` bleibt unberührt** — sie ist der eingefrorene Access-Zielstand 61
(„NICHT VON HAND AENDERN"), eingebettete Ressource des Migrators. Gegenprobe: weder `PV_Modell`
(Schritt 64) noch `I_sc_max` (Schritt 70) noch `Tab_Nutzungsdauer` (Schritt 75) stehen darin.

### 3.3 `SchemaMigration` — Aufbau einer Schrittmethode

Die Datei liegt in der **Schale**: `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`.
Der Kern kennt nur die Zielzahl: `SchemaStand.Zielversion = 76`
(`EPOS.Kern/Allgemein/Update/SchemaStand.cs:93`), und `SchemaMigration.ZIEL_VERSION` leitet dorthin
weiter (`:108`).

**Das Rezept steht im Quelltext** (`SchemaMigration.cs:3499-3514`):

1. Nummer ab 62, lückenlos aufsteigend.
2. Der Schrittkörper benutzt **ausschließlich** `SqliteDdl` (`:3923`), `SqliteSpalteAnlegen`
   (`:4062`), `SqliteSpalteVorhanden` (`:4039`), `SqliteTabelleVorhanden` (`:4022`) —
   nie `Ddl`/`NonQuery`/`Scalar`, die auf `Lauf.Conn` arbeiten, und die ist im SQLite-Zweig `null`.
   Für DML gibt es `SqliteDml` (`:3934`).
3. **Erst** Schrittkonstante, Methode und `SCHRITTE_SQLITE`-Eintrag, **dann** `ZIEL_VERSION` anheben.

Die Schrittkonstante ist eine `public const int` mit ausführlicher XML-Doku
(`SCHRITT_75_NUTZUNGSDAUER = 75`, `:2762`; `SCHRITT_76_TRAEGERSATZ_EINDEUTIG = 76`, `:2816`).
Der Eintrag in `SCHRITTE_SQLITE` (`:3516`) trägt **vier** Stücke — Nummer, was der Schritt tut,
**was ohne ihn schiefginge**, und die Methode (`:3720-3730`).

`Schritt_76_TraegersatzEindeutig(Lauf l)` (`:4961-5004`) ist die Vorlage: er liest eine Zahl
(`SqliteZahl`, `:3980`), notiert sie (`l.Notiz`), führt bei Bedarf DML aus, prüft nach, setzt bei
Misserfolg `l.LetzterFehler` und gibt `false` zurück, und schließt mit einer `l.Notiz`, die in
einem Satz sagt, **dass sich kein Rechenergebnis ändert**. Die SQL-Texte selbst stehen **nicht**
in der Schale, sondern im Kern: `ProjektEnergietraegerEindeutig`
(`EPOS.Kern/Allgemein/Update/ProjektEnergietraegerEindeutig.cs:65`, `SQL_ENTDOPPELN:98`,
`SQL_INDEX:110`) — „EINE Quelle für Migration, Testdatenbank und Nachweis". Dasselbe Muster tragen
`NutzungsdauerSchema` und `SpeicherAuslegungStrict` (beide unter `EPOS.Kern/Allgemein/Update/`).

Der Spaltenschritt sieht aus wie `Schritt_70_PvStrangpruefung` (`:4672-4697`):

```csharp
foreach (SchemaSpalte s in SchemaKatalog.Schritt70_WrKurzschlussstrom)
    if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                             StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
```

**Sichten in einer Migration: gibt es bisher nicht.** `grep` über
`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` findet `CREATE VIEW` nur in
Kommentaren (`:1075`, `:1079`, `:1309`) — und dort geht es um den **Access**-Zweig (Schritt 36,
`SCHRITT_36_ENERGIETRAEGER_ABFRAGE = 36`, `:1333`), der über die ACE-Verbindung lief und
eingefroren ist. `DROP VIEW` kommt repoweit in **keiner** `.cs`- und keiner `.sql`-Datei vor.
`sql/schema/002_views.sql` wird zur Laufzeit **nicht ausgeführt** — kein Quelltext liest die Datei;
sie ist Generatorausgabe und Dokumentation.

**Folge:** Schritt 77 wird der **erste** Schritt des SQLite-Zweigs, der eine Sicht neu aufbaut. Der
Weg ist `SqliteDdl(l, "DROP VIEW IF EXISTS \"Abfrage_Projektgebaeude\"", …)` gefolgt von
`SqliteDdl(l, GebaeudeSchema.SQL_VIEW_PROJEKTGEBAEUDE, …)` — idempotent durch `IF NOT EXISTS`
bzw. das vorausgehende `DROP`. Die Definition gehört in den Kern (Abschnitt 6.3), damit Migration
und `TestDatenbank` dieselbe lesen; `002_views.sql` bleibt unberührt, und Konzept 6.2
(„das Schemaskript und der Migrationsschritt führen dieselbe Definition") ist an dieser Stelle
**zu korrigieren**.

### 3.4 Die drei Leser von `Tab_Gebaeude`

**Indexleser.** `ProjektGebaeudeCtrl.ReadAll(int ID_Projekt)`
(`EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:26`): `SELECT * FROM Abfrage_Projektgebaeude WHERE ID_Projekt = ?`
(`:29`), danach **58 Zuweisungen über `row[0] … row[57]`** (`:42-99`). `row[57]` ist
`Tab_Gebaeude.ID` → `item.ID_Gebaeude` (`:99`). Er ist der **einzige** Leser der Sicht — repoweit
gibt es außer `002_views.sql:89` und zwei Doku-Kommentaren keinen weiteren Treffer.

**Namensleser.** `GebaeudeCtrl.MapRowToModel(DataRow row)`
(`EPOS.Kern/Controller/GebaeudeCtrl.cs:50-110`): je Feld
`if (dt.Columns.Contains("X") && row["X"] != DBNull.Value) item.Y = Convert.ToZ(row["X"]);`.
Er liest `Tab_Gebaeude` **oder** `Tab_Gebaeude_STAMM` (`:55-57`: Namensfeld `Gebaeudename` bzw.
`Bezeichner`). `GebaeudeStammCtrl` hat denselben Leser (`:291` ist die Zwillingszeile).

**Die Namensfalle.** `GebaeudeCtrl.cs:66` und `GebaeudeStammCtrl.cs:291` bilden die Spalte
`Fensterflaeche_Ost_West` auf das Feld `Fensterflaeche_Ost` ab; `ProjektGebaeudeCtrl.cs:56` tut
dasselbe über `row[14]`. Gelesen wird das Feld in `SimulationWaermebedarf.cs:752`, `:756`, `:822`,
`:826` (Ost-/West-Anteil in `SolareGewinneC`); geschrieben in
`GebaeudeStammCtrl.BuildValueParams` (`:345`, Parameter `@b09`, `:358`) und in der Windows-Hülle
`WindowsFormsApplication1/Views/Gebäude/GebaeudeKatalogHuelle.cs:364`, `:443`, `:453`.
Deklariert ist es zweimal: `EPOS.Kern/Model/GebaeudeModel.cs:20` und
`EPOS.Kern/Model/ProjektGebaeudeModel.cs:26` (Vorbelegung `:76` bzw. `:88`).
**Insgesamt 15 Stellen** — sie müssen alle in einem Zug auf `Fensterflaeche_OstWest` umbenannt
werden, **bevor** die DB-Spalte `Fensterflaeche_Ost` existiert. Sonst zieht der Namensleser nach
Schritt 77 die neue, leere Spalte, und das Tagesmodell verliert seine Ost-/Westfenster ohne jede
Meldung.

### 3.5 `GebaeudeStammCtrl` — Katalogkopie und Schreibwege

| Methode | Zeile | Spaltenliste |
|---|---|---|
| `BuildValueParams(GebaeudeModel m)` | `:345` | 54 Parameter `@b00…` in **Positionsreihenfolge** |
| `Insert(GebaeudeModel m)` | `:406` | `INSERT INTO Tab_Gebaeude_STAMM (…54 Spalten…)` (`:409`) |
| `Overwrite(GebaeudeModel m)` | `:418` | `UPDATE … SET …53 Spalten… WHERE Bezeichner = ?` (`:426`) |
| `CopyFromStamm(string szBezeichner, int idProjekt, int idProjektGebaeude)` | `:439` | `INSERT INTO Tab_Gebaeude (…55 Spalten…)` (`:449`), Werte aus `SELECT * FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?` (`:441-443`) |

Alle vier führen **fest verdrahtete Spaltenlisten**. Eine neue Spalte, die im Projekt wirken soll,
muss in **allen** vieren mitlaufen — sonst verliert die Katalogübernahme sie still.

**Und hier sitzt die zweite Falle.** `CopyFromStamm` bildet jeden Wert so ab (`:458-470` und
weiter): `r["X"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["X"])`, bei Texten `""`.
Das macht aus „NULL = Vorgabe" im Katalog ein hartes 0 im Projekt. Für `Rahmenanteil` (Vorgabe 0,3),
`Verschattungsfaktor` (0,9), `Masseanteil_Aussen` (0,3), `Innenflaechenfaktor` (2,5) und
`Heizung_Strahlungsanteil` (0,3) wäre 0 **kein** neutraler Wert; für `Gebaeude_Modell` und
`Grundflaeche_Randbedingung` wäre `""` nicht dasselbe wie NULL (Abschnitt 3.1: NULL = `VDI6007`
bzw. `ERDREICH`).
**Die neuen Spalten sind deshalb NULL-erhaltend zu kopieren** — `r["X"] == DBNull.Value ?
(object)DBNull.Value : …` — und der Eingangsbauer muss die Vorgabe aus NULL **oder** aus einem
leeren Text ableiten. Ein Test dazu gehört in Abschnitt 5 (Konzept 10.3, „Katalogkopie").

### 3.6 Auslieferungsvorlage

`Werkzeuge/Auslieferungsvorlage` kennt **keine Spaltenlisten**: `Projektsicht.Spalten(string)`
(`Projektsicht.cs:95-103`) fragt `DataRepository.SpaltenVonTabelle`, `Projektsicht.Hat` (`:106`)
prüft namensbasiert, `Prueflauf.Textspalten` (`:193-201`) fragt `pragma_table_info`, und
`Vorlagenbau.KatalogeBereinigen` (`:128`) entscheidet nach dem Vorhandensein einer Spalte
`ReadOnly`. Neue Spalten in `Tab_Gebaeude(_STAMM)` und `Tab_Solar(_STAMM)` laufen automatisch mit.
**Kein Handgriff nötig** — auch nicht am Prüfbericht.

---

## 4. Ergebnisse und Referenzlauf

### 4.1 Die Ergebniskante im Kern

`SimulationErgebnisCtrl.Bedarf(SimulationWaermebedarf wb, SimulationStrombedarf sb)`
(`EPOS.Kern/Controller/SimulationErgebnisCtrl.cs:813`) füllt `BedarfErgebnis` (`:801-810`):

```csharp
e.WaermelastMaxKw       = wb.Waermebedarf_Max;        // :818  kW
e.WaermebedarfGesamtMwh = wb.Waermebedarf_Gesamt;     // :819  MWh
e.KanalMwh              = SimulationRunner.BedarfJeKanal(wb); // :820  MWh, Reihenfolge Kanal
```

`Kanal.HEIZUNG = 0`, `BRAUCHWASSER = 1`, `PROZESS = 2`, `ANZAHL = 3`
(`EPOS.Kern/Allgemein/Simulation/SimulationKanaele.cs:429-438`); `Kanalsatz.Heizung` ist
`Bedarf[Kanal.HEIZUNG]` (`:598`, `:620`). **Einen vierten Kanal gibt es nicht** — der Kühlbedarf
aus Konzept 4.5 kann also nur als informative Reihe geführt werden, nicht als Kanal.

`Tab_ErgebnisEnergiebedarf` (`sql/schema/001_grundschema.sql:850-862`) trägt elf Spalten:
`ID`, `ID_Ergebnis`, `Waermebedarf_Gesamt`, `Waermelast_Max`, `Strombedarf_Gesamt`,
`Strombedarf_Max`, `Waermerestbedarf`, `Stromrestbedarf`, `Waermebedarf_Heizung`,
`Waermebedarf_Brauchwasser`, `Waermebedarf_Prozess`. `ErgebnisCtrl` (`EPOS.Kern/Controller/ErgebnisCtrl.cs:17`)
schreibt sie in `Save` (`:83`) und liest sie in `Load` (`:736`); `TAB_ENERGIE` steht auf `:20`.
`:1117` trägt eine „einmalige, tolerante Migration", die fehlende Spalten ergänzt —
ein Muster, das für neue Gebäudekennzahlen **nicht** kopiert werden sollte (ADR-001: Schemaänderung
gehört in einen nummerierten Schritt).

**Empfehlung:** die Gebäudekennzahlen des Stundenmodells (drei Spitzenwerte, Kühlenergie,
Überhitzungsstunden, mittlere Raumtemperatur) gehen **nicht** in `Tab_ErgebnisEnergiebedarf`.
Sie sind je Gebäude, nicht je Projekt; die Tabelle ist einzeilig je Lauf. Der Weg ist derselbe wie
bei den Emissionsgrößen (Konzept „Weg A", `Referenzlauf/Ergebnisexport.cs:284-287`): **Skalare in
`aggregate.csv`**, keine Spalte.

### 4.2 `Referenzlauf/Ergebnisexport.cs`

`ProjektAusfuehren(int idProjekt, string zielOrdner, Protokoll log)` (`:34`) rechnet über
`SimulationRunner.SimuliereUndSpeichere` (`:42`) und schreibt dann Vektoren:

```csharp
dateien += Vektor(zielOrdner, "waermebedarf.csv",          wb.Waermebedarf,          summen); // :58
dateien += Vektor(zielOrdner, "waermebedarf_gebaeude.csv", wb.Waermebedarf_Gebaeude, summen); // :59
dateien += Vektor(zielOrdner, "stundentemperatur.csv",     wb.Stundentemperatur,     summen); // :64
```

`Vektor` (`:573-592`) schreibt `Index;Wert` mit UTF-8-BOM, bildet die Summe und legt sie als
`Vektor.<dateiname>.Summe` in `summen` ab (`:590`, ausgegeben `:428-429`).

**Achtung — eine Einheiteninkonsistenz im Bestand:** `wb.Waermebedarf_Gebaeude` steht in **Watt**
(Abschnitt 1.1: die Addition `:208` geschieht vor `WattToKw:222`), während `waermebedarf.csv` in
kWh steht. `waermebedarf_gebaeude.csv` der Basis führt also Watt. Das ist eingefroren und darf
nicht angefasst werden.

**Wo die neuen Reihen hingehören:** unmittelbar nach `:64`, in einem Block, der **nur** läuft,
wenn das Projekt mindestens ein VDI-6007-Gebäude führt — dasselbe Muster wie die bedingten
Modulblöcke (`:70`, `:110`, `:120`, `:130`, `:140`) und wie der Erdreichblock, der ohne Erdreich
**keinen einzigen Eintrag** erzeugt (`:247-253`, Begründung `:249-251`):

```csharp
// nur für VDI-6007-Gebäude, sonst bleibt der Bestandsordner byte-gleich
dateien += Vektor(zielOrdner, "raumtemperatur.csv",        geb.Raumtemperatur,       summen);
dateien += Vektor(zielOrdner, "operative_temperatur.csv",  geb.OperativeTemperatur,  summen);
dateien += Vektor(zielOrdner, "kuehlbedarf.csv",           geb.Kuehlbedarf,          summen);
```

Bei mehreren Gebäuden je Projekt braucht der Dateiname einen Index — Vorbild
`"quellspeicher_" + kennung + "_soc.csv"` (`:99`). Vorschlag: `raumtemperatur_<n>.csv` mit `n` =
Schleifenindex des Gebäudes, damit die Zuordnung reproduzierbar bleibt.

**Skalare** in `aggregate.csv` nach dem Muster `Erdreich[i].…` (`:256-271`), Präfix `Geb[i].`:
`Geb[i].ID_Gebaeude`, `Geb[i].Modell`, `Geb[i].SpitzeKw`, `Geb[i].SpitzeTagesmittelKw`,
`Geb[i].Spitze95Kw`, `Geb[i].KuehlenergieKwh`, `Geb[i].StundenMitKuehlbedarf`,
`Geb[i].MittlereRaumtemperaturHeizzeit`, `Geb[i].HeizwaermeKwh`. Einheit im Namen
(Hausregel `EPOS.Kern/CLAUDE.md`, Abschnitt „Einheiten", Punkt 3).

### 4.3 Wie `vergleich` mit neuen Dateien umgeht — **das ist der harte Punkt**

`Referenzlauf/Vergleich.cs:41`. Toleranzen `TOLERANZ_RELATIV = 1e-4`, `TOLERANZ_ABSOLUT = 0.01`
(`:43-44`). Der Ausschluss `--ohne` wirkt **nur auf Schlüssel innerhalb einer Datei**
(`_ausgenommen`, `:61-62`, gefüllt `:76-79`, Klassenkopf `:47-60`: „Eine Etappe, die eine neue
ERGEBNISSPALTE einführt, erweitert damit die Schlüsselliste in `aggregate.csv`").

Eine **Datei**, die nur im neuen Lauf liegt, wird dagegen so behandelt (`:183-190`):

```csharp
if (!refDateien.ContainsKey(datei))
    ergebnis.Add(new Abweichung {
        Datei = datei, Schluessel = "-", Schwere = double.MaxValue,
        Beschreibung = "Datei nur im Vergleichslauf vorhanden" });
```

`Schwere = double.MaxValue` ⇒ FAIL, und es gibt keinen Schalter dagegen.

**Folge für die Planung:** Sobald ein Referenzprojekt stündlich rechnet, entstehen neue CSV-Dateien,
und der Vergleich gegen die alte Basis ist zwangsläufig rot. Das ist mit E1 ohnehin so vorgesehen
(die Basis wird mit G1 vollständig neu eingefroren). Aber es heißt auch: **die Abnahmesperre von GB
muss vor jeder Zeile G1-Code laufen**, und der Regressionstest aus N1.1 („jedes Referenzprojekt
ausdrücklich auf `TAGESBILANZ` setzen und gegen die letzte Bestandsbasis halten") funktioniert nur,
wenn die drei neuen Reihen für `TAGESBILANZ`-Gebäude **gar nicht** geschrieben werden — nicht
„mit Nullen gefüllt". Der bedingte Block aus 4.2 ist damit keine Bequemlichkeit, sondern Bedingung.

`EPOS.Referenzlauf` und das Windows-Werkzeug teilen sich **eine** Fassung: `Ergebnisexport.cs`,
`Vergleich.cs`, `Plausibilitaet.cs`, `Projektauswahl.cs`, `Protokoll.cs`, `DbUmgebung.cs` sind in
`EPOS.Referenzlauf/EPOS.Referenzlauf.csproj:42-51` per `<Compile Include="..\Referenzlauf\…" Link="…" />`
eingebunden. Eine Änderung wirkt auf beiden Wegen.

---

## 5. Tests

### 5.1 Reine Rechenproben

Vorbild `EPOS.Kern.Tests/PvKoeffizientenTests.cs`: keine Sammlung, keine Datenbank, ein privater
Erbauer für die Eingangsgröße (`:15-36`), dann `[Fact]`-Methoden mit sprechenden deutschen Namen
und `Assert.Equal(erwartet, ist, 9)` (`:43-44`). Genau diese Form brauchen `Zonenmodell2K` und
`ErsatzparameterRC` (Konzept 10.2): Grenzfälle, Skalierung, Determinismus, Eigenwerte,
Lastbestimmung, Vorlaufkonvergenz, benannte Fehler.

### 5.2 Datenbankfälle

`[Collection("Testdatenbank")]` + `IClassFixture<TestDatenbank>` —
`EPOS.Kern.Tests/GebaeudeBedarfCtrlTests.cs:30-35`. Die Sammlung ist definiert in
`EPOS.Kern.Tests/TestDatenbank.cs:29` (`[CollectionDefinition("Testdatenbank")]`) und ist die
**eine serielle Sammlung**: wer die Testdatenbank benutzt **oder** ein `Dienste.*` tauscht, gehört
hinein; der Wächter `DiensteSammlungTests` prüft es (Kopf `:17-27`).
Fehlt die Datei, schweigen die Fälle (`Vorhanden`, `:98-152`); eine LFS-Zeigerdatei fällt über
`LfsZeigerProbe.IstZeiger` (`:44-60`) mit klarer Meldung auf.

**Kulturvorrichtung:** `EPOS.Kern.Tests/Kulturvorrichtung.cs:29` pinnt **alle vier** Kulturwerte
auf `de-DE` (`:38-50`). Jede Testklasse mit deutschen Ressourcentexten führt sie.
`GebaeudeBedarfCtrlTests` hat dafür eine eigene kleine `DeutscheOberflaeche` (`:41-52`).

**Schemapflege der Arbeitskopie.** `TestDatenbank` zieht das Schema selbst nach
(`:197-241`): je Schritt ein `foreach (SchemaSpalte s in SchemaKatalog.SchrittNN_…) SpalteSicherstellen(s);`
(`:197`, `:204`), für Tabellen der Aufruf der Kern-Schemaklasse (`:208`, `:216`, `:224-239`), und
zuletzt `UPDATE Tab_Applikation SET SchemaVersion = SchemaStand.Zielversion` (`:241`).
`SpalteSicherstellen` (`:268-273`) prüft `DataRepository.SpalteVorhanden` und legt sonst über
`StilleDb.SqliteSpaltenTyp` an. **Schritt 77 und der Tab_Solar-Schritt müssen hier eingetragen
werden** — sonst laufen die Datenbankfälle auf einer Kopie ohne die neuen Spalten.
Für die Sicht gilt: sie wird über `DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_…)`
nachgezogen, nach dem Muster der `NutzungsdauerSchema.Anweisungen`-Schleife (`:224-225`).

**Migrationstest.** `EPOS.Kern.Tests/Migration74Tests.cs` ist die Vorlage: Teil 1 prüft **Texte**
ohne Datenbank (`Assert.True(SchemaStand.Zielversion >= 74, …)`, `:34-40`), Teil 2 den Umbau auf
der Arbeitskopie. Für Schritt 77 heißt das: Zielstand ≥ 77, Spaltenliste vollständig, Sicht führt
die neuen Spalten **hinter** `Tab_Gebaeude.ID`, `ProjektGebaeudeCtrl` liest alle 58 Bestandsfelder
unverändert, Wiederholbarkeit.

### 5.3 Die vier Wächter — reißt das neue Modell sie?

| Wächter | Umfang | Wirkung auf `Simulation/Gebaeude/` |
|---|---|---|
| `DoubleWacheTests` (`EPOS.Kern.Tests/DoubleWacheTests.cs:39`) | alle `.cs` unter `EPOS.Kern/Allgemein/Simulation/` **mit Unterordnern** (`SearchOption.AllDirectories`, `:206`) plus `BhkwPlan.cs` (`:203`) | **greift**. Kein `float`, kein `Convert.ToSingle`, kein `MathF.`, kein `f`-Suffix (`Floatstelle`, `:50-55`). Ausnahmeliste ist **leer** (`:63-64`) und soll es bleiben. `System.Numerics.Complex` (G3) ist `double`-basiert und unkritisch. |
| `EinheitenWacheTests` (`:…`) | Wächter 1: Faktor 1000 in Anzeige und Hüllen; Wächter 2: Jahressummen der **sieben namentlich gelisteten** Simulationsklassen (`Simulationsklassen`, `EinheitenWacheTests.cs:201-206`) | **greift nur halb.** Eine neue Datei unter `Simulation/Gebaeude/` steht **nicht** in der Liste — der Namenswächter sieht sie nicht. Die Liste ist um `GebaeudeModellErgebnis.cs` zu **erweitern**, sonst entsteht eine stille Lücke. Regeln: Skalarfelder (`Skalarfeld`, `:217-219`), Energienamen (`bedarf|verbrauch|produktion|ertrag|summe|gesamt|rest`, `:225-227`), Suffix `Kwh`/`Mwh` (`:230-231`). Stundenreihen (`double[8760]`) sind bewusst draußen (`:212-215`). |
| `RechenrandTests` (`EPOS.Kern.Tests/RechenrandTests.cs:29`) | prüft `Rechenrand.Zu` und `SchwelleErreicht` selbst (`EPOS.Kern/Allgemein/Simulation/Rechenrand.cs:78`, `:95`), nicht den Bestand | **greift nicht automatisch**, aber die Regel gilt: Die Kappung an `Maximaleraumtemperatur`, die Leistungsgrenze `Heizleistung_Max` und die Bisektion des Umschaltzeitpunkts (Konzept 4.5) sind Betriebsschwellen und nehmen `Rechenrand.SchwelleErreicht`, nicht `>=`. |
| `ParallelitaetWacheTests` (`EPOS.Kern.Tests/ParallelitaetWacheTests.cs`) | vier plattformfreie Projekte, sieben Muster (`Wege`, `:75-84`) | **greift**. Der Löser ist ohnehin einfädig; ein `Parallel.For` über die Gebäude wäre nur über `SpeicherEngine/Kulturweitergabe` erlaubt — und widerspräche dem Determinismus-Versprechen (Konzept 4.8). Nicht tun. |

Zwei weitere, die der Plan trifft:

- `DokumentationLinkWacheTests` (`:133` Verweise, `:170` **„Der_Index_nennt_jedes_Papier"**):
  **jede** neue Datei unter `Dokumentation/aktuell/` braucht eine Zeile in
  `Dokumentation/LIESMICH.md`, und jeder relative Verweis muss ein Ziel haben.
- `HuellenwegTests` (`EPOS.UI.Tests/HuellenwegTests.cs`): kein modales Systemfenster im
  Blazor-Ereignis. Betrifft die Gebäudehülle beim Umzug nach `EPOS.UI.Daten` (Konzept 8.3).

### 5.4 Nicht auszuliefernde Prüfdaten (Normzahlen)

**Ein Muster für lokal beizustellende, gitignorierte Prüfdaten gibt es heute nicht.** `.gitignore`
kennt `dev/` (`:383`), `.work/` (`:376`), Arbeitskopien unter `Referenzlaeufe/Arbeitskopie/`
(`:377`), Schlüsseldateien (`api_gemini.txt`, `*.apikey`) und Datenbankbeidateien — aber keinen
Ort für Prüfdaten. `Referenzlaeufe/Importproben/` ist das Gegenteil: eingefroren, versioniert,
„gehört zum Testbestand und wird nie gelöscht" (`Referenzlaeufe/LIESMICH.md:127-130`).

**Das vorhandene Muster für „Datei fehlt ⇒ Fall schweigt" ist `TestDatenbank`**
(`EPOS.Kern.Tests/TestDatenbank.cs:98-152`): Die Vorrichtung sucht die Datei aufwärts vom
Laufordner (`Quelle()`, `:280-286`), setzt `Vorhanden` und jeder Fall beginnt mit
`if (!_db.Vorhanden) return;`. **Genau das ist die Bauform für `GebaeudeModellNormfallTests`:**

- eine Vorrichtung `Normzahlen` mit `Vorhanden`, die eine Datei sucht (Vorschlag:
  `Referenzlaeufe/Normzahlen/vdi6007_blatt1_anhang_a1.csv`) — ohne Datei schweigt jeder Fall;
- ein neuer `.gitignore`-Eintrag `Referenzlaeufe/Normzahlen/` (mit begründendem Kommentar, wie
  die Einträge daneben);
- ein `Referenzlaeufe/Normzahlen/LIESMICH.md`, **versioniert**, das sagt, woher die Zahlen kommen
  (VDI 6007 Blatt 1:2015-06, Tabellen A1.3…A12.3, Seiten 41–63) und wie sie einzutragen sind —
  ohne eine einzige Zahl;
- ein **versionierter** Gegenwächter, der genau das prüft, was ohne die Zahlen prüfbar ist:
  Bandbreite, Prüfregel `± 0,1 K` / `± 1 W`, Vorzeichenkonvention (Heizlast positiv), die
  Bestandsprobe „die Vorrichtung sucht wirklich" — Vorbild
  `DoubleWacheTests.Der_Waechter_sieht_den_Bestand_und_jede_Ausnahme_existiert` (`:137`);
- die CI sieht die Datei nie, also müssen die Normfälle in `kern.yml` **schweigend** durchlaufen.
  Das ist eine bewusste Lücke im Gate und gehört ins Protokoll: der Nachweis der zwölf Testfälle
  ist ein **lokaler** Nachweis, kein CI-Nachweis. Der Auszug des Laufs (Abweichung je Fall, ohne
  Absolutwerte) gehört als Tabelle in die Dokumentation — Zahlen, die nichts rekonstruierbar machen.

Die Alternative — Normzahlen versioniert wie `Importproben` — verstößt gegen die Vorbemerkung der
Richtlinie (Konzept N1.2, „das Ausliefern der Normzahlen … ist eine Vervielfältigung").
`Referenzlaeufe/Kenndaten_Test.sqlite` liegt zwar in LFS, ist aber öffentlich im Klon; LFS ist
**keine** Zugriffsbeschränkung.

---

## 6. Der Einbindungsplan

### 6.1 Neue Dateien — `EPOS.Kern/Allgemein/Simulation/Gebaeude/`

Alle vier ohne `DataRepository`, ohne `SimulationProtokoll`, ohne Statik, durchgehend `double`
(Vorbild `EPOS.Kern/Allgemein/Simulation/PvErweitertesModell.cs`).

**`ErsatzparameterRC.cs`** — die reduzierten RC-Größen eines Gebäudes (Konzept 4.2/4.3):

```csharp
internal sealed record ErsatzparameterRC(
    double C_AW_Jk,        double C_IW_Jk,        // Wärmekapazitäten [J/K]
    double R_Rest_AW_KW,   double R_1_AW_KW,      // Widerstände [K/W]
    double R_1_IW_KW,      double R_conv_AW_KW,
    double R_conv_IW_KW,   double R_rad_KW,
    double R_ext_KW,                              // H_ve + U_w·A_w + Σψ·L
    double A_AW_opak_M2,   double A_IW_M2,        // Bezugsflächen [m²]
    double SummeUA_opak_WK);                      // Σ(U·A)_opak [W/K] für θ_eq
```
dazu `internal static ErsatzparameterRC AusKlassenweg(GebaeudeModellEingang e)` mit den harten
Prüfungen aus Konzept 4.8 (`R_Rest_AW > 0` mit benanntem Fehler, `5 ≤ Bauweise/Wohnflaeche ≤ 200`,
U-Werte 0,1…6, `0 < g ≤ 1`).

**`Zonenmodell2K.cs`** — der Löser. **Name nach Norm** (Nachtrag N1.3: die Richtlinie sagt
„2-K-Modell", „7R2C" ist nur Kurzform); Konzept 4.1 und 11 nennen noch `Zonenmodell7R2C` und sind
an dieser Stelle zu korrigieren.

```csharp
internal sealed class Zonenmodell2K
{
    internal Zonenmodell2K(ErsatzparameterRC p);   // baut A, b-Struktur, Φ, Γ, Ψ einmal
    internal double[] Eigenwerte { get; }          // beide reell und negativ (Probe 10.2)
    internal void Zuruecksetzen(double thetaStart);
    internal Stundenergebnis Schritt(in Stundenrand r);  // eine Blockstunde
}

internal readonly struct Stundenrand
{
    internal readonly double ThetaOut, ThetaEq, ThetaSoll, ThetaMax;
    internal readonly double PhiRadAW, PhiRadIW, PhiConv;   // [W]
    internal readonly double HeizleistungMaxW;              // NaN = unbegrenzt
    internal readonly double HeizungStrahlungsanteil;
}

internal readonly struct Stundenergebnis
{
    internal readonly double HeizleistungW, KuehlleistungW;  // Blockmittel, beide ≥ 0
    internal readonly double ThetaAirMittel, ThetaOpMittel;
    internal readonly double ThetaMAwEnde, ThetaMIwEnde;
}
```
Zustand je Instanz (zwei `double`), keine Statik, keine Datenbank, kein `float`.

**`GebaeudeModellEingang.cs`** — baut aus `ProjektGebaeudeModel` und den Klimareihen die 8 760
Randbedingungen:

```csharp
internal sealed class GebaeudeModellEingang
{
    internal static GebaeudeModellEingang Bauen(
        ProjektGebaeudeModel gebaeude,
        IReadOnlyList<SolardatenModel> solarOrtszeit,  // aus ReadOrtszeit, 8760 Zeilen
        bool[] wochenende,                             // WE[365] aus KlimakalenderLesen
        double laengengrad, double breitengrad);

    internal double[] ThetaOut { get; }        // [°C], 8760
    internal double[] ThetaEq { get; }         // [°C], 8760 — U·A-gewichtet
    internal double[] PhiSolarAW { get; }      // [W], 8760
    internal double[] PhiSolarIW { get; }      // [W]
    internal double[] PhiSolarLuft { get; }    // [W] — a_kon-Anteil
    internal double[] PhiIntern { get; }       // [W]
    internal double[] ThetaSoll { get; }       // [°C], 8760
    internal double[] ThetaMax { get; }        // [°C], 8760
    internal ErsatzparameterRC Parameter { get; }
}
```
Sie ruft `SolarCalculator.CalculateHourlyHayDavies` je Orientierung mit
`Tilt = 90`, `Azimuth ∈ {0, −90, 180, 90}` und `dayOfYear = zeile.TagUtc`, `hour = zeile.StundeUtc`
(Azimutkonvention: `KlimaImportAblauf.cs:135`; die Stundenmitte-Frage aus Abschnitt 2.3 entscheidet
hier, an **einer** Stelle). Die Erdreichtemperatur kommt aus dem Bestand:
`ErdreichTemperatur.JahresprofilKollektor(thetaOut, 1.0, DbWerte.BODENTYP_SAND_FEUCHT)`
(`EPOS.Kern/Allgemein/Simulation/ErdreichTemperatur.cs:411`) — mit λ = 1,4 W/(mK) und
ρ·c_p = 1,90 MJ/(m³K) (`:185`) ist a = 0,74 mm²/s = 0,064 m²/d, Dämpfungstiefe 2,72 m, also
Dämpfung `exp(−1/2,72) = 0,69` und Phasenverzug `(1/2,72)/(2π)·365 = 21,4 Tage` — die Zahlen aus
Konzept 4.4 (0,68 / 22 Tage) auf zwei Stellen. **Kein neuer Kusuda-Code.**

**`GebaeudeModellErgebnis.cs`** — Reihen und Kennzahlen:

```csharp
internal sealed class GebaeudeModellErgebnis
{
    internal double[] HeizlastW { get; }              // 8760, Watt — geht in den Zielvektor
    internal double[] Raumtemperatur { get; }         // 8760 [°C]
    internal double[] OperativeTemperatur { get; }    // 8760 [°C]
    internal double[] KuehlbedarfW { get; }           // 8760 [W]
    internal double JahresheizwaermeKwh { get; }
    internal double SpitzeKw { get; }
    internal double SpitzeTagesmittelKw { get; }      // Mittel über 24 Blockstunden
    internal double Spitze95Kw { get; }
    internal double KuehlenergieKwh { get; }
    internal int StundenMitKuehlbedarf { get; }
    internal double MittlereRaumtemperaturHeizzeit { get; }
}
```
**Einheit im Namen** — sonst reißt der Namenswächter, sobald die Klasse in
`EinheitenWacheTests.Simulationsklassen` aufgenommen ist (Abschnitt 5.3).

**`Bauteilreduktion.cs`** kommt erst mit G3 (Kettenmatrix, `System.Numerics.Complex`) und ist hier
nur als leerer Platz vorgemerkt.

### 6.2 Geänderte Dateien — Stelle und Art

| Datei:Zeile | Art der Änderung | Stufe |
|---|---|---|
| `EPOS.Kern/Allgemein/BhkwPlan.cs:51`, `:54`, `:399`, `:433` | `_prevRoomTemp` von `static` auf Instanzzustand; `ResetState` je Gebäude. **Ändert 1008 und 1039.** | **GB** |
| `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:684-733` | Ferienmaske: Warnung statt stiller Fehlgriff bei `Ferienbeginn_1 > Ferienende_1`, `-1` einheitlich (Q18) | **GB** |
| `Referenzlaeufe/Kenndaten_Test.sqlite` | `Bauweise` von Gebäude 10576 auf 15 200 Wh/K (E4/Q22) | **GB** |
| `Referenzlaeufe/LIESMICH.md` nach `:100-121` | **vierte** Einfrierregel „gesäte Gebäudedaten" im Muster der dritten | **GB** |
| `CLAUDE.md`, Abschnitt „Regressionsnetz" | vierter Spiegelstrich in der Einfrierliste | **GB** |
| `EPOS.Kern/Model/GebaeudeModel.cs:20`, `:76`; `ProjektGebaeudeModel.cs:26`, `:88`; `GebaeudeCtrl.cs:66`; `GebaeudeStammCtrl.cs:291`, `:358`; `ProjektGebaeudeCtrl.cs:56`; `SimulationWaermebedarf.cs:752`, `:756`, `:822`, `:826`; `GebaeudeKatalogHuelle.cs:364`, `:443`, `:453` | **`Fensterflaeche_Ost` → `Fensterflaeche_OstWest`**, 15 Stellen, **vor** Schritt 77 | **G1, zuerst** |
| `EPOS.Kern/Allgemein/DbWerte.cs` nach `:2199` | fünf Persistenzwerte (Abschnitt 3.1), NULL-Bedeutung nach E1 | G1 |
| `EPOS.Kern/Allgemein/Update/` **neu** `GebaeudeSchema.cs` | Spaltennamen, `SchemaSpalte[]`, `SQL_VIEW_PROJEKTGEBAEUDE` — eine Quelle für Migration, Testdatenbank, Nachweis | G1 |
| `EPOS.Kern/Allgemein/Update/SchemaKatalog.cs` | `TAB_GEBAEUDE`/`TAB_GEBAEUDE_STAMM`-Konstanten, Verweis auf `GebaeudeSchema` | G1 |
| `EPOS.Kern/Allgemein/Update/SchemaStand.cs:93` | `Zielversion` 76 → 79 (**zuletzt**, nach Konstante, Methode und Eintrag) | G1 |
| `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` | drei Schrittkonstanten, drei Methoden, drei `SCHRITTE_SQLITE`-Einträge (`:3731` davor) | G1/G2 |
| `EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:42-99` | **Indexleser → Namensleser** (Muster `GebaeudeCtrl.MapRowToModel`), danach die neuen Spalten | G1 |
| `EPOS.Kern/Controller/GebaeudeCtrl.cs:50-110`, `GebaeudeStammCtrl.cs:291 ff.` | elf neue Felder namensbasiert | G1 |
| `EPOS.Kern/Controller/GebaeudeStammCtrl.cs:345`, `:409`, `:426`, `:449` | elf Spalten in **allen vier** Listen; in `CopyFromStamm` **NULL-erhaltend** (Abschnitt 3.5) | G1 |
| `EPOS.Kern/Model/GebaeudeModel.cs`, `ProjektGebaeudeModel.cs` | elf `double?`/`string`-Felder (nullbar, damit „nicht gesetzt" ankommt) | G1 |
| `SimulationWaermebedarf.cs:513-535` | `KlimakalenderLesen` behält die Ortszeit-Solarreihe (statt sie in `:914-920` wegzuwerfen) | G1 |
| `SimulationWaermebedarf.cs:581` | **die Verzweigung**: `if (Modell(item) == VDI6007) { … } else Berechnung_Gebaeude_Tageswerte(item, index); ` | G1 |
| `SimulationWaermebedarf.cs:598-608` | im VDI-Zweig: `VectorInit(ziel)` und Übernahme von `GebaeudeModellErgebnis.HeizlastW` statt `StdWerte` | G1 |
| `SimulationWaermebedarf.cs:645-653` | Verbrauchsrückrechnung: `VerbrauchAlt` aus dem Stundenmodell, `VerbrauchAlt > 0` benannt geprüft | G1 |
| `SimulationWaermebedarf.cs` neu | Feld `List<GebaeudeModellErgebnis> GebaeudeErgebnisse` (statt `MaxP`, das tot ist) | G1 |
| `EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs:21-47`, `:94-136` | `GebaeudeBedarfErgebnis` um Reihen und Kennzahlen erweitern; `Rechnen` gibt sie durch | G1/G2 |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor`, `GebaeudeKatalogDialog.razor`, `GebaeudeDaten.cs`, `GebaeudeKatalogDaten.cs` | Gruppe „Hülle und Rechenmodell" (E2: je Bauteilgruppe U, A, U·A, Randbedingung; Summen H_T, H_ve, H_ges) | G1 |
| `WindowsFormsApplication1/Views/Gebäude/GebaeudeHuelle.cs:335`, `:342` | Umzug nach `EPOS.UI.Daten/Bedarf/GebaeudeHuelle.cs` (Konzept 8.3); `HuellenwegTests` gilt | G1 |
| `EPOS.Kern/MyResource/Resource.resx` + `.en-US.resx`, danach `Werkzeuge/ResourceDesigner` | alle neuen Schlüssel, beide Sprachen | G1 |
| `EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:57-89`, `:602-635` | `IR(h)` (+ optional `WD10m`, `SP`) lesen; `RH`, `WS10m`, `IR(h)` schreiben | G1 |
| `EPOS.Kern/Controller/SolardatenCtrl.cs:29-42` | drei Spalten im `MapDataRowToModel`; `SolardatenModel` drei Felder | G1 |
| `Referenzlauf/Ergebnisexport.cs` nach `:64` und nach `:273` | drei bedingte Vektoren, `Geb[i].*`-Skalare (Abschnitt 4.2) | G1 |
| `EPOS.Kern.Tests/TestDatenbank.cs:197-241` | drei Schritte nachziehen, Sicht neu bauen | G1 |
| `EPOS.Kern.Tests/EinheitenWacheTests.cs:201-206` | `GebaeudeModellErgebnis.cs` in `Simulationsklassen` | G1 |
| `Dokumentation/LIESMICH.md` | Indexzeilen für dieses Papier und das Umsetzungskonzept (`DokumentationLinkWache`) | sofort |

### 6.3 Schemaschritt 77 — vollständig

**Konstante** (`SchemaMigration.cs`, nach `:2816`):
`public const int SCHRITT_77_GEBAEUDEMODELL = 77;` mit XML-Doku, die sagt: elf Spalten in beiden
Tabellen, **NULL = VDI 6007** (E1), Sicht neu, kein DML — und dass dieser Schritt **selbst**
ergebnisneutral ist: er legt Spalten an; der Rechenweg wechselt erst, wenn der Code der Stufe G1
sie liest.

**Spaltenteil** — neue Kernklasse `EPOS.Kern/Allgemein/Update/GebaeudeSchema.cs`:

```csharp
public static readonly SchemaSpalte[] Schritt77_Gebaeudemodell =
{
    new SchemaSpalte(TAB_GEBAEUDE, "Gebaeude_Modell",            "TEXT(20)"),
    new SchemaSpalte(TAB_GEBAEUDE, "Fensterflaeche_Ost",         "DOUBLE"),
    new SchemaSpalte(TAB_GEBAEUDE, "Fensterflaeche_West",        "DOUBLE"),
    new SchemaSpalte(TAB_GEBAEUDE, "Rahmenanteil",               "DOUBLE"),
    new SchemaSpalte(TAB_GEBAEUDE, "Verschattungsfaktor",        "DOUBLE"),
    new SchemaSpalte(TAB_GEBAEUDE, "Grundflaeche_Randbedingung", "TEXT(20)"),
    // … und dieselben elf noch einmal für TAB_GEBAEUDE_STAMM
};
```
(vollständig 22 Einträge: die elf aus Konzept 6.1 × zwei Tabellen).

| Spalte | Typangabe | SQLite daraus | NULL bedeutet |
|---|---|---|---|
| `Gebaeude_Modell` | `TEXT(20)` | `TEXT` | **`VDI6007`** (E1) |
| `Fensterflaeche_Ost` | `DOUBLE` | `REAL` | ½ `Fensterflaeche_Ost_West` |
| `Fensterflaeche_West` | `DOUBLE` | `REAL` | ½ `Fensterflaeche_Ost_West` |
| `Rahmenanteil` | `DOUBLE` | `REAL` | 0,3 |
| `Verschattungsfaktor` | `DOUBLE` | `REAL` | 0,9 |
| `Grundflaeche_Randbedingung` | `TEXT(20)` | `TEXT` | `ERDREICH` |
| `Masseanteil_Aussen` | `DOUBLE` | `REAL` | 0,3 |
| `Innenflaechenfaktor` | `DOUBLE` | `REAL` | 2,5 |
| `Heizung_Strahlungsanteil` | `DOUBLE` | `REAL` | 0,3 |
| `Heizleistung_Max` | `DOUBLE` | `REAL` (kW) | unbegrenzt |
| `Aussenbauteile_Strahlung` | **`YESNO`** | `INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))` | — (Schalter) |

Die Boolean-Übersetzung kommt gratis aus `StilleDb.SqliteSpaltenTyp:239-246`; sie ist der einzige
Grund, warum die Typangaben in **Access**-Schreibweise stehen bleiben.

**Sichtteil** — `GebaeudeSchema.SQL_VIEW_PROJEKTGEBAEUDE`: wörtlich die Definition aus
`sql/schema/002_views.sql:90-91`, ergänzt um die elf `Tab_Gebaeude.<Spalte>` **hinter**
`Tab_Gebaeude.ID`. Damit bleibt `row[57] = ID` (`ProjektGebaeudeCtrl.cs:99`) gültig und der
Indexleser überlebt den Schritt — auch wenn er im selben Merge auf Namen umgestellt wird
(Gurt und Hosenträger).

**Schrittmethode** (`SchemaMigration.cs`, nach `:5004`):

```csharp
private static bool Schritt_77_Gebaeudemodell(Lauf l)
{
    foreach (SchemaSpalte s in GebaeudeSchema.Schritt77_Gebaeudemodell)
        if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                 StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

    if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_DROP,  "Sicht " + GebaeudeSchema.VIEW)) return false;
    if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_NEU,   "Sicht " + GebaeudeSchema.VIEW)) return false;

    l.Notiz("77: … KEIN Rechenergebnis aendert sich durch diesen Schritt.");
    return true;
}
```
`SQL_VIEW_DROP` ist `DROP VIEW IF EXISTS "Abfrage_Projektgebaeude"` — damit ist der Schritt
wiederholbar.

**`SCHRITTE_SQLITE`-Eintrag** (vor der schließenden Klammer `:3731`), vier Stücke nach dem Muster
`:3720-3730`; das „was ohne ihn schiefginge" lautet: *Die neuen Spalten erreichten den Leser nicht —
die Sicht hat eine feste Spaltenliste, und SQLite kennt kein `ALTER VIEW`. Das Gebäudemodell liefe
für jedes Gebäude auf die Vorgabewerte, ohne dass eine Eingabe des Anwenders je ankäme.*

**Zielversion** zuletzt: `SchemaStand.Zielversion` auf 77 (bzw. 79 nach Schritt 78/79).

**Katalogkopie und Namensleser** gehören in **denselben** Merge wie Schritt 77 — sonst existiert
ein Stand, in dem die Spalten da sind und keine Lesekette sie sieht.

### 6.4 Schemaschritt 78 — die drei G2-Spalten

`Luftwechsel_Infiltration` (`DOUBLE`, NULL = 0,3 1/h), `Luftwechsel_Nutzer` (`DOUBLE`, NULL = 0,4),
`Sommerlueftung` (`YESNO`) — je beide Tabellen, also sechs Einträge. Gleiche Bauform wie 77, ohne
Sicht**neubau**: die Sicht muss aber ein **zweites Mal** neu gebaut werden, damit die drei Spalten
den Leser erreichen. Da G1 und G2 nach E1 gemeinsam ausgeliefert werden, ist die einfachere
Variante, **77 und 78 zu einem Schritt zu verschmelzen** (14 Spalten, eine Sicht, ein Neuaufbau).
**Empfehlung: verschmelzen** — zwei Sichtneuaufbauten hintereinander sind zwei Gelegenheiten, die
Definitionen auseinanderlaufen zu lassen, und Konzept 6.1 hat die Trennung nur vorgesehen, solange
G2 eine eigene Auslieferung war.

### 6.5 Schemaschritt für `Tab_Solar` — **eigener Schritt, ja**

Drei Spalten, beide Tabellen (`Tab_Solar`, `Tab_Solar_STAMM`), je `DOUBLE → REAL`:

| Spalte | Quelle | Einheit | NULL bedeutet |
|---|---|---|---|
| `Gegenstrahlung` | PVGIS `IR(h)` | W/m² | Schätzung nach Blatt 3 Gl. (84)–(88) aus der Sonnenwahrscheinlichkeit |
| `Windgeschwindigkeit` | PVGIS `WS10m` | m/s | nicht verfügbar (h_a bleibt 25 W/(m²K)) |
| `Luftfeuchte` | PVGIS `RH` | % | nicht verfügbar |

**Warum ein eigener Schritt und nicht Teil von 77:**

1. **Andere Wirkung.** 77 wirkt sofort (die Spalten sind mit dem nächsten Dialogspeichern gefüllt).
   Die Klimaspalten bleiben in **allen Bestandsregionen NULL**, bis der Anwender die Region neu
   importiert — eine Zusage, die im Schrittbericht eigens stehen muss und in einer gemeinsamen
   Notiz mit 77 untergehen würde.
2. **Anderer Mitläufercode.** 77 zieht Leser, Katalogkopie, Modell und Dialog mit; die
   Klimaspalten ziehen `TmyHourlyData`, `SaveTmyData`, `SolardatenCtrl.MapDataRowToModel` und die
   Importprobe mit. Zwei Prüfketten, zwei Abnahmen.
3. **Anderes Risiko.** 77 baut eine Sicht neu — der erste Schritt des SQLite-Zweigs, der das tut.
   Wer die Klimaspalten daranbindet, koppelt ihren Rücklauf an dieses Risiko ohne Gegenwert.
4. **Keine technische Notwendigkeit dagegen.** `ALTER TABLE … ADD COLUMN` ist in SQLite eine
   reine Metadatenänderung; die rund 280 000 Zeilen von `Tab_Solar_STAMM` werden nicht angefasst.
   Der Schritt kostet Millisekunden.

**Nummer:** 79 (nach 77 und 78) bzw. 78, falls 77/78 nach 6.4 verschmolzen werden.

**Kein Zwang zum Neuimport.** Solange `Gegenstrahlung` NULL ist, gilt die Schätzung; der
Schalter `Aussenbauteile_Strahlung` ist in G1 ohnehin ausgeschaltet (Konzept 4.4). Der Wert wird
also erst mit G2 scharf — aber die **Spalte** gehört in dieselbe Auslieferung, damit ein Anwender,
der ohnehin eine Region neu importiert, den Wert nicht ein zweites Mal holen muss.

### 6.6 Reihenfolge der Merges und was je Merge den Referenzlauf trifft

| # | Merge | Inhalt | Wirkung auf den Referenzlauf |
|---|---|---|---|
| **M0** | **G0 — Löser** | `Zonenmodell2K`, `ErsatzparameterRC`, Normfälle, Rechenproben; **keine** Datenbank, **keine** Verzweigung | **keine.** Kein Aufrufer, keine Zeile des Bestandswegs geändert. Gate: Kern-Filter grün; die Normfälle schweigen ohne die lokale Zahlendatei (5.4) |
| **M1** | **GB — Bestandsbefunde** | `_prevRoomTemp` als Instanzzustand, Warnungen statt stiller NaN, `Bauweise` 10576, vierte Einfrierregel | **Ergebnisse ändern sich: 1008 und 1039** (Reihenfolgeabhängigkeit) und **1008** (Bauweise). Eigener Einfrierschritt: Lauf, Vergleich, Begründung in `Referenzlaeufe/LIESMICH.md`, grüner CI-Lauf. Diese Basis ist die **letzte reine Bestandsbasis** |
| **M2** | **Umbenennung** | `Fensterflaeche_Ost` → `Fensterflaeche_OstWest`, 15 Stellen | **keine** — reine Umbenennung ohne DB-Berührung. Muss trotzdem einzeln gegen die GB-Basis laufen, weil jede Zeile davon in `SolareGewinneC` mündet |
| **M3** | **Schema** | Schritt 77 (+78), Namensleser, `GebaeudeSchema`, `DbWerte`, Katalogkopie, `TestDatenbank` | **keine** — Spalten bleiben NULL, kein Leser rechnet damit. Der Sichtneubau ist die Probe: `ProjektGebaeudeCtrl` muss alle 58 Bestandsfelder unverändert liefern. Gegen die GB-Basis **byte-gleich** |
| **M4** | **Klimaspalten** | Schritt 79, `TmyHourlyData`, `SaveTmyData`, `SolardatenCtrl` | **keine** — neue Spalten NULL, `Sol_*` unverändert. Die Importprobe gegen `pvgis_tmy_stuttgart_72h.json` muss dieselben `Sol_*` liefern wie bisher |
| **M5** | **G1 + G2 — das Modell** | `GebaeudeModellEingang`, `GebaeudeModellErgebnis`, Verzweigung, Dialog, Hülle, Texte, Ergebnisexport | **alle dreizehn Projekte ändern sich** (+7 bis +33 % Jahresheizwärme, 5.5); drei neue CSV je VDI-Gebäude. **Basis vollständig neu einfrieren** (E1/Q14). Zusätzlich der Rückweg-Test: jedes Referenzprojekt ausdrücklich auf `TAGESBILANZ` und gegen die **GB**-Basis gehalten |

**Was zwischen M1 und M5 gilt:** jeder Merge läuft gegen die GB-Basis und muss `GESAMT: PASS`
melden. Nur M5 friert neu ein. Wer M3 und M5 zusammenlegt, kann nachher nicht mehr sagen, ob eine
Abweichung vom Schema oder vom Modell kommt — genau die Trennung, die E4 für GB begründet.

### 6.7 Risiken

| Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|
| **R1 — Namensfalle `Fensterflaeche_Ost`** | Das Tagesmodell verliert Ost-/Westfenster **still**; Jahresheizwärme steigt, kein Fehler, keine Warnung | eigener Merge **M2 vor M3**; ein Test, der `Fensterflaeche_OstWest` gegen `Abfrage_Projektgebaeude` hält, bevor die Spalte existiert |
| **R2 — `CopyFromStamm` macht NULL zu 0** | Aus dem Katalog übernommene Gebäude rechnen mit Rahmenanteil 0, Verschattung 0, Innenflächenfaktor 0 — physikalisch unsinnig, ohne Meldung | NULL-erhaltende Bindung für die elf neuen Spalten (`GebaeudeStammCtrl.cs:449 ff.`); Datenbankfall „Katalogkopie hält NULL" (Konzept 10.3) |
| **R3 — Sicht und Leser laufen auseinander** | Jeder spätere Schemaschritt verschiebt die Zuordnung still oder liefert nichts | Sichtdefinition **im Kern** (`GebaeudeSchema`), eine Quelle für Migration und `TestDatenbank`; neue Spalten **hinter** `Tab_Gebaeude.ID`; Namensleser im selben Merge |
| **R4 — neue CSV brechen den Vergleich** | `Vergleich` meldet `Schwere = double.MaxValue`, nicht abschaltbar (`Vergleich.cs:183-190`) | die drei Reihen **nur** für VDI-6007-Gebäude schreiben (Muster Erdreichblock, `Ergebnisexport.cs:247-253`) — sonst ist der `TAGESBILANZ`-Regressionstest aus N1.1 nicht führbar |
| **R5 — Zeitbezug Stundenanfang / Stundenmitte** | Ost- und Westflächen um eine halbe Stunde verschoben; trifft genau die Orientierungen, deren Trennung G1 neu einführt | in G1 **messen und entscheiden**, an der einen Stelle in `GebaeudeModellEingang`; `Tab_Solar.Sol_*` bleibt unberührt (Referenzbasis) |
| **R6 — Wochenendkalender aus dem Referenzjahr** | Konzept 4.4 leitet den Wochentag aus `SolardatenCtrl.Referenzjahr` ab; `Tab_Klimadaten.WE` stammt aus dem PVGIS-Jahr 2020 (`KlimaImportAblauf.cs:354`). Der dort verlangte Test schlägt fehl, sobald ein Projekt eine Preisreihe ≠ 2020 führt | Wochenendmaske aus `WE[365]` (`SimulationWaermebedarf.cs:524`) nehmen; Konzept 4.4 korrigieren |
| **R7 — statische Seitenwirkung von `CalculateHourly`** | `sonnenwinkel`, `sonnen_azimut`, `lastCosTheta` (`SolarPVGISCalculator.cs:302-304`) sind prozessweit; `SimulationSolarthermie` liest `lastCosTheta` unmittelbar nach dem Aufruf | ausschließlich `CalculateHourlyHayDavies` benutzen (`:455`, schreibt nichts, Kopf `:446-452`) |
| **R8 — Normzahlen im Repository** | Vervielfältigung nach Vorbemerkung der Richtlinie (N1.2); LFS ist keine Zugriffsbeschränkung | gitignorierte lokale Datei + `Vorhanden`-Vorrichtung nach Muster `TestDatenbank` (5.4); der Nachweis ist ein lokaler, kein CI-Nachweis — das gehört ins Protokoll |
| **R9 — Namenswächter sieht das neue Modell nicht** | `EinheitenWacheTests.Simulationsklassen` (`:201-206`) ist eine feste Liste; eine Jahressumme ohne Einheit im Namen fällt durch | `GebaeudeModellErgebnis.cs` in die Liste eintragen — im selben Merge, in dem die Klasse entsteht |
| **R10 — Doppelrechnung bei Verbrauchsgebäuden** | `Bewohner_und_Flaeche_berechnen:647` rechnet das Gebäude ein zweites Mal; der Löser muss zustandsfrei zweimal laufen | `Zonenmodell2K.Zuruecksetzen` vor jedem Lauf; Rechenprobe „zwei Läufe byte-gleich" (Konzept 10.2); `VerbrauchAlt > 0` benannt prüfen (`:650`) |
| **R11 — mehr als 100 Gebäude** | `HeizwaermebedarfGeb[100]` (`:31`) und `MaxP[100]` (`:56`) — `IndexOutOfRangeException` bei `:816` | beim Anfassen der Schleife mitnehmen: `MaxP` löschen (tot), `HeizwaermebedarfGeb` auf `ctrl.rows` dimensionieren |
| **R12 — Indexzeile fehlt** | `DokumentationLinkWacheTests.Der_Index_nennt_jedes_Papier` (`:170`) wird rot, und `../Umsetzungskonzept_…md` existiert am 15.09.2026 noch nicht | Indexzeilen für dieses Papier **und** das Umsetzungskonzept in `Dokumentation/LIESMICH.md`, im selben Schritt wie das Umsetzungskonzept selbst |

---

## 7. Korrekturen am Konzept, die dieser Befund nahelegt

1. **4.1 / 11:** die Löserklasse heißt nach Nachtrag N1.3 `Zonenmodell2K`, nicht `Zonenmodell7R2C`.
2. **4.4:** der Wochentag des 1. Januar kommt aus `Tab_Klimadaten.WE`, nicht aus
   `SolardatenCtrl.Referenzjahr` — sonst ist der dort genannte Test nicht führbar (R6).
3. **4.4:** die Erdreichtemperatur ist bereits im Kern
   (`ErdreichTemperatur.JahresprofilKollektor`, `:411`); die Zahlen 0,68 und 22 Tage fallen aus dem
   Vorgabeboden. Kein neuer Code.
4. **4.6:** der Bestandsvorlauf sind **15** Tage (`SimulationWaermebedarf.cs:748`), nicht wie an
   einer Stelle gelesen 15 Tage im Tagesmodell und ein anderer Wert anderswo — die Zahl ist belegt.
5. **6.2:** `sql/schema/002_views.sql` wird zur Laufzeit **nicht** ausgeführt und ist eingefroren
   („NICHT VON HAND AENDERN", vgl. `WechselrichterSchema.cs:33-38`). Die Sichtdefinition gehört in
   eine Kernklasse; das Schemaskript bleibt unberührt.
6. **6.1:** Schritt 78 sollte mit 77 verschmelzen, weil G1 und G2 nach E1 gemeinsam ausgeliefert
   werden (6.4).
7. **10.4:** die neuen Reihen müssen für `TAGESBILANZ`-Gebäude **gar nicht** entstehen — nicht
   „nur für VDI-6007-Gebäude gefüllt". `Vergleich` kennt keinen Dateiausschluss (R4).
8. **10.5:** `EinheitenWacheTests` greift auf einer neuen Datei nur halb; die Liste
   `Simulationsklassen` ist zu erweitern (R9).
