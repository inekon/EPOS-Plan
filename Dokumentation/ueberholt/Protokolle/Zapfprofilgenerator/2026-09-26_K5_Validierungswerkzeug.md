# K5 — Validierungswerkzeug und erster Lauf an offenen Messreihen (26.09.2026)

Protokoll des Postens **#523**. Auftrag: das Validierungswerkzeug der Stufe Z5 bauen — den Vergleich
der Rechnung mit echten Messreihen, die **nie ins Repositorium** kommen — und es auf offen
lizenzierten Fremddaten laufen lassen. Zweig `zv` von `4fdda0c2`, ein Agent (Opus 5 mit 1M-Kontext),
Worktree `.claude/worktrees/zv`. Kein Schemaschritt, Testdatenbank unberührt.

Das Ergebnis des Laufs steht als eigenes Papier in
[`aktuell/Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md`](../../../aktuell/Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md);
die Festlegungen stehen als **Nachtrag N22** im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md). Dieses
Protokoll führt den Weg, die Gegenprüfungen und die Abweichungen.

---

## 1. Das Werkzeug

`Werkzeuge/ZapfprofilValidierung` — ein Konsolenprojekt `net10.0` mit eigener Projektmappe, die auch
das Testprojekt führt (Muster `Auslieferungsvorlage`, `Gebaeudevergleich`, `Formularkarte`). **Nicht**
in `WP-Plan.sln`, **nicht** im Kern-Filter; die Nachbarwerkzeuge stehen dort ebenso nicht, und der
Auftrag „in `WP-Plan.sln` aufnehmen wie `Auslieferungsvorlage`" ist damit erfüllt, indem es genau wie
jene außen bleibt (Abweichung A1 unten).

Dreizehn Dateien: `Program.cs` (nur Kodierung und Auffangnetz), `Einstieg.cs` (der Ablauf, den auch
die Proben fahren), `Argumente.cs`, `Objektbeschreibung.cs` (`objekt.json`), `Katalogquelle.cs` und
`Katalogbau.cs` (Paketordner und SQLite zu den Modellen des Kerns), `Sqlitehilfe.cs` (fünf lesende
`SELECT` auf einer `immutable=1`-Verbindung), `Objektlauf.cs` (Rechnung, Kalibrierung, Vergleich,
Vorschlag), `Objektbefund.cs` (die drei Kriterien je Objekt), `Sammelkriterium.cs` (das vierte),
`Bericht.cs`, `Berichtswache.cs`, `Beispielreihe.cs`.

**Datenbankfrei im Rechenweg.** Gerechnet wird mit `ZapfprofilRechner.Rechnen` auf einem
`Zapfprofileingang` mit **einer** Zone — derselbe Weg, den das Programm nimmt. Der Katalog kommt als
**Modell** herein: `Nutzungsart`, `Tagesgangsatz`, `Parametersatz`, `Zapfkategorie`, gebaut aus einem
Paketordner im Format N2 **oder** aus einer `.sqlite`. Kein Controller, keine Projektkopie, kein
`DataRepository`, kein Schreibzugriff; eine SQLite-Quelle wird `immutable=1` und `ReadOnly` geöffnet
und bleibt byte-gleich (geprüft: keine `-wal`- und `-shm`-Beidatei). Der Auftrag hatte die
Projektkopie als Rückfall vorgesehen — sie war nicht nötig.

Der **freie Paketteil** `Referenzlaeufe/Katalogpaket_frei/` genügt als Katalogquelle allein nicht: Er
führt nur die Parameter, die im Repositorium stehen dürfen; Kaltwasser-, Wohnen- und
Zirkulationsparameter des Mengengerüsts fehlen. Deshalb bringt das Beispiel einen vollständigen,
**erfundenen** Paketteil mit, und die Vorgabe der Katalogquelle ist die Testdatenbank.

---

## 2. Zwei benannte Unterschiede zum Dialogweg

**(1) Verglichen wird gegen die kalibrierte Reihe.** `ZapfprofilHuelle.Vergleichsbericht` hält die
Messung gegen die **rohe** Rechnung. Das ist im Dialog richtig — dort ist die Bezugsmenge des Projekts
gepflegt. Bei einem Messobjekt ist sie eine Schätzung, und die gerechnete Stundenleistung ist ihr
proportional: Eine um 20 % falsche Personenzahl verschöbe das Spitzenverhältnis um 20 %, und das Band
der Dauerlinie prüfte die Schätzung statt der Gestalt der Reihe. Das Werkzeug kalibriert deshalb erst
(Konzept 4.1) und vergleicht dann; der Niveaufehler steht allein im **Kalibrierfaktor**, einer
Verhältniszahl im Bericht. Der erste Lauf hat das bestätigt: Die Faktoren reichen von 0,16 bis 5,1 —
gegen die rohe Reihe wäre jede Aussage über Band und Form von diesen Faktoren verdeckt worden.

