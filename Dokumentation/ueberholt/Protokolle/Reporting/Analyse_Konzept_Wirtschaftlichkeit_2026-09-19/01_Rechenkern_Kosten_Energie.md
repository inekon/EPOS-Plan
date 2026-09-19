# Rechenkern Teil 1 — Rahmen, Investition, Zuschüsse, Betriebskosten, Energiekosten/CO₂

Stand 19.09.2026 · Zweig `ios_migration_september` · `SchemaStand.Zielversion = 94`
(`EPOS.Kern/Allgemein/Update/SchemaStand.cs:265`, gelesen) · Referenzbasis
`Referenzlaeufe/2026-09-18_R9_Kesselbrennstoff`.
Leitpapier: `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`
(im Folgenden **KONS**, Zeilennummern des Papiers). Vorarbeiten werden mit Kennung
zitiert (`01/B1`, `03/#29`), nicht wiederholt; nachgerechnet wurde nichts.

---

## 0 Ergebnis in fünf Sätzen

Der Rechenkern dieses Teils ist **weiter als sein Konzept**: Kaskade, Zuschussklemme,
Ersatz/Restwert samt p_I, die zwei Preissteigerungstöpfe, die Elektrokessel-Regel E1 und
der anlageneigene Strompreis sind gebaut und an einem Ort zusammengeführt
(`InvestKaskade`, `KapitalwertRechner.Ersatz`, `EndenergieAufloeser`), während § 3.2/§ 3.4
des KONS noch Zustände beschreiben, die seit den Paketen FX2/FX5/W5‑B/H4 nicht mehr gelten
— vier Konzeptsätze sind heute **sachlich falsch**, nicht nur veraltet. Von den § 4-Befunden
sind I‑1, I‑2, I‑3, B‑1, B‑5, N1 und N3 erledigt und B‑4 zur Hälfte; offen bleiben I‑4,
I‑5, I‑6, B‑2, B‑3, B‑6, B‑7 und N2 (letzteres gewollt), dazu als **neuer Befund** eine
Restreihenfolgeabhängigkeit in Kaskadenrunde 2 (`InvestKaskade.cs:215‑232`). Die
Regressionsanker des § 6.2 stehen **nur zu einem Drittel** im Testwerk: 45.312,50 ·
12.001,00 · 13.000,00 liegen als `InlineData` in `InvestKaskadeTests`, Projekt 1030 ist
mit 410.000,00 € **neu verankert** (der KONS-Satz „überholt" stimmt nicht mehr) — dagegen
finden sich 99,00 €/a, −2.220.322,32 € und +20.927,61 € in **keinem** Test, und keine
einzige Kapitalwert-Zusicherung ist absolut, alle sind Differenz- oder
Bitgleichheitsproben. `EPOS.Referenzlauf` und `Referenzlauf/` frieren **ausschließlich**
Simulationsgrößen ein (`Tab_Ergebnis*`, Ganglinien) — kein Kapitalwert, kein
`Tab_ErgebnisWirtschaftlichkeit`, keine Betriebs- oder Energiekosten —, sodass der
kleinste Nachweisweg für Rechenänderungen das dotnet-Dateiskript gegen die reinen Rechner
bleibt (`01/§ 2`) plus die 14 datenbanknahen Testklassen dieses Teils. Dringendster
Umsetzungspunkt ist nicht eine Formel, sondern eine Schichtverletzung: die einzige
Produktions-Aufrufstelle von `KostenEmissionRechner.Berechne` — Schritt 5/6 der
Rechenreihenfolge — liegt in der **Windows-Schale**
(`WindowsFormsApplication1/Allgemein/Bericht/BerichtsDatenSammler.cs:421`), womit die
Wirtschaftlichkeit auf iOS heute gar nicht rechenbar ist.

---

## 1 Regeltafel §§ 3.1–3.5 und 3.10

Stand-Schlüssel: **U** umgesetzt · **A** abweichend (Code ≠ Konzept) · **F** fehlt ·
**NP** nicht prüfbar. Alle Codeorte sind gelesen, nicht gefolgert.

### 1.1 § 3.1 Rahmen — Kapitalwert nach DIN EN 17463 (KONS 1396–1475)

| Regel (§, Kurzform) | Codeort | Stand | Beleg | Test | Rechenwirkung, falls offen |
|---|---|---|---|---|---|
| KW = −I₀ + Σ(E−A)/(1+i)^t + RW/(1+i)^T + Einmalzahlung | `KapitalwertRechner.cs:760` | U | `z.Kapitalwert = -z.Investition - z.BarwertAusgaben + z.BarwertEinnahmen + z.RestwertBarwert`; Einmalzahlung über `:684` | `KwkgPauschaleZeileTests` | — |
| A_t = Betrieb·(1+p_B)^(t−1) + Energie·(1+p_E)^(t−1) + CO2_t + Ersatz_t | `:729–736` | U | zwei Zweige (mit/ohne jahresscharfer CO₂-Reihe), Klammer `(energieJahr + behgJahr)` unangetastet | `KapitalwertRechnerPreisindexTests` | — |
| **Dritter Topf** Endenergie mit p_E (FX3/R‑2) — im KONS § 3.1 **nicht genannt** | `:725–727`, `:736` | A | `endenergieAusgabe` wird der Klammer nachgestellt; Formelkarte des KONS zeigt nur zwei Töpfe | `BetriebskostenBasisTests`, `ValeriLueckenTests` | Konzept nachziehen (kein Codebedarf) |
| E_t = Einspeiseerlös₁ nominal konstant + Σ Reihe.Wert(t) | `:737–740`, `:754` | U | `einnahmen = erloesJahr` ohne Steigerung | `KwkgPauschaleZeileTests` | — |
| Sechs benannte Erlösreihen, Index 0 = Einmalzahlung, nicht abgezinst, mindert I₀ nicht | `:129–160`, `:678–686` | U | sechs Konstanten; `einmalT0` geht in `BarwertReihe[0]` und `BarwertEinnahmen` | `KwkgPauschaleZeileTests:59` | — |
| n ≥ 1 sonst n = T (dann kein Ersatz, kein Restwert) | `KapitalwertRechner.cs:432`, `:434` | U | `OhneDauer` gesetzt | `ErsatzRestwertTafelTests` | — |
| start = StartJahr falls > 1, sonst 0; start > T ⇒ nur Ausweis | `:441–447` | U | `b.Ausserhalb` | `ErsatzRestwertTafelTests` | — |
| Ersatz t_j = round(start + k·n), 1 ≤ t_j < T | `:467–482` | U | Schleife mit `Math.Round`, `if (tj >= T) break` | `ErsatzRestwertTafelTests`, `PreisInvestitionTests` | — |
| **Ersatz indiziert mit (1+p_I)^t_j** (W5‑B‑12) — im KONS § 3.1 **nicht genannt** | `:474–478` | A | `letzterFaktor = Math.Pow(1.0 + pI, tj)`; Szenarienkonzept § 10.1 (Z. 389) beschreibt es | `PreisInvestitionTests` (10), `Migration72Tests` (5) | Konzept nachziehen |
| RW_T = Betrag × Restdauer / n, linear, nur Restdauer > 0 | `:492–497` | U | `rest > 1e-9`; **auf der Preisbasis der letzten Beschaffung** (p_I) | `ErsatzRestwertTafelTests` | — |
| a(i,n) = i·q/(q−1); a = 1/n bei i ≈ 0 | `:346–353` | U | Schwelle `1e-12` | `ErsatzRestwertTafelTests` | — |
| Kapitalwertdifferenz, Annuität, dyn. Amortisation ohne Restwert, IZF Bisektion −99 %…1000 %, 200 Schritte | `:770–804` (IZF), `:806–828` (Amort.) | U | `lo = -0.99, hi = 10.0`, `for (iter < 200)`; Amortisation über `BarwertReihe` ohne Restwert | keine eigene Testklasse | — |
| Wärmegestehungskosten (−KW·a)/(MWh·1000) | `WirtschaftlichkeitZeilen.cs` / Rechenweg 08 | NP (außerhalb dieses Teils gelesen) | `01/§ 3.10` hat die Formel bereits nachgerechnet | — | — |
| Szenariowert = Szenariospalte ≠ 0, sonst Erwartet; `szenarioGepflegt ⇔ \|Δ\| > 1e−9`, **an allen drei Lesestellen identisch** | `WirtschaftlichkeitCtrl.cs:6329–6341` (Kern), `:6094`, `:6236`, `InvestKaskade.cs:403` | U | drei Lesestellen mit identischem Ausdruck gefunden und gelesen | `SzenarioParameterTests` (17), `ValeriLueckenTests` (15) | — |
| Sensitivität: Zins ±1 %‑Pkt · p_E ±1 %‑Pkt · Investition ±10 % (**Zuschuss nicht mitskaliert**) · Energiekosten inkl. CO₂ ±10 % · KWKG‑Bonus entfällt | `WirtschaftlichkeitCtrl.cs:5146–5200`; Zuschuss unskaliert `:5140` | U | `e.Zuschuss` wird unverändert durchgereicht; `investFaktor` greift nur auf `InvestPosition` (`:5024 ff.`) und auf den investgekoppelten Betriebsanteil (FX5‑a) | — | — |

### 1.2 § 3.2 Investitionskosten — Drei-Runden-Kaskade (KONS 1476–1524)

| Regel | Codeort | Stand | Beleg | Test | Rechenwirkung |
|---|---|---|---|---|---|
| Lesepunkt `Tab_ProjektWerte` Kat. 1 **„ohne ORDER BY (Befund I‑3)"** | `InvestKaskade.cs:163–168` | **A** | Abfrage trägt weiterhin kein `ORDER BY` — aber das Ergebnis ist seit FX2 **reihenfolgeunabhängig** (Zwei-Phasen-Runde 3, `:248–252`). Die KONS-Zeile beschreibt einen Mangel, den es nicht mehr gibt | `InvestKaskadeTests` | Konzeptsatz streichen |
| Runde 1: Vorrang BETRAG/leer → VALERI → Menge frisch vor Konserve → ein Rechenweg `BetriebskostenCtrl.Betrag` | `InvestKaskade.cs:205–213` + `:393–421` | U | `InvestBetrag` reiht Kaskadenbasis → `BaugroesseSumme` → `Menge`-Spalte und ruft `BetriebskostenCtrl.Betrag` | `InvestKaskadeTests`, `BetriebskostenBaugroesseTests` | — |
| `EUR_PRO_KWP` = Σ `Tab_Energieanlagen.PV_Leistung` × Satz (**I‑1**) | `TechnikPlanwertCtrl.cs:696–700` → `PhotovoltaikCtrl.cs:215–243` | **A, erledigt** | gerechnet wird `SUM(Tab_PV.Leistung × Tab_Energieanlagen.PV_Leistung)/1000` = echte kWp; dieselbe Formel wie Simulation und Vergütungsdialog | `KwpBemessungHerleitungTests` (10) | Konzeptzeile berichtigen |
| `EUR_PRO_KW_LEISTUNG` am BHKW = P_el; am Pufferspeicher = Gesamtvolumen (€/Ltr.) | `TechnikPlanwertCtrl.cs:786–812` | U | Anwenderentscheid 15.09.2026 im Code belegt | `BemessungBhkwPufferspeicherTests`, `PufferspeicherVolumenbemessungTests`, `LeistungsbemessungBetriebTests` | — |
| Art ↔ Gewerk gekreuzt: falsches Paar ⇒ **null** | `TechnikPlanwertCtrl.cs:773–830` (`Geraetespalte` gibt `null`) | U | jeder Zweig endet mit `return null` | `BetriebskostenBemessungsmatrixTests` (9) | — |
| Pufferspeicher: `EUR_PRO_KWH_KAPAZITAET` liefert null | `TechnikPlanwertCtrl.cs:826` | U | `komponentenID == 5 ? "Energie" : null` — Puffer ist 6 | dito | — |
| Runde 2 Basis = Σ Runde‑1-Zeilen mit `IsMainComponent`, ≠ ZUSCHUSS, gleiche `KomponentenID` | `InvestKaskade.cs:215–232` | **U mit Restrisiko** | Schleife setzt `z.Abgeleitet = true` **innerhalb** derselben Runde; eine zweite `PROZENT_ERZEUGERKOSTEN`-Zeile mit `IsMainComponent = TRUE` derselben Komponente zählt die erste mit → dieselbe Reihenfolgeabhängigkeit wie I‑3, nur in Runde 2. **Neuer Befund** | `InvestKaskadeTests` deckt den Fall nicht ab | S; Zwei-Phasen-Muster der Runde 3 auf Runde 2 übertragen |
| Runde 3 stufig Anlage → Komponente → Projekt; Basis 0 → null → Mengenkette | `InvestKaskade.cs:248–279` | U | `basisZeilen` vor der Zuweisungsschleife eingefroren; Stufen `sAnlage/sKomponente/sProjekt`; `basis != 0 ? basis : (double?)null` | `InvestKaskadeTests` | — |
| Kaskadenwirkung Projekt 1042 = **+20.927,61 €** | — | **F** | Faktor 1,155 nirgends als Testzusicherung; siehe § 3 | — | M (Ankertest) |

### 1.3 § 3.3 Reduktionen — Zuschüsse (KONS 1525–1540)

| Regel | Codeort | Stand | Beleg | Test | Rechenwirkung |
|---|---|---|---|---|---|
| I₀_brutto = Σ Nicht-Zuschuss mit start = 0 | `KapitalwertRechner.cs:640`, `:658` | U | `z.InvestitionBrutto = z.Investition` nach der Positionsschleife | `ValeriLueckenTests` | — |
| Zuschuss = min(Σ Zuschusszeilen, I₀_brutto) — **Klemme** | `:665–667` | U | `Math.Min`, `ZuschussUeberhang` als Ausweis | — (kein eigener Fall gefunden) | S (Klemmentest) |
| Kennzeichen `Kostenart = "ZUSCHUSS"` getrimmt, **ohne** Groß-/Kleinschreibung; `Zuschuss = Σ \|Betrag\|` | `WirtschaftlichkeitCtrl.cs:5784–5795` (`OrdinalIgnoreCase`), `:5690` (`Math.Abs`) | U | — | `BetriebskostenBasisTests` (Zuschuss bleibt außerhalb der Basis) | — |
| Abzug **nach** der Positionsschleife; Ersatz/Restwert bleiben brutto; `Ergebnis.Investition` brutto | `:650–672` | U | Kommentar und Reihenfolge im Code gelesen | — | — |
| Zuschusszeilen erzeugen keinen Ersatz, keinen Restwert, keine Kaskadenbasis | `WirtschaftlichkeitCtrl.cs:5686–5691`, `InvestKaskade.cs:250`, `:366` | U | `continue` vor der Positionsliste; `!h.Zuschuss` in beiden Basisbildungen; `Summen` trägt 0 bei | `BetriebskostenBasisTests` | — |

### 1.4 § 3.4 Betriebskosten (KONS 1541–1606)

| Regel | Codeort | Stand | Beleg | Test | Rechenwirkung |
|---|---|---|---|---|---|
| **„Sperre zuerst: fehlt Menge oder Satz ⇒ Betrag = 0, nicht der gespeicherte Wert"** | `BetriebskostenCtrl.cs:77–98` | **A — Konzept falsch** | seit Anwenderentscheid I‑2 (30.08.2026, FX2) gilt genau umgekehrt: `wert = eingegeben`. Der KONS-Satz beschreibt den Zustand **vor** FX2 | `BetriebskostenBemessungsmatrixTests` | KONS Z. 1543/1544 berichtigen |
| Gruppe A absolut (`BETRAG`, `JAHRESBETRAG`) | `BetriebskostenCtrl.cs:67–75` | U | — | dito | — |
| Gruppe B Prozent = Menge × Satz/100 | `:114–121` | U | sechs `PROZENT_*`-Arten | dito | — |
| Gruppe C Produkt = Menge × Satz | `:122–132` | U | zehn `EUR_PRO_*`-Arten | dito | — |
| Vorrang 1: Szenariowert gepflegt → keine Ableitung | `WirtschaftlichkeitCtrl.cs:6094`, `:6115` | U | — | `ValeriLueckenTests` | — |
| Vorrang 3: Endenergie-Arten **immer frisch**; Auflöser null ⇒ **„Betrag 0"** | `WirtschaftlichkeitCtrl.cs:6100–6102` + `BetriebskostenCtrl.cs:77` | **A** | Menge wird immer ersetzt (Konserve greift nie — das stimmt), aber bei null greift seit I‑2 der **erfasste Wert**, nicht 0 | `ElektrokesselStromTests` (16) | KONS berichtigen |
| Vorrang 4: **9** rückfall-ermittelbare Arten | `WirtschaftlichkeitCtrl.cs:6447–6459` | **A** | es sind **10**; `EUR_PRO_H` kam mit FX2 dazu | `BetriebskostenBaugroesseTests` (14+15) | KONS berichtigen |
| Vorrang 5: `EUR_PRO_H`, `EUR_PRO_KWH`, `PROZENT_BRENNSTOFF-`/`STROMKOSTEN` **nur Konserve (B‑4)** | `:6448–6449`, `:6500–6520` | **A, halb erledigt** | `EUR_PRO_H`, `EUR_PRO_KWH_THERMISCH`, `EUR_PRO_KWH_ELEKTRISCH` holen ihre Menge aus dem Lauf (`BetriebsstundenH`, `WaermeerzeugungKwh`, `StromgroesseKwh`); **nur** `PROZENT_BRENNSTOFFKOSTEN`/`_STROMKOSTEN` bleiben Konserve | dito | KONS berichtigen; Rest von B‑4 siehe § 6 |
| Endenergie BHKW / Kessel (Brennstoff) | `EndenergieAufloeser.cs:592–630` | U | `Verbrauch × 1000 × Arbeitspreis(CarrierId)` | `KesselBrennstoffModulTests` (7) | — |
| **Elektrokessel-Regel E1**: Gerät mit `Brennstoff = 13`, Bedarf = Σ(Waerme_Gas+Waerme_Oel)×1000, Kosten × Strompreis, Modulzeile bleibt 0 | `EndenergieAufloeser.cs:280–302` (`ElektrokesselLaden`/`IstElektrokessel`), `:366–372`, `:451–505` | U | `SimulationSPK.StromeinsatzElektrokesselMwh` ist derselbe Weg wie in der Simulation | `ElektrokesselStromTests` (16) | — |
| Wärmepumpe: (Stromverbrauch + Heizstab) × 1000 × Strompreis | `EndenergieAufloeser.cs:506–535` | U | — | `HilfsstromStrompreisJeAnlageTests` (5) | — |
| PV · Solarthermie · Speicher: null, nur Jahresbetrag | `EndenergieAufloeser.cs` (keine Zweige), `TechnikPlanwertCtrl.cs:826` | U | — | `BetriebskostenBemessungsmatrixTests` | — |
| Zwei Welten am selben Gewerk, nie eine Summe | `EndenergieAufloeser.cs:442–461` | U | anlagenscharf die Welt des Kessels, als Komponentensumme die Brennstoffwelt, nur reine Elektrokesselprojekte die Stromwelt | `ElektrokesselStromTests` | — |
| Preis = `PreisArbeit / EffHi`, ohne Grund- und Leistungspreis | `KostenEmissionRechner.cs:1154 ff.` (Kommentar) / `EndenergieAufloeser` Preisweg | U | — | `EnergiekostenGrundTests` | — |
| **Strompreis einer Anlage = der ihres eigenen Trägers** (Entscheid 19.09.2026) | `EndenergieAufloeser.cs:96–108`, `:421`, `:499`, `:532`; Leser `WirtschaftlichkeitCtrl.cs:6406–6412` | U | `BewertungspreisJeKwh`; Weg A und Weg B bewerten identisch | `HilfsstromStrompreisJeAnlageTests` | — |
| Weg B übergibt den **bewerteten** Bedarf (keine zweite Formel) | `WirtschaftlichkeitCtrl.cs:6410–6412`, `BetriebskostenCtrl.cs:106–113` | U | — | dito | — |
| Erlös: `IstErloes && wert > 0 → −wert`, an drei Stellen identisch geklemmt | `BetriebskostenCtrl.cs:137`; `WirtschaftlichkeitCtrl.cs:6090`, `:6116` | U | drei Stellen gelesen | — | — |
| Vorrang „Prozent vor Absolut", unterlegenes Feld **gesperrt, nicht geleert** (KL4) | Dialogseite (`KostenPositionCtrl`/Hüllen) | NP in diesem Teil | Rechenkern kennt nur die Bemessungsart; die Sperre ist Dialoglogik | — | — |
| `InvestSummeFuer` = `SUM(EingegebenerWert)` Kat. 1, **abgeleitete Beträge fehlen (B‑5)** | `BetriebskostenCtrl.cs:413–431` + `:291`, `:250–256` | **A, erledigt** | seit W5‑B‑8 ist die Basis `InvestKaskade.Summen`; stufig Anlage → Komponente → Projekt, vor Zuschussabzug | `BetriebskostenBasisTests` (7), `BetriebskostenAnlagenbezugTests` (2) | KONS Z. 1604–1606 berichtigen |

### 1.5 § 3.5 Energiekosten und CO₂ (KONS 1607–1712)

| Regel | Codeort | Stand | Beleg | Test | Rechenwirkung |
|---|---|---|---|---|---|
| `verbrauchJeTraeger[carrier] += Verbrauch`; Menge ≤ 0 verworfen; `carrier ≤ 0` bei Menge > 0 ⇒ `kostenVollstaendig = false` | `KostenEmissionRechner.cs:368–373`, `:320` | U | `verbrauchOhneTraeger` | `EnergiekostenGrundTests` (8+9) | — |
| Arbeitspreis [A] mit `eff_hi`, [B] ohne — **nie durch η geteilt** | `:386–394` | U | zwei Zweige | dito | — |
| Vorrang nur > 0: `custom_price_work` → `price_work` → null ⇒ Energiekosten null | `:1100–1150` | U | — | dito | — |
| Grundpreis einmal p. a.; `custom_price_base` gilt auch bei 0 (**N2**, gewollt) | `:395`, `:1144` | U (gewollte Asymmetrie) | `info.Grundpreis = sGrund ?? kGrund` — nur NULL fällt durch | `EnergiekostenGrundTests` | — |
| Leistungspreis Brennstoff: kw = (P_el+P_therm)/η bzw. P_therm/η; η > 1,5 ÷ 100, außerhalb (0;1,5] übersprungen; Saisonreihe vor konstantem Satz; JAHR/MONAT; kw ≤ 0 ⇒ kein Anteil | `:405–433`, `:751–792` (`AnschlussleistungKW`), `:793–816` (`EtaNormiert`) | U | Stromträger ausdrücklich ausgenommen (`kv.Key != stromCarrierId`) | `StromLeistungspreisTests` (12) | — |
| Netzbezug Strom = Rest × 1000 × Preis + Grundpreis + Leistungsanteil; Spitze = **Viertelstundenreihe**, MONAT = Σ₁₂, Saisonreihe zuerst; ohne Reihen kein Anteil, Träger wird benannt | `:474`, `:505–532` | U | `spitze.JahrKW` / `spitze.MonatKW[mo]` / `spitze.MonatssummeKW`; `v.LeistungspreisOhneSpitze` | `StromLeistungspreisTests` | — |
| Tarifmodus ersetzt den **ganzen** Flat-Anteil samt Leistungspreis | `WirtschaftlichkeitCtrl.cs:2030–2040` | U | `e.Energie = v.Energiekosten − v.StromkostenNetz + stromTarif` | — | — |
| **Kein Aufschlag** — Anteile zerlegen den Arbeitspreis; Σ aktive Anteile = Arbeitspreis als **Kohärenzzeile, nie Summand** | `WirtschaftlichkeitCtrl.cs:5397–5402` (`erg.AufschlagJahr = 0.0`), `EnergietraegerPreiskarte.cs:161`, `:172` | U | Spalte `Tab_Ergebnis.AufschlagBetrag` bleibt nur wegen `SELECT *` des Referenzexports | `PreisanteileTests` (8+2), `EnergietraegerPreiskarteTests` (17+16), `PreisbasenTests` (17) | — |
| Anzeigekante: € je Einheit = ct/kWh ÷ 100 × H_i; Gegenrichtung ×100 ÷ H_i; ohne Heizwert bleibt ct/kWh | `EnergietraegerPreiskarte.cs:161–176` | U | `AnteilJeEinheit` / `AnteilCtKwh` — die eine Stelle | `EnergietraegerPreiskarteTests` | — |
| Kohärenztoleranz **0,0001 €/kWh** | Anzeigekante `Preisanteile`/Preiskarte | NP | in `KohaerenzPruefung.cs` nicht gefunden; die Toleranz liegt in der Trägerkarten-Anzeige, nicht im Rechenkern | `PreisanteileTests` | — |
| Preisbasis von den Umrechnungsregeln entkoppelt (`UR‑1`): zwei Einträge, Faktor = Heizwert | `Preisbasen`/`EnergieEinheitenPruefung` | U | Restpunkt „Kennung statt Kartenzustand" steht offen (Status Z. 400, „Nach #341") | `PreisbasenTests` (17) | S, Schemaschritt |
| BEHG: `behgBasisT = CO2Brennstoff + BiogenBehgMenge × BehgOhneNachweis/1000` | `WirtschaftlichkeitCtrl.cs:2080–2090` | U | — | `Co2StromtraegerRueckfallTests` (9+6) | — |
| CO₂-Preis: Override > 0 (mit p_E fortgeschrieben), sonst Katalogpfad; **0 heißt „Pfad"** | `WirtschaftlichkeitCtrl.cs:3225–3231` (Override → `null` → Jahr‑1-Betrag `:2102‑2103`, in `KapitalwertRechner.cs:756` mit p_E), `:3237–3251` (Pfad je Kalenderjahr) | U | `GESETZ_CO2_PREIS_NEHS`, Status GESICHERT/PROGNOSE | `GesetzkatalogSaatWacheTests` | — |
| CO₂ nur aus Anlagenbrennstoff, nie aus PV | `KostenEmissionRechner.cs:368–420` (nur BHKW-/Kesselmodule in `add`) | U | — | `Co2StromtraegerRueckfallTests` | — |
| Emissionsfaktor-Kette PROJEKT → KATALOG → STAMM → CARRIER → null; Strommix-Rückfall 435 g/kWh mit Hinweis | `EmissionsFaktorLader.cs`, `Emissionsquelle.cs`; Hinweis `WirtschaftlichkeitCtrl.cs:5464–5470` | U | `KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH` | `EmissionsquelleTests` (12), `Co2StromtraegerRueckfallTests` | — |
| „keine stillen Teilsummen" | `KostenEmissionRechner.cs:17` (Klassenregel), `:320`, `:601` | U | betroffene Kennzahl bleibt `null` („—") | `EnergiekostenGrundTests` | — |
| **Kohärenzfall „CO₂ im Arbeitspreis **und** BEHG-Reihe"** | `KohaerenzPruefung.cs:123–195` | **F** | die Prüfklasse kennt keinen CO₂-Fall (gelesen); Rechenweg 04 verlangt ihn — `01/B1` | `KohaerenzNachtraegeTests` (5) deckt ihn nicht ab | M; ohne ihn kann ein Projekt CO₂ doppelt buchen (`01/B1`: −443.981 € auf ΔKW) |

