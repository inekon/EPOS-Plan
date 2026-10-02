Das Gerät (`~/mnt/Wärmespeicher`) war nur zeitweise erreichbar; ich konnte DIN V 18599‑10, VDI 6002 Bl. 1 und Bl. 2 vollständig als Text extrahieren und auswerten. Die Logalux‑Planungsunterlage 2018 ließ sich lokal nicht mehr extrahieren – ich habe stattdessen die inhaltsgleiche Buderus‑Ausgabe 6 720 818 349 (2015/10) online geladen und ausgewertet. Hinweis: ein Zwischenordner `~/mnt/Wärmespeicher/_tww_tmp/` (4 PNG‑Seitenrenderings) konnte wegen der Trennung evtl. nicht mehr gelöscht werden – bitte ggf. entfernen.

---

# TWW-Zapfprofile: Normative und regelwerksbasierte Grundlagen (D/A/CH)

**Zweck:** Grundlagenbericht für die Erzeugung von TWW-Zapfprofilen für (a) 8760‑h‑Jahressimulation und (b) Speicher-/Erzeugerauslegung.
**Wichtiger Hinweis zur Verwendung:** Alle Zahlenwerte aus DIN‑, VDI‑ und SIA‑Regelwerken unten sind **urheberrechtlich geschützt**. Sie dürfen intern als Rechengrundlage/Parametrierung verwendet werden, aber **nicht 1:1 als Tabelle in Handbuch, Marketing oder Programmoberfläche abgedruckt** werden. Details in Kapitel 11.

---

## 0. Kernbefunde vorab

1. **Es gibt in Deutschland keine einzige Quelle, die beides liefert** (breite Nutzungsartenabdeckung + Stunden-/Minutenprofil). Die Arbeitsteilung ist:
   - **Menge/Jahresbilanz, viele Nutzungsarten:** DIN V 18599‑10 Tabelle 7 (22 Nutzungskategorien) – aber **kein Tagesgang**, nur ein Gleichzeitigkeitsindikator n_SP ∈ {1, 2}.
   - **Tagesgang, wenige Nutzungsarten:** VDI 6002 Bl. 1 (Wohngebäude) und Bl. 2 (Studentenwohnheim, Senioren-/Pflegeheim, Krankenhaus, Hallenbad, Campingplatz) – Stunden-, Tages-, Wochen-, Jahresprofile, aber überwiegend als **Diagramme**, nur ein Profil (Studentenwohnheim) als Zahlentabelle.
   - **Auslegungsspitze Wohngebäude:** DIN 4708 (N/N_L) bzw. DIN EN 12831‑3 (Summenlinienverfahren).
   - **Rohrnetz-Spitzendurchfluss/Diversität:** DIN 1988‑300 (geschlossene Formeln, 7 Gebäudetypen).
2. **Überraschung bei DIN V 18599‑10:2018‑09:** Die 41 „ausführlichen Nutzungsprofile" (Anhang A, Tab. A.1–A.41) enthalten **keinen TWW‑Wert**. Tabelle 5 (Spalten 1–30) hat keine TWW‑Spalte. Der TWW‑Nutzenergiebedarf steht **ausschließlich in Tabelle 7**, die eine **eigene, gröbere Nutzungsgliederung** mit 22 Zeilen verwendet und über eine „Bezugsfläche" (Hauptnutzung nach Tab. 5) an die Zonierung anknüpft. Für ein Tool heißt das: **Mapping-Tabelle Nutzungsprofil → TWW‑Kategorie ist selbst zu bauen.**
3. **Frei/offen verwendbar** sind praktisch nur: VO (EU) 814/2013 und 812/2013 (Amtsblatt der EU), IEA‑SHC‑Task‑26‑Profile / DHWcalc (Uni Kassel/Marburg), BBSR‑Online 17/2017. Alles andere ist lizenzpflichtig.

---

## 1. DIN V 18599‑10 – Nutzungsrandbedingungen (Ausgabe 2018‑09)

