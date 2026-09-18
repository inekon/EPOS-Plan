# 03 · Kosten der Photovoltaik

**Dialog:** `KostenKomponenteDialog` (`EPOS.UI/Dialoge/Kosten/`), Klappliste auf der PV-Anlage — dasselbe
Fenster wie in `01` und `02` · **Mockup:** `../../Mockups/Dialog_Formel_Zahlenprobe.html#pvkosten` ·
**Norm:** VDI 2067 · DIN EN 17463, 6.4 (Endzahlungen statt Restwert, siehe `01`) ·
**Code:** `BemessungKatalog.Auswahl` (Gewerk 3), `PhotovoltaikCtrl.KwpSumme`, `PhotovoltaikCtrl.KwpHerleitung`,
`TechnikPlanwertCtrl.BaugroesseSumme`, `InvestKaskade`, `EndenergieAufloeser`, `KostenHerleitung.Bilde`,
`KostenSummenCtrl.Fuss`, `KostenSummenCtrl.KennzahlText` ·
**Konzept:** § 2.1, § 3.1, § 3.2

## Was die Photovoltaik anders macht — und was der Dialog deshalb anders zeigt

| PV-Eigenheit | Im Dialog |
|---|---|
| Die Baugröße ist die installierte Leistung in kWp und wird gerechnet (Modulleistung × Modulanzahl ÷ 1000), nicht aus einer Gerätespalte gelesen | Bemessung „je kWp Leistung" (und gleichbedeutend „je kW elektrisch"); Herleitungszeile „× 300,00 kWp · kWp der Anlage · Runde 1 · 750 Module × 400 Wp = 300,00 kWp" — die beiden Faktoren stehen mit, weil sie in keiner Maske nebeneinander stehen |
| Die Anlagenspalte `Tab_Energieanlagen.PV_Leistung` ist die Modulanzahl | Ein €/kWp-Satz trifft nie sie, sondern nur die gerechneten kWp (`PhotovoltaikCtrl.KwpSumme`, dieselbe Rechnung wie Simulation und Vergütungsdialog); fehlt ein Modul mit Leistung: ⚠ und Grund „kein Gerät mit dieser Baugröße im Projekt" |
| PV hat keine Endenergie | Die Endenergie-Arten stehen nicht in der Klappliste des Betriebsrasters; Hilfsenergie nur als fester Jahresbetrag, keine Pflicht |
| Der Wechselrichter lebt 12 Jahre, die Module 25 | Spalte Nutzungsdauer [a] wie bei jeder Technik; Ersatz und Restwert rechnet der Dialog nicht vor (U30, Tafel in `01`) |
| Der Ertrag altert | Kein Kostenattribut: Feld „Degradation [%/a]" im Vergütungsdialog (`06`), je Stammprojekt, wirkt in der Erlösreihe |
| Zuschuss und EEG-Vergütung schließen sich in der Regel aus | Keine Zuschusszeile im Beispiel — der Summenfuß bleibt zweizeilig (die dritte Zeile steht nur mit Erlös-/Zuschusszeile) |
| Die branchenübliche Kennzahl ist €/kWp | Der Summenfuß nennt sie als „spezifisch 640,50 €/kWp"; ein Wartungssatz wird als „je kWp Leistung" mit der Einheit „€/kWp·a" bemessen |

## Was der Dialog zeigt — Investitionskosten

