# PB-1 — Plattformbefund des Referenzlaufs: Ursache belegt, Entscheid offen (#598)

Stand: 29.09.2026 · Zweig `ios_migration_september`, gemessen auf `59f3539a` (Kern, Referenzlauf und
Testdatenbank wie `b5caea3c`) · Windows-Sitzung, Opus 5.5 ohne Agenten · Statuszeile „#598“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Kein Rechenweg, keine Basis und keine
Testdatenbank geändert; jede Messung lief in einem Wegwerfstand des Worktrees, der danach zurückgesetzt wurde.

## 1 Anlass

Auftrag des Anwenders vom 29.09.2026: In der Cloud (Linux, SDK 10.0.400) war der Referenzlauf gegen die auf
Windows eingefrorene Basis R23 bei **1008 (54 Abweichungen), 1023 (21) und 1042 (2)** rot — deterministisch
und schon vor KP1b. Verlangt: (1) die Ursache belegen, also die erste abweichende Stunde und die kippende
Entscheidung; (2) einen Entscheid vorschlagen: (a) die Entscheidung robust machen (neue Basis), (b) eine
Linux-Basis oder eine Ausnahmeliste, (c) das Cloud-Gate auf die sieben CI-Projekte beschränken; (3) bis
dahin das Gate so dokumentieren, dass die drei Projekte als vorbestehender Plattformbefund erkennbar sind.
Der Punkt stand offen als „Nach #595 (a)“.

**Lage bei Beginn.** Mit #595 ist R24 **auf Linux** eingefroren. Das Cloud-Gate ist damit grün (15/15,
460/460 CSV byte-gleich), und der Befund zeigt sich in der Gegenrichtung. Ein Windows-Lauf gegen R24 ergab
hier: **1008 FAIL (52), 1023 FAIL (21), 1042 FAIL (2), 441/460 CSV byte-gleich**; innerhalb der Toleranz
verschieden sind `heizstab.csv` von 1007 und 1046 sowie `kessel_leistung.csv`/`kessel_strom.csv` von 1024.
R24 trägt die Linux-Reihen, der Windows-Lauf die Windows-Reihen. Damit lagen beide Seiten des Vergleichs auf
einem Rechner vor, ein Linux-Rechner war nicht nötig.

## 2 Ursache: die C-Bibliothek, nicht die Arithmetik

.NET rechnet `+ − × ÷`, `Math.Sqrt`, `Math.Round` sowie Zahlenlesen und -schreiben auf beiden Plattformen
bitgleich. `Math.Sin`, `Cos`, `Asin`, `Acos`, `Exp`, `Log` und `Pow` reicht die Laufzeit dagegen an die
C-Bibliothek der Plattform durch: unter Windows an die UCRT, unter Linux an glibc. Beide liefern nicht immer
dasselbe letzte Bit.

**Messung.** Eine Wegwerf-Klasse `Math` im Namensraum `WindowsFormsApplication1` verdeckt `System.Math` für
den ganzen Kern, ohne dass eine Aufrufstelle geändert wird. Sie reicht alles durch und merkt je Aufrufstelle
(`CallerFilePath`/`CallerLineNumber`) die Argumente. Im Lauf der fünfzehn Projekte fielen so **625 757
verschiedene Argumente** an. Für jedes wurde der korrekt gerundete Wert mit Python `decimal` auf 60 Stellen
bestimmt. Die UCRT liegt dabei 1 ULP neben dem korrekt gerundeten Wert:

| Aufrufstelle | verschiedene Argumente | 1 ULP daneben |
|---|---|---|
| `Matrix2.cs:202` `Exp` (Gebäudematrix VDI 6007) | 291 138 | 0,71 % |
| `Waermeuebergabe.cs` `Pow` (vier Stellen) | 203 593 | 0,05 % |
| `SolarPVGISCalculator.cs` `Sin`/`Cos`/`Asin`/`Acos` (Sonnenstand) | 102 914 | 1,1–4,9 % je Stelle |
| `ErdreichTemperatur.cs:469` `Cos` | 17 520 | 3,4 % |
| `PvErweitertesModell.cs:97` `Log` | 6 577 | 0,05 % |
| `SimulationSolarthermie.cs:357` `Acos` | 3 947 | 3,9 % |

