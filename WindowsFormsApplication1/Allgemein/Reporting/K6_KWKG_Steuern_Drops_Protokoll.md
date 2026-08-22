# K6 · KWKG-Tatbestände, einheitenrichtige Steuersätze, CO₂-Preispfad und die Drops (HF6/HF1)

> Schlussetappe K6 des Konzepts „Aktualisierung der Kosten- und Energieträgerstruktur in
> EPOS-Plan" (`Konzept_Kosten_Energietraeger_EPOS-Plan.md`, § 8 = HF6, § 3 = HF1,
> Entscheidungen E3/E4/E5).
> Ausgangsstand `12618a6` (K5b), Schemastand 27.
> Zwei Commits: **K6a** `2c12219` (Migrationsschritt 28 = M-D), **K6b** (Schritt 29 = M-E,
> dieser Commit). Bearbeitet am 20.08.2026.

Kurzfassung: Der KWK-Zuschlag auf **selbst genutzten** Strom hängt jetzt am Tatbestand des
§ 6 Abs. 3, das Vbh-Kontingent leitet sich aus der Anlagenart nach § 8 ab, und die
Pauschale des § 9 ist eine Einmalzahlung im Jahr 0 statt einer Investitionsminderung. Der
CO₂-Preis kommt jahresgenau aus dem Gesetzeskatalog. § 54 EnergieStG ist als dritte
Entlastungsnorm wählbar, mit seinem Sockelbetrag. Zuletzt sind sieben Alttabellen und die
Kategorie-3-Zeilen gefallen.

**Der wichtigste Befund der Etappe steht in Abschnitt 2: Zwei der vier
Auftragspunkte zu HF6 waren bereits erfüllt** — die einheitenrichtigen Steuersätze und der
§-9b-Sockel stehen seit Etappe E4 im Programm. Was fehlte, war etwas anderes.

---

## 1 Schrittnummern und Schemastand

| | |
|---|---|
| Höchster belegter Schritt vor K6 | 27 (`SCHRITT_27_KOMPONENTEN_KATALOG`, K5) |
| **M-D** | **28** — `SCHRITT_28_KWKG_TATBESTAND` (K6a) |
| **M-E** | **29** — `SCHRITT_29_ALTTABELLEN` (K6b) |
| `ZIEL_VERSION` | 27 → **29** (`SchemaMigration.cs:77`) |

Keine Kollision: Die Nummern 28 und 29 waren frei, die Reihenfolge im Feld `SCHRITTE`
folgt der Nummer.

---

## 2 Was schon da war — der Befund vor der ersten Zeile Code

Der Auftrag nannte vier Arbeiten zu HF6. **Zwei davon waren erledigt**, und zwar seit
Etappe E4 (19.08.2026). Das ist keine Nebensächlichkeit: Wer sie „umgesetzt" hätte, hätte
eine zweite Wahrheit über dieselben Zahlen angelegt.

### 2.1 Die Steuersätze stehen bereits in der Gesetzeseinheit (L8)

`GesetzKatalog.Vorbelegung()` führt sie so, wie das Gesetz sie bemisst — nachgemessen am
Code, nicht angenommen:

