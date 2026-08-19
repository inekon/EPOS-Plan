# W4 — Etappe E8: Abnahme, neue Referenzbasis, Abschlussprotokoll

**Stand: 19.08.2026.** Abnahme der Ausbaustufe W4 gegen
[`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md). Ausgangsstand **`e94be10`**
(Merge des Strangs „KI-Assistent-Aufgabensteuerung" neben W4), Arbeitsbaum sauber. Die
KI-Aufgabensteuerung ist **nicht** Gegenstand dieser Abnahme.

> **Ergebnis in einem Satz: Die Ausbaustufe ist abgenommen — mit vier benannten Vorbehalten.**
> Die Rechenkette steht, ist an 40 eigenen Proben getroffen und ergebnisneutral für den Bestand;
> die vier von den Vorgängeretappen selbst benannten Prüflücken sind gemessen und geschlossen.
> Offen bleiben: die **Zahlenprobe gegen die Altanwendung** aus Abschnitt 8 des Konzepts, die nie
> gerechnet wurde; **L12** (Methodenwechsel 2027) als reine Katalogdatenseite ohne Leser; **L13**
> (Biomasse-Konvention) ohne jede Umsetzung; und die Tatsache, dass **keine der neuen Rechenklassen
> einen dauerhaften Test** hat — jede Messung der Ausbaustufe ist ein Einzelnachweis, der beim
> nächsten Build nicht mitläuft.

> **Berichtigung vom 19.08.2026 (nach der Abnahme): zwei der vier Vorbehalte sind erledigt.**
> **A3 (L12)** und **A4 (L13)** sind umgesetzt —
> [`W4_L12_L13_Methodenwechsel_Protokoll.md`](W4_L12_L13_Methodenwechsel_Protokoll.md),
> Migrationsschritt 23, Katalog-Generation 4. Ergebnisneutral für Bestandsprojekte (216/216
> byte-gleich gegen B6, 972/972 Wirtschaftlichkeitswerte identisch gegen `3307378`), Wirkung mit
> Zahlen belegt. Die Befunde A3 und A4 unten bleiben als **Zustandsbeschreibung zum
> Abnahmezeitpunkt** stehen; ihre Einordnung „eigene Ausbaustufe" beziehungsweise „fachliche
> Entscheidung" hat sich bestätigt. **Offen bleiben A8** (Zahlenprobe gegen die Altanwendung)
> **und A1** (keine dauerhaften Tests).
>
> Ein Befund der Abnahme hat sich dabei als noch schärfer erwiesen, als er formuliert war: L12 war
> **kein fehlender Parameter**, sondern die Systemgrenze des `EmissionsBilanzRechner` selbst — die
> getrennte Referenz erzeugt den KWK-Strom im Kraftwerkspark, und genau das *ist* die abgeschaffte
> Stromgutschriftmethode. Ebenso stand die Biomasse-Konvention aus A4 nicht im Code, sondern in den
> Katalogwerten von `Tab_Brennstoff_Stamm` (Holz 20, Biogas 140, Rapsöl 210 g/kWh — reine
> Vorkettenwerte). Die Suche „0 Codetreffer" war richtig und hat deshalb dennoch nicht alles
> gezeigt.

---

## 1 Was diese Abnahme geprüft hat — und was nicht

| Geprüft | Wie |
|---|---|
| Konzept Punkt für Punkt (L1–L13, Datenmodell, Rechenkette, Eingabe, Administration, Verifikation) | Quellensicht gegen jede Konzeptzeile, Abweichungen gegen die Protokolle gehalten |
| Einheitendisziplin (L3), Netto (L8), Drei-Schichten-Regel | eigene Messungen am Code (Abschnitt 2.3, 2.4, 5.3) |
| Die 17 Fehler der Altanwendung | Nummer 10 und 15 selbst nachvollzogen, die übrigen aus einem Vorlauf übernommen (Abschnitt 3) |
| Die vier Prüflücken aus E6/E7 | Reflection-Harnisch mit Dialogwächter gegen Wegwerfkopien (Abschnitt 4) |
| Ergebnisneutralität des Rechenkerns | Referenzlauf gegen `2026-08-19_B5`, Basiswechsel auf B6 (Abschnitt 6) |

**Nicht Gegenstand:** der Strang KI-Assistent-Aufgabensteuerung; die Stufen W1 bis W3; der
Simulationskern (von W4 nicht angefasst — nachgewiesen, siehe 6).

---

## 2 Abnahme gegen die Leitentscheidungen L1 bis L13

| # | Inhalt | Stand | Beleg |
|---|---|---|---|
| **L1** | Ausbaustufe: KWKG-Reihe erweitern, bei mehr als einer Reihe auf **benannte Reihen** umstellen | **umgesetzt** | `KapitalwertRechner.ErloesReihe` (`:47-102`), `Rechne` (`:141-166`); Einspeisung der Reihen `WirtschaftlichkeitCtrl.cs:1259-1264`, `:1271`. E7 macht sie erstmals sichtbar (`WIRT_REIHE_*` als Spaltenköpfe) |
| **L2** | Ein Katalog statt Konstanten im Code | **umgesetzt** | `Tab_Gesetzesparameter`, `GesetzKatalog.cs`; Pflegemaske `Views/Admin/Form_Gesetzesparameter.cs`; generationsweise Nachsaat (E6). Gemessen: 182 → 186 Zeilen beim ersten Start, 0 beim zweiten |
| **L3** | Einheitendisziplin — jeder Satz in seiner gesetzlichen Einheit, Umrechnung nur über gepflegte Heizwerte | **umgesetzt, selbst nachgeprüft** | `SteuerGutschriftRechner.MengeInGesetzlicherEinheit:341-393`. Siehe 2.3 |
| **L4** | Steuersatz und Entlastungssatz getrennt | **umgesetzt** | Katalog führt `STROMST_REGELSATZ` 20,50 · `STROMST_ENTLASTUNG_9B` 20,00 · `STROMST_SOCKELBETRAG_9B` 250 €/a einzeln — in der Steuerherkunft der Lücke-2-Messung (4.2) alle drei einzeln ausgewiesen |
| **L5** | Kostenposition erweitern statt eigener Erlöstabelle | **umgesetzt** | Migrationsschritt 19, fünf Spalten an `Tab_ProjektWerte`. *Abweichung `Bemessung` TEXT(30) statt TEXT(20) — im E3-Protokoll begründet (bei TEXT(20) scheitert das UPDATE still)* |
| **L6** | Vollbenutzungsstunden elektrisch und je Modul | **umgesetzt** | Migrationsschritt 18. *Abweichung: Spalte heißt `VbhThermisch` statt `Betriebsstunden` — im E2-Protokoll begründet* |
| **L7** | Wartung genau eine Angabe · Satz vorgeschlagen · je Modul · alle drei Leistungsmodelle | **umgesetzt** | E3 (`BetriebskostenCtrl.Katalog`, elf Positionen), E5 (`StromTarifRechner.Leistungskosten`), E6 (`KwkgSatzRechner`). **Einschränkung:** die Konzept-Datenmodellzeile `Tab_BHKW/_STAMM.Wartungsbemessung TEXT(20)` ist **nicht angelegt** — siehe 2.5, Befund A2 |
| **L8** | Netto verbindlich, Umsatzsteuer Katalogparameter, **kein** USt auf den KWK-Zuschlag | **umgesetzt, selbst nachgeprüft** | Siehe 2.4 |
| **L9** | Rechenlogik ohne Datenbankzugriff, **Tests im vorhandenen Testprojekt** | **teilweise** | Die reinen Funktionen existieren (`SteuerGutschriftRechner`, `StromTarifRechner`, `KwkgSatzRechner` — DTO-Ein-/Ausgabe, kein DB-Bezug). **Tests gibt es nicht:** Weder `SpeicherEngine.Tests` noch `KiKern.Tests` nennen eine dieser Klassen. Siehe Befund **A1** |
| **L10** | HT/NT entfällt, Vier-Preis-Struktur mit demselben Durchschnittspreis | **teilweise, begründet** | Erfüllt im **neuen** Rollenmodell (ein Arbeitspreis je Rolle). Im **Vorgabemodell `ZONEN`** bleibt HT/NT vollständig in Kraft — E5 hat es bewusst unangetastet gelassen, weil daran die Ergebnisneutralität hängt (E5-Protokoll 3.x). Das Konzept ist an dieser Stelle überholt, siehe 8 |
| **L11** | Zwei Faktorensätze strikt getrennt, nie dieselbe Variable | **umgesetzt im Katalog und im Steuerpfad** | Klassen `EF_NACHWEIS` (30 Zeilen) und `EF_BILANZ` (36) getrennt; E4 nimmt für den CO₂-Grenzwert ausdrücklich `EF_BILANZ_EBEV_*` (`SteuerGutschriftRechner.Co2JeEnergieertrag:523-545`). **Einschränkung:** `EmissionsBilanzRechner` liest weiterhin `Tab_Kraftwerkspark` und nicht den Katalog — die Trennung wirkt im Steuerpfad, nicht in der Emissionsbilanz |
| **L12** | Methodenwechsel 01.01.2027 abbilden, beide Rechenwege parallel, Auswahlparameter mit Ausweis im Bericht | **nicht umgesetzt (nur Datenseite)** | Der Katalog führt `EF_NACHWEIS_VERDRAENGUNGSSTROMMIX` und `PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX` mit einer Jahreszeile 2027 **ohne Wert**. **Keine Codezeile liest einen der beiden Schlüssel** (Suche über alle `.cs` außerhalb von `DbWerte.cs`/`GesetzKatalog.cs`: 0 Treffer). Es gibt keinen zweiten Rechenweg, keinen Auswahlparameter und keinen Berichtsausweis. Als offener Punkt 3 im Umsetzungsstand geführt. Befund **A3** — **am 19.08.2026 umgesetzt**, siehe die Berichtigung im Kopf |
| **L13** | Bilanzierungskonvention Biomasse als Einstellung mit Ausweis im Bericht | **nicht umgesetzt** | Der Seed trägt die Regel (E1), aber es gibt **keine Einstellung** und **keinen Ausweis**. Der Punkt steht auch **nicht** in der Liste offener Punkte des Umsetzungsstands — er ist zwischen E1 und E8 aus dem Blick geraten. Befund **A4** — **am 19.08.2026 umgesetzt**, siehe die Berichtigung im Kopf |

### 2.3 Einheitendisziplin (L3) — eigene Prüfung

Das war der Faktor-10-Fehler der Altanwendung, deshalb im Einzelnen nachgerechnet:

- `MengeInGesetzlicherEinheit` verzweigt über die **Einheit des Katalogsatzes**, nicht über den
  Brennstoff: `EUR_MWH` · `EUR_1000L` · `EUR_1000KG` · `EUR_GJ`.
- Je Liter (`:371-372`): `brennstoffMWh × 1000 / EffHi / 1000` — MWh → kWh → Liter → 1.000 Liter.
  Dimensionsprobe stimmt, und der Divisor ist der **gepflegte** Heizwert, keine Konstante.
- Je Kilogramm (`:379-380`): dieselbe Kette gegen `EffHi` in kWh/kg.
- **Die Verweigerung ist Teil der Disziplin:** Passt die Abrechnungseinheit nicht zur Einheit des
  Satzes, liefert die Funktion `null` und der Aufruf trägt eine Begründung ein (`:373-374`,
  `:384-385`) — statt über eine geratene Dichte umzurechnen. `energy_carrier.density` ist im
  gesamten Bestand leer (21 von 21 Trägern).
- Erdgas wird brennwertbezogen bemessen; umgerechnet wird über `EffHs / EffHi` des Trägers
  (`:355-360`), nicht über den pauschalen Vorschriftenfaktor 1,11. In der Lücke-2-Messung
  ausgewiesen als **Ho/Hi = 1,1048**. Fehlt der Brennwert, wird heizwertbezogen weitergerechnet
  **und gesagt, dass die Entlastung dadurch rund 10 % zu niedrig liegt** (`:362-366`) — die
  konservative Richtung.

**Ergebnis: L3 ist eingehalten.** Es gibt im Steuerpfad keine Einheitenumrechnung an einer
gepflegten Zahl vorbei.

### 2.4 Netto ist verbindlich (L8) — eigene Prüfung

- **Kein hart codiertes `1,19` mehr im gesamten Projekt.** Suche über alle `.cs`: die einzigen
  Treffer sind zwei **Kommentare**, die den Altfehler beschreiben (`DbWerte.cs:966`,
  `GesetzKatalog.cs:1016`) und ein Doku-Kommentar in `Views/Kosten/Form_Betriebskosten.cs:20`.
- Der Satz kommt aus dem Katalog (`GESETZ_UMSATZSTEUER_REGELSATZ`, 19,0 % ab 2007) und wird an
  **genau einer** Stelle gelesen: `Form_Betriebskosten.cs:76`, für die Bruttospalte der Anzeige.
- **Im Wirtschaftlichkeitspfad kommt Umsatzsteuer nicht vor.** Weder `WirtschaftlichkeitCtrl` noch
  `KapitalwertRechner` noch `KwkgSatzRechner` lesen den Schlüssel. Der KWK-Zuschlag wird damit
  nirgends mit Umsatzsteuer multipliziert (Befund 4 der Altanwendung).

**Ergebnis: L8 ist eingehalten.**

### 2.5 Datenmodell (Konzept Abschnitt 3)

| Konzeptzeile | Stand |
|---|---|
| `Tab_Gesetzesparameter` (neu) | **umgesetzt** (E1, ohne Migrationsschritt — begründet) |
| `Tab_ProjektWerte` + `Kostenart`, `Bemessung`, `IstErloes`, `Menge`, `Einheitpreis` | **umgesetzt** (Schritt 19), 13 → 18 Spalten |
| `Tab_ErgebnisBHKW.VbhElektrisch` | **umgesetzt** (Schritt 18) |
| `Tab_ErgebnisBHKWModul.VbhThermisch`, `.VbhElektrisch` | **umgesetzt** (Schritt 18) |
| `Tab_ErgebnisWirtschaftlichkeit.KWKGVbhElektrisch` | **umgesetzt** über `SpalteSicher` (E2, begründet) |
| **`Tab_BHKW`, `Tab_BHKW_STAMM` + `Wartungsbemessung TEXT(20)`** | **nicht angelegt** — Suche über alle `.cs`: 0 Treffer. Befund **A2** |
| `Tab_ProjektTarif` + Leistungsmodell, Staffel, Grundpreis, `GueltigAb` | **umgesetzt** (Schritt 21, 36 Spalten). *Abweichung: Einspeiserolle ohne Leistungsstaffel — im E5-Protokoll 2.2 begründet* |
| **`Tab_Kraftwerkspark` + `CO`, `Staub`, `GueltigAb`, `Quelle`, `ReadOnly`, `Bezugsbasis TEXT(12)`** | **nicht angelegt** — 0 Treffer auf `Bezugsbasis`. Das Konzept nennt den Punkt in Abschnitt 9 selbst als offen („noch offen, Etappe E6 oder später"); er ist aber **nie in die Liste offener Punkte des Umsetzungsstands übernommen** worden. Befund **A5** |
| `Tab_ProjektWirtschaftlichkeit` + Steuerparameter | **umgesetzt** (Schritt 20, sechs Spalten; E5 zwei weitere) |
| `Tab_Energieanlagen` + acht KWKG-Spalten | **umgesetzt** (Schritt 22, 57 → 65 Spalten, alle NULL) |

### 2.6 Rechenkette (Konzept Abschnitt 4)

- **4.1 Betriebskosten (VDI 2067):** alle Positionen der Konzepttabelle sind im
  `BetriebskostenCtrl.Katalog` abgebildet (elf statt zwölf Zeilen — die Wartung wird nicht mehr
  doppelt geführt, im E3-Protokoll 3.2 begründet). Empfehlungsbereiche werden angezeigt. **Der
  Konzept-Vorrang „Prozentangabe schlägt Absolutangabe" ist bewusst ersetzt** durch L7: genau eine
  sichtbar gewählte Bemessung, die übrigen Felder gesperrt. Begründet.
- **4.2 Steuern:** umgesetzt; die Formelzeile des Konzepts trägt inzwischen die aus E4
  zurückgeschriebene Berichtigung (§ 53 auf den **gesamten** BHKW-Brennstoff). Die 2-MW-Grenze wird
  **je Anlage** geprüft (`SteuerGutschriftRechner:428-521`).
- **4.3 Strom und Erlöse:** umgesetzt; die Formelzeile `KWK-Zuschlag je Modul = min(Δ Vbh,
  Jahresdeckel, Restkontingent) × Pel × Satz` ist mit E6 als **Tranchenstaffel** realisiert und trägt
  im Konzept die Berichtigung aus E6. Die Zuordnung „drei Begrenzungen der Altanwendung auf eine
  reduziert" habe ich selbst nachvollzogen — siehe 3.
- **Eingabe (Abschnitt 5):** die drei geforderten Dialoge existieren. Der **letzte Punkt des
  Abschnitts ist nicht erfüllt**: „Anzeigetexte ausschließlich über `MyResource`". Gemessen:

  | Maske | Etappe | `MyResource`-Zugriffe | deutsche Literale |
  |---|---|---|---|
  | `Views/Admin/Form_Gesetzesparameter.cs` | E1 | 48 | **0** |
  | `Views/Kosten/Form_Betriebskosten.cs` | E3 | 41 | **0** |
  | `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs` | E4/E5 | **0** | 23 |
  | `Views/Wirtschaftlichkeit/Form_Tarifstruktur.cs` | E5 | **0** | 30 |
  | `Views/Wirtschaftlichkeit/Form_KwkgModule.cs` | E6 | **0** | 10 |

  Die neun beziehungsweise 81 Ressourcenschlüssel, die E6 und E7 angelegt haben, bedienen den
  **Bericht** (`BausteineWirtschaftlichkeit.cs`, `ExcelBerichtGenerator.cs`), nicht die Dialoge.
  **Kein Protokoll benennt diese Abweichung** — sie ist damit unbegründet. Befund **A6**.
- **Administration (Abschnitt 6):** die Maske existiert (E1). Von den drei ausdrücklich genannten
  Wertegruppen, die „pflegbar werden" sollten, sind zwei erledigt (KWKG-Stichtage und -Grenzen mit
  Code-Rückfallebene; Umsatzsteuer). **Die Stromsteuersätze in `Model/StromAufschlagModel.cs:25-70`
  sind weiterhin `const double`** — und sie sind kein toter Bestand: Der Aufschlagsblock der E5
  rechnet mit ihnen (`STROMSTEUER_REGELFALL = 2.050` ct/kWh, in der Lücke-2-Messung als Bestandteil
  der 11,746 ct/kWh sichtbar). Befund **A7**, siehe auch 8 („Doppelte Wahrheiten").

### 2.7 Verifikation (Konzept Abschnitt 8)

| Konzeptforderung | Stand |
|---|---|
| **Zahlenprobe gegen die Altanwendung** — das Beispiel aus dem Erlös-Screenshot (Bedarf 100 MWh, Restbezug 62, Einspeisung 34, Eigenverbrauch 38; vermiedene Kosten 3.657 / −341 / 3.316 €; Einspeiseerlös 1.028 €; Zuschlag 5.488 und 3.059 €) | **nicht erfüllt** — keine der sieben Etappen hat dieses Beispiel gerechnet. Suche nach jeder der sechs Zahlen über alle sieben Protokolle: **0 Treffer**. Keine Etappe begründet den Verzicht. Befund **A8** |
| Referenzlauf byte-identisch je Etappe außer E2 | **erfüllt**, in jeder Etappe und erneut in E8 (Abschnitt 6) |
| Reflection-Harnisch mit Dialogwächter | **erfüllt**, in E1, E3 und jetzt E8 |
| Build 0 Fehler, exakt 6 Bestandswarnungen | **erfüllt** (V1) |

**A8 ist der schwerwiegendste Abnahmebefund.** Der Referenzlauf beweist, dass sich am Bestand nichts
geändert hat; die Handrechnungen der Etappen beweisen, dass die neuen Formeln in sich stimmen. Was
**keine** Probe zeigt, ist, dass die neue Kette dieselbe Aufgabe löst wie die Anwendung, die sie
ablöst. Genau dafür war die Zahlenprobe im Konzept vorgesehen.

---

## 3 Die 17 Fehler der Altanwendung — keiner nachgebaut

Die Einzelprüfung stammt aus einem Vorlauf zu dieser Etappe; **13 sind als NICHT NACHGEBAUT belegt,
4 als NICHT ANWENDBAR** (8 `spezVerKosten`, 9 EEG-Formel, 13 Pauschale ≤ 2 kW, 16
Blattbeschriftungen — die zugehörigen Funktionen existieren im heutigen Modul nicht). Die Belege
sind mit Fundstelle übernommen. **Zwei habe ich selbst nachvollzogen**, weil dort mehrere Rechenwege
zusammenlaufen:

**Befund 15 — „drei widersprüchliche Begrenzungen des KWK-Bonus".** Nachgeprüft:
- `KapitalwertRechner.cs` enthält **keine** eigene Vbh-, Kontingent- oder Deckelungslogik. Alle
  Treffer auf „Vbh/Kontingent/Deckel/KWKG" sind Kommentare und der Reihenname
  `ErloesReihe.KWKG = "KWKG_ZUSCHLAG"`. Gerechnet wird nur `einnahmen += reihe.Wert(t)` (`:286`) —
  die Reihe kommt bereits gedeckelt an.
- Die Deckelung entsteht an **einer** Stelle: `BaueKwkgReihe` liefert Reihe **und** Jahr-1-Wert aus
  demselben Aufruf (`WirtschaftlichkeitCtrl.cs:1259-1261`), und in beiden Rechenwegen gilt
  `jahr1 = reihe[1]` (`:1746` projektweit, `:1889` je Anlage). Die Einzeljahresanzeige ist damit
  konstruktiv das erste Jahr der gedeckelten Reihe — genau das, was Konzept 4.3 fordert.
  → **nicht nachgebaut.**

**Befund 10 — „Stromsteuer doppelt".** Nachgeprüft:
- Die Befreiung nach § 9 Abs. 1 Nr. 3 rechnet ausschließlich mit dem **Eigenverbrauch**:
  `r.StromsteuerBefreiungEur = regelsatz × e.KwkEigenMWh × anteil`
  (`SteuerGutschriftRechner.cs:518`).
- Der zweite Auftritt der Stromsteuer ist die **Belastung im Bezugspreis**, bemessen am
  **Netzbezug** (`WirtschaftlichkeitCtrl.RechneAufschlaege`). Verschiedene Mengen, keine
  Überschneidung.
- Der Abgleich wird im Klartext ausgewiesen (`:1454-1457`) — in meiner Lücke-2-Messung wörtlich:
  „Stromsteuer: Belastung 2,050 ct/kWh im Bezugspreis und Entlastung nach § 9b als Gutschrift —
  kein Doppelansatz, sondern die zwei Seiten derselben Vorschrift."
  → **nicht nachgebaut.**

**Anmerkung zur Belastbarkeit:** Die übrigen 15 Einordnungen sind übernommen, nicht von mir
nachgerechnet. Die Fundstellen stehen im Umsetzungsstand und in den Etappenprotokollen.

---

## 4 Die vier Prüflücken — gemessen und geschlossen

Alle Messungen liefen über einen Reflection-Harnisch (`Harness.exe`) mit **Dialogwächter** (CBT-Hook
auf Fensterklasse `#32770`) gegen **Wegwerfkopien** einer migrierten Datenbank. Der Harnisch bricht
ab, wenn der Zielpfad auf `%ProgramData%` zeigt. Die produktive Datenbank ist in keiner Messung
beschrieben worden (V8).

### 4.1 Lücke 1 — der Fall, in dem E6 überhaupt wirkt

**Warum die Lücke besteht:** Bei Projekt 1030 liegen **beide** Module über dem Jahresdeckel; dort ist
die Summe der Modulreihen algebraisch die projektweite Reihe. Ein Rückschritt auf den projektweiten
Rechenweg fiele im Referenzlauf **nicht** auf.

**Messung 1a — Module treffen den Deckel unterschiedlich** (E6-Fall H3, auf Datenebene nachgebaut:
großes Modul auf 500 MWh / 2.000 h, Aggregat entsprechend auf 873,78 MWh / 2.912,60 h):

```
Modul 1  BHKW EW M 50 S [K] Erdgas    50 kW   373,78 MWh   Vbh 7.475,69 h   → Jahr 1  6.199,93 €
Modul 2  Agenitor 306 (250 kW el)    250 kW   500,00 MWh   Vbh 2.000,00 h   → Jahr 1 20.000,00 €

Handrechnung projektweit (Stand vor E6) :      34.951,20 €/a
Handrechnung je Modul     (Stand E6/E7) :      26.199,93 €/a
gemessen                                :      26.199,93 €/a
Abweichung                              :      −8.751,27 €/a   (−25,04 %)
```

Beide Handrechnungen habe ich unabhängig nachvollzogen: Modul 1 ist gedeckelt
(14.951,20 € × 3.100/7.475,69 = 6.199,93 €), Modul 2 liegt mit 2.000 h **unter** dem Deckel von
3.100 h und bekommt den vollen Betrag; projektweit ergibt der leistungsgewichtete Mittelwert
2.912,60 h < 3.100 h und damit **gar keine** Deckelung. **6 PASS, 0 FAIL, 0 Dialoge.**
Der Wert deckt sich mit den −25,0 % des E6-Protokolls.

**Messung 1b — derselbe Wirkungsfall über den gepflegten Weg**, also über die E6-Spalte
`Tab_Energieanlagen.KWKG_Vbh_Jahresdeckel`, auf **frisch simuliertem** Ergebnis und mit
Strommatrix — ohne jede Präparation der Ergebniszeilen:

```
A  ohne Anlagen-Deckel :  44.265,22 €/a   (7.377,46 + 36.887,76)
B  Anlage „Agenitor 306" KWKG_Vbh_Jahresdeckel = 6.000 h
B  mit Anlagen-Deckel  :  71.456,97 €/a   (7.377,46 + 64.079,51)
Δ                      : +27.191,75 €/a  (+61,43 %)   ·   Δ Kapitalwert +24.406,66 €
```

Das kleine Modul bleibt zeichengleich gedeckelt, das große wird ungedeckelt (Jahr-1-Wert × Vbh/Deckel
auf vier Nachkommastellen getroffen), und die Projektsumme ist exakt die Summe der Modulwerte.
**7 PASS, 0 FAIL, 0 Dialoge.**

> **Damit ist belegt, was der Referenzlauf nicht belegen kann:** Die modulscharfe Rechnung ist nicht
> nur anders gebaut, sie liefert in der Konstellation, für die sie gemacht ist, ein anderes Ergebnis —
> und zwar über den **gepflegten** Weg, nicht nur über präparierte Ergebniszeilen.

### 4.2 Lücke 2 — die volle E4/E5-Kette bei gepflegten Angaben

Auf einer Wegwerfkopie wurden gesetzt: `Unternehmensart = PROD_GEWERBE`, räumlicher Zusammenhang und
Hocheffizienz bestätigt, Jahresnutzungsgrad 85 %, `Energiesteuer_Wahl = PARAGRAF_53A`,
`Aufteilung_Methode = VOLLER_BRENNSTOFF`, `Aufschlaege_Anwenden = WAHR`,
`Einspeiseverguetung_KWK = 0,08 €/kWh`, Tarif-Modus `ROLLEN` (Bezug `JAHRESHOECHSTLAST`, Reststrom
`STAFFEL`, alle vier Staffelstufen gepflegt).

| Größe | A (Vorgabezustand) | B (alles gepflegt) | Δ |
|---|---:|---:|---:|
| Energiesteuer § 53a Abs. 5 | 0,00 | **21.598,65** | +21.598,65 |
| Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3 | 0,00 | **28.564,62** | +28.564,62 |
| Stromsteuer-Entlastung § 9b | 0,00 | **61.150,17** | +61.150,17 |
| KWK-Zuschlag Jahr 1 | 44.265,22 | 44.265,22 | **0,00** |
| Einspeiseerlös | 0,00 | **26.134,51** | +26.134,51 |
| vermiedene Kosten Arbeit | 0,00 | **255.616,68** | +255.616,68 |
| vermiedene Kosten Leistung | 0,00 | **−96.753,77** | −96.753,77 |
| Energiekosten | 1.124.957,70 | **1.817.164,42** | +692.206,72 |
| Kapitalwert | −21.443.872,69 | **−31.669.532,02** | −10.225.659,33 |

**5 PASS, 0 FAIL, 0 Dialoge.** Bemerkenswert und geprüft:

- **Die drei Steuerwerte treffen die Handrechnungen des E4-Protokolls auf den Cent** — 21.598,65 €
  (= 4.423,19 MWh × 1,1048 × 4,42 €/MWh), 28.564,62 € (= 20,50 × 1.393,3959 MWh), 61.150,17 €
  (= 20,00 × 3.070,0086 − 250). Die E4-Fälle F1/F2 sind damit auf dem heutigen Stand
  **reproduzierbar**, nicht nur behauptet.
- **Der negative Leistungsanteil der vermiedenen Kosten ist der Regelfall**, wie E5 sagt: −96.753,77 €
  gegen +255.616,68 € Arbeit.
- **Der Vorgabezustand liefert durchgehend 0 €** — E4 und E5 sind für Bestandsprojekte
  ergebnisneutral, hier erneut gemessen statt zitiert.
- Der Klartext trägt sämtliche geforderten Nachweise: Tarifnachweis mit beiden Leistungsmodellen,
  Einspeiseerlös getrennt vom Zuschlag, KWKG je Modul, Aufschlagszerlegung und den
  Stromsteuer-Abgleich.

### 4.3 Lücke 3 — der Ergebnisreiter `UcWirtschaftlichkeit`, interaktiv

Vergleichsgruppe Stamm 1030 + Varianten 1018 und 1017; Reiter in einem echten `Form` geöffnet,
Variantenliste angehakt (der Klickpfad des Anwenders), Szenarien durchgeschaltet.

```
Grid: 4 Spalten, 15 Zeilen · Variantenliste 3 Einträge · Parameter- und Statuszeile beschriftet
Zeilen im Reiter: 15 — davon aus WirtschaftlichkeitZeilen: 13, reiterspezifisch: 2
Wertvergleich Reiter ↔ zentraler Zeilendefinition: 39/39 zeichengleich
Szenariowechsel Best / Worst / Erwartet: je 15 Zeilen, 4 Spalten; Rückkehr identisch
Schalter btnTarif / btnParameter / btnVerlauf / btnBerechnen: aktiv und beschriftet
Dialogwächter: 0 Meldungen
```

**17 PASS, 0 FAIL.** Die von E7 zugesagte Wertgleichheit zwischen Reiter und zentraler
Zeilendefinition ist damit **gemessen**: 39 von 39 Werten zeichengleich.

**Dabei aufgefallen — Befund B1:** Die beiden reiterspezifischen Zeilen tragen **denselben Titel
„Hinweis"**. `UcWirtschaftlichkeit.cs:573-576` legt eine Zeile für `x.Hinweis` und eine zweite für
`x.Fehlgrund` an, beide mit dem Literal `"Hinweis"`. Sobald in einer Vergleichsgruppe beides vorkommt
— der Regelfall, wenn der Stamm rechnet und Varianten mangels Arbeitspreis nicht —, stehen zwei
gleich beschriftete Zeilen untereinander und der Leser kann sie nicht unterscheiden. Ohne
Rechenwirkung. Nicht behoben, Begründung in 5.2.

### 4.4 Lücke 4 — der mehrspaltige Variantenpfad im Bericht

Auf einer Wegwerfkopie eine Variantengruppe angelegt: Stamm 1030 + vier Varianten (1018, 1017, 1008,
1021). Erzeugt wurden Word **und** Excel mit allen Bausteinen einschließlich `wirtschaftlichkeit`
(im Standardkatalog nicht aktiv — sonst fiele genau der Teil weg, um den es geht).

```
gesammelt: 5 Projekte, 15 Ergebniszeilen
VariantenBloecke: 2 Blöcke, Aufteilung 3 + 1  (MAX_VARIANTEN_JE_BLOCK = 3)
Word : E8_Varianten.docx   743.480 Byte
Excel: E8_Varianten.xlsx    33.359 Byte
Dialogwächter: 0 Meldungen
```

**5 PASS, 0 FAIL.** Der mehrspaltige Pfad trägt: `WordKontext.VariantenBloecke`
(`WordBerichtGenerator.cs:381`) zerlegt vier Varianten korrekt in zwei Blöcke und wiederholt die
Stammspalte; beide Generatoren laufen ohne Ausnahme durch. Die 19 protokollierten Warnungen sind
sämtlich Datenpflegehinweise der Varianten (fehlende Energieträger, fehlende Temperaturpaare,
„Energiekosten nicht bestimmbar"), keine Berichtsfehler.

> **Was diese Messung nicht zeigt:** Sie prüft, dass der Pfad **läuft** und die Blockzerlegung
> stimmt — nicht, dass jede Zelle im mehrspaltigen Layout inhaltlich richtig steht. Eine
> Zell-für-Zell-Prüfung des Variantenlayouts gegen die Einzelberichte ist nicht erfolgt.

---

## 5 Befunde

### 5.1 Behoben in dieser Etappe

**Keine.** E8 hat keine Codezeile geändert. Alles Gefundene ist entweder Dokumentation (Abschnitt 8)
oder unter 5.2 als Befund ausgewiesen.

### 5.2 Dokumentiert, nicht geändert

| # | Befund | Wirkung | Warum nicht hier behoben |
|---|---|---|---|
| **A1** | **Keine der neuen Rechenklassen hat einen Test.** `SteuerGutschriftRechner`, `StromTarifRechner`, `KwkgSatzRechner` und `KapitalwertRechner` kommen in `SpeicherEngine.Tests` und `KiKern.Tests` nicht vor. L9 verlangt ausdrücklich „Tests im vorhandenen Testprojekt" | keine Rechenwirkung — aber **jede** Messung der Ausbaustufe ist ein Einzelnachweis aus einem Wegwerf-Harnisch. Beim nächsten Build läuft nichts davon mit | ein Testprojekt zu füllen ist Umsetzung, nicht Abnahme; Umfang und Schnitt sind zu entscheiden |
| **A2** | `Tab_BHKW`/`Tab_BHKW_STAMM.Wartungsbemessung TEXT(20)` aus dem Konzept-Datenmodell ist **nicht angelegt** | keine — die Sache (genau eine Bemessung, L7) ist über `Tab_ProjektWerte.Bemessung` erfüllt | Schemaänderung; und der gewählte Weg ist der bessere (die Bemessung gehört zur Kostenposition, nicht zum Gerätekatalog). Das **Konzept** ist zu berichtigen, nicht der Code — erledigt, siehe 8 |
| **A3** | **L12 ist nur Datenseite.** Die 2027er-Zeilen des Verdrängungsstrommix stehen im Katalog, **kein Code liest sie**; kein zweiter Rechenweg, kein Auswahlparameter, kein Berichtsausweis | ab 01.01.2027 rechnet die Emissionsbilanz unverändert weiter, ohne dass jemand die Methodenfrage gestellt bekommt. Das Konzept nennt L12 „für BHKW-Projekte die folgenreichste Änderung des gesamten Vorhabens" | eigene Ausbaustufe mit fachlicher Entscheidung (offener Punkt 3) — **erledigt am 19.08.2026** |
| **A4** | **L13 ist nirgends umgesetzt** — keine Einstellung, kein Ausweis für die Biomasse-Bilanzierungskonvention. Der Punkt stand bis heute **auch nicht** in den offenen Punkten | Projekte mit biogenem Brennstoff bekommen eine Konvention, ohne dass sie benannt wird | fachliche Entscheidung; ab jetzt als offener Punkt geführt (siehe 8) — **erledigt am 19.08.2026** |
| **A5** | **`Tab_Kraftwerkspark.Bezugsbasis`** und die vier weiteren Konzeptspalten sind nicht angelegt. Der Definitionsbruch des Altkatalogs (Faktoren je kWh Brennstoff und je kWh Strom in derselben Spalte) besteht fort | die Emissionsbilanz kann Faktoren verwechseln, ohne dass es auffällt | Schemaänderung mit Datenpflege; das Konzept nennt ihn selbst als offen — er war nur nie in der Offene-Punkte-Liste. Ab jetzt geführt |
| **A6** | **Drei der fünf neuen Masken tragen deutsche Anzeigetexte als Literal** (`Form_WirtschaftlichkeitParameter` 23, `Form_Tarifstruktur` 30, `Form_KwkgModule` 10), obwohl Konzept Abschnitt 5 „ausschließlich über `MyResource`" verlangt. **Kein Protokoll begründet die Abweichung** | die neuen Dialoge erscheinen auf englischer Oberfläche deutsch | 63 Texte in beide `.resx` plus Designer ist eine Umsetzungsetappe. **Kein Verstoß gegen die Drei-Schichten-Regel im engeren Sinn** — es sind Anzeigetexte, die als Anzeige verwendet werden, keine Steuerwerte |
| **A7** | **Die Stromsteuersätze in `Model/StromAufschlagModel.cs:25-70` sind weiterhin Konstanten**, obwohl Konzept Abschnitt 6 sie ausdrücklich als „pflegbar zu machen" nennt. `STROMSTEUER_REGELFALL = 2.050` ct/kWh steht neben dem Katalogwert `STROMST_REGELSATZ = 20,50 €/MWh` — **derselbe Satz an zwei Orten** | heute wertgleich. Wird im Katalog ein neues Jahr gepflegt, rechnet der Aufschlagsblock still mit dem alten Satz weiter | betrifft das Stromspeicher-Modul und den Aufschlagsblock, nicht die W4-Rechenkette. Neue doppelte Wahrheit, ab jetzt geführt |
| **A8** | **Die Zahlenprobe gegen die Altanwendung (Konzept 8, erster Spiegelstrich) wurde nie gerechnet.** Keine der sechs Zahlen des Erlös-Screenshots kommt in einem der sieben Protokolle vor | Es gibt **keinen** Nachweis, dass die neue Kette dieselbe Aufgabe löst wie die abgelöste Excel-Anwendung. Die Handrechnungen prüfen die Formeln gegen sich selbst, nicht gegen das Vorbild | die Probe braucht die Eingangsgrößen des Screenshots als Projekt und eine Bewertung jeder Abweichung gegen die 17 Befunde — das ist ein eigener Vorgang, kein Abnahmeschritt |
| **B1** | `UcWirtschaftlichkeit.cs:573-576` beschriftet **zwei** Zeilen mit `"Hinweis"` (eine für `Hinweis`, eine für `Fehlgrund`) | Anzeige; in einer Vergleichsgruppe mit beidem sind die Zeilen nicht unterscheidbar | Der saubere Weg braucht einen neuen Ressourcenschlüssel in beiden `.resx` **und** im Designer — laut `CLAUDE.md` nicht von Hand zu pflegen. Ein zweites deutsches Literal würde A6 vertiefen. **Empfehlung: mit der Behebung von A6 zusammen erledigen** |

### 5.3 Geprüft und in Ordnung

- **Drei-Schichten-Regel, Schicht 2 (P2 „Anzeigetext als Steuerwert"):** über
  `Allgemein/Wirtschaftlichkeit`, `Views/Wirtschaftlichkeit`, `Views/Kosten`, `Views/Admin` und
  `Allgemein/Bericht` **zwei** Treffer, beide unbedenklich:
  `Form_Gesetzesparameter.cs:698-699` liest `SelectedItem.ToString()` — die ComboBoxen sind aber mit
  **`DbWerte`-Konstanten** befüllt (`:531`, `:547-552`), der gelesene Wert ist also der
  Persistenzwert, kein Anzeigetext. **Kein Anzeigetext ist zum Steuerwert geworden.**
- **Drei-Schichten-Regel, Schicht 1 (P3 „Persistenzwert als Literal"):** kein W4-Treffer. Die
  gemeldeten Fundstellen sind Bestand außerhalb der Ausbaustufe (Diagrammserien `"BHKW"`,
  `"Photovoltaik"`, Tabellennamen, Gruppenvorgabe `"Allgemein"`) und stammen sämtlich aus früheren
  Phasen.
- **Migrationskette 18 bis 22** läuft auf der Arbeitskopie fehlerfrei und idempotent durch; die vier
  Vorbelegungsschritte melden auf einer bereits gepflegten Datenbank jeweils **0 vorbelegte Zeilen**
  („der Rechenweg bleibt unverändert").

---

## 6 Neue Referenzbasis B6

**Referenzlauf** über `--projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030`, Arbeitskopie-Mechanismus,
Codestand `e94be10` aus einem `git archive HEAD`-Export außerhalb des Repos.

```
vergleich 2026-08-19_B5  <E8-Lauf>
  9 von 9 Projekten PASS · GESAMT: PASS (2 366 177 Werte innerhalb der Toleranz)
  Byte-/MD5-Vergleich: 216 von 216 Dateien gleich, 0 abweichend
```

**216/216 byte-identisch — wie erwartet.** Selbstvergleich der neuen Basis: zweiter Lauf desselben
Codes auf derselben Quelle ebenfalls **9/9 PASS, 216/216 byte-gleich**.

**Eingefroren als [`Referenzlaeufe/2026-08-19_B6/`](../../../Referenzlaeufe/2026-08-19_B6/lauf_protokoll.md)**,
216 CSV, 32 MB, keine `.accdb`. `LIESMICH.md` der Suite ist auf B6 umgestellt.

> **Ein Nebenbefund zur Quelle, der festgehalten gehört:** Die produktive Datenbank ist seit B5
> **nicht mehr dieselbe Datei** — sie ist von 96 436 224 Byte / MD5 `66F4806A…` / Schemastand 17 auf
> 92 700 672 Byte / MD5 `0873B892…` / Schemastand **21** gewandert, weil die Sitzung des Anwenders
> die Migrationsschritte 18 bis 21 ausgeführt und die Datei um 14:46 komprimiert hat. **Dass die
> Ergebnisse trotzdem byte-identisch sind, ist der eigentliche Nachweis** der Ergebnisneutralität
> dieser Schritte — stärker als ein Lauf auf unveränderter Quelle ihn hätte führen können.

### Was diese Basis **nicht** absichert

Die vollständige Liste steht im Laufprotokoll der Basis. Die vier wichtigsten:

1. **Die Wirtschaftlichkeitsrechnung überhaupt nicht.** Der Referenzlauf ruft sie nicht auf.
   Kapitalwert, KWK-Zuschlag, Steuergutschriften, Tarife und Betriebskosten stehen in **keiner**
   eingefrorenen Basis.
2. **Den Wirkungsfall der Etappe E6** (Lücke 1) — kein Referenzprojekt hat Module, die den
   Jahresdeckel unterschiedlich treffen.
3. **Die gesamte E4/E5-Kette** (Lücke 2) — kein Referenzprojekt pflegt Steuer- oder Tarifangaben.
4. **Den mehrspaltigen Variantenpfad** (Lücke 4) und die **VDI-2067-Bemessungsarten** aus E3.

---

## 7 Vorschlag zur Referenzmenge — **zur Entscheidung, nicht ausgeführt**

Die Lücken 1, 2 und 4 sind heute gemessen, aber nicht **dauerhaft** abgedeckt: Meine Messungen liefen
auf Wegwerfkopien und sind mit ihnen gelöscht. Für eine Regressionsabdeckung müsste die produktive
Datenbank ergänzt werden. **Das habe ich nicht getan** — die Datenbank ist strikt read-only.

### Vorschlag V1 (empfohlen): ein zehntes Referenzprojekt **1031**

**„Referenz BHKW-Kaskade, ungleich gedeckelt und voll gepflegt"** — als Kopie von 1030, damit 1030
seine heutige Rolle (beide Module gedeckelt, die Positivseite beider KWKG-Guards) unverändert behält.

| Tabelle | Feld | Wert | Was dadurch abgesichert wird |
|---|---|---|---|
| `Tab_Energieanlagen` (Anlage „Agenitor 306", 250 kW) | `KWKG_Vbh_Jahresdeckel` | **6000** | **Lücke 1:** die Module treffen den Deckel unterschiedlich. Erwartet: KWK-Zuschlag Jahr 1 **44.265,22 → 71.456,97 €/a (+61,43 %)**, Kapitalwert +24.406,66 € |
| `Tab_ProjektWirtschaftlichkeit` | `Unternehmensart` | `PROD_GEWERBE` | **Lücke 2:** § 9b wird wirksam — erwartet **61.150,17 €/a** |
| | `Hocheffizienz_Nachweis` | WAHR | § 9 Abs. 1 Nr. 3 wird wirksam — erwartet **28.564,62 €/a** |
| | `Raeumlicher_Zusammenhang` | WAHR | zweite Bedingung derselben Befreiung |
| | `Jahresnutzungsgrad` | **85** | Schwelle des § 53a (70 %) wird überschritten |
| | `Energiesteuer_Wahl` | `PARAGRAF_53A` | Energiesteuergutschrift — erwartet **21.598,65 €/a** |
| | `Aufteilung_Methode` | `VOLLER_BRENNSTOFF` | das rechtlich belegte Verfahren |
| | `Einspeiseverguetung_KWK` | **0,08** | Einspeiseerlös ohne PV — erwartet **26.134,51 €/a** |
| | `Aufschlaege_Anwenden` | **FALSCH** | bewusst AUS lassen: die Entscheidung steht beim Nutzer aus (offener Punkt), und ein Referenzprojekt darf sie nicht vorwegnehmen |
| `Tab_ProjektTarif` | `Tarif_Modus` | `ROLLEN` | **Lücke 2:** Rollenmodell und Differenzmethode |
| | `Bezug_Leistungsmodell` / `Rest_Leistungsmodell` | `JAHRESHOECHSTLAST` / `STAFFEL` | zwei der drei Leistungspreismodelle in einem Projekt |
| | Arbeits-, Grund- und Staffelpreise | wie in 4.2 gemessen | vermiedene Kosten Arbeit **+255.616,68 €**, Leistung **−96.753,77 €** |
| `Tab_ProjektWerte` | eine Position auf `Bemessung = PROZENT_INVESTITION`, eine auf `EUR_PRO_KWH` | | **E3:** die Bemessungsarten wirken erstmals in einem Referenzprojekt |
| `Tab_Variante` | 1031 als Stamm mit **vier** Varianten | | **Lücke 4:** `VariantenBloecke` erzeugt zwei Blöcke (3 + 1) |

**Kosten und Nebenwirkungen, ehrlich benannt:**
- Die Referenzliste wächst auf **zehn** IDs; jeder künftige Lauf braucht
  `--projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1031`. `Projektauswahl.MAX_PROJEKTE = 9`
  begrenzt **nur die automatische Auswahl** und steht dem nicht im Weg.
- +22 CSV, rund +4 MB, und ein **Basiswechsel auf B7** (die Basis bekommt einen Ordner mehr).
- Die vier Varianten müssen Projekte sein, die **nicht** schon Variante eines anderen Stamms sind
  (`Tab_Variante.ID_Projekt` ist eindeutig); im Bestand scheiden 1023 und 1024 deshalb aus.

### Vorschlag V2 (minimalinvasiv, nur Lücke 1)

Statt eines neuen Projekts an der vorhandenen Anlage „Agenitor 306" in **1030**
`KWKG_Vbh_Jahresdeckel = 6000` setzen. Kein neues Projekt, kein Basiswechsel, keine CSV-Änderung
(die Simulation bleibt unberührt — nachgewiesen in Messung 1b).

**Dagegen spricht:** 1030 verlöre seine heutige Eigenschaft „beide Module gedeckelt" — und damit den
einzigen Beleg dafür, dass die alte projektweite Rechnung in diesem Fall zufällig richtig war. Ich
empfehle V1.

**Beides ist nicht ausgeführt.** Vor einer Umsetzung gehört eine datierte Sicherung der
`Kenndaten.accdb` angelegt und geprüft, dass keine `Kenndaten.laccdb` daneben liegt.

---

## 8 Was Etappe E8 an den Dokumenten geändert hat

- **`Konzept_BHKW_Kosten_Erloese.md`** ist an vier Stellen auf den tatsächlichen Stand gebracht: die
  im E4-Protokoll widerlegte Annahme zur Brennstoffaufteilung in Abschnitt 5 (das dort noch genannte
  Formular „1131a" existiert nicht), die Datenmodellzeile `Wartungsbemessung`, L10 und der
  Etappenplan. Einzelheiten in der Änderungsliste des Konzepts selbst.
- **`W4_Umsetzungsstand.md`** trägt E8, den Abschnitt „Was Etappe E8 entschieden hat", die
  abgeschlossene Etappentabelle und die fortgeschriebenen Listen „Offene Punkte" und „Doppelte
  Wahrheiten".
- **`Referenzlaeufe/LIESMICH.md`** ist auf die Basis B6 umgestellt, samt der neuen Warnung, dass der
  Referenzlauf den Rechenkern absichert und **nicht** die Wirtschaftlichkeit.

---

## 9 Verifikation

| # | Prüfung | Ergebnis |
|---|---|---|
| V1 | Build `WP-Plan.sln`, x86/Debug, eigener `OutDir` | **0 Fehler, exakt 6 Bestandswarnungen** (WErzeugerModel CS0108, StromverbraucherStammCtrl CS0108, KlimaregionStammCtrl CS0109 ×2, MDIMainForm CS4014 + CS1998). Die drei neuen Projekte `KiKern`, `KiKern.Tests`, `KiHarnisch` bringen keine eigene Warnung |
| V2 | Referenzlauf gegen `2026-08-19_B5` | **9/9 PASS**, 2 366 177 Werte |
| V3 | Byte-/MD5-Vergleich gegen B5 | **216/216 gleich, 0 abweichend** |
| V4 | Selbstvergleich der Basis B6 | **9/9 PASS, 216/216 byte-gleich** |
| V5 | Migration der Arbeitskopie | Schemastand **21 → 22**, Schritt 22 „OK" (8 Spalten), Schritte 19/20/21 „bereits erledigt" mit **0 vorbelegten Zeilen** |
| V6 | Harnisch Lücke 1a / 1b / 2 / 3 / 4 | **6 / 7 / 5 / 17 / 5 PASS, 0 FAIL** — zusammen **40 Proben ohne Fehlschlag** |
| V7 | Dialogwächter über alle fünf Läufe | **0 unerwartete Meldungen** |
| V8 | Produktive `Kenndaten.accdb` vor und nach allen Läufen | Größe 92 700 672 Byte, Zeitstempel 19.08.2026 14:46:27.810, MD5 `0873B892ADFEE0DC266DBC0814EB93A7` — **unverändert**; keine `Kenndaten.laccdb` vorhanden; Schemastand weiterhin **21** |
| V9 | `bin\` des Repos | unberührt — Build ausschließlich mit `-p:OutDir=<Scratch>`, Referenzlauf und Harnisch aus einem `git archive`-Export außerhalb des Repos |
| V10 | Drei-Schichten-Regel P2 im W4-Bereich | 2 Treffer, beide unbedenklich (ComboBox mit `DbWerte`-Konstanten befüllt) |
| V11 | Drei-Schichten-Regel P3 im W4-Bereich | kein W4-Treffer |
| V12 | Hart codierte `1,19` im gesamten Projekt | **0** (nur Kommentare, die den Altfehler beschreiben) |

**Arbeitsumgebung:** Export `C:\Waermeplan\_e8` (`git archive HEAD`), Wegwerfdatenbanken unter
`C:\Waermeplan\_e8\dbs\{l1a,l1b,l2,l3,l4}`, Berichte unter `C:\Waermeplan\_e8\berichte`. Nichts
davon liegt im Repo.

---

## 10 Restliste — was nach W4 offen bleibt

**Mit Rechenwirkung oder fachlicher Entscheidung:**

1. **Zahlenprobe gegen die Altanwendung** (A8) — der einzige fehlende Nachweis, dass die neue Kette
   das Vorbild trifft.
2. ~~**L12, Methodenwechsel 2027** (A3)~~ — **am 19.08.2026 umgesetzt**
   ([`W4_L12_L13_Methodenwechsel_Protokoll.md`](W4_L12_L13_Methodenwechsel_Protokoll.md)). Neu
   offen daran: Der Wechsel greift über die Projektangabe `Bilanz_Jahr`, **nicht automatisch zum
   01.01.2027** — begründet mit der Reproduzierbarkeit gespeicherter Rechnungen, und ausdrücklich
   zur Entscheidung gestellt.
3. ~~**L13, Biomasse-Konvention** (A4)~~ — **am 19.08.2026 umgesetzt**, mit dem Nachweis, dass die
   bisherige Konvention in den Katalogwerten des Brennstoffs stand. Bio-Heizöl-Mischungen bleiben
   ausgenommen (kein Feld für den biogenen Anteil).
4. **Aufschläge: Vorgabeverhalten** — gemessen (+32 bis 34 % Energiekosten, −30 bis 33 % Kapitalwert),
   Schalter steht auf AUS, **die Entscheidung steht beim Nutzer aus** (unverändert aus E5).
5. **§ 53 neben § 53a** — rechtlich ungeklärt, als Option modelliert; vor produktivem Einsatz mit dem
   Hauptzollamt klären.
6. **Kategorie 3 „Energiekosten"** — pflegbar, von keiner Rechnung gelesen.
7. **`Tab_Kraftwerkspark.Bezugsbasis`** (A5) — der Definitionsbruch des Altkatalogs besteht fort.

**Technische Schuld ohne Rechenwirkung:**

8. **Keine Tests für die neuen Rechenklassen** (A1) — der gewichtigste Punkt dieser Gruppe.
9. **Lokalisierung der drei neuen Masken** (A6), zusammen mit der doppelten Zeilenbeschriftung
   „Hinweis" (B1).
10. **Stromsteuersatz an zwei Orten** (A7).
11. **Der Bestandsfehler aus Phase 11**: „Differenzdiagramm entfällt — für das Stammprojekt konnte
    keine Zahlungsreihe gerechnet werden" erscheint auch, wenn es schlicht keine Varianten gibt
    (aus E7, unverändert offen; die Berichtigung ist eine Zeile).
12. Die aus den Etappen übernommenen kleineren Punkte: Preissteigerung der Hilfsenergie (E3),
    feinere Bezugsgrößen der VDI-Positionen (E3), Lastbilder nicht persistiert (E5), Varianten mit
    abweichendem Tarif (E5), Kontingent aus der Anlagenart ableiten (E6), Modul-Dialog speichert
    sofort (E6), `KennzahlenKatalog` ohne Anschluss an die Wirtschaftlichkeit (E4/E7).

---

## 11 Abnahmeurteil

**Die Ausbaustufe W4 ist abgenommen.** Was das Konzept als Rechenkette beschreibt, ist gebaut,
gemessen und für den Bestand ergebnisneutral; die vier Prüflücken, die die Vorgängeretappen selbst
benannt haben, sind mit Zahlen geschlossen; der Rechenkern ist nachweislich unberührt.

**Die Abnahme steht unter vier Vorbehalten**, die keine Etappe mehr auflösen kann, ohne selbst zur
Umsetzungsetappe zu werden: die fehlende Zahlenprobe gegen die Altanwendung (A8), die nur
datenseitig vorhandene Umsetzung von L12 (A3), die fehlende L13 (A4) und das vollständige Fehlen
dauerhafter Tests für die neuen Rechenklassen (A1).

> **Nachtrag 19.08.2026:** A3 und A4 sind mit einer eigenen Umsetzung erledigt
> ([`W4_L12_L13_Methodenwechsel_Protokoll.md`](W4_L12_L13_Methodenwechsel_Protokoll.md)) — genau
> auf dem Weg, den dieser Satz vorgezeichnet hat. **Zwei Vorbehalte bleiben: A8 und A1.**

**Wovon ich nicht überzeugt bin und was ich ausdrücklich als Unsicherheit stehen lasse:**

- **Die Belastbarkeit der 15 nicht selbst nachgerechneten Altfehler-Einordnungen.** Ich habe 10 und
  15 geprüft; die übrigen sind übernommen.
- **Ob die Handrechnungen der Etappen die richtige Frage beantworten.** Sie prüfen jede Formel gegen
  ihre eigene Herleitung. Solange A8 offen ist, kann eine systematisch falsche Auslegung des Gesetzes
  in allen Proben gleichzeitig „stimmen".
- **Die inhaltliche Richtigkeit des mehrspaltigen Variantenlayouts.** Lücke 4 belegt, dass der Pfad
  läuft und die Blöcke stimmen — nicht, dass jede Zelle richtig steht.
- **Die Näherungen, die W4 bewusst führt:** der Split Eigenstrom/Einspeisung je Modul (E6), die
  mengenproportionale Aufteilung des Einspeiseerlöses im Rollenmodell (E7), „alles ist
  Eigenverbrauch" ohne Strombedarfsreihe (E4). Alle drei sind im Code benannt — keine ist gegen
  eine Stundenreihe geprüft, weil es die im Modell nicht gibt.
