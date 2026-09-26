# Speicherauslegung: Auslieferungswerte aus der Vorlage V4 (26.09.2026)

Protokoll des Postens **#543** (vergeben als #538; die Nummer trägt G6b, #543 und #542 sind anderweitig vergeben). Auftrag: Anwenderauftrag „setze um: Speicherauslegung" — die
Setzungen der Speicherauslegung bekommen ihre Auslieferungswerte aus der INEKON-Vorlage
`TWW-Auslegung_V4.xlsx` und wandern in den freien Paketteil (Prüfliste ZU21, Abschnitt 3;
Umsetzungskonzept 4.7, Nachtrag N28). Zweig `zsp`, Worktree `.claude/worktrees/zsp`, Opus 5.5.
Kein Schemaschritt.

---

## 1. Quelle und Ablesung

Die Vorlage liegt in der Ablage des Anwenders (Laufwerk Z:, Ordner Wärmespeicher) und wurde nur
gelesen: als ZIP/XML (`xl/sharedStrings.xml`, `xl/worksheets/sheet1.xml` … `sheet6.xml`, Formeln aus
`<f>`), ohne Kopie ins Repositorium. Blattköpfe „Version 2.1.2", Dateistand 30.07.2026 (Dateizeit und
`docProps/core.xml`), Blattfolge Eingaben, Zapfprofil, DIN 4708-2, Berechnung, Ergebnis, Hilfe.

