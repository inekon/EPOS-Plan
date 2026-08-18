# W4 · Etappe E1 — Katalog gesetzlicher Parameter

**Stand: 18.08.2026.** Umsetzung der Etappe **E1** aus
[`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md), Abschnitt 7.
Faktenbasis aller Werte:
[`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md)
(Rechtsstand 18.08.2026, Quellen dort abgerufen).

**Ergebnis in einem Satz:** Die Parameter sind pflegbar, die Lesefassade steht, die
`Tab_KWKG_Staffel` ist wertgleich überführt — und **kein einziger gerechneter Wert hat
sich geändert** (A/B-Referenzlauf 8/8 PASS, 194/194 CSV byte-identisch).

---

## 1 Was umgesetzt wurde

| # | Gegenstand | Datei : Zeile |
|---|---|---|
| 1 | Tabelle `Tab_Gesetzesparameter`, angelegt und eingesät über das `StelleKatalogSicher`-Muster | `Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs:374-450` (`StelleKatalogSicher`) |
| 2 | Seed mit **182 Zeilen** über **135 Schlüssel** in **8 Klassen** | `GesetzKatalog.cs:461-841` (`Vorbelegung`) |
| 3 | Lesefassade `Wert` / `WertMitHerkunft` / `Reihe` / `AlleDerKlasse` / `Klassen` / `Neuladen` | `GesetzKatalog.cs:122-201` |
| 4 | Schreibweg der Maske: `Anlegen` / `Aendern` / `Loeschen` | `GesetzKatalog.cs:281-341` |
| 5 | Konstanten der Persistenzschicht (161 Stück: 8 Klassen, 3 Status, 15 Einheiten, 135 Schlüssel) | `Allgemein/DbWerte.cs:507-838` |
| 6 | Aufhängung des Katalogs im Bestandspfad | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:236-239` |
| 7 | Überführung `Tab_KWKG_Staffel` → Katalogschlüssel `KWKG_VBH_JAHRESDECKEL` | `WirtschaftlichkeitCtrl.cs:947-979` (`LadeKwkgStaffel`), Lookup unveraendert `:981-988` |
| 8 | Pflegemaske „Gesetzliche Parameter" mit Klassenfilter, Anlegen, Ändern, Löschen | `Views/Admin/Form_Gesetzesparameter.cs:33-442` |
| 9 | Zeilendialog mit fester Einheiten- und Statusauswahl | `Views/Admin/Form_Gesetzesparameter.cs:444-705` |
| 10 | Menüeintrag Administration → „Gesetzliche Parameter…" | `MDIMainForm.cs:38-39, 42-75` (`InitGesetzeMenue`) |
| 11 | 36 Ressourcenschlüssel in beiden Sprachen samt Designer und Katalognachtrag | `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`, `Allgemein/Simulation/Lokalisierung_Katalog.md` |

**Nicht angefasst:** `Allgemein/Simulation/*` — `git diff --name-only` liefert dort null
Treffer. Ebenso unberührt bleibt `Tab_KWKG_Staffel` selbst: Die Tabelle wird von
`StelleTabellenSicher` weiterhin angelegt und eingesät, nur nicht mehr **gelesen**.

---

## 2 Entwurfsentscheidungen

### 2.1 `StelleKatalogSicher` statt Migrationsschritt — und warum das nicht kollidiert

Der Auftrag verlangte die Prüfung, ob das Muster mit der Migrationsarchitektur kollidiert.
**Es kollidiert nicht**, und zwar aus einem inhaltlichen Grund:

`SchemaMigration` ist für das zuständig, was **bestehende Zeilen** anfasst — neue Spalten
in Projekttabellen, Vorbelegungen, Beziehungen, Löschweitergaben (`SQL_CREATE_SPVARIANTE`
+ `SpVarianteTabelle`: Tabelle hart, Index und Fremdschlüssel weich). Dort ist der
Schemastand die Klammer, die sicherstellt, dass ein Schritt genau einmal und in der
richtigen Reihenfolge läuft.

`Tab_Gesetzesparameter` hat davon nichts: keine Spalte in einer Bestandstabelle, kein
Fremdschlüssel, kein Bezug auf Projektdaten, keine Reihenfolgeabhängigkeit. Sie ist eine
**reine Zusatztabelle mit Auslieferungsinhalt** — dieselbe Gattung wie
`Tab_KWKG_Staffel`, die aus genau diesem Grund schon heute über
`WirtschaftlichkeitCtrl.StelleTabellenSicher:151-191` entsteht. Das Muster ist
idempotent (CREATE nur bei fehlender Tabelle, Seed nur bei `COUNT(*) = 0`) und damit
gegen Doppelstart und gegen einen abgebrochenen ersten Versuch gleichermaßen sicher.

Der praktische Ausschlag: Eine Bestandsinstallation bekommt die Werte **beim nächsten
Start**, ohne dass jemand eine Migration anstößt oder der Schemastand hochgezogen wird —
und ohne dass ein Referenzlauf-Vergleich sich um eine Schemastandsänderung kümmern muss.

Aufgehängt ist der Aufruf am Ende von `WirtschaftlichkeitCtrl.StelleTabellenSicher`
(eigener Fang, eigene Verbindung — ein Fehlschlag darf die Tabellen darüber nicht
gefährden) und zusätzlich im Konstruktor der Pflegemaske.

### 2.2 Der entfallene Satz ist eine Jahreszeile ohne Wert

`Wert DOUBLE` ist NULL-fähig, und genau das trägt L12. Zum 01.01.2027 entfällt der
Verdrängungsstrommix **ersatzlos**. Drei Modellierungen standen zur Wahl:

| Variante | Folge |
|---|---|
| keine 2027-Zeile | Die Stichtagsregel führt die 860 g/kWh bis in alle Ewigkeit fort — **falsch** |
| 2027-Zeile mit Wert 0 | 0 g CO₂-Äq/kWh ist eine Gutschrift von 100 % — **noch falscher** |
| **2027-Zeile ohne Wert** | `Wert()` liefert `null`, `WertMitHerkunft()` liefert die Zeile mit Quelle „entfällt ersatzlos, Bewertung nach DIN EN 15316-4-5" |

Gewählt ist die dritte. Sie unterscheidet zugleich zwischen „nicht gepflegt" (keine Zeile,
`WertMitHerkunft` liefert `null`) und „bewusst entfallen" (Zeile vorhanden, Wert `null`) —
der Aufrufer kann im Bericht das eine vom anderen unterscheiden. Betrifft
`EF_NACHWEIS_VERDRAENGUNGSSTROMMIX` und `PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX`.

### 2.3 Kein Treffer ergibt `null`, nie 0

Dieselbe Regel wie die Nullpreis-Regel in `KostenEmissionRechner.cs:238-266` (Befund D5):
Ein nicht gepflegter Satz darf sich nicht als „kostenlos" durch die Rechnung schleichen.
`Wert()` gibt deshalb `double?` zurück, und die Aufrufer der Etappen E4 bis E6 **müssen**
den Fall behandeln.

### 2.4 Unveränderte Faktoren bekommen keine 2027-Zeile

Anlage 9 und Anlage 4 ändern sich zum 01.01.2027 nur teilweise. Für die unveränderten
Faktoren (Heizöl 310, Erdgas 240, Holz 20, Fernwärmewerte, die fossilen PEF) gibt es
**eine** Zeile ab 2020, deren Quelle ausdrücklich „GEG/GModG Anlage 9 — durch das GModG
unverändert" lautet. Eine wortgleiche Dublette ab 2027 wäre Rauschen, das den Blick auf
die zwölf tatsächlichen Änderungen verstellt. Wer wissen will, welche Fassung gilt, liest
die Quelle der Zeile.

### 2.5 Zwei Faktorensätze, an drei Stellen getrennt (L11)

1. **Klasse** — `EF_NACHWEIS` gegen `EF_BILANZ`, in der Maske zwei getrennte Bereiche mit
   ausdrücklich unterscheidenden Anzeigenamen („gesetzlicher Nachweis" / „reale Bilanz").
2. **Schlüsselpräfix** — `EF_NACHWEIS_*` gegen `EF_BILANZ_*`; der Harness prüft, dass
   Präfix und Klasse in jeder Seed-Zeile zusammenpassen (Probe A10c).
3. **Größenordnung** — der Harness weist nach, dass Nachweiswert (100 g/kWh ab 2027) und
   reale Bilanz (406 g/kWh mit Vorkette) um mehr als Faktor 3 auseinanderliegen
   (Probe A10b). Wer die beiden je auf dieselbe Variable legt, fällt hier auf.

### 2.6 Einheitenliste — fünf Einheiten mehr als im Auftrag

Der Auftrag nennt `EUR/MWh`, `EUR/1000l`, `EUR/1000kg`, `ct/kWh`, `g/kWh`, `EUR/t`, `h`,
`Prozent`, `Jahr`, `-`. Fünf der geforderten Seed-Werte passen in keine davon; sie in eine
vorhandene zu zwängen wäre genau der Einheitenfehler, den L3 verhindern soll:

| ergänzt | wofür | Beispiel |
|---|---|---|
| `kW` | Leistungsgrenzen | Leistungsstufen 50/100/250/2000 kW, Stromsteuer-Nennleistungsgrenze 2.000 kW |
| `km` | räumlicher Zusammenhang | 4,5 km nach § 12b StromStV |
| `EUR/a` | Sockelbeträge | 250 €/Kalenderjahr nach § 9b StromStG und § 54 EnergieStG |
| `EUR/GJ` | Kohle nach § 53a Abs. 5 | 0,16 €/GJ |
| `GJ/MWh` | Hi/Ho-Umrechnung der EBeV | 3,2508 GJ/MWh |

Die Liste steht als `DbWerte.GESETZ_EINHEIT_*` und ist im Zeilendialog eine feste
Auswahl — Freitext gäbe es sonst „EUR/MWh" und „€/MWh" nebeneinander.

### 2.7 Der Stichtag als Jahreszahl

`KWKG_STICHTAG_DAUERBETRIEB` steht als **2026 mit Einheit `Jahr`**, nicht als Datum
31.12.2026. Grund: Das Feld `Wert` ist `DOUBLE`; ein Datum darin wäre eine
OLE-Automation-Zahl und damit für einen Menschen in der Pflegemaske unlesbar. Der
Gesetzeswortlaut („Aufnahme des Dauerbetriebs bis zum 31.12.") ist ohnehin
jahresbezogen; die Quelle der Zeile sagt es ausdrücklich. Ebenso
`KWKG_REALISIERUNGSFRIST` = 4 Jahre.

---

## 3 Überführung `Tab_KWKG_Staffel` — wertgleich

Die acht Zeilen des Jahresdeckels stehen jetzt als Schlüssel `KWKG_VBH_JAHRESDECKEL` im
Katalog. `WirtschaftlichkeitCtrl.LadeKwkgStaffel:970` liest über
`new GesetzKatalog().Reihe(...)`; `StaffelDeckel` ist **unverändert** geblieben, damit die
Lookup-Semantik nachweisbar dieselbe ist. Die Alttabelle bleibt stehen und wird von
`StelleTabellenSicher` weiterhin gepflegt — sie wird nur nicht mehr gelesen.

**Ein Unterschied in den Rohdaten, keiner im Ergebnis.** Die Alttabelle beginnt bei
`JahrVon = 2020`, der Katalog bei `JahrVon = 2021` — so steht es im Gesetz (§ 8 Abs. 4,
Grundlagen Abschnitt 1.4). Auf den Lookup wirkt sich das nicht aus, weil `StaffelDeckel`
mit dem Wert der **ersten** Zeile beginnt und ihn erst ab dem passenden Jahr überschreibt.
Nachgewiesen Jahr für Jahr von 2015 bis 2045:

| Jahr | alt (`Tab_KWKG_Staffel`) | neu (Katalog) | | Jahr | alt | neu |
|---|---|---|---|---|---|---|
| 2015 | 5000 | 5000 | | 2026 | 3300 | 3300 |
| 2016 | 5000 | 5000 | | 2027 | 3100 | 3100 |
| 2017 | 5000 | 5000 | | 2028 | 2900 | 2900 |
| 2018 | 5000 | 5000 | | 2029 | 2700 | 2700 |
| 2019 | 5000 | 5000 | | 2030 | 2500 | 2500 |
| 2020 | 5000 | 5000 | | 2031 | 2500 | 2500 |
| 2021 | 5000 | 5000 | | 2032 | 2500 | 2500 |
| 2022 | 5000 | 5000 | | … | … | … |
| 2023 | 4000 | 4000 | | 2045 | 2500 | 2500 |
| 2024 | 4000 | 4000 | | | | |
| 2025 | 3500 | 3500 | | | | |

**31 von 31 Jahren wertgleich, Abweichungen: 0** (Harness-Probe B2, ausgeführt gegen die
echten `LadeKwkgStaffel`/`StaffelDeckel` per Reflection).

---

## 4 Die Vorbelegung

**182 Zeilen, 135 Schlüssel, 8 Klassen.** Verteilung:

| Klasse | Zeilen | Inhalt |
|---|---|---|
| `KWKG` | 49 | Zuschlagssätze eingespeist und eigengenutzt, Leistungsstufen, Vbh-Kontingente, Kostenschwellen, Mindestalter, Jahresdeckel, Kleinanlagenpauschale, Fristen |
| `EF_BILANZ` | 36 | UBA-Strommix 2020–2025 (drei Reihen), EBeV-Brennstofffaktoren, BAFA-Werte |
| `EF_NACHWEIS` | 30 | Anlage 9 beider Fassungen |
| `PEF_NACHWEIS` | 29 | Anlage 4 beider Fassungen |
| `CO2_PREIS` | 15 | nEHS-Reihe 2021–2030, Korridore, Nachverkauf, Nachkauf |
| `ENERGIESTEUER` | 15 | Regelsätze § 2, Entlastung § 53a Abs. 5, Entlastung § 54 |
| `STROMSTEUER` | 7 | Regelsatz, Entlastung § 9b, Sockelbetrag, Grenzen der Befreiung |
| `UMSATZSTEUER` | 1 | Regelsatz 19 % |

Vollständige Liste:

| # | Schlüssel | Klasse | JahrVon | Wert | Einheit | Status | Quelle |
|---|---|---|---|---|---|---|---|
| 1 | `KWKG_ZUSCHLAG_EINSPEISUNG_BIS50KW` | KWKG | 2020 | 8 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 1 — eingespeister KWK-Strom, bis 50 kW |
| 2 | `KWKG_ZUSCHLAG_EINSPEISUNG_BIS100KW` | KWKG | 2020 | 6 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 1 — eingespeister KWK-Strom, über 50 bis 100 kW |
| 3 | `KWKG_ZUSCHLAG_EINSPEISUNG_BIS250KW` | KWKG | 2020 | 5 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 1 — eingespeister KWK-Strom, über 100 bis 250 kW |
| 4 | `KWKG_ZUSCHLAG_EINSPEISUNG_BIS2MW` | KWKG | 2020 | 4,4 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 1 — eingespeister KWK-Strom, über 250 kW bis 2 MW |
| 5 | `KWKG_ZUSCHLAG_EINSPEISUNG_UEBER2MW` | KWKG | 2020 | 3,4 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 1 — eingespeister KWK-Strom, über 2 MW, neu/modernisiert |
| 6 | `KWKG_ZUSCHLAG_EINSPEISUNG_UEBER2MW_NACHGERUESTET` | KWKG | 2020 | 3,1 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 1 — eingespeister KWK-Strom, über 2 MW, nachgerüstet |
| 7 | `KWKG_ZUSCHLAG_NEU_BIS50KW_EINSPEISUNG` | KWKG | 2020 | 16 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 3a — neue Anlagen bis 50 kWel, geht Abs. 1 und 2 vor, eingespeist |
| 8 | `KWKG_ZUSCHLAG_NEU_BIS50KW_EIGEN` | KWKG | 2020 | 8 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 3a — neue Anlagen bis 50 kWel, geht Abs. 1 und 2 vor, nicht eingespeist |
| 9 | `KWKG_ZUSCHLAG_EIGEN_N1_BIS50KW` | KWKG | 2020 | 4 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 1 (Anlagen bis 100 kW), bis 50 kW |
| 10 | `KWKG_ZUSCHLAG_EIGEN_N1_BIS100KW` | KWKG | 2020 | 3 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 1 (Anlagen bis 100 kW), 50 bis 100 kW |
| 11 | `KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW` | KWKG | 2020 | 4 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 2 (Kundenanlage/geschl. Verteilernetz) |
| 12 | `KWKG_ZUSCHLAG_EIGEN_N2_BIS100KW` | KWKG | 2020 | 3 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 2 (Kundenanlage/geschl. Verteilernetz) |
| 13 | `KWKG_ZUSCHLAG_EIGEN_N2_BIS250KW` | KWKG | 2020 | 2 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 2 (Kundenanlage/geschl. Verteilernetz) |
| 14 | `KWKG_ZUSCHLAG_EIGEN_N2_BIS2MW` | KWKG | 2020 | 1,5 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 2 (Kundenanlage/geschl. Verteilernetz) |
| 15 | `KWKG_ZUSCHLAG_EIGEN_N2_UEBER2MW` | KWKG | 2020 | 1 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 2 (Kundenanlage/geschl. Verteilernetz) |
| 16 | `KWKG_ZUSCHLAG_EIGEN_N3_BIS50KW` | KWKG | 2020 | 5,41 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 3 (stromkostenintensiv) |
| 17 | `KWKG_ZUSCHLAG_EIGEN_N3_BIS250KW` | KWKG | 2020 | 4 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 3 (stromkostenintensiv), 50 bis 250 kW |
| 18 | `KWKG_ZUSCHLAG_EIGEN_N3_BIS2MW` | KWKG | 2020 | 2,4 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 3 (stromkostenintensiv) |
| 19 | `KWKG_ZUSCHLAG_EIGEN_N3_UEBER2MW` | KWKG | 2020 | 1,8 | ct/kWh | GESICHERT | KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 3 (stromkostenintensiv) |
| 20 | `KWKG_LEISTUNGSSTUFE_1_KW` | KWKG | 2020 | 50 | kW | GESICHERT | KWKG 2025 § 7 — Obergrenze der Leistungsklasse |
| 21 | `KWKG_LEISTUNGSSTUFE_2_KW` | KWKG | 2020 | 100 | kW | GESICHERT | KWKG 2025 § 7 — Obergrenze der Leistungsklasse |
| 22 | `KWKG_LEISTUNGSSTUFE_3_KW` | KWKG | 2020 | 250 | kW | GESICHERT | KWKG 2025 § 7 — Obergrenze der Leistungsklasse |
| 23 | `KWKG_LEISTUNGSSTUFE_4_KW` | KWKG | 2020 | 2000 | kW | GESICHERT | KWKG 2025 § 7 — Obergrenze der Leistungsklasse |
| 24 | `KWKG_VBH_NEUANLAGE` | KWKG | 2020 | 30000 | h | GESICHERT | KWKG 2025 § 8 Abs. 1 — neue Anlagen |
| 25 | `KWKG_VBH_MODERNISIERT_10` | KWKG | 2020 | 6000 | h | GESICHERT | KWKG 2025 § 8 Abs. 2 — modernisierte Anlagen, ab 10 % (nur Dampfsammelschienen-KWK > 50 MW) |
| 26 | `KWKG_VBH_MODERNISIERT_25` | KWKG | 2020 | 15000 | h | GESICHERT | KWKG 2025 § 8 Abs. 2 — modernisierte Anlagen, ab 25 % |
| 27 | `KWKG_VBH_MODERNISIERT_50` | KWKG | 2020 | 30000 | h | GESICHERT | KWKG 2025 § 8 Abs. 2 — modernisierte Anlagen, ab 50 % |
| 28 | `KWKG_VBH_NACHGERUESTET_10` | KWKG | 2020 | 10000 | h | GESICHERT | KWKG 2025 § 8 Abs. 3 — nachgerüstete Anlagen, 10 bis unter 25 % |
| 29 | `KWKG_VBH_NACHGERUESTET_25` | KWKG | 2020 | 15000 | h | GESICHERT | KWKG 2025 § 8 Abs. 3 — nachgerüstete Anlagen, 25 bis unter 50 % |
| 30 | `KWKG_VBH_NACHGERUESTET_50` | KWKG | 2020 | 30000 | h | GESICHERT | KWKG 2025 § 8 Abs. 3 — nachgerüstete Anlagen, ab 50 % |
| 31 | `KWKG_KOSTENSCHWELLE_10` | KWKG | 2020 | 10 | Prozent | GESICHERT | KWKG 2025 § 8 Abs. 2/3 — Anteil an den Neuherstellungskosten |
| 32 | `KWKG_KOSTENSCHWELLE_25` | KWKG | 2020 | 25 | Prozent | GESICHERT | KWKG 2025 § 8 Abs. 2/3 — Anteil an den Neuherstellungskosten |
| 33 | `KWKG_KOSTENSCHWELLE_50` | KWKG | 2020 | 50 | Prozent | GESICHERT | KWKG 2025 § 8 Abs. 2/3 — Anteil an den Neuherstellungskosten |
| 34 | `KWKG_MINDESTALTER_10` | KWKG | 2020 | 2 | Jahr | GESICHERT | KWKG 2025 § 8 Abs. 2 — Mindestabstand zur Inbetriebnahme (Schwelle 10 %) |
| 35 | `KWKG_MINDESTALTER_25` | KWKG | 2020 | 5 | Jahr | GESICHERT | KWKG 2025 § 8 Abs. 2 — Mindestabstand zur Inbetriebnahme (Schwelle 25 %) |
| 36 | `KWKG_MINDESTALTER_50` | KWKG | 2020 | 10 | Jahr | GESICHERT | KWKG 2025 § 8 Abs. 2 — Mindestabstand zur Inbetriebnahme (Schwelle 50 %) |
| 37 | `KWKG_VBH_JAHRESDECKEL` | KWKG | 2021 | 5000 | h | GESICHERT | KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr |
| 38 | `KWKG_VBH_JAHRESDECKEL` | KWKG | 2023 | 4000 | h | GESICHERT | KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr |
| 39 | `KWKG_VBH_JAHRESDECKEL` | KWKG | 2025 | 3500 | h | GESICHERT | KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr |
| 40 | `KWKG_VBH_JAHRESDECKEL` | KWKG | 2026 | 3300 | h | GESICHERT | KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr |
| 41 | `KWKG_VBH_JAHRESDECKEL` | KWKG | 2027 | 3100 | h | GESICHERT | KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr |
| 42 | `KWKG_VBH_JAHRESDECKEL` | KWKG | 2028 | 2900 | h | GESICHERT | KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr |
| 43 | `KWKG_VBH_JAHRESDECKEL` | KWKG | 2029 | 2700 | h | GESICHERT | KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr |
| 44 | `KWKG_VBH_JAHRESDECKEL` | KWKG | 2030 | 2500 | h | GESICHERT | KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr |
| 45 | `KWKG_PAUSCHALE_BIS2KW` | KWKG | 2020 | 4 | ct/kWh | GESICHERT | KWKG 2025 § 9 — pauschale Vorauszahlung für Anlagen bis 2 kWel |
| 46 | `KWKG_PAUSCHALE_BIS2KW_VBH` | KWKG | 2020 | 60000 | h | GESICHERT | KWKG 2025 § 9 — pauschale Vorauszahlung für Anlagen bis 2 kWel |
| 47 | `KWKG_PAUSCHALE_GRENZE_KW` | KWKG | 2020 | 2 | kW | GESICHERT | KWKG 2025 § 9 — pauschale Vorauszahlung für Anlagen bis 2 kWel |
| 48 | `KWKG_STICHTAG_DAUERBETRIEB` | KWKG | 2020 | 2026 | Jahr | GESICHERT | KWKG 2025 § 6 Abs. 1 — Dauerbetrieb bis zum 31.12. dieses Jahres |
| 49 | `KWKG_REALISIERUNGSFRIST` | KWKG | 2025 | 4 | Jahr | GESICHERT | KWKG 2025 § 6 — Novelle 2025: bis 4 Jahre später bei Genehmigung/Beauftragung |
| 50 | `STROMST_REGELSATZ` | STROMSTEUER | 2026 | 20,5 | EUR/MWh | GESICHERT | § 3 StromStG, Fassung vom 22.12.2025 (BGBl. 2025 I Nr. 340) |
| 51 | `STROMST_ENTLASTUNG_9B` | STROMSTEUER | 2026 | 20 | EUR/MWh | GESICHERT | § 9b StromStG — Entlastung für das produzierende Gewerbe (Formular 1453) |
| 52 | `STROMST_SOCKELBETRAG_9B` | STROMSTEUER | 2026 | 250 | EUR/a | GESICHERT | § 9b StromStG — Sockelbetrag je Kalenderjahr (entspricht 12,5 MWh/a) |
| 53 | `STROMST_GRENZE_BEFREIUNG_9_1_3_KW` | STROMSTEUER | 2026 | 2000 | kW | GESICHERT | § 9 Abs. 1 Nr. 3 StromStG — elektrische Nennleistung der KWK-Anlage |
| 54 | `STROMST_RADIUS_RAEUMLICH_KM` | STROMSTEUER | 2026 | 4,5 | km | GESICHERT | § 12b StromStV — räumlicher Zusammenhang (steht in der Verordnung) |
| 55 | `STROMST_CO2_GRENZWERT_HOCHEFFIZIENT` | STROMSTEUER | 2026 | 270 | g/kWh | GESICHERT | § 2 StromStG — hocheffizient, fossile Anlagen, je kWh Energieertrag |
| 56 | `STROMST_ERLAUBNISSCHWELLE_KW` | STROMSTEUER | 2026 | 1000 | kW | GESICHERT | StromStG — Erlaubnisschwelle für Anlagenbetreiber |
| 57 | `ENERGIEST_ERDGAS` | ENERGIESTEUER | 2003 | 5,5 | EUR/MWh | GESICHERT | EnergieStG § 2 Abs. 3 Satz 1 Nr. 4 — Erdgas |
| 58 | `ENERGIEST_HEIZOEL_EL` | ENERGIESTEUER | 2003 | 61,35 | EUR/1000l | GESICHERT | EnergieStG § 2 Abs. 3 Satz 1 Nr. 1 Buchst. a — Heizöl EL, Schwefel bis 50 mg/kg |
| 59 | `ENERGIEST_GASOEL_SCHWEFELREICH` | ENERGIESTEUER | 2003 | 76,35 | EUR/1000l | GESICHERT | EnergieStG § 2 Abs. 3 Satz 1 Nr. 1 — Gasöl, Schwefel über 50 mg/kg |
| 60 | `ENERGIEST_FLUESSIGGAS` | ENERGIESTEUER | 2003 | 60,6 | EUR/1000kg | GESICHERT | EnergieStG § 2 Abs. 3 Satz 1 Nr. 5 — Flüssiggas |
| 61 | `ENERGIEST_SCHWEROEL` | ENERGIESTEUER | 2003 | 25 | EUR/1000kg | GESICHERT | EnergieStG § 2 Abs. 3 Satz 1 Nr. 2 — Schweröl |
| 62 | `ENERGIEST_53A5_ERDGAS` | ENERGIESTEUER | 2024 | 4,42 | EUR/MWh | GESICHERT | EnergieStG § 53a Abs. 5 — Gasturbinen und Verbrennungsmotoren (Formular 1135) |
| 63 | `ENERGIEST_53A5_HEIZOEL_EL` | ENERGIESTEUER | 2024 | 40,35 | EUR/1000l | GESICHERT | EnergieStG § 53a Abs. 5 — Gasturbinen und Verbrennungsmotoren (Formular 1135) |
| 64 | `ENERGIEST_53A5_FLUESSIGGAS` | ENERGIESTEUER | 2024 | 19,6 | EUR/1000kg | GESICHERT | EnergieStG § 53a Abs. 5 — Gasturbinen und Verbrennungsmotoren (Formular 1135) |
| 65 | `ENERGIEST_53A5_SCHWEROEL` | ENERGIESTEUER | 2024 | 4 | EUR/1000kg | GESICHERT | EnergieStG § 53a Abs. 5 — Gasturbinen und Verbrennungsmotoren (Formular 1135) |
| 66 | `ENERGIEST_53A5_KOHLE` | ENERGIESTEUER | 2024 | 0,16 | EUR/GJ | GESICHERT | EnergieStG § 53a Abs. 5 — Gasturbinen und Verbrennungsmotoren (Formular 1135) |
| 67 | `ENERGIEST_53A_MINDESTNUTZUNGSGRAD` | ENERGIESTEUER | 2024 | 70 | Prozent | GESICHERT | EnergieStG § 53a — Monats- oder Jahresnutzungsgrad als Voraussetzung |
| 68 | `ENERGIEST_54_ERDGAS` | ENERGIESTEUER | 2024 | 1,38 | EUR/MWh | GESICHERT | EnergieStG § 54 — Heizstoffe im produzierenden Gewerbe (Formular 1450) |
| 69 | `ENERGIEST_54_HEIZOEL_EL` | ENERGIESTEUER | 2024 | 15,34 | EUR/1000l | GESICHERT | EnergieStG § 54 — Heizstoffe im produzierenden Gewerbe (Formular 1450) |
| 70 | `ENERGIEST_54_FLUESSIGGAS` | ENERGIESTEUER | 2024 | 15,15 | EUR/1000kg | GESICHERT | EnergieStG § 54 — Heizstoffe im produzierenden Gewerbe (Formular 1450) |
| 71 | `ENERGIEST_54_SOCKELBETRAG` | ENERGIESTEUER | 2024 | 250 | EUR/a | GESICHERT | EnergieStG § 54 — Heizstoffe im produzierenden Gewerbe (Formular 1450), Sockelbetrag |
| 72 | `CO2_PREIS_NEHS` | CO2_PREIS | 2021 | 25 | EUR/t | GESICHERT | BEHG § 10 Abs. 2 — Festpreisphase des nationalen Emissionshandels |
| 73 | `CO2_PREIS_NEHS` | CO2_PREIS | 2022 | 30 | EUR/t | GESICHERT | BEHG § 10 Abs. 2 — Festpreisphase des nationalen Emissionshandels |
| 74 | `CO2_PREIS_NEHS` | CO2_PREIS | 2023 | 30 | EUR/t | GESICHERT | BEHG § 10 Abs. 2 — Festpreisphase des nationalen Emissionshandels |
| 75 | `CO2_PREIS_NEHS` | CO2_PREIS | 2024 | 45 | EUR/t | GESICHERT | BEHG § 10 Abs. 2 — Festpreisphase des nationalen Emissionshandels |
| 76 | `CO2_PREIS_NEHS` | CO2_PREIS | 2025 | 55 | EUR/t | GESICHERT | BEHG § 10 Abs. 2 — Festpreisphase des nationalen Emissionshandels |
| 77 | `CO2_PREIS_NEHS` | CO2_PREIS | 2026 | 65 | EUR/t | GESICHERT | EEX-Auktionen 2026 — durchgehend am Höchstpreis des Korridors zugeteilt |
| 78 | `CO2_PREIS_NEHS` | CO2_PREIS | 2027 | 65 | EUR/t | VORLAEUFIG | Kabinettsbeschluss 12.08.2026 (3. BEHG-ÄndG); Bundestag und Bundesrat stehen aus |
| 79 | `CO2_PREIS_NEHS` | CO2_PREIS | 2028 | 95 | EUR/t | PROGNOSE | Projektionsbericht 2026 der Bundesregierung — nur sekundär belegt |
| 80 | `CO2_PREIS_NEHS` | CO2_PREIS | 2030 | 125 | EUR/t | PROGNOSE | Projektionsbericht 2026 der Bundesregierung — nur sekundär belegt |
| 81 | `CO2_PREIS_NEHS_KORRIDOR_MIN` | CO2_PREIS | 2026 | 55 | EUR/t | GESICHERT | BEHG § 10 Abs. 2 — Untergrenze des Preiskorridors 2026 |
| 82 | `CO2_PREIS_NEHS_KORRIDOR_MAX` | CO2_PREIS | 2026 | 65 | EUR/t | GESICHERT | BEHG § 10 Abs. 2 — Obergrenze des Preiskorridors 2026 |
| 83 | `CO2_PREIS_NEHS_KORRIDOR_MIN` | CO2_PREIS | 2027 | 55 | EUR/t | VORLAEUFIG | Kabinettsbeschluss 12.08.2026 — Korridor 2027, Gesetz im Verfahren |
| 84 | `CO2_PREIS_NEHS_KORRIDOR_MAX` | CO2_PREIS | 2027 | 65 | EUR/t | VORLAEUFIG | Kabinettsbeschluss 12.08.2026 — Korridor 2027, Gesetz im Verfahren |
| 85 | `CO2_PREIS_NEHS_NACHVERKAUF` | CO2_PREIS | 2026 | 68 | EUR/t | GESICHERT | DEHSt — Verkauf ab 03.11.2026, unbegrenzte Menge |
| 86 | `CO2_PREIS_NEHS_NACHKAUF` | CO2_PREIS | 2027 | 70 | EUR/t | GESICHERT | DEHSt — Nachkauf von 2026er-Zertifikaten bis 31.08.2027 |
| 87 | `EF_NACHWEIS_HEIZOEL` | EF_NACHWEIS | 2020 | 310 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — durch das GModG unverändert |
| 88 | `EF_NACHWEIS_ERDGAS` | EF_NACHWEIS | 2020 | 240 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — durch das GModG unverändert |
| 89 | `EF_NACHWEIS_FLUESSIGGAS` | EF_NACHWEIS | 2020 | 270 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — durch das GModG unverändert |
| 90 | `EF_NACHWEIS_STEINKOHLE` | EF_NACHWEIS | 2020 | 400 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — durch das GModG unverändert |
| 91 | `EF_NACHWEIS_BRAUNKOHLE` | EF_NACHWEIS | 2020 | 430 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — durch das GModG unverändert |
| 92 | `EF_NACHWEIS_HOLZ` | EF_NACHWEIS | 2020 | 20 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — durch das GModG unverändert |
| 93 | `EF_NACHWEIS_STROM_NETZ` | EF_NACHWEIS | 2020 | 560 | g/kWh | GESICHERT | GEG Anlage 9, Fassung bis 31.12.2026 |
| 94 | `EF_NACHWEIS_STROM_NETZ` | EF_NACHWEIS | 2027 | 100 | g/kWh | GESICHERT | GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 95 | `EF_NACHWEIS_BIOGAS` | EF_NACHWEIS | 2020 | 140 | g/kWh | GESICHERT | GEG Anlage 9, Fassung bis 31.12.2026 |
| 96 | `EF_NACHWEIS_BIOGAS` | EF_NACHWEIS | 2027 | 80 | g/kWh | GESICHERT | GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 97 | `EF_NACHWEIS_BIOGAS_GEBAEUDENAH` | EF_NACHWEIS | 2020 | 75 | g/kWh | GESICHERT | GEG Anlage 9, Fassung bis 31.12.2026 |
| 98 | `EF_NACHWEIS_BIOGAS_GEBAEUDENAH` | EF_NACHWEIS | 2027 | 70 | g/kWh | GESICHERT | GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 99 | `EF_NACHWEIS_BIOMETHAN` | EF_NACHWEIS | 2020 | 240 | g/kWh | GESICHERT | GEG Anlage 9, Fassung bis 31.12.2026 |
| 100 | `EF_NACHWEIS_BIOMETHAN` | EF_NACHWEIS | 2027 | 80 | g/kWh | GESICHERT | GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 101 | `EF_NACHWEIS_BIOGENES_FLUESSIGGAS` | EF_NACHWEIS | 2020 | 180 | g/kWh | GESICHERT | GEG Anlage 9, Fassung bis 31.12.2026 |
| 102 | `EF_NACHWEIS_BIOGENES_FLUESSIGGAS` | EF_NACHWEIS | 2027 | 80 | g/kWh | GESICHERT | GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 103 | `EF_NACHWEIS_BIOOEL` | EF_NACHWEIS | 2020 | 210 | g/kWh | GESICHERT | GEG Anlage 9, Fassung bis 31.12.2026 |
| 104 | `EF_NACHWEIS_BIOOEL` | EF_NACHWEIS | 2027 | 80 | g/kWh | GESICHERT | GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 105 | `EF_NACHWEIS_ABWAERME` | EF_NACHWEIS | 2020 | 40 | g/kWh | GESICHERT | GEG Anlage 9, Fassung bis 31.12.2026 |
| 106 | `EF_NACHWEIS_ABWAERME` | EF_NACHWEIS | 2027 | 10 | g/kWh | GESICHERT | GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 107 | `EF_NACHWEIS_VERDRAENGUNGSSTROMMIX` | EF_NACHWEIS | 2020 | 860 | g/kWh | GESICHERT | GEG Anlage 9, Fassung bis 31.12.2026 — Verdrängungsstrommix KWK |
| 108 | `EF_NACHWEIS_VERDRAENGUNGSSTROMMIX` | EF_NACHWEIS | 2027 | *(entfällt)* | g/kWh | GESICHERT | GModG: entfällt ersatzlos, Bewertung nach DIN EN 15316-4-5 (L12) |
| 109 | `EF_NACHWEIS_FW_KWK_KOHLE` | EF_NACHWEIS | 2020 | 300 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — Fernwärme aus KWK mit mindestens 70 %, Kohle |
| 110 | `EF_NACHWEIS_FW_KWK_GAS_FLUESSIG` | EF_NACHWEIS | 2020 | 180 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — Fernwärme aus KWK mit mindestens 70 %, gasförmig/flüssig |
| 111 | `EF_NACHWEIS_FW_KWK_ERNEUERBAR` | EF_NACHWEIS | 2020 | 40 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — Fernwärme aus KWK mit mindestens 70 %, erneuerbar |
| 112 | `EF_NACHWEIS_FW_HEIZWERK_KOHLE` | EF_NACHWEIS | 2020 | 400 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — Fernwärme aus Heizwerken, Kohle |
| 113 | `EF_NACHWEIS_FW_HEIZWERK_GAS_FLUESSIG` | EF_NACHWEIS | 2020 | 300 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — Fernwärme aus Heizwerken, gasförmig/flüssig |
| 114 | `EF_NACHWEIS_FW_HEIZWERK_ERNEUERBAR` | EF_NACHWEIS | 2020 | 60 | g/kWh | GESICHERT | GEG/GModG Anlage 9 — Fernwärme aus Heizwerken, erneuerbar |
| 115 | `EF_NACHWEIS_FW_VORKETTE_AUFSCHLAG` | EF_NACHWEIS | 2027 | 20 | Prozent | GESICHERT | GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226 — pauschaler Aufschlag Vorkette und Netzverluste |
| 116 | `EF_NACHWEIS_FW_VORKETTE_MINDEST` | EF_NACHWEIS | 2027 | 40 | g/kWh | GESICHERT | GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226 — Mindestaufschlag Vorkette und Netzverluste |
| 117 | `EF_BILANZ_STROMMIX_CO2_DIREKT` | EF_BILANZ | 2020 | 365 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, CO2 direkt |
| 118 | `EF_BILANZ_STROMMIX_THG_OHNE_VORKETTE` | EF_BILANZ | 2020 | 373 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG ohne Vorkette |
| 119 | `EF_BILANZ_STROMMIX_THG_MIT_VORKETTE` | EF_BILANZ | 2020 | 435 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG mit Vorkette — maßgeblich |
| 120 | `EF_BILANZ_STROMMIX_CO2_DIREKT` | EF_BILANZ | 2021 | 406 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, CO2 direkt |
| 121 | `EF_BILANZ_STROMMIX_THG_OHNE_VORKETTE` | EF_BILANZ | 2021 | 414 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG ohne Vorkette |
| 122 | `EF_BILANZ_STROMMIX_THG_MIT_VORKETTE` | EF_BILANZ | 2021 | 477 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG mit Vorkette — maßgeblich |
| 123 | `EF_BILANZ_STROMMIX_CO2_DIREKT` | EF_BILANZ | 2022 | 433 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, CO2 direkt |
| 124 | `EF_BILANZ_STROMMIX_THG_OHNE_VORKETTE` | EF_BILANZ | 2022 | 441 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG ohne Vorkette |
| 125 | `EF_BILANZ_STROMMIX_THG_MIT_VORKETTE` | EF_BILANZ | 2022 | 503 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG mit Vorkette — maßgeblich |
| 126 | `EF_BILANZ_STROMMIX_CO2_DIREKT` | EF_BILANZ | 2023 | 379 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, CO2 direkt |
| 127 | `EF_BILANZ_STROMMIX_THG_OHNE_VORKETTE` | EF_BILANZ | 2023 | 387 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG ohne Vorkette |
| 128 | `EF_BILANZ_STROMMIX_THG_MIT_VORKETTE` | EF_BILANZ | 2023 | 442 | g/kWh | GESICHERT | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG mit Vorkette — maßgeblich |
| 129 | `EF_BILANZ_STROMMIX_CO2_DIREKT` | EF_BILANZ | 2024 | 353 | g/kWh | VORLAEUFIG | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, CO2 direkt (vorläufig) |
| 130 | `EF_BILANZ_STROMMIX_THG_OHNE_VORKETTE` | EF_BILANZ | 2024 | 361 | g/kWh | VORLAEUFIG | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG ohne Vorkette (vorläufig) |
| 131 | `EF_BILANZ_STROMMIX_THG_MIT_VORKETTE` | EF_BILANZ | 2024 | 414 | g/kWh | VORLAEUFIG | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG mit Vorkette — maßgeblich (vorläufig) |
| 132 | `EF_BILANZ_STROMMIX_CO2_DIREKT` | EF_BILANZ | 2025 | 344 | g/kWh | VORLAEUFIG | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, CO2 direkt (geschätzt) |
| 133 | `EF_BILANZ_STROMMIX_THG_OHNE_VORKETTE` | EF_BILANZ | 2025 | 352 | g/kWh | VORLAEUFIG | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG ohne Vorkette (geschätzt) |
| 134 | `EF_BILANZ_STROMMIX_THG_MIT_VORKETTE` | EF_BILANZ | 2025 | 406 | g/kWh | VORLAEUFIG | UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026, THG mit Vorkette — maßgeblich (geschätzt) |
| 135 | `EF_BILANZ_EBEV_ERDGAS_HI` | EF_BILANZ | 2023 | 200,9 | g/kWh | GESICHERT | EBeV 2030, Anlage 2 Teil 4 — 55,8 t CO2/TJ, heizwertbezogen |
| 136 | `EF_BILANZ_EBEV_ERDGAS_HO` | EF_BILANZ | 2023 | 181,4 | g/kWh | GESICHERT | EBeV 2030, Anlage 2 Teil 4 — brennwertbezogen, die deutsche Abrechnungspraxis |
| 137 | `EF_BILANZ_EBEV_HEIZOEL_EL` | EF_BILANZ | 2023 | 266,4 | g/kWh | GESICHERT | EBeV 2030, Anlage 2 Teil 4 — 74,0 t CO2/TJ |
| 138 | `EF_BILANZ_EBEV_HEIZOEL_S` | EF_BILANZ | 2023 | 286,9 | g/kWh | GESICHERT | EBeV 2030, Anlage 2 Teil 4 — 79,7 t CO2/TJ |
| 139 | `EF_BILANZ_EBEV_FLUESSIGGAS` | EF_BILANZ | 2023 | 235,8 | g/kWh | GESICHERT | EBeV 2030, Anlage 2 Teil 4 — 65,5 t CO2/TJ |
| 140 | `EF_BILANZ_EBEV_PFLANZENOEL` | EF_BILANZ | 2023 | 266,4 | g/kWh | GESICHERT | EBeV 2030, Anlage 2 Teil 4 — auch Tierfette und Altspeiseöl |
| 141 | `EF_BILANZ_EBEV_BIODIESEL` | EF_BILANZ | 2023 | 266,4 | g/kWh | GESICHERT | EBeV 2030, Anlage 2 Teil 4 |
| 142 | `EF_BILANZ_EBEV_BIOMASSE` | EF_BILANZ | 2023 | 0 | g/kWh | GESICHERT | EBeV 2030 § 8 — nur MIT Nachhaltigkeitsnachweis, sonst voller fossiler Wert (L13) |
| 143 | `EF_BILANZ_EBEV_UMRECHNUNG_HO` | EF_BILANZ | 2023 | 3,2508 | GJ/MWh | GESICHERT | EBeV 2030, Anlage 2 Teil 4 — Umrechnung brennwertbezogener Mengen; Hi/Ho-Falle rund 10 % |
| 144 | `EF_BILANZ_BAFA_BIOGAS` | EF_BILANZ | 2026 | 152 | g/kWh | GESICHERT | BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen |
| 145 | `EF_BILANZ_BAFA_KLAERGAS` | EF_BILANZ | 2026 | 50 | g/kWh | GESICHERT | BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen |
| 146 | `EF_BILANZ_BAFA_DEPONIEGAS` | EF_BILANZ | 2026 | 50 | g/kWh | GESICHERT | BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen |
| 147 | `EF_BILANZ_BAFA_PELLETS` | EF_BILANZ | 2026 | 36 | g/kWh | GESICHERT | BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen |
| 148 | `EF_BILANZ_BAFA_HOLZ_TROCKEN` | EF_BILANZ | 2026 | 27 | g/kWh | GESICHERT | BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen |
| 149 | `EF_BILANZ_BAFA_BIODIESEL` | EF_BILANZ | 2026 | 70 | g/kWh | GESICHERT | BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen |
| 150 | `EF_BILANZ_BAFA_KLAERSCHLAMM` | EF_BILANZ | 2026 | 10 | g/kWh | GESICHERT | BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen |
| 151 | `EF_BILANZ_BAFA_FERNWAERME` | EF_BILANZ | 2026 | 280 | g/kWh | GESICHERT | BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen |
| 152 | `EF_BILANZ_BAFA_STROM` | EF_BILANZ | 2026 | 435 | g/kWh | GESICHERT | BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen |
| 153 | `PEF_NACHWEIS_HEIZOEL` | PEF_NACHWEIS | 2020 | 1,1 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert |
| 154 | `PEF_NACHWEIS_ERDGAS` | PEF_NACHWEIS | 2020 | 1,1 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert |
| 155 | `PEF_NACHWEIS_FLUESSIGGAS` | PEF_NACHWEIS | 2020 | 1,1 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert |
| 156 | `PEF_NACHWEIS_STEINKOHLE` | PEF_NACHWEIS | 2020 | 1,1 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert |
| 157 | `PEF_NACHWEIS_BRAUNKOHLE` | PEF_NACHWEIS | 2020 | 1,2 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert |
| 158 | `PEF_NACHWEIS_STROM_GEBAEUDENAH` | PEF_NACHWEIS | 2020 | 0 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert — PV, Wind am Gebäude |
| 159 | `PEF_NACHWEIS_ERDWAERME` | PEF_NACHWEIS | 2020 | 0 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert |
| 160 | `PEF_NACHWEIS_SOLARTHERMIE` | PEF_NACHWEIS | 2020 | 0 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert |
| 161 | `PEF_NACHWEIS_UMGEBUNGSWAERME` | PEF_NACHWEIS | 2020 | 0 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert |
| 162 | `PEF_NACHWEIS_ABWAERME` | PEF_NACHWEIS | 2020 | 0 | - | GESICHERT | GEG/GModG Anlage 4 — durch das GModG unverändert |
| 163 | `PEF_NACHWEIS_STROM_NETZ` | PEF_NACHWEIS | 2020 | 1,8 | - | GESICHERT | GEG Anlage 4, Fassung bis 31.12.2026 — nicht erneuerbarer Anteil |
| 164 | `PEF_NACHWEIS_STROM_NETZ` | PEF_NACHWEIS | 2027 | 1,5 | - | GESICHERT | GModG Anlage 4 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 165 | `PEF_NACHWEIS_HOLZ` | PEF_NACHWEIS | 2020 | 0,2 | - | GESICHERT | GEG Anlage 4, Fassung bis 31.12.2026 — nicht erneuerbarer Anteil |
| 166 | `PEF_NACHWEIS_HOLZ` | PEF_NACHWEIS | 2027 | 0,7 | - | GESICHERT | GModG Anlage 4 ab 01.01.2027, BGBl. 2026 I Nr. 226 — Faktor 3,5 |
| 167 | `PEF_NACHWEIS_BIOGAS` | PEF_NACHWEIS | 2020 | 1,1 | - | GESICHERT | GEG Anlage 4, Fassung bis 31.12.2026 — nicht erneuerbarer Anteil |
| 168 | `PEF_NACHWEIS_BIOGAS` | PEF_NACHWEIS | 2027 | 0,7 | - | GESICHERT | GModG Anlage 4 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 169 | `PEF_NACHWEIS_BIOMETHAN` | PEF_NACHWEIS | 2020 | 1,1 | - | GESICHERT | GEG Anlage 4, Fassung bis 31.12.2026 — nicht erneuerbarer Anteil |
| 170 | `PEF_NACHWEIS_BIOMETHAN` | PEF_NACHWEIS | 2027 | 0,7 | - | GESICHERT | GModG Anlage 4 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 171 | `PEF_NACHWEIS_BIOGENES_FLUESSIGGAS` | PEF_NACHWEIS | 2020 | 1,1 | - | GESICHERT | GEG Anlage 4, Fassung bis 31.12.2026 — nicht erneuerbarer Anteil |
| 172 | `PEF_NACHWEIS_BIOGENES_FLUESSIGGAS` | PEF_NACHWEIS | 2027 | 0,7 | - | GESICHERT | GModG Anlage 4 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 173 | `PEF_NACHWEIS_BIOOEL` | PEF_NACHWEIS | 2020 | 1,1 | - | GESICHERT | GEG Anlage 4, Fassung bis 31.12.2026 — nicht erneuerbarer Anteil |
| 174 | `PEF_NACHWEIS_BIOOEL` | PEF_NACHWEIS | 2027 | 0,7 | - | GESICHERT | GModG Anlage 4 ab 01.01.2027, BGBl. 2026 I Nr. 226 |
| 175 | `PEF_NACHWEIS_WASSERSTOFF` | PEF_NACHWEIS | 2027 | 0,7 | - | GESICHERT | GModG Anlage 4 ab 01.01.2027, BGBl. 2026 I Nr. 226 — Wasserstoff, Derivate, synthetisches Heizöl |
| 176 | `PEF_NACHWEIS_FERNWAERME` | PEF_NACHWEIS | 2027 | 0,7 | - | GESICHERT | GModG Anlage 4 ab 01.01.2027, BGBl. 2026 I Nr. 226 — Standardwert Fernwärme |
| 177 | `PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX` | PEF_NACHWEIS | 2020 | 2,8 | - | GESICHERT | GEG Anlage 4, Fassung bis 31.12.2026 — nicht erneuerbarer Anteil — Verdrängungsstrommix KWK |
| 178 | `PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX` | PEF_NACHWEIS | 2027 | *(entfällt)* | - | GESICHERT | GModG: entfällt ersatzlos, Bewertung nach DIN EN 15316-4-5 (L12) |
| 179 | `PEF_NACHWEIS_BIOMASSE_RAEUMLICH` | PEF_NACHWEIS | 2020 | 0,3 | - | GESICHERT | GEG/GModG § 22 Abs. 1 Satz 2 — Biomasse im unmittelbaren räumlichen Zusammenhang |
| 180 | `PEF_NACHWEIS_FW_MINDESTWERT` | PEF_NACHWEIS | 2027 | 0,5 | - | GESICHERT | GModG § 22 Abs. 6 — Untergrenze für Fernwärme |
| 181 | `PEF_NACHWEIS_FW_MINDERUNG_JE_PROZENTPUNKT` | PEF_NACHWEIS | 2027 | 0,002 | - | GESICHERT | GModG § 22 Abs. 6 — Minderung je Prozentpunkt erneuerbarer Anteil |
| 182 | `UMSATZSTEUER_REGELSATZ` | UMSATZSTEUER | 2007 | 19 | Prozent | GESICHERT | UStG § 12 Abs. 1 — Regelsteuersatz seit 01.01.2007 |

---

## 5 Verifikation

### 5.1 Referenzlauf — warum A/B und nicht gegen `2026-08-16_B4`

`Referenzlaeufe/LIESMICH.md` führt **`2026-08-16_B4`** als gültige Basis. Diese Basis ist
auf der produktiven `Kenndaten.accdb` mit **Datenstand 15.08.2026 22:50** gerechnet. Der
heutige Datenstand ist ein anderer: Ein Kontrollvergleich des unveränderten HEAD-Laufs
gegen B4 ergibt **FAIL in 7 von 8 Projekten** — neue Stromspeicher- und
Pufferspeicherzeilen in 1017/1018/1024, verschwundene `carrier_id`-Zuordnungen in
1017/1023/1024, ein zusätzliches BHKW-Modul in 1018. Das sind **Datenänderungen des
Anwenders**, kein Codeeffekt: Der Lauf, der sie zeigt, ist der **unveränderte HEAD**.

Deshalb der A/B-Nachweis auf **derselben Wegwerf-Kopie**:

```
Referenzlauf.exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb  <Scratch>\DB
A = HEAD 12cdce6            → projekt <id> <Scratch>\A\Projekt_<id> <Scratch>\DB
B = HEAD + Etappe E1        → projekt <id> <Scratch>\B\Projekt_<id> <Scratch>\DB
Referenzlauf.exe vergleich <Scratch>\A <Scratch>\B
```

Beide Läufe mit dem jeweils dazu gebauten `Referenzlauf.csproj` (ProjectReference auf die
App, also Exe und DLL garantiert konsistent), Feature-Flag `Kaskade_Zweikanalig` **AUS**,
acht Projekte 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024.

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (21 Dateien, 254140 Werte)
Projekt_1018: PASS (22 Dateien, 236633 Werte)
Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1023: PASS (25 Dateien, 262918 Werte)
Projekt_1024: PASS (26 Dateien, 271692 Werte)

GESAMT: PASS (2 129 512 Werte innerhalb der Toleranz)
```

Zusätzlich der schärfere Nachweis: **194 von 194 CSV byte-/MD5-gleich, 0 abweichend.**

### 5.2 Verifikationstabelle

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| V1 | Referenzlauf A/B, Flag AUS, 8 Projekte | `Referenzlauf.exe vergleich` auf gemeinsamer DB-Kopie | **8/8 PASS**, 2 129 512 Werte |
| V2 | Byte-Identität der Ergebnisdateien | MD5 je Datei | **194/194 gleich**, 0 abweichend |
| V3 | Engine unberührt | `git diff --name-only -- Allgemein/Simulation` | **0 Dateien** |
| V4 | KWKG-Jahresdeckel wertgleich | Harness B2: alter Tabellen-Lookup gegen Katalog-Lookup, Jahre 2015–2045, beide durch das echte `StaffelDeckel` | **31/31 gleich**, 0 Abweichungen |
| V5 | Alttabelle unangetastet | Harness B1: `Tab_KWKG_Staffel` weiterhin 8 Zeilen ab 2020 | **erfüllt** |
| V6 | Seed vollständig | Harness A1: `COUNT(*)` gegen `Vorbelegung().Count` | **182 = 182** |
| V7 | Doppelstart legt nichts doppelt an | Harness A2: dreimal `StelleKatalogSicher` | **182 unverändert** |
| V8 | Seed ohne Schlüssel/Jahr-Dubletten | Harness A3 | **0 Dubletten** |
| V9 | Feldlängen der Persistenz | Harness A4: 60/40/20/12/120 Zeichen | **0 Überschreitungen** |
| V10 | Jede Seed-Zeile über die Fassade identisch (Wert, Einheit, Status, Quelle, Klasse, JahrVon) | Harness A5, alle 182 Zeilen | **0 Abweichungen** |
| V11 | Stichtagsgrenze 2026 gegen 2027 | Harness A6a–i: EF Strom 560→100, PEF Strom 1,8→1,5, PEF Holz 0,2→0,7, EF Biogas 140→80, Fortführung nach 2030 | **9/9** |
| V12 | Verdrängungsstrommix entfällt ab 2027 | Harness A7a–f: 860 bzw. 2,8 in 2026, `null` in 2027 und 2035, Herkunftszeile mit JahrVon 2027 vorhanden | **6/6** |
| V13 | Kein Treffer ergibt `null`, nie 0 | Harness A8a–d: Jahr vor der ersten Zeile, unbekannter Schlüssel, bewusste Lücke Stromsteuer 2025 | **4/4** |
| V14 | Jahresreihe CO₂-Preis mit Herkunft | Harness A9a–i: 2021/2025/2026/2027, Fortführung 2029 und 2031, Status PROGNOSE, Einheit EUR/t, verwendetes JahrVon 2028 | **9/9** |
| V15 | L11 — Nachweis und Bilanz getrennt | Harness A10a–c: Größenordnung Faktor > 3, Präfix passt zur Klasse in allen 182 Zeilen | **3/3** |
| V16 | L3 — Einheitendisziplin | Harness A11a–e: Erdgas EUR/MWh, Heizöl EUR/1000l, Flüssiggas EUR/1000kg; 61,35 gegen 40,35 | **5/5** |
| V17 | L4 — Entlastungssatz und Sockelbetrag einzeln | Harness A12a–b | **2/2** |
| V18 | Umsatzsteuer hinterlegt (noch ohne Rechenwirkung) | Harness A13 | **19 %** |
| V19 | Code-Rückfallebene | Harness A15a–c: Tabelle gelöscht, Werte weiter da, `AusRueckfallebene` gesetzt, Neuanlage sät wieder ein | **3/3** |
| V20 | Maske — Klassenfilter | Harness C1a–c: Umsatzsteuer 1 Zeile, KWKG 49 Zeilen wie im Seed | **3/3** |
| V21 | Maske — Anlegen | Harness C2a–b | **2/2** |
| V22 | Maske — Dublettenprüfung Schlüssel + Jahr | Harness C3a–b: eine Meldung, keine zweite Zeile | **2/2** |
| V23 | Maske — Rückfrage bei Vergangenheitszeile, Antwort „Ja" | Harness C4a–e: neue Jahreszeile 2041 angelegt, Zeile 2021 und Zwischenjahr 2030 unverändert | **5/5** |
| V24 | Maske — Rückfrage abgebrochen | Harness C5a–b: Zeile bleibt unverändert | **2/2** |
| V25 | Maske — Zukunftszeile ohne Rückfrage | Harness C6a–d | **4/4** |
| V26 | Maske — Rückfrage, Antwort „Nein" ändert die bestehende Zeile | Harness C6e–i: Wert geändert, keine neue Zeile, danach zurückgesetzt | **5/5** |
| V27 | Maske — Löschen mit Rückfrage | Harness C7a–f: „Nein" löscht nicht, „Ja" löscht, Wert danach weg | **6/6** |
| V28 | Katalog nach allen Masken-Proben wieder auf Seed-Stand | Harness C8a–c | **182 Zeilen** |
| V29 | Deutsch und Englisch | Harness D1–D9: Titel, sechs Spaltenköpfe, Knopf, Klassen-Anzeigename je Sprache | **27/27** |
| V30 | Kein Anzeigetext ist Steuerwert | Harness D9: „CO₂-Preis" / „CO₂ price" in der Auswahl, gespeicherter Wert bleibt `CO2_PREIS` | **erfüllt** |
| V31 | Dialogwächter | Wächter-Thread über `EnumThreadWindows`, Fensterklasse `#32770` | **0 unerwartete Dialoge** |
| V32 | Build | `MSBuild WP-Plan.sln -p:Platform=x86`, Rebuild, Ausgabe in den Scratch-Ordner | **0 Fehler, exakt 6 Bestandswarnungen** |
| V33 | Kodierung und Zeilenenden | `file` je geänderter Datei, Suche nach U+FFFD | **alle UTF-8, alle CRLF, 0 Ersatzzeichen** |
| V34 | Produktiv-Datenbank nur gelesen | keine `Kenndaten.laccdb`; alle Schreibproben auf Wegwerf-Kopien mit vorher geprüftem `DataRepository.GetDBPath()` | **erfüllt** |

**Gesamtergebnis Harness: 110 Proben, 0 Fehler, 0 unerwartete Dialoge.**

### 5.3 Ergebnisneutralität — ausdrücklich

**Etappe E1 ist ergebnisneutral.** Kein gerechneter Wert eines Bestandsprojekts ändert
sich: 8 von 8 Projekten PASS, 194 von 194 Ergebnisdateien byte-identisch, 2 129 512
verglichene Werte ohne eine einzige Abweichung. Die einzige Änderung an einem
Bestandsrechenweg ist die Quelle der KWKG-Vbh-Staffel, und die ist Jahr für Jahr von 2015
bis 2045 als wertgleich nachgewiesen.

Der Katalog **wird** von der Anwendung angelegt und eingesät, sobald der
Wirtschaftlichkeitspfad läuft — gelesen wird daraus in E1 aber ausschließlich der
Jahresdeckel. Alle übrigen 174 Zeilen liegen bereit und wirken auf nichts.

---

## 6 Hinweise zum Betrieb

- **Die produktive Datenbank wurde nur gelesen.** Alle Schreibproben liefen auf Kopien
  unter einem eigenen Scratch-Ordner; der Referenzlauf kopiert mit `File.Copy` und biegt
  den DB-Pfad der Anwendung um. Eine `Kenndaten.laccdb` lag zu keinem Zeitpunkt neben der
  Quelle. Die Dateigröße der produktiven `Kenndaten.accdb` ist vor und nach der Etappe
  identisch (94 834 688 Byte); ihr Änderungszeitstempel ist während der Arbeit einmal
  gewandert, zu einem Zeitpunkt, an dem kein Prozess dieser Etappe auf sie zugegriffen
  hat — die Ursache liegt außerhalb (laufende Sitzung des Anwenders).
- **Visual Studio läuft mit.** Während der Umsetzung hat eine geöffnete
  VS-Instanz `Resource.Designer.cs` aus der geänderten `Resource.resx` **selbst neu
  erzeugt** (alphabetisch einsortiert). Der von Hand angehängte Block war dadurch
  doppelt und wurde entfernt; im Ergebnis stehen die 36 Schlüssel genau einmal, an der
  vom Generator vorgesehenen Stelle. Wer die `.resx` erneut ändert, sollte den Designer
  danach auf Dubletten prüfen.

---

## 7 Offene Punkte für E2 und die Folgeetappen

### Aus dieser Etappe entstanden

1. **Bewusste Lücken in der Jahresabdeckung.** Drei Bereiche sind erst ab einem
   bestimmten Jahr gepflegt, weil die Grundlagen für frühere Jahre keine belastbare
   Aussage hergeben. `Wert()` liefert davor `null` — eine sichtbare Lücke statt eines
   geratenen Werts:
   - **Stromsteuer ab 2026.** Die Absenkung 2024/2025 war befristet und ist erst zum
     01.01.2026 dauerhaft ins Gesetz übernommen worden; ob die Sätze 20,50/20,00 in den
     befristeten Jahren betragsgleich galten, sagt die Quelle nicht ausdrücklich.
   - **Energiesteuer-Entlastungen ab 2024.** Die Absätze 6 bis 8 des § 53a sind zum
     31.12.2023 entfallen; erst ab 2024 gilt die heutige Konstellation.
   - **EBeV-Brennstofffaktoren ab 2023.** Für die Abrechnungsjahre 2021 und 2022 galt die
     Vorgängerverordnung; deren Werte sind nicht erfasst.
2. **§ 53a Abs. 2 (allgemeiner Absatz) fehlt.** Erfasst ist nur Abs. 5, der für Motor-BHKW
   einschlägige. Abs. 2 weicht beim Flüssiggas erheblich ab (60,60 statt 19,60 €/1.000 kg)
   und wird gebraucht, sobald E4 andere Anlagenarten abbildet.
3. **Schadstoffe außer CO₂** (SO₂, NO₂, CO, Staub, PM10 des Strommix, Datenjahr 2021) sind
   nicht eingesät. Sie gehören zu `Tab_Kraftwerkspark` und dessen neuem Feld `Bezugsbasis`
   (Konzept Abschnitt 3) und werden dort eingeordnet, nicht hier.
4. **Der Zeilendialog prüft die Einheit nicht gegen den Schlüssel.** Nichts hindert daran,
   `ENERGIEST_ERDGAS` auf `EUR/1000l` zu stellen. Eine Plausibilitätsregel je
   Schlüsselpräfix wäre möglich, wurde aber bewusst zurückgestellt: Sie müsste bei jedem
   neuen Schlüssel mitgepflegt werden, und die Einheit steht in der Liste sichtbar neben
   dem Wert.
5. **Kein Löschschutz für Seed-Zeilen.** Der Anwender kann eine ausgelieferte Zeile
   löschen; die Rückfrage nennt die Folge, aber ein `ReadOnly`-Flag wie in den
   `_STAMM`-Tabellen gibt es nicht. Falls gewünscht, ist das eine additive Spalte.

### Aus dem Konzept übernommen

6. **E2 (Vbh-Korrektur, L6) ändert Ergebnisse bewusst.** `WirtschaftlichkeitCtrl.cs:848`
   setzt die erreichten Vollbenutzungsstunden auf `Betriebsstunden_Gesamt` — die Summe
   **thermischer** Vbh über alle Module, die 8.760 h überschreiten kann. Dafür gilt der
   K-3-Weg: A/B-Nachweis, Wirkungsbeleg, neuer Basis-Freeze.
7. **Die Referenzbasis braucht ohnehin einen Neuschnitt**, unabhängig von E2: `B4` ist
   gegen den heutigen Datenstand nicht mehr vergleichbar (Abschnitt 5.1). Ein neuer
   Basis-Freeze auf dem aktuellen Datenstand sollte vor E2 stehen, sonst vermischen sich
   Datenänderung und Rechenänderung.
8. **Die Umsatzsteuer ist hinterlegt, aber nicht angeschlossen.** Die 40 hart codierten
   Stellen mit `1,19` bleiben in E1 unverändert; L8 wird in E3 umgesetzt.
9. **§ 53 neben § 53a** bleibt rechtlich ungeklärt und wird in E4 als einstellbare Option
   modelliert — der Katalog hält beide Satzreihen dafür schon bereit.
