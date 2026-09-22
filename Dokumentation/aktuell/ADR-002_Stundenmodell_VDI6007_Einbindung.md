# ADR-002: Stundenmodell VDI 6007 als Vorgabemodell — Einbindung mit einer Naht und Neu-Einfrieren der Basis

**Status:** Angenommen (15.09.2026, Anwenderentscheide E1, E2, E4, E8, E10)
**Ergänzung (16.09.2026, E20; 17.09.2026, E26):** Die „eine Naht" ist seit [ADR-006](ADR-006_Trennung_Altweg_VDI6007.md) eine **Weiche am Eingang** der Gebäudebedarfsrechnung zwischen zwei getrennten Modulen (`Altweg/`, `Gebaeude/`); der Tagesbilanz-Weg ist ein eingefrorener Bestandsweg, der nach E23 jetzt bleibt und nach E26 mit der Stufe GA abgelöst wird (Zeitpunkt offen, Q24). Alles Übrige dieses ADR gilt unverändert, **mit Ausnahme von Entscheidung 3, Satz 3** (Flächen- und Bewohnerrechnung).
**Datum:** 15.09.2026
**Entscheider:** Anwender (Projektverantwortung EPOS-Plan)
**Betrifft:** [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Kapitel 4, 6, 10 und Nachtrag 1), [`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Kapitel 1 und 4), Einfrierregeln des Regressionsnetzes in der Wurzel-`CLAUDE.md`
**Berührte Bereiche:** `EPOS.Kern/Allgemein/Simulation/`, `EPOS.Kern/Allgemein/BhkwPlan.cs`, Gebäudespalten-Schritt (Papiername M3) und Klimaspalten-Schritt (M4), `Referenzlaeufe/`

---

## Kontext

EPOS-Plan rechnet den Heizwärmebedarf eines Gebäudes heute mit dem **Tagesmodell** aus der
BHKWPLAN-Bibliothek (`EPOS.Kern/Allgemein/BhkwPlan.cs`): ein Ein-Knoten-Modell
(1R1C) auf Tagesmitteln aus `Tab_Klimadaten`, dessen Tageswert nachgelagert auf Stunden
verteilt wird. Die Wärme eines Gebäudes entsteht an genau einer Stelle,
`SimulationWaermebedarf.HeizwaermeEinesGebaeudes`
(`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:566`); Lauf und Bedarfsdialog
rufen dieselben Methoden, der Zielvektor ist ein Einzelgebäude-Puffer in Watt mit einer
einzigen Umrechnung nach kW (Befund L).

Der Anwender hat am 15.09.2026 beauftragt, das Gebäudemodell auf **Stundenwerte nach
VDI 6007 Blatt 1** umzustellen und dabei Datenmodell und Lösungsschema aus dem Material
auf dem Netzlaufwerk zu übernehmen, den Rechenweg aber **neu zu schreiben**. Die Prüfung des
Materials ergab (Befund A, B, G): das Python/C#-Modell trägt einen Strukturfehler in der
Systemmatrix (diagonal statt voll besetzt), der in den Normtestfällen bis zu 304 K Abweichung
erzeugt; die Lizenz ist ungeklärt. Übernehmbar sind allein Datenmodell und Lösungsschema.

Ein außerhalb des Repositoriums gebauter Prototyp des 2-K-Modells („7R2C": fünf Knoten
θ_m,AW, θ_m,IW, θ_s,AW, θ_s,IW, θ_air, algebraisch auf ein 2×2-System reduziert; exakte
Diskretisierung Φ = exp(A·h); Stundenmittel; ideale Regelung mit Bisektion des
Umschaltzeitpunkts) liegt im Normband der Richtlinie
mit Druckrundung (E10: ± 0,15 K bzw. ± 1,5 W) in 35 von 36 Prüfungen; offen ist allein
Testbeispiel 11 in zwei Umschaltstunden (Befunde E und J). Der Vergleich mit den AixLib-Modellen (zwölf von zwölf Fällen)
diente der Entwicklung; maßgeblich ist nach Nachtrag N1.2 allein das Normband.

### Kräfte, die die Entscheidung formen

1. **Regressionsnetz.** Die Basis `2026-09-22_R11_Bestandsbefunde` umfasst dreizehn Projekte
   bei einer Toleranz von 1e-4. Jede Änderung am Gebäudemodell ändert alle dreizehn
   Projekte; der Prototyp liegt je Projekt 7 bis 33 % über dem Tagesmodell (Befund F). Ohne
   Neu-Einfrieren gibt es keinen Nachweis mehr.
2. **Zwei Rechenwege sind teuer.** Jeder Fehler, jede Kennzahl und jede Wiki-Seite wäre
   doppelt zu pflegen; die Anwender müssten wissen, wann welches Modell gilt.
3. **Bestandsbefunde.** Der Bestandsweg hat stille Fehler, die vor dem Modellwechsel
   sichtbar zu machen sind: statischer Zustand `_prevRoomTemp` über Gebäude hinweg,
   Gebäude 10576 mit `Bauweise = 50 Wh/K` als Rückfallwert, NaN ohne Warnung, eine
   Obergrenze von 100 Gebäuden (Befunde D, G und L).
4. **Skalierung.** Die Hochrechnung des Bedarfs auf eine andere Fläche oder ein anderes
   Volumen und die Verbrauchs-Rückrechnung werden im Feld gebraucht (Entscheid E8).
5. **Zwei Zeitbasen.** `Tab_Solar` liegt stündlich in UTC vor, `Tab_Klimadaten` als
   Tagesmittel; ein Stundenmodell braucht stündliche Außentemperatur (Schemaschritt für
   `Tab_Solar`).
6. **Plattformfreiheit.** Der Rechenkern läuft unverändert auf Windows und iOS; keine
   native Abhängigkeit, keine Fremdbibliothek für die Physik.
7. **Normzahlen.** Die Referenzwerte der Richtlinie sind urheberrechtlich geschützt und
   dürfen nicht ausgeliefert werden; der Normfallnachweis ist ein lokaler Nachweis.

## Entscheidung

1. **Das 2-K-Modell nach VDI 6007 Blatt 1 rechnet stündlich und ist das Vorgabemodell für
   alle Gebäude, auch bestehende** (E1, „feste Entscheidung"). Die Tagesbilanz bleibt als
   ausdrücklich wählbare Ausnahme erhalten (`Tab_Gebaeude.Gebaeude_Modell`, NULL = VDI 6007).
2. **Der Rechenweg wird im Kern neu geschrieben.** Kein Quelltext aus dem Material wird
   übernommen; übernommen werden das Datenmodell (Bauteilgruppen AW/IW, Fenster im AW-Zweig,
   Bauteilreduktion nach Gl. (1)–(17)) und das Lösungsschema (Zustandsraum, exakte
   Diskretisierung, Stundenmittel als Blockmittel, Vorlauf 30 Tage). Der Prototyp ist
   Vorlage und zugleich unabhängige Zweitimplementierung für die Abnahme.
3. **Die Einbindung hat eine Naht.** Die Verzweigung zwischen Tagesmodell und Stundenmodell
   sitzt in `HeizwaermeEinesGebaeudes`; der Watt-Puffer, die Umrechnung nach kW, der
   Kanal HEIZUNG und `Waermelast_Max` als Maximum des Kanalsummenvektors bleiben unberührt.
   Satz 3 dieser Entscheidung („Die Flächen- und Bewohnerrechnung … folgt derselben
   Modellwahl") ist durch E20 und [ADR-006](ADR-006_Trennung_Altweg_VDI6007.md) ersetzt: Der
   Vorbereitungsschritt läuft vor der Weiche und kennt keine Modellwahl; er liefert nur, was
   ohne Modellauf feststeht. Bewohnerzahl und Skalierungsfaktor nach E8 entstehen je Modul aus
   dessen erstem Lauf, die Fassade führt die Schleife.
4. **Die Bestandsgewichte entfallen im Stundenmodell** (E2). Der Gebäudedialog zeigt je
   Bauteil U, A und U·A ohne verdeckte Faktoren; dieselbe Zielstruktur trägt später den
   IFC- und den gbXML-Import.
5. **Die Skalierung bleibt** als Nachmultiplikation der Fläche und als
   Verbrauchs-Rückrechnung erhalten (E8); sie entfällt erst für Gebäude mit echter Hülle aus
   dem Bauteilkatalog (G3) oder einem Import (G4).
6. **Reihenfolge und Einfrieren** (E4): G0 (Löser im Kern ohne Wirkung, Normtestfälle lokal)
   → GB (Bestandsbefunde beheben, **eigener Einfrierschritt** auf dem Bestandsweg, vierte
   Einfrierregel „gesäte Gebäudedaten der Testdatenbank") → Schemaschritte → G1 + G2
   gemeinsam (**Basis vollständig neu eingefroren**). Zwischen GB und G1 muss jeder Merge
   `GESAMT: PASS` gegen die GB-Basis melden.
7. **Produktausweis** (E10): „Rechenkern nach VDI 6007 Blatt 1; elf der zwölf Testbeispiele
   im Normband einschließlich Druckrundung, Testbeispiel 11 in zwei Umschaltstunden um
    3,4 W daneben (3,9 W gegen das Band ohne Druckrundung)" — bis G0 den Fall 11 über einen eigenen Knoten der Kühldecke löst.

## Betrachtete Optionen

### Option A: Das Material übernehmen und anpassen

Das Python/C#-Modell nach C# übertragen, den Matrixfehler beheben, in den Kern hängen.

| Dimension | Bewertung |
|---|---|
| Komplexität | niedrig bis mittel |
| Richtigkeit | nicht gegeben — Strukturfehler in der Systemmatrix, weitere Abweichungen von Blatt 1 (Befund B) |
| Lizenz | ungeklärt |
| Nachweisbarkeit | schwach — kein Testfall des Materials besteht |

**Dafür:** schnellster Einstieg. **Dagegen:** Man baut auf einem falschen Modell auf und
kann später nicht mehr sagen, welche Abweichung Erbe und welche Absicht ist; Lizenzrisiko.

### Option B: Tagesmodell bleibt Vorgabe, Stundenmodell wählbar je Gebäude

Beide Modelle ohne vorgesehenes Ende nebeneinander, neue Gebäude mit Vorgabe Tagesbilanz.

| Dimension | Bewertung |
|---|---|
| Komplexität | mittel — zwei Rechenwege, zwei Kennzahlensätze |
| Regressionsnetz | Basis bleibt zunächst unverändert |
| Pflege | doppelt: Fehler, Kennzahlen, Wiki, Tests |
| Anwendernutzen | gering — die meisten Gebäude blieben im Tagesmodell |

**Dafür:** kein Neu-Einfrieren. **Dagegen:** Zwei Wahrheiten je Projekt; die gestrichenen
Bestandsgewichte und der Skalierungsweg wären in zwei Fassungen zu halten. Vom Anwender
ausdrücklich verworfen (E1).

### Option C: Stundenmodell als Vorgabe, Neuschreiben, eine Naht, Basis neu einfrieren *(gewählt)*

| Dimension | Bewertung |
|---|---|
| Komplexität | mittel — ein Löser, eine Verzweigung, zwei Schemaschritte |
| Richtigkeit | belegt — Prototyp 35/36 im Normband mit Druckrundung |
| Regressionsnetz | einmaliges Neu-Einfrieren, davor eigener Einfrierschritt GB |
| Rechenzeit | rund 5 ms je Gebäude und Jahr (Befund H) |
| Plattform | reine BCL, kein Fremdpaket |

**Dafür:** ein Modell, ein Nachweis, eine Zielstruktur für Dialog und Importe.
**Dagegen:** Alle dreizehn Referenzprojekte ändern sich; der Dialog ist umzubauen.

### Option D: Externe Simulationsmaschine anbinden

EnergyPlus oder ein Modelica-Werkzeug als Prozess oder Bibliothek.

| Dimension | Bewertung |
|---|---|
| Komplexität | hoch — Prozesskopplung, Eingabedateien, Ergebnisrücklesen |
| Plattform | iOS nicht erreichbar |
| Lizenz und Auslieferung | zusätzliche Fremdanteile, Installationsgröße |
| Laufzeit | Sekunden bis Minuten je Gebäude statt Millisekunden |

**Dagegen:** Bricht die Plattformfreiheit und die Größenordnung der Laufzeit; ein
Wärmeversorgungskonzept mit vielen Varianten braucht Millisekunden je Gebäude.

## Abwägung

Die Entscheidung fällt zwischen B und C, denn A scheidet an der Richtigkeit und D an der
Plattform aus. B schont die Basis, kauft das aber mit zwei Rechenwegen ohne Ende; C kostet
einmal das Neu-Einfrieren und liefert dafür ein einziges, nachgewiesenes Modell. Da der
Prototyp die Normtestfälle bereits besteht und die Abweichung zum Bestand quantifiziert ist
(Befund F: die Parametrierung, nicht die Struktur, trägt den Löwenanteil), ist der Wechsel
kein Sprung ins Ungewisse. Der eigene Einfrierschritt GB vor G1 trennt die Bestandskorrekturen
vom Modellwechsel, sodass jede spätere Abweichung eindeutig zuzuordnen bleibt.

## Konsequenzen

- **Einfacher wird:** eine Physik, eine Kennzahlenmenge, eine Wiki-Seite; U·A je Bauteil
  im Dialog ist zugleich die Zielstruktur der Importe; Raumtemperatur, Kühlbedarf und
  Spitzenkennzahlen stehen erstmals je Gebäude zur Verfügung.
- **Schwerer wird:** die Basis wird zweimal neu eingefroren (GB, G1+G2) und ist mit
  Begründung in `Referenzlaeufe/LIESMICH.md` zu dokumentieren; der Normfallnachweis läuft
  lokal, nicht in der CI (Normzahlen nicht im Repositorium); der Katalogeditor bekommt
  einen Schreibweg und zwei neue Fensterfelder (Ost, West).
- **Später zu prüfen:** der Zeitbezug der Sonnengeometrie (Stundenanfang wie der Bestand
  oder Stundenmitte wie Blatt 3) wird in G1 gemessen; Hay-Davies bleibt als benannte
  Abweichung von Blatt 3 stehen, solange kein Bedeckungsgrad vorliegt; Testbeispiel 11
  wird in G0 über einen eigenen Oberflächenknoten der Kühldecke nachgezogen.
- **Der Bestandsweg** bleibt als wählbare Ausnahme regressionsgeprüft und wird nicht
  weiterentwickelt.

## Aufgaben

1. [ ] G0: Löser im Kern (`EPOS.Kern/Allgemein/Simulation/Gebaeude/`), Normtestfälle als
       lokal beizustellende Datei, Fall 11 mit Deckenknoten.
2. [ ] GB: Warnungen statt NaN, Instanzzustand statt `_prevRoomTemp`, Korrektur Gebäude
       10576, Grenze 100 Gebäude, vierte Einfrierregel; eigener Einfrierschritt.
3. [ ] Gebäudespalten-Schritt M3 und Klimaspalten-Schritt M4 (Zusammenlegung nach Frage U5),
       `Tab_Solar`-Schritt, Sicht `Abfrage_Projektgebaeude` neu aufbauen. Die Schrittnummern
       werden erst bei der Beauftragung an `SchemaStand.Zielversion` abgelesen.
4. [ ] G1 + G2: Verzweigung, Vorlauf, Skalierung, Dialogumbau, Ergebnisdarstellung; Basis
       neu einfrieren, Statuszeile in
       [`Status_Gebaeudesimulation_VDI6007.md`](Status_Gebaeudesimulation_VDI6007.md).
5. [ ] Wiki-Seite „Gebäudemodell VDI 6007" und Logbuch-Eintrag mit dem Upload.