### 1.6 § 3.10 Rechenreihenfolge (KONS 1996–2016)

| Schritt | Codeort | Stand | Beleg |
|---|---|---|---|
| 1–3 Investition Runden 1–3 | `InvestKaskade.cs:205 / 215 / 233` | U | drei Schleifen in dieser Reihenfolge |
| 4 Zuschussabzug nach der Positionsschleife | `KapitalwertRechner.cs:650–672` | U | nach `foreach (InvestPosition …)` |
| 5 Simulationslauf, jüngster Lauf = höchste `Tab_Ergebnis.ID` (**B‑3**) | `WirtschaftlichkeitCtrl.cs:6882–6886` | U (Befund bleibt) | `ORDER BY ID DESC LIMIT 1` — nicht der Zeitstempel |
| 6 Energiekosten **vor** Betriebsseite und Kapitalwert (R‑3) | `WindowsFormsApplication1/.../BerichtsDatenSammler.cs:421` → `WirtschaftlichkeitCtrl.cs:1973` (`e.Energie = v.Energiekosten`) | U **in der Sache, A in der Schicht** | die Energiekosten sind bei `BaueEingabe` bereits gerechnet; der Rechner wird aber **nur aus der Windows-Schale** gerufen |
| 7 Betriebskosten, `InvestSummeFuer` greift auf Kat. 1 | `WirtschaftlichkeitCtrl.cs:1951` → `:6490–6496` | U | Kaskade je Leseschleife einmal (`investSummen`-Merker) |
| 8 CO₂/BEHG nach den Trägermengen, Preis je Kalenderjahr | `WirtschaftlichkeitCtrl.cs:2080–2101` | U | — |
| 9 Vergütungen, 10 Steuern | `:2453 ff.`, `:3425 ff.` | (zweiter Bericht) | — |
| 11 Kapitalwert, dann Kennzahlen und Sensitivität | `:5018` (`RechneBild`), `:5146` | U | eine einzige Aufrufstelle von `KapitalwertRechner.Rechne` (`:5136`) |
| 12 Kohärenzprüfung zuletzt | `KohaerenzPruefung.cs:142` | U | liest gebuchte Jahr‑1-Werte |

