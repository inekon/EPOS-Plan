# ZapfprofilValidierung — der Rechennachweis gegen echte Messreihen

Werkzeug zur **Stufe Z5** des
[Umsetzungskonzepts Zapfprofilgenerator](../../Dokumentation/aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md)
und zu seinem offenen Punkt **K5**: Es hält die gerechnete Jahresreihe des
Zapfprofilgenerators gegen **gemessene** Reihen und gibt je Objekt eine Ampel nach den
Abnahmekriterien des Kapitels 7.

**Die Messreihen kommen nie ins Repositorium.** Sie liegen beim Anwender; hierher kommt nur
der Bericht, und der trägt **ausschließlich Verhältniszahlen, Anteile und Zählungen** — keine
gemessene Menge, keine gemessene Leistung, keinen Objektnamen außer einer anonymen Kennung.
Eine Wache im Werkzeug hält jeden Berichtstext dagegen und **schreibt nicht**, wenn sie etwas
findet.

---

## 1. Aufruf

```
dotnet run --project Werkzeuge/ZapfprofilValidierung -c Release -- <ordner> --ziel <berichtordner>
       [--katalog <sqlite|paketordner>] [--realisierungen N] [--seed S] [--trocken] [--beispielreihe]
```

| Angabe | Bedeutung |
|---|---|
| `<ordner>` | je Messobjekt ein Unterordner mit `objekt.json` und `messreihe.csv` |
| `--ziel` | Ordner der Berichte (je Objekt Markdown und CSV, dazu `Sammelbericht.md` und `Sammelbericht.csv`) |
| `--katalog` | Katalogquelle: eine `.sqlite`-Datei **oder** ein Paketordner (Format N2). Vorgabe: `Referenzlaeufe/Kenndaten_Test.sqlite`, aufwärts gesucht |
| `--realisierungen` | überschreibt die Zahl der Ensemble-Realisierungen jedes Objekts |
| `--seed` | überschreibt den Seed jedes Objekts |
| `--trocken` | nur lesen, rechnen und prüfen; nichts schreiben |
| `--beispielreihe` | erzeugt statt der Berichte die **synthetische** `messreihe.csv` jedes Objekts (siehe Abschnitt 6) |

Rückgabe: `0` alle Objekte grün, `2` Aufruf, `3` Schreibort, `5` mindestens ein Objekt nicht
abgenommen, `6` Fund der Berichtswache, `1` unerwartet.

Beispiel (das mitgelieferte, erfundene Beispiel):

```
dotnet run --project Werkzeuge/ZapfprofilValidierung -c Release -- \
    Werkzeuge/ZapfprofilValidierung/Beispiel \
    --katalog Werkzeuge/ZapfprofilValidierung/Beispiel/katalog --ziel <berichtordner>
```

---

## 2. Wohin die Messreihen gehören

Ins Repositorium **nie**. Vorgesehen sind zwei Orte außerhalb:

| Ort | Für wen |
|---|---|
| `Referenzlaeufe/Messreihen_INEKON/` | die eigenen, freigegebenen Reihen von INEKON. Der Ordner ist in `.gitignore` **bis auf sein `LIESMICH.md`** ausgenommen — genau wie `Referenzlaeufe/Normzahlen/`. Die Wache `EPOS.Kern.Tests/RepositoryOrdnungWacheTests` prüft die Regel und meldet jede Datei, die dort im Index landet |
| ein Ordner außerhalb des Repositoriums | offen lizenzierte Fremddaten samt ihren umgesetzten Reihen (siehe [`Konverter/LIESMICH.md`](Konverter/LIESMICH.md)) |

**Anonymisieren ist Sache des Anwenders.** Das Werkzeug kann eine Kennung nicht von einem Namen
unterscheiden; es prüft nur, dass keine gemessene **Zahl** und keine Einheit einer Menge in den
Bericht gerät.

---

## 3. Das Datenformat

### 3.1 `messreihe.csv` — das Format des `Messreihenleser`

* **Kopfzeile** mit Spaltennamen. Erkannt werden
  * ein Zeitstempel: `Zeitstempel`, `Zeitpunkt`, `Timestamp`, `DateTime`, `Datum_Zeit`, `Datum/Zeit`
  * **oder** Datum und Uhrzeit getrennt: `Datum`/`Date`/`Tag` und `Uhrzeit`/`Zeit`/`Time`
  * die Wertspalte: `Wert`, `Value`, `Menge`, `Verbrauch`, `Leistung`, `Energie`, `Volumen`
