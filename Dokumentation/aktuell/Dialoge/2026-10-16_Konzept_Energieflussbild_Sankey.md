# Energieflussbild (Sankey) im Abschnitt „Ergebnisse – Energie“

**Gegenstand.** Befund und Konzept zum Anwenderauftrag vom 16.10.2026 (Welle SK): Der Abschnitt
„Ergebnisse – Energie“ der Seite „Berichte & Kosten → Übersicht“ bekommt neben Tabelle und
Grafik eine dritte Darstellung — ein Energieflussbild (Sankey) über Strom, Wärme und Kälte
einer gewählten Version, aus den Jahresaggregaten des gespeicherten Simulationsergebnisses, und
dasselbe Bild im Wortbericht. Das Papier prüft zuerst an echten Zahlen, ob die Aggregate ein
geschlossenes Bild hergeben, und legt dann Knotenmodell, Renderer, Oberfläche, Bericht, Tests
und Schritte fest. Kein Produktcode ist Teil dieses Auftrags.

**Anwenderentscheide (16.10.2026, fest).**

| Kennung | Entscheid |
|---|---|
| **SK-1** | Ein Bild mit Strom, Wärme **und** Kälte. |
| **SK-2** | Dritte Stellung „Fluss“ am Umschalter „Tabelle \| Grafik \| Fluss“; die Wahl gilt je Benutzer wie heute (`Uebersicht.Ergebnisse.Ansicht`). |
| **SK-3** | Ein Bild für **eine** gewählte Version; die Auswahl steht über dem Bild, Vorgabe ist der Stamm; eine nicht simulierte Version hat kein Bild und wird benannt. |
| **SK-4** | Der Wortbericht bekommt das Bild in derselben Welle (Vorlagenfeld mit Katalogfassung, Bausteinweg). |

**Abgrenzung.** Zeichenmodell, SVG-Weg, Zeigerzeile und Farbrollen sind in
[`Konzept_Diagramme_Interaktiv_EPOS-Plan.md`](../Konzept_Diagramme_Interaktiv_EPOS-Plan.md)
festgelegt (Abschnitte 6 und 9); dieses Papier nimmt sie als gegeben. Die Kennzahlen des
Abschnitts und ihre Gruppen stehen im Kern (`Energievergleich`, `KennzahlenKatalog`) und bleiben
unverändert. Mockup: `Dokumentation/aktuell/Mockups/Energieflussbild_Mockup.html` (Umschalter,
Versionswahl, Bild mit Strom, Wärme und Kälte, Speicher mit Verlustband, Zeigertext, verborgene Textalternative,
schmale Ansicht). Ist das Bild umgesetzt, wandert dieses Papier nach `ueberholt/`; der gültige
Stand steht dann im Code, in der Wiki-Quelle „Varianten“ und im Konzept Diagramme (Abschnitt 9,
Farbrollen).

---

## 1. Befund: Was die Jahresaggregate hergeben

### 1.1 Prüfweg

Geprüft wurde an der eingefrorenen Basis `Referenzlaeufe/2026-10-16_R57_Schnellaufheizung`
(Datei `aggregate.csv` je Projekt — dieselben Zahlen, die `ErgebnisCtrl.Load` aus den
`Tab_Ergebnis*`-Tabellen liest, dazu die Skalare des Laufs), ohne eigenen Lauf. Je Knoten wurde
Zufluss gegen Abfluss + Verlust + Füllstandsänderung gehalten. Neun Projekte: 1018 (BHKW, Kessel,
Puffer), 1049 (dazu Solarthermie), 1030 (BHKW-Kaskade, Puffer, Strombedarf), 1046 (Wärmepumpe mit
Heizstab, Kessel, Photovoltaik, Stromspeicherflotte), 1007 (dieselbe Anlage ohne Flotte), 1058
(AK3: Wärmepumpe mit Erdsonde, BHKW, Elektrokessel, Heizungspuffer, Kühlung über die Wärmepumpe),
1017 (Kühlung über die Wärmepumpe, ohne Puffer), 1063 (Kältemaschine mit Kältespeicher), 1072
(dazu Split-Geräte). Die Aggregate stehen mit zwei Nachkommastellen in MWh; ein Rest bis
0,01 MWh je Summand ist Rundung.

Die Rechnung je Knoten (alle Werte MWh/a):

- **Wärme:** Zufluss = Wärme der Wärmepumpe (`Waermeproduktion_WP`, ohne Heizstab) + Heizstab
  (`Stromverbrauch_Heizstab`, 1:1 Wärme) + BHKW-Wärme + Kesselwärme + Solarwärme (`Waermeproduktion`,
  die gelieferte Menge). Abfluss = gedeckter Bedarf (`Waermebedarf_Gesamt − Waermerestbedarf`) +
  Pufferverluste + Füllstandsänderung (`SOC_Ende`, der Lauf beginnt leer).
- **Strom:** Zufluss = Netzbezug (`Stromrestbedarf`) + BHKW-Strom + PV-Strom. Abfluss = Verbraucher
  (`Strombedarf_Gesamt`) + Strom der Wärmepumpe + Heizstab + Kühlstrom samt Hilfsstrom +
  Split-Strom + Strom des Elektrokessels (`Heizkessel.Stromverbrauch`) + bekannte Einspeisung
  (PV-Überschuss bzw. Netzeinspeisung der Flotte) + Speicherverluste + Füllstandsänderung +
  Eigenverbrauch des Speichersystems.
- **Kälte:** Zufluss = Kälte der Wärmepumpe + Kälte der Kältemaschinen (`Kaelteproduktion_MWh`,
  samt dem Teil, der den Kältespeicher lädt) + Split-Kälte. Abfluss = gedeckter Kältebedarf +
  Verluste und Füllstandsänderung des Kältespeichers.

### 1.2 Bilanztabelle

