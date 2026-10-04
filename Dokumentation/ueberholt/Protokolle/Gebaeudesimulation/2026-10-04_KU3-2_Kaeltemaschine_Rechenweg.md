# Protokoll KU3-2 — Kältemaschine: Rechenweg (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle KU3-2 (E67, E68), ein Opus-Auftrag (104 Aufrufe), Commits `0681f57`, `871b6b3`, `b540dee`, `5d63458`, `5e8613b`, Merge `6f5367b`. Statuszeile #714.
**Entscheid:** keiner neu beim Anwender; Entscheide der Orchestrierung (Abschnitt 4). Kein Schemaschritt (Schritt 183 für KU3-4 angemeldet), Basis R35 unverändert, Referenzlauf byte-gleich.

## 1 Auftrag

Den Rechenweg der Kältemaschine bauen (Kühlkonzept 5.3 bis 5.5, 6.1): Kennlinie, Rückkühlung, Verdichter, freie Kühlung, Einordnung in die Kältekaskade, Ergebnisse und Meldungen. Wirtschaftlichkeit und Anlagenzeile folgen in KU3-4.

## 2 Vorgehen

Ein Opus-Auftrag in fünf Commits (Rechenklasse, Kaskade, Ergebnisse, Fehlerbehebung Wärmepumpenzeile, Tests); ein konfliktfreier Merge; Statuszeile und Papiere aus der Orchestrierung.

## 3 Ergebnis

- **Rechenklasse** `EPOS.Kern/Allgemein/Simulation/Kaelte/Kaeltemaschine.cs` mit `KaeltemaschinenKennlinie` (bilinear über Rückkühl- und Kaltwassertemperatur; Randwert außerhalb mit Zählung und Meldung; ohne Punkte keine Rechnung mit Warnung) und Festwerten in `KaelteFestwerte.cs`.
- **Rückkühltemperatur je Stunde:** Luft Außentemperatur + 5 K, Trockenkühler + 10 K, Wasser fest 25 °C, Nasskühler Feuchtkugeltemperatur nach Stull (2011) + 5 K aus der Luftfeuchte des Ortszeit-Klimas (neu `SimulationWaermebedarf.Luftfeuchte_stuendlich()`), ohne Luftfeuchte Außentemperatur − 3 K mit Hinweis.
- **Kaltwassertemperatur:** kleinste Kaltwasser-Stützstelle der Kennlinie (Muster K21), mindestens `Kaltwasser_Vorlauf_Min` mit Hinweis.
- **Verdichter:** Kapazität und EER aus der Kennlinie, höchstens der offene Bedarf; Mindestteillast bezogen auf die Nennkälteleistung — darunter taktet die Maschine ohne Anfahrverlust und liefert die Last (Taktstunden gezählt, keine Unterdeckung).
- **Kältestrom** = Kälte / EER · (1 + Hilfsstromanteil) + `Hilfsstrom_Rueckkuehlung_kW` × Laufanteil (NULL = 0).
- **Freie Kühlung** nur bei Trocken- und Nasskühler, wenn die Rückkühltemperatur mindestens 3 K unter der Kaltwassertemperatur liegt: Deckung bis zur Nennkälteleistung mit Ersatz-EER 15 plus Hilfsstrom, Verdichter in dieser Stunde aus; Stunden und Kälte gezählt (K8: Betriebsfall, kein Erzeuger; der Weg über die Wärmequelle der Wärmepumpe ist nicht gebaut).
- **Kaskade** (`SimulationControl.Kaelte.cs`): die Maschine hat keinen Kaskadenplatz und deckt nach allen Wärmepumpen im Kühlbetrieb, mehrere in Kennungsreihenfolge (Hinweis im Laufprotokoll); Stunden mit freier Kühlung decken Maschinen mit Trocken-/Nasskühler vor allen anderen; ohne Kältemaschine bleibt die Schleife Zeichen für Zeichen die alte; ohne Wärmepumpe Kältekaskade am Ende der Wärmekaskade vor der Stromstufe; Kältemaschinen zählen als angelegte Erzeuger (Warnung „Kältebedarf ohne Kälteerzeuger“ entfällt); Unterdeckung je Maschine mit Stunden an der Leistungsgrenze und offener Menge; Kühltage und Sperrzeiten der Wärmepumpe gelten nicht für die Maschine.
- **Ergebnisse:** Bedarf, Deckung, `Kaelterestbedarf`, Deckungsgrad und Kältestrom enthalten die Maschine; `Tab_ErgebnisWaermepumpe.Kaelteproduktion_WP` und `Stromverbrauch_Kuehlung` tragen nur noch die Wärmepumpen (behobener Fehler, `5d63458`); Kennzahlendatei neu `Kaelte[i].ID_Kaeltemaschine`, `.HilfsstromMwh`, `.FreieKuehlungStunden`, `.FreieKuehlungMwh`; je Maschine wird nichts gespeichert (KU3-4).
- **Parameter und Meldungen:** `ParameterVerwendung.AlleArten` mit `Anlagenart.Kaeltemaschine` (gerechnet: Bezeichner, Nennkälteleistung, Rückkühlart, Mindestteillast, Hilfsstrom, Kaltwasservorlauf min; nur Dialog: ID, Firma, Typ, Beschreibung, Nenn-EER, Kältemittel, Modulkosten, ReadOnly, Katalogspalten); acht Meldungen `SIMENG_KAELTE_KM_*` in beiden Sprachen.
- **Tests:** `KaeltemaschineRechenwegTests` (9, ohne Datenbank: Kennlinie 56,0 kW / EER 3,725 bei 30/9 °C, Randwert, leer, Rückkühlart, Kaltwassergrenze, Mindestteillast, Kältestrom, freie Kühlung, Reihenfolge), `KaeltemaschineDatenbankTests` (4 an einer Kopie von 1017 mit der 50-kW-Saatmaschine: Wärmepumpe vor Maschine, Rest 0,075 → 0 MWh/a, Kältestrom 0,865 → 0,889 MWh/a, 84 Taktstunden; gespeicherte Wärmepumpenzeile ohne Maschinenkälte; zu kleine Maschine mit Unterdeckungsgrund; ohne Wärmepumpe allein; Kühlung aus → Hinweis). Kern-Filter 486/486; Referenzlauf 8 Projekte gegen R35 PASS und byte-gleich (1017 und 1047 rechnen ihre Kälte unverändert); SQL 0 Fundstellen; Designer ohne Befund. Gate-Zahlen trägt die Orchestrierung in der Statuszeile nach.

