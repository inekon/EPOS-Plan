# Recherche Pufferspeicher-Auslegung, vierte Runde — Internationale Literatur zur Auslegung und Optimierung von Brauchwasserspeichern (01.10.2026)

**Zweck.** Vierte Runde der Recherche zur Pufferspeicher-Auslegung, Fortsetzung von
[`2026-10-01_Recherche_Pufferspeicherauslegung.md`](2026-10-01_Recherche_Pufferspeicherauslegung.md) (Runde 1),
[`2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md`](2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md) (Runde 2,
Brauchwasser- und Kombizone über die Zapfprofil-Auslegung) und
[`2026-10-01_Recherche_Normen_Speichermodell.md`](2026-10-01_Recherche_Normen_Speichermodell.md) (Runde 3).
Auftrag des Anwenders (01.10.2026): „Gibt es international noch Literatur und Informationen zur
Auslegung und Optimierung von Brauchwasser-Pufferspeichern? — Nehme auf.“ Dieses Papier ordnet
die internationale Literatur (Schweiz, Österreich, Großbritannien, USA, IEA) gegen den Stand von
EPOS-Plan und sagt, was davon in Konzept und Bau einfließt.

**Grenzen.** Normen und Handbücher sind zitiert, nicht abgeschrieben; die ASHRAE-, ASPE- und
CIBSE-Werke lagen nicht im Volltext vor und sind über Fachbeiträge (Sekundärquellen) belegt. Angaben
in Gallonen und °F sind umgerechnet (1 gal = 3,785 l; 120 °F = 49 °C, 140–160 °F = 60–71 °C). Keine
Produktdaten; Rechenbeispiele fiktiv.


## 1 Quellenlage

| Quelle | Art | Zugang | Ertrag |
|---|---|---|---|
| SIA 385/2:2025 *Anlagen für Trinkwarmwasser in Gebäuden — Warmwasserbedarf, Gesamtanforderungen und Auslegung* (68 Seiten, gültig ab 01.02.2025) | Norm (Schweiz), Umsetzung von EN 12831-3, EN 15316-3 und EN 15316-5 | nur Inhaltsverzeichnis und Vorwort frei | zweistufiges Verfahren (Vorstudie, Detailauslegung), Speichervolumen und Leistung in 4.2, normativer Anhang A Bedarf, B Speicherverluste, F stündliche Verteilung; 35 l je Person und Tag als Bedarfsansatz (IEA zitiert) |
| ÖNORM EN 12828:2023 (Österreich) | Norm | Vortrag der WKO | Leistung für Trinkwassererwärmung nach EN 12831-3, Alternativverfahren zulässig |
| BS EN 12831-3:2017; CIBSE Domestic Heating Design Guide 2026; CIBSE Guide G (2014); CIBSE Journal Modul 179 | Norm/Leitfaden (Großbritannien) | Sekundär | Speicher aus Verbrauch einer Bezugsperiode, die von 10 °C in rund zwei Stunden aufheizbar ist; Leitfaden 2026 stellt auf EN 12831-3 um |
| ASHRAE Handbook HVAC Applications, Kapitel 51 *Service Water Heating* (2019/2023) | Handbuch (USA) | Sekundär (Consulting-Specifying Engineer 2020) | Bemessung je Person oder je Zapfstelle (Tabelle 10), Spitzenstundenbedarf × Bedarfsfaktor (Hotel 0,25 … Schule 0,40) × Speicherfaktor (Krankenhaus 0,60 … Büro 2,00), stundenweise Zeitbewertung (Speicher − Bedarf + Nachheizung) |
| ASPE *Domestic Water Heating Design Manual*, 2. Auflage | Handbuch (USA) | Sekundär | Methodik je Gebäudetyp, Zirkulation |
| Ecotope, *Ecosizer — Central Heat Pump Water Heating Sizing Tool Manual* (33 Seiten, Dezember 2020) | Werkzeughandbuch (USA), Primärquelle | PDF frei | vollständiges Auslegungsverfahren für zentrale Wärmepumpen-Warmwasseranlagen in Mehrfamilienhäusern (Abschnitt 3.2) |
| Energy Trust of Oregon / Ecotope, *Design Guide for Central Heat Pump Water Heaters* (15 Seiten, 2023) | Leitfaden (USA) | PDF frei | Systemkonfigurationen (Single-/Multi-Pass, Swing Tank, Paralleltank), Schichtung, Aufstellung, Nachrüstung |
| ACEEE Summer Study 2024, *Eliminating the Swing Tank …* | Fachbeitrag (USA) | PDF frei | Zirkulationsrückführung, Lastverschiebung, Ecosizer als Standardwerkzeug |
| IEA HPT Annex 46 *Domestic Hot Water Heat Pumps*: Task-1-Bericht (68 Seiten) und *Legionella and Heat Pump Water Heaters* (52 Seiten), 2020 | Forschungsbericht (international) | PDF frei | Bedarf je Person, Speichergrößen, Überdimensionierung, Schichtung, Hygiene-Befunde |
| Hot Water Association, *Design Guide — Stored Hot Water Solutions in Heat Networks* (29 Seiten, 2018) | Leitfaden (Großbritannien) | PDF frei | Bedarf je Person in Großanlagen, Gleichzeitigkeit, Speicher gegen Durchlauferhitzer, Zapfmengen |
| Forschung: variable Speichervolumina (Energies 2020), Schichtung mit „chimney heaters“ (2024), modellprädiktive Regelung geschichteter Speicher (arXiv 2023/2024), IEA SHC Task 42 Kompaktspeicher | Fachaufsätze | Zusammenfassungen | Schichtungseffizienz, kleinere Volumina bei besserer Schichtung, Lastverschiebung |


