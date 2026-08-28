# Paket B1 — Booster-Temperaturkopplung: Umsetzungsprotokoll

Stand: 28.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Kapitel 8.2 (Booster-Wärmepumpe), 8.3 (Aufräumen), 8.4 (Heizkessel mit Puffer-Quelle),
Leitentscheidung L8, Entscheidungen **F9** (Booster als Anzeigeregel) und **F13**
(Kappung + Protokoll), Paketzeile **B1**. Vorleistung
[`P1_Schichtmodell_Protokoll.md`](P1_Schichtmodell_Protokoll.md) (Schnittstelle
`SchichtTemperatur(i)`/`T_oben`, Ticket P1-O4).
Build `WP-Plan.sln` + `Referenzlauf.csproj` x64 Debug: **0 Fehler**.
**Kein Schema-Schritt** — B1 fasst die Datenbankstruktur nicht an (`ZIEL_VERSION` bleibt 53).

> **Meilenstein Z4 erreicht.** Die Quelltemperatur eines Erzeugers am geteilten Pufferspeicher
> ist kein Eingangswert mehr, sondern folgt dem Speicherzustand — für Wärmepumpe **und**
> Heizkessel, über dieselbe Schnittstelle und denselben Lesezeitpunkt.

## 1. Umfang

`WaermequelleClass.Quelltemperatur` lieferte bis P1 **einmal beim Modulaufbau** ein komplettes
Jahresprofil `float[8760]`, das die Stundenschleife danach nur noch ablas. Für einen
**geteilten** Quellpuffer — einen Speicher, der zugleich Senke eines anderen Erzeugers ist —
ist das falsch: Seine Temperatur folgt dem Ladezustand, und genau diese Aufwertung ist die
Physik des Boosters. B1 ersetzt den Wertetausch durch einen **Schnittstellenwechsel**: Die
Quelltemperatur wird **je Stunde genau einmal** gebildet, unmittelbar vor Phase B der
Rechenebene der beziehenden Anlage, und gilt für Bedarfs- **und** Ladephase derselben Stunde.

Dazu: die **Kappung nach unten** an der Kennlinie (F13) samt Protokoll, die
Quelltemperatur als **Laufergebnis** (`QUELLTEMP_<AnlagenID>` im `ZeitreihenSatz`), das
**Booster-Badge** als reine Anzeigeregel (F9) und die Schließung einer W3-Lücke am Heizkessel.

