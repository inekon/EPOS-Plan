# B7 — Erlösrubrik, Energiekosten je Anlage, eine Emissionsspalte

Protokoll zur Etappe B7 des
[Wirtschaftlichkeitskonzepts](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
(§ 2.5, § 2.6, § 3.5, § 7). Anwenderauftrag 17.09.2026: „setze B7 nach B6 um", zusammen mit dem
Befund „Vergütungen und Reduktionen (Energiesteuer, Stromsteuer, …) sind in den Ergebnissen nicht
dargestellt."

---

## 1 Messung vor dem Bau

### 1.1 Die zwölf Positionen der Rubrik — wo sie entstehen und was persistiert ist

| Position | Entsteht in | Im Kapitalwert | Persistierte Größe | Zeile vor B7 |
|---|---|---|---|---|
| A1 KWK-Bonus Einspeisung (§ 7 Abs. 1 KWKG) | `WirtschaftlichkeitCtrl.BaueKwkgReihe`, je Modul | ja (Erlösreihe `KWKG`) | nur die Summe `KwkgErloes` | nein — nur die Summe, `WIRT_ZEILE_KWKG` |
| A2 KWK-Bonus Eigenstrom (§ 7 Abs. 2) | dito, Tatbestand § 6 Abs. 3 je Anlage | ja, dieselbe Reihe | nur die Summe | nein |
| A3 KWKG-Pauschale (§ 9 KWKG) | `PauschaleReihe` — **einmalig im Jahr 0** | ja (Reihe `KWKG_PAUSCHALE`) | **keine** — `KwkgJahr1` bleibt 0, wenn sie greift | nein |
| A4 Energiesteuer BHKW (§ 53/§ 53a Abs. 5) | `SteuerGutschriftRechner.Energiesteuer` | ja (Reihe `ENERGIESTEUER`) | `Energiesteuer` — **Summe mit A5** | ja, gemeinsam |
| A5 Energiesteuer Kessel (§ 54) | dieselbe Funktion, `summe54` | ja, dieselbe Reihe | dieselbe Spalte | ja, gemeinsam |
| A6 Stromsteuer-Entlastung (§ 9b) | `SteuerGutschriftRechner.StromsteuerEntlastung` | ja (Reihe `STROMSTEUER_ENTLASTUNG`) | `StromsteuerEntlastung` | ja |
| A7 Stromsteuer-Befreiung (§ 9 Abs. 1 Nr. 3) | dieselbe Klasse | **nach Modus (B6)** | `StromsteuerBefreiung` + `StromsteuerBefreiungModus` | ja |
| A8 Einspeiseerlös Strom | drei Pfade (Flat, Tarifmatrix, Rollenmodell) | ja | `Einspeiseerloes`, `…Pv`, `…Kwk` | ja |
| A9 PV-Vergütung (EEG) | `PvErloesRechner`, Reihe `PV_VERGUETUNG` | ja | `PvMarktpraemie`, `PvKompensation51a`, … | ja (P6-Block) |
| A10 Restwert | `KapitalwertRechner` | ja | `Restwert` (Barwert) | ja |
| B1 Vermiedene Stromkosten | `StromTarifRechner` (Differenzmethode) | **nein** (steckt im Reststrombetrag) | `VermiedenArbeit/Leistung/Gesamt` | ja, drei Zeilen |
| B2 PV-Ausweis (Bezug, Kappung, Ausfall) | `PvErloesRechner` | nein | `PvVermiedenerBezug`, `PvKappungsverlustKwh`, … | ja |

**Zwei Befunde daraus, die den Bau bestimmt haben.**

- **Die Aufteilung A1/A2 und A4/A5 liegt nicht persistiert vor.** A1/A2 steckt im
  `KwkgModulNachweis`, der nur im frischen Lauf lebt; A4/A5 gibt es überhaupt nicht getrennt —
  `SteuerErgebnis.EnergiesteuerEur` ist eine Summe.
- **A3 hat keine Jahr-1-Größe.** Greift die Pauschale, setzt der Rechner `KwkgJahr1` auf 0 und
  hängt eine Reihe mit einem Betrag im **Jahr 0** an.

### 1.2 Die drei Ausgabewege

`WirtschaftlichkeitZeilen.Kennzahlen` ist seit E7 die eine Zeilendefinition; gelesen wird sie im
Ergebnisreiter (`WirtschaftlichkeitSeiteGaben`), im Word-Baustein
(`BausteineWirtschaftlichkeit.SchreibeVergleich`) und im Excel-Generator
(`ExcelBerichtGenerator`). Bericht und Excel rechnen den Lauf frisch; der persistierte Stand ist
nur ihr Rückfallnetz. Der Reiter zeigt nach „Berechnen" den frischen Lauf, nach dem Öffnen den
geladenen.

**Nebenbefund, bestätigt:** Die SICHTBARKEIT einer Zeile entschied sich an drei Stellen nach drei
Regeln — der Reiter filterte zusätzlich über die gewählten Spalten, Excel über den
Szenarioblock, Word gar nicht. Seite und Bericht konnten damit verschiedene Tabellen zeigen,
entgegen dem Versprechen von E7.

### 1.3 Die Kostenseite

Die Energieträgertabelle (`KostenSeiteGaben.Traegerspalten`/`Traeger`) führte zehn Spalten,
davon drei feste Emissionsspalten. Die CO₂-Spalte folgte dem Berechnungsmodus bereits
(`EmissionsFaktorSatz.Wirksam(modus)`); Kopf und Kurztext taten es nicht. Der Kurztext nannte
Ebene und Modus, aber **keine Herleitung** — und `Wirksam` fällt ohne Artenkatalog still auf den
reinen CO₂-Faktor zurück.

### 1.4 Die Mengen je Anlage

`KostenEmissionRechner.BerechneIntern` sammelt den Verbrauch je **Träger**
(`verbrauchJeTraeger`), gespeist aus den BHKW- und Heizkesselmodulen des Laufs
(`ErgebnisBHKWModulModel.CarrierId/Verbrauch`). Eine anlagenscharfe Struktur gab es nicht; die
Preise liest `LadeTraeger` (Arbeits-, Grund- und Leistungspreis, Heizwert).

---

## 2 Was gebaut wurde

### 2.1 Die Rubrik

Ein Block in `WirtschaftlichkeitZeilen.Kennzahlen` zwischen den Kostenzeilen und dem
Nettobarwert. `WirtZeile` trägt dafür fünf neue Merkmale: `Block` (`BLOCK_A`/`BLOCK_B`),
`IstUeberschrift`, `IstSumme`, `Einzug` und `Grundtext`.

**Block A** entsteht über `Erloes(...)`, das die Zeile zugleich in die Summandenliste legt —
die Summenzeile `ERL_A_SUMME` summiert damit genau die Zeilen über ihr. **Block B** entsteht
über `Ausweis(...)`, das keine Summandenliste kennt. Die Trennung ist kein Flag, das jemand
setzen oder vergessen könnte, sondern zwei verschiedene Wege in die Liste.

| Schlüssel | Titel (de) | Titel (en) | Block |
|---|---|---|---|
| `ERL_KOPF_A` | Erlöse und Vorteile — zahlungswirksam (Jahr 1) | Revenues and benefits — cash-effective (year 1) | A |
| `ERL_A_KWKG` | KWK-Zuschlag (§ 7 KWKG) [€/a] | CHP bonus (sec. 7 KWKG) [€/a] | A |
| `ERL_A1_EINSPEISUNG` | davon Einspeisung (§ 7 Abs. 1 KWKG) [€/a] | of which fed into the grid (sec. 7 (1) KWKG) [€/a] | A, Einzug |
| `ERL_A2_EIGEN` | davon Eigenstrom (§ 7 Abs. 2 KWKG) [€/a] | of which self-consumed (sec. 7 (2) KWKG) [€/a] | A, Einzug |
| `VBH_ELEKTRISCH` | Vollbenutzungsstunden elektrisch, KWKG-Basis [h/a] | (Bestand) | A, Einzug |
| `ERL_A_ENERGIESTEUER` | Energiesteuer-Entlastung (§ 53/§ 53a bzw. § 54 EnergieStG) [€/a] | Energy tax relief (sec. 53/53a resp. 54 EnergieStG) [€/a] | A |
| `ERL_A_STROMST_ENTLASTUNG` | Stromsteuer-Entlastung Netzbezug (§ 9b StromStG) [€/a] | Electricity tax relief on grid supply (sec. 9b StromStG) [€/a] | A |
| `ERL_A_STROMST_BEFREIUNG` | Stromsteuer-Befreiung Eigenverbrauch (§ 9 Abs. 1 Nr. 3 StromStG) [€/a] | Electricity tax exemption, self-consumption … | A, nur Modus ERLOES |
| `EINSPEISEERLOES` | Einspeiseerlös Strom [€/a] | Feed-in revenue, electricity [€/a] | A |
| `EINSPEISEERLOES_PV` / `_KWK`, `PV_FORM`, `PV_AW`, `PV_MARKTPRAEMIE`, `PV_51A` | (Bestand) | (Bestand) | A |
| `ERL_A_SUMME` | Summe Erlöse und Vorteile, zahlungswirksam [€/a] | Total revenues and benefits, cash-effective [€/a] | A, Summe |
| `ERL_KOPF_B` | Ausweis — nicht in der Summe | Disclosure — never part of the total | B |
| `ERL_B_STROMST_BEFREIUNG` | Stromsteuer-Befreiung … (Ausweis, nicht im Kapitalwert) | (Bestand aus B6) | B, nur Modus AUSWEIS |
| `VERMIEDEN_GESAMT` | Vermiedene Stromkosten, brutto [€/a] (Ausweis) | Avoided electricity cost, gross [€/a] (disclosure) | B |
| `VERMIEDEN_ARBEIT` / `_LEISTUNG` | (Bestand) | (Bestand) | B, Einzug |
| `ERL_B1_ABZUG_9B` | abzüglich entgangener Entlastung (§ 9b StromStG) [€/a] | less forgone relief (sec. 9b StromStG) [€/a] | B, Einzug |
| `ERL_B1_EFFEKTIV` | Vermiedene Stromkosten, effektiv [€/a] (Ausweis) | Avoided electricity cost, effective [€/a] (disclosure) | B |
| `PV_VERMIEDEN`, `PV_AUSFALL_KWH`, `PV_AUSFALL_EUR`, `PV_KAPPUNG` | (Bestand) | (Bestand) | B |

**Die Nullzeile mit Grund.** Eine A-Zeile erscheint, sobald das Projekt eine Anlage führt, für
die die Position gilt — auch bei Betrag 0. Die Zelle trägt dann den Klartext der fehlenden
Grundlage: „0 — kein KWK-Zuschlagssatz gepflegt oder Kontingent erschöpft", „0 — keine
Entlastungsnorm gewählt oder kein Steuersatz zugeordnet", „0 — nur produzierendes Gewerbe;
abzüglich Sockelbetrag 250 €/a", „0 — keine Einspeisevergütung gepflegt". **Excel bekommt
weiterhin die blanke Zahl** (`ExcelWert`), sonst wären Filter und Diagramme des Blattes hinüber.

Ohne BHKW entstehen die KWKG- und die Energiesteuerzeile gar nicht erst: „immer zeigen" heißt
auch bei Betrag 0 — nicht in jedem Projekt.

### 2.2 Die § 9b-Korrektur des Ausweises

Drei neue, **nicht persistierte** Felder am `WirtschaftlichkeitErgebnis`:
`VermiedenMengeMWh` (Bedarf ohne Anlage − Restbezug, neu geführt in
`StromErloesErgebnis.VermiedenMengeMWh`), `VermiedenEntlastung9bJahr` und
`ProduzierendesGewerbe`; `VermiedenEffektivJahr` ist die Rechnung darüber.

Der Satz kommt jahresgenau aus dem Gesetzeskatalog (`GESETZ_STROMST_ENTLASTUNG_9B`), die
Prüfung der Unternehmensart aus derselben Funktion, mit der die Steuerrechnung rechnet
(`SteuerGutschriftRechner.ProduzierendesGewerbe`, dafür von `private` auf `public`).

**Es wird keine Reihe angehängt und keine verändert** — der Kapitalwert ist unberührt.

### 2.3 Energiekosten je Anlage

`EnergieAnlageNachweis` (Anlage, Träger, Menge in MWh und in der Abrechnungseinheit, Einheit,
Preis, Kosten) entsteht in `KostenEmissionRechner.AnlageZeile` mit **derselben** Rechnung wie
die Trägersumme: mengenbasiert über den Heizwert, sonst direkt je kWh. Je BHKW- und
Kesselmodul eine Zeile, dazu eine für den Netzbezug (Wärmepumpe, Hilfsenergie, Gebäude — der
Rechenkern führt den Restbezug als eine Menge).

Der **Grundpreis** steckt bewusst nicht in den Anlagenzeilen: Er fällt einmal je Träger an,
nicht je Anlage, und stünde anteilig als erfundene Zahl da. Ohne Träger, ohne Menge oder ohne
Arbeitspreis entsteht keine Zeile.

Die Zeilen erscheinen unter „Energiekosten [€/a]" als Unterzeilen „davon <Anlage> [€/a]";
`WirtschaftlichkeitZeilen.AnlageHerleitung` liefert die Klartextform
„4.200,00 L × 0,9500 €/L = 3.990,00 €/a (Heizöl)".

### 2.4 Eine Emissionsspalte nach Modus

Aus zehn Spalten wurden acht. `EmissionsAusweis.SpaltenkopfEmission(modus)` liefert den Kopf,
`EmissionsAusweis.HerleitungEmission(satz, modus, kultur)` den Kurztext nach den drei Fällen der
Entscheidung E-1. Der `EmissionsFaktorSatz` führt dafür zwei neue Felder: `Zeilen` (die
Beitragszeilen der Summe) und `ArtenkatalogFehlt`.

| Fall | Spalte | Kurztext |
|---|---|---|
| Regelfall (mehrere Arten) | gewichtete Summe | „CO2 240,000 + CH4 0,500 × 28 = 254,0 g/kWh (GWP100). Herkunft des CO₂-Wertes: Ebene „KATALOG". Lesekette: …" |
| Nur CO₂ gepflegt | = CO₂-Faktor | „Für diesen Energieträger ist außer CO₂ keine weitere Emissionsart hinterlegt — der Äquivalentwert entspricht deshalb dem CO₂-Faktor." |
| Wert ist bereits Äquivalent (F3) | unverändert | „Der hinterlegte Wert ist bereits ein CO₂-Äquivalent (Ebene „KATALOG") und wird nicht aufsummiert." |
| **Kein Artenkatalog** | reiner CO₂-Faktor | „Der Artenkatalog führt für diesen Energieträger keine Äquivalenzfaktoren — ausgewiesen wird der reine CO₂-Faktor der Ebene „…", **nicht ein Äquivalent**." |

Der vierte Fall ist der Kern der Regel „kein stiller Rückfall": `Wirksam(CO2E)` liefert dort den
CO₂-Faktor, und das steht da, statt unter einem Äquivalentkopf unwidersprochen zu bleiben.

SO₂ und NOx sind aus der **Kostentabelle** verschwunden; sie bleiben im Katalog
(`emissionsart`/`emissionswert`), im Energieträgerdialog und in der Emissionsbilanz vollständig.

### 2.5 Die Nebenbefunde

- **Eine Sichtbarkeitsregel für alle drei Ausgaben.** `WirtschaftlichkeitZeilen.Sichtbare`
  filtert die Zeilenliste einmal über die MENGE der Gruppe; die zweite Prüfung im Reiter und die
  dritte in Excel sind entfallen. Eine Blocküberschrift ohne Zeilen und eine Summe ohne
  Summanden fallen weg.
- **Die Vorschau des BHKW-Dialogs (Gruppe 6) liest dieselbe Quelle.** Die Handliste aus fünf
  Zahlen ist entfallen — mit ihr die Zeile „Stromsteuer p. a." als Summe aus Befreiung und
  Entlastung, also aus einer Position, die im Kapitalwert steht, und einer, die nur ausgewiesen
  wird. Gruppe 1b und 2 sind unangetastet.
- **Die Parameterzeile der Emissionsbilanz** heißt „Emissionsbilanz:" statt „Bilanzierung:"
  (`BILANZ_AUSWEIS`, beide Sprachen) — zwischen Geldangaben las sie sich als Vergütung.

---

## 3 Zahlen des Nachweises

### 3.1 Der synthetische Lauf der Rubrik (`ErloesrubrikTests`)

| Größe | Wert |
|---|---|
| A1/A2 KWK-Zuschlag | 7.316,00 €/a |
| A4/A5 Energiesteuer-Entlastung | 5.119,00 €/a |
| A6 Stromsteuer-Entlastung § 9b | 906,00 €/a |
| A8 Einspeiseerlös | 1.234,00 €/a |
| **Summe Block A** | **14.575,00 €/a** |
| A7 Befreiung § 9 Abs. 1 Nr. 3, Modus AUSWEIS | 86.000,00 €/a — **nicht in der Summe** |
| B1 vermiedene Kosten brutto | 4.321,00 €/a (Arbeit 4.662,00; Leistung −341,00) |
| vermiedene Menge | 20,00 MWh/a |
| Abzug § 9b (20,00 €/MWh × 20 MWh) | −400,00 €/a |
| **B1 effektiv** | **3.921,00 €/a** |
| Kapitalwert vorher = nachher | −123.456,00 € (unverändert; es wird keine Reihe berührt) |

Im Modus ERLOES wandert A7 nach Block A: Summe 14.575,00 + 86.000,00 = **100.575,00 €/a**.
Ohne produzierendes Gewerbe ist der Abzug 0, brutto **ist** effektiv, und die beiden
Korrekturzeilen entfallen.

### 3.2 Gegenproben — gefahren und im Prüfstand festgehalten

| Gegenprobe | Erwartung | Ergebnis |
|---|---|---|
| Block-B-Zeile in die Summe genommen | Summe 18.896,00 statt 14.575,00 €/a | rot; als Fall `Gegenprobe_Block_B_in_der_Summe_waere_eine_andere_Zahl` stehen geblieben |
| Rückfall CO2E → CO2 still eingebaut (Zweig `ArtenkatalogFehlt` entfernt) | Kurztext ohne die Benennung | rot in `Ohne_Artenkatalog_wird_der_Rueckfall_auf_CO2_benannt`; zurückgebaut |
| SO₂/NOx in die Kostentabelle zurückgeholt | Kopf trägt drei Emissionsspalten | rot in `SO2_und_NOx_stehen_nicht_mehr_in_der_Energietraegertabelle`; zurückgebaut |
| A-Zeile ohne Erzeugungswächter (nur `ImmerZeigen`) | KWKG-Zeile auch im reinen PV-Projekt | rot in `Ohne_BHKW_stehen_KWKG_und_Energiesteuer_nicht_in_der_Rubrik` — beim Bau aufgefallen und behoben |

### 3.3 Prüffälle

- **`EPOS.Kern.Tests/ErloesrubrikTests`** — 10 Fälle: Summe = Σ der A-Zeilen, Gegenprobe Block B,
  Blockkennung des Ausweises, § 9b-Korrektur mit und ohne produzierendes Gewerbe, A7 nach Modus,
  Nullzeile mit Klartext, Rubrik ohne BHKW, Block B ohne Zeilen, Aufschlüsselung der
  Energiekosten.
- **`EPOS.Kern.Tests/EmissionsspalteTests`** — 7 Fälle: Kopf nach Modus, die drei Fälle der
  Entscheidung E-1, kein stiller Rückfall, Modus CO2, beide Sprachen.
- **`EPOS.UI.Tests/Seiten/KostenSeiteTests`** — drei Fälle umgestellt und zwei neu: eine
  Emissionsspalte mit Kurztext, Kopf nach Modus, SO₂/NOx nicht mehr vorhanden.
- **`EPOS.UI.Tests/Dialoge/BhkwWirtschaftlichkeitDialogTests`** — der Vorschaufall misst jetzt
  die Rubrik samt Summe und der Trennung von Befreiung und Entlastung.

---

## 4 Abnahme

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Debug` | 0 Fehler, 5 Warnungen (Bestand, Schranke 7) |
| Windows-Schale (`-p:EnableWindowsTargeting=true`, x64) | 0 Fehler, 5 Warnungen (Bestand) |
| `EPOS.Kern.Tests` | 3 268 / 3 268 |
| `EPOS.UI.Tests` | 4 606 / 4 606 |
| `SpeicherEngine.Tests` / `KiKern.Tests` / `SpeicherPlanung.Tests` | 378 / 499 / 27 (1 übersprungen) |
| `SqlDialektPruefer` | 1 488 Texte, **0 Fundstellen** |
| Referenzlauf 1030, 1007, 1017, 1045, 1046 gegen `2026-09-16_R8_Heizkessel_Kaskade` | **5 / 5 PASS** |

Kein Schemaschritt, keine neue Referenzbasis, kein Push, kein CI- und kein iOS-Lauf.

---

## 5 Logbuch-Entwurf (Wiki, bestehende Version 1.2.0.2)

- Die Ergebnisse der Wirtschaftlichkeit führen die Erlösseite in einer eigenen Rubrik: zuerst
  die zahlungswirksamen Positionen mit ihrer Summe, darunter die Beträge, die nur ausgewiesen
  und nie mitgerechnet werden.
- Die vermiedenen Stromkosten werden zusätzlich abzüglich der entgangenen Entlastung nach
  § 9b StromStG ausgewiesen.
- Unter den Energiekosten steht je Anlage, aus welcher Menge und welchem Preis der Betrag
  entstanden ist.
- Die Energieträgertabelle der Kostenseite zeigt statt drei Emissionsspalten eine, die dem
  Berechnungsmodus des Projekts folgt.

---

## 6 Offene Punkte

Sie stehen als `B7-1` bis `B7-4` in § 6.3 des Konzepts: die gemeinsame Zeile für § 53/§ 53a und
§ 54, die fehlende Persistenz der Modul- und Anlagennachweise, die KWKG-Pauschale ohne
Rubrikzeile und der aus den Ergebnisdaten abgeleitete statt vom Rechner durchgereichte Grund
einer Nullzeile.

Nicht Gegenstand dieser Etappe und ausdrücklich liegen geblieben: **V-3** (das Mehrjahresbild
führt keine Spalten für die KWKG-Pauschale und die PV-Vergütung, `WirtschaftlichkeitZeilen`,
Klasse `Mehrjahresbild`) — ein B8-Punkt.
