# Protokoll KP0 — Reserve-Probe der Aufheizleistung an den Referenzgebäuden (27.09.2026)

**Kopf.** Letzter offener Punkt der Stufe KP0 (Teilkonzept
[Konditionierungsprofile](../../../aktuell/Konzept_Konditionierungsprofile_EPOS-Plan.md) 4.4, „KP0-Probe
(R2)"; Leitkonzept [N1.59/N1.60](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)): Der
Anwender hat die Aufheizleistung P_auf = (1 + ρ) · Φ_stat an der kältesten Stunde mit ρ = 20 % als Startwert
entschieden (P5, E52) und vor KP1 eine Probe an den Referenzgebäuden verlangt, weil die Rampe nur in gut
gedämmten Bauten oder bei knapper `Heizleistung_Max` wirkt (Konzept 4.5). Diese Probe rechnet ρ und die
Bemessungsvarianten (a) kälteste Stunde und (b) kälteste Stunde − ΔT_K an allen fünfzehn Referenzprojekten
durch und trägt den Befund in Konzept 4.4 nach.

## 1 Vorgehen

Grundlage sind die Formeln aus Konzept 4.3–4.5 (Stufenformel in Gleichgewichtsform, P_auf, Bemessung (a)/(b))
und die vorhandenen Gegenprüfungs-Skripte der Sitzung (`gp_physik.py` für die Stufenformel und die
Modalgrößen des geregelten Falls, `gp_klima.py`/`synthese_klima.py` für kälteste Stunde und kältestes
Tagesmittel je Klimaregion, `gp_ref.py` für die Referenzgebäude-Abfrage, `synthese_beispiel.py`/
`synthese_beispiel2.py` für das Zahlenbeispiel an Projekt 1045). Daraus zusammengesetzt: `kp0_reserveprobe.py`
(Scratchpad der Sitzung, nicht im Repositorium).

Die Probe rechnet **nicht** das volle VDI-6007-Bauteilnetz jedes Gebäudes neu — das ist Kernlogik
(`Zonenmodell2K`/`GebaeudeModellEingang`) und wird hier nicht nachgebaut. Stattdessen:

- **H_s (stationärer Leitwert)** je Gebäude aus den vorhandenen Datenbankfeldern: Σ (U-Wert × Fläche) für
  Außenwand, Fenster, Dach, Grundfläche und Sonstiges, zuzüglich der Lüftung (0,34 Wh/(m³·K) ×
  Luftwechselrate × Nutzfläche × Raumhöhe) — die Standardformel Transmission + Lüftung, ohne
  Randbedingungskorrekturen (Grundflächen-Randbedingung, Innenflächenfaktor je Gebäude), die der Kern führt,
  diese Probe aber nicht einzeln nachbildet. Kalibriert mit einem **einzigen** Faktor auf den an Projekt 1045
  validierten Wert (H_s = 971,8 W/K, Rechenschritte 9.1/9.2, Gegenprüfung A-3): Faktor 1,029 (die rohe Formel
  liefert 944,7 W/K).
- **C_w (Überschusswärme je Kelvin)** aus dem Datenbankfeld `Tab_Gebaeude.Bauweise` (Speicherfähigkeit der
  Bauweise in Wh/K, an Projekt 1045 10,05 kWh/K, Konzept 4.2) mit dem an Projekt 1045 validierten Anteil
  „wirksame Kapazität / Speicherfähigkeit der Bauweise" (5,78 von 10,05 kWh/K), auf jedes Gebäude über sein
  eigenes `Bauweise`-Feld übertragen; C_1/C_2 im selben Anteil wie an Projekt 1045 (0,24/5,78 bzw. 5,55/5,78).
- **τ_1 = 1,02 h, τ_2 = 4,54 h** unverändert aus der Gegenprüfung, für alle Gebäude übernommen — die Probe
  variiert die Netzform nicht, nur H_s und C_w je Gebäude. Das ist die Näherung dieser Probe.
- **Standardsprung:** Nachtabsenkung → Tag, mit den Werten des jeweiligen Gebäudes; an allen 17
  Referenzgebäuden steht in der Testdatenbank `Raumsolltemperatur_Nachtabsenkung` = 18 °C und
  `Raumsolltemperatur_Tag` = 20 °C (ΔT = 2 K), Absenkdauer D = 8 h (`Nachtabsenkung_Beginn`/`_Ende` leer, Vorgabe
  22–6 Uhr).
- **Kälteste Stunde und kältestes Tagesmittel** je Projekt aus `Tab_Solar`, geführt über die Klimaregion des
  Projekts (`Tab_Projekt.ID_Klimaregion`), wie in `gp_klima.py`/`synthese_klima.py`.

**Gegenprobe der Kalibrierung:** Mit dem Sprung 17 → 21 °C (ΔT = 4 K) aus dem Zahlenbeispiel von Konzept 4.5
liefert dasselbe kalibrierte H_s/C_w für Projekt 1045 eine Überhöhung von rund 13 % — das Zahlenbeispiel des
Papiers nennt „+13 %" an derselben Stelle (Konzept 4.5, Zeile „(a) −18,2 °C"). Die Kalibrierung ist damit an der
einzigen unabhängig nachgerechneten Stelle bestätigt.

