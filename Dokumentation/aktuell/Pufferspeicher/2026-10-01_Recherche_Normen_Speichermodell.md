# Recherche Pufferspeicher-Auslegung, dritte Runde — Normdateien vom 01.10.2026: VDI-MT 4645 Blatt 1, prEN 15316-5:2024, DIN EN 15332 (01.10.2026)

**Zweck.** Dritte Runde der Recherche zur Pufferspeicher-Auslegung, Fortsetzung von
[`2026-10-01_Recherche_Pufferspeicherauslegung.md`](2026-10-01_Recherche_Pufferspeicherauslegung.md)
(Runde 1: Heizungspuffer nach Erzeugern, Kriterien K1–K16, Vorschläge V1–V20, Entscheide E-P8–E-P17)
und [`2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md`](2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md)
(Runde 2: Speicherklassen, Zapfprofil-Kopplung, V21–V32, E-P18–E-P25). Anlass sind vier Normdateien,
die der Anwender am 01.10.2026 unter `Quellen/Waermespeicher-Tool/` abgelegt hat (Sync-Commits
6b8806fe und 5dadb3e5). Dieses Papier sagt, was jede Datei ist, was sie für die Auslegung und für das
Puffermodell des Kerns hergibt, und schreibt Kriterientabelle, Vorschläge und Entscheide fort.

**Grenzen.** Die Normen sind lizenziert (Anwenderentscheid E-P17: sie bleiben für Konzeption und
Umsetzung im Repositorium). Sie werden hier mit Abschnitts- und Formelnummern **zitiert, nicht
abgeschrieben**; Zahlen stehen nur, wo sie für den Rechenweg gebraucht werden. Keine Produktdaten.
Alle Rechenbeispiele sind fiktiv.


## 1 Befund in Kürze

| Datei unter `Quellen/Waermespeicher-Tool/` | Was es ist | Ertrag für die Auslegung |
|---|---|---|
| VDI-MT 4645 Blatt 1:2023-04 (zweisprachig, 21 Seiten) | **Qualifikationsblatt** („Mensch und Technik“): Schulungsinhalte, Qualifizierungsnachweis, Muster für Teilnahmebescheinigung; ersetzt VDI 4645 Blatt 1:2018-03 | **keiner** — keine Auslegungsregel, kein Richtwert. Es bestätigt nur die Gliederung des Hauptblatts (Abschnitt 2). **Das Hauptblatt VDI 4645 fehlt weiterhin** |
| E DIN EN 15316-5:2024-07 (prEN 15316-5:2024, deutsch/englisch, 98 Seiten; Entwurf, Ersatz für DIN EN 15316-5:2017-09) | Rechenverfahren für Speicher der Raumheizung und Trinkwassererwärmung: **Schichtenmodell** (Methode A, 1 bis 10 Schichten) und Einvolumen-Modell (Methode B), Bereitschaftsverlust als W/K, Standardwerte im Anhang B | hoch: Umrechnung kWh/d → W/K (Formel 3), Lade-/Abschaltlogik über Temperaturen, Lage der Ein- und Austritte je Schicht, Reihen-/Parallelverbund, Vergleichsmaßstab für das Puffermodell des Kerns (Abschnitt 3) |
| DIN EN 15332:2020-01 (EN 15332:2019, deutsch, 18 Seiten) | **Prüfnorm** für den Nennwärmeverlust Q_B [kWh/d] von Trinkwarmwasserspeichern bis 2 000 l; Grundlage der Ökodesign-Deklaration (Anhang ZA/ZB zu 814/2013 und 812/2013) | mittel: belegt die Prüfbedingung 65 °C bzw. ≥ 45 K über 20 °C Umgebung hinter dem Katalogwert `Bereitschaftsverluste`; nimmt primäre Heizpufferspeicher aus dem Anwendungsbereich aus (Abschnitt 4) |
| E DIN EN 15332/A1:2023-01 (EN 15332:2019/prA1:2022, deutsch/englisch, 28 Seiten; Entwurf) | Änderung der Prüfnorm: Begriffe, Fühlerlagen, Reglerhysterese, Dämmung der Anschlüsse, Messablauf | gering: Präzisierungen des Prüfverfahrens, keine neuen Zahlen für die Auslegung |


## 2 VDI-MT 4645 Blatt 1:2023-04 — Qualifikationsblatt, kein Auslegungsblatt

