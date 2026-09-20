# E1 — Nachweisfundament der Wirtschaftlichkeit

**Datum:** 19.09.2026
**Zweig:** `worktree-agent-a4919b05ccb0f1789` (Worktree `.claude/worktrees/agent-a4919b05ccb0f1789`, abgezweigt von `455edfd4`)
**Grundlage:** `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md` § 3.5 (Befunde N1–N4) und § 5 (Etappe E1)
**Stand der Messung:** Testdatenbank `Referenzlaeufe/Kenndaten_Test.sqlite`, Schemastand 96

## Commits

| Kennung | Betreff |
|---|---|
| `cf2b8c88` | E1: Kaskadenrunde 2 in zwei Phasen, reihenfolgeunabhaengig |
| `19655863` | E1: Ankertests Wirtschaftlichkeit, absolute Kapitalwerte gepinnt |
| `efbc3127` | E1: Formatpaar-Waechter fuer WirtZeile.Format/ExcelFormat |
| `5a57308b` | E1: Blattstruktur-Wache ueber Excel- und Wortbericht |
| `c9171488` | E1: Testklasse fuer den SteuerGutschriftRechner |
| `ad2a5547` | E1: Testklasse fuer den EegSatzRechner, 16 BNetzA-Werte gepinnt |
| `4e374c21` | E1: EEG-Faelle des PvErloesRechners (§ 51, Kappung, Praemie, § 51a) |

## Überblick

| Punkt | Klasse | Fälle |
|---|---|---:|
| 1 | `EPOS.Kern.Tests/WirtschaftlichkeitAnkerTests.cs` | 9 |
| 2 | `EPOS.Kern.Tests/SteuerGutschriftRechnerTests.cs` | 39 |
| 3 | `EPOS.Kern.Tests/EegSatzRechnerTests.cs` | 49 |
| 4 | `EPOS.Kern.Tests/PvErloesRechnerEegTests.cs` | 23 |
| 5 | `EPOS.Kern.Tests/BerichtBlattstrukturWacheTests.cs` | 5 |
| 6 | `EPOS.Kern.Tests/WirtZeileFormatWacheTests.cs` | 4 |
| 7 | `EPOS.Kern.Tests/InvestKaskadeTests.cs` (erweitert) | 25 (vorher 23) |
| | **neu insgesamt** | **131** |

---

## 1 Ankertests

`WirtschaftlichkeitAnkerTests`, `[Collection("Testdatenbank")]`, Kulturvorrichtung, 9 Fälle.

### Der Kern-Weg ist vorhanden — ein Sammler war nicht nötig

Die Untersuchung hat den Weg gefunden, `BerichtsDaten` eines echten Projekts **im Kern** zu füllen; er wird im Bestand bereits von sechs Testklassen gefahren (`ErgebnisNachweisPersistenzTests`, `KwkgErsatzwegGewichtetTests`, `KwkAnlagenwahrheitTests`, `KohaerenzNachtraegeTests`, `StromsteuerBefreiungModusTests`, `PreisInvestitionTests`):

```
WirtschaftlichkeitCtrl.LadeParameter(id)
  → new VariantenDaten { Ergebnis = new ErgebnisCtrl().Load(id) }
  → KostenEmissionRechner.Berechne(v)
  → new BerichtsDaten { … }.Varianten.Add(v)
  → new WirtschaftlichkeitCtrl().Berechne(daten, p)
```

Das ist dieselbe Kette, die die Schale über `BerichtsDatenSammler` fährt. **Es wurde kein neuer Kern-Controller und kein nachgebauter Sammler angelegt.**