| Schlüssel | Wert | Blatt, Zelle, Beschriftung | Testwert vorher |
|---|---|---|---|
| `Speicherauslegung.Speichertemperatur_Vorgabe` | 60 °C | Eingaben B22 „Speichertemperatur T_Speicher" | 56 °C |
| `Speicherauslegung.Nutzanteil` | 0,80 | Eingaben B30 „nutzbarer Speicheranteil f_nutz" | 0,75 |
| `Speicherauslegung.Zuschlag` | 0,15 | Eingaben B31 „Sicherheitszuschlag" | 0,10 |
| `Speicherauslegung.Ladefenster.Laenge` | 8 h | Eingaben B63 „verfügbares Ladezeitfenster" | 10 h |
| `Speicherauslegung.Klassisch.LiterJePersonTag` | 35 l/(P·d) | Berechnung B387 „Faustwert-Zapfmenge" | 40 l/(P·d) |
| `Speicherauslegung.Klassisch.Spreizung` | 50 K | Berechnung B388 „Bezug Warmwasser" 60 °C minus B389 „Bezug Kaltwasser" 10 °C | 45 K |
| `Speicherauslegung.Klassisch.Warnfaktor` | 3 | Ergebnis B34, Formel `IF($E$16>3*MAX($E$13,$E$14,$E$15), …)` | 2,5 |
| `Speicherauslegung.Nenninhalt.Raster` | 1 000 l | Ergebnis B25 `CEILING($B$24,1000)` über dem Listenende; A54 „oberhalb 10.000 l wird auf volle 1.000 l aufgerundet" | 500 l |
| `Speicherauslegung.Nenninhalt.Liste.1` … `.14` | 100, 150, 200, 300, 400, 500, 800, 1 000, 1 500, 2 000, 3 000, 5 000, 8 000, 10 000 l | Ergebnis A40 … A53 „Nachschlagetabelle marktübliche Speichergrößen" | 120, 250, 400, 650, 900, 1 400 l |
| `Speicherauslegung.Ladefenster.Beginn` | — | kein Wert in V4: die Bilanz lädt konstant über 24 h (Berechnung B12 „konstant über 24 h") | 22 h (bleibt fiktiv) |
| `Speicherauslegung.GLF_Gueltigkeitsgrenze` | — | kein Wert in V4: Hilfe B66 nur qualitativ | 30 (bleibt fiktiv) |

**Gegenprüfung** gegen die Formelsammlung der Vorlagenanalyse
(`Dokumentation/aktuell/Zapfprofilgenerator/2026-09-22_Vorlagenanalyse_TWW-Auslegung_V4.md`,
Abschnitte 2.1, 2.4, 2.5, 3): f_nutz 0,80 und Zuschlag 0,15 (2.1), 35 l/(P·d) bei 60/10 °C
(„V_klass = Personen · 35 · (60 − 10) / ΔT · (1 + Zuschlag)"), Speichergrößen-Liste 100 … 10 000 l
und „> 10.000 l: auf volle 1.000 l" (2.5, 3), Ladezeitfenster 8 h (2.1), Faktor 3 der Warnung zum
klassischen Faustwert (2.5) — alle gleich. **Abweichungen:** Wirkung des Ladefensters (V4:
Schätzhilfe; EPOS-Plan: zusätzlich Nachladegrenze der Stundenbilanz), Speichertemperatur 60 °C gleich
der Mindesttemperatur nach DVGW W 551 (ausgeliefert ist der Eingabewert der Vorlage;
`W551.Mindesttemperatur` unberührt). Alle Testwerte weichen ab (sie waren bewusst neben jedem Wert der
Vorlage gewählt).

## 2. Paketteil und Erzeugungsweg

- Neue Quelle `Referenzlaeufe/Skripte/speicherauslegung_v4.json`: je Schlüssel Wert, Einheit, Blatt,
  Zelle, Fundstelle in Worten, Beschriftung; Kopf mit Vorlage, Herkunftsart und den zwei offenen
  Schlüsseln samt Grund.
- `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py`: `--paketteil-schreiben` erzeugt die 22 Zeilen am
  Ende von `Tab_TwwParameter_STAMM.csv` (die 13 übrigen bleiben Handpflege) und hält sie bei jedem Lauf
  dagegen; Prüfregel der Parameterdatei `FREI` oder `EIGENKONSTRUKTION`; freie Parameter, die der
  Paketteil nicht mehr führt, fallen für beide Herkunftsarten. Die fiktiven Einträge der 22 Schlüssel
  sind aus dem Testkatalog entfernt (gleicher natürlicher Schlüssel wie die Paketteilzeile).
- Zeilen: Status `AUSLIEFERUNG`, `ReadOnly` 1, ohne Katalogversion, `Version` `FREI-1`, Herkunftsart
  `EIGENKONSTRUKTION`, `Quelle` „INEKON-Vorlage TWW-Auslegung V4 (Version 2.1.2, 30.07.2026), Blatt …,
  Zeile …, Spalte …", `Ausgabe` = Beschriftung. Die Fundstelle steht in Worten: die erste Fassung mit
  Zelladresse („Zelle B387") ließ `WikiProduktdatenWacheTests.Kein_Katalogtext_der_Tww_Kataloge_nennt_Hersteller_oder_Produktdaten`
  anschlagen (Typcode-Muster `B387`, `B388`, `B389`).

**Herkunftsart-Entscheid.** `EIGENKONSTRUKTION`: Setzung von INEKON aus eigener Unterlage — `FREI`
wäre eine falsche Aussage über eine nicht veröffentlichte Mappe, `VERFAHREN` gilt für gerechnete
Werte. `TwwKataloge.PAKETTEIL_HERKUNFT` ist dafür um `EIGENKONSTRUKTION` erweitert (die Einspielregel
nimmt die Herkunft jeder Provenienzgruppe gegen diese Liste). Abweichung vom Quellendossier (Tabelle
„INEKON-Setzungen": Quelle „Eigenkonstruktion", ohne Ausgabe): die Quelle nennt die Vorlage samt
Fundstelle, die Ausgabe die Beschriftung — so verlangt der Auftrag, und es ist nachprüfbarer.

## 3. Testdatenbank

Aus der origin-Fassung neu gesät (erst `97e56579` nach #537, beim Abschluss-Merge unverändert):
`tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite --paketteil-schreiben` — 8 Zeilen
angelegt (`Liste.7` … `.14`), 14 nachgeführt (die fiktiven Zeilen derselben Schlüssel); zweiter Lauf
„0 Zeile(n) angelegt, 0 nachgefuehrt"; `integrity_check` ok, `foreign_key_check` 0, Status
AUSLIEFERUNG/IMPORT 0. Zellvergleich gegen die origin-Datei: Schema gleich, abweichend nur
`Tab_TwwParameter_STAMM` (85 → 93 Zeilen) und der Zähler in `sqlite_sequence`. Neue LFS-Kennung
`22e67400`, mit aktivem LFS-Filter committet, keine `-shm`/`-wal`.

## 4. Werkzeug, Wachen und Tests

- `Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs`: `PAKETTEIL_HERKUNFT` mit drei Arten, Zählung der
  Paketteilzeilen im Bericht über alle drei; **neuer Prüfposten** beim Einspielen:
  `ParameterDesPaketteilsPruefen` — jeder Parameter des Paketteils ist ein Schlüssel von
  `TwwParameterkatalog`, in dessen Einheit und Bereich, sonst benannter Abbruch mit Datei und Zeile;
  Berichtszeile „ok      jeder Parameter des Paketteils ist ein Schluessel des Programms, in Einheit
  und Bereich (35)".
- `Werkzeuge/Auslieferungsvorlage.Tests`: `TwwVorlageTests` (Herkunftsart je Zeile wie die Datei,
  Zählung über Schlüssel und Herkunftsart, neue Berichtszeile), `AblaufTests` (freie Zeilen über drei
  Herkunftsarten).
- `EPOS.Kern.Tests/TwwKatalogWacheTests`: Anzahl der Paketteilparameter über `FREI` und
  `EIGENKONSTRUKTION`. `TwwParameterschluesselWacheTests` unverändert grün (jeder Schlüssel bekannt,
  Einheit und Bereich passen).
- Tests auf der Testdatenbank: `ZapfprofilAuslegungHuelleTests` (Nenninhalt aus der Liste der
  Datenbank, Nutzanteil 0,8, Zuschlag 0,15), `ZapfprofilAuslegungCtrlTests` (Nenninhalt aus der Liste
  der Datenbank), `ZapfprofilHuelleStufenTests` (Ladefenster 8 h); `AuslegungTestbau` bekommt
  `NenninhalteDerDatenbank`.

## 5. Papiere

Umsetzungskonzept: Nachtrag **N28** (Nummer nach #539 = N27 und #540 = N29), Kapitel 9 Zeilen K8
und ZU21; Prüfliste ZU21 Abschnitt 3; `Referenzlaeufe/Katalogpaket_frei/LIESMICH.md` (Regel 1 mit drei
Herkunftsarten, Tabelle 35 Zeilen, Absatz zur Speicherauslegung); Index; Logbuch-Satz #543 in
`Wiki_Update_2026-09-26.md` unter 1.2.0.4; Wiki-Quelle „Brauchwasser-Zapfprofil", Abschnitt
Verfahrensvergleich der Auslegung (ein Satz zur Herkunft der Vorgaben, Tabuwortprüfung ohne Treffer).

## 6. Gate

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler (vor und nach dem Abschluss-Merge).
- Gefilterte Tests (Tww, Zapfprofil, Speicherauslegung, Auslegung, Parameter, Auslieferung, Vorlage,
  Dokumentations-, Wiki- und Ordnungswache): 1 194/1 194, nach dem Merge wiederholt.
- Voller Lauf `WP-Plan.Kern.slnf`: 0 Fehler (KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 + 1
  übersprungen, EPOS.UI.Tests 6 531, EPOS.Kern.Tests 8 096 + 1 übersprungen); nach dem dritten Merge
  wiederholt: EPOS.UI.Tests 6 545, EPOS.Kern.Tests 8 115 + 1, übrige unverändert, 0 Fehler.
- `Auslieferungsvorlage.Tests`: 38/38, nach dem Merge wiederholt.
- `SqlDialektPruefer`: 1 988 SQL-Texte, 0 Fundstellen (nach dem dritten Merge 1 990, 0 Fundstellen).
- Referenzlauf der sechs CI-Projekte gegen `2026-09-26_R20_Zapfprofil`: alle PASS (2 208 587 Werte;
  1045 rechnet Zapfprofil, die Parameter der Speicherauslegung berühren die Bilanz nicht), nach dem
  Merge wiederholt.

## 7. Merge und Folgen

Zwei Merges von `origin/ios_migration_september`: `ee33dd66` (#537 Ecodesign, Testdatenbank von
origin genommen, Saat wiederholt) und `5eaf8e25` (#539 Validierung, #540 Katalogeinstieg iOS;
Konflikte in Konzept, Index und Logbuch beidseitig zusammengeführt, Testdatenbank von origin
unverändert, Saat 0/0) und `4121813c` (G6b Mehrzonenmodell; die Statusnummer #538 ist dort belegt,
dieser Posten läuft als #543; Konflikte in Index und Statusdatei zusammengeführt, Testdatenbank
unverändert, Saat 0/0; Build, voller Lauf, Vorlagentests, Prüfer und Referenzlauf wiederholt).

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| — | `Speicherauslegung.Ladefenster.Beginn` und `…GLF_Gueltigkeitsgrenze` festlegen oder bewusst Projekt bzw. externem Katalogpaket überlassen; bis dahin lehnt die Speicherauslegung einer Auslieferung ohne Beginn im Projekt benannt ab | Anwenderentscheid | offen |
| — | Logbuch-Satz #543 mit dem Sammel-Upload 1.2.0.4 veröffentlichen | Anwender | mit dem Upload |
