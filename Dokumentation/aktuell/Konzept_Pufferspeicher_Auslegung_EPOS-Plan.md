# Konzept: Pufferspeicher-Auslegung — Heizungs-, Kombi-, Brauchwasser- und Prozesswärmepuffer

> **Rev. 1 (03.10.2026).** Auftrag des Anwenders vom 30.09./01.10.2026: ein Konzept zur Pufferspeicher-Auslegung mit
> umfassender Recherche, gekoppelt an das Zapfprofil, erreichbar über die Kachel Pufferspeicher und ① Simulation
> Konfiguration, geführt bedient mit Vorlagen und Beispielen; Bau der Stufe P1 (Kern und Schema) am selben Tag. Die
> Recherche liegt in vier Runden unter `Dokumentation/aktuell/Pufferspeicher/`; das Abnahme-Mockup ist
> `Dokumentation/aktuell/Mockups/Pufferspeicher_Auslegung_Mockup.html`. Die Entscheide des Anwenders vom 01.10.2026
> (E-P12, E-P17, E-P31, E-P32, Freigabe P1, Einstiege) stehen in 9; alle übrigen Entscheide der Recherche gelten nach
> Empfehlung (E60).
>
> **Rev. 2 (03.10.2026, nach dem Bau P1).** Schemaschritt **169** statt 167 und Basis **R33** statt R32, weil der Arbeitszweig am selben Tag die Schritte 167 (Teillast WP/BHKW, M4) und 168 (Strom in Viertelstunden, M5) und die Basis R33 vergeben hat. P1 ist gebaut; Festlegungen beim Bau, Abweichungen und Nachweise stehen im Protokoll [`2026-10-03_P1_Pufferauslegung.md`](../ueberholt/Protokolle/Simulation/2026-10-03_P1_Pufferauslegung.md), die Ergänzungen zu 4.1 in 10.


## 0. Das Ergebnis in zehn Punkten

1. **Ein Rechenweg je Speicherklasse, zusammengesetzt aus Zonen:** Der Projektpuffer trägt ein Klassen-Set aus
   Heizung, Brauchwasser und Prozess (Kombi = Heizung + Brauchwasser). Die Auslegung bemisst je Zone aus den
   Bedarfsreihen des Projekts und addiert die Zonen; die Brauchwasserzone kommt unverändert aus der
   Zapfprofil-Speicherauslegung (F1, F2).
2. **Zehn Kriterien für die Heizzone,** zwei Simulationskriterien dazu: Vorprüfung Anlagenvolumen (K1, 3 l/kW),
   Faustwert nach Gerätetyp (K2), Mindestlaufzeit (K3, VDI 4645 Gleichung 22), Sperrzeit (K4, VDI 4645 Gleichung 23
   mit Trägheits-Gutschrift; Tool-Weg aus dem Lastgang als Expertenvariante), Deckungsgrad (D1, Durchlauf mit
   Bisektion), Taktziel (D2, Zweipunkt mit Schwellen des Puffers), Gegenprobe (K8), Festbrennstoff (K9, 1. BImSchV/BEG
   und DIN EN 303-5), Solar (K10), Bereitschaftsverlust (K11). § 14a EnWG spielt keine Rolle (E-P12).
3. **Bemessend ist das größte Kriterium je Zone,** geteilt durch den nutzbaren Anteil aus den Schwellen des Puffers
   (`Schwelle_Aus` − `Schwelle_Ein`), die Zonen addiert, auf das Nenninhalts-Raster gerundet und an der Praxisgrenze
   gestoppt; Konstante 1,16 Wh/(l·K) wie im Kern (F3).
4. **Die Richtlinie ist der Standard, das Tool die Expertenvariante:** Wo VDI 4645 (Entwurf 2026-03) einen Weg vorgibt,
   rechnet ihn die Auslegung mit Herkunftsmarke; das Wärmespeicher-Tool liefert Durchlauf- und Zweipunktsimulation sowie
   den Lastgang-Weg der Sperrzeit (F4).
5. **Vorlagen statt Formulare:** sieben Anlagentyp-Vorlagen (Wärmepumpe monovalent, Wärmepumpe bivalent, BHKW, Kessel
   Gas/Öl, Festbrennstoffkessel, Solarthermie, Prozesspuffer) belegen Kriterien, Beispielwerte und Herkunftsmarken; die
   Wärmepumpe kennt Fixed-Speed/leistungsgeregelt und Zweiterzeuger frei/Heizstab gesperrt (F5).
6. **Nutzungsprofil statt Gebäudeart:** Wohnen, Beherbergung, Pflege/Krankenhaus, Büro/Schule, Gewerbe/Prozess werden
   aus Zapfprofil, Konditionierung und Prozesswärme des Projekts abgeleitet und als Herkunft gezeigt (F6).
7. **Hinweise mit Codes statt Sperren:** Plausibilitätsband nach Übergabeart, Abtaureserve bei Trinkwasservorrang,
   Überdimensionierung des Brauchwassers, Extrapolation über 2 000 l, Hygiene nach W 551 und IEA Annex 46, Tank-im-Tank,
   „ohne Puffer nur leistungsgeregelt“ — jede Zeile mit Herkunft (F7).
8. **Schemaschritt 169:** `Tab_PufferAuslegung` (STRICT, je Projektpuffer, NULL = Vorgabe) und die Vorgabetabelle
   `Tab_PufferAuslegungParameter_STAMM` mit Saat im Schritt — eine eigene Tabelle, weil die Tww-Parametertabelle
   paketgebunden ist und neue Schlüssel bestehende Datenbanken nicht über das Nachladen erreichen (F8).