- Inhalt: Anwendungsbereich, Qualifizierungswege, Schulungsinhalte (Anhang A), Nachweise und Register,
  Qualitätsmerkmale von Schulungen, Muster (Anhänge B und C). Die Tabelle der Schulungsinhalte verweist
  auf die Abschnitte des **Hauptblatts VDI 4645**: 8.5 Trinkwassererwärmung, 8.6 Dimensionierung der
  Wärmepumpe, 8.7 Betriebsweise, **8.8 Wärmespeicher**, 8.10 Nutzung von Solarenergie, 8.11 und 9.11
  Anlagenkonzept/Hydraulik (9.x für größere Gebäude).
- Die Richtwerte, die Runde 1 nur sekundär belegen konnte (20 l/kW für die Abtauung, 8.8.4; Puffer bei
  Sperrzeiten), stehen also im **Hauptblatt**, Abschnitt 8.8 — dieses Blatt liegt weder im Repositorium
  noch unter `Z:`. Gültig ist die Ausgabe VDI 4645:2018-03; der Stand einer Überarbeitung ist beim VDI
  zu prüfen.
- **Folge:** E-P16 bleibt offen und wird umformuliert: *Hauptblatt* VDI 4645 beschaffen, nicht ein
  weiteres MT-Blatt (Abschnitt 5.3). Für P1 ändert sich nichts — die Vorlage „Wärmepumpe“ trägt ihre
  Werte aus BWP 2016, VdZ 2025, Fraunhofer WP-QS 2025 und dem Wärmespeicher-Tool (Runde 1, K2–K4).


## 3 prEN 15316-5:2024 — das Schichtenmodell der Norm neben dem Puffermodell des Kerns

### 3.1 Was die Norm rechnet

- **Methode A** (6.4.3): Speicher als Stapel von N_vol Schichten (1 bis 10, Tabelle 9), Energiebilanz
  je Schicht (Bild 2, Formel 1 — nur illustrativ); **Wärmeleitung zwischen den Schichten wird
  vernachlässigt** (Anmerkung 2 zu 5.4), die Schichtung entsteht aus Entnahme, Eintrag und einer
  Mischregel (Schritt 7, Anhang C: Matrixverfahren in zwei Durchgängen, für Tabellenkalkulation
  und Software gedacht). Acht Schritte je Zeitschritt: Temperaturen sammeln, Zapfvolumen,
  Entnahme Trinkwasser (Volumen, dann Energie), Entnahme Heizung, Energieeintrag, Umschichten,
  Verluste und Endtemperaturen.
- **Methode B** (6.4.4): ein Volumen, Energiebilanz mit nutzbarer Energie oberhalb der
  Mindesttemperatur; gleichzeitiger Eintrag des Erzeugers während der Zapfung (Formeln 47–49) —
  das ist im Kern die Bilanz, die auch das Wärmespeicher-Tool (`storage_sim.simulate`) rechnet.
- **Eingangsdaten** (Tabelle 6): Gesamtvolumen, Schichtanteile f_vol,i, Bereitschaftsverlust-
  koeffizient H_sto;ls [W/K], je Eintrag die Schicht, Leistung und Wärmeübertrager, je Erzeuger
  **Abschalttemperatur (Sollwert) und Einschalttemperatur**, je Austritt die Schicht. Solar liegt
  per Vorgabe in Schicht 1 (unten).
- **Erzeugerlogik** (Schritt 6, Anmerkung 4): Erzeuger laufen **ein/aus** und schalten ein, wenn die
  Temperatur der Fühlerschicht N_vol,bu nach der Entnahme unter die Einschalttemperatur fällt;
  sie wirken nur auf die Fühlerschicht und die darüber liegenden. Prioritäten bei mehreren
  Erzeugern: Solar vor Heizungsanschluss vor elektrischer Nachheizung in der Heizperiode,
  außerhalb Nachheizung vor Heizungsanschluss (Tabelle B.7).
- **Bereitschaftsverlust** (6.3.2): aus dem deklarierten Tagesverlust nach Formel (3)
  H_sto;ls = 1000·Q_stby;ls;ref / (24·(ϑ_set;ref − ϑ_amb;ref)); fehlt die Deklaration, Formel (4)
  H = (c1 + c2·V^c3)·1000/… mit Tabelle B.6 — die nur elektrische Speicher (EN 60379/EN 50440) und
  Solarspeicher (EN 12977-3/-4) kennt, **keinen Heizungspuffer**. Verteilung auf die Schichten nach
  Mantelfläche (Formel 5) oder U-Wert der Dämmung (Formeln 6 und 7, h_a = 8 W/(m²·K)).
