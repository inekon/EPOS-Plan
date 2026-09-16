# Befund I — VDI 6007 Blatt 1 bis 3 gegen das Konzeptpapier (15.09.2026)

**Protokoll.** Befund eines Analyse-Agenten (Modell Opus) im Auftrag des Konzepts
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Sitzung vom 15.09.2026, nachdem der Anwender die drei Blätter der Richtlinie als PDF
bereitgestellt hatte (Textfassungen unter `epos-spike\vdi\`). Wortlaut wie geliefert; die
Folgen stehen im Konzept, Nachtrag 1. Die Richtlinie ist urheberrechtlich geschützt — der
Befund zitiert Seiten und Gleichungsnummern, keine Passagen.

---

## 1. Ausgabe und Rechtslage der vorliegenden PDFs

| | Blatt 1 (Raummodell) | Blatt 2 (Fenstermodell) | Blatt 3 (Solare Einstrahlung) |
|---|---|---|---|
| Ausgabe | Juni 2015, Weißdruck | März 2012, Weißdruck | Juni 2015, Weißdruck |
| Frühere Ausgabe | 03.12 | 08.09 Entwurf, nur deutsch | 04.12 |
| ICS | 91.120.10, 91.140.10 | 91.120.10 | 91.120.10, 91.140.10 |
| Umfang | 96 Seiten | 76 Seiten | 36 Seiten |
| Wasserzeichen je Seite | Fingerabdruck + „BEST BeuthStandardsCollection – Stand 2016-05" | Fingerabdruck + „NormCD – Stand 2012-08" | identischer Fingerabdruck wie Blatt 1, „BEST … Stand 2016-05" |

Herausgeber ist der VDI (VDI-Gesellschaft Bauen und Gebäudetechnik, Fachbereich TGA), Vertrieb Beuth Verlag GmbH, Berlin. Die deutsche Fassung ist verbindlich (Blatt 1, Seite 1). Alle drei Titelseiten tragen den Vermerk, dass Vervielfältigung — auch innerbetrieblich — nicht gestattet ist; die Vorbemerkung (je Seite 2) behält Nachdruck, Fotokopie, elektronische Verwendung und Übersetzung auszugsweise wie vollständig vor und verweist auf die Lizenzbedingungen unter www.vdi.de/richtlinien.

Ein Klartext-Lizenzvermerk steht nicht im Text. Die PDF-Metadaten sind verschlüsselt (DRM), auslesbar ist nur der je Seite eingebrannte Hex-Fingerabdruck. Die Kombination aus zwei Beuth-Abo-Produkten („BeuthStandardsCollection" 2016-05 für Blatt 1 und 3, ältere „NormCD" 2012-08 für Blatt 2) ist das typische Bild eines gewachsenen Firmenbestands. **Bewertung: plausibel lizenziert**, aber der Fingerabdruck identifiziert den Bezieher; jede Weitergabe der PDFs ist rückverfolgbar. Für die Verwendung im Produkt (Abschnitt 8) ist der Abo-Vertrag maßgeblich, nicht die PDF.

## 2. Blatt 1 — Struktur und Fundstellen

Gliederung: Vorbemerkung/Einleitung (S. 2), 1 Anwendungsbereich (S. 3), 2 Begriffe (S. 4), 3 Indizes (S. 4), 4 Meteorologische Daten (S. 5–6), 5 Gebäude und Räume (S. 7), 6 Modellbildung (S. 7–37) mit 6.1 Anforderungen (S. 7), 6.2 Randbedingungen (S. 8–10), 6.3 Bauteile (S. 11–14), 6.4 Raum (S. 14–30), 6.5 Ablaufplan (S. 30), 6.6 Validierung (S. 31), 6.7 Testbeispiele (S. 31–36), 6.8 Programmierhinweis (S. 36–37); Anhang A1 Testbeispiele (S. 38–63), A2 Diagramme (S. 64–78), Anhang B Ablaufplan (S. 79–95), Schrifttum (S. 96).

- **2-K-Modell**: Begriff S. 4; die Bezeichnung „7R2C" kommt in der Richtlinie nicht vor. Vollständiges Ersatzschaltbild Bild 3, Seite 17. Elemente: R₁,IW, C₁,IW, R₁,AW, C₁,AW, R_Rest,AW sowie die Dreieckschaltung R_α;kon;IW, R_α;kon;AW, R_α;str;AW/IW, die per Stern-Dreieck-Transformation (Gl. (55)–(57), S. 25) in R_α;Stern;IL, R_α;Stern;AW, R_α;Stern;IW überführt wird; dazu R_Lue (Gl. (75), S. 27).
- **Ersatzparameter aus dem Schichtaufbau**: Abschnitt 6.3, S. 11–14. Kettenmatrix je Schicht Gl. (1)–(9), Gesamtwand Gl. (11) (Multiplikation von innen nach außen, Reihenfolge nicht vertauschbar). Identifikation: R₁ Gl. (12), R₂ Gl. (13), C₁ Gl. (14), C₂ Gl. (15), R₃ Gl. (16), C₁,korr Gl. (17).
- **Bezugsperioden** (S. 12–13): je Einzelbauteil T_BT = 7 Tage, Ausnahme raumseitig abgedeckte Speichermassen T_BT = 2 Tage; Entscheidungskriterium Gl. (10a)–(10d) über R₁;rel und C₁;rel, je Bauteil getrennt. Für die Zusammenfassung zum Raum T_RA = 5 Tage, Gl. (10e).
- **Getrennte Anregung innen/außen**: ja, implizit — R₁ folgt aus a₂₂/a₁₂, R₂ aus a₁₁/a₁₂ (Gl. (12)/(13)); bei symmetrischer Belastung reduziert sich das Modell auf R₁ und C₁ (S. 14), bei einseitiger auf Bild 2 mit C₁,korr.
- **Fenster im Netz** (S. 14 und S. 17): C_AF ≈ 0; R₁,AF = R_AF/6 (Gl. (25)), mit R_AF = (1/U_AF − 1/α_I − 1/α_A)·1/A (Gl. (26)). Die Fenster werden nach den Wänden parallelgeschaltet; die Kapazität der Wände bleibt dabei unverändert (Reihenfolge ergebnisrelevant). R_ges,AW Gl. (27), R_Rest,AW Gl. (28) mit Klemmen (28a)–(28c).
- **Äquivalente Außentemperatur**: Gl. (32), S. 18. θ_A,eq = θ_A,Lu + Δθ_lw + Δθ_kw. Langwellig Gl. (33) mit θ_Atm (Gl. (34)), θ_Erd (Gl. (35)), Emissionsgrad Erdboden 0,93, Emissionsgrad ε_F der Fläche und Sichtfaktor φ = (1 + cos γ_F)/2 (Gl. (36a)) bzw. mit Horizontüberhöhung γ_H (Gl. (36b)); α_str außen aus (E_A + E_E)/(θ_Atm − θ_Erd), Rückfallwert 5,0 (Gl. (37), S. 19). Kurzwellig Gl. (38): Δθ_kw = (I_dir + I_diff)·a_F/α_A mit α_A = α_kon,A + α_str,A. Für transparente Flächen entfällt der kurzwellige Term (Gl. (39), S. 20). Nachbarraum Gl. (40). Gewichtung über alle Außenflächen nach U·A (Gl. (41)/(42), S. 20).
- **Verteilung von Strahlungslasten**: innere Quellen flächenproportional, Gl. (43)/(44), S. 21: IW erhält (A_Raum − A_AW)/A_Raum. Solar durch Fenster Gl. (45)/(46): IW erhält (A_Raum − A_AW)/(A_Raum − A_v), AW (A_AW − A_v)/(A_Raum − A_v), wobei A_v die gesamte Fläche (opak + transparent) der jeweiligen Orientierung/Neigung ist — Fenster und parallele Bauteile werden nicht beaufschlagt (S. 10).
- **Übergangskoeffizienten**: α_str = 5,0 W/(m²K) ist nur für den inneren Strahlungsaustausch fest gesetzt (Gl. (30), S. 18; Anmerkung dort: α_ges > 5,0 nötig). Die konvektiven Werte sind je Bauteil vorzugeben (S. 10); in den Testräumen: Boden/Decke α_kon,i = 1,7, Wände/Fenster 2,7, Kühldecke (Testbeispiel 11) 5,0, außen α_kon,a = 20,0 (Tabellen A.x.1). α_A = 20 + 5 = 25 W/(m²K) folgt daraus, ist aber kein gesetzter Normwert.
- **Erdreich/Nachbarräume**: ein Erdreichmodell existiert nicht. Erdberührte und an Keller grenzende Bauteile sind „Außenwände" (Begriff S. 4) bzw. nicht adiabate Innenbauteile und werden über θ_NR,eq (Gl. (40), S. 20) angebunden; die Nachbarraumtemperatur ist Eingabe.
- **Regelung/Lastberechnung**: Aufteilung der Anlagenleistung Gl. (49)–(54), S. 22–23 (konvektiv / Flächenheizung IW / Flächenkühlung AW, Summe = 1, Heizen und Kühlen getrennt, Anteile stundenweise änderbar, Bild 4). Last als Reaktionsgröße Gl. (96)–(98), S. 28; Lufttemperatur als Reaktionsgröße Gl. (99)–(102), S. 29; operative Temperatur Gl. (103), S. 29: Mittel aus Raumluft- und flächengewichteter Oberflächentemperatur.
- **Zeitschritt und Lösung**: 1 h, Aktions- und Reaktionsgrößen als Stundenmittelwerte (Treppenfunktion), Laplace-Lösung mit Vorgeschichte VO (Gl. (108)–(114), S. 29), Konstanten KO in Tabelle 1 (IW, S. 26) und Tabelle 2 (AW, S. 28), E = exp(−Z) mit Grenze „Z > 170 ⇒ E = 0" (S. 27). Ausdrücklich (S. 24): Momentanwerte am Ende eines Zeitschritts bilden die Verhältnisse nicht korrekt ab.

## 3. Blatt 1 — die Testfälle

Zwölf Testbeispiele, nummeriert 1…12 (Abschnitt 6.7, S. 31–36; Tabellen im Anhang A1, je Fall drei Tabellen An.1 Bauteildaten, An.2 Nutzungsprofile, An.3 Wetter + Ergebnisse):

| Nr. | Seite (A1) | Kurzbeschreibung |
|---|---|---|
| 1 | 40–41 | Typraum S, 1 000 W konvektiv 6–18 Uhr, θ_a = 22 °C konstant, keine Strahlung |
| 2 | 42–43 | Typraum S, 1 000 W strahlend |
| 3 | 44–45 | Typraum L, konvektiv |
| 4 | 46–47 | Typraum L, strahlend |
| 5 | 48–49 | Typraum S, Außentemperaturgang + Fenstersolar, Sonnenschutz ab > 100 W/m² |
| 6 | 50–51 | Lastberechnung mit Sollwertsprung (22 → 27 → 22 °C) |
| 7 | 52–53 | wie 6, aber Auslegungsgrenze ±500 W |
| 8 | 54–55 | wie 5 + zweite Fassade West, kurzwellige Absorption a = 0,70 auf beide AW |
| 9 | 56–57 | wie 8 + langwelliger Austausch, ε = 0,90 |
| 10 | 58–59 | wie 5, FB1 nicht adiabat (Keller 15 °C) |
| 11 | 60–61 | wie 7, Kühlung über aufgeputzte Kühldecke (α_kon DE1 → 5,0) |
| 12 | 62–63 | wie 5 mit Fensterlüftung, c·ρ = 1,1953 kJ/(m³K) |

**Die Referenzergebnisse stehen als Tabellen im Text** (Tabelle An.3, jeweils zweite Seite des Falls): je Tag 1, 10 und 60, Stunden 1…24, jeweils Lufttemperatur, empfundene (operative) Temperatur und Heiz-/Kühllast, getrennt für Programm 1 und Programm 2; bei den Fällen 1–7 zusätzlich eine Spalte „Ergebnisse VDI 6020" (nur Lufttemperatur und Last). Temperaturen mit einer, Lasten mit null Nachkommastellen. Die Eingabetabellen und Ergebnisse liegen zusätzlich als Excel-Arbeitsmappen auf dem beiliegenden Datenträger (S. 38, Anmerkung 1).

**Zulässige Abweichung** (Abschnitt 6.6, S. 31): Raumluft- und operative Temperatur im Bereich der Ergebnisse von Programm 1 und Programm 2 ± 0,1 °C, Heiz-/Kühllasten im selben Bereich ± 1 W. Das ist ein Band zwischen zwei Referenzreihen, nicht eine einzelne Referenz.

**Prüfgröße**: Stundenmittel. Belege: S. 24 (Reaktionsgrößen als Stundenmittelwerte, Treppenfunktion; Momentanwert am Schrittende untauglich) und S. 38 (Ausgabe als Mittelwert der betrachteten Stunde, „n-te Stunde", die 11. Stunde = 10:00–11:00 Uhr). Weitere Randbedingungen: 60 Tage mit identischem Tagesgang, Start stationär bei 22 °C Außentemperatur, Quellen null, Nebenraum (nur Fall 10) 15 °C (S. 32–33); VO_Anfang = 0 (S. 29).

**Stichprobe gegen die Prototyp-Referenz (AixLib).** Testfall 1, Tag 60, Tabelle A1.3 (S. 41, Programm 1 = Programm 2) gegen `vergleich_1.csv`:

| Stunde | Richtlinie | AixLib-Referenz | Δ |
|---|---|---|---|
| 1 | 49,9 °C | 323,05 K = 49,90 °C | 0 |
| 7 | 54,9 | 328,05 K = 54,90 | 0 |
| 12 | 55,5 | 328,65 K = 55,50 | 0 |
| 18 | 56,2 | 329,35 K = 56,20 | 0 |
| 19 | 50,6 | 323,75 K = 50,60 | 0 |
| Tag 10, Std. 1 | 37,7 | 310,85 K = 37,70 | 0 |

Ebenso Testfall 12, Tag 60 (Tabelle A12.3, S. 63): 30,5 / 33,2 / 34,9 / 30,9 °C ↔ 303,65 / 306,35 / 308,05 / 304,05 K — identisch. Die AixLib-Reihen sind also nicht bloß gerundet, sondern die abgedruckten Normzahlen selbst, mit +273,15 K umgerechnet.

**Aber:** AixLib führt je Fall nur eine Spalte, und zwar nicht konsistent dieselbe. Testfall 10, Tag 60, Stunde 2: Richtlinie P1 = 25,3 °C, P2 = 25,4 °C — AixLib nimmt 25,3 (P1). Testfall 11, Tag 60, Stunde 10: P1 = −121 W, P2 = −122 W — AixLib nimmt −122 (P2). Daraus folgen Korrekturen zum Konzept 5.2:

- **Testfall 10** (Konzept: 0,143 K, „ja knapp"): Der Prototyp liefert Stunde 2 = 25,443 °C. Normband 25,3…25,4 ± 0,1 ⇒ 25,2…25,5 — bestanden mit 0,057 K Reserve. Die „knappe" Bewertung ist ein Artefakt des Einspaltenvergleichs.
- **Testfall 9** (Konzept: 0,136 K): Maximum Tag 60, Stunde 22: P1 = 41,3, P2 = 41,4; Prototyp 41,436 ⇒ Band 41,2…41,5, bestanden. Auch Stunde 12 (43,333 gegen Band 43,1…43,4) und Stunde 20 (41,733 gegen 41,5…41,8) liegen sicher innen. Die im Konzept beschriebene „Reserve von nur 0,01 K" existiert gegen die Richtlinie nicht.
- **Testfall 11** (Kühldecke): fällt auch gegen die Richtlinie durch. Tag 60, Stunde 10: Band −120…−123 W, Prototyp −126,27 W — 3,3 W außerhalb. Alle übrigen Stunden liegen innen.
- **Testfall 6**: hier ist der Prototyp schlechter als das Konzept meldet. Die Richtlinie (Tabelle A6.3, S. 51) gibt Tag 1, Stunde 7 +764 W, Stunde 19 −638 W. Die AixLib-Referenz ist vorzeichenverkehrt (−764 / +638). Die Richtlinie definiert eindeutig (S. 9): Q̇_HK > 0 = Heizlast, < 0 = Kühllast; die Fälle 7 und 11 halten diese Konvention in beiden Quellen ein, nur Fall 6 ist gedreht. Betragsmäßig: Band 763…765 W, Prototyp 765,48 W ⇒ 0,48 W außerhalb; mit der Rundungsunschärfe der ganzzahligen Tabelle (± 0,5 W) grenzwertig. Die Konzeptaussage „Testfall 6 negativ = Heizen" (5.3) ist normwidrig.

## 4. Blatt 1 — Ersatzparameter des Testraums

Die Zahlen R₁,IW = 0,000596 K/W, C₁,IW = 14 836 000 J/K, R₁,AW = 0,00437, R_Rest,AW = 0,04277, C₁,AW = 1 600 800 J/K stehen nirgends in der Richtlinie (Suche über alle 96 Seiten ohne Treffer). Sie sind abgeleitete Werte (aus AixLib bzw. dem Docx).

Was die Richtlinie stattdessen liefert, ist die vollständige Eingabeseite für die Ableitung: Tabelle A.1.1 (S. 40) für Typraum S — FB1 17,5 m² (PVC 2 mm / Estrich 45 mm / Steinwolle 060 12 mm / Beton 2400 150 mm), DE1 17,5 m² (gespiegelt), IT1 2,0 m² Buche 40 mm, IW1 38,5 m² Hohlblocksteine 240 mm, AF1 7,0 m² Fenster U = 2,1 W/(m²K), AW1 3,5 m² (Beton 2100 240 mm / Dämmung 047 62 mm / Fassadenplatte 25 mm) — und Tabelle A.3.1 (S. 44) für Typraum L — FB2/DE2 sechsschichtig mit Luftschicht 200 mm und Metalldecke 1 mm, IT2 Tischlerplatte 40 mm, IW2 Porenbeton 120 mm, AW2 Brettschalung/Dämmung/Brettschalung. Dazu λ, ρ, c, α_kon innen/außen, Neigung, Orientierung, g_tot,dir/g = g_tot,diff/g = 0,15 und a_kon = 0,09.

Damit ist der Nachweis für G3 möglich, aber anders als im Konzept formuliert. Die Richtlinie kennt keine Soll-RC-Werte zum Abgleich; sie gibt Schichtaufbau und Ergebnisreihe. Der normkonforme Nachweis lautet: Reduktion nach Gl. (11)–(17) aus Tabelle A.1.1/A.3.1 rechnen, damit simulieren, und die Tabellen A1.3 bzw. A3.3 innerhalb ± 0,1 K treffen. Der Vergleich gegen 0,000596 usw. bleibt als interne Zwischenprobe brauchbar (Plausibilität: C₁,IW = 14,84 MJ/K entspricht 49,8 % der physikalischen Kapazität aller adiabaten Bauteile — genau die Halbierung, die Gl. (12)/(14) für symmetrische Belastung von selbst erzeugen; C₁,AW = 1,60 MJ/K gegen 1,76 MJ/K Rohkapazität von AW1 passt zur außen liegenden Dämmung). **Warnung für die Umsetzung:** Konzept 4.3 schreibt „Innenbauteile symmetrisch bis zur Mittelebene". Die Richtlinie baut die Kettenmatrix über den vollständigen Schichtaufbau (Gl. (11), S. 13) und reduziert erst danach auf R₁/C₁ (S. 14). Wer zusätzlich an der Mittelebene halbiert, halbiert zweimal.

## 5. Blatt 2 — Fenstermodell

Blatt 2 ist kein Fenstermodell für den Raumknoten, sondern ein Rechenverfahren zur Ermittlung der energetischen Kennwerte einer Verglasungs-/Sonnenschutz-Kombination aus bis zu fünf festen Schichten mit Zwischenräumen (S. 3–5).

Festgelegt wird: g-Wert (5.1, S. 10): g = τ_e + q_i, mit q_i = q_i,c + q_i,r + q_i,v (Konvektion, Strahlung, Durchlüftung); Inward Flowing Fractions (5.3, S. 11). U-Wert (5.2, S. 10): U = U_r+c + U_v — ausdrücklich informativ: der nach Blatt 2 berechnete U-Wert geht nicht in Blatt 1, Blatt 3 oder VDI 2078 ein (Anmerkung S. 11). Konvektivanteil a_kon (8.5.3, S. 29 und Gl. (59), S. 39): a_kon = (q_i,c + q_i,v)/g, mit Sonnenschutz a_tot,kon = (q_i,c + q_i,v)_tot/g_tot. Thermik: konvektiver Übergang im Zwischenraum (8.1, S. 24), Strahlungsleitwert (8.2, Gl. (25), S. 24), Durchlüftung (8.3, S. 25), Lamellen mit Winkelfaktoren für 45° Anstellwinkel und 10 % Überdeckung (Tabelle A4, S. 48). Schnittstelle (Abschnitt 10, S. 39–40): an Blatt 1/3 gehen g, a_kon, T_L, und für die Kombination mit Sonnenschutz getrennt g_tot,dir, g_tot,diff, a_tot,kon, T_L,tot,dir, T_L,tot,diff.

Vorgabewerte (Anhang A5, Tabelle A5 ff., S. 49–57): Verglasungen mit (g / T_L / a_kon): Einfachglas Float 0,90 / 0,85 / 0,02; 2-fach Isolier 0,78 / 0,73 / 0,03; 2-fach Wärmeschutz 0,64 / 0,72 / 0,07; 2-fach Sonnenschutz 0,40 / 0,66 / 0,05; 2-fach Sonnenschutz verspiegelt 0,31 / 0,42 / 0,05; 3-fach Wärmeschutz 0,48 / 0,59 / 0,09. Sonnenschutzsysteme (τ_e / ρ_e): Raffstore 45° cut-off 0,00/0,60, verschmutzt 0,00/0,30, Screen hell 0,20/0,40, Screen dunkel 0,10/0,20. Kombinationstabellen liefern g_tot,dir / g_tot,diff / a_tot,kon — Beispiel Einfachverglasung: außen liegender Raffstore 0,14 / 0,43 / a_tot,kon = 0,10; innen liegender Raffstore 0,45 / 0,74 / a_tot,kon = 0,43, innen Screen dunkel durchlüftet a_tot,kon = 0,52.

Was Blatt 2 ausdrücklich nicht leistet: Rahmen, Profile und Randverbund werden von den Algorithmen nicht berücksichtigt (Abschnitt 9, S. 38–39). Ein „Rahmenanteil" ist keine Blatt-2-Größe. Verschattung durch Umgebung steht in Blatt 3 Abschnitt 12, nicht in Blatt 2.

**Empfehlung für das Konzept.** G1 übernimmt aus Blatt 2 nur die Tabellenwerte: (g, a_kon) je Verglasungsart und (g_tot,dir, g_tot,diff, a_tot,kon) je Sonnenschutzkombination aus Tabelle A5 ff. Das ersetzt den fest verdrahteten 9-%-Konvektivanteil und die pauschale Sonnenschutzminderung. G3 darf frühestens die winkelabhängige Korrektur korg aus Blatt 3 Abschnitt 8 aufnehmen. Der Rechenkern von Blatt 2 (Schicht-für-Schicht-Strahlungsphysik, Zwei-Bereichs-Modell, Zwischenraumkennwerte, Lamellen) gehört nicht in EPOS-Plan — er erzeugt Katalogwerte und ist ein eigenes Werkzeug.

## 6. Blatt 3 — Solarstrahlungsmodell

- **Sonnenstand** (Abschnitt 5, S. 5–7): OZ/MOZ/WOZ mit Sommerzeit Gl. (1)–(3), Zeitgleichung Gl. (5), Deklination Gl. (6), Sonnenhöhe Gl. (7), Stundenwinkel Gl. (8), Azimut Gl. (9)/(10) mit Nord = 0°, Ost = 90°, Süd = 180°, West = 270°, Einstrahlwinkel Gl. (11). Nur Nordhalbkugel (Anmerkung 2, S. 8). Berechnung zur Stundenmitte (S. 11: 11. Stunde ⇒ t = 10,5).
- **Umrechnung auf geneigte Flächen** (Abschnitt 7, S. 11–15): Verfahren nach Aydinli und Krochmann. Direkt Gl. (29)/(30). Diffus über einen Umrechnungsfaktor R_diff, getrennt nach zwei Himmelszuständen: bedeckt R_diff,bed Gl. (33) — nur von γ_F abhängig, rotationssymmetrisch; klar R_diff,klar Gl. (34) als Summe R₁₈₀ + R_WBL + R_WBNL + R_ξ (Gl. (37)–(44)) — anisotrop, abhängig von Sonnenhöhe, Flächenneigung und Azimutdifferenz. Mischung über die Sonnenwahrscheinlichkeit SSW = 1 − BED (Bedeckungsgrad), Gl. (47)–(49). Für TRY-Daten: P_diff,klar,hor = P_diff,bed,hor = P_diff,TRY,hor und F_B = 1 (Gl. (26)–(28), S. 11), SSW wird aber weiterhin gebraucht.
- **Bodenreflexion** (7.3, S. 15): Gl. (50), P_Umg,F = (P_diff,hor + P_dir,hor)·0,5·ρ_Umg·(1 − cos γ_F), Regelwert ρ_Umg = 0,2.
- **Horizontüberhöhung** (7.4, S. 15–16): Ersatzwinkel α_χ = γ_F + χ in Gl. (33) und (50), verallgemeinert Gl. (52)–(54). Verbauung/Eigenverschattung geometrisch in Abschnitt 12 (S. 31–35).
- **Langwelliger Austausch / Himmelstemperatur** (Abschnitt 10, S. 24–25): A (atmosphärische Gegenstrahlung) Gl. (84) für klaren Himmel und Gl. (85)–(88) bewölkungsabhängig; E (Ausstrahlung der Erdoberfläche) Gl. (89). Anmerkung S. 25: A und E heißen in Blatt 1 E_A und E_E. Für TRY sind beide Größen bereits enthalten; die Rechnung nach Abschnitt 10 gilt der CDP/HDP.
- **Validierung** (Abschnitt 13, S. 36): Testbeispiele 7–10 der VDI 2078 bzw. 8–10 der VDI 6020 — nicht in Blatt 3 abgedruckt.

**Vergleich mit dem Konzept.** Der isotrope `Sol_*`-Weg des Bestands ist nicht normkonform — die Kritik in 3.4/Q20 trifft zu. Hay-Davies ist aber ebenfalls nicht normkonform: Blatt 3 schreibt Aydinli/Krochmann vor, nicht das Zirkumsolar-plus-Isotrop-Schema von Hay-Davies. Es ist eine erhebliche Verbesserung gegenüber isotrop und für G1 vertretbar, muss aber als bewusste Abweichung von VDI 6007-3 benannt werden. Zwei Hürden für echte Normkonformität: (a) der Bedeckungsgrad ist in `Tab_Solar` nicht vorhanden, ohne SSW ist Gl. (47)/(48) nicht rechenbar; (b) Blatt 3 verlangt Längen-/Breitengrad des TRY-Referenzorts, nicht des Projektorts (S. 6, Hinweis). Die Albedo nennt das Konzept in 4.4 nicht; Normwert ist 0,2 (Blatt 3, S. 15) — das deckt sich mit dem Importwert des Bestands.

## 7. Abgleich mit dem Konzept

**Bestätigt:** zwei Massenknoten AW/IW, drei algebraische Knoten, voll besetzte Systemmatrix (Bild 3, S. 17); Stundenschritt, stückweise konstante Eingänge, Stundenmittel als Ergebnis (S. 24, S. 38); θ_op = ½·(θ_air + flächengewichtete Oberflächentemperatur) (Gl. (103), S. 29); radiative Lasten flächenproportional, Fenstersolar ohne die Fensterfläche selbst und die parallelen Bauteile (Gl. (43)–(46), S. 21; S. 10); h_rad = 5 W/(m²K) für den inneren Strahlungsaustausch (Gl. (30), S. 18); h_a = 25 W/(m²K) als Summe aus äußerer Konvektion und Strahlung (Gl. (38)); Kettenmatrix, Bezugsperiode, Identifikationsvorschrift für G3 (Gl. (1)–(17), S. 11–14); eine äquivalente Außentemperatur am AW-Pfad, U·A-gewichtet (Gl. (41)/(42), S. 20); ideale Regelung mit Sollwerthaltung über die ganze Stunde und affiner Leistung, Leistungsgrenze, Umschaltung innerhalb der Stunde (S. 24; Gl. (96)–(98)); Verbot des stillen Rückfalls, Division-durch-null-Behandlung (6.8, S. 36–37); „Blatt 3 = Solarstrahlungsmodell" im Kern richtig, aber unvollständig (zusätzlich langwellige Ein-/Ausstrahlung, Beleuchtungsschaltzeitpunkte, Verschattungsgeometrie).

**Widersprüche und Ungenauigkeiten:** siehe Konzept, Nachtrag 1, Abschnitt N1.3 (h_conv je Bauteil; Fensterpfad im AW-Zweig; Bezeichnungen; Blockmittel statt gleitend; Sichtfaktor statt Bewölkungsfaktor; α = 0,6 kein Normwert; 9 % konvektiv nur Testbeispiele; Rahmenanteil und Winkelfaktor außerhalb der Norm; Verschattung geometrisch; kein Erdreichmodell; c·ρ = 1,1953 kJ/(m³K) im Testfall 12; Vorzeichen Testfall 6; Prüfschwelle als Band; Testfall 11 nicht bestanden; Innenbauteile über den vollständigen Aufbau).

## 8. Folgen für Q2 und G0

Ja — G0 kann die Referenzwerte direkt aus der Richtlinie nehmen. Die Ergebnisreihen stehen vollständig im Text (Tabellen A1.3 … A12.3, S. 41–63), je Fall zwei Programmspalten × drei Tage × 24 Stunden × drei Größen. Für die heute geprüfte Größe je Fall sind das 12 × 2 × 3 × 24 = 1 728 Zahlen; mit Luft- und operativer Temperatur sowie Last rund 5 200. Dazu die Eingaben: zwölf Bauteiltabellen (je 14 Zeilen), zwölf Nutzungstabellen (je 24 Zeilen × 12 Spalten) und für die Fälle 5, 8, 9, 10, 12 die Wetter-/Einstrahlspalten.

Abtippen ist vermutlich unnötig: Blatt 1, Seite 38, Anmerkung 1 nennt einen beiliegenden Datenträger mit Excel-Arbeitsmappen aller Eingabewerte und Ergebnisse. Erster Schritt in G0: prüfen, ob dieser Datenträger (bzw. der Download im Beuth-/DIN-Media-Konto) zum lizenzierten Bestand gehört.

Rechtliche Bedingung: Die PDFs sind DRM-geschützt und je Seite fingerabgedruckt; die Vorbemerkung (S. 2) behält die elektronische Verwendung auszugsweise wie vollständig vor. Interne Validierung mit einer lizenzierten Ausgabe ist der bestimmungsgemäße Gebrauch; das Ausliefern der Normzahlen in Testdateien eines kommerziellen Produkts ist eine Vervielfältigung und bedarf einer Klärung mit dem VDI. Praktischer Weg: Normzahlen als nicht ausgeliefertes Prüfmittel führen, im ausgelieferten Test nur die berechneten Abweichungen und das Bestanden-Kriterium. Damit entfällt zugleich die AixLib-Lizenzfrage; der AixLib-Vermerk wird nur noch dort gebraucht, wo Formeln nachgebaut wurden — und auch das lässt sich jetzt gegen Blatt 1 Gl. (32)–(46) belegen.

Welche Testfälle ohne Blatt 2/3 nicht gehen: keiner. Alle zwölf Fälle sind in Blatt 1 selbsttragend. Blatt 2 und 3 werden erst für reale Projekte gebraucht; ihre Validierung läuft über fremde Testbeispiele: Blatt 3, Abschnitt 13 verweist auf VDI 2078 Testbeispiele 7–10 bzw. VDI 6020 Testbeispiele 8–10 — beide Richtlinien liegen nicht vor; Blatt 2, Abschnitt 11 verweist auf Anhang A5 und den Datenträger. Ein normkonformer Nachweis für den Strahlungsweg verlangt also zusätzlich VDI 6020 oder VDI 2078.