9. **Drei Einstiege, eine Maske:** Knopf „Auslegen…“ im Pufferspeicher-Dialog (Kachel), „Pufferspeicher auslegen…“ in
   ① Simulation Konfiguration als freie Ansicht der `AppWurzel` nach dem Muster der Stromspeicher-Auslegung, und in P2
   „An Speicherauslegung übergeben…“ aus dem Zapfprofil; beide Plattformen über `EPOS.UI` (F9).
10. **Stufen P0–P3 mit 13–18 PT:** P0 Konzept und Mockup (dieses Papier), **P1 Kern und Schema (heute, freigegeben)**,
    P2 Oberfläche mit drei Einstiegen, P3 Bericht, Wiki und Logbuch. Die Referenzbasis R33 bleibt byte-gleich, weil die
    Auslegung nur rechnet und auf Zuruf schreibt; `SimulationPufferspeicher.cs`, `TwwSpeicherauslegung.cs` und das
    Zapfprofil bleiben unverändert (F10).


## 1. Auftrag, Einordnung, Befund heute

**Auftrag** (Anwender, 30.09./01.10.2026): Konzept zur Pufferspeicherauslegung unter Nutzung aller Informationen (auch
`Z:`), Berücksichtigung des Zapfprofils samt Dokumentation, Zugang über Kachel Pufferspeicher und ① Simulation
Konfiguration, umfassende Recherche, übersichtliche geführte Bedienung mit Beispielen und Vorlagen, Mockup zur Abnahme,
Start am Samstag mit allem, P1 bauen, `Quellen/Waermespeicher-Tool/` berücksichtigen; Optimierung nach Erzeugern und
Nutzungen mit Unterscheidung Heizungs-, Kombi-, Brauchwasser- und Prozesswärmepuffer; Kopplung an das Zapfprofil für
nutzungsartenoptimierte Auslegung; internationale Literatur zu Brauchwasserspeichern aufnehmen (01.10.2026).

**Befund heute** (Runde 1, Abschnitt 2): EPOS-Plan hat keine Pufferauslegung. Der Projektpuffer (`Tab_Pufferspeicher`,
28 Spalten) kennt Volumen, Temperaturpaar, Schwellen 10/95 %, Nachrang-Schwelle, Klassen-Set, Schichten, Nutztemperatur
Brauchwasser, Entnahmehöhen, Lade- und Entladeleistung; der Katalog `Tab_Pufferspeicher_STAMM` trägt 13 Zeilen von 101
bis 3 000 l mit Bereitschaftsverlust in kWh je 24 h. Der Kern rechnet den Puffer als Energiescheiben zwischen Rücklauf
und Vorlauf mit der Konstante 1,16 Wh/(l·K), Hysterese über die Schwellen, Verluste anteilig zum Füllstand. Die
Zapfprofil-Speicherauslegung (`TwwSpeicherauslegung`, Lindley über zwei Wochen, 14 Nenninhalte, Zuschläge) rechnet nur den
Trinkwasserspeicher; ihr Knopf „An Speicherauslegung übergeben…“ ist deaktiviert. Startzähler kennt nur der Kessel; die
Wärmepumpe hat ein Sperrfenster ohne Mitternachtsübertrag und keine Mindestlaufzeit. Das Wärmespeicher-Tool
(`Quellen/Waermespeicher-Tool/`, Python) rechnet vier Kriterien (Abtauung, Takt, Sperrzeit, Deckung mit Bisektion) und
die Zweipunktsimulation; seine Tests sind die Vorlage der C#-Fälle.

**Recherche:** Runde 1 [`2026-10-01_Recherche_Pufferspeicherauslegung.md`](Pufferspeicher/2026-10-01_Recherche_Pufferspeicherauslegung.md)
(Kriterientabelle K1–K16, Mockup-Prüfung, V1–V20), Runde 2
[`2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md`](Pufferspeicher/2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md)
(Speicherklassen, Zapfprofil-Kopplung, Nutzungsprofil, V21–V32), Runde 3
[`2026-10-01_Recherche_Normen_Speichermodell.md`](Pufferspeicher/2026-10-01_Recherche_Normen_Speichermodell.md)
(VDI 4645 Entwurf 2026-03, prEN 15316-5:2024, DIN EN 15332, VDI-Whitepaper, V33–V45), Runde 4
[`2026-10-01_Recherche_International_Brauchwasserspeicher.md`](Pufferspeicher/2026-10-01_Recherche_International_Brauchwasserspeicher.md)
(SIA, ASHRAE, Ecosizer, IEA Annex 46, V46–V49). Dieses Konzept wiederholt die Belege nicht; es legt fest.


## 2. Anforderungen und Abgrenzung

### 2.1 Anforderungen

| Nr. | Anforderung | Quelle |
|---|---|---|
| A1 | Auslegung je Speicherklasse Heizung, Brauchwasser, Prozess und Kombi, aus den Bedarfsreihen des Projekts | Auftrag, Runde 2 |
| A2 | Kopplung an das Zapfprofil: Brauchwasserzone aus `TwwSpeicherauslegung` über `ZapfprofilCtrl.Auslegung`, Zapfprofil unverändert | Auftrag, E-P18 |
| A3 | Kriterien nach VDI 4645 (Entwurf 2026-03) als Standard, Tool-Wege als Expertenvariante, alle Zahlen mit Herkunftsmarke | Runde 3, E-P28 |
| A4 | Vorlagen je Anlagentyp mit Beispielwerten; Stufen Schnell/Standard/Experte | Auftrag, Mockup |
| A5 | Drei Einstiege (Kachel, ① Konfiguration, Zapfprofil), beide Plattformen | Anwenderentscheid, E-P13, E-P24 |
| A6 | Übernahme in den Projektpuffer als Ändern oder Neuanlegen über `PufferSpCtrl`; Schwellen und Temperaturpaar unverändert | Runde 2, Tabelle 4.3 |
| A7 | Deterministisch, ohne Simulationslauf, plattformfrei im Kern | CLAUDE.md |
| A8 | Referenzbasis byte-gleich; Einfrierregeln unberührt | Regressionsnetz |
| A9 | Keine Produktdaten, Normen nur zitiert; Herkunft „VDI 4645 E 2026-03“ (Entwurf) ausgewiesen | E-P17 |