Zur Frage, welche Schalenlogik in den Kern gehört (Vorgriff auf E3 Schritt 4): `BerichtsDatenSammler.SammleFuerBericht` selbst ist bereits WinForms-frei — es steht keine einzige Windows-API darin. Was ihn heute an die Schale bindet, ist allein seine **Lage** im Projekt `WindowsFormsApplication1` und ein einziger Aufruf, `EnergieMengen.BaueBrennstoffmengen` (`WindowsFormsApplication1/Views/Varianten/EnergieMengen.cs`), der das Feld `VariantenDaten.Brennstoffmengen` füllt. Dieses Feld liest `WirtschaftlichkeitCtrl.Berechne` **nicht**. Für E3 heißt das: Der Sammler kann als Ganzes in den Kern (oder nach `EPOS.UI.Daten`) wandern, sobald `EnergieMengen` mitgeht; die Fortschrittsmeldung (`IProgress<Fortschritt>`, `CancellationToken`) ist keine Windowsbindung und darf bleiben.

### Die gemessenen Anker

| Größe | Konzept § 6.2 | **gemessen** | Abweichung |
|---|---:|---:|---|
| (a) `LiesBetriebskosten(1024, Erwartet)` | 99,00 €/a | **99,00 €/a** | — (trifft) |
| (b) Kaskade 1042 gegen `SUM(EingegebenerWert)` | +20.927,61 € | **±0,00 €** (beide 13.000,00 €) | −20.927,61 € |
| (c) Kapitalwert 1024 (Erwartet, Referenz = Stamm) | −2.220.322,32 € | **−2.896.359,13 €** | −676.036,81 € |
| (c) Kapitalwert 1030 (Erwartet, Referenz = Stamm) | (nicht genannt) | **−21.895.377,28 €** | — |

Toleranz je Anker 0,01 €. Mitgepinnt als Eingrenzungshilfe: Investition 12.001,00 € / 410.000,00 €, Betriebskosten 99,00 € / 20.000,00 €, Energiekosten 188.167,18 € / 1.176.906,60 €.

**Ursache der Abweichung (b).** Kein Rechenfehler, sondern der Datenstand. Projekt 1042 führt acht Kategorie-1-Zeilen; genau **eine** trägt einen Wert (13.000,00 € `BETRAG`). Die drei Prozentzeilen — eine `PROZENT_ERZEUGERKOSTEN` (ID 101600518), zwei `PROZENT_INVESTITION` (101600519, 101600521) — haben **keinen Einheitpreis** und rechnen deshalb 0. Ohne Satz keine Ableitung, ohne Ableitung kein Aufschlag. Die Konzeptzahl stammt von einem Stand, in dem diese Zeilen Sätze trugen.