**Eigenständige** Quellspeicher (Erdsonden-Ersatz mit `WQ_Spreizung`, Start voll) behalten die
statische Quelltemperatur — ihr Ersatzpaar (Spreizung/0) sind keine Speichertemperaturen, eine
Zustandsformel darauf wäre Scheinphysik (Konzept 8.2, letzter Absatz; 7.6).

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Quell-Schnittstelle am Speicher** | `SimulationPufferspeicher.QuellEntnahmeTemperatur` — die EINE Stelle, an der die Quell-Entnahmehöhe sitzt. Bis Q1 (Schritt 54, `WQ_Anschlusshoehe`) fest **oben** = `SchichtTemperatur(0)`; bei N = 1 ist das die Ein-Zonen-Ersatztemperatur `RL_eff + A/Q_max · (VL_eff − RL_eff)` aus Konzept 8.2, ohne Sonderzweig | `SimulationPufferspeicher.cs` |
| **Kopplungsentscheidung** | Merkmal ist die **Rolle der Speicherinstanz**: Ein geteilter Puffer ist ein SENKENspeicher der Registry (`IstQuelle == false`) — genau die Instanz, die `QuellspeicherUebernehmen` seit D5a einsetzt; die eigene Quellinstanz eines eigenständigen Speichers trägt `IstQuelle == true`. Zusätzlich verlangt: `Q_max > 0` und `VL_eff > RL_eff` (ohne Temperaturachse gäbe es nichts zu koppeln) | `SimulationWaermepumpe.BoosterKopplungVorbereiten`, `SimulationControl.KesselQuellbezugSetzen` |
| **Einmal-je-Stunde-Abfrage (WP)** | `SimulationWaermepumpe.Quelltemperatur_Stunde(stunde)` schreibt für alle gekoppelten Module der AKTIVEN Ebene `wp_quelltemp[i][stunde]`. Eigene Vektorkopie je gekoppeltem Modul — `Quelltemperatur` gibt in mehreren Fällen den ÜBERGEBENEN Außentemperaturvektor zurück, mehrere Module teilen sich dann dasselbe Array und es ist zugleich `Temperatur` | `SimulationWaermepumpe.cs` |
| **Einmal-je-Stunde-Abfrage (Kessel)** | `SimulationSPK.Quelltemperatur_Stunde(stunde)` bildet den Quellanteil neu: `Anteil = (T_Quelle − RL)/(VL − RL)`, auf 0…1 geklemmt. **Keine neue Physik** — Formel, Mengenrechnung, die beiden Schranken in `MaxAbgabe` und die Buchung in `QuellwaermeHolen` bleiben Zeichen für Zeichen die von D5a; getauscht ist allein die HERKUNFT von `T_Quelle`. Eigener Setzweg `QuellkopplungSetzen` statt `QuellbezugSetzen`, weil letzteres bei Anteil 0 den Speicherbezug LÖSCHT — beim Booster ist Anteil 0 ein Stundenzustand, kein Ende der Kopplung | `SimulationSPK.cs`, `SimulationControl.KesselQuellbezugSetzen` |
| **Der Lesezeitpunkt** | EINE Zeile in der Ebenenschleife der `Kaskadenschleife`, direkt nach `ModulEbeneSetzen(ebene)` und vor Phase B. Damit: NACH allem, was die vorigen Ebenen in dieser Stunde in den Quellpuffer geladen haben, und VOR jeder eigenen Entnahme; der Wert gilt für die ganze Stunde dieser Ebene. Ohne gekoppeltes Modul kehren beide Aufrufe sofort zurück — der Bestand sieht von der Zeile nichts | `Kaskadenschleife.Rechnen` |
| **F13 — Kappung nach unten** | In `berechne_wptherm`: Für ein GEKOPPELTES Modul gilt unterhalb der untersten Stützstelle deren COP; gezählt werden Stunden **und Temperaturbereich**, gemeldet einmal je Modul am Laufende (`KappungUntenMelden`, Bauart wie `KappungObenMelden`/V0-9). Für alle übrigen Quellen — Außenluft, Erdreich, Profil, CSV, konstante Temperatur, eigenständiger Quellspeicher — bleibt die Projekteinstellung `Extrapolation_erlaubt` und die lineare Verlängerung **unverändert** | `SimulationWaermepumpe.cs` |
| **Quelltemperatur als Laufergebnis** | Je gekoppeltem Modul eine Reihe `QUELLTEMP_<AnlagenID>` im `ZeitreihenSatz` (sprachneutral, ASCII — Schicht 2). Ablage im Modul: WP über die bestehende `Quelltemperaturen`-Liste, Kessel über den neuen Vektor `Quelltemperaturen(index)`. Bewusst NICHT in `z.Speicherreihen` — diese Liste führt das kWh-Füllstandsdiagramm (dieselbe Begründung wie bei `PUFFER_*_TOBEN` aus P1) | `ZeitreihenExtraktor.cs`, `SimulationSPK.cs` |
| **F9 — Booster-Badge** | `Warnkriterien.BoosterAnlagen(idProjekt)` als EINE Wahrheit: Anlage mit `WQ_Typ='Pufferspeicher'`, deren Quellpuffer von mindestens einer ANDEREN Anlage geladen wird. Kein neuer Anlagentyp, kein Schemafeld. Der Chip steht an Wärmepumpe **und** Heizkessel (8.4 Gleichbehandlung), Stil `QuelleKaskade`, Tooltip nennt den Quellspeicher, Doppelklickziel ist die Quelle. Einmal je Kartenauffrischung geholt (Muster `WarnbefundeSammeln`), auch in den Schema-Hinweisen | `Warnkriterien.cs`, `Form_Simulation_Config.Karten.cs`, `.Schema.cs` |
| **W3 am Kessel (Lücke geschlossen)** | `Projektbild.AnlagenVorlauf` fällt beim Heizkessel auf `Tab_Heizkessel.Vorlauf` über `Tab_Energieanlagen.ID_Kessel` zurück — dieselbe Vorrangkette, die die Engine für den Kessel-Quellanteil benutzt. `Tab_Energieanlagen.Vorlauf` ist an Kesseln durchweg 0; ohne den Rückgriff könnte W3 an einem Kessel **nie** anschlagen, obwohl das Kriterium den ERZEUGER meint. Träge (eine Abfrage, erst wenn ein Kessel geprüft wird), nur ein VOLLSTÄNDIGES Paar zählt | `Warnkriterien.cs` |
| **Protokoll des Laufaufbaus** | `SimulationControl.BoosterKopplungVorbereiten` meldet je gekoppelter Anlage Speicher, Temperaturband, Schichtzahl und die F13-Regel; `KesselQuellbezugSetzen` hat für den gekoppelten Fall eine eigene Zeile („Kessel-Kaskade (Booster) …"). `KesselQuelleOhneWirkungMelden` zählt einen gekoppelten Kessel als wirksam, auch wenn sein Anteil beim Aufbau 0 ist — beim Laufaufbau steht der Puffer noch leer | `SimulationControl.cs` |

**Neue Ressourcenschlüssel: 5** (`SIM_KARTE_BOOSTER`, `SIM_KARTE_TIP_BOOSTER`,
`SIM_REIHE_QUELLTEMPERATUR`, `SIMENG_WP_KAPPUNG_UNTEN_HINWEIS`,
`SIMENG_KESSEL_QUELLKOPPLUNG_HINWEIS`), **0 entfernt**; Bestand danach **2574 `data`-Knoten**
je `.resx` (DE/EN deckungsgleich) und **2570 Designer-Eigenschaften**. Einzelnachweis in
[`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md), Abschnitt „Nachtrag Paket B1".

## 3. Warum die Kopplung genau dort liest, wo sie liest

Der SOC eines Puffers ändert sich innerhalb einer Stunde mehrfach: Phase A entlädt, Phase B
entnimmt, die Ladephasen füllen, Phase E entlädt erneut, Phase G zieht die Verluste ab. Eine
Quelltemperatur „aus dem Puffer" wäre ohne festen Lesezeitpunkt **nicht reproduzierbar
spezifiziert** — das Ergebnis hinge davon ab, an welcher Stelle des Rumpfes jemand die Abfrage
einbaut.

Konzept 8.2 legt den Punkt fest: **je Stunde genau einmal, unmittelbar vor Phase B der
Rechenebene der beziehenden Anlage.** Das ist umgesetzt, und die Folge ist beabsichtigt und
messbar: Der Booster sieht den Speicher in **dem** Zustand, den die vorgelagerte Ebene ihm in
dieser Stunde hinterlassen hat. Steht dort ein Erzeuger mit reichlich Leistung, liest der
Booster systematisch die Abschaltschwelle des Speichers (Wirkprobe A1/A2: konstant 9,5 bzw.
59 °C = 95 % des Bandes); wird die Ladung knapp, wandert die Quelltemperatur mit
(Wirkprobe A3: 58 verschiedene Werte in 268 Stunden unterhalb der Schwelle, Kessel K2: 558
Werte). In beiden Fällen gilt exakt

```
QUELLTEMP(h) = RL_eff + min( SOC_Ende(h−1) + Ladung(h) , SchwelleAus · Q_max ) / Q_max
                        · (VL_eff − RL_eff)
```

— auf vier Nachkommastellen nachgerechnet (Abschnitt 4.3 und 4.4).

## 4. Verifikation

### 4.1 Build und Referenzmenge

`WP-Plan.sln` und `Referenzlauf.csproj`, Debug × x64: **0 Fehler**; die verbliebenen fünf
Warnungen (CS0108/CS0109/CS1998) sind Bestand und liegen außerhalb dieses Pakets.

Referenzlauf über die feste Dreizehnermenge (1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024,
1030, 1039, 1040, 1041, 1042) gegen die Basis **`2026-08-28_P1`**:

```
Projekt_1007 … Projekt_1042 : 13 × PASS   (3 532 029 Werte innerhalb der Toleranz)
MD5-Vergleich                : 329 von 329 CSV byte-gleich, 0 Abweichungen,
                               0 fehlende, 0 zusätzliche Dateien
pruefen                      : GESAMT plausibel (keine NaN/Inf)
Laufprotokoll                : 45 verschiedene Meldungen vorher, 45 nachher,
                               Mengenvergleich leer — keine neue, keine entfallene
```

Das ist die zugesagte Messlatte. **Kein Projekt der Referenzmenge hat einen konfigurierten
geteilten Quellpuffer** — 1042 trägt die Booster-Kette, aber die WP-Quelle ist unkonfiguriert
(Warnkriterium `QUELLE_FEHLT`), und 1021 hat einen eigenständigen Quellspeicher. B1 ist auf
diesem Bestand konstruktiv wirkungslos, und genau das weist der Byte-Vergleich nach.

**Vorlauf-Kontrolle:** Vor dem ersten Codeeingriff wurde derselbe Lauf mit dem unveränderten
P1-Stand gefahren — ebenfalls 13/13 PASS und 329/329 byte-gleich. Die Basis war also gültig,
bevor B1 sie berührt hat; die Datenpflege des Anwenders vom 27.08. 23:34 (nach dem Einfrieren
der Basis) hat die Referenzprojekte nicht verändert.

Beide Probeordner sind nach dem Vergleich **gelöscht** — sie wären byte-gleich mit der Basis.
**Die Basis `2026-08-28_P1` bleibt unverändert gültig.**

### 4.2 Wirkprobe Wärmepumpe (Projekt 1042, Wegwerf-Kopie)

Konstellation genau nach Konzept 8.2: Der Kombi-Speicher **1054197** („Stora B 1000-6 ER 1 B")
wird von Heizkessel 14785 und der Luft-Wasser-WP 14806 geladen; die Sole-Wasser-WP
**14807** („CS7800iLW 16", Vorlauf 55 °C, Kennlinie −5 … 25 °C) bekommt ihn per SQL als
Wärmequelle (`WQ_Typ='Pufferspeicher'`, `WQ_ID_Puffer=1054197`) — ein **geteilter** Puffer, und
die WP ist selbst keiner seiner Lader (kein Kurzschluss).

| Runde | Konfiguration | Modul 14807: Produktion / Strom / JAZ | Quelltemperatur |
|---|---|---|---|
| **A0** vorher | keine Quelle → Außenluft-Rückfall | 7 381,545 / 2 390,304 kWh / **3,0881** | Außenluft −18,17 … 33,53 °C, Mittel 9,87 |
| **A1** gekoppelt | Puffer ohne Temperaturpaar (RL_eff 0 / VL_eff 10) | 7 381,571 / **2 365,888** kWh / **3,1200** | konstant **9,5 °C** (= 95 % des Bandes) |
| **A2** warmer Puffer | Puffer 60/40 gepflegt, Ladeleistung 3 kW | 7 375,582 / **1 596,446** kWh / **4,6200** | 42,68 … 59,00 °C, Mittel 58,95 |
| **A3** knappe Ladung | wie A2, nur die Luft-WP als Lader, Ladeleistung 1,5 kW | 7 367,976 / 1 594,800 kWh / 4,6200 | 41,34 … 59,00 °C, Mittel 58,87, **58 verschiedene Werte**, 268 h unter der Schwelle |

Ablesbar:

- **Der COP folgt dem Speicher.** A0 → A1 ist der isolierte Kopplungseffekt: bei praktisch
  identischer Produktion (7 381,545 → 7 381,571 kWh) sinkt der Strombedarf um 24,4 kWh, die
  Modul-JAZ steigt um **+1,03 %**. Klein, weil der Puffer ohne gepflegtes Paar nur ein
  0…10-°C-Band hat. Mit gepflegtem Paar (A2) sind es **−33,2 % Strom** bei gleicher
  Wärmemenge und eine JAZ von 4,62.
- **4,62 ist kein Zufall**, sondern der COP der obersten Stützstelle: Bei 42…59 °C
  Quelltemperatur greift die Kappung nach oben in **8 760 von 8 760 Stunden** — der vom Konzept
  vorhergesagte „Normalfall beim Booster" (F13), jetzt sichtbar statt still.
- **`QUELLE_FEHLT` verschwindet** mit A1 aus Warnkatalog und Laufprotokoll; dafür erscheinen
  der Kaskaden-Hinweis (D5a) und die neue Booster-Zeile.
- **Die Ganglinie ist ein Laufergebnis:** `QUELLTEMP_14807` steht im `ZeitreihenSatz`
  (in A0 gibt es sie nicht).

### 4.3 Stichstunden: die Ganglinie gegen den SOC-Verlauf

Aus Runde A3 (Puffer 60/40, `Q_max` 22,388 kWh, Ladeleistung 1,5 kW, Abschaltschwelle 95 %):

| Stunde | SOC am Ende von h−1 [kWh] | erwartet: 40 + min(SOC+1,5 ; 21,2686)/22,388 · 20 | gemessen `QUELLTEMP(h)` |
|---|---|---|---|
| 49 | 0,0574 | 41,3913 | **41,3913** |
| 50 | 1,2771 | 42,4808 | **42,4808** |
| 51 | 2,5021 | 43,5752 | **43,5752** |
| 52 | 3,7324 | 44,6743 | **44,6743** |
| 53 | 4,9292 | 45,7434 | **45,7434** |
| 54 | 6,0537 | 46,7480 | **46,7480** |
| 55 | 7,0417 | 47,6306 | **47,6306** |
| 56 | 7,7642 | 48,2761 | **48,2761** |

Acht aufeinanderfolgende Stunden eines Wiederauffüllvorgangs, jede auf vier Nachkommastellen
deckungsgleich. Die Quelltemperatur ist damit nachweislich **der Speicherzustand am
Lesezeitpunkt** und keine Kopie eines Eingangsprofils.

### 4.4 F13 — Kappung nach unten mit Protokoll

Für den Nachweis muss die unterste Stützstelle **über** dem Temperaturband des Puffers liegen.
Auf der Wegwerf-Kopie wurden dazu die beiden untersten Stützstellen der Kennlinie
(ID_WP 1672037, Vorlauf 55: −5 °C und 0 °C) entfernt — die Kennlinie beginnt danach bei
**+5 °C**, was der realistischen Datenlage eines Sole-/Wasser-Geräts mit Hochtemperaturquelle
entspricht. Puffer 1054197 wieder ohne Temperaturpaar (Band 0…10 °C).

Ergebnis: Quelltemperatur 1,34 … 9,50 °C, und im Laufprotokoll steht

> *Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet in **40 Stunden** die untere
> Stützstelle der Kennlinie (5,0 °C); gemessen wurden **1,3 bis 4,2 °C**. Für diese Stunden gilt
> der COP der untersten Stützstelle. Die Quelle ist ein geteilter Pufferspeicher — dort wird
> bewusst gekappt statt extrapoliert …*

Der Extrapolationshinweis für dieses Modul **entfällt** dabei, der Lauf bricht nicht ab, und die
Projekteinstellung `Extrapolation_erlaubt` bleibt für die Außenluft-Module unberührt (die
Luft-Wasser-WP 14806 desselben Projekts meldet ihre 957 Kappungsstunden nach oben unverändert).

### 4.5 Wirkprobe Heizkessel (Projekt 1023, Wegwerf-Kopie)

Konstellation nach Konzept 8.4: Puffer **1018023** („Vitocell 140-E 600 Ltr", 65/45,
`Q_max` 13,92 kWh) wird von den beiden Wärmepumpen 11203/11204 geladen; der Heizkessel
**11205** („ecoVIT VKK 186/5", 19,3 kW) bekommt ihn als Wärmequelle. Damit der Hub bestimmbar
ist, wurde am Kessel das Temperaturpaar 65/45 gepflegt (`Tab_Heizkessel` — in der produktiven
Datenbank trägt **kein einziger** der 23 Kessel ein Paar; das ist die eigentliche Ursache dafür,
dass die Kessel-Kaskade bislang nie einen Wert ≠ 0 hatte).

| Größe | K0b: Paar gepflegt, **keine** Quelle | K1: geteilte Quelle, unbegrenzte Ladung | K2: geteilte Quelle, Ladeleistung 6 kW |
|---|---|---|---|
| Quellwärme aus dem Puffer | **0** kWh | **88 047,33** kWh | **45 243,75** kWh |
| Kesselleistung (brennstoffbasiert) Σ | 66 492,14 kWh | 82 749,27 kWh | 107 414,13 kWh |
| Jahresnutzungsgrad | 84,686 % | **86,812 %** | **87,010 %** |
| Quelltemperatur | — (Systemrücklauf) | 61,83 … 65,00 °C, Mittel **64,60** | 53,62 … 65,00 °C, Mittel **56,39**, **558 verschiedene Werte** |
| Quellanteil je Stunde | — | 0,8415 … 1,0000, Mittel 0,9802 | **0,4310 … 1,0000, Mittel 0,5696** |
| **statischer Pfad (vor B1)** hätte gerechnet | — | konstant **1,0000** | konstant **1,0000** |

Die letzte Zeile ist der Kern von 8.4: Der statische Pfad setzte `T_Quelle` = Vorlauf der
Speicherzeile (hier 65 °C) und kam damit auf einen Anteil von **1,0 über das ganze Jahr** — der
Puffer hätte immer die volle Anhebung getragen, auch wenn er leer war. Die Kopplung misst
stattdessen, was wirklich drinsteht: in K2 im Mittel **57 %**, in 6 079 von 8 760 Stunden unter
50 %.

Stichstunden K2 (erwartet `45 + min(SOC+6 ; 13,224)/13,92 · 20`):

| Stunde | SOC am Ende von h−1 | erwartet | gemessen |
|---|---|---|---|
| 2209 | 1,7618 | 56,1520 | **56,1520** |
| 2210 | 2,0650 | 56,5877 | **56,5877** |
| 2211 | 1,9551 | 56,4297 | **56,4297** |
| 2212 | 0,1922 | 53,8968 | **53,8968** |
| 2233 | 4,7034 | 60,3784 | **60,3784** |
| 2234 | 5,8747 | 62,0614 | **62,0614** |

Das Laufprotokoll trägt beide neuen Zeilen — die Einrichtung („Kessel-Kaskade (Booster):
Anlage 11205 … Die Quelltemperatur folgt dem Speicherzustand … 45 … 65 °C …") und die
Jahresbilanz („Heizkessel 'ecoVIT VKK 186/5': Die Eintrittstemperatur folgte dem geteilten
Quellpuffer — über das Jahr 61,8 bis 65,0 °C, im Mittel 64,6 °C …").

**Zur Einordnung der Absolutzahlen:** K0b → K1 mischt zwei Wirkungen, und das ist keine
Schwäche der Messung, sondern die Konfiguration selbst — mit einer Puffer-Quelle wandert der
Kessel auf **Rechenebene 1** (D5a) und rechnet danach hinter seinen Ladern. Die B1-eigene
Wirkung ist die Anteilsspalte, und die ist zwischen statisch (1,0000) und gekoppelt (0,5696)
sauber getrennt.

### 4.6 Statisch-Beleg: eigenständiger Quellspeicher bleibt unberührt (Projekt 1021)

| Befund | Messung |
|---|---|
| Quellspeicher 1018014, Rolle | `Verwendung = Quelle`, `IstQuelle = True` |
| Kopplung des Moduls 10361 | **`gekoppelt: False`** |
| Quelltemperatur | **konstant 10 °C** — der Bestandswert `(Vorlauf + Rücklauf)/2` der Speicherzeile |
| Reihe `QUELLTEMP_10361` | **existiert nicht** |
| `Warnkriterien.BoosterAnlagen(1021)` | **leer** |
| Warnkatalog | unverändert W5 („… wird von keiner Anlage dieses Projekts geladen") |
| Referenzlauf | Projekt_1021 **PASS**, alle 21 CSV **byte-gleich** |

Genau die in Konzept 8.2 verlangte Abgrenzung: Wo das Temperaturpaar ein Ersatzwertpaar
(Spreizung/0) ist, bleibt die Rechnung, wie sie war.

### 4.7 F9 — die Anzeigeregel, auf drei Projekten geprüft

| Projekt | `BoosterAnlagen` | Bewertung |
|---|---|---|
| 1042 (WP-Quelle gesetzt) | Anlage **14807** → Puffer 1054197 | Wärmepumpe am geteilten Puffer → Badge |
| 1023 (Kessel-Quelle gesetzt) | Anlage **11205** → Puffer 1018023 | **Heizkessel** am geteilten Puffer → Badge (8.4 Gleichbehandlung) |
| 1021 (eigenständiger Quellspeicher) | leer | kein Badge — richtig, hier boostet nichts |

Die Regel läuft ausschließlich auf der Konfiguration (kein neues DB-Feld, kein Anlagentyp) und
benutzt dieselbe Engine-Auflösung, gegen die auch W5 und der Ring prüfen.

### 4.8 W3 am Kessel — Lücke belegt und geschlossen

Vor B1: `Warnkriterien` las den Erzeuger-Vorlauf nur aus `Tab_Energieanlagen.Vorlauf`; an
Kesseln steht dort durchweg 0 (nachgemessen an 1023 und 1042). W3 konnte an einem Heizkessel
**nie** anschlagen. Nach dem Rückgriff auf `Tab_Heizkessel` über `ID_Kessel`:

> *[W3] Anlage „ecoVIT VKK 186/5": Der Erzeuger-Vorlauf 50 °C liegt unter dem wirksamen Vorlauf
> 65 °C des Zielspeichers „Vitocell 140-E 600 Ltr".*

**Ohne Wirkung auf den Bestand:** In der produktiven Datenbank trägt kein Kessel ein
Temperaturpaar (23 Zeilen geprüft, 0 gepflegt) — die Meldungsmenge der Referenzmenge ist
unverändert (4.1).

### 4.9 Umgang mit den Daten

Alle Wirkproben liefen auf einer **Wegwerf-Kopie außerhalb des Repos**
(`Referenzlauf.exe migration` aus der produktiven Datei, Schemastand 53). Nach Abschluss wurde
die Kopie gelöscht, **frisch neu angelegt** und nachgeprüft, dass keine der Änderungen
zurückblieb: `WQ_Typ`/`WQ_ID_Puffer` an 14807 und 11205 wieder leer, Puffer 1054197 ohne
Temperaturpaar und mit `Ladeleistung_Max = 0`, Puffer 1018023 wieder 65/45, Kessel 1018254 ohne
Temperaturpaar, `Tab_Kenndaten` für ID_WP 1672037/Vorlauf 55 wieder mit **7** Stützstellen, die
Senkenzeilen von 11205 und 14785 im Ausgangszustand.

**Die produktive `Kenndaten.accdb` wurde nicht beschrieben** — Zeitstempel **27.08.2026
23:34:25** und Größe **151 949 312 Bytes** vor dem ersten und nach dem letzten Schritt
identisch, keine `Kenndaten.laccdb`, kein Access- oder Anwendungsprozess während der Arbeit.

## 5. Was NICHT geändert wurde

- **Die Mengenrechnung.** Weder beim Kessel (`MaxAbgabe`, `QuellwaermeHolen`, die beiden
  Schranken aus E-K1-1) noch bei der Wärmepumpe (Verdampferwärme `PTHERM − PEL`,
  Quellbegrenzung, `QuellentnahmeMelden`) ist eine Zeile angefasst. B1 tauscht ausschließlich
  die Herkunft einer Temperatur.
- **Die Rechenebenen.** Wer nach wem rechnet, entscheidet unverändert D5a
  (`QuellbezuegeAufbauen` → `EbenenAufloesen`). B1 hängt sich nur in die vorhandene Schleife.
- **Die obere Kappung** (V0-9) — sie war schon da und bleibt Wort für Wort; F13 ergänzt die
  Gegenrichtung.
- **`WQ_CSV` und die delimitierten Profile** (`WQ_Monatswerte`/`WQ_Wochenwerte`) — Altlasten
  nach Konzept 8.1/F12, Paket Q1.
- **Der Aufräumauftrag 8.3** hat kein Ergebnis: Der Schnittstellenwechsel macht keine einzige
  Codestelle offensichtlich tot. Der `TYP_PUFFER`-Zweig in `WaermequelleClass.Quelltemperatur`
  trägt weiterhin den eigenständigen Quellspeicher und die Vorbelegung des gekoppelten
  Vektors; `potenzialTherm/El`, `WP_Betriebsart` und die Heapsort-Reste sind unabhängige
  Restposten (B1-O6).

## 6. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| B1-O1 | Die **Quell-Entnahmehöhe** ist bis Q1 fest „oben" (`SimulationPufferspeicher.QuellEntnahmeTemperatur`). Die Spalte `WQ_Anschlusshoehe` entsteht erst mit Schema-Schritt 54; die Methode ist die eine Stelle, an der Q1 sie einsetzt — die Aufrufer in WP und Kessel bleiben dann unverändert | **Q1 / Schritt 54** |
| B1-O2 | Der Lesezeitpunkt liegt konzeptgemäß NACH der Ladephase der Vorebene. Der Booster sieht den Speicher deshalb im am weitesten geladenen Zustand der Stunde; bei reichlich Ladeleistung ist die Quelltemperatur systematisch die Abschaltschwelle (Wirkprobe A1/A2). Das ist gewollt und dokumentiert, aber es lohnt die Nachfrage beim Produktverantwortlichen, ob nicht der Zustand VOR der Ladephase die konservativere Aussage wäre | Rückfrage |
| B1-O3 | Das **Referenzlauf-Werkzeug** exportiert keine `QUELLTEMP`-CSV — dieselbe Linie wie P1-O1/E1-O3: Es ist das Messinstrument, geänderte Dateinamen machten jeden Vergleich mit älteren Ständen unmöglich. Aufnahme nur mit einem Basiswechsel | Basiswechsel |
| B1-O4 | Die **Schema-Kaskadenkante** unterscheidet geteilten und eigenständigen Quellpuffer nicht (1021 bekommt sie ebenso). Das Booster-Kennzeichen steht auf der Erzeugerkarte und — über `ErzeugerChips` — in den Schema-Hinweisen; eine eigene Kantensignatur wäre ein Eingriff in Legende und `Kantenart`-Enum und ist nicht trivial | P2/Bericht |
| B1-O5 | **P1-O6 ist erledigt**, nicht offen: `SchichtparameterUebernehmen` liest `Entnahme_Heizung/_BW/_Prozess` bereits über den Helfer `Entnahmehoehe(r, …)` mit den Konzept-Defaults bei NULL (Klassen-Set-abhängig 0,5/1,0). Nachgeprüft an beiden Aufrufstellen (Registry-Aufbau und BHKW-Ersatzpendelspeicher) und an `StilleDb.Feld`, das für NULL und fehlende Spalte gleichermaßen `null` liefert. Das Ticket im P1-Protokoll ist zu schließen | erledigt |
| B1-O6 | 8.3-Restposten (`potenzialTherm/El`, `WP_Betriebsart`, Heapsort-Reste, `Rest_Speicher`-Gruppe, `MAX_WP`-Fehlertext, COP-Plausibilitätsguard) bleiben — keiner davon wird durch den Schnittstellenwechsel tot, und ein Aufräumschnitt im selben Paket hätte den Byte-Nachweis vermischt | Paket L |
| B1-O7 | Der Lokalisierungskatalog hat **keinen Nachtrag für Paket P1** (dessen 27 Schlüssel sind nur im P1-Protokoll gezählt). B1 hat seinen eigenen Nachtrag angelegt; der P1-Nachtrag fehlt weiter | Paket L |
| B1-O8 | **Der Booster-Fall ist in der Referenzmenge nicht abgedeckt.** 1042 heißt „Booster-Kette mit Kombi-Speicher", trägt die Kette aber unkonfiguriert (`QUELLE_FEHLT` seit S2). Solange das so bleibt, sichert kein Referenzlauf die B1-Rechnung ab — nur die Wirkproben dieses Protokolls. Empfehlung: 1042 in der produktiven Datenbank konfigurieren (WP 14807 → Puffer 1054197, Temperaturpaar am Kombi-Speicher) und die Basis danach neu setzen | Anwender / Orchestrator |
| B1-O9 | Die Protokolltexte des Laufaufbaus (`BoosterKopplungVorbereiten`, `KesselQuellbezugSetzen`) sind inline deutsch wie ihr Nachbarbestand; nur die beiden Modul-Meldungen am Laufende sind lokalisiert | Paket L (P1-O7/S2-O9) |
| B1-O10 | `Tab_Heizkessel.Vorlauf/Ruecklauf` sind im gesamten produktiven Bestand ungepflegt (0 von 23). Damit bleibt die Kessel-Kaskade auch nach B1 in der Praxis wirkungslos, bis der Anwender das Paar pflegt — die Engine meldet den Grund seit D5a („Temperaturpaar für den Hub ist nicht bestimmbar"), und W3 am Kessel kann erst dann anschlagen | Anwender |