### 2.2 Abgrenzung

Nicht Teil dieses Konzepts: Startzähler für Wärmepumpe und BHKW im Kern (V13), Sperrprofil mit mehreren Fenstern im
Kern (V14), Prozesstemperatur und Wiki-Seite Prozesswärme (V29), Zapf-Nutzungsarten Büro/Schule/Gewerbe (V31),
ID-Mapping (V32), umgebungsbezogenes Verlustmodell der Simulation (V35, neue Referenzbasis), Speicher-gegen-Leistung-Kurve
nach dem Ecosizer-Weg (V47, E-P32), Aufheizkriterium nach KP3 (V30). Sie stehen als Folgeaufträge in 8.

### 2.3 Berührte Festlegungen der Schwesterpapiere

Zapfprofilgenerator (Umsetzungskonzept, Stufe Z5, ZU1 „Übergabe an die Speicherauslegung“): erfüllt durch den dritten
Einstieg in P2. Konzept Simulationsablauf: der Puffer rechnet unverändert; die Auslegung schreibt nur Stammdaten des
Projektpuffers. Konzept Konditionierungsprofile: das Nutzungsprofil liest `Nutzung` der Kalendervorlage (WOHNEN, BUERO,
SCHULE, SONSTIGE); das Aufheizkriterium folgt KP3 (V30). Entscheidungsvorlage Modellgrenzen: Zirkulation des
Bestandswegs (`Tab_Einstellungen.Zirkulation_Leistung_kW`, Schritt 166) ist die Quelle für V40, wenn das Zapfprofil
keine Zirkulation trägt.


## 3. Fachliches Modell

### 3.1 Größen

| Größe | Symbol | Quelle im Projekt |
|---|---|---|
| Heizreihe, Brauchwasserreihe, Prozessreihe [kWh/h, 8 760] | Q_H(t), Q_B(t), Q_P(t) | `SimulationLaufCtrl.Vorpruefen` + `Bedarf`, dann `SimulationWaermebedarf.KanaeleDrei()` (Vorbild `StromspeicherAuslegungCtrl.SimulationslaufVorbereiten`), kein Lauf |
| Auslegungsheizlast [kW] | Q̇_Ausl | Maximum der Heizreihe; Eingabe überschreibbar |
| Erzeuger Rang 1 und 2 der Kaskade | — | `Tab_Einstellungen.Tool_1…Tool_4`, `Tab_Energieanlagen.ID_Type` (1 WP, 2 Solar, 10 Kessel, 11 BHKW), Nenn-/Mindestleistung, Kennfeld, `Heizstab`, Brennstoff, `Sperrzeit_von/_bis` |
| Temperaturpaar, Schwellen des Puffers | ϑ_VL, ϑ_RL, s_ein, s_aus | `Tab_Pufferspeicher` (Vorgabe 10 %/95 %) |
| Nutzbarer Anteil | η_s = s_aus − s_ein | aus den Schwellen (E-P9); Vorgabe 0,85 |
| Spezifische Kapazität | c = 1,16 Wh/(l·K) | `ProjektPuffer.WH_JE_LITER_KELVIN` (E-P10) |
| Übergabeart je Gebäude | — | `Tab_Gebaeude.Uebergabe_Art` (RADIATOR, FLAECHE, KONVEKTOR, NULL = ideal → wie FLAECHE mit Hinweis) |
| Heizgrenze | ϑ_HG | `Tab_Einstellungen.Kessel_Heizgrenze`, leer 15 °C (`SimulationSPK.HEIZGRENZE_VORGABE_C`) |
| Anlagenvolumen [l] | V_Hz | Eingabe; Vorgabe aus Übergabeart × Q̇_Ausl (FLAECHE 15 l/kW, RADIATOR 10 l/kW, KONVEKTOR 6 l/kW; Herkunft BaCoGa, Sekundär) |
| Zapfprofil | D_max, Nenninhalt, Ladeleistung, Personen, Zirkulation, Topologie | `ZapfprofilCtrl.Auslegung` → `Speicherauslegungsergebnis` |

### 3.2 Klassenweiche und Zonen

| Klasse | Zonen | Rechenweg | Ergebnis |
|---|---|---|---|
| {H} | Heizzone | K1–K4, D1, D2, K8–K11 (3.3) | V_H |
| {B} | Brauchwasserzone | `ZapfprofilCtrl.Auslegung`: Topologie Speicher → `NenninhaltL`; Frischwasser/Wohnungsstation → V_B = D_max·1000·(1 + z_D)/(c·ΔT_B·η_s) | V_B |
| {H,B} | beide | V_H + V_B; Zonenanteile V_H/(V_H + V_B); `Schichten_Anzahl` ≥ 2 | V_H + V_B |
| {P} | Prozesszone | D1 und D2 auf Q_P(t) mit dem Temperaturpaar des Puffers | V_P |
| {H,P}, {B,P}, {H,B,P} | Summe der Zonen | je Zone ihr Weg | Summe |

