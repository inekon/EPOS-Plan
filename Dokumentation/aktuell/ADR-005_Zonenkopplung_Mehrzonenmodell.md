# ADR-005: Zonenkopplung im Mehrzonenmodell — Gauß-Seidel je Stunde über die Nachbarraum-Randbedingung

**Status:** Vorgeschlagen (15.09.2026, Empfehlung M1 des Mehrzonenkonzepts; Gegenlesen des Papiers läuft, Entscheid des Anwenders steht aus)
**Datum:** 15.09.2026
**Entscheider:** Anwender (Projektverantwortung EPOS-Plan)
**Betrifft:** [`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md)
(Kapitel 2, 9 und 10), [`Gebaeudesimulation/2026-09-15_Befund_O_Zonenkopplung_VDI6007.md`](Gebaeudesimulation/2026-09-15_Befund_O_Zonenkopplung_VDI6007.md),
Nachtrag N1.12 des [Konzepts](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (Entscheid E7)
**Berührte Bereiche:** `EPOS.Kern/Allgemein/Simulation/`, Datenmodell Zone → Bauteil → Aufbau → Schicht → Baustoff

---

## Kontext

Nach Entscheid E7 rechnet EPOS-Plan zuerst ein Einzonenmodell je Gebäude (Stufen G0 bis G2);
ein Mehrzonenmodell entsteht als spätere Stufe G6 und wird über den IFC-Import gespeist.
VDI 6007 Blatt 1 (Abschnitt 5.3) und VDI 2078 (Abschnitt 6.2) nehmen die Zonierung eines
Gebäudes ausdrücklich aus ihrem Gegenstand aus; geregelt ist allein die **Randbedingung eines
einzelnen Raums zu einem Nachbarraum**: dessen Bauteile gehören in die AW-Gruppe, seine
Lufttemperatur geht als äquivalente Temperatur θ_NR,eq nach Gl. (40) ein und wird mit U·A
nach Gl. (41)/(42) gewichtet (Befund O). Ein Mehrzonenmodell ist also eine EPOS-Erweiterung;
der Produktausweis bleibt „Rechenkern nach VDI 6007 Blatt 1" **je Zone**.

Die IFC-Dateien liefern die Zonentopologie nicht (keine `IfcZone` in vier gemessenen
Dateien), wohl aber Raumgrenzen mit `CorrespondingBoundary`, aus denen die Nachbarschaft
und die Trennflächen gewinnbar sind (Befund P). Das Mehrzonenkonzept sieht bis zu 50 Zonen
je Gebäude vor, unbeheizte Zonen (Keller, Treppenhaus) mit eigener Temperatur und einen
Luftaustausch zwischen Zonenpaaren.

### Kräfte, die die Entscheidung formen

1. **Das validierte 7R2C-Netz je Zone darf sich nicht ändern**; die Kopplung darf nur in
   der Randbedingung θ_A,eq,gew sitzen. Ein Gebäude ohne Zonendaten muss bitgleich wie die
   Einzonenrechnung desselben Programmstands (nach G3) rechnen.
2. **Ideale Regelung mit Umschaltzeitpunkt:** die Bisektion des Umschaltpunkts macht das
   System innerhalb der Stunde stückweise linear; ein gemeinsames Zustandssystem aller Zonen
   müsste bei jedem Umschalten einer Zone neu zerlegt werden.
3. **Luftaustausch zwischen Zonen** koppelt die Luftknoten unmittelbar und ist der Grund,
   warum eine Kopplung über die Vorstunde zu weit hinkt.
4. **Rechenzeit:** rund 5 ms je Zone und Jahr; 50 Zonen mit mehreren Durchläufen je Stunde
   liegen bei 0,75 bis 1,5 s je Gebäude und Jahr — tragbar, weil nur Mehrzonengebäude sie zahlen.
5. **Nachweisbarkeit:** Es gibt keinen Normtestfall für gekoppelte Zonen; der Nachweis
   muss aus Grenzfällen kommen (eine Zone = Einzonenmodell; zwei identische Zonen ohne
   Austausch = zwei Einzonenmodelle; exaktes Gesamtsystem für N = 2 als Orakel).

## Entscheidung (vorgeschlagen)

1. **Kopplung über die Nachbarraum-Randbedingung:** Trennbauteile zur Nachbarzone gehören
   in die AW-Gruppe der Zone; die Nachbartemperatur geht als θ_NR,eq nach Gl. (40) ein und
   wird nach Gl. (41)/(42) gewichtet; der Strahlungsaustausch endet an der Zonengrenze.
2. **Gauß-Seidel innerhalb der Stunde:** Die Zonen werden in fester Reihenfolge je Stunde
   durchlaufen, jede Zone rechnet mit den jüngsten Nachbartemperaturen; Abbruch, wenn sich
   keine Zonenlufttemperatur um mehr als 0,01 K und keine Zonenheizlast um mehr als 0,1 W
   ändert — in geregelten Zonen ist die Last das aussagekräftige Maß —, spätestens nach
   50 Durchläufen mit benanntem Fehler (beteiligte Zonen und Volumenstrom). Wechselt die
   Regelungszuordnung einer Zone zwischen zwei Durchläufen, wird das Muster des ersten
   Durchlaufs für diese Stunde festgehalten und der Wechsel im Protokoll gezählt.
3. **Zonen-Luftaustausch** als Paare mit `CHECK (ID_ZoneA < ID_ZoneB)`, im selben Durchlauf
   berücksichtigt.
4. **Nachweis:** Vorschlag A (Vorstunde) als Vergleichsrechnung, das 4×4-Gesamtsystem für
   N = 2 als exaktes Prüforakel; Probe „eine Zone bitgleich zum Einzonenmodell" als Gate.

## Betrachtete Optionen

### Option A: Kopplung über die Vorstunde (explizit)

Jede Zone rechnet mit den Nachbartemperaturen der vergangenen Stunde.

| Dimension | Bewertung |
|---|---|
| Komplexität | niedrig — eine Schleife, kein Iterieren |
| Genauigkeit | Verzug von einer Stunde je Kopplung; bei Luftaustausch spürbar |
| Rechenzeit | N × Einzonenmodell |
| Stabilität | bei starkem Luftaustausch Schwingungen möglich |

**Dafür:** einfach, deterministisch. **Dagegen:** Treppenhaus und offene Küche (großer
Luftaustausch) rechnen falsch; nur haltbar, wenn der Luftaustausch gestrichen wird (Frage M4).

### Option B: Gauß-Seidel innerhalb der Stunde *(vorgeschlagen)*

| Dimension | Bewertung |
|---|---|
| Komplexität | mittel — Iteration, Schwelle, Abbruchregel, feste Reihenfolge |
| Genauigkeit | konvergiert gegen die gleichzeitige Lösung der Stunde |
| Rechenzeit | N × Durchläufe (typisch 3 bis 6) × Einzonenmodell |
| Stabilität | Konvergenz bei diagonaldominanter Kopplung; Nichtkonvergenz wird benannt |

**Dafür:** das Zonennetz bleibt unverändert, die Kopplung bleibt in der Randbedingung; der
Luftaustausch ist darstellbar. **Dagegen:** Iteration je Stunde; Reihenfolgeabhängigkeit
der Zwischenwerte (nicht des Ergebnisses innerhalb der Schwelle).

### Option C: Gemeinsames Zustandssystem aller Zonen (2N × 2N)

Alle Zonen in einer Systemmatrix, eine exakte Diskretisierung je Stunde.

| Dimension | Bewertung |
|---|---|
| Komplexität | hoch — Matrixexponential 2N × 2N, Neuzerlegung bei jedem Umschaltpunkt einer Zone |
| Genauigkeit | exakt für stückweise konstante Eingänge |
| Rechenzeit | Zerlegungen je Umschaltpunkt; bei 50 Zonen viele Umschaltpunkte je Stunde |
| Nachweis | ideal als Orakel für N = 2 |

**Dafür:** exakt. **Dagegen:** die ideale Regelung mit Bisektion des Umschaltpunkts je Zone
sprengt die Struktur; das validierte Einzonennetz wäre nicht mehr Zeichen für Zeichen dasselbe.

## Abwägung

C ist das Orakel, nicht der Produktweg: exakt, aber mit der Umschaltlogik je Zone
unverträglich. A ist der billige Weg und reicht, solange es keinen Luftaustausch zwischen
Zonen gibt — den aber braucht jedes Treppenhaus. B hält das validierte Zonennetz
unverändert, bringt den Luftaustausch unter und kostet Iterationen, deren Zahl bei üblichen
Gebäuden klein bleibt. Die Wahl wird nicht vorausgesetzt, sondern gemessen: die Probe mit
zwei Zonen hält B gegen C, und A bleibt als Vergleichsrechnung stehen, damit der Gewinn
sichtbar ist.

## Konsequenzen

- **Einfacher wird:** unbeheizte Zonen bekommen eine eigene Temperatur (Kellertemperatur
  sichtbar); Zonen aus dem IFC-Import lassen sich koppeln, ohne ein zweites Modell zu bauen.
- **Schwerer wird:** eine Abbruchregel mit benanntem Fehler; die Reihenfolge der Zonen ist
  festzulegen; der Strahlungsaustausch endet an der Zonengrenze — ein Argument für große
  Zonen (Mindestgröße, 4-K-Regel).
- **Normstatus:** „Mehrzonensimulation nach VDI 6007" wäre falsch; Ausweis je Zone, die
  Kopplung als EPOS-Erweiterung benannt (wie Kusuda und Hay-Davies).
- **Später zu prüfen:** Vorlauf 30 Tage mit Konvergenzprobe; Obergrenze 50 Zonen;
  Rechenzeit bei vielen Zonen.

## Aufgaben

1. [ ] Entscheid des Anwenders zu M1 (Kopplungsweg) und M4 (Zonen-Luftaustausch) nach
       Abschluss des Gegenlesens des Mehrzonenkonzepts; dann Status dieses ADR auf
       „Angenommen" setzen oder Option ändern.
2. [ ] Prüforakel: 4×4-Gesamtsystem für zwei Zonen im Testprojekt.
3. [ ] G6b: Zonenschleife, Gruppenbildung, θ_NR,eq, Gauß-Seidel, Proben 1–12.