* **Einheit in Klammern** im Kopf der Wertspalte — `(kWh)`, `(m³)`/`(m3)`, `(kW)`; sie bestimmt die
  Messgröße, wenn `objekt.json` keine nennt. Eine unbekannte Einheit wird abgelehnt, nicht geraten.
* **Trenner** `;`, Tabulator oder `,` — der häufigste der Kopfzeile gewinnt. Dezimalkomma ist
  erlaubt, solange der Trenner nicht das Komma ist.
* **Zeitstempel** ISO (`2025-01-01T00:00`, auch mit Leerzeichen und Sekunden) oder deutsch
  (`01.01.2025 00:00`); Datum allein für eine Tagesreihe.
* **Raster** 1, 5, 10, 15, 60 oder 1440 Minuten — gemessen aus dem kleinsten Abstand zweier Zeilen.
  Für Band und Formabgleich braucht es **höchstens 1 h**; eine Tagesreihe trägt nur die Energie.
* **Lücken**: Jeder größere Abstand ist ein Vielfaches des Rasters, wird mit 0 gefüllt und gezählt.
  Über der Schwelle (Vorgabe **5 %**, Parameter `Zapfprofil.Validierung.Lueckenanteil`) lehnt der
  Leser die Datei ab.
* **Länge**: mindestens **30 Tage** (Parameter `Zapfprofil.Validierung.Kalibrierung.MindestTage`),
  empfohlen **ein Messjahr**. Ein Teiljahr wird benannt hochgerechnet (siehe 4.3).
* **Kein negativer Wert**, keine `NaN`, höchstens 600 000 Zeilen und 64 MiB.
* **UTF-8** (Vorspann erlaubt).

Zwei Beispielzeilen:

```
Zeitstempel;Wert (kWh)
2025-01-01T00:00;1.482
2025-01-01T01:00;0.935
```

### 3.2 `objekt.json` — was das Werkzeug über das Objekt wissen muss

Der vollständige, kommentierte Satz steht in
[`Beispiel/BSP-WOHNEN-01/objekt.json`](Beispiel/BSP-WOHNEN-01/objekt.json). Die Felder:

| Feld | Bedeutung |
|---|---|
| `kennung` | die **anonyme** Kennung; das Einzige, was in einen Bericht kommt |
| `nutzungsart` | Bezeichner der Nutzungsart im Katalog (natürlicher Schlüssel) |
| `bezugsmenge` | Bezugsmenge der Zone in der Bezugsart der Nutzungsart (Personen, WE, Betten …) |
| `bezugsmenge_herkunft` | `Veroeffentlichung` (belegt), `Abgeleitet` (aus einer belegten Größe mit benannter Annahme), `Platzhalter` oder `Unbekannt` (Niveau allein aus der Kalibrierung); leer = nicht angegeben. Platzhalter und unbekannte Mengen tragen die √N-Skalierung nicht |
| `niveau` | `Niedrig`, `Mittel` (Vorgabe), `Hoch` |
| `bilanzgrenze` | Bilanzgrenze des **Zählers**: `Zapfstelle`, `MitVerteilung`, `MitSpeicher` |
| `zirkulation` | rechnet die Zone eine Zirkulation? |
| `speicherverlust_kwh_je_jahr` | nur bei `MitSpeicher` |
| `zapftemperatur_c`, `kaltwasser_mittel_c`, `kaltwasser_amplitude_k` | Temperaturen der Zone; leer = Katalog und Parametersatz |
| `kalender.wochentag_jan1` | Wochentag des 1. Januar des Messjahrs, 0 = Montag … 6 = Sonntag |
| `kalender.feiertagsregion` | nur ein Vermerk; der Kern kennt keine Feiertagstabelle |
| `kalender.feiertage` | Feiertage des Messjahrs als Jahrestage 1 … 365: Sonntagsmenge und Sonntagsgang der Rechnung, und im Formabgleich zählt ein gemessener Tag auf einem Feiertag als Sonn-/Feiertag (`Messvergleichseingang.MessFeiertage`); ein Feiertag am Samstag bleibt Samstag |
| `kalender.ferien` | bis zu vier Fenster `{ "beginn": …, "ende": … }` (Ruhetage) |
| `messung.datei` | Dateiname der Reihe; leer = `messreihe.csv` |
| `messung.groesse` | `Energie`, `Volumen`, `Leistung`; leer = die Einheit der Kopfzeile entscheidet |
| `messung.zeitstempel` | `Ortszeit` (Vorgabe, mit Sommerzeit) oder `Normalzeit` (ohne Umstellung) |
| `messung.lueckenanteil_hoechstens` | leer = der Katalogparameter |
| `messung.quelle` | anonymer Vermerk (Zählerart, Zeitraum) |
| `stochastik.seed`, `stochastik.realisierungen`, `stochastik.jahresreihe_stochastisch` | die Stochastik (4.4); 0 oder 1 Realisierungen = kein Ensemble |
| `vermerk` | freier Vermerk für den Bericht — nichts Identifizierendes |

