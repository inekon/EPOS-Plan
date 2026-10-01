# Recherche Pufferspeicher-Auslegung, dritte Runde — Normdateien vom 01.10.2026: VDI 4645 (Entwurf 2026-03) und VDI-MT 4645 Blatt 1, prEN 15316-5:2024, DIN EN 15332 (01.10.2026)

**Zweck.** Dritte Runde der Recherche zur Pufferspeicher-Auslegung, Fortsetzung von
[`2026-10-01_Recherche_Pufferspeicherauslegung.md`](2026-10-01_Recherche_Pufferspeicherauslegung.md)
(Runde 1: Heizungspuffer nach Erzeugern, Kriterien K1–K16, Vorschläge V1–V20, Entscheide E-P8–E-P17)
und [`2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md`](2026-10-01_Recherche_Pufferoptimierung_Erzeuger_Nutzungen.md)
(Runde 2: Speicherklassen, Zapfprofil-Kopplung, V21–V32, E-P18–E-P25). Anlass sind fünf Normdateien,
die der Anwender am 01.10.2026 unter `Quellen/Waermespeicher-Tool/` abgelegt hat (Sync-Commits
6b8806fe, 5dadb3e5 und 59af9382 — mit dem letzten kam das Hauptblatt VDI 4645; die Datei VDI 4655
liegt weiterhin dort). Dieses Papier sagt, was jede Datei ist, was sie für die Auslegung und für das
Puffermodell des Kerns hergibt, und schreibt Kriterientabelle, Vorschläge und Entscheide fort.

**Grenzen.** Die Normen sind lizenziert (Anwenderentscheid E-P17: sie bleiben für Konzeption und
Umsetzung im Repositorium). Sie werden hier mit Abschnitts- und Formelnummern **zitiert, nicht
abgeschrieben**; Zahlen stehen nur, wo sie für den Rechenweg gebraucht werden. Keine Produktdaten.
Alle Rechenbeispiele sind fiktiv.


## 1 Befund in Kürze

| Datei unter `Quellen/Waermespeicher-Tool/` | Was es ist | Ertrag für die Auslegung |
|---|---|---|
| **VDI 4645, Entwurf März 2026** (deutsch, 143 Seiten; Einsprüche bis 31.05.2026; ersetzt VDI 4645:2018-03) — *Heizungsanlagen mit elektrisch betriebenen Wärmepumpen in Ein- und Mehrfamilienhäusern — Planung, Errichtung, Betrieb* | **das Hauptblatt**: Abschnitt 7.8 Wärmespeicher (Aufgaben, Speichertypen, Prüfung „Puffer erforderlich?“, überschlägige Faustwerte), Abschnitt 8.8 Pufferspeicher und deren Dimensionierung (Gleichung 22 Mindestlaufzeit, Gleichung 23 Abschaltzeiten mit Tabellen 14 und 15), Anhang F Hydraulik, Anhang I Rechenbeispiel | **hoch**: belegt K1 (3 l/kW), K2 (20 l/kW Fixed-Speed, 3 l/kW leistungsgeregelt), K3 (Gleichung 22), gibt K4 einen besseren Rechenweg (Gleichung 23 mit Trägheits-Gutschrift) und stützt E-P12 (§ 14a wird für die Auslegung nicht berücksichtigt) — Abschnitt 2 |
| VDI-MT 4645 Blatt 1:2023-04 (zweisprachig, 21 Seiten) | **Qualifikationsblatt** („Mensch und Technik“): Schulungsinhalte, Qualifizierungsnachweis, Muster für Teilnahmebescheinigung; ersetzt VDI 4645 Blatt 1:2018-03 | **keiner** — keine Auslegungsregel, kein Richtwert; es bestätigt nur die Gliederung des Hauptblatts |
| E DIN EN 15316-5:2024-07 (prEN 15316-5:2024, deutsch/englisch, 98 Seiten; Entwurf, Ersatz für DIN EN 15316-5:2017-09) | Rechenverfahren für Speicher der Raumheizung und Trinkwassererwärmung: **Schichtenmodell** (Methode A, 1 bis 10 Schichten) und Einvolumen-Modell (Methode B), Bereitschaftsverlust als W/K, Standardwerte im Anhang B | hoch: Umrechnung kWh/d → W/K (Formel 3), Lade-/Abschaltlogik über Temperaturen, Lage der Ein- und Austritte je Schicht, Reihen-/Parallelverbund, Vergleichsmaßstab für das Puffermodell des Kerns (Abschnitt 3) |
| DIN EN 15332:2020-01 (EN 15332:2019, deutsch, 18 Seiten) | **Prüfnorm** für den Nennwärmeverlust Q_B [kWh/d] von Trinkwarmwasserspeichern bis 2 000 l; Grundlage der Ökodesign-Deklaration (Anhang ZA/ZB zu 814/2013 und 812/2013) | mittel: belegt die Prüfbedingung 65 °C bzw. ≥ 45 K über 20 °C Umgebung hinter dem Katalogwert `Bereitschaftsverluste`; nimmt primäre Heizpufferspeicher aus dem Anwendungsbereich aus (Abschnitt 4) |
| E DIN EN 15332/A1:2023-01 (EN 15332:2019/prA1:2022, deutsch/englisch, 28 Seiten; Entwurf) | Änderung der Prüfnorm: Begriffe, Fühlerlagen, Reglerhysterese, Dämmung der Anschlüsse, Messablauf | gering: Präzisierungen des Prüfverfahrens, keine neuen Zahlen für die Auslegung |


