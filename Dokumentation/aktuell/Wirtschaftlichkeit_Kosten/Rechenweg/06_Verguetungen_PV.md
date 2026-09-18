# 06 · Vergütungen Photovoltaik

**Dialog:** `PhotovoltaikVerguetungDialog` (`EPOS.UI/Dialoge/Wirtschaftlichkeit/`), Hülle
`WindowsFormsApplication1/Views/Wirtschaftlichkeit/PhotovoltaikVerguetungHuelle.cs` · **Mockup:**
`../../Mockups/Dialog_Formel_Zahlenprobe.html#pv` · **Recht:** § 21, § 21c, § 25, § 49, § 51, § 51a EEG ·
**Code:** `EegSatzRechner` (anzulegender Wert), `PvErloesRechner` (Erlösreihe `PV_VERGUETUNG`),
`PvKennzahlenRechner` (Kennzahlzeile), `GesetzKatalog` (Klasse EEG), `ProjektPhotovoltaikCtrl.Jahresmarktwert` ·
**Konzept:** § 2.3, § 3.6 (Photovoltaik / EEG)

Die PV-Seite ist der Gegenentwurf zum BHKW: keine Staffel über Leistungsanteile im Zuschlag, dafür
ein anzulegender Wert, der degressiv altert, eine Marktprämie als Differenzgröße und zwei
Kürzungstatbestände — negative Preise und die 60-%-Kappung. Der Dialog rechnet nichts selbst; Sätze,
Reihe und Kennzahlen kommen aus den drei Rechnern des Kerns, der Katalog als Delegat.

## Was der Dialog zeigt

Schalter „Vergütung anwenden" im Kopf, dann sieben Gruppen untereinander in der Reihenfolge des Dialogs. Den Satz gibt
der Anwender an **einer** Stelle ein: **AW-Override [ct/kWh] (0 = Katalog)** in der Gruppe „Anzulegender Wert"; leer
oder 0 heißt, der Katalogwert gilt, und die Zeilen darunter sagen, wie er entsteht. Feste EV, Statuszeilen, Vorschau
und Kennzahlen sind Herleitungszeilen, keine Felder. Neun Zahlen-, Datums- und Ganzzahlfelder, zwei Optionsgruppen,
zwei Klapplisten „Anwenden", drei Schalter.