| Projekt | Knoten | Zufluss | Abfluss + Verlust + ΔSOC | Rest MWh | Rest % | Bemerkung |
|---|---|---:|---:|---:|---:|---|
| 1018 | Wärme | 53,890 | 52,720 + 1,158 + 0,011 | +0,002 | 0,00 | schließt |
| 1018 | Puffer | 38,949 | 37,781 + 1,158 + 0,011 | 0,000 | 0,00 | schließt exakt |
| 1018 | Strom | 18,070 | 0 | +18,070 | 100 | kein Verbraucher: alles ist BHKW-Einspeisung, kein Aggregat |
| 1049 | Wärme | 53,560 | 52,720 + 0,812 + 0,026 | +0,002 | 0,00 | schließt; Solarüberschuss 21,01 ist Potenzial, keine gelieferte Energie |
| 1049 | Puffer | 51,559 | 50,721 + 0,812 + 0,026 | 0,000 | 0,00 | schließt exakt |
| 1030 | Wärme | 6 139,33 | 6 137,56 + 1,214 + 0,551 | +0,006 | 0,00 | schließt |
| 1030 | Strom | 4 790,48 | 4 790,09 | +0,390 | 0,01 | Rest = BHKW-Einspeisung (aus den Viertelstundenreihen nachgerechnet 0,392) |
| 1046 | Wärme | 71,410 | 71,420 | −0,010 | −0,01 | Rundung; Heizstab 37,81 zählt 1:1 als Wärme |
| 1046 | Strom | 69,110 | 67,610 + 0,903 + 0,148 + 0,014 + 0,438 | −0,003 | 0,00 | schließt **nur** mit dem Eigenverbrauch des Speichersystems 0,438 (Standby) |
| 1046 | Stromspeicher | 1,208 | 1,045 + 0,148 + 0,014 | 0,000 | 0,00 | schließt exakt (Flottenzahlen) |
| 1007 | Strom | 67,990 | 67,610 + 0,330 | +0,050 | 0,07 | Rest = Verluste und Füllstand des Stromspeichers (der Referenzlauf exportiert die Zeile nicht; im Ergebnisbaum vorhanden) |
| 1058 | Wärme | 72,030 | 70,130 + 1,733 + 0,165 | +0,001 | 0,00 | schließt; Umweltwärme 43,090 = Erdreichentzug 43,089 |
| 1058 | Puffer | 72,024 | 70,125 + 1,733 + 0,165 | 0,000 | 0,00 | schließt exakt |
| 1058 | Strom | 35,140 | 31,532 | +3,608 | 10,3 | Rest = BHKW-Einspeisung; kein Aggregat im Ergebnisbaum |
| 1058 | Kälte | 3,810 | 3,810 | 0,000 | 0,00 | schließt |
| 1063 | Wärme | 75,320 | 75,330 | −0,010 | −0,01 | Rundung; 0,06 ungedeckt |
| 1063 | Strom | 42,000 | 31,185 | +10,815 | 25,8 | Rest = BHKW-Einspeisung |
| 1063 | Kälte | 4,153 | 4,051 + 0,102 + 0,000 | +0,001 | 0,01 | schließt; Kältespeicher Ladung 2,478 = Entladung 2,376 + Verlust 0,102 |
| 1072 | Kälte | 10,142 | 10,050 + 0,092 | +0,001 | 0,01 | schließt; Split 5,0 Kälte aus 1,529 Strom |
| 1072 | Strom | 42,940 | 33,019 | +9,921 | 23,1 | Rest = BHKW-Einspeisung |
| 1017 | Kälte | 4,050 | 4,050 | 0,000 | 0,00 | schließt |

### 1.3 Wärme: schließt

Die Wärmebilanz schließt in allen neun Projekten auf Rundungsmaß (≤ 0,01 MWh). Die Pufferzeilen
(`Tab_ErgebnisPufferspeicher`: `Ladung_gesamt`, `Entladung_gesamt`, `Verluste_gesamt`, `SOC_Ende`,
`Entladung_Kanal`) schließen exakt. Zwei Regeln, die der Flussbau kennen muss:

- `Waermeproduktion_WP` ist die Verdichterwärme **ohne** Heizstab; der Heizstab zählt mit seinem
  Strom 1:1 als Wärme (1046: 25,09 + 37,81 + 8,51 = 71,41 gegen 71,44 Bedarf − 0,02 ungedeckt).
- **Umweltwärme** gibt es nicht als Aggregat; sie ist `Waermeproduktion_WP − Stromverbrauch_WP`.
  Bei Erdsonden deckt sich die Differenz mit dem gespeicherten Erdreichentzug
  (`ErdreichErgebnisSpeicher.Zeile.JahresentzugKWh`, 1058: 43 090 gegen 43 089 kWh) — die
  Ableitung ist damit belegt.
- Der **Solarüberschuss** (`Solarthermie.Ueberschuss`, 1049: 21,01 MWh) ist nicht abgenommene
  Kollektorwärme (Stagnation), keine Energie im System; `Waermeproduktion` ist die gelieferte
  Menge und schließt die Bilanz.
- Ein Brennwertkessel kann mehr Wärme liefern als Brennstoff (Heizwert) eingeht (1049:
  Jahresnutzungsgrad 100,05 %; 1030: 103,11 %). Ein Wandlerknoten mit negativem Verlust ist in
  einem Sankey nicht zeichenbar — siehe SK-Q4.

### 1.4 Strom: schließt bis auf eine Größe

Die Strombilanz ist im Lauf viertelstündlich geschlossen (`SimulationControl`,
`Strombedarf_Verbraucher_viertelstuendlich`, `BhkwEinspeisungDesLaufs`). Auf den Aggregaten
schließt sie in jedem Projekt, sobald **eine** Größe eingesetzt wird, die der Ergebnisbaum nicht
führt:

- **BHKW-Einspeisung.** `ErgebnisBHKWModel` kennt `Stromproduktion` und `Strombedarfsdeckung`,
  nicht die Einspeisung. Sie steht nur in `Tab_ErgebnisStromMatrix.KwkEinspMWh` — und diese
  Zeile schreibt allein `WirtschaftlichkeitCtrl.Persistiere`, also erst eine
  Wirtschaftlichkeitsrechnung, nicht der Simulationslauf; in der Testdatenbank liegt sie von den
  fünf geprüften Projekten nur für 1018, mit einem Stand vom August. Der Zeitreihensatz, aus dem der
  `KennzahlenKatalog` die PV-Abregelung liest, ist in der Übersicht nicht geladen
  (`EnergievergleichCtrl.Lade` lädt den gespeicherten Baum, keine Reihen).
- **Eigenverbrauch des Speichersystems** (`SimulationControl.SpeichersystemEigenverbrauchKwh`,
  nur mit Flotte und gepflegtem `Standby_Verbrauch`): im Referenzlauf exportiert, im Ergebnisbaum
  nicht (1046: 0,438 MWh, 0,6 % des Stroms).
- **PV-Abregelung** an der Einspeisegrenze (`SimulationErgebnisCtrl.PhotovoltaikErgebnis.AbregelungMwh`):
  nicht im Ergebnisbaum; in keinem der neun Projekte wirksam.

Der Rest am Stromknoten ist deshalb in jedem BHKW-Projekt genau die Einspeisung (1030: Rest 0,390,
aus den Viertelstundenreihen der Basis nachgerechnet 0,392). Eine Nachrechnung aus Stundenreihen
trifft die Zahl bei Wärmepumpen- und Kesselstrom nur näherungsweise (1058: 5,3 statt 3,6), weil
die Kaskade je Viertelstunde bucht — die exakte Menge kennt nur der Lauf.

### 1.5 Kälte: schließt

Kälte der Wärmepumpe (`Kaelteproduktion_WP`), Kälte der Kältemaschinen (`Kaelteproduktion_MWh`,
samt Ladung des Kältespeichers), freie Kühlung (`FreieKuehlung_MWh`) und Split-Kälte
(`ErgebnisKaelteDezentralModel.Kaelte_MWh`) decken den Kältebedarf (`Kaeltebedarf_Gesamt`); der
Kältespeicher steht als Pufferzeile mit `Verwendung = Kaelte` und `Entladung_Kanal[KUEHLUNG]`.
Alle vier Kälteprojekte schließen auf ≤ 0,001 MWh. Der Strom der Kälte liegt vollständig vor
(`Stromverbrauch_Kuehlung`, `Stromverbrauch_MWh`, `Hilfsstrom_MWh`, `Strom_MWh` der Split-Zeile).

### 1.6 Herkunft je Fluss