- **Standardwerte** (Anhang B): vier Schichten mit Anteilen **0,37 / 0,37 / 0,16 / 0,10** von unten
  (Tabelle B.4); Trinkwasser-Austritt Schicht 4, Kaltwasser-Eintritt Schicht 1, Heizungs-Ein- und
  -Austritt Schicht 3, Nachheizung Schicht 3, Solar Schicht 1 (Tabelle B.5; Methode B: alles 1);
  Verbund mehrerer Behälter **seriell oder parallel** (Tabelle B.10) mit Prioritätsreihenfolge
  (Tabelle 8); Vorgaben 40 °C Trinkwarmwasser, 13,5 °C Kaltwasser (Tabelle B.13).

### 3.2 Vergleich mit dem Puffermodell des Kerns (`SimulationPufferspeicher.cs`, `ProjektPuffer.cs`)

| Größe | prEN 15316-5:2024 | EPOS-Plan heute | Folge |
|---|---|---|---|
| Zustandsgröße | Temperatur je Schicht | Füllstand SOC [kWh] zwischen Rücklauf und Vorlauf; Schichten als Energiescheiben (`Schichten_Anzahl`, `Hoehe`, `Lambda_Eff`) | gleichwertig für die Auslegung; EPOS rechnet die Wärmeleitung zwischen Schichten, die die Norm bewusst weglässt — feiner, kein Änderungsbedarf |
| Ein-/Ausschalten | Einschalt- und Abschalttemperatur an der Fühlerschicht | `Schwelle_Ein`/`Schwelle_Aus` am SOC (Hysterese 10 %/95 %), Nachrang `Schwelle_Aus_Nachrang` | derselbe Zweipunktgedanke; bei einem Volumen sind Temperatur- und SOC-Schwelle ineinander umrechenbar (ϑ = ϑ_RL + SOC/Q_max·ΔT). Kriterium K7 (Zweipunkt) der Auslegung bleibt mit den Schwellen des Projektpuffers (V2) |
| Kombispeicher | Trinkwasser-Austritt oben (Schicht 4), Heizung mittig (Schicht 3), Erzeuger wirkt ab Fühlerschicht nach oben | Bereitschaftszone oben (`T_Nutz_BW`, `Entnahme_BW` 1,0), Heizung/Prozess tiefer (`Entnahme_*`), wirksam nur bei `Schichten_Anzahl` > 1 | stützt V24 (Zonen schreiben, `Schichten_Anzahl` ≥ 2); die Standardanteile 0,10 oben / 0,16 / 0,37 / 0,37 sind eine **Vorgabehilfe** für die Zonenanteile, keine Norm für den Heizpuffer (Entscheid E-P27) |
| Bereitschaftsverlust | H [W/K] × (ϑ_Schicht − ϑ_Umgebung) je Schicht und Stunde | `Bereitschaftsverluste` [kWh/24h] ÷ 24 bei vollem Speicher, **anteilig zum SOC** (`VerlustProStunde`), Schichtanteil nach `Schicht_Verluste` | Abweichung in beide Richtungen: bei leerem Puffer rechnet EPOS keinen Verlust, obwohl der Behälter noch auf Rücklauftemperatur über Raumtemperatur steht; bei vollem Puffer rechnet EPOS den 45-K-Wert, auch wenn das Temperaturpaar nur 35 K über dem Raum liegt. Für die **Auslegung** unerheblich (D1/D2 rechnen mit dem Modell des Kerns, V35 ist Folgeauftrag) |
| Mehrere Behälter | seriell/parallel (B.10), Priorität (Tabelle 8) | `Z_AnlagePufferVerbund` Parallelverbund (Q_max und Verluste addiert, Schwellen vom Leitspeicher) | Norm deckt beide Einbindungen; K12 „Einbindung“ der Auslegung (parallel/Reihe) ist normkonform benannt |
| Solar | Eintrag unten, Schicht 1 | Nachrang-Schwelle 30 % am Puffer (Referenzprojekt 1049, Einfrierregel) | kein Änderungsbedarf |