## 2 VDI 4645 — Hauptblatt (Entwurf März 2026) und Blatt 1

### 2.1 Blatt 1 ist das Qualifikationsblatt

VDI-MT 4645 Blatt 1:2023-04 enthält Anwendungsbereich, Qualifizierungswege, Schulungsinhalte
(Anhang A), Nachweise und Register, Qualitätsmerkmale von Schulungen und Muster (Anhänge B und C).
Keine Auslegungsregel. Die Richtwerte stehen im Hauptblatt, das der Anwender am Abend des
01.10.2026 nachgelegt hat (E-P16 damit erledigt).

### 2.2 Was das Hauptblatt zum Pufferspeicher sagt

- **Aufgaben** (7.8.1): Laufzeit der Wärmepumpe im Teillastfall verlängern (vor allem ungeregelte
  Luft/Wasser-Geräte), Schalthäufigkeit verringern, Abtauenergie vorhalten, hydraulische Entkopplung
  (Parallelschaltung), Energiemanagement bei mehreren Erzeugern, Eigenstrom/flexible Tarife/
  netzdienliche Regelung, gegebenenfalls Heizwasser für die Trinkwassererwärmung. Fußnote: bei
  Fixed-Speed-Geräten die Mindestlaufzeit des Herstellers, bei leistungsgeregelten die Mindestleistung
  beachten.
- **Speichertypen** (7.8.2): getrennte Speicher, Tank-im-Tank-Kombispeicher (für Wärmepumpen weniger
  geeignet, hohes Temperaturniveau), Heizwasserspeicher mit Frischwassersystem oder innenliegendem
  Wärmeübertrager; wichtig sind Schichtung, geringe Verluste, Nachheizregelung.
- **Prüfung „Puffer erforderlich?“** (7.8.3): für Sole/Wasser- und Wasser/Wasser-Geräte ohne
  Herstellerangabe genügt, wenn je kW maximaler Heizleistung **mindestens 3 l nicht absperrbares,
  durchströmbares Anlagenvolumen** vorhanden ist (Übergabeeinrichtungen gelten als absperrbar);
  daneben Mindestlaufzeit, Mindestvolumenstrom und Abtauenergie nach Hersteller.
- **Faustwerte** (7.8.4, bis 50 kW monovalent): **mindestens 20 l/kW** Leistung im Auslegungspunkt
  für Fixed-Speed-Geräte, **mindestens 3 l/kW** für leistungsgeregelte, abweichende Herstellerangaben
  gehen vor; eine detaillierte Auslegung nach 8.8 wird empfohlen.
- **Mindestlaufzeit** (8.8, Gleichung 22): V = Q̇_WP,Nenn · t / (ρ · c_W · Δϑ) mit der Leistung in
  der Anlaufphase (ersatzweise Nennleistung), der Mindestlaufzeit des Herstellers, c_W = 1,163 Wh/(kg·K)
  und Δϑ von der Rücklauftemperatur bis zur maximalen Vorlauftemperatur. **Dieses Volumen deckt die
  Abtauenergie mit; eine eigene Abtaurechnung ist nicht erforderlich.**
- **Abschaltzeiten/Lastmanagement** (8.8, Gleichung 23, bis 50 kW): V_SP,t = Q̇_Ausl · (t_WP,aus −
  t_aus,max) / (ρ · c_W · ((ϑ_VL,N + dT_SP) − (ϑ_R + dT_Wü,min))) − V_Hz. Darin: akzeptable
  Stillstandzeit **t_aus,max nach Tabelle 14** (Heizgrenze 15 °C: Radiatoren 1 h, Fußbodenheizung
  1,5 h; 12 °C: 2 h / 2,5 h; 10 °C: 3 h / 3,5 h — in Mietwohnungen mit Radiatoren gegebenenfalls
  kürzer), Überladung **dT_SP bis 5 K** über der Heizkurve (darüber Mischventil), kleinste nutzbare
  Übertemperatur der Übergabe **dT_Wü,min nach Tabelle 15** (Fußbodenheizung 5 K, Radiatoren 15 K,
  Konvektoren 20 K, Heizkörper mit Lüfter 15 K), Abzug des Wasserinhalts der Anlage V_Hz. Nur
  Abschaltungen sind so darstellbar, Teilleistung nur durch Modellrechnung. **Bivalent entfällt die
  Bemessung**, der zweite Erzeuger überbrückt.
- **§ 14a EnWG** (7.6): Leistungsminderungen des Netzbetreibers „werden für die Auslegung nicht
  berücksichtigt“, um keine zu hohe Wärmepumpenleistung zu errechnen — die Richtlinie stützt den
  Anwenderentscheid E-P12.
- **Hydraulik** (Anhang F): ohne Puffer nur mit leistungsgeregelten Geräten und ausreichender
  Speichermasse für die Abtauung (F1); der Puffer entkoppelt, sichert den Mindestvolumenstrom und
  hält Abtauenergie vor (F2); Trennspeicher und hydraulische Weiche nach dem Nennvolumenstrom
  auslegen (8.11.4), Trinkwasserspeicher-Wärmeübertrager mindestens 0,25 m²/kW.
