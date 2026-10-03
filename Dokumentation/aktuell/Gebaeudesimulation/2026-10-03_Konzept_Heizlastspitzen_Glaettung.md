# Konzept: Heizlastspitzen nach Sollwertsprüngen — Glättungsfilter gegen Aufheizoptimierung

Prüfung der Anwenderfrage vom 03.10.2026: „In der Gebäudesimulation zeigt das Diagramm viele Spitzen, die durch
Temperatursprünge entstehen, wo die Heizung stark nach oben regelt; daraus folgt eine viel zu hohe Spitzenleistung für die
Heizungsauslegung. Kann man durch FFT oder Z-Transformation einen Filter bestimmen, den man in einem zweiten Durchlauf durch das
Jahresstunden-Array einbezieht?“ — samt des mitgelieferten Vorschlags (Tiefpass 1. Ordnung als IIR-Filter über die bilineare
Transformation, wahlweise FFT-Tiefpass, Vorwärts-Rückwärts-Filterung gegen die Phasenverschiebung).

Grundlagen: [Teilkonzept Konditionierungsprofile](../Konzept_Konditionierungsprofile_EPOS-Plan.md) (Abschnitte 4.3–4.8),
[Entwurf KP3](2026-10-02_Entwurf_KP3.md), [Leitkonzept VDI 6007](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Abschnitte 4.5, 4.7), Code `EPOS.Kern/Allgemein/Simulation/Gebaeude/` (`Zonenmodell2K`, `Aufheizantwort`, `Aufheizoptimierung`,
`GebaeudeModellErgebnis`, `GebaeudeModellEingang`), `EPOS.Kern/Allgemein/Simulation/GebaeudeKennzahlen.cs`.

## 1. Woher die Spitzen kommen

Der Rechenweg führt die Heizung als **ideale Last**: Jede Stunde liefert sie genau die Leistung, die den Heizsollwert hält. Springt
der Sollwert nach einer Absenkung nach oben (Nachtabsenkung → Nutzungsbeginn, Wochenende → Montag, Ferien → Betrieb), muss die
Heizung in **einer** Stunde die ausgekühlten Massen und die Raumluft auf den neuen Sollwert bringen. Das ergibt eine Spitze, die
ein Mehrfaches der stationären Heizlast betragen kann; ihre Höhe hängt an der Absenktiefe, der Absenkdauer und den Zeitkonstanten
des Gebäudes (R1: `Zonenmodell2K.Aufheizantwort`, τ₁ und τ₂ des Zweikapazitätenmodells). Sie ist kein numerisches Artefakt und kein
Wärmeimpuls, sondern ein **Leistungsüberschuss** (Teilkonzept, Grundsatz 7): Dieselbe Wärme lässt sich über mehrere Stunden
verteilen.

Ohne eine Begrenzung der Heizleistung (`Heizleistung_Max` leer) kennt der Lauf keine Grenze nach oben; das Diagramm zeigt dann die
Spitzen der idealen Last.

## 2. Was EPOS-Plan dafür schon hat

| Mittel | Ort | Wirkung |
|---|---|---|
| **Auslegungsheizlast** (stationär am Auslegungstag) | `GebaeudeModellEingang.AuslegungsheizlastW`, `Zonenmodell2K.StationaereHeizlastW` | Die normgemäße Auslegungsgröße der Übergabe und der Nennleistung; sie ist von den Simulationsspitzen unabhängig |
| **Heizleistung_Max** (Begrenzung) | Gebäude, Quelle „Grenze“ | Kappt die Leistung physikalisch; die Raumtemperatur bleibt nach dem Sprung so lange darunter, bis die Massen nachgeladen sind |
| **Aufheizoptimierung** (KP3, R1–R4 gebaut) | `Aufheizoptimierung`, `Aufheizplan`, Projekteinstellung | Bemisst je Gebäude und Zone eine Aufheizleistung P_auf — Quelle Ziel: (1 + ρ)·Φ_stat an der kältesten Stunde, Quelle Grenze: `Heizleistung_Max` — und beginnt den Sollwertanstieg früher als Rampe über n Stunden aus der Stufenformel, so dass die Spitze P_auf nicht übersteigt und der Sollwert zu Nutzungsbeginn erreicht ist. Lauftest 1018: Spitze 37,36 → 32,12 kW bei +0,05 % Jahreswärme |
| **Spitze als Tagesmittel** | `GebaeudeModellErgebnis.SpitzeTagesmittelKw`, `GebaeudeKennzahlen.GroesstesTagesmittel` | Größtes gleitendes Mittel über 24 Blockstunden — ein zweiseitiger Rechteckfilter (FIR) ohne Phasenverschiebung; steht neben `SpitzeKw` in Ergebniszeile und Bedarfsdialog |
| **Nachweisband W3** (KP3) | `Aufheiztage_Nachweisband` | Zählt die Tage, an denen der Lauf trotz Rampe über 1,01·P_auf liegt |

Die physikalische Trägheit, die der Vorschlag mit einem PT1-Glied „abbilden“ will, **steckt bereits im Modell**: Das
Zweikapazitätenmodell nach VDI 6007 trägt die Zeitkonstanten des Gebäudes; die Aufheizantwort (R1) ist genau die Sprungantwort
dieses Modells. Ein zusätzlicher Tiefpass auf der Ergebnisreihe zählte dieselbe Trägheit ein zweites Mal.

## 3. Bewertung der beiden Filterwege