| Fluss | Herkunft | Aggregat / Ableitung |
|---|---|---|
| Netzbezug → Strom | direkt | `Energiebedarf.Stromrestbedarf` |
| Photovoltaik → Strom | direkt | `Photovoltaik.Stromproduktion` |
| BHKW → Strom | direkt | `BHKW.Stromproduktion` |
| Brennstoff je Träger → BHKW / Kessel | direkt | `BHKWModulModel.Verbrauch`, `HeizkesselModulModel.Verbrauch` mit `CarrierId` (Heizwert; dieselbe Quelle wie die Trägerzeilen des Abschnitts über `EnergiekostenJeTraeger`) |
| Strom → Wärmepumpe / Heizstab / Elektrokessel / Kältemaschine / Split / Wärmepumpe (Kühlen) | direkt | `Stromverbrauch_WP`, `Stromverbrauch_Heizstab`, `Heizkessel.Stromverbrauch` (bei Brennstoff Strom), `Stromverbrauch_MWh + Hilfsstrom_MWh`, `Strom_MWh`, `Stromverbrauch_Kuehlung` |
| Umweltwärme → Wärmepumpe | abgeleitet | `Waermeproduktion_WP − Stromverbrauch_WP` (belegt am Erdreichentzug) |
| Umwelt → Wärmepumpe (Kühlen) / Kältemaschine / Split / freie Kühlung | abgeleitet | Kälte − Strom (Konvention, Abschnitt 2.1) |
| Kondensationswärme → Brennwertkessel | abgeleitet | `max(0, Wärme − Brennstoff)` je Modul (SK-Q4) |
| Wandler → Wärme (Bus) | direkt | `Waermeproduktion_WP`, `Stromverbrauch_Heizstab`, `BHKW.Waermeproduktion`, `Heizkessel.Waermeproduktion`, `Solarthermie.Waermeproduktion` |
| Wandler → Verlust | abgeleitet | Brennstoff − Wärme (− Strom) je Wandler, nie negativ |
| Wärme → Puffer, Puffer → Senken, Puffer → Verlust | direkt | `Ladung_gesamt`, `Entladung_Kanal[k]`, `Verluste_gesamt`, `SOC_Ende` |
| Wärme → Heizung / Warmwasser / Prozesswärme (direkt) | abgeleitet | `Waermebedarf_Kanal[k] − Entladung_Kanal[k]`, der ungedeckte Rest nach SK-Q2 |
| Kälte → Kältespeicher → Kühlung | direkt | Pufferzeile `Verwendung = Kaelte` |
| Strom → Stromspeicher → Strombedarf, → Verlust | direkt | `Ladung_Gesamt`, `Entladung_Gesamt`, `Verluste_Gesamt` je `ErgebnisStromspeicherModel`; Füllstandsänderung als `Ladung − Entladung − Verluste` |
| Strom → Einspeisung PV | direkt | `Photovoltaik.Ueberschuss` (mit Flotte `Einspeisung_Mit` der Speicherzeile) |
| Strom → Einspeisung BHKW | **bilanziert** | Rest am Stromknoten, wenn ein BHKW rechnet und 0 ≤ Rest ≤ BHKW-Strom (Abschnitt 2.4) |
| Strom → Eigenverbrauch Speichersystem | **fehlt** | nur im Lauf; erscheint als „nicht zugeordnet“ (1046: 0,6 %) |
| Strom → PV-Abregelung | **fehlt** | nur im Lauf; mit Einspeisegrenze erscheint der Betrag als „nicht zugeordnet“ |
| Solarthermie: nicht nutzbarer Überschuss | nicht gezeichnet | `Solarthermie.Ueberschuss` nur in Zeigertext und Textalternative (SK-Q3) |

### 1.7 Folgerung

Ein kleines Zusatzaggregat (`Tab_ErgebnisBHKW.Stromeinspeisung`, `Tab_ErgebnisStromspeicher.Eigenverbrauch_System`,
`Tab_ErgebnisPhotovoltaik.Abregelung`) wäre fachlich sauber, **berührt aber den Referenzlauf**:
Der Export liest `SELECT * FROM Tab_Ergebnis*`, eine neue Spalte ist eine neue Zeile in
`aggregate.csv`, und der Vergleich meldet sie als „Eintrag nur im Vergleichslauf“ — die Basis
wäre neu einzufrieren, und es bräuchte einen Schemaschritt (nächste freie Nummer 239). Den
Rechenweg änderte keine der drei Spalten. Empfehlung (SK-Q1): **ohne Schemaschritt** — die
BHKW-Einspeisung wird am Stromknoten bilanziert (die Bilanz ist im Lauf geschlossen, der Rest
ist exakt bis auf Rundung), Standby und Abregelung erscheinen als benanntes Band „nicht
zugeordnet“ (selten, klein). Die drei Spalten bleiben als späterer Schritt vorgemerkt.

---

## 2. Knoten- und Flussmodell

### 2.1 Sicht und Konvention

**Endenergie-Sicht** mit drei Sammelknoten („Busse“) Strom, Wärme, Kälte. Jede Kilowattstunde
erscheint genau einmal je Spalte; ein Band trägt die gelieferte, nicht die mögliche Energie. Zwei
Konventionen, die das Bild lesbar und jeden Wandlerknoten geschlossen halten:

1. **Umweltanteil als Zufluss.** Eine Wärmepumpe bekommt neben dem Strom das Band „Umweltwärme“
   (Wärme − Strom). Für Kälteerzeuger gilt dasselbe spiegelbildlich: Ein Band „Umwelt (Kälte)“
   = Kälte − Strom tritt ein, damit der Knoten mit EER > 1 schließt. Das ist eine Nutzenergie-
   Konvention (die Abwärme an die Umgebung bleibt ungezeichnet), sie steht so in der Legende
   des Zeigertexts („Umweltanteil nach Jahresarbeitszahl“) — SK-Q6.
2. **Speicher als Zwischenknoten.** Der Bus lädt den Speicher (`Ladung`), der Speicher liefert
   `Entladung` an die Senken und ein Verlustband; was direkt gedeckt wird, umgeht den Speicher
   (`Bedarf_k − Entladung_k`). Für den Stromspeicher geht die Entladung an „Strombedarf“ — der
   Lauf entlädt in den Rest des Anschlusses, eine Zuordnung zu einzelnen Verbrauchern gibt es
   nicht, und der Zeigertext sagt das.

### 2.2 Spalten und Knoten

Sieben Spalten; ein Knoten erscheint nur, wenn ein Band an ihm hängt.

| Spalte | Knoten (Reihenfolge von oben) | Quelle |
|---|---|---|
| 0 Quellen | Netzbezug · Photovoltaik · je Brennstoffträger (Erdgas, Heizöl, Pellets, …, Name des Trägers) · Umweltwärme · Umwelt (Kälte) · Kondensationswärme · Solarthermie | `Energiebedarf`, `Photovoltaik`, Modulzeilen mit `CarrierId`, Ableitungen |
| 1 Wandler I | BHKW · Heizkessel (Brennstoff) | `BHKW`, `Heizkessel` (ohne Elektrokessel) |
| 2 Bus Strom | Strom | Summe der Zuflüsse |
| 3 Wandler II | Wärmepumpe (Heizen) · Heizstab · Elektrokessel · Wärmepumpe (Kühlen) · Kältemaschine (je Maschine) · freie Kühlung · Split-Geräte (je Zeile) | `Waermepumpe`, `Heizkessel` mit Brennstoff Strom, `Kaeltemaschinen`, `KaelteDezentral` |
| 4 Busse Wärme, Kälte | Wärme · Kälte | Summen |
| 5 Speicher | Pufferspeicher (je Zeile, Verwendung Heizung/Brauchwasser/Prozess) · Kältespeicher · Stromspeicher (je Zeile) | `Pufferspeicher`, `Stromspeicher` |
| 6 Senken | Heizung · Warmwasser · Prozesswärme · Kühlung · Strombedarf · Einspeisung PV · Einspeisung BHKW · ungedeckt · nicht zugeordnet | `Waermebedarf_Kanal`, `Kaeltebedarf_Gesamt`, `Strombedarf_Gesamt`, Rest |

