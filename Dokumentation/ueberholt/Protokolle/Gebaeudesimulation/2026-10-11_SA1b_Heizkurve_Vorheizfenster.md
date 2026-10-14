# Protokoll SA1b — Heizkurve im Vorheizfenster am Fenstersollwert

**11.10.2026 · Sitzung Gebäudesimulation · Auftrag SA1b.** Befund aus
[SA1](2026-10-11_SA1_Schnellaufheizung_Kern.md) („Befund am Rande“): Im Vorheizfenster fährt die Heizkurve am abgesenkten
Sollwert, nicht an θ_T. Geklärt, für die neuen Verfahren behoben, für die Sollwertrampe gemessen, Schnellaufheizung neu
gemessen. Nur Rechenkern: kein Schema, keine Oberfläche, keine Basis neu.

## Ursache

- **Entstehung:** `GebaeudeModellEingang.KopplungAufloesen` (Einzone, Schritt E) rechnet `VorlaufC[h] = Heizkurve.VorlaufC(ThetaSoll[h], ThetaOut[h])`
  beim Bauen; `GebaeudeModellEingang.ZonenkopplungAufloesen` (Mehrzonen) rechnet einmal die Kurve am höchsten Heizsollwert der
  gekoppelten Zonen in die **eine** Reihe des `Gebaeudeheizkreis`, die jede gekoppelte Zone als `VorlaufC` teilt.
- **Lücke:** Der Plan schreibt danach über `Aufheizoptimierung.PlanSetzen` → `HeizsollwertMitRampeSetzen` nur `ThetaSoll`
  neu; die Vorlaufreihe bleibt die des Bauers (Kalender mit Absenkung).
- **Leser:** allein der Stundenrand (`RandOhneFahrplan`, `vorlaufC: VorlaufC[h]`) — darauf bauen Einzone, Zonenschleife mit
  Zonenübergabe, AK1 (Projektvorlauf = Mischung der Zonenergebnisse, `HeizkreisErgebnis`/`HeizkreisProjekt`), der AK3-Kreis
  (derselbe Eingang) und die Auskunft (`GebaeudeBedarfCtrl` liest den Heizkreis des Laufs). Die **Vorausschau** des Vorheizplans
  (`Vorheizvorausschau.Ankunft`) setzt mit `MitVorheizen` nur den Sollwert θ_T, nicht den Vorlauf — derselbe Befund auch dort.
- **(i) neue Verfahren „Vorheizzeit vorgeben/berechnen“:** betroffen (Lauf und Vorausschau, Option 2 bestimmt den Bedarf zu
  lang).
- **(ii) Sollwertrampe:** **nicht betroffen.** `Aufheizoptimierung.Planen` plant eine gekoppelte Zone nicht (Zustand
  GEKOPPELT, W5/F13; `Aufheizzone.Gekoppelt = KopplungWirksam …`): Ihr Sollwert bleibt unverändert, also auch der passende
  Vorlauf. Die Zonen, die rampen (IDEAL, ohne Heizkreis), tragen keinen Vorlauf und gehen in den gemeinsamen Vorlauf nicht ein.
  Dasselbe gilt für den Rampen-Rückfall der neuen Verfahren (AH-V5, Zone ohne Vorheizzeit).
- **(iii) Kalendersprung:** nicht betroffen — ab h_s trägt schon die gebaute Sollwertreihe θ_T, die Kurve folgt.
- **Raumeinfluss** (`Heizkurve_Raumeinfluss`, H2): nimmt den richtigen Sollwert — `Raumeinfluss.Anheben` merkt sich
  `r.ThetaSoll` des Randes (mit Plan); er hob aber auf dem zu niedrigen Grundvorlauf an und hat den Befund in AK3 (1058, k_R = 1)
  teilweise ausgeglichen.

## Behebung (Vorgabe für die neuen Verfahren)

- `GebaeudeModellEingang.VorlaufNachSollwertBilden(zonen)`: bildet die Vorlaufreihe an Ort und Stelle am jetzt gültigen
  Sollwert neu — Einzone wie Schritt E, Mehrzonen am höchsten Sollwert der gekoppelten Zonen in die gemeinsame Reihe. Ohne
  Heizkurve (fester Vorlauf) oder ohne wirksamen Heizkreis nichts; ein unveränderter Sollwert ergibt dieselben Bits.
