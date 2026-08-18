# Analyse der Altanwendung `BHKW-WP-PLAN.XLSM`

**Stand: 18.08.2026.** Referenzdokumentation der Excel-/VBA-Anwendung, die mit
[`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md) abgelöst wird.
Zweck: Die Rechenwege bleiben nachvollziehbar, auch wenn die Originaldatei nicht
zur Hand ist — und es ist festgehalten, **welche Fehler bewusst nicht übernommen
werden**.

Quelle: `Z:\…\20-Development\BHKWPlan\BHKW-WP-PLAN.XLSM` (2,06 MB, 149 VBA-Module,
rund 52.000 Zeilen) und `TABELLEN.XLS` (2,23 MB, 46 Blätter, RC4-verschlüsselt,
Öffnen-Passwort `uep` — es steht im VBA-Code). Beide Originale wurden nur gelesen.

---

## 1 Aufbau der Altanwendung

`TABELLEN.XLS` ist **keine Parametersammlung, sondern die Projektvorlage**: Bei
„Neues Projekt" wird sie kopiert und als `PROJEKTE\<Name>.XLS` gespeichert. Die
Rechenlogik liegt teils im VBA, teils in Tabellenformeln — insbesondere die
KWKG-Deckelung steht **ausschließlich** in Formeln des Blattes
`Tab_Wirtschaftlichkeit_kap`.

Der Tarifkatalog liegt getrennt in `DB-TARIF.XLS` (Blätter `TarifBezug`,
`TarifEinspeisung`, ebenfalls Passwort `uep`). **Diese Datei lag nicht vor.**

Die Mengenaufteilung Bezug/Einspeisung/Eigenverbrauch lief zuletzt über
Python-UDFs aus `C:\Program Files (x86)\BHKW-Plan\bhkwplan.py` (nicht vorhanden);
die maßgebliche lesbare Referenz ist die VBA-Fassung `maketarifcodes` in
`Mod_ErloeseEing`.

---

## 2 Rechenkette

### 2.1 Grundlagen

- **Mehrwertsteuer:** `Tab_Kosten!B210` = „Netto" → Faktor 1,0; „Brutto" → 1,19.
  Der Satz **1,19 ist an über 40 Stellen hart codiert**.
- **Annuität** (`Modul_Bib.Annuitaet`), q = 1 + i/100:
  `a = qⁿ · (q−1) / (qⁿ − 1) · I`
- Zwei Zinsfaktoren: einer mit Zinsreduktion für BHKW-Module, einer für alles
  übrige.
- Alle Vektoren `Single`, Rundung durchgängig `Int(x·10ⁿ + 0,5)/10ⁿ` (VBA-`Int`
  ist Floor — bei negativen Werten also nicht kaufmännisch).

### 2.2 Strommengen

Photovoltaik wird **vorab** vom Strombedarf abgezogen; das im Ergebnisdialog
gezeigte „Strombedarf − PV" ist bereits bereinigt. Danach stundenweise:

```
Strombezug(i)       = max(0, Strombedarf(i) − Stromproduktion(i))
Stromeinspeisung(i) = max(0, Stromproduktion(i) − Strombedarf(i))
Eigenverbrauch(i)   = Strombedarf(i) − Strombezug(i)
```

Aggregiert je Tarifcode; zusätzlich Jahres-, Sommer- und Wintermaxima für die
Leistungspreise.

### 2.3 Drei Tarife

`Tab_Tarifstruktur` hält drei vollständige Preisregelungen: **Bezug** (Tarif ohne
BHKW, Referenz für die vermiedenen Kosten), **Einspeisung** und **Restbezug**
(Tarif mit BHKW — kleinere Abnahme, meist teurer).

```
Arbeit ohne BHKW   = Σ Menge(Bedarf, Zone)   × Bezugs-Arbeitspreis(Zone)   × 1000
Arbeit mit BHKW    = Σ Menge(Restbezug, Zone)× Reststrom-Arbeitspreis(Zone)× 1000
Einsparung Arbeit  = Arbeit ohne BHKW − Arbeit mit BHKW
```

**Leistungspreis — zwei sich ausschließende Modelle, Monatspreis hat Vorrang**
(so auch die Dialogbeschriftung „Neue Eingabe Leistungspreis pro Monat (hat
Vorrang)"):

- *Staffelmodell:* vier Stufen mit kW-Grenzen. Ist der Sommerpreis 0, wird nur
  das Jahresmaximum mit den Winterpreisen gestaffelt, sonst Sommer- und
  Wintermaximum getrennt.
- *Monatsmodell:* Summe über zwölf Monate aus Monatsmaximum × Leistungspreis.
  Monatsgrenzen als feste Stundenindizes.

```
Einsparung Leistung   = Leistung ohne BHKW − Leistung mit BHKW      (regelmäßig negativ!)
Vermiedene Kosten     = Einsparung Arbeit + Einsparung Leistung
Einspeisevergütung    = Σ Menge(Einspeisung, Zone) × Einspeisepreis(Zone) × 1000
```

Der Leistungsanteil ist meist negativ, weil der Reststrom-Leistungspreis über dem
Bezugs-Leistungspreis liegt — daher die −341 € im Beispiel.

**Kennzahlen des Ergebnisdialogs:**

```
Anteil Eigennutzung = Eigenverbrauch / Stromproduktion × 100
spez. vermiedene Kosten = vermiedene Kosten / Eigenverbrauch      [€/kWh]
spez. Einspeiseerlös    = Einspeisevergütung / (Produktion − Eigenverbrauch)
Stromgutschrift         = Einspeisevergütung + vermiedene Kosten
```

**Gegenprobe am Beispiel** (Erzeugung 38 + 34 = 72 MWh): 38/72 = 52,8 → 53 % ✓ ·
3316/38 = 87,3 → 0,087 ✓ · 1028/34 = 30,2 → 0,030 ✓

### 2.4 KWK-Zuschlag

Die Sätze werden per Knopf gesetzt („KWKG-2016" oder „KWKG-2023") und danach frei
editierbar. Beide Routinen bilden einen **auf die Gesamtleistung gemittelten**
Satz:

| P_el | Eigennutzung | Einspeisung |
|---|---|---|
| > 2000 kW | 0 | (0,031·(P−2000) + 0,044·1750 + 0,05·150 + 0,06·50 + 0,08·50)/P |
| > 250 kW | 0 | (0,044·(P−250) + 0,05·150 + 0,06·50 + 0,08·50)/P |
| > 100 kW | 0 | (0,05·(P−100) + 0,06·50 + 0,08·50)/P |
| > 50 kW | (0,03·(P−50) + 0,04·50)/P | (0,06·(P−50) + 0,08·50)/P |
| ≤ 50 kW (2016) | 0,04 | 0,08 |
| ≤ 50 kW (2023) | **0,08** | **0,16** |

**Einzeljahres-Erlös** (Dialog „Ergebnisse der Erlösberechnung") ist **völlig
ungedeckelt** — weder Kontingent noch Jahresdeckel greifen dort:

```
Bonus Einspeisung = (Produktion − Eigenverbrauch) × Satz_Einspeisung × 1000
Bonus Eigennutzung = Eigenverbrauch × Satz_Eigennutzung × 1000
```

**Vollbenutzungsstunden-Budget** aus dem Dialog „Modernisierung und Nachrüstung":

| Anlagenart | Vbh |
|---|---|
| neue KWK ≤ 2 kWel (pauschal) | 60.000 |
| neue KWK ≤ 50 kWel (KWKG 2016) | 60.000 |
| neue KWK ≥ 50 kWel (KWKG 2016) | 30.000 |
| neue KWK nach KWKG 2020 | 30.000 |
| modernisiert ≥ 25 % (frühestens 5 J.) | 15.000 |
| modernisiert ≥ 50 % (frühestens 10 J.) | 30.000 |
| nachgerüstet ≥ 10 / ≥ 25 / ≥ 50 % | 10.000 / 15.000 / 30.000 |

**Die Vergütungsdauer steckt in Tabellenformeln**, nicht im VBA. Über 15 Jahre
gerechnet:

```
erreichte Vbh (Jahr n)  = kumuliert, aus Stromerzeugung / installierter Leistung
vergütete Vbh (Jahr n)  = MIN( Δ erreichte Vbh , Jahresdeckel(IBN-Jahr + n) )
kumuliert vergütet      = MIN( Σ vergütete Vbh , Vbh-Budget )
Bonus (Jahr n)          = Δ kumuliert × P_el × Satz × Anteil
```

Jahresdeckel: vor 2020 → 8.760 h · 2020–2022 → 5.000 · 2023/2024 → 4.000 ·
ab 2025 → 3.500. Ist das Budget erschöpft, entfällt der Zuschlag. Auf das
Betrachtungsjahr umgelegt wird über „Mittelwert pro Jahr über die Nutzungsdauer".

Eine **zweite, unabhängige Begrenzung** existiert für die statische Amortisation:
`Dauer = Vbh-Budget / durchschnittliche Modullaufzeit`; danach wird ohne Bonus
weitergerechnet.

**Pauschale ≤ 2 kWel:** `60.000 × 0,08 = 4.800 €` werden **von der
Investitionssumme abgezogen** (wie ein Zuschuss), nicht als laufender Erlös
geführt; die Bonussätze werden dabei auf 0 gesetzt.

**Das Inbetriebnahmedatum steuert die Satzwahl nicht** — die entsprechenden
Prüfungen sind auskommentiert; es dient nur als Jahreszahl für Jahresdeckel- und
CO₂-Preistabelle.

### 2.5 EEG-Bonus

```
Strom_EEG = (Wärmeproduktion − Fermenterwärme − Wärmeüberschuss)
            × Stromproduktion / Wärmeproduktion
Bonus     = Strom_EEG × Satz × 1000
```

### 2.6 Betriebskosten nach VDI 2067

| Position | Formel | Bemessungsgrundlage |
|---|---|---|
| Wartung BHKW (Betriebsstunden) | MwSt × €/h × Σ Betriebsstunden aller Module | je Modul |
| Wartung BHKW (Erzeugung) | MwSt × €/kWhel × Stromproduktion × 1000 | |
| Instandhaltung BHKW | Investition BHKW × % | **netto** |
| Instandhaltung Heizkessel | Investition Kessel × % | **netto** |
| Instandhaltung Wärmezentrale | (Heizungstechnik + Einbindung + Puffer + Abgasanlage) × % | **brutto** |
| Instandhaltung bauliche Anlagen | (Heizraum + Schornstein + bauliche Maßnahmen + Öllagerung + Gasanschluss) × % | **brutto** |
| Instandhaltung Stromeinspeisung | Investition Stromeinspeisung × % | **brutto** |
| Personalkosten | Investitionssumme × % | **brutto** |
| Steuern/Versicherung/Verwaltung | Investitionssumme × % | **brutto** |
| **Hilfsenergiekosten** | **Brennstoffkosten × %** | Energiekosten |
| Reserveleistung, Sonstiges | Betrag × MwSt | absolut |

**Vorrangregel:** Prozentangabe schlägt Absolutangabe; die Absolutfelder werden
beim Speichern geleert. Absolutwerte werden netto eingegeben, das Nebenfeld zeigt
netto × 1,19.

**Empfehlungsbereiche aus den Dialogbeschriftungen:** Instandhaltung BHKW 3,0–9,0 %,
Heizkessel 1,5–2,5 %, Wärmezentrale 1,8–2,2 %, bauliche Anlagen 1,0–1,5 %,
Stromeinspeisung 1,8–2,2 %, Personal 1,0–4,0 %, Verwaltung 0,8–2,0 %.

### 2.7 Energiesteuer und Stromsteuer

```
Energiesteuer Gas = MwSt × Satz × Gasverbrauch_BHKW × 1,108      (1,108 = Ho/Hi)
Energiesteuer Öl  = MwSt × Satz × Ölverbrauch_BHKW / 10
Energiesteuer Flg = MwSt × Satz × Flüssiggasverbrauch_BHKW
Stromsteuer       = MwSt × Satz × Eigenverbrauch
```

| Feld | Vorgabe | tatsächliche Einheit | Bemessungsmenge |
|---|---|---|---|
| Gas | 5,50 | €/MWh (Ho) | nur BHKW |
| Öl | 61,35 | €/1.000 l | nur BHKW |
| Flüssiggas | 4,40 | €/MWh | nur BHKW |
| Stromsteuer | −20,50 | €/MWh | **Eigenverbrauch**, nicht Erzeugung |

Die Steuern gehen als `+ Energiesteuer − Stromsteuer` in Gewinn und Amortisation
ein; da der Stromsteuerwert negativ vorgegeben ist, wirkt er als Gutschrift.

### 2.8 Gesamtrechnung

```
Kosten KWK     = Kapitalkosten + Betriebskosten + Brennstoffkosten
Einnahmen      = (Einspeisevergütung + vermiedene Kosten + KWK-Bonus + EEG-Bonus) × MwSt
Gewinn         = −Kosten KWK + Einnahmen + Kosten Vergleichsheizung
Gewinn n. St.  = Gewinn + Energiesteuern − Stromsteuer
```

Statische Rückzahlzeit als Fixpunktverfahren mit vierfacher Annuitäts-Iteration.

---

## 3 Parameterwerte aus `TABELLEN.XLS`

**Vollbenutzungsstunden-Jahresdeckel** (nach Inbetriebnahmejahr): 2020–2022 je
5.000 · 2023/2024 je 4.000 · ab 2025 3.500 · vor 2020 8.760 (kein Deckel).
Kumuliertes Budget 30.000 Vbh.

**BEHG-CO₂-Preis** (€/t): 2021 → 25 · 2022 → 30 · 2023 → 35 · 2024 → 45 ·
ab 2025 → 55 · vor 2021 → 0.

**Steuersätze:** Flüssiggas 4,4 · Gas 5,5 · Öl 61,35 · Stromsteuer −20,5.

**Heizwerte:** Erdgas 11,48 kWh/m³ · Flüssiggas 13,77 kWh/kg · Öl 10,08 kWh/l ·
Biogas 6 kWh/m³ · Rapsöl 8,75 kWh/l.

**Preissteigerungen (Beispielprojekt):** Brennstoff 8 % · Strom 8 % · Wartung 5 % ·
Kapitalzins 8 %; Szenariomatrix −2 % bis +8 %.

**Primärenergiefaktoren (EnEV 2016):** Strom 1,8 · Nahwärme 2,8 · Gas/Öl/Flüssiggas
1,1 · Holz 0,2 · Pflanzenöl 0 · BHKW-Stromgutschrift 2,8 · Bio-Erdgas 0,5.

---

## 4 Eingabemasken

Die 95 Dialoge sind VBA-UserForms; Steuerelementnamen und Beschriftungen wurden
aus den Designerdaten extrahiert. Die für das Konzept maßgeblichen:

- **`Dial_ErloesEing`** — Durchschnittspreise (Bezug, Einspeisung, Reststrom),
  Auswahl der Strompreisregelungen, KWK-Bonus (Eigenstrom, Einspeisung,
  Inbetriebnahmedatum, Knöpfe für KWKG-2016/2023, Modernisierung, Pauschale),
  EEG-Block mit Wärmeerzeugung, Überschuss, Stromkennzahl, Fermenterbeheizung.
- **`Dial_ErloesErg`** — das Zielbild: vermiedener Strombezug mit Aufteilung,
  vermiedene Kosten (Arbeit, Leistung, Summe, spezifisch), Stromeinspeisung
  (Winter/Sommer, HT/NT, Jahreserlöse), Jahresboni nach KWKG und EEG.
- **`Dial_KWK_Modernisierung`** — neun Optionsfelder für die Anlagenart mit den
  zugehörigen Vollbenutzungsstunden.
- **`Dial_BetriebKost`** — drei Spalten (Prozent / absolut netto / brutto
  gesperrt), zwölf Positionen, Empfehlungsbereiche in den Beschriftungen.
- **`Dial_KonKosten`** — Energie- und Stromsteuer, Mehrwertsteuerschalter,
  alternative Wärmepreise der Vergleichsheizung.
- **`Dial_Tarifbezug`** mit `Dial_HTNTZeiten` / `Dial_HTNTWoche` — Leistungspreise
  (vier Stufen), monatlicher Leistungspreis mit Vorrang, Arbeitspreise, HT-Zeiten
  je Wochentag.

---

## 5 Befunde: Fehler und Ungereimtheiten der Altanwendung

Diese Punkte werden **nicht** übernommen; die Entscheidungen dazu stehen im
Konzept.

| # | Befund | Wirkung |
|---|---|---|
| 1 | **Öl-Steuerbasis ignoriert den Heizwert** (`Verbrauch/10` statt `/10,08`) | 0,8 % Fehler; Heizwertänderung bleibt wirkungslos |
| 2 | **Einheit „61,35 €/MWh Öl"** ist in Wahrheit €/1.000 l | Faktor ≈ 10 |
| 3 | **Flüssiggassatz 4,40 €/MWh** keinem gesetzlichen Satz zuordenbar | vermutlich mit Erdgas verwechselt |
| 4 | **Umsatzsteuer auf den KWK-Zuschlag** | real nicht umsatzsteuerbar |
| 5 | **Prozentbasis mischt netto und brutto** | im Brutto-Modus weichen Positionen um 19 % voneinander ab |
| 6 | **Wartungsfelder überschreiben sich** (€/kWh gewinnt kommentarlos) | stiller Datenverlust |
| 7 | **„oder Instandhaltung BHKW" wird addiert** | Beschriftung widerspricht der Rechnung |
| 8 | **`spezVerKosten` als €/kW beschriftet**, rechnerisch €/kWh | Einheitenfehler in der Anzeige |
| 9 | **EEG-Formel liest den Satz statt des Jahresbetrags** (Kapitalwertblatt) | Größenordnungsfehler |
| 10 | **Stromsteuer inkonsistent**: Protokollzeile rechnet mit Erzeugung, Wirtschaftlichkeit mit Eigenverbrauch | zwei verschiedene Werte |
| 11 | **Leistungserlös aus Einspeisung fest 0**, obwohl Maske und Blatt Felder dafür haben | tote Funktion |
| 12 | **KWKG-2023-Staffel nur halb umgesetzt** — nur der Zweig ≤ 50 kW unterscheidet sich von 2016 | Sätze > 50 kW veraltet |
| 13 | **Pauschale ≤ 2 kWel widersprüchlich**: Dialog nennt 4 ct/kWh (2.400 €), Code rechnet 4.800 € | Faktor 2 |
| 14 | **Inbetriebnahmedatum ohne Wirkung** auf die Satzwahl | Fassung hängt am gedrückten Knopf |
| 15 | **Zwei parallele Bonus-Begrenzungen** (Einzeljahr ungedeckelt, Kapitalwert gedeckelt, Amortisation dritte Regel) | drei Wahrheiten |
| 16 | **Blattbeschriftungen im Referenz-Brennstoffblock falsch** | „Fernwärme" statt „Sonstige Brennstoffe"/„Strom" |
| 17 | **CO₂-Referenz** nimmt nur den Heizkessel, nicht die getrennte Erzeugung inkl. Kraftwerk | Vergleich zu günstig |

---

## 6 Extraktion — Vorgehen (für spätere Nachprüfungen)

Weder `oletools` (kein Python installiert) noch der COM-Weg über
`Workbook.VBProject` (Vertrauensstellung „Zugriff auf das VBA-Projektobjektmodell"
ist aus) waren verfügbar. Erfolgreich war: `xl/vbaProject.bin` aus der XLSM (ein
ZIP) entnehmen und mit einem selbst geschriebenen MS-CFB-Parser samt
MS-OVBA-Dekompressor auslesen — 149 von 149 Modulen verlustfrei, Codepage 1252.
Aus denselben Streams stammen die Dialogbeschreibungen.

`TABELLEN.XLS` ließ sich nur über Excel-COM mit dem Passwort `uep` öffnen
(OLEDB scheitert an der RC4-Verschlüsselung). Dabei liefert `Range.Value2` die
Werte und `Range.Formula` die Formeln; `Range.Text` ist bei Mehrzellbereichen
nicht nutzbar.

Die zunächst fehlenden Dateien `DB-TARIF.XLS`, `DB-Kraftwerk.XLS` und
`bhkwplan.py` wurden am 18.08.2026 nachgereicht; ihre Auswertung steht in den
Abschnitten 7 und 8.

---

## 7 Stammdatenkataloge

> **Die Zahlenwerte beider Kataloge sind veraltet** (Preisstand teils 1996,
> Emissionsdaten bis 2020) und werden **nicht übernommen**. Ausgewertet wurde die
> **Struktur** — sie bestimmt, welche Felder die neuen Pflegemasken brauchen.

### 7.1 `DB-TARIF.XLS` — Strompreise

`TarifBezug` 55 Spalten, belegt 1–51, **28 Datensätze**; `TarifEinspeisung`
50 Spalten, belegt 1–43, **15 Datensätze**.

**Ein Blatt, zwei Rollen:** `TarifBezug` liefert sowohl den Bezugs- als auch den
Reststromtarif — die Übernahme schreibt mit identischem Mapping nach
`Tab_Tarifstruktur` Zeilen 3–24 beziehungsweise 63–84.

Vier Feldgruppen: Identität (1–3), Leistungspreis-Staffel (4–15), Arbeitspreise
(16–19), Zeitstruktur (20–49, davon **28 Spalten allein für die HT-Fenster je
Wochentag**). Die bis dahin undokumentierten Spalten sind aufgelöst:
**Spalte 50 = monatlicher Leistungspreis** (€/kW·Monat, hat Vorrang vor der
Staffel), **Spalte 51 = Grundpreis** (in allen 28 Sätzen 0, also tot).

Vier Fallen, die beim Nachbau zu vermeiden sind:

| Falle | Erläuterung |
|---|---|
| **Stufen*breiten*, keine Obergrenzen** | „500/1500/6000" bedeutet Grenzen bei 500, 2.000 und 8.000 kW — die Staffelroutine summiert kumulativ |
| **Stufe 4 nie befüllt** | die Speicherzeile ist auskommentiert; die vierte Stufe ist der unbegrenzte Rest |
| **Sommerpreis 0 als versteckter Modellschalter** | dann wird nur das Jahresmaximum mit Winterpreisen gestaffelt — bei 22 von 28 Sätzen der Fall |
| **Währungsfalle** | Kopftexte sagen „DM/kW", die Werte sind Euro (142,139 = 278 DM ÷ 1,95583) |

Beim Einspeisungsblatt sind **Sollleistung und Reduktionsfaktoren leer oder 0**,
und es gibt **keinen aktiven Lesepfad** mehr — beides bestätigt Befund 11 („Leistungserlös
Einspeisung fest 0") von der Datenseite.

**Folge für das Zielmodell:** Mehr als die Hälfte des Blattes entfällt — die 28
HT-Fenster-Spalten, drei der vier Arbeitspreise, Stufe 4 und die
Einspeise-Sollleistung. Zu übernehmen sind Identität, ein Arbeitspreis,
Sommeranfang und -ende (bleiben nötig, weil die Staffel Sommer und Winter
trennt), Grundpreis sowie ein **explizites Leistungsmodell** statt der
Schalterlogik über den Sommerpreis. Zwei Korrekturen gehören in die neue Maske:
Staffelgrenzen als **kumulierte Obergrenze** statt als Breite, und ein Feld
**Gültig ab** — der Altkatalog trägt den Preisstand nur im Beschreibungstext
(„Stand 1.1.96") und überschreibt beim Speichern ersatzlos.

### 7.2 `DB-Kraftwerk.XLS` — Kraftwerkspark

Ein Blatt, 10 Spalten, **11 Datensätze**, Bezugsjahre 1994–2020 (auch die Zeile
„2023" nutzt Daten von 2020). Kein Bezugsjahr-Feld — es steckt nur im
Bezeichnertext.

Abgleich mit dem vorhandenen `Tab_Kraftwerkspark`:

| Größe | Altkatalog | Bestand EPOS-Plan |
|---|---|---|
| Wirkungsgrad | Bruch (0,43) | **Prozent** (42) |
| CO₂ | **mg**/kWh Brennstoff | **g**/kWh Brennstoff — Faktor 1.000 |
| SO₂, NOx | mg/kWh | mg/kWh — identisch |
| CO, Staub | vorhanden | **fehlen** |
| Bezugsjahr, Quelle | fehlen bzw. Fließtext | fehlen |

Der Wertevergleich deckt drei Definitionsprobleme im Altkatalog auf: Der
Wirkungsgrad einer Zeile ist ein **Textwert**; fünf Mixzeilen ab 2016 tragen
Faktoren **je kWh Strom** in einer Spalte, die je kWh **Brennstoff** deklariert
ist; und absolute Emissionsfaktoren stehen neben negativen
Netto-Vermeidungssalden. Zusätzlich ist **Staub im aktiven Altcode tot** — die
Aktualisierung kopiert nur die ersten sieben Spalten, die Referenzrechnung liest
aber weiterhin die nie gefüllte Zelle.

**Folge für das Zielmodell:** keine Wertübernahme. Die Struktur ist additiv zu
ergänzen um `CO`, `Staub`, `GueltigAb`, `Quelle`, `ReadOnly` und vor allem
**`Bezugsbasis`** mit den Werten `BRENNSTOFF` oder `STROM` — das macht den
Definitionsbruch des Altkatalogs strukturell unmöglich und deckt zugleich den
bestehenden Seed-Satz „Deutscher Strommix" ab, der bewusst je kWh Strom definiert
ist.

---

## 8 `bhkwplan.py` — Gegenprobe zur Mengenlogik

Die Datei bestätigt die aus dem VBA rekonstruierten Formeln:
`Strombezug = max(0, Bedarf − Produktion)`,
`Einspeisung = max(0, Produktion − Bedarf)`,
`Eigenverbrauch = Bedarf − Bezug`, je Tarifzone summiert und in MWh geführt.
Ebenso die Tarifcode-Bildung, die Monatsgrenzen als feste Stundenindizes und die
Maxima je Zone.

**Eine Methodenfrage war zu klären:** `py_einsparung_arbeit` rechnet
`Eigenverbrauch × Arbeitspreis` — also mit **einem** Preis —, während der
VBA-Code die **Differenz zweier Tarife** bildet. Welche Methode füllt den
Ergebnisdialog?

**Antwort: die Differenzmethode.** Die Python-Werte werden dreißig Zeilen später
überschrieben:

```vba
einsparung_arbeit(0) = KostenArbeitStrombezug - KostenArbeitReststrombezug
```

und genau dieser Index 0 landet im Feld „Vermiedene Kosten / für die Arbeit".
Bestätigt von der Tabellenseite: `Tab_Erloese` führt die Zeilen „Bezugskosten /
Strombedarfskosten / Reststrompreis / Reststrombezugskosten / vermiedene Kosten".

Die Indizes 1 bis 4 behalten dagegen die Python-Werte und erscheinen so in den
Diagnoseblättern — dort steht die eigenverbrauchsbewertete Zahl, im Dialog die
Differenz. **Referenz für die Zahlenprobe ist der Dialogwert.**
`py_einsparung_arbeit` ist Restbestand der Portierung von der früheren nativen
`BHKWPLAN.DLL`; der zugehörige Altaufruf steht auskommentiert daneben.
