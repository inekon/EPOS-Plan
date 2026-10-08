# Recherche A — Rücklaufgrenzen von Wärmepumpen im Datenblatt-Sweep

Reine Web-Recherche (nur lesen, kein Repo-Bezug). Ziel: prüfen, ob Wärmepumpen neben dem
Höchstvorlauf auch eine Rücklaufgrenze (max., ggf. min.) haben — über R744 hinaus auch bei
unterkritischen Kältemitteln und bei Gas-Absorptionswärmepumpen. 16 Produktlinien untersucht
(Zielkorridor 12–14, durch die kategorienweise „×1–2"-Spannen bis 16 ausgeschöpft).

Kennzeichnung in allen Zellen: **ausdr.** = im Herstellerdokument wörtlich/als Zahl genannt;
**Abl.** = aus genannten Werten abgeleitet/berechnet, nicht selbst so im Dokument; **sek.** =
Händler-/Sekundärquelle, weil Originaldokument nicht (vollständig) auswertbar war; **k. A.** =
keine Angabe gefunden; **nicht erreichbar** = Dokument lag vor, war aber technisch nicht
auswertbar (siehe Fallstricke unten).

**Fallstricke der Recherche:** Viele Hersteller-PDF (Planungsunterlagen, Data Books, Submittals)
ließen sich mit dem verfügbaren Fetch-Werkzeug nicht als Text extrahieren (eingebettete
Schriften ohne Unicode-Zuordnung → nur Binärmüll). Dort wurde auf die Snippet-Synthese der
Suche, auf HTML-Spiegelungen (manualslib, Händlerseiten) oder auf Sekundärquellen ausgewichen;
das ist in der Tabelle vermerkt. Absolutwerte aus Foren/Communities sind als solche gekennzeichnet
und nur verwendet, wenn kein Herstellertext erreichbar war.


## (a) Tabelle je Produktlinie

| # | Kältemittel | Typ / Hersteller | VL max | RL max (ausdr./Abl.) | RL/VL min | Nennspreizung | Spreizung min/max | Mindestvolumenstrom | Regelparameter | Quelle |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | R290 | Viessmann Vitocal 250-A/252-A Monoblock, Luft/Wasser | 70 °C (Modernisierungsgerät) | **65 °C, ausdr.** (PRO-Variante) | VL min 20 °C (Parameter 1192–1194, Werksvorgabe); Estrichprogramm startet laut Anwenderforen ab ca. 25 °C (sek., kein Herstellerwortlaut) | k. A. | k. A. | ca. 1000 l/h beim Abtauen (ausdr.); im normalen Heizbetrieb niedrigere Werte berichtet, Herstellerminimum für Normalbetrieb nicht isoliert (sek.) | Rücklauf-Obergrenze als Geräteschutz (PRO 65 °C); externer Restdruckverlust 600 mbar bei 1000 l/h | Q42, Q43 |
| 2 | R290 | Vaillant aroTHERM plus VWL 35/6 A – 125/6 A (inkl. 75/6), Luft/Wasser | 75 °C | **nicht zweifelsfrei ermittelt** — Installationsunterlagen erwähnen eine Verdichter-Sperrbedingung bei Rücklauf „> 56 °C"; Kontext (Dauerbetrieb vs. Einzelfall) nicht geklärt → als unsicher gekennzeichnet, nicht als Grenzwert übernommen | VL min 20–22 °C (Dokumente uneinheitlich); Estrichtrocknung ohne E-Heizstab ab RL **> 10 °C** möglich, ausdr. | k. A. | k. A. | modellabhängig 400–995 l/h (min) bis 860–2065 l/h (max), ausdr. je Baugröße | Betriebsdruck 0,05–0,30 MPa; kein einzelner RL-Sollwert gefunden | Q44, Q45 |
| 3 | R290 | Stiebel Eltron WPL-A Plus/HT (Luft/Wasser) mit Wärmepumpenmanager WPMW II/WPMS II | 75 °C (HT-Reihe) | **„Rücklauftemperatur-MAX": Werkseinstellung 50 °C, einstellbar 20–55 °C, ausdr.** — bei Erreichen am Rücklauffühler schalten alle Wärmepumpen sofort ab | k. A. (nicht in den Auszügen) | k. A. | k. A. | 640 l/h (WPL-A 05/07 HK230 Premium), ausdr. | Explizite „Rücklauftemperatur-MAX"-Funktion; Begründung: Hochdruckwächter soll nicht ansprechen, keine Fehlermeldung bei Erreichen | Q46, Q47 |
| 4 | R32 | Mitsubishi Ecodan PUZ-WM50…WM112, Luft/Wasser | 60 °C | **59 °C, ausdr.** (zwei unabhängige Mitsubishi-Technikdokumente: „nominal return water temperature range +9 to +59 °C") | **RL min 9 °C, ausdr.** | k. A. | k. A. | modellabhängig 6,5–14,3 l/min (WM50) bis 14,4–32,1 l/min (WM112) — unterer Wert = Minimum | k. A. außer Bereichsgrenzen | Q48 |
| 5 | R32 | Daikin Altherma 3 (M / Low-Temp-Monobloc / H MT/HT), Luft/Wasser | modellabhängig, Standard 60–65 °C, HT-Reihe höher (nicht einheitlich isoliert) | **55 °C, ausdr.**: „Der Rücklauf zur Wärmepumpe darf 55 °C nicht überschreiten" (Installer Reference Guide, modellübergreifend zitiert) | k. A. | k. A. | k. A. | 12–28 l/min je Modell/Betriebsart (12 l/min Low-Temp 05+07; 20 l/min Heizen/Abtauen > −5 °C; 22 l/min < −5 °C; 28 l/min Trinkwarmwasser) | Mindestvolumenstrom-Überwachung mit Fehlercode 7H und sofortigem Stopp; RL-Grenze 55 °C separat genannt | Q49 |
| 6 | R410A | Mitsubishi CAHV-P500YA-HPB, gewerbliche Luft/Wasser-WP mit Zwischeneinspritzung (EVI) | 70 °C (Bereich 25–70 °C) | **nicht als Absolutwert benannt; Abl.** aus ΔT-Regelband 3–5 K bei VL 70 °C → RL ca. 65–67 °C | **VL min 25 °C, ausdr.** | **3–5 °C als Regelband, ausdr.** („Differenz zwischen Ein- und Austritt soll 3–5 °C betragen") | < 3 °C → Durchfluss verringern; ≥ 6 °C → Durchfluss zu gering, ausdr. | 7,5 m³/h von Gesamtbereich 7,5–15,0 m³/h (= 50 % des Bereichsmaximums), ausdr. | ΔT-Band 3–5 K ersetzt hier eine feste RL-Grenze als zentrale Durchfluss-Regelgröße | Q50 |
| 7 | R410A | NIBE F2040 (6–16 kW), Luft/Wasser | ca. 58 °C (Austritt WP) | **ca. 55 °C, ausdr. laut Technikdaten** (Quelle teils sek., Originaldokument nicht vollständig gegengelesen) | k. A. | k. A. | k. A. | F2040-12: Minimum 0,15 l/s (≈ 540 l/h); F2040-8: 0,38 l/s als Maximum (Ladestrang) | k. A. isoliert | Q52 |
| 8 | R454C | Mitsubishi Ecodan CAHV-R450YA-HPB, gewerbliche Luft/Wasser-WP mit EVI (Nachfolger von CAHV-P) | 70 °C (Bereich 24–70 °C) | **nicht separat benannt; Abl.** — vermutlich dieselbe ΔT-Logik wie CAHV-P, für R454C-Variante nicht einzeln bestätigt | **VL min 24 °C, ausdr.** | k. A. (vermutlich 3–5 K wie CAHV-P, nicht bestätigt) | k. A. | 25 l/min von Bereich 25–250 l/min (= 10 % des Maximums), ausdr. | k. A. isoliert; Umgebungsbereich −25 bis +43 °C | Q51 |
| 9 | R407C | Hoval Belaria twin I (20–30) / twin IR (20–30), Luft/Wasser | **55 °C, ausdr.** | **nicht erreichbar** | k. A. | k. A. | k. A. | k. A. | max. Betriebsdruck Heizseite 6 bar; Abtau per Kreislaufumkehr | Q53 |
| 10 | R744 (transkritisch) | Mitsubishi Q-ton, gewerbliches Trinkwarmwassergerät | 90 °C (Austritt, Bereich 60–90 °C einstellbar) | **63 °C, ausdr.** — hier kein Heizkreis-Rücklauf, sondern Kaltwasser-/Rücklaufeintritt im „Warm-up"-Modus (35–63 °C); „Top-up"-Modus 5–35 °C | **Kaltwassereintritt min. 5 °C, ausdr.** | k. A. (DHW-Gerät, kein klassischer Heizkreis) | k. A. | k. A. in Auszügen; 30-kW-Gerät liefert laut Hersteller > 600 l/h bei Nennbedingungen (Abl.) | Zwei Betriebsmodi mit unterschiedlichen Eintrittsfenstern — Begründung: Gaskühler-Auslegung des transkritischen Kreises, nicht Verflüssiger-Druckschutz | Q54 |
| 11 | R744 (transkritisch) | Mayekawa Unimo AW, industrielles/gewerbliches Heizgerät, Luftquelle | **90 °C (194 °F), Bereich Austritt 65–90 °C, ausdr.** | **65 °C (149 °F), ausdr.** als Obergrenze Einlasswasser — klassischer Heizkreis-Rücklauf | **RL min 5 °C (41 °F), ausdr.** | k. A. | k. A. | max. Durchfluss 8,7 gpm (≈ 1976 l/h); Minimum nicht separat genannt | Eintrittsfenster 5–65 °C als Gaskühler-/Prozessgrenze des transkritischen CO₂-Kreises | Q55 |
| 12 | R1234ze(E) | Friotherm Unitop 43 / Unitop 50, Industrie-/Fernwärme-Großwärmepumpe (Turboverdichter, zweistufig) | Unitop 43: 50 °C einstufig / > 80 °C zweistufig; Unitop 50: > 80 °C | **kein fester Herstellergrenzwert gefunden.** Betriebsbeispiel einer 5×-Unitop-50FY-Anlage (sek., Fachbericht): Winter 50 °C Eintritt/62 °C Austritt; Sommer 45 °C Eintritt/88 °C Austritt — Betriebsdaten, keine deklarierte Grenze | k. A. | k. A. (Anlagenbeispiel zeigt ΔT 12–43 K je Lastfall) | k. A. | k. A. | Grenze ergibt sich faktisch aus zulässigem Verdichter-Austrittsdruck/-temperatur, nicht aus einer wasserseitigen Rücklaufgrenze | Q56 |
| 13 | R600/R601/R1234ze(E)/R1233zd(E) | Viking Heat Engines HeatBooster, industrielle Hochtemperatur-WP (Kolbenverdichter) | bis 165 °C (aktuelle Hardware), bis 215 °C vorbereitet; Senkenbereich insgesamt 70–200 °C angegeben | **nicht separat als Grenzwert benannt.** Unterer Rand des Senkenbereichs (70 °C) könnte faktisch die Mindest-Rücklauftemperatur der Anwendung markieren — Abl., unsicher | Quellenbereich (Wärmequelle, nicht Senken-Rücklauf) 2–150 °C, ausdr., aber anderer Bezug | k. A. | k. A. | **nicht erreichbar** | k. A. isoliert | Q57 |
| 14 | NH₃/H₂O (Gas-Absorption) | Robur GAHP-A, Gasabsorptionswärmepumpe Luft/Wasser | 60 °C (140 °F) Spitzenwert, nominal 50 °C (122 °F); DHW separat max. 60 °C | **ausdr., sehr explizit:** Wasser/Glykol-Eintritt darf im Dauerbetrieb 50 °C (122 °F) nicht überschreiten, absolutes Maximum 55 °C (131 °F) — sonst Abschaltung zum Schutz vor Hochdruck im Ammoniak-Kältekreis | Kontinuierlicher Betrieb ab ca. 2 °C (35,6 °F) möglich, ausdr. | Nennbedingungen A7W35/A7W50/A7W65 mit 41,3/38,3/31,1 kW bei konstantem Nenndurchfluss 2500 l/h — lastabhängig statt fixem ΔT-Sollwert | bei VL 60 °C (Spitzenlast) ΔT ca. 15 K (27 °F), ausdr. | **1400 l/h von Nenndurchfluss 2500 l/h (56 %)** bzw. von Max. 4000 l/h (35 %) — Werte ausdr., Prozentsatz Abl. | Explizite RL-Grenze (50/55 °C) als Hochdruckschutz des Ammoniak-Kreises — direkte Begründung wie bei Kompressions-WP | Q58 |
| 15 | R410A (Sole/Wasser) | Viessmann Vitocal 300-G, Erdwärme | 60–65 °C je Baureihe (BWC 65 °C, BWS-A-Serie 60 °C) | **nicht erreichbar** in den ausgewerteten Auszügen | Sole-Eintritt (Quelle, nicht Heizungsrücklauf) −10 bis 25 °C, ausdr. | k. A. | k. A. | Sekundär (Heizwasser) 1100 l/h, Primär (Sole) 1800 l/h, beide ausdr. (Restförderhöhen 650/590 mbar) | max. Betriebsdruck 3 bar beidseitig | Q59 (teilw. sek.) |
| 16 | k. A. (Sole/Wasser, beliebig) | NIBE S1255, Erdwärme | ca. 70 °C (65 °C nur mit Verdichter, 70 °C mit Zusatzheizung) | **ca. 58 °C — ausdr. laut Herstellerangabe, Quelle teils sek. transkribiert** | Heizwasser-Austrittsbereich unteres Ende 15 °C, ausdr. | k. A. direkt | k. A. | **10 l/kW** (leistungsbezogene Herstellerkennzahl statt Absolutwert) | k. A. isoliert | Q59 |


## (b) Auswertung

**Ausdrückliche max. Rücklauftemperatur — wie viele?**
Von 16 Produktlinien nennen **9** (56 %) einen ausdrücklichen Zahlenwert für die maximale
Rücklauf- bzw. (bei den beiden CO₂-Geräten) Eintrittstemperatur: Viessmann Vitocal 250-A PRO
(R290, 65 °C), Stiebel Eltron WPL-A über WPMW/WPMS-Manager (R290, 50/55 °C), Mitsubishi Ecodan
R32 (59 °C), Daikin Altherma R32 (55 °C), NIBE F2040 R410A (ca. 55 °C, schwächer belegt),
Mitsubishi Q-ton R744 (63 °C, DHW-Eintritt), Mayekawa Unimo R744 (65 °C, echter Heizkreis-Rücklauf),
Robur GAHP-A NH₃/H₂O (50/55 °C, am explizitesten begründet) und NIBE S1255 Sole/Wasser (ca. 58 °C,
schwächer belegt). **Das Entscheidende: mindestens 5 der 7 nicht-R744-Fälle mit solider
Primärquelle liegen außerhalb von CO₂** — R290, R32 und NH₃/H₂O-Absorption nennen ebenso
explizite Rücklaufgrenzen wie R744. Keine ausdrückliche Grenze fanden sich bei den beiden
gewerblichen EVI-Geräten (Mitsubishi CAHV-P/-R, R410A/R454C — dort regelt statt eines Festwerts
ein ΔT-Band von 3–5 K den Volumenstrom), bei Hoval Belaria (R407C), bei den beiden
Großwärmepumpen (Friotherm R1234ze(E), Viking HeatBooster) und bei Vaillant aroTHERM plus (R290,
nur eine unklare „>56 °C"-Verdichterbedingung, nicht als generelle Grenze bestätigt) und Viessmann
Vitocal 300-G (Sole/Wasser).

**Typischer Abstand VL max − RL max?**
Uneinheitlich, abhängig von der Bauart: Bei drehzahlgeregelten Kompressions-WP mit fester
ΔT-Logik ist der Abstand klein (Ecodan R32 ≈ 1 K, NIBE F2040 ≈ 3 K, CAHV-EVI ≈ 3–5 K als Regelband,
Robur GAHP-A bei Spitzenlast ≈ 5 K). Bei den R290-Hochtemperaturgeräten mit fester
„Rücklauf-MAX"-Abschaltschwelle liegt er deutlich höher (Viessmann 250-A ≈ 5 K bezogen auf 70/65 °C,
Stiebel WPL-A ≈ 20–25 K bezogen auf 75 °C VL und 50/55 °C RL-Abschaltung — hier sind VL max und
RL max aber unterschiedliche Betriebspunkte, kein direkter Vergleich). Am größten ist der Abstand
bei den beiden CO₂-Geräten (Q-ton ≈ 27 K, Mayekawa Unimo ≈ 25 K) — das ist kein Zufall, sondern die
Auslegungsbesonderheit des transkritischen Gaskühlers, der gerade von einem großen Temperaturhub
lebt. Insgesamt: **5–10 K bei unterkritischen Kompressions-WP mit ΔT-Regelung, 20–25 K bei
Geräten mit fester Abschaltschwelle, 25–30 K bei den CO₂-Geräten.**

**Minimale Rücklauf-/Vorlauftemperatur — wie viele und welcher Wert?**
8 von 16 Linien nennen einen Minimalwert (VL- oder RL-seitig): Viessmann 250-A (VL min 20 °C,
Parameter), Vaillant aroTHERM plus (VL min 20–22 °C; RL > 10 °C für Estrich ohne E-Heizstab),
Ecodan R32 (RL min 9 °C), CAHV-P/-R (VL min 24–25 °C), Q-ton (Kaltwassereintritt min 5 °C),
Mayekawa Unimo (RL min 5 °C), Robur GAHP-A (Betrieb ab ca. 2 °C) und NIBE S1255 (VL-Austritt ab
15 °C). Muster: **Komfort-/Wohngebäude-Geräte liegen bei 15–25 °C** (Heizkurven-Mindestvorlauf,
Estrichtrocknung), **Gewerbe-/Industriegeräte eher bei 2–9 °C** (reine Frostschutz-/
Kaltstart-Untergrenze ohne Komfortbezug).

**Mindestvolumenstrom in % des Nennstroms?**
Nur dort berechenbar, wo Hersteller Minimum und Maximum/Nennwert gemeinsam nennen: Mitsubishi
CAHV-P (7,5 von 15,0 m³/h = 50 % des Bereichsmaximums), Mitsubishi Ecodan R32 (6,5 von 14,3 bzw.
14,4 von 32,1 l/min ≈ 45 % des Bereichsmaximums, modellübergreifend konsistent), Robur GAHP-A
(1400 von 2500 l/h Nenndurchfluss = 56 %, bzw. von 4000 l/h Maximaldurchfluss = 35 %). Deutlich
außerhalb dieses Clusters liegt die modulierende Mitsubishi CAHV-R (R454C): 25 von 250 l/min = nur
10 % — ein viel größerer Turndown, vermutlich durch den breiteren Invertermodulationsbereich des
neueren Geräts. **Grobes Muster: 35–56 % des Nenn-/Maximaldurchflusses als Minimum, mit einzelnen
Geräten (CAHV-R) deutlich darunter.** Begründungen, wo genannt: Strömungswächter-/Frostschutz
(implizit bei den meisten), ΔT-Fenster-Einhaltung (CAHV, explizit 3–5 K), Verflüssiger-/
Gaskühler-Mindestdurchsatz (R744-Geräte).

**Begründungen der Hersteller für die Rücklaufgrenze:**
- Unterkritische Kompressions-WP (R290, R32, R410A) und NH₃/H₂O-Absorption: **Schutz vor
  Ansprechen des Hochdruckwächters** bzw. Überschreiten der zulässigen Verflüssigungsdruck-/
  Sattdampfgrenze des Kältemittelkreises — bei Stiebel Eltron und Robur GAHP-A wörtlich so
  begründet, bei Daikin/NIBE über Fehlerabschaltung/Grenzwertnennung ohne ausführliche Begründung.
- CO₂-Geräte (Q-ton, Mayekawa Unimo): Eintrittstemperaturfenster aus der **Gaskühler-Auslegung**
  des transkritischen Kreises bzw. aus der Abgrenzung von Betriebsmodi (Top-up/Warm-up), nicht aus
  einer klassischen Verflüssiger-Drucküberschreitung — CO₂ gibt oberhalb des kritischen Punkts
  ohnehin gleitend Wärme ab.
- Gewerbliche EVI-Geräte (CAHV-P/-R): keine feste Grenze, sondern eine **ΔT-Bandregelung
  (3–5 K) des Wasservolumenstroms** — die „Grenze" ist dynamisch und volumenstromabhängig statt
  ein fester Temperaturwert.
- Großwärmepumpen (Friotherm, Viking): Begrenzung faktisch durch den **zulässigen
  Verdichter-Austrittsdruck/-temperatur** der Turbo- bzw. Kolbenverdichter, nicht durch eine
  wasserseitige Rücklaufgrenze; in der Praxis werden große Glide-Spannen gefahren.


## (c) Quellenliste [Q42]–[Q59]

- **[Q42]** Viessmann Deutschland GmbH: *Planungsanleitung Vitocal 250-A / 252-A Monoblock
  (2,6 bis 13,4 kW)*. Planungsunterlage, Fassung über Viessmann-Community archiviert, Download
  über Viessmann-Partnerforum. <https://community.viessmann.de/viessmann/attachments/viessmann/customers-heatpump-hybrid/109104/1/pa-viessmann-Vitocal-250-A-252-A-Monoblock-2,6-bis-13,4kW-2%20(1).pdf>
- **[Q43]** Viessmann Deutschland GmbH: *Vitocal 250-A PRO AWO-AC-AF 251.A40 — Datenblatt/
  Planungsanleitung*. Datenblatt (sek. Händlerspiegelung, da Originaldokument nicht vollständig
  textextrahierbar). <https://www.heizprofishop.at/Datenbl%C3%A4tter/Viessmann/250A/250%20pro%20datenblatt.pdf>
  ; ergänzend manualslib-Spiegelung der Planungsanleitung: <https://www.manualslib.de/manual/1501140/Viessmann-Vitocal-250-A-Pro-Awo-Ac-Af-251-A40.html>
- **[Q44]** Vaillant Deutschland GmbH & Co. KG: *aroTHERM plus — Installations- und
  Wartungsanleitung*. Installationsanleitung, Herstellerdownload. <https://www.vaillant.de/api/download/product/de/installationsanleitung_arotherm-plus_1483561.pdf>
- **[Q45]** Vaillant Group: *aroTHERM plus VWL 35/6 A bis VWL 125/6 A — Technisches Datenblatt*.
  Datenblatt, über Partnerportal gespiegelt. <https://cdn.hausfabrik.at/cache/documents/product/0010021625-23670.pdf>
- **[Q46]** Stiebel Eltron: *WPMW II / WPMS II — Wärmepumpen-Manager für Heizungs-/
  Warmwasserbereitung, Datenblatt* (enthält die Funktion „Rücklauftemperatur-MAX", auch in
  WPL-A-Anlagen verwendet). Datenblatt, Händlerspiegelung. <https://www.eibmarkt.com/isroot/eibmarkt/Files/Datenblatt/WPMW%20II.pdf>
- **[Q47]** Stiebel Eltron: *Technisches Datenblatt WPL-A 07 HK 230 Premium* (stellvertretend für
  die WPL-A-Plus/HT-Baureihe mit R290). Datenblatt, Herstellerdownload über Partnerportal.
  <https://www.solarwatt.de/canto/download/gfmt12ilih2pd7ct7kh5iav11p>
- **[Q48]** Mitsubishi Electric: *Ecodan R32 — Single Phase Spec Sheet* (Juli 2021) sowie
  *PUZ-WM50VHA — Datenblatt*. Datenblatt/Spec Sheet, Herstellerdokument über Partnerspiegelung.
  <https://static1.squarespace.com/static/5894a3b0be659481ff6f152c/t/60f031af7fedc2490c0ccfc1/1626354096029/Mitsubishi+Ecodan+Single+Phase+Spec+Sheet+Jul+2021.pdf>
  ; <https://www.energylabuk.com/wp-content/uploads/2023/12/Mitsubishi-Ecodan-PUZ-WM50VHA.pdf>
- **[Q49]** Daikin Europe N.V.: *Installer Reference Guide / Installation Manual — Daikin
  Altherma (Low Temperature Monobloc u. a. R32-Baureihen)*. Installationsanleitung,
  Herstellerdownload. <https://www.daikin.co.uk/content/dam/document-library/installation-manuals/heat/air-to-water-heat-pump-low-temperature/eblq-cv3/EBLQ05-07CV3-EDLQ05-07CV3_4PEN403578-1F_Installation%20Manual_English.pdf>
- **[Q50]** Mitsubishi Electric: *Data Book / Submittal — CAHV-P500YA-HPB(-BS) Hot Water Heat
  Pump*. Data Book/technisches Submittal, Herstellerdokument. <https://www.mitsubishitechinfo.ca/sites/default/files/SB_CAHV-P500YA-HPB_201507.pdf>
  ; <https://planetaklimata.com.ua/instr/Mitsubishi_Electric/Mitsubishi_Electric_CAHV-P500YA-HPB_Data_Book_Eng.pdf>
- **[Q51]** Mitsubishi Electric: *Technical Submittal — Ecodan CAHV-R450YA-HPB (R454C)*.
  Technisches Submittal, Herstellerdokument über Planungsportal archiviert. <https://docs.planning.org.uk/20250806/48/_LEWIS_DCAPR_128943/hti55v5xbbsqbye0.pdf>
  ; ergänzend (sek., Fachpresse): Cooling Post, *„Mitsubishi heat pump uses R454C"*, 2022.
  <https://www.coolingpost.com/products/mitsubishi-heat-pump-uses-r454c/>
- **[Q52]** NIBE Energy Systems: *F2040 — Technisches Handbuch / Installateurhandbuch*.
  Installateurhandbuch, Herstellerdokument über manualslib gespiegelt. <https://www.manualslib.de/manual/969185/Nibe-F2040.html>
- **[Q53]** Hoval Aktiengesellschaft: *Belaria twin I (20–30), Belaria twin IR (20–30) —
  Produktbeschreibung*. Datenblatt, Herstellerdownload. <https://cdn.hoval.com/belaria-twin-i-ir-at-d-22_hybris_original.pdf>
- **[Q54]** Mitsubishi Heavy Industries Thermal Systems: *Q-ton — High Performance CO₂ Heat
  Pump*. Produktbroschüre mit technischen Daten, Herstellerdownload (italienische Vertriebsseite).
  <https://www.mitsubishi-termal.it/wp-content/uploads/2023/08/qton-mhi-en.pdf>
- **[Q55]** Mayekawa Mfg. Co., Ltd.: *Unimo-AW — Air Source CO₂ Heat Pump, Spec Sheet*.
  Datenblatt, Herstellerdownload (Mayekawa Americas). <https://americas.mayekawa.com/mna/downloads/pdf/Heat%20Pumps/Unimo-AW.pdf>
- **[Q56]** Friotherm AG: *Unitop 43* und *Unitop 50* — Produktseiten; ergänzend *Uniturbo 50FY*
  Verdichter-Datenblatt und Anlagenbeispiel. Produktseite/Datenblatt, Herstellerdokument.
  <https://www.friotherm.com/products/unitop/unitop-43/> ; <https://www.friotherm.com/products/unitop/unitop-50/>
  ; <https://www.friotherm.com/wp-content/uploads/2017/12/turbo50fy_uk_g008.pdf>
- **[Q57]** Viking Heat Engines AS: *HeatBooster* — Produktseite. Firmenseite, Herstellerdokument.
  <http://www.vikingheatengines.com/heatbooster> ; ergänzend (sek., Fachbericht): IEA HPT
  Annex 58, *„HeatBooster — High-Temperature Heat Pumps"*. <https://heatpumpingtechnologies.org/content/uploads/sites/70/2022/07/heaten-heatbooster.pdf>
- **[Q58]** Robur S.p.A.: *GAHP-A — Installation, Use and Maintenance Manual* (USA/CSA-Variante
  mit Tinlet-EU-Angaben, sowie EU-Standardvariante). Installationsanleitung, Herstellerdokument.
  <https://www.robur.com/hubfs/doc/D-LBR369_H_24MCLSVI015_GAHP-A_USA_CSA_60Hz_Tinlet_EU.pdf>
  ; <https://www.robur.com/hubfs/doc/D-LBR548_AB_22MCLSVI158_GAHP_A_F02_A5_EN.pdf>
- **[Q59]** Viessmann: *Vitocal 300-G — technische Daten* (sek., Händlerangaben, da
  Originaldatenblatt nicht vollständig auswertbar). <https://heizung-billiger.de/420989-viessmann-vitocal-sole-wasser-warmepumpe-300-g-typ-bwc-301c16-r410a-1027kw-bei-b0-w35-viessmann-z019533.html>
  ; dazu NIBE Energy Systems: *S1155/S1255 — Technisches Handbuch Sole/Wasser-Wärmepumpe*
  (Herstellerdokument, über Partnerportal gespiegelt). <https://www.schmid-energy.ch/app/uploads/2023/04/Technisches-Handbuch-Sole-Wasser-WP-DE.pdf>