Klappliste „Komponente:" auf „PV-Anlage 1", Optionsgruppe auf „Investitionskosten", Reiter „Kosten Invest/Betrieb"
und — wie beim Blockheizkraftwerk — „Ertrag/Bonus". Das Zeilenraster trägt die sieben Spalten aus `01`: Aktionen ·
Position (Textfeld) · Bemessung (Klappliste, an der Photovoltaik: fester Betrag · % der Investition · % der
Erzeugerkosten · je kW elektrisch · je kWp Leistung) · Satz (Zahlenfeld, Einheit €/kWp, % oder €) · Betrag netto [€]
(gerechnet, nie eingebbar; 🔗 bei fester Bemessung) · Nutzungsdauer [a] · Worst/Best. Unter dem Betrag jeder
gerechneten Zeile die Herleitungszeile mit Bezugsgröße, Herkunft und Runde — bei einer GERECHNETEN Bezugsgröße
dazu ihre Faktoren; der Werkzeugtipp nennt die Rechnung („320,00 €/kWp × 300,00 kWp") und darunter dieselbe
Faktorenzeile. Summenfuß: „Summe Investitionskosten netto: 192.150,00 €", „Summe brutto:
228.658,50 € (Umsatzsteuer 19 % aus dem Katalog)", „spezifisch 640,50 €/kWp"; keine dritte Betragszeile, weil
keine Zuschusszeile. Knöpfe „+ Position hinzufügen", „Aus Vorlage übernehmen…", „Positionskatalog…";
Fußleiste Abbrechen · Speichern · OK.

| Position | Bemessung | Satz | Bezugsgröße (Werkzeugtipp) | Herleitungszeile | Betrag | Nutzungsdauer |
|---|---|---|---|---|---|---|
| PV-Module (Hauptposition der Vorlage) | je kWp Leistung | 320,00 €/kWp | 300,00 kWp — `PhotovoltaikCtrl.KwpSumme` (750 × 400 Wp ÷ 1000) | × 300,00 kWp · kWp der Anlage · Runde 1 · 750 Module × 400 Wp = 300,00 kWp | 96.000,00 | 25 |
| Wechselrichter | je kWp Leistung | 80,00 €/kWp | 300,00 kWp | dieselbe Zeile | 24.000,00 | 12 |
| Unterkonstruktion und Montage | je kWp Leistung | 150,00 €/kWp | 300,00 kWp | dieselbe Zeile | 45.000,00 | 25 |
| Elektroinstallation, Netzanschluss | fester Betrag | 18.000,00 € | — | Satz = Betrag (🔗) | 18.000,00 | 25 |
| Planung und Genehmigung | % der Investition | 5,00 % | 183.000,00 € — Stufe Anlage | × 183.000,00 € · Stufe Anlage · Runde 3 | 9.150,00 | — |
| **Summe** | | | netto = I₀, brutto 228.658,50 €, spezifisch 640,50 €/kWp | | **192.150,00** | |

**Was das Mockup darüber hinaus zeigt**, steht im Anhang Umsetzungsstand: die Gruppe „Ersatz und Restwert"
(U30) — Wechselrichter n = 12 a → Ersatz im Jahr 12 mit 24.000,00 €, Restwert am Ende 39.800,00 €, Barwerte
16.833 und 22.036 € (Rechnung in `01`).

## Was der Dialog zeigt — Betriebskosten

Optionsgruppe auf „Betriebskosten": Betragsspalte „Betrag netto [€/a]", Spalte Nutzungsdauer leer, Bemessungen des
Betriebsrasters an der Photovoltaik: fester Jahresbetrag · % der Investition · je kWh elektrisch (erzeugter Strom
aus dem Lauf; Herleitungszeile „× 285.000,00 kWh · Lauf"; ohne Lauf ⚠ und „kein Simulationslauf") · je kWp
Leistung (Satz in €/kWp·a — die Leistung kennt kein Jahr, der Satz trägt es deshalb selbst). Die
Endenergie-Arten fehlen, weil der `EndenergieAufloeser` für die Photovoltaik keine Menge liefert. Pflichtzeilen
der Vorlage (Schemaschritt 59): „Wartung / Inspektion PV-Anlage" und „Instandhaltung
PV-Module / Gestell" (% der Investition); Hilfsenergiekosten sind keine Pflicht und nur als Jahresbetrag erfassbar.

| Position | Bemessung | Satz | Herleitungszeile | Betrag |
|---|---|---|---|---|
| Wartung / Inspektion PV-Anlage — Pflicht | je kWp Leistung | 12,00 €/kWp·a | × 300,00 kWp · kWp der Anlage · 750 Module × 400 Wp = 300,00 kWp | 3.600,00 |
| Instandhaltung PV-Module / Gestell — Pflicht | % der Investition | 0,50 % | × 192.150,00 € · Investitionssumme | 960,75 |
| Versicherung, Steuern, Verwaltung — Empfehlung 0,8–2 % | % der Investition | 0,25 % | × 192.150,00 € · Investitionssumme | 480,38 |
| Telekommunikation / Monitoring | fester Jahresbetrag | 600,00 €/a | Satz = Betrag (🔗) | 600,00 |
| Hilfsenergiekosten — keine Pflicht | fester Jahresbetrag | 90,00 €/a | Satz = Betrag (🔗) | 90,00 |
| **Summe Betriebskosten netto** | | | brutto 6.820,04 €/a | **5.731,13** |

Der Wartungsansatz 12,00 €/kWp·a × 300,00 kWp wird je kWp bemessen; die Bezugsgröße ist dieselbe kWp-Summe wie
auf der Investitionsseite, und die Herkunft der Zeile ist deshalb die Anlage, nicht der Lauf. Die Degradation ist
kein Attribut dieses Dialogs; eine Quellenangabe zu ihr gibt es nirgends (U9).

## Was der Dialog zeigt — Reiter Ertrag/Bonus

Bei der Photovoltaik eine Gruppe „PV-Vergütung (EEG) — eine Vergütungswahrheit (V4/F7)": Erklärungssatz
(`KDLG_ERTRAG_PV`), Klappliste „Stammprojekt:" und Knopf „PV-Vergütungsdialog öffnen…" — der Dialog aus `06` —,
daneben „Gesetzesparameter…" (Gesetzeskatalog als Überlagerung). Kein zweiter Rechenweg.

## Berechnungsgrundlage

```
Bezugsgröße — kWp, gerechnet statt gelesen (TechnikPlanwertCtrl.BaugroesseSumme → PhotovoltaikCtrl.KwpSumme)
  kWp = Σ Tab_PV.Leistung [W je Modul] × Tab_Energieanlagen.PV_Leistung [Modulanzahl] ÷ 1000
        auf die Anlagenzeile eingegrenzt; Summe ≤ 0 ⇒ null (⚠ „kein Gerät mit dieser Baugröße im Projekt")
  „je kWp Leistung" und „je kW elektrisch" meinen dieselbe Größe (IstPvLeistungsart) — EINE Rechnung
  Herleitungszeile: × {kWp} · kWp der Anlage · Runde 1   (KDLG_HERL_BASIS, KDLG_HERK_ANLAGE, KDLG_GR_KWP)
                    · {Module} Module × {Wp} Wp = {kWp} kWp            (KDLG_HERL_KWP)
                    mehrere Stränge:  Σ Module × Leistung = {kWp} kWp  (KDLG_HERL_KWP_SUMME)

Runde 1 — direkte Arten     Betrag = kWp × Satz ;  fester Betrag: Satz = Betrag
Runde 2 — % der Erzeugerkosten   Basis = Hauptposition „PV-Module"  (im Beispiel nicht belegt)
Runde 3 — % der Investition      Basis = Σ Runden 1 und 2 der Anlage (Stufe Anlage; siehe 01)
Summenfuß                        netto = I₀ ; keine Zuschusszeile ⇒ keine dritte Zeile (KostenSummenCtrl.Fuss)
Kennzahl                         spezifisch = netto ÷ kWp   (KostenSummenCtrl.KennzahlText, KDLG_KENN_EUR_KWP)
                                 nur an der Photovoltaik mit gepflegten kWp; reine Anzeige

Betrieb — keine Kaskade, keine Endenergie
  A  fester Jahresbetrag      Betrag = Satz
  B  % der Investition        Betrag = Investitionssumme × Satz / 100     „× 192.150,00 € · Investitionssumme"
  C  je kWh elektrisch        Betrag = erzeugter Strom [kWh] × Satz       „× 285.000,00 kWh · Lauf"
  D  je kWp Leistung          Betrag = kWp × Satz [€/kWp·a]               „× 300,00 kWp · kWp der Anlage"
  Endenergie-Arten: nicht in der Auswahl (BasisGrund = GEWERK) — Hilfsenergie nur als Jahresbetrag

Ersatz und Restwert   technikneutral aus der Nutzungsdauer; Formel, Tafel und Barwerte in 01 (U30)
Degradation           kein Kostenattribut — Feld des Vergütungsdialogs (06), wirkt in der Erlösreihe
```

## Berechnungserläuterung am Beispielprojekt

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| R1 Bezugsgröße | 750 × 400 / 1000 | 300,00 kWp | KwpSumme der Anlagenzeile; die Herleitungszeile nennt Ergebnis und Faktoren |
| R1 PV-Module | 300,00 × 320,00 | 96.000,00 € | Hauptposition der Vorlage |
| R1 Wechselrichter | 300,00 × 80,00 | 24.000,00 € | n = 12 a |
| R1 Unterkonstruktion | 300,00 × 150,00 | 45.000,00 € | |
| R1 Elektroinstallation | Satz = Betrag | 18.000,00 € | 🔗 |
| **Basis für Runde 3** | 96.000 + 24.000 + 45.000 + 18.000 | **183.000,00 €** | Stufe Anlage |
| R3 Planung 5 % | 183.000,00 × 5 / 100 | 9.150,00 € | „× 183.000,00 € · Stufe Anlage · Runde 3" |
| **I₀** | 183.000,00 + 9.150,00 | **192.150,00 €** | Nettosumme; brutto 228.658,50 € |
| Kennzahl | 192.150,00 / 300,00 | 640,50 €/kWp | „spezifisch 640,50 €/kWp" im Summenfuß |
| Ersatz Wechselrichter (U30) | t = 12 | 24.000,00 € im Jahr 12 | Barwert 16.833 € (Rechnung in 01) |
| Restwert Photovoltaik Jahr 20 (U30) | 19.200 + 8.000 + 9.000 + 3.600 | 39.800,00 € | Barwert 22.036 € (Rechnung in 01) |
| B Wartung | 12,00 × 300,00 | 3.600,00 €/a | Pflicht; Bemessung „je kWp Leistung" |
| B Instandhaltung 0,50 % | 192.150 × 0,50 / 100 | 960,75 €/a | Pflicht |
| B Versicherung 0,25 % | 192.150 × 0,25 / 100 | 480,38 €/a | |
| B Monitoring + Hilfsenergie | 600 + 90 | 690,00 €/a | Jahresbeträge |
| **Betriebskosten Jahr 1** | 3.600 + 960,75 + 480,38 + 690 | **5.731,13 €/a** | brutto 6.820,04 €/a |

## Befunde und offene Punkte

| Nr. | Punkt | Behandlung |
|---|---|---|
| I-1 | `PV_Leistung` ist die Modulanzahl; ein €/kWp-Satz darf sie nie treffen | gebaut: `BaugroesseSumme` rechnet die kWp über `PhotovoltaikCtrl.KwpSumme`; die Auswahl bietet nur Arten mit Bezugsgröße |
| U35 | Die Herleitung der kWp (Modulanzahl × Modulleistung) fehlte — `BaugroesseHerleitung` lieferte für die Photovoltaik leer | gebaut: `PhotovoltaikCtrl.KwpHerleitung` liefert „750 Module × 400 Wp = 300,00 kWp" für Werkzeugtipp und Herleitungszeile; bei mehreren Strängen die Summenform |
| U33 | Kein Wartungssatz je kWp·a im Betriebsraster (`BM_KWP` nur im Investitionsraster) | gebaut: `BM_KWP` ist auch `FuerBetrieb`, Einheit „€/kWp·a"; keine bestehende Position ändert sich |
| U34 | Kennzahl „spezifisch €/kWp" im Summenfuß | gebaut: `KostenSummenCtrl.KennzahlText`, Nettosumme ÷ kWp; ohne kWp entfällt sie |
| U30 | Gruppe „Ersatz und Restwert" unter dem Raster | Tafel und Barwerte in `01`; Zahlenprobe Photovoltaik: Jahr 12 24.000,00 €, Restwert 39.800,00 € |
| U9 | Degradation je Stammprojekt im Vergütungsdialog, ohne Quellenangabe | `06`; Vorschlag im Anhang |
| — | Kumulierung Zuschuss / EEG | keine Zuschusszeile im Beispiel; erfassbar wie in `01` |
