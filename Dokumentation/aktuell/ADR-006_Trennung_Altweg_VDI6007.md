# ADR-006: Ablösung des Tagesbilanz-Wegs — zwei getrennte Rechenwege, der Altweg als Übergang bis zur Ablösung

**Status:** Angenommen (16.09.2026, Entscheid E20 des Anwenders — Konzept-Nachtrag N1.25)
**Ergänzung (16.09.2026, E23; 17.09.2026, E26):** E23 („GA: altweg soll bleiben") heißt: Der Altweg bleibt **jetzt** — als eingefrorenes Modul ohne neue Funktion, mit Referenzprojekt und Rückweg-Test. E26 stellt klar, dass dies ein **Übergang** ist: Der VDI-Weg löst den Altweg später vollständig ab und muss eigenständig arbeiten. Entscheidung 4 lautet deshalb wieder: Die Ablösung ist die **Stufe GA**, ihr Zeitpunkt ist offen (Q24), ihr Umfang ist Q25; bis dahin keine neue Funktion im Altweg. Der Text ist auf den Stand E20 + E23 + E26 gebracht (Konzept N1.25, N1.28, N1.31); die Entscheidungen 1 bis 3 und 5 gelten unverändert (1 mit dem Vertrag des Vorbereitungsschritts nach Softwarearchitektur 1.3 und Konzept N1.31), 6 ist neu.
**Datum:** 16.09.2026
**Entscheider:** Anwender (Projektverantwortung EPOS-Plan)
**Betrifft:** [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Nachtrag N1.1, N1.25, N1.28, N1.31, Kapitel 11 und 13), [`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Kapitel 1, 2, 5 und 6 — Löschliste der Stufe GA), [`Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md`](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md)
(Kapitel 1, 3, 5 und 6), [`ADR-002`](ADR-002_Stundenmodell_VDI6007_Einbindung.md) (Ergänzungsvermerk)
**Berührte Bereiche:** `EPOS.Kern/Allgemein/Simulation/` (Fassade, Modul `Altweg/`, Modul `Gebaeude/`),
`EPOS.UI/Dialoge/Bedarf/`, `EPOS.UI.Daten/Bedarf/`, Gebäudespalten-Schemaschritt, `Referenzlaeufe/`

---

## Kontext

Entscheid E1 hat das Stundenmodell nach VDI 6007 Blatt 1 zum Rechenmodell für alle Gebäude
gemacht und die Tagesbilanz als je Gebäude wählbare Ausnahme stehen lassen. ADR-002 hat die
Einbindung als **eine Naht** im Bestandscode beschrieben: eine Verzweigung an der Stelle, an der
der Lauf heute das Tagesmodell ruft, der Tagesbilanz-Zweig „Zeichen für Zeichen" im selben Rumpf.
Die Ausarbeitung hat daraus zwei Verzweigungspunkte (`SimulationWaermebedarf.cs:581` und `:647`),
einen Gebäudedialog mit Modellreiter, dessen Felder je nach Modell versteckt oder gesperrt werden
(Frage U2), und ein Referenzprojekt gemacht, das bis zur Ablösung auf Tagesbilanz steht (Frage A15).

Am 16.09.2026 hat der Anwender entschieden (E20): Die neue und die alte Berechnung werden
**vollständig getrennt**, die neue **löst die alte ab**, die alte bleibt **nur als Übergang**, und
**alle Dialoge und Eingaben** folgen der VDI-6007-Struktur. Am selben Tag hat er nachgeschärft (E23,
„GA: altweg soll bleiben"): Der Altweg bleibt **jetzt** als eingefrorener Bestandsweg. Am 17.09.2026
hat er dazu klargestellt (E26), dass das Altmodell nur noch als Übergang arbeitet und das Neumodell
es später vollständig ablöst; die Ablösung ist die Stufe GA, ihr Zeitpunkt ist offen (Q24).

### Kräfte, die die Entscheidung formen

1. **Zwei Rechenwege in einem Rumpf verlangsamen jede Änderung**: Jede Zeile des VDI-Wegs muss
   beweisen, dass sie den Tagesbilanz-Zweig nicht berührt. Getrennte Module machen diesen Beweis zu
   einer Frage der Aufrufstruktur; der Rückweg-Test prüft dann bis zur Ablösung ein abgeschlossenes
   Modul.
2. **Die Oberfläche kann nicht zwei Strukturen tragen**: Ein Dialog, der je nach Modell Felder
   versteckt, sperrt oder umdeutet, ist für den Anwender ein Rätsel und für die Tests ein
   Zustandsraum (U2). Ein Dialog in einer Struktur mit einem eingeklappten Zusatzabschnitt ist beides
   nicht.
3. **Bestandsprojekte brauchen den Bestandsweg**: Nach E1 liefern alle Projekte andere Zahlen; der
   Anwender will den alten Wert daneben sehen und den Bestandsweg je Gebäude ausdrücklich wählen
   können. Damit daraus kein zweites Produkt im Produkt wird, bekommt der Altweg keine neue Funktion.
4. **Die Referenzbasis ist die Abnahme**: Die Verschiebung des Altwegs darf kein Ergebnis ändern;
   sie muss byte-gleich nachweisbar sein, bevor der VDI-Weg angebunden wird.

## Entscheidung

1. **Eine Weiche, zwei Module.** `SimulationWaermebedarf` wird zur Fassade: Ein modellfreier
   Vorbereitungsschritt (`GebaeudeVorbereitung`) liefert, was beide Wege brauchen und ohne
   Modellauf feststeht — Klimakalender (dem VDI-Modul nur der gemeinsame Teil), bisheriger
   Verbrauch, Flächen, Einheit und Jahresnutzungsgrad (Vertrag: Softwarearchitektur 1.3);
   dann liest die Weiche den Rechenweg des Gebäudes und ruft genau ein Modul. Bewohnerzahl
   und Skalierungsfaktor nach E8 entstehen je Modul aus dessen erstem Lauf; die Fassade führt
   die Schleife, und nichts läuft über die Modulgrenze (Befund X, X5). Der
   Tagesbilanz-Weg wandert **Zeichen für Zeichen** nach `EPOS.Kern/Allgemein/Simulation/Altweg/`
   und bekommt keine Funktion mehr; der VDI-Weg lebt in
   `Gebaeude/` und ruft nichts aus dem Altweg. Es gibt keinen zweiten Verzweigungspunkt (A16
   gegenstandslos).
2. **Oberfläche in VDI-Struktur.** Gebäudedialog, Katalogdialog, Skalierungsdialog und
   Bedarfsdialog werden nach den Eingaben des VDI-Wegs aufgebaut; die Modellparameter sind immer
   sichtbar und bearbeitbar. Felder, die nur der Altweg liest, erscheinen allein bei einem Gebäude
   auf dem Altweg in einem eingeklappten Abschnitt „Tagesbilanz (Bestandsweg)". Der Schalter heißt
   „Rechenweg" mit Vorgabe „VDI 6007" und Wert „Tagesbilanz"; Schalter und Abschnitt bleiben für
   die Dauer des Übergangs und entfallen mit der Stufe GA.
3. **Kein zweites Datenmodell.** Der Gebäudespalten-Schritt bringt die VDI-Spalten und die
   Umbenennung nach E19; Altweg-Spalten bleiben für die Dauer des Übergangs unangetastet (E23) und
   entfallen erst mit der Stufe GA. Ein Befund je Feld weist die Klasse aus (nur Altweg, beide,
   nur VDI).
4. **Der Altweg ist der Übergang; die Ablösung ist die Stufe GA (E23, E26).** Modul, Weiche,
   Schalter, Altweg-Spalten, das Referenzprojekt auf dem Altweg und der Rückweg-Test bleiben,
   solange der Übergang läuft. Der Altweg bekommt keine neue Funktion — nur Fehlerbehebung (GB) —,
   keine Kühllast, keine Anlagenkopplung und keine Zonen; er ist Vergleichsmaßstab, kein zweites
   Produkt. Die Ablösung ist die **Stufe GA — Altweg ablösen**, die letzte Stufe des Stufenplans:
   ohne Termin und in keiner Summe (5–8 PT). **Q24** ist offen und fragt, wann der VDI-Weg bewährt
   genug ist, dass GA beauftragt wird (empfohlenes Ablösekriterium: alle Referenz- und
   Bestandsprojekte einmal auf VDI 6007 gerechnet und je Projekt erklärt; eine Feldphase von
   mindestens einer Heizperiode ohne offenen Fehler; KU1 und, falls beauftragt, AK1 abgenommen;
   die Ausbauprobe grün). **Q25** ist offen und fragt nach dem Umfang der Stufe GA; die Empfehlung
   steht als Löschliste im Umsetzungskonzept, Kapitel 6.
5. **Ausweis.** Ein Gebäude auf dem Altweg trägt „Tagesbilanz (Bestandsweg)" statt des
   Produktausweises nach E10.
6. **Jeder Altweg-Sonderfall wird sofort eingetragen.** Jede Stufe, die einen Altweg-Sonderfall
   einführt — Hinweistext, Ressourcenschlüssel, Sonderweg in Verteilung, Deckung oder Bericht —,
   trägt ihn im selben Auftrag in die Löschliste der Stufe GA im Umsetzungskonzept, Kapitel 6,
   ein. So bleibt der Umfang von GA vollständig, ohne dass er am Ende neu erhoben werden muss.

## Betrachtete Optionen

### Option A: Verzweigung im Bestandscode (bisheriger Plan, ADR-002)

| Dimension | Bewertung |
|---|---|
| Komplexität | niedrig am Anfang — zwei `if`, der Rest bleibt |
| Wartung | hoch — jede Änderung am Rumpf berührt beide Wege; der Rückweg-Test muss den gemeinsamen Rumpf prüfen |
| Oberfläche | ein Dialog mit zwei Zuständen (U2), Felder je nach Modell versteckt oder gesperrt |
| Ende | keines vorgesehen |

**Dafür:** kleinster erster Schritt. **Dagegen:** zwei Produkte in einem Rumpf, ohne vorgesehenes
Ende.

### Option B: Vollständige Trennung, Altweg als eingefrorenes Modul *(angenommen)*

| Dimension | Bewertung |
|---|---|
| Komplexität | mittel — Verschiebung, Fassade, Vorbereitungsschritt; 3–5 PT zusätzlich in G1 |
| Wartung | niedrig — der Altweg ist abgeschlossen, der VDI-Weg frei |
| Oberfläche | eine Struktur, ein eingeklappter Zusatzabschnitt „Tagesbilanz (Bestandsweg)" |
| Nachweis | Verschiebung byte-gleich gegen die Basis, dann Anbindung |
| Ende | Stufe GA, Zeitpunkt offen (Q24, E26) — bis dahin bleiben Referenzprojekt und Rückweg-Test |

**Dafür:** klare Grenzen, klare Oberfläche, ein abgeschlossener Altweg. **Dagegen:** ein zusätzlicher
Nachweisschritt in G1; zwei Rechenwege nebeneinander brauchen bis zur Ablösung Disziplin (kein
Feature im Altweg) und halten Referenzprojekt und Rückweg-Test in jeder Basis.

### Option C: Sofortige Entfernung des Tagesbilanz-Wegs mit G1 + G2

| Dimension | Bewertung |
|---|---|
| Komplexität | am niedrigsten im Kern — ein Weg |
| Anwender | kein Vergleich alt/neu, kein Rückweg; Bestandsprojekte springen ohne Brücke |
| Nachweis | Rückweg-Test entfällt sofort; GB wird gegenstandslos |

**Dafür:** nur ein Rechenweg. **Dagegen:** widerspricht E20 („nur als Übergang") und E23 („altweg
soll bleiben") und lässt den Anwender mit einem Zahlensprung von +7 bis +33 % ohne Vergleich zurück.

## Abwägung

A ist der Weg, den ADR-002 beschrieben hat, und er ist billig — aber nur am ersten Tag. C ist
sauber, aber hart gegenüber Bestandsprojekten und nimmt dem Anwender genau die Brücke, die er
verlangt hat. B kostet einen Nachweisschritt mehr und liefert dafür, was E20 verlangt: Trennung,
Ablösung, ein eingefrorener Bestandsweg für die Dauer des Übergangs (E20, E23, E26). Die Ablösung
selbst bleibt als Stufe GA im Plan. Die Weiche am Eingang ist die „eine Naht" aus
ADR-002 in konsequenter Form; ADR-002 bleibt gültig und bekommt einen Ergänzungsvermerk.

## Konsequenzen

- **Einfacher wird:** Der VDI-Weg kann ohne Rücksicht auf den Rumpf des Bestands gebaut werden;
  die Oberfläche hat eine Struktur; Tests des Dialogs kennen keinen Modellzustand mehr.
- **Schwerer wird:** ein zusätzlicher byte-gleicher Nachweis der Verschiebung in G1; der
  Abschnitt „Tagesbilanz (Bestandsweg)" im Dialog; zwei Rechenwege nebeneinander bis zur Stufe GA
  (E23, E26) — Referenzprojekt und Rückweg-Test in jeder Basis, gemeinsam genutzte Stellen
  zweifach zu prüfen. Das bleibt ein Risiko, solange der Übergang läuft.
- **Offen bleibt:** Q24 (wann GA beauftragt wird) und Q25 (Umfang der Stufe GA); der Vergleich
  alt/neu im Bedarfsdialog (G2) bleibt, solange es beide Wege gibt, und entfällt mit GA.
- **Gegenstandslos:** A16 (zweiter Verzweigungspunkt), U2 (Felder verstecken oder sperren).
- **Angepasst:** A15 — das Referenzprojekt auf dem Altweg bleibt bis zur Stufe GA in der jeweils
  aktuellen Basis, ebenso der Rückweg-Test (E23, E26); GA ist ein eigener Einfrieranlass.

## Aufgaben

1. [x] Entscheid des Anwenders (16.09.2026, E20); Konzept-Nachtrag N1.25; Ergänzungsvermerk in
       ADR-002.
2. [x] Befund X: Feldzuordnung Altweg / beide / VDI je Spalte und Dialogfeld; Aufrufstellen des
       Altwegs; was Brauchwasser und Prozesswärme mit dem Gebäudeweg teilen.
3. [ ] G1, erster Schritt: Altweg Zeichen für Zeichen nach `Altweg/` verschieben, Fassade und
       Vorbereitungsschritt bauen, Referenzlauf **byte-gleich**; erst dann den VDI-Weg anbinden.
4. [ ] G1 + G2: Dialoge in VDI-Struktur mit eingeklapptem Abschnitt „Tagesbilanz (Bestandsweg)";
       Schalter „Rechenweg"; Ausweis „Tagesbilanz (Bestandsweg)".
5. [x] Entscheide E23 (16.09.2026) und E26 (17.09.2026): Der Altweg bleibt jetzt und arbeitet als
       Übergang; die Ablösung ist die Stufe GA mit offenem Zeitpunkt. **Q24 und Q25 sind wieder
       offen** (Konzept N1.28, N1.31). Die Tagesverteilungstabellen `Tab_DBTagV` und
       `Tab_DBTagVDaten` behalten mit dem Altweg ihren Leser und entfallen mit GA; die leserlosen
       Spalten `WW_Bedarf` und `Waermebedarf` (Befund X) stehen in derselben Löschliste.
6. [ ] Löschliste der Stufe GA im Umsetzungskonzept, Kapitel 6, führen und bei jedem Auftrag
       fortschreiben, der einen Altweg-Sonderfall einführt (Entscheidung 6).
7. [ ] Ausbauprobe einrichten: statisch in der Modultrennungswache (außer der Weiche in
       `SimulationWaermebedarf`, dem Rückweg-Test und der Wache selbst nennt keine Datei des Kerns
       `Altweg/`) und als Gate von GA (ein Bau mit umbenanntem Ordner `Altweg/` übersetzt nach
       Entfernen der Weiche, und der Referenzlauf aller Projekte ohne Altweg-Gebäude bleibt
       byte-gleich).
