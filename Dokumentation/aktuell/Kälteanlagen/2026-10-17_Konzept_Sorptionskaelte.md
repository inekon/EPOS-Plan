# Konzept: Sorptionskälte (Absorption und Adsorption) in EPOS-Plan

**Stand 17.10.2026** · Anlass: Anwenderauftrag vom 17.10.2026 („erstelle ein Konzept die Absorptionskälte mit
aufzunehmen. Gibt es zu diesen Anlagen Daten?“) · Codestand `f791209c0` (Zweig `ios_migration_september`) · nimmt die
zurückgestellte Stufe **K-G** der [Konzeptprüfung Kälteanlagen](2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md)
(5.1, Frage KKP-Q6, Entscheid E118) auf und schlägt vor, sie durch den Stufenplan **SOK** (Sorptionskälte) zu ersetzen ·
Vorlage zur Entscheidung, kein Code geändert.

Externe Angaben sind Recherchestand vom 17.10.2026, viele nur aus Suchauszügen (die meisten Fachseiten waren aus der
Sitzung nicht abrufbar; abgerufen wurden Dateien auf GitHub). Normen werden nur mit Fundstelle genannt, ihre Inhalte
sind eigene Zusammenfassung und — wo vermerkt — nicht am Wortlaut geprüft. Förderhinweise sind Prüfpunkte, **kein
Rechtsrat**. Das Papier nennt keine Kennwerte konkreter Produkte; Bandbreiten stammen aus Übersichtsliteratur, das
Rechenbeispiel aus einem veröffentlichten Forschungsgerät.

---

## 0. Antwort in Kürze

**Gibt es Daten?** Ja, aber nicht in der Form, die EPOS-Plan für Kältemaschinen gewohnt ist:

1. **Kein maschinenlesbarer Herstellerkatalog.** VDI 3805 hat kein Blatt für Kälte- oder Sorptionsmaschinen; die 39
   VDI-3805-Dateien im Repositorium (Blätter 3, 19, 20, 22) enthalten kein Sorptionsgerät. EPREL führt keine
   Kaltwassersätze, Eurovent zeigt kein eigenes Absorptionsprogramm, das AHRI-Verzeichnis ist lizenzpflichtig.
2. **Herstellerdatenblätter** führen das, was ein Stundenmodell braucht: Nennpunkt mit Kälteleistung, Antriebswärme
   und Rückkühlwärme bei Heiz-, Kühl- und Kaltwassertemperatur, Wärmeverhältnis, Leistungskorrekturdiagramme über
   Heizwasser- und Kühlwassertemperatur, Einsatzgrenzen, Hilfsstrom. Sie liegen als PDF vor; Tabellen kommen aus den
   Auslegungsprogrammen der Hersteller beim Anwender.
3. **Normen liefern Prüfbedingungen und Pauschalwerte, keine Kennfelder:** DIN/TS 18599-7:2025-10 (Monatsverfahren mit
   Nennkennwert und mittlerem Teillastfaktor, auch für Absorption), AHRI 560-2023 (Prüfbedingungen und Teillastkennzahl
   für LiBr-Maschinen), DIN EN 12309 (nur gasbefeuerte Sorptionsgeräte bis 70 kW), VDMA 24247-9:2022-01 (Leitfaden
   Sorptionskälteanlagen).
4. **Offene Rechenparameter gibt es:** Die *charakteristische Gleichung* (Hellmann, Schweigler, Ziegler; angepasst von
   Kühn und Ziegler) beschreibt eine Sorptionsmaschine mit sechs Parametern; **oemof-thermal (MIT-Lizenz)** liefert fünf
   Parametersätze (vier ein- und ein zweistufiges LiBr-Gerät, 4,5 kW bis 1 163 kW), Puig-Arnavat u. a. (2010) die
   Anpassung an Katalogdaten. Ein Satz davon ist erkennbar fehlerhaft (3.3).
5. **Simulationsbibliotheken** bieten Kurvenmodelle (EnergyPlus `Chiller:Absorption:Indirect`, direkt- und abgasbefeuerte
   Kälte-/Heizgeräte, Modelica Buildings), aber kaum Datensätze: EnergyPlus liefert einen abgasbefeuerten Kurvensatz und
   Beispielkurven in Testdateien.
6. **Forschung und Handbücher** (IEA SHC Task 38/48/53, Solar Cooling Handbook, Fraunhofer ISE, ILK Dresden, ZAE Bayern,
   TU Berlin) geben Bandbreiten (einstufig LiBr ≈ 0,7 bei 80–120 °C, zweistufig ≈ 1,3 bei 160–180 °C, Adsorption
   0,2–0,6) und Prüfverfahren, keine freien Gerätekataloge.

**Folge für EPOS-Plan:** Ein Typkennfeld je Bauart aus der charakteristischen Gleichung, skaliert auf den Nennpunkt des
Datenblatts, ist der tragfähige Rückfall — wie die neutralen Typkennfelder der elektrischen Kältemaschine (KM1). Eigene
Datenblattpunkte passen die sechs Parameter an.

**Empfehlung:** zuerst die **einstufige LiBr/H₂O-Maschine mit Heizwasserantrieb**, gerechnet mit der charakteristischen
Gleichung, in zwei Antriebswegen: **extern** über einen Energieträger (Fernwärme, Abwärme) an ihrem Platz in der
Kältefolge und **gekoppelt** an Erzeuger des Projekts (BHKW, Solarthermie) über eine Vorstufe vor beiden Kaskaden.
Stufenplan SOK-0 bis SOK-6, rund 25–35 PT (Abschnitt 11); jedes bestehende Referenzprojekt bleibt byte-gleich.

---

## 1. Bestand: worauf Sorptionskälte aufsetzt

### 1.1 Bausteine

| Baustein | Stand (Code, Schema) | Trägt | Fehlt für Sorption |
|---|---|---|---|
| **Kältemaschine als Erzeuger** | Anlagenart 13 in `Tab_Typ_Energieanlagen`; Gerät `Tab_Kaeltemaschine(_STAMM)`, Kennfeld `Tab_Kenndaten_Kaeltemaschine(_STAMM)` (Rückkühl- × Kaltwassertemperatur → Kälteleistung, EER); Rechenklasse `EPOS.Kern/Allgemein/Simulation/Kaelte/Kaeltemaschine.cs`, Teillast und Takten (`Kaeltemaschinenteillast.cs`, KM3), Randweg (`KaeltemaschinenRand.cs`) | Katalog, Projektkopie, Kältefolge, Rückkühlart, Mindestteillast, Kostenvorlage, Ergebnis `Tab_ErgebnisKaeltemaschine` | Antriebswärme, dritte Temperaturachse, Wärmeverhältnis; der EER ist **Strom**-Leistungszahl |
| **Katalogfeld Geräteart** | K-A, Schritt 211: `Geraeteart` mit `CHECK` auf `KWS_LUFT`, `KWS_WASSER`, `KWS_FREIKUEHLUNG`, `SPLIT`, `MULTISPLIT`, `VRF`, **`ABSORPTION`** (`KaelteKatalogfelderSchema.GERAETEART_ABSORPTION`, „Stufe K-G“); die CSV-Vorlage `Quellen/Kaeltemaschine_Kennfeldvorlage.csv` nimmt den Wert an | Kennzeichen einer Sorptionsmaschine im Katalog; die Kältemittelprüfung gibt für `ABSORPTION` keinen Befund (`Kaeltemittelzulaessigkeit.GERAETEKLASSE_ABSORPTION`) | kein Wert `ADSORPTION`; eine `CHECK`-Änderung an einer `STRICT`-Tabelle erzwingt den Neubau der Tabelle |
| **Typkennfelder** | `EPOS.Kern/Allgemein/Katalog/KaeltemaschinenTypkennfelder.cs` mit 34 neutralen Sätzen aus PNNL Copper (BSD-2), eingespielt mit `ReadOnly = 1` und Katalogschlüssel | Muster für neutrale, eingebettete Typkennfelder samt Quelle und Lizenz | kein Sorptionssatz (die Copper-Sätze sind Verdichtermaschinen; die Teillastableitung `KaeltemaschineTeillastkurve.Verdichterregelung` liefert für die Verdichterart „absorption“ keine Regelung) |
| **Kältefolge** | `Kaeltefolge.cs`: Stufen freie Kühlung → Kältespeicher → Wärmepumpen → Kältemaschinen, Rang je Anlage `Kaelte_Rang`; Rechnung in `Kaeltekaskade.cs` | Platz eines Kälteerzeugers | — |
| **Rückkühlwerk** | K-F1 bis K-F3 (E120): eigenes Glied `Rueckkuehlwerk.cs`, `Tab_Rueckkuehlwerk(_STAMM)`, Verweis `ID_Rueckkuehlwerk` an der Anlagenzeile; Annäherung fest oder lastabhängig, Ventilatorstrom, Wasserbilanz, Teil-Freikühlung `REIHE` | Rückkühltemperatur aus der Abwärme der Stunde, Ventilator- und Wasserbilanz | Abwärme heute = Kälte + Verdichterstrom; bei Sorption Kälte + Antriebswärme + Hilfsstrom (etwa das Doppelte) |
| **Kältespeicher** | Pufferzeile mit Verwendung Kälte, Entladung in der Kältefolge, geladen von allen Kälteerzeugern (Kühlkonzept 4.6) | Entkopplung über die Zeit | — |
| **Kälte-Wärme-Kopplung KWN** | KWN-0 (Schritt 218, `Kaelte_Art` Raum/Prozess), KWN-1a (Rechenklasse `KaelteWaermeKopplung.cs`); KWN-1b (Schritt 242, Spalte `Kopplungsseite` an `Z_AnlageSenke`) und KWN-1c (Kopplungsvorstufe vor beiden Kaskaden, Kopplungsbuchung, erweiterte Deckungsprobe) auf Zweig `kwn1c-kopplungsvorstufe`, noch nicht gemergt; Kühlkonzept [5.6](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) | **Ort und Muster** einer Stufe, die vor beiden Jahresschleifen Wärme und Kälte derselben Stunde entscheidet, und die benannte Tür durch die Trennung der Deckungswelten | Sorption ist der umgekehrte Fall: Wärme **hinein**, Kälte heraus |
| **Wärmekaskade mit Senken** | `Kaskadenschleife.cs`, Plätze `Tool_1…4`; Senken je Erzeuger in `Z_AnlageSenke` (Ziele `Heizkreis`, `PufferHeizung`, `PufferBrauchwasser`, `PufferKombi`, `Prozesswaerme`, `PufferProzess`, `Kaeltekreis`; Persistenzwerte in `DbWerte.cs`) | Senkenbindung je Quelle mit Rang, Puffer als Zwischenglied | kein Ziel „Antrieb einer Sorptionsmaschine“, keine Verweisspalte auf eine Zielanlage |
| **Wärmequellen** | BHKW (`Tab_BHKW` mit `Vorlauf`, `Ruecklauf`, Teillast, Anfahrverlust, Mindestlaufzeit), Solarthermie (Kollektorfeld, Solarkreis mit `Arbeitstemperatur_Weg`, Senken direkt und über Puffer, Referenzprojekt 1049), Heizkessel, Pufferspeicher (Schichten, `Nutzung_*`) | Antriebswärme in passender Temperatur (BHKW-Vorlauf 80–90 °C; Kollektorfeld je nach Bauart) | **Fernwärme** gibt es nur als Brennstoff 23 bzw. Energieträger „Fernwärme“ (`pricing_model` HEAT) — ein Kessel mit diesem Brennstoff ist der Ersatz einer Übergabestation; eine eigene Anlagenart fehlt |
| **Wirtschaftlichkeit und Emissionen** | Kostenkomponente 11 „Kältemaschine“, Kostenvorlagen; Emissionen über Träger und Brennstoff; KWK-Bilanzierung nach `BilanzKonvention.cs` (L12, L13) | Kosten und Emissionen der Antriebswärme über die Quelle bzw. den Träger | Kennzahlen je Nutzen (Kälte aus Wärme) |

### 1.2 Befunde

- **Eine Kältemaschine mit Geräteart `ABSORPTION` rechnet heute als elektrische Kältemaschine.** Die Geräteart wirkt
  nur in Katalogfilter und Kältemittelprüfung; die Simulation liest sie nicht. Wer über die CSV-Vorlage eine
  Absorptionsmaschine einspielt und das Wärmeverhältnis in die EER-Spalte schreibt, bekommt einen Kältestrom von Q₀/ζ
  (bei ζ = 0,75 das 1,3-Fache der Kälte) statt eines Hilfsstroms von wenigen Prozent; wer einen „elektrischen“ EER
  einträgt, bekommt keine Antriebswärme. Empfehlung: Hinweis in der Konfigurationsprüfung, bis SOK rechnet (Frage
  SOK-Q10).
- **Testdatenbank:** 37 Kältemaschinen im Katalog (12 `KWS_LUFT`, 25 `KWS_WASSER`), 4 Projektkopien (`KWS_WASSER`),
  keine mit Absorption; kein Typ, keine Beschreibung nennt Sorption. Das Referenzprojekt **1055** (und seine Kopie
  **1063**, in der CI-Auswahl) trägt Kühlbetrieb, Kältemaschine mit Trocken-Rückkühler, Kältespeicher **und ein BHKW** —
  die natürliche Vorlage eines Sorptions-Referenzprojekts.
- **Code und Papiere:** Sorption kommt nur als Geräteart (K-A), als Kältemittelklasse (KM4), als Matrixzeile und
  Stufe K-G der Konzeptprüfung (Abschnitte 3, 4, 5) und im Entwurf Split/VRF/Rückkühlwerk (zurückgestellt) vor. Alle
  übrigen Treffer für „Absorption“ betreffen den solaren Absorptionsgrad der Bauteile (DA-8).