### 3.3 Ertrag für die Auslegung

1. **W/K neben kWh/d** (V33): Das Ergebnisblatt zeigt den Bereitschaftsverlust des gewählten
   Katalogsatzes oder der Ökodesign-Vorgabe (K11) zusätzlich als H = Q_B·1000/(24·45) W/K — die
   Umrechnung der Formel (3) mit der Prüfbedingung 45 K (Abschnitt 4). Beispiele: 2,5 kWh/d →
   2,31 W/K; Klasse-C-Grenze 1 000 l (3,57 kWh/d) → 3,30 W/K; 2 000 l (4,58 kWh/d) → 4,24 W/K.
2. **Betriebsverlust statt Prüfwert** in der Nutzen-Aufwand-Zeile (V16, V34): Jahresverlust
   Q_a ≈ Q_B · (ϑ_Puffer,mittel − ϑ_Raum)/45 K · 365 — der Prüfwert gilt bei 45 K, im Betrieb liegt ein
   Heizungspuffer meist bei 25–35 K über dem Aufstellraum (Beispiel: 45 °C mittel, 15 °C Raum →
   Faktor 0,67). Herkunft „EN 15316-5 Formel 3, Prüfbedingung EN 15332“ in der Herkunftsmarke.
3. **Zonenanteile mit Vorgabehilfe** (E-P27): Beim Kombipuffer schlägt die Übernahme (Runde 2,
   Tabelle 4.3) die Zonenanteile aus V_H/(V_H + V_B) vor; wo keine Zonen gerechnet wurden, dient die
   Standardaufteilung der Norm (oben 10 % + 16 % Bereitschaft, unten 2 × 37 %) als **Beispielwert**
   mit Herkunftsmarke „Vorgabe“, nicht als Regel.
4. **Reihen- und Parallelverbund** bleiben in der Auslegung als Einbindung des Behälters (K12)
   benannt; der Verbund mehrerer Behälter ist Sache des Kerns (`Z_AnlagePufferVerbund`), nicht der
   Auslegung (Runde 2, Abschnitt 3.6).


## 4 DIN EN 15332:2020-01 mit Änderung A1 (Entwurf 2023-01) — die Prüfbedingung hinter dem Katalogwert

- **Anwendungsbereich** (Abschnitt 1): energetische Bewertung von Trink-/Sanitärwarmwasser-
  Speichersystemen bis **2 000 l**; **primäre Heizpufferspeicher werden nicht behandelt**, ebenso nicht
  die Speicher in Kombi-Boilern. Die Begriffe „primärer Heizpuffertank“ (nur Primärwasser) und
  „Puffertank“ (geschlossener Wärmespeicher, der Energie aus verschiedenen Quellen aufnimmt und später
  abgibt) sind dennoch definiert (3.24, 3.25; in A1 neu nummeriert 3.15, 3.16).
- **Nennwärmeverlust Q_B** (3.19, 5.3): Energieverlust in kWh/d bei **65 °C ± 3 K** Speichertemperatur
  oder **mindestens 45 K** über der Umgebung, Umgebung **20 °C ± 3 K**, Luftgeschwindigkeit ≤ 0,25 m/s;
  Messung nach Beharrung über mindestens zwei weitere 24-h-Zeiträume, jeweils vom Abschalten des
  Thermostats zum Abschalten; der Verlust ist der elektrische Verbrauch im Beharrungszustand.
  Anschlüsse sind gedämmt (Tabelle 1: 20 mm bis 22 mm Innendurchmesser, 30 mm bis 35 mm — Fassung
  A1). A1 verlangt zusätzlich eine Reglerhysterese ≤ ±1 K, legt die Fühler oben (≤ 25 mm unter dem
  Zapfpunkt) und unten (≤ 25 mm über dem Boden) fest und fordert T5 ≥ T4 − 15 K am Boden.
- **Baureihen** (5.1): geprüft werden kleinster und größter Behälter, Zwischengrößen per
  Interpolation über das Nennvolumen, solange das Volumenverhältnis 2 : 1 nicht übersteigt;
  Simulationsergebnisse (z. B. bei Vakuumdämmung) sind mit Vergleichsmessungen zu prüfen, Toleranz
  5 %.