**(2) Die verglichene Reihe folgt der Bilanzgrenze des Zählers.** Die Hülle summiert stets Zapfung und
Zirkulation. Hier sagt die `objekt.json` die Grenze: an der Zapfstelle allein die Zapfung, mit
Verteilung oder Speicher beide Teile. Dieselbe Grenze rechnet die Kalibrierung ohnehin
(`Messkalibrierung.Jahresmesswert`); beide Seiten gleich zu halten ist die einzige Lesart, in der das
Energieverhältnis eine Aussage ist. Sichtbar wurde das am Beispielobjekt `BSP-PFLEGE-01`: Mit der
Summenbildung der Hülle und der Grenze „Zapfstelle" blieb ein Kalibrierfaktor von 1,25 stehen.

---

## 3. Die Abnahmekriterien als Ampel

| Nr. | Kriterium | Ort | Schranke |
|---|---|---|---|
| (b) | Band der Dauerlinie | je Objekt | Parameter `Zapfprofil.Validierung.Band.Unten`/`.Oben` |
| (d) | Formabgleich Tagesgang | je Objekt | Parameter `Zapfprofil.Validierung.Formschwelle` |
| (4) | Energie nach Kalibrierung | je Objekt | relativ 1e-9 |
| (c) | √N-Skalierung | **über alle Objekte**, im Sammelbericht | Steigung −0,5 ± 0,25 |

**Warum (c) kein Kriterium je Objekt ist** — die einzige fachliche Umdeutung des Auftrags, der vier
Ampeln je Objekt vorsah: `Skalierungsmass = Spitzenverhaeltnis · √N` ist an **einem** Objekt nur eine
Zahl. Ein Band um 1 hieße zu behaupten, die Rechnung überschätze die Spitze jedes Objekts um genau den
Faktor √N; das sagt das Konzept nicht (Kapitel 7 nennt „√N-Skalierung der Überschätzung", also eine
Aussage über Objekte verschiedener Größe), und schon ein Mehrfamilienhaus mit 48 Personen könnte es
nicht erfüllen. Geprüft wird deshalb die **Steigung** von ln(Spitzenverhältnis) über ln(N) über alle
Objekte; das Maß je Objekt steht im Bericht. Das Band ± 0,25 ist eine numerische Setzung des
Werkzeugs — die halbe Strecke zwischen „kein Zusammenhang" (0) und „doppelt so steil" (−1) — und
steht in jedem Bericht, damit niemand sie für eine Norm nimmt. Unter drei Objekten mit verschiedenem N
bleibt das Kriterium **gelb**.

**Gelb heißt „nicht entschieden"**, nie „in Ordnung": keine Stundenwerte, kein vollständiger Messtag
je Tagtyp, zu wenige Objekte.

---

## 4. Die Berichtswache

Der Bericht ist die eine Stelle, an der Messdaten das Werkzeug verlassen, und er wird ins
Repositorium gelegt. Drei Schichten, jede mit Gegenprobe:

1. **Einheiten einer Menge oder Leistung** (`kWh`, `MWh`, `m³`, `Btu`, `Liter`, `kW`, `l/min`, `l/d`)
   stehen nicht im Bericht — ein Bericht aus Verhältniszahlen braucht sie nicht.
2. **Die Kennzahlen jeder Messreihe** (Jahresmenge, Energie, größter Wert, Tagesmittel) in jeder
   Schreibweise von zwei bis sechs Nachkommastellen und in der Rundreiseform, mit Punkt und mit Komma
   — geprüft mit **Ziffernrandprüfung** (eine Zahl gilt nur als gefunden, wenn sie nicht in einer
   längeren steht; ein Satzpunkt ist eine Grenze, eine folgende Ziffer nicht) und erst ab **sechs
   Ziffern**. Der Grund für die Ziffernschranke ist gemessen, nicht angenommen: Ohne sie hielt die
   Wache zwei Berichte zurück, weil eine Bandgrenze („0.621") und eine Energieabweichung („-37.2 %")
   zufällig mit einer gerundeten Messgröße zusammenfielen. Ein **versehentlich** mitgeschriebener
   Absolutwert trägt dagegen die Genauigkeit, mit der er gerechnet wurde.