| Schlüssel | Wert | Einheit | JahrVon | Fundstelle im Code |
|---|---|---|---|---|
| `ENERGIEST_ERDGAS` | 5,50 | €/MWh | 2003 | `GesetzKatalog.cs:792` |
| `ENERGIEST_HEIZOEL_EL` | 61,35 | €/**1.000 l** | 2003 | `:794` |
| `ENERGIEST_FLUESSIGGAS` | 60,60 | €/**1.000 kg** | 2003 | `:798` |
| `ENERGIEST_53A5_HEIZOEL_EL` | 40,35 | €/1.000 l | 2024 | `:808` |
| `ENERGIEST_53A5_FLUESSIGGAS` | 19,60 | €/1.000 kg | 2024 | `:809` |
| `STROMST_ENTLASTUNG_9B` | 20,00 | €/MWh | 2026 | `:774` |
| `STROMST_REGELSATZ` | 20,50 | €/MWh | 2026 | `:773` |

Und `SteuerGutschriftRechner.MengeInGesetzlicherEinheit`
(`SteuerGutschriftRechner.cs:341-393`) rechnet bereits über `eff_hi` und die
Abrechnungseinheit um — genau die K2/K3-Regelkette.

**Folge:** Es wurden **keine** neuen Katalogzeilen mit `JahrVon = 2026` gesät. Sie hätten
dieselben Zahlen ein zweites Mal geführt; die Stichtagsregel
(`GesetzKatalog.WertMitHerkunft`) hätte dann bei gleichem Jahr eine von zwei
gleichwertigen Zeilen gegriffen — eine Dublette ohne fachlichen Gewinn und mit
Verwechslungsgefahr bei der nächsten Novelle. Der Auftragspunkt gilt als **durch E4
erfüllt**, nachgewiesen statt wiederholt.

### 2.2 Der § 9b-Sockel wird bereits abgezogen

`SteuerGutschriftRechner.StromsteuerEntlastung` (`:555-593`) rechnet
`max(0, 20,00 €/MWh × Netzbezug − 250 €/a)` und begründet die Null.
`STROMST_SOCKELBETRAG_9B` steht im Katalog (`GesetzKatalog.cs:776`).

**Offen war der zweite Sockel:** § 54 EnergieStG. Sein Katalogwert existierte
(`ENERGIEST_54_SOCKELBETRAG`, 250 €/a, `:819`), aber § 54 war **nicht wählbar** — es gab
nur `KEINE`, `PARAGRAF_53`, `PARAGRAF_53A`. Ein Sockel ohne Norm zieht nichts ab. Das ist
in K6 nachgeholt (Abschnitt 5).

### 2.3 Die Satzstaffeln des § 7 sind vollständig implementiert

`KwkgSatzRechner` (Etappe E6) rechnet Abs. 1, Abs. 2 (drei Tatbestände) und Abs. 3a — und
zwar mit **marginalen Tranchen**, nicht mit Klassen. Auch die Anlagengrenzen des § 7
Abs. 3a und des § 6 Abs. 3 Nr. 1 stehen im Katalog (Generation 3).

**Folge:** Der Auftragssatz „fehlende Sätze als Katalogzeilen seeden" traf auf **keine
fehlende Zeile**. Was fehlte, war die **Projektangabe**, die entscheidet, ob § 6 Abs. 3
überhaupt erfüllt ist — und genau die ist der Kern von K6a.

### 2.4 `Form_Gesetzesparameter` zeigt den Status schon

Die Liste führt sechs Spalten, darunter `Status`
(`Views/Admin/Form_Gesetzesparameter.cs:119`, gefüllt `:240`). Auftragspunkt 5 „falls noch
nicht" — **war schon**, keine Änderung.

---

## 3 M-D (Schritt 28) — die vier Projektangaben

### 3.1 Kein neuer Wertevorrat: die Konstanten gab es bereits

Das Konzept § 8.1 nennt die Werte klein geschrieben (`keiner`, `anlage_bis_100kw`,
`kundenanlage`, `stromkostenintensiv`; `neu`, `modernisiert`, `nachgeruestet`). Im Code
stehen sie seit Etappe E6 **groß** — als Steuerwerte der Angaben **je Anlage**:

| Konzept | Konstante (Bestand) | Wert |
|---|---|---|
| `keiner` | `DbWerte.KWKG_EIGENFALL_KEINER` | `KEINER` |
| `anlage_bis_100kw` | `KWKG_EIGENFALL_NR1` | `NR1_BIS100KW` |
| `kundenanlage` | `KWKG_EIGENFALL_NR2` | `NR2_KUNDENANLAGE` |
| `stromkostenintensiv` | `KWKG_EIGENFALL_NR3` | `NR3_STROMINTENSIV` |
| `neu` | `KWKG_ANLAGENART_NEU` | `NEUANLAGE` |
| `modernisiert` | `KWKG_ANLAGENART_MODERNISIERT` | `MODERNISIERT` |
| `nachgeruestet` | `KWKG_ANLAGENART_NACHGERUESTET` | `NACHGERUESTET` |

**Bewusst KEINE zweite Reihe angelegt.** Projekt- und Anlagenangabe laufen in denselben
`KwkgSatzRechner`; zwei Wertevorräte für dieselbe Fachfrage wären eine zweite Wahrheit.
Vermerkt an `DbWerte.cs:650-664`.

### 3.2 Die vier Spalten (`Tab_ProjektWirtschaftlichkeit`)

`SchemaKatalog.cs:1413-1477`, `Schritt28_KwkgTatbestand`:

| Spalte | Typ | Bedeutung von „leer" |
|---|---|---|
| `KWKG_Tatbestand` | TEXT(30) | **nicht angegeben** → rechnet wie bisher, mit Hinweis |
| `KWKG_Anlagenart` | TEXT(20) | nicht angegeben → Kontingent-Override bleibt |
| `KWKG_Kostenanteil` | DOUBLE | nicht gepflegt → keine Stufe ableitbar |
| `KWKG_Pauschalmodus` | YESNO | ACE belegt `False` = keine Pauschale |

Breiten: längster Wert `NR2_KUNDENANLAGE` (16) → 30; `NACHGERUESTET` (13) → 20.

### 3.3 Die Entscheidung, an der die Ergebnisneutralität hängt

**Schritt 28 hat KEIN DML auf Projektzeilen** — anders als 19b, 20b, 21b und 23b.

Der Grund ist scharf: Eine Vorbelegung mit `KEINER` wäre der fachlich „richtige" Wert
(§ 7 Abs. 2 gewährt den Eigenstromzuschlag nur in drei Fällen) — und hätte **jedem
Bestandsprojekt mit gepflegtem Eigenstrom-Satz den Zuschlag genommen**, still, in einer
Etappe, die das nicht angekündigt hat. Deshalb drei Zustände statt zwei
(`WirtschaftlichkeitCtrl.cs:1693-1712`):

| `KWKG_Tatbestand` | Wirkung | Meldung |
|---|---|---|
| leer / NULL | Satz bleibt stehen — **wie bisher** | `WIRT_KWKG_TATBESTAND_OFFEN` („ungeprüft") |
| `KEINER` (ausdrücklich) | Eigenstrom-Satz = 0 | `WIRT_KWKG_TATBESTAND_KEINER` (nennt § 7 Abs. 2) |
| `NR1`/`NR2`/`NR3` | Satz bleibt stehen | — |

Dasselbe Muster wie `Biomasse_Nachweis` aus L13: leer bedeutet den bestandswahrenden Wert,
erst die ausdrückliche Angabe ändert etwas.

**Nachgewiesen an der Scratch-Kopie** (Abschnitt 7.1): Nach 28a stehen alle drei
Nicht-YESNO-Spalten auf NULL und `KWKG_Pauschalmodus` auf `False`.

### 3.4 Vbh-Kontingent nach § 8 — `KwkgKontingentRechner` (neu)

`Allgemein/Wirtschaftlichkeit/KwkgKontingentRechner.cs`, 196 Zeilen, reine Funktion mit
Katalog-Delegat (Muster `KwkgSatzRechner`, Leitentscheidung L9).

Formel — **Override zuerst** (`WirtschaftlichkeitCtrl.KontingentDesProjekts`):

```
KWKG_Vbh_Kontingent > 0            → dieser Wert (jede Bestandsdatenbank)
sonst Anlagenart leer              → dieser Wert (also 0, wie bisher)
sonst neu                          → 30.000 h            (§ 8 Abs. 1)
sonst modernisiert, Anteil ≥ 50 %  → 30.000 h            (§ 8 Abs. 2)
                     Anteil ≥ 25 % → 15.000 h
                     Anteil < 25 % →      0 h + Fehlgrund
sonst nachgerüstet,  Anteil ≥ 50 % → 30.000 h            (§ 8 Abs. 3)
                     Anteil ≥ 25 % → 15.000 h
                     Anteil ≥ 10 % → 10.000 h
                     Anteil < 10 % →      0 h + Fehlgrund
```

Alle Zahlen kommen aus dem Katalog (`KWKG_VBH_*`, `KWKG_KOSTENSCHWELLE_*`), nicht aus dem
Code. Die Herleitung erscheint als Hinweiszeile am Ergebnis.

**Ergebnisneutral:** Der Zweig greift nur, wenn das Kontingent 0 **und** die Anlagenart
ausdrücklich erfasst ist. Beides zusammen gibt es in keiner Bestandsdatenbank.

### 3.5 Pauschale § 9 — Einmalerlös im Jahr 0

`WirtschaftlichkeitCtrl.PauschaleReihe`. Betrag:

```
0,04 €/kWh × 60.000 Vbh × P_el[kW]      → bei 2,0 kW: 4.800 €
```

Alle drei Zahlen aus dem Katalog (`KWKG_PAUSCHALE_BIS2KW`, `…_VBH`, `…_GRENZE`).

- Greift die Pauschale, wird `BaueKwkgReihe` **gar nicht erst aufgerufen** — § 9: „damit
  entfällt die Einzelabrechnung".
- Über 2 kW bleibt der Schalter **wirkungslos**, mit Hinweis, der die Leistung und die
  Grenze nennt. Der laufende Zuschlag rechnet weiter.

**Die eine strukturelle Erweiterung, die das nötig machte:** `KapitalwertRechner` summierte
Erlösreihen nur ab Index 1; Index 0 war ungenutzt. Er wird jetzt als **Einmalzahlung im
Jahr 0** ausgewertet (`KapitalwertRechner.cs`, neuer Block vor der Jahresschleife). Additiv
— jede Reihe vor K6 führt dort eine 0, `einmalT0` ist dann 0, und
`BarwertReihe[0] = −Investition` bleibt Zeichen für Zeichen der Wert von vorher.

Bewusst **kein** Abzug von I₀: Die Altanwendung buchte den „PauschBonus" als
Investitionsminderung mit Nutzungsdauer — und erzeugte damit Ersatzbeschaffungen und einen
Restwert auf Geld, das nie ersetzt werden muss (Konzept Anhang A(e)).

Im Novellen-Szenario („KWKG-Bonus entfällt") fällt die Pauschale mit weg — sie ist
derselbe Fördertopf (`IstKwkgReihe`, `WirtschaftlichkeitCtrl.cs`).

---

## 4 CO₂-Preispfad (Entscheidung E5)

### 4.1 Die Seeds — und was an ihnen berichtigt wurde

Der Katalog führte den Pfad bereits, **aber mit den Stützstellen des mittleren
Szenarios**, die das Konzept § 8.3 ausdrücklich verworfen hat („Die Szenarien mittel/hoch
bleiben als dokumentierte Alternativen vermerkt, werden aber **nicht gesät**").

| JahrVon | vorher | **nachher** | Status | Quelle |
|---|---|---|---|---|
| 2021–2025 | 25/30/30/45/55 €/t | unverändert | GESICHERT | BEHG § 10 Abs. 2 |
| 2026 | 65 €/t | unverändert | GESICHERT | EEX-Auktionen 2026, durchgehend am Höchstpreis |
| 2027 | 65 €/t | unverändert | VORLAEUFIG | Kabinettsbeschluss 12.08.2026, Bundestag/Bundesrat stehen aus |
| **2028** | 95 €/t | **80 €/t** | PROGNOSE | **Konzept E5 — konservativ, Marktkommentare 2026; frei editierbar** |
| **2030** | 125 €/t | **entfällt** | — | — |

**Zu 2027:** Der Auftrag nannte 60 €/t (Korridormittel). Gesät ist **65 €/t** — die Zeile
existierte bereits, und der Auftrag sagt „falls schon vorhanden: nicht doppeln". Fachlich
ist 65 auch der bessere Wert: Die Grundlagen-Doku § 8.1 belegt, dass **alle sieben**
Versteigerungen 2026 am Höchstpreis endeten (Nachfrage 13- bis 26-fach überzeichnet), und
der Korridor 2027 ist derselbe. Ein Mittelwert würde eine Preisbildung unterstellen, die
es nicht gibt. **Zur Kenntnis für Philipp** — das ist eine bewusste Abweichung vom
Auftragswortlaut, keine Auslassung.

**Zu 2030:** „konstant ab 2028" ist **eine** Stützstelle. Eine zweite mit 125 €/t
widerspräche ihr, deshalb entfällt sie.

Umgesetzt an zwei Stellen:
- `GesetzKatalog.Vorbelegung()` — für jede frische Datenbank (`GesetzKatalog.cs:837`).
- **Schritt 28b** — für Bestandsdatenbanken, die den alten Seed schon tragen. Die
  generationsweise Nachsaat erreicht sie nicht: Sie legt nur NEUE Zeilen an.

**Die Bedingung prüft Wert UND Quelle** (`… AND [Wert] = 95 AND Quelle LIKE
'%Projektionsbericht%'`). Getroffen wird ausschließlich die unveränderte Seed-Zeile; hat
der Anwender sie gepflegt, bleibt sie stehen — E5 sagt ausdrücklich „frei editierbar".
Bewiesen mit einer Kontrollzeile (Abschnitt 7.1).

### 4.2 Der Rechenweg — und die eine gewollte Ergebnisänderung

`WirtschaftlichkeitCtrl.BaueCo2Reihe` bildet die Abgabe jahresscharf:
`BEHG-Menge[t] × Preis(Förderbeginn + t − 1)`. Der Projektwert `CO2_Preis` ist nur noch der
Override „konstanter Preis".

> **ACHTUNG — die eine bewusste Ergebnisänderung der Etappe.** Bis K6 bedeutete
> `CO2_Preis = 0` **„CO₂-Abgabe aus"**. Ab K6 bedeutet es **„Pfad aus dem
> Gesetzeskatalog"**. Jedes Bestandsprojekt mit 0 — und das ist der Vorgabewert —
> bekommt damit eine BEHG-Abgabe, die es vorher nicht hatte.
>
> So hat es das Konzept in § 8.3 entschieden, und § 10 kündigt für K6 gewollte
> Ergebnisänderungen an. Das Ergebnis weist die Umstellung als Hinweiszeile aus
> (`WIRT_CO2_PFAD`, nennt Anfangsjahr, Endjahr, beide Preise und das Prognosejahr), der
> Dialog nennt sie in der Beschriftung („0 = Pfad") und in einer eigenen Zeile darunter.
> **Wer den alten Zustand will, trägt einen konstanten Preis ein oder leert die
> Katalogklasse.** → Sichtprüfliste, Abschnitt 9.

**Warum `Foerderbeginn` und nicht das Bilanzjahr:** Die Abgabe ist ein *Zahlungsstrom* der
Betriebsjahre und gehört auf dieselbe Zeitachse wie die KWKG- und die drei Steuerreihen
(Regel aus E4). Das Bilanzjahr aus L12 wählt dagegen eine *Methode* der Emissionsbilanz und
darf gerade nicht am Förderbeginn hängen. Zwei Größen, zwei Fragen.

`KapitalwertRechner.Rechne` bekam dafür einen optionalen Parameter `double[] behgJeJahr`.
Ist er `null`, bleibt der Ausgabenausdruck **zeichengleich** der Fassung vor K6 —
insbesondere bleibt `(energieJahr + behgJahr)` eine Klammer (Warnung aus E7). Zwei Zweige
statt einer umgeformten Zeile, genau deshalb.

---

## 5 § 54 EnergieStG mit Sockelbetrag

Neu wählbar: `DbWerte.ENERGIESTEUER_WAHL_54 = "PARAGRAF_54"`.

| | |
|---|---|
| Sätze | Erdgas 1,38 €/MWh · Heizöl EL 15,34 €/1.000 l · Flüssiggas 15,15 €/1.000 kg (Katalog, `ENERGIEST_54_*`) |
| Sockel | **250 €/Kalenderjahr**, abgezogen **vor** dem Ausweis; darunter 0 € mit Begründung |
| Voraussetzung | produzierendes Gewerbe oder Land-/Forstwirtschaft — dieselbe Prüfung wie § 9b (`ProduzierendesGewerbe`, jetzt an EINER Stelle) |
| Formular | 1450, Frist 31.12. des Folgejahres |

**Bewusste Lücke, im Ergebnis ausgewiesen:** § 54 entlastet **Heiz**stoffe. Die
Anlagenliste der Steuerprüfung führt ausschließlich BHKW; Kessel- und Spitzenlastbrennstoff
sind nicht darin. Wer § 54 wählt, bekommt die Entlastung deshalb nur auf den
BHKW-Brennstoff — und die Rechnung sagt das als Begründungszeile
(`STEUER_ENERGIEST_54_BEMESSUNG`), statt eine zu kleine Zahl unkommentiert zu zeigen.
Schweröl und Kohle kennt § 54 nicht; für sie liefert `Energiesteuer54Schluessel` einen
leeren Schlüssel, und die Rechnung meldet „kein Satz zugeordnet".

**Ergebnisneutral:** opt-in. Die Vorbelegung bleibt `KEINE`.

---

## 6 Die Gegenprobe zur Altanwendung — Heizöl, Faktor 10

Der Konzeptauftrag verlangt den Nachweis, dass der Öl-Fehler der Altanwendung
(„61,35 €/MWh") strukturell ausgeschlossen ist. Gemessen an der Scratch-Kopie:
`energy_carrier` führt **Heizöl EL** mit `billing_unit = L` und
`hi_kwh_per_unit = 10` (carrier 56).

Für **1.000 MWh** BHKW-Brennstoff, § 53 (voller Satz 61,35 €/1.000 l):

| | Rechnung | Ergebnis |
|---|---|---|
| **alt** (Einheitenfehler, €/MWh) | 1.000 MWh × 61,35 €/MWh | **61.350 €** |
| **neu** (einheitenrichtig) | 1.000 MWh × 1.000 kWh/MWh ÷ 10 kWh/l = 100.000 l = 100 × 1.000 l; × 61,35 € | **6.135 €** |
| | | **Faktor 10,0 — exakt**, weil `eff_hi` genau 10,0 kWh/l ist |

Nach § 53a Abs. 5 (40,35 €/1.000 l) dieselbe Relation: 4.035 € statt 40.350 €.

Der Rechenweg (`SteuerGutschriftRechner.cs:369-375`) ist der aus E4; K6 hat ihn **nicht
geändert** — die Gegenprobe belegt, dass er stimmt.

---

## 7 Verifikation an der Scratch-Kopie

Kopie von `Referenzlaeufe\Arbeitskopie\Kenndaten.accdb` (17.08.2026, 94 MB), kurzlebige
Einzelprozesse über ACE-OLEDB; dieselben Anweisungen wie im C#-Schritt.

**Zwei Randbefunde zur Kopie:** Sie steht auf **Schemastand 13**, nicht 27 — sie ist der
Referenzlauf-Stand vom 17.08. und nicht mitmigriert. Und sie führt
**`Tab_Gesetzesparameter` nicht**; die Tabelle legt `GesetzKatalog.StelleKatalogSicher`
erst beim ersten Programmzugriff an. Ein durchgehender Lauf 27 → 29 war an ihr deshalb
nicht darstellbar; geprüft wurden — wie in K5 — **die Anweisungen der Schritte selbst**.

### 7.1 Schritt 28 (M-D)

```
28a  KWKG_Tatbestand / KWKG_Anlagenart / KWKG_Kostenanteil / KWKG_Pauschalmodus
     Erstlauf : alle vier angelegt
     Zweitlauf: 4 × „Feld ist bereits in der Tabelle vorhanden"  (ACE 3380)
                → SpaltenAnlegen/Ddl wertet das als Erfolg = idempotent
```

**ACE-Vorbelegung nachgemessen** (1 Parametersatz in dieser Kopie):

| Spalte | Zustand nach 28a |
|---|---|
| `KWKG_Tatbestand` | 1 von 1 **NULL** |
| `KWKG_Anlagenart` | 1 von 1 **NULL** |
| `KWKG_Kostenanteil` | 1 von 1 **NULL** |
| `KWKG_Pauschalmodus` | 1 von 1 **False** |

Genau der Zustand, der die Bestandsrechnung fortführt.

```
28b  CO2-Preispfad
     vor : 2026=65 GESICHERT | 2027=65 VORLAEUFIG | 2028=95 PROGNOSE
           2030=125 PROGNOSE | 2032=110 PROGNOSE (Kontrollzeile „vom Anwender gepflegt")
     Erstlauf : UPDATE = 1 Zeile, DELETE = 1 Zeile
     nach: 2026=65 | 2027=65 | 2028=80 (Quelle „Konzept … E5") | 2032=110 unverändert
     Zweitlauf: UPDATE = 0, DELETE = 0                    → idempotent
```

Die Kontrollzeile 2032 belegt die enge Bindung: Eine vom Anwender gepflegte Prognosezeile
bleibt unangetastet.

### 7.2 Schritt 29 (M-E)

**Objektliste vorher: 109 Tabellen. Nachher: 105.**

| Objekt | Erstlauf | Zweitlauf |
|---|---|---|
| Beziehung `Tab_ProjektTab_Brennstoff_Projekt` | **entfernt** | Tabelle fehlt |
| Beziehung `Tab_Brennstoff_StammTab_Brennstoff_Projekt` | **entfernt** | Tabelle fehlt |
| Beziehung `Tab_KostenKategorieTab_ProjektWerte` | **entfernt** | nicht vorhanden |
| Beziehung `Tab_ProjektWerteTab_KostenKategorie` | nicht vorhanden | nicht vorhanden |
| `Tab_Brennstoff_Projekt` | **DROP ok** | nicht vorhanden |
| `energy_unit` | **DROP ok** | nicht vorhanden |
| `energy_group` | nicht vorhanden | nicht vorhanden |
| `Tab_KostenKategorie` | **DROP ok** | nicht vorhanden |
| `Tab_KWKG_Staffel` | **DROP ok** | nicht vorhanden |
| `Tab_BHKW_neu` | nicht vorhanden | nicht vorhanden |
| `Tab_BHKW_Einf` | nicht vorhanden | nicht vorhanden |
| **Summe** | **4 entfernt, 0 offen** | **0 / 0 — idempotent** |

**Der Beziehungsname zu `Tab_ProjektWerte` war unbekannt** (Konzept § 3.2: „Access-Beziehung
zu `Tab_ProjektWerte` fällt mit dem Drop"). Die Kandidatenliste hat ihn getroffen:
`Tab_KostenKategorieTab_ProjektWerte`, liegend auf `Tab_ProjektWerte` — Access-Konvention
Haupttabelle + Detailtabelle. Ohne diesen Treffer wäre `DROP TABLE Tab_KostenKategorie`
gescheitert und die Tabelle als „manuell" stehen geblieben.

**Kategorie 3 (E3):** In dieser Arbeitskopie **0 Zeilen vorher, 0 gelöscht, 0 nachher**.
Die Kopie ist bereits sauber; in der Produktiv-Datenbank kann die Zahl abweichen — der
Schritt protokolliert sie (`29c: n Kategorie-3-Altzeile(n) … geloescht`).

**Gegenprobe — die aktiven Tabellen stehen unverändert:** `Tab_ProjektWerte`,
`Tab_Kostenprofil`, `Tab_Preisreihe`, `pricing_model`, `energy_conversion`,
`energy_carrier`, `Tab_KostenGruppenKatalog`, `Tab_KostenKomponente`, `Tab_Kostenfaktor`,
`Tab_DBTagV`, `Tab_ProjektTarif`, `Tab_Kraftwerkspark`, `Tab_ProjektWirtschaftlichkeit` —
alle 13 vorhanden.

### 7.3 `EnergieEinheitenPruefung` — argumentiert, nicht gefahren

Der Prüfer aus K2 liest `energy_carrier`, `energy_conversion` und
`energy_project_settings`. **Keine dieser drei Tabellen wird von Schritt 28 oder 29
angefasst** — 28 schreibt nur an `Tab_ProjektWirtschaftlichkeit` und
`Tab_Gesetzesparameter`, 29 droppt `energy_unit`/`energy_group`, die der Prüfer nicht
liest (Beleg: `energy_group` fehlt in dieser Kopie ohnehin schon, und K3 meldete trotzdem
0 Befunde). Der Befundstand kann sich durch K6 also nicht ändern.

**Der Live-Lauf steht trotzdem auf der Sichtprüfliste** (Abschnitt 9) — argumentiert ist
nicht gemessen.

### 7.4 Builds

| Zeitpunkt | Ergebnis |
|---|---|
| nach K6a (Schritt 28, Rechenweg, Dialog) | **exit 0**, 6 Warnungen |
| nach K6b (Schritt 29, Nachzüge) | **exit 0**, 6 Warnungen |

Beide Male exakt die sechs bekannten Altwarnungen (`StromverbraucherStammCtrl.cs:25`,
`KlimaregionStammCtrl.cs:22/23`, `WErzeugerModel.cs:6`, `MDIMainForm.cs:348/359`).
**Keine Diagnose aus einer K6-Datei.**

---

## 8 Code-Nachzüge (K6b)

| Datei:Zeile | Maßnahme |
|---|---|
| `Controller/ProjektDuplizierenCtrl.cs:44-56` | `Tab_KostenKategorie` und `energy_unit` aus der Ausschlussliste entfernt, mit Begründung (die Liste schützt Kataloge vor dem schema-getriebenen Kopierlauf; ein Eintrag für eine gedroppte Tabelle wäre irreführend) |
| `Controller/KostenPositionCtrl.cs:197-217` | Kommentar von `GruppeSichern` auf **E4** umgestellt: `Abfrage_ProjektKostenInvestBetrieb` wird von Hand gelöscht; die Methode bleibt, weil der Gruppenkatalog von `Form_Kosten` und seit K5 von den Gruppenköpfen gelesen wird — er hing nie an der Abfrage |
| `migration.manuell.sql:23-26` | K1-Nachzügler: „Brennstoff_Projekt" aus der Aufzählung der Zuordnungs-IDs gestrichen, mit Datumshinweis. Die beiden Skriptabschnitte `:239` und `:490` waren schon in K1 heraus |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs:35-46` | Doc-Kommentar `KwkgVbhJahresdeckel`: Quelle ist seit E1 `Tab_Gesetzesparameter`, nicht `Tab_KWKG_Staffel`; `KwkgVbhKontingent` als Override dokumentiert |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:2807-2810` | „Der endgültige DROP folgt in M-E" → „Seit K6 ist sie ganz weg — Schritt 29 droppt sie" |

**Repoweite Restsuche nach den sieben Namen:** Außer den obigen Stellen ausschließlich
**Kommentare, Konzept- und Protokolltexte** (`GesetzKatalog.cs:270/413/744`,
`DbWerte.cs:321/1475`, `K1`–`K5`-Protokolle, `W4_*`, `UMSETZUNGSSTAND.md`,
`Konzept_*`-Dokumente). **Historie wird nicht umgeschrieben** — ein Protokoll beschreibt
den Stand seines Tages.

---

## 9 Sichtprüfliste für Philipp

1. **Parameterdialog** (Wirtschaftlichkeit → Parameter, Projekt mit BHKW):
   - Gruppe „BHKW — KWKG 2025" zeigt vier neue Zeilen: *Eigenstrom-Tatbestand*,
     *Anlagenart*, *Anteil Neuherstellungskosten [%]*, Kästchen *Pauschale § 9 KWKG*.
   - Beide Auswahlen stehen bei einem Bestandsprojekt auf **„(nicht angegeben)"**.
   - Das Kontingentfeld heißt jetzt „Vbh-Kontingent gesamt [h] **(0 = automatisch)**".
   - Der Hinweistext unten nennt die Formularnummern **1453 / 1131 / 1135 / 1450** und die
     Frist 31.12. des Folgejahres.
2. **CO₂-Zeile** (Gruppe „Brennstoff — BEHG …"): Feld „CO₂-Preis BEHG [€/t] **(0 = Pfad)**",
   darunter die graue Zeile „CO₂-Preis: Pfad aus Gesetzeskatalog (Prognose ab 2028)" und
   der Knopf **„⚙ Gesetzeskatalog pflegen (CO₂-Preispfad)…"**, der die Maske direkt auf der
   Klasse CO₂-Preispfad öffnet.
3. **Gesetzeskatalog**, Klasse CO₂-Preispfad: Zeilen 2021…2028, die **2028er auf 80 €/t mit
   Status PROGNOSE** und Quelle „Konzept Kosten/Energieträger E5…"; **keine 2030er-Zeile
   mit 125 €/t** mehr. Die Spalte Status ist sichtbar.
4. **Die Ergebnisänderung ansehen** (wichtigster Punkt): Ein Bestandsprojekt mit
   Brennstoff-Erzeuger und `CO₂-Preis = 0` bekommt ab jetzt eine BEHG-Abgabe aus dem Pfad.
   Im Hinweisfeld des Ergebnisses steht die Zeile „CO₂-Preis: jahresgenauer Pfad …". **Ist
   das so gewollt?** Wenn nicht: konstanten Preis eintragen (Override) — dann steht dort
   „konstanter Projektwert … (Override)".
5. **Chat-Frage Energieträger** (`energietraeger_pruefen`): weiterhin **0 Befunde**.
6. **Migration**: Beim nächsten Programmstart auf der Arbeitskopie läuft 13 → 29 durch. Im
   `migration_protokoll.txt` die Zeilen **28a/28b** und **29a/29b/29c** lesen — vor allem
   die Zahl der gelöschten **Kategorie-3-Zeilen** und etwaige „MANUELL"-Marker.
7. **Manuelle Access-Schritte** — unverändert offen, Reihenfolge zählt:
   - Checkliste `K1_Aufraeumung_Protokoll.md` § 6 bzw. Konzept **Anhang B**.
   - Die gespeicherten Abfragen aus Konzept § 3.3 löschen, **darunter neu
     `Abfrage_ProjektKostenInvestBetrieb` (E4)**.
   - Die `energy_unit`-Join-Abfrage (Kandidat `Abfrage_Neues_Kosten_Model`) **war vor den
     Drops zu löschen** — gespeicherte Abfragen blockieren den Drop nicht, sie stehen
     danach nur leer da.
   - Sichtprüfung der mehrdeutigen Objekte `Tab_KostenKategorien` (Plural),
     `Tab_ErgebnisKomponente`, `Tab_ErgebnisMonat`, `Tab_Gebaeude1` — in der Arbeitskopie
     **existiert keines davon**.
   - Danach Komprimieren/Reparieren, Kopie nach `Referenzlaeufe\Arbeitskopie\`
     aktualisieren.

---

## 10 Bewusste Lücken und Offenes

1. **Der 6.000-Vbh-Sonderfall des § 8 Abs. 2 ist NICHT implementiert.** Er gilt ab 10 %
   Kostenanteil, aber ausschließlich für **Dampfsammelschienen-KWK über 50 MW** mit zwei
   Jahren Mindestabstand. EPOS-Plan führt weder eine Anlagenbauart noch Leistungen dieser
   Größenordnung; die Ausschreibungsgrenze des § 8a liegt bei 500 kW und ist ohnehin die
   harte Obergrenze der Förderfähigkeit. Die Stufe wäre toter Code **mit
   Fehlbedienungsgefahr**: Ein modernisiertes 200-kW-BHKW mit 12 % Kostenanteil bekäme
   6.000 Vbh, obwohl ihm nach dem Gesetz nichts zusteht. Der Katalogschlüssel
   `KWKG_VBH_MODERNISIERT_10` bleibt gepflegt (Vollständigkeit des Gesetzesabbilds), wird
   aber nicht gelesen.
2. **Der Mindestabstand zur Inbetriebnahme (§ 8 Abs. 2: 5 bzw. 10 Jahre) wird nicht
   geprüft.** Er bezieht sich auf die Inbetriebnahme der **Alt**anlage; die führt das
   Datenmodell nicht. Die Herleitung sagt das als Vorbehalt.
3. **§ 54 bemisst sich hier auf den BHKW-Brennstoff**, nicht auf Kessel-Heizstoffe
   (Abschnitt 5). Wird ausgewiesen.
4. **Kein Referenzlauf-Vergleich gefahren.** K6 ändert Ergebnisse **gewollt** (CO₂-Pfad),
   ein Byte-Vergleich wäre hier nicht das richtige Werkzeug. Die Abnahme läuft über die
   Sichtprüfliste.
5. **Der Tatbestand je ANLAGE bleibt beim Katalogvorschlag.** Trägt eine Anlage einen
   eigenen Satz (`KWKG_Satz_Eigen`), gilt er unverändert; die Prüfung des § 6 Abs. 3
   greift auf Projektebene und für jede Anlage ohne eigenen Satz. Modulscharfe
   Tatbestände hätte E6 anlegen müssen.
6. **`SPALTE_EA_KWKG_ANLAGENART` und `SPALTE_PW_KWKG_ANLAGENART` heißen beide
   `KWKG_Anlagenart`** — verschiedene Tabellen (`Tab_Energieanlagen` gegen
   `Tab_ProjektWirtschaftlichkeit`), deshalb kein Konflikt, aber beim Lesen von SQL-Text
   leicht zu verwechseln.
7. **Das K6-Zählwerk in `SchemaMigration` wurde in EINEM Block angelegt** (Commit K6a) —
   die drei Zähler des Schritts 29 standen dort also schon vor ihrem Schritt. Inert und
   auf 0.
8. **Die Migration wurde nicht über die Anwendung ausgelöst**, sondern mit identischen
   Anweisungen an der Scratch-Kopie nachgestellt (Abschnitt 7). Der erste echte Lauf
   passiert beim nächsten Programmstart auf der Arbeitskopie.

---

## 11 Konfliktmarker-Sweep

Repoweit auf `<<<<<<<` in `*.cs`, `*.resx`, `*.md`, `*.sql`, `*.csproj` — **am Anfang und
am Ende der Etappe kein echter Treffer**. Die vier Fundstellen sind Prosa in
`K1`/`K2`/`K4`/`K5`-Protokollen, die den Sweep selbst beschreiben. Ausgeschlossen:
`.claude\worktrees\`.

**Encoding:** Alle 13 berührten Dateien sind UTF-8 mit durchgehend CRLF; die `.resx` und
`Resource.Designer.cs` wurden binär gelesen und geschrieben, Zeilenenden vorher und
nachher gezählt (6.347 / 6.341 / 18.275 CRLF, **0 einzelne LF**). Beide `.resx` sind nach
dem Eingriff als XML geparst worden. Die neue Datei `KwkgKontingentRechner.cs` wurde nach
dem Anlegen auf CRLF normalisiert (196 Zeilen). Kein cp1252-Fall in dieser Etappe.

**MyResource:** 37 neue Schlüssel, additiv in `Resource.resx`, `Resource.en-US.resx` und
`Resource.Designer.cs` — je Schlüssel deutsch **und** englisch, kein bestehender Eintrag
angefasst.

---

## 12 Nachtrag 20.08.2026: Abfragenbereinigung (K1-Checkliste, von Philipp beauftragt)

Ausgeführt per ADOX gegen beide Datenbanken, jeweils mit Vorab-Sicherung und Referenz-Check
(kein Überlebender referenziert einen Löschkandidaten; alle Querbezüge lagen intern unter den
Kandidaten: `Abfrage_ProjektKostenKomponenten` → 7× `Abfrage_Kosten_*`, `Abfrage_MaxMin_Vorlauf` →
Max/Min, `Abfrage_KenndatenKuehlung_Max` → `Abfrage_Kuehlung_MaxLast`).

- **Produktiv-DB** (`%USERPROFILE%\source
epos\WP-PLAN\Kenndaten.accdb`, per DSN TEST): 30 → 11 Abfragen,
  **19 gelöscht**; `Abfrage_ProjektKostenEnergie` und `Abfrage_ProjektKostenInvestBetrieb` (E4) existierten dort nicht.
- **Arbeitskopie** (`Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`): 36 → 15, **21 gelöscht** (inkl. E4).
- **Befund + Reparatur:** Der Produktiv-DB fehlte die AKTIVE `Abfrage_Energietraeger_Effektiv` (Code liest sie an
  4 Stellen; die Migration legt sie nicht an, nur Kommentar `SchemaKatalog.cs:1356`). Definition aus der
  Arbeitskopie übertragen (View, 356 Zeichen), Probelesen: 8 Zeilen. **Empfehlung:** Anlage der Abfrage als
  Migrationsschritt nachrüsten, damit frische DBs sie sicher führen.
- **Nicht gelöscht** (nicht auf der beschlossenen Liste, Kandidaten für eine eigene Runde): `Abfrage1`,
  `Abfrage2`, `Tab_BHKW_Einfügen_Test` (nur Arbeitskopie); `Tab_StromganglinieDaten Abfrage` (Herkunft unklar).
- Sicherungen: `WP-PLAN\Kenndaten.accdb.vor_2026-08-20_Abfragenbereinigung.bak` bzw.
  `Documents\WP-Plan_DB-Sicherungen\` (aus dem Repo-Baum herausgehalten).