- Gerufen in `Vorheizplanung.AnwendenEinzone` und `Vorheizplanung.AnwendenZonen` nach dem Setzen aller Pläne.
- Vorausschau: `GebaeudeModellEingang.KurveAmSollwert` setzt in den Fensterstunden die Kurve am Fenstersollwert (nie unter dem
  Vorlauf des Randes, damit im Mehrzonenweg der höchste Sollwert gilt), gekappt am Vorlaufangebot der Anlage
  (`Stundenrand.MitKurvenvorlauf`). Die Schnellaufheizung setzt darauf θ_T + Δθ.
- Sollwertrampe: interner Schalter `Aufheizoptimierung.RampeVorlaufNachfuehren` (Vorgabe `false` = Bestand) ruft dieselbe
  Neubildung nach `Anwenden`/`AnwendenZonen`. Er ist nach (ii) wirkungslos (siehe Messung).
- Kein Referenzprojekt rechnet ein neues Verfahren mit Heizkreis: neues Verfahren nur 1065 (BERECHNET, ohne Heizkreis);
  Aufheizoptimierung an mit Heizkreis nur 1054 (AK1, Rampe → GEKOPPELT). Die Basis bleibt R52.

## Messung Sollwertrampe (Schalter aus/an, Projektlauf mit eigener Vorgabe)

Referenzprojekte mit Aufheizoptimierung (Rampe) und wirksamem Heizkreis: nur **1054**. `HeizkurveVorheizfensterTests.Messung_SA1b_Rampe`:

| Projekt | Schalter | Heizwärme MWh | Spitze kW | Vorlauf max °C | Vorlauf Mittel °C | WP-Strom |
|---|---|---|---|---|---|---|
| 1054 | aus | 47,595 | 27,5 | 55,0 | 35,95 | – (Kessel) |
| 1054 | an | 47,595 | 27,5 | 55,0 | 35,95 | – |

Alle Größen gleich, die Unterschreitungsstunden ebenso. Referenzlauf 1054 mit Schalter an (Vorgabe vorübergehend `true`,
nicht committet) gegen R52: PASS, 28/28 CSV byte-gleich. **Kein
Anwenderentscheid nötig** — die Rampe plant gekoppelte Zonen nicht; der Schalter kann bleiben oder entfallen.

## Neumessung Schnellaufheizung (Option 2, Toleranz 20 %, ε 1 K, mit Behebung)

`SchnellaufheizungTests.Messung_SA1` mit `EPOS_MESSUNG=1`, dieselben Spalten wie im SA1-Protokoll (dort die Werte vor der
Behebung).

| Projekt | Δθ K | t_V h | Bedarf max h | Nächte o. Absenkung | Tage über t_V | Unterschr. K | Heizwärme MWh | WP-Strom MWh | AZ | Spitze kW | Vorlauf max °C | Std. überhöht | Std. an Vorlauf_Max |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1054 (AK1, 3 Zonen, Kessel) | aus | 9 | 12 | 365 | 8 | 3,91 | 52,280 | – | – | 23,6 | 55,0 | 0 | 0 |
| | 2 | 6 | 9 | 0 | 7 | 4,14 | 51,240 | – | – | 25,4 | 57,0 | 297 | 0 |
| | 3 | 5 | 9 | 0 | 9 | 4,22 | 50,792 | – | – | 26,4 | 58,0 | 302 | 0 |
| | 5 | 4 | 8 | 0 | 6 | 4,27 | 50,352 | – | – | 27,4 | 60,0 | 284 | 0 |
| 1047 (AK1, WP) | aus | 1 | 1 | 0 | 0 | 2,33 | 70,490 | 2,240 | 4,45 | 42,1 | 55,0 | 0 | 0 |
| | 2 | 1 | 1 | 0 | 0 | 2,33 | 70,579 | 2,303 | 4,41 | 42,1 | 55,0 | 75 | 3 |
| | 3 | 1 | 1 | 0 | 0 | 2,33 | 70,622 | 2,340 | 4,38 | 42,1 | 55,0 | 75 | 5 |
| | 5 | 1 | 1 | 0 | 0 | 2,33 | 70,664 | 2,388 | 4,34 | 42,1 | 55,0 | 75 | 36 |
| 1058 (AK3, WP mit Puffer) | aus | 1 | 1 | 0 | 0 | 2,31 | 70,913 | 12,656 | 4,29 | 42,0 | 55,0 | 0 | 0 |
| | 2 | 1 | 1 | 0 | 0 | 2,31 | 70,949 | 12,620 | 4,29 | 42,0 | 55,0 | 59 | 2 |
| | 3 | 1 | 1 | 0 | 0 | 2,31 | 70,979 | 12,589 | 4,29 | 42,0 | 55,0 | 59 | 3 |
| | 5 | 1 | 1 | 0 | 0 | 2,31 | 71,008 | 12,561 | 4,30 | 42,0 | 55,0 | 59 | 28 |
| 1065 (ohne Heizkreis, Gegenprobe) | aus/2/3/5 | 2 | 5 | 0 | 5 | 1,54 | 24,673 | 3,183 | 3,88 | 28,3 | – | 0 | 0 |