*(Quelle: lokal ausgewertetes PDF „DIN V 18599-10 - DIN.pdf", Beuth-Standards-Collection Stand 2018‑11)*

### 1.1 Struktur

| Element | Inhalt |
|---|---|
| Tabelle 4 | Nutzungsrandbedingungen **Wohngebäude** (inkl. TWW‑Formel) |
| Tabelle 5 | Nutzungsrandbedingungen **Nichtwohngebäude**, **41 Nutzungen** (lfd. Nr. 1–41, davon 22.1/22.2/22.3), Spalten 1–30. **Keine TWW‑Spalte.** |
| Tabelle 6 | Für alle NWG-Nutzungen gemeinsam: F_V = 0,9; k₂ = 0,9; Δθ_EMS = 0; f_adapt = 1 |
| **Tabelle 7** | **Richtwerte des Nutzenergiebedarfs Trinkwarmwasser für NWG** – die zentrale TWW‑Quelle |
| Tabelle 8 | Tage je Monat d_mth |
| Anhang A | Tab. A.1–A.41: ausführliche Nutzungsprofile mit Bandbreiten (tief/mittel/hoch) – **ohne TWW** |
| Anhang C | Formblatt zur Dokumentation eines eigenen Nutzungsprofils |
| Anhang D | Beispiel „Fertigungshalle – zweischichtig"; D.8 Trinkwarmwasser: für Industriebetriebe Waschen/Duschen **1,5 kWh je Beschäftigten und Tag** bzw. **90 Wh/(m²·d)** |

### 1.2 Wohngebäude (Tabelle 4)

- **Nutzenergiebedarf TWW:** `Q_w,b = max[16,5 − (A_NGF,WE,m · 0,05) ; 8,5]` in **kWh/(m²·a)**, bezogen auf die **NGF einer mittleren Wohneinheit**; gilt sinngemäß für Wohnnutzung in NWG (EFH/MFH).
- Monatswert: `Q_w,b = q_w,b/365 · d_w,mth · A_NGF`, mit d_w,mth = Monatslänge (Tab. 8), d. h. **365 Nutzungstage/a, keine Wochen-/Jahresgangmodulation**.
- Nutzungszeit 0:00–24:00, Heizbetriebszeit 6:00–23:00.
- **Wichtig:** Bis zur Ausgabe 2016 stand hier ein Pauschalwert von **12,5 kWh/(m²·a)** (analog DIN V 4701‑10). Die flächenabhängige Formel 2018 senkt den Wert für große Wohnungen bis auf 8,5 kWh/(m²·a). Hintergrund ist die BBSR‑Studie (s. 1.5).
- Zahlenbeispiel (eigene Rechnung): A_NGF,WE = 70 m² → 16,5 − 3,5 = **13,0 kWh/(m²·a)**; 100 m² → **11,5**; 160 m² und mehr → **8,5**.

### 1.3 Tabelle 7 – TWW‑Nutzenergiebedarf NWG (Werte, sinngemäß wiedergegeben)

Bezugsfläche ist jeweils die **NGF der genannten Hauptnutzung**. n_SP = Anzahl der Spitzenzapfungen/Tag.

| Nutzung | nutzungsbezogen | flächenbezogen Wh/(m²·d) | Bezugsfläche | n_SP |
|---|---|---|---|---|
| Bürogebäude | 0,4 kWh/(Person·d) | 30 | Bürofläche | 1 |
| Bettenzimmer/Krankenhaus | 6 kWh/(Bett·d) | 400 | Bettenzimmer | 1 |
| Schule ohne Duschen | 0,4 kWh/(Person·d) | 130 | Klassenräume | 1 |
| Schule mit Duschen | 1,5 kWh/(Person·d) | 500 | Klassenräume | 2 |
| Einzelhandel/Kaufhaus | 1 kWh/(Beschäftigter·d) | 10 | Verkaufsfläche | 1 |
| Werkstatt/Industriebetrieb (Waschen, Duschen) | 1,8 kWh/(Beschäftigter·d) | 90 | Werkstatt-/Betriebsfläche | 2 |
| Hotel einfach | 1,9 kWh/(Bett·d) | 240 | Hotelzimmer | 2 |
| Hotel mittel | 3,5 kWh/(Bett·d) | 350 | Hotelzimmer | 2 |
| Hotel Luxus | 5,5 kWh/(Bett·d) | 460 | Hotelzimmer | 2 |
| Restaurant/Gaststätte | 1,1 kWh/(Sitzplatz·d) | 920 | Gastraum | 1 |
| Heim | 2,3 kWh/(Person·d) | 150 | Zimmer | 2 |
| Kaserne | 1,8 kWh/(Person·d) | 180 | Zimmer | 2 |
| Sportanlage mit Dusche | 1,8 kWh/(Person·d) | – | – | 1 |
| Gewerbeküche, Kantine ᴾ | 0,4 kWh/Menü | – | – | 1 |
| Bäckerei ᴾ | 5 kWh/(Beschäftigter·d) | – | – | 1 |
| Friseur ᴾ | 6 kWh/(Beschäftigter·d) | – | – | 1 |
| Fleischerei mit Produktion ᴾ | 18 kWh/(Beschäftigter·d) | – | – | 1 |
| Wäscherei ᴾ | 20 kWh/100 kg Wäsche | – | – | 1 |
| Brauerei ᴾ | 15 kWh/100 l Bier | – | – | 1 |
| Molkerei ᴾ | 10 kWh/100 l Milch | – | – | 1 |
| Saunabereich | 2,8 kWh/(Person·d) | 235 | Saunabereich | 2 |
| Labor | 0,4 kWh/(Person·d) | 30 | Laborfläche | 1 |
| Fitnessraum | 1,5 kWh/(Person·d) | 300 | Fitnessraum | 2 |

ᴾ = Prozesswärme; Einbeziehung in die Bilanz nach DIN V 18599‑8 ist zu dokumentieren.

**Rechenvorschrift (Fußnote a):** `Q_w,b = q_w,b,d · d_mth/365 · d_nutz · Bezugsgröße` [kWh/Monat].
**Bagatellgrenze (Fußnote b):** < 0,2 kWh je Person bzw. Beschäftigtem und Tag darf vernachlässigt werden – „entspricht etwa 5 l je Person und Tag bei 45 °C Warmwassertemperatur".

**Daraus folgt die implizite Referenztemperatur:** 5 l · 35 K · 1,163 Wh/(l·K) ≈ 204 Wh. Die 18599‑Werte sind also auf **ΔT = 35 K (45 °C Zapf-, 10 °C Kaltwassertemperatur)** bezogen. *(Eigene Herleitung; in der Norm nicht explizit als Bezugstemperatur ausgewiesen.)*
Umrechnung für ein Tool (eigene Rechnung): l@60 °C = kWh · 1000 / (50 · 1,163) = kWh · 17,2; l@45 °C = kWh · 24,6.
Beispiele: Bürogebäude 0,4 kWh/(P·d) ≈ **9,8 l/(P·d) @45 °C** ≈ 6,9 l @60 °C. Hotel Luxus 5,5 kWh ≈ 135 l @45 °C ≈ 95 l @60 °C.

### 1.4 Nutzungszeiten aus Tabelle 5 (relevant für Profilbildung)

Nutzungsbeginn/-ende, tägliche Nutzungsstunden t_nutz,d, jährliche Nutzungstage d_nutz,a:

| Nr. | Nutzung | von–bis | h/d | d/a |
|---|---|---|---|---|
| 1–5 | Einzel-/Gruppen-/Großraumbüro, Besprechung, Schalterhalle | 07:00–18:00 | 11 | 250 |
| 6, 7 | Einzelhandel/Kaufhaus (auch Lebensmittel) | 08:00–20:00 | 12 | 300 |
| 8 | Klassenzimmer, Gruppenraum (Kindergarten) | 08:00–15:00 | 7 | 200 |
| 9 | Hörsaal, Auditorium | 08:00–18:00 | 10 | 150 |
| 10 | Bettenzimmer | 00:00–24:00 | 24 | 365 |
| 11 | Hotelzimmer | 21:00–08:00 | 11 | 365 |
| 12 | Kantine | 08:00–15:00 | 7 | 250 |
| 13 | Restaurant | 10:00–00:00 | 14 | 300 |
| 14, 15 | Küchen NWG / Küche Vorbereitung, Lager | 10:00–23:00 | 13 | 300 |
| 16–20 | WC/Sanitär, sonst. Aufenthalt, Nebenflächen, Verkehr, Lager | 07:00–18:00 | 11 | 250 |
| 21 | Rechenzentrum | 00:00–24:00 | 24 | 365 |
| 22.1–22.3 | Gewerbliche/industrielle Hallen (schwer/mittel/leicht) | 07:00–16:00 | 9 | 230 |
| 23, 24 | Zuschauerbereich / Foyer (Theater) | 19:00–23:00 | 4 | 250 |
| 25 | Bühne | 13:00–23:00 | 10 | 250 |
| 26 | Messe/Kongress | 09:00–18:00 | 9 | 150 |
| 27 | Ausstellung/Museum | 10:00–18:00 | 8 | 250 |
| 28–30 | Bibliothek (Lesesaal / Freihand / Magazin) | 08:00–20:00 | 12 | 300 |
| 31 | Turnhalle | 08:00–23:00 | 15 | 250 |
| 32 | Parkhaus (Büro/Privat) | 07:00–18:00 | 11 | 250 |
| 33 | Parkhaus (öffentlich) | 09:00–00:00 | 15 | 365 |
| 34 | Saunabereich | 10:00–22:00 | 12 | 365 |
| 35 | Fitnessraum | 08:00–23:00 | 15 | 365 |
| 36, 37 | Labor / Untersuchung + Behandlung | 07:00–18:00 | 11 | 250 |
| 38, 39 | Spezialpflegebereiche / Flure allg. Pflegebereich | 00:00–24:00 | 24 | 365 |
| 40 | Arztpraxen, therapeutische Praxen | 08:00–18:00 | 10 | 250 |
| 41 | Lagerhallen, Logistikhallen | 00:00–24:00 | 24 | 365 |

**Verwendbarkeit für Profile:** Diese Nutzungszeiten sind ein brauchbares Gerüst, um TWW‑Tagesprofile auf das Nutzungsfenster zu legen (Rechteck oder Doppelspitze je nach n_SP). Sie sind aber **nicht als TWW‑Zeitverteilung normiert** – die Zuordnung ist eine Modellannahme des Tools, nicht Norminhalt.

### 1.5 Wertung DIN V 18599‑10

| Kriterium | Bewertung |
|---|---|
| Nutzungsarten | NWG: 22 TWW‑Kategorien (Tab. 7) über 41 Zonierungsprofile; WG: EFH/MFH über Flächenformel |
| Zeitliche Auflösung | **Monat** (Jahresbilanz). Kein Tagesgang, kein Wochengang. n_SP = 1/2 ist nur eine Auslegungshilfe |
| Für Jahressimulation | Nur als **Jahres-/Monatssumme** brauchbar; Tagesgang muss aus anderer Quelle kommen |
| Für Auslegung | Ungeeignet (keine Spitzenleistung) |
| Nutzereingaben | Gering: Zone/Nutzung + Bezugsgröße (Fläche, Personen, Betten, Sitzplätze) |
| Lizenz | Lizenzpflichtig (DIN/Beuth). Zwar über GEG amtlich in Bezug genommen, aber der Normtext inkl. Tabellenwerte bleibt geschützt |
| Empirische Absicherung | Für Wohngebäude gut (BBSR 17/2017: ista‑Daten 1,7 Mio. Datensätze → **11,1 kWh/(m²·a)** MFH; co2online ~10 kWh/(m²·a) MFH, **9,2 kWh/(m²·a)** EFH/ZFH; Techem/Brunata‑Auswertung → 9–13 kWh/(m²·a)). Für NWG sind die Tab.‑7‑Werte weitgehend Expertenschätzungen |

Quelle Absicherung: [BBSR-Online-Publikation 17/2017 „Nutzenergiebedarf für Warmwasser in Wohngebäuden"](https://www.bbsr.bund.de/BBSR/DE/veroeffentlichungen/bbsr-online/2017/bbsr-online-17-2017-dl.pdf?__blob=publicationFile&v=2) (frei verfügbar).

---

## 2. DIN 4708 – Zentrale Wassererwärmungsanlagen (Teil 1–3)

*(Quellen: [Buderus Planungsunterlage Logalux 6 720 818 349](https://www.heizungsdiscount24.de/pdf/Buderus-Logalux-Planungsunterlage.pdf), [DIN 4708‑1 Inhaltsverzeichnis](https://www.bhkw-infozentrum.de/richtlinien/din_4708_teil1_inhalt.pdf), [SBZ Monteur 07/2010](https://www.sbz-monteur.de/wp-content/uploads/2015/09/F%C3%BCr-Warmduscher-geeignet_07.2010.pdf), [baunetzwissen](https://www.baunetzwissen.de/heizung/fachwissen/warmwasser/warmwasserbedarf-fuer-wohngebaeude-161292))*

### 2.1 Aufbau
- **Teil 1:** Begriffe und Berechnungsgrundlagen
- **Teil 2:** Regeln zur Ermittlung des Wärmebedarfs (Bedarfskennzahl **N**)
- **Teil 3:** Regeln zur Leistungsprüfung von Wassererwärmern (Leistungskennzahl **N_L**)

### 2.2 Kernkonzepte

**Einheitswohnung (N = 1):** 4 Räume, **Belegungszahl p = 3,5**, eine Normalbadewanne DIN 4475‑E (1600 × 700 mm), Zapfstellenbedarf **w_V = 5820 Wh** (rechnerisch 5700 Wh) → Wärmebedarf **3,5 × 5820 = 20 370 Wh ≈ 20,4 kWh/Tag** (bei ΔT = 35 K → ca. 500 l @45 °C).

**Bedarfskennzahl:** `N = Σ(n · p · Σw_V) / (3,5 · 5820)`

**Belegungszahlen p (Richtwerte nach Raumzahl r):**

| r | 1 | 1½ | 2 | 2½ | 3 | 3½ | 4 | 4½ | 5 | 5½ | 6 | 6½ | 7 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| p | 2,0¹ | 2,0 | 2,0 | 2,3 | 2,7 | 3,1 | **3,5** | 3,9 | 4,3 | 4,6 | 5,0 | 5,4 | 5,6 |

¹ p = 2,5 wenn überwiegend 1-/2‑Raum‑Wohnungen. Raumzahl r = Wohn-, Schlaf-, Aufenthaltsräume; Küche (außer Wohnküche), Diele, Flur, Bad, Abstellraum zählen nicht.

**Wertigkeiten / Zapfstellenbedarf w_V (Wärmemengenbedarf je Entnahme, ΔT = 35 K):**

| Verbrauchseinrichtung | Kurzz. | V_E [l] | w_V [Wh] |
|---|---|---|---|
| Badewanne DIN 4475‑E 1600×700 | NB 1 | 140 | 5820 (5700 rechn.) |
| Badewanne DIN 4475‑E 1700×750 | NB 2 | 160 | 6510 |
| Kleinraum-/Stufenwanne | KB | 120 | 4890 |
| Großraumwanne 1800×750 | GB | 200 | 8140 |
| Brausekabine, Sparbrause (6 min) | BRS | 40 | 1630 |
| Brausekabine, Normalbrause | BRN | 90 | 3660 |
| Brausekabine, Luxusbrause | BRL | 180 | 7320 |
| Waschtisch | WT | 17 | 730 |
| Bidet | BD | 20 | 810 |
| Handwaschbecken | HT | 9 | 370 |
| Küchenspüle | SP | 30 | 1120 |

Grundformel: `w_V = V_E · Δϑ · c`, mit Δϑ = 35 K.

**Anrechnungsregeln:** Bei **Normalausstattung** wird je Wohnung nur die Badewanne angesetzt (auch wenn nur eine Brausekabine vorhanden ist, wird der Badewannenwert genommen); Waschtisch, Bidet, Spüle bleiben unberücksichtigt. Bei **Komfortausstattung** zusätzliche Einrichtungen nach Katalog; Gästezimmer‑Badewanne mit 50 % w_V, Gästezimmer‑Brause mit 100 %.

### 2.3 Zapfperiode (Bedarfsspitze)
Die Theorie geht von einer **gaußglockenförmigen Zapfperiode** aus: zu Beginn langsam ansteigend, in der Mitte Maximum, gegen Ende abfallend. Sie wird in **5 Zapfzeiten und 4 Pausenzeiten** zerlegt; die **dritte Zapfung ist die größte und dauert immer 10 Minuten**. Alle übrigen Zeiten und Zapfmengen sind für **N = 1 bis N = 300** in DIN 4708 festgelegt. Die Höhe der dritten Zapfung liefert die **Spitzenzapfleistung** – genau der Wert, den man für die Auslegung einer Frischwasserstation braucht.

### 2.4 Speicherauswahl (3 Forderungen)
1. N_L des Speichers ≥ N des Gebäudes
2. Wärmeerzeugerleistung ≥ die zur N_L gehörende Warmwasser‑Dauerleistung bei 10/45 °C
3. Bei kombinierter Heizung/TWW: Kesselzuschlag nach DIN 4708‑2

### 2.5 Grenzen (wichtig!)
- **Nur gemischt belegte Wohngebäude.** Ausdrücklich **außerhalb** des Gültigkeitsbereichs: Werkswohnungen, Firmenwohnungen, **Hotels, Altenwohnheime, Wohnheime, Studentenwohnheime, Campingplätze** und andere wohnungsähnliche Gebäude (dort höhere Gleichzeitigkeit → in der Praxis über „Normalverteilung mit freier Periodendauer" gerechnet).
- Nur **Auslegungsfall**, keine Zeitreihe, kein Jahresenergiebedarf.
- N_L‑Werte sind **auf 10/45 °C und übliche Heizkessel-Vorlauftemperaturen bezogen**; für Wärmepumpen praktisch nicht verfügbar – der [BWP‑Leitfaden Trinkwassererwärmung (2019)](https://www.waermepumpe.de/uploads/tx_bcpageflip/BWP_LF_TWW_2019_02.pdf) stellt ausdrücklich fest, dass das NL‑Verfahren bei Wärmepumpensystemen „in der Regel nicht angewendet werden kann, da NL‑Zahlen der Speicher für die im Wärmepumpenbetrieb verwendeten Vorlauftemperaturen kaum zur Verfügung stehen".

### 2.6 Wertung

| Kriterium | Bewertung |
|---|---|
| Nutzungsarten | **Nur Wohngebäude** (gemischt belegt) |
| Auflösung | Auslegungsereignis (Zapfperiode ~ 1 h, Kernzapfung 10 min); keine Jahreszeitreihe |
| Für Jahressimulation | Ungeeignet |
| Für Auslegung | Referenz in D für Speicher + Frischwasserstation |
| Nutzereingaben | Mittel–hoch: je Wohnungsgruppe Raumzahl, Anzahl WE, Belegung, Zapfstellenausstattung |
| Lizenz | Lizenzpflichtig; **aber**: N-/w_V-Werte sind in Herstellerplanungsunterlagen (Buderus, Viessmann) frei publiziert und dort zitierfähig |

---

## 3. DIN EN 12831‑3 und nationale Ergänzung

*(Quellen: [IKZ‑Fachartikel „DIN EN 12831‑3 als Ersatz für DIN 4708"](https://www.ikz.de/fileadmin/user_upload/008_011.pdf), [DIN‑Pressemitteilung](https://www.din.de/resource/blob/297944/8f3257426e8236edfd6990726f704c4f/pressemitteilung-din-en-12831-3-data.pdf), [EPB Center Demo EN 12831‑3](https://epb.center/document/demo-en-12831-3/))*

- **Titel:** „Energetische Bewertung von Gebäuden – Verfahren zur Berechnung der Norm‑Heizlast – Teil 3: Trinkwassererwärmungsanlagen, Heizlast und Bedarfsbestimmung, Module M8‑2, M8‑3". Ausgabe **2017‑09**, Entwurf zur nationalen Ergänzung **DIN EN 12831‑3/A100:2021‑09**.
- **Methode:** **Summenlinienverfahren** (kumulierte Bedarfskennlinie vs. Angebotskennlinie aus Speicherinhalt + effektiver Nachheizleistung − Wärmeverluste). Bilanziert in **Minutenschritten**. Damit werden Speichervolumen **und** Nachheizleistung als Wertepaar bestimmt – nicht ein einzelner Punkt wie bei DIN 4708.
- **Zapfprofile:** Charakteristische **24‑h‑Zapfprofile je Nutzungsart** (Anhang B: Stundenzapfprofile). Ziel sind „Norm‑Zapfprofile" für jede Gebäudeart; **individualisierte Profile aus Messdaten sind explizit zulässig**. Für die Bedarfscharakterisierung werden Werktags-/Feiertags- (ggf. Samstags‑)Tagesprofile zu einer Jahreszeitreihe kombiniert; das EPB‑Referenztool gibt Monats- und Stundenwerte aus.
- **Verhältnis zu DIN 4708:** EN 12831‑3 ist auf **alle Gebäudearten** anwendbar und soll DIN 4708 langfristig ablösen; DIN 4708 kann für Wohngebäude aber **weiterhin angewendet werden**. Die nationale Ergänzung soll u. a. die Übertragung der Bedarfs-/Leistungskennzahlen auf das Summenlinienverfahren für Wohngebäude ermöglichen sowie Prüf-/Randbedingungen und normative Zapfprofile für Gebäudekategorien liefern.
- **Größenordnungs-Vergleich (IKZ, MFH mit 11 Wohnungen, ca. 200 kWh/d):**

| Verfahren | Speicher | Leistung |
|---|---|---|
| DIN 4708 | 380 l | 60 kW |
| DIN EN 12831‑3 (hoher Bedarf) | 380 l | 30 kW |
| DIN EN 12831‑3 (realistisch, 40 l/(P·d)) | 300 l | 20 kW |

  → Die neue Norm führt bei gleicher Speichergröße zu **deutlich geringerer Erzeugerleistung** – relevant für Wärmepumpen-Auslegung.

**Wertung:** Methodisch die **beste Passung zu eurer Aufgabe** (Zeitreihe + Auslegung in einem), aber: Anhang B ist informativ/begrenzt, die deutschen Norm‑Zapfprofile sind noch nicht so breit wie DIN V 18599‑10 Tab. 7. Auflösung Minute. Nutzereingaben: mittel bis hoch (Zapfprofil, Speicherverluste, Nachheizleistung, Temperaturen). Lizenzpflichtig.

---

## 4. VDI 6002 Blatt 1 und Blatt 2 (Ausgabe 2014‑03)

*(direkt aus den lokalen PDFs ausgewertet)*

### 4.1 Blatt 1 – „Solare Trinkwassererwärmung, Allgemeine Grundlagen, Systemtechnik und Anwendung im Wohnungsbau"

**Anhang D „Profile des Warmwasserbedarfs"** (S. 80–84) – die eigentlich relevante Fundstelle:

- **Datenbasis:** Verbrauchsmessungen der **ZfS – Rationelle Energietechnik GmbH, Hilden**, aus dem Programm **Solarthermie‑2000**. Vergleichbar mit anderen Untersuchungen.
- **Gültigkeit:** repräsentativ für **große Wohngebäude mit > 100 Bewohnern**. Für EFH/ZFH wurden **ausdrücklich keine Profile definiert** („z. B. während des Urlaubs kein Bedarf; bei Zweipersonenhaushalten mit beiden berufstätig kein Bedarf während der Arbeitszeit") – dort sind die gezeigten Profile anzupassen.
- **Bild D1 – Jahresprofil (Monatsauflösung):** berücksichtigt Urlaubszeiten auch außerhalb des Sommers (Osterferien; Herbst-/Weihnachtsferien, letztere den Herbstferien zugeschlagen). **Empfehlung der Richtlinie: Simulationsprogramme sollten das Jahr in Wochen, nicht in Monaten auflösen.**
- **Bild D2 – Wochenprofil (Tagesauflösung):** Mo–Fr gleich bewertet (Mo etwas geringer, Fr etwas höher, Unterschiede vernachlässigbar); **Samstag etwas höher als Werktag, Sonntag noch etwas höher als Samstag**. Der Sonntagsanstieg ist im Winter stärker ausgeprägt; angesetzt ist ein Jahresmittelwert. Feiertage = Sonntage.
- **Bilder D3–D5 – Tagesprofile (Stundenauflösung)** für Arbeitstag / Samstag / Sonntag:
  - **Nachtstunden (1.–5. Stunde): geringer, aber keineswegs vernachlässigbarer Bedarf.**
  - **Arbeitstag:** ausgeprägte Morgen- **und** Abendspitze; die Richtlinie setzt **beide Spitzen gleich hoch** an (je nach Bewohnerstruktur kann real die eine oder andere dominieren). Tagesminimum (außer Nacht) **am frühen Nachmittag, ca. 15./16. Tagesstunde**.
  - **Samstag:** Morgenspitze **später und höher** als werktags; Abendspitze kaum noch erkennbar; Nachmittagsminimum nur schwach.
  - **Sonntag:** ähnlich Samstag, Abendspitze etwas höher, Nachmittagsminimum etwas tiefer.
  - **Für die Auslegung wichtig:** „An einzelnen Tagen können die Stundenspitzen am Morgen oder am Abend **fast den doppelten Wert** der eingezeichneten Maxima erreichen. Dies ist besonders bei der Komponentenauslegung zu beachten, für Simulationsprogrammprofile jedoch weniger wichtig."

⚠️ **Einschränkung:** Die Profile D1–D5 liegen in Blatt 1 **nur als Balkendiagramme** vor; im Text-Layer des PDF sind **keine Zahlenwerte** enthalten. Für ein Tool müssten sie aus der Grafik digitalisiert werden (rechtlich: abgeleitete Werte, kein 1:1‑Abdruck → besser als Modellparameter kapseln).

### 4.2 Blatt 2 – Studentenwohnheime, Seniorenheime, Krankenhäuser, Hallenbäder, Campingplätze

**Verbrauchskennwerte (alle l/(vp·d) bei 60 °C; vp = Vollbelegungsperson):**

| Gebäudetyp | Bezugsgröße vp | Auslegung sommerl. Schwachlast (Bereich Messwerte) | Jahresmittel | Winter‑Spitzenperiode |
|---|---|---|---|---|
| **Studentenwohnheim** (Tab. 2) | Bewohner | **20** (12–30) | 37 (Messbereich 34–45) | 46 (Messbereich 45–55) |
| **Seniorenheim/Pflegeheim** (Tab. 4) | Anzahl Betten | **33** (30–45) | 36 (34–50) | 40 (38–53) |
| **Krankenhaus** (Tab. 5) | Anzahl Betten | **33** (30–50)* | 38 (35–55) | 42 (40–60) |
| **Hallenbad Standard** (Tab. 6) | Besucher in Schwachlastperiode | **22** (20–30) | – | – |
| **Hallenbad gut ausgestattet** (Tab. 6) | Besucher | **35** (30–50) | – | – |

*In Tab. 5 steht als Auslegungswert 33, die informativen Zeilen beziehen sich aber auf „35 l/(vp·d) im Sommer" – **Inkonsistenz im Normtext; im Zweifel 35 l/(vp·d) verwenden.** Bitte im Tool als Bandbreite hinterlegen.

Vergleichswert Hallenbad: VDI 2089 Bl. 1 nennt 50–80 l bei 42 °C ≈ 30–50 l bei 60 °C; in Standardhallenbädern eher am unteren Rand.

**Auslegungsempfehlungen Hallenbad (Solar):** je 60–70 l Tagesbedarf @60 °C → 1 m² Kollektorfläche; spez. Solarspeichervolumen 50–55 l je m² Kollektor.

**Campingplätze:** Blatt 2 unterscheidet **Sommercampingplatz** und **Ganzjahrescampingplatz** (Jahresprofile Bild 12/13, Wochenauflösung; Basis: Diplomarbeit, geglättet). Die Richtlinie **rät ausdrücklich davon ab**, die Profile statt Messdaten zu verwenden – sie sollen nur „einen Eindruck von der Bedarfsstruktur geben". Tabelle 7 gibt die Witterungsabhängigkeit der Belegung:

| Stellplatzstruktur | Witterungsabhängigkeit |
|---|---|
| Dauerstellplätze | gering bis mäßig |
| Touristenplätze (1 bis mehrere Wochen) | stark |
| Touristenplätze für Tagesgäste | sehr stark |

**Tabelle 3 – Tagesprofile Studentenwohnheim (Stundenauflösung, % vom Tagesbedarf)** – die **einzige numerisch vorliegende Profiltabelle** des ganzen Regelwerks:

| Tagesstunde | Montag | Di–Do | Freitag | Samstag | Sonn-/Feiertag |
|---|---|---|---|---|---|
| 1 | 1,5 | 1,2 | 1,5 | 2,0 | 1,3 |
| 2 | 1,1 | 0,9 | 1,1 | 0,7 | 1,2 |
| 3 | 0,5 | 0,4 | 0,6 | 0,3 | 0,7 |
| 4 | 0,7 | 0,6 | 0,8 | 0,7 | 0,5 |
| 5 | 1,3 | 1,4 | 1,8 | 1,3 | 0,6 |
| 6 | 3,5 | 4,2 | 4,6 | 2,5 | 0,7 |
| 7 | 5,6 | 7,3 | 8,2 | 3,8 | 1,2 |
| 8 | 5,2 | 6,6 | 7,4 | 4,6 | 3,8 |
| 9 | 5,0 | 6,3 | 7,0 | 7,8 | 5,0 |
| 10 | 4,5 | 5,0 | 5,5 | 6,9 | 5,8 |
| 11 | 4,2 | 4,4 | 4,8 | 5,5 | 8,0 |
| 12 | 3,6 | 3,8 | 4,2 | 6,3 | 7,4 |
| 13 | 3,2 | 3,2 | 3,4 | 6,2 | 7,1 |
| 14 | 4,2 | 4,0 | 4,1 | 6,0 | 5,0 |
| 15 | 4,4 | 4,1 | 4,2 | 4,0 | 4,0 |
| 16 | 4,8 | 4,3 | 4,2 | 4,1 | 4,4 |
| 17 | 6,2 | 5,5 | 4,5 | 5,2 | 4,6 |
| 18 | 6,6 | 6,1 | 5,3 | 5,4 | 6,2 |
| 19 | 7,1 | 6,4 | 5,5 | 6,6 | 7,6 |
| 20 | 6,8 | 6,2 | 5,3 | 5,2 | 6,1 |
| 21 | 6,6 | 5,9 | 5,0 | 4,2 | 6,0 |
| 22 | 6,2 | 5,7 | 4,8 | 4,0 | 6,0 |
| 23 | 4,2 | 3,8 | 3,5 | 4,0 | 4,0 |
| 24 | 3,0 | 2,7 | 2,7 | 2,7 | 2,8 |
| **Σ** | 100 | 100 | 100 | 100 | 100 |

Anmerkungen der Norm: Mo geringerer Vormittagsbedarf (Rückkehrer); Fr geringerer Nachmittagsbedarf (frühe Abreise); Sa späterer Morgenanstieg, flachere Mittagssenke, geringerer Spätabendbedarf; So sehr später Morgenanstieg, hoher Bedarf bis ca. 22 Uhr (Rückkehrer). „Wenn eine Aufteilung auf Mo, Di–Do und Fr im Simulationsprogramm nicht möglich ist, kann für Mo–Fr das Profil von Di–Do benutzt werden."

**Übrige Tagesprofile in Blatt 2** (Bild 8/9 Seniorenheim Werktag / Sa+So, Bild 10 Krankenhaus „alle Wochentage gleich", Bild 16/17 Campingplatz Werktag/Sonntag) sind **nur als Diagramme** vorhanden – gleiches Digitalisierungsproblem wie Blatt 1.

**Jahres-/Wochenprofile Blatt 2:** Bild 1 (Studentenwohnheim, Wochenauflösung, normiert auf Sommer‑Schwachlast = 1; Verhältnis Schwachlast:Spitze typ. **1 : 2,3**; vorlesungsfreie Zeiten in Tabelle 1 tabelliert), Bild 2 (Monatsauflösung), Bild 3 (Wochenprofil), Bild 5–7 (Seniorenheim), Bild 11 (Hallenbad), Bild 12–15 (Camping).

### 4.3 Wertung VDI 6002

| Kriterium | Bewertung |
|---|---|
| Nutzungsarten | Bl. 1: große Wohngebäude (>100 Bewohner). Bl. 2: Studentenwohnheim, Senioren-/Pflegeheim, Krankenhaus, Hallenbad, Campingplatz (Sommer/Ganzjahr) |
| Auflösung | **Stunde** (Tagesprofil), **Tag** (Wochenprofil), **Woche/Monat** (Jahresprofil) – exakt das, was für 8760 h gebraucht wird |
| Für Jahressimulation | **Beste deutschsprachige Quelle**, ausdrücklich für Simulationsprogramme gedacht |
| Für Auslegung | Bedingt: Hinweis auf „fast doppelte" Einzeltagesspitzen; keine geschlossene Spitzenlastformel |
| Nutzereingaben | Sehr gering: Gebäudetyp + Vollbelegungspersonen (Betten/Bewohner/Besucher) |
| Lizenz | Lizenzpflichtig (VDI/Beuth). Kennwerte werden aber z. B. von Buderus mit Quellenangabe „Werte nach VDI 6002" publiziert → für Kennwerte gibt es zitierfähige Sekundärquellen |
| Lücke | Kein Hotel, kein Bürogebäude, keine Schule, keine Sporthalle, kein EFH/kleines MFH |

---

## 5. Ecodesign-/Energielabel-Zapfprofile (VO (EU) 814/2013 & 812/2013, EN 16147)

*(Quellen: [VO (EU) Nr. 814/2013, EUR‑Lex](https://eur-lex.europa.eu/legal-content/DE/ALL/?uri=CELEX:32013R0814); Auszug + Auswertung: [BFE‑Bericht „Energieeinsparpotenzial Warmwasserbehälter", J. Nipkow](https://pubdb.bfe.admin.ch/de/publication/download/8306))*

### 5.1 Aufbau
- Anhang VII Tabelle 3 der VO 814/2013 definiert **Lastprofile (Zapfprofile)** **3XS, XXS, XS, S, M, L, XL, XXL, 3XL, 4XL** (in 812/2013 – Label – kommen 3XS, 3XL, 4XL nicht vor).
- Jedes Profil ist eine **feste Zapffolge mit Uhrzeit**, entnommener Nutzenergie **Q_tap** [kWh], Entnahme‑Volumenstrom **f** [l/min], Nutzbarkeitsgrenze **T_m** [°C] und zu erreichender Mindesttemperatur **T_p** [°C].
- **Zeitfenster: 07:00 bis 21:45**, danach bis 07:00 keine Entnahme.
- Default: wo kein f angegeben ist, gilt **2 l/min**; wo kein T_m angegeben ist, gilt **25 °C**; wo kein T_p angegeben ist, besteht keine Anforderung.
- **Q_ref** = Tagessumme der Q_tap.

### 5.2 Q_ref je Profil

| Profil | 3XS | XXS | XS | S | M | L | XL | XXL | 3XL | 4XL |
|---|---|---|---|---|---|---|---|---|---|---|
| **Q_ref [kWh/d]** | 0,345 | 2,1 | 2,1 | 2,1 | **5,845** | **11,655** | 19,07 | 24,53 | 46,76 | 93,52 |
| resultierender Speicherinhalt @ΔT 45 K [l] | 6,6 | 40,1 | 40,1 | 40,1 | 111,7 | 222,7 | 364,4 | 468,8 | 893,6 | 1787,3 |
| Mindest‑Mischwasser 40 °C ab 29.9.2015 [l] | – | – | – | – | 65 | 130 | 210 | 300 | 520 | 1040 |

Beachte: XXS, XS und S haben **dasselbe Q_ref (2,1 kWh)**, unterscheiden sich aber in der **Zapffolge** (Zahl, Größe und Zeitpunkt der Entnahmen) – XS z. B. mit einer einzigen großen Entnahme (0,525 kWh um 07:30), S mit vielen kleinen 0,105‑kWh‑Entnahmen.

### 5.3 Struktur der wichtigsten Profile (Ausschnitt, Vormittagsteil)

| Uhrzeit | 3XS Q_tap | XXS | XS | S | M (Q_tap / l/min / T_m / T_p) | L (Q_tap / l/min / T_m / T_p) |
|---|---|---|---|---|---|---|
| 07:00 | 0,015 | 0,105 | – | 0,105 | 0,105 / 3 | 0,105 / 3 |
| 07:05 | 0,015 | – | – | – | **1,4 / 6 / – / 40** | **1,4 / 6 / – / 40** |
| 07:15 | 0,015 | – | – | – | – | – |
| 07:30 | 0,015 | 0,105 | **0,525** | 0,105 | 0,105 / 3 | 0,105 / 3 |
| 08:05 | – | – | – | – | – | **3,605 / 10 / 10 / 40** (Bad) |
| 08:30 | – | 0,105 | – | 0,105 | 0,105 / 3 | 0,105 / 3 |
| 09:30 | 0,015 | 0,105 | – | 0,105 | 0,105 / 3 | 0,105 / 3 |
| 10:30 | – | – | – | – | 0,105 / 3 / 10 / 40 | 0,105 / 3 / 10 / 40 |
| … bis 21:45 | … | … | … | … | … | … |
| **Σ Q_ref** | 0,345 | 2,1 | 2,1 | 2,1 | 5,845 | 11,655 |

### 5.4 EN 16147
EN 16147 („Wärmepumpen mit elektrisch angetriebenen Verdichtern – Prüfungen, Leistungsbemessung und Anforderungen an die Kennzeichnung von Geräten zur Warmwasserbereitung") verwendet **dieselben Zapfprofilbezeichnungen 3XS…4XL** und liefert daraus COP_DHW, Aufheizzeit, Bereitschaftsverlust P_es, Mischwassermenge V_max/V_40.

### 5.5 Wozu sie taugen – und wozu nicht

**Taugen für:**
- Rückrechnung von Herstellerdatenblättern (η_wh, COP_DHW, Q_elec, V_40) auf reale Betriebspunkte – **das ist ihr eigentlicher Wert für eine Planungssoftware**.
- Plausibilisierung eines EFH-/Wohnungs‑Tagesbedarfs (M ≈ 5,845 kWh/d ≈ 100 l @60 °C ≈ Familie ohne Baden; L ≈ 11,655 kWh/d ≈ 200 l @60 °C ≈ Familie mit Baden – so auch im BWP‑Leitfaden verwendet).
- Als **Referenz-Tagesprofil eines Einzelhaushalts**, wenn kein besseres vorliegt.

**Taugen nicht für:**
- Jahressimulation (kein Wochen-, kein Jahresgang, konstant 365 Tage identisch, keine Urlaubs-/Wochenendmodulation).
- Nichtwohngebäude.
- Mehrfamilienhäuser (keine Diversität/Überlagerung vorgesehen; naive Vervielfachung erzeugt unrealistisch synchrone Spitzen).
- Auslegung von Speichern in MFH – die Profile sind **Prüfvorschriften**, keine Bedarfsprofile.

**Lizenz: frei.** Amtsblatt der EU, uneingeschränkt reproduzierbar (mit Quellenangabe). **Das ist die einzige komplett frei nutzbare, exakt spezifizierte Zapffolge im gesamten Regelwerk.**

**Nutzereingaben:** minimal (Profilklasse wählen).

---

## 6. VDI 4655 – Referenzlastprofile von Wohngebäuden

*(Quellen: [VDI‑Richtlinienseite](https://www.vdi.de/richtlinien/details/vdi-4655-referenzlastprofile-von-wohngebaeuden-fuer-strom-heizung-und-trinkwarmwasser-sowie-referenzerzeugungsprofile-fuer-fotovoltaikanlagen), [Solarserver 09.07.2021](https://www.solarserver.de/2021/07/09/richtlinie-vdi-4655-realistische-energiebedarfe-von-wohngebaeuden-ermitteln/), [Haustec](https://www.haustec.de/heizung/waermeerzeugung/richtlinie-vdi-4655-energiebedarf-von-wohngebaeuden-ermitteln))*

- Titel: „Referenzlastprofile von Wohngebäuden für Strom, Heizung und Trinkwarmwasser sowie Referenzerzeugungsprofile für Fotovoltaikanlagen", Ausgabe **2021‑07** (Vorgänger 2019‑09 Entwurf, davor 2008).
- **Konzept: Typtage.** Kombinationen aus Jahreszeit (Übergang/Sommer/Winter), Bewölkung (heiter/bewölkt) und Wochentag (Werktag/Sonntag); die Typtagverteilung basiert auf den **Testreferenzjahren TRY 2017**, Zeitreihen der Klimadaten für alle **15 Klimazonen** sind enthalten.
- **Auflösung:** EFH **2 s, 1 min und 15 min**; MFH **15 min**.
- **Anwendungsbereich:** EFH (Neubau und Bestand) bis **max. 6 Personen**; MFH (Bestand) bis **25 Wohneinheiten**.
- **TWW:** wird als eigenes Referenzlastprofil je Typtag geliefert; Jahresbedarf wird über einen Bedarfskennwert skaliert und mit den Typtagfaktoren auf das Jahr verteilt.

**Wertung:**

| Kriterium | Bewertung |
|---|---|
| Nutzungsarten | **nur Wohngebäude** (EFH, MFH) |
| Auflösung | 2 s / 1 min / 15 min – die feinste im deutschen Regelwerk |
| Für Jahressimulation | **Sehr gut** (Typtagverfahren erzeugt vollständige 8760‑h‑Reihe, klimazonen-konsistent mit Heiz- und PV‑Profil) |
| Für Auslegung | Bedingt – MFH‑Profile sind 15‑min‑Mittelwerte, echte 10‑min‑Zapfspitze nach DIN 4708 wird unterschätzt |
| Nutzereingaben | Gering: Gebäudetyp, Personenzahl/WE, Klimazone, Jahresbedarf |
| Grenzen | **> 25 WE nicht abgedeckt** – genau der Bereich, in dem Zirkulation und Gleichzeitigkeit interessant werden. Keine NWG |
| Lizenz | Lizenzpflichtig (ca. 123 € Basisversion); Datensätze Teil der Lizenz |

---

## 7. Zirkulation, Temperatur, Hygiene, Spitzendurchfluss

### 7.1 DVGW W 551 – Legionellen

*(Quelle: [DVGW‑W‑551‑Zusammenfassung, bosy‑online/Buderus Katalog K13](http://www.bosy-online.de/trinkwasser/dvgw-arbeitsblatt_w551.pdf); Buderus Logalux Planungsunterlage Kap. 5.1.5/5.1.6)*

**Abgrenzung:**
- **Kleinanlage:** Ein-/Zweifamilienhäuser (beliebiger Speicherinhalt) **oder** Anlagen mit Speicherinhalt ≤ 400 l **und** Inhalt jeder einzelnen Rohrleitung zwischen Warmwasseraustritt und Entnahmestelle ≤ **3 l** (Zirkulationsleitung zählt nicht mit). → Anforderungen sind **Empfehlung**.
- **Großanlage:** alle übrigen, d. h. Speicher > 400 l **oder** Rohrleitungsinhalt > 3 l. Typisch: Wohngebäude, Hotels, Altenheime, Krankenhäuser, Sport-/Industrieanlagen, Campingplätze, Schwimmbäder.

**Anforderungen an Großanlagen:**
- **60 °C am Warmwasseraustritt** des Speichers bzw. der Frischwasserstation bei bestimmungsgemäßem Betrieb.
- **Schaltdifferenz des Reglers darf 55 °C nicht unterschreiten.**
- Bei **Vorwärmstufen**: gesamter Inhalt muss **einmal pro Tag auf 60 °C** erwärmt werden.
- **Maximale Auskühlung des Zirkulationswassers: 5 K** gegenüber der Speicheraustrittstemperatur (→ Zirkulationsrücklauf ≥ 55 °C).
- Zirkulationsleitungen/Begleitheizungen bis **unmittelbar an die Entnahmearmatur**.
- **Zeitsteuerung darf die Zirkulation max. 8 h täglich unterbrechen** (auch in GEG/EnEV so verankert).
- Ausgenommen von der Zirkulationspflicht: Stockwerks-/Einzelzuleitungen mit ≤ 3 l Inhalt.
- Schwerkraftzirkulation soll vermieden werden.
- Bei **solarer Beheizung von Kleinanlagen**: Laufzeit der Zirkulationspumpe auf ein Minimum begrenzen.

**3‑Liter‑Leitungslängen (Kupferrohr):** 10×1,0 → 60,0 m; 12×1,0 → 38,0 m; 15×1,0 → 22,5 m; 18×1,0 → 14,9 m; 22×1,0 → 9,5 m; 28×1,0 → 5,7 m; 28×1,5 → 6,1 m; 35×1,5 → 3,7 m.

### 7.2 DVGW W 553 – Bemessung von Zirkulationssystemen
Regelt die Ermittlung des Zirkulationsvolumenstroms aus den Rohrleitungswärmeverlusten und den hydraulischen Abgleich (Strangregulierventile / thermische Regulierventile). Kernkriterium bleibt die **5‑K‑Regel** aus W 551. Zirkulationsleitungen sind „nach DIN 1988‑300 bzw. DVGW W 553 zu dimensionieren".

### 7.3 Zirkulations-/Verteilverluste – Kennwerte (Faustwerte)

*(Quelle: [DELTA‑Q, „Kennwerte – Verteilnetze", nach DIN V 4701‑10](https://www.delta-q.de/wp-content/uploads/2021/12/kennwerte_verteilnetze.pdf))*

Flächenbezogener Wärmeverlust der TWW‑ und Zirkulationsverteilung, **zentrale Netze, Neubau, Bezug A_N** [kWh/(m²·a)]:

| A_N [m²] | mit Zirkulation, außerhalb therm. Hülle | mit Zirk., innerhalb (Verlust / Heizwärmegutschrift) | ohne Zirk., außerhalb | ohne Zirk., innerhalb (Verlust / Gutschrift) |
|---|---|---|---|---|
| 100 | 14,6 | 12,1 / 5,4 | 6,7 | 5,1 / 2,3 |
| 150 | 11,6 | 9,8 / 4,4 | 5,4 | 4,2 / 1,9 |
| 200 | 10,1 | 8,7 / 3,9 | 4,7 | 3,8 / 1,7 |
| 300 | 8,7 | 7,7 / 3,5 | 4,0 | 3,3 / 1,5 |
| 500 | 7,6 | 6,9 / 3,1 | 3,4 | 3,0 / 1,3 |
| 750 | 7,1 | 6,6 / 3,0 | – | – |
| 1 000 | 6,9 | 6,5 / 2,9 | – | – |
| 1 500 | 6,8 | 6,4 / 2,9 | – | – |
| 2 500 | 6,6 | 6,3 / 2,8 | – | – |
| 5 000–10 000 | 6,6 | 6,3 / 2,8 | – | – |

**Merkregel:** In kleinen MFH sind die Zirkulationsverluste (10–15 kWh/(m²·a)) **größer als der TWW‑Nutzenergiebedarf** (8,5–13 kWh/(m²·a)). Für eine Wärmepumpen‑JAZ ist das der dominierende Effekt – ein TWW‑Zapfprofil ohne Zirkulationsmodell ist für JAZ‑Aussagen wertlos.

Dezentrale Netze (DIN V 4701‑10): Untertischgerät 0,25 kWh/(m²·a) (Gutschrift 0,11); Badezimmer mit mehreren Zapfstellen 0,76 (0,34); 2 Räume mit gemeinsamer Installationswand 1,01 (0,45); wohnungszentrale TWW‑Versorgung 1,51 (0,68).

### 7.4 VDI 6003 – Komfortkriterien und Anforderungsstufen (Ausgabe 2018‑08)

*(Quelle: [TGA Fachplaner, „Komfortkriterien verbessern Hygiene und Wirtschaftlichkeit"](https://www.tga-fachplaner.de/sites/default/files/ulmer/de-tga/document/file_131900.pdf))*

Definiert **Anforderungsstufen I, II, III** je Entnahmestelle. Ausdrücklich **nicht** „niedriger/mittlerer/hoher Komfort", sondern unterschiedliche Nutzeranforderungen.

**Mindest-Entnahmerate [l/min]:**

| Entnahmestelle | I | II | III |
|---|---|---|---|
| Waschtisch | 3 | 5 | 6 |
| Dusche | 7 | 9 | 9 |
| Badewanne | 7 | 10 | 13 |
| Bidet | – | 3 | 3 |

**Maximale Zeit bis Erreichen der Nutztemperatur [s]:**

| Entnahmestelle | I | II | III |
|---|---|---|---|
| Waschtisch (40 °C) | 60* | 18 | 10 |
| Dusche (42 °C) | 26* | 10 | 7 |
| Badewanne (45 °C) | 26* | 12 | 9 |
| Bidet | – | 15 | 15 |

*Stufe I folgt der 3‑Liter‑Regel des DVGW. Rechtsprechung: 15 s bis 40 °C an jeder Zapfstelle gilt als zumutbarer Mindeststandard.

**Relevanz für ein Zapfprofil-Tool:** VDI 6003 liefert die **Entnahmeraten und Zapfdauern**, aus denen sich synthetische Zapfereignisse (Volumen = Rate × Dauer) generieren lassen – die Brücke zwischen „Tagesmenge in l" und „Einzelzapfung in l/min".

### 7.5 DIN 1988‑300 – Spitzendurchfluss (Diversitätsfunktion)

*(Quelle: [emax‑haustechnik, Umrechnungstabellen nach DIN 1988‑300:2012‑05, Tab. 3](https://www.emax-haustechnik.de/shop/images/products/media/VG042_Umrechnungstabelle_Durchfluss__sdb-de.pdf))*

`V̇_S = a · (ΣV̇_R)^b − c`, Geltungsbereich **0,2 ≤ ΣV̇_R ≤ 500 l/s**

| Gebäudetyp | a | b | c |
|---|---|---|---|
| Wohngebäude | 1,48 | 0,19 | 0,94 |
| Bettenhaus im Krankenhaus | 0,75 | 0,44 | 0,18 |
| Hotel | 0,70 | 0,48 | 0,13 |
| Schule | 0,91 | 0,31 | 0,38 |
| Verwaltungsgebäude | 0,91 | 0,31 | 0,38 |
| Einrichtungen für betreutes Wohnen, Seniorenheim | 1,48 | 0,19 | 0,94 |
| Pflegeheim | 1,40 | 0,14 | 0,92 |

Plausibilisierung: Hotel mit ΣV̇_R = 40 l/s → 0,70 · 40^0,48 − 0,13 = **3,98 l/s** (≈ 10 % Gleichzeitigkeit). Wohngebäude ΣV̇_R = 1 l/s → 0,54 l/s; ΣV̇_R = 10 l/s → 1,35 l/s; ΣV̇_R = 100 l/s → 2,60 l/s. **Der Exponent b < 0,5 ist die eigentliche Diversitätsfunktion.** In der Praxis (Buderus): „In Wohngebäuden ergeben sich gewöhnlich höhere Spitzenvolumenströme im Vergleich zur Auslegung nach DIN 4708."

Berechnungsdurchflüsse V̇_R einzelner Armaturen (DIN 1988‑300 Tab. 2): Waschtisch/Küchenspüle/Bidet 0,07 l/s; Duschwanne 0,15 l/s; Urinal‑Druckspüler 0,30 l/s (jeweils kalt bzw. warm getrennt anzusetzen).

**Achtung:** DIN 1988‑300 dimensioniert **Rohrleitungen**, nicht Erzeuger. Der V̇_S ist ein Momentanwert ohne Dauer – für die Speicherauslegung fehlt die Zeitkomponente.

---

## 8. Schweiz (SIA) und USA (ASHRAE/Hunter) als Ergänzung

### 8.1 SIA 2024:2021 – Raumnutzungsdaten

*(Quellen: [SIA 2024:2021 Leseprobe](https://shop.sia.ch/16d618e8-b0d7-41e1-b20f-926743792ed7/D/DownloadAnhang), [Grundlagenbericht SIA 2024](https://cms.sia.ch/de/api/getMedia/941), [Statusbericht „Harmonisierung SIA‑Standardwerte und Gebäudekategorien"](https://cms.sia.ch/de/api/getMedia/940))*

- **45 Raumnutzungen** (SIA 2024:2021, 80 Seiten). Struktur analog DIN V 18599‑10 Tab. 5: Personenfläche, Präsenzzeit, Raumtemperatur, Aussenluft‑Volumenstrom, Elektrizität, **Wärmebedarf Warmwasser**.
- **Anhang E (normativ): „Herleitung des Warmwasserbedarfs"** – SIA 2024 leitet die Warmwasser‑Standardwerte aus **SIA 385/2** ab.
- Anhang F harmonisiert auf **12 Gebäudekategorien**. Wärmebedarf Warmwasser [kWh/(m²·a)]:

| Kat. | I MFH | II EFH | III Verwaltung | IV Schule | V Verkauf | VI Restaurant | VII Versammlung | VIII Spital | IX Industrie | X Lager | XI Sportbaute | XII Hallenbad |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **SIA 380/1 (bisher)** | 21 | 14 | 7 | 7 | 7 | 56 | 14 | 28 | 7 | 1 | 83 | 83 |
| **SIA 2024, Nutzwarmwasser** | 12 | 10 | 1 | 2 | 1 | 44 | 4 | 22 | 1 | 1 | 30 | 70 |
| **SIA 2024, inkl. 50 % Verluste** | 17 | 15 | 2 | 2 | 2 | 65 | 5 | 33 | 2 | 1 | 45 | 104 |

  Bemerkenswert: der pauschale Verlustaufschlag von **+50 % des Nutzwarmwasserbedarfs** für Speicher-, Verteil- und Ausstossverluste ist eine sehr handliche Faustformel.

### 8.2 SIA 385/2 – Anlagen für Trinkwarmwasser in Gebäuden (2015; Nachfolger 2025)

*(Quellen: [SIA 385/2 Leseprobe/Inhalt](http://shop.sia.ch/cbaa0218-f498-4536-b824-5fc0a7db360a/D/DownloadAnhang), [BFE‑Bericht „Elektrische Wassererwärmung", Nipkow](https://pubdb.bfe.admin.ch/de/publication/download/7388))*

- **Nutzwarmwasserbedarf Wohnbauten: 35…45 Normliter/(Person·d)**, wobei **1 Normliter ≈ 58 Wh** entspricht (das ist 1 l von 10 auf 60 °C: 50 K × 1,163 = 58,15 Wh). Ohne Ausstossverluste („kalter Zapfen"), die je nach Konfiguration nochmals **bis ca. 5 Normliter** ausmachen können.
- Speicherverluste: Zielwert SIA 385/1 grob **1 kWh/Tag** für einen 150‑l‑Speicher.
- Aufbau: Kap. 3 „Grobauslegung: Ausstosszeit und Gesamtanforderung"; Kap. 4 „Feinplanung", darin **4.2 „Statistische Verteilung der Warmwasserentnahmen"** und 4.3 Speichervolumen/-konfiguration und Anschlussleistung; Anhang A (normativ) Nutzwarmwasserbedarf; Anhang B Speicherwärmeverluste; Anhang D Wärmeverluste warmgehaltener Leitungen; Anhang E Ausstosswärmeverluste.
- SIA 385/1 + 385/2 sind die **schweizerische Umsetzung von EN 15316‑3‑1 („Charakterisierung des Bedarfs / Zapfprogramm")** und EN 15316‑3‑2.

**Was SIA abdeckt, was D nicht abdeckt:**
- **Ausstossverluste („kalter Zapfen") als eigene, normativ berechnete Bilanzposition** – in DIN V 18599 nur pauschal enthalten. Für Wohnungsstationen/dezentrale Systeme ist das der entscheidende Unterschied.
- **Ausstosszeit als normatives Komfort- und Auslegungskriterium** (in D nur über VDI 6003 als Richtlinie).
- Der **Normliter (58 Wh)** als saubere, temperaturunabhängige Bezugsgröße – methodisch eleganter als die deutsche 45‑°C-/60‑°C‑Mischung.
- Gebäudekategorien **XI Sportbaute** und **XII Hallenbad** mit eigenem Warmwasser‑Kennwert (in DIN V 18599‑10 Tab. 7 nur „Sportanlage mit Dusche" ohne Flächenbezug).

### 8.3 ASHRAE / Hunter

*(Quelle: ASHRAE Handbook – HVAC Applications, Kap. „Service Water Heating", wiedergegeben u. a. in [Alstrom/Elge Sizing Charts](https://elge-technologies.com/wp-content/uploads/2020/01/domestic-hwh-sizing.pdf))*

**Chart 1 – „Hot-Water Demand per Fixture" [gal/h @140 °F] mit Demand-Faktor und Speicherfaktor:**

| Fixture | Apt. House | Club | Gym | Hospital | Hotel | Ind. Plant | Office | Priv. Res. | School | YMCA |
|---|---|---|---|---|---|---|---|---|---|---|
| Waschtisch privat | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| Waschtisch öffentlich | 4 | 6 | 8 | 6 | 8 | 12 | 6 | – | 15 | 8 |
| Badewanne | 20 | 20 | 30 | 20 | 20 | – | – | 20 | – | 30 |
| Dusche | 30 | 150 | 225 | 75 | 75 | 225 | 30 | 30 | 225 | 225 |
| Küchenspüle | 10 | 20 | – | 20 | 30 | 20 | 20 | 20 | 20 | 20 |
| Geschirrspüler | 15 | 50–100 | – | 50–150 | 50–200 | 20–100 | – | 15 | 20–100 | 20–100 |
| **Demand Factor** | **0,30** | 0,30 | 0,40 | **0,25** | **0,25** | 0,40 | 0,30 | 0,30 | 0,40 | 0,40 |
| **Storage Capacity Factor** | **1,25** | 0,90 | 1,00 | 0,60 | 0,80 | 1,00 | 2,00 | 0,70 | 1,00 | 1,00 |

**Chart 2 – gemessene Verbräuche aus 129 Gebäuden** (max. Stunde / max. Tag / Durchschnittstag). **Enthält explizit eine Diversitätsstaffelung nach Objektgröße:**

| Gebäudetyp | Max. Stunde | Max. Tag | Ø Tag |
|---|---|---|---|
| Männer-Wohnheim | 3,8 gal/Student | 22 gal | 13,1 gal |
| Frauen-Wohnheim | 5,0 gal/Student | 26,5 gal | 12,3 gal |
| Motel < 60 Einheiten | 6,0 gal/Einheit | 35,0 | 20,0 |
| Motel 60 Einheiten | 5,0 | 25,0 | 14,0 |
| Motel ≥ 100 Einheiten | 4,0 | 15,0 | 10,0 |
| Pflegeheim | 4,5 gal/Bett | 30,0 | 18,4 |
| Bürogebäude | 0,4 gal/Person | 2,0 | 1,0 |
| Restaurant Typ A (volle Mahlzeit) | 1,5 gal/Mahlzeit_max/h | 11,0 | 2,4 gal/Ø‑Mahlzeit/d |
| Restaurant Typ B (Imbiss) | 0,7 | 6,0 | 0,7 |
| **Apartments ≤ 20 WE** | **12,0 gal/WE** | 80,0 | 42,0 |
| **Apartments 50 WE** | **10,0** | 73,0 | 40,0 |
| **Apartments 75 WE** | **8,5** | 66,0 | 38,0 |
| **Apartments 100 WE** | **7,0** | 60,0 | 37,0 |
| **Apartments ≥ 200 WE** | **5,0** | 50,0 | 35,0 |
| Grundschule | 0,6 gal/Schüler | 1,5 | 0,6 (je Betriebstag) |
| High School | 1,0 | 3,6 | 1,8 |

Umrechnung: 1 gal ≈ 3,785 l; die Werte sind auf 140 °F (60 °C) bezogen.

**Was ASHRAE abdeckt, was D nicht abdeckt:**
- **Club, Gym/YMCA, Motel, Wohnheime nach Geschlecht, Food Service nach Typ A/B, Industrial Plant** – Nutzungsarten mit Duschcharakter, die in D nur pauschal („Sportanlage mit Dusche") vorkommen.
- **Explizite Trennung von „Maximum Hour", „Maximum Day" und „Average Day"** je Nutzungsart – genau die drei Größen, die eine Planungssoftware braucht (Erzeugerspitze / Speicherbemessung / Jahresbilanz).
- **Speicherfaktor je Gebäudetyp** (0,6 Krankenhaus … 2,0 Bürogebäude) als direkte Umsetzungsregel Spitzenstunde → Speichervolumen.
- **Gemessene Diversität über die Wohnungszahl** (12 → 5 gal/WE·h von 20 auf ≥200 WE, Faktor 2,4).

**Lizenz:** ASHRAE Handbook ist urheberrechtlich geschützt; die Tabellen sind aber in zahllosen frei zugänglichen Sekundärquellen abgedruckt.

---

## 9. Diversitäts-/Gleichzeitigkeitsfunktionen – Übersicht

| Quelle | Form | Bezugsgröße | Bemerkung |
|---|---|---|---|
| **DIN 4708‑2** | N = f(Wohnungen, Belegung, Zapfstellen); N → N_L über normierte Zapfperiode | Bedarfskennzahl N (1…300) | Die Gleichzeitigkeit steckt implizit in der genormten Zapfperiode (5 Zapfungen/4 Pausen, 3. Zapfung 10 min). Nur Wohngebäude |
| **DIN 1988‑300 Tab. 3** | `V̇_S = a·(ΣV̇_R)^b − c`, 7 Gebäudetypen | Summendurchfluss ΣV̇_R [l/s] | Geschlossene, gut implementierbare Formel. Nur Rohrnetz/Momentandurchfluss |
| **DIN V 18599‑10 Tab. 7** | n_SP ∈ {1, 2} | Nutzungsart | n_SP = 1: Bedarf über den Tag verteilt; n_SP = 2: Bedarf zu bestimmten Tageszeiten. Sehr grob, aber die einzige TWW‑Gleichzeitigkeitsangabe für NWG |
| **ASHRAE Chart 2** | Tabellierte Staffelung nach Objektgröße | Anzahl Apartments / Motelzimmer | 20 WE: 12 gal/WE·h → ≥200 WE: 5 gal/WE·h. Empirisch (129 Gebäude) |
| **TU Dresden / ITG** (für Wohnungsstationen) | Gleichzeitigkeitskurve GF(n_Wohnungen), liegt **unterhalb** der DIN‑4708‑Kurve | Anzahl Wohnungen | In [Tuxhorn Planungsleitfaden tubra‑Wohnungsstationen](https://www.tuxhorn.de/wp-content/uploads/2022/05/Auswahl-Wohnungsstation.pdf) als Diagramm mit „obere Kurve DIN 4708 / untere Kurve TU‑Dresden". Beispiel: 1 WE → GF = 1; 2 WE → 2; 4 WE → 2; 6 WE → 3. GF wird auf die nächstgrößere ganze Zahl gerundet. Netzvolumenstrom: `V̇_ges = (W_Anz − GF)·V̇_HZ + GF·V̇_WW` (Vorrangschaltung). Spitzenlastzeit 10 min |
| **Stochastische Modelle** (DHWcalc / IEA SHC Task 26, heatbeat) | Diversität entsteht aus der Superposition statistisch unabhängiger Einzelzapfungen | Anzahl Haushalte | Vergleichszahlen aus [heatbeat](https://heatbeat.de/de/blog/2/): mit Speicher sinkt der Gleichzeitigkeitsfaktor von 100 % (1 Gebäude) auf ca. 40–50 % (20 Gebäude) und 30–45 % (100 Gebäude); **ohne** Speicher auf ca. 15–20 % (20) und ~10 % (100). Beispiel EFH ohne Speicher: 42 kW Spitze; ideal geladener Speicher: 3 kW |

**Empfehlung:** Für ein Tool, das sowohl Jahresreihe als auch Auslegung liefern soll, ist der stochastische Superpositionsansatz (Kap. 10) der einzige, der **beide** Anforderungen mit **einer** Modellphysik bedient – die Gleichzeitigkeit fällt dann als Ergebnis an, statt als eingebauter Faktor.

---

## 10. Freie Referenz für die Profilgenerierung: IEA SHC Task 26 / DHWcalc

*(Quellen: [Jordan/Vajen, „Realistic Domestic Hot-Water Profiles in Different Time Scales", V2.0, Universität Marburg 2001](https://sel.me.wisc.edu/trnsys/trnlib/iea-shc-task26/iea-shc-task26-load-profiles-description-jordan.pdf); [DHWcalc, Uni Kassel](https://www.uni-kassel.de/maschinenbau/en/institute/thermische-energietechnik/fachgebiete/solar-und-anlagentechnik/downloads.html))*

Weil im Regelwerk die Tagesprofile fast nur als Diagramm vorliegen, ist dieses Modell die praktikabelste **frei dokumentierte** Grundlage:

**Vier Zapfkategorien (Basis 200 l/d EFH):**

| | A: kleine Zapfung (Händewaschen) | B: mittlere Zapfung (Spülmaschine etc.) | C: Baden | D: Duschen | Σ |
|---|---|---|---|---|---|
| V̇ [l/min] | 1 | 6 | 14 | 8 | |
| Dauer [min] | 1 | 1 | 10 | 5 | |
| Ereignisse/Tag | 28 | 12 | 0,143 (1×/Woche) | 2 | |
| σ (Gauß) | 2 | 2 | 2 | 2 | |
| Volumen/Ereignis [l] | 1 | 6 | 140 | 40 | |
| Volumen/Tag [l] | 28 | 72 | 20 | 80 | **200** |
| Anteil | 0,14 | 0,36 | 0,10 | 0,40 | 1,00 |

Maximale Energie einer Einzelzapfung: 14 l/min × 10 min × 1,16 Wh/(kg·K) × 35 K = **5680 Wh** — bewusst an DIN 4708 (5820 Wh) angelehnt.

**Wahrscheinlichkeitsmodell:** `prob = prob(Jahr) · prob(Wochentag) · prob(Tag) · prob(Urlaub)`
- **prob(Jahr):** Sinusfunktion mit ±10 % Amplitude um den Tagesmittelwert (Mack98 fand ±25 %: ±5 K Kaltwassertemperatur ≙ ±14 % plus Verhaltensvariation).
- **prob(Wochentag):** Mo–Do **95 %**, Fr **98 %**, Sa **109 %**, So **113 %** vom Mittel. Die Wochentagsverteilung wird nur auf die Kategorie „Baden" angewendet (deutlich stärkere Sa/So-Überhöhung).
- **prob(Tag):** kleine und mittlere Zapfungen **gleichverteilt zwischen 5:00 und 23:00**; Duschen und Baden mit Morgen- und Abendspitze.
- **prob(Urlaub):** je 100 l/d Grundlast eine 2‑Wochen‑Periode mit reduziertem Bedarf zwischen 1. Juni und 30. September (zufälliger Startzeitpunkt). Für ein MFH ergeben sich entsprechend viele, zeitlich versetzte Urlaubsperioden — **das ist genau der Diversitätsmechanismus, der die Sommer-Schwachlast realistisch abbildet.**

**Auflösungen:** 1 min, 6 min, 1 h; Profildateien für 100/200/400/800/1600/3200 l/d, **durch Superposition beliebig skalierbar** (jede Verdopplung mit anderem Zufallsstartwert). Die Autoren warnen: bei Stundenprofilen werden die Volumenströme so klein, dass sie „für kleine und mittlere Solaranlagen nicht mehr als realistisch angesehen werden können".

**Lizenz:** Bericht und Profile sind frei verfügbar (IEA SHC Task 26); DHWcalc wird von der Uni Kassel kostenlos bereitgestellt. **Der einzige komplett offene, vollständig dokumentierte Profilgenerator im deutschsprachigen Raum.**

---

## 11. Lizenz- und Urheberrechtsbewertung

| Quelle | Status | Was ihr dürft / nicht dürft |
|---|---|---|
| **VO (EU) 814/2013, 812/2013** | **frei** (Amtsblatt EU) | Zapfprofile 3XS–4XL vollständig reproduzierbar. Sicherste Basis für eine sichtbare Profiltabelle im Tool |
| **IEA SHC Task 26 / DHWcalc** | **frei** (mit Quellenangabe) | Parameter und Profile verwendbar und dokumentierbar |
| **BBSR‑Online 17/2017** | **frei** | Kennwerte zitierbar |
| **DIN V 18599‑10** | lizenzpflichtig; über GEG amtlich in Bezug genommen | Werte **im Programm implementieren**: branchenüblich und faktisch unvermeidlich (jede 18599‑Software tut das). **Tabellen 4/5/7 nicht als Tabelle in Handbuch/UI/Marketing abdrucken.** Empfehlung: DIN‑Media‑Lizenz für Softwarehersteller klären. Ausgabe von berechneten Ergebnissen ist unkritisch |
| **DIN 4708‑1/‑2/‑3** | lizenzpflichtig | Kennwerte (Belegungszahlen, w_V) sind in Herstellerunterlagen (Buderus, Viessmann) **frei publiziert**; über diese Sekundärquelle zitierbar. Die Zapfperioden-Tabelle N = 1…300 ist **nicht** frei verfügbar → Lizenz nötig oder EN 12831‑3 verwenden |
| **DIN EN 12831‑3 (+/A100)** | lizenzpflichtig | Für die Methodik (Summenlinienverfahren) unbedenklich, für die Anhang‑B‑Profile Lizenz nötig |
| **DIN 1988‑300** | lizenzpflichtig | Die a/b/c‑Konstanten sind in Herstellerunterlagen frei publiziert; Formel als solche ist keine schutzfähige Schöpfung |
| **VDI 6002 Bl. 1/2, VDI 4655, VDI 6003, VDI 2089** | lizenzpflichtig (Vervielfältigung „auch für innerbetriebliche Zwecke nicht gestattet", auf jeder Seite vermerkt) | **Hier ist die Lage am strengsten.** Kennwerte VDI 6002 sind über Buderus mit Quellenangabe zitierbar; die **Tabelle 3 (Stundenprofil Studentenwohnheim) und die Diagramm-Profile nicht**. Empfehlung: VDI‑Lizenz oder eigene, aus Messdaten/DHWcalc abgeleitete Profile, die qualitativ gleichwertig sind |
| **DVGW W 551 / W 553** | lizenzpflichtig; W 551 ist als „allgemein anerkannte Regel der Technik" faktisch Referenz | Die Kernanforderungen (60/55/5 K/8 h/3 l) sind in unzähligen Fachpublikationen frei wiedergegeben |
| **SIA 2024, 385/1, 385/2** | lizenzpflichtig | Gebäudekategorie-Kennwerte sind über den frei publizierten [SIA‑Statusbericht](https://cms.sia.ch/de/api/getMedia/940) zitierbar |
| **ASHRAE Handbook** | lizenzpflichtig, aber weit verbreitet reproduziert | Für einen deutschen Markt ohnehin nur als Plausibilitätsvergleich |

**Praktische Empfehlung:** Normwerte im Code als parametrierte Datensätze kapseln, in der Dokumentation nur **Verfahren + Normverweis** nennen („Berechnung nach DIN V 18599‑10:2018‑09, Tabelle 7"), Ergebniswerte ausgeben statt Eingangstabellen. Für die sichtbare Nutzeroberfläche bevorzugt EU‑814/2013- und DHWcalc-basierte Profile verwenden.

---

## 12. Gesamtbewertungsmatrix

| Quelle | Nutzungsarten | Auflösung | Jahressim. | Auslegung | Nutzereingaben | Lizenz |
|---|---|---|---|---|---|---|
| DIN V 18599‑10 Tab. 4/7 | WG + 22 NWG-Kategorien | Monat | ●●●○○ (nur Summe) | ○○○○○ | sehr gering | lizenzpflichtig |
| DIN 4708‑1…3 | WG (gemischt belegt) | Auslegungsereignis | ○○○○○ | ●●●●● | mittel–hoch | lizenzpflichtig |
| DIN EN 12831‑3 (+A100) | alle Gebäudearten | Minute | ●●●●○ | ●●●●● | hoch | lizenzpflichtig |
| VDI 6002 Bl. 1 | große Wohngebäude >100 P | h / d / Woche | ●●●●● | ●●○○○ | sehr gering | lizenzpflichtig |
| VDI 6002 Bl. 2 | Studentenwohnheim, Senioren-/Pflegeheim, Krankenhaus, Hallenbad, Camping | h / d / Woche | ●●●●● | ●●○○○ | sehr gering | lizenzpflichtig |
| VO (EU) 814/2013 | Geräteklassen (kein Gebäudetyp) | Minute (feste Zapffolge) | ●○○○○ | ●●○○○ | minimal | **frei** |
| VDI 4655 | EFH ≤6 P, MFH ≤25 WE | 2 s / 1 min / 15 min | ●●●●● | ●●●○○ | gering | lizenzpflichtig |
| DIN 1988‑300 | 7 Gebäudetypen | Momentanwert | ○○○○○ | ●●●●○ (Rohrnetz) | mittel | lizenzpflichtig |
| VDI 6003 | Entnahmestellen-Ebene | Ereignis (s, l/min) | ○○○○○ | ●●●○○ | gering | lizenzpflichtig |
| DVGW W 551/553 | alle | – (Randbedingungen) | – | – (Nebenbedingung) | gering | lizenzpflichtig |
| SIA 2024 / 385/2 | 45 Raumnutzungen / 12 Gebäudekat. | Jahr, Auslegungsereignis | ●●●○○ | ●●●●○ | gering–mittel | lizenzpflichtig |
| ASHRAE HVAC Applications | ~10 Gebäudetypen | Stunde / Tag / Jahr | ●●○○○ | ●●●●○ | gering | lizenzpflichtig |
| IEA SHC Task 26 / DHWcalc | WG (skalierbar) | 1 min / 6 min / 1 h | ●●●●● | ●●●●○ | gering | **frei** |

---

## 13. Konkrete Empfehlung für die Architektur des Zapfprofil-Generators

1. **Zwei-Schichten-Modell trennen:**
   - **Schicht A – Menge (kWh/a bzw. l/d):** DIN V 18599‑10 Tab. 4/7 für die Bilanz (GEG-Konformität), alternativ VDI 6002 Bl. 2 bzw. Logalux Tab. 87 für Nutzungsarten, die 18599 nicht abdeckt.
   - **Schicht B – Form (normiertes Profil, Σ = 1):** Stundenanteile je Wochentagstyp × Wochentagsfaktoren × Jahresgang. Damit ist jede Nutzungsart durch **1 Menge + 1 Formvektor** definiert und die Nutzereingaben bleiben minimal.
2. **Referenztemperatur konsequent führen.** DIN V 18599‑10 rechnet implizit auf **45 °C/ΔT 35 K**, VDI 6002 und Logalux auf **60 °C**, SIA in **Normlitern à 58 Wh**, Ecodesign in **kWh Nutzenergie**. Intern in **kWh Nutzenergie** rechnen und nur an der Oberfläche in Liter umrechnen – das vermeidet die häufigste Fehlerquelle.
3. **Zirkulation als eigenständigen, dauerhaften Grundlastanteil** modellieren (nicht in das Zapfprofil einrechnen): 6,3–14,6 kWh/(m²·a) je nach A_N und Lage (Kap. 7.3), zeitlich als 16–24 h/d Konstantlast mit W‑551‑Temperaturniveau (55–60 °C). Für die WP‑JAZ ist das der dominierende Term.
4. **Für die Auslegung nicht das Jahresprofil verwenden**, sondern separat:
   - Wohngebäude: DIN 4708 N/N_L **oder** DIN EN 12831‑3 Summenlinie
   - NWG: Summenlinienverfahren mit Blockverteilung/seriellem Bedarf (die 5 Bedarfskategorien aus der Logalux-Systematik sind eine gute Klassifikation: Normalverteilung DIN 4708 / Normalverteilung freie Periodendauer / Blockverteilung Dauerbedarf oder Einzelspitze / serieller Bedarf / komplexes Profil)
   - Rohrnetz: DIN 1988‑300
   - Und die VDI‑6002‑Warnung beachten: **Einzeltagesspitzen können nahezu das Doppelte der mittleren Profilmaxima erreichen.**
5. **Diversität stochastisch statt per Faktor.** Zapfereignisse je Wohneinheit unabhängig ziehen (Kategorien/Parameter nach Jordan/Vajen, Entnahmeraten nach VDI 6003) und superponieren. Dann fallen Gleichzeitigkeit und Spitzenlast als Ergebnis an und lassen sich gegen DIN 1988‑300 und DIN 4708 validieren.
6. **Lücken der Regelwerke, die ihr selbst schließen müsst:** Hotel-Tagesgang, Büro-Tagesgang, Schule mit Duschen (Blockcharakter nach Unterrichtsende), Sporthalle (DIN 18032‑1: 40 °C, 9–10 l/min je Dusche, 4 min/Person, 25 Personen je Übungseinheit, Speicher 60 °C, Aufheizzeit 50 min), Kita, Restaurant/Kantine (Blockspitze je Mahlzeit), Fitnessstudio, Sauna. Für diese gibt es **keine** normativen Tagesgänge im deutschen Regelwerk — hier bleibt nur Kombination aus DIN‑V‑18599‑10‑Nutzungszeit + n_SP + ASHRAE‑Maximum‑Hour‑Faktor.

---

## 14. Verwendete Quellen

**Lokal ausgewertet (Gerät):** DIN V 18599‑10:2018‑09 (Beuth Standards Collection); VDI 6002 Blatt 1:2014‑03 (ML 2074726); VDI 6002 Blatt 2:2014‑03 (ML 2074727).

**Online:**
- [Buderus Planungsunterlage Logalux, 6 720 818 349 (2015/10)](https://www.heizungsdiscount24.de/pdf/Buderus-Logalux-Planungsunterlage.pdf) – Ersatz für die nicht extrahierbare 2018er-Ausgabe
- [VO (EU) Nr. 814/2013 (EUR‑Lex)](https://eur-lex.europa.eu/legal-content/DE/ALL/?uri=CELEX:32013R0814)
- [BFE-Bericht „Energieeinsparpotenzial Warmwasserbehälter" (J. Nipkow) – Auszug Anhang VII, Lastprofile](https://pubdb.bfe.admin.ch/de/publication/download/8306)
- [BFE-Bericht „Elektrische Wassererwärmung" (Nipkow) – SIA 385/2 Normliter](https://pubdb.bfe.admin.ch/de/publication/download/7388)
- [BBSR-Online-Publikation 17/2017 – Nutzenergiebedarf Warmwasser Wohngebäude](https://www.bbsr.bund.de/BBSR/DE/veroeffentlichungen/bbsr-online/2017/bbsr-online-17-2017-dl.pdf?__blob=publicationFile&v=2)
- [Jordan/Vajen, IEA SHC Task 26 – Realistic DHW Profiles](https://sel.me.wisc.edu/trnsys/trnlib/iea-shc-task26/iea-shc-task26-load-profiles-description-jordan.pdf)
- [emax haustechnik – DIN 1988-300 Umrechnungstabellen Summen-/Spitzendurchfluss](https://www.emax-haustechnik.de/shop/images/products/media/VG042_Umrechnungstabelle_Durchfluss__sdb-de.pdf)
- [DELTA-Q – Kennwerte Verteilnetze (nach DIN V 4701-10)](https://www.delta-q.de/wp-content/uploads/2021/12/kennwerte_verteilnetze.pdf)
- [DVGW-Arbeitsblatt W 551 – Zusammenfassung (bosy-online)](http://www.bosy-online.de/trinkwasser/dvgw-arbeitsblatt_w551.pdf)
- [TGA Fachplaner – VDI 6003 Komfortkriterien](https://www.tga-fachplaner.de/sites/default/files/ulmer/de-tga/document/file_131900.pdf)
- [IKZ – DIN EN 12831-3 als Ersatz für DIN 4708](https://www.ikz.de/fileadmin/user_upload/008_011.pdf)
- [DIN – Pressemitteilung nationale Ergänzung DIN EN 12831-3](https://www.din.de/resource/blob/297944/8f3257426e8236edfd6990726f704c4f/pressemitteilung-din-en-12831-3-data.pdf)
- [EPB Center – Demo EN 12831-3](https://epb.center/document/demo-en-12831-3/)
- [VDI – Richtlinie VDI 4655](https://www.vdi.de/richtlinien/details/vdi-4655-referenzlastprofile-von-wohngebaeuden-fuer-strom-heizung-und-trinkwarmwasser-sowie-referenzerzeugungsprofile-fuer-fotovoltaikanlagen), [Solarserver](https://www.solarserver.de/2021/07/09/richtlinie-vdi-4655-realistische-energiebedarfe-von-wohngebaeuden-ermitteln/), [Haustec](https://www.haustec.de/heizung/waermeerzeugung/richtlinie-vdi-4655-energiebedarf-von-wohngebaeuden-ermitteln)
- [SIA 2024:2021 Leseprobe](https://shop.sia.ch/16d618e8-b0d7-41e1-b20f-926743792ed7/D/DownloadAnhang), [SIA-Statusbericht Harmonisierung Standardwerte](https://cms.sia.ch/de/api/getMedia/940), [SIA 385/2:2015 Leseprobe](http://shop.sia.ch/cbaa0218-f498-4536-b824-5fc0a7db360a/D/DownloadAnhang)
- [ASHRAE HVAC Applications – DHW Sizing Charts](https://elge-technologies.com/wp-content/uploads/2020/01/domestic-hwh-sizing.pdf)
- [Tuxhorn – Planungsleitfaden tubra-Wohnungsstationen (Gleichzeitigkeit DIN 4708 vs. TU Dresden)](https://www.tuxhorn.de/wp-content/uploads/2022/05/Auswahl-Wohnungsstation.pdf)
- [heatbeat – Trinkwarmwasser-Bedarfsprofile für Fernwärmenetze](https://heatbeat.de/de/blog/2/)
- [BWP – Leitfaden Trinkwassererwärmung (2019)](https://www.waermepumpe.de/uploads/tx_bcpageflip/BWP_LF_TWW_2019_02.pdf)
- [DIN 4708-1 Inhaltsverzeichnis](https://www.bhkw-infozentrum.de/richtlinien/din_4708_teil1_inhalt.pdf), [SBZ Monteur 07/2010 – Bedarfs-/Leistungskennzahl](https://www.sbz-monteur.de/wp-content/uploads/2015/09/F%C3%BCr-Warmduscher-geeignet_07.2010.pdf), [BauNetz Wissen – Warmwasserbedarf Wohngebäude](https://www.baunetzwissen.de/heizung/fachwissen/warmwasser/warmwasserbedarf-fuer-wohngebaeude-161292)
- [DELTA-Q – Details zur Neuausgabe DIN V 18599 (2018)](https://www.delta-q.de/wp-content/uploads/2018-DIN-V-18599-Beschreibung.pdf)

---

## 15. Verbleibende Unsicherheiten / offene Punkte

- **Nicht extrahiert:** Die Tagesgang-**Diagramme** aus VDI 6002 Bl. 1 (Bild D1–D5) und Bl. 2 (Bild 8, 9, 10, 16, 17) enthalten keine Zahlen im PDF-Textlayer. Ich hatte die betreffenden Seiten bereits als PNG gerendert (`/tmp/tww/b2-25/26/28/29.png` auf dem Gerät), konnte sie aber wegen des Verbindungsabbruchs nicht mehr visuell auslesen. **Das wäre der nächste Schritt** – daraus ließen sich die Stundenanteile für Seniorenheim (Werktag/Sa/So), Krankenhaus (alle Tage gleich) und Camping ablesen.
- **Logalux 2018:** Ich habe die 2015er-Ausgabe ausgewertet. Tabellennummern und Werte sollten identisch sein, die Seitenzahlen weichen ab. Bitte gegen euer PDF gegenprüfen, insbesondere Tab. 87 (Richtwerte Warmwasser-/Wärmemengenbedarf).
- **VDI 6002 Bl. 2 Tab. 5 (Krankenhaus):** Auslegungswert 33 l/(vp·d) vs. informative Bezugsgröße 35 l/(vp·d) – Widerspruch im Normtext, bitte nachprüfen.
- **DIN EN 12831-3 Anhang B:** Die konkreten Stundenzapfprofile je Gebäudeart konnte ich ohne Normlizenz nicht verifizieren. Das ist die wichtigste noch zu beschaffende Quelle, wenn ihr NWG-Tagesgänge normativ belegen wollt.
- **DIN 4708-2 Zapfperioden-Tabelle (N = 1…300):** nur über Normlizenz verfügbar; die Herstellerunterlagen geben nur das Prinzip wieder, nicht die Wertetabelle.
- **SIA 385/2 Anhang A** (Nutzwarmwasserbedarf je Gebäudekategorie in l/(P·d)) ist hinter der Normlizenz; ich konnte nur den Wohnbau-Wert (35–45 Normliter) über den BFE-Bericht belegen.
- Die Umrechnung der DIN-V-18599-10-Tab.-7-Werte in Liter (Kap. 1.3) beruht auf der aus der Bagatellklausel **abgeleiteten** Bezugstemperatur 45 °C/ΔT 35 K – die Norm nennt sie nicht explizit als allgemeine Bezugsgröße.agentId: a69fe3ff0d48c9281 (use SendMessage with to: 'a69fe3ff0d48c9281', summary: '<5-10 word recap>' to continue this agent)
<usage>subagent_tokens: 285838
tool_uses: 163
duration_ms: 2163506</usage>