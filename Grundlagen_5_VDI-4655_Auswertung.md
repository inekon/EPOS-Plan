# Fachbericht: Auswertung VDI 4655 (Ausgabe Juli 2021) für das TWW-Zapfprofil-Modul in WP-Plan

**Erstellt für:** INEKON – interne technische Bewertung
**Quelle:** `/root/.claude/uploads/abe216b5-16df-50de-9ac2-cba4717d4cac/c2b17d98-VDI_4655__VDI_e.V_.pdf` (52 Seiten, vollständig gelesen in 3 Leseoperationen)
**Stand:** 29.07.2026

---

## 0. Urheberrechtshinweis (bitte zuerst lesen)

Die ausgewertete Datei ist ein lizenziertes Exemplar aus der *BEST BeuthStandardsCollection* (Fußzeilenvermerk „Stand 2022-01"). Auf **jedem Seitenrand** steht wörtlich:

> „Alle Rechte vorbehalten © Verein Deutscher Ingenieure e.V., Düsseldorf 2021 … **Vervielfältigung – auch für innerbetriebliche Zwecke – nicht gestattet** / Reproduction – even for internal use – not permitted"

Ferner Vorbemerkung (S. 2): „Alle Rechte, insbesondere die des Nachdrucks, der Fotokopie, der elektronischen Verwendung und der Übersetzung, jeweils auszugsweise oder vollständig, sind vorbehalten. Die Nutzung dieser Richtlinie ist unter Wahrung des Urheberrechts und unter Beachtung der Lizenzbedingungen … möglich."

**Konsequenzen für INEKON:**
1. Dieser Bericht ist ein **internes Arbeitsdokument** zur Erschließung der Norm. Die nachstehend zitierten Zahlenwerte dienen der technischen Bewertung, nicht der Weiterverbreitung. Keine Weitergabe an Dritte, keine Veröffentlichung, keine Anlage zu Kundenberichten.
2. Eine **Einbettung der Referenzlastprofil-Datensätze (CD-ROM-Inhalte) oder der Faktortabellen in das auslieferbare Produkt WP-Plan ist nach diesem Vermerk nicht durch die Leselizenz gedeckt.** Dafür ist eine gesonderte Verwertungs-/Softwarelizenz von VDI bzw. Beuth einzuholen. Dieser Punkt ist vor jeder Implementierungsentscheidung zu klären (siehe Abschnitt 7.6).

---

## 1. Ausgabestand

| Merkmal | Befund | Fundstelle |
|---|---|---|
| Nummer / Ausgabe | **VDI 4655, Juli 2021** (July 2021) | Titelseite, S. 1 |
| Titel deutsch | „Referenzlastprofile von Wohngebäuden für Strom, Heizung und Trinkwarmwasser sowie Referenzerzeugungsprofile für Fotovoltaikanlagen" | Titelseite |
| Titel englisch | „Reference load profiles of residential buildings for power, heat, and domestic hot water as well as reference generation profiles for photovoltaic plants" | Titelseite |
| Sprachfassung | Ausg. deutsch/englisch; **„Die deutsche Version dieser Richtlinie ist verbindlich."** | Titelseite |
| Frühere Ausgaben | **„05.08; VDI 4655 Blatt 1: 2019-09 Entwurf, deutsch"** | Randspalte Titelseite |
| ICS | 27.010, 91.120.10 | Titelseite |
| Herausgeber-Gremium | VDI-Gesellschaft Energie und Umwelt (GEU), Fachbereich Energietechnik; VDI-Handbuch Energietechnik / VDI-Handbuch Wärme-/Heiztechnik | Titelseite |

**Antwort auf die Ausgangsfrage:** Es liegt die **Neufassung 2021-07** vor, **nicht** die Ausgabe 2008-05.

**Bezug zur Blattstruktur:** Der Vorgänger hieß *VDI 4655 Blatt 1* (Ausgabe 2008-05, Entwurf 2019-09). Die vorliegende Ausgabe trägt **keine Blattnummer mehr** – sie firmiert als „VDI 4655". Die Richtlinienreihe existiert weiter; S. 2: „Eine Liste der aktuell verfügbaren und in Bearbeitung befindlichen Blätter dieser Richtlinienreihe … sind im Internet abrufbar unter www.vdi.de/4655." Welche weiteren Blätter derzeit bestehen, ist dem Dokument **nicht** zu entnehmen (nicht abgedruckt).

**Wesentliche Neuerungen gegenüber 2008 (soweit aus dem Text der Ausgabe 2021 selbst belegbar):**
- Zusätzliche **Zwei-Sekunden-Messdaten** von fünf Bestands-EFH und fünf Niedrigenergiehäusern durch das DLR Institut für Vernetzte Energiesysteme, Forschungsvorhaben **NOVAREF** [1] (Einleitung, S. 2).
- Erstmals **Referenzlastprofile für Niedrigenergiegebäude (NEH)** zusätzlich zu Bestandsgebäuden (Einleitung, S. 2; Abschnitt 5.2).
- Erstmals **PV-Referenzerzeugungsprofile** und die daraus folgende Erweiterung auf **zwölf** Typtagkategorien (Abschnitt 7; Tabelle 2, S. 5).
- Nutzung der **aktualisierten TRY2017-Datensätze** des DWD für die 15 Repräsentanzstationen (Abschnitt 5, S. 13).

---

## 2. Methodik vollständig

### 2.1 Anwendungsbereich und Geltungsgrenzen (Abschnitt 1, S. 3)

> „Diese Richtlinie findet Anwendung für die Deckung des Bedarfs von Strom, Heizwärme und Trinkwarmwasser in Wohngebäuden. Für Einfamilienhäuser existieren Datensätze für Bestandsgebäude und Niedrigenergiehäuser, für Mehrfamilienhäuser nur im Bestand. **Der Geltungsbereich erstreckt sich bei Einfamilienhäusern auf eine maximale Anzahl von sechs Personen und bei Mehrfamilienhäusern auf bis zu 25 Wohneinheiten.**"

### 2.2 Typtag-Systematik

**Definitionen (Abschnitt 2, S. 3–6):**

| Begriff | Definition laut Norm |
|---|---|
| Bestandsgebäude | Gebäude mit einer Heizgrenztemperatur von **15 °C** |
| Niedrigenergiehaus (NEH) | Gebäude mit einer Heizgrenztemperatur von **12 °C** |
| Heizgrenztemperatur (HG) | Tagesmitteltemperatur, ab der ein Gebäude beheizt werden muss |
| **Sommertag** | Tag mit einer Tagesmitteltemperatur **über der Heizgrenztemperatur** |
| **Übergangstag** | Tag mit einer Tagesmitteltemperatur **zwischen 5 °C und der Heizgrenztemperatur** |
| **Wintertag** | Tag mit einer Tagesmitteltemperatur **unter 5 °C** |
| Tagesmitteltemperatur | Mittelwert der Temperatur über 24 h; maßgeblich Lufttemperatur 2 m über Erdboden nach DIN 4710 |
| **bewölkt (B)** | Bedeckungsgrad, dessen Tagesmittel über 24 h (DWD) **≥ 5/8** |
| **heiter (H)** | Bedeckungsgrad, dessen Tagesmittel über 24 h (DWD) **< 5/8** |
| **Werktag (W)** | Montag bis Samstag, solange diese Tage nicht auf einen gesetzlichen Feiertag fallen |
| **Sonntag (S)** | der letzte Tag der Woche. **Anmerkung 2: „Auch alle gesetzlichen Feiertage werden als Sonntage in der Richtlinie betrachtet."** |
| Sieben-Tage-Mittel | gleitender arithmetischer Mittelwert der Außentemperatur über den betreffenden Tag + die sechs vorhergehenden Tage. Anmerkung: „Mit diesem Wert wird der Einfluss der thermischen Trägheit der Gebäudehülle berücksichtigt." |
| **Referenzlastprofil** | „typischer Verlauf der energetischen Lastgänge aller Tage einer Typtagkategorie" |
| Urlaubstag | Tag mit konstantem Strombedarf i.H.v. **50 %** des Tagesbedarfs eines normal bewohnten Tages gleicher Typtagkategorie; **kein Warmwasserbedarf**; Heizwärme Übergangs-/Winter-Urlaubstag konstant bei **80 %**; Sommer-Urlaubstag kein Heizwärmebedarf |

**Zentrale, für die WP-Plan-Bewertung entscheidende Anmerkung (S. 4, Begriff „Referenzlastprofil", Anmerkung 1):**

> „Ein Referenzlastprofil ist **die Auswahl desjenigen Profils aus der Menge der Tage aus der jeweiligen Typtagkategorie, das dem typischen Verlauf der gemessenen Tage am nächsten kommt**. Somit ist gewährleistet, dass die Charakteristika eines Profils erhalten bleiben und **nicht durch Mittelwertbildung und die damit verbundene Glättung verloren gehen**."

→ **Die Formvektoren sind also ausgewählte reale Einzeltage, keine arithmetischen Mittelwertprofile.** Das ist eine wichtige Präzisierung gegenüber der bisherigen INEKON-Annahme (siehe Abschnitt 7.5).

**Typtagkategorien (Tabelle 1, S. 5) – 10 Kategorien ohne PV:**

| Jahreszeit | Werktag heiter | Werktag bewölkt | Sonntag heiter | Sonntag bewölkt |
|---|---|---|---|---|
| Übergang (Ü) | ÜWH | ÜWB | ÜSH | ÜSB |
| Sommer (S) | SWX (nicht nach Bewölkung unterteilt) | SWX | SSX | SSX |
| Winter (W) | WWH | WWB | WSH | WSB |

Anmerkung 1 zu Tabelle 1: „Für die Sommertyptage ist keine Unterscheidung nach der Bewölkung vorgenommen worden, da an den Sommertagen der Heizwärmebedarf sehr gering und nicht von der Bewölkung abhängig ist."

**Tabelle 2 (S. 5) – 12 Kategorien mit PV:** Sommer wird zusätzlich in SWH/SWB/SSH/SSB aufgeteilt, weil „heitere und bewölkte Tage im Sommer durchaus unterschiedliche Charakteristika haben" (Anmerkung 2).

### 2.3 Klimazonen / TRY-Regionen (Abschnitt 5, S. 10–13)

- **15 Klimazonen** nach DIN 4710, VDI 4710 Blatt 2, DIN EN 12831 Beiblatt 1 (Bild 2 Zonenkarte S. 11; Tabelle 3 S. 12).
- Repräsentanzstationen und Jahresmittel der Außentemperatur (Tabelle 3, S. 12):

| Zone | Bezeichnung | Station | ϑm,e [°C] |
|---|---|---|---|
| 1 | Nordseeküste | Bremerhaven | 9,0 |
| 2 | Ostseeküste | Rostock-Warnemünde | 8,4 |
| 3 | Nordwestdeutsches Tiefland | Hamburg-Fuhlsbüttel | 8,5 |
| 4 | Nordostdeutsches Tiefland | Potsdam | 9,5 |
| 5 | Nordrhein-westfälische Bucht und Emsland | Essen | 8,1 |
| 6 | Nördliche/westliche Mittelgebirge, Randgebiete | Bad Marienberg | 6,8 |
| 7 | Nördliche/westliche Mittelgebirge, zentrale Bereiche | Kassel | 8,8 |
| 8 | Oberharz und Schwarzwald (mittlere Lage) | Braunlage | 6,0 |
| 9 | Thüringer Becken und sächsisches Hügelland | Chemnitz | 7,9 |
| 10 | Südöstliche Mittelgebirge bis 1000 m | Hof | 6,3 |
| 11 | Erzgebirge, Böhmer- und Schwarzwald oberhalb 1000 m | Fichtelberg | 3,0 |
| 12 | Oberrheingraben und unteres Neckartal | Mannheim | 10,2 |
| 13 | Schwäbisch-fränkisches Stufenland und Alpenvorland | Passau | 7,9 |
| 14 | Schwäbische Alb und Baar | Stötten | 6,8 |
| 15 | Alpenrand und Täler | Garmisch-Partenkirchen | 6,8 |

- **Wichtiger normativer Hinweis (S. 10/13):** „Der DWD hat das Konzept der Einteilung Deutschlands in 15 TRY-Klimazonen mit je einer Repräsentanzstation … aufgegeben. Anstelle der Datensätze für 15 TRY-Klimazonen stellt der DWD in der aktualisierten Version TRY-Datensätze für jeden km² … zur Verfügung." Und weiter: „Für die vergleichende Betrachtung von Anlagen nach VDI 4655 wird jedoch das Verfahren mit 15 TRY-Klimazonen beibehalten, weil es den Einfluss des Gebäudestandorts mit hinreichender Genauigkeit abbildet." Ausdrücklich eingeräumt: „Für die Planung und Auslegung von heiz- und raumlufttechnische Anlagen sowie für energetische Bilanzierungen bietet das Verfahren der Aufteilung Deutschlands in km²-große Flächen eine höhere Genauigkeit."
- **Standortzuordnung** über Ort/PLZ: **Tabelle A1 (S. 44–50)**, „Einordnung einiger Städte in die einzelnen Klimazonen (in Anlehnung an DIN EN 12831 Beiblatt 1)" – ca. 700 Orte mit PLZ → Zonennummer. Städte mit mehreren PLZ sind mit der niedrigsten PLZ und einem `*` eingetragen. Für nicht gelistete Orte: Zuordnung über die nächstgelegene gelistete Stadt (Beispiel Abschnitt 8.3, S. 26: Wetter/58300 → über Hagen 6 km und Witten 8 km → TRY05).

### 2.4 Anzahl Typtage je Zone

Randbedingungen aller Verteilungen (Abschnitte 5.1/5.2, S. 13):
- TRY-Datensatz: **TRY2017**
- **Kalenderjahr (Wochentagzuordnung): 2014** → 365 Tage, **kein Schaltjahr**
- Grenze Sommer–Übergang = Heizgrenztemperatur: **15 °C** (Bestand) bzw. **12 °C** (NEH)
- Grenze Übergang–Winter: **5 °C**

**Tabelle 4 (S. 14) – Anzahl Typtage, Bestandsgebäude:**

| Zone | ÜWH | ÜWB | ÜSH | ÜSB | SWX | SSX | WWH | WWB | WSH | WSB | Heiztage |
|---|---|---|---|---|---|---|---|---|---|---|---|
| TRY01 | 47 | 74 | 13 | 13 | 95 | 17 | 17 | 71 | 3 | 15 | 253 |
| TRY02 | 56 | 64 | 16 | 13 | 82 | 13 | 30 | 71 | 6 | 14 | 270 |
| TRY03 | 50 | 78 | 12 | 14 | 81 | 16 | 30 | 65 | 6 | 13 | 268 |
| TRY04 | 35 | 63 | 11 | 12 | 99 | 19 | 31 | 75 | 10 | 10 | 247 |
| TRY05 | 39 | 95 | 6 | 21 | 98 | 20 | 12 | 58 | 3 | 13 | 247 |
| TRY06 | 55 | 84 | 11 | 19 | 50 | 11 | 23 | 90 | 6 | 16 | 304 |
| TRY07 | 40 | 90 | 12 | 14 | 78 | 17 | 18 | 77 | 4 | 15 | 270 |
| TRY08 | 40 | 86 | 8 | 19 | 47 | 7 | 24 | 107 | 8 | 19 | 311 |
| TRY09 | 55 | 64 | 8 | 18 | 71 | 16 | 28 | 84 | 4 | 17 | 278 |
| TRY10 | 47 | 65 | 16 | 12 | 56 | 9 | 32 | 101 | 8 | 19 | 300 |
| TRY11 | 53 | 86 | 10 | 18 | 20 | 4 | 39 | 104 | 11 | 20 | 341 |
| TRY12 | 50 | 70 | 8 | 18 | 110 | 21 | 21 | 50 | 7 | 10 | 234 |
| TRY13 | 41 | 70 | 13 | 13 | 78 | 14 | 29 | 83 | 3 | 21 | 273 |
| TRY14 | 47 | 66 | 10 | 19 | 71 | 11 | 30 | 87 | 5 | 19 | 283 |
| TRY15 | 42 | 71 | 6 | 18 | 62 | 14 | 49 | 77 | 10 | 16 | 289 |

Prüfsumme: Zeilensumme = 365; Heiztage = Summe ohne SWX/SSX (verifiziert für TRY01).

**Tabelle 6 (S. 15) – Anzahl Typtage, Niedrigenergiehäuser:**

| Zone | ÜWH | ÜWB | ÜSH | ÜSB | SWX | SSX | WWH | WWB | WSH | WSB | Heiztage |
|---|---|---|---|---|---|---|---|---|---|---|---|
| TRY01 | 28 | 57 | 10 | 7 | 131 | 26 | 17 | 71 | 3 | 15 | 208 |
| TRY02 | 43 | 31 | 9 | 9 | 128 | 24 | 30 | 71 | 6 | 14 | 213 |
| TRY03 | 33 | 48 | 9 | 7 | 128 | 26 | 30 | 65 | 6 | 13 | 211 |
| TRY04 | 19 | 49 | 7 | 9 | 129 | 26 | 31 | 75 | 10 | 10 | 210 |
| TRY05 | 27 | 67 | 2 | 17 | 138 | 28 | 12 | 58 | 3 | 13 | 199 |
| TRY06 | 34 | 56 | 10 | 13 | 99 | 18 | 23 | 90 | 6 | 16 | 248 |
| TRY07 | 23 | 58 | 10 | 7 | 127 | 26 | 18 | 77 | 4 | 15 | 212 |
| TRY08 | 28 | 62 | 5 | 13 | 83 | 16 | 24 | 107 | 8 | 19 | 266 |
| TRY09 | 29 | 41 | 7 | 13 | 120 | 22 | 28 | 84 | 4 | 17 | 223 |
| TRY10 | 28 | 37 | 11 | 6 | 103 | 20 | 32 | 101 | 8 | 19 | 242 |
| TRY11 | 31 | 58 | 7 | 14 | 70 | 11 | 39 | 104 | 11 | 20 | 284 |
| TRY12 | 31 | 49 | 5 | 11 | 150 | 31 | 21 | 50 | 7 | 10 | 184 |
| TRY13 | 23 | 39 | 8 | 7 | 127 | 25 | 29 | 83 | 3 | 21 | 213 |
| TRY14 | 35 | 44 | 9 | 10 | 105 | 21 | 30 | 87 | 5 | 19 | 239 |
| TRY15 | 24 | 39 | 5 | 10 | 112 | 23 | 49 | 77 | 10 | 16 | 230 |

Zusätzlich: **Tabelle 9** (Bestand mit PV, 12 Kategorien, S. 22) und **Tabelle 11** (NEH mit PV, S. 23); **Tabellen 5, 7, 10, 12** enthalten die zugehörigen mittleren Außentemperaturen (Sieben-Tage-Mittel) je Typtag und Zone.

### 2.5 Konstruktion des Jahresprofils

Zweistufig:

1. **Tagesenergie je Typtag** (Abschnitt 6.4, S. 18), Gleichungen (1)–(3):

   ```
   Q_Heiz,TT = Q_Heiz,a · F_Heiz,TT                                (1)
   W_TT      = W_a      · ( 1/365 + N_Pers/WE · F_el,TT )          (2)
   Q_TWE,TT  = Q_TWE,a  · ( 1/365 + N_Pers/WE · F_TWE,TT )         (3)
   ```
   `N_Pers` = Personenzahl (EFH), `N_WE` = Anzahl Wohneinheiten (MFH).

   **Beachten:** Nur Strom und TWW haben den Term `1/365 + N·F`; die **Heizung skaliert rein über F_Heiz,TT ohne Personen-/WE-Term**.

   Anmerkung zu (1)–(3): „Die Faktoren F_el,TT und F_TWE,TT sind teilweise negativ, da sie **eine Schwankung um einen Jahresmittelwert** darstellen. Die Werte für die Tagesbedarfe … bleiben in der Regel positiv. **In Einzelfällen kann die Berechnung für die Typtagkategorie SWX zu einem negativen Wert beim Trinkwassererwärmungsbedarf führen. In diesem Fall ist der Faktor F_TWE,SWX = 0 zu setzen.**"

2. **Zeitverlauf über den Tag** (Abschnitt 6.4, S. 19), Gleichungen (4)–(6):

   ```
   Q_Heiz,TT(t) = F_Heiz,n,TT(t) · Q_Heiz,TT                       (4)
   W_TT(t)      = F_el,n,TT(t)   · W_TT                            (5)
   Q_TWE,TT(t)  = F_TWE,n,TT(t)  · Q_TWE,TT                        (6)
   ```
   „Die Referenzlastprofile sind als Quotienten aus momentanem Energiebedarf und Tagesenergiebedarf getrennt für jede der drei Energieformen angegeben." Die normierten Werte sind dimensionslos; zusätzlich liegen **kumulierte** Varianten (Indizes n, k) vor.

3. **Zuordnungsregeln / Jahresgang** (Abschnitte 6.5/6.6, S. 21): „Die Summe aller Tagesbedarfe des Kalenderjahres ergibt den … Jahresbedarf … Entsprechend den Angaben in Abschnitt 9 hat ein Kalenderjahr die dort für die jeweilige TRY-Klimazone genannte Anzahl an Tagen in der entsprechenden Typtagkategorie." Und: „Die vorliegenden Tageslastgänge lassen sich aneinanderreihen, um z.B. Wochen, Jahreszeiten oder Jahre zu simulieren. Hierzu lassen sich auch Urlaubstage … berücksichtigen."
   Die **konkrete Reihenfolge der Typtage im Jahr** liegt nicht im Papierteil, sondern auf der CD-ROM: Anhang, Ordner B1–B4 enthalten „Anzahl Typtage pro Typtagkategorie / Außentemperaturen (Sieben-Tage-Mittel) in °C / **Reihenfolge der Typtage**" (S. 42).

### 2.6 Zeitauflösung (Abschnitt 6.4, S. 19; Anhang D, S. 43)

| Gebäudetyp | verfügbare Zeitschritte |
|---|---|
| EFH Bestand | **2 s, 1 min, 15 min** |
| EFH Niedrigenergiehaus | **2 s, 1 min, 15 min** |
| **MFH Bestand** | **nur 15 min** |
| PV-Erzeugungsprofile | Beispiel Tabelle 13 zeigt 1-min-Raster (Ordner E, „PV-Leistung, normiert") |

Wörtlich S. 19: „Im Einfamilienhaus für Bestand und Niedrigenergiehäuser sind dies Zeitabschnitte von 2 s, 1 min und 15 min, **im Mehrfamilienhaus von 15 min**."

### 2.7 EFH vs. MFH – Datenbasis und Unterschiede

**Datenbasis (Einleitung, S. 2):**
> „Dem Richtlinienausschuss wurden existierende ein- bis zweijährige Messdaten von Ein- und Mehrfamilienhäusern zur Verfügung gestellt – Strom-, Heizwärme- und Trinkwarmwasserlastgänge. Die Messdaten von Ein- oder Zweifamilienhäusern liegen als Minutenmittelwerte, die Messdaten in Mehrfamilienhäusern als 15-Minuten-Mittelwerte vor. Im Rahmen der turnusmäßigen Überprüfung der Richtlinie wurden zusätzlich Zwei-Sekunden-Messdaten von **fünf Einfamilienhäusern im Bestand und fünf Niedrigenergiehäusern** vom DLR Institut für Vernetzte Energiesysteme (siehe Forschungsvorhaben NOVAREF [1]) bereitgestellt."

**⚠ Wichtige Informationslücke:** Die **Gesamtzahl der vermessenen Gebäude** wird im Normtext **nicht genannt**. Nur die 5 + 5 Gebäude der NOVAREF-Nacherhebung sind beziffert. Für Mehrfamilienhäuser wird **weder die Anzahl der Messobjekte noch deren Größe (Anzahl WE je Messobjekt) angegeben**. Diese Lücke ist für die Spitzenlastbewertung zentral (siehe Abschnitt 7.5). Die Methodik der Profilermittlung ist nicht im Normtext, sondern extern beschrieben: Anmerkung 2 zu „Referenzlastprofil": „Die Vorgehensweise bei der Ermittlung der Referenzlastprofile ist in [2] ausführlich beschrieben." ([2] = Dubielzig, G. et al.: *Referenzlastprofile von Ein- und Mehrfamilienhäusern für den Einsatz von KWK-Anlagen*, Fortschr.-Ber. VDI, Reihe 6, Nr. 560, VDI Verlag 2007, ISBN 978-3-18-356006-6 – **also eine Quelle aus 2007, d.h. der Datenbestand der Ausgabe 2008**.)

**Auswertung/Erstellung:** „Analyse und Auswertung der Messdaten sowie die Ermittlung von Referenzlastprofilen wurden vom Gas- und Wärme-Institut Essen e.V. und vom DLR Institut für Vernetzte Energiesysteme geleistet." (S. 3)

**Definitionen der Gebäudetypen (Abschnitt 6.1, S. 16):**
- **EFH:** „Wohngebäude mit maximal sechs Personen in bis zu **zwei Wohneinheiten** und **einer gemeinsamen Heizungs- und TWE-Anlage**. Sofern Gebäude mit zwei Wohneinheiten eine getrennte Heizungsanlage haben, sind sie jeweils als separate Einfamilienhäuser zu betrachten."
- **MFH:** „Wohngebäude oder Teile von Wohngebäuden mit **mindestens drei bis zu 25 Wohneinheiten** und **einer gemeinsamen Heizungs- und TWE-Anlage**."

**Strukturelle Unterschiede EFH/MFH:**

| | EFH | MFH |
|---|---|---|
| Bestand | ja | ja |
| NEH | ja | **nein** |
| Skalierungsgröße | Personen (max. 6) | Wohneinheiten (3–25) |
| Zeitauflösung Profil | 2 s / 1 min / 15 min | **nur 15 min** |
| Zusätzlicher Faktor F_el,vent,TT | nur NEH-Tabellen | nein |
| Faktor-Tabellen | Tab. 17–31 (Bestand), Tab. 32–46 (NEH) | Tab. 17–31 (Bestand) |

---

## 3. TWW-spezifische Auswertung (Kernteil)

### 3.1 Bilanzgrenze Trinkwassererwärmung (Abschnitt 4, S. 9)

> **„Trinkwassererwärmung** – Eingeschlossen ist der Wärmebedarf sämtlicher Zapfstellen einschließlich der Verteilungs- und Leitungsverluste. **Verluste aufgrund einer Trinkwarmwasserzirkulation sind ebenfalls eingeschlossen.**
> **Nicht eingeschlossen** sind Wärmeverluste eines Warmwasser- oder Kombispeichers. Der Strombedarf einer Zirkulationspumpe wird im Referenzlastprofil für den elektrischen Strom berücksichtigt."

Vergleich Heizwärme (S. 9): „Eingeschlossen ist der gesamte Heizwärmebedarf inklusive aller Verteilungs- und Leitungsverluste. Nicht eingeschlossen sind Wärmeverluste eines Puffer- oder Kombispeichers."

**Wichtig für WP-Plan:** Q_TWE nach VDI 4655 ist **kein Nutzenergiewert an der Zapfstelle**, sondern enthält Verteil-, Leitungs- und **Zirkulationsverluste** – aber **ohne Speicherverluste**. Ein direkter Zahlenvergleich mit DIN V 18599 oder DIN EN 12831-3 erfordert Angleichung der Bilanzgrenzen.

### 3.2 Jahres-TWW-Bedarf (Abschnitt 6.2.3, S. 17)

> „Für die Ermittlung des Energiebedarfs für die Trinkwassererwärmung kann die EnEV … herangezogen werden. **Aus den Auswertungen der für diese Richtlinie zugrunde gelegten Messdaten hat sich für den Jahresbedarf ergeben:**
> - **500 kWh/Pers im Einfamilienhaus**
> - **1000 kWh/WE im Mehrfamilienhaus**"

Abgeleitete Tagesmittelwerte (eigene Rechnung, 1/365):
- EFH: **1,370 kWh/(Pers·d)**
- MFH: **2,740 kWh/(WE·d)**

**Bemerkenswert:** Der MFH-Wert ist **personenzahl-unabhängig** je WE definiert. Eine WE zählt damit implizit wie 2 Personen. Es gibt **keine** Differenzierung nach Wohnungsgröße, Belegung, Zirkulationssystem oder Baualter.

### 3.3 Faktoren F_TWE,TT je Typtag – vollständige Erfassung

Reihenfolge der Spalten stets: **ÜWH | ÜWB | ÜSH | ÜSB | SWX | SSX | WWH | WWB | WSH | WSB**
Quelle: Tabellen 17–31 (Bestand, S. 29–36) bzw. Tabellen 32–46 (NEH, S. 36–41).

#### 3.3.1 F_TWE,TT — **EFH Bestand** (Tabellen 17–31)

| Zone | ÜWH | ÜWB | ÜSH | ÜSB | SWX | SSX | WWH | WWB | WSH | WSB |
|---|---|---|---|---|---|---|---|---|---|---|
| TRY01 | 4,9168E-06 | 2,0373E-05 | 3,1152E-04 | −1,2077E-05 | −1,6449E-04 | −2,8687E-05 | −4,4219E-07 | 9,0113E-05 | 1,8521E-04 | 2,3578E-04 |
| TRY02 | −4,2261E-06 | 1,1178E-05 | 3,0136E-04 | −2,1163E-05 | −1,7307E-04 | −3,7718E-05 | −9,5672E-06 | 8,0686E-05 | 1,7547E-04 | 2,2586E-04 |
| TRY03 | 4,1789E-07 | 1,5848E-05 | 3,0652E-04 | −1,6548E-05 | −1,6871E-04 | −3,3130E-05 | −4,9323E-06 | 8,5475E-05 | 1,8042E-04 | 2,3090E-04 |
| TRY04 | 8,0425E-06 | 2,3516E-05 | 3,1500E-04 | −8,9703E-06 | −1,6156E-04 | −2,5599E-05 | 2,6774E-06 | 9,3336E-05 | 1,8854E-04 | 2,3916E-04 |
| TRY05 | 1,6241E-05 | 3,1761E-05 | 3,2411E-04 | −8,2218E-07 | −1,5386E-04 | −1,7501E-05 | 1,0860E-05 | 1,0179E-04 | 1,9728E-04 | 2,4805E-04 |
| TRY06 | −2,1242E-05 | −5,9336E-06 | 2,8244E-04 | −3,8074E-05 | −1,8903E-04 | −5,4525E-05 | −2,6550E-05 | 6,3142E-05 | 1,5733E-04 | 2,0742E-04 |
| TRY07 | −4,6280E-06 | 1,0774E-05 | 3,0091E-04 | −2,1562E-05 | −1,7345E-04 | −3,8114E-05 | −9,9683E-06 | 8,0272E-05 | 1,7504E-04 | 2,2543E-04 |
| TRY08 | −2,7287E-05 | −1,2012E-05 | 2,7572E-04 | −4,4081E-05 | −1,9471E-04 | −6,0496E-05 | −3,2583E-05 | 5,6910E-05 | 1,5089E-04 | 2,0086E-04 |
| TRY09 | −6,0655E-06 | 9,3286E-06 | 2,9931E-04 | −2,2991E-05 | −1,7479E-04 | −3,9534E-05 | −1,1403E-05 | 7,8790E-05 | 1,7351E-04 | 2,2387E-04 |
| TRY10 | −2,7558E-05 | −1,2285E-05 | 2,7542E-04 | −4,4350E-05 | −1,9496E-04 | −6,0764E-05 | −3,2854E-05 | 5,6630E-05 | 1,5060E-04 | 2,0057E-04 |
| TRY11 | −4,2589E-05 | −2,7400E-05 | 2,5871E-04 | −5,9288E-05 | −2,0906E-04 | −7,5610E-05 | −4,7855E-05 | 4,1133E-05 | 1,3458E-04 | 1,8427E-04 |
| TRY12 | 2,3149E-05 | 3,8708E-05 | 3,3179E-04 | 6,0427E-06 | −1,4738E-04 | −1,0678E-05 | 1,7754E-05 | 1,0891E-04 | 2,0464E-04 | 2,5554E-04 |
| TRY13 | −9,4502E-06 | 5,9248E-06 | 2,9555E-04 | −2,6355E-05 | −1,7797E-04 | −4,2878E-05 | −1,4781E-05 | 7,5300E-05 | 1,6990E-04 | 2,2020E-04 |
| TRY14 | −1,0638E-05 | 4,7308E-06 | 2,9423E-04 | −2,7535E-05 | −1,7908E-04 | −4,4050E-05 | −1,5966E-05 | 7,4076E-05 | 1,6863E-04 | 2,1891E-04 |
| TRY15 | −9,4072E-06 | 5,9680E-06 | 2,9560E-04 | −2,6312E-05 | −1,7793E-04 | −4,2835E-05 | −1,4738E-05 | 7,5344E-05 | 1,6995E-04 | 2,2025E-04 |

#### 3.3.2 F_TWE,TT — **MFH Bestand** (Tabellen 17–31, jeweils unterer Block)

| Zone | ÜWH | ÜWB | ÜSH | ÜSB | SWX | SSX | WWH | WWB | WSH | WSB |
|---|---|---|---|---|---|---|---|---|---|---|
| TRY01 | 1,8209E-05 | 9,2697E-06 | 9,2697E-06 | 2,2184E-05 | −4,8345E-05 | −3,9405E-05 | 3,4104E-05 | 3,3110E-05 | 2,6157E-05 | 2,0197E-05 |
| TRY02 | 1,4390E-05 | 5,4628E-06 | 5,4628E-06 | 1,8359E-05 | −5,2072E-05 | −4,3145E-05 | 3,0263E-05 | 2,9270E-05 | 2,2327E-05 | 1,6375E-05 |
| TRY03 | 1,5168E-05 | 6,2391E-06 | 6,2391E-06 | 1,9139E-05 | −5,1312E-05 | −4,2382E-05 | 3,1046E-05 | 3,0053E-05 | 2,3108E-05 | 1,7154E-05 |
| TRY04 | 1,8048E-05 | 9,1085E-06 | 9,1085E-06 | 2,2022E-05 | −4,8503E-05 | −3,9563E-05 | 3,3941E-05 | 3,2948E-05 | 2,5995E-05 | 2,0035E-05 |
| TRY05 | 2,0260E-05 | 1,1314E-05 | 1,1314E-05 | 2,4237E-05 | −4,6344E-05 | −3,7397E-05 | 3,6166E-05 | 3,5172E-05 | 2,8214E-05 | 2,2249E-05 |
| TRY06 | 8,0501E-06 | −8,5675E-07 | −8,5675E-07 | 1,2010E-05 | −5,8260E-05 | −4,9352E-05 | 2,3886E-05 | 2,2896E-05 | 1,5969E-05 | 1,0030E-05 |
| TRY07 | 1,5139E-05 | 6,2092E-06 | 6,2092E-06 | 1,9109E-05 | −5,1342E-05 | −4,2411E-05 | 3,1016E-05 | 3,0023E-05 | 2,3078E-05 | 1,7124E-05 |
| TRY08 | 6,6528E-06 | −2,8477E-06 | −2,8477E-06 | 1,0010E-05 | −6,0209E-05 | −5,1308E-05 | 2,1877E-05 | 2,0888E-05 | 1,3965E-05 | 8,0312E-06 |
| TRY09 | 1,2185E-05 | 3,2642E-06 | 3,2642E-06 | 1,6150E-05 | −5,4225E-05 | −4,5304E-05 | 2,8044E-05 | 2,7053E-05 | 2,0115E-05 | 1,4167E-05 |
| TRY10 | 7,6613E-06 | −1,2443E-06 | −1,2443E-06 | 1,1620E-05 | −5,8639E-05 | −4,9733E-05 | 2,3495E-05 | 2,2505E-05 | 1,5579E-05 | 9,6409E-06 |
| TRY11 | 1,4024E-07 | −8,7410E-06 | −8,7410E-06 | 4,0885E-06 | −6,5979E-05 | −5,7097E-05 | 1,5930E-05 | 1,4943E-05 | 8,0358E-06 | 2,1144E-06 |
| TRY12 | 2,1954E-05 | 1,3002E-05 | 1,3002E-05 | 2,5934E-05 | −4,4691E-05 | −3,5739E-05 | 3,7870E-05 | 3,6875E-05 | 2,9913E-05 | 2,3944E-05 |
| TRY13 | 1,3470E-05 | 4,5454E-06 | 4,5454E-06 | 1,7437E-05 | −5,2971E-05 | −4,4046E-05 | 2,9337E-05 | 2,8345E-05 | 2,1404E-05 | 1,5454E-05 |
| TRY14 | 1,1238E-05 | 2,3211E-06 | 2,3211E-06 | 1,5203E-05 | −5,5148E-05 | −4,6231E-05 | 2,7092E-05 | 2,6101E-05 | 1,9166E-05 | 1,3220E-05 |
| TRY15 | 9,5929E-06 | 6,8103E-07 | 6,8103E-07 | 1,3555E-05 | −5,6754E-05 | −4,7842E-05 | 2,5438E-05 | 2,4447E-05 | 1,7516E-05 | 1,1574E-05 |

**⚠ Auffälligkeit:** In allen 15 MFH-Blöcken sind **F_TWE,ÜWB und F_TWE,ÜSH exakt identisch**. Analog sind in den MFH-Blöcken **F_el,ÜSH und F_el,ÜSB identisch** (z.B. TRY01: beide −9,6069E-06). Der Normtext erläutert diese Kopplung **nicht**. Mögliche Ursachen: identische Referenztage, oder ein Satz-/Datenfehler. **Vor produktiver Nutzung gegen die CD-ROM-Datensätze (Ordner C2) verifizieren.**

#### 3.3.3 F_TWE,TT — **EFH Niedrigenergiehaus** (Tabellen 32–46)

| Zone | ÜWH | ÜWB | ÜSH | ÜSB | SWX | SSX | WWH | WWB | WSH | WSB |
|---|---|---|---|---|---|---|---|---|---|---|
| TRY01 | −4,5942E-05 | 5,0101E-05 | 5,7314E-04 | 3,3132E-04 | −2,3693E-04 | −4,4451E-06 | 2,4406E-04 | 1,2888E-04 | 5,2760E-04 | 4,4338E-04 |
| TRY02 | −5,4347E-05 | 4,1397E-05 | 5,6281E-04 | 3,2174E-04 | −2,4474E-04 | −1,2979E-05 | 2,3476E-04 | 1,1993E-04 | 5,1740E-04 | 4,3345E-04 |
| TRY03 | −5,2801E-05 | 4,2998E-05 | 5,6471E-04 | 3,2350E-04 | −2,4330E-04 | −1,1409E-05 | 2,3647E-04 | 1,2158E-04 | 5,1928E-04 | 4,3527E-04 |
| TRY04 | −5,8913E-05 | 3,6667E-05 | 5,5719E-04 | 3,1653E-04 | −2,4898E-04 | −1,7616E-05 | 2,2970E-04 | 1,1507E-04 | 5,1187E-04 | 4,2805E-04 |
| TRY05 | −2,9205E-05 | 6,7435E-05 | 5,9373E-04 | 3,5040E-04 | −2,2138E-04 | 1,2550E-05 | 2,6260E-04 | 1,4671E-04 | 5,4790E-04 | 4,6315E-04 |
| TRY06 | −8,6324E-05 | 8,2794E-06 | 5,2348E-04 | 2,8528E-04 | −2,7445E-04 | −4,5449E-05 | 1,9934E-04 | 8,5881E-05 | 4,7862E-04 | 3,9566E-04 |
| TRY07 | −5,3390E-05 | 4,2387E-05 | 5,6399E-04 | 3,2283E-04 | −2,4385E-04 | −1,2008E-05 | 2,3582E-04 | 1,2095E-04 | 5,1856E-04 | 4,3458E-04 |
| TRY08 | −1,0277E-04 | −8,7504E-06 | 5,0326E-04 | 2,6654E-04 | −2,8973E-04 | −6,2146E-05 | 1,8112E-04 | 6,8370E-05 | 4,5867E-04 | 3,7623E-04 |
| TRY09 | −6,6843E-05 | 2,8455E-05 | 5,4744E-04 | 3,0749E-04 | −2,5635E-04 | −2,5668E-05 | 2,2091E-04 | 1,0663E-04 | 5,0225E-04 | 4,1868E-04 |
| TRY10 | −9,3155E-05 | 1,2041E-06 | 5,1508E-04 | 2,7749E-04 | −2,8080E-04 | −5,2386E-05 | 1,9177E-04 | 7,8605E-05 | 4,7033E-04 | 3,8759E-04 |
| TRY11 | −1,2717E-04 | −3,4024E-05 | 4,7325E-04 | 2,3872E-04 | −3,1240E-04 | −8,6925E-05 | 1,5409E-04 | 4,2382E-05 | 4,2907E-04 | 3,4739E-04 |
| TRY12 | −2,3014E-05 | 7,3847E-05 | 6,0134E-04 | 3,5746E-04 | −2,1563E-04 | 1,8836E-05 | 2,6946E-04 | 1,5330E-04 | 5,5541E-04 | 4,7047E-04 |
| TRY13 | −7,2725E-05 | 3,2719E-05 | 5,5250E-04 | 3,1219E-04 | −2,5252E-04 | −2,1487E-05 | 2,2547E-04 | 1,1101E-04 | 5,0724E-04 | 4,2354E-04 |
| TRY14 | **−1,2637E-05 ⚠** | 1,2413E-05 | 5,2839E-04 | 2,8983E-04 | −2,7074E-04 | −4,1396E-05 | 2,0376E-04 | 9,0131E-05 | 4,8346E-04 | 4,0038E-04 |
| TRY15 | −8,4823E-05 | 9,8331E-06 | 5,2533E-04 | 2,8699E-04 | −2,7305E-04 | −4,3925E-05 | 2,0100E-04 | 8,7478E-05 | 4,8044E-04 | 3,9743E-04 |

**⚠ Unsicherer Wert:** TRY14 / ÜWH (`−1,2637E-05`) passt nicht in das Muster der Nachbarzonen (TRY13: −7,27E-05; TRY15: −8,48E-05; erwartbar ca. −8E-05 bis −9E-05). Zusätzlich stimmt die Ziffernfolge exakt mit F_el,ÜWH von TRY15 (−1,2637E-04) überein → **Verdacht auf Extraktions-/OCR-Fehler**. Wert vor Nutzung an der Norm bzw. CD-ROM (Ordner C1) verifizieren.

#### 3.3.4 Verifikationsregeln für die Implementierung (eigene Ableitung, an der Norm geprüft)

Aus dem Aufbau der Gleichungen (1)–(3) und der Aussage in 6.5 („Die Summe aller Tagesbedarfe des Kalenderjahres ergibt den … Jahresbedarf") folgen zwei zwingende Prüfsummen je Klimazone:

```
Σ_TT ( n_TT · F_Heiz,TT )  = 1
Σ_TT ( n_TT · F_TWE,TT )  = 0     (analog F_el,TT)
```

Verifiziert an TRY05:
- **F_Heiz, EFH Bestand:** Σ n_TT·F_Heiz,TT = **0,99999** ✓
- **F_TWE, EFH Bestand:** Σ n_TT·F_TWE,TT = **+3,1E-04**; bei N_Pers = 3 entspricht das **+0,09 %** Abweichung von 1 → reiner Rundungseffekt ✓
- **F_TWE, MFH Bestand:** Σ n_TT·F_TWE,TT = **+9,3E-05**; bei N_WE = 25 → **+0,23 %** ✓

→ Diese Prüfsummen eignen sich als **automatischer Unit-Test** beim Einlesen der Faktoren in WP-Plan (Toleranz ≤ 0,5 %). Sie hätten den TRY14/ÜWH-Verdachtsfall vermutlich nicht aufgedeckt (Effekt zu klein), aber sie sichern gegen Zeilen-/Spaltenvertauschungen.

### 3.4 Verhältnis TWW zu Heizung in den Typtagprofilen

Beispielrechnung der Norm (Abschnitt 8, S. 25–27): EFH Bestand, 110 m², **3 Personen**, Wetter/58300 → TRY05; Q_Heiz,a = 8250 kWh; W_a = 3 · 1650 = **4950 kWh**; Q_TWE,a = 3 · 500 = **1500 kWh**.

**Tabelle 16 (S. 27) – Tagesenergiebedarfe des Beispielgebäudes:**

| Typtag | Q_Heiz,TT [kWh] | W_TT [kWh] | Q_TWE,TT [kWh] | TWW-Anteil an (Heiz+TWW) |
|---|---|---|---|---|
| ÜWH | 20,46 | 12,93 | 4,18 | 17 % |
| ÜWB | 25,44 | 13,73 | 4,25 | 14 % |
| ÜSH | 11,08 | 15,22 | 5,57 | 33 % |
| ÜSB | 26,94 | 15,59 | 4,11 | 13 % |
| **SWX** | **0** | 11,64 | 3,42 | **100 %** |
| **SSX** | **0** | 14,75 | 4,03 | **100 %** |
| WWH | 52,62 | 13,72 | 4,16 | 7,3 % |
| WWB | 51,70 | 14,67 | 4,57 | 8,1 % |
| WSH | 53,71 | 16,55 | 5,00 | 8,5 % |
| WSB | 47,05 | 17,05 | 5,23 | 10,0 % |

**Kernbefunde:**
- An **Sommertagen ist der Heizwärmebedarf exakt null** (F_Heiz,SWX = F_Heiz,SSX = 0,0000E+00 in allen 45 Faktortabellen). Der Wärmeerzeuger deckt im Sommer ausschließlich TWW.
- Im Winter macht TWW nur **7–10 %** der Tageswärme aus, im Übergang **13–33 %**.
- **Die TWW-Tagesenergie schwankt über alle zehn Typtage nur zwischen 3,42 und 5,57 kWh** – das sind **−17 % bis +36 %** um den Jahresmittelwert von 1500/365 = 4,11 kWh. Zum Vergleich: der Heizwärmebedarf schwankt zwischen 0 und 53,71 kWh (Faktor ∞ bzw. 0…13× Mittelwert).

→ **Der deterministische VDI-4655-Pfad bildet für TWW praktisch keine Tagesenergie-Streuung ab.** Reale Tag-zu-Tag-Streuungen im TWW-Bedarf eines EFH liegen deutlich darüber. Das ist der quantitativ belastbarste Beleg dafür, dass VDI 4655 für TWW **bilanz-, nicht bemessungsorientiert** ist.

### 3.5 Aussagen zu Gleichzeitigkeit, WE-Zahl, Mittelwertcharakter, Streuung

Das ist der wichtigste Abschnitt für die WP-Plan-Entscheidung – und er ist überwiegend ein **Negativbefund**.

**a) Gleichzeitigkeit:** Die Begriffe „Gleichzeitigkeit", „Gleichzeitigkeitsfaktor", „Spitzenvolumenstrom", „Zapfspitze" oder „Bemessungsdurchfluss" kommen im gesamten Normtext (S. 1–52) **nicht vor**. Es gibt **keine** Gleichzeitigkeitsfunktion, **keine** WE-abhängige Dämpfung, **keine** Aussage über die Gleichzeitigkeit der zugrunde liegenden Messobjekte.

**b) Skalierung über WE-Zahl:** Die Skalierung ist über Gl. (3) und (6) **strikt linear in N_WE**:
```
Q_TWE,TT(t) = F_TWE,n,TT(t) · Q_TWE,a · ( 1/365 + N_WE · F_TWE,TT )
```
Der **Formvektor F_TWE,n,TT(t) ist unabhängig von N_WE**. Damit skaliert die momentane Leistung linear mit der Wohneinheitenzahl. Die im Referenz-MFH real vorhandene Gleichzeitigkeit wird auf jede Gebäudegröße von 3 bis 25 WE unverändert übertragen.
**Da die Größe der vermessenen MFH nicht angegeben ist, ist die implizite Gleichzeitigkeitsannahme unbekannt und nicht prüfbar.**

**c) Grenzen der WE-Zahl:** Ja – **3 bis 25 WE** (Abschnitte 1 und 6.1). Über 25 WE hinaus ist die Richtlinie nicht anwendbar. Eine Begründung für diese Grenze wird nicht gegeben; es ist naheliegend (aber **Vermutung**), dass sie den Bereich der Messobjekte abdeckt.

**d) Sind die Profile Mittelwerte?** **Nein, ausdrücklich nicht.** S. 4, Anmerkung 1 zu „Referenzlastprofil": Auswahl eines realen Tages, „damit die Charakteristika eines Profils erhalten bleiben und nicht durch Mittelwertbildung und die damit verbundene Glättung verloren gehen." Sichtbar wird das in **Bild 4 (S. 28)**, dem kumulierten Referenzlastprofil EFH ÜWH: die TWE-Kurve verläuft als **ausgeprägte Treppenfunktion** – (grafisch abgelesen, daher ±) ein Sprung von ca. 0,04 auf ca. 0,55 zwischen etwa 05:30 und 06:30 Uhr, ein zweiter Sprung auf ca. 0,77 gegen 12:00 Uhr, weitere Stufen bis 1,0 gegen 22:00 Uhr. **Rund 50 % der Tages-TWW-Energie entfallen im EFH-Referenztag auf einen einzigen Morgenblock.**
→ Für **EFH** ist der Formvektor also durchaus „spitzig" (Einzelzapfungen eines realen Hauses). Für **MFH** wird diese Spitzigkeit durch die **15-min-Mittelung der Rohdaten** systematisch gedämpft – eine 5-Minuten-Duschspitze ist in einem 15-min-Mittelwert prinzipiell nicht abbildbar.

