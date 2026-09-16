# Befund H — Ost/West-Messung und Rechenzeit des Prototyps (15.09.2026)

**Protokoll.** Befund eines Prüf-Agenten (Modell Opus) im Auftrag des Konzepts
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Sitzung vom 15.09.2026, als Nachlauf zu den Befunden D bis G (das „billigste fehlende
Experiment" der Kritik). Wortlaut wie geliefert; Speicherindizes 0-basiert in UTC-Reihenfolge.
Pfade unter `epos-spike` bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

## 1. Ost/West-Messung

**Feldbefund vorab:** Das JSON-Feld heißt `Fensterflaeche_Ost_West` (kein Feld `Fensterflaeche_Ost`). Gelesen in `EposLauf\Epos.cs:67`, aufgeteilt bisher fest hälftig in `Epos.cs:261`. Für die Messung wurde `Opt.OstAnteil` ergänzt (Vorgabe 0,5 = unverändertes Verhalten); Gegenprobe: der komplette `EposLauf`-Ausgabetext ist zeichengleich mit der gespeicherten `lauf.txt`.

12 Projekte mit Gebäude (1030 hat keines), 15 Gebäude. Klimadaten unterscheiden Ost und West: Jahressumme Ost liegt 7,3 % (München) bis 9,3 % (Stuttgart) über West, Tagesmaximum bei Index 8 (Ost) gegen 14 (West).

### Jahresheizwärme und Spitze

| Projekt | 50/50 kWh | 100 % Ost | 100 % West | Spanne % | Spitze 50/50 / Ost / West kW | Index | Tagesmittel-Max kW (50/50 / Ost / West) |
|---|---|---|---|---|---|---|---|
| 1007 | 62 566 | 62 055 | 63 375 | 2,11 | 39,66 / 39,70 / 39,63 | 1398 | 25,31 / 25,40 / 25,22 |
| 1008 | 103 833 | 103 375 | 105 728 | 2,27 | 54,88 / 54,89 / 54,88 | 1398 | 39,01 / 39,15 / 38,86 |
| 1017 | 83 751 | 83 026 | 84 751 | 2,06 | 56,45 / 56,50 / 56,40 | 1398 | 32,57 / 32,69 / 32,44 |
| 1018 | 62 332 | 62 119 | 62 582 | 0,74 | 33,26 / 33,26 / 33,25 | 438 | 22,06 / 22,09 / 22,03 |
| 1023 | 412 781 | 408 677 | 418 831 | 2,46 | 276,60 / 277,26 / 275,94 | 1398 | 171,33 / 172,19 / 170,48 |
| 1024 | 412 781 | 408 677 | 418 831 | 2,46 | 276,60 / 277,26 / 275,94 | 1398 | 171,33 / 172,19 / 170,48 |
| 1039 | 530 468 | 525 925 | 537 037 | 2,10 | 334,84 / 335,52 / 334,16 | 1398 | 214,14 / 215,04 / 213,24 |
| 1040 | 63 677 | 63 427 | 63 982 | 0,87 | 33,56 / 33,57 / 33,55 | 1398 | 24,03 / 24,06 / 24,00 |
| 1041 | 63 677 | 63 427 | 63 982 | 0,87 | 33,56 / 33,57 / 33,55 | 1398 | 24,03 / 24,06 / 24,00 |
| 1042 | 63 677 | 63 427 | 63 982 | 0,87 | 33,56 / 33,57 / 33,55 | 1398 | 24,03 / 24,06 / 24,00 |
| 1045 | 63 677 | 63 427 | 63 982 | 0,87 | 33,56 / 33,57 / 33,55 | 1398 | 24,03 / 24,06 / 24,00 |
| 1046 | 62 566 | 62 055 | 63 375 | 2,11 | 39,66 / 39,70 / 39,63 | 1398 | 25,31 / 25,40 / 25,22 |

Der Spitzenindex ist in allen drei Varianten identisch — die Spitze fällt in eine Stunde ohne Einstrahlung, dort ist die Orientierung wirkungslos. Spitzenlast ändert sich um höchstens 0,24 % gegen den 50/50-Fall (volle Spanne 0,48 %), das Tagesmittel-Maximum um höchstens 0,50 % (volle Spanne 1,0 %).

### Morgen- und Abendlast (Jahressumme je Tagesstunde, UTC, kWh)

| Projekt | Σ Stunden 5–9: 50/50 / Ost / West | Δ Ost / Δ West % | Σ Stunden 15–19: 50/50 / Ost / West | Δ Ost / Δ West % | Morgen/Abend-Faktor |
|---|---|---|---|---|---|
| 1007, 1046 | 18 910 / 18 000 / 20 041 | −4,81 / +5,98 | 11 600 / 12 048 / 11 207 | +3,86 / −3,39 | 1,63 |
| 1008 | 23 109 / 21 696 / 25 484 | −6,11 / +10,28 | 20 571 / 21 265 / 20 113 | +3,38 / −2,23 | 1,12 |
| 1017 | 27 954 / 26 789 / 29 279 | −4,17 / +4,74 | 16 220 / 16 796 / 15 704 | +3,55 / −3,19 | 1,72 |
| 1018 | 20 648 / 20 202 / 21 114 | −2,16 / +2,26 | 11 885 / 12 088 / 11 691 | +1,71 / −1,63 | 1,74 |
| 1023, 1024 | 126 655 / 120 941 / 133 677 | −4,51 / +5,54 | 79 294 / 81 962 / 76 916 | +3,36 / −3,00 | 1,60 |
| 1039 | 158 554 / 152 092 / 166 364 | −4,08 / +4,93 | 101 343 / 104 334 / 98 663 | +2,95 / −2,64 | 1,56 |
| 1040/1041/1042/1045 | 17 660 / 17 224 / 18 131 | −2,47 / +2,67 | 11 626 / 11 806 / 11 456 | +1,56 / −1,46 | 1,52 |

Einzelstunden (Beispiel 1023, Stunde 8): 24 514 → 22 555 (Ost) / 26 918 (West), also −8,0 %/+9,8 % — der größte Einzeleffekt im ganzen Satz.

### Stunden über 24 °C (Projektebene; Stunde zählt, wenn mindestens ein Gebäude darüber liegt)

| Projekt | 50/50 | Ost | West | Δ Ost / Δ West % |
|---|---|---|---|---|
| 1007, 1046 | 939 | 1016 | 924 | +8,2 / −1,6 |
| 1008 | 1729 | 1743 | 1666 | +0,8 / −3,6 |
| 1017 | 794 | 828 | 775 | +4,3 / −2,4 |
| 1018 | 473 | 479 | 459 | +1,3 / −3,0 |
| 1023, 1024, 1039 | 1425 | 1486 | 1389/1390 | +4,3 / −2,5 |
| 1040/1041/1042/1045 | 495 | 497 | 507 | +0,4 / +2,4 |

### Beurteilung der Behauptung — widerlegt in der starken Form

Die Morgen-/Abendstruktur ist auch ohne Ost/West-Trennung vorhanden und quantitativ dominant: die Morgensumme liegt im Basisfall um Faktor 1,12 bis 1,74 über der Abendsumme. Diese Asymmetrie stammt aus Nachtabsenkung und Außentemperaturgang, nicht aus der Fensterorientierung.

Die komplette Unsicherheit aus der fehlenden Trennung (voller Spann 100 % Ost gegen 100 % West) beträgt: Jahreswärme ≤ 2,46 %, Spitzenlast ≤ 0,48 %, Tagesmittel-Maximum ≤ 1,0 %, Morgensumme −6,1 % bis +10,3 %, Abendsumme −3,4 % bis +3,9 %, Stunden > 24 °C −3,6 % bis +8,2 %. Die 50/50-Aufteilung liegt dabei innerhalb ~1 % der Mitte beider Extreme.

Richtig bleibt nur die schwache Fassung: wer die Morgen-/Abendsummen auf besser als etwa 5 % auflösen will, braucht die getrennten Flächen. Die Klimadaten würden das hergeben — es fehlt allein die Flächenaufteilung in der Datenbank.

## 2. Rechenzeit

Protokoll: Release ohne Debugger, 20 Kerne, Workstation-GC. Globaler Warmlauf über alle gemessenen Fälle, erst danach 10 Wiederholungen je Fall, Median, zwei vollständige Durchgänge zur Kontrolle. Stopwatch nur um die `Advance`-Schleife.

| Fall | Regelung | Schritte | Median ms | µs/Schritt | ms je 8760 | ms je 9480 | Umschaltereignisse |
|---|---|---|---|---|---|---|---|
| (a) EPOS-Jahreslauf, Median über 15 Gebäude | ideal, QMin 0 / QMax ∞ | 9480 | **5,09** (4,51–9,09) | 0,48–0,96 | — | **5,09** | 203–826 je 8760 h |
| (b) Testfall 7 | ideal, ±500 W | 1440 | 0,556 | 0,39 | 3,38 | 3,66 | 3 |
| (c) Testfall 1 | Freilauf | 1440 | 0,325 | 0,23 | 1,98 | 2,14 | 0 |
| Kontrolle: Testfall 6 | ideal, unbegrenzt | 1440 | 0,497 | 0,34 | 3,02 | 3,27 | 0 |
| Kontrolle: Testfall 11 | Kühldecke, ±500 W | 1440 | 2,629 | 1,83 | 15,99 | 17,31 | 239 |

Durchgang 1 und 2 stimmen auf < 2 % überein.

**Erklärung der Differenz zu den Befunden E und F.** Die 87–121 ms je Jahr (Befund E) sind die Spalte `ms_8760` aus `validierung.csv` — eine Hochrechnung aus dem einzigen, kalten Lauf über 1440 Schritte, in dem die gestufte Kompilierung noch läuft (Kaltmessung: TF 1 = 74,8, TF 6 = 117,0, TF 7 = 125,0 ms je 8760). Gleicher Effekt im Adapter: der allererste `Rechne`-Aufruf in einem frischen Prozess kostet 237 ms, der zweite 129–172 ms, ab dem dritten 10–13 ms, eingeschwungen 5 ms. Die 2,35 s für rund 100 Läufe (Befund F) sind eine Programmlaufzeit: Prozessstart, JIT-Anlauf, Einlesen von 13 Klimadateien (allein 338 ms JSON-Parsen), Altmodell und ~6,5 MB CSV-Ausgabe; der Löseranteil sind 75 warme Jahresläufe in 0,43 s = 5,74 ms je Lauf.

Strukturelle Anteile: Freilauf ist am billigsten (eine `Discretisation` je Schritt statt zwei); Testfall 7 hat trotz Begrenzung nur 3 Ereignisse in 1440 Stunden; der EPOS-Lauf mit `QMin = 0` hat 203–826 Ereignisse je Jahr, jedes mit 60 Bisektionsiterationen — Marginalkosten rund 7,4 µs je Umschaltereignis, das erklärt die Spanne 4,5–9,1 ms zwischen den Gebäuden vollständig; der Adapter baut je Schritt Lasten und Sollwert neu auf (0,54 gegen 0,39 µs/Schritt).

**Zahl für das Konzeptpapier:** rund 5 ms je Gebäude und Jahr (9480 Stundenschritte einschließlich 720 h Vorlauf), Median 5,09 ms, Spanne 4,5–9,1 ms über die 15 Gebäude; Planungsgröße 10 ms je Gebäude und Jahr. Einmalig je Prozess kommen rund 0,25 s JIT-Anlauf hinzu.

## 3. Kleinigkeiten

**(a) `spez_Waermeverbrauch` Gebäude 10576:** 112,5 (Rohtext in `daten\gebaeude_1008.json`: `"spez_Waermeverbrauch":112.5`). Der Gebäudename `MFH-H-U-112` trägt den gerundeten Wert.

**(b) Zwölf Maximalabweichungen aus `Prototyp\out\validierung.csv`** nach erneutem `--all`-Lauf (ohne `--tc11-window`):

| Testfall | Größe | Toleranz | max. Abweichung | bestanden |
|---|---|---|---|---|
| 1 | TAir | 0,15 K | 0,0553 K | ja |
| 2 | TAir | 0,15 K | 0,0516 K | ja |
| 3 | TAir | 0,15 K | 0,0590 K | ja |
| 4 | TAir | 0,15 K | 0,0558 K | ja |
| 5 | TAir | 0,15 K | 0,0579 K | ja |
| 6 | Q | 1,5 W | 1,4986 W | ja |
| 7 | Q | 1,5 W | 0,6389 W | ja |
| 8 | TAir | 0,15 K | 0,0504 K | ja |
| 9 | TAir | 0,15 K | 0,1363 K | ja |
| 10 | TAir | 0,15 K | 0,1430 K | ja |
| 11 | Q | 1,5 W | 4,9183 W | nein (mit `--tc11-window`: 1,376 W, ja) |
| 12 | TAir | 0,15 K | 0,0538 K | ja |

**(c) Luftkapazität VAir:** in elf der zwölf Testfälle 0 (`referenz\mo\TestCase1.mo:22` usw.), einzige Ausnahme Testfall 12 mit 0,1 m³ (`TestCase12.mo:27`). Im C#-Zonenmodell existiert die Größe nicht (`Prototyp\Zone.cs:5-24` führt nur `CExt` und `CInt`); der Luftknoten wird algebraisch eliminiert (`Prototyp\Solver.cs:32`). Der EPOS-Adapter setzt dieselben `ZoneParams` (`EposLauf\Epos.cs:241-260`) — physikalisch vertretbar (Zeitkonstante der Raumluft ≪ 1 h), aber der Grund, warum die 7R2C-Zone keine Trägheit unterhalb der Stunde besitzt.

**Arbeitsspuren:** neue Ordner `MessEpos\` und `MessProto\` im Arbeitsordner; Ausgaben `Prototyp\out\ostwest_projekte.csv`, `ostwest_gebaeude.csv`, `zeit_epos.csv`, `zeit_prototyp.csv`, `mess_epos.txt`, `mess_proto.txt`, `mess_epos_kalt.txt`. An `EposLauf\Epos.cs` drei rein additive Änderungen (`Opt.OstAnteil`, `Modell.LetzteLoeserMs`, `GebErgebnis.Segmente`), Original als `Epos.cs.bak` daneben. Im Repositorium wurde nichts geändert, die Testdatenbank nicht geöffnet.
