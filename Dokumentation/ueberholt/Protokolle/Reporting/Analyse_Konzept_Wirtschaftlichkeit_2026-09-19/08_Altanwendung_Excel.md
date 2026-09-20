# Inventar der Altanwendung BHKW-Plan und Machbarkeit der Zahlenprobe A8

**Stand 19.09.2026.** Fortschreibung von `Dokumentation/ueberholt/Protokolle/Reporting/Analyse_Altanwendung_BHKW-Plan.md`
(18.08.2026). Jene Analyse beschreibt die **VBA-Seite** (149 Module, Rechenketten, 17 Befunde) und die
beiden Stammdatenkataloge; sie enthält **keine Blatt- und Zelltafel** und keine ausgewertete
Projektmappe. Genau das ergänzt dieser Bericht. Alle Mappen wurden nur gelesen; die Quellen auf dem
Netzlaufwerk blieben unverändert.

## 0 Ergebnis in fünf Sätzen

Die Altanwendung besteht aus einer reinen Programmhülle (`BHKW-WP-PLAN.XLSM`, nur die zwei Blätter
`Tmp` und `Startmenue`, keine einzige Formel) und der Projektvorlage `TABELLEN.XLS` mit 41
Arbeitsblättern, in der die gesamte Wirtschaftlichkeit steckt — Eingaben auf `Tab_Kosten`, Erlöse auf
`Tab_Erloese`, statischer Jahresvergleich auf `Tab_Wirtschaftlichkeit`, der Kapitalwert auf
`Tab_Wirtschaftlichkeit_kap` und **ein zweites Mal, mit anderen Preissteigerungen und anderem Ergebnis,
auf `Tab_kurz_KWKG2020`**; die neuere der beiden Programmhüllen ist die aus `BHKWPlan\` (23.08.2026
gegen 25.10.2025), beide `TABELLEN.XLS` sind blattgleich. Die drei benannten Testprojekte liefern die
Zahlenprobe nur zur Hälfte: `goetz_test.XLS` ist ein synthetischer Funktionstest (4 kW, 573 h/a,
Wartung 1 €/kWh_el, KWK-Bonus 0), und die beiden inhaltsgleichen `englmar`-Mappen sind ein echtes
Projekt (33 kW_el, 5 178 h/a, gemessene KWKG-2023-Sätze 8/16 ct/kWh), stammen aber aus der älteren
Blattgeneration **ohne Kapitalwertblatt** — dort gibt es nur Jahresüberschuss und statische
Amortisation. Der Rechenkern deckt sich besser als erwartet: Annuitätenformel, Energiesteuersatz
5,50 €/MWh auf den ungeteilten BHKW-Brennstoff mit Ho/Hi-Faktor 1,108, Vbh-Kontingent 30 000 h und
Jahresdeckel 5 000/4 000/3 500 h sind mit dem EPOS-Konzept deckungsgleich, während Restwert und
Ersatzbeschaffung ganz fehlen, drei statt zwei Preissteigerungsreihen geführt werden und Ölsteuerbasis
wie Stromsteuermenge auseinanderlaufen. A8 ist **machbar und nicht bei null** — `Rechenweg/08` enthält
bereits eine bestandene Gegenprobe gegen eine Altmappe („Höfingen", Kapitalwert 65 259 €) —, es fehlt
aber die Referenzmappe selbst: keine der drei Höfingen-Dateien auf dem Laufwerk trägt diese Zahlen.
Die Sperre „wartet auf Zulieferung der BHKW-Plan-Excel" ist damit aufgehoben, an ihre Stelle tritt ein
engerer, mit Umfang S lösbarer Punkt: die Referenzmappe benennen und das maßgebliche Kapitalwertblatt
festlegen.

## 1 Werkzeuge, Abdeckung und Grenzen

| Mappentyp | Werkzeug | Ergebnis |
|---|---|---|
| `.XLSM` | dotnet-Dateiskript, `ClosedXML@0.105.1` | Blätter, Zellen, Formeln, benannte Bereiche — vollständig |
| `.XLS` (BIFF, RC4-verschlüsselt, Kennwort (bekannt, hier nicht genannt)) | `ExcelDataReader@3.7.0` + `System.Text.Encoding.CodePages@9.0.0`, Kennwort über `ExcelReaderConfiguration.Password` | Blätter und **Werte** — vollständig |
| `.XLS`, Formeln | `NPOI@2.7.2` mit `Biff8EncryptionKey.CurrentUserPassword` | **gescheitert**: `NotImplementedException — Implement it based on poi 4.2 in the future` (RC4-Entschlüsselung nicht umgesetzt) |

**Folge:** Für die XLS-Mappen gibt es in diesem Lauf **keine Formeltexte**. Jede Formelaussage unten ist
entweder aus der Blattbeschriftung gelesen, durch Nachrechnen der Werte belegt („gemessen") oder aus der
früheren VBA-Analyse übernommen — das ist jeweils gekennzeichnet. Kein Excel-Programm wurde gestartet.

Der Leser zählt in `TABELLEN.XLS` **41 Arbeitsblätter**, die frühere Analyse nannte 46 Blätter. Die
Differenz ist **vermutet** auf Diagrammblätter zurückzuführen, die `ExcelDataReader` nicht ausgibt;
nachgeprüft wurde das nicht.

Kopiert wurden alle zehn benannten Dateien (fünf aus `BHKW-WP-Plan_Vorlagen\`, vier plus `bhkwplan.py`
aus `BHKWPlan\`) sowie ergänzend drei Höfingen-Mappen (siehe § 6.1).

## 2 Die beiden `BHKW-WP-PLAN.XLSM` — welche ist die neuere

| Merkmal | `…\Wirtschaftlichkeit\BHKW-WP-Plan_Vorlagen\` | `…\BHKWPlan\` |
|---|---|---|
| Dateigröße | 2 293 458 B | 1 980 157 B |
| Dateidatum | 25.10.2025 | 23.08.2026 |
| `docProps/core.xml` · `dcterms:modified` | 2025-10-25T11:05:26Z | **2026-08-23T19:56:58Z** |
| `dcterms:created` · `dc:creator` | 2000-07-25 · Steinborn | identisch |
| `xl/vbaProject.bin` (entpackt) | 5 728 768 B | 4 279 296 B |
| Blätter | `Tmp`, `Startmenue` | identisch |
| benannte Bereiche (Mappenebene) | 18, alle auf `Startmenue` | identisch |

**Die Fassung aus `BHKWPlan\` ist die neuere** — zehn Monate jünger, belegt durch das Änderungsdatum in
den Dokumenteigenschaften. Der kleinere VBA-Stream widerspricht dem nicht: Er enthält neben dem
Quelltext den kompilierten P-Code, der beim Wechsel der Office-Fassung verworfen und neu erzeugt wird
(**vermutet**, nicht nachgeprüft).

**Beide Hüllen enthalten keine Rechenlogik in Zellen:** Der Leser findet 274 bzw. 126 belegte Zellen und
**null Formeln**. `Tmp` ist ein Zwischenspeicher für Katalogzeilen (BHKW-Module mit Hersteller,
Leistungen, Wirkungsgrad, Investition; Klimagebiete; Gebäudetypen), `Startmenue` trägt Schaltflächen und
einen Zahlenstreifen. Die 18 benannten Bereiche sind sämtlich Makroeinsprünge (`NeuesProj_anlegen`,
`Tageswerte_ausfüllen`, `SchalterHandler`, …), keine Rechengrößen; drei davon lösen auf `#N/A` auf, zwei
auf `#NAME?` (`_xleta.T`, `_xleta.TEXT`).

