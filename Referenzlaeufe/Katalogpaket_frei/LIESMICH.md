# Freier Paketteil des Zapfprofilgenerators

Die Katalogdaten des Zapfprofilgenerators, die im Repositorium stehen dürfen (Umsetzungskonzept
Zapfprofilgenerator, Kapitel 6, Zeile „frei"; Anwenderentscheid ZU20 für die abgeleiteten
VDI-6002-Nutzungsarten) und ohne die eine Auslieferung weder stochastisch rechnet noch die
Bedarfstag-Quelle (5) anbietet, weder Liter noch Stunden über der Schwelle anzeigt, weder eine
große Zirkulation nennt noch einen Katalog der Nutzungsarten führt. Sie stehen **einmal** hier:

- [`Werkzeuge/Auslieferungsvorlage`](../../Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs) spielt den
  Ordner in **jede** Vorlage ein (`PaketteilEinspielen`), nach dem externen `--katalogpaket`;
- [`Skripte/tww_testkatalog_fiktiv.py`](../Skripte/tww_testkatalog_fiktiv.py) schreibt dieselben
  Zeilen in die Testdatenbank; die drei Träger der abgeleiteten Werte (`Tab_TwwTagesgangsatz_STAMM.csv`,
  `Tab_TwwTagesgang_STAMM.csv`, `Tab_TwwNutzungsart_STAMM.csv`) **erzeugt** es dabei aus
  [`Skripte/tww_katalogwerte_abgeleitet.json`](../Skripte/tww_katalogwerte_abgeleitet.json)
  (Schalter `--paketteil-schreiben`) und hält sie bei jedem Lauf dagegen — von Hand wird in
  diesen drei Dateien nichts geändert;
- [`Skripte/ecodesign_profile_bauen.py`](../Skripte/ecodesign_profile_bauen.py) **erzeugt**
  `Tab_TwwBedarfstag_STAMM.csv` und `Tab_TwwBedarfstagEreignis_STAMM.csv` aus der Rohtabelle
  [`Skripte/ecodesign_profile_814_2013.json`](../Skripte/ecodesign_profile_814_2013.json) —
  von Hand wird in diesen zwei Dateien nichts geändert;
- dasselbe Skript `tww_testkatalog_fiktiv.py --paketteil-schreiben` **erzeugt** die Zeilen
  `Speicherauslegung.*` am Ende von `Tab_TwwParameter_STAMM.csv` aus
  [`Skripte/speicherauslegung_v4.json`](../Skripte/speicherauslegung_v4.json) (Werte der
  INEKON-Vorlage TWW-Auslegung V4 samt Fundstelle) und dahinter die zwei Hinweisschwellen
  `Zapfprofil.Messwert.Rueckfrageschwelle` und `Zapfprofil.Formvektor.Warnschwelle` aus
  [`Skripte/zapfprofil_setzungen_inekon.json`](../Skripte/zapfprofil_setzungen_inekon.json) —
  von Hand wird an diesen Zeilen nichts geändert, die übrigen Zeilen der Datei bleiben Handpflege;
- dasselbe Skript nimmt in die drei Träger der abgeleiteten Werte die Nutzungsart
  „Hotel (aus Messung)" samt Tagesgangsatz auf, aus
  [`Skripte/tww_hotel_aus_messung.json`](../Skripte/tww_hotel_aus_messung.json) (gerundete
  Kennwerte dreier gemessener Hotels, gebildet von
  [`Skripte/hotel_aus_messung_bauen.py`](../Skripte/hotel_aus_messung_bauen.py));
- die Wache `EPOS.Kern.Tests/TwwKatalogWacheTests.Die_freien_Zeilen_der_Testdatenbank_gleichen_dem_Paketteil`
  hält Testdatenbank und Dateien gleich.

## Format

Paketformat N2 des Umsetzungskonzepts (Kapitel 6 (b)): je Tabelle `<Tabelle>.csv`, UTF-8,
Kopfzeile mit den Spaltennamen der Tabelle, Trenner `;`, Zahlen mit Punkt, leeres Feld = NULL.
Dazu vier Regeln des Paketteils:

