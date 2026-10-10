# Protokoll AH-V1 — Deckelreihe je Stunde und Zustandssicherung der Zone

**10.10.2026 · Sitzung Gebäudesimulation · Welle V1 der Aufheizoptimierung Fassung 2.** Grundlage:
[Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md), Abschnitte 2.2–2.4, 2.7
und Wellenplan (Abschnitt 6, Zeile V1). Ohne Verfahrenswahl, Vorlauf, Plan, Schema, Oberfläche und Ressourcen; keine
Zahl ändert sich, solange niemand eine Reihe setzt.

## Was gebaut ist

| Baustein | Inhalt |
|---|---|
| `GebaeudeModellEingang.HeizleistungMaxReiheW` | optionale Deckelreihe der Zone, 8 760 Werte in W, gesetzt über `HeizleistungMaxReiheSetzen` (geprüft, kopiert, `null` nimmt sie zurück) |
| `GebaeudeModellEingang.HeizleistungMaxBei(h)` | die wirksame Grenze der Stunde vor der Verfügbarkeit; alle sechs Randzweige (Einzone/Klassenweg und Zonenschleife, je ideal, AK1, Kälteseite) lesen sie statt des Skalars |
| `Aufheizplan.Deckelreihe`, `Aufheizoptimierung.PlanSetzen` | der eine Schreibweg vom Plan in den Eingang: Sollwertreihe (wenn angehoben) und Deckelreihe (wenn vorhanden); `Anwenden` und `AnwendenZonen` gehen darüber |
| `Zonenmodell2K.ZustandSichern()` / `ZustandSetzen(in Zonenzustand)` | unveränderlicher `Zonenzustand`: beide Massentemperaturen und das Muster der zuletzt gerechneten Stunde; Rechenpuffer und ihre Zähler bleiben unberührt |
| `EPOS.Kern.Tests/AufheizDeckelreiheTests` | 18 Fälle, siehe Nachweise |

## Konvention der Reihe

- `NaN` heißt „keine eigene Grenze in dieser Stunde“; `+∞` gilt ebenso. Jeder andere Wert ist endlich und ≥ 0
  (sonst `ArgumentException`); die Länge ist 8 760.
- Wirksam ist je Stunde die kleinere Zahl aus Skalar `Heizleistung_Max` und Reihe. `HeizleistungMaxBei` gibt stets einen
  der beiden Operanden unverändert zurück — ohne Reihe oder bei „keine Grenze“ genau den Skalar, ohne Rechnung. Deshalb ist
  der Lauf ohne Reihe bitgleich.
- Mit wirksamer Kopplung schneidet danach die Schranke der Verfügbarkeit (AK2) wie bisher
  (`Stundenrand.MitVerfuegbarkeit`, ebenso im AK3-Kreis über `Anlagenkopplung`). Ist sie die kleinere Grenze, nennt der
  Löser den Grund `Verfuegbarkeit`, sonst `HeizleistungMax`.

## Wie die Reihe die Rechenwege erreicht

- **Einzone und Mehrzonen:** Der Rand jeder Stunde entsteht im Eingang der Zone (`Rand(h, …)` und
  `Rand(h, …, θ_eq, θ_Lue, …)` der Zonenschleife). Die Reihe hängt am Eingang, also trägt sie jede Zone für sich.
- **Kopplungsweg AK1/AK2:** derselbe Rand mit Übergabe. `SchrittUebergabe` kappt über den Begrenzungsgrund
  `HeizleistungMax` am selben Feld `Stundenrand.HeizleistungMaxW`; ein neuer Löserweg ist nicht nötig.
- **AK3-Kreis:** Der Stepper des Kreises baut auf demselben Eingang wie der Lauf (`Vdi6007Rechenweg`, Einzone und
  Mehrzonen). Die Deckelreihe reist deshalb wie die Sollwertreihe: Der Plan setzt beide über `PlanSetzen` in den Eingang,
  `AufheizplanSetzen` hängt den Plan an die Kreiszone. Der Test `AK3_Kreis_sieht_die_Reihe_des_Plans` rechnet den Kreis mit
  einem Kessel ohne Begrenzung und findet die Kappung in genau den Stunden der Reihe.
- **Nicht erreicht (mit Absicht):** `Aufheizoptimierung` bemisst P_auf weiter aus dem Skalar; die Bemessung wird in V2
  neu gefasst.

## Zustand sichern und setzen

