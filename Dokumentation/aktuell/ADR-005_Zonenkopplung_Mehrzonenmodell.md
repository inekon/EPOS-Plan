# ADR-005: Zonenkopplung im Mehrzonenmodell — Gauß-Seidel je Stunde über die Nachbarraum-Randbedingung

**Status:** Angenommen (16.09.2026, Entscheid E17 des Anwenders — Konzept-Nachtrag N1.22; damit sind M1 (Weg B) und M4 (Zonen-Luftaustausch in G6b) entschieden; vorgeschlagen am 15.09.2026 als Empfehlung M1 des Mehrzonenkonzepts)
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
   liegen bei **rund 1,1 bis 2,0 s je Gebäude und Jahr** — einschließlich des ungekoppelten
   Vorlaufs der 4-K-Zuordnung und des zweiten Vorlaufs aus der Konvergenzprobe
   ([Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md), 2.9). Tragbar, weil nur
   Mehrzonengebäude sie zahlen.
5. **Nachweisbarkeit:** Es gibt keinen Normtestfall für gekoppelte Zonen; der Nachweis
   muss aus Grenzfällen kommen (eine Zone = Einzonenmodell; zwei identische Zonen ohne
   Austausch = zwei Einzonenmodelle; exaktes Gesamtsystem für N = 2 als Orakel).

## Entscheidung

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

### Option B: Gauß-Seidel innerhalb der Stunde *(angenommen)*

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

1. [x] Entscheid des Anwenders zu M1 (Kopplungsweg B) und M4 (Zonen-Luftaustausch als Paare in
       G6b): angenommen am 16.09.2026, Entscheid **E17** (Konzept-Nachtrag N1.22); Status auf
       „Angenommen" gesetzt, Architekturfrage **A8** damit beantwortet, Sperrpunkt vor G6b
       aufgehoben. Die Messpflicht (Aufgabe 2 und die Probe „eine Zone bitgleich") gehört zum Entscheid.
2. [x] Prüforakel: 4×4-Gesamtsystem für zwei Zonen, exakt je Stunde diskretisiert — Probe 5b nach
       A8: höchstens 1,5·10⁻⁴ K bei 0 bis 400 m³/h, Kriterium < 0,001 K (E49, Konzept N1.55).
3. [x] G6b: Zonenschleife, Gruppenbildung, θ_NR,eq, Gauß-Seidel, Proben 1–12 samt 12a — umgesetzt am
       26.09.2026 (Konzept N1.56, [Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G6b_Mehrzonenrechnung.md));
       Probe 6 bestätigt die Wahl B gemessen (Vorstunde gegen Iteration höchstens 0,019 K,
       Jahresenergie 2,5·10⁻⁵).

**Vermerke aus der Umsetzung G6b.**

- **Abbruchkriterium mit Φ_c:** Neben der Heizlast bricht die Iteration erst ab, wenn auch keine
  Kühlleistung sich um mehr als 0,1 W ändert — eine gekühlte Zone ist so geregelt wie eine geheizte
  (Festlegung 6 des Auftrags, N1.56 Nr. 6).
- **Stundenmittel gegen augenblickliche Kopplung:** Die Kopplung tauscht je Stunde das Stundenmittel
  der Nachbartemperatur aus; die Trennwand wird je Seite reduziert, ihre Masse steht in beiden Zonen.
  Gegen eine wandaufgelöste Referenz mit der Trennwand als eigenem Massenknoten liegt die
  Jahresheizwärme um +0,044 % daneben, der Anteil der Dynamik höchstens 7·10⁻⁶; die Stundenlast
  weicht in der Phasenlage bis 256 W ab (mit Nachtabsenkung) und gleicht sich über das Jahr aus.
  Eine erhaltende Kopplung ist nicht verlangt (E49, K1 = V0;
  [Entwurf](../ueberholt/Entwurf_erhaltende_Zonenkopplung_G6b.md)).
- **Messung am echten Gebäude:** 50 Zonen rechnen in rund 0,55 s je Gebäude und Jahr, Durchläufe im
  Mittel 2,0, höchstens 3 — unter dem Ziel 1,1–2,0 s; die Obergrenze 50 bleibt bis G6c eine
  Pflegegrenze (M12).