Jeder dieser Fälle liegt nahe am Rundungsmittelpunkt (Median 0,006 ULP bei `Exp`). Es sind die schweren
Rundungsfälle, die glibc mit seinem Fehler unter etwa 0,52 ULP fast immer, aber nicht immer korrekt rundet.

**Gegenprobe.** Setzt man im Windows-Lauf die korrekt gerundeten Werte ein, rechnet Windows **13 der 15
Projekte byte-gleich zur Linux-Basis R24**, darunter 1023, 1024 und 1042; über alle Projekte sind es 455/460
CSV. Übrig bleiben 1008 in einer Stunde (2261) und drei Heizstabzeilen von 1007 und 1046. Dort rundet auch
glibc „falsch“, also wie die UCRT. Für `TagesbilanzPhysik.cs:215` ist das belegt (0,0003 ULP vom
Mittelpunkt): Setzt man dort den korrekt gerundeten Wert ein, kippt 1040, das auf beiden Plattformen gleich
rechnet.

Nach Funktionsfamilien getrennt:
- **1042** folgt allein aus den Winkelfunktionen des Sonnenstands.
- **1008, 1023 und 1024** brauchen `Exp` der Gebäudematrix und die Winkelfunktionen zusammen.
- `Pow` und `Log` wirken in keinem Projekt.

Das Einsetzen allein der transzendenten Werte erklärt den Befund. Einen Hinweis auf SQLite, Kultur oder
Zeitzone gibt es nicht.

**Warum 441 Dateien trotzdem byte-gleich sind.** `Ergebnisexport` schreibt jede Zahl mit neun geltenden
Stellen (`G9`). Ein Unterschied im letzten Bit bleibt darin unsichtbar, solange er keine Entscheidung kippt.
Die Gebäudereihen weichen intern ab, in der CSV aber nicht.

## 3 Die kippenden Entscheidungen

Verglichen wurde eine Spur der Entscheidungen (Hysterese, Laden, Entladen, Phase G, Laufzeitzählung) des
Windows-Laufs mit derselben Spur des nachgebildeten Linux-Laufs. Die Stunden sind 0-basiert wie in den CSV;
die Zeilennummern gelten für den unveränderten Stand.

| Projekt | erste Stunde | was kippt | Stelle |
|---|---|---|---|
| 1023 | 2500 (in den CSV ab 2501) | Die Nachentladung der Phase E nimmt 0,696 aus dem vollen Puffer (13,92) und steuert ihn auf genau `Q_max · SchwelleAus` = 13,224. Windows landet bei 13,224000000000002, Linux bei 13,223999999999998. Die Abschaltprüfung der Phase G `sp.SOC >= sp.Q_max * sp.SchwelleAus` beendet deshalb auf Windows den Ladebetrieb, auf Linux nicht. Stunde 2501 bucht Linux 0,779 Ladung und 13,92 Entladung, Windows 13,92 und 27,06 | `Kaskadenschleife.cs:1006–1008` |
| 1008 | 2531 (in den CSV ab 2532) | Dieselbe Prüfung: 6,96 − 0,348 = 6,612 = `Q_max · SchwelleAus`, Windows 1 ULP darüber, Linux 1 ULP darunter. Die übrigen Stunden bis 7410 samt Kessel, Wärmepumpe und Reststrom in Stunde 7014 folgen aus derselben Prüfung: Mit Zahlenrand dort rechnen beide Plattformen 1008 gleich | ebenda |
| 1042 | 1740 (Quellpuffer am Stundenende), gezählt ab 1747 | Der **Quellpuffer** der Wärmepumpe (Modul 1) behält auf Linux einen Rest von 4,4·10⁻¹⁶ kWh, auf Windows 0. `faktor = quelle.SOC / quellAnteil` skaliert die Leistung ohne Untergrenze auf 7,2·10⁻¹⁶ kWh; die Speicherladung zählt `ladung / ladeTherm` = 1 volle Betriebsstunde. Der Rest schrumpft je Stunde um den Faktor ≈ 10⁻¹⁶ (10⁻³², 10⁻⁴⁷ …) und zählt jedes Mal. Linux hat 9 solche Stunden mehr (1747–1749, 1781–1786) und eine weniger (1797): **+8 Betriebsstunden** (2 073,4 → 2 081,4), Vollbenutzungsstunden der zwei Module +4 | `SimulationWaermepumpe.cs:1381`, `:1924` |
| 1007, 1046 | 2531 | Der Heizstab deckt einen Auslöschungsrest von 8,3·10⁻¹⁷ kWh — innerhalb der Toleranz, keine Entscheidung | — |
| 1024 | 2577 | Der Kessel deckt 6,7·10⁻¹⁶ kWh — innerhalb der Toleranz | — |

