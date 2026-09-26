# Protokoll G6b — Zoneneingabe und Rechenweg des Mehrzonenmodells (26.09.2026)

**Auftrag.** Stufe G6b der Gebäudesimulation (Orchestrator, 26.09.2026, mit Anhang A): mehrere Zonen je
Gebäude rechnen — Schemaschritt S-G mit Trennflächen, Luftaustausch und Ergebnis je Zone, die Werte der
Zone mit Vorgabenkaskade, Trennflächen und Luftströme im Dialog, der Zoneneingang mit Nachbarraum-
Randbedingung, die Zonenschleife (Gauß-Seidel je Stunde nach ADR-005), die Proben 1–12 samt 12a, das
Ergebnis je Zone in Dialog, Bericht und Export und zuletzt die Freischaltung. Nicht in G6b: der
Zonenimport (G6c), das Referenzprojekt mit Zonen (G6d), die Übergabe je Zone (AK2/AK3) und die
Kühlspalten je Zone (KU3).

**Anwenderentscheide vom 26.09.2026 (E49, Konzept N1.55):** Anhang A nach Empfehlung — A1 = M3 (b)
Vorgabe mit Übersteuerung je Trennfläche; A2 = M5 (a) eigene Zeilen für unbeheizte Zonen; A3 = M6 (a)
30 Tage mit Probe, 90 Tage benannt, nur ab zwei Zonen; A4 (a) Heizung und Kühlung je Zone ideal,
Kühlwerte vom Gebäude, AK1 bei mehreren Zonen als ideale feste Last mit Warnung; A5 (a) Zonenwerte auch
bei einer Zone; A6 ja (`Tab_ErgebnisZone`, nur Skalare); A7 (b) nach Messung; A8 (a) Probe 5 geteilt.
Dazu Probe 3 mit dem Band 3 %, Probe 5b mit < 0,001 K und K1–K3 zur erhaltenden Kopplung (K1 = V0).

## 1 Wellen

| Welle | Inhalt | Commits | Gate, CI |
|---|---|---|---|
| W0 | Netz der Einzonenreihen des Bauteilwegs (acht Fälle, SHA-256 je Reihe auf Windows x64, sonst Momentprobe), Probe 12a bestätigt | `43d876b5` | Kern 7655, UI 6454; 14/14 byte-gleich gegen R19 |
| W1 | Regeln zwischen Zonen, Vorgabenkaskade (A5), Σ H_T ohne Grenze `ZONE`; **Schemaschritt S-G (147)**: `Tab_Bauteil.ID_Nachbarzone`/`Trennflaeche_Zuordnung`, `Tab_Zonenluftstrom`, `Tab_ErgebnisZone`; Datenweg, Kopierwege, Transfer | `fae690e0`, `e3c9f59a` | Kern 7866, UI 6458; 14/14 byte-gleich gegen R19; CI 36208324273 |
| W2 | Werte der Zone im Zonendialog, Trennflächen im Bauteildialog, Zonenreiter mit Volumen und „beheizt", Luftaustausch zwischen Zonen, Rückfragen nach den Festlegungen 8 und 9, KI-Sichten, Hilfebereich | `d2dd074d`, `ef47f130`, `3548f198` | CI 36214480466 |
| W3 | `GebaeudeKlima` herausgelöst, Stundenschritt mit Muster, Zoneneingang mit Nachbargliedern und Luftkopplung, Messentscheid A7 an Testbeispiel 10, Probe 11 | `79f5dd7b`, `40dc1095`, `5e3769de`, `929b6723` | Kern 8016; CI 36219318944 |
| W4 | Zonenschleife (Teilgruppen, Gauß-Seidel, Abbruch 0,01 K / 0,1 W für Φ_h und Φ_c, Muster, Vorlauf nach A3, Startwert nach Festlegung 7, 4-K-Regel mit adiabatem Vorlauf), Proben 1–9, Kriterien der Proben 3 und 5b; Entwurf der erhaltenden Kopplung; Probe 4 auf echte Größen gegen die wandaufgelöste Referenz | `8089c5cc`, `04b8e348`, `a3448e7c`, `fb15628f`, `7312e7a4` | Kern 8043, UI 6531; 14/14 byte-gleich gegen R19 und nach dem Merge gegen R20 |
| W5 | Ergebnis je Zone und Gebäudekennzahlen nach Festlegung 10, `Tab_ErgebnisZone` schreiben und lesen; Freischaltung (Laufgrenze und Freigabeschalter gestrichen); Bedarfsdialog mit Zeilen und Diagrammwahl je Zone; Bericht und Export je Zone; Wiki-Quellen; Messung am echten Gebäude | `50d5adf1`, `a5684331`, `11e9a211`, `e53345a7`, `cefc6558`, `a50558a1`, `bbce8471`, `09b7c495` | Kern 8092, UI 6533, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27; Windows-Schale 0 Fehler; SqlDialektPruefer 0 Fundstellen; Auslieferungsvorlage 38/38; Reduzierprobe grün; 14/14 PASS, 432/432 CSV byte-gleich gegen R20 |