- **Zuordnung zur Ökodesign-Verordnung** (Anhang ZA, Tabelle ZA.1): der „Stillstandsverlust“ nach
  814/2013 Anhang II 2.1 ist der Nennwärmeverlust dieser Norm (Abschnitte 4.2, 5.3), das
  „Speichervolumen“ das tatsächlich gemessene (5.4). Für die **Energieeffizienzklasse** nach 812/2013
  verweist Anhang ZB auf dieselben Abschnitte.

**Ertrag:** Der Katalogwert `Bereitschaftsverluste` [kWh/24h] in `Tab_Pufferspeicher(_STAMM)` ist
der Prüfwert bei 45 K — die Auslegung beschriftet ihn so („kWh/d bei 45 K“) und rechnet ihn nach
Abschnitt 3.3 um. Dass die Prüfnorm Heizpufferspeicher ausnimmt, ändert nichts an der Deklaration:
die Verordnungen 812/2013 und 814/2013 erfassen Warmwasserspeicher für Heizzwecke bis 2 000 l
(Runde 1, K11), und die Hersteller deklarieren den Stillstandsverlust nach demselben 45-K-Verfahren
(EN 12897 für indirekt beheizte Speicher nutzt dieselben Randbedingungen). Für Puffer **über
2 000 l** gibt es keine Deklarationspflicht — die Vorgabe nach K11 ist dort eine Extrapolation und
wird so markiert (Warnhinweis mit Code, V20).


## 5 Folgen

### 5.1 Kriterientabelle (Runde 1, Abschnitt 4) — Ergänzungen

| Nr. | Ergänzung |
|---|---|
| K11 Bereitschaftsverlust | Herkunft „Prüfwert bei 45 K (EN 15332 5.3)“; Anzeige zusätzlich in W/K nach EN 15316-5 Formel (3); Betriebsverlust für die Nutzen-Aufwand-Zeile mit dem Faktor (ϑ_Puffer − ϑ_Raum)/45 K; über 2 000 l als Extrapolation gekennzeichnet |
| K12 Einbindung | Reihen- und Parallelschaltung sind in EN 15316-5 (Tabelle B.10) als die beiden Verbundarten benannt — Wortwahl der Auslegung bleibt |
| K7 Zweipunkt | Norm-Erzeugerlogik ist Ein/Aus an der Fühlerschicht (Schritt 6, Anmerkung 4): bestätigt das Zweipunktkriterium; Modulationsmodus (V4) bleibt EPOS-eigene Erweiterung |

### 5.2 Verbesserungsvorschläge (Fortsetzung V1–V32)

| Nr. | Vorschlag | Nutzen | Aufwand | Empfehlung |
|---|---|---|---|---|
| V33 | **W/K-Anzeige** des Bereitschaftsverlusts im Ergebnis (Formel 3, 45 K) mit Herkunftsmarke | Vergleichbarkeit mit Datenblättern, die W/K oder W angeben; Grundlage für V34 | klein (Rechenzeile im DTO) | **A, P1** |
| V34 | **Betriebsverlust** in der Nutzen-Aufwand-Zeile (V16): Q_B·ΔT_Betrieb/45 K·365 statt Q_B·365 | realistische kWh/a (Faktor 0,55–0,8) | klein | **A, P1** |
| V35 | **Verlustmodell der Simulation umgebungsbezogen** rechnen (H·(ϑ_Schicht − ϑ_Raum) statt SOC-anteilig) | physikalisch richtiger bei leerem und bei niedrig temperiertem Puffer; braucht eine Raumtemperatur des Aufstellorts (heute nicht im Modell) | mittel; **ändert den Rechenweg aller Referenzprojekte mit Puffer → neue Referenzbasis** | **C, Folgeauftrag** nach P2, nicht in P1 (E-P26) |
| V36 | **Zonenanteile mit Vorgabehilfe** 0,10/0,16/0,37/0,37 für die Kombi-Übernahme, wo keine Zonen gerechnet wurden | Beispielwert statt leerem Feld; normnah | klein | **B, P1** (E-P27) |

### 5.3 Entscheide (Fortsetzung E-P8–E-P25)

