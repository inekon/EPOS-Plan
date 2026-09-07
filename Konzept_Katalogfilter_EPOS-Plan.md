# Konzept: Katalogfilter EPOS-Plan — Auswahl und Suche nach den wichtigen Parametern

**Rev. 1 — 07.09.2026 — Befund, Vorschlag und Mockup zur Entscheidung durch den Anwender.
Nichts davon ist umgesetzt.**

Auftrag (Anwenderwunsch **W14a‑E‑9**, 07.09.2026, im Wortlaut):

> „Die Dialoge unter Menü Administration → Energiesysteme, Administration → Wärmebedarf &
> Heizung, Administration → Strombedarf & Speicher sollen eine Auswahl/Suche erhalten, um die
> wichtigen Parameter der Komponenten bei der Auswahl eingrenzen zu können. Gebe einen
> Vorschlag. Evtl. auch anderes Design der Dialogbox."

Mockup: **`Mockups/Katalogfilter_Vorschlag.html`** — drei Reiter (M1 Heizkessel-Verwaltung,
M2 Wärmepumpen-Projektauswahl, M3 PV-Module mit 20 749 Zeilen), selbständig im Browser, ohne
Server und ohne Netz. Die Namen und Zahlen darin sind **gemessen**: aus
`Referenzlaeufe/Kenndaten_Test.sqlite` und aus `VDI-3805-Daten/PV/CEC Modules.csv`.

Dieses Papier ist die Fortsetzung von zwei Linien, die schon da sind und bisher
nebeneinanderher liefen: der **Filterleiste der Importmasken** (`ImportZahlenfilter`,
`KatalogFilterbereich`, `Suchmuster`) und dem **Katalogfilter der Wärmepumpe**
(`WaermepumpenKatalogFilter`, sieben Klapplisten, vier Zahlenfelder, ein Suchfeld). Beide
tun dasselbe, keine der beiden weiß von der anderen, und in zehn von vierzehn
Katalogverwaltungen gibt es gar nichts.

---

## 1. Befund

### 1.1 Die drei Köpfe und was hinter ihnen steht

Die drei genannten Köpfe stehen als Daten in `EPOS.UI/Bausteine/Menuetabelle.cs` (seit W16c ist
sie die Quelle des Menüs). Sie führen zusammen **15 Menüpunkte**; dahinter stehen **14
Katalogverwaltungen** und **eine Rechenmaske ohne Katalog** (Lastspitzenkappung).

| Kopf | Punkte |
|---|---|
| **Wärmebedarf & Heizung** (`MenuItem_WBundHeizung`) | Brauchwasser, Heizkessel, Wärmepumpe, BHKW, Solarkollektoren, ▸ Profile & Lastgänge (Wärmebedarf Lastgang, Prozesswärme, Solarthermieganglinie) |
| **Strombedarf & Speicher** (`MenuItem_StromBedarfundSp`) | Stromverbraucher, Stromganglinie, Stromspeicher, Lastspitzenkappung |
| **Energiesysteme** (`MenuItem_Energiesysteme`) | ▸ Photovoltaik (PV Module, Wechselrichter), Pufferspeicher |

### 1.2 Inventar der Verwaltungsdialoge

Zeilenzahlen gemessen am 07.09.2026 auf `Referenzlaeufe/Kenndaten_Test.sqlite`; die Spalte
„nach Import" nennt die Größenordnung, in der der Katalog beim Anwender nach einem
Herstellerimport steht (CEC-Zahlen aus den Auslieferungsdateien unter `VDI-3805-Daten/PV/`).

| # | Menüpunkt | Dialog (Ausprägung) | Listenspalten heute | Filter / Suche heute | Zeilen | nach Import |
|---|---|---|---|---|---|---|
| 1 | Brauchwasser | `Bedarf/BedarfAdminDialog` (Brauchwasser) | Wahl, **Bezeichner** | **keine** | 16 | — |
| 2 | Heizkessel | `Erzeuger/KatalogBrowserDialog` (Heizkessel) | Wahl, **Bezeichner** | 2 Klapplisten: Brennstoffgruppe (13), Leistungsstufe (6 feste Stufen) | 63 | VDI 3805, je Hersteller |
| 3 | Wärmepumpe | `Waermepumpe/WaermepumpeStammDialog`, Katalog als Überlagerung `WaermepumpenKatalogDialog` | Stammliste: **Bezeichner**. Katalog: Hersteller, Modell, VL max, VL min, Leistung, Zuheizung, Bauart | Stammliste **keine**; Katalog: **7 Klapplisten + 4 Zahlenfelder + Suche** | 51 (+ 1 960 Kennlinienzeilen) | VDI 3805 |
| 4 | BHKW | `Erzeuger/KatalogBrowserDialog` (BHKW) | Wahl, **Bezeichner**, Eigenschaften (4 Zeilen in EINER Zelle) | 2 Klapplisten: Brennstoffgruppe, Leistungsstufe | 79 | — |
| 5 | Solarkollektoren | `Erzeuger/KatalogBrowserDialog` (Solarkollektoren) | Wahl, **Bezeichner**, Eigenschaften | **keine** (`KatalogFilterArt.Keiner`) | 7 | VDI 3805 |
| 6 | Wärmebedarf Lastgang | `Bedarf/WaermebedarfAdminDialog` | Wahl, **Bezeichner** | **keine** (das `Filter=` dort ist der Dateiwähler) | 4 | — |
| 7 | Prozesswärme | `Bedarf/BedarfAdminDialog` (Prozesswärme) | Wahl, **Bezeichner** | **keine** | 32 | — |
| 8 | Solarthermieganglinie | `Solarthermie/SolarganglinieAdminDialog` | Wahl, **Bezeichner** | **keine** | 1 | — |
| 9 | Stromverbraucher | `Bedarf/BedarfAdminDialog` (Stromverbraucher) | Wahl, **Bezeichner** | **keine** | 41 | — |
| 10 | Stromganglinie | `Strom/StromganglinieAdminDialog` | Wahl, **Bezeichner**, Zeitintervall | **keine** | 3 | — |
| 11 | Stromspeicher | `Erzeuger/ModulKatalogDialog` (Stromspeicher) | Wahl, **Bezeichner** | **keine** (`HatHerstellerfilter = false`) | 5 | **6 654** (CEC) + 4 (bslib) |
| 12 | Lastspitzenkappung | `Strom/PeakShavingDialog` | — kein Katalog | — | — | — |
| 13 | PV Module | `Erzeuger/ModulKatalogDialog` (Photovoltaik) | Wahl, **Bezeichner** | **keine** (`HatHerstellerfilter = false`) | 6 | **20 743**, 258 Hersteller |
| 14 | Wechselrichter | `Erzeuger/ModulKatalogDialog` (Wechselrichter) | Wahl, **Bezeichner** | 1 Klappliste Hersteller | 1 | **2 343**, 152 Hersteller |
| 15 | Pufferspeicher | `Erzeuger/KatalogBrowserDialog` (Pufferspeicher) | Wahl, **Bezeichner** | 2 Klapplisten: Hersteller, Volumenstufe (6 feste Stufen) | 13 | VDI 3805 |

**Vier Zahlen aus dieser Tabelle:**

1. **Zehn von vierzehn Katalogverwaltungen haben gar keinen Filter.**
2. **Zwölf von vierzehn Listen zeigen genau eine Spalte: den Namen.** Wer zwei Geräte
   vergleichen will, muss sie nacheinander anklicken und den Detailblock lesen. Die zwei
   Ausnahmen (BHKW, Solarkollektoren) tragen ihre Kennwerte als **mehrzeiligen Text in EINER
   Zelle** (`Zeilenbauplan` in `KatalogBrowserProfil`) — nicht sortierbar, nicht vergleichbar.
3. **Die drei größten Kataloge haben den schwächsten Filter.** PV-Module (20 743 nach Import)
   und Stromspeicher (6 658) haben **keinen**, der Wechselrichter (2 343) hat eine Klappliste.
   Die kleinsten — Pufferspeicher mit 13 Sätzen, Heizkessel mit 63 — haben zwei.
4. **Die Wärmepumpe hat als einzige den vollständigen Filter** — und sie hat ihn an der
   falschen Stelle: Er sitzt in einer Überlagerung „Modul-Katalog…", nicht über der Stammliste,
   die der Anwender beim Öffnen sieht.

### 1.3 Inventar der Projektseite