1065 ist in jeder Zahl gleich wie in SA1. In 1047 und 1058 bleibt die Schnellaufheizung in 2 Fensterstunden wirkungslos
(Vorlauf schon an `Vorlauf_Max`).

**Wie viel der SA1-Wirkung auf den Befund entfiel.**

- **1047/1058:** Die 13 Tage über t_V und der Bedarf 2 h verschwinden schon mit der Behebung allein — dieser Teil der
  SA1-Wirkung war ganz der Befund. Die Behebung kostet in 1047 +0,4 % Heizwärme und +4,1 % WP-Strom (AZ 4,47 → 4,45); die
  Überhöhung um 2 K danach nur noch +0,1 % Wärme und +2,8 % Strom (SA1: +11,6 %), 75 statt 190 überhöhte Stunden, 3 statt 8 an
  `Vorlauf_Max`. In 1058 senkt die Behebung den WP-Strom sogar leicht (−1,4 %; der Puffer lädt im Fenster bei höherem Vorlauf).
- **1054:** Die Behebung kürzt t_V von 12 auf 9 h (Bedarf max 18 → 12 h) — die Hälfte der t_V-Kürzung aus SA1. Die
  Nachtabsenkung erhält aber erst die Schnellaufheizung (365 → 0 Nächte); ihre Wärmeersparnis sinkt von −2,5 % auf −2,0 %
  (3 K −2,8 %, 5 K −3,7 %), die Spitze steigt um 8 % (12 %, 16 %); rund ein Fünftel der SA1-Ersparnis bei 2 K war der Befund.

## Empfehlung: Vorgabe Δθ bleibt 2 K

Die Behebung hat den Nutzen der Schnellaufheizung bei der Wärmepumpe mit kurzer Vorheizzeit (1047/1058) aufgezehrt — dort
bleibt nur ihr Preis (Strom, Stunden an `Vorlauf_Max`), und der wächst mit Δθ. Wo sie wirkt (1054: lange Vorheizzeit über
der Absenkung), erreicht 2 K das Ziel bereits (jede Nacht abgesenkt, t_V 9 → 6 h); 3 und 5 K kürzen t_V nur um ein bzw.
zwei Stunden bei weiter steigender Spitze. `Vorheizvorgabe.UEBERHOEHUNG_VORGABE_K` bleibt 2,0 (Kommentar auf die neuen Zahlen
gebracht). Der Schalter bleibt aus (Vorgabe); für Anlagen wie 1047 ist er nach der Behebung nicht zu empfehlen.

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release` → 0 Fehler.
- Tests mit Filter (HeizkurveVorheiz*, Schnellauf*, Vorheiz*, Aufheiz*, Zonen*, Anlagenkopplung*, Ak3*, GebaeudeModell*,
  GebaeudeStepper*, Stundenrand*, SimulationWaermebedarf*, alle Wachen): 1 411 grün, 1 übersprungen (Messfall). Neue Klasse `HeizkurveVorheizfensterTests` (Kurve folgt im Fenster θ_T bei Vorgabe und
  Berechnet, Vorausschau, Rampe mit/ohne Schalter unverändert, Schalter zieht eine angehobene Reihe nach). Die Vorrichtung
  `VorheizAnkunftsbezugTests.Gekoppelt` bildet nach dem Setzen des Kalenders den Vorlauf am Kalender (wie ein Bauer, der den
  Kalender selbst trägt).
- Referenzlauf aller 30 Projekte gegen `Referenzlaeufe/2026-10-11_R52_Vorheizen`: GESAMT PASS (10 422 417 Werte), 979/979 CSV
  byte-gleich — keine Basis neu.

## Offene Punkte

- `Gebaeudeheizkreis.StundenOhneHeizungH` zählt die Stunden ohne Sollwert beim Bauen (vor dem Plan); Fensterstunden aus „aus“
  zählen dort noch mit. Nur Auskunft, kein Rechenweg.
- Ob der wirkungslose Schalter `RampeVorlaufNachfuehren` entfällt, entscheidet die Orchestrierung (er schützt nur, falls die
  Rampe je gekoppelte Zonen planen sollte).