**Die Bilanzgrenze entscheidet, was verglichen wird**: Bei `Zapfstelle` steht die Zapfung der
Rechnung gegen den Zähler, bei `MitVerteilung` und `MitSpeicher` Zapfung **und** Zirkulation. Wer
einen Zähler hinter der Zirkulation hat, muss `MitVerteilung` setzen, sonst vergleicht das Werkzeug
Ungleiches.

---

## 4. Wie gerechnet wird

### 4.1 Datenbankfrei, ein Rechenweg

Gerechnet wird mit `ZapfprofilRechner.Rechnen` auf einem `Zapfprofileingang` mit **einer** Zone —
derselbe Weg, den das Programm nimmt. Verglichen wird mit `Messvergleich`, kalibriert mit
`Messkalibrierung`, gelesen mit `Messreihenleser`; alles Bestand des Kerns. Der **Katalog kommt als
Modell herein**: Nutzungsarten, Tagesgangsätze, Parametersatz und Zapfkategorien werden aus einem
Paketordner (CSV) oder aus einer `.sqlite`-Datei **gelesen** und zu den Modellen des Kerns gebaut.
Kein Controller, keine Projektkopie, keine Schreiboperation; eine `.sqlite`-Quelle wird
`immutable=1` und `ReadOnly` geöffnet und bleibt byte-gleich.

Der **freie Paketteil** `Referenzlaeufe/Katalogpaket_frei/` genügt allein **nicht**: Er führt nur
die Parameter, die im Repositorium stehen dürfen; die Kaltwasser-, Wohnen- und
Zirkulationsparameter des Mengengerüsts fehlen dort. Als Paketordner taugt nur einer, dessen
`Tab_TwwParameter_STAMM.csv` alle gebrauchten Schlüssel führt — das mitgelieferte Beispiel bringt
einen solchen, **erfundenen** Paketteil mit.

### 4.2 Erst kalibrieren, dann vergleichen

Die gerechnete Stundenleistung ist der Bezugsmenge proportional. Verglich man die **rohe** Reihe, so
verschöbe eine um 20 % falsch geschätzte Personenzahl das Spitzenverhältnis um 20 %; das Band der
Dauerlinie prüfte dann die Schätzung der Bezugsmenge und nicht die Gestalt der Reihe. Bei einem
Messobjekt ist die Bezugsmenge oft nur ungefähr bekannt — die Reihe ist es nicht. Deshalb:

1. **Kalibrieren** (Konzept 4.1): Der Jahreswert der Messreihe bringt das Mengengerüst auf ihr
   Niveau. Danach ist die Jahresenergie **exakt** der Nettomesswert.
2. **Vergleichen** gegen die kalibrierte Reihe: Band, Formabgleich und Monatsanteile sagen nun
   etwas über **Gleichzeitigkeit und Gestalt**. Das Niveau steht allein im **Kalibrierfaktor** —
   einer Verhältniszahl, die im Bericht steht und die verrät, wie weit die geschätzte Bezugsmenge
   neben der Messung liegt.

Das ist der eine benannte Unterschied zum Dialogweg (`ZapfprofilHuelle.Vergleichsbericht`), der die
rohe Reihe nimmt: Im Dialog ist die Bezugsmenge des Projekts gepflegt, hier ist sie eine Angabe über
ein fremdes Objekt.

### 4.3 Teiljahr

