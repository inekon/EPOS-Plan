# Kälteanlagen: Konzeptprüfung zu Gerätearten, Katalog und Datenimport

**Stand 10.10.2026** · Anlass: Anwenderauftrag vom 10.10.2026 („Es fehlen im Katalog Split-Kälteanlagen und
weitere. Welche Möglichkeiten des Datenimports für Kälteanlagen (auch Herstellerdaten) gibt es?“, Nachfrage
„Gibt es VDI-3805-Daten zu Kälteanlagen?“) · Codestand `8100e397` (Zweig `ios_migration_september`) ·
schreibt die [Recherche Kälteanlagen vom 08.10.2026](2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md)
fort; Vorlage zur Entscheidung, kein Code geändert.

Geprüft wurden die Recherche vom 08.10.2026, die [Umsetzung KM1](2026-10-09_Umsetzung_KM1_Typkennfelder.md),
das [Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) (5.0, 5.1, 5.3, 12, 14), das
[Fachkonzept KM3](../../ueberholt/Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md), Stufe 5 des
[Konzepts Katalogauswahl](../Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md) und die Entscheide E15, E33,
E68, E74 und E116 im [Status Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md). Externe Angaben sind
Recherchestand, teils nur aus Suchauszügen; sie sind **kein Rechtsrat**. Das Papier nennt keine Normwerte und
keine Kennwerte konkreter Produkte.

---

## 1. Antwort in Kürze

Der Katalog führt heute ausschließlich **Kaltwassersätze**: 37 schreibgeschützte Sätze ohne Hersteller (3
Beispielgeräte, 34 neutrale Typkennfelder aus offenen Kurven), dazu die reversiblen Wärmepumpen, von denen 15
von 51 eine Kühlleistung und 7 eine Kühlkennlinie tragen. Split-, Multisplit-, VRF-Geräte, Rückkühlwerke und
Absorptionskälte fehlen nicht nur im Katalog, sondern im Rechenkern: EPOS-Plan kennt keinen Kälteerzeuger ohne
Kaltwasser und keine luftgeführte Übergabe, und das Kühlkonzept (§14) schließt Luftführung und RLT aus — ein
Split-Katalog ohne Kernerweiterung wäre eine Liste ohne Rechnung. **VDI 3805 hat kein Blatt für
Kaltwassersätze, Split/VRF oder Rückkühler**; ein Forschungsschlussbericht stellt diese Lücke fest, ein Blatt
entstünde erst auf Antrag von Kälteherstellern. Kältedaten in VDI 3805 gibt es nur an **Wärmepumpen (Blatt 22,
Kühlblöcke)**: 7 von 12 Herstellern der vorliegenden Dateien liefern sie, rund ein Drittel der Blöcke liegt in
Heizlage und wird beim Import benannt abgelehnt. Der Menüpunkt „Import Wärmepumpen VDI 3805“ im
Administrationsmenü ist deshalb nicht sinnlos — er bringt die Kühlkennlinien der reversiblen Wärmepumpen —,
aber er ist **kein Weg für Kältemaschinen**; deren Import liegt im Katalogdialog „Kältemaschinen → Import…“
(Copper-Kurvendatei, CSV-Kennfeldvorlage) und fehlt im Menü „Daten & Import“. Für Hersteller-Kältedaten bleibt
der praktikable Weg die **CSV-Vorlage aus Leistungstabellen der Auslegungsprogramme** und, als neue Variante,
die **Ökodesign-Produktdatenblätter (Punkte A–D)**; EPREL taugt für Splitgeräte bis 12 kW als Stammdatenquelle,
Eurovent und AHRI nur mit Vertrag. Vorgeschlagen werden acht Stufen K-A bis K-H: zuerst Katalogfelder und
Importvarianten für die vorhandenen Kaltwassersätze (rund 6–9 PT), danach — nur nach Aufhebung der Abgrenzung
§14 für Direktverdampfer — Split/Multisplit als neue Anlagenart (rund 18–26 PT).

---

## 2. Prüfung der bestehenden Konzepte

### 2.1 Recherche vom 08.10.2026 (sieben Stufen)