1. **Jede Zeile** trägt Status `AUSLIEFERUNG`, `ReadOnly` 1 und in jeder Provenienzgruppe eine
   Herkunftsart aus drei: `FREI` für eine frei verfügbare Quelle (EU-Recht, veröffentlichte
   Parametrik, eigene Setzung), `EIGENKONSTRUKTION` für eine Setzung von INEKON **aus einer
   eigenen, nicht veröffentlichten Unterlage oder Modellannahme** — die Setzungen der Speicherauslegung aus der Vorlage
   TWW-Auslegung V4 (N28), die zwei Hinweisschwellen (INEKON-Setzung) und die Nutzungsart „Hotel (aus
   Messung)" (Modellannahme aus dem Mittel dreier gemessener Hotels, ZU36) — und `VERFAHREN` für einen **aus einem Verfahren gerechneten** Wert
   — die aus VDI 6002 abgeleiteten Nutzungsarten samt Tagesgängen (ZU19/ZU20). `FREI` wäre für
   sie eine falsche Aussage: VDI 6002 ist keine frei verfügbare Quelle, und die Zahl der Zeile
   steht in keiner Richtlinie, sondern kommt aus der Ableitungsregel von
   [`Skripte/normzahlen_abgeleitet_bauen.py`](../Skripte/normzahlen_abgeleitet_bauen.py). Der
   Tagesgangsatz führt keine Herkunftsspalte — seine Herkunft steht an seinen Tagesgängen.
2. **Keine Katalogversion.** Die Zeilen treten der Katalogversion des Katalogs bei, in den sie
   kommen — in der Vorlage der des zuletzt angelegten Parameters (sonst `FREI-1`), in der
   Testdatenbank `TEST-1`. Sonst sähe der Parametersatz, der nur eine Katalogversion liest, die
   Parameter der Stochastik nicht. `Version` (Provenienz) nennt den Stand des Paketteils.
3. **Schlüssel des Pakets.** Die `ID` eines Bedarfstags verknüpft ihn mit seinen Ereignissen
   (`ID_Bedarfstag`), die `ID` eines Tagesgangsatzes mit seinen Tagesgängen und mit den
   Nutzungsarten, die ihn tragen (`ID_Tagesgangsatz`); die Datenbank vergibt die echte ID. Tritt
   ein Tagesgangsatz zurück, weil das externe Katalogpaket ihn führt, treten seine Tagesgänge und
   die Nutzungsarten des Paketteils, die auf ihn zeigen, benannt mit ihm zurück. Die Zapfkategorien führen **keine**
   `ID_Nutzungsart`: Sie sind Vorgabesätze, von denen jede Nutzungsart mit Status `AUSLIEFERUNG`
   ohne eigene Kategorien **den Satz ihrer Gruppe** bekommt (in der Testdatenbank jede Nutzungsart
   des Testkatalogs).
4. **Die Steuerspalte `Gruppe`** (nur `Tab_TwwZapfkategorie_STAMM.csv`) ist die **einzige** Spalte
   des Paketteils, die keine Spalte ihrer Tabelle ist; sie wird nie geschrieben. Sie trägt
   `Wohnen` oder `Nichtwohnen` und sagt, an welche Nutzungsarten der Satz bindet: Die
   **Kalenderart** entscheidet — `Wohnen` (1) heißt Wohnnutzung, jede andere (Arbeitstage,
   Schulferien, Betrieb, Auslastungsgang) Nichtwohnen (`TwwSchema.Kategoriengruppe`, dieselbe
   Regel in Auslieferungsvorlage, Einspielskript und Kern; die Bezugsart trennt nicht, weil
   Personen Wohn- wie Nichtwohnnutzungen tragen). Leer bleibt sie nur in einem älteren Paketteil
   ohne Gruppen — dann bindet der eine Satz an jede Nutzungsart. Fehlt der Satz einer Gruppe,
   bleiben ihre Nutzungsarten ohne Kategorien und rechnen nicht stochastisch; der Prüfbericht
   meldet das.

