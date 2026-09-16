# Gegenlesen des Konzepts — Blickwinkel Physik und Norm (15.09.2026)

**Protokoll.** Einzelbefund des Workflows ‚konzept-gegenlesen‘ (Modell Opus) im Auftrag des Konzepts [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Wortlaut wie geliefert; Zeilenbelege und Zahlen sind im Konzept nach dem Gegenlesen korrigiert, bei Widerspruch gilt das Konzept. Pfade unter `C:\Users\…pos-spike\` bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

## Befunde – Blickwinkel „Physik und Norm" (Kap. 0, 4, 5)

**1. Testfall 11 ist in den Rohdaten durchgefallen — Schwere: hoch**
(a) 5.2: „| 11 Kühldecke (Senke am IW-Oberflächenknoten) | Φ | 1,376 W | ja¹ |"
(b) Die mitgelieferte Messdatei weist für Testfall 11 eine maximale Abweichung von 4,9183 W bei Toleranz 1,5 W und das Urteil „nein" aus; die 1,376 W stammen aus einem zweiten Lauf, in dem der Löser das 120-s-Messfenster der AixLib-Referenz nachbildet — geändert wurde also das Abnahmeverfahren, nicht der Löser.
(c) `C:\Users\Dirk\AppData\Local\Temp\epos-spike\Prototyp\out\validierung.csv`, Zeile „11;Q;W;1.5;4.9183;…;nein;85.0"; Gegenstück `…\out\lauf.log:164` („max |Abw| = 1.376 W -> BESTANDEN").
(e) Ersatz: „| 11 Kühldecke (Senke am IW-Oberflächenknoten) | Φ | 4,92 W roh / 1,376 W mit Nachbildung des 120-s-Messfensters der Referenz | **nein / ja** |" — und im Fließtext: „Testfall 11 besteht nur, wenn das Messfenster der Referenz nachgebildet wird; roh liegt er mit 4,92 W über der Schwelle. Er ist damit **nicht** als bestanden zu zählen, solange die Nachbildung nicht Teil der Prüfvorschrift ist."

**2. „besteht alle zwölf Normtestfälle" gilt nur gegen die gelockerte AixLib-Schwelle — Schwere: hoch**
(a) 0.2: „**Der neu geschriebene Löser besteht alle zwölf Normtestfälle** (Abweichung 0,05–0,14 K bzw. 0,6–1,5 W; Prüfschwelle 0,15 K / 1,5 W wie in den AixLib-Validierungen, die Norm nennt 0,1 K / 1 W)."
(b) An der im selben Satz genannten Normschwelle 0,1 K / 1 W fallen vier der zwölf Fälle durch: TF 6 mit 1,4986 W, TF 9 mit 0,1363 K, TF 10 mit 0,1430 K, TF 11 mit 4,9183 W — der Satz behauptet das Bestehen der *Norm*testfälle und belegt es mit einer um 50 % gelockerten Schwelle.
(c) `…\out\validierung.csv` (Spalten `toleranz`/`max_abw`, alle zwölf Zeilen); Papier 5.2.
(e) Ersatz: „**Der neu geschriebene Löser besteht alle zwölf Normtestfälle an der AixLib-Prüfschwelle 0,15 K / 1,5 W** (Abweichung 0,05–0,14 K bzw. 0,6–1,5 W). An der Normschwelle 0,1 K / 1 W bleiben vier Fälle offen (6, 9, 10, 11); ob die Rundung der Referenztabellen sie erklärt, ist in G0 vor der Übernahme zu klären."

**3. „Falsche Richtung" der Absorptionspauschale ist durch die eigenen Katalogkennzahlen widerlegt — Schwere: hoch**
(a) 4.4: „Die Pauschale „+0,6·I/25 − 3 K" ist ausdrücklich **nicht** zulässig: sie erhöht den Bedarf in allen Projekten um 3–7 % (5.8), weil −3 K Tag und Nacht wirkt."
(b) Die Ablehnung stützt sich allein auf das Vorzeichen; gemessen an der einzigen unabhängigen Referenz des Papiers (`spez_Waermeverbrauch`, 5.5) verbessert die Variante die Trefferquote in sechs von sieben Katalogbauten, weil der Prototyp durchweg **unter** der Katalogkennzahl liegt.
(c) `…\out\vergleich_zusammenfassung.csv`, Spalte `abs_kWh`: 10614 66 989,2/340 m² = 197,0 gegen Katalog 212 (86,8 % → 92,9 %); 10643 84 094,2/201 = 418,4 gegen 451 (85,7 % → 92,8 %); 10599 98,4 %; 10632 94,9 %; 10628 91,1 %; 10642 95,0 %; nur 10576 (die fehlerhafte Zeile) überschießt auf 106,8 %.
(e) Ersatz: „Die Pauschale „+0,6·I/25 − 3 K" ist keine Physik und wird nicht übernommen (α und ΔE_r getrennt, Himmelsaustausch bewölkungsabhängig, 4.4). Ihre Wirkung von +3,2 bis +7,1 % (5.8) zeigt zugleich, in welcher Größenordnung der in G1 abgeschaltete Strahlungsterm fehlt: mit ihm träfe der Prototyp die Katalogkennzahl zu 91–99 % statt zu 86–100 %. Der Schalter `Aussenbauteile_Strahlung` ist deshalb in G1 vorzusehen und in G2 gegen Messwerte zu prüfen."

**4. Kap. 0 unterschlägt das Projekt, in dem der Prototyp unter dem Tagesmodell liegt — Schwere: hoch**
(a) 0.4: „Er liegt 7–33 % über dem Tagesmodell; zwei Drittel davon sind versteckte Kalibrierfaktoren 0,83/0,95/0,45 im Bestand und der rohe g-Wert"
(b) Tabelle 5.5 weist für Gebäude 10643 **−2,1 %** aus; die Spanne ist damit −2 bis +33 %, nicht 7–33 %.
(c) Papier 5.5, Zeile „1039 | 10643 | 201 | 77 705 | 79 373 | −2,1 %"; `…\out\vergleich_zusammenfassung.csv`: 77704.5 gegen 79373 → −2,10 %.
(e) Ersatz: „Er liegt zwischen −2 und +33 % gegenüber dem Tagesmodell (10576 ist der Datenfehler, 5.11); das einzige Gebäude mit negativem Vorzeichen ist der Extremfall 10643 (U_AW 2,90)."

**5. „zwei Drittel davon" ist arithmetisch unmöglich — Schwere: hoch**
(a) 0.4: „zwei Drittel davon sind versteckte Kalibrierfaktoren 0,83/0,95/0,45 im Bestand und der rohe g-Wert, der reine Struktureffekt beträgt −5 bis −13 %."
(b) Nach der Zerlegung 5.10 tragen die Parametrierungsanteile +20,7 bis +41,6 Prozentpunkte bei Gesamtabweichungen von +7,3 bis +33,0 % — also in **jedem** Projekt mehr als 100 % (1040–1045: 13,9 + 6,8 = 20,7 gegen 7,3 → 284 %); der Struktureffekt zieht sie wieder herunter. „Zwei Drittel" kehrt das Verhältnis um.
(c) Papier 5.10, alle sechs Zeilen; Gegenprobe `…\out\empfindlichkeit_1045.csv`: „Altmodell-Randbedingungen;51421.1;-19.25;-13.37".
(e) Ersatz: „Die versteckten Kalibrierfaktoren 0,83/0,95/0,45/0,83 und der rohe g-Wert erklären die Abweichung vollständig und darüber hinaus (+21 bis +42 Prozentpunkte); die Modellstruktur wirkt gegenläufig und senkt den Bedarf bei gleichen Randbedingungen um 5 bis 13 %."

**6. „43 % über der Spitze … als Tagesmittel nur 18 %" — zwei verschiedene Bezugsgrößen in einem Satz — Schwere: hoch**
(a) 4.5: „sie liegt 43 % über der Spitze des Tagesmodells, als Tagesmittel nur 18 % (5.6)."; gleichlautend 5.6: „Als Tagesmittel schrumpft der Unterschied von +43 % auf +18 %."
(b) Aus Tabelle 5.6 selbst folgt für die Stundenspitze eine Spanne von **−10,0 %** (1040–1045) bis **+57,0 %** (1017), über alle zwölf Projekte aufsummiert +28,8 %; +43 % trifft allein Projekt 1023/1024 (276,60/193,96 = +42,6 %). Die +18 % dagegen sind der Summenwert über alle zwölf Projekte (797,2/677,3 = +17,7 %). Der Vergleich stellt einen Einzelwert einem Gesamtwert gegenüber.
(c) Papier 5.6 (alle sieben Zeilen); `…\out\vergleich_zusammenfassung.csv` (`proto_max_kW`) gegen Papier 3.7 (`Max kW`).
(e) Ersatz: „sie liegt je nach Projekt −10 bis +57 % neben der Spitze des Tagesmodells (über alle zwölf Projekte +29 %); als gleitendes Tagesmittel schrumpft der Abstand auf +18 % (5.6)."

**7. „10 von 12" Projekten mit gleicher Spitzenstunde — es sind 11 — Schwere: mittel**
(a) 4.5: „ohne Leistungsgrenze fällt die Jahresspitze in 10 von 12 Referenzprojekten auf dieselbe Stunde"; ebenso 5.6: „In zehn von zwölf Projekten liegt die Prototyp-Spitze auf demselben Index".
(b) Nur Projekt 1018 weicht ab (Index 438, 19. Jan 6 h); 1007, 1008, 1017, 1023, 1024, 1039, 1040, 1041, 1042, 1045, 1046 = elf Projekte liegen auf Index 1398.
(c) `…\out\vergleich_zusammenfassung.csv`, Spalte `proto_max_std` (1399 in 14 von 15 Zeilen, 439 nur bei 10632); `…\out\lauf_vergleich.txt:72` „Std 19. (19. Jan, 6 h)".
(e) Ersatz: „fällt die Jahresspitze in **elf von zwölf** Referenzprojekten auf dieselbe Stunde (Index 1398 = 28. Feb, 6 h); nur Projekt 1018 (München) weicht ab (19. Jan, 6 h)."

**8. Verlustkoeffizient: Bezugsfehler bei „14,9–26,4 %" — Schwere: mittel**
(a) 5.10: „Die Gewichte senken den Verlustkoeffizienten der sieben Katalogbauten um 14,9–26,4 % (10614: 234,1 gegen 203,7 W/K; 10632: 4 440 gegen 3 636 W/K)."
(b) 203,7/234,1 ist eine Senkung um **13,0 %**, 3 636/4 440 um 18,1 %. Die 14,9 % bzw. 26,4 % sind die Werte des Protokolls für L_ungewichtet/L_gewichtet − 1, also die Erhöhung mit dem umgekehrten Bezug; als Senkung gelesen ergibt die Spanne 13,0–20,9 %.
(c) `…\out\lauf_vergleich.txt`, Zeilen „L_proto 234.1 W/K vs L_alt 203.7 W/K ( 14.9 %)" und „L_proto 693.3 … L_alt 548.4 ( 26.4 %)"; Summenprobe aus `vergleich_zusammenfassung.csv` (`UA_opak`+`UA_masselos`+`H_ve` = 234,07 bzw. 4 440,3 W/K).
(e) Ersatz: „Der ungewichtete Ansatz liegt bei den sieben Katalogbauten 14,9–26,4 % über dem gewichteten (10614: 234,1 gegen 203,7 W/K; 10632: 4 440 gegen 3 636 W/K); umgekehrt gelesen senken die Gewichte den Verlustkoeffizienten um 13,0–20,9 %."

**9. „Ost 6,8–8,5 % über West" widerspricht den eigenen Klimazahlen — Schwere: mittel**
(a) 5.12: „(Ost liegt in der Jahressumme 6,8–8,5 % über West, Tagesmaximum Ost bei Index 8, West bei Index 14)."
(b) Aus den Klimadaten ergibt sich Stuttgart +9,30 % und München +7,29 %; 3.5 nennt für Stuttgart selbst „88,7 gegen 81,1 W/m²" = +9,4 %. Die angegebene Spanne trifft keinen der beiden Orte.
(c) `C:\Users\Dirk\AppData\Local\Temp\epos-spike\daten\probe_log.txt`: `SOLSTAT 1007001 Sol_Ost sum=776870.3` gegen `Sol_West sum=710762.1`; `SOLSTAT 1018047 Sol_Ost sum=761404.4` gegen `Sol_West sum=709677.1`.
(e) Ersatz: „(Ost liegt in der Jahressumme 7,3 % (München) bis 9,3 % (Stuttgart) über West, Tagesmaximum Ost bei Index 8, West bei Index 14)."

**10. Tabelle 5.12 mischt drei Kennzahlen unter einer Spaltenüberschrift — Schwere: mittel**
(a) 5.12, Spaltenkopf: „| Größe | Spanne 100 % Ost gegen 100 % West | 50/50 liegt |" mit „| Stundenspitze | ≤ 0,24 %; … | — |"
(b) Die Zeile Jahresheizwärme nennt die volle Spanne (1023: 2,46 %), die Zeilen Stundenspitze und Tagesmittel-Maximum die **halbe** Spanne gegen 50/50 (1023 tatsächlich 0,48 % bzw. 1,00 %), die letzten drei Zeilen vorzeichenbehaftete Abweichungen. Unter einer Überschrift sind drei Rechenweisen vermischt.
(c) `…\out\ostwest_projekte.csv`, Projekt 1023: `max_kW` 277,261 (Ost) gegen 275,942 (West) bei Basis 276,601 → volle Spanne 0,477 %; `tagesmittel_max_kW` 172,191 gegen 170,476 bei 171,333 → 1,00 %.
(e) Ersatz: „| Stundenspitze | ≤ 0,48 % (je Extremfall ≤ 0,24 % gegen 50/50); Speicherindex in allen Varianten gleich | — | / | Tagesmittel-Maximum | ≤ 1,0 % (je Extremfall ≤ 0,5 %) | — |" — und Spaltenkopf: „Spanne 100 % Ost gegen 100 % West, bezogen auf den 50/50-Fall".

**11. Die 18,8 K Amplitude sind aus Kapitel 3 nicht nachrechenbar — Schwere: mittel**
(a) 4.4: „Kusuda-Temperatur in 1 m Tiefe aus Jahresmittel und **erster Harmonischer** der Außenluft (nicht (max−min)/2: das ergäbe 18,8 K Amplitude und −3 °C Erdreich);"
(b) Der Wert 18,75 K ist die Rohamplitude der **Tagesmittel**; aus den in 3.5 genannten Stundenextremen −18,2 … 33,5 °C liefert (max−min)/2 25,85 K und −7,8 °C Erdreich. Der Leser kann die Gegenrechnung nicht reproduzieren.
(c) `…\out\lauf_vergleich.txt`: „1007001: T_mittel 9.875 C, Amplitude(Harmonische) 9.315 K (roh max-min/2 18.75 K) … T_Erd min 3.50 C / max 16.25 C"; Papier 3.5 („Temperatur −18,2 … 33,5 °C, Mittel 9,88 °C").
(e) Ersatz: „… (nicht (max−min)/2 der **Tagesmittel**: das ergäbe 18,8 K Amplitude und −3,0 °C Erdreich; aus den Stundenextremen sogar 25,9 K und −7,8 °C). Harmonische Amplitude Stuttgart 9,32 K, Dämpfung 0,685, Erdreich 3,5 … 16,3 °C."

**12. „Zeitkonstanten … 1–2 Tage" widerspricht der zitierten Stelle — Schwere: mittel**
(a) 4.6: „Zeitkonstanten schwerer Bauweisen liegen bei 1–2 Tagen (5.7), der Vorlauf ist ausreichend und deterministisch."
(b) 5.7 und die Rohdaten nennen 7,5–24,8 h; der größte Wert entspricht 1,03 Tagen, „1–2 Tage" kommt nicht vor.
(c) Papier 5.7 („Zeitkonstanten C/H 7,5–24,8 h"); `…\out\vergleich_zusammenfassung.csv`, Spalte `tau_h` (Maximum 24,8 bei 10599; 28,12 h nur im korrigierten Sonderfall 10576, 5.11).
(e) Ersatz: „Die Zeitkonstanten der Referenzgebäude liegen bei 7,5–24,8 h (korrigiertes 10576: 28,1 h); 30 Tage Vorlauf sind rund das Fünfundzwanzigfache und damit reichlich bemessen."

**13. Spezifikation und validierter Prototyp verteilen den Fenstersolareintrag unterschiedlich — Schwere: mittel**
(a) 4.4: „Eintrag zu 100 % radiativ auf die Oberflächen."
(b) 5.3 hält für dieselbe Größe fest: „9 % des Fenstersolareintrags konvektiv an die Luft" — so wurde gegen die zwölf Normtestfälle validiert. Die Vorschrift für den Kern (4.4) weicht also von dem ab, was den Gütebeweis getragen hat, ohne dass die Abweichung benannt wird.
(c) Papier 4.4 gegen 5.3; `…\out\lauf.log:11-12` („Strahlungssplit Solar : AW=[0.000000] IW=[1.000000]").
(e) Ersatz: „Eintrag radiativ auf die Oberflächen; 9 % konvektiv an die Luft, wie in den Normtestfällen und im Prototyp (5.3). Die Verteilung auf AW/IW folgt `splitFacVal`."

**14. Zwei unvereinbare Formeln für die äquivalente Außentemperatur — Schwere: mittel**
(a) 4.4: „`θ_eq,k = θ_out + (α·I_k − ε·ΔE_r,k)/h_a` mit α = 0,6, h_a = 25 W/(m²K)"
(b) 5.3 beschreibt für dieselbe Größe „`(T_Himmel − T_Luft)·h_rad/(h_rad + h_a)`" und „kurzwelligem `H_sol·α/(h_rad + h_a)`". Mit den in 1.5/4.2 festgelegten Werten h_rad = 5 und h_a = 25 W/(m²K) stehen sich Nenner 25 und 30 gegenüber — 17 % Unterschied im Strahlungsterm; zudem ist h_rad in 4.2 als **innerer** Strahlungskoeffizient definiert und wird in 5.3 außen verwendet.
(c) Papier 4.2 (Tabelle R_rad), 4.4, 5.3, 1.5.
(e) Ersatz: „`θ_eq,k = θ_out + (α·I_k − F_r·ε·ΔE_r,k)/h_a` mit α = 0,6 und h_a = 25 W/(m²K) als **Summe** aus äußerem Konvektions- und Strahlungsübergang (h_a = h_conv,a + h_rad,a = 20 + 5); das ist dieselbe Formel, die 5.3 als `α·H_sol/(h_rad + h_a)` schreibt. Die Bezeichner sind zu vereinheitlichen."

**15. Fenster und Wärmebrücken fehlen in den Knotenbilanzen — Schwere: mittel**
(a) 4.2, Tabelle: „| Fenster, Wärmebrücken (masselos) | θ_out ↔ θ_air | U_w·A_w und Σψ·L — derselbe Zweig wie die Lüftung, deshalb im Netz mit R_ve zusammengefasst |"
(b) Dieselbe Tabelle definiert eine Zeile darüber R_ve = 1/H_ve mit H_ve = n·V·0,34, also rein die Lüftung; die fünfte Knotenbilanz führt entsprechend nur „(θ_out − θ_air)/R_ve". So gelesen fehlen U_w·A_w und Σψ·L in den Gleichungen — ein Leser, der die Gleichungen umsetzt, baut ein anderes Modell als der Prototyp.
(c) Papier 4.2, Widerstandstabelle Zeile R_ve und Gleichung 5; Gegenprobe `…\out\vergleich_zusammenfassung.csv` führt `UA_masselos` als eigene Spalte neben `H_ve` (10645: 232,73 gegen 131,55 W/K — der masselose Anteil ist fast doppelt so groß wie die Lüftung).
(e) Ersatz: „| R_ext | θ_out ↔ θ_air | 1 / H_ext, H_ext = H_ve + U_w·A_w + Σψ·L, H_ve = n · V · 0,34 Wh/(m³K) |" und in der fünften Bilanz „+ (θ_out − θ_air)/R_ext".

**16. h_ms = 9,1 W/(m²K) wird auf zwei verschiedene Flächen zugleich angewandt — Schwere: mittel**
(a) 4.3: „R_1,AW = 1 / (h_ms · A_AW,opak) (h_ms = 9,1 W/(m²K), DIN EN ISO 13790 12.2.2)" / „A_IW = f_IW · Wohnflaeche (f_IW = Innenflaechenfaktor, Vorgabe 2,5 — A_m/A_f nach ISO 13790)"
(b) In ISO 13790 gehört h_ms = 9,1 zur **einen** wirksamen Speicherfläche A_m der Zone (A_m = 2,5·A_f); das Papier setzt h_ms einmal auf die opake Hüllfläche und ein zweites Mal auf A_IW = 2,5·A_f und verdoppelt damit die an h_ms hängende Fläche. Für 10643 sind das 492 + 503 = 995 m² statt der 503 m² nach ISO.
(c) Papier 4.3; Flächen aus 3.4 (A_AW 280,3 + A_D 116,9 + A_G 88 + Sonstige ≈ 7,3 = 492,5 m²; Wohnfläche 201 m²), Gegenprobe `…\out\vergleich_zusammenfassung.csv` `UA_opak` 984,04 für 10643.
(e) Ersatz: „R_1,AW = 1/(h_ms · A_AW,opak) und R_1,IW = 1/(h_ms · A_IW) mit h_ms = 9,1 W/(m²K). **Abweichung von ISO 13790 bewusst:** dort hängt h_ms an einer einzigen wirksamen Speicherfläche A_m = 2,5·A_f; hier wird der Koeffizient auf beide Massepfade des 7R2C-Netzes angewandt. 5.9 zeigt, dass die Jahresenergie darauf nicht reagiert (≤ 0,1 %); für Leistungsgrenze und Kühlgrenze ist die Festlegung in G3 durch den Bauteilweg abzulösen."

**17. Die ISO-13790-Klassen sind nur zu dritt zitiert, die Namensgleichheit verdeckt die Verschiebung — Schwere: mittel**
(a) 4.3: „(leicht 72, schwer 180, sehr schwer 360 kJ/(m2K) je m² Wohnfläche; DIN EN ISO 13790 Tab. 12: 80 / 165 / 370 kJ/(m2K) für sehr leicht / mittel / sehr schwer)"
(b) Tab. 12 kennt fünf Klassen (80 / 110 / 165 / 260 / 370). Zitiert werden genau die drei, die zu den EPOS-Werten passen; ausgelassen sind ISO „leicht" (110) und ISO „schwer" (260) — also die beiden, deren Namen mit den EPOS-Namen übereinstimmen. Die EPOS-Klasse „schwer" (180) ist damit in Wahrheit die ISO-Klasse „mittel", die EPOS-Klasse „leicht" (72) die ISO-Klasse „sehr leicht". Das steht nirgends.
(c) Papier 4.3 und 3.3 („20 / 50 / 100 Wh/(m²K) für leicht / schwer / sehr schwer"); `EPOS.Kern\Allgemein\Gebaeudebauweise.cs:26-33` (LEICHT ×20, SCHWER ×50, SEHR_SCHWER ×100).
(e) Ersatz: „(die drei EPOS-Bauarten ergeben 72 / 180 / 360 kJ/(m²K) je m² Wohnfläche. DIN EN ISO 13790 Tab. 12 kennt fünf Klassen — 80 / 110 / 165 / 260 / 370 kJ/(m²K) für sehr leicht / leicht / mittel / schwer / sehr schwer. Die EPOS-Namen sind gegenüber ISO um eine Stufe verschoben: EPOS „leicht" entspricht ISO *sehr leicht*, EPOS „schwer" ISO *mittel*. Beim Umbenennen der Klappliste ist das zu berücksichtigen.)"

**18. Monatsmuster: die genannte Hochwinterspanne gilt für sieben von zwölf Projekten nicht — Schwere: mittel**
(a) 5.6: „Die Abweichung ist im Hochwinter klein (+10 bis +24 %) und in der Übergangszeit groß (+20 bis +180 %) … In allen Projekten dasselbe Muster."
(b) Im Hochwinter (Jan/Feb/Dez) liegen 1040, 1041, 1042, 1045 bei +0,8 bis +3,3 %, 1018 im Februar bei +32,9 % und 1008 bei +73 bis +82 %. In der Übergangszeit erreichen 1008 im Juli +823 % und 1018 im Juli +386 %; bei 1017 und 1023/1024 liefert das Tagesmodell im Juli 0 kWh, der relative Vergleich ist dort gar nicht definiert (im Protokoll als „–").
(c) `…\out\vergleich_monate.csv` (alle Zeilen); `…\out\lauf_vergleich.txt:20,44,65,86,107,158,179` (Zeilen „Abw %").
(e) Ersatz: „Die Abweichung ist im Hochwinter am kleinsten (+1 bis +25 %, bei der fehlerhaften Zeile 10576 +73 bis +94 %) und wächst zur Übergangszeit auf +20 bis +180 % (Ausreißer Juli: 1018 +386 %, 1008 +823 %; bei 1017 und 1023/1024 liefert das Tagesmodell im Juli 0 kWh, dort ist der relative Vergleich nicht definiert). Das Muster — klein im Winter, groß in der Übergangszeit — ist in allen Projekten dasselbe, die Höhe nicht."

**19. „86–101 %" ist nach oben und unten falsch gerundet — Schwere: mittel**
(a) 0.4: „trifft die Katalogkennzahl kWh/m²a zu 86–101 % (das heutige Modell 48–88 %)."
(b) Aus 5.5 ergibt sich 85,7 % (10643: 386,6/451) bis 100,3 % (10576: 112,8/112,5). Ein Wert über 100 % kommt nur bei der fehlerhaften Zeile 10576 vor und liegt dort bei 100,3 %, nicht 101 %.
(c) Papier 5.5 (Spalten „Prototyp kWh/(m²a)" und „Katalog"); `…\out\vergleich_zusammenfassung.csv` (`proto_kWh_m2a`).
(e) Ersatz: „trifft die Katalogkennzahl kWh/m²a zu 86–100 % — am unteren Rand 10643 (85,7 %), am oberen die fehlerhafte Zeile 10576 (100,3 %); das heutige Modell 48–88 %."

**20. F_F-Regel greift auf ein Baujahr zu, das es nicht gibt — Schwere: mittel**
(a) 4.4: „F_F = 1 − `Rahmenanteil` (Vorgabe 0,3, also 0,7; ab Baualtersklasse nach 1995: 0,75)"
(b) 2.2 hält für dieselbe Tabelle fest: „**kein Baujahr als Zahl**", und 3.1 zeigt `Baualtersklasse` als Buchstaben (A, D, F, G, H). Die Regel „ab Baualtersklasse nach 1995" ist ohne eine hier nicht definierte Zuordnung Buchstabe → Jahr nicht ausführbar — und F_F ist Teil des größten geratenen Hebels (6,4 %, 5.9/5.14).
(c) Papier 2.2 (Zeile „Klassen"), 3.1, 4.4, 5.14 Punkt 2.
(e) Ersatz: „F_F = 1 − `Rahmenanteil` (Vorgabe 0,3, also 0,7). Eine klassenabhängige Vorgabe (0,75 für neuere Bauten) setzt eine Tabelle `Baualtersklasse` → Baujahrspanne voraus, die es heute nicht gibt (2.2); sie gehört als eigener Punkt in 6.1."

**21. Der Wechsel 0,333 → 0,34 Wh/(m³K) wird nicht als Änderung benannt — Schwere: niedrig**
(a) 4.4: „**Lüftung** H_ve = `Luftwechselrate` · V · 0,34 Wh/(m³K), V = `Wohnflaeche` · `Raumhoehe`; ohne den Temperaturfaktor des Bestands."
(b) Der Bestand rechnet 1,2 · 0,2777777777777778 = 0,3333 Wh/(m³K); der neue Wert 0,34 hebt H_ve zusätzlich um 2 %. Der Satz nennt als Unterschied nur den Temperaturfaktor. In 5.9 taucht der Posten unkommentiert als „Lüftung mit 1/3" auf.
(c) `EPOS.Kern\Allgemein\BhkwPlan.cs:357-359`; Papier 3.3, 4.4, 5.9 (Zeile „Bestandsgewichte 0,83/0,95/0,45/0,83 und Lüftung mit 1/3").
(e) Ersatz: „**Lüftung** H_ve = `Luftwechselrate` · V · 0,34 Wh/(m³K), V = `Wohnflaeche` · `Raumhoehe`; der Bestand rechnet mit 1,2/3,6 = 0,3333 Wh/(m³K) und zusätzlich mit einem Temperaturfaktor — beides entfällt, der reine Zahlenwechsel hebt H_ve um 2 %."

**22. Nullstunden: 2 097 gegen nachgezählte 2 092 — Schwere: niedrig**
(a) 5.6: „Zugleich hat der Prototyp 2 097 Nullstunden gegen 936: das Tagesprofil schmiert an Übergangstagen Last auf alle Stunden"
(b) Nachgezählt aus der Ergebnisreihe des Prototyps sind es 2 092 Stunden mit Q = 0; der Wert 936 für das Tagesmodell stimmt exakt.
(c) `…\out\vergleich_1045.csv`, Spalte `Q_proto_W` (2 092 Nullen) gegen `Q_heute_W` (936 Nullen), 8 760 Zeilen.
(e) Ersatz: „Zugleich hat der Prototyp 2 092 Nullstunden gegen 936".

**23. Das Zwei-Buckel-Profil wird an den falschen Stunden belegt — Schwere: niedrig**
(a) 5.6, Tabelle: „| heute | 1 008 | 995 | 2 717 | 3 647 | 3 820 | 2 630 | **3 573** | **3 681** | 1 064 |"
(b) Die beiden fett gesetzten Werte sind nicht die Maxima des Tagesmodells: die Stundensummen erreichen ihr Maximum bei Stunde 19 (3 875 kWh) und Stunde 9 (3 833 kWh) — beide Stunden fehlen in der Tabellenauswahl, so dass der behauptete Doppelbuckel gerade an seinen Spitzen nicht belegt ist.
(c) Nachgerechnet aus `…\out\vergleich_1045.csv`, Spalte `Q_heute_W`, Jahressummen je Tagesstunde: h9 = 3 833, h19 = 3 875 kWh.
(e) Ersatz: Spalten 9 und 19 in die Tabelle aufnehmen und dort fett setzen: „| Stunde | 0 | 3 | 6 | 7 | 9 | 12 | 18 | 19 | 23 |" mit „| heute | 1 008 | 995 | 2 717 | 3 647 | **3 833** | 2 630 | 3 573 | **3 875** | 1 064 |".

---

**Geprüft und bestätigt:** Knotenbilanzen 4.2 (alle fünf Gleichungen vorzeichenrichtig und konsistent mit dem in 1.2 beschriebenen Fehler des Materials); die Eliminationsformel `A = C⁻¹(K·M⁻¹·K − diag(G_1+G_Rest, G_2))` ist korrekt hergeleitet und liefert mit den Parametern aus `…\out\lauf.log:6-10` (G1 228,9423; GRest 23,3816; G2 1678,7159; GcAW 28,35; GcIW 169,12; GRad 52,5; C1,AW 1 600 848,94; C1,IW 14 836 354,63) nachgerechnet die Zeitkonstanten 263,1 h und 5,34 h — wie in 4.2 mit „264 h und 5,3 h" angegeben; ebenso bestätigt sich das Verhältnis Nebendiagonale/Diagonale von 0,704 („rund 70 %", 1.2); beide Eigenwerte sind wegen der Symmetrie von `K·M⁻¹·K` zwingend reell und negativ, die Aussage ist beweisbar und nicht nur beobachtet. **R_Rest bleibt für alle Referenzgebäude positiv:** für 10643 (U_AW 2,90 / A_AW 280,3; U_D 0,80 / A_D 116,9; U_G 0,80 / A_G 88; dazu 7,25 W/K aus Sonstiges, `UA_opak` 984,04) ist A_AW,opak ≈ 492,5 m², 1/Σ(U·A) = 1,016·10⁻³, R_1,AW = 1/(9,1·492,5) = 2,232·10⁻⁴, R_si/A = 0,13/492,5 = 2,640·10⁻⁴ → **R_Rest = 5,29·10⁻⁴ K/W > 0**; die Formel wird erst ab einem mittleren U über 4,17 W/(m²K) negativ, der Höchstwert im Bestand liegt bei 2,01 (10643) — die Klemme 1e‑6 greift nirgends. Der R_si-Abzug von 0,13 m²K/W passt zum Ersatz durch h_conv 2,7 + h_rad 5 = 7,7 W/(m²K) = 1/0,13. Die Kusuda-Konstanten stimmen: Dämpfung exp(−1·√(π/(0,06·365))) = 0,6847 („0,68"), Phasenverzug 0,5·√(365/(π·0,06)) = 22,0 Tage, Erdreich Stuttgart 9,875 ± 9,315·0,6847 = 3,50 … 16,25 °C („3,5 … 16,3"). 0,34 Wh/(m³K) und F_W = 0,9 entsprechen ISO 13790; F_r 0,5 Wand / 1,0 Dach ist normkonform. C_ges = Bauweise · 3 600 J/Wh ist richtig. Die Tabelle 5.2 gibt alle zwölf Abweichungen der Rohdatei korrekt wieder (0,0553 → 0,055 usw.), ebenso die Reserve „nur 0,01 K" bei Testfall 10 (0,150 − 0,143 = 0,007). Tabelle 5.5 ist durchgängig aus den Rohdaten reproduzierbar (alle kWh-, kWh/(m²a)- und Abweichungswerte, Skalierungsprobe 62 566/13 617 = 4,5947 gegen den Faktor 4,5946); ebenso die Monatssummen (Prototyp-Summe 62 567 gegen 62 566 kWh), die Stundensummen des Projekts 1045 (alle neun Werte beider Modelle exakt), die vollständige Empfindlichkeitstabelle 5.9 einschließlich der Spalten Spitze und Stunden > 24 °C, die Variantenspannen in 5.8 (+3,2…+7,1 % bzw. −2,3…−14,1 % je Projekt), die Korrelationen (r Tag 0,982–0,997, r Stunde 0,736–0,925), die Kennzahlen in 5.7 (Jahresmittel 19,73–21,12 °C, 184–1 425 Stunden > 24 °C = 2,1–16,3 %, τ 7,5–24,8 h), der Sonderfall 5.11 in allen fünf Zeilen, sowie in 5.12 die Jahresspanne 0,7–2,5 %, die Morgen-/Abendsummen (−6,1…+10,3 % und −3,4…+3,9 %), die Stunden über 24 °C (−3,6…+8,2 %) und das Verhältnis Morgen-/Abendsumme 1,12–1,74. Auch 5.13 hält stand: Median 5,09 ms über die 15 Gebäude, Spanne 4,5–9,1 ms, 9 480 Schritte = 8 760 + 30 Tage Vorlauf, Umschaltereignisse 203–826 (= `segmente` − 8 760) und rund 7 µs je Ereignis erklären die Spanne rechnerisch. Die Codebelege zu `Gebaeudebauweise` (×20/50/100, Rückfall 50), `SpezWaermeverlusteC` (Gewichte 0,83/1,0/0,95/0,45/1,0, Brücken ×0,83) und `TaeglHeizlastWG` (Sollwertfenster 7–22, Solar nur h 9–14 mit Faktor 4, Kappung, statisches `_prevRoomTemp`) treffen den Code zeilengenau.