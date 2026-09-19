# Rechenkern Teil 2 — Vergütungen, Energiesteuer, Stromsteuer, Kohärenz, Emissionen, Gesetzeskatalog, Erlösrubrik

*Analyse des konsolidierten Wirtschaftlichkeitskonzepts für die Umsetzung in EPOS-Plan.
Stand 19.09.2026, Zweig `ios_migration_september`, `SchemaStand.Zielversion = 94`
(`EPOS.Kern/Allgemein/Update/SchemaStand.cs:265`), nächster freier Schemaschritt 95.
Nur gelesen, nichts geändert. Vorarbeiten `2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md`
und die Protokolle `00…06` werden mit Kennung zitiert, nicht wiederholt; die Zahlen des
Beispielprojekts sind in `01_Nachrechnung.md` nachgerechnet und hier nicht nachgeprüft.*

## 0 Ergebnis in fünf Sätzen

Der Rechenkern bildet die §§ 3.6–3.9 und 3.11 des Konzepts fast vollständig ab — Mischsatz,
Tranchen, Kontingent und Jahresdeckel je Anlage, Ersatzweg als leistungsgewichtete virtuelle
Gesamtanlage, Netting, Prüfkette, Pauschale § 9, § 53/§ 53a/§ 54 mit Einheitenkette und Sockel,
§ 9 Abs. 1 Nr. 3 mit vier Bedingungen und Ausweismodus, § 9b, CO₂-Preispfad mit Prognosemarke und
Methodenwechsel 2027 —, sodass die Umsetzungsarbeit nicht im Neubau, sondern in fünf benannten
Lücken liegt. Rechenwirksam offen sind genau drei: **K-1** (zweiter Fall des § 2 Nr. 16 KWKG,
braucht zwei Anlagenspalten und damit Schemaschritt 95), **S-2** (kein projektweites
Doppelentlastungsverbot; heute nur ein Hinweis, Fall 5 der Kohärenzprüfung) und **V-2** (§ 51a
wird mit dem anzulegenden Wert statt mit der Einspeisevergütung bewertet); alles Übrige ist
Ausweis, Zeilenstruktur oder Katalogpflege. Der Gesetzeskatalog trägt 49 KWKG-, 9 Stromsteuer-,
3 Umlagen-, 15 Energiesteuer-, 12 CO₂- und 30 EEG-Zeilen, die mit den Sätzen des
Grundlagen-Papiers übereinstimmen; drei gesäte Schlüssel haben **keinen Leser**
(`STROMST_ERLAUBNISSCHWELLE_KW`, `EF_BILANZ_EBEV_ERDGAS_HO`, `EF_BILANZ_EBEV_UMRECHNUNG_HO`),
während `STROMST_REDUZIERT_SATZ` entgegen dem Konzepttext längst gesät **und** gelesen wird.
Die Kohärenzprüfung hat acht gebaute Fälle, aber **nicht** den CO₂-Fall aus `01/B1` — und sie
erscheint an drei Orten (Seite, BHKW-Dialog, Nachweisumschlag), nicht im Wort- und Excelbericht
und nicht in der Erlösrubrik. Die Testabdeckung ist für die KWKG-Seite gut (u. a.
`KwkgErsatzwegGewichtetTests`, `KwkgSatzHerkunftTests`, `KwkgPauschaleZeileTests`), für den
**`SteuerGutschriftRechner` und den `EegSatzRechner` jedoch null** — beide haben keine einzige
Testklasse, und der Referenzlauf deckt die Wirtschaftlichkeit nachweislich nicht ab
(`aggregate.csv` führt ausschließlich Simulationsgrößen).

---

## 1 Regeltafel §§ 3.6–3.9 und 3.11

Alle Codeorte relativ zur Repowurzel. „umgesetzt" heißt: gelesen und der Regel entsprechend;
„abweichend" heißt: gebaut, aber anders als das Konzept es schreibt; „fehlt" heißt: kein Code.

### 1.1 § 3.6 KWKG-Zuschlag (Konzept Z. 1713–1906)

| Regel (§, Kurzform) | Codeort | Stand | Beleg | Test | Rechenwirkung |
|---|---|---|---|---|---|
| Mischsatz **marginal**, nicht klassenweise (§ 7 Abs. 1/2) | `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KwkgSatzRechner.cs:226` `Mischsatz`, Staffeln :155 / :177 | umgesetzt — **aber nur als Vorschlag** | Tranchenschleife mit `min(Obergrenze, P_el) − Obergrenze_(k−1)` | `KwkgSatzHerkunftTests` (11 Fakten) | — (siehe Zeile darunter) |
| Der **gerechnete** Satz ist der an der Anlage gespeicherte, nicht der Mischsatz | `WirtschaftlichkeitCtrl.cs:2967` (`SatzEigenDerAnlage`), `:2968` (`a.SatzEinspCt ?? 0`) | abweichend zum Leseeindruck des Konzepts | Die Jahresreihe liest `BhkwAnlage.SatzEigenCt/SatzEinspCt`; `KwkgSatzRechner` läuft nur für `KwkgModulNachweis.HerleitungEigen` (`:3036 ff.`) und den Dialog | dito | keine, solange der Anwender den Vorschlag übernimmt; sonst rechnet der Kern mit dem Handwert |
| § 7 Abs. 3a geht Abs. 1 **und** 2 vor (≤ 50 kW neu: 16,00 / 8,00) | `KwkgSatzRechner.cs:106` → `Pauschal` `:273` | umgesetzt | Sonderzweig vor beiden Staffeln | `KwkgSatzHerkunftTests` | — |
| Abs. 2 nur in den drei Tatbeständen § 6 Abs. 3; `KEINER` ⇒ 0 | `KwkgSatzRechner.cs:177` `EigenStaffel`; Laufregel `WirtschaftlichkeitCtrl.cs:3100` `SatzEigenDerAnlage` | umgesetzt | drei Satzreihen N1/N2/N3; K6-Regel „leerer Tatbestand ⇒ Satz bleibt, Meldung ungeprüft" ist im Lauf verankert | `KwkAnlagenwahrheitTests` (11+1/5) | — |
| Kein Rückfall Anlage → Projekt mehr (BK1, Schritt 89) | `WirtschaftlichkeitCtrl.cs:2965–2968` | umgesetzt | Kommentar und Code lesen ausschließlich `a.*` | `KwkgProjektaltspaltenTests` (11), `KwkgProjektspaltenWacheTests` (2) | — |
| Vbh-Kontingent § 8 je Anlage (Override > 0, sonst Anlagenart + Kostenanteil) | `KwkgKontingentRechner.cs:74` `Ableiten`; Laufweg `WirtschaftlichkeitCtrl.cs:3144` `KontingentDerAnlage`, Aufruf `:2983` | umgesetzt | 30.000 / 15.000 / 10.000 h nach Schwelle 50/25/10 % | `KwkAnlagenwahrheitTests` | — |
| 6.000-h-Stufe (§ 8 Abs. 2, Dampfsammelschiene > 50 MW) | `KwkgKontingentRechner.cs:45–55` | bewusst **nicht** gebaut, Schlüssel gesät | Klassenkommentar nennt Grund und Fehlbedienungsgefahr | — | keine (Anlagen dieser Größe sind über § 8a ohnehin gesperrt) |
| Mindestabstand zur Inbetriebnahme (§ 8 Abs. 2: 5/10 a) | `KwkgKontingentRechner.cs:57–60` | **fehlt**, als Vorbehalt in der Herleitung benannt | Altanlagen-Inbetriebnahme steht nicht im Datenmodell | — | ja, falls nachgezogen: Kontingent 0 statt 15.000/30.000 h bei zu jungen Altanlagen |
| Jahresdeckel § 8 Abs. 4, Staffel 5.000 … 2.500 h | `WirtschaftlichkeitCtrl.cs:4071` `StaffelDeckel`, Aufruf `:2989`; Katalog `GesetzKatalog.cs:1213–1221` | umgesetzt | acht Stützstellen 2021/23/25/26/27/28/29/30 | `KatalogpflegeTests` | — |
| Jahresschleife: `min(Vbh, Deckel, Rest) × (1 − Abschlag)`, Rest zehrt | `WirtschaftlichkeitCtrl.cs:2986–2999` | umgesetzt | Negativpreis-Abschlag aus `p.KwkgAbschlagNegativ`, Rest wird um die **vergütete** Menge gemindert | `KwkgErsatzwegGewichtetTests` (6, für den Ersatzweg) | — |
| Pauschale § 9 (≤ 2 kW): `0,04 × 60.000 × P_el`, einmalig Index 0, schließt die laufende Reihe aus | `WirtschaftlichkeitCtrl.cs:3312` `PauschaleReihe`; Aufruf `:2119` | umgesetzt | drei Katalogwerte, über der Grenze Schalter wirkungslos mit Begründung | `KwkgPauschaleZeileTests` (11+1/3) | — |
| Ersatzweg BK1a: leistungsgewichtete virtuelle Gesamtanlage | `WirtschaftlichkeitCtrl.cs:2751` `ReiheErsatzGewichtet` | umgesetzt | Gewicht `g_i = P_el,i`, vier Größen gemischt (`:2782–2794`) | `KwkgErsatzwegGewichtetTests` | — |
| Ersatzweg: Jahresdeckel **je Jahr** neu gemischt | `WirtschaftlichkeitCtrl.cs:2833–2840` | umgesetzt | Schleife über die Anlagen **innerhalb** der Jahresschleife | dito | — |
| Ersatzweg `G ≤ 0`: arithmetisch mitteln und benennen; Hinweise lokal entdoppeln | `:2769` (`nachLeistung`), `:2806` (`WIRT_KWKG_ERSATZ_OHNE_LEISTUNG`), `Entdoppelt` `:2857` | umgesetzt | — | dito | — |
| Netting „Eigen zuerst", nur auf die Zuschlagsmengen | `HilfsstromRechner.cs:83` `NettoSplit`; Anteilsbildung `WirtschaftlichkeitCtrl.cs:2939–2943` | umgesetzt | `StromMatrix`, § 9 Nr. 3, CO₂-Grenzwert und Vbh bleiben brutto — ausdrücklich in `WirtschaftlichkeitCtrl.cs:3584–3590` | `HilfsstromStrompreisJeAnlageTests` (5), `KesselBrennstoffModulTests` (7) | — |
| Eigenstrom-Tatbestand § 6 Abs. 3 wird gegen den **Anlagen**satz geprüft | `WirtschaftlichkeitCtrl.cs:3100` | umgesetzt | Lücke aus B3b geschlossen | `KwkAnlagenwahrheitTests` | — |
| Prüfkette: Stichtag ≤ 31.12.2026 · Realisierungsfrist 4 a · Ausschreibung > 500 kW · Heizöl-Neuanlage ab 2025 | `WirtschaftlichkeitCtrl.cs:2507–2537` (projektweit), `:4463–4480` (je Anlage), `AusschreibungsgrenzeKW` `:4382`, `BhkwMitHeizoel` `:4997`; Konstanten `:206`, `:207`, `:217` | umgesetzt | Fristen je Anlage, wenn die Anlage eigene Fristdaten führt (`:2507`) | — (keine eigene Testklasse) | — |
| **K-1: zweiter Fall § 2 Nr. 16 KWKG** (Abwärmeabfuhr ⇒ `Nutzwärme × σ`) | keiner; gesucht in `EPOS.Kern/`, `EPOS.UI/`, `WindowsFormsApplication1/` — `Abwaermeabfuhr` ergibt 0 Treffer, `Stromkennzahl` nur Katalog-/Stammrechnung (`EPOS.Kern/Controller/BHKWStammCtrl.cs:462`) | **fehlt** | Mengenbildung `WirtschaftlichkeitCtrl.cs:2939` rechnet stets `max(0, Klemmenerzeugung − Hilfsstrom)` | — | **ja** — Zuschlag für Notkühler-Anlagen heute zu hoch |
| Einspeiseerlös `PV_Überschuss × EV + KWK_Einspeisung × EV_KWK`, nominal konstant | `StromMatrix.cs:234` `Einspeiseerloes`, `:253` `EinspeiseerloesPv` | umgesetzt | Zonen- vor Rollentarif; PV-Dialog ersetzt den PV-Anteil | `StromLeistungspreisTests` (12) | — |
| Vermiedene Stromkosten als **Ausweis**, Leistungsanteil regelmäßig negativ | `WirtschaftlichkeitZeilen.cs:549–556` (Block B) | umgesetzt | Block B kommt in keine Summe (`Ausweis()` gegen `Erloes()`) | `ErloesrubrikTests` (10) | — |
| Korrektur „abzüglich entgangener § 9b-Entlastung" | `WirtschaftlichkeitZeilen.cs:558–566`; Größen im Umschlag `ErgebnisNachweisUmschlag.cs:84,87` | umgesetzt | Bemessung ist `VermiedenMengeMWh`, Satz aus `GESETZ_STROMST_ENTLASTUNG_9B`, Unternehmensart aus `SteuerGutschriftRechner.ProduzierendesGewerbe` (`:495`) | `ErgebnisNachweisPersistenzTests` (8) | — |