---

## 2 § 4 Befunde (KONS 2070–2104) und § 7 Zeile B8 (KONS 2418)

### 2.1 Investitionsseite

| Nr. | Stand | Beleg |
|---|---|---|
| **I‑1** kWp = Modulanzahl | **erledigt** | `TechnikPlanwertCtrl.cs:696–700` → `PhotovoltaikCtrl.cs:227–240`: `SUM(Tab_PV.Leistung × Tab_Energieanlagen.PV_Leistung)/1000`, anlagenscharf; derselbe Kern wie Simulation und EEG-Größenklassen. Test `KwpBemessungHerleitungTests` (10 Fälle) |
| **I‑2** Ableitung ohne Satz ⇒ 0 € | **erledigt** | `BetriebskostenCtrl.cs:77–98`: `wert = eingegeben`. Unterschieden wird „nicht ermittelbar" (NULL) von „ermittelt und null" |
| **I‑3** Runde 3 reihenfolgeabhängig | **erledigt** | `InvestKaskade.cs:240–252`: Basiszeilen vor der Zuweisungsschleife eingefroren, %-Zeilen zählen einander nie mit → deterministisch **ohne** `ORDER BY` |
| **I‑4** `BaugroesseSumme` entdoppelt nicht | **offen (gewollt bei PV/Solar)** | `TechnikPlanwertCtrl.cs:714–718` (`malModulanzahl` beim Kollektorfeld), `:696` (kWp = Modulanzahl × Wp). Keine Entdopplung im Code gefunden |
| **I‑5** Vergleichsstrenge uneinheitlich | **offen** | `WirtschaftlichkeitCtrl.cs:5791` ZUSCHUSS mit `OrdinalIgnoreCase`; `:5722` und `InvestKaskade.cs:372` `PROZENT_*` mit `Ordinal` |
| **I‑6** Nicht migrierte Datenbank | **offen, bewusst** | `InvestKaskade.cs:139–141` (`StelleSpaltenSicher`) und `:151` — ohne die Spalten aus Schritt 19 gibt es weder Kaskade noch Zuschusserkennung. Auf einer SQLite-Datei nach Schritt 94 praktisch gegenstandslos |

