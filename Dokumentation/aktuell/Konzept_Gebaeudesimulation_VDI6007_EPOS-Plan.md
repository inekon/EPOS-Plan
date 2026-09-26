# Konzept: Gebäudesimulation EPOS-Plan — dynamisches Gebäudemodell nach VDI 6007 und IFC-Import

**Rev. 3 (Prüfung 17.09.2026, E26 eingearbeitet) mit Nachtrag 1 —
Rev. 2 vom 16.09.2026: E20 und E23 im Hauptteil; Rev. 1 vom 15.09.2026: Prüfung, Prototyp und
Vorschlag**

Was diese Fassung ändert: Der Hauptteil folgt E26 (N1.31) — die Stufe **GA — Altweg ablösen**
steht wieder als letzte Stufe ohne Termin im Plan, Q24 und Q25 sind wieder offen, die
Schemaschritte heißen nach ihren Papiernamen (M3, M4) statt nach festen Nummern, und die
Rev.-1-Kapitel 4.4, 5.2 und 5.3 sind auf die Nachträge N1.2, N1.3, N1.14 und N1.15 nachgezogen.
Nachgezogen am 22.09.2026: Der Klimaspalten-Schritt M4 ist durch Schemaschritt 95 vorweggenommen
und steht als umgesetzt in 2.2, 2.3, 6.1, 11 und 12; der Aufwand der Anlagenkopplung folgt deren
Rev. 2 (Kapitel 13 Q26, Kapitel 15).
Nachgezogen am 22.09.2026 mit **E27** (N1.32): Q24, Q25 und Q26 sind entschieden — die Stufe GA
wird beauftragbar, sobald das Ablösekriterium aus Q24 erfüllt ist, ihr Umfang ist die Löschliste
(Q25), der Stufenplan der Anlagenkopplung folgt der Empfehlung (Q26); Kapitel 0, 4.1, 4.4, 6.1,
6.4, 10.4, 11, 13, 15 und 16 folgen.
Nachgezogen am 22.09.2026 mit **E28** (N1.33): U4 und U9 sind nach Empfehlung entschieden — der
Glossarabschnitt „Gebäudehülle und Gebäudemodell" entsteht vor den Übersetzungen (vor G1), die
Grenze von 100 Gebäuden im Bestandsweg fällt in GB; vor G0, GB und G1 ist kein Anwenderentscheid
mehr offen, das Register zählt 20 offene Punkte.
Nachgezogen am 23.09.2026 mit **E29** (N1.34): Die Endwahl zu U6 ist gefallen — das Gebäudemodell
rechnet die Sonnengeometrie auf den **Stundenanfang**, wie Photovoltaik und Solarthermie; eine
Umstellung auf die Stundenmitte gibt es nur für alle drei gemeinsam. Die Folgeaufgabe aus E27 zu
U6 ist damit erledigt.
Nachgezogen am 23.09.2026 mit **E30** (N1.35): Die Gebäudekennzahlen aus Kapitel 9 schreibt der
Lauf in die neue Ergebnistabelle `Tab_ErgebnisGebaeude` (Schemaschritt 107), der Bericht liest sie
— Abschnitt „Gebäude" in der Projektbeschreibung und der Produktausweis nach E10 im Berichtskopf
(A12); Kapitel 9 folgt.
Nachgezogen am 23.09.2026 mit **E31** (N1.36): K4, K5, K6, K7 und K12 der Kühlung sind nach
Empfehlung entschieden — die Kühlung steht zuletzt in der Knappheitsreihenfolge und ohne
Bedienelement, gerechnet wird sensible Kälte ohne Entfeuchtung mit der Grenze an jeder Kältezahl,
gleichzeitiges Heizen und Kühlen wird nicht saldiert, der Kältespeicher ist nach KU3 vertagt, und
die Kühlung gilt auch auf iOS ohne eigenen Lauf; vor KU1 ist kein Anwenderentscheid mehr offen,
das Register zählt 15 offene Punkte.
Nachgezogen am 23.09.2026 mit **E32** (N1.37): Ein Gebäude ohne wirksame Kühlung läuft frei — der
Löser kappt nicht mehr an `Maximaleraumtemperatur`, die Raumtemperatur darf darüber steigen, und
die Überhitzungsstunden zählen die Stunden darüber im freien Lauf; einen „informativen"
Kühlbedarf gibt es nicht mehr, Kühlreihe und Kühlkennzahlen nur bei wirksamer Kühlung. Kapitel 4.5
und 4.6 folgen.
Nachgezogen am 23.09.2026 mit **E33** (N1.38): K8, K21 und K23 der Kühlung sind nach Empfehlung
entschieden, **K9 abweichend** — der Kältestrom läuft per Vorgabe über den Stromträger und Tarif
des Heizbetriebs, wahlweise je Anlage über einen anderen Stromträger des Projekts, gewählt an der
Anlagenzeile; vor KU2 ist kein Anwenderentscheid mehr offen, das Register zählt 11 offene Punkte.
Nachgezogen am 23.09.2026 mit **E34** (N1.39), Ergänzung zu K9: Wählt eine Wärmepumpe für den
Kühlbetrieb einen anderen Stromträger als das Projekt, ist je Anlage wählbar, wie der Kältestrom in
Kosten und Emissionen eingeht — **anteilig am Netzbezug** (Vorgabe; PV-Eigenverbrauch gemeinsam,
Leistungspreis beim Projektträger) oder über einen **eigenen Zähler**; umgesetzt mit der dritten
Welle von KU2, das Register zählt weiter 11 offene Punkte.
Nachgezogen am 24.09.2026 mit **E35** (N1.40), Ergänzung zu E34: Ein **eigener Zähler** des
Kältestroms trägt zusätzlich **Grund- und Leistungspreis seines Stromträgers** — je Zähler (je Anlage)
einen Grundpreis und den Leistungspreis auf die eigene Spitze des Kältestroms der Anlage; anteilig am
Netzbezug bleibt es bei E34; umgesetzt mit der vierten Welle von KU2, das Register zählt weiter 11
offene Punkte.
Nachgezogen am 24.09.2026 mit **E36** (N1.41): Die Rechenzeit der Anlagenkopplung (N-A4) gilt nur
bei **wirksamer Kopplung** und lautet dann **höchstens 100 ms je Gebäude und Jahr** — gemessen 21 bis
31 ms; Gebäude ohne Kopplung bleiben beim Bestand. Nachgezogen mit der dritten Welle von AK1, das
Register zählt weiter 11 offene Punkte.
Nachgezogen am 25.09.2026 mit **E37** (N1.42): Die Kälteseite der Anlagenkopplung (H9) bekommt
**eigene Spalten für die Kühlübergabe** am Gebäude — Schalter `Kuehluebergabe_Aktiv`, Art (Kühldecke,
Flächenkühlung, Gebläsekonvektor), Exponent, Nennleistung, Auslegungspunkt und eine Vorlaufgrenze als
Vorgabe statt Taupunktrechnung —, fester Kaltwasser-Vorlauf, Nennleistung aus einem Auslegungstag;
wirksame Kopplung heißt nun „Heizseite oder Kälteseite wirksam". Umgesetzt mit der vierten Welle von
AK1, das Register zählt weiter 8 offene Punkte.
Nachgezogen am 24.09.2026 mit **E38** (N1.43): U13, U14 und U15 sind nach Empfehlung entschieden —
mehrere Gebäude einer Datei kommen eines je Lauf, die Wandfläche wird um Fenster und Außentüren
vermindert, ψ kommt als Vorgabe je Baualtersklasse, die Anschlusslängen bleiben leer, für gbXML und
IFC gleich; für G4a gibt es genau einen iOS-Lauf, nur nach ausdrücklicher Rückfrage bei der Abnahme.
Zugleich ist die Stufe G4 beauftragt (zuerst G4c, dann G4a); vor G4 ist kein Anwenderentscheid mehr
offen, das Register zählt 8 offene Punkte.
Nachgezogen am 25.09.2026 mit **E39** (N1.44), **E40** (N1.45) und den **Festlegungen der Umsetzung
G3** (N1.46): Der Baustoffkatalog führt neben 65 herstellerneutralen Stoffen 67 Herstellerprodukte mit
Quelle je Zeile; „Gebäude als eine Zone übernehmen" rechnet Bauteilflächen und ψ·L mit dem Faktor der
bisherigen Nachmultiplikation hoch, danach gilt die echte Hülle. Die **Stufe G3 ist abgeschlossen**;
Kapitel 4.3, 4.7, 6.3, 8.4, 11 und 12 folgen, das Register zählt weiter 8 offene Punkte.
Nachgezogen am 25.09.2026 mit **E42** (N1.47): Die Größengrenze des gbXML-Imports liegt auf iOS bei
**25 MB** wie unter Windows statt 10 MB — nach der Messung im iOS-Lauf zur Abnahme von G4a (rund
7,5 MB Prozessspeicher je MB Datei); die IFC-Grenzen bleiben; das Register zählt weiter 8 offene Punkte.
Nachgezogen am 25.09.2026 mit **E43** (N1.48): Der Gebäudeimport belegt fehlende Angaben mit ausgewiesenen,
änderbaren Vorgaben — innere Gewinne 5 W/m² × Nutzfläche, Tagsollwert 20 °C, Nachtabsenkung 18 °C —, und
Beginn und Ende der Nachtabsenkung sind je Gebäude einstellbar (leer = 22 bis 6 Uhr, Schemaschritt 144,
ergebnisneutral); das Register zählt weiter 8 offene Punkte.
Nachgezogen am 25.09.2026 mit **E44** und **E45** (N1.49): Die **Stufe G4b ist gebaut und abgeschlossen**
— nach E44 vor der Feldphase von G4a. Ein importiertes Gebäude kommt auf Wunsch als **eine Zone mit den
Bauteilen und Aufbauten der Datei** ins Projekt und rechnet den Bauteilweg; die innere Masse folgt der
Datenlage (Innenbauteile beider Seiten oder der Innenflächenfaktor aus der Datei), bei vollständigen
Schichten bleibt der U-Wert leer, Vorhangfassaden rechnen transparent (E45); kein Schemaschritt,
ergebnisneutral. Kapitel 4.3, 4.7, 6.3 und 7.6 folgen, das Register zählt weiter 8 offene Punkte.

Auftrag (Anwender, 15.09.2026, im Wortlaut):

> „Gebäudesimulation EPOS-Plan: Konzept und Umsetzung." — „Es soll geprüft werden, ob der
> Datenimport für das Gebäudemodell im IFC-Format verwendet werden kann und wie das in
> EPOS-Plan umgesetzt werden kann." — „Erstelle zuerst ein Konzept."

Leitplanken des Anwenders (15.09.2026): **Stundenwerte als Basis. Neuschreiben nach
Spezifikation, Datenmodell und Lösungsschema übernehmen. Prüfen der Simulationsdaten aus dem
bestehenden Gebäudemodell. IFC-Import prüfen.**

Grundlage: das Material unter `Z:\…\15-Anleitungen_Literatur\Simulation-Gebäudemodell`
(`vdi6007_gebaeudemodell.py`, `VDI6007_VisualStudio.zip`, `VDI6007_Dokumentation_DotNet.docx`,
`VDI_6007_Gebaeudemodell_Anleitung.docx`, `vdi6007_ergebnisse.png`); der Codestand des Zweigs
`ios_migration_september` vom 15.09.2026 (`SimulationWaermebedarf.cs`, `BhkwPlan.cs`,
`SolarPVGISCalculator.cs`, `KlimaImportAblauf.cs`, `sql/schema/001_grundschema.sql`,
Referenzbasis `2026-09-11_R7_Speicherflotte`); eine IFC-Recherche mit Quellen vom 15.09.2026;
eine Datenprüfung gegen die Testdatenbank (Kapitel 3) und ein außerhalb des Repositoriums
gebauter Prototyp, der gegen die zwölf Normtestfälle und gegen das heutige Modell gefahren
wurde (Kapitel 5). Den Rechenweg des Bestands beschreibt das überholte Papier
[`WP-Plan_Doku_Waermebedarf_Deckung_Pufferspeicher.md`](../ueberholt/WP-Plan_Doku_Waermebedarf_Deckung_Pufferspeicher.md)
(Codestand Juni 2026, damals noch über die native DLL); dieses Papier setzt darauf auf.
Bauform und Abnahme folgen dem Vorbild des PV-Zweigs
([`Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`](Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md),
Stufe E2: ein Persistenzwert entscheidet über den Rechenweg). Anders als dort **löst das neue
Modell das alte ab**: die beiden Wege liegen in getrennten Modulen, und der Tagesbilanz-Weg
bleibt als eingefrorener Bestandsweg im Produkt, bis die Stufe GA ihn ablöst
(E20, E23, E26).

Alle Zahlen dieses Papiers sind gerechnet, nicht geschätzt; Datei- und Zeilenbelege wurden
in einer zweiten Runde gegengelesen. Die Prüfwerkzeuge (Datenbankprobe, Prototyp, Adapter)
liegen außerhalb des Repositoriums und sind keine Auslieferung.

---

## 0. Das Ergebnis in acht Punkten

1. **Das Material auf Z: taugt als Vorlage, nicht als Rechenkern.** Sein Löser setzt die
   Systemmatrix bewusst diagonal an; dem Massenknoten der Außenbauteile fehlt die
   Rückkopplung, der Normtestfall 1 wird um rund 20 K verfehlt (Toleranz 0,1 K). Der Prototyp
   belegt es in Gegenrichtung: mit künstlich diagonaler Matrix versagt der sonst bestandene
   Löser in allen zwölf Testfällen um bis zu 304 K.
2. **Der neu geschriebene Löser besteht 35 der 36 Prüfungen im Normband nach E10** — elf der
   zwölf Testfälle liegen vollständig im Band aus beiden Programmspalten samt Druckrundung
   (5.2, N1.14, N1.15). Offen bleibt allein Testfall 11 (Kühldecke): zwei Umschaltstunden
   liegen 3,4 W bzw. 2,8 W neben dem Band. Er wird in G0 vor der Übernahme geklärt oder als
   benannte Grenze ausgewiesen — das ist der einzige offene technische Punkt des Vorschlags.
   Der Löser ist reines C# ohne Abhängigkeit, 984 Zeilen, und wird in Stufe G0 in den Kern
   übertragen.
3. **Der Bestand rechnet ein Tageslast-Modell mit einer Kapazität** auf Tagesmittel-Klima und
   verteilt die Tageslast über Tagesprofile auf Stunden. Die Datenbank trägt die
   **Bilanzgrößen** (Flächen, U-Werte, Gesamtkapazität, Nutzung, Sollwerte); neun weitere
   Größen sind Vorgaben, keine Daten (3.8). Die Klimadaten liegen stündlich vor, lückenlos,
   mit Global-, Direkt- und Diffusstrahlung.
4. **Der Prototyp läuft aus den vorhandenen Feldern** auf allen zwölf Projekten mit Gebäude
   und trifft die Katalogkennzahl kWh/m²a zu 86–100 % (das heutige Modell 48–88 %). Er liegt
   je Projekt 7–33 % über dem Tagesmodell. Die versteckten Kalibrierfaktoren
   0,83/0,95/0,45/0,83 im Bestand und der rohe g-Wert erklären diese Abweichung vollständig
   und darüber hinaus (+21 bis +42 Prozentpunkte); die Modellstruktur wirkt gegenläufig und
   senkt den Bedarf bei gleichen Randbedingungen um 5 bis 13 %. Die Abweichung ist also
   Parametrierung, nicht Physik. Auf Tagesebene stimmen beide Modelle mit r = 0,98–0,997
   überein.
5. **Vorschlag: ein Ein-Zonen-Modell 7R2C nach VDI 6007-1 als vorgegebener Rechenweg
   für alle Gebäude.** Es löst die Tagesbilanz ab (E20, 16.09.2026). Diese wandert Zeichen
   für Zeichen in ein eigenes Modul `Altweg/`, bekommt keine neue Funktion mehr und bleibt
   für die Dauer des Übergangs als **Bestandsweg** je Gebäude wählbar (E23, E26). Eine
   Weiche am Eingang der Gebäudebedarfsrechnung ruft genau ein Modul; die Referenzbasis wird
   mit G1 neu eingefroren.
6. **Drei Datenbefunde sind unabhängig vom Modell zu beheben:** ein Gebäude der
   Testdatenbank trägt den stillen Rückfallwert `Bauweise = 50 Wh/K` (drückt das Tagesmodell
   um 34 %), der Rechenweg teilt ungeschützt durch `Wohnflaeche`, und der Raumtemperatur-
   Zustand des Bestands ist ein statisches Feld — das Ergebnis hängt an der Zeilenreihenfolge.
7. **IFC-Import ist machbar** — mit reinem C# im Kern (xBIM Essentials, net10.0, CDDL-1.0)
   ohne Geometriekernel, über einen Zuordnungsdialog mit Vorgaben je Baualtersklasse. IFC
   liefert Rohmaterial wechselnder Qualität, nie fertige Modelldaten; für den Wohnbestand ist
   die Ausbeute gering. gbXML ist eine spätere Ergänzung.
8. **Reihenfolge:** G0 Löser mit Normtests im Kern → GB Bestandsbefunde → G1 Trennung der
   Rechenwege und Anbindung des VDI-Modells → G2 Ergebnisdarstellung → G3 Bauteilkatalog mit
   Schichtaufbau → G4 IFC-Import → G5 Geometrieableitung und gbXML (bei Bedarf). Letzte
   Stufe ist **GA — Altweg ablösen**; sie wird beauftragbar, sobald das Ablösekriterium erfüllt
   ist (Q24, E27), ihre 5–8 PT stecken in keiner Summe (E23, E26).

---

## 1. Befund A — das Material auf Z:

### 1.1 Was vorliegt

| Datei | Inhalt | Urteil |
|---|---|---|
| `vdi6007_gebaeudemodell.py` (1 211 Zeilen) | Einzonenmodell 7R2C, Bauteilreduktion per Kettenmatrix (Periode 24 h), Zeitschritt 3 600 s, Gauß-Seidel für die masselosen Knoten, Testfall 1 und ein Winterszenario, matplotlib-Bild | Struktur brauchbar, Löser falsch (1.2), keine Tests |
| `VDI6007_VisualStudio.zip` (net8.0, ein Exe-Projekt, keine NuGet-Abhängigkeit) | dieselbe Physik als C# (`WallLayer`, `BuildingElement`, `RcParameters`, `ChainMatrixReducer`, `ThermalZoneModel`, `ZoneInputs`/`ZoneOutputs`), plus `PhiSolar`-Eingang und drei Testräume | 1:1 portiert samt Fehler; kein Testprojekt |
| `VDI6007_Dokumentation_DotNet.docx` | zwölf Kapitel: Blätter 1–3 der Richtlinie, die sieben Widerstände und zwei Kapazitäten, Knotenbilanzen, Zustandsraum, Diskretisierung, **die zwölf Normtestfälle** mit Toleranz ±0,1 K / ±1 W, Standardwerte 2,7 / 25 / 5 W/(m²K), Vergleich mit ISO 13790 und ISO 52016 | **die brauchbarste Datei — die Spezifikation** |
| `VDI_6007_Gebaeudemodell_Anleitung.docx` | allgemeine Programmieranleitung mit anderem Netzwerk (Kettentopologie R3–C2–R2–C1–R1) und erfundenen Testfällen „TF 1–5"; Blatt 3 falsch als Heiz-/Kühlsysteme bezeichnet | nicht Spezifikation, nicht Bedienungsanleitung |
| `vdi6007_ergebnisse.png` | Verlauf des Testfalls 1 und des Szenarios | dokumentiert den Fehlerzustand |

Beide Docx sind maschinell erzeugt (Autor „Un-named", 07.02.2026) und nicht redaktionell
geprüft. Die in der Docx als „Referenzwerte VDI 6007 Testraum 1, Bauweise S" genannten
RC-Werte (R_1,IW = 0,000596 K/W, C_1,IW = 14 836 000 J/K, R_1,AW = 0,00437 K/W,
R_Rest,AW = 0,04277 K/W, C_1,AW = 1 600 800 J/K) sind die Parameter des AixLib-Testfalls 1
(5.3) — der Code des Materials reproduziert sie nicht.

### 1.2 Der Strukturfehler

Das 7R2C-Modell hat zwei Zustandsgrößen (die Temperaturen der Massenknoten θ_m,AW und
θ_m,IW) und drei algebraische Knoten (Oberfläche AW, Oberfläche IW, Raumluft). Der
Massenknoten der Außenbauteile hängt an **zwei** Widerständen: R_Rest,AW nach außen und
R_1,AW zur Innenoberfläche. Das Material setzt die Systemmatrix **bewusst diagonal**
(`a12 = a21 = 0`) und lässt den Term `+G_1,AW · θ_s,AW / C_1,AW` in der Zustandsgleichung weg.
Der Massenknoten hat damit nur noch eine Quelle und läuft stationär auf

```
θ_m,AW,∞ = θ_eq · R_1,AW / (R_1,AW + R_Rest,AW)
```

statt auf θ_eq. Mit den vom Code selbst erzeugten Parametern (R_1,AW = 0,00734 K/W,
R_Rest,AW = 0,169 K/W) ergibt das bei θ_eq = 22 °C einen Stationärwert von **0,9 °C** — die
Außenwand wird zur künstlichen Kältesenke, die Energiebilanz ist nicht geschlossen.
Nachrechnung des Testfalls 1 (schwere Bauweise, 1 000 W konvektiv 6–18 Uhr, θ_a = 22 °C
konstant): Tag 60 max/min **10,7 / 4,4 °C** statt der Sollwerte um 28 / 26 °C. Die Werte decken
sich mit dem mitgelieferten Bild. Der Prototyp (5.2) zeigt dieselbe Signatur, wenn man seine
Matrix künstlich diagonalisiert: der Nebendiagonalterm ist im Testfall 1 rund 70 % des
Diagonalterms — kein Korrekturglied, sondern der dominierende Beitrag.

Dazu kommen: die Lastbestimmung schätzt Φ_hc linear ohne Iteration und verfehlt den Sollwert
um rund 2 K (im Bild: Sollwert 20 °C, erreicht 22 °C); die Bauteilreduktion liest R_1 und C_1
aus `Z = a12/a22` der Kettenmatrix und trifft die AixLib-Werte des Normtestraums nicht
(R_Rest,AW um +295 %, R_1,IW um +117 %).

### 1.3 Was fehlt

Kein Fenstermodell (kein U_w, kein g-Wert, kein Rahmenanteil), kein Strahlungsmodell (die
äquivalente Außentemperatur wird von außen als Skalar hereingereicht, nur im Demo hart
verdrahtet), keine Orientierungen, keine Erdreichkopplung, keine Nachbarzonen, kein
Klimadatenformat (beide Fassungen erzeugen Sinusprofile im Code), kein 8760-Raster, keine
Datei-Ausgabe, keine Tests, keine Trennung Bibliothek/Konsole (`Console.WriteLine` im Kern,
`print` mitten in der Reduktion).

### 1.4 Lizenz

Die README des C#-Projekts sagt nur „Dieses Projekt dient Lehr- und Studienzwecken". DlEs.darf.anwendung in epos-plan finden.

### 1.5 Was übernommen wird

| übernommen | neu |
|---|---|
| Datenmodell Schicht → Bauteil → Bauteilart (Außen/Innen) → Zone, deutsch benannt (`Bauteilschicht`, `Bauteil`, `Bauteilart`, `ErsatzparameterRC`, `Zonenrandbedingungen`, `Zonenergebnis`, `Zonenmodell`) | Systemmatrix voll besetzt, algebraische Elimination der masselosen Knoten |
| Lösungsschema: exakte Diskretisierung für stückweise konstante Eingänge, Zeitschritt 1 h | Bauteilreduktion nach Norm mit Nachweis gegen die Normwerte |
| Kettenmatrix-Mechanik (γ = ξ(1+j), cosh/sinh, `System.Numerics.Complex`) als Baustein | Regelung: exakte Lastbestimmung, Leistungsgrenze, Sollwertfahrplan |
| Standardwerte h_conv = 2,7, h_a = 25, h_rad = 5 W/(m²K) | Fenster, Solar je Orientierung, Erdreich, Klimaschnittstelle 8760, Vorlauf |
| die zwölf Testfälle der Docx als Prüfliste | Testprojekt mit Assert gegen die Normtoleranz |

---

## 2. Befund B — der Rechenweg des Bestands

### 2.1 Der Weg heute

Einstieg ist `SimulationWaermebedarf.Waermebedarf_berechnen(ID_Projekt, ID_Klimaregion)`
(`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:128`). Je Gebäude ruft
`HeizwaermeEinesGebaeudes` (`:566-611`) die Tagesrechnung `Berechnung_Gebaeude_Tageswerte`
(`:684-888`), die für jeden der 365 Tage drei aus `BHKWPLAN.DLL` portierte Physikfunktionen
aus `EPOS.Kern/Allgemein/BhkwPlan.cs` zieht:

- `SolareGewinneC` (`:311-318`): `(E_N·A_N + ((E_O+E_W)/2)·A_OW + E_S·A_S)·g` — **eine**
  gemeinsame Ost/West-Fensterfläche, Tageswerte der Strahlung, der g-Wert roh (`:316`).
- `SpezWaermeverlusteC` (`:342-362`, Gewichte im Code `:347-353`): Transmission
  `0,83·U_W·A_W + U_F·A_F + 0,95·U_D·A_D + 0,45·U_G·A_G + U_S·A_S`, Wärmebrücken
  `(ψ₁L₁+ψ₂L₂+ψ₃L₃)·0,83`, Lüftung `f·(A_Wohn·h)·1,2·n·0,2778` (im Code `0.2777…`, also
  1/3,6 — zusammen 0,3333 Wh/(m³K)) mit `f = 1 + 0,025·θ_a` für θ_a < 0 (`:355-359`).
- `TaeglHeizlastWG` (`:388-436`): 24-Stunden-Schleife mit Sollwertfahrplan (7–22 Uhr Tag,
  sonst Nacht, Wochenend- und Ferienabsenkung), Heizlast
  `(T_prev − θ_a)·L + (T_soll − T_prev)·C − Q_i` (`:418`), solare Entlastung `4·Q_sol` nur in
  den Stunden 9–14 (`:420`, `:428`), Fortschreibung `T = e^(−L/C)·T_prev + (1 − e^(−L/C))·(…)/L`
  (`:426`), Kappung auf `Maximaleraumtemperatur` (`:430`), Rückgabe
  `acc · gesamtflaeche / wohnflaeche` (`:435`). **`C` ist `Bauweise`** — die einzige
  thermische Masse.

Die Tageslast wird über ein 24-h-Tagesprofil je Tagtyp (`Abfrage_Tagverteilung`, 120 bzw.
192 Werte; `BhkwPlan.StdWerte` `:259-289`) auf Stunden verteilt; die Weiche ist allein
`Typ == "Wohngebaeude  VDI 2067"` (zwei Leerzeichen, `SimulationWaermebedarf.cs:601-608`),
das Feld `Wohngebaeude_Nicht_Wohngebaeude` steuert nur die Katalogauswahl
(`GebaeudeCtrl.cs:23`, `GebaeudeStammCtrl.cs:83-84`; ein Treffer in der Testdatenbank,
Gebäude 10632) und geht in keine Rechnung ein. Die Gebäudereihe entsteht in Watt und wird
einmal nach kW gebracht (`:222`); der Kanal `HEIZUNG` führt kW je
Stunde, `waermebedarf_gebaeude.csv` des Referenzlaufs dagegen den ungewandelten
Gebäudevektor in W (`Referenzlauf/Ergebnisexport.cs:59`), `aggregate.csv` MWh und kW.
Externe Lastgänge, Prozesswärme und Brauchwasser gehen in ihre Kanäle
(`Kanal.HEIZUNG/BRAUCHWASSER/PROZESS`, `SimulationKanaele.cs:426-472`); `Waermebedarf` ist die
Kanalsumme (`SimulationWaermebedarf.SummenvektorAusKanaelen`, `SimulationWaermebedarf.cs:439`). **Ein Gebäudemodell ersetzt einen Summanden
des Kanals `HEIZUNG`** — Projekt 1041 der Testdatenbank ist der Mischfall (59,35 MWh Gebäude
plus 65,43 MWh externe Ganglinie im selben Kanal), Projekt 1030 hat gar kein Gebäude.

Vier Eigenheiten, die das neue Modell nicht erben darf:

1. **Statischer Zustand.** `_prevRoomTemp` ist ein statisches Feld (`BhkwPlan.cs:51`), wird
   nur bei `day == 1` auf die Nachtabsenkung gesetzt (`:398`) und sonst über alle Aufrufe
   fortgeschrieben (`:433`); `ResetState()` (`:54`) ruft die Produktion nirgends. Gebäude i+1
   startet seinen Vorlauf (Tage 350–364, `SimulationWaermebedarf.cs:748-814`) mit der
   Endtemperatur von Gebäude i — das Projektergebnis hängt an der Zeilenreihenfolge.
2. **Keine Nullprüfung.** `:435` teilt durch `wohnflaeche`, `SimulationWaermebedarf.cs:571`
   und `:653` durch `Flaeche_Nutzer`, `:651` durch das Rechenergebnis `VerbrauchAlt`; eine 0
   ergibt NaN oder Unendlich ohne Meldung. Der Dialog ist
   abgesichert (`Gebaeudebauweise.cs:44`), der Rechenweg nicht.
3. **Zwei Zeitbasen.** Die Tageswerte kommen roh in UTC-Tagen (`KlimadatenCtrl.cs:37`), die
   Stundentemperatur für Wärmepumpe und Erdreich ortszeitkorrigiert
   (`SolardatenCtrl.ReadOrtszeit`, `:156-208`; dokumentiert in
   `SimulationWaermebedarf.cs:903-911`).
4. **Namensfalle.** Die Spalte heißt `Fensterflaeche_Ost_West`
   (`001_grundschema.sql:1144`), das Modellfeld `Fensterflaeche_Ost`
   (`EPOS.Kern/Model/ProjektGebaeudeModel.cs:26`, im Laufweg gefüllt aus Spalte 14 der
   Sicht in `ProjektGebaeudeCtrl.cs:56`, im Katalogweg in `GebaeudeCtrl.cs:66`); der
   Aufruf `SolareGewinneC(…, item.Fensterflaeche_Ost, …)` (`:826`) meint die Summenfläche.

Im ganzen Kern gibt es weder Gradtagzahl noch Heizgrenz- noch Normaußentemperatur; die
Skalierung vom Katalog- auf das Projektgebäude ist eine reine Nachmultiplikation des
Ergebnisses mit `Z_AuswahlWohnflaeche / Wohnflaeche` (`:435`), die Verbrauchs-Rückrechnung
(`Bewohner_und_Flaeche_berechnen`, `:613-656`) bestimmt daraus eine fiktive Fläche.
`GebaeudeBedarfCtrl.Rechnen` (`EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs:94-136`) ruft für
**ein** Gebäude dieselben Methoden wie der Lauf — Regel „Eine Auskunft ruft den Rechenweg
des Laufs".

### 2.2 Datenmodell Gebäude

`Tab_Gebaeude` (`sql/schema/001_grundschema.sql:1131-1188`, STRICT) ist die Projektkopie des
Katalogs `Tab_Gebaeude_STAMM`; die Kopie zieht `GebaeudeStammCtrl.CopyFromStamm` (`:439`).
Sie führt:

| Gruppe | Spalten | für VDI 6007 |
|---|---|---|
| Hülle | `Flaeche_Außenwand`, `gesamte_Fensterflaeche`, `Dachflaeche`, `Grundflaeche`, `Sonstige_Flaechen`; `k_Wert_Außenwand/Fenster/Dachflaeche/Grundflaeche/Sonstiges` | Außenbauteile und Fenster: vollständig für den Klassenweg (4.3) |
| Wärmebrücken | `WBVK_*` mit `Abmessung_*` (drei Paare ψ·L) | masseloser Zusatzleitwert |
| Fenster | `Fensterflaeche_Sued`, `Fensterflaeche_Ost_West`, `Fensterflaeche_Nord`, `Fensterdurchlassgrad` | **Ost und West nur gemeinsam** — zu trennen (6.1) |
| Masse | `Bauweise` = Wohnfläche × 20 / 50 / 100 Wh/(m²K) (`EPOS.Kern/Allgemein/Gebaeudebauweise.cs:22-67`, Rückfall `50` bei Index außerhalb 0–2, `:66`) | Gesamtkapazität; Aufteilung Außen/Innen fehlt |
| Nutzung | `Interne_Waermegewinne` (W), `Luftwechselrate` (1/h), `Raumhoehe` (m), `Wohnflaeche`, `Wohnflaeche_gesamt`, `Bewohner`, `Flaeche_Nutzer` (m²/Person) | vorhanden; Einheiten in 3.3 belegt |
| Sollwerte | `Raumsolltemperatur_Tag/Nachtabsenkung/Wochenende/Ferien`, `Maximaleraumtemperatur`, `Wochenende`, `Ferien`, `Ferienbeginn/-ende_1…4` (Tag des Jahres) | Sollwertfahrplan übernommen; Ferienzeiträume einheitlich nach Tagesindex, Werte über 365 benannt abgelehnt (3.3) |
| Klassen | `Baualtersklasse`, `Gebaeudeart`, `Wohngebaeude_Nicht_Wohngebaeude`, `Typ` | Vorgaben je Klasse (IFC, 7.6); **kein Baujahr als Zahl** |
| Verbrauch | `WW_Bedarf`, `spez_Waermeverbrauch` (kWh/m²a), `Waermebedarf` (kW); `Z_ProjektGebaeude.Wohnflaeche_Waermebedarf`, `Einheit_Waermebedarf_Wohnflaeche`, `Jahresnutzungsgrad` (`:2873-2881`) | Skalierung bleibt (4.7) |

Nicht vorhanden: Neigungen, freie Azimute, Verschattung, Rahmenanteil, Innenbauteile,
Schichtaufbauten, Absorptionsgrade, Randbedingung der Grundfläche (Erdreich, Keller,
Außenluft), eine Heizleistungsgrenze. `Tab_Projekt` führt nur `ID_Klimaregion`;
`energy_project_settings` trägt keine Gebäudedaten.

**Falle für jeden Schemaschritt:** `ProjektGebaeudeCtrl.ReadAll`
(`EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:26-104`) liest die Sicht
`Abfrage_Projektgebaeude` (`sql/schema/002_views.sql:89-91`, feste Spaltenliste, endet auf
`Tab_Gebaeude.ID`) **nach Spaltenindex** `row[0]…row[57]`. Neue Spalten erreichen den Leser
nur über die Sicht: der Gebäudespalten-Schritt (Papiername **M3**, 6.1) muss **beides** tun —
die Sicht neu aufbauen (`DROP
VIEW` + `CREATE VIEW`, neue Spalten hinter `Tab_Gebaeude.ID`; SQLite kennt kein `ALTER VIEW`)
**und** den Leser auf Namenszugriff umstellen (wie `GebaeudeCtrl.MapRowToModel`), damit
künftige Spalten die Zuordnung nicht mehr still verschieben (6.2).

**Nummern stehen in keinem Papier.** Die Schemaschritte dieses Vorhabens tragen Papiernamen
(M3 Gebäudespalten, M4 Klimaspalten); ihre Nummer wird bei der Beauftragung an
`SchemaStand.Zielversion` abgelesen — Stand 22.09.2026: **100**, nächste freie **101**
(`EPOS.Kern/Allgemein/Update/SchemaStand.cs:341`). Der Klimaspalten-Schritt M4 ist bereits
umgesetzt: Schemaschritt 95 hat ihn vorweggenommen (2.3, 6.1). Reihenfolge
für einen neuen Schritt: Konstante → Methode → `SCHRITTE`-Eintrag in
`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` → `Zielversion`;
Spaltendefinitionen in `EPOS.Kern/Allgemein/Update/SchemaKatalog.cs` (Muster `Schritt70_*`),
Rahmen [`ADR-001`](ADR-001_Schema-Ausrollung.md).

### 2.3 Klimadaten

Quelle ist PVGIS-TMY (`EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:100`), Ablauf in
`EPOS.Kern/Allgemein/Import/KlimaImportAblauf.cs`. `Tab_Solar` (`001_grundschema.sql:2153-2182`)
führt **8 760 Zeilen je Klimaregion** mit `Temperatur` (°C), `Globalstrahlung` (GHI),
`Direktstrahlung` (**DNI, normal**), `Diffusstrahlung` (DHI), `Sonnenwinkel` (Höhenwinkel,
Grad) und `Sol_Nord/Ost/Sued/West` (W/m² auf senkrechter Fassade); keine Zeitspalte,
Reihenfolge `ORDER BY ID` = UTC, die Ortszeit-Korrektur sitzt beim Lesen
(`SolarZeitbasis.cs`, `SolardatenCtrl.ReadOrtszeit`: ganze Zeilen um 1 h MEZ bzw. 2 h MESZ
verschoben, EU-Regel, Warnung bei ≠ 8 760 Zeilen). Die vier Fassadenwerte entstehen beim
Import über `SolarCalculator.CalculateHourly` (Neigung 90°, Azimut Süd 0, Ost −90, Nord 180,
West 90; `KlimaImportAblauf.cs:132-136`, `:305-338`) mit **isotroper** Transposition und
Albedo 0,2. Daneben gibt es `Sonnengeometrie` (`SolarPVGISCalculator.cs:353-384`) und
**Hay-Davies** (`CalculateHourlyHayDavies`, `SolarPVGISCalculator.cs:455-482`); Perez nicht. `Tab_Klimadaten` (365 Tage) trägt
24-h-Mittel derselben Größen (`GetDailyAverages`, `:494-502`; `Sonnenwinkel` als
Tagesmaximum), `WE` (Sa/So, `KlimaImportAblauf.cs:354`), `TagTyp_W` (2 = trüber Tag, Diffus >
½ Global, `:355`), `TagTyp_NW` (Quartal × Werktag/Wochenende).

**Klimaspalten (Papiername M4) — umgesetzt.** Schemaschritt 95 (Anwenderentscheid 19.09.2026,
Aufträge KL-3/KL-4) hat den Klimaspalten-Schritt der Gebäudesimulation vorweggenommen:
`Tab_Solar` und `Tab_Solar_STAMM` führen `Gegenstrahlung` (W/m²), `Luftfeuchte` (%) und
`Bedeckungsgrad` (Achtel), `Tab_Klimaregion` und `Tab_Klimaregion_STAMM` die Herkunft `Quelle` und
`Importdatum`; Schritt 97 ergänzt `Szenario` und `Bezugsjahr` der Klimaregion. Der Import schreibt
die Klimagrößen aus PVGIS (`IR(h)`, `RH`) bzw. aus dem DWD-Testreferenzjahr, der zweiten
Importquelle (`A`, `RF`, `N`); PVGIS liefert keinen Bedeckungsgrad. Im Altbestand stehen alle
Spalten auf NULL, bis die Region neu importiert wird; NULL heißt „nicht verfügbar" bzw.
„Altbestand", nie 0. Eine Spalte `Windgeschwindigkeit` gibt es nicht — kein Rechenweg liest sie.
Quellen, Bedienweg und Zuordnung beschreibt das
[Konzept Klimadatenquellen](Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md).

**Folge für das Konzept:** Das Gebäudemodell braucht **keine neue Klimaquelle**. Stündliche
Außentemperatur, GHI/DNI/DHI und Sonnengeometrie liegen vor; das Modell rechnet die
Fassaden- und Fensterstrahlung je Orientierung und Neigung selbst mit Hay-Davies (die
isotropen `Sol_*`-Spalten bleiben dem Altweg; 3.4 zeigt, dass sie für Nord zu hoch sind).

### 2.4 Ergebnisse und Referenzlauf

Ergebnistabellen: `Tab_Ergebnis` mit `Tab_ErgebnisEnergiebedarf` (`Waermebedarf_Gesamt`,
`Waermelast_Max`, `Waermebedarf_Heizung/Brauchwasser/Prozess`; `:850-863`), geschrieben aus
`SimulationLaufCtrl.ErgebnisSpeichern` (`:210`) über `ErgebnisCtrl`. Anzeige über
`SimulationErgebnisCtrl.Bedarf` (`:813-828`), Hülle `EPOS.UI.Daten/Bedarf/BedarfErgebnisHuelle.cs`,
Seite `EPOS.UI/Seiten/Simulation/SimulationErgebnisSeite.razor`; Regeln in
[`Doku_Simulationsergebnis_Darstellung.md`](Doku_Simulationsergebnis_Darstellung.md).

Der Referenzlauf vergleicht je Projekt `aggregate.csv` und die Vektordateien aus
`Referenzlauf/Ergebnisexport.cs:58-67` — darunter **`waermebedarf_gebaeude.csv`**, die ein
neues Gebäudemodell unmittelbar trifft. Toleranz `Referenzlauf/Vergleich.cs:43-44`: relativ
1e‑4 ab Betrag 1, sonst absolut 0,01, je Skalar und je Vektorelement. Der heutige Lauf aller
dreizehn Projekte samt Anlagensimulation dauert 4 s (`protokoll.txt` der Basis). Die
Verschiebung des Tagesbilanz-Wegs in sein eigenes Modul ist ergebnisneutral und wird
**byte-gleich** gegen die Basis abgenommen, bevor der VDI-Weg angebunden wird; mit der
Umstellung der Referenzprojekte auf VDI 6007 wird die Basis neu eingefroren (E20,
16.09.2026; 10.4).

### 2.5 Andockpunkte

1. `SimulationWaermebedarf` wird zur **Fassade**: ein modellfreier Vorbereitungsschritt
   liefert, was beide Wege brauchen (Bewohnerzahl aus der Nutzfläche, Skalierungsfaktor,
   Klimareihen), dann liest **eine Weiche am Eingang** den Rechenweg des Gebäudes und ruft
   genau ein Modul (Muster `SimulationPV.cs:700-709`: ein Textwert entscheidet). Der heutige
   Rumpf um `HeizwaermeEinesGebaeudes` (`:566`) wandert dabei vollständig in den Altweg;
   einen zweiten Verzweigungspunkt gibt es nicht (E20, 16.09.2026).
2. Ein Persistenzwert in `EPOS.Kern/Allgemein/DbWerte.cs` (Muster
   `PV_MODELL_EINFACH/ERWEITERT`, `:2173/2180`) plus Spalte an `Tab_Gebaeude` und `_STAMM`.
3. Dialoge `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor` (Gruppe „Verbrauch" ab `:153`),
   `GebaeudeKatalogDialog.razor`, `GebaeudeBedarfDialog.razor` (`:117`).
4. Die Gebäudehülle sitzt **heute in der Windows-Schale**
   (`WindowsFormsApplication1/Views/Gebäude/GebaeudeHuelle.cs:322-350`); `EPOS.UI.Daten` hat
   für Gebäude noch keine Hülle. Eine neue Naht gehört nach `EPOS.UI.Daten`.
5. Texte in `EPOS.Kern/MyResource/*.resx` beider Sprachen, danach `Werkzeuge/ResourceDesigner`.

### 2.6 Grenzen des Tagesmodells — warum es abgelöst wird

Das Tagesmodell kennt eine Kapazität, verteilt Solargewinne pauschal auf 9–14 Uhr mit dem
Faktor 4 (das erhält die Tagesenergie, aber als Rechteck: die Mittagsstunden bekommen mehr,
als sie nutzen können, die Randstunden nichts), kappt die Raumtemperatur hart und verliert
die Energie, kennt weder Strahlungs- noch Oberflächenknoten, keine Aufheizspitze nach
Absenkung als Folge der Bauteilmasse, keine Kühllast, keine operative Temperatur und keine
Ost/West-Trennung. Bei kleiner Kapazität bricht es zusammen (5.11). Für Jahresmengen ist es
brauchbar und über die Verbrauchs-Rückrechnung kalibrierbar; für Spitzenlast,
Absenkverhalten, sommerliche Überhitzung, Fensterorientierung, Bauteilaufbauten und die
Kopplung an Vorlauftemperaturen und Wärmepumpenfahrpläne braucht es ein stündliches,
physikalisch geschlossenes Modell. Dafür ist VDI 6007-1 die Referenz — und die Datenlage im
Bestand trägt es (Kapitel 3 und 5).

---

## 3. Prüfung der Bestandsdaten (gemessen)

Geprüft am 15.09.2026 gegen `Referenzlaeufe/Kenndaten_Test.sqlite` (nur lesend,
`Mode=ReadOnly`) und die Basis `2026-09-11_R7_Speicherflotte` mit einer Konsolenprobe
(`Microsoft.Data.Sqlite` 10.0.11 aus `Directory.Packages.props:20`) außerhalb des
Repositoriums. Die Leseart ist nachgewiesen: die Probe rechnet die drei Physikfunktionen des
Bestands nach und trifft die Jahressumme von `waermebedarf_gebaeude.csv` jedes Projekts auf
unter 0,01 % (1007: 53 072 nachgerechnet gegen 53 071,74 kWh).

### 3.1 Umfang

Dreizehn Referenzprojekte, **15 Gebäudezeilen**, effektiv **acht verschiedene
Katalogbauten** (1007/1046, 1023/1024/1039 und 1040/1041/1042/1045 teilen Zeilen). Projekt **1030 hat
kein Gebäude** (6 137,56 MWh rein aus Ganglinie), Projekt **1041 ist ein Mischfall** (Gebäude
plus externe Ganglinie im Heizkanal). Zwei Klimaorte: Stuttgart (elf Regionen, je Projekt
eine Kopie) und München (zwei); `Klimazone_DIN4710` ist in allen dreizehn Regionen NULL
oder 0. Alle 15 Zeilen nehmen den Flächenweg (`Einheit_Waermebedarf_Wohnflaeche =
„Wohnfläche [m²]"`); der Verbrauchsweg `Bewohner_und_Flaeche_berechnen` wird in der
Referenz nie betreten. Textwerte der 15 Zeilen: `Typ` = „Wohngebaeude  VDI 2067" (10×),
„Wohnblock" (4), „Hotel" (1); `Baualtersklasse` A (9), F (3), D (1), G (1), H (1);
`Gebaeudeart` Einfamilienhaus (9), großes Mehrfamilienhaus (4), Mehrfamilienhaus (1),
Hotel (1). Projekt
1007 heißt in der Basis „Laurentiuskirche", trägt aber `Typ` Wohngebäude und `Gebaeudeart`
Einfamilienhaus — die Typangaben sind nicht belastbar.

### 3.2 Vollständigkeit (15 Zeilen)

| Feldgruppe | gefüllt | Befund |
|---|---|---|
| Geometrie (Wohnfläche, Außenwand, Fenster, Dach, Grund, Raumhöhe), U-Werte AW/Fenster/Dach/Grund, Fenster Süd und Ost/West, Bauweise, innere Gewinne, Luftwechsel, g-Wert, Wärmebrücken ψ, Sollwerte Tag/Nacht/Max | 15/15 | vollständig |
| `k_Wert_Sonstiges` · `Sonstige_Flaechen` | 7/15 | sonst 0 — zulässig |
| `Fensterflaeche_Nord` | 12/15 | sonst 0 |
| `Abmessung_Anschluß_Außenwand_Kellerdecke` | 11/15 | sonst 0 |
| `Waermebedarf` (kW), `spez_Waermeverbrauch` | 11/15 | Katalogkennzahlen |
| Wochenend-/Ferien-Sollwerte und -Flags, Ferienzeiträume | 0/15 wirksam | `Ferienbeginn_1` überall 366 (= aus), Flags 0 |
| `Wohnflaeche` = `Wohnflaeche_gesamt` | 15/15 gleich | das Paar trägt keine Information |
| `Luftwechselrate` | 15/15 = 0,7 | Einheitswert, keine Differenzierung |
| `Z_ProjektGebaeude.Jahresnutzungsgrad` | 15/15 = 1 | — |

### 3.3 Einheiten und Semantik (mit Codebeleg)

| Feld | Einheit / Bedeutung | Beleg |
|---|---|---|
| `Interne_Waermegewinne` | **W**, Leistung des ganzen Katalogbaus, zeitlich konstant | `pHzg = … − innereGewinne` mit L in W/K, `BhkwPlan.cs:418` |
| `Bauweise` | **Wh/K**; spezifisch 20 / 50 / 100 Wh/(m²K) für leicht / schwer / sehr schwer | `a = 1 − exp(−L/C)` bei Stundenschritt, `:426`; `Gebaeudebauweise.cs:24-32` |
| `Luftwechselrate` | 1/h | `V·1,2·n·0,2778` = W/K, `:355-359` |
| `Fensterdurchlassgrad` | g-Wert 0–1, roh (kein F_F, F_S, F_W) | `:316`; Daten 0,59–0,75 |
| `Ferienbeginn_n` / `Ferienende_n` | Tag des Jahres. **Zeitraum 1** ist der Jahreswechselblock (Beginn…365 **und** 0…Ende, ohne `−1`-Versatz); nur sein Beginn ist mit `≤ 365` geriegelt, `366` heißt dort „aus". **Zeiträume 2–4** laufen `Beginn−1 … Ende` und prüfen nur `> 0`. Ein `Ferienende_n = 366` greift in allen vier Zeiträumen über das Feld `bool[365]` hinaus. Das VDI-6007-Modell bildet den Fahrplan einheitlich nach Tagesindex 1…365 ab; **0 und 366 heißen dort „aus"** und werden still übergangen, benannt abgelehnt wird allein ein Tag außerhalb 1…365 in einem **aktiven** Fahrplan (4.4, 4.8) — an dieser Stelle bewusst nicht zeichengleich zum Bestand | `SimulationWaermebedarf.cs:59`, `:700-732` |
| `Ferien`, `Wochenende` | Flags; Ferien wirksam ab 0,9 und `Raumsolltemperatur_Ferien ≥ 1`; WE nur bei `Raumsolltemperatur_Wochenende > 5` | `:689-699`, `:777-780` |
| `k_Wert_*` | W/(m²K), im Bestand mit festen Gewichten 0,83 / 1,0 / 0,95 / 0,45 / 1,0 | `:347-353` |
| `WBVK_*`, `Abmessung_*` | ψ in W/(mK), Länge in m, Summe × 0,83 | `:353` |
| `Wohnflaeche` | m², Nenner der Skalierung; `Wohnflaeche_gesamt` Basis für Bewohner und Verbrauchsweg | `:435`; `SimulationWaermebedarf.cs:641-648` |
| `Flaeche_Nutzer` | m²/Person (`Bewohner = Fläche / Flaeche_Nutzer`) | `:571` |
| `spez_Waermeverbrauch`, `Waermebedarf` | kWh/(m²a) bzw. kW — Katalogkennzahlen, keine Rechnungseingänge | — |
| `WW_Bedarf` | kWh/a, im Lauf ohne Wirkung | `AbweichungsErmittler.cs:128` |
| `Tab_Solar.Sol_*` | W/m² auf senkrechter Fassade, stündlich, isotrop | `KlimaImportAblauf.cs:132-136`, `:305-338` |
| `Tab_Klimadaten.*` | 24-h-Mittel; `Sonnenwinkel` Tagesmaximum; `WE` 0/1; `TagTyp_W` 1/2; `TagTyp_NW` 1–8 | `SolarPVGISCalculator.cs:494-502`; `KlimaImportAblauf.cs:354-374` |

### 3.4 Plausibilität und Datenfehler

| Geb | Projekte | Wfl m² | U_AW | U_F | U_D | U_G | Bauweise Wh/K | spez. Wh/(m²K) | Q_i W/m² | g |
|---|---|---|---|---|---|---|---|---|---|---|
| 10614 | 1007, 1046 | 74 | 0,81 | 2,5 | 0,35 | 0,35 | 3 700 | 50 | 2,84 | 0,70 |
| 10576 | 1008 | 304 | 0,48 | 1,4 | 0,21 | 0,47 | **50** | **0,16** | 1,84 | 0,59 |
| 10577 | 1008 | 74 | 0,81 | 2,5 | 0,35 | 0,35 | 3 700 | 50 | 2,84 | 0,70 |
| 10599 | 1017 | 744 | 0,28 | 1,3 | 0,29 | 0,39 | 37 220 | 50 | 2,07 | 0,62 |
| 10632 | 1018 | 1 975 | 0,76 | 1,8 | 0,30 | 0,55 | 98 765 | 50 | 2,26 | 0,70 |
| 10628 | 1023, 1024, 1039 | 3 596 | 0,45 | 2,5 | 0,58 | 1,14 | 179 800 | 50 | 2,10 | 0,70 |
| 10642 | 1039 | 130 | 1,35 | 2,8 | 1,10 | 1,19 | 13 000 | 100 | 3,23 | 0,70 |
| 10643 | 1039 | 201 | 2,90 | 2,8 | 0,80 | 0,80 | 10 050 | 50 | 2,30 | 0,75 |
| 10645 | 1040/1041/1042/1045 | 201 | 1,84 | 2,8 | 0,80 | 0,80 | 10 050 | 50 | 2,30 | 0,75 |

- U-Werte 0,28–2,90 (Wand), 1,3–2,8 (Fenster), 0,21–1,10 (Dach), 0,35–1,19 (Grund) W/(m²K):
  Bestandsspanne, plausibel. `gesamte_Fensterflaeche` = Süd + Ost/West + Nord in allen
  15 Zeilen exakt. Innere Gewinne 1,84–3,23 W/m² am unteren Rand (DIN V 18599 Wohnen rund
  2,1). `Flaeche_Nutzer` 18–38 m²/Person, `Bewohner` konsistent.
- **Datenfehler 10576:** `Bauweise = 50 Wh/K` bei 304 m² ist der nackte Rückfallwert aus
  `Gebaeudebauweise.cs:66` (0,16 statt 50 Wh/(m²K)). Die Zeitkonstante des Tagesmodells
  bricht damit auf Minuten zusammen; das Tagesmodell verliert den Sollwertbezug und liefert
  54 statt rund 82 kWh/m²a (5.11). Nichts warnt.
- **Katalogkennzahl gegen Lauf:** das Tagesmodell erreicht `spez_Waermeverbrauch` nur zu
  48–88 % (10576: 54,1 gegen 112,5; 10614: 156,1 gegen 212; 10643: 394,9 gegen 451).
- `Sol_Nord` = 431,6 kWh/(m²a) (Stuttgart) ist für eine senkrechte Nordfassade zu hoch
  (Messwerte in Deutschland rund 300–350): isotropes Diffusmodell plus Albedo 0,2 ohne
  Horizontaufhellungsdefizit. Deshalb rechnet das Gebäudemodell die Fassadenstrahlung mit
  Hay-Davies selbst (4.4).

### 3.5 Klimadaten

`Tab_Solar` ist in allen dreizehn Regionen vollständig (8 760 Zeilen, kein NULL). Stuttgart:
Temperatur −18,2 … 33,5 °C, Mittel 9,88 °C; GHI 1 220,8 kWh/(m²a); DNI 1 203; DHI 574; die
isotropen Fassadenwerte Süd 935, Ost 777, West 711, Nord 432 kWh/(m²a). München: Mittel
9,58 °C, GHI 1 201,9 kWh/(m²a). Die Speicherstunde mit der größten Jahressumme
Globalstrahlung ist Index 11 — bei 9,18° Ost liegt der wahre Mittag bei 11:15 UTC, die
Ablage ist also UTC. Ost und West liegen stündlich getrennt vor (Jahresmittel 88,7 gegen
81,1 W/m²).

### 3.6 Skalierung je Projekt

| Projekt | Geb | Nenner `Wohnflaeche` | Zähler `Wohnflaeche_Waermebedarf` | Faktor |
|---|---|---|---|---|
| 1007, 1046 | 10614/10652 | 74 | 340 | **4,5946** |
| 1008 | 10576 | 304 | 800 | **2,6316** |
| 1008 | 10577 | 74 | 74 | 1 |
| 1017 | 10599 | 744,4 | 744 | 0,9995 |
| 1018 | 10632 | 1 975,3 | 500 | **0,2531** |
| 1023, 1024, 1039 | 10628/29/44 | 3 596 | 3 596 | 1 |
| 1039 | 10642, 10643 | 130, 201 | 130, 201 | 1 |
| 1040/1041/1042/1045 | 10645/46/47/51 | 201 | 201 | 1 |

Der Faktor ist eine reine Nachmultiplikation (Gegenprobe: 10577 × 4,5946 = 10614 auf
0,008 %). Bei 1007 wird eine 74-m²-Hülle mit 114 m² Außenwand auf 340 m² Wohnfläche
gedehnt, bei 1018 auf ein Viertel gestaucht. Für ein Ein-Kapazitäten-Modell ist das ein
Multiplikator; für ein Modell, in dem Flächen Kapazität und Strahlungsaustausch tragen, ist
es physikalisch unscharf — aber deterministisch und mit dem Bestand vergleichbar (4.7).

### 3.7 Referenzwerte je Projekt (Basis `2026-09-11_R7_Speicherflotte`)

| Projekt | wirksame Fläche m² | Gebäude-Jahresheizwärme kWh | kWh/(m²a) | Max kW | Speicherindex des Maximums |
|---|---|---|---|---|---|
| 1007, 1046 | 340 | 53 071,7 | 156,1 | 34,99 | 451 |
| 1008 | 874 | 54 817,8 | 62,7 | 37,82 | 1 362 |
| 1017 | 744 | 62 964,7 | 84,6 | 35,95 | 426 |
| 1018 | 500 | 46 881,4 | 93,8 | 34,10 | 8 371 |
| 1023, 1024 | 3 596 | 329 796,5 | 91,7 | 193,96 | 8 357 |
| 1030 | — | 0 | — | — | — |
| 1039 | 3 927 | 445 619,7 | 113,5 | 253,02 | 426 |
| 1040, 1041, 1042, 1045 | 201 | 59 354,1 | 295,3 | 37,29 | 1 387 |

Speicherindex 0-basiert in UTC-Reihenfolge (der Gebäudeweg wendet die Ortszeitregel nicht
an); Uhrzeiten werden hier bewusst nicht genannt. `waermebedarf_gebaeude.csv` steht in W,
`waermebedarf.csv` in kW, `aggregate.csv` in MWh (Last in kW).

### 3.8 Bewertung

**Klimadaten: vollständig ausreichend.** **Gebäudedaten: ausreichend zum Start, nicht zum
Abschluss.** Der Klassenweg (4.3) fährt aus den vorhandenen Feldern; jede der folgenden
Größen ist dabei eine Annahme mit Vorgabewert, kein Datum: Aufteilung der Kapazität in
Außen- und Innenbauteile, Innenbauteilfläche, R_1 aus U-Wert, Aufteilung Ost/West der
Fensterfläche, Rahmen-, Verschattungs- und Winkelfaktor, Absorptionsgrad, Randbedingung der
Grundfläche, Trennung Infiltration/Nutzerlüftung. Was davon die Jahresenergie bewegt und was
nicht, misst Kapitel 5.9; daraus folgen die Pflichtfelder in 6.1.

---

## 4. Zielbild und Rechenweg

### 4.1 Grundsatz: ein Rechenweg (VDI 6007), der Altweg als eingefrorener Bestandsweg

- **VDI 6007 ist der Rechenweg, die Tagesbilanz der Bestandsweg.** Das neue Modell löst das
  alte als Vorgabe ab (E20, 16.09.2026); die Tagesbilanz bleibt **jetzt** je Gebäude wählbar
  — als funktionierender Übergang, den der VDI-Weg später vollständig ablöst (E23, E26). Das
  Ende des Übergangs ist die Stufe **GA — Altweg ablösen** (Kapitel 11); sie wird
  beauftragbar, sobald die vier Bedingungen des Ablösekriteriums erfüllt sind (Q24, E27).
- **Ein Feld entscheidet:** `Tab_Gebaeude.Gebaeude_Modell` trägt den **Rechenweg** —
  NULL = VDI 6007 (Vorgabe), Wert `TAGESBILANZ` = Altweg, wählbar bis zur Ablösung —,
  Persistenzwerte `GEBAEUDE_MODELL_TAGESBILANZ` / `GEBAEUDE_MODELL_VDI6007` in `DbWerte`.
  Gelesen wird es an **einer** Stelle; alles Weitere (Skalierung, Kanal, Summen, Dauerlinie,
  Energieprobe, Mischfälle mit Ganglinien) bleibt unverändert.
- **Eine Weiche, zwei Module.** `SimulationWaermebedarf` ist die Fassade: ein **modellfreier
  Vorbereitungsschritt** stellt bereit, was **ohne** Modellauf feststeht — Klimakalender,
  Verbrauch und Flächen des Projekts —, dann ruft die Weiche genau ein Modul. Bewohnerzahl
  und Skalierungsfaktor nach E8 entstehen **je Modul** aus dessen erstem Lauf; die Schleife
  darüber führt die Fassade (E26). Der Klimakalender hat einen gemeinsamen und einen
  Altweg-Teil; das VDI-Modul bekommt nur den gemeinsamen. Der Vertrag des
  Vorbereitungsschritts steht an einer Stelle:
  [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 1.3. Es gibt
  keinen zweiten Verzweigungspunkt, und keiner der beiden Wege ruft den anderen.
- **Der Tagesbilanz-Weg wandert Zeichen für Zeichen in den Altweg.** Modul
  `EPOS.Kern/Allgemein/Simulation/Altweg/`, abgeschlossen, ohne neue Funktion, bis zur
  Ablösung nur noch Fehlerbehebung. Die Verschiebung ist ergebnisneutral und wird mit einem
  **byte-gleichen** Referenzlauf abgenommen, bevor der VDI-Weg angebunden wird. Ein
  Referenzprojekt rechnet danach auf VDI 6007, ein weiteres hält bis zur Stufe GA
  den Rückweg-Test; die Basis wird mit G1 neu eingefroren (10.4).
- **Der VDI-Weg ist eigenständig.** Er braucht nichts aus `Altweg/`; die
  **Modultrennungswache** prüft beide Richtungen und die Altweg-Datenquellen. Die
  **Ausbauprobe** hält die Eigenständigkeit nachweisbar: statisch nennt außer der Weiche, dem
  Rückweg-Test und der Wache keine Datei des Kerns `Altweg/`; als Gate der Stufe GA übersetzt
  ein Bau mit umbenanntem Ordner `Altweg/` nach Entfernen der Weiche, und der Referenzlauf
  aller Projekte ohne Altweg-Gebäude bleibt byte-gleich (E26).
- **Die Physik lebt in einer reinen Rechenklasse** ohne `DataRepository` und ohne
  `SimulationProtokoll` (Vorbild `EPOS.Kern/Allgemein/Simulation/PvErweitertesModell.cs`):
  Namensraum `EPOS.Kern/Allgemein/Simulation/Gebaeude/`, Klassen `Zonenmodell7R2C`
  (Löser), `ErsatzparameterRC` (Record), `Bauteilreduktion` (Kettenmatrix, G3),
  `GebaeudeModellEingang` (baut aus `ProjektGebaeudeModel` und den Klimareihen die
  Randbedingungen), `GebaeudeModellErgebnis` (Reihen und Kennzahlen); das Modul ruft
  **nichts** aus dem Altweg. `double` durchgehend
  (`DoubleWacheTests`), Einheiten am Feldnamen (`EinheitenWacheTests`), **Zustand je
  Instanz, nichts Statisches** (2.1, Punkt 1).
- **Eine Zone je Gebäude.** Mehrzonen sind Abgrenzung (Kapitel 15); ein Projekt mit
  mehreren Gebäuden rechnet je Gebäude eine Zone und summiert, wie heute.

### 4.2 Das Modell 7R2C

Knoten: zwei Massenknoten θ_m,AW (Außenbauteile) und θ_m,IW (Innenbauteile) mit den
Kapazitäten C_1,AW und C_1,IW; drei algebraische Knoten θ_s,AW, θ_s,IW (Innenoberflächen)
und θ_air (Raumluft, Luftkapazität null wie in den Normtestfällen). Widerstände:

| Widerstand | zwischen | Wert |
|---|---|---|
| R_Rest,AW | θ_eq,AW ↔ θ_m,AW | aus Reduktion (4.3), einschließlich äußerem Übergang 1/(h_a·A) |
| R_1,AW | θ_m,AW ↔ θ_s,AW | aus Reduktion |
| R_1,IW | θ_m,IW ↔ θ_s,IW | aus Reduktion |
| R_conv,AW | θ_s,AW ↔ θ_air | 1 / (h_conv · A_AW), h_conv = 2,7 W/(m²K) |
| R_conv,IW | θ_s,IW ↔ θ_air | 1 / (h_conv · A_IW) |
| R_rad | θ_s,AW ↔ θ_s,IW | 1 / (h_rad,i · A_rad), h_rad,i = 5 W/(m²K) (**innerer** Strahlungsaustausch), Bezugsfläche wie in den Testfällen |
| R_ext | θ_out ↔ θ_air | 1 / H_ext, **H_ext = H_ve + U_w·A_w + Σψ·L**, H_ve = n · V · 0,34 Wh/(m³K) — Fenster und Wärmebrücken laufen masselos im selben Zweig wie die Lüftung. **E14 (16.09.2026): die Fenster liegen nicht mehr hier, sondern im AW-Zweig** (R_1,AF = R_AF/6, Gl. (25)–(28), parallel nach den Wänden); R_ext trägt dann allein Lüftung und Wärmebrücken, H_ext = H_ve + Σψ·L (N1.19) |

Knotenbilanzen (Φ in W, Widerstände in K/W):

```
C_AW · dθ_m,AW/dt = (θ_eq − θ_m,AW)/R_Rest,AW + (θ_s,AW − θ_m,AW)/R_1,AW
C_IW · dθ_m,IW/dt = (θ_s,IW − θ_m,IW)/R_1,IW
0 = (θ_m,AW − θ_s,AW)/R_1,AW + (θ_air − θ_s,AW)/R_conv,AW + (θ_s,IW − θ_s,AW)/R_rad + Φ_rad,AW
0 = (θ_m,IW − θ_s,IW)/R_1,IW + (θ_air − θ_s,IW)/R_conv,IW + (θ_s,AW − θ_s,IW)/R_rad + Φ_rad,IW
0 = (θ_s,AW − θ_air)/R_conv,AW + (θ_s,IW − θ_air)/R_conv,IW + (θ_out − θ_air)/R_ext + Φ_conv + Φ_h
```

Der Luftknoten wird aufgelöst, die beiden Oberflächengleichungen bilden ein konstantes
2×2-System `M·θ_s = K·x + v`; Einsetzen in die Zustandsgleichungen ergibt `dx/dt = A·x + b`
mit **voll besetzter 2×2-Matrix A** (`A = C⁻¹(K·M⁻¹·K − diag(G_1+G_Rest, G_2))`). Der
Nebendiagonalterm verschwindet nur ohne Strahlungskopplung und ohne gemeinsamen Luftknoten
— genau das hatte das Material weggeworfen. Beide Eigenwerte sind reell und negativ
(Testfall 1: Zeitkonstanten 264 h und 5,3 h). Die exakte Diskretisierung für stückweise
konstante Eingänge, Δt = 3 600 s:

```
x(h) = Φ·x₀ + Γ·b,   Stundenmittel (1/h)·(Γ·x₀ + Ψ·b)
Φ = exp(A·h),   Γ = ∫₀ʰ exp(A·τ)dτ,   Ψ = ∫₀ʰ∫₀^τ exp(A·s)ds dτ
```

Die drei Matrixfunktionen entstehen geschlossen über die Sylvester-Formel aus den beiden
Eigenwerten (Reihenentwicklung nahe null gegen Auslöschung). Das ist unbedingt stabil,
deterministisch, und liefert **Endwert und Stundenmittel exakt** — das Stundenmittel ist
zwingend, weil die Normreferenz gleitende Stundenmittel vergleicht. Strahlungslasten (innere,
solare, Heizanteil) werden flächenproportional auf die Oberflächenknoten verteilt,
konvektive auf die Luft. Die operative Temperatur θ_op = 0,5·θ_air + 0,5·θ̄_s
(flächengewichtet) ist Kennzahl.

### 4.3 Bauteilreduktion — zwei Wege

**Klassenweg (G1, ohne neue Eingaben):** Die Datenbank kennt U-Werte, Flächen und die
Bauweise als Gesamtkapazität. Daraus:

```
C_ges     = Bauweise [Wh/K] · 3 600 J/Wh                      (die drei EPOS-Bauarten ergeben 72 / 180 / 360 kJ/(m²K) je m² Wohnfläche;
                                                                DIN EN ISO 13790 Tab. 12 kennt fünf Klassen 80 / 110 / 165 / 260 / 370 für
                                                                sehr leicht / leicht / mittel / schwer / sehr schwer — die EPOS-Namen sind um
                                                                eine Stufe verschoben: EPOS „leicht" = ISO sehr leicht, EPOS „schwer" = ISO mittel)
C_1,AW    = a_AW · C_ges,   C_1,IW = (1 − a_AW) · C_ges          (a_AW = Masseanteil_Aussen, Vorgabe 0,3)
A_AW,opak = Flaeche_Außenwand + Dachflaeche + Grundflaeche + Sonstige_Flaechen
R_1,AW    = 1 / (h_ms · A_AW,opak)                               (h_ms = 9,1 W/(m²K), DIN EN ISO 13790 12.2.2)
R_Rest,AW = 1 / Σ(U·A)_opak − R_1,AW − R_si / A_AW,opak          (R_si = 0,13 m²K/W: der innere Übergang steckt im U-Wert
                                                                und wird im Modell durch R_conv und R_rad ersetzt; wird der Ausdruck ≤ 0 —
                                                                rechnerisch ab mittlerem U über 4,17 W/(m²K) —, bricht die Rechnung mit
                                                                benanntem Fehler ab, keine Klemme; für alle Referenzgebäude bleibt er positiv,
                                                                Minimum 5,3·10⁻⁴ K/W bei 10643)
A_IW      = f_IW · Nutzflaeche                                    (f_IW = Innenflaechenfaktor, Vorgabe 2,5 — A_m/A_f nach ISO 13790;
                                                                Bezugsfläche ist die Nutzfläche (E13), die Spalte heißt nach M3 `Nutzflaeche` (E19))
R_1,IW    = 1 / (h_ms · A_IW)
```

**Warum der Abzug — und was er kostet.** Im Netz tritt an die Stelle von R_si nicht 1/R_si,
sondern R_innen,eff = R_conv,AW ∥ (R_conv,IW + R_rad): R_rad ist im 2-K-Netz kein
Luft-Oberflächen-Widerstand, sondern der Austausch zwischen den beiden Oberflächenknoten. Der
Klassenweg gibt die Katalog-U-Werte deshalb bewusst nicht wieder — für das Gebäude aus
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 9 sind es 607,48 statt
686,95 W/K, also 11,6 % weniger. Herleitung und Zahlen: Rechenschritte 2, Schritt A4.

**Die Gewichte 0,83 / 0,95 / 0,45 / 0,83 des Bestands entfallen.** Sie sind keine Physik,
sondern eine Kalibrierung der alten DLL; sie senken den Verlustkoeffizienten um 13–21 %
(5.10), und der ungewichtete Ansatz trifft die Katalogkennzahl besser (5.5). **Bewusste
Abweichung von ISO 13790:** dort hängt h_ms an der einen wirksamen Speicherfläche
A_m = 2,5·A_f; hier wird der Koeffizient auf beide Massepfade des 7R2C-Netzes angewandt.
Die Prüfung 5.9 zeigt: a_AW zwischen 0,2 und 0,5 und die Lage des Massenknotens (innen oder
Wandmitte) ändern die Jahresenergie um höchstens 0,1 % — die RC-Strukturparameter sind kein
Streitpunkt, die Randbedingungen sind es; für Leistungs- und Kühlgrenze löst G3 sie durch
den Bauteilweg ab.

**Bauteilweg (G3, mit Schichtaufbau; umgesetzt 25.09.2026, N1.46):** Je Bauteil Schichten
(d, λ, ρ, c) von innen nach außen; Reduktion nach VDI 6007-1 (Kettenmatrix im Frequenzbereich,
Bezugsperiode je Bauteil nach (10a)–(10d), Identifikation nach (12)–(17)), Aggregation mehrerer
Bauteile einer Gruppe über die **komplexen Widerstände** mit der Bezugsperiode des Raums
T_RA = 5 d (Gl. (19)–(24)) — nicht über ΣC und Σ1/R getrennt; Innenbauteile symmetrisch über den
vollständigen Aufbau. **Nachweis:** Die Richtlinie nennt keine Soll-RC-Werte; die Reduktion aus den
Bauteiltabellen der zwölf Testbeispiele trifft die Parameter der Testräume (Validierungsmodelle der
AixLib) relativ ≤ 10⁻³, und die Normfälle mit diesen Parametern bleiben 11 von 12 im Band
([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 10.3). **Wann er gilt:** Ein
Gebäude mit genau einer Zone rechnet über seine Bauteile (Datenlage, A14), ohne Zone den
Klassenweg; eine Gruppe ohne Schichten rechnet den Klassenweg aus den Bauteilsummen (Grenzfall,
bitgleich bis auf Rundung); zwei Zonen werden bis G6 benannt abgelehnt. Die eine Zone entsteht von
Hand, über „Gebäude als eine Zone übernehmen" oder aus dem Gebäudeimport (G4b, N1.49). Mit Zone sind die
Hüllzeilen des Dialogs abgeleitete Anzeigen aus den Bauteilen (Summenregel, Mehrzonenkonzept 4.3).

### 4.4 Randbedingungen

- **Außentemperatur** θ_out(h) aus `Tab_Solar.Temperatur` im Ortszeit-Lesepfad
  (`ReadOrtszeit`) — das Gebäudemodell rechnet in **einer** Zeitbasis (Ortszeit, wie PV und
  Solarthermie, Q21) und braucht `Tab_Klimadaten` nicht mehr. Die Wochenendmaske `WE[365]`
  bildet der modellfreie Vorbereitungsschritt aus dem Wochentag des 1. Januar des
  Referenzjahres der Zeitbasis (`SolardatenCtrl.Referenzjahr(idProjekt)`, `:222`); eine Probe
  hält sie gegen `Tab_Klimadaten.WE` derselben Region (`KlimaImportAblauf.cs:354`). Die Maske
  trägt dieser Ortszeit-Kalender; so ist **U7** mit E27 entschieden (N1.32).
- **Eine äquivalente Außentemperatur am AW-Massepfad**, U·A-gewichtet aus den Bauteilen
  (wie `T_EqAir` der Norm): `θ_eq = (Σ_opak U·A·θ_eq,k + U_G·A_G·θ_grund) / Σ(U·A)_opak`.
  Für Wand, Dach und Sonstiges gilt in G1 `θ_eq,k = θ_out` (Parität mit dem Bestand). Der
  Schalter `Aussenbauteile_Strahlung` (Vorgabe aus) setzt
  `θ_eq,k = θ_out + (α·I_k − F_r·ε·ΔE_r,k)/h_a` mit α = 0,6 und h_a = 25 W/(m²K) als
  **Summe** aus äußerem Konvektions- und Strahlungsübergang — dieselbe Formel, die 5.3 als
  `α·H_sol/(h_rad,a + h_conv,a)` schreibt — und dem langwelligen Verlust über den
  **geometrischen Sichtfaktor** φ = (1 + cos γ_F)/2 (senkrechte Wand 0,5, waagerechtes Dach
  1,0); die Bewölkung steckt allein in der Gegenstrahlung E_A der Klimadaten, und für
  transparente Flächen entfällt der kurzwellige Term (N1.3). Die Pauschale
  „+0,6·I/25 − 3 K" ist keine Physik und wird nicht übernommen. Ihre Wirkung von +3,2 bis
  +7,1 % (5.8) zeigt zugleich die Größenordnung des in G1 abgeschalteten Strahlungsterms:
  mit ihm träfe der Prototyp die Katalogkennzahl zu 91–99 % statt zu 86–95 %. Der Schalter
  ist deshalb in G1 vorzusehen und in G2 mit der Normformel zu füllen.
- **Grundfläche** nach `Grundflaeche_Randbedingung` (6.1): **Erdreich** (Vorgabe) mit
  Kusuda-Temperatur in 1 m Tiefe aus Jahresmittel und **erster Harmonischer** der
  Außenluft (nicht (max−min)/2 der Tagesmittel: das ergäbe 18,8 K Amplitude und −3,0 °C
  Erdreich; aus den Stundenextremen sogar 25,9 K);
  Temperaturleitfähigkeit 0,06 m²/d, Dämpfung 0,68, Phasenverzug 22 Tage — Stuttgart
  3,5 … 16,3 °C. **Unbeheizter Keller:** θ_NR aus der Spalte `Kellertemperatur` (Vorgabe
  10 °C, 6.1), angebunden als äquivalente Nachbarraumtemperatur θ_NR,eq — kein
  Reduktionsfaktor gegen θ_out (N1.3). **Außenluft:** θ_out. Der Unterschied zwischen Erdreich und dem Bestandsfaktor 0,45 beträgt
  2,3–14,1 % der Jahresenergie, streng nach Grundflächenanteil (5.8) — deshalb ein Feld.
- **Solar durch Fenster** je Orientierung o ∈ {N, O, S, W} (G3: je Bauteil mit Neigung):
  `Φ_sol = Σ_o A_w,o · g · F_F · F_S · F_W · I_o(h)`; `I_o` rechnet das Modell mit
  `CalculateHourlyHayDavies` aus GHI/DNI/DHI und Sonnengeometrie (die isotropen `Sol_*` sind
  für Nord rund 25 % zu hoch, 3.4). F_F = 1 − `Rahmenanteil` (Vorgabe 0,3, also 0,7; eine
  Vorgabe je Baualter setzt eine Tabelle `Baualtersklasse` → Baujahrspanne voraus, die es
  heute nicht gibt, 6.1), F_S = `Verschattungsfaktor` (Vorgabe 0,9 frei stehend, 0,8 Reihe,
  0,7 Innenstadt), F_W = 0,9 (Winkelkorrektur, ISO 13790). `Fensterflaeche_Ost` und `_West`
  getrennt (6.1); solange beide NULL sind, bildet der **Vorbereitungsschritt** je die Hälfte
  der Summenfläche — eine Abhängigkeit vom Bestandsfeld, die nur für den Übergang gilt (6.1).
  Eintrag radiativ auf die
  Oberflächen mit **9 % konvektiv an die Luft** — wie in den Normtestfällen und im Prototyp
  (5.3); die Verteilung auf AW und IW folgt `splitFacVal`. F_F·F_S = 0,63 gegen den
  rohen g-Wert des Bestands ist der zweitgrößte Einzelposten (5.10): beim größten
  Katalogbau sind die Solargewinne das 6,5-fache der inneren Gewinne.
- **Fenster (Transmission)** masselos mit U_w·A_w; **Wärmebrücken** Σψ·L masselos.
- **Lüftung** H_ve = `Luftwechselrate` · V · 0,34 Wh/(m³K), V = `Wohnflaeche` · `Raumhoehe`;
  ohne den Temperaturfaktor des Bestands (der rechnet mit 0,3333 Wh/(m³K),
  `BhkwPlan.cs:357/359` — der reine Zahlenwechsel hebt H_ve um 2 %). G2: Trennung Infiltration (Vorgabe 0,3 1/h) und
  Nutzerlüftung (0,4 1/h) sowie eine **Sommerlüftungsregel** (n auf 2,0 1/h, wenn
  θ_air > 23 °C und θ_out < θ_air − 2 K), ohne die das Modell 184–1 425 Stunden über 24 °C
  meldet (5.7).
- **Innere Gewinne** `Interne_Waermegewinne` (W), 50 % konvektiv / 50 % radiativ, in G1
  zeitlich konstant; ein Wochenprofil ist G2+.
- **Sollwerte** wie im Bestand: Stunden 7–22 `Raumsolltemperatur_Tag`, sonst
  `Nachtabsenkung` — die Nachtzeit ist je Gebäude einstellbar (`Nachtabsenkung_Beginn`/`_Ende`,
  leer = 22 bis 6 Uhr, also genau diese Stunden; E43, N1.48); an den Wochenendtagen der Maske `WE[365]`
  `Raumsolltemperatur_Wochenende`, sofern dieser Wert > 5 ist — die Spalte
  `Tab_Gebaeude.Wochenende` geht in **keine** Rechnung ein; in Ferienzeiträumen
  `Raumsolltemperatur_Ferien`, und Ferien haben Vorrang vor dem Wochenende. Der
  Ferienfahrplan wirkt nur, wenn er **aktiv** ist (`Ferien` > 0,9 und
  `Raumsolltemperatur_Ferien` ≥ 1); **0 und 366 heißen „aus"** und werden still übergangen,
  benannt abgelehnt wird allein ein Tag außerhalb 1…365 in einem aktiven Fahrplan (3.3,
  [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Schritt E).
  Obere Grenze `Maximaleraumtemperatur`.

### 4.5 Regelung, Heizen, Kühlen

- **Ideale Heizung, kontinuierlich:** die Lufttemperatur ist algebraisch, also hält die
  Regelung θ_air = θ_soll **zu jedem Zeitpunkt** der Stunde; die Leistung ist dann eine
  affine Funktion des Zustands, ihr Stundenmittel folgt exakt über Ψ. So vergleicht es die
  Norm; eine „Sollwert am Schrittende"-Variante ergäbe in den Lastfällen andere
  Stundenmittel. Bei Umschaltungen innerhalb der Stunde (Sollwertsprung, Übergang
  Heizen/frei) sucht der Löser den Umschaltzeitpunkt per Bisektion (bis zu 60 Schritte);
  das ist die einzige Iteration, und sie ist deterministisch.
- Φ_h ≥ 0, optional Φ_h ≤ `Heizleistung_Max` (NULL = unbegrenzt). Strahlungsanteil der
  Heizung `Heizung_Strahlungsanteil` (Vorgabe 0,3 für Heizkörper; 0 = rein konvektiv; der
  Prototyp lief mit 0).
- **Spitzenlast:** ohne Leistungsgrenze fällt die Jahresspitze in elf von zwölf
  Referenzprojekten auf dieselbe Stunde (Index 1 398) — die erste nach Ende der
  Nachtabsenkung am kältesten Tag; nur Projekt 1018 (München) weicht ab. Sie liegt je nach
  Projekt −10 bis +57 % neben der Spitze des Tagesmodells (über alle zwölf Projekte
  zusammen +29 %); als gleitendes Tagesmittel schrumpft der Abstand auf +18 % (5.6). Das ist
  Physik (Aufheizen nach Absenkung), aber kein Auslegungswert. `Waermelast_Max` bleibt
  unverändert das Maximum des Kanalsummenvektors des Projekts
  (`SimulationWaermebedarf.cs:401`), damit Dauerlinie, Deckung und Anzeige eine Basis
  behalten; das Modell führt **zusätzlich je Gebäude** drei Kennzahlen: Spitze (Stunde),
  Spitze als gleitendes Tagesmittel, 95-%-Quantil der Stundenlast (Q7).
- **Kühlung nur, wo sie wirksam ist (E12, KU1; E32, N1.37):** Mit Projektschalter „Kühlung
  rechnen", `Kuehlung_Aktiv` und Kühlsollwert regelt der Löser zusätzlich auf den Kühlsollwert,
  optional bis `Kuehlleistung_Max`; die dafür nötige Leistung ist die Reihe `Kuehlbedarf`, und sie
  geht in den vierten Kanal `KUEHLUNG` ([Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 3.2).
  **Ohne wirksame Kühlung läuft das Gebäude frei:** keine obere Grenze, keine abgeführte Wärme,
  keine Kühlreihe — die Raumtemperatur darf über `Maximaleraumtemperatur` steigen.
  `Maximaleraumtemperatur` ist allein die Grenze der Überhitzungskennzahl: Sie zählt, was ohne
  Anlage geschieht. Eine Kappung an ihr als „informativer" Kühlbedarf ist mit E32 entfallen.
- Zwischen Heiz- und Kühlsollwert liegt ein Totband (der Abstand zweier Sollwerte, keine
  Hysterese); keine Reglerdynamik: das Modell liefert den Bedarf, die Deckung rechnet
  `SimulationControl` wie heute.

### 4.6 Zeitraster, Vorlauf, Ergebnisreihen

Festes Raster 8 760 Stunden ohne Schaltjahr, in Ortszeit. **Vorlauf:** 30 Tage (die letzten
30 Tage des Jahres) vor dem 1. Januar, Ergebnisse verworfen — statt der 15 Tage des Bestands;
die gemessenen Zeitkonstanten der Referenzgebäude liegen bei 7,5–24,8 h (5.7), 30 Tage sind
rund das Dreißigfache. Für die **langsameren Normtesträume** (bis 264 h, 4.2) reicht eine
feste Vorlauflänge nicht: nach 720 h bleibt dort ein Rest, der nur bei kleiner Anfangsstörung
unter 0,1 K liegt ([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
7.2). Die Normtests in G0 laufen deshalb mit einem **deterministischen Abbruchkriterium**:
Vorlauf verlängern, bis der Zustandsunterschied zweier aufeinanderfolgender Vorlaufwochen
unter 0,01 K liegt, höchstens zwölf Wochen, danach benannter Fehler. Für Projektläufe bleiben
die 30 Tage fest. Ergebnis je Gebäude: `Heizlast[8760]` (W → kW, in `Kanal.HEIZUNG`),
`Raumtemperatur[8760]`, `OperativeTemperatur[8760]`, `Kuehlbedarf[8760]` (nur bei wirksamer
Kühlung, E32); Kennzahlen: Jahresheizwärme, die drei Spitzenwerte (4.5), Kühlenergie und Stunden
mit Kühlbedarf (nur bei wirksamer Kühlung), mittlere Raumtemperatur in der Heizzeit,
Überhitzungsstunden.

### 4.7 Skalierung und Verbrauchs-Rückrechnung

Die Rechnung läuft mit den Katalogdaten des Gebäudes; das Ergebnis wird — wie heute — mit
dem Faktor `Z_AuswahlWohnflaeche / Wohnflaeche` nachmultipliziert. **Diese Skalierung nach E8
ist ein Schritt des VDI-Moduls, kein Rückgriff auf den Bestand** (E20, 16.09.2026): Faktor,
Bewohnerzahl und Klimareihen liefert der modellfreie Vorbereitungsschritt vor der Weiche
(4.1), angewandt werden sie im VDI-Weg selbst; der Altweg wird dabei nicht gerufen. Damit
bleibt die Verbrauchs-Rückrechnung eine **Verhältnisrechnung** mit einem einzigen Kataloglauf
(`VerbrauchNeu / VerbrauchAlt`), unabhängig davon, dass das Modell selbst stückweise linear
ist (Nullstunden, Kappung, Leistungsgrenze). Das ist deterministisch und mit dem Bestand
vergleichbar; physikalisch sauberer ist die echte Hülle — die liefern der Bauteilkatalog (G3)
und der IFC-Import (G4), dann entfällt die Nachmultiplikation für diese Gebäude.

**Umgesetzt mit G3 (25.09.2026, E40, N1.45):** Ein Gebäude mit Zone rechnet mit der echten Hülle
und dem Faktor 1; eine Verbrauchs- oder Flächenangabe steht nur als Hinweis im Protokoll, die
Skalierungsangabe ist weich gesperrt. „Gebäude als eine Zone übernehmen" rechnet Bauteilflächen und
ψ·L mit dem Faktor der bisherigen Nachmultiplikation hoch, die Zone trägt die hochgerechnete
Nutzfläche, und die flächenbezogenen Größen folgen ihr über den Flächenschlüssel — das Ergebnis bleibt
beim Übernehmen gleich; Leistungsgrenzen werden nicht hochgerechnet. Der Import (G4) füllt die
Summenfelder des Klassenwegs und legt auf Wunsch eine Zone mit den Bauteilen und Aufbauten der Datei an
(G4b, N1.49); ein so übernommenes Gebäude rechnet von Beginn an mit der echten Hülle und dem Faktor 1,
seine Zone trägt die Nutzfläche der Datei.

### 4.8 Determinismus, Rechenzeit, Prüfungen

Reine 2×2-Arithmetik ohne Zufall; Zustand je Instanz; zwei Läufe liefern byte-gleiche
Reihen. Rechenzeit rund 5 ms je Gebäude und Jahr, Planungsgröße 10 ms (5.13); die dreizehn
Referenzprojekte bleiben damit unter 0,2 s gegenüber den 4 s des heutigen Gesamtlaufs. Harte
Prüfungen vor der Rechnung (benannte Fehler, kein stiller Rückfall — das ist **Hausregel**, keine
Regel der Richtlinie; ihr Programmierhinweis 6.8 verlangt nur, Divisionen durch null
auszuschließen, und die einzigen Setzwerte, die sie selbst vorschreibt, sind die Grenzfälle
(28a)–(28c) der Außenbauteilgruppe als Schutzregel für widersprüchliche Eingaben): `Nutzflaeche > 0`,
`Flaeche_Nutzer > 0`, `Raumhoehe > 0`, `VerbrauchAlt > 0` vor der Rückrechnung,
`5 ≤ Bauweise/Nutzflaeche ≤ 200 Wh/(m²K)`, 0 < g ≤ 1, U-Werte 0,1–6 W/(m²K), R_Rest,AW > 0
(4.3), Ferientage 1…365 in einem aktiven Fahrplan (4.4), Summe der Fensterflächen =
`gesamte_Fensterflaeche`. Es gelten
`DoubleWacheTests`, `EinheitenWacheTests`, `RechenrandTests`, `ParallelitaetWacheTests`;
keine Windows-API, keine NuGet-Abhängigkeit im Modell (`System.Numerics.Complex` für G3 ist
BCL).

---

## 5. Prototyp: Validierung und Vergleich (gemessen)

Der Prototyp ist ein Konsolenprojekt außerhalb des Repositoriums (net10.0, reines C#, kein
NuGet, 984 Zeilen in sieben Dateien: 2×2-Matrixfunktionen, Zonennetz, Eliminierung,
Zeitschritt mit Ereignisbehandlung, Testfall-Lader, Kommandozeile). Er ist der Vorläufer
der Stufe G0 und wird nicht als Ganzes übernommen, sondern nach Hausregeln in
`EPOS.Kern/Allgemein/Simulation/Gebaeude/` neu benannt und mit Tests eingebaut.

### 5.1 Herkunft und Lizenz der Referenzdaten

Die Referenzwerte der zwölf Testfälle (Parameter, Randbedingungen, Referenzreihen für
Tag 1, 10 und 60, je 72 Prüfpunkte) stammen aus den Validierungsmodellen
`AixLib.ThermalZones.ReducedOrder.Validation.VDI6007.TestCase1…12` (RWTH Aachen, E.ON ERC,
EBC; **überarbeitete 3-Klausel-BSD-Lizenz mit Zusatzabsatz zur Rückgabe von
Verbesserungen**, Wortlaut in `AixLib/UsersGuide/License.mo` — nicht identisch mit SPDX
`BSD-3-Clause`, deshalb ist der AixLib-Wortlaut selbst mitzuliefern; Vermerk „Copyright (c)
2010-2018, RWTH Aachen University, E.ON Energy Research Center, Institute for Energy
Efficient Buildings and Indoor Climate"). Die Tabellen dort geben die Normwerte der
VDI 6007 Blatt 1 wieder; die Richtlinie selbst lag nicht vor. **Offen bleibt, ob die
Zahlenwerte von dieser Lizenz gedeckt sind** — der VDI hält das Urheberrecht an den
Normtabellen. Vor der Abnahme ist die Richtlinie zu beziehen und die Zitierfähigkeit zu
klären, oder die Referenzreihen bleiben interne Prüfdaten und werden nicht ausgeliefert (Q2). Aus AixLib wurden außerdem die Formeln der äquivalenten Außentemperatur
(`PartialVDI6007.mo`, `VDI6007.mo`) und der Strahlungsverteilung (`splitFacVal.mo`) gelesen
und nachgebaut — kein Quelltext übernommen, aber die Herkunft ist zu nennen, und
Copyright-Vermerk sowie Haftungsausschluss der BSD-Lizenz gehören zu jedem Test, der die
Zahlen führt. Vom Material auf Z: stammt keine Zeile (1.4).

### 5.2 Validierung gegen die zwölf Normtestfälle

Geprüft wird nach der Regel der Richtlinie: Prüfgröße ist das **Stundenmittel**, es muss im
**Band zwischen den beiden Programmspalten** der Referenztabellen liegen, erweitert um die
Toleranz und — nach **E10** (N1.15) — um die halbe Druckstelle der gedruckten Tabellen.
Geprüft werden Lufttemperatur, operative Temperatur und Last an den Tagen 1, 10 und 60, je
24 Stunden; die Auszüge der Referenztabellen sind von zwei unabhängigen Lesern gewonnen und
gegeneinander gehalten worden (N1.14).

| Ergebnis gegen das Band nach E10 | Stand |
|---|---|
| Prüfungen Fall × Größe | **35 von 36 bestanden** |
| Fälle vollständig im Band | **11 von 12** |
| erst mit der Druckrundung im Band | Fall 6 (Last, 0,50 W), Fall 9 (0,011 K) und Fall 10 (0,060 K) |
| offen | **Testfall 11 (Kühldecke):** zwei Umschaltstunden 3,4 W bzw. 2,8 W neben dem Band (ohne Druckrundung 3,9 W bzw. 3,3 W) |

Testfall 11 ist damit der einzige substanzielle Befund: Der Umschaltzeitpunkt Heizen → Kühlen
liegt rund 45 s zu früh. Die wahrscheinlichste Ursache — das Ein-Knoten-Innenbauteil trennt
die Kühldecke nicht von den übrigen Innenflächen — ist diagnostisch belegt, nicht bewiesen;
G0 löst den Fall über getrennte Innenoberflächen oder weist ihn als bekannte Grenze aus
(N1.14, N1.15). Die frühere Bewertung dieses Kapitels ist damit ersetzt: Sie maß gegen **eine**
AixLib-Reihe statt gegen das Band aus beiden Programmspalten — daher galten die Fälle 9 und 10
als knapp — und ließ Fall 11 nur mit einem nachgebildeten Messfenster der Referenz bestehen,
was das Abnahmeverfahren ändert, nicht das Modell.

**Gegenprobe des Strukturfehlers:** mit künstlich diagonaler Systemmatrix versagt derselbe
Löser in allen Fällen — Testfall 1: 304 K, Testfall 6: 11 491 W, Testfall 12: 202 K.

### 5.3 Abbildung der Normfälle auf das Netz

R_Rest,AW enthält den äußeren Übergang (Testfall 1: 0,03896 + 0,00381 = 0,04277 K/W — der
Wert der Docx); radiative innere Lasten flächenproportional auf AW, Fenster und IW; die
Fenster-Solarverteilung nach `splitFacVal` (bei einer Orientierung geht der gesamte
Fenstersolareintrag auf die IW-Oberfläche); 9 % des Fenstersolareintrags konvektiv an die
Luft; θ_eq je Wand mit langwelligem Anteil `(T_Himmel − T_Luft)·h_rad/(h_rad + h_a)` und
kurzwelligem `H_sol·α/(h_rad + h_a)`; Sonnenschutz g = 0,15 ab 100 W/m²; Lüftung
Testfall 12 über Massenstrom. **Vorzeichen:** Heizen ist positiv, Kühlen negativ — die
Konvention der Richtlinie; allein die AixLib-Reihe von Testfall 6 ist gedreht und wird beim
Einlesen gespiegelt (N1.2, N1.3). Testfall 11 trägt danach beide Vorzeichen, weil er
innerhalb des Tages von Heizen auf Kühlen umschaltet. Die Luftkapazität (0,1 m³ nur in
Testfall 12) ist vernachlässigt
(Zeitkonstante 0,4 s).

### 5.4 Abbildung der Bestandsdaten (Klassenweg, wie in 4.3 und 4.4)

Umgesetzt wie vorgegeben; vier bewusste Festlegungen: Fenster und Wärmebrücken laufen als
masseloser Leitwert über denselben Zweig wie die Lüftung (algebraisch identisch); eine
U·A-gewichtete äquivalente Außentemperatur am einen AW-Massepfad; Strahlung nur auf AW und
IW normiert (kein Fensteroberflächenknoten); Kusuda mit harmonischer Amplitude. Die
Fassadenstrahlung kam aus den isotropen `Sol_*`-Spalten — der Hay-Davies-Weg aus 4.4 ist im
Prototyp **nicht** gemessen; seine Wirkung wird in G1 ausgewiesen.
Sollwertfenster 7–22 Uhr wie im Bestand; Wochenend- und Ferienlogik implementiert, in
allen 15 Zeilen wirkungslos (3.2); Heizung ideal, unbegrenzt, konvektiv; keine Kühlung,
freier Lauf über den Sollwert; Zeitreihen in Speicherreihenfolge (UTC) für **beide** Modelle,
damit die Ortszeitverschiebung aus dem Vergleich herausfällt; Vorlauf 30 Tage.

### 5.5 Jahresheizwärme je Projekt und Gebäude

Referenz „heute" ist `waermebedarf_gebaeude.csv` der Basis; je Gebäude nachgerechnet und
auf 0,001 % bestätigt.

| Projekt | Geb | m² | Prototyp kWh | heute kWh | Abw. | Prototyp kWh/(m²a) | heute | Katalog |
|---|---|---|---|---|---|---|---|---|
| 1007, 1046 | 10614 | 340 | 62 566 | 53 072 | +17,9 % | 184,0 | 156,1 | 212 |
| 1008 | 10576 | 800 | 90 216 | 43 267 | **+108,5 %** | 112,8 | 54,1 | 112,5 |
| 1008 | 10577 | 74 | 13 617 | 11 551 | +17,9 % | 184,0 | 156,1 | 212 |
| 1017 | 10599 | 744 | 83 751 | 62 965 | +33,0 % | 112,6 | 84,6 | 118 |
| 1018 | 10632 | 500 | 62 332 | 46 881 | +33,0 % | 124,7 | 93,8 | 136 |
| 1023, 1024 | 10628 | 3 596 | 412 781 | 329 797 | +25,2 % | 114,8 | 91,7 | 130 |
| 1039 | 10642 | 130 | 39 982 | 36 451 | +9,7 % | 307,6 | 280,4 | 338 |
| 1039 | 10643 | 201 | 77 705 | 79 373 | −2,1 % | 386,6 | 394,9 | 451 |
| 1040/1041/1042/1045 | 10645 | 201 | 63 677 | 59 354 | +7,3 % | 316,8 | 295,3 | — |

Der Prototyp liegt je Projekt 7–33 % über dem Tagesmodell (10576 ist der Datenfehler, 5.11;
das Einzelgebäude 10643 liegt 2 % darunter) und trifft die Katalogkennzahl
`spez_Waermeverbrauch` zu **86–100 %** (85,7 % bei 10643 bis 100,3 % bei der fehlerhaften
Zeile 10576; ohne sie 86–95 %), das Tagesmodell zu 48–88 %. Auf Tagesebene stimmen beide Modelle mit r = 0,982–0,997 überein (Stundenreihe
r = 0,74–0,93); der Bruch sitzt in der Stundenverteilung, nicht in der Bilanz.

**Die Vergleichszahlen dieses Abschnitts sind ohne den Abzug R_si/A in R_Rest,AW entstanden**
(4.3): der Prototyp rechnet ohne ihn, ebenso die 86–100 % der Katalogkennzahl. Mit dem Abzug
steigt der Jahresbedarf um rund 12 % —
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 9.6 misst am Gebäude 10645
+11,8 % aus dem Abzug allein und +12,9 % zusammen mit F_W = 0,9. Die Wirkung ist **je
Referenzgebäude auszuweisen, bevor die Basis neu eingefroren wird**.

### 5.6 Spitzenlast und Tagesprofil

| Projekt | Prototyp max kW | Index | heute max kW | Index | Prototyp Tagesmittel-Max kW | heute |
|---|---|---|---|---|---|---|
| 1007, 1046 | 39,66 | 1 398 | 34,99 | 451 | 25,31 | 21,44 |
| 1008 | 54,88 | 1 398 | 37,82 | 1 362 | 39,01 | 23,17 |
| 1017 | 56,45 | 1 398 | 35,95 | 426 | 32,57 | 26,82 |
| 1018 | 33,26 | 438 | 34,10 | 8 371 | 22,06 | 19,35 |
| 1023, 1024 | 276,60 | 1 398 | 193,96 | 8 357 | 171,33 | 144,11 |
| 1039 | 334,84 | 1 398 | 253,02 | 426 | 214,14 | 185,41 |
| 1040/1041/1042/1045 | 33,56 | 1 398 | 37,29 | 1 387 | 24,03 | 22,85 |

(Speicherindex 0-basiert, UTC.) In elf von zwölf Projekten liegt die Prototyp-Spitze auf
demselben Index — die erste Stunde nach Ende der Nachtabsenkung am kältesten Tag des
Stuttgarter Datensatzes: der ideale, unbegrenzte Heizer deckt den Sollwertsprung 18 → 20 °C
in einer Stunde. Das Tagesmodell kennt diese Spitze nicht, weil es die Tagessumme über eine
Verteilungstabelle streut; der heutige Spitzenlastvergleich vergleicht Physik mit einer
Tabelle. Die Stundenspitze liegt je Projekt −10 % (1040/1041/1042/1045) bis +57 % (1017)
neben der des Tagesmodells, über alle zwölf Projekte summiert +29 %; als gleitendes
Tagesmittel schrumpft der Abstand auf +18 %.

Jahressumme je Tagesstunde (UTC), Projekt 1045, kWh:

| Stunde | 0 | 3 | 6 | 7 | 8 | 12 | 18 | 20 | 23 |
|---|---|---|---|---|---|---|---|---|---|
| Prototyp | 2 376 | 2 880 | **4 389** | 3 910 | 3 379 | 2 132 | 2 489 | 2 819 | 2 165 |
| heute | 1 008 | 995 | 2 717 | 3 647 | 3 820 | 2 630 | 3 573 | 3 681 | 1 064 |

Die Maxima des Tagesmodells liegen bei Stunde 9 (3 833 kWh) und 19 (3 875 kWh), die der
Spaltenauswahl fehlen. Das Tagesmodell zeigt ein Zwei-Buckel-Profil (7–9 h und 18–21 h) mit gekappter Nacht; der
Prototyp eine Aufheizspitze um 6–7 h und eine physikalische Grundlast in der Nacht. Die
Nachtabsenkung steckt im Bestand nicht in der Physik, sondern im Verteilungsprofil. Zugleich
hat der Prototyp 2 092 Nullstunden gegen 936: das Tagesprofil schmiert an Übergangstagen
Last auf alle Stunden, der Prototyp läuft bei Solargewinn frei.

**Monatsmuster** (1007, kWh): Prototyp 11 314 / 12 140 / 9 238 / 2 161 / 1 403 / 1 141 / 24 /
611 / 955 / 5 758 / 7 483 / 10 339; heute 10 009 / 10 533 / 7 452 / 1 521 / 1 066 / 896 / 15 /
413 / 698 / 4 797 / 6 279 / 9 392. Die Abweichung ist im Hochwinter klein (bei 1007 +10
bis +24 %, bei 1040/1041/1042/1045 +1 bis +3 %; Ausnahme 1008 mit dem Datenfehler) und in
der Übergangszeit groß (+20 bis +180 %), wo solare und innere Gewinne die Bilanz
bestimmen; im Juli liefert das Tagesmodell bei 1017 und 1023/1024 null, dort ist der
relative Vergleich nicht definiert. Das Muster ist in allen Projekten dasselbe, die Höhe
nicht.

### 5.7 Raumtemperatur und Überhitzung

Bei `Maximaleraumtemperatur` = 24 °C in allen Zeilen, ohne Kühlung, ohne Sommerlüftung
(n = 0,7 konstant), ohne Sonnenschutz: Jahresmittel der Lufttemperatur 19,7–21,1 °C,
**184 bis 1 425 Stunden über 24 °C** (2–16 % des Jahres); Zeitkonstanten C/H 7,5–24,8 h,
bei 10576 0,09 h (Datenfehler). Das Tagesmodell kappt hier und wirft die Information weg.
Für G2 folgt daraus die Sommerlüftungsregel (4.4); die Überhitzungsstunden haben in EPOS
heute keinen Empfänger (kein Kühlkanal).

### 5.8 Varianten

| Variante | Wirkung auf die Jahresheizwärme |
|---|---|
| θ_eq = θ_out + 0,6·I/25 − 3 K (Pauschale) | **+3,2 bis +7,1 %** in allen Projekten — falsche Richtung; −3 K wirkt Tag und Nacht, +0,6·I/25 im Jahresmittel nur +1,4 K. Für G1 nicht zulässig; α und ΔE_r getrennt, Himmelsaustausch bewölkungsabhängig (4.4) |
| Erdreich: Bestandsfaktor 0,45 statt Kusuda | **−2,3 bis −14,1 %**, streng nach Anteil U_G·A_G an Σ(U·A)_opak (10 % bis 46 %). Nicht äquivalent: Kusuda schickt die volle Fläche gegen 3,5–16,3 °C, der Faktor 45 % der Fläche gegen −18 … +34 °C. Die Beschreibungen der Katalogbauten nennen alle drei Fälle (unbeheizter Keller, Kellerdecke, Gewölbe) — deshalb das Feld `Grundflaeche_Randbedingung` |

### 5.9 Empfindlichkeit (Projekt 1045, Gebäude 10645, Basis 63 677 kWh)

| Variante | Δ Jahresenergie | Δ Spitze | Stunden > 24 °C |
|---|---|---|---|
| C_1,AW / C_1,IW = 0,2 / 0,8 | −0,07 % | −0,5 % | 491 |
| C_1,AW / C_1,IW = 0,5 / 0,5 | +0,10 % | +0,4 % | 501 |
| Massenknoten in Wandmitte (R_1 = R_Rest) | +0,06 % | −1,0 % | 509 |
| F_S 0,7 statt 0,9 | **+2,67 %** | +0,1 % | 390 |
| ohne F_F·F_S (g roh wie Bestand) | **−6,43 %** | −0,1 % | 856 |
| Bestandsgewichte 0,83/0,95/0,45/0,83 und Lüftung mit 1/3 | **−12,97 %** | −7,5 % | 630 |
| beides (Bestands-Randbedingungen) | **−19,25 %** | −7,7 % | 1 082 |
| Erdreich 0,45 | −2,30 % | +0,9 % | 574 |

**Die RC-Strukturparameter sind für die Jahresenergie irrelevant (≤ 0,1 %)** und für die
Spitze fast (≤ 1 %): bei idealer, unbegrenzter Regelung bestimmt die Kapazitätsaufteilung
nur die Verteilung im Tag. Sie wird wichtig, sobald Leistungsgrenze, Speicherhysterese oder
Kühlgrenze dazukommen. **F_S schlägt mit 2,7 % je 0,2 durch**, F_F·F_S zusammen mit 6,4 %.

### 5.10 Zerlegung der Abweichung: Parametrierung gegen Modellstruktur

| Projekt | gesamt | davon ungewichtete U·A statt 0,83/0,95/0,45 | davon F_F·F_S = 0,63 statt roh | Rest = Modellstruktur |
|---|---|---|---|---|
| 1007, 1046 | +17,9 % | +16,1 | +10,9 | **−9,1** |
| 1017 | +33,0 % | +24,1 | +14,0 | −5,1 |
| 1018 | +33,0 % | +25,6 | +16,0 | −8,6 |
| 1023, 1024 | +25,2 % | +17,2 | +15,6 | −7,6 |
| 1039 | +19,0 % | +16,3 | +12,9 | −10,2 |
| 1040/1041/1042/1045 | +7,3 % | +13,9 | +6,8 | **−13,4** |

Der ungewichtete Ansatz liegt bei den acht Katalogbauten 14,9–26,4 % über dem gewichteten
(10614: 234,1 gegen 203,7 W/K; 10632: 4 440 gegen 3 636 W/K); umgekehrt gelesen senken die
Gewichte den Verlustkoeffizienten um 13,0–20,9 %. Bei gleichen Randbedingungen
liefert das 7R2C **5–13 % weniger** als das Tagesmodell — aus vier Gründen: der stündliche
Solareintrag statt des 6-Stunden-Rechtecks (die Randstunden werden verwertet, die
Mittagsübermenge nicht mehr verworfen), zwei Kapazitäten statt einer (die Innenbauteile
tragen den Solareintrag in den Abend), keine harte Kappung mit Energieverlust, und die
physikalische statt tabellarische Stundenverteilung.

### 5.11 Sonderfall 10576: der stille Rückfallwert

| | kWh/a | kWh/(m²a) |
|---|---|---|
| Tagesmodell mit `Bauweise = 50 Wh/K` (Ist) | 43 267 | 54,1 |
| Tagesmodell mit 15 200 Wh/K (korrigiert, 50 Wh/(m²K)) | 65 773 | 82,2 |
| Prototyp mit 50 Wh/K | 90 216 | 112,8 |
| Prototyp mit 15 200 Wh/K | 83 374 | 104,2 |
| Katalogkennzahl | — | 112,5 |

Das Tagesmodell reagiert auf die Korrektur mit +52 %, der Prototyp mit −7,6 %: bei C → 0
wird im Bestand `(T_soll − T_prev)·C` zu null und `1 − e^(−L/C)` zu eins, die
Fortschreibung verliert den Sollwertbezug und heizt gegen die eigene Vorstunde. Der
Prototyp ist gegen kleine Kapazitäten robust (Regelung am algebraischen Luftknoten). Eine
Plausibilitätsgrenze auf `Bauweise` (4.8) ist die billigste Verbesserung im ganzen Paket;
die Korrektur der Testdatenbank verändert das Referenzergebnis von Projekt 1008. Dafür gibt
es heute **keine** Einfrierregel — die drei bestehenden (`Referenzlaeufe/LIESMICH.md:57/79/100`)
decken Emissionsfaktoren, PV-Modulkoeffizienten und den Flottenstand 1046. G1 legt deshalb
mit dem Schritt GB (Kapitel 11) eine vierte Einfrierregel „gesäte Gebäudedaten" an (10.4, Q22).

### 5.12 Ost/West-Trennung — gemessen

Die Datenbank führt Ost und West als eine Summenfläche; der Prototyp teilt hälftig. Gemessen
wurde die volle Spanne — dieselbe Fläche ganz nach Ost bzw. ganz nach West — mit den
stündlich getrennten Strahlungsreihen (Ost liegt in der Jahressumme 7,3 % (München) bis 9,3 % (Stuttgart) über West,
Tagesmaximum Ost bei Index 8, West bei Index 14).

| Größe | Spanne 100 % Ost gegen 100 % West, bezogen auf den 50/50-Fall | 50/50 liegt |
|---|---|---|
| Jahresheizwärme | 0,7–2,5 % (1023: 408 677 gegen 418 831 kWh) | innerhalb 1 % der Mitte |
| Stundenspitze | ≤ 0,48 %; Speicherindex in allen Varianten gleich (Stunde ohne Einstrahlung) | — |
| Tagesmittel-Maximum | ≤ 1,0 % | — |
| Jahressumme der Morgenstunden (5–9 UTC) | −6,1 % bis +10,3 % | — |
| Jahressumme der Abendstunden (15–19 UTC) | −3,4 % bis +3,9 % | — |
| Stunden über 24 °C | −3,6 % bis +8,2 % | — |

**Die starke Behauptung „ohne Ost/West-Trennung ist die Morgen-/Abendspitze nicht
darstellbar" ist widerlegt.** Die Morgen-/Abendasymmetrie ist auch ohne Trennung da und
dominant (Morgensumme 1,12- bis 1,74-mal Abendsumme) — sie stammt aus Nachtabsenkung und
Temperaturgang, nicht aus der Fensterorientierung. Richtig bleibt die schwache Fassung: wer
Morgen- und Abendsummen auf besser als rund 5 % auflösen will, braucht die getrennten
Flächen; die Klimadaten geben es her, es fehlt allein die Flächenaufteilung. Die Trennung
bleibt deshalb empfohlen (billig, die Daten liegen vor), ist aber kein Pflichtfeld für G1.

### 5.13 Rechenzeit — gemessen

Release ohne Debugger, globaler Warmlauf, danach zehn Wiederholungen je Fall, Median; die
Stoppuhr umschließt nur die Zeitschrittschleife.

| Fall | Regelung | Schritte | Median | Umschaltereignisse |
|---|---|---|---|---|
| EPOS-Jahreslauf, Median über 15 Gebäude (9 480 Schritte inkl. Vorlauf) | ideal, Φ ≥ 0, unbegrenzt | 9 480 | **5,09 ms** (4,5–9,1) | 203–826 je Jahr |
| Testfall 7 (ideal, ±500 W) | geregelt | 1 440 | 0,56 ms (3,7 ms je 9 480) | 3 |
| Testfall 1 (Freilauf) | — | 1 440 | 0,33 ms (2,1 ms je 9 480) | 0 |
| Testfall 11 (Kühldecke, ±500 W) | geregelt | 1 440 | 2,63 ms (17 ms je 9 480) | 239 |

Je Umschaltereignis (Bisektion des Umschaltzeitpunkts) fallen rund 7 µs an, das erklärt die
Spanne 4,5–9,1 ms zwischen den Gebäuden vollständig. Frühere Zahlen von 87–121 ms je Jahr
waren Hochrechnungen aus einem kalten Lauf mit gestufter JIT-Kompilierung (der erste Aufruf
in einem frischen Prozess kostet rund 0,25 s, der dritte 10 ms); 2,35 s für rund 100 Läufe
enthielten Prozessstart, JSON-Einlesen und CSV-Ausgabe. **Planungsgröße: 10 ms je Gebäude
und Jahr** — die dreizehn Referenzprojekte kosten damit unter 0,2 s zusätzlich zu den 4 s
des heutigen Gesamtlaufs.

### 5.14 Was daraus für G1 folgt

1. Fenster Ost und West getrennt erfassen; Migration hälftig mit Kennzeichen „geschätzt".
   Gemessen wirkt die Trennung klein (5.12: Jahresenergie ≤ 2,5 %, Spitze ≤ 0,5 %,
   Morgensumme bis 10 %) — empfohlen, nicht Pflicht.
2. Verschattungsfaktor und Rahmenanteil als Felder mit Vorgaben je Lage und Baualter — der
   größte geratene Hebel (6,4 %).
3. Die Bestandsgewichte 0,83/0,95/0,45/0,83 im VDI-6007-Weg streichen (Q16).
4. Randbedingung der Grundfläche als Feld (Erdreich, unbeheizter Keller, Außenluft).
5. Harte Plausibilitätsprüfungen statt stiller Rückfälle; `Bauweise` in der Testdatenbank
   korrigieren.
6. Kapazitätsaufteilung 0,3/0,7, A_IW = 2,5·A_f, h_ms 9,1 als Vorgaben — unkritisch.
7. Spitzenlast dreifach ausweisen; Leistungsgrenze als Option.
8. Eine Zeitbasis (Ortszeit) und Zustand je Instanz.
9. Hay-Davies für die Fassadenstrahlung im Modell; Nordfassade sonst zu hoch.
10. Kein Absorptions-/Abstrahlungsterm in G1 (Schalter vorhanden, Pauschale verboten).

Einschränkung, die stehen bleibt: die Referenz stammt aus dem Tagesmodell, Übereinstimmung
wäre kein Gütebeweis. Der Gütebeweis des Lösers sind die zwölf Normtestfälle; dieser
Vergleich zeigt, wo und warum die Modelle auseinanderlaufen. Belastbar gegen die Wirklichkeit
validieren ließe sich EPOS erst an gemessenen Verbräuchen — in den dreizehn Projekten
kommen keine vor.

---

## 6. Datenmodell und Migration

### 6.1 Gebäudespalten-Schritt (Papiername M3) — `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`

Alle neuen Spalten ohne DDL-DEFAULT auf Fachwerten; **NULL = Vorgabe** (Hausregel aus dem
PV-Zweig). Beide Tabellen, weil der Katalog in das Projekt kopiert wird.

| Spalte | Typ | Bedeutung | NULL bedeutet |
|---|---|---|---|
| `Gebaeude_Modell` | TEXT | Schalter „Rechenweg": `TAGESBILANZ` = Altweg (Bestandsweg, bis zur Ablösung durch GA); NULL = VDI 6007 | VDI 6007 |
| `Fensterflaeche_Ost` | REAL m² | Fenster Ost | ½ `Fensterflaeche_Ost_West` |
| `Fensterflaeche_West` | REAL m² | Fenster West | ½ `Fensterflaeche_Ost_West` |
| `Rahmenanteil` | REAL | Anteil Rahmen an der Fensterfläche | 0,3 |
| `Verschattungsfaktor` | REAL | F_S | 0,9 |
| `Grundflaeche_Randbedingung` | TEXT | `ERDREICH`, `KELLER`, `AUSSENLUFT` | Erdreich |
| `Kellertemperatur` | REAL °C | Temperatur des unbeheizten Kellers bei Randbedingung `KELLER` | 10 °C (N1.3) |
| `Masseanteil_Aussen` | REAL | a_AW | 0,3 |
| `Innenflaechenfaktor` | REAL | f_IW = A_IW / Nutzfläche (E13; die Spalte heißt nach M3 `Nutzflaeche`, E19) | 2,5 |
| `Heizung_Strahlungsanteil` | REAL | radiativer Anteil der Heizung | 0,3 |
| `Heizleistung_Max` | REAL kW | Leistungsgrenze der idealen Heizung | unbegrenzt |
| `Aussenbauteile_Strahlung` | INTEGER `NOT NULL DEFAULT 0 CHECK ("Aussenbauteile_Strahlung" IN (0,1))` | θ_eq mit Absorption und Abstrahlung | — (Schalter, kein Fachwert: hier gilt die Boolean-Regel aus `BETRIEB_SQLITE.md`) |

**Mit G2 kommen drei weitere Gebäudespalten:** `Luftwechsel_Infiltration` (REAL 1/h,
NULL = 0,3), `Luftwechsel_Nutzer` (REAL 1/h, NULL = 0,4), `Sommerlueftung` (INTEGER
`NOT NULL DEFAULT 0 CHECK (… IN (0,1))`). Nach U5 legt M3 sie zusammen mit den zwölf oben an —
**15 Spalten je Tabelle, 30 `SchemaSpalte`-Einträge**. Die Reihenfolge im Schrittkörper steht
fest (N1.24): erst je Tabelle `RENAME COLUMN Wohnflaeche → Nutzflaeche`, wo die Spalte noch so
heißt, dann die neuen Spalten, dann der Sichtneubau (6.2). Die Klimaspalten sind ein eigener
Schritt (Papiername **M4**) und **bereits umgesetzt**: Schemaschritt 95 (19.09.2026) hat
`Gegenstrahlung`, `Luftfeuchte` und `Bedeckungsgrad` in `Tab_Solar(_STAMM)` sowie `Quelle` und
`Importdatum` in `Tab_Klimaregion(_STAMM)` angelegt, Schritt 97 `Szenario` und `Bezugsjahr` (2.3);
eine Spalte `Windgeschwindigkeit` gibt es nicht, weil sie keinen Leser hätte.
Eine Vorgabe des Rahmenanteils je Baualter
braucht eine Tabelle `Baualtersklasse` → Baujahrspanne, die es heute nicht gibt (die Klasse
ist ein Buchstabe, 3.1); sie kommt, wenn überhaupt, mit `Baujahr` im IFC-Schritt (7.6).

Der Altweg liest keine dieser Spalten; `Fensterflaeche_Ost_West` bleibt, damit
`SolareGewinneC` unverändert rechnet. Der Dialog pflegt Ost und West getrennt und schreibt
die Summe in `Ost_West` mit — eine Wahrheit im Dialog, zwei Leser im Kern; das Modellfeld
`Fensterflaeche_Ost` wird dabei in `Fensterflaeche_OstWest` umbenannt, damit die Namensfalle
(2.1, Punkt 4) verschwindet.

**Die NULL-Vorgabe der Ost- und Westfläche ist eine Abhängigkeit vom Bestandsfeld, die nur für
den Übergang gilt.** Sind beide Spalten NULL, bildet sie der **Vorbereitungsschritt** der
Fassade aus `Fensterflaeche_Ost_West` (je die Hälfte), nicht das VDI-Modul — so bleibt das
Modul frei von Altweg-Feldern (4.1, 4.4). Die Stufe GA füllt `Fensterflaeche_Ost` und `_West`
einmalig aus `Fensterflaeche_Ost_West`, **bevor** sie das Bestandsfeld entfernt (E26).

**Kein zweites Datenmodell, und die Altweg-Spalten bleiben für den Übergang stehen**
(E20, E23, E26): Es gibt keine zweite Gebäudetabelle; Spalten, die **nur der Altweg** liest,
fasst M3 nicht an. Sie bleiben, solange der Übergang läuft, ebenso `Gebaeude_Modell` selbst.
Entfernt werden sie mit der Stufe **GA — Altweg ablösen** in einem eigenen Schemaschritt
(`DROP COLUMN` je Tabelle und Sichtneubau); derselbe Schritt nimmt die leserlosen Spalten
`WW_Bedarf` und `Waermebedarf` mit. Er wird fällig, sobald das Ablösekriterium erfüllt ist (Q24, E27); sein Umfang (Q25, E27) steht in
Kapitel 11 und in der Löschliste des
[Umsetzungskonzepts](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md).
Welche Spalte zu welchem Weg gehört —
„nur Altweg", „beide", „nur VDI" —, weist je Feld ein eigener Befund aus:
[`Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md`](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md).
`Baujahr` (INTEGER) kommt erst mit dem IFC-Schritt (7.6), zusammen mit Herkunftskennzeichen.

### 6.2 Sicht neu aufbauen, Leser auf Namen

Die Sicht `Abfrage_Projektgebaeude` hat eine feste Spaltenliste
(`sql/schema/002_views.sql:89-91`); neue Spalten von `Tab_Gebaeude` erreichen den Leser nur,
wenn der Gebäudespalten-Schritt M3 die Sicht neu aufbaut (`DROP VIEW` + `CREATE VIEW`, neue
Spalten hinter `Tab_Gebaeude.ID`; SQLite kennt kein `ALTER VIEW`). **Ab M3 ist
`GebaeudeSchema.SQL_VIEW_NEU` die einzige Quelle der Sichtdefinition**;
`sql/schema/002_views.sql` bleibt der eingefrorene Stand 61 und wird nicht nachgezogen — es
gehört dem Access-Zweig und wird zur Laufzeit nicht gelesen. Zugleich wird
`ProjektGebaeudeCtrl.ReadAll`
von Index- auf Namenszugriff umgestellt (Muster `GebaeudeCtrl.MapRowToModel`), mit einem
Test, der alle 58 Bestandsfelder gegen die Testdatenbank hält, bevor eine Spalte hinzukommt.
Ohne beides verschiebt jeder Schemaschritt die Zuordnung still oder liefert die neuen
Werte gar nicht.

### 6.3 Stufe G3 — Bauteilkatalog

**Wie gebaut (25.09.2026).** Drei Schemaschritte legen **alle acht Tabellen** an, STRICT, Beziehungen
über IDs (Softwarearchitektur W1):

| Schritt | Tabellen | Inhalt |
|---|---|---|
| **132** (S-A, `BaustoffSchema`) | `Tab_Baustoff_STAMM`, `Tab_Baustoff` | spaltengleich, mit `Hersteller` (NULL = herstellerneutral, E39); Saat von 65 herstellerneutralen Stoffen (DIN 4108-4 / DIN EN ISO 10456) und 67 Herstellerprodukten, `ReadOnly = 1`, Quelle je Zeile |
| **133** (S-B, `BauteilaufbauSchema`) | `Tab_Bauteilaufbau(_STAMM)`, `Tab_Bauteilschicht(_STAMM)` | der wiederverwendbare Aufbau und seine Schichten (innen → außen, Stoffwerte als Kopie) |
| **134** (S-C, `ZonenSchema`) | `Tab_Zone`, `Tab_Bauteil` | Zone am Gebäude samt den Spaltenblöcken aus KU-S1 und AK-S1; Bauteil an der Zone (neun Bauteilarten, vier Randbedingungen) |

Die Projektkopien `Tab_Baustoff` und `Tab_Bauteilaufbau` tragen den Fremdschlüssel auf `Tab_Projekt`
(Hausregel seit Schemaschritt 96, N1.46). Die vollständigen Spaltenlisten stehen in der
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 2.2, Kopier- und
Katalogregeln im [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 3.5 und 4.2. Die Schicht
hängt am **Aufbau**, nicht unmittelbar am Bauteil, die Namensspalte heißt hausüblich `Bezeichner`; die
Tabellen entstehen **einmal**, G6 übernimmt sie unverändert und ergänzt allein mit S-G
`Tab_Zonenluftstrom` und `Tab_Bauteil.ID_Nachbarzone`. Der Bauteilkatalog ist zugleich das Ziel des
Imports auf Bauteilebene aus IFC und gbXML (G4b, 7.6, N1.49: Zone, Bauteile, Aufbauten und Schichten als
Projektkopien, ohne eigenen Schemaschritt) und ersetzt für Gebäude mit Zone die Nachmultiplikation
(4.7).

### 6.4 Persistenzwerte

Spalte `Gebaeude_Modell` und Werte nach dem PV-Vorbild (`Tab_Energieanlagen.PV_Modell`,
`DbWerte.cs:2173/2180`): `DbWerte.GEBAEUDE_MODELL_TAGESBILANZ = "TAGESBILANZ"`, `GEBAEUDE_MODELL_VDI6007 = "VDI6007"`,
`GRUND_ERDREICH/KELLER/AUSSENLUFT`; gelesen ausschließlich in der Weiche am Eingang (4.1), im
Eingangsbauer und in der Anzeige. In der Oberfläche heißt das Feld **„Rechenweg"** mit der
Vorgabe „VDI 6007" (NULL) und dem Wert „Tagesbilanz"; Schalter, Spalte und Werte bleiben, bis
die Stufe GA sie entfernt (E20, E23, E26; beauftragbar nach dem Ablösekriterium, Q24, E27).

---

## 7. IFC-Import

### 7.1 Das Ergebnis in einem Satz

**IFC kann das Gebäudemodell speisen, liefert die VDI-6007-Daten aber nicht fertig, sondern
Rohmaterial wechselnder Qualität** — ein reiner C#-Leser ohne Geometriekernel reicht für
eine erste Stufe, plattformfrei bis iOS; die eigentliche Arbeit liegt im Zuordnungsdialog
und in den Vorgabewerten. Kein ernsthaftes Energiewerkzeug verlässt sich auf die Exporte
der Autorensysteme — IDA ICE, Hottgenroth, bim2sim rekonstruieren und reichern an.

### 7.2 Was IFC trägt

Geprüft gegen IFC 4.3.2 (ISO 16739-1:2024) und IFC4 ADD2 TC1 (Quellen: buildingSMART
Lexical, 15.09.2026).

| Modellgröße | IFC-Quelle | Hinweis |
|---|---|---|
| Wohnfläche, Raumhöhe, Volumen | `IfcSpace` + `Qto_SpaceBaseQuantities` (`NetFloorArea`, `Height`, `NetVolume`) | Zonen über `IfcZone` (nicht hierarchisch, Mehrfachzuordnung möglich) |
| U-Werte, außen/innen | `Pset_WallCommon`, `Pset_SlabCommon`, `Pset_RoofCommon`, `Pset_WindowCommon`, `Pset_DoorCommon`: `ThermalTransmittance`, `IsExternal` | optional; Revit schreibt `ThermalTransmittance` nur mit gemapptem Shared Parameter |
| g-Wert | `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` (= g nach Spezifikation), `ThermalTransmittanceSummer/Winter` | selten gefüllt |
| Flächen | `Qto_WallBaseQuantities.GrossSideArea` (ohne Öffnungsabzug) / `NetSideArea` (mit); `Qto_WindowBaseQuantities.Area`; `Qto_SlabBaseQuantities.GrossArea` | Revit nur mit Häkchen „Basismengen" |
| Schichtaufbau, Stoffwerte | `IfcMaterialLayerSet`/`IfcMaterialLayer` + `Pset_MaterialThermal` (λ, c), `Pset_MaterialCommon` (ρ) | vollständiger Eingang für die Reduktion (G3) |
| Orientierung, Ort | `TrueNorth` (optional, Vorgabe [0,1], bei `IfcMapConversion` nur informativ), `IfcSite.RefLatitude/RefLongitude` (`IfcCompoundPlaneAngleMeasure` = **`LIST [3:4] OF INTEGER`**: Grad, Minuten, Sekunden, optional Millionstel-Sekunden, alle mit gleichem Vorzeichen — nicht Dezimalgrad) | zwei klassische Importfallen; `IfcZone` kann laut Schema auch Zonen enthalten, Schachtelung ist zu entschachteln |
| Raumgrenzen | `IfcRelSpaceBoundary`, ab IFC4 `…1stLevel`/`…2ndLevel` (Typ 2a Raum gegenüber, 2b Bauteil gegenüber); Fenster über `ParentBoundary`, Eltern- und Kindfläche **überlappen** | 1st Level ist laut Norm nicht für thermische Analysen nutzbar |
| Nutzung | `Pset_SpaceThermalRequirements` (in IFC 4.3 entfallen), `Pset_SpaceOccupancyRequirements`, `Pset_SpaceThermalLoad` (alle Werte als `IfcPowerMeasure` typisiert, auch die Luftwechselrate — Schemafehler) | nicht belastbar |
| Baujahr | `Pset_BuildingCommon.YearOfConstruction` als `IfcLabel` (**Text**) | „ca. 1965" parsen |

**Space Boundaries sind in keiner Haupt-MVD Pflicht** (Reference View 1.2 nennt sie nicht;
Design Transfer View ist Entwurf; die eigene „Space Boundary Add-on View" ist ein Anhang zur
IFC2x3-Koordinationsansicht). Ein zertifizierungskonformer IFC4-Export enthält also weder
2nd-Level-Grenzen noch garantiert U-Werte.

### 7.3 Was die Autorensysteme liefern

| System | `IfcSpace` | Space Boundaries | thermische Psets |
|---|---|---|---|
| Revit | ja (Option) | Option none / 1st / 2nd Level | `ThermalTransmittance` nur mit Mapping |
| Archicad | ja (Zonen) | an/aus, immer 2nd Level — aber als **Basisklasse `IFCRELSPACEBOUNDARY` mit `Name='2ndLevel'`**, nicht als `…2ndLevel`-Entität | über Übersetzer |
| Allplan | ja | nein (belegt für Version 2023) | begrenzt |
| Vectorworks | ja | nicht dokumentiert; Energiepfad ist gbXML | — |

Erfahrungsbefund der Literatur (RWTH, Automation in Construction 2025): Zertifizierung
garantiert keine vollständigen Energiedaten; 2nd-Level-Geometrie kommt, die Gegenstücke in
Nachbarzonen fehlen; Geometriefehler und Datenverlust sind in den ausgewerteten
Interoperabilitätsstudien die Regel, nicht die Ausnahme (Fundstelle vor G4 nachtragen).
Deutsche Praxis (Plancal nova): „das Architekturmodell ist in der Regel für
eine thermische Betrachtung nicht geeignet" — mehrschalige Wände als mehrere Elemente, Putz
und Beläge als eigene Bauteile, U-Werte fehlen.

### 7.4 Bibliotheken (Stand 15.09.2026, aus NuGet und GitHub verifiziert)

| Bibliothek | Version | Lizenz | Zielrahmen | nativ? | IFC | Geometrie |
|---|---|---|---|---|---|---|
| **Xbim.IO.MemoryModel** — der Paketzuschnitt des Kerns nach [`ADR-003`](ADR-003_IFC_xBIM_ohne_Geometriekernel.md): es zieht `Xbim.Common`, `Xbim.Ifc2x3`, `Xbim.Ifc4` und `Xbim.Ifc4x3` nach; das Metapaket `Xbim.Essentials` und `Xbim.Ifc` werden **nicht** referenziert, weil sie `Xbim.IO.Esent` (ManagedEsent, nur Windows) mitbringen | 6.1.605 | CDDL-1.0 | **net10.0**, net8.0, netstandard2.0/2.1 | **rein verwaltet** | 2x3, 4, 4.3 | nein |
| Xbim.Geometry (`Xbim.Geometry.Engine.Interop`) | 6.3.891-netcore (**Vorabversion**), stabil zuletzt 5.1.820 (nur net472) | CDDL-1.0, **zieht `Xbim.Geometry.Occt` 7.8.1 unter LGPL-2.1 + OCCT-exception-1.0 nach** | net472, net8.0 | **C++/CLI, nur Windows** (`Ijwhost.dll`, `win-x64`) | dito | ja (OCCT) |
| **GeometryGymIFC_Core** | 26.8.17 | MIT | netstandard2.0, net6–8 | rein verwaltet | 2x3, 4, 4.3, 4.4 | nein |
| ara3d/IFC-toolkit | Vorabversion | MIT | net8.0 | optional web-ifc-DLL | STEP | über C++ |
| Hypar.IFC4 | 1.2.0 | MIT | net6.0 | nein | 4 | nein; seit 2023 ohne Pflege |
| IfcOpenShell | — | LGPL-3.0 | C++/Python | ja | alle | ja; **keine gepflegte .NET-Anbindung** |
| web-ifc | 0.0.77 | MPL-2.0 | WASM/C++ | ja | alle | ja |
| BIMserver | — | AGPL-3.0 | Java | Server | alle | ja — ausgeschlossen |

Die Entity-Factory von xBIM ist generierter Code (1 438 `case`-Zweige), kein
`Reflection.Emit` — AOT-tauglich. iOS-Risiko ist das Trimming (`ExpressMetaData` nutzt
`module.GetTypes()`); Abhilfe ist ein `TrimmerRootDescriptor`, früh im iOS-Lauf zu prüfen.
**CDDL-1.0 ist Datei-Copyleft.** Die Einbindung in ein proprietäres Produkt ist zulässig
(§ 3.6 „Larger Work"), die eigene Binärfassung darf unter eigener Lizenz ausgeliefert werden
(§ 3.5). Drei Auflagen bleiben: (1) § 3.1 — der Quelltext **aller** ausgelieferten
CDDL-Dateien muss unter CDDL verfügbar sein, auch wenn nichts geändert wurde; der dauerhafte
Verweis auf die xBIM-Quellen (github.com/xBimTeam bzw. die NuGet-Quellpakete) genügt und ist
dem Empfänger mitzuteilen; (2) § 3.4 — Copyright-, Patent- und Markenvermerke bleiben
stehen, eigene Änderungen an CDDL-Dateien sind zu kennzeichnen; (3) der Lizenztext wird mit
ausgeliefert (Installationspaket: Lizenzhinweise). Daraus die Regel: **xBIM nur als
NuGet-Paket einbinden, nie forken.** LGPL (IfcOpenShell) und GPL (IFC2SB) scheiden für den
Kern aus; **auch `Xbim.Geometry` fällt damit aus dem Kern** — nicht nur wegen C++/CLI,
sondern weil OCCT unter LGPL-2.1 steht und selbst in der Windows-Schale dynamische Bindung
und Austauschbarkeit nachzuweisen wären. MPL-2.0 (web-ifc) ist ebenfalls Datei-Copyleft und
nur für den optionalen ara3d-Pfad zu prüfen, nicht für den Kern.

**Empfehlung:** `Xbim.IO.MemoryModel` 6.1.605 mit `Xbim.Common`, `Xbim.Ifc2x3`, `Xbim.Ifc4`
und `Xbim.Ifc4x3` im Kern; `GeometryGymIFC_Core` (MIT) als
Ausweg, falls CDDL nicht freigegeben wird; `Xbim.Geometry` **nie im Kern**, allenfalls in der
Windows-Schale für eine Vorschau, auf iOS benannt abgelehnt — dasselbe Muster wie
`Google.OrTools`.

### 7.5 Ohne Geometriekernel — Regeln und Fallen

Was ohne Kernel geht: U-Werte und `IsExternal` aus Psets; Bruttoflächen aus den
Quantity-Sets; Fensterflächen aus `Qto_WindowBaseQuantities`; Volumen als Summe der
Raumvolumen; thermische Masse aus Schichtaufbau und Stoffwerten; **Azimut** aus der Kette
`IfcLocalPlacement` → `'Axis'`-Repräsentation der Wand (bei `IfcMaterialLayerSetUsage`
zwingend, parallel zur x-Achse des Objektsystems) → `TrueNorth` — reine Matrixmultiplikation.

Die Fallen, die der Leser kennen muss:

1. Quantities fehlen oft → Rückfall Länge × Höhe aus `IfcExtrudedAreaSolid`; scheitert bei
   nicht-prismatischen Wänden.
2. Fensterabzug: `GrossSideArea` ohne, `NetSideArea` mit Abzug; Fenster über
   `IfcRelVoidsElement` → `IfcOpeningElement` → `IfcRelFillsElement`. Nie mischen.
3. Gedrehte Gebäude: `TrueNorth` fehlt oder steht neben `IfcMapConversion` (dann nicht
   addieren).
4. `IsExternal` fehlt oder ist falsch → Rückfall: eine Wand ist außen, wenn nur **eine**
   Raumgrenze auf sie zeigt.
5. Mehrschalige Wände als mehrere Elemente → über gemeinsame Grenze oder Achslage
   zusammenfassen.
6. Drei optionale Höhenbezüge (`IfcSite.RefElevation`, `IfcBuilding.ElevationOfRefHeight`,
   `IfcBuildingStorey.Elevation`).
7. `YearOfConstruction` ist Text.
8. Nachbarbebauung fehlt praktisch immer → Verschattung bleibt Eingabe.
9. Archicad-Grenzen nur über `Name`/`Description` erkennbar (7.3).

Für Ein- und Mehrfamilienhäuser mit rechteckigem Grundriss reicht das; für gegliederte
Nichtwohngebäude ohne Quantities wird es unzuverlässig — dort braucht es Stufe G5.

### 7.6 Der Importweg in EPOS-Plan

1. **Einstieg** im Gebäudedialog: Knopf „Aus IFC-Datei übernehmen …" (und im
   Katalogdialog). Die Dateiwahl läuft über `IDateiDienst` (`EPOS.Kern/Allgemein/Dienste/`),
   also auf beiden Plattformen; `KeineDateiwahl` lehnt benannt ab.
2. **Leser** `EPOS.Kern/Allgemein/Import/Ifc/IfcImportAblauf.cs` (Muster
   `KatalogImportAblauf`, `KlimaImportAblauf`) öffnet die Datei mit `Xbim.IO.MemoryModel`,
   erkennt Schema (2x3 / 4 / 4.3) und liefert einen `IfcImportSatz`: je Zielfeld Wert,
   **Herkunft** (`IFC`, `Vorgabe`, `leer`) und Beleg (Entität, Pset). Größenlimit für Blazor
   Hybrid (Vorschlag 50 MB), Fehler benannt.
3. **Zuordnung** in G4a auf `Tab_Gebaeude`: Wohnfläche und Raumhöhe aus den Räumen;
   Volumen; U-Werte je Bauteilgruppe **flächengewichtet** auf die fünf Kategorien
   `k_Wert_Außenwand/Fenster/Dachflaeche/Grundflaeche/Sonstiges`; Fensterflächen über den
   Azimut in die vier Sektoren (Sektorbreite 90°, Mitte N/O/S/W) — Ost und West getrennt
   (6.1); g-Wert → `Fensterdurchlassgrad`; Schichtaufbau → `Bauweise` (Summe ρ·c·d der
   raumseitigen Schichten bis 10 cm, ISO 13786-Näherung); Bodenplatte gegen Erdreich oder
   Keller → `Grundflaeche_Randbedingung`; `YearOfConstruction` → `Baujahr` →
   `Baualtersklasse`. Auf Wunsch (Schalter „Als Zone mit Bauteilen übernehmen", G4b, N1.49)
   zusätzlich **eine** Zone mit je Bauteil einer Zeile in `Tab_Bauteil` (Nettofläche, Azimut samt
   Nordwinkel, Neigung, Randbedingung, U, g, Herkunft `IFC`/`GBXML`, Quellkennung) und den Aufbauten
   samt Schichten als Projektkopien — dann echte Hülle statt Nachmultiplikation. Die innere Masse
   folgt der Datenlage: Innenbauteile beider Seiten oder der Innenflächenfaktor aus der Datei (E45).
4. **Vorgaben**: Was IFC nicht liefert, wird je `Baualtersklasse` vorbelegt (Typgebäude
   TABULA/IWU, Zenodo 2025 — Record und Datensatzlizenz vor G4 eintragen) und **sichtbar
   als Vorgabe markiert**. Pflicht sind nur
   Wohnfläche und Raumhöhe (Q11).
5. **Dialog** `IfcZuordnungDialog.razor`: Tabelle Feld | IFC-Wert | Beleg | Vorgabe |
   übernehmen; OK schreibt, Abbrechen verwirft (Hausmuster OK/Abbrechen). Nichts wird ohne OK
   geschrieben; die Plausibilitätsprüfungen aus 4.8 laufen vor dem Schreiben.
6. **Nachweise**: KIT-Datei `AC20-FZK-Haus.ifc` (Archicad 20, IFC4, 8 Räume, 81 Raumgrenzen,
   33 U-Werte, Nutzung uneingeschränkt mit Namensnennung im vorgegebenen Wortlaut) als
   Importprobe unter
   `Referenzlaeufe/Importproben/` mit Quellenvermerk; Tests: Schema erkannt, Räume gezählt,
   Sektorzuordnung, Herkunft je Feld, Archicad-Grenzen erkannt, Größenlimit greift.

### 7.7 gbXML

Schema 8.01 (Januar 2026), thermische Zonentopologie ist Teil des Schemas; Archicad und
Revit exportieren gbXML mit Raumbegrenzungen zuverlässiger als IFC. Aber: kein
ISO-Standard, Lizenz des Schemas ungeklärt (keine Angabe auffindbar — vor G5 eine
schriftliche Nutzungserlaubnis für das XSD einholen), im DACH-Raum schwach verbreitet
(Solar-Computer, DDS-CAD, AX3000; Hottgenroth und ZUB Helena ohne dokumentierten
gbXML-Import); der deutsche Nachweisweg (DIN V 18599, VDI 2552 Blatt 11.9) läuft auf IFC.
**Ergänzung, nicht Ersatz — Stufe G5, reines XSD-Deserialisieren, 10–15 PT.**

### 7.8 Testdateien

| Datei | Quelle | Räume | Raumgrenzen | U-Werte | Nutzung |
|---|---|---|---|---|---|
| `AC20-FZK-Haus.ifc` (EFH, IFC4, 2,5 MB) | KIT/IAI | 8 | 81 (Basisklasse, `2ndLevel`/`2a`) | 33, 7× `Pset_SpaceThermalRequirements` | frei mit Namensnennung |
| `AC-20-Smiley-West-10-Bldg` (Reihenhaus) | KIT/IAI | 140 | 1 689 | 180 | frei mit Namensnennung |
| `AC20-Institute-Var-2.ifc` (Büro) | KIT/IAI | 83 | 1 000 | **0** | frei mit Namensnennung |
| `FM_ARC_DigitalHub_with_SB.ifc` (Revit 2019) | RWTH E3D GitLab | 59 | 1 560 / 2 582 echte 1st/2nd-Entitäten | 719, 85× Glazing-Pset | **keine Lizenzdatei — Rückfrage** |
| Duplex Apartment (IFC2x3) | buildingSMART Community | 21 | 265, nur 1st Level | 0 | frei; Git LFS |

Die Zahlen wurden am 15.09.2026 durch Herunterladen und Auszählen der Dateien geprüft.
Das Open-IFC-Model-Repositorium der Universität Auckland ist leer; die bim2sim-Testdaten
führen keine Lizenz.

### 7.9 Praxisrelevanz und Grenzen

Für die Zielgruppe Heizungserneuerung im Bestand (VDI 4645, EFH/MFH) liegt praktisch kein
Modellbestand vor; IFC-Modelle kommen aus Neubau und großer Sanierung im Nichtwohnbereich
sowie aus Scan-to-BIM. **IFC ist ein Komfortweg für Nichtwohn- und Quartiersprojekte, kein
Ersatz für die Eingabe** — und er lohnt erst, wenn das Gebäudemodell (G0–G2) steht, sonst
importiert man in ein Tagesmodell, das die Daten nicht nutzt. **Ein IFC-Betrachter gehört nicht zum
Import** — was EPOS-Plan stattdessen zeigt, steht in Nachtrag N1.16 (Entscheid E11).

---

## 8. Oberfläche

### 8.1 Gebäudedialog

`GebaeudeDialog.razor` und `GebaeudeKatalogDialog.razor` werden nach den Eingaben des
VDI-Wegs aufgebaut — es gibt nur **eine** Dialogstruktur, die des künftig alleinigen
Rechenwegs (E20, 16.09.2026): Nutzfläche und Raumhöhe, Bauteile mit U·A, Fenster je
Orientierung (Ost und West getrennt) mit Rahmenanteil und Verschattung, Randbedingung der
Grundfläche, die Modellparameter aus 6.1 (Masseanteil außen, Innenflächenfaktor, Schalter
Außenbauteile mit Strahlung), Infiltration und Nutzerlüftung, Heizung mit Strahlungsanteil
und Leistungsgrenze — jeweils mit Vorgabe-Anzeige („Vorgabe 0,3"). **Die Modellparameter sind
immer sichtbar und bearbeitbar**, auch bei einem Gebäude des Bestandswegs; sie gelten dort
nach einer Umstellung. Der Schalter **„Rechenweg"** trägt für die Dauer des Übergangs die
Werte „VDI 6007" (Vorgabe) und „Tagesbilanz" samt Herleitungszeile; mit der Stufe GA entfällt
er (E23, E26). Felder, die
**nur der Altweg** liest, erscheinen allein bei einem Gebäude des Bestandswegs, in einem
eingeklappten Abschnitt **„Tagesbilanz (Bestandsweg)"** mit dem Hinweis, dass dieser Weg
keine neue Funktion mehr bekommt; die Feldzuordnung steht in Befund X (6.1). Dieselbe
Struktur gilt für den Zuordnungsdialog der Skalierung (`GebaeudeWohnflaecheDialog`). Der
Infobutton auf die Wiki-Seite kommt mit G2, zusammen mit der Seite selbst. Die Gruppe
„Verbrauch" bleibt. Knopf „Aus IFC-Datei übernehmen …" ab G4. Plausibilitätsmeldungen (4.8)
erscheinen benannt beim Speichern.

### 8.2 Bedarfsdialog und Ergebnis

`GebaeudeBedarfDialog.razor` zeigt bei VDI 6007 zusätzlich: Raumtemperatur-Jahresverlauf
(Luft und operativ, mit Sollwertband), Kühlbedarf informativ, die Kennzahlen aus 4.6 mit den
drei Spitzenwerten, und den Vergleich „Tagesbilanz | VDI 6007" für dasselbe Gebäude (beide
Wege sind Auskünfte über `GebaeudeBedarfCtrl`) — dieser Vergleich bleibt, **solange der Altweg
besteht**; er gehört zum Übergang und entfällt mit der Stufe GA, zusammen mit dem vierten
Controller-Parameter `modellErzwungen` (E23, E26). Ein Gebäude des Bestandswegs trägt statt des
Produktausweises nach E10 die Zeile **„Tagesbilanz (Bestandsweg)"** (E20, E23). Reihenfolge
und Steuerzeile nach `Doku_Simulationsergebnis_Darstellung.md`.

### 8.3 Hülle nach `EPOS.UI.Daten`

Die Gebäudehülle wandert aus der Windows-Schale nach `EPOS.UI.Daten/Bedarf/GebaeudeHuelle.cs`
(DTO `GebaeudeBedarfDaten` erweitert um den Rechenweg und die Modellparameter; keine 8 760
Werte im DTO, Bilder über Delegat). Das ist ohnehin für iOS fällig und wird mit G1 erledigt.

### 8.4 Menü

Kein neuer Menüpunkt für G0–G2. Die zwei Kataloge der Stufe G3 — „Baustoffe" und
„Bauteilaufbauten" — stehen gemeinsam unter Administration › Gebäude nach „Gebäudetypen" (Menü ist
Daten: `Menuetabelle.cs`; umgesetzt 25.09.2026), der IFC-Import läuft aus dem
Gebäudedialog, nicht aus „Datenimport" (er ist projektbezogen, kein Katalogimport).

### 8.5 Texte

Alle neuen Schlüssel in `Resource.resx` und `Resource.en-US.resx` (Glossar:
`Glossar_Lokalisierung.md`), danach `Werkzeuge/ResourceDesigner`.

---

## 9. Bericht, Diagramme, Wiki

- **Quelle der Gebäudezahlen im Bericht ist die Ergebnistabelle `Tab_ErgebnisGebaeude`**
  (E30, N1.35): Der Lauf schreibt je Gebäude Rechenweg, Wärmebedarf, die drei Spitzenwerte und
  auf dem VDI-Weg Kühlenergie, Stunden mit Kühlbedarf, mittlere Raumtemperatur, Überhitzungs-
  und Sommerlüftungsstunden; der Bericht liest sie und rechnet nichts nach.
- Projektbeschreibung: der Abschnitt **„Gebäude (Simulationsergebnis Stamm)"** je Gebäude mit
  dem Rechenweg als Text — „VDI 6007" bzw. bei einem Gebäude des Bestandswegs **„Tagesbilanz
  (Bestandsweg)"**, solange der Altweg besteht (E20, E23, E26) —, den drei Spitzenwerten und den
  Kühlkennzahlen; ohne Gebäudezeile entfällt der Abschnitt. Der Berichtskopf trägt den
  **Produktausweis nach E10** (A12), sobald ein Gebäude auf dem VDI-Weg gerechnet hat. Der
  Excel-Bericht hat kein Gegenstück zur Projektbeschreibung und bleibt ohne Gebäudeabschnitt.
- `KennzahlenKatalog.cs`: die drei Spitzenwerte, Kühlenergie und Stunden mit Kühlbedarf als
  Kennzahlen der Gruppe `GR_GEBAEUDE`, gelesen aus derselben Tabelle; `AbweichungsErmittler.cs`
  führt den Rechenweg im Variantenvergleich.
- `ChartRenderer.cs`: ein neues Bild „Raumtemperatur" (Jahresverlauf, Sollwertband) —
  `Proben/ChartProben` bekommt die Gegenprobe (Maße, Farben, Determinismus).
- Wiki: neue Seite „Gebäudemodell VDI 6007" (Funktion, Parameter, Vorgaben, Grenzen; ohne
  Hersteller- und Produktdaten), Erweiterung der Seite Gebäude; Logbuch-Eintrag mit der
  Version beim Upload (Regel Konzept Hilfesystem 13.3).

---

## 10. Tests und Abnahme

### 10.1 Normtestfälle

Die zwölf Testfälle der VDI 6007-1 (Testraum 52,5 m³ / 17,5 m², Bauweise S und L; siehe
5.2) werden als xUnit-Fälle `GebaeudeModellNormfallTests` gebaut, Referenzdaten aus AixLib
(überarbeitete 3-Klausel-BSD-Lizenz mit Zusatzabsatz, Wortlaut und Copyright-Vermerk im
Test, Zitierfähigkeit der Normwerte vorab geklärt — 5.1), geprüft gegen das **Band aus beiden
Programmspalten samt Druckrundung nach E10** (5.2), mit Ausweis der Reserve je Fall und
Größe; ein weiterer Test hält die diagonalisierte Matrix als
Negativprobe. Dazu die Bauteilreduktion (G3) gegen die Normwerte der Testräume — dafür ist
die Richtlinie zu beschaffen (Q2).

### 10.2 Reine Rechenproben

Ohne Datenbank: Grenzfälle (C → sehr groß: Temperatur konstant; R_ve → 0: θ_air = θ_out;
Φ_h = 0 und stationär: Bilanz geschlossen), Skalierung (alle Flächen und die Masse mit s
ergeben s-fache Last), Determinismus (zwei Läufe byte-gleich, zwei Gebäude in beliebiger
Reihenfolge gleich), Eigenwerte reell und negativ für alle Klassenparameter der
Testdatenbank, Lastbestimmung hält den Sollwert auf 1e‑9 K, Kappung liefert Kühlleistung
≥ 0, Vorlauf konvergiert, Plausibilitätsprüfungen werfen benannte Fehler.

### 10.3 Datenbankfälle

`[Collection("Testdatenbank")]` mit `Kulturvorrichtung`: `GebaeudeBedarfCtrl` liefert für ein
umgestelltes Gebäude dieselbe Reihe wie der Lauf (Muster `GebaeudeBedarfCtrlTests`);
Gebäudespalten-Schritt M3 auf der Testdatenbank; Namensleser hält alle Felder; Ost/West-Summe
konsistent; Mischfall Gebäude plus Ganglinie (Projekt 1041) summiert richtig.

### 10.4 Referenzlauf und das positive Abnahmekriterium

- **Verschiebung des Altwegs:** der Referenzlauf ist danach **byte-gleich** — das ist die
  Abnahme des ersten G1-Schritts, vor jeder Anbindung des VDI-Wegs (E20, 16.09.2026). Die
  Bestandsbefunde GB werden vorher behoben, damit das verschobene Modul der geprüfte Stand ist.
- **Bestandsprojekte:** unverändert gegen die dann gültige Basis — die Abnahmesperre von G1.
- **Referenzprojekte beider Wege:** ein Projekt (Kopie eines Einzelgebäude-Projekts, etwa
  1045) rechnet auf dem VDI-Weg — nach E20 ist das die Vorgabe, `Gebaeude_Modell` bleibt dort
  NULL —, ein weiteres steht **für die Dauer des Übergangs** auf
  `Gebaeude_Modell = TAGESBILANZ` und hält den Rückweg-Test. Beides wird mit jeder Basis
  eingefroren, bis die Stufe GA das Projekt auf VDI 6007 umstellt und den Rückweg-Test
  einstellt; GA ist dafür ein eigener Einfrierschritt (E23, E26). Nach A15 (E27): **ein**
  Referenzprojekt in der jeweils aktuellen Basis, kein zweiter Basisordner; die Arbeitskopie gegen
  die GB-Basis nur bis zum Merge G1 + G2. Mit G1 wird die
  Basis neu eingefroren und
  in [`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md) begründet
  (neue, vierte Einfrierregel „gesäte Gebäudedaten": `Tab_Gebaeude(_STAMM)` mit `Bauweise`,
  U-Werten, Flächen, Sollwerten, `Luftwechselrate`, `Fensterdurchlassgrad` — auch in den
  Abschnitt „Regressionsnetz" der `CLAUDE.md`). Die neuen Reihen `raumtemperatur.csv`,
  `operative_temperatur.csv` und `kuehlbedarf.csv` exportiert `Ergebnisexport` nur für
  VDI-6007-Gebäude, damit die Bestandsordner byte-gleich bleiben.
- **Positives Kriterium für G1**, weil gemessene Verbräuche fehlen: (1) die Normtests
  bestehen; (2) der Kern reproduziert die Zahlen des Prototyps aus Kapitel 5 innerhalb 1e‑6
  relativ **in einem Prüfmodus mit den Randbedingungen des Prototyps** (UTC-Reihenfolge,
  isotrope `Sol_*`-Spalten, kein Absorptionsterm; der Prototyp ist die unabhängige
  Zweitimplementierung), und der Unterschied zwischen Prüfmodus und Auslieferungsweg
  (Ortszeit, Hay-Davies) wird je Referenzprojekt als Zahl ausgewiesen und begründet;
  (3) Tagessummen-Korrelation zum Tagesmodell r ≥ 0,98; (4) Katalogkennzahl
  `spez_Waermeverbrauch` innerhalb eines Abnahmefensters, das **in G0 neu bestimmt** wird.
  Das bisherige Fenster 85–105 % stammt aus einer Messung **ohne** den Abzug R_si/A (5.5);
  G0 wiederholt sie mit dem Auslieferungsweg (R_si-Abzug, F_W = 0,9, Hay-Davies, Ortszeit,
  a_kon = 0,09), trägt die neuen Prozentwerte je Referenzgebäude ein und leitet das Fenster
  daraus ab. Bis dahin ist das Kriterium **informativ** und keine Abnahmesperre.
  **Bestimmt mit der Schlusswelle G1 + G2 (23.09.2026):** gemessen 95,8–109,4 % je
  Referenzgebäude mit Katalogwert, das Fenster ist **90–115 %** und eine Prüfung in
  `GebaeudeVdi6007DatenbankTests` (Protokoll unter
  `Dokumentation/ueberholt/Protokolle/Gebaeudesimulation/`).
- Die Korrektur von `Bauweise` in Gebäude 10576 (Q22), die Warnungen im Tagesmodell (Q18)
  und der Instanzzustand statt `_prevRoomTemp` (Q23) bilden den eigenen Einfrierschritt GB
  (Kapitel 11), der **vor** G1 läuft. Gemessen ändert GB allein die Referenzergebnisse von
  **1008**; **1039 bleibt byte-gleich** — der statische Zustand erreichte dort nie ein Ergebnis,
  weil Tag 1 die Vortemperatur auf den Nachtsollwert setzt und der Jahreslauf den Vorlauf
  überschreibt (Basis `2026-09-22_R11_Bestandsbefunde`,
  [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Zeile GB).

### 10.5 Wächter

`DoubleWache`, `EinheitenWache`, `RechenrandTests`, `ParallelitaetWache`,
`DokumentationLinkWache`, `RepositoryOrdnungWache`, `WikiProduktdatenWache` — unverändert;
dazu die Hüllenwegwache, sobald die Gebäudehülle wandert (8.3).

---

## 11. Vorschlag in Stufen

| Stufe | Inhalt | Abnahme | Aufwand |
|---|---|---|---|
| **G0 — Löser und Normtests im Kern** | `Zonenmodell7R2C`, `ErsatzparameterRC`, Diskretisierung, Regelung nach Hausregeln neu benannt (der Prototyp ist die Vorlage); `GebaeudeModellNormfallTests` und Rechenproben (10.1, 10.2); Klärung des Drifts in Testfall 9/10; keine Datenbank, keine Oberfläche | zwölf Normtestfälle bestanden; Kern-Filter grün | klein, 2–4 PT |
| **GB — Bestandsbefunde** | Warnungen statt stiller NaN im Tagesmodell (Q18), `_prevRoomTemp` als Instanzzustand mit `ResetState` je Gebäude (Q23), Korrektur 10576 in der Testdatenbank (Q22), vierte Einfrierregel „gesäte Gebäudedaten" | Referenzergebnisse von 1008 ändern sich (1039 bleibt byte-gleich, 10.4) — eigener, begründeter Einfrierschritt; läuft **vor** der Verschiebung des Altwegs, damit das verschobene Modul der geprüfte Stand ist | klein, 1–2 PT |
| **G1 — Trennung der Wege und Anbindung des VDI-Modells** | **zuerst:** Tagesbilanz-Weg Zeichen für Zeichen nach `EPOS.Kern/Allgemein/Simulation/Altweg/`, Fassade `SimulationWaermebedarf` mit modellfreiem Vorbereitungsschritt und **einer** Weiche am Eingang; **dann:** der Gebäudespalten-Schritt M3, Namensleser, `DbWerte`, `GebaeudeModellEingang` (Klassenweg 4.3, Randbedingungen 4.4, Hay-Davies je Orientierung), Plausibilitätsprüfungen, Vorlauf, Anbindung des Moduls `Gebaeude/`, Dialoge in VDI-Struktur mit Schalter „Rechenweg" und eingeklapptem Abschnitt „Tagesbilanz (Bestandsweg)" (8.1), Hülle nach `EPOS.UI.Daten`, Texte | Verschiebung **byte-gleich** gegen die Basis, als eigener Schritt vor der Anbindung; danach Referenzlauf der Bestandsprojekte unverändert; Referenzprojekte beider Wege, Basis neu eingefroren; Kriterien 10.4 | mittel, 10–16 PT |
| **G2 — Ergebnisdarstellung** | Raumtemperatur, Kühlbedarf informativ, drei Spitzenwerte, Bild, Bericht, Vergleich Tagesbilanz/VDI 6007 im Bedarfsdialog (bleibt bis GA), Ausweis „Tagesbilanz (Bestandsweg)", Sommerlüftungsregel und Infiltration/Nutzerlüftung, Wiki-Seite; der Klimaspalten-Schritt M4 ist durch Schemaschritt 95 (19.09.2026) vorweggenommen und umgesetzt (2.3) | ChartProben grün; Sichtabnahme Windows | klein–mittel, 3–5 PT |
| **G3 — Bauteilkatalog** | die acht Tabellen der Schritte 132–134 (6.3), Verwaltungen „Baustoffe" und „Bauteilaufbauten", Zonen- und Bauteildialog im Gebäudedialog, Bauteilweg mit Kettenmatrix-Reduktion und Normnachweis, geneigte Fenster, echte Hülle statt Nachmultiplikation (E40) — **abgeschlossen 25.09.2026** (N1.44–N1.46) | Reduktion trifft die Parameter der Testräume; Bauteilweg = Klassenweg im Grenzfall gleicher U und C | mittel–groß, 8–12 PT |
| **G4 — IFC-Import Stufe 1** | `Xbim.Ifc4` im Kern, `IfcImportAblauf`, `IfcImportSatz`, Zuordnungsdialog, Vorgaben je Baualtersklasse, `Baujahr`, Importprobe KIT, iOS-Trimming-Nachweis | Importprobe bestanden; Windows-Nachweis; iOS-Lauf nach Rückfrage | mittel–groß, 10–20 PT (mit G3-Anbindung +5) |
| **G5 — Geometrieableitung, gbXML** | eigene Auswertung von `IfcExtrudedAreaSolid` und Placement-Kette, Öffnungsabzug; gbXML-Leser | nur bei Bedarf aus der Praxis | groß, 30–60 PT; gbXML 10–15 PT |
| **GA — Altweg ablösen** (letzte Stufe) | Modul `Altweg/`, Weiche, `IGebaeudeRechenweg`, Modultrennungswache, Schalter „Rechenweg", Abschnitt „Tagesbilanz (Bestandsweg)", Spalte `Gebaeude_Modell` und die nur vom Altweg gelesenen Spalten (`DROP COLUMN` je Tabelle und Sichtneubau; dabei `Fensterflaeche_Ost`/`_West` einmalig aus `Fensterflaeche_Ost_West` füllen, 6.1), `Tab_DBTagV`/`Tab_DBTagVDaten`, Vergleich alt/neu samt `modellErzwungen`, Ausweis und Kältebedarf-0-Hinweis, der AK-Sonderfall „feste Last", die Schreibstellen der Flags `Wochenende`/`Ferien`, Referenzprojekt auf VDI 6007 umstellen, Rückweg-Test einstellen, Basis neu einfrieren. Umfang: Q25 (E27: vollständige Ablösung nach der Löschliste), N1.25 Punkt 4, N1.31; die Löschliste führt das [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) | **Ausbauprobe** grün: ein Bau mit umbenanntem Ordner `Altweg/` übersetzt nach Entfernen der Weiche, und der Referenzlauf aller Projekte ohne Altweg-Gebäude bleibt byte-gleich; eigener, begründeter Einfrierschritt | **5–8 PT; beauftragbar, sobald die vier Bedingungen aus Q24 erfüllt sind (E27; Prüfung mit jeder Abnahme, Stand in der Statusdatei), in keiner Summe** |

Aufwände sind Größenordnungen für Entwicklung und Nachweis; Agentenarbeit verkürzt die
Kalenderzeit, nicht die Prüfzeit. **Summen:** G0+GB 3–6 PT; G0–G1 13–22 PT; G0–G2
16–27 PT; G0–G3 24–39 PT; G0–G4 34–64 PT — die 5–8 PT der Stufe GA stecken in **keiner**
dieser Summen. Die **verbindlichen** Aufwände führt das
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) in Kapitel 4;
es misst die Stufen gegen den Bestand und liegt deshalb über den Größenordnungen dieser
Tabelle. Bei Abweichung gilt das Umsetzungskonzept.

---

## 12. Größenordnungen

- **Rechenzeit:** rund 5 ms je Gebäude und Jahr, Planungsgröße 10 ms (5.13); die
  dreizehn Referenzprojekte bleiben mit unter 0,2 s deutlich unter
  den 4 s des heutigen Gesamtlaufs.
- **Genauigkeit:** elf der zwölf Normtestfälle liegen vollständig im Band nach E10, 35 von
  36 Prüfungen bestehen (5.2). Die Jahresheizwärme
  realer Gebäude hängt weit stärker an Randbedingungen als an der Modellstruktur — gemessen:
  RC-Aufteilung ≤ 0,1 %, Verschattung 2,7 % je 0,2, Rahmen und Verschattung zusammen 6,4 %,
  Erdreich 2–14 %, Bestandsgewichte 13–21 % (5.9, 5.10).
- **Abweichung zum Tagesmodell:** je Projekt +7 bis +33 % Jahresenergie — mehr als
  vollständig Parametrierung, gegenläufig 5–13 % Modellstruktur; Spitzenlast je Projekt
  −10 bis +57 % (Stunde), in der Summe +29 %, als Tagesmittel +18 %.
- **Datenbank:** zwölf Gebäudespalten je Tabelle (G1) und drei weitere mit G2 — nach U5 in
  **einem** Schritt (M3): **15 Spalten je Tabelle, 30 `SchemaSpalte`-Einträge** (6.1); die
  Klimaspalten (M4) sind bereits umgesetzt (Schemaschritt 95: drei in `Tab_Solar(_STAMM)`, zwei in
  `Tab_Klimaregion(_STAMM)`; Schritt 97: `Szenario`, `Bezugsjahr`); dazu acht Tabellen (G3, Schritte 132–134); eine
  Spalte und Herkunftskennzeichen (G4). `Gebaeude_Modell` und die Spalten, die nur der Altweg liest,
  bleiben bis zur Stufe GA (E23, E26; Befund X, 6.1).
- **Aufwand der Trennung (E20, 16.09.2026):** rund **3–5 PT** zusätzlich in G1 für
  Verschiebung des Altwegs, Fassade, Vorbereitungsschritt und den byte-gleichen Nachweis und
  rund **1 PT** für den Abschnitt „Tagesbilanz (Bestandsweg)" im Dialog; das Ablösen des
  Altwegs kostet später **5–8 PT** und steckt in keiner Summe (Stufe GA, Kapitel 11). Der
  Dialogumbau selbst war mit E2 und E13 bereits geplant. Verbindlich sind die Aufwände des
  [Umsetzungskonzepts](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Kapitel 4.
- **Abhängigkeiten:** G0–G3 ohne NuGet; G4 `Xbim.Ifc4`/`Xbim.IO.MemoryModel` (rund 10 MB
  Assemblies, rein verwaltet).

---

## 13. Fragen mit Empfehlung

| Nr. | Frage | Empfehlung |
|---|---|---|
| **Q1** | Modellwahl je Gebäude mit Vorgabe Tagesbilanz — oder VDI 6007 als Vorgabe für neue Gebäude? | **Je Gebäude, Vorgabe Tagesbilanz**; Vorgabe erst umstellen, wenn G2 abgenommen und die Wiki-Seite steht — **siehe Nachtrag 1 (entschieden: Stundenwerte)** — (E20/E23: Altweg als eingefrorener Bestandsweg in eigenem Modul) |
| **Q2** | Quelle der Normreferenzwerte: die Richtlinie (liegt VDI 6007-1 bei INEKON vor?) oder die AixLib-Validierungsmodelle (überarbeitete BSD-Lizenz, 5.1)? Dürfen die Normzahlen in ausgelieferten Tests stehen? | **AixLib mit Quellenvermerk und Lizenzwortlaut** für die Tests (so lief der Prototyp); **die Richtlinie beziehen** — für die Zitierfähigkeit der Zahlen und den Nachweis der Bauteilreduktion (G3); bis dahin bleiben die Referenzreihen interne Prüfdaten — **siehe Nachtrag 1 (Richtlinie liegt vor)** |
| **Q3** | Thermische Masse in G1 aus der Bauweise-Klasse mit Aufteilung 0,3 / 0,7 — oder gleich Schichtaufbau? | **Klassenweg in G1** (gemessen unkritisch, ≤ 0,1 %); Schichtaufbau in G3 — **siehe Nachtrag 1 (entschieden 16.09.2026)** |
| **Q4** | Erdreich: Kusuda (harmonische Amplitude) als Vorgabe, Keller und Außenluft als Feldwerte? | **Ja**; der Bestandsfaktor 0,45 wird nicht nachgebaut — **siehe Nachtrag 1 (entschieden 16.09.2026)** |
| **Q5** | Opake Außenbauteile mit Absorption und Abstrahlung in G1? | **Aus (Parität), Schalter vorhanden**; die Pauschale −3 K ist verboten; Normformel in G2 nachrüsten — **siehe Nachtrag 1 (entschieden 16.09.2026)** |
| **Q6** | Fensterpfad im Netz (Teil des Lüftungszweigs, wie im Prototyp, oder eigener Oberflächenknoten)? | **Wie im Prototyp** (validiert über Testfälle 5, 8, 9); ein Fensterknoten erst mit G3 — **siehe Nachtrag 1 und N1.19 (entschieden 16.09.2026, E14: Normweg in G1, eigener Fensterknoten frühestens G3)** |
| **Q7** | `Heizleistung_Max` als Option; wie werden die drei Spitzenwerte je Gebäude neben `Waermelast_Max` geführt? | **Option ja, NULL = unbegrenzt. `Waermelast_Max` bleibt unverändert das Maximum des Kanalsummenvektors** (`SimulationWaermebedarf.cs:401`), damit Dauerlinie, Deckung und Anzeige eine Basis behalten; Stundenspitze, gleitendes Tagesmittel und 95-%-Quantil werden **zusätzlich je Gebäude** ausgewiesen und im Bericht daneben gestellt. Wer die Aufheizspitze nicht auslegen will, setzt `Heizleistung_Max`; Rückrechnung ohne Grenze — **siehe Nachtrag 1 (entschieden 16.09.2026)** |
| **Q8** | Kühlung als vierter Kanal? | **Nein — informativ** (Kühlenergie, Stunden); ein Kanal ist ein eigenes Konzept — **siehe Nachtrag 1 (entschieden 16.09.2026, E12: Kühlung wird aufgenommen)** |
| **Q9** | IFC-Bibliothek: xBIM (CDDL-1.0 mit Quelltextpflicht für die ausgelieferten CDDL-Dateien, 7.4) oder GeometryGymIFC (MIT)? | **xBIM als NuGet-Paket, nie geforkt**; Lizenzentscheid dokumentieren, Lizenztext und Quellenverweis ins Installationspaket |
| **Q10** | IFC-Import auch auf iOS? | **Ja, Kern-Weg ist plattformfrei**; Trimming-Nachweis im iOS-Lauf nach Rückfrage; Größenlimit 50 MB — **siehe Nachtrag 1 und N1.23 (entschieden 16.09.2026, E18: ja, wie empfohlen; die iOS-Zahl wird in G4 gemessen)** |
| **Q11** | Pflichtfelder des Imports und Vorgaben je Baualtersklasse (TABULA/IWU)? | **Nur Wohnfläche und Raumhöhe Pflicht**, Rest Vorgabe mit Herkunftsmarke — **siehe Nachtrag 1 (entschieden 16.09.2026, E13: Nutzfläche statt Wohnfläche; Q11a mit E19, N1.24: die Spalte heißt künftig `Nutzflaeche`)** |
| **Q12** | KIT-Datei `AC20-FZK-Haus.ifc` (2,5 MB) als Importprobe ins Repositorium (`Referenzlaeufe/Importproben/`)? RWTH- und bim2sim-Dateien nur nach Lizenzklärung? | **KIT ja** (Quellenvermerk), **RWTH/bim2sim nein**, bis die Nutzung geklärt ist — **siehe Nachtrag 1 (entschieden 16.09.2026)** |
| **Q13** | gbXML? | **G5, erst bei Bedarf aus der Praxis** — **siehe Nachtrag 1 (entschieden 16.09.2026; durch E9 überholt)** |
| **Q14** | Neues Referenzprojekt mit VDI 6007 in der Testdatenbank und Neu-Einfrieren der Basis mit G1? | **Ja** — sonst ist das Modell im Regressionsnetz unsichtbar |
| **Q15** | Reihenfolge G0 → G1 → G2 → G3 → G4 → G5; G0 und G1 als erste Beauftragung? | **Ja**, G0 zuerst — ohne bestandene Normtests keine Anbindung — **siehe Nachtrag 1 (entschieden 16.09.2026)** — (E20/E23: Altweg als eingefrorener Bestandsweg in eigenem Modul) |
| **Q16** | Die Bestandsgewichte 0,83 / 0,95 / 0,45 / 0,83 im VDI-6007-Weg streichen? | **Ja** — sie sind Kalibrierung, keine Physik; der ungewichtete Weg trifft die Katalogkennzahl zu 86–100 % |
| **Q17** | Verschattungsfaktor und Rahmenanteil als Felder mit Vorgabe je Lage (0,9 / 0,8 / 0,7) bzw. 0,3? | **Ja** — der größte geratene Hebel (6,4 %); eine Vorgabe je Baualter erst, wenn `Baujahr` vorliegt (G4) — **siehe Nachtrag 1 (entschieden 16.09.2026)** |
| **Q18** | Harte Plausibilitätsprüfungen (4.8) im VDI-6007-Weg; im Tagesmodell nur Warnung, um die Basis nicht zu berühren? | **Ja, so** — **siehe Nachtrag 1 (entschieden 16.09.2026)** — (E20/E23: Altweg als eingefrorener Bestandsweg in eigenem Modul) |
| **Q19** | Sommerlüftungsregel und Trennung Infiltration/Nutzerlüftung in G2? | **Ja, G2** — für die Jahresheizwärme unerheblich, für die Überhitzungskennzahl entscheidend — **siehe Nachtrag 1 (entschieden 16.09.2026)** |
| **Q20** | Fassadenstrahlung im Gebäudemodell mit Hay-Davies aus GHI/DNI/DHI statt der isotropen `Sol_*`-Spalten? | **Ja** — Nord ist isotrop rund 25 % zu hoch; die Spalten bleiben dem Altweg — **siehe Nachtrag 1 (entschieden 16.09.2026, N1.17)** — (E20/E23: Altweg als eingefrorener Bestandsweg in eigenem Modul) |
| **Q21** | Eine Zeitbasis (Ortszeit) für das Gebäudemodell, wie PV und Solarthermie? | **Ja** — das Modell braucht `Tab_Klimadaten` nicht — **siehe Nachtrag 1 (entschieden 16.09.2026)** |
| **Q22** | `Bauweise` von Gebäude 10576 in der Testdatenbank auf 15 200 Wh/K korrigieren (Projekt 1008 ändert sich, Basis neu einfrieren) und dafür eine vierte Einfrierregel „gesäte Gebäudedaten" anlegen? | **Ja, im Einfrierschritt GB** (Kapitel 11) |
| **Q23** | Statischen Zustand `_prevRoomTemp` im Bestand beheben (Instanzzustand, `ResetState` je Gebäude)? Das ändert Projekte mit mehreren Gebäuden und damit die Basis — gemessen in GB allein 1008, 1039 bleibt byte-gleich (10.4) | **Ja, aber als eigener, begründeter Einfrierschritt** — nicht still mit G1 |
| **Q24** | Wann ist der VDI-Weg bewährt genug, dass die Stufe **GA — Altweg ablösen** beauftragt wird (Modul, Weiche, Schalter „Rechenweg", Altweg-Spalten, Übergangs-Referenzprojekt, Neu-Einfrieren)? | **entschieden (E27, 22.09.2026, N1.32): Option (a), das Ablösekriterium** — GA wird beauftragbar und fällig, sobald alle vier Bedingungen erfüllt sind: (1) alle Referenz- und Bestandsprojekte des Anwenders sind einmal auf VDI 6007 gerechnet und die Abweichung zum Altweg ist je Projekt erklärt; (2) eine Feldphase von mindestens einer Heizperiode ohne offenen Fehler am VDI-Weg; (3) KU1 und, falls beauftragt, AK1 sind abgenommen; (4) die Ausbauprobe ist grün (4.1); geprüft wird mit jeder Abnahme, der Stand steht in der Statusdatei |
| **Q25** | Umfang der Stufe GA (Befund X): Bleibt `Typ` (Gebäudetyp mit Tagesverteilung) als Katalogmerkmal in der Hauptstruktur, und was wird aus `Tab_DBTagV` und dem `GebaeudetypDialog`, wenn der Altweg als einziger Rechenleser entfällt? Fallen die leserlosen Spalten `WW_Bedarf` und `Waermebedarf` mit dem GA-Schemaschritt? | **entschieden (E27, 22.09.2026, N1.32): Option (a), vollständige Ablösung nach der Löschliste** (Umsetzungskonzept Kapitel 6), im Sinne von N1.25 Punkt 4 und der Stufenzeile GA in Kapitel 11: `Typ` und die Tagesverteilungstabellen fallen mit dem Altweg, die Altweg-Spalten mit einem Schemaschritt (`DROP COLUMN` je Tabelle, Sichtneubau), und die leserlosen Spalten `WW_Bedarf` und `Waermebedarf` gehen im selben Schritt mit |
| **Q26** | Stufenplan der Anlagenkopplung (E22): Welche Stufen werden beauftragt und wann — **AK1** (Heizkreis als Randbedingung) nach G2, **AK2** (Erzeugerfahrplan als Verfügbarkeit) und **AK3** (geschlossener Kreis) danach? | **AK1 nach G2 einplanen; AK2 nach abgenommenem AK1 und einer Feldphase des VDI-Wegs, AK3 danach**; Aufwand nach dem Papier (Rev. 2): AK0 1–2 PT, AK1 **10–15 PT**, AK2 11–15 PT, AK3 23–38 PT, AK0–AK3 zusammen **45–70 PT** zuzüglich rund 0,5 PT je Einfrierschritt, in keiner Summe von Kapitel 11; jede Stufe je Gebäude oder Projekt wählbar mit Vorgabe aus und eigenem Einfrierschritt — **entschieden (E27, 22.09.2026, N1.32) nach Empfehlung (a)**; AK3 bleibt nach H6 (E24) erst nach der Feldphase von AK1 und AK2 zugesagt (E22, E23, N1.27; eigenes Papier [`Anlagenkopplung`](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md)) |

---

## 14. Risiken

| Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|
| Testfall 11 offen (zwei Umschaltstunden 3,4 W bzw. 2,8 W neben dem Band, 5.2) | der Löser trifft den Wechsel Heizen → Kühlen rund 45 s zu früh; mit Flächenkühlung wäre das rechenwirksam | Ursache in G0 klären (getrennter Knoten für die Kühldecke) oder als benannte Grenze ausweisen; der Normfalltest weist die Reserve je Fall aus |
| Normreferenzwerte nur über AixLib (Lizenz, Zitierfähigkeit, 5.1) | Tests dürfen die Zahlen nicht ausliefern | Richtlinie beziehen; Referenzreihen bis dahin interne Prüfdaten (Q2) |
| Mehrfaches Neu-Einfrieren der Basis (GB, G1 + G2, G6d — Zonenprojekt, siehe [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md)) | Regressionsnetz zeitweise ohne belastbare Basis | jeden Einfrierschritt einzeln begründen; die Verschiebung des Altwegs ist byte-gleich und kein Einfrierschritt; Reihenfolge Kapitel 16 |
| Zwei Rechenwege nebeneinander — **bis GA** (E23, E26) | Referenzprojekt und Rückweg-Test in jeder Basis, gemeinsam genutzte Stellen zweifach zu prüfen; jede Stufe, die einen Altweg-Sonderfall einführt, verlängert die Löschliste | Modultrennung, Modultrennungswache und Ausbauprobe halten die Kosten klein; die Löschliste im Umsetzungskonzept hält die Stufe GA beauftragbar (Q24) |
| CDDL-Auflagen für xBIM (7.4) | G4 ohne Bibliothek, wenn die Freigabe fehlt | NuGet-Einbindung ohne Fork; MIT-Ausweg GeometryGymIFC (Q9) |
| iOS-Trimming mit xBIM (7.4) | IFC-Import auf iOS nicht lauffähig | `TrimmerRootDescriptor`, früher iOS-Lauf nach Rückfrage (Q10) |
| Keine Validierung an Messwerten (5.14) | Katalogkennzahl bleibt die einzige äußere Referenz | ein Projekt mit gemessenem Verbrauch in die Testdatenbank aufnehmen, sobald eines vorliegt |
| Nachmultiplikation der Hülle mit Faktor 0,25–4,6 (3.6) | Kapazität und Strahlungsaustausch physikalisch unscharf | echte Hülle mit G3/G4; bis dahin im Bericht ausgewiesen |
| Überhitzungsstunden ohne Nutzerlüftung (bis 1 425 h, 5.7) | Kühlkennzahl in G1 überzeichnet | Sommerlüftungsregel in G2; Kennzahl bis dahin als vorläufig gekennzeichnet |

---

## 15. Abgrenzung — was dieses Papier nicht behandelt

Mehrzonenmodelle und Nachbarräume (Testfall 10 wird als Test gebaut, nicht als Funktion),
Feuchtebilanz, Kühlung als Kanal und Kältemaschinen (**aufgehoben durch E12, N1.18** — Regelung im
Kühlkonzept), Flächenheizsysteme als
Bauteilaktivierung (Testfall 11 nur als Test), die Kopplung von Vorlauftemperatur und
Erzeugerfahrplan an die Raumtemperatur (**seit E22, N1.27 eine benannte Erweiterung mit eigenem
Papier** — [Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md), Stufen AK1 bis AK3 nach G2 bzw. nach AK1 (Q26, E27); Aufwand nach
dessen Rev. 2 für AK1 rund 10–15 PT, für AK0–AK3 rund 45–70 PT; kein Bestandteil der
Stufen G0 bis GA), sommerlicher Wärmeschutz als Nachweis nach
DIN 4108-2, Nachweise nach GEG/DIN V 18599, Verschattung durch Nachbarbebauung (nur als
Faktor), Lüftung mit Wärmerückgewinnung (kann als wirksamer Luftwechsel eingegeben werden),
Nutzungsprofile für Nichtwohngebäude (SIA 2024 / DIN V 18599-10), Scan-to-BIM-Aufnahmen,
gbXML-Details, die Validierung an gemessenen Verbräuchen (dafür fehlen Daten im Repositorium).

Ebenfalls nicht behandelt: **ein vollwertiger 3D-IFC-Betrachter mit Geometriekernel** — benannt
abgelehnt; was stattdessen gebaut wird, steht in Nachtrag N1.16 (Entscheid E11).

---

## 16. Reihenfolge

1. Entscheide Q1–Q26 — mit E27 (22.09.2026) sind alle entschieden.
2. **G0** in einem Worktree: Löser, Normtests, Rechenproben, Klärung Testfall 9/10; Abnahme
   Kern-Filter grün.
3. **GB**: Warnungen im Tagesmodell, Instanzzustand statt `_prevRoomTemp`, Korrektur 10576,
   vierte Einfrierregel — eigener, begründeter Einfrierschritt, **vor** der Verschiebung des
   Altwegs, damit das verschobene Modul der geprüfte Stand ist (E20, 16.09.2026).
4. **G1**: zuerst den Tagesbilanz-Weg Zeichen für Zeichen nach `Altweg/`, dazu Fassade und
   modellfreier Vorbereitungsschritt — eigener Merge, Referenzlauf **byte-gleich**; dann
   der Gebäudespalten-Schritt M3 und Namensleser (eigener Merge, Referenzlauf unverändert), dann Anbindung
   des VDI-Wegs, Prüfungen, Dialoge in VDI-Struktur, Hülle; Referenzprojekte beider Wege,
   Basis neu einfrieren; Statuszeile und Protokoll.
5. **G2**, dann **G3**; **G4** erst, wenn G2 im Feld ist. Wiki-Seite mit G2, Logbuch beim
   Upload.
6. **GA — Altweg ablösen** ist die letzte Stufe (E23, E26). Bis dahin bleibt der Altweg als
   eingefrorener Bestandsweg im Produkt — Modul, Weiche, Schalter „Rechenweg",
   Altweg-Spalten, Referenzprojekt auf dem Altweg und Rückweg-Test. GA wird beauftragbar,
   sobald das Ablösekriterium aus Q24 erfüllt ist (E27, Kapitel 13); der Umfang (Q25) steht in
   Kapitel 11.

---

## Nachtrag 1 (15.09.2026) — Entscheid zu Q1, die Richtlinie liegt vor, Erläuterungen

Anlass: der Anwender hat Q1 entschieden („Stundenwerte"), die drei Blätter der VDI 6007
als PDF bereitgestellt (`Z:\…\Simulation-Gebäudemodell\VDI 6007 Blatt 1/2/3`) und um
Erläuterungen zu Q16, Q9 und Q14/Q22/Q23 gebeten. Die Richtlinie wurde als Text ausgezogen
und gegen die Kapitel 4, 5 und 10 gehalten (Befund I,
[`Gebaeudesimulation/2026-09-15_Befund_I_VDI6007_Richtlinie_Abgleich.md`](Gebaeudesimulation/2026-09-15_Befund_I_VDI6007_Richtlinie_Abgleich.md)).
Die Kapitel 0–16 bleiben als Rev. 1 stehen; was dieser Nachtrag ändert, gilt vor ihnen.

### N1.1 Entscheid E1 — Q1: Stundenwerte als Vorgabe

Anwender, 15.09.2026: „Modellwahl je Gebäude mit Vorgabe Tagesbilanz (Q1): Stundenwerte."
und auf Nachfrage: „feste Entscheidung!". **Entscheid E1, endgültig: das Stundenmodell
VDI 6007 ist das Rechenmodell für alle Gebäude — neue wie bestehende.** Die Tagesbilanz
bleibt nur als bewusst je Gebäude wählbare Ausnahme erhalten (Vergleich, Übergang), sie ist
keine Vorgabe mehr. **E20 (16.09.2026, N1.25) schärft: nur noch als Übergangsweg in einem eigenen
Modul, der mit Stufe GA entfällt.** Damit kehrt sich die NULL-Semantik von 6.1 um: `Gebaeude_Modell` NULL =
`VDI6007`; die Tagesbilanz ist der ausdrücklich gesetzte Wert `TAGESBILANZ`. Der
Migrationsschritt schreibt **nichts** in bestehende Zeilen; alle Gebäude folgen der Vorgabe.

Folgen:

- **Bestehende Projekte liefern nach dem Update andere Zahlen** — Jahresheizwärme je
  Projekt +7 bis +33 % gegenüber dem Tagesmodell (5.5), andere Spitzenlast, dazu
  Kühlbedarf und Überhitzungsstunden als neue Kennzahlen. Das ist gewollt und wird im
  Wiki-Logbuch mit Version und Begründung veröffentlicht; der Bedarfsdialog zeigt beide
  Wege nebeneinander (8.2), damit der Unterschied je Gebäude erklärbar ist.
- **Die Referenzbasis wird mit G1 vollständig neu eingefroren:** alle dreizehn
  Referenzprojekte rechnen stündlich. Das Regressionsnetz für den Bestandsweg bleibt
  trotzdem erhalten — ein Test setzt jedes Referenzprojekt ausdrücklich auf
  `TAGESBILANZ` und hält es gegen die letzte Bestandsbasis (nach GB); die Abnahmesperre
  „Bestandsprojekte unverändert" aus 10.4 gilt damit für den ausdrücklich gewählten
  Bestandsweg, nicht mehr für die Vorgabe.
- **G2 gehört in dieselbe Auslieferung wie G1**, weil jedes Gebäude stündlich rechnet:
  Ergebnisdarstellung, Sommerlüftungsregel und Infiltration/Nutzerlüftung, Wiki-Seite.
  Ohne sie sähe der Anwender Kühlbedarf und Überhitzungsstunden ohne Erklärung.
- **GB rückt vor G1:** die Bestandsbefunde (Warnungen, Instanzzustand, Korrektur 10576,
  vierte Einfrierregel) werden zuerst als eigener Einfrierschritt auf dem Bestandsweg
  abgenommen; erst danach folgt die große Neueinfrierung mit G1. So bleibt getrennt
  nachvollziehbar, was der Bestand und was das neue Modell verändert hat.
- Kapitel 16 liest sich neu: **G0 → GB → G1 + G2 gemeinsam → G3 → G4 → G5.**

### N1.2 Q2 — die Richtlinie liegt vor

- **Ausgaben:** Blatt 1 (Raummodell) 2015-06, Blatt 2 (Fenstermodell) 2012-03, Blatt 3
  (solare Einstrahlung) 2015-06, jeweils Weißdruck, Bezug über Beuth-Abonnements
  (Fingerabdruck je Seite, DRM). Die Lizenzierung ist plausibel; die PDFs sind
  personalisiert, jede Weitergabe rückverfolgbar.
- **Die Referenzwerte stehen in der Richtlinie:** Blatt 1, Anhang A1, Tabellen A1.3 bis
  A12.3 (Seiten 41–63), je Testfall **zwei Programmspalten**, Tag 1, 10 und 60, Stunden
  1–24, Lufttemperatur, operative Temperatur und Heiz-/Kühllast. Die **Prüfregel** (6.6,
  Seite 31): Ergebnis im **Band zwischen Programm 1 und 2, ± 0,1 K bzw. ± 1 W**; die
  Prüfgröße ist das **Blockmittel der Stunde** (Seiten 24 und 38), nicht der Momentanwert
  und nicht ein gleitendes Mittel. Seite 38 nennt einen **Datenträger mit
  Excel-Arbeitsmappen** aller Eingaben und Ergebnisse — zu prüfen, ob er zum Abonnement
  gehört; dann ist die Übernahme ein Import statt 1 728 bis 5 200 abgetippter Zahlen.
- **Neubewertung der Validierung (5.2):** die AixLib-Reihen sind die Normzahlen selbst
  (+273,15 K), aber je Fall nur **eine** der beiden Programmspalten. Gegen das Normband
  gilt: Testfall 9 bestanden (Band 41,2–41,5 °C, Prototyp 41,436), Testfall 10 bestanden
  mit 0,057 K Reserve — die „knappe" Bewertung war ein Artefakt des Einspaltenvergleichs.
  **Testfall 11 fällt durch** (Tag 60, Stunde 10: −126,3 W gegen Band −120 … −123 W, 3,3 W
  daneben); die Nachbildung des 120-s-Messfensters aus 5.2 entfällt, der Fall wird in G0
  gelöst (Verdacht: α_kon der Kühldecke 5,0 je Bauteil und die Zuordnung des
  Flächenkühlanteils als Strahlungsquelle auf IW, Gl. (51)/(53), Seite 22–23).
  **Testfall 6:** die Norm definiert Heizlast **positiv** (Seite 9, Tabelle A6.3); die
  AixLib-Reihe für Fall 6 ist gedreht, 5.3 („Testfall 6 negativ = Heizen") ist normwidrig.
  Betragsmäßig liegt der Prototyp mit 765,48 W 0,48 W außerhalb des Bands 763–765 W —
  grenzwertig im Rundungsband der ganzzahligen Tabelle. Stand damit: **zehn Fälle sicher,
  Fall 6 grenzwertig, Fall 11 offen.**
- **Rechtslage:** die interne Validierung mit der lizenzierten Ausgabe ist der
  bestimmungsgemäße Gebrauch. Das **Ausliefern der Normzahlen in Testdateien** eines
  Produkts ist eine Vervielfältigung (Vorbemerkung Seite 2) und bedarf einer Klärung mit
  dem VDI. Deshalb: Normzahlen als **nicht ausgeliefertes Prüfmittel** führen — lokal
  beizustellende Datei außerhalb des Installationspakets, im Repositorium nur mit
  Zugriffsbeschränkung oder gar nicht; die ausgelieferten Tests tragen nur Abweichungen
  und Bestanden-Kriterium. AixLib entfällt als Referenzquelle; die nachgebauten Formeln
  (θ_eq, Strahlungsverteilung) werden gegen Blatt 1 Gl. (32)–(46) belegt.
- **Blatt 2 und 3 brauchen fremde Prüfbeispiele:** Blatt 3, Abschnitt 13 verweist auf VDI
  2078 (Testbeispiele 7–10) bzw. VDI 6020 (8–10); Blatt 2 auf seinen Anhang A5 und den
  Datenträger. Für einen normkonformen Nachweis des Strahlungswegs (G1/G2) ist **VDI 6020
  oder VDI 2078 zu beschaffen** — neuer Beschaffungspunkt neben Q2.
- **Q2, neue Fassung:** Referenzwerte aus der Richtlinie, Prüfregel Band ± 0,1 K / ± 1 W,
  Quellenangabe „VDI 6007 Blatt 1:2015-06, Tabelle An.3"; Datenträger prüfen; Normzahlen
  nicht ausliefern; VDI 6020/2078 beschaffen.

### N1.3 Korrekturen am Rechenweg aus der Richtlinie

| Stelle | Befund aus Blatt 1–3 | Folge |
|---|---|---|
| 4.2, Bezeichnungen | „7R2C" steht nicht in der Richtlinie; sie sagt **2-K-Modell** und benennt R_1,IW, R_1,AW, R_Rest,AW, R_α;kon;IW, R_α;kon;AW, R_α;str;AW/IW (Dreieck, per Stern-Dreieck-Transformation Gl. (55)–(57)) und R_Lue | Normbezeichnungen in Code und Doku; „7R2C" nur als Kurzform |
| 4.2, h_conv | α_kon ist **je Bauteil** vorzugeben (Seite 10): Testräume 1,7 Boden/Decke, 2,7 Wände/Fenster, 5,0 Kühldecke; ein globales 2,7 trifft Testfall 11 nicht | R_conv,AW und R_conv,IW als Parallelschaltung über die Bauteile; 2,7 nur Vorgabe für Wände im Klassenweg; G0 |
| 4.2, Fensterpfad (Q6) | die Norm führt Fenster **im AW-Zweig** (R_1,AF = R_AF/6, Gl. (25)–(28), parallel nach den Wänden) und in θ_A,eq,gew (Gl. (41)); der Prototyp koppelt Fenster direkt an die Luft | Q6 ist durch die Testfälle **nicht** belegt (sie liefen nach 5.3 über den Normweg); der Klassenweg von G1 bleibt als bewusste Abweichung erlaubt, G3 stellt auf Gl. (25)–(28) um — **mit E14 gilt der Normweg schon in G1**, der Klassenweg von G1 führt die Fenster nach Gl. (25)–(28) (N1.19) |
| 4.2, „gleitende Stundenmittel" | Norm: **Blockmittel** je Stunde („n-te Stunde") | Wort „gleitend" streichen (gilt auch für die Spitzenlast-Kennzahl in 4.5: „Tagesmittel" = Mittel über 24 Blockstunden) |
| 4.3, Innenbauteile | „symmetrisch bis zur Mittelebene" droht doppelt zu halbieren: die Norm baut die Kettenmatrix über den **vollständigen** Aufbau (Gl. (11)) und reduziert erst danach (Seite 14) | Formulierung ersetzen; G3 |
| 4.3, Nachweis der Reduktion | die Richtlinie nennt **keine** Soll-RC-Werte (die Zahlen aus 1.1 stammen aus AixLib); sie gibt Schichtaufbau (Tabellen A.1.1 Typraum S, A.3.1 Typraum L) **und** Ergebnisreihen | Nachweis in G3: Reduktion nach Gl. (11)–(17) aus A.1.1/A.3.1, Simulation, Treffen von A1.3/A3.3 im Band; Bezugsperioden 7 Tage je Bauteil (2 Tage bei raumseitig abgedeckter Speichermasse), 5 Tage für den Raum |
| 4.4, θ_eq | F_r ist der **geometrische** Sichtfaktor φ = (1 + cos γ_F)/2 (Gl. (36a)); die Bewölkung steckt allein in der Gegenstrahlung E_Atm der Klimadaten (Blatt 3 Gl. (85)); für **transparente** Flächen entfällt der kurzwellige Term (Gl. (39)); α_A = α_kon,A + α_str,A (Gl. (38)) | Formulierung korrigieren; Gl. (32)–(38) zitieren |
| 4.4, α = 0,6 | kein Normwert; Testfälle 8/9 rechnen a = 0,70 und ε = 0,90 | als EPOS-Vorgabe deklarieren, Feld je Bauteil in G3 |
| 4.4, 9 % konvektiv | a_kon = 0,09 gilt **nur für die Testbeispiele** (3-fach-Wärmeschutzverglasung); Blatt 2, Tabelle A5 nennt je Verglasung 0,02 (Einfachglas) bis 0,09 (3-fach) und mit innen liegendem Sonnenschutz bis 0,52 | a_kon je Verglasung/Sonnenschutz aus Blatt 2 Tabelle A5/A6; 0,09 nur Vorgabe für 3-fach-Wärmeschutz |
| 4.4, Rahmenanteil F_F | kein Begriff der VDI 6007; Blatt 2 schließt Rahmen ausdrücklich aus (Abschnitt 9); Testfälle 0 % | als EPOS-Vorgabe außerhalb der Norm kennzeichnen (Quelle DIN V 18599) |
| 4.4, F_W = 0,9 | Norm: winkelabhängige Korrektur korg getrennt für direkt, diffus klar, diffus bedeckt, Boden (Blatt 3, 8.1, Gl. (59)–(61)) | 0,9 als Näherung; korg in G3 |
| 4.4, F_S | Verschattung ist in Blatt 3, Abschnitt 12 geometrisch geregelt | Pauschalfaktoren als Vereinfachung kennzeichnen |
| 4.4, Erdreich (Q4) | VDI 6007-1 hat **kein** Erdreichmodell; erdberührte und kellerangrenzende Bauteile laufen über θ_NR,eq (Gl. (40)) mit **vorzugebender** Nachbarraumtemperatur | Kusuda ist eine EPOS-Ergänzung außerhalb der Norm (verletzt sie nicht); Quelle nennen; `KELLER` = θ_NR,eq mit vorgegebener Kellertemperatur statt Faktor 0,5 |
| 4.4, H_ve | Testbeispiel 12 schreibt c·ρ = 1,1953 kJ/(m³K) = 0,332 Wh/(m³K) vor; der Bestandswert 0,3333 liegt näher an der Norm als 0,34 | Normfälle mit 1,1953; für Projekte Quelle des 0,34 (DIN EN 12831) nennen; das Argument „Zahlenwechsel hebt H_ve um 2 %" entfällt |
| 4.4 / 2.3, Strahlungsmodell (Q20) | Blatt 3 schreibt **Aydinli/Krochmann** vor: bedeckter Himmel rotationssymmetrisch, klarer Himmel anisotrop, Mischung über die Sonnenwahrscheinlichkeit aus dem **Bedeckungsgrad**; Albedo 0,2 (Regelwert); Koordinaten des TRY-Referenzorts, nicht des Projektorts | isotrop (Bestand) ist nicht normkonform, **Hay-Davies auch nicht** — als bewusste Abweichung führen; der Bedeckungsgrad fehlt in `Tab_Solar` (PVGIS liefert ihn nicht), Normkonformität wäre erst mit einer TRY-Quelle erreichbar |
| 4.5 / 5.3, Vorzeichen | Heizlast positiv (Seite 9) | Konvention übernehmen; AixLib-Reihe für Fall 6 beim Einlesen spiegeln |
| 5.1 | „die Richtlinie selbst lag nicht vor" | überholt, siehe N1.2 |
| 5.2 / 10.1, Prüfschwelle | 0,15 K / 1,5 W gegen eine Reihe (AixLib) | **Band P1…P2 ± 0,1 K / ± 1 W** nach 6.6; Fußnote 1 zu Fall 11 streichen |
| 4.8 | Blatt 1, **Abschnitt 6.4, Seite 27**: E = 0 ab Z > 170 als Abschneidegrenze des abklingenden Exponentialterms (**Unterlauf**, nicht Überlauf — alle Eigenwerte sind negativ). Der Programmierhinweis 6.8 (Seiten 36–37) trägt anderes: das Verbot des stillen Rückfalls und die Behandlung der Division durch null | deckungsgleich; Grenze für exp(−Z) in G0 übernehmen ([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 1.3 und 5) |

Bestätigt hat die Richtlinie: die zwei Massenknoten mit voll besetzter Systemmatrix
(Bild 3, Seite 17), Stundenschritt mit Stundenmitteln, θ_op als Mittel aus Luft- und
flächengewichteter Oberflächentemperatur (Gl. (103)), die flächenproportionale Verteilung
der Strahlungslasten mit Ausschluss der Fensterfläche (Gl. (43)–(46)), h_rad = 5 innen
(Gl. (30)), h_a = 25 als Summe, die U·A-gewichtete äquivalente Außentemperatur
(Gl. (41)/(42)), die ideale Regelung mit Sollwerthaltung über die Stunde (Gl. (96)–(102)).

**Vermerk 22.09.2026:** Die Zeile 4.8 oben schreibt dem Programmierhinweis 6.8 das Verbot des
stillen Rückfalls zu. Das trifft nicht zu: 6.8 verlangt nur, Divisionen durch null bei fehlenden
Bauteilgruppen auszuschließen. Das Verbot des stillen Rückfalls ist Hausregel; berichtigt im
Hauptteil 4.8 und in den [Rechenschritten](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(A4, A7a, 5, 7.1).

### N1.4 Erläuterungen

**Q16 — die Bestandsgewichte im neuen Weg streichen.** Der heutige Rechenweg multipliziert
die Transmissionsverluste nicht mit dem echten U·A, sondern mit 0,83·U·A für die
Außenwand, 0,95 für das Dach, 0,45 für die Bodenplatte und 0,83 für die Wärmebrücken
(`BhkwPlan.cs:347-353`); Fenster und Sonstiges gehen voll ein. Die Faktoren stammen aus
BHKW-Plan, eine Herleitung gibt es nicht; sie ähneln den Temperatur-Minderungsfaktoren der
Heizlastnorm (Erdreich, unbeheizte Räume) und wirken zugleich als Kalibrierung des
Tagesmodells auf beobachtete Verbräuche. Wirkung: der Verlustkoeffizient ist 13–21 %
kleiner, das Tagesmodell trifft die Katalogkennzahl kWh/m²a nur zu 48–88 %. Der
VDI-6007-Weg rechnet mit den echten U·A und gibt jeder Fläche eine eigene Randbedingung:
Erdreich oder Keller bekommen ihre Temperatur über das Feld `Grundflaeche_Randbedingung`,
Wärmebrücken zählen voll. Faktor **und** Randbedingung zusammen wären eine doppelte
Minderung. Gemessen: ohne die Gewichte trifft der Prototyp die Katalogkennzahl zu
86–100 % (5.5). „Streichen" heißt: **nur im neuen Weg**. Das Tagesmodell behält seine
Gewichte, weil daran die Referenzbasis und die Kalibrierung bestehender Projekte hängen;
der Bedarfsdialog zeigt beide Wege nebeneinander (8.2), damit der Unterschied erklärbar
bleibt.

**Q9 — xBIM unter CDDL-Auflagen.** xBIM ist die freie .NET-Bibliothek, die IFC-Dateien
liest (Essentials: rein verwaltet, läuft auf Windows und iOS). Ihre Lizenz CDDL-1.0 ist ein
**Datei-Copyleft**: nur die Dateien von xBIM selbst bleiben unter CDDL, unser Code bleibt
proprietär, und das Gesamtprodukt darf unter eigener Lizenz ausgeliefert werden (§ 3.5 und
§ 3.6 „Larger Work"). Drei Auflagen bleiben: (1) der Quelltext der ausgelieferten
CDDL-Dateien muss verfügbar sein, auch wenn wir nichts ändern — ein dauerhafter Verweis
auf die xBIM-Quellen (GitHub, NuGet-Quellpakete) im Lizenzhinweis genügt; (2) Copyright-
und Lizenzvermerke bleiben stehen, eigene Änderungen an CDDL-Dateien wären zu kennzeichnen
und offenzulegen; (3) der Lizenztext liegt im Installationspaket bei (Seite
„Lizenzhinweise", wie für andere Fremdbibliotheken). Praktische Regel: **xBIM nur als
unverändertes NuGet-Paket einbinden, nie forken oder patchen** — dann gibt es nichts
offenzulegen. Was nicht geht: `Xbim.Geometry`, weil es Windows-gebunden ist und die
Geometriebibliothek OCCT unter LGPL nachzieht. Der Ausweg, falls CDDL nicht freigegeben
wird: GeometryGymIFC_Core unter MIT (gleiche Aufgabe ohne Geometrie, kleineres Ökosystem).

**Q14, Q22, Q23 — Neu-Einfrieren der Basis mit einer vierten Einfrierregel.** Die
Referenzbasis ist der eingefrorene Ergebnissatz der vierzehn Testprojekte
(`Referenzlaeufe/2026-09-26_R21_BhkwDeckung`; die Befunde dieses Papiers sind gegen die Basis R7 gemessen); jede Änderung am Rechenweg wird gegen sie
gehalten, mit Toleranz 1e‑4 relativ. Sie bleibt nur gültig, wenn sich weder Rechenweg noch
gesäte Daten der Testdatenbank ändern. Für die gesäten Daten nennt die `CLAUDE.md` drei
**Einfrierregeln** — Bereiche, deren Änderung eine neue Basis erzwingt: Emissionsfaktoren,
PV-Modulkoeffizienten, Flottenstand des Projekts 1046. Gebäudedaten fehlen in dieser Liste.
Daraus die drei Fragen: **Q14** — ein neues Referenzprojekt mit VDI 6007 erzeugt neue
Ergebnisse, also eine neue Basis; ohne dieses Projekt prüft kein Test das neue Modell.
**Q22** — die Korrektur des Rückfallwerts `Bauweise = 50 Wh/K` in Gebäude 10576 ändert
Projekt 1008 (+52 % im Tagesmodell); das bricht die Basis, und weil es keine Regel dafür
gibt, geschähe es unbemerkt — deshalb die **vierte Einfrierregel „gesäte Gebäudedaten"**
(`Tab_Gebaeude(_STAMM)`: Bauweise, U-Werte, Flächen, Sollwerte, Luftwechsel, g-Wert),
eingetragen in `Referenzlaeufe/LIESMICH.md` und in den Abschnitt „Regressionsnetz" der
`CLAUDE.md`. **Q23** — der statische Raumtemperatur-Zustand des Bestands macht das Ergebnis
von der Zeilenreihenfolge abhängig; die Behebung ändert Projekte mit mehreren Gebäuden
(1008, 1039) geringfügig, also wieder die Basis. Jeder dieser Punkte wird als **eigener,
begründeter Einfrierschritt** geführt (Stufe GB), damit hinterher nachvollziehbar bleibt,
welche Zahl sich aus welchem Grund geändert hat; ein Einfrierschritt kostet Referenzlauf,
Vergleich, Begründung in `Referenzlaeufe/LIESMICH.md` und einen grünen CI-Lauf.

### N1.5 Was sich an den Entscheiden ändert

| Nr. | Stand nach Nachtrag 1 |
|---|---|
| Q1 | **endgültig entschieden (E1): Stundenmodell für alle Gebäude, auch bestehende**; Tagesbilanz nur als ausdrücklich wählbare Ausnahme; Basis wird mit G1 vollständig neu eingefroren, Bestandsweg über ausdrückliche Wahl weiter regressionsgeprüft |
| Q2 | Referenzwerte aus der Richtlinie, Band ± 0,1 K / ± 1 W, Normzahlen nicht ausliefern, Datenträger prüfen, VDI 6020 oder 2078 beschaffen |
| Q6 | Fensterpfad des Klassenwegs ist eine bewusste Abweichung, nicht durch Testfälle belegt; G3 stellt auf den Normweg um — **mit E14 gilt der Normweg schon in G1** (N1.19) |
| Q20 | Hay-Davies bleibt Empfehlung für G1, aber als Abweichung von Blatt 3 (Aydinli/Krochmann braucht den Bedeckungsgrad) gekennzeichnet |
| Q16 | **entschieden (E2): Gewichte im Stundenmodell gestrichen; der Gebäudedialog zeigt je Bauteil U, A und U·A ohne verdeckte Faktoren** (N1.6) |
| Q9 | **entschieden (E3, nach Empfehlung): xBIM Essentials als unverändertes NuGet-Paket unter CDDL-1.0**, Lizenztext und Quellenverweis im Installationspaket, nie geforkt; `Xbim.Geometry` nicht; Ausweg GeometryGymIFC (MIT), falls die CDDL-Auflagen später nicht tragbar sind (N1.7) |
| Q22, Q23 | **entschieden (E4, nach Empfehlung): Korrektur `Bauweise` 10576 auf 15 200 Wh/K und Instanzzustand statt `_prevRoomTemp` im eigenen Einfrierschritt GB vor G1, dazu die vierte Einfrierregel (gesäte Gebäudedaten)** in `Referenzlaeufe/LIESMICH.md` und `CLAUDE.md` (N1.8); Q14 ist mit E1 erledigt |
| G0 | zusätzlich: Testfall 11 lösen, Testfall 6 klären, α_kon je Bauteil, Vorzeichen, Band-Prüfregel, Normzahlen als nicht ausgeliefertes Prüfmittel |
| Reihenfolge | G0 → GB → G1 + G2 gemeinsam → G3 → G4 → G5 |
| Q14 | entschieden mit E1: alle dreizehn Referenzprojekte werden mit G1 auf das Stundenmodell umgestellt und neu eingefroren; ein zusätzliches Referenzprojekt ist nicht mehr nötig |

### N1.6 Entscheid E2 — Q16: Bestandsgewichte gestrichen, Dialog auf U·A

Anwender, 15.09.2026: „Q16 — Bestandsgewichte streichen: Daten für die U-Werte können
mit U·A gerechnet werden → Dialog anpassen. Für späteren VDI-Weg nötig. Auch für
IFC-Datei-Import."

**Entscheid E2:** Im Stundenmodell gehen die Transmissionsverluste ungewichtet mit U·A
ein (4.3); die Faktoren 0,83 / 0,95 / 0,45 / 0,83 bleiben allein dem ausdrücklich gewählten
Tagesbilanz-Weg, dessen Rechnung sich nicht ändert. Der Gebäudedialog wird auf diese
Größen umgebaut:

- Je Bauteilgruppe — Außenwand, Fenster, Dach, Bodenplatte, Sonstiges, die drei
  Wärmebrücken — eine Zeile mit U bzw. ψ, A bzw. L, dem Produkt **U·A in W/K** und der
  Randbedingung (Außenluft, Erdreich, Keller); darunter die Summe H_T, der
  Lüftungsleitwert H_ve aus Luftwechsel und Volumen und H_ges — sichtbar, ohne verdeckte
  Faktoren. Im Tagesbilanz-Weg zeigt der Dialog daneben den gewichteten Wert, damit der
  Unterschied je Gebäude erklärbar bleibt.
- Diese Zeilen sind die **gemeinsame Zielstruktur** für den Klassenweg (4.3), den
  Bauteilweg (G3, dann je Bauteil statt je Gruppe) und den IFC-Import (7.6: U-Werte je
  Bauteilgruppe flächengewichtet, Flächen aus den Quantity-Sets — dieselben Zeilen mit
  Herkunftskennzeichen IFC / Vorgabe / manuell).
- Umsetzung: die U·A-Anzeige mit G1 (Dialoggruppe „Rechenmodell" wird zur Gruppe
  „Hülle und Rechenmodell"), die Herkunftskennzeichen mit G4; die Kennzahl H_T wandert in
  `KennzahlenKatalog.cs` und den Bericht (Kapitel 9).

### N1.7 Entscheid E3 — Q9: xBIM nach Empfehlung

Anwender, 15.09.2026: „Q9: Empfehlung." **Entscheid E3:** der IFC-Import (G4) benutzt
`Xbim.Essentials` 6.1.605 (`Xbim.Ifc2x3`, `Xbim.Ifc4`, `Xbim.Ifc4x3`, `Xbim.IO.MemoryModel`)
im Kern als **unverändertes NuGet-Paket** unter CDDL-1.0 (7.4): Lizenztext und dauerhafter
Verweis auf die xBIM-Quellen kommen in die Lizenzhinweise des Installationspakets,
Copyright-Vermerke bleiben stehen, es wird nichts geforkt oder gepatcht. `Xbim.Geometry`
bleibt draußen (Windows-gebunden, LGPL über OCCT). Fällt die CDDL-Freigabe später weg,
ist `GeometryGymIFC_Core` (MIT) der Ausweg mit gleichem Leseumfang ohne Geometrie. Die
Paketversion wird in `Directory.Packages.props` geführt; der iOS-Trimming-Nachweis
(Q10) bleibt Teil von G4.

### N1.8 Entscheid E4 — Q14, Q22, Q23: Neu-Einfrieren nach Empfehlung

Anwender, 15.09.2026: „Q14, Q22, Q23: Empfehlung." **Entscheid E4:**

- **Q22:** `Bauweise` von Gebäude 10576 wird in der Testdatenbank auf 15 200 Wh/K
  (50 Wh/(m²K) × 304 m²) korrigiert. Zugleich entsteht die **vierte Einfrierregel „gesäte
  Gebäudedaten"** (`Tab_Gebaeude(_STAMM)`: `Bauweise`, U-Werte, Flächen, Sollwerte,
  `Luftwechselrate`, `Fensterdurchlassgrad`) in `Referenzlaeufe/LIESMICH.md` und im
  Abschnitt „Regressionsnetz" der `CLAUDE.md`.
- **Q23:** der statische Zustand `_prevRoomTemp` wird durch einen Instanzzustand mit
  `ResetState` je Gebäude ersetzt; das Tagesmodell wird damit reihenfolgeunabhängig.
- Beides läuft als **eigener Einfrierschritt GB vor G1** auf dem Bestandsweg: Referenzlauf,
  Vergleich, Begründung in `Referenzlaeufe/LIESMICH.md`, grüner CI-Lauf; die neue Basis
  ist die letzte reine Bestandsbasis, gegen die der ausdrücklich gewählte Tagesbilanz-Weg
  später regressionsgeprüft wird (N1.1).
- **Q14** ist mit E1 erledigt: alle dreizehn Referenzprojekte werden mit G1 auf das
  Stundenmodell umgestellt und die Basis erneut eingefroren; ein zusätzliches
  Referenzprojekt entfällt.

**Vermerk 23.09.2026:** Die Prognose aus N1.4, Q23 ändere die Projekte mit mehreren Gebäuden
(1008, 1039), hat sich im Einfrierschritt GB nur für 1008 bestätigt; 1039 blieb byte-gleich, weil
der statische Zustand dort nie ein Ergebnis erreichte (Tag 1 setzt die Vortemperatur auf den
Nachtsollwert, der Jahreslauf überschreibt den Vorlauf). Berichtigt im Hauptteil 10.4, 11 und 13
(Q23); belegt in der [Statusdatei](Status_Gebaeudesimulation_VDI6007.md), Zeile GB.

### N1.9 VDI 6020:2022 und VDI 2078:2015 liegen vor (Befund K)

Der Anwender hat beide Richtlinien nachgereicht
([`Gebaeudesimulation/2026-09-15_Befund_K_VDI6020_VDI2078_Abgleich.md`](Gebaeudesimulation/2026-09-15_Befund_K_VDI6020_VDI2078_Abgleich.md)).
Was sich daraus ändert:

- **Lizenzlage:** VDI 2078 trägt denselben Beuth-Abo-Fingerabdruck wie Blatt 1 und 3
  (plausibel lizenziert). **VDI 6020:2022 trägt die Ausdruck-Fußzeile einer
  IP-Hochschullizenz** (Universität Tübingen, „ip-user", Ausdruck am 15.09.2026); solche
  Lizenzen decken in aller Regel keine kommerzielle Produktentwicklung. Herkunft klären,
  eigene Lizenz beziehen; bis dahin nichts aus VDI 6020:2022 in Code, Testdaten oder
  Auslieferung übernehmen — dieser Nachtrag zitiert nur.
- **Rückhalt für E1:** VDI 6020, 6.2.3.4 (Seite 45) erklärt 1-Kapazitäten-Modelle für
  Ganzjahressimulationen „ohne Ausnahme" für ungeeignet, einschließlich des
  Abschätzverfahrens der VDI 2078 und der analogen Rechnung in DIN V 18599. Der
  Tagesbilanz-Weg des Bestands ist damit als Jahresrechenverfahren normativ
  disqualifiziert und bleibt nur als bewusst gewählte Ausnahme.
- **Referenzergebnisse stehen in keiner der beiden Richtlinien gedruckt** — nur als
  Excel-Arbeitsmappen auf den Datenträgern (VDI 6020 7.3/9.1/Anhang C2, VDI 2078
  Anhang C2). Dazu fehlen die Klimareihen TRY05 Würzburg (DWD 1986) für alle Jahresfälle
  und TRY03 Hamburg / TRY12 Mannheim (DWD 2004) für VDI 2078 Testbeispiele 4 und 6. Der
  Beschaffungspunkt aus N1.2 lautet neu: **Datenträger bzw. Downloadpakete zu VDI 6007
  Blatt 1, VDI 2078 und VDI 6020, die DWD-TRY-Datensätze, und eine eigene Lizenz der
  VDI 6020:2022.**
- **Nachweisweg für den Strahlungsweg:** VDI 6020 lässt „das Strahlungsmodell der VDI 6007
  Blatt 3 oder ein adäquates Modell" zu (5.1.11); VDI 2078, 9.1, **Fall B** regelt den
  Nachweis für einen Rechenkern nach Blatt 1 mit anderem Strahlungsmodell: Blatt-1-Fälle
  nach Fall A (Typ 1, ± 0,1 K / ± 1 W), die VDI-2078-Testbeispiele 7–10 nach Typ 2
  (**± 0,2 °C / ± 5 W je Stunde, absolut**), die übrigen statistisch (Typ 3). Hay-Davies
  (Q20) ist damit kein Normverstoß, aber die Typ-2-Schwelle wird er auf Nord- und Ostflächen
  nach aller Erfahrung reißen. Solange Testbeispiele 7–10 nicht bestanden sind, ist nur
  „Rechenkern nach VDI 6007 Blatt 1" belegbar, nicht „validiert nach VDI 6020/2078".
  VDI 6020 5.1.11 fordert außerdem den Bedeckungsgrad oder die Sonnenwahrscheinlichkeit als
  Klimaparameter — `Tab_Solar` führt ihn nicht (PVGIS liefert ihn nicht).
- **Neue Pflicht- und Kürfälle:** G0 unverändert (Blatt 1, 1–12, Band). **G1 Pflicht:
  VDI 2078 Testbeispiel 7.1/7.2** (Sonnenstand, Umrechnung, langwelliger Austausch, korg
  und g_dir; Eingaben vollständig gedruckt in Anhang A1 und B1, ohne TRY rechenbar; ohne
  Datenträger nur qualitativ gegen die Verlaufsbilder Seite 72). **G1 empfohlen:
  Testbeispiel 10** (Umrechnung auf N, S, O, W, horizontal, vor und hinter der Verglasung —
  der Fall, an dem sich Hay-Davies gegen Aydinli/Krochmann quantifiziert; braucht TRY05).
  **G2 empfohlen: VDI 6020 Testbeispiel 16.1/16.2** (Fensterlüftung ohne Kühlung,
  Übertemperaturgradstunden — deckt sich mit der Sommerlüftungsregel). Später optional: 8/9,
  12/13, 14/15. Nicht relevant: VDI 2078 Anhang D (1-K) und Testbeispiele 1–6.
  Abnahmekriterien 10.1/10.4 bekommen mit G1 eine Zeile „Strahlungsweg: VDI 2078
  Testbeispiel 7, Typ 2".
- **Genauigkeitserwartung:** VDI 2078, Tabellen 9/10 (Seiten 82–85) messen das 2-K-Modell
  selbst gegen das n-K-Referenzmodell: Mittelwert der stündlichen Abweichung bis
  0,64–0,75 K bzw. 17–41 W, Standardabweichung bis 1,1 K. Die Modellklasse trägt eine
  Unschärfe dieser Größenordnung; die gemessenen 86–100 % Katalogtreffer (5.5) liegen im
  Rahmen.
- **Korrekturen an N1.3:** der Eintrag zum Rahmenanteil war zu scharf — VDI 6020 5.1.15
  (Seite 17) regelt den Fensterrahmen ausdrücklich (Verglasung und Rahmen als ein oder zwei
  Bauteile), nur Blatt 2 schließt ihn aus; Quelle ist VDI 6020/2078, nicht DIN V 18599.
  c·ρ der Luft: 1,1953 kJ/(m³K) gilt für Blatt-1-Testfall 12, 1,2 kJ/(m³K) für die
  VDI-6020-Beispiele (Seite 51). VDI 6020 5.1.17 und VDI 2078 5.2.1 erlauben, die
  Diffusstrahlung bei Eigen- und Fremdbeschattung unkorrigiert zu lassen — das entlastet
  die Pauschalfaktoren F_S; für die Direktstrahlung bleibt Blatt 3, Abschnitt 12.
- **Neu aufzunehmen:** die **Bemaßungsregel** VDI 6020 5.1.2 (Seite 13) — Außenbauteile
  nach dem Bruttomaß der Außenseite, Innenbauteile nach lichten Maßen; sie gehört in den
  Gebäudedialog nach E2 (U·A je Bauteil) und in die Zuordnungsregel des IFC-Imports (7.6:
  `GrossSideArea`, nicht `NetSideArea`). Die **Anlagenschnittstelle** VDI 6020 5.3.3,
  Gl. (4)–(8) (Seiten 29–31): Q̇_HK,Raum → Zuluftanteil + Restbedarf → Nutzenergiebedarf ist
  die normative Übergabegröße vom Gebäudemodell an `SimulationControl`; Erzeuger,
  Wärmepumpe und Speicher kommen in beiden Richtlinien nicht vor — die Deckungsrechnung
  bleibt normfrei. Die **Heating Design Period** (VDI 6020 Anhang A1, informativ) ist der
  normnahe Kandidat für eine Auslegungsheizlast im Stundenmodell (14 Tage bedeckt, 4 Tage
  Anlauf, Heating Design Day) — Abgrenzung, für ein späteres Papier.
- **Namensfalle:** „Testbeispiel 11" ist in VDI 6020 seit 2022 unbelegt, „Testfall 11" im
  Konzept meint den Kühldeckenfall aus Blatt 1. In allen Papieren die Richtlinie mitnennen.

### N1.10 Entscheid E5 — Datenträger abgelehnt, TRY als zweite Importquelle

Anwender, 15.09.2026: „Datenträger, TRY-Daten: verwende vorliegende TMY-Daten."
**Entscheid E5, fortgeschrieben am 19.09.2026:** Die **Datenträger der Richtlinien bleiben
abgelehnt**; **DWD-Testreferenzjahre sind die zweite Importquelle** neben PVGIS-TMY. Sie
kommen nicht als Beilage einer Richtlinie ins Haus, sondern über den Klimadaten-Import — als
eigene TRY-Datei oder aus den offenen TRY-Regionaldaten; der Weg, das Spaltenbild, die
Zeitbasis und die Lizenz stehen in
[`Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md`](Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md).
Klimabasis des Gebäudemodells ist damit je Klimaregion das, was bei ihrem Import geholt
wurde — PVGIS-TMY oder TRY —, in `Tab_Solar` (2.3) in derselben Form: Temperatur, Global-,
Direkt- und Diffusstrahlung, Sonnenwinkel je Stunde.

Folgen:

- **G0 bleibt vollständig:** die zwölf Testfälle der VDI 6007 Blatt 1 brauchen kein
  Klima — ihre Eingaben und Referenzwerte stehen gedruckt im Text (N1.2); Testfall 5 und
  8–10 führen die Einstrahlung bereits als Tabellenwerte.
- **Die Testbeispiele 8–16 der VDI 6020 und 8–16 der VDI 2078 sind nicht nachrechenbar —
  das gilt unverändert weiter:** sie setzen TRY05 Würzburg voraus, und ihre
  Referenzergebnisse liegen nur auf den Datenträgern. Ein Testreferenzjahr aus dem
  Klimadaten-Import ersetzt weder das eine noch das andere: TRY05 Würzburg ist ein anderer
  Datensatz als die heutigen Testreferenzjahre, und die Referenzergebnisse fehlen so oder
  so. Der Nachweis des Strahlungswegs nach VDI 2078, 9.1, Fall B (Typ 2,
  ± 0,2 °C / ± 5 W) ist damit **nicht** führbar. EPOS-Plan weist aus: „Rechenkern nach
  VDI 6007 Blatt 1, validiert an den zwölf Testbeispielen" — nicht „validiert nach
  VDI 6020/2078". Das steht so in Wiki und Bericht.
- **Was den Strahlungsweg stattdessen absichert (G1):** (1) VDI 2078, Testbeispiel 7.1
  (Einstrahlung außen auf 1 m² für CDP und CDD aus den gedruckten Klimaparametern,
  Anhang A1 und B1) — rechenbar ohne TRY, Abgleich qualitativ gegen die gedruckten
  Verlaufsbilder (Seite 72); (2) ein **interner Modellvergleich auf den TMY-Daten**: das
  Verfahren nach Blatt 3 (Aydinli/Krochmann, Gl. (29)–(50)) wird neben Hay-Davies
  implementiert und auf den dreizehn Klimaregionen je Orientierung und Neigung gegen
  Hay-Davies gehalten; berichtet werden Jahressumme, Stundenabweichung und der Anteil der
  Stunden innerhalb ± 5 W/m². Das ist kein Normnachweis, aber die Zahl, die die Wahl des
  Transpositionsmodells (Q20) trägt. Fällt der Unterschied klein aus, bleibt Hay-Davies;
  sonst wird Blatt 3 der Weg.
- **Bedeckungsgrad und Sonnenwahrscheinlichkeit** (Blatt 3, Gl. (47)–(49); VDI 6020 5.1.11)
  liefert PVGIS nicht. Ersatz: die Sonnenwahrscheinlichkeit wird je Stunde aus dem
  Diffusanteil geschätzt (SSW ≈ 1 − Diffus/Global bei Sonne über dem Horizont, geklemmt
  auf 0…1); als Abweichung von Blatt 3 dokumentiert. **Eine TRY-Reihe führt den
  Bedeckungsgrad `N`** — gespeichert wird er nicht, weil `Tab_Solar` keine Spalte dafür hat;
  er ist damit Kandidat desselben Schemaschritts (siehe unten und Klimadatenkonzept,
  Abschnitt 6). Bis dahin gilt die Schätzung für jede Region, gleich welcher Herkunft.
- **Langwelliger Austausch außen** (Blatt 1, Gl. (33)–(37)): der PVGIS-TMY-Abruf führt neben
  Temperatur, Strahlung, Wind und Feuchte auch die atmosphärische Gegenstrahlung; in G1 ist
  zu prüfen, ob `KlimaImportAblauf` sie erhält, und sie dann als neue Spalte in `Tab_Solar`
  zu persistieren (Schemaschritt mit G1; Bestandsregionen bekommen den Wert beim nächsten
  Klimaimport, bis dahin Rückfall auf die Schätzung nach Blatt 3, Gl. (84)–(88) mit der
  Sonnenwahrscheinlichkeit von oben). Wind und Feuchte werden bei der Gelegenheit ebenfalls
  persistiert (2.3). **Eine TRY-Reihe führt `A` (atmosphärische Gegenstrahlung) und `E`
  (langwellige Ausstrahlung)** ebenso — auch sie werden mangels Spalte nicht gespeichert und
  gehören in denselben Schemaschritt.
- **Zeitbasis:** PVGIS-TMY steht in UTC (2.3); das Gebäudemodell rechnet in Ortszeit über
  `ReadOrtszeit` (4.4). Der Sonnenstand nach Blatt 3 wird zur Stundenmitte gerechnet
  (Seite 11) — das ist mit `Sonnengeometrie` abzugleichen (G1).
- **Beschaffungspunkte, neuer Stand:** offen bleibt allein die Lizenzfrage zur VDI 6020:2022
  (N1.9); die Datenträger entfallen.

### N1.11 Entscheid E6 — VDI 6020:2022 nur zu Forschungszwecken

Anwender, 15.09.2026: „VDI 6020:2022 Nutzung nur zu Forschung." **Entscheid E6:** die
vorliegende VDI 6020:2022 ist eine Forschungslizenz. Sie dient allein dem Verständnis; **nichts
daraus geht in Code, Tests, Testdaten, Wiki, Bericht oder Auslieferung**, und keine Produktregel
wird auf sie gestützt. Für das Produkt tragen ausschließlich die lizenzierten Quellen: VDI 6007
Blatt 1–3 (2015/2012), VDI 2078:2015 und die zitierten DIN-/ISO-Normen. Folgen für die
Aussagen aus N1.9:

| Aussage aus N1.9 | Stand nach E6 |
|---|---|
| 1-K-Modelle für Jahressimulationen ungeeignet (VDI 6020 6.2.3.4) | bleibt Hintergrundwissen zu E1; die Begründung von E1 im Wiki und im Bericht nennt die Messergebnisse (Kapitel 5) und VDI 6007 Blatt 1, nicht VDI 6020 |
| Typ-2-Schwelle ± 0,2 °C / ± 5 W, Validierungsfälle A/B, Testbeispiel 7 | stammen aus **VDI 2078**, 9.1/9.2 — bleiben Produktgrundlage |
| Bemaßungsregel Bruttomaß außen / lichte Maße innen (VDI 6020 5.1.2) | für den Gebäudedialog (E2) und den IFC-Import aus VDI 2078, Abschnitt 6, bzw. DIN EN ISO 13789 belegen — in G1 zu prüfen; bis dahin gilt die Regel als Arbeitsannahme ohne Normzitat |
| Anlagenschnittstelle Gl. (4)–(8) | Hintergrund; die Übergabegröße Q̇_HK,Raum an `SimulationControl` wird ohne Normzitat definiert (4.6) |
| Bedeckungsgrad als Klimaparameter (VDI 6020 5.1.11) | die Anforderung folgt für das Produkt aus VDI 6007 Blatt 3, Gl. (47)–(49) — unverändert |
| Testbeispiele 14–16, Heating Design Period, Konformitätserklärung | Forschung; keine Produktplanung darauf |
| Testbeispiele 1–7 | identisch mit VDI 6007 Blatt 1 — dort belegt |

Der Befund K bleibt als Lesenotiz unter `Gebaeudesimulation/` mit dem Vermerk der
Nutzungsbeschränkung; die Textfassung unter `epos-spike\vdi\VDI6020_2022.txt` wird nach
Abschluss der Konzeptphase gelöscht. Die Statuszeile „Lizenz VDI 6020" ist damit
entschieden; eine Produktlizenz wird nicht beschafft.

### N1.12 Entscheide E7 und E8 — Einzonenmodell zuerst, Mehrzonen über IFC, Skalierung bleibt

Anwender, 15.09.2026: „Als erstes soll die Gebäudesimulation nach VDI 6007 ein Einzonenmodell
analog zum bestehenden verwenden. Mit dem Import einer IFC-Datei soll ein Mehrzonenmodell
möglich sein. Dazu muss ein Konzept erstellt werden, wie die Eingaben für die Zonen und die
importierten Materialdaten und Flächen erfolgen kann." — und: „Übernommen werden aus dem
bisherigen Gebäudemodell soll die Skalierung: die Hochrechnung des Energiebedarfs auf eine
andere Fläche/Volumen."

**Entscheid E7:** Die Stufen G0 bis G2 bauen das **Einzonenmodell** — eine Zone je Gebäude,
Eingaben wie im Bestand (Kapitel 4.1, 4.3 Klassenweg), Datenquelle `Tab_Gebaeude`. Das
**Mehrzonenmodell** ist eine spätere Stufe, die auf dem Bauteilkatalog (G3) und dem
IFC-Import (G4) aufsetzt: Zonen, Bauteile je Zone mit Flächen und Orientierung, Aufbauten
mit importierten Materialdaten. Es bekommt ein eigenes Konzeptpapier
(`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`, in Arbeit) mit dem Rechenweg der
Zonenkopplung nach VDI 6007 Blatt 1 (nicht adiabate Innenbauteile über θ_NR,eq, Gl. (40)),
dem Datenmodell Zone → Bauteil → Aufbau → Schicht → Baustoff, den Dialogen für die
Zoneneingabe und den Zuordnungsregeln des Imports. Ohne Zonendaten rechnet ein Gebäude
weiter als eine Zone; das Einzonenmodell bleibt davon unberührt.

**Entscheid E8:** Die **Skalierung des Bestands bleibt im Stundenmodell erhalten**, wie in
4.7 beschrieben: die Rechnung läuft mit den Katalogdaten des Gebäudes, das Ergebnis wird mit
dem Faktor `Z_AuswahlWohnflaeche / Wohnflaeche` auf die Projektfläche hochgerechnet
(`BhkwPlan.cs:435`, Argumente `:392`), und die Verbrauchs-Rückrechnung über die fiktive
Fläche (`Bewohner_und_Flaeche_berechnen`, `SimulationWaermebedarf.cs:613-656`) bleibt eine
Verhältnisrechnung mit einem Kataloglauf. Beides gilt für den Klassenweg (Einzonenmodell
aus `Tab_Gebaeude`). Für Gebäude mit echter Hülle aus Bauteilkatalog oder IFC (G3/G4)
entfällt die Nachmultiplikation, weil die Flächen dann das Projektgebäude selbst beschreiben; das
Volumen folgt aus Zonenflächen und Raumhöhen. Die Statusdatei führt beide Entscheide.

### N1.13 Entscheid E9 — Import und Export von gbXML und IFC

Anwender, 15.09.2026: „Der Import und Export von gbXML und IFC sollen möglich sein."
**Entscheid E9:** EPOS-Plan tauscht Gebäudedaten in beide Richtungen und in beiden Formaten.
Das ändert gegenüber Rev. 1: der gbXML-Import (bisher Kür in G5, Q13) wird Pflicht, und es
kommen zwei Exporte hinzu, die Rev. 1 nicht kannte. Gegenstand des Exports ist das
Gebäudemodell von EPOS-Plan — im Einzonenmodell die Hülle nach E2 (Bauteilgruppen mit U·A,
Fenster je Orientierung, Volumen), im Mehrzonenmodell (E7) Zonen, Bauteile je Zone mit
Fläche und Orientierung, Aufbauten, Schichten und Baustoffe — samt Ergebnissen
(Heizwärmebedarf je Zone und Jahr, Spitzenlast) als Eigenschaften. Ein Export ohne
Geometriekernel liefert **keine Gebäudegeometrie**, sondern ein semantisches Modell:
in IFC Objekte mit Eigenschaften und Mengen ohne Körperdarstellung (bzw. bei zuvor
importierten Dateien die Rückgabe der Datei mit angereicherten Eigenschaftssätzen), in
gbXML Flächen als Rechteckgeometrie aus Fläche, Azimut, Neigung. Was die Zielwerkzeuge
(Archicad, Revit, IDA ICE, DesignBuilder, Solar-Computer, Hottgenroth) damit anfangen
können, ist Gegenstand der Prüfung. Das Konzept dazu entsteht als eigenes Papier
(`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`), nachdem das Mehrzonenmodell steht, weil
der Export dessen Datenmodell abbildet. Stufen: gbXML-Import neben dem IFC-Import in G4,
Exporte als G7.

### N1.14 Normband-Validierung aller zwölf Testfälle (Befund J)

Die Tabellen A1.3 bis A12.3 der Blatt 1 wurden von zwei unabhängigen Lesern (Textfassung
und Wortkoordinaten) ausgezogen, mit einer Drittlesung und gegen die AixLib-Reihen
abgeglichen: **6 192 Zellen, null Abweichungen** zwischen den Lesungen; die AixLib-Reihen
sind in 864 Zellen zeichengleich die Normzahlen (Programm 1; Fall 11 Programm 2; Fall 6 mit
gedrehtem Vorzeichen). Der Prototyp wurde dann für alle zwölf Fälle nach der Normregel 6.6
geprüft — Band [min(P1,P2) − tol, max(P1,P2) + tol], tol = 0,1 K bzw. 1 W, Prüfgröße das
Stundenmittel, Tag 1, 10 und 60, Luft- und operative Temperatur und Last
([`Gebaeudesimulation/2026-09-15_Befund_J_Normband_Validierung.md`](Gebaeudesimulation/2026-09-15_Befund_J_Normband_Validierung.md)).

| Ergebnis | Zahl |
|---|---|
| geprüfte Zellen (12 Fälle × 3 Größen × 3 Tage × 24 Stunden) | 2 592, davon 2 553 im Band |
| Prüfungen Fall × Größe | **30 von 36 bestanden** |
| Fälle vollständig im Band | **8 von 12** (1, 2, 3, 4, 5, 7, 8, 12) |
| Fall 6, Last | bis 0,50 W über dem Band in 14 Stunden — phasenstarr zum Sollwertprofil, keine Parametervariation räumt es ab; ein echter, kleiner Modellrest (0,2 % der Spitzenlast) |
| Fall 9, Luft und operativ | 0,011 K bzw. 0,006 K in je einer Stunde |
| Fall 10, Luft und operativ | 0,024 K (8 Stunden) bzw. 0,060 K (11 Stunden) |
| Fall 11, Last | **3,9 W in Stunde 10 der Tage 10 und 60** — die Umschaltstunde Heizen → Kühlen; der Umschaltzeitpunkt liegt rund 45 s zu früh; Hypothese: die Kühldecke braucht einen eigenen Oberflächenknoten (α_kon 5,0 nur dort), das Ein-Knoten-IW löst ihn nicht auf |
| Rechenzeit | 12 Fälle × 1 440 Schritte in 1,32 s samt Prozessstart; 6–120 ms je 8 760 Schritte |

Zwei Folgerungen für G0: (1) Die Fälle 9 und 10 hängen an der Lesart von Abschnitt 6.6 —
die Norm druckt auf 0,1 K; bezieht man die Druckrundung ein, verschwinden ihre
Überschreitungen (maximal 0,06 K). Das ist eine Auslegungsfrage, die G0 mit dem Anwender
festlegt; bis dahin zählen die Fälle als knapp nicht bestanden. (2) Fall 11 ist der
einzige substanzielle Befund; G0 löst ihn über getrennte Innenoberflächen (Kühldecke als
eigener Knoten) oder weist ihn als bekannte Grenze aus. Die operative Temperatur ist in
10 von 12 Fällen vollständig im Band — sie hat allerdings keinen externen Beleg (die
AixLib-Reihen führen sie nicht), ihre 1 728 Normzellen tragen allein die drei
übereinstimmenden Lesungen. Gegen die VDI-6020-Spalte der Fälle 1–7 weicht der Prototyp
erwartungsgemäß ab (bis 2,2 K, 66 W): er rechnet das 2-K-Modell der VDI 6007, nicht das
Referenzverfahren der VDI 6020. Der Ausweis im Produkt lautet damit: **„Rechenkern nach
VDI 6007 Blatt 1; acht der zwölf Testbeispiele vollständig im Normband, drei innerhalb
0,06 K bzw. 0,5 W, Testbeispiel 11 in zwei Umschaltstunden um 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung)"** — bis G0
die offenen Punkte schließt.

### N1.15 Entscheid E10 — Druckrundung als Toleranz

Anwender, 15.09.2026: „Druckrundung als Toleranz zulassen (Fälle 9 und 10)." **Entscheid
E10:** Die Referenzwerte der Tabellen A1.3–A12.3 sind gedruckte Rundungen — Temperaturen auf
0,1 K, Lasten auf 1 W. Ein gedruckter Wert steht für ein Intervall von ± einer halben
Druckstelle um den wahren Wert; die Prüfregel 6.6 wird auf den wahren Wert bezogen. Das
Prüfband lautet damit

```
[min(P1,P2) − tol − ½ Druckstelle,  max(P1,P2) + tol + ½ Druckstelle]
  Temperaturen: tol = 0,1 K, ½ Druckstelle = 0,05 K  →  ± 0,15 K
  Lasten:       tol = 1 W,   ½ Druckstelle = 0,5 W   →  ± 1,5 W
```

Die Regel wird einheitlich auf alle Größen angewandt, nicht nur auf die vom Anwender
genannten Fälle 9 und 10 — sonst hinge das Urteil an der Größe statt an der Regel. Folgen
für den Stand aus N1.14: Fall 9 (0,011 K) und Fall 10 (0,060 K) bestehen; **Fall 6 (0,50 W)
besteht ebenfalls**, weil seine größte Überschreitung unter der halben Druckstelle liegt;
Fall 11 bleibt offen: seine zwei Umschaltstunden liegen **um 3,4 W bzw. 2,8 W** neben dem Band
nach E10 (gegen das strenge Band ohne Druckrundung 3,9 W bzw. 3,3 W;
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 10.3). Damit sind **35 von 36 Prüfungen bestanden und 11 von 12
Fällen vollständig im Band**. Für G0 heißt das: `GebaeudeModellNormfallTests` prüfen mit
diesem Band und weisen die Reserve aus; offen bleibt allein Testfall 11. Die **wahrscheinlichste
Ursache** — das Ein-Knoten-Innenbauteil trennt die Kühldecke (α_kon = 5,0) nicht von den übrigen
Innenflächen, der Umschaltzeitpunkt liegt dadurch rund 45 s zu früh — ist **diagnostisch belegt,
aber nicht bewiesen**; bewiesen ist der Umschaltversatz, nicht seine Ursache, und ein Nachweis
bräuchte ein Modell mit getrenntem Deckenknoten (Rechenschritte 10.3). Der Produktausweis lautet neu: **„Rechenkern nach VDI 6007
Blatt 1; elf der zwölf Testbeispiele im Normband einschließlich Druckrundung, Testbeispiel
11 in zwei Umschaltstunden um 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung)"** — bis G0
auch diesen Punkt schließt.

### N1.16 Entscheid E11 — Gebäudebetrachter: Grundriss und schematische Körper

Anwender, 15.09.2026: „ergänze im Konzept: ifc Viewer — Variante: 2D-Grundriss je Geschoss aus den
Raumgrenzen (SVG in einer Razor-Komponente) und Schematische Körper aus EPOS-Daten (Quader je Zone,
Platte je Bauteil) — Zusammen gebaut."

**Entscheid E11: ein Gebäudebetrachter, zwei Ansichten, ein Datenmodell.** EPOS-Plan bekommt keinen
IFC-Betrachter, sondern eine eigene Ansicht auf das eigene Gebäudemodell. Beide Ansichten lesen
dieselbe Quelle: ein **Zonengeometrie-Modell** im Rechenkern (Arbeitsname; den endgültigen Namen
setzt das Architekturpapier — es führt das Modell als `Zonengeometrie` mit `Zonenumriss`,
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 1.3) mit je Zone einem
Grundrisspolygon, einer Höhe, einem Geschoss und der
Zuordnung der Bauteile zu den Polygonkanten bzw. zu Boden und Decke.

**Woher das Polygon kommt.** Mit IFC aus den **Raumgrenzen** (`IfcRelSpaceBoundary`) — nach Befund P, 2.3
sind die Polygonflächen ohne Geometriekernel zu rechnen
([Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md), 6.2). Ohne IFC aus Zonenfläche und dem
Seitenverhältnis der Bauteilgruppen: h = V/A, l = A_NS/(2·h), b = A_OW/(2·h) — dieselbe Herleitung
wie die synthetische Quadergeometrie in G7b des Datenaustauschkonzepts. Beim Einzonenmodell (E7) ist
das ein Quader je Gebäude.

| Teil | Technik | Stufe | Aufwand |
|---|---|---|---|
| **Zonengeometrie-Modell** (gemeinsames Fundament) | Kern, plattformfrei: Polygon, Höhe, Geschoss, Zuordnung der Bauteile zu Kanten, Boden und Decke | **G6c** | 3–5 PT |
| **Ansicht 1 — 2D-Grundriss je Geschoss** | SVG in einer Razor-Komponente, **keine Bibliothek**; Räume und Zonen als Polygone, Farbe je Zone, Klick auf einen Raum wählt die Zone bzw. ordnet sie zu | **G6c** — Zuordnungsdialog des IFC-Imports; der gbXML-Import speist dieselbe Ansicht über `Space`/`Zone` | 3–5 PT |
| **Ansicht 2 — schematische Körper** | Polygon um die Höhe extrudiert (bei Rechteckgrundriss ein Quader je Zone), Platte je Bauteil an Kante, Boden und Decke; three.js (MIT) **lokal** unter `EPOS.UI/wwwroot`, nie vom CDN | **G7b** — Sichtprüfung dessen, was der gbXML-Export (`PolyLoop`) und der IFC-Export G7e (`IfcExtrudedAreaSolid`) schreiben | 4–7 PT |
| | | **zusammen** | **10–17 PT** |

**Die Ansicht ist der Prüfstand der Exporte, nicht ihr Beiwerk.** G7b und G7e lesen dasselbe
Zonengeometrie-Modell und **schreiben** es nur noch; sie sparen dadurch 3–5 PT. Getrennt gebaut wären
es 10–20 PT plus eine zweite, abweichende Geometrie — und zwei Bilder desselben Gebäudes, die nicht
zueinander passen.

**Regeln.**

- **Eine Komponente** mit Umschalter „Grundriss | Körper" (Arbeitsname `GebaeudeAnsicht.razor`).
- **„schematisch" steht sichtbar in der Oberfläche.** Ohne IFC ist die Anordnung der Zonen zueinander
  erfunden (Reihung je Geschoss); die Kennzeichnung ist dieselbe Pflicht wie bei den Exporten.
- **three.js kommt auf die Lizenzhinweisseite** (Frage U10) — wie jeder andere ausgelieferte
  Fremdanteil.
- **iOS:** WebGL läuft in der WebView; kleine Dreieckszahl, kein Speicherproblem.
- **Abnahme:** bunit-Test der Komponente; **Determinismus der Geometrie** als Probe — gleiche Eingabe
  ergibt gleiche Polygone und einen byteweise gleichen Export.

**Benannt abgelehnt.** Ein **vollwertiger 3D-IFC-Betrachter** (web-ifc mit three.js in der WebView,
15–25 PT, MPL-2.0, auf iOS speicherkritisch) und die **native xBIM Geometry Engine** (nur Windows,
OCCT unter LGPL, gegen [`ADR-003`](ADR-003_IFC_xBIM_ohne_Geometriekernel.md)). Beides bleibt eine
spätere Option **nur bei Bedarf aus der Praxis** und ist nicht geplant. „Datei extern öffnen" bleibt
als Handgriff über `Dienste.Datei` zulässig.

Die Einzelheiten stehen als **Nachtrag 1** im
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) (Kapitel 14: gemeinsames
Modell, beide Exporte, Proben, Aufwand) und als Abschnitt 6.7 im
[Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (Grundrissansicht im
Zuordnungsdialog).

### N1.17 Entscheide vom 16.09.2026 zu den Fragen Q3–Q5, Q7, Q11–Q13, Q15, Q17–Q21

Anwender, 16.09.2026: „Q3-W5: Empfehlung / Q6: was spricht gegen ‚eigener Oberflächenknoten?' ?
-> diese auswahl scheint mir vernünftiger. erläutere / Q7: Empfehlung / Q8: Kühlung aufnehmen,
konzept dazu erweitern / Q11: Wohnfläche ändern in Nutzfläche, sonst empfehlung / Q12: betrachte
als geklärt, importdatei für alle umsetzen / Q13 - Q21: Empfehlung." — „Q3-W5" ist als **Q3–Q5**
zu lesen.

Damit gelten die Empfehlungen aus Kapitel 13 als Entscheide. Die Tabelle nennt je Frage den
Entscheid im Wortlaut der Empfehlung und die Stelle, die ihn trägt. Q8 bekommt einen eigenen
Abschnitt (N1.18, Entscheid E12); Q6 bekommt die erbetene Erläuterung (N1.19) und bleibt offen.

| Nr. | Entscheid (16.09.2026) | Folge / Stelle |
|---|---|---|
| **Q3** | Thermische Masse in G1 aus der Bauweise-Klasse mit der Aufteilung 0,3 / 0,7 — **Klassenweg in G1** (gemessen unkritisch, ≤ 0,1 %), Schichtaufbau in G3 | Kapitel 4.3; Schritt A der [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md); der Bauteilweg bleibt Stufe G3 (6.3) |
| **Q4** | Erdreich nach Kusuda (harmonische Amplitude) als Vorgabe, Keller und Außenluft als Feldwerte; **der Bestandsfaktor 0,45 wird nicht nachgebaut** | Kapitel 4.4; Schritt E der Rechenschritte; benannte Erweiterung gegenüber der Richtlinie |
| **Q5** | Opake Außenbauteile mit Absorption und Abstrahlung in G1 **aus (Parität), Schalter vorhanden**; die Pauschale −3 K bleibt verboten; die Normformel wird in G2 nachgerüstet | Kapitel 4.4; Schalter `Aussenbauteile_Strahlung` aus Schemaschritt 77 (6.1) |
| **Q7** | `Heizleistung_Max` als Option, NULL = unbegrenzt; **`Waermelast_Max` bleibt unverändert das Maximum des Kanalsummenvektors**; Stundenspitze, gleitendes Tagesmittel und 95-%-Quantil stehen zusätzlich je Gebäude | Kapitel 4.5 und 8.2; Kennzahlen und Bericht in der [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) |
| **Q11** | **Entscheid E13 — Nutzfläche statt Wohnfläche.** Pflichtfelder des Imports sind die **Nutzfläche** (beheizte Netto-Grundfläche) und die **Raumhöhe**, alle übrigen Felder Vorgabe mit Herkunftsmarke. Bezugsfläche des Stundenmodells ist die Nutzfläche: A_IW = 2,5 · Nutzfläche, und die Skalierung nach E8 rechnet auf die Nutzfläche hoch | Absatz unter dieser Tabelle; Kapitel 4.3 und 4.7; Rechenschritte 1.1 und Schritt A; Umsetzungskonzept 2; Datenaustauschkonzept 3.4; Mehrzonenkonzept 4.2 |
| **Q12** | **Als geklärt zu betrachten.** Die KIT-Datei `AC20-FZK-Haus.ifc` kommt mit Quellenvermerk als Importprobe ins Repositorium; **Importproben werden für alle Importwege umgesetzt** — IFC und gbXML, dazu die selbst erzeugten Rundlaufdateien; RWTH- und bim2sim-Dateien erst nach Lizenzklärung | Kapitel 7.8; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 8.3 und 9; Ablage `Referenzlaeufe/Importproben/` |
| **Q13** | **Durch E9 überholt:** der gbXML-Import ist nicht Kür in G5, sondern **Pflicht in G4c**, dazu kommen beide Exporte als Stufe G7 | N1.13; Datenaustauschkonzept 3 und 10 |
| **Q15** | Reihenfolge G0 → G1 → G2 → G3 → G4 → G5; **G0 und G1 als erste Beauftragung**, G0 zuerst — ohne bestandene Normtests keine Anbindung | Kapitel 11 und 16 |
| **Q17** | Verschattungsfaktor und Rahmenanteil als Felder mit Vorgabe je Lage (0,9 / 0,8 / 0,7) bzw. 0,3 — der größte geratene Hebel; eine Vorgabe je Baualter erst, wenn `Baujahr` vorliegt (G4) | Kapitel 4.4; Schemaschritt 77 (6.1); Dialoggruppe im Umsetzungskonzept 2 |
| **Q18** | Harte Plausibilitätsprüfungen im VDI-6007-Weg, im Tagesmodell nur Warnung — damit die Basis unberührt bleibt | Kapitel 4.8; die Warnungen gehören in den Einfrierschritt GB (Kapitel 16) |
| **Q19** | Sommerlüftungsregel und die Trennung von Infiltration und Nutzerlüftung in **G2** — für die Jahresheizwärme unerheblich, für die Überhitzungskennzahl entscheidend | Kapitel 4.4 und 11; die Kennzahl bleibt bis dahin vorläufig (Kapitel 14) |
| **Q20** | Fassadenstrahlung im Gebäudemodell mit Hay-Davies aus GHI/DNI/DHI; die isotropen `Sol_*`-Spalten bleiben dem Bestandsweg | N1.4; Schritt E der Rechenschritte |
| **Q21** | Eine Zeitbasis (Ortszeit) für das Gebäudemodell wie bei PV und Solarthermie; `Tab_Klimadaten` braucht das Modell nicht | Kapitel 4.4; Schritt E der Rechenschritte |

**Zu E13 (Q11): eine Annahme und eine Rückfrage — durch E19 (N1.24) erledigt.** Entschieden war am Vormittag die **Bezeichnung**, nicht die
Spalte. Der Orchestrator nimmt an — **Annahme, keine Festlegung** —, dass die Bestandsspalte
`Tab_Gebaeude.Wohnflaeche` technisch bestehen bleibt, weil der Tagesbilanz-Weg und die
Referenzbasis sie lesen, und dass sie im Stundenmodell, in den Dialogen und in den Importen nur
als **„Nutzfläche"** beschriftet und gelesen wird: **keine neue Spalte, kein Schemaschritt dafür.**
Daraus folgt die **Frage Q11a** an den Anwender: Bleibt es bei der einen Spalte unter neuem Namen
in der Oberfläche — oder soll das Schema eine eigene Spalte `Nutzflaeche` bekommen, mit Migration
der Bestandszeilen und einem eigenen Einfrierschritt? Die Antwort kam am selben Tag: **E19** (N1.24)
wählt die eigene Spalte; die Annahme ist aufgehoben, die gekennzeichneten Stellen sind nachgezogen.

**Was danach offen ist.** **Q10** (IFC-Import auch auf iOS) wurde am 16.09.2026 zunächst nicht genannt
und ist am selben Tag nachentschieden (**E18**, N1.23), **Q11a** ebenso (**E19**, N1.24) — von den 23 Fragen ist keine mehr offen; **Q24** (Ende des Übergangs) und **Q25** (Umfang der Stufe GA, Befund X) kamen mit **E20** hinzu (N1.25), **Q26** (Stufenplan der Anlagenkopplung) mit **E22** (N1.27). Alles Übrige war schon entschieden: **Q8** mit **E12**
(N1.18), **Q6** mit **E14** — der Anwender hat die Erläuterung erbeten und am selben Tag
entschieden (N1.19) —, davor **Q1** (E1), **Q2** samt Klimabasis (E5), **Q9** (E3), **Q14, Q22,
Q23** (E4) und **Q16** (E2).

### N1.18 Entscheid E12 — Q8: Kühlung wird aufgenommen

Anwender, 16.09.2026: „Q8: Kühlung aufnehmen, konzept dazu erweitern."

**Entscheid E12: Kühlung wird als vierter Kanal aufgenommen.** Neben `HEIZUNG`, `BRAUCHWASSER`
und `PROZESS` tritt der Kanal **`KUEHLUNG`**, und sein Bedarf wird **durch Kälteerzeuger gedeckt**
— er bleibt nicht länger eine bloß informative Reihe je Gebäude. Die Antwort aus Rev. 1 („Nein —
informativ", Kapitel 13, Q8) ist damit aufgehoben.

**Das Konzept dazu ist ein eigenes Papier.** Es entsteht als
[`Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md`](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) (in Arbeit; der Bestandsbefund W über Kanäle,
Erzeuger, Speicher, Wirtschaftlichkeit und Bericht läuft) — solange die Datei fehlt, steht ihr
Name hier als Text ohne Verweis. Es setzt seine **eigenen Fragen K1 …** und regelt, was der vierte
Kanal in Rechenkern, Datenmodell, Oberfläche, Bericht, Wirtschaftlichkeit und Regressionsnetz
auslöst. **Dieses Papier entscheidet davon nichts vor**; es vermerkt nur, wo E12 den bisherigen
Stand aufhebt.

| Stelle | Bisheriger Stand | Folge aus E12 |
|---|---|---|
| Kapitel 8.2 (Bedarfsdialog und Ergebnis) | Kühlenergie und Stunden mit Kühlbedarf als informative Kennzahlen | ein Kanal mit Deckung: Erzeugerwahl, Deckungsanteile, Kennzahlen — die Aufteilung setzt das Kühlkonzept |
| Kapitel 9 (Bericht, Diagramme, Wiki) | Kühlkennzahlen neben den Heizkennzahlen | eigene Kanalzeile, Deckungsbild, Wiki-Abschnitt samt Logbuch-Eintrag |
| Kapitel 15 (Abgrenzung) | „Kühlung als Kanal und Kältemaschinen" ausgeschlossen | **aufgehoben** — der Ausschluss trägt jetzt den Vermerk auf diesen Abschnitt |
| [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) B9, Abwägung 10, Wiedervorlage | „Drei Kanäle, und Kühlung ist keiner" | Nachtragssatz an den Kanalaussagen gesetzt; die Kanalzahl selbst ändert das Kühlkonzept |
| [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) (Ergebnisdarstellung, Abgrenzung) | „Informative Reihe, kein vierter Kanal" | Nachtragssatz gesetzt; Kennzahlen, Export und Bericht zieht das Kühlkonzept nach |
| [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 6 (Abgrenzung) | mit Kapitel 15 ausgeschlossen | Verweis auf E12 gesetzt |
| [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 12 (Abgrenzung) | Kühlung je Zone ausgeschlossen | Verweis auf E12 gesetzt; die Kühllast je Zone gehört ins Kühlkonzept |
| Referenzbasis | dreizehn Projekte, Kanalsummen ohne Kühlung | ein vierter Kanal bewegt Kanalsummen, Deckung und Wirtschaftlichkeit: das verlangt einen eigenen, begründeten Einfrierschritt — das Kühlkonzept benennt ihn und seinen Platz in der Reihenfolge (Kapitel 16) |

**Was E12 nicht ist.** Kein Auftrag, den Kanal vor G1 zu bauen, und keine Änderung an den Stufen
G0 bis G2: das Stundenmodell rechnet die Kühlleistung schon als Reihe (4.5), erst die Deckung
macht daraus einen Kanal. Welche Stufe ihn bringt, setzt das Kühlkonzept.

### N1.19 Erläuterung zu Q6 — Fensterpfad: drei Wege

Anwender, 16.09.2026: „Q6: was spricht gegen ‚eigener Oberflächenknoten?' ? -> diese auswahl
scheint mir vernünftiger. erläutere". Die Erläuterung ist am selben Tag beantwortet worden; der
Entscheid **E14** steht am Ende dieses Abschnitts. Die Erläuterung im Wortlaut:

Drei Wege stehen zur Wahl. **(A) Klassenweg des Prototyps:** das Fenster hängt als masseloser
Widerstand am Luftknoten, zusammen mit Lüftung und Wärmebrücken in R_ext; die Fensterfläche nimmt
nicht am Strahlungsaustausch der Oberflächen teil. **(B) Normweg nach VDI 6007 Blatt 1,
Gl. (25)–(28):** das Fenster gehört zum AW-Zweig; R_AF wird in R_1,AF = R_AF/6 und
R_Rest,AF = 5/6 · R_AF geteilt und parallel zu den Außenwänden am gemeinsamen Oberflächenknoten
θ_s,AW angeschlossen; die Fensterfläche zählt in der Strahlungsverteilung und in der Gewichtung
der äquivalenten Außentemperatur nach Gl. (41). **(C) Eigener Oberflächenknoten θ_s,AF:** ein
dritter Oberflächenknoten ohne Kapazität, mit eigenem konvektiven Übergang zur Luft, eigenem
Strahlungswiderstand zu den anderen Oberflächen und R_AF nach außen.

Was gegen (C) spricht: Erstens verlässt (C) das Netz der Richtlinie, das genau zwei
Oberflächenknoten kennt; die zwölf Testbeispiele und die Prüfregel 6.6 prüfen das Normnetz, und
der Ausweis „Rechenkern nach VDI 6007 Blatt 1" gilt für den Kopplungsweg dann nur mit einer
benannten Erweiterung — wie bei Kusuda und Hay-Davies. Zweitens legt die Richtlinie die Parameter
eines dritten Knotens nicht fest (Strahlungswiderstand Fenster–Innenwand, Fenster–Außenwand,
konvektiver Übergang am Glas); EPOS müsste sie selbst setzen. Drittens ist ein Knoten ohne
Kapazität algebraisch eliminierbar: mit denselben Übergangskoeffizienten wie an der Außenwand
fällt (C) rechnerisch auf (B) zurück — die Teilung R_AF/6 der Richtlinie ist genau diese
Elimination in vereinfachter Form. (C) liefert also nur dann etwas Neues, wenn seine Parameter von
denen der Wand abweichen, und dann liefert es vor allem eine eigene Glasoberflächentemperatur.

Was für (C) spricht: eine eigene Glasoberflächentemperatur verbessert die operative Temperatur und
Behaglichkeitskennzahlen (kalte Scheibe im Winter, warme im Sommer), erlaubt Tauwasser- und
Sommerbewertungen je Fensterlage und trägt die Kühllast mit Solargewinnen genauer — Punkte, die
mit E12 (Kühlung) an Gewicht gewinnen. Auf die Jahresheizwärme wirkt der Unterschied zwischen (B)
und (C) klein; auf Spitzen und Überhitzungsstunden kann er sichtbar werden.

Empfehlung: **(B) schon in G1**, nicht erst in G3 — der Löser entsteht in G0 neu, der Normweg
kostet dort nichts zusätzlich, und die Basis wird mit G1 ohnehin neu eingefroren; die in G3
geplante Umstellung des Fensterpfads (Mehrzonenkonzept 3.3) und der damit verbundene zweite
Einfrierschritt entfallen. **(C) als benannte EPOS-Erweiterung mit G3** (Bauteilkatalog, Fenster je
Lage mit eigenem U und g), wenn die Behaglichkeits- und Kühlkennzahlen sie brauchen; bis dahin
bleibt sie Option. Wer (C) sofort will, trägt die Zusatzannahmen und den Nachweis, dass die
Testbeispiele 5, 8 und 9 im Normband bleiben.

**Entscheid E14 (16.09.2026).** Anwender: „Q6: wie empfohlen, erst mit G3." Damit gilt:

- **Weg B ist der Produktweg und gilt schon im Klassenweg der Stufe G1**, nicht erst mit G3: das
  Fenster liegt im AW-Zweig, R_AF geteilt in R_1,AF = R_AF/6 und R_Rest,AF = 5/6 · R_AF, parallel
  nach den Wänden am gemeinsamen Oberflächenknoten θ_s,AW; die Fensterfläche zählt in der
  Strahlungsverteilung und in der Gewichtung der äquivalenten Außentemperatur nach Gl. (41).
- **Weg C** (eigener Oberflächenknoten θ_s,AF) ist eine **benannte EPOS-Erweiterung frühestens mit
  G3** — nur, wenn die Behaglichkeits- oder Kühlkennzahlen sie brauchen, und nur mit den
  Zusatzannahmen und dem Nachweis, dass die Testbeispiele 5, 8 und 9 im Normband bleiben.
- **Weg A** (Prototyp: Fenster als Widerstand am Luftknoten in R_ext) **entfällt für das Produkt**;
  der Prototyp bleibt Prüfwerkzeug, und die mit ihm gemessenen Zwischenwerte in Kapitel 5 und im
  Zahlenweg der Rechenschritte sind nach dieser Konvention entstanden.
- **Die Abnahme von G0 und G1 ändert sich nicht:** alle zwölf Testbeispiele im Normband nach E10.
  Die im [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) für G3 vorgesehene Umstellung
  des Fensterpfads samt ihrer Einfrierfolge **entfällt**; die Umstellung von R_rad auf Gl. (29)/(31)
  in G3 bleibt davon unberührt.

**Vermerk 22.09.2026:** Der Rest des Fensterzweigs `R_Rest,AF` enthält den äußeren
Wärmeübergangswiderstand des Fensters, damit die Summe des Zweigs 1/(U·A) ergibt und zu Gl. (27)
passt; die Angabe 5/6·R_AF oben gilt nur für den Anteil ohne äußeren Übergang. Das ist eine
Korrektur der Formel beim Abschluss von G0, kein neuer Entscheid — E14 bleibt, wie er ist
([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) A7a, B6).

### N1.20 Entscheid E15 — Wärmepumpen mit Kühlfunktion: Auswahl und Konfiguration

**Entscheid E15 (Anwender, 16.09.2026), im Wortlaut:**

> „Konzept: es gibt Wärmepumpen mit Kühlfunktion. Diese sind auch im Katalog. Es sollte eine
> Auswahl mit Wärmepumpen mit Kühlfunktion geben und entsprechender Konfiguration, so dass die
> Anforderungen an einen Erzeuger erfüllt sind und insbes. die Gebäudesimulation nach VDI 6007
> dann möglich wird."

**Die Festlegung in fünf Sätzen.** Erstens: „Kühlfähig" heißt zweierlei — **kühlfähig im
Katalog** (`Tab_WP_STAMM.Kuehlleistung > 0`, die Angabe, die ein Datenblatt führt) und
**rechenbar kühlfähig** (es liegt eine Kühlkennlinie vor); gerechnet wird mit der Kennlinie, und
der Katalog wird nach der Nennleistung durchsucht. Zweitens: Die Katalogliste der Wärmepumpe
bekommt die Auswahl **„nur mit Kühlfunktion"** auf der vorhandenen Spalte „Kühlen", die heute
angezeigt, aber nicht gefiltert werden kann. Drittens: Die Übernahme eines Katalogsatzes in ein
Projekt bringt die Kühlkennlinie **bereits heute** vollständig mit — auf allen drei Wegen
(Katalogsatz, Gewerkübernahme, Projektduplikat); daran ist nichts zu bauen, wohl aber ein
Datenbankfall, der es festhält. Viertens: Die Projektanlage bekommt vier Einstellungen —
Kühlbetrieb, Kühl-Vorlauf (er wählt die Kennlinie, wie der Heizvorlauf es auf der Wärmeseite
tut), Umschaltregel und Hilfsstromanteil —, und der Kühlbetrieb ist **nur** einschaltbar, wenn
eine Kühlkennlinie vorliegt; fehlt sie bei vorhandener Nennkühlleistung, erscheint eine benannte
Warnung statt einer stillen Ablehnung. Fünftens: Eine so konfigurierte Anlage rechnet die
Simulation mit Kühlung vollständig — Bedarf, Deckung, Kältestrom, Kosten, Emissionen, Kennzahlen
und Bericht —, und zwar **frühestens nach G1 + G2 + KU1 + KU2**, weil das Stundenmodell die
Kühllast liefert, die Sommerlüftung sie auf das richtige Maß bringt, KU1 den Kanal öffnet und
KU2 den Erzeuger bringt.

**Wo das ausgeführt ist.** Abschnitt **5.0** des
[Kühlkonzepts](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) — mit der Messung, wie viele
Katalogsätze es betrifft, mit der Stelle, die das Filtern heute verhindert, mit den
Übernahmewegen und mit den Konfigurationsfeldern. Die Schemaspalten stehen dort in 7.3
(`KU-S3`), die Dialogführung in 8.2 und 8.6, die Proben in 10.2 und 10.3, der Aufwand in 11.1.

**Vier Fragen bleiben offen und gehören zu diesem Entscheid** (Kühlkonzept 12.1): **K20** — der
Weg zum Filter: benannte Ausnahme von der Regel „Kennzeichenspalten sind nicht filterbar" oder
eine eigene Filterart (Empfehlung: eigene Filterart). **K21** — Kühl-Vorlauf als Auswahl aus den
Stützstellen oder als freie Eingabe mit Interpolation (Empfehlung: Auswahl). **K22** — führt die
Spalte `COP` der Kühlkennlinie wirklich den EER, oder in manchen Herstellersätzen etwas anderes
(vor KU2 zu prüfen). **K23** — Hilfsstrom je Anlage oder pauschal je Projekt (Empfehlung: je
Anlage).

**E12 bleibt unberührt.** E15 entscheidet nichts über den Kanal, den Rechenweg oder die
Abgrenzung; er betrifft allein den Weg des Anwenders zum Kälteerzeuger. Die Ausschlüsse aus
Kapitel 15 — Feuchte und Entfeuchtung, Bauteilaktivierung als Funktion, Kältemittelemissionen —
gelten unverändert weiter.

### N1.21 Entscheid E16 — ADR-004 angenommen: gbXML-Leseweg

**Entscheid E16 (Anwender, 16.09.2026), im Wortlaut:** „ADR-004: Annehmen".

**Was damit gilt.** [`ADR-004_gbXML_LINQ_to_XML.md`](ADR-004_gbXML_LINQ_to_XML.md) steht auf
„Angenommen": gbXML wird mit LINQ to XML (`System.Xml.Linq`) gegen ein handgeschriebenes Modell
der rund 25 benötigten Elemente gelesen und geschrieben — reine BCL, keine Code-Erzeugung, keine
Reflexion, kein Paket. Der Import ist tolerant (Datei als Strom, Kodierung aus der XML-Deklaration,
Einheiten global und lokal aufgelöst, `RectangularGeometry` als Flächenquelle, unvollständige
Aufbauten als masselos mit Warnung, fehlende Konstruktionen als Herkunft „Vorgabe"); der Export ist
zweistufig (Stufe 1 schemagültig mit `version="6.01"`, Stufe 2 synthetische Quadergeometrie mit
Kennzeichnung „schematisch"); die Schemaprüfung läuft nur im Test gegen eine nicht ausgelieferte
XSD-Kopie. Damit ist Architekturfrage **A7** der Softwarearchitektur beantwortet und der Sperrpunkt
vor G4c aufgehoben.

**Was offen bleibt.** Der ADR entscheidet den Leseweg, nicht die Beauftragung: **D1** (gbXML-Import
vor IFC-Import), **D2** (gbXML-Export nur mit Stufe 2) und **D16** (Zonenbildung X1…X3 mit G6c)
bleiben Fragen des [Datenaustauschkonzepts](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) (11.1);
die Ablage der XSD-Kopie im Repositorium (D3, 11.2) ist mit G4c zu klären.

**Nachgezogen:** Statusdatei Abschnitte 1 und 3, Indexzeile, Fragentabellen der
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) (A7, Stufentabelle) und
des [Systementwurfs](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) (B4), Datenaustauschkonzept
Kapitel 1, [Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md).

### N1.22 Entscheid E17 — ADR-005 angenommen: Zonenkopplung im Mehrzonenmodell

**Entscheid E17 (Anwender, 16.09.2026), im Wortlaut:** „ADR-005: annehmen".

**Was damit gilt.** [`ADR-005_Zonenkopplung_Mehrzonenmodell.md`](ADR-005_Zonenkopplung_Mehrzonenmodell.md)
steht auf „Angenommen", und mit ihm sind zwei Fragen des
[Mehrzonenkonzepts](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) entschieden. **M1** — Kopplungsweg
**B**: Die Trennbauteile zur Nachbarzone gehören in die AW-Gruppe der Zone, die Nachbartemperatur
geht als θ_NR,eq nach Gl. (40) ein und wird nach Gl. (41)/(42) gewichtet, der Strahlungsaustausch
endet an der Zonengrenze; je Stunde läuft ein Gauß-Seidel-Durchlauf über die Zonen in fester
Reihenfolge, Abbruch bei 0,01 K **und** 0,1 W, höchstens 50 Durchläufe mit benanntem Fehler
(beteiligte Zonen und Volumenstrom), das Regelungsmuster des ersten Durchlaufs einer Stunde wird
festgehalten und jeder Wechsel gezählt. **M4** — der Zonen-Luftaustausch kommt in **G6b**, als
Paare mit `CHECK (ID_ZoneA < ID_ZoneB)` (Schemaschritt S-G), im selben Durchlauf berücksichtigt.
Das validierte 7R2C-Netz je Zone bleibt unverändert; ein Gebäude ohne Zonendaten rechnet bitgleich
wie das Einzonenmodell desselben Programmstands. Damit ist **A8** der Softwarearchitektur
beantwortet und der Sperrpunkt vor G6b aufgehoben.

**Messpflicht, die zum Entscheid gehört.** Die Wahl wird gemessen bestätigt, nicht vorausgesetzt:
Vorschlag A (Vorstunde) bleibt als Vergleichsrechnung stehen, das 4×4-Gesamtsystem für zwei Zonen
ist das exakte Prüforakel (Mehrzonenkonzept, Probe 6), und die Probe „eine Zone bitgleich zum
Einzonenmodell" ist das Gate von G6b.

**Normstatus.** Der Produktausweis lautet weiterhin „Rechenkern nach VDI 6007 Blatt 1" **je Zone**;
die Kopplung ist eine benannte EPOS-Erweiterung wie Kusuda und Hay-Davies. „Mehrzonensimulation
nach VDI 6007" wird nirgends behauptet.

**Nachgezogen:** Statusdatei Abschnitte 1 und 3, Indexzeile, Mehrzonenkonzept (Kapitel 1 und 2,
Fragentabelle 10: M1, M4), [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md)
(A8, Stufentabelle), [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) (B5),
[Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md).

### N1.23 Entscheid E18 — Q10: IFC-Import auch auf iOS

**Entscheid E18 (Anwender, 16.09.2026), im Wortlaut:** „Q10: Umsetzen/Empfehlung".

**Was damit gilt.** Der IFC-Import steht auf **beiden Schalen** zur Verfügung; der Leser wird auch
in der iOS-Hülle registriert, nicht benannt abgelehnt. Der Rechenweg ist plattformfrei (7.4: die
Entity-Factory von xBIM ist erzeugter Code, kein `Reflection.Emit`); das iOS-Risiko ist das
Trimming der Metadatenklasse, Abhilfe ist ein `TrimmerRootDescriptor`. Mit der Empfehlung
angenommen sind die beiden Schärfungen des
[Umsetzungskonzepts](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md): Das Größenlimit
wird **benannt abgelehnt**, nicht versucht, und die iOS-Zahl wird in G4 **gemessen**, nicht
geschätzt — die 50 MB aus 7.9 sind der Windows-Richtwert bis zur Messung (**U11**); der Gerätebau
wird **einmalig von Hand** nachgewiesen, nicht dauerhaft im iOS-Workflow (**U16**). Der
Trimming-Nachweis ist Teil von G4 und läuft in einem iOS-Lauf **nach Rückfrage** — jeder
macOS-Läufer braucht das Wort des Anwenders, ohne Ausnahme.

**Was das für die Architektur heißt.** Die Naht `IGebaeudeLeser` (Softwarearchitektur A2) wird
trotzdem von Anfang an gezogen — nicht, um iOS abzulehnen, sondern damit ein späterer Umzug des
Lesers in ein eigenes Projekt eine Fabrikzeile statt eines Umbaus kostet, falls der Gerätebau
Typen vermisst. A2 selbst bleibt eine Frage des Architekturpapiers.

**Nachgezogen:** Statusdatei Abschnitte 1 und 2 (G4), Kapitel 13 (Q10), Umsetzungskonzept Kapitel 5
(U11, U16), [Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md).

### N1.24 Entscheid E19 — Q11a: eigene Spalte `Nutzflaeche`

**Entscheid E19 (Anwender, 16.09.2026), im Wortlaut:** „Q11a: Option (b) — Wohnflaeche sollte
zukünftig Nutzflaeche sein".

**Was damit gilt.** Die Bezugsfläche des Gebäudes heißt im Schema, was sie ist: Die Spalte
`Wohnflaeche` in `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` wird zu **`Nutzflaeche`** — eine
**Umbenennung mit Wertübernahme**, keine zweite Spalte. Die Bestandswerte gehen 1:1 über; ein
Umrechnungsfaktor von Wohn- auf Nutzfläche wird nicht angewandt, weil der Bestand die Fläche schon
heute als Bezugsfläche der Rechnung führt. Die Annahme aus N1.17 („eine Spalte, neue Beschriftung")
ist aufgehoben; die Regel für den Fall „beide Spalten gefüllt" aus dem Register entfällt, weil es
keine zwei Spalten gibt.

**Wo die Umbenennung geschieht.** Im Gebäudespalten-Schemaschritt (Papiername **M3**) zusammen mit
dem Sichtneubau: `ALTER TABLE … RENAME COLUMN` in beiden Tabellen, danach die Sicht
`Abfrage_Projektgebaeude` aus `GebaeudeSchema.SQL_VIEW_NEU` mit der Spalte `Nutzflaeche`. Alle
Leser und Schreiber ziehen im selben Schritt auf den neuen Namen: der Rechenweg
(`SimulationWaermebedarf.cs`), `GebaeudeStammCtrl`, die Modelle, `GebaeudeDialog` und
`GebaeudeKatalogDialog`, die Gebäudehüllen und der Bericht — rund 200 Fundstellen des Namens im
Quelltext, von denen etwa die Hälfte die Skalierungsspalten meint und bleibt.
`sql/schema/001_grundschema.sql` ist der eingefrorene Grundstand und wird nicht nachgezogen (wie es
W17 der Softwarearchitektur für die Sicht regelt). Aufwand rund 2–3 PT innerhalb von M3.

**Was unverändert bleibt.** `Wohnflaeche_gesamt` (Basis des Bewohner- und Verbrauchswegs) und die
Skalierungsspalten der Projektzuordnung (`Z_ProjektGebaeude.Wohnflaeche_Waermebedarf`,
`Einheit_Waermebedarf_Wohnflaeche`, `Z_AuswahlWohnflaeche`) bezeichnen die **Projektfläche des
Anwenders** im Skalierungsweg nach E8, nicht die Bezugsfläche des Modells; sie behalten Namen und
Bedeutung. Das ist eine Folge von E8, keine neue Frage.

**Referenzbasis.** Die Umbenennung ist ergebnisneutral: Der Referenzlauf muss **byte-gleich**
bleiben; eine Abweichung wäre ein Fehler der Migration, kein Einfrieranlass. Der Schemastand der
Testdatenbank wandert mit dem Schritt (Migrationstest, Auslieferungsvorlage, Schemawerkzeug).

**Nachgezogen:** Statusdatei Abschnitte 1 bis 3, Kapitel 13 (Q11), N1.17 (Annahme aufgehoben),
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (Eingabetabelle, Schritt A),
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (1.6, 2),
[Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (Flächenprobe der Zonen),
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) (W13, Schrittfolge M3),
[Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Kapitel 1 entfällt).

### N1.25 Entscheid E20 — Trennung der Rechenwege: VDI 6007 löst die Tagesbilanz ab, der Altweg bleibt nur als Übergang

**Entscheid E20 (Anwender, 16.09.2026), im Wortlaut:**

> „generell soll die neue (VDI 6007) und alte Gebäudeenergiebedarfsberechnung komplett getrennt
> werden. die Neue soll die alte ablösen, nur als Übergang die alte behalten werden. Alle Dialoge
> und eingaben sollen daher angepasst werden an die zukünftig alleinige VDI 6007 Struktur."

**Was sich gegenüber E1 ändert.** E1 hat das Stundenmodell zum Rechenmodell für alle Gebäude
gemacht und die Tagesbilanz als je Gebäude wählbare Ausnahme stehen lassen. Die Papiere haben das
als **eine Verzweigung im Bestandscode** ausgeführt: zwei Verzweigungspunkte in
`SimulationWaermebedarf.cs` (`:581`, `:647`), der Tagesbilanz-Zweig „Zeichen für Zeichen" im selben
Rumpf, der Gebäudedialog mit einem dritten Reiter, dessen sieben Modellparameterfelder im
Tagesbilanz-Weg versteckt oder gesperrt werden sollten (U2), und ein Referenzprojekt, das dauerhaft
auf Tagesbilanz steht (A15). E20 zieht daraus die Konsequenz: **Die beiden Rechenwege werden
vollständig getrennt, der alte ist ein Übergangsweg mit Ende, und die Oberfläche folgt allein der
Struktur des neuen.** E1 bleibt in der Sache bestehen — VDI 6007 für alle Gebäude, Basis neu
einfrieren —; was E20 schärft, ist die Bauform der Einbindung und die Befristung des Altwegs.

**Die Festlegungen.**

1. **Zwei Module, eine Weiche.** Der Tagesbilanz-Weg wird **Zeichen für Zeichen** in ein eigenes,
   abgeschlossenes Modul verschoben (Arbeitsname **Altweg**, Ordner
   `EPOS.Kern/Allgemein/Simulation/Altweg/`), das keine neue Funktion mehr bekommt und allein gegen
   die Referenzbasis gehalten wird. Der VDI-Weg ist das Modul `Gebaeude/` mit `GebaeudeModellEingang`,
   `Zonenmodell7R2C`, `GebaeudeModellErgebnis` und der Skalierung nach E8 — es ruft **nichts** aus
   dem Altweg. Die bisher geplanten zwei Verzweigungspunkte werden zu **einer Weiche am Eingang**
   der Gebäudebedarfsrechnung (Fassade `SimulationWaermebedarf`): sie liest den Rechenweg des
   Gebäudes und ruft genau ein Modul. Was beide Wege brauchen — Bewohnerzahl aus der Nutzfläche,
   Skalierungsfaktor nach E8, Klimareihen —, stellt ein **modellfreier Vorbereitungsschritt** vor der
   Weiche bereit; keiner der Wege ruft den anderen. Modellfrei sind dabei Klimakalender, Bewohner,
   Projekt- und Katalogfläche und der daraus gebildete Flächenfaktor; die Verbrauchs-Rückrechnung
   braucht mit `VerbrauchAlt` ein Ergebnis des gewählten Moduls und läuft deshalb **innerhalb** des
   Moduls als zweiter Aufruf, nie über die Modulgrenze — der Altweg rechnet seine Skalierung
   unverändert selbst und liest den Faktor nicht (Befund X, Punkt X5). Damit ist Architekturfrage **A16** (Zuschnitt
   des zweiten Verzweigungspunkts) gegenstandslos: Es gibt keinen zweiten Punkt.
2. **Dialoge und Eingaben in VDI-Struktur.** Gebäudedialog, Gebäudekatalogdialog, der
   Zuordnungsdialog der Skalierung (bisher `GebaeudeWohnflaecheDialog`) und der Bedarfsdialog werden
   nach den Eingaben des VDI-Wegs aufgebaut: Nutzfläche und Raumhöhe (E13, E19), Bauteile mit U·A
   (E2), Fenster je Orientierung mit Rahmenanteil und Verschattung, Randbedingung der Grundfläche,
   Modellparameter, Infiltration, Nutzerlüftung, Sommerlüftung, Heizung mit Strahlungsanteil und
   Leistungsgrenze. Die **Modellparameter sind immer sichtbar und bearbeitbar**, auch bei einem
   Gebäude auf dem Altweg — sie gelten dann nach der Umstellung. Felder, die **nur der Altweg**
   liest, erscheinen nur bei einem Gebäude auf dem Altweg, in einem **eingeklappten Abschnitt
   „Übergang: Tagesbilanz"** mit dem Hinweis, dass dieser Weg entfällt; sie verschwinden mit ihm.
   Der Modellschalter heißt **„Rechenweg"** mit den Werten **„VDI 6007"** (Vorgabe, NULL) und
   **„Tagesbilanz (Übergang)"**, trägt eine Herleitungszeile und wird mit dem Altweg entfernt.
   Frage **U2** ist damit überholt.
3. **Datenmodell.** Es gibt **keine zweite Gebäudetabelle**. Der Gebäudespalten-Schritt (M3, U5)
   bringt die VDI-Spalten und die Umbenennung nach E19; Spalten, die nur der Altweg liest, werden
   dort **nicht angefasst** und entfallen erst mit dem Altweg. Welche Spalten das sind, weist ein
   eigener Befund je Feld aus (Klasse „nur Altweg", „beide", „nur VDI"):
   [`Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md`](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md).
4. **Der Übergang hat ein Ende: Stufe GA — Altweg entfernen.** Sie entfernt Modul, Weiche,
   Rechenweg-Schalter und die Altweg-Spalten (ein Schemaschritt mit `DROP COLUMN` und Sichtneubau),
   stellt das Referenzprojekt des Übergangs auf VDI 6007 um, stellt den Rückweg-Test ein, friert die
   Basis neu ein und trägt den Logbuch-Eintrag ins Wiki. Der Zeitpunkt ist die neue Frage **Q24**;
   Empfehlung: **frühestens nach G3**, wenn alle Referenzprojekte und die Bestandsprojekte des
   Anwenders einmal auf VDI 6007 gerechnet und geprüft sind. Bis dahin bekommt der Altweg **keine
   Änderung außer Fehlerbehebung**; die Bestandsbefunde GB werden **vor** der Verschiebung
   behoben, damit das verschobene Modul der geprüfte Stand ist.
5. **Bericht und Ausweis.** Ein Gebäude auf dem Altweg trägt im Bericht und im Bedarfsdialog die
   Zeile **„Tagesbilanz (Übergangsweg)"** statt des Produktausweises nach E10; der Vergleich beider
   Wege im Bedarfsdialog (Stufe G2) bleibt als Übergangshilfe und entfällt mit GA.
6. **Referenzbasis.** Unverändert: G1 + G2 frieren die Basis neu ein, ein Referenzprojekt steht für
   die **Dauer des Übergangs** auf dem Altweg (A15, Wortlaut angepasst) und hält den Rückweg-Test.
   Die Verschiebung des Altwegs in sein Modul ist ergebnisneutral und wird mit einem **byte-gleichen**
   Referenzlauf abgenommen — vor der Anbindung des VDI-Wegs, als eigener Schritt innerhalb von G1.
   GA ist ein Einfrierschritt.

**Aufwand.** Die Trennung kostet in G1 zusätzlich rund **3–5 PT** (Verschieben des Altwegs,
Fassade, Vorbereitungsschritt, byte-gleicher Nachweis), der Übergangsabschnitt im Dialog rund
**1 PT**; die Stufe GA rund **5–8 PT**. Der Dialogumbau selbst war mit E2 und E13 bereits geplant
und wird durch E20 nicht größer, sondern klarer.

**Was bleibt, was entfällt.**

| Bisher | Nach E20 |
|---|---|
| Zwei Verzweigungspunkte im Bestandsrumpf, Tagesbilanz-Zweig „Zeichen für Zeichen" im selben Code | eine Weiche am Eingang, Altweg als eigenes Modul, VDI-Weg als eigenes Modul, gemeinsamer Vorbereitungsschritt |
| Gebäudedialog mit Modellreiter, Felder je nach Modell versteckt oder gesperrt (U2) | Dialog in VDI-Struktur; Altweg-Felder nur im eingeklappten Übergangsabschnitt eines Altweg-Gebäudes |
| Modellwahl `Gebaeude_Modell` als gleichrangige Wahl | Schalter „Rechenweg" mit Vorgabe VDI 6007 und Wert „Tagesbilanz (Übergang)", entfällt mit GA |
| Referenzprojekt dauerhaft auf Tagesbilanz (A15) | für die Dauer des Übergangs, Umstellung mit GA |
| kein Ende des Altwegs vorgesehen | Stufe GA, Zeitpunkt Q24 |
| A16 offen | gegenstandslos |

**Nachgezogen:** [`ADR-006`](ADR-006_Trennung_Altweg_VDI6007.md) (neu, angenommen) und ein
Ergänzungsvermerk in [`ADR-002`](ADR-002_Stundenmodell_VDI6007_Einbindung.md); Kapitel 11
(Stufe GA) und 13 (Q24) dieses Papiers; Statusdatei Abschnitte 1 bis 3; Nachträge in
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md),
[Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) und
[Rechenschritten](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md); Befund X;
[Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (U2 und A16
gestrichen, Q24 neu, A15 im Wortlaut, Festlegung zur Dialogstruktur in Kapitel 8).

### N1.26 Entscheid E21 — Simulation Kältebedarf analog zu Simulation Wärmebedarf

**Entscheid E21 (Anwender, 16.09.2026), im Wortlaut:** „erweitere dabei auch den Teil Simulation
Kältebedarf analog zu Simulation Wärmebedarf."

**Was damit gilt.** Die Kälteseite bekommt denselben Aufbau wie die Wärmeseite — nicht als
Anhängsel des Wärmebedarfs, sondern als Gegenstück mit denselben Mustern:

1. **Zwei Fassaden, ein Lauf.** Neben `SimulationWaermebedarf` steht die Fassade
   **`SimulationKaeltebedarf`**. Beide lesen denselben modellfreien Vorbereitungsschritt (E20), und
   beide werden vom VDI-Modul `Gebaeude/` bedient, das je Stunde Heiz- **und** Kühllast in **einem**
   Lauf liefert; die Fassaden verteilen die Ergebnisse auf ihre Kanäle, sie rechnen das Gebäude nicht
   zweimal. Auf der Kälteseite gibt es **keinen Altweg**: Der Tagesbilanz-Weg liefert keine
   Kühllast, ein Gebäude auf dem Altweg trägt Kältebedarf 0 mit dem benannten Hinweis „Tagesbilanz
   (Übergangsweg) liefert keine Kühllast" — nie eine stille Null.
2. **Ergebnis und Kennzahlen.** Der vierte Kanal `KUEHLUNG` (E12) führt die Stundenreihe positiver
   Kältemengen (Vorzeichenregel des Kühlkonzepts 3.3). Die Kennzahlen spiegeln die Wärmeseite:
   Jahreskälte, Kältespitze **`Kaeltebedarf_Max`** als Gegenstück zu `Waermebedarf_Max` (Rohfeld der Fassade; das
   Ergebnisfeld heißt `Kaeltelast_Max`, wie `Waermebedarf_Max` → `Waermelast_Max`), Stunden mit
   Kühlbedarf, Überhitzungsstunden; die Dauerlinie der Kälteseite ist ein eigenes Bild.
3. **Bedarfsdialog.** Ein Abschnitt **„Kältebedarf"** steht neben dem Abschnitt „Wärmebedarf" auf
   Gebäude- und Projektebene und benutzt dieselben Bausteine (Kennzahlkachel, Kanalzeile,
   Monatsstapel, Dauerlinie). Die Eingaben liegen in der Gruppe „Kühlung" des Gebäudedialogs, die
   Teil der VDI-Struktur ist (E20); der Umfang der Eingaben bleibt Frage K11.
4. **Deckung.** `DeckungKanalKaelte` folgt den Mustern der Wärmedeckung — Deckungsreihenfolge,
   Unterdeckung als eigene Zeile, Kälteprobe je Stunde an der Stelle der Energieprobe; Kälteerzeuger
   sind die Wärmepumpen mit Kühlfunktion (E15) und die Kältemaschine (KU3).
5. **Bericht, Wirtschaftlichkeit, Emissionen.** Abschnitte „Kältebedarf und -deckung" analog zur
   Wärmeseite; der Kältestrom steht in der Strombilanz; der Ausweis gilt je Kanal.
6. **Stufen.** KU1 = Fassade `SimulationKaeltebedarf`, Kanal, Kennzahlen und Bedarfsdialog-Abschnitt
   (mit G1 + G2); KU2 = Deckung; KU3 = Kältemaschine und Zeitprofil. Die **Symmetrie ist
   Bauvorschrift**: gleiche Klassenmuster, gleiche Dialog- und Berichtsbausteine wie die Wärmeseite;
   jede Abweichung wird benannt. Die Wache aus E20 (kein Verweis zwischen `Altweg/` und `Gebaeude/`)
   gilt auch für die Kältefassade.

**Was damit entschieden ist.** Frage **K1** des Kühlkonzepts (vierter Kanal in der bestehenden
Kanalstruktur oder eigene Kältestruktur) ist in der Sache beantwortet: vierter Kanal mit getrennter
Deckungsseite — die Empfehlung des Papiers. **K2** (Vorzeichen) bleibt offen; **K11** (Umfang der
Kühleingaben) bleibt offen.

**Nachgezogen:** Statusdatei Abschnitte 1 und 3; [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md)
Rev. 3 (Kapitel 0, 1, 3.7, 4, 5, 6.4, 8, 10, 11, 12); [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md)
(Klassenliste, Integration, Dialogführung, Stufentabelle); [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md)
(Anforderung, Komponentenbild, Nachweis); [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md);
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 1.1;
[Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (K1 gestrichen).

### N1.27 Entscheid E22 — Anlagenkopplung: Vorlauftemperatur und Erzeugerfahrplan an der Raumtemperatur

**Frage des Anwenders (16.09.2026):** „kann das Gebäudesimulationskonzept erweitert werden um die
Kopplung von Vorlauftemperatur und Erzeugerfahrplan an die Raumtemperatur?" — **Entscheid E22, im
Wortlaut:** „trage es als Nachtrag mit einer neuen Frage Q26 zum Stufenplan ein und schreibe ein
eigenes Konzeptpapier dazu".

**Was damit gilt.** Die Kopplung wird Gegenstand der Gebäudesimulation — als **benannte
EPOS-Erweiterung** wie das Kusuda-Erdreich oder die Zonenkopplung, nicht als Bestandteil der
Richtlinie. Der Ausschluss in Kapitel 15 wird eingegrenzt: Die Kopplung ist **kein Bestandteil der
Stufen G0 bis GA**, aber Gegenstand des eigenen Papiers
[`Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md`](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md).
Kapitel 2.6 hat den Weg dahin schon benannt: Erst das stündliche, physikalisch geschlossene Modell
macht die Kopplung an Vorlauftemperaturen und Erzeugerfahrpläne überhaupt möglich.

**Drei Stufen, eine Vorstufe.**

| Stufe | Inhalt | Wo sie lebt | Frühestens |
|---|---|---|---|
| **AK0** | das Konzeptpapier | — | jetzt |
| **AK1 — Heizkreis als Randbedingung** | Vorlauf aus einer Heizkurve (außentemperaturgeführt, Auslegungspunkt) oder fest je Anlage; Übergabeleistung als Funktion von Heizmitteltemperatur und Raumtemperatur mit Heizkörperexponent; Rücklauf als Ergebnis. Reicht die Übergabe nicht, sinkt die Raumtemperatur — Leistungsvorgabe statt idealer Regelung; Aufheizspitzen nach Absenkung werden sichtbar; der benötigte Vorlauf geht in die Wärmepumpen-Kennlinien ein | Einbahnstraße Anlage → Gebäude, vollständig im Modul `Gebaeude/` | nach **G2** |
| **AK2 — Erzeugerfahrplan als Verfügbarkeit** | Sperrzeiten, Zeitprogramme, Leistungsgrenzen und Speicherstand begrenzen je Stunde, was ankommt; die Raumtemperatur fällt und erholt sich; Unterdeckung wird zu **Komfortstunden** statt zu ungedeckten Kilowattstunden | neue Naht zwischen Gebäudemodul und Deckung (Verfügbarkeit als Profil oder gekoppelt) | nach **GA** — **E23 (16.09.2026, N1.28) hebt auf:** AK2 folgt auf AK1 und eine Feldphase, AK3 danach |
| **AK3 — geschlossener Kreis** | Gebäude, Erzeugerkaskade und Speicher rechnen je Stunde zusammen; der Vorlauf wirkt auf den Wirkungsgrad zurück, die Regelung auf den Vorlauf; Iteration je Stunde nach dem Muster der Zonenkopplung (ADR-005) | ändert den Grundsatz „erst Bedarf, dann Deckung": der Bedarf wird Ergebnis | nach **AK2** und einer Feldphase |

**Was bleibt.** Das Raummodell bleibt das der VDI 6007 Blatt 1; die Normtestbeispiele rechnen
weiter mit idealer Regelung, der Produktausweis nach E10 ändert sich nicht. Grenzfallprobe jeder
Stufe: Mit unbegrenzter Übergabe rechnet der gekoppelte Weg **bitgleich** wie die ideale Regelung.
Jede Stufe ist **je Gebäude oder Projekt wählbar, Vorgabe aus**, und bekommt einen eigenen,
begründeten Einfrierschritt; Bestandsprojekte rechnen unverändert, solange sie aus ist. Der Altweg
wird nicht berührt; AK2 und AK3 kommen erst, wenn er entfernt ist — zwei Bedarfsbegriffe neben
zwei Rechenwegen wären nicht zu pflegen. **E23 (16.09.2026, N1.28) hebt auf:** AK2 folgt auf
AK1 und eine Feldphase, AK3 danach; Gebäude auf dem Altweg gehen als feste Last ein. Die
Kälteseite folgt nach E21 spiegelbildlich (Kaltwasser-Vorlauf, Kühlkennlinien).

**Was heute schon da ist und was fehlt.** Vorhanden: Sperrzeiten und Vorlauf je Wärmepumpe mit
Kennlinien über dem Vorlauf, Vorlauf und Rücklauf der Pufferspeicher, die Absenktage des Altwegs.
Es fehlen: eine Heizkurve mit Auslegungstemperaturen, die Übergabeart mit Exponent, ein
stündliches Zeitprogramm für den Raumsollwert und eine einheitliche Verfügbarkeit je Erzeugertyp —
Gebäude- und Anlagenspalten mit Vorgabewerten, kein neues Datenmodell.

**Aufwand, grob und ohne Befund** (das Papier beziffert nach): AK1 rund 8–12 PT, AK2 10–15 PT,
AK3 20–35 PT zuzüglich Neu-Einfrieren.

**Die neue Frage Q26** (Kapitel 13): Welche Stufen werden beauftragt und wann? Empfehlung: AK1 nach
G2 einplanen, AK2 und AK3 erst nach GA und einer Feldphase des VDI-Wegs — **offen**.

**Nachgezogen:** Kapitel 13 (Q26) und 15 dieses Papiers; Statusdatei Abschnitte 1 und 3;
Abgrenzungen in [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md),
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) und
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md); Indexzeile;
[Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Q26 neu).

### N1.28 Entscheid E23 — Der Altweg bleibt: Stufe GA entfällt

**Entscheid E23 (Anwender, 16.09.2026), im Wortlaut:** „GA: altweg soll bleiben."

**Was damit gilt.** Der Tagesbilanz-Weg — der **Altweg** — bleibt **dauerhaft** im Produkt. Die mit E20
vorgesehene Stufe **GA — Altweg entfernen** entfällt; es gibt kein Ende des Übergangs, weil es
keinen Übergang mehr gibt, sondern zwei Rechenwege nebeneinander: den VDI-Weg als Vorgabe für alle
Gebäude (E1) und den Altweg als **eingefrorenen Bestandsweg**, den ein Gebäude ausdrücklich wählt.
Frage **Q24** (Wann endet der Übergang?) ist damit beantwortet: nie. Frage **Q25** (Umfang der
Stufe GA) ist gegenstandslos: Es gibt keinen GA-Schemaschritt; `Typ`, die Tagesverteilung und die
Spalten, die nur der Altweg liest, bleiben. Die leserlosen Spalten `WW_Bedarf` und `Waermebedarf`
(Befund X) sind ein gewöhnlicher Aufräumpunkt ohne eigene Stufe.

**Was von E20 unverändert gilt.** Die **Trennung** bleibt der Kern: der Altweg als eigenes Modul
`Altweg/`, Zeichen für Zeichen verschoben und byte-gleich abgenommen; die Weiche am Eingang; der
modellfreie Vorbereitungsschritt; die Wache, dass kein Modul das andere ruft. Die **Dialoge** bleiben
in VDI-Struktur; Felder, die nur der Altweg liest, stehen bei einem Gebäude auf dem Altweg in einem
eingeklappten Abschnitt, der jetzt **„Tagesbilanz (Bestandsweg)"** heißt, nicht mehr „Übergang".
Der Schalter **„Rechenweg"** bleibt dauerhaft mit der Vorgabe „VDI 6007" und dem Wert
„Tagesbilanz". Ein Gebäude auf dem Altweg trägt im Bericht **„Tagesbilanz (Bestandsweg)"** statt
des Produktausweises nach E10. Die Kälteseite liefert für Altweg-Gebäude dauerhaft Kältebedarf 0
mit benanntem Hinweis (E21).

**Was der Altweg dauerhaft nicht bekommt.** Keine neue Funktion — nur Fehlerbehebung (die
Bestandsbefunde GB vor der Verschiebung); keine Kühllast; keine Anlagenkopplung (E22); keine
Zonen. Er ist der Vergleichsmaßstab, kein zweites Produkt.

**Referenzbasis.** Das Referenzprojekt auf dem Altweg bleibt **dauerhaft** in der Basis, der
Rückweg-Test bleibt dauerhaft. Der Einfrierschritt GA entfällt.
**E26 (17.09.2026, N1.31) präzisiert:** beides bleibt für die Dauer des Übergangs — bis zur
Stufe GA — in jeder Basis; GA ist wieder ein eigener Einfrierschritt, und für A15 gilt die
Fassung aus N1.25 Punkt 6 („für die Dauer des Übergangs").

**Anlagenkopplung (E22).** AK2 und AK3 waren an die Stufe GA gebunden, weil sie den Bedarfsbegriff
ändern. Ohne GA gilt: **AK2 und AK3 wirken auf Gebäude des VDI-Wegs**; Gebäude auf dem Altweg gehen
in Deckung und Kopplung als **feste Last** ein, wie heute. Voraussetzung für AK2 ist damit nicht GA,
sondern **AK1 abgenommen und eine Feldphase**; AK3 folgt auf AK2. Die Empfehlung zu **Q26** lautet
entsprechend: AK1 nach G2, AK2 nach AK1 und Feldphase, AK3 danach — Q26 bleibt offen.

**Aufwand.** Die 5–8 PT der Stufe GA entfallen. Dauerhaft bleiben die Kosten zweier Rechenwege: das
Referenzprojekt und der Rückweg-Test in jeder Basis, und jede gemeinsam genutzte Stelle
(Vorbereitungsschritt, Dialog, Kanäle, Vergleich alt/neu im Bedarfsdialog) ist für beide Wege zu
prüfen. Die Modultrennung hält diese Kosten klein.
**E26 (17.09.2026, N1.31) präzisiert:** Das Risiko „zwei Rechenwege nebeneinander" bleibt ein
benanntes Risiko und gilt bis zur Stufe GA; deren 5–8 PT entfallen nicht, sie stehen nur in
keiner Summe. Damit gelten auch N1.1 (E20 schärft E1 auf „Übergangsweg …, der mit Stufe GA
entfällt") und N1.25 Punkt 4 und 6 wieder.

**Wortwahl in allen Papieren.** „Übergang", „Übergangsweg", „befristet", „entfällt mit GA" sind
durch „Bestandsweg", „dauerhaft", „bleibt" ersetzt; die Stufe GA ist aus den Stufentabellen
genommen oder als entfallen gekennzeichnet.

**Nachgezogen:** Kapitel 0, 4.1, 6.1, 6.4, 8, 9, 10.4, 11, 12, 13 (Q24, Q25, Q26), 14, 15 und 16 dieses
Papiers; Statusdatei Abschnitte 1 bis 3; [`ADR-006`](ADR-006_Trennung_Altweg_VDI6007.md) und
[`ADR-002`](ADR-002_Stundenmodell_VDI6007_Einbindung.md) (Ergänzungsvermerke);
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md),
[Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md),
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md),
[Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md),
[Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Q24
entschieden, Q25 entfallen, Q26 und H-Fragen angepasst), Indexzeilen, Word-Kurzfassung.

### N1.29 Entscheid E24 — Die zwölf Fragen der Anlagenkopplung

**Entscheid E24 (Anwender, 16.09.2026), im Wortlaut:** „H-Fragen sind entschieden - ok".

**Was damit gilt.** Die zwölf Fragen H1 bis H12 des Papiers
[`Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md`](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md)
(Kapitel 13.1) sind sämtlich **nach Empfehlung des Papiers** entschieden; die technischen
Festlegungen H-F1 bis H-F12 (13.2) sind zur Kenntnis genommen. Der Stufenplan bleibt **Q26**.

| Nr. | Entscheid | Wirkt in |
|---|---|---|
| **H1** | Raumthermostat als P-Regler mit Proportionalband, Vorgabe 1 K; `Xp = 0` fällt bitgleich auf die ideale Regelung zurück | AK1 |
| **H2** | Heizkurve in AK1 außentemperaturgeführt; eine raumgeführte Korrektur erst in AK3 | AK1 |
| **H3** | Übergabe je Zone ab G6, je Gebäude davor; ein Vorlauf je Gebäude; mehrere Heizkreise benannt abgelehnt | AK1 |
| **H4** | Verfügbarkeit in AK2 als vorab gerechnetes Profil, der Speicher als Vorrat über die Sperrdauer; echte Kopplung erst AK3 | AK2 |
| **H5** | Komfortschwelle 1,0 K in der Nutzungszeit als Eingabe mit Vorgabe; drei Zahlen: Unterschreitungsstunden, Kelvinstunden, längste Strecke | AK2 |
| **H6** | AK3 wird jetzt nicht zugesagt; der Entscheid fällt nach einer Feldphase von AK1 und AK2 | AK3 |
| **H7** | Die Kopplung wirkt in beiden Läufen der Verhältnisrechnung (E8); die Nennleistung der Übergabe folgt bei NULL der skalierten Auslegungslast; eine feste Nennleistung wird im Bericht benannt | AK1 |
| **H8** | Wochenprofil als Spalte mit 168 Werten je Gebäude (Sollwertprofil) bzw. je Anlage (Zeitprogramm), strenger Parser; keine Profiltabelle | AK1, AK2 |
| **H9** | Kälteseite in AK1, sobald KU2 den Kühl-Vorlauf liefert; sonst benannt vertagt | AK1 |
| **H10** | Auslegungs-Außentemperatur aus der Klimareihe des Projekts hergeleitet als Vorgabe, ein Feld überschreibt; die Herleitung steht im Dialog | AK1 |
| **H11** | Die Dialoggruppe heißt **„Wärmeübergabe"**; die Wärmesenke behält „Heizkreis" | AK1 |
| **H12** | Leerer `Heizung_Strahlungsanteil` bedeutet künftig „Vorgabe der Übergabeart"; Glossar, Herleitungszeile und Datenbankfall halten die Bedeutungsänderung fest | AK1 |

**Was offen bleibt.** Q26 — welche Stufen wann beauftragt werden; nach E23 ohne Bindung an eine
Stufe GA. H6 ist kein offener Punkt, sondern ein vertagter Entscheid mit benanntem Zeitpunkt.

**Nachgezogen:** Statusdatei Abschnitte 1 und 3; Anlagenkopplung Kapitel 13.1 (Entscheidvermerk je
Zeile); [Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md)
(Kapitel 7 als Entscheidstand, 62 Punkte); Indexzeile; Word-Kurzfassung.

### N1.30 Entscheid E25 — Das Proportionalband des Raumreglers ist wählbar

**Entscheid E25 (Anwender, 16.09.2026), im Wortlaut:** „AK1:Raumthermostat als P-Regler mit 1 K
Proportionalband -> proportionalband soll wählbar sein, 2K, 0,5K oder frei".

**Was damit gilt.** Der Raumthermostat der Stufe AK1 bleibt ein **P-Regler** (H1, E24); sein
Proportionalband `Xp` ist im Dialog **wählbar**: **0,5 K**, **1 K** oder **2 K** als Schnellwahl oder
ein **freier Wert** zwischen 0 und 5 K. `Xp = 0` bleibt die ideale Regelung mit Grenze und rechnet
bitgleich wie der Bestand (Grenzfall der Anlagenkopplung, 3.7). Die **Vorgabe bleibt 1 K** (E24);
der Entscheid nennt keine neue Vorgabe.

**Was sich nicht ändert.** Gespeichert wird allein der Wert in `Regler_Proportionalband` (REAL, K;
NULL = Vorgabe 1 K) — keine zweite Spalte, kein weiterer Schemaschritt; AK-S1 behält seine
13 Spalten je Gebäudetabelle. Die Schnellwahl ist Dialogführung: Sie setzt den Wert; trifft ein
gespeicherter Wert keine Schnellwahl, zeigt der Dialog „frei" mit dem Wert. Der Rechenschritt H
liest weiter nur `Xp`; die Herleitungszeile lautet „Vorgabe 1 K (EPOS-Wert); 0 K = ideale
Regelung". Die Rechenproben „Grenzfall bitgleich" und „Reglerband" gelten unverändert; hinzu kommt
eine Dialogprobe (Schnellwahl setzt den Wert, ein Wert ohne Schnellwahl zeigt „frei").

**Nachgezogen:** Statusdatei Abschnitte 1 und 3;
[Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) (Kopf, F-A10, Reglerwahl
4.4, Spaltentabelle, Dialogbild und Feldtabelle, 13.1 zu H1);
[Register der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Kapitel 7,
H1); Indexzeile; Word-Kurzfassung (1.3, 8b.3, 8b.6, 8b.7).

### N1.31 Entscheid E26 — Klarstellung: Der Altweg ist Übergang, der VDI-Weg löst ihn später komplett ab

**Entscheid E26 (Anwender, 17.09.2026), im Wortlaut:** „Insbesondere ist wichtig, dass 1. das
Altmodell noch als Übergang funktioniert und 2. dass das Neumodell (VDI 6007) das alte später
komplett ablösen wird und eigenständig arbeiten muss." (aus dem Prüfauftrag „gehe alle Konzepte
… nochmals durch und prüfe auf Konsistenz und Umsetzbarkeit").

**Was damit gilt.** E23 („GA: altweg soll bleiben") bedeutet: Der Altweg bleibt **jetzt** — als
funktionierender **Übergang**, nicht auf Dauer. Die Lesart „dauerhaft, nie entfernen" aus N1.28
ist zurückgenommen; die Sachaussagen von N1.28 zum Altweg selbst bleiben: eigenes Modul `Altweg/`,
Zeichen für Zeichen verschoben und byte-gleich abgenommen, keine neue Funktion (nur Fehlerbehebung
GB), keine Kühllast, keine Anlagenkopplung, keine Zonen; je Gebäude ausdrücklich wählbar; Dialog
in VDI-Struktur mit eingeklapptem Abschnitt „Tagesbilanz (Bestandsweg)"; Ausweis „Tagesbilanz
(Bestandsweg)"; Kältebedarf 0 mit Hinweis; in der Anlagenkopplung feste Last.

**Die Ablösung kehrt in den Plan zurück.** Die Stufe **GA — Altweg ablösen** ist wieder die letzte
Stufe des Stufenplans, ohne Termin und in keiner Summe (5–8 PT). **Q24** ist wieder offen und
lautet jetzt: *Wann ist der VDI-Weg bewährt genug, dass GA beauftragt wird?* Empfohlenes
Ablösekriterium: (1) alle Referenz- und Bestandsprojekte des Anwenders sind einmal auf VDI 6007
gerechnet und die Abweichung zum Altweg ist je Projekt erklärt; (2) eine Feldphase von mindestens
einer Heizperiode ohne offenen Fehler am VDI-Weg; (3) KU1 und, falls beauftragt, AK1 sind
abgenommen; (4) die Ausbauprobe ist grün. **Q25** lebt wieder als *Umfang der Stufe GA*;
Empfehlung unverändert zu N1.25 Punkt 4: Modul, Weiche, `IGebaeudeRechenweg`, Modultrennungswache,
Schalter „Rechenweg", Abschnitt „Tagesbilanz (Bestandsweg)", Spalte `Gebaeude_Modell` und die
Spalten, die nur der Altweg liest (Befund X, `DROP COLUMN` je Tabelle und Sichtneubau),
`Tab_DBTagV`/`Tab_DBTagVDaten`, Vergleich alt/neu samt `modellErzwungen`, Ausweis,
Kältebedarf-0-Hinweis, der AK-Sonderfall „feste Last", Referenzprojekt des Altwegs auf VDI 6007
umstellen, Rückweg-Test einstellen, Basis neu einfrieren. Das Umsetzungskonzept führt diese
**Löschliste der Stufe GA** in Kapitel 6; jede Stufe, die einen Altweg-Sonderfall einführt, trägt
ihn im selben Auftrag dort ein (ADR-006).

**Eigenständigkeit des VDI-Wegs.** Der VDI-Weg braucht nichts aus `Altweg/`: Der modellfreie
Vorbereitungsschritt liegt außerhalb beider Module (Fassade) und liefert nur, was ohne Modellauf
feststeht (Klimakalender, Verbrauch und Flächen des Projekts); Bewohnerzahl und Skalierung nach E8
entstehen je Modul aus dessen erstem Lauf. Der Klimakalender hat einen gemeinsamen Teil und einen
Altweg-Teil; das VDI-Modul bekommt nur den gemeinsamen. Die NULL-Vorgabe der Ost- und Westfenster
aus `Fensterflaeche_Ost_West` bildet der Vorbereitungsschritt, und die Stufe GA füllt die beiden
Spalten einmalig, bevor sie das Bestandsfeld entfernt. Der Kältebedarf ist Teil des VDI-Moduls;
Bericht und Kennzahlen kennen den Altweg nur über Weiche und Ausweis. Die **Modultrennungswache**
(`ModultrennungswacheTests`) prüft **beide** Richtungen und die Altweg-Datenquellen. Neu ist die
**Ausbauprobe**: statisch — außer Weiche, Rückweg-Test und Wache nennt keine Datei des Kerns
`Altweg/`; als Gate von GA — ein Bau mit umbenanntem Ordner `Altweg/` übersetzt nach Entfernen der
Weiche, und der Referenzlauf aller Projekte ohne Altweg-Gebäude bleibt byte-gleich.

**Wortwahl in allen Papieren.** „dauerhaft" (im Sinne von „nie entfernen") wird zu „bis zur
Ablösung (Stufe GA, Zeitpunkt offen)" oder „für die Dauer des Übergangs"; „eine Stufe GA gibt es
nicht" wird zu „Stufe GA, Zeitpunkt offen (Q24)"; das Risiko „zwei Rechenwege nebeneinander"
gilt bis GA. „Bestandsweg" bleibt der Name des Altwegs im Dialog und im Bericht. Fassade und
Vorbereitungsschritt bleiben über die Ablösung hinaus; Weiche, `IGebaeudeRechenweg` und Wache
leben bis GA.

**Referenzbasis.** Referenzprojekt auf dem Altweg und Rückweg-Test bleiben bis zur Stufe GA in
jeder Basis (Empfehlung zu A15: ein Referenzprojekt in der jeweils aktuellen Basis, kein zweiter
Basisordner); GA ist ein eigener Einfrierschritt.

**Prüfung vom 17.09.2026.** Mit derselben Nachricht hat der Anwender die Prüfung aller Papiere
auf Konsistenz und Umsetzbarkeit beauftragt. Ergebnis und Festlegungen stehen im Prüfprotokoll
[`Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md`](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md);
die Festlegungen sind im [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kapitel 8
gesammelt, Widerspruch bis zur Beauftragung von G1 möglich.

**Nachgezogen:** Kapitel 0, 4, 5, 6, 8, 10, 11, 12, 13 (Q24, Q25, Q26), 14, 15 und 16 dieses
Papiers (Rev. 3); Statusdatei; [`ADR-006`](ADR-006_Trennung_Altweg_VDI6007.md) und
[`ADR-002`](ADR-002_Stundenmodell_VDI6007_Einbindung.md); Umsetzungskonzept (Stufe GA, Löschliste,
Ausbauprobe), Softwarearchitektur, Systementwurf, Rechenschritte, Kühlkonzept, Anlagenkopplung,
Mehrzonenmodell, Befund X; Register (Q24, Q25 wieder offen, U17 neu); Indexzeilen; Word-Kurzfassung.

### N1.32 Entscheid E27 — Anwenderentscheide zu den vor G1 fälligen Punkten

**Entscheid E27 (Anwender, 22.09.2026).** Antworten auf die Punkte aus Kapitel 0 des
[Registers der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) und aus
Kapitel 5 des [Prüfprotokolls](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md),
als vollständige Liste; dazu die drei Konzeptfragen Q24, Q25 und Q26. Die Grundregel steht im
Entscheid selbst: Wo „Empfehlung" steht, gilt die Empfehlung des Registers im Wortlaut des
jeweiligen Registerabschnitts.

**Was damit gilt.** Entschieden sind 44 der bisher 66 offenen Registerpunkte und die Festlegung K2.
Bis auf **K10** folgen alle Antworten der Empfehlung des Registers; bei **D1** hatte der Anwender
keine Präferenz, es gilt der Vorschlag des Papiers. Ein Satz je Punkt, gruppiert wie im Entscheid:

**Rechenkern**

| Nr. | Entscheid | Wirkt vor |
|---|---|---|
| **U7** | Die Wochenendmaske kommt aus dem Ortszeit-Kalender des Referenzjahres (Option (a)); die Probe gegen `Tab_Klimadaten.WE` derselben Region bleibt | G1 |
| **U6** | Das Verfahren ist entschieden: In G1 werden beide Zeitbezüge der Sonnengeometrie (Stundenanfang, Stundenmitte) an der einen Stelle (Klimaklasse, A18) gemessen; die Endwahl folgt mit der Messung vor dem Einfrieren | Einfrieren G1 + G2 |
| **A18** | Der Klimaweg bleibt eine eigene Klasse, ausschließlich vom Eingangsbauer gerufen | G1 |
| **A15** | Option (a): ein Referenzprojekt mit `Gebaeude_Modell = TAGESBILANZ` bis GA in der jeweils aktuellen Basis; die GB-Arbeitskopie nur bis zum Merge G1 + G2; der Rückweg-Test umfasst nur dieses Projekt und endet mit GA | G1 + G2 |
| **A1** | Die Kaskade der Gebäudekinder (Löschen/Neuanlegen) bleibt; der Schreibweg wird vor G3 gemessen, die Rettung an der Löschstelle eingebaut | G3 |

**Umsetzung und Oberfläche**

| Nr. | Entscheid | Wirkt vor |
|---|---|---|
| **U8** | Die Normzahlen liegen lokal und gitignoriert; der Normfallnachweis läuft lokal, die Lücke im Gate steht im Protokoll | G0 |
| **U5** (= A9) | Die zwei Gebäudespalten-Schemaschritte werden zu einem (M3, ein Sichtneubau) verschmolzen | G1 |
| **U1** (= A4) | Der Katalogeditor bekommt ab G1 einen Schreibweg; „Speichern unter…" bleibt als nicht schließender Zweitknopf | G1 |
| **U3** (= A5) | Das `Zahlenfeld` bekommt einen `Platzhalter`, rein additiv | G1 |
| **A10** | Der Gebäudedialog zieht mit G1 nach `EPOS.UI.Daten` | G1 |
| **A12** | Der Produktausweis steht im Wortlaut von E10 im Berichtskopf und auf der Wiki-Seite | G1, G2 |
| **A11** | Schemaschrittnummern werden erst bei Beauftragung vergeben; bis dahin gelten die Papiernamen | erste Auslieferung |
| **A14** | Der Umschalter Klassenweg → Bauteilweg folgt der Datenlage; Rückfrage vor der ersten Zone, Herleitungszeile in beiden Stellungen | G3 |

**Kühlung**

| Nr. | Entscheid | Wirkt vor |
|---|---|---|
| **K2** | Der Kältekanal führt positive Kältemengen (Festlegung bestätigt) | KU1 |
| **K10** | **Abweichend von der Empfehlung:** Programmeinstellung für neue Projekte, Vorgabe aus (siehe unten) | KU1 |
| **K11** | Eigener Kühlsollwert mit Zeitprofil und eigene Kühlleistungsgrenze nach Empfehlung (a) des Registers: Sollwert und Grenze in KU1, das Zeitprofil nach deren Wortlaut in KU3 | KU1 |
| **K19** | KU2 bekommt einen eigenen, kleinen Einfrierschritt | KU2 |
| **K22** | Vor KU2 wird an den vorhandenen Kühlkennlinien geprüft, ob die COP-Spalte das Kälteverhältnis führt, und das Ergebnis im Glossar festgehalten | KU2 |
| **K24** | Der Kältestrom bleibt Skalar (Projektsumme in der Kennzahlendatei), bis der Bericht ihn je Anlage verlangt; dann eine Ergebnisspalte je Anlage im selben Schemaschritt wie `KU-S4`, nicht nachträglich (Empfehlung, K18a) | KU2 |

**Datenaustausch**

| Nr. | Entscheid | Wirkt vor |
|---|---|---|
| **D1** | Der gbXML-Import kommt vor dem IFC-Import (Vorschlag des Papiers; der Anwender hat keine Präferenz) | Beauftragung G4c/G4a |
| **D16** | Die gbXML-Zonenbildung erweitert E7 auf ein zweites Format | Beauftragung G4c |
| **D4** | Der Export führt Ergebnisgrößen in kWh mit ausdrücklicher Einheit | G7c |
| **D5** | Beide Exportformate tragen von Anfang an deterministische Kennungen | G7a, G7c |
| **D2** | Der gbXML-Export kommt erst zusammen mit der zweiten Stufe (synthetische Geometrie) | Beauftragung G7 |
| **D6** | Die semantische Stufe (G7c) wird zuerst gebaut; das Gegenüber des IFC-Exports (Werkzeug, Zweck) benennt der Anwender vor der Stufe, die über die semantische hinausgeht | G7e |
| **D11** | Die Rückgabe angereicherter fremder IFC-Dateien ist zulässig, mit Kennung in der Datei und Beipackzettel | G7d |
| **D17** | Option (b): Die gbXML-XSD liegt außerhalb des Repositoriums (`.gitignore`, Einrichtungshinweis, LIESMICH-Zeile mit Herkunft, Abrufdatum und Lizenzstand „keine"); der Validierungstest wird benannt übersprungen, wenn die Datei fehlt (Muster U8) | G4c |

**Mehrzonenmodell und IFC**

| Nr. | Entscheid | Wirkt vor |
|---|---|---|
| **U10** | Die Lizenzhinweisseite kommt mit der ersten IFC-Stufe, für alle ausgelieferten Fremdanteile | G4 |
| **U12** | Die Vorgaben je Baualtersklasse kommen aus dem eigenen EPOS-Gebäudekatalog | G4 |
| **A2** | Das IFC-Paket bleibt am Kern; die Naht `IGebaeudeLeser` wird von Anfang an gezogen | G4 |
| **A3** | Der Zuordnungsdialog trägt den formatfreien Namen | G4 |
| **A13** | Die Gebäudetabelle bekommt keine Herkunftsspalten | G4 |
| **A17** | Kein eigener Maskenschlüssel für Import und Export; Überlagerung im Gebäudedialog | G4 |
| **M9** | Die Synonymtabelle steht in der Auslieferung | G6a |
| **M14** | Auslieferungskatalog mit Wertekopie und Projektkopie der Baustoffe — beides bleibt | G6a |
| **A6** | Zonen, Bauteile und Luftströme werden als ein Aggregat in einer Transaktion geschrieben | G6b |
| **M2** | Der Zonenimport verwendet das Raumseitenmaß | G6c |
| **M10** | Die große Testdatei kommt ins Repositorium, nur zusammen mit dem LFS-Eintrag | G6c |
| **U17** | Ein Altweg-Gebäude ohne Tagesverteilung wird benannt abgelehnt, mit GA | GA |

**Stufenplan**

| Nr. | Entscheid | Wirkt vor |
|---|---|---|
| **Q24** | Option (a): GA wird beauftragbar und fällig, sobald alle vier Bedingungen erfüllt sind — (1) Referenz- und Bestandsprojekte auf VDI 6007 gerechnet und die Abweichung erklärt, (2) Feldphase von mindestens einer Heizperiode ohne offenen Fehler, (3) KU1 und, falls beauftragt, AK1 abgenommen, (4) Ausbauprobe grün; geprüft mit jeder Abnahme, der Stand steht in der Statusdatei | Beauftragung GA |
| **Q25** | Option (a): vollständige Ablösung nach der Löschliste (Umsetzungskonzept Kapitel 6) | Beauftragung GA |
| **Q26** | Empfehlung (a): AK1 nach G2; AK2 nach abgenommenem AK1 und Feldphase; AK3 danach, nach H6 (E24) weiter erst nach der Feldphase von AK1 und AK2 zugesagt; Aufwand AK0 1–2, AK1 10–15, AK2 11–15, AK3 23–38, zusammen 45–70 PT | Beauftragung G2 (AK1) |
| **H6** | Bereits mit E24 entschieden, zur Kenntnis; unverändert | AK3 |

**Die Abweichung: K10.** Das Register empfahl, den Kühlbetrieb bis zu einer ausdrücklichen
Projekteinstellung ausgeschaltet zu lassen (Vorgabe 0). Der Anwender entscheidet **abweichend**:
Er legt in den **Programmeinstellungen** fest, ob **neue** Projekte mit eingeschalteter Kühlung
angelegt werden; die Vorgabe dieser Einstellung ist **aus**. Bestehende Projekte —
Bestandsprojekte und die Referenzprojekte der Testdatenbank — bleiben aus, bis die
Projekteinstellung ausdrücklich eingeschaltet wird; der Schutz der Referenzprojekte bleibt damit
bestehen. Die Projekteinstellung selbst bleibt und ist je Projekt schaltbar. Die
Programmeinstellung wird über die vorhandene Schnittstelle `Dienste.Einstellungen` umgesetzt und
ist mit KU1 fällig.

**Was offen bleibt.**

- **Folgeaufgaben aus E27:** **U6** — die Endwahl des Zeitbezugs fällt nach der Messung in G1, vor
  dem Einfrieren von G1 + G2; **K22** — die Prüfung der COP-Spalte samt Glossareintrag vor KU2;
  **D6** — das Gegenüber des IFC-Exports ist vom Anwender zu benennen, fällig vor der Stufe, die
  über die semantische hinausgeht. Zu **M10** verlangt die Empfehlung im Registerwortlaut außerdem,
  die Lizenz der Fassung mit Raumgrenzen vor dem ersten Commit der Datei nachzufragen.
- **Register:** 22 offene Punkte, keiner davon mehr in dessen Kapitel 0 — U4 (G1), U9 (GB), U13–U15
  (G4), M3, M5–M8 und M11–M13 (G6b bis G6d), K4–K9, K12, K21 und K23 (KU1, KU2).
- Die Festlegungen F-Ü1 bis F-D1 der Prüfung vom 17.09.2026 (Register 8.4) bleiben bis zur
  Beauftragung von G1 widersprechbar.

**Arbeitsentscheid vom selben Tag (kein Konzeptentscheid):** Die Word-Kurzfassung ruht bis zur
Beauftragung von G1.

**Betroffene Stufen:** G0 (U8), G1 und G2 (U1, U3, U5, U6, U7, A10, A12, A15, A18), G3 (A1, A14),
G4 (U10, U12, A2, A3, A13, A17, D1, D16, D17), G6a–G6c (M2, M9, M10, M14, A6), G7 (D2, D4, D5, D6,
D11), KU1 und KU2 (K2, K10, K11, K19, K22, K24), AK1–AK3 (Q26), GA (Q24, Q25, U17).

**Nachgezogen:** Kopf, Kapitel 0 (Punkt 8), 4.1, 4.4, 6.1, 6.4, 10.4, 11 (Stufe GA), 13 (Q24,
Q25, Q26), 15 und 16 dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitte 1 bis 3; [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Vermerk je
Punkt, Kapitel 0 neu gefasst, Zählung 22). Die Fragentabellen von Umsetzungskonzept,
Softwarearchitektur, Kühlkonzept, Mehrzonenkonzept und Datenaustauschkonzept sowie die Indexzeile
in [`Dokumentation/LIESMICH.md`](../LIESMICH.md) tragen den Entscheid in einem eigenen Nachzug.

### N1.33 Entscheid E28 — U4 und U9 nach Empfehlung: vor dem Start kein Anwenderentscheid mehr offen

**Entscheid E28 (Anwender, 22.09.2026).** Der Anwender entscheidet die beiden Punkte **U4** und
**U9** des [Registers der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md)
nach dessen Empfehlung. Beide waren nach E27 (N1.32) die letzten offenen Punkte, die vor dem
Start fällig sind (U9 vor GB, U4 vor G1). Wie in E27 gilt die Empfehlung im Wortlaut des
jeweiligen Registerabschnitts.

**Was damit gilt.**

| Nr. | Entscheid | Wirkt vor |
|---|---|---|
| **U4** | Ja — der Abschnitt „13. Gebäudehülle und Gebäudemodell" in [`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md) entsteht, **bevor** die englischen Werte der Ressourcen geschrieben werden; ohne ihn entstünden zwei Übersetzungen desselben Begriffs | G1 (Ressourcen des Gebäudedialogs) |
| **U9** | Ja, in GB — die feste Grenze von 100 Gebäuden im Bestandsweg wird dort behoben, wo die Schleife ohnehin angefasst wird: das ungelesene Feld wird gelöscht, das andere auf die tatsächliche Zeilenzahl dimensioniert; ergebnisneutral, also ohne Einfrieranlass, und vor der Verschiebung nach `Altweg/` (E20) | GB |

**Was offen bleibt.** Vor dem Start (G0, GB, G1) ist **kein Anwenderentscheid mehr offen**. Es
bleiben die Folgeaufgaben aus E27 (U6 Endwahl vor dem Einfrieren von G1 + G2, K22 vor KU2, D6 vor
der Stufe über die semantische hinaus) und der Widerspruchsvorbehalt zu den Festlegungen F-Ü1 bis
F-D1 bis zur Beauftragung von G1 (Register 8.4). Das Register zählt **20 offene Punkte** — U13–U15
(G4), M3, M5–M8 und M11–M13 (G6b bis G6d), K4–K9, K12, K21 und K23 (KU1, KU2).

**Betroffene Stufen:** GB (U9), G1 (U4).

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitte 1 bis 3; [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Vermerk unter
U4 und U9, Kopf, Kapitel 0, 2 und 9, Zählung 20);
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Vorspann, 1.1,
2.9, 4 (Stufe GB) und 5; [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md)
(Verweise auf U4 und U9); die Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.34 Entscheid E29 — U6: Die Sonnengeometrie rechnet auf den Stundenanfang

**Entscheid E29 (Anwender, 23.09.2026).** Das Gebäudemodell rechnet den Sonnenstand jeder
Klimastunde auf den **Stundenanfang** der UTC-Stunde — dieselbe Konvention wie Photovoltaik und
Solarthermie und wie der Klimaimport, der die Zeitmarke des Stundenanfangs übergibt. Das ist die
Endwahl, die E27 (N1.32) zu **U6** als Folgeaufgabe offen gelassen hatte: Das Verfahren — in G1
beide Zeitbezüge an der einen Stelle messen, dann wählen — ist durchlaufen.

**Die Messung in G1** (Klimaweg `GebaeudeKlimaweg`, Testdatenbank, Klimaregionen der Projekte
1045, 1017 und 1023; Stundenmitte gegenüber Stundenanfang):

| Größe | Wirkung der Stundenmitte |
|---|---|
| Jahressumme der Strahlung auf die Ostfassade | −10,1 % |
| Jahressumme der Strahlung auf die Westfassade | +10,5 % |
| Jahressumme der Strahlung auf die Südfassade | +0,2 % |
| Jahresheizwärme der Gebäude | +0,04 bis +0,10 % |

**Begründung.** Für den Heizbedarf ist der Zeitbezug ohne Belang: Ost und West gleichen sich in
der Jahressumme nahezu aus, die Jahresheizwärme bewegt sich um höchstens ein Promille. Dagegen
wiegt, dass in einem Programm **eine** Sonne scheint: Gebäude, Photovoltaik und Solarthermie
rechnen denselben Sonnenstand zur selben Stunde, und ein Vergleich zwischen Gebäudegewinnen und
Anlagenertrag bekommt keinen Versatz von einer halben Stunde, der nur im Gebäude stünde.

**Was damit gilt.**

- Die Vorgabe `GebaeudeKlimaweg.ZEITBEZUG_VORGABE = Zeitbezug.Stundenanfang` ist **entschieden**,
  nicht mehr vorläufig. Der Schalter `Zeitbezug` bleibt im Kern stehen — für Messungen und Tests,
  nicht als Eingabe des Anwenders.
- Die Abweichung „Zeitbezug Stundenanfang statt Stundenmitte" der
  [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (Kapitel 11, Zeile 5)
  bleibt als **benannte Abweichung** von Blatt 3 stehen; sie ist jetzt entschieden.
- **Eine Umstellung auf die Stundenmitte gibt es nur für alle drei gemeinsam** — Gebäude,
  Photovoltaik und Solarthermie —, als eigener Auftrag mit eigenem Einfrierschritt. Das
  Gebäudemodell allein wird nicht umgestellt.
- Die Basis, die mit G1 + G2 neu eingefroren wird, rechnet mit dem Stundenanfang.

**Was offen bleibt.** Von den Folgeaufgaben aus E27 bleiben **K22** (vor KU2) und **D6** (vor der
Stufe über die semantische hinaus). Das Register zählt weiterhin **20 offene Punkte**; U6 war mit
E27 bereits entschieden, erledigt ist jetzt seine Folgeaufgabe.

**Betroffene Stufen:** G1 + G2 (Einfrieren der Basis).

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitte 1 und 2; [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (U6, Kopf,
Kapitel 0 und 9); [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
Vorspann, 1.2 und 5 (U6); [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
Schritt E1 und Kapitel 11 (Zeile 5); im Code die Vorgabe in `GebaeudeKlimaweg`.

### N1.35 Entscheid E30 — Die Gebäudekennzahlen gehen über eine Ergebnistabelle in den Bericht

**Entscheid E30 (Anwender, 23.09.2026).** Die Kennzahlen je Gebäude aus Kapitel 9 — Rechenweg,
Wärmebedarf, die drei Spitzenwerte, Kühlkennzahlen, Raumtemperatur und Überhitzungsstunden —
schreibt der **Simulationslauf in eine neue Ergebnistabelle**, und der Bericht **liest** sie. Der
Berichtsweg rechnet sie nicht neu.

**Anlass.** Mit G2 war der Berichtsteil aus Kapitel 9 offen geblieben: Der Bericht liest allein
die gespeicherten Ergebnistabellen, und die Gebäudekennzahlen lagen nur im Speicher des Laufs
(`GebaeudeErgebnistraeger`) und in den Skalaren `Geb[n].*` des Referenzlauf-Exports. Die zwei
Wege waren: die Kennzahlen im Berichtsweg ein zweites Mal rechnen oder sie beim Lauf ablegen. Ein
zweites Rechnen widerspräche der Hausregel „eine Auskunft ruft den Rechenweg des Laufs, sie
schreibt ihn nicht ab" und verlängerte jeden Bericht um einen Gebäudelauf je Stand.

**Was damit gilt.**

- **Schemaschritt 107** legt die STRICT-Tabelle `Tab_ErgebnisGebaeude` an (DDL an einer Stelle:
  `ErgebnisGebaeudeSchema`; zwei Indizes auf den Verweisen). Je Lauf und Gebäude eine Zeile:
  `ID_Ergebnis` (Kopf, Löschweitergabe), `ID_Gebaeude` (`Tab_Gebaeude.ID`, Löschweitergabe),
  `Merkplatz` (der Index `n` von `Geb[n]`), `Gebaeudename`, `Rechenweg` (`VDI6007` oder
  `TAGESBILANZ`, der wirksame Weg), `Heizwaerme_Mwh`, `Spitze_Kw`, `SpitzeTagesmittel_Kw`,
  `Spitze95_Kw` — diese Größen haben beide Wege — sowie `Kuehlenergie_Mwh`, `Kuehlstunden_H`,
  `MittlereRaumtemperatur_C`, `Ueberhitzungsstunden_H`, `Sommerlueftungsstunden_H`,
  `ObereRaumtemperatur_C`, die es nur auf dem VDI-Weg gibt: Auf dem Tagesbilanz-Weg stehen dort
  **NULL** („nicht gerechnet", nie 0). Die Einheit steht im Spaltennamen.
- **Der Lauf schreibt** die Zeilen dort, wo er alle Ergebnistabellen schreibt (`ErgebnisCtrl.Save`),
  und ein neuer Lauf **ersetzt** sie wie die übrigen. Gebildet werden die Zahlen in der
  Gebäudeschleife aus einer Kopie der Einzelreihe über `GebaeudeKennzahlen` — dieselbe Stelle, aus
  der die Auskunft des Gebäudedialogs (`GebaeudeBedarfCtrl`) ihre Spitzenwerte bildet; Dialog und
  Bericht nennen dieselbe Zahl. Die Werte gehen ungerundet in die Tabelle.
- **Der Bericht** trägt in der Projektbeschreibung den Abschnitt **„Gebäude (Simulationsergebnis
  Stamm)"**: je Gebäude Rechenweg, Wärmebedarf Heizung, die drei Spitzenwerte und auf dem VDI-Weg
  Kühlenergie (informativ), Stunden mit Kühlbedarf, mittlere Raumtemperatur der Nutzungszeit und
  Überhitzungsstunden. Ein Gebäude des Tagesbilanz-Wegs trägt „Tagesbilanz (Bestandsweg)" und
  keine Kühlzeilen (E20, E21, E23). **Ohne Gebäudezeile entfällt der Abschnitt** — kein Gebäude,
  kein Ergebnis oder eine Datenbank vor Schritt 107.
- **Der Produktausweis nach E10 steht im Berichtskopf** (A12, E27), sobald ein Stand des Berichts
  ein Gebäude auf dem VDI-Weg gerechnet hat — als Zeile „Gebäudemodell" des Deckblatts, im
  Wortlaut aus **einem** Ressourcenschlüssel (`GEB_PRODUKTAUSWEIS_VDI6007`, beide Sprachen).
- **Der Excel-Bericht** bleibt ohne Gebäudeabschnitt: Er hat kein Gegenstück zur
  Projektbeschreibung, in das die Zeilen gehörten.
- **Der Referenzlauf** exportiert die Tabelle nicht; die Kennzahlen stehen dort schon als Skalare
  `Geb[n].*` (Umsetzungskonzept 1.8). Die Basis `2026-09-23_R12_Gebaeudemodell` bleibt
  **byte-gleich** (alle dreizehn Projekte, außer `protokoll.txt`).

**Was offen bleibt.** Die fünf Gebäudekennzahlen des `KennzahlenKatalog` mit der Gruppe
`GR_GEBAEUDE` (Softwarearchitektur 4.3) und der Rechenweg im Variantenvergleich
(`AbweichungsErmittler`) folgen als eigener Auftrag; sie können jetzt aus der Tabelle lesen.

**Betroffene Stufen:** G2 (Bericht).

**Nachgezogen:** Kopf dieses Papiers; Kapitel 9; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitt 1 (E30) und Abschnitt 2 (G2); im Code Schemaschritt 107 (`SchemaMigration`,
`Werkzeuge/Testdatenbankschema`, Testdatenbank auf Stand 107), `ErgebnisCtrl`,
`SimulationWaermebedarf`, `ProjektbeschreibungBaustein`, `DeckblattBaustein`; Tests
`ErgebnisGebaeudeTests`.

### N1.36 Entscheid E31 — K4, K5, K6, K7 und K12 der Kühlung nach Empfehlung

**Entscheid E31 (Anwender, 23.09.2026).** Der Anwender entscheidet die fünf Punkte **K4**, **K5**,
**K6**, **K7** und **K12** des [Registers der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md)
nach dessen Empfehlung. Alle fünf waren vor **KU1** fällig; mit ihnen ist Stufe KU1 des
[Kühlkonzepts](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) ohne offenen Anwenderentscheid
beauftragbar. Wie in E27 und E28 gilt die Empfehlung im Wortlaut des jeweiligen
Registerabschnitts.

**Was damit gilt.**

| Nr. | Entscheid | Wirkt vor |
|---|---|---|
| **K4** | Die Kühlung steht **zuletzt** in der Knappheitsreihenfolge, und ihr Rang wird in der Oberfläche **nicht** zur Bearbeitung angeboten: Die Reihenfolge regelt die knappe **Wärme**erzeugung, an der die Kälteseite unbeteiligt ist — das vierte Glied macht die Folge vollständig und steuert nichts (Kühlkonzept 4.5) | KU1 (Kanal) |
| **K5** | Die Feuchte bleibt ausgeschlossen — gerechnet wird **sensible Kälte ohne Entfeuchtung** —, und die Grenze steht an **jeder** Kältezahl: Dialog, Bericht, Wiki und Export (Kühlkonzept 1.3, 3.6, 9.2) | KU1 (Ressourcen, Dialog, Export) |
| **K6** | Zonen, die in derselben Stunde heizen und kühlen, werden **nicht saldiert**: Heiz- und Kühlkanal tragen je ihren Betrag, und die Kennzahl „Stunden mit gleichzeitigem Heizen und Kühlen" weist den Fall aus (Kühlkonzept 3.5) | KU1 (Regel im Kanal); wirksam mit den Zonen (G6) |
| **K7** | Der **Kältespeicher** wird nach **KU3** vertagt, gemeinsam mit der Kältemaschine; bis dahin gibt es **keinen Persistenzwert ohne Rechenweg** — keinen Verwendungswert `VERWENDUNG_KAELTE`, keine Eingabe eines Kältespeichers (Kühlkonzept 4.6, 7.6) | KU1 (Schemaumfang); gebaut frühestens in KU3 |
| **K12** | Die Kühlung gilt **auch auf iOS**: Der Kern ist plattformfrei, es entsteht kein neuer Maskenschlüssel; einen eigenen iOS-Lauf gibt es für KU1 und KU2 **nicht**, der Nachweis ist der Kern-Lauf (Kühlkonzept 8.6, 10.6) | KU1 |

**Was das für den Schemaumfang von KU1 heißt (K7).** Die Ergebnisspalte
`Tab_ErgebnisPufferspeicher.Entladung_Kuehlung` gehört weiter zu `KU-S4` (Kühlkonzept 7.4): Sie ist
keine Eingabe, die einen Rechenweg verspricht, sondern die vierte Spalte des gleichförmigen
Kanalschreibwegs, und sie bleibt leer, bis ein Kältespeicher rechnet. Ausgeschlossen sind der
Verwendungswert und jede Eingabe eines Kältespeichers.

**Was offen bleibt.** Das Register zählt **15 offene Punkte** — U13–U15 (G4), M3, M5–M8 und
M11–M13 (G6b bis G6d), K8, K9, K21 und K23 (KU2; die freie Kühlung aus K8 mit KU3). Von den
Folgeaufgaben aus E27 bleiben **K22** (vor KU2) und **D6** (vor der Stufe über die semantische
hinaus).

**Betroffene Stufen:** KU1 (K4, K5, K6, K7, K12), KU3 (K7).

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitte 1 und 3; [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Vermerk unter
K4, K5, K6, K7 und K12, Kopf, Kapitel 0, 6 und 9, Zählung 15);
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) Kopf, 3.5, 4.5, 4.6, 8.6, 10.6,
11.1 und 12; die Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.37 Entscheid E32 — Gebäude ohne wirksame Kühlung laufen frei

**Entscheid E32 (Anwender, 23.09.2026).** Ein Gebäude **ohne wirksame Kühlung** — der
Projektschalter „Kühlung rechnen" (`Tab_Einstellungen.Kuehlbetrieb`) ist aus, der Haken
`Kuehlung_Aktiv` fehlt oder es gibt keinen Kühlsollwert — wird im Löser **nicht mehr** an
`Maximaleraumtemperatur` gekappt. Bis dahin hielt der Löser jedes solche Gebäude mit einer idealen
Kühlung ohne Leistungsgrenze auf θ_max, und die dabei abgeführte Energie erschien als
„informativer" Kühlbedarf (Kapitel 4.5 alter Fassung). Das Gebäude **läuft jetzt frei**: Es wird
keine Wärme abgeführt, die Raumtemperatur darf über θ_max steigen, und die
**Überhitzungsstunden** zählen die Stunden darüber im freien Lauf — sie messen, was **ohne**
Anlage geschieht ([Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 6.4, 7.1). Mit
wirksamer Kühlung bleibt alles, wie es mit KU1 gebaut ist: Regelung auf den Kühlsollwert,
Kühlleistungsgrenze, Kühlreihe im Kühlkanal. Beauftragt mit der vierten Welle von KU1.

**Was damit gilt.**

| Gegenstand | Regel |
|---|---|
| **Löser** | Ohne wirksame Kühlung ist die obere Regelgrenze +∞ (`GebaeudeModellEingang.KuehlSollwert`, `Stundenrand` „keine Kühlung"); die Betriebsfälle „Kühlen geregelt" und „Kühlgrenze" treten nicht ein, das Totband ist nach oben offen ([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 7.1). Kühlt der Löser trotzdem in einer Stunde, ist das ein benannter Fehler (`ErgebnisUnplausibel`), keine Zahl |
| **Raumtemperatur** | darf über `Maximaleraumtemperatur` steigen; die Sommerlüftung eines ungekühlten Gebäudes schaltet weiter ab 23 °C |
| **Überhitzungsstunden** | Stunden der Nutzungszeit mit θ_op über `Maximaleraumtemperatur` — ohne wirksame Kühlung im freien Lauf; mit wirksamer Kühlung dieselbe Grenze, nicht der Kühlsollwert |
| **Kühlreihe und Kühlkennzahlen** | gibt es nur bei wirksamer Kühlung: ohne sie sind `KuehlbedarfKwh`, `KuehlenergieMwh` und `StundenMitKuehlbedarf` `null` („nicht verfügbar", keine 0); der Bedarfsdialog zeigt „—" (K18) und die Überhitzungsstunden, `Tab_ErgebnisGebaeude.Kuehlenergie_Mwh` und `Kuehlstunden_H` bleiben NULL, der Bericht zeigt „—", und der Referenzlauf schreibt weder `kuehlbedarf_<n>.csv` noch `Geb[n].KuehlenergieMwh` und `Geb[n].StundenMitKuehlbedarf` |
| **Normfälle** | unberührt: Sie setzen ihre Ränder selbst (`NormfallLeser`, AixLib-Modelle) und gehen nicht über den Eingangsbauer — lokal gemessen wie in G0, elf der zwölf im Band |
| **Tagesbilanz-Weg** | unberührt (1040 byte-gleich) |

**Was es ablöst.** Die Kappung als „informativer" Kühlbedarf (4.5 und 4.6 alter Fassung;
Kühlkonzept 3.1, 3.2 und die Skizze in 8.1; Rechenschritte 7.1 „Vor KU1 …"), die Dialogsätze „Ohne
Haken bleibt die Überhitzung informativ" und „Der Kühlbedarf ist informativ — die Wärme, die
abgeführt werden müsste …" in beiden Sprachen, und die Lesart „ab KU1 über `Kuehl_Sollwert`" der
Überhitzungskennzahl (Rechenschritte 8.2, Umsetzungskonzept 1.4, Register F-S4): Die Grenze ist
`Maximaleraumtemperatur`, wie im Kühlkonzept 7.1 festgelegt und in KU1 gebaut.

**Wirkung — gemessen vor dem Einfrieren** (Referenzlauf aller dreizehn Projekte gegen die Basis
`2026-09-23_R12_Gebaeudemodell`, Testdatenbank unverändert; Jahresheizwärme je Gebäude
`Geb[n].JahresheizwaermeMwh`, Überhitzungsstunden `Geb[n].Ueberhitzungsstunden`):

| Gebäude (Projekte) | Heizwärme R12 → E32 [MWh/a] | relativ | Überhitzungsstunden R12 → E32 [h] | höchste Raumluft E32 [°C] |
|---|---:|---:|---:|---:|
| 10614, 10577, 10652 (1007, 1008, 1046) | 69,07 → 68,97; 15,03 → 15,01 | −0,145 % | 469 → 600 | 33,2 |
| 10576 (1008) | 89,17 → 89,13 | −0,044 % | 295 → 440 | 30,4 |
| 10599 (1017) | 90,19 → 90,15 | −0,045 % | 305 → 455 | 30,3 |
| 10632 (1018) | 68,27 → 68,25 | −0,032 % | 154 → 233 | 29,1 |
| 10628, 10629, 10644 (1023, 1024, 1039) | 450,56 → 449,90 | −0,145 % | 628 → 840 | 33,4 |
| 10642 (1039) | 48,02 → 48,02 | −0,009 % | 72 → 102 | 27,5 |
| 10643 (1039) | 99,14 → 99,10 | −0,041 % | 264 → 308 | 32,2 |
| 10646, 10647, 10651 (1041, 1042, 1045) | 75,98 → 75,94 | −0,050 % | 277 → 331 | 32,0 |

Die **Heizwärme sinkt um höchstens 0,15 %** — die Wärme, die die Kappung im Sommer abführte,
bleibt jetzt in den Speichermassen und senkt den Heizbedarf der Übergangszeit ein wenig; die
Schwelle des Auftrags (3 %) ist weit unterschritten. Die Wärmelast (Winterspitze) bleibt gleich.
Die **Überhitzungsstunden steigen um 17 bis 51 %**: θ_op liegt im freien Lauf in mehr Stunden
über θ_max als unter der Kappung, die nur die Raumluft hielt. Die Raumluft erreicht bis
33,4 °C — die Referenzgebäude führen keine Sommerlüftung. Projekte ohne VDI-Gebäude (1030) und auf
dem Tagesbilanz-Weg (1040) bleiben byte-gleich. Die Basis wird im selben Auftrag neu eingefroren
(`Referenzlaeufe/LIESMICH.md`).

**Betroffene Stufen:** KU1 (Welle 4, Einfrierschritt mit dem Referenzprojekt mit Kühlung), KU2
(die Kältedeckung setzt auf demselben Rechenweg auf).

**Nachgezogen:** Kopf dieses Papiers, 4.5 und 4.6; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitt 1 (E32); [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) Kopf, 3.1,
3.2, 3.4, 4.7, 6.4, 7.1, 8.1 und 8.4; [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
Kopf, 7.1, 8.1, 8.2 und 9; [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
1.4; [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) F-S4; die Wiki-Quellen „Kühlung"
und „Gebäudemodell VDI 6007"; die Dialogsätze in beiden Sprachen.

### N1.38 Entscheid E33 — K8, K9, K21 und K23 der Kühlung; K9 abweichend von der Empfehlung

**Entscheid E33 (Anwender, 23.09.2026).** Der Anwender entscheidet die vier vor **KU2** fälligen
Punkte **K8**, **K9**, **K21** und **K23** des
[Registers der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md): **K8, K21
und K23 nach Empfehlung, K9 abweichend davon.** Mit ihnen ist Stufe KU2 des
[Kühlkonzepts](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) ohne offenen Anwenderentscheid
beauftragbar; beauftragt ist ihre erste Welle (Schemaschritt `KU-S3`, ergebnisneutral). Wo
„nach Empfehlung" steht, gilt die Empfehlung im Wortlaut des jeweiligen Registerabschnitts.

**Was damit gilt.**

| Nr. | Entscheid | Wirkt vor |
|---|---|---|
| **K8** | Nach Empfehlung: **bauen, aber keine als eigener Erzeuger.** Die **Rückkühlung** ist Bestandteil der Kältemaschine — bei der reversiblen Wärmepumpe steckt sie in der Maschine und ihrer Kennlinie, die über der Außen- bzw. Quellentemperatur aufgetragen ist (Kühlkonzept 5.1, Festlegung 5), bei der Kältemaschine kommt sie mit dieser in KU3; die **freie Kühlung** ist ein Betriebsfall der vorhandenen Maschine (KU3); die **Nachtlüftung** ist eine Gebäudemaßnahme und steht mit der Sommerlüftung im freien Lauf (G2). Die Erzeugerauswahl bekommt keinen Eintrag „freie Kühlung" oder „Rückkühlung" (Kühlkonzept 5.4) | KU2 (reversible Wärmepumpe ohne eigenes Rückkühlmodell), KU3 |
| **K9** | **Abweichend von der Empfehlung:** Der Kältestrom läuft **per Vorgabe** über denselben Stromträger und denselben Tarif wie die Wärmepumpe im Heizbetrieb; **wahlweise** kann **je Anlage** ein anderer Stromträger des Projekts gewählt werden; ein leeres Feld (NULL) heißt „wie Heizbetrieb". Die Beziehung läuft über die Kennung des Trägers (Hausregel: neue Beziehungen über IDs). Die Empfehlung lautete: immer derselbe Stromträger und Tarif — ein eigener Kältetarif sei eine zweite Wahrheit für dieselbe Steckdose (Kühlkonzept 6.3) | KU2 (Schema, Dialog, Kältestrom, Kosten, Emissionen) |
| **K21** | Nach Empfehlung: Der **Kühl-Vorlauf** wird aus den **Stützstellen** der Kühlkennlinie **ausgewählt**; keine Interpolation über den Vorlauf; ein leeres Feld heißt **kleinster Stützwert**; eine Extrapolation wird gewarnt wie auf der Heizseite; eine Stützstellenprobe je Vorlauf sichert es ab (Kühlkonzept 5.1, Festlegung 2; 8.2; 10.2) | KU2 (Kennlinienleser, Erzeugerdialog) |
| **K23** | Nach Empfehlung: Der **Hilfsstromanteil** des Kältekreises steht **je Anlage** (`Kuehl_Hilfsstromanteil` an der Wärmepumpe); NULL heißt **kein Zuschlag** (Kühlkonzept 6.1, 7.3) | KU2 (Schema, Kältestrom) |

**K9 — wo die Auswahl sitzt, geprüft am Code.** Die Wärmepumpe bekommt ihren Stromträger heute
an drei Stellen:

1. **An der Anlagenzeile** (`Tab_Energieanlagen.ID_Carrier`, ET-5 vom 08.09.2026): Der
   Wärmepumpendialog bietet in der Gruppe „Energieträger" die Träger des Katalogs an, Vorgabe ist
   der Stromträger des Projekts; die Wahl wird je Anlage gespeichert und dem Projekt zugeordnet
   (`energy_project_settings`). Der Gerätekatalog (`Tab_WP_STAMM`) kennt keine Träger — sie
   gehören zum Projekt.
2. **Der Stromträger des Projekts** (`Emissionsquelle.StromTraeger`,
   `ProjektEnergietraegerCtrl.StromTraegerDerAnlagen`): der an einer Anlage gewählte, dem Projekt
   zugeordnete Stromträger, die Wärmepumpe zuerst; sonst die Zuordnung des Projekts, sonst der
   Auslieferungsträger. Mit ihm bepreist `KostenEmissionRechner` den **Netzbezug einmal**
   (Arbeitspreis, Staffel, Aufschläge) und bewertet die Emissionen des Netzstroms.
3. **Anlagenscharf** bewertet allein der `EndenergieAufloeser` (Anwenderentscheid 19.09.2026):
   Eine Anlage mit eigenem Stromträger bemisst ihre Betriebskosten (Wege A und B) mit dessen
   Arbeitspreis (`ProjektEnergietraegerCtrl.EigeneStromTraeger`).

**Die Auswahl gehört damit an die Anlagenzeile, neben den Stromträger des Heizbetriebs:**
`Tab_Energieanlagen.Kuehl_ID_Carrier`, ein ganzzahliger Verweis auf `energy_carrier.id`, NULL = wie
Heizbetrieb. „Wie Heizbetrieb" heißt: der Stromträger, mit dem die Anlage im Heizbetrieb rechnet —
ihr `ID_Carrier`, sonst der des Projekts. Am Gerät (`Tab_WP`) steht die Wahl nicht, weil dort auch
der Heizträger nicht steht, und eine Katalogspalte gibt es nicht, weil ein Katalogsatz keine Träger
eines Projekts kennt. Das weicht nicht von „je Anlage" ab: Stromträger werden im Bestand bereits
je Anlage gewählt.

**Was daraus für den Rechenweg folgt (KU2, ab Welle 2).** Bepreist wird der Netzbezug heute
**einmal**, mit dem Stromträger des Projekts; einen Tarif je Verbraucher gibt es nicht (ET-5).
Solange die Kühlwahl leer ist, bleibt es dabei — der Kältestrom geht in dieselbe Stufenrechnung und
denselben Netzbezug (Kühlkonzept 6.1). Trägt eine Anlage einen **anderen** Kühlträger, braucht der
Kältestrom eine eigene Bepreisung und eine eigene Emissionszuordnung für seinen Anteil am
Netzbezug. Wie dieser Anteil gebildet wird — Eigenverbrauch aus Photovoltaik und Stromspeicher
stehen in der Stufenrechnung vor dem Netzbezug —, legt die Welle fest, die den Kältestrom in Kosten
und Emissionen bringt; die Regel wird mit ihr vorgelegt.

**Was offen bleibt.** Das Register zählt **11 offene Punkte** — U13–U15 (G4), M3, M5–M8 und
M11–M13 (G6b bis G6d); im Kühlkonzept ist kein Punkt mehr offen. Von den Folgeaufgaben aus E27
bleiben **K22** (die Prüfung der COP-Spalte, vor KU2) und **D6** (vor der Stufe über die
semantische hinaus).

**Betroffene Stufen:** KU2 (K8 für die reversible Wärmepumpe, K9, K21, K23), KU3 (K8: Kältemaschine
mit Rückkühlung, freie Kühlung); die Nachtlüftung aus K8 steht mit G2.

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitte 1 und 3; [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Vermerk unter
K8, K9, K21 und K23, Kopf, Kapitel 0, 6 und 9, Zählung 11);
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) Kopf, 5.0.5, 5.1, 5.4, 6.1, 6.2,
6.3, 7.3, 8.2, 11.1 und 12; die Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.39 Entscheid E34 — Kältestrom eines abweichenden Kühlträgers: anteilig am Netzbezug oder eigener Zähler

**Entscheid E34 (Anwender, 23.09.2026).** Ergänzung zu **K9** und **E33** (N1.38): Wählt eine
Wärmepumpe für den Kühlbetrieb einen **anderen Stromträger als das Projekt**
(`Tab_Energieanlagen.Kuehl_ID_Carrier`), ist **je Anlage wählbar**, wie ihr Kältestrom in Kosten und
Emissionen eingeht:

1. **Anteilig am Netzbezug — Vorgabe.** Es gibt **einen** Netzanschluss. Der Netzbezug jedes
   Zeitschritts der Stufenrechnung wird nach dem **Anteil des Kältestroms am Stromverbrauch** dieses
   Zeitschritts aufgeteilt; der Anteil des Kältestroms trägt Arbeitspreis und CO₂-Faktor des
   **Kühlträgers**, der Rest die des Projektträgers. Der **Eigenverbrauch aus Photovoltaik** (und
   Stromspeicher) bleibt **gemeinsam** — er deckt Kältestrom und übrigen Strom in der Reihenfolge der
   Stufenrechnung, ohne Vorrang —, und der **Leistungspreis** bleibt beim Stromträger des Projekts.
2. **Eigener Zähler.** Der Kältestrom wird **vollständig** mit dem Kühlträger bepreist und bewertet
   und **nicht** aus PV-Eigenstrom oder Stromspeicher gedeckt: Er läuft neben der Stufenrechnung,
   nicht durch sie.

Beauftragt als Dokumentation mit der zweiten Welle von KU2; **umgesetzt (Schema und Rechnung) wird
er in der dritten Welle**, zusammen mit Wirtschaftlichkeit und Emissionen der Kälteseite.

**Was damit gilt.**

| Fall | Regel |
|---|---|
| `Kuehl_ID_Carrier` leer (NULL) oder gleich dem Stromträger des Projekts | keine Wahl nötig: Der Kältestrom läuft wie der Wärmepumpenstrom durch die Stufenrechnung und trägt Tarif und Faktor des Projekts (K9, Vorgabe aus E33) |
| anderer Kühlträger, Wahl (1) — **Vorgabe** | Netzbezug(t) = Netzbezug der Stufenrechnung im Zeitschritt t; Anteil(t) = Kältestrom(t) / Stromverbrauch(t); der Kühlträger trägt Netzbezug(t) · Anteil(t) — Arbeitspreis und CO₂-Faktor aus `KostenEmissionRechner.ArbeitspreisJeKwh` bzw. `Emissionsquelle.Fuer` mit seiner Kennung; PV-Eigenverbrauch gemeinsam, Leistungspreis beim Projektträger. Mehrere Anlagen mit eigenem Kühlträger teilen den Netzbezug nach ihren Anteilen |
| anderer Kühlträger, Wahl (2) | der ganze Kältestrom dieser Anlage mit dem Kühlträger bepreist und bewertet; er geht nicht in die Stufenrechnung aus Eigenverbrauch, Speicher und Netzbezug ein |

**Wo die Wahl steht.** Je Anlage an der Anlagenzeile neben `Kuehl_ID_Carrier` — dort, wo die
Wärmepumpe ihre beiden Stromträger wählt (K9, E33); Name, Typangabe und Schemaschritt vergibt die
dritte Welle bei ihrer Beauftragung (Umsetzungskonzept 1.6, ADR-001). Ohne abweichenden Kühlträger
ist die Wahl wirkungslos und wird im Dialog nicht angeboten.

**Was bis zur dritten Welle gilt (Übergang, benannt).** Die zweite Welle bringt den Kältestrom als
eigene Stundenreihe in die Stufenrechnung — an derselben Stelle wie den Wärmepumpenstrom
([Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 6.1) — und bepreist den Netzbezug
weiter **einmal**, mit dem Stromträger des Projekts. Ein gesetzter Kühlträger wirkt bis zur dritten
Welle nicht; der Lauf sagt es als Hinweis, statt still anders zu rechnen.

**Was offen bleibt.** Das Register zählt weiter **11 offene Punkte**; E34 schließt keine Frage und
öffnet keine. Die Regel, die N1.38 der Welle mit Kältestrom in Kosten und Emissionen aufgetragen
hat, ist damit festgelegt.

**Betroffene Stufen:** KU2 (Welle 3: Schema der Wahl, Aufteilung des Netzbezugs, Wirtschaftlichkeit,
Emissionen, Erzeugerdialog; Welle 2: nur der benannte Übergang).

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitt 1 (E34) und 3; [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Vermerk
unter K9, Kopf); [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) Kopf, 6.1, 6.3 und
12.1; die Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.40 Entscheid E35 — Ein eigener Zähler des Kältestroms trägt Grund- und Leistungspreis seines Stromträgers

**Entscheid E35 (Anwender, 24.09.2026).** Ergänzung zu **E34** (N1.39): Wählt eine Wärmepumpe mit
abweichendem Kühlträger die Abrechnungsart **„eigener Zähler"** (`Tab_Energieanlagen.Kuehl_EigenerZaehler
= 1`), trägt ihr Kältestrom **zusätzlich Grund- und Leistungspreis seines Stromträgers**:

1. **Grundpreis je Zähler.** Der Grundpreis des Kühlträgers fällt je Jahr einmal je Zähler an.
2. **Leistungspreis auf die eigene Spitze.** Führt der Kühlträger einen Leistungspreis, bemisst er sich
   an der **eigenen Spitze des Kältestroms dieser Anlage** — nach der Leistungspreisregel des Trägers,
   wie beim Netzbezug des Projekts: die zweistufige Staffel (`Leistungspreis_Staffel*`) vor der
   Saisonreihe, danach der Satz je Monat oder je Jahr.

Bei **„anteilig am Netzbezug"** bleibt alles wie E34: Grund- und Leistungspreis beim Stromträger des
Projekts. Beauftragt und umgesetzt mit der vierten Welle von KU2.

**Was damit gilt.**

| Fall | Regel |
|---|---|
| `Kuehl_ID_Carrier` leer (NULL) oder gleich dem Stromträger des Projekts | unverändert (E33, E34): Tarif und Faktor des Projekts; Grund- und Leistungspreis des Projektträgers, der Leistungspreis auf die Spitze des Anschlusses |
| anderer Kühlträger, anteilig am Netzbezug — **Vorgabe** | unverändert (E34): Arbeitspreis und CO₂-Faktor des Kühlträgers für seinen Anteil am Netzbezug; Grund- und Leistungspreis beim Projektträger |
| anderer Kühlträger, eigener Zähler | Arbeitspreis und CO₂-Faktor des Kühlträgers für den ganzen Kältestrom (E34), **dazu** je Zähler der Grundpreis des Kühlträgers und — wenn gepflegt — sein Leistungspreis auf die eigene Spitze des Kältestroms der Anlage |

**Ein Zähler je Anlage — geprüft und benannt.** Die Abrechnungsart steht je Anlage (E34), und das
Datenmodell kennt keinen Zähler, den mehrere Anlagen teilen. Zwei Anlagen mit demselben Kühlträger und
eigenem Zähler sind deshalb **zwei Zähler**: zwei Grundpreise, zwei eigene Spitzen. Ein gemeinsamer
Zähler mehrerer Anlagen ist nicht abbildbar; er käme mit einer eigenen Zählerzuordnung an der
Anlagenzeile. Der Grundpreis gehört zum Zähler, nicht zur Menge — er steht auch in einem Jahr ohne
Kältestrom; ein Leistungspreis ohne Kältestrom ist 0.

**Die eigene Spitze.** Der Kältestrom geht je Stunde mit derselben Leistung in jede der vier
Viertelstunden, wie in der Stufenrechnung; die Viertelstundenspitze der Anlage ist damit ihre
Stundenspitze, Jahres- und Monatsspitzen nach derselben Monatseinteilung wie die Bezugsspitze des
Anschlusses (`ZeitreihenSatz.Kaeltestromspitzen`). Wie die Bezugsspitze gibt es sie nur aus einem
frischen Lauf: Ob ein Lauf die Zeitreihen braucht, fragt `KostenEmissionRechner.StromLeistungspreisGepflegt`
jetzt auch für den Kühlträger eines eigenen Zählers; fehlen sie doch, steht der Grundpreis, und der
Leistungspreis wird mit dem Namen des Trägers als fehlend benannt, nicht still übergangen.

**Szenarien (E9a).** Im Szenariolauf gelten die wirksamen Szenariopreise des Kühlträgers — Grund- und
Leistungspreis wie der Arbeitspreis; ein Szenario-Leistungspreis neben Staffel oder Saisonreihe bleibt
ohne Wirkung und wird benannt. Ein Mengenszenario skaliert Kältestrom und eigene Spitze; der Grundpreis
bleibt als Festbetrag stehen.

**Ausweis.** In den Energiekosten je Anlage stehen neben „Kältestrom … (eigener Zähler)" je Zähler die
Zeilen „Grundpreis Kältestromzähler …" (1 a × Grundpreis) und „Leistungspreis Kältestromzähler …" (Spitze
× Satz); die Kosten des Kältestroms in Kennzahlen und Bericht enthalten beide, der Leistungsanteil der
Energiekosten den Leistungspreis. Ein Rollentarif lässt beide stehen — sie gehören nicht zum Stromanteil
des Anschlusses.

**Was offen bleibt.** Das Register zählt weiter **11 offene Punkte**; E35 beantwortet die Frage, die das
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 6.2 an den Anwender gestellt hatte, und
öffnet keine.

**Betroffene Stufen:** KU2 (Welle 4: Kosten des eigenen Zählers, eigene Spitze, Ausweis).

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Abschnitt 1
(E35) und 2 (KU2); [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Vermerk unter K9,
Kopf); [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 6.1, 6.2, 6.3 und 11.1; die
Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.41 Entscheid E36 — Die Rechenzeit der Anlagenkopplung: höchstens 100 ms je gekoppeltem Gebäude und Jahr

**Entscheid E36 (Anwender, 24.09.2026).** Die Anforderung **N-A4** der
[Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) (2.2) verlangte für AK1
höchstens 20 % mehr Rechenzeit als der Bestand. Die zweite Welle von AK1 hat gemessen, dass das mit
dem Fallsystem je Abschnitt (Schritt H: Übergabe begrenzt, gesättigt oder im Regelbereich, bis zu
zwei Umschaltzeitpunkte je Stunde) nicht zu halten ist: je gekoppeltem Gebäude und Jahr **21 bis
31 ms** statt **7 ms** ohne Kopplung. Der Anwender entscheidet:

1. **N-A4 gilt nur bei wirksamer Kopplung** — Projektstufe AK1 oder höher, Haken „Übergabe rechnen"
   und eine Übergabeart ungleich ideal — und lautet dann: **höchstens 100 ms je Gebäude und Jahr**.
2. **Gebäude ohne wirksame Kopplung bleiben unverändert** — sie rechnen auf dem Bestandszweig
   (Grenzfall A der Anlagenkopplung, 3.7) byte-gleich und so schnell wie bisher (rund 5 ms je Zone und
   Jahr, 4.8 und 5.13).

**Was damit gilt.**

| Fall | Grenze | Gemessen |
|---|---|---|
| Gebäude ohne wirksame Kopplung | unverändert: der Bestand (4.8, 5.13) | 7 ms je Gebäude und Jahr |
| Gebäude mit wirksamer Kopplung (AK1) | höchstens **100 ms** je Gebäude und Jahr | 21 bis 31 ms |
| AK3 (Iteration je Stunde) | wird vor der Abnahme an einem Mehrzonengebäude gemessen und fortgeschrieben (Anlagenkopplung 6.3) | — |

**Die Probe berichtet, sie richtet nicht.** Die Läufer der CI sind verschieden schnell; eine harte
Schwelle von 100 ms wäre eine Aussage über den Läufer, nicht über den Rechenweg. Die Messprobe des
gekoppelten Jahreslaufs (`AnlagenkopplungEingangTests`) gibt die Zeit aus und scheitert erst beim
**Fünffachen** der Grenze (500 ms) — dann ist am Rechenweg etwas grundsätzlich falsch, nicht der
Läufer langsam.

**Was offen bleibt.** Das Register zählt weiter **11 offene Punkte**; E36 beantwortet die Frage, die
die zweite Welle von AK1 zu N-A4 an den Anwender gestellt hatte, und öffnet keine.

**Betroffene Stufen:** AK1 (Welle 3: Anforderung und Messprobe); AK3 (Messung vor der Abnahme,
unverändert).

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Abschnitt 1
(E36) und 2 (AK1); [Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) Kopf und
2.2 (N-A4); die Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.42 Entscheid E37 — Die Kälteseite der Kopplung bekommt eigene Spalten für die Kühlübergabe

**Entscheid E37 (Anwender, 24.09.2026).** Die Kälteseite der
[Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) war nach **H9** (E24) für
AK1 zugesagt, sobald KU2 steht; die zweite Welle von AK1 hat sie **benannt vertagt**, weil `AK-S1`
keine gebäudeseitigen Spalten der Kühlübergabe führt und der Auslegungspunkt nach der Anlagenkopplung
7.4 (alter Punkt 4) an der Anlage hing. Der Anwender entscheidet: **Die Kälteseite bekommt eigene
Spalten am Gebäude**, gebaut mit der vierten Welle von AK1. Zugleich beantwortet er die vier Fragen
des Umsetzungsauftrags:

| Nr. | Frage | Entscheid |
|---|---|---|
| **A1** | eigener Schalter der Kühlübergabe? | **ja — abweichend von der Empfehlung:** `Kuehluebergabe_Aktiv` als erste Spalte, behandelt wie `Heizkreis_Aktiv`; beim Abschalten bleibt die gewählte Art erhalten, der Wortlaut von N-A4 (E36) bleibt |
| **A2** | Nennleistung, wenn das Feld leer ist | nach Empfehlung: die Kühllast eines **periodisch eingeschwungenen Auslegungstags** (höchstes Tagesmittel der Außentemperatur, solare und innere Lasten, ideal auf den Kühlsollwert) — das Gegenstück der stationären Heizlast (H7), kein Normnachweis, kein zusätzlicher Jahreslauf |
| **A3** | Kühlkurve und Kennlinienwahl der Wärmepumpe je Stunde | nach Empfehlung **vertagt**: fester Kaltwasser-Vorlauf; die Wärmepumpe rechnet wie in KU2 am `Kuehl_Vorlauf`, die Mischgruppe sitzt am Gebäude |
| **A4** | EPOS-Vorgaben je Art | wie vorgeschlagen (Tabelle unten) |

**Die Spalten** — Papiername `KAK-S1`, Schemaschritt **135**, je in `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`, also sechzehn
`SchemaSpalte`-Einträge, hinter den dreizehn Übergabespalten von `AK-S1`; die Sicht
`Abfrage_Projektgebaeude` wird zum vierten Mal neu gebaut (90 → 98 Spalten). NULL ist die Vorgabe, kein
DDL-DEFAULT auf einem Fachwert; die Bereiche prüft der Eingang.

| Spalte | Typangabe | NULL bedeutet |
|---|---|---|
| `Kuehluebergabe_Aktiv` | `YESNO` wie `Heizkreis_Aktiv` | — (Schalter, Vorgabe aus) |
| `Kuehl_Uebergabe_Art` | `TEXT(20)` | ideal: Kälteseite nicht gekoppelt, Bestandsweg |
| `Kuehl_Uebergabe_Exponent` | `DOUBLE` | Vorgabe der Art (1,0 bis 1,6) |
| `Kuehl_Uebergabe_Leistung_Nenn` | `DOUBLE` (kW, sensibel) | hergeleitet aus dem Auslegungstag (A2) |
| `Kuehl_Auslegung_Vorlauf` | `DOUBLE` (°C) | Vorgabe der Art (4 bis 22 °C) |
| `Kuehl_Auslegung_Ruecklauf` | `DOUBLE` (°C) | Vorgabe der Art; Vorlauf < Rücklauf < Raum |
| `Kuehl_Auslegung_Raumtemperatur` | `DOUBLE` (°C) | `Kuehl_Sollwert` (20 bis 30 °C) |
| `Kuehl_Vorlaufgrenze` | `DOUBLE` (°C) | Vorgabe der Art; beim Gebläsekonvektor keine Grenze |

| Art (EPOS-Vorgabe, A4) | n | Auslegung Vorlauf/Rücklauf | Strahlungsanteil | Vorlaufgrenze |
|---|---|---|---|---|
| Kühldecke | 1,1 | 16/19 °C | 0,5 | 16 °C |
| Flächenkühlung | 1,1 | 16/19 °C | 0,5 | 16 °C; Estrich masselos |
| Gebläsekonvektor | 1,0 | 7/12 °C | 0 | keine; Leistung sensibel, ohne Entfeuchtung (K5) |

Dazu die Ergebnisspalten der Kälteseite (Papiername `KAK-S3`, ein **eigener** Schemaschritt, **136**, alle
nullbar, NULL = nicht kühlgekoppelt gerechnet): `Kuehl_Vorlauf_Mittel`, `Kuehl_Ruecklauf_Mittel` und
`Kuehl_Uebergabe_Begrenzt_Stunden` in `Tab_ErgebnisEnergiebedarf`; `Kuehl_Uebergabe_Art`,
`KuehlVorlaufMittel_C`, `KuehlRuecklaufMittel_C`, `KuehlUebergabeBegrenzt_H` und
`KuehlVorlaufgrenze_H` in `Tab_ErgebnisGebaeude`.

**Der Rechenweg in drei Sätzen.** Schritt H der Anlagenkopplung (10.2) rechnet die Kälteseite
spiegelbildlich — alle Temperaturen und Vergleiche im gespiegelten Raum (−θ), die Kennwerte der
Übergabe mit (−V, −R, −θ_i,N), der P-Regler mit demselben Proportionalband auf dem Band
[θ_kühl, θ_kühl + Xp]. Der Kaltwasser-Vorlauf ist fest: der kälteste wirksame `Kuehl_Vorlauf` der
Wärmepumpen im Kühlbetrieb, am Gebäude auf die Vorlaufgrenze hochgemischt (max(Anlage, Grenze)), ohne
Anlagenwert der Auslegungsvorlauf. Ist die Übergabe in einer Stunde gesättigt und der Vorlauf an der
Grenze, trägt die Stunde den Grund `VORLAUFGRENZE_KUEHLUNG`; die Grenze ist eine Vorgabe, keine
gerechnete Taupunktgrenze (K5).

**Die Zone.** `Kuehl_Uebergabe_Art`, `Kuehl_Uebergabe_Exponent` und `Kuehl_Uebergabe_Leistung_Nenn`
kommen in einem **eigenen Schemaschritt nach S-C** an `Tab_Zone` — Schemaschritt **137**, S-C stand
beim Schemacommit schon (NULL = Wert des Gebäudes bzw. Anteil der Zonenfläche) —, ohne Schalter wie die
Heizseite. Gerechnet wird die Übergabe je Zone ab G6.

**Neue benannte Abweichungen von der Symmetrie** (Anlagenkopplung 7.4, ersetzen den alten Punkt 4):
keine Kühlkurve und keine Kennlinienwahl je Stunde, die Wärmepumpe bleibt am `Kuehl_Vorlauf` (A3); ein
gemeinsames `Regler_Proportionalband` — ein Raumregler, zwei Sequenzen; kein Sollwertprofil der Kühlung
(kommt mit KU3); der Strahlungsanteil ist eine Vorgabe je Art ohne eigene Spalte (Kühlkonzept 3.2); die
Nennleistung kommt aus dem Auslegungstag statt aus einer stationären Rechnung (A2).

**Folge für E36 und N-A4.** Der Wortlaut bleibt; **wirksame Kopplung heißt „Heizseite oder Kälteseite
wirksam"**. Die Kälteseite ist wirksam mit Projektstufe AK1 oder höher, wirksamer Kühlung nach E32
(Projektschalter Kühlbetrieb, `Kuehlung_Aktiv`, Kühlsollwert), dem Schalter `Kuehluebergabe_Aktiv` und
einer Kühlübergabeart ungleich ideal (A1) — unabhängig von `Heizkreis_Aktiv`.

**Was offen bleibt.** Das Register zählt weiter **8 offene Punkte**; E37 beantwortet H9 für AK1 und
öffnet keine Frage. Referenzprojekt mit Kopplung, Einfrierregel und Basis R15 kommen mit der fünften
Welle von AK1; sie friert `Kuehluebergabe_Aktiv` mit ein.

**Betroffene Stufen:** AK1 (Wellen 4 und 5), G3 (Schritt S-C, Zonenspalten), G6 (Übergabe je Zone),
KU3 (Sollwertprofil der Kühlung).

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Abschnitt 1
(E37); [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Vermerk unter H9);
[Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) Kopf, 2.1 (F-A16), 2.2
(N-A4), 7.1, 7.2, 7.4, 8, 8.1, 8.3, 8.4, 9.1, 9.5, 10.5, 11.1 und 13.1;
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 3.2 und 7.1;
[Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 4.2; die Indexzeilen in
[`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.43 Entscheid E38 — U13, U14 und U15 nach Empfehlung; ein iOS-Lauf zur Abnahme von G4a

**Entscheid E38 (Anwender, 24.09.2026).** Der Anwender entscheidet die drei Punkte **U13**, **U14**
und **U15** des [Registers der offenen Entscheide](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md)
nach dessen Empfehlung und legt fest, wie der iOS-Nachweis des IFC-Imports aus **E18** (N1.23)
läuft. Die drei waren nach E28 (N1.33) die letzten offenen Fragen des
[Umsetzungskonzepts](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) und vor G4 fällig.
Sie gelten für **beide Importwege** —
den gbXML-Import (G4c) und den IFC-Import (G4a) —, weil beide über das gemeinsame Zuordnungsgerüst
dieselben Zielfelder füllen ([Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md)
2.1). Wie in E27 und E28 gilt die Empfehlung im Wortlaut des jeweiligen Registerabschnitts.

**Was damit gilt.**

| Nr. | Entscheid | Wirkt in |
|---|---|---|
| **U13** | Mehrere Gebäude in einer Datei: **eines je Lauf** — der Dialog bietet die Gebäude der Datei in einer Klappliste an, übernommen wird je Lauf eines. „Alle auf einmal" entfällt und mit ihm die erzeugten Katalognamen und die Dublettenlogik des Katalogimports | G4c (`Campus/Building`, Datenaustauschkonzept 3.4), G4a (`IfcBuilding`, Umsetzungskonzept 3.5 Nr. 1) |
| **U14** | Die Wandfläche wird um Fenster und Außentüren **vermindert**: A_Wand = Σ Bruttowandfläche (IFC `GrossSideArea`, gbXML `RectangularGeometry`) − Σ A_Fenster − Σ A_Außentür; ohne Abzug zählte die Öffnung als Wand und als Fenster. Wird die Differenz negativ, greift beim IFC-Weg `NetSideArea`, sonst Warnung und A_Wand = 0; beim gbXML-Weg A = 0, Zeile rot, `IMP_GBXML_PROT_NETTOFLAECHE_NEGATIV` | G4c (Datenaustauschkonzept 3.6), G4a (Umsetzungskonzept 3.5 Nr. 10) |
| **U15** | Die drei Wärmebrückenkennwerte ψ werden **als Vorgabe je Baualtersklasse** gesetzt, die drei Anschlusslängen bleiben **leer** — eine geratene Länge sähe aus wie eine gemessene; beide tragen ihre Herkunftsmarke (`Vorgabe` bzw. `Leer`) | G4c (Datenaustauschkonzept 3.7), G4a (Umsetzungskonzept 3.4) |
| **E18** (IFC auf iOS) | **Genau ein** iOS-Lauf (`ios.yml`) als Trimming- und Gerätenachweis für G4a — **ausschließlich nach ausdrücklicher Rückfrage beim Anwender zum Zeitpunkt der Abnahme von G4a**; sonst gilt für die Sitzung „keine iOS-/macOS-Läufe". Für G4c heißt das: Abnahme ohne iOS-Lauf | G4a (G4-8, Abnahme) |

**Beauftragung der Stufe G4.** Mit E38 beauftragt der Anwender zugleich die Stufe **G4**: zuerst
**G4c** (gbXML-Import), dann **G4a** (IFC-Import) — die Reihenfolge aus D1 (E27, N1.32); **G4b**
(IFC auf Bauteilebene) erst nach G3 und nachdem G4a im Feld war
([Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 3.8 und Kapitel 4).

**Was offen bleibt.** Vor G4 ist **kein Anwenderentscheid mehr offen**; Konzept, Umsetzungskonzept,
Datenaustauschkonzept, Softwarearchitektur und Kühlkonzept haben keinen offenen Registerpunkt mehr.
Das Register zählt **8 offene Punkte** — M3, M5–M8 und M11–M13 (G6b bis G6d). Es bleibt die
Folgeaufgabe **D6** aus E27 (das Gegenüber des IFC-Exports, vor der Stufe über die semantische
hinaus). Weil G4c ohne iOS-Lauf abgenommen wird, bleibt die iOS-Zahl der gbXML-Größengrenze
(Datenaustauschkonzept 11.2, D15) bis zur Abnahme von G4a ein Richtwert.

**Betroffene Stufen:** G4c (U13, U14, U15), G4a (U13, U14, U15, der eine iOS-Lauf), G4b (erst nach
G3 und nachdem G4a im Feld war).

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Kopf
und Abschnitte 1 (E38), 2 (G4) und 3; [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md)
(Vermerk unter U13, U14 und U15, Ergänzung unter U16, Kopf, Kapitel 0, 2 und 9, Zählung 8);
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Kopf, Vorspann, 3.4
bis 3.8, 4 und 5; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) Kopf, 3.4,
3.6, 3.7 und 9; die Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.44 Entscheid E39 — Der Baustoffkatalog führt auch die Produkte der wichtigsten Hersteller

**Entscheid E39 (Anwenderwunsch, 24.09.2026).** Schemaschritt S-A der Stufe G3 sät den
Baustoffkatalog; geplant waren rund 60 herstellerneutrale Stoffe nach DIN 4108-4 und
DIN EN ISO 10456 (6.3, [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 3.5). Der Anwender
wünscht dazu die Produkte der wichtigsten Hersteller. Umgesetzt mit der zweiten Welle von G3
(Schritt **132**):

1. **Eine Saat in zwei Teilen.** **65 herstellerneutrale Stoffe** (DIN 4108-4:2020-11,
   DIN EN ISO 10456; feste Ids 1 bis 65) und **67 Herstellerprodukte** (feste Ids 1001 bis 1067) mit
   den Bemessungswerten aus den Herstellerunterlagen (Datenblatt, Leistungserklärung, Zulassung) —
   beide `ReadOnly = 1`, Herkunft `VORGABE`, **Quelle je Zeile**. Der Beleg jeder Herstellerzeile
   steht als Kommentar neben der Saatzeile im Quelltext, nicht in der Datenbank.
2. **Spalte `Hersteller`** an `Tab_Baustoff_STAMM` und `Tab_Baustoff`, spaltengleich, höchstens
   80 Zeichen, **NULL = herstellerneutral**; der natürliche Schlüssel der Saat ist Hersteller und
   Bezeichner. Die Verwaltung „Baustoffe" filtert nach Gruppe, Hersteller und „nur herstellerneutral".
3. **Keine Hersteller im Wiki.** Hersteller- und Produktnamen gehören in den Katalog, nie ins Wiki;
   `WikiProduktdatenWacheTests` hält die Wiki-Quellen auch gegen die Herstellerzeilen des
   Baustoffkatalogs (Normnamen wie „Stahlbeton" bleiben erlaubt).

**Benannte Lücken.** Ein großer Dämmstoffhersteller fehlt: Seine Seite verlangt eine Zugangsprüfung,
die die Recherche nicht umgeht. Einige Rohdichten stammen aus Umweltproduktdeklarationen statt aus dem
Datenblatt; die Quelle der Zeile nennt das (etwa „…; EPD niedriger Rohdichtebereich“). Die Saat legt
nur an, was fehlt — eine später ergänzte oder geänderte Zeile erreicht eine bestehende Installation
nur über einen eigenen Schemaschritt. Für die Zeilen 1041 und 1066 ist das **Schritt 143**
(`BaustoffQuellenBerichtigung`): Er ergänzt ihre Quelle um die Herkunft der Rohdichte (FDES bzw.
Mindestwert der Brandklasse nach VDPM-EPD), im Katalog und in jeder Projektkopie, und nur dort, wo der
alte Saattext wortgleich steht.

**Was offen bleibt.** Nichts; das Register zählt weiter **8 offene Punkte**.

**Betroffene Stufen:** G3 (Schritt 132, Saat, Verwaltung „Baustoffe"); G6c (Namensabgleich gegen
`Bezeichner`, Mehrzonenkonzept 3.5).

**Nachgezogen:** Kopf dieses Papiers und 6.3; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitt 1 (E39); [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Kopf, 3.5 und 4.2;
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) Kopf und 2.2; die
Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.45 Entscheid E40 — „Gebäude als eine Zone übernehmen" rechnet die Hülle hoch

**Entscheid E40 (Anwender, 25.09.2026, „Hochrechnen").** Nach E8 (N1.12, 4.7) rechnet ein Gebäude
ohne Zone mit den Katalogdaten und wird mit dem Faktor der Skalierung — Nutzflächenzuordnung oder
Verbrauchs-Rückrechnung — nachmultipliziert. Ein Gebäude mit Zone hat die echte Hülle und rechnet
mit dem Faktor 1 (dritte Welle von G3). Offen war, was „Gebäude als eine Zone übernehmen" aus einem
Gebäude macht, dessen Ergebnis bis dahin hochgerechnet wurde. Der Anwender entscheidet:

1. **Hochrechnen.** Die Übernahme multipliziert die Bauteilflächen und ψ·L mit dem Faktor der
   bisherigen Nachmultiplikation; die Zone trägt die hochgerechnete Nutzfläche (Faktor × Nutzfläche).
2. **Flächenschlüssel der Zone.** Die flächenbezogenen Größen folgen der Nutzfläche der Zone:
   Luftvolumen, Speichermasse der Bauweise, innere Gewinne und die Innenfläche f_IW·A_f. Der Anteil 1
   rechnet bitgleich.
3. **Das Ergebnis bleibt beim Übernehmen gleich.** Nachgewiesen über die Datenbank an 1007
   (Faktor 4,59), 1008, 1018 und 1017 (ohne Kühlleistungsgrenze), dazu an 1007 mit Verbrauchsangabe:
   relativ **≤ 10⁻⁹**. Danach gilt die echte Hülle ohne Nachmultiplikation; eine Verbrauchs- oder
   Flächenangabe steht nur als Hinweis im Protokoll, die Skalierungsangabe ist mit Herleitungszeile
   weich gesperrt.
4. **Leistungsgrenzen werden nicht hochgerechnet** (Heiz- und Kühlleistungsgrenze) — benannt in der
   Rückfrage vor der Übernahme, die Faktor, Nutzfläche alt und neu, Angabe und die Leistungsgrenzen
   des Gebäudes mit ihrem Wert nennt.

**Verworfen:** die Übernahme im Katalogmaß (das Ergebnis spränge beim Übernehmen um den Faktor) und
eine Sperre der Übernahme bei Hochrechnung (sie schlösse gerade die skalierten Gebäude vom Bauteilweg
aus).

**Was offen bleibt.** Nichts; E8 gilt für Gebäude ohne Zone unverändert, das Register zählt weiter
**8 offene Punkte**.

**Betroffene Stufen:** G3 (sechste Welle, D2); G6 (Flächenschlüssel je Zone bei mehreren Zonen).

**Nachgezogen:** Kopf dieses Papiers und 4.7; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Abschnitte 1 (E40) und 2 (G3); [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
8.3; die Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.46 Festlegungen der Umsetzung G3 — benannt, nicht entschieden

**Anlass.** Die Stufe G3 ist in sechs Wellen gebaut (A, B, W, D1, C, D2; 24./25.09.2026, vom Anwender
am 24.09.2026 beauftragt; [Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G3_Bauteilkatalog.md)).
Wo die Papiere schwiegen oder die Umsetzung von ihnen abweicht, hat sie festgelegt. Die Liste nennt
diese Festlegungen, damit sie nicht als Anwenderentscheide gelesen werden; Widerspruch ist möglich
und würde ein eigener Entscheid.

| # | Festlegung | Wo |
|---|---|---|
| 1 | **W1 gilt:** alle acht Tabellen entstehen mit G3 (Schritte 132–134) — auch `Tab_Bauteilaufbau(_STAMM)`, die das Mehrzonenkonzept Rev. 3 noch G6a zuschrieb | 6.3; Mehrzonenkonzept 4.4, 9 |
| 2 | Die Identifikation nach (12)–(17) folgt der quelloffenen Referenzumsetzung und ist numerisch belegt (Normnachweis) | 4.3; Rechenschritte B4 |
| 3 | Die Bauteile einer Gruppe werden über die **komplexen Widerstände** mit T_RA = 5 d parallel geschaltet (19)–(24), nicht über ΣC und Σ1/R | 4.3; Rechenschritte B5 |
| 4 | Bezugsperiode **je Bauteil** nach (10a)–(10d) | Rechenschritte B1 |
| 5 | Grenzfall: Eine Gruppe ohne Schichten rechnet den Klassenweg aus den Bauteilsummen — Bauteilweg = Klassenweg an den 15 Testgebäuden bis 4,7·10⁻¹⁶ relativ, im Jahreslauf (fünf Gebäude, je acht Varianten) 1,3·10⁻¹⁴ | Rechenschritte B7 |
| 6 | Ein **masseloses opakes Bauteil** geht wie ein Fenster ein (R₁ = R/6 nach (25)/(26)); ein **masseloses Innenbauteil** trägt nur Fläche | Rechenschritte 3 |
| 7 | α_kon je Bauteil geht als Σ(α·A) in die Gruppe ein — Testbeispiel 10 liegt damit in einzelnen Stunden höchstens **0,088 K** außerhalb des Bands (benannte Abweichung wie Fall 11); die AixLib trifft es nur mit einem um **15,7 %** höheren konvektiven Übergang | Rechenschritte 10.3, 11 Zeile 20 |
| 8 | Im Normnachweis benannt, nicht an den Formeln: ein **Druckfehler der Richtlinie in Testbeispiel 4** (Decke DE2; der Nachweis nimmt das Bauteil aus Testbeispiel 3) und die Abweichung **FB1 in Testbeispiel 1** (Eingangsdaten der AixLib, rund 3·10⁻⁴ relativ) | Rechenschritte 10.3, 11 Zeile 21 |
| 9 | Die **Azimutpflicht** gilt nur an Außenluft: Eine Wand an Außenluft ohne Azimut wird benannt abgelehnt, an Erdreich, unbeheiztem Raum oder innerhalb der Zone ist er entbehrlich | Mehrzonenkonzept 4.2, 5.3 |
| 10 | Eine **leere Randbedingung** heißt an Innenwand und Decke „innerhalb der Zone", sonst Außenluft — die Regel steht an einer Stelle (`GebaeudeZonenabbildung.RandAusZeile`) | Softwarearchitektur 2.2 |
| 11 | Ein **unbeheizter Nachbarraum** rechnet mit der Kellertemperatur des Gebäudes — bis zum Entscheid M3 des Registers und G6b | Mehrzonenkonzept 2.5 |
| 12 | Eine Zone mit `IstBeheizt = 0` rechnet in G3 **wie beheizt**; unbeheizte Zonen kommen mit G6 | Mehrzonenkonzept 2.5 |
| 13 | G3 liest von der Zone **nur Nutzfläche (E40) und Bauteile**; Sollwerte, Lüftung, Gewinne, Kühl- und Übergabespalten der Zone bleiben bis G6 ungelesen, es gelten die Werte des Gebäudes; zwei Zonen werden benannt abgelehnt | 4.3; Mehrzonenkonzept 4.2 |
| 14 | **Kaskadenmessung A1:** Kein gewöhnlicher Speicherweg löscht ein Gebäude und legt es neu an; Zonen fallen nur beim Entfernen oder Tauschen des Gebäudes und beim Löschen des Projekts — eine Rettung ist nicht nötig | Register A1 |
| 15 | Die Projektkopien `Tab_Baustoff` und `Tab_Bauteilaufbau` tragen den **Fremdschlüssel auf `Tab_Projekt`** (`ON DELETE CASCADE ON UPDATE CASCADE`) nach der Hausregel seit Schemaschritt 96 — abweichend von Softwarearchitektur 2.2 („ohne Fremdschlüssel, wie der Bestand"), die den Stand davor beschrieb | Softwarearchitektur 2.2; Mehrzonenkonzept 4.2 |
| 16 | Die Dämmstoffe der herstellerneutralen Saat heißen nach dem Nennwert „λD 0,0xy" statt nach der Wärmeleitstufe „WLS"; die Spalte `Lambda` trägt den Bemessungswert | Mehrzonenkonzept 3.5 |
| 17 | **Menüplatz:** die zwei Kataloge gemeinsam unter Administration › Gebäude nach „Gebäudetypen" (beide Schalen), nicht unter einem eigenen Punkt „Bauteilkatalog" | 8.4; Softwarearchitektur 3.1 |

**Was offen bleibt.** Die Katalogseite des Gebäudedialogs ist die virtualisierte `Katalogliste`
([Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 3.2, Regel 5); ihre
Rasterprobe (Fälle GD1–GD3) ist im Skriptlauf mit dem installierten Edge gemessen, samt scharfer
Gegenprobe ([Rasterprobe](../../Proben/Rasterprobe/LIESMICH.md)). Aus G3 bleibt nichts offen; G4b
(Bauteile aus IFC) wartet auf die Beauftragung. Das Register zählt weiter **8 offene Punkte**.

**Betroffene Stufen:** G3 (abgeschlossen 25.09.2026); G4b (Bauteile aus IFC); G6a–G6d (übernehmen
die Tabellen unverändert).

**Nachgezogen:** Kopf dieses Papiers, 4.3, 4.7, 6.3, 8.4, 11 und 12;
[Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Kopf und Abschnitte 1 bis 3;
[Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Kopf, 3.5, 4.2, 4.4 und 9;
[Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kopf, A1, A14 und F-M1;
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) Kopf, 2.2, 3.1, 3.2 und 5;
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Kopf, 3, 8.3, 10.3 und 11;
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Kopf und 4; das
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G3_Bauteilkatalog.md); die
Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.47 Entscheid E42 — Die gbXML-Grenze liegt auf iOS bei 25 MB

**Entscheid E42 (Anwender, 25.09.2026).** Die Größengrenze des gbXML-Imports auf iOS wird von **10 MB auf
25 MB** angehoben — dieselbe Zahl wie unter Windows. Grundlage ist die Messung im einen iOS-Lauf zur Abnahme
von G4a (E38; getrimmte Release-App, AOT, Simulator): Der gbXML-Leser braucht rund **8 MB verwalteten bzw.
7,5 MB Prozessspeicher je MB Datei**; die 9,8-MB-Probe hob den Prozess auf rund 240 MB, eine 25-MB-Datei kostet
damit rund 190 MB zusätzlich. D15 (Datenaustauschkonzept 11.2) hatte die iOS-Zahl ausdrücklich als zu messen
geführt; E38 hielt sie bis zur Abnahme von G4a als Richtwert.

**Was damit gilt.**

| Format | Windows | iOS | Grundlage |
|---|---|---|---|
| gbXML | 25 MB | **25 MB** (vorher Richtwert 10 MB) | D15, **E42** nach Messung |
| IFC | 50 MB | 20 MB | U11 (E18); gemessen und bestätigt (20,6-MB-Datei: Prozess rund 400 MB) |

Eine größere Datei wird weiter benannt abgelehnt, bevor gelesen wird. Die Zahl steht als
`GbxmlImportProfil.MAX_BYTES_IOS`; die Hülle belegt sie auf iOS.

**Was offen bleibt.** Nichts Neues; das Register zählt weiter **8 offene Punkte**. Die Geräte-Paketgröße
bleibt offen, bis der Gerätebau der iOS-Schale linkt (Protokoll G4, Abschnitt 10).

**Betroffene Stufen:** G4c (Profil), G4a (Messung).

**Nachgezogen:** Kopf dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Kopf und
Abschnitt 1 (E42) und 2 (G4); [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md)
1.5 (Regel 2 — dort stehen die vier Zahlen); [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md)
2.5 und 11.2 (D15); das [Protokoll G4](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-24_G4_Importe.md);
die Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.48 Entscheid E43 — Vorgaben des Gebäudeimports, Nachtzeit je Gebäude

**Anlass.** Die Sichtprobe des Gebäudeimports (Protokoll G4, Abschnitt 13) zeigte zwei Lücken: Das
Umsetzungskonzept 3.4 nannte für die inneren Gewinne „Vorgabe je Gebäudeart (Wohnbau 5 W/m²)“ und für die
Sollwerte „Vorgaben des Grundlagensatzes“ — beides stand in keinem Code. Der Import übernahm 0 W und, wenn die
Datei keinen Sollwert trug, einen Tagsollwert von 0 °C, den weder der Editor noch das Stundenmodell ablehnten.

**Entscheid E43 (Anwender, 25.09.2026).**

1. **Innere Gewinne:** Vorgabe **5 W/m² × Nutzfläche**, ein Wert für alle Gebäudearten, als ausgewiesene
   Vorgabe (Herkunft „Vorgabe“ mit Beleg), änderbar im Zuordnungsdialog und im Gebäudeeditor. Die
   Auslegungsleistungen der Datei bleiben Vorschlag im Beleg.
2. **Sollwerte ohne Angabe der Datei:** Tag **20 °C**, Nachtabsenkung **18 °C** (höchstens der Tagsollwert),
   als ausgewiesene, änderbare Vorgaben; ein Tagsollwert der Datei gilt vor der Vorgabe.
3. **Die Nachtzeit ist je Gebäude einstellbar:** Beginn und Ende der Nachtabsenkung als volle Stunde (0–23),
   Nacht = [Beginn, Ende), auch über Mitternacht. **Leer heißt 22 bis 6 Uhr** — genau der feste Fahrplan
   (Rechenschritte E8: Tagsollwert in den Stunden 7 … 22, 1-basiert), bitgleich hergeleitet aus
   `GebaeudeFestwerte.TAG_ERSTE_STUNDE`/`TAG_LETZTE_STUNDE`. Nur eine der beiden Angaben oder Beginn = Ende
   ist ein benannter Eingabefehler. Der Import trägt 22 und 6 als Vorgabe ein.

**Was damit gilt.**

| Größe | Vorgabe | Regel |
|---|---|---|
| Innere Gewinne | 5 W/m² × Nutzfläche | ohne Nutzfläche 0 W; Handwert im Dialog und im Editor |
| Tagsollwert | 20 °C | aus der Datei, wenn vorhanden |
| Nachtabsenkung | 18 °C | höchstens der Tagsollwert |
| Nachtzeit | 22 bis 6 Uhr | Spalten `Nachtabsenkung_Beginn` und `Nachtabsenkung_Ende` (Schemaschritt 144) an den Gebäudetabellen, leer = feste Zeit |

**Rechenweg.** Die Nutzungszeit richtet sich nach dem Fahrplan des Gebäudes: der Sollwertfahrplan, die
Kennzahlen der Nutzungszeit (mittlere Raumtemperatur, Überhitzungsstunden; Rechenschritte 8.2), die
Komfortstunden der Anlagenkopplung und die Bestandswoche der Übergabevorgaben. Ein gepflegtes Wochenprofil
(Anlagenkopplung, `Sollwertprofil`) gilt weiter vor dem Tag/Nacht-Fahrplan. Der Tagesbilanz-Altweg liest die
neuen Spalten nicht. **Ergebnisneutral:** Alle Gebäude der Testdatenbank tragen leere Spalten; der
Referenzlauf der 14 Projekte gegen R16 ist byte-gleich, eine neue Basis entsteht nicht.

**Was offen bleibt.** Nichts Neues; das Register zählt weiter **8 offene Punkte**.

**Betroffene Stufen:** G4 (Import), G1/G2 (Stundenfahrplan), AK1 (Komfortstunden).

**Nachgezogen:** Kopf und 4.4 dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Kopf und
Abschnitt 1 (E43) und 2 (G4); [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
3.4 (Zeilen `Waermegewinne` und Sollwerte); [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
1.1, E8 und 8.2; [Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 4.1 (Hinweis);
das [Protokoll G4](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-24_G4_Importe.md) Abschnitt 14; die
Indexzeile in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.49 Entscheide E44 und E45 — Stufe G4b, der Bauteilimport

**Anlass.** Der Gebäudeimport der Stufe G4 (G4c gbXML, G4a IFC) füllt die Summenfelder von
`Tab_Gebaeude` (Einzonenweg, Zonenregel X4); das Gebäude rechnet danach den Klassenweg. Die echte
Hülle aus der Datei — Zone, Bauteile, Aufbauten und Schichten in den Tabellen der Stufe G3 — war als
**G4b** geplant, nach E38 (N1.43) erst nach G3 **und** nachdem G4a im Feld war. G3 ist abgeschlossen
(N1.46), G4a gebaut und im Gebäudedialog angebunden; die Feldphase von G4a steht aus.

**Entscheid E44 (Anwender, 25.09.2026, „fahre mit G4b fort“).** G4b wird jetzt gebaut, abweichend von
E38 vor der Feldphase von G4a.

**Entscheid E45 (Anwender, 25.09.2026) — drei Rechenregeln des Bauteilimports.**

1. **Innere Masse nach Datenlage.** „Vollständig“ heißt: Jede innere Trennfläche zwischen übernommenen
   beheizten Räumen hat eine Fläche und einen vollständigen Aufbau (Dicke, λ, ρ, cp), und die Innenfläche
   beider Seiten liegt im Band **1,0 … 5,0 × Nutzfläche** — das Band steht um die Vorgabe f_IW = 2,5
   (4.3); DIN EN ISO 13790 nennt 2,5 bis 3,5. Dann werden die Trennflächen Bauteilzeilen
   `INNENWAND`/`DECKE` mit leerer Randbedingung (innerhalb der Zone), und beide Seiten zählen: IFC je
   Raumbegrenzung, gbXML als zwei Zeilen, weil eine unsymmetrische Decke von beiden Seiten verschiedene
   Masse hat. Sonst gibt es keine Innenzeilen; der Innenflächenfaktor kommt aus der Datei (gemessene
   Innenfläche beider Seiten ÷ Nutzfläche) in die Spalte `Tab_Gebaeude.Innenflaechenfaktor`, die Masse
   aus der Bauweise. Ohne Innenflächen in der Datei gilt die Vorgabe 2,5; außerhalb des Bands wird der
   Faktor übernommen und gewarnt. Der Dialog nennt den gewählten Weg samt Grund.
2. **U-Wert neben vollständigen Schichten.** Die Schichten rechnen, `U_Wert` bleibt leer; weicht der
   U-Wert der Datei um mehr als 5 % ab, wird das gemeldet. Für den Import weicht das vom Vorrang des
   eingetragenen U-Werts ab ([Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 3.4). Grund:
   R₁, C₁ und U·A kommen aus denselben Schichten, R_Rest kann nicht negativ werden.
3. **Vorhangfassaden transparent.** Eine Zeile `VORHANGFASSADE`; U und g aus der Datei, sonst U aus der
   Fenstervorgabe der Baualtersklasse (Herkunft `VORGABE`); g, Rahmenanteil und Verschattung bleiben
   leer, dann gelten die Werte des Gebäudes bzw. die Vorgaben. In den Summenfeldern stehen sie weiter
   unter „Sonstige“.

**Was damit gilt.**

| Fall | Was der Import schreibt | Grundlage |
|---|---|---|
| Wahl im Importdialog | Abschnitt „Bauteile (echte Hülle)“ mit dem Schalter „Als Zone mit Bauteilen übernehmen“: vorbelegt ein, wenn der Vorschlag gebildet werden kann; sonst aus und gesperrt, der Grund daneben. Ohne Schalter bleibt es beim Summenweg ohne Zone | E44 |
| Summenfelder | füllt der Import weiter; jede Bauteilgruppe des Vorschlags summiert dieselbe Nettofläche wie ihr Summenfeld, sonst benannt abgelehnt | Einzonenweg X4 |
| Zone | **eine** Zone je Gebäude mit Nutzfläche, Volumen und Raumhöhe der übernommenen beheizten Räume, Herkunft `IFC`/`GBXML` | E44 |
| Hüllbauteile | je Bauteil eine Zeile: Nettofläche nach dem Fensterabzug (U14), Azimut samt Nordwinkel (IFC), Neigung, Randbedingung, U, g, Herkunft, Quellkennung; Fenster und Türen je eine Zeile mit dem Azimut ihrer Wand | E44 |
| Vollständige Schichten | Aufbau samt Schichten innen → außen als Projektkopie in `Tab_Bauteilaufbau`/`Tab_Bauteilschicht`, `U_Wert` leer, Abweichung des Datei-U über 5 % gemeldet | E45/2 |
| Unvollständige Stoffwerte | kein Aufbau, nur der U-Wert — der der Datei, sonst der aus einer masselosen Schichtung, sonst die Vorgabe der Baualtersklasse (Herkunft `VORGABE`) | E45/2; Datenaustauschkonzept 3.6 |
| Innere Masse, vollständig | Zeilen `INNENWAND`/`DECKE` mit leerer Randbedingung, beide Seiten | E45/1 |
| Innere Masse, sonst | keine Innenzeilen; `Tab_Gebaeude.Innenflaechenfaktor` aus der Datei, Masse aus der Bauweise; ohne Innenflächen leer (2,5) | E45/1 |
| Vorhangfassade | Zeile `VORHANGFASSADE`, transparent mit Sonneneintrag | E45/3 |
| Schreibweg | ein Vorgang: Projektkopie, `GebaeudeZonenCtrl.VorschlagSchreiben` und `GebaeudeImportCtrl.SchreibeHerkunft` mit den Paarungen für Gebäude, Zone, Bauteile und Aufbauten in `Tab_Importzuordnung`; scheitert ein Teil, bleibt nichts | Datenaustauschkonzept 7.2 |

**Festlegungen der Umsetzung — benannt, nicht entschieden.** Wo die Papiere schwiegen oder die
Umsetzung von ihnen abweicht, hat sie festgelegt; Widerspruch ist möglich und würde ein eigener
Entscheid.

| # | Festlegung | Wo |
|---|---|---|
| 1 | Flächen gegen **unbeheizte oder unbekannte Räume** bekommen die Randbedingung `UNBEHEIZT` (Kellertemperatur, N1.46 Nr. 11) statt Außenluft wie im Einzonenweg; das Summenfeld bleibt (Boden → Grundfläche, Decke → Dach, Wand → Sonstige). Grund: Innenwände ohne Azimut würden an Außenluft nach N1.46 Nr. 9 abgelehnt | Datenaustauschkonzept 3.5 |
| 2 | **Erdberührte Wände und Decken** behalten ihre Bauteilart mit `ERDREICH` und zählen im Summenfeld zur Grundfläche | Datenaustauschkonzept 3.5 |
| 3 | **Fenster in erdberührten Wänden** (auch Vorhangfassaden) rechnen an Außenluft, weil G3 transparente Bauteile an Erdreich ablehnt | Datenaustauschkonzept 3.5 |
| 4 | Die **Herkunft einer Zeile** ist das Format (`IFC`, `GBXML`) und wird `VORGABE`, sobald Fläche oder U eine Vorgabe ist | Datenaustauschkonzept 2.2 |
| 5 | Die **Quellkennung der Zone** ist die Kennung des Gebäudes | Datenaustauschkonzept 7.2 |
| 6 | Ein **g ≤ 0 oder > 1** bleibt leer (Wert des Gebäudes) | Datenaustauschkonzept 3.4 |
| 7 | **Innentüren** werden von der Innenwand abgezogen und bekommen keine Zeile | Datenaustauschkonzept 3.6 |
| 8 | Das **Zielfeld Innenflächenfaktor** trägt den gemessenen Wert auch ohne Schalter (Summenweg), mit Herkunft ausgewiesen und abwählbar, gelb außerhalb des Bands | Umsetzungskonzept 3.4; Rechenschritte A2 |
| 9 | Die Zone behält die **Nutzfläche der Datei**; eine Handänderung der Nutzfläche am Gebäude rechnet über den Flächenschlüssel (E40, N1.45) | 4.7 |
| 10 | **Nicht umgesetzt:** der Namensabgleich mit `Tab_Baustoff` — `ID_Baustoff` bleibt leer, die Stoffwerte sind an die Schicht kopiert | Mehrzonenkonzept 3.5, 6.3 |
| 11 | Eine **IFC-Geschossdecke** trägt in der Datei keine Neigung; sie folgt, wo möglich, aus der Sicht der Räume oder ihrer Geschosslage, sonst gilt die Vorgabe nach Bauteilart — das wirkt nur auf den Übergangswiderstand | Datenaustauschkonzept 3.4 |
| 12 | **Mehrere Zonen** (G6c) bleiben benannt abgelehnt; trägt das Gebäude schon eine Zone, wird nichts geschrieben | Mehrzonenkonzept 6 |

**Rechenweg.** Ein so übernommenes Gebäude rechnet den Bauteilweg (4.3) mit der echten Hülle und dem
Faktor 1 (4.7). Mit Innenzeilen rechnet die Innengruppe über die Schichten, sonst den Klassenweg mit
A_IW = f_IW · A_f und dem Faktor aus der Datei. Kein Schemaschritt — die Tabellen stammen aus G3
(Schritte 132–134) und S-F (Schritt 138), die Spalte `Innenflaechenfaktor` aus M3. **Ergebnisneutral:**
kein Referenzprojekt ist importiert; der Referenzlauf der 14 Projekte gegen R16 war in beiden Wellen
unverändert. **Auskunft** der Jahresheizwärme an Projekt 1045 (dieselbe Gebäudezeile, Klassenweg gegen
Bauteilweg): `gbxml_haus_si.xml` 10,930 / 10,643 MWh (0,974; Innenbauteile übernommen, A_IW 145 m²),
`ifc4_haus.ifc` 10,766 / 10,820 MWh (1,005; Faktor aus der Datei, 201,6 m²),
`gbxml_innenflaechen_teilweise.xml` 5,339 / 5,356 MWh (1,003; Faktor aus der Datei, 66 m²).

**Nachtrag zu E44 (Anwender, 26.09.2026): der Namensabgleich N1…N7 wird vorgezogen.** IFC-Dateien
liefern fast nie brauchbare Stoffwerte; ohne Abgleich bekommt ein IFC-Haus Bauteile, aber keine
Aufbauten. Der Anwender hat deshalb den Namensabgleich aus G6c in G4b vorgezogen; die Synonymtabelle
(Register **M9**, bisher G6a zugeordnet) kommt mit, abgestimmt mit der Sitzung G6a. Was damit gilt:

| Was | Regel |
|---|---|
| Schema | Schritt **146**: `Tab_Baustoffsynonym_STAMM` (normalisierter Materialname, Sprache, Verweis auf `Tab_Baustoff_STAMM`, `ReadOnly`, Quelle; 212 Synonyme deutsch/englisch in der Auslieferung) und `Tab_Baustoffzuordnung` (die eigene Zuordnung N7 **je Projekt**) |
| Reihenfolge | **N7 → N6 → N3 → N4 → N5**: eine gemerkte Zuordnung steht vor jedem automatischen Treffer, sonst ließe sich ein falscher Treffer nie überstimmen; Sonderfälle ohne Stoff (N6) vor dem Katalog, weil eine Schraffur nie ein Stoff ist |
| Herstellerzeilen (E39) | nur beim genauen Namen (N3); eine herstellerneutrale Zeile geht vor; N5 trifft nie ein Produkt |
| Stoffwerte | Band je Wert (λ [0,005; 500], ρ [5; 8 000], c [100; 5 000]); ein Wert der Datei im Band hat Vorrang, fehlende Werte kommen aus dem Katalog; Luftschicht als ruhende Luftschicht nach DIN EN ISO 6946, Schraffur und „Solid …“ verworfen |
| Gegenprobe | liegen alle Werte der Datei vor, rechnet die Datei; weicht λ um mehr als 50 % vom getroffenen Katalogwert ab, gibt es eine Meldung |
| Herkunft | ein Aufbau mit mindestens einem Katalogwert trägt `KATALOG`, die Schicht den Verweis auf die Projektkopie des Baustoffs; die Bauteilzeile behält das Format |
| Meldungen | formatfrei `IMP_BAUTEIL_PROT_*` (Mehrzonenkonzept 6.6 nachgezogen) |

Ergebnisneutral: Testdatenbank auf 146, Referenzlauf 14/14 PASS gegen R19. An den Proben: IFC mit
Materialnamen 0 → 6 Aufbauten (16 von 20 Namen getroffen), IFC mit Nullwerten 0 → 4.

Die zweite Welle bringt den Abschnitt **„Baustoffe“** in den Importdialog: je Materialname der Datei
die Zahl der Schichten, die Stufe (genauer Name, Synonym, Wortanfang, Luftschicht, verworfen, eigene
Zuordnung, ohne Treffer), der zugeordnete Baustoff und die Herkunft der Werte; eine Klappliste der
Katalogbaustoffe nach Gruppe setzt oder ändert die Zuordnung, Namen ohne Treffer sind gelb, jede
Änderung bildet den Bauteilvorschlag neu. Festlegungen: Die eigene Zuordnung wird **beim Speichern
der Projektliste** im selben Vorgang wie Projektkopie, Zone und Herkunft gemerkt, als erster Schritt,
damit ein späterer Fehler sie zurückrollt; sie wird **auch ohne Bauteilschalter** gemerkt, weil sie die
Namen der Datei beschreibt und für das Projekt gilt; die Synonyme bekommen **keine Verwaltung**
(`KatalogRegistry` unberührt) — sie kommen mit der Auslieferung, projektbezogene Wünsche deckt N7; ohne
Projekt (Wirt der Rasterprobe, Tests) nimmt die Hülle Katalog und Synonyme aus der Saat. Kein
Schemaschritt. Einzelheiten im
[Protokoll G4b](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G4b_Bauteilimport.md), Abschnitte 10
und 11.

**Was offen bleibt.** Die Windows-Sichtabnahme (Anwender); der Wiki-Upload der Seite „Gebäudeimport“
mit dem Sammel-Upload 1.2.0.4; Azimute im Dialog mit bis zu drei Nachkommastellen (Kleinigkeit); die
eine Ansicht der gemerkten Zuordnungen eines Projekts außerhalb des Importdialogs; mehrere Zonen (G6c). E44 und E45 berühren
keinen offenen Registerpunkt; M9 ist mit dem Nachtrag umgesetzt; das Register zählt weiter **8 offene
Punkte** (M3, M5–M8, M11–M13) — M7 und M13 bleiben vor G6c fällig.

**Betroffene Stufen:** G4b (abgeschlossen 25.09.2026); G4c und G4a (Zuordnung, Zielfeld
Innenflächenfaktor); G6c (mehrere Zonen, Namensabgleich).

**Nachgezogen:** Kopf, 4.3, 4.7, 6.3 und 7.6 dieses Papiers; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md)
Kopf und Abschnitte 1 (E44, E45), 2 (G4b) und 3; [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
Kopf, 3.1, 3.4, 3.8 und 4; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) Kopf und
3.4 bis 3.7; [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Kopf, 1.2, 3.4 und 6;
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) Kapitel 5 (Zeile G4b);
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Kopf, 1.1 und A2; das
[Update-Papier](Wiki_Update_2026-09-26.md) (Logbuch 1.2.0.4, Seite „Gebäudeimport“); das
[Protokoll G4b](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G4b_Bauteilimport.md); die
Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.50 Entscheid E46 — G6a beauftragt: eine zweite Zone ist speicherbar, 50 Zonen je Gebäude

**Anlass.** Der Auftrag der Stufe G6a (Pflege mehrerer Zonen je Gebäude) stellte drei Fragen (Anhang A),
weil der Lauf mehrere Zonen erst mit G6b rechnet.

**Entscheid (Anwender, 25.09.2026):**

| # | Frage | Entscheid |
|---|---|---|
| A1 | Ist eine zweite Zone schon vor G6b speicherbar? | **ja, mit Schalter:** speicherbar mit Rückfrage und Sperrzeile („Mit N Zonen lehnt die Simulation dieses Gebäude benannt ab"); der Freigabeschalter im Kern (`GebaeudeZonenregeln.MehrereZonenFreigegeben`) steht an und wird für eine Auslieferung vor G6b ausgeschaltet — dann höchstens eine Zone, „+ Neue Zone …" nennt die Sperre. Wiki und Logbuch erst mit G6b |
| A2 | Fragt „Aus dem Projekt entfernen" bei einem Gebäude mit Zonen nach? | **ja:** die Rückfrage nennt die Zahl der Zonen und Bauteile; bei „Nein" bleibt alles stehen |
| A3 | Obergrenze je Gebäude? | **50 Zonen** als vorläufige Konstante der Regelklasse (`GebaeudeZonenregeln.PFLEGEGRENZE`); M12 bleibt bis G6c offen, G6c misst und setzt sie endgültig |

**Betroffene Stufen:** G6a (umgesetzt mit den Wellen 1 bis 4); G6b (rechnet mehrere Zonen, hebt die
Laufgrenze); G6c (M12).

### N1.51 Festlegungen der Umsetzung G6a — benannt, nicht entschieden

**Anlass.** Die Stufe G6a ist in vier Wellen gebaut (W1 bis W4; 25./26.09.2026;
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G6a_Zonenpflege.md)). Wo die Papiere
schwiegen, hat die Umsetzung festgelegt. Die Liste nennt diese Festlegungen, damit sie nicht als
Anwenderentscheide gelesen werden; Widerspruch ist möglich und würde ein eigener Entscheid.

| # | Festlegung | Wo |
|---|---|---|
| 1 | **Eine Regelklasse** (`GebaeudeZonenregeln`, öffentlich): Laufgrenze 1, Pflegegrenze 50 (E46/A3), Freigabeschalter (E46/A1); der Umschalter des Laufs (`GebaeudeZonensatz.EineZone`) und die Oberfläche lesen nur hier | 4.3; Mehrzonenkonzept 5.3 |
| 2 | **Ab zwei Zonen ist die Nutzfläche Pflicht** — im Kern (`GebaeudeZonenCtrl.Pruefen`, Dialog und Schreibweg) und im Zonendialog (kein Platzhalter „Nutzfläche des Gebäudes"): leer hieße „die Fläche des Gebäudes" und zählte sie doppelt | Mehrzonenkonzept 4.2, 5.3 |
| 3 | **Doppelte positive Ids** von Zonen oder Bauteilen in der Liste sind ein Fehler — sonst schriebe der Abgleich dieselbe Zeile zweimal; vorläufige Ids dürfen sich wiederholen | Mehrzonenkonzept 4.1 |
| 4 | **Hinweise statt Fehler:** Σ Nutzfläche der Zonen gegen die des Gebäudes ab 5 % Abweichung — erst ab zwei Zonen, weil eine einzelne Zone nach der Übernahme (E40) bewusst die Fläche des wirklichen Gebäudes trägt —, dazu eine Zone ohne Bauteil an Außenluft, Erdreich oder unbeheiztem Raum | Mehrzonenkonzept 5.3 (E19) |
| 5 | **Eine Formel für die Zonenkennwerte** (`Zonenkennwerte`): Fläche, Volumen (das der Zone, sonst Fläche × Raumhöhe, gekennzeichnet als abgeleitet), H_T = Σ U·A + Σ ψ·L mit U aus dem Aufbau, H_ve nach der Regel des Laufs (Fläche der Zone × Raumhöhe des **Gebäudes**, auf dem VDI-Weg mit dem wirksamen Luftwechsel), Zahl der Bauteile; bei einer Zone bitgleich zur Anzeige davor | 4.3; Mehrzonenkonzept 4.3 |
| 6 | **Der Arbeitsstand arbeitet über Ids**; eine neue vorläufige Id liegt unter allen vergebenen (−1 trägt die Übernahmezone). Das behebt die stille Löschung: Das frühere Ersetzen der ganzen Liste behielt beim OK nur eine Zone | Softwarearchitektur 3.3 |
| 7 | **Ein Duplikat nennt seine Vorlage** (`ZoneDaten.VorlageId`): Die Hülle übernimmt deren ungelesene Spalten (Sollwerte, Lüftung, Kühl- und Übergabewerte), nicht aber Herkunft, Quellkennung und Importpaarung; Zone und Bauteile der Kopie sind neu und manuell | Datenaustauschkonzept 7.4 |
| 8 | **Die Texte** der Zonenpflege führen das Präfix `GEBZ_` fort (nicht `ZON_`), die Meldungen des Kerns `ZONE_MSG_`/`ZONE_HINWEIS_` | Softwarearchitektur 3.2 |
| 9 | **Die Bericht-Zonentabelle** steht je Gebäude mit Zonen im Gebäudeblock der Projektbeschreibung und führt Zone, Nutzfläche, Volumen (abgeleitet mit Stern und Hinweis), H_T, H_ve und Bauteile samt Summenzeile, ohne Spalte „beheizt" (Festlegung 12 in N1.46) und ohne Heizwärme und Spitze (G6b); ohne Zonen entfällt sie | Mehrzonenkonzept 9 |
| 10 | **Die Zonenmerkmale des Variantenvergleichs** — Zahl der Zonen, Σ Nutzfläche, Σ H_T, Rechenweg der Hülle (Klassenweg, Bauteilweg oder „Bauteilweg (n von m Gebäuden)") — gelten über **alle** Gebäude des Projekts, nicht nur über das erste wie die übrigen Gebäudemerkmale | Konzept Berichtswesen, Baustein 4 |

**Vermerke für G6b.**
- `Tab_Zone` hat keine Nachtzeit (`Nachtabsenkung_Beginn`/`_Ende` am Gebäude, Schemaschritt 144); die
  Regel „gleicher Name wie am Gebäude" (`ZonenSchema.GebaeudewertSpalten`) greift für sie nicht.
- Σ H_T über die Zonen gilt nur ohne Grenzen zu einer Nachbarzone; mit `Randbedingung = 'ZONE'` wird
  eine Trennfläche sonst doppelt gezählt.
- `IstBeheizt = 0` rechnet bis G6b wie beheizt (N1.46, 12); die Bedeutung ist mit der Zonenschleife
  festzulegen.
- Heizwärme und Spitze je Zone gehören in die Bericht-Zonentabelle, sobald G6b sie rechnet; H_ve folgt
  dann der Raumhöhe bzw. dem Volumen der Zone.

**Was offen bleibt.** Den Freigabeschalter `GebaeudeZonenregeln.MehrereZonenFreigegeben` vor jeder Auslieferung
ohne G6b ausschalten (E46/A1); Wiki und Logbuch mit G6b; die Windows-Sichtabnahme des Zonenreiters
und der Zonentabelle im Bericht.

**Betroffene Stufen:** G6a; G6b (Vermerke oben).

**Nachgezogen:** [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Kopf und Abschnitte 1 und 2;
[Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Kopf, 2, 4.4, 5.3, 8 und 9;
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 3.2;
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Löschliste GA; das
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G6a_Zonenpflege.md); die Indexzeile in
[`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.52 Entscheid E47 — Baualtersklassen nach Bauzeitraum, Energiestandard als eigenes Feld

**Entscheid E47 (Anwender, 26.09.2026).** Anlass ist die Windows-Sichtabnahme des Gebäudeimports (Protokoll
G4, Abschnitt 15, Punkt 6). Die 21 Baualtersklassen A–U werden abgelöst: Die **Baualtersklasse** ist ein
Bauzeitraum mit den Buchstaben der Deutschen Wohngebäudetypologie des IWU (A bis 1859 … K 2010–2015), ergänzt
um **L 2016–2020** und **M ab 2021**, für Wohn- und Nichtwohngebäude gleich; das **Baujahr führt**, die Klasse
folgt aus ihm und ist nur ohne Baujahr wählbar. Der **Energiestandard** ist ein eigenes, freiwilliges Feld mit
zwölf Einträgen (teilsaniert, saniert nach GModG, Niedrigenergiehaus, Effizienzhaus 115/100 historisch, 85,
70, 55, 40, Denkmal, Passivhaus, Nullemissionsgebäude), gefiltert nach Wohn- und Nichtwohngebäude.

**Vorgaben.** E27 bleibt: Mediane der eigenen Katalogsätze, zuerst je Energiestandard, sonst je Klasse; ohne
Katalogsatz bleibt die Vorgabe leer. Übernommen werden nur die Jahresgrenzen der Typologie, nicht ihre
Kennwerte.

**Bestand.** Ein Schemaschritt schlüsselt die gespeicherten Klassen um (Baujahr zuerst, sonst Tabelle im
Konzept Baualtersklassen, Abschnitt 5) und benennt die Sätze des Auslieferungskatalogs um, deren zweiter
Namensteil der alte Buchstabe ist. Kein Rechenweg liest Klasse oder Standard; der Referenzlauf bleibt
byte-gleich, eine neue Basis entsteht nicht.

**Was offen bleibt.** Nichts Neues im Register; die Umsetzung folgt in Wellen nach dem
[Konzept Baualtersklassen](Konzept_Baualtersklassen_Energiestandard_EPOS-Plan.md) (Abschnitt 7).

**Betroffene Stufen:** G4 (Import, Vorgaben), G4b (Bauteilvorschlag), Gebäudeeditor und -verwaltung, Bericht.

### N1.53 Entscheid E48 — G7a vorab gebaut, hinter einem Freigabeschalter; Schemakopie lokal; Klassenweg als Übernahmevorschlag

**Anlass.** Der Auftrag der Stufe G7a (gbXML-Export, Stufe 1: Daten ohne Geometrie) stellte drei Fragen
(Anhang A). D2 (mit E27 entschieden, Register Kapitel 4) legt fest, dass der gbXML-Export nur mit der
zweiten Stufe kommt — G7a und G7b zusammen; G7a vorab zu bauen braucht deshalb einen eigenen Entscheid.

**Entscheid E48 (Anwender, 26.09.2026):**

| # | Frage | Entscheid |
|---|---|---|
| F1 | G7a jetzt bauen, abweichend von D2/E27 und vor G6b bis G6d? | **ja, als Bauabweichung, nicht als Auslieferungsabweichung:** G7a steht hinter dem Freigabeschalter `GebaeudeExportRegeln.GbxmlExportFreigegeben` (`private const` im Kern), der im Entwicklungsstand an ist und vor jeder Auslieferung ausgeschaltet wird; ausgeliefert wird G7a samt Wiki und Logbuch erst mit G7b. D2 bleibt als Auslieferungsregel stehen. Nutzen jetzt: das Rundlauf-Regressionsnetz Export ↔ Import und der Beleg- und Archivexport im Entwicklungsstand. Ob das Zonengeometrie-Modell für G7b vorgezogen wird, entscheidet der Auftrag G7b |
| F2 | Die gbXML-Schemakopie lokal beistellen? | **ja:** `GreenBuildingXML_Ver8.01.xsd` (387 450 Byte, aus dem Repositorium GreenBuildingXML/gbXML_Schemas, abgerufen 26.09.2026) liegt nach D17 unter `Referenzlaeufe/Schemakopien/`, per `.gitignore` ausgeschlossen, mit `LIESMICH.md` (Herkunft, Abrufdatum, Lizenzstand „keine"); nie versioniert, nie ausgeliefert, nie aus dem Netz geladen |
| F3 | Klassenweg (Gebäude ohne Zonen): was wird exportiert? | **(a) der Übernahmevorschlag**, genau wie „Hülle und Zonen…" ihn bildet — samt Hochrechnung (E40) und bei einer Verbrauchsangabe den Größen aus dem Verbrauchsverhältnis; Faktor und Grundlage stehen in `Campus/Description` |

**Regel ohne Anwenderfrage.** Die Postleitzahl ist eine freiwillige Eingabe des Exportdialogs und wird nicht
gespeichert; ohne sie entfällt `Location` samt Nordangabe (Probe 3: `Location` ist optional). Eine
Projektspalte wäre ein eigener Schemaschritt außerhalb von G7a.

**Betroffene Stufen:** G7a (umgesetzt in vier Wellen, N1.54); G7b (Auslieferung, Wiki, Logbuch, Einstieg
im Bedarfsdialog, Probe 20); G7d (Round-Trip-Sperre).

### N1.54 Festlegungen der Umsetzung G7a — benannt, nicht entschieden

**Anlass.** Die Stufe G7a ist in vier Wellen gebaut (W1 bis W4; 26.09.2026;
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G7a_gbXML-Export.md)). Wo Auftrag und
Papiere schwiegen, hat die Umsetzung festgelegt; der Orchestrator hat jede Festlegung abgenommen. Die
Liste nennt sie, damit sie nicht als Anwenderentscheide gelesen werden; Widerspruch ist möglich und würde
ein eigener Entscheid.

| # | Festlegung | Wo |
|---|---|---|
| 1 | **Gegen UNBEHEIZT die übliche Innenfläche:** Außenwand, Dach und Bodenplatte gegen einen unbeheizten Raum werden `InteriorWall`, `Ceiling` bzw. `InteriorFloor` mit dem Platzhalter „unbeheizt" als zweitem Raum; sie kehren als Innenwand bzw. Decke zurück — ein benannter Wechsel, die Randbedingung bleibt | Datenaustauschkonzept 5.2 |
| 2 | **Trennfläche zur Nachbarzone** (nach G6b): eine Innenfläche nach der Neigung mit dem Raum der Nachbarzone, Randkürzel `zo`; mit beheizter Nachbarzone kehrt sie als innere Masse zurück (benannter Wechsel); ohne Nachbarzone benannte Ablehnung | Datenaustauschkonzept 5.2 |
| 3 | **Zusätzliche Kennungen** nach der Regel aus Schlüsseln: Schicht, Stoff und Fenstertyp des Klassenwegs, Aufbau, Schicht und Stoff der Innenmasse (`epos-innenmasse-<G>`, bei mehreren Zonen `-<Z>`), `epos-programm` und `epos-person` | Datenaustauschkonzept 5.4 |
| 4 | **`DocumentHistory` ohne Anwender- und Lizenzdaten:** `PersonInfo` trägt Programmname und Fassung; der einzige Zeitstempel steht in `CreatedBy/@date` | Datenaustauschkonzept 5.2 |
| 5 | **Zahlen ohne Exponent:** Viele Größen sind im Schema `xsd:decimal`; die rundlaufende Form wird ziffernerhaltend ausgeschrieben | Datenaustauschkonzept 5.2 |
| 6 | **Ein Leseweg im Kern:** `GebaeudeExportSatz.Lesen` ruft nur die Controller des Laufs; die Hülle ruft ihn, Probe 1b läuft darüber. Ein leerer Zonenwert ist der des Gebäudes; innere Gewinne der Zone sind ihr Anteil nach dem Flächenschlüssel, Personen die Bewohner oder Fläche ÷ Fläche je Nutzer, der Luftwechsel die Infiltration | Softwarearchitektur 1.6, 4.6 |
| 7 | **Ersatzschichtung, Sonderfälle:** Innengruppe ohne U mit λ = 1,0 W/(mK); bei R ≤ 0 ein masseloser Stoff mit R = 0,001, das U trägt die Konstruktion; an einer Bandgrenze liegen λ und ρ auf dem Band; eine Ersatzfläche innerer Masse steht auch auf dem Bauteilweg, wenn Innenbauteile fehlen; eine Tür ohne Schichten in einer Gruppe mit Schichten wird masselos, ihr U steht an der Öffnung; Trennflächen ohne Schichten masselos, bis G6b ihre Gruppe entscheidet | Datenaustauschkonzept 5.3 |
| 8 | **Ruhende Luftschicht als Dicke und Widerstand** nach Tabelle 8 (Kandidat (i)); sie kehrt mit dem Namensabgleich vollständig zurück, ohne ihn masselos | Datenaustauschkonzept 5.3 |
| 9 | **Rechteck:** Senkrechte Flächen haben die Raumhöhe als Höhe, alle übrigen sind quadratisch; G7b ersetzt das durch die Geometrie des Zonengeometrie-Modells | Datenaustauschkonzept 5.2 |
| 10 | **Die Verlustliste** ist per Reflexion vollständig über die Modelle von Gebäude, Zone und Bauteil; gemessen im Rundlauf sind die Verluste, die das Probegebäude trägt. Der Name des Partners eines Innenpaars geht verloren | Datenaustauschkonzept 9 |
| 11 | **Oberfläche:** Die Bestätigung „Meldungen gelesen" setzt nur der Anwender, der Hilfe-Assistent liest sie; eine Ablehnung trägt nur „Schließen"; der Hinweis „gespeicherter Stand" gilt einer Zeile mit geänderter Fläche und Verbrauch; Hilfeschlüssel ohne Anker bis G7b; der Maskenname `GebaeudeExport` steht auch in `KiChatKontext` (Bereich Gebäude) | Softwarearchitektur 3.2, 3.8 |

**Was offen bleibt.** Den Freigabeschalter `GebaeudeExportRegeln.GbxmlExportFreigegeben` vor jeder
Auslieferung ohne G7b ausschalten (E48/F1) — zusammen mit `GebaeudeZonenregeln.MehrereZonenFreigegeben`
(E46/A1); Wiki und Logbuch mit G7b; die Windows-Sichtabnahme von Knopf und Exportdialog.

**Betroffene Stufen:** G7a; G7b (Rechteck, Einstieg im Bedarfsdialog, Hilfe-Anker); G6b (Trennflächen).

**Nachgezogen:** [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Abschnitte 1 und 2;
[Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) D2 und D17;
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) Kopf, 2.1, 5.2, 5.3, 5.4, 9 und 10;
[Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 1.5, 1.6 und 4.6; das
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G7a_gbXML-Export.md); die Indexzeilen in
[`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.55 Entscheid E49 — G6b: Anhang A nach Empfehlung, Kriterien der Proben 3, 4 und 5b, erhaltende Kopplung V0

**Anlass.** Der Auftrag der Stufe G6b (Zoneneingabe und Rechenweg des Mehrzonenmodells) stellte acht
Fragen (Anhang A, darunter die im Register vor G6b fälligen M3, M5 und M6); die Proben der Welle W4
brachten drei weitere zur Entscheidung — das Band von Probe 3, das Kriterium von Probe 5b und die
Frage nach einer erhaltenden Kopplung zum Befund von Probe 4.

**Entscheid E49 (Anwender, 26.09.2026) — Anhang A, alle nach Empfehlung:**

| # | Frage | Entscheid |
|---|---|---|
| A1 = M3 | Gilt die 4-K-Regel fest oder je Trennfläche übersteuerbar? | **(b) Vorgabe mit Übersteuerung je Trennfläche:** Spalte `Tab_Bauteil.Trennflaeche_Zuordnung` (`IW`/`AW`, NULL = 4-K-Regel) mit Schemaschritt S-G (147); Δϑ aus dem adiabaten Vorlauf als Beleg, eine Überschreitung im gekoppelten Lauf wird benannt. N1.46 Nr. 11 (`UNBEHEIZT` ohne Zone rechnet mit der Kellertemperatur) bleibt |
| A2 = M5 | Bekommen unbeheizte Zonen eigene Zeilen im Bedarfsdialog? | **(a) ja:** ohne Heizwärme und Last, mit Temperatur und Überhitzungsstunden |
| A3 = M6 | Vorlauf? | **(a) 30 Tage mit Probe**, Verlängerung auf 90 Tage benannt (über 0,05 K), nur ab zwei Zonen; bei einer Zone bleibt der Vorlauf der Einzonenrechnung |
| A4 | Anlagenseite und Kühlung bei mehreren Zonen? | **(a)** Heizung und Kühlung je Zone ideal; die Kühlwerte kommen vom Gebäude, die Grenze anteilig, `Tab_Zone.Kuehl_*` bleiben ungelesen; bei wirksamem AK1 rechnet das Gebäude ideal und geht als feste Last in die Anlage, mit Protokollwarnung |
| A5 | Gelten die Zonenwerte auch bei einer Zone? | **(a) ja** — die Vorgabenkaskade Zone, sonst Gebäude, gilt für jede Zone; der Fall „Volumen und Raumhöhe" des Netzes W0 ändert sich benannt |
| A6 | Wird das Ergebnis je Zone gespeichert? | **ja:** `Tab_ErgebnisZone`, nur Skalare, mit S-G; der Bericht liest nur Gespeichertes (E30) |
| A7 | Übergang an der Trennfläche? | **Messentscheid (b):** wie am unbeheizten Raum (`Nachbaruebergang.WieUnbeheizt`) — Testbeispiel 10 mit der Trennfläche höchstens 0,0883 K außerhalb des Bands, nur konvektiv (a) 2,9748 K |
| A8 | Probe 5? | **(a) geteilt:** 5a gegen den Fixpunkt der Iteration, 5b gegen das exakt diskretisierte 4×4-System |

**Zu den Proben (Anwender, 26.09.2026):**

- **Probe 3:** Die Jahresenergie liegt im **Band 3 %** (AW gegen IW +1,8 % stationär, +2,4 % im
  Jahresgang) — eine modellbedingte Abweichung, benannt.
- **Probe 5b:** Das Kriterium ist **< 0,001 K** gegen das 4×4-System (gemessen höchstens 1,5·10⁻⁴ K).
- **Probe 4 — erhaltende Kopplung** ([Entwurf](../ueberholt/Entwurf_erhaltende_Zonenkopplung_G6b.md)):
  **K1 = V0** — der Rechenweg bleibt, die Freischaltung folgt ohne Umbau; **K2** — Probe 4 (d) wird
  gemessen und danach benannt; **K3** — verlangt die Messung eine erhaltende Kopplung, kommt V4 als
  eigene Stufe nach G6b. Gemessen: (c) stationäre Erhaltung 5·10⁻¹⁶, (d) gegen die wandaufgelöste
  Referenz +0,044 %, Anteil der Dynamik höchstens 7·10⁻⁶. Benannte Kriterien: **(c) < 0,1 %,
  (d) gesamt < 0,1 %, Dynamik < 0,01 %.** Eine erhaltende Kopplung ist nicht verlangt, V4 entfällt;
  die Zuordnung des Nachbarglieds (−6,7 % über das Gebäude) führt Probe 4 nur als benannte Information.

**Betroffene Stufen:** G6b (umgesetzt in sechs Wellen, N1.56); G6c (M12 mit der Messung aus G6b);
AK2 und AK3 (Übergabe je Zone); KU3 (Kühlspalten je Zone).

### N1.56 Festlegungen der Umsetzung G6b — benannt, nicht entschieden

**Anlass.** Die Stufe G6b ist in sechs Wellen gebaut (W0 bis W5; 26.09.2026;
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G6b_Mehrzonenrechnung.md)). Die Festlegungen
1–12 standen im Auftrag, die übrigen hat die Umsetzung getroffen, wo Auftrag und Papiere schwiegen; der
Orchestrator hat jede Welle abgenommen. Die Liste nennt sie, damit sie nicht als Anwenderentscheide
gelesen werden; Widerspruch ist möglich und würde ein eigener Entscheid.

| # | Festlegung | Wo |
|---|---|---|
| 1 | **Die Nachtzeit kommt vom Gebäude** — `Tab_Zone` hat keine (N1.51, Vermerk) | Mehrzonenkonzept 2.6 |
| 2 | **`IstBeheizt = 0` heißt frei schwingend:** kein Heizen, kein Kühlen, kein Sollwert; ein Gebäude ohne beheizte Zone wird benannt abgelehnt | Mehrzonenkonzept 2.5 |
| 3 | **`UNBEHEIZT` ohne Zone** bleibt bei der Kellertemperatur (N1.46 Nr. 11); die gerechnete Alternative ist die Randbedingung `ZONE` zu einer unbeheizten Zone (A1) | Mehrzonenkonzept 2.2 |
| 4 | **`ZONE` gilt für dieselben Bauteilarten wie `UNBEHEIZT`**, auch für Fenster und Tür | Mehrzonenkonzept 4.2 |
| 5 | **Leistungsgrenzen:** Übernimmt eine Zone die Grenze vom Gebäude, wird sie erst ab zwei Zonen nach der Fläche anteilig verteilt; bei einer Zone bleibt es beim Stand G3 | Mehrzonenkonzept 2.6 |
| 6 | **Abbruch der Iteration** ab dem zweiten Durchlauf: alle Δθ̄_air < 0,01 K **und** alle ΔΦ_h, ΔΦ_c < 0,1 W — Φ_c ergänzt ADR-005 Nr. 2; höchstens 50 Durchläufe, sonst `ZonenkopplungKonvergiertNicht` mit Gebäude, Stunde, Zonen, letzter Änderung und Luftaustausch | ADR-005 |
| 7 | **Startwert einer unbeheizten Zone:** das Mittel von θ_eq über die Vorlaufstunden; beheizte Zonen beginnen am Sollwert der Startstunde | Mehrzonenkonzept 2.9 |
| 8 | **Zone löschen, auf die Trennflächen zeigen:** Die Rückfrage nennt diese Bauteile und die entfallenden Luftströme; „Ja" setzt die Bauteile auf `UNBEHEIZT` ohne Nachbar, sichtbar im Arbeitsstand | Mehrzonenkonzept 5.1 |
| 9 | **Zone duplizieren:** Die Kopie übernimmt ihre eigenen Trennflächen mit unverändertem Nachbarn, der Editor fragt; die Paarprüfung fängt eine Doppelung ab | Mehrzonenkonzept 5.1 |
| 10 | **Gebäudekennzahlen bei Zonen:** Heizlast Σ max(Φ_h,z, 0), Kühlbedarf Σ getrennt (E31); Raumluft, operative Temperatur, Heizsollwert und θ_max flächengewichtet über die beheizten Zonen; Überhitzungs-, Kühl-, Sommerlüftungs- und Umschaltstunden sowie „gleichzeitig heizen und kühlen" als Stunden, in denen mindestens eine beheizte Zone den Fall erfüllt — die Überhitzung gegen `Maximaleraumtemperatur` (Rechenschritte 8.2, E32) | Rechenschritte 8.2 |
| 11 | **Skalierung:** Ein Gebäude mit Zonen trägt seine echte Hülle, der Faktor ist 1 (E40); Zonen- und Gebäudewerte gehen ohne Faktor auf | Rechenschritte 8.3 |
| 12 | **Ein Kopplungsfehler bricht den ganzen Bedarfslauf ab** (benannter Fehler, `false` bis in die Gebäudeschleife) | Mehrzonenkonzept 2.4 |
| 13 | **Eine Grenze für Pflege und Lauf:** `GebaeudeZonenregeln.PFLEGEGRENZE` (50); Laufgrenze und Freigabeschalter (E46/A1) sind gestrichen. Der Einzonenweg nimmt höchstens eine Zone; zwei und mehr lehnt er benannt ab — erreichbar nur über die Übergabe- und Kühlauskunft des Gebäudedialogs (A4) und jenseits der Grenze | Mehrzonenkonzept 5.3 |
| 14 | **Ab zwei Zonen ist die Nutzfläche auch im Lauf Pflicht** (`PflichtgroesseFehlt`); die Bezugsfläche des Gebäudes ist die Σ Nutzfläche der beheizten Zonen | Mehrzonenkonzept 4.2 |
| 15 | **Teilgruppen** über koppelnde Trennflächen (Außengruppe) und Luftströme; Reihenfolge nach Rang und Kennung, Start ab θ̄ der Vorstunde; die Sommerlüftungsregel je Zone einmal je Stunde vor den Durchläufen; das Muster des ersten Durchlaufs wird gehalten und gezählt | ADR-005; Mehrzonenkonzept 2.4 |
| 16 | **4-K-Regel:** Der adiabate Vorlauf rechnet nur beheizte Paare ohne ausdrückliche Zuordnung; eine unbeheizte Nachbarzone koppelt immer über die Außengruppe | Mehrzonenkonzept 2.2 |
| 17 | **Nachbarübergang** wie am unbeheizten Raum (A7) als Festwert `GebaeudeFestwerte.NACHBARUEBERGANG` | Mehrzonenkonzept 2.3 |
| 18 | **`Tab_ErgebnisZone`** trägt neben Heizwärme, Spitze, Kühlenergie, mittlerer Raumtemperatur und Überhitzungsstunden drei Befunde der Schleife: Δϑ_max zu einer Nachbarzone, Höchstzahl der Durchläufe, Stunden mit gehaltenem Muster; NULL heißt „nicht gerechnet" | Mehrzonenkonzept 4.2, 7 |
| 19 | **Bedarfsdialog:** die Gruppe „Zonen" ab zwei Zonen und die Wahl „Diagramme für:" (Wärmelast und Raumtemperatur je Zone, eigene Kennung und Zwischenspeicher je Bild); eine unbeheizte Zone nennt statt des Lastbilds ihren Grund; der Assistent führt die Wahl als Katalogfeld `diagramm` | Mehrzonenkonzept 7 |
| 20 | **Bericht:** Die Zonentabelle nimmt „beheizt", Heizwärme und Spitze nur mit Zonenzeilen des Laufs auf; die Summenzeile addiert die Heizwärme, nicht die Spitzen | Mehrzonenkonzept 7 |
| 21 | **Export:** `Geb[n].Zone[k].*` nur ab zwei Zonen, ohne Reihen und ohne die Befunde der Schleife; Energie nur für eine beheizte Zone, Kühlenergie nur bei wirksamer Kühlung, Δϑ_max nur mit Nachbarzone | Umsetzungskonzept 1.8 |

**Messung am echten Gebäude (Grundlage für M12).** Ein G4b-Import, von Hand in Wohnungen und ein
unbeheiztes Treppenhaus geteilt: 50 Zonen rechnen in rund 0,55 s je Gebäude und Jahr (11 ms je Zone),
Durchläufe im Mittel 2,0, höchstens 3; die Heizwärme ist ab vier Wohnungen von der Teilung
unabhängig (−0,09 %). Einzelheiten im Protokoll, Abschnitt 3.

**Was offen bleibt.** Die Windows-Sichtabnahme (Zonenreiter mit Werten, Trennflächen und
Luftaustausch; Bedarfsdialog mit Zonen und Diagrammwahl; Bericht); der Wiki-Upload der Seite
„Mehrzonenmodell" samt Nachzügen und Logbuch-Satz. Der Vermerk in N1.54 zum Freigabeschalter
`GebaeudeZonenregeln.MehrereZonenFreigegeben` ist gegenstandslos — der Schalter ist gestrichen.

**Betroffene Stufen:** G6b; G6c (M7, M8, M12, M13), G6d (M11); AK2, AK3; KU3.

**Nachgezogen:** [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Abschnitte 1, 2 und 3;
[Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kopf, Kapitel 0 und M3, M5, M6;
[Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Kopf, 2.9, 7, 8.1, 9 und 10;
[ADR-005](ADR-005_Zonenkopplung_Mehrzonenmodell.md) Aufgaben;
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 7.2 und 8.2;
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) Stufe KU3;
[Anlagenkopplungskonzept](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 6.5;
[Hilfesystem](Konzept_Hilfesystem_Wikidokumentation.md) Seitentabelle;
[`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md) Aktuelle Basis; das
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G6b_Mehrzonenrechnung.md); der Entwurf der
erhaltenden Kopplung nach `ueberholt/`; die Indexzeilen in [`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.57 Entscheid E50 — G6c beauftragt; M7, M8, M12 und M13 nach Empfehlung

**Anlass.** G4b ist samt Namensabgleich abgeschlossen (N1.49); ein importiertes Gebäude kommt bisher als
**eine** Zone ins Projekt (Regel Z5 bzw. X4). Der nächste Schritt des Importwegs ist der Zonenimport G6c.
Vor G6c fällig waren nach dem [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) M7, M8, M12 und
M13; M7 und M13 legen den Umfang der Stufe fest, M8 und M12 die Zahl der Zonen, die der Import vorschlägt.

**Auftrag (Anwender, 26.09.2026, im Wortlaut):**

> „Kleine Nacharbeiten an G4b, dann G6c, Zonenimport mit mehreren Zonen aus IFC/gbXML, U-Wert-Vorgaben für
> Neubauten ab 2021"

Der letzte Teil des Auftrags ist E51 (N1.58).

**Entscheid E50 (Anwender, 26.09.2026):**

| # | Frage | Entscheid |
|---|---|---|
| Auftrag | Wird G6c jetzt gebaut? | **ja** — nach den kleinen Nacharbeiten an G4b; G6c baut den Zonenimport mit mehreren Zonen aus IFC und gbXML (D16) |
| M7 | Welche Zonenregel ist beim Import die Vorgabe? | **(a) je Geschoss, Rückfall auf die gröbste Regel** (eine Zone), wenn die Raumgrenzen fehlen — nach Empfehlung ([Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.1, 6.5) |
| M13 | Wie weit geht die Rekonstruktion der Nachbarschaften, wenn ein Autorensystem keine Raumgrenzenpaare schreibt? | **(a) vollständig** — Paarbildung über die Geometrie; alle gemessenen Dateien sind nutzbar, auch die kleine lizenzfreie Referenzdatei; rund 2–3 PT mehr als die magere Fassung — nach Empfehlung (Mehrzonenkonzept 6.2) |
| M8 | Gilt eine Mindestgröße je Zone, und was geschieht mit einer zu kleinen Zone? | **(a) Mindestgröße max(2 m², 2 % der Gebäudegrundfläche)** mit Zuschlag zum Nachbarn mit der größten gemeinsamen Grenzfläche — nach Empfehlung (Mehrzonenkonzept 6.1) |
| M12 | Gilt eine Obergrenze von 50 Zonen je Gebäude, und wie hart? | **(a) 50 Zonen als Vorgabe;** der Import warnt mit Rückfrage und schlägt eine gröbere Regel vor (auf Geschosse zusammenlegen), die Rechnung lehnt darüber benannt ab — nach Empfehlung (Mehrzonenkonzept 2.9, 6.6) |

**Was damit gilt.**

- **Zonenvorschlag:** Beim IFC-Import ist **Z4 (je Geschoss)** vorbelegt, sofern mehr als ein Geschoss Räume
  trägt, sonst Z5; ohne Raumgrenzen ist **Z5** die Vorgabe und Z4 nur wählbar, wenn der Anwender die
  Trenndecke selbst einträgt (Mehrzonenkonzept 6.5). Beim gbXML-Import entspricht Z4 die Regel **X2**; die
  Regelkette des [Datenaustauschkonzepts](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.3 bleibt stehen
  (X1 nur bei einer echten Zonengliederung der Datei, sonst X2, sonst X4). Wer X1 anders will, entscheidet
  eigens.
- **Nachbarschaften:** Die Rekonstruktion folgt Mehrzonenkonzept 6.2 — Kandidaten über dasselbe Bauteil,
  eindeutig bei genau einem Kandidaten, sonst Geometrie in Weltkoordinaten (Flächeninhalt innerhalb 1 %,
  Schwerpunktabstand kleiner als die Bauteildicke), sonst „unbekannt" mit Randbedingung `UNBEHEIZT` und
  roter Zeile; die Trennflächenbilanz A→B gegen B→A meldet ab 2 % (6.6). Probe 18 (Autorensystem ohne
  Paare) gehört zur Abnahme.
- **Mindestgröße und Obergrenze:** Eine Zone unter max(2 m², 2 % der Gebäudegrundfläche) wird dem Nachbarn
  mit der größten gemeinsamen Grenzfläche zugeschlagen; ohne Nachbarn bleibt sie stehen und der Dialog warnt
  (Mehrzonenkonzept 6.1, Fehlerbild „zu klein"). Über **50 Zonen** warnt der Import mit Rückfrage und
  schlägt vor, auf Geschosse zusammenzulegen (6.6); die Rechnung lehnt mehr als 50 Zonen benannt ab (2.9).
  Die Konstante aus E46/A3 (`GebaeudeZonenregeln.PFLEGEGRENZE`) ist damit die Vorgabe; seit G6b ist sie die
  eine Grenze für Pflege und Lauf (N1.56 Nr. 13). Die Laufzeitmessung aus G6b (N1.56: 50 Zonen in rund
  0,55 s je Gebäude und Jahr) stützt die Zahl; verlangt eine spätere Messung eine andere, ist das ein eigener
  Entscheid.
- **Aufwand:** Die Spanne 16–26 PT der Stufe G6c (Mehrzonenkonzept 9) rechnet mit der Empfehlung, also mit
  der vollständigen Rekonstruktion; die gbXML-Regeln X1…X3 sind darin weiterhin nicht enthalten und werden
  mit dem Wellenplan beziffert.

**Was offen bleibt.** Vor G6c ist kein Anwenderentscheid mehr offen; M3, M5 und M6 hat E49 (N1.55)
entschieden, vor G6d bleibt M11. Das Register zählt **1 offenen Punkt**. Vor G6c zu messen bleiben
`IfcSpatialZone` und `ParentBoundary` (Mehrzonenkonzept 6.1, 6.2); vor dem ersten Commit der großen
Testdatei ist nach M10 ihre Lizenz nachzufragen. Die Freischaltung mehrerer Zonen, an der G6c hängt, hat
G6b gebracht (N1.56 Nr. 13).

**Betroffene Stufen:** G6c (beauftragt, noch nicht begonnen); G6b (Grenze und Freischaltung, N1.56); G4c
und G4a (Importweg, Zuordnungsdialog).

**Nachgezogen:** [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kopf, Lesehinweis, Kapitel 0,
3 (M7, M8, M12, M13) und 9; [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Kopf und Abschnitte 1 (E50),
2 (G6c) und 3; [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Kopf, 2.9, 6.1, 6.2, 6.5, 6.6
und 10; die
[Übergabe](Gebaeudesimulation/2026-09-26_Uebergabe_G6c_Katalog_M_A.md) samt Indexzeile in
[`Dokumentation/LIESMICH.md`](../LIESMICH.md).

### N1.58 Entscheid E51 — Klassen ohne Katalogsatz: freie Werte nach Stein/Loga (2025) und eigene Katalogsätze; E27 geändert

**Anlass.** Der letzte Teil des Auftrags vom 26.09.2026 („U-Wert-Vorgaben für Neubauten ab 2021", N1.57).
Nach E47 (N1.52) haben die Klassen **M** (ab 2021) und **A** (bis 1859) keine Katalogsätze; nach F4 des
[Konzepts Baualtersklassen](Konzept_Baualtersklassen_Energiestandard_EPOS-Plan.md) („leer lassen, Katalog
später ergänzen") liefern sie keine U-, g- und ψ-Vorgaben. Der Bauteilvorschlag (G4b) lehnt deshalb einen
importierten Neubau ohne U-Werte und ohne Schichten benannt ab (opakes Bauteil ohne U-Wert und ohne
Vorgabe).

**Entscheid E51 (Anwender, 26.09.2026): beides — freie Werte übernehmen und den Katalog ergänzen.** F4 des
Konzepts Baualtersklassen ist aufgehoben, **E27 wird geändert** (U12).

1. **Katalog ergänzen.** Die Klassen M und A bekommen eigene Sätze im Auslieferungskatalog, mit neutralen
   Namen und ohne Produktdaten — etwa ein Neubau nach dem Mindeststandard des Gebäudeenergiegesetzes (GEG)
   und einer nach Effizienzhaus 55 für M, typische Altbauten für A.
2. **Vorrang.** Hat eine Klasse oder ein Energiestandard eigene Katalogsätze, gilt weiter deren Median —
   insoweit bleibt E27. **Nur ohne Katalogsatz** gilt der **freie Wert** aus Stein/Loga (2025), sichtbar mit
   Herkunft und Beleg in der Feldzeile und in der Meldung, nie still.
3. **Quelle.** Stein, B.; Loga, T. (2025): *Das Typgebäude-Modell zur energetischen Bewertung des
   Wohngebäudebestands*, Institut Wohnen und Umwelt (IWU) im Auftrag des BBSR, Zenodo, Record 15488271,
   Lizenz CC BY 4.0. Die Quellenangabe gehört ins Programm (Herleitungszeile), ins Wiki und in die
   Lizenzhinweise (`Setup/Vorlage/Lizenzhinweise.txt`). Die IWU-Wohngebäudetypologie 2015 bleibt unfrei; von
   ihr stammen weiter nur die Jahresgrenzen der Klassen.
4. **Die PDF der Quelle** (4,38 MB) darf geladen werden — nur lokal, nie ins Repositorium. Ob sie die Klasse A
   abdeckt, klärt die Umsetzung.

**Was damit gilt.** U12 (E27: Vorgaben aus dem eigenen EPOS-Gebäudekatalog, „leer lassen" als Rückfall)
gilt nur noch, soweit Katalogsätze vorhanden sind; eine Klasse oder ein Standard ohne Satz liefert künftig
den freien Wert mit Herkunft und Beleg statt einer leeren Vorgabe. Ein Wert der Nachbarklasse wird weiter
nie geliehen. Bis zur Umsetzung gilt der Stand nach E47: A und M liefern keine Vorgabe, die Meldung nennt es.

**Was offen bleibt — mit der Umsetzung zu klären:**

1. **Reichweite der Quelle:** welche Klassen, Bauteile und Größen (U-Werte, g-Wert, ψ) Stein/Loga (2025)
   liefert, ob Klasse A enthalten ist und wie die Werte eines Wohngebäudemodells für Nichtwohngebäude gelten.
2. **Kennwerte der eigenen Sätze:** für M mit Fundstelle im GEG (Anlage 1 Referenzgebäude Wohngebäude bzw.
   Anlage 2 Nichtwohngebäude, Fassung belegen) und in den technischen Mindestanforderungen der
   Bundesförderung für effiziente Gebäude (Effizienzhaus 55); für A aus der freien Quelle. Der Entwurf wird
   beim Anwender bestätigt.
3. **Weg in die Auslieferung:** Saat per Schemaschritt mit festen Ids und `ReadOnly = 1`, der nur anlegt, was
   fehlt — nächster freier Schritt heute **149**, die Nummer wird spät geprüft —, oder Pflege in der
   produktiven Datenbank vor der Auslieferungsvorlage. Die Mediane rechnet `GebaeudeVorgabenTests` aus der
   Testdatenbank nach; die Sätze müssen deshalb auch dort stehen.
4. **`GebaeudeVorgaben`:** Vorrang Standard → Klasse → freier Wert, Herkunft und Beleg des freien Werts
   (Herleitungszeile mit Quellenangabe); ob die neuen Sätze einen Energiestandard tragen, entscheidet mit, ob
   auch dessen Zeile eine Vorgabe aus dem Katalog bekommt.
5. **Referenzlauf byte-gleich:** Kein Referenzprojekt nutzt die neuen Sätze, kein Rechenweg liest Klasse oder
   Vorgabe. Die Einfrierregel „gesäte Gebäudedaten" nennt `Tab_Gebaeude(_STAMM)`; ob neue, von keinem
   Referenzprojekt genutzte Katalogsätze unter sie fallen, ist mit der Umsetzung zu prüfen und in
   `Referenzlaeufe/LIESMICH.md` zu vermerken.

E51 berührt keinen offenen Registerpunkt; U12 trägt den Vermerk der Änderung, das Register zählt weiter
**1 offenen Punkt**.

**Betroffene Stufen:** G4 (Import, Vorgaben je Klasse), G4b (Bauteilvorschlag), Gebäudekatalog,
Auslieferungsvorlage und Lizenzhinweise.

**Nachgezogen:** [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Kopf und Abschnitte 1 (E51), 2 (G4) und 3;
[Konzept Baualtersklassen](Konzept_Baualtersklassen_Energiestandard_EPOS-Plan.md) Kopf, 4 und 8 (F4);
[Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kopf und U12 (Kapitel 0 und 2); die
[Übergabe](Gebaeudesimulation/2026-09-26_Uebergabe_G6c_Katalog_M_A.md). Umsetzungskonzept 5 (U12) und die
übrigen Stellen, die U12 zitieren, zieht die Umsetzung nach.
