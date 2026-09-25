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