**e) Streuung / Einzeltagesspitzen:** Die Norm enthält **keinerlei** statistische Angaben – keine Standardabweichungen, keine Perzentile, keine Extremwert- oder Häufigkeitsbetrachtungen, keine Angaben zur Bandbreite zwischen den Messobjekten. Es wird ausschließlich **ein** Referenztag je Typtagkategorie geliefert.

**f) Ausdrückliche Abgrenzung gegenüber genormten Zapfprofilen** (S. 4, Anmerkung zu „Lastprofil"):
> **„Die Lastprofile für Trinkwassererwärmung (TWE) sind nicht identisch mit genormten Profilen, z.B. nach DIN EN 15450."**

Das ist die **einzige** explizite Selbstabgrenzung der Norm im TWW-Kontext – und sie ist inhaltlich genau die für WP-Plan relevante.

---

## 4. Strom-Profile (Kurzfassung)

**Bilanzgrenze (Abschnitt 4, S. 8; Abschnitt 6.2.2, S. 17):**
- **Eingeschlossen:** alle von Haushaltsstrom versorgten Verbraucher, Strombedarf der **Heizkreispumpen** und der **Zirkulationspumpen** der TWW-Versorgung, Hilfsstrom für Regelung der Heizungsanlage sowie ggf. Lüftungsanlagen.
- **Nicht eingeschlossen:** „Elektrowärmepumpen und Geräte zur dezentralen Bereitstellung der Raumwärme (z.B. elektrische Heizstrahler, Ölradiatoren, elektrische Speicherheizungen) sowie Geräte, die der elektrischen Erwärmung von Trinkwasser (Durchlauferhitzer, Boiler, Untertischgeräte etc.) dienen."

**Jahresstrombedarf (Abschnitt 6.2.2, S. 17):**

| EFH – Personenzahl | Jahresstrombedarf |
|---|---|
| 1 Person | 2350 kWh (absolut) |
| 2 Personen | 2020 kWh/Pers → 4040 kWh |
| 3 Personen | 1650 kWh/Pers → 4950 kWh |
| 4 Personen | 1500 kWh/Pers → 6000 kWh |
| 5 Personen | 1400 kWh/Pers → 7000 kWh |
| 6 Personen | 1350 kWh/Pers → 8100 kWh |

**MFH: 3000 kWh/WE, unabhängig von der Personenzahl.**
Für Bestandsgebäude: „kann der Verbrauchswert des Vorjahres zum Vergleich herangezogen werden."

**Für WP-Plan besonders relevant (Abschnitt 4, „Sonderfälle", S. 9/10):**
> **„Wärmepumpenstrom** – Der Strombedarf einer elektrischen Wärmepumpe muss zusätzlich zum Referenzlastprofil für den Strom berücksichtigt werden. Das betrifft alle Komponenten der Anlage also Pumpen, Kompressoren und Gebläse, aber auch elektrische Heizstäbe."
> **„Elektromobilität** – Der Strombedarf für das Laden von Elektromobilen (Pkw, E-Bike etc.) muss zusätzlich zum Referenzlastprofil für den Strom berücksichtigt werden."

Weitere Sonderfälle: PV-Anlage und Batteriespeicher verändern das Strom-Referenzlastprofil **nicht** (Superposition, ggf. negativer Bedarf); thermische Solaranlagen verändern weder Heiz- noch TWE-Profil; **automatisierte Energiemanagementsysteme lassen sich mit den vorhandenen Referenzlastprofilen ausdrücklich nicht abbilden** („da eine Zuordnung des Bedarfs zu einzelnen Verbrauchern nicht ohne Weiteres möglich ist", S. 10).

**Zusätzlicher Faktor bei NEH:** In den Tabellen 32–46 existiert eine vierte Zeile **F_el,vent,TT** (nur NEH). Dieses Formelzeichen ist **weder in Abschnitt 3 (Formelzeichen/Indizes) noch in Abschnitt 6.4 definiert oder erläutert**. Naheliegende, aber **unbelegte Vermutung**: Faktor für den Strombedarf der Lüftungsanlage (Bezug zu 6.2.2 „ggf. Lüftungsanlagen"). Vor Nutzung klären.

**PV (Abschnitt 7, S. 21; Tabelle 13, Bild 3):** Erzeugungsprofile normiert auf die **Peakleistung** in **kW/kWp**, zugeordnet zu den **12** Typtagkategorien der Tabelle 2. Bild 3 (S. 26) zeigt den Typtag ÜSB mit stark fluktuierendem Verlauf und Spitzen bis ca. 0,9 kW/kWp gegen 12:30 Uhr.

---

## 5. Anwendungsgrenzen laut Normtext

### 5.1 Wofür die Profile ausdrücklich gedacht sind (Abschnitt 1, S. 3 – wörtlich)

> „Diese Richtlinie bietet mit Referenzlastprofilen **Grundlagen und ein Instrumentarium für die Auslegung von Strom- und Wärmeerzeugungsanlagen, z.B. Kraft-Wärme-Kopplung, Wärmepumpen, Fotovoltaik, Solarthermie, in Wohngebäuden und ihrer Wirtschaftlichkeitsberechnung**. Hieraus können unter anderem **Prüfbedingungen für die Ermittlung des Nutzungsgrads, Auslegungskriterien und Verfahren zum Testen** abgeleitet werden. Ebenso kann diese Richtlinie **für Simulationen und Auslegungsberechnungen, z.B. für Wärme- und Stromspeicher, herangezogen werden. Betriebszeiten und Betriebszyklen lassen sich zur Bestimmung von Lebensdauer und Wartungsintervallen nutzen.**"

Einleitung (S. 2): „Diese Richtlinie soll der **Optimierung der Auslegung und des Zusammenspiels von Energieversorgungsanlagen und Speichern** für Ein- und Mehrfamilienhäuser dienen, um die Effizienz und Wirtschaftlichkeit des Systems zu verbessern."

Bilanzgrenzen (Abschnitt 4, S. 7): „Die Referenzlastprofile beschreiben den Energiebedarf eines Objekts **unabhängig von der Art der Deckung dieses Energiebedarfs**."

### 5.2 Wofür ausdrücklich NICHT

**Wichtiger Befund: Eine explizite Negativabgrenzung gegen Spitzenlast- oder Heizlastauslegung existiert im Normtext nicht.** Es gibt **keine Warnung** in der Art „nicht für die Bestimmung der Normheizlast / des Bemessungsdurchflusses / der Spitzenzapfleistung geeignet". Wer eine solche Zitatstelle sucht, wird sie in dieser Ausgabe nicht finden.

Die einzigen expliziten Ausschlüsse und Grenzen sind:

| Ausschluss / Grenze | Fundstelle |
|---|---|
| **„Die Lastprofile für Trinkwassererwärmung (TWE) sind nicht identisch mit genormten Profilen, z.B. nach DIN EN 15450."** | S. 4, Anm. zu „Lastprofil" |
| Nur **Wohngebäude** | Abschnitt 1 |
| EFH: **max. 6 Personen**, max. 2 WE mit gemeinsamer Anlage | Abschnitt 1, 6.1 |
| MFH: **3 bis 25 WE**, gemeinsame Anlage | Abschnitt 1, 6.1 |
| MFH: **nur Bestand**, keine NEH-Datensätze | Abschnitt 1 |
| MFH: **nur 15-min-Auflösung** | Abschnitt 6.4 |
| Nur **Deutschland**, 15 TRY-Klimazonen | Abschnitt 5 |
| km²-TRY „bietet **höhere Genauigkeit**" für Planung/Auslegung von HLK-Anlagen und energetische Bilanzierungen; 15-Zonen-Verfahren nur „für die **vergleichende Betrachtung** von Anlagen nach VDI 4655 … beibehalten" | S. 13 (impliziter Genauigkeitsvorbehalt) |
| Speicherverluste (Wärme wie Warmwasser) **nicht** in den Profilen enthalten | Abschnitt 4, S. 9 |
| WP-Strom, E-Mobilität, elektrische Direktheizung, elektrische TWW-Bereitung **nicht** enthalten | Abschnitt 4, 6.2.2 |
| Automatisierte Energiemanagementsysteme „lassen sich mit den vorhandenen Referenzlastprofilen **nicht abbilden**" | S. 10 |

**Fazit zu Frage 5:** Die Norm positioniert sich selbst als Werkzeug für **Systemauslegung, Betriebssimulation, Speicherdimensionierung und Wirtschaftlichkeitsrechnung**. Sie enthält jedoch **weder eine Aussage, dass sie für Spitzenlastbemessung geeignet sei, noch eine Warnung, dass sie es nicht sei**. Die Nichteignung für Zapfspitzen muss **aus den Methodikmerkmalen** (15-min-MFH-Daten, lineare N_WE-Skalierung, ein Referenztag je Kategorie, keine Streuungsangaben, ausdrückliche Nicht-Identität mit DIN EN 15450) hergeleitet werden – sie steht nicht als Verbotssatz im Text.

---

## 6. Abgleich mit demandlib (oemof) 0.2.2

**Vorbemerkung zur Belastbarkeit:** Ich habe in dieser Sitzung **weder den demandlib-Quellcode noch die Ausgabe VDI 4655 Blatt 1:2008-05 eingesehen**. Alle Aussagen unter „Indizien" beruhen auf dem Vergleich der Angaben aus dem Auftrag (`vdi`-Modul, Keys `Q_TWW_a`, `F_TWW_TT`, 1-min-Auflösung, kein Schaltjahr) mit dem vorliegenden Normtext 2021. **Sie sind ausdrücklich als Indizienschluss zu behandeln, nicht als verifizierter Befund.**

### 6.1 Indizien für „demandlib basiert auf der Ausgabe 2008"

| Indiz | Beleg 2021 | Bewertung |
|---|---|---|
| **Nomenklatur `TWW` vs. `TWE`** | Die Ausgabe 2021 verwendet **durchgängig und ausschließlich den Index `TWE` (Trinkwassererwärmung)** – Abkürzungsverzeichnis S. 7, alle Gleichungen (1)–(6), alle Tabellen 8, 17–46, CD-ROM-Ordnerbezeichnungen. `TWW` wird 2021 nur im **Titel** und im Bilanzgrenzen-Bild als Sachbegriff „Trinkwarmwasser" verwendet, **nie als Formelindex**. demandlib nutzt `Q_TWW_a` / `F_TWW_TT` als Formelindex. | **Starkes Indiz** für Ausgabe 2008 (die Umbenennung TWW→TWE ist plausibel Teil der Neufassung – dies ist jedoch **Vermutung**, da mir die 2008er Nomenklatur nicht im Original vorliegt) |
| **NEH-Datensätze** | 2021 erstmals vorhanden (Abschnitte 5.2, 9.2, Tab. 32–46, CD-ROM D2). Aus der Einleitung geht hervor, dass sie über NOVAREF (2014–2016) neu hinzukamen. | Wenn demandlib **keine** NEH-/Heizgrenze-12-°C-Variante kennt → Ausgabe 2008 |
| **PV-Profile / 12 Typtage** | 2021 neu (Abschnitt 7, Tab. 2, 9–13, CD-ROM E). | Wenn demandlib nur 10 Typtagkategorien kennt → Ausgabe 2008 |
| **TRY2017 + Kalenderjahr 2014** | 2021 explizit (Abschnitte 5.1/5.2). | Die Typtag-Anzahlen je Zone (Tab. 4/6) sind ausgabespezifisch. Wenn demandlib abweichende Zahlen mitbringt → andere Ausgabe |
| **2-s-Auflösung** | 2021 neu für EFH (Anhang D1/D2). | Wenn demandlib nur 1 min und 15 min kennt → Ausgabe 2008 |
| **F_el,vent,TT** | 2021 nur in NEH-Tabellen. | Wenn demandlib diesen Faktor nicht kennt → Ausgabe 2008 |

### 6.2 Was systematisch übereinstimmt (ausgabeunabhängig)

- **Struktur der Typtagkennungen** (ÜWH/ÜWB/ÜSH/ÜSB/SWX/SSX/WWH/WWB/WSH/WSB) und die 10-Kategorien-Grundsystematik.
- **Gleichungsform** `Q_TWW_TT = Q_TWW_a · (1/365 + N · F_TWW_TT)` – die Konstante **365** und damit die **Nicht-Berücksichtigung von Schaltjahren** ist in der Ausgabe 2021 in Gl. (2) und (3) **wörtlich so enthalten** und durch das Bezugs-Kalenderjahr **2014** (kein Schaltjahr) methodisch begründet. → **demandlibs „kein Schaltjahr" ist mit beiden Ausgaben konsistent und taugt nicht zur Ausgabenunterscheidung.**
- **1-min-Zeitauflösung** ist in 2021 für EFH vorhanden. → ebenfalls kein Unterscheidungsmerkmal für EFH. **Achtung bei MFH:** 2021 liefert für MFH **ausschließlich 15 min**. Wenn demandlib MFH-Profile in 1-min-Raster ausgibt, ist das entweder eine Interpolation oder stammt aus einer anderen Datenquelle – **das wäre ein eigenständiger Genauigkeitsvorbehalt für alle MFH-Spitzenaussagen aus demandlib.**
- **F_TWW_TT teilweise negativ** und die Sonderregel `F_TWE,SWX = 0` bei negativem Tagesbedarf (S. 18) – prüfen, ob demandlib diese Klemmung implementiert. Falls nicht: **potenzieller negativer TWW-Tagesbedarf im Sommer.**

### 6.3 Konkreter Prüfplan (empfohlen, geringer Aufwand, hoher Erkenntniswert)

1. **Typtaganzahlen:** demandlib-Tabelle für TRY05 gegen Tabelle 4 dieses Berichts prüfen (erwartet 39/95/6/21/98/20/12/58/3/13, Σ = 365). Abweichung ⇒ andere Ausgabe.
2. **F-Faktoren:** demandlib `F_TWW_TT` für TRY05/EFH gegen Abschnitt 3.3.1 dieses Berichts prüfen (ÜWH = 1,6241E-05 …). Abweichung ⇒ andere Ausgabe.
3. **Jahres-TWW-Defaults:** Prüfen, ob demandlib 500 kWh/Pers (EFH) und 1000 kWh/WE (MFH) verwendet.
4. **Strom-Defaults:** Prüfen gegen 2350/2020/1650/1500/1400/1350 kWh/Pers bzw. 3000 kWh/WE.
5. **NEH/PV:** Existenz prüfen. Fehlen beide ⇒ sehr starkes Indiz für 2008.
6. **SWX-Klemmung:** Prüfen, ob `max(F_TWE,SWX, 0)` implementiert ist.

**Zwischenfazit (mit Vorbehalt):** Die Nomenklatur `TWW` in demandlib passt **nicht** zur vorliegenden Ausgabe 2021 (dort konsequent `TWE`). Die Wahrscheinlichkeit, dass demandlib 0.2.2 auf der **Ausgabe 2008-05** (bzw. deren CD-ROM-Daten) beruht, ist hoch. **Verifiziert ist das nicht.** Solange nicht Prüfschritt 1 oder 2 durchgeführt wurde, dürfen demandlib-Ergebnisse **nicht** als „nach VDI 4655:2021" bezeichnet werden.

---

## 7. Bewertung für das WP-Plan-TWW-Modul

### 7.1 (a) Formvektor-/Typtag-Quelle für den deterministischen Bilanzpfad Wohnen (S2) — **geeignet, mit Einschränkungen**

**Pro:**
- Die Norm liefert genau das, was S2 braucht: **normierte, dimensionslose Tagesformvektoren** F_TWE,n,TT(t) plus eine saubere Trennung von *Jahresmenge* (Q_TWE,a) × *Tagesanteil* (1/365 + N·F) × *Tagesform* (Gl. 6). Das ist strukturell identisch zum Konzept-V1.1-Ansatz „Jahresmenge → Tagesenergie → Formvektor".
- Die Formvektoren sind **reale, nicht geglättete Einzeltage** (S. 4, Anm. 1) – für EFH damit realistisch strukturiert (deutliche Morgen-/Mittags-/Abendblöcke, Bild 4).
- Die Anker **500 kWh/(Pers·a)** bzw. **1000 kWh/(WE·a)** sind messdatengestützt und für den Bilanzpfad direkt verwendbar.
- **Zwingende Randbedingung ist berücksichtigt:** SWX kann negative Tagesenergie ergeben → `F_TWE,SWX = 0` klemmen (S. 18).

**Contra / Auflagen:**
- **MFH-Formvektoren nur in 15-min-Auflösung.** Für ein Zapfprofil-Modul, das intern feiner rechnet, muss die Auflösung entweder beibehalten oder die Interpolation als solche gekennzeichnet werden. Eine 1-min-Interpolation eines 15-min-MFH-Profils erzeugt **keine** zusätzliche Information über Zapfspitzen.
- **Nur 10 Tagesformen für 365 Tage.** Jeder Typtag wird 3- bis 150-mal identisch wiederholt (Tab. 4). Die resultierende Jahresdauerlinie ist stufig; das Maximum tritt nicht einmal, sondern n_TT-mal auf.
- Kein NEH-Datensatz für MFH – für MFH-Neubau/Effizienzhaus fehlt die Datenbasis.
- **Lizenz:** siehe 7.6.

**Empfehlung:** VDI 4655 als **eine wählbare Formvektor-Quelle** im Bilanzpfad S2 führen, nicht als einzige. Kennzeichnung im UI und im Berechnungsprotokoll: „VDI 4655:2021, Typtag <XX>, TRY<nn>, Auflösung <15 min / 1 min>, Bilanzgrenze inkl. Verteil-/Zirkulationsverluste, **ohne Speicherverluste**".

### 7.2 (b) Jahresgang-/Wetterkopplung — **klar empfehlenswert, stärkster Nutzen**

Das ist der Bereich, in dem VDI 4655 dem bisherigen Konzept **echten Mehrwert** liefert, ohne Spitzenlastrisiken zu erzeugen:

- **Vollständig spezifizierte Zuordnungsregel** aus drei Merkmalen: Jahreszeit (Tagesmitteltemperatur gegen HG 15 °C / 12 °C und 5 °C), Bewölkung (DWD-Bedeckungsgrad-Tagesmittel ≥/< 5/8), Wochentag (Werktag Mo–Sa / Sonntag, **Feiertage = Sonntag**).
- **Fertige Standortzuordnung** über PLZ → Klimazone (Tabelle A1, ca. 700 Orte) und 15 Repräsentanzstationen mit TRY2017.
- **Fertige Jahresverteilungen** (Tab. 4/6/9/11) inkl. Reihenfolge der Typtage auf CD-ROM (Ordner B1–B4) – d.h. ein Jahresgang lässt sich ohne eigene Wetterdatenverarbeitung erzeugen.
- Die **Sieben-Tage-Mitteltemperatur** als Trägheitsmaß ist ein methodisch sauberer, direkt übernehmbarer Baustein.

**⚠ Präzisierungsbedarf:** Die Begriffsdefinitionen (Sommer-/Übergangs-/Wintertag) beziehen sich auf die **Tagesmitteltemperatur**, während die Tabellen 5/7/10/12 und die Faktortabellen die **Sieben-Tage-Mitteltemperatur** der Typtage ausweisen und der Anhang (Ordner B) ebenfalls „Außentemperaturen (Sieben-Tage-Mittel)" nennt. Welche der beiden Größen bei eigener Wetterdatenverarbeitung für die **Kategoriezuordnung** heranzuziehen ist, geht aus dem gedruckten Text **nicht eindeutig hervor**. Vor Implementierung einer eigenen Zuordnungsroutine ist dies zu klären (Vergleich einer eigenen Zuordnung gegen die CD-ROM-Reihenfolgedatei für eine Zone ist der schnellste Test).

**Warnhinweis für die Produktkommunikation:** Der DWD hat das 15-Zonen-Konzept **aufgegeben**; die Norm räumt selbst ein, dass km²-TRY für Planung/Auslegung genauer sind (S. 13). Das 15-Zonen-Verfahren dient „der vergleichenden Betrachtung von Anlagen nach VDI 4655". WP-Plan sollte es entsprechend als **Vergleichs-/Bilanzklima**, nicht als Auslegungsklima labeln.

### 7.3 (c) Validierungsreferenz — **ja, gut geeignet, dafür sogar prädestiniert**

Vorschlag für konkrete Validierungsanker im WP-Plan-Testsuite:

1. **Jahresenergie:** Ein stochastisch erzeugtes WP-Plan-Zapfprofil für ein EFH mit N Personen muss über das Jahr auf 500·N kWh (± definierte Toleranz) laufen; MFH auf 1000·N_WE kWh — **bei angeglichener Bilanzgrenze** (Verteil-/Zirkulationsverluste inkludiert, Speicherverluste exkludiert).
2. **Sommer-Nulllast Heizung:** F_Heiz,SWX = F_Heiz,SSX = 0 ⇒ die Sommer-Erzeugerlast in WP-Plan muss reines TWW sein. Guter Test für den Sommer-Betriebspunkt der WP (Taktung, JAZ-Beitrag).
3. **Tagesenergie-Bandbreite:** Der VDI-Bereich (Beispiel Tab. 16: 3,42–5,57 kWh bei 4,11 kWh Mittel) ist die **untere** Plausibilitätsgrenze der Streuung. Ein stochastisches WP-Plan-Modell **soll** breiter streuen; wenn es enger streut, ist es zu deterministisch.
4. **Prüfsummen** Σ n_TT·F_Heiz,TT = 1 und Σ n_TT·F_TWE,TT ≈ 0 als Unit-Test des Faktor-Imports (siehe 3.3.4).
5. **Beispielrechnung Abschnitt 8** (EFH 110 m², 3 Pers., Wetter/58300 → TRY05, Q_Heiz,a 8250 kWh, W_a 4950 kWh, Q_TWE,a 1500 kWh → Tabelle 16) als **normbelegter Regressionstest** für die Implementierung der Gl. (1)–(6).

### 7.4 (d) NICHT für Auslegungsspitzen — **Bestätigt, aber die Begründung muss umformuliert werden**

Die bisherige Konzeptbegründung („implizite 100-%-Gleichzeitigkeit", „Mittelwertprofile") ist in dieser Form **nicht durch den Normtext gedeckt** und sollte präzisiert werden. Die tragfähige, belegbare Begründungskette lautet:

1. **Die Norm beansprucht Spitzenlastbemessung nicht.** Ihr Anwendungsbereich (Abschnitt 1) nennt Systemauslegung, Wirtschaftlichkeit, Nutzungsgrad, Speicherdimensionierung, Betriebszyklen – **nicht** Bemessungsdurchfluss oder Zapfspitzen.
2. **Ausdrückliche Selbstabgrenzung:** „Die Lastprofile für Trinkwassererwärmung (TWE) sind nicht identisch mit genormten Profilen, z.B. nach DIN EN 15450." (S. 4)
3. **MFH-Rohdaten sind 15-Minuten-Mittelwerte** (S. 2) und die MFH-Profile liegen **ausschließlich** in 15 min vor (S. 19, Anhang D3). Eine Zapfspitze im Minutenbereich ist darin physikalisch nicht enthalten.
4. **Skalierung ist strikt linear in N_WE** (Gl. 3/6), der Formvektor ist von N_WE unabhängig. Es gibt **keine** Gleichzeitigkeitsfunktion. Die Gleichzeitigkeit des Referenz-MFH wird unverändert auf 3 bis 25 WE übertragen. **Die WE-Zahl der Messobjekte ist in der Norm nicht angegeben** – die implizite Gleichzeitigkeit ist damit unbekannt und nicht prüfbar. (Falls das Referenz-MFH klein war, folgt daraus systematische Überschätzung bei großen Zielgebäuden – das ist die technisch korrekte Fassung des bisherigen „100-%-Gleichzeitigkeits"-Arguments.)
5. **Nur 10 Tagesformen für 365 Tage.** Das Tagesmaximum eines Typtags wiederholt sich n_TT-mal (bis über 100-mal). Eine daraus konstruierte Jahresdauerlinie ist im oberen Bereich **stufig und zu flach**; ihr Maximum ist kein seltenes Ereignis, sondern ein häufig wiederholtes. Genau dieses Verhalten erklärt den Vorprojekt-Befund „Messspitze ≈ P90 der synthetischen Dauerlinie".
6. **Keine Streuungs-/Extremwertangaben** in der gesamten Richtlinie. Ein Bemessungswert braucht ein Überschreitungskriterium; die Norm liefert keines.

**Empfohlene Formulierung im Konzept V1.2:** „VDI 4655:2021 wird als Formvektor- und Jahresgangquelle für den deterministischen Bilanzpfad Wohnen sowie als Validierungsreferenz genutzt. Für die Bemessung von Erzeuger-/Speicher-Zapfspitzen ist sie **nicht** vorgesehen: die Richtlinie beansprucht dies nicht, grenzt ihre TWE-Profile ausdrücklich von genormten Zapfprofilen (DIN EN 15450) ab, liefert MFH-Profile nur als 15-Minuten-Mittelwerte, skaliert linear in der Wohneinheitenzahl ohne Gleichzeitigkeitsansatz und macht keinerlei Angaben zu Streuung oder Überschreitungshäufigkeiten."

### 7.5 Passt der 2,4×-Befund zu den Aussagen der Norm?

**Teils ja, teils muss die Begründung korrigiert werden.**

| Vorprojekt-Aussage | Befund aus der Norm 2021 | Bewertung |
|---|---|---|
| „VDI-4655-Profile sind Mittelwertprofile" | **Falsch für diese Ausgabe.** S. 4 Anm. 1: Auswahl eines realen Einzeltages, ausdrücklich **um Glättung durch Mittelwertbildung zu vermeiden**. | **Zu korrigieren** |
| „implizite 100-%-Gleichzeitigkeit" | Die Norm sagt dazu nichts. Faktisch: **lineare Skalierung mit N_WE ohne Gleichzeitigkeitsfunktion**, Formvektor N_WE-unabhängig. Ob das 100 % entspricht, hängt von der (unbekannten) Größe der Referenz-MFH ab. | **Präzisieren:** „lineare, gleichzeitigkeitsfreie WE-Skalierung mit unbekannter impliziter Gleichzeitigkeit" |
| „Lastspitze bei MFH ~2,4× überschätzt" | Durch die Norm **weder bestätigt noch widerlegt** (keine Streuungs-/Spitzenlastaussagen). Plausibel erklärbar durch: 15-min-Mittelung der MFH-Rohdaten (dämpfend), lineare WE-Skalierung (verstärkend bei großem Zielgebäude), nur 10 wiederholte Tagesformen (verstärkend im oberen Dauerlinienbereich). | **Beibehalten als eigener empirischer Befund**, klar als solcher gekennzeichnet – nicht als Normaussage |
| „Messspitze ≈ P90 der synthetischen Jahresdauerlinie" | Methodisch konsistent mit dem Aufbau der Norm: eine aus 10 Tagesformen mit Wiederholungshäufigkeiten 3…150 zusammengesetzte Dauerlinie hat im obersten Perzentilbereich systematisch zu wenig Auflösung. | **Konsistent** |

**Zusätzliches, neues Argument aus dieser Auswertung:** Die **TWW-Tagesenergie** schwankt in VDI 4655 nur um **−17 % / +36 %** um den Jahresmittelwert (Tab. 16). Der deterministische Pfad kann also **weder die Tag-zu-Tag-Streuung noch – über 15-min-MFH-Vektoren – die Intra-Tages-Spitzen** eines MFH abbilden. Beide Effekte fehlen; die Richtung ihres Netto-Einflusses auf die Jahresspitze ist ohne Messvergleich nicht vorherzusagen. Der INEKON-Vorprojektbefund ist damit ein **notwendiger empirischer Beitrag**, den die Norm nicht liefert.

### 7.6 Verhältnis zu DIN EN 12831-3 / A100 und DIN V 18599-10

**Was VDI 4655 tatsächlich zitiert (Schrifttum, S. 52) – präzise Feststellung:**
- DIN 4710:2003-01; VDI 4710 Blatt 2:2007-05
- DIN EN 12831 (Norm-Heizlast); **DIN EN 12831 Beiblatt 1:2008-07 – zurückgezogen 2020-04, Nachfolger DIN/TS 12831-1**; DIN/TS 12831-1:2020-04
- DIN EN 15450:2007-12 (Planung von Heizungsanlagen mit Wärmepumpen)
- DIN EN ISO 52016-1:2018-04; DIN V 4108-6:2003-06
- **DIN V 4701-10:2003-08 – zurückgezogen 2020-04, Nachfolger DIN V 18599-12**
- **DIN V 18599** (allgemein) und **DIN/TS 18599-12:2021-04** (Tabellenverfahren für Wohngebäude)
- GEG 2020; EnEV (zurückgezogen 2020-11-01)

**Wichtig:** **DIN EN 12831-3** (Trinkwassererwärmungsanlagen) wird **nicht** zitiert. **DIN V 18599-10** wird **nicht** einzeln zitiert (nur DIN V 18599 als Reihe und Teil 12). Es besteht also **keine normative Verzahnung** zwischen VDI 4655 und DIN EN 12831-3.

**Verhältnis zu DIN EN 12831-3 / A100 (Auslegung):**
- **Kein Widerspruch, sondern Rollentrennung.** 12831-3 bemisst Erzeuger und Speicher gegen ein Bedarfsprofil mit definierten Spitzenzapfungen; VDI 4655 beschreibt den **energetischen Tages-/Jahresgang** eines typischen Betriebs. Die Norm selbst grenzt ihre TWE-Profile ausdrücklich von genormten Profilen ab (Beispiel DIN EN 15450).
- **Praktische Konsequenz für WP-Plan:** VDI 4655 darf die Auslegungsrechnung nach 12831-3/A100 **nicht ersetzen und nicht überschreiben**. Sinnvolle Architektur: getrennte Pfade – **Auslegungspfad** (12831-3/A100, Bemessungsspitze, Speicherleistungskennzahl N_L) und **Bilanz-/Simulationspfad** (VDI 4655, Jahresenergie, Betriebsstunden, Taktung, JAZ, Wirtschaftlichkeit). Ergebnisse beider Pfade **nie** in einer Kennzahl mischen.
- **Achtung Bilanzgrenze:** VDI 4655 Q_TWE enthält **Zirkulationsverluste**. Wird der VDI-Wert unreflektiert als „Nutzenergie" in eine 12831-3-Rechnung gegeben, die Zirkulation separat aufschlägt, entsteht **Doppelzählung**.

**Verhältnis zu DIN V 18599-10 (Bedarfskennwerte / Nutzungsrandbedingungen):**
- **Ergänzung, kein Widerspruch – aber nicht direkt vergleichbar.** 18599-10 liefert **flächenbezogene** Nutzungsrandbedingungen/Bedarfskennwerte auf Nutzenergieebene; VDI 4655 liefert **personen-/WE-bezogene** Jahreswerte (500 kWh/Pers, 1000 kWh/WE) auf einer Bilanzebene **inkl. Verteil-, Leitungs- und Zirkulationsverlusten, exkl. Speicherverlusten** (S. 9).
- **Vor jedem Zahlenvergleich ist die Bilanzgrenze anzugleichen.** Ein direkter Vergleich „500 kWh/Pers vs. X kWh/(m²·a)" ohne Verlustbereinigung und ohne Flächen-/Belegungsannahme ist methodisch unzulässig. *(Ich nenne hier bewusst keine 18599-10-Zahlenwerte – die Norm liegt mir nicht vor; jede konkrete Gegenüberstellung ist mit dem Originaltext zu belegen.)*
- VDI 4655 selbst verweist für die Jahresenergiebedarfe auf **DIN V 4701-10 oder DIN V 18599** (Abschnitte 6.2, 6.2.1) und für TWW alternativ auf EnEV/GEG (Abschnitt 6.2.3). Die Norm sieht sich also **nicht** als Konkurrenz zu 18599, sondern nutzt sie als Eingangsgröße. **Das ist die natürliche Kopplung für WP-Plan:** Jahresmenge aus 18599 (oder aus dem WP-Plan-eigenen Bedarfsmodell), Zeitstruktur aus VDI 4655.

### 7.7 Lizenz- und Produktrisiko (bitte vor Implementierung entscheiden)

- Der Seitenvermerk „Vervielfältigung – **auch für innerbetriebliche Zwecke** – nicht gestattet" ist restriktiver als das übliche „nur für den internen Gebrauch". Das betrifft schon die **Weitergabe dieses Berichts** innerhalb des Hauses über den Kreis der Norm-Nutzungsberechtigten hinaus.
- Die eigentlichen Nutzdaten (Formvektoren, alle 45 Faktortabellen maschinenlesbar, Typtag-Reihenfolgen, PV-Profile) liegen auf der **beiliegenden CD-ROM** (Anhang, S. 42/43; S. 51: „Hier ist ein Datenträger eingeklebt."). **Der vorliegende PDF-Auszug enthält diese Daten nicht.** Für eine Implementierung sind die CD-ROM-Daten zu beschaffen.
- **Empfehlung:** Vor jeder Codierarbeit eine schriftliche Klärung mit VDI/Beuth zur Frage „Einbettung der VDI-4655-Datensätze in eine kommerziell vertriebene Planungssoftware" einholen. Alternativ: WP-Plan implementiert nur die **Methodik** (Gleichungen, Typtagsystematik – nicht schutzfähig als solche) und erwartet die **Datendateien beim Anwender**, der eine eigene Normlizenz besitzt (Import-Schnittstelle statt Auslieferung). Diese zweite Variante ist das risikoärmste Produktdesign.

### 7.8 Zusammenfassende Empfehlung

| Rolle | Empfehlung | Begründung |
|---|---|---|
| (a) Formvektor-/Typtagquelle für S2 (Bilanzpfad Wohnen) | **Ja, als wählbare Quelle** – EFH mit 1-min-Vektoren; MFH nur mit ausdrücklichem 15-min-Vorbehalt | Norm liefert genau die Struktur; MFH-Auflösung begrenzt |
| (b) Jahresgang-/Wetterkopplung | **Ja, mit hoher Priorität** – Typtagsystematik, HG 15/12 °C, 5-°C-Grenze, Bewölkung 5/8, Feiertag=Sonntag, 15 TRY-Zonen, PLZ-Tabelle A1 | Bestdokumentierter, unmittelbar nutzbarer Beitrag; Klärung Tagesmittel vs. 7-Tage-Mittel offen |
| (c) Validierungsreferenz | **Ja** – Jahresanker 500/1000 kWh, Prüfsummen, Beispielrechnung Abschnitt 8 als Regressionstest | Normbelegt und reproduzierbar |
| (d) Auslegungsspitzen | **Nein** – strikt ausschließen | Norm beansprucht es nicht; 15-min-MFH-Daten; lineare WE-Skalierung ohne Gleichzeitigkeit; keine Streuungsangaben; ausdrückliche Abgrenzung von DIN EN 15450 |
| Stromseite WP-Plan | **Ja** – Haushaltsstromprofile + Jahreswerte nutzbar; **WP-Strom und E-Mobilität müssen additiv modelliert werden** (Norm schließt sie explizit aus) | Abschnitt 4, 6.2.2 |

---

## 8. Offene Punkte und Unsicherheiten (ausdrücklich gekennzeichnet)

1. **⚠ TRY14 / EFH-NEH / F_TWE,ÜWH = −1,2637E-05** – passt nicht ins Muster, Ziffernfolge identisch mit einem Nachbarwert. **Verdacht auf Extraktionsfehler.** Am Original bzw. der CD-ROM verifizieren.
2. **⚠ MFH-Tabellen:** F_TWE,ÜWB ≡ F_TWE,ÜSH und F_el,ÜSH ≡ F_el,ÜSB in allen 15 Zonen. Im Normtext nicht erläutert. Klären, ob real oder Datenfehler.
3. **⚠ Alle in diesem Bericht wiedergegebenen Zahlenwerte** wurden aus der gerenderten PDF-Darstellung übernommen. Vor produktiver Nutzung sind sie gegen die maschinenlesbaren CD-ROM-Datensätze (Ordner C1/C2) abzugleichen. Die Prüfsummen aus 3.3.4 fangen Struktur-, nicht Einzelziffernfehler.
4. **⚠ F_el,vent,TT** (nur NEH-Tabellen 32–46) ist im gedruckten Normteil **nicht definiert**. Bedeutung (vermutlich Lüftungsstrom) unbestätigt.
5. **⚠ Zuordnungsgröße für die Typtagkategorie**: Tagesmitteltemperatur (Begriffsdefinition) vs. Sieben-Tage-Mittel (Tabellenangaben, CD-ROM-Ordner B). Nicht eindeutig aus dem gedruckten Text ableitbar.
6. **⚠ Anzahl und Größe der vermessenen Gebäude** (außer 5 EFH-Bestand + 5 NEH aus NOVAREF) sind im Normtext **nicht angegeben**; für MFH fehlen Anzahl der Objekte und deren WE-Zahl vollständig. Quelle wäre [2] Dubielzig et al., Fortschr.-Ber. VDI Reihe 6 Nr. 560 (2007) – nicht Bestandteil dieses PDF.
7. **⚠ Alle Aussagen zu demandlib 0.2.2** in Abschnitt 6 sind Indizienschlüsse ohne Codeeinsicht. Ebenso ist der Vergleich mit der Ausgabe 2008-05 nicht am Original erfolgt.
8. **⚠ Bild 4 (kumuliertes EFH-ÜWH-Profil)** wurde grafisch abgelesen; die genannten Stufenhöhen (~0,55 nach dem Morgenblock, ~0,77 nach 12 Uhr) sind Schätzwerte aus der Abbildung, keine Tabellenwerte.
9. **CD-ROM nicht vorhanden:** Der ausgewertete PDF-Auszug enthält den Datenträgerinhalt nicht (S. 51: „Hier ist ein Datenträger eingeklebt."). Sämtliche Formvektoren F_*,n,TT(t), die Typtag-Reihenfolgen und die PV-Profile fehlen damit für die Implementierung.
10. **DIN EN 12831-3 und DIN V 18599-10 lagen für diesen Bericht nicht vor.** Die Aussagen in 7.6 zum Verhältnis beruhen auf dem, was die VDI 4655 selbst zitiert bzw. nicht zitiert, sowie auf allgemeiner Normkenntnis; konkrete Zahlenvergleiche wurden bewusst unterlassen.agentId: a7cf8a986c16a4faf (use SendMessage with to: 'a7cf8a986c16a4faf', summary: '<5-10 word recap>' to continue this agent)
<usage>subagent_tokens: 158364
tool_uses: 3
duration_ms: 670310</usage>