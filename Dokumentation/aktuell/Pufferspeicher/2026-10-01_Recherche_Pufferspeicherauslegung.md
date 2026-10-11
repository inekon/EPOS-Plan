# Recherche Pufferspeicher-Auslegung — Befund, Kriterientabelle, Prüfung des Entwurfs (01.10.2026)

**Zweck.** Vorarbeit für das Konzept `Konzept_Pufferspeicher_Auslegung_EPOS-Plan.md`, das am
03.10.2026 entsteht (Routine „Pufferspeicher-Auslegung: Konzept und Bau P1“). Das Papier sammelt,
was der Bestand von EPOS-Plan hergibt, was das Wärmespeicher-Tool unter `Quellen/Waermespeicher-Tool/`
rechnet, welche Regeln und Richtwerte die Fachwelt kennt, und prüft daran den Entwurf des
Auslegungsdialogs (Mockup `Dokumentation/aktuell/Mockups/Pufferspeicher_Auslegung_Mockup.html`). Es endet mit
Verbesserungsvorschlägen samt Empfehlung und den Folgen für den Bauplan P1.

**Auftrag (Anwender, 01.10.2026):** „Recherchiere zu Pufferspeicherauslegung und prüfe nach
Optimierung und Verbesserung des Konzepts.“

**Fortsetzung.** Die zweite Runde — Speicherklassen (Heizung, Kombi, Brauchwasser, Prozess),
Erzeuger je Klasse, Kopplung an Zapfprofil und Nutzungsarten — steht in
[`2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md`](2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md);
sie klärt V12 (Lastgang-Quelle) und ersetzt E-P11 durch das Nutzungsprofil. Die dritte Runde — die am
01.10.2026 abgelegten Normdateien (VDI-MT 4645 Blatt 1, prEN 15316-5:2024, DIN EN 15332 mit A1) —
steht in [`2026-10-01_Recherche_Normen_Speichermodell.md`](2026-10-01_Recherche_Normen_Speichermodell.md); sie liest das
Hauptblatt VDI 4645 (Entwurf 2026-03) gegen K1–K4 und K14 (V37–V39, E-P16 erledigt, E-P28) und ergänzt
K11 um die Umrechnung in W/K (V33–V36, E-P26–E-P27).

**Grenzen.** Normtexte werden zitiert, nicht abgeschrieben; aus Herstellerunterlagen kommen keine
Produktdaten (Konzept Hilfesystem, Abschnitt 13). Zahlen aus Blogs und Ratgeberseiten sind als
Sekundärquellen gekennzeichnet und tragen keine Vorgabewerte. Alle Rechenbeispiele sind fiktiv.


## 1 Quellenlage

| Quelle | Art | Was sie beiträgt |
|---|---|---|
| `Quellen/Waermespeicher-Tool/Waermespeicher-Tool/wsp/sizing_buffer.py`, `storage_sim.py`, `models.py`, `tests/test_sizing.py`, `tests/test_zweipunkt.py` | Quellcode des Vorprojekts (Juli 2026) | die vier Kriterien, Zweipunkt-Betriebssimulation, Vorgabewerte, Raster, Praxisgrenze, Testfälle |
| `Quellen/Waermespeicher-Tool/Dokumentation_Waermespeicher-Tool.docx` C.6–C.8 | Dokumentation des Tools (V1.1) | Begründung je Kriterium, Anwendbarkeitstabelle, Validierung gegen die SWSG-Excel (0 Abweichungen, 911 Ladezyklen), Grenzen |
| BWP, *Leitfaden Hydraulik*, Stand Juli 2016 ([PDF](https://www.waermepumpe.de/uploads/media/BWP_LF_Hydraulik_final_web.pdf)) | Verbandsleitfaden | Mindestvolumen 3–5 l/kW ohne Puffer; Puffer 20–25 l/kW (Laufzeit), 30–40 l/kW (Sperrzeiten); Biomasse 30/55 l/kW; Kühlpuffer 20–25 l/kW |
| VdZ, *Umsteigen auf die Wärmepumpe — Leitfaden für den Fachhandwerker*, 2025 ([PDF](https://files.vdzev.de/pdfs/umsteigen-auf-die-waermepumpe/VdZ_Waermepumpen_WEB_Einzelseiten.pdf)) | Verbandsleitfaden | Mindestlaufzeiten bis 10 min, 3 l/kW dauerhaft durchströmtes Anlagenvolumen, Trenn- gegen Reihenspeicher |
| Fraunhofer ISE, *Abschlussbericht WP-QS im Bestand*, 25.11.2025, FKZ 03EN2029A ([PDF](https://www.ise.fraunhofer.de/content/dam/ise/de/documents/projekte/WP-QS_Schlussbericht.pdf)) | Feldtest, 77 Anlagen | Verdichterstarts je Jahr (51 Luft/Wasser-WP), Ursachen des Taktens, Mischverluste paralleler Speicher, Herstellerobergrenzen je Stunde, Hinweis „VDI 4645 ohne Vorgaben zu Mindestlaufzeit und Schalthäufigkeit“ |
| Lechner, O'Connell, Meierhofer, Brautsch (OTH Amberg-Weiden), *Optimierte Monitoring-, Betriebs- und Regelstrategien für BHKW*, Zukunft Bau F 3058, 2017 ([PDF](https://www.oth-aw.de/files/oth-aw/Forschung/Institute/kwk/Aktuelles/ZukunftBau_F3058.pdf)) | Forschungsbericht | spezifisches Puffervolumen je kW_th, Starts und Betriebsstunden je Start realer BHKW, Herstellergrenzen, Modulation |
| 1. BImSchV § 5 ([gesetze-im-internet](https://www.gesetze-im-internet.de/bimschv_1_2010/__5.html)) | Verordnung | 55 l/kW handbeschickt, 20 l/kW automatisch beschickt, 12 l je Liter Füllraum (Altanlagen) |
| BEG EM, technische Mindestanforderungen Biomasse (BAFA, Laufzeit 2024–2030; [Übersicht](https://www.bafa.de/SharedDocs/Downloads/DE/Energie/ew_biomasse_foerderuebersicht.pdf)) | Förderregel | 30 l/kW Pellets/Hackschnitzel, 55 l/kW Scheitholz |
| Mini-KWK-Richtlinie, BAnz AT 29.12.2014 B5 ([PDF](https://www.bhkw-infozentrum.de/download/richtlinie_mini_kwk_2015.pdf)) | Förderregel (ausgelaufen 2020) | Wärmespeicher ≥ 60 l je kW_th, ab 26,7 kW_th genügen 1 600 l |
| Verordnungen (EU) 812/2013 und 814/2013; BDH-Infoblatt Nr. 74 (August 2020, [PDF](https://www.bdh-industrie.de/fileadmin/user_upload/Downloads/Infoblaetter/Infoblatt_Nr_74_Energetische_Bewertung_Warmwasserspeicher.pdf)); [eur-lex 812/2013](https://eur-lex.europa.eu/legal-content/DE/TXT/HTML/?uri=CELEX%3A02013R0812-20180426) | Ökodesign | Warmhalteverluste S = 16,66 + 8,33·V^0,4 W als Grenze (seit 09/2017, bis 2 000 l, auch Heizungs- und Kombispeicher), Klassen A+–G, Prüfung bei 45 K |
| § 14a EnWG mit BNetzA-Festlegungen (ab 01.01.2024; [Verbraucherzentrale](https://verbraucherzentrale-energieberatung.de/erneuerbare-energien/photovoltaik/steuerbare-verbrauchseinrichtung/), [BNetzA](https://www.bundesnetzagentur.de/DE/Vportal/Energie/SteuerbareVBE/artikel.html?nn=877500)) | Gesetz/Festlegung | Dimmung auf mindestens 4,2 kW, höchstens 2 h je Tag präventiv, Bestandsvereinbarungen (3 × 2 h) längstens bis 31.12.2028. **Nach Anwenderentscheid 01.10.2026 für die Pufferauslegung nicht relevant** (K5 und V5 entfallen, E-P12) |
| Dongellini, Morini, *On-off cycling losses of reversible air-to-water heat pump systems as a function of the unit power modulation capacity*, Energy Conversion and Management 196 (2019) 966–978, doi 10.1016/j.enconman.2019.06.022 | Fachaufsatz | Taktverluste senken den SPF einstufiger Geräte um bis zu 12 %; modulierende Geräte vermeiden sie weitgehend |
| Bagarella, Lazzarin, Noro, *Sizing strategy of on–off and modulating heat pump systems based on annual energy analysis*, Int. J. Refrigeration (2016), doi 10.1016/j.ijrefrig.2016.02.015; Conti, Franco, Bartoli, Testi, *A design methodology for thermal storages in heat pump systems to reduce partial-load losses*, Applied Thermal Engineering (2022), doi 10.1016/j.applthermaleng.2022.118971; Ali, Perticaroli, Marra, Pepe, Zanoli, *Impact of Buffer Sizing on Heat Pump Performances for Residential Applications*, ICCC 2025, doi 10.1109/iccc65605.2025.11022836 | Fachaufsätze (nur Abstract/Titel gelesen) | Puffergröße gegen Teillast- und Taktverluste; Kennzahlen Zyklen, Abtauzyklen, Ölrückführung |
| BaCoGa, *Ermittlung des Wasserinhaltes* ([PDF](http://www.bacoga.com/wp-content/uploads/2016/07/Ermittlung-Wasserinhalt.pdf)) | Herstellertabelle, produktneutral | Wasserinhalt je 1,16 kW: Konvektoren 6 l, Plattenheizkörper 10 l, Radiatoren 14 l; Fußbodenheizung 150 l je 100 m² |
| energie-experten.org ([VDI 4645](https://www.energie-experten.org/heizung/waermepumpe/planung/vdi-4645), [Solarthermie](https://www.energie-experten.org/heizung/solarthermie/solarthermieanlage/auslegung)), baunetzwissen.de ([Pufferspeicher](https://www.baunetzwissen.de/heizung/fachwissen/speicher/dimensionierung-von-pufferspeichern-161296)), heizung.de ([Modulation](https://www.heizung.de/ratgeber/diverses/modulation-regulierung-der-heizleistung.html)) | Sekundärquellen | VDI 4645 Abschnitt 8.8.4 nenne 20 l/kW (nicht am Normtext geprüft); Solar 50 l/m² Flach-, 60–70 l/m² Röhrenkollektor; Gas moduliert 1:6, Öl 1:4 — auf Anwenderhinweis am 01.10.2026 breiter ausgewertet (Warmwasser, Pufferspeicher, Taktung, Sperrzeiten, VDI-Whitepaper, Festbrennstoff nach DIN EN 303-5): Runde 3, Abschnitt 7, V40–V43, E-P29 |

**VDI 4645:** Das Hauptblatt liegt seit dem Abend des 01.10.2026 als Entwurf März 2026 unter
`Quellen/Waermespeicher-Tool/` und ist in Runde 3 (Abschnitt 2) gegengelesen: 3 l/kW Vorprüfung (7.8.3),
Faustwerte 20 l/kW Fixed-Speed und 3 l/kW leistungsgeregelt (7.8.4), Mindestlaufzeit nach Gleichung 22,
Abschaltzeiten nach Gleichung 23 mit Trägheits-Gutschrift (Tabellen 14 und 15); Vorgaben zu
Schalthäufigkeiten enthält sie nicht (so auch der Fraunhofer-Bericht). Die VDI-MT 4645 Blatt 1:2023-04
ist das Qualifikationsblatt ohne Auslegungsregeln. Die älteren Norm-PDFs im selben Ordner (DIN 4708,
DIN EN 12831-3, DIN V 18599-10, VDI 6002) betreffen Trinkwarmwasser und Lastprofile, nicht den
Heizpuffer; prEN 15316-5:2024 (Schichtenmodell) und DIN EN 15332 mit A1 (Prüfung des
Nennwärmeverlusts) sind in Runde 3 ausgewertet.


## 2 Bestand in EPOS-Plan (Befund des Sonnet-Agenten, Zweig `ios_migration_september`, Schemastand 158)

### 2.1 Was der Puffer heute kann

- **Datenmodell:** `Tab_Pufferspeicher` (Projekt, 28 Spalten) trägt Gesamtvolumen, Vorlauf/Rücklauf
  (INT °C), `Schwelle_Ein` (10 %), `Schwelle_Aus` (95 %), `Schwelle_Aus_Nachrang` (Solar 30 %),
  `Schwelle_Reserve` (BHKW), Nutzungsklassen, Schichten, Lade-/Entladeleistung;
  `Tab_Pufferspeicher_STAMM` (13 Katalogzeilen, 101–3 000 l) nur Volumen, Bereitschaftsverlust
  [kWh/24 h], Investitionskosten; Katalogbezug allein über den Bezeichner (kein `ID_Stamm`).
  Zuordnung zum Erzeuger über `Z_AnlageSenke` (Ziel, Rang, Ladeprio). Modell `PufferSpModel.cs`;
  Temperaturen und Schwellen liest `WaermesenkeClass.PufferInfo`.
- **Rechenweg:** Kapazität `Q_max = V · 1,16 · ΔT / 1000` (`SimulationPufferspeicher.cs:415`;
  Rückfall ΔT 10 K). **Die Konstante ist 1,16 Wh/(l·K)**, nicht 1,163 wie im Tool und im
  Zapfprofil-Mengengerüst. Hysterese über `SchwelleEin`/`SchwelleAus`, Laden/Entladen in der
  `Kaskadenschleife` je Stunde, Verluste je Stunde anteilig am SOC, Vollzyklen im Ergebnis
  (`Tab_ErgebnisPufferspeicher`). Stundenreihen SOC/Ladung/Entladung liegen am Objekt, als CSV nur im
  Referenzlauf.
- **Zählung:** **keine Starts, Takte oder Laufzeiten am Puffer oder an der Wärmepumpe.** Der Kessel
  zählt seit R29 (`Starts_Spk`, `Taktstunden_Spk`, `Anfahrverlust_KWh_Spk`) aus `Mindestleistung`,
  `Mindestlaufzeit_min`, `Anfahrverlust_kWh` (Normvorgaben: P_min 0,3·P_nenn Gas-Brennwert, sonst
  0,6; Mindestlaufzeit 10 min); das BHKW kennt Modulationsgrenze (`Grenzleistung`, Vorgabe 30 %),
  aber keine Mindestlaufzeit.
- **Wärmepumpe:** Kennfeld (COP, P_th je Vorlauf und Außentemperatur), Betriebsmodus
  Laufzeit/Leistung/PV, Bivalenz (alternativ/parallel/teilparallel), **ein** Sperrfenster je Anlage
  (`Sperrzeit_von`/`_bis`, Stunden, ohne Mitternachtsübertrag, P = 0). **Keine Felder für
  Mindestleistung, Mindestlaufzeit, Abtauung.** Das Wiki sagt es ausdrücklich („Takten,
  Mindestlaufzeiten und Abtauverluste rechnet EPOS-Plan nicht“).
- **Solarthermie:** Kollektorsatz mit Aperturfläche, Modulanzahl, Nachrang-Schwelle am Puffer.
- **Brennstoffe:** `Tab_Brennstoff_Stamm` kennt Holz (ID 12) und Pellets (ID 15); ein
  Festbrennstoffkessel ist also als Kessel mit diesem Brennstoff abbildbar.

### 2.2 Das Vorbild: TWW-Speicherauslegung (Zapfprofilgenerator)

`EPOS.Kern/Allgemein/Zapfprofil/TwwSpeicherauslegung.cs` (796 Zeilen): Verfahrensvergleich
(`Verfahrensvolumen` je Verfahren mit Gültigkeit, Band, Kennwert, Rechenweg), `Nenninhaltsliste`
(14 Nenninhalte, Raster über dem Listenende, Anwenderliste vor Vorgabe), Warnliste als
`Auslegungshinweis(Code, ZapfSatz, Warnung)` mit Ressourcentexten, Vorgabewerte als Schlüssel in
`Tab_TwwParameter_STAMM` (24 `Speicherauslegung.*`-Zeilen, Herkunftsart, Beleg), gesät aus dem
**freien Paketteil** (`Referenzlaeufe/Katalogpaket_frei/*.csv`, `TwwPaketteilCtrl.Nachladen`,
Wachen), Projektwerte in `Tab_TwwProjekt` (NULL = Vorgabe), Herkunftsmarken
`Wertstatus` Vorgabe|Ueberschrieben|Kalibriert|Umgerechnet (`Herkunftsprotokoll.cs`). Dialog als
Überlagerung im Zapfprofil-Dialog, 1 692 Zeilen, mit Karte „Herkunft“.
**Korrektur zum Auftrag:** `SpeicherAuslegungCtrl*` ist die Stromspeicher-Flottenauslegung (JSON
in `Tab_SpeicherAuslegung.Daten`), nicht das TWW-Muster; Schemaschritt 155 heißt
`TwwFuellstandSchema`.

### 2.3 Die zwei Einstiege im Bestand

- **Kachel „Pufferspeicher“** (`Kachelschluessel.Pufferspeicher`, Startseite Reiter Erzeuger) führt
  über die Plattformhülle in `PufferspeicherDialog.razor` (Zweispaltenauswahl Projektliste/Katalog) —
  **eine Kachel hat heute genau eine Klickwirkung**; auf iOS gibt `IProjektQuelle.StartseiteGaben`
  Standard null.
- **① Konfiguration:** „Pufferspeicher anlegen / verwalten…“ öffnet die Überlagerung
  `PufferSpProjektDialog`; „Stromspeicher auslegen…“ ist nur sichtbar, wenn `Dienste.AuslegungOeffnen`
  belegt ist (Hausregel „kein Delegat, kein Knopf“), und öffnet über
  `Navigationsziel.OeffneMaske(Seitenschluessel.StromspeicherAuslegung)` die **freie Ansicht** in
  `AppWurzel` (Rückwegstapel `_rueckweg`, Tiefe 3, „← zurück“ → `ZurueckZumAufrufer`). Dieses Muster
  trägt beide Plattformen.


## 3 Das Wärmespeicher-Tool im Detail

| Baustein | Fundstelle | Inhalt |
|---|---|---|
| Vorgaben | `models.py:159–168` (`BufferParams`) | P_WP 50 kW, P_WP,min 15 kW, P_bivalent 0, ΔT 10 K, Mindestlaufzeit 10 min, Abtauung 20 l/kW (0 bei Sole/Wasser), Sperrzeiten als Liste (Start, Dauer), Ziel-Deckungsgrad 1,0 |
| Raster | `models.py:22` | 100, 150, 200, 300, 400, 500, 800, 1 000, 1 500, 2 000, 3 000, 5 000, 8 000, 10 000 l; darüber auf 100 l gerundet (`round_to_standard_volume`) |
| Umrechnung | `models.py:26` | V = Q·1000/(1,163·ΔT) |
| A Abtauung | `sizing_buffer.py:81` | V = v_spez · P_WP |
| B Taktung | `sizing_buffer.py:86` | V = P_min · t_min/60 / (1,163·ΔT) · 1000 |
| C Sperrzeit | `sizing_buffer.py:92` | je Fenster: höchstes rollierendes Mittel des Lastgangs über die Sperrdauer × Dauer; ohne Lastgang P_WP als Ersatz; Maximum über alle Fenster |
| D Lastgang-Simulation | `sizing_buffer.py:211`, `storage_sim.find_min_capacity` | **Durchlauf**-Bilanz SOC(t+1) = clip(SOC + (P_verf − Q)·Δt, 0, C) mit P_gen = P_WP + P_bivalent und Sperrmaske; Bisektion (Toleranz 1 kWh, bis 50 000 kWh) auf den **Ziel-Deckungsgrad**; ungültig, wenn selbst die Obergrenze nicht reicht — dann nicht bemessend |
| Empfehlung | `sizing_buffer.py:281` | Maximum der aktiven Kriterien, aufs Raster gerundet; Praxisgrenze 100 000 l mit Warnung und Auswegen |
| Betriebssimulation | `storage_sim.simulate_zweipunkt` | Zweipunkt: Laden mit P = min(Q + (C−SOC)/Δt, P_max) bis voll, Entladen ohne Erzeuger bis Mindestfüllstand (20 % von C), Umschalten im selben Schritt, Unterdeckung nur im Laden, `ladezyklen` = Wechsel Entladen→Laden; validiert gegen die SWSG-Excel (8 760 h identisch, 911 Zyklen) |
| Tests | `tests/test_sizing.py` (Handrechnungen A–C, Empfehlung, Raster), `tests/test_zweipunkt.py` (Konstantlast, Trigger-Schritt, Unterdeckung, Maske) | übernehmbar als C#-Fälle |

**Zwei Dinge, die das Tool nicht tut** (Dokumentation C.8): keine Schichtung, keine Speicherverluste
in der Bilanz, keine Hysterese in der Auslegung (nur in der Betriebssimulation), keine Modulation beim
Laden (der Erzeuger lädt immer mit P_max), kein Start-Ziel als Auslegungskriterium.


## 4 Fachliche Recherche: Kriterientabelle

Legende Aufnahme: **A** = ins Konzept als Kriterium, **V** = als Vorgabewert/Vorlage, **H** = als
Hinweis/Warnung, **—** = nicht übernommen.

| Nr. | Kriterium | Formel / Regel | Quelle | Vorgabewert | Einsatzgrenze | Aufnahme |
|---|---|---|---|---|---|---|
| K1 | Mindestanlagenvolumen ohne Puffer | V_Anlage,dauerhaft durchströmt ≥ 3–5 l/kW Heizleistung | BWP 2016 (3–5 l/kW); VdZ 2025 (3 l/kW) | 3 l/kW | nur Prüfung, ob überhaupt ein Puffer nötig ist; Herstellerangabe geht vor | **A** (als Vorprüfung, neu) |
| K2 | Abtauung (Luft/Wasser-WP, Umkehrabtauung) | V = v_spez · P_WP | Tool C.6.1 (20 l/kW, 15–35); BWP 2016 („Abtauenergie nach Herstellerangabe“); VDI 4645 8.8.4 laut Sekundärquellen 20 l/kW | 20 l/kW | entfällt bei Sole/Wasser, Wasser/Wasser und bei Abtauung aus dem Gerät | **A** (wie Mockup) |
| K3 | Taktung / Mindestlaufzeit | V = P_min · t_min / (c · ΔT · (s_aus − s_ein)) | Tool C.6.2 (ohne Hysterese-Anteil); VdZ 2025 (Mindestlaufzeit „bis zu 10 Minuten“); WP-QS 2025 (Ölrückführung, Hersteller 6–12 Starts/h) | t_min 10 min WP, 10 min Kessel (EPOS-Vorgabe), 60 min BHKW; P_min = Modulationsgrenze (WP 30 %, BHKW `Grenzleistung`, Kessel `Mindestleistung`) | Ein/Aus-Geräte: P_min = P_nenn | **A** (Mockup: 20 min → 10 min; Hysterese-Anteil ergänzen) |
| K4 | EVU-Sperrzeit (Wärmepumpentarif, neutrale Eingabe) | V = Q̄_sperr(t_sperr) · t_sperr / (c·ΔT·(s_aus−s_ein)), Q̄ = höchstes rollierendes Mittel des Lastgangs | Tool C.6.3; BWP 2016: 30–40 l/kW P_WP je Sperrzeit (Faustwert) | Sperrprofil als Eingabe: Sperrdauer und Lage je Tag (Vorlagen „keine“, „2 × 2 h“, „3 × 2 h“, „eigenes“), ohne Rechtsbezug und ohne Enddatum-Hinweis (Anwenderentscheid E-P12) | entfällt, wenn ein Zweiterzeuger in der Sperre freigegeben ist; Teildeckung mit Gebäudeträgheit nur Fußboden/schwere Bauweise | **A** (wie Mockup) |
| K5 | ~~Netzorientierte Steuerung nach § 14a EnWG~~ | — | — | — | **entfällt nach Anwenderentscheid 01.10.2026** („§ 14a EnWG betrifft nicht den Wärmepuffer — nicht relevant“, E-P12); die Sperrzeit bleibt allein K4 als neutrale Eingabe; Nummer bleibt reserviert | — |
| K6 | Deckung bei Erzeuger unter Spitzenlast | Durchlauf-Bilanz, Bisektion auf Ziel-Deckungsgrad | Tool C.6.4 (validiert gegen konstruierte Lastfälle) | Ziel 100 %; Praxisgrenze 100 m³ | nur bemessend, wenn P_gen < Spitzenlast oder Sperrmaske aktiv; monovalent + 100 % → Saisonalspeicher-Dimension | **A** (im Mockup fehlt es — siehe 5) |
| K7 | Taktziel (Starts je Tag) | kleinstes V, mit dem die Betriebssimulation das Startziel hält | WP-QS 2025: 540–15 820 Starts/a, 90 % < 5 500, ein Drittel ≤ 2 000 („sehr moderat“); Hersteller 6–12 Starts/h; OTH 2017 (BHKW): 160–672 Starts/a, 9–31 Betriebsstunden je Start; Hersteller begrenzen Starts/Tag | WP ≤ 6/Tag (≈ 2 000/a), Warnung > 15/Tag (≈ 5 500/a); BHKW ≤ 2/Tag; Kessel ≤ 12/Tag (EPOS-Taktzählung) | Zweipunkt mit P_max lädt zu schnell für modulierende Geräte → Modulationsmodus (Abschnitt 6, V4) | **A** (Mockup-Kriterium D, umbenannt; neu gegenüber dem Tool) |
| K8 | Faustwerte je kW | WP 20–25 l/kW (Laufzeit), 30–40 l/kW (Sperrzeit); BHKW 60 l/kW_th; Kessel nichtmodulierend 30–50 l/kW; Festbrennstoff 55/20/30 l/kW | BWP 2016; Mini-KWK 2015; OTH 2017 (Anlagen mit 24–99 l/kW_th); Sekundärquellen für Kessel | je Vorlage | nur Gegenprobe, nie bemessend (wie Mockup E) | **V/H** |
| K9 | Gesetzliches/förderrechtliches Mindestvolumen Festbrennstoff | V ≥ 55 l/kW handbeschickt (12 l je Liter Füllraum Altanlagen), ≥ 20 l/kW automatisch (1. BImSchV); BEG: 30 l/kW Pellets/Hackschnitzel, 55 l/kW Scheitholz | 1. BImSchV § 5 Abs. 4; BAFA BEG EM | nach Brennstoff des Kessels (Holz → 55, Pellets → 30 für Förderung, 20 gesetzlich) | nur Kessel mit Brennstoff Holz/Pellets | **A** (neu, als Kriterium „Mindestvolumen nach Vorschrift“, bemessend) |
| K10 | Solarthermie | V = 50 l/m² Flachkollektor, 60–70 l/m² Röhrenkollektor (Aperturfläche); Normbezug DIN EN 12977 | energie-experten, baunetzwissen (Sekundär); DIN EN 12977 nur Verweis | 50 l/m² | Tagesspeicher; Nachrang-Schwelle aus dem Projekt | **A** (Vorlage Solar, ersetzt dort K2/K3) |
| K11 | Bereitschaftsverlust | S_max = 16,66 + 8,33·V^0,4 W (Klasse C-Grenze, Ökodesign seit 09/2017 bis 2 000 l, auch Heizungs- und Kombispeicher); Klassen: A+ < 5,5+3,16·V^0,4 … G ≥ 31+16,66·V^0,4; 1 W = 0,024 kWh/d | 814/2013, 812/2013 Anhang II, BDH 74 | Katalogsatz vor Formel; ohne Katalogsatz Klasse-C-Grenze (1 000 l → 3,6 kWh/d, 2 000 l → 4,6 kWh/d) | Prüfung bei 45 K; Heizpuffer laufen kühler → Formel ist Obergrenze | **V** (Vorgabe mit Herkunft „Ökodesign“), **H** (Katalogsatz ohne Verlust) |
| K12 | Hydraulische Einbindung | Parallel-/Trennspeicher: Entkopplung, Mischverluste meist < 2 K, bis 5 K bei Volumenstrom Heizkreis > WP-Kreis oder vertauschten Anschlüssen; Reihenspeicher (+ Überströmventil): nur Volumen, keine Entkopplung | WP-QS 2025 (17 parallel, 7 seriell; 10 von 17 < 1,2 K, 4 von 17 2,4–5 K); VdZ 2025; BWP 2016 | Einbindung „parallel“ als Vorgabe; nutzbares ΔT parallel = Vorlauf − Rücklauf, Reihe im Rücklauf ≈ Hysterese (≈ 5 K) | Reihenpuffer verdoppelt rechnerisch das Volumen der Energiekriterien | **V/H** (Experten-Feld „Einbindung“, Hinweistext) |
| K13 | Taktverluste in der Effizienz | SPF −12 % bei einstufigen Geräten über die Saison; modulierende vermeiden Taktverluste weitgehend | Dongellini & Morini 2019 | — | Begründung des Taktziels, kein Rechenwert | **H** |
| K14 | Anlagenwasserinhalt (Gutschrift) | je 1,16 kW: Konvektoren 6 l, Plattenheizkörper 10 l, Radiatoren 14 l; FBH 150 l je 100 m², gemischt 350 l je 100 m² | BaCoGa (produktneutral) | 0 l Gutschrift (vorsichtig); Experte: Vorlage nach Übergabeart | nur der dauerhaft durchströmte Teil zählt (Thermostatventile!) | **V** (Expertenfeld) |
| K15 | Modulation Gas/Öl | Gas-Brennwert 1:6, Öl-Brennwert 1:4 | heizung.de (Sekundär); EPOS `Kesselkennlinie` 0,3/0,6 | EPOS-Vorgaben | Puffer bei Kessel nur gegen Takten nichtmodulierender Geräte | **V** (Vorlage Kessel) |
| K16 | Nenninhalte | 100 … 10 000 l (14 Stufen), Raster 1 000 l darüber | Tool; TWW-Speicherauslegung (`Speicherauslegung.Nenninhalt.*`) | gleiche Liste wie TWW | — | **V** (Schlüssel wiederverwenden) |

**Nicht übernommen:** die Faustformel „Volumen = Leistung × Pufferzeit × 860/ΔT“ (identisch mit
K3/K4, nur andere Konstante), Sekundärwerte „50–80 l/kW Reihenpuffer“ (unbelegt; ergibt sich aus
K12 rechnerisch), „Wohnfläche 2–3 l/m²“ (kein Bezug zu Erzeuger und Lastgang).


## 5 Prüfung des Entwurfs (Mockup, Stand 30.09.2026) gegen Tool und Recherche

| Nr. | Befund | Bewertung | Folge für Konzept und Mockup |
|---|---|---|---|
| P1 | Mockup-Kriterium **D „Betriebssimulation ≤ 6 Starts/Tag“** ist nicht das Tool-Kriterium 4. Das Tool bemisst mit der **Durchlauf-Bilanz auf Deckungsgrad** (K6); die Zweipunkt-Simulation ist dort nur Betriebsbild eines gewählten Speichers | Das Mockup lässt K6 aus — bei monovalenter Wärmepumpe unter Spitzenlast oder mit Sperrmaske ist K6 aber das bemessende Kriterium (Tool C.6.6). Das Startziel ist eine echte Ergänzung, keine Ersetzung | **beide aufnehmen:** D1 Deckung (K6, validiert), D2 Taktziel (K7, neu). Mockup: Kriterienkarte D teilen, Ergebnisblatt erweitert |
| P2 | Konstante **1,163** (Tool, Mockup „11,63 kWh je 1 000 l“) gegen **1,16** im Kern (`SimulationPufferspeicher`, Wiki, `ProjektPuffer`) | Die Auslegung muss dieselbe Kapazität zeigen wie Dialog und Simulation, sonst widersprechen sich Zahlen in derselben Maske (0,26 %) | Auslegung rechnet mit **1,16** (`ProjektPuffer.WH_JE_LITER_KELVIN`); Tool-Testwerte mit Toleranz 0,5 % übernehmen; Mockup „11,6 kWh“ |
| P3 | **Nutzbarer Anteil 85 %** als eigener Parameter | 85 % = `Schwelle_Aus − Schwelle_Ein` (95 − 10) des Projektpuffers. Ein zweiter Parameter wäre eine Dublette | Anteil **aus den Schwellen ableiten**, nicht eingeben; Expertenfeld entfällt; gilt für K3, K4, K5, K6, K7, nicht für K2, K9, K10 |
| P4 | **Mindestlaufzeit 20 min** in der Vorlage Wärmepumpe | Tool 10 min; VdZ „bis zu 10 Minuten“; fixed-speed 3–10 min (Sekundär) | Vorgabe **10 min**, Beispiel „3–10 min nach Herstellerangabe“; BHKW 60 min; Kessel 10 min aus `Kesselkennlinie` |
| P5 | **Sperrzeitprofil 3 × 2 h** als Regelfall der Vorlage Wärmepumpe; das Mockup führte zusätzlich „§ 14a-Dimmung auf 4,2 kW“ als Profil | Anwenderentscheid 01.10.2026: § 14a EnWG ist für den Wärmepuffer nicht relevant — die Dimmung entfällt als Profil und als Kriterium (K5) | Profile neutral: „keine“ (Vorgabe), „2 × 2 h“, „3 × 2 h“, „eigenes“ — ohne Rechtsbezug und ohne Enddatum-Hinweis; die Profilzeile des Mockups ist angepasst |
| P6 | Die **Übernahme** schreibt nichts in die Sperrzeit der Wärmepumpe — der Kern kennt **ein** Fenster ohne Mitternachtsübertrag und keine Dimmung | Auslegung und Jahressimulation können zu verschiedenen Sperrannahmen rechnen | Konzept: Hinweis im Ergebnis („Simulation rechnet mit Sperrfenster … “); **Folgeauftrag** Sperrprofil und Dimmung im Kern (Schema `Tab_Energieanlagen`, `SimulationWaermepumpe.cs:1384`) — nicht P1 |
| P7 | **Startziel 6/Tag** ohne Quelle | WP-QS: ein Drittel der Anlagen ≤ 2 000 Starts/a (≈ 5,5/Tag) „sehr moderat“, 90 % < 5 500 (≈ 15/Tag); Hersteller 6–12/h | Vorgabe 6/Tag belegt; **Warnschwelle 15/Tag** ergänzen; BHKW 2/Tag (OTH), Kessel 12/Tag |
| P8 | **Abtauung 20 l/kW** | Tool, BWP (Herstellerangabe), VDI 4645 (Sekundär) stimmen überein | bleibt; Herkunft „BWP 2016 / Tool“ |
| P9 | **Keine Prüfung des Anlagenvolumens** (K1) | BWP/VdZ: ab 3–5 l/kW dauerhaft durchströmt braucht eine modulierende WP mit Flächenheizung oft keinen Puffer | Schritt 1 bekommt die Frage „Anlagenvolumen dauerhaft durchströmt?“ mit Vorlage je Übergabeart (K14); Ergebnis darf „kein Puffer erforderlich“ lauten |
| P10 | **Festbrennstoff fehlt** in den Vorlagen | 1. BImSchV und BEG schreiben Mindestvolumen vor; EPOS kennt Holz und Pellets als Brennstoff | sechste Vorlage „Festbrennstoffkessel“ mit K9 als bemessendem Kriterium; Vorlage „Kessel mit Taktbegrenzung“ bleibt für Gas/Öl |
| P11 | **Gebäudeart (EFH/MFH/Gewerbe)** als eigene Vorlagen-Dimension | Der Lastgang kommt aus dem Projekt; die Gebäudeart ändert nur Beispielwerte | Gebäudeart nur als Beispielhilfe („z. B. MFH 40–80 kW“), keine eigene Vorgabenzeile → 6 statt 15 Vorlagen |
| P12 | **Hydraulische Einbindung** fehlt | Mischverluste bis 5 K, Reihenpuffer ohne Entkopplung (WP-QS, VdZ) | Expertenfeld „Einbindung: parallel (Trennspeicher) / Reihe im Rücklauf“, wirkt auf ΔT_nutz und auf den Hinweistext zur Vorlauftemperatur |
| P13 | **Bereitschaftsverlust 3,5 kWh/d** im Beispiel ohne Herkunft | Ökodesign-Grenze 2 000 l = 4,6 kWh/d (Klasse C), Katalog 1 000 l = 2,5–3,8 | Vorgabe = Klasse-C-Grenze mit Herkunft „Ökodesign 814/2013“, Katalogsatz überschreibt |
| P14 | Beispiel „Ein/Aus-Gerät 1 720 l“ bei K3 | richtig (P_min = P_nenn) | bleibt |
| P15 | **Raster** 14 Stufen | identisch mit TWW-Schlüsseln `Speicherauslegung.Nenninhalt.Liste.1–14`, Raster 1 000 l | Schlüssel **wiederverwenden**, keine zweite Liste |
| P16 | **Kachel-Einstieg** mit zwei Knöpfen | Startseiten-Kacheln sind Daten mit einer Klickwirkung | Entscheid: (a) Kachel bekommt zweite Wirkung (Baustein erweitern), (b) Knopf „Auslegen…“ im `PufferspeicherDialog`, den die Kachel öffnet, (c) eigene Kachel „Pufferauslegung“. **Empfehlung (b):** erfüllt „unter der Kachel Pufferspeicher“, kein neuer Baustein, beide Plattformen |


## 6 Verbesserungsvorschläge (Optimierungen) mit Empfehlung

| Nr. | Vorschlag | Nutzen | Aufwand / Stufe | Empfehlung |
|---|---|---|---|---|
| V1 | **Zwei Simulationskriterien** D1 Deckung (Durchlauf, Tool) und D2 Taktziel (Zweipunkt-Bisektion) | D1 deckt Spitzen- und Sperrfälle (validiert), D2 das Taktverhalten; Ergebnisblatt zeigt beide Volumina | P1, +0,5 PT | ja |
| V2 | **Hysterese aus dem Projektpuffer** statt Parameter „nutzbarer Anteil“: alle Energiekriterien durch (s_aus − s_ein) teilen; Betriebssimulation mit denselben Schwellen (Ein 10 %, Aus 95 %) statt fest 20 % Mindestfüllstand | Auslegung und Jahressimulation rechnen mit einem Satz Schwellen; ein Parameter weniger | P1, 0 PT (Vereinfachung) | ja |
| V3 | **Konstante 1,16** aus `ProjektPuffer.WH_JE_LITER_KELVIN` wiederverwenden | keine Zahlenwidersprüche in der Maske | P1, 0 PT | ja |
| V4 | **Modulationsmodus der Betriebssimulation:** im Zustand Laden P = clamp(Q + (C − SOC)/Δt, P_min, P_max) statt P_max; Ein/Aus und BHKW taktend bleiben Zweipunkt mit P_max | Zweipunkt mit Volllast lädt für Inverter-Geräte zu schnell und überschätzt Starts; der Modus gibt Starts nur dort, wo die Last unter P_min fällt | P1, +0,5 PT; Expertenschalter, Vorgabe nach Vorlage | ja — mit dem Hinweis des Feldtests, dass die Regelung mehr Einfluss hat als die Modulation |
| V5 | ~~§ 14a-Kriterium K5 mit Restleistung~~ | — | — | **entfällt** (Anwenderentscheid 01.10.2026, E-P12) |
| V6 | **Vorprüfung Anlagenvolumen (K1) mit Gutschrift (K14)**: Ergebnis „kein Puffer erforderlich, wenn V_Anlage ≥ 3 l/kW und Flächenheizung ohne Einzelraumregelung“ | verhindert unnötige Puffer (Kosten, Verluste, Vorlaufanhebung) | P1 Kern 0,25 PT, P2 Dialog 0,25 PT | ja; Gutschrift vorsichtig (Vorgabe 0 l), Experte |
| V7 | **Vorlage Festbrennstoffkessel** mit K9 (BImSchV/BEG) aus dem Brennstoff des Projektkessels | Rechtssicherheit; automatisch richtig, wenn Brennstoff Holz/Pellets | P1, +0,25 PT | ja |
| V8 | **Vorlage BHKW** mit Laufzeitziel (K3 mit t_min 60 min), Faustwert 60 l/kW_th (Mini-KWK), Startziel 2/Tag; Wiederverwendung `PufferSpCtrl.PendelspeicherVolumenLiter` prüfen | belegte Werte; kein Doppelrechenweg zum Pendelspeicher | P1, +0,25 PT | ja |
| V9 | **Vorlage Solarthermie** mit K10 aus Aperturfläche × Modulanzahl des Projekts; Nachrang-Schwelle aus dem Puffer | nutzt vorhandene Projektdaten | P1, +0,25 PT | ja |
| V10 | **Einbindung parallel/Reihe** als Expertenfeld mit ΔT_nutz-Wirkung und Hinweistext (Mischverluste, Vorlaufanhebung) | Planungsqualität; belegt durch Feldtest | P1 0,25 PT, P2 0,25 PT | ja |
| V11 | **Bereitschaftsverlust-Vorgabe nach Ökodesign** (Klasse-C-Grenze) mit Herkunft; Katalogsatz überschreibt; Warnung bei Katalogsatz ohne Verlust | Übernahme braucht einen Verlustwert für `Tab_Pufferspeicher` | P1, 0,25 PT | ja |
| V12 | **Lastgang-Quelle:** zuerst die Stundenreihen des letzten Simulationsergebnisses (am Objekt), sonst Bedarfsreihen ohne Erzeuger; Beispiel-Lastgang nur als Rückfall mit Warnung | Auslegung ohne neuen Simulationslauf | P1, Klärpunkt: ob die Stundenreihen persistiert sind (`Tab_Ergebnis*` ohne Reihen) | Klärung am 03.10. vor W2 |
| V13 | **Startzähler Wärmepumpe und BHKW in der Jahressimulation** (wie `Starts_Spk` des Kessels) und Knopf „mit Jahressimulation nachrechnen“ | das Betriebsbild käme aus dem echten Rechenweg; Nachweis des Vorschlags | eigener Auftrag, Rechenweg-neutral (nur Zählung), Referenzlauf: neue Ergebnisspalten sind kein Rechenwegeingriff, aber Byte-Vergleich (nur Information) ändert sich | später (nach P2), eigener Auftrag |
| V14 | **Sperrprofil im Kern** (`Tab_Energieanlagen` erweitern: mehrere Fenster, Mitternachtsübertrag; ohne Dimmung) | Übernahme könnte dann die Sperrannahme mitschreiben; Jahressimulation rechnet die Sperrzeiten | eigener Auftrag mit Schemaschritt und Einfrierregel-Prüfung (Referenzprojekte mit Sperrzeit?) | später, eigener Auftrag |
| V15 | **Gebäudeträgheit statt Pauschalanteil:** Sperrzeit-Teildeckung aus dem VDI-6007-Gebäudemodell (Raumtemperaturabfall bei abgeschaltetem Erzeuger) | EPOS-Plan hat das Modell; ersetzt den geschätzten „50 %“ | P3 oder später, 1–2 PT | später; Expertenanteil bleibt bis dahin |
| V16 | **Nutzen-Aufwand-Zeile** im Ergebnis: Mehrinvestition (Katalog `Investitionskosten` je Nenninhalt), Bereitschaftsverlust kWh/a, Starts je Tag — für 1 000 / 2 000 / 3 000 l nebeneinander | der Mockup zeigt den Vergleich schon als Knopf; Zahlen aus Katalog und Simulation vorhanden | P2, 0,5 PT | ja (nachrichtlich, keine Wirtschaftlichkeitsrechnung) |
| V17 | **Vorlagen-Dimension Gebäudeart streichen** (nur Beispielhilfe) | 6 statt 15 Vorlagenzeilen in der Saat | P1, −0,25 PT | ja |
| V18 | **Datenmodell:** eine STRICT-Tabelle `Tab_PufferAuslegung` (Zeile je Projektpuffer, NULL = Vorgabe) nach dem Muster `Tab_TwwProjekt`; Vorlagen und Vorgabewerte als Schlüssel `Pufferauslegung.*` im freien Paketteil — **im selben Mechanismus wie `Tab_TwwParameter_STAMM`** (Tabelle umbenennen oder Schwestertabelle gleichen Aufbaus) statt JSON wie `Tab_SpeicherAuslegung` | typisiert, wachbar, Herkunft je Wert, Saat über CSV | P1 W1; Entscheid Tabelle/Schwester am 03.10. | Schwestertabelle `Tab_Auslegungsparameter_STAMM` nur, wenn Umbenennen Referenz- oder Wiki-Stände berührt; sonst eine gemeinsame Parametertabelle |
| V19 | **Kachel-Einstieg** nach P16 (b): Knopf „Auslegen…“ im `PufferspeicherDialog`, zusätzlich ① Konfiguration | kein neuer Kachelbaustein | P2 | ja |
| V20 | **Warnliste mit Codes** wie `Auslegungshinweis` des Zapfprofils (Ressourcentexte, keine festen Sätze); Codes: KEIN_PUFFER_NOETIG, SPERRE_VON_KESSEL_GEDECKT, SPERRVERTRAG_LAEUFT_AUS_2028, PRAXISGRENZE, P_GEN_UNTER_SPITZE, STARTS_UEBER_WARNSCHWELLE, KATALOG_OHNE_VERLUST, REIHENPUFFER_DELTA_T, VDI_4645_NICHT_GEPRUEFT | Zweisprachigkeit, Wiederverwendung | P1 | ja |


## 7 Offene Entscheide für den Anwender (mit Empfehlung)

| Nr. | Frage | Empfehlung |
|---|---|---|
| E-P8 | Kriterium D in zwei Kriterien teilen (D1 Deckung nach Tool, D2 Taktziel)? | ja (V1) |
| E-P9 | Nutzbarer Anteil aus den Schwellen des Projektpuffers statt eigener Eingabe? | ja (V2) |
| E-P10 | Konstante 1,16 des Kerns auch in der Auslegung? | ja (V3) |
| E-P11 | Vorlagen: sechs Anlagentypen (Wärmepumpe monovalent, bivalent, BHKW, Kessel Gas/Öl mit Taktbegrenzung, **Festbrennstoffkessel**, Solarthermie), Gebäudeart nur als Beispielhilfe? | ja (V7, V17) |
| E-P12 | § 14a-Dimmung als eigenes Kriterium mit Restleistung; Bestandstarif 3 × 2 h nur als Profil mit Enddatum-Hinweis? | **entschieden 01.10.2026: nein** — § 14a EnWG ist nach Anwenderentscheid für den Wärmepuffer nicht relevant; K5 und V5 entfallen, die Sperrzeit bleibt als neutrale Eingabe K4 ohne Rechtsbezug und ohne Enddatum-Hinweis |
| E-P13 | Kachel-Einstieg als Knopf „Auslegen…“ im Pufferspeicher-Dialog (b) statt zweiter Kachelwirkung (a) oder eigener Kachel (c)? | (b) |
| E-P14 | Vorprüfung „kein Puffer erforderlich“ zulassen, wenn das Anlagenvolumen reicht? | ja (V6) |
| E-P15 | Startzähler und Sperrprofil im Kern (V13, V14) als eigene Folgeaufträge nach P2? | ja, nicht in P1 |
| E-P16 | VDI 4645 beschaffen und vor P2 gegenlesen? | **erledigt 01.10.2026**: Hauptblatt (Entwurf März 2026) liegt vor und ist in Runde 3, Abschnitt 2.3 gegengelesen — K1–K4 und K14 bestätigt bzw. präzisiert, V37–V39, E-P28 |
| E-P17 | Norm-PDFs und Herstellerunterlage unter `Quellen/Waermespeicher-Tool/` im Repositorium belassen oder entfernen (Hinweis vom 01.10.2026)? | **entschieden 01.10.2026: belassen** für Konzeption und Umsetzung (Repositorium wird nur vom Anwender genutzt), danach entfernen |


## 8 Folgen für den Bauplan P1 (03.10.2026)

1. **W1 Schema:** `Tab_PufferAuslegung` (STRICT, FK `ID_Projekt` CASCADE, `ID_Pufferspeicher` nullbar
   für „neu anlegen“, NULL-Spalten = Vorgabe) und Vorgabeschlüssel `Pufferauslegung.*` im freien
   Paketteil (Vorlagen V1–V6 als Schlüsselgruppen `Pufferauslegung.Vorlage.<Typ>.*`); Nenninhalte aus
   `Speicherauslegung.Nenninhalt.*` wiederverwenden. Nächster Schemaschritt **159** (vor dem Commit
   gegen `origin` messen). Entscheid V18 zur Parametertabelle zuerst.
2. **W2 Kern** `EPOS.Kern/Allgemein/Pufferauslegung/`: Kriterien K1–K4 (K5 entfällt, E-P12), K6 (Durchlauf +
   Bisektion), K7 (Zweipunkt + Modulationsmodus + Bisektion), K8 (Gegenprobe), K9, K10; Hysterese aus
   Schwellen; Konstante 1,16; Sperrprofile mit Mitternachtsübertrag; Raster;
   Praxisgrenze; Warnliste mit Codes; deterministisch.
3. **W3 Controller** `PufferAuslegungCtrl`: Vorbelegung aus Projekt (Erzeuger Rang 1 und 2 der
   Kaskade, Senken, Temperaturpaar und Schwellen des Puffers, Brennstoff des Kessels, Aperturfläche),
   Lastgang (V12), Vorlagen, Rechnen, Speichern, Übernahme (Volumen,
   Katalogsatz-Vorschlag mit nächstem Nenninhalt, Bereitschaftsverlust nach V11, Schwellen
   unverändert) als Ändern oder Neuanlegen über `PufferSpCtrl`.
4. **W4 Tests:** Python-Fälle aus `test_sizing.py` und `test_zweipunkt.py` (Konstante 1,163 →
   1,16, Toleranz 0,5 %), Mockup-Zahlen (60 kW, 18 kW, 10 min, ΔT 10 K: K2 1 200 l, K3 mit Hysterese
   0,85: 18 · (10/60) · 1000 / (1,16 · 10 · 0,85) = 304 l, K4 82 kW · 2 h: 16 630 l), K9 (30 kW
   Scheitholz → 1 650 l), Modulationsmodus gegen Zweipunkt (weniger Starts),
   Referenzlauf byte-gleich gegen R29.
5. **Mockup nachziehen:** Kriterienkarte D1/D2, 11,6 kWh, Mindestlaufzeit 10 min, Vorlage
   Festbrennstoff, Vorprüfung Anlagenvolumen, Profile nach P5, Einbindung, Herkunft des
   Bereitschaftsverlusts, Warnschwelle 15 Starts/Tag, Kachel-Einstieg nach E-P13.
6. **Nicht in P1:** V13, V14, V15, V16 (P2), Wiki (P3).


## 9 Quellenverzeichnis (abgerufen 01.10.2026)

- BWP, Leitfaden Hydraulik, Stand Juli 2016 — https://www.waermepumpe.de/uploads/media/BWP_LF_Hydraulik_final_web.pdf
- VdZ, Umsteigen auf die Wärmepumpe, Leitfaden für den Fachhandwerker (2025) — https://files.vdzev.de/pdfs/umsteigen-auf-die-waermepumpe/VdZ_Waermepumpen_WEB_Einzelseiten.pdf
- Fraunhofer ISE, Abschlussbericht WP-QS im Bestand, 25.11.2025 (2. Fassung) — https://www.ise.fraunhofer.de/content/dam/ise/de/documents/projekte/WP-QS_Schlussbericht.pdf
- Lechner, O'Connell, Meierhofer, Brautsch, Optimierte Monitoring-, Betriebs- und Regelstrategien für Blockheizkraftwerke, Zukunft Bau F 3058, OTH Amberg-Weiden 2017 — https://www.oth-aw.de/files/oth-aw/Forschung/Institute/kwk/Aktuelles/ZukunftBau_F3058.pdf
- 1. BImSchV § 5 — https://www.gesetze-im-internet.de/bimschv_1_2010/__5.html
- BAFA, Förderübersicht Biomasse (BEG EM) — https://www.bafa.de/SharedDocs/Downloads/DE/Energie/ew_biomasse_foerderuebersicht.pdf
- Mini-KWK-Richtlinie, BAnz AT 29.12.2014 B5 — https://www.bhkw-infozentrum.de/download/richtlinie_mini_kwk_2015.pdf
- Verordnung (EU) 812/2013, konsolidiert — https://eur-lex.europa.eu/legal-content/DE/TXT/HTML/?uri=CELEX%3A02013R0812-20180426
- BDH, Informationsblatt Nr. 74, Energetische Bewertung Warmwasserspeicher, August 2020 — https://www.bdh-industrie.de/fileadmin/user_upload/Downloads/Infoblaetter/Infoblatt_Nr_74_Energetische_Bewertung_Warmwasserspeicher.pdf
- Verbraucherzentrale Energieberatung, Steuerbare Verbrauchseinrichtung — https://verbraucherzentrale-energieberatung.de/erneuerbare-energien/photovoltaik/steuerbare-verbrauchseinrichtung/
- Bundesnetzagentur, § 14a EnWG Steuerbare Verbrauchseinrichtungen — https://www.bundesnetzagentur.de/DE/Vportal/Energie/SteuerbareVBE/artikel.html?nn=877500
- Dongellini, Morini (2019), Energy Conversion and Management 196, 966–978 — https://doi.org/10.1016/j.enconman.2019.06.022
- Bagarella, Lazzarin, Noro (2016), International Journal of Refrigeration — https://doi.org/10.1016/j.ijrefrig.2016.02.015
- Conti, Franco, Bartoli, Testi (2022), Applied Thermal Engineering — https://doi.org/10.1016/j.applthermaleng.2022.118971
- Ali, Perticaroli, Marra, Pepe, Zanoli (2025), 26th International Carpathian Control Conference — https://doi.org/10.1109/iccc65605.2025.11022836
- BaCoGa Technik, Ermittlung des Wasserinhaltes — http://www.bacoga.com/wp-content/uploads/2016/07/Ermittlung-Wasserinhalt.pdf
- energie-experten.org, Wärmepumpen-Planung nach VDI 4645 (Sekundär) — https://www.energie-experten.org/heizung/waermepumpe/planung/vdi-4645
- energie-experten.org, Auslegung und Größe von Solarthermie-Anlagen (Sekundär) — https://www.energie-experten.org/heizung/solarthermie/solarthermieanlage/auslegung
- baunetzwissen.de, Dimensionierung von Pufferspeichern (Sekundär) — https://www.baunetzwissen.de/heizung/fachwissen/speicher/dimensionierung-von-pufferspeichern-161296
- heizung.de, Modulation: Regulierung der Heizleistung (Sekundär) — https://www.heizung.de/ratgeber/diverses/modulation-regulierung-der-heizleistung.html
