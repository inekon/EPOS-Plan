# Befund K — VDI 6020:2022 und VDI 2078:2015 gegen das Konzeptpapier (15.09.2026)

**Protokoll.** Befund eines Analyse-Agenten (Modell Opus) im Auftrag des Konzepts
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Sitzung vom 15.09.2026, nachdem der Anwender VDI 6020 (2022-12) und VDI 2078 (2015-06)
als PDF bereitgestellt hatte (Textfassungen unter `epos-spike\vdi\`). Wortlaut wie
geliefert; die Folgen stehen im Konzept, Nachtrag 1, Abschnitt N1.9. Die Richtlinien sind
urheberrechtlich geschützt — der Befund zitiert Seiten, Abschnitte und Gleichungsnummern,
keine Passagen und keine Tabellen.

**Nutzungsbeschränkung (Entscheid E6, 15.09.2026):** die ausgewertete VDI 6020:2022 liegt nur zu
Forschungszwecken vor. Dieser Befund ist eine Lesenotiz; nichts daraus geht in Code, Tests, Wiki,
Bericht oder Auslieferung. Produktregeln stützen sich auf VDI 6007 Blatt 1–3 und VDI 2078.

**Quellenlage.** Bilder und einige Tabellen liegen in den PDFs als Grafik vor und fehlen im
Textauszug — insbesondere Bild 15 (VDI 6020, S. 52) und Tabelle 8 (VDI 2078, S. 58), beide
die Zuordnung der Testbeispiele zwischen den Richtlinien, ferner Tabelle 6 (Mustertabelle,
VDI 6020 S. 60–61) und Bild C1 (Inhaltsverzeichnis des Datenträgers, S. 83). Die Zuordnung
ließ sich aus dem Fließtext und aus VDI 6007 Blatt 1, Abschnitt 6.6/6.7 rekonstruieren.

---

## 1. Ausgabe und Rechtslage

| | VDI 6020 | VDI 2078 |
|---|---|---|
| Ausgabe | Dezember 2022, Weißdruck, deutsch/englisch | Juni 2015, Weißdruck, deutsch/englisch |
| Titel | Anforderungen an thermisch-energetische Rechenverfahren zur Gebäude- und Anlagensimulation | Berechnung der thermischen Lasten und Raumtemperaturen (Auslegung Kühllast und Jahressimulation) |
| Frühere Ausgaben | VDI 6020 Blatt 1:2001-05; 09.16 Entwurf | 07.96; 03.12 Entwurf; VDI 2078 Blatt 1:2003-02 |
| Umfang | 96 nummerierte Seiten + Anhänge A–D | 149 nummerierte Seiten + Anhänge A–E |
| Vermerk je Seite | Ausdruck-Fußzeile „Datum/Uhrzeit des Ausdrucks: 2026-09-15, 17:57 — Firmenname: Eberhard Karls Universität Tübingen — Benutzername: ip-user — Printed copies are uncontrolled" | Beuth-Fingerabdruck + „BEST BeuthStandardsCollection – Stand 2016-11" |

**Nachfolge bestätigt.** VDI 6020:2022-12 ist der Weißdruck-Nachfolger der VDI 6020
Blatt 1:2001-05, die VDI 6007 Blatt 1 zitiert. Der Titel führt jetzt ausdrücklich die
Anlagensimulation; neu gegenüber 2001 sind Heating Design Period/Day (Anhang A1), die
Anbindung der Cooling Design Period an VDI 2078, Abschnitt 5.3 und Anhang A2 zur
Anlagenrückkopplung, die Testbeispiele 14–16, der Wegfall von Testbeispiel 11 und die
Konformitätserklärung nach DIN EN ISO/IEC 17050-1 (Anhang D).

**Rechtslage — abweichender Befund gegenüber Blatt 1 bis 3.** VDI 2078 trägt denselben
Beuth-Abo-Fingerabdruck wie Blatt 1 und 3 (spätere Lieferung, Stand 2016-11): plausibel
lizenziert. **VDI 6020:2022 trägt keinen Abo-Vermerk, sondern die Ausdruck-Fußzeile einer
IP-basierten Hochschullizenz** (Universität Tübingen, Benutzer „ip-user", Ausdruck am Tag
der Bereitstellung). Campus-Lizenzen sind üblicherweise auf Angehörige der Einrichtung und
auf Forschung und Lehre beschränkt; die Nutzung als Arbeitsgrundlage einer kommerziellen
Produktentwicklung ist davon in aller Regel nicht gedeckt. Das ist eine Feststellung zum
Dokument, keine Rechtsberatung. Empfehlung: Herkunft klären und für die Produktentwicklung
eine eigene Lizenz beziehen; bis dahin nichts aus VDI 6020:2022 in Code, Testdaten oder
Auslieferungsdokumente übernehmen.

## 2. VDI 6020:2022 — Struktur, Anforderungen, Testbeispiele

Gliederung: 1 Anwendungsbereich (S. 6), 5 Anforderungen und Randbedingungen (S. 12–33; 5.2
Nutzung, 5.3 Wärmezu-/-abfuhr durch die TGA), 6 Modelle und Rechenverfahren (S. 33–45),
7 Validierung (S. 46–48), 8 Testbeispiele (S. 48–60), 9 Durchführung/Mustertabellen
(S. 60–61), Anhang A Berechnungsalgorithmen (S. 62–76), B Klimadaten (S. 77–80), C Daten der
Testbeispiele (S. 81–93), D Konformitätserklärung (S. 94–95).

**Teilmodelle (6.1, S. 33–37):** keine eigene Physik, sondern Verweise — Raummodell →
VDI 6007 Blatt 1, Fenstermodell → Blatt 2, Strahlungsmodell → Blatt 3; meteorologische Daten
TRY des DWD mit Stadt- und Höhenkorrektur. Abschnitt 6.2 ordnet die Rechenverfahren
(Differenzenverfahren, Gewichtsfunktionen, elektrische Ersatzmodelle mit Beuken-, n-K-, 2-K-
und 1-K-Modell). Drei Aussagen sind für EPOS-Plan unmittelbar erheblich:

- **6.2.3.3 (S. 44):** das 2-K-Modell ist der Rechenkern für VDI 2078 gemäß VDI 6007
  Blatt 1; Bezugsperioden 7 Tage bzw. 2 Tage bei raumseitig abgedeckter Speichermasse —
  deckungsgleich mit Blatt 1.
- **6.2.3.4 (S. 45): 1-Kapazitäten-Modelle sind für Ganzjahressimulationen ungeeignet**,
  „ohne Ausnahme", einschließlich des Abschätzverfahrens der VDI 2078 und der analogen
  Rechnung in DIN V 18599. Das ist der normative Rückhalt für Entscheid E1: der
  Tagesbilanz-Weg des Bestands ist als Jahresrechenverfahren normativ disqualifiziert.
- **Einleitung (S. 5):** kein bestimmtes Rechenverfahren ist vorgeschrieben — weder das
  Beukenmodell noch Fenster- und Strahlungsmodell nach Blatt 2/3; maßgeblich sind ein
  gleichwertiges oder genaueres Verfahren und die geforderte Genauigkeit.

**Referenzmodell (6.3, S. 45):** n-Kapazitäten-Modell auf Beuken-Basis, gegen
Schaltkreisanalyse für die Beispiele 1–6 geprüft und nach ASHRAE 140 validiert.

**Validierungssystematik (7.1/7.2, S. 46–47):** punktuelle Vergleiche gelten als untauglich;
im Regelfall statistische Auswertung. Tabelle 2 (S. 47): **Typ a** (Raumtemperatur,
Heiz-/Kühllast) Mittelwert der stündlichen Abweichung ≤ 1,0 °C bzw. ≤ 50 W,
Standardabweichung ≤ 1,5 °C bzw. ≤ 60 W; **Typ b** (Außentemperatur, Solarstrahlung und
deren Umrechnung) absolut je Stunde ≤ ± 0,2 °C bzw. ≤ ± 5 W. Tabelle 4 (S. 52): Typ a für
die Beispiele 1–7 und 12–16, Typ b für 8–10; Prüfreferenz stets das n-K-Modell. Tabelle 5:
Beispiele 1–7 Einschwingperiode 60 Tage, Auswertung der Tage 1, 10, 60 getrennt; 8–10 und
12–16 über alle 8 760 Stunden des TRY gemeinsam.

**Randbedingungen (8.1, S. 49–51):** Tabelle 3 mit α_S = 5, α_a = 20, α_i,vert = 2,7,
α_i,hor = 1,7, α_Flächenkühlung = 5, α_Flächenheizung = 7 W/(m²K); innere
Strahlungsquellen flächenproportional; Fenstersolar ohne die Verglasung selbst und ohne
Bauteile gleicher Orientierung; kein Mobiliar; keine Horizontüberhöhung, Eigen- oder
Fremdbeschattung; c·ρ Luft = 1,2 kJ/(m³K); Beleuchtung und Personen je 50 % strahlend;
Projektstandort = Referenzstation des TRY. Abschnitt 8.3 (S. 51–53): alle 16 Beispiele sind
zu absolvieren; g-Wert und korg sind in den Strahlungsvorgaben enthalten, a_kon = 0,09,
Rahmenanteil 0 %.

**Testbeispiele (8.3.1/8.3.2, S. 53–60):** 1–4 Typraum S bzw. L, θ_a = 22 °C, konvektive
bzw. strahlende innere Quelle; 5 Typraum S mit Tagesgang und Fenstersolar; 6, 7
Lastberechnung mit Sollwertsprung (7 mit Leistungsgrenze); 8, 9 ohne Raummodell, TRY05
Würzburg, Strahlungslast Südseite mit/ohne langwelligen Austausch; 10 ohne Raummodell,
Umrechnung der Gesamtstrahlung auf N, S, O, W und horizontal, vor und hinter der
Verglasung; 11 nicht belegt (Inhalt der Ausgabe 2001 überholt, Nummer beibehalten); 12, 13
Typraum S, TRY05, helligkeitsgesteuerte Beleuchtung bzw. ohne, Zweipunktregelung; 14.1/14.2
Deckenkühlung oberflächennah bzw. Betonkern mit Fensterlüftung; 15 Quelllüftung; 16.1/16.2
Fensterlüftung ohne Kühlung (Zusatzlüftung in der Verkehrszeit bzw. auch nachts;
Schwellen θ_i > 23 °C, Δθ > 1 K). **Die Beispiele 1–7 sind identisch mit den
Testbeispielen 1–7 der VDI 6007 Blatt 1** (Gegenprobe: Klimadaten Tabelle B2, S. 77–78,
Zeile für Zeile gleich den Wetterspalten der Tabelle A5.3 in Blatt 1).

**Referenzergebnisse: nicht im Text.** Anhang C3.1 druckt nur Tabelle C1 (S. 84) mit
Zwischenergebnissen der Beispiele mit Anlagenrückkopplung; C3.2 (S. 84–93) enthält
ausschließlich Grafiken. Sämtliche Zahlen — Eingaben wie Ergebnisse — liegen als
Excel-Arbeitsmappen auf dem beiliegenden Datenträger (7.3, S. 47; 8, S. 48; 9.1, S. 60;
Anhang C2, S. 83). Gedruckt sind die Klimadaten der Beispiele 1–7 (Anhang B) und die
Schichtaufbauten der Typräume S und L (Anhang C1, deckungsgleich mit Blatt 1).

**Anlagentechnik:** 5.3 (S. 26–33) und Anhang A2 behandeln die Rückkopplung Raum ↔ Anlage
(verminderte Anlagenleistung, Proportionalbereich, Regelstrategien, Fensterlüftung); 5.3.3,
Gl. (4)–(8), S. 29–31, definiert die Bilanzkette Q̇_HK,Raum → Zuluftanteil + Restbedarf →
Nutzenergiebedarf. **Dort endet der Geltungsbereich:** eine Volltextsuche liefert in beiden
Richtlinien keinen Treffer für Wärmepumpe, Wärmeerzeuger, Pufferspeicher oder
Erzeugerbilanz. Die Deckungsrechnung in `SimulationControl` liegt außerhalb beider
Richtlinien; sie muss nur die Übergabegröße aus Gl. (5)/(6) entgegennehmen.

## 3. VDI 2078:2015 — Struktur, Verhältnis zur VDI 6007, Testbeispiele

Gliederung: 5 Meteorologische Daten (S. 12–18), 6 Gebäude und Nutzung (S. 18–38),
7 Berechnungsgrundlagen (S. 38–57), 8 Testbeispiele (S. 57–77), 9 Validierung (S. 77–81),
Anhang A Algorithmen (S. 86–115), B Kennwerte (S. 116–130), C Testbeispiele (S. 131–136),
D Abschätzverfahren (S. 137–147), E Konformitätserklärung.

**Auslegung gegen Jahressimulation:** Regelfall der Auslegung ist seit 2015 das
aperiodische Einschwingen in der Cooling Design Period (14 Tage bedeckt, 4 Tage Anlauf,
dann der Cooling Design Day; 7.3, S. 46–49; Anhang A1); Jahressimulation mit TRY.

**Verhältnis zur VDI 6007 (7.2, S. 45–46):** VDI 2078 nutzt das 2-K-Modell nach Blatt 1 als
Standard-Raummodell, das Strahlungsmodell nach Blatt 3 (5.2.1) und das Fenstermodell nach
Blatt 2 (5.2.2); andere Raummodelle sind zulässig, wenn nach Abschnitt 9 validiert.
Zusatzregel S. 46: Innenbauteile zu Nachbarräumen mit Δϑ < 4 K dürfen wie symmetrisch
beaufschlagte, adiabate Bauteile behandelt werden — die normative Deckung für den
Ein-Zonen-Ansatz innerhalb eines Gebäudes.

**Testbeispiele (8, S. 57–76):** 1–6 für die Räume XL bis XS (Auslegungskühllast, Hamburg
und Mannheim), 7–10 ohne Raummodell, 11 nicht übernommen, 12–16 mit dem Typraum S; 8–16 mit
TRY05 Würzburg (DWD 1986), Jahresrechnung für 4 und 6 mit TRY03 Hamburg und TRY12
Mannheim (DWD 2004). Die von Blatt 3, Abschnitt 13 aufgerufenen Fälle: **Testbeispiel 7**
(S. 62) — Außentemperatur und Sonneneinstrahlung, getrennt direkt/diffus, je Stunde der CDP
und des CDD; 7.1 Einstrahlung außen auf 1 m², 7.2 durch 1 m² Verglasung eintretende Energie
(korg nach Blatt 3 mit fiktivem U = 1,2 W/(m²K)); Ersatz für das entfallene Beispiel 11 der
VDI 6020. **8 und 9** Strahlungslast Südseite mit/ohne langwelligen Austausch; **10**
Umrechnung der Gesamtstrahlung auf N, S, O, W und horizontal.

**Toleranzen (9.2, S. 79) und Validierungsfälle (9.1, S. 78; Zuordnung S. 80):** Typ 1 =
Blatt-1-Grenzen ± 0,1 °C / ± 1 W je Stunde; Typ 2 = VDI-2078-Grenzen ± 0,2 °C / ± 5 W je
Stunde; Typ 3 = VDI-6020-Grenzen statistisch. **Fall A** (Rechenkern nach Blatt 1 und
Strahlungsmodell nach Blatt 3): Typ 1 für die Blatt-1-Beispiele, Typ 2 für die
VDI-2078-Beispiele. **Fall B** (anderer Rechenkern und/oder anderes Strahlungsmodell):
Typ 2 für die Testbeispiele 7–10, Typ 3 für alle übrigen. Anmerkung S. 78, einschlägig für
EPOS-Plan: ein Programm mit dem Rechenkern nach Blatt 1, aber anderem Strahlungsmodell, ist
mit den Blatt-1-Beispielen nach Fall A und mit den VDI-2078-Beispielen nach Fall B zu
validieren.

**Referenzergebnisse: ebenfalls nicht im Text** (Abschnitt 8.2 nur Verlaufsgrafiken; Zahlen
auf dem Datenträger, Anhang C2). Gedruckt sind die Häufigkeits- und Übertemperatur-
gradstundentabellen zu Testbeispiel 6 (Bild 55a/55b, S. 71–72) sowie **Tabelle 9** (S. 82–83)
und **Tabelle 10** (S. 84–85) für den Validierungsfall C. Tabelle 9 misst das 2-K-Modell
gegen das n-K-Modell über die zwölf Blatt-1-Beispiele: Mittelwert der stündlichen
Abweichung bis 0,64 K (Luft) / 0,66 K (operativ) / 17,1 W, Standardabweichung bis 1,09 K /
1,05 K / 40,9 W. Tabelle 10 für die VDI-2078-Beispiele: bis 0,75 K / 0,72 K / 40,7 W bzw.
0,78 K / 0,82 K / 55,6 W; Klimagrößen absolut bis 0,1 K, 1 W, 1 W/m².

**Nutzungsprofile und innere Lasten:** VDI 2078 definiert keine Nutzungsprofile je Raumtyp;
für innere Wärmequellen verweist sie auf DIN V 18599-10 (S. 22). Sie liefert:
Personenwärmeabgabe als Gleichungen (6)–(20), S. 25–26 (trockene Abgabe für vier
Aktivitätsgrade, 50 % strahlend); Beleuchtung (Tabellen 1–3, S. 28–30; B12);
Arbeitshilfen (Tabelle 4, S. 31); Wärmeübergangskoeffizienten (Tabelle 6, S. 41);
Beispielräume XL bis XS mit vollständigen Aufbauten (Anhang C1, S. 131–135, gedruckt);
Fassadenkennwerte (Anhang B3, S. 123–130: sechs Verglasungen × Sonnenschutzlagen und
-arten, zweischalige Fassaden, Korrekturfaktor für gekipptes Fenster); CDP-/CDD-
Klimaparameter je Kühllastzone (Anhang B1, S. 116–118, gedruckt).

## 4. Abgleich mit dem Konzept

**G0 bleibt unverändert:** Blatt-1-Testfälle 1–12, Band Programm 1…2 ± 0,1 K / ± 1 W
(in VDI 2078 als Typ 1 geführt). VDI 6020:2022 liefert keine aktualisierten Toleranzen für
diese Fälle und ersetzt sie nicht. Namensfalle: „Testbeispiel 11" ist in VDI 6020 seit 2022
nicht belegt, während „Testfall 11" im Konzept den Kühldeckenfall aus Blatt 1 meint —
Richtlinie stets mitnennen.

**Hay-Davies — Aussage bleibt, wird präziser:** nach VDI 6020 kein Normverstoß, sondern ein
geregelter Fall (Einleitung S. 5; 6.1.3; 5.1.11: „das Strahlungsmodell der VDI 6007 Blatt 3
oder ein adäquates Modell"); VDI 2078, 9.1, Fall B regelt den Nachweisweg. Zwei
Einschränkungen: (1) die Prüfschwelle für den Strahlungsweg ist hart und absolut, ± 0,2 °C
und ± 5 W je Stunde (Typ b / Typ 2 für die Testbeispiele 7–10); Hay-Davies gegen
Aydinli/Krochmann wird sie auf Nord- und Ostflächen nach aller Erfahrung reißen — solange
7–10 nicht bestanden sind, ist nur „Rechenkern nach VDI 6007 Blatt 1" belegbar, nicht
„validiert nach VDI 6020/2078"; (2) 5.1.11 (S. 16) fordert den Bedeckungsgrad oder die
Sonnenwahrscheinlichkeit als Klimaparameter — die fehlende Spalte in `Tab_Solar` ist damit
eine Anforderung der übergeordneten Richtlinie.

**Beschaffungspunkt N1.2 — teilweise erledigt, neu zu fassen:** beide Richtlinien liegen
vor, aber die Referenzergebnisse zu den Testbeispielen 7–10 stehen in keiner gedruckt; sie
liegen nur auf den Datenträgern. Zusätzlich fehlen TRY05 Würzburg (DWD 1986) für VDI 6020
TB 8–16 und VDI 2078 TB 8–16 sowie TRY03 Hamburg und TRY12 Mannheim (DWD 2004) für
VDI 2078 TB 4 und 6. Neu: Datenträger bzw. Downloadpaket zu VDI 6007 Blatt 1, VDI 2078 und
VDI 6020, die DWD-TRY-Datensätze, und eine eigene, für die Produktentwicklung taugliche
Lizenz der VDI 6020:2022.

**Abnahmekriterien 10.1 und 10.4:** 10.1 (G0) bleibt beim Band. Ergänzung in G1: eine eigene
Zeile „Strahlungsweg" mit VDI 2078, Testbeispiel 7 als Pflichtfall und Typ-2-Schwelle —
der einzige einschlägige Fall, dessen Eingangsdaten vollständig im gedruckten Text stehen
(Anhang A1, Anhang B1) und der ohne TRY-Datei rechenbar ist. Der Befund aus VDI 6020
6.2.3.4 (1-K ungeeignet) gehört in die Begründung von E1.

**Korrekturen gegenüber Nachtrag N1.3:**

| Stelle | Befund | Folge |
|---|---|---|
| c·ρ Luft | VDI 6020 S. 51: Standardwert 1,2 kJ/(m³K); Blatt 1 Testbeispiel 12: 1,1953 kJ/(m³K) | zwei Werte: 1,1953 für den Blatt-1-Fall 12, 1,2 für die VDI-6020-Beispiele; N1.3 präzisieren |
| Rahmenanteil | N1.3 („kein Begriff der VDI 6007, Quelle DIN V 18599") ist zu scharf: VDI 6020 5.1.15 (S. 17) regelt den Fensterrahmen ausdrücklich (Verglasung und Rahmen als ein oder zwei Bauteile, unterschiedliche Emissionsgrade; Details in VDI 2078); nur Blatt 2 schließt den Rahmen aus | Quelle ist VDI 6020 5.1.15 / VDI 2078 |
| Verschattung | VDI 6020 5.1.17 (S. 18) und VDI 2078 5.2.1 (S. 15) erlauben, die Diffusstrahlung bei Eigen- und Fremdbeschattung unkorrigiert zu lassen | entlastet die Pauschalfaktoren; für die Direktstrahlung bleibt Blatt 3, Abschnitt 12 |
| Bemaßung (neu) | VDI 6020 5.1.2 (S. 13): Außenbauteile nach dem Bruttomaß der Außenseite, Innenbauteile nach lichten Maßen; die Typräume S und L weichen historisch ab (Anhang C1, S. 81) | betrifft die U·A-Ermittlung nach Entscheid E2 und den Gebäudedialog; aufnehmen |
| Anlagenschnittstelle (neu) | VDI 6020 5.3.3, Gl. (4)–(8), S. 29–31 | normative Übergabegröße vom Gebäudemodell an `SimulationControl`; Erzeugerseite normfrei |
| Genauigkeitserwartung | VDI 2078 Tabelle 9/10: das 2-K-Modell weicht vom n-K-Referenzmodell im Mittel um bis zu 0,64–0,75 K und 17–41 W ab, Standardabweichung bis 1,1 K | als Genauigkeitserwartung ins Konzept; die gemessenen 86–100 % Katalogtreffer liegen im Rahmen |
| Nummerierung in VDI 6020 | Bild 1 (S. 7/8) verweist auf 4.1, 4.2, 5.1.1, 6.1, 7.3, 8.1 — gedruckte Gliederung führt dieselben Inhalte unter 5.1, 5.2, 6.1.1, 7.1, 8.3, 9.2 | Redaktionsfehler; beim Zitieren die gedruckte Gliederung verwenden |
| Beleuchtung/Kkor | Blatt 3, Gl. (100), S. 43 weicht bei der Tageslichtkorrektur bewusst von VDI 6020:2001 ab und folgt VDI 2078 | ohne Belang, solange keine tageslichtabhängige Beleuchtung gerechnet wird |
| Unstimmigkeit | VDI 6020 S. 58: Zulufttemperatur Testbeispiel 15 = 21 °C; Bildunterschrift S. 92: 18 °C | Vorrang des Datenträgers; nur relevant, falls TB 15 gerechnet wird |

## 5. Aufwand

**Gedruckte Referenzdaten: praktisch null** — abtippen ist keine Option. Ohne Datenträger
übernehmbar: VDI 6020 Typräume S und L (S. 81–83), Klimadaten der Beispiele 1–7 (S. 77–79),
Tabellen 2–5 und C1; VDI 2078 Räume XL bis XS (S. 131–135), CDP-/CDD-Klimaparameter
(S. 116–118), Kühllastzonen (Tabelle B1), Fassadenkennwerte (B3), Personen- und
Beleuchtungsgleichungen, Tabellen 9/10. **Import statt Abtippen: ja, sobald die Datenträger
vorliegen** — beide Richtlinien liefern Eingaben und Ergebnisse als Excel-Arbeitsmappen mit
vorbesetzten Arbeitsblättern und verknüpfter Auswertedatei (VDI 6020 9.1, S. 60; VDI 2078
9.3, S. 81). Zusätzlich zu beschaffen: TRY05 Würzburg (DWD 1986), TRY03 Hamburg und TRY12
Mannheim (DWD 2004); Starttag 1. Januar = Mittwoch; TRY-Referenzstation als Projektstandort.

**Pflicht und Kür für EPOS-Plan:** G0 Pflicht unverändert (Blatt 1, 1–12, Typ 1). G1
Pflicht für Strahlungsweg und Fenster: VDI 2078 Testbeispiel 7.1/7.2, Typ 2 (± 0,2 K /
± 5 W) — ohne Datenträger nur qualitativ gegen die Verlaufsbilder (S. 72). G1 dringend
empfohlen: Testbeispiel 10 (Umrechnung auf N, S, O, W, horizontal, vor/hinter Verglasung) —
der Fall, an dem sich Hay-Davies gegen Aydinli/Krochmann quantifiziert; braucht TRY05. G2
empfohlen: VDI 6020 Testbeispiel 16.1/16.2 (Fensterlüftung ohne Kühlung, Übertemperatur-
gradstunden) — deckt sich mit der Sommerlüftungsregel; für VDI 2078 Testbeispiel 6 sind
Häufigkeits- und Gradstundenwerte gedruckt (S. 71–72). Später optional: 8 und 9
(langwelliger Austausch außen), 12/13 (Beleuchtung), 14/15 (Kühldecke, Betonkern,
Quelllüftung). Nicht relevant: VDI 2078 Anhang D (Abschätzverfahren, 1-K) und die
Testbeispiele 1–6 (Auslegungskühllast). Anregung: die Heating Design Period (VDI 6020
Anhang A1, S. 62–63, informativ) als normnaher Ersatz für eine reine Norm-Außentemperatur
bei der Heizlast — passt zum Stundenmodell nach Entscheid E1.
