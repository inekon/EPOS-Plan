# Validierung des Zapfprofilgenerators an offen lizenzierten Messreihen

Erster Rechennachweis der **Stufe Z5** an **gemessenen** Reihen (Umsetzungskonzept
Zapfprofilgenerator, [Kapitel 7 Zeile Z5](../Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md)).
Der offene Punkt **K5** — die Freigabe von INEKON-Messreihen — ist damit **nicht** erledigt: Dieser
Lauf steht auf frei lizenzierten Fremddaten, nicht auf eigenen Objekten. Er beantwortet die Frage,
**ob der Nachweis technisch trägt und was er zeigt**; die Validierung an eigenen Objekten kommt
danach.

Gerechnet hat [`Werkzeuge/ZapfprofilValidierung`](../../../Werkzeuge/ZapfprofilValidierung/LIESMICH.md);
die Konverter der drei Quellen stehen unter
[`Werkzeuge/ZapfprofilValidierung/Konverter/`](../../../Werkzeuge/ZapfprofilValidierung/Konverter/LIESMICH.md).

**Dieses Papier führt nur Verhältniszahlen, Anteile und Zählungen** — keine gemessene Menge, keine
gemessene Leistung, keinen Objektnamen. Die Rohdaten und die umgesetzten Reihen liegen außerhalb des
Repositoriums und bleiben dort.

---

## 1. Die Quellen und ihre Namensnennung

Alle drei Datensätze stehen unter **CC BY 4.0**; die Namensnennung gehört in jede Veröffentlichung,
die diese Zahlen benutzt.

| Kennungen | Quelle | Inhalt |
|---|---|---|
| `NO-AB1` … `NO-NH4` | Sørensen, Å. L. et al.: *Measurement data on domestic hot water consumption and related energy use in hotels, nursing homes and apartment buildings in Norway.* Mendeley Data V2, doi:10.17632/m3xy22pf4j.2; Beschreibung: Data in Brief 2021, doi:10.1016/j.dib.2021.107228 | zwölf Gebäude im Raum Oslo (AB Wohngebäude, HO Hotels, NH Pflegeheime), je sechs bis acht Wochen 2018/2019, stündlich, mit getrennter Zapfenergie und Zirkulationsverlusten |
| `ES-EFH0` … `ES-EFH9` | *hihAigua dataset*, Zenodo, doi:10.5281/zenodo.18456405 | zehn spanische Wohnhäuser, Jahr 2025, Warmwasserzähler mit kumuliertem Zählerstand, Ablesungen etwa alle sieben Minuten |
| `US-922`, `US-1101` | NREL/OpenEI, *Domestic hot water distribution system losses and demand control* (Forbell), doi:10.25984/2204257 | zwei Mehrfamilienhäuser in New York mit zentraler Trinkwassererwärmung und Zirkulation, Juni 2013 bis April 2014, 5-Minuten-Werte; genommen ist die an den Zapfstellen abgegebene Energie |

**Geprüft und nicht verwendet:** Forschungsprojekt *Flexitility — Dezentrale
Trinkwasserzwischenspeicher*, Zenodo doi:10.5281/zenodo.17831069 (CC BY 4.0). Die Dateien führen den
**Gesamt-Trinkwasserdurchfluss am Hausanschluss**, kein getrenntes Warmwasser — für einen Vergleich
mit dem Zapfprofilgenerator ungeeignet.

**Drei der zwölf norwegischen Gebäude fallen aus**, weil kein zusammenhängendes Fenster von 30 Tagen
mit höchstens 5 % Lücken bleibt (längstes Fenster 20, 28 und 11 Tage). Geprüft wurden damit
**21 Objekte**.

---

## 2. Wie gerechnet wurde

* **Katalog:** `Referenzlaeufe/Kenndaten_Test.sqlite` — acht Nutzungsarten, 85 Parameter,
  24 Zapfkategorien. Die Zuordnung: AB und die beiden US-Häuser auf „Wohnen groß (abgeleitet)",
  HO auf „Krankenhaus (abgeleitet)", NH auf „Seniorenheim (abgeleitet)", die spanischen Wohnhäuser
  auf „Ein- und Zweifamilienhaus (abgeleitet)".
* **Rechenweg:** stochastisch, zehn Realisierungen je Objekt, feste Saat. Ohne Ensemble wäre das Band
  der Dauerlinie keine Aussage — die Lehre, an der es hängt (Konzept 3.6), spricht von der
  stochastischen Reihe.
* **Kalibriert vor dem Vergleich:** Der Jahreswert der Messreihe bringt das Mengengerüst auf ihr
  Niveau; danach prüfen Band, Formabgleich und Monatsanteile **Gestalt und Gleichzeitigkeit**, nicht
  die Schätzung der Bezugsmenge. Das Niveau steht allein im Kalibrierfaktor.