ΔT_B = T_Puffer,oben − T_Rücklauf,Frischwasser, Vorgabe 65/25 °C → 40 K (E-P22); z_D = 0,15 Durchmischungszuschlag (V38,
VDI 4645 Anhang I). Zirkulation: trägt das Zapfprofil keine Zirkulation, Zuschlag aus `Zirkulation_Leistung_kW` des
Projekts, sonst 35 % des Tagesbedarfs über 5 K oder 100 W je Wohneinheit (V40, V49, Expertenfeld).

### 3.3 Kriterien der Heizzone

| Nr. | Kriterium | Formel | Vorgaben | Herkunft |
|---|---|---|---|---|
| K1 | Vorprüfung „kein Puffer erforderlich“ | V_Hz ≥ 3 l/kW · Q̇_WP,max und keine Einzelraumregelung → Ergebnis ohne Puffer mit Hinweis; sonst Gutschrift V_Hz in K4 | 3 l/kW | VDI 4645 E 2026-03, 7.8.3 |
| K2 | Faustwert nach Gerätetyp | V = v_spez · Q̇_WP,Ausl; Abtau-Gegenprobe 20 l/kW Heizlast als Reserve (Hinweiscode bei Trinkwasservorrang, V45) | Fixed-Speed 20, leistungsgeregelt 3 l/kW | VDI 4645 7.8.4; Whitepaper RWTH |
| K3 | Mindestlaufzeit | V = Q̇_WP,Nenn · t_min / (c · Δϑ · η_s), Δϑ = ϑ_VL − ϑ_RL | t_min 10 min (Hersteller), Fixed-Speed; bei geregelt mit Mindestleistung | VDI 4645 Gl. 22; Tool C.6.2 |
| K4 | Sperrzeit, Standardweg | V = Q̇_Ausl · (t_sperr − t_aus,max) · 1000 / (c · ((ϑ_VL + dT_SP) − (ϑ_R + dT_Wü,min))) − V_Hz; t_aus,max nach Tabelle 14 (Heizgrenze × Übergabeart), dT_Wü,min nach Tabelle 15, dT_SP ≤ 5 K; entfällt bei `Zweiterzeuger_Frei`, Heizstab mitgesperrt (V44) | Tabellen 14/15 als Vorgabeschlüssel | VDI 4645 Gl. 23, Tab. 14/15 |
| K4e | Sperrzeit, Expertenweg | V = Q̄_sperr · t_sperr · 1000 / (c · Δϑ · η_s), Q̄ rollierendes Mittel der Heizreihe über t_sperr, Sperrprofil je Tag mit Mitternachtsübertrag | Profile keine, 2 × 2 h, 3 × 2 h, eigenes | Tool C.6.3; BWP 2016 |
| D1 | Deckungsgrad | Durchlaufsimulation (Erzeuger deckt Last, Überschuss lädt, Defizit entlädt) mit Bisektion auf das Deckungsziel | Ziel 1,0; Grenze 100 000 l | Tool `simulate`, `find_min_capacity` |
| D2 | Taktziel | Zweipunktsimulation mit den Schwellen des Puffers (Laden bis s_aus, Entladen bis s_ein, Umschalten im Schritt), Modulationsmodus für geregelte Geräte (Mindestleistung), Bisektion auf Starts je Tag; Kennzahlen Starts je Tag und je Heizperiode | Startziel 6/Tag, Warnschwelle 15/Tag, 3 000 je Heizperiode | Tool `simulate_zweipunkt`; Fraunhofer WP-QS; energie-experten (Sekundär) |
| K8 | Gegenprobe | größtes Kriterium gegen Band und Praxisgrenze | — | — |
| K9 | Festbrennstoff | Faustwert nach Brennstoff (Scheitholz 55, Pellets/Hackschnitzel 30, gesetzlich 20 l/kW) und zweiter Weg V = 15 · Q_K · T_B · (1 − 0,3 · Q_H/Q_K,min), T_B 4 h | — | 1. BImSchV § 5, BEG; DIN EN 303-5 (V43) |
| K10 | Solarthermie | V = 50 l/m² Aperturfläche (Röhre 60–70); Nachrang-Schwelle aus dem Projekt | 50 l/m² | Sekundär, DIN EN 12977 |
| K11 | Bereitschaftsverlust | Katalogsatz, sonst Klasse-C-Grenze S = 16,66 + 8,33 · V^0,4 W; Anzeige kWh/d bei 45 K **und** W/K = Q_B · 1000/(24 · 45); Betriebsverlust kWh/a = Q_B · (ϑ_mittel − ϑ_Raum)/45 · 365; über 2 000 l Extrapolation | ϑ_Raum 15 °C | 812/814/2013; EN 15316-5 Formel 3; EN 15332 |

**Plausibilitätsband nach Übergabeart** (V41): FLAECHE 10–20, RADIATOR/KONVEKTOR 25–45 l je kW Heizlast; dazu 12–35 l je
kW Wärmepumpenleistung (DIN EN 15450). Hinweiscodes über und unter dem Band, keine Sperre.

### 3.4 Empfehlung

Je Zone: bemessendes Kriterium = Maximum der aktiven Kriterien (K2, K3, K4 bzw. K4e, D1, D2, K9, K10); V_Zone =
bemessender Wert (Kriterien, die bereits η_s enthalten, nicht erneut teilen); Summe der Zonen; Rundung auf den nächsten
Nenninhalt der Liste `Speicherauslegung.Nenninhalt.*` (Tww-Parameter, Rückfall feste Liste 100 … 10 000 l, Raster
1 000 l); Praxisgrenze 100 000 l mit Warncode; Katalogsatz-Vorschlag = kleinster Katalogpuffer ≥ Summe (Bauform
Pufferspeicher bzw. Kombispeicher bei {H,B}). Vorprüfung K1 positiv → Empfehlung „kein Puffer“, Zonen Brauchwasser und
Prozess rechnen trotzdem.