- **`Quellen/`:** DIN EN 14825 (Fassung 2023-10, Anwendungsbereich nur elektrisch angetriebene Verdichter) und
  DIN/TS 18599-10 liegen vor; DIN/TS 18599-7 nicht.

---

## 2. Gegenstand, Begriffe und Abgrenzung

### 2.1 Bauarten

Bandbreiten aus Übersichtsliteratur (Quellen [L5], [L6], [L8], [L9]); keine Produktwerte.

| Bauart | Arbeitsstoffpaar | Antrieb (Eintritt) | Kaltwasser | Wärmeverhältnis ζ (Band) | Leistung | Einsatz |
|---|---|---|---|---|---|---|
| **Absorption einstufig** | H₂O/LiBr (Wasser als Kältemittel) | Heizwasser 70–120 °C, Niederdruckdampf | ab etwa 5 °C (Wasser gefriert) | ≈ 0,6–0,8 | ab etwa 5 kW bis in den MW-Bereich | Klimakälte; KWKK mit BHKW, Fernwärme im Sommer, Solar, Abwärme |
| **Absorption zweistufig** | H₂O/LiBr | Dampf oder Heißwasser 140–180 °C, Abgas, direkt gasbefeuert | wie einstufig | ≈ 1,1–1,4 | mittlere und große Geräte | Dampfnetze, Gasturbinen- und Motorabgas, direkt befeuert |
| **Absorption dreistufig** | H₂O/LiBr | 200–240 °C | wie einstufig | ≈ 1,6–1,8 | große Geräte, selten | nur benannt |
| **Absorption NH₃/H₂O** | Ammoniak/Wasser | Heizwasser 90–120 °C, mehrstufig bis 175 °C; auch direkt gasbefeuert | auch unter 0 °C (Sole bis −50 °C mit mehrstufigen Kreisen) | ≈ 0,5–0,6 über 0 °C, unter 0,5 unter 0 °C | Kleingeräte (direkt befeuert, auch luftgekühlt) bis Industrieanlagen | Prozesskälte unter 0 °C, Gewerbekälte |
| **Adsorption** | Wasser an Silicagel oder Zeolith (fester Sorbent, zwei oder mehr Kammern im Wechsel) | Heizwasser 50–95 °C | Klimakälte, eher höhere Kaltwassertemperaturen | Geräte ≈ 0,4–0,6 (Literaturband 0,2–0,65) | Module etwa 10–40 kW, kaskadierbar | niedrige Antriebstemperatur: Solar, Fernwärme-Rücklauf, Rechenzentrums- und Motorkühlwasser |

**Gemeinsam:** Kältemittel verdampft im Verdampfer und kühlt das Kaltwasser; ein Sorbens nimmt den Dampf auf
(Absorber bzw. Adsorber), die Antriebswärme treibt ihn im Austreiber (Generator, Desorber) wieder aus, der Kondensator
verflüssigt ihn. **Absorber und Kondensator geben Wärme an das Kühlwasser ab** — die Rückkühlung führt die Kälte und
die Antriebswärme ab. Strom brauchen nur Lösungs- und Kältemittelpumpen (bzw. Ventile und Vakuumpumpe der Adsorption)
und die Regelung.

### 2.2 Antriebe

| Antrieb | Quelle in EPOS-Plan | Temperatur | Bemerkung |
|---|---|---|---|
| Heizwasser aus BHKW | BHKW (Motorkühlwasser und Abgaswärmeübertrager) | Vorlauf 80–90 °C | klassische Kraft-Wärme-Kälte-Kopplung (KWKK) |
| Heizwasser aus Solarthermie | Kollektorfeld direkt oder über Puffer | 70–95 °C nur mit Vakuumröhren bzw. konzentrierenden Kollektoren; Flachkollektoren eher für Adsorption | Ertrag und Kältebedarf fallen zeitlich zusammen |
| Fernwärme | heute nur Brennstoff 23 / Träger „Fernwärme“ | Sommer-Vorlauf netzabhängig 70–110 °C | Leistung mit Vorlauf stark veränderlich; Rücklaufanhebung ist Netzfrage |
| Abwärme | keine Quellart; als Träger mit Preis oder Lastgang abbildbar | prozessabhängig | Verfügbarkeitsprofil nötig |
| Heizkessel | Kessel | frei | primärenergetisch meist ungünstiger als Kompressionskälte; nur benannt |
| Dampf | keine Dampfseite in EPOS-Plan | > 120 °C | zweistufige Maschinen; nur benannt |
| direkt befeuert | Brennstoff (Gas) | — | Kenngröße GUE nach DIN EN 12309; als Träger abbildbar |

### 2.3 Begriffe

- **Wärmeverhältnis** ζ (engl. thermal COP): ζ = Q₀ / Q_H, Kälte je Antriebswärme.
- **GUE** (Gas Utilization Efficiency, DIN EN 12309): Nutzenergie je Gaszufuhr (Heizwert) bei direkt befeuerten Geräten.
- **Antriebswärme** Q_H (Generatorwärme), **Rückkühlwärme** Q_C (Absorber + Kondensator), **Hilfsstrom** P_int (intern).
- **Charakteristische Temperaturdifferenz** ΔΔt': Linearkombination der drei mittleren äußeren Temperaturen (5.2).
- **KWKK** — Kraft-Wärme-Kälte-Kopplung; das KWKG verwendet den Begriff für die Umwandlung von KWK-Nutzwärme in
  Nutzkälte durch thermisch angetriebene Kältemaschinen (§ 2, Begriffsbestimmungen, Fassung prüfen).