### 2.2 Betriebsseite

| Nr. | Stand | Beleg |
|---|---|---|
| **B‑1** Kessel-Modulspalte `Verbrauch` leer | **bestätigt erledigt** | `KostenEmissionRechner.cs:271–316`: die Warnung bleibt als Wächter stehen und schweigt im frischen Lauf; Elektrokessel ausgenommen (`StromKesselNamen`, `:872`). Test `KesselBrennstoffModulTests` (7) |
| **B‑2** Asymmetrie der Rückfälle | **offen, bewusst** | `WirtschaftlichkeitCtrl.cs:6098–6108`: Endenergie-Arten unbedingt frisch (`menge = …` ersetzt), Rückfall-Arten bedingt (`if (frisch.HasValue)`). Die Asymmetrie ist im Kommentar begründet, nicht aufgelöst |
| **B‑3** „jüngster Lauf" = höchste ID | **offen** | `WirtschaftlichkeitCtrl.cs:6884` `ORDER BY ID DESC LIMIT 1` |
| **B‑4** vier Arten nie frisch | **halb erledigt** | `EUR_PRO_H`, `EUR_PRO_KWH_THERMISCH`, `EUR_PRO_KWH_ELEKTRISCH` seit FX2 frisch (`:6448`, `:6500–6520`); `PROZENT_BRENNSTOFFKOSTEN`/`_STROMKOSTEN` weiterhin nur Konserve — sie liegen seit FX4‑b zwar im p_E-Topf (`:6371–6376`), holen ihre Menge aber nicht frisch |
| **B‑5** `InvestSummeFuer` summiert `EingegebenerWert` | **erledigt** | `BetriebskostenCtrl.cs:291–320` + `:413–431` über `InvestKaskade.Summen` (W5‑B‑8), im Szenario über `BetragImSzenario` (W5‑B‑11) |
| **B‑6** Fehler werden geschluckt (`catch {}`) | **offen** | `InvestKaskade.cs:141`, `:277` (`catch { }` um die ganze Kaskade), `WirtschaftlichkeitCtrl.cs:6161` (`catch { }` um die Betriebsschleife), `BetriebskostenCtrl.cs:254` (`catch { return new Dictionary…; }`), `PhotovoltaikCtrl.cs:241` (`catch { return 0; }`) |
| **B‑7** `MengenEinheit` beschriftet neue Arten mit „€" | **offen** | `BetriebskostenCtrl.cs:201–208`: nur `EUR_PRO_H` → „h/a" und `EUR_PRO_KWH` → „kWh/a", alles andere `KOSTEN_EINHEIT_EURO`. Einzige Leserin: `WirtschaftlichkeitZeilen.cs:1071` (Herleitungszeile) |

