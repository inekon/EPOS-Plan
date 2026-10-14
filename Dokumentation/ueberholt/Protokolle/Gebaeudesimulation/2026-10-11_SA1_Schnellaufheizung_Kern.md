# Protokoll SA1 — Schnellaufheizung im Rechenkern (Raumsollwert-Überhöhung der Heizkurve)

**11.10.2026 · Sitzung Gebäudesimulation · Anwenderauftrag „setze um: Schnellaufheizung“, umgesetzt nach E128.** Grundlage:
[Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md) 2.1–2.7 (insbesondere 2.7
AK1/AK3, F17 Ankunftsbezug, F20 Sperrzeit), 5.1–5.3; aufgesetzt auf [AH-V3b](2026-10-11_AH_V3b_AK3_Auskunft_Ankunftsbezug.md),
[AH-V3c](2026-10-11_AH_V3c_Entscheide_F17_F20.md) und [AH-V4](2026-10-11_AH_V4_Schema215.md). Nur Rechenkern: kein Schema,
keine Oberfläche (SA2/SA3).

## Was gebaut ist

| Baustein | Inhalt |
|---|---|
| `Model/Vorheizvorgabe.cs` | `Schnellaufheizung` (Vorgabe aus) und `UeberhoehungK` (Δθ, Bereich > 0 … 10 K, leer = Kern-Konstante `UEBERHOEHUNG_VORGABE_K` = 2 K) als `init`; `SchnellaufheizungWirksam` nur mit „Vorheizzeit vorgeben“ und „Vorheizzeit berechnet“; `MitSchnellaufheizung`; die Abwandlungen tragen beide Felder mit |
| Plan (`Vorheizplanung.PlanBilden`) | an einer Zone mit wirksamem Heizkreis und Heizkurve je Fensterstunde Δθ und der Ankunftsbezug des Sprungs (`Vorheizplan.UeberhoehungReiheK`, `UeberhoehungAnkunftC`); `Aufheizoptimierung.PlanSetzen` setzt beide neben Sollwert- und Deckelreihe in den Eingang (`GebaeudeModellEingang.UeberhoehungSetzen`). Ohne Schalter kein Aufruf |
| Rand (`GebaeudeModellEingang.Rand`, `Stundenrand`) | in einer Fensterstunde der Kurvenvorlauf bei θ_T + Δθ **ohne** Deckel des Auslegungsvorlaufs (`Heizkurve.VorlaufUngedeckeltC`) samt Ankunftsbezug (`Stundenrand.MitUeberhoehung`); `MitVerfuegbarkeit` merkt das Vorlaufangebot der Anlage (`VorlaufAngebotC`) — in AK1/AK2 aus dem Fahrplan (`Vorlauf_Max`, sonst `Vorlauf` der Erzeuger), im AK3-Kreis aus der Anlagenkopplung |
| Löser (`Zonenmodell2K.Schritt`, `VersucheSchrittMitMuster`) | Ende bei Ankunft (unten); wirkt die Überhöhung, steigt der Vorlauf auf den überhöhten Kurvenwert, oben am Vorlaufangebot (ohne Angebot am Auslegungsvorlauf), nie darunter; an der Grenze Begrenzungsgrund `VorlaufAnlage`. Deckelreihe P_V/P_K und Verfügbarkeitsschranke unverändert |
| Vorausschau | `Vorheizvorausschau` rechnet die Fensterstunden mit derselben Überhöhung und derselben Entscheidung; θ_stat bleibt ohne Überhöhung (`AnkunftsschwelleC` mit dem Rand der Ankunftsprüfung) — die berechnete Vorheizzeit wird kürzer |
| Nachweis/Ergebnis | `Stundenergebnis` (`Ueberhoeht`, `UeberhoehungBegrenzt`, `UeberhoehungWirkungslos`) → `Zonenlauf` → `GebaeudeModellErgebnis` (Stundenreihen) → `Vorheiznachweis` je Zone und `Vorheizgebaeude` je Gebäude: Stunden mit Überhöhung, höchster Vorlauf darin, Stunden an der Vorlaufgrenze, wirkungslose Stunden, Fensterstunden mit Überhöhung in einer Sperrzeit, Zonen ohne Heizkreis |
| Hinweise (de/en) | `SIMENG_VORH_SCHNELL` (Kennzahlen), `SIMENG_VORH_SCHNELL_GRENZE` (wirkungslos an `Vorlauf_Max`), `SIMENG_VORH_SCHNELL_SPERRZEIT`, `SIMENG_VORH_SCHNELL_OHNE_HEIZKREIS`; `Resource.Designer.cs` neu geschrieben |
| Testnaht bis SA2 | `SimulationWaermebedarf.SchnellaufheizungTestnaht` (statisch, `Func<Vorheizvorgabe, Vorheizvorgabe>`): setzt Schalter und Δθ an der gelesenen Vorgabe des Projektlaufs; `null` = ohne Wirkung. SA2 ersetzt sie durch die Spalten |
| Tests | `SchnellaufheizungTests` (11 Fälle samt Messung) |