Führt das externe Katalogpaket dieselbe Zeile (Parameter: Schlüssel; Bedarfstag, Tagesgangsatz
und Nutzungsart: Bezeichner — jeweils in derselben Katalogversion) oder für eine Nutzungsart eigene
Kategorien, gilt das Katalogpaket; der Prüfbericht meldet die Schlüsselgleichheit.

## Inhalt und Quellen

| Datei | Zeilen | Inhalt | Quelle |
|---|---|---|---|
| `Tab_TwwBedarfstag_STAMM.csv` | 9 | die neun Ecodesign-Zapfprofile XXS bis 4XL, je ein Bedarfstag der Art 5, Bezugsart Wohneinheiten (2, ab Schemastand 124: das Lastprofil beschreibt einen Haushalt) mit Bezugsmenge: Profil L eine Wohneinheit, jedes andere Q_ref / Q_ref(L) Wohneinheiten (XXS, XS, S 0,18; M 0,5; XL 1,64; XXL 2,1; 3XL 4,01; 4XL 8,02) — die Auslegung skaliert sie linear mit den Wohneinheiten der Gruppe (siehe unten); Profil L führt die ID 1 (Anwenderentscheid „Abschnitt 1: Ecodesign — erweitere Profil", N26) | Verordnung (EU) Nr. 814/2013 der Kommission, Anhang III, Tabelle 1, Lastprofile XXS bis 4XL (ABl. L 239 vom 6.9.2013) — EU-Recht |
| `Tab_TwwBedarfstagEreignis_STAMM.csv` | 161 | die Zapfungen der neun Profile: Beginn, Dauer, Energie Q_tap; je Profil ist die Tagessumme = Q_ref | wie oben; die Dauer ist eine Setzung der Umsetzung (siehe unten) |
| `Tab_TwwParameter_STAMM.csv` | 37 | `Speicherauslegung.*` (22 Zeilen, siehe unten); die zwei Hinweisschwellen `Zapfprofil.Messwert.Rueckfrageschwelle` (0,5: Hinweis, wenn der Kalibrierfaktor eines Jahresmesswerts um mehr als diesen Anteil von 1 abweicht) und `Zapfprofil.Formvektor.Warnschwelle` (0,01: Hinweis, wenn die Summe eines Formvektors um mehr als diesen Betrag von 1 abweicht); Speichertemperatur, Nutzanteil, Zuschlag, Länge des Ladefensters, die drei Werte des klassischen Faustwerts, Raster und 14 Stufen der Nenninhaltsliste; `Zapfprofil.Stochastik.*`: Urlaubsversatz, Vielfaches der Mindestzahl, Konsistenzschwelle, Quantile P95 und P99; `Zapfprofil.Zirkulation.Hinweisverhaeltnis` (Hinweis, wenn die Zirkulation mehr als das 1,5-Fache der Zapfung verliert); `Zapfprofil.Anzeigetemperatur` (45 °C, Literanzeige) und `Zapfprofil.Stundenschwelle` (0,1 kW, Stunden über der Schwelle) — die Vorgaben der Anzeige, wenn weder Dialog noch Einstellung eine nennen; `Zapfprofil.Validierung.*`: die fünf Setzungen der Validierung gegen eine Messreihe — Bandgrenzen der synthetischen Spitze (0,85 und 0,95), Formschwelle des Tagesgangs (0,01), höchster Lückenanteil einer Messreihe (0,05) und kürzeste Reihe für einen Kalibriervorschlag (30 d) | Speicherauslegung: INEKON-Vorlage TWW-Auslegung V4 (Version 2.1.2), Herkunftsart `EIGENKONSTRUKTION`, Fundstelle je Zeile in `Quelle`; die zwei Hinweisschwellen: INEKON-Setzung (Konzept 2.2 bzw. 2.4), Herkunftsart `EIGENKONSTRUKTION`; Quantile: Standardnormalverteilung; die übrigen: Setzungen des Zapfprofilgenerators (Umsetzungskonzept 4.4, 4.0, 4.6 und Warnlogik der Stufe Z4; Konsistenzschwelle nach der Warnlogik des Konzepts TWW-Zapfprofile) |
| `Tab_TwwTagesgangsatz_STAMM.csv` | 5 | die Tagesgangsätze der abgeleiteten Nutzungsarten (Wohnen groß, Studentenwohnheim, Seniorenheim, Krankenhaus) und des Hotels; das Ein- und Zweifamilienhaus teilt den Satz des großen Wohngebäudes | abgeleitet aus VDI 6002 Blatt 1 und 2 (Ausgabe 2014-03) nach der Regel von [`Skripte/normzahlen_abgeleitet_bauen.py`](../Skripte/normzahlen_abgeleitet_bauen.py) — **kein Wert der Richtlinie** (ZU19); Hotel: siehe unten |
| `Tab_TwwTagesgang_STAMM.csv` | 20 | je Satz vier Tagesgänge (Werktag, Samstag, Sonntag, Ruhetag = Sonntag; beim Krankenhaus derselbe Gang für jeden Tagtyp), je 24 Stundenanteile mit Summe 1 | wie oben; Herkunftsart `VERFAHREN`, beim Hotel `EIGENKONSTRUKTION` |
| `Tab_TwwNutzungsart_STAMM.csv` | 6 | die fünf abgeleiteten Nutzungsarten „… (abgeleitet)": Wohnen groß, Ein- und Zweifamilienhaus, Studentenwohnheim (Bezugsart Person, Kalender Wohnen), Seniorenheim, Krankenhaus (Bezugsart Bett, Kalender Auslastungsgang) — Bedarf niedrig/mittel/hoch in kWh je Einheit und Tag, Monatsfaktoren (Mittel 1), Wochenanteile (Summe 1), Verweis auf den Tagesgangsatz; dazu „Hotel (aus Messung)" (Bezugsart Bett = Zimmer, Kalender Betrieb, Gruppe Nichtwohnen) | wie oben; zwei Setzungen für das Ein- und Zweifamilienhaus (geliehene Formen des großen Wohngebäudes, mittlerer Bedarf = Mitte der Spanne); Hotel: Mittel aus drei Hotels, Sørensen et al. 2021, doi:10.1016/j.dib.2021.107228 (CC BY 4.0), Herkunftsart `EIGENKONSTRUKTION` |
| `Tab_TwwZapfkategorie_STAMM.csv` | 6 | **zwei Vorgabesätze** (Steuerspalte `Gruppe`): Wohnen mit vier Kategorien (Kurzzapfung, mittlere Zapfung, Wannenbad, Dusche), Nichtwohnen mit zwei (Kurzzapfung, Duschzapfung) — je Kategorie mittlerer Volumenstrom, Dauer, Anteil, Streuung, Kappung | Wohnen: Jordan/Vajen, IEA SHC Task 26 — die Parametrik des Einfamilienhauses, wie sie das Protokoll der DHWcalc-Referenzdatei [im Testordner](../../EPOS.Kern.Tests/Proben/Zapfprofil/OpenDHW/LIESMICH.md) ausweist; Nichtwohnen: **Modellannahme** nach dem OpenDHW-Muster (zwei Kategorien statt vier) — beide **Modellannahme** |

**Die Setzungen der Speicherauslegung (N28).** Die Werte stehen in der INEKON-eigenen Vorlage
TWW-Auslegung V4 (Blätter Eingaben, Berechnung, Ergebnis); sie sind weder Norm- noch Produktwerte.
Speichertemperatur 60 °C, Nutzanteil 0,80, Zuschlag 0,15, Ladefenster 8 h, klassischer Faustwert
35 l/(P·d) bei 50 K Bezugsspreizung mit Warnfaktor 3, Nenninhalte 100, 150, 200, 300, 400, 500,
800, 1 000, 1 500, 2 000, 3 000, 5 000, 8 000 und 10 000 l, darüber das Raster 1 000 l. Die
Fundstelle steht in `Quelle` in Worten (Zeile, Spalte), die Zelladresse in der JSON-Datei. Nicht
ausgeliefert werden `Speicherauslegung.Ladefenster.Beginn` und
`Speicherauslegung.GLF_Gueltigkeitsgrenze`: V4 führt dafür keinen Wert (Kopf „offen" der
JSON-Datei). Nennen weder Projekt noch Katalogpaket den Beginn, lehnt die Speicherauslegung benannt
ab (`PARAMETER_SCHLUESSEL_FEHLT`); ohne Grenze entfällt der Gültigkeitshinweis des GLF-Verfahrens.

**Die abgeleiteten VDI-6002-Werte (ZU19, ZU20).** Die drei Träger werden **erzeugt**, nicht
getippt: `Skripte/tww_testkatalog_fiktiv.py --paketteil-schreiben` schreibt sie aus
`Skripte/tww_katalogwerte_abgeleitet.json`, und jeder weitere Lauf des Skripts bricht ab, wenn eine
der drei Dateien vom Erzeugnis abweicht. Wer an der JSON-Datei oder an der Auswahl der
Nutzungsarten etwas ändert, lässt das Skript mit dem Schalter laufen und committet Dateien und
Testdatenbank im selben Schritt. Kein Wert dieser Dateien ist ein Originalwert der Richtlinie; die
Ableitungsregel steht im Kopf von `Skripte/normzahlen_abgeleitet_bauen.py`.

**Die neun Ecodesign-Zapfprofile (Anwenderentscheid „Abschnitt 1: Ecodesign — erweitere Profil",
N26).** Der Paketteil führt alle Lastprofile der Tabelle 1 außer 3XS: XXS, XS, S, M, L, XL, XXL,
3XL, 4XL. `Skripte/ecodesign_profile_bauen.py` **erzeugt** die beiden Dateien aus der Rohtabelle
`Skripte/ecodesign_profile_814_2013.json` (Uhrzeit, Q_tap, Volumenstrom, Temperaturen je Zapfung
und Profil, dazu Q_ref der Verordnung zur Gegenprobe); jeder Lauf prüft die Summe jedes Profils
gegen sein Q_ref und dass Profil L (ID 1) byte-genau seinen bisherigen Bestand reproduziert. Wer
an der JSON-Rohtabelle etwas ändert, lässt das Skript mit dem Schalter `--schreiben` laufen und
committet beide Dateien im selben Schritt.

**Bezugsmenge der Ecodesign-Zapfprofile (Folgeposten #546).** Profil L beschreibt eine
Wohneinheit; jedes andere Profil beschreibt Q_ref / Q_ref(L) Wohneinheiten, kaufmännisch auf zwei
Nachkommastellen — `Skripte/ecodesign_profile_bauen.py` rechnet sie aus den Q_ref der Rohtabelle.
Die Auslegung skaliert den Tag mit `Ziel / Bezugsmenge`, Ziel = die Wohneinheiten der Gruppe (die
Bezugsmenge der Zonen in Wohneinheiten, sonst die Wohneinheiten ihrer Wohnungstabellen). Das ist
eine **Modellannahme**: lineare Skalierung ohne Gleichzeitigkeit. Über zehn Wohneinheiten nennt die
Auslegung das als Hinweis (`ECODESIGN_SKALIERT`, keine Sperre) — für große Zonen ist der
stochastische Bedarfstag maßgeblich.

**Die Nutzungsart „Hotel (aus Messung)" (ZU36).** VDI 6002 führt für Hotels weder Bedarfswerte
noch Profile; der Katalogtyp kommt deshalb aus Messungen: aus den drei Hotelreihen der offen
lizenzierten Datensammlung von Sørensen et al. (Mendeley Data V2, doi:10.17632/m3xy22pf4j.2;
Beschreibung Data in Brief 37 (2021) 107228, doi:10.1016/j.dib.2021.107228; CC BY 4.0), allein
der Kanal Zapfenergie. Je Hotel Tagesbedarf je Zimmer, Wochenanteile und Stundenanteile je Tagtyp
nach demselben Verfahren wie der Kalibriervorschlag des Kerns; über die drei Hotels das
ungewichtete Mittel, niedrig und hoch das kleinste und das größte Hotel. Gerundet: Bedarf auf
0,1 kWh je Zimmer und Tag (3,2 / 3,9 / 4,9), Anteile auf drei Stellen. Im Repositorium stehen nur
diese gerundeten Kenn- und Verhältniswerte, keine Messreihe und keine Messmenge eines Gebäudes; die
Regel steht im Kopf von `Skripte/hotel_aus_messung_bauen.py`. **Modellannahmen** (Herkunftsart
`EIGENKONSTRUKTION`): drei Hotels einer Region als Mittel, ein Zimmer gilt als ein Bett, flacher
Jahresgang (die Reihen umfassen sechs bis zwanzig Wochen), Bezugstemperaturen 60/12 °C wie die
abgeleiteten Zeilen.

**Dauer der Ecodesign-Zapfungen.** Die Tabelle der Verordnung nennt Energie, Volumenstrom und
Temperaturen, keine Dauer. Setzung: Dauer = Volumen / Volumenstrom, Volumen = Q_tap / (c_w ·
(Nutztemperatur − 10 °C)), Nutztemperatur = Spitzentemperatur, wo angegeben, sonst die
Mindesttemperatur; ganze Minuten kaufmännisch, mindestens 1. Die Energie jeder Zapfung ist Q_tap
unverändert.

**Zapfkategorien, Gruppe Wohnen.** Vier Kategorien ohne obere Kappung (`Kappung_l_min` leer); die
Streuung je Kategorie wie im DHWcalc-Protokoll.

**Zapfkategorien, Gruppe Nichtwohnen** (Konzept 4.4: „für Nichtwohnen zwei Kategorien nach dem
OpenDHW-Muster"). Das Muster ist die **Zahl und Art der Kategorien** — eine Kurzzapfung am
Waschtisch und eine Zapfung in Höhe einer Dusche —, nicht ein übernommener Zahlenwert. Volumenstrom,
Dauer, Anteil, Streuung und Kappung sind **freie Modellannahmen** von INEKON mit runden Werten:
Kurzzapfung 2 l/min über 1 Minute mit Anteil 0,6, Duschzapfung 8 l/min über 5 Minuten mit Anteil
0,4, Streuung je 1 l/min, Kappung 6 bzw. 20 l/min (was eine Armatur bzw. eine Brause höchstens
gibt). Die Anteile summieren wie im Wohnsatz auf 1; die Kappung begrenzt den gezogenen Volumenstrom
nach oben und geht in das doppelt gestutzte Mittel der λ-Kalibrierung ein. Gemessene Reihen ändern
daran nichts: Die Kalibrierung einer Nichtwohn-Zone (Stufe Z5) schreibt Tagesbedarf, Wochenfaktoren
und Tagesgänge in eine Anwenderkopie und nimmt die Kategorien unverändert mit.

**Setzungen zur Bestätigung (ZU21).** Die zwei Hinweisschwellen, die Bezugsmengen der
Ecodesign-Profile und die Modellannahmen des Hotels sind am 26.09.2026 umgesetzt (#546). Urlaubsversatz, Vielfaches, Konsistenzschwelle, das
Hinweisverhältnis der Zirkulation, die Anzeigetemperatur, die Stundenschwelle, die fünf
Setzungen der Validierung (`Zapfprofil.Validierung.*`) und die **fünf Werte je Kategorie des
Nichtwohnsatzes** samt der Gruppenregel (Kalenderart) sind Setzungen
von INEKON; der Anwender bestätigt oder ändert sie vor der ersten Auslieferung. Ohne
Hinweisverhältnis entfällt der Hinweis zur Zirkulation; ohne Anzeigetemperatur bzw. Stundenschwelle
entfällt die Literanzeige bzw. die Zählung, sofern weder der Dialog noch die Einstellung einen Wert
nennt.

Keine Normzahl, kein Hersteller- oder Produktwert.
