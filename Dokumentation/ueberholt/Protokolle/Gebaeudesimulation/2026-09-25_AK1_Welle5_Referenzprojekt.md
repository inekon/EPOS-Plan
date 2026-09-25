# Protokoll: AK1 Welle 5 — das Referenzprojekt mit Kopplung und die Basis R15 (25.09.2026)

**Auftrag** (Anwenderentscheide vom 25.09.2026): fünfte und letzte Welle der Stufe AK1 der
Anlagenkopplung — ein Referenzprojekt mit Kopplung als Kopie von 1017 mit Heizkreis und Kühlübergabe,
die neue Einfrierregel „gesäte Auslegungsdaten der Übergabe", die neue Referenzbasis R15 mit vierzehn
Projekten und das neue Projekt in der CI. Maßgeblich:
[Anlagenkopplung](../../../aktuell/Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 8.4, 11.4 und
11.5, [Kühlkonzept](../../../aktuell/Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 10.4,
[`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md).

## 1 Umsetzung

| Teil | Stand |
|---|---|
| Referenzprojekt | **1047 „Referenz Anlagenkopplung AK1"** (nächste freie Nummer), Kopie von 1017 über `Referenzlaeufe/Skripte/anlagenkopplung_1047_referenzprojekt.py` (Abschnitt 2) |
| Kopplung | `Tab_Einstellungen.Anlagenkopplung` = „AK1", Kühlbetrieb wie 1017; Gebäude 10653: `Heizkreis_Aktiv` 1, `Uebergabe_Art` „RADIATOR", `Heizkurve_Aktiv` 1, `Kuehluebergabe_Aktiv` 1, `Kuehl_Uebergabe_Art` „KUEHLDECKE"; alle übrigen Übergabespalten NULL (Vorgaben der Art), `Regler_Proportionalband` und `Sollwertprofil` NULL |
| Einfrierregel „gesäte Auslegungsdaten der Übergabe" | Wurzel-`CLAUDE.md` (Regressionsnetz), `Referenzlaeufe/LIESMICH.md` (eigener Regelabschnitt), Anlagenkopplung 11.4 als umgesetzt; die Regeln „gesäte Gebäudedaten" und „gesäte Kältedaten" nennen 1047 mit |
| Neue Basis | `Referenzlaeufe/2026-09-25_R15_Anlagenkopplung`, vierzehn Projekte (Abschnitt 4); R14 mit Protokoll nach `Dokumentation/ueberholt/Referenzbasen/`, Ordner entfernt |
| CI | `kern.yml`: Basispfad R15, Projektliste 1030, 1007, 1017, 1045, 1046, 1047; `ios.yml`: nur der Basispfad (rechnet weiter 1030, nicht gestartet) |
| Tests | Projektlisten um 1047 ergänzt: `GebaeudeRueckwegTests`, `GebaeudeVdi6007Tests`, `GebaeudeVdi6007G2Tests`, `ErsatzparameterBauteilwegTests` (vierzehn Referenzprojekte), `WirtschaftlichkeitAnkerTests`, `ZapfprofilCtrlTests`, `ZapfprofilWeicheTests` (sechs CI-Projekte); Bestandszahlen der Testdatenbank nachgezogen: `ReferenzprojektKaelteerzeugerTests` (20 Kennlinienzeilen, die Kopie gleich der Saat), `PreisbasisSchrittTests` (Trägerzeilen), `GebaeudeKatalogverweisTests` (27 Projektgebäude) |
| Papiere | `Referenzlaeufe/LIESMICH.md` (Regel, aktuelle Basis, entfernte Basen), Archiv der Referenzbasen (R14-Abschnitt, Tabellenzeile), `CLAUDE.md`, Status der Gebäudesimulation (AK1, GA, Kopf), Anlagenkopplung (Kopf, 11.4, 11.5), Basisname in Konzept Gebäudesimulation (Q14), Systementwurf (B14, Prüfebene 4), Softwarearchitektur und Kühlkonzept (CI), Konzept Wirtschaftlichkeit (Kopf, Referenzbasis, 6.2) |
| Kein Schemaschritt, kein Rechenweg, kein Logbuch-Satz | Die Welle ändert allein Daten und Basis; eingefroren auf Schemastand 139, nach dem Zusammenführen mit Z5 steht die Testdatenbank auf 140 (Abschnitt 5) |

## 2 Das Referenzprojekt

**Kopierweg.** Einen skriptfähigen Kopierweg des Programms gibt es nicht: `ProjektDuplizierenCtrl`
(„Projekt Speichern unter") ist ein Controller des Kerns ohne Werkzeug davor. Das Skript bildet ihn
Schritt für Schritt nach — Tabellenplan (Projektspalte, `KINDER`, Kinder über deklarierte
Fremdschlüssel, Ausschlüsse, Ergebnistabellen ausgelassen), Reihenfolge, Versatz je Tabelle (MAX über
alle − MIN über die Quellzeilen + 1), freie Projekt-Id über alle Projekttabellen und dieselben
Anweisungen `INSERT … SELECT` mit `IIF(Spalte > 0, Spalte + Versatz, Spalte)`. Die zwei Nachzüge des
Programms (Geräteanker der Kostenpositionen, Bezüge der Speicherauslegung) haben bei 1017 nichts zu tun;
das Skript prüft das und bricht sonst ab. **Gegenprobe:** Auf einer Kopie der Testdatenbank hat ein
Prüfstand außerhalb des Repositoriums `ProjektDuplizierenCtrl.Duplizieren` selbst aufgerufen; der
Zellvergleich aller Tabellen zwischen Programm- und Skriptkopie zeigt genau die sieben gesetzten Zellen
— gleiche Ids, gleiche Zeilen, gleiche `sqlite_sequence`.

**Zellvergleich gegen die Vorfassung** (Sicherung vorher außerhalb des Repositoriums, alle 144 Tabellen,
10 506 981 Zellen): 9 365 neue Zeilen in 24 Tabellen — Projekt, Einstellungen, Klimaregion mit 365
Tagen Klimadaten und 8 760 Solarstunden, Gebäude mit Zuordnung und Tagesverteilung (1 + 192 Zeilen),
vier Anlagen, Wärmepumpe mit neun Kennfeld- und zehn Kühlkennlinienzeilen, BHKW, Heizkessel,
Stromspeicher samt Variante, Stromverbraucher, drei Senkenzeilen, vier Preis- und drei Trägerzeilen,
Wirtschaftlichkeitsparameter — und 21 geänderte Zeilen in `sqlite_sequence`; keine andere Zeile geändert
oder entfernt, Schema gleich (144 Tabellen, 14 Sichten, 217 Indizes). `integrity_check` ok,
`foreign_key_check` leer, 67 903 488 → 68 734 976 Byte, LFS-SHA-256 `f700e81e…` → `5643a7ca…`. Ein
zweiter Lauf des Skripts meldet „nichts zu tun", die Datei bleibt byte-gleich. Kein Hersteller- oder
Produktname kommt dazu; der Projektname ist neutral.

## 3 Plausibilität vor dem Einfrieren

1047 gegen 1017 (dasselbe Gebäude, dieselben Erzeuger; 1017 rechnet ideal):

| | 1017 | 1047 |
|---|---:|---:|
| Heizwärme [MWh/a] | 90,19 | 82,75 (−8,3 %) |
| Heizlastspitze [kW] | 63,16 | 45,64 |
| mittlere Raumtemperatur der Heizzeit [°C] | 20,72 | 20,08 |
| Vorlauf / Rücklauf, bedarfsgewichtet [°C] | — | 36,98 / 33,56 |
| Stunden mit begrenzter Wärmeübergabe [h] | — | 1 108,2 (bis 2,4 K unter dem Sollwert) |
| Kältebedarf [MWh/a] | 2,52 | 2,33 (−7,5 %) |
| Kühlvorlauf / Kühlrücklauf, bedarfsgewichtet [°C] | — | 18,00 / 18,81 |
| Stunden mit begrenzter Kühlübergabe [h] | — | 0 |
| Stunden mit Kühlbedarf; Überhitzungsstunden [h] | 402; 306 | 423; 327 |
| Wärmedeckung BHKW / Elektrokessel / Wärmepumpe [%] | 77,5 / 22,3 / 0,05 | 82,4 / 17,6 / 0 |
| Kältedeckung durch die Wärmepumpe | 98,4 % | 98,8 % |
| Strom der Wärmepumpe Heizseite / Kühlseite [MWh/a] | 0,02 / 0,55 | 0 / 0,51 |
| Netzbezug [MWh/a] | 655,88 | 651,23 |
| Rechenzeit je Gebäude und Jahr (Heizwärme samt Eingang) | 22 ms | 50 bis 62 ms |

- **Heizseite.** Die Übergabe nach den Vorgaben ist auf die stationäre Auslegungsheizlast bemessen; nach
  der Absenkung reicht sie in 1 108 Stunden nicht, die Aufheizspitze wird gekappt, und das Band von 1 K
  hält den Raum im Mittel etwas unter dem Sollwert — daher 8,3 % weniger Heizwärme. Heizwärme, Spitze und
  begrenzte Stunden sind dieselben wie in der Probe der zweiten Welle mit dem Heizkreis allein.
- **Kälteseite.** Das Band hebt die Raumluft bis zu 1 K über den Kühlsollwert, bevor die Kühldecke voll
  liefert (Risiko 6 der vierten Welle): Kältebedarf −7,5 %, Stunden mit Kühlbedarf 402 → 423,
  Überhitzungsstunden 306 → 327 (Probe der vierten Welle mit der Kühldecke allein: 2,36 MWh/a, 330 h).
  Gewollt; an den Sollwerten von 1017 ist nichts geändert. Der Kühlvorlauf ist das Maximum aus dem
  Kühl-Vorlauf der Maschine (18 °C) und der Vorlaufgrenze (16 °C); die Nennleistung der Kühldecke kommt
  aus dem Auslegungstag (19. Juni, 26,4 °C Tagesmittel, 20,41 kW am Katalog-Gebäude).
- **Erzeuger.** Die Wärmepumpe steht auf Kaskadenplatz 3; mit der gekappten Spitze decken BHKW und
  Elektrokessel den Heizbedarf ganz, die Wärmepumpe liefert keine Wärme (in 1017: 0,04 MWh/a). Die
  Kennlinienwahl am gerechneten Vorlauf läuft (35 °C 3 858 h, 45 °C 1 782 h, 55 °C 122 h, 2 309 h unter
  35 °C), wirkt in der Basis aber auf kein Ergebnis — siehe Abschnitt 6. Kühlseite: EER-Jahreswert 4,52
  wie in 1017, Kältestrom 0,51 MWh/a ganz aus dem Netz, 0,03 MWh/a Kälte ungedeckt.
- **Rechenzeit (E36).** Gemessen mit einem Prüfstand außerhalb des Repositoriums, `HeizwaermeEinesGebaeudes`
  auf der Testdatenbank, das Beste aus fünf Läufen nach dem Anlauf, zwei Messungen: 1047 49,5 und 62,3 ms,
  1017 22,2 ms — unter der Grenze von 100 ms je Gebäude und Jahr.

## 4 Nullnachweis und die neue Basis

- `git fetch` vor dem Einfrieren: origin unverändert auf dem Ausgangsstand, kein Merge nötig.
- **Nullnachweis** auf dem Stand mit 1047 in der Testdatenbank: die dreizehn alten Projekte gegen
  `2026-09-24_R14_Kaelteerzeuger` **GESAMT: PASS** (4 207 049 Werte), 394/394 CSV byte-gleich, nur
  `protokoll.txt` anders.
- **Einfrieren:** `Referenzlaeufe/2026-09-25_R15_Anlagenkopplung`, vierzehn Projekte, 432 CSV, 2 447
  Skalare (Aufbau und Werkzeug wie R14). 1047 bringt 38 Dateien und 198 Skalare, darunter die sechs
  gekoppelten Reihen `vorlauf_0.csv`, `ruecklauf_0.csv`, `uebergabe_0.csv`, `kuehlvorlauf_0.csv`,
  `kuehlruecklauf_0.csv`, `kuehluebergabe_0.csv`. Die dreizehn alten Projektordner sind byte-gleich zu
  R14.
- **Determinismus:** zweiter Lauf 14/14, 432/432 CSV byte-gleich; Vergleich GESAMT: PASS (4 610 207 Werte).
- **CI-Kommandozeile:** der Lauf der sechs Projekte mit genau der Zeile aus `kern.yml` samt Vergleich
  gegen R15: **6/6 PASS** (2 208 587 Werte), 198/198 CSV byte-gleich.
- **NaN:** `vorlauf_0.csv` und `ruecklauf_0.csv` von 1047 tragen in den 1 063 Stunden ohne Heizbetrieb
  NaN — die gewollte Lücke der Reihe (8.3); der Vergleich nimmt NaN gegen NaN als gleich. Sonst kein NaN.

## 5 Abnahme

Nach dem Einfrieren ist origin zweimal weitergegangen und zusammengeführt worden:

1. **G3 Wellen C und D1** (Verwaltungen Baustoffe und Bauteilaufbauten; Datenbankleser der Zonen für
   den Lauf) — ohne Konflikt, die Testdatenbank dort unverändert; R15 14/14 PASS, 432/432 CSV
   byte-gleich; Gate grün (Kern 6 728, UI 6 194).
2. **Z5 des Zapfprofilgenerators** mit Schemaschritt **140** (`Tab_TwwMessreihe`) und nachgeführtem
   Tww-Testkatalog — Konflikt in der Testdatenbank und in `Referenzlaeufe/LIESMICH.md`. Die
   Testdatenbank ist die Fassung von origin (`5de448e8…`), auf die das Skript erneut angewandt ist; ein
   zweiter Lauf ändert nichts. Zellvergleich gegen die Fassung von origin (145 Tabellen, 10 507 032
   Zellen): genau die 9 365 Zeilen von 1047 und 21 Zeilen `sqlite_sequence`; gegen die Einfrierfassung
   von R15 allein die Zeilen von Z5, die Zeilen von 1047 Id für Id gleich. `integrity_check` ok,
   `foreign_key_check` leer, 68 747 264 Byte, LFS-SHA-256 `72a98cdd…`; `Testdatenbankschema --trocken`
   legt nichts an. **R15 bleibt:** 14/14 PASS, 432/432 CSV byte-gleich. Der Nachtrag Z5 steht beim
   R15-Abschnitt samt diesem Nachweis.

Die Abnahme lief auf dem Stand nach der zweiten Zusammenführung:

| Prüfung | Ergebnis |
|---|---|
| Bau des Kern-Filters | 0 Fehler |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | 0 Fehler |
| `EPOS.Referenzlauf` | 0 Fehler |
| Test-Gate | Kern 6 815 (1 übersprungen), UI 6 237, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), alle grün — darin `DokumentationLinkWacheTests`, `RepositoryOrdnungWacheTests`, `WikiProduktdatenWacheTests`, `GebaeudeRueckwegTests`; vor den Zusammenführungen ebenso grün (Kern 6 690, UI 6 154) |
| `Auslieferungsvorlage.Tests` | 34 grün |
| SqlDialektPruefer | 1 921 SQL-Texte, 0 Fundstellen |
| Referenzlauf der vierzehn Projekte gegen R15 | GESAMT: PASS (4 610 207 Werte), 432/432 CSV byte-gleich |
| Die sechs CI-Projekte mit der Zeile aus `kern.yml` gegen R15 | GESAMT: PASS (2 208 587 Werte), 198/198 CSV byte-gleich |
| Konfliktmarker, `git status` | keine; sauber |

Der erste Lauf des Gates auf dem Stand mit 1047 zeigte neun rote Bestandsproben, die die Testdatenbank
Zelle für Zelle festhalten (eine gesäte Kopplung, zwei kühlende Projekte, 20 Kennlinienzeilen, 27
Projektgebäude, drei Trägerzeilen mehr); sie sind auf das neue Referenzprojekt nachgezogen (Abschnitt 1).
Kein Rechentest ist rot geworden.

## 6 Offen

- **Die Kennlinienwahl der Wärmepumpe am gerechneten Vorlauf** (die benannte Ausnahme von AK1, 6.1)
  rechnet in 1047, wirkt aber auf kein Ergebnis der Basis, weil die Wärmepumpe auf Kaskadenplatz 3
  keine Wärme liefert. Eine Probe, die sie im Regressionsnetz hält, bräuchte die Wärmepumpe vor dem
  Elektrokessel — eine Änderung am Kaskadenplatz von 1047 wäre ein Basiswechsel und ist Sache des
  Anwenders.
- **`pruefen`** (`Referenzlauf/Plausibilitaet.cs`) beanstandet die gewollten NaN-Lücken von
  `vorlauf_0.csv` und `ruecklauf_0.csv`. Weder CI noch Gate rufen den Modus; eine benannte Ausnahme für
  die Reihen des Heizkreises wäre ein kleiner Nachzug am Werkzeug.
- Aus AK1 weiter offen: die iOS-Zeilen je Maske (N-A5, Wochenraster) ohne eigenen Lauf; der Upload der
  Wiki-Seiten mit dem Sammel-Upload (diese Welle ändert keine Bedienung und bekommt keinen Logbuch-Satz).
