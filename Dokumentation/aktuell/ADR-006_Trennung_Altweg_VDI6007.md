# ADR-006: Ablösung des Tagesbilanz-Wegs — zwei getrennte Rechenwege, der Altweg nur als Übergang

**Status:** Angenommen (16.09.2026, Entscheid E20 des Anwenders — Konzept-Nachtrag N1.25)
**Datum:** 16.09.2026
**Entscheider:** Anwender (Projektverantwortung EPOS-Plan)
**Betrifft:** [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Nachtrag N1.1, N1.25, Kapitel 11 und 13), [`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Kapitel 1, 2 und 5), [`Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md`](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md)
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
(Frage U2), und ein Referenzprojekt gemacht, das dauerhaft auf Tagesbilanz steht (Frage A15).

Am 16.09.2026 hat der Anwender entschieden (E20): Die neue und die alte Berechnung werden
**vollständig getrennt**, die neue **löst die alte ab**, die alte bleibt **nur als Übergang**, und
**alle Dialoge und Eingaben** folgen der künftig alleinigen VDI-6007-Struktur.

### Kräfte, die die Entscheidung formen

1. **Zwei Rechenwege in einem Rumpf verlangsamen jede Änderung**: Jede Zeile des VDI-Wegs muss
   beweisen, dass sie den Tagesbilanz-Zweig nicht berührt, und der Rückweg-Test wird dauerhaft
   gebraucht. Getrennte Module machen diesen Beweis zu einer Frage der Aufrufstruktur.
2. **Die Oberfläche kann nicht zwei Strukturen tragen**: Ein Dialog, der je nach Modell Felder
   versteckt, sperrt oder umdeutet, ist für den Anwender ein Rätsel und für die Tests ein
   Zustandsraum (U2). Ein Dialog in einer Struktur mit einem befristeten Zusatzabschnitt ist beides
   nicht.
3. **Bestandsprojekte brauchen einen Übergang**: Nach E1 liefern alle Projekte andere Zahlen; der
   Anwender will den alten Wert eine Zeit lang daneben sehen und den Bestandsweg ausdrücklich
   wählen können. Ohne Ende bliebe das aber ein zweites Produkt im Produkt.
4. **Die Referenzbasis ist die Abnahme**: Die Verschiebung des Altwegs darf kein Ergebnis ändern;
   sie muss byte-gleich nachweisbar sein, bevor der VDI-Weg angebunden wird.

## Entscheidung

1. **Eine Weiche, zwei Module.** `SimulationWaermebedarf` wird zur Fassade: Ein modellfreier
   Vorbereitungsschritt liefert, was beide Wege brauchen (Bewohner aus der Nutzfläche,
   Skalierungsfaktor nach E8, Klimareihen); dann liest die Weiche den Rechenweg des Gebäudes und
   ruft genau ein Modul. Modellfrei sind Klimakalender, Bewohner, die beiden Flächen und der
   Flächenfaktor; die Verbrauchs-Rückrechnung braucht ein Ergebnis des gewählten Moduls und läuft
   innerhalb des Moduls (zweiter Aufruf), nie über die Modulgrenze (Befund X, X5). Der Tagesbilanz-Weg wandert **Zeichen für Zeichen** nach
   `EPOS.Kern/Allgemein/Simulation/Altweg/` und bekommt keine Funktion mehr; der VDI-Weg lebt in
   `Gebaeude/` und ruft nichts aus dem Altweg. Es gibt keinen zweiten Verzweigungspunkt (A16
   gegenstandslos).
2. **Oberfläche in VDI-Struktur.** Gebäudedialog, Katalogdialog, Skalierungsdialog und
   Bedarfsdialog werden nach den Eingaben des VDI-Wegs aufgebaut; die Modellparameter sind immer
   sichtbar und bearbeitbar. Felder, die nur der Altweg liest, erscheinen allein bei einem Gebäude
   auf dem Altweg in einem eingeklappten Abschnitt „Übergang: Tagesbilanz" und entfallen mit ihm.
   Der Schalter heißt „Rechenweg" mit Vorgabe „VDI 6007" und Wert „Tagesbilanz (Übergang)".
3. **Kein zweites Datenmodell.** Der Gebäudespalten-Schritt bringt die VDI-Spalten und die
   Umbenennung nach E19; Altweg-Spalten bleiben bis zum Ende des Übergangs unangetastet. Ein
   Befund je Feld weist die Klasse aus (nur Altweg, beide, nur VDI).
4. **Der Übergang endet mit Stufe GA.** Sie entfernt Modul, Weiche, Schalter und Altweg-Spalten
   (Schemaschritt mit `DROP COLUMN` und Sichtneubau), stellt das Referenzprojekt des Übergangs um,
   stellt den Rückweg-Test ein und friert die Basis neu ein. Der Zeitpunkt ist Frage Q24;
   Empfehlung frühestens nach G3, wenn alle Referenz- und Bestandsprojekte einmal auf VDI 6007
   gerechnet und geprüft sind. Bis dahin: keine Änderung am Altweg außer Fehlerbehebung.
5. **Ausweis.** Ein Gebäude auf dem Altweg trägt „Tagesbilanz (Übergangsweg)" statt des
   Produktausweises nach E10.

## Betrachtete Optionen

### Option A: Verzweigung im Bestandscode (bisheriger Plan, ADR-002)

| Dimension | Bewertung |
|---|---|
| Komplexität | niedrig am Anfang — zwei `if`, der Rest bleibt |
| Wartung | hoch — jede Änderung am Rumpf berührt beide Wege; der Rückweg-Test bleibt für immer |
| Oberfläche | ein Dialog mit zwei Zuständen (U2), Felder je nach Modell versteckt oder gesperrt |
| Ende | keines vorgesehen |

**Dafür:** kleinster erster Schritt. **Dagegen:** zwei Produkte in einem Rumpf, dauerhaft.

### Option B: Vollständige Trennung, Altweg als befristetes Modul *(angenommen)*

| Dimension | Bewertung |
|---|---|
| Komplexität | mittel — Verschiebung, Fassade, Vorbereitungsschritt; 3–5 PT zusätzlich in G1, 5–8 PT für GA |
| Wartung | niedrig — der Altweg ist abgeschlossen, der VDI-Weg frei |
| Oberfläche | eine Struktur, ein befristeter Zusatzabschnitt |
| Nachweis | Verschiebung byte-gleich gegen die Basis, dann Anbindung |
| Ende | Stufe GA, Zeitpunkt Q24 |

**Dafür:** klare Grenzen, klare Oberfläche, ein Ende. **Dagegen:** ein zusätzlicher Nachweisschritt
in G1; der Übergang braucht Disziplin (kein Feature im Altweg).

### Option C: Sofortige Entfernung des Tagesbilanz-Wegs mit G1 + G2

| Dimension | Bewertung |
|---|---|
| Komplexität | am niedrigsten im Kern — ein Weg |
| Anwender | kein Vergleich alt/neu, kein Rückweg; Bestandsprojekte springen ohne Brücke |
| Nachweis | Rückweg-Test entfällt sofort; GB wird gegenstandslos |

**Dafür:** kein Übergang zu pflegen. **Dagegen:** widerspricht dem Wort „nur als Übergang" und
lässt den Anwender mit einem Zahlensprung von +7 bis +33 % ohne Vergleich zurück.

## Abwägung

A ist der Weg, den ADR-002 beschrieben hat, und er ist billig — aber nur am ersten Tag. C ist
sauber, aber hart gegenüber Bestandsprojekten und nimmt dem Anwender genau die Brücke, die er
verlangt hat. B kostet einen Nachweisschritt mehr und liefert dafür, was E20 verlangt: Trennung,
Ablösung, Übergang mit Ende. Die Weiche am Eingang ist die „eine Naht" aus ADR-002 in
konsequenter Form; ADR-002 bleibt gültig und bekommt einen Ergänzungsvermerk.

## Konsequenzen

- **Einfacher wird:** Der VDI-Weg kann ohne Rücksicht auf den Rumpf des Bestands gebaut werden;
  die Oberfläche hat eine Struktur; Tests des Dialogs kennen keinen Modellzustand mehr.
- **Schwerer wird:** ein zusätzlicher byte-gleicher Nachweis der Verschiebung in G1; der
  Übergangsabschnitt im Dialog; die Stufe GA samt Schemaschritt und Neu-Einfrieren.
- **Offen bleibt:** Q24 (Zeitpunkt von GA). Der Vergleich alt/neu im Bedarfsdialog (G2) ist
  Übergangshilfe und entfällt mit GA.
- **Gegenstandslos:** A16 (zweiter Verzweigungspunkt), U2 (Felder verstecken oder sperren).
- **Angepasst:** A15 — das Referenzprojekt steht für die Dauer des Übergangs auf dem Altweg.

## Aufgaben

1. [x] Entscheid des Anwenders (16.09.2026, E20); Konzept-Nachtrag N1.25; Ergänzungsvermerk in
       ADR-002.
2. [x] Befund X: Feldzuordnung Altweg / beide / VDI je Spalte und Dialogfeld; Aufrufstellen des
       Altwegs; was Brauchwasser und Prozesswärme mit dem Gebäudeweg teilen.
3. [ ] G1, erster Schritt: Altweg Zeichen für Zeichen nach `Altweg/` verschieben, Fassade und
       Vorbereitungsschritt bauen, Referenzlauf **byte-gleich**; erst dann den VDI-Weg anbinden.
4. [ ] G1 + G2: Dialoge in VDI-Struktur mit eingeklapptem Übergangsabschnitt; Schalter
       „Rechenweg"; Ausweis „Tagesbilanz (Übergangsweg)".
5. [ ] Q24 entscheiden; Stufe GA ausführen (Modul, Weiche, Schalter, Spalten, Referenzprojekt,
       Rückweg-Test, Basis, Logbuch). Dabei prüfen, ob die Tagesverteilungstabellen
       `Tab_DBTagV` und `Tab_DBTagV_Daten` mit dem Altweg ihren letzten Leser verlieren und mit
       entfallen (Befund X).