## Lösung für „Ende bei Ankunft“

**Entscheidung je Stunde am Zustand der Massen, im Löser.** Am Beginn einer Fensterstunde rechnet `Zonenmodell2K` die Raumluft
im Augenblick am Beginn der Stunde (`LuftAmBeginn`) mit dem Rand **ohne** Überhöhung — dasselbe Maß wie die Ankunft des
Nachweises und der Vorausschau (E125 F17 (b)) — und vergleicht sie über `Rechenrand.SchwelleErreicht` mit dem Bezug
min(θ_T, θ_stat) − ε des Sprungs. Ist er erreicht, rechnet die Stunde ohne Überhöhung (der Rand Zeichen für Zeichen wie ohne
Schalter), sonst mit dem überhöhten Vorlauf; spätestens die Sprungstunde selbst trägt keine Überhöhung mehr (sie liegt
außerhalb des Fensters). Begründung: Die Entscheidung braucht keinen Zustand außer den Massen, die ohnehin zwischen den Stunden
reisen — sie gilt damit ohne weitere Naht in Einzonenlauf, Zonenschleife, AK3-Kreis, Vorausschau und Probelauf, bei
Tageswiederholungen im Kreis und beim Einschwingen. Eine Umschaltung innerhalb der Stunde hätte einen zusätzlichen
Abschnittswechsel im Abschnittslöser mit einer Ereignissuche auf die Raumluft verlangt, ohne dass die stündlichen Ergebnisse
(Sprung, Ankunft, Deckel) davon gewinnen.

**Zonenschleife:** Eine Stunde wird dort mehrfach aus demselben Zustand gerechnet (Gauß-Seidel, Musterprobe), die Nachbarluft im
Rand ändert sich zwischen den Durchläufen. Ohne Vorkehrung kippte die Entscheidung an der Schwelle zwischen den Durchläufen
(1054 mit Δθ = 5 K: `ZonenkopplungKonvergiertNicht` in Stunde 629). Das Modell merkt deshalb die Entscheidung samt
Ausgangszustand (Massen, Bezug, Rohvorlauf, bitgenau) und nimmt sie für die Wiederholungen derselben Stunde: Die Entscheidung
fällt einmal, im ersten Durchlauf. Fällt die Raumluft nach der Ankunft innerhalb des Fensters wieder unter den Bezug, setzt die
Überhöhung in der nächsten Stunde wieder ein — dieselbe Regel, keine Hysterese.

## Messung (Option 2, Toleranz 20 %, ε 1 K, ganzer Projektlauf auf der Testkopie)

`SchnellaufheizungTests.Messung_SA1` mit `EPOS_MESSUNG=1`; Heizwärme ist die Summe der Gebäudeheizlast, WP-Strom/-Wärme die
Jahressummen der Wärmepumpe des Projekts, AZ deren Quotient; „Vorlauf max“ der höchste Heizkreisvorlauf des Jahres.

