# Recherche Pufferoptimierung nach Erzeugern und Nutzungen — Heizungs-, Kombi-, Brauchwasser- und Prozesswärmepuffer (01.10.2026)

**Zweck.** Zweite Runde der Recherche zur Pufferspeicher-Auslegung, Fortsetzung von
[`2026-10-01_Recherche_Pufferspeicherauslegung.md`](2026-10-01_Recherche_Pufferspeicherauslegung.md)
(dort: Heizungspuffer nach Erzeugern, Kriterien K1–K16, Prüfung des Mockups, Vorschläge V1–V20).
Diese Runde unterscheidet die **Speicherklassen** Heizungspuffer, Kombipuffer, Brauchwasserspeicher
und Prozesswärmepuffer, ordnet die Erzeuger (Wärmepumpe, BHKW, Kessel, Festbrennstoff, Solarthermie)
den Klassen zu und legt fest, wie die Auslegung an das **Zapfprofil** und die **Nutzungsarten**
gekoppelt wird — Auftrag des Anwenders vom 01.10.2026: „Recherchiere auch zur
Pufferspeicheroptimierung auch bezüglich der unterschiedlichen Erzeuger und Nutzungen. Unterscheide
Heizungspuffer, Kombipuffer und Brauchwasser sowie Prozesswärmepuffer. … Kopple die Auslegung auch
an das Zapfprofil – also für Nutzungsarten optimierte Auslegung.“

**Anwenderentscheid E-P17 (01.10.2026):** Die Norm-PDFs und die Herstellerunterlage unter
`Quellen/Waermespeicher-Tool/` bleiben für den Zeitraum von Konzeption und Umsetzung im
Repositorium (Repositorium wird nur vom Anwender genutzt); danach entfernen. Sie werden zitiert,
nicht abgeschrieben.

**Fortsetzung.** Die dritte Runde wertet die am 01.10.2026 abgelegten Normdateien aus (VDI 4645
Hauptblatt, Entwurf 2026-03 — Gegenlesen von K1–K4, V37–V39; VDI-MT 4645 Blatt 1 — Qualifikationsblatt
ohne Auslegungsregeln; prEN 15316-5:2024 — Schichtenmodell und
W/K-Umrechnung; DIN EN 15332 mit A1 — Prüfbedingung 45 K hinter dem Katalogwert):
[`2026-10-01_Recherche_Normen_Speichermodell.md`](2026-10-01_Recherche_Normen_Speichermodell.md) mit V33–V36 und E-P26–E-P27.

Grenzen wie in Runde 1: Normen zitiert, keine Produktdaten, Sekundärquellen gekennzeichnet, alle
Rechenbeispiele fiktiv.


## 1 Quellenlage der zweiten Runde