Fünfzehn Referenzprojekte (Liste [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md)):
1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047, 1049 — davon 1030 ohne
Gebäude (17 Gebäude in vierzehn Projekten mit Gebäude). Projekt 1040 rechnet bis zur Stufe GA auf dem
Tagesbilanz-Weg (Altweg) und bleibt ohne Aufheizrechnung — wie AK1-gekoppelte Einzonengebäude in KP3 (F13) ist
es hier benannt ausgenommen, nicht gerechnet.

## 2 Tabelle

Je Gebäude: Klimaregion-Zeitkonstanten τ_1/τ_2 unverändert aus der Gegenprüfung, C_w kalibriert aus der
Bauweise, Φ_stat an der kältesten Stunde und am kältesten Tagesmittel (nur zur Einordnung), die Sprungspitze
ohne Rampe für den Standardsprung, ihre Überhöhung gegenüber Φ_stat, die Aufheizleistung P_auf bei ρ = 20 %,
die Höchstzeit t_max in Bemessung (a) und (b) und ρ_min — die Reserve, ab der (a) ohne Rampe (n = 1) auskäme.
Gebäude nur mit Projektnummer und Baualtersklasse (Buchstabe), keine Katalog- oder Herstellerbezeichnung.

| Projekt | Klasse | τ_1/τ_2 [h] | C_w [kWh/K] | Φ_stat kälteste Std. [kW] | Φ_stat kältestes TM [kW] | Spitze ohne Rampe [kW] | Überhöhung [%] | P_auf bei ρ=20 % [kW] | t_max (a) [h] | t_max (b) [h] | ρ_min [%] |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1007 | B | 1,02/4,54 | 2,13 | 8,76 | 7,11 | 9,68 | 10,5 | 10,51 | 0 | 0 | 10,5 |
| 1008 (Gebäude 1) | I | 1,02/4,54 | 8,74 | 20,01 | 16,25 | 23,78 | 18,9 | 24,01 | 0 | 2 | 18,9 |
| 1008 (Gebäude 2) | B | 1,02/4,54 | 2,13 | 8,76 | 7,11 | 9,68 | 10,5 | 10,51 | 0 | 0 | 10,5 |
| 1017 | E | 1,02/4,54 | 21,41 | 55,42 | 45,01 | 64,66 | 16,7 | 66,51 | 0 | 1 | 16,7 |
| 1018 | H | 1,02/4,54 | 56,80 | 131,60 | 121,43 | 156,11 | 18,6 | 157,91 | 0 | 3 | 18,6 |
| 1023 | G | 1,02/4,54 | 103,41 | 284,00 | 230,65 | 328,63 | 15,7 | 340,80 | 0 | 1 | 15,7 |
| 1024 | G | 1,02/4,54 | 103,41 | 284,00 | 230,65 | 328,63 | 15,7 | 340,80 | 0 | 1 | 15,7 |
| 1039 (Gebäude 1) | B | 1,02/4,54 | 7,48 | 26,66 | 21,65 | 29,89 | 12,1 | 32,00 | 0 | 0 | 12,1 |
| 1039 (Gebäude 2) | B | 1,02/4,54 | 5,78 | 48,76 | 39,60 | 51,25 | 5,1 | 58,51 | 0 | 0 | 5,1 |
| 1039 (Gebäude 3) | G | 1,02/4,54 | 103,41 | 284,00 | 230,65 | 328,63 | 15,7 | 340,80 | 0 | 1 | 15,7 |
| 1040 | B | — | — | — | — | — | — | — | Altweg, nicht anwendbar | Altweg, nicht anwendbar | — |
| 1041 | B | 1,02/4,54 | 5,78 | 37,09 | 30,13 | 39,59 | 6,7 | 44,51 | 0 | 0 | 6,7 |
| 1042 | B | 1,02/4,54 | 5,78 | 37,09 | 30,13 | 39,59 | 6,7 | 44,51 | 0 | 0 | 6,7 |
| 1045 | B | 1,02/4,54 | 5,78 | 37,09 | 30,13 | 39,59 | 6,7 | 44,51 | 0 | 0 | 6,7 |
| 1046 | B | 1,02/4,54 | 2,13 | 8,76 | 7,11 | 9,68 | 10,5 | 10,51 | 0 | 0 | 10,5 |
| 1047 | E | 1,02/4,54 | 21,41 | 55,42 | 45,01 | 64,66 | 16,7 | 66,51 | 0 | 1 | 16,7 |
| 1049 | H | 1,02/4,54 | 56,80 | 131,60 | 121,43 | 156,11 | 18,6 | 157,91 | 0 | 3 | 18,6 |

**Zusammenfassung** (16 gerechnete Gebäude, 1040 ausgenommen):