### 3.5 Nutzungsprofil

Ableitung (Reihenfolge): Zapf-Nutzungsart der Zonen (Hotel → Beherbergung; Senioren/Krankenhaus → Pflege/Krankenhaus;
Wohnen/EFH/Studentenwohnheim → Wohnen) → `Tab_Prozesswaerme` vorhanden → Gewerbe/Prozess → Konditionierungs-`Nutzung`
BUERO/SCHULE → Büro/Schule → sonst Wohnen mit Hinweis „Vorgabe“. Wirkung: Beispielwerte und Hinweistexte, kein
Rechenwert (E-P20). Als Gegenprobe zeigt die Brauchwasserzone den Tagesbedarf je Person gegen 28–50 l bei 60 °C (V46,
IEA Annex 46) und den Hinweiscode Überdimensionierung über 2 × Tagesbedarf.

### 3.6 Warnliste

Codes (Präfix `PA-`): `PA-KEIN-PUFFER` (K1), `PA-BAND-UNTER`, `PA-BAND-UEBER`, `PA-ABTAU-VORRANG` (V45), `PA-STARTS-TAG`,
`PA-STARTS-JAHR`, `PA-PRAXISGRENZE`, `PA-EXTRAPOLATION` (> 2 000 l), `PA-TANK-IM-TANK`, `PA-OHNE-PUFFER-GEREGELT`,
`PA-HYGIENE-W551`, `PA-HYGIENE-TEMPERATUR` (IEA: > 52 °C Austritt, ≥ 55 °C Vorlauf, periodisches Aufheizen kein
Ersatz), `PA-BW-UEBERDIMENSIONIERT`, `PA-UEBERGABE-UNBEKANNT`, `PA-ZWEITERZEUGER-FREI`, `PA-HEIZSTAB-GESPERRT`,
`PA-KEINE-REIHE` (Kanal leer). Jede Zeile trägt Stufe (Hinweis, Warnung), Text (Ressourcenschlüssel de/en) und
Herkunft.


## 4. Datenmodell und Schema

### 4.1 Schemaschritt 169 (`PufferAuslegungSchema`, `SCHRITT = BedarfNetzKalenderSchema.SCHRITT + 1`)

**`Tab_PufferAuslegung`** (STRICT): `ID` INTEGER PRIMARY KEY AUTOINCREMENT, `ID_Projekt` INTEGER NOT NULL REFERENCES
`Tab_Projekt`(ID) ON DELETE CASCADE, `ID_Pufferspeicher` INTEGER REFERENCES `Tab_Pufferspeicher`(ID) ON DELETE SET
NULL, `Klasse_Heizung`/`Klasse_Brauchwasser`/`Klasse_Prozess` INTEGER NOT NULL DEFAULT 0 CHECK IN (0,1),
`Nutzungsprofil` TEXT CHECK IN ('WOHNEN','BEHERBERGUNG','PFLEGE','BUERO_SCHULE','GEWERBE'), `Vorlage` TEXT CHECK IN
('WP_MONO','WP_BIVALENT','BHKW','KESSEL','FESTBRENNSTOFF','SOLAR','PROZESS'), `WP_Geregelt` INTEGER CHECK IN (0,1),
`Zweiterzeuger_Frei` INTEGER CHECK IN (0,1), `Mindestlaufzeit_min` REAL CHECK (> 0), `Mindestleistung_kW` REAL CHECK (≥ 0),
`Anlagenvolumen_l` REAL CHECK (≥ 0), `Sperrprofil` TEXT CHECK IN ('KEINE','ZWEI_MAL_ZWEI','DREI_MAL_ZWEI','EIGEN'),
`Sperrdauer_h` REAL CHECK (0 … 24), `Sperrbeginn_h` REAL CHECK (0 … 24), `Startziel_je_Tag` REAL CHECK (> 0),
`Deckungsziel` REAL CHECK (0 … 1), `DeltaT_B_K` REAL CHECK (> 0), `T_Puffer_Oben_C` REAL, `Zirkulation_Weg` TEXT CHECK
IN ('ZAPFPROFIL','PROJEKT','ANTEIL','JE_WE'), `BHKW_Verschiebedauer_h` REAL CHECK (≥ 0), `Volumen_H_l`, `Volumen_B_l`,
`Volumen_P_l`, `Volumen_Empfehlung_l` REAL, `Bemessend` TEXT, `Berechnet_am` TEXT. NULL = Vorgabe. Eine Zeile je
Projektpuffer, zusätzlich Zeilen mit `ID_Pufferspeicher` NULL für „neu anlegen“.

**`Tab_PufferAuslegungParameter_STAMM`** (STRICT): `ID`, `Schluessel` TEXT NOT NULL UNIQUE, `Wert` REAL NOT NULL,
`Einheit` TEXT, `Quelle` TEXT NOT NULL, `Herkunftsart` TEXT CHECK IN ('RICHTLINIE','STUDIE','SETZUNG','SEKUNDAER'),
`ReadOnly` INTEGER NOT NULL DEFAULT 1 CHECK IN (0,1). Saat im Schritt mit `INSERT OR IGNORE` (ergebnisneutral,
wiederholbar). Schlüssel (`Pufferauslegung.*`):