- **Rechenbeispiel** (Anhang I3): 13,8 kW Anlaufleistung, 5 min Mindestlaufzeit, 30 K (30 → 60 °C)
  → 1,15 kWh → rund 33 l. Anhang I1: Trinkwasserspeicher V = Q_DPB / (c_W · (60 − 10 K)) mit
  **Zuschlag 15 % für Durchmischung** (f_TWE = 1,15); Heizwasserspeicher für ein Frischwassersystem
  mit 55/25 °C und demselben Zuschlag.
- Nicht enthalten: Vorgaben zu Startzahlen oder Schalthäufigkeit (das deckt sich mit Fraunhofer
  WP-QS), Regeln für Prozesswärme, Zahlen für Speicher über 50 kW Anlagenleistung.

### 2.3 Gegenlesen der Kriterien aus Runde 1

| Kriterium | VDI 4645 (Entwurf 2026) | Runde 1 / Mockup | Folge |
|---|---|---|---|
| K1 Vorprüfung Anlagenvolumen | ≥ 3 l/kW nicht absperrbar (7.8.3) | 3–5 l/kW (BWP, VdZ) | bestätigt; Vorgabe 3 l/kW, Herkunft „VDI 4645 7.8.3“ |
| K2 Abtauung 20 l/kW | 20 l/kW ist der Faustwert für **Fixed-Speed-Geräte** (Laufzeit, Takten, Abtauen zusammen); leistungsgeregelt 3 l/kW; Abtauvolumen im Mindestlaufzeit-Volumen enthalten | 20 l/kW als eigenes Abtaukriterium | K2 wird zum **Faustwert nach Gerätetyp** (V39); die Abtaurechnung bleibt als Gegenprobe, ist aber nicht bemessend, wenn K3 größer ist |
| K3 Mindestlaufzeit | Gleichung 22, c_W 1,163, Δϑ Rücklauf → max. Vorlauf | Tool: P_min · t_min / (c · ΔT · Hysterese) | bestätigt; EPOS rechnet mit 1,16 und den Schwellen des Puffers (E-P9/E-P10); Anlaufleistung = Nennleistung, wenn das Kennfeld nichts anderes hergibt |
| K4 Sperrzeit | Gleichung 23: Auslegungsheizlast × (Sperrdauer − akzeptable Stillstandzeit) ÷ nutzbare Spreizung bis zur Übergabe-Untergrenze − Anlagenvolumen; bivalent keine Bemessung | Tool: rollierendes Lastmittel × Sperrdauer ÷ (c · ΔT · Hysterese), kein Trägheitsabzug | **neuer Standardweg** (V37): EPOS kennt Übergabeart und Heizgrenze je Gebäude, kann Tabellen 14 und 15 belegen; der Tool-Weg bleibt als Expertenvariante „aus dem Lastgang“ |
| K14 Gebäudeträgheit | Tabelle 14 als Gutschrift (1–3,5 h) | V6: Gutschrift nur Fußboden/schwere Bauweise | Tabelle 14 ersetzt die Eigenschätzung |
| K12 Einbindung | parallel mit Puffer (F2), ohne Puffer nur leistungsgeregelt (F1) | parallel/Reihe | bestätigt; Warnhinweis „ohne Puffer nur leistungsgeregelt“ |
| K11 Bereitschaftsverlust | nur qualitativ („geringe Wärmeverluste“) | Ökodesign, EN 15316-5 | keine Änderung |
| Kombipuffer | Tank-im-Tank für Wärmepumpen weniger geeignet; Frischwassersystem 55/25 °C mit 15 % Zuschlag | Runde 2: 65/25 °C → ΔT 40 K | Vorgabe bleibt 65/25 °C (W 551-Reserve), **Zuschlag 15 %** kommt dazu (V38); Hinweis zur Tank-im-Tank-Bauart in die Warnliste |
| § 14a EnWG | für die Auslegung nicht berücksichtigt (7.6) | E-P12 entschieden: nicht relevant | bestätigt |


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
| K1 Vorprüfung | Vorgabe 3 l/kW nicht absperrbares Anlagenvolumen, Herkunft „VDI 4645 7.8.3“ |
| K2 Faustwert | 20 l/kW nur für Fixed-Speed-Geräte, 3 l/kW für leistungsgeregelte (VDI 4645 7.8.4); Abtauvolumen gilt als im Mindestlaufzeit-Volumen enthalten (8.8) — Abtaurechnung als Gegenprobe, nicht bemessend |
| K3 Mindestlaufzeit | Herkunft „VDI 4645 Gleichung 22“; Leistung in der Anlaufphase, ersatzweise Nennleistung |
| K4 Sperrzeit | Standardweg nach VDI 4645 Gleichung 23 mit Tabelle 14 (Stillstandzeit nach Heizgrenze und Übergabeart) und Tabelle 15 (Übergabe-Untergrenze), Überladung ≤ 5 K, Abzug des Anlagenvolumens; bivalent keine Bemessung; Tool-Weg aus dem Lastgang als Expertenvariante |
| K14 Gebäudeträgheit | Gutschrift nach Tabelle 14 statt eigener Schätzung |

### 5.2 Verbesserungsvorschläge (Fortsetzung V1–V32)