3. **Die Bauform:** `Objektbefund` führt gar keinen absoluten Messwert. Auch der Kalibriervorschlag
   einer Nichtwohn-Zone steht als **Verhältnis** da („Tagesbedarf je Einheit, Messung/Rechnung") und
   nicht als Betrag; den Betrag erhält der Anwender daraus und trägt ihn in seine eigene Katalogkopie
   ein, nicht in ein Papier.

Ein Fund bricht den Lauf mit Rückgabe 6 ab — es entsteht **keine** Datei. Was die Wache nicht kann:
einen Objektnamen erkennen. Die Anonymisierung bleibt Sache des Anwenders; beide `LIESMICH.md` sagen
es.

---

## 5. Ablage der Reihen

`Referenzlaeufe/Messreihen_INEKON/` mit `LIESMICH.md`, in `.gitignore` bis auf dieses ausgenommen —
Bauform und Begründung wie `Referenzlaeufe/Normzahlen/`. `RepositoryOrdnungWacheTests` hat zwei neue
Fälle nach demselben Muster: die Regel in `.gitignore`, Git wendet sie auf erfundene Pfade wirklich an
und lässt das `LIESMICH.md` frei, nichts steht im Index — dazu die Gegenprobe, dass die Regel
Unterordner trifft und einen ähnlich benannten Ordner nicht.

Offen lizenzierte Fremddaten gehören **nicht** dorthin, sondern in einen Ordner außerhalb des
Repositoriums.

---

## 6. Das Beispiel

`Beispiel/` mit erfundenem Katalog (`katalog/`, fünf CSV-Dateien, runde Werte, Herkunftsart `FIKTIV`)
und zwei Objekten: `BSP-WOHNEN-01` (Personen, Kalender Wohnen, `MitVerteilung`) und `BSP-PFLEGE-01`
(Betten, Kalender Arbeitstage — es bekommt den Kalibriervorschlag). Die `objekt.json` des ersten ist
vollständig kommentiert und dient als Muster.

Die Messreihen sind **synthetisch aus der Rechnung selbst**: die deterministische Jahresreihe mal
einem festen Rauschen (± 15 %, Kongruenzgenerator mit fester Saat), auf **gleiche Jahresenergie**
gebracht und mit einer auf die **Bandmitte gekappten** Spitze; die abgeschnittene Energie wird
wertgewichtet auf die übrigen Stunden verteilt. Der Schalter `--beispielreihe` erzeugt sie
wiederholbar.

**Warum gekappt und nicht gestreckt** — ein Befund des Baus: Weil gegen die kalibrierte Reihe
verglichen wird, verschwindet ein Streckfaktor auf die ganze Reihe wieder. Was die Spitze wirklich
senkt, ist eine **flachere Gestalt** bei gleicher Energie — und genau das sagt die Lehre, an der das
Band hängt (Konzept 3.6). Die Bandmitte kommt aus einem Probelauf des Kern-Vergleichs, nicht aus einem
eigenen Quantil; so setzt dieselbe Rechnung die Grenzen, die später prüft. Beide Objekte sind damit
grün **durch Konstruktion**; das Beispiel prüft den Weg des Werkzeugs, nicht den Rechenweg. Das vierte
Kriterium bleibt im Beispiel gelb — eine aus der Rechnung stammende Reihe könnte eine √N-Abhängigkeit
ohnehin nicht zeigen.

---

## 7. Die Konverter

`Werkzeuge/ZapfprofilValidierung/Konverter/` — `gemeinsam.py` und drei Skripte, **ohne Zusatzpaket**
(auch die Excel-Quelle wird mit `zipfile` und `xml.etree` gelesen; `openpyxl` liegt auf dem Rechner
nicht, und ein Werkzeug des Repositoriums soll keine Abhängigkeit mitbringen, die nur es braucht).
Quellen, Lizenzen und die Eigenheiten je Quelle stehen im `LIESMICH.md` des Ordners.

Vier Regeln, die alle teilen: ein Kalenderjahr; ein zusammenhängendes Fenster mit höchstens 5 % Lücken
und mindestens 30 Tagen, sonst **benannt übergangen**; negative Werte auf 0 und gezählt;
Bezugsmengen als **Platzhalter**, solange sie nicht belegt sind, und als solche im Vermerk.

Ein Befund beim Bau: Das Fenster musste einen **wiederholten** Zeitstempel verkraften. Die
norwegischen Dateien führen die Herbstumstellung der Sommerzeit als doppelte Stunde; die erste Fassung
rechnete daraus einen negativen Lückenanteil.

---

## 8. Der erste Lauf

21 Objekte aus drei Quellen (neun von zwölf norwegischen Gebäuden, zehn spanische Wohnhäuser, zwei
Mehrfamilienhäuser in New York), stochastisch mit zehn Realisierungen, Katalog `Kenndaten_Test.sqlite`,
Laufzeit rund drei Sekunden. Das Ergebnis samt Zahlen, Ursachen und sechs Folgen (V1 bis V5, K5) steht
im Papier
[`2026-09-26_Validierung_offene_Messreihen.md`](../../../aktuell/Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md).
In zwei Sätzen: Die Energie stimmt nach der Kalibrierung bei **allen 21** Objekten exakt, und die
√N-Skalierung ist mit einer Steigung von −0,29 **bestätigt**; Band und Formabgleich sind an keinem
Objekt erfüllt, mit benannten Ursachen (Platzhalter-Bezugsmengen, unbekannte Feiertage, eine Zone je
Objekt, und für kleine Einheitenzahlen ein Bandkriterium, das dort keine brauchbare Messlatte ist).

Die **vierte Quelle** (Flexitility, Zenodo doi:10.5281/zenodo.17831069) wurde geprüft und **nicht
verwendet**: Sie führt den Gesamt-Trinkwasserdurchfluss am Hausanschluss, kein getrenntes Warmwasser.
Ein Konverter dafür entstand nicht.

---

## 9. Abweichungen vom Auftrag

| Nr. | Abweichung | Grund |
|---|---|---|
| A1 | Werkzeug und Proben stehen **nicht** in `WP-Plan.sln` und nicht im Kern-Filter | Der Auftrag nannte `Auslieferungsvorlage` als Muster — und genau die steht dort nicht („Gehoert bewusst NICHT in WP-Plan.sln"), ebenso `Gebaeudevergleich` und `Formularkarte`. Dem Muster folgen heißt: eigene Projektmappe, eigener CI-Schritt |
| A2 | Die √N-Skalierung ist **kein** Kriterium je Objekt, sondern eines über alle Objekte | Abschnitt 3; ein Band um 1 an einem Objekt wäre eine Erfindung, die kein reales Objekt erfüllt |
| A3 | Verglichen wird gegen die **kalibrierte** Reihe, nicht gegen die rohe | Abschnitt 2 (1) |
| A4 | Die verglichene Reihe folgt der **Bilanzgrenze** des Zählers | Abschnitt 2 (2) |
| A5 | Der Kalibriervorschlag steht im Bericht als **Verhältnis**, nicht als Betrag | Der Auftrag wollte „Vorschlagsparameter" im Bericht und gleichzeitig keine absoluten Mengen. Ein Tagesbedarf in kWh ist eine gemessene Menge; als Verhältnis zur Rechnung ist er dieselbe Aussage und fällt nicht unter K5 |
| A6 | Es gibt **zwei** Beispielobjekte, nicht eines | Der Kalibriervorschlag gilt nur Nichtwohn-Zonen; ohne ein zweites Objekt wäre er im Beispiel nicht vorgekommen |
| A7 | Zusätzlich drei **Konverter** und ein Validierungsbericht im Repositorium | Anwenderauftrag vom 26.09.2026 („Nehme Norwegen und Zenodo", später Forbell); die Flexitility-Quelle wurde auf Anwenderentscheid verworfen |

Kein `SqlDialektPruefer`-Lauf: Das Werkzeug führt fünf eigene `SELECT`-Texte in `Sqlitehilfe.cs`,
und der Prüfer sieht `Werkzeuge/` nicht (dieselbe Lage wie bei `Gebaeudevergleich`). Sie laufen
stattdessen in den Proben gegen eine Kopie der Testdatenbank.

---

## 10. Gates

| Gate | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| `dotnet build Werkzeuge/ZapfprofilValidierung/ZapfprofilValidierung.sln -c Release` | 0 Fehler, 0 Warnungen |
| `dotnet test Werkzeuge/ZapfprofilValidierung/ZapfprofilValidierung.sln` | 30 erfolgreich |
| gefilterte Kern-Tests (`RepositoryOrdnungWache`, `DokumentationLinkWache`, `WikiProduktdatenWache`) | 31 erfolgreich |
| voller Testlauf `WP-Plan.Kern.slnf` | siehe Statuszeile #523 |
| Beispiel-Lauf des Werkzeugs (`Beispiel/`, erfundener Katalog) | beide Objekte grün, Kalibrierfaktor 1, Residuum 0 |
| Lauf an 21 offenen Messreihen | durchgelaufen, Ergebnis im Validierungsbericht |

Die Testdatenbank ist unberührt; ein Schemaschritt war nicht nötig.