## 2 Befund nach Themen

### 2.1 Bedarf je Person und Tag

| Quelle | Wert | Bezug |
|---|---|---|
| IEA Annex 46 (Schweiz) | 45–50 l zentral, 35 l dezentral; Feldmessung Mehrfamilienhaus 36–44 l, Zehn Haushalte ein Jahr 33,6 l | 60 °C; SIA 385/2 nennt 35 l |
| Hot Water Association (Großbritannien) | 28 l im Mittel über 100 Personen, 21 l Sommer, 32 l Februar | 60 °C, Heizwerk-Messung |
| ASHRAE (USA) | „niedrig“ 20 gal = 76 l, „mittel“ 49 gal = 185 l | 49 °C (≈ 59 bzw. 144 l bei 60 °C) |
| Ecosizer (Seattle, 3 Gebäude mit Sparbrausen) | 98. Perzentil 25 gal = 95 l; nach Wohnungsgröße 1,74 (1 Zimmer) bis 4,23 Personen (4 Zimmer) | 49 °C (≈ 74 l bei 60 °C); ASHRAE „mittel“ gilt als überschätzt |
| EN 15450 Anhang E (Runde 3) | Einzelperson 36 l, Familie 100 l, Familie mit Baden 200 l | 60 °C |

**Lesart:** Die Spanne 28 bis 50 l je Person bei 60 °C ist international deckungsgleich mit den Zapf-
Nutzungsarten des EPOS-Katalogs (Messungen, Runde 2); nur die ASHRAE-Mittelwerte liegen deutlich
höher und gelten auch in den USA als überholt. Für die **Nutzungsprofile** (Runde 2, 4.2) ist das eine
Gegenprobe, keine neue Vorgabe.

### 2.2 Auslegungsverfahren

- **Summenlinie (EN 12831-3, SIA 385/2, CIBSE 2026):** Bedarfs- und Angebotskurve über den Tag, die
  größte Differenz ist der Speicher — der Weg, den `TwwSpeicherauslegung` rechnet (Lindley-D_max über
  zwei Wochen). SIA 385/2 ergänzt eine Vorstudie zur Optimierung (ohne/mit warmgehaltenen Leitungen)
  und normiert Speicher- und Leitungsverluste (Anhänge B, D, E).