| Quelle | Art | Was sie beiträgt |
|---|---|---|
| BWP, *Leitfaden Trinkwassererwärmung*, Stand Februar 2023 ([PDF](https://www.waermepumpe.de/fileadmin/user_upload/waermepumpe/08_Sonstige/Filedump/BWP_LF_TWW_2023.pdf)) | Verbandsleitfaden | Systeme (Speicher, Durchfluss/Frischwasserstation, Wohnungsstation, Tank-in-Tank, Puffer mit Durchflusswärmeübertrager) mit Vor- und Nachteilen für Wärmepumpen; Auslegungsgang in sieben Schritten; 15–20 % Durchmischungszuschlag; vereinfachtes Verfahren 25 l/(P·d) bei 60 °C, verdoppelt bis ~10 Personen; Umrechnung auf die Bevorratungstemperatur |
| GdW/dena, *Praxisleitfaden für Wärmepumpen in Mehrfamilienhäusern*, 03/2024 ([PDF](https://www.gebaeudeforum.de/fileadmin/gebaeudeforum/Downloads/Leitfaden-Handbuch/Leitfaden_Waermepumpen-in-Mehrfamilienhaeusern.pdf)) | Leitfaden | Großanlage nach W 551 (≥ 60 °C Austritt, Zirkulation ≥ 55 °C, Vorwärmstufen einmal täglich 60 °C), Kombispeicher als Heizungspuffer mit integriertem Trinkwasserbereiter, Frischwasserstation (Puffertemperatur „um einige Grad höher“), Speicherladesysteme mit Ladelanze, Ultrafiltration (Absenkung auf 45–47 °C nach Erprobung); Praxisbeispiele mit Heizungspuffern 560–1 000 l |
| Umweltbundesamt, *Factsheet Trinkwarmwasserkonzepte*, 01.11.2023 ([PDF](https://www.umweltbundesamt.de/system/files/medien/11850/publikationen/factsheet_trinkwarmwasserkonzepte.pdf)) | Behördenpapier | TrinkwV 400-l-/3-l-Regel, W 551 Mindesttemperaturen, Empfehlung „gespeicherte Warmwassermenge möglichst gering“; Beispiel: JAZ der Trinkwassererwärmung sinkt von 3,22 auf 2,19 bei 60-°C-Niveau im Typ-EFH |
| DVGW, *Energieeinsparungen im Warmwasserbereich*, Langfassung, März 2023 ([PDF](https://www.dvgw.de/medien/dvgw/leistungen/publikationen/energiesparen-warmwasser-dvgw-langfassung.pdf)) | Regelsetzer | Großanlage > 400 l und/oder > 3 l; 60 °C am Austritt, 55 °C in der gesamten Zirkulation; „eine höhere Temperatur ist aus Sicht der technischen Regeln nicht erforderlich“; Absenkung unter das Schutzniveau abgelehnt |
| IKZ, *DIN EN 12831-3 als Ersatz für DIN 4708* ([PDF](https://www.ikz.de/fileadmin/user_upload/008_011.pdf)) | Fachartikel | Summenlinienverfahren für Speichervolumen und Nachheizleistung „für alle Systeme, Gebäude- und Nutzungsarten“, charakteristische Zapfprofile je Nutzungsart (Hotel, Krankenhaus, Schule, Verwaltung, Wohnen, Senioren-/Pflegeheim) |
| kwk-flexperten.net, *Wärmenutzung und Wärmespeicher* ([Seite](https://www.kwk-flexperten.net/w%C3%A4rmenutzung-und-w%C3%A4rmespeicher)) | Fachportal (KWK) | Faustregel 30 m³ je MWh bei 30 K Spreizung (90/60 °C); Beispiel 500 kW_th, 14 h Betriebspause → 200 m³; Rücklauf möglichst unter 60 °C |
| BMWi, SINTEG *Blaupause 11: Flexibilisierung von KWK-Anlagen* ([PDF](https://www.bundeswirtschaftsministerium.de/Redaktion/DE/Dossier/Sinteg/4-3-blaupause.pdf?__blob=publicationFile&v=1)) | Projektbericht | Wärmespeicher als Flexibilitätsquelle; Verschiebedauer 3 h mit 95 % Speicherwirkungsgrad (Netzbeispiel); Speicher nah an der KWK-Anlage |
| dena, *Gutachten zur Wärmespeicherstrategie*, August 2024 ([PDF](https://www.dena.de/fileadmin/dena/Dokumente/Projektportrait/Energiepolitische_Beratung_des_Bundesministeriums_fuer_Wirtschaft_und_Klimaschutz/Gutachten_zur_Waermespeicherstrategie.pdf)) | Gutachten | Gebäudespeicher verschieben Wärme „über mehrere Stunden“, Pufferspeicher zur kurzzeitigen Entkopplung; Prozesswärme 80 bis > 1 500 °C — Wasserspeicher nur für den unteren Bereich |
| Lechner u. a. (OTH), *Optimierte Monitoring-, Betriebs- und Regelstrategien für BHKW*, 2017 (Runde 1) | Forschungsbericht | spezifische Puffervolumina 24–99 l/kW_th realer Anlagen, 9–31 Betriebsstunden je Start, Modulation 35–50 % |
| EPOS-Plan-Bestand (Sonnet-Befund, 01.10.2026) | Repositorium | Speicherklassen als Set {H,B,P}, Zapfprofil-Speicherauslegung ohne Übernahme, Prozesswärme ohne Temperatur, Konditionierungsprofile, Lastgang-Quellen (Abschnitt 2) |
| energie-experten.org, baunetzwissen.de (Solar), reduco/xpora/mepbau (Puffer) | Sekundärquellen | 50 l/m² Flach-, 60–70 l/m² Röhrenkollektor; 60 l/m² mit mindestens 750 l; 80 l/Person Vorwärmspeicher; Frischwasserstation: Puffer ≥ 10 K über der Zapftemperatur, 400–800 l im MFH — ohne Vorgabewirkung |

**Dünn belegt:** Prozesswärmepuffer. Die Fachliteratur behandelt Industriespeicher strategisch
(dena) oder als Spitzenkappung aus dem Lastgang (Peak Shaving); belastbare Richtwerte in l/kW gibt
es nicht. Für EPOS-Plan trägt deshalb das Lastgang-Kriterium (K6/K7 aus Runde 1) mit der
Prozessreihe, nicht ein Faustwert.


## 2 Bestand in EPOS-Plan (Befund des Sonnet-Agenten, Schemastand 158)

### 2.1 Speicherklassen

- **Eine Tabelle, ein Set:** `Tab_Pufferspeicher.Nutzung_Heizung/_Brauchwasser/_Prozess` (0/1) bildet
  die Klasse als Menge; „Kombi“ ist der Anzeigename von {H,B}
  (Konzept Brauchwasser/Heizung/Puffer § 6.1). `Speichertyp` ist nur Bauform
  (Pufferspeicher | Solarspeicher | Kombispeicher); `Z_AnlageSenke.Ziel`
  (PufferHeizung | PufferBrauchwasser | PufferKombi | PufferProzess) ist der **Ladezweck**, entladen
  wird allein nach dem Set (`Warnkriterien.ZielKanaele`, W1 bei Abweichung). Es gibt **keinen
  eigenen Brauchwasserspeicher-Typ** — ein Brauchwasserspeicher ist ein Puffer mit Set {B}.
- **Klassenregeln im Rechenweg:** Entnahmehöhe für H und P = 0,5, wenn das Set B enthält, sonst 1,0;
  B stets 1,0 (Bereitschaftszone oben); Nutztemperatur nur für B (`T_Nutz_BW`, geklemmt auf den
  wirksamen Vorlauf), H und P am Rücklauf. Beides wirkt **nur bei `Schichten_Anzahl` > 1**
  (`SimulationControl.cs:3357–3363`, `SimulationPufferspeicher.EntladefaehigkeitKanal`). Entnahme
  aus einem kWh-Vorrat je Kanal nach Knappheitsfolge B → P → H → K; kein Wärmeübertrager-, kein
  Zirkulationsmodell; Spitzen nur über `Entladeleistung_Max` je Stunde.
- **Testdatenbank:** 60 Puffer — {H} 40, {H,B} 10, {B} 10, mit P 0; `Entnahme_*` nie gepflegt;
  Schichtung N > 1 nur an zwei Brauchwasserpuffern (N = 10, `T_Nutz_BW` 55).
- **Warnkriterien:** W1 Ziel nicht im Set, W2 Kombi-/Solarbauform ohne B, W3 Erzeuger-Vorlauf unter
  dem wirksamen Vorlauf bzw. unter `T_Nutz_BW`, W4 `T_Nutz` über dem Vorlauf, W6 Schichtung am
  Verbund. **Nichts zu Volumen, Hygiene oder Mindestgröße** — die W-551-Mindesttemperatur steht nur
  in der Zapfprofil-Auslegung (`Auslegungsparameter.cs`, Katalogschlüssel `W551.Mindesttemperatur`).

### 2.2 Zapfprofil und Nutzungsarten

- `Tab_TwwNutzungsart_STAMM` (51 Spalten): `Bezugsart` 1–8 (Personen, Wohneinheiten, Betten,
  Duschplätze, Sitzplätze, Beschäftigte, Fläche, Zimmer), drei Bedarfsniveaus, Zapf- und
  Kaltwasserbezug, Bilanzgrenze, Kalenderart, Monats- und Wochenfaktoren, Tagesgangsatz.
  Auslieferung (freier Paketteil): **sechs Nutzungsarten** — Wohnen groß, Ein- und
  Zweifamilienhaus, Studentenwohnheim, Seniorenheim, Krankenhaus, Hotel (je Zimmer, aus Messung);
  **kein Büro, keine Schule, kein Gewerbe** (die kennt nur der Altkatalog `Tab_Brauchwasser_STAMM`).
- `Tab_TwwZone`: `ID_Nutzungsart` (Pflicht), `ID_Gebaeude`, Bezugsmenge, Niveau, **`Topologie`**
  1 Speicher | 2 Frischwasserstation | 3 Durchfluss | 4 Wohnungsstation, Zirkulation, Ferien.
  `Tab_TwwProjekt`: eine Zeile je Projekt mit `Speicher_C`, `Nutzanteil`, `Zuschlag`,
  `Ladefenster`, `Lade_*`, `Fuellstand_Bezug`, `Auslegung_Volumen_l/_Leistung_Kw`.
- **TWW-Speicherauslegung** (`TwwSpeicherauslegung.Rechnen`, `internal`): Eingang ist die
  **168-h-Wochenreihe** (keine 8 760-Reihe), Speicher- und Kaltwassertemperatur, Nutzanteil 0,8,
  Zuschlag 0,15, Ladefenster (22 h + 8 h), Ladeleistung, Zirkulation, Topologie, Nenninhalte.
  Ergebnis: Lindley-Defizit D_max (kWh, über zwei Wochen), `VolumenProfilL` =
  D_max·1000/(1,163·Δθ)/f_nutz·(1+z_S), `VolumenDinL`, `VolumenGlfL`, `VolumenKlassischL`
  (nachrichtlich), Band, `NenninhaltL`, Füllstand (`KapazitaetKwh`, `MinFuellstandKwh`,
  `ReserveAnteil`), Hinweiscodes. **Volumen nur bei Topologie Speicher; Frischwasser-, Durchfluss-
  und Wohnungsstation liefern eine Leistung (Minutenspitze).**
- **Übernahme heute: keine.** Kein Zapfprofil-Code referenziert `Tab_Pufferspeicher`; „OK“ legt
  nur einen Punkt in `Tab_TwwProjekt.Auslegung_Volumen_l/_Leistung_Kw` (erste Speichergruppe); der
  Knopf „An Speicherauslegung übergeben…“ ist `aria-disabled` („kommt mit einer späteren Fassung“,
  `ZapfprofilAuslegungTexte.cs:413`). ZU1/ZU2 im Umsetzungskonzept Zapfprofilgenerator: die
  Kernklassen sind so geschnitten, dass ein späterer Dialog „etwa aus dem Pufferspeicher“ sie ohne
  Änderung ruft.
- **Nutzungsart-Kennung:** nur je Zone (`Tab_TwwZone.ID_Nutzungsart`); `Tab_Projekt` hat keine;
  `Tab_Gebaeude.Gebaeudeart` ist Freitext (Einfamilienhaus, Hotel, Schule, Altenheim,
  Krankenhaus, Verwaltungsgebäude …); Konditionierungsvorlagen tragen `Nutzung`
  WOHNEN | BUERO | SCHULE | SONSTIGE. **Kein ID-Mapping zwischen den dreien.**

### 2.3 Prozesswärme

`Tab_Prozesswaerme` (Monatssummen) × `Tab_Prozesstyp(_STAMM)` (168-h-Wochenprofil) →
`prozesswerte[8760]` kWh/h, Kanal PROZESS; Deckung direkt (Senke „Prozesswaerme“) oder über einen
Puffer mit `Nutzung_Prozess`. **Keine Temperatur, keine Betriebszeiten außer dem Wochenprofil, kein
`T_Nutz_Prozess`, kein Testdatenbank-Fall mit Prozesspuffer, keine Wiki-Seite** (Verweise in
`Programm Dokumentation.wiki:133–137` laufen ins Leere).

### 2.4 Konditionierungsprofile und Aufheizen

Kalender je Gebäude/Zone (Heiz-, Kühlsollwert, Lüftung, Geräte, Personen), Vorlagen mit `Nutzung`
WOHNEN | BUERO | SCHULE | SONSTIGE (14 Zeilen). Der Lauf liest sie über `Konditionierungdatenweg`
in das VDI-6007-Modell; ohne Kalender gilt der Standardfahrplan (Nachtabsenkung 22–6 Uhr,
Wochenende, Ferien). **Aufheizoptimierung KP3 ist nicht gebaut**; das Konzept
(Konditionierungsprofile, Abschnitte 4.1–4.8) legt die Stufenformel fest:
Φ_n = Φ_stat(θ_T, T_a) + Δθ/(n·h)·Σ_k C_k·(1 − e^(−n·h/τ_k)), n = kleinstes n mit Φ_n ≤ P_auf,
P_auf = `Heizleistung_Max` oder (1 + ρ)·Φ_stat,max mit ρ = 20 %. Ergebnisse (geplant):
`Aufheiz_Leistung_Kw`, `Aufheizzeit_Max_H`, `Aufheizstunden_H`.

### 2.5 Lastgang-Quellen (klärt V12 aus Runde 1)

Stundenreihen werden **nicht persistiert** (`Tab_ErgebnisEnergiebedarf` nur Jahressummen). Ohne
Erzeugerlauf liefert `SimulationLaufCtrl.Bedarf(idProjekt, idKlimaregion, …)` →
`SimulationWaermebedarf.Waermebedarf_berechnen` → `Kanalsatz.Bedarf[HEIZUNG|BRAUCHWASSER|PROZESS|
KUEHLUNG][8760]` in kWh/h (Vorbild `StromspeicherAuslegungCtrl.SimulationslaufVorbereiten`). Die
Brauchwasser-Zapfreihe kommt aus `ZapfprofilCtrl.Rechnen` (8 760 kWh/h) bzw.
`BedarfsVorschauCtrl.ProjektVorschau`. **Die Auslegung braucht also keinen Simulationslauf und keine
neue Persistenz** — Bedarfsreihen je Kanal sind auf Abruf da.

### 2.6 Mehrere Puffer

`Z_AnlagePufferVerbund` (Parallelverbund: Q_max und Verluste addiert, Schwellen vom Leitspeicher,
Leitspeicher N = 1); eigenständige Puffer laden Rang für Rang nach `Ladeordnung` (Solar 10, WP 20,
BHKW 30, Kessel 40), entladen je Kanal nach `Entladeprio`. Ergebnis je Puffer in
`Tab_ErgebnisPufferspeicher`.


## 3 Was die Fachwelt je Speicherklasse sagt

### 3.1 Heizungspuffer {H}

Runde 1 (K1–K16). Ergänzung aus dieser Runde: Der Puffer wirkt nach dena-Gutachten als
Kurzzeitspeicher „über mehrere Stunden“; die Nutzungsart greift über den **Heizlastgang** ein
(Nachtabsenkung und Wochenende bei Büro/Schule erzeugen die Aufheizspitze am Morgen, Wohnen heizt
durch). Ein Aufheizkriterium — Puffer deckt die Differenz zwischen Aufheizleistung Φ_n und
Erzeugerleistung über n Stunden — ist erst mit KP3 belegbar (Abschnitt 2.4).

### 3.2 Brauchwasserspeicher {B}

| Thema | Aussage | Quelle |
|---|---|---|
| Verfahren | Summenlinie (Lindley) für Speichervolumen und Nachheizleistung, „für alle Systeme, Gebäude- und Nutzungsarten“, mit Zapfprofilen je Nutzungsart; DIN 4708 (N-Zahl) nur Wohnen | DIN EN 12831-3 (IKZ); EPOS: `TwwSpeicherauslegung` rechnet genau das |
| Auslegungsgang | 1 Lastprofil, 2 Energiebedarf der längsten Periode, 3 theoretisches Volumen, 4 Zuschläge Abstrahl-/Durchmischungsverluste (15–20 %), 5 Heizleistung, 6 Plausibilität Tagesbedarf, 7 Heizleistung TWE | BWP TWW 2023 |
| Vereinfacht (EFH) | 25 l/(P·d) bei 60 °C, bis ~10 Personen verdoppelt = Mindestvolumen; Umrechnung V_tsoll = V_60·(60 − t_cw)/(t_soll − t_cw) | BWP TWW 2023 |
| Hygiene | Großanlage > 400 l und/oder > 3 l Leitungsinhalt: ≥ 60 °C am Austritt, ≥ 55 °C in der Zirkulation, Vorwärmstufen einmal täglich 60 °C; „gespeicherte Warmwassermenge möglichst gering“ | DVGW W 551 (DVGW 2023), TrinkwV (UBA 2023), GdW 2024 |
| Effizienz | 60-°C-Niveau senkt die JAZ der Trinkwassererwärmung im Beispiel von 3,22 auf 2,19; Auswege: zweiter Erzeuger, Heizstab, Hochtemperatur-WP, Frischwasserstation, Ultrafiltration (45–47 °C nach Erprobung) | UBA 2023, GdW 2024 |
| Systeme | Speicher-Trinkwassererwärmer; Durchfluss (Frischwasserstation: hygienisch, Puffer „einige Grad“ höher, Zirkulation dahinter); Wohnungsstation (Kleinanlage, niedrigere Temperaturen); Tank-in-Tank („für Wärmepumpen ungünstig“, Volumenstrom begrenzt); Puffer mit integriertem Durchflusswärmeübertrager (hygienisch, begrenzte Temperaturen) | BWP TWW 2023, GdW 2024 |
| Bezugsgrößen | Hotel 140–220 l/(Gast·d), Krankenhaus 100–300 l/(Bett·d), Altenheim 25–40, Büro 10–40 l/(P·d) (Sekundär, nur Größenordnung) | baulinks/IKZ-Umfeld |

**Folge für EPOS-Plan:** Der Brauchwasserspeicher wird **nicht** neu bemessen — die
TWW-Speicherauslegung des Zapfprofils ist das Verfahren der Norm. Die Pufferauslegung **ruft sie**
und übernimmt ihr Ergebnis in den Puffer mit Set {B}: Topologie Speicher → Volumen (`NenninhaltL`);
Topologie Frischwasserstation/Wohnungsstation → **Pufferteil** aus D_max und dem Temperaturpaar des
Heizungspuffers (Abschnitt 4).

### 3.3 Kombipuffer {H,B}

| Thema | Aussage | Quelle |
|---|---|---|
| Bauformen | Heizungspuffer mit integriertem Trinkwasserbereiter (Tank-in-Tank) oder mit Durchflusswärmeübertrager; „hauptsächlich Heizungswasser bevorratet“, Hygieneanforderungen gelten trotzdem | GdW 2024, BWP TWW 2023 |
| Temperatur | Puffer muss über der Trinkwarmwasser-Solltemperatur liegen (Grädigkeit); bei Wärmepumpen begrenzt die Vorlauftemperatur die Zapftemperatur; Tank-in-Tank bringt das ganze Heizwasser auf Trinkwassertemperatur (Effizienzverlust) | BWP TWW 2023, GdW 2024 |
| Zonen | oben Bereitschaftszone Trinkwasser, unten Heizzone; Schichtung durch Ladelanzen und Rückführung je Betriebsweise (Zirkulation/Zapfung) erhalten | GdW 2024 |
| Solar-Kombi EFH | 10–15 m² Kollektor mit 600–1 000 l; 50–80 l/m²; 80 l/Person Vorwärmung (Sekundär) | energie-experten, baunetzwissen |

**Folge:** Kombipuffer = **Heizzone nach Runde 1 + Brauchwasserzone nach 3.2**, beide am
gemeinsamen Temperaturpaar des Behälters; EPOS setzt die Zonen über `Schichten_Anzahl` > 1,
`T_Nutz_BW` und die `Entnahme_*`-Anteile (heute nie gepflegt) — die Auslegung schreibt sie.

### 3.4 Prozesswärmepuffer {P}

| Thema | Aussage | Quelle |
|---|---|---|
| Rolle | Entkopplung von Erzeugung und Verbrauch über Stunden; Lastspitzen kappen, Spitzenlasterzeuger sparen | dena 2024 |
| Temperaturen | Prozesswärme 80 bis > 1 500 °C; Wasserpuffer nur für den unteren Bereich (drucklos < 95 °C) | dena 2024 |
| Verfahren | aus dem Lastgang: Erzeuger auf Mittel-/Grundlast, Puffer trägt die Spitze über ihre Dauer; Speichergröße aus Lastprofil und Tarif, nicht aus Faustwerten | Peak-Shaving-Literatur (allgemein) |

**Folge:** Der Prozesspuffer wird mit K6 (Deckung bei Erzeuger unter Spitze) und K7 (Taktziel) auf
der **Prozessreihe** bemessen; Spreizung aus dem Temperaturpaar des Puffers, weil EPOS keine
Prozesstemperatur kennt. Batch- und Schichtbetrieb stecken bereits im 168-h-Wochenprofil des
Prozesstyps. Warnung, wenn der Erzeuger-Vorlauf unter dem Prozessbedarf liegt, ist ohne
Prozesstemperatur nicht möglich → Lücke (Abschnitt 6, E-P21).

### 3.5 Erzeuger je Klasse

| Erzeuger | Heizungspuffer | Brauchwasser | Kombi | Prozess | Quellen |
|---|---|---|---|---|---|
| Wärmepumpe | K1–K7 (Runde 1); parallel/Reihe | 60 °C nur mit Hochtemperatur-WP, zweitem Erzeuger oder Heizstab; Frischwasserstation bevorzugt; Tank-in-Tank ungünstig | Vorlauf prüfen (EPOS W3 vorhanden); Heizstab für die einmal tägliche 60-°C-Aufheizung | selten; Temperatur begrenzt | BWP TWW 2023, GdW 2024, UBA 2023 |
| BHKW | Laufzeit ≥ 1 h, Faustwert 60 l/kW_th, 24–99 l/kW_th real; stromgeführt: Speicher für die Verschiebedauer (30 m³/MWh bei 30 K; Beispiel 14 h Betriebspause) | hohe Vorläufe (90/60 °C) decken 60 °C ohne Zusatz | natürlicher Kombi-Lader; Rücklauf unter 60 °C halten | gut geeignet bis 95 °C | OTH 2017, Mini-KWK, kwk-flexperten, SINTEG |
| Kessel Gas/Öl | nur gegen Takten (K3), modulierend meist ohne | Puffer nur als Vorlage für Frischwasserstation | wie Brauchwasser | Spitzenlast | heizung.de (Sekundär), EPOS `Kesselkennlinie` |
| Festbrennstoff | K9 (55/20/30 l/kW) bemessend | Kombi üblich (Scheitholz + Trinkwasser) | V_H nach K9 + V_B nach 3.2 | — | 1. BImSchV, BEG, BWP 2016 |
| Solarthermie | K10 (50–70 l/m²) | Vorwärmspeicher 80 l/Person (Sekundär); einmal täglich 60 °C (W 551) | Solar-Kombi 600–1 000 l EFH; Nachrang 30 % am Puffer (EPOS) | Prozesswärme < 95 °C möglich | energie-experten, baunetzwissen, GdW 2024 |

### 3.6 Mehrere Behälter

Serienschaltung erhält die Schichtung (Rücklauf des ersten wird Vorlauf des zweiten), Parallelschaltung
nach Tichelmann senkt Volumenströme je Behälter — nur Foren- und Herstellerwissen, keine Regel. EPOS
bildet den Parallelverbund als addierte Kapazität ab; die Auslegung liefert ein Gesamtvolumen und
nennt den Verbund nur im Hinweis.


## 4 Kopplung an Zapfprofil und Nutzungsarten — Vorschlag

### 4.1 Grundsatz

Die Pufferauslegung bemisst **je Speicherklasse aus den Bedarfsreihen des Projekts**, nicht aus
Gebäudeart-Faustwerten:

| Klasse | Bedarfsquelle | Verfahren | Ergebnis |
|---|---|---|---|
| {H} | `Kanalsatz.Bedarf[HEIZUNG]` (8 760 h) aus `SimulationLaufCtrl.Bedarf`; Erzeugerdaten | K1–K10 (Runde 1) | V_H |
| {B} | Zapfprofil-Zonen des Projekts (`ZapfprofilCtrl`, Wochenreihe) | `TwwSpeicherauslegung.Rechnen` (unverändert) | Topologie Speicher: `NenninhaltL`; sonst D_max und Ladeleistung |
| {H,B} | beide | V_H + V_B,Puffer mit V_B,Puffer = D_max·1000/(c·ΔT_B·(s_aus − s_ein)), ΔT_B = T_Puffer,oben − T_Rücklauf,Frischwasser (Vorgabe 40 K bei 65/25 °C) | Gesamtvolumen, Zonenanteile, `T_Nutz_BW`, `Schichten_Anzahl` |
| {P} | `Kanalsatz.Bedarf[PROZESS]` | K6, K7 mit Puffer-ΔT | V_P |

Die Schnittstelle zum Zapfprofil ist **konstantenfrei**: D_max in kWh und Ladeleistung in kW sind
unabhängig von c_w und Nutzanteil; erst die Umrechnung in Liter benutzt die Puffer-Seite (1,16,
Schwellen). Die Zapfprofil-Seite bleibt unverändert (Referenzprojekt 1045, Einfrierregel).

### 4.2 Nutzungsprofil statt Gebäudeart

Statt der Gebäudeart-Beispielhilfe (E-P11, Runde 1) leitet die Auslegung ein **Nutzungsprofil**
aus dem Projekt ab und zeigt es als Herkunft an:

| Nutzungsprofil | abgeleitet aus | Wirkung auf die Auslegung | Beleg |
|---|---|---|---|
| Wohnen | Zapf-Nutzungsart Wohnen/EFH/Studentenwohnheim; Konditionierung WOHNEN | Morgen-/Abendspitze Trinkwasser → Kombi oder Frischwasserstation; Heizung durchgehend; Sperrprofil nach Tarif | BWP TWW 2023 (Zapfprofile), DIN EN 12831-3 |
| Beherbergung (Hotel) | Zapf-Nutzungsart Hotel (je Zimmer, Messung) | Brauchwasser dominiert: große Bereitschaftszone, hohe Ladeleistung; Großanlage → 60 °C | DIN EN 12831-3 Nutzungsarten, EPOS-Katalog |
| Pflege/Krankenhaus | Seniorenheim, Krankenhaus | kontinuierlicher Bedarf, Hygiene vorrangig: Frischwasserstation oder Speicher mit täglicher 60-°C-Aufheizung; Heizstab/Zweiterzeuger bei WP | W 551, GdW 2024 |
| Büro/Schule | Konditionierung BUERO/SCHULE; Zapf-Nutzungsart fehlt im Katalog | Heizung dominiert: Nachtabsenkung und Wochenende → Aufheizspitze morgens (mit KP3), Sperr-/Dimmzeiten am Morgen; Brauchwasser klein (Altkatalog oder Faustwert 10–40 l/(P·d), Sekundär) | DIN EN 12831-3, Konzept Konditionierungsprofile |
| Gewerbe/Prozess | `Tab_Prozesswaerme` vorhanden | Prozesspuffer nach 3.4; Schichtbetrieb aus dem Wochenprofil | dena 2024 |

Fehlt eine Quelle (kein Zapfprofil, kein Kalender), bleibt die Vorlage Anlagentyp aus Runde 1 mit
Herkunft „Vorgabe“ und Hinweis.

### 4.3 Übernahme in den Puffer je Klasse

| Feld in `Tab_Pufferspeicher` | {H} | {B} | {H,B} | {P} |
|---|---|---|---|---|
| `Gesamtvolumen` | V_H | V_B | V_H + V_B,Puffer | V_P |
| `Nutzung_*` | H | B | H, B | P |
| `Vorlauf`/`Ruecklauf` | unverändert | T_Speicher (Zapfprofil) / Kaltwasser | Puffer-Paar | unverändert |
| `T_Nutz_BW` | — | aus Zapfprofil (≥ W-551-Mindesttemperatur bei Großanlage) | dito | — |
| `Schichten_Anzahl`, `Entnahme_*` | 1 | 1 | ≥ 2; Anteile aus V_H/(V_H + V_B) (Semantik der Entnahmehöhe in W3 am Modell prüfen) | 1 |
| `Bereitschaftsverluste` | Katalog, sonst Ökodesign (K11) | dito | dito | dito |
| `Schwelle_*` | unverändert | unverändert | unverändert | unverändert |
| Katalogsatz | nächster Nenninhalt, Bauform `Pufferspeicher` | `Kombispeicher`/`Solarspeicher` nur bei Bauform | Bauform `Kombispeicher` (W2) | `Pufferspeicher` |

Der Knopf „An Speicherauslegung übergeben…“ im Zapfprofil-Auslegungsdialog wird mit P2 aktiv und
öffnet die Pufferauslegung mit vorgewählter Klasse {B} bzw. {H,B} — das ist der dritte Einstieg
neben Kachel und ① Konfiguration und erfüllt ZU1.


## 5 Prüfung des Entwurfs gegen diese Runde

| Nr. | Befund | Folge |
|---|---|---|
| P17 | Das Mockup legt nur den Heizungspuffer aus; „Brauchwasser hat seine eigene Auslegung im Zapfprofil“ und F3 „Kombi getrennt, Summe nachrichtlich“ | **Ersetzen:** Schritt 1 bekommt die **Speicherklasse** (aus dem Set des gewählten Puffers, änderbar); {H,B} rechnet beide Zonen und zeigt sie im Ergebnis als gestapelte Zonen; F3 wird zu „Kombi = Summe der Zonen, bemessend“ |
| P18 | Gebäudeart (EFH/MFH/Gewerbe) als Beispielhilfe (E-P11) | durch das **Nutzungsprofil** aus 4.2 ersetzen; Beispielwerte folgen dem Profil |
| P19 | Vorlagen je Anlagentyp (sechs) | bleiben für die Heizzone; die Brauchwasserzone hat keine Vorlage, sondern das Zapfprofil; die Prozesszone hat die Vorlage „Prozesspuffer“ (K6/K7, Puffer-ΔT) → **sieben** Vorlagen |
| P20 | Keine Hygieneprüfung | Hinweise: Großanlage (> 400 l Trinkwasser oder > 3 l Leitung) → 60 °C/55 °C; Wärmepumpe mit Vorlauf unter `T_Nutz_BW` + Grädigkeit → W3-Text; Tank-in-Tank bei WP → Warnung „ungünstig“ |
| P21 | Übernahme nur Volumen/Katalog/Schwellen | zusätzlich `Nutzung_*`, `T_Nutz_BW`, `Schichten_Anzahl`, `Entnahme_*` (Tabelle 4.3) |
| P22 | Lastgang-Quelle offen (V12) | geklärt: `SimulationLaufCtrl.Bedarf` je Kanal, Zapfreihe aus `ZapfprofilCtrl` — kein Simulationslauf nötig |
| P23 | Prozesspuffer fehlt im Mockup | Vorlage „Prozesspuffer“ mit Hinweis „Temperaturniveau des Prozesses prüfen“ (bis E-P21 gelöst) |


## 6 Verbesserungsvorschläge dieser Runde (Fortsetzung V1–V20)

| Nr. | Vorschlag | Nutzen | Stufe | Empfehlung |
|---|---|---|---|---|
| V21 | **Klassenweiche** in der Auslegung: Set des Puffers → Verfahren je Zone (4.1) | eine Auslegung für alle vier Klassen | P1 | ja |
| V22 | **Zapfprofil-Kopplung:** `TwwSpeicherauslegung.Rechnen` aus der Pufferauslegung rufen (gleiche Assembly, `internal` genügt), D_max und Ladeleistung übernehmen, Frischwasser-Topologien als Pufferteil umrechnen | Norm-Verfahren wiederverwendet, Zapfprofil unverändert | P1 | ja |
| V23 | **Nutzungsprofil** aus Zapf-Zonen, Konditionierungs-`Nutzung` und Prozessdaten ableiten; Gebäudeart nur noch als Anzeige | Auslegung folgt der Nutzung, nicht einem Freitext | P1 (Ableitung), P2 (Anzeige) | ja |
| V24 | **Kombi-Zonen schreiben:** `Schichten_Anzahl` ≥ 2, `T_Nutz_BW`, `Entnahme_*` aus den Zonenanteilen | die Simulation rechnet die Bereitschaftszone, die heute nur bei N > 1 wirkt | P1 W3 | ja; Semantik der Entnahmehöhe prüfen |
| V25 | **Hygiene-Hinweise** (Großanlage, 60/55 °C, tägliche Aufheizung der Vorwärmstufe, Tank-in-Tank bei WP) mit Codes | Planungssicherheit; W 551 ist Regel der Technik | P1 (Codes), P2 (Texte) | ja |
| V26 | **Frischwasserstation als Topologie der Heizzone:** Pufferteil V_B,Puffer mit ΔT_B aus T_Puffer,oben und Frischwasser-Rücklauf (Vorgabe 65/25 °C → 40 K; Expertenfeld) | größte Effizienzreserve bei Wärmepumpen im MFH (UBA, GdW) | P1 | ja |
| V27 | **Dritter Einstieg** aus dem Zapfprofil-Auslegungsdialog („An Speicherauslegung übergeben…“ aktivieren, Ziel Pufferauslegung mit Klasse {B}/{H,B}) | erfüllt ZU1; Anwender kommt vom Brauchwasser zum Puffer | P2 | ja |
| V28 | **BHKW stromgeführt:** Kriterium „Verschiebedauer“ V = P_th·t_flex/(c·ΔT·(s_aus − s_ein)), t_flex Vorgabe 2 h (Experte), aktiv nur bei `modeBHKW` stromgeführt | flexibler Betrieb braucht Speicherstunden, nicht Faustwerte | P1 | ja (Vorgabe ohne Norm, als Experte gekennzeichnet) |
| V29 | **Prozesstemperatur** `Tab_Prozesswaerme.Vorlauf/Ruecklauf` (Schemaschritt) und Warnung > 95 °C; Wiki-Seite Prozesswärme | heute kann kein Erzeuger-Vorlauf gegen den Prozess geprüft werden; Wiki-Verweise laufen ins Leere | eigener Folgeauftrag | ja, nicht P1 |
| V30 | **Aufheizkriterium** V_auf = (Φ_n − P_gen)·n·h/(c·ΔT·(s_aus − s_ein)) für Büro/Schule | Puffer statt Erzeugerüberdimensionierung für die Morgenspitze | nach KP3 | später |
| V31 | **Zapf-Nutzungsarten Büro/Schule/Gewerbe** im Auslieferungskatalog ergänzen (Katalogpaket, Herkunft Altkatalog/DIN EN 12831-3-Profilfamilie) | das Nutzungsprofil Büro/Schule hat sonst keine Brauchwasserquelle | Folgeauftrag Zapfprofil | ja |
| V32 | **ID-Mapping** Gebäudeart ↔ Zapf-Nutzungsart ↔ Konditionierungs-`Nutzung` (Tabelle statt Freitext, CLAUDE.md „Beziehungen über IDs“) | Nutzungsprofil ohne Textvergleich | Folgeauftrag | ja |


## 7 Offene Entscheide für den Anwender (Fortsetzung)

| Nr. | Frage | Empfehlung |
|---|---|---|
| E-P17 | Norm-PDFs im Repositorium | **entschieden 01.10.2026: belassen** für Konzeption und Umsetzung, danach entfernen |
| E-P18 | Brauchwasser- und Kombizone über `TwwSpeicherauslegung` des Zapfprofils rechnen (V22), Zapfprofil unverändert? | ja |
| E-P19 | Kombipuffer = Summe der Zonen, bemessend; Zonenanteile und `T_Nutz_BW` in den Puffer schreiben (V24)? | ja; ersetzt F3 „Summe nachrichtlich“ |
| E-P20 | Nutzungsprofil (Wohnen, Beherbergung, Pflege/Krankenhaus, Büro/Schule, Gewerbe/Prozess) aus dem Projekt ableiten statt Gebäudeart (V23)? | ja; ersetzt E-P11 „Gebäudeart als Beispielhilfe“ |
| E-P21 | Prozesstemperatur als Schemaschritt und Wiki-Seite Prozesswärme als eigener Folgeauftrag (V29)? | ja, nach P2 |
| E-P22 | Frischwasserstation als Topologie der Heizzone mit Vorgabe 65/25 °C (V26)? | ja |
| E-P23 | BHKW-Verschiebedauer 2 h als Expertenvorgabe (V28)? | ja |
| E-P24 | Dritter Einstieg aus dem Zapfprofil-Auslegungsdialog (V27) in P2? | ja |
| E-P25 | Zapf-Nutzungsarten Büro/Schule/Gewerbe und ID-Mapping als Folgeaufträge (V31, V32)? | ja |


## 8 Folgen für den Bauplan P1 (Ergänzung zu Runde 1, Abschnitt 8)

1. **W1 Schema 159:** `Tab_PufferAuslegung` zusätzlich mit `Klasse` (Set), `Nutzungsprofil`,
   `Volumen_H_l`, `Volumen_B_l`, `Volumen_P_l`, `DeltaT_B_K`, `T_Puffer_Oben_C`, `BHKW_Verschiebedauer_h`;
   Vorlagen: sechs Anlagentypen + „Prozesspuffer“; Vorgabeschlüssel `Pufferauslegung.Frischwasser.*`,
   `Pufferauslegung.BHKW.Verschiebedauer_h`.
2. **W2 Kern:** Klassenweiche (V21); Heizzone K1–K10; Brauchwasserzone über
   `TwwSpeicherauslegung.Rechnen` mit der Wochenreihe aus `ZapfprofilCtrl` (V22, V26); Prozesszone
   K6/K7 auf `Kanalsatz.Bedarf[PROZESS]`; Bedarfsreihen über `SimulationLaufCtrl.Bedarf` (2.5);
   BHKW-Verschiebedauer (V28); Nutzungsprofil-Ableitung (V23); Hygiene-Codes (V25).
3. **W3 Controller:** Übernahme nach Tabelle 4.3 (V24); Vorbelegung liest Set, Zonen, Topologie,
   Konditionierungs-`Nutzung`, Prozessdaten.
4. **W4 Tests:** Kombi-Beispiel (fiktiv: D_max 24 kWh, 65/25 °C → V_B,Puffer = 24 000/(1,16·40·0,85) =
   609 l; V_H 2 000 l; Summe 2 609 l → Nenninhalt 3 000 l), Prozess-Beispiel auf der Prozessreihe
   eines Testprojekts (1041 hat `Z_Projekt_Prozesswaerme`), Nutzungsprofil-Ableitung je
   Testprojekt, Frischwasser-Topologie, BHKW-Verschiebedauer; Referenzlauf byte-gleich.
5. **Mockup:** Schritt 1 „Speicherklasse“ und Nutzungsprofil-Herkunft, Ergebnis mit Zonen,
   Vorlage „Prozesspuffer“, Hygiene-Hinweise, Einstieg aus dem Zapfprofil als Bild.
6. **Nicht P1:** V27 (P2), V29, V31, V32 (Folgeaufträge), V30 (nach KP3).


## 9 Status der Recherche (01.10.2026)

| Teil | Stand | Belegtiefe |
|---|---|---|
| Heizungspuffer nach Erzeugern (Runde 1) | fertig, K1–K16 | Primärquellen für WP (BWP, VdZ, Fraunhofer), Festbrennstoff (BImSchV, BEG), BHKW (OTH, Mini-KWK), Ökodesign (EU, BDH), § 14a EnWG recherchiert, nach Anwenderentscheid 01.10.2026 nicht relevant; VDI 4645 nur sekundär |
| Brauchwasserspeicher | fertig | DIN EN 12831-3 (IKZ), BWP TWW 2023, W 551 (DVGW, UBA, GdW); Verfahren in EPOS vorhanden |
| Kombipuffer | fertig | BWP TWW 2023, GdW 2024; Zonenlogik aus dem EPOS-Modell |
| Prozesswärmepuffer | ausreichend für P1 | nur dena und allgemeine Peak-Shaving-Literatur; keine Richtwerte — Lastgang-Kriterium trägt |
| Nutzungsarten-Kopplung | Vorschlag fertig | DIN EN 12831-3 Nutzungsarten, EPOS-Katalog (sechs Zapf-Nutzungsarten), Konditionierungs-`Nutzung`; Lücken Büro/Schule/Gewerbe und ID-Mapping benannt |
| Bestand EPOS-Plan | fertig (zwei Sonnet-Befunde) | Klassen, Rechenweg, Zapfprofil-Schnittstelle, Lastgang-Quellen, Konditionierung, Verbund |
| Normdateien vom 01.10.2026 (Runde 3) | fertig | VDI 4645 Hauptblatt (Entwurf 2026-03): 3 l/kW Vorprüfung, Faustwerte 20/3 l/kW nach Gerätetyp, Gleichung 22 Mindestlaufzeit, Gleichung 23 Abschaltzeiten mit Tabellen 14/15 — K1–K4/K14 bestätigt bzw. präzisiert, V37–V39; VDI-MT 4645 Blatt 1 = Qualifikationsblatt (kein Ertrag); prEN 15316-5:2024 Schichtenmodell, Formel (3) kWh/d → W/K, Standardaufteilung 0,10/0,16/0,37/0,37, Verbund seriell/parallel; DIN EN 15332 + A1 Prüfbedingung 65 °C bzw. ≥ 45 K über 20 °C; Vergleich mit dem Puffermodell des Kerns (Verlustansatz SOC-anteilig → V35 Folgeauftrag) |
| Sekundärquelle energie-experten.org (Runde 3, Abschnitt 7) | fertig | acht Seiten gegengelesen: Plausibilitätsband 12–35 l/kW (DIN EN 15450), Zirkulation 30–40 %, Starts 10–15/Tag und 2 000–3 000 je Heizperiode, DIN EN 303-5 für Festbrennstoff, VDI-Whitepaper (nur Meldung) — V40–V43, E-P29 |
| Offen | VDI-Whitepaper „Thermische Speicher in Wärmepumpensystemen“ beschaffen (E-P29); Herstellerangaben Mindestlaufzeit nur allgemein (VDI 4645 verweist auf den Hersteller); Prozesstemperatur fehlt im Datenmodell; Raumtemperatur des Aufstellorts fehlt im Modell (V35) | — |


## 10 Quellenverzeichnis der zweiten Runde (abgerufen 01.10.2026)

- BWP, Leitfaden Trinkwassererwärmung, Stand Februar 2023 — https://www.waermepumpe.de/fileadmin/user_upload/waermepumpe/08_Sonstige/Filedump/BWP_LF_TWW_2023.pdf
- GdW/dena, Praxisleitfaden für Wärmepumpen in Mehrfamilienhäusern, 03/2024 — https://www.gebaeudeforum.de/fileadmin/gebaeudeforum/Downloads/Leitfaden-Handbuch/Leitfaden_Waermepumpen-in-Mehrfamilienhaeusern.pdf
- Umweltbundesamt, Factsheet Trinkwarmwasserkonzepte, 01.11.2023 — https://www.umweltbundesamt.de/system/files/medien/11850/publikationen/factsheet_trinkwarmwasserkonzepte.pdf
- DVGW, Energieeinsparungen im Warmwasserbereich in Trinkwasser-Installationen, Langfassung, März 2023 — https://www.dvgw.de/medien/dvgw/leistungen/publikationen/energiesparen-warmwasser-dvgw-langfassung.pdf
- IKZ, DIN EN 12831-3 als Ersatz für DIN 4708 — https://www.ikz.de/fileadmin/user_upload/008_011.pdf
- kwk-flexperten.net, Wärmenutzung und Wärmespeicher — https://www.kwk-flexperten.net/w%C3%A4rmenutzung-und-w%C3%A4rmespeicher
- BMWi, SINTEG Blaupause 11: Flexibilisierung von KWK-Anlagen — https://www.bundeswirtschaftsministerium.de/Redaktion/DE/Dossier/Sinteg/4-3-blaupause.pdf?__blob=publicationFile&v=1
- dena, Gutachten zur Wärmespeicherstrategie, August 2024 — https://www.dena.de/fileadmin/dena/Dokumente/Projektportrait/Energiepolitische_Beratung_des_Bundesministeriums_fuer_Wirtschaft_und_Klimaschutz/Gutachten_zur_Waermespeicherstrategie.pdf
- energie-experten.org, Auslegung und Größe von Solarthermie-Anlagen (Sekundär) — https://www.energie-experten.org/heizung/solarthermie/solarthermieanlage/auslegung
- baunetzwissen.de, Solar- und Pufferspeicher (Sekundär) — https://www.baunetzwissen.de/heizung/fachwissen/waermepumpen-und-solarenergie/solar--und-pufferspeicher-161350
- Runde 1: siehe `2026-10-01_Recherche_Pufferspeicherauslegung.md`, Abschnitt 9