## 4 Entscheide der Orchestrierung

- Die Festwerte der Rückkühlung und der freien Kühlung sind benannte Festwerte, keine Eingaben.
- Die Mindestteillast taktet, statt unterzudecken.
- Die Kaltwassergrenze (`Kaltwasser_Vorlauf_Min`) wirkt als Hinweis, nicht als Sperre.
- Die Anlagenzeile der Maschine entsteht mit Schemaschritt 183 in KU3-4.

## 5 Offenes

- **Struktur-Befund:** `Tab_Energieanlagen` hat keinen Verweis auf `Tab_Kaeltemaschine`, `Tab_Typ_Energieanlagen` keinen Eintrag; jede Projektkopie zählt als eine Maschine (Anzahl 1) ohne Kaskadenplatz, `Kuehl_Vorlauf`, Hilfsstromanteil, Kühlträger und Zähler. **Schemaschritt 183** (KU3-4): Typeintrag, Verweis, `Tab_KostenKomponente` Nr. 11 mit Vorlagen und Nutzungsdauer, `Tab_ErgebnisKaeltemaschine`.
- **Wirtschaftlichkeit:** erst mit KU3-4; der Kältestrom läuft bis dahin über Netzbezug und Stromspeicher-Lastreihe in Kosten und Emissionen.
- **Entscheid beim Anwender:** freie Kühlung über die Wärmequelle der Wärmepumpe (Sole) mit KU3-5 bauen oder nicht.
- **Folgewellen:** KU3-3 Kühlung je Zone (läuft), KU3-4 Anlage, Erzeugerdialog, Wirtschaftlichkeit, Ergebnis je Maschine, Bericht, Referenzprojekt, Basis, KU3-5 Kältespeicher.