| Schlüssel | Wert | Einheit | Quelle |
|---|---|---|---|
| `Konstante.Wh_je_l_K` | 1,16 | Wh/(l·K) | Kern |
| `Vorpruefung.Anlagenvolumen_l_kW` | 3 | l/kW | VDI 4645 E 2026-03, 7.8.3 |
| `Anlagenvolumen.FLAECHE_l_kW` / `RADIATOR_l_kW` / `KONVEKTOR_l_kW` | 15 / 10 / 6 | l/kW | BaCoGa (Sekundär) |
| `Faustwert.FixedSpeed_l_kW` / `Geregelt_l_kW` | 20 / 3 | l/kW | VDI 4645 7.8.4 |
| `Abtau.Reserve_l_kW` | 20 | l/kW Heizlast | Whitepaper RWTH |
| `Mindestlaufzeit_min` | 10 | min | Hersteller (VdZ 2025) |
| `Sperrzeit.Stillstand.15.RADIATOR_h` … `10.FLAECHE_h` (sechs Werte 1 / 1,5 / 2 / 2,5 / 3 / 3,5) | | h | VDI 4645 Tab. 14 |
| `Sperrzeit.Uebertemperatur.FLAECHE_K` / `RADIATOR_K` / `KONVEKTOR_K` / `LUEFTER_K` | 5 / 15 / 20 / 15 | K | VDI 4645 Tab. 15 |
| `Sperrzeit.Ueberladung_K` | 5 | K | VDI 4645 8.8 |
| `Takt.Startziel_je_Tag` / `Warnschwelle_je_Tag` / `Heizperiode_Max` | 6 / 15 / 3 000 | 1/d, 1/d, 1/a | Fraunhofer WP-QS; Sekundär |
| `Deckung.Ziel` / `Praxisgrenze_l` | 1,0 / 100 000 | – / l | Tool |
| `Band.FLAECHE.Min_l_kW` / `Max_l_kW`, `Band.RADIATOR.Min_l_kW` / `Max_l_kW`, `Band.WP.Min_l_kW` / `Max_l_kW` | 10 / 20, 25 / 45, 12 / 35 | l/kW | Whitepaper RWTH; DIN EN 15450 |
| `Festbrennstoff.Scheitholz_l_kW` / `Pellets_l_kW` / `Gesetz_l_kW` / `Abbrandperiode_h` | 55 / 30 / 20 / 4 | l/kW, h | 1. BImSchV, BEG, DIN EN 303-5 |
| `Solar.Flach_l_m2` / `Roehre_l_m2` | 50 / 65 | l/m² | Sekundär |
| `Frischwasser.T_Oben_C` / `T_Ruecklauf_C` / `Zuschlag` | 65 / 25 / 0,15 | °C, – | Runde 2; VDI 4645 Anhang I |
| `Zirkulation.Anteil` / `W_je_WE` | 0,35 / 100 | –, W | Sekundär; Ecosizer |
| `BHKW.Verschiebedauer_h` | 2 | h | kwk-flexperten (Sekundär) |
| `Bereitschaft.Raumtemperatur_C` / `Pruef_DeltaT_K` / `Extrapolation_ab_l` | 15 / 45 / 2 000 | °C, K, l | EN 15332; 812/2013 |
| `Brauchwasser.Bedarf_Min_l_P` / `Bedarf_Max_l_P` / `Ueberdimensionierung_Faktor` | 28 / 50 / 2,0 | l, – | IEA Annex 46 |
| `Zonen.Vorgabe_Oben` … (0,10 / 0,16 / 0,37 / 0,37) | | – | EN 15316-5 Tab. B.4 |
| `Vorlage.<Typ>.*` | je Vorlage: Kriterienschalter 0/1 und Beispielwerte | | Runde 1, Abschnitt 6 |

### 4.2 Einhängen

`SchemaStand.Zielversion = PufferAuslegungSchema.SCHRITT`; `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`
(Konstante, Eintrag, `Schritt_PufferAuslegung`); `Paketanhebung.STUFEN` (`Art.Ddl`, die Saat läuft im DDL-Schritt);
`EPOS.Kern.Tests/TestDatenbank.SchemaNachziehen`; `Werkzeuge/Testdatenbankschema/Program.cs`; `ProjektDuplizierenCtrl.KINDER`
braucht keinen Eintrag (eigene `ID_Projekt`), `ID_Pufferspeicher` wird beim Duplizieren über `FK_MAP` umgesetzt;
Testdatenbank mit `Werkzeuge/Testdatenbankschema` auf 169, Nachtrag in `Referenzlaeufe/LIESMICH.md`. Export/Import
des Projekts (XML) nimmt die Zeile mit, wenn der Projektexport Tabellen generisch führt; sonst Folgeauftrag P2.


## 5. Rechenkern und Einbau

Namensraum `EPOS.Kern/Allgemein/Pufferauslegung/`:

- `PufferAuslegungEingang` (record): Klassen-Set, Vorlage, Erzeugerdaten (Typ, Nennleistung, Mindestleistung,
  geregelt, Heizstab, Zweiterzeuger frei, Brennstoff, Kollektorfläche), Puffer (ϑ_VL, ϑ_RL, s_ein, s_aus),
  Reihen (Q_H, Q_B, Q_P), Übergabeart, Heizgrenze, Anlagenvolumen, Sperrprofil, Startziel, Deckungsziel, Zapfprofil-
  Ergebnis (D_max, Nenninhalt, Ladeleistung, Personen, Zirkulation, Topologie), Parametersatz.
- `PufferAuslegungParameter`: liest `Tab_PufferAuslegungParameter_STAMM` in ein `IReadOnlyDictionary<string,double>`
  mit Rückfall auf die eingebauten Werte (gleiche Tabelle wie 4.1).
