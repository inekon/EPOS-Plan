# Beispieldateien

Zwei aufeinander abgestimmte Beispiele für einen **MFH-Bestand mit 150 WE plus
Bürogebäude**, Größenordnung angelehnt an eine reale Wohnungswirtschafts-
Liegenschaft (≈ 611 MWh/a Wärme, Messspitze ≈ 125 kW).

Beide Dateien beschreiben **dieselbe Liegenschaft** — einmal als
Eingabeprojekt für die synthetische Profilerzeugung, einmal als
„gemessener" Jahreslastgang. So lässt sich unmittelbar vergleichen, wie weit
Synthetik und Messung auseinanderliegen (siehe unten).

| Datei | Was es ist | Wo es in der App hingehört |
|---|---|---|
| `beispiel_projekt.json` | Vollständiges Projekt (Gebäudetabelle, TRY-Region, TWW- und Puffer-Parameter, UI-Einstellungen) | Sidebar → **📂 Projekt laden (JSON)** |
| `beispiel_lastgang.csv` | Stündlicher Jahreslastgang 2025, 8760 Zeilen, Semikolon + deutsches Dezimalkomma | Tab 1 → Quelle **Messdaten** → Datei-Upload |

---

## `beispiel_projekt.json`

Portfolio:

| Gebäude | Kategorie | WE | Pers./WE | Anzahl | Q_Heiz [kWh/a] | Q_TWW [kWh/a] |
|---|---|---|---|---|---|---|
| MFH-Bestand Musterstraße 1-9 | MFH | 30 | 2,2 | 5 | 88 000 | 22 000 |
| Bürogebäude Verwaltung | GHD (`gko`) | – | – | 1 | 61 000 (gesamt) | 8 % TWW-Anteil |

Weitere Vorgaben: TRY-Region **12** (Oberrheingraben/Mannheim), Jahr **2025**
(kein Schaltjahr — VDI 4655 in demandlib 0.2.2 unterstützt keine Schaltjahre),
TWW-Speicher 60/10 °C mit 45 kW Ladeleistung und 4 kW Zirkulation,
Puffer 100 kW WP + 60 kW bivalent, ΔT 10 K, EVU-Sperrzeiten 11–13 h und 17–19 h.

**So nutzen:** App starten → Sidebar → *Projekt laden (JSON)* → Datei wählen →
Tab 1 → *Profil erzeugen*. Danach laufen Tab 2–5 mit den mitgelieferten
Parametern durch.

Ergebnis der Profilerzeugung (Referenzwerte, seed 42):

| Kennwert | Wert |
|---|---|
| Jahresarbeit gesamt | **611,0 MWh/a** (496,1 Heizung / 114,9 TWW) |
| TWW-Anteil | 18,8 % |
| Spitzenlast (synthetisch) | **297,5 kW** |
| Volllaststunden | **2 054 h** |
| P bei 90 % der Jahresarbeit | 128,6 kW |
| GLF nach DIN 4708 (N = 150) | 0,066 |

---

## `beispiel_lastgang.csv`

Format exakt wie die realen Auswertungsdateien:

```
Datum;Lastgang Wärmebedarf [kW]
01.01.2025 00:00;84,879
01.01.2025 01:00;88,065
...
```

- Trennzeichen `;`, Dezimalkomma `,`, Zeitstempel `TT.MM.JJJJ hh:mm`
- 8 760 Stundenwerte (01.01.2025 00:00 – 31.12.2025 23:00), keine Lücken
- Werte = **Gesamtwärme** (Heizung + TWW) in kW

| Kennwert | Wert |
|---|---|
| Jahresarbeit | **611,0 MWh/a** |
| Spitzenlast | **124,9 kW** |
| Volllaststunden | **4 892 h** |
| Sommer-Baseline (Jun–Aug, Mittel > 0) | 18,7 kW → TWW-Anteil 22,6 % |

**So nutzen:** Tab 1 → Quelle *Messdaten* → Datei hochladen →
Zeitstempelspalte `Datum`, Wertspalte `Lastgang Wärmebedarf [kW]`,
Einheit `kW`, Dezimaltrennzeichen `,`. Der Heizung/TWW-Split erfolgt über die
Sommer-Baseline (Jun–Aug), Faktor 1,0.

Die Datei ist **synthetisch erzeugt**, nicht gemessen: Basis ist dasselbe
VDI-4655/BDEW-Profil wie im Projekt-JSON, anschließend erzeugerseitig auf
ca. 108 kW begrenzt (mit Nachholen der zurückgestellten Energie, wie es ein
leistungsbegrenzter Erzeuger mit Puffer und Gebäudespeichermasse bewirkt),
mit 3,5 % Messrauschen überlagert, auf ein Zirkulations-Grundband von 3,5 kW
angehoben und energieerhaltend auf 611 MWh/a normiert. Sie enthält keine
personenbezogenen oder realen Verbrauchsdaten.

---

## Synthetik vs. Messung — worauf zu achten ist

Beide Dateien haben **dieselbe Jahresarbeit**, aber eine völlig andere
Leistungscharakteristik:

| | Synthetik (Projekt-JSON) | „Messung" (CSV) | real (Referenzobjekt) |
|---|---|---|---|
| Jahresarbeit | 611 MWh | 611 MWh | ≈ 611 MWh |
| Spitzenlast | 297 kW | 125 kW | ≈ 125 kW |
| Volllaststunden | 2 054 h | 4 892 h | ≈ 4 900 h |

Der Faktor ≈ 2,4 zwischen den Spitzen ist **kein Fehler des Tools**, sondern
systematisch:

- VDI-4655-Typtagprofile bilden die *Bedarfsspitze* bei gleichzeitiger
  Anforderung aller Wohnungen ab. Bei Stundenauflösung wirkt der
  Gleichzeitigkeits-Zeitversatz (`sigma`) praktisch nicht — die Kopien werden
  aufaddiert.
- Eine reale Messung erfasst dagegen die *Erzeugerleistung*: begrenzt durch
  Kesselleistung, Vorlauftemperaturregelung, vorhandenen Puffer und die
  thermische Trägheit des Gebäudes. Spitzen werden gekappt und zeitlich
  verteilt.
- Die reale Messspitze von 125 kW liegt beim synthetischen Profil ungefähr auf
  dem **P-90-%-Punkt** der Jahresdauerlinie (128,6 kW), d. h. der Leistung,
  die 90 % der Jahreswärmearbeit deckt.

**Konsequenz für die Auslegung:** Wer synthetische Profile für die
Erzeugerauslegung verwendet und sich an der Spitzenlast orientiert, legt
deutlich zu groß aus. Für die Erzeugerleistung ist der Bivalenzpunkt aus der
Jahresdauerlinie (Tab 2, „P bei 90/95/99 %") die belastbarere Größe; die
synthetische Spitze taugt für die Speicherauslegung als konservative
Obergrenze.

---

## Erwartete Auslegungsergebnisse (Referenzwerte)

Mit dem synthetischen Profil des Projekt-JSON:

**TWW-Speicher** (150 WE, 2,2 P/WE, 60/10 °C, p_lade 45 kW):

| Verfahren | Volumen |
|---|---|
| DIN 4708 (N = 94,3; W_z = 64,5 kWh) | 1 387 l |
| Profilbasiert (SOC-Defizit) | 1 288 l |
| Faustwert (35 l/(P·d) × 330 P) | 13 282 l |

Das Tool nimmt das Maximum und empfiehlt formal 13 300 l, weist aber auf die
Spreizung der Verfahren um Faktor 10 hin. **Fachlich belastbar sind hier
DIN 4708 und das profilbasierte Verfahren (≈ 1 400–2 000 l);** der Faustwert
unterstellt eine einzige Speicherladung je Tag und ist für große Wohnanlagen
mit ausreichender Ladeleistung systematisch zu groß. MFH-üblich sind
1 000–5 000 l bzw. eine Frischwasserstation mit Puffer.

**Pufferspeicher** (ΔT 10 K, 20 l/kW Abtauung, Sperrzeiten 2 × 2 h):

| P_WP (monovalent) | Abtauung | Taktung | EVU-Sperrzeit | Simulation | Empfehlung |
|---|---|---|---|---|---|
| 80 kW | 1 600 l | 344 l | 36 891 l | Ziel nicht erreichbar (Deckung 95,9 % selbst bei 50 MWh) | 36 900 l (Sperrzeit) |
| 100 kW | 2 000 l | 430 l | 36 891 l | 3 026 108 l (35 194 kWh) | 3 026 100 l + Warnung „> 100 m³" |
| 100 kW + 60 kW bivalent (Projekt-JSON) | 2 000 l | 430 l | 36 891 l | 303 077 l (3 525 kWh) | 303 100 l + Warnung „> 100 m³" |
| 150 kW | 3 000 l | 645 l | 36 891 l | 406 923 l (4 733 kWh) | 406 900 l + Warnung „> 100 m³" |

Beide Ergebnisse sind rechnerisch korrekt, aber **nicht als Speichergröße
bestellbar**: Ein monovalenter Erzeuger von 80–100 kW gegen eine Heizspitze von
221 kW lässt sich nicht mit einem Puffer kompensieren — das führt auf einen
Saisonalspeicher. Praktische Wege: Erzeuger vergrößern, bivalenten
Spitzenlasterzeuger vorsehen, oder die Sperrzeit nur teilweise aus dem Puffer
decken. Deckt der Erzeuger die Spitzenlast, fallen Simulation und
Sperrzeit-Kriterium auf denselben Wert zusammen (≈ 34–37 m³ bei 2 h Sperrzeit
und ~200 kW Mittelleistung) — auch das ist noch kein bestellbarer Puffer,
sondern das Argument für eine Teildeckung während der Sperrzeit.

---

## Dateien prüfen / neu erzeugen

Beide Dateien sind reproduzierbar (fester Seed 42 bzw. 2025). Schema und
Kennwerte werden dauerhaft abgeprüft:

```bash
python3 -m pytest tests/test_verification.py -q
```
