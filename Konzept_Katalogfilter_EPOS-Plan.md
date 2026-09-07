# Konzept: Katalogfilter EPOS-Plan — Auswahl und Suche nach den wichtigen Parametern

**Rev. 3 — 07.09.2026 — der Anwender hat entschieden (`W14a‑E‑10`): Der Filter sitzt
an der SPALTE, und nach seiner zweiten Rückmeldung vom selben Tag **im Spaltenkopf neben dem
Namen**, ohne getrennte Filterzeile, mit der **Liste über die ganze Breite**. Kapitel 5.6 ist
der gültige Vorschlag, 5.2–5.5 stehen als Geschichte daneben.**

> **Stand 07.09.2026: STUFE S1 IST UMGESETZT** (Zweig `w140-katalogfilter-s1`, Commits
> `78b0f1e`, `ce43d2a`, `f842465`) — die acht Verwaltungsdialoge tragen das Spaltenmodell,
> **Q1 = ja** (ein Feld je Zahlenspalte) und **Q2 = ja** (untereinander). Die Stufen **S2**
> (Projektdialoge und Assistent) und **S3** (Bedarf, Zeitreihen, Vergleich, Import) stehen
> aus; Kapitel 9 nennt die Commits, Kapitel 10 den Stand der offenen Punkte.

**Was Rev. 3 gegenüber Rev. 2 ändert** — die zweite Rückmeldung im Wortlaut:

> „W14a‑E‑10: Katalogfilter — Die Filter sollten über den Spaltennamen sitzen (Hersteller,
> Modell, Leistung, … — siehe Screenshot als Beispiel) und nicht separat, außer ‚Suche' über
> alle Felder. Liste wie zuvor über ganze Breite, sonst zu schmale Liste. Erstelle neues
> Mockup."