### 1.2 § 3.6 Photovoltaik / EEG (Konzept Z. 1874–1894)

| Regel | Codeort | Stand | Beleg | Test | Rechenwirkung |
|---|---|---|---|---|---|
| Degression 0,99ⁿ auf **unrundeter** Basis, Halbjahresstichtage ab 01.02.2024 | `EegSatzRechner.cs:92` `DegressionsBeginn`, `:121–123` | umgesetzt | Rundung erst am Ausgabewert (Nachtrag N1) | **kein Test** | — |
| `AW_mix` als marginale Klassen 10/40/100/400/1000 kWp, auf 2 Stellen gerundet | `EegSatzRechner.cs:104` `AnzulegenderWert` | umgesetzt | `AwMixCt` und `AwMixCtUnrundet` getrennt geführt (`:16`, `:23`) | **kein Test** | — |
| `EV_mix = max(0, AW_mix − 0,40)`, nur ≤ 100 kW | `EegSatzRechner.cs:27–34` | umgesetzt | `EvZulaessig` an der 100-kW-Grenze | **kein Test** | — |
| Ausfallvergütung `AW × 0,8`, nur > 100 kW | `EegSatzRechner.cs:206–210` | umgesetzt | `AusfallvergZulaessig = kwp > evGrenze` | **kein Test** | — |
| § 51 AUTO: IBN < 25.02.2025 → nein; ≥ 100 kWp → ja; sonst ab dem Jahr nach iMSys | `PvErloesRechner.cs:99` `Par51Stichtag`, `:240–253` | umgesetzt | JA/NEIN übersteuern | **kein Test** | — |
| Ausfallanteil: Pauschale 20 % oder stundenscharf aus der Spotreihe | `PvErloesRechner.cs:254–271` | umgesetzt | `AusfallGemessen` benennt die Herkunft | **kein Test** | — |
| 60-%-Kappung `Σ max(0, Einsp_h − 0,6 × kWp)` | `PvErloesRechner.cs:273–287` | umgesetzt, nur Stufe 2 messbar | AUTO = feste EV ohne iMSys | **kein Test** | — |
| Marktprämie `Spot€ + Arbeit × max(0, AW − JW)/100 − Arbeit × DV/100` | `PvErloesRechner.cs:385–426` | umgesetzt | Jahresmarktwert aus `EEG_JAHRESMARKTWERT_SOLAR` | **kein Test** | — |
| § 51a: `Ausfallarbeit_J1 × 0,5 × Satz/100` im letzten Vergütungsjahr | `PvErloesRechner.cs:437–446` | **abweichend (V-2)** | `e.AwMixCt` statt `EvMix` — `:443` | **kein Test** | ja, kleiner Betrag: Differenz `0,40 ct/kWh × Ausfallarbeit × 0,5`, einmalig |
| EV-Rundung (V-1) | `EegSatzRechner.cs:23,30` | benannt, unverändert | `EvMixCtUnrundet` liegt vor, der Erlös rechnet mit dem gerundeten Wert | — | Rundungsdifferenz, < 0,005 ct/kWh |

### 1.3 § 3.7 Energiesteuer anlagenscharf (Konzept Z. 1907–1941)

| Regel | Codeort | Stand | Beleg | Test | Rechenwirkung |
|---|---|---|---|---|---|
| Ein Rechnerlauf je Betrachtungsjahr, Katalog „jüngste Zeile mit `JahrVon ≤ Jahr`", fehlender Satz ⇒ 0 € **mit Begründung** | `SteuerGutschriftRechner.cs:240` `Rechne`; Katalogregel `GesetzKatalog.cs:245–252` | umgesetzt | `STEUER_ENERGIEST_SATZ_FEHLT` statt Rateversuch | **keine Testklasse** | — |
| `Wahl(a) = Anlagenwert ?? Projektwert`, je Anlage **eine** Wahl (R-U1) | `SteuerGutschriftRechner.cs:437` `Wahl` | umgesetzt | `KEINE` / `PARAGRAF_53` / `PARAGRAF_53A` / `PARAGRAF_54`, nie Kombination | — | — |
| § 53: `Satz_voll × Menge`, `VOLLER_BRENNSTOFF` ungeteilt / `ENERGETISCH` `Strom/(Strom+Wärme)` | `SteuerGutschriftRechner.cs:378` (Auswahl), `:558` `Stromanteil` | umgesetzt | Methode je Anlage mit Projektrückfall (`:447`) | — | — |
| § 53/§ 53a nur für Stromerzeuger; Kessel ⇒ 0 € + Begründung | `SteuerGutschriftRechner.cs:323–332` | umgesetzt | `STEUER_ENERGIEST_NUR_54` | — | — |
| § 53a: Teilsatz × Gesamteinsatz, Nutzungsgradschwelle 70 % (Projektgröße), kein Abbruch | `:335–339`, `:512` `NutzungsgradErfuellt` | umgesetzt | Prüfung höchstens einmal je Lauf | — | — |
| § 53a **Abs. 3** (4,96 €/MWh, produzierendes Gewerbe, Kesselseite) | keiner; Katalog führt nur `ENERGIEST_53A5_*` | **fehlt** (R-U2) | `GesetzKatalog.cs:1207–1213` sät ausschließlich Abs. 5 | — | ja, falls gebaut — höherer Erdgassatz auf verheizten Brennstoff |
| § 54: `max(0, Σ Teilsatz × Menge − 250 €)`, Sockel **einmal** je Lauf auf den § 54-Teil | `SteuerGutschriftRechner.cs:403–420` | umgesetzt | `summe54` getrennt von `summeGesamt` | — | — |
| § 54 nur produzierendes Gewerbe / Land- und Forstwirtschaft | `:350–355`, `:495` `ProduzierendesGewerbe` | umgesetzt | dieselbe Funktion wie § 9b | — | — |
| Kessel-Bemessung `Verbrauch > 0 ?? (Waerme_Gas + Waerme_Oel) ÷ (η/100)` | `WirtschaftlichkeitCtrl.cs` `KesselAnlagenErgaenzen` (Aufruf `:3578`) | umgesetzt | seit B-1 trägt die Modulzeile `Verbrauch` | `KesselBrennstoffModulTests` (7) | — |
| Einheitenkette €/MWh mit `eff_hs / eff_hi`, €/1.000 l bzw. kg ohne geratene Dichte, €/GJ × 3,6 | `SteuerGutschriftRechner.cs:573` `MengeInGesetzlicherEinheit`; `GJ_JE_MWH` `:229` | umgesetzt | ohne passende Abrechnungseinheit kein Betrag, sondern ein Grund | — | — |
| **S-2: projektweites Doppelentlastungsverbot** | keiner — `Energiesteuer` `:285` summiert § 53- und § 54-Beträge ohne Gegenprüfung | **fehlt** | grep „Doppelentlastung" in `EPOS.Kern/` = 0 Treffer | Hinweis: `KohaerenzPruefung.MischlageEnergiesteuer` `:735` | **ja** — Anlage A nach § 53 und Anlage B nach § 54 rechnen heute beide |

### 1.4 § 3.8 Stromsteuer (Konzept Z. 1942–1982)

