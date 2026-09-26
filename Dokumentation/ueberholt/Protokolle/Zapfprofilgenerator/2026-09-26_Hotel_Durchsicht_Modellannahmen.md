# Hotel (aus Messung): fachliche Durchsicht der Modellannahmen (26.09.2026)

Protokoll der **Welle #561**. Auftrag: Anwenderentscheid 26.09.2026 „Hotel-Modellannahmen:
ausführen, keine .eml anfragen" — fachliche Durchsicht des Katalogtyps „Hotel (aus Messung)"
(Umsetzungskonzept Zapfprofilgenerator ZU36, Nachtrag N31 (A); Zeile „Hotel (aus Messung):
Kennwerte" der [Prüfliste ZU21](../../../aktuell/Zapfprofilgenerator/2026-09-25_Pruefliste_ZU21_Setzungen.md)).

**Quelle** (Namensnennung nach CC BY 4.0): Sørensen, Å. L. et al.: Measurement data on domestic hot
water consumption and related energy use in hotels, nursing homes and apartment buildings in Norway.
Mendeley Data V2, doi:10.17632/m3xy22pf4j.2; Beschreibung Data in Brief 37 (2021) 107228,
doi:10.1016/j.dib.2021.107228 — CC BY 4.0. Die Rohdaten liegen außerhalb des Repositoriums; gerechnet
ist mit eigenen Skripten auf der Regel `Referenzlaeufe/Skripte/hotel_aus_messung_bauen.py` (dieselben
Fenster, derselbe Kanal Zapfenergie, dieselben Formeln).

**Rahmen.** Worktree `zv910`, Opus 5.5. Kein Bau, kein Test, Testdatenbank und Paketteil nicht
angefasst. **K5:** Dieses Protokoll nennt von den Messreihen nur Verhältnisse, Anteile und Zählungen;
Mengen mit Einheit sind entweder die veröffentlichten Katalogkennwerte (3,2 / 3,9 / 4,9 kWh je Zimmer
und Tag) oder ausdrücklich als **Literatur** gekennzeichnet.

**Gegenstand.** Bedarf 3,2 / 3,9 / 4,9 kWh je Zimmer und Tag (niedrig / mittel / hoch), Wochen- und
Stundenanteile als ungewichtetes Mittel der Hotels HO1 (434 Zimmer), HO2 (355) und HO4 (151),
Monatsfaktoren 1, Bezugsart Bett (ein Zimmer = ein Bett), Kalenderart Betrieb, Bezug 60/12 °C.

---

## (a) Bedarf je Zimmer: Streuung und Literatur

**Zwischen den Hotels.** Tagesbedarf je Zimmer relativ zum Mittel der drei: HO1 0,94, HO2 0,82,
HO4 1,24. Die Spanne hoch/niedrig ist 1,52. Das deckt sich mit dem dritten Validierungslauf (8.4 des
[Validierungsberichts](../../../aktuell/Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md)):
Kalibrierfaktoren 0,83 bis 1,23 gegen das Mittel.

**Über die Messwochen.** Wochenmittel relativ zum Mittel des eigenen Fensters (nur volle
Kalenderwochen): HO1 0,58 bis 1,19 (vier volle Wochen, Variationskoeffizient 0,27), HO2 0,81 bis
1,18 (sechs Wochen, 0,11), HO4 0,70 bis 1,21 (neunzehn Wochen, 0,12). Die beiden tiefsten Wochen
sind in beiden Jahren die **Osterwoche** (HO1 KW 13/2018: 0,58; HO4 KW 16/2019: 0,70) — das Bild
eines Stadthotels mit Geschäftsreisenden. HO1 ohne die Osterwoche läge bei etwa 1,08 seines
Fenstermittels; der Katalogwert enthält die Osterwochen also mit (HO1 und HO4) und liegt dadurch
eher an der unteren Seite eines Betriebsmittels. Die Tagesstreuung (Variationskoeffizient der
Tagessummen) liegt bei 0,17 bis 0,32.

**Literatur (Größenordnung, nicht aus einer Norm übernommen).** Der Katalogwert 3,9 kWh je Zimmer
und Tag entspricht bei 60/12 °C (48 K, 1,163 Wh je Liter und Kelvin, also rund 56 Wh je Liter)
rund 70 l Warmwasser von 60 °C je Zimmer und Tag; die Stufen 3,2 / 4,9 entsprechen rund 57 / 88 l.
Zum Vergleich allgemein zugängliche Faustwerte:
- *ASHRAE Handbook, HVAC Applications, Kapitel Service Water Heating* (Tabelle für Motels): im
  Tagesmittel je Einheit rund 10 bis 20 gal (≈ 38 bis 76 l) bei 140 °F (60 °C), mit fallender
  Menge bei wachsender Zahl der Einheiten — **Literatur**, entspricht rund 2,1 bis 4,2 kWh je Einheit
  und Tag bei 48 K.
- *Deutschsprachige Fachliteratur* (Recknagel, Taschenbuch für Heizung und Klimatechnik;
  Planungsunterlagen der Hotellerie und der Speicherhersteller): je nach Ausstattung rund 50 bis
  120 l je Bett bzw. Gast und Tag bei 60 °C — **Literatur**, entspricht rund 2,8 bis 6,7 kWh je
  Bett und Tag. Die Werte sind hier als Spanne aus dem Gedächtnis der Fachliteratur genannt, nicht
  im Auftrag nachgeschlagen; sie dienen nur der Einordnung.

Der Katalogwert liegt in beiden Spannen, im mittleren Bereich. **Belegung:** Die Messung kennt weder
Belegung noch Gästezahl. Der Wert gilt **je vorhandenem Zimmer** über alle Tage, also einschließlich
leerer Zimmer, und für das **ganze Haus** (was Küche, Wäscherei oder Wellness am selben Warmwasser
ziehen, steckt mit darin — die Quelle trennt das in unseren Dateien nicht). Bei einer für Stadthotels
üblichen Zimmerauslastung von 60 bis 80 % und 1,3 bis 1,6 Gästen je belegtem Zimmer (**Literatur**,
Größenordnung) wären das je Gast rund 55 bis 90 l bei 60 °C — ebenfalls im Band der Faustwerte.
**Temperaturbezug:** Die Dateien führen Energie, keine Temperaturen; 60/12 °C ist eine Setzung wie bei
den abgeleiteten Zeilen. Läge die wirkliche Spreizung in Oslo bei 45 bis 55 K statt 48 K, verschöbe das
die daraus abgeleitete Literzahl um den Faktor 0,87 bis 1,07 — nicht die Energie, mit der der
Generator rechnet.

**Bewertung: haltbar.** Wert und Stufen liegen in der Größenordnung der Literatur; Vorbehalte:
eine Region, drei Häuser, Osterwochen im Mittel, Belegung unbekannt.

## (b) Ungewichtetes gegen zimmergewichtetes Mittel

Gebildet sind je Tagtyp die 24 Stundenanteile und die sieben Wochenanteile einmal ungewichtet (Regel),
einmal nach Zimmerzahl gewichtet und einmal nach Zimmerzahl × Bedarf je Zimmer (Anteil am
gemeinsamen Verbrauch) gewichtet. Maß wie die Formschwelle: mittlere absolute Abweichung der Anteile.

| Vektor | zimmergewichtet: mittl. \|Δ\| | größte \|Δ\| (Stunde) | Spitze gew./ungew. | verbrauchsgewichtet: mittl. \|Δ\| | größte \|Δ\| | Spitze gew./ungew. |
|---|---|---|---|---|---|---|
| Werktag | 0,0023 | 0,011 (7–8 Uhr) | 1,07 | 0,0017 | 0,008 (7–8 Uhr) | 1,05 |
| Samstag | 0,0015 | 0,006 (7–8 Uhr) | 1,01 | 0,0010 | 0,003 | 1,00 |
| Sonn-/Feiertag | 0,0016 | 0,009 (7–8 Uhr) | 1,03 | 0,0010 | 0,006 | 1,02 |
| Woche (7 Anteile) | 0,0019 | 0,004 | – | 0,0008 | 0,002 | – |

Alle mittleren Abweichungen liegen bei einem Viertel der Formschwelle 0,01 oder darunter. Jedes
einzelne Hotel weicht vom ungewichteten Mittel um 0,004 bis 0,009 ab (Werktag 0,006 / 0,006 / 0,009),
also ebenfalls unter der Schwelle — die drei Formen sind einander ähnlich; die Gewichtung hebt vor
allem die Morgenspitze der beiden großen Häuser um bis zu 7 % an (vgl. 8.4 des Validierungsberichts:
die großen Häuser haben die dichtere Morgenspitze).

**Bedarf:** zimmergewichtet / ungewichtet = 0,94 (gerundet 3,7 statt 3,9 kWh je Zimmer und Tag). Die
Verschiebung ist kleiner als die Streuung zwischen den Häusern (0,82 bis 1,24) und als die
Kalibrierfaktoren des Validierungslaufs. Das ungewichtete Mittel beschreibt „ein typisches Hotel";
das gewichtete wäre „ein typisches Zimmer der Stichprobe" und ließe das große HO1 die Stufe mittel
prägen. Keiner der beiden Wege ist aus den Daten vorzuziehen.

**Bewertung: haltbar.** Keine Form verschiebt sich über die Formschwelle; der Bedarf nur um den
Faktor 0,94. Regel bleibt ungewichtet.

## (c) Flacher Jahresgang

**Abdeckung.** Die Rohdateien reichen **nicht** über das Konverterfenster hinaus: Jede Datei umfasst
nur einen Abschnitt eines einzigen Jahres, und das Fenster nimmt ihn nahezu ganz (Lückenanteil unter
0,1 %).

| Hotel | Jahr | volle Tage | volle Kalenderwochen | Monate (Tage je Monat) |
|---|---|---|---|---|
| HO1 | 2018 | 42 | 4 (KW 13–16) | März 18, April 24 |
| HO2 | 2018 | 45 | 6 (KW 35–40) | August 8, September 30, Oktober 7 |
| HO4 | 2019 | 138 | 19 (KW 14–32) | März 1, April 30, Mai 31, Juni 30, Juli 31, August 15 |
| HO3 | 2018 | – | – | Rohdatei 45 Tage, aber Lücken; längstes Fenster 20 Tage < 30, deshalb nicht verwendet |

Von November bis Februar gibt es **keinen einzigen Messtag**; Monate mit mindestens zwei vollen
Wochen gibt es je Hotel zwei (HO1), einen (HO2) und fünf (HO4).

**Monatsfaktoren je Hotel** (Monatsmittel / Fenstermittel, nur Monate mit ≥ 14 Tagen): HO1 März 0,97,
April 1,02; HO2 September 1,03; HO4 April 0,85, Mai 0,96, Juni 1,00, Juli 1,12, August 1,12. Nur HO4
zeigt einen Gang (Juli/April 1,32, Sommertourismus und Osterwoche im April), und das an einem Haus,
in einem Jahr, über fünf Monate. Über die Häuser lässt sich kein Monat zweimal belegen, und die
Monatsunterschiede innerhalb eines Hauses liegen in derselben Größe wie seine Wochenstreuung.

**Bewertung: haltbar als Modellannahme, offen in der Sache.** Die Datenbasis **trägt keinen
Jahresgang** — weder schätzbar noch widerlegbar. Monatsfaktoren 1 bleiben. Wer den Saisongang eines
Hauses kennt (Auslastung je Monat), setzt ihn als Auslastungsgang der Zone; der überschreibt die
Katalogmonate je Monat (`Formvektor.Auslastungsgang`).

## (d) Wochenende, Kalender „Betrieb", Bezug Bett = Zimmer

**Wochenende zu Werktag** (Wochenanteil / Mittel Montag bis Freitag):

| | HO1 | HO2 | HO4 | Mittel (Katalog) |
|---|---|---|---|---|
| Samstag / Werktag | 1,16 | 0,96 | 1,11 | 1,07 |
| Sonntag / Werktag | 0,91 | 0,63 | 0,97 | 0,83 |

Auffällig ist der **Montag** (Katalog: Montag / Mittel Dienstag bis Freitag = 0,73): Die Morgenspitze
eines Tages gehört den Gästen der Nacht davor, und die Nacht Sonntag auf Montag ist in Stadthotels die
schwächste. HO2 zeigt das Muster eines Geschäftsreisenden-Hotels am deutlichsten (Sonntag und
Montag je rund zwei Drittel eines Werktags).

**Kalender „Betrieb" im Generator.** Die Kalenderart unterscheidet im Rechenweg nur Wohnen und
Nichtwohnen (Gruppe, Vorgabesatz der Zapfkategorien, keine Urlaubsentkopplung). Der Tagtyp folgt für
alle Kalenderarten derselben Regel (`Zapfkalender.Bilden`): Werktag, Samstag, Sonn-/Feiertag aus den
Kennzeichen der Klimaregion, **Ruhetag nur in einem Ferienfenster der Zone**. Das Tagesgewicht ist am
Werktag der eigene Wochenanteil (Montag bis Freitag getrennt), am Samstag der Samstagsanteil, an Sonn-
und Feiertagen der Sonntagsanteil (`Formvektor.Tagesgewicht`). Die sieben Wochenanteile tragen das
Hotelmuster damit vollständig, den schwachen Montag eingeschlossen; Ruhetage gibt es ohne
Ferienfenster keine — für ein ganzjährig geöffnetes Hotel richtig.

Zwei Hinweise, keine Anpassung:
- *Feiertage in den Wochenanteilen:* Die Regel mittelt je Wochentag einschließlich der Feiertage (HO1
  vier, HO2 keiner, HO4 neun Feiertage im Fenster), der Generator gibt einem Feiertag dagegen den
  Sonntagsanteil. Ohne Feiertage gebildet weichen die Wochenanteile um im Mittel 0,0027 (größte 0,004)
  ab — unter der Formschwelle. Der Kern (`Messkalibrierung.Nichtwohnparameter`) rechnet gleich wie die
  Regel; beide bleiben deckungsgleich.
- *Ferienfenster:* Die Zeile hat keinen Ferienfaktor, ein Ruhetag erhält also den Sonntagsanteil
  (0,83 eines Werktags). Eine Saisonschließung bildet ein Ferienfenster deshalb **nicht** ab; dafür
  ist der Auslastungsgang der Zone (Monat = 0) der Weg.

**Bett = Zimmer.** Der Katalog kennt keine Bezugsart „Zimmer"; genommen ist Betten (3) mit der
Zimmerzahl der Quelle. Der Wert ist **je Zimmer** gebildet. Gibt ein Anwender die Bettenzahl eines
Hauses mit Doppelzimmern ein (üblich 1,5 bis 2 Betten je Zimmer, **Literatur**, Größenordnung),
überschätzt er den Bedarf um eben diesen Faktor 1,5 bis 2 — mehr als die ganze Stufenspreizung.

**Bewertung:** Kalender Betrieb und Wochenanteile **haltbar**. Bezug Bett = Zimmer **haltbar als
Wert, anpassen im Wortlaut**: Bezugsmenge ist die **Zimmerzahl** des ganzen Hauses. In der Regel
(Kopf und Modellannahme der JSON-Datei) geschärft; Bezeichnung oder Hilfetext der Katalogzeile sollen
es ebenso sagen (Folge 2).

## (e) Die drei Stufen

**Spreizung.** Gerundet niedrig / mittel = 0,82 und hoch / mittel = 1,26 (ungerundet 0,82 / 1,24).
Die übrigen Zeilen des freien Paketteils:

| Nutzungsart | niedrig / mittel | hoch / mittel |
|---|---|---|
| Wohnen groß (abgeleitet) | 0,76 | 1,14 |
| Ein- und Zweifamilienhaus (abgeleitet) | 0,85 | 1,15 |
| Studentenwohnheim (abgeleitet) | 0,92 | 1,21 |
| Seniorenheim (abgeleitet) | 0,94 | 1,37 |
| Krankenhaus (abgeleitet) | 0,92 | 1,46 |
| **Hotel (aus Messung)** | **0,82** | **1,26** |

Die Hotelspreizung liegt im Band der übrigen Typen (niedrig 0,76 bis 0,94, hoch 1,14 bis 1,46).

**Zuordnung.** Die Regel rechnet richtig: niedrig = kleinster, hoch = größter **Bedarf je Zimmer**
(`min`/`max` der drei Tagesbedarfe). Der Wortlaut der Prüfliste und der Modellannahme („kleinstes
Hotel / größtes Hotel", „das kleinste und das größte der drei Hotels") liest sich aber als
Hotelgröße — und das ist falsch: Nach Zimmerzahl ist die Reihenfolge HO4 < HO2 < HO1, nach Bedarf je
Zimmer HO2 < HO1 < HO4. Das **kleinste** Haus trägt die Stufe **hoch**, das mittelgroße die Stufe
niedrig, das größte liegt nahe dem Mittel. Eine Größenabhängigkeit lässt sich an drei Häusern nicht
ablesen (Rangkorrelation −0,5); HO4 ist zudem das einzige im Frühjahr und Sommer gemessene Haus (sein
Juli liegt bei 1,12 seines Mittels), ein Teil seines Vorsprungs kann Saison sein.

**Bewertung: Werte haltbar, Wortlaut angepasst.**

---

## Änderung an der Regel

`Referenzlaeufe/Skripte/hotel_aus_messung_bauen.py`: Kopf Schritt 4 („niedrig = kleinster, hoch =
größter Tagesbedarf je Zimmer … nicht das kleinste oder größte Hotel"), Modellannahmen im Kopf
(Bezugsmenge = Zimmerzahl; Reihen tragen keinen Jahresgang), Absatz „Durchsicht 26.09.2026" mit
Verweis auf dieses Protokoll, und zwei Einträge der Liste `modellannahmen`. Skript neu gelaufen: In
`tww_hotel_aus_messung.json` ändern sich **nur** die zwei Texte der Modellannahmen
(„ein Zimmer gilt als ein Bett: Bezugsmenge ist die Zimmerzahl, nicht die Bettenzahl";
„niedrig und hoch sind der kleinste und der größte Tagesbedarf je Zimmer der drei Hotels");
Bedarf, Wochen- und Stundenanteile unverändert. `tww_testkatalog_fiktiv.py` liest die
Modellannahmen nicht — Paketteil und Testdatenbank ändern sich dadurch nicht.

## Ergebnis je Annahme

| Annahme | Befund (Verhältnisse) | Ergebnis |
|---|---|---|
| Bedarf 3,2 / 3,9 / 4,9 kWh je Zimmer und Tag | Häuser 0,82 / 0,94 / 1,24 des Mittels; Wochen 0,58–1,21 des eigenen Mittels (Osterwochen tief); Katalogwert in der Spanne der Literatur | **haltbar** (Vorbehalt: eine Region, Belegung unbekannt) |
| Ungewichtetes Mittel der Formen | zimmergewichtet mittl. \|Δ\| ≤ 0,0023, Spitze × 1,07; Woche 0,0019 | **haltbar** |
| Ungewichtetes Mittel des Bedarfs | zimmergewichtet × 0,94 (3,7 statt 3,9) | **haltbar** (Wahl „typisches Hotel") |
| Flacher Jahresgang | Rohdaten = Fenster; kein Messtag November–Februar; Monatsfaktoren nur an HO4 schätzbar (0,85–1,12) | **haltbar als Modellannahme**; Jahresgang **offen** — Datenbasis trägt keinen |
| Kalender Betrieb, Wochenanteile | Sa/WT 1,07, So/WT 0,83, Mo/Di–Fr 0,73; Feiertagsbehandlung wirkt < 0,004 | **haltbar** |
| Bett = Zimmer | Wert je Zimmer; Bettenzahl statt Zimmerzahl überschätzt um 1,5–2 | **Wert haltbar, Wortlaut angepasst** (Regel); Katalogzeile/Hilfe nachziehen |
| Stufen niedrig/hoch | Spreizung 0,82 / 1,26 im Band der übrigen Typen; Stufe folgt dem Bedarf, nicht der Größe (kleinstes Haus = hoch) | **Werte haltbar, Wortlaut angepasst** |

## Folgen

1. **Prüfliste ZU21**, Zeile „Hotel (aus Messung): Kennwerte": Wortlaut „kleinstes Hotel / größtes
   Hotel" ersetzen durch „kleinster / größter Bedarf je Zimmer der drei Hotels"; Stand „Durchsicht
   ausgeführt 26.09.2026 (#561)". Auftraggeber.
2. **Katalogzeile und Hilfe:** Bezeichnung oder Hilfetext soll sagen, dass die Bezugsmenge die
   **Zimmerzahl** ist (z. B. „Hotel (aus Messung, je Zimmer)" oder ein Satz auf der Wiki-Seite des
   Katalogs); eine Umbenennung der Zeile ist eine Paketänderung für den Auftraggeber.
3. **Jahresgang:** bleibt flach, bis eigene Hotelmessungen über ein Jahr vorliegen (K5); bis dahin
   ist der Auslastungsgang der Zone der Weg für Saisonhäuser.
4. **Kein Einfrierfall:** Bedarf, Wochen- und Stundenanteile sind unverändert; die Basis bleibt.