- **SOK** — Kurzzeichen dieses Papiers (Sorptionskälte). „SK“ ist vergeben (Energieflussbild, Status #1162–#1165),
  „KWK“ steht für das BHKW, „KWN“ für die Kälte-Wärme-Kopplung des Kühlkonzepts 5.6.

### 2.4 Was zuerst kommt — und was nicht

**Empfehlung: einstufig H₂O/LiBr mit Heizwasserantrieb** (SOK-Q2). Gründe: (1) Sie ist die Bauart der Anwendungen, für
die EPOS-Plan die Quellen schon rechnet — BHKW, Solarthermie, Fernwärme. (2) Für sie gibt es die meisten offenen
Parametersätze (3.3) und die Prüfnorm AHRI 560. (3) Ihr Kaltwasser liegt im Band der Klimakälte und der Kühlkurve
(Kühlkonzept), es braucht keine Sole. (4) Das Modell (charakteristische Gleichung) trägt Adsorption und zweistufige
Maschinen mit anderen Parametern ohne Codeänderung — das Datenmodell wird dafür offen angelegt.

**Nicht in der ersten Stufe:** Dampf und Abgas als Antrieb, direkt befeuerte Geräte (benannt, Datenmodell offen),
NH₃/H₂O unter 0 °C (braucht Prozesskälte mit Soletemperatur, KWN-Q7), Absorptionswärmepumpen und -transformatoren (Wärme
als Nutzen), Nutzung der Rückkühlwärme der Sorptionsmaschine für Niedertemperaturwärme, Fahrplanoptimierung über mehrere
Stunden, die Dynamik eines Adsorptionszyklus innerhalb der Stunde.

---

## 3. Datenlage

### 3.1 Normen und Regelwerke

| Quelle | Gegenstand und was sie liefert | maschinenlesbar | Nutzen für EPOS-Plan |
|---|---|---|---|
| **DIN V 18599-7:2018-09**, ersetzt durch **DIN/TS 18599-7:2025-10** (204 S.) [N1] | Endenergie Raumlufttechnik und Klimakälte im Nichtwohnbau. Kennwertverfahren mit Nennkennzahl und mittlerem Teillastfaktor PLV_av aus Stundenrechnungen typischer Erzeuger, **für Kompressions- und Absorptionskältemaschinen dokumentiert**; die Gliederung der Vornorm führt einen Unterabschnitt zur Erzeugernutzenergie der Wärmeversorgung einer Absorptionskältemaschine (7.1.4), Kennwerte in Anhang A und das Verfahren der Teillastfaktoren in Anhang B (Fassung 2018-09); Fassung 2025 mit überarbeiteten Kennwerten der Kälteerzeuger (Abschnitt 7) und neuem Abschnitt 8.5 samt Anhang H zur Abwärmenutzung von Kälteanlagen (nach Inhaltsangaben, nicht am Wortlaut geprüft) | nein (PDF, kostenpflichtig; nicht in `Quellen/`) | Plausibilitätsband für ζ und Teillast; Bilanzbegriffe. Kein Stundenkennfeld. GEG-Nachweise bleiben ausgeschlossen (Kühlkonzept §14) |
| **VDI 2067 Blatt 1** (2012-09, Entwurf 2025-04) [N2] | Wirtschaftlichkeit: Nutzungsdauer, Instandsetzung, Wartung je Komponente (Anhang) | nein | Kostenvorlage „Sorptionskältemaschine“; ob eine eigene Zeile für Absorption besteht, ist nicht geprüft. Zu Blatt 3 (RLT) und 40 (Energieaufwand der Erzeugung) wurde kein Sorptionsinhalt ermittelt |
| **VDI 3805** [N3] | Produktdatenaustausch TGA; Blätter u. a. 3 (Wärmeerzeuger), 19 (Solar), 20 (Speicher), 22 (Wärmepumpen) | ja (für vorhandene Blätter) | **kein Blatt für Kältemaschinen**, auch nicht für Sorption (Konzeptprüfung 4, Forschungsschlussbericht 2021). Eigene Suche in allen 39 `.vdi`-Dateien unter `VDI-3805-Daten/`: Treffer nur „Absorberbeschichtung“ (Kollektoren, Blatt 19); keine Gas-Sorptionswärmepumpe in Blatt 22 |
| **DIN EN 12309** Reihe [N4] | Gasbefeuerte Sorptionsgeräte für Heizen und/oder Kühlen bis 70 kW Wärmebelastung: Teil 1 Begriffe (2023), Teil 2 Sicherheit, Teil 3 Prüfbedingungen (2025-10, ersetzt 2014), Teil 6 saisonale Kennzahlen (2025: SGUE im Kühlbetrieb, Heizbetrieb), Teil 7 Hybridgeräte | nein | nur für direkt befeuerte Kleingeräte (später, SOK-Q5); keine Heizwasser-Absorber |
| DIN EN 14511 / 14825 | Prüfung und Saisonkennzahlen | nein | **nicht anwendbar**: Anwendungsbereich elektrisch angetriebene Verdichter (DIN EN 14825 Abschnitt 1, Fassung in `Quellen/`) |
| **AHRI Standard 560-2023** (ersetzt 560-2000) [N5] | Leistungsbemessung wassergekühlter LiBr-Absorptions-Kaltwassersätze und -Heizgeräte: Standard-Prüfbedingungen, Volllast und IPLV/NPLV (Teillastkennzahl); nicht für luftgekühlte, abgasbefeuerte Geräte und Wärmepumpen | Norm als PDF frei bei AHRI; Produktverzeichnis nur mit Lizenz | Nennpunkt-Definition für US-Datenblätter; Teillastkennzahl als Plausibilität |
| **Eurovent Certified Performance** [N6] | Programm LCP-HP (Kaltwassersätze, Wärmepumpen) | Daten nur mit Vertrag | **kein eigenes Absorptionsprogramm gefunden** (Recherchestand) |
| **VDMA 24247-9:2022-01** „Sorptionskälteanlagen“ [N7] | Leitfaden und Checkliste zur effizienten Einbindung (Abwärme, Solarwärme) | nein | Planungshinweise für Hilfetexte und Prüfregeln; keine Kennfelder bekannt |
| **AMEV Kälte 2017** (AMEV Nr. 139) [N8] | Planung, Ausführung, Betrieb von Kälteanlagen öffentlicher Gebäude | nein | Inhalt zu Sorption nicht eingesehen |
| **BAFA/BEG-Unterlagen** [F1] | Gerätelisten nur für Wärmepumpen (Konzeptprüfung 4); Kälte-Klima-Richtlinie seit 01.03.2024, bis 31.12.2026, nur nicht-halogenierte Kältemittel; die Fassung 2014 förderte Sorptionsanlagen 5–500 kW mit KWK- oder Abwärmeantrieb | nein | keine Kennwerte; nur Prüfpunkte (9.4) |
| **UK Energy Technology List** [F3] | Kategorie „Absorption and other heat driven cooling and heating equipment“, nur in CHPQA-zertifizierten KWK-Anlagen | Liste online | Nennwerte nach abweichender Grundlage; nur Hinweis |

### 3.2 Herstellerdaten

**Was ein Datenblatt typischerweise führt** (Muster aus Datenblättern und Fachartikeln [H1]–[H4]; keine Produktwerte):

| Angabe | Rechenbedarf in EPOS-Plan |
|---|---|
| Nennkälteleistung bei Nennpunkt; Nennpunkte der Kleingeräte etwa Heizwasser 80–90 °C, Kühlwasser 27–31 °C Eintritt, Kaltwasser 6–7 °C oder 15–18 °C (Kühldecke) Austritt | Nennpunkt der Gleichung, Skalierung des Typkennfelds |
| Antriebswärme und Rückkühlwärme am Nennpunkt, daraus ζ | Bilanzprobe (Q_C ≈ Q₀ + Q_H + P_int; an der Fremdkopie eines Kleingeräte-Datenblatts [H1] schließt sie auf unter 1 %) |
| Ein- und Austritt bzw. Spreizung und Volumenstrom aller drei Kreise, Druckverluste | mittlere Temperaturen der Gleichung; Pumpenstrom bleibt Eingabe |
| **Leistungskorrekturdiagramme**: Kälteleistung und Wärmeverhältnis über Heizwassertemperatur, meist als Kurvenschar je Kühlwassertemperatur, oft eine zweite Schar über Kaltwassertemperatur | Anpassungspunkte der sechs Parameter (8.2) |
| Teillastkurve (ζ über Last) oder Regelbereich (z. B. 10–100 %) | Mindestteillast, Prüfpunkte der Teillastdarstellung |
| Zulässige Heizwasser- und Kühlwassertemperaturen (Einsatzgrenzen, Mindestkühlwasser) | Grenzen in der Stunde (5.8) |
| Elektrische Leistungsaufnahme (Lösungspumpe, Kältemittelpumpe, Regelung; bei Adsorption Ventile, Vakuumpumpe) | Hilfsstrom intern |
| Rückkühlempfehlung (Kühlturm, Hybridkühler, Trockenkühler mit Mindestfläche) | Rückkühlwerk K-F |

**Daten als Datei:** Es gibt **kein** genormtes Austauschformat. Kein VDI-3805-Blatt, keine EPREL-Gruppe, kein
Eurovent-Programm. Hersteller veröffentlichen PDF-Datenblätter; Leistungstabellen entstehen in ihren
Auslegungsprogrammen (beim Anwender, Export in Excel/CSV möglich). BIM-Objekte führen Geometrie und Nennwerte (nicht
geprüft). Erfahrung aus der TRNSYS-Gemeinde (TESS, [M4]): Für Absorber gibt es „wenig Standardisierung der Datenformate“,
und ihre Kennfelder **normieren sich deutlich schlechter** als die von Wärmepumpen — ein Grund, statt normierter Tabellen
ein physikalisch gestütztes Modell zu wählen (4.5).

**Leistungsbereiche:** Kleingeräte (Absorption und Adsorption) etwa 5–50 kW, oft modular kaskadierbar; Absorber unter
etwa 5 kW waren laut einem älteren Forschungsbericht nicht am Markt [L10]; mittlere Geräte 50–500 kW; Großgeräte ein- und
zweistufig bis in den Megawattbereich (Fernkälte, Industrie, Rechenzentren).

### 3.3 Offene Datensätze und Literatur

| Quelle | Inhalt | Nutzungsrecht (Recherchestand) | Eignung als Typkennfeld |
|---|---|---|---|
| **oemof-thermal**, `examples/absorption_heatpump_and_chiller/data/characteristic_parameters.csv` [D1] | fünf Parametersätze der charakteristischen Gleichung (a, e, s_E, r_E, s_G, r_G): vier einstufige LiBr-Geräte (4,5 / 10 / 15 / 768 kW) und ein zweistufiges (1 163 kW); Quelle Kühn/Ziegler [L2] und Puig-Arnavat u. a. [L3] | Code MIT („oemof developing group“, 2019); die Parameterwerte sind veröffentlichte Messergebnisse bzw. Anpassungen; Herstellernamen stehen als Satzname — **neutralisieren** | **ja**, nach Prüfung. **Befund:** Satz 3 (768 kW, einstufig) führt s_G = s_E und r_G = r_E, also ζ ≡ 1 — für eine einstufige Maschine physikalisch unmöglich, offenbar ein Übertragungsfehler; nicht übernehmen |
| **Kühn, Ziegler (2005)** [L2] | Messung eines 10-kW-H₂O/LiBr-Geräts, angepasste charakteristische Gleichung (Parameter a = 2,5; e = 1,8; s_E = 0,42 kW/K; r_E = 0,9 kW; s_G = 0,51 kW/K; r_G = 2 kW, über [D1]; die Datei nennt keine Einheiten, sie folgen aus der Größenordnung des Geräts) | Veröffentlichung; Zahlen als Fakten zitierbar | **ja** (Forschungsgerät, kein Produkt) |
| **Puig-Arnavat, López-Villada, Bruno, Coronas (2010)** [L3] | Vergleich zweier Formen der charakteristischen Gleichung; Kühn/Ziegler ist die einfachste bei gleicher oder besserer Güte; Anpassung an Katalog- und Messdaten ein- und zweistufiger Maschinen | Veröffentlichung (Int. J. Refrig. 33(1), 70–78) | **ja** — Verfahren der Anpassung (multiple Regression) für den Import |
| **Hellmann, Schweigler, Ziegler (1998/1999)** [L1] | Herleitung: Kälteleistung und Antriebswärme als lineare Funktionen einer charakteristischen Temperaturdifferenz; analytisch lösbar, nah an der numerischen Simulation, solange die Betriebsbedingungen nicht zu weit vom Bezugspunkt liegen | Veröffentlichung | Begründung des Modells |
| **EnergyPlus** `datasets/ExhaustFiredChiller.idf` [D2] | **ein** Kurvensatz eines abgasbefeuerten zweistufigen Geräts (231 kW, Nenn-COP 1,39; Kapazität und Wärmeeinsatz über Temperatur, Wärmeeinsatz über Teillast), „nach persönlicher Mitteilung eines Herstellers“ (2011) | BSD-3 laut Recherche 08.10.2026, Lizenztext nicht geprüft | nur für zweistufig/Abgas, später |
| **EnergyPlus** `testfiles/IndirectAbsorptionChiller.idf` [D3] | Beispielkurven `Chiller:Absorption:Indirect` (dampfbetrieben): Wärmeeinsatz über Teillast (kubisch), Kapazitäts- und Wärmeeinsatzkorrektur über Kondensator- und Kaltwassertemperatur, **keine** Korrektur über die Heizwassertemperatur; Herkunft der Kurven nicht dokumentiert | wie oben | nur Gegenprobe der Teillastform |
| **Modelica Buildings** `Buildings.Fluid.Chillers.AbsorptionIndirectSteam` [D4] | Kurvenmodell wie EnergyPlus (sechs Funktionen), Datensatztyp `Data.AbsorptionIndirectSteam.Generic` | modifizierte BSD, nicht geprüft | Modellvergleich |
| **IEA SHC Task 38** (Solar Air-Conditioning), **Task 48** (Qualitätssicherung solarer Kühlung), **Task 53** [L4] | Marktübersichten und Anlagenlisten (Task 38, Subtask A 2–20 kW, B1 Anlagenliste, C1 Neuentwicklungen), Prüfverfahren für kontinuierliche und diskontinuierliche Sorptionsmaschinen (Task 48 A1, CNR/Fraunhofer ISE 2015), Bandbreiten ζ und Antriebstemperatur; Herstellervorträge (Task 53) | Berichte frei lesbar, Weiterverwendung von Tabellen nicht geprüft | Bandbreiten, Prüfbedingungen, Validierung — keine Gerätekennfelder |
| **Solar Cooling Handbook** (Henning, Motta, Mugnier; 3. Aufl. 2013) [L5] | Planungshandbuch aus Task 38: Bauarten, Kennzahlen, Auslegung | kostenpflichtig | Bandbreiten |
| **Fraunhofer ISE, ILK Dresden, ZAE Bayern, TU Berlin** [L6], [L10], [L11] | Entwicklungen kleiner Absorber (ILK: H₂O/LiBr-Kleingerät, DBU-Bericht; TU Berlin/TU Dresden/ZAE Bayern: Geräte um 50 und 160 kW), Double-Lift-Kreise mit ζ 0,30–0,35 für niedrige Antriebstemperaturen, Feldmessungen | Berichte frei lesbar | Bandbreiten, Validierung |
| **Ghafoor, Munir (2015)** [L8] | Übersicht solarer Kühlung: Absorption ζ 0,6–0,8 (Simulation) bzw. 0,40–0,85 (Experiment) bei 70–100 °C Antrieb; Adsorption 0,2–0,6 | Veröffentlichung | Plausibilitätsband |

**Frei nutzbar** sind damit: das Rechenmodell (Veröffentlichung), die Parametersätze von Kühn/Ziegler und Puig-Arnavat
als Zahlen (über oemof-thermal unter MIT) und die Bandbreiten. Gerätekataloge mit Herstellerbezug sind es nicht; die
ausgelieferten Typkennfelder bleiben deshalb **neutral** (Bezeichner „Typkennfeld Absorption einstufig 50 kW“, Firma
leer, Quelle und Lizenz in der Beschreibung, KKP-Q7).

---

## 4. Rechenmodelle für die Stundensimulation

### 4.1 Charakteristische Gleichung (Δt-Methode)

Hellmann, Schweigler und Ziegler zeigen, dass sich Kälteleistung und Antriebswärme einer Sorptionsmaschine in einem
weiten Betriebsbereich als **lineare Funktionen einer einzigen Temperaturdifferenz** schreiben lassen [L1]; Kühn und
Ziegler passen die Form an Messdaten an [L2], Puig-Arnavat u. a. bestätigen sie für ein- und zweistufige Maschinen [L3]:

```
ΔΔt' = t̄_H − a · t̄_AC + e · t̄_E          mittlere Temperaturen der äußeren Kreise [°C]
Q̇₀   = s_E · ΔΔt' + r_E                    Kälteleistung   [kW]
Q̇_H  = s_G · ΔΔt' + r_G                    Antriebswärme   [kW]
ζ    = Q̇₀ / Q̇_H
```

t̄_H Heizwasser (Austreiber), t̄_AC Kühlwasser (Absorber und Kondensator, in Reihe, gemeinsam), t̄_E Kaltwasser
(Verdampfer). Sechs Parameter, anpassbar aus wenigen Punkten (oemof nennt mindestens zwei Betriebspunkte; für a und e
braucht es Punkte bei verschiedenen Temperaturen). **Teillast** ergibt sich ohne weiteren Parameter, wenn die Maschine
über die Antriebstemperatur geregelt wird (Mischventil im Heizwasser): Die Regelung senkt ΔΔt', Q̇_H fällt mit s_G, und
der Achsenabschnitt r_G (Wärmeverluste, Lösungsumlauf) senkt ζ bei kleiner Last (Rechenbeispiel 5.9).

### 4.2 EnergyPlus

- **`Chiller:Absorption:Indirect`** [M1], [M2]: Kapazität = Nennkapazität × CAPFT_Verdampfer(t_Kaltwasser,aus) ×
  CAPFT_Kondensator(t_Kühlwasser,ein) × CAPFT_Generator(t_Heizwasser,ein) (je kubisch; die Generatorkorrektur nur bei
  Heizwasser); Antriebswärme = Kapazität × HIR(PLR) × Korrektur über Kühlwasser- und Kaltwassertemperatur ×
  Taktanteil min(1, PLR/PLR_min); Pumpenstrom = Nennpumpenleistung × EIR(PLR) × Taktanteil;
  **Q̇_Kond = Q̇_Verd + Q̇_Generator + P_Pumpe**. Eingaben u. a. minimale, maximale, optimale Teillast, untere Grenze der
  Kühlwassertemperatur (Warnung, keine Korrektur), untere Grenze des Kaltwasseraustritts, untere Grenze des
  Heizwassereintritts.
- **`Chiller:Absorption`** (BLAST): nur Teillastkurven; laut Engineering Reference „wahrscheinlich“ künftig entfernt.
- **`ChillerHeater:Absorption:DirectFired`** und **`…:DoubleEffect`** (abgasbefeuert): Brennstoff- bzw. Wärmeeinsatz
  = Kapazität × Laufanteil × Kennzahl × Korrektur über Temperatur × Korrektur über Teillast; Heizbetrieb zusätzlich.

### 4.3 TRNSYS Type 107

Einstufiger, heizwassergetriebener Absorber mit **normierter Leistungsdatei**: Anteil der Nennkapazität und Anteil des
Nennwärmeeinsatzes über Heizwassereintritt, Kühlwassereintritt, Kaltwassersollwert und Teillast; die mitgelieferte Datei
deckt nur einen engen Heizwasserbereich (etwa 109–116 °C) ab [M4]. Nutzer berichten, dass eigene Dateien den ganzen
Raster aus Herstellerdaten brauchen und die Normierung bei Absorbern schlecht trägt; als Alternative wird ein
Gemeinschaftsmodell auf der charakteristischen Gleichung genannt (Albers).

### 4.4 DOE-2

DOE-2 rechnet Absorber (ein-, zweistufig, gasbefeuert) wie seine Verdichtermaschinen mit Kapazitäts- und
Wärmeeinsatzkurven über Temperatur und Teillast; EnergyPlus' BLAST- und Indirect-Modell stehen in dieser Linie (eigene
Kenntnis, nicht am DOE-2-Text geprüft).

### 4.5 Bewertung für EPOS-Plan

| Kriterium | Charakteristische Gleichung | Kurvenprodukt (EnergyPlus Indirect) | Normierte Tabelle (Type 107) | Tabellenkennfeld wie KM (3D) |
|---|---|---|---|---|
| Eingabe aus Datenblatt | **Nennpunkt + 2–5 Korrekturpunkte** | sechs Kurven à 3–4 Koeffizienten — Datenblätter liefern sie nicht | voller Raster, liefert kein Datenblatt | voller Raster (3 Achsen), selten verfügbar |
| Robust bei wenigen Stützstellen | **ja** (6 Parameter, physikalisch gestützt, lineare Extrapolation) | nein (kubische Kurven schwingen außerhalb) | nein | nein (Rand = Randwert) |
| Antriebstemperatur als dritte Dimension | **in der Gleichung** | als Faktor | als Achse | als Achse |
| Teillast | **aus der Gleichung** (Regelung über Antriebstemperatur), Takten darunter | eigene Kurve | eigene Spalte | eigene Kurve |
| Typkennfeld als Rückfall | **a, e je Bauart; s, r normiert auf Nennpunkt** | fehlende freie Kurven | normierte Datei, schlecht übertragbar | — |
| Ein- und zweistufig, Adsorption | ja (Parameter); Adsorption empirisch, Validierung nötig | ja | nur einstufig | ja |
| Rechenaufwand je Stunde | zwei Geradengleichungen | sechs Polynome | Interpolation 4D | Interpolation 3D |