* **Bilanzgrenze je Objekt:** Wo die Quelle Zirkulationsverluste getrennt führt, ist die Messung ihre
  Summe mit der Zapfung und die Grenze `MitVerteilung`; sonst `Zapfstelle`, und dann rechnet auch die
  Vergleichsreihe ohne Zirkulation.
* **Teiljahr:** Die norwegischen Reihen decken 39 bis 143 Tage, die beiden US-Häuser 90 und 160 Tage;
  ihr Jahresmesswert ist über den Jahresgang der Rechnung **hochgerechnet** und benannt. Die zehn
  spanischen Reihen decken 299 bis 363 Tage.
* **Bezugsmengen sind bis auf zwei Platzhalter.** Belegt sind `NO-AB2` (56 Wohnungen) und `NO-HO1`
  (434 Zimmer); alle übrigen tragen runde Annahmen. Weil vor dem Vergleich kalibriert wird, tragen
  die drei Kriterien je Objekt diesen Fehler **nicht** — das vierte Kriterium (die √N-Skalierung)
  dagegen schon.

---

## 3. Das Ergebnis in vier Sätzen

1. **Die Energie stimmt nach der Kalibrierung bei allen 21 Objekten exakt** (Residuum 0). Der
   Kalibrierweg des Kerns trägt, auch aus einem Teiljahr.
2. **Die √N-Skalierung der Überschätzung ist bestätigt:** Die Steigung von ln(Spitzenverhältnis) über
   ln(N) beträgt **−0,29** über 21 Objekte und liegt damit im Band −0,5 ± 0,25. Die Spitze je Einheit
   fällt also mit der Objektgröße, wie das Konzept es annimmt (4.4) — und das, obwohl die meisten
   Bezugsmengen Platzhalter sind.
3. **Das Band der Dauerlinie ist an keinem Objekt erfüllt:** Die gemessene Spitze liegt überall
   **über** dem P85–P95-Band der gerechneten Reihe. Bei den großen Wohngebäuden knapp (Verhältnis
   0,36 bis 0,53 gegen ein Band von etwa 0,24 bis 0,39 — die Lehre ist fast getroffen), bei den
   Ein- und Zweifamilienhäusern weit (1,1 bis 8,2 gegen ein Band von 0,036 bis 0,087).
4. **Der Formabgleich hält die Schwelle von 0,01 nur an drei Objekten** (`NO-AB3`, `NO-AB4`,
   `US-1101`); die übrigen liegen bei 0,009 bis 0,038. Die Größenordnung ist plausibel, die Schwelle
   ist für gemessene Objekte eng.

Damit ist **kein Objekt grün** — aber die roten Kriterien sind Befunde mit Ursachen, nicht Fehler des
Rechenwegs. Abschnitt 5 nennt sie.

---

## 4. Die Zahlen

### 4.1 Ampel je Objekt

| Kennung | Nutzungsart | N | Ampel | Band | Form | Energie |
|---|---|---|---|---|---|---|
| ES-EFH0 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| ES-EFH1 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| ES-EFH2 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| ES-EFH3 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| ES-EFH4 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| ES-EFH5 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| ES-EFH6 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| ES-EFH7 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| ES-EFH8 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| ES-EFH9 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | rot | rot | rot | gruen |
| NO-AB1 | Wohnen groß (abgeleitet) | 60 | rot | rot | rot | gruen |
| NO-AB2 | Wohnen groß (abgeleitet) | 56 | rot | rot | rot | gruen |
| NO-AB3 | Wohnen groß (abgeleitet) | 60 | rot | rot | gruen | gruen |
| NO-AB4 | Wohnen groß (abgeleitet) | 60 | rot | rot | gruen | gruen |
| NO-HO1 | Krankenhaus (abgeleitet) | 434 | rot | rot | rot | gruen |
| NO-HO2 | Krankenhaus (abgeleitet) | 200 | rot | rot | rot | gruen |
| NO-HO4 | Krankenhaus (abgeleitet) | 200 | rot | rot | rot | gruen |
| NO-NH2 | Seniorenheim (abgeleitet) | 100 | rot | rot | rot | gruen |
| NO-NH4 | Seniorenheim (abgeleitet) | 100 | rot | rot | rot | gruen |
| US-1101 | Wohnen groß (abgeleitet) | 100 | rot | rot | gruen | gruen |
| US-922 | Wohnen groß (abgeleitet) | 100 | rot | rot | rot | gruen |

Zählung: **0 grün, 0 gelb, 21 rot.**

