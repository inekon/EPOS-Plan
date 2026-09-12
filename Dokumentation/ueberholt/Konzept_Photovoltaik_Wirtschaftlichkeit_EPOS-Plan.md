# Konzept: Photovoltaik-Wirtschaftlichkeit EPOS-Plan — Eingabedialog und Rechenmodell

**Rev. 1 — 25.08.2026 — zur Abnahme durch Philipp** · Nachtrag 1 (25.08.2026): Abgleich pv@now/PV-Now, siehe Ende des Dokuments — enthält zwei Korrekturen am Rechenmodell (Degressionsmethode, Marktprämien-Systematik)

Auftrag: Es soll ein Dialog für die Eingabe der relevanten Daten zur Photovoltaik-Wirtschaftlichkeitsberechnung
entstehen. Abzubilden sind Eigenstromnutzung, Einspeisung, Vergütungsausfall (negative Spotpreise), flexible
Strompreise sowie anzulegender Wert / Marktwert / Direktvermarktung — nach den aktuellen gesetzlichen
Regelungen (Stand August 2026).

Grundlage sind vier Erhebungen vom 25.08.2026: Kartierung PV-Rechenkern, Kartierung PV-Datenmodell/UI,
Kartierung Wirtschaftlichkeits-/Preismodul (alle mit Datei:Zeile-Belegen am Arbeitsstand) und eine
Web-Recherche zur Rechtslage (BNetzA-Fördersätze 01.08.2026, Clearingstelle EEG|KWKG zum Solarspitzengesetz).
Zahlenwerte der Rechtslage tragen Stand-Datum und Quelle; nicht sicher belegte Punkte sind als solche
gekennzeichnet (Abschnitt 3.7).

**Abgrenzung:** Dieses Konzept ändert nichts an der PV-*Ertragsrechnung* (`SimulationPV`) — deren Schwächen
(Tag-Index-Versatz, harte Konstanten, Rasterverlust) sind dokumentiert und ein eigener Arbeitsstrang. Hier
geht es ausschließlich um die *wirtschaftliche Bewertung* der bereits gerechneten Strommengen. Solarthermie
(`Tab_Solarkollektoren*`, `ID_Type=2`) wird nicht berührt. Mieterstrom (§ 48a EEG) und gemeinschaftliche
Gebäudeversorgung (§ 42b EnWG) sind zunächst außen vor (Entscheidungsfrage F1).

---

## 1. Ziel und Einordnung

EPOS-Plan rechnet die PV-Wirtschaftlichkeit heute mit **einem einzigen, manuell gepflegten Skalar**:
`Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung` [€/kWh], Erlös = PV-Überschuss × Satz
(`WirtschaftlichkeitCtrl.cs:1375-1377`), nominal konstant über den Betrachtungszeitraum. EEG-Systematik
(anzulegender Wert, Größenklassen, Degression, Marktprämie, Direktvermarktung, Vergütungsausfall bei
negativen Preisen) existiert im Code **nicht** — repoweite Suche nach „Marktwert", „Marktprämie",
„Direktvermarkt", „anzulegend", „EEG": null Treffer; beschrieben ist das Modell nur im
`Konzept_Stromspeicher_EPOS-Plan.md:341-342`.

Ziel: Ein stammprojektbezogener Dialog „Photovoltaik-Vergütung" plus zugehöriges Rechenmodell, das

1. die **Vermarktungsform** wählbar macht (feste Einspeisevergütung, Marktprämie/Direktvermarktung,
   sonstige Direktvermarktung/PPA, keine Vergütung),
2. den **anzulegenden Wert** aus Inbetriebnahmedatum, Anlagengröße und Einspeiseart gesetzeskonform
   herleitet (leistungsanteilige Mischrechnung, Degression, Abschläge) — mit Override,
3. den **Vergütungsausfall** nach § 51 EEG (negative Spotpreise) und die Kompensation nach § 51a EEG
   abbildet,