Zwischen zwei Stunden trägt `Zonenmodell2K` nur die zwei Massentemperaturen. Der P-Regler der Übergabe rechnet je Stunde
aus Rand und Zustand und hat keinen Übertrag. Mitgesichert wird das Muster der letzten Stunde (für die Mustertreue der
Zonenschleife). Fallsysteme und Aufheizantworten sind Rechenpuffer mit bitgenauem Schlüssel und bleiben stehen.
**Zustand außerhalb des Modells** liegt im `Zonenlauf` (Sommerlüftungs- und Nachtauskühlregel mit Vortagswerten) und in
der Zonenschleife (Lufttemperaturen der Nachbarn). Eine Vorausschau über diese Wege sichert sie in V2/V3 zusätzlich oder
rechnet, wie im Entwurf 2.4 vorgesehen, die `Zonenmodell2K.Schritt`-Folge mit den Rändern der Vorlaufbahn.

## Messung der Rechenzeit (Release, Linux, 4 Kerne, .NET 10.0.12; Median, zwei Durchgänge gleichlautend)

| Fall | 8 760 `Schritt` (Rohschleife, Start 20 °C ohne Vorlauf) | `Laufen` (720 h Vorlauf, Jahr, Ergebnis) |
|---|---|---|
| Einzone ideal (`Vdi6007Probe`) | 15 ms (Spanne 14–19) | 7,8 ms (7,1–8,8) |
| Einzone Büro mit Kalendern und Aufheizrampe (`Bueroprobe`) | 26 ms (25–29) | — |
| AK1 Heizkreis mit Heizkurve (`Vdi6007Probe`) | 64–65 ms (21–108) | 26–27 ms (24–30) |
| Sichern + Setzen | 7–8 ns je Rundlauf (10 000 in 0,07–0,08 ms) | — |

Die Rohschleife ist langsamer als `Laufen`, weil sie ohne eingeschwungenen Vorlauf beginnt und mehr Fallwechsel und
Bisektionen trifft. Sie ist damit die obere Schranke. Das Gebäude von 1051 wurde nicht eigens gemessen; die Büroprobe ist
derselbe Gebäudetyp mit Kalendern und Rampe.

**Abschätzung für V3** (Entwurf 2.4: Vorlauf, Vorausschauen und Lauf zusammen höchstens 10 Zonenjahre je Zone): ideal
höchstens 10 × 26 ms ≈ 0,26 s je Zone mit der Rohschleife als Schranke (≈ 0,1 s mit `Laufen`); AK1 höchstens
10 × 65 ms ≈ 0,65 s (≈ 0,3 s). Das liegt im Bereich einer Sekunde je Zone, wie im Entwurf angenommen. Die
Zustandssicherung kostet dabei nichts Messbares.

## Nachweise

- Build `WP-Plan.Kern.slnf` Release: 0 Fehler (Warnungen Bestand); `DokumentationLinkWacheTests` grün.
- `AufheizDeckelreiheTests` 18/18 grün. Abgedeckt: (a) Reihe überall „keine Grenze“ (NaN und +∞) bitgleich für alle acht
  Einzonenfälle (ideal, AK1 Heiz- und Kälteseite, Kühlung, Rand unbeheizt, Sommerlüftung, Leistungsgrenze, Volumen) und
  für drei Zonen; (b) Grenze in fünf Winterstunden, ideal und AK1: in genau diesen Stunden Leistung ≤ Grenze, Betriebsfall
  Heizgrenze, Kappungsanteil > 0, davor bitgleich, sonst kein Kappungsanteil; (c) Minimum mit Skalar und mit
  AK2-Verfügbarkeit; (d) AK3-Kreis über den Plan; (e) Sichern/Setzen-Rundlauf über 48 Stunden, ideal und AK1, auch in ein
  zweites Modell; dazu die Prüfung und Kopie der Reihe.
- Betroffene Klassen (`Ak3*`, `Anlagenkopplung*`, `Aufheiz*`, `GebaeudeEinzonennetz*`, `GebaeudeStepper*`,
  `GebaeudeModell*`, `Kappungsanteil*`, `ZonenEingang*`, `Zonenmodell*`, `Zonenschleife*`, `Zonenlauf*`): 681 grün,
  1 übersprungen (Bestand), 0 rot.
- Referenzlauf über alle 29 Projekte der Basis `2026-10-10_R51_FreieKuehlung`: **GESAMT PASS (10 036 768 Werte)**. Der
  Byte-Vergleich ist identisch: Alle CSV-Dateien gleichen der Basis byteweise, nur `protokoll.txt` weicht ab (Zeitstempel).

## Offen für V2/V3

- Die Bemessung P_auf/P_V und der Vorlauf (V2) lesen den Skalar. Sie setzen die Deckelreihe künftig über `PlanSetzen`.
- Für eine Vorausschau über `Zonenlauf` oder `Zonenschleife` statt über das nackte Modell fehlt die Sicherung der
  Lüftungsregeln und der Nachbarluft (siehe oben).