- **ASHRAE-Faktorenmethode:** möglicher Bedarf aus Zapfstellen oder Personen × Bedarfsfaktor =
  Spitzenbedarf; × Speicherfaktor = Speicher; Zeitbewertung Stunde für Stunde: verfügbar = vorher −
  Bedarf + Nachheizung, begrenzt auf den Speicherinhalt. Faktoren je Gebäudetyp (Hotel 0,25, Schule
  0,40; Speicherfaktor Krankenhaus 0,60, Büro 2,00) — eine **tabellarische Nutzungsart-Logik**, die
  unserem Nutzungsprofil entspricht.
- **Ecosizer-Methode** (Weiterentwicklung der „More Accurate Method“ aus ASHRAE 2015, S. 50.15 f.):
  1. Erzeugerleistung aus Tagesbedarf ÷ maximale Verdichterlaufzeit h_max (Vorgabe **16 h**, Spanne
     16–20 h: viel Speicher, wenig Leistung).
  2. Speicher = größtes **Laufvolumen** eines Spitzenereignisses, das Integral aus Zapfung minus
     Erzeugung ab Beginn der Spitze (Lastprofil 98. Perzentil, zwei Spitzen morgens und abends).
  3. Gesamtvolumen = Laufvolumen ÷ (1 − Aquastat-Anteil), weil der Fühler erst nach einem Teil der
     Entnahme einschaltet; Speicherwirkungsgrad für Durchmischung.
  4. Umrechnung Liefer- auf Speichertemperatur mit (T_Liefer − T_KW)/(T_Speicher − T_KW).
  5. **Kurve Speicher gegen Leistung**, indem h_max von 24 h bis zum Minimum variiert wird —
     das Planungsergebnis ist ein Band, kein Punkt. Speicherverhältnis 1 bis 12, Vorgabe 5,2.
  Zirkulationsverlust je Wohnung: Median 66 W, 75. Perzentil 175 W; Heizstab im Swing Tank auf das
  75. Perzentil (Faktor 1,75) auslegen.
- **Heat-Networks-Leitfaden:** Speicher mindestens für die **Spitze von 10 Minuten**, Wiederaufheizung in
  25–30 min; Zapfmengen Bad 60 l, Dusche 30–55 l; Wohnungstabelle 75 l / 1 kW (1 Person) bis 125 l /
  1,5 kW (2 Personen); Gleichzeitigkeit über 100 Wohnungen sehr klein (8 kW zentral für 100 Wohnungen
  im Tagesmittel gegenüber 158 kW Durchlauf).
- **CIBSE/CIPHE:** Speicher = Verbrauch einer Bezugsperiode, in rund zwei Stunden von 10 °C aufheizbar.

### 2.3 Systemkonzepte für Wärmepumpen-Warmwasser (USA)

- **Single-Pass** (ein Durchgang, 39–56 K Hub, geschichteter Speicher von oben, CO₂-Geräte) gegen
  **Multi-Pass** (Umwälzung mit 5–6 K je Durchgang, weniger Schichtung). Single-Pass braucht weniger
  Speicher und erreicht 57–71 °C für die Hygiene; öffentliche Werkzeuge rechnen nur Single-Pass.
- **Swing Tank:** ungeschichteter Nachheizbehälter in Reihe, der die Zirkulationsrückläufe aufnimmt und
  den geschichteten Primärspeicher vor Durchmischung schützt; Primärspeicher 60–71 °C, Mischventil auf
  49 °C; Heizstab als Rückfall bei langer Schwachlast. Alternative: Paralleltank oder Rückführung in den
  Primärspeicher (ACEEE 2024: kein Industriestandard).