### 2.3 Energiekosten

| Nr. | Stand | Beleg |
|---|---|---|
| **N1** Kesselbrennstoff fehlte still | **bestätigt erledigt** | `KostenEmissionRechner.cs:271–316` (Meldung ohne Ableitung), `:320`/`:601` („keine stillen Teilsummen"), `v.KesselOhneVerbrauch` als benannter Grund |
| **N2** 0 beim Grundpreis gültig, 0 beim Arbeitspreis „ungepflegt" | **offen, gewollt** | `KostenEmissionRechner.cs:1144` (`sGrund ?? kGrund` — nur NULL fällt durch) gegen `:384` (`info.PreisArbeit.HasValue`, Vorrang nur für Werte > 0, `:1100–1150`). Zwei Regeln, beide belegt |
| **N3** ungepflegte Anteilsspalten wirkten als 11,746 ct/kWh | **bestätigt erledigt** | `WirtschaftlichkeitCtrl.cs:5397` `erg.AufschlagJahr = 0.0` — es gibt keinen Aufschlagsbetrag mehr; die Anteile zerlegen den Arbeitspreis (`PreisanteileTests`, `EnergietraegerPreiskarteTests`) |

### 2.4 § 7 Zeile B8 (KONS 2418)

| Punkt | Stand |
|---|---|
| **I‑1 (kWp)** | erledigt (s. o.) |
| **I‑3 (ORDER BY)** | erledigt — allerdings **nicht** durch ein `ORDER BY`, sondern durch die Zwei-Phasen-Runde 3; die B8-Formulierung „(ORDER BY)" trifft den Weg nicht mehr |
| **B‑1/N1 (Kessel-Verbrauch)** | erledigt |
| **N3 (Aufschlags-NULL)** | erledigt |
| **V‑3 (Berichtsspalten)** | offen — gehört in den zweiten Rechenkern-Bericht (nur benannt) |
| **S‑2 (kein projektweites Doppelentlastungsverbot)** | offen — gehört in den zweiten Rechenkern-Bericht (nur benannt) |

---

## 3 § 6.2 Regressionsanker (KONS 2230–2241)

Gesucht wurde in `EPOS.Kern.Tests/`, `EPOS.Referenzlauf/`, `Referenzlauf/` mit Punkt- und
Tausenderpunkt-Varianten (`2220322`, `2.220.322`, `45312`, `12001`, `13000`, `20927`, `99.0`).

| Anker | Wert | gefunden in | aktiver Test? |
|---|---|---|---|
| `LiesBetriebskosten(1024)` | 99,00 €/a | **nirgends** | nein |
| Kapitalwert 1024 | −2.220.322,32 € | **nirgends** | nein |
| `LiesInvestitionen` 1018 | 45.312,50 | `EPOS.Kern.Tests/InvestKaskadeTests.cs:273` (`InlineData`) | **ja** |
| `LiesInvestitionen` 1024 | 12.001,00 | `InvestKaskadeTests.cs:276` | **ja** |
| `LiesInvestitionen` 1042 | 13.000,00 | `InvestKaskadeTests.cs:285` | **ja** |
| Kaskadenregression 1042 | +20.927,61 | **nirgends** | nein |
| Referenzbasis | `Referenzlaeufe/2026-09-18_R9_Kesselbrennstoff` | vorhanden, 13 Projektordner + `protokoll.txt` | — (nur Simulation, § 4) |

**Projekt 1030 ist neu verankert.** `InvestKaskadeTests.cs:281` trägt
`[InlineData(1030, 410000.0)]` innerhalb von `LiesInvestitionen_bleibt_zahlengleich`
(16 `InlineData`-Zeilen, gemessen am 08.09.2026 auf `Referenzlaeufe/Kenndaten_Test.sqlite`,
siehe Kommentar `InvestKaskadeTests.cs:259–267`). Der KONS-Satz „Die 1030-Anker sind durch
den Kaskaden-Umbau **überholt** und müssen neu gesetzt werden" (Z. 2240) ist damit für die
Investitionsseite erledigt; für Kapitalwert und Betriebskosten von 1030 gibt es weiterhin
keinen Anker.

**Kein einziger absoluter Kapitalwert-Anker im Testwerk.** Alle gefundenen Zusicherungen
sind relativ: `KapitalwertRechnerPreisindexTests.cs:81` und `:213` (Bitgleichheit alt/neu),
`KwkgPauschaleZeileTests.cs:59` (Differenz = Pauschale), `PreisInvestitionTests.cs:297`,
`:314` (Gleichheit zweier Parametersätze), `ReferenzprojektTests.cs:224` (Referenz = Stamm
rechnet gleich), `ErloesrubrikTests.cs:160` (gesetzter Wert 123.456,00 €, kein
gerechneter). Eine Rechenänderung am Kapitalwert fällt heute in **keinem** Test auf, solange
sie Stamm und Variante gleichermaßen trifft.

---

## 4 Referenzlauf — was er rechnet, und der kleinste Nachweisweg

**Gemessen (Quelltexte gelesen):** Weder `EPOS.Referenzlauf/Program.cs` (321 Zeilen) noch
die neun Dateien unter `Referenzlauf/` (3 049 Zeilen) enthalten das Wort
„Wirtschaftlichkeit", „Kapitalwert" oder „ErgebnisWirtschaftlichkeit" — einzige Treffer:
`Referenzlauf/Ergebnisexport.cs:369–370`, und das ist der **Peak-Zielwert der
Speicherflotte**, nicht die Wirtschaftlichkeit.

Eingefroren wird (`Referenzlauf/Ergebnisexport.cs:14–19`, `:443–505`):

| Gegenstand | Quelle |
|---|---|
| `aggregate.csv` — alle Skalare | `SELECT *` auf `Tab_Ergebnis`, `Tab_ErgebnisEnergiebedarf`, `Tab_ErgebnisWaermepumpe/BHKW/Heizkessel/Solarthermie/Photovoltaik` samt Modultabellen |
| Ganglinien je Modul | 8 760 Stunden- bzw. 35 040 Viertelstundenwerte als eigene CSV |
| flüchtige Spalten ausgenommen | `ID`, `ID_Ergebnis*`, `Zeitstempel` (`:22–28`) |

Stichprobe `Referenzlaeufe/2026-09-18_R9_Kesselbrennstoff/Projekt_1042/`: 29 CSV-Dateien,
`aggregate.csv` beginnt mit `Lauf.ID_Projekt;1042`, `Sim.Restwaerme`, `Sim.Reststrom` —
keine Datei enthält „kapitalwert" oder „wirtschaft" (grep, ohne Treffer).

**Folge:** Der Referenzlauf ist ein Simulations-, kein Wirtschaftlichkeitsnachweis. Eine
Änderung an Kaskade, Betriebskosten, Energiekosten oder Kapitalwert läuft dort
**unbemerkt** durch, solange sie die Simulationsgrößen nicht anfasst.

### Der kleinste Nachweisweg für Rechenänderungen dieses Teils

1. **Rein aufrufbare Rechner, dotnet-Dateiskript** (`01/§ 2`, dort belegt): `#:project` auf
   `EPOS.Kern.csproj`, `#:property AssemblyName=EPOS.Kern.Tests` für die `internal`-Sicht;
   Gesetzeswerte aus `GesetzKatalog.Vorbelegung()`, keine Datenbank. Rein und damit ohne
   Aufbau prüfbar: `KapitalwertRechner` (`Rechne`, `Ersatz`, `Barwert`, `Annuitaet`,
   `InternerZinsfuss`, `AmortisationDifferenz`) und `ErsatzRestwertTafel`. Laufzeit dort
   gemessen: 41 s Erstübersetzung, danach 1,1 s je Lauf.
2. **Nicht rein und deshalb nur über die Testdatenbank prüfbar** (`01/§ 2`, bestätigt durch
   Lesen): `WirtschaftlichkeitCtrl`, `InvestKaskade.Lies/Summen`, `BetriebskostenCtrl.Betrag`,
   `KostenEmissionRechner`, `KostenSummenCtrl`, `KostenHerleitung.Bilde`. Dafür ist
   `[Collection("Testdatenbank")]` mit `TestDatenbank`-Arbeitskopie der Weg; die 16
   `InlineData`-Zeilen in `InvestKaskadeTests` sind der einzige projektweite Zahlenanker.
3. **Vorschlag (Umsetzungsliste P‑1):** die drei fehlenden § 6.2-Anker als eigene
   Theorie-Klasse nachziehen — 99,00 (`LiesBetriebskosten(1024)`), +20.927,61
   (Kaskadendelta 1042) und einen absoluten Kapitalwert (1024) über `RechneBild`/`Rechne`
   mit festem Parametersatz. Damit hätte jede Rechenänderung dieses Teils einen A/B-Nachweis,
   ohne dass der Referenzlauf um die Wirtschaftlichkeit erweitert werden muss.

---

## 5 Testabdeckung dieses Teils

Zählung: `grep -c '\[Fact\]' / '\[Theory\]' / '\[InlineData'` je Datei.

| Rechner/Controller | Testklasse(n) | Fact | Theory | InlineData | Lücken |
|---|---|---|---|---|---|
| `KapitalwertRechner` (Rahmen, p_I) | `KapitalwertRechnerPreisindexTests` · `PreisInvestitionTests` · `Migration72Tests` | 7 · 10 · 5 | 0 | 0 | kein absoluter KW-Anker; IZF und `AmortisationDifferenz` ohne eigenen Fall |
| `KapitalwertRechner.Ersatz`/`Barwert` + `ErsatzRestwertTafel` | `ErsatzRestwertTafelTests` | 13 | 0 | 0 | `start > T` (nur Ausweis) nicht belegt |
| `InvestKaskade` (Runden 1–3) | `InvestKaskadeTests` | 7 | 1 | 16 | Runde‑2-Reihenfolge (neuer Befund) ungeprüft; Zuschussklemme `Math.Min` ungeprüft |
| Bezugsgrößen der Invest-/Betriebsseite | `BetriebskostenBaugroesseTests` · `BetriebskostenBemessungsmatrixTests` · `LeistungsbemessungBetriebTests` · `KwpBemessungHerleitungTests` · `KostenHerleitungTests` | 14 · 9 · 4 · 10 · 16 | 4 · 0 · 4 · 0 · 0 | 15 · 0 · 21 · 0 · 0 | — |
| `BetriebskostenCtrl.InvestSummeFuer` / Kaskadenbasis | `BetriebskostenBasisTests` · `BetriebskostenAnlagenbezugTests` | 7 · 2 | 0 | 0 | — |
| Szenarien (`SzenarioSatz`, `FuerSzenario`) | `SzenarioParameterTests` · `ValeriLueckenTests` | 17 · 15 | 0 | 0 | keine Fälle für Trägerpreis-, Erlös- oder Rahmen-Szenarien (gibt es nicht, § 6) |
| `EndenergieAufloeser` / E1 | `ElektrokesselStromTests` · `KesselBrennstoffModulTests` · `HilfsstromStrompreisJeAnlageTests` | 16 · 7 · 5 | 0 | 0 | eigener `ID_Carrier` an einer Anlage nur durch Testaufbau belegt (Status Z. 428 „Nach #366 (c)") |
| `KostenEmissionRechner` (Arbeits-/Grund-/Leistungspreis, CO₂) | `EnergiekostenGrundTests` · `StromLeistungspreisTests` · `Co2StromtraegerRueckfallTests` · `EmissionsquelleTests` | 8 · 12 · 9 · 12 | 1 · 0 · 2 · 0 | 9 · 0 · 6 · 0 | kein Fall für den Tarifersatz des Leistungsanteils |
| Preisanteile / Anzeigekante | `PreisanteileTests` · `EnergietraegerPreiskarteTests` · `PreisbasenTests` | 8 · 17 · 17 | 1 · 3 · 0 | 2 · 16 · 0 | — |
| `KohaerenzPruefung` | `KohaerenzNachtraegeTests` | 5 | 0 | 0 | **CO₂-Fall fehlt vollständig** (`01/B1`) |
| Nachweisumschlag | `ErgebnisNachweisPersistenzTests` | — | — | — | Investitionspositionen/Ersatz-Restwert nicht im Umschlag (§ 6) |

Summe der unmittelbar zu diesem Teil gehörenden Klassen: **27 Klassen, 253 `[Fact]`,
16 `[Theory]` mit 85 `[InlineData]`** (gezählt, nicht geschätzt).

---

## 6 Umsetzungsliste dieses Teils

Größe S ≤ ½ Tag · M ≈ 1–2 Tage · L > 2 Tage. „Rechenwirkung" heißt: der Kapitalwert eines
Bestandsprojekts kann sich ändern.

| # | Punkt | Größe | Rechenwirkung | Nachweisweg | Abhängigkeit / Reihenfolge | Schema |
|---|---|---|---|---|---|---|
| 1 | **`KostenEmissionRechner.Berechne` aus der Windows-Schale holen** — einzige Produktions-Aufrufstelle ist `WindowsFormsApplication1/Allgemein/Bericht/BerichtsDatenSammler.cs:421` (510 Zeilen Fachlogik in der Schale, gegen `CLAUDE.md:51` „Keine Fachmaske") | L | nein (Umzug) | vorhandene Kern-Tests müssen unverändert grün bleiben; Windows-Lauf | **zuerst** — ohne ihn rechnet iOS keine Wirtschaftlichkeit | nein |
| 2 | **Kohärenzzeile „CO₂ im Arbeitspreis **und** BEHG-Reihe"** in `KohaerenzPruefung.cs` (kennt heute keinen CO₂-Fall) | M | nein (Warnung) | neuer Fall in `KohaerenzNachtraegeTests`; `01/B1` liefert die Zahlen | nach Entscheid Q zu `01/B1` (Weg a/b) | nein |
| 3 | **Restreihenfolge in Kaskadenrunde 2** — Zwei-Phasen-Muster der Runde 3 (`InvestKaskade.cs:248`) auf Runde 2 (`:215–232`) übertragen | S | ja (nur bei zwei `PROZENT_ERZEUGERKOSTEN`-Hauptzeilen je Komponente) | Theorie-Fall in `InvestKaskadeTests` mit vertauschter Einfügereihenfolge | unabhängig | nein |
| 4 | **Drei fehlende § 6.2-Anker** (99,00 · −2.220.322,32 · +20.927,61) als Tests setzen | S | nein | eigene Klasse, Werte aus `01/§ 3` | **vor** jeder Rechenänderung | nein |
| 5 | **B‑4 Rest**: `PROZENT_BRENNSTOFFKOSTEN`/`_STROMKOSTEN` frisch ermitteln (heute nur Konserve, `WirtschaftlichkeitCtrl.cs:6447`) | M | ja | `BetriebskostenBaugroesseTests` erweitern; A/B gegen die drei Anker | nach 4 | nein |
| 6 | **B‑6**: `catch {}` durch benannte Fehlgründe ersetzen (`InvestKaskade.cs:141/277`, `WirtschaftlichkeitCtrl.cs:6161`, `BetriebskostenCtrl.cs:254`) | M | nein (heute stumme 0 → künftig benannt) | Fehlerfall je Stelle | nach 1 (Schichten stehen dann) | nein |
| 7 | **B‑7**: `MengenEinheit` (`BetriebskostenCtrl.cs:201`) an `BemessungKatalog` anschließen wie `SatzEinheit` (`:275`) | S | nein (Beschriftung) | `KostenHerleitungTests` | unabhängig | nein |
| 8 | **I‑5**: Vergleichsstrenge vereinheitlichen (`Ordinal` vs. `OrdinalIgnoreCase`) | S | ja (nur bei abweichender Schreibweise in Altdaten) | Fall mit `"prozent_investition"` | unabhängig | nein |
| 9 | **Ersatz/Restwert entkoppeln** — je Position oder je Technik ein Kennzeichen „Ersatz führen" / „Restwert ansetzen" (KONS § 2.13 (3) Z. 936–938; heute schaltet allein n ≥ 1 beides zugleich, `KapitalwertRechner.cs:432`) | M | ja | `ErsatzRestwertTafelTests` + neuer KW-Anker | nach 4; zusammen mit 10 | **ja** (Schritt 95 ff.) |
| 10 | **Nutzungsdauer-Vorgaben nachpflegbar machen** (KONS Z. 940–949): Knopf „Nutzungsdauern vorbelegen" für Bestandspositionen, Pflegeort für `NutzungsdauerID`, geräteeigene Dauerspalten und die Speicherflotte anschließen; heute tragen 103 von 109 Positionen keine Dauer | L | ja (sobald gepflegt) | `NutzungsdauerTests` (15) erweitern; Hinweis über `NutzungsdauerAbgleich` | offener Punkt U39 (Status Z. 419, „Nach #357 (b)") | ja |
| 11 | **Vollständige Szenarien nach § 2.11.5** (KONS Z. 693–734) — **gemessen fehlt alles außer den Positionsspalten**: es gibt weder `custom_price_work_best/_worst` noch die acht `_Best`/`_Worst`-Rahmenspalten noch einen Mengenfaktor (grep über `EPOS.Kern/`, kein Treffer). `FuerSzenario` (`WirtschaftlichkeitDaten.cs:518–531`) ersetzt heute **nur** Zinssatz, p_E, p_B und p_I; Investition, Ertrag und Nutzungsdauer wirken als **pauschale Prozent-/Jahresausschläge** je Zeile (`SzenarioSatz.InvestFaktor` `:187`, `ErtragFaktor` `:193`, `DauerFuer` `:202`) — ein anderes Modell als die im Konzept verlangten Best/Worst-Werte je Parameter | L | ja (je Pflege; NULL = wie Erwartet hält es bis dahin neutral) | `SzenarioParameterTests` je neuer Größe; Erwartet muss bitgleich bleiben | Entscheid V‑4 ist gefallen („danach", KONS Z. 691); nach 1 und 4 | **ja**, mehrere Schritte |
| 12 | **Degradation je Position (V‑G2)** — KONS Z. 645 „fehlt vollständig"; bestätigt: im Rechenkern trägt nur der Stromspeicher eine `Degradation` (`SchemaKatalog.cs:838`), keine Kostenposition | M | ja (Vorgabe 0 %/a neutral) | neuer Fall gegen `KapitalwertRechner.Rechne` | Teil von V‑E, nach 11 | ja |
| 13 | **p_I** — **nichts zu tun**, aber Konzept nachziehen: umgesetzt (W5‑B‑12, Migrationsschritt 72), Tests `PreisInvestitionTests` (10) und `Migration72Tests` (5); KONS § 3.1 kennt p_I nicht | S (Papier) | nein | — | mit § 7 dieses Berichts | nein |
| 14 | **R‑2 (Hilfsenergie mit p_B statt p_E)** — **erledigt**: `IstEnergiepreisArt` (`WirtschaftlichkeitCtrl.cs:6371`) legt beide Endenergie-Arten plus die zwei Alt-Arten in den p_E-Topf; die Hilfsstrom-Saat trägt seit Migrationsschritt 94 `PROZENT_ENDENERGIEBEDARF` (`HilfsstromBemessungVorlage.cs:72/76`) und damit p_E | — | nein | `ValeriLueckenTests`, `HilfsstromBemessungVorlageTests` | — | nein |
| 15 | **Nachweisumschlag um die Investitionsseite erweitern** — `ErgebnisNachweisUmschlag.cs:72–121` führt KWKG-Module, Energiekosten je Anlage, Betriebskostenpositionen und Kohärenzhinweise, aber **keine** Investitionspositionen und kein Ersatz-/Restwertbild; nach dem Neuladen ist die Kaskadenherleitung weg | M | nein (Ausweis) | `ErgebnisNachweisPersistenzTests` erweitern; Längenwächter 4 MiB beachten (`:65`) | nach 9 | nein (`SpalteSicher`) |
| 16 | **`Abfrage_Kostenfaktoren` um die Schritt‑19-Spalten erweitern** — die Sicht liegt seit der SQLite-Migration **im Repo** (`sql/schema/002_views.sql:55–70`) und kennt `Bemessung`, `Menge`, `Einheitpreis`, `Kostenart`, `IstErloes`, `StartJahr`, `ID_Anlage` nicht; der zweite Leseweg (KONS Z. 2398) ließe sich damit schließen | S | nein | `SchemaLayoutTests` | unabhängig | ja (View-Neubau) |

---

## 7 Aussagen des Konzepts über den Kern, die nicht mehr stimmen

Jeder Punkt gemessen (Code gelesen), mit Zeile des KONS und Berichtigungsvorschlag.

| KONS-Zeile | Aussage | Befund | Vorschlag |
|---|---|---|---|
| 1543–1544 | „Sperre zuerst: fehlt Menge **oder** Satz ⇒ Betrag = 0, nicht der gespeicherte Wert" | **falsch** seit FX2/I‑2: `BetriebskostenCtrl.cs:77–98` gibt den erfassten Wert zurück | „Fehlt Menge oder Satz, ist die Ableitung nicht rechenbar; dann gilt der erfasste Betrag (Anwenderentscheid I‑2, 30.08.2026). Eine ermittelte Menge 0 rechnet weiter zu 0." |
| 1553 | „Endenergie-Arten … Auflöser null ⇒ **Betrag 0** — die Konserve greift nie" | halb falsch: Konserve greift wirklich nie, aber der Betrag ist der erfasste Wert | „… Auflöser null ⇒ erfasster Betrag (I‑2); die Konserve greift nie." |
| 1555–1557 | „Rückfall-ermittelbare Arten (**9 Stück**)" / „Übrige Arten (`EUR_PRO_H`, `EUR_PRO_KWH`, …): nur Konserve (Befund B‑4)" | falsch: es sind **10** (`WirtschaftlichkeitCtrl.cs:6447–6459`); `EUR_PRO_H` und die beiden `EUR_PRO_KWH_*` sind seit FX2 frisch | „Rückfall-ermittelbare Arten (10 Stück) … Nur Konserve bleiben `PROZENT_BRENNSTOFFKOSTEN` und `PROZENT_STROMKOSTEN` (Rest von B‑4)." |
| 1478 | „Lesepunkt `Tab_ProjektWerte` mit `KategorieID = 1`, **ohne ORDER BY** (Befund I‑3)" | irreführend: die Abfrage hat weiterhin kein `ORDER BY` (`InvestKaskade.cs:163–168`), das Ergebnis ist aber seit FX2 reihenfolgeunabhängig | „… ohne `ORDER BY`; die Runde 3 friert ihre Basiszeilen vorher ein und ist deshalb reihenfolgeunabhängig (I‑3 erledigt)." |
| 1494 · 2076 | `EUR_PRO_KWP` „Σ `Tab_Energieanlagen.PV_Leistung` × Satz ⚠ **I‑1**" | falsch seit FX1/FX2: `PhotovoltaikCtrl.KwpSumme` rechnet Modulanzahl × Modulleistung ÷ 1000 | Formelzelle auf „Σ (Modulanzahl × Modulleistung)/1000 × Satz, `PhotovoltaikCtrl.KwpSumme`" ändern; Warnzeichen streichen, I‑1 in § 4 auf ✔ setzen |
| 1604–1606 | „`InvestSummeFuer`: `SUM(EingegebenerWert)` … abgeleitete Beträge fehlen (Befund B‑5)" | falsch seit W5‑B‑8: Basis ist `InvestKaskade.Summen` (`BetriebskostenCtrl.cs:291–320`) | „`InvestSummeFuer`: Summe der Investitionskaskade, stufig Anlage → Komponente → Projekt, vor Zuschussabzug (B‑5 erledigt)." |
| 1396–1420 (§ 3.1-Formelkarte) | A_t kennt nur zwei Preissteigerungstöpfe, kein p_I, keinen Endenergie-Topf | unvollständig: `KapitalwertRechner.cs:725–736` und `:474–478` | dritte Zeile `Endenergie_1 × (1+p_E)^(t−1)` und `Ersatz_t = A₀·(1+p_I)^t` aufnehmen (Quelle: Szenarienkonzept § 10.1) |
| 2080 | I‑3: „ohne ORDER BY entscheidet **ACE**" | ACE gibt es nicht mehr: `System.Data.OleDb` ist weder Quelltext noch PackageReference (`EPOS.Kern/Allgemein/DataRepository.cs:30`) | „… entschied früher die Datenbank" — Vergangenheitsform, ACE streichen |
| 2356–2360 | drei ACE-Fallen („`UPDATE … WHERE x IN (SELECT …)` trifft 0 Zeilen", „falscher Spaltenname meldet fehlenden Parameter", „zwei gemischte ACE-Verbindungen") | gegenstandslos auf SQLite | als „Altlasten des Access-Zweigs" kennzeichnen oder nach `ueberholt/` verschieben |
| 2398 | „Zwei Lesewege auf die Kostenposition — die **gespeicherte Access-Abfrage** kennt die neuen Spalten nicht" | halb falsch: die Abfrage ist seit der Migration eine **versionierte SQLite-Sicht im Repo** (`sql/schema/002_views.sql:55`), die Spalten fehlen dort weiterhin | „… die Sicht `Abfrage_Kostenfaktoren` (`sql/schema/002_views.sql`) kennt die neuen Spalten nicht — sie liegt im Repo und ließe sich erweitern (Umsetzungsliste 16)." |
| 2400 | „Vorrang Projekt vor Katalog in drei Implementierungen … **eine Access-Abfrage**" | dieselbe Berichtigung | „… und die Sicht `Abfrage_Energietraeger_Effektiv` (`sql/schema/002_views.sql:26`)" |
| Codekommentar `WirtschaftlichkeitCtrl.cs:6177–6180` | „die gespeicherte Access-Abfrage liegt **außerhalb des Repos**" | falsch (dieselbe Sicht liegt im Repo) | Kommentar berichtigen |
| 107–108 · 119 · 225 · 241 · 468 · 481 · 523 · 591 · 664 · 682 · 700 · 728 | `Form_BhkwWirtschaftlichkeit`, `Form_PhotovoltaikVerguetung`, `Form_WirtschaftlichkeitParameter`, `Form_KostenKomponente`, `Form_CaseEingabe`, `UcWirtschaftlichkeit`, `UcBkKosten` | **weiterhin gültig als Hilfe-/KI-Kennungen**, nicht mehr als Klassennamen: die Dialoge heißen heute `…Huelle` (Windows-Schale) und `…Daten`/`…Texte` (`EPOS.UI/Dialoge/…`), z. B. `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitTexte.cs:16` | einmal im Konzept erklären: „`Form_*`/`Uc*` sind die eingefrorenen Hilfe- und KI-Kennungen; die Klassen tragen seit der Schalentrennung `…Huelle`/`…Daten`" |
| 136 | „8 Bestandsfelder (heute in `Form_KwkgModule`)" | `Form_KwkgModule` existiert im ganzen Baum nicht mehr (grep ohne Treffer) | Kennung ersetzen oder Zusatz „aufgelöst mit BK1" |
| 2240 | „Die 1030-Anker sind durch den Kaskaden-Umbau überholt und müssen neu gesetzt werden" | für die Investitionsseite erledigt: `InvestKaskadeTests.cs:281` verankert 1030 mit 410.000,00 € | „1030 ist auf der Investitionsseite neu verankert (410.000,00 €); Kapitalwert und Betriebskosten von 1030 tragen weiterhin keinen Anker." |
| 2418 (§ 7 B8) | „I‑3 (ORDER BY)" | der Weg war ein anderer (Zwei-Phasen-Runde 3) | „I‑3 (Runde 3 reihenfolgeunabhängig)" |
| Kopfzeile | „Stand 02.09.2026, Zielversion 61" | Zielversion ist 94 (`SchemaStand.cs:265`) | Kopf auf 19.09.2026 / Zielversion 94 ziehen |

---

## Nicht geprüft

- **Nicht gerechnet und nicht nachgerechnet.** Kein Programmlauf, kein Test, kein Build —
  Auftragsregel (1). Alle Zahlen stammen aus gelesenen Quelltexten, aus `01/…` oder aus dem
  KONS.
- **Zweiter Rechenkern-Teil:** KWKG (§ 3.6/3.7), Energie- und Stromsteuer (§ 3.8), PV/EEG
  (§ 3.9), Erlösrubrik, Ergebnisansicht, VALERI-Szenarienansicht, Befunde V‑3 und S‑2 — nur
  benannt, nicht untersucht.
- **`WirtschaftlichkeitCtrl.cs`** (7 336 Zeilen) wurde abschnittweise gelesen
  (`LadeParameter`, `BaueEingabe`, `RechneBild`, `BaueSensitivitaet`, `RechneProjekt`,
  `LiesInvestitionen`, `LiesBetriebskosten*`, `Szenariowert`, `BaueCo2Reihe`, `LiesErgebnisId`),
  nicht vollständig; `BaueKwkgReihe`, `BaueSteuerReihen`, `RechneRollentarif`,
  `RechnePvVerguetung` und `Persistiere` blieben außen vor.
- **`WirtschaftlichkeitZeilen.cs`, `WirtschaftlichkeitEmpfehlung.cs`, `GesetzKatalog.cs`,
  `StromMatrix.cs`, `StromTarifRechner.cs`, `BilanzKonvention.cs`** nur per grep berührt.
- **Kohärenztoleranz 0,0001 €/kWh** (KONS Z. 1676) nicht im Code lokalisiert — in
  `KohaerenzPruefung.cs` nicht vorhanden; vermutlich in der Trägerkarten-Anzeige
  (`Preisanteile`/`EnergietraegerPreiskarte`), dort nicht nachgesehen.
- **`Tab_ErgebnisWirtschaftlichkeit`**: die Spaltenliste wurde nicht gelesen; die Aussage
  „der Referenzlauf kennt sie nicht" stützt sich auf die Exportquelle
  (`Referenzlauf/Ergebnisexport.cs:443–505`), nicht auf das Schema.
- **`BerichtsDatenSammler.cs`** (510 Zeilen) nur Kopf und Aufrufstelle gelesen; wie tief die
  Fachlogik dort reicht, ist **nicht** gemessen — die Größenschätzung „L" für
  Umsetzungspunkt 1 ist insofern vermutet.
- **Testdatenbank und Referenzläufe** nicht geöffnet; die Ankerlage stützt sich allein auf
  Quelltextsuche in `EPOS.Kern.Tests/`.
- **`EPOS.UI`, `EPOS.UI.Daten`, `EPOS.iOS`** und die Mockups: nicht Gegenstand dieses Teils.