**Empfehlung (SOK-Q7):** **ein** Rechenweg, die **charakteristische Gleichung** nach Kühn/Ziegler, mit Gerätegrenze
(größte Kälteleistung), Einsatzgrenzen, Takten unter der Mindestteillast und Anfahrverlust. Tabellen und Kurvensätze sind
**Eingabewege**, keine Rechenwege: Der Import passt die sechs Parameter an Datenblattpunkte, eine Leistungstabelle oder
ein Kurvenprodukt an (multiple Regression nach [L3]) und schreibt Anpassungsgüte und Herkunft ins Protokoll — das Muster
von KM1, das Copper-Kurven in das Kennfeld der Kältemaschine überführt, hier in umgekehrter Richtung.

---

## 5. Physik einer Stunde

### 5.1 Größen der Stunde

| Größe | Einheit | Herkunft |
|---|---|---|
| Q₀ Kälte | kWh | Kältebedarf der Kältesenke (Raumkühlung, Prozesskälte), begrenzt durch die Gerätegrenze |
| Q_H Antriebswärme | kWh | aus der Gleichung; gedeckt durch Träger (extern) oder Wärmekaskade (gekoppelt) |
| Q_C Rückkühlwärme | kWh | Bilanz 5.4 |
| P_int Hilfsstrom intern | kWh | 5.5 |
| t_H Heizwassereintritt | °C | extern: Vorgabe der Anlage; gekoppelt: Vorlauf der deckenden Quelle (5.2) |
| t_K Kühlwassereintritt | °C | Rückkühlwerk K-F bzw. Rückkühlart (Festwerte) |
| t_0 Kaltwasseraustritt | °C | Kühlvorlauf der Anlage (`Kuehl_Vorlauf`), Kühlkurve bzw. Temperaturpaar der Prozesskälte |

### 5.2 Kälteleistung und Antriebswärme

Gleichung 4.1 mit den **mittleren** Temperaturen der drei äußeren Kreise. Festlegung für EPOS-Plan, damit die Stunde
explizit bleibt: Die Mitteltemperaturen werden aus den Eintrittstemperaturen (Heiz- und Kühlwasser) bzw. dem
Austritt (Kaltwasser) und den **Nennspreizungen** des Datenblatts gebildet,

```
t̄_H  = t_H − Δt_H,N / 2          t̄_AC = t_K + Δt_K,N / 2          t̄_E = t_0 + Δt_0,N / 2
```

— keine Iteration über die lastabhängige Spreizung (Fehler klein gegen die Datenlage; Proben in SOK-1).

**Gerätegrenze:** Q̇₀,max(h) = min(s_E · ΔΔt'(h) + r_E ; f_max · Q̇₀,N) mit f_max als Überlastgrenze des Geräts (Vorgabe
1,0; Datenblattwert zulässig). **Mindestantrieb:** t_H < t_H,min oder Q̇₀,max ≤ 0 → keine Kälte in dieser Stunde (Grund
`ANTRIEB`, gezählt).

### 5.3 Wärmeverhältnis

ζ(h) = Q₀(h) / Q_H(h); im Jahr ζ̄ = ΣQ₀ / ΣQ_H. ζ fällt mit steigender Kühlwassertemperatur nur schwach, die
**Kälteleistung** dagegen stark (Beispiel 5.9: 30 → 35 °C mittleres Kühlwasser kostet rund 37 % Kälteleistung bei
fast gleichem ζ). Deshalb ist die Rückkühlung die empfindlichste Stelle des Modells.

### 5.4 Rückkühlwärme

Energiebilanz des Geräts (stationär, Wärmeverluste an die Umgebung vernachlässigt bzw. in r_G enthalten):

```
Q_C = Q₀ + Q_H + P_int                                   EnergyPlus Engineering Reference [M2]
Q_C / Q₀ = 1 + 1/ζ + P_int/Q₀   ≈ 2,3 bei ζ = 0,78        elektrische Kältemaschine: 1 + 1/EER ≈ 1,25 bei EER 4
```

Das Rückkühlwerk muss also **etwa das Doppelte** der Abwärme einer Verdichtermaschine gleicher Kälteleistung abführen.
In EPOS-Plan geht Q_C als Abwärme der Stunde an das Rückkühlwerk (K-F2, lastabhängige Annäherung, Ventilatorstrom,
Wasser); die Rückkühltemperatur folgt daraus in **genau einem Schritt** wie bei der Kältemaschine (Zirkularität:
Rückkühltemperatur ↔ Kälteleistung).

### 5.5 Hilfsstrom

```
P_int = p_int · τ_Lauf                   p_int  interne Aufnahme laut Datenblatt [kW], τ_Lauf Laufanteil der Stunde
```

Vorgabe: konstant während des Laufs (die Lösungspumpe läuft mit fester Drehzahl), wie `EIR(PLR) = 1` in der
EnergyPlus-Testdatei [D3]. Externe Pumpen bleiben der Hilfsstromanteil der Anlage (`Kuehl_Hilfsstromanteil`), der
Ventilatorstrom bleibt beim Rückkühlwerk. Gebucht wird der Hilfsstrom auf den Stromträger der Anlage
(`Kuehl_ID_Carrier`, `Kuehl_EigenerZaehler`).

### 5.6 Teillast und Mindestantrieb

- **Über der Mindestteillast** regelt die Maschine über die Antriebstemperatur: ΔΔt'_T = (Q̇₀,Bedarf − r_E) / s_E,
  Q̇_H = s_G · ΔΔt'_T + r_G (4.1). ζ sinkt mit der Last, weil r_G nicht mit der Last fällt.
- **Unter der Mindestteillast** x_min (Feld `Mindestteillast_Prozent`, Vorgabe aus dem Typkennfeld) taktet sie: Laufanteil
  τ = Q₀,Bedarf / (x_min · Q̇₀,max), im Lauf gilt der Punkt bei x_min; je Start fällt ein **Anfahrverlust** an
  Antriebswärme an (5.7). Das ist das Muster der Kesselkennlinie
  ([Konzept Kessel](../../ueberholt/Konzept_Kessel_Kennlinie_EPOS-Plan.md): Starts im Takt höchstens 60 / Mindestlaufzeit, jeder
  Start kostet den Anfahrverlust).

### 5.7 Trägheit und Anfahren

Absorber brauchen nach dem Einschalten Minuten bis zu einer halben Stunde bis zur vollen Leistung (Aufheizen der Lösung
und der Wärmeübertragermassen), beim Abschalten einen Verdünnungslauf gegen Kristallisation; Adsorber arbeiten in
Zyklen von etwa 5–7 Minuten mit Wärmerückgewinnung zwischen den Kammern [L12]. Für ein Stundenraster genügt:

```
Q_H,Start = n_Start · E_Anfahr            n_Start aus dem Takt (5.6), E_Anfahr Anfahrverlust je Start [kWh]
```

Der Zyklus der Adsorption bleibt innerhalb der Stunde gemittelt (stationäre Kennlinie). Vorgaben für E_Anfahr und die
Mindestlaufzeit werden in SOK-1 mit Quelle festgelegt; bis dahin 0 (Start gezählt, ohne Verlust — wie der Kessel mit
gepflegter 0).

### 5.8 Kristallisation und Einsatzgrenzen

H₂O/LiBr kristallisiert, wenn die starke Lösung zu konzentriert und zu kalt wird — typisch bei hoher Antriebstemperatur
zusammen mit **plötzlich kaltem Kühlwasser** oder bei Abschalten ohne Verdünnungslauf; Hersteller begrenzen deshalb die
Kühlwassertemperatur nach unten (Bypass am Rückkühlwerk) und die Heizwassertemperatur nach oben, Grenzwerte sind
gerätespezifisch [H5]. Im Stundenmodell wird das **keine Physik, sondern eine Grenze**:

```
t_K,wirk = max(t_K ; t_K,min)       das Rückkühlwerk hält das Kühlwasser über der Mindesttemperatur (Bypass)
t_H,wirk = min(t_H ; t_H,max)       die Maschine mischt den Antrieb auf die Höchsttemperatur herunter
t_K > t_K,max  → keine Kälte (Grund KUEHLWASSER)      t_H < t_H,min → keine Kälte (Grund ANTRIEB)
t_0 < Kaltwasser_Vorlauf_Min → Grund KALTWASSER (Wasser als Kältemittel: kein Kaltwasser nahe 0 °C)
```

Jede Grenzstunde wird gezählt und als Hinweis gemeldet (Muster `HUB` der KWN). Freie Kühlung über das Rückkühlwerk
der Sorptionsanlage (Übertrager am Kühlturm vorbei an der Maschine) ist physikalisch möglich, in der ersten Stufe aber
nicht vorgesehen (benannt); sie bleibt der elektrischen Kältemaschine vorbehalten.

### 5.9 Rechenbeispiel

Parameter des 10-kW-Forschungsgeräts nach Kühn/Ziegler [L2] (a = 2,5; e = 1,8; s_E = 0,42 kW/K; r_E = 0,9 kW;
s_G = 0,51 kW/K; r_G = 2 kW), eigene Rechnung, ohne Gerätegrenze:

| t̄_H / t̄_AC / t̄_E [°C] | ΔΔt' [K] | Q̇₀ [kW] | Q̇_H [kW] | ζ | Q̇_C / Q̇₀ |
|---|---|---|---|---|---|
| 80 / 30 / 15 | 32,0 | 14,3 | 18,3 | 0,78 | 2,28 |
| 80 / **35** / 15 | 19,5 | 9,1 | 12,0 | 0,76 | 2,31 |
| **70** / 30 / 15 | 22,0 | 10,1 | 13,2 | 0,77 | 2,30 |
| 80 / 30 / **9** | 21,2 | 9,8 | 12,8 | 0,77 | 2,31 |

Teillast bei 80 / 30 / 15 °C über die Antriebstemperatur: 75 % → ζ 0,77; 50 % → 0,75; 30 % → 0,70; 20 % → 0,65;
10 % → 0,54. Daraus folgen zwei Aussagen für den Bericht: Die Kälteleistung hängt viel stärker an Kühlwasser und
Kaltwasser als ζ, und unter etwa 20–30 % Last lohnt Takten statt Drosseln.

---

## 6. Einordnung in die Simulation

### 6.1 Ein Gerät, zwei Seiten

Die Sorptionsmaschine ist **Wärmesenke** (Antrieb) und **Kälteerzeuger** zugleich — das Spiegelbild der KWN, in der eine
Kältemaschine Kälte und Wärme **liefert**. EPOS-Plan rechnet die Wärmekaskade über das ganze Jahr **vor** der
Kältekaskade (Kühlkonzept 5.6.1). Daraus folgen zwei Antriebswege, die sich in Aufwand und Nutzen klar trennen:

| Weg | Antriebswärme kommt | Platz | Bedarf an Kopplung | typischer Fall |
|---|---|---|---|---|
| **EXTERN** | als Endenergie eines Trägers: Fernwärme, Abwärme mit Preis, später Gas (direkt befeuert) | **in der Kältefolge** an ihrem Rang (`Kaelte_Rang`), wie eine Kältemaschine | keiner — die Wärmekaskade bleibt unberührt | Absorber an der Fernwärme im Sommer, Abwärme eines Betriebs |
| **GEKOPPELT** | von Erzeugern des Projekts über die Wärmekaskade: BHKW, Solarthermie, (Kessel), später Puffer | **Vorstufe** vor beiden Kaskaden, Antrieb als Senke in der Wärmekaskade, **Nachstufe** vor der Kältekaskade | Senke, Buchung, Probe | KWKK mit BHKW, solare Kühlung |

### 6.2 Weg EXTERN

Die Maschine steht in der Kältefolge wie eine Kältemaschine: Sie deckt an ihrem Rang den Restbedarf bis zu ihrer
Gerätegrenze (5.2), lädt wie jeder Kälteerzeuger den Kältespeicher (Kühlkonzept 4.6), gibt Q_C an ihr Rückkühlwerk und
bucht Q_H auf den **Träger der Antriebswärme** (`Antrieb_ID_Carrier`, Arbeitspreis, Emissionsfaktor des Trägers) mit
optionaler Anschlussleistung (`Antrieb_Leistung_Max_kW`, leer = unbegrenzt). t_H ist die Vorgabe der Anlage
(`Antrieb_Vorlauf_C`), auf Wunsch später als Monatsreihe (Sommervorlauf der Fernwärme). Die Deckungswelten bleiben
getrennt: Die Antriebswärme ist Endenergie wie der Strom einer Kältemaschine, kein Wärmekanal.

### 6.3 Weg GEKOPPELT — Ablauf des Laufs

1. **Bedarf** in Pass 1 samt Zonensperre; Abzug der dezentralen Kälte (Split).
2. **Kopplungsvorstufe KWN** (Kühlkonzept 5.6.6), falls ein Kopplungsgerät da ist.
3. **Sorptionsvorstufe** über alle 8 760 Stunden, je gekoppelter Sorptionsanlage: geplante Kälte
   Q₀,plan = min(Restkälte der gebundenen Kältesenke ; Q̇₀,max(t_H,plan, t_K, t_0)), unter der Freigabe (6.5); daraus
   der **Antriebsbedarf** Q_H,plan nach 5.6 samt Takt und Anfahrverlust; t_K aus dem Rückkühlwerk mit der Abwärme beim
   Plan in einem Schritt. t_H,plan ist die Vorgabe der Anlage, höchstens der Vorlauf der gebundenen Quellen nach ihren
   Gerätedaten (BHKW und Kessel: `Vorlauf`; Kollektorfeld: Arbeitstemperatur nach `Arbeitstemperatur_Weg`). Reine
   Funktion von Vektoren, ohne Speicherzustand.