| Gruppe (Ressourcentext) | Felder und Zeilen im Beispiel |
|---|---|
| **Anlage** | Zeile „Installierte Leistung: rechnerisch 300,0 kWp" (dieselbe kWp-Summe wie in `03`) · Zahlenfeld Override [kWp] (0 = keiner) 0,00 · Datumsfeld Inbetriebnahme 01.08.2026 · Zahlenfeld Degradation [%/a] 0,50 mit Werkzeugtipp (wirkt nur in der Erlösreihe) · Optionsgruppe Einspeiseart: **Überschusseinspeisung** / Volleinspeisung · Warnband nur über 1 MW („Ausschreibung — AW-Override nötig"); die Zeile „über 2 MW: Stromsteuer prüfen" kann nicht erscheinen (U36) |
| **Anzulegender Wert** | Zahlenfeld **AW-Override [ct/kWh] (0 = Katalog)** 0,00 · Zeile „AW_mix: **6,04 ct/kWh**" (mit Override: „AW_mix: 7,00 ct/kWh (Override)") · Katalogherleitung des Satzrechners: „IBN 01.08.2026: 6 Degressionsschritte (1 %/Halbjahr ab 01.02.2024), Faktor 0,9415. bis 10 kW: 10 kW × 8,10 ct/kWh; bis 40 kW: 30 kW × 7,06 ct/kWh; bis 100 kW: 60 kW × 5,84 ct/kWh; bis 400 kW: 200 kW × 5,84 ct/kWh; AW_mix = 6,04 ct/kWh (Überschusseinspeisung)." · Zeile „Feste EV (AW − 0,40): 5,64 ct/kWh" |
| **Vermarktung** | Optionsgruppe: Feste Einspeisevergütung (gesperrt über 100 kW) · **Direktvermarktung mit Marktprämie** · Sonstige Direktvermarktung / PPA · Keine Vergütung (unentgeltlich) (gesperrt ab 200 kW) · Zahlenfeld DV-Entgelt [ct/kWh] 0,40 (nur bei Marktprämie aktiv) · Zahlenfelder PPA-Festpreis [ct/kWh] (0 = keiner) und PPA-Aufschlag auf Spot [ct/kWh], nur bei sonstiger Direktvermarktung aktiv · Hinweiszeile „Feste EV nur bis 100 kW (§ 21 Abs. 1 Nr. 1 EEG)." · Der Jahresmarktwert ist kein Feld und keine Zeile (U27): amtlicher Wert des Kalenderjahres aus dem Marktwert-Import, ersatzweise der letzte bekannte Wert (2025: 4,508 ct/kWh) mit der Marktwertentwicklung fortgeschrieben; das Beispiel nimmt importierte 4,50 ct/kWh für 2026 |
| **Vergütungsausfall (§ 51 / § 51a)** | Klappliste Anwenden: Automatisch · Ganzzahlfeld iMSys-Einbaujahr (0 = keins) 0 · Zahlenfeld Ausfallanteil der Einspeisearbeit [%] 20,0 · Schalter § 51a-Kompensation (Laufzeitverlängerung) ✓ · Statuszeile „greift ab der ersten negativen Viertelstunde." (Inbetriebnahme ab dem 25.02.2025, Anlage ≥ 100 kWp) |
| **Strompreis / Bezugsbewertung** | Schalter „Netzbezug stundenscharf aus Preiszeitreihe bewerten" · Hinweiszeile zur Stromsteuerfreiheit des Eigenverbrauchs (§ 9 StromStG) |
| **60-%-Wirkleistungsbegrenzung (§ 9 Abs. 2 EEG)** | Klappliste Anwenden: **Ja** · Statuszeile „aktiv: Einspeisung auf 60 % der kWp begrenzt (ohne iMSys)." — „Automatisch" begrenzt nur bei fester Einspeisevergütung ohne iMSys, bei Direktvermarktung greift die Kappung nur mit „Ja" · Der Verlust Σ max(0; Einsp_h − 180 kW) ist nur mit Stundenreihe messbar: in der Vorschau (Stufe 1) 0 kWh, im Lauf aus der Reihe `PV_UEBERSCHUSS` gemessen |
| **Vorschau** | Zeile „Einspeisung 199,5 MWh/a · Satz Jahr 1: 4,51 ct/kWh · Erlös Jahr 1: 9.001 €/a · Vergütungsausfall 39.900 kWh (2.410 €) · § 51a-Gutschrift 1.096 € (Jahr 20)" · Kennzahlzeilen „Eigenverbrauchsquote 30,0 % · Autarkiegrad 6,0 % · Vorteil durch PV: 16.791 €/a" und „Stromgestehungskosten: 5,38 ct/kWh (LCOE₀, mit Satz vergleichbar) · 6,54 ct/kWh (diskontiert)" · ohne Lauf: „Noch kein Simulationsergebnis — …" |

Knöpfe „Marktwerte importieren…" (nur, wenn die Umgebung eine Datei wählen kann) und „Einspeise-Tarif…" mit
Sprunghinweis; Fußleiste: Abbrechen · Übernehmen (schreibt den Vergütungssatz des Stammprojekts; die Inbetriebnahme
ist Pflicht).

**Vorschau Jahr 1 (2026), aufgeschlüsselt** (U27 — der Dialog zeigt die eine Zeile):

| Position | Herleitung | € |
|---|---|---|
| Spoterlös | 159,6 MWh × 4,50 ct | 7.182,00 |
| Marktprämie | 159,6 MWh × (6,04 − 4,50) ct | 2.457,84 |
| DV-Entgelt | 159,6 MWh × 0,40 ct | − 638,40 |
| Kappungsverlust 60 % | Stufe 1: nicht messbar | 0 kWh |
| **Vergütung PV** | | **9.001,44** |
| Vergütungsausfall § 51 — Ausweis | 39,9 MWh abgeregelt × 6,04 ct | 2.409,96 |
| Vermiedener Netzbezug — Ausweis | 85,5 MWh × 28,80 ct | 24.624,00 |

## Berechnungsgrundlage