### 4.2 Die √N-Skalierung über alle Objekte

| Kriterium | Ampel | Maß | Schranke | Bemerkung |
|---|---|---|---|---|
| Wurzel-N-Skalierung (über alle Objekte) | gruen | -0.2899 | -0.5 ± 0.25 | Steigung der Ausgleichsgeraden von ln(Spitzenverhaeltnis) ueber ln(N) aus 21 Objekten |

| Kennung | N | Spitzenverhältnis [-] | 1/√N [-] | Skalierungsmaß [-] |
|---|---|---|---|---|
| ES-EFH0 | 3 | 3.2524 | 0.5774 | 5.6333 |
| ES-EFH1 | 3 | 3.1342 | 0.5774 | 5.4286 |
| ES-EFH2 | 3 | 5.4918 | 0.5774 | 9.5121 |
| ES-EFH3 | 3 | 1.8736 | 0.5774 | 3.2452 |
| ES-EFH4 | 3 | 2.8519 | 0.5774 | 4.9396 |
| ES-EFH5 | 3 | 8.1958 | 0.5774 | 14.1956 |
| ES-EFH6 | 3 | 2.4185 | 0.5774 | 4.189 |
| ES-EFH7 | 3 | 1.4351 | 0.5774 | 2.4857 |
| ES-EFH8 | 3 | 1.4735 | 0.5774 | 2.5522 |
| ES-EFH9 | 3 | 1.1213 | 0.5774 | 1.9421 |
| NO-AB1 | 60 | 0.5061 | 0.1291 | 3.9205 |
| NO-AB2 | 56 | 0.356 | 0.1336 | 2.6644 |
| NO-AB3 | 60 | 0.5274 | 0.1291 | 4.0854 |
| NO-AB4 | 60 | 0.4816 | 0.1291 | 3.7304 |
| NO-HO1 | 434 | 1.7003 | 0.048 | 35.4212 |
| NO-HO2 | 200 | 1.7635 | 0.0707 | 24.9402 |
| NO-HO4 | 200 | 1.1398 | 0.0707 | 16.1189 |
| NO-NH2 | 100 | 0.8413 | 0.1 | 8.4134 |
| NO-NH4 | 100 | 0.6486 | 0.1 | 6.4858 |
| US-1101 | 100 | 0.7703 | 0.1 | 7.7032 |
| US-922 | 100 | 0.4834 | 0.1 | 4.8336 |

### 4.3 Kennzahlen je Objekt

| Kennung | Energie­verhältnis | Spitzen­verhältnis | Band | Lage | Skalierungs­maß | Formmaß (Schwelle) | Monats­abweichung | Kalibrier­faktor | Residuum |
|---|---|---|---|---|---|---|---|---|---|
| ES-EFH0 | 1 | 3.2524 | 0.036 … 0.087 | Oberhalb | 5.6333 | 0.0297 (0.01) | 0.0382 | 0.7488 | 0 |
| ES-EFH1 | 1 | 3.1342 | 0.036 … 0.087 | Oberhalb | 5.4286 | 0.0245 (0.01) | 0.0512 | 0.5212 | 0 |
| ES-EFH2 | 1 | 5.4918 | 0.036 … 0.087 | Oberhalb | 9.5121 | 0.0263 (0.01) | 0.04 | 0.6615 | 0 |
| ES-EFH3 | 1 | 1.8736 | 0.036 … 0.087 | Oberhalb | 3.2452 | 0.0372 (0.01) | 0.0499 | 0.1631 | 0 |
| ES-EFH4 | 1 | 2.8519 | 0.036 … 0.087 | Oberhalb | 4.9396 | 0.036 (0.01) | 0.0291 | 0.2987 | 0 |
| ES-EFH5 | 1 | 8.1958 | 0.036 … 0.087 | Oberhalb | 14.1956 | 0.0297 (0.01) | 0.0316 | 0.7906 | 0 |
| ES-EFH6 | 1 | 2.4185 | 0.036 … 0.087 | Oberhalb | 4.189 | 0.0348 (0.01) | 0.0578 | 1.2867 | 0 |
| ES-EFH7 | 1 | 1.4351 | 0.036 … 0.087 | Oberhalb | 2.4857 | 0.0285 (0.01) | 0.0229 | 0.7088 | 0 |
| ES-EFH8 | 1 | 1.4735 | 0.036 … 0.087 | Oberhalb | 2.5522 | 0.0214 (0.01) | 0.019 | 0.8672 | 0 |
| ES-EFH9 | 1 | 1.1213 | 0.036 … 0.087 | Oberhalb | 1.9421 | 0.0381 (0.01) | 0.0437 | 0.4424 | 0 |
| NO-AB1 | 1 | 0.5061 | 0.254 … 0.372 | Oberhalb | 3.9205 | 0.0121 (0.01) | 0.0359 | 4.4346 | 0 |
| NO-AB2 | 1 | 0.356 | 0.24 … 0.352 | Oberhalb | 2.6644 | 0.0154 (0.01) | 0.0447 | 4.4891 | 0 |
| NO-AB3 | 1 | 0.5274 | 0.269 … 0.393 | Oberhalb | 4.0854 | 0.0086 (0.01) | 0.0097 | 3.3345 | 0 |
| NO-AB4 | 1 | 0.4816 | 0.248 … 0.375 | Oberhalb | 3.7304 | 0.0093 (0.01) | 0.0254 | 4.3559 | 0 |
| NO-HO1 | 1 | 1.7003 | 0.517 … 0.624 | Oberhalb | 35.4212 | 0.0176 (0.01) | 0.0299 | 1.6891 | 0 |
| NO-HO2 | 1 | 1.7635 | 0.481 … 0.596 | Oberhalb | 24.9402 | 0.019 (0.01) | 0.0227 | 2.6829 | 0 |
| NO-HO4 | 1 | 1.1398 | 0.509 … 0.63 | Oberhalb | 16.1189 | 0.0204 (0.01) | 0.0343 | 1.671 | 0 |
| NO-NH2 | 1 | 0.8413 | 0.274 … 0.394 | Oberhalb | 8.4134 | 0.0236 (0.01) | 0.03 | 0.5913 | 0 |
| NO-NH4 | 1 | 0.6486 | 0.307 … 0.442 | Oberhalb | 6.4858 | 0.0194 (0.01) | 0.0145 | 1.7446 | 0 |
| US-1101 | 1 | 0.7703 | 0.32 … 0.44 | Oberhalb | 7.7032 | 0.0092 (0.01) | 0.0529 | 2.8823 | 0 |
| US-922 | 1 | 0.4834 | 0.32 … 0.44 | Oberhalb | 4.8336 | 0.0115 (0.01) | 0.0288 | 5.122 | 0 |