Die Linux-CI fand nach W4d einen Stellenvergleich, der am Rundungsrand kippte (`ZonenschleifeTests`,
behoben auf origin mit `6324e65d`); W5 stellte dieselbe Form in `ZonenEingangTests` auf Toleranzen um.

## 2 Proben (Mehrzonenkonzept 8.1)

| Nr. | Ergebnis | Kriterium |
|---|---|---|
| 1 | Lauf 1: Keller auf 15 °C gehalten, Wohnzone **bitgleich** zur Einzonenrechnung mit unbeheiztem Rand; Testbeispiel 10 mit der Trennfläche als Außenbauteil: größte Überschreitung des Bands 0,0883 K mit dem Übergang wie am unbeheizten Raum (A7 b), 2,9748 K nur konvektiv (A7 a) | nicht schlechter als G3 (0,088 K außerhalb des Bands, N1.46 Nr. 7) |
| 2 | zwei gleiche Hälften: Zonenreihen bitgleich, Gebäudesumme gegen die Einzone bis 1e-14 K | bitgleich bzw. 1e-9 / 1e-6 K |
| 3 | Antrieb über die Trennfläche 1e-9 K; Jahresenergie AW gegen IW +1,8 % stationär, +2,4 % im Jahresgang | Band 3 %, modellbedingt und benannt |
| 4 | Bilanz je Zone Rest 7e-8 bzw. 4e-15, Luftströme stündlich Σ = 0; (c) stationäre Erhaltung 5·10⁻¹⁶; (d) gegen die wandaufgelöste Referenz +0,044 % stationär, +0,043 % im Jahresgang, +0,044 % mit Nachtabsenkung, Anteil der Dynamik 7·10⁻⁶ bzw. 3·10⁻⁶ | (c) < 0,1 %; (d) gesamt < 0,1 %, Dynamik < 0,01 % |
| 5a | Abstand zum Fixpunkt 3,5e-5 K, 1,2e-2 W | Fixpunkt der Iteration |
| 5b | gegen das exakt diskretisierte 4×4-System 3,3e-5 / 5,0e-5 / 1,5e-4 K bei 0 / 100 / 400 m³/h | < 0,001 K |
| 6 | Vorstunde gegen Iteration höchstens 0,019 K, Jahresenergie 2,5e-5 | gemessen, begründet die Wegwahl B |
| 7 | Determinismus, auch mit umgekehrter Eingabereihenfolge | byte-gleich |
| 8 | Nichtkonvergenz benannt (`ZonenkopplungKonvergiertNicht`); Pendelstunde hält das Muster und zählt es | benannter Fehler |
| 9 | Zone ohne AW (10¹²), ohne IW, A_AW > A_IW; Σ B_v = 1 als stehende Zusicherung | Richtlinie 6.8 |
| 10 | Referenzlauf 14/14 PASS, 432/432 CSV byte-gleich (jede Welle) | bitgleich |
| 11 | FB1 als Innenbauteil (Testbeispiel 5) im Band, als Trennfläche (Testbeispiel 10) 0,0883 K | im Band, als Paar |
| 12a | R_rad ist schon die Fallunterscheidung Gl. (29)/(31), bitgleich in Klassen- und Bauteilweg — bestätigend, kein Einfrierschritt | bitgleich |

**Messprobe (synthetisch, W4b):** 10 Zonen 298 ms, 50 Zonen 645 ms (12,9 ms je Zone und Jahr),
Durchläufe im Mittel 2,8, höchstens 4, Anteil der Vorläufe 29–34 %.

## 3 Messung am echten Gebäude (Grundlage für M12)

Ein G4b-Import (`gbxml_haus_si.xml`, 120 m², 34 Bauteile, davon 6 innen) über
`GebaeudeZonenCtrl.VorschlagSchreiben` in einer Arbeitskopie der Testdatenbank unter `%TEMP%`
(Projekt 1045), von Hand geteilt: n Wohnungen mit je 0,85/n jedes Außen- und Innenbauteils und der
Nutzfläche, ein unbeheiztes Treppenhaus mit 0,15; je Wohnung eine Trennwand zum Treppenhaus
(Σ 40 m²) und zur Nachbarwohnung (Σ 60 m², Aufbau der importierten Innenwand), Luftaustausch je
Wohnung mit dem Treppenhaus (Σ 30 m³/h). Median aus fünf Läufen nach einem Aufwärmlauf, Rechner mit
22 logischen Kernen (Windows 11). Die vorübergehende Messdatei ist nicht eingecheckt.