| Nr. | Vorschlag | Nutzen | Aufwand | Empfehlung |
|---|---|---|---|---|
| V33 | **W/K-Anzeige** des Bereitschaftsverlusts im Ergebnis (Formel 3, 45 K) mit Herkunftsmarke | Vergleichbarkeit mit Datenblättern, die W/K oder W angeben; Grundlage für V34 | klein (Rechenzeile im DTO) | **A, P1** |
| V34 | **Betriebsverlust** in der Nutzen-Aufwand-Zeile (V16): Q_B·ΔT_Betrieb/45 K·365 statt Q_B·365 | realistische kWh/a (Faktor 0,55–0,8) | klein | **A, P1** |
| V35 | **Verlustmodell der Simulation umgebungsbezogen** rechnen (H·(ϑ_Schicht − ϑ_Raum) statt SOC-anteilig) | physikalisch richtiger bei leerem und bei niedrig temperiertem Puffer; braucht eine Raumtemperatur des Aufstellorts (heute nicht im Modell) | mittel; **ändert den Rechenweg aller Referenzprojekte mit Puffer → neue Referenzbasis** | **C, Folgeauftrag** nach P2, nicht in P1 (E-P26) |
| V36 | **Zonenanteile mit Vorgabehilfe** 0,10/0,16/0,37/0,37 für die Kombi-Übernahme, wo keine Zonen gerechnet wurden | Beispielwert statt leerem Feld; normnah | klein | **B, P1** (E-P27) |
| V37 | **K4 nach VDI 4645 Gleichung 23** als Standardweg: Auslegungsheizlast (aus dem Lastgang-Maximum oder der Heizlast des Projekts) × (Sperrdauer − t_aus,max nach Tabelle 14 aus Heizgrenze und Übergabeart des Gebäudes) ÷ (c · ((ϑ_VL + dT_SP) − (ϑ_RL + dT_Wü,min nach Tabelle 15))) − Anlagenvolumen; bivalent entfällt K4; Tool-Weg (rollierendes Lastmittel, Puffer-ΔT, Hysterese) als Expertenvariante mit Gegenüberstellung | richtlinienkonform, deutlich kleinere Volumina (Beispiel Abschnitt 5.4: 2 400 l statt 16 630 l), nutzt Übergabe- und Heizgrenzdaten, die EPOS je Gebäude führt | mittel (Tabellen 14/15 als Vorgabeschlüssel, Ableitung Übergabeart → dT_Wü,min) | **A, P1** (E-P28) |
| V38 | **Durchmischungszuschlag 15 %** auf die Brauchwasser- und Frischwasserzone (f_TWE = 1,15) | VDI 4645 Anhang I1 und BWP TWW (15–20 %) stimmen überein; heute rechnet die Zapfprofil-Auslegung ihren eigenen Zuschlag (`Speicherauslegung.Zuschlag` 0,15) — für die Frischwasserzone (Runde 2, 4.1) fehlte er | klein (Vorgabeschlüssel `Pufferauslegung.Frischwasser.Zuschlag` = 0,15) | **A, P1** |
| V40 | **Zirkulationszuschlag** für die Brauchwasserzone (Vorgabe 35 % des Tagesbedarfs über 5 K), nur wenn die Zapfprofil-Auslegung ihn nicht trägt | energie-experten (30–40 %), VDI 4645 Anhang I (Q_Zirk) | klein, nach Prüfung am Modell | **B, P1** |
| V41 | **Plausibilitätsband nach Übergabeart** (Fußbodenheizung 10–20, Heizkörper 25–45 l je kW Heizlast nach dem VDI-Whitepaper; dazu 12–35 l je kW Wärmepumpenleistung nach DIN EN 15450) als Hinweiszeile mit Hinweiscodes über und unter dem Band (Abschnitt 7.1) | ordnet die Empfehlung ein, ohne zu sperren | klein | **A, P1** |
| V44 | **Bivalenz unterscheiden**: K4 entfällt nur bei freigegebenem Zweiterzeuger; Heizstab gilt als mitgesperrt (Abschnitt 7.1) | verhindert zu kleine Puffer bei monoenergetischen Anlagen | klein | **A, P1** |
| V45 | **Hinweiscode Abtauung + Trinkwasservorrang** unter 20 l/kW Heizlast (Abschnitt 7.1) | Komfortfall aus dem Whitepaper | klein | **B, P1** |
| V42 | **Kennzahl Starts je Heizperiode** mit Hinweisschwelle 3 000 neben Starts je Tag | Lebensdauer-Maß der Hersteller; Portalband 10–15/Tag als Beispielhilfe im Expertenfeld | klein | **B, P1** |
| V43 | **K9 Festbrennstoff zweiter Weg nach DIN EN 303-5**: V = 15 · Q_K · T_B · (1 − 0,3 · Q_H/Q_K,min), Vorgabe Abbrandperiode 4 h Scheitholz, Expertenfeld | normnäher als der Faustwert, wenn Abbrandperiode und Mindestleistung vorliegen | klein | **B, P1** |
| V39 | **Vorlage Wärmepumpe mit Schalter Fixed-Speed / leistungsgeregelt**: Faustwert 20 bzw. 3 l/kW (K2), Mindestlaufzeit bzw. Mindestleistung als führende Eingabe, Modulationsmodus (V4) nur bei leistungsgeregelt | folgt 7.8.1 Fußnote und 7.8.4; passt zum Kennfeld (`Mindestleistung` fehlt der Wärmepumpe heute → Eingabe mit Vorgabe 30 %) | klein | **A, P1** |