- **Grundsatz:** „mehr Speicher, weniger Leistung“ — Verdichter 16–20 h je Tag, dadurch günstigste Anlage
  und Lastverschiebungsfähigkeit; Gegenteil der Gas-Logik „schnelle Nachheizung, kleiner Speicher“.

### 2.4 Hygiene (IEA Annex 46, Legionella-Bericht)

- Wachstum 25–45 °C, Verdopplung in 6 h bei 35–40 °C; Abtötung um 90 % in 100 min bei 50 °C, in 2 min
  bei 60 °C; D-Wert 2 h bei 50 °C.
- Sichere Anlagen: **> 52 °C am Speicheraustritt, > 50 °C an der Zapfstelle**, dafür meist ≥ 55 °C
  Vorlauf, bei großen oder verzweigten Netzen 60 °C. **Es gibt keine Volumengrenze**, oberhalb derer das
  Risiko steigt, solange der Inhalt > 50 °C bleibt.
- **Periodisches Aufheizen** auf 60–70 °C (täglich oder wöchentlich) **erhöht** nach Feldstudien das
  Risiko eher (Mathys 2008; 50 % positiv in Gebäuden mit wöchentlicher 70-°C-Fahrt) — die
  „Legionellenschaltung“ ist kein Ersatz für dauerhaft ausreichende Temperatur.
- Die 3-Liter-Regel (Leitungsinhalt bis zur Zapfstelle, W 551) gilt international als Grenze für
  Kleinanlagen ohne weitere Maßnahmen; dezentrale Wohnungsstationen an Niedertemperaturnetzen
  (55 °C) sind in Dänemark als sicher nachgewiesen.
- IEA Task-1-Bericht: Speicher in der Praxis **oft überdimensioniert**; Bereitschaftsverluste von
  Wärmepumpen-Warmwasserbereitern wegen zu großer Speicher häufig höher als bei Elektrospeichern;
  Legionellenprävention in der Schweiz typisch einmal täglich 60 °C für eine Stunde.

### 2.5 Schichtung und Optimierung (Forschung)

- Schichtung ist die wichtigste Stellgröße: schnellere Warmwasserlieferung, weniger Energie, höhere
  Erträge aus Solar und Wärmepumpe; Einströmgeometrie („chimney heaters“, Schichtlader) untersucht an
  38–75 l.
- Speicher mit **variablem Wasservolumen** erreichen nach einer Studie die halbe Größe geschichteter
  Speicher bei 33 % geringerer Wärmepumpenleistung (8 % mehr Exergie) — Sonderbauart, kein Katalogstand.
- Modellprädiktive Regelung geschichteter Elektrospeicher für Lastverschiebung (arXiv 2023/2024):
  Ergebnis ist Regelung, nicht Volumen.


## 3 Vergleich mit EPOS-Plan und Runde 2

| Thema | International | EPOS-Plan heute / Runde 2 | Folge |
|---|---|---|---|
| Bedarf je Person | 28–50 l bei 60 °C (IEA, UK), Ecosizer 74 l-Äquivalent | Zapf-Nutzungsarten aus Messungen | Gegenprobe im Nutzungsprofil (V46), keine Vorgabe |
| Verfahren | Summenlinie (EU), Faktoren (ASHRAE), Laufvolumen mit Laufzeitbegrenzung (Ecosizer) | Summenlinie über zwei Wochen (Lindley) | Lindley bleibt; Ecosizer ergänzt die **Leistungsseite**: Speicher-gegen-Leistung-Kurve mit Verdichterlaufzeit (V47) |
| Aquastat-Anteil, Speicherwirkungsgrad | explizite Eingaben | Nutzanteil 0,8 und Zuschlag 0,15 im freien Paketteil | gleichwertig; Herkunft im Hilfetext nennen |
| Zirkulation | 66 W median / 175 W 75. Perzentil je Wohnung; 30–40 % des Tagesbedarfs (Runde 3) | V40 Zirkulationszuschlag | Vorgabe je Wohnung als zweiter Weg (V49) |
| Hygiene | > 52 °C Austritt, ≥ 55 °C Vorlauf, 60 °C Großanlagen; periodisches Aufheizen kritisch; keine Volumengrenze | W 551, UBA, DVGW (Runde 2, V25) | Hinweistext ergänzen (V48); W 551 und Trinkwasserverordnung bleiben Maßstab in Deutschland |
| Speicher gegen Leistung bei Wärmepumpen | 16–20 h Laufzeit, großer Speicher | Zapfprofil rechnet Volumen; Leistung aus `TwwSpeicherauslegung` | V47 |
| Überdimensionierung | IEA: häufig, höhere Bereitschaftsverluste | Nutzen-Aufwand-Zeile V16/V34 | Hinweiscode, wenn Brauchwasserzone > 2 × Tagesbedarf (V46) |