| Zonen | Wohnungen | Laufzeit | je Zone und Jahr | Teilgruppen | Vorlauf | Durchläufe Mittel/Max | Musterwechsel | Heizwärme | gegen die Einzone |
|---|---|---|---|---|---|---|---|---|---|
| 1 (Import) | — | 23 ms | — | — | 720 h | — | — | 10,043 MWh | 1 |
| 2 | 1 | 21 ms | 10,5 ms | 1 | 1 440 h | 2,59 / 3 | 4 | 10,692 MWh | 1,065 |
| 5 | 4 | 56 ms | 11,2 ms | 1 | 1 440 h | 2,45 / 3 | 8 | 10,359 MWh | 1,031 |
| 10 | 9 | 152 ms | 15,2 ms | 1 | 1 440 h | 2,31 / 3 | 48 | 10,354 MWh | 1,031 |
| 20 | 19 | 277 ms | 13,8 ms | 1 | 1 440 h | 2,13 / 3 | 23 | 10,352 MWh | 1,031 |
| 50 | 49 | 552 ms | 11,0 ms | 1 | 1 440 h | 2,03 / 3 | 53 | 10,350 MWh | 1,031 |

Befund: 50 Zonen rechnen in rund 0,55 s je Gebäude und Jahr, unter dem Ziel des Mehrzonenkonzepts 2.9
(1,1–2,0 s); die Laufzeit wächst linear mit der Zonenzahl. Der Vorlauf wurde nie verlängert. Die
Heizwärme ist ab vier Wohnungen von der Teilung unabhängig (10,359 → 10,350 MWh, −0,09 %); die
Differenz zur Einzone kommt aus dem unbeheizten Treppenhaus und den Trennwänden als Speichermasse,
nicht aus der Zahl der Zonen. Für M12 spricht die Rechenzeit nicht gegen 50 Zonen; die Grenze bleibt
bis G6c eine Pflegegrenze.

## 4 Festlegungen und Abweichungen

Die Festlegungen des Auftrags (1–12) und die der Umsetzung stehen in Konzept N1.56. Benannte
Abweichungen vom Auftrag:

- **Probe 4:** Der Befund der Welle W4 (−6,7 % Bilanz über das Gebäude) maß die Zuordnung des
  Nachbarglieds, nicht die Heizlast; berichtigt im
  [Entwurf](../../Entwurf_erhaltende_Zonenkopplung_G6b.md). Nach K1–K3 bleibt der Rechenweg
  (V0), die Messung verlangt keine erhaltende Kopplung, V4 entfällt.
- **Freischaltung:** Mit der Laufgrenze fällt auch der Freigabeschalter (E46/A1); ein Gebäude trägt und
  rechnet bis 50 Zonen. Der Einzonenweg lehnt zwei und mehr Zonen weiter benannt ab — erreichbar nur
  über die Übergabe- und Kühlauskunft des Gebäudedialogs, die für ein Gebäude mit mehreren Zonen nicht
  gilt (A4), und jenseits der Grenze.
- **Export:** `Geb[n].Zone[k].*` ohne die Diagnosezahlen (Durchläufe, Musterwechsel) und ohne Reihen je
  Zone; die Diagnosezahlen stehen in `Tab_ErgebnisZone`.
- **Bericht:** Die Summenzeile addiert die Heizwärme der Zonen, nicht ihre Spitzen.
- **Bedarfsdialog:** Die Diagrammwahl gilt Wärmelast- und Raumtemperaturbild; der Assistent führt sie als
  Katalogfeld `diagramm` (Eingabestellen des Dialogs 2 → 3).

## 5 Wiki und Logbuch

Repo-Quellen fortgeschrieben (Commits `a50558a1`, `bbce8471`, `09b7c495`): die neue Seite
„Programm Dokumentation/Mehrzonenmodell" und die Nachzüge in „Gebäude" und „Gebäudemodell VDI 6007";
gegengelesen mit dem Regex der Wurzel-`CLAUDE.md` (0 Treffer), keine Produktdaten, Produktaussage im
Wortlaut von E10 „je Zone". Nicht hochgeladen.

**Logbuch-Entwurf** (Version beim Anwender zu erfragen, Datum mit der Veröffentlichung):

- „Gebäude im Projekt rechnen mit bis zu 50 Zonen – jede Zone nach VDI 6007 Blatt 1, gekoppelt über
  Trennflächen und Luftaustausch, mit Ergebnissen je Zone im Wärmebedarf und im Bericht."

## 6 Aufwand

Geschätzt 15,5–20,5 PT (Auftrag), geleistet in 24 Commits am 26.09.2026 (samt dem Abschlusscommit
der Papiere); nach Umfang rund 16 PT.

## 7 Offen

- Windows-Sichtabnahme: Zonenreiter mit Werten der Zone, Trennflächen und Luftaustausch; Bedarfsdialog
  mit Zonentabelle und Diagrammwahl; Zonentabelle im Bericht mit Heizwärme und Spitze.
- Wiki-Upload der drei Seiten mit dem nächsten Sammel-Upload, samt Logbuch-Satz.
- G6c (M7, M8, M12 mit dieser Messung, M13), G6d (M11), KU3 (Kühlung je Zone), AK2/AK3 (Übergabe je Zone).
- Kein iOS-Lauf nötig: Die Hülle ist unberührt; ohne Schemaschritt 147 lehnt der Kern Nachbarn und
  Luftströme benannt ab.