### 5.3 Entscheide (Fortsetzung E-P8–E-P25)

| Nr. | Frage | Empfehlung |
|---|---|---|
| E-P16 | Hauptblatt VDI 4645 beschaffen und gegenlesen? | **erledigt 01.10.2026**: Entwurf März 2026 liegt unter `Quellen/Waermespeicher-Tool/`, Gegenlesen in Abschnitt 2.3; Hinweis: Entwurf mit Einspruchsfrist bis 31.05.2026, die Weißdruck-Fassung kann abweichen — Herkunftsmarke „VDI 4645 E 2026-03“ |
| E-P12 | § 14a EnWG | **entschieden: nicht relevant** — VDI 4645 7.6 bestätigt: für die Auslegung nicht berücksichtigt |
| E-P28 | K4 Sperrzeit nach VDI 4645 Gleichung 23 als Standardweg (V37), Tool-Weg als Expertenvariante? | ja |
| E-P29 | VDI-Whitepaper „Thermische Speicher in Wärmepumpensystemen“ beschaffen und gegenlesen? | **erledigt 01.10.2026** (Abschnitt 7.1): V41 neu gefasst, V44, V45, V16 ergänzt |
| E-P30 | Vorlage „Wärmepumpe bivalent“ mit der Wahl „Zweiterzeuger frei in der Sperre“ / „Heizstab (gesperrt)“ (V44)? | ja |
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
- **W1 Schema (V37, V38, V39):** Vorgabeschlüssel `Pufferauslegung.Sperrzeit.Stillstand.<Heizgrenze>.<Uebergabe>_h`
  (Tabelle 14: sechs Werte), `Pufferauslegung.Sperrzeit.Uebertemperatur.<Uebergabe>_K` (Tabelle 15: vier
  Werte), `Pufferauslegung.Sperrzeit.Ueberladung_K` = 5, `Pufferauslegung.Frischwasser.Zuschlag` = 0,15,
  `Pufferauslegung.Vorlage.Waermepumpe.Faustwert_FixedSpeed_l_kW` = 20 und `…_Geregelt_l_kW` = 3;
  Spalte `WP_Geregelt` (0/1) in `Tab_PufferAuslegung`.
- **W2 Kern (V37):** K4 in zwei Wegen — Standard nach Gleichung 23 (Auslegungsheizlast = Maximum der
  Heizreihe oder Heizlast des Projekts; Übergabeart und Heizgrenze je Gebäude → t_aus,max und
  dT_Wü,min, bei mehreren Gebäuden der kleinste Wert; Anlagenvolumen aus K1), Experte aus dem Lastgang
  (Tool-Weg); bivalent mit Freigabe des Zweiterzeugers in der Sperre: K4 entfällt mit Hinweis. K2 als
  Faustwert nach Gerätetyp, Abtauung als Gegenprobe.
- **W4 Tests (Handrechnungen, Konstante 1,16):** Gleichung 22: 13,8 kW · 5 min, 30 K → 1,15 kWh →
  1 150/(1,16 · 30) = 33,0 l (Anhang I3 nennt rund 33 l mit 1,163); Gleichung 23 mit den Mockup-Zahlen:
  82 kW, Sperre 2 h, Radiatoren bei Heizgrenze 15 °C → t_aus,max 1 h, ϑ_VL 55 °C + 5 K, ϑ_RL 20 °C +
  15 K → Spreizung 25 K → 82 · 1 · 1000/(1,16 · 25) = 2 828 l, abzüglich 400 l Anlagenvolumen = 2 428 l
  (Tool-Weg: 16 630 l — beide Zahlen im Ergebnis gegenüberstellen); Faustwert 60 kW Fixed-Speed →
  1 200 l, geregelt → 180 l.
- **Keine Änderung** an `SimulationPufferspeicher.cs` in P1 (V35 ist Folgeauftrag; Referenzbasis
  R29 bleibt byte-gleich).


## 6 Status nach drei Runden

Die Statustabelle wird an einer Stelle geführt: Runde 2, Abschnitt 9 — dort steht der Stand nach
dieser Runde (alle fünf Normdateien ausgewertet, E-P16 erledigt).


## 7 Sekundärquelle energie-experten.org — Nachtrag auf Anwenderhinweis (01.10.2026)

Der Anwender hat die Seite „Warmwasser mit Wärmepumpe“ und das Portal energie-experten.org als Quelle
genannt. Das Portal ist ein Ratgeber (Sekundärquelle, Runde 1 nutzte es bereits für VDI 4645 und
Solarthermie); seine Zahlen tragen keine Vorgabewerte, sie dienen als Gegenprobe und Beispielhilfe.
Die Seiten sperren den direkten Abruf (HTTP 403) und wurden über einen Lesedienst gelesen; die
Aktualisierungsdaten stehen in Abschnitt 8. Keine Produktdaten übernommen.