## 4 Vorschläge (Fortsetzung V1–V45) und Entscheide

| Nr. | Vorschlag | Nutzen | Aufwand | Empfehlung |
|---|---|---|---|---|
| V46 | **Gegenprobe Bedarf je Person** im Ergebnis der Brauchwasserzone: Tagesbedarf ÷ Personen gegen das Band 28–50 l (60 °C) mit Herkunft IEA/UK; Hinweiscode, wenn das Zonenvolumen mehr als das Doppelte des Tagesbedarfs beträgt (Überdimensionierung) | ordnet das Zapfprofil-Ergebnis ein | klein | **A, P1** |
| V47 | **Kurve Speicher gegen Leistung** für die Brauchwasserzone bei Wärmepumpen (Ecosizer-Weg): Laufzeitbegrenzung 16–20 h, Laufvolumen des Spitzenereignisses aus der Zapfreihe, Aquastat-Anteil aus den Schwellen | zeigt dem Planer den Tausch Speicher gegen Leistung | mittel (eigene Rechenfunktion auf der Zapfreihe) | **B, P2/P3** (E-P32) |
| V48 | **Hygiene-Hinweistext** erweitern: > 52 °C am Austritt, ≥ 55 °C Vorlauf, 60 °C bei Großanlagen; periodisches Aufheizen ersetzt keine dauerhafte Temperatur; keine Volumengrenze bei > 50 °C — als Information neben W 551 | fachlich aktuell, hält von Legionellenschaltungen als Alibi ab | klein | **A, P1** |
| V49 | **Zirkulationsverlust je Wohnung** als zweiter Weg für V40: Vorgabe 100 W je Wohneinheit (zwischen Median 66 und 75. Perzentil 175 W), Expertenfeld | für Projekte ohne Zirkulationsdaten | klein | **B, P1** |

| Nr. | Frage | Empfehlung |
|---|---|---|
| E-P31 | Internationale Werte (Runde 4) nur als Gegenprobe und Beispielhilfe, nie als Vorgabewert — Vorgaben bleiben EN 12831-3/VDI/W 551? | ja |
| E-P32 | Speicher-gegen-Leistung-Kurve (V47) als Folgeauftrag nach P2, nicht in P1? | ja |


## 5 Folgen für den Bauplan P1

- **W1:** Vorgabeschlüssel `Pufferauslegung.Brauchwasser.Bedarf_Min_l_P` = 28, `…Bedarf_Max_l_P` = 50,
  `Pufferauslegung.Brauchwasser.Ueberdimensionierung_Faktor` = 2,0, `Pufferauslegung.Zirkulation.W_je_WE` = 100.
- **W2:** Kennzahl Tagesbedarf je Person (60 °C-Äquivalent) im Ergebnis der Brauchwasserzone mit
  Hinweiscodes unter/über dem Band und bei Überdimensionierung; Hygiene-Hinweistexte nach V48; V49 als
  zweiter Zirkulationsweg.