- `HeizzoneRechner`: K1–K4, K4e, K8–K11, Band; `Betriebssimulation`: Durchlauf, Zweipunkt (Hysterese über Schwellen,
  Modulationsmodus), Bisektion; `BrauchwasserzoneRechner`: Topologie-Weiche, Zuschläge, Zirkulation, Kennzahl je Person;
  `ProzesszoneRechner`: D1/D2 auf Q_P; `Nutzungsprofil`: Ableitung; `PufferAuslegungErgebnis`: je Zone bemessendes
  Kriterium, alle Kriterienwerte mit Herkunft, Empfehlung, Katalogvorschlag, Kennzahlen (Starts, Verluste kWh/d, W/K,
  kWh/a), Warnliste; `PufferAuslegung.Rechnen(eingang)` als Fassade, rein funktional, deterministisch.
- `EPOS.Kern/Controller/PufferAuslegungCtrl`: `Vorbelegen(idProjekt, idPuffer?)` (liest Projekt, Puffer, Kaskade,
  Erzeuger, Gebäude, Einstellungen, Zapfprofil über `ZapfprofilCtrl.Auslegung`, Prozessdaten, Konditionierung, baut den
  Eingang, leitet das Nutzungsprofil ab), `Reihen(idProjekt, idKlimaregion)` (Vorpruefen + Bedarf + `KanaeleDrei()`),
  `Rechnen(eingang)`, `Speichern(zeile)` (Tab_PufferAuslegung), `Uebernehmen(ergebnis, idPuffer?)` (über
  `PufferSpCtrl.ProjektPufferAnlegen/Aendern`: Volumen, Katalogsatz, Bereitschaftsverlust, `Nutzung_*`, `T_Nutz_BW`,
  `Schichtdaten` mit `Schichten_Anzahl` ≥ 2 und Entnahmeanteilen, Schwellen und Temperaturpaar unverändert; nie auf
  Referenzprojekte in Tests), `Vorlagen()`.
- Alle Datenbankzugriffe über `DataRepository` mit `?`-Parametern; `SqlDialektPruefer` 0 Fundstellen.


## 6. Benutzerführung (P2)

Eine Razor-Komponente `PufferAuslegungSeite` in `EPOS.UI`, Hülle `PufferAuslegungHuelle` in `EPOS.UI.Daten`, freie Ansicht
der `AppWurzel` mit „← zurück“ (Muster Stromspeicher-Auslegung). Vier Schritte: 1 Anlage, Speicherklasse, Vorlage,
Nutzungsprofil (Herkunft) → 2 Lastgang und Randbedingungen (Reihen aus dem Projekt, Übergabeart, Heizgrenze,
Anlagenvolumen, Sperrprofil, Zapfprofil-Zonen) → 3 Kriterien (Karten mit Schalter, Wert, Herkunft; Stufen
Schnell/Standard/Experte blenden Karten) → 4 Ergebnis und Übernahme (Zonen, Empfehlung, Betriebsbild, Kennzahlen,
Nutzen-Aufwand-Zeile mit JAZ-Hinweis, Warnliste, Übernahme als Ändern/Neuanlegen). Einstiege: Knopf „Auslegen…“ im
`PufferspeicherDialog`; „Pufferspeicher auslegen…“ in `SimulationKonfigSeite` unter „Pufferspeicher anlegen / verwalten…“;
„An Speicherauslegung übergeben…“ im Zapfprofil-Auslegungsdialog (aktiviert, Klasse {B} bzw. {H,B} vorgewählt). Texte in
`MyResource.Resource.*` (de/en), `ResourceDesigner` ziehen; KI-Maske im Katalog; Rasterprobe nicht betroffen.


## 7. Stufen und Aufwand

| Stufe | Inhalt | Abnahme | PT |
|---|---|---|---|
| **P0** | Recherche (vier Runden), Mockup, dieses Konzept — abgeschlossen 03.10.2026 | Wachen grün | 3 |
| **P1** | Schemaschritt 169 mit Vorgabetabelle und Saat, Rechenkern, Controller, Tests, Testdatenbank 169 — **heute** | Kern-Filter 0 Fehler, Tests grün, Referenzlauf 16/16 byte-gleich gegen R33, Windows-Schale 0 Fehler | 5–7 |
| **P2** | Oberfläche, drei Einstiege, Hülle, Texte, KI-Maske, bunit | UI-Tests, Sichtabnahme Windows, iOS nach Rückfrage | 4–6 |
| **P3** | Bericht (Abschnitt Pufferauslegung), Wiki-Seite, Logbuch-Satz, Export/Import der Zeile | Wiki-Suchmuster, Berichtstests | 1–2 |
| **Summe** | | | **13–18** |


## 8. Folgeaufträge

V13 Startzähler WP/BHKW, V14 Sperrprofil im Kern (mehrere Fenster, Mitternachtsübertrag, ohne Dimmung), V29
Prozesstemperatur und Wiki-Seite Prozesswärme, V30 Aufheizkriterium nach KP3, V31 Zapf-Nutzungsarten Büro/Schule/Gewerbe,
V32 ID-Mapping, V35 umgebungsbezogenes Verlustmodell (neue Referenzbasis), V47 Speicher-gegen-Leistung-Kurve (Ecosizer),
E-P17 Entfernen der Normdateien nach der Umsetzung, Weißdruck VDI 4645 gegen den Entwurf prüfen.


## 9. Festlegungen und Entscheide

### 9.1 Festlegungen nach Empfehlung (F1–F10)

F1 Zonenmodell je Klasse; F2 Brauchwasserzone unverändert aus dem Zapfprofil; F3 Bemessung als Maximum ÷ nutzbarer
Anteil, Konstante 1,16; F4 VDI 4645 als Standard, Tool als Experte; F5 sieben Vorlagen mit Gerätetyp- und
Bivalenz-Schalter; F6 Nutzungsprofil als Herkunft; F7 Hinweiscodes statt Sperren; F8 eigene Vorgabetabelle mit Saat im
Schemaschritt (statt Tww-Parametertabelle, Entscheid V18); F9 drei Einstiege über `EPOS.UI`; F10 keine Änderung an
Simulation, Zapfprofil und Referenzbasis. Dazu gelten E-P8–E-P11, E-P13–E-P15, E-P18–E-P28 und E-P30 der Recherche nach
Empfehlung.

### 9.2 Entscheide des Anwenders (E60, 01.10.2026)

| Nr. | Entscheid |
|---|---|
| E-P12 | § 14a EnWG ist für den Wärmepuffer nicht relevant: kein Kriterium, keine Dimmung, kein Enddatum; Sperrzeit nur als neutrale Eingabe |
| E-P17 | Norm-PDFs und Herstellerunterlage unter `Quellen/Waermespeicher-Tool/` bleiben für Konzeption und Umsetzung; nur zitiert |
| E-P31 | Internationale Werte (Runde 4) sind Gegenprobe und Beispielhilfe, Vorgaben bleiben EN 12831-3, VDI, W 551 |
| E-P32 | Speicher-gegen-Leistung-Kurve (V47) als Folgeauftrag nach P2 |
| — | Vorschläge V46–V49 angenommen; Einstiege Kachel (Knopf im Dialog) und ① Konfiguration; Bau P1 freigegeben |


## 10. Nachweise, Abnahme, Einfrierregel

- **Tests P1:** Tool-Fälle aus `test_sizing.py` und `test_zweipunkt.py` als C#-Fälle (Konstante 1,16, Toleranz 0,5 %);
  Handrechnungen: K2 60 kW Fixed-Speed 1 200 l, geregelt 180 l; K3 18 kW · 10 min · 10 K · 0,85 → 304 l; Gleichung 22
  13,8 kW · 5 min · 30 K → 33,0 l; K4 82 kW, 2 h, RADIATOR, Heizgrenze 15 °C → (82 · 1 · 1000)/(1,16 · 25) − 400 =
  2 428 l, Expertenweg 16 630 l; K9 30 kW Scheitholz 1 650 l und DIN EN 303-5 1 080 l; Band 60 kW RADIATOR 1 500–2 700 l,
  FLAECHE 600–1 200 l; Kombi D_max 24 kWh, 40 K, 0,85, Zuschlag 15 % → 700 l + 2 000 l → 3 000 l; Brauchwasser 40
  Personen 1 600 l/d → 40 l je Person, Zone 3 500 l → Überdimensionierung; Zirkulation 20 WE × 100 W → 48 kWh/d; K11
  2,5 kWh/d → 2,315 W/K, Klasse C 1 000 l → 3,569 kWh/d → 3,304 W/K, Betriebsfaktor 0,667 → 608 kWh/a, 2 500 l →
  Extrapolation; Prozesszone am Testprojekt 1041; Nutzungsprofil je Testprojekt; Schemaschritt zweimal;
  `ProjektplanKinderWacheTests`; Paketanhebung.
- **Nachtrag P1 (Rev. 2):** Die Vorgabetabelle trägt über 4.1 hinaus `Sperrzeit.Raumtemperatur_C` 20, `WP.Mindestleistung_Anteil` 0,3 und `Puffer.Schwelle_Ein`/`_Aus` 0,10/0,95; die Zonenschlüssel heißen `Zonen.Vorgabe_Oben/_MitteOben/_MitteUnten/_Unten`; Saat 148 Zeilen. K4 nach Gleichung 23 rechnet ohne η_s, K3, K4e, D1 und die Volumenkriterien mit η_s, D2 über die Schwellen. K11 Klasse C bei 1 000 l ergibt 3,568 kWh/d. Der Controller heißt `PufferAuslegungCtrl` mit `Vorbelegen`, `Reihen`, `Rechnen`, `Durchrechnen`, `Speichern`, `Uebernehmen`, `Vorlagen`, `Katalog`; Export/Import und Duplizieren tragen `Tab_PufferAuslegung` über den generischen Plan mit. Welle M4 (Schritt 167) liefert `Mindestleistung_kW` und Taktverlust der Wärmepumpe als künftige Vorbelegung (P2).
- **Gate:** `WP-Plan.Kern.slnf` Release 0 Fehler, alle Testprojekte mit den xUnit-Schaltern, Windows-Schale auf Linux
  0 Fehler, Referenzlauf 16/16 byte-gleich gegen `2026-10-02_R33_Viertelstunden`, Wachen Dokumentation/Repository/Wiki.
- **Einfrierregel:** unberührt — die Auslegung schreibt nur auf Zuruf in `Tab_Pufferspeicher`; Tests übernehmen nie auf
  Referenzprojekte, sondern auf Kopien.
- **Wiki (P3, gebündelt):** neue Seite „Pufferspeicher auslegen“ (Bedienung), Grundlagenseite Pufferspeicher ergänzt um
  die Kriterien; Logbuch-Entwurf: „Die Pufferspeicher-Auslegung bemisst Heizungs-, Kombi-, Brauchwasser- und
  Prozesswärmepuffer nach VDI 4645 und Betriebssimulation; sie ist über die Kachel Pufferspeicher und die
  Simulationskonfiguration erreichbar.“ Version beim Anwender erfragen.


## 11. Verweise

Recherche Runde 1–4 (1), Mockup `Dokumentation/aktuell/Mockups/Pufferspeicher_Auslegung_Mockup.html`,
[Konzept Simulationsablauf](Konzept_Simulationsablauf_EPOS-Plan.md), [Konzept Konditionierungsprofile](Konzept_Konditionierungsprofile_EPOS-Plan.md),
[Referenzläufe](../../Referenzlaeufe/LIESMICH.md), Wärmespeicher-Tool `Quellen/Waermespeicher-Tool/`, Normdateien ebenda
(zitiert nach E-P17).