| Seite | Aussagen (Zahlen mit Kontext) | Deckung mit Runde 1–3 | Folge |
|---|---|---|---|
| Warmwasser mit Wärmepumpe | 1,45 kWh je Person und Tag ≈ 25 l bei 60 °C; Faustformel V_SP,min = 2 · n · 25 l · (60 − 10)/(t_soll − 10) (Beispiel 3 Personen, 50 °C → 187,5 l → 200 l); Zuschlag Durchmischung 15–20 %; Zirkulationsverluste 30–40 % des Tagesbedarfs, als Volumen über 60/55 °C umgerechnet; Wärmeübertrager 0,25 m²/kW; Ladestation 2–5 K Spreizung; Kombispeicher für Wärmepumpen energetisch nachteilig; JAZ Trinkwasser 2–2,5 (Fraunhofer: 3,1 Erdreich, 2,4 Luft), 19 % Anteil Trinkwasser im Bestand; Zapfprofile EN 15450 Anhang E (Einzelperson 36 l, Familie 100 l, Familie mit Baden 200 l bei 60 °C; Q_DP 2,1 / 5,845 / 11,655 kWh, Spitzenbedarf 0,945 / 2,24 / 4,445 kWh); 55–60 °C, W 551, DIN 4708-2 (N-Zahl) | Zuschlag 15 % = VDI 4645 Anhang I (V38); 0,25 m²/kW = VDI 4645 Anhang F; Zapfprofil-Werte = VDI 4645 Anhang J3 (5,845 kWh); Zirkulationsvolumen fehlt in Runde 2 | **V40**: Zirkulationszuschlag für die Brauchwasserzone bei Topologie Speicher/Frischwasser, wenn das Projekt Zirkulation führt (Vorgabe 35 % des Tagesbedarfs, umgerechnet über 5 K) — nur, wenn die Zapfprofil-Auslegung ihn nicht schon trägt (am Modell prüfen) |
| Pufferspeicher für Wärmepumpen | DIN EN 15450: 12–35 l/kW maximaler Wärmepumpenleistung; VDI 4645 8.8.4: 20 l/kW; 3 l/kW nicht absperrbar → kein Puffer; Sperrzeit: V = P · t / (1,163 · ΔT), Faustwert 30–40 l/kW je Stunde Sperre; ohne Speichermassen 1–2 K Abkühlung in der Sperre; Einbindung Reihe (Vor- oder Rücklauf), parallel als Pendelpuffer; Fraunhofer: Puffer senkt Schalthäufigkeit um 20–60 % | alles in K1–K4 und K12 enthalten; das Band 12–35 l/kW der DIN EN 15450 ist neu | **V41**: Plausibilitätsband 12–35 l/kW (DIN EN 15450) als Hinweiszeile im Ergebnis der Heizzone — Empfehlung außerhalb des Bandes bekommt einen Hinweiscode, keine Sperre |
| News: VDI-Whitepaper (Meldung) | Zusammenfassung der Studie, siehe 7.1 — das Whitepaper selbst liegt seit dem Abend des 01.10.2026 vor | — | E-P29 erledigt |
| Wärmepumpe taktet | Hersteller 6–12 Starts/h; empfohlen 10–15 Starts/Tag, 30–60 min je Zyklus; 2 000–3 000 Starts je Heizperiode; Mindestlaufzeit 6–20 min; Taktpunkt (Heizlast = Mindestleistung) möglichst ≥ +5 °C; VDI 4645 ohne Vorgaben zu Mindestlaufzeit und Schalthäufigkeit; Fraunhofer: Überdimensionierung in Feldmessungen nicht als entscheidend nachweisbar | Startziel D2 (Runde 1: 6 Starts/Tag, Warnschwelle 15/Tag) liegt unter dem Portalband 10–15/Tag; Mindestlaufzeit 10 min (P4) liegt im Band 6–20 min | **V42**: Ergebniskennzahl „Starts je Heizperiode“ neben Starts je Tag mit Hinweisschwelle 3 000; Startziel-Vorgabe bleibt 6/Tag (Fraunhofer-Median), das Portalband 10–15/Tag als Beispielhilfe im Experten-Feld |
| EVU-Sperrzeit (18.07.2026) | seit 2024: Dimmung auf 4,2 kW bis 2 h, „in der Regel kein Einfluss auf die Dimensionierung“; vor 2024: 3 × 2 h, Sperrzeitfaktor in der Leistung | bestätigt E-P12 und VDI 4645 7.6 | keine |
| Trinkwasserspeicher | VDI 6023: 30 l je Person und Tag; Speicher 1,5- bis 2-fach des Tagesbedarfs, mindestens etwa 100 l; Tabelle 2 Personen 200 l … 9–10 Personen 900–1 000 l; Legionellen: nicht überdimensionieren, wöchentlich über 60 °C, VDI/DVGW 6023 | Faustwerte; die Zapfprofil-Auslegung rechnet genauer (Runde 2) | Beispielhilfe im Schritt „Lastgang“ für die Brauchwasserzone; keine Vorgabe |
| Pufferspeicher-Technik (Heizungstechnik) | Festbrennstoff nach DIN EN 303-5: V_Pu = 15 · Q_K · T_B · (1 − 0,3 · Q_H/Q_K,min) mit Nennleistung Q_K, Abbrandperiode T_B, Heizlast Q_H, kleinster Kesselleistung Q_K,min; Richtwert 35–75 l/kW für Holzheizungen; Anschlüsse: Vorlauf oben, Rücklauf unten; Fühler oben/mittig/unten; Ökodesign Klasse C seit 26.09.2017 bis 2 000 l | K9 kennt nur 1. BImSchV und BEG (55/30/20 l/kW); die EN-303-5-Formel braucht die Abbrandperiode | **V43**: K9 um die DIN-EN-303-5-Formel als zweiten Weg ergänzen, wenn Abbrandperiode und Mindestleistung des Kessels vorliegen (Expertenfeld; Vorgabe T_B 4 h Scheitholz); sonst bleibt der Faustwert |
| News: Feldtest Schichtenspeicher (12.02.2025) | Hochschule Düsseldorf, Zentrum ZIES: Austausch fehlerhaft eingebundener Speicher gegen gut schichtende Technik senkte den Strombedarf einer Luft/Wasser-Wärmepumpe um rund 30 %, JAZ +11 %, Schichtungseffizienz 85 % (Niedrigenergiehaus 1994, 250 m², 750 l + 2 × 120 l → 900 l) | zeigt, dass Hydraulik und Schichtung mehr wiegen als das Volumen; Produktnamen nicht übernommen | Begründungstext für den Hinweis „Schichtung und Einbindung vor Volumen“ im Ergebnisblatt; keine Rechengröße |