Verluste enden nicht in einer Sammelsenke rechts, sondern in einem **schmalen Verlustknoten in
der Lücke zur nächsten Spalte** (Spalte + ½, direkt unter dem Wandler bzw. Speicher), beschriftet
„Verlust 1,9“. So kreuzt kein Verlustband die Busse, und der Leser sieht den Verlust dort, wo er
entsteht (SK-Q7).

Die Wärmepumpe steht einmal, wenn sie nur heizt; heizt und kühlt sie, sind es zwei Knoten
(„Wärmepumpe (Heizen)“, „Wärmepumpe (Kühlen)“), weil beide verschiedene Busse bedienen. Mehrere
Pufferspeicher stehen je als Knoten; die Busse sind je Energieform genau einer.

### 2.3 Flüsse

Der Flussbau ist eine Kernklasse `Energiefluss` (`EPOS.Kern/Allgemein/Bericht/Energiefluss.cs`)
mit `Energiefluss.Aus(VariantenDaten v, bool englisch)` → `EnergieflussModell` (Knoten, Flüsse,
Bilanz je Knoten, Hinweise). Eingang ist dieselbe `VariantenDaten`, die `EnergievergleichCtrl.Lade`
für den Abschnitt baut (Ergebnisbaum, Brennstoffmengen, Trägernachweise); nichts wird neu
geladen, nichts simuliert. Jeder Fluss trägt `Von`, `Nach`, `Mwh`, `Rolle` (Farbrolle),
`Herkunft` (direkt / abgeleitet / bilanziert) und einen fertigen Zeigertext; ein Knoten trägt
Zufluss, Abfluss, Rest und Spalte. Die Reihenfolge der Knoten ist die der Tabelle 2.2, die der
Bänder an einem Knoten folgt der Lage des Gegenknotens (weniger Kreuzungen).

### 2.4 Bilanzprüfung und Verhalten bei fehlendem Schluss

Je Knoten gilt `|Zufluss − Abfluss| ≤ max(0,05 MWh; 0,5 % des Zuflusses)` als geschlossen; ein
Rest innerhalb der Toleranz wird nicht gezeichnet, der Knoten nimmt die Zuflusshöhe. Darüber:

1. **Stromknoten mit BHKW:** Ist `0 ≤ Rest ≤ BHKW-Strom`, wird der Rest zum Band „Einspeisung
   BHKW“ mit Herkunft *bilanziert*; der Zeigertext sagt „bilanziert aus der Strombilanz des
   Laufs“. Ohne BHKW oder außerhalb der Plausibilität gilt 2.
2. **Sonst:** ein Band „nicht zugeordnet“ (Rolle `REST`, gestrichelter Rand) vom Knoten in die
   Senkenspalte (bei negativem Rest aus der Quellenspalte in den Knoten), dazu eine leise
   Hinweiszeile unter dem Bild („Bilanz am Knoten Strom schließt nicht: 0,44 MWh/a nicht
   zugeordnet“) und dieselbe Zeile im Assistententext. Das Band ist Pflicht, nicht nur der
   Hinweis: Ein Sankey, dessen Knoten sichtbar nicht schließt, wird als Fehler gelesen; ein
   benanntes Band sagt, was fehlt (SK-Q5).

Ungedeckter Wärme- oder Kältebedarf (`Waermerestbedarf`, `Kaelterestbedarf`) ist kein
Bilanzfehler, sondern ein Band „ungedeckt“ (Rolle `REST`) aus der Quellenspalte in die Senke —
nur, wenn es die Mindestbandbreite erreicht; sonst nennt es der Reiter „Tabelle“ (SK-Q2).

### 2.5 Kleine Flüsse, Mindestbandbreite, Bündelung

- Maßstab: Bildpunkte je MWh aus der höchsten Spaltensumme über die nutzbare Höhe; ein Band
  unter **2 px** wird 2 px breit gezeichnet und zählt mit 2 px in die Knotenhöhe (die Zahl steht
  im Zeigertext).
- Ein Band unter **8 px** trägt keine Wertbeschriftung; ein Knoten unter 12 px keine Beschriftung
  im Bild (beide stehen im Reiter „Tabelle“).
- **Bündelung:** Quellknoten (Brennstoffträger, Split-Zeilen, Pufferspeicher), deren Summe unter
  1 % des zugehörigen Busses liegt und von denen es mehr als drei gibt, werden zu „weitere
  (n)“ gebündelt; der Reiter „Tabelle“ führt sie einzeln. Ein einzelner kleiner Knoten bleibt stehen.
- Ein Fluss von 0 erscheint nicht; eine Version ohne Wärme, Strom oder Kälte lässt den Bus weg.

---

## 3. Zeichnen: `ChartRenderer.Energiefluss*`

### 3.1 Layout

Neue Teildatei `EPOS.Kern/Allgemein/Bericht/ChartRenderer.Energiefluss.cs`, ein reines
Pixelbild ohne Zeichenfläche (wie Versionsbalken und Kesselkennlinie): kein Zoom, der Zeigertext
kommt aus `data-wert` des Elements unter dem Zeiger (DG-E3-10). `EnergieflussModell(EnergieflussModell daten, Bildmass mass)`
→ `Zeichenmodell`, dazu `EnergieflussBild(…)` als PNG für die Proben.

- Spalten gleichmäßig über die Breite abzüglich Beschriftungsrand links und rechts (je 150 px
  breit, 100 px schmal); Knotenbreite 14 px; Lücke zwischen Knoten einer Spalte 10 px; jede
  Spalte vertikal zentriert.
- Spaltentitel oben („Quellen“, „Wandler“, „Strom“, „Wandler“, „Wärme · Kälte“, „Speicher“,
  „Bedarf“) in `TEXT`, 11 pt.
- Höhe des Bildes: breit 1 180 × 560, schmal 680 × 640, Bericht 1 240 × 620 (Faktor 2 auf
  620 × 310 pt). Die Breite kommt aus `Bildmass`; die Beschriftungsregeln schalten bei
  `Breite < 900` auf „schmal“ (Abschnitt 3.3).

### 3.2 Bänder

Ein Band ist ein `Pfad` (geschlossen, Füllung in der Farbrolle mit Deckung 0,55, kein Rand):
Oberkante von `(x0, y0o)` nach `(x1, y1o)` als abgetastete S-Kurve
`y(t) = y0 + (y1 − y0)·(3t² − 2t³)` mit 24 Stützstellen, Unterkante zurück. Das Zeichenmodell
kennt keine Bézierkurve, und der SVG-Schreiber wie der Skia-Maler zeichnen denselben Polygonzug —
Bericht und Bildschirm bleiben byte- bzw. knotengleich. Ein Verlustband endet in seinem
Verlustknoten (Spalte + ½) mit derselben Kurve. Knoten sind `Rechteck` in `KNOTEN` mit 1-px-Rand
`RAHMEN`.

### 3.3 Beschriftung, Zeiger, Marken

- Quellen beschriften links des Knotens, Senken rechts, innere Spalten über dem Knoten,
  11 pt / schmal 10 pt. **Am Bildschirm tragen Knoten nur ihren Namen, Bänder keine Zahlen**
  (SK-Q12); die Werte erscheinen im Zeigertext. Im Word-Bericht (kein Überfahren) trägt jeder
  Knoten unter dem Namen seine Jahressumme „56,3 MWh/a“; Bänder bleiben ohne Zahlen.