4. **Wärmekaskade** wie heute, dazu je Sorptionsanlage eine **Antriebssenke** mit dem Stundenbedarf Q_H,plan. Bedient
   wird sie nur von den Quellen, deren Senkenliste (`Z_AnlageSenke`) eine Zeile mit Ziel `Sorptionsantrieb` und dem
   **Verweis auf diese Sorptionsanlage** trägt, in deren Senkenrang. Ergebnis je Stunde: gedeckte Antriebswärme
   Q_H,ist ≤ Q_H,plan und die Vorlauftemperatur der deckenden Quellen (mengengewichtet).
5. **Sorptionsnachstufe:** tatsächliche Kälte aus der gedeckten Antriebswärme — Umkehrung von 4.1
   (ΔΔt' = (Q̇_H,ist − r_G)/s_G, Q̇₀ = s_E · ΔΔt' + r_E), mit der tatsächlichen Antriebstemperatur in **genau einem
   Schritt**. Liegt sie unter t_H,plan (Kollektorfeld kühler als geplant), nimmt die Maschine höchstens
   Q̇_H,max(t_H,ist) auf; der Rest ist **nicht verwertbare Antriebswärme** — als Verlust der Stunde gebucht, gezählt
   und gemeldet, nie nachträglich an die Wärmekaskade zurückgegeben. Dann Rückkühlbilanz, Hilfsstrom, Ergebnisreihen.
   Die Restkälte (Bedarf − Q₀,ist) geht an die Kältekaskade.
6. **Kältekaskade** wie heute mit der Restkälte: freie Kühlung, Kältespeicher, Wärmepumpen, Kältemaschinen.

Keine Iteration, keine Rückwirkung von der Kälte- auf die Wärmeseite derselben Stunde — die Reihenfolge Wärme vor Kälte
bleibt, wie sie ist. **Reihenfolge der Vorstufen:** KWN vor SOK; Wärme aus einer KWN-Kopplung (35–60 °C) taugt nicht
als Antrieb, Wechselwirkung entsteht nur über den gemeinsamen Restbedarf.

### 6.4 Führung

| Führung | Antriebssenke in der Wärmekaskade | Kessel | Wann |
|---|---|---|---|
| **kältegeführt** (Vorgabe) | wie jede Wärmesenke: Sie darf eine gebundene Quelle **starten** (BHKW läuft mehr, solange sein Senkenrang reicht) | nur wenn ausdrücklich gebunden, mit Hinweis „Antrieb aus Kessel“ | KWKK mit BHKW, Fernwärme-Übergabe als Kessel |
| **wärmegeführt (Mitnahme)** | nur Wärme, die eine gebundene Quelle in der Stunde **ohnehin** bereitstellen kann — eine laufende Quelle bis zu ihrer Leistung, Solarertrag immer; kein Start allein für den Antrieb | nicht zulässig (benannt abgelehnt) | „Überschusswärme in Kälte“ — BHKW, das für Heizung und Brauchwasser ohnehin läuft, solare Kühlung |

Der Senkenrang an der Quelle entscheidet über den Vorrang gegenüber Heizung, Brauchwasser und Prozesswärme; Vorgabe beim
Anlegen: Antrieb **hinter** allen bestehenden Senken. Die Mitnahme ist eine neue Eigenschaft der Senke („nicht
startauslösend“) und deshalb die aufwendigere Führung (SOK-Q3).

### 6.5 Rang, freie Kühlung, Kältespeicher

- **EXTERN** hat einen Platz in der Kältefolge (`Kaelte_Rang`, Vorgabe: Stufe der Kältemaschinen, vor den Verdichtern
  nach Anlagen-ID).
- **GEKOPPELT** hat **keinen** Platz in der Kältefolge (wie die Kopplungsvorstufe KWN und das Rückkühlwerk); sie deckt
  vor ihr. Damit stünde sie auch vor der freien Kühlung — in Stunden, in denen eine Kältemaschine des Projekts frei
  kühlen könnte, wäre das Verschwendung. Vorschlag: Feld **Freigabe ab Außentemperatur** (`Freigabe_Aussen_C`, leer =
  immer) — einfach, deterministisch, vom Planer gesetzt (SOK-Q6).
- **Kältespeicher:** EXTERN lädt ihn wie jeder Kälteerzeuger. GEKOPPELT deckt in der ersten Stufe nur den Bedarf der
  Stunde; ein Ladeangebot an den Kältespeicher (Mittagsüberschuss der Solarthermie in Kälte) folgt dem Muster KWN-5
  (Überschuss als Ladeangebot in die Jahresschleife des Speichers) als Stufe SOK-7.

### 6.6 Pufferspeicher als Antriebspuffer

Ein Puffer zwischen BHKW bzw. Kollektorfeld und Sorptionsmaschine glättet den Antrieb und ist bei Solar praktisch
immer vorhanden. Er braucht eine Nutzung „Sorption“ (`Nutzung_Sorption`, 0/1 mit `CHECK`) und ein Pufferziel; die
Antriebstemperatur folgt dann der oberen Schicht (Muster: Arbeitstemperatur des Solarkreises aus der Pufferzone, 1049).
Erste Stufe ohne Puffer: BHKW und Kollektorfeld decken die Antriebssenke **direkt** (Direktsenke wie `Prozesswaerme`);
der Antriebspuffer kommt mit SOK-7.

### 6.7 Rückkühlung

Sorptionsmaschinen sind wassergekühlt: Rückkühlart `LUFT` wird für sie benannt abgelehnt (Ausnahme später: luftgekühlte
NH₃-Geräte, direkt befeuert). Das Rückkühlwerk (K-F) rechnet mit der Abwärme Q_C statt Kälte + Verdichterstrom; seine
Überlaststunden (Lastanteil > 1) zeigen ein zu kleines Werk. Eine Prüfregel vergleicht die Nennleistung des Werks mit
Q̇₀,N · (1 + 1/ζ_N) und meldet „Rückkühlwerk für Sorption zu klein“ als Hinweis. Die Rückkühlwärme bleibt in der ersten
Stufe ungenutzt (Niedertemperaturnutzung: später, wie KWN-Q9).

### 6.8 Kopplungsstufen AK0–AK3

Auf AK0 bis AK2 entsteht der Kältebedarf in Pass 1 — beide Wege rechnen wie beschrieben. Auf **AK3** entsteht der
Raumbedarf erst in der Stundenkette mit Wärme- und Kälteschranke; die Sorptionsmaschine müsste dort als Verfügbarkeit in
die Kälteschranke und mit ihrem Antrieb in die Wärmeschranke. Vorschlag: auf AK3 in der ersten Stufe nur **Prozesskälte**
als gebundene Kältesenke (wie KWN Stufe 1 auf AK3), Raumkühlung benannt abgelehnt; eigene Welle SOK-8 (Muster KWN-7).

### 6.9 Naht durch die Trennung der Deckungswelten

Die Trennung (Kühlkonzept 4.4) wird nicht aufgehoben. Wie die KWN eine **Kopplungsbuchung** hat, bekommt der Weg
GEKOPPELT eine **Antriebsbuchung** je Anlage und Stunde (Q_H,plan, Q_H,ist je Quelle, t_H, Q₀, Q_C, P_int):

- Die Antriebssenke ist **kein** fünfter Kanal und kein Teil des Kanals `PROZESS`: Sie erscheint nicht im
  Wärmebedarf, in den Bedarfskennzahlen und im Bedarfsbericht, sondern als eigene Position „Antrieb Sorptionskälte“ in
  der Wärmeerzeugung der Quellen (Muster: die Kopplungsbuchung steht in eigenen Reihen, nicht in den Kanalreihen).
- Die **Deckungsprobe Kälte** zählt die Nachstufe als zulässigen Kälteerzeuger außerhalb der Kältekaskade, nur sie.
- Eine **Sorptionsprobe** hält je Stunde Q_H,ist ≤ Q_H,plan, Q_H,ist = Q_H,genutzt + Q_H,nicht verwertbar,
  Q_C = Q₀ + Q_H,genutzt + P_int und τ ≤ 1 (Stufe Fehler, Lauf fehlgeschlagen — dieselbe Schärfe wie die
  Kopplungsprobe).
- **Abwägung zu einem Unterposten der Prozesswärme:** Die Antriebswärme als Unterposten des Kanals `PROZESS` (wie die
  Prozesskälte im Kühlkanal, KWN-0) wäre billiger, bände aber jede Prozesswärmequelle an den Antrieb und mischte die
  Antriebswärme in den Wärmebedarf. Die eigene Senke mit Verweis auf die Sorptionsanlage ist sauberer und trägt die
  Quellenbindung über IDs.

### 6.10 Byte-Gleichheit

Ohne Sorptionsanlage (keine Kindzeile, 7.1) entsteht **kein** Objekt der Vor- und Nachstufe, keine Antriebssenke und
kein neuer Eintrag in der Kältefolge; die Kältemaschine rechnet Zeichen für Zeichen wie heute. Eine bestehende Maschine
mit Geräteart `ABSORPTION` ohne Kindzeile rechnet weiter wie heute (1.2) und bekommt nur den Hinweis. Abnahme jeder Welle
bis zum Einfrieren: Referenzlauf aller Projekte byte-gleich.

---

## 7. Datenmodell (Vorschlag)

Papiernamen **SOK-S1** (Gerät und Katalog) und **SOK-S2** (Anlage, Senke, Ergebnis); die Nummern vergibt jeder Schritt
erst bei seiner Anmeldung vor dem Bau (Zeile „Schemaschritt angemeldet“ der Statusdatei,
[ADR-001](../ADR-001_Schema-Ausrollung.md)). Alle neuen Spalten nullbar, leer heißt Vorgabe bzw. „keine Sorption“;
Boolesche Spalten mit `CHECK (spalte IN (0,1))`, Wertemengen mit `CHECK … IN (…)`, ohne Umlaut.

### 7.1 Gerät: wo die Sorptionsdaten wohnen

| Variante | Für | Gegen |
|---|---|---|
| (a) Spalten an `Tab_Kaeltemaschine(_STAMM)` | ein Katalog, eine Tabelle | 15–20 Spalten, die für Verdichter leer bleiben; die EER-Spalten bekämen eine zweite Bedeutung — genau der Fehler aus 1.2; jeder Leser der Kältemaschine müsste verzweigen |
| (b) eigene Anlagenart und Tabelle `Tab_Sorptionskaeltemaschine(_STAMM)` | saubere Semantik | doppelte Infrastruktur (Katalogdialog, Register, Projektkopie, Kosten, Ergebnis, Kältefolge); Anlagenart 14 ist für Raumklimageräte vorgesehen (Entwurf Split/VRF) |
| **(c) Anlagenart 13 bleibt, Sorptionsdaten in einer 1:1-Kindtabelle** | Katalog, Kältefolge, Rückkühlwerk, Kältespeicher, Kosten und Ergebnis bleiben geteilt; die Sorptionsfelder haben eine eigene, eindeutige Heimat | eine Prüfregel muss den Kopf konsistent halten (EER-Felder leer, Geräteart `ABSORPTION`) |

**Empfehlung (c):** `Tab_Kaeltemaschine_Sorption(_STAMM)` mit `ID_Kaeltemaschine` (`UNIQUE`, `ON DELETE CASCADE`). Die
Geräteart bleibt `ABSORPTION` als Oberbegriff „Sorption“ (Anzeigetext „Sorption (Absorption/Adsorption)“); die Bauart
steht in der Kindtabelle — so braucht `ADSORPTION` keinen Neubau der `STRICT`-Tabelle wegen einer geänderten
`CHECK`-Klausel.

| Spalte | Bedeutung |
|---|---|
| `Sorption_Bauart` | `LIBR_EINSTUFIG`, `LIBR_ZWEISTUFIG`, `NH3_WASSER`, `ADSORPTION_SILIKAGEL`, `ADSORPTION_ZEOLITH` |
| `Antriebsmedium` | `HEIZWASSER` (Vorgabe), `DAMPF`, `ABGAS`, `DIREKT` (die letzten drei benannt, erste Stufe lehnt sie ab) |
| `Nenn_Waermeverhaeltnis` | ζ_N am Nennpunkt [–] (Prüfbereich je Bauart) |
| `Nenn_Antrieb_C`, `Nenn_Antrieb_Spreizung_K` | Heizwassereintritt und Spreizung am Nennpunkt |
| `Nenn_Kuehlwasser_C`, `Nenn_Kuehlwasser_Spreizung_K` | Kühlwassereintritt und Spreizung |
| `Nenn_Kaltwasser_C`, `Nenn_Kaltwasser_Spreizung_K` | Kaltwasseraustritt und Spreizung |
| `Antrieb_Min_C`, `Antrieb_Max_C`, `Kuehlwasser_Min_C`, `Kuehlwasser_Max_C` | Einsatzgrenzen (5.8) |
| `Hilfsstrom_kW` | interne Aufnahme p_int (5.5) |
| `Anfahrverlust_kWh`, `Mindestlaufzeit_min` | Takt (5.6, 5.7); Mindestteillast bleibt `Mindestteillast_Prozent` am Kopf |
| `Kennlinie_a`, `Kennlinie_e`, `Kennlinie_sE_rel`, `Kennlinie_rE_rel`, `Kennlinie_sG_rel`, `Kennlinie_rG_rel` | Parameter der Gleichung, s und r **bezogen auf die Nennkälteleistung** [1/K bzw. –], damit ein Typkennfeld skaliert; leer = aus Typkennfeld der Bauart und Nennpunkt (7.4) |
| `Kennlinie_Herkunft` | `TYPKENNFELD`, `DATENBLATT`, `TABELLE`, `KURVEN` |
| `Kennlinie_Guete` | Bestimmtheitsmaß bzw. größte Abweichung der Anpassung [%] |
| `Brenner_Wirkungsgrad` | nur `DIREKT` (später) |