| Regel | Codeort | Stand | Beleg | Test | Rechenwirkung |
|---|---|---|---|---|---|
| § 9 Abs. 1 Nr. 3: `Regelsatz × KwkEigen × Anteil`, Anteil aus dem Strom der bestandenen Anlagen | `SteuerGutschriftRechner.cs:660` `StromsteuerBefreiung`, Betrag `:750` | umgesetzt | Anteilsbereinigung wie beim KWKG-Guard | `StromsteuerBefreiungModusTests` (8+1/2) | — |
| Vier Bedingungen: Hocheffizienz · 4,5 km · P_el ≤ 2 MW **je Anlage** · CO₂ < 270 g/kWh Energieertrag | `:664–678` (Nachweis, Radius), `:713` (Grenze), `:717–722` (CO₂) | umgesetzt | CO₂-Faktor aus `EF_BILANZ_EBEV_*` über `WirtschaftlichkeitCtrl.cs:3999–4015`, Bezug `Brennstoff/(Strom+Wärme)` | dito | — |
| Ohne Stundenreihen keine Befreiung (`KwkEigenMWh` null ⇒ 0 mit Grund) | `:679–683`; Quelle `WirtschaftlichkeitCtrl.cs:3592` | umgesetzt | `matrix.KwkEigenGesamtMWh` nur bei vorhandener Reihe | dito | — |
| Modus `AUSWEIS` (Vorgabe) / `ERLOES`, Schemaschritt 88, reist ins Ergebnis | `DbWerte.cs:847,860`; `SchemaKatalog.cs:3370` `Schritt88_StromsteuerModus` | umgesetzt | Zeilen `WirtschaftlichkeitZeilen.cs:442` (A) und `:541` (B) | `StromsteuerBefreiungModusTests` | — |
| § 9b: `max(0, 20,00 €/MWh × Netzbezug − 250 €)`, nur produzierendes Gewerbe | `SteuerGutschriftRechner.cs:787` `StromsteuerEntlastung`, Betrag `:820` | umgesetzt | Mengen der beiden Vorschriften disjunkt (Eigenverbrauch gegen Netzbezug) | — | — |
| **S-5: Erlaubnisschwelle 1.000 kW** | Katalog `GesetzKatalog.cs:1147`; **kein Leser** im ganzen Baum | **fehlt** | grep `GESETZ_STROMST_ERLAUBNISSCHWELLE` außerhalb Saat und `DbWerte` = 0 Treffer | — | keine (reine Verfahrenspflicht), aber als Hinweiszeile vorgesehen |
| **S-5: Radius 4,5 km nur Meldungstext** | `SteuerGutschriftRechner.cs:671` — der Katalogwert wird nur in die Begründung formatiert, geprüft wird der Schalter `RaeumlicherZusammenhang` | abweichend, bewusst | Anwenderangabe, keine Geometrie | — | keine |

### 1.5 § 3.9 Kohärenzprüfung und § 3.11 Emissionen / CO₂-Pfad

| Regel | Codeort | Stand | Beleg | Test | Rechenwirkung |
|---|---|---|---|---|---|
| Kohärenzfälle 1–5 inkl. 4a und Doppelzählung § 9 Nr. 3 | `KohaerenzPruefung.cs:142` `Pruefe` | umgesetzt (Einzelheiten § 4) | acht Erzeuger, jeder in eigenem `try` | `KohaerenzNachtraegeTests` (5) | keine (Warnzeilen ohne Rechenwirkung) |
| **CO₂-Kohärenzfall** (Arbeitspreis mit CO₂-Anteil **und** BEHG-Reihe) | keiner | **fehlt** (`01/B1`, Änderungsplan § 3.3, Schwere hoch) | grep `CO2|BEHG` in `KohaerenzPruefung.cs` = 0 Treffer | — | keine, aber deckt einen rechenwirksamen Doppelansatz auf |
| Zwei Faktorensätze, nie dieselbe Variable (Nachweis gegen reale Bilanz) | Katalogklassen `EFN` / `EFB`, `GesetzKatalog.cs:1288–1390`; Lader `EmissionsFaktorLader.cs` | umgesetzt | L11 im Saatkommentar festgehalten | `EmissionsquelleTests` (12), `EmissionsspalteTests` (7), `Co2StromtraegerRueckfallTests` (17) | — |
| Methodenwechsel 01.01.2027 (PEF 1,8→1,5, Holz 0,2→0,7, Strom 560→100, Verdrängungsstrommix entfällt) | `GesetzKatalog.cs:1300–1307` (Wertlose 2027-Zeile), `:1413–1440`; Auflösung `BilanzKonvention.cs:113` `Bestimme` | umgesetzt | Zeile **ohne Wert** statt Weglassen — sonst führte die Stichtagsregel die 860 fort | — | — |
| KWK-Stromgutschrift ab 2027 als **methodische Wahl** im Bericht | `BilanzKonvention.cs:189–216` (`Stromgutschrift`, `OhneGutschrift`, `Substitution`) | umgesetzt | `EF_BILANZ_SUBSTITUTION_STROM` 685 g/kWh, Status VORLÄUFIG | — | — |
| Brennstoff-Emissionsfaktoren EBeV (Erdgas 200,9 H_i · 181,4 H_s · Heizöl EL 266,4 …) | Saat `GesetzKatalog.cs:1352–1372`; Leser nur `WirtschaftlichkeitCtrl.cs:3999–4015` | teilweise | **nur die H_i-Schlüssel werden gelesen**; `EF_BILANZ_EBEV_ERDGAS_HO` und `EF_BILANZ_EBEV_UMRECHNUNG_HO` (3,2508) haben keinen Leser | — | ja, falls die Hi/Ho-Falle im Code abgefangen wird: CO₂-Grenzwertprüfung des § 2 StromStG rund 10 % niedriger |
| Biomasse-Nullregel § 8 EBeV, `BehgOhneNachweis` | `BilanzKonvention.cs:243–252`, `:219–241` | umgesetzt | ohne Nachweis fossiler Standardwert (Pflanzenöl 266,4) | — | — |
| Bilanzkonvention als **ausgewiesene Einstellung**, nicht als stille Annahme | `BilanzKonvention.cs:86`, Hinweis `:102` | umgesetzt | Projektfeld mit Rückfall `BILANZJAHR_RUECKFALL = 2026` (`:58`) | — | — |
| CO₂-Preispfad als Stützstellenreihe, Prognosemarke | `WirtschaftlichkeitCtrl.cs:3219` `BaueCo2Reihe`; Prognoseerkennung `:3246–3248`; Lücken benannt `:3271` | umgesetzt | Status `GESICHERT/VORLÄUFIG/PROGNOSE` aus der Katalogzeile; `CO2_Preis > 0` übersteuert | — | — |
| Strommix-Rückfall 435 g/kWh als Kohärenzzeile | `KostenEmissionRechner.cs:569` setzt `CO2StrommixRueckfall`; Ausgabe als **gewöhnlicher Laufhinweis** `WirtschaftlichkeitCtrl.cs:5465–5468` | abweichend | § 3.9 führt ihn in der Kohärenztafel; im Code ist er kein `KohaerenzHinweis`, und der Wert 435 steht nicht im Text | `Co2StromtraegerRueckfallTests` | keine |

---

## 2 Befunde S-2, S-1…S-6, K-1, V-1…V-4, R-1…R-3, R-U1…R-U5