| Nr. | Frage | Empfehlung |
|---|---|---|
| E-P16 (neu gefasst) | **Hauptblatt** VDI 4645 (Abschnitt 8.8 Wärmespeicher) beschaffen und vor P2 gegenlesen? Das am 01.10.2026 abgelegte Blatt 1 ist das Qualifikationsblatt ohne Auslegungsregeln | ja — Hauptblatt, gültige Ausgabe 2018-03, Überarbeitungsstand beim VDI prüfen |
| E-P26 | Verlustmodell der Simulation umgebungsbezogen umstellen (V35) — als Folgeauftrag mit neuer Referenzbasis, nicht in P1? | ja, Folgeauftrag nach P2 |
| E-P27 | Standardaufteilung der Norm (0,10/0,16/0,37/0,37) als Vorgabehilfe für Kombi-Zonen (V36), EPOS-Schichtmodell (`Hoehe`, `Lambda_Eff`) unverändert? | ja |

### 5.4 Folgen für den Bauplan P1 (Ergänzung zu Runde 1 und 2, jeweils Abschnitt 8)

- **W2 Kern:** Ergebnis je Zone trägt `Bereitschaftsverlust_kWh_d` (Katalog oder K11-Vorgabe),
  `Bereitschaftsverlust_W_K` (Formel 3, 45 K) und `Betriebsverlust_kWh_a` (V34, mit
  ϑ_Puffer,mittel = (Vorlauf + Rücklauf)/2 des Temperaturpaars und einer Raumtemperatur-Vorgabe
  `Pufferauslegung.Raumtemperatur_C` = 15 °C im freien Paketteil, Herkunftsmarke „Vorgabe“).
  Über 2 000 l Warncode „Bereitschaftsverlust extrapoliert“.
- **W3 Controller:** Übernahme in den Kombipuffer mit Zonenanteilen aus V_H/(V_H + V_B), sonst
  Vorgabehilfe nach V36; `Schichten_Anzahl` ≥ 2 wie V24. Semantik von `Entnahme_*` am Modell prüfen
  (Runde 2, Tabelle 4.3), bevor Anteile geschrieben werden.
- **W4 Tests (Handrechnungen):** 2,5 kWh/d → 2,315 W/K; Klasse-C-Grenze 1 000 l → 148,7 W →
  3,569 kWh/d → 3,304 W/K; Betriebsfaktor (45 °C − 15 °C)/45 K = 0,667 → 2,5 kWh/d · 0,667 · 365 =
  608 kWh/a; 2 500 l → Warncode Extrapolation.
- **Keine Änderung** an `SimulationPufferspeicher.cs` in P1 (V35 ist Folgeauftrag; Referenzbasis
  R29 bleibt byte-gleich).


## 6 Status nach drei Runden

Die Statustabelle wird an einer Stelle geführt: Runde 2, Abschnitt 9 — dort steht der Stand nach
dieser Runde (Normdateien ausgewertet; offen bleibt das Hauptblatt VDI 4645).


## 7 Quellen dieser Runde (lokale Dateien unter `Quellen/Waermespeicher-Tool/`, nur zitiert)

- VDI-MT 4645 Blatt 1:2023-04, *Planung und Dimensionierung von Wärmepumpenanlagen — Qualifikation zur
  Planung, Errichtung und Inbetriebnahme* (zweisprachig; ersetzt VDI 4645 Blatt 1:2018-03).
- E DIN EN 15316-5:2024-07, *Energetische Bewertung von Gebäuden — Verfahren zur Berechnung der
  Energieanforderungen und Nutzungsgrade der Anlagen — Teil 5: Raumheizung und Speichersysteme für
  erwärmtes Trinkwasser (keine Kühlung), Modul M3-7, M8-7*; deutsche und englische Fassung
  prEN 15316-5:2024 (Entwurf, vorgesehen als Ersatz für DIN EN 15316-5:2017-09).
- DIN EN 15332:2020-01, *Heizkessel — Energetische Bewertung von Warmwasserspeichern*; deutsche
  Fassung EN 15332:2019.
- E DIN EN 15332/A1:2023-01, Änderung A1 zu DIN EN 15332:2020-01; deutsche und englische Fassung
  EN 15332:2019/prA1:2022 (Entwurf).
- Zum Vergleich herangezogen: `EPOS.Kern/Allgemein/Simulation/SimulationPufferspeicher.cs`
  (Verlustansatz `VerlustProStunde`, Schichtverluste), `EPOS.Kern/Allgemein/Update/ProjektPuffer.cs`
  (`WH_JE_LITER_KELVIN` = 1,16), `Quellen/Waermespeicher-Tool/Waermespeicher-Tool/wsp/storage_sim.py`.