### 7.1 Das VDI-Whitepaper selbst (gelesen am 01.10.2026)

*Thermische Speicher in Wärmepumpensystemen: zu klein, zu groß oder überhaupt notwendig?* —
Wüllhorst, Vering, Müller, Haas (RWTH Aachen, Lehrstuhl für Gebäude- und Raumklimatechnik),
VDI Wissensforum GmbH, 9 Seiten, Datei `WP_Thermische_Speicher_in_Waermepumpensystemen1_.pdf`
unter `Quellen/Waermespeicher-Tool/`. Es ist eine **Simulationsstudie**, keine Richtlinie; es zitiert
VDI 4645:2018 und DIN EN 15450:2007.

- **Methode:** Modelica (BESMod, AixLib), Jahressimulation, Standort Potsdam; Puffer als direkt
  beladener Schichtspeicher **parallel**, Beladung und Entnahme oben; Trinkwasserspeicher 125 l
  parallel mit Vorrang; Regelung über Speichertemperatur-Hysterese **±5 K** mit PI-Regler
  (Trinkwasser ±2,5 K); 30 Volumina von **5 bis 150 l je kW Heizlast** (36,5 bis 1 935 l); zwei
  Wärmepumpengrößen (Bivalenztemperatur −12 °C und 0 °C, Zusatz durch Heizstab) und zwei
  TABULA-Gebäude (Neubau 2015 mit Fußbodenheizung, Altbau 1980 mit Heizkörpern); Abtauung
  zeitgesteuert alle 30 bis 240 min; EVU-Sperre als zwei feste Fenster von 2 h ohne vorausschauendes
  Laden, Heizstab nach 30 min unter der unteren Hysteresegrenze; PV-Überhitzung des Puffers um 15 K.
  Bezugsgröße ist die **Heizlast**, mit Umrechnungsfaktoren auf die Wärmepumpenleistung der DIN
  (maximale Leistung) und der VDI (Nennleistung), weil Datenblätter die Maximalleistung oft nicht
  nennen.
- **Schalthäufigkeit (Abschnitt 3):** Schaltvorgänge je Tag sinken mit dem Volumen und
  konvergieren; die JAZ sinkt zugleich (Speicherverluste, häufigerer Heizstab). Monovalent mit
  Heizkörpern lohnt Volumen bis **45 l/kW Heizlast**; Kompromiss aus Schaltzahl und JAZ
  **25–45 l/kW Heizlast**; kleinere Wärmepumpen und trägere Übergabe: **10–20 l/kW** genügen.
- **Bereifung (Abschnitt 4):** mindestens **20 l/kW**, sonst Raumtemperatur unter 18 °C, wenn die
  Abtauung ungünstig liegt — der kritische Fall ist **Abtauung mit anschließender Beladung des
  Trinkwasserspeichers** (Vorrang).
- **EVU-Sperre (Abschnitt 5):** zwei Sperren von 2 h je Tag; Neubau mit Fußbodenheizung ohne
  Diskomfort bei jeder Größe; Altbau: monovalent **25 l/kW Heizlast**, bivalent (Heizstab)
  **40–50 l/kW Heizlast** — der Heizstab ist in der Sperre ebenfalls gesperrt, die kleinere Wärmepumpe
  braucht mehr Vorrat.
- **PV (Abschnitt 6):** kostenoptimal ist bei heutigen Speicherpreisen stets die **minimale**
  Speichergröße; regelbasierte SG-Ready-Steuerung nutzt PV-Strom nicht ganzjährig effizient.
- **Fazit (Abschnitt 7):** Richtlinienwerte passen für Bestand mit Heizkörpern ohne Sperren; bei
  Fußbodenheizung und bei Sperrbetrachtung sind sie tendenziell zu groß und senken die JAZ; größere
  Speicher selten wirtschaftlich; die Schalthäufigkeit hat ohne bekannten Lebensdauer-Zusammenhang
  keinen Einfluss auf die Wirtschaftlichkeit; Richtlinien sollten das Volumen spezifischer fassen.

**Folgen für die Auslegung:**

