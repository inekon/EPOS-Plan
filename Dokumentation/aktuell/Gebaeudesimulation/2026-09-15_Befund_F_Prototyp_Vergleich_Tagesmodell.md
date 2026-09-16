# Befund F — Prototyp gegen das heutige Tagesmodell an den Referenzprojekten (15.09.2026)

**Protokoll.** Befund eines Prüf-Agenten (Modell Opus, Workflow ‚gebaeudemodell-pruefen‘, Stufe Prototyp 2) im Auftrag des Konzepts [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Wortlaut wie geliefert; Zeilenbelege und Zahlen sind im Konzept nach dem Gegenlesen korrigiert, bei Widerspruch gilt das Konzept. Pfade unter `C:\Users\…pos-spike\` bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

# Befund „Prototyp 2 — Vergleich mit dem heutigen Modell"

Im Repository `C:\Waermeplan\EPOS-Plan` wurde nichts angelegt oder geändert (`git status --porcelain` liefert unverändert dieselben 24 Einträge wie zu Sitzungsbeginn). Die Testdatenbank wurde in diesem Schritt gar nicht geöffnet — gerechnet wurde ausschließlich auf den JSON-Abzügen aus dem Befund „Daten".

---

## 1. Werkzeuge und Laufweg

Neues Projekt **`C:\Users\Dirk\AppData\Local\Temp\epos-spike\EposLauf\`** (net10.0, Exe `EposLauf`, kein NuGet-Paket):

| Datei | Zeilen | Inhalt |
|---|---|---|
| `EposLauf.csproj` | 22 | bindet den **validierten Lösekern unverändert per `<Compile Include="..\Prototyp\*.cs" Link=…>`** ein: `Mat2.cs`, `Zone.cs`, `Solver.cs`, `Simulation.cs`. Kein Byte am Prototyp geändert. |
| `Epos.cs` | 320 | EPOS-Adapter: `Geb`, `Klima` (inkl. Kusuda), `Opt` (Parametersatz/Varianten), `Modell.Rechne(...)`. Ersetzt `Cases.cs` als Lader, wie in Befund 1 Abschnitt 1 vorgesehen. `ControlSpec` ist hier identisch neu deklariert (im Prototyp steht der Typ in `Cases.cs:5`). |
| `Altmodell.cs` | 66 | Nachbildung von `SpezWaermeverlusteC`/`SolareGewinneC`/`TaeglHeizlastWG` (EPOS.Kern/Allgemein/BhkwPlan.cs:311-435) als Gegenprobe. |
| `Program.cs` | 220 | Lauf über alle Projekte, CSV-Ausgabe, Varianten, Empfindlichkeitsprobe. |

**Laufzeit gesamt 2,35 s** für 12 Projekte × 15 Gebäudezeilen × 5 Parametersätze (rund 100 Jahresläufe à 9480 Stunden inkl. Vorlauf) plus 12 Klima-JSON (je 8760 Objekte) und 30 Altmodell-Jahresläufe. Der Löser selbst ist damit für EPOS unkritisch.

**Ausgabe** in `C:\Users\Dirk\AppData\Local\Temp\epos-spike\Prototyp\out\`:
`vergleich_1007.csv` … `vergleich_1046.csv` (**12 Dateien**, 8761 Zeilen, Spalten `stunde;T_aussen_C;Q_proto_W;Q_heute_W;Q_proto_abs_W;Q_proto_erd045_W` und je Gebäude `T_luft_<id>_C;T_soll_<id>_C;Q_<id>_W`), `vergleich_zusammenfassung.csv`, `vergleich_monate.csv`, `empfindlichkeit_1045.csv`, `lauf_vergleich.txt` (vollständiges Protokoll).
Für **Projekt 1030 gibt es keine Datei** — es hat kein Gebäude (`gebaeude_1030.json` = `[]`), der Prototyp ist dort nicht anwendbar; sein Wärmebedarf kommt aus Ganglinien.

---

## 2. Umsetzung der Abbildungsregel G1 — und die vier bewussten Abweichungen

Umgesetzt wie vorgegeben: V = Wohnflaeche·Raumhoehe, A_floor = Wohnflaeche (Katalogflächen, Skalierungsfaktor **erst auf das Ergebnis**, `Modell.Rechne`, Epos.cs:307); C_ges = Bauweise·3600 J/Wh, C1,AW = 0,3·C_ges, C1,IW = 0,7·C_ges; R1,AW = 1/(A_opak·9,1), RRest,AW = 1/ΣU·A − R1,AW (Klemme 1e−6); A_IW = 2,5·A_floor, R1,IW = 1/(A_IW·9,1); h_conv = 2,7, h_rad = 5,0; H_ve = n·V·0,34; innere Gewinne 50/50; Sollwert Tag/Nacht, Vorlauf 30 Tage Dezember verworfen; Heizung ideal, unbegrenzt, 100 % konvektiv, `QMin = 0` ⇒ **keine Kühlung, freier Lauf über den Sollwert**; Zeitreihen in Speicherreihenfolge (UTC).

**Abweichung 1 — Fenster und Wärmebrücken laufen über den Lüftungspfad.** `ZoneNetwork` (Zone.cs:26-79) kennt keinen masselosen Fenster-Leitwert; `GWin` ist dort der g-Wert für den Solareintrag, nicht ein U·A. Ein masseloser Leitwert gegen θ_out zum Luftknoten ist aber **exakt** derselbe Zweig wie die Lüftung. Deshalb: `Gve = H_ve + U_F·A_F + Σψ·L`, `T_ve = θ_out`. Das ist algebraisch identisch zur geforderten Abbildung und erforderte **keine Zeile Änderung am Lösekern**.

**Abweichung 2 — ein Massepfad mit U·A-gewichteter äquivalenter Außentemperatur.** Das 7R2C hat genau **eine** Randtemperatur am AW-Massepfad. Grundfläche, Wand, Dach und Sonstiges haben aber verschiedene Randtemperaturen. Gerechnet wird daher wie in AixLib/VDI 6007 über Gewichtsfaktoren:
θ_eq = (U_W·A_W·θ_eq,W + U_D·A_D·θ_eq,D + U_S·A_S·θ_eq,W + U_G·A_G·θ_Erd) / ΣU·A — analog zu `T_EqAir = Σ T_EqWall·wfWall + T_Gro·wfGro` (Befund 1, Abschnitt 3). A_opak = A_AW + A_Dach + A_Sonst + A_Grund geht in GcAW, GRad, R1,AW und RRest,AW ein.

**Abweichung 3 — Strahlung nur auf AW und IW normiert.** `AWin = [0]` im Netz gesetzt, damit `FRadExt = A_opak/(A_opak+A_IW)`, `FRadInt = A_IW/(A_opak+A_IW)` (Zone.cs:60-62). Sonst ginge der auf die masselosen Fenster entfallende Strahlungsanteil verloren, weil es keinen Fensteroberflächenknoten gibt. Entspricht der Vorgabe „Solargewinn zu 100 % radiativ auf die Oberflächen".

**Abweichung 4 — Kusuda-Amplitude aus der ersten Harmonischen, nicht aus (max−min)/2.** (max−min)/2 der Tagesmittel ergibt für Stuttgart **18,75 K** und damit eine Erdreichtemperatur von **−2,97 … 22,72 °C in 1 m Tiefe** — unphysikalisch. Ausgleich der Tagesmittel mit T(d) = T_m + a·cos(ωd) + b·sin(ωd) (Epos.cs:137-153) ergibt:

| Region | T_mittel | Amplitude (Harmonische) | (max−min)/2 roh | kältester Tag | θ_Erd (1 m) min/max |
|---|---|---|---|---|---|
| Stuttgart (11 Regionen) | 9,875 °C | **9,315 K** | 18,75 K | 19 | **3,50 / 16,25 °C** |
| München (1018047) | 9,575 °C | **8,605 K** | 15,54 K | 11 | **3,68 / 15,47 °C** |

Dämpfung exp(−1·√(π/(365·0,06))) = 0,6847, Phasenverzug ½·√(365/(π·0,06)) = 22,0 Tage.

**Weitere Festlegungen, die zu nennen sind.** Sollwertfenster wie im Altmodell `h = 7…22` mit **h = Stunde-des-Tages + 1** (BhkwPlan.cs:403-407) — damit ist der Vergleich fair. Wochenend- und Ferienabsenkung sind in **allen 15 Zeilen wirkungslos** (Flag `Wochenende` = 0, `Raumsolltemperatur_Wochenende` = 0 oder NULL, `Ferienbeginn_1` = 366); die Logik ist implementiert (Epos.cs, `Regel`), greift aber nie. **Ferien wurden weggelassen** und das ist hier folgenlos. Die Ortszeitverschiebung (UTC → MEZ +1 h / MESZ +2 h, `SolardatenCtrl.ReadOrtszeit`, EPOS.Kern/Controller/SolardatenCtrl.cs:156-208) ist **nicht** angewandt — beide Modelle laufen auf denselben UTC-Reihen, die Verschiebung fällt aus dem Vergleich heraus.

---

## 3. Ergebnis je Projekt und Gebäude

Referenz „heute" je Projekt = Summe von `referenz_<projekt>_gebaeude.csv` (W je Stunde); **je Gebäude** = `Heizwaerme_nachgerechnet_kWh` aus dem Befund „Daten" (auf < 0,01 % gegen die Referenzreihe geprüft, in diesem Lauf mit `Altmodell.cs` erneut auf **< 0,001 %** bestätigt, s. Abschnitt 7).

| Projekt | Geb | m² | **Proto kWh** | **heute kWh** | **Abw %** | Proto kWh/m²a | heute kWh/m²a | Katalog kWh/m²a |
|---|---|---|---|---|---|---|---|---|
| 1007 | 10614 | 340 | 62 566 | 53 072 | **+17,9** | 184,0 | 156,1 | 212 |
| 1008 | 10576 | 800 | 90 216 | 43 267 | **+108,5** | 112,8 | 54,1 | **112** |
| 1008 | 10577 | 74 | 13 617 | 11 551 | +17,9 | 184,0 | 156,1 | 212 |
| 1008 | **Σ** | 874 | **103 833** | **54 818** | **+89,4** | 118,8 | 62,7 | — |
| 1017 | 10599 | 744 | 83 751 | 62 965 | **+33,0** | 112,6 | 84,6 | 118 |
| 1018 | 10632 | 500 | 62 332 | 46 881 | **+33,0** | 124,7 | 93,8 | 136 |
| 1023 | 10628 | 3 596 | 412 781 | 329 797 | **+25,2** | 114,8 | 91,7 | 130 |
| 1024 | 10629 | 3 596 | 412 781 | 329 797 | +25,2 | 114,8 | 91,7 | 130 |
| 1030 | — | — | — | 0 | — | — | — | — |
| 1039 | 10642 | 130 | 39 982 | 36 451 | +9,7 | 307,6 | 280,4 | 338 |
| 1039 | 10643 | 201 | 77 705 | 79 373 | **−2,1** | 386,6 | 394,9 | 451 |
| 1039 | 10644 | 3 596 | 412 781 | 329 797 | +25,2 | 114,8 | 91,7 | 130 |
| 1039 | **Σ** | 3 927 | **530 468** | **445 620** | **+19,0** | 135,1 | 113,5 | — |
| 1040/41/42/45 | 10645/46/47/51 | 201 | 63 677 | 59 354 | **+7,3** | 316,8 | 295,3 | 0 (leer) |
| 1046 | 10652 | 340 | 62 566 | 53 072 | +17,9 | 184,0 | 156,1 | 212 |

**Der Prototyp liegt durchgängig 7–33 % über dem heutigen Modell** (Ausreißer 1008 s. Abschnitt 7, Einzelfall 10643 −2,1 %). Bemerkenswert: **der Prototyp trifft die Katalogkennzahl `spez_Waermeverbrauch` deutlich besser als das Altmodell** — 10576: 112,8 gegen Katalog 112 (Altmodell 54,1); 10599: 112,6 gegen 118 (Altmodell 84,6); 10628: 114,8 gegen 130 (Altmodell 91,7); 10643: 386,6 gegen 451 (Altmodell 394,9). Das Altmodell erreicht die Katalogkennzahl nur zu 48–88 %, der Prototyp zu **86–101 %**.

### Spitzenlast

| Projekt | Proto max kW | Stunde | heute max kW | Stunde | **Proto Tagesmittel-Max** | **heute Tagesmittel-Max** |
|---|---|---|---|---|---|---|
| 1007/1046 | 39,66 | 1399 (28. Feb, 7 h) | 34,99 | 452 (19. Jan, 20 h) | 25,31 | 21,44 |
| 1008 | 54,88 | 1399 | 37,82 | 1363 (26. Feb, 20 h) | 39,01 | 23,17 |
| 1017 | 56,45 | 1399 | 35,95 | 427 (18. Jan, 19 h) | 32,57 | 26,82 |
| 1018 | 33,26 | 439 (19. Jan, 7 h) | 34,10 | 8372 (15. Dez, 20 h) | 22,06 | 19,35 |
| 1023/1024 | 276,60 | 1399 | 193,96 | 8358 (15. Dez, 6 h) | 171,33 | 144,11 |
| 1039 | 334,84 | 1399 | 253,02 | 427 | 214,14 | 185,41 |
| 1040–1045 | 33,56 | 1399 | 37,29 | 1388 (27. Feb, 20 h) | 24,03 | 22,85 |

**Die Prototyp-Spitze liegt in 10 von 12 Projekten exakt auf Stunde 1399** — das ist der 28. Februar, die **erste Stunde nach dem Ende der Nachtabsenkung** (Sollwertsprung 18 → 20 °C) am kältesten Tag des Stuttgarter Datensatzes. Der ideale, unbegrenzte Heizer deckt den Sprung in einer Stunde; das Altmodell kennt diese Spitze prinzipiell nicht, weil es die **Tagessumme** über `Abfrage_Tagverteilung` verteilt (SimulationWaermebedarf.cs:601-608, 670). Das ist **kein physikalischer, sondern ein Regelungs-/Auflösungsunterschied**. Rechnet man die Spitze als **Tagesmittel**, schrumpft die Differenz von +43 % (1007) bzw. +43 % (1023) auf **+18 % / +19 %** und liegt damit genau im Band der Jahresenergie-Abweichung.

### Profiltreue

| Projekt | r (Stundenreihe) | r (Tagessummen) | RMSE kW | Mittelwert heute kW |
|---|---|---|---|---|
| 1007/1046 | 0,839 | **0,995** | 4,27 | 6,06 |
| 1008 | 0,736 | 0,982 | 9,37 | 6,26 |
| 1017 | 0,881 | 0,987 | 5,68 | 7,19 |
| 1018 | 0,822 | 0,985 | 4,55 | 5,35 |
| 1023/1024 | 0,910 | 0,990 | 24,69 | 37,65 |
| 1039 | 0,925 | 0,993 | 26,68 | 50,87 |
| 1040–1045 | 0,831 | **0,997** | 4,10 | 6,78 |

**Auf Tagesebene stimmen beide Modelle sehr gut überein (r = 0,98…0,997); der Bruch liegt in der Stundenverteilung.** Jahressumme je Tagesstunde für Projekt 1045 (kWh):

| Stunde (UTC) | 0 | 3 | 6 | 7 | 8 | 12 | 18 | 20 | 23 |
|---|---|---|---|---|---|---|---|---|---|
| Prototyp | 2376 | 2880 | **4389** | 3910 | 3379 | 2132 | 2489 | 2819 | 2165 |
| heute | 1008 | 995 | 2717 | 3647 | 3820 | 2630 | **3573** | **3681** | 1064 |

Das Altmodell hat ein **Zwei-Buckel-Profil (7–9 h und 18–21 h) mit gekappter Nacht**; der Prototyp hat **eine Aufheizspitze um 6–7 h und eine deutliche Grundlast in der Nacht** (2376–3043 kWh je Nachtstunde gegen 995–1177 im Altmodell). Die Nachtabsenkung steckt im Altmodell nicht in der Physik, sondern im Verteilungsprofil. Zugleich hat der Prototyp **2097 Nullstunden gegen 936** beim Altmodell: das Tagesprofil schmiert auch an Übergangstagen Last auf alle 24 Stunden, während der Prototyp bei Solargewinn frei läuft.

### Monatssummen (kWh)

**1007 und 1046** (identische Gebäudezeile, verschiedene Klimaregion-Kopien desselben Orts)

| | Jan | Feb | Mrz | Apr | Mai | Jun | Jul | Aug | Sep | Okt | Nov | Dez |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Prototyp | 11 314 | 12 140 | 9 238 | 2 161 | 1 403 | 1 141 | 24 | 611 | 955 | 5 758 | 7 483 | 10 339 |
| heute | 10 009 | 10 533 | 7 452 | 1 521 | 1 066 | 896 | 15 | 413 | 698 | 4 797 | 6 279 | 9 392 |
| Abw % | +13 | +15 | +24 | +42 | +32 | +27 | +52 | +48 | +37 | +20 | +19 | +10 |

**1017**

| | Jan | Feb | Mrz | Apr | Mai | Jun | Jul | Aug | Sep | Okt | Nov | Dez |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Prototyp | 15 625 | 16 327 | 12 638 | 3 188 | 1 853 | 1 300 | 15 | 562 | 984 | 7 296 | 9 775 | 14 189 |
| heute | 12 567 | 13 023 | 8 842 | 1 196 | 857 | 716 | 0 | 317 | 347 | 5 691 | 7 473 | 11 936 |
| Abw % | +24 | +25 | +43 | +167 | +116 | +82 | — | +77 | +183 | +28 | +31 | +19 |

**1045 (= 1040/1041/1042)**

| | Jan | Feb | Mrz | Apr | Mai | Jun | Jul | Aug | Sep | Okt | Nov | Dez |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Prototyp | 10 663 | 11 532 | 9 196 | 2 897 | 2 051 | 1 660 | 132 | 1 039 | 1 533 | 5 871 | 7 388 | 9 715 |
| heute | 10 384 | 11 162 | 8 397 | 2 173 | 1 599 | 1 382 | 93 | 832 | 1 203 | 5 514 | 6 975 | 9 639 |
| Abw % | +3 | +3 | +10 | +33 | +28 | +20 | +42 | +25 | +27 | +6 | +6 | +1 |

**1023/1024**: Proto 80 105 / 84 143 / 61 447 / 10 937 / 6 032 / 4 552 / 8 / 2 006 / 3 240 / 36 879 / 49 990 / 73 443; heute 66 633 / 68 051 / 45 146 / 7 160 / 4 513 / 3 722 / 0 / 1 475 / 2 029 / 28 496 / 38 871 / 63 701.
**1018**: Proto 11 915 / 9 131 / 8 011 / 2 557 / 1 860 / 1 382 / 194 / 239 / 1 991 / 5 309 / 8 692 / 11 052; heute 9 652 / 6 868 / 5 540 / 1 187 / 940 / 948 / 40 / 90 / 1 205 / 4 015 / 7 158 / 9 240.
**1039**: Proto 99 479 / 104 975 / 78 447 / 16 914 / 10 257 / 7 841 / 327 / 4 014 / 6 241 / 47 520 / 63 451 / 91 004; heute 86 580 / 89 630 / 61 718 / 11 563 / 7 709 / 6 468 / 185 / 3 174 / 4 490 / 39 395 / 52 593 / 82 116.
**1008**: Proto 17 767 / 18 946 / 14 530 / 4 963 / 3 496 / 2 696 / 443 / 1 693 / 2 535 / 8 905 / 11 675 / 16 185; heute 9 903 / 10 415 / 7 499 / 2 061 / 1 440 / 1 224 / 48 / 638 / 948 / 4 918 / 6 390 / 9 335.

**Muster:** Die Abweichung ist im Hochwinter klein (+1 bis +24 %) und in der **Übergangszeit groß (+20 bis +180 %)** — genau dort, wo die solaren und inneren Gewinne die Bilanz bestimmen. Vollständig in `vergleich_monate.csv`.

### Raumtemperatur und Überhitzung (alle Maximaleraumtemperatur = 24 °C)

| Geb | T_Luft Jahresmittel °C | **Stunden > 24 °C** | τ = C/H [h] |
|---|---|---|---|
| 10614 / 10577 / 10652 | 20,60 | 939 | 15,8 |
| 10576 | 21,06 | **1386** | **0,09** |
| 10599 | 20,51 | 794 | 24,8 |
| 10632 | 20,19 | 473 | 22,2 |
| 10628 / 10629 / 10644 | 21,12 | **1425** | 23,3 |
| 10642 | 19,73 | 184 | 18,8 |
| 10643 | 19,96 | 440 | 7,5 |
| 10645 / 46 / 47 / 51 | 20,06 | 495 | 9,6 |

184 bis 1425 Stunden über 24 °C (2–16 % des Jahres). Ohne Kühlung, ohne Fensterlüftung (n = 0,7 1/h konstant in allen 15 Zeilen) und ohne Sonnenschutz ist das zu erwarten; es ist zugleich das Signal, dass eine sommerliche Nutzerregel fehlt. Das Altmodell kappt hier einfach bei `maxRaumtemp` (BhkwPlan.cs:432) und wirft die Information weg.

---

## 4. Varianten

### θ_eq mit Absorption (θ_eq = θ_out + 0,6·I/25 − 3 K, Wand: Mittel der vier Fassaden, Dach: Globalstrahlung)

| Projekt | Basis kWh | Absorption kWh | **Δ zur Basis** | Δ zu heute |
|---|---|---|---|---|
| 1007/1046 | 62 566 | 66 989 | **+7,1 %** | +26,2 % |
| 1008 | 103 833 | 110 658 | +6,6 % | +101,9 % |
| 1017 | 83 751 | 86 422 | **+3,2 %** | +37,3 % |
| 1018 | 62 332 | 64 541 | +3,5 % | +37,7 % |
| 1023/1024 | 412 781 | 426 029 | +3,2 % | +29,2 % |
| 1039 | 530 468 | 551 864 | +4,0 % | +23,8 % |
| 1040–1045 | 63 677 | 68 217 | +7,1 % | +14,9 % |

**Die Variante erhöht den Bedarf um 3–7 %, also in die falsche Richtung.** Das liegt am pauschalen −3 K für die langwellige Abstrahlung: über das Jahr gemittelt ist 0,6·I/25 in Deutschland nur rund +1,4 K (bei ~200 W/m² Tagesmittel im Sommer, nahe 0 im Winter), −3 K wirkt aber Tag und Nacht, das ganze Jahr. **Für Stufe G1 ist die Variante so nicht brauchbar**: α und Δθ_er müssen getrennt und der Himmelsaustausch muss bewölkungsabhängig gerechnet werden (VDI 6007-1 Gl. für θ_eq mit ε·ΔE_r/h_a). Empfehlung: bei G1 **ohne** Absorption bleiben oder α = 0,5 mit Δθ_er = 0 ansetzen.

### Erdreich: Kusuda (Basis) gegen Altmodell-Faktor 0,45

| Projekt | Basis (Kusuda) | 0,45·U_G·A_G gegen θ_out | **Δ** | U_G·A_G-Anteil an ΣU·A_opak |
|---|---|---|---|---|
| 1007/1046 | 62 566 | 60 345 | **−3,6 %** | 13,2 % |
| 1008 | 103 833 | 97 761 | −5,8 % | 30,5 % (10576) |
| 1017 | 83 751 | 73 215 | **−12,6 %** | 45,8 % |
| 1018 | 62 332 | 53 520 | **−14,1 %** | 44,8 % |
| 1023/1024 | 412 781 | 382 016 | −7,5 % | 39,7 % |
| 1039 | 530 468 | 494 285 | −6,8 % | — |
| 1040–1045 | 63 677 | 62 216 | −2,3 % | 10,2 % |

Die beiden Ansätze unterscheiden sich um **2,3 bis 14,1 %**, streng nach dem Anteil der Grundfläche an der Hülle. Sie sind physikalisch **nicht** äquivalent: Kusuda schickt die volle Fläche gegen 3,5…16,3 °C, das Altmodell 45 % der Fläche gegen −18…+34 °C. Kusuda ist das deutlich bessere Modell (keine Tagesschwankung im Erdreich, kein Vorzeichenwechsel im Sommer), aber es fehlt die Angabe, **ob die Grundfläche gegen Erdreich oder gegen einen unbeheizten Keller** grenzt — 10614 („unbeheizter Keller" laut `Beschreibung`), 10599 („KD bzw. Grundfläche"), 10643 („KD: Gewölbe mit Schlacke") zeigen alle drei Fälle. Für G1 ist das ein Pflichtfeld (s. Abschnitt 8).

---

## 5. Empfindlichkeitsprobe — Projekt 1045, Gebäude 10651 (EFH-A-U-347s, 201 m², skal 1, heute 59 354 kWh)

Datei `empfindlichkeit_1045.csv`:

| Variante | Jahr kWh | **Δ Basis** | Δ heute | max kW | Std > 24 °C | τ h |
|---|---|---|---|---|---|---|
| **Basis** C1 0,3/0,7, h_ms 9,1, F_S 0,9 | 63 677 | 0,00 % | +7,3 % | 33,56 | 495 | 9,6 |
| C1 **0,2/0,8** | 63 631 | **−0,07 %** | +7,2 % | 33,39 | 491 | 9,6 |
| C1 **0,5/0,5** | 63 744 | **+0,10 %** | +7,4 % | 33,71 | 501 | 9,6 |
| **Massenknoten Wandmitte** (R1 = RRest = ½·ΣR) | 63 715 | **+0,06 %** | +7,3 % | 33,24 | 509 | 9,6 |
| **F_S 0,7** statt 0,9 | 65 375 | **+2,67 %** | +10,1 % | 33,58 | 390 | 9,6 |
| Erdreich 0,45 | 62 216 | −2,30 % | +4,8 % | 33,85 | 574 | 9,9 |
| ohne F_F/F_S (Solar roh wie Altmodell) | 59 584 | **−6,43 %** | **+0,4 %** | 33,51 | 856 | 9,6 |
| Altmodell-Bauteilgewichte (0,83/0,95/0,45/0,83, Lüftung 1/3) | 55 419 | **−12,97 %** | −6,6 % | 31,03 | 630 | 11,2 |
| Altmodell-Randbedingungen (beides) | 51 421 | **−19,25 %** | **−13,4 %** | 30,97 | 1 082 | 11,2 |
| θ_eq mit Absorption | 68 217 | +7,13 % | +14,9 % | 34,91 | 698 | 9,6 |

**Die drei RC-Strukturparameter, über die man am meisten streitet, sind für die Jahresenergie irrelevant** (≤ 0,1 %) und für die Spitzenlast fast irrelevant (≤ 1,0 %). Bei idealer, unbegrenzter Regelung auf einen Sollwert bestimmt die Kapazitätsaufteilung nur die *Verteilung* innerhalb des Tages, nicht die Bilanz. Sie wird erst wichtig, sobald (a) eine Leistungsbegrenzung, (b) eine Speicherhysterese oder (c) eine Kühlgrenze dazukommt — genau das, was EPOS in `SimulationWaermebedarf` nachgelagert tut. **F_S dagegen schlägt mit 2,7 % je 0,2 durch**, F_F·F_S zusammen mit 6,4 %.

---

## 6. Deutung: was ist Modellunterschied, was ist Datenlücke

Die Zerlegung ist sauber möglich, weil die beiden Diagnosevarianten den Prototyp schrittweise auf die Randbedingungen des Altmodells zurückdrehen. Δ in Prozentpunkten gegen „heute":

| Projekt | **gesamt** | davon: volle U·A-Gewichte statt 0,83/0,95/0,45 | davon: F_F·F_S = 0,63 statt roh | **Rest = reine Modellstruktur** |
|---|---|---|---|---|
| 1007/1046 | +17,9 | **+16,1** | **+10,9** | **−9,1** |
| 1017 | +33,0 | +24,1 | +14,0 | −5,1 |
| 1018 | +33,0 | +25,6 | +16,0 | −8,6 |
| 1023/1024 | +25,2 | +17,2 | +15,6 | −7,6 |
| 1039 | +19,0 | +16,3 | +12,9 | −10,2 |
| 1040–1045 | +7,3 | +13,9 | +6,8 | **−13,4** |
| 1008 | +89,4 | +24,7 | +10,1 | **+54,6** (Sonderfall, s. u.) |

### (a) Was **Parametrierung** ist, nicht Modell — und den Löwenanteil erklärt

**Die fest verdrahteten Gewichte 0,83 / 0,95 / 0,45 / 0,83 in `SpezWaermeverlusteC` (BhkwPlan.cs:342-362) senken den Wärmeverlustkoeffizienten um 14–26 %:**

| Geb | L_Prototyp W/K | L_Altmodell W/K | Δ |
|---|---|---|---|
| 10614 | 234,1 | 203,7 | **+14,9 %** |
| 10576 | 540,5 | 466,4 | +15,9 % |
| 10599 | 1 498,8 | 1 233,9 | **+21,5 %** |
| 10632 | 4 440,3 | 3 635,7 | **+22,1 %** |
| 10628 | 7 713,4 | 6 686,9 | +15,4 % |
| 10642 | 693,3 | 548,4 | **+26,4 %** |
| 10643 / 10645 | 1 348,3 / 1 051,2 | 1 146,1 / 899,5 | +17,6 % / +16,9 % |

Diese Gewichte sind **keine Physik, sondern eine Kalibrierung** (bzw. die Nachbildung einer alten DLL). Wer ein VDI-6007-Modell aufsetzt, muss sie fallenlassen — und bekommt dann zwangsläufig einen höheren Bedarf. Der Prototyp liegt damit näher an der Katalogkennzahl `spez_Waermeverbrauch` (Abschnitt 3), was dafür spricht, dass die **ungewichteten** U·A-Werte richtiger sind und die 0,83/0,95/0,45 einen nicht dokumentierten Korrekturbedarf des Altmodells verdecken.

**Die zweite große Größe ist das Fenster:** `Fensterdurchlassgrad` wird im Altmodell **roh** benutzt (BhkwPlan.cs:316), der Prototyp multipliziert F_F = 0,7 und F_S = 0,9 dazu. Jahressolargewinn:

| Geb | Proto (mit 0,63) | Altmodell (roh) | Differenz |
|---|---|---|---|
| 10614 | 25 552 kWh | 40 558 kWh | −15 006 kWh |
| 10599 | 39 576 | 62 819 | −23 243 |
| 10628 | 270 251 | 428 970 | −158 719 |
| 10632 | 29 175 | 46 309 | −17 134 |
| 10645 | 14 660 | 23 270 | −8 610 |

Gegen die inneren Gewinne (8 452 / 13 483 / 66 226 / 9 903 / 4 047 kWh) ist das ein **sehr großer Posten** — beim 10628 sind die Solargewinne das **6,5-fache** der inneren Gewinne. Das ist zugleich das stärkste Argument für ein belastbares Verschattungs-/Rahmenmodell: F_F und F_S entscheiden hier über zweistellige Prozentsätze.

### (b) Was echter **Modellunterschied** ist — und in dieselbe Richtung zeigt (−5 bis −13 %)

Bei identischen Bauteilgewichten und identischem rohen g-Wert liefert der Prototyp **5 bis 13 % WENIGER** als das Altmodell. Vier Ursachen, alle im Befund „Daten" benannt:

1. **Solar nur 9–14 Uhr mit Faktor 4** (BhkwPlan.cs:420, 431). Die *Tagesenergie* bleibt erhalten (6 h × 4 = 24 h), aber als Rechteck. Ein Rechteck aus dem Tagesmittel ist **schmaler und höher** als der wahre Tagesgang: die Mittagsstunden bekommen mehr, als sie nutzen können, dort greift `pHzg < 0 → 0` und die Übermenge geht verloren; die Rand- und Nachmittagsstunden bekommen gar nichts und müssen geheizt werden. Das **erhöht** den Altmodell-Bedarf. Der Prototyp nutzt `Tab_Solar` stündlich, verwertet die Randstunden mit und kommt deshalb niedriger heraus.
2. **Zwei Kapazitäten statt einer.** Der Prototyp speichert den Solareintrag in der Innenbauteilmasse (C1,IW = 0,7·C_ges an einem Knoten mit R1,IW = 1/(2,5·A_floor·9,1)) und gibt ihn Stunden später wieder ab. Das Altmodell hat einen einzigen Knoten, an dem Sollwert, Kappung und Kapazität zusammenfallen.
3. **Kappung bei `Maximaleraumtemperatur`** (BhkwPlan.cs:432). Das Altmodell schneidet die Raumtemperatur hart auf 24 °C ab und startet die nächste Stunde von dort — die überschüssige Energie ist weg und steht am Abend nicht mehr zur Verfügung. Der Prototyp lässt frei laufen (184–1425 Stunden über 24 °C) und trägt die Wärme mit.
4. **Tagesprofil-Verteilung.** Reine Formsache für die Jahresenergie, entscheidend für die Spitze (Abschnitt 3): der Prototyp erzeugt eine reale Aufheizspitze um 6–7 h, das Altmodell einen glatten Zwei-Buckel. **Der Spitzenlastvergleich in der heutigen Form vergleicht Physik mit einer Verteilungstabelle.**

### (c) Was **Datenlücke** ist

- **Fenster Ost/West nicht getrennt.** `Fensterflaeche_Ost_West` ist ein Summenwert; der Prototyp musste ihn hälftig aufteilen. `Sol_Ost` und `Sol_West` liegen stündlich getrennt vor (Jahresmittel 88,68 gegen 81,14 W/m², 9 % Unterschied). Die Jahresenergie ändert sich dadurch kaum (die Halbierung ist energetisch fast neutral), **aber die Morgen- und Abendspitze ist nicht darstellbar** — und genau die ist das Argument für eine Stundensimulation. Ohne dieses Feld ist die Spitzenlastaussage eines 7R2C nur halb so viel wert.
- **Keine Innenbauteilflächen.** A_IW = 2,5·A_floor ist geraten. Die Empfindlichkeitsprobe zeigt zwar, dass die Aufteilung C1,AW/C1,IW bei idealer Regelung fast folgenlos ist (≤ 0,1 %) — aber sobald Leistungsgrenzen und Speicher dazukommen, ist das nicht mehr so.
- **Keine Verschattung (F_S), kein Rahmenanteil (F_F), keine Einfallswinkelkorrektur (F_W).** Der zweitgrößte Einzelposten (s. oben), vollständig geraten.
- **`Bauweise` ist eine Dreipunktwahl 20/50/100 Wh/(m²K)** und in einem Fall der nackte Rückfallwert (s. Abschnitt 7).
- **Kein Absorptionsgrad α, kein Himmelsaustausch** — die Absorptionsvariante scheitert genau daran (Abschnitt 4).
- **Keine Erdreich-/Kellerrandbedingung** — 2,3 bis 14,1 % Unterschied zwischen zwei gleich gut begründbaren Annahmen.
- **Luftwechsel pauschal 0,7 1/h in allen 15 Zeilen**, keine Trennung Infiltration/Lüftung, keine Fensterlüftung — deswegen die 184–1425 Überhitzungsstunden.
- **Nutzungsprofil leer.** WE- und Ferienabsenkung sind in allen 15 Zeilen inaktiv; ein Nutzungsprofil ist an diesen Daten nicht validierbar.

---

## 7. Gegenprobe des Altmodells und der Sonderfall 1008

`Altmodell.cs` rechnet `TaeglHeizlastWG` nach — inklusive Vorlauftage 350…365, Rücksetzen von `_prevRoomTemp` bei `day == 1` (BhkwPlan.cs:405) und der Lüftungs-Temperaturkorrektur f = (θ_a·0,025 + 1) für θ_a < 0. **Alle 15 Gebäudezeilen werden auf ≤ 0,001 % getroffen** (z. B. 10614: 53 072 gegen 53 072; 10642: 36 450 gegen 36 451 = −0,001 %). Damit ist die Leseart des Altmodells und die Behandlung der Tagesmittelklimadaten in diesem Lauf unabhängig bestätigt.

**Projekt 1008, Gebäude 10576, ist der Beweis dafür, dass das heutige Modell bei kleiner Kapazität zusammenbricht.** Es trägt den Rückfallwert `Bauweise = 50 Wh/K` bei 304 m² (`Gebaeudebauweise.cs:69`), also 0,16 statt 50 Wh/(m²K):

| | Jahr kWh | kWh/m²a | τ |
|---|---|---|---|
| Altmodell, C = 50 Wh/K (Ist-Daten) | 43 267 | **54,1** | — |
| Altmodell, C = 15 200 Wh/K (korrigiert) | 65 773 | **82,2** | — |
| Prototyp, C = 50 Wh/K | 90 216 | **112,8** | 0,09 h |
| Prototyp, C = 15 200 Wh/K | 83 374 | **104,2** | 28,1 h |
| **Katalogkennzahl `spez_Waermeverbrauch`** | — | **112** | — |

Das Altmodell reagiert auf die Korrektur mit **+52 %**, der Prototyp mit **−7,6 %**. Grund: bei C → 0 wird in `TaeglHeizlastWG` der Term `(tSoll − tPrev)·C` zu Null und gleichzeitig `a = 1 − exp(−L/C) → 1`; die Temperaturfortschreibung wird zu `tPrev_neu = tPrev` — das Modell **verliert den Sollwertbezug vollständig** und heizt gegen seine eigene Vorstunde, die nach dem Reset bei `Raumsolltemperatur_Nachtabsenkung` = 18 °C hängen bleibt. Der Prototyp ist gegen kleine Kapazitäten robust (der Luftknoten ist algebraisch, die Regelung greift am Luftknoten an) und trifft die Katalogkennzahl 112 kWh/m²a praktisch exakt. **Der Ausreißer +89,4 % in Projekt 1008 ist also kein Prototypfehler, sondern ein Datenfehler, den das Altmodell in die falsche Richtung verstärkt.**

---

## 8. Was für eine Stufe G1 hinzukommen muss — und welche Standardwerte

**Pflicht (ohne diese Felder ist G1 nicht seriös):**

| # | Feld | Warum | **empfohlener Standardwert** |
|---|---|---|---|
| 1 | `Fensterflaeche_Ost` / `_West` getrennt | Morgen-/Abendspitze; der einzige Grund, überhaupt stündlich zu rechnen. Sol_Ost/Sol_West liegen getrennt vor. | Migration bestehender Sätze: je 50 % von `Fensterflaeche_Ost_West`, Flag „geschätzt" |
| 2 | `F_S` Verschattung je Orientierung | 2,7 % Jahresenergie je 0,2; größter geratener Hebel | **0,9** frei stehend, **0,8** Reihe/Zeile, **0,7** Innenstadt |
| 3 | `F_F` Rahmenanteil | zusammen mit F_S 6,4 % | **0,7** (Bestand), 0,75 (nach 1995) |
| 4 | `Bauweise` als geprüfter Wert + Plausibilitätsgrenze | Rückfallwert 50 Wh/K zerstört das Ergebnis (10576) | Grenze **5 ≤ Bauweise/Wohnflaeche ≤ 200 Wh/(m²K)**; sonst harter Fehler, nicht stiller Rückfall. Vorbesetzung leicht/schwer/sehr schwer = **20 / 50 / 100 Wh/(m²K)** wie heute |
| 5 | Randbedingung der Grundfläche (Erdreich / unbeheizter Keller / Außenluft) | 2,3–14,1 % Unterschied | **Erdreich**, Kusuda z = 1 m, α = 0,06 m²/d, aus Jahresmittel und **harmonischer** Jahresamplitude der Aussenluft; unbeheizter Keller: Reduktionsfaktor **0,5** gegen θ_out |
| 6 | Innenbauteilfläche A_IW | trägt C1,IW und den Strahlungsaustausch | **2,5 · Wohnflaeche** (bestätigt sich als unkritisch, ≤ 0,1 %) |
| 7 | Aufteilung C1,AW / C1,IW | VDI 6007 braucht beide | **0,3 / 0,7** (die Probe zeigt: 0,2/0,8 bis 0,5/0,5 ändern ≤ 0,1 %, also kein Streitpunkt) |
| 8 | h_ms, h_conv, h_rad | Netzstruktur | **9,1 / 2,7 / 5,0 W/(m²K)** (DIN EN ISO 13790); Massenknoten in Wandmitte als Alternative ändert 0,06 % — nicht relevant |
| 9 | Ausdrückliche Entscheidung zu den Gewichten 0,83/0,95/0,45/0,83 | erklärt 14–26 % | **Streichen.** Sie sind eine verdeckte Kalibrierung; der ungewichtete Ansatz trifft `spez_Waermeverbrauch` besser |
| 10 | Lüftung: Infiltration und Nutzerlüftung getrennt, Sommerlüftung | 184–1425 h über 24 °C | Infiltration **n₅₀-abhängig, ersatzweise 0,3 1/h**; Nutzerlüftung **0,4 1/h**; Sommernachtlüftung **auf 2,0 1/h erhöhen, wenn θ_L > 23 °C und θ_a < θ_L − 2 K** |

**Nachrangig, aber vor einer Freigabe zu klären:** α (Absorptionsgrad) und Δθ_er (langwellige Abstrahlung) getrennt statt der Pauschale „0,6·I/25 − 3 K", die in allen Projekten in die falsche Richtung zeigt; Fensterneigung (heute fest 90°, `KlimaImportAblauf.cs:133`) für Dachflächenfenster; Einfallswinkelkorrektur F_W; anisotropes Hay-Davies-Transpositionsmodell statt isotrop (`CalculateHourlyHayDavies` liegt in `SolarPVGISCalculator.cs:421` bereit) — Sol_Nord = 431,6 kWh/(m²a) ist für Stuttgart zu hoch.

**Und drei Dinge, die keine Felder sind, sondern Festlegungen:**
- **Eine Leistungsbegrenzung der Heizung.** Ohne sie fällt die Jahresspitze in 10 von 12 Projekten auf dieselbe Stunde 1399 (erste Stunde nach der Nachtabsenkung) und ist ein Aufheiz-, kein Auslegungswert. Vorschlag: Spitze als **gleitendes Tagesmittel** oder als **95-%-Quantil der Stundenwerte** berichten, oder eine Aufheizleistungsgrenze (z. B. 1,5 × Auslegungsheizlast) einführen.
- **Der Skalierungsfaktor.** Bei 1007/1046 wird eine 74-m²-Hülle mit 114 m² Außenwand auf 340 m² Wohnfläche gedehnt (Faktor 4,59), bei 1018 auf 0,25 gestaucht. Für ein 1R1C ist das ein Multiplikator; für 7R2C, wo Flächen die Kapazität und den Strahlungsaustausch tragen, ist es physikalisch falsch. In diesem Lauf wurde er vorschriftsgemäß nur auf das Ergebnis angewandt — für G1 muss entschieden werden: **echte Hülle erfassen oder Geometrie mitskalieren.**
- **Eine Zeitbasis.** Heute laufen Stundenreihe (ortszeitkorrigiert, `SolardatenCtrl.cs:156-208`) und Tageskalender (roh UTC, `KlimadatenCtrl.cs:37`) im selben Lauf auseinander. Ein 7R2C braucht nur noch die Stundenreihe; die Tagesmittel entfallen. **Das ist eine Vereinfachung, kein Mehraufwand.**

---

## 9. Fazit für das Konzeptpapier

**Ja — ein VDI-6007-Modell lässt sich aus den vorhandenen Feldern von `Tab_Gebaeude` fahren und liefert plausible Jahresheizwärme.** Der Prototyp läuft für alle 12 Projekte mit Gebäudedaten in 2,35 s durch, erreicht 86–101 % der Katalogkennzahl `spez_Waermeverbrauch` (das heutige Modell 48–88 %) und trifft die Tagessummen des Altmodells mit r = 0,98…0,997.

**Die Abweichung von +7 bis +33 % ist zu etwa zwei Dritteln Parametrierung, nicht Modell.** Fallen die verdeckten Kalibrierfaktoren 0,83/0,95/0,45/0,83 weg (+14…+26 %) und kommen Rahmen- und Verschattungsfaktoren hinzu (+7…+16 %), bleibt als reiner Struktureffekt des 7R2C ein **Minus von 5 bis 13 %** — der Prototyp ist bei gleichen Randbedingungen *sparsamer*, weil er den Solareintrag stündlich statt als 6-Stunden-Rechteck verwertet und die Wärme in zwei Kapazitäten über die Kappung bei 24 °C hinweg mitträgt.

**Die Spitzenlast ist mit den heutigen Daten nicht belastbar**, aus zwei unabhängigen Gründen: die fehlende Ost/West-Trennung der Fenster und die fehlende Leistungsbegrenzung. Als Tagesmittel gerechnet liegt der Prototyp nur +18 bis +19 % über dem Altmodell, als Stundenwert +43 %.

**Der wichtigste Einzelbefund ist Gebäude 10576:** ein einzelner falscher Kapazitätswert (`Bauweise = 50 Wh/K`) drückt das heutige Modell um 34 % nach unten, ohne dass irgendetwas warnt. Der Prototyp ist gegen denselben Fehler weitgehend unempfindlich. Eine Plausibilitätsgrenze auf `Bauweise` ist die billigste Verbesserung im ganzen Paket.

**Einschränkung, die im Papier stehen muss:** Die Referenz stammt selbst aus dem 1R1C-Modell. Übereinstimmung wäre kein Gütebeweis. Der Gütebeweis des Lösers liegt in den 12 bestandenen VDI-6007-1-Testfällen (Befund „Prototyp 1"); dieser Lauf zeigt nur, **wo und warum** die beiden Modelle auseinanderlaufen. Belastbar validieren ließe sich EPOS erst an gemessenen Verbräuchen — die in diesen 13 Projekten nicht vorkommen (alle 15 Zeilen benutzen den Flächenweg, der Verbrauchsweg `Bewohner_und_Flaeche_berechnen` wird nie betreten).