# Protokoll: AK1 Welle 5 — das Referenzprojekt mit Kopplung und die Basis R15 (25.09.2026)

**Auftrag** (Anwenderentscheide vom 25.09.2026): fünfte und letzte Welle der Stufe AK1 der
Anlagenkopplung — ein Referenzprojekt mit Kopplung als Kopie von 1017 mit Heizkreis und Kühlübergabe,
die neue Einfrierregel „gesäte Auslegungsdaten der Übergabe", die neue Referenzbasis R15 mit vierzehn
Projekten und das neue Projekt in der CI. **Nachsteuerung am selben Tag** (Anwenderentscheid zum ersten
offenen Punkt der ersten Abnahme): In 1047 rückt die Wärmepumpe vor den Elektrokessel, R15 wird neu
eingefroren; dazu die benannte NaN-Ausnahme in `pruefen`. Maßgeblich:
[Anlagenkopplung](../../../aktuell/Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 6.1, 8.3,
8.4, 11.4 und 11.5, [Kühlkonzept](../../../aktuell/Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 10.4,
[`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md).

## 1 Umsetzung

| Teil | Stand |
|---|---|
| Referenzprojekt | **1047 „Referenz Anlagenkopplung AK1"** (nächste freie Nummer), Kopie von 1017 über `Referenzlaeufe/Skripte/anlagenkopplung_1047_referenzprojekt.py` (Abschnitt 2) |
| Kopplung | `Tab_Einstellungen.Anlagenkopplung` = „AK1", Kühlbetrieb wie 1017; Gebäude 10653: `Heizkreis_Aktiv` 1, `Uebergabe_Art` „RADIATOR", `Heizkurve_Aktiv` 1, `Kuehluebergabe_Aktiv` 1, `Kuehl_Uebergabe_Art` „KUEHLDECKE"; alle übrigen Übergabespalten NULL (Vorgaben der Art), `Regler_Proportionalband` und `Sollwertprofil` NULL |
| Kaskade | 1047: BHKW, Wärmepumpe, Elektrokessel (`Tool_2`/`Tool_3` getauscht wie der Pfeil „nach vorn" der Simulationskonfiguration); `Kaskade_Gepflegt` bleibt 0 wie in allen Projekten der Testdatenbank — die Marke wirkt nur, wenn der Heizkessel keinen Platz hat, und sie mitzusetzen hätte eine dritte Zelle ohne Rechenwirkung bedeutet (Muster KU2: `Tool_3` von 1017 ohne Marke). 1017 bleibt BHKW, Elektrokessel, Wärmepumpe |
| Einfrierregel „gesäte Auslegungsdaten der Übergabe" | Wurzel-`CLAUDE.md` (Regressionsnetz), `Referenzlaeufe/LIESMICH.md` (eigener Regelabschnitt), Anlagenkopplung 11.4 als umgesetzt; sie umfasst die Kaskade eines gekoppelten Referenzprojekts; „gesäte Kältedaten" deckt den Platz der Wärmepumpe zugleich (Wortlaut seit KU2 „der Kaskadenplatz der Wärmepumpe"); die Regeln „gesäte Gebäudedaten" und „gesäte Kältedaten" nennen 1047 mit |
| Neue Basis | `Referenzlaeufe/2026-09-25_R15_Anlagenkopplung`, vierzehn Projekte, auf Schemastand 141 (Abschnitt 4); R14 mit Protokoll nach `Dokumentation/ueberholt/Referenzbasen/`, Ordner entfernt; die Nachträge Z5 (140) und #496 (141), gegen R14 gemessen, stehen dort am Ende des R14-Abschnitts |
| CI | `kern.yml`: Basispfad R15, Projektliste 1030, 1007, 1017, 1045, 1046, 1047; `ios.yml`: nur der Basispfad (rechnet weiter 1030, nicht gestartet) |
| `pruefen` | `Referenzlauf/Plausibilitaet.cs`: benannte Ausnahme für die NaN-Lücken von `vorlauf_<n>`, `ruecklauf_<n>`, `kuehlvorlauf_<n>`, `kuehlruecklauf_<n>.csv` — nur diese Muster, NaN nur als Lücke (Hinweis), Inf, Text und eine Reihe nur aus Lücken bleiben beanstandet; Test `ReferenzlaufPlausibilitaetTests` (15 Fälle, die Datei in `EPOS.Kern.Tests` verlinkt wie in `EPOS.Referenzlauf`) |
| Tests | Projektlisten um 1047 ergänzt: `GebaeudeRueckwegTests`, `GebaeudeVdi6007Tests`, `GebaeudeVdi6007G2Tests`, `ErsatzparameterBauteilwegTests` (vierzehn Referenzprojekte), `WirtschaftlichkeitAnkerTests`, `ZapfprofilCtrlTests`, `ZapfprofilWeicheTests` (sechs CI-Projekte); Bestandsproben der Testdatenbank nachgezogen: `AnlagenkopplungSchemaTests`, `KuehluebergabeSchemaTests` (gesät ist allein 1047, Zelle für Zelle), `KuehlungSchemaTests`, `KuehlungErzeugerSchemaTests`, `KuehlbetriebProgrammeinstellungTests`, `ReferenzprojektKaelteerzeugerTests` (1017 und 1047 kühlen, die Kühlkennlinie der Kopie gleich der Saat), `PreisbasisSchrittTests` (drei Trägerzeilen mehr), `GebaeudeKatalogverweisTests` (27 Projektgebäude) |
| Papiere | `Referenzlaeufe/LIESMICH.md` (Regeln, aktuelle Basis, entfernte Basen), Archiv der Referenzbasen (R14-Abschnitt, Tabellenzeile), `CLAUDE.md`, Status der Gebäudesimulation (AK1, GA, Kopf), Anlagenkopplung (Kopf, 11.4, 11.5), Basisname in Konzept Gebäudesimulation (Q14), Systementwurf (B14, Prüfebene 4), Softwarearchitektur und Kühlkonzept (CI), Konzept Wirtschaftlichkeit (Kopf, Referenzbasis, 6.2) |
| Kein Schemaschritt, kein Rechenweg, kein Logbuch-Satz | Die Welle ändert allein Daten, Basis und das Prüfwerkzeug; die Schemastände 140 und 141 kommen von origin (Abschnitt 5) |

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
Zellvergleich aller Tabellen zwischen Programm- und Skriptkopie zeigt genau die gesetzten Zellen
(damals sieben, mit der Kaskade neun) — gleiche Ids, gleiche Zeilen, gleiche `sqlite_sequence`.

**Neun Zellen:** Beschreibung, `Anlagenkopplung`, `Tool_2` und `Tool_3` (Abschnitt 1, Kaskade), am
Gebäude 10653 die fünf Schalter und Arten der Kopplung. Das Skript prüft die ganze Kaskade von Vorlage
und Kopie (`Tool_1` bis `Tool_6`, `Kaskade_Gepflegt`) und die Kühl- und Übergabespalten beider Gebäude.

**Zellvergleiche.** Die Kaskade gegen die Fassung davor (`72a98cdd…`): genau die zwei Zellen `Tool_2` und
`Tool_3` von 1047. Die endgültige Testdatenbank ist die Fassung von origin mit Schemastand 141
(`a427aa72…`) samt Skript; gegen sie (10 507 032 Zellen): 9 365 neue Zeilen in 24 Tabellen — Projekt,
Einstellungen, Klimaregion mit 365 Tagen Klimadaten und 8 760 Solarstunden, Gebäude mit Zuordnung und
Tagesverteilung (1 + 192 Zeilen), vier Anlagen, Wärmepumpe mit neun Kennfeld- und zehn
Kühlkennlinienzeilen, BHKW, Heizkessel, Stromspeicher samt Variante, Stromverbraucher, drei Senkenzeilen,
vier Preis- und drei Trägerzeilen, Wirtschaftlichkeitsparameter — und 21 geänderte Zeilen in
`sqlite_sequence`; keine andere Zeile geändert oder entfernt, Schema gleich. `integrity_check` ok,
`foreign_key_check` leer, 68 747 264 Byte, LFS-SHA-256 `b48add6a…`. Ein zweiter Lauf des Skripts meldet
„nichts zu tun", die Datei bleibt byte-gleich; ein frischer Lauf auf der Fassung 140 von origin ergab
dieselben Bytes wie die Anwendung auf die vorige eigene Fassung. Kein Hersteller- oder Produktname kommt
dazu; der Projektname ist neutral.

## 3 Plausibilität vor dem Einfrieren

1047 gegen 1017 (dasselbe Gebäude, dieselben Erzeuger; 1017 rechnet ideal, mit der Wärmepumpe auf
Platz 3):

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
| Kaskade der Wärmeerzeuger | BHKW, Elektrokessel, WP | BHKW, WP, Elektrokessel |
| Wärmedeckung BHKW / Wärmepumpe / Elektrokessel [%] | 77,5 / 0,05 / 22,3 | 82,4 / 16,4 / 1,2 |
| Wärme der Wärmepumpe; Strom Heizseite [MWh/a] | 0,04; 0,02 | 13,60; 3,54 |
| Jahresarbeitszahl der Wärmepumpe im Heizbetrieb | 2,49 | 3,84 |
| Kältedeckung durch die Wärmepumpe; Strom Kühlseite [MWh/a] | 98,4 %; 0,55 | 98,8 %; 0,51 |
| Netzbezug [MWh/a] | 655,88 | 641,18 |
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
- **Erzeuger.** In der ersten Einfrierung stand die Wärmepumpe auf Platz 3 und lieferte keine Wärme —
  BHKW und Elektrokessel deckten die gekappte Last ganz. Auf Platz 2 übernimmt sie 13,60 MWh/a, der
  Elektrokessel nur noch 1,00 MWh/a (vorher 14,60), BHKW und Kälteseite bleiben Zeichen für Zeichen. Die
  Kennlinienwahl am gerechneten Vorlauf nennt der Lauf je Stützstelle: 35 °C 3 858 h, 45 °C 1 782 h,
  55 °C 122 h, 2 309 h unter 35 °C (dort gilt die unterste Kennlinie).
- **Die Kennlinienwahl wirkt** — Gegenprobe an einer Arbeitskopie außerhalb des Repositoriums, 1047 mit
  `Anlagenkopplung` NULL, Kaskade gleich: Die Wärmepumpe rechnet dann durchgehend an der Kennlinie des
  Anlagenvorlaufs (`Tab_Energieanlagen.Vorlauf` 55 °C); Jahresarbeitszahl **3,05** (19,04 MWh/a Wärme,
  6,25 MWh/a Strom, 575 Betriebsstunden) gegen **3,84** gekoppelt (13,60 / 3,54, 396 h). Die Probe
  vermischt zwei Wirkungen — ungekoppelt ist auch der Heizbedarf höher (90,19 MWh/a) —, die Richtung und
  der Abstand zeigen aber die Kennlinie am niedrigen Vorlauf.
- **Rechenzeit (E36).** Gemessen mit einem Prüfstand außerhalb des Repositoriums, `HeizwaermeEinesGebaeudes`
  auf der Testdatenbank, das Beste aus fünf Läufen nach dem Anlauf, zwei Messungen: 1047 49,5 und 62,3 ms,
  1017 22,2 ms — unter der Grenze von 100 ms je Gebäude und Jahr. Die Kaskade berührt die Gebäuderechnung
  nicht.

## 4 Nullnachweis und die neue Basis

- **Nullnachweis** auf dem Stand mit 1047 in der Testdatenbank: die dreizehn alten Projekte gegen
  `2026-09-24_R14_Kaelteerzeuger` **GESAMT: PASS** (4 207 049 Werte), 394/394 CSV byte-gleich, nur
  `protokoll.txt` anders.
- **Erste Einfrierung** (Schemastand 139, Wärmepumpe von 1047 auf Platz 3): vierzehn Projekte, 432 CSV,
  2 447 Skalare; zweiter Lauf byte-gleich; die sechs CI-Projekte PASS.
- **Neu eingefroren** unter demselben Namen (noch nicht veröffentlicht), nach `git fetch` und dem
  Zusammenführen mit origin (Schemastand 141, Abschnitt 5): `Referenzlaeufe/2026-09-25_R15_Anlagenkopplung`,
  vierzehn Projekte, 432 CSV, 2 447 Skalare. Gegen die erste Einfrierung: **die dreizehn übrigen Projekte
  byte-gleich** (394/394 CSV), in 1047 neun Dateien anders (`aggregate.csv`, `wp_produktion`, `wp_strom`,
  `wp_waermebedarf`, `wp_restwaerme`, `kessel_leistung`, `kessel_strom`, `kessel_waermebedarf`,
  `reststrom_viertelstunde`), 29 gleich — darunter die sechs gekoppelten Reihen `vorlauf_0.csv`,
  `ruecklauf_0.csv`, `uebergabe_0.csv`, `kuehlvorlauf_0.csv`, `kuehlruecklauf_0.csv`, `kuehluebergabe_0.csv`.
- **Determinismus:** zweiter Lauf 14/14, 432/432 CSV byte-gleich; Vergleich GESAMT: PASS (4 610 207 Werte).
- **CI-Kommandozeile:** der Lauf der sechs Projekte mit genau der Zeile aus `kern.yml` samt Vergleich
  gegen R15: **6/6 PASS** (2 208 587 Werte), 198/198 CSV byte-gleich.
- **NaN und `pruefen`:** `vorlauf_0.csv` und `ruecklauf_0.csv` von 1047 tragen in den 1 063 Stunden ohne
  Heizbetrieb NaN — die gewollte Lücke der Reihe (8.3); der Vergleich nimmt NaN gegen NaN als gleich.
  `pruefen` über R15: **GESAMT plausibel**, die Lücken als Hinweis. Die erste Einfrierung beanstandete
  `pruefen` außer den Lücken wegen `wp_produktion.csv` = 0 — genau die Wärmepumpe ohne Wärme, die die
  Kaskade behebt.

## 5 Zusammenführungen und Abnahme

Während der Welle ist origin viermal weitergegangen und zusammengeführt worden:

1. **G3 Wellen C und D1** (Verwaltungen Baustoffe und Bauteilaufbauten; Datenbankleser der Zonen für
   den Lauf) — ohne Konflikt, die Testdatenbank dort unverändert; R15 14/14 PASS, byte-gleich.
2. **Z5 des Zapfprofilgenerators** mit Schemaschritt **140** (`Tab_TwwMessreihe`) und nachgeführtem
   Tww-Testkatalog — Konflikt in der Testdatenbank und in `Referenzlaeufe/LIESMICH.md`; Testdatenbank als
   Fassung von origin (`5de448e8…`) plus Skript; R15 14/14 PASS, byte-gleich.
3. **#496** mit Schemaschritt **141** (Folgeberichtigung im Gebäudekatalog, kein Projekt führt die Sätze)
   — Konflikt in der Testdatenbank und in `Referenzlaeufe/LIESMICH.md`; Testdatenbank als Fassung von
   origin (`a427aa72…`) plus Skript samt Kaskade (Abschnitt 2), dann das neue Einfrieren.
4. **#497 bis #499** (Projektassistent, Unternehmensart im Parameterdialog der Wirtschaftlichkeit,
   Zapfprofil-Entscheide) — weder Testdatenbank noch Rechenweg; Konflikt allein im Kopf des
   Wirtschaftlichkeitskonzepts (Basisname). R15 14/14 PASS, byte-gleich.

Die Nachträge Z5 und #496 sind gegen R14 gemessen; sie stehen im Wortlaut am Ende des archivierten
R14-Abschnitts unter `Dokumentation/ueberholt/Referenzbasen/`.

Die Abnahme lief auf dem Endstand:

| Prüfung | Ergebnis |
|---|---|
| Bau des Kern-Filters | 0 Fehler |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | 0 Fehler |
| `EPOS.Referenzlauf` | 0 Fehler |
| Test-Gate | Kern 6 841 (1 übersprungen), UI 6 243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), alle grün — darin `ReferenzlaufPlausibilitaetTests`, `DokumentationLinkWacheTests`, `RepositoryOrdnungWacheTests`, `WikiProduktdatenWacheTests`, `GebaeudeRueckwegTests`, `HeizkesselKaskadeTests` (keine Kaskadenmarke in der Testdatenbank) |
| `Auslieferungsvorlage.Tests` | 34 grün |
| SqlDialektPruefer | 1 921 SQL-Texte, 0 Fundstellen |
| `Werkzeuge/Testdatenbankschema --trocken` | nichts anzulegen |
| Referenzlauf der vierzehn Projekte gegen R15 | GESAMT: PASS (4 610 207 Werte), 432/432 CSV byte-gleich |
| Die sechs CI-Projekte mit der Zeile aus `kern.yml` gegen R15 | GESAMT: PASS (2 208 587 Werte), 198/198 CSV byte-gleich |
| `pruefen` über R15 | GESAMT: plausibel (die Lücken von 1047 als Hinweis) |
| Konfliktmarker, `git status` | keine; sauber |

Der erste Lauf des Gates auf dem Stand mit 1047 zeigte neun rote Bestandsproben, die die Testdatenbank
Zelle für Zelle festhalten (eine gesäte Kopplung, zwei kühlende Projekte, 20 Kennlinienzeilen, 27
Projektgebäude, drei Trägerzeilen mehr); sie sind auf das neue Referenzprojekt nachgezogen (Abschnitt 1).
Kein Rechentest ist rot geworden.

## 6 Offen

- Aus AK1 weiter offen: die iOS-Zeilen je Maske (N-A5, Wochenraster) ohne eigenen Lauf; der Upload der
  Wiki-Seiten mit dem Sammel-Upload (diese Welle ändert keine Bedienung und bekommt keinen Logbuch-Satz).