| Nr. | Folge |
|---|---|
| V41 (neu gefasst) | **Plausibilitätsband nach Übergabeart in l je kW Heizlast:** Fußbodenheizung 10–20, Heizkörper 25–45 (Whitepaper), daneben das Herstellerband 12–35 l je kW Wärmepumpenleistung (DIN EN 15450); Hinweiscode über dem oberen Bandwert „JAZ-Verlust wahrscheinlich“, unter dem unteren „Schaltzahl und Abtaureserve prüfen“ |
| V44 | **Bivalenz unterscheiden:** nur ein in der Sperre **freigegebener Zweiterzeuger** lässt K4 entfallen; monoenergetisch (Heizstab) ist mitgesperrt (VDI 4645 7.12, Whitepaper) und braucht nach dem Whitepaper **mehr** Vorrat als monovalent — Vorlage „Wärmepumpe bivalent“ bekommt die Wahl „Zweiterzeuger frei in der Sperre“ / „Heizstab (gesperrt)“ |
| V45 | **Warnhinweis Abtauung + Trinkwasservorrang:** bei Kombi oder Trinkwasservorrang die Abtau-Gegenprobe (K2, 20 l/kW) nicht unter die Mindestreserve fallen lassen; Hinweiscode, wenn die Heizzone kleiner als 20 l/kW Heizlast ausfällt |
| V16 (Ergänzung) | Nutzen-Aufwand-Zeile sagt ausdrücklich: Mehrvolumen senkt Starts, erhöht Bereitschaftsverlust und senkt die JAZ; das Taktziel D2 ist ein Komfort- und Verschleißkriterium, kein Wirtschaftlichkeitsnachweis (Whitepaper, Abschnitt 7) |
| Modellbezug | Die Studie rechnet mit Hysterese ±5 K am Puffer und Parallelverschaltung mit Entnahme oben — das entspricht dem EPOS-Puffer mit Schwellen und Bereitschaftszone oben; Bezugsgröße Heizlast = Maximum der Heizreihe oder Heizlast des Projekts (wie V37) |

**Zusammenfassung zu energie-experten.org:** Das Portal liefert keine neue Rechenregel, aber drei brauchbare Ergänzungen —
das Plausibilitätsband 12–35 l/kW (DIN EN 15450), die Zirkulations- und Starts-Kennzahlen (V40, V42)
und die DIN-EN-303-5-Formel für Festbrennstoff (V43) — sowie den Hinweis auf das VDI-Whitepaper
(E-P29), das die Übergabeart als entscheidende Randbedingung benennt und damit den Standardweg V37
stützt. Alle übrigen Zahlen decken sich mit Runde 1 bis 3.


## 8 Quellen dieser Runde

Lokale Dateien unter `Quellen/Waermespeicher-Tool/` (nur zitiert):

- VDI 4645, Entwurf März 2026, *Heizungsanlagen mit elektrisch betriebenen Wärmepumpen in Ein- und
  Mehrfamilienhäusern — Planung, Errichtung, Betrieb* (deutsch; Einsprüche bis 31.05.2026; ersetzt
  VDI 4645:2018-03). Zitiert: 7.6, 7.8.1–7.8.4, 8.8 mit Gleichungen 22 und 23, Tabellen 14 und 15,
  8.11.4, Anhang F, Anhang I.
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

Sekundärquelle energie-experten.org (Ratgeberportal, abgerufen 01.10.2026 über Lesedienst; Aktualisierungsdatum, soweit angegeben):
- Warmwasser mit Wärmepumpe — https://www.energie-experten.org/heizung/waermepumpe/waermepumpenheizung/warmwasser
- Pufferspeicher für Wärmepumpen — https://www.energie-experten.org/heizung/waermepumpe/waermepumpenheizung/pufferspeicher
- VDI-Whitepaper: Wärmepumpen-Speicher tendenziell zu groß — https://www.energie-experten.org/news/vdi-whitepaper-waermepumpen-speicher-tendenziell-zu-gross
- Wärmepumpe taktet — https://www.energie-experten.org/heizung/waermepumpe/planung/taktung
- EVU Sperrzeit (18.07.2026) — https://www.energie-experten.org/heizung/waermepumpe/leistung/sperrzeiten
- Trinkwasserspeicher — https://www.energie-experten.org/heizung/heizungstechnik/warmwasserspeicher/trinkwasserspeicher
- Pufferspeicher-Technik, Auslegung & Kosten — https://www.energie-experten.org/heizung/heizungstechnik/pufferspeicher
- Wärmepumpen-Pufferspeicher im Test (12.02.2025) — https://www.energie-experten.org/news/waermepumpen-pufferspeicher-im-test-der-beste-schichtenspeicher-spart-30-strom

Studie (lokale Datei unter `Quellen/Waermespeicher-Tool/`, nur zitiert):
- Wüllhorst, Vering, Müller, Haas: *Thermische Speicher in Wärmepumpensystemen: zu klein, zu groß oder überhaupt notwendig?*, VDI Wissensforum GmbH, 9 Seiten (Whitepaper; Simulationsstudie der RWTH Aachen).

Zum Vergleich herangezogen:
- `EPOS.Kern/Allgemein/Simulation/SimulationPufferspeicher.cs`
  (Verlustansatz `VerlustProStunde`, Schichtverluste), `EPOS.Kern/Allgemein/Update/ProjektPuffer.cs`
  (`WH_JE_LITER_KELVIN` = 1,16), `Quellen/Waermespeicher-Tool/Waermespeicher-Tool/wsp/storage_sim.py`.