- Schmal: Namen in Kurzform (`EnergieflussKnoten.Kurzname`, Ressource); die Werte stehen auch
  hier nur im Zeigertext, der Reiter „Tabelle“ bleibt vollständig.
- Jedes Band trägt `Marke = "fluss:<von>:<nach>"` und `Wert = "<Von> → <Nach>: 38,4 MWh/a
  (71 % des Wärmebedarfs)"`; ein bilanziertes Band ergänzt „· bilanziert“, ein abgeleitetes
  „· abgeleitet: Wärme − Strom“. Jeder Knoten trägt `Marke = "knoten:<id>"` und `Wert =
  "<Name>: Zufluss x · Abfluss y MWh/a"`. Der Baustein `DiagrammSvg` zeigt den Wert in seiner
  Zeigerzeile; eine Änderung am Baustein ist nicht nötig (`data-wert` steht an jedem Befehl,
  auch am `Pfad`).
- Keine Legende im Bild (`LegendeSchaltbar="false"` wie bei Ring und Kuchen); die Farben sind
  die der Rollen und damit die der übrigen Bilder.

### 3.4 Farbrollen

Vorhandene Rollen tragen die Bänder: `STROM_NETZ`, `STROM_PV`, `STROM_BHKW`, `STROM_SPEICHER`,
`SPEICHERLADUNG`, `WAERME_WP`, `WAERME_BHKW`, `WAERME_KESSEL`, `WAERME_SOLAR`, `HEIZSTAB`,
`HEIZWAERME`, `WARMWASSER`, `PROZESSWAERME`, `KAELTE_DEZENTRAL`, `VERBRAUCH_GESAMT` (Strombedarf),
`UEBERSCHUSS` (Einspeisung), `REST` (ungedeckt, nicht zugeordnet), `TEXT`, `RAHMEN`. Die Regel der
Zuordnung: **Ein Band in einen Bus trägt die Rolle seiner Quelle** (Netz, Photovoltaik, BHKW-Strom,
Wärmepumpe, Kessel …), **ein Band aus einem Bus die Rolle seiner Verwendung** (Wärmepumpe,
Heizstab, Elektrokessel, Kälteerzeuger, Strombedarf, Speicherladung, Einspeisung; aus dem Wärmebus
die Kanalrollen Heizung, Warmwasser, Prozesswärme). So sagt die Farbe am Strombus, wofür der
Strom gebraucht wird, und am Wärmebus, woher die Wärme kommt. **Fünf neue Rollen** in
`Farbpalette.Vorgabe` und in `Diagrammfarben` (Gruppe „Erzeuger und Bedarf“ bzw. „Allgemein“),
mit Ressourcen in beiden Sprachen:

| Rolle | Wofür | Vorgabe |
|---|---|---|
| `BRENNSTOFF` | Bänder Brennstoffträger → Wandler (alle Träger eine Rolle; der Name unterscheidet) | #8B6F47 |
| `UMWELTWAERME` | Umweltwärme → Wärmepumpe, Umwelt (Kälte) → Kälteerzeuger, Kondensationswärme | #7FB8A4 |
| `KAELTE` | Kälte der Erzeuger → Bus Kälte → Kältespeicher → Kühlung (Split-Bänder behalten `KAELTE_DEZENTRAL`) | #2A7FA6 |
| `VERLUST` | Verlustbänder und Verlustknoten | #B8B8B8 |
| `KNOTEN` | Knotenrechtecke | #4A5769 |

Die Wache `ChartRendererRollenTests` bekommt das Bild als `InlineData("Energiefluss")`: Jedes
Band und jeder Knoten tragen genau ihre Rolle; mit der Tauschpalette fällt jeder aufgelöste
Farbwert auf. `DiagrammfarbenWacheTests` bleibt grün, weil weder Hülle noch Komponente eine
Farbzahl kennen.

---

## 4. Oberfläche

### 4.1 Dritte Stellung

`UebersichtErgebnisse.razor`: der Umschalter zeichnet drei Stellungen aus einer Liste
(`ANSICHT_TABELLE`, `ANSICHT_GRAFIK`, neu `ANSICHT_FLUSS = "fluss"`), `role="radiogroup"` wie
heute, Pfeiltasten laufen über alle drei, Pos1/Ende auf die erste und letzte. Die Ablage bleibt
`Uebersicht.Ergebnisse.Ansicht` (ein unbekannter Wert fällt wie heute auf die Tabelle).
Ressourcen: `BK_ERG_ANSICHT_FLUSS` („Fluss“ / „Flow“), dazu die Texte der Komponente unten.
`IstGrafik` wird zu einem Dreiwert (`Ansicht`), die Fälle der Tabelle bleiben unverändert.

### 4.2 Versionswahl (SK-3)

Über dem Bild steht ein `Auswahlfeld` „Version“ mit den Versionen des Vergleichs in der
Reihenfolge der Spalten (Stamm zuerst, Vorgabe Stamm); die Wahl gilt je Sitzung
(Sitzungsablage der Hülle wie die Klappgruppen, nicht je Benutzer). Eine nicht simulierte Version
steht im Feld mit dem Zusatz „(nicht simuliert)“, ist wählbar und zeigt statt des Bildes die
Karte „Für diese Version liegt kein Simulationsergebnis vor“ (`BK_ERG_KEINE_SIMULATION`-Familie,
neuer Schlüssel `BK_ERG_FLUSS_NICHT_SIMULIERT`). „nur Unterschiede“ und „Anzeige anpassen…“
wirken nicht auf das Bild (es zeigt keine Kennzahlgruppen); der Knopf „Anzeige anpassen…“ bleibt
im Kopf, seine Einstellung bleibt für Tabelle und Grafik gültig.

### 4.3 Komponente, Hülle, DTO

- `EPOS.UI/Seiten/Berichte/UebersichtErgebnisFluss.razor` — zeichnet Versionswahl, `DiagrammSvg`
  (`Modell`, `Kennung = "ergebnis-fluss-<idProjekt>-<breit|schmal>"`, `OhneZoom="true"`,
  `LegendeSchaltbar="false"`), die verborgene Textalternative, die Hinweiszeile und die Quelle.
- `UebersichtErgebnisFlussDaten.cs` — DTO `ErgebnisFlussStand` (Version, Zeichenmodell breit und
  schmal, Flussliste `ErgebnisFlussZeile(Von, Nach, Wert, Anteil, Herkunft)`, Hinweise,
  `NichtSimuliert`); die Komponente kennt keine Kernfachklasse außer dem Zeichenmodell (wie die
  Grafik heute).
- Hülle `UebersichtErgebnisGaben`: neuer Delegat `FlussLaden: Func<int, ErgebnisFlussStand>` →
  Kern `EnergieflussCtrl.Lade(idProjekt, englisch, kultur)` → `EnergievergleichCtrl.Lade` (aus
  demselben Puffer je Simulationsstand) → `Energiefluss.Aus` → `ChartRenderer.EnergieflussModell`
  zweimal (breit, schmal). Das Modell wird je Version und Stand zwischengespeichert; die
  Komponente baut ihren Baum nur bei neuer Referenz (Hausregel).
- **Breit und schmal** (SK-Q8): Beide Modelle stehen im DOM, das Stilblatt zeigt unter 900 px
  Fensterbreite das schmale (`.epos-ergebnis-fluss--schmal`), darüber das breite. Kein JS, keine
  Breitenmessung; auf dem iPad (rund 700 px) steht damit ein für 680 px gesetztes Bild im
  Maßstab 1:1. Die Kennungen sind verschieden (Clip-Pfade).