**Die Prüfung der Phase G entscheidet systematisch am letzten Bit, nicht nur plattformweise.** In 1008 liegen
**1 634 von 4 677** Abschaltprüfungen innerhalb des Rechenrands an der Schwelle, in 1023 333 von 5 058. Dort
entscheidet auf jeder Plattform das letzte Bit. Die Plattform trifft davon nur die wenigen Stunden, in denen
eine Transzendente anders rundet. Dieselbe Abschaltschwelle trägt in `HystereseFortschreiben`
(`SimulationPufferspeicher.cs:1115`) seit dem Entscheid W8‑O‑5d‑Q1 vom 07.09.2026 (Basis R5) den Zahlenrand.
Die gleichlautende Prüfung der Phase G (Paket 5, 15.08.2026) blieb damals ohne Rand. Sie fällt unter die
Kernregel „Einen Rand braucht jede Marke, auf die eine Rechnung zusteuert“ (`EPOS.Kern/CLAUDE.md`).

**Die Scheinstunden von 1042 sind ebenfalls kein Plattformfehler.** Auf Windows zählt Modul 1 **498 seiner
2 073 Betriebsstunden** mit einer Ladung unter 10⁻⁹ kWh, auf Linux 506 seiner 2 081. Erzeugte Wärme und Strom
ändert das nicht (20,84 MWh auf beiden Plattformen), wohl aber die Betriebs- und Vollbenutzungsstunden.

Nicht beobachtet, aber gleich gebaut: die Einschaltschwelle `SOC <= Q_max * SchwelleEin`
(`SimulationPufferspeicher.cs:1114`, bewusst ohne Rand) und `rest[kanal] > 0.0001`
(`Kaskadenschleife.cs:1791`).

## 4 Messung der Wege (Wegwerfstand)

Beide Ränder wurden als Schalter eingebaut und je auf Windows und im nachgebildeten Linux-Lauf gerechnet:
- **(a1)** `Rechenrand.SchwelleErreicht(sp.SOC, sp.Q_max * sp.SchwelleAus)` in Phase G;
- **(a2)** ein Rest des Quellpuffers unter `Rechenrand.ABSOLUT` (10⁻⁹ kWh) gilt als leer.

| Stand | Windows gegen Linux-Nachbildung | Windows gegen R24 |
|---|---|---|
| unverändert | 1008, 1023, 1042 FAIL; 441/460 byte-gleich | 1008 (52), 1023 (21), 1042 (2) FAIL; 441/460 |
| mit (a1) | nur 1042 FAIL; 453/460 | 1008, 1018, 1023, 1039, 1042 FAIL; 417/460 |
| mit (a1) und (a2) | **GESAMT PASS**; 455/460 (nur Reste im Band: 1007/1046 Heizstab, 1024 Kessel, 1042 Quellpuffer) | wie mit (a1) |

Wirkung von (a1) und (a2) auf die Basis (Windows gegen R24, Skalare der `aggregate.csv`):

| Projekt | größte Änderung |
|---|---|
| 1008 | `Puffer.Ladung_gesamt` +7,8 %, Kessel-Wärme −0,3 %, WP-Vollbenutzungsstunden +0,15 % |
| 1018 | `Puffer.Ladung_gesamt` +24,6 %, Durchsatz des Puffers −27,6 %, `kessel_restwaerme` −26 % |
| 1023 | `Puffer.Ladung_gesamt` +0,63 %, WP-Vollbenutzungsstunden +0,13 %, Kessel −0,04 % |
| 1039 | `Puffer.Ladung_gesamt` +0,14 %, Durchsatz des Brauchwasserpuffers −13 % |
| 1042 | Betriebsstunden Modul 1: 2 081,4 → 1 575,4; WP-Vollbenutzungsstunden 4 038,34 → 3 785,34 |

Die zehn übrigen Projekte ändern sich durch (a1) und (a2) nicht. Im Windows-Lauf bleiben dort nur die Reste im
Band von 1007, 1024 und 1046.