| Stufe der Recherche | Stand am 10.10.2026 | Bewertung |
|---|---|---|
| 1 Importart Kältemaschine, Typkennfelder, CSV-Vorlage | **gebaut** als KM1 (#841) und KM2 (#848, Schemaschritt 203: 34 Typkennfelder in jeder Datenbank) | gilt; die Ausgangslage-Tabelle der Recherche („3 Beispielgeräte, kein Importer“, „fünf Importarten“) ist überholt |
| 2 Teillast und Takten der Kältemaschine | **gebaut** als KM3 (E116, #876–#879, Schemaschritt 210, Referenzprojekt 1063, Basis R49); Wiki-Upload offen | gilt; die Lücke „konstanter EER, Takten ohne Verlust“ ist geschlossen |
| 3 Rückkühlung parametrieren, Teil-Freikühlung | **offen**; die freie Kühlung über die Wärmequelle ist gebaut (KU3-6) und bekommt mit FK ihr Referenzprojekt 1064 (Basis R51), die Teil-Freikühlung am Rückkühler fehlt | gilt; Rückkühler als eigener Katalog bleibt offen (hier K-F) |
| 4 Kältemittel als Stammdatum | **offen**; `Kaeltemittel` ist Freitext ohne Rechenwirkung, die Wärmepumpe hat eine Kältemittel-Schnellwahl im Stammblatt (UB-E3) | gilt, steht aber gegen Kühlkonzept §14 (unten) |
| 5 Split/Multisplit ≤ 12 kW als neue Anlagenart | **offen** | gilt fachlich; braucht einen Konzeptentscheid gegen §14 (hier K-D) |
| 6 Kühlkennfelder der WP ergänzen | **offen** | gilt; VDI-3805-Kühlblöcke bleiben die Hauptquelle |
| 7 R744, passive Erdkühlung, VRF > 12 kW | **offen**, bedingt | gilt; VRF hier als K-E nach K-D |
| „Nicht verfolgen“ | — | gilt unverändert (AHRI ohne Lizenz, EPREL-Liste mit nachgeahmten Kopfzeilen, ETIM/BAFA als Kennfeldquelle, CoolProp zur Laufzeit, Rooftops) |

**Lücke der Recherche:** Absorptions- und Adsorptionskälte, das Rückkühlwerk als eigenes Glied und die Frage
nach VDI 3805 kommen nicht oder nur am Rand vor; Abschnitt 3 und 4 schließen das.

**Namenshinweis:** Die Kurzzeichen weichen von der Stufennummer ab — Stufe 1 ist KM1, KM2 ist der
Schemaschritt zu Stufe 1, Stufe 2 ist KM3. Dieses Papier benennt seine Stufen deshalb mit Buchstaben (K-A bis
K-H) und ordnet sie in 5.1 den Recherche-Stufen zu.

### 2.2 Umsetzung KM1

Gilt. Formate, Rasterregel, Bezug der Luftkühlung (Kennfeld an Rückkühltemperatur = Außenluft + 5 K) und
Nennpunkt sind der Bestand. Der Abschnitt „Was Stufe 2 noch braucht“ ist mit KM3 erledigt bis auf die
**Herkunftscodes der Copper-Kurven** und die **Skalierung auf den Nennpunkt eines EU-Datenblatts** — beide
gehören in K-C. Die CSV-Vorlage kennt nur das Raster Rückkühl- × Kaltwassertemperatur; eine Form für
Teillastpunkte A–D oder für Nennwerte allein fehlt.

### 2.3 Kühlkonzept

| Abschnitt | Prüfergebnis |
|---|---|
| 5.0 WP mit Kühlfunktion (E15) | gilt; „kühlfähig“ heißt Katalog `Kuehlleistung > 0` (15 von 51), rechenbar nur mit Kühlkennlinie (7) — die Lücke von 8 Sätzen ist Datenlage, kein Fehler |
| 5.1 Importbefund Kühlblöcke | gilt; `KuehlblockPruefung` lehnt Heizlage und vertauschte Achsen benannt ab (gemessen: 854 Heizlage und 33 Achsen von 2 517 erreichten Blöcken) |
| 5.3 Kältemaschine (KU3) | gilt für den Kaltwassersatz; die Tabelle nennt „Kältemittel als Text“ und keine Bauart — genau die Felder, die der Katalogfilter heute vermisst |
| 12 Fragen | erledigt mit E31, E33, E68, E74, E75 |
| **14 Abgrenzung** | **Widerspruch zum Anwenderwunsch:** „Luftführung und Kanalnetz“ und „RLT als Gewerk“ sind ausgeschlossen, „Kältemittel und F-Gase“ ebenso. Ein Split- oder VRF-Gerät gibt seine Kälte über die Raumluft ab (Direktverdampfung) — es ist kein RLT-Gerät, fällt aber unter den Wortlaut „Luftführung“ nur dann nicht, wenn die Übergabe als **Umluft am Raum ohne Kanal** festgelegt wird. Das braucht einen ausdrücklichen Entscheid (Frage 3) |

**Weitere Spannung:** §14 schließt „Kältemittel und F-Gase“ aus, die Wärmepumpe hat seit UB-E3 eine
Kältemittel-Schnellwahl, die CSV-Vorlage und `Tab_Kaeltemaschine_STAMM` führen `Kaeltemittel`. Solange das
Feld nur beschreibt (kein GWP, keine Emission), ist das kein Bruch; ein GWP-Feld oder eine
Zulässigkeitsprüfung nach F-Gas-Verordnung (Recherche Stufe 4, hier K-A) braucht dagegen die Lockerung von §14
für das **Stammdatum** — die direkte Treibhauswirkung (Füllmenge, Leckage) bleibt ausgeschlossen.

### 2.4 Fachkonzept KM3 (überholt, umgesetzt)

Die Abgrenzung (WP-Kühlbetrieb Stufe 6, Rückkühler Stufe 3, Kältemittel Stufe 4, Split/Multisplit/VRF Stufe 5)
gilt weiter und deckt sich mit diesem Papier. Die Verdichterregelung (`Verdichterregelung`) und der Randweg
(`Kennfeld_Randweg`) stehen im Katalog; eine **Bauart** (Kaltwassersatz luftgekühlt, wassergekühlt, Split …)
gibt es weiterhin nicht.

### 2.5 Konzept Katalogauswahl, Stufe 5

„Katalogauswahl der Kältemaschine“ ist noch offen. Der Katalogdialog der Kältemaschinen ist mit KD (#881)
bereits an die Wärmepumpe angeglichen (Spalten, Herkunft, Schalter „Typkennfelder ausblenden“, Kennlinie als
Diagramm, eigenes Fenster unter Windows). Ein Katalogfilter nach **Geräteart** kann es nicht geben, solange das
Feld fehlt — `Typ` ist freier Text. Die Konfiguration der Kälte in einem eigenen Bereich der
Simulationskonfiguration ist in Arbeit (Entwurf Kältebereich, 10.10.2026); K-A liefert ihm das Feld, nach dem
die Kälte-Kachel filtern kann.

### 2.6 Entscheide

| Entscheid | Prüfergebnis |
|---|---|
| E15 | gilt (WP mit Kühlfunktion, Auswahl „nur mit Kühlfunktion“) |
| E33 | gilt; K8 „Rückkühlung als Bestandteil der Kältemaschine, kein eigener Erzeuger“ steht gegen ein Rückkühlwerk als Glied (K-F) — K-F braucht einen Folgeentscheid zu K8 |
| E68 | gilt (Kältespeicher gebaut) |
| **E74** | **überholt durch den Bestand:** E74 (04.10.2026) entschied „keine eigene Kachel, der Knopf ‚Kältemaschinen…‘ bleibt der Einstieg“; mit #758, #777 und #838 (06. und 09.10.2026, Anwenderaufträge) trägt die Startseite die Kachel „Kühlung und Kälteanlagen“. Der Statuseintrag E74 sollte als „abgelöst durch die Anwenderaufträge vom 06.10.2026 (#758, #777)“ vermerkt werden |
| E116 | gilt, umgesetzt (KM3) |

### 2.7 Einfrierregel „gesäte Kältemaschinendaten“

Die Einfrierregeln „gesäte Kältemaschinendaten eines Referenzprojekts“ (1055, 1059, AK3) und „gesäte
Teillastdaten einer Kältemaschine“ (1063) stehen in [`CLAUDE.md`](../../../CLAUDE.md) („Regressionsnetz“) und in
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md); mit FK kommt die Regel „gesäte Daten der freien
Kühlung“ (1064) hinzu. Für die Stufen dieses Papiers gilt: Katalogsätze (`_STAMM`) berühren keine Einfrierregel; die Projektkopien der Referenzprojekte schon.

### 2.8 Bestandsbefunde, die das Konzept berühren

- **Katalog:** 37 Sätze `Tab_Kaeltemaschine_STAMM`, alle `ReadOnly`, alle ohne Firma, 20 bis 2 000 kW;
  Rückkühlart LUFT 12, NASSKUEHLER 13, TROCKENKUEHLER 8, WASSER 4. Kennlinie nur Rückkühl- ×
  Kaltwassertemperatur, Teillast über Skalarspalten. WP-Katalog: `Tab_WP_STAMM.Bauart` bei 45 von 51 leer.
- **Importwege:** Kältemaschine nur im Katalogdialog (nur Windows; iOS lehnt benannt ab und verweist auf
  „Typkennfelder laden…“); WP-Kühlkennlinien nur über VDI 3805 Blatt 22. Das Menü „Daten & Import“ führt die
  Importe in der Reihenfolge der Kataloge, **ohne Kältemaschine** — ein Anwender sucht sie dort vergeblich.
- **VDI-3805-Dateien unter `VDI-3805-Daten/WP-Daten/`:** Kühlblöcke (Satzart 710.09, Betriebsart 2) in den
  Dateien von 7 der 12 Hersteller, zwischen 14 und 898 Blöcken je Datei, zusammen rund 2 640; 5 Hersteller
  führen nur die Nennkühlleistung im Gerätesatz.

---

## 3. Gerätearten-Matrix

Abbildbarkeit: **ja** = rechnet heute; **teilweise** = rechnet mit Ersatzannahme; **nein** = kein Rechenweg.
Priorität aus Sicht der Datenlage und des Nutzens für Planungsprojekte (A hoch, C niedrig).

| Geräteart | Rechenbedarf | Abbildbar heute | Nötige Kernerweiterung | Datenquelle | Priorität |
|---|---|---|---|---|---|
| Kaltwassersatz luftgekühlt | Kennfeld Außenluft × Kaltwasser, Teillast, Takten | **ja** (Rückkühlart LUFT) | keine; Bauartfeld (K-A) | Typkennfelder, CSV aus Auslegungsprogramm, Ökodesign-Datenblatt (A–D), Eurovent (Vertrag) | A |
| Kaltwassersatz wassergekühlt mit Trocken-/Nasskühler | dazu Rückkühltemperatur | **ja** (Festwert-Grädigkeit) | Rückkühler parametrierbar (K-F) | wie oben; Rückkühler: Herstellerdatenblatt, Eurovent HE (Vertrag) | A |
| Kaltwassersatz mit Brunnen-/Flusswasser | Quelltemperatur | **teilweise** (WASSER fest) | Kopplung an Quelle (K-F) | wie oben | B |
| Reversible Wärmepumpe im Kühlbetrieb | Kühlkennlinie Vorlauf × Quelle, Takten | **ja** | keine | VDI 3805 Blatt 22 (Kühlblöcke), hplib, CSV | A (Daten ergänzen) |
| Freie Kühlung (Sonde, Trockenkühler) | Wärmeübertrager, Pumpe | **teilweise** (über die Wärmequelle gebaut, Referenzprojekt 1064; Rückkühler mit Festwerten) | Teil-Freikühlung am Rückkühler (Recherche Stufe 3) | keine Produktdaten nötig | B |
| Split/Monosplit ≤ 12 kW | Kennfeld Außenluft × Raumluft, Teillast A–D, Raumluft-Übergabe | **nein** | Erzeuger ohne Kaltwasser, Übergabe „Umluftgerät am Raum“, Zuordnung Gerät ↔ Zone (K-D) | EPREL (Stammdaten), Ökodesign-Datenblatt (A–D), offene DX-Kurven | A (nach Entscheid §14) |
| Multisplit ≤ 12 kW / > 12 kW | dazu mehrere Innengeräte, Kombinationsfaktor | **nein** | wie Split, mehrere Zonen je Außengerät (K-D) | EPREL ≤ 12 kW, Datenblatt, Eurovent (Vertrag) | B |
| VRF/VRV | Kombinationsverhältnis, Leitungskorrektur, Teillastkurven | **nein** | auf K-D: Außengerät mit vielen Innengeräten, Korrekturen (K-E) | Eurovent VRF (Vertrag), Herstellerprogramme, offene Kurven | C |
| Rooftop / Kompaktklimagerät | Luftvolumenstrom, Außenluftanteil, Feuchte | **nein** | RLT als Gewerk — §14 schließt aus | — | nicht verfolgen |
| Rückkühlwerk (Trocken, hybrid/adiabat, Kühlturm) | Approach, Feuchtkugel, Ventilator, Wasser | **teilweise** (fester Zuschlag) | eigenes Glied mit Katalog (K-F) | Herstellerdatenblätter, Eurovent HE/CT (Vertrag) | B |
| Absorption (LiBr, NH₃) | Antriebswärme als Senke, Kennfeld Antrieb × Rückkühl × Kaltwasser, größere Rückkühlung | **nein** | neue Anlagenart mit Wärmesenke an Puffer/BHKW/Solar/Fernwärme, Rückkühlung dimensioniert für Kälte + Antrieb (K-G) | Herstellerdatenblätter; EnergyPlus-Kurven (abgasbefeuert, indirekt); charakteristische Gleichung aus der Forschung | C |
| Adsorption | wie Absorption, kleiner, zyklisch | **nein** | wie K-G | Herstellerdatenblätter, Forschungsmodelle | C |
| CO₂-Kälte (R744, transkritisch) | eigener Kreisprozess | **nein** | Recherche Stufe 7 | Herstellerkennfelder | C |

---

## 4. Datenquellen und Importwege

Nutzungsrecht = Recherchestand, nicht anwaltlich geprüft. Eignung: **S** Startkatalog (Auslieferung),
**A** Anwenderimport (Datei des Anwenders), **R** automatischer Abruf aus dem Programm.

| Quelle | Inhalt | Zugriff | Nutzungsrecht (Recherchestand) | S | A | R |
|---|---|---|---|---|---|---|
| VDI 3805 Blatt 22 (Wärmepumpen) | Kühlblöcke reversibler WP (Vorlauf × Quelle × Last) | Herstellerdateien, Import im Administrationsmenü | Herstellerdaten, Weitergabe nur im Rahmen der Herstellerlieferung | nein | **ja** (vorhanden) | nein |
| VDI 3805 Blatt 6, 18, 27, 37 (Entwurf) | Übergabe: Heiz-/Kühlkonvektoren, Flächenkühlung, Deckenstrahlplatten, Gebläsekonvektoren/Fassadengeräte | Herstellerdateien | wie oben | nein | später (Übergabe, nicht Erzeuger) | nein |
| VDI 3805 für Kaltwassersätze, Split/VRF, Rückkühler | — **kein Blatt**; Lücke festgestellt im [Forschungsschlussbericht 2021](https://edocs.tib.eu/files/e01fb24/1880717301.pdf), ein Blatt entstünde erst auf Antrag von mindestens zwei Kälteherstellern | — | — | — | — | — |
| PNNL Copper ([github.com/pnnl/copper](https://github.com/pnnl/copper)) | Kurvensätze Kaltwassersätze (CAPFT, EIRFT, EIRFPLR) | Datei | BSD-2; Hersteller-/Modellbezug neutralisiert ausliefern | **ja** (genutzt) | ja (vorhanden) | nein |
| EnergyPlus `datasets` ([github.com/NREL/EnergyPlus](https://github.com/NREL/EnergyPlus/tree/develop/datasets)) | `Chillers.idf`, `AirCooledChiller.idf` (über 160 Kurvensätze), `DXCoolingCoil.idf`, `ResidentialACsAndHPsPerfCurves.idf`, `ExhaustFiredChiller.idf` | Datei (IDF) | BSD-3 laut Recherche, Lizenztext nicht geprüft; Namen neutralisieren | **ja** (nach Lizenzprüfung) | ja (neuer Parser) | nein |
| Modelica Buildings ([github.com/lbl-srg/modelica-buildings](https://github.com/lbl-srg/modelica-buildings)) | Datensätze ElectricEIR / ReformulatedEIR | Datei (Modelica-Records) | modifizierte BSD, nicht geprüft | bedingt | bedingt | nein |
| hplib ([github.com/FZJ-IEK3-VSA/hplib](https://github.com/FZJ-IEK3-VSA/hplib)) | Keymark-Auswertung, linearer EER-Fit für geregelte L/W-WP | Datei (CSV) | Code MIT, Daten CC BY 4.0 (Namensnennung) | bedingt (WP-Kühlung) | ja | nein |
| EPREL `airconditioners` ([eprel.ec.europa.eu](https://eprel.ec.europa.eu)) | Raumklimageräte ≤ 12 kW: Bauart, Innengeräte, Kältemittel, GWP, Pdesignc, SEER/SCOP, Energieklasse, Schall; **keine A–D-Punkte** | öffentliche API; Einzelabruf je Registriernummer offen, Listen-/Massenabruf nur mit Schlüssel auf Antrag | Bedingungen (seit 03.06.2024) nicht selbst gelesen | nein (Rechte offen) | ja (JSON des Einzelabrufs) | **ja** (Einzelabruf, K-H) |
| Ökodesign-Produktdatenblätter: [VO 206/2012](https://eur-lex.europa.eu/legal-content/DE/TXT/?uri=CELEX:32012R0206) (Raumklimageräte), [VO 626/2011](https://eur-lex.europa.eu/legal-content/DE/TXT/?uri=CELEX:32011R0626) (Label), [VO 2016/2281](https://eur-lex.europa.eu/legal-content/DE/TXT/?uri=CELEX:32016R2281) (Kaltwassersätze bis 2 000 kW, Luftkühlung > 12 kW), [VO 2015/1095](https://eur-lex.europa.eu/legal-content/DE/TXT/?uri=CELEX:32015R1095) (Prozesskühler) | Teillastpunkte A–D, saisonale Kennzahl, Taktkennwert, Nebenaufnahmen, Kältemittel | Pflichtveröffentlichung des Herstellers (PDF), keine zentrale Datenbank | Daten des Herstellers; Abtippen durch den Anwender unbedenklich, Sammeln in den Auslieferungskatalog nicht geklärt | nein | **ja** (Form „Ökodesign-Datenblatt A–D“, K-C) | nein |
| Eurovent Certified Performance ([eurovent-certification.com](https://www.eurovent-certification.com)) | LCP-HP (Kaltwassersätze, Teillasttabelle, saisonale Kennzahl), AC, VRF, RT, CT, HE | Modellseiten; CSV je Marke für registrierte Nutzer; keine offene API | Weitergabe nur über Partnerschaften | nur mit Vertrag | Datei des Anwenders denkbar | nur mit Vertrag |
| Heat Pump Keymark ([heatpumpkeymark.com](https://www.heatpumpkeymark.com)) | WP-Zertifikate; Kühlen nur bei bestimmten Anwendungen | Zertifikats-PDF | offen einsehbar, Export nicht belegt | nein | über hplib | nein |
| BAFA-Gerätelisten | Wärmepumpen ohne Kühlliste | Liste | — | nein | nein | nein |
| ETIM / BMEcat (Großhandel) | Split-Klassen mit Nennpunkten, keine Kennfelder | Katalogdatei des Großhandels | Lizenz des Lieferanten | nein | bedingt (nur Nennwerte) | nein |
| Auslegungsprogramme der Hersteller | Leistungstabellen (CSV/Excel) für das ausgelegte Gerät | Export beim Anwender | Anwenderseite | nein | **ja** (CSV-Vorlage, vorhanden) | nein |
| AHRI Directory | US-zertifizierte Kälte- und Klimageräte | Abo | nur mit kostenpflichtiger Lizenz | nein | nein | nein |
| ENERGY STAR ([data.energystar.gov](https://data.energystar.gov)), AU Energy Rating ([reg.energyrating.gov.au](https://reg.energyrating.gov.au)), UK ETL Packaged Chillers ([etl.energysecurity.gov.uk](https://etl.energysecurity.gov.uk)) | Nennwerte nach US-, AU- bzw. UK-Prüfnormen | Open Data / Liste | offen (Open Data), Prüfbedingungen abweichend | nein (Prüfnormen) | bedingt | nein |
| Absorption/Adsorption | Herstellerdatenblätter, Forschungsmodelle (charakteristische Gleichung) | PDF, Literatur | Herstellerdaten bzw. Veröffentlichung | nein | ja (CSV-Variante, K-G) | nein |

Normen, die die Stufen berühren (nur Nummer und Gegenstand): EN 14825 (saisonale Kennzahlen und
Teillastprüfung von Klimageräten, Kaltwassersätzen und Wärmepumpen), EN 14511 (Prüfung bei Volllast),
DIN/TS 18599-7 (Energiebewertung Raumlufttechnik und Kälte), EN 1048 (Prüfung von Trockenkühlern),
VDI 3805 Blätter 6, 18, 22, 27, 37.

---

## 5. Vorschlag: fortgeschriebener Stufenplan

### 5.1 Übersicht

| Stufe | Inhalt | Recherche-Stufe | Kern | Schema | Oberfläche | Basiswirkung | Aufwand | hängt an |
|---|---|---|---|---|---|---|---|---|
| **K-A** — gebaut ([Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_K-A_Katalogfelder_Kaelte.md), Schemaschritt 211) | Katalogfelder Geräteart, Hersteller, Kältemittel (mit GWP-Feld), saisonale Kennzahl | 4 (teilweise) | Prüfregeln im Stammcontroller, Prüfsumme der Katalogfassung | Schemaschritt: `Geraeteart` (Wertemenge), `Kaeltemittel_GWP`, `SEER`/`Eta_s_c` an `Tab_Kaeltemaschine(_STAMM)`; Geräteart der 37 Sätze nachtragen | Spalten und Filter im Katalogdialog; Filter in der Kälte-Kachel | keine (nur `_STAMM` und neue leere Projektspalten) | 2–3 PT | Entscheid §14 für das Stammdatum (Frage 3) |
| **K-B** | Neutraler Startkatalog aus offenen Kurven erweitern (EnergyPlus `Chillers.idf`, `AirCooledChiller.idf`), Auswahlregel je Rückkühlart × Verdichter × Leistungsklasse | 1 (Ausbau) | IDF-Leser in der Importart Kältemaschine | Schemaschritt mit neuen ReadOnly-Typkennfeldern | keine neue | keine | 2–3 PT | K-A; Lizenzprüfung EnergyPlus |
| **K-C** — gebaut ([Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_K-C_Kaelteimport_Varianten.md)) | Importvarianten der CSV-Vorlage: „Nennwerte allein“, „Ökodesign-Datenblatt A–D“ (Teillastpunkte → Teillastkurve und Taktkennwert, Kennfeld aus Typkennfeld skaliert), Skalierung eines Typkennfelds auf den Nennpunkt; Herkunftscodes; Eintrag „Kältemaschinen“ im Menü „Daten & Import“ | 1, 2 (Rest) | Leser und Umrechnung A–D in der Importart | keines | Menüpunkt, Leseprotokoll | keine | 2–3 PT | K-A |
| **K-D** | Split und Multisplit als neue Anlagenart: Erzeuger ohne Kaltwasser, Kennfeld Außenluft × Raumluft (Teillast A–D), Übergabe „Umluftgerät am Raum“ ohne Kanal, Zuordnung Außengerät ↔ Zonen, Kombinationsfaktor | 5 | neuer Erzeugerzweig in der Kältekaskade, neue Übergabeart neben `Kuehluebergabe`, Deckung je Zone | neue Anlagenart in `Tab_Typ_Energieanlagen`, `Tab_Klimageraet(_STAMM)` mit Kennfeld, Zuordnung Gerät ↔ Zone; Register in der Katalogfassung | Katalogdialog, Erzeugerdialog, Kälte-Bereich, Bericht | neues Referenzprojekt → neue Basis; bestehende Projekte bitgleich | 18–26 PT | Entscheid §14 (Frage 3), K-A |
| **K-E** | VRF: ein Außengerät, viele Innengeräte, Kombinationsverhältnis, Leitungskorrektur | 7 (VRF) | Ausbau von K-D | Innengerätetabelle | wie K-D | neues Referenzprojekt | 8–12 PT | K-D |
| **K-F** | Rückkühlwerk als eigenes Glied: Trocken, hybrid/adiabat, Kühlturm; Approach lastabhängig, Feuchtkugel, Ventilatorstrom, Wasserverbrauch; WASSER an die Quelle gekoppelt | 3 | Rückkühlung aus `KaelteFestwerte` in eine Klasse mit Katalog; Festwerte als Vorgabe | `Tab_Rueckkuehler(_STAMM)`, Verweis an der Kältemaschinen-Anlage | Katalog, Anlagenschema (Kältebahn) | mit Vorgaben = Festwerte bitgleich; Teil-Freikühlung neue Basis | 8–12 PT | Folgeentscheid zu E33/K8 (Frage 5), FK |
| **K-G** | Absorptions-/Adsorptionskälte: Antriebswärme als Senke an Puffer/BHKW/Solar/Fernwärme, Kennfeld Antrieb × Rückkühl × Kaltwasser, Rückkühlung für Kälte + Antriebswärme | — (neu) | neue Anlagenart, Senke im Wärmenetz (`Z_AnlageSenke`), Kopplung Kälte- und Wärmeseite | neue Tabellen mit Kennfeld | Katalog, Erzeugerdialog | neues Referenzprojekt | 15–22 PT | K-F; Frage 6 |
| **K-H** | EPREL-Abruf je Registriernummer: Stammdaten eines Splitgeräts übernehmen, A–D aus dem Datenblatt nachtragen | 5 (Daten) | Abrufnaht (plattformfrei, über benannten Dienst), JSON-Leser | keines über K-D hinaus | Feld „Registriernummer“ im Katalogdialog | keine | 2–4 PT | K-D; Frage 4 |

Summe ohne K-G: rund 42–63 PT; der Teil für die vorhandenen Kaltwassersätze (K-A bis K-C) rund 6–9 PT.

### 5.2 Erläuterung je Stufe

**K-A Katalogfelder** — gebaut ([Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_K-A_Katalogfelder_Kaelte.md); Schemaschritt 211, Entscheid E118). `Typ` bleibt Freitext; die Geräteart wird eine Wertemenge (Kaltwassersatz luftgekühlt,
Kaltwassersatz wassergekühlt, Kaltwassersatz mit Freikühlung, später Split, Multisplit, VRF, Absorption),
neue Spalten mit `CHECK`. Firma füllen nur Anwenderimporte; die ausgelieferten Typkennfelder bleiben ohne
Firma. Das GWP-Feld beschreibt, rechnet aber keine Emission (direkte Wirkung bleibt nach §14 ausgeschlossen);
eine Warnung nach den Stichtagen der F-Gas-Verordnung ist die Ausbaustufe (Recherche Stufe 4).

**K-B Startkatalog.** Die Auswahlregel aus KM1 (je Rückkühlart, Verdichter und Leistungsklasse ein Satz) wird
um die großen wassergekühlten Maschinen und weitere Luftkühler erweitert; Ziel ist eine Abdeckung von 20 bis
3 000 kW ohne Dubletten. Die Sätze bleiben „Typkennfeld“, neutral benannt.

**K-C Importvarianten** — gebaut ([Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_K-C_Kaelteimport_Varianten.md)). Die CSV-Vorlage bekommt Kopfschlüssel für die Form: „Kennfeld“ (heute), „Nennwerte“
(ein Punkt, Kennfeld aus dem nächstliegenden Typkennfeld skaliert) und „Ökodesign A–D“ (vier Teillastpunkte
mit Außentemperatur, Leistung und EER, dazu Taktkennwert; daraus die Teillastkurve von KM3 und der Nennpunkt).
Der Menüpunkt „Kältemaschinen“ in „Daten & Import“ öffnet dieselbe Importart.

**K-D Split/Multisplit.** Die Kennfeldklasse wird mit umgedeuteten Achsen wiederverwendet (Außenluft statt
Rückkühlung, Raumluft statt Kaltwasser); die Raumluft-Feuchtkugel wird fest angenommen und die latente Last
bleibt ausgeschlossen (sensibler Kühlkanal). Die Übergabe liefert Kälte unmittelbar an die Zone, ohne
Kaltwasser, ohne Puffer und ohne Kanal. Validierung an einem frei zugänglichen Prüffallsatz für Splitgeräte
(siehe Recherche, Abschnitt Split).

**K-E VRF** folgt K-D, sobald Daten über Herstellerprogramme oder einen Eurovent-Vertrag vorliegen.

**K-F Rückkühlwerk** löst die festen Grädigkeiten durch ein Glied mit Katalog ab; die heutigen Festwerte
werden Vorgaben, damit die Referenzprojekte bitgleich bleiben, solange kein Rückkühler gewählt ist.

**K-G Absorption** ist der einzige Weg, Abwärme (BHKW, Solar, Fernwärme) in Kälte zu wandeln; sie lohnt nur
für Projekte mit Wärmeüberschuss im Sommer.

**K-H EPREL-Abruf** setzt einen API-Schlüssel und die gelesenen Nutzungsbedingungen voraus; ohne beides bleibt
nur der Datei-Import des Einzelabrufs durch den Anwender.

### 5.3 Was nicht vorgeschlagen wird

Rooftops und Kompaktklimageräte (RLT, §14), ein eigenes VDI-3805-Blatt (keine Normungsrolle von INEKON;
allenfalls Mitwirkung, wenn Hersteller es beantragen), Massenabruf aus EPREL ohne Schlüssel, AHRI-Daten ohne
Lizenz, Eurovent-Daten ohne Vertrag.

---

## 6. Fragen an den Anwender

Kurzzeichen **KKP** (Konzeptprüfung Kälte); jede Frage mit Empfehlung.

| Nr. | Frage | Empfehlung |
|---|---|---|
| KKP-Q1 | Reihenfolge: zuerst die vorhandenen Kaltwassersätze ausbauen (K-A, K-C, K-B), dann Split (K-D)? | **Ja.** K-A bis K-C kosten rund 6–9 PT, ändern keine Basis und geben der Kälte-Kachel den Gerätearten-Filter; K-D baut darauf auf |
| KKP-Q2 | Split/Multisplit überhaupt als rechnende Anlagenart (K-D), oder genügt ein Katalog ohne Rechnung? | **Rechnende Anlagenart oder gar nicht** — ein Katalog ohne Rechenweg täuscht eine Funktion vor; K-D beauftragen, wenn Wohn- und Kleingewerbeprojekte mit Splitgeräten geplant werden |
| KKP-Q3 | Abgrenzung Kühlkonzept §14 lockern: (a) Direktverdampfer mit Umluftgerät am Raum ohne Kanal zulassen; (b) Kältemittel und GWP als beschreibendes Stammdatum zulassen; direkte Treibhauswirkung, Luftführung mit Kanal und RLT bleiben ausgeschlossen? | **Ja zu (a) und (b)** in diesem Umfang; §14 wird im selben Schritt fortgeschrieben |
| KKP-Q4 | EPREL-API-Schlüssel beantragen (K-H)? | **Ja, beantragen, aber K-H erst nach K-D bauen** — der Antrag klärt Bedingungen und Rate-Limits ohne Kosten |
| KKP-Q5 | Rückkühlwerk als eigenes Glied (K-F) — E33/K8 („Rückkühlung Bestandteil der Kältemaschine“) entsprechend fortschreiben? | **Ja, nach FK** — Vorgaben gleich den heutigen Festwerten, damit die Basis hält |
| KKP-Q6 | Absorptions-/Adsorptionskälte (K-G)? | **Zurückstellen**, bis ein Projekt mit sommerlichem Wärmeüberschuss ansteht |
| KKP-Q7 | Herstellernamen: im Katalog bei Anwenderimporten ja, in ausgelieferten Typkennfeldern und im Wiki nein? | **Ja** — Katalog ja (Anwenderdaten), Auslieferung neutral, Wiki nach Produktdatenregel ohne Hersteller |
| KKP-Q8 | Papierpflege: E74 im Status als abgelöst vermerken (die Einfrierregeln stehen bereits in `CLAUDE.md`)? | **Ja**, als kleine Papierpflege mit dem nächsten Statusschritt |