Eine Reihe unter einem Jahr wird zum Jahresmesswert **hochgerechnet** — über den Jahresgang der
Rechnung, wenn er da ist, sonst flach. Der Bericht nennt den abgedeckten Jahresanteil und trägt den
Hinweis `MESSKALIBRIERUNG_HOCHGERECHNET`. Ein Jahreswert aus einem Teiljahr ist nie ein gemessener
Jahreswert.

### 4.4 Stochastik

Ohne Ensemble (`realisierungen` 0 oder 1) rechnet die Jahresreihe deterministisch: Formvektor mal
Jahresmenge. Das Band der Dauerlinie ist dann eine schwache Aussage — die Lehre, an der es hängt
(Konzept 3.6), spricht von der **stochastischen** Reihe. Für eine Validierung deshalb
`realisierungen` ab 2 setzen (10 sind ein guter Wert); dann steht auch die **Spitzenstreuung** der
Realisierungen im Bericht.

---

## 5. Der Bericht

Je Objekt `<Kennung>.md` und `<Kennung>.csv`, dazu `Sammelbericht.md` und `Sammelbericht.csv`.

**Der Objektbericht** führt fünf Abschnitte: *Eingang* (Nutzungsart, Einheiten, Bilanzgrenze,
Rechenweg, Auflösung, Länge, Lückenanteil, Jahresanteil), *Abnahme* (die drei Kriterien je Objekt
mit Ampel, Maß, Schranke und Satz), *die fünf Kennzahlen*, *Form je Tagtyp*, *Monatsanteile*, bei
einer Nichtwohn-Nutzungsart den *Kalibriervorschlag* und am Ende die *Hinweise des Rechenwegs*
(jeder Satz des Kerns mit seiner Kennung).

**Der Sammelbericht** führt die Ampel je Objekt, das vierte Kriterium über alle Objekte, eine
Kennzahlentabelle, die Zählung der Ampeln und die Liste der nicht ausgewerteten Objekte.

### 5.1 Die Abnahmekriterien (Kapitel 7, Zeile Z5)

| Nr. | Kriterium | grün, wenn | Schranke |
|---|---|---|---|
| (b) | **Band der Dauerlinie** | die Messspitze liegt im P85–P95-Band der gerechneten Dauerlinie | Parameter `Zapfprofil.Validierung.Band.Unten`/`.Oben` |
| (d) | **Formabgleich Tagesgang** | die größte mittlere Stundenabweichung der Tagtypen hält die Schwelle | Parameter `Zapfprofil.Validierung.Formschwelle` |
| (4) | **Energie nach Kalibrierung** | die Jahresenergie ist nach der Kalibrierung der Nettomesswert | relativ 1e-9 |
| (c) | **√N-Skalierung** — **über alle Objekte mit belastbarer Bezugsmenge**, im Sammelbericht | die Steigung von ln(Spitzenverhältnis) über ln(N) liegt bei −0,5 | ± 0,25 (numerische Setzung des Werkzeugs) |

**Gelb** heißt „nicht entschieden": Die Kennzahl ist nicht bildbar (keine Stundenwerte, kein
vollständiger Messtag je Tagtyp, weniger als drei Objekte mit verschiedener Einheitenzahl). Gelb ist
nie „in Ordnung".

**Warum Platzhalter die √N-Skalierung nicht tragen:** Die Steigung hängt an N. Eine gesetzte runde
Zahl sagt nichts über die Größe des Objekts; Objekte mit `bezugsmenge_herkunft` `Platzhalter` oder
`Unbekannt` fallen deshalb aus der Ausgleichsgeraden, und der Satz des Kriteriums nennt ihre Zahl. Zum
Vergleich steht die Steigung über alle Objekte darunter (ohne Ampel).

**Analyse „Band je Größenklasse" (keine Ampel).** Der Sammelbericht führt neben dem Bandkriterium
zwei Zusatzmaße (`Bandanalyse.cs`): das **Perzentil der Messspitze** in der gerechneten Dauerlinie
(welches Quantil sie trifft; 1 = auf oder über der größten gerechneten Stunde) und die Lage der
Messspitze gegen die **Jahresspitzen der Realisierungen** (bezogen auf die verglichene Realisierung).
Verdichtet wird je Größenklasse (N < 10, 10 bis 99, ab 100). Die Maße liefern die Zahlen für einen
Entscheid über `Zapfprofil.Validierung.Band.*`; die Ampel bleibt das Konzeptkriterium.