## 5 Entscheidungsvorlage (Anwenderentscheid offen)

- **(a) Die Entscheidungen robust machen — Empfehlung.** Dazu gehören (a1) und (a2) aus Abschnitt 4 in einer
  Welle, samt neuer Basis R25 und Protokoll in `Referenzlaeufe/LIESMICH.md`.
  - (a1) setzt den bestehenden Entscheid W8‑O‑5d‑Q1 an der Stelle um, an der er fehlt. Er nimmt auf jeder
    Plattform den Zufall aus jeder dritten Abschaltprüfung von 1008.
  - (a2) beseitigt die Scheinstunden: In 1042 ist ein Viertel der Betriebsstunden von Modul 1 eine Folge
    von 10⁻¹⁶ kWh.
  - Danach rechnen Windows und Linux alle fünfzehn Projekte innerhalb der Toleranz gleich. Byte-gleich
    werden sie nicht; es bleiben die Reste im Band.
  - Preis: Der Rechenweg von 1008, 1018, 1023, 1039 und 1042 ändert sich (Abschnitt 4), die Einfrierregeln
    sind nicht berührt.
  - Kein Beweis, dass keine weitere Kante besteht (Abschnitt 3, letzter Absatz).
- **(b) Linux-Basis.** Das ist mit R24 der gegenwärtige Stand, er gilt bis zum Entscheid. Nachteil: Jeder
  Windows-Lauf gegen R24 ist bei 1008, 1023 und 1042 rot. Ein Windows-Gate bräuchte eine Ausnahmeliste. Jede
  Änderung, die die Kante berührt, kann verschieben, welche Stunden auf welcher Plattform kippen. Die Kante
  selbst bleibt.
- **(c) Cloud-Gate auf die sieben CI-Projekte beschränken.** Seit R24 nicht mehr nötig, denn das Cloud-Gate
  ist grün. Es nähme 1008, 1018, 1023, 1039 und 1042 aus der Abnahme, gerade die Projekte, die diese Kante
  durchlaufen. Nicht empfohlen.

## 6 Gate bis zum Entscheid

- `Werkzeuge/Gate/gate_linux.sh` Schritt 5 zeigt jetzt jedes rote Projekt, nicht nur die letzten drei
  Zeilen. Es liest die Plattform der Basis aus ihrem `protokoll.txt` (Quellpfad) und ordnet jedes rote
  Projekt ein:
  - „vorbestehender Plattformbefund PB‑1“, wenn Basis und Lauf auf verschiedenen Plattformen stehen und das
    Projekt 1008, 1023 oder 1042 ist;
  - „PB‑1-Projekt, gleiche Plattform — prüfen“ und „NEU“ sonst.
- Dazu nennt es die byte-verschiedenen CSV. Die Liste steht einmal im Skript (`PB1_PROJEKTE`).
- `Werkzeuge/Gate/LIESMICH.md` beschreibt die Erwartung je Plattform:
  - Linux gegen R24: 15/15, 460/460;
  - Windows gegen R24: 1008 (52), 1023 (21), 1042 (2) FAIL, 441/460;
  - Linux gegen eine Windows-Basis wie R23: 1008 (54), 1023 (21), 1042 (2).
- `Referenzlaeufe/LIESMICH.md` trägt die belegte Ursache als Nachtrag zu R24.

## 7 Wiederholen

Das Verfahren braucht kein Linux, sofern die Basis auf der anderen Plattform eingefroren ist:
1. Windows-Lauf gegen die Basis, `vergleich`, byte-weiser Dateivergleich.
2. Wegwerfdatei in `EPOS.Kern` mit `internal static class Math` im Namensraum `WindowsFormsApplication1`,
   Schalter über Umgebungsvariablen: protokollieren, ersetzen, je Funktion oder Aufrufstelle.
3. Korrekt gerundete Werte mit Python `decimal`; Sinus und Kosinus über Taylorreihen nach Reduktion mit π
   auf 100 Stellen, Arkusfunktionen über `atan`.
4. Ersetzungslauf gegen die Basis.
5. Entscheidungsspur an Hysterese, `Laden`, `Entladen`, Phase G und Laufzeitzählung, Vergleich der Spuren
   mit Toleranz 1e‑9 relativ.

Die Skripte lagen im Scratchpad der Sitzung und sind nicht im Repository.