| Nr. | Stand | Beleg | Was eine Umsetzung bräuchte |
|---|---|---|---|
| **S-2** Doppelentlastungsverbot | **offen, rechenwirksam** | `SteuerGutschriftRechner.Energiesteuer` `:285–425` summiert je Anlage ohne projektweite Gegenprüfung; nur `KohaerenzPruefung.MischlageEnergiesteuer` `:735` meldet die Mischlage als **Hinweis** | Entscheid, ob die Mischlage verboten oder erlaubt ist (R-U1 gehört dazu); dann entweder eine Sperre im Rechner (§ 54-Betrag verwerfen, wenn eine Anlage § 53/53a zieht) oder die Hochstufung der Zeile auf Warnung; keine Schemaänderung |
| **S-1** überholte Zeilennummern älterer Protokolle | erledigt / gegenstandslos | betrifft Protokolle, nicht den Kern | — |
| **S-3** § 9-Meldung nennt Kessel „(0 kW)" | offen, kosmetisch | `SteuerAnlage.Klartext` `:112` formatiert `PelKW` immer; Kesselzeilen tragen 0 | Klartext bei `Stromerzeuger == false` ohne kW-Angabe; eine Zeile |
| **S-4** €/GJ ohne H_o-Umrechnung (für Kohle konsistent) | bewusst offen | `MengeInGesetzlicherEinheit` `:573`, `GJ_JE_MWH = 3,6` `:229` | nur falls ein Träger mit H_o-Abrechnung und €/GJ-Satz aufträte; heute keiner |
| **S-5** Radius 4,5 km nur Meldungstext · Erlaubnisschwelle 1.000 kW nirgends gelesen | offen | siehe § 1.4; `GESETZ_STROMST_ERLAUBNISSCHWELLE` ohne Leser | Radius: bleibt Anwenderangabe (Geometrie fehlt im Datenmodell). Schwelle: eine Hinweiszeile im Steuerlauf oder im BHKW-Dialog, die `P_el ≥ 1.000 kW` auf die Erlaubnispflicht anspricht — kein Rechenwerk |
| **S-6** `STROMST_REDUZIERT_SATZ` ungesät | **erledigt — Konzepttext überholt** | Saat `GesetzKatalog.cs:1156` (Generation 7, 0,50 €/MWh); Leser `EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs:659` (Schnellwahl) | nichts; § 4 S-6 und § 6.3 Punkt 14 sind nachzuziehen (§ 8) |
| **K-1** zweiter Fall § 2 Nr. 16 KWKG | **offen, rechenwirksam, entschieden 18.09.2026** | keine Spalte `KWKG_Abwaermeabfuhr`, keine Stromkennzahl an `Tab_Energieanlagen`; Mengenbildung `WirtschaftlichkeitCtrl.cs:2939` | Schemaschritt **95** (nicht 90/92 wie in den Papieren, beide vergeben): zwei Spalten `KWKG_Abwaermeabfuhr` (0/1, CHECK) und `KWKG_Stromkennzahl` (nullbar) an `Tab_Energieanlagen`; Vorschlag σ = P_el/P_th aus `Tab_BHKW` am Feld (Muster BK1); Fallunterscheidung in `ReiheJeAnlage` `:2939`: `min(Nettostromerzeugung, Nutzwärme × σ)`; **vorher zu klären**: ob die Nutzwärme (Wärmeproduktion − Wärmeüberschuss) modulscharf im Ergebnismodell vorliegt; Schreibweg in `KwkgAnlagenCtrl.Speichere` `:256`; Dialogfeld Gruppe 1b; kein Referenzprojekt betroffen |
| **V-1** EV-Rundung (EvMix unrundet, Erlös gerundet) | offen, benannt | `EegSatzRechner.cs:23,30` führt beide Größen | Entscheid, welche Größe den Erlös bildet; eine Zeile im `PvErloesRechner` |
| **V-2** § 51a bewertet mit AW statt EV | **offen, rechenwirksam** | `PvErloesRechner.cs:443` `e.AwMixCt` | eine Zeile: `EvMix` verwenden, wenn die Anlage feste Einspeisevergütung fährt; sonst bleibt AW richtig. Vorher fachlich klären, weil § 51a die entgangene **Vergütung** ersetzt |
| **V-3** PV-Reihe ohne eigene Spalte in der Mehrjahrestabelle | offen | `ErloesReihe.PV_VERGUETUNG` existiert (`KapitalwertRechner.cs:159`), `Mehrjahresbild.Baue` `WirtschaftlichkeitZeilen.cs:1196–1209` nimmt sie nicht auf | ein `m.Reihe(b, ErloesReihe.PV_VERGUETUNG, …)` plus ein Ressourcenschlüssel `WIRT_REIHE_PV`; die Selbstprüfung „Summe der Positionsspalten = Netto nominal" bleibt erhalten, weil die Reihe heute schon in `NominalReihe` steckt |
| **V-4** Eigen-/Einspeise-Split je Anlage ist benannte Näherung | offen, bewusst | `WirtschaftlichkeitCtrl.cs:2941–2943` verteilt die projektweiten Mengen nach dem Netto-Stromanteil | echte modulscharfe Zuordnung setzte modulscharfe Stundenreihen voraus; heute liefert die Simulation nur die Projektsumme je Zone |
| **R-1** Rahmenparameter je Stammprojekt, nicht je Variante | offen | `WirtschaftlichkeitCtrl.LadeParameter(idStamm)` (Aufruf u. a. `BhkwWirtschaftlichkeitHuelle.cs:127`) | Parameterzeile je Stand — Schema und Migration; mit § 2.16 (Vergütung je Variante, Schritt 93) besteht das Muster bereits |
| **R-2** Hilfsenergie steigt mit p_B statt p_E | offen (bei gleichen Sätzen null) | `KapitalwertRechner` trennt `BetriebJeJahr` und `EndenergieAnteilJeJahr` bereits (`WirtschaftlichkeitZeilen.cs:1177–1187`) | die Hilfsenergie-Position dem Endenergie-Topf zuweisen; rechenwirksam nur bei p_B ≠ p_E |
| **R-3** ohne bestimmbare Energiekosten kein Kapitalwert | Absicht | — | — |
| **R-U1** § 53 neben § 53a | bewusst offen (Recht) | als Auswahl modelliert, `SteuerGutschriftRechner.cs:437` | Klärung mit dem Hauptzollamt; der Code müsste nur dann geändert werden, wenn anteilige Anwendung zulässig wäre |
| **R-U2** § 53a Abs. 3, 4,96 €/MWh | offen, nicht umgesetzt | Katalog führt nur `ENERGIEST_53A5_*` (`GesetzKatalog.cs:1207–1213`) | Volltextprüfung; dann ein Katalogschlüssel `ENERGIEST_53A3_ERDGAS` + eine vierte Wahl `PARAGRAF_53A_ABS3` mit Unternehmensart-Bedingung; Schemabedarf nur, wenn die Wahlliste erweitert wird (Textspalte, Breite 20 reicht) |
| **R-U3** Ausschluss fossiler flüssiger Brennstoffe | umgesetzt als Prüfkette | `BhkwMitHeizoel` `WirtschaftlichkeitCtrl.cs:4997`, Kategorien `:221` | — |
| **R-U4** EuGH 09.07.2026 Beihilfe | bewusst offen, kein Primärbeleg | — | — |
| **R-U5** keine Nachfolgeregelung nach 2030 | teilweise umgesetzt | `GESETZ_KWKG_STICHTAG_DAUERBETRIEB` = 2026 (`GesetzKatalog.cs:1176`) ist der **Dauerbetriebs**stichtag, **nicht** das Förderende; eine Zeile „Förderende 2030" fehlt im Katalog, und der Jahresdeckel endet mit der 2030er-Stufe | ein Katalogschlüssel `KWKG_FOERDERENDE` (Jahr) plus Lesung in der Jahresschleife `:2986`; ohne ihn läuft der Zuschlag heute rechnerisch über 2030 hinaus, solange Kontingent und Deckel es zulassen |

---

## 3 Gesetzeskatalog — Saat gegen das Grundlagen-Papier

Saat: `EPOS.Kern/Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs:1000–1518`; Schlüsselnamen
`EPOS.Kern/Allgemein/DbWerte.cs:2128–2622`; Eindeutigkeit `EPOS.Kern/Allgemein/Update/GesetzesparameterEindeutig.cs`.
Generationen 1–7; die Nachsaat holt spätere Generationen in Bestandsdatenbanken nach (`:690–790`).

### 3.1 Die Saat im Überblick (Auszug; Werte gegen Grundlagen § 1.3/1.4, 2.1, 3.2–3.5, 7.7, 8.4 gelesen)

| Schlüssel | Klasse | JahrVon | Wert | Einheit | Status | Soll (Grundlagen) |
|---|---|---|---|---|---|---|
| `KWKG_ZUSCHLAG_EINSPEISUNG_BIS50KW … _UEBER2MW_NACHGER` (6 Zeilen) | KWKG | 2020 | 8,0 / 6,0 / 5,0 / 4,4 / 3,4 / 3,1 | ct/kWh | GESICHERT | § 1.3 — **stimmt** |
| `KWKG_ZUSCHLAG_NEU_BIS50KW_EINSP` / `_EIGEN` | KWKG | 2020 | 16,0 / 8,0 | ct/kWh | GESICHERT | § 7 Abs. 3a — **stimmt** |
| `KWKG_ZUSCHLAG_EIGEN_N1_*` (2), `_N2_*` (5), `_N3_*` (4) | KWKG | 2020 | 4,0/3,0 · 4,0/3,0/2,0/1,5/1,0 · 5,41/4,0/2,4/1,8 | ct/kWh | GESICHERT | § 1.3 — **stimmt** |
| `KWKG_LEISTUNGSSTUFE_1…4` | KWKG | 2020 | 50 / 100 / 250 / 2000 | kW | GESICHERT | **stimmt** |
| `KWKG_AUSSCHREIBUNG_GRENZE` (Gen. 2) | KWKG | 2020 | 500 | kW | GESICHERT | § 8a — **stimmt** |
| `KWKG_NEUANLAGE_GRENZE`, `KWKG_EIGEN_N1_GRENZE` (Gen. 3) | KWKG | 2020 | 50 / 100 | kW | GESICHERT | **stimmt** |
| `KWKG_VBH_NEUANLAGE` · `_MODERNISIERT_10/25/50` · `_NACHGERUESTET_10/25/50` | KWKG | 2020 | 30.000 · 6.000/15.000/30.000 · 10.000/15.000/30.000 | h | GESICHERT | § 1.4 — **stimmt** |
| `KWKG_KOSTENSCHWELLE_10/25/50`, `KWKG_MINDESTALTER_10/25/50` | KWKG | 2020 | 10/25/50 % · 2/5/10 a | %, a | GESICHERT | **stimmt**; Mindestalter ohne Leser |
| `KWKG_VBH_JAHRESDECKEL` (8 Zeilen) | KWKG | 2021…2030 | 5000·4000·3500·3300·3100·2900·2700·2500 | h | GESICHERT | § 8 Abs. 4 — **stimmt** |
| `KWKG_PAUSCHALE_BIS2KW` / `_VBH` / `_GRENZE` | KWKG | 2020 | 4,0 ct · 60.000 h · 2 kW | — | GESICHERT | § 9 — **stimmt** |
| `KWKG_STICHTAG_DAUERBETRIEB` · `KWKG_REALISIERUNGSFRIST` | KWKG | 2020 / 2025 | 2026 · 4 | a | GESICHERT | § 6 — **stimmt** |
| `STROMST_REGELSATZ` | STROMST | 2026 | 20,50 | €/MWh | GESICHERT | § 3 StromStG — **stimmt** |
| `STROMST_ENTLASTUNG_9B` · `STROMST_SOCKELBETRAG_9B` | STROMST | 2026 | 20,00 · 250 | €/MWh, €/a | GESICHERT | **stimmt** |
| `STROMST_GRENZE_BEFREIUNG_9_1_3_KW` · `STROMST_RADIUS_RAEUMLICH_KM` · `STROMST_CO2_GRENZWERT` | STROMST | 2026 | 2000 · 4,5 · 270 | kW, km, g/kWh | GESICHERT | § 2.1 — **stimmt** |
| `STROMST_ERLAUBNISSCHWELLE_KW` | STROMST | 2026 | 1000 | kW | GESICHERT | **gesät, kein Leser** (S-5) |
| `STROMST_REDUZIERT_SATZ` (Gen. 7) | STROMST | 2026 | 0,50 | €/MWh | GESICHERT | **gesät und gelesen** — S-6 erledigt |
| `UMLAGE_KWKG` · `_OFFSHORE` · `_STROMNEV19` (Gen. 7) | UMLAGEN | 2026 | 0,446 · 0,941 · 1,559 | ct/kWh | GESICHERT | Anwenderangabe 17.09.2026 |
| `ENERGIEST_ERDGAS` · `_HEIZOEL_EL` · `_GASOEL_SCHWEFELREICH` · `_FLUESSIGGAS` · `_SCHWEROEL` | ENERGIEST | 2003 | 5,50 · 61,35 · 76,35 · 60,60 · 25,00 | €/MWh, €/1.000 l, €/1.000 kg | GESICHERT | § 3.1 — **stimmt** |
| `ENERGIEST_53A5_ERDGAS/_HEIZOEL_EL/_FLUESSIGGAS/_SCHWEROEL/_KOHLE` · `_53A_NUTZUNGSGRAD` | ENERGIEST | 2024 | 4,42 · 40,35 · 19,60 · 4,00 · 0,16 · 70 | €/MWh, €/1.000 l, €/1.000 kg, €/GJ, % | GESICHERT | § 3.3 — **stimmt** |
| `ENERGIEST_54_ERDGAS/_HEIZOEL_EL/_FLUESSIGGAS/_SOCKELBETRAG` | ENERGIEST | 2024 | 1,38 · 15,34 · 15,15 · 250 | — | GESICHERT | § 3.4 — **stimmt** |
| `CO2_PREIS_NEHS` (8 Zeilen) | CO2 | 2021…2028 | 25·30·30·45·55·65 · 65 · 80 | €/t | G bis 2026, **V** 2027, **P** 2028 | § 8.4 — **stimmt** (konservative Vorbelegung) |
| `CO2_PREIS_NEHS_KORRIDOR_MIN/MAX` · `_NACHVERKAUF` · `_NACHKAUF` | CO2 | 2026/2027 | 55/65 · 55/65 · 68 · 70 | €/t | G bzw. V | § 8.1/8.4 — **stimmt** |
| `EF_NACHWEIS_*` (GEG/GModG Anlage 9), `PEF_NACHWEIS_*` (Anlage 4) je mit 2020- und 2027-Zeile | EFN / PEFN | 2020 / 2027 | u. a. Strom 560→100, Holz-PEF 0,2→0,7, Strom-PEF 1,8→1,5 | g/kWh, — | GESICHERT | § 7.2/7.3 — **stimmt**; Verdrängungsstrommix ab 2027 als **Zeile ohne Wert** |
| `EF_BILANZ_STROMMIX_*` (3 × 6 Jahre 2020–2025) | EFB | 2020…2025 | direkt 365…344 · ohne VK 373…352 · mit VK 435…406 | g/kWh | G bis 2023, V ab 2024 | § 7.6 — **stimmt** |
| `EF_BILANZ_EBEV_ERDGAS_HI/_HO` · `_HEIZOEL_EL/_S` · `_FLUESSIGGAS` · `_PFLANZENOEL` · `_BIODIESEL` · `_BIOMASSE` · `_UMRECHNUNG_HO` | EFB | 2023 | 200,9 / 181,4 · 266,4 / 286,9 · 235,8 · 266,4 · 266,4 · 0,0 · 3,2508 | g/kWh, GJ/MWh | GESICHERT | § 7.7 — **stimmt**; `_HO` und `_UMRECHNUNG_HO` **ohne Leser** |
| `EF_BILANZ_BAFA_*` (9 Zeilen) | EFB | 2026 | Biogas 152 · Pellets 36 · Holz 27 · Fernwärme 280 · Strom 435 … | g/kWh | GESICHERT | § 7.7 — **stimmt** |
| `EF_BILANZ_SUBSTITUTION_STROM` · `_BIOGEN_VERBRENNUNG` (Gen. 4) | EFB | 2024 | 685 · 365 | g/kWh | **VORLÄUFIG** | § 7.4/7.8 — methodische Wahl, richtig als vorläufig geführt |
| `EEG_AW_BASIS_UE_10…1000` · `EEG_AW_VOLL_ZUSCHLAG_10…1000` (Gen. 5) | EEG | 2022 | 8,60/7,50/6,20/6,20/6,20 · 4,8/3,8/5,1/3,2/1,9 | ct/kWh | GESICHERT | PV-Konzept § 6.2 |
| `EEG_DEGRESSION_HALBJAHR` · `_EV_ABSCHLAG` · `_AUSFALLVERG_ABSCHLAG` · `_EV_GRENZE_KW` · `_UNENTGELTLICH_GRENZE_KW` · `_AUSSCHREIBUNG_GRENZE_KW` · `_KAPPUNG_PROZENT` · `_VERGUETUNGSDAUER` | EEG | 2022–2024 | 1 % · 0,4 ct · 20 % · 100 kW · 200 kW · 1.000 kW · 60 % · 20 a | — | GESICHERT | — |
| `EEG_51_GRENZE_KW` · `EEG_51A_FAKTOR_SOLAR` · `EEG_51A_VLVST_MONAT_1…12` | EEG | 2025 | 100 kW · 0,5 · 87…73 | — | GESICHERT | — |
| `EEG_SOLARPAKET_AUFSCHLAG` | EEG | 2024 | 1,5 | ct/kWh | **VORLÄUFIG** | Beihilfevorbehalt, „NICHT anwenden (F8)" |
| `EEG_JAHRESMARKTWERT_SOLAR` (Gen. 6) | EEG | 2024 / 2025 | 4,624 / 4,508 | ct/kWh | GESICHERT | — |
| `UMSATZSTEUER_REGELSATZ` | UST | 2007 | 19 | % | GESICHERT | hinterlegt, ohne Rechenwirkung (L8) |