**Gemeinsam.** Jeder lineare Tiefpass mit Gleichverstärkung 1 erhält die Jahressumme, verteilt aber die Leistung um: Die geglättete
Reihe ist nicht mehr die Leistung, die den Sollwert hält. Wird sie in einem **zweiten Durchlauf** als Heizleistung vorgegeben, läuft
die Heizung offen (ohne Rückführung auf die Raumtemperatur): Die Räume erreichen den Sollwert nach dem Sprung später, ohne dass
der Lauf das ausweist, und die Sollwertreihe des Projekts wird nicht mehr eingehalten. Das wäre ein stillschweigender Eingriff in
den Rechenweg, der gegen das Regressionsnetz (Byte-Gleichheit bei Schalter aus) und gegen die Regel „eine Bemessung, zwei
Verwendungen“ (Entwurf KP3, Grundsatz 3) stünde.

**IIR-Tiefpass (Z-Transformation, PT1).** Einfach, O(N), aber mit Phasenverschiebung; die Vorwärts-Rückwärts-Form (filtfilt)
hebt sie auf, verdoppelt die Ordnung und braucht Randbehandlung am Jahresring. Die Zeitkonstante T (3–12 h) ist frei — sie hat
keinen Bezug zu den Gebäudekonstanten, es sei denn, man setzt sie aus τ₁/τ₂, und dann ist sie redundant zum Modell. Als
Auslegungssicht liefert sie nichts, was das Rechteckmittel nicht auch liefert.

**FFT-Tiefpass.** Für ein periodisches Jahr (8 760 Werte, Ring) sauber definierbar, ohne Phasenverschiebung; ein harter
Frequenzschnitt („Brick-Wall“) erzeugt aber **Gibbs-Überschwinger** an jedem Sprung — negative Heizlasten und Vorläufer vor dem
Sprung —, ein Gauß-Fenster entspricht einer Faltung im Zeitbereich und bringt gegenüber einem Zeitbereichskern nichts. Eine
FFT-Bibliothek fehlt im Kern; die Diagrammbilder und der Referenzlauf müssten sie reproduzierbar auf allen Plattformen rechnen
(Plattformrundung, gestörter Lauf).

**Ergebnis:** Kein Filter gehört in den Rechenweg. Die Frage „welche Spitze ist für die Heizungsauslegung maßgebend?“ ist eine
Frage der **Auswertung**, und sie ist mit KP3 physikalisch beantwortet: Die Aufheizleistung P_auf ist die Spitze, die ein Gebäude
mit Rampe braucht, und sie steht je Gebäude und Zone in der Ergebniszeile (`Aufheiz_Leistung_Kw`) und in der Herleitungszeile
(D2) — ohne Jahreslauf. Die DIN EN 12831-1 geht denselben Weg mit ihrem Aufheizzuschlag für unterbrochenen Heizbetrieb; KP3
rechnet ihn aus dem Modell statt aus einer Tabelle.

## 4. Vorschlag

1. **Auslegungsgröße:** Für die Heizungsauslegung gilt die stationäre Auslegungsheizlast plus die Aufheizleistung aus der
   KP3-Bemessung, nicht das Maximum der idealen Last. Der Bedarfsdialog (O2) und der Bericht (O3) weisen beides nebeneinander aus:
   `SpitzeKw` (ideale Last), `SpitzeTagesmittelKw`, `Aufheiz_Leistung_Kw` mit Quelle, dazu der Hinweis, dass die ideale Spitze bei
   Schalter aus keine Auslegungsgröße ist. Kein neuer Rechenweg; die Texte kommen mit O2/O3.
2. **Auswertungsglättung als Kennzahl (optional, E60):** `GroesstesTagesmittel` wird auf eine wählbare Mittelungsdauer n ∈ {1, 3,
   6, 12, 24} h verallgemeinert („Spitze als n-h-Mittel“, zweiseitiges Rechteckmittel über den Jahresring, ohne Phasenverschiebung);
   die Dauer ist eine **Anzeigewahl** im Bedarfsdialog, keine Projekteinstellung und keine Spalte — Diagramm und Zeile zeigen die
   geglättete Reihe als zweite Linie. Aufwand klein (Kern eine Funktion mit Test, Dialog eine Auswahl); byte-gleich für den
   Referenzlauf, weil kein Ergebnis der Simulation entsteht.
3. **Nicht umgesetzt:** IIR- oder FFT-Filter im Rechenweg, ein zweiter Durchlauf mit gefilterter Leistung. Wer die Spitze im Lauf
   senken will, schaltet die Aufheizoptimierung ein oder trägt `Heizleistung_Max` ein — beides ist geschlossen geregelt und wird im
   Ergebnis ausgewiesen (W1–W5).

## 5. Entscheid

- **E60 (entschieden 03.10.2026): Vorschlag 1.** Auslegungsgröße ist die stationäre Auslegungsheizlast plus die Aufheizleistung aus der
  KP3-Bemessung; Bedarfsdialog (O2) und Bericht (O3) weisen ideale Spitze, Tagesmittel und P_auf nebeneinander aus. Kein Filter im
  Rechenweg; Vorschlag 2 (Kennzahl „Spitze als n-h-Mittel“) nicht beauftragt. Nachzug in Register, Statusdatei, Teilkonzept und Entwurf
  KP3 (Wellen O2/O3) mit den E59-Papieren.
- Die ursprüngliche Frage: Soll die Kennzahl „Spitze als n-h-Mittel“ mit wählbarer Dauer kommen (Vorschlag 2), oder genügen `SpitzeTagesmittelKw`
  und die Aufheizleistung aus KP3? Empfehlung: Vorschlag 1 sofort mit O2/O3, Vorschlag 2 nur auf Wunsch als kleine Welle nach KP3.