**Stützstellen** (optional) `Tab_Kenndaten_Sorption(_STAMM)`: `ID_Kaeltemaschine`, `Antrieb_C`, `Kuehlwasser_C`,
`Kaltwasser_C`, `Kaelteleistung_kW`, `Antriebswaerme_kW`. Der Rechenweg liest nur die Parameter; die Stützstellen sind
Herkunft und Anzeige (Dialog: Punkte gegen Gerade). Der Controller passt beim Speichern an (8.2).

**Abwägung „dritte Kennfelddimension oder Parameter“:** Eine dritte Achse in `Tab_Kenndaten_Kaeltemaschine` wäre
formal klein, verlangte aber einen vollen 3D-Raster (den kein Datenblatt liefert) und würde am Rand konstant statt
linear fortgesetzt. Die Parameter der Gleichung sind die bessere Speicherform; die Stützstellen bleiben daneben.

### 7.2 Anlage

An `Tab_Energieanlagen` (Zeile der Anlagenart 13 mit Sorptionsgerät):

| Spalte | Bedeutung |
|---|---|
| `Antrieb_Weg` | `EXTERN`, `GEKOPPELT`; leer = keine Sorption (Pflicht, sobald das Gerät eine Kindzeile hat) |
| `Antrieb_ID_Carrier` | Träger der Antriebswärme bei `EXTERN` (Verweis auf `energy_carrier`, z. B. Fernwärme) |
| `Antrieb_Vorlauf_C` | Vorgabe Heizwassereintritt; leer = Nennpunkt |
| `Antrieb_Leistung_Max_kW` | Anschlussleistung bei `EXTERN`; leer = unbegrenzt |
| `Antrieb_Fuehrung` | `KAELTE` (leer), `WAERME` — nur `GEKOPPELT` |
| `Freigabe_Aussen_C` | Sorption nur ab dieser Außentemperatur; leer = immer |
| `Kaelte_Art_Bindung` | `RAUM`, `PROZESS`, leer = beide (welche Kältesenke die Vorstufe bedient; AK3: nur `PROZESS`) |

Rückkühlwerk über den vorhandenen Verweis `ID_Rueckkuehlwerk`; Stromträger des Hilfsstroms über `Kuehl_ID_Carrier`.

### 7.3 Antriebsquelle über IDs

An `Z_AnlageSenke` (Senkenliste der **Quelle**, z. B. des BHKW):

- neuer Zielwert `Sorptionsantrieb` (`DbWerte.WS_ZIEL_SORPTIONSANTRIEB`, Persistenzwert ohne Umlaut, eingefroren);
- neue Spalte `ID_Zielanlage` INTEGER, `REFERENCES Tab_Energieanlagen(ID) ON DELETE CASCADE` — die Sorptionsanlage, die
  diese Zeile bedient; Pflicht bei Ziel `Sorptionsantrieb`, sonst leer.

Die Kette hängt an KWN-1b (`Kopplungsseite` an derselben Tabelle). Der Senkendialog bietet das Ziel nur an Quellen an,
die heizwasserseitig taugen (BHKW, Solarthermie, Kessel mit Hinweis; Wärmepumpe benannt abgelehnt — ihr Vorlauf reicht
nicht und Strom in Wärme in Kälte ist ein Umweg). Später: Pufferziel `PufferSorption` und `Nutzung_Sorption` (6.6).

### 7.4 Typkennfelder

Je Bauart ein **normiertes** Typkennfeld (a, e, s_E/Q̇₀,N, r_E/Q̇₀,N, s_G/Q̇₀,N, r_G/Q̇₀,N) samt Nennpunkt und Grenzen,
eingebettet wie `KaeltemaschinenTypkennfelder` (Ressource, Quelle und Lizenz in der Beschreibung, `ReadOnly = 1`,
Katalogschlüssel und Prüfsumme über die Katalogfassung, Register „KM“). Startsatz:

| Typkennfeld | Grundlage | Leistungsklassen |
|---|---|---|
| Absorption einstufig | Kühn/Ziegler (10 kW) und Puig-Arnavat (4,5 / 15 kW), Satznamen neutralisiert; der große Satz aus [D1] erst nach Klärung des Fehlers (3.3) | 15, 50, 150 kW; Klassen ab 500 kW erst mit einem geprüften großen Satz — ein auf das Hundertfache skaliertes Kleingerät überschätzt die relativen Verluste r_G |
| Absorption zweistufig | Puig-Arnavat (1 163 kW) | 500, 1 500 kW |
| Adsorption | **offen** — kein freier Parametersatz gefunden; Anpassung an ein Datenblatt nötig (SOK-Q4) | — |
| NH₃/H₂O | offen | — |

Aus Nennkälteleistung, ζ_N und Nennpunkt eines Datenblatts entsteht ein **Typkennfeld aus Nennwerten**: a und e der
Bauart, s und r so skaliert, dass der Nennpunkt genau getroffen wird (Muster K-C „Nennwerte allein“).

### 7.5 Ergebnisse

Am Ergebnis der Kältemaschine (`Tab_ErgebnisKaeltemaschine`) die Jahreswerte `Sorption_Kaelte_MWh`,
`Sorption_Antrieb_MWh`, `Sorption_Antrieb_Plan_MWh`, `Sorption_Antrieb_Fehl_MWh`, `Sorption_Antrieb_Verlust_MWh`
(nicht verwertbar, 6.3), `Sorption_Hilfsstrom_MWh`,
`Sorption_Rueckkuehlung_MWh`, `Sorption_Waermeverhaeltnis`, `Sorption_Stunden`, `Sorption_Starts`, die Grenzstunden
(`ANTRIEB`, `KUEHLWASSER`, `KALTWASSER`); die Antriebswärme **je Quelle** in einer Kindtabelle
`Tab_ErgebnisSorptionAntrieb` (`ID_Ergebnis`, `ID_Quelle`, `Waerme_MWh`, `Vorlauf_Mittel_C`). Stundenreihen nur, wenn eine
Sorptionsanlage rechnet (Muster `Kaeltebedarf_Prozess`: Datei und Schlüssel nur bei Wert > 0).

**Schemabedarf grob:** SOK-S1 eine Kindtabelle mit `_STAMM`, eine optionale Stützstellentabelle mit `_STAMM`, Register
in der Katalogfassung; SOK-S2 sieben Spalten an `Tab_Energieanlagen`, eine an `Z_AnlageSenke`, ein Zielwert, rund
zwölf Ergebnisspalten und eine Ergebnis-Kindtabelle. Kein Neubau einer bestehenden Tabelle.

---

## 8. Daten und Import

### 8.1 Was der Anwender eingibt

**Pflicht (Datenblatt, Nennpunkt):** Bauart, Nennkälteleistung, Wärmeverhältnis (oder Antriebswärme), Heiz-, Kühl- und
Kaltwassertemperatur samt Spreizung, Rückkühlwärme (nur zur Bilanzprobe), Hilfsstrom, Einsatzgrenzen (Heizwasser
min./max., Kühlwasser min./max.), Mindestteillast. **An der Anlage:** Antriebsweg, Träger oder Quellen, Antriebsvorlauf,
Führung, Freigabe, Rückkühlwerk.

**Empfohlen:** zwei bis fünf Punkte aus den Leistungskorrekturdiagrammen (Kälteleistung und ζ bei anderer Heiz- bzw.
Kühlwassertemperatur) — sie machen aus dem Typkennfeld ein Gerätekennfeld.

### 8.2 Was aus dem Typkennfeld kommt

Ohne Korrekturpunkte: a und e der Bauart, s und r aus Nennpunkt und ζ_N (7.4), Vorgaben für Grenzen, Anfahrverlust und
Mindestlaufzeit. Mit Korrekturpunkten: Anpassung aller sechs Parameter nach der Methode der kleinsten Quadrate
([L3]; Kälte- und Wärmegleichung getrennt, a und e gemeinsam), Rückfall auf a und e der Bauart, wenn die Punkte sie
nicht bestimmen (alle bei derselben Kaltwassertemperatur → e bleibt Vorgabe). Anpassungsgüte und Herkunft gehen ins
Leseprotokoll und in `Kennlinie_Guete`; eine Abweichung über 5 % am Nennpunkt ist ein Fehler der Eingabe.

### 8.3 Import

Ein Import lohnt — als **Variante der vorhandenen CSV-Vorlage** (K-C), nicht als neues Format:

- Kopfschlüssel `Form;Sorption` mit den Nennfeldern aus 8.1 und Zeilen `Punkt;Antrieb_C;Kuehlwasser_C;Kaltwasser_C;Kaelteleistung_kW;Antriebswaerme_kW`
  (beliebig viele, auch eine Tabelle aus dem Auslegungsprogramm des Herstellers);
- Prüfungen: Bilanz Rückkühlung, ζ im Band der Bauart, Grenzen geordnet, Nennpunkt unter den Punkten oder getroffen;
- Vorlage `Quellen/Kaeltemaschine_Sorptionsvorlage.csv` neben den drei vorhandenen Vorlagen;
- Kurvenimport aus EnergyPlus-IDF (`Chiller:Absorption:Indirect`) nur als spätere Option — die Kurven werden an einem
  Raster ausgewertet und angepasst wie eine Tabelle;
- Plattform wie die Kältemaschine: Import im Katalogdialog unter Windows, iOS lehnt benannt ab und bietet die
  Typkennfelder an.

---

## 9. Ergebnisse, Kennzahlen, Bericht, Wirtschaftlichkeit

### 9.1 Ergebnisse und Kennzahlen

| Größe | Einheit | Bildung |
|---|---|---|
| Kälte aus Sorption | MWh/a | ΣQ₀ je Anlage, Anteil an der Kältedeckung |
| Antriebswärme je Quelle | MWh/a | ΣQ_H,ist je Quelle (BHKW, Solar, Kessel, Träger) |
| Fehlende Antriebswärme | MWh/a | Σ(Q_H,plan − Q_H,ist); Hinweis bei > 5 % |
| Wärmeverhältnis (Jahr) | – | ΣQ₀ / ΣQ_H |
| Hilfsstrom | MWh/a | ΣP_int; dazu Ventilatorstrom des Rückkühlwerks |
| **Elektrische Arbeitszahl der Sorptionskälte** | – | ΣQ₀ / (ΣP_int + Ventilator- und Pumpenstrom) — der Vergleich zur Jahresarbeitszahl Kälte (Kühlkonzept 6.4) |
| Rückkühlwärme, Wasserverbrauch | MWh/a, m³/a | ΣQ_C; Wasser aus K-F |
| Laufstunden, Starts, Grenzstunden | h/a, – | 5.6–5.8 |
| **BHKW-Mehrlaufzeit durch Kälte** | h/a | Vollbenutzungsstunden des BHKW mit und ohne Antriebssenke (Variantenvergleich) |

Die Kennzahlen stehen in der Gruppe der Kälte (`GR_KAELTE`, Kühlkonzept 6.4) als eigene Untergruppe „Sorptionskälte“.
Der Bericht führt je Sorptionsanlage eine Tafel (Kälte, Antriebswärme je Quelle, ζ, Hilfsstrom, Rückkühlung, Stunden)
und im Deckungsbild der Kälte eine Fläche „aus Sorption“, im Deckungsbild der Wärme die Position „Antrieb
Sorptionskälte“; das Energieflussbild (SK-1 bis SK-4) bekommt einen Pfad Quelle → Sorption → Kälte und Sorption →
Rückkühlung.

### 9.2 Primärenergie und Emissionen

Ein eigener Faktor entsteht nicht. **EXTERN:** Emissionen und Primärenergie über den Träger der Antriebswärme (Faktoren
des Trägers, z. B. Fernwärme) und den Stromträger des Hilfsstroms. **GEKOPPELT:** Die Antriebswärme ist Wärme der Quelle;
ihr Brennstoff steht schon in der Bilanz des BHKW bzw. Kessels, Solarwärme ist emissionsfrei. Der KWK-Anteil folgt der
Bilanzierungskonvention des Projekts (`BilanzKonvention`, L12/L13) — dieselbe Regel wie für jede BHKW-Wärme. Eine
Kennzahl „CO₂ je kWh Sorptionskälte“ ist Auswertung (mit der Wärmeaufteilung des Projekts), keine Bilanzregel; die
Ersparnis gegenüber Kompressionskälte zeigt der **Variantenvergleich** (Muster KWN-Q4: keine Gutschrift in der Bilanz).

### 9.3 Wirtschaftlichkeit

- **Investition:** Kostenkomponente 11 „Kältemaschine“ mit eigener Kostenvorlage „Sorptionskältemaschine“; Rückkühlwerk
  über seine Vorlage (K-F, deutlich größer); Einbindung der Antriebsseite (Übertrager, Mischventil) als Position der
  Vorlage. Nutzungsdauer und Instandhaltung nach VDI 2067 Blatt 1 (Zeile am Wortlaut prüfen).
- **Betrieb:** Antriebswärme EXTERN über den Arbeitspreis des Trägers (Fernwärme, Abwärme mit Preis 0 oder
  Verrechnungspreis); GEKOPPELT ohne eigenen Preis (der Brennstoff steht beim BHKW); Hilfsstrom, Ventilatorstrom,
  Wasser und Abwasser des Rückkühlwerks; Wartung.
- **Kostenteilung** zwischen Wärme und Kälte eines BHKW nur als Auswertung (spezifische Kältekosten), nicht in der
  Bilanz; der Mehrerlös aus KWK-Strom durch mehr Laufzeit erscheint in der BHKW-Rechnung von selbst.

### 9.4 Förderung — nur Prüfpunkte, kein Rechtsrat