**Ursache der Abweichung (c).** Nicht nachgerechnet, nur eingegrenzt. Die Konzeptzahl stammt von einem älteren Stand; als Kandidaten benennt der Auftrag Kesselbrennstoff (B-1/#331), Hilfsstrom (#365/#366) und die Schemaschritte 93–96. Der gemessene Wert ist der Anker; die Herleitung der Differenz gehört in E7, wo die Rechenwege ohnehin angefasst werden.

### Grenze: vier der fünf CI-Projekte tragen keinen Anker

1007, 1017, 1045 und 1046 können **keinen** absoluten Kapitalwert tragen, und zwar aus zwei Gründen zugleich:

- `ErgebnisCtrl.Load(id)` liefert `null` — die Testdatenbank führt für sie **keinen gebuchten Ergebnisstand**. Der Referenzlauf simuliert sie je Lauf frisch und schreibt in eine Arbeitskopie.
- Sie haben **keine Kategorie-1-Kostenzeilen**; `InvestKaskade.Summen` ist für alle drei Szenarien 0,00 €.

Ohne Mengengerüst gibt es keine Energiekosten und damit keinen Kapitalwert. Das ist als Theorie festgehalten (`Referenzprojekte_der_CI_tragen_keinen_gebuchten_Stand`, 4 Datensätze) und fällt, sobald eines der Projekte Daten bekommt. Damit läuft die Forderung „mindestens ein absoluter Kapitalwert-Anker in `EPOS.Kern.Tests`" über **zwei** Projekte (1024 und 1030), nicht über fünf.

Ein fünfter Fall hält fest, dass Projekt 1042 zwar Stand und Investition hat, aber keinen Stromträger — der Kern nennt den Grund benannt (`EnergiekostenGrund`), statt still 0 zu rechnen.

---

## 2 `SteuerGutschriftRechnerTests` (neu)

39 Fälle, **rein ohne Datenbank**, Katalogsätze aus `GesetzKatalog.Vorbelegung()`. Beispiel: das BHKW des Rechenwegs 05 — 300 kW_el, 1.650,0 MWh Strom bei 38 %, also 4.342,105 MWh Hi, 1.953,947 MWh Wärme, Hs/Hi = 11,6/10,5.

| Größe | Rechenweg 05 (Mockup) | `01_Nachrechnung.md` § 3 | **gemessen** |
|---|---:|---:|---:|
| Brennstoff Hs | 4.797,2 MWh | 4.796,99 MWh | **4.796,992 MWh** |
| § 53 voller Brennstoff | 26.384,3 € | 26.383,46 € | **26.383,46 €** |
| § 53 energetisch | 12.082 € | 12.079,17 € | **12.079,17 €** |
| § 53a (Nutzungsgrad 83 %) | 21.203,4 € | 21.202,71 € | **21.202,71 €** |
| § 54 (prod. Gewerbe) | 6.370,1 € | 6.369,85 € | **6.369,85 €** |
| § 9 Abs. 1 Nr. 3 | 23.677,5 € | 23.677,5 € | **23.677,50 €** |
| § 9b (Netzbezug 250,0 MWh) | 4.750,0 € | 4.750,0 € | **4.750,00 €** |
| § 9b (Netzbezug 335,5 MWh) | 6.460,0 € | 6.460,0 € | **6.460,00 €** |

Die Spalte „Mockup" weicht ab, weil sie über den **gerundeten** Brennwertfaktor 1,1048 rechnet (Befund B4 der Mockup-Prüfung); der Kern rechnet 11,6/10,5 = 1,104762 durch. **Gepinnt ist der Kern** — er trifft die Nachrechnung auf den Cent.

Weiter abgedeckt: Nutzungsgradschwelle 70 % (erfüllt, knapp verfehlt mit 69,9 %, ungepflegt, genau auf der Schwelle); Sockel 250 € **einmal je Lauf** (zwei Anlagen ergeben 2 × brutto − 250 €, nicht 2 × netto); § 54 nur für produzierendes Gewerbe bzw. Land- und Forstwirtschaft; Kessel mit § 53/53a ⇒ 0 € mit Hinweis auf § 54 (`STEUER_ENERGIEST_NUR_54`); fehlender Katalogsatz ⇒ 0 € unter Nennung des Schlüssels; die Einheitenkette €/MWh mit Hs/Hi **und** konservativ ohne Hs, €/1.000 l (100 × 61,35 €), €/1.000 kg (80 × 60,60 €), €/GJ × 3,6 (3.600 × 0,16 €) sowie die Weigerung, für eine je Liter gemessene Menge eine Dichte zu raten; die vier Bedingungen des § 9 Abs. 1 Nr. 3 **einzeln** verletzt (Hocheffizienz, räumlicher Zusammenhang, P_el über 2 MW, CO₂ über 270 g/kWh) mit unberührter § 9b-Entlastung; die Leistungsgrenze genau auf 2.000 kW; § 9b als `max(0, 20 × Netzbezug − 250)` an sechs Stützstellen einschließlich der Sockelschwelle 12,5 MWh.

**Grenze — Modus AUSWEIS/ERLOES.** Der Auftrag nennt ihn für diese Klasse; er ist aber **keine Größe dieses Rechners**. `SteuerGutschriftRechner` liefert den Betrag, die Entscheidung, ob er in den Kapitalwert eingeht, trifft `WirtschaftlichkeitCtrl` über `Tab_ProjektWirtschaftlichkeit.Stromst_Befreiung_Modus`; dafür gibt es `StromsteuerBefreiungModusTests`. Ein Fall (`Der_Rechner_kennt_den_Modus_der_Befreiung_nicht`) hält diese Arbeitsteilung fest, statt sie doppelt zu prüfen.

---

## 3 `EegSatzRechnerTests` (neu)

49 Fälle, rein. Löst das Abnahmekriterium der Katalogsaat ein: „Alle 16 BNetzA-Werte 08/2026 sind Unit-Test-Soll" (`GesetzKatalog.cs:1522`).

**Die 16 Werte für Inbetriebnahme ab 01.08.2026, alle getroffen** (ct/kWh):

| Klasse | AW Überschuss | AW Volleinspeisung | feste EV Überschuss | feste EV Volleinspeisung |
|---|---:|---:|---:|---:|
| bis 10 kW | 8,10 | 12,62 | 7,70 | 12,22 |
| bis 40 kW | 7,06 | 10,64 | 6,66 | 10,24 |
| bis 100 kW | 5,84 | 10,64 | 5,44 | 10,24 |
| bis 400 kW | 5,84 | 8,85 | — | — |
| bis 1.000 kW | 5,84 | 7,63 | — | — |

Zehn anzulegende Werte plus sechs feste Einspeisevergütungen (AW − 0,40, nur bis 100 kW) = 16.

Weiter: Degressionsschritte an neun Stichtagen ab 01.02.2024 (31.01.2024 ⇒ 0, 01.02.2024 ⇒ 1, 01.08.2026 ⇒ 6); die unrundete Fortschreibung 0,99ⁿ; `AW_mix` 300 kWp = **6,04 ct/kWh** samt Zerlegung in die vier marginalen Tranchen 10/30/60/200 kW; der Mix an fünf Stützstellen (10 → 8,10; 40 → 7,32; 100 → 6,43; 300 → 6,04; 400 → 5,99); `EV_mix = max(0, AW_mix − 0,40)` nur ≤ 100 kW und Ausfallvergütung `AW × 0,8` nur > 100 kW, **komplementär** geprüft (was das eine bekommt, bekommt das andere nicht); die Ausschreibungsgrenze 1.000 kW (darüber `Unvollstaendig` statt geraten, Anteil 500 kW bei 1.500 kWp); das Inbetriebnahmeprinzip; Klassen außerhalb der Grenzen ⇒ `null`.

### Zwei Befunde aus der Messung

**E1-EEG-1 — Zahlendreher in der Dokumentation.** Der Klassenkommentar `EegSatzRechner.cs:74` nennt für die sechs Degressionsschritte „8,60 × 0,99⁶ = **8,09679**". Nachgerechnet sind es 8,60 × 0,99⁶ = 8,0967292848…, auf fünf Nachkommastellen also **8,09673**. Der Wert für fünf Schritte (8,17851) stimmt. Am *anzuwendenden* Wert 8,10 ändert der Dreher nichts — deshalb ist er nie aufgefallen. Gepinnt ist die Rechnung; der Kommentar sollte bei Gelegenheit berichtigt werden (Textänderung, keine Rechenwirkung).

**E1-EEG-2 — die Ausfallvergütung rechnet auf der unrundeten Mischung.** Bei 100 kWp ist `AwMixCtUnrundet` = 6,432 und `AwMixCt` = 6,43; die Ausfallvergütung ist 6,432 × 0,8 = 5,1456 → **5,15**, nicht 6,43 × 0,8 = 5,14. Das ist dieselbe Regel wie bei der Degression (gerundet wird nur der Ausgabewert) und kein Fehler — es steht aber in keinem Papier und ist jetzt im Test benannt.

---

## 4 `PvErloesRechnerEegTests` (neu)

23 Fälle, rein, Reihen synthetisch. Beispiel des Rechenwegs 06: 300 kWp, IBN 01.08.2026, Überschusseinspeisung in Direktvermarktung, 199.500 kWh, Jahresmarktwert 4,50 ct, DV-Entgelt 0,40 ct, T = 20.

| Größe | Rechenweg 06 | **gemessen** |
|---|---:|---:|
| Spoterlös (159.600 kWh × 4,50 ct) | 7.182,00 € | **7.182,00 €** |
| Marktprämie (159.600 × (6,04 − 4,50) ct) | 2.457,84 € | **2.457,84 €** |
| DV-Entgelt (159.600 × 0,40 ct) | − 638,40 € | **− 638,40 €** |
| **Vergütung Jahr 1** | **9.001,44 €/a** | **9.001,44 €/a** |
| § 51a im letzten Jahr | 1.095,52 € | **1.095,52 €** |
| Jahr 20 gesamt | 7.042,29 € | **7.042,29 €** |

§ 51 AUTO in allen drei Zweigen: IBN vor dem 25.02.2025 ⇒ nein; ab 100 kWp sofort; darunter erst im Jahr **nach** dem iMSys-Einbau (IBN 2026, iMSys 2028 ⇒ Jahre 1–3 voll, ab Jahr 4 genau 80 %). Die Schalter `JA`/`NEIN` übersteuern in beide Richtungen. Ausfallanteil: Pauschale 20 % bzw. gepflegter Wert ohne Stundenreihe (als *nicht gemessen* gekennzeichnet), stundenscharf `Σ Einsp(Spot<0) / Σ Einsp` mit Reihe — in der Prüfreihe 2,05 % statt 20 %. 60-%-Kappung `Σ max(0, Einsp_h − 0,6 × kWp)` = 7.000 kWh (100 h zu 250 kW gegen die Schwelle 180 kW), ohne Reihe nicht messbar, mit Schalter `NEIN` abgeschaltet.

**Offener Befund V-2, im Testkommentar vermerkt.** § 51a rechnet `Ausfallarbeit_J1 × 0,5 × AW / 100 × Degradationsfaktor`, also mit dem **anzulegenden Wert**. Ob das der richtige Satz ist, ist offen — die Vorschrift spricht vom Vergütungssatz, und bei Direktvermarktung ist der nicht der AW. **Gepinnt ist das heutige Verhalten**, damit eine spätere Korrektur als Änderung sichtbar wird und nicht als Zufall durchläuft.

Nebenbefund zum Papier: Die Zahl 7.042,29 € für Jahr 20 trifft nur, wenn dem Rechner der **Eigenverbrauch** mitgegeben wird (85.500 kWh zu 0,288 €/kWh) — die Degradation schlägt ihn in Mehrbezug um. Der Rechenweg nennt diese Größen in der Vorbemerkung, nicht in der Rechenzeile; im Test stehen sie am Fall.

---

## 5 `BerichtBlattstrukturWacheTests` (neu)

5 Fälle, `[Collection("Testdatenbank")]`, Kultur `de-DE`. Befund N4: `ExcelBerichtGenerator.Erzeuge` und `WordBerichtGenerator.Erzeuge` wurden von **keinem** Test gerufen. Beide schreiben jetzt in den Temp-Ordner und werden zurückgelesen (ClosedXML für `.xlsx`, `DocumentFormat.OpenXml` für `.docx` — beide transitiv über `EPOS.Kern`, keine zusätzliche Paketreferenz nötig).

Daten synthetisch nach dem Muster `ReferenzprojektTests.Gruppendaten()` (ein Stamm, eine Variante, verschiedene Energiekosten), alle Bausteine aktiv. Die Testdatenbank wird gebraucht, weil der Wirtschaftlichkeitsblock Speicher- und KWKG-Zustände abfragt.

**Excel — gepinnt:** 5 Blätter in der Reihenfolge `Übersicht`, `Vergleich`, `Wirtschaftlichkeit`, `Stamm`, `Variante A`; je Blatt die Ankerzeilen — Titel `EPOS-Plan — Variantenvergleich`, Köpfe der Varianten- (Z9) und Gewerketabelle (Z13), Kopf des Vergleichsblatts (Z1), Titel des Wirtschaftlichkeitsblatts, die drei Szenario-Blöcke (`Szenario: Erwartet` Z7, `Best` Z23, `Worst` Z40) mit je eigenem Tabellenkopf (Z8/Z25/Z42), Kapitalwert-Verlauf (Z59) mit Kopf `Jahr | Stamm | Variante A` (Z60) und Mehrjahresübersicht (Z84) mit Kopf `Jahr | Energiekosten | Netto nominal` (Z88); als **Zahlenanker** die Zeile `Nettobarwert über T [€]` (Z18) mit **−178.529,70 / −133.897,27 €**, gegengeprüft gegen das letzte Verlaufsjahr (Z81, bei Restwert 0 dieselbe Zahl) und die nominalen Energiekosten der Mehrjahrestabelle (Z90, −12.000,00 €).

**Word — gepinnt:** Titel `Stammprojekt`, Untertitel, die Reihenfolge der sieben `Heading1` (Inhalt, Projektbeschreibung, Komponenten & Varianten, Berechnungsergebnisse je Variante, Variantenvergleich, Wirtschaftlichkeit, Anhang), **10 Tabellen**, die Köpfe der ersten drei und die Kopfzeile der Kennzahlentabelle `Kennzahl ; Stamm ; Variante A`.

**Bewusst nicht gepinnt:** die Zeile „Berichtsdatum" der Übersicht (Z7) und der Parameterblock (Z2) mit seinem „Rechenstand" — beide tragen einen Zeitstempel und wären am nächsten Tag rot.

**Zur Schriftmessung:** Eine Vorbelegung ist **nicht** nötig. `Erzeuge` ruft `GrafikModulSicherstellen()` selbst (`ExcelBerichtGenerator.cs:158`); findet `RueckfallSchrift()` keine Systemschrift, prüft `MessprobeLaeuft()`, ob ClosedXML ohne Hilfe messen kann — seit 0.105.1 gelingt das dank eingebetteter Schrift auch ohne jede Systemschrift. Der äußere `try/catch` sorgt dafür, dass ein Bericht nie an der Spaltenbreite scheitert. Der Wortbericht findet seine Vorlage im Testlauf nicht (sie liegt nur im Schalenprojekt) und nimmt den vorgesehenen Rückfallzweig mit programmatischen Ersatz-Styles.

---

## 6 `WirtZeileFormatWacheTests` (neu)

4 Fälle. Jede `WirtZeile` trägt `Format` (.NET-Zahlformat für Word und Ergebnisreiter) und `ExcelFormat` (Zellformat) nebeneinander; sie beschreiben dieselbe Genauigkeit in zwei Sprachen. Erlaubte Paare nach Protokoll 05/§ 6.2: `N0`/`#,##0`, `N1`/`#,##0.0`, `N2`/`#,##0.00`, `N3`/`#,##0.000`, `N4`/`#,##0.0000`.

Geprüft über `WirtschaftlichkeitZeilen.Kennzahlen` mit und ohne ausdrückliche Referenz sowie über `Sichtbare`, einmal auf einer synthetischen Menge (Stamm und Variante × drei Szenarien, Felder so belegt, dass die vier abweichenden Formate auftreten) und einmal auf den echten Ergebnissen des Projekts 1030. Dazu eine **Gegenprobe**, dass die geprüfte Menge wirklich mehrere Genauigkeiten aufspannt (N0, N1, N2, N3) — sonst prüfte die Wache nur den Feldinitialisierer — und ein Fall für die Vorgabe einer frischen Zeile.

Heute weichen genau vier Zeilen von der Vorgabe ab und setzen beide Felder von Hand (`WirtschaftlichkeitZeilen.cs:479` anzulegender Wert N2, `:619` Amortisation N1, `:625` interner Zinsfuß N1, `:631` Gestehungskosten N3). Alle Paare sind stimmig; die Wache ist eine **Vorwärtssicherung** für die fünfte Zeile.

`Mehrjahresbild` liefert keine `WirtZeile`, sondern einen eigenen Zeilentyp (`MehrjahresSpalte`) und fällt deshalb nicht unter diesen Wächter; eine „Erlösrubrik"-Methode gibt es nicht — das ist ein Block (`WirtZeile.BLOCK_A`/`BLOCK_B`) innerhalb des privaten `Baue`, den `Kennzahlen` mitliefert und der damit ebenfalls geprüft ist.

---

## 7 Kaskadenrunde 2 — die einzige Codeänderung

`EPOS.Kern/Controller/InvestKaskade.cs`, Runde 2 (`:215 ff.`).

**Befund** (`01/§ 1.2`): Die Schleife setzte jede fertige Zeile **innerhalb** der Runde auf `Abgeleitet = true`. War eine `PROZENT_ERZEUGERKOSTEN`-Zeile selbst als Hauptposition gekennzeichnet (`Tab_Kostenfaktor.IsMainComponent`), rechnete eine **zweite** solche Zeile derselben Komponente die **erste** in ihre Basis ein. Weil die Leseabfrage kein `ORDER BY` trägt, entschied damit die Datenbank über das Ergebnis.

**Änderung:** Das Zwei-Phasen-Muster der Runde 3 (Anwenderentscheid I-3, `:240 ff.`) ist jetzt auch hier — die Hauptzeilen werden **vor** der Zuweisungsschleife in `hauptZeilen` eingefroren. `z.Abgeleitet = true` bleibt in der Schleife, weil Runde 3 die Zeilen der Runde 2 in ihrer Basis braucht. 19 Zeilen geändert, davon 12 Kommentar.

**Neue Theorie** `Erzeugerkostenzeilen_zaehlen_einander_nicht_mit` (2 Datensätze): zwei `PROZENT_ERZEUGERKOSTEN`-Zeilen derselben Komponente (Wärmepumpe des Projekts 1040), beide als Hauptposition gekennzeichnet, mit 10 % und 3 % — in **vertauschter Rollenbelegung**. Beide Läufe liefern 566,00 € und 169,80 € aus der **einen** Basis 5.660,00 €. Benutzt werden die Zeilen 101600495 (StammID 112) und 101600496 (StammID 113), weil nur diese beiden Kostenfaktoren im Projekt einmalig vergeben sind — die StammIDs 110, 111 und 114 hängen an je zwei Projektzeilen.

**Gegenprobe (A/B am Test).** Mit der alten Fassung fallen **beide** Datensätze der neuen Theorie, während die übrigen 23 Fälle der Klasse — einschließlich der 16 `InlineData`-Anker — grün bleiben. Das belegt zugleich die Wirkung und die Unschädlichkeit.

**A/B über die ganze Testdatenbank.** `InvestKaskade.Summen` wurde für **alle 25 Projekte × 3 Szenarien** vor und nach der Änderung gemessen, je Schlüssel `(Komponente, Anlage)` und als Gesamtsumme — 249 Datenzeilen. Ergebnis: **zeilenweise identisch.** Der Bestand ist nicht betroffen; kein heutiges Projekt hat eine `PROZENT_ERZEUGERKOSTEN`-Zeile, die zugleich Hauptposition ist.

Die 16 `InlineData`-Anker `LiesInvestitionen_bleibt_zahlengleich` sind unverändert grün:

| Projekt | Summe [€] | Projekt | Summe [€] |
|---|---:|---|---:|
| 1007 | 0,0 | 1030 | 410.000,0 |
| 1018 | 45.312,5 | 1031 | 45.312,5 |
| 1019 | 7.001,0 | 1032 | 10.001,0 |
| 1023 | 7.001,0 | 1040 | 54.975,5 |
| 1024 | 12.001,0 | 1041 | 6.775,5 |
| 1026 | 6.775,5 | 1042 | 13.000,0 |
| 1028 | 6.775,5 | 1043 | 19.775,5 |
| 1029 | 6.775,5 | 1044 | 19.775,5 |

---

## Gate

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | **0 Fehler**, 5 Warnungen (Bestand) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | **0 Fehler**, 9.541 erfolgreich, 1 übersprungen (Bestand) |
| — `KiKern.Tests` | 499 |
| — `SpeicherEngine.Tests` | 378 |
| — `SpeicherPlanung.Tests` | 27 (+1 übersprungen, Bestand) |
| — `EPOS.UI.Tests` | 4.786 |
| — `EPOS.Kern.Tests` | **3.851** |
| Referenzlauf 1030, 1007, 1017, 1045, 1046 | 5 von 5 erfolgreich |
| Vergleich gegen `2026-09-18_R9_Kesselbrennstoff` | **GESAMT: PASS** (1.656.417 Werte in Toleranz) |
| Byte-Vergleich (nur Information) | alle fünf **byte-gleich** |
| `git grep '\bProgram\.'` über `EPOS.Kern/*.cs` | leer (ohne Kommentarzeilen) |
| `git grep` Windows-APIs über `EPOS.Kern/*.cs` | leer (ohne Kommentarzeilen) |
| Kulturpinnung | alle sechs neuen Klassen führen `Kulturvorrichtung` |
| `git status` | sauber |

Der Vergleich wird — wie in `kern.yml` — gegen eine Kopie der **fünf** Basisprojekte gefahren. Gegen die vollständige Basis (13 Projekte) meldet er `FAIL` mit „im Vergleichslauf nicht vorhanden" für die acht nicht gerechneten Projekte; die fünf gerechneten sind auch dort `PASS`.

## `git diff --stat`

```
 EPOS.Kern.Tests/BerichtBlattstrukturWacheTests.cs | 352 ++++++++++++++
 EPOS.Kern.Tests/EegSatzRechnerTests.cs            | 350 ++++++++++++++
 EPOS.Kern.Tests/InvestKaskadeTests.cs             |  79 ++++
 EPOS.Kern.Tests/PvErloesRechnerEegTests.cs        | 425 +++++++++++++++++
 EPOS.Kern.Tests/SteuerGutschriftRechnerTests.cs   | 545 ++++++++++++++++++++++
 EPOS.Kern.Tests/WirtZeileFormatWacheTests.cs      | 249 ++++++++++
 EPOS.Kern.Tests/WirtschaftlichkeitAnkerTests.cs   | 292 ++++++++++++
 EPOS.Kern/Controller/InvestKaskade.cs             |  19 +-
 8 files changed, 2309 insertions(+), 2 deletions(-)
```

## Was offen bleibt

| Nr | Punkt | Warum |
|---|---|---|
| 1 | Absoluter Kapitalwert für 1007, 1017, 1045, 1046 | Kein gebuchter Ergebnisstand und keine Kostenzeilen in der Testdatenbank. Als Fall festgehalten; nachziehbar, sobald die Projekte Daten bekommen |
| 2 | Herleitung der Kapitalwert-Abweichung 1024 (−676.036,81 €) | Nur benannt, nicht nachgerechnet. Gehört zu E7, wo die Rechenwege ohnehin angefasst werden |
| 3 | Kaskadenanker 1042 (+20.927,61 €) | Auf dem heutigen Datenstand nicht reproduzierbar (Prozentzeilen ohne Satz). Gemessener Stand ist gepinnt |
| 4 | `EegSatzRechner.cs:74` „8,09679" | Zahlendreher im Kommentar, richtig ist 8,09673. Reine Textberichtigung, keine Rechenwirkung |
| 5 | V-2 (§ 51a mit AW) | Bewusst nicht geändert — E1 ändert keine Rechenwege außer Punkt 7. Heutiges Verhalten gepinnt |
| 6 | `BerichtsDatenSammler` in den Kern | E3 Schritt 4. Die Untersuchung nennt oben, was dafür mitgehen muss (`EnergieMengen.BaueBrennstoffmengen`) |