```
Anzulegender Wert (EegSatzRechner)
  Degression   Faktor 0,99^n   (n = Halbjahresstichtage 1.2. / 1.8. ab 01.02.2024 bis zur Inbetriebnahme)
  AW_Klasse  = round( Basis_Klasse × 0,99^n , 2 )      marginale Klassen 10 / 40 / 100 / 400 / 1000 kWp
  AW_mix     = round( Σ Anteil_k × AW_Klasse_k / Σ Anteil_k , 2 )
  EV_fest    = max(0, AW_mix − 0,40)              nur ≤ 100 kW
  Ausfallvergütung = AW × (1 − 20 %)              nur > 100 kW

Vergütungsdauer   20 Jahre + Restmonate des Inbetriebnahmejahres (§ 25 Abs. 1)
                  IBN August: 245 Monate ⇒ letztes Vergütungsjahr = min(T, 21) = 20

§ 51 — negative Preise, je Jahr (AUTO)
  IBN < 25.02.2025 → nein ; ≥ 100 kWp → ja ; sonst ab dem Jahr nach dem iMSys-Einbau
  Ausfallarbeit  = Einspeisung × a          a = 20 % Pauschale (Stufe 1)  ODER  Σ Einsp(Spot < 0) / Σ Einsp (Stufe 2)
  Arbeit         = Einspeisung − Ausfallarbeit
  Die Ausfallarbeit wird abgeregelt: kein Spoterlös, keine Prämie, kein DV-Entgelt.
  Ausweis: Vergütungsausfall = Ausfallarbeit × AW / 100

Marktprämie (Direktvermarktung, § 21 EEG — Pflicht ab 100 kW)
  Erlös = Arbeit × JW / 100 + Arbeit × max(0, AW − JW) / 100 − Arbeit × DV / 100
  JW    = amtlicher Jahresmarktwert des Kalenderjahres (Katalog EEG_JAHRESMARKTWERT_SOLAR, Projekt-Override),
          sonst letzter bekannter Wert × (1 + Marktwertentwicklung)^n
  Stufe 2: Spoterlös = Σ Einsp_h × Spot_h ohne Negativstunden, mit dem Jahresmarktwert fortgeschrieben

Degradation (E2.4)   Faktor_t = (1 − d/100)^(t−1)     Jahr 1 = 1; d = 0 ⇒ exakt 1
  Arbeit_t    = Arbeit × Faktor_t
  Mehrbezug_t = Eigenverbrauch_kWh × (1 − Faktor_t) × Strompreis      als Abzug in derselben Reihe

§ 51a — Kompensation im letzten Vergütungsjahr
  Gutschrift = Ausfallarbeit(Jahr 1) × 0,5 × AW / 100 × Faktor_T       Einmalzahlung, auf ihr Jahr abgezinst

60-%-Kappung   Verlust [kWh] = Σ_h max(0, Einsp_h − 0,6 × kWp)       nur mit Stundenreihe; AUTO nur bei fester EV ohne iMSys

Feste EV       Erlös = Arbeit × EV_fest / 100                         nur ≤ 100 kW

Die PV-Reihe steigt NICHT mit p_E: Der anzulegende Wert ist gesetzlich fixiert (nominal konstant,
DIN EN 17463, 6.3.2). Der Jahresmarktwert schwankt — er gehört in die Szenarienpflege, nicht in eine
Preissteigerungsrate. Steigt er über den AW, entfällt die Prämie ganz, der Spoterlös steigt aber.
```

Belege des Bestands: 8,60 × 0,99^6 → 8,10 ct/kWh ab 08/2026 trifft 16 von 16 BNetzA-Werten exakt ·
300 kWp → 6,04 ct/kWh · Marktprämie Jahr 1 = 13.536,00 € und § 51a = 1.812,00 € in einem anderen
Bestandsprojekt (Formelbeleg, nicht das Beispielprojekt).

## Berechnungserläuterung am Beispielprojekt