4. die **Eigenstromnutzung** korrekt bewertet (vermiedener Vollbezugspreis, optional stundenscharf über die
   vorhandene Preiszeitreihe = „flexible Strompreise"),
5. sich in die vorhandene Kapitalwertrechnung nach DIN EN 17463 einfügt (`KapitalwertRechner`), ohne
   Bestandsprojekte zu verändern (**Aktiv-Schalter, inaktiv = exakt heutiges Verhalten**).

Alles Nötige existiert als Hausmuster: jahresscharfe Erlösreihen (`KapitalwertRechner.ErloesReihe`,
Vorbild KWKG-Zuschlag/BEHG-Reihe), Gesetzesparameterkatalog (`Tab_Gesetzesparameter` mit
Schlüssel/Klasse/JahrVon/Wert/Status/Quelle samt Pflegemaske), Spotpreis-Zeitreihe inkl. negativer Stunden
(`Tab_Preisreihe`/`Tab_PreisreiheDaten`), Dialogmuster (`Form_Tarifstruktur`,
`Form_WirtschaftlichkeitParameter`) und die Mengenaufbereitung aus Stundenreihen (`StromMatrix`).

---

## 2. Ist-Stand (verifiziert, mit Belegen)

### 2.1 Mengen

| Größe | Quelle | Anmerkung |
|---|---|---|
| PV-Erzeugung (theoretisch, nach WR-Faktor 0,95) | `SimulationPV.Stromproduktion_Theoretisch` (8.760, kWh/h) | speist auch die SpeicherEngine (`StromspeicherSimCtrl.BauePvReihe:878-886`) |
| Eigenverbrauch (Direktverbrauch) | `SimulationPV.Stromproduktion` (`SimulationPV.cs:144,149`) | seit AP2b ohne Speicherentnahme |
| Einspeisung/Überschuss | `SimulationPV.Ueberschuss` (`:147`) | **vor** Speicherladung |
| Netzbezug | `Rest_Strombedarf_viertelstuendlich` nach PV-Abzug und Speicherentladung (`SimulationControl.cs:606, 632`) | |
| Wirtschaftlichkeits-Mengen | `StromMatrix` je Zone aus `ZeitreihenSatz.PV_UEBERSCHUSS` / `PV_GENUTZT` (`StromMatrix.cs:141-167`) | |

### 2.2 Erlöse und Preise

- Flat-Pfad: `Erlös = Photovoltaik.Ueberschuss [MWh] × 1000 × Einspeiseverguetung [€/kWh]`
  (`WirtschaftlichkeitCtrl.cs:1375-1377`); Tarifzonen-Pfad ersetzt das durch `Einsp_W_HT/W_NT/S_HT/S_NT`
  bzw. die Einspeiserolle (`:1424-1430`).
- **Zwei getrennte Vergütungswahrheiten:** `Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung` [€/kWh]
  (liest nur die Wirtschaftlichkeit) und `energy_project_settings.Verguetung_PV` [ct/kWh, Vorgabe 5,0]
  (liest nur die Speichersimulation, `StromPreisCtrl.cs:180`). Beide unabhängig pflegbar → inkonsistente
  Läufe möglich.
- Die Spotpreis-Zeitreihe (Import BNetzA/SMARD-Format, negative Werte zulässig,
  `SpotreihenAufbereitung.cs:43`) wird von der **Wirtschaftlichkeit nicht gelesen** — nur von der
  Speichersimulation und der Preisvorschau. Aufschlagsblock (Netzentgelt/Umlagen/Stromsteuer
  2,05|0,05/Konzession/Vertrieb) existiert an `energy_project_settings` (`SchemaKatalog.cs:433-507`).
- Eigenverbrauch wird **nie als Erlös gebucht** — er wirkt über geringere Reststromkosten
  (`KostenEmissionRechner.cs:122`; Doppelzählungsregel `WirtschaftlichkeitCtrl.cs:1542-1547`). Diese
  Modellregel bleibt bestehen.

### 2.3 Sachfehler, die vor dem Vergütungsmodell zu bereinigen sind

- **V1 — BHKW-Strom wird als PV-Überschuss etikettiert:** Beim BHKW-Abzug bleiben negative Viertelstunden
  im Restbedarf stehen (`SubVectors(..., korrigiert: false)`), `SimulationPV.cs:144` klemmt nicht bei 0 —
  nicht gespeicherter BHKW-Überschuss landet in `Ueberschuss` und damit in der **PV-Einspeisezeile** der
  Wirtschaftlichkeit (`Analyse_B5_SimulationSSP.md:54-58`; Projekt 1018: 24.532 negative Viertelstunden).
  Jede EEG-Vergütungsrechnung auf dieser Menge wäre falsch.
- **V2 — Einspeisemenge bei aktivem Speicher:** `StromMatrix` bewertet `PV_UEBERSCHUSS` (vor
  Speicherladung) als Einspeisung, während der Netzbezug bereits um die Speicherentladung reduziert ist —
  in den Speicher geladene PV-Energie wird heute doppelt begünstigt (als Einspeiseerlös **und** als
  vermiedener Bezug). Zieldefinition: `E_einsp[i] = max(0, Ueberschuss[i] − Speicherladung_aus_PV[i])`;
  die Ladereihe liefert die SpeicherEngine.
- **V3 — kWp existiert nicht als gepflegte Größe:** `Tab_Energieanlagen.PV_Leistung` ist die
  **Modulanzahl** (`Form_Simulation_Config.Karten.cs:957-963`); kWp gibt es nur rechnerisch
  (`Tab_PV.Leistung [W] × Anzahl / 1000`). Die EEG-Größenklassen brauchen kWp verlässlich (Abschnitt 4.2).
- **V4 — Vergütungs-Doppelwahrheit** (siehe 2.2): eine führende Quelle festlegen (Empfehlung: der neue
  Dialog; `StromPreisCtrl` bezieht `v_pv` künftig von dort — Entscheidungsfrage F7).

---

## 3. Rechtsrahmen Stand August 2026

Recherchestand 25.08.2026. Maßgebliche Quelle für Sätze: Bundesnetzagentur, „EEG-Förderung und
-Fördersätze" (Abruf 25.08.2026); Mechanik § 51/§ 51a: Clearingstelle EEG|KWKG, 49. Fachgespräch
(02.07.2025). Alle Werte gehören **in den Gesetzeskatalog, nicht in den Code** (Abschnitt 6).

### 3.1 Anzulegende Werte und feste Einspeisevergütung (Gebäudeanlagen, IBN ab 01.08.2026)

| Leistungsklasse (leistungsanteilig) | AW Überschuss | AW Volleinspeisung | feste EV Überschuss | feste EV Volleinspeisung |
|---|---:|---:|---:|---:|
| bis 10 kW | 8,10 | 12,62 | 7,70 | 12,22 |
| bis 40 kW | 7,06 | 10,64 | 6,66 | 10,24 |
| bis 100 kW | 5,84 | 10,64 | 5,44 | 10,24 |
| bis 400 kW | 5,84 | 8,85 | — | — |
| bis 1.000 kW | 5,84 | 7,63 | — | — |

Alle Werte ct/kWh. Feste Einspeisevergütung nur bis 100 kW installierter Leistung (§ 21 Abs. 1 Nr. 1 EEG);
darüber Marktprämie (Direktvermarktung) oder befristete Ausfallvergütung (AW − 20 %, § 53 Abs. 3).
Feste EV = AW − **0,4 ct/kWh** (§ 53 Abs. 1). Über 1 MW: Ausschreibungssegment, AW = Zuschlagswert
(manuelle Eingabe).

**Rechenmechanik (verifiziert):** Basiswerte Überschuss 8,60 / 7,50 / 6,20 / 6,20 / 6,20 ct/kWh,
Volleinspeisungs-Zuschläge +4,8 / +3,8 / +5,1 / +3,2 / +1,9 (Stand-Fassung IBN 30.07.2022–31.01.2024);
Degression **1 % je Halbjahr** ab 01.02.2024, Stichtage 1.2. und 1.8., je Schritt auf 2 Nachkommastellen
gerundet (§ 49 EEG). 6 Schritte bis 01.08.2026 reproduzieren **alle** BNetzA-Werte exakt
(8,60 → 8,10; 6,20 → 5,84; 13,40 → 12,62 usw.). Die Software rechnet den AW also aus Basiswerten +
Degressionsformel, nicht aus einer je Halbjahr gepflegten Tabelle.

**Leistungsanteilige Mischrechnung** (kein Stufentarif): Beispiel 300 kWp Überschuss/DV, IBN 08/2026:
(10×8,10 + 30×7,06 + 60×5,84 + 200×5,84) / 300 = **6,04 ct/kWh**.

**Solarpaket-I-Vorbehalt:** Die im Gesetzestext enthaltene Erhöhung um +1,5 ct/kWh (> 40 kW) steht unter
EU-beihilferechtlichem Genehmigungsvorbehalt (§ 101 EEG) und ist laut BNetzA-Fußnote (Abruf 25.08.2026)
**nicht** in den geltenden Sätzen enthalten → als vorbereiteter, deaktivierter Katalogwert führen (F8).

**Vergütungsdauer:** 20 Jahre + Rest des Inbetriebnahmejahres (§ 25 Abs. 1 EEG), zzgl. Verlängerung nach
§ 51a (3.3).

### 3.2 § 51 EEG — Vergütungsausfall bei negativen Spotpreisen (Solarspitzengesetz, in Kraft 25.02.2025)

- Für Anlagen mit IBN **ab 25.02.2025**: anzulegender Wert = **0 ab der ersten negativen Viertelstunde**
  (Day-Ahead, Viertelstundenkontrakte, § 3 Nr. 42a EEG). Keine Mindestdauer mehr.
- Gilt für Marktprämie **und** feste Einspeisevergütung (§ 51 senkt den AW, Bemessungsgrundlage beider).
- **Ausnahme:** Anlagen **< 100 kW** sind verschont, bis ein intelligentes Messsystem eingebaut ist
  (wirksam ab Ablauf des Einbau-Kalenderjahres); Anlagen < 2 kW bis zur BNetzA-Festlegung.
- Bestandsanlagen (IBN 01.01.2023–24.02.2025): alte 4/3/2/1-Stunden-Regel, Ausnahme bis 400 kW; ältere
  Staffeln analog (F9: Empfehlung — nur Neuanlagenregel exakt abbilden, EPOS-Plan plant Neuanlagen).
- Häufigkeit (amtlich, BNetzA/SMARD): 2023 **301 h**, 2024 **457 h**, 2025 **573 h** mit negativen
  Day-Ahead-Preisen; H1/2026 je nach Zählweise 291–298 h. Seit 10/2025 handelt die EPEX Day-Ahead
  viertelstündlich — Statistiken davor/danach nicht direkt vergleichbar.
- **Quantifizierung des Verlusts (LfL Bayern, Solar-012, Stand 26.01.2026, SMARD-Einspeiseprofil):**
  Anteil der Solarstromerzeugung in Negativpreiszeiten 2023: 6,7 % → 2024: 14,5 % → **2025: 23,4 %**.
  Für die Neuanlagen-Kohorte (jede Viertelstunde zählt) entspricht das dem Marktprämien-/EV-Ausfall vor
  § 51a-Kompensation — die Größenordnung des Stufe-1-Vorschlagswerts (4.4).

### 3.3 § 51a EEG — Kompensation durch Verlängerung

Ausgefallene **Viertelstunden werden gezählt, für Solar durch 2 geteilt** (Faktor 0,5) und als
„Volllastviertelstunden"-Kontingent **an das Ende des Vergütungszeitraums angehängt**, dort monatsweise
gegen eine gesetzlich fixierte Ertragstabelle verrechnet (Jan 87, Feb 189, Mrz 340, Apr 442, Mai 490,
Jun 508, Jul 498, Aug 453, Sep 371, Okt 231, Nov 118, Dez 73; Summe 3.800 Viertelstunden = 950
Volllaststunden/a), Aufrundung auf das Monatsende. Netto verbleibt also **rund die Hälfte** des Ausfalls
als echter Verlust. Gilt nur für Solaranlagen mit IBN ab 25.02.2025.

### 3.4 Direktvermarktung / Marktprämie (§§ 20, 21a, 23a EEG)

- **Marktprämie = max(0, anzulegender Wert − Monatsmarktwert Solar)** (Anlage 1 zu § 23a EEG; nie
  negativ — übersteigt der Marktwert den AW, behält der Betreiber den Mehrerlös). Betreiber erhält
  Markterlös + Marktprämie, abzüglich Direktvermarktungsentgelt.
- **Monatsmarktwert Solar (MW):** erzeugungsgewichteter Monatsmittelwert der Viertelstunden-Spotpreise
  (Anlage 1 Nr. 5.2 EEG); Veröffentlichung durch die ÜNB auf netztransparenz.de bis zum 10. Werktag des
  Folgemonats — Datenquelle der Software. Recherchiert liegen die vollständigen Monatsreihen 2024–07/2026
  vor (Saatdaten für 6.3): **Jahresmarktwert 2024: 4,624 / 2025: 4,508 ct/kWh**; Spannweite 2025/26
  ca. 1,3–11,5 ct/kWh. **Verwechslungswarnung:** netztransparenz.de weist zusätzlich einen
  „Jahresmittelwert nach § 33 EEG 2012" aus (2024: 5,858) — für die Marktprämie ist ausschließlich der
  MW/JW nach Anlage 1 maßgeblich. Ein Jahresmittel unterschätzt das Profilrisiko systematisch →
  Monatsauflösung (Kannibalisierung: MW Solar lag 2025 **50 %** unter dem Marktdurchschnitt, 2023 −24 %).
- **Direktvermarktungsentgelt:** kein amtlicher Wert, frei verhandelt. BMWK-Monitoringbericht 2024:
  Kurzfristvermarktungskosten Solar ≈ 0,5 ct/kWh; Marktpraxis-Dienstleistungsentgelte 0,1–0,4 ct/kWh,
  teils zzgl. Grundgebühr → Eingabeparameter, Vorbelegung 0,3 ct/kWh. Bei > 25 kW kommen Technikpflichten
  hinzu (§ 10b Fernsteuerbarkeit, § 21b Abs. 3 Viertelstundenmessung) — als Betriebskostenpositionen
  erfassbar, nicht im Entgelt versteckt.
- **Sonstige Direktvermarktung (§ 21a):** freier Strompreis (PPA), keine EEG-Zahlung, dafür
  Herkunftsnachweise vermarktbar; § 51 greift mangels EEG-Zahlungsanspruch nicht. Wechsel der
  Veräußerungsform monatlich zum Monatsersten (§ 21b Abs. 1 S. 2), Mitteilung mit gut einem Monat Vorlauf
  (§ 21c Abs. 1); der Direktvermarkter selbst ist jederzeit wechselbar (§ 21b Abs. 4).
- **Unentgeltliche Abnahme** (§ 21 Abs. 1 Nr. 2, < 200 kW, aus dem Solarpaket I): keine Zahlung —
  Grundlage für reine Eigenverbrauchs-/Nulleinspeisungs-Modelle. **Fallstrick § 21c Abs. 1 EEG:** Anlagen
  < 200 kW ohne ausdrückliche Zuordnung landen **automatisch** in der unentgeltlichen Abnahme (0 ct) —
  der Dialog weist darauf hin, dass die gewählte Vermarktungsform auch tatsächlich angemeldet werden muss.
  Sperrfrist: nach unentgeltlicher Abnahme 24 Monate keine Ausfallvergütung (§ 21b Abs. 1 S. 4).

### 3.5 60-%-Wirkleistungsbegrenzung (§ 9 Abs. 2 EEG)

Nur für Neuanlagen (IBN ab 25.02.2025) **mit fester Einspeisevergütung oder Mieterstromzuschlag** und ohne
iMSys; begrenzt die **Einspeisung am Verknüpfungspunkt** auf 60 % der installierten Leistung — Eigenverbrauch
und Speicherladung dahinter bleiben frei. Entfällt nach iMSys-Einbau + erfolgreicher Steuerbarkeitstestung.
**Direktvermarktung ist nicht betroffen.** Aus der Überschuss-Viertelstundenreihe exakt berechenbar.

### 3.6 Eigenverbrauch — Abgaben

- **Stromsteuer:** Befreiung für Eigenverbrauch aus EE-Anlagen bis 2 MW im räumlichen Zusammenhang
  (§ 9 Abs. 1 Nr. 3 StromStG). Seit 01.01.2026 (3. EnergieStG/StromStG-ÄndG): standort- und
  betreiberbezogene Anlagendefinition, die frühere standortübergreifende Verklammerung ist entfallen —
  verteilte Dachanlagen werden für die 2-MW-Grenze nicht mehr zusammengerechnet.
- **EEG-Umlage:** seit 07/2022 null, mit dem EnFG abgeschafft. Netzentgelte/Umlagen/Konzessionsabgabe
  fallen auf echten Eigenverbrauch hinter dem Netzverknüpfungspunkt nicht an.
- Konsequenz fürs Modell: Der Nutzen der Eigenstromnutzung ist der **vermiedene Vollbezugspreis**
  (Arbeitspreis + aktive Aufschläge). Eine eigene Steuer-Rechenposition ist nicht nötig; ein Hinweistext
  im Dialog genügt (bei Anlagen > 2 MW oder Drittbelieferung stimmt die Annahme nicht → Warnhinweis).

### 3.7 Ausblick und Unsicherheiten

- **EEG-Novelle (Kabinettsbeschluss 29.07.2026, nicht in Kraft; Bundestagsberatung nach der Sommerpause):**
  laut Entwurf entfällt für Neuanlagen < 25 kW ab 2027 der Marktprämienanspruch (stattdessen
  Direktvermarktungsbonus 1,5 ct/kWh für 4 Jahre); Übergangszahlung 36 Monate (1 ct/kWh unter den
  bisherigen Sätzen) mit jährlich schrumpfendem Kreis (2027: < 50 kW, 2028: < 25 kW, 2029: < 7 kW);
  50-%-Einspeisebegrenzung für mittlere Dachanlagen. Treiber ist die EU-Strommarktreform
  (Art. 19d VO (EU) 2024/1747: Direktförderung neuer Anlagen ab 17.07.2027 als zweiseitige
  Differenzverträge). Eine „vereinfachte Direktvermarktung" über den Netzbetreiber existiert Stand heute
  **nicht** als geltendes Recht. Konsequenz: Vergütungslogik strikt **datengetrieben und
  stichtagsabhängig** bauen (Katalog), nie fest verdrahten.
- Nicht primärquellenfest (im Katalog mit Status VORLAEUFIG bzw. bei Umsetzung nachprüfen):
  Monatsmarktwerte 2024–2026 vollständig (netztransparenz.de), Negativpreisstatistik amtlich,
  Direktvermarktungsentgelte (ohnehin Eingabeparameter), Status EU-Genehmigung Solarpaket I,
  Stromsteuersatz 2026, § 21b-Wechselfristen.

---

## 4. Rechenmodell

### 4.1 Vermarktungsformen (Auswahl im Dialog)

| Form | Erlös je eingespeister kWh | Verfügbarkeit |
|---|---|---|
| **(a) Feste Einspeisevergütung** | `EV = AW_mix − 0,4 ct/kWh`, konstant | nur kWp ≤ 100 |
| **(b) Direktvermarktung / Marktprämie** | `MW_solar(Monat) + max(0, AW_mix − MW_solar(Monat)) − DV-Entgelt` | Standard > 100 kW, wählbar darunter |
| **(c) Sonstige Direktvermarktung / PPA** | fester Preis [ct/kWh] **oder** Spotreihe ± Aufschlag | immer |
| **(d) Keine Vergütung** (unentgeltliche Abnahme / Nulleinspeisung) | 0 | immer |

Zusätzlich: Einspeiseart **Überschuss- oder Volleinspeisung** (bestimmt die AW-Spalte; bei Volleinspeisung
ist die Eigenverbrauchsmenge 0 und der gesamte Ertrag Einspeisung — Konsistenzprüfung gegen die
Simulation), Inbetriebnahmedatum (bestimmt Degressionsstand und § 51-Regime).

### 4.2 Anzulegender Wert `AW_mix`

```
kWp        = Σ über PV-Anlagen des Projekts (Tab_PV.Leistung [W] × Modulanzahl) / 1000   (Override möglich)
AW_Klasse  = Basiswert(Klasse, Einspeiseart) × Degression(IBN-Stichtag)                  (je Schritt gerundet)
AW_mix     = Σ (Klassenanteil_kW × AW_Klasse) / kWp                                      (leistungsanteilig)
```

Anzeige im Dialog mit Herleitung (Klassenzerlegung, Degressionsstand, Quelle/Stand aus dem Katalog) und
**Override-Feld** (z. B. Ausschreibungszuschlag > 1 MW oder künftige Rechtsänderung). Der AW ist über die
Laufzeit **fest** (Inbetriebnahmeprinzip) — die Degression betrifft nur den Stichtag, nicht die
Betriebsjahre.

### 4.3 Mengen

Aus der Simulation (nach Bereinigung V1/V2), viertelstündlich:

```
E_eigen[i]  = Direktverbrauch                (SimulationPV.Stromproduktion, expandiert)
E_einsp[i]  = max(0, Ueberschuss[i] − Speicherladung_aus_PV[i])
E_kapp[i]   = max(0, P_einsp[i] − 0,6 × kWp) × Δt        (nur wenn Kappung greift, 3.5)
```

Bei Volleinspeisung: `E_einsp = Erzeugung`, `E_eigen = 0`.

### 4.4 Einspeiseerlös — zwei Rechentiefen

**Stufe 1 — ohne Spotreihe (immer möglich):** Monatssummen der Einspeisung × Sätze.
Marktprämienfall: je Monat `E_einsp_m × (MW_solar_m + max(0, AW_mix − MW_solar_m)) − DV_Entgelt × E_einsp_m`;
fehlen Monatsmarktwerte, ersatzweise Jahresmarktwert (Szenarioparameter, Fortschreibung
„Marktwertentwicklung %/a"). Vergütungsausfall § 51 pauschal: Parameter **„Ausfallanteil der
Einspeisearbeit [%]"** (editierbar; Vorbelegung **20 %** — Anhalt: LfL-Messung 23,4 % der
Solarerzeugung in Negativpreiszeiten 2025, Tendenz steigend; wenn eine Spotreihe vorliegt, zeigt der
Dialog den daraus gemessenen Ist-Anteil an).

**Stufe 2 — mit Spotreihe (Hausbestand `Tab_Preisreihe`):** intervallscharf,
`v[i]` je Viertelstunde wie im Stromspeicher-Konzept §4.3:

```
Negativintervall (Spot[i] < 0) und § 51 greift:
    AW → 0; Standardannahme Direktvermarktung: Abregelung → E_einsp[i] entfällt (Ausfallmenge zählen)
sonst:
    (a) v[i] = EV                       (b) v[i] = Spot[i] + MP_Monat − DV_Entgelt
```

§ 51 greift, wenn: IBN ≥ 25.02.2025 **und** (kWp ≥ 100 **oder** iMSys eingebaut, wirksam ab Folgejahr des
Einbaus). Die importierten Spotreihen sind Stundenwerte (8.760) — die § 51-Zählung erfolgt dann
stundenbasiert als dokumentierte Näherung; Viertelstundenreihen (35.040) werden bereits akzeptiert
(`StromPreisCtrl.cs:279-284`).

**§ 51a-Kompensation (optional, Standard an):** Jahresausfallmenge → Viertelstundenzahl → × 0,5 →
Gutschrift `AW_mix × kompensierte Menge` als Erlösposition im letzten Betrachtungsjahr (vereinfachte
Barwert-Abbildung der Laufzeitverlängerung; exakte Monatstabellen-Mechanik ist Katalogwissen und wird im
Protokoll ausgewiesen). Ausweis immer getrennt: „Vergütungsausfall [kWh | €]" und „davon kompensiert".

### 4.5 Eigenstromnutzung und flexible Strompreise

Die Hausregel bleibt: **kein Erlösansatz für Eigenverbrauch** (Doppelzählungsverbot,
`WirtschaftlichkeitCtrl.cs:1542-1547`) — der Nutzen entsteht als geringerer Netzbezug im Vergleich zum
Stammprojekt. Neu sind zwei Dinge:

1. **Stundenscharfe Bezugsbewertung (Option „Strompreis aus Preiszeitreihe"):** Die Wirtschaftlichkeit
   liest erstmals die vorhandene Preiszeitreihe (Spot oder Kostenprofil + Aufschlagsblock, exakt der
   Rechenweg der Speichersimulation `StromPreisCtrl.BaueEnergiereihe`) und bewertet den Netzbezug
   `Σ Netzbezug[i] × p_bezug[i]` statt Flat-Arbeitspreis. Damit wird der Wert der PV-Eigennutzung
   zeitrichtig (PV senkt Bezug mittags — bei Spotpreisen der billigeren Stunden!). Das ist bewusst
   **dieselbe Preisquelle wie beim Stromspeicher** — eine Preiswahrheit je Projekt (F10).
2. **Informative Kennzahl „Vermiedener Strombezug durch PV"** = `Σ E_eigen[i] × p_bezug[i]` — reine
   Ausweisgröße (Kennzahlzeile), fließt nicht zusätzlich in den Kapitalwert ein.

Stromsteuer-Hinweis im Dialog (statisch): „Eigenverbrauch aus Anlagen ≤ 2 MW im räumlichen Zusammenhang ist
stromsteuerfrei (§ 9 StromStG); bei Lieferung an Dritte gelten andere Regeln." Warnung, wenn kWp > 2.000.

### 4.6 Kapitalwert-Einbettung

- Einspeiseerlös PV wird zur **jahresscharfen benannten Erlösreihe** (`ErloesReihe`, neuer Schlüssel
  `PV_VERGUETUNG`) statt des heutigen Skalars: Jahr 1…T aus 4.4; nach Ablauf der Vergütungsdauer
  (20 Jahre + Rest ab IBN; bei T > Restdauer) fällt der Erlös auf den reinen Marktwert (Fall b/c) bzw. 0
  (Fall a) zurück. § 51a-Gutschrift gemäß 4.4.
- Szenarien Worst/Erwartet/Best erhalten den Parameter **Marktwertentwicklung [%/a]** (analog
  Preissteigerung Energie); der AW bleibt szenariofest (gesetzlich fixiert).
- Inaktiver Dialog (`Aktiv = false`) ⇒ Rechenweg unverändert: Flat-`Einspeiseverguetung` wie heute.
  **Ergebnisneutralität ist Abnahmekriterium** (Referenzlauf-Vergleich).

---

## 5. Zusammenspiel mit Speicher und Tarifstruktur

- **Speicher:** Die SpeicherEngine bewertet Ladung aus PV bereits mit dem Opportunitätswert `v_pv[i]`
  (Stromspeicher-Konzept §4.3). Nach V4/F7 liefert der PV-Dialog diesen Satz — `StromPreisCtrl` liest
  künftig `v_pv` aus dem PV-Vergütungsmodell (bei Stufe 1 als Konstante `AW_mix` bzw. EV, bei Stufe 2 als
  Reihe). `energy_project_settings.Verguetung_PV` wird zur Anzeige-/Altlast und beim Speichern des Dialogs
  mitgeführt (Übergang), langfristig stillgelegt.
- **Tarifstruktur (`Tab_ProjektTarif`):** Ist der PV-Dialog aktiv, ersetzt sein Erlösmodell die
  PV-Einspeisezonen (`Einsp_*` wirken dann nur noch für KWK); umgekehrt bleibt bei inaktivem PV-Dialog
  alles beim Bestand. Gleiches Ersetzungsmuster wie heute Tarif ↔ Flat (`WirtschaftlichkeitCtrl.cs:1424-1430`).
- **BHKW:** unverändert (KWKG-Strang); durch V1 wird die Trennung PV-/BHKW-Einspeisung erstmals sauber.

---

## 6. Datenmodell

### 6.1 Neue Projekttabelle `Tab_ProjektPhotovoltaik` (eine Zeile je Stammprojekt)

Muster `Tab_ProjektTarif` (Aktiv-Schalter, stammbezogen). Anlage über **Migrationsschritt 38+**
(ZIEL_VERSION heute 37; Nummer bei Umsetzung neu prüfen — linearer Zähler, Hausregel).

| Spalte | Typ | Bedeutung / Vorbelegung |
|---|---|---|
| `ID`, `ID_Projekt` (UNIQUE) | LONG | MAX+1-Muster |
| `Aktiv` | YESNO | false = Bestandsverhalten |
| `Vermarktungsform` | TEXT(30) | `PV_EV` / `PV_MARKTPRAEMIE` / `PV_SONSTIGE_DV` / `PV_KEINE` (DbWerte, eingefroren) |
| `Einspeiseart` | TEXT(20) | `PV_UEBERSCHUSS` / `PV_VOLL` |
| `Inbetriebnahme` | DATETIME | Pflicht; Default 1.1. des Simulationsjahres |
| `KwpOverride` | DOUBLE, nullable | NULL = rechnerisch aus Anlagen |
| `AwOverride` | DOUBLE, nullable | ct/kWh; NULL = Katalogherleitung |
| `DvEntgelt` | DOUBLE | ct/kWh, Default 0,3 (frei verhandelter Marktwert, Bandbreite ca. 0,1–0,6; kein Gesetzeswert) |
| `PpaPreis` | DOUBLE, nullable | ct/kWh (Form c); alternativ `PpaSpotAufschlag` |
| `PpaSpotAufschlag` | DOUBLE, nullable | ct/kWh auf Spot (Form c mit Reihe) |
| `Par51_Anwenden` | TEXT(20) | `AUTO` / `JA` / `NEIN` (AUTO = Regel aus 4.4) |
| `IMSys_Einbaujahr` | LONG, nullable | für AUTO-Regel und Kappungsende |
| `AusfallanteilProzent` | DOUBLE, nullable | Stufe-1-Pauschale; NULL = Vorschlag 5 % |
| `Par51a_Kompensieren` | YESNO | Default true |
| `Kappung60_Anwenden` | TEXT(20) | `AUTO` / `JA` / `NEIN` |
| `MarktwertJahresmittel` | DOUBLE, nullable | ct/kWh, Rückfall wenn keine Monatswerte |
| `MarktwertEntwicklung` | DOUBLE | %/a, Default 0 |
| `BezugAusPreisreihe` | YESNO | Default false (Option 4.5.1) |
| `GeaendertAm` | DATETIME | |

NULL heißt durchgängig „nicht gepflegt / Rückfall", nie 0 (Hausregel `GesetzKatalog.cs:102-106`).

### 6.2 Gesetzeskatalog — neue Klasse `EEG` in `Tab_Gesetzesparameter`

Ablage nach Bestandsmuster (Schlüssel/Klasse/JahrVon/Wert/Einheit/Status/Quelle, generationsweise
Nachsaat, Pflegemaske `Form_Gesetzesparameter`):

| Schlüssel (ASCII) | Wert | Einheit | Status |
|---|---:|---|---|
| `EEG_AW_BASIS_UE_10/40/100/400/1000` | 8,60 / 7,50 / 6,20 / 6,20 / 6,20 | ct/kWh | GESICHERT |
| `EEG_AW_VOLL_ZUSCHLAG_10/40/100/400/1000` | 4,8 / 3,8 / 5,1 / 3,2 / 1,9 | ct/kWh | GESICHERT |
| `EEG_DEGRESSION_HALBJAHR` | 1,0 | % | GESICHERT |
| `EEG_DEGRESSION_BEGINN` | 2024 (01.02.) | Jahr | GESICHERT |
| `EEG_EV_ABSCHLAG` | 0,4 | ct/kWh | GESICHERT |
| `EEG_AUSFALLVERGUETUNG_ABSCHLAG` | 20 | % | GESICHERT |
| `EEG_EV_GRENZE_KW` | 100 | kW | GESICHERT |
| `EEG_AUSSCHREIBUNG_GRENZE_KW` | 1000 | kW | GESICHERT |
| `EEG_51_GRENZE_KW` | 100 | kW | GESICHERT |
| `EEG_51_IBN_STICHTAG` | 2025 (25.02.) | Jahr | GESICHERT |
| `EEG_51A_FAKTOR_SOLAR` | 0,5 | ohne | GESICHERT |
| `EEG_51A_VLVST_MONAT_1…12` | 87…73 (Tabelle 3.3) | h | GESICHERT |
| `EEG_KAPPUNG_PROZENT` | 60 | % | GESICHERT |
| `EEG_VERGUETUNGSDAUER` | 20 | Jahr | GESICHERT |
| `EEG_SOLARPAKET_AUFSCHLAG` | 1,5 | ct/kWh | **VORLAEUFIG** (nicht anwenden bis EU-Genehmigung, F8) |

Der `EegSatzRechner` (neu, Muster `KwkgSatzRechner`: Katalog als Delegat, UI-frei, testbar) kapselt
Klassenzerlegung, Degression (iterativ mit Rundung je Schritt) und Abschläge. **Unit-Tests verifizieren
die komplette BNetzA-Tabelle 3.1** (alle 16 Werte) — das ist das Abnahmekriterium der Katalogsaat.

### 6.3 Monatsmarktwerte Solar

Empfehlung (F2): als **Stamm-Preisreihe** im Bestandsmodell — `Tab_Preisreihe` mit neuer Auflösung
`MONAT` (12 Werte je Jahr, `ID_Projekt = NULL`, Bezeichner „Marktwert Solar", Einheit ct/kWh), Pflege
manuell oder CSV-Import von netztransparenz.de (eigener kleiner Importer nach Spotimport-Muster).
Rückfallkette: Monatsreihe des Jahres → `MarktwertJahresmittel` aus dem Dialog → Hinweis „nicht gepflegt".
**Saatdaten:** Die recherchierten Monatsreihen 2024, 2025 und 01–07/2026 werden als Stammreihen
mitgeliefert (2024/2025: 9 von 24 Werten unabhängig gegengeprüft; vor Freigabe komplett gegen den
CSV-Download von netztransparenz.de verifizieren — Prüfschritt in Etappe P3).

### 6.4 Ergebnis-Persistenz

`Tab_ErgebnisWirtschaftlichkeit` additiv über `SpalteSicher` (Bestandsmuster, `WirtschaftlichkeitCtrl.cs:299-418`):
`PvVerguetungsform`, `PvAnzulegenderWert` (ct/kWh, Mix), `PvMarktpraemie` (€), `PvVerguetungsausfallKwh`,
`PvVerguetungsausfall` (€), `PvKompensation51a` (€), `PvKappungsverlustKwh`, `PvVermiedenerBezug` (€,
informativ). Kennzahlzeilen nach dem `Irgendein(...)`-Sichtbarkeitsmuster (`WirtschaftlichkeitZeilen.cs`).

---

## 7. Der Dialog `Form_PhotovoltaikVerguetung`

**Andockpunkt:** Knopf **„Photovoltaik…"** in der Knopfleiste von `UcWirtschaftlichkeit` (neben
„Tarifstruktur…", „Parameter…"; `UcWirtschaftlichkeit.cs:138-142`), sichtbar nur bei
`_erzeuger.Photovoltaik` (`WirtschaftlichkeitCtrl.ErzeugerDerGruppe`). Muster `Form_Tarifstruktur`:
stammbezogen, `Gespeichert`-Flag ⇒ Neuberechnung.

**Hausregeln:** komplett programmatische UI (kein Designer), `MyResource` de+en (Präfix `PVW_*`),
Drei-Schichten-Regel (DbWerte-Persistenzwert ≠ ASCII-Schlüssel ≠ Anzeigetext), `HilfeKontext`-Eintrag,
Zahlen über `Program.ZahlParsen/ZahlPruefen/ZahlFaerben`, Layout-Helfer aus
`Form_WirtschaftlichkeitParameter` (Gruppe/Zeile/AuswahlZeile/SchalterZeile), Höhendeckelung + AutoScroll.

**Aufbau (Gruppen):**

1. **Anlage** — kWp rechnerisch (aus Modulkatalog × Anzahl, mit Klassenzerlegung 10/40/100/400/1000) +
   Override; Inbetriebnahmedatum; Einspeiseart Überschuss/Voll. Warnhinweise: kWp > 1.000 (Ausschreibung —
   AW-Override nötig), kWp > 2.000 (Stromsteuer), Widerspruch Volleinspeisung ↔ vorhandener Eigenverbrauch.
2. **Vermarktung** — vier Optionen (4.1) als Radiogruppe; feste EV gesperrt bei kWp > 100 (Tooltip nennt
   den Grund); Felder DV-Entgelt bzw. PPA-Preis/Spot-Aufschlag je nach Wahl aktiviert
   (Enabled-Umschaltung, nicht Ausblenden — Befund `Form_Tarifstruktur.cs:29-32`). Hinweiszeile zur
   § 21c-Automatik: ohne aktive Anmeldung beim Netzbetreiber gilt für Anlagen < 200 kW die unentgeltliche
   Abnahme (0 ct) — die gewählte Form muss real zugeordnet werden.
3. **Anzulegender Wert** — hergeleiteter `AW_mix` mit Herkunftszeile (Degressionsstand, Katalog-Quelle,
   Stand-Datum) und Override; Anzeige der resultierenden festen EV (AW − 0,4).
4. **Vergütungsausfall (§ 51/§ 51a)** — Anwenden AUTO/JA/NEIN mit erklärender Statuszeile („greift nicht:
   Anlage < 100 kW ohne iMSys"), iMSys-Einbaujahr, Kompensation § 51a an/aus, Stufe-1-Pauschale
   [%-Ausfallarbeit] mit Messwert-Anzeige aus der Spotreihe, sofern vorhanden.
5. **Strompreis/Bezugsbewertung** — Schalter „Netzbezug stundenscharf aus Preiszeitreihe bewerten" +
   Anzeige der Projektpreisquelle (Spotreihe/Kostenprofil/Fixpreis, Absprung in den Spotimport nach dem
   Muster `Form_Kosten`-Karten); statischer Stromsteuer-Hinweis (4.5).
6. **60-%-Begrenzung** — AUTO/JA/NEIN mit Statuszeile (Bedingungen 3.5) und, wenn aktiv, gemessenem
   Kappungsverlust aus der Zeitreihe.
7. **Vorschau** — Live-Block nach dem Muster der Speicher-Preisvorschau (derselbe Rechenweg wie die
   Simulation, keine Zweitrechnung): mittlerer Vergütungssatz, Erlös p. a., Ausfall p. a., vermiedener
   Bezug p. a.

---

## 8. Etappenplan

| Etappe | Inhalt | Abnahme |
|---|---|---|
| **P1** | Bereinigungen V1 (BHKW-Etikettierung: Klemme/getrennte Reihen), V2 (Einspeisemenge nach Speicherladung), V3 (kWp-Herleitung als geteilte Hilfsfunktion). **Umgesetzt 26.08.2026** (9ace8af): SimulationPV klemmt + `BhkwUeberschuss`-Reihe (Zeitreihe `BHKW_Ueberschuss`, Protokollhinweis); Einspeisung = max(0, Überschuss − `LadungAcKwh`) in ErgebnisModel und Zeitreihe; `PhotovoltaikCtrl.KwpDesProjekts` | ERFÜLLT per Erhaltungsbeweis (alt ≡ neu + BhkwUeberschuss je Stunde, Abweichung 0 ⇒ Nicht-BHKW byte-identisch; die DB-Kopie führt kein PV+BHKW-Projekt — Positivfall synthetisch belegt, Realabnahme 1018 auf der Produktiv-DB in P6); Realbeleg V2 an 1007: 1,773 → 0,759 MWh nach 1,014 MWh Ladung; Smoke pv1 11/11 |
| **P2** | `EegSatzRechner` + Katalogklasse EEG (Saat, Generation), Unit-Tests gegen BNetzA-Tabelle. **Umgesetzt 26.08.2026** (19d66ce): Generation 5 (33 Schlüssel), Degression UNRUNDET je Klasse (N1), anzuwendender Klassenwert gerundet, Mix auf den Tabellenwerten | ERFÜLLT: Smoke pv2 37/37 — alle 16 BNetzA-Werte 08/2026 exakt, unrundete Werte 02–07/2026, Mischsätze 6,04/6,43/7,41, Saat idempotent 0/0 |
| **P3** | Migration (Tab_ProjektPhotovoltaik), Monatsmarktwert-Preisreihe (Auflösung MONAT) + Import, Controller. **Umgesetzt 26.08.2026** (310c48c): Schritt 41 (Tabelle + Saat 2024/2025/Jan–Jul 2026), `ProjektPhotovoltaikCtrl` (Vorbelegungen N5 0,40 / F5 20 %), Katalog-Generation 6: Jahresmarktwerte 4,624/4,508 (N2); CSV-Importer folgt mit dem P6-Prüfschritt (Reihen gegen den netztransparenz-Download verifizieren) | ERFÜLLT: Smoke pv3 20/20 (Idempotenz, Reihenwerte, Rückfallketten ohne stille Vorjahresübernahme, Upsert) |
| **P4** | Erlösbildung Stufe 1+2, § 51/§ 51a, Kappung, `ErloesReihe PV_VERGUETUNG`, V4-Umschluss (`v_pv` für Speicher). **Umgesetzt 26.08.2026** (c0f6012): `PvErloesRechner` (jahresscharf, N2-Jahresmarktwert mit Fortschreibung, Dauerende 20 a + IBN-Monate, § 51 AUTO je Jahr, Stufe-2-Messung aus der Spotreihe, § 51a-Gutschrift im letzten Vergütungsjahr, Kappung aus der Stundenreihe); Einbettung NACH allen drei Erlöspfaden (Flat/Rollen/Tarif) über `e.ErloesPv`; `v_pv` aus `VpvCtKwh` | ERFÜLLT: Smoke pv4 24/24 gegen Handrechnung (13.536,00 €/a; § 51a 1.812,00 im Jahr 20; iMSys-Staffel; gemessene 50 %; Kappung 200 kWh); Neutralität: ohne Zeile/inaktiv läuft exakt der Bestandsweg; Voll-Referenzvergleich = P6 (INEKON) |
| **P5** | Dialog + Kennzahlen/Ergebnisspalten/Bericht (Word/Excel-Bausteine). **Teilumsetzung 26.08.2026**: `Form_PhotovoltaikVerguetung` (7 Gruppen inkl. Live-Herleitung, N3/N4-Sperrlogik, § 51-/Kappungs-Status, Vorschau aus dem EINEN Rechenweg; Designer-fähig statt programmatisch — bewusste Abweichung nach der jüngeren FK1/Ä6-Entscheidung des Kostendialoge-Konzepts), Andockknopf „Photovoltaik…“ in `UcWirtschaftlichkeit` (sichtbar nur bei PV), 52 PVW_*-Schlüssel de+en, HilfeKontext. *Nachtrag Ä18 (26.08.2026, Kostendialoge-Konzept):* Fußknopf „Einspeise-Tarif…“ öffnet die Tarifstruktur in der PV-SICHT (Einspeisepreise beider Modelle) — der PV-Anteil der Tarifstruktur liegt damit unter der PV-Wirtschaftlichkeit. **Restumfang mit P6 umgesetzt 26.08.2026** (Ergebnisspalten 6.4, Kennzahlzeile N.3 im Dialog, PV-Ausweisblock in `WirtschaftlichkeitZeilen` = Reiter/Word/Excel aus EINER Definition) | Smoke pv5 14/14 (Vorbelegung, Sperrlogik, Statuszeilen, Roundtrip, Knopf); Layout-Sweep 120 Formulare 0 Befunde; **offen: Sichtprüfung Philipp** |
| **P6** | Gesamtabnahme am Programm: PV-Projekt mit Speicher, alle 4 Vermarktungsformen, Protokollhinweise. **Umgesetzt 26.08.2026** (Protokoll `PV_P6_Protokoll.md`): § 6.4-Ergebnisspalten (8 Pv*-Spalten via SpalteSicher, Roundtrip über `LadeErgebnisse`) + PV-Ausweisblock in `WirtschaftlichkeitZeilen` (eine Wahrheit für Reiter/Word/Excel, ohne aktiven Dialog KEINE Zeile); Kennzahlzeile N.3 im Dialog (`PvKennzahlenRechner`: LCOE₀ + LCOE diskontiert, EV-Quote+Autarkie stets als Paar [mit Speicher aus der Speicherrechnung], Vorteil/a; fehlende Grundlagen werden benannt); Marktwert-CSV-Importer (netztransparenz-Format + Jahr;Monat;Wert, Ersetzen je Jahr) + externe Saat-Verifikation (6 Monatswerte + beide Jahresmarktwerte 4,624/4,508 bestätigt); INEKON-Referenz N.4 ERFÜLLT: − 0,76 %/− 0,47 % auf den Kapitalwert (± 1 % nach Angleich der Zeitkonvention; Rohwerte − 3,44 %/− 2,76 % = dokumentierte Konventionsdifferenz — EPOS zinst nachschüssig [Bestand], pv@now monatlich); V1-Realabnahme 1018 auf Kopie der Produktiv-DB (4/4: keine falsch etikettierte PV-Reihe mehr) | pv6-Smoke 28/28 + pv6real 4/4; Regression pv1–pv5 und kd2–kd6 grün, Layout-Sweep 120 Formulare 0 Befunde; offen: Sichtprüfung Philipp |

Jede Etappe eigener Commit-Block mit Protokoll-MD (Hausmuster), Build via VS-MSBuild x64, kein Push
(Sync-Automatik Philipps).

---

## 9. Entscheidungsfragen an Philipp

| Nr. | Frage | Empfehlung |
|---|---|---|
| **F1** | Mieterstrom (§ 48a) und gemeinschaftliche Gebäudeversorgung (§ 42b EnWG) in Rev. 2 aufnehmen oder außen vor? | außen vor; Katalogschlüssel reservieren |
| **F2** | Monatsmarktwerte als Stamm-Preisreihe (Auflösung MONAT) oder als Katalogschlüssel je Monat? | Preisreihe (Bestandsinfrastruktur, importierbar) |
| **F3** | § 51a-Kompensation vereinfacht als Gutschrift im letzten Betrachtungsjahr (Faktor 0,5) statt echter Laufzeitverlängerung? | ja, mit getrenntem Ausweis |
| **F4** | Verhalten in Negativpreis-Intervallen bei Direktvermarktung: Abregelung (Menge entfällt) oder Einspeisung zum Negativpreis? | Abregelung (Standard der Direktvermarkter) |
| **F5** | Vorbelegung „Ausfallanteil der Einspeisearbeit" für Stufe 1 (ohne Spotreihe): 20 %? | 20 % (LfL 2025: 23,4 %), editierbar, Messwert-Anzeige wenn Spotreihe da |
| **F6** | 60-%-Kappung mitrechnen (aus Zeitreihe) oder nur Hinweis? | mitrechnen — billig und exakt |
| **F7** | PV-Dialog wird führende Vergütungsquelle auch für die Speichersimulation (`v_pv`), `energy_project_settings.Verguetung_PV` wird Übergangsaltlast? | ja — eine Vergütungswahrheit |
| **F8** | Solarpaket-Aufschlag +1,5 ct/kWh als vorbereiteter Katalogwert (Status VORLAEUFIG, nicht angewandt)? | ja |
| **F9** | § 51-Altanlagen-Staffeln (IBN vor 25.02.2025: 4/6-h-Regeln) abbilden? | nein — EPOS-Plan plant Neuanlagen; Altanlagen ohne Ausfallrechnung |
| **F10** | Stundenscharfe Bezugsbewertung (Preiszeitreihe in der Wirtschaftlichkeit) sofort in P4 oder späterer Ausbau? | sofort — die Naht wird ohnehin geöffnet |
| **F11** | EEG-Novelle 2027 (Kabinettsbeschluss 29.07.2026): weitere Vermarktungsform „Übergangszahlung" als Platzhalter vorsehen? | nur Katalog-/Enum-Reserve, keine Logik |
| **F12** | Bestätigung: aktiver PV-Dialog ersetzt die PV-Einspeisezonen der Tarifstruktur (`Einsp_*` wirken nur noch für KWK)? | ja |

---

## Anhang A — Monatsmarktwerte Solar (Saatdaten für 6.3)

Quelle: netztransparenz.de (Anlage 1 Nr. 5.2 zu § 23a EEG), rekonstruiert über LfL Bayern Solar-012
(Stand 26.01.2026) bzw. Einzelbelege pv-magazine/DGS. Mit ✔ markierte Werte sind durch unabhängige
Einzelbelege gegengeprüft; **vor Freigabe komplette Reihe gegen den netztransparenz-CSV-Download
verifizieren** (Prüfschritt P3). Alle Werte ct/kWh.

| Monat | 2024 | 2025 | 2026 |
|---|---:|---:|---:|
| Januar | 7,535 | 11,511 | 11,019 ✔ |
| Februar | 5,875 | 11,099 ✔ | 7,717 ✔ |
| März | 4,965 ✔ | 5,027 ✔ | 5,455 ✔ |
| April | 3,795 | 3,041 ✔ | 1,317 ✔ |
| Mai | 3,161 | 1,997 | 3,163 ✔ |
| Juni | 4,635 | 1,843 ✔ | 6,190 ✔ |
| Juli | 3,554 | 5,923 ✔ | 5,226 ✔ |
| August | 4,263 | 3,832 | — (Veröff. ~10.09.2026) |
| September | 4,512 ✔ | 4,307 | — |
| Oktober | 6,752 | 6,980 | — |
| November | 10,076 | 9,102 ✔ | — |
| Dezember | 11,171 | 9,373 ✔ | — |
| **Jahresmarktwert** | **4,624** | **4,508** | — |

Nicht verwechseln: der auf netztransparenz.de zusätzlich ausgewiesene „Jahresmittelwert nach § 33
EEG 2012" (2024: 5,858; 2025: 6,170) ist **nicht** der für die Marktprämie maßgebliche Wert.

## Anhang B — § 51-Kohorten (nur zur Einordnung; Modell bildet die Neuanlagenregel ab, F9)

| Inbetriebnahme | betroffen ab | Regel |
|---|---|---|
| vor 01.01.2016 | — | keine Reduktion |
| ab 01.01.2016 | 500 kWp | 6 aufeinanderfolgende Negativstunden |
| ab 01.01.2021 | 500 kWp | 4 aufeinanderfolgende Negativstunden |
| ab 01.01.2023 | 400 kWp | 4 h → 3 h (2024/25) → 2 h (2026) → jede Stunde (ab 2027) |
| **ab 25.02.2025** | **2 kWp** (Ausnahmen 3.2) | **jede negative Viertelstunde** |

Gemessener Förderverlust je Kohorte (LfL, SMARD-Profil): Neuanlagen 2023: 6,7 % → 2024: 14,5 % →
2025: 23,4 % der Solararbeit; Alt-Kohorte „ab 2016, ≥ 500 kWp": 2025 nur 3,8 %.

---

## Nachtrag 1 (25.08.2026) — Abgleich mit pv@now („PV-Now", DGS Franken)

Quellen (Ordner `PV-Konzept_PV-Now\`, Stand 25.08.2026): `Betreibermodelle_Verguetungen_PV_Uebersicht (1).docx`
(Systematik des pv@now manager 10.0.0: Betreibermodelle, Erlöswege, Rechtsrahmen),
`Betreibermodelle_Verguetungen_PV (2).xlsx` (11 Blätter: Modellkatalog, Erlöswege, Methodik,
Kennzahlen-Herleitung, Vergütungssätze, Marktwerte, Projektvergleich „INEKON Schulung 01"),
`kennzahlen_modell.py` (monatsgenaue Nachbildung beider Schulungspläne, 250 Monate, Abgleich gegen
Tool-Werte, Ø-Abweichung 0,20 %). Alle Angaben netto, ct/kWh.

### N.1 Korrekturen am Rev.-1-Rechenmodell

**N1 — Degression rechnet auf unrundeter Basis (Korrektur zu § „Degression").** Rev. 1 schreibt
„1 % je Halbjahr, je Schritt auf 2 Nachkommastellen gerundet". Die von der BNetzA mitveröffentlichten
**unrundeten** Werte beweisen die andere Methode — Basis bleibt unrundet, gerundet wird nur der
Ausgabewert: 8,60 × 0,99⁵ = 8,17851 → feste EV 7,77851 (BNetzA-Wert exakt); ebenso
7,50 × 0,99⁵ − 0,40 = 6,73243 ✔, 6,20 × 0,99⁵ − 0,40 = 5,49614 ✔, Voll 13,40 × 0,99⁵ − 0,40
= 12,34327 ✔. Schrittweises Runden liefert an Zwischenstichtagen abweichende Werte (z. B. 08/2024:
8,42 statt 8,43). **Folge für `EegSatzRechner`:** intern mit unrundeter Kette rechnen
(`Basis × 0,99^n`), Rundung auf 2 Nachkommastellen erst am anzuwendenden Satz; die
Rev.-1-Unit-Tests gegen alle 16 BNetzA-Werte bleiben das Abnahmekriterium und sind um die
unrundeten Werte (Blatt „Vergütungssätze" Block 3) zu ergänzen. Nächste Absenkung: 01.02.2027.

**N2 — Marktprämie mit Jahresmarktwert (Prüfpunkt mit Korrekturempfehlung).** Rev. 1 formuliert den
DV-Erlös monatlich: `MW_solar(Monat) + max(0, AW_mix − MW_solar(Monat))`. pv@now setzt für
Inbetriebnahme **ab 01.01.2023** die Jahresmarktwert-Systematik an: Marktprämie
`MP = max(0, AW_mix − Jahresmarktwert Solar)`, einheitlich fürs Kalenderjahr (2025: 4,508; 2024:
4,624 ct/kWh); der Spoterlös bleibt zeitaufgelöst. Der Unterschied ist materiell: die monatliche
max(0,…)-Klammer kappt Monate mit MW > AW (z. B. Januar 2026: 11,019 ct/kWh) und verschenkt dort
die Prämiendifferenz. Da `Tab_ProjektPhotovoltaik` bereits `MarktwertJahresmittel` führt, ist die
Umstellung modellseitig klein: **Erlös DV = Spoterlös (zeitaufgelöst) + MP (Jahr)**; die
Monatsmarktwerte bleiben für die Bewertung des Spoterlöses und für § 51-Zeitfenster relevant.
Altanlagen-Monatssystematik ist nicht erforderlich (F9: EPOS-Plan plant Neuanlagen).

**N3 — Ausfallvergütung nur > 100 kW.** Zum vorhandenen `EEG_AUSFALLVERGUETUNG_ABSCHLAG` (20 %,
also 80 % des AW) kommt die Zulässigkeitsregel: Anspruch nur für Anlagen **> 100 kW**
(§ 21 Abs. 1 Nr. 3 i. V. m. § 53 Abs. 3 EEG), begrenzt auf max. 3 Monate in Folge und 6 Monate je
Kalenderjahr. pv@now weist den Satz fälschlich auch bei 30 kWp aus — **nicht nachbauen**; der
Dialog blendet die Option unterhalb 100 kW aus.

**N4 — Unentgeltliche Abnahme mit 200-kW-Grenze.** Die Vermarktungsform `PV_KEINE` (unentgeltliche
Abnahme, § 21 Abs. 1 Nr. 2 EEG) gilt nur für Anlagen **< 200 kW**; kein 0,40-ct-Abzug. Als
Plausibilitätsregel im Dialog ergänzen.

**N5 — Vorbelegung DV-Entgelt.** Rev. 1 belegt `DvEntgelt` mit 0,3 ct/kWh vor (Bandbreite 0,1–0,6);
pv@now nutzt 0,40 ct/kWh (Analogie zur Vermarktungspauschale § 53 Abs. 1 Nr. 2). Empfehlung:
**0,40** übernehmen — Rechenergebnisse werden damit unmittelbar mit pv@now-Rechnungen vergleichbar;
editierbar bleibt der Wert ohnehin. (Abstimmung Philipp.)

### N.2 Bestätigte Rev.-1-Festlegungen

pv@now bestätigt ohne Abweichung: anzulegende Werte 08/2026–01/2027 (Überschuss 8,10/7,06/5,84/
5,84/5,84 · Voll 12,62/10,64/10,64/8,85/7,63; feste EV je −0,40: 7,70/6,66/5,44 bzw. 12,22/10,24/
10,24) · feste EV nur ≤ 100 kW · leistungsanteiliger Mischsatz (§ 23c EEG; Gegenbeispiele 30 kWp
→ AW 7,41 / 100 kWp → AW 6,43 decken sich mit der Rev.-1-Methode) · MP ≥ 0 ·
Solarpaket-Aufschlag +1,5 ct/kWh **nicht anwenden** (§ 101 EEG, EU-Beihilfevorbehalt; ebenso
ausgesetzt: § 48 Abs. 1b Agri-PV, § 37b, § 37d; Clawback-Risiko offen) · § 51 ab erster negativer
Viertelstunde für IBN ab 25.02.2025, Ausnahmen < 100 kW bis iMSys-Einbau und < 2 kW; in der
Direktvermarktung keine Kappung · § 51a-Nachholung Faktor 0,5 mit ertragsgewichteten
Monatskontingenten · 60-%-Kappung § 9 Abs. 2 bei fester EV ohne iMSys · Vergütungsdauer 20 Jahre
zzgl. Inbetriebnahmemonate (§ 25 Abs. 1) · Jahresmarktwerte 2024 = 4,624 / 2025 = 4,508 ct/kWh
samt Verwechslungswarnung zum „Marktwert Solar(a) nach § 33 EEG 2012" (2025: 6,17) ·
Eigenverbrauch ist keine Vergütung, sondern vermiedener Netzbezug zum vollen Arbeitspreis
(Fallbeispiel: 45,83 ct/kWh Bezug gegen 7,47 ct/kWh Einspeisung — Verhältnis 6,1 : 1; stützt die
stundenscharfe Bezugsbewertung, F10).

### N.3 Ergänzungen aus pv@now (Übernahmevorschläge)

1. **Betreibermodell-Achse bestätigt F1-Zuschnitt.** pv@now trennt Betreibermodell (Rollen
   Investor/Betreiber/Eigentümer/Verbraucher; 5 Familien, 25 Modelle) vom Erlösmodell — Kennzahlen
   sind akteursbezogen. EPOS-Plan rechnet die Personalunion I = B = G = V (Bauherr); dafür genügen
   Einspeiseart (Überschuss/Voll) + Vermarktungsform aus Rev. 1 vollständig. Mieterstrom-/Miet-/
   Teilmietmodelle wären Mehrparteien-Rechnungen → außen vor, Katalogschlüssel reserviert
   (F1-Empfehlung gestärkt). Für den reservierten Mieterstrom-Schlüssel die aktuellen Zuschläge
   notiert: IBN 08/2026–01/2027: 2,51 / 2,33 / 1,57 ct/kWh (≤ 10 / ≤ 40 / ≤ 1.000 kW).
2. **Monatsmarktwerte 2026 für die Saat** (Anhang A, Spalte 2026, bisher leer): Jan 11,019 ·
   Feb 7,717 · Mär 5,455 · Apr 1,317 · Mai 3,163 · Jun 6,190 · Jul 5,226 ct/kWh (August bei
   Erstellung noch unveröffentlicht). Der April-Wert zeigt die solare Kannibalisierung.
3. **Kennzahlen-Ausweis der Vorschau (Gruppe 7) erweitern:** Stromgestehungskosten **LCOE₀**
   (KZS = 0) zusätzlich zum diskontierten LCOE — nur LCOE₀ ist mit einem Vergütungssatz
   vergleichbar; **Eigenverbrauchsquote und Autarkiegrad stets als Paar** anzeigen (Fallbeispiel
   83 % vs. 26 % — einzeln entsteht ein falscher Eindruck); Kennzahl „Vorteil durch PV je Jahr
   [€/a]". Alles Anzeige aus dem vorhandenen Rechenweg, keine Zweitrechnung.
4. **EEG-Novelle 2027 (Regierungsentwurf, Kabinett 29.07.2026)** substantiiert F11: Wegfall der
   festen EV für Neuanlagen, DV-Pflicht, einheitlicher AW 6,2 ct/kWh, Übergangszahlung 36 Monate
   mit sinkenden Schwellen (2027 < 50 kW, 2028 < 25 kW, 2029/30 < 7 kW, ab 2031 keine), DV-Bonus
   bis 1,5 ct/kWh (< 25 kW, max. 48 Monate), dauerhafte Einspeisekappung 50 % Dach / 70 %
   Freifläche. Kein geltendes Recht — Enum-/Katalogreserve wie in F11 empfohlen genügt;
   zusätzlich Hinweiszeile im Dialog bei Inbetriebnahmedatum ab 2027 („Rechtslage Entwurfsstand;
   Bestandsschutz § 25 EEG bei IBN bis 31.12.2026").
5. **Anhaltswerte Strompreis** für die Vorbelegungsprüfung: BDEW 08/2026 Haushalte 37,0 ct/kWh
   (Quelle ohne Netto-/Brutto-Kennzeichnung), Industrie/Gewerbe Mittelspannung 17,2 ct/kWh netto;
   PPA Solar DE 2025: 3,5–4,5 ct/kWh (deckt die Rev.-1-Vorbelegung PpaPreis).

### N.4 Referenz-Testfall für die Abnahme (Etappe P6)

Der pv@now-Schulungsfall „INEKON Schulung 01" wird Referenz-Testfall des EPOS-Rechenwegs:
30 kWp · 1.078 kWh/(kWp·a) (PVGIS Stuttgart) · Leistungsminderung 5 % über Laufzeit · Investition
54.000 € (100 % EK) · KZS 3,5 % · 250 Monate (20 Jahre + 10 IBN-Monate) · Gesamtbedarf
100.000 kWh/a (Lastprofil G4) · Netzstrompreis 30 ct/kWh, +4 %/a · Plan Überschuss: EV-Quote
83,4 %, feste EV 7,47 ct/kWh (Mischsatz IBN 2023), Betriebskosten 2.040 €/a · Plan Volleinspeisung:
11,60 ct/kWh, 1.600 €/a. Soll-Kennzahlen (pv@now): Kapitalwert **+92.568 €** / **−22.979 €**,
IRR 15,99 % / −2,10 %, Amortisation 8/9 Jahre bzw. keine, LCOE 17,85/16,47 (LCOE₀ 14,64/13,25)
ct/kWh. `kennzahlen_modell.py` (Ordner PV-Konzept_PV-Now, reines Python ohne Abhängigkeiten)
reproduziert diese Werte monatsgenau mit Ø 0,20 % Abweichung und dient als **Prüfstand**: Der
EPOS-Rechenweg muss den Fall in derselben Größenordnung treffen (Toleranzvorschlag ±1 % auf den
Kapitalwert; Abweichungsquellen dokumentieren — EPOS rechnet jahresweise/ValERI, pv@now
monatsgenau). Bekannte Sensitivitäten der Annahmen (aus dem Modellabgleich): LCOE-Menge
undiskontiert 7,25 % · Minderung erst am Laufzeitende 4,9 % · Messkosten als PV-Ausgabe 3,6 %.

Hinweis daraus für die Betriebskosten-Vorlage Photovoltaik (Kostendialoge-Konzept, Folie 24):
Zählermiete/Messstellenbetrieb nur ansetzen, soweit **PV-bedingt** — mit und ohne PV identische
Messkosten sind ergebnisneutral und verfälschen sonst den PV-Vergleich.

### N.5 Bewusst nicht übernommen

Mehrparteien-Kennzahlensätze (je Akteur getrennte Auswertung), Anlagenmiete/Teilmiete/Dachpacht als
Zahlungsströme, Umsatzsteuer-Logik (Nullsteuersatz § 12 Abs. 3 UStG, Kleinunternehmerregelung),
Ertragsteuereffekte (AfA, IAB, Sonder-AfA § 7g), Kreditfinanzierung (bis 2 Kredite), Rückbau-/
Restwertposten als eigene Dialogfelder — kollidiert mit den bestehenden Nicht-Zielen (netto, keine
Finanzierungsrechnung, ValERI-Systematik) bzw. mit der Ein-Akteur-Sicht von EPOS-Plan.
Ausschreibungsergebnisse und PPA-Marktreihen bleiben Anhaltswerte, keine Katalogdaten.

### N.6 Wirkung auf die offenen Fragen

- **F1** (Mieterstrom/GGV): Empfehlung „außen vor, Schlüssel reservieren" durch die
  Betreibermodell-Systematik bestätigt; aktuelle Zuschlagssätze notiert (N.3 Nr. 1).
- **F5** (Ausfallanteil-Vorbelegung 20 %): unverändert; pv@now liefert dazu keinen Gegenwert.
- **F9** (keine Altanlagen-Staffeln): bestätigt — N2 betrifft nur die Neuanlagen-Systematik.
- **F10** (stundenscharfe Bezugsbewertung sofort): gestärkt durch das 6,1:1-Verhältnis (N.2).
- **F11** (Novelle 2027): Entwurfsstand konkretisiert (N.3 Nr. 4), Empfehlung unverändert.
- **Neu F13:** Feld „Anlagenart" (Gebäude / Lärmschutzwand / Sonstige nach § 48 Abs. 1) aufnehmen
  oder Gebäude fest annehmen? Empfehlung: Gebäude fest vorbelegt, Katalogreserve für „Sonstige"
  (Freiflächensätze 6,59/6,19 ct/kWh nur als Katalogzeilen, kein Dialogpfad).
- **Neu F14:** Anlagen > 1 MW ohne Ausschreibung (§ 48 Abs. 1a: 2026 1. Segment 5,48 / Gebäude
  10,16 ct/kWh, kalenderjahresbezogen, keine § 49-Degression) und Ausschreibungspfad (§ 22 Abs. 3)
  abbilden? Empfehlung: nein — Rev.-1-Warnschwellen (kWp > 1.000 / > 2.000) genügen, Katalogreserve.