### 3.2 Abweichungen, fehlende Jahre, ungesäte Schlüssel

| Punkt | Befund | Beleg |
|---|---|---|
| **BEHG 2029 ff.** | Der Pfad endet mit **einer** Stützstelle 2028 (80 €/t, PROGNOSE); die Stichtagsregel schreibt sie fort. Das ist der Entscheid E5 („konstant ab 2028 ist EINE Stützstelle"), **keine Lücke** — aber ein 20-Jahres-Lauf ab 2026 rechnet damit 18 Prognosejahre zum selben Preis. Korridor und Nachkauf enden 2027. | `GesetzKatalog.cs:1240–1253`; Kommentar begründet den Verzicht auf die 2030er-Stützstelle |
| **EU-ETS 2 ab 2028** | Als Status im Katalog abgebildet (P ab 2028), aber **keine eigene Klasse/kein Schlüssel** für den europäischen Preis; § 3.11 und Grundlagen § 8.2 unterscheiden beide Regime | Saat kennt nur `CO2_PREIS_NEHS*` |
| **KWKG-Fristen 2030** | Jahresdeckel bis 2030 gesät; ein **Förderende** ist nicht gesät (R-U5). `KWKG_STICHTAG_DAUERBETRIEB` = 2026 ist der Dauerbetriebsstichtag | `GesetzKatalog.cs:1221`, `:1176` |
| **§ 51 Schwellen** | `EEG_51_GRENZE_KW` = 100 kW ab 2025 ist gesät; der **Stichtag 25.02.2025** steht als `DateTime`-Konstante im Code, nicht im Katalog | `PvErloesRechner.cs:99` `Par51Stichtag` |
| **§ 53a Abs. 3** | kein Schlüssel (R-U2) | — |
| `STROMST_REDUZIERT_SATZ` | **entgegen dem Konzept gesät (Gen. 7) und gelesen** | `GesetzKatalog.cs:1156`; `EnergietraegerHuelle.cs:659` |
| Gesäte Schlüssel **ohne Leser** | `STROMST_ERLAUBNISSCHWELLE_KW` · `EF_BILANZ_EBEV_ERDGAS_HO` · `EF_BILANZ_EBEV_UMRECHNUNG_HO` · `KWKG_VBH_MODERNISIERT_10` (bewusst) · `KWKG_MINDESTALTER_10/25/50` (bewusst) · `UMSATZSTEUER_REGELSATZ` (L8, bewusst) | grep über `EPOS.Kern/`, `EPOS.UI/`, `EPOS.UI.Daten/`, `WindowsFormsApplication1/` |
| **Doppelung Katalog ↔ `const` (§ 6.5)** | `StrompreisZerlegungModel` führt fünf Konstanten wertgleich zum Katalog: `STROMSTEUER_REGELFALL = 2,050` (`:78`) gegen `STROMST_REGELSATZ` 20,50 €/MWh; `STROMSTEUER_REDUZIERT = 0,050` (`:89`) gegen `STROMST_REDUZIERT_SATZ` 0,50 €/MWh; `UMLAGE_KWKG_VORGABE 0,446` · `UMLAGE_OFFSHORE_VORGABE 0,941` · `UMLAGE_STROMNEV19_VORGABE 1,559` (`:42–54`) gegen die drei `UMLAGE_*`-Schlüssel. Sie sind ausdrücklich als **Rückfallebene** deklariert und werden nur verwendet, wenn der Katalog für das Bilanzjahr nichts liefert (`EnergietraegerHuelle.cs:656–661`). **Gekoppelt ist nichts**: eine gepflegte Novelle erreicht die Konstanten nicht. | `EPOS.Kern/Model/StrompreisZerlegungModel.cs:35–110` |
| Kleiner Textfehler | Der Kommentar an `STROMSTEUER_REDUZIERT` nennt „Saatgeneration 5", die Saat trägt Generation 7 | `StrompreisZerlegungModel.cs:86` gegen `GesetzKatalog.cs:1158` |
| Saatwache | `Tab_Gesetzesparameter` führt `CHECK (length("Quelle") <= 120)`; eine zu lange Quelle ließ drei Zeilen still ausfallen — jetzt mit Längenschranke und Warnliste | `GesetzKatalog.cs:1183–1192`, `SaatWarnungen` `:678`; Test `GesetzkatalogSaatWacheTests` (3) |

---

## 4 Kohärenzprüfung

### 4.1 Gebaute Fälle gegen § 3.9

| Fall | Auslöser (Code) | Text / Schwere | Rechenwirkung |
|---|---|---|---|
| **1 konsistent** | `KohaerenzPruefung.cs:558–570` | `BESTAETIGUNG`, grün, ohne Betrag | keine |
| **2 Entlastung ohne Belastung** (Brennstoffseite) | `:523–535` | `WARNUNG` **mit Betrag** | keine |
| **2 Strom** (kein Stromträger · Komponente abgeschaltet · 0 ct/kWh) — je Vorschrift eine Zeile | `:835`, `:843–851`, `:877` `Fall2Strom` | `WARNUNG` mit gebuchtem Betrag | keine |
| **3 Belastung ohne Entlastung** (Brennstoff) | `:537–556` | `HINWEIS` | keine |
| **3 Strom** (§ 9b nicht gebucht trotz prod. Gewerbe und Netzbezug) | `:856–866` | `HINWEIS` | keine |
| **4 Satz ≠ Katalogsatz**, Toleranz 0,005 ct/kWh, je Träger eine Zeile | `:602` `Fall4Brennstoff`, `:937` `Fall4Strom`; Toleranz `:126` | `HINWEIS` mit beiden Sätzen | keine |
| **4a Einheit nicht vergleichbar** (1.000 kg/l gegen andere Abrechnungseinheit) | `:669` `EinheitNichtVergleichbar` | `HINWEIS` ohne Betrag | keine |
| **5 Mischlage § 53/53a neben § 54** (FX5-b, aus S-2) | `:735` `MischlageEnergiesteuer` | `HINWEIS` | keine |
| **Doppelzählung § 9 Abs. 1 Nr. 3** (Modus `ERLOES`) | `:917` `DoppelzaehlungBefreiung` | `WARNUNG` mit Betrag | keine |
| **Doppelpflege Hilfsenergie** (Anlagenanteil > 0 **und** aktive Kostenposition) | `:231` | `WARNUNG` | keine |
| **PV-Vergütungsherkunft** (§ 2.16; drei Lagen) — *im Konzept § 3.9 nicht geführt* | `:302` `PvVerguetungHerkunft` | `HINWEIS` | keine |

### 4.2 Fehlende Fälle

| Fehlt | Beleg | Folge |
|---|---|---|
| **CO₂-Bestandteil im Arbeitspreis **und** BEHG-Reihe gebucht** (`01/B1`, R1 des Befundpapiers) | `KohaerenzPruefung.cs` enthält weder `CO2` noch `BEHG`; `KapitalwertRechner.Rechne` bucht eine übergebene BEHG-Reihe zusätzlich (`:679–681`) | Doppelansatz bleibt unbemerkt; im Beispiel `01/B1` −443.981 € auf ΔKW |
| **Strommix-Rückfall als Kohärenzzeile** (§ 3.9 letzte Tabellenzeile) | gebaut, aber als gewöhnlicher Laufhinweis `WirtschaftlichkeitCtrl.cs:5465–5468`; der Wert 435 g/kWh steht nicht im Text | Zeile erscheint in der Hinweisspalte, nicht in der Kohärenzgruppe; die Schwere fehlt |
| **§ 53a-Nutzungsgrad ungepflegt** und **Sockel deckt § 54** erscheinen nur als `STEUER_*`-Begründungen im Laufhinweis, nicht als Kohärenzzeilen | `SteuerGutschriftRechner.cs:409–414`, `:515–521` | Vermischung zweier Kanäle (siehe 9d, § 5) |

### 4.3 Wo die Zeilen ausgegeben werden

| Ort | Codeort | Umfang |
|---|---|---|
| **Wirtschaftlichkeitsseite** (Windows) | `WindowsFormsApplication1/Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs:730` `KohaerenzZeilen` | alle Fälle des Laufs, je Hinweis eine Matrixzeile, Marken ⚠ / ✓ / · |
| **BHKW-Dialog**, Gruppe „Kohärenzprüfung (Energie- und Stromsteuer)" | `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor:368–382`, Quelle `SteuerlicheHinweise()` `:1053` | die steuerlichen Fälle **aus einem geladenen Lauf**; die laufunabhängige Doppelpflege steht in Gruppe 5 (`:396`) — Zulieferung `BhkwWirtschaftlichkeitHuelle.cs:139–141` ruft `Pruefe(idStamm, null)` |
| **Nachweisumschlag** (Persistenz) | `ErgebnisNachweisUmschlag.cs:81,150,213,228` | reisen als JSON in `Tab_ErgebnisWirtschaftlichkeit.Nachweis_Json` mit (B7P) |
| **Wort- und Excelbericht** | — | **nicht ausgegeben**; grep `Kohaerenz` in `EPOS.Kern/Allgemein/Bericht/` = 0 Treffer |
| **Erlösrubrik** | — | **nicht ausgegeben**; `WirtschaftlichkeitZeilen` kennt keinen Kohärenzblock |

Nebenbefund: Der Kommentar an `WirtschaftlichkeitSeiteGaben.cs:727–728` („Sie sind nicht
persistiert; ein aus der Datenbank geladener Stand zeigt sie deshalb nicht") ist seit B7P
überholt — § 6.3 Punkt 17 führt die Persistenz als erledigt.

---

## 5 Erlösrubrik im Kern

Zeilenkatalog: `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs`.
Vier Verwender derselben Definition: Wortbericht `BausteineWirtschaftlichkeit.cs:665`,
Excel `ExcelBerichtGenerator.cs:429`, BHKW-Dialog-Vorschau
`BhkwWirtschaftlichkeitDialog.razor:1156`, Windows-Seite `WirtschaftlichkeitSeiteGaben.cs:619`.

### 5.1 Zeilenkatalog gegen § 2.6 Block A/B

| § 2.6 | Zeile im Kern | Codeort | Abweichung |
|---|---|---|---|
| A1+A2 | `ERL_A_KWKG` mit Unterzeilen `ERL_A1_EINSPEISUNG`, `ERL_A1_SATZ`, `ERL_A2_EIGEN`, `ERL_A2_SATZ` | `:357–384` | **eine** Zeile mit zwei Unterzeilen (bewusst, aus `KwkgModulNachweis`) |
| A3 | `ERL_A_KWKG_PAUSCHALE`, in **€**, **nicht** in der Summe | `:405–412` | **gebaut** (U17/#346) — § 6.3 9c/9g sagen noch „fehlt" |
| A4+A5 | `ERL_A_ENERGIESTEUER` | `:421–429` | **eine** Zeile; `SteuerErgebnis.EnergiesteuerEur` (`SteuerGutschriftRechner.cs:171`) ist eine Summe → U7 offen (`03/#64`, `01/B12`) |
| A6 | `ERL_A_STROMST_ENTLASTUNG` | `:431–440` | — |
| A7 | `ERL_A_STROMST_BEFREIUNG` (Modus ERLOES) bzw. `ERL_B_STROMST_BEFREIUNG` (AUSWEIS) | `:442–446`, `:541` | — |
| A8 | `EINSPEISEERLOES` mit Unterzeilen PV / KWK | `:447–461` | — |
| A9 | `PV_FORM`, `PV_AW`, `PV_HERKUNFT`, `PV_MARKTPRAEMIE`, `PV_51A` | `:463–500` | — |
| A10 | Restwert bei `NETTOBARWERT`, **nicht** in der Summe | `:502` (Kommentar) | — |
| Summe A | `ERL_A_SUMME`, summiert nur die über ihr liegenden `Erloes()`-Zeilen | `:503–530` | — |
| B1 | `VERMIEDEN_GESAMT` + Arbeit/Leistung + `ERL_B1_ABZUG_9B` + `ERL_B1_EFFEKTIV` | `:549–570` | — |
| B2 | `PV_VERMIEDEN`, `PV_AUSFALL_KWH`, `PV_AUSFALL_EUR`, `PV_KAPPUNG` | `:573–585` | — |

### 5.2 Was U6 („Komponente innen") im Kern braucht

| Stück | Heutiger Stand | Was fehlt |
|---|---|---|
| **Anlagenbezug je Katalogzeile** | `WirtZeile` (`:28–120`) führt `Schluessel`, `Titel`, `Block`, `Einzug`, `IstSumme`, `IstUeberschrift` — **kein** Anlagen- oder Komponentenfeld | ein Feld `Komponente` (BHKW / PV / Kessel / projektweit) plus Zwischensummenzeilen je Komponentenblock; die Summenlogik `:514–528` zählt heute alle `blockA`-Zeilen über der Summenzeile und muss je Block gruppieren |
| **Daten je Anlage** | vorhanden: `KwkgModule` (`WirtschaftlichkeitDaten.cs:1115`, mit `EigenMWh`/`EinspeisungMWh` je Modul) und `EnergiekostenJeAnlage` (`:1132`), beide im Nachweisumschlag persistiert | **nicht** vorhanden: Steuerbeträge je Anlage — `SteuerErgebnis` (`SteuerGutschriftRechner.cs:168–189`) liefert drei Skalare |
| **Eigenverbrauch je Anlage aus `StromMatrix`** | **liegt dort NICHT getrennt vor.** `StromMatrix.Zone` (`StromMatrix.cs:35–56`) trennt nach **Tarifzone** (Winter/Sommer × HT/NT), nicht nach Anlage; die einzige anlagenscharfe Aufteilung ist die Näherung `anteil = stromNetto(i)/Σ stromNetto` in `WirtschaftlichkeitCtrl.cs:2939–2943` (V-4) | entweder die Näherung ausdrücklich als Verteilschlüssel der Rubrik übernehmen (dann ist die Zwischensumme eine Näherung und muss es sagen) oder modulscharfe Stundenreihen — Letzteres ist ein Simulationsthema, kein Rubrikthema |
| **Leistungsanteil projektweit** | `VermiedenLeistungJahr` (`:554`) ist eine Projektgröße aus `BezugsspitzeKW` | bleibt im Block „projektweit"; Q15 verlangt die Vorabklärung ausdrücklich |
| **§ 9b projektweit** | hängt am Restbezug, keine Anlage | Block „projektweit" |

### 5.3 Was U7 (§ 53/53a gegen § 54) im Kern braucht

`SteuerErgebnis` mit **zwei** Beträgen statt einem: `EnergiesteuerEur` aufteilen in
`Energiesteuer53Eur` und `Energiesteuer54Eur`. Der Rechner führt die Trennung bereits intern —
`summe54` gegen `summeGesamt` (`SteuerGutschriftRechner.cs:308`, `:397`, `:406–424`); der Sockel
wird schon auf den § 54-Teil bezogen. Nötig sind: zwei Felder, die Buchung in
`BaueSteuerReihen` (`WirtschaftlichkeitCtrl.cs:3425`), zwei Zeilen in `WirtschaftlichkeitZeilen`
(`:421`) und ein zweiter Ressourcenschlüssel. **Kein Schemabedarf** — die Summe bleibt dieselbe,
also auch der Kapitalwert; gespeicherte Läufe behalten ihre Zahl. Soll die Aufteilung auch beim
geladenen Stand stehen, reist sie im Nachweisumschlag mit (`ErgebnisNachweisUmschlag.cs`,
Fassungsnummer erhöhen).

### 5.4 9c/9g (Pauschale als Jahr-0-Zeile) und 9d (Begründungen je Position)

* **9c/9g — erledigt.** Die Pauschale hat eine eigene Rubrikzeile in € (`:405–412`), eine eigene
  Spalte in der Mehrjahrestabelle (`:1209` `ReiheAbJahr0`), eine eigene Erlösreihe
  (`KapitalwertRechner.cs:149` `KWKG_PAUSCHALE`) und reist als `KwkgPauschaleEur` im Umschlag mit.
  Nachweis `KwkgPauschaleZeileTests` (11 Fakten + 1 Theorie / 3 Datensätze). Der offene Punkt
  im Konzept ist überholt (§ 8).
* **9d — offen.** Die Gründe kommen aus den Ergebnisdaten, nicht vom Rechner: `WirtZeile.Grundtext`
  (`:106`) liefert die **Bedingung** der Position (z. B. `WIRT_GRUND_KWKG`, `:359`), während die
  Diagnose des Laufs als ein mit „ | " verketteter Text in `erg.Hinweis` steht
  (`WirtschaftlichkeitCtrl.Anhaengen` `:3376`). Eine saubere Lösung führte die Begründungen je
  Position im `SteuerErgebnis` — dieselbe Erweiterung, die U7 ohnehin anfasst: statt
  `List<string> Begruendungen` (`SteuerGutschriftRechner.cs:180`) eine Liste von
  (Position, Grund)-Paaren.

---

## 6 Testabdeckung

Gezählt mit `grep -c` über `[Fact]` / `[Theory]` / `InlineData` in `EPOS.Kern.Tests/` und
`EPOS.UI.Tests/` (ohne `bin/`, `obj/`).

| Rechner dieses Teils | Testklasse(n) | Fallzahl | Bewertung |
|---|---|---|---|
| `KwkgSatzRechner` | `KwkgSatzHerkunftTests`, `BhkwWirtschaftlichkeitDialogTests` | 11 Fakten · 65 F + 2 Th/4 ID | gut (Tranchen, Herleitung, Stellenzahl U26) |
| `KwkgKontingentRechner` | `KwkAnlagenwahrheitTests` | 11 F + 1 Th/5 ID | gut |
| `KwkgAktivierung` | `KwkAnlagenwahrheitTests` | s. o. | gut |
| Ersatzweg `ReiheErsatzGewichtet` | `KwkgErsatzwegGewichtetTests` | 6 F | trägt die Gewichtung und `G ≤ 0` |
| Pauschale § 9 | `KwkgPauschaleZeileTests` | 11 F + 1 Th/3 ID | gut |
| `KwkgSatzHerkunft` | `KwkgSatzHerkunftTests` | 11 F | gut |
| **`SteuerGutschriftRechner`** | **keine** | **0** | **größte Lücke**: § 53/53a/54, Sockel, Einheitenkette, § 9 Nr. 3 (vier Bedingungen), § 9b — kein einziger Test |
| **`EegSatzRechner`** | **keine** | **0** | AW_mix, Degression, EV-Grenze, Ausfallvergütung ungetestet |
| `PvErloesRechner` | `PvStrangRechnungTests` (17 F) betrifft die **Strangrechnung**, nicht § 51/Marktprämie/§ 51a | 0 für die EEG-Logik | § 51, 60-%-Kappung, Marktprämie, § 51a ungetestet |
| `PvKennzahlenRechner` | keine | 0 | — |
| `StromTarifRechner` | keine | 0 | vermiedene Kosten nur mittelbar über `StromLeistungspreisTests` (12) |
| `StromMatrix` | `StromLeistungspreisTests` | 12 F | Leistungspreis und Zonen |
| `KohaerenzPruefung` | `KohaerenzNachtraegeTests`, `LokalisierungWirtschaftlichkeitWacheTests`, `ProjekttransferTests` | 5 F + 1 + … | deckt die Nachträge, nicht alle acht Fälle |
| `GesetzKatalog` | `KatalogpflegeTests` (57 F + 6 Th/59 ID), `GesetzkatalogSaatWacheTests` (3), `GesetzesparameterEindeutigTests` (8), `StrompreisZerlegungTests` (11) | 79 F + 6 Th | sehr gut |
| `WirtschaftlichkeitZeilen` | `ErloesrubrikTests` (10), `KwkgPauschaleZeileTests`, `KwkgSatzHerkunftTests`, `PvVerguetungJeVarianteTests` (11), `ReferenzprojektTests`, `StromLeistungspreisTests`, `SzenarioParameterTests` | > 40 | gut |
| `ErgebnisNachweisUmschlag` | `ErgebnisNachweisPersistenzTests` (8), `KwkgPauschaleZeileTests`, `PvVerguetungJeVarianteTests` | 8+ | gut |
| Stromsteuermodus | `StromsteuerBefreiungModusTests` | 8 F + 1 Th/2 ID | gut |
| `HilfsstromRechner` | `KesselBrennstoffModulTests` (7), `HilfsstromStrompreisJeAnlageTests` (5), `HilfsstromBemessungVorlageTests` (7) | 19 | gut |
| `BilanzKonvention`, `EmissionsBilanzRechner` | keine direkten | 0 | mittelbar über `EmissionsquelleTests` (12), `EmissionsspalteTests` (7), `Co2StromtraegerRueckfallTests` (17) |
| `WirtschaftlichkeitEmpfehlung` | `ValeriLueckenTests` | — | nicht Teil dieser Analyse |

**Nachweisweg für Rechenänderungen.** Die Referenzbasis
`Referenzlaeufe/2026-09-18_R9_Kesselbrennstoff` deckt die Wirtschaftlichkeit **nicht** ab:
`Projekt_1030/aggregate.csv` führt 160 Größen, alle aus Simulation, Bedarf, Emissionen und
Vektorsummen — kein Kapitalwert, kein KWKG-, Steuer- oder Erlöswert (geprüft mit
`cut -d';' -f1 | sort -u`). Rechenwirksame Änderungen dieses Teils sind damit **ausschließlich**
über Unit-Tests und A/B-Handproben nachzuweisen; ein neuer Referenzstand wird erst nötig, wenn
eine Änderung eine in `aggregate.csv` landende Größe berührt (so bereits für K-1 festgestellt,
Konzept § 3.6). *Die Frage, ob der Referenzlauf erweitert werden sollte, prüft ein anderer Agent —
hier nur benannt.*

---

## 7 Umsetzungsliste dieses Teils

| # | Punkt | Größe | Rechen­wirkung | Nachweisweg | Abhängigkeit | Schema |
|---|---|---|---|---|---|---|
| 1 | **K-1** Abwärmeabfuhr + Stromkennzahl je Anlage, Fall 2 des § 2 Nr. 16 rechnen | **L** | **ja** | neue Testklasse `KwkgStromkennzahlTests` (Fall 1 unverändert, Fall 2 greift, σ-Vorschlag); A/B an 1030 | vorher klären: Nutzwärme je Modul im Ergebnismodell; Schreibweg `KwkgAnlagenCtrl.Speichere`; Dialogfeld Gruppe 1b | **ja — Schritt 95** (2 Spalten an `Tab_Energieanlagen`) |
| 2 | **S-2** projektweites Doppelentlastungsverbot | M | **ja** | Test „§ 53 an A und § 54 an B ⇒ nur eine Entlastung + Begründung"; Fall 5 der Kohärenzprüfung bleibt als Hinweis | Anwenderentscheid, ob Sperre oder Warnung; hängt an R-U1 | nein |
| 3 | **U7** `SteuerErgebnis` mit zwei Beträgen, zwei Rubrikzeilen | M | nein (Summe unverändert) | `ErloesrubrikTests` um zwei Zeilen; erste Testklasse für `SteuerGutschriftRechner` | keine | nein (Umschlagfassung erhöhen) |
| 4 | **CO₂-Kohärenzfall** (Arbeitspreis + BEHG-Reihe), Warnung mit Betrag | S | nein | `KohaerenzNachtraegeTests` um einen Fall | Anzeige in Kat. 5 des BHKW-Dialogs und in der Rubrik (§ 4.3 — Rubrik hat heute keinen Kohärenzblock) | nein |
| 5 | **V-3** PV-Spalte in der Mehrjahrestabelle | **S** | nein | Selbstprüfung „Summe der Positionsspalten = Netto nominal" in `ErloesrubrikTests` | Ressourcenschlüssel `WIRT_REIHE_PV` (de/en) | nein |
| 6 | **U6** Erlösrubrik nach Komponente innen (Blöcke, Zwischensummen, „projektweit", Gründe) | **L** | nein | `ErloesrubrikTests` erweitern; Zahlenprobe der Kat. 7 nachvollziehbar machen | **Q15**; setzt U7 (3) voraus, sonst hat die Energiesteuerzeile keinen Komponentenbezug; Verteilschlüssel des Eigenverbrauchs klären (V-4) | nein |
| 7 | **9d** Begründungen je Position im `SteuerErgebnis` | M | nein | Test „Nullzeile nennt ihren Grund, nicht den Laufhinweis" | am besten zusammen mit (3) | nein |
| 8 | **V-2** § 51a mit EV statt AW bewerten | **S** | **ja** (klein) | erste Testklasse für `PvErloesRechner`-EEG-Logik | fachliche Klärung | nein |
| 9 | **V-1** EV-Rundung vereinheitlichen | S | ja (Rundung) | dito | Entscheid | nein |
| 10 | **Testlücke schließen**: `SteuerGutschriftRechnerTests` und `EegSatzRechnerTests` anlegen (11 Handproben Energiesteuer, 16 BNetzA-Werte EEG sind bereits als Soll dokumentiert) | M | nein | die Tests selbst | keine | nein |
| 11 | **R-U2** § 53a Abs. 3 (4,96 €/MWh) | M | **ja** | Test + Katalogzeile | Volltextprüfung; Anwenderentscheid | nein (Wahlliste ist Text, Breite 20) |
| 12 | **R-U5** Förderende als Katalogparameter (`KWKG_FOERDERENDE`) | S | **ja** bei Läufen über 2030 | Test „Reihe endet mit dem Förderende" | Rechtsklärung | nein |
| 13 | **S-5** Erlaubnisschwelle 1.000 kW lesen (Hinweiszeile) | S | nein | Test auf die Meldung | keine | nein |
| 14 | **S-3** Kessel-Klartext ohne „(0 kW)" | S | nein | Sichtprobe / Test auf den Text | keine | nein |
| 15 | **Hi/Ho-Falle** im CO₂-Grenzwert des § 2 StromStG absichern (`EBEV_ERDGAS_HO`, `UMRECHNUNG_HO` lesen) | M | **ja** (Grenzwertprüfung) | Test „Erdgas brennwertbezogen fällt nicht fälschlich über 270 g/kWh" | Abrechnungseinheit des Trägers muss bekannt sein — die Kette steht in `MengeInGesetzlicherEinheit` bereits | nein |
| 16 | **R-2** Hilfsenergie mit p_E statt p_B fortschreiben | M | **ja** bei p_B ≠ p_E | A/B mit zwei Preisindizes | `KapitalwertRechner` trennt die Töpfe bereits | nein |
| 17 | Strommix-Rückfall als echte Kohärenzzeile mit 435 g/kWh im Text | S | nein | `Co2StromtraegerRueckfallTests` erweitern | keine | nein |
| 18 | **R-1** Rahmenparameter je Variante | **L** | **ja** | A/B je Variante | Muster liegt mit Schritt 92/93 vor | **ja** |

**Reihenfolge-Empfehlung:** 5 → 4 → 3 → 6 (Rubrik erst nach U7) und unabhängig davon 10 vor 1,
2, 8, 11, 12, 15, 16 — jede rechenwirksame Änderung dieses Teils braucht zuerst einen Test, weil
der Referenzlauf sie nicht deckt.

---

## 8 Aussagen des Konzepts über den Kern, die nicht mehr stimmen

| Zeile | Aussage | Befund | Berichtigungsvorschlag |
|---|---|---|---|
| Kopfzeile (Z. 1–13) | „Stand 02.09.2026, Zielversion 61" | `SchemaStand.Zielversion = 94` (`SchemaStand.cs:265`) | Datum auf 19.09.2026, Zielversion auf 94 — oder nach **Q25** die Zeile streichen und nur Datum + Zielversion führen |
| Z. 1751 | „Die projektweite Ableitung bleibt für den Ersatzweg stehen" (Vbh-Kontingent § 8) | widerspricht dem BK1a-Absatz darunter; der Ersatzweg leitet je Anlage ab (`WirtschaftlichkeitCtrl.cs:2790–2792` ruft `KontingentDerAnlage`) | Satz ersetzen: „Der Ersatzweg leitet das Kontingent ebenfalls je Anlage ab und mischt es leistungsgewichtet (BK1a)." |
| Z. 1825 ff. (K-1-Kasten) | „nächster freier Schemaschritt ist **92** (90 ist BK1a, 91 ist BK1b)" | 92 = `Schritt92_Referenzprojekt`, 93 = `Schritt93_VerguetungJeVariante`, 94 = Hilfsenergie-DML (`SchemaKatalog.cs:3413`, `:3473`; `SchemaStand.cs:265`) | „nächster freier Schemaschritt ist **95**" |
| `Rechenweg/05_Verguetungen_BHKW.md` Z. 303 | K-1 „Schemaschritt 90" | dito | auf 95 nachziehen |
| Z. 2111 (§ 4, S-6) | „`STROMST_REDUZIERT_SATZ` ungesät" | gesät (Generation 7, `GesetzKatalog.cs:1156`) **und** gelesen (`EnergietraegerHuelle.cs:659`) | S-6 auf „erledigt" setzen; nur die Erlaubnisschwelle 1.000 kW bleibt offen |
| Z. 2352 (§ 6.3 Punkt 14) | „Reduzierter Stromsteuersatz bleibt Konstante bis zur Katalog-Nachpflege" | Katalog führt ihn; `StrompreisZerlegungModel.STROMSTEUER_REDUZIERT` ist nur noch Rückfallebene (`:82–89`) | Punkt 14 streichen oder auf „Konstante bleibt als Rückfallebene, wertgleich" umschreiben |
| Z. 2274 (9c) | „Die KWKG-Pauschale (A3) hat keine Rubrikzeile" | Zeile gebaut (`WirtschaftlichkeitZeilen.cs:405–412`), Spalte gebaut (`:1209`), Test `KwkgPauschaleZeileTests` | 9c streichen, Verweis auf § 2.6 (A3) |
| Z. 2299 (9g) | „Ein Jahr-0-Ausweis der KWKG-Pauschale … ist als Vorschlag aufgenommen und **nicht gebaut**" | mit U17 / Statuszeile **#346** umgesetzt | 9g auf „erledigt mit U17 (#346)" setzen |
| Z. 2246 (§ 6.3, B5-Kernaufgabe 1) | „`KwkgAnlagenCtrl.Speichere` von 8 auf 11 Spalten (K7)" | gebaut: 8 + 4 Spalten über `mitSteuerangaben` (`KwkgAnlagenCtrl.cs:283–299`) — also 12, nicht 11 | Punkt 1 als erledigt kennzeichnen; **K7** in § 5 und in `Rechenweg/05` ebenfalls |
| Z. 969–971 (§ 2.13 (4)) | „die Eigenverbrauchsmengen je Anlage aus der Strommatrix, die dort **getrennt vorliegen**" | `StromMatrix.Zone` trennt nach **Tarifzone**, nicht nach Anlage (`StromMatrix.cs:35–56`); die einzige anlagenscharfe Größe ist die Näherung in `WirtschaftlichkeitCtrl.cs:2941` (V-4) | Satz ersetzen: „… braucht einen Verteilschlüssel je Anlage; die Strommatrix trennt nur nach Tarifzone, der Kern verteilt heute nach dem Netto-Stromanteil (V-4)." |
| Z. 1991 (§ 3.9, letzte Tabellenzeile) | „Strommix-Rückfall … Hinweis (435 g/kWh)" als Kohärenzfall | ist kein `KohaerenzHinweis`, sondern ein Laufhinweis ohne Wertangabe (`WirtschaftlichkeitCtrl.cs:5465`) | entweder in der Tabelle als „Laufhinweis" kennzeichnen oder den Fall in `KohaerenzPruefung` nachbauen (Umsetzungsliste 17) |
| Z. 2185 ff. (§ 5, Schema-Nummernraum) | „62 ist damit vergeben — neue Schritte anderer Etappen ab 63"; „für M-3 / § 9 Nr. 3 noch ‚Schritt 62' … auf **63** nachzuziehen" | § 9 Nr. 3 ist mit **88** umgesetzt (`SchemaKatalog.cs:3370`); die 62/63-Passage ist gegenstandslos | Absatz auf einen Satz kürzen: „Schemaschritt 88 trägt den Modus; der Nummernraum steht bei 95." |
| § 2.2 / § 2.3 Überschriften | `Form_BhkwWirtschaftlichkeit`, `Form_PhotovoltaikVerguetung`, `Form_WirtschaftlichkeitParameter` | **keine dieser Dateien existiert mehr**; der Baum führt Razor-Dialoge `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor`, `PhotovoltaikVerguetungDialog.razor`, `WirtschaftlichkeitParameterDialog.razor` mit Windows-Hüllen in `WindowsFormsApplication1/Views/Wirtschaftlichkeit/` (`BhkwWirtschaftlichkeitHuelle.cs` usw.) | die drei `Form_*`-Namen durch Dialog + Hülle ersetzen; betrifft auch `help_mapping.txt`, wo `Form_BhkwWirtschaftlichkeit.btn_Help` noch fehlt (`05/§6`) |
| Z. 2111 (§ 4, V-3) | „Die PV-Reihe hat in der Mehrjahrestabelle keine eigene Spalte" | **stimmt weiterhin**, aber die Ursache ist benennbar: `ErloesReihe.PV_VERGUETUNG` existiert bereits (`KapitalwertRechner.cs:159`), nur der Aufruf in `Mehrjahresbild.Baue` fehlt | Satz um „ein Aufruf und ein Ressourcenschlüssel" ergänzen — das macht V-3 von einer offenen Frage zu einer S-Aufgabe |

Nebenbefunde im Code (nicht Konzept, aber zum Nachziehen): der Kommentar an
`WirtschaftlichkeitSeiteGaben.cs:727` („Kohärenzzeilen nicht persistiert") ist seit B7P überholt,
und `StrompreisZerlegungModel.cs:86` nennt für `STROMST_REDUZIERT_SATZ` Saatgeneration 5 statt 7.

---

## Nicht geprüft

* **Keine Zahlen nachgerechnet.** Mischsätze, Steuerbeträge, EEG-Reihen und Kapitalwerte des
  Beispielprojekts stehen in `01_Nachrechnung.md`; sie wurden hier weder nachvollzogen noch
  gegengerechnet.
* **Kein Build, kein Test ausgeführt.** Die Fallzahlen sind gezählte Attribute, keine Laufergebnisse;
  ob die genannten Tests grün sind, ist nicht geprüft.
* **Live-Datenbank und Testdatenbank nicht abgefragt.** Alle Aussagen über gesäte Werte stammen aus
  der Saatfunktion `GesetzKatalog.Vorbelegung`, nicht aus einer Datenbank; welche Zeilen eine
  Bestandsinstallation tatsächlich führt (Generationen, Nachsaat, Anwenderpflege), ist offen.
* **Rechenwege 01–04 und 08** nur punktuell gelesen (04 für den CO₂-Doppelansatz über `01/B1`);
  Investitions-, Betriebs- und Nutzungsdauerseite gehören zu anderen Teilen.
* **Mockups nicht angesehen.** Die Aussagen zu U6/U7/U17/U23/U26 stammen aus dem Befundpapier und
  den Protokollen, nicht aus den HTML-Dateien.
* **`KapitalwertRechner`** nur an den Stellen gelesen, die Erlösreihen buchen (`:127–165`,
  `:679–681`, `:737–740`); Diskontierung, Szenarien und Restwert sind nicht geprüft.
* **`EmissionsBilanzRechner`, `EmissionsFaktorLader`, `Emissionsquelle`, `EndenergieAufloeser`**
  nur über Signaturen und Aufrufstellen berührt; die Emissionsbilanz selbst ist nicht durchgesehen.
* **UI-Seite.** Ob Dialog und Seite die hier beschriebenen Kernwerte vollständig und richtig
  anzeigen, ist nur dort geprüft, wo es um den Ausgabeort der Kohärenzzeilen ging.
* **Lokalisierung.** Ob alle genannten Ressourcenschlüssel in `de` und `en-US` vorliegen, ist nicht
  nachgesehen (`03/§6.2` führt das).
* **Referenzlauf-Erweiterung.** Dass `aggregate.csv` keine Wirtschaftlichkeitsgrößen führt, ist
  gemessen; ob und wie der Referenzlauf erweitert werden sollte, prüft ein anderer Agent.