Das Energieverhältnis ist 1, weil vor dem Vergleich kalibriert wird; das Niveau steht im
Kalibrierfaktor.

---

## 5. Was die roten Kriterien sagen

**(a) Das Band bei den Ein- und Zweifamilienhäusern** (Verhältnis bis 8,2, Band 0,036 … 0,087). Das
Band ist hier extrem niedrig: Die gerechnete stochastische Reihe für drei Personen hat **eine**
Extremstunde, gegen die P85 und P95 ihrer Dauerlinie verschwinden. Das ist der Kern des Befunds — bei
kleinen Einheitenzahlen trägt eine einzelne gezogene Gleichzeitigkeit die Jahresspitze, und ein
Quantilband der Dauerlinie ist dort keine brauchbare Messlatte. Für kleine Objekte braucht das
Kriterium eine andere Form (Vorschlag: gegen das Perzentil der **Ensemblespitzen** statt gegen ein
Quantil der Dauerlinie einer Realisierung).

**(b) Das Band bei den großen Objekten** (Verhältnis 0,36 … 0,53 gegen 0,24 … 0,39) liegt knapp und
durchweg auf derselben Seite. Die Rechnung **unterschätzt** hier die gemessene Spitze leicht — die
Lehre aus Konzept 3.6 („Messspitze bei etwa P90") ist der Richtung nach bestätigt, dem Betrag nach
etwas zu niedrig angesetzt. Ein Band bis P97 oder P98 hätte fast alle großen Objekte eingefangen.

**(c) Der Formabgleich.** Die mittlere Stundenabweichung liegt bei 0,009 bis 0,038, die Schwelle bei
0,01. Drei Ursachen, jede ohne Rechenwegfehler: Die Quellen nennen **keine Feiertage** (ein Feiertag
fällt damit in den Tagtyp „Werktag"); jedes Objekt rechnet mit **einer Zone**, obwohl ein Hotel mit
Restaurant oder ein Wohngebäude mit Gewerbe eine Mischnutzung ist; und die Tagesgänge des Katalogs
sind aus VDI 6002 **abgeleitet**, also Mittelwerte einer Gebäudeklasse und nicht eines Objekts. Eine
Abweichung von 0,02 heißt, dass etwa ein Viertel der Tagesenergie in anderen Stunden liegt — für ein
einzelnes Objekt gegen einen Klassenmittelwert ist das wenig.

**(d) Die Kalibrierfaktoren** reichen von 0,16 bis 5,1. Sie messen, wie weit die **geschätzte
Bezugsmenge** neben der Wirklichkeit liegt, nicht den Rechenweg. Bei den beiden belegten Objekten
stehen 4,5 (`NO-AB2`) und 1,7 (`NO-HO1`). Der erste Wert ist ein **Zuordnungsfehler dieses Laufs**,
kein Befund am Generator: Für `AB` ist die Bezugsmenge in Personen zu setzen; 56 Wohnungen sind keine
56 Personen.

---

## 6. Folgen

| Nr. | Folge | Für wen |
|---|---|---|
| V1 | **Bezugsmengen und Bezugsarten nachtragen** — die Gebäudekennwerte der drei Veröffentlichungen in die `objekt.json` der Objekte, und für die norwegischen Wohngebäude Personen statt Wohnungen. Danach trägt auch das vierte Kriterium belastbar | Anwender bzw. Agent eines Folgepostens |
| V2 | **Feiertage je Land und Jahr** in die `objekt.json` eintragen; danach den Formabgleich neu lesen | Folgeposten |
| V3 | **Das Bandkriterium für kleine Einheitenzahlen überarbeiten** — Quantil der Ensemblespitzen statt Quantil der Dauerlinie, oder eine Untergrenze für N, unter der das Kriterium gelb bleibt. Berührt `Messvergleich` im Kern und damit die Stufe Z5 | Konzeptentscheid, dann Kern |
| V4 | **Die Bandgrenzen prüfen** (Parameter `Zapfprofil.Validierung.Band.Unten`/`.Oben`, heute 0,85 / 0,95): Für die großen Objekte lag die Messspitze durchweg knapp darüber | Konzeptentscheid |
| V5 | **Die Formschwelle prüfen** (Parameter `Zapfprofil.Validierung.Formschwelle`, heute 0,01): Sie wurde an erfundenen Reihen gesetzt; gemessene Objekte gegen einen Klassenmittelwert liegen bei 0,01 bis 0,04 | Konzeptentscheid |
| K5 | **Die Validierung an eigenen, freigegebenen Objekten** bleibt offen. Sie ist der eigentliche Nachweis der Stufe; das Werkzeug und der Weg stehen jetzt | Anwender |

**Was dieser Lauf schon belegt:** Der Nachweisweg trägt — Leser, Katalogquelle, Rechnung,
Kalibrierung, Vergleich, Ampel und Bericht laufen an echten, fremden Reihen durch, und die
Kalibrierung ist bei allen 21 Objekten exakt. Zwei der vier Kriterien sind damit belegt (Energie,
√N-Skalierung), zwei brauchen entweder bessere Eingangsangaben (V1, V2) oder eine Überarbeitung des
Kriteriums selbst (V3 bis V5).

---

## 7. Zweiter Lauf

Nachgetragen am 26.09.2026 (Nachtrag N27 im
[Umsetzungskonzept](../Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md)). Die Folgen V1 bis V5
aus Abschnitt 6 sind abgearbeitet oder benannt geschlossen (7.1); danach sind alle 21 Objekte mit
denselben Einstellungen wie im ersten Lauf neu gerechnet — zehn Realisierungen, feste Saat, Katalog
`Referenzlaeufe/Kenndaten_Test.sqlite`, Hotels weiter auf „Krankenhaus (abgeleitet)", norwegische
Bilanzgrenze wie im ersten Lauf. Die Berichtswache des Werkzeugs war ohne Fund.

### 7.1 Was an den Eingängen geändert ist

| Folge | Ergebnis |
|---|---|
| V1 Bezugsmengen | **Belegt oder abgeleitet statt Platzhalter.** Norwegen aus Tabelle 1 der Beschreibung (Data in Brief 2021): Wohnungen 96, 56, 56, 86, Hotelzimmer 434, 355, 139, 151, Pflegeheimzimmer 148, 52, 50, 96. Wohngebäude in Personen mit einer Belegung nach der Schlafzimmerzahl, die der Text nennt (ein Schlafzimmer: 1,5 Personen, zwei: 2,0, zwei bis drei: 2,5 — **Annahme**); Hotelzimmer als ein Bett (Annahme), Pflegeheimzimmer als Betten (belegt). New York: „approximately 50 apartments" je Haus (Building America Case Study DOE/GO-102016-4704, 2016) mal 2,5 Personen (Annahme). Spanien: Die Quelle nennt keine Bewohnerzahl; 2,5 Personen stehen nur als Rechenwert, Herkunft „unbekannt". Jede `objekt.json` führt die Herkunft (`bezugsmenge_herkunft`), die √N-Skalierung nimmt nur belegte und abgeleitete Mengen. Die Kennwerte stehen mit Zitat in den Konvertern |
| V2 Kalender | **Feiertage des Messjahrs je Land**, berechnet in den Konvertern: Norwegen die gesetzlichen (Helligdagsloven, 1./17. Mai), Spanien die landesweiten (die Quelle nennt keine Region), USA die Bundesfeiertage (5 U.S.C. 6103). **Ferien** trägt keine Quelle, und im Format wären sie Ruhetage — für Wohnhäuser, Hotels und Pflegeheime falsch; sie bleiben leer. **Der Kern kannte Feiertage nur auf der Seite der Rechnung**: Die Messung ordnete jeden Tag nach seinem Wochentag ein, eine Liste in `objekt.json` hätte den Formabgleich nicht erreicht. `Messvergleichseingang.MessFeiertage` schließt das; ohne Angabe ist der Dialogweg unverändert. Dazu die **Zeitzone der spanischen Reihen**: Die Quelle ist UTC, die Bewohner leben nach der Ortszeit — der Konverter rechnet in MEZ/MESZ um |
| V3 Band | **Analyse „Band je Größenklasse"** im Werkzeug, ohne Ampel (7.3); die Ampel bleibt das Konzeptkriterium mit den in ZU21 bestätigten Bandgrenzen |
| V4 Bandgrenzen | in V3 aufgegangen — dieselbe Frage, jetzt mit Zahlen je Größenklasse (7.3) |
| V5 Formschwelle | **benannt geschlossen**: ZU21 hat 0,01 bestätigt; die Zahlen des zweiten Laufs (7.2) geben für große Wohnobjekte keinen Anlass zur Änderung, für Einzelhaushalte gehört die Frage zu ZU35 |
| Zonen je Objekt | **nicht anwendbar, geschlossen.** Die Quelle nennt eine Küche nur für ein Hotel (HO4: „a restaurant and large kitchen facilities") und keine Mahlzeitenzahl; für eine zweite Zone fehlt die Bezugsmenge. Die Pflegeheime kochen zentral außer Haus („Most hot food is made at centralized kitchens"), die Wohngebäude nennen kein Gewerbe |
| neu: Messartefakte | **Die Jahresspitze einiger spanischer Haushalte war kein Zapfereignis**: ein Nachholwert nach einer Übertragungslücke von bis zu zwei Wochen oder eine einzelne Ablesung mit dem Zehnfachen des nächstgrößten Durchflusses. Der Konverter verwirft Intervalle über zwei Stunden und mittlere Durchflüsse über 20 Liter je Minute (Berechnungsdurchfluss einer Badewanne nach DIN EN 806-3: 18 Liter je Minute) und zählt sie (zwei bis sieben je Haushalt); den übrigen Zuwachs verteilt er zeitanteilig auf die Stunden |

### 7.2 Ampel je Objekt

| Kennung | Nutzungsart | N | Herkunft N | Ampel | Band | Form | Energie |
|---|---|---|---|---|---|---|---|
| ES-EFH0 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| ES-EFH1 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| ES-EFH2 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| ES-EFH3 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| ES-EFH4 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| ES-EFH5 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| ES-EFH6 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| ES-EFH7 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| ES-EFH8 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| ES-EFH9 | Ein- und Zweifamilienhaus (abgeleitet) | 3 | unbekannt | rot | rot | rot | gruen |
| NO-AB1 | Wohnen groß (abgeleitet) | 144 | abgeleitet | rot | rot | rot | gruen |
| NO-AB2 | Wohnen groß (abgeleitet) | 84 | abgeleitet | rot | rot | rot | gruen |
| NO-AB3 | Wohnen groß (abgeleitet) | 112 | abgeleitet | rot | rot | gruen | gruen |
| NO-AB4 | Wohnen groß (abgeleitet) | 215 | abgeleitet | rot | rot | gruen | gruen |
| NO-HO1 | Krankenhaus (abgeleitet) | 434 | abgeleitet | rot | rot | rot | gruen |
| NO-HO2 | Krankenhaus (abgeleitet) | 355 | abgeleitet | rot | rot | rot | gruen |
| NO-HO4 | Krankenhaus (abgeleitet) | 151 | abgeleitet | rot | rot | rot | gruen |
| NO-NH2 | Seniorenheim (abgeleitet) | 52 | belegt | rot | rot | rot | gruen |
| NO-NH4 | Seniorenheim (abgeleitet) | 96 | belegt | rot | rot | rot | gruen |
| US-1101 | Wohnen groß (abgeleitet) | 125 | abgeleitet | rot | rot | gruen | gruen |
| US-922 | Wohnen groß (abgeleitet) | 125 | abgeleitet | rot | rot | rot | gruen |

**Gegen den ersten Lauf:**

| Kriterium | erster Lauf grün / gelb / rot | zweiter Lauf grün / gelb / rot |
|---|---|---|
| Band der Dauerlinie | 0 / 0 / 21 | 0 / 0 / 21 |
| Formabgleich | 3 / 0 / 18 | 3 / 0 / 18 (dieselben drei: `NO-AB3`, `NO-AB4`, `US-1101`) |
| Energie nach Kalibrierung | 21 / 0 / 0 | 21 / 0 / 0 |
| √N-Skalierung | grün, −0,29 aus 21 Objekten | **rot, +0,54 aus 11 Objekten mit belastbarer Bezugsmenge**; über alle 21 zum Vergleich −0,06 |
| Objekte | 0 / 0 / 21 | 0 / 0 / 21 |

| Kennzahl | erster Lauf | zweiter Lauf |
|---|---|---|
| Kalibrierfaktor Norwegen | 0,59 … 4,49 | 1,12 … 3,08 |
| Kalibrierfaktor New York | 2,88 / 5,12 | 2,31 / 4,11 |
| Spitzenverhältnis Haushalte (N = 3) | 1,12 … 8,20 | 0,60 … 3,20 |
| Spitzenverhältnis große Wohnobjekte | 0,36 … 0,77 | 0,46 … 0,86 |
| Spitzenverhältnis Hotels, Pflegeheime | 0,65 … 1,76 | 0,66 … 1,79 |
| Formmaß große Wohnobjekte | 0,0086 … 0,0154 | 0,0085 … 0,0153 |
| Formmaß Hotels, Pflegeheime | 0,0176 … 0,0236 | 0,0183 … 0,0247 |
| Formmaß Haushalte | 0,0214 … 0,0381 | 0,0224 … 0,0418 |

### 7.3 Was sich verbessert hat und was nicht

**(a) Die Bezugsmengen (V1) machen die Kalibrierfaktoren plausibel** — sie liegen für Norwegen jetzt
zwischen 1,1 und 3,1 statt zwischen 0,6 und 4,5. Und sie **nehmen der √N-Skalierung die Bestätigung**:
Die Steigung −0,29 des ersten Laufs hing an den Platzhaltern. Mit belegten Mengen steigt das
Spitzenverhältnis mit N (+0,54), vor allem weil die drei Hotels die größten N tragen und mit dem
Tagesgang „Krankenhaus" ihre Morgenspitze nicht treffen (Spitzenverhältnis 1,1 bis 1,8). Auch unter
den sechs großen Wohnobjekten allein fällt es nicht mit N (Steigung +0,52) — ihre N reichen nur von
84 bis 215, kaum ein Faktor 2,6, und über so wenig Spanne ist die Steigung Streuung. **Das ist kein
Befund gegen das 1/√N-Gesetz**, sondern die Grenze dieser Stichprobe: Die Größen einer Nutzungsart
spannen keine Größenordnung. Die Prüfung braucht Objekte **einer** Nutzungsart über eine Größenordnung
von N — das bleibt K5.

**(b) Die Feiertage (V2) ändern den Formabgleich kaum.** Eine Gegenrechnung desselben Laufs ohne
Feiertage verschiebt das Formmaß je Objekt um höchstens 0,005, und kein Objekt wechselt die Ampel. Die
Feiertage sind wenige Tage des Messfensters; die Abweichung liegt in der **Tagesgestalt der
Gebäudeart gegen das Klassenmittel** aus VDI 6002, nicht im Kalender. Die Korrektur gehört trotzdem in
den Weg: Ohne sie verglich der Formabgleich an jedem Feiertag Ungleiches.

**(c) Die Messartefakte waren der größte Teil der „weit roten" Haushalte.** Das Spitzenverhältnis
fällt von bis zu 8,2 auf höchstens 3,2. Rot bleibt das Band dort trotzdem — aus dem Grund, den
Abschnitt 5 (a) nennt.

**(d) Band je Größenklasse (V3, Analyse ohne Ampel).** Das Werkzeug weist je Objekt aus, **welches
Quantil der gerechneten Dauerlinie die Messspitze trifft**, und hält sie gegen die **Jahresspitzen
der Realisierungen**:

| Klasse | Objekte | Perzentil der Messspitze kleinstes … Median … größtes | im Konzeptband P85–P95 | über der Rechenspitze | im Bereich der Ensemblespitzen |
|---|---|---|---|---|---|
| N < 10 (Haushalte) | 10 | 0,9978 … 1 … 1 | 0 | 6 | 3 |
| 10 ≤ N < 100 | 3 | 0,9621 … 0,9856 … 0,9982 | 0 | 0 | 0 |
| N ≥ 100 | 8 | 0,9716 … 0,9989 … 1 | 0 | 3 (die Hotels) | 1 |

Ohne die Hotels (Katalogtyp fehlt) liegen die acht Wohn- und Pflegeobjekte mit N ≥ 10 zwischen den
Perzentilen **0,962 und 0,9994** — alle über dem Konzeptband, alle unter der Rechenspitze. Die Lehre
„Messspitze bei etwa P90 der synthetischen Dauerlinie" (Konzept 3.6) ist der **Richtung** nach
bestätigt (die Rechnung überschätzt die Spitze), dem **Quantil** nach nicht: An Stundenwerten
gemessener Objekte liegt die Spitze bei P96 bis P99,9. Bei den Haushalten trifft die Messspitze die
obersten Stunden der Dauerlinie oder liegt darüber; ein Quantilband der Dauerlinie misst dort die
Ziehung einer einzelnen Stunde. Die Ensemblespitzen fangen die Messspitze bei drei von zehn
Haushalten ein — auch das ist keine brauchbare Messlatte für ein Einzelobjekt.

### 7.4 Empfehlung zum Band

**Frage ZU35 an den Anwender** (Konzept Kapitel 9): das Bandkriterium nach Größenklasse.

* **N ≥ 10: Band P95 bis P99,9** statt P85 bis P95 (Parameter `Zapfprofil.Validierung.Band.Unten`
  0,95, `.Oben` 0,999). Es hätte alle acht Wohn- und Pflegeobjekte des zweiten Laufs eingefangen und
  bleibt ein Band — eine Rechnung, die die Spitze gar nicht überschätzt, fiele weiter heraus.
  **Vorbehalt:** Der Vorschlag ist an denselben Daten abgelesen, die er einfängt; übernommen werden
  sollte er erst, wenn die eigenen Objekte aus K5 ihn bestätigen. Bis dahin bleibt die Ampel beim
  bestätigten P85–P95, und die Analyse steht daneben.
* **N < 10: kein Band der Dauerlinie**, das Kriterium ist dort „nicht bewertbar" (gelb) — eine
  Setzung des Werkzeugs, keine Parameteränderung. Den Formabgleich betrifft dieselbe Grenze: Ein
  einzelner Haushalt gegen ein Klassenmittel liegt bei 0,022 bis 0,042; eine Schwelle für ihn wäre
  eine eigene Frage, keine Änderung der bestätigten 0,01.
* **Hotels** gehören nicht in die Entscheidungsgrundlage, solange der Katalog keinen Typ „Hotel"
  führt (Folge V6).

### 7.5 Stand der Folgen

| Nr. | Folge | Stand |
|---|---|---|
| V1 | Bezugsmengen und Bezugsarten | **erledigt** (7.1); New York und Spanien tragen eine benannte Annahme bzw. „unbekannt" |
| V2 | Feiertage je Land und Jahr | **erledigt** (7.1), samt Kernergänzung `MessFeiertage` und Ortszeit der spanischen Reihen |
| V3 | Bandkriterium für kleine Einheitenzahlen | **Analyse erledigt** (7.3), Entscheid als ZU35 offen |
| V4 | Bandgrenzen | in V3 und ZU35 aufgegangen |
| V5 | Formschwelle | **geschlossen** — ZU21 bestätigt; Einzelhaushalte unter ZU35 |
| V6 | **Katalogtyp „Hotel"** (Bezugsart Betten oder Zimmer, eigener Tages- und Wochengang): Die Hotels treffen mit „Krankenhaus" ihre Spitze nicht | **erledigt 26.09.2026** (N31): „Hotel (aus Messung)" im freien Paketteil — VDI 6002 führt kein Hotel, der Typ ist das Mittel der drei Hotelreihen dieses Berichts; ein dritter Lauf mit ihm ist deshalb eine Übertragungsprobe (Folge V8) |
| V7 | **Spitzenstreuung im Werkzeugbericht** bezieht die Ensemblespitzen der unkalibrierten Rechnung auf die Spitze der kalibrierten Reihe; bei einem Kalibrierfaktor ungleich 1 ist sie um diesen Faktor verschoben. Die Analyse 7.3 bezieht beide Seiten auf dieselbe Realisierung und ist davon frei | **erledigt 26.09.2026** (N31): die Realisierungsspitzen gehen mit dem Streckfaktor der Zapfung auf die Stufe der kalibrierten Reihe |
| K5 | Validierung an eigenen, freigegebenen Objekten — auch die Prüfung der √N-Skalierung und die Bestätigung von ZU35 | Anwender |