**Warum die √N-Skalierung kein Kriterium je Objekt ist:** Sie ist eine Aussage über das Verhältnis
von Objekten **verschiedener Größe** — die Spitze je Einheit fällt mit der Zahl der Einheiten wie
1/√N (Konzept 4.4). An einem Objekt ist `Skalierungsmaß = Spitzenverhältnis · √N` nur eine Zahl; ein
Band um 1 hieße zu behaupten, die Rechnung überschätze die Spitze **jedes** Objekts um genau den
Faktor √N. Das sagt das Konzept nicht, und schon ein Mehrfamilienhaus mit 48 Personen könnte es
nicht erfüllen. Das Maß steht deshalb je Objekt im Bericht, geprüft wird die **Steigung** über alle.

### 5.2 Was nie im Bericht steht

* keine gemessene Menge, keine gemessene Leistung, kein Zählerstand;
* keine Einheit einer Menge oder Leistung (`kWh`, `MWh`, `m³`, `Btu`, `Liter`, `kW`, `l/min`) —
  ein Bericht aus Verhältniszahlen braucht sie nicht;
* kein Objektname, keine Anschrift, kein Kunde — nur die anonyme Kennung;
* auch der **Kalibriervorschlag** für eine Nichtwohn-Nutzungsart steht **als Verhältnis** da
  („Tagesbedarf je Einheit, Messung/Rechnung") und nicht als Betrag. Den Betrag erhält der Anwender
  daraus mit dem gerechneten Tagesbedarf je Einheit; er gehört in seine eigene Katalogkopie
  (Status „eigen"), nicht in ein Papier.

Die **Berichtswache** (`Berichtswache.cs`) prüft jeden Text vor dem Schreiben in drei Schichten:
die Einheitenregel, die Kennzahlen der Messreihen (jede Schreibweise mit mindestens sechs Ziffern)
und — als Bauform — dass der `Objektbefund` überhaupt keinen absoluten Messwert führt. Ein Fund
bricht den Lauf mit Rückgabe 6 ab; es entsteht **keine** Datei.

---

## 6. Das mitgelieferte Beispiel

`Beispiel/` trägt einen **erfundenen** Katalog (`katalog/`, runde Werte, Herkunftsart `FIKTIV`) und
zwei Objekte: `BSP-WOHNEN-01` (Personen, Kalender Wohnen, Bilanzgrenze `MitVerteilung`) und
`BSP-PFLEGE-01` (Betten, Kalender Arbeitstage — es bekommt den Kalibriervorschlag).

Die Messreihen sind **synthetisch und aus der Rechnung selbst erzeugt**: die deterministische
Jahresreihe, mal einem festen Rauschen (± 15 %, feste Saat), mit **unveränderter Jahresenergie** und
mit einer auf die **Bandmitte gekappten** Spitze; die abgeschnittene Energie wird auf die übrigen
Stunden verteilt. Beide Objekte sind damit **grün durch Konstruktion** — das Beispiel prüft den Weg
des Werkzeugs, nicht den Rechenweg des Generators. Wiederherstellen:

```
dotnet run --project Werkzeuge/ZapfprofilValidierung -c Release -- \
    Werkzeuge/ZapfprofilValidierung/Beispiel \
    --katalog Werkzeuge/ZapfprofilValidierung/Beispiel/katalog --beispielreihe
```

Das vierte Kriterium bleibt im Beispiel **gelb**: Es braucht drei Objekte mit verschiedener
Einheitenzahl, und eine Reihe, die aus der Rechnung stammt, könnte eine √N-Abhängigkeit ohnehin
nicht zeigen. Dafür braucht es echte Messungen.

---

## 7. Bauen und prüfen

```
dotnet build Werkzeuge/ZapfprofilValidierung/ZapfprofilValidierung.sln -c Release
dotnet test  Werkzeuge/ZapfprofilValidierung/ZapfprofilValidierung.sln -c Release --no-build
```

Eigene Projektmappe wie bei den Nachbarn (`Auslieferungsvorlage`, `Gebaeudevergleich`,
`Formularkarte`); das Werkzeug steht **nicht** in `WP-Plan.sln` und nicht im Kern-Filter. Seine
Tests laufen in `kern.yml` als eigener Schritt. Der Fall gegen
`Referenzlaeufe/Messreihen_INEKON/` **schweigt**, solange der Ordner keine Objekte führt — in der CI
also immer.