Die beiden `TABELLEN.XLS` (23.02.2025 im Vorlagenordner, 18.08.2026 unter `BHKWPlan\`) sind
**blattgleich**: gleiche 41 Blattnamen in gleicher Reihenfolge, gleiche belegte Zellzahl je Blatt. Die
Prüfsummen unterscheiden sich, ein inhaltlicher Unterschied wurde nicht gefunden.

## 3 Wo die Wirtschaftlichkeit steckt — Blattkarte `TABELLEN.XLS`

Die Mappe ist die **Projektvorlage**: „Neues Projekt" kopiert sie und speichert sie als
`PROJEKTE\<Name>.XLS`. Die Blätter sind Zwitter aus Eingabespiegel und VBA-geschriebenem
Ergebnisprotokoll; die eigentlichen Eingabemasken sind die VBA-Dialoge.

### 3.1 Die tragenden Blätter

| Blatt | Bereich | belegte Zellen | Rolle |
|---|---|---|---|
| `Tab_Kosten` | A1:U531 | 1 371 | **Eingabe**: Investitionen, Nutzungsdauern, Zins, Betriebskosten, Brennstoffpreise, Referenzsystem |
| `Tab_Erloese` | A1:F82 | 71 | vermiedene Kosten, Einspeiseerlös, KWKG-Bonus, EEG-Bonus |
| `Tab_Wirtschaftlichkeit` | A1:C99 | 34 | **statischer** Jahresvergleich BHKW gegen Vergleichsheizung |
| `Tab_Wirtschaftlichkeit_kap` | A1:AD100 | 496 | **dynamisch**: Kapitalwert, Jahresreihen, Vbh-Deckelung, Szenariomatrix |
| `Tab_Wirtschaftlichkeit_kap_WP` | A1:AB381 | 382 | dasselbe für die Wärmepumpenvariante |
| `Tab_kurz` · `_KWKG2016` · `_KWKG2020` · `_WP` | bis A1:AQ381 | 1 030 / 733 / 739 / 595 | Kurzbericht — **`_KWKG2020` trägt einen zweiten, eigenen Kapitalwert** |
| `Tab_Tarifstruktur` | A1:I84 | 146 | drei Preisregelungen (Bezug, Einspeisung, Restbezug) |
| `Tab_BHKW`, `Tab_Spitzenkessel`, `Tab_Heizkessel`, `Tab_Energiebilanz`, `Tab_Eigenstrombedarf` | klein | 17–57 | Simulationsergebnisse als Rechengrundlage |
| `Ganglinie`, `Dauerlinie`, `Stromdaten`, `Tab_ges` | groß | bis 1 685 | Stunden- und Sortierreihen |

Benannte Bereiche gibt es in `TABELLEN.XLS` und in den Projektmappen **nicht** — die Verweise laufen
über feste Zelladressen im VBA. Für A8 ist das günstig: Die Adressen sind über die Mappen hinweg stabil.

### 3.2 Eingabeblatt `Tab_Kosten` — die Kernzellen

| Größe | Zelle | Bemerkung |
|---|---|---|
| Zinssatz | `B3` | in Prozent |
| Zinsreduktion BHKW-Module | `B4` | zweiter Zinssatz, nur für die Module |
| Zuschuss · pauschalierter Bonus | `F3` · `F2` | mindern die Investition |
| BHKW-Module: Investition / Nutzungsdauer / Kapitalkosten | `B6:B15` / `D6:D15` / `F6:F15` | je Modul eine Zeile |
| Spitzenkessel | `B19:F24` | |
| Heizzentrale, 16 Posten, Summe | `B48:F62`, `B63`/`F63` | |
| Gesamtinvestition · Gesamtkapitalkosten | `B71` · `F71` | |
| Betriebskosten, 12 Positionen, Summe | Zeilen 78–91, `F92` | Spalte D = Prozentsatz, E = Einheit, F = Betrag |
| Brennstoffkosten, 11 Träger, Summe | Zeilen 98–108, `F109` | Mengen **brennwertbezogen (Ho)** |
| Referenzsystem (Heizkessel, Heizraum, Netz) | ab Zeile 110 | gleicher Aufbau |

**Gemessen** an `englmar hdg.XLS`: `B6` = 48 600 €, `D6` = 10 a, `B3` = 3 % ergeben mit
a = i·q^n/(q^n−1) den Wert 5 697,4 €/a; das Blatt trägt `F6` = 5 697. Ebenso Spitzenkessel
(16 238,63 € / 20 a → 1 091,5 gegen `F19` = 1 091) und Referenzkessel (19 288,49 € / 20 a → 1 296,5
gegen `F113` = 1 296). Die **Annuitätenformel der früheren Analyse ist damit von der Tabellenseite
bestätigt — und zwar ohne Restwert**.

### 3.3 Statisches Ergebnisblatt `Tab_Wirtschaftlichkeit`

Ungewöhnlicher Aufbau: Spalte A trägt die Bezeichnung, die Ergebniszahlen stehen in B (BHKW) und C
(Vergleichsheizung) — **außer** beim Überschuss, der in `A67` steht und dessen Einheit in `B67`.

| Bezeichnung | Zelle BHKW / Vergleich |
|---|---|
| Kapitalkosten · Betriebskosten · Brennstoffkosten · Solarkosten | `B4`/`C4` bis `B7`/`C7` |
| Gesamtkosten | `B8`/`C8` |
| Energiesteuerrückerstattung · eingesparte Stromsteuer | `B9` · `B10` |
| Stromeinspeisung + Boni · vermiedene Strombezugskosten | `B11` · `B12` |
| Nettokosten · Gesamterlös | `B13`/`C13` · `B14` |
| spez. Wärme- / Stromgestehungskosten | `B15`/`C15` · `B16` |
| Kalkulationszinssatz | `B39` |
| **Überschuss gegenüber der Vergleichsheizung (mit Energiesteuer)** | `A67` (Wert), `B67` (Einheit) |
| Amortisationszeit mit / ohne Energiesteuer | `B87` · `B102` |
| Steuersätze Flüssiggas / Gas / Öl / Strom | `B93` · `B94` · `B95` · `B97` (4,4 · 5,5 · 61,35 · −20,5) |

### 3.4 Kapitalwertblatt `Tab_Wirtschaftlichkeit_kap`

| Größe | Zelle | Bemerkung |
|---|---|---|
| Preissteigerung Brennstoff / Strom / Wartung | `H3` · `J3` · `L3` | Vorlage 8 % / 8 % / 5 % |
| Kapitalzins | `H4` | |
| **Kapitalwert (nach Nutzungsdauer BHKW)** | `J4` | das Kernergebnis |
| interner Zinssatz | `L4` | in den geprüften Mappen leer |
| Wärmebedarf · Stromproduktion · Kosten KWK · Kosten Heiz · Nutzungsdauer | `P3` · `P4` · `R3` · `R4` · `T3` | Kopfgrößen |
| Jahresraster 0…15 · Mittelwertspalte | `G7:V7` · Spalte `W` („Mittelwert pro Jahr über Nutzungszeit") | |
| Investition (Mehrkosten), kumuliert | `G8:V8` | `G8` = Mehrinvestition, Periode 0 |
| Einsparung · abgezinste Einsparung · Zinsen | Zeilen 9 · 10 · 11 | |
| Einnahmen Strom (vermiedene Kosten, Einspeisung) und Wärme (Betriebs-, Brennstoffkosten des Vergleichssystems) | Zeilen 12–15 | |
| eingesparte Stromsteuer · Energiesteuerrückerstattung | Zeilen 16 · 17 | |
| Brennstoff- und Betriebskosten KWK | Zeilen 18 · 19 | |
| Bonus EEG · Bonus KWK-Strom | Zeilen 20 · 21 | |
| BEHG-CO₂ KWK / Vergleich / Bilanz | Zeilen 22–24 | |
| **erreichte / vergütete / kumuliert vergütete Vbh** | Zeilen 25 · 26 · 27 | die KWKG-Deckelung |
| CO₂-Preis je Jahr | Zeile 29 | 0 · 25 · 30 · 35 · 45 · 55 … |
| KWK-Bonus Eigenstrom / Einspeisung je Jahr | Zeilen 31 · 32 | |
| Zahlungsreihe für den internen Zins | `G35:V35` | |
| Gestehungskostenreihen BHKW / Vergleich | Zeilen 39–41 | |
| **Vbh-Jahresdeckel nach Inbetriebnahmejahr** | `F49:G58` | 2020–22 je 5 000 · 2023/24 je 4 000 · 2025 3 500 · sonst 8 760 |
| **BEHG-CO₂-Preispfad** | `I50:J56` | 2021 25 · 2022 30 · 2023 35 · 2024 45 · 2025 55 |
| **Szenariomatrix** Kapitalzins (−2…8 %) × Teuerungsrate (−2…8 %) | `M52:S57` | 36 Kapitalwerte |
| Überschuss · Amortisationszeit | `A69`/`B69` · `B88` | |
| Energie- und Stromsteuerblock | Zeilen 90–99 | **in den geprüften Mappen teilweise tot, siehe § 4.4** |

### 3.5 Der zweite Kapitalwert im Kurzbericht `Tab_kurz_KWKG2020`

Dieses Blatt rechnet den Kapitalwert **noch einmal**, mit eigenen Eingaben und eigener Legende:

| Größe | Zelle |
|---|---|
| Zinssatz · Zinsreduktion BHKW · Zuschuss | `B49` · `B50` · `B52` |
| Nutzungsdauer BHKW · spez. Systemkosten | `E49` · `E50` |
| Summe Investitionen KWK / Vergleich · Mehrinvestition | `B54` / `D54` · `B55` |
| Kapitalkosten (Annuität) | `B57` / `D57` |
| Betriebs- und Brennstoffkosten (Mittelwerte über die Nutzungszeit) | `B59`/`D59` · `B60`/`D60` |
| Energiesteuer · Stromsteuer · Einspeisung · vermiedener Bezug | `B63` · `B64` · `B65` · `B66` |
| **KWK-Bonus gesamt / Eigenstrom / Einspeisung** | `B67` · `D67` · `E67` |
| Gesamterlös · Nettokosten | `B68` · `B69` |
| Anlagenart im Klartext · **Vbh-Kontingent** | `A73` · `B74` |
| Überschuss ohne / mit Kapitalkosten | `B76` · `E76` |
| **Amortisationszeit nach Kapitalwert** | `B81` |
| Preissteigerungen Brennstoff / Strom / Wartung | `K105` · `M105` · `O105` |
| Kapitalzins · **Kapitalwert** · interner Zins | `K106` · `M106` · `O106` |
| Jahresreihen 0…15 | Zeilen 108–124 |

**Die Legende in `Q104:V106` ist die einzige Stelle, an der die Altanwendung ihre Kapitalwertformel
ausschreibt** (gelesen, nicht rekonstruiert):

| Zelle | Inhalt |
|---|---|
| `R105` | Ausgaben = Brennstoffkosten + Betriebskosten + BEHG-CO₂-Abgabe |
| `R106` | Erträge = Einnahmen Strom + Einnahmen Wärme + Steuer gesamt (Energiesteuerrückerstattung + …) |
| `V104` | Einsparung E = Erträge − Ausgaben |
| `V105` | abgezinste Einsparung = Einsparung / (1+i)^t |
| `V106` | **Kapitalwert C₀ = Investition + Summe abgezinste Einsparungen** |

**Neuer Befund 18** (zur Liste der früheren Analyse): In derselben Mappe stehen **zwei Kapitalwerte mit
verschiedenen Zahlen**, weil die beiden Blätter verschiedene Preissteigerungen tragen. In
`goetz_test.XLS`: `Tab_Wirtschaftlichkeit_kap!J4` = −72 507,28 € bei 8/8/5 %, dagegen
`Tab_kurz_KWKG2020!M106` = −79 186,87 € bei 0/0/0 %. Das reiht sich in Befund 15 der früheren Analyse
ein („drei Wahrheiten" beim Bonus) und ist für A8 entscheidend: **ohne Festlegung, welches Blatt gilt,
gibt es keine eindeutige Referenzzahl.**

## 4 Die drei Testprojekte

**Hinweis:** Es handelt sich um Kundenprojekte. Unten stehen nur Rechengrößen und Zellbezüge; Namen
jenseits der Dateinamen und Adressen sind weggelassen.

### 4.1 Zwei Blattgenerationen

| Mappe | Blätter | `…_kap` | `Tab_kurz_KWKG2016/2020` | Kapitalwert vorhanden |
|---|---|---|---|---|
| `goetz_test.XLS` | 41 | ja | ja | **zweimal** |
| `englmar hdg.XLS` | 34 | **nein** | **nein** | **nein** |
| `englmar haus der gastlichkeit.XLS` | 34 | **nein** | **nein** | **nein** |
| *(Vergleich)* `BHKW_Höfingen_Erneuerung_20kWel.XLS` | 36 | nein — der Kapitalwertblock liegt **in** `Tab_Wirtschaftlichkeit` (30 Spalten) | ja | ja |

Es gibt also mindestens **drei** Layoutstände. Jede Zahlenprobe muss die Generation der Mappe zuerst
feststellen; das Erkennungsmerkmal ist die Blattliste.

Die beiden `englmar`-Dateien sind **dieselbe Rechnung**: Alle geprüften Ergebniszellen stimmen
zifferngleich überein, `Tab_Kosten!C1` trägt in beiden denselben Projektnamen. Sie zählen als **eine**
Probe.

### 4.2 Gefüllte Eingaben

| Größe | Zelle | `goetz_test.XLS` | `englmar` (beide) |
|---|---|---|---|
| Projektname · Projektdatum | `Tab_Titel!A50` · `C50` | goetz_test · 13.06.2025 | (nur Dateiname) |
| elektrische / thermische Leistung | `Tab_BHKW!F7` · `E7` | **4,0 / 7,5 kW** | **33,0 / 68,1 kW** |
| Modullaufzeit (= Vbh) | `Tab_BHKW!D7` | 573 h/a | 5 178 h/a |
| Stromerzeugung · Wärmeerzeugung BHKW | `Tab_BHKW!C17` · `B17` | 2,3 · 4,3 MWh | 170,9 · 352,6 MWh |
| Jahresnutzungsgrad (Ho) | `Tab_BHKW!C24` | 81,1 % | 94 % |
| KWK-Deckungsanteil Wärme | `Tab_BHKW!C21` | 32,6 % | 64,1 % |
| Betriebsweise | `Tab_BHKW!C23` | wärmegeführt | wärmegeführt |
| Brennstoff BHKW / Spitzenkessel (Hu) | `Tab_Energiebilanz!B11` · `B12` | — | 502,4 · 191,6 MWh |
| Strombedarf · Reststrombezug · Einspeisung · Eigenverbrauch | `Tab_Erloese!F6` · `F7` · `F8` · `F9` | — | 175,49 · 67,12 · 62,50 · 108,38 MWh |
| Bezugsarbeitspreis · Reststrompreis | `Tab_Erloese!B14` · `B16` | — | 0,28 · 0,28 €/kWh |
| Einspeisearbeitspreis | `Tab_Erloese!B36` | 0,06 €/kWh | 0,10 €/kWh |
| Inbetriebnahmedatum | `Tab_Erloese!B33` | 01.01.2020 | **01.01.2024** |
| Zinssatz · Zinsreduktion BHKW | `Tab_Kosten!B3` · `B4` | 1,5 % · 1,15 % | 3,0 % · 0 % |
| Nutzungsdauer BHKW | `Tab_Kosten!D6` | 13,33 a | 10 a |
| Investition BHKW · gesamt | `Tab_Kosten!B6` · `B71` | 17 150 · 20 881,41 € | 48 600 · 64 838,63 € |
| Brennstoff und Preis | `Tab_Kosten!B100`/`D100` bzw. `B98`/`D98` | 1 877,78 l Heizöl à 0,70 €/l | 768 982,6 kWh(Ho) Erdgas à 0,10 €/kWh |
| Wartung BHKW (Erzeugung) | `Tab_Kosten!D81` | **1,00 €/kWh_el** | 0,0322 €/kWh_el |
| Referenzkessel Investition / Nutzungsdauer | `Tab_Kosten!B113` · `D113` | 3 731,41 € · 20 a | 19 288,49 € · 20 a |

`goetz_test.XLS` ist als Sachprojekt **unbrauchbar**: Der Wartungssatz von 1,00 €/kWh_el ist
unrealistisch, und die Prozentsätze der Betriebskosten sind in `Tab_Kosten!D82:D89` schlicht
durchnummeriert (2, 3, 4, 5, 6, 7, 8, 9) — eine Funktionsprüfung, kein Projekt.

### 4.3 Ergebnisse

| Ergebnis | Zelle | `goetz_test.XLS` | `englmar` |
|---|---|---|---|
| Kapitalkosten BHKW / Vergleich | `Tab_Wirtschaftlichkeit!B4` · `C4` | 1 536,08 · 217 | 6 788,89 · 1 296 |
| Betriebskosten | `B5` · `C5` | 6 018,94 · 100 | 5 468,41 · 0 |
| Brennstoffkosten | `B6` · `C6` | 1 314,44 · 990 | 76 898,26 · 31 290 |
| Gesamtkosten | `B8` · `C8` | 8 869,47 · 1 307 | 89 155,56 · 32 586 |
| **Energiesteuerrückerstattung** | `B9` | 47,12 | **3 061,78** |
| **eingesparte Stromsteuer** | `B10` | 8,52 | **2 221,85** |
| Stromeinspeisung + Boni | `B11` | 113 | 24 922 |
| vermiedene Strombezugskosten | `B12` | 798 | 30 346 |
| **Gesamterlös** | `B14` | 966,63 | **60 551** |
| spez. Wärmegestehungskosten BHKW / Vergleich | `B15` · `C15` | 0,599 · 0,099 €/kWh | 0,052 · 0,059 €/kWh |
| **Überschuss p. a. (mit Energiesteuer)** | `A67` | **−6 596 €/a** | **+3 982 €/a** |
| statische Amortisation mit / ohne Energiesteuer | `B87` · `B102` | 9 999 · 9 999 a | **5,3** · 9 999 a |
| **KWKG-Bonus Eigenstrom · Einspeisung · Summe** | `Tab_Erloese!B45` · `D45` · `B46` | 0 · 0 · 0 | **8 670,65 · 10 000,74 · 18 671,39 €/a** |
| Einspeiseerlös aus Arbeit und Leistung | `Tab_Erloese!F42` | 112,62 | 6 250,33 |
| **Kapitalwert (dynamisch)** | `Tab_Wirtschaftlichkeit_kap!J4` | **−72 507,28 €** | Blatt fehlt |
| **Kapitalwert (Kurzbericht)** | `Tab_kurz_KWKG2020!M106` | **−79 186,87 €** | Blatt fehlt |
| Mehrinvestition | `Tab_Wirtschaftlichkeit_kap!G8` | −17 150 € | — |
| Vbh-Kontingent · Anlagenart | `Tab_kurz_KWKG2020!B74` · `A73` | 30 000 h · „neue KWK-Anlagen für KWKG 2020" | Blatt fehlt |

### 4.4 Was die Zahlen belegen — gerechnete Gegenproben

| Prüfung | Rechnung | Ergebnis |
|---|---|---|
| **Annuität ohne Restwert** | 48 600 €, 10 a, 3 % → a = 0,117231 → 5 697,4 | `Tab_Kosten!F6` = 5 697 — **trifft** |
| **Energiesteuer: Basis und Faktor** | 502,4 MWh(Hu) BHKW × 1,108 = 556,66 MWh(Ho) × 5,50 €/MWh = 3 061,6 | `B9` = 3 061,78 — **trifft**; Bemessung nur BHKW und Ho/Hi = 1,108 bestätigt |
| **KWKG-Satz Eigenstrom** | 8 670,65 € ÷ 108,383 MWh = 80,00 €/MWh | **8,00 ct/kWh** — KWKG-2023-Zweig ≤ 50 kW |
| **KWKG-Satz Einspeisung** | 10 000,74 € ÷ 62,503 MWh = 160,00 €/MWh | **16,00 ct/kWh** — ebenso |
| **Erlössumme schließt** | 6 250,33 + 18 671,39 = 24 921,7 · + 30 345,52 + 3 061,78 + 2 221,85 = 60 551,2 | `B11` = 24 922 · `B14` = 60 551 — **trifft** |
| **Stromsteuer mit zwei Mengen** | `B10` 2 221,85 ÷ 20,5 = 108,4 MWh = Eigenverbrauch; `C97` −3 503 ÷ 20,5 = 170,9 MWh = **Erzeugung** | Befund 10 der früheren Analyse **zahlenmäßig belegt** |
| **Bonus ungedeckelt** | 5 178 h/a bei Inbetriebnahme 2024 über dem Jahresdeckel 4 000 h; 10 a × 5 178 h = 51 780 h über dem Kontingent 30 000 h — in `englmar` wirkt beides nicht | Befund 15 bestätigt; die Generation ohne `…_kap` **kennt gar keine Deckelung** |
| **tote Zellen im Kapitalwertblatt** | `goetz_test`: `Tab_Wirtschaftlichkeit_kap!C94` = 919 und `C97` = −842 stehen zifferngleich schon in der **leeren Vorlage** `TABELLEN.XLS`, während `C92`/`C99` daneben projektbezogen gerechnet sind (−9 431,77 / −9 384,65) | Der Steuerblock des Kapitalwertblattes ist **Vorlagenrest**, keine Projektzahl — für A8 gesperrt |
| **zwei Überschüsse** | `Tab_Wirtschaftlichkeit!A67` = −6 596 gegen `Tab_Wirtschaftlichkeit_kap!A69` = −6 597 | Rundungsabweichung; bei A8 ist das maßgebliche Blatt zu benennen |

## 5 Abgleich mit dem EPOS-Konzept

| Rechengröße | Altanwendung (Beleg) | EPOS-Konzept | Urteil |
|---|---|---|---|
| **Kapitalwert** | C₀ = Investition + Σ abgezinste Einsparungen; 15 Jahresspalten, Auswertung „nach Nutzungsdauer BHKW" (`Tab_kurz_KWKG2020!Q104:V106`, `Tab_Wirtschaftlichkeit_kap!F7:W35`) | § 3.1 / `Rechenweg/08`: −I₀ + Σ(E−A)/(1+i)^t + RW_T/(1+i)^T + Einmalzahlung, T aus dem Rahmen | **dieselbe Grundformel, engerer Umfang** |
| Restwert · Ersatzbeschaffung · Startjahr | **gibt es nicht** | § 3.1: Restwert linear, Ersatz bei t = round(start + k·n), Startjahr | **fehlt vollständig** |
| Zeithorizont | Nutzungsdauer BHKW (13,33 a bzw. 10 a), Tabelle bis 15 a | T = 20 a im Beispiel, Rahmenwert je Stammprojekt | **andere Bezugsgröße** |
| **Annuität** | Kapitalwiedergewinnungsfaktor **je Investitionsposten** (`Tab_Kosten`, Spalte F), zwei Zinssätze `B3`/`B4` | a(i,n) = i(1+i)^n/((1+i)^n−1), angewandt auf die **Kapitalwertdifferenz**, ein Zinssatz | **gleiche Formel, andere Ebene** — alt = Kostenannuität, EPOS = Kennzahl aus dem Kapitalwert |
| **Preissteigerung** | **drei** Reihen (Brennstoff `H3`, Strom `J3`, Wartung `L3`) und im Kurzbericht ein zweiter, abweichend gefüllter Satz (`K105`/`M105`/`O105`) | § 3.1: „Genau ZWEI Preissteigerungsreihen — keine je Träger, keine je Position" | **anderer Rechenweg**, alt feiner, aber in sich widersprüchlich |
| **KWKG-Zuschlagssätze** | auf die Gesamtleistung gemittelter Satz, per Knopf gesetzt; ≤ 50 kW **8,00 / 16,00 ct/kWh gemessen** | § 3.6: marginale Staffel Σ Breite_k·Satz_k/P_el; § 7 Abs. 3a 16,00/8,00 ct ≤ 50 kW | **gleiche Marginalidee**; ≤ 50 kW deckungsgleich, > 50 kW alte Rechtslage (Befund 12) |
| **Vbh-Kontingent** | 30 000 h (`Tab_kurz_KWKG2020!B74`); im KWKG-2016-Zweig 60 000 h für ≤ 50 kW | § 3.6: neu 30 000 · modernisiert 30 000/15 000 · nachgerüstet 30 000/15 000/10 000 | **im Kern gleich**; der 60 000-h-Zweig ist überholt (Grundlagen § 5) |
| **Vbh-Jahresdeckel** | `F49:G58`: 5 000 (2020–22) · 4 000 (2023/24) · 3 500 (ab 2025) · 8 760 sonst | § 3.6 `Deckel(A)`, Staffel § 8 Abs. 4 | **gleiche Staffel** — aber nur im `…_kap`-Blatt; die 34-Blatt-Generation kennt ihn nicht |
| Eigenstromtatbestand § 6 Abs. 3 | **gibt es nicht** — nur ein Optionsfeld für die Anlagenart | § 3.6: `KWKG_Eigenstromfall`, `KEINER` ⇒ Satz 0 | **fehlt** |
| **Energiesteuer Gas** | 5,50 €/MWh × Brennstoff **nur BHKW** × 1,108 (`B94`; an `englmar` gegengerechnet) | § 3.7 § 53: Satz × **ungeteilte** Anlagenmenge, brennwertbezogen | **gleich** in Satz, Basis und Abgrenzung |
| **Aufteilung Brennstoff Strom/Wärme** | existiert nicht; die Grenze läuft zwischen BHKW und Kessel | § 3.5 / § 3.7: `VOLLER_BRENNSTOFF` als Vorgabe, `ENERGETISCH` als bewusste Untergrenze | **gleich in der Sache** — Grundlagen § 3.5 („es gibt sie nicht") wird von der Altanwendung gestützt |
| Energiesteuer Öl | 61,35 auf `Menge/10` (`B95`) | Grundlagen § 5: 61,35 € je 1 000 l, § 53a 40,35 | **Einheitenfehler**, Faktor ≈ 10 |
| Energiesteuer Flüssiggas | 4,40 €/MWh (`B93`) | Besteuerung je 1 000 kg, 60,60 voll / 19,60 nach § 53a | **nicht zuordenbar** |
| § 53a · § 54 · 250-€-Sockel | **gibt es nicht** | § 3.7 mit Nutzungsgradschwelle 70 % und Sockel einmal je Lauf | **fehlt** |
| **Stromsteuer** | −20,50 €/MWh (`B97`), **zwei Mengenbasen in derselben Mappe** | § 3.8: 20,50 €/MWh × KwkEigen, vier Bedingungen (Hocheffizienz, 4,5 km, ≤ 2 MW, CO₂ < 270 g/kWh) | **Satz gleich**, Bedingungsprüfung fehlt, Menge uneindeutig |
| § 9b (Netzbezug) | **gibt es nicht** | § 3.8: 20,00 €/MWh × Netzbezug − 250 € | **fehlt** |
| CO₂-Preis | BEHG-Pfad 25/30/35/45/55 €/t, ab 2025 eingefroren (`I50:J56`) | § 3.11 Katalogpfad | gleiche Idee, Pfad eingefroren |
| Umsatzsteuer | Faktor 1,19 hart codiert; `englmar` läuft „ohne MwSt" (`Tab_Wirtschaftlichkeit!A1`) | EPOS rechnet netto | **anderer Rahmen**, je Mappe zu prüfen |
| vermiedene Strombezugskosten | Differenz zweier Tarife (Bezug gegen Reststrom), `Tab_Erloese!F15 − F17` | § 3.5 / Beispielprojekt: Differenz „Bezug ohne Anlage" gegen Restbezug | **gleiche Methode**, andere Datenquelle |
| Photovoltaik | vorab vom Strombedarf abgezogen | Beispielprojekt: eigene Anlage mit Direktvermarktung und Marktprämie | **anderer Rechenweg** |

## 6 Machbarkeit der Zahlenprobe A8

### 6.1 Ausgangslage — A8 ist nicht bei null

`Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md`, Abschnitt „Berechnungserläuterung an der
Höfingen-Mappe", enthält bereits eine **bestandene** externe Gegenprobe gegen eine Altanwendungsmappe:
Mehrinvestition 55 745 €, Jahresüberschuss 10 436 €/a, Nutzungsdauer 13,3 a, i = 2 %, Kapitalwert
**65 259 €** aus `Tab_kurz_KWKG2020`, interner Zinsfuß 20,4 %, dynamische Amortisation 4,33 a. Das
Verfahren steht also, die Referenzzelle ist benannt, und
`Pruefung_Mockups_2026-09-19/01_Nachrechnung.md` Zeilen 291–293 hat die Arithmetik nachgerechnet.

**Die zitierte Mappe selbst ist aber nicht identifiziert.** Auf dem Netzlaufwerk liegen unter
`…\01-WP_WaermePlan\BHKW_WP_Plan\vor_Dialogumbau_zuletzt_AKTUELL_ausgeliefert\Projekte\` **231**
Projektmappen, darunter drei Höfingen-Dateien. Keine davon trägt die zitierten Zahlen:

| Mappe | Mehrinvestition `B55` | Nutzungsdauer `E49` | Zins `B49` | Amortisation `B81` |
|---|---|---|---|---|
| `BHKW_Höfingen_Erneuerung_20kWel.XLS` | 48 495,66 € | 10 a | 2 % | 4,52 a |
| `BHKW_Höfingen_Erneuerung_9kWel.XLS` | 35 845,66 € | 10 a | 2 % | 5,64 a |
| `BHWK_Höfingen_Erneuerung.XLS` | 43 320,66 € | 10 a | 2 % | 5,16 a |
| *zitiert in `Rechenweg/08`* | **55 745 €** | **13,3 a** | 2 % | **4,33 a** |

Das ist der **erste zu schließende Punkt**: Die Referenzmappe muss beschafft oder durch eine andere
ersetzt werden — sonst hängt der bereits dokumentierte Nachweis an einer Quelle, die niemand öffnen kann.

**Blattkarte:** `bhkwplan.py` liefert **keine** — 286 Zeilen, 19 xlwings-UDFs, die ausschließlich auf
übergebenen Arrays rechnen; **kein einziger Blatt- oder Zellbezug** kommt darin vor. Die frühere Analyse
liefert die VBA- und Katalogseite, ebenfalls ohne Blatt- und Zelltafel. Die Tafeln in § 3 und § 6.5
dieses Berichts sind die ersten.

### 6.2 Eignung der drei benannten Testprojekte

| Mappe | Eignung für A8 | Begründung |
|---|---|---|
| `goetz_test.XLS` | **Formelprobe ja, Sachprobe nein** | einzige der drei mit beiden Kapitalwerten und vollständigem Jahresraster; inhaltlich synthetisch (4 kW, 573 h/a, Wartung 1 €/kWh_el, Betriebskostensätze durchnummeriert, KWKG-Bonus 0) |
| `englmar hdg.XLS` / `englmar haus der gastlichkeit.XLS` | **Teilprobe ja, Kapitalwert nein** | echtes Projekt mit belastbaren Zahlen und gemessenen KWKG-2023-Sätzen; Generation **ohne** `Tab_Wirtschaftlichkeit_kap` — es gibt dort keinen Kapitalwert, keine Vbh-Deckelung, keinen Jahresdeckel |
| beide `englmar` zusammen | **eine** Probe | inhaltsgleich |

**Folge:** Mit den drei benannten Dateien allein lässt sich A8 **nicht vollständig** führen. Für die
Stufen „Kapitalkosten, Brennstoff, Energiesteuer, KWKG-Zuschlag" reichen sie; für die Stufe
„Kapitalwert" braucht es eine Mappe der 41- oder 36-Blatt-Generation mit echten Projektzahlen — etwa aus
dem oben genannten Bestand.

### 6.3 Welche Eingaben fehlen

**EPOS fehlt aus der Altmappe** (muss aus ihr übernommen werden):

- Stundenreihen Strombedarf und -erzeugung (`Ganglinie`, `Stromdaten`) — ohne sie liefert EPOS'
  § 9 Abs. 1 Nr. 3 nach § 3.8 den Wert 0 mit Begründung
- getrennte Bezugs-, Reststrom- und Einspeisetarife (`Tab_Tarifstruktur`) samt HT/NT- und
  Sommer/Winter-Zonen
- die Vergleichsheizung als eigenes System — in EPOS über Stamm- bzw. Referenzprojekt abbildbar, aber
  eine bewusste Zuordnung

**Der Altanwendung fehlen** (EPOS-Eingaben ohne Gegenstück; bei der Nachbildung zu neutralisieren):

- Restwert, Ersatzbeschaffung, Startjahr, Einmalzahlung
- KWKG-Stichtag, Anlagenart als Datenfeld, Eigenstromtatbestand § 6 Abs. 3, Kostenanteil
- § 53a, § 54 mit 250-€-Sockel, § 9b, die vier Bedingungen des § 9 Abs. 1 Nr. 3
- Unternehmensart, Hocheffizienz- und 270-g-Prüfung
- Photovoltaik als eigene Anlage mit Direktvermarktung
- **eine** statt drei Preissteigerungsreihen

### 6.4 Was vergleichbar ist und was nicht

| Ergebnisgröße | vergleichbar? | Auflage bzw. Grund |
|---|---|---|
| Kapitalkostenannuität je Posten | **ja, direkt** | Restwert 0, gleiche Nutzungsdauer, ein Zinssatz (Zinsreduktion `B4` = 0) |
| Brennstoffkosten p. a. | **ja** | Ho/Hi-Basis gleichsetzen; die Altanwendung rechnet durchgängig Ho |
| Energiesteuer § 53 | **ja** | gleiche Basis (ungeteilte BHKW-Menge), gleicher Satz, gleicher Faktor 1,108 |
| KWKG-Zuschlag p. a. | **nur ≤ 50 kW** | darüber rechnet die Altanwendung Sätze vor KWKG 2023 |
| vermiedene Strombezugskosten | **bedingt** | gleiche Methode, aber die Altmappe braucht zwei gepflegte Tarife |
| Kapitalwert | **ja, mit Neutralschaltung** | Restwert aus, Ersatz aus, T = Nutzungsdauer BHKW, p_E = p_B = Altwert, Blatt benennen (§ 3.5) |
| Stromsteuer | **nein ohne Vorabentscheid** | die Altanwendung liefert zwei Werte zu verschiedenen Mengen; EPOS rechnet Befreiung statt Erstattung |
| Amortisationszeit | **nein** | die Altanwendung hat drei nebeneinanderstehende Verfahren |
| Emissionen, Primärenergie | **nein** | Altkatalog veraltet, Einheitenbruch mg/g (frühere Analyse § 7.2) |
| umsatzsteuerbehaftete Größen | **nein ohne Prüfung** | Faktor 1,19 hart codiert, je Mappe verschieden geschaltet |

### 6.5 Vorschlag eines Prüfverfahrens

| # | Schritt | Umfang | Inhalt |
|---|---|---|---|
| 1 | **Referenzmappe festlegen** | **S** | Die in `Rechenweg/08` zitierte Höfingen-Mappe beschaffen oder ersetzen; Ersatzkandidaten im Bestand von 231 Mappen. Ergebnis: eine benannte Datei mit Prüfsumme |
| 2 | **Generation feststellen, Zelltafel einfrieren** | **S** | Blattliste lesen; 34/36 Blätter ⇒ ohne eigenes Kapitalwertblatt, 41 ⇒ mit. Zelltafel aus § 3 übernehmen |
| 3 | **Eingabespiegel** | **M** | Altmappe → EPOS-Projekt: Anlage, Nutzungsdauer, Zins, Investitionen je Posten, Betriebskostensätze, Brennstoffpreise, Vbh, Inbetriebnahme, Tarife. Jede Lücke protokollieren, nichts raten |
| 4 | **Neutralschaltung dokumentieren** | **S** | EPOS-Lauf mit Restwert 0, ohne Ersatz, T = Nutzungsdauer, p_E = p_B, § 9b aus, § 9 Nr. 3 als Erstattung, netto. Diese Liste ist Teil des Nachweises, nicht sein Kleingedrucktes |
| 5 | **Stufenprobe statt Gesamtprobe** | **M** | Fünf Teilproben in dieser Reihenfolge, jede mit eigener Toleranz: (a) Kapitalkostenannuität (b) Brennstoffkosten (c) Energiesteuer § 53 (d) KWKG-Zuschlag p. a. (e) Kapitalwert. Hält (a), sind (b)–(e) aussagekräftig |
| 6 | **Erwartete Abweichungen vorab benennen** | **S** | Öl-Einheit, Stromsteuerbasis, ungedeckelter Bonus in der 34-Blatt-Generation, Umsatzsteuer, Sätze > 50 kW, zweiter Kapitalwert — als „erwartete Differenz" führen, nicht als Fehlschlag |
| 7 | **Mengenprobe über die Stundenreihen** | **L** | nur falls gewünscht: `Ganglinie`/`Stromdaten` gegen EPOS' Strommatrix; teuer, weil Tarifzonen und Matrix aufeinander abgebildet werden müssen. **Für A8 nicht erforderlich** |

**Referenzzellen für Schritt 5** (Generation mit 41 Blättern):

| Zweck | Blatt!Zelle |
|---|---|
| Zinssatz · Nutzungsdauer BHKW | `Tab_Kosten!B3` · `Tab_Kosten!D6` |
| Investition gesamt · Kapitalkosten gesamt | `Tab_Kosten!B71` · `Tab_Kosten!F71` |
| Betriebskosten · Brennstoffkosten | `Tab_Kosten!F92` · `Tab_Kosten!F109` |
| Referenzsystem Kapitalkosten | `Tab_Kosten!F113` |
| vermiedene Kosten · Einspeiseerlös | `Tab_Erloese!F27` · `Tab_Erloese!F42` |
| KWKG-Bonus Eigen / Einspeisung / Summe | `Tab_Erloese!B45` · `D45` · `B46` |
| Energiesteuer · Stromsteuer | `Tab_Wirtschaftlichkeit!B9` · `B10` |
| Gesamterlös · Überschuss p. a. | `Tab_Wirtschaftlichkeit!B14` · `A67` |
| **Kapitalwert (dynamisch)** · Mehrinvestition | `Tab_Wirtschaftlichkeit_kap!J4` · `G8` |
| Vbh-Reihen (erreicht / vergütet / kumuliert) | `Tab_Wirtschaftlichkeit_kap!H25:V27` |
| **Kapitalwert (Kurzbericht)** · Vbh-Kontingent · Amortisation | `Tab_kurz_KWKG2020!M106` · `B74` · `B81` |

Generation mit 34 Blättern (`englmar`): kein Kapitalwert; Referenz sind `Tab_Wirtschaftlichkeit!A67`
(Überschuss) und `B87` (statische Amortisation) sowie die Erlöszellen auf `Tab_Erloese`.

**Gesamtaufwand:** Schritte 1–6 zusammen **M**; mit Schritt 7 **L**. Die Sperre „wartet auf Zulieferung
der BHKW-Plan-Excel" (Konzept § 6.3 Nr. 20, § 7 B9) ist **aufgehoben** — die Mappen liegen vor und sind
maschinell lesbar. An ihre Stelle tritt der engere Punkt aus § 6.1.

## 7 Die übrigen Dateien in `20-Development\Wirtschaftlichkeit\`

### 7.1 `db_Struktur_Pricing_Modell.sql` (3 015 B, 02.05.2026)

Sechs Tabellen, je ein Satz:

| Tabelle | Inhalt |
|---|---|
| `pricing_model` | Lookup der Abrechnungsmodelle mit drei Sätzen (Gas: Abrechnung Hs / Berechnung Hi · Brennstoff Hi-basiert · Direktenergie) und den Flags, ob Hi, Hs und Dichte zu pflegen sind |
| `energy_group` | Lookup der VDI-3805-Gruppen mit Sortierschlüssel |
| `unit` | Lookup der Einheiten mit Mengenart `volume` / `mass` / `energy` / `stack_volume` |
| `energy_carrier` | Hauptobjekt Energieträger mit Gruppe, Abrechnungsmodell, Abrechnungseinheit, Hi und Hs je Einheit und Dichte — zwei CHECK-Regeln erzwingen, dass Hs nur beim Gasmodell steht und Hi außer bei Direktenergie immer |
| `energy_conversion` | Umrechnungskanten (Träger, von-Einheit, zu-Einheit, Faktor) mit Merker, ob der Anwender den Faktor geändert hat |
| `energy_price` | versionierte Preise je Träger mit `valid_from`/`valid_to`, Grund-, Arbeits- und Leistungspreis sowie der Basisangabe Hs oder Hi |

**Kein Artefakt der Altanwendung**, sondern der Entwurf, aus dem EPOS' `energy_carrier` hervorgegangen
ist — `Beispielprojekt.md` § 1 nennt diese Tabelle als Quelle im Datenmodell. Für A8 nur mittelbar von
Belang: Die Datei erklärt, warum EPOS Hi und Hs getrennt führt, während die Altanwendung den Faktor
1,108 fest verdrahtet.

### 7.2 `wirtschaftlichkeit.html` (47 489 B) und `wirtschaftlichkeit2.html` (69 380 B), beide 28.03.2026

Beide tragen den Titel „Wirtschaftlichkeitsberechnung", je elf Tabellen und einen Skriptblock. Zweck:
**Mockups des künftigen EPOS-Dialogs** `Form_BhkwWirtschaftlichkeit` (Konzept § 7, Etappe B5), nicht
Unterlagen der Altanwendung. Gliederung in beiden gleich: BHKW-Anlage · Heizungstechnik &
Infrastruktur · Bauliche Maßnahmen & Nebenkosten · Nahwärmenetz · Gesamt-Investition; Betriebskosten mit
„Vollwartung / Wartung BHKW", „Instandhaltung Wärmeerzeugung", „Personal & Verwaltung", „Sonstige
Betriebskosten", „Freie Eingabe (Sonstiges)"; Rahmenfelder Zinssatz, Zuschuss (BaFa/KfW),
Betrachtungszeitraum.

Unterschiede der zweiten Fassung: „Energieträger" heißt dort „Brennstoffkosten", das Feld
**Klimaregion entfällt**, und drei Knöpfe kommen hinzu („Neu berechnen", „Bericht öffnen",
„Eingabedaten"). Die Beschriftung „Vollwartung / Wartung BHKW" ist genau die Asymmetrie, die Konzept
§ 6.3 Nr. 19 als offenen Punkt führt.

### 7.3 `Energiekosten_Designvorschlag.docx` (328 642 B, 02.05.2026)

Nur inventarisiert, nicht geöffnet.

### 7.4 Fortschreibung zu den Katalogen in `BHKWPlan\`

| Datei | Blätter | belegte Zellen | Abgleich mit der früheren Analyse |
|---|---|---|---|
| `DB-TARIF.XLS` (141 824 B, 18.08.2026) | `TarifBezug`, `TarifEinspeisung` | 1 381 / 642 | Struktur wie dort beschrieben (51 bzw. 44 belegte Spalten) |
| `DB-Kraftwerk.XLS` (39 936 B, 18.08.2026) | `Kraftwerk` | 115, 19 Zeilen | frühere Analyse nannte 11 Datensätze; die Mappe trägt jetzt mehr Zeilen — **nicht aufgeklärt** |
| `bhkwplan.py` (9 978 B, 06.10.2023) | — | 286 Zeilen, 19 UDFs | bestätigt § 8 der früheren Analyse; **liefert keine Blattkarte** |

## Nicht geprüft

- **Formeln der XLS-Mappen.** `ExcelDataReader` liefert nur zwischengespeicherte Werte; `NPOI` scheitert
  an der RC4-Verschlüsselung. Alle Formelaussagen sind aus Beschriftungen gelesen, durch Nachrechnen
  belegt oder aus der früheren VBA-Analyse übernommen — ein Formeltext aus einer XLS-Zelle steht in
  diesem Bericht nirgends.
- **VBA-Code beider `XLSM`** — nicht erneut ausgelesen; die frühere Analyse deckt 149 Module ab. Ob sich
  der Code zwischen den Fassungen 25.10.2025 und 23.08.2026 geändert hat, ist **offen**; verglichen wurde
  nur die Streamgröße.
- **Die Blattzahl-Differenz 41 gegen 46** (frühere Analyse) — Vermutung Diagrammblätter, nicht belegt.
- **Inhaltlich nicht ausgewertet:** `Tab_Tarifstruktur`, `Tab_ges`, `Ganglinie`, `Dauerlinie`,
  `Gebaeudedaten`, `Tab_Emissionen`, `Tab_Energieaufwand`, `Tab_Wirtschaftlichkeit_kap_WP`,
  `Tab_kurz_KWKG2016`, `Tab_kurz_WP` sowie die PV-, EEG- und Biogaszweige von `goetz_test.XLS`.
- **Die 231 Projektmappen** unter `…\BHKW_WP_Plan\vor_Dialogumbau_zuletzt_AKTUELL_ausgeliefert\Projekte\`
  wurden nur gelistet; drei Höfingen-Mappen stichprobenhaft gelesen, die übrigen nicht.
- **`Energiekosten_Designvorschlag.docx`** nicht geöffnet.
- **Die beiden HTML-Mockups** wurden über Titel, Überschriften und Gruppenbeschriftungen erfasst, nicht
  Feld für Feld.
- **Kein Abgleich gegen einen tatsächlichen EPOS-Lauf** — dieser Bericht bereitet A8 vor, er führt ihn
  nicht durch.