| Projekt | Δθ K | t_V h | Bedarf max h | Nächte o. Absenkung | Tage über t_V | Unterschr. K | Heizwärme MWh | WP-Strom MWh | AZ | Spitze kW | Vorlauf max °C | Std. überhöht | Std. an Vorlauf_Max |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1054 (AK1, 3 Zonen, Kessel) | aus | 12 | 18 | 365 | 12 | 3,97 | 52,232 | – | – | 23,8 | 55,0 | 0 | 0 |
| | 2 | 6 | 9 | 0 | 7 | 4,14 | 50,934 | – | – | 25,2 | 57,0 | 483 | 0 |
| | 3 | 5 | 8 | 0 | 9 | 4,14 | 50,517 | – | – | 26,3 | 58,0 | 446 | 0 |
| | 5 | 4 | 7 | 0 | 6 | 4,19 | 50,097 | – | – | 27,1 | 60,0 | 382 | 0 |
| 1047 (AK1, WP) | aus | 1 | 2 | 0 | 13 | 2,39 | 70,195 | 2,152 | 4,47 | 42,0 | 55,0 | 0 | 0 |
| | 2 | 1 | 1 | 0 | 0 | 2,39 | 70,645 | 2,401 | 4,39 | 42,0 | 55,0 | 190 | 8 |
| | 3 | 1 | 1 | 0 | 0 | 2,39 | 70,740 | 2,492 | 4,35 | 42,0 | 55,0 | 190 | 11 |
| | 5 | 1 | 1 | 0 | 0 | 2,39 | 70,826 | 2,599 | 4,27 | 42,0 | 55,0 | 190 | 54 |
| 1058 (AK3, WP mit Puffer) | aus | 1 | 2 | 0 | 13 | 2,31 | 70,679 | 12,831 | 4,28 | 42,0 | 55,0 | 0 | 0 |
| | 2 | 1 | 1 | 0 | 0 | 2,31 | 70,975 | 12,578 | 4,29 | 42,0 | 55,0 | 189 | 8 |
| | 3 | 1 | 1 | 0 | 0 | 2,31 | 71,056 | 12,505 | 4,30 | 42,0 | 55,0 | 189 | 11 |
| | 5 | 1 | 1 | 0 | 0 | 2,31 | 71,128 | 12,442 | 4,30 | 42,0 | 55,0 | 189 | 54 |
| 1065 (ohne Heizkreis, Gegenprobe) | aus/2/3/5 | 2 | 5 | 0 | 5 | 1,54 | 24,673 | 3,183 | 3,88 | 28,3 | – | 0 | 0 |

Wirkungslose Stunden (Vorlauf schon an der Grenze) und Fensterstunden in einer Sperrzeit: in allen Läufen 0 bzw. nicht gemeldet.
1065 bleibt in jeder Zahl gleich und meldet „ohne Wirkung“ an seiner Zone ohne Heizkreis.

**Lesart.** 1054: Ohne Überhöhung liegt t_V mit 12 h über der Werktagsabsenkung — jede Nacht wird durchgeheizt (365 Nächte).
Schon 2 K halbieren t_V und erhalten die Absenkung jeder Nacht; die Heizwärme sinkt dadurch um 2,5 % (3 K −3,3 %, 5 K −4,1 %),
die Spitze steigt um 6 % (10 %, 14 %). Die Tage über t_V und die Unterschreitung um 4 K stammen von der Zone, deren Übergabe den
Sollwert stationär nicht hält (Ankunftsbezug θ_stat); sie ändern sich mit Δθ kaum. 1047/1058: t_V bleibt 1 h, die 13 Tage über
t_V verschwinden schon mit 2 K. Der Preis: Heizwärme +0,4 bis +0,9 %; in 1047 steigt der Strom der Wärmepumpe um 11,6 % (2 K),
15,8 % (3 K), 20,8 % (5 K) und die Arbeitszahl fällt von 4,47 auf 4,39/4,35/4,27, die Stunden an `Vorlauf_Max` (55 °C) wachsen
von 8 auf 11 und 54. In 1058 (Kreis mit Puffer) übernimmt der Kessel einen Teil der überhöhten Fensterstunden — WP-Strom und
WP-Wärme sinken leicht, die Arbeitszahl bleibt bei 4,3.