- Stilblatt: `.epos-ergebnis-fluss`, `-kopf` (Versionswahl), `-bild`, `-hinweis`,
  `-wertetabelle` (sichtbar, nicht nur Sprachausgabe — die Tabelle ist die Textalternative und
  zugleich die Nachprüfung der Zahlen); Prüfung in `StilblattTests`.

### 4.4 Textalternative, Zeigerzeile, Hinweise

- **Keine sichtbare Wertetabelle unter dem Bild** (SK-Q12): Die Werte stehen im Reiter
  „Tabelle“. Für Bildschirmleser trägt das Bild eine **visuell verborgene Textalternative**
  (`aria-describedby`, wie bei der Grafik der Versionsbalken) mit Von · Nach · MWh/a · Anteil
  · Herkunft, auch zu gebündelten und nicht beschrifteten Flüssen, zum Solarüberschuss, zur
  Füllstandsänderung je Speicher und zur Bilanz je Knoten (Zufluss, Abfluss, Rest).
- Die **Zeigerzeile** des Bausteins zeigt die Werte beim Überfahren von Band oder Knoten
  (Abschnitt 3.3: Von → Nach, MWh/a, Anteil); auf Berührgeräten beim Tippen.
- **Hinweise** als leise Zeilen unter dem Bild: nicht geschlossene Knoten (2.4), gebündelte
  Knoten („3 Träger gebündelt“), die Konvention Umweltanteil, die Quelle („Werte aus dem letzten
  gespeicherten Simulationslauf“).

### 4.5 KI-Sicht

`UebersichtErgebnisse.Text()` beginnt mit `KI_ANSICHT_FLUSS = "Ansicht: Fluss (Energieflussbild
der Version …)"`, führt danach wie heute die Gruppenzeilen der Tabelle (die Zahlen sind in allen
Ansichten dieselben) und hängt den Block `[fluss] Energiefluss — <Version>` an: je Fluss eine
Zeile „Von → Nach: 38,4 MWh/a (71 %) · direkt|abgeleitet|bilanziert“, je nicht geschlossenem
Knoten die Hinweiszeile. `UebersichtSeiteKiSicht.ErgebnisseLesen` bleibt der Weg. Die
Versionswahl ist ein Anzeigezustand, kein Einstellwert der Seite (kein Feld in der Feldkarte).

---

## 5. Bericht (SK-4)

- `Berichtsbilder.Energiefluss(VariantenDaten v, Bildmass? mass)` in `Berichtsbilder.Ergebnis.cs`:
  `Energiefluss.Aus(v, BerichtTexte.Englisch)` → `ChartRenderer.EnergieflussModell`; `null` ohne
  Ergebnis oder ohne Fluss. Knoten tragen ihre Jahressumme in MWh/a, Bänder keine Zahlen
  (SK-Q12).
- **Vorlagenfeld** `stand.bild.energiefluss` (Kontext Stand, `Vorlagenbedarf.Keiner` — das Bild
  braucht keine Zeitreihen) und Schalter `hat.bild.energiefluss`, beide `Seit =
  FASSUNG_ENERGIEFLUSS = 25`; `Vorlagenfeldkatalog.KATALOGFASSUNG` auf **25**;
  `_bildgroessen["stand.bild.energiefluss"] = (FAKTOR_BREIT, Bildmass.MIN_BREITE)`. Messlatte
  `EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v25.txt` aus dem Testlauf kopieren,
  `VorlagenfeldkatalogWacheTests` auf 25 (die v24-Liste bleibt unverändert).
- **Bausteinweg:** `ErgebnisseBaustein.SchreibeWord` zeichnet nach der Kennzahlentafel
  `k.Bild(m, 620, 310)` mit Beschriftung „Energiefluss (Jahresbilanz Strom, Wärme, Kälte;
  Umweltanteil nach Jahresarbeitszahl)“ — vor den Ganglinien, weil es ohne Zeitreihen immer da
  ist. Der Excel-Bericht bekommt kein Diagramm (ein Sankey ist kein Excel-Diagrammtyp); der
  Prüfer meldet den Platzhalter in Excel als „nur Word“ (`Vorlagenausgabe.Word`).
- Die Standardvorlage wird nicht neu gebaut (die letzte
  Fassungsanhebung auf 24 hat sie ebenfalls nicht berührt); wer das Bild in einer eigenen
  Vorlage will, setzt den Platzhalter.

---

## 6. Tests und Proben

| Ebene | Test | Inhalt |
|---|---|---|
| Kern | `EnergieflussTests` (neu) | synthetische Ergebnisbäume: Knoten und Bänder je Anlagenart, Bilanz schließt, Toleranz, bilanzierte BHKW-Einspeisung (Plausibilitätsfenster), „nicht zugeordnet“ beidseitig, ungedeckt, Heizstab 1:1, Brennwert mit Kondensationswärme, Kälte mit Speicher und Split, Bündelung ab vier kleinen Trägern, Reihenfolge der Knoten, englische Namen |
| Kern | `EnergieflussReferenzprojektWacheTests` (neu) | rechnet 1018, 1046, 1058 und 1063 wie die übrigen Wachen (`SimulationRunner.Simuliere` auf der Testdatenbank) und hält die Bänder gegen die Zahlen aus Abschnitt 1.2 (Toleranz 0,01 MWh), jeden Knoten geschlossen, bei 1046 das Band „nicht zugeordnet“ 0,44 |
| Kern | `EnergieflussModellTests` (neu) | Zeichenmodell: Marken und Werte an jedem Band und Knoten, Mindestband 2 px, keine Beschriftung unter 8 px, schmal ohne Bandwerte, Determinismus (zweimal zeichnen byte-gleich), Rollen statt Farbwerten |
| Kern | `ChartRendererRollenTests` | neue `InlineData("Energiefluss")` |
| Kern | `VorlagenfeldkatalogWacheTests`, `VorlagenprueferTests` | Fassung 25, Platzhalter und Schalter, Excel „nur Word“ |
| Kern | `EnergieflussBerichtTests` (neu), `WordBaukastenTests` | Bild im Baustein, Bild aus Platzhalter, ohne Ergebnis kein Bild mit Grund, Excel „nur Word“ |
| UI | `UebersichtErgebnisseTests` | drei Stellungen, Tastatur über drei, Ablage „fluss“, unbekannter Wert → Tabelle, KI-Text mit Flussblock |
| UI | `UebersichtErgebnisFlussTests` (neu) | Versionswahl mit Vorgabe Stamm, Wechsel lädt neu, nicht simulierte Version benannt ohne Bild, Textalternative vollständig und verborgen, keine sichtbare Wertetabelle, Hinweiszeilen, zwei `DiagrammSvg` mit verschiedenen Kennungen, ohne Gaben nichts |
| UI | `StilblattTests` | neue Regeln, Medienregel 900 px, kein Nesting |
| Proben | `Proben/ChartProben/Program.Energiefluss.cs` (neu) | Maßproben `energiefluss`, `energiefluss_schmal`, `energiefluss_kaelte_split`; Gegenproben `energiefluss_speicher_wirkt` (mit/ohne Puffer), `energiefluss_rest_wirkt` (geschlossen/nicht zugeordnet), `energiefluss_schmal_wirkt` (breit/schmal); `svg_energiefluss` (Knotenzahl, `data-wert` an Bändern); neue Zeilen in einer neuen `Messlatte_<Datum>.sha256` (die alten Zeilen bleiben unverändert) |
| Browser | `Proben/Rasterprobe/diagrammprobe.mjs` | unverändert, weil `DiagrammSvg` nicht angefasst wird; ein neuer Fall `fluss` misst, dass die Zeigerzeile beim Überfahren eines Bandes den `data-wert` zeigt und die schmale Fassung bei 700 px ohne Querrollen steht — **vor der Abnahme von SK-3 ziehen** |
| Referenz | `EPOS.Referenzlauf` | keine Änderung am Rechenweg; die achtzehn Projekte bleiben `PASS` |