| | Rev. 2 | **Rev. 3** |
|---|---|---|
| über der Liste | Suchzeile **und** Filter-/Trefferzeile („Brennstoff enthält Gas · P_th 10..60 · [Alle löschen] 15 von 63") | **eine** Zeile: Suche links, Trefferzahl rechts. Die Filterzeile fällt (5.6.4) |
| woran man sieht, dass gefiltert wird | an der Filterzeile | **allein am gefüllten Trichter** im Spaltenkopf — gefüllt gegen Umriss, damit es ohne Farbe trägt |
| Liste und Eingabe | nebeneinander (`Katalograhmen` 60/40) | **untereinander**: Liste über die ganze Breite, Eingabe darunter (5.6.1) |
| Spaltenzahl | rund fünf | **sechs bis neun**, gemessen (5.6.5) |
| Projektauswahl (M2) | `Zweispaltenauswahl`: Projekt links, Katalog rechts | **untereinander**, Pfeile als Textknöpfe dazwischen → **Frage W14a‑E‑10‑Q2** (8.2) |
| Rücksetzer | „Alle löschen", immer da, gesperrt wenn nichts gesetzt | **„Filter zurücksetzen"**, Textknopf, **nur sichtbar wenn etwas gesetzt ist** — optional |

Auftrag (Anwenderwunsch **W14a‑E‑9**, 07.09.2026, im Wortlaut):

> „Die Dialoge unter Menü Administration → Energiesysteme, Administration → Wärmebedarf &
> Heizung, Administration → Strombedarf & Speicher sollen eine Auswahl/Suche erhalten, um die
> wichtigen Parameter der Komponenten bei der Auswahl eingrenzen zu können. Gebe einen
> Vorschlag. Evtl. auch anderes Design der Dialogbox."

**Der Entscheid dazu (`W14a‑E‑10`, 07.09.2026, im Wortlaut):**

> „Q1 bis Q12: ändere den Katalogfilter — nur Suche (Screenshot 2), Hersteller, Brennstoff etc.
> sollte an Spalte mit Sortieren und Suchen (Beispiel Screenshot 1) erfolgen. Das Schema des
> Dialogs sollte immer gleich aussehen (Wärmepumpe ähnlich wie PV-Module und Heizkessel)."
> — und: „erstelle aktualisiertes Mockup".

Damit ist die Empfehlung aus Rev. 1 (das **Zonenmodell** 5.2 mit Klapplisten, Bereichsfeldern und
Chips) **abgelehnt** und die dort verworfene Variante **A4** — Filter am Spaltenkopf — **gewählt**.
Was bleibt, steht in **Kapitel 5.6**; was fällt, steht als Geschichte in 5.2 bis 5.5. Kapitel 1
bis 4 gelten **unverändert weiter**: Der Befund ist derselbe, und die Parameter aus Kapitel 4
werden jetzt **Spalten** statt Feldern einer Leiste.

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
`—` niemand liest ihn. Fett = Vorschlag für den Filter, seit W14a‑E‑10 also für eine **Spalte**
mit Sortierung und Trichter (Kapitel 4).

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

## 3. Vorschlag: EIN Filterprofil je Katalog

> **Nach dem Entscheid W14a‑E‑10 gilt dieses Kapitel weiter — mit EINER Ersetzung.** Die
> **Daten** (`Katalogfilterprofil`) und die **Rechnung** (`Katalogfilter`) bleiben Wort für Wort,
> ebenso die Mechanik aus 3.3, die Hausregeln aus 3.4 und die Frage „Speicher oder SQL" aus 3.6.
> Was entfällt, ist das **Bild**: Statt `EPOS.UI/Bausteine/Filterleiste.razor` bekommt der
> Standard `EPOS.UI/Standards/Raster.razor` je Spalte ein `ColumnOptions`-Popover, und über der
> Liste steht nur noch **eine** Zeile — Suchfeld links, Trefferzahl rechts (**5.6.4**, Rev. 3).
> Aus „Klappliste", „Zahlenbereich" und „Merkmalsschalter" wird je **eine Spalte mit einem
> Feld**; die Chips aus 3.5 entfallen ersatzlos, ihre Aufgabe („woran sehe ich, dass gefiltert
> wird?") übernehmen der **gefüllte Trichter** im Spaltenkopf und die Trefferzahl.

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
EPOS.Kern/Allgemein/Katalog/Zahlenausdruck.cs        (neu, nach W14a-E-10 — ">10", "10..60", "=15")
EPOS.UI/Bausteine/Spaltenfilter.razor                (neu — das BILD im ColumnOptions-Popover)
```

*Die dritte Zeile hieß in Rev. 1 `EPOS.UI/Bausteine/Filterleiste.razor`; nach dem Entscheid
W14a‑E‑10 sitzt das Bild im Spaltenkopf des vorhandenen `Raster` (5.6.7).*

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

### 3.4 Die Hausregeln, die der Filter einhalten muss

> *Gilt unverändert; „die Leiste" heißt nach W14a‑E‑10 „der Spaltenkopf und die Zone A".*

| Regel | Herkunft | Folge für den Baustein |
|---|---|---|
| **Filter stehen ÜBER der Tabelle, auf die sie wirken** | W6‑O‑4; #76 („Filter gehören über die Katalogliste in die RECHTE Spalte") | die Leiste ist das erste Kind der Listenspalte, nicht ein Block darunter |
| **Die gewählte Zeile bleibt gewählt, auch wenn der Filter sie ausblendet** | `EnergietraegerDialog` (W4): „Der gewählte Träger BLEIBT GEWÄHLT … beim Leeren der Suche wieder markiert" | der Filter wirkt auf die **Anzeige**, nie auf die Auswahl |
| **Virtualisierung ab 120 Zeilen** | Raster-Fix W6‑B‑2 (`Raster.Virtualisiert`) | der Wirt schaltet an der **gefilterten** Zeilenzahl; das `@key` an (Virtualisiert, Zeilenzahl) bleibt |
| **Klicksemantik W6‑E‑5** | `Zeilenmarkierung` seit 07.09.2026 | einfacher Klick schaltet um, `Strg` ebenso, `Umschalt` nimmt den Bereich, Doppelklick übernimmt. Nach einem Filterwechsel wird die Markierung über `Hinzufuegen` wiederhergestellt |
| **44 px Berührungsziele, keine Hover-Abhängigkeit** | iL4, `--epos-touchziel` | jedes Filterfeld, jeder Chip und jedes „×" ist 44 px hoch; das „×" ist ein eigenes Ziel, kein `:hover`-Symbol |
| **Eine Liste steht in einem festen Rahmen mit Rollbalken** — außer im `Katalograhmen` | W9‑B‑2 / `--epos-listenhoehe` | *Rev. 3: die Ausnahme fällt.* Steht die Eingabe **unter** der Liste, braucht auch der `Katalograhmen` eine Höhengrenze — sonst schiebt eine lange Liste den Eingabeblock beliebig weit nach unten. Überall gilt dieselbe Höhe: 1,3 × `--epos-listenhoehe` = elf Zeilen (5.6.5); die kurze Projektliste bekommt vier |
| **Ein Parameterblock steht im `Formularraster`** | iU8‑E‑2 / W14a‑E‑7 | der Detailbereich bleibt, wie er seit W14a ist |
| **Kein Delegat, kein Bedienelement** | `ModulKatalogDialog.Filterbar` | ein Profil ohne Klapplisten zeichnet keine; eine Hülle ohne Suchdelegat bekommt kein Suchfeld |
| **Farben nur als Token in `:root`** | W16b‑E‑5 | die Leiste braucht **keine neue Farbe**: `--epos-flaeche`, `--epos-rahmen-leise`, `--epos-text-leise` und die vorhandene Klasse `.epos-chip` genügen |

### 3.5 Chips, Trefferzähler, Zurücksetzen, Sortierung, Gedächtnis

> *Nach W14a‑E‑10 entfallen die **Chips**; Rev. 3 lässt auch die Filter-/Trefferzeile fallen, die in Rev. 2 an ihre Stelle getreten war. Was gefiltert ist, sagt der **gefüllte Trichter** im Spaltenkopf; der Knopf **„Zurücksetzen"** kehrt als Textknopf **„Filter zurücksetzen"** in die Suchzeile zurück, aber nur, solange etwas gesetzt ist (5.6.4). **Trefferzähler**, **„Kein Treffer."**, **Sortierung** und **Gedächtnis** gelten wörtlich weiter.*

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

## 4. Die konkreten Filterkriterien je Katalog

> **Nach dem Entscheid W14a‑E‑10 liest sich diese Tabelle anders, aber sie gilt.** Aus
> „Klappliste" wird eine **Textspalte** mit dem Feld „enthält…", aus „Bereich" eine
> **Zahlenspalte** mit einem Feld (`10..60`), aus „Schalter" eine Spalte mit „ja"/„nein" und
> „enthält ja", aus „Suche" das **eine** Suchfeld über alle Spalten der Zone A. Die Zeile
> **Spalten** ist damit die eigentliche Aussage jedes Abschnitts; die Zeile **A** („im
> Aufklapper Weitere Filter") entfällt — was nicht als Spalte dasteht, ist auch nicht
> filterbar (5.6). Neben dem Eingabeblock haben rund **fünf** Parameterspalten Platz; die
> übrigen Parameter stehen im Detailblock und in der Parameterübersicht.

Der frühere Aufbau, auf den sich die Zeilen beziehen: **Zeile 1** Klapplisten + Suche ·
**Zeile 2** Zahlenbereiche + „Weitere Filter ▾" + „Zurücksetzen" · **Zeile 3** Chips +
Trefferzähler. „A" = im Aufklapper „Weitere Filter", nicht in der Grundzeile.

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

**Was fehlt, sind die Spalten.** Zwischen der Überschrift und der Liste ist heute nichts —
oder zwei Klapplisten, die aussehen wie Eingabefelder und nicht wie ein Filter. Und die Liste
selbst ist eine Namensspalte: Sie **zeigt** die Parameter nicht, nach denen der Anwender sucht,
also kann er weder nach ihnen sortieren noch — nach dem Entscheid W14a‑E‑10 — an ihnen filtern.
Der Befund dieses Abschnitts ist damit der eigentliche Grund, warum das Spaltenmodell mehr
Arbeit an den **Controllern** als an der Oberfläche bedeutet (S1.5).

Drei Sätze zum Bestand, damit die Bewertung nicht besser klingt, als sie ist:

1. Die zwei Klapplisten des Heizkessels heißen „Filtern nach Brennstoff:" und „Filtern nach
   Leistung:" und stehen ohne Rahmen, ohne Trefferzahl und ohne Rücksetzknopf über der Liste.
   Ob gerade gefiltert wird, sieht man nur, wenn man beide liest.
2. Die Leistungsstufen sind **fest**: „< 50", „50…200", „200…500", „500…1000", „≥ 1000". Wer
   einen 30‑kW-Kessel sucht, bekommt jede Zeile unter 50 kW.
3. Der einzige vollständige Filter des Hauses (Wärmepumpe) hat **elf** Bedienelemente
   nebeneinander im `epos-zahlenraster` — er ist so breit wie die Maske und braucht selbst eine
   Ordnung.

### 5.2 Der Vorschlag von Rev. 1: drei Zonen mit einer Filterleiste

> *Nicht gewählt, Entscheid **W14a‑E‑10**. Der Abschnitt bleibt als Geschichte stehen; der gültige Vorschlag steht in **5.6**.*

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

> *Zur Filterleiste aus 5.2 gehörig und damit nicht gewählt (**W14a‑E‑10**). Von der Tabelle
> bleiben die **Listenspalten** und die **Zone C** richtig; **Zone B** und die **Listenhöhe**
> sind mit Rev. 3 überholt — Liste über die ganze Breite, Eingabe darunter, Höhe 1,3 ×
> `--epos-listenhoehe` (5.6.1 und 5.6.5), und die zwei Listen der Projektauswahl stehen
> untereinander statt nebeneinander (Frage Q2, 8.2).*

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

### 5.4 Alternativen — und was der Anwender daraus gewählt hat

> *Die Bewertungen sind die von Rev. 1. Der Anwender hat **A4** gewählt; die drei Einwände dagegen sind unten entkräftet, nicht wiederholt.*

| | Alternative | Bewertung |
|---|---|---|
| **A1** | **Kartenansicht statt Tabelle** — je Gerät eine `Kachel` mit Name, Hersteller und drei Kennwerten, im `Kachelraster` | **Nein.** Eine Karte braucht das Drei- bis Vierfache der Höhe einer Tabellenzeile; bei 20 743 Modulen sind das rund 60 Bildschirmseiten statt 15. Vor allem aber: Karten **können nicht sortieren** und stellen die Werte nicht untereinander — genau das ist beim Vergleichen die Arbeit. Karten sind im Haus der EINSTIEG (Startseite, Komponentenauswahl), nicht die Auswahl aus einem Katalog |
| **A2** | **Facettenleiste links** (wie im Webshop): eine schmale Spalte mit allen Filtern, Liste rechts | **Nein.** Sie nimmt 240–280 px genau dort, wo die `Zweispaltenauswahl` schon eine linke Spalte hat (die Projektliste) — der Dialog hätte dann drei Spalten plus Pfeile. Und sie wäre eine **zweite Anordnungssprache** neben `Katalograhmen`; #76 hat das Haus gerade auf eine gebracht |
| **A3** | **Filterzeile als Aufklapper** — ein Knopf „Filter ▾", der die ganze Leiste zeigt oder verbirgt | **Teilweise ja** — als Staffelung. Die **erste** Zeile (Klapplisten + Suche) steht immer offen, die **zweite** (Zahlenbereiche) und der Rest hängen unter „Weitere Filter ▾". Alles zu verbergen wäre falsch: Ein Filter, den man nicht sieht, erklärt eine kurze Liste nicht — und die **Chips bleiben immer sichtbar**, gerade weil die Felder es nicht sind |
| **A4** | **Filtersymbol je Spaltenkopf** (Tabellenkalkulations-Art) | **GEWÄHLT — Anwenderentscheid W14a‑E‑10.** Rev. 1 hatte hier „Nein" stehen, mit drei Einwänden. Alle drei sind hinfällig, und zwar nachprüfbar: **(1) kein Hover.** Der Trichter ist ein `<button>` im Kopf, erreichbar mit Maus **und** Tabulator; Enter öffnet, Esc schließt (`QuickGrid.razor.js`, `keyDownHandler`). **(2) Der Zustand ist sichtbar** — an jeder gefilterten Spalte steht ein **gefüllter** Trichter (gefüllt gegen Umriss, es trägt auch ohne Farbe), und weil die Liste seit Rev. 3 über die ganze Breite läuft, stehen alle Spaltenköpfe gleichzeitig da. **(3) Der Trefferzähler hat einen Ort** — die Suchzeile, rechts außen. **(4) QuickGrid kann es doch:** `ColumnBase<TGridItem>.ColumnOptions` ist ein `RenderFragment`, das die Zelle als Popover unter dem Kopf zeigt, geöffnet über einen Knopf, den QuickGrid selbst in den Kopf setzt — Fundstellen in 5.6 |
| **A5** | **Nur mehr Spalten, kein Filter** — die Liste zeigt die Parameter, Sortieren genügt | **Nein**, aber die Hälfte davon ist Teil des Vorschlags: Die Spalten kommen ohnehin. Für 63 Heizkessel würde Sortieren reichen; für 20 743 Module nicht |
| **A6** | **Alles bleibt, nur die zehn fehlenden Klapplisten werden ergänzt** | **Nein.** Das wären zehn weitere handgeschriebene Filterzeilen — genau der Zustand aus Befund 1.4, nur zehnmal schlimmer |

*Empfehlung von Rev. 1 war das Zonenmodell aus 5.2 mit der Staffelung aus A3.* **Der
Anwender hat anders entschieden (W14a‑E‑10): A4.** Was beide Wege teilen, gilt weiter — an den
drei Bausteinen, die das Haus gerade eingeführt hat (`Katalograhmen`, `Formularraster`,
`Zweispaltenauswahl`), ändert sich **nichts**. Der Unterschied ist, wo der Filter steht: A4
setzt keinen vierten Baustein darüber, sondern erweitert den Spaltenkopf des vorhandenen
`Raster`.

### 5.5 Vergleich von zwei bis drei markierten Zeilen

> *Bleibt gültig — der Vergleich hängt an der Mehrfachwahl, nicht am Filterbild (Q3, Stufe S3).*

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

**Stand nach S3.3 (07.09.2026): umgesetzt, mit drei Festlegungen, die der Text offen ließ.**

1. **Markiert wird mit `Strg` oder `Umschalt` auf dem vorhandenen Wahlknopf** — nicht mit einer
   zweiten Spalte voller Kästchen. `Zeilenwahl` meldet die Zusatztasten seit W6‑E‑5 über
   `Tastenwahl`, und der Rückruf läuft **vor** `Gewaehltwerden`; damit sind „wählen" (der
   Detailblock des Wirtes) und „markieren" (der Vergleich) auf einem Klickziel zu trennen. Eine
   eigene Spalte hätte alle fünfzehn Wirte um eine Spalte verbreitert — für eine Wahl, die man
   selten trifft. `Umschalt` tut dabei **dasselbe wie `Strg`** und wählt keinen Bereich: Bei
   höchstens drei Geräten wäre ein Bereich in aller Regel schon zu groß.
2. **Die VIERTE Markierung fällt, nicht die älteste.** Wer drei Geräte nebeneinandergelegt hat,
   hat sie ausgesucht; ihm still das erste wegzunehmen wäre die überraschendere Antwort. Statt
   dessen steht ein Hinweis da (`KFLT_VERGLEICH_GRENZE`), und die drei bleiben.
3. **Die Zeilen kommen aus der Parameterübersicht** (`ParameterUebersichtCtrl.Werte`, W14a‑E‑8) —
   ALLE Parameter des Katalogs, nicht nur die Listenspalten. Verdrahtet ist der Weg in den drei
   Verwaltungswirten (dort gab es den Delegaten schon), in den sieben Projektdialogen und im
   Wärmepumpenkatalog samt seinen beiden Wirten. Für die **Bedarfskataloge** liefert
   `BedarfStammCtrl.Vergleichszeilen` Typ, Beschreibung, Jahressumme und die **zwölf
   Monatswerte** — der Monatsgang ist der Grund, warum zwei Profile mit derselben Jahressumme
   verschieden rechnen. Für die drei **Zeitreihenkataloge** stehen die Profilspalten
   (Bezeichner, Jahresarbeit, Spitze); eine Parameterübersicht haben sie nicht, sie sind keine
   Anlagen.

Und eine Auflage, die der Text nur zwischen den Zeilen trug: Die Kennzeichnung trägt **Worte**
(„abweichend"/„stimmig") und nicht nur Farbe — dieselbe Regel wie beim gefüllten Trichter.

---

### 5.6 Das Spaltenmodell (Anwenderentscheid W14a‑E‑10)

**Der Entscheid in einem Satz:** *Über* der Liste steht nur noch die **Suche**; alles, wonach
gefiltert wird, sitzt **im Spaltenkopf neben dem Namen** — mit Sortierung und einem Feld
„enthält…". Die **Liste läuft über die ganze Breite**, der Eingabe- bzw. Detailblock steht
darunter. Und: **jeder Katalogdialog sieht gleich aus** — die Wärmepumpe wie der Heizkessel
wie die PV-Module.

#### 5.6.1 Das eine Schema — drei Zonen, untereinander

```
+------------------------------------------------------------------------------+
|  Verwaltung Heizkessel                                                    [?] |  Titel + InfoKnopf
|  ZONE A                                                                       |
|  Suche ueber alle Spalten [                    ]   15 von 63 Saetzen          |  EIN Feld, * und ?
|                                                    [Filter zuruecksetzen]     |  nur wenn gesetzt
+------------------------------------------------------------------------------+
| ZONE B oben - die LISTE ueber die GANZE Breite                                |
| +---+---------------+------------------+-----------+-------+------+---------+ |
| |Wa | Bezeichner  V | Hersteller     V | Brennst V | P_th V| eta V|Brennwert| |
| +---+---------------+------------------+-----------+-------+------+---------+ |
| | o | ecoTEC ...    | Vaillant ...     | Erdgas E  |  16,6 |0,970 | nein    | |
| | * | ecoCOMPACT    | Vaillant ...     | Erdgas E  |  15,0 |0,980 | nein    | |
| +---+---------------+------------------+-----------+-------+------+---------+ |
|      ^ Titel+Sortierpfeil       ^ Trichter          ohne Trichter: nur ^^     |
+------------------------------------------------------------------------------+
| ZONE B unten - EINGABE / DETAIL, ebenfalls ueber die ganze Breite             |
|  -- Eingabe der Heizkesseldaten ------------------------------------------    |
|  Name:       [ecoTEC plus VC 15CS/1-5]  | Hersteller: [Vaillant Deutschl...]  |
|  Brennstoff: [Erdgas E              ]   | Beschreibung: [Brennwert-Kessel  ]  |
|  > Alle Parameter und ihre Verwendung                                         |
+------------------------------------------------------------------------------+
| ZONE C  [Speichern]     [Neu...] [Kopieren...] [Vergleichen] [Loeschen] [OK]  |
+------------------------------------------------------------------------------+
```

| Zone | Inhalt | in der **Verwaltung** | in den **Projektdialogen** |
|---|---|---|---|
| **A** | Titel + `InfoKnopf`; **eine** Zeile: links **ein** Suchfeld über alle Spalten (`*` und `?` wie in den Importmasken), rechts die **Trefferzahl** und — nur wenn ein Filter gesetzt ist — der Textknopf „Filter zurücksetzen" | über die ganze Breite | über die ganze Breite und **unmittelbar über der Katalogliste**, auf die sie wirkt (#76 wörtlich, siehe unten) |
| **B** | Liste **mit Parameterspalten**, darunter Detail | `Katalograhmen`, **untereinander**: Liste über die ganze Breite, **Eingabe** darunter (schreibend, `Formularraster`) | **Projektliste** oben (kurz, höchstens vier Zeilen), Übernahmeleiste, dann dieselbe Katalogliste über die ganze Breite, dann der Detailblock (lesend) → **Frage Q2** |
| **C** | Aktionsleiste | Neu…, Kopieren…, Löschen, Vergleichen, Speichern, OK | ▲ ins Projekt übernehmen / ▼ aus dem Projekt entfernen, Bearbeiten…, Löschen, Verwaltung…, OK |

**Warum untereinander und nicht nebeneinander.** Der Anwender hat es begründet: *„Liste wie
zuvor über ganze Breite, sonst zu schmale Liste."* Gemessen im Mockup (Chromium, 1 366 px):
neben dem Eingabeblock hatten **fünf** Parameterspalten Platz, über die ganze Breite sind es
**sechs bis neun** — ohne dass die Liste in sich waagerecht rollt. Zugleich zeigt der
Hersteller seinen vollen Namen („Vaillant Deutschland GmbH & Co. KG"), der vorher nach 9 rem
abbrach. Der Preis steht in 5.6.5.

Gegenüber 5.2 fallen damit **vier** Dinge weg: die Klapplisten, die Bereichsfeldpaare, die
Chips — und mit Rev. 3 die **Filterzeile** selbst. Es kommt **eines** dazu: der Spaltenkopf.
`Formularraster`, Parameterübersicht und Aktionsleiste bleiben unangetastet; der
`Katalograhmen` behält seine Bausteine, aber **nicht** seine Anordnung, und die
`Zweispaltenauswahl` steht ganz zur Frage (Q2).

#### 5.6.2 Der Spaltenkopf

Vorbild ist der Tabellenkopf aus der Anwendung, die der Anwender als Screenshot 1 gezeigt hat:
`NR ▼ | STATUS ⇅ ▼ | KUNDE ⇅ ▼ | … | PRIORITÄT ⇅ | BESCHREIBUNG ▼`. Er trägt **alles in einer
Zeile**, direkt hinter dem Namen — und **nicht jede Spalte hat beides**: manche nur Sortierung,
manche nur den Trichter. Je Spalte **bis zu zwei** Knöpfe:

| Teil | Zeichen | Verhalten |
|---|---|---|
| **Titel + Sortierpfeil** | `⇅` unsortiert, `▲` auf, `▼` ab | Ein `<button>`. Erster Klick sortiert auf, zweiter ab, **dritter hebt die Sortierung auf** und stellt die Reihenfolge des Controllers wieder her. Es ist immer **höchstens eine** Spalte sortiert |
| **Trichter** | **Umriss** = kein Filter, **gefüllt** = gefiltert | Ein zweiter `<button>`, 44 px hoch wie die ganze Kopfzelle. Er öffnet das Popover; Esc, ein Klick daneben und ein Klick auf einen anderen Trichter schließen es |

**Der Unterschied gefüllt/nicht gefüllt muss ohne Farbe tragen** (Auflage des Anwenders zu
Rev. 3). Deshalb ist er kein Farbwechsel, sondern ein **Formwechsel**: nicht gesetzt ist der
Trichter ein dünner Umriss (`fill: none`, Strichstärke 1,3 in `--epos-text-sehr-leise`),
gesetzt eine **volle Fläche** (`fill: currentColor`, Strichstärke 2 in `--epos-marke`). In
Graustufen bleibt ein voller Trichter ein voller Trichter; die Farbe kommt nur obendrauf. Es
kommt **keine neue Farbe** dazu (W16b‑E‑5).

**Spalten ohne sinnvollen Filter tragen NUR den Sortierpfeil** — genau wie `PRIORITÄT ⇅` im
Vorbild. Das sind die Wahlspalte (weder Sortierung noch Trichter) und die **Kennzeichen
ja/nein**: `Brennwert` beim Heizkessel, `Kühlen` bei der Wärmepumpe, „im Projekt verwendet"
(Q12). Begründung: Ein Feld „enthält ja" für zwei Werte ist ein Bedienelement ohne Gewinn —
die Sortierung stellt die sechs bzw. fünfzehn Sätze ohnehin zusammen. Umgesetzt wird das über
`ColumnBase.ColumnOptions`: **wird es nicht gesetzt, zeichnet QuickGrid keinen Optionsknopf** —
es braucht also kein Ausschalten, nur ein Weglassen.

Das **Popover** trägt genau drei Dinge: den Spaltennamen als Überschrift, **ein** Eingabefeld
und den Knopf **„Filter löschen"**. Nichts sonst — kein „Übernehmen", kein „Alle auswählen",
keine Werteliste. Der Filter wirkt beim Tippen (dieselbe Sofortwirkung wie die Suche im
`EnergietraegerDialog`).

**Eine gefilterte Spalte ist auch in den Zellen erkennbar.** `ColumnBase.Class` setzt die Klasse
auf Kopf **und** Körperzellen; eine gefilterte Spalte bekommt damit eine leise Tönung
(`--epos-karte-flaeche-hover`), keine neue Farbe.

#### 5.6.3 Was ein Spaltenfilter versteht

| Spaltenart | Feld | Regel |
|---|---|---|
| **Text** (Bezeichner, Hersteller, Brennstoff, Technologie, Quelle, Bauart …) | Platzhalter **„enthält…"** | Teilzeichenkette, **Groß/Klein egal — auch bei Umlauten** („öl" findet „Öl"). Der Vergleich ist `StringComparison.CurrentCultureIgnoreCase`, genau wie `VdiAuswahlFilter.Passt:70`. Kein Platzhalter nötig; `*` und `?` sind trotzdem erlaubt und gehen durch `Suchmuster.Uebersetzen`. **Nicht** umgeschrieben wird die Ersatzschreibweise: „Oel" findet „Öl" nicht — dafür gibt es im Haus keine Regel, und eine neue wäre eine eigene Entscheidung |
| **Zahl** (P_th, η, P_STC, Volumen, Energie …) | dasselbe **eine** Feld | Es versteht `>10`, `>=10`, `<60`, `<=60`, `=15`, `10..60` und die bloße Zahl `15` (= `=15`). Dezimaltrennzeichen ist das der Kultur. Ein Ausdruck, den es nicht versteht, ist **kein** Filter — dieselbe Regel wie beim kaputten Suchmuster (3.3) → **Frage W14a‑E‑10‑Q1** |
| **Ja/Nein** (Brennwert, „nur eigene Sätze") | dasselbe Feld | Die Spalte zeigt „ja"/„nein"; „enthält ja" ist der Filter. Kein eigener Schalter mehr |

**Der Filter arbeitet auf dem ANGEZEIGTEN Wert, nicht auf der Datenbankspalte.** Das ist keine
Feinheit, sondern der Grund, warum das Modell überhaupt trägt: `Tab_Heizkessel_STAMM.Brennstoff`
ist eine **Zahl**, in der Spalte steht der Name aus `Tab_Brennstoff_Stamm`. „enthält Gas"
trifft dort Stadtgas, Erdgas LL, Erdgas E, Flüssiggas (Propan/Butan) und Biogas; im
Heizkesselkatalog sind davon **gemessen 52 von 63 Sätzen** belegt — genau die Menge, die heute
die Klappliste „Brennstoffgruppe" liefert (die zwei Wege kommen auf dieselbe Zahl, weil
`HeizkesselStammCtrl` genau diese Kennungen als Gasgruppe führt). Dasselbe gilt für
abgeleitete Größen (σ, C‑Rate, Modulfläche, COP A2/W35, VL max): Sie werden beim Aufbau der
Zeilenliste einmal gerechnet (3.2) und sind danach ganz normale Spalten — sortierbar **und**
filterbar.

**Verknüpfung.** Die Spaltenfilter wirken **UND** (jede gesetzte Spalte muss passen). Das
Suchfeld der Zone A wirkt **ODER über alle Spalten** (ein Treffer in irgendeiner Spalte genügt)
und **UND über mehrere Begriffe** — die Regel von `VdiAuswahlFilter.Passt`. Suche und
Spaltenfilter gelten gleichzeitig; die gemeinsame Trefferzahl steht in der Suchzeile.

#### 5.6.4 Die Suchzeile — und warum die Filterzeile fällt

Rev. 2 hatte über der Liste **zwei** Zeilen: die Suche und darunter eine Filterzeile, die jeden
gesetzten Filter im Klartext aufzählte („Brennstoff enthält ‚Gas' · P_th 10..60 · η ≥ 0,95
[Alle löschen] 15 von 63 Sätzen"). Der Anwender hat sie abgelehnt: *„nicht separat, außer
‚Suche' über alle Felder."* **Sie fällt ersatzlos.** Übrig bleibt **eine** Zeile:

```
Suche ueber alle Spalten [                              ]        15 von 63 Saetzen   [Filter zuruecksetzen]
```

| Stück | Regel |
|---|---|
| **Suchfeld** | links, **ein** Feld über alle Spalten; `*` und `?` erlaubt, mehrere Begriffe wirken als UND (`Suchmuster`, `VdiAuswahlFilter`) |
| **Trefferzahl** | rechtsbündig, **immer** da: „15 von 63 Sätzen", ohne Filter „63 von 63 Sätzen". Sie ist die Zeile aus W7‑A‑7, jetzt überall — und die Entsprechung zu „126 Projekt(e)" im Vorbild des Anwenders |
| **„Filter zurücksetzen"** | rechts neben der Zahl, **Textknopf** (kein Kasten — er soll nicht wie ein Filter aussehen), 44 px hoch (iL4). Er erscheint **nur, wenn mindestens ein Spaltenfilter gesetzt ist**, und verschwindet danach wieder. Er ist kein Filter, sondern ein Rücksetzer. **Optional** — siehe unten |
| **„Kein Treffer."** | steht statt einer leeren Liste (`ETV_SUCHE_LEER`, wie im `EnergietraegerDialog`) |

**Der Rücksetzer ist als optional gekennzeichnet.** Ohne ihn muss man jede gefilterte Spalte
einzeln über ihren Trichter zurücknehmen — bei drei Filtern drei Popover. Das ist zumutbar,
aber lästig; der wörtliche Entscheid verlangt ihn nicht. Vorschlag: **einbauen**, weil er nichts
verdeckt (er ist nur da, wenn es etwas zurückzusetzen gibt) und weil er die einzige Stelle ist,
an der man ohne Suchen im Spaltenkopf wieder auf null kommt. Kosten: ein Ressourcenschlüssel je
Sprache und drei Zeilen im Wirt. Wenn er nicht gewollt ist, entfällt er ohne Folgen für den Rest.

**Was mit der Filterzeile verloren geht — und was an ihre Stelle tritt.** Die Filterzeile war in
Rev. 2 die Antwort auf den Einwand gegen die Variante A4: *Ein Filter, dessen Spalte gerade aus
dem sichtbaren Ausschnitt gerollt ist, ist unsichtbar.* Dieser Einwand ist mit Rev. 3 **kleiner,
aber nicht ganz weg**:

- Er ist kleiner, weil die Liste jetzt die **ganze Breite** hat und bei 1 366 px in keinem der
  drei Reiter waagerecht rollt (gemessen, Kapitel 6). Alle Spaltenköpfe stehen gleichzeitig da,
  also auch alle Trichter.
- Er ist nicht ganz weg, weil ein Katalog mit mehr Spalten oder ein schmaleres Fenster die Liste
  wieder rollen lässt. Für diesen Fall gilt: Die **Trefferzahl** verrät jederzeit, dass gefiltert
  wird („15 von 63" ≠ „63 von 63"), und der **Rücksetzer** steht daneben. Was man verliert, ist
  nur die Auskunft, *welche* Spalte filtert und *womit* — dafür rollt man die Liste.

#### 5.6.5 Wie viele Spalten — und welche Liste bekommt Filter

**Über die ganze Breite haben sechs bis neun Parameterspalten Platz**, ohne dass die Liste bei
1 366 px waagerecht rollt. Gemessen im Mockup (Chromium, Dialogbreite 1 303 px bei 1 366 px
Fensterbreite):

| Reiter | Parameterspalten | vorher (neben dem Eingabeblock) |
|---|---|---|
| **M1 Heizkessel** | **6** — Bezeichner · Hersteller · Brennstoff · P_th · η · Brennwert | 5 |
| **M2 Wärmepumpe** | **9** — Hersteller · Modell · Quelle · P_N · VL min · VL max · Zuheizung · Kühlen · COP A2/W35 | 6 (und die Liste rollte um 118 px in sich) |
| **M3 PV-Modul** | **7** — Bezeichner · Hersteller · P_STC · η · Technologie · A_Modul · T_NOCT | 5 |

Damit stehen die **Spalten**-Zeilen aus Kapitel 4 vollständig da — mit **zwei gemessenen
Ausnahmen**, und beide sind Datenbefunde, keine Layoutfragen:

| weggelassen | warum |
|---|---|
| Heizkessel: `Vorlauf`, `Rücklauf`, `Investitionskosten` | **0 von 63**, **0 von 63** und **4 von 63** Sätzen sind gepflegt. Eine Spalte, die fast immer leer ist, kostet Breite und trägt nichts |
| Wärmepumpe: `Bauart` | **45 von 51** leer (5 Split, 1 Monoblock) — derselbe Grund |

Was darüber hinausgeht, steht im **Detailblock** und in der **Parameterübersicht** (W14a‑E‑8).
Die Grenze des Modells bleibt: **Sortieren und Filtern kann man nur nach dem, was als Spalte
dasteht** — die Wahl der Spalten aus Kapitel 4 ist eine fachliche Entscheidung, keine
Layoutfrage. Neu ist nur, dass die Grenze weiter außen liegt.

**Die Höhe ist der Preis der Breite.** Steht die Eingabe unter statt neben der Liste, wird der
Dialog höher: gemessen 1 152 px (M1), 1 228 px (M2) und 1 225 px (M3) bei 1 366 px Fensterbreite.
Die Liste ist deshalb **höhenbegrenzt und rollt in sich** (Hausregel W9‑B‑2): 1,3 × 
`--epos-listenhoehe` = 458 px, das sind bei 45 px Kopfzelle und 37 px Zeilenhöhe **elf Zeilen**.
Ohne diese Grenze schöbe eine lange Liste den Eingabeblock beliebig weit nach unten. Die
kurze **Projektliste** in M2 bekommt 12 rem = **vier Zeilen**.

**Nicht jede Liste bekommt Trichter.** Die **Projektliste** führt Zeilen im einstelligen
Bereich; sie bekommt sortierbare Spaltenköpfe, aber **keine** Spaltenfilter. Die Hausregel dazu
steht schon da: *kein Delegat, kein Bedienelement* (`ModulKatalogDialog.Filterbar`) — kein
Filterprofil, kein Trichter.

#### 5.6.6 Wo gefiltert wird: **vor** dem Raster, nicht im Raster

Das ist der Punkt, an dem eine Umsetzung schiefgehen kann, deshalb steht er hier ausdrücklich.

`Raster.razor` ist eine dünne Hülle um QuickGrid und reicht `Zeilen` als `IQueryable<TZeile>`
weiter. **QuickGrid filtert nicht.** `ColumnOptions` ist nur ein Ort im Kopf, an dem ein
Bedienelement stehen darf; was es mit der Eingabe macht, entscheidet der Wirt. Der Weg ist
deshalb derselbe wie heute beim Wärmepumpenkatalog:

1. Das Popover meldet dem Wirt den neuen Ausdruck (`WertChanged`).
2. Der Wirt legt ihn in den `Katalogfilterstand` und ruft
   `…StammCtrl.Katalogzeilen(kriterien)` — der Controller entscheidet Speicher oder `WHERE`
   (3.6, Frage Q6).
3. Der Wirt reicht die **bereits eingeschränkte** Menge als `Zeilen` an `Raster`.

Für die 20 749 PV-Module heißt das: Das Raster bekommt nach dem Filtern 15 Zeilen und
virtualisiert sie gar nicht mehr. **Genau dabei greift der Fix W6‑B‑2**: Der Wirt schaltet
`Virtualisiert` an der Zeilenzahl (≥ 120), und ein Spaltenfilter führt regelmäßig über diese
Schwelle hinweg (20 749 → 15). Das `@key` an `(Virtualisiert, Zeilenzahl)` baut das QuickGrid
dann neu auf, statt den alten Zwischenspeicher zu zeigen — der belegte Fehler aus dem
Geräteimport („der Filter bei der Herstellerauswahl funktioniert nicht"). Der Spaltenfilter
darf deshalb **nicht** über `RefreshDataAsync()` gehen; die Begründung steht im Kopf von
`Raster.razor`.

Nebenwirkung, die man kennen muss: Ein neues QuickGrid heißt auch ein **geschlossenes**
Popover. Der Filterzustand lebt deshalb im Wirt (`Katalogfilterstand`), nicht im Popover —
sonst wäre er nach dem ersten Tastendruck weg.

#### 5.6.7 Was QuickGrid dafür mitbringt (Fundstellen)

Paket `Microsoft.AspNetCore.Components.QuickGrid`, **Version 10.0.11** (`Directory.Packages.props`
Zeile 79). Belege aus dem Paket selbst, damit der Umsetzungsagent nicht rät:

| Was | Fundstelle |
|---|---|
| **`ColumnOptions`** — „If specified, indicates that this column has this associated options UI. **A button to display this UI will be included in the header cell by default.**" | `lib/net10.0/Microsoft.AspNetCore.Components.QuickGrid.xml`, Eintrag ``P:…ColumnBase`1.ColumnOptions`` |
| **`Sortable`**, **`SortBy`**, **`IsDefaultSortColumn`**, **`InitialSortDirection`** | dieselbe Datei, ``P:…ColumnBase`1.Sortable`` ff. |
| **`ShowColumnOptionsAsync(column)` / `HideColumnOptionsAsync()`** — nötig, wenn man den Kopf über `HeaderTemplate` selbst zeichnet | ``M:…QuickGrid`1.ShowColumnOptionsAsync``, ``M:…QuickGrid`1.HideColumnOptionsAsync`` |
| **`Class`** setzt die Klasse „in the class attribute of table **header and body cells** for this column" — damit die gefilterte Spalte auch in den Zellen erkennbar wird | ``P:…ColumnBase`1.Class`` |
| Der Optionsknopf ist ein **`<button>`**, kein Hover-Menü: `th .col-options-button { border: none; padding: 0; width: 1rem; … }`, und der Kommentar daneben lautet „Deep to make it easy for people adding a col-options-button element in a custom HeaderTemplate" | `staticwebassets/Microsoft.AspNetCore.Components.QuickGrid.boiwgh0w5b.bundle.scp.css` |
| **Esc schließt**, ein Klick daneben schließt, und `checkColumnOptionsPosition` **rückt das Popover in das Raster hinein**, wenn es links oder rechts überhängt | `staticwebassets/QuickGrid.razor.js` (`keyDownHandler`, `bodyClickHandler`, `checkColumnOptionsPosition`) |

Zwei Dinge muss das Haus selbst beisteuern:

1. **Das Bild des Knopfs.** QuickGrid zeichnet vorgabemäßig drei waagerechte Striche
   (Hamburger), kein Trichter, und `.col-options` malt sich `background: white; border: 1px solid
   silver` fest ein. Beides wird im Stilblatt überschrieben — mit vorhandenen Token, **ohne neue
   Farbe** (W16b‑E‑5). Der gefüllte Trichter der gefilterten Spalte ist dabei nur ein `fill`.
2. **Den Ausdruck.** `>10`, `10..60`, `=15` liest niemand für uns; das ist eine kleine Klasse im
   Kern (`Zahlenausdruck`), prüfbar ohne Oberfläche — der natürliche Ort für die Kernproben aus
   S1.1.

#### 5.6.8 Was aus den Hausregeln unverändert gilt

| Regel | Herkunft | im Spaltenmodell |
|---|---|---|
| Filter stehen **über** der Tabelle, auf die sie wirken | W6‑O‑4, #76 | Die Suchzeile steht über der Liste — in den Projektdialogen mit der gestapelten Anordnung sogar **unmittelbar** darüber, statt sie im Klartext der rechten Liste zuzuweisen. Die Regel wird damit wörtlich statt erklärend erfüllt |
| Die gewählte Zeile **bleibt gewählt**, auch wenn der Filter sie ausblendet | `EnergietraegerDialog` (W4) | unverändert — der Filter wirkt auf die Anzeige, nie auf die Auswahl; nach einem Filterwechsel wird die Markierung über `Hinzufuegen` wiederhergestellt (S1.7) |
| Virtualisierung ab 120 Zeilen, `@key` an (Virtualisiert, Zeilenzahl) | W6‑B‑2 | siehe 5.6.6 — hier wird sie erst wirklich gebraucht |
| Klicksemantik, Mehrfachwahl mit Strg/Umschalt | W6‑E‑5 | unverändert; sie macht „Vergleichen" frei (5.5) |
| **44 px** Berührungsziele, **keine Hover-Abhängigkeit** | iL4, `--epos-touchziel` | die Kopfzelle ist 44 px hoch, Titel- und Trichterknopf füllen sie aus |
| Liste in festem Rahmen mit Rollbalken, außer im `Katalograhmen` | W9‑B‑2 | **jetzt auch im Katalograhmen**: Steht die Eingabe unter der Liste statt daneben, muss die Liste eine Höhengrenze haben, sonst schiebt sie den Eingabeblock nach unten. 1,3 × `--epos-listenhoehe` = elf Zeilen (5.6.5) |
| Parameterblock im `Formularraster` | iU8‑E‑2 / W14a‑E‑7 | unverändert — nur breiter. `auto-fill` über `--epos-formularspalte` (26 rem) legt bei der vollen Dialogbreite **zwei** Feldpaare nebeneinander (gemessen bei 1 366 px) bzw. **drei** bei 1 920 px. Die Spaltenzahl hängt an der Breite des **Rasters**, nicht des Fensters — das ist die Regel selbst, keine neue Entscheidung |
| Farben nur als Token in `:root` | W16b‑E‑5 | Trichter, Popover und die Tönung der gefilterten Spalte kommen ohne neue Farbe aus |
| **Gedächtnis:** Filterzustand je Katalog **für die Sitzung**, gemeinsam für Verwaltung und Projektdialog | Q2 | unverändert — jetzt hält der `Katalogfilterstand` je Spalte einen Ausdruck statt je Feld einen Wert, dazu Sortierspalte und ‑richtung |

---

## 6. Das Mockup

`Mockups/Katalogfilter_Vorschlag.html` — eine Datei, kein CDN, kein Rahmenwerk, keine
Schriftdatei von außen. Farben, Maße und Klassennamen sind aus `EPOS.UI/wwwroot/epos-ui.css`
übernommen (Kopfband `#0F1F3D`, Reiterband AliceBlue `#f0f8ff`, cremefarbene Knöpfe `#f5f4ef`,
Beschriftungsspalte 12 rem, kurzes Zahlenfeld 8 em, Berührungsziel 44 px, Ecke 6 px). Es
braucht **keine neue Farbe**.

Es ist ein Bild — aber ein bedienbares, damit der Anwender das Modell ausprobieren kann, statt
es zu glauben:

| bedienbar | tut |
|---|---|
| Reiterwechsel | zeigt eines der drei Blätter; `…#m1` / `#m2` / `#m3` in der Adresszeile wählt gleich das gewünschte |
| **Trichter** | öffnet und schließt das Popover; Esc und ein Klick daneben schließen es, und es rückt in die Liste hinein, wenn es überhängt — genau wie `QuickGrid.razor.js` es tut |
| **„Filter löschen"** | nimmt den Filter dieser Spalte zurück: der Trichter wird vom gefüllten zum Umriss, die Spaltentönung geht weg, die Zeilen, die nur an diesem Filter gescheitert waren, erscheinen, und der Trefferzähler nennt die **gemessene** Zahl für den neuen Stand |
| **„Filter zurücksetzen"** | dasselbe für alle Spalten; danach **verschwindet der Knopf** und der Zähler sagt „x von x Sätzen" |
| **Klick auf einen Spaltentitel** | sortiert die sichtbaren Zeilen wirklich — auf, ab, aus (dritter Klick stellt die Reihenfolge wieder her). Zahlenspalten werden als Zahlen sortiert, Textspalten mit `localeCompare('de')`. Gilt auch für die **Projektliste** in M2, die sortierbar ist, aber keine Trichter trägt |

Die Eingabefelder selbst sind schreibgeschützt; das Blatt rechnet nicht, es zeigt.

| Reiter | zeigt | Datenherkunft |
|---|---|---|
| **M1 — Heizkessel (Verwaltung)** | das Schema vollständig: Suchzeile mit Trefferzahl, **sechs** Parameterspalten über die ganze Breite, Eingabeblock **darunter** im `Formularraster`, Parameterübersicht, Aktionsleiste. `Brennwert` trägt **nur den Sortierpfeil**. Gefiltert: `Brennstoff` enthält „Gas" · `P_th` `10..60` · `η` `>=0,95`; das Popover ist an **Brennstoff** offen → **„15 von 63 Sätzen"** | `Tab_Heizkessel_STAMM` (63 Sätze) |
| **M2 — Wärmepumpe (Projektauswahl)** | **denselben** Aufbau, gestapelt: Projektliste oben über die ganze Breite (sortierbar, ohne Trichter, vier Zeilen hoch), darunter die Übernahmeleiste („▲ ins Projekt übernehmen", „▼ aus dem Projekt entfernen"), darunter Suchzeile und Katalogliste mit **denselben Spaltenköpfen** wie M1/M3 (**neun** Parameterspalten), darunter die Kenndaten. `Kühlen` trägt nur den Sortierpfeil. Gefiltert: `Quelle` enthält „Luft" · `P_N` `5..12` · `VL max` `>=60`; das Popover ist an **Quelle** offen → **„7 von 51 Sätzen"** | `Tab_WP` (Projekt 1008), `Tab_WP_STAMM` + `Tab_Kenndaten_STAMM` (VL min/max und COP A2/W35 aus den Kennlinien) |
| **M3 — PV-Module (Verwaltung, 20 749 Zeilen)** | derselbe Aufbau wie M1 mit **sieben** Parameterspalten. Das Popover ist hier an einer **Zahlenspalte** offen (`P_STC` mit `500..600`) — die Bedienung, zu der **W14a‑E‑10‑Q1** gestellt ist. Gefiltert zusätzlich: `Hersteller` enthält „LONGi" · `η` `>=21,5` → **„15 von 20 749 Sätzen"** | `VDI-3805-Daten/PV/CEC Modules.csv` (20 743 Datenzeilen, 258 Hersteller, 5 Technologien) plus die 6 Sätze aus `Tab_PV_STAMM` |

**Geprüft am Bild, nicht am Gefühl** — Chromium, `1 366 × 768` und `1 920 × 1 080`, je Reiter
ein PNG. Die Zahlen (Anhang A):

| geprüft | Ergebnis |
|---|---|
| Rollt die **Seite** waagerecht? | **Nein**, in keinem der sechs Bilder (`scrollWidth == clientWidth`) |
| Rollt eine **Liste** waagerecht? | **Nein** — auch die neunspaltige Katalogliste in M2 nicht (vorher 118 px). Das erledigt **O‑5** |
| Wie viele Zeilen stehen ohne Rollen in der Liste? | **11** (M1, M3) bei 458 px Höhe; die Projektliste in M2 fasst **4** |
| Wie hoch wird der Dialog? | 1 152 px (M1), 1 228 px (M2), 1 225 px (M3) bei 1 366 px; bei 1 920 px 1 081 / 1 132 / 1 177 px |
| Wo beginnt der Eingabeblock? | **674 px** unter der Dialogoberkante (M1, M3) — bei 768 px Fensterhöhe steht sein Kopfband noch im Bild, seine Felder erreicht man mit dem Rollbalken der Maske. In M2 beginnt der Kenndatenblock bei **825 px** (zwei Listen übereinander). Entscheidend ist: Die **Liste** schluckt die Seite nicht mehr, sie rollt in sich |
| Trefferzahlen der Bedienprobe | M1 15 → 25 → 63 · M2 7 → 10 → 51 · M3 15 → 789 → 20 749 — jeweils gemessene Werte; der Rücksetzer verschwindet beim letzten Schritt |

---

## 7. Vorschlag in drei Stufen

Neu geschnitten nach dem Entscheid **W14a‑E‑10**. Der Schnitt folgt jetzt der **Dialogart**
(Verwaltung → Projekt → Rest), nicht mehr dem Baustein: Es gibt keinen eigenen Filterbaustein
mehr, der zuerst fertig sein müsste.

### Stufe S1 — der Spaltenfilter und die acht Verwaltungsdialoge

> **UMGESETZT am 07.09.2026** (Commits `78b0f1e`, `ce43d2a`, `f842465`). Was dabei anders
> heißt, als hier steht: Der Weg der Controller ist `…StammCtrl.Katalogfilterzeilen()` und
> nicht `Katalogzeilen()` — den Namen führen `PufferSpStammCtrl` (Projektdialog),
> `SolarkollektorenStammCtrl`, `StromspeicherStammCtrl` und `WPStammCtrl` bereits mit einer
> anderen Bedeutung. Dazu kommen zwei Dinge, die die Tabelle nicht nennt: ein **achtes**
> Profil für die Wärmepumpe (S1.6 verlangt ihren Dialog, also braucht sie eines) und der
> Baustein **`Katalogliste`**, der Zone A und das Raster zusammenhält — sonst stünde derselbe
> Aufbau achtmal im Markup.

| Schritt | Inhalt |
|---|---|
| S1.1 | `Katalogfilterprofil` + `Katalogfilter` im Kern (Daten und Rechnung, Kapitel 3) und die kleine Klasse **`Zahlenausdruck`** (`>10`, `<60`, `10..60`, `=15`; ein unverstandener Ausdruck ist **kein** Filter). Beides ohne Oberfläche prüfbar — Kernproben |
| S1.2 | **Spaltenfilter im Standard `Raster`**: eine Vorlage `Spaltenfilter.razor` für das `ColumnOptions`-Popover (Spaltenname, ein Feld, „Filter löschen"), dazu die Stilblattregeln für Trichter (**gefüllt gegen Umriss**, 5.6.2), Popover und die Tönung der gefilterten Spalte (Klasse über `ColumnBase.Class`) — **ohne neue Farbe**. `bunit`-Test |
| S1.3 | **Zone A**: die **eine** Suchzeile — Suchfeld über alle Spalten links, Trefferzahl rechts, „Filter zurücksetzen" nur wenn gesetzt (5.6.4), „Kein Treffer." statt leerer Liste. **Kleiner als in Rev. 2**: keine Filterzeile, keine Aufzählung der gesetzten Filter |
| S1.4 | Die sieben Profile: Heizkessel, BHKW, Solarkollektoren, Pufferspeicher, PV-Modul, Wechselrichter, Stromspeicher — je **sechs bis neun** Spalten aus Kapitel 4 (5.6.5); Kennzeichenspalten ohne `ColumnOptions` |
| S1.5 | **Parameterspalten aus den Controllern**: `…StammCtrl.Katalogzeilen` liefert die Anzeige- und Filterwerte statt `ID, Bezeichner`; abgeleitete Größen (σ, C‑Rate, Modulfläche, COP A2/W35, VL min/max) werden dort gerechnet. **Sieben Controller — der größte Posten der Stufe**, und mit Rev. 3 etwas größer: es sind mehr Spalten je Katalog |
| S1.6 | Die **acht Verwaltungsdialoge** umstellen: `KatalogBrowserDialog` (Heizkessel, BHKW, Solarkollektoren, Pufferspeicher), `ModulKatalogDialog` (PV, Wechselrichter, Stromspeicher), `WaermepumpeStammDialog`. `KatalogFilterArt` und `HatHerstellerfilter` entfallen zugunsten des Profils |
| S1.7 | **`Katalograhmen` von nebeneinander auf untereinander** (5.6.1): Liste über die ganze Breite, Eingabe darunter, Liste höhenbegrenzt auf 1,3 × `--epos-listenhoehe`. Eine Änderung am Baustein, nicht an acht Dialogen — `KatalograhmenTests` ist der Wächter |
| S1.8 | Sortierung über die Spaltenköpfe (auf / ab / aus) und **Wiederherstellen der Markierung** nach einem Filterwechsel; `Virtualisiert` und der `@key`-Fix W6‑B‑2 unter dem Filter nachweisen (5.6.6) |
| S1.9 | Ressourcenschlüssel beider Sprachen + `Werkzeuge/ResourceDesigner` ziehen |

**Aufwand: 10–14 Agentenstunden** (Rev. 2: 10–14, Rev. 1: 12–16). Die Summe bleibt, die
Verteilung ändert sich: **S1.3 wird kleiner** (die Filterzeile mit ihrer Aufzählung, dem
Zustandstext und „Alle löschen" entfällt — geschätzt eine halbe bis eine Stunde weniger),
**S1.4 und S1.5 werden größer** (statt rund fünf Spalten je Katalog sechs bis neun, und die
abgeleiteten Größen dazu), und **S1.7 kommt neu dazu** (der Umbau des `Katalograhmens` auf
untereinander, rund eine halbe Stunde — eine Änderung an einem Baustein). **S1.5 bleibt
unverändert der größte Posten.**
**Risiko: gering.** Reine Oberfläche und Leseweg; **der Referenzlauf ist unberührt**. Wächter:
`StilblattTests`, `ListenrahmenTests`, `RasterTests`, `KatalogdialogTests`,
`FormularrasterTests`, `ParametersatzTests`, neue Kernproben für `Katalogfilter` und
`Zahlenausdruck`, `SqlDialektPruefer` für jede neue Abfrage.

### Stufe S2 — die Projektdialoge und der Assistent

> **UMGESETZT am 07.09.2026** (Zweig `w145-katalogfilter-s2`, sechs Commits: `11e1316`
> Schemaschritt 68 · `9fc285e` `Zweispaltenauswahl` untereinander · `84bd1a0`
> Filterregister und Verwendungsspalte · `e3c4a51` die sechs Erzeuger-Projektdialoge ·
> `5370b5a` die Wärmepumpe im Spaltenmodell · `df07e18` der Assistent). Drei Dinge heißen
> anders, als hier steht: **S2.3** stempelt die Spalte aus der **lebenden Projektliste**
> statt aus einer Zählabfrage (die Dialoge schreiben erst beim OK zurück — eine Abfrage
> wäre nach der ersten Übernahme veraltet; die Forderung „einmal für die ganze Liste"
> bleibt wörtlich erfüllt, siehe 9.1 Q12); **S2.5** heißt `Katalogfilterregister` und liegt
> im Kern neben `Katalogfilter`; und **S2.6** betrifft **elf** Wirte, nicht acht — **fünf**
> davon ohne Katalog (Gebäude, Bedarfsprofile, Wärmebedarf extern, Stromganglinie,
> Solarganglinie), die nur die neue Anordnung erben.

| Schritt | Inhalt |
|---|---|
| S2.1 | Dieselben Profile in den Projektdialogen — Heizkessel, BHKW, Pufferspeicher, PV, Stromspeicher, Solarkollektoren, **Wärmepumpe**: Katalogliste über die ganze Breite, Suchzeile unmittelbar darüber (#76). **Umgesetzt** in `e3c4a51` (sechs Dialoge) und `5370b5a` (Wärmepumpe). Die zwei Filterklapplisten je Hülle sind gefallen — Q5 |
| S2.2 | **Wärmepumpe:** Die elf Bedienelemente des `WaermepumpenKatalogDialog` fallen in die Spalten — 7 Klapplisten (Hersteller, Auslegung, Quelle/`Typ`, Regelung, Bauart, Aufstellung, Zuheizung) werden Textspalten, die 4 Zahlenfelder (VL min/max, P_N min/max) werden **zwei** Zahlenspalten mit je einem Feld. Damit ist der Dialog wie M1 und M3 gebaut — der Kern des Entscheids. **Umgesetzt** in `5370b5a`: neun Spalten, **vier** der elf werden KEINE Spalte (Bauart 45/51 leer, Auslegung ist dieselbe Aussage wie „Kühlen", Regelung und Aufstellung stehen im Kenndatenblock — alles gemessen, siehe 9.2). Dabei fällt auch der Knopf „Modul-Katalog…" im `WaermepumpeStammDialog`: Er zeigte danach dieselbe Liste, die dahinter schon steht |
| S2.3 | Spalte **„im Projekt verwendet"** (eine Zählabfrage für die ganze Liste, nicht je Zeile) — Q12. **Umgesetzt** in `84bd1a0` als `Katalogverwendung.Stempeln` — **aus der lebenden Projektliste** statt aus einer Abfrage; damit entsteht keine neue SQL und die Spalte stimmt auch nach der ersten Übernahme (9.1 Q12) |
| S2.4 | Der Assistent (Seiten 4, 5, 7) erbt das Schema über dieselben Komponenten. **Umgesetzt** in `df07e18` — und zwar ohne eine Zeile Anwendungscode: Die Seitentabelle nennt seit iU9‑W16a.5 die Komponenten selbst. Drei Wächterfälle halten das fest |
| S2.5 | `Katalogfilterstand` je Katalog über die Sitzung, gemeinsam für Verwaltung und Projektdialog (Q2 aus Kapitel 8) — jetzt mit Sortierspalte und ‑richtung. **Umgesetzt** in `84bd1a0` als `EPOS.Kern/Allgemein/Katalog/Katalogfilterregister.cs` (kein `static` in einer Razor-Komponente); `Filterstandvorgabe` ist der benannte Rückweg je Dialog |
| S2.6 | **`Zweispaltenauswahl` von nebeneinander auf untereinander** (Frage **W14a‑E‑10‑Q2**, 8.2): Projektliste oben mit Höhengrenze, Übernahmeleiste mit den zwei Pfeilknöpfen als Textzeile, Katalogliste darunter. Die Umbruchregel bei 900 px, die den Baustein heute schon senkrecht stellt, wird damit zur **einzigen** Anordnung — der Baustein wird eher kleiner. `ZweispaltenauswahlTests` ist der Wächter. **Umgesetzt** in `9fc285e`; es sind **elf** Wirte, nicht acht — **sechs** mit Katalog und **fünf** ohne. Gefallen sind die Medienabfrage, die schmale Mittelspalte samt Token `--epos-zweispalten-mitte` und das waagerechte Pfeilpaar ◀▶; neu ist `--epos-projektlistenhoehe` |
| **Q7** | **Nicht im ursprünglichen Schnitt**, aber Voraussetzung für S2.1 beim Stromspeicher: **Schemaschritt 68** legt `Firma` in `Tab_Stromspeicher_STAMM` und der Projektkopie an und trägt den Bezeichnerpräfix einmalig nach (Befund D‑3). Eine Spalte, die es in der Tabelle nicht gibt, kann man nicht sortieren. **Umgesetzt** in `11e1316`, `SchemaStand.Zielversion` 67 → 68 |

**Aufwand: soll 7–10 Agentenstunden** (Rev. 2: 6–9, Rev. 1: 8–12) — **ist rund 9**, also im
oberen Drittel des Ansatzes. Die Verteilung ist eine andere als geschätzt:

| Schritt | Ansatz | Ist | Warum |
|---|---|---|---|
| S2.1 | groß | **groß** | wie geschätzt: sechs Dialoge, sechs Hüllen, 30 neue Prüffälle |
| S2.2 | mittel | **mittel** | der Dialog ist kleiner geworden, aber sein Prüfstand war vollständig neu zu schreiben (18 Fälle auf die elf Bedienelemente gebaut) |
| S2.3 | mittel | **klein** | die Abfrage entfiel: die Projektliste steht schon da |
| S2.4 | klein | **sehr klein** | nichts zu bauen, nur zu belegen |
| S2.5 | klein | **klein** | eine Klasse, 13 Prüffälle |
| S2.6 | mittel | **mittel** | wie in Rev. 3 vorhergesagt; die elf Wirte brauchten keine Änderung |
| **Q7** | *nicht angesetzt* | **mittel** | Schemaschritt, Nachtrag, Editorfeld, zwei Importwege, Testdatenbank — der einzige Posten, der über den Ansatz hinausgeht |

Die Überschreitung ist damit **vollständig Q7 zuzurechnen**: Rev. 3 hat den Schemaschritt als
Frage geführt (9.1 Q7), nicht als Schritt der Stufe. Alles andere hat Zone A und die
Spaltenköpfe aus S1 geerbt, wie geplant.
**Risiko: gering–mittel** — sieben Dialoge plus Assistent, alle auf demselben Baustein.
**Das neue Risiko lag in S2.6**: Die `Zweispaltenauswahl` steht auch außerhalb der Kataloge;
jeder Wirt musste danach nachgesehen werden. Deshalb war Q2 vor S2.6 zu entscheiden, nicht
währenddessen — **eingetreten ist es nicht**: Die Anordnung steckt im Baustein, und keiner der
elf Wirte brauchte eine Änderung.

### Stufe S3 — Bedarf, Zeitreihen, Vergleich, Import

| Schritt | Inhalt |
|---|---|
| S3.1 | Profile für die drei Bedarfskataloge (Typ, Jahressumme, Beschreibung) — `BedarfAdminDialog` und `BedarfsProfileDialog` |
| S3.2 | Profile für die drei Zeitreihenkataloge samt Jahresarbeit/Spitze aus **einer** `GROUP BY`-Abfrage |
| S3.3 | **Vergleich** von zwei bis drei markierten Zeilen als `Ueberlagerung` (5.5) |
| S3.4 | Dasselbe Schema in den **Importmasken** (`KatalogImportDialog`, `ModulImportDialog`) — dort löst es die zwei vorhandenen Fassungen ab; damit gibt es die Mechanik nur noch einmal |

**Aufwand: 7–9 Agentenstunden.** **Risiko: mittel** bei S3.4 — die Importmasken sind
abgenommen, und ihr Filterverhalten ist bitgleich zum Vorläufer nachgewiesen; die Umstellung
muss das Verhalten erhalten, nicht angleichen.

**Ist (07.09.2026): vier Commits, alle vier Schritte umgesetzt** — siehe Kapitel 9. Das
Risiko von S3.4 hat sich an einer Stelle verwirklicht, und zwar an einer erwarteten: Die
**Filtervorbelegung** der Zahlenleisten (10…200 kW, 0…1000 l, 0…5 m², 0…100 kW; 0…999 W,
0…50 %) ist ersatzlos entfallen, weil es im Spaltenmodell kein Feld gibt, das etwas
vorbelegen könnte. Das ist der einzige Punkt, an dem die Importmasken sich anders verhalten
als vorher; alles Übrige — Mehrfachwahl, gesammelte Wahl, Statuszeile, Virtualisierung,
Übernehmen mit Vorprüfung und Konfliktdialog — steht unverändert und ist von den 45 + 44
Fällen der beiden Prüfstände weiter gedeckt.

**Summe S1–S3: 24–33 Agentenstunden** (Rev. 2: 23–32, Rev. 1: 28–38). Nach **S1** ist der Wunsch W14a‑E‑9 für
die drei Köpfe erfüllt; S2 trägt ihn in die Projektseite und macht die Wärmepumpe zum Zwilling
von Heizkessel und PV-Modul — das, was der Anwender ausdrücklich verlangt hat.

**Reihenfolge:** S1 → S2 → S3. S1 allein ist auslieferbar; S2 ohne S1 ist es nicht (Profil und
Spaltenkopf kommen aus S1). S3.4 sollte **nicht** vor S2 kommen: Wer zuerst an den abgenommenen
Importmasken arbeitet, riskiert einen Rückschritt an einer Stelle, die schon stimmt.

---

## 8. Entscheidungsfragen

> **Die Tabelle steht hier im Wortlaut von Rev. 1** — sie ist das, was der Anwender vorgelegt
> bekam und mit „Q1 bis Q12: ändere den Katalogfilter …" beantwortet hat. Was jede einzelne
> Frage **unter dem Spaltenmodell** noch bedeutet, steht in Kapitel 9; einzelne Empfehlungen
> dieser Tabelle sind dort überholt.

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


### 8.1 Neu aus dem Entscheid: **W14a‑E‑10‑Q1** — ein Feld oder zwei je Zahlenspalte?

**Die Frage.** Textspalten sind klar: ein Feld, „enthält…". Aber eine **Zahlenspalte** soll
nicht nur „enthält 15" können, sondern einen Bereich. Der Anwender hat gesagt: *„nur Suche"*.
Was steht im Popover einer Zahlenspalte?

| | Vorschlag | Bewertung |
|---|---|---|
| **V1** | **EIN Feld, das einen Ausdruck versteht**: `>10`, `>=10`, `<60`, `<=60`, `=15`, `10..60` und die bloße Zahl `15` | **Empfehlung.** Jedes Popover sieht gleich aus, egal welche Spalte — genau das, was der Entscheid verlangt. Der leere Platzhalter nennt die Formen (`z. B. >10, <60, 10..60, =15`), also muss sie niemand auswendig können. Ein unverstandener Ausdruck ist **kein** Filter, wie ein kaputtes Suchmuster (3.3). Die Ausdruckslesung ist eine kleine Klasse im Kern und ohne Oberfläche prüfbar |
| **V2** | **Zwei Felder „von / bis"**, wie in den Importmasken (`ImportZahlenfilter`, Obergrenze 0 = keine Obergrenze) | Nein. Das ist die Bedienung, die der Anwender gerade abgewählt hat — sie kommt aus der Filterleiste zurück, nur kleiner. Zwei Felder heißen zwei Tabulatorstopps, zwei leere Zustände und die Sonderregel „0 heißt alles", die man nicht sieht |
| **V3** | **Zwei Popover-Reihen: eine Klappliste `> < = zwischen` plus Feld(er)** | Nein. Drei Bedienelemente für einen Zahlenvergleich; das Popover wäre kein Popover mehr, sondern ein Formular |

**Empfehlung: V1.** Sie hält den Satz des Anwenders ein („nur Suche"), macht das Popover jeder
Spalte gleich und kostet eine prüfbare Kernklasse statt einer zweiten Bedienung. Im Mockup ist
sie an M3 (`P_STC` mit `500..600`) offen gezeichnet, damit man sie ansehen kann, bevor man
zustimmt.

**Zu entscheiden ist außerdem**, was ohne Vergleichszeichen gilt: `15` = `=15` (Vorschlag) oder
`15` = `>=15`. Der Vorschlag ist die Gleichheit — sie ist die einzige Lesart, die bei einer
Textspalte dasselbe bedeutet.

### 8.2 Neu aus Rev. 3: **W14a‑E‑10‑Q2** — Projektliste und Katalog nebeneinander oder untereinander?

**Die Frage.** Der Anwender hat verlangt: *„Liste wie zuvor über ganze Breite, sonst zu schmale
Liste."* Das gilt auch für die **Projektdialoge**. Dort steht die Katalogliste heute in der
`Zweispaltenauswahl` **rechts neben** der Projektliste (Entscheid **#76**), mit den zwei
Pfeilknöpfen in einer Mittelspalte. „Ganze Breite" und „nebeneinander" schließen einander aus.

| | Vorschlag | Bewertung |
|---|---|---|
| **V1** | **Untereinander.** Oben die kurze Projektliste über die ganze Breite (höchstens vier Zeilen), darunter eine **Übernahmeleiste** mit den zwei Pfeilen als Textknöpfe („▲ ins Projekt übernehmen", „▼ aus dem Projekt entfernen"), darunter Suchzeile und Katalogliste über die ganze Breite, darunter der Detailblock | **Empfehlung.** Drei Gründe, alle gemessen: (1) Die **Katalogliste braucht die Breite** — nebeneinander rollte sie bei 1 366 px schon mit **sechs** Parameterspalten um 118 px in sich (O‑5); untereinander trägt sie **neun** und rollt um **0 px**. (2) Die **Projektliste ist kurz** — im Prüfprojekt zwei Zeilen; nebeneinander verschenkt sie die halbe Breite. (3) Die Suchzeile steht damit **unmittelbar über der Liste**, auf die sie wirkt — Hausregel #76 wörtlich statt im Klartext. Preis: Der Dialog wird höher (gemessen 1 228 px bei 1 366 px Breite), der Detailblock beginnt 825 px unter der Oberkante und braucht bei 768 px Fensterhöhe den Rollbalken der Maske |
| **V2** | **Nebeneinander erst ab 1 920 px**, darunter untereinander (eine Medienabfrage, wie sie der Baustein bei 900 px heute schon hat) | Möglich, aber es sind **zwei** Bilder derselben Maske. Der Anwender hat gerade verlangt, dass jeder Katalogdialog **gleich** aussieht; zwei Anordnungen derselben Maske je nach Schirmbreite arbeiten dagegen. Und die Zahl 1 920 wäre gegriffen: Auch dort bleibt der Dialog auf `max-width` begrenzt, die Katalogliste bekäme also nicht die volle Breite, sondern wieder nur einen Teil |
| **V3** | **Nebeneinander lassen**, Katalogliste breiter als die Projektliste (der alte Vorschlag O‑5: links `flex: 0 1 24rem`) | Das war die Antwort von Rev. 2 auf O‑5. Sie hilft, reicht aber nicht: Gemessen blieben bei 1 366 px 118 px Überhang, und mit den drei Spalten, die Rev. 3 dazugewinnt, wäre es mehr. Es widerspricht außerdem dem Wortlaut („über ganze Breite") |

**Empfehlung: V1 (untereinander).** Der Entscheid **#76** („Projekt links, Katalog rechts")
ändert sich damit in seiner **Anordnung**, nicht in seiner Aussage: Es bleiben zwei Listen mit
Übernahmeknöpfen dazwischen, und der Filter steht weiter über der Liste, auf die er wirkt.
**Zu entscheiden vor S2.6** — danach ist es ein Umbau an acht Wirten statt an einem.

---

## 9. Entscheide

**Kennung des Wunsches: `W14a‑E‑9`** (07.09.2026). Die Fragen aus Kapitel 8 tragen die
Kennungen `W14a‑E‑9‑Q1` … `W14a‑E‑9‑Q12`.

| Kennung | Entscheid | Datum | Umsetzung |
|---|---|---|---|
| **W14a‑E‑10** | „Q1 bis Q12: ändere den Katalogfilter — nur Suche (Screenshot 2), Hersteller, Brennstoff etc. sollte an Spalte mit Sortieren und Suchen (Beispiel Screenshot 1) erfolgen. Das Schema des Dialogs sollte immer gleich aussehen (Wärmepumpe ähnlich wie PV-Module und Heizkessel)." — dazu: „erstelle aktualisiertes Mockup" | 07.09.2026 | **Konzept 5.6 und Mockup fortgeschrieben** (Rev. 2). Umsetzung: Stufe S1–S3, Kapitel 7. Nichts davon ist gebaut |
| **W14a‑E‑10, Ergänzung** | „W14a‑E‑10: Katalogfilter — Die Filter sollten über den Spaltennamen sitzen (Hersteller, Modell, Leistung, … — siehe Screenshot als Beispiel) und nicht separat, außer ‚Suche' über alle Felder. Liste wie zuvor über ganze Breite, sonst zu schmale Liste. Erstelle neues Mockup." | 07.09.2026 | **Rev. 3, dieser Stand.** Drei Folgen: (1) die **Filterzeile fällt** — über der Liste steht nur noch die Suche mit der Trefferzahl, ein gesetzter Filter ist allein am **gefüllten Trichter** erkennbar (5.6.4); (2) die **Liste läuft über die ganze Breite**, der Eingabe-/Detailblock steht darunter (5.6.1) — damit tragen die Listen **sechs bis neun** Parameterspalten statt fünf (5.6.5); (3) das gilt auch für die Projektauswahl, weshalb Projektliste und Katalog **untereinander** stehen → **Frage W14a‑E‑10‑Q2** (8.2). Der Aufwand verschiebt sich um rund eine Stunde nach oben (Kapitel 7) |
| **W14a‑E‑10‑Q1** | „Katalogfilter: Empfehlung jeweils ja" — **EIN Feld je Zahlenspalte** (V1 aus 8.1): Es versteht `>10`, `>=10`, `<60`, `<=60`, `=15`, `10..60` und die bloße Zahl `15` (= `=15`); ein unverstandener Ausdruck ist **kein** Filter | 07.09.2026 | **Umgesetzt** als `EPOS.Kern/Allgemein/Katalog/Zahlenausdruck.cs`, geprüft ohne Oberfläche in `EPOS.Kern.Tests/ZahlenausdruckTests` (alle sieben Formen, zwei unverstandene, de‑DE und en‑US) |
| **W14a‑E‑10‑Q2** | dieselbe Antwort: **untereinander** (V1 aus 8.2) — Projektliste oben, Übernahmeleiste, Katalogliste, Detailblock | 07.09.2026 | Betrifft **Stufe S2** (Schritt S2.6, `Zweispaltenauswahl`). In S1 ist der Zwilling erledigt: Der `Katalograhmen` steht seit S1.7 untereinander |
| **W14a‑E‑10‑O‑10** | „O‑10 und O‑11: Empfehlung" — die **Filtervorbelegung der Importmasken bleibt entfallen**; nach dem Einlesen steht die ganze Datei in der Liste, eingegrenzt wird über den Trichter der Spalte (`10..200`). Der benannte Rückweg (vorbelegter Spaltenausdruck im `Katalogfilterstand`) wird nicht gebaut | 07.09.2026 | Kein Programmschritt; O‑10 in Kapitel 10 geschlossen |
| **W14a‑E‑10‑O‑11** | dieselbe Antwort — die **Suche der Importmasken läuft über alle Spalten**, wie in den anderen 19 Wirten seit S1 (Hausregel 5.6.4); keine Spaltenliste am Suchfeld, die genaue Suche leistet der Trichter je Spalte | 07.09.2026 | Kein Programmschritt; O‑11 in Kapitel 10 geschlossen |

**Stufe S3 ist umgesetzt** — Zweig `w148-katalogfilter-s3`, vier Commits:
`92854e1` (**S3.1**: die drei Bedarfskataloge — `Katalogfilterprofil.FuerBedarf`,
`BedarfStammCtrl.Katalogfilterzeilen`, `BedarfAdminDialog` und `BedarfsProfileDialog` auf
der `Katalogliste`), `1b02bce` (**S3.2**: die drei Zeitreihenkataloge —
`Katalogfilterprofil.FuerZeitreihe`, `GanglinienAuswertungCtrl.Kennzahlen` mit **einer**
`GROUP BY`-Abfrage je Katalog, sechs Wirte), `df34bc5` (**S3.3**: der Vergleich im Baustein
`Katalogliste` — **Q3 = ja**) und `2186ab9` (**S3.4**: die zwei Importmasken auf der
`Katalogliste` — **Q10 = ja**). **Q3 = ja, Q6 = ja (bestätigt), Q10 = ja; Q11 unverändert
nein.** Der Rechenweg ist unberührt: Referenzlauf 1030/1007/1017/1045 byte-gleich gegen
`Referenzlaeufe/2026-09-07_R5_Zahlenrand`; **kein Schema angefasst**
(`SchemaStand.Zielversion` bleibt 68, die Testdatenbank ist byte-gleich).

**Damit tragen 21 Dialoge in 16 Komponenten die eine `Katalogliste`** — acht in der
Verwaltung, sieben im Projekt, sechs für Bedarf und Zeitreihen und die zwei Importmasken
(mehrere Wirte teilen sich eine Komponente). Die Filtermechanik gibt es im ganzen Haus
**einmal**.

**Stufe S2 ist umgesetzt** — Zweig `w145-katalogfilter-s2`, sechs Commits:
`11e1316` (**Q7**: Schemaschritt 68, `Firma` in `Tab_Stromspeicher_STAMM` und der
Projektkopie, Nachtrag aus dem Bezeichnerpräfix, `SchemaStand.Zielversion` 67 → 68),
`9fc285e` (**S2.6**: die `Zweispaltenauswahl` steht untereinander — **Q2 = ja**),
`84bd1a0` (**S2.5** `Katalogfilterregister` und **S2.3** `Katalogverwendung` — **Q12 = ja**),
`e3c4a51` (**S2.1**: die sechs Erzeuger-Projektdialoge auf der `Katalogliste`),
`5370b5a` (**S2.2**: die Wärmepumpe im Spaltenmodell — **der Kern des Entscheids**) und
`df07e18` (**S2.4**: der Assistent). **Q2 = ja, Q7 = ja, Q12 = ja.** Der Rechenweg ist
unberührt: Referenzlauf 1030/1007/1017/1045 byte-gleich gegen
`Referenzlaeufe/2026-09-07_R5_Zahlenrand` — auch nach Schemaschritt 68, denn kein
Rechenweg liest den Hersteller.

**Entscheid #76 ist durch Q2 ERGÄNZT, nicht abgelöst.** Die Regel „Projekt ↔ Datenbank
immer über `Zweispaltenauswahl`" gilt unverändert und für dieselben **elf** Wirte; was sich
ändert, ist ihre ANORDNUNG — aus nebeneinander (Projekt links, Katalog rechts, zwei Knöpfe
in einer schmalen Mittelspalte) wird untereinander (Projektliste oben mit Höhengrenze,
Übernahmeleiste als Textzeile, Katalogliste unten über die ganze Breite). Der Kern von #76
bleibt wörtlich stehen: **ein** Baustein, kein Dialog baut das Muster selbst, und der Filter
steht über der Liste, auf die er wirkt — jetzt sogar unmittelbar darüber statt im Klartext
zugewiesen. Die Parameternamen `Links`/`Rechts` bleiben ebenfalls: Sie benennen die Rolle
(Projekt- und Katalogblock), nicht den Platz.

**Stufe S1 ist umgesetzt** — Zweig `w140-katalogfilter-s1`, drei Commits:
`78b0f1e` (Kern: `Katalogfilterprofil`, `Katalogfilter`, `Zahlenausdruck`, acht
`…StammCtrl.Katalogfilterzeilen`, 49 Ressourcenschlüssel), `ce43d2a` (Bausteine
`Spaltenfilter` und `Katalogliste`, Stilblatt, `Katalograhmen` untereinander) und
`f842465` (die acht Verwaltungsdialoge). **Q1 = ja, Q2 = ja.** Der Rechenweg ist
unberührt: Referenzlauf 1030/1007/1017/1045 byte-gleich gegen
`Referenzlaeufe/2026-09-07_R5_Zahlenrand`.

### 9.1 Was der Entscheid für jede der zwölf Fragen bedeutet

| Nr. | Stand nach W14a‑E‑10 |
|---|---|
| **Q1** — sind die Parameter aus Kapitel 4 die richtigen? | **Ja, unverändert** — sie werden jetzt **Spalten** statt Felder einer Leiste. Die Grenze ist mit Rev. 3 weiter außen: über die ganze Breite haben **sechs bis neun** Platz statt fünf (5.6.5), damit stehen die **Spalten**-Zeilen aus Kapitel 4 vollständig da. Es bleibt: **filterbar ist nur, was als Spalte dasteht** — und was gemessen fast nie gepflegt ist (Vorlauf/Rücklauf beim Kessel, Bauart bei der Wärmepumpe), fällt heraus |
| **Q2** — Filterzustand merken? | **Unverändert: Sitzung**, je Katalog, gemeinsam für Verwaltung und Projektdialog (S2.5). Der `Katalogfilterstand` hält jetzt je Spalte einen Ausdruck plus Sortierspalte und ‑richtung. **Erledigt mit S2.5** (`84bd1a0`): `EPOS.Kern/Allgemein/Katalog/Katalogfilterregister.cs` hält je `Anlagenart` **eine** Instanz unter einem Schloss. Es wird **nichts geschrieben** — kein `Dienste.Einstellungen`, keine Datei; `Leeren()` ist der einzige Weg hinaus. Der **Projektwechsel räumt nicht auf**: Der Filter hängt am KATALOG, und der ist projektübergreifend |
| **Q3** — Vergleich von zwei bis drei Zeilen? | **Ja — erledigt mit S3.3** (`df34bc5`). Er sitzt im BAUSTEIN `Katalogliste` und gilt damit in allen Wirten auf einen Schlag. Markiert wird mit `Strg`/`Umschalt` auf dem vorhandenen Wahlknopf (keine zweite Spalte), die vierte Markierung wird mit Hinweis abgewiesen, und die Zeilen kommen aus `ParameterUebersichtCtrl.Werte` — dieselbe Quelle wie die Parameterübersicht aus W14a‑E‑8, keine zweite Liste. Einzelheiten in 5.5 |
| **Q4** — welche Design-Variante? | **Ersetzt.** Nicht das Zonenmodell aus 5.2, sondern **A4**: Filter und Sortierung am Spaltenkopf (5.6). Klapplisten, Bereichsfeldpaare, „Weitere Filter ▾" und die Chips entfallen — und mit Rev. 3 auch die **Filter-/Trefferzeile**. Es bleibt **eine** Zeile über der Liste: Suchfeld links, Trefferzahl rechts, dazu der Rücksetzer, sobald etwas gesetzt ist (5.6.4). Dazu kommt die **Anordnung**: Liste über die ganze Breite, Eingabe darunter (5.6.1) |
| **Q5** — ersetzen Zahlenbereiche die sechs festen Leistungs- bzw. Volumenstufen? | **Ersetzt: die festen Stufen entfallen ersatzlos.** Rev. 1 wollte sie als Schnellwahl behalten; im Spaltenmodell gibt es keine Klappliste mehr, in die sie passen. **Sortieren nach `P_th` und der Ausdruck `10..60` leisten dasselbe genauer** — und lösen zugleich den Einwand aus 5.1/2 („wer einen 30‑kW-Kessel sucht, bekommt jede Zeile unter 50 kW"). `LEISTUNG_SQL` und `VOLUMEN_SQL` fallen mit S1.5 |
| **Q6** — wo wird gefiltert, Speicher oder SQL? | **Bestätigt nach S3: der Controller entscheidet** — und in allen 21 Wirten fiel die Entscheidung auf den SPEICHER, weil `Katalogfilter.Anwenden` eine reine Funktion ist und die größte Liste (20 743 PV‑Module) einmal je Filterschritt durchläuft. **Unverändert: der Controller entscheidet** (bis rund 5 000 Zeilen im Speicher, darüber `WHERE` mit `DbParam`). Wichtiger geworden ist die andere Hälfte der Antwort: Der Filter greift **vor** dem Raster; das Raster bekommt eine bereits eingeschränkte `IQueryable` (5.6.6) |
| **Q7** — bekommt `Tab_Stromspeicher_STAMM` eine Spalte `Firma` (D‑3)? | **Ja — und dringlicher als in Rev. 1.** „Hersteller" ist jetzt eine **Spalte**, nach der man sortiert und filtert; eine Spalte, die es in der Tabelle gar nicht gibt, kann man nicht sortieren. **Erledigt mit Schemaschritt 68** (`11e1316`): `Firma` steht in **beiden** Tabellen (Stamm und Projektkopie, wie es die sechs anderen Kataloge halten), `SchemaStand.Zielversion` 67 → **68**, die Testdatenbank ist eingespielt (STRICT‑Tabellen bleiben **117** — eine Spalte, keine Tabelle). Der Nachtrag trägt den Text vor dem ersten Doppelpunkt einmalig nach und **rät nicht**: Was kein Präfix hat, bleibt leer — im Auslieferungskatalog sind das **alle fünf** Altsätze. Das **Bezeichnerpräfix bleibt Rückfall** (`StromspeicherStammCtrl.Hersteller`), die gepflegte Spalte schlägt es; beide Importwege (CEC, bslib) schreiben `Firma` **zusätzlich** zum Präfix, damit ein umbenannter Bezeichner den Hersteller nicht verliert |
| **Q8** — wird das Brennwert-Kennzeichen berichtigt (D‑1)? | **Unverändert: ja**, berichtigt werden die **Daten**. Im Spaltenmodell ist „Brennwert" eine Spalte mit „ja"/„nein"; solange die Daten schief sind, findet „enthält ja" 6 von 63 Sätzen, während 46 Beschreibungen „Brennwert" nennen — **die Suche über alle Spalten findet die 46**, und genau daran fällt der Datenfehler auf |
| **Q9** — Wirkungsgrad in zwei Einheiten (D‑2)? | **Unverändert:** ein Wert **> 2** gilt als Prozent und wird durch 100 geteilt — jetzt schon beim **Anzeigen**, denn der Spaltenfilter arbeitet auf dem angezeigten Wert (5.6.3). Der eine Datensatz (`eloBLOCK VE 10`) wird zusätzlich berichtigt |
| **Q10** — gilt das auch für die Importmasken? | **Ja — erledigt mit S3.4** (`2186ab9`). `KatalogImportDialog` (fünf Ausprägungen) und `ModulImportDialog` (zwei) zeigen ihre Kandidatenliste in der `Katalogliste`; die zwei handgeschriebenen Filterfassungen — Klapplisten Hersteller/Technologie plus Suche, Herstellerklappliste plus ein bis zwei von/bis-Paare — sind gefallen. Die Spalten kommen als `Listenprofil` aus den zwei Importprofilen im Kern. **Erhalten** sind Mehrfachwahl W6‑E‑5 samt der über Filterschritte gesammelten Wahl, die Statuszeile, die Virtualisierung ab 120 Zeilen, der `@key`-Fix W6‑B‑2 und der ganze Übernahmeweg; **entfallen** ist die Filtervorbelegung des Designers (siehe O‑10) |
| **Q11** — gilt es auch für Gebäude, Gebäudetypen, Klimadaten, Kosten? | **Unverändert: nicht in S1–S3.** Der Wunsch nennt drei Köpfe. Die Ausweitung ist danach eine Stunde je Katalog — im Spaltenmodell eher weniger, weil nur Spalten dazukommen |
| **Q12** — Spalte „im Projekt verwendet"? | **Unverändert: ja, in S2.3**, und nur in den **Projektdialogen**. **Erledigt** (`84bd1a0`) — mit **einer begründeten Abweichung vom Wortlaut**: Rev. 3 nennt „eine Zählabfrage für die ganze Liste"; die Quelle ist statt dessen die **lebende Projektliste** des Dialogs. Grund: Die Projektdialoge schreiben erst beim OK zurück (der Aufrufer löscht danach die `Tab_Energieanlagen` dieses Typs und schreibt die Liste neu) — eine Zählabfrage wäre in dem Augenblick veraltet, in dem der Anwender die erste Zeile übernimmt oder entfernt. Die Forderung „**einmal** für die ganze Liste, nicht je Zeile" bleibt wörtlich erfüllt: Die Namen wandern in ein `HashSet`, danach ist jede Zeile ein Nachschlagen in konstanter Zeit — für die 20 749 PV‑Module **ein** Durchlauf. Und es entsteht dadurch **keine neue SQL**. Die Spalte ist **sortierbar ohne Trichter** (ein Freitextfeld auf zwei Werten wäre ein Bedienelement für nichts) und steht am **Ende** des Profils; sie sortiert wie eine Zahl (0/1), damit „Ja" in beiden Sprachen an derselben Stelle steht |

### 9.2 Die Spaltenwahl der Wärmepumpe (S2.2) — was gemessen wurde

Der Entscheid nennt sieben Klapplisten und vier Zahlenfelder. **Neun** davon werden Spalten,
**vier** nicht. Die Zahlen sind gegen `Referenzlaeufe/Kenndaten_Test.sqlite` gemessen (51
Stammsätze) und stehen als Prüffall in `EPOS.Kern.Tests/KatalogspaltenfilterTests`:

| Bedienelement | wird | Begründung |
|---|---|---|
| Hersteller | **Textspalte** mit Trichter | gepflegt |
| Quelle (`Typ`) | **Textspalte** mit Trichter | gepflegt; „enthält Luft" trifft 34 von 51 |
| Zuheizung | **Zahlenspalte** | `Tab_WP_STAMM.Heizung` [kW] |
| P_N min **+** max | **eine** Zahlenspalte | ein Feld statt zweier Kästen — Q1; `5..12` trifft 31 |
| VL min | **Zahlenspalte** | `Min(Vorlauf)` je `ID_WP` aus `Tab_Kenndaten_STAMM` |
| VL max | **Zahlenspalte** | `Max(Vorlauf)` je `ID_WP`; `>=60` trifft 18 |
| **Bauart** | *keine Spalte* | **45 von 51 Sätzen leer** (5 Split, 1 Monoblock). Eine Spalte, die fast immer leer ist, kostet Breite und trägt nichts — sie steht im Kenndatenblock |
| **Auslegung** | *keine Spalte* | „Heizen"/„Heizen/Kühlen" ist **gerechnet** aus `Kuehlleistung > 0` — und genau das sagt die Spalte „Kühlen" schon. Die zwei Mengen sind Satz für Satz gleich (**15 von 51**) |
| **Regelung** | *keine Spalte* | im Kenndatenblock; der Planer sucht nicht danach |
| **Aufstellung** | *keine Spalte* | ebenso |

Dazu die zwei Spalten, die der Katalog schon in der Verwaltung führt (Modell, Kennzeichen
Kühlen) und der abgeleitete **COP bei A2/W35** — zusammen **neun**. Der Filterstand des
Mockups M2 (Quelle „Luft" **und** P_N `5..12` **und** VL max `>=60`) trifft damit **7 von
51** — dieselbe Zahl wie über die elf Bedienelemente des Vorläufers; der Vergleich beider
Wege steht als Prüffall
`Waermepumpe_Spalten_treffen_dieselbe_Menge_wie_die_elf_Bedienelemente`.

**Deshalb bleiben `WaermepumpenKatalogFilter` und `WPStammCtrl.KatalogZeilen` stehen**,
obwohl sie seit S2.2 keinen Wirt mehr haben: Sie sind die Gegenprobe. Dasselbe gilt für
`HeizkesselStammCtrl.Filtern`+`LEISTUNG_SQL`, `PufferSpStammCtrl.Filtern`+`VOLUMEN_SQL`,
`BHKWStammCtrl.Filtern`+`LeistungFilterText` und `BHKWCtrl.LeistungFilterText` — alle vier
mit einem ausdrücklichen Markierungsblock im Quelltext. Noch **einen Wirt** haben
`PhotovoltaikStammCtrl.Filtern` (`PhotovoltaikHuelle.ModulEintraege`, W6‑O‑6) und
`WechselrichterStammCtrl.Filtern` (`WechselrichterEintraege`, W6‑O‑4): Sie füllen die
Gerätewahl der PV‑Maske, nicht einen Katalogfilter.

---

## 10. Offene Punkte

| Kennung | Punkt |
|---|---|
| **O‑1** | Die fünf Wärmepumpenspalten `Laenge`, `Breite`, `Hoehe`, `Gewicht`, `Raum` sind in `ParameterVerwendung` mit **`Keine`** eingestuft — niemand liest sie. Sie stehen weder im Filter noch in den Spalten dieses Vorschlags. Ob sie bleiben, ist eine eigene Frage |
| **O‑2** | Die **Stromkennzahl** des BHKW, die **C‑Rate** des Stromspeichers und der **COP bei A2/W35** sind hier abgeleitete Anzeigegrößen. Ob sie in den Katalog gehören (gerechnet beim Import, gespeichert), ist eine Datenmodellfrage und keine Oberflächenfrage |
| **O‑3** | Der `Motortyp` des BHKW führt 45 verschiedene Werte in 79 Sätzen, darunter Schreibvarianten desselben Motors („Gas-Otto-Motor", „Gas-Otto-Motor_ 2G", „Gas-Otto-Motor_2G"). Eine Klappliste darüber wäre unbrauchbar; eine Bereinigung ist eine eigene Aufgabe |
| **O‑4** | Der Hersteller steht in mehreren Schreibweisen desselben Hauses („EC Power A/S" 10 Sätze, „EC POWER A/S" 7; „2G Energy AG" 21, „2-G Energietechnik GmbH" 17). Das Spaltenmodell **entschärft** das: „enthält 2G" trifft beide Schreibweisen, „enthält EC Power" ebenfalls (Groß/Klein egal) — eine Klappliste hätte sie als verschiedene Hersteller geführt. Die Zusammenführung selbst gehört weiter zur Dublettenpflege |
| **O‑5** | ~~Die Katalogliste der `Zweispaltenauswahl` hat nur die halbe Breite.~~ **Erledigt mit Rev. 3 (07.09.2026).** Der Anwender hat die Ursache gestrichen, nicht das Symptom: Die Liste läuft über die **ganze** Breite, die zwei Listen stehen untereinander. Gemessen rollt die Katalogliste in M2 jetzt um **0 px** statt um 118 px — und das mit **neun** Parameterspalten statt sechs. Die Höchstbreite der linken Spalte (`flex: 0 1 24rem`) wird damit gegenstandslos; an ihre Stelle tritt der Umbau des Bausteins, zu dem **Frage W14a‑E‑10‑Q2** (8.2) gestellt ist |
| **O‑6** | ~~**Ein neues Raster schließt das Popover.**~~ **Entschieden und belegt mit S1.2 (07.09.2026).** Gemessen in `EPOS.UI.Tests/Bausteine/SpaltenfilterTests.O6_Ein_neu_aufgebautes_Raster_schliesst_das_Popover`: Führt QuickGrid das Popover selbst über `ColumnOptions`, steht es nach einem Klick auf den Optionsknopf da — und ist nach einem Wechsel der Zeilenzahl **weg**; der `@key`-Fix W6‑B‑2 baut das Raster neu auf, und die neue Instanz beginnt ohne Zustand. **Gewählt ist der erste der zwei Wege: Das Feld wirkt bei ENTER oder beim VERLASSEN, nicht beim Tippen.** Tippen ändert nur den Text in `Spaltenfilter` (`@oninput`), die Zeilenmenge bleibt unberührt, das Raster wird nicht neu gebaut und das Popover steht mit seinem Schreibzeiger. Enter, ein Feldwechsel (`onchange`) und das Verlassen (`onfocusout`) übernehmen und schließen; Esc schließt ohne zu übernehmen. **Der zweite Weg** — „Filterstand im Wirt halten und das Popover nach dem Neuaufbau wieder öffnen" — ist zur Hälfte trotzdem gebaut (der Stand liegt im `Katalogfilterstand` des Wirtes, sonst wäre er nach dem ersten Zeichen weg), löste den Schreibzeiger aber **nicht**: Das Eingabefeld wäre nach dem Neuaufbau ein NEUES DOM-Element, der Fokus damit fort, und der Anwender tippte nach jedem Zeichen ins Leere |
| **O‑7** | **Der Dialog wird höher, als ein 768‑px‑Fenster fasst.** Steht die Eingabe unter statt neben der Liste, misst der Dialog gemessen 1 152 px (M1), 1 225 px (M3) und 1 228 px (M2) bei 1 366 px Breite; der Eingabeblock beginnt 674 px unter der Oberkante, in M2 der Kenndatenblock erst bei 825 px. Bei 768 px Fensterhöhe braucht man dafür den Rollbalken der **Maske**. Das ist kein Bruch mit W9‑B‑2 — die **Liste** rollt in sich und schluckt die Seite nicht mehr —, aber es ist der Preis der Anordnung und gehört auf den Tisch. Drei Stellschrauben, falls es stört: die Listenhöhe (heute elf Zeilen), die Parameterübersicht (ist schon zugeklappt), und in M2 der Kenndatenblock, den man ebenfalls als Aufklapper führen könnte. **Nach der Abnahme des Mockups zu entscheiden**, nicht vorher — es hängt daran, wie der Anwender die Maske tatsächlich benutzt.<br><br>**Stand nach S1.7 (07.09.2026): unverändert offen, und die Zahlen sind die des MOCKUPS geblieben.** Eine Dialoghöhe ist im bunit-Markup **nicht messbar** — bunit rechnet kein CSS aus (Lehre W6‑B‑1); geprüft ist deshalb nur die REGEL im Stilblatt: Die Liste trägt `max-height: calc(var(--epos-listenhoehe) * 1.3)` = 458 px = elf Zeilen und rollt in sich, der Eingabeblock rollt nicht mehr selbst, und der Rollbalken der Maske sitzt an `.epos-katalog-dialog` (`overflow: auto`). Die Höhe misst der **Anwender** an der Windows-Abnahme — Abnahmepunkte A‑W14a‑E10‑11 und ‑12.<br><br>**Stand nach S2 (07.09.2026): unverändert offen.** S2.6 hat die Frage von den Verwaltungs- auf die PROJEKTdialoge ausgeweitet — dort stehen jetzt Projektliste, Übernahmeleiste, Katalogliste und Detailblock untereinander, also **vier** Blöcke statt zwei. Gegengerechnet ist dabei die Projektliste: Sie trägt seit S2.6 `--epos-projektlistenhoehe` (12 rem = vier Zeilen) und wächst nicht mehr mit ihrem Bestand. Auch das ist im Markup nicht messbar; es gehört zu denselben zwei Abnahmepunkten |
| **O‑8** | **Die Spalte „im Projekt verwendet" hängt an der lebenden Liste, nicht an der Datenbank** (9.1 Q12). Das ist innerhalb eines Dialogs richtig und dort auch die einzig richtige Quelle. Was sie NICHT sagt, ist, ob ein Katalogsatz in einem **anderen** Projekt verwendet wird — die Frage, die bei der Katalogpflege („darf ich den löschen?") interessiert. Dafür gäbe es sie in der VERWALTUNG, und dort wäre sie tatsächlich eine Zählabfrage über alle Projekte. Q12 hat sie ausdrücklich auf die Projektdialoge beschränkt; ob die Verwaltung eine zweite, anders gemeinte Spalte bekommt, ist eine eigene Frage. Der Löschweg ist davon unberührt — er fragt weiter `GesperrtDurchProjekt` |
| **O‑9** | **Vier Wärmepumpenmerkmale sind jetzt nur noch über die Detailansicht erreichbar** (9.2): Bauart, Regelung, Aufstellung und die abgeleitete Auslegung. Solange Bauart in 45 von 51 Sätzen leer ist, kostet eine Spalte mehr, als sie trägt — **wird der Katalog gepflegt, kehrt sich das um**. Die Spalte wäre dann eine Zeile in `Katalogfilterprofil` und sonst nichts; die Zahlen dafür stehen in `KatalogspaltenfilterTests` und fallen rot aus, sobald sich der Datenbestand ändert |
| **O‑10** | **Entschieden 07.09.2026 („O‑10 und O‑11: Empfehlung"): bleibt so, keine Vorbelegung.** **Die Filtervorbelegung der Importmasken ist mit S3.4 entfallen** — 10…200 kW (Heizkessel), 0…1000 l (Pufferspeicher), 0…5 m² (Solar), 0…100 kW (Wärmepumpe), 0…999 W und 0…50 % (PV‑Module). Sie war die Vorbelegung von ZWEI Zahlenfeldern; im Spaltenmodell gibt es kein Feld, das etwas vorbelegen könnte. Nach dem Lesen steht jetzt die ganze Datei da, und wer eingrenzen will, schreibt `10..200` in den Trichter der Spalte. **Zwei Gründe, warum das die bessere Antwort ist:** Beim Solarimport ließ die alte Vorbelegung nach dem Lesen regelmäßig NULL Zeilen stehen (drei Prüffälle mussten sie eigens aufmachen), und beim Stromspeicherimport war eine Vorbelegung, die Zeilen verschwinden lässt, schon mit W13‑E‑2 als unerklärlich verworfen worden. **Wenn der Anwender sie zurückhaben will**, wäre der Weg ein vorbelegter Spaltenausdruck im `Katalogfilterstand` beim Aufmachen — eine Zeile Code je Ausprägung, aber eben ein Filter, der ohne Zutun Zeilen ausblendet |
| **O‑11** | **Entschieden 07.09.2026 („O‑10 und O‑11: Empfehlung"): bleibt so, Suche über alle Spalten.** **Die Suche der Importmasken läuft jetzt über ALLE Spalten statt nur über den Namen.** Der Vorläufer hielt `Suchmuster.Uebersetzen(text)` gegen `ImportZeile.Name` (Modulimport) bzw. gegen Bezeichner und Firma (`VdiAuswahlFilter.Passt`, Katalogimport); `Katalogfilter` hält denselben Ausdruck gegen jede Spalte. Die Platzhalter `*` und `?` gelten unverändert — es ist dasselbe `Suchmuster`. Die Folge ist eine WEITERE Treffermenge: `*650*` trifft im Modulimport künftig auch eine Zeile, deren Isc oder Voc „650" enthält. Das ist die Hausregel der `Katalogliste` (5.6.4) und in allen anderen 19 Wirten seit S1 so; ob die Importmasken eine Ausnahme brauchen, ist eine eigene Frage — die Antwort wäre eine Spaltenliste am Suchfeld, nicht ein zweiter Suchweg |
| **O‑12** | **`ModulImportProfil` und `KatalogImportProfil` führen Daten, die keine Maske mehr liest**: `FilterHersteller`, `FilterTechnologie`, `FilterSuche`, `SuchePlatzhalter`, `TextAlle`, `Zahlenfilter` samt Vorbelegungen und `KatalogImportProfil.FilterVon`/`FilterBis`/`FilterMaximum`/`FilterBezeichnung`/`Zweitfilter`/`HerstellerFilter`. Sie stehen absichtlich noch da — sie sind die Herkunft der Spaltentitel und der Nachkommastellen, und ihre Prüffälle beschreiben, was die Masken **vorher** taten. Ein Aufräumen wäre eine eigene, gefahrlose Ablösung nach der Windows-Abnahme von S3.4 |
| **O‑13** | ~~**Der Fall `KataloglisteTests.Zwanzigtausend_Zeilen_werden_zu_fuenfzehn_und_das_Raster_zeigt_sie` flackert unter Last.**~~ **Erledigt am 07.09.2026.** Befund der Windows-Sandbox: `EPOS.UI.Tests` 3 216 von 3 217 grün, dieser eine Fall „flackert unter Last und ist allein grün"; auf Linux in jedem Gate grün. Es war **kein Fehler der `Katalogliste`, sondern ein Wettlauf im Test** — derselbe wie in W16b‑O‑2 (`c3b1513`) und W6‑B‑2‑O‑1 (`586d8b5`). Ursache: bunits synchrones `Change()` gibt das Ereignis nur beim Zeichner ab und wartet nicht auf den Behandler; ist dessen Warteschlange belegt, wird das Ereignis eingereiht und die nächste Zeile des Falls liest den Stand VOR dem Filtern („20.749 von 20.749 Sätzen"). Das Werkstück legt der `@key`-Fix W6‑B‑2 selbst hin: Beim Übergang 20 749 → 15 wechseln `Virtualisiert` **und** Zeilenzahl, Blazor verwirft das QuickGrid und baut ein neues mitsamt dessen `OnAfterRenderAsync` und asynchronem Datenabruf. **Behoben nur am Test** (Helfer `Gezeichnet`, `Sortiert` und ein wartendes `Filter` in `KataloglisteTests`), kein Produktcode. `SpaltenfilterTests` ist durchgesehen: Dort bleibt jede Liste unter der Schwelle `VirtualisierenAb` = 120, es wird nie neu aufgebaut, und die Fälle sind unverändert. Messungen und Wiederholungsnachweis im Protokoll `iU9_W15a_Blazor_Port_Protokoll.md`, Abschnitt „W14a‑E‑10‑O‑13" |

---

## Anhang A — Die Zahlen des Mockups

Gemessen am 07.09.2026 gegen `Referenzlaeufe/Kenndaten_Test.sqlite` und
`VDI-3805-Daten/PV/CEC Modules.csv`.

| Größe | Wert |
|---|---|
| `Tab_Heizkessel_STAMM` gesamt | 63 |
| davon `Brennstoff` enthält „Gas" (52) **und** `P_th 10..60` (54) | 43 |
| Hersteller im Heizkesselkatalog | 5 (Vaillant 57, Buderus 2, Bosch 2, Viessmann 1, ohne 1) |
| `Tab_WP_STAMM` gesamt | 51 |
| davon `Quelle` enthält „Luft" (34) **und** `P_N 5..12` (31) | 24 |
| Quellenarten | Luft-Wasser 34, Sole-Wasser 14, Wasser-Wasser 1, ohne 2 |
| Hersteller im Wärmepumpenkatalog | 5 (Bosch 19, Wolf 15, STIEBEL ELTRON 9, „test" 5, MAX WEISHAUPT 3) |
| `Tab_WP_STAMM.Bauart` gepflegt | **6 von 51** (Split 5, Monoblock 1) — deshalb im Mockup keine Spalte |
| `Tab_WP_STAMM.Heizung` (el. Zuheizung) > 0 | **37 von 51**; Werte 0, 5, 6, 8, 9, 10, 12 kW |
| `Tab_WP_STAMM.Kuehlleistung` > 0 → Spalte „Kühlen = ja" | **15 von 51** (18 Sätze stehen auf 0, 18 sind leer) |
| VL min aus `Tab_Kenndaten_STAMM` | nur **zwei** Werte: 25 °C (10 Sätze), 35 °C (40), einer ohne Kennlinie |
| `Tab_Heizkessel_STAMM.Brennwert = 1` | **6 von 63** — und **keiner** davon ist unter den 15 Treffern des Mockups (Befund D‑1) |
| `Tab_Heizkessel_STAMM.Vorlauf` / `.Ruecklauf` gepflegt | **0 von 63** / **0 von 63** — deshalb keine Spalten |
| `Tab_Heizkessel_STAMM.Investitionskosten` ≠ 0 | **4 von 63** — deshalb keine Spalte |
| `T_NOCT` in `CEC Modules.csv` gepflegt | **20 743 von 20 743**; `A_c` ebenso — beide taugen als Spalte |
| `Tab_PV_STAMM` mit `Laenge = Breite = 0` | **1 von 6** (`Philadelphia Solar PS-M144(HCBF)-530W`) → Modulfläche als Halbgeviertstrich, nicht als 0 (W6‑E‑1) |
| `Tab_Kenndaten_STAMM` (Kennlinienpunkte) | 1 960 |
| `Tab_BHKW_STAMM` | 79, 9 Hersteller, 45 Motortypen |
| `Tab_Pufferspeicher_STAMM` | 13, 4 Hersteller, 3 Speichertypen |
| `Tab_Solarkollektoren_STAMM` | 7, 2 Typen (Flach 4, Röhre 2) |
| `Tab_PV_STAMM` | 6 |
| Zelltechnologien in `CEC Modules.csv` | Mono-c-Si 15 274, Multi-c-Si 5 177, Thin Film 157, CdTe 119, CIGS 16 |
| `CEC Modules.csv` Datenzeilen / Hersteller / Technologien | **20 743** / 258 / 5 |
| `CEC Inverters.csv` Geräte / Hersteller | 2 343 / 152 (gemessen 06.09.2026) |
| CEC Energy Storage System List | 6 654 Zeilen / 130 Hersteller (gemessen 07.09.2026) |
| `Tab_Stromspeicher_STAMM` | 5, 3 Chemien |
| `Tab_Brauchwasser_STAMM` / `Tab_Prozesswaerme_STAMM` / `Tab_Stromverbraucher_STAMM` | 16 / 32 / 41 |
| `Tab_Waermebedarf_STAMM` / `Tab_Stromganglinie_STAMM` / `Tab_Solarganglinie_STAMM` | 4 / 3 / 1 |

**Die drei Filterstände des Mockups** — je Reiter die drei Spaltenfilter und die Zahl, die
bleibt, wenn genau einer gelöscht wird. Die Bedienmechanik der HTML-Datei nimmt ihre Zahlen aus
dieser Tabelle; die Liste zeigt einen Auszug, der Zähler nennt die gemessene Wahrheit.

| Reiter | alle drei | ohne 1. Filter | ohne 2. Filter | ohne 3. Filter | gesamt |
|---|---|---|---|---|---|
| **M1** `Brennstoff` enthält „Gas" · `P_th` `10..60` · `η` `>=0,95` | **15** | 25 (ohne Brennstoff) | 22 (ohne P_th) | 43 (ohne η) | 63 |
| **M2** `Quelle` enthält „Luft" · `P_N` `5..12` · `VL max` `>=60` | **7** | 10 (ohne Quelle) | 11 (ohne P_N) | 24 (ohne VL max) | 51 |
| **M3** `Hersteller` enthält „LONGi" · `P_STC` `500..600` · `η` `>=21,5` | **15** | 789 (ohne Hersteller) | 31 (ohne P_STC) | 18 (ohne η) | 20 749 |

**Jeder Filter für sich**, damit man sieht, was der einzelne Ausdruck leistet:

| Reiter | 1. Filter allein | 2. Filter allein | 3. Filter allein |
|---|---|---|---|
| **M1** | Brennstoff enthält „Gas" → **52** | `P_th 10..60` → **54** | `η >=0,95` → **32** |
| **M2** | Quelle enthält „Luft" → **34** | `P_N 5..12` → **31** | `VL max >=60` → **18** |
| **M3** | Hersteller enthält „LONGi" → **252** | `P_STC 500..600` → **2 158** | `η >=21,5` → **2 172** |

**Wie die drei Spaltenwerte gebildet werden** (ohne diese Regeln sind die Zahlen nicht
nachrechenbar):

| Spalte | Bildung |
|---|---|
| **M1 `Brennstoff`** | der Name aus `Tab_Brennstoff_Stamm` zur Zahl in `Tab_Heizkessel_STAMM.Brennstoff`. „enthält Gas" trifft Stadtgas (4), Erdgas LL (3), Erdgas E (44), Flüssiggas Propan (1) — 52; nicht getroffen: Elektrische Energie (8), Heizöl EL (3) |
| **M1 `η`** | `Wirkungsgrad_Gas`, ersatzweise `Wirkungsgrad_Öl`; ein Wert **> 2** gilt als Prozent und wird durch 100 geteilt (D‑2, Q9). Ohne diese Weiche wären es 33 statt 32 Treffer |
| **M2 `VL max`** | `MAX(Vorlauf)` je `ID_WP` über `Tab_Kenndaten_STAMM` |
| **M2 `COP A2/W35`** | `COP` aus `Tab_Kenndaten_STAMM` bei `Vorlauf = 35` und `Temperatur = 2`; leer, wo der Punkt fehlt (Halbgeviertstrich, nicht 0 — W6‑E‑1) |
| **M2 `VL min`** | `MIN(Vorlauf)` je `ID_WP` über `Tab_Kenndaten_STAMM` |
| **M2 `Zuheizung`** | `Tab_WP_STAMM.Heizung` [kW] |
| **M2 `Kühlen`** | `Tab_WP_STAMM.Kuehlleistung > 0` → „ja"/„nein"; leer zählt als „nein" |
| **M1 `Brennwert`** | `Tab_Heizkessel_STAMM.Brennwert` → „ja"/„nein" |
| **M3 `η [%]`** | gerechnet: `STC / A_c / 10`. Die CEC-Liste führt keine Wirkungsgradspalte; `Tab_PV_STAMM.Wirkungsgrad` entsteht beim Import genauso |
| **M3 `A_Modul`** | die CEC-Spalte `A_c` [m²]; in `Tab_PV_STAMM` `Laenge × Breite`. `Length` und `Width` sind in der CEC-Liste **leer** — die Fläche kommt dort nur aus `A_c` |
| **M3 `T_NOCT`** | die CEC-Spalte `T_NOCT` [°C] |

**Die Maße des Mockups** — gemessen am 07.09.2026 mit Chromium (headless, `deviceScaleFactor 1`),
je Reiter ein PNG bei `1 366 × 768` und `1 920 × 1 080`:

| Größe | M1 | M2 | M3 |
|---|---|---|---|
| Parameterspalten (ohne „Wahl") | 6 | 9 | 7 |
| davon nur sortierbar, ohne Trichter | 1 (`Brennwert`) | 1 (`Kühlen`) | 0 |
| Rollt die Liste bei 1 366 px waagerecht? | 0 px | 0 px (Projektliste 0 px) | 0 px |
| Listenhöhe / Zeilen ohne Rollen | 458 px / **11** | 306 px / 7 (Projektliste 121 px / 4 möglich) | 458 px / **11** |
| Dialogbreite 1 366 px / 1 920 px | 1 303 / 1 400 px | 1 303 / 1 400 px | 1 303 / 1 400 px |
| Dialoghöhe 1 366 px / 1 920 px | 1 152 / 1 081 px | 1 228 / 1 132 px | 1 225 / 1 177 px |
| Eingabe- bzw. Detailblock beginnt (unter der Dialogoberkante) | 674 px | 825 px | 674 px |
| Seitenhöhe des Mockup-Blatts bei 1 366 px (mit Kopf, Reiterband und Fußnoten) | 1 851 px | 1 978 px | 1 906 px |
| Rollt die **Seite** waagerecht? | nein | nein | nein |

Kopfzelle 45 px, Datenzeile 37 px (der Wahlknopf misst 26 px, dazu 2 × 4 px Polsterung und die
Trennlinie); daraus die Listenhöhe von 1,3 × `--epos-listenhoehe` = 458 px für elf Zeilen und
12 rem = 192 px für die vier Zeilen der Projektliste.

## Anhang B — Was dieses Papier nicht behandelt

- **Den Rechenweg.** Kein Vorschlag berührt eine Simulations- oder Wirtschaftlichkeitsformel;
  die jeweils eingefrorene Referenzbasis bleibt in allen drei Stufen byte-gleich.
- **Die Dublettenpflege** der Hersteller- und Motortypschreibweisen (O‑3, O‑4) — sie hat mit
  `Konzept_Dublettenpruefung_Import_EPOS-Plan.md` ein eigenes Papier.
- **Die Kataloge außerhalb der drei Köpfe** (Gebäude, Gebäudetypen, Klimadaten, Kostenvorlagen,
  Energieträger, Gesetzesparameter) — siehe Q11.
- **Die Frage, ob ein Katalogparameter überhaupt gebraucht wird** (O‑1) — das beantwortet
  `ParameterVerwendung`, nicht ein Filter.