## Empfohlene Vorgabe Δθ = 2 K (gesetzt als `Vorheizvorgabe.UEBERHOEHUNG_VORGABE_K`)

Die kleinste gemessene Überhöhung erreicht bereits beide Ziele der Schnellaufheizung: Sie erhält in 1054 die Nachtabsenkung
jeder Nacht (t_V 12 → 6 h, Heizwärme −2,5 %) und beseitigt in 1047 und 1058 jeden Tag über t_V. 3 und 5 K kürzen t_V nur noch um
eine bzw. zwei Stunden und sparen in 1054 weitere 0,8 bzw. 1,6 % Wärme, heben aber an der Wärmepumpe den Strom gegen 2 K um
weitere 4 bzw. 8 %, senken die Arbeitszahl und laufen öfter an `Vorlauf_Max` (11 bzw. 54 statt 8 Stunden); die Spitze steigt.
Vorheizzeit, Nachtabsenkung, Wärme und WP-Strom zusammen sprechen für 2 K; ein größerer Wert bleibt Eingabe (SA2/SA3).

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release` → 0 Fehler.
- Tests: siehe Bericht (Filter Schnellauf*, Vorheiz*, Aufheiz*, Zonen*, Anlagenkopplung*, Ak3*, GebaeudeModell*, GebaeudeStepper*,
  Stundenrand*, Kappungsanteil*, SimulationWaermebedarf*, alle Wachen samt `*ReferenzprojektWacheTests`, Ressourcen, Designer).
- Referenzlauf aller 30 Projekte gegen `Referenzlaeufe/2026-10-11_R52_Vorheizen`: GESAMT PASS, 979/979 CSV byte-gleich (Schalter
  aus ⇒ bitgleich; keine Basis neu).

## Befund am Rande

Die Vorlaufreihe eines gekoppelten Eingangs (`GebaeudeModellEingang.VorlaufC`, Schritt E) entsteht beim Bauen aus der
Sollwertreihe **ohne** Rampe; `HeizsollwertMitRampeSetzen` bildet sie nicht neu. Im Vorheizfenster fährt die Heizkurve deshalb
heute am abgesenkten Sollwert, nicht an θ_T, während der Raumregler θ_T sieht. Die Schnellaufheizung rechnet die Kurve im Fenster
ausdrücklich bei θ_T + Δθ und ist davon nicht berührt; der Weg ohne Schalter bleibt, wie er ist (Basis R52).

## Offene Punkte für SA2–SA4

- **SA2 (Schema/Datenweg):** zwei Spalten an `Tab_Einstellungen` (Schalter 0/1 mit `CHECK`, Überhöhung K, leer = Vorgabe), Lesen
  und Schreiben in `KonfigurationCtrl.VorheizvorgabeLesen`/`AufheizvorgabeSetzen`, Testnaht `SchnellaufheizungTestnaht` entfernen,
  Schemaschritt 217 (angemeldet).
- **SA3 (Oberfläche/Bericht):** Schalter und Δθ in der Gruppe Aufheizoptimierung (nur mit Option 1/2, Hinweis „nur mit
  Heizkreis“), Kennzahlen der Schnellaufheizung in Ergebniszeile, Bericht und Export (Export-Schlüssel neu ⇒ Referenz-CSV; bis
  dahin bewusst nicht exportiert), Wiki-Quelle und Logbuch-Entwurf.
- **SA4 (Rechenweg/Referenz):** gemeinsamer Vorlauf im Mehrzonen-Heizkreis — die Überhöhung wirkt heute je Zone an ihrer Übergabe;
  der Gebäudevorlauf als Maximum über die Zonen mit Überhöhung wäre physikalisch näher (Entscheid). Ob der Kurvenvorlauf im
  Fenster ohne Schalter θ_T statt des abgesenkten Sollwerts folgen soll (Befund oben; neue Basis). Ein Referenzprojekt mit
  Schnellaufheizung (z. B. Kopie von 1054 oder 1047) samt Wache und Einfrierregel.