---

## 7. Wiki und Logbuch

- Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Varianten.wiki`, Abschnitt „Tabelle oder
  Grafik“ wird „Tabelle, Grafik oder Fluss“: ein Absatz zum Bild (Version wählen, Busse, Speicher
  mit Verlust, Umweltanteil als Konvention, bilanzierte Einspeisung, Textalternative, nicht
  simulierte Version). Keine Hersteller- oder Produktnamen; Beispiele neutral.
- Wiki-Quelle des Berichts (Platzhalterliste): `stand.bild.energiefluss`.
- Logbuch-Entwurf (Versionsnummer beim Anwender erfragen): „Seit <Version> zeigt der Abschnitt
  ‚Ergebnisse – Energie‘ der Übersicht ein Energieflussbild (Stellung ‚Fluss‘), und der
  Wortbericht führt es je Variante (Platzhalter `stand.bild.energiefluss`).“

---

## 8. Umsetzungsschritte

Jeder Schritt ist für einen Opus-Agenten mit höchstens rund 150 Werkzeugaufrufen geschnitten;
Bau mit `WP-Plan.Kern.slnf`, Tests mit `--filter`, das Gate fährt die Orchestrierung einmal nach
dem Merge. Kein Schemaschritt (Abschnitt 1.7).

| Schritt | Inhalt | Abnahme |
|---|---|---|
| **SK-1** Flussmodell im Kern | `Energiefluss.cs` (Knoten, Flüsse, Bilanz, Bündelung, Herkunft, Zeigertexte), `EnergieflussCtrl`, Ressourcen der Knotennamen samt Kurznamen in beiden Sprachen, `designer_neu.py` | `EnergieflussTests`, `EnergieflussReferenzprojektWacheTests` (1018, 1046, 1058, 1063) grün |
| **SK-2** Renderer | `ChartRenderer.Energiefluss.cs` (Layout, S-Kurven-Polygone, Verlustknoten, breit/schmal, Marken/Werte), fünf Farbrollen in `Farbpalette`, `Diagrammfarben` und Einstellungsdialog (Ressourcen), `Program.Energiefluss.cs` der ChartProben samt neuer Messlatte | `EnergieflussModellTests`, `ChartRendererRollenTests`, ChartProben 0 Verstöße, Messlatte alte Zeilen unverändert |
| **SK-3** Oberfläche | dritte Stellung, `UebersichtErgebnisFluss.razor` + Daten, Hülle `FlussLaden`, Stilblatt, Versionswahl, Textalternative verborgen, Hinweise, KI-Text, Wiki-Quelle „Varianten“ | `UebersichtErgebnisseTests`, `UebersichtErgebnisFlussTests`, `StilblattTests`, `KiMaskenabdeckungWacheTests`, `WikiProduktdatenWacheTests`; Windows-Schale mit `EnableWindowsTargeting=true` gebaut (keine Hülle der Schale betroffen, Nachweis trotzdem); `diagrammprobe.mjs` Fall `fluss` |
| **SK-4** Bericht | `Berichtsbilder.Energiefluss`, Vorlagenfeld und Schalter, Katalogfassung 25 mit Messlatte v25, Baustein „Ergebnisse je Variante“, Wiki-Quelle Bericht | `VorlagenfeldkatalogWacheTests`, `VorlagenprueferTests`, `WordBaukastenTests`, Wortbericht eines Referenzprojekts mit Bild (Sichtprobe) |
| **SK-5** Abschluss | Statuszeile, Protokoll unter `ueberholt/Protokolle/`, Logbuch-Entwurf, Konzept Diagramme Abschnitt 9 um die fünf Rollen ergänzt, dieses Papier nach `ueberholt/` mit Indexzeile | `DokumentationLinkWacheTests`, `RepositoryOrdnungWacheTests` |

SK-1 und SK-2 sind unabhängig und laufen parallel (der Renderer arbeitet auf dem Modelltyp aus
SK-1; der Typ wird zuerst als Vertrag festgelegt, ein Agent legt ihn an, der andere baut dagegen).
SK-3 und SK-4 folgen beiden. Vorgemerkt (nicht in dieser Welle): die drei Ergebnisspalten aus
Abschnitt 1.7 als Schemaschritt mit neuer Basis, falls der Anwender die exakte Ausweisung von
Standby und Abregelung will.

---

## 9. Berührungspunkte mit anderen Sitzungen

- **Abschnitt „Ergebnisse – Energie“:** gebaut in den Wellen vom 13./14.10.2026 (Tabelle, Grafik,
  Anzeige anpassen); keine Sitzung arbeitet dort gerade. Der Umschalter und die Ablage werden
  erweitert, nicht ersetzt.
- **Katalogfassung:** 24 ist die laufende (KB-7c2, Wasserkosten). SK-4 vergibt 25; eine Sitzung,
  die parallel ein Vorlagenfeld anlegt, stimmt die Nummer in der Statusdatei ab (dieselbe Regel
  wie bei Schemaschritten, nur ohne Anmeldezeile — der Merge zeigt den Konflikt in
  `KATALOGFASSUNG`).
- **Farbrollen:** `Farbpalette` und `Diagrammfarben` werden von DG-Wellen gepflegt; die fünf
  Rollen hängen am Ende ihrer Gruppen und ändern keine bestehende Vorgabe (die ChartProben-
  Messlatte bleibt).
- **Flotte (SP/KB-5):** Standby-Eigenverbrauch bleibt im Bild „nicht zugeordnet“, bis die
  Ergebnisspalte kommt; die Sitzung Stromspeicher wird über die Vormerkung informiert.
- **Schemaschritte:** 238 ist angemeldet (KD-4), 239 frei; diese Welle meldet keinen an.

---

## 10. Offene Entscheide

| Kennung | Frage | Empfehlung |
|---|---|---|
| **SK-Q1** | BHKW-Einspeisung, Standby und PV-Abregelung: (a) am Stromknoten bilanzieren bzw. als „nicht zugeordnet“ zeigen, kein Schemaschritt; (b) drei Ergebnisspalten (Schemaschritt 239) mit neuer Basis R58 | **(a).** Die Bilanz ist im Lauf geschlossen, der Rest exakt bis auf Rundung; (b) kostet Schemaschritt, Basis und Referenzvergleich für 0,6 % eines Projekts. (b) bleibt vorgemerkt. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q2** | Ungedeckter Bedarf: (a) Band „ungedeckt“ aus der Quellenspalte in die Senke; (b) nur Zeigertext und Hinweis | **(a) ab Mindestbandbreite, sonst (b).** Ein Senkenknoten, der kleiner ist als der Bedarf, täuscht sonst eine vollständige Deckung vor. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q3** | Solarüberschuss (nicht abgenommene Kollektorwärme): (a) nicht zeichnen, nur Zeigertext und Tabelle; (b) als Band Solarthermie → „nicht nutzbar“ | **(a).** Der Überschuss ist Potenzial, keine Energie im System; als Band verdoppelte er die Solarquelle (1049: 21 gegen 7,8 MWh) und verzerrte den Maßstab. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q4** | Brennwertkessel mit Nutzungsgrad über 100 % (Heizwert): (a) Zufluss „Kondensationswärme“ = Wärme − Brennstoff, Verlust 0; (b) Brennstoff im Bild in Brennwert (Hs) | **(a).** Der Katalog und die Trägerzeilen rechnen in Heizwert; (b) widerspräche der Tabelle darüber. Das Band ist klein und benannt. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q5** | Nicht geschlossener Knoten: (a) benanntes Band „nicht zugeordnet“ plus Hinweiszeile; (b) nur Hinweiszeile, Knoten auf Zuflusshöhe | **(a).** Ein sichtbar offener Knoten wird als Fehler gelesen; das Band sagt, wie viel fehlt. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q6** | Kälte: (a) Nutzenergie-Konvention mit Zufluss „Umwelt (Kälte)“ = Kälte − Strom, spiegelbildlich zur Wärmepumpe; (b) physikalische Sicht mit Senke „Abwärme an Umgebung“ = Kälte + Strom und Kälte als Zufluss aus dem Gebäude | **(a).** Die Senke „Kühlung“ bleibt ein Bedarf wie Heizung; (b) ließe die Kälte als Quelle erscheinen. Die Konvention steht im Zeigertext und in der Wiki-Quelle. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q7** | Verluste: (a) Verlustknoten in der Lücke neben dem Wandler bzw. Speicher; (b) Sammelsenke „Verluste“ rechts | **(a).** Keine Bänder, die die Busse kreuzen; der Verlust steht dort, wo er entsteht. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q8** | Schmale Ansicht: (a) zwei Modelle (breit 1 180 px, schmal 680 px), Stilblatt wählt unter 900 px; (b) ein Modell, Browser skaliert | **(a).** Bei (b) wäre die Schrift auf dem iPad rund 6 px; (a) braucht kein JS. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q9** | Speicher: (a) Füllstandsänderung im Verlustband, aufgeschlüsselt im Zeigertext, eigenes Band erst ab 1 % der Ladung; (b) immer eigenes Band | **(a).** Die Füllstandsänderung ist in allen Referenzprojekten unter 0,3 % der Ladung; ein eigenes Band wäre fast immer 2 px. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q10** | Bericht: (a) Bild im Baustein „Ergebnisse je Variante“ nach der Kennzahlentafel, 620 × 310 pt; (b) eigener Baustein „Energiefluss“ | **(a).** Das Bild gehört zu den Ergebnissen der Variante; ein eigener Baustein brächte nur eine weitere Kapitelwahl. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q11** | Versionswahl: (a) Versionen der Vergleichswahl (Stamm und „Im Vergleich“); (b) alle Versionen der Gruppe | **(a).** Dieselben Versionen wie Tabelle und Grafik; eine Version, die nicht im Vergleich ist, nimmt der Anwender über die Vergleichswahl hinzu. — *entschieden 16.10.2026 nach Empfehlung* |
| **SK-Q12** | Werte am Bildschirm: (a) im Bild an Bändern und Knoten bzw. als Tabelle darunter; (b) nur im Zeigertext | **(b).** Anwendervorgabe: „Werte mit Mouse-over anzeigen, nicht darunter (die Werte stehen schon im Tab Tabelle).“ Knoten tragen am Bildschirm nur den Namen; Textalternative für Bildschirmleser verborgen; Hinweiszeilen bleiben; im Word-Bericht tragen Knoten ihre Jahressumme. — *entschieden 16.10.2026, Anwendervorgabe* |

---

## 11. Fundstellen

- Abschnitt und Grafik: `EPOS.UI/Seiten/Berichte/UebersichtErgebnisse.razor` (Umschalter
  `ANSICHT_TABELLE`/`ANSICHT_GRAFIK`, `Text()`), `UebersichtErgebnisGrafik.razor`,
  `UebersichtErgebnisGrafikDaten.cs` (`UebersichtErgebnisGrafikBau`, Rollen der Versionen),
  `UebersichtErgebnisDaten.cs`, Hülle `EPOS.UI.Daten/Bericht/UebersichtErgebnisGaben.cs`
  (`EINSTELLUNG_ANSICHT`, Puffer je Simulationsstand), `UebersichtSeiteKiSicht.cs`.
- Kern: `EPOS.Kern/Controller/EnergievergleichCtrl.cs` (`Lade`: Ergebnisbaum, Brennstoffmengen,
  `KostenEmissionRechner`, `KennzahlenKatalog`), `EPOS.Kern/Allgemein/Bericht/Energievergleich.cs`
  (Gruppen, Trägerzeilen aus `EnergiekostenJeTraeger`), `EPOS.Kern/Model/ErgebnisModel.cs`,
  `ErgebnisPufferspeicherModel.cs`, `EPOS.Kern/Allgemein/Wirtschaftlichkeit/StromMatrix.cs` und
  `WirtschaftlichkeitCtrl.Persistiere` (Schreiber der Matrix), `SimulationControl.cs`
  (`BhkwEinspeisungDesLaufs`, `SpeichersystemEigenverbrauchKwh`), `SimulationErgebnisCtrl.cs`
  (`PhotovoltaikErgebnis.AbregelungMwh`).
- Zeichnen: `EPOS.Kern/Allgemein/Bericht/Zeichnung/Zeichenmodell.cs` (`Pfad`, `Zeichenbefehl.Wert`),
  `Farbpalette.cs` (Rollen, Vorgabe), `Diagrammfarben.cs` (Gruppen), `SvgSchreiber.cs`
  (`data-wert`), `ChartRenderer.Versionsbalken.cs` und `ChartRenderer.Kesselkennlinie.cs`
  (Vorbilder ohne Zeichenfläche), `Bildmass.cs`.
- Bericht: `WordBerichtGenerator.Bild(Zeichenmodell, int, int)`,
  `Vorlagen/Vorlagenfeldkatalog.Bilder.cs` (`Bild(…)`, `_bildgroessen`, `FASSUNG_*`),
  `Vorlagenfeldkatalog.cs` (`KATALOGFASSUNG`), `Bausteine/BausteineVergleich.cs`
  (`ErgebnisseBaustein`), `Berichtsbilder.cs`, `Berichtsbilder.Ergebnis.cs`.
- Proben und Wachen: `Proben/ChartProben/Program.cs` (`Pruefe`, `Unterschiedlich`, `SvgProbe`),
  `Messlatte_2026-10-14.sha256`, `EPOS.Kern.Tests/ChartRendererRollenTests.cs`,
  `VorlagenfeldkatalogWacheTests.cs`, `Messlatten/Vorlagenfeldkatalog_v24.txt`,
  `SolarWaermeMonateTests.cs` (Muster einer Wache mit Lauf), `EPOS.UI.Tests/Seiten/UebersichtErgebnisseTests.cs`,
  `UebersichtErgebnisGrafikTests.cs`, `Proben/Rasterprobe/diagrammprobe.mjs`.
- Befundzahlen: `Referenzlaeufe/2026-10-16_R57_Schnellaufheizung/Projekt_<n>/aggregate.csv`,
  Reihen `bhkw_strom.csv`, `strombedarf_viertelstunde.csv`, `reststrom_viertelstunde.csv`,
  `wp_strom.csv`, `kessel_strom.csv`; Exportschlüssel in `Referenzlauf/Ergebnisexport.cs`,
  Vergleichsregel „Eintrag nur im Vergleichslauf“ in `Referenzlauf/Vergleich.cs`.
