# Befund O — Mehrzonenmodell: Zonenkopplung nach VDI 6007 Blatt 1 und VDI 2078 (15.09.2026)

**Protokoll.** Befund O eines Erkundungs-Agenten (Modell Opus, nur lesend) im Auftrag des Konzepts [`../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md), Sitzung vom 15.09.2026.

Quellen: VDI 6007 Blatt 1:2015-06 (Raummodell), VDI 6007 Blatt 2:2012-03, Blatt 3:2015-06,
VDI 2078:2015. Zitiert werden ausschließlich Seiten-, Bild- und Gleichungsnummern; Wortlaut und
Zahlenreihen der Richtlinien bleiben draußen. VDI 6020:2022 wird nach Entscheid E6 (Konzept
N1.11) **nicht** herangezogen — keine Aussage dieses Befunds stützt sich darauf. Jede Aussage
über den Quelltext trägt Datei und Zeile.

---

## 0. Das Ergebnis in neun Sätzen

1. **Die Richtlinie kennt keine Zonenkopplung.** VDI 6007 Blatt 1, Abschnitt 5.3 (S. 7) sagt
   ausdrücklich, dass eine Zonierung innerhalb eines Gebäudes zur Zusammenfassung von Räumen
   nicht Gegenstand der Richtlinie ist; VDI 2078, Abschnitt 6.2 (S. 21) wiederholt das und grenzt
   sich dabei ausdrücklich gegen DIN V 18599 ab.
2. Was die Richtlinie stattdessen regelt, ist die **Randbedingung** eines einzelnen Raums zu einem
   anders temperierten Nachbarraum: dessen Bauteile gehören in die **AW-Gruppe** (Blatt 1, S. 15
   und Gl. (27), S. 17), die Nachbarraumtemperatur geht als äquivalente Temperatur θ_NR,eq nach
   **Gl. (40)** (S. 20) ein und wird in θ_A,eq,gew nach **Gl. (41)/(42)** (S. 20) mit U·A gewichtet.
3. **θ_NR ist eine Aktionsgröße, keine Reaktionsgröße:** Blatt 1 führt sie unter den inneren
   Wärmequellen (6.2, S. 9) und gibt sie in den Testbeispielen als **Stundenprofil** vor
   (Tabelle A10.2, S. 58); Testbeispiel 10 heißt im Text „Nebenraum ist ein Keller mit
   vorgegebener Temperatur" (S. 36). Damit ist die Frage „innerhalb der Stunde oder mit der
   Vorstunde" von der Richtlinie **nicht entschieden** — sie stellt sich dort nicht.
4. Geliefert wird die **Luft**temperatur des Nachbarraums (θ_NR,Lu in Gl. (40)), nicht dessen
   Oberflächentemperatur; der Aufschlag auf θ_NR,eq entsteht allein aus strahlenden Quellen auf
   der Nachbarraumseite der Wand über den **konvektiven** Übergangskoeffizienten α_kon,A;NR.
5. **VDI 2078, 7.2 (S. 46)** erlaubt, Innenbauteile zu Räumen mit ähnlichen Bedingungen
   (Δϑ < 4 K) in der Regel wie symmetrisch beaufschlagte, adiabate Innenbauteile zu behandeln —
   das ist die Regel, die ein Mehrzonenmodell klein hält.
6. **Testbeispiel 10 ist der Prüfstein:** ein einziges nicht adiabates Innenbauteil (Fußboden FB1,
   17,5 m², Keller 15 °C) verdoppelt den Verlustleitwert des Testraums; die Lufttemperatur am
   60. Tag liegt rund 19 K unter der des sonst gleichen Testbeispiels 5 (Tabellen A5.3 und A10.3,
   S. 55 und 58/59). Ein Kopplungsweg, der diesen Fall trifft, ist belegt.
7. **Vorschlag für EPOS-Plan:** N Zonen, je Zone das validierte Einzonenmodell des Konzepts (4.2),
   AW-Gruppe = Außenbauteile **plus** Bauteile zu Zonen anderer Temperatur, IW-Gruppe = adiabate
   Bauteile; Kopplung über θ_NR,eq mit **Gauß-Seidel-Iteration innerhalb der Stunde**
   (Vorschlag B) bis unter 0,01 K, fester Durchlaufreihenfolge und benanntem Fehler bei
   Nichtkonvergenz.
8. **Unbeheizte Zonen** (Keller, Treppenhaus, Dachraum) sind Zonen ohne Heizung. Damit löst sich
   die Randbedingung `KELLER` des Einzonenmodells (Konzept 4.4) physikalisch auf — die
   Randbedingung **Erdreich** bleibt, sie rutscht nur eine Ebene tiefer an die Bodenplatte der
   Kellerzone.
9. **Ein Mehrzonenmodell ist eine EPOS-Erweiterung außerhalb der Richtlinie**, genau wie das
   Kusuda-Erdreichmodell (Konzept N1.3). Die Validierungsaussage bleibt „Rechenkern nach VDI 6007
   Blatt 1" **je Zone**; die Kopplung wird durch Testbeispiel 10 und interne Konsistenzproben
   belegt, nicht durch die Richtlinie.

---

## 1. Was die Richtlinie über Räume, Zonen und Nachbarräume sagt

### 1.1 Die drei Begriffe

| Begriff | Fundstelle | Inhalt |
|---|---|---|
| **Raum (Bilanzbereich)** | Blatt 1, 6.2, S. 10 | Voraussetzung ist eine **homogene** Raumlufttemperatur, auch über die Raumhöhe; die für den Bilanzbereich angegebenen Quellen und Senken werden dort wirksam. Sind diese Voraussetzungen nicht erfüllt, sind die Bilanzgrenzen so zu legen, dass sie für Teilbereiche (Zonen) gelten — mit Verweis auf 5.3 |
| **Zone (Aufteilung)** | Blatt 1, 5.3, S. 7; VDI 2078, 6.2, S. 21 | Aufteilung **innerhalb eines Raums**, wenn der Raum groß ist, die Bereiche sehr unterschiedliche Lasten tragen und kein thermischer Ausgleich stattfindet; bei hohen Räumen (Atrien, Hallen) auch vertikal. **Eine Zonierung innerhalb eines Gebäudes zur Zusammenfassung von Räumen ist nicht Gegenstand der Richtlinie** — VDI 2078 setzt das ausdrücklich von DIN V 18599 ab |
| **Nachbarraum / Nebenraum** | Blatt 1, 2 (Begriffe), S. 4; 6.4, S. 15 | Ein Bauteil mit **gleichen** Randbedingungen auf beiden Seiten ist eine Innenwand (adiabates Wandverhalten); ein Bauteil mit **unterschiedlichen** Randbedingungen ist eine Außenwand — die Richtlinie nennt als Beispiel ausdrücklich die Kellerdecke (S. 4). Nachbarräume mit anderer Temperatur sind Keller, Dachräume usw. (S. 15) |

**Die Beschreibung von Gebäude und Räumen** verweist Blatt 1 in 5.1 und 5.2 (S. 7) weiter an
VDI 2078, Abschnitt „Gebäude". Dort steht (VDI 2078, 6.1, S. 18): die Berechnung wird in der Regel
nur für **einen Raum oder für Raumgruppen** durchgeführt, die Lage eines Raums in der
Gebäudestruktur wird festgelegt, und die Bauteilgeometrie entsteht aus Bauteilen gleicher
Eigenschaft **unter Berücksichtigung der thermischen Randbedingungen (z. B. unterschiedliche
Raumtemperaturen bei Innenbauteilen)**. Die Summe dieser Angaben heißt dort „thermisches
Gebäudemodell". Das ist die Datenstruktur, die ein Mehrzonenmodell braucht — die Rechenvorschrift
zur Kopplung steht dort aber nicht.

**Nebenbefund für Entscheid E6.** Die Bemaßungsregel, die N1.9 aus VDI 6020 gezogen und N1.11
als „Arbeitsannahme ohne Normzitat" zurückgestuft hat, steht **auch in VDI 2078, 6.1, S. 18** und
ist damit für das Produkt belegbar: Innenbauteile mit Nettomaßen (raumseitig sichtbare Flächen,
weil der Austausch über die Oberfläche läuft), Außenbauteile mit Bruttomaßen (Rücksicht auf die
Wärmebedarfsberechnung, damit die Hüllfläche stimmt — gilt auch für erdberührte Flächen),
**anders temperierte Nebenräume mit Nettoflächen**, Fenster immer einschließlich Rahmen
(Maueröffnung). Die letzte Regel ist für ein Mehrzonenmodell die wichtigste: die Trennfläche zur
Nachbarzone zählt netto, nicht brutto. Der Merkposten „Normzitat in G1 belegen" aus Konzept N1.11
und Befund N (Abbildungsregel `FlaecheAussenwand`) ist damit erledigt — Quelle ist VDI 2078,
Abschnitt 6.1, Seite 18.

### 1.2 Nicht adiabate Innenbauteile gehören in die AW-Gruppe

Blatt 1, 6.4, S. 15 (Anfang des Abschnitts „Thermisches Verhalten des Raums"):

- Alle Außenbauteile **und alle Innenbauteile zu anders temperierten Nachbarräumen** (Keller,
  Dachräume usw.) werden zu **einem asymmetrisch beaufschlagten Bauteil** zusammengefasst —
  Index **AW**.
- Alle Innenbauteile mit symmetrischer Beaufschlagung auf beiden Oberflächen werden zu **einem
  symmetrisch beaufschlagten Bauteil mit adiabatem Wandverhalten** zusammengefasst — Index **IW**.
- Für die AW-Gruppe werden die unterschiedlichen Außenlasten mit der äquivalenten
  Außentemperatur nach Gl. (32) (S. 18) zusammengefasst; **nach demselben Prinzip lässt sich auch
  eine äquivalente Außentemperatur für anders temperierte Nachbarräume berechnen** (S. 15).

Die Zugehörigkeit zieht sich durch die Formeln:

| Größe | Gleichung | Seite | Bedeutung für die Kopplung |
|---|---|---|---|
| R_ges,AW | (27) | 17 | Summe U·A **über alle** Außenwände und Außenfenster; der Text zu (27) nennt ausdrücklich „sowie nicht adiabate Innenbauteile" |
| R_Rest,AW | (28), Sonderfälle (28a)–(28c) | 17–18 | der verbleibende Widerstand am Massepfad; (28a) klemmt ihn auf den äußeren Übergangswiderstand, (28c) auf R_1,AW ≥ 10⁻¹⁰ |
| R_α;str;AW/IW | (29), Anpassung (31) | 18 | der Strahlungswiderstand zwischen Außen- und Innenflächen. **Ist die Fläche der zusammengefassten IW kleiner als die der zusammengefassten AW — einschließlich der Flächen zu Nachbarräumen mit anderen Temperaturen und/oder anderen Strahlungsbedingungen —, gilt Gl. (31) statt Gl. (29)** |
| α_str | (30) | 18 | 5,0 W/(m²K) innen, mit der Anmerkung α_ges > 5,0 W/(m²K) |

Gl. (31) ist für ein Mehrzonenmodell der unauffällige, aber scharfe Punkt: sobald eine Trennwand
von IW nach AW wandert, kann die AW-Fläche die IW-Fläche übersteigen, und der Strahlungswiderstand
ist dann über die **Innen**flächen zu bilden. Eine Umsetzung, die nur Gl. (29) kennt, rechnet
kleine Zonen mit großen Trennflächen falsch. Das Einzonenmodell des Konzepts (4.2) führt
R_rad mit einer festen „Bezugsfläche wie in den Testfällen" — das ist für das Mehrzonenmodell
durch die Fallunterscheidung (29)/(31) zu ersetzen.

### 1.3 θ_NR,eq — Gl. (40) — und die Gewichtung — Gl. (41)/(42)

Blatt 1, S. 19 unten: die Berechnung von θ_A,eq erfolgt **getrennt** für jede Außenfläche mit
unterschiedlicher Orientierung, für transparente Flächen **und für Trennwände zu anders
temperierten Nachbarräumen**.

**Gl. (40), S. 20** — die äquivalente Nachbarraumtemperatur entsteht „in Anlehnung an Gl. (32)"
durch zwei Vereinfachungen, die der Text vor der Gleichung nennt:

- θ_A,Lu wird durch **θ_NR,Lu** ersetzt — die **Lufttemperatur** des Nachbarraums;
- α_A wird durch **α_kon,A;NR** ersetzt — nur der **konvektive** Übergangskoeffizient auf der
  Nachbarraumseite, weil der langwellige Austausch mit Himmel und Erdboden entfällt;
- Q̇_str,A;NR ist die Summe der strahlenden Wärmequellen und -senken **auf der Nachbarraumseite**
  der Wand.

Damit ist θ_NR,eq = θ_NR,Lu + Q̇_str,A;NR · 1/(α_kon,A;NR · A_A,NR). Ohne strahlende Quellen auf der
Nachbarseite — der Regelfall und der Fall des Testbeispiels 10 — ist **θ_NR,eq = θ_NR,Lu**.

**Gl. (41), S. 20** — die gewichtete äquivalente Außentemperatur der zusammengefassten AW ist die
Summe dreier Teilsummen: über die opaken Außenwände (θ_A,eq,AWv · B_AWv), über die Außenfenster
(θ_A,eq,AFv · B_AFv) und **über die Nachbarraumflächen (θ_A,eq,NRv · B_NRv)**.

**Gl. (42), S. 20** — der Bewertungsfaktor des v-ten Bauteils ist B_v = U_v·A_v / Σ(U·A) über alle
p Bauteile der AW-Gruppe. Das ist exakt die U·A-Gewichtung, die Konzept 4.4 für θ_eq beschreibt —
mit dem Unterschied, dass die Summe im Mehrzonenfall **auch die Trennflächen** enthält.

**Folge für den Rechenweg:** θ_A,eq,gew einer Zone ist eine **affine Funktion** der
Lufttemperaturen aller Nachbarzonen, mit Koeffizienten B_NRv, die aus U·A folgen und über das Jahr
konstant sind. Das ist die Grundlage dafür, dass alle drei Kopplungswege in Abschnitt 3.2 sauber
formulierbar sind.

### 1.4 Reduktion einseitig belasteter Bauteile: Bild 2, C_1,korr, Gl. (17)

Blatt 1, 6.3, S. 14:

- Herrschen in angrenzenden Räumen **gleiche Temperaturverhältnisse**, darf man von symmetrischer
  Belastung ausgehen; das Ersatzmodell nach **Bild 1** (S. 11: R_1, R_2, R_3, C_1, C_2) reduziert
  sich dann auf **R_1 und C_1**.
- Bei **einseitiger** thermischer Belastung gilt die Vereinfachung nach **Bild 2** (S. 14), und die
  korrigierte Wärmespeicherkapazität folgt aus **Gl. (17)** aus R_W = R_1 + R_2 + R_3 (Gl. (16)),
  ω_BT und den Elementen a₁₂, a₂₂ der Gesamt-Kettenmatrix.
- Der Text zu Gl. (17) hält fest: bei Außenbauteilen **und Innenwänden unter asymmetrischer
  Belastung** muss außer der Speicherfunktion auch der **Wärmefluss durch** das Bauteil
  berücksichtigt werden.
- Ein Außenfenster ist ein Sonderfall von Bild 2 mit C_1,korr praktisch null und R_1 nach
  Gl. (25).

Für das Mehrzonenmodell heißt das: **dieselbe Trennwand wird in zwei Zonen unterschiedlich
reduziert.** Ist die Temperaturdifferenz klein (1.5), reduziert sie in beiden Zonen symmetrisch
auf R_1/C_1 und zählt als IW. Ist sie groß, reduziert sie in **beiden** Zonen nach Bild 2 mit
C_1,korr aus Gl. (17) und zählt in **beiden** als AW — jede Zone sieht ihre eigene Innenseite, die
Kettenmatrix wird für jede Zone in ihrer eigenen Zählrichtung (von ihrem Raum nach außen,
Gl. (11), S. 13) aufgebaut. Da die Kettenmatrix im Allgemeinen nicht richtungssymmetrisch ist
(ausdrücklich S. 13), ergeben sich **zwei verschiedene** Parametersätze derselben Wand. Das ist
kein Widerspruch, sondern Absicht: jede Zone bekommt das Ersatzmodell ihrer Seite. Die Korrektur
aus Konzept N1.3 („symmetrisch bis zur Mittelebene" droht doppelt zu halbieren) gilt hier
verschärft — **es wird nichts halbiert; die Wand geht mit ihrer vollen Masse in jede der beiden
Reduktionen ein**, und die Energiebilanz stimmt trotzdem, weil die Kopplung über θ_NR,eq und
R_Rest läuft, nicht über eine geteilte Kapazität. Genau dieser Punkt ist in der Energieprobe
(3.6, Probe 4) zu messen und nicht zu glauben.

### 1.5 Die 4-K-Regel der VDI 2078

**VDI 2078, 7.2, S. 46**, unmittelbar nach der Wiederholung der AW/IW-Zusammenfassung:
Innenbauteile mit **ähnlichen Raumkonditionen (Δϑ < 4 K)** können in der Regel wie Innenbauteile
mit symmetrischer Beaufschlagung auf beiden Oberflächen berücksichtigt werden — also adiabat.
Derselbe Abschnitt hält fest, dass als **Standard** das Raummodell nach VDI 6007 Blatt 1 verwendet
wird und andere Raummodelle nur zulässig sind, wenn sie nach Abschnitt 9 validiert sind.

Praktische Bedeutung: In einem Wohngebäude, in dem alle beheizten Zonen denselben Sollwert und
dieselbe Nachtabsenkung fahren, ist **jede Trennwand zwischen beheizten Zonen adiabat**. Es
koppeln nur beheizte Zonen gegen unbeheizte (Keller, Treppenhaus, Dachraum) und Zonen mit
bewusst unterschiedlichem Sollwert (Schlafzimmer 18 °C gegen Wohnzimmer 21 °C: Δϑ = 3 K, also
weiterhin adiabat — bei 16 °C gegen 21 °C nicht mehr).

Die Regel ist eine **Vorgabe-Entscheidung, keine Wahrheit**: Δϑ ist im Betrieb nicht konstant.
Umsetzungsvorschlag: die Zuordnung IW/AW wird **einmal vor dem Lauf** aus den Solltemperaturen der
beiden Zonen entschieden (max. Betrag der Sollwertdifferenz über die 8 760 Stunden; unbeheizte
Zonen gelten immer als „anders temperiert"), im Protokoll benannt und im Dialog angezeigt. Eine
Umschaltung **während** des Laufs wäre eine Strukturänderung des RC-Netzes und damit ein
Determinismusrisiko — sie wird ausgeschlossen.

### 1.6 Luft- oder Oberflächentemperatur? Und: innerhalb der Stunde oder mit der Vorstunde?

**Luft.** Gl. (40) setzt θ_NR,Lu ein, nicht eine Oberflächentemperatur. Das passt zur
Modellstruktur: die Nachbarseite der Trennwand hat im Ersatzmodell nach Bild 2 keinen eigenen
Oberflächenknoten, sondern nur den Übergangswiderstand 1/(α_kon,A;NR · A). Eine Kopplung über
Oberflächentemperaturen wäre eine andere, feinere Modellklasse und ist von der Richtlinie nicht
gedeckt.

**Zeitliche Kopplung: von der Richtlinie nicht geregelt.** Die Belege:

- Blatt 1, 6.2, S. 9: unter den **inneren** Wärmequellen und -senken stehen ausdrücklich „eine von
  der Raumlufttemperatur abweichende Lufttemperatur in einem Nebenraum" und „ein Luftaustausch mit
  Nebenräumen" — beide also als **Aktionsgrößen** (unabhängige Variablen), nicht als
  Reaktionsgrößen. Die Reaktionsgrößen zählt S. 21 abschließend auf: θ_s,IW, θ_s,AW sowie wahlweise
  θ_I,Lu und Q̇_HK.
- Blatt 1, 6.2, S. 10: alle Aktionsgrößen sind „in Stundenschritten (als Mittelwerte)" zu
  formulieren.
- Blatt 1, 6.7, S. 36 zu Testbeispiel 10: „Nebenraum ist ein Keller mit **vorgegebener**
  Temperatur".
- Blatt 1, Tabelle A10.2, S. 58: die Kellertemperatur steht als eigene Spalte im **Stundenprofil**
  der Gebäudenutzung, gleichrangig neben Personen, Beleuchtung, Maschinen, Zuluftvolumenstrom und
  Solltemperatur.
- Blatt 1, 6.5, S. 30/31: der Ablaufplan in Anhang B1 ist ausdrücklich „nur eine mögliche
  Variante"; auf Gleichungsnummern wurde im Plan bewusst verzichtet.

**Schluss:** Die Richtlinie sieht **weder** eine Iteration innerhalb der Stunde **noch** eine
Kopplung mit der Vorstunde vor, weil sie die Nachbarraumtemperatur überhaupt nicht als Ergebnis
einer Rechnung behandelt. Wer sie als Ergebnis einer zweiten Zone einsetzt, verlässt den geregelten
Bereich — er verletzt die Richtlinie nicht, kann sich aber auch nicht auf sie berufen. Die Wahl
zwischen Vorstunde und Iteration ist damit eine **EPOS-Entscheidung**, die nach Genauigkeit,
Determinismus und Rechenzeit zu treffen und zu begründen ist (3.2).

---

## 2. Testbeispiel 10 als Kopplungsnachweis

### 2.1 Was der Fall ist

Blatt 1, 6.7, S. 36 und „Allgemeine Hinweise zu den zwölf Testbeispielen", S. 32:

- Alle Innenbauteile der zwölf Testbeispiele (FB1, FB2, DE1, DE2, IT1, IT2, IW1, IW2) sind
  **adiabat** — **einzige Ausnahme** ist der Fußboden FB1 in Testbeispiel 10 (S. 32).
- Testbeispiel 10 ist Testbeispiel 5, jedoch ist FB1 eine **nicht adiabate Innenfläche**; der
  Nebenraum ist ein Keller mit vorgegebener Temperatur (S. 36).
- Die Kellertemperatur beträgt 15 °C; sie gilt auch für den Anfangszustand der Rechnung
  (S. 33, Aufzählung der Randbedingungen am Startpunkt: Außentemperatur 22 °C, Temperatur im
  Nebenraum zu FB1 15 °C „nur im Testbeispiel 10", äußere und innere Wärmequellen 0 W,
  Raumtemperatur stationär als Folge dieser Randbedingungen).
- Gerechnet wird über 60 Tage mit täglich gleichen Zeitgängen (S. 33).

### 2.2 Wie der Fußboden modelliert ist

Aus Tabelle A10.1 (S. 58): FB1 hat 17,50 m² und vier Schichten von innen nach außen — PVC-Belag,
Estrich, Steinwolle, Beton — je mit d, λ, ρ und c. Für FB1 sind **zwei** Übergangskoeffizienten
angegeben, α_kon_a **und** α_kon_i, beide 1,7 W/(m²K); bei allen übrigen Innenbauteilen der
Tabelle steht nur α_kon_i. Genau diese zweite Spalte ist die Modellaussage: FB1 hat eine
**Außenseite** — die Kellerseite — mit eigenem konvektivem Übergang, und das ist das α_kon,A;NR aus
Gl. (40). Der langwellige und kurzwellige Anteil entfällt dort, weshalb Gl. (40) gegenüber
Gl. (32) nur noch den Strahlungsquellenterm trägt.

Rechnerisch folgt daraus (eigene Ableitung aus den Schichtdaten, nicht aus der Richtlinie
entnommen): der Schichtwiderstand von FB1 beträgt rund 0,32 m²K/W, mit dem kellerseitigen Übergang
rund 0,90 m²K/W. Der Testraum verliert ohne FB1 nur über AW1 (3,50 m²) und AF1 (7,00 m², U =
2,1 W/(m²K)) — zusammen rund 17 W/K. Das nicht adiabate FB1 steuert eine Leitfähigkeit derselben
Größenordnung bei und **verdoppelt damit den Verlustleitwert des Raums**.

### 2.3 Was der Test liefert

Tabelle A10.2 (S. 58) gibt die Aktionsgrößen als 24-Stunden-Profil: Personen, Beleuchtung,
Maschinen und Sonstiges je mit Wärmeabgabe und Konvektivanteil, **Temp_NR** (Nebenraum FB),
Vol_ZL_AL (hier durchgehend 0) und Soll-Temperatur.

Tabelle A10.3 (S. 58/59) gibt Wetterdaten (Außenlufttemperatur, langwellige Ausstrahlung,
atmosphärische Gegenstrahlung, Gesamt- und Diffusstrahlung je für Außenwand Süd, Außenwand West,
Fenster Süd und Fenster West) und die Ergebnisse für **Tag 1, Tag 10 und Tag 60**, je mit
Lufttemperatur, empfundener (operativer) Temperatur und Heiz-/Kühllast, **je Tag in zwei Spalten
für Programm 1 und Programm 2**. Die Prüfregel dazu steht in 6.6, S. 31: Ergebnis im **Band**
zwischen Programm 1 und Programm 2, ± 0,1 °C für die Temperaturen und ± 1 W für die Lasten.

Testbeispiel 10 rechnet **frei schwingend**: die Heiz-/Kühllast ist in allen drei Tagen und allen
Programmspalten 0 W. Geprüft werden also ausschließlich Lufttemperatur und operative Temperatur —
und das ist der schärfere Test, weil eine geregelte Zone die Modellfehler in der Last versteckt,
eine freie Zone sie in der Temperatur zeigt.

### 2.4 Warum er als Kopplungsnachweis taugt

Der Vergleich mit Testbeispiel 5, das bis auf FB1 identisch ist (Tabelle A5.3, S. 55): die
Lufttemperatur des Testraums liegt am 60. Tag rund **19 K** niedriger, wenn FB1 an den 15-°C-Keller
grenzt. Ein solcher Abstand deckt jede plausible Fehlbedienung des Kopplungspfads auf — falsches
Vorzeichen, vergessene Gewichtung in Gl. (41), verwechselte Bezugsfläche, C_1,korr statt C_1 oder
umgekehrt, ein am Luftknoten statt am Massepfad angehängter Nachbarraum.

**Anwendung im Mehrzonenmodell (Konzept 3.6, Probe 1):** Der Keller wird als **zweite Zone**
modelliert, die über eine ideale Heizung/Kühlung ohne Leistungsgrenze exakt auf 15 °C gehalten
wird. Dann muss Zone 1 dieselben Zahlen liefern wie die Einzonenrechnung mit vorgegebener
Nebenraumtemperatur — und beide müssen im Normband liegen. Damit ist derselbe Testfall zugleich
Nachweis für das Einzonenmodell (Konzept 10.1, Stufe G0) **und** für den Kopplungsweg
(Mehrzonenstufe), ohne dass zusätzliche Normzahlen gebraucht werden. Die Normzahlen bleiben nach
Konzept N1.2 ein **nicht ausgeliefertes** Prüfmittel.

---

## 3. Rechenvorschlag: N gekoppelte 2-K-Zonen

### 3.1 (a) Je Zone das validierte Einzonenmodell

Je Zone z ∈ 1…N genau das Netz aus Konzept 4.2, unverändert, mit zwei Zuständen θ_m,AW,z und
θ_m,IW,z, drei algebraischen Knoten θ_s,AW,z, θ_s,IW,z, θ_air,z und der exakten Diskretisierung über
Φ, Γ, Ψ aus den beiden Eigenwerten. Geändert wird allein die **Bauteilzuordnung**:

- **AW-Gruppe der Zone z** = alle Außenbauteile der Zone **plus** alle Bauteile zu Zonen anderer
  Temperatur (Blatt 1, S. 15 und Gl. (27), S. 17). Jedes dieser Bauteile bekommt ein eigenes
  θ_A,eq,v: die Außenbauteile nach Gl. (32)–(38), die Trennflächen nach Gl. (40); zusammengefasst
  über Gl. (41)/(42).
- **IW-Gruppe der Zone z** = alle adiabaten Bauteile: Innenbauteile innerhalb der Zone und
  Trennflächen zu Zonen mit Δϑ < 4 K (VDI 2078, 7.2, S. 46). Deren Reduktion bleibt symmetrisch
  auf R_1/C_1 (Blatt 1, S. 14).
- **R_α;str;AW/IW** nach Gl. (29) oder, wenn A_IW < A_AW, nach Gl. (31) (S. 18) — die
  Fallunterscheidung ist neu gegenüber dem Einzonenmodell (1.2).
- **α_kon je Bauteil** nach Blatt 1, S. 10 und Konzept N1.3: für die Trennfläche gelten zwei
  Werte, der raumseitige α_kon_i der eigenen Zone und der α_kon,A;NR der Nachbarseite; beide gehen
  getrennt ein (Vorbild Tabelle A10.1, FB1).

Damit bleibt das validierte Modell Zeichen für Zeichen dasselbe; die Kopplung sitzt
**ausschließlich** in θ_A,eq,gew,z. Das ist der wichtigste Entwurfsgrundsatz dieses Vorschlags:
**keine zweite Physik, nur eine zusätzliche Randbedingung.**

### 3.2 (b) Die Kopplung: drei Wege

Gemeinsame Form. Aus Gl. (41)/(42) ist die gewichtete äquivalente Außentemperatur der Zone z eine
affine Funktion der Nachbar-Lufttemperaturen:

```
θ_A,eq,gew,z(h) = θ_ext,z(h) + Σ_{j ∈ Nachbarn(z)} B_zj · θ_air,j(h)
B_zj = Σ_{v ∈ Trennflächen z↔j} U_v·A_v / Σ_{v ∈ AW-Gruppe z} U_v·A_v      (Gl. (42))
```

θ_ext,z sammelt alle außenluft- und strahlungsbestimmten Anteile; Σ_j B_zj + (Anteil der
Außenbauteile) = 1. Die Nachbarzone wirkt also **nicht** direkt auf den Luftknoten, sondern über
R_Rest,AW auf den **Massenknoten** θ_m,AW,z — hinter der Kapazität C_1,AW. Das dämpft jede
Kopplungswirkung innerhalb einer Stunde erheblich und ist der physikalische Grund, warum schon
einfache Verfahren hier tragen. **Ausnahme:** ein Zonen-Luftaustausch (3.4) wirkt unmittelbar auf
den Luftknoten und ist die einzige starre Kopplung.

**Vorschlag A — explizit, Zonentemperatur der Vorstunde.** θ_air,j der Stunde h−1 (Stundenmittel,
nicht Endwert) geht als θ_NR,Lu in Stunde h ein. Ein Durchlauf je Stunde, Reihenfolge der Zonen
ohne Einfluss auf das Ergebnis, sobald ausschließlich Vorstundenwerte gelesen werden.

- *Genauigkeit:* Phasenfehler von einer Stunde auf dem Kopplungspfad. Größenordnung: Änderung der
  Nachbarzonentemperatur je Stunde mal Kopplungsanteil B_zj mal Empfindlichkeit des Luftknotens.
  Bei einer unbeheizten Kellerzone mit Zeitkonstanten von vielen Stunden ist das klein; bei zwei
  Zonen mit gegenläufiger Nachtabsenkung und großer Trennfläche ist es nicht klein. **Zu messen,
  nicht zu schätzen** — die Probe steht in 3.6.
- *Stabilität:* unbedingt stabil, solange die Kopplung über den Massepfad läuft (die Rückkopplung
  ist durch C_1,AW gedämpft). **Mit direktem Zonen-Luftaustausch nicht mehr garantiert:** dort
  entsteht eine unmittelbare Rückkopplung Luft↔Luft, und ein expliziter Schritt kann bei großen
  Volumenströmen (n_ij · V groß gegen die übrigen Leitwerte) aufschwingen.
- *Rechenzeit:* genau N-mal das Einzonenmodell, also rund 5 ms je Zone und Jahr (Konzept 5.13);
  50 Zonen ≈ 0,25 s.
- *Determinismus:* vollständig; keine Iteration, keine Abbruchschwelle.
- *Testbarkeit:* sehr gut — jede Zone ist für sich das geprüfte Einzonenmodell.

**Vorschlag B — Gauß-Seidel innerhalb der Stunde.** In Stunde h wird über die Zonen in **fester**
Reihenfolge iteriert; jede Zone rechnet ihren Stundenschritt mit den zuletzt bekannten
Nachbartemperaturen derselben Stunde. Abbruch, wenn sich **alle** Zonen-Stundenmittel um weniger
als 0,01 K ändern.

- *Genauigkeit:* im Grenzwert exakt gleich dem gekoppelten System (Vorschlag C) — der Fixpunkt der
  Iteration ist dessen Lösung, bis auf die Abbruchschwelle.
- *Stabilität und Konvergenz:* Das algebraische System der Luftknoten ist **strikt diagonaldominant**
  — der Selbstleitwert einer Zone enthält neben den Kopplungsleitwerten stets auch Lüftung,
  Fenster, Wärmebrücken und die beiden Massepfade. Gauß-Seidel konvergiert damit; wegen der
  Dämpfung durch C_1,AW (siehe oben) in der Regel in wenigen Durchläufen. Der Fall, der langsam
  wird, ist eine Gruppe stark gekoppelter, verlustarmer Zonen mit hohem gegenseitigem
  Luftaustausch.
- *Rechenzeit:* N-mal Einzonenmodell mal Anzahl der Durchläufe. Bei drei bis sechs Durchläufen
  0,75–1,5 s für 50 Zonen und ein Jahr. Deutlich über der Planungsgröße 10 ms je Gebäude
  (Konzept 4.8), aber gegenüber den 4 s des heutigen Gesamtlaufs vertretbar — **und nur für
  Gebäude mit Mehrzonenmodell**.
- *Determinismus:* gegeben, **wenn** Reihenfolge, Schwelle und Höchstzahl der Durchläufe fest
  verdrahtet sind und die Nichtkonvergenz ein **benannter Fehler** ist (kein stiller Rückfall auf
  den letzten Stand — Blatt 1, 6.8, S. 36 und Konzept 4.8). Vorschlag: Reihenfolge nach
  Zonen-ID aufsteigend, Schwelle 0,01 K, höchstens 50 Durchläufe.
- *Testbarkeit:* gut; für N = 2 gegen Vorschlag C prüfbar (3.6, Probe 5).

**Vorschlag C — gekoppeltes lineares Gesamtsystem, 2N Zustände.** Alle Zonen in einem Netz: 3N
algebraische Knoten (θ_s,AW, θ_s,IW, θ_air je Zone), 2N Zustände. Die algebraischen Knoten werden
gemeinsam eliminiert, es entsteht dx/dt = A_ges·x + b_ges mit A_ges ∈ ℝ^{2N×2N}, blockweise besetzt
nach der Nachbarschaft. Exakte Diskretisierung wie im Einzonenmodell.

- *Genauigkeit:* exakt (im Rahmen der Modellklasse); keine Schwelle, kein Phasenfehler.
- *Stabilität:* unbedingt stabil.
- *Rechenzeit:* Der geschlossene Weg über die Sylvester-Formel aus **zwei** Eigenwerten
  (Konzept 4.2) **entfällt** — für 2N = 100 Zustände braucht es eine allgemeine Matrixexponential-
  Rechnung (Skalierung und Quadrierung auf der erweiterten Blockmatrix, die Φ, Γ und Ψ in einem
  Zug liefert). Das ist eine **einmalige** Vorbereitung je Regelungszustand; der Stundenschritt ist
  danach ein Matrix-Vektor-Produkt (100×100 ≈ 10⁴ Operationen, 8 760-mal ≈ 10⁸ — Bruchteile einer
  Sekunde).
- *Der Haken:* Die ideale Heizung macht θ_air einer **geregelten** Zone zur vorgegebenen Größe und
  die Heizlast zur Reaktionsgröße; in einer **freien** Zone ist es umgekehrt. Welche Zonen geregelt
  sind, wechselt stündlich (Nachtabsenkung, Kappung an `Maximaleraumtemperatur`, Leistungsgrenze).
  Jede Kombination ist ein **anderes** A_ges — im schlechtesten Fall 2^N Matrizen. Ein Zwischenspeicher
  über die tatsächlich vorkommenden Muster (in einem Wohngebäude mit gleichem Zeitplan sind es
  wenige) macht es praktikabel, aber die Laufzeit wird datenabhängig, und die Umschaltsuche per
  Bisektion innerhalb der Stunde (Konzept 4.5) müsste über **alle** Zonen gleichzeitig laufen.
- *Testbarkeit:* für kleine N ausgezeichnet, für große N nur noch gegen sich selbst.

**Bewertung und Empfehlung.**

| Kriterium | A (Vorstunde) | B (Gauß-Seidel) | C (Gesamtsystem) |
|---|---|---|---|
| Genauigkeit | Phasenfehler 1 h auf dem Kopplungspfad | exakt bis 0,01 K | exakt |
| Stabilität | gut ohne Zonen-Luftaustausch, fraglich mit | gut (Diagonaldominanz) | unbedingt |
| Rechenzeit (N = 50, ein Jahr) | ≈ 0,25 s | ≈ 0,8–1,5 s | Vorbereitung je Regelungsmuster, danach schnell |
| Determinismus | vollständig | gegeben bei fester Reihenfolge, Schwelle, Höchstzahl | gegeben, aber Laufzeit datenabhängig |
| Testbarkeit | jede Zone = geprüftes Einzonenmodell | dito, plus Vergleich gegen C | nur als Ganzes |
| Aufwand | klein | mittel | groß (Matrixexponential, Regelungsmuster, Bisektion über N Zonen) |

**Empfehlung: Vorschlag B als Produktweg**, mit Vorschlag A als Vergleichsrechnung (ein Schalter
in der Probe, nicht im Dialog) und **Vorschlag C für N = 2 als Prüforakel** — für zwei Zonen ist
das Gesamtsystem 4×4, die Eigenwerte sind sauber rechenbar, und die Gauß-Seidel-Lösung muss es auf
besser als 0,001 K treffen. Damit hat der Iterationsweg einen exakten Gegenpol, ohne dass der volle
Aufwand von C ins Produkt muss.

**Warum nicht A allein:** Der Zonen-Luftaustausch (3.4) ist der Punkt, an dem A kippt, und er ist
genau die Funktion, die ein Mehrzonenmodell interessant macht (Treppenhaus, offene Küche). Wer A
wählt, muss den Luftaustausch weglassen oder begrenzen.

### 3.3 (c) Unbeheizte Zonen — und was aus der Erdreich-Randbedingung wird

Eine unbeheizte Zone ist eine Zone **ohne Heizung**: Φ_h ≡ 0, θ_air frei schwingend, kein Sollwert,
keine Kappung. Sie liefert ihre eigene Lufttemperatur als θ_NR,Lu an alle Nachbarzonen. Keine
Sonderphysik, kein Reduktionsfaktor.

**Was sich damit auflöst.** Konzept 4.4 kennt für die Grundfläche drei Randbedingungen: `ERDREICH`
(Kusuda), `KELLER` (im Rev.-1-Stand Reduktionsfaktor 0,5 gegen θ_out, nach Konzept N1.3 auf
θ_NR,eq mit **vorgegebener** Kellertemperatur korrigiert) und `AUSSENLUFT`. Mit einer Kellerzone
wird `KELLER` **zur Rechnung** statt zur Vorgabe: die Kellerlufttemperatur ist das Ergebnis der
Bilanz aus Kellerwänden gegen Erdreich, Bodenplatte gegen Erdreich, Kellerdecke gegen die beheizten
Zonen, Kellerfenstern und Kellerlüftung. Der Anwender muss keine Kellertemperatur mehr raten — das
ist der stärkste fachliche Gewinn des Mehrzonenmodells und zugleich die Antwort auf den Befund aus
Konzept N1.3, dass VDI 6007-1 **kein** Erdreichmodell hat.

**Was bleibt.** Die Kellerzone selbst braucht eine Erdreich-Randbedingung für ihre Bodenplatte und
ihre erdberührten Wände. Das Kusuda-Modell bleibt also — es rutscht nur eine Ebene tiefer. Im Kern
liegt es bereits als datenbankfreie Rechenklasse vor: `EPOS.Kern/Allgemein/Simulation/ErdreichTemperatur.cs:34`
(`public static class ErdreichTemperatur`), mit dem Jahresgang nach Kusuda im Klassenkopf
(`:16-19`), der Amplituden- und Phasenbestimmung aus dem 8 760er-Außentemperaturvektor über eine
Sinusregression der zwölf Monatsmittel (`:24-27`, ausdrücklich **nicht** aus den Stundenextrema) und
`AnalysiereJahresgang(double[] aussentemp)` als Einstieg (`:297`). Die Klasse ist heute für die
Wärmequelle Erdreich nach VDI 4640 gebaut (`:10`); für das Gebäudemodell ist sie ohne Änderung
nutzbar, weil sie keine Datenbank- und keine Oberflächenbindung hat (`:29-33`). **Sie ist nicht
anzupassen, sondern zu rufen** — das hält die Fachänderung an einer Stelle.

**Drei typische unbeheizte Zonen und was sie brauchen:**

| Zone | Hüllflächen | Besonderheit |
|---|---|---|
| Keller | Bodenplatte und Kellerwände gegen Erdreich (Kusuda), Kellerdecke gegen die beheizten Zonen, ggf. Kellerfenster | Lüftung meist gering; großer Speicher, sehr träge — Vorlauf (Konzept 4.6, 30 Tage) prüfen |
| Treppenhaus | Außenwände und Fenster gegen Außenluft, Trennwände und Decken gegen die beheizten Zonen | **starke Luftkopplung** über offene Türen und Kamineffekt — der Fall, für den 3.4 gebraucht wird |
| Dachraum | Dachfläche gegen Außenluft (mit Strahlung, Konzept 4.4, Schalter `Aussenbauteile_Strahlung`), oberste Geschossdecke gegen die beheizten Zonen | hohe Lüftungsrate (Belüftung), geringe Masse — schnelle Zeitkonstante, Sommerüberhitzung deutlich |

### 3.4 (d) Sollwerte, innere Gewinne, Lüftung und Zonen-Luftaustausch

**Je Zone eigene Werte.** Alles, was Konzept 4.4 heute je Gebäude führt, wird je Zone geführt:
Solltemperatur Tag, Nachtabsenkung, Wochenende, Ferien, `Maximaleraumtemperatur`,
`Interne_Waermegewinne` mit dem 50/50-Aufteilungsverhältnis, `Luftwechselrate` (bzw. G2:
Infiltration und Nutzerlüftung), `Heizung_Strahlungsanteil`, `Heizleistung_Max`. Die Bezugsgrößen
Wohnfläche, Raumhöhe und Volumen ebenfalls. Im Bestand hängen diese Felder am Gebäude:
`EPOS.Kern/Model/ProjektGebaeudeModel.cs:23` (`Interne_Waermegewinne`), `:29-33` (die vier
Sollwerte und `Maximaleraumtemperatur`), `:44-45` (`Wohnflaeche`, `Raumhoehe`), `:52`
(`Luftwechselrate`). Für Zonen brauchen sie eine eigene Tabelle unterhalb von `Tab_Gebaeude`
(Muster: Bauteilkatalog G3, Konzept 6.3, `Tab_Bauteil.ID_Gebaeude`).

**Solare Gewinne je Zone.** Fenster gehören einer Zone; Φ_sol wird je Zone aus **ihren** Fenstern
gebildet und nach Gl. (43)–(46) (S. 21) **innerhalb dieser Zone** auf IW und AW verteilt, mit dem
in Gl. (45)/(46) vorgeschriebenen Ausschluss der bestrahlten Fläche A_v selbst. Es gibt in der
Richtlinie **keinen** Weg, mit dem Sonne aus Zone A in Zone B gelangt (5.).

**Zonen-Luftaustausch — ist er vorgesehen?** Ja, aber nur als Randbedingung, nicht als Rechenweg:

- Blatt 1, 6.2, S. 9 nennt „einen Luftaustausch mit Nebenräumen" ausdrücklich unter den inneren
  Wärmequellen und -senken; VDI 2078, 7.1, S. 38 wiederholt die Liste wörtlich.
- Eine **eigene Gleichung** dafür gibt es nicht. Der Lüftungspfad des 2-K-Modells ist ein einziger
  Widerstand R_Lue = 1/(c_L · ρ_L · V̇_L) nach **Gl. (75), S. 27**, und die zugehörige Temperatur
  θ_Lue ist dort als „Zulufttemperatur für Infiltration, Fensterlüftung und Raumlufttechnik,
  **gewichtet nach den Volumenströmen**" erklärt.
- VDI 2078, 6.2.2, S. 35 zählt für den Luftwechsel eines Raums nur Infiltration, hygienischen
  Mindestluftwechsel und Fensterlüftung auf — Zonenströme kommen dort nicht vor.

**Daraus der Vorschlag:** je Zone eine Liste von Zuluftströmen (V̇_k, Quelle: Außenluft oder eine
andere Zone), daraus

```
V̇_ges,z = Σ_k V̇_k,z
θ_Lue,z = ( Σ_k V̇_k,z · θ_k ) / V̇_ges,z          (θ_k = θ_out oder θ_air der Quellzone)
R_Lue,z = 1 / ( c_L · ρ_L · V̇_ges,z )             (Gl. (75), S. 27)
```

Das ist **genau** die von der Richtlinie vorgesehene Form, nur mit einer zusätzlichen Quelle. Zwei
Auflagen:

1. **Massenbilanz.** Der Anwender gibt **Paare** ein (Zone A → Zone B, V̇); der Gegenstrom
   B → A mit demselben V̇ entsteht automatisch. Sonst verletzt das Modell die Luftbilanz, und die
   Energieprobe (3.6, Probe 4) schlägt fehl. Frei eingegebene Einzelströme werden **benannt
   abgelehnt**.
2. **Das ist die einzige starre Kopplung.** θ_air der Quellzone wirkt unmittelbar auf den
   Luftknoten der Zielzone, ohne Kapazität dazwischen. Sie ist der Grund für die Empfehlung
   Vorschlag B (3.2) und für die Konvergenzbetrachtung dort.

Für c_L·ρ_L gilt Konzept N1.3: die Blatt-1-Testfälle rechnen mit 1,1953 kJ/(m³K) (Testbeispiel 12,
S. 36); für Projekte nennt das Konzept 0,34 Wh/(m³K) mit Quelle DIN EN 12831. Für den
Zonen-Luftaustausch gilt derselbe Wert wie für die Lüftung der Zone — **ein** Wert im ganzen Modell,
kein zweiter.

### 3.5 (e) Ergebnis je Zone und Gebäudesumme im Kanal HEIZUNG

Die Anbindung bleibt, wo sie ist. Heute rechnet die Gebäudeschleife in
`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:190-214`: je Gebäude ein genullter
Einzelpuffer (`:188`), der Aufruf `HeizwaermeEinesGebaeudes(ctrl.items[i], i, Waermebedarf_EinGebaeude)`
(`:197`), die Addition **genau einmal** auf den Heizkanal (`:201`) und einmal auf die
Gebäudesumme (`:208`), die Energieprobe (`:204`) und die Spitze je Gebäude in `MaxP[i]` (`:212`).
Die Umrechnung W → kW steht danach an einer Stelle (`:222`), die Jahressumme bei `:229`. Der
Kanalname ist `DbWerte.KANAL_HEIZUNG` (`EPOS.Kern/Allgemein/DbWerte.cs:1260`).

**Vorschlag:** die Zonenschleife läuft **innerhalb** von `HeizwaermeEinesGebaeudes`
(`SimulationWaermebedarf.cs:566`), nicht daneben. Diese Methode ist dafür gebaut: der Kommentar bei
`:192-196` hält fest, dass ihr Rumpf Anweisung für Anweisung derselbe Text ist wie der frühere
Schleifenrumpf, damit der Gebäudedialog **genau diese** Rechnung für ein Gebäude fahren kann. Sie
füllt den Zielvektor in Watt (`:562-563`) und meldet `false`, wenn die Rechnung nicht möglich ist
(`:590-595`, benannte Warnung über `SimulationProtokoll`, kein stiller Rückfall).

Damit gilt:

- **Je Zone** entstehen die vier Reihen aus Konzept 4.6 (`Heizlast`, `Raumtemperatur`,
  `OperativeTemperatur`, `Kuehlbedarf`) plus die Kennzahlen; sie gehen in Dialog, Bericht und
  Diagramm, **nicht** in den Kanal.
- **Die Gebäudesumme** ist die Summe der Zonen-Heizlasten, in Watt in den Zielvektor — damit
  bleiben Kanal, Energieprobe, `MaxP`, Dauerlinie, Deckung und Skalierung (Konzept 4.7)
  unverändert. Kein Aufrufer außerhalb von `HeizwaermeEinesGebaeudes` merkt, dass es Zonen gibt.
- **`Waermelast_Max`** bleibt das Maximum des Kanalsummenvektors (`SimulationWaermebedarf.cs:401`,
  `Waermebedarf_Max = Maximaler_Waermebedarf(Waermebedarf)`), wie in Konzept 4.5 festgelegt.
- Die **Skalierung** `Z_AuswahlWohnflaeche / Wohnflaeche` (Konzept 4.7) wird auf der
  Gebäudesumme angewandt, nicht je Zone — sonst verschieben sich die Zonenanteile gegeneinander.
  Liegt ein Bauteilkatalog oder ein IFC-Modell vor, entfällt sie ohnehin.

### 3.6 (f) Prüfungen

| Nr. | Probe | Kriterium |
|---|---|---|
| 1 | **Testbeispiel 10 als Kopplungsnachweis.** Keller als zweite Zone, ideal auf 15 °C gehalten | Zone 1 im Normband nach Blatt 1, 6.6, S. 31 (± 0,1 °C, ± 1 W) für Tag 1, 10, 60; zugleich identisch mit der Einzonenrechnung bei vorgegebener Nebenraumtemperatur (bitgleich) |
| 2 | **Zwei identische Zonen = eine Zone.** Ein Gebäude, zweimal dieselbe halbe Zone, Trennwand adiabat | Zonen-Temperaturreihen **bitgleich** untereinander; Gebäudesumme bitgleich zur Einzonenrechnung des ganzen Gebäudes. Bricht diese Probe, stimmt die Flächenaufteilung oder Gl. (29)/(31) nicht |
| 3 | **Adiabate Symmetrie.** Zwei Zonen mit gleichem Sollwert und gleichen Lasten, Trennfläche einmal als IW (4-K-Regel) und einmal ausdrücklich als AW gerechnet | Wärmestrom über die Trennfläche = 0 im AW-Fall; Jahresenergie beider Rechnungen innerhalb 0,1 % — das prüft, dass die Zuordnung IW/AW physikalisch folgenlos ist, solange Δϑ = 0 |
| 4 | **Energiebilanz über alle Zonen.** Σ_z ( Heizung + innere Gewinne + solare Gewinne − Transmission − Lüftung − Speicheränderung ) über das Jahr, einschließlich der Zonenströme | Summe der zoneninternen Austauschströme = 0 (Massen- und Energiebilanz des Luftaustauschs); Gesamtbilanz innerhalb 1e‑6 der Jahresenergie. Das ist die Probe, die die doppelte Reduktion derselben Trennwand (1.4) prüft |
| 5 | **Iteration gegen exakte Lösung.** N = 2, Vorschlag B gegen Vorschlag C (4×4-Gesamtsystem) | Abweichung aller Stundenwerte < 0,001 K und < 0,1 W |
| 6 | **Vorstunde gegen Iteration.** Vorschlag A gegen Vorschlag B auf einem Referenzgebäude mit unbeheiztem Keller und einem mit Treppenhaus-Luftaustausch | **misst**, was das Konzept sonst schätzen müsste: maximale Stundenabweichung, Jahresenergieabweichung je Zone. Ergebnis ist die Begründung der Wegwahl, nicht deren Voraussetzung |
| 7 | **Determinismus.** Zwei Läufe desselben Modells; zusätzlich Lauf mit umgekehrter Zonen-Eingabereihenfolge bei fester interner Sortierung | byte-gleiche Reihen (Konzept 4.8, `ParallelitaetWacheTests`) |
| 8 | **Nichtkonvergenz.** Künstlich stark gekoppelte Zonen, Höchstzahl der Durchläufe erreicht | **benannter Fehler** im Protokoll, Abbruch — kein stiller Rückfall (Blatt 1, 6.8, S. 36; Konzept 4.8) |
| 9 | **Grenzfälle der Bauteilzuordnung.** Zone ohne AW; Zone ohne IW; Zone, deren AW-Fläche die IW-Fläche übersteigt | Blatt 1, 6.8, S. 36/37 verlangt ausdrücklich eine Behandlung: fehlt die zusammengefasste IW, darf nicht durch deren Fläche geteilt werden; Entsprechendes gilt für fehlende nicht adiabate Bauteile; für einen Raum ohne AW ist die Koeffizientenmatrix mit 10¹² statt 0 zu belegen. Der dritte Fall ist der Umschaltpunkt Gl. (29) → Gl. (31) |
| 10 | **Referenzlauf.** Alle dreizehn Referenzprojekte, jedes Gebäude als **eine** Zone | bitgleich zur Einzonenrechnung — das Mehrzonenmodell darf ein Einzonengebäude nicht verändern. Ohne diese Probe wird jede Mehrzonenstufe zu einem Einfrierschritt |

---

## 4. Bauteilreduktion aus Schichtdaten — die Grundlage für importierte Materialdaten

Blatt 1, 6.3, S. 11–14. Der Weg ist geschlossen vorgeschrieben; er ist zugleich der Weg, den der
Bauteilkatalog (Konzept 6.3, Stufe G3) und der IFC-Import (Befund N, Abbildungsregel `Bauweise`)
zu bedienen haben.

### 4.1 Der Rechenweg in sieben Schritten

| Schritt | Gleichung | Seite | Inhalt |
|---|---|---|---|
| 1 | (1), (2) | 11 | Je homogener Schicht v eine **Kettenmatrix** A_v im periodischen Fall, eindimensionaler Wärmefluss; x ist die Koordinate in Richtung der Wandnormalen |
| 2 | (3)–(8) | 11–12 | Die vier komplexen Elemente a₁₁, a₁₂, a₂₁, a₂₂ aus ω_BT, R und C der Schicht (Kombinationen von sinh/cosh und sin/cos über √(½ ω R C)) |
| 3 | (5)-Kasten, (9) | 12 | **R = s/λ** in m²K/W, **C = c·ρ·s** in J/(m²K), **ω = 2π/(86 400 · T)** mit T in Tagen |
| 4 | (10a)–(10e) | 12–13 | **Bezugsperioden.** Je Bauteil T_BT = **7 Tage** (Verweis auf DIN EN ISO 13786); **Ausnahme: 2 Tage** für Bauteile mit raumseitig wärmetechnisch abgedeckten Speichermassen (Beispiel der Richtlinie: abgehängte Decken). Die Entscheidung fällt **je Bauteil getrennt** über zwei Kriterien auf R₁;rel = R₁(2 d)/R₁(7 d) und C₁;rel = C₁(2 d)/C₁(7 d): (10a) R₁;rel > 0,99 **und** C₁;rel < 0,95; (10b) R₁;rel < 0,95 **und** C₁;rel < 0,95 **und** abs(R₁;rel − C₁;rel) > 0,30 → dann (10c) T_BT = 2, sonst (10d) T_BT = 7. Für die **Zusammenfassung zum Raum** gilt (10e) T_RA = **5 Tage** |
| 5 | (11) | 13 | **Gesamtwand** = Produkt der Schichtmatrizen. Die Multiplikation **beginnt mit den dem Raum zugewandten Schichten**, die Zählrichtung weist vom Raum nach außen, und die **Reihenfolge darf nicht vertauscht werden**, weil A₁,n im Allgemeinen nicht richtungssymmetrisch ist |
| 6 | (12)–(16) | 13–14 | R₁, R₂, C₁, C₂ aus den Elementen von A₁,n, je mit 1/A bzw. ·A (A = Wandfläche in m²); R₃ = (1/A)·Σ(s_v/λ_v) − R₁ − R₂ |
| 7 | (17) | 14 | **C₁,korr** für einseitige Belastung (Bild 2, S. 14), aus R_W = R₁+R₂+R₃ (Gl. (16)), ω_BT und a₁₂, a₂₂ |

**Symmetrisch belastete Bauteile** (gleiche Temperaturverhältnisse in den angrenzenden Räumen)
reduzieren nach S. 14 auf **R₁ und C₁** aus Bild 1 — ohne Korrektur.

### 4.2 Aggregation über Bauteile

Blatt 1, S. 15–17. Die Richtlinie lässt zwei Wege zu und **schreibt einen vor**: getrennte
Parallelschaltung von Kapazitäten und Widerständen **oder** Parallelschaltung der komplexen
Widerstände; weil die getrennte Schaltung Räume mit stark unterschiedlichen Speichermassen oder mit
thermisch abgedeckten Speichermassen schlechter abbildet, **wird die Parallelschaltung über die
komplexen Widerstände gewählt** (S. 16, mit Begründung aus Vergleichsrechnungen gegen das
Beuken-Modell).

| Gleichung | Seite | Inhalt |
|---|---|---|
| (19) | 16 | Z₁;IWμ = R₁;IWμ + 1/(j·ω_RA·C₁;IWμ) — der komplexe Widerstand je Bauteil, mit ω_RA aus T_RA = 5 Tagen |
| (20), (21) | 16 | R₁ = Re Z₁, C₁ = 1/(ω_RA · Im Z₁) |
| (22) | 16 | Parallelschaltung über alle Innenflächen: Z₁;IW = 1 / Σ(1/Z₁;IWμ) |
| (23), (24) | 16 | die ausgeschriebene Zweierform für R₁;IW und C₁;IW; bei mehr als zwei Bauteilen **mehrfach nacheinander** auszuführen |
| — | 16 | Für die Außenflächen gelten dieselben Erläuterungen; auch dort ist die Parallelschaltung über die komplexen Widerstände **vorgegeben** |

**Fenster, Gl. (25)–(28), S. 17.** Die Reihenfolge ist vorgeschrieben und ergebnisrelevant: die
Parallelschaltung der Fensterwiderstände hat **nach** der der Wände zu erfolgen, „da das Ergebnis
von der Reihenfolge der Berechnung beeinflusst wird"; dabei werden die Fensterwiderstände mit den
Wandwiderständen parallelgeschaltet, **die Wärmekapazität der Wände bleibt unverändert**.

- **Gl. (25):** R₁;AFv = R_AFv / 6 — der Wert für Außenfenster ist bei der Herleitung der
  Wandersatzmodelle nicht festgelegt und wird in Analogie zu R₁;AWv so angesetzt.
- **Gl. (26):** R_AFv = (1/U_AFv − 1/α_Iv − 1/α_Av) · 1/A_AFv — der U-Wert wird um **beide**
  Übergangswiderstände bereinigt, weil das Modell sie über R_α;kon und R_α;str selbst führt. Für
  Innenfenster gilt Entsprechendes.
- **Gl. (27):** R_ges,AW = 1 / ( Σ U_AWv·A_AWv + Σ U_AFv·A_AFv ) — vereinfachte Gesamtrechnung der
  zusammengefassten Außenbauteile **einschließlich der nicht adiabaten Innenbauteile**.
- **Gl. (28) mit (28a)–(28c):** R_Rest,AW als Differenz; (28a) klemmt auf R_α;ges;AW;A, (28b) gibt
  die Umkehrform für R₁;AW, (28c) setzt R₁;AW ≥ 10⁻¹⁰.

### 4.3 Welche Stoffwerte je Schicht nötig sind

**Vier Angaben je Schicht**, und nur vier: **d** (Dicke, m), **λ** (W/(mK)), **ρ** (kg/m³),
**c** (spezifische Wärmekapazität). Belege: Gl. (5)-Kasten und Gl. (9), S. 12 (R = s/λ,
C = c·ρ·s); Blatt 1, 6.2, S. 10 („Der Aufbau speicherfähiger Bauteile ist schichtweise anzugeben.
Die Stoffwerte der Schichten — Wärmeleitfähigkeit, Dichte und spezifische Wärmekapazität — gelten
als konstant."); VDI 2078, 6.1, S. 18 (Schichtaufbau von innen nach außen mit Dicke sowie die
Stoffwerte λ, ρ, c_p sind maßgebend, mit Verweis auf VDI 6007 Blatt 1).

**Einheitenfalle.** Gl. (9) verlangt C in J/(m²K), also c in J/(kgK). Die Bauteiltabellen der
Richtlinie (z. B. Tabelle A10.1, S. 58) führen c in **kJ/(kgK)** — Faktor 1 000. Der IFC-Weg liefert
`Pset_MaterialThermal.SpecificHeatCapacity` in SI, also J/(kgK) (Befund N, Abbildungsregel
`Bauweise`). Beim Baustoffkatalog `Tab_Baustoff_STAMM` steht im Konzept 6.3 bereits `cp` in
J/(kgK) — das ist die richtige Wahl; der Katalogimport aus gedruckten Tabellen braucht die
Umrechnung und einen Plausibilitätsriegel (500 ≤ c ≤ 3 000 J/(kgK) deckt Beton bis Holz ab).

**Nicht oder gering speichernde Bauteile.** VDI 2078, 6.1, S. 18 erlaubt ausdrücklich, transparente
Bauteile **allein mit dem U-Wert** zu veranschlagen; Blatt 1, S. 14 setzt die Speicherkapazität des
Außenfensters „praktisch zu null" und verweist für R₁ auf Gl. (25). Blatt 1, 6.2, S. 10 erlaubt
zusätzlich, die Stoffwerte **speicherloser** Bauteile zeitvariabel zu führen (Abluftfenster,
temporärer Wärmeschutz wie geschlossene Rollläden) — der zugehörige Korrekturweg steht in 6.5,
S. 30 (Gl. (115)–(120)).

**Was bei Luftschichten, Dämmung und Putz gilt:**

- **Luftschichten.** Die Richtlinie gibt dafür **keine** Regel — die Kettenmatrix setzt Wärmeleitung
  voraus, eine ruhende Luftschicht überträgt aber auch durch Strahlung und Konvektion. Der übliche
  Weg (DIN EN ISO 6946) ist ein **äquivalenter Wärmewiderstand ohne Kapazität**: R = R_g (aus der
  Schichtdicke und der Belüftungsart), C ≈ 0. Für C → 0 entartet die Kettenmatrix zu
  a₁₁ = a₂₂ = 1, a₁₂ = R, a₂₁ = 0; die Ausdrücke (3)–(8) enthalten dann 0/0-Formen. Das ist
  **numerisch abzufangen** — Reihenentwicklung für kleines ω·R·C statt Division —, und es fällt
  unter die ausdrückliche Vorgabe in Blatt 1, 6.8, S. 36: Division durch 0 ist zu vermeiden, „im
  weitesten Sinne". Zusatzregel für den Import: **stark belüftete** Luftschichten sind keine
  Schicht, sondern eine Außenoberfläche — alles dahinter zählt nicht mehr (hinterlüftete Fassade).
  Weil die Richtlinie dazu schweigt, gehört die Regel als **EPOS-Regel mit Quelle DIN EN ISO 6946**
  gekennzeichnet, wie das Konzept es für Kusuda und die Pauschalfaktoren tut (N1.3).
- **Dämmung.** Volle Behandlung mit d, λ, ρ, c. Wegen ρ·c ≈ 40 000 … 100 000 J/(m³K) gegenüber
  Beton mit rund 2,2·10⁶ J/(m³K) ist sie praktisch masselos, aber **nicht** null — und ihre Lage
  entscheidet: außen liegende Dämmung lässt die Masse zum Raum hin wirksam, innen liegende deckt
  sie ab. Genau das ist der Fall, für den Gl. (10a)/(10b) die Bezugsperiode auf 2 Tage umschalten.
  Der Löser darf also **nicht** pauschal mit 7 Tagen rechnen, sonst trifft er innengedämmte
  Altbauten systematisch falsch.
- **Putz.** Normale Schicht; dünn (1–2 cm) und mit mittlerem ρ·c. Er deckt die Speichermasse
  **nicht** ab — die Umschaltung auf 2 Tage zielt auf abgehängte Decken und Vorsatzschalen, nicht
  auf Putz. Trotzdem gehört er in den Schichtsatz, weil er R₁ und damit die Oberflächentemperatur
  merklich verschiebt.

**Nachweis der Reduktion (Konzept N1.3, Zeile „4.3, Nachweis"):** Die Richtlinie nennt **keine**
Soll-RC-Werte; sie gibt Schichtaufbauten (Tabellen A1.1 Typraum S, A3.1 Typraum L) **und**
Ergebnisreihen. Der Nachweis ist also indirekt: Reduktion nach Gl. (1)–(17) aus den gedruckten
Schichtdaten, Aggregation nach Gl. (19)–(28), Simulation, Treffen der Ergebnistabellen im Band.
Für ein Mehrzonenmodell kommt hinzu: **Testbeispiel 10 prüft die Reduktion von FB1 in der
AW-Variante** (Gl. (17), C₁,korr), während dasselbe FB1 in Testbeispiel 5 in der IW-Variante
(R₁/C₁) geprüft wird. Die beiden Fälle zusammen sind der einzige normbelegte Nachweis, dass beide
Reduktionsarten desselben Bauteils richtig gerechnet werden — sie gehören deshalb als **Paar** in
die Abnahme.

---

## 5. Grenzen

**Feuchte.** Blatt 1, 6.2, S. 10: die Berechnung einer latenten (feuchten) Wärmelast — der
Enthalpie des zu- oder abgeführten Feuchtigkeitsstroms — ist **nicht Gegenstand** der Richtlinie.
Die Wärmelast ist durchgehend sensibel. Ein Mehrzonenmodell erbt das: kein Feuchtetransport
zwischen Zonen, keine Kondensat- oder Schimmelaussage, auch nicht mittelbar über
Oberflächentemperaturen. Deckt sich mit Konzept 15.

**Wärmebrücken.** Im 2-K-Modell kommen sie nicht vor. Das Konzept führt sie masselos als Σψ·L im
Zweig H_ext (4.2). Im Mehrzonenfall entsteht ein **Zuordnungsproblem**: der Anschluss
Außenwand/Kellerdecke (`ProjektGebaeudeModel.cs:48`,
`Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke`) liegt genau auf der Zonengrenze.
Wird er beiden Zonen zugeschlagen, zählt er doppelt; wird er einer zugeschlagen, ist die Wahl
willkürlich. Vorschlag: ψ·L gehört **der Zone, in der die wärmere Seite liegt**, und die Regel steht
im Dialog; Befund N nennt für ψ ohnehin keine IFC-Quelle („ψ-Werte stehen in keinem Standard-Pset",
Abbildungsregel `WbvkFensterWand` ff.), der Wert bleibt also eine Eingabe.

**Zonen-Luftströme.** Siehe 3.4: als Randbedingung genannt (Blatt 1, 6.2, S. 9; VDI 2078, 7.1,
S. 38), aber ohne eigene Gleichung. Was **nicht** modelliert wird: Auftriebs- und Windantrieb,
Druckbilanz des Gebäudes, offene Türen als variabler Querschnitt, Kamineffekt im Treppenhaus. Alle
Ströme sind Eingaben mit festem oder profiliertem Volumenstrom. Wer ein Treppenhaus mit offenen
Türen rechnet, rechnet den Strom, den er selbst eingegeben hat — die Richtlinie bietet dafür keinen
Rückhalt, und der Dialog muss das sagen.

**Mehrzonen-Solarverteilung.** Gl. (43)–(46), S. 21 verteilen die Strahlung **innerhalb eines
Raums**, flächenproportional, mit Ausschluss der bestrahlten Fläche selbst; Blatt 1, 6.2, S. 10
präzisiert, dass das Fenster und die dazu parallelen Bauteile — auch das Bauteil, in dem das
Fenster sitzt — nicht beaufschlagt werden. **Über Zonengrenzen hinweg gibt es keinen
Strahlungsweg.** Praktische Folge: eine verglaste Zwischenwand, ein Atrium mit angrenzenden Büros,
eine Galerie über zwei Geschosse sind mit N gekoppelten 2-K-Zonen nicht abbildbar. Die Antwort der
Richtlinie darauf steht in 5.3, S. 7: wo kein thermischer Ausgleich stattfindet, teilt man den
**Raum** — wo er stattfindet, ist es **ein** Raum. Ein Atrium mit angrenzenden Bereichen ist nach
dieser Lesart ein Raum mit vertikaler Aufteilung, nicht ein Gebäude aus gekoppelten Zonen.

**Langwelliger Austausch zwischen Zonen.** Der Strahlungsaustausch des 2-K-Modells ist streng
raumintern: α_str = 5,0 W/(m²K) nach Gl. (30), S. 18, der zusammengefasste Widerstand
R_α;str;AW/IW nach Gl. (29) bzw. (31), und die Stern-Dreieck-Umrechnung nach Gl. (55)–(57), S. 25.
Zwischen zwei Zonen gibt es **keinen** Strahlungspfad — auch nicht durch eine offene Tür. Blatt 1,
6.4, S. 15 nennt als Voraussetzung ausdrücklich, dass **alle Flächen des Raums** entsprechend ihrer
Größe am Strahlungsaustausch beteiligt sind; die Zonengrenze schneidet diesen Austausch. Eine Zone,
die einen erheblichen Teil ihrer Umfassungsfläche an eine andere Zone verliert, bekommt damit einen
strukturellen Fehler in der operativen Temperatur (Gl. (103), S. 29: Mittel aus Luft- und
flächengewichteter Oberflächentemperatur). Das ist ein Argument für **große** Zonen und gegen eine
raumfeine Zonierung — und es ist der Grund, warum die 4-K-Regel (1.5) nicht nur Rechenzeit spart,
sondern die Modellgüte erhöht.

**Modellklasse.** Das 2-K-Modell trägt gegenüber dem n-K-Referenzmodell eine Unschärfe, die
VDI 2078, Tabellen 9 und 10 (S. 82–85) beziffert (Konzept N1.9 nennt die Größenordnungen). Eine
Kopplungsvariante, die sich um weniger als diese Unschärfe unterscheidet, ist keine bessere Physik,
sondern nur eine andere Zahl. Das ist der Maßstab, an dem Probe 6 (3.6) zu lesen ist.

**Normstatus.** Ein Mehrzonenmodell ist von VDI 6007 Blatt 1 **nicht** gedeckt (5.3, S. 7) und von
VDI 2078 ausdrücklich nicht vorgesehen (6.2, S. 21). Es ist eine EPOS-Erweiterung und als solche zu
kennzeichnen — wie Kusuda (Konzept N1.3), Hay-Davies (Q20) und die Pauschalfaktoren F_F, F_S, F_W.
Die Aussage in Wiki und Bericht bleibt „Rechenkern nach VDI 6007 Blatt 1, validiert an den zwölf
Testbeispielen" (Konzept N1.10) — **je Zone**; der Satz darf nicht zu „Mehrzonensimulation nach
VDI 6007" werden.

---

## 6. Was daraus für das Mehrzonenkonzept folgt

1. **Reihenfolge.** Das Mehrzonenmodell baut auf G3 (Bauteilkatalog) auf, nicht auf G1. Ohne
   Schichtdaten gibt es keine Trennwandreduktion nach Gl. (11)–(17) — der Klassenweg (Konzept 4.3)
   kennt nur eine Gesamtkapazität je Gebäude und keine einzelne Wand. Die Stufenfolge des Konzepts
   (N1.1: G0 → GB → G1+G2 → G3 → G4 → G5) bleibt damit unangetastet; das Mehrzonenmodell ist **G5
   oder später**.
2. **Datenmodell.** Eine Zonentabelle unterhalb von `Tab_Gebaeude` (Muster `Tab_Bauteil`,
   Konzept 6.3), `Tab_Bauteil.ID_Zone` statt bzw. neben `ID_Gebaeude`, und eine Tabelle der
   Zonenbeziehungen (Trennfläche, Luftstrom). Beziehungen über IDs, STRICT, eigener
   Migrationsschritt — die Hausregel aus `CLAUDE.md`, Abschnitt „Datenhaltung".
   `Abfrage_Projektgebaeude` (`sql/schema/002_views.sql:89-91`) muss dafür ein zweites Mal neu
   aufgebaut werden; der Namensleser aus Konzept 6.2 ist Voraussetzung.
3. **IFC.** Befund N, Sonderfall 9 hält fest, dass `IfcZone` in G4a **gar nicht** gelesen wird und
   die Räume über `IIfcRelContainedInSpatialStructure` bzw. `IIfcRelAggregates` am Geschoss hängen,
   „und das genügt für eine Einzonenrechnung". Für das Mehrzonenmodell wird genau dieser Punkt
   aufgemacht — mit der dort schon benannten Falle, dass `IfcZone` in `RelatedObjects` weitere
   `IfcZone` und `IfcSpatialZone` tragen darf und **entschachtelt** werden muss, sonst zählen
   Flächen doppelt. Die Trennflächen zwischen Zonen kommen aus `IfcRelSpaceBoundary` (2nd Level) —
   dort, wo Befund N, Sonderfall 4 die Archicad-Eigenheit beschreibt.
4. **Zonenzahl.** Für N bis 50 ist Vorschlag B rechenzeitverträglich. Eine harte Obergrenze gehört
   trotzdem ins Modell (Vorschlag: 50 Zonen je Gebäude), mit benannter Ablehnung darüber — nicht,
   weil die Physik versagt, sondern weil ein IFC-Import mit 400 Räumen sonst unbemerkt einen
   Jahreslauf von Minuten erzeugt.
5. **Was der Anwender entscheiden muss.** (i) Kopplungsweg A oder B — Empfehlung B, nach Messung
   durch Probe 6; (ii) ob der Zonen-Luftaustausch überhaupt eingebaut wird (er ist die einzige
   starre Kopplung und der Grund gegen A); (iii) ob die 4-K-Regel als feste Vorgabe gilt oder je
   Trennfläche übersteuerbar ist; (iv) ob unbeheizte Zonen im Bedarfsdialog eigene Zeilen bekommen
   (sie tragen keine Heizlast, aber Temperatur und Überhitzungsstunden).