- **W4:** Handrechnung: 40 Personen, Tagesbedarf 1 600 l bei 60 °C → 40 l je Person (im Band); Zone 3 500 l
  → Hinweiscode Überdimensionierung (> 3 200 l); Zirkulation 20 Wohnungen × 100 W = 2 kW → 48 kWh/d.
- Keine Änderung an `TwwSpeicherauslegung.cs` und am Zapfprofil (Referenzprojekt 1045, Einfrierregel).


## 6 Quellen (abgerufen 01.10.2026)

- SIA 385/2:2025 — https://shop.sia.ch/collection%20des%20normes/architecte/385-2_2025_d/D/Product/ (Inhaltsverzeichnis und Vorwort: https://shop.sia.ch/d93384e3-1dad-4fc1-a14f-8bad29bf782a/D/DownloadAnhang)
- ÖNORM EN 12828:2023, Vortrag WKO Steiermark — https://www.wko.at/stmk/gewerbe-handwerk/sanitaer-heizung-lueftung/praesentation-oenorm-h-12828-2023-01-instatllateurfachtag-20.pdf
- BS EN 12831-3:2017 — https://knowledge.bsigroup.com/products/energy-performance-of-buildings-method-for-calculation-of-the-design-heat-load-domestic-hot-water-systems-heat-load-and-characterisation-of-needs-module-m8-2-m8-3
- CIBSE Domestic Heating Design Guide 2026 — https://www.cibse.org/knowledge-research/knowledge-portal/cibse-domestic-heating-design-guide-2026/ ; CIBSE Journal Modul 179 — https://www.cibsejournal.com/cpd/modules/2021-05-dhw/
- ASHRAE Handbook HVAC Applications, Kapitel 51 — https://handbook.ashrae.org/Handbooks/A23/IP/A23_Ch51/a23_ch51_ip.aspx ; Sekundär: Consulting-Specifying Engineer, *How to select a commercial water heater* — https://www.csemag.com/articles/how-to-select-a-commercial-water-heater/
- ASPE Domestic Water Heating Design Manual — https://aspe.org/product/domestic-water-heating-design-manual-2nd-edition/
- Ecotope, Ecosizer Manual (2020) — https://ecotope-publications-database.ecotope.com/2020_005_ecosizer-chpwh-sizing-tool-manual12232020.pdf ; Werkzeug — https://ecosizer.ecotope.com/sizer/
- Energy Trust of Oregon, Design Guide for Central Heat Pump Water Heaters (2023) — https://www.energytrust.org/wp-content/uploads/2023/04/New-Buildings_Design-Guide-for-Central-Heat-Pump-Water-Heaters.pdf ; PNNL — https://basc.pnnl.gov/resource-guides/central-heat-pump-water-heaters-multifamily-buildings
- ACEEE Summer Study 2024, Eliminating the Swing Tank — https://aceee.org/sites/default/files/proceedings/ssb24/pdfs/Eliminating%20the%20Swing%20Tank%20and%20Other%20Design%20Considerations%20in%20Large-Capacity%20CO2%20Heat%20Pump%20Water%20Heating.pdf
- IEA HPT Annex 46, Task 1 General Report — https://heatpumpingtechnologies.org/content/uploads/sites/53/2020/10/hpt-an46-02-task-1-general-report-hpt-annex-46.pdf ; Legionella and Heat Pump Water Heaters — https://heatpumpingtechnologies.org/content/uploads/sites/53/2020/10/hpt-an46-03-task-1-legionella-and-heat-pumps.pdf
- Hot Water Association, Design Guide Stored Hot Water Solutions in Heat Networks (2018) — https://www.hotwater.org.uk/uploads/5B053A7597A5F.pdf
- Forschung: Energies 13(24):6576 (2020) — https://doi.org/10.3390/en13246576 ; Chimney type heaters (2024) — https://www.sciencedirect.com/science/article/pii/S2666202724003823 ; MPC stratified water heaters — https://arxiv.org/pdf/2312.04102 und https://arxiv.org/pdf/2408.02868