| Größe | Wert |
|---|---|
| Median t_max (a) | 0 h (Spannweite 0–0 h) |
| Median t_max (b) | 0,5 h (Spannweite 0–3 h) |
| n bei ρ = 20 % (Bemessung a) | n = 1 an allen 16 Gebäuden — keine Rampe |
| Gebäude ohne Rampe bei ρ = 20 % (a) | 16 von 16 (100 %) |
| Bemessung (a) unerreichbar bei ρ = 20 % | 0 von 16 |
| Bemessung (b) unerreichbar bei ρ = 20 % | 0 von 16 |
| ρ_min, Median | 13,9 % |
| ρ_min, Spannweite | 5,1–18,9 % |

## 3 Befund

Am Standardsprung (Nachtabsenkung 18 → 20 °C, wie er an allen 17 Referenzgebäuden gesetzt ist) braucht
**keines** der 16 gerechneten Gebäude bei ρ = 20 % eine Rampe in Bemessung (a): Die Sprungspitze ohne Rampe
liegt an jedem Gebäude unter P_auf, n = 1 hält das Kriterium an der kältesten Stunde ohne Ausnahme. In
Bemessung (b) — zwei Kelvin kältere Reserve — braucht die Hälfte der Gebäude noch immer keine Rampe, die
übrigen höchstens drei Stunden; keines der 16 Gebäude ist in (a) oder (b) unerreichbar, und die Absenkdauer
(D = 8 h) begrenzt nirgends.

Die Überhöhung der Sprungspitze gegenüber der stationären Last (das ist ρ_min — die Reserve, ab der (a) ohne
Rampe auskäme) liegt zwischen 5,1 % (Projekt 1039, zweites Gebäude) und 18,9 % (Projekte 1018 und 1049, Klasse
H — das größere, langsamere Gebäude mit der höchsten Speicherfähigkeit der Bauweise unter den Referenzgebäuden).
ρ = 20 % hält damit an jedem der 17 Referenzgebäude eine Reserve von mindestens 1,1 Prozentpunkten — knapp beim
größten, gut bemessen bei den übrigen. Die Bandbreite folgt der Speicherfähigkeit der Bauweise: Je größer C_w
gegenüber H_s, desto höher die Überhöhung — dieselbe Aussage, die Konzept 4.5 am einzelnen Beispielhaus schon
trifft, hier über alle Referenzgebäude bestätigt.

Kein Referenzgebäude der aktuellen Basis erreicht die „gut gedämmte" Kennlinie aus dem Zahlenbeispiel von
Konzept 4.5 (dort künstlich verschärft: Restleitwert der Außenwand geviertelt, Leitwert am Luftknoten halbiert);
alle 17 Gebäude sind Bestandsbauten mit U-Werten der Außenwand zwischen 0,28 und 2,9 W/(m²·K). Ein deutlich
größerer Sprung als der Standardsprung — etwa eine Wochenend- oder Ferienabsenkung mit mehr als 2 K — würde die
Überhöhung überproportional anheben (die Gegenprobe mit dem Sprung 17 → 21 °C aus Konzept 4.5 zeigt für Projekt
1045 rund die doppelte Überhöhung bei doppeltem ΔT); an den beiden Klasse-H-Gebäuden (18,9 %) wäre ρ = 20 % dann
nicht mehr ausreichend. Diese Probe rechnet nur den Standardsprung, wie beauftragt; ein größerer Sprung ist
damit nicht geprüft und bleibt eine Beobachtung für KP1 ff., keine Feststellung.

## 4 Empfehlung

**ρ bleibt bei 20 %.** Für den Standardsprung, den einzigen, den diese Probe rechnet, hält die Vorgabe an allen
17 Referenzgebäuden eine positive Reserve, keine Rampe wird in Bemessung (a) nötig, und Bemessung (b) bleibt an
jedem Gebäude erreichbar. Ein Anlass, den Startwert vor KP1 zu ändern, besteht nicht. Empfehlung an KP1: Die
Beobachtung zu größeren Sprüngen (Wochenend-/Ferienabsenkung) im Auge behalten, sobald das neue Referenzprojekt
(10.2) mit einer eigenen Nachtauskühlung und Aufheizoptimierung steht — dort lässt sich die Bandbreite an einem
gerechneten Fall statt an einer Näherung nachweisen. Widerspruch gegen diese Festlegung ist bis zur Beauftragung
von KP1 möglich (Muster der Festlegungen F1–F22, Konzept 9.1).

## 5 Skript

`kp0_reserveprobe.py`, Scratchpad der Sitzung (nicht im Repositorium) — zusammengesetzt aus `gp_physik.py`,
`gp_klima.py`, `gp_ref.py`, `synthese_beispiel.py`, `synthese_beispiel2.py` und `synthese_klima.py`. Aufruf:
`py kp0_reserveprobe.py` aus der Repositoriumswurzel, liest `Referenzlaeufe/Kenndaten_Test.sqlite` nur lesend
(`mode=ro&immutable=1`), keine Schreibwirkung, kein Referenzlauf.