Gerechnet mit `PvErloesRechner.Rechne` in Stufe 1 (keine Stundenreihe), T = 20, Jahresmarktwert 2026 = 4,50 ct/kWh
(Annahme), Eigenverbrauch 85.500 kWh und Strompreis 0,288 €/kWh für den Mehrbezug.

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| 1 Degression | IBN 01.08.2026 → 6 Stichtage → 0,99^6 | 0,9415 | Klassenwerte 8,60 → 8,10 · 7,50 → 7,06 · 6,20 → 5,84 |
| 2 Klassenanteile | 10 × 8,10 + 30 × 7,06 + 60 × 5,84 + 200 × 5,84 | 1.811,2 ct·kWp | marginale Klassen, gerundete Tabellenwerte |
| **AW gemischt** | 1.811,2 ÷ 300 = 6,037 | **6,04 ct/kWh** | Belegwert für 300 kWp; feste EV 5,64 (gesperrt) |
| 3 Ausfallarbeit § 51 | 199.500 × 20 % | 39.900 kWh | abgeregelt; vergütete Arbeit 159.600 kWh; Ausweis 39.900 × 6,04 ÷ 100 = 2.409,96 € |
| 4 Spoterlös | 159.600 × 4,50 ÷ 100 | 7.182,00 € | Jahresmarktwert Solar 2026 |
| 5 Marktprämie | 159.600 × (6,04 − 4,50) ÷ 100 | 2.457,84 € | Differenz AW zu Marktwert |
| 6 DV-Entgelt | 159.600 × 0,40 ÷ 100 | − 638,40 € | Kosten des Direktvermarkters |
| 7 Kappung 60 % | Stufe 1: keine Stundenreihe | 0 kWh | im Lauf gemessen, mit AW bewertet |
| **PV-Vergütung Jahr 1** | 7.182,00 + 2.457,84 − 638,40 | **9.001,44 €** | Satz Jahr 1 = 9.001,44 ÷ 199.500 = 4,51 ct/kWh |
| 8 Jahr t | 9.001,44 × 0,995^(t−1) − 85.500 × (1 − 0,995^(t−1)) × 0,288 | Jahr 19: 6.100,40 € | Jahr 20 vor Gutschrift 5.946,77 €; Mehrbezug über 20 Jahre 22.705,69 € |
| 9 § 51a im letzten Jahr | 39.900 × 0,5 × 6,04 ÷ 100 × 0,9092 | 1.095,52 € | Jahr 20 = 5.946,77 + 1.095,52 = 7.042,29 € |
| **Reihe über 20 Jahre** | Σ nominal · Σ ÷ 1,03^t | **150.118 € · 113.800 €** | nominal · Barwert |
| Vermiedener Bezug | 85,5 MWh × 288 €/MWh | 24.624,00 € | **Ausweis** — steckt im Reststrombetrag |

Mit Degradation (`03` kennt sie nicht, sie ist Feld dieses Dialogs) sinkt die Einspeisung bis Jahr 20 auf
199,5 × 0,9092 = 181,4 MWh; die Reihe folgt der Menge, der AW bleibt.

## Befunde und offene Punkte

| Nr. | Punkt | Behandlung |
|---|---|---|
| — | Feste Vergütung über 100 kW nicht wählbar — Option gesperrt mit Hinweis; unzulässige Wahl schaltet auf Marktprämie um | Bestand ✓ |
| — | > 1 MW Ausschreibung: AW-Override nötig | Warnzeile im Bestand ✓ |
| U36 | Die Warnzeile „über 2 MW: Stromsteuer prüfen" kann nie erscheinen — die 1-MW-Prüfung greift zuerst | Mockup Abschnitt 6, Anhang Umsetzungsstand |
| V-G5 | Jahresmarktwert, PPA-/DV-Preise: Best/Worst-Paar je Feld | Szenarioabdeckung (Konzept § 2.11.5), Etappe V-E |
| V-G2 | Degradation wirkt auf die vergütete Arbeit und als Mehrbezug auf den vermiedenen Bezug | Feld dieses Dialogs, Vorbelegung 0,5 beim Anlegen, Bestand NULL = 0 |
| — | Bei Direktvermarktung greift die 60-%-Kappung in Stellung „Automatisch" nicht (AUTO = feste EV ohne iMSys); das Beispiel rechnet sie mit der Stellung „Ja" — ohne Stundenreihe bleibt der Verlust 0 | Mockup Abschnitt 6 zeigt „Ja" mit Statuszeile „aktiv" und 0 kWh in der Vorschau |
| U27 | Aufgeschlüsselte Vorschau (Spoterlös, Marktprämie, DV-Entgelt, Kappung, Summe, Vergütungsausfall, vermiedener Netzbezug) statt der einen Zeile; Jahresmarktwertzeile in der Gruppe Vermarktung — Marktwert-Override und Marktwertentwicklung haben im Dialog kein Feld | Mockup Abschnitt 6, Anhang Umsetzungsstand |
| — | § 51a-Formel ist eine Näherung (Verlängerung der Vergütungsdauer um die Ausfallstunden) | im Bericht als Näherung deklarieren |