Gepflegt wird im Admin, **ausgewählt wird im Projekt** — und dort steht dieselbe Katalogliste
noch einmal. Seit dem Anwenderentscheid **#76** (05.09.2026) sind das elf Dialoge auf dem
Baustein `Zweispaltenauswahl`: Projekt links, Katalog rechts, zwei Pfeilknöpfe dazwischen. Die
Filter gehören dabei **über die rechte Spalte** (Hausregel aus #76).

| Projektdialog | Katalogliste rechts | Filter heute | derselbe Katalog wie |
|---|---|---|---|
| `Erzeuger/HeizkesselDialog` | Bezeichner | 2 Klapplisten (Brennstoffgruppe, Leistungsstufe) | #2 |
| `Erzeuger/BhkwDialog` | Bezeichner | 2 Klapplisten | #4 |
| `Erzeuger/PufferspeicherDialog` | Bezeichner | 2 Klapplisten (Hersteller, Volumenstufe) | #15 |
| `Erzeuger/PhotovoltaikDialog` | Bezeichner | **1 Klappliste Hersteller** | #13 |
| `Erzeuger/StromspeicherDialog` | Bezeichner | **keine** („kein Filter", Kopfkommentar) | #11 |
| `Solarthermie/SolarkollektorenDialog` | Bezeichner | **keine** | #5 |
| `Waermepumpe/WaermepumpenDialog`, `WaermepumpeAnlageDialog` | (Liste + Detail) | Katalog als Überlagerung mit voller Filterleiste | #3 |
| `Strom/StromganglinieDialog` | Bezeichner | **keine** | #10 |
| `Solarthermie/SolarganglinieDialog` | Bezeichner | **keine** | #8 |
| `Bedarf/BedarfsProfileDialog` (drei Drillinge) | Bezeichner | **keine** | #1, #7, #9 |
| Assistent, Seiten 4/5/7 | dieselben Komponenten | wie oben | — |

**Die Asymmetrie ist der eigentliche Befund:** Der PV-Modulkatalog hat auf der **Projektseite**
einen Herstellerfilter (`PhotovoltaikStammCtrl.Filtern(hersteller)`), in der **Verwaltung**
nicht — dieselbe Tabelle, zwei verschiedene Antworten auf dieselbe Frage. Beim Heizkessel und
beim Pufferspeicher rufen zwar beide Seiten dieselbe Methode `Filtern(…)`, aber jede Seite baut
ihre Klapplisten selbst.

### 1.4 Drei Filtermechaniken, die dasselbe tun

| Ort | Mechanik | Wo gefiltert wird |
|---|---|---|
| **Importmasken** (`ModulImportProfil`, `KatalogImportProfil`) | Herstellerklappliste, Technologieklappliste, Suchfeld mit `*`/`?`, ein bis zwei Zahlenbereiche „von … bis" mit Vorbelegung; **Obergrenze 0 = keine Obergrenze** | im Speicher, über der gelesenen Datei |
| **Wärmepumpenkatalog** (`WaermepumpenKatalogFilter`) | 7 Gleichheitsfilter (`null` = „Alle"), 2 Zahlenbereiche, Suchmuster über die Bezeichnung; `Werte(zeilen, merkmal)` baut jede Klappliste aus dem Bestand | im Speicher, über dem ganzen Katalog |
| **Erzeugerkataloge** (`HeizkesselStammCtrl.Filtern`, `BHKWStammCtrl.Filtern`, `PufferSpStammCtrl.Filtern`, `PhotovoltaikStammCtrl.Filtern`) | fest verdrahtete `WHERE`-Ketten je Gruppe, sechs feste Leistungs- bzw. Volumenstufen als `string[]` | **in SQL**, und die Abfrage liefert nur `ID, Bezeichner` |

Die dritte Mechanik ist der Grund für Befund 1.2/2: Weil `SELECT ID, Bezeichner` gelesen wird,
**kann** die Liste keine Parameterspalten zeigen — die Werte sind gar nicht da.

Die gemeinsame Grundlage gibt es schon: `EPOS.Kern/Allgemein/Suchmuster.cs` (`*` = beliebig
viele, `?` = genau eins; mit Platzhalter verankert, ohne Platzhalter Teilsuche; ein kaputtes
Muster ist **kein** Filter) und `VdiAuswahlFilter.Passt` (mehrere Begriffe als UND über mehrere
Felder). Zwei Suchen im Haus, wo eine genügt.

### 1.5 Was die Daten selbst noch sagen

Beim Ausmessen der Testdatenbank für das Mockup sind drei Dinge aufgefallen, die den Filter
betreffen und die deshalb hier stehen — sie sind **Befunde, keine Vorschläge**:

| Kennung | Befund | Zahlen |
|---|---|---|
| **D‑1** | Das Kennzeichen `Tab_Heizkessel_STAMM.Brennwert` ist **nicht gepflegt**: 6 von 63 Sätzen tragen 1, während **46** Beschreibungen „Brennwert" nennen. Ein Schalter „nur Brennwert" auf dem Kennzeichen fände 6 statt 46 | 6 / 46 / 63 |
| **D‑2** | `Wirkungsgrad_Gas` steht **in zwei Einheiten**: 62 Sätze als Faktor (0,803 … 1,0), einer als Prozent (94,0 / 99,0 — `eloBLOCK VE 10`). Ein Bereichsfilter „η von … bis" müsste beide treffen | 1 von 63 |
| **D‑3** | `Tab_Stromspeicher_STAMM` hat **keine Spalte `Firma`**. Der Import schreibt den Hersteller in den Bezeichner (`StromspeicherImportSatz`: „Hersteller: Modell"), und der Modulimport gewinnt ihn als „Text vor dem ersten Doppelpunkt" zurück. Eine Herstellerklappliste für diesen Katalog hat heute keine Spalte, aus der sie sich füllen könnte | — |

---

## 2. Die wichtigen Parameter je Komponente

Grundlage ist `EPOS.Kern/Allgemein/Katalog/ParameterVerwendung.cs` (Anwenderwunsch W14a‑E‑8,
06.09.2026). Sie stuft jeden Katalogparameter in **Simulation / Wirtschaftlichkeit / Bericht /
Dialog / Keine** ein und nennt zu jeder Stufe die **Fundstelle** — die Zeile, in der der Wert
gelesen wird. „Wichtig" heißt hier: **Stufe Simulation oder Wirtschaftlichkeit** (der Wert
verändert das Ergebnis) **oder** er trennt die Bauart, nach der ein Planer sucht.

Legende: `SIM` Simulation · `WIRT` Wirtschaftlichkeit · `BER` Bericht · `DLG` nur Dialog ·
`—` niemand liest ihn. Fett = Vorschlag für die Filterzeile (Kapitel 4).

### 2.1 Heizkessel — `Tab_Heizkessel_STAMM`

| Parameter | Einheit | Stufe | warum er wichtig ist |
|---|---|---|---|
| **Ptherm** | kW | SIM · WIRT · BER | `SimulationSPK.cs:148`; die Auslegungsgröße schlechthin |
| **Brennstoff** | — | SIM · WIRT | `SimulationSPK.cs:175`; entscheidet Preis **und** Emission |
| **Wirkungsgrad_Gas / _Öl** | — | SIM · WIRT · BER | `SimulationSPK.cs:162/163` |
| **Firma** | — | BER | Hersteller — das erste, wonach ein Planer sortiert |
| **Brennwert** | — | BER | Bauart; Befund D‑1 |
| Betriebsbereitschaftverlust | % | SIM | `SimulationSPK.cs:178` |
| Vorlauf / Ruecklauf | °C | SIM | `SimulationControl.cs:3890` |
| Investitionskosten | € | WIRT | `TechnikPlanwertCtrl.cs:357` |
| Wartungskosten (+ Einheit) | — | WIRT | `TechnikPlanwertCtrl.cs:823` |
| Raumbedarf, Nutzungsdauer, CO2/SO2/NOx/CO/Staub | — | DLG | seit W14a‑E‑8‑B1 nur Anzeige |

### 2.2 BHKW — `Tab_BHKW_STAMM`

| Parameter | Einheit | Stufe | warum |
|---|---|---|---|
| **Pel** | kW | SIM · WIRT · BER | `SimulationBHKW.cs:283`; KWKG-Deckel `WirtschaftlichkeitCtrl.cs:3585` |
| **Ptherm** | kW | SIM · WIRT · BER | `SimulationBHKW.cs:282` |
| **Wirkungsgrad** | — | SIM · WIRT · BER | `SimulationBHKW.cs:287` |
| **Brennstoff** | — | SIM · WIRT | `SimulationBHKW.cs:286` |
| **Firma** | — | BER | Hersteller |
| *Stromkennzahl σ = Pel / Ptherm* | — | *abgeleitet* | die Kennzahl, nach der ein BHKW ausgewählt wird — sie steht **nirgends** im Katalog |
| Grenzleistung | % | SIM | Teillastgrenze, `SimulationBHKW.cs:312` |
| Motortyp | — | BER | 45 verschiedene Werte in 79 Sätzen — als Spalte und Suchfeld tauglich, als Klappliste nicht |
| Kosten_Modul/Montage/Lieferung/Schallschutz/Abgasreinigung | € | WIRT | `TechnikPlanwertCtrl.cs:317–325` |

### 2.3 Wärmepumpe — `Tab_WP_STAMM` (+ `Tab_Kenndaten_STAMM`)

| Parameter | Einheit | Stufe | warum |
|---|---|---|---|
| **Typ** (Luft‑/Sole‑/Wasser‑Wasser) | — | SIM · BER | `SimulationWaermepumpe.cs:543` — die Quellenwahl |
| **Nennleistung** | kW | SIM · BER | `SimulationWaermepumpe.cs:541` |
| **max. Vorlauf** (aus den Kennlinien) | °C | — | die Grenze, an der eine Wärmepumpe für ein Bestandsgebäude ausscheidet; im Bestandsfilter bereits vorhanden |
| **Firma** | — | BER | Hersteller |
| **Kuehlleistung → Auslegung** | kW | BER | trennt „Heizen" von „Heizen und Kühlen" |
| Heizung (el. Zuheizung) | kW | SIM | Heizstabphase `:1553` |
| Regelung, Bauart, Aufstellung | — | BER | Bauartmerkmale, schon heute Filterkriterien |
| *COP bei A2/W35 (aus `Tab_Kenndaten_STAMM`)* | — | *abgeleitet* | die Gütezahl; sie steht als Kennlinienpunkt da und nirgends als Spalte |
| Modulkosten | € | WIRT | `TechnikPlanwertCtrl.cs:345` |
| Laenge/Breite/Hoehe/Gewicht/Raum | — | **—** | fünf Spalten, die **niemand** liest |

### 2.4 Solarkollektoren — `Tab_Solarkollektoren_STAMM`

| Parameter | Einheit | Stufe | warum |
|---|---|---|---|
| **Aperturflaeche** | m² | SIM · BER | `SimulationSolarthermie.cs:232` — mit ihr wird gerechnet |
| **h0** (Konversionsfaktor η₀) | — | SIM | `SimulationSolarthermie.cs:242` |
| **Kollektortyp** (Flach / Röhre) | — | BER | die Bauart |
| **Firma** | — | DLG | Hersteller |
| k1 / k2 | W/(m²·K), W/(m²·K²) | SIM | `:243`, `:244` |
| Kdir | — | SIM | IAM direkt, `:245` |
| Investitionskosten | € | WIRT | `TechnikPlanwertCtrl.cs:341` |
| Modulflaeche | m² | DLG | gerechnet wird mit der Apertur |

### 2.5 Pufferspeicher — `Tab_Pufferspeicher_STAMM`

| Parameter | Einheit | Stufe | warum |
|---|---|---|---|
| **Gesamtvolumen** | l | SIM · BER | `SimulationControl.cs:1681` |
| **Bereitschaftsverluste** | kWh/d | SIM | `WaermequelleClass.cs:805` |
| **Speichertyp** | — | SIM · BER | `Warnkriterien.cs:1307/984` — Kriterium W4 unterscheidet Kombi- und Solarspeicher |
| **Hersteller** | — | DLG | schon heute die Klappliste |
| Investitionskosten | € | WIRT | `TechnikPlanwertCtrl.cs:357` |

### 2.6 PV-Module — `Tab_PV_STAMM`

| Parameter | Einheit | Stufe | warum |
|---|---|---|---|
| **Leistung** (P_STC) | W | SIM · BER | `SimulationPV.cs:497` |
| **Wirkungsgrad** | % | SIM · BER | `SimulationPV.cs:184/480` |
| **Technologie** | — | SIM · BER | `SimulationPV.cs:590` — der Huld-Satz je Zelltechnologie; fünf Werte in der CEC-Liste |
| **Firma** | — | BER | 258 Hersteller nach dem CEC-Import |
| *Modulfläche = Laenge × Breite* | m² | SIM (`:183/480`) | *abgeleitet*; die Belegungsdichte auf dem Dach |
| gamma_PMP | %/K | SIM | Temperaturkoeffizient, `:535` |
| T_NOCT | °C | SIM | Zelltemperatur, `:508` |
| Modulkosten | € | WIRT | `TechnikPlanwertCtrl.cs:349` |
| U_Mpp, U_Leerlauf, I_Mpp, I_Kurzschluss, alpha_SC, beta_OC | V, A | DLG | Auslegungswerte des Strangs — Detailblock, nicht Filter |

### 2.7 Wechselrichter — `Tab_Wechselrichter_STAMM`

| Parameter | Einheit | Stufe | warum |
|---|---|---|---|
| **P_AC_Nenn** | kW | SIM | `PvStrangModell.Stunde` — Auslastung und **Clipping** |
| **Eta_Euro** | — | SIM (Kennlinie) | die eine Kennzahl, die zwei Geräte vergleichbar macht |
| **Anzahl_Mppt** | — | DLG (P4/P5) | Zahl der Tracker — die Ost/West-Frage |
| **Firma** | — | DLG | 152 Hersteller; schon heute die Klappliste |
| U_Dc_Max | V | DLG (P1) | die Spannungsgrenze der Strangprüfung |
| U_Mpp_Min / U_Mpp_Max, U_Start, I_Dc_Max | V, A | DLG (P2/P3) | Eingangsfenster |
| Eta05…Eta100 | — | SIM | Stützstellen der Kennlinie |
| Herkunft | — | DLG | CEC / OND / HAND — woher der Satz kommt |
| Kosten | € | DLG | — |

### 2.8 Stromspeicher — `Tab_Stromspeicher_STAMM`

| Parameter | Einheit | Stufe | warum |
|---|---|---|---|
| **Energie** | kWh | SIM · WIRT · BER | `StromspeicherSimCtrl.cs:1109` |
| **Leistung** | kW | SIM · WIRT · BER | `:1110` |
| **Typ** (Chemie) | — | BER | Lithium-Ionen / Lithium-Eisen-Phosphat / … |
| **Wirkungsgrad_RT** | — | SIM | `:1120` — der Umlaufwirkungsgrad |
| *C‑Rate = Leistung / Energie* | 1/h | *abgeleitet* | trennt Heim- von Netzspeicher; heute nirgends sichtbar |
| Zyklen_Zugesichert | — | SIM · WIRT | `ArbitragePlaner.cs:198` |
| Degradation, Ladezustand, Standby_Verbrauch | %, W | SIM | `:1118`, `:1117`, `:1115` |
| Modulkosten, Leistungskosten, Investition_Fix, Verschleisskosten | € | WIRT | `TechnikPlanwertCtrl.cs:330–334` |
| *Hersteller* | — | **fehlt** | Befund D‑3 |

### 2.9 Bedarfskataloge — Brauchwasser, Prozesswärme, Stromverbraucher

Alle drei sind Drillinge derselben Tabellenform (`Bezeichner`, `Typ`, `Beschreibung`,
`Monat_1…12`, `ReadOnly`); der `Typ` verweist auf einen Profilkatalog mit 168 Wochenstunden.

| Parameter | Stufe | warum |
|---|---|---|
| **Typ** | SIM | das Stundenprofil — der Grund, warum zwei gleich große Bedarfe verschieden rechnen |
| *Jahressumme (Σ Monat_1…12)* | *abgeleitet* | die Größenordnung; sie steht heute nur im Detailblock des gewählten Satzes |
| **Bezeichner, Beschreibung** | DLG | die Beschreibungen der VDI‑6002-Sätze tragen den Kennwert im Text („28 l/(P·d) @60 °C") — sie sind der eigentliche Suchraum |

### 2.10 Zeitreihen — Wärmebedarf Lastgang, Stromganglinie, Solarthermieganglinie

`Bezeichner` (+ `Zeitinterval` bei der Stromganglinie, + `Beschreibung` bei der
Solarganglinie). Jahresarbeit und Spitze wären die fachlich richtigen Filtergrößen, stehen
aber nicht am Kopfsatz, sondern in der Wertetabelle (78 840 bzw. 35 040 Zeilen in der
Testdatenbank) — siehe Kapitel 4.10 und Frage **Q6**.

---

## 3. Vorschlag: EIN Baustein `Filterleiste` mit einem Filterprofil je Katalog

### 3.1 Warum einer und nicht vierzehn

Es gibt bereits drei Mechaniken (1.4), und mit dem Wunsch W14a‑E‑9 kämen zehn weitere Filter
dazu. Vierzehn handgeschriebene Filterzeilen sind vierzehn Wahrheiten darüber, was „Alle"
heißt, ob eine Obergrenze 0 „nichts" oder „alles" bedeutet und ob `*` ein Platzhalter ist.

Das Haus hat für genau diese Lage ein Muster, dreimal erprobt: **`KatalogImportProfil`**,
**`KatalogBrowserProfil`**, **`ModulKatalogProfil`** — der Bauplan steht einmal als Komponente,
die Unterschiede stehen als **Daten im Kern**. Der Vorschlag ist der vierte Zwilling.

```
EPOS.Kern/Allgemein/Katalog/Katalogfilterprofil.cs   (neu — DATEN, keine Datenbank)
EPOS.Kern/Allgemein/Katalog/Katalogfilter.cs         (neu — die RECHNUNG, prüfbar ohne Oberfläche)
EPOS.UI/Bausteine/Filterleiste.razor                 (neu — das BILD, filtert nicht selbst)
```

Regel **F4** des Wellenplans gilt weiter: *Die Komponente filtert nicht selbst.* Sie sammelt
die Kriterien ein und ruft `Katalogfilter.Anwenden(zeilen, kriterien)` — genau so, wie es
`WaermepumpenKatalogDialog` heute schon mit `WaermepumpenKatalogFilter` tut.

### 3.2 Was in einem Filterprofil steht

Ein Profil beschreibt **eine Filterzeile**, nichts weiter. Vier Bauteile, alle aus dem Bestand
abgeschaut:

| Bauteil | Vorbild im Bestand | was es trägt |
|---|---|---|
| **Klappliste** (`Filterklappliste`) | `WaermepumpenKatalogFilter.Werte`, die Herstellerlisten der Importe | Merkmalsschlüssel (Spaltenname), Beschriftung, ob die Werte aus dem Bestand kommen oder aus einer festen Liste (Brennstoffgruppen). **„Alle" ist `null`**, nie ein Text (Abweichung A‑2 der Welle 7) |
| **Zahlenbereich** (`Filterbereich`) | `ImportZahlenfilter`, `KatalogFilterbereich` | Merkmalsschlüssel, Beschriftung, Einheit, Min/Max, Vorbelegung von/bis, Nachkommastellen. **Obergrenze 0 = keine Obergrenze** (wörtlich aus `ApplyFilter`) |
| **Suchfeld** (`Filtersuche`) | `Suchmuster.Uebersetzen`, `VdiAuswahlFilter.Passt` | die Felder, über die gesucht wird (Bezeichner, Hersteller, Beschreibung, Typ); `*` und `?`; mehrere Begriffe als UND; ein kaputtes Muster ist **kein** Filter |
| **Merkmalsschalter** (`Filterschalter`) | neu, aber trivial | „nur eigene" / „nur Auslieferung" (`ReadOnly`), „nur Brennwert", „im Projekt verwendet" |

Dazu je Profil die **Listenspalten** (Schlüssel, Titel, Einheit, Ausrichtung, Sortierbarkeit)
— denn ohne Parameterspalten nützt ein Parameterfilter wenig (Befund 1.2/2).

**Abgeleitete Größen sind gleichberechtigte Merkmale.** Stromkennzahl σ, C‑Rate, Modulfläche
und COP bei A2/W35 stehen in keiner Spalte; das Profil nennt sie als *gerechnetes* Merkmal, und
`Katalogfilter` rechnet sie einmal beim Aufbau der Zeilenliste. Nur so kann die Liste nach
ihnen sortieren.

### 3.3 Die Mechanik — was aus dem Bestand wörtlich übernommen wird

| Regel | Herkunft |
|---|---|
| „Alle" ist der Steuerwert `null`, nie der übersetzte Text | `WaermepumpenKatalogFilter`, A‑2 (W7) |
| Eine Obergrenze 0 heißt „keine Obergrenze" | `ImportZahlenfilter` |
| `*` = beliebig viele, `?` = genau eins; **mit** Platzhalter verankert (`^…$`), **ohne** Teilsuche; Groß/klein egal; kaputtes Muster = kein Filter | `Suchmuster.Uebersetzen` |
| Mehrere durch Leerzeichen getrennte Begriffe wirken als UND über alle Suchfelder | `VdiAuswahlFilter.Passt` |
| Die Klapplistenwerte kommen **aus dem Bestand**, ohne Leerwerte, ohne Dubletten, aufsteigend | `WaermepumpenKatalogFilter.Werte` |
| Die Vorbelegung zeigt **alles** — eine Vorbelegung, die beim Aufmachen Zeilen verschwinden ließe, ist unerklärlich | Entscheid W13‑E‑2 (Stromspeicherimport) |

### 3.4 Die Hausregeln, die die Leiste einhalten muss

| Regel | Herkunft | Folge für den Baustein |
|---|---|---|
| **Filter stehen ÜBER der Tabelle, auf die sie wirken** | W6‑O‑4; #76 („Filter gehören über die Katalogliste in die RECHTE Spalte") | die Leiste ist das erste Kind der Listenspalte, nicht ein Block darunter |
| **Die gewählte Zeile bleibt gewählt, auch wenn der Filter sie ausblendet** | `EnergietraegerDialog` (W4): „Der gewählte Träger BLEIBT GEWÄHLT … beim Leeren der Suche wieder markiert" | der Filter wirkt auf die **Anzeige**, nie auf die Auswahl |
| **Virtualisierung ab 120 Zeilen** | Raster-Fix W6‑B‑2 (`Raster.Virtualisiert`) | der Wirt schaltet an der **gefilterten** Zeilenzahl; das `@key` an (Virtualisiert, Zeilenzahl) bleibt |
| **Klicksemantik W6‑E‑5** | `Zeilenmarkierung` seit 07.09.2026 | einfacher Klick schaltet um, `Strg` ebenso, `Umschalt` nimmt den Bereich, Doppelklick übernimmt. Nach einem Filterwechsel wird die Markierung über `Hinzufuegen` wiederhergestellt |
| **44 px Berührungsziele, keine Hover-Abhängigkeit** | iL4, `--epos-touchziel` | jedes Filterfeld, jeder Chip und jedes „×" ist 44 px hoch; das „×" ist ein eigenes Ziel, kein `:hover`-Symbol |
| **Eine Liste steht in einem festen Rahmen mit Rollbalken** — außer im `Katalograhmen` | W9‑B‑2 / `--epos-listenhoehe` | in den Verwaltungen nimmt die Liste die verbleibende Höhe, in den Projektdialogen gilt 22 rem |
| **Ein Parameterblock steht im `Formularraster`** | iU8‑E‑2 / W14a‑E‑7 | der Detailbereich bleibt, wie er seit W14a ist |
| **Kein Delegat, kein Bedienelement** | `ModulKatalogDialog.Filterbar` | ein Profil ohne Klapplisten zeichnet keine; eine Hülle ohne Suchdelegat bekommt kein Suchfeld |
| **Farben nur als Token in `:root`** | W16b‑E‑5 | die Leiste braucht **keine neue Farbe**: `--epos-flaeche`, `--epos-rahmen-leise`, `--epos-text-leise` und die vorhandene Klasse `.epos-chip` genügen |

### 3.5 Chips, Trefferzähler, Zurücksetzen, Sortierung, Gedächtnis

- **Chips.** Jeder gesetzte Filter erscheint als abwählbarer Chip: „Brennstoff: Gas ×",
  „P_th 10…60 kW ×", „Suche: eco* ×". Er ist die **einzige** Stelle, an der der Anwender den
  ganzen Filterzustand auf einen Blick sieht — bei zusammengeklappten Bereichsfeldern ist er
  sogar die einzige Stelle überhaupt. `.epos-chip` gibt es schon; der Chip bekommt ein
  eigenes 44‑px-„×".
- **Trefferzähler.** Eine `Herleitungszeile` rechts über der Liste: **„15 von 20 749 Sätzen"**. Der
  Wärmepumpenkatalog hat das seit W7 (A‑7: die Zahl stand im Vorläufer im **Fenstertitel**, den
  eine Überlagerung nicht hat) — dieselbe Zeile, überall.
- **„Kein Treffer."** Statt einer leeren Liste steht der Satz aus `EnergietraegerDialog`
  (`ETV_SUCHE_LEER`).
- **Zurücksetzen.** Ein Knopf am Ende der Leiste, wie in `ModulImportDialog` und
  `WaermepumpenKatalogDialog`. Er setzt auf die Profilvorbelegung zurück, nicht auf leer.
- **Sortierung.** Über die Spaltenköpfe des `Raster` (QuickGrid kann das; `ProjektListe` macht
  es seit W15a samt Gleichstandsauflösung über den Namen). Ein Klick auf den Kopf sortiert
  auf, ein zweiter ab. Die Sortierung ist **kein** Chip — sie blendet nichts aus.
- **Gedächtnis.** Der Filterzustand wird **je Katalog für die Sitzung** gemerkt, damit ein
  zweiter Aufruf derselben Verwaltung dort weitermacht, wo der erste aufgehört hat. Ablage:
  ein Wörterbuch im Wirt (`Katalogfilterstand`), **nicht** `Dienste.Einstellungen` — siehe
  Frage **Q2**.

### 3.6 Wo gefiltert wird — im Speicher oder in SQL

`WaermepumpenKatalogFilter` sagt es im Klassenkommentar: „bei einer Handvoll hundert
Stammsätzen ist es schneller als neun Abfragen, und es hält die Datenbank aus dem Dialog
heraus." Das gilt für zwölf der vierzehn Kataloge. Es gilt **nicht** für PV-Module (20 743),
Wechselrichter (2 343) und Stromspeicher (6 658) nach einem Import.

Vorschlag (Frage **Q6**): Der **Controller** entscheidet, nicht die Komponente.
`…StammCtrl.Katalogzeilen(kriterien)` liest bis zu einer Schwelle alles und filtert im
Speicher; darüber übersetzt er die Kriterien in ein `WHERE` mit `DbParam`. Für die Komponente
ändert sich dadurch **nichts** — sie ruft einen Delegaten und bekommt Zeilen. Jede neue
SQL-Anweisung geht durch `Werkzeuge/SqlDialektPruefer`.

---

## 4. Die konkrete Filterzeile je Katalog

Aufbau überall gleich: **Zeile 1** Klapplisten + Suche · **Zeile 2** Zahlenbereiche +
„Weitere Filter ▾" + „Zurücksetzen" · **Zeile 3** Chips + Trefferzähler.
„A" = im Aufklapper „Weitere Filter", nicht in der Grundzeile.

### 4.1 Heizkessel (63)

| | |
|---|---|
| Klapplisten | **Brennstoffgruppe** (13, Bestand aus `HeizkesselStammCtrl`) · **Hersteller** (`Firma`, aus dem Bestand) |
| Bereiche | **Nennleistung P_th** [kW] 0…1 000 · **Wirkungsgrad** [–] 0…1,2 (η_Gas bzw. η_Öl je nach Gruppe) |
| Suche | Bezeichner, Firma, Beschreibung |
| Schalter | „nur Brennwert" (D‑1) · „nur eigene Sätze" (`ReadOnly = 0`) |
| Spalten | Bezeichner · Hersteller · Brennstoff · **P_th** [kW] · **η** · Brennwert |
| A | Vorlauf/Rücklauf [°C] · Investitionskosten [€] |

*Die sechs festen Leistungsstufen (`LEISTUNG_SQL`) bleiben als Schnellwahl in der Klappliste
„Leistung" erhalten — sie **füllen** dann die zwei Bereichsfelder, statt sie zu ersetzen
(Frage **Q5**).*

### 4.2 BHKW (79)

| | |
|---|---|
| Klapplisten | **Brennstoffgruppe** · **Hersteller** (9 Werte im Bestand) |
| Bereiche | **P_el** [kW] · **P_th** [kW] |
| Suche | Bezeichner, Firma, **Motortyp** (45 verschiedene Werte — Suchfeld, keine Klappliste) |
| Schalter | „nur eigene Sätze" (in der Auslieferung sind alle 79 geschützt) |
| Spalten | Bezeichner · Hersteller · Brennstoff · **P_el** · **P_th** · **σ = P_el/P_th** *(abgeleitet)* · η · Motortyp |
| A | Wirkungsgrad [–] · Grenzleistung [%] |

### 4.3 Wärmepumpe (51)

| | |
|---|---|
| Klapplisten | **Hersteller** · **Quelle** (`Typ`: Luft-Wasser 34, Sole-Wasser 14, Wasser-Wasser 1) · **Auslegung** (Heizen / Heizen + Kühlen) |
| Bereiche | **Nennleistung** [kW] · **max. Vorlauf** [°C] |
| Suche | Bezeichnung **und Hersteller** (heute nur die Bezeichnung) |
| A | Regelung (5) · Bauart (Split/Monoblock) · Aufstellung (5) · el. Zuheizung [kW] |
| Spalten | Hersteller · Modell · **Quelle** · **Nennleistung** · **VL max** · VL min · Zuheizung · Bauart · *COP A2/W35* |

*Das ist der Bestandsfilter aus `WaermepumpenKatalogFilter` — unverändert in der Wirkung,
nur anders gestaffelt: drei Klapplisten offen, vier im Aufklapper. Und er steht dann auch
über der **Stammliste** der Verwaltung, nicht nur in der Überlagerung.*

### 4.4 Solarkollektoren (7 → nach VDI-Import Hunderte)

| | |
|---|---|
| Klapplisten | **Kollektortyp** (Flach / Röhre) · **Hersteller** |
| Bereiche | **Aperturfläche** [m²] · **Konversionsfaktor η₀** [–] |
| Suche | Bezeichner, Firma |
| Spalten | Bezeichner · Hersteller · **Kollektortyp** · **A_ap** [m²] · **η₀** · k1 |
| A | k1, k2 · Investitionskosten |

### 4.5 Pufferspeicher (13)

| | |
|---|---|
| Klapplisten | **Hersteller** (Bestand) · **Speichertyp** (Puffer / Kombi / Solar) |
| Bereiche | **Gesamtvolumen** [l] · **Bereitschaftsverluste** [kWh/d] |
| Suche | Bezeichner, Hersteller |
| Spalten | Bezeichner · Hersteller · **Speichertyp** · **V** [l] · **q_B** [kWh/d] |
| A | Investitionskosten |

*Die sechs Volumenstufen (`VOLUMEN_SQL`) verhalten sich wie beim Heizkessel (Q5).*

### 4.6 PV-Module (6 → 20 743)

| | |
|---|---|
| Klapplisten | **Hersteller** (258) · **Zelltechnologie** (Mono‑c‑Si, Multi‑c‑Si, CdTe, CIGS, Thin Film) |
| Bereiche | **P_STC** [W] · **Wirkungsgrad** [%] |
| Suche | Bezeichner, Firma |
| Schalter | „nur eigene Sätze" |
| Spalten | Bezeichner · Hersteller · **P_STC** [W] · **η** [%] · **Technologie** · *Fläche L×B* [m²] · T_NOCT |
| A | Modulfläche [m²] · γ_PMP [%/K] · Modulkosten [€] |

### 4.7 Wechselrichter (1 → 2 343)

| | |
|---|---|
| Klapplisten | **Hersteller** (152, Bestand) · **Anzahl MPPT** (1/2/3/…) · **Herkunft** (CEC / OND / HAND) |
| Bereiche | **P_AC,nenn** [kW] · **η_euro** [–] |
| Suche | Bezeichner, Firma |
| Spalten | Bezeichner · Hersteller · **P_AC** [kW] · **η_euro** · **MPPT** · U_DC,max [V] · Herkunft |
| A | U_MPP min/max · Stränge je MPPT · Kosten |

### 4.8 Stromspeicher (5 → 6 658)

| | |
|---|---|
| Klapplisten | **Hersteller** (Präfix vor dem ersten Doppelpunkt, bis Befund D‑3 behoben ist) · **Chemie** (`Typ`) |
| Bereiche | **Energie** [kWh] · **Leistung** [kW] |
| Suche | Bezeichner |
| Spalten | Bezeichner · Hersteller · **Typ** · **E** [kWh] · **P** [kW] · *C‑Rate* [1/h] · η_RT · Zyklen |
| A | η_RT [–] · Zyklen · Modulkosten [€/kWh] |

### 4.9 Brauchwasser / Prozesswärme / Stromverbraucher (16 / 32 / 41)

| | |
|---|---|
| Klappliste | **Typ** (Profilzuordnung) |
| Bereich | **Jahressumme** [MWh] *(abgeleitet aus Monat_1…12)* |
| Suche | Bezeichner, Typ, **Beschreibung** (die VDI‑6002-Sätze tragen ihren Kennwert im Text) |
| Schalter | „nur eigene Sätze" |
| Spalten | Bezeichner · **Typ** · **Jahressumme** · Beschreibung (einzeilig gekürzt) |

### 4.10 Wärmebedarf-Lastgang / Stromganglinie / Solarthermieganglinie (4 / 3 / 1)

| | |
|---|---|
| Suche | Bezeichner (+ Beschreibung bei der Solarganglinie) |
| Klappliste | **Zeitintervall** — nur die Stromganglinie |
| Spalten | Bezeichner · Zeitintervall · *Jahresarbeit* [MWh] · *Spitze* [kW] |

*Jahresarbeit und Spitze brauchen eine Aggregatabfrage über die Wertetabelle (78 840 bzw.
35 040 Zeilen). Vorschlag: **eine** `GROUP BY`-Abfrage beim Aufbau der Liste, wie es
`WPStammCtrl.KatalogZeilen` für die Vorlaufgrenzen der Wärmepumpe schon tut — aber erst in
Stufe **S3**, weil hier drei bis vier Zeilen zur Wahl stehen und eine Suche genügt.*

---

## 5. Vorschlag für das Dialogdesign

### 5.1 Was das heutige Bild leistet — und was nicht

**Es leistet mehr, als es vor drei Tagen tat.** Seit dem 05.09.2026 stehen Liste und
Eingabeblock **nebeneinander** (`Katalograhmen`, Anwenderwunsch „Admin-Menüs sind nicht an
Größe Bildschirm angepasst"), die Parameter stehen im **`Formularraster`** mit der
Beschriftung neben dem Feld (iU8‑E‑2 / W14a‑E‑7), die Projekt/Datenbank-Dialoge folgen alle
der **`Zweispaltenauswahl`** (#76), und seit W14a‑E‑8 gibt es unter dem Formular die
**Parameterübersicht** mit der Verwendung jeder Spalte. Der Rahmen stimmt.

**Was fehlt, ist genau eine Zone.** Zwischen der Überschrift und der Liste ist heute nichts —
oder zwei Klapplisten, die aussehen wie Eingabefelder und nicht wie ein Filter. Und die Liste
selbst ist eine Namensspalte: Sie **zeigt** die Parameter nicht, nach denen der Anwender sucht,
also kann er auch nicht nach ihnen sortieren.

Drei Sätze zum Bestand, damit die Bewertung nicht besser klingt, als sie ist:

1. Die zwei Klapplisten des Heizkessels heißen „Filtern nach Brennstoff:" und „Filtern nach
   Leistung:" und stehen ohne Rahmen, ohne Trefferzahl und ohne Rücksetzknopf über der Liste.
   Ob gerade gefiltert wird, sieht man nur, wenn man beide liest.
2. Die Leistungsstufen sind **fest**: „< 50", „50…200", „200…500", „500…1000", „≥ 1000". Wer
   einen 30‑kW-Kessel sucht, bekommt jede Zeile unter 50 kW.
3. Der einzige vollständige Filter des Hauses (Wärmepumpe) hat **elf** Bedienelemente
   nebeneinander im `epos-zahlenraster` — er ist so breit wie die Maske und braucht selbst eine
   Ordnung.

### 5.2 Der Vorschlag: drei Zonen

**Zone A — die Filterleiste**, über die ganze Breite, direkt unter Titel und Kopfband.
**Zone B — das Paar**: links die Liste mit drei bis fünf Parameterspalten, rechts der
Detailbereich. **Zone C — die Aktionsleiste** unten.

```
+------------------------------------------------------------------------------+
|  Verwaltung Heizkessel                                                    [?] |  Titel + InfoKnopf
+------------------------------------------------------------------------------+
| ZONE A - Filterleiste                                                         |
|  Brennstoff [Gas       v]  Hersteller [(alle)   v]  Suche [                ]  |  Zeile 1
|  P_th von [  10] bis [  60] kW   eta von [0,95] bis [1,20]  [Weitere v][Zur.] |  Zeile 2
|  (Brennstoff: Gas x) (P_th 10...60 kW x) (eta >= 0,95 x)     15 von 63 Saetzen|  Zeile 3
+-----------------------------------+------------------------------------------+
| ZONE B links - die Liste          | ZONE B rechts - Detail                    |
| +---+------------+------+------+  |  -- Eingabe der Heizkesseldaten --------  |
| | o | Bezeichner | P_th | eta  |  |  Name:        [ecoTEC plus VC 15CS/1-5]   |
| +---+------------+------+------+  |  Hersteller:  [Vaillant Deutschland ...]  |
| | * | ecoTEC ... | 16,6 | 0,97 |  |  Leistung:    [  16,6] kW                 |
| | o | ecoCOMPACT | 15,0 | 0,98 |  |  Wirkungsgrad:[  0,97]                    |
| | o | ecoVIT ... | 19,3 | 0,87 |  |  Vorlauf:     [    70] Grad C             |
| +---+------------+------+------+  |  > Alle Parameter und ihre Verwendung     |
+-----------------------------------+------------------------------------------+
| ZONE C  [Speichern]     [Neu...] [Kopieren...] [Vergleichen] [Loeschen] [OK]  |
+------------------------------------------------------------------------------+
```

**Auf schmalem Schirm** (Medienabfrage 900 px, dieselbe Schwelle wie `Katalograhmen` und
`Zweispaltenauswahl`): Zone A bricht in einspaltige Felder um, Zone B stapelt Liste über
Detail, Zone C bleibt unten. Der Umbruch ist eine **Medienabfrage, kein `flex-wrap`** — dieselbe
Begründung wie bei den Pfeilzeichen der `Zweispaltenauswahl`.

### 5.3 Was für Verwaltung und Projektauswahl gleich ist — und was nicht

| | Verwaltung (Administration) | Projektauswahl („aus Datenbank übernehmen", Assistent) |
|---|---|---|
| Zone A | **identisch** — dasselbe Profil, dieselbe Leiste, dieselben Chips | **identisch**, aber **in der rechten Spalte** der `Zweispaltenauswahl` (#76: „Filter gehören über die Katalogliste in die RECHTE Spalte") |
| Zone B | `Katalograhmen`: Liste links, **Eingabe** rechts (schreibend) | `Zweispaltenauswahl`: **Projektliste** links, Katalog rechts; der Detailblock steht **unter dem Paar**, über die volle Breite, und ist **lesend** |
| Listenspalten | dieselben (aus dem Profil) | dieselben — plus die Spalte **„im Projekt"** (Häkchen), wenn der Satz schon übernommen ist |
| Zone C | Neu…, Kopieren…, Löschen, Vergleichen, Speichern, OK | ◀ Übernehmen / Entfernen ▶ (die zwei Pfeilknöpfe), Bearbeiten…, Löschen, Verwaltung…, OK |
| Merkmalsschalter | „nur eigene Sätze" | zusätzlich **„im Projekt verwendet"** |
| Höhe der Liste | verbleibende Höhe (`Katalograhmen`, Ausnahme zu W9‑B‑2) | `--epos-listenhoehe` = 22 rem |

**Der Filterzustand ist je Katalog gemeinsam** — wer im Projektdialog „Gas, 10…60 kW" gesetzt
hat und dann über „Verwaltung…" in die Administration springt, findet dort denselben
Ausschnitt. Das ist der Sinn eines Profils; zwei getrennte Zustände wären wieder zwei
Wahrheiten.

### 5.4 Alternativen — und warum sie es nicht werden

| | Alternative | Bewertung |
|---|---|---|
| **A1** | **Kartenansicht statt Tabelle** — je Gerät eine `Kachel` mit Name, Hersteller und drei Kennwerten, im `Kachelraster` | **Nein.** Eine Karte braucht das Drei- bis Vierfache der Höhe einer Tabellenzeile; bei 20 743 Modulen sind das rund 60 Bildschirmseiten statt 15. Vor allem aber: Karten **können nicht sortieren** und stellen die Werte nicht untereinander — genau das ist beim Vergleichen die Arbeit. Karten sind im Haus der EINSTIEG (Startseite, Komponentenauswahl), nicht die Auswahl aus einem Katalog |
| **A2** | **Facettenleiste links** (wie im Webshop): eine schmale Spalte mit allen Filtern, Liste rechts | **Nein.** Sie nimmt 240–280 px genau dort, wo die `Zweispaltenauswahl` schon eine linke Spalte hat (die Projektliste) — der Dialog hätte dann drei Spalten plus Pfeile. Und sie wäre eine **zweite Anordnungssprache** neben `Katalograhmen`; #76 hat das Haus gerade auf eine gebracht |
| **A3** | **Filterzeile als Aufklapper** — ein Knopf „Filter ▾", der die ganze Leiste zeigt oder verbirgt | **Teilweise ja** — als Staffelung. Die **erste** Zeile (Klapplisten + Suche) steht immer offen, die **zweite** (Zahlenbereiche) und der Rest hängen unter „Weitere Filter ▾". Alles zu verbergen wäre falsch: Ein Filter, den man nicht sieht, erklärt eine kurze Liste nicht — und die **Chips bleiben immer sichtbar**, gerade weil die Felder es nicht sind |
| **A4** | **Filtersymbol je Spaltenkopf** (Tabellenkalkulations-Art) | **Nein.** Ein Menü am Spaltenkopf ist ein Hover- und Mausbedienelement (iL4 verbietet Hover-Abhängigkeit), der Zustand ist unsichtbar, es gibt keinen Ort für einen Trefferzähler, und `QuickGrid` bringt so etwas nicht mit |
| **A5** | **Nur mehr Spalten, kein Filter** — die Liste zeigt die Parameter, Sortieren genügt | **Nein**, aber die Hälfte davon ist Teil des Vorschlags: Die Spalten kommen ohnehin. Für 63 Heizkessel würde Sortieren reichen; für 20 743 Module nicht |
| **A6** | **Alles bleibt, nur die zehn fehlenden Klapplisten werden ergänzt** | **Nein.** Das wären zehn weitere handgeschriebene Filterzeilen — genau der Zustand aus Befund 1.4, nur zehnmal schlimmer |

**Empfehlung: das Zonenmodell aus 5.2 mit der Staffelung aus A3.** Es ändert an den drei
Bausteinen, die das Haus gerade eingeführt hat (`Katalograhmen`, `Formularraster`,
`Zweispaltenauswahl`), **nichts** — es setzt einen vierten darüber.

### 5.5 Vergleich von zwei bis drei markierten Zeilen

Die Mehrfachwahl gibt es seit **W6‑E‑5** (`Zeilenwahl.Mehrfach`, `Zeilenmarkierung` mit
`Strg`/`Umschalt`). Damit ist der Vergleich fast geschenkt: Sind zwei oder drei Zeilen
markiert, wird der Knopf **„Vergleichen"** in Zone C frei und öffnet eine `Ueberlagerung`
(`epos-ueberlagerung--breit`, wie die Dreispalten-Detailansicht der Wärmepumpe seit W7‑E‑2)
mit **einer Zeile je Parameter und einer Spalte je Gerät**. Abweichende Werte werden
hervorgehoben, gleiche bleiben leise — dieselbe Anmutung wie `Kohaerenzzeile`
(„stimmig"/„abweichend").

Grenze: **drei** Geräte. Vier Spalten plus Parameterspalte passen auf einem 900‑px-Schirm nicht
mehr nebeneinander, und der Vergleich wäre wieder eine Tabelle, durch die man rollt.

Empfehlung: **ja, aber in Stufe S3** — er hängt an nichts, was S1 und S2 brauchen.

---

## 6. Das Mockup

`Mockups/Katalogfilter_Vorschlag.html` — eine Datei, kein CDN, kein Rahmenwerk, keine
Schriftdatei von außen. Farben, Maße und Klassennamen sind aus `EPOS.UI/wwwroot/epos-ui.css`
übernommen (Kopfband `#0F1F3D`, Reiterband AliceBlue `#f0f8ff`, cremefarbene Knöpfe `#f5f4ef`,
Beschriftungsspalte 12 rem, kurzes Zahlenfeld 8 em, Berührungsziel 44 px, Ecke 6 px). Es ist
ein **Bild**: Die Eingabefelder sind schreibgeschützt; belegt sind nur der Reiterwechsel und
das „×" der Chips (dann verschwinden die zugehörigen Zeilen und der Trefferzähler zählt neu).

| Reiter | zeigt | Datenherkunft |
|---|---|---|
| **M1 — Heizkessel (Verwaltung)** | Zone A vollständig, Liste mit sechs Spalten und Sortierpfeil auf „P_th", Detailbereich im `Formularraster`, Parameterübersicht zugeklappt, Aktionsleiste. Filter: Gas · 10…60 kW · η ≥ 0,95 → Trefferzähler **„15 von 63 Sätzen"** | `Tab_Heizkessel_STAMM` (63 Sätze) |
| **M2 — Wärmepumpe (Projektauswahl)** | `Zweispaltenauswahl`: links die zwei Wärmepumpen des Projekts „Heinestr 15", rechts der Katalog mit der Filterleiste darüber, dazwischen ◀ ▶. Detailblock unter dem Paar. Filter: Luft-Wasser · 5…12 kW · VL max ≥ 60 °C → **„7 von 51 Sätzen"** | `Tab_WP` (Projekt 1008) und `Tab_WP_STAMM` + `Tab_Kenndaten_STAMM` (Vorlaufgrenzen, COP A2/W35) |
| **M3 — PV-Module (Verwaltung, 20 749 Zeilen)** | derselbe Aufbau wie M1, mit Virtualisierungshinweis und Herstellerklappliste über 258 Werten. Filter: LONGi · 500…600 W · η ≥ 21,5 % → **„15 von 20 749 Sätzen"** | `VDI-3805-Daten/PV/CEC Modules.csv` (20 743 Datenzeilen, 258 Hersteller, 5 Technologien) plus die 6 Sätze aus `Tab_PV_STAMM` |

---

## 7. Vorschlag in drei Stufen

### Stufe S1 — der Baustein und die sieben Erzeugerkataloge in der Verwaltung

| Schritt | Inhalt |
|---|---|
| S1.1 | `Katalogfilterprofil` + `Katalogfilter` im Kern (Daten und Rechnung), mit `Suchmuster` und den Regeln aus 3.3; Kern-Proben |
| S1.2 | Baustein `EPOS.UI/Bausteine/Filterleiste.razor` (Klapplisten, Suche, Bereiche, Aufklapper, Chips, Trefferzähler, Zurücksetzen) + `bunit`-Test + Stilblattregeln |
| S1.3 | Die sieben Profile: Heizkessel, BHKW, Solarkollektoren, Pufferspeicher, PV-Modul, Wechselrichter, Stromspeicher |
| S1.4 | `KatalogBrowserDialog` (4 Ausprägungen) und `ModulKatalogDialog` (3 Ausprägungen) auf die Leiste umstellen; `KatalogFilterArt` und `HatHerstellerfilter` entfallen zugunsten des Profils |
| S1.5 | **Parameterspalten** in beiden Listen: `…StammCtrl.Katalogzeilen` liefert die Filter- und Anzeigewerte statt `ID, Bezeichner`; abgeleitete Größen (σ, C‑Rate, Fläche) werden dort gerechnet |
| S1.6 | Sortierung über die Spaltenköpfe; Wiederherstellen der Markierung nach einem Filterwechsel |
| S1.7 | Ressourcenschlüssel beider Sprachen + `Werkzeuge/ResourceDesigner` ziehen |

**Aufwand: 12–16 Agentenstunden.**
**Risiko: gering.** Reine Oberfläche und Leseweg; **der Referenzlauf ist unberührt** (keine
Rechenspalte wird angefasst). Wächter: `StilblattTests`, `ListenrahmenTests`,
`KatalogdialogTests`, `FormularrasterTests`, `ParametersatzTests`, neue Kernproben für
`Katalogfilter`, `SqlDialektPruefer` für jede neue Abfrage.
Offene Kante: die Umstellung von `SELECT ID, Bezeichner` auf die volle Zeile berührt sieben
Controller — dort wird gemessen, nicht geschätzt.

### Stufe S2 — die Projektauswahl und der Assistent

| Schritt | Inhalt |
|---|---|
| S2.1 | Dieselben sieben Profile in den Projektdialogen (`HeizkesselDialog`, `BhkwDialog`, `PufferspeicherDialog`, `PhotovoltaikDialog`, `StromspeicherDialog`, `SolarkollektorenDialog`, `StromganglinieDialog`/`SolarganglinieDialog`) — Leiste in die **rechte** Spalte der `Zweispaltenauswahl` |
| S2.2 | Wärmepumpe: `WaermepumpenKatalogFilter` wird ein Profil; die Leiste steht auch über der **Stammliste** der Verwaltung; die sieben Klapplisten werden 3 + 4 (Aufklapper) |
| S2.3 | Spalte und Schalter **„im Projekt verwendet"** (eine Zählabfrage für die ganze Liste, nicht je Zeile) |
| S2.4 | Der Assistent (Seiten 4, 5, 7) erbt die Leiste über dieselben Komponenten |
| S2.5 | Filterzustand je Katalog über die Sitzung, gemeinsam für Verwaltung und Projektdialog |

**Aufwand: 8–12 Agentenstunden.** **Risiko: gering–mittel** — elf Dialoge, aber alle auf
demselben Baustein; `ZweispaltenauswahlTests` ist der Wächter.

### Stufe S3 — Bedarf, Zeitreihen, Vergleich

| Schritt | Inhalt |
|---|---|
| S3.1 | Profile für die drei Bedarfskataloge (Typ, Jahressumme, Beschreibungssuche) — `BedarfAdminDialog` und `BedarfsProfileDialog` |
| S3.2 | Profile für die drei Zeitreihenkataloge samt Jahresarbeit/Spitze aus **einer** `GROUP BY`-Abfrage |
| S3.3 | **Vergleich** von zwei bis drei markierten Zeilen als `Ueberlagerung` (5.5) |
| S3.4 | Dieselbe Leiste in den **Importmasken** (`KatalogImportDialog`, `ModulImportDialog`) — dort löst sie die zwei vorhandenen Fassungen ab; damit gibt es die Mechanik nur noch einmal |

**Aufwand: 8–10 Agentenstunden.** **Risiko: mittel** bei S3.4 — die Importmasken sind
abgenommen, und ihr Filterverhalten ist bitgleich zum Vorläufer nachgewiesen; die Umstellung
muss das Verhalten erhalten, nicht angleichen.

**Summe S1–S3: 28–38 Agentenstunden.** Nach **S1** ist der Wunsch W14a‑E‑9 für die drei Köpfe
erfüllt; S2 und S3 tragen ihn in die Projektseite und in den Rest des Hauses.

**Reihenfolge:** S1 → S2 → S3. S1 allein ist auslieferbar; S2 ohne S1 ist es nicht (das Profil
kommt aus S1). S3.4 sollte **nicht** vor S2 kommen: Wer zuerst an den abgenommenen
Importmasken arbeitet, riskiert einen Rückschritt an einer Stelle, die schon stimmt.

---

## 8. Entscheidungsfragen

| Nr. | Frage | Empfehlung |
|---|---|---|
| **Q1** | Sind die Parameter je Katalog aus **Kapitel 4** die richtigen? | **Ja** — sie sind aus `ParameterVerwendung` abgeleitet (Stufe Simulation/Wirtschaftlichkeit) und um die Bauartmerkmale ergänzt, nach denen ein Planer sucht |
| **Q2** | Filterzustand **merken** — nur für die Sitzung oder dauerhaft? | **Sitzung**, je Katalog, gemeinsam für Verwaltung und Projektdialog (S2.5). Dauerhaft über `Dienste.Einstellungen` erst, wenn der Anwender es nach der Abnahme vermisst — ein gespeicherter Filter, an den man sich beim nächsten Programmstart nicht erinnert, lässt einen Katalog leer wirken |
| **Q3** | **Vergleich** von zwei bis drei Zeilen — ja oder nein? | **Ja, in S3.** Die Mehrfachwahl gibt es seit W6‑E‑5; der Vergleich ist eine Überlagerung und hängt an nichts |
| **Q4** | Welche **Design-Variante**? | **Das Zonenmodell aus 5.2** mit der Staffelung aus A3 (Zeile 1 offen, Zahlenbereiche unter „Weitere Filter ▾", Chips immer sichtbar). Kartenansicht (A1) und Facettenleiste (A2) werden **nicht** empfohlen |
| **Q5** | Ersetzen die **Zahlenbereiche** die sechs festen Leistungs- bzw. Volumenstufen? | **Nein, sie ergänzen sie.** Die Klappliste bleibt als Schnellwahl und **füllt** die zwei Bereichsfelder; wer genauer will, tippt. So geht keine Bedienung verloren, die der Anwender kennt |
| **Q6** | **Wo** wird gefiltert — im Speicher oder in SQL? | **Der Controller entscheidet**: bis rund 5 000 Zeilen im Speicher (Muster Wärmepumpe), darüber `WHERE` mit `DbParam`. Für die Komponente ändert sich nichts |
| **Q7** | Bekommt `Tab_Stromspeicher_STAMM` eine **Spalte `Firma`** (Befund D‑3)? | **Ja**, als eigener Schemaschritt mit Nachtrag aus dem Bezeichnerpräfix. Bis dahin füllt sich die Klappliste aus dem Präfix — dieselbe Regel, die der Modulimport schon anwendet |
| **Q8** | Wird das **Brennwert-Kennzeichen** (Befund D‑1) berichtigt? | **Ja** — 40 Sätze, deren Beschreibung „Brennwert" nennt, tragen `Brennwert = 0`. Der Filter bleibt auf dem Kennzeichen; berichtigt werden die **Daten**, nicht der Filter. Bis dahin heißt der Schalter „nur Brennwert (laut Kennzeichen)" |
| **Q9** | Wie geht der Filter mit dem **Wirkungsgrad in zwei Einheiten** um (Befund D‑2)? | Der Filter rechnet einen Wert **> 2 als Prozent** und teilt durch 100 — dieselbe wertabhängige Weiche, die `StromspeicherImportSatz` seit W13‑E‑2 für den Umlaufwirkungsgrad hat. Der eine Datensatz wird zusätzlich berichtigt |
| **Q10** | Gilt die Leiste auch für die **Importmasken**? | **Ja, in S3.4** — dann gibt es die Filtermechanik im ganzen Haus nur noch einmal. Nicht früher: Die Importmasken sind abgenommen |
| **Q11** | Gilt die Leiste auch für **Gebäude, Gebäudetypen, Klimadaten, Kosten**? | **Nicht in S1–S3.** Der Wunsch nennt drei Köpfe; `GebaeudeDialog` und `EnergietraegerDialog` haben bereits eine Suche. Wenn die drei Köpfe stehen, ist die Ausweitung eine Stunde je Katalog |
| **Q12** | Bekommt die Liste eine Spalte **„im Projekt verwendet"**? | **Ja, in S2.3** — aber nur in den **Projektdialogen**; in der Verwaltung wäre sie eine Zählabfrage über alle Projekte ohne Nutzen für die Pflege |

---

## 9. Entscheide

*Dieses Kapitel bleibt leer, bis der Anwender entschieden hat.*

**Kennung des Wunsches: `W14a‑E‑9`** (07.09.2026). Die Fragen aus Kapitel 8 tragen die
Kennungen `W14a‑E‑9‑Q1` … `W14a‑E‑9‑Q12`.

| Kennung | Entscheid | Datum | Umsetzung |
|---|---|---|---|
| — | — | — | — |

---

## 10. Offene Punkte

| Kennung | Punkt |
|---|---|
| **O‑1** | Die fünf Wärmepumpenspalten `Laenge`, `Breite`, `Hoehe`, `Gewicht`, `Raum` sind in `ParameterVerwendung` mit **`Keine`** eingestuft — niemand liest sie. Sie stehen weder im Filter noch in den Spalten dieses Vorschlags. Ob sie bleiben, ist eine eigene Frage |
| **O‑2** | Die **Stromkennzahl** des BHKW, die **C‑Rate** des Stromspeichers und der **COP bei A2/W35** sind hier abgeleitete Anzeigegrößen. Ob sie in den Katalog gehören (gerechnet beim Import, gespeichert), ist eine Datenmodellfrage und keine Oberflächenfrage |
| **O‑3** | Der `Motortyp` des BHKW führt 45 verschiedene Werte in 79 Sätzen, darunter Schreibvarianten desselben Motors („Gas-Otto-Motor", „Gas-Otto-Motor_ 2G", „Gas-Otto-Motor_2G"). Eine Klappliste darüber wäre unbrauchbar; eine Bereinigung ist eine eigene Aufgabe |
| **O‑4** | Der Hersteller steht in mehreren Schreibweisen desselben Hauses („EC Power A/S" 10 Sätze, „EC POWER A/S" 7; „2G Energy AG" 21, „2-G Energietechnik GmbH" 17). Die Klappliste zeigt sie als **verschiedene** Hersteller. Eine Zusammenführung gehört zur Dublettenpflege, nicht zum Filter |

---

## Anhang A — Die Zahlen des Mockups

Gemessen am 07.09.2026 gegen `Referenzlaeufe/Kenndaten_Test.sqlite` und
`VDI-3805-Daten/PV/CEC Modules.csv`.

| Größe | Wert |
|---|---|
| `Tab_Heizkessel_STAMM` gesamt | 63 |
| davon Gasbrennstoffe (1…5, 14) und 10 ≤ P_th ≤ 60 kW | 43 |
| Hersteller im Heizkesselkatalog | 5 (Vaillant 57, Buderus 2, Bosch 2, Viessmann 1, ohne 1) |
| `Tab_WP_STAMM` gesamt | 51 |
| davon Luft-Wasser **und** stetig **und** 5 ≤ P_N ≤ 12 kW | 24 |
| Quellenarten | Luft-Wasser 34, Sole-Wasser 14, Wasser-Wasser 1, ohne 2 |
| `Tab_Kenndaten_STAMM` (Kennlinienpunkte) | 1 960 |
| `Tab_BHKW_STAMM` | 79, 9 Hersteller, 45 Motortypen |
| `Tab_Pufferspeicher_STAMM` | 13, 4 Hersteller, 3 Speichertypen |
| `Tab_Solarkollektoren_STAMM` | 7, 2 Typen (Flach 4, Röhre 2) |
| `Tab_PV_STAMM` | 6 |
| `CEC Modules.csv` Datenzeilen / Hersteller / Technologien | **20 743** / 258 / 5 |
| `CEC Inverters.csv` Geräte / Hersteller | 2 343 / 152 (gemessen 06.09.2026) |
| CEC Energy Storage System List | 6 654 Zeilen / 130 Hersteller (gemessen 07.09.2026) |
| `Tab_Stromspeicher_STAMM` | 5, 3 Chemien |
| `Tab_Brauchwasser_STAMM` / `Tab_Prozesswaerme_STAMM` / `Tab_Stromverbraucher_STAMM` | 16 / 32 / 41 |
| `Tab_Waermebedarf_STAMM` / `Tab_Stromganglinie_STAMM` / `Tab_Solarganglinie_STAMM` | 4 / 3 / 1 |

**Die drei Filterstände des Mockups** — je Reiter der volle Filter und die Zahl, die bleibt,
wenn genau ein Chip abgewählt wird. Die Chip-Mechanik der HTML-Datei nimmt ihre Zahlen aus
dieser Tabelle; die Liste zeigt einen Auszug, der Zähler nennt die gemessene Wahrheit.

| Reiter | alle drei | ohne Klappliste | ohne Bereich 1 | ohne Bereich 2 | gesamt |
|---|---|---|---|---|---|
| **M1** Gas · 10…60 kW · η ≥ 0,95 | **15** | 24 (ohne Brennstoff) | 22 (ohne Leistung) | 43 (ohne η) | 63 |
| **M2** Luft-Wasser · 5…12 kW · VL max ≥ 60 °C | **7** | 10 (ohne Quelle) | 11 (ohne Leistung) | 24 (ohne Vorlauf) | 51 |
| **M3** LONGi · 500…600 W · η ≥ 21,5 % | **15** | 789 (ohne Hersteller) | 31 (ohne Leistung) | 18 (ohne η) | 20 749 |

## Anhang B — Was dieses Papier nicht behandelt

- **Den Rechenweg.** Kein Vorschlag berührt eine Simulations- oder Wirtschaftlichkeitsformel;
  der Referenzlauf `2026-09-06_R3_Straenge` bleibt in allen drei Stufen byte-gleich.
- **Die Dublettenpflege** der Hersteller- und Motortypschreibweisen (O‑3, O‑4) — sie hat mit
  `Konzept_Dublettenpruefung_Import_EPOS-Plan.md` ein eigenes Papier.
- **Die Kataloge außerhalb der drei Köpfe** (Gebäude, Gebäudetypen, Klimadaten, Kostenvorlagen,
  Energieträger, Gesetzesparameter) — siehe Q11.
- **Die Frage, ob ein Katalogparameter überhaupt gebraucht wird** (O‑1) — das beantwortet
  `ParameterVerwendung`, nicht ein Filter.