| Prüfpunkt | Was zu prüfen ist |
|---|---|
| KWKG [F2] | Begriff Kraft-Wärme-Kälte-Kopplung (§ 2), Zuschläge für Kältenetze und Kältespeicher, in die KWKK-Kälte eingespeist wird (Paragrafen je Fassung verschieden), Anrechnung der Antriebswärme als KWK-Nutzwärme für den KWK-Strom; geltende Fassung und Fristen |
| Kälte-Klima-Richtlinie (BAFA) [F1] | ob Sorptionskälteanlagen in der Fassung ab 01.03.2024 förderfähig sind (die Fassung 2014 nannte sie mit KWK- oder Abwärmeantrieb), Leistungsgrenzen, Laufzeit bis 31.12.2026 |
| BEG / EEW | ob Sorptionskälte in Gebäude- bzw. Wirtschaftsförderung (Prozesskälte, Abwärmenutzung) erfasst ist |
| Fernwärmeversorger | Sommertarife oder Kälteprodukte, Rücklaufbedingungen (Netzfrage, kein Rechenweg) |

EPOS-Plan rechnet keine Förderung in die Kälte ein; der Bericht kann die Prüfpunkte als Hinweis nennen.

---

## 10. Byte-Gleichheit und Referenzprojekte

**Ohne Kindzeile in `Tab_Kaeltemaschine_Sorption` und ohne `Antrieb_Weg`** rechnet jedes Projekt wie heute (6.10);
das ist die Abnahme jeder Welle bis SOK-6.

**Neue Referenzprojekte**, Nummer jeweils die nächste freie:

| Fall | Kopie von | Änderung | Wache | CI |
|---|---|---|---|---|
| **gekoppelt, BHKW** | 1055 (BHKW, Kältemaschine mit Trocken-Rückkühler, Kältespeicher, Kühlbetrieb) | eine Sorptionsanlage (Typkennfeld einstufig, Nennpunkt aus Datenblattmuster) vor der vorhandenen Kältemaschine, Antrieb aus dem BHKW kältegeführt (Senkenzeile `Sorptionsantrieb` hinter den bestehenden), Rückkühlwerk nass bzw. hybrid, Freigabe ab 18 °C | `SorptionGekoppeltReferenzprojektWacheTests` | **ja** (Hauptweg) |
| **extern, Fernwärme** | 1055 | Sorptionsanlage mit Antrieb über den Träger Fernwärme, Anschlussleistung begrenzt, Rang vor der Kältemaschine | `SorptionExternReferenzprojektWacheTests` | nein |
| *solar (spätere Welle)* | 1049 (Kollektorfeld vor BHKW und Kessel) | Kühlbetrieb und Sorptionsanlage mit Antrieb aus dem Kollektorfeld, Mitnahme | — | — |

**Einfrierregel (Entwurf, für `CLAUDE.md`):** gesäte Sorptionsdaten eines Referenzprojekts: die Kindzeile seiner
Sorptionsmaschine in `Tab_Kaeltemaschine_Sorption` (jede Spalte) samt Stützstellen, Geräteart und Nennkälteleistung am
Kopf, an der Anlagenzeile `Antrieb_Weg`, `Antrieb_ID_Carrier`, `Antrieb_Vorlauf_C`, `Antrieb_Leistung_Max_kW`,
`Antrieb_Fuehrung`, `Freigabe_Aussen_C`, `Kaelte_Art_Bindung`, `Kaelte_Rang` und `ID_Rueckkuehlwerk`, die Senkenzeilen
`Sorptionsantrieb` (Rang, `ID_Zielanlage`) seiner Quellen, Vorlauf und Rücklauf dieser Quellen, die Faktoren und Preise
des Antriebsträgers, das Rückkühlwerk der Sorptionsanlage, die eingebauten Typkennfelder der Sorption und die Festwerte
des Rechenwegs (Spreizungsregel, Vorgaben für Grenzen, Anfahrverlust und Mindestlaufzeit), dazu das Anlegen oder
Entfernen eines Referenzprojekts mit Sorptionskälte. Begründung jedes Wechsels in
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md).

---

## 11. Stufenplan

Kurzzeichen **SOK**; vor der Wahl in [`Status_iOS_Migration.md`](../Status_iOS_Migration.md) geprüft: „SK“ ist das
Energieflussbild (#1162–#1165), „SOK“ kommt nirgends vor.

| Welle | Inhalt | Aufwand | Abnahme | hängt an |
|---|---|---|---|---|
| **SOK-0** | Konzeptentscheide (Fragen 12); Papierpflege: K-G in der Konzeptprüfung als „abgelöst durch SOK“, Entscheid im Status; auf Wunsch der Hinweis „Geräteart Absorption rechnet als elektrische Kältemaschine“ in der Konfigurationsprüfung (ohne Zahlenwirkung) | 0,5–1 PT | `DokumentationLinkWacheTests`; mit Hinweis: Kern-Filter grün, Referenzlauf byte-gleich | — |
| **SOK-1** | Reine Rechenklasse `Sorptionskaeltemaschine` (`EPOS.Kern/Allgemein/Simulation/Kaelte/`): Gleichung, Mitteltemperaturen aus Nennspreizungen, Gerätegrenze, Einsatzgrenzen mit Gründen, Teillast über Antriebstemperatur, Takten mit Anfahrverlust, Umkehrung Antrieb → Kälte, Rückkühlbilanz, Hilfsstrom; Anpassung der Parameter an Punkte; Typkennfeld aus Nennwerten; Festwerte in `KaelteFestwerte` | 3–4 PT | 25–30 Rechenproben ohne Datenbank: Gegenprobe der Werte aus 5.9, Bilanz je Stunde, Umkehrung (Kälte → Antrieb → Kälte identisch), Grenzen an beiden Seiten, Takt und Starts, Anpassung trifft erzeugte Punkte, fehlerhafter Satz (ζ ≡ 1) wird abgewiesen; Kern-Filter grün | — |
| **SOK-2** | Schema SOK-S1 und SOK-S2 (je ein Schritt, Anmeldung vor dem Bau), Modelle, Controller mit Prüfregeln (Kopf konsistent, ζ-Band, Grenzen, Rückkühlart, Weg und Quellen), Projektkopie und Übernahme aus dem Katalog, Register in der Katalogfassung, Testdatenbank gehoben | 3–4 PT | Schematests, `SqlDialektPruefer`, Rundreise Katalog → Projekt, Prüfregeln; Referenzlauf byte-gleich | KWN-1b gemergt (`Z_AnlageSenke`) |
| **SOK-3a** | Einbau **EXTERN**: Sorptionsmaschine in der Kältefolge an ihrem Rang, Träger der Antriebswärme in Kosten und Emissionen, Rückkühlwerk mit Q_C, Hilfsstrom, Ergebnisspalten, Deckungsprobe | 3–4 PT | Lauftests (Rang, Kältespeicher wird geladen, Anschlussleistung greift), Deckungsprobe ohne Fehler; Referenzlauf byte-gleich | SOK-1, SOK-2; K-F (gebaut) |
| **SOK-3b** | Einbau **GEKOPPELT**: Sorptionsvorstufe, Antriebssenke in der Wärmekaskade (kältegeführt), Nachstufe, Antriebsbuchung, Sorptionsprobe, Ergebnis je Quelle; Freigabe; AK3 nur Prozesskälte | 6–9 PT | Rechen- und Lauftests (Antrieb voll, Antrieb fehlt, Antrieb zu kalt, Freigabe, BHKW-Mehrlaufzeit), Sorptions- und Deckungsprobe ohne Fehler; Referenzlauf byte-gleich | SOK-3a; **KWN-1c gemergt** (Ort der Vorstufe, Muster Buchung und Probe) |
| **SOK-4** | Typkennfelder (einstufig, zweistufig; neutral, eingebettet, Register), CSV-Variante „Sorption“ mit Anpassung und Leseprotokoll, Vorlage unter `Quellen/`, Menüpunkt im Import | 3–4 PT | Importtests (Nennwerte allein, Punkte, Tabelle), Typkennfeld-Wache (Quelle, Lizenz, keine Firma), `WikiProduktdatenWacheTests`; Referenzlauf byte-gleich | SOK-2 |
| **SOK-5** | Oberfläche: Gruppe „Sorption“ im Katalog- und Erzeugerdialog (Bauart, Nennpunkt, Grenzen, Punkte gegen Gerade), Antriebsweg und Quellen in der Senkenwahl, Kälte-Bereich; Bericht (Tafel, Deckungsbilder, Energieflussbild), Kennzahlen, Ressourcen in beiden Sprachen, Wiki-Quellen, Logbuch-Entwurf | 4–6 PT | bunit der Dialoge; Schale auf Linux gebaut (0 Fehler); `designer_neu.py`; `BerichtSchreiberOhneDatenbankWacheTests`; ChartProben; Wiki-Gegenlesen | SOK-3b, SOK-4 |
| **SOK-6** | Referenzprojekte gekoppelt (CI) und extern, Wachen, Einfrierregel in `CLAUDE.md`, neue Basis | 2–3 PT | Wachen grün; Referenzlauf aller Projekte gegen die neue Basis, Begründung in `Referenzlaeufe/LIESMICH.md` | SOK-3b, SOK-4 |
| *SOK-7* | Antriebspuffer (`Nutzung_Sorption`, Pufferziel) und Ladeangebot an den Kältespeicher; Führung wärmegeführt (Mitnahme) | 3–5 PT | Speicherbilanz-Proben; Referenzlauf | SOK-6; Muster KWN-5 |
| *SOK-8* | AK3: Sorption als Verfügbarkeit in der Kälteschranke, Antrieb in der Wärmeschranke | 3–5 PT | Komfortkennzahlen; 1058/1059 byte-gleich | SOK-6; Muster KWN-7 |
| *SOK-9* | weitere Bauarten: Adsorption (nach Validierung an einem Datenblatt), NH₃/H₂O mit Sole, direkt befeuert (Brennstoffträger, GUE), Dampf | je 2–3 PT | Rechenproben je Bauart; Referenzlauf | SOK-6 |

**Summe SOK-0 bis SOK-6: 25–35 PT**, mit SOK-7 bis SOK-9 rund 35–50 PT. Gegenüber der Schätzung K-G (15–22 PT) kommen
der Weg EXTERN, die Anpassung an Datenblattpunkte und Oberfläche/Bericht hinzu. **Voraussetzungen:** K-F gebaut
(Rückkühlwerk), KWN-1b und KWN-1c auf `ios_migration_september`. **Abgrenzung:** keine Fahrplanoptimierung, keine
Nutzung der Rückkühlwärme, keine Dynamik innerhalb der Stunde; ein iOS-Lauf ist für keine Welle begründet (keine
Änderung an der iOS-Hülle).

---

## 12. Fragen an den Anwender

| Kennung | Frage | Empfehlung |
|---|---|---|
| **SOK-Q1** | K-G (zurückgestellt mit E118) wieder aufnehmen und durch den Stufenplan SOK ersetzen? | **Ja**, Reihenfolge nach KWN-1c; K-G in der Konzeptprüfung als abgelöst vermerken |
| **SOK-Q2** | Umfang der ersten Stufe? | **Einstufig H₂O/LiBr mit Heizwasser**, beide Wege EXTERN (Fernwärme, Abwärme als Träger) und GEKOPPELT (BHKW, Solarthermie direkt); Kessel nur ausdrücklich gebunden mit Hinweis; Datenmodell offen für die übrigen Bauarten |
| **SOK-Q3** | Wärme- oder kältegeführt? | **Kältegeführt als Vorgabe** (SOK-3b); wärmegeführt als Mitnahme mit SOK-7, weil sie eine neue Senkeneigenschaft braucht und erst mit Puffer ihren Nutzen zeigt |
| **SOK-Q4** | Adsorption gleich mit? | **Nein, aber vorbereitet**: Bauart und Parameter im Schema ab SOK-2; Typkennfeld und Freigabe erst nach Gegenrechnung eines Datenblatts (SOK-9) — es gibt keinen freien Parametersatz |
| **SOK-Q5** | Direkt befeuerte Geräte? | **Später** (SOK-9) als EXTERN mit Brennstoffträger und GUE; erste Stufe lehnt `DIREKT` benannt ab |
| **SOK-Q6** | Rang: GEKOPPELT vor der ganzen Kältefolge, mit Freigabe ab Außentemperatur; EXTERN an ihrem `Kaelte_Rang`? | **Ja** — deterministisch und vom Planer steuerbar; die freie Kühlung der Kältemaschinen bleibt über die Freigabe geschützt |
| **SOK-Q7** | Rechenmodell: charakteristische Gleichung als einziger Rechenweg, Tabellen und Kurven nur als Eingabe (Anpassung)? | **Ja** (4.5) |
| **SOK-Q8** | Typkennfelder aus offenen Parametersätzen (Kühn/Ziegler, Puig-Arnavat über oemof-thermal, MIT), neutral benannt, ohne den fehlerhaften Satz? | **Ja**; Lizenz- und Quellvermerk wie bei den Copper-Typkennfeldern |
| **SOK-Q9** | Referenzprojekte als Kopien von 1055, das gekoppelte in der CI-Auswahl? | **Ja** — 1055 trägt BHKW, Kältemaschine, Rückkühler und Kältespeicher; das solare Projekt (Kopie von 1049) folgt mit SOK-7 |
| **SOK-Q10** | Hinweis jetzt: Kältemaschinen mit Geräteart `ABSORPTION` rechnen als elektrische Maschine? | **Ja**, als Hinweis ohne Zahlenwirkung in SOK-0 — schützt vor einer falsch eingespielten Maschine, bis SOK rechnet |
| **SOK-Q11** | Abwärme mit stündlicher Verfügbarkeit (Lastgang) als Antrieb? | **Später**; erste Stufe EXTERN mit Träger und Anschlussleistung |

---

## 13. Risiken

| Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|
| Typkennfelder aus wenigen Forschungs- und Katalogsätzen | Kälteleistung außerhalb des Nennpunkts unsicher | Anpassung an Datenblattpunkte empfehlen; Gütewert und Herkunft im Bericht zeigen |
| Rückkühltemperatur bestimmt die Kälteleistung stark (5.3) | zu kleines oder trockenes Rückkühlwerk überschätzt die Kälte | Prüfregel Rückkühlwerk, Grenzstunden, lastabhängige Annäherung (K-F) |
| Antriebstemperatur in der Vorstufe nur Plan (Gerätedaten der Quellen) | Kollektorfeld liefert kühler als geplant, Antrieb teils nicht verwertbar | Nachstufe begrenzt in einem Schritt; nicht verwertbare Wärme gebucht, gezählt, gemeldet |
| Mitnahme (wärmegeführt) braucht eine neue Senkeneigenschaft | Eingriff in die Kaskadenschleife | erst SOK-7, nach dem kältegeführten Weg |
| Doppelbuchung Wärme/Kälte | stille Fehler in Bilanz und Kennzahlen | Antriebsbuchung, Sorptionsprobe, Deckungsprobe (6.9) |

---

## 14. Quellen

Abgerufen bzw. recherchiert am 17.10.2026. „Suchauszug“ = Inhalt nur aus Suchergebnissen, Seite nicht geöffnet.

**Normen und Regelwerke**

- [N1] DIN/TS 18599-7:2025-10 (ersetzt DIN V 18599-7:2018-09), Inhaltsangabe: <https://www.dinmedia.de/de/vornorm/din-ts-18599-7/390215105>;
  DIN V 18599-7:2018-09 (Gliederung 7.1.4, Anhang A, B laut Suchauszug baunormenlexikon):
  <https://www.baunormenlexikon.de/norm/din-v-18599-7/98f3ea7e-e942-431c-adf4-101dd799e819> — Suchauszug.
- [N2] VDI 2067 Blatt 1 (2012-09, Entwurf 2025-04), Handbuchübersicht: <https://vdi.de/en/home/vdi-standards/details/vdi-handbuch-ressourcenmanagement-in-der-umwelttechnik> — Suchauszug.
- [N3] VDI 3805: Konzeptprüfung Kälteanlagen, Abschnitt 4, mit dem Forschungsschlussbericht 2021 <https://edocs.tib.eu/files/e01fb24/1880717301.pdf>;
  eigene Durchsicht `VDI-3805-Daten/` (39 `.vdi`-Dateien, Blätter 3, 19, 20, 22).
- [N4] DIN EN 12309 Reihe: Teil 3:2025-10 <https://www.dinmedia.de/de/norm/din-en-12309-3/379893929>, Teil 1:2023
  <https://www.dinmedia.de/de/norm/din-en-12309-1/367944454>, Teil 6:2025 <https://www.evs.ee/en/evs-en-12309-6-2025> — Suchauszug.
- [N5] AHRI Standard 560-2023 (I-P): <https://www.ahrinet.org/system/files/2023-12/AHRI%20Standard%20560-2023%20%28I-P%29.pdf>;
  AHRI 560-2000: <https://www.ahrinet.org/system/files/2023-06/AHRI_Standard_560_2000.pdf> — Suchauszug.
- [N6] Eurovent LCP-HP: <https://eurovent-certification.com/en/cms/news/programme-focus-chillers-lcp-hp> — Suchauszug.
- [N7] VDMA 24247-9:2022-01 Sorptionskälteanlagen: <https://www.dinmedia.de/de/technische-regel/vdma-24247-9/347828585> — Suchauszug.
- [N8] AMEV Kälte 2017 (AMEV Nr. 139): <https://www.din.de/de/mitwirken/normenausschuesse/fnkae/veroeffentlichungen/wdc-beuth:din21:285989217> — Suchauszug.
- DIN EN 14825:2023-10, Abschnitt 1 Anwendungsbereich: `Quellen/DIN EN 14825 - DIN.pdf` (eingesehen).

**Literatur**

- [L1] Hellmann, H.-M.; Schweigler, C.; Ziegler, F.: A simple method for modelling the operating characteristics of
  absorption chillers. Eurotherm Seminar, 1998; The characteristic equations of absorption chillers. Int. Sorption
  Heat Pump Conf., München 1999. IIR: <https://iifiir.org/en/fridoc/the-characteristic-equations-of-absorption-chillers-18494> — Suchauszug.
- [L2] Kühn, A.; Ziegler, F.: Operational results of a 10 kW absorption chiller and adaptation of the characteristic
  equation. 1st Int. Conf. Solar Air Conditioning, Bad Staffelstein, 6.–7.10.2005 (zitiert und parametriert in [D1]).
- [L3] Puig-Arnavat, M.; López-Villada, J.; Bruno, J. C.; Coronas, A.: Analysis and parameter identification for
  characteristic equations of single- and double-effect absorption chillers by means of multivariable regression.
  Int. J. Refrigeration 33 (2010) 1, 70–78. <https://orbit.dtu.dk/en/publications/analysis-and-parameter-identification-for-characteristic-equation/> — Suchauszug.
- [L4] IEA SHC Task 38 (Publikationen <https://task38.iea-shc.org/publications>, Bericht B1, C1), Task 48 (Subtask A1
  Chiller Characterization, <https://task48.iea-shc.org/subtasks>), Task 53 (Herstellervortrag 2016,
  <https://iea-shc.org/Data/Sites/1/media/events/task53/workshop-industry-madrid/task53-meeting-20160411-yazaki.pdf>);
  Bandbreiten ein-/zwei-/dreistufig aus <https://task48.iea-shc.org/Data/Sites/6/documents/tools/main-presentation,-part-a.pdf> — Suchauszug.
- [L5] Henning, H.-M.; Motta, M.; Mugnier, D.: Solar Cooling Handbook. 3. Aufl., 2013 (IEA SHC Task 38),
  <https://pubs.iea-shc.org:443/Data/Sites/1/documents/press/2013-12-04-solar-cooling-handbook-published.pdf> — Suchauszug.
- [L6] Fraunhofer ISE, Henning: EuroSun 2010 Keynote (Technologieklassen):
  <https://solarthermalworld.org/wp-content/uploads/2012/07/EuroSun2010_Keynote-Henning.pdf> — Suchauszug.
- [L7] Meunier, F.; Neveu, P.: Absorption chillers and heat pumps — ammonia-water systems (Techniques de l'Ingénieur):
  <https://www.techniques-ingenieur.fr/en/resources/article/ti204/liquid-absorption-machines-be9738/v1>; Cefarin,
  Dissertation Udine (NH₃/H₂O unter 0 °C): <https://air.uniud.it/retrieve/e27ce0c5-6065-055e-e053-6605fe0a7873/10990_609_TESI%20PhD-%20CEFARIN.pdf> — Suchauszug.
- [L8] Ghafoor, A.; Munir, A.: Worldwide overview of solar thermal cooling technologies. Renew. Sustain. Energy Rev. 43
  (2015) 763–774, <https://ideas.repec.org/a/eee/rensus/v43y2015icp763-774.html> — Suchauszug.
- [L9] LiBr ein-/zweistufig, Antriebstemperaturen und Wärmeverhältnisse: Übersicht Techniques de l'Ingénieur
  <https://techniques-ingenieur.fr/en/resources/article/ti204/liquid-absorption-machines-be9735/v2> — Suchauszug.
- [L10] ILK Dresden: DBU-Abschlussbericht AZ 22416 (H₂O/LiBr-Kleinabsorber) <https://opac.dbu.de/ab/DBU-Abschlussbericht-AZ-22416.pdf>;
  TIB-Bericht solare Klimaanlage < 5 kW (ILK, EAW, TU Ilmenau) <https://edocs.tib.eu/files/e01fb09/605458405.pdf>;
  Fachartikel Die Kälte (Double-Lift ζ 0,30–0,35) <https://www.diekaelte.de/sites/default/files/ulmer/file_171823.pdf> — Suchauszug.
- [L11] Absorber 50/160 kW (TU Berlin, TU Dresden, ZAE Bayern): <https://chemanager-online.com/de/themen/energiesparend-kuehlung-mit-waerme> — Suchauszug.
- [L12] Zykluszeiten von Adsorbern: <https://www.mdpi.com/2076-3417/12/13/6542> — Suchauszug.

**Daten und Software**

- [D1] oemof-thermal (MIT, Standardzweig `dev`): <https://github.com/oemof/oemof-thermal>; Parameterdatei
  <https://raw.githubusercontent.com/oemof/oemof-thermal/dev/examples/absorption_heatpump_and_chiller/data/characteristic_parameters.csv>
  und Dokumentation `docs/absorption_chillers.rst` (beide abgerufen).
- [D2] EnergyPlus `datasets/ExhaustFiredChiller.idf`: <https://raw.githubusercontent.com/NREL/EnergyPlus/develop/datasets/ExhaustFiredChiller.idf> (abgerufen).
- [D3] EnergyPlus `testfiles/IndirectAbsorptionChiller.idf`: <https://raw.githubusercontent.com/NREL/EnergyPlus/develop/testfiles/IndirectAbsorptionChiller.idf> (abgerufen).
- [D4] Modelica Buildings Library, `Buildings.Fluid.Chillers`: <https://simulationresearch.lbl.gov/modelica/releases/latest/help/Buildings_Fluid_Chillers.html> — Suchauszug.
- [M1] EnergyPlus Input Output Reference, `Chiller:Absorption:Indirect`:
  <https://raw.githubusercontent.com/NREL/EnergyPlus/develop/doc/input-output-reference/src/overview/group-plant-equipment.tex> (abgerufen).
- [M2] EnergyPlus Engineering Reference, Chillers (Indirect, BLAST, DirectFired, DoubleEffect):
  <https://raw.githubusercontent.com/NREL/EnergyPlus/develop/doc/engineering-reference/src/simulation-models-encyclopedic-reference/chillers.tex> (abgerufen).
- [M4] TRNSYS Type 107, Nutzerliste (TESS-Antworten 2010–2019): <https://onebuilding.org/archive-part2/trnsys-users-onebuilding.org/2010-January/038802.html>,
  <https://onebuilding.org/archive/trnsys-users-onebuilding.org/2012-November/024351.html>,
  <https://onebuilding.org/archive-part2/trnsys-users-onebuilding.org/2011-September/041899.html> — Suchauszug.

**Herstellerseitige Angaben** (Muster, keine Werte übernommen)

- [H1] Datenblatt-Fremdkopie eines Kleinabsorbers (Nennpunkt, Einsatzbereich Heizwasser, Rückkühlwärme):
  <https://personales.upv.es/vsoto/index_archivos/FTP/SOLAR/WFCYAZAKI.pdf> — Suchauszug.
- [H2] Messprojekt Krankenhaus Kamenz (Heiz-, Rückkühl-, Kaltseite eines Absorbers): <https://tu-dresden.de/ing/maschinenwesen/iet/gewv/forschung/forschungsprojekte/alte_projekte/Kamenz/kaelte> — Suchauszug.
- [H3] Herstellervortrag Absorber 50/160/500 kW (Nennbedingungen): <https://ihk.de/blueprint/servlet/resource/blob/6292396/4e919c2d86ce284289438c3ab496a23c/folien-baelz-data.pdf> — Suchauszug.
- [H4] Adsorber: Antriebstemperatur 50–95 °C, Silicagel und Zeolith: <https://fahrenheit.cool/en/?p=2619> — Suchauszug.
- [H5] Kristallisationsgrenzen und Kühlwasser-Bypass (Patente und Fachliteratur):
  <https://patents.google.com/patent/US6637221>, <https://iifiir.org/en/fridoc/analysis-of-the-part-load-behaviour-of-sorption-chillers-with-thermally-21357> — Suchauszug.

**Förderung und Recht** (Prüfpunkte, kein Rechtsrat)

- [F1] Kälte-Klima-Richtlinie: Umweltbundesamt <https://www.umweltbundesamt.de/themen/klima-energie/fluorierte-treibhausgase-fckw/natuerliche-kaeltemittel-in-stationaeren-anlagen/allgemeine-informationen/foerdermittel>;
  Fassung 2014 <https://www.umwelt-online.de/recht/luft/kaelteklimarl_in.htm>; Verbändemitteilung 12/2025
  <https://updates.fgk.de/presseverwaltung/pressedateien/251216_PM_Kaelte-Klima-Richtlinie.pdf> — Suchauszug.
- [F2] KWKG § 2 (Begriffe KWKK, Kältespeicher): <https://www.gesetze-im-internet.de/kwkg_2016/__2.html>, Fassung 2025
  <https://gesetze.legal/bund/kwkg_2025/2>, Gliederung <https://gesetze.legal/bund/kwkg_2025/inhaltsuebersicht> — Suchauszug.
- [F3] UK Energy Technology List, Kategorie Absorption: <https://etl.energysecurity.gov.uk/products/refrigeration-equipment/absorption-other-heat-driven-cooling-heating-equipment> — Suchauszug.

**Papiere im Repositorium**

[Konzeptprüfung Kälteanlagen](2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md) (3, 4, 5, 6),
[Recherche Kälteanlagen](2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md),
[Entwurf Split/VRF/Rückkühlwerk](2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md) (5, Rückkühlwerk),
[Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) (4.4, 4.6, 5.3, 5.6, 6.4, 14),
[Anlagenkopplung](../Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md),
[Konzept Kältebedarf](../../ueberholt/Konzept_Kaeltebedarf_EPOS-Plan.md) (4.2),
[Konzept Kältemittel](../../ueberholt/Konzept_Kaeltemittel_Stammdatum_EPOS-Plan.md) (Geräteklasse Absorption),
[Konzept Kessel-Kennlinie](../../ueberholt/Konzept_Kessel_Kennlinie_EPOS-Plan.md) (Takt und Anfahrverlust),
[Wirtschaftlichkeit](../Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md),
[Status Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md) (E118, E120, E127),
[`CLAUDE.md`](../../../CLAUDE.md) (Regressionsnetz, Einfrierregeln).
