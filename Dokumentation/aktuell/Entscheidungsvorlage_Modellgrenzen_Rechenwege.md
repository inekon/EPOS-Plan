# Entscheidungsvorlage: Modellgrenzen der Rechenwege — Vorschlag und Empfehlung

Stand 02.10.2026. Anlass: Der Anwender hat am 29.09.2026 die Abschnitte „Grenzen und Annahmen“
der Rechenweg-Hilfeseiten (`EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki`) gelesen und zu jedem
Punkt gefragt: „Kann das Modell geändert werden? Wie sinnvoll? Gebe Vorschlag und Empfehlung.“

Dieses Papier beantwortet jede Frage einzeln. Je Punkt:

- **Stand heute** — was der Kern rechnet, mit Beleg `Datei:Zeile` (nachgelesen am 02.10.2026 auf
  `ios_migration_september`);
- **Frage** — was der Anwender wissen will;
- **Vorschlag** — fachlich konkret: Formel oder Regel, Daten, Dialogfeld, Schema;
- **Empfehlung** — ja, nein oder später, begründet nach Nutzen für die Planungsaussage,
  Datenlage beim Anwender, Aufwand (S bis 2 PT, M bis 8 PT, L darüber) und Wirkung auf die
  Referenzbasis und die Einfrierregeln der `CLAUDE.md`;
- **Entscheidung des Anwenders: ☐** — zum Ankreuzen.

Was schon gebaut ist, steht als **erledigt** mit Statuszeile aus
[`Status_iOS_Migration.md`](Status_iOS_Migration.md) und wird nicht neu vorgeschlagen.

**Leitregel für alle Vorschläge:** Jedes neue Feld bekommt eine Vorgabe, die das heutige
Ergebnis Zeichen für Zeichen erhält (leer oder 0 = Rechnung wie heute). Dann bleibt die Basis
unberührt, bis ein Referenzprojekt das Feld setzt. Wo das nicht geht, steht „Basis neu“.

Am Ende stehen die Übersichtstafel und der Wellenvorschlag.

---

## 1. Solarthermie

Quelle des Rechenwegs: `EPOS.Kern/Allgemein/Simulation/SimulationSolarthermie.cs`. Referenzprojekt
1049 rechnet ein Kollektorfeld; jede Änderung am Ertrag friert dessen Basis neu ein
(Einfrierregel „gesäte Solardaten des Referenzprojekts 1049“).

### ST1 Solarkreispumpe und Hilfsenergie — umgesetzt

- **Stand heute:** Der Lauf rechnet keinen Pumpenstrom. Das Potenzial ist
  `leistungProQm · f.Flaeche · leitungsverluste / 1000` (`SimulationSolarthermie.cs:283`); ein
  Stromanteil kommt nicht vor. Die Spalte `Tab_Energieanlagen.Hilfsenergie_Anteil` gibt es für
  jede Anlage, sie wird aber nur für Brennstofferzeuger zur Menge
  (`EPOS.Kern/Allgemein/Wirtschaftlichkeit/HilfsstromRechner.cs:11-18`: Hilfsstrom = Anteil ×
  Brennstoff). Als Kostengröße lässt sich Hilfsenergie in der Wirtschaftlichkeit über die
  Betriebskostenposition „Hilfsenergie“ (VDI 2067) ansetzen — nicht als Strommenge im Lauf.
- **Frage:** Ist Hilfsenergie enthalten?
- **Vorschlag:** Pumpenstrom als Stundenreihe des Felds:
  `E_P(h) = P_P · 1 h`, wenn das Feld in Stunde h Wärme abgibt (Direktdeckung oder Ladung > 0),
  sonst 0. `P_P` [W] an der Anlagenzeile des Kollektorfelds (Feld „Leistung Solarkreispumpe“,
  Vorgabe leer = 0). Hilfsweise ohne gepflegte Leistung: der vorhandene `Hilfsenergie_Anteil`
  [%] bezogen auf den **genutzten** Solarertrag. Die Reihe geht in den Strombedarf
  (Restbedarf) und als Ausweis in den Reiter „Solarthermie“; Bezug VDI 6002 Blatt 1 und
  EN 15316-4-3 (Hilfsenergie des Solarkreises).
- **Empfehlung: ja**, Aufwand S. Der Posten ist klein (meist wenige Prozent des Ertrags), gehört
  aber in eine ehrliche Strom- und CO₂-Bilanz und ist mit einem Feld erledigt. Vorgabe 0 → Basis
  unberührt.
- **Umgesetzt (Welle M2):** Feld `Tab_Energieanlagen.Pumpenleistung_W` am Kollektorfeld (leer =
  kein Pumpenstrom); die Reihe geht in den Rest-Strombedarf und in den Reiter „Solarthermie“.
  Ohne gepflegte Leistung gilt der `Hilfsenergie_Anteil` des Felds bezogen auf die
  genutzte Wärme (Deckung plus Ladung). [Konzept Simulationsablauf, Abschnitt 16](Konzept_Simulationsablauf_EPOS-Plan.md).
- **Entscheidung des Anwenders: ☑ (entschieden, umgesetzt)**

### ST2 Feste Arbeitstemperatur 50 °C — umgesetzt

- **Stand heute:** `double tStorage = 50; // Annahme Speichertemperatur`
  (`SimulationSolarthermie.cs:246`), eingesetzt in den Kollektorwirkungsgrad
  `η = η₀·IAM − a₁·ΔT/G − a₂·ΔT²/G` mit `ΔT = tStorage − tAmb` (`:372-373`). Das Potenzial wird
  **vor** der Stundenschleife für das ganze Jahr gerechnet (`Kollektorfelder_Lesen`, `:216-283`) —
  es kann deshalb heute gar nicht vom Speicherzustand abhängen.
- **Frage:** Nicht korrekt; auf die (maximale) Speichertemperatur setzen?
- **Vorschlag:** Nicht die **maximale**, sondern die **Eintrittstemperatur am Kollektor** führt:
  Der Kollektor sieht den kalten Teil des Speichers. Regel je Stunde:
  `ϑ_ein = ϑ_unten(h) + ΔT_WT`, `ϑ_m = ϑ_ein + ΔT_Koll/2`, `ΔT = ϑ_m − ϑ_a`.
  `ϑ_unten` ist die Temperatur der untersten Zone des Senkenpuffers (das Schichtmodell führt sie
  stündlich, `SimulationPufferspeicher.cs:1180-1215`); bei einer Zone
  `ϑ = ϑ_RL + SOC/Q_max · (ϑ_VL − ϑ_RL)`; ohne Puffer (Direktdeckung) der Heizkreisrücklauf
  der Anlagenkopplung oder das Temperaturpaar der Senke. `ΔT_WT` siehe ST4, `ΔT_Koll` Vorgabe
  10 K. Die maximale Speichertemperatur wäre zu vorsichtig (Ertrag zu klein); 50 °C fest ist für
  Vorwärmstufen zu vorsichtig und für Heizungsunterstützung mit hohem Rücklauf zu optimistisch.
  Normbezug: EN ISO 9806 (Kennlinie auf die mittlere Fluidtemperatur), EN 15316-4-3.
  Umbau: Die Wirkungsgradformel wandert vom Jahresvorlauf in die Stundenschleife (Phase der
  Solarthermie), das Potenzial wird je Stunde nach dem Speicherzustand der Vorstunde gebildet.
- **Empfehlung: ja**, Aufwand M. Größter fachlicher Hebel der Solarthermie: Vorwärmanlagen
  gewinnen, Anlagen an hohem Rücklauf verlieren Ertrag — beides heute unsichtbar. Basis neu für
  1049; als Option mit Vorgabe „fest 50 °C“ einführbar, dann erst mit Umstellen von 1049 neu.
- **Umgesetzt (Welle M2):** Feld `Arbeitstemperatur_Weg` (`fest`/`speicher`, leer = fest) am
  Kollektorfeld; das Potenzial wird je Stunde mit dem Speicherstand vom Stundenbeginn gebildet.
  Ohne Puffer gilt der Heizkreisrücklauf der Anlagenkopplung, sonst 50 °C mit Hinweis — das
  Temperaturpaar der Senke wird nicht herangezogen. Referenzprojekt 1049 rechnet mit `speicher`,
  Basis `2026-10-02_R32_Solarthermie`.
- **Entscheidung des Anwenders: ☑ (entschieden, umgesetzt)**

### ST3 Leitungsverluste pauschal 8 % — Stufe 1 umgesetzt

- **Stand heute:** `double leitungsverluste = 0.92;` (`SimulationSolarthermie.cs:247`),
  Faktor auf das Potenzial (`:283`). Ein Feld gibt es nicht; die 8 % sind ein Erfahrungswert
  aus dem Vorläufer und nirgends hergeleitet.
- **Frage:** Warum nicht einstellbar, was ist sinnvoll?
- **Vorschlag:** Stufe 1: Feld „Verluste Solarkreis [%]“ am Kollektorfeld, Vorgabe 8. Stufe 2
  (mit ST2): physikalisch `Q_L(h) = U_L · L · (ϑ_m − ϑ_Umg)` in Betriebsstunden mit
  Leitungslänge `L` [m] und längenbezogenem Verlust `U_L` [W/(m·K)] (Vorgabe nach
  EN 15316-4-3; Größenordnung 0,2–0,3 W/(m·K) gedämmt) — kurze Leitungen auf dem Flachdach eines
  Wohnhauses und lange Freiflächenleitungen unterscheiden sich um ein Vielfaches.
- **Empfehlung: Stufe 1 ja** (S, Vorgabe 8 → Basis unberührt); **Stufe 2 später**, zusammen mit
  ST2, weil sie dieselbe mittlere Temperatur braucht.
- **Umgesetzt (Welle M2), Stufe 1:** Feld `Solarkreisverluste_Prozent` (0–50 %, leer = 8 %,
  bitgleich 0,92). Stufe 2 bleibt offen.
- **Entscheidung des Anwenders: ☑ Stufe 1 (entschieden, umgesetzt); Stufe 2 ☐**

### ST4 Kein Wärmeübertrager zum Speicher — umgesetzt

- **Stand heute:** Die Übertragung ist verlustfrei bis auf den Faktor 0,92
  (`SimulationSolarthermie.cs:247`).
- **Frage:** (aus der Hilfeseite) Ist das sinnvoll?
- **Vorschlag:** Kein eigenes Übertragermodell, sondern die Grädigkeit `ΔT_WT` [K] als Feld am
  Kollektorfeld (Vorgabe 5 K bei externem Plattenübertrager, 10 K bei innenliegendem
  Glattrohr) — sie hebt nach ST2 die Kollektortemperatur und senkt so den Ertrag. Mehr braucht
  eine Energiebilanzrechnung nicht.
- **Empfehlung: ja, als Teil von ST2**, kein eigener Aufwand. Ohne ST2 wirkungslos.
- **Umgesetzt (Welle M2):** Felder `Uebertrager_Graedigkeit_K` (leer = 5 K) und
  `Kollektor_Spreizung_K` (leer = 10 K), wirksam nur mit `speicher`.
- **Entscheidung des Anwenders: ☑ (entschieden, umgesetzt)**

### ST5 „K_dfu/K_diff rechnet nicht mit“ — umgesetzt

- **Stand heute:** Die Einfallswinkelkorrektur (IAM) wird aus `K_dir50` gebildet
  (`b₀ = (1 − K_dir50)/(1/cos 50° − 1)`, `SimulationSolarthermie.cs:360-365`) und auf die
  **gesamte** geneigte Strahlung angewandt (`η₀_eff = η₀ · IAM`, `:369`), also auch auf Diffus-
  und Bodenreflexanteil. `K_dfu` steht im Katalog und in der Projektkopie (`Kdfu` in
  `EPOS.Kern/Controller/SolarkollektorenCtrl.cs:160-163`), wird aber nirgends gelesen.
- **Frage:** Was bedeutet das?
- **Erläuterung:** Ein Kollektor nimmt Strahlung, die schräg einfällt, schlechter auf als
  senkrechte (Reflexion an der Abdeckung). Die Prüfung nach EN ISO 9806 liefert dafür zwei
  Korrekturfaktoren: `K_b(θ)` für die **Direktstrahlung** abhängig vom Einfallswinkel θ
  (Katalogwert `K_dir50` = Wert bei 50°) und `K_d` für die **Diffusstrahlung**, die aus allen
  Richtungen des Himmels kommt und deshalb einen festen Wert hat (Katalogwert `K_dfu`, typisch
  0,85–0,95). Richtig ist `G_eff = K_b(θ)·G_b + K_d·(G_d + G_r)`. Heute erhält der Diffusanteil
  den Faktor der Direktstrahlung: bei hoher Sonne (IAM ≈ 1) zu viel, bei tiefer Sonne (IAM klein)
  zu wenig.
- **Vorschlag:** Die Transposition (`SolarCalculator.CalculateHourly`, `:266-272`) liefert die drei
  Anteile getrennt; `CalculateThermalPower` bekommt `G_b`, `G_d + G_r` und `K_dfu` und rechnet
  `η₀·(K_b·G_b + K_d·G_dr) − a₁·ΔT − a₂·ΔT²` je m². Fehlt `K_dfu` (0 oder leer), gilt
  `K_d = K_b(θ)` wie heute.
- **Empfehlung: ja**, Aufwand S. Die Daten liegen schon im Katalog, der Fehler ist systematisch
  und normwidrig. Basis neu für 1049 (sofern dessen Kollektor `K_dfu` führt).
- **Umgesetzt (Welle M2):** `G_b = DNI · cos θ`, `G_dr = G_t − G_b` im Kollektorweg
  (`Solarkreis.LeistungJeQm`); ohne `K_dfu` der alte Weg, bitgleich. Der Kollektor von 1049 führt
  `K_dfu = 0` — keine Basiswirkung.
- **Entscheidung des Anwenders: ☑ (entschieden, umgesetzt)**

### ST6 Modulfläche nur Anzeige, Aperturfläche rechnet — umgesetzt

- **Stand heute:** `double nFlaeche = ctrlsol.m_Aperturfläche;` (`SimulationSolarthermie.cs:232`),
  Gesamtfläche = Apertur × Anzahl. (Der Kommentar an `:159` sagt „Modulfläche · Anzahl“ und ist
  veraltet.)
- **Frage:** Ist das korrekt?
- **Erläuterung und Vorschlag:** Richtig ist **die Fläche, auf die die Kennwerte η₀, a₁, a₂
  bezogen sind**. Ältere Datenblätter (EN 12975) beziehen auf die Apertur, Prüfberichte nach
  EN ISO 9806:2017 meist auf die Bruttofläche. Passen Fläche und Kennwerte nicht zusammen, liegt
  der Ertrag um das Verhältnis Apertur/Brutto (typisch 0,9) daneben. Vorschlag: Katalogspalte
  „Bezugsfläche der Kennwerte“ (Apertur/Brutto, Vorgabe Apertur), der Lauf nimmt die passende
  Fläche; der Import nach VDI 3805 setzt sie, wo die Quelle sie nennt.
- **Empfehlung: ja**, Aufwand S. Vorgabe Apertur → Basis unberührt. Vorher prüfen, worauf die
  Kennwerte der ausgelieferten Kollektorsätze bezogen sind (siehe „Fragliches“).
- **Umgesetzt (Welle M2):** Katalogspalte `Bezugsflaeche` (`apertur`/`brutto`, Vorgabe apertur)
  an `Tab_Solarkollektoren(_STAMM)`, Auswahl im Katalogdialog; `brutto` rechnet mit der
  Modulfläche, ohne sie mit der Apertur und einer Warnung. Der VDI-3805-Import setzt `brutto`
  nur, wenn die Bezugsfläche der Datei der Bruttofläche gleicht und von der Apertur abweicht.
- **Entscheidung des Anwenders: ☑ (entschieden, umgesetzt)**

### ST7 Vor- und Rücklauf des Kollektorfelds — erledigt

- **Stand heute:** Es gibt kein solches Feld mehr. Vor- und Rücklauf wurden aus Katalog und
  Projektkopie entfernt (`SolarkollektorenCtrl.cs:158-159`,
  `SolarkollektorenStammCtrl.cs:207-208`; Statuszeile **#552**, Schemaschritt 150). Der Dialog
  fragt keines ab (`EPOS.UI/Dialoge/Solarthermie/SolarkollektorenDialog.razor:11`).
- **Frage:** Feld entfernen.
- **Empfehlung: erledigt.** Mit ST2 kommt eine Temperatur zurück — aber als gerechnete Größe
  aus dem Speicher, nicht als Eingabe.
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

### ST8 Solarthermie-Ganglinien kein Rechenweg — umgesetzt

- **Stand heute:** Die zugeordnete Ganglinie ist eigener Rechenweg (Weg a: mit Senken,
  Pufferladung und Kaskade wie das Kollektorfeld), umgesetzt über Folgeauftrag 4 der
  [`Folgeauftraege_Technikdokumentation_EPOS-Plan.md`](Folgeauftraege_Technikdokumentation_EPOS-Plan.md).
  Weiche, Einheit, Rückfälle und Bericht:
  [Konzept Simulationsablauf, Abschnitt 14](Konzept_Simulationsablauf_EPOS-Plan.md). Kein
  Referenzprojekt führt eine Ganglinie; die Referenzbasis bleibt.
- **Entscheidung des Anwenders: ☑ (entschieden, umgesetzt)**

---

## 2. Brauchwasser

Zwei Wege: der **Bestandsweg** (12 Monatswerte × 168-Stunden-Wochenprofil, `ProfilBedarf.Rechnen`)
und der **Zapfprofilgenerator** (Stufen Z1–Z5, Statuszeilen **#443**, **#451**, **#453**,
**#464**, **#495**). Die Weiche wählt je Projekt exklusiv
(`SimulationWaermebedarf.cs:1356-1371`); Referenzprojekt 1045 rechnet über den Generator.
Konzept: [`Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md`](Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md).

### BW1 Kein Zapfmodell — erledigt über den Generator

- **Stand heute:** Der Generator rechnet Mengengerüst, Kalender, Zapfereignisse (Stochastik),
  Zirkulation und die Auslegung (Summenlinie, Perzentil, Normvergleich). Der Bestandsweg bleibt
  daneben unverändert als einfacher Weg für Projekte, die nur eine Jahresmenge kennen.
- **Frage:** Stand des Bestandswegs?
- **Empfehlung: erledigt.** Den Bestandsweg **behalten** (schnell, für Gewerbe ohne
  Wohnungsdaten ausreichend), aber nicht weiter ausbauen; neue Fachlichkeit nur im Generator.
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

### BW2 Starrer Wochengang

- **Stand heute:** Bestandsweg: ein Wochenprofil von 168 Werten für alle 52 Wochen
  (`ProfilBedarf.cs:351`, `WOCHEN_STUNDEN = 168`). Generator: Kalender mit Werktag, Samstag,
  Sonn- und Feiertag, Ferienfenstern und Kaltwasser-Jahresgang (Konzept 4.2).
- **Frage:** (aus der Hilfeseite) Änderbar?
- **Vorschlag:** Für den Bestandsweg keine eigene Lösung — siehe PW2: eine **gemeinsame
  Kalenderschicht** für alle drei Profilarten (Brauchwasser, Prozesswärme, Strom), die
  Feiertage und Betriebsferien auf das Wochenprofil legt.
- **Empfehlung: über PW2** (dort entschieden). Für Wohngebäude ist der Generator der Weg.
- **Entscheidung des Anwenders: ☐**

### BW3 Keine Temperaturen im Rechenweg

- **Stand heute:** Bestandsweg: Energiemengen ohne Temperatur (Hilfeseite Brauchwasser, Gl. 1–6).
  Generator: Zapftemperatur, Kaltwasser-Jahresgang, Speicher-Solltemperatur
  (Konzept 4.0, Tabelle der Temperaturen).
- **Frage:** Sinnvoll zu ändern?
- **Erläuterung:** Die Temperatur des Brauchwassers wirkt an zwei Stellen: (1) auf die
  **Menge** (Nutzenergie = Volumen × c × (ϑ_Zapf − ϑ_KW)) — das leistet der Generator; (2) auf
  den **Erzeuger** (die Wärmepumpe muss für 55–60 °C höher heben als für den Heizkreis, der
  Brennwertkessel kondensiert weniger). (2) hängt nicht am Bedarf, sondern an der Anlage: Die
  Wärmepumpe rechnet ihren Brauchwasseranteil heute an der Vorlauftemperatur der Anlage.
- **Vorschlag:** Für den Bestandsweg nichts; der Bedarf bleibt Energie. Für (2) einen
  **Brauchwasser-Vorlauf je Erzeuger** (Feld „Vorlauf Trinkwassererwärmung“, Vorgabe leer = wie
  Anlage), mit dem die Wärmepumpe den Brauchwasserkanal am eigenen Kennfeldpunkt rechnet
  (VDI 4650 Blatt 1 trennt Heizung und Warmwasser bei der JAZ ebenso).
- **Empfehlung: später**, Aufwand M. Sinnvoll für Wärmepumpenprojekte mit hohem
  Warmwasseranteil; zuerst prüfen, wie die Kanäle der Wärmepumpe heute getrennt laufen. Vorgabe
  leer → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

### BW4 Keine Zirkulationsverluste als Posten; Netzverluste

- **Stand heute:** Bestandsweg: keine Zirkulation (`Brauchwasser_Zirkulation_Mwh` „auf dem
  Bestandsweg 0“, `SimulationWaermebedarf.cs:34-38`). Generator: Zirkulation als eigene Teilreihe
  nach drei Methoden (Konzept 4.3). Die **Netzverluste** sind **ein** Wert je Projekt in % oder
  kWh/a, als konstanter Stundenbetrag (`SimulationWaermebedarf.cs:636-650`), der je Stunde
  anteilig auf Heizung, Brauchwasser und Prozess verteilt wird (`:651-656`,
  `NetzverlusteVerteilen`).
- **Frage:** Gelten die Netzverluste auch für die Heizung? Vorschlag?
- **Erläuterung:** Ja — der Netzverlust ist ein Verlust des ganzen Verteilnetzes und trifft alle
  drei Kanäle im Verhältnis ihres Stundenbedarfs. Eine Zirkulation ist physikalisch etwas anderes:
  Sie läuft auch nachts und im Sommer, wenn kaum gezapft wird, mit nahezu fester Leistung.
- **Vorschlag:** Netzverluste **je Kanal** statt eines Projektwerts: drei Felder (Heizung,
  Brauchwasser, Prozess) in % oder kWh/a; Vorgabe: alle drei leer = der heutige Projektwert mit
  heutiger Verteilung. Für den Brauchwasserkanal eine **Zirkulation als feste Leistung in
  Laufstunden** (`P_zirk` [kW], Laufzeit [h/d]) — dieselbe Formel wie Methode „manuell“ des
  Generators (Konzept 4.3), damit der Bestandsweg sie ohne Wohnungsdaten bekommt. Normen:
  EN 15316-3 (Verteilung TWW), DIN V 18599-8, DVGW W 551 (Laufzeit), für Wärmenetze AGFW-Regeln.
- **Empfehlung: ja**, Aufwand M. Die Zirkulation ist in Mehrfamilienhäusern oft ein Drittel bis
  die Hälfte der Brauchwasserwärme und bestimmt Wärmepumpen- und Solarauslegung mit. Vorgabe
  leer → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

### BW5 Kein Legionellenbetrieb, keine Nachheizung

- **Stand heute:** Kein eigener Vorgang, weder im Bestandsweg noch im Generator (der Generator
  benutzt DVGW W 551 nur für Mindesttemperatur und Großanlagenkriterium, Konzept 4.5/4.7).
- **Frage:** Sinnvoll? Signifikant?
- **Erläuterung:** Energetisch klein: Eine wöchentliche Aufheizung eines 1 000-l-Speichers von
  60 auf 70 °C kostet `1 000 · 1,163 · 10 / 1000 ≈ 11,6 kWh`, im Jahr rund 0,6 MWh plus etwas
  Mehrverlust — gegen typische Brauchwassermengen eines solchen Hauses wenige Prozent.
  **Signifikant ist sie für die Wärmepumpe**, weil sie die Spitze oberhalb ihres Vorlaufs mit
  Heizstab oder Zweiterzeuger decken muss, und für die Leistungsspitze im Stromlastgang.
- **Vorschlag:** Projektschalter „Thermische Desinfektion“ mit Intervall (Tage), Uhrzeit,
  Zieltemperatur und Volumen (aus dem Brauchwasserspeicher); im Lauf ein Zusatzbedarf
  `Q = V · c · (ϑ_Ziel − ϑ_Soll)` in der gewählten Stunde, als eigener Posten im
  Brauchwasserkanal und vom Erzeuger gedeckt, der die Zieltemperatur erreicht (sonst Heizstab).
- **Empfehlung: später**, Aufwand M. Erst sinnvoll, wenn Speicher und Wärmepumpen-Vorlauf je
  Kanal (BW3, PS5) da sind; vorher gäbe es niemanden, der die Temperatur prüft.
- **Entscheidung des Anwenders: ☐**

### BW6 Einheit MWh im Lauf — erledigt

- **Stand heute:** Gerechnet wird in MWh, angezeigt in der gewählten Einheit; Bedarfsdialog und
  Ergebnisdialog lassen MWh oder kWh wählen und merken sich die Wahl
  (`EPOS.UI/Dialoge/Bedarf/BedarfsProfileDialog.razor:379`,
  `EPOS.UI/Dialoge/Bedarf/BedarfErgebnisDialog.razor:279-283`, Anwenderentscheid W8‑O‑5).
- **Frage:** kWh oder wählbar?
- **Empfehlung: erledigt** (wählbar). Die interne Rechengröße bleibt MWh, weil Bericht,
  Wirtschaftlichkeit und Referenzlauf daran hängen; eine Umstellung brächte nichts Sichtbares.
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

---

## 3. Prozesswärme

Dieselbe Profilroutine wie Brauchwasser (`EPOS.Kern/Allgemein/Simulation/ProfilBedarf.cs`),
Aufruf in `SimulationWaermebedarf.cs:1293`. Die Einfrierregel „gesäte Bedarfsdaten“ gilt für
jedes Referenzprojekt, das eine Prozesswärme-Zuordnung trägt.

### PW1 Wärmemenge ohne Temperaturniveau

- **Stand heute:** In der Rechnung kommt keine Temperatur vor (Hilfeseite Prozesswärme, Gl. 1–6);
  die Temperatur wirkt nur als Warnkriterium W3 an der Senkenzuordnung.
- **Frage:** Temperatur vorgebbar, auch als eigener Verlauf Bedarf + Temperatur?
- **Vorschlag:** Zwei Stufen.
  1. **Temperaturpaar je Prozess:** Spalten `Vorlauf`, `Ruecklauf` [°C] am Prozesswärmesatz
     (`Tab_Prozesswaerme`, Projektkopie; Schemaschritt, leer erlaubt). Der Lauf führt je Stunde
     die **höchste geforderte Vorlauftemperatur** des Prozesskanals (den Rücklauf
     mengengewichtet). Wirkung: Die Wärmepumpe wertet ihr Kennfeld für den Prozessanteil an
     dieser Temperatur aus; ein Erzeuger, der sie nicht erreicht, deckt den Prozesskanal nicht
     (heute nur Warnung W3); der Brennwertkessel sieht den Prozessrücklauf; der Puffer entnimmt
     ab der Zone, die die Temperatur hält (`Entnahme_*`, `T_Nutz`).
  2. **Zweikanalige Ganglinie:** Der Lastgangimport mit Kanal „Prozesswärme“ bekommt eine zweite
     Spalte „Vorlauftemperatur“ (8760 bzw. 35040 Werte). Für Prozesse mit wechselndem Niveau
     (Reinigungszyklen, Chargen).
- **Empfehlung: Stufe 1 ja** (M; ohne Temperatur bleibt die Wärmepumpe in der Industrie nicht
  bewertbar, und genau das fragen Kunden); **Stufe 2 später** (M), wenn erste Messreihen mit
  Temperatur vorliegen. Vorgabe leer → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

### PW2 Starrer Wochengang

- **Stand heute:** Ein 168-Stunden-Muster für alle Wochen (`ProfilBedarf.cs:351`); Betriebsferien
  gehen nur über einen kleineren Monatswert.
- **Frage:** (aus der Hilfeseite) Änderbar?
- **Vorschlag:** **Kalenderschicht für die Profilroutine:** je Profilzuordnung optional ein
  Betriebskalender mit Feiertagen (Bundesland), Betriebsferien (Zeiträume) und Tagtyp-Regel
  („Feiertag wie Sonntag“, „Ferientag = Faktor f × Mittelwert“). Wiederverwendet wird die
  Kalenderkarte der Konditionierungsprofile
  ([`Konzept_Konditionierungsprofile_EPOS-Plan.md`](Konzept_Konditionierungsprofile_EPOS-Plan.md),
  Feiertagsregeln und Vorlagen schon gebaut). Die Monatsmenge bleibt erhalten (die Normierung
  verteilt nur um) — außer der Anwender wählt „Ferien kürzen die Monatsmenge“. Gilt für alle drei
  Profilarten (BW2, Strombedarf).
- **Empfehlung: ja**, Aufwand M. Ein Werk mit drei Wochen Sommerstillstand ist der Normalfall der
  Industrieplanung; heute muss der Anwender das über Monatswerte von Hand nachbilden und verliert
  die Stundenstruktur. Ohne Kalender → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

### PW3 Monatsmenge unantastbar; unterschiedliche Monatsprofile

- **Stand heute:** Je Monat wird das Wochenprofil auf die Monatsmenge normiert; ein Typ hat ein
  Wochenprofil für alle Monate.
- **Frage:** Unterschiedliche Monatsprofile als Option?
- **Vorschlag:** Optional **ein Wochenprofil je Saison** (Winter, Übergang, Sommer) oder je Monat:
  Tabelle `Z_Prozesstyp_Monat` (`ID_Typ`, `Monat` 1–12, `ID_Wochenprofil`), leer = heutiges eine
  Profil. Dialog: Reiter „Wochenprofil“ bekommt eine Monatsleiste, in der jedem Monat ein Profil
  zugewiesen wird (Vorgabe „wie Januar“). Anlehnung an die Typtage der VDI 4655.
- **Empfehlung: später**, Aufwand M. Nutzen bei saisonalen Prozessen (Trocknung, Gewächshaus,
  Lebensmittel mit Erntesaison); mit PW2 und Monatswerten ist das Meiste schon abbildbar.
- **Entscheidung des Anwenders: ☐**

### PW4 Keine Verteilverluste je Prozess

- **Stand heute:** Eingabe ist Nutzwärme am Prozess; Verluste nur über den projektweiten
  Netzverlust (`SimulationWaermebedarf.cs:636-656`).
- **Frage:** Eigene Netzverluste angebbar?
- **Vorschlag:** Über BW4 (Netzverluste je Kanal) für den ganzen Prozesskanal; zusätzlich
  optional je Prozesssatz `Verlust_Prozent` (Leitung zum Prozess, Wärmeübertrager), der die
  Zeile vor der Kanalsumme erhöht: `q'(h) = q(h) · (1 + v/100)`.
- **Empfehlung: ja über BW4** (Kanalwert); **je Prozess später** (S), weil der Anwender selten
  getrennte Leitungsdaten hat. Vorgabe leer → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

### PW5 Kein Auslieferungskatalog

- **Stand heute:** Keine ausgelieferten Prozessprofile (Hilfeseite: „Absicht: Ein Prozess ist ein
  Einzelfall“).
- **Frage:** Standardprofile industrieller Prozesse vorschlagen.
- **Vorschlag:** Ein kleiner Katalog **typischer Betriebsweisen** (Wochenprofil + Monatsfaktoren +
  Temperaturniveau als Vorbelegung nach PW1), neutral benannt, mit runden Werten und dem Vermerk
  „Schichtmodell, keine Messung“:

  | Satz | Wochenprofil | Monatsfaktoren | Temperaturniveau (Vorbelegung) |
  |---|---|---|---|
  | Einschicht 5 Tage | Mo–Fr 6–14 Uhr Volllast, die Stunde davor 50 % | August 0,4 (Ferien), Dezember 0,8 | 60/40 °C |
  | Zweischicht 5 Tage | Mo–Fr 6–22 Uhr | wie oben | 70/50 °C |
  | Dreischicht 5 Tage | Mo 6 Uhr bis Sa 6 Uhr durchgehend | wie oben | 80/60 °C |
  | Durchlaufbetrieb 7 Tage | 7 × 24 h, 10 % Schwankung Tag/Nacht | 1,0, ein Revisionsmonat 0,7 | 90/70 °C |
  | Reinigung/Spülen (CIP) | Mo–Fr zwei Spitzen je Schichtende | 1,0 | 75/40 °C |
  | Trocknung/Lackierung | Mo–Fr 6–22 Uhr, Anfahrspitze 150 % in der ersten Stunde | 1,0 | 120/90 °C |
  | Waschen/Bäder (Galvanik, Wäscherei) | Mo–Fr 6–18 Uhr, Aufheizspitze zum Wochenstart | 1,0 | 60/45 °C |
  | Raumlufttechnik Halle | Mo–Fr 5–20 Uhr | Winter 1,0, Sommer 0,1 | 50/30 °C |

  Ausgeliefert als `Tab_Prozesstyp`-Sätze mit `ReadOnly = 1`, Jahresmenge je Satz 100 MWh
  (wird im Projekt skaliert).
- **Empfehlung: ja**, Aufwand S (Saat-Schritt wie bei Gebäuden und Kalendervorlagen). Spart dem
  Anwender das Tippen von 168 Werten und macht PW1 sofort nutzbar. Keine Herstellerdaten. Basis
  unberührt (Katalogsätze, keine Zuordnung in Referenzprojekten).
- **Entscheidung des Anwenders: ☐**

### PW6 Abbruch statt Nullprofil bei fehlendem Typbezug

- **Stand heute:** Fehlt der Typ eines Profils, meldet die Profilroutine eine Warnung und bricht
  die Schleife ab (`ProfilBedarf.cs:678-686`, `vollstaendig = false; break;`). Profile, die
  **vorher** in der Liste standen, sind dann schon aufaddiert; die folgenden fehlen. Der
  Wärmezweig wertet die Rückgabe nicht aus (`SimulationWaermebedarf.cs:1293`, ebenso `:1378` beim
  Brauchwasser) — der Lauf rechnet mit der **Teilsumme** weiter. Nur der Stromzweig bricht
  wirklich ab (`SimulationStrombedarf.cs:268-276`, `if (!vollstaendig) return null;`).
- **Frage:** Prüfen, Empfehlung.
- **Vorschlag:** Einheitlich und ehrlich: Ein Profil ohne Typ wird **übersprungen** (Anteil 0,
  Warnung mit Name) und die übrigen gerechnet — dieselbe Regel wie „kein Wochenprofil“ und
  „Monatssumme 0“ (`ProfilBedarf.cs:543-550`). Zusätzlich verhindert der Dialog das Speichern
  eines Profils ohne Typ (Pflichtfeld), sodass der Fall nur aus Altbeständen kommt.
- **Empfehlung: ja**, Aufwand S. Heute ist das Ergebnis von der Reihenfolge der Profile
  abhängig — das ist ein Fehler, keine Modellgrenze. Basis unberührt (kein Referenzprojekt
  enthält ein Profil ohne Typ; durch den Referenzlauf zu bestätigen).
- **Entscheidung des Anwenders: ☐**

---

## 4. Heizkessel

### HK1 Teillast, Takten, Brennwert — erledigt

- **Stand heute:** Gebaut nach dem Konzept
  [`Konzept_Kessel_Kennlinie_EPOS-Plan.md`](../ueberholt/Konzept_Kessel_Kennlinie_EPOS-Plan.md):
  Teillastkurve η(β) (**#625**), Kennzeichen „Brennwert“ und Brennwertkennlinie über den Rücklauf
  (**#627**, Schemaschritt 158), Takten mit Mindestleistung, Mindestlaufzeit und Anfahrverlust
  (**#630**), Kurve im Kesseleditor (**#635**). Referenzprojekt 1050.
- **Empfehlung: erledigt.** Die Rechenweg-Hilfeseite `Berechnung/Heizkessel.wiki` nennt in
  „Grenzen und Annahmen“ (Z. 311–317) noch „kein Teillastwirkungsgrad“, „keine Taktung“, „keine
  Brennwertrechnung“ — sie ist nachzuziehen (siehe „Fragliches“).
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

### HK2 Erläuterung „s_η = 1,5“

- **Stand heute:** `Kesselkennlinie.WirkungsgradAlsFaktor` deutet einen gespeicherten
  Wirkungsgrad über 1,5 als Prozentwert und teilt durch 100
  (`SimulationSPK.cs:536-544`; Hilfeseite Heizkessel Z. 79, 155–164).
- **Erläuterung:** Wirkungsgrade stehen im Bestand teils als Faktor (0,93), teils als Prozent (93).
  Die Schwelle trennt beide. Sie liegt nicht bei 1,0, weil ein Brennwertkessel, auf den
  **Heizwert** bezogen, über 100 % erreicht (etwa 1,04 als Faktor) — mit Schwelle 1,0 würde er
  als „1,04 %“ gelesen und auf 0,0104 zerlegt. Echte Prozentwerte liegen über 50, echte Faktoren
  unter etwa 1,1; 1,5 trennt sauber. Die Schwelle ist eine Lesehilfe, kein Modell.
- **Empfehlung: nichts ändern** (eine Bereinigung aller Bestandswerte auf Faktoren wäre ein
  Datenschritt ohne Rechennutzen).
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

### HK3 Erläuterung des Brennwert-Rechenwegs

- **Stand heute:** `η_eff = η_tr(β) + Δ₃₀ · g(ϑ_RL)` (`Kesselkennlinie.cs:318-324`) mit
  - trockener Teillastkurve `η_tr(β) = η₃₀' + (η₁₀₀ − η₃₀')·(β − 0,3)/0,7`, β auf [0,3; 1]
    (`:186-192`), wobei `η₃₀' = η₃₀ − Δ₃₀` der Teillastwert ohne Kondensation ist;
  - Kondensationsgewinn bei 30 °C Rücklauf `Δ₃₀` = 0,08 (Gas), 0,04 (Öl), 0,05 (Holz)
    (`:228-238`);
  - Kondensationsanteil `g = (ϑ_Tau − ϑ_RL)/(ϑ_Tau − 30)`, auf [0; 1,2] geklemmt, Taupunkt 57 °C
    (Gas), 47 °C (Öl), 50 °C (Holz) (`:219-225`, `:274-288`);
  - Obergrenze Hs/Hi des Brennstoffs;
  - Rücklauf der Stunde aus der Kette Heizkreisrücklauf (AK1) → Senkenspeicher → gepflegtes Paar
    → Rückfall 50 °C (`SimulationSPK.cs:341-348`).

  Probe: β = 0,3 bei 30 °C ergibt η₃₀, β = 1 über dem Taupunkt ergibt η₁₀₀ — der Katalog wird an
  beiden Prüfpunkten (Richtlinie 92/42/EWG, EN 15316-4-1) getroffen.
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

### HK4 Keine hydraulische Grenze zwischen Quellpuffer und Kessel

- **Stand heute:** Zwischen Puffer und Kessel wirken nur die Lade- und Entladeleistungsgrenzen
  des Speichers (`P_lad,max`, `P_ent,max`; Hilfeseite Pufferspeicher, Grenzen).
- **Frage:** Sinnvoll?
- **Erläuterung und Vorschlag:** Ein Übertrager begrenzt die Leistung und hebt die Temperatur.
  Die Leistungsgrenze **ist** schon da: `P_ent,max` auf die Übertragerleistung setzen. Die
  Temperaturwirkung (Grädigkeit) bräuchte ein Temperaturmodell zwischen Puffer und Kessel, das es
  sonst nirgends gibt. Hinweistext im Pufferdialog: „Übertrager: Leistung als Entladegrenze
  eintragen“.
- **Empfehlung: nein** (nur Hinweistext, S). Kein Planungsfall, der damit anders ausginge.
- **Entscheidung des Anwenders: ☐**

---

## 5. Wärmepumpe

Quelle: `EPOS.Kern/Allgemein/Simulation/SimulationWaermepumpe.cs`.

### WP1 Keine Taktung innerhalb der Stunde

- **Stand heute:** Eine Stunde ist voll, moduliert oder aus; Mindestlaufzeit, Mindestpause und
  Anlaufverluste fehlen (Hilfeseite Wärmepumpe, Grenzen). Moduliert heißt heute: jede Teillast bis
  0 mit dem COP des Kennfeldpunkts.
- **Frage:** Anders abbildbar?
- **Vorschlag:** Taktverlust nach **EN 14825** (Teillastkorrektur unterhalb der kleinsten
  Modulationsstufe):
  `COP_takt = COP · CR / (C_d · CR + (1 − C_d))`, `CR = Q_Stunde / (P_min · 1 h)` für
  `0 < CR < 1`, sonst `COP_takt = COP`. `P_min` [kW] = kleinste Modulationsleistung (neue
  Katalog- und Projektspalte „Mindestleistung“, leer = keine Taktrechnung), `C_d` Vorgabe 0,9
  (Vorgabewert der EN 14825, wenn nicht gemessen). Zusätzlich die Zahl der Starts wie beim Kessel
  (`Kesselkennlinie.Taktet`, `StartsHoechstens`, `Kesselkennlinie.cs:460-490`) als Ausweis —
  Starts je Jahr sind eine Planungsgröße (Pufferauslegung, VDI 4645).
- **Empfehlung: ja**, Aufwand M. Gleiche Bauart wie Kessel E4, die Mindestleistung steht in jedem
  Datenblatt, der Effekt trifft genau die Übergangszeit, in der überdimensionierte Geräte schlecht
  laufen. Vorgabe leer → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

### WP2 Extrapolation nach oben gekappt, nach unten vorhanden

- **Stand heute:** Über der obersten Stützstelle gilt deren COP (Kappung, gezählt und einmal je
  Lauf gemeldet, `SimulationWaermepumpe.cs:452-466`). Unter der untersten Stützstelle wird für
  Außenluft, Erdreich und Profil linear verlängert, solange die Projekteinstellung
  `Extrapolation_erlaubt` gesetzt ist (Vorbelegung ja, `:449`); für die Pufferquelle (Booster)
  wird stattdessen gekappt (`:470-484`).
- **Erläuterung:** Nach oben gibt es keine Herstellerdaten, und der COP wächst dort nicht beliebig
  (Verdichtergrenzen) — Kappung ist die vorsichtige Wahl. Nach unten ist die Unterschreitung ein
  echter Betriebszustand (kalter Tag); die Gerade aus den beiden untersten Punkten ist dort die
  beste verfügbare Schätzung. Am Booster heißt „unter der Kennlinie“ nur, dass der Quellpuffer
  leer ist — deshalb Kappung.
- **Empfehlung: nichts ändern.** Optional (S): Untergrenze des Einsatzbereichs als Katalogspalte,
  unter der das Gerät abschaltet und der Zweiterzeuger übernimmt — nur auf Wunsch.
- **Entscheidung des Anwenders: ☐**

### WP3 Keine Abtauverluste, keine Feuchtekorrektur

- **Stand heute:** Was das Kennfeld ausweist, gilt (Hilfeseite Wärmepumpe, Grenzen).
- **Frage:** Vorschlag?
- **Erläuterung:** Kennfelder nach EN 14511 enthalten an den Prüfpunkten A2 und A‑7 die Abtauung
  bereits als integrierten Mittelwert. Ein pauschaler Abschlag auf solche Daten zählt doppelt.
  Nur Kennfelder ohne Abtauung (reine Verdichterdaten) brauchen eine Korrektur.
- **Vorschlag:** Katalogkennzeichen „Abtauung im Kennfeld enthalten“ (Vorgabe ja). Bei „nein“
  ein Abtaufaktor `f_ab(ϑ_a, φ)` auf den COP für Außenlufttemperaturen zwischen −10 und +6 °C
  (Maximum um 0 bis +3 °C bei hoher Feuchte; Verfahren nach EN 15316-4-2), die relative Feuchte
  aus den Klimadaten (TRY führen sie).
- **Empfehlung: später**, Aufwand M. Erst klären, welche Kennfelder im Katalog ohne Abtauung
  sind; ist die Antwort „keine“, entfällt der Punkt. Vorgabe ja → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

---

## 6. BHKW

Quelle: `EPOS.Kern/Allgemein/Simulation/SimulationBHKW.cs`.

### BH1 Teillastkennlinie

- **Stand heute:** Ein elektrischer und ein thermischer Wirkungsgrad für jede Auslastung; die
  „Laufzeit“ ist eine thermische Vollbenutzungsstundenzahl, keine Betriebsstundenzahl
  (`SimulationBHKW.cs:107-120`).
- **Frage:** Ja, als Option — Vorschlag wie die Kesselkennlinie.
- **Vorschlag:** Zwei Stützpunkte wie beim Kessel: Volllast (η_el,100, η_th,100 — vorhanden) und
  Teillast bei 50 % elektrischer Last (neue Spalten `Wirkungsgrad_el_Teillast50`,
  `Wirkungsgrad_th_Teillast50`, leer = wie Volllast). Linear in β zwischen 0,5 und 1:
  `η(β) = η₅₀ + (η₁₀₀ − η₅₀)·(β − 0,5)/0,5`; Brennstoff `B = P_el/η_el(β)`, Wärme
  `Q = B · η_th(β)`. Die Stromkennzahl ändert sich damit in Teillast (typisch fällt η_el, steigt
  η_th). Unter β = 0,5 läuft das Modul nicht moduliert, sondern taktet (BH2). Normbezug:
  EN 15316-4-4. Editor wie beim Kessel (#635) mit kleiner Kurve.
- **Empfehlung: ja**, Aufwand M. Wärmegeführte Module laufen im Sommer lange in Teillast; die
  KWKG-Strommenge und der Brennstoff hängen daran. Herstellerblätter nennen Werte bei 50 % und
  75 %. Vorgabe leer → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

### BH2 Taktung, Mindestlaufzeit, Anfahrverluste

- **Stand heute:** Keine Taktung (`SimulationBHKW.cs:109-113`). Die „Untere Grenzleistung“ wirkt
  nicht (Befund in Folgeauftrag 6 der
  [`Folgeauftraege_Technikdokumentation_EPOS-Plan.md`](Folgeauftraege_Technikdokumentation_EPOS-Plan.md)).
- **Frage:** Machbar wie Kessel E4?
- **Vorschlag:** Ja, dieselbe Regel: Liegt die Wärme der Stunde unter `P_min · 1 h`, taktet das
  Modul mit `n = min(⌊60/t_min⌋, ⌈Q/(P_min·t_min/60)⌉)` Starts, je Start ein Anfahrverlust
  `E_anf` [kWh Brennstoff]; Mindestlaufzeit `t_min` [min]. `P_min` ist die (wirksam gemachte)
  Untergrenze aus Folgeauftrag 6 — beide Aufträge zusammen umsetzen. Ausweis: Starts je Jahr
  (Wartungsintervalle der Hersteller sind startabhängig).
- **Empfehlung: ja**, Aufwand M (mit Folgeauftrag 6). Die Regel und ihre Tests liegen beim Kessel
  bereit. Basis: Folgeauftrag 6 berührt BHKW-Projekte der Referenzbasis (die Testdatenbank führt
  Grenzleistungen von 468 bis 1 027 % — Datenbefund, vor der Umsetzung zu bereinigen); mit
  Vorgabe leer sonst unberührt.
- **Entscheidung des Anwenders: ☐**

### BH3 Erläuterung Energieprobe

- **Stand heute:** `Energieprobe()` prüft nach jedem Lauf „Produktion = Direktdeckung +
  Speicherladung + Überschuss“ über das Jahr (Toleranz 1 kWh) und jede Stunde (0,01 kWh) und
  schreibt nur auf die Konsole (`SimulationBHKW.cs:1841-1853`).
- **Erläuterung:** Ein Selbsttest für Entwickler: Er findet Buchungsfehler im Modul, sagt aber
  nichts über die Eingaben des Anwenders. Deshalb steht er nicht im Simulationsprotokoll.
- **Empfehlung: nichts ändern.**
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

---

## 7. Pufferspeicher

Quelle: `EPOS.Kern/Allgemein/Simulation/SimulationPufferspeicher.cs`. Vergleich mit der Norm:
[`2026-10-01_Recherche_Normen_Speichermodell.md`](Pufferspeicher/2026-10-01_Recherche_Normen_Speichermodell.md),
Abschnitt 3.

### PS1 Ideale Schichtung, gleich große Zonen

- **Stand heute:** Es gibt ein **Schichtspeichermodell** (Paket P1): N Zonen gleichen Volumens,
  Zustand je Zone als Energie `E[i] ∈ [0, Q_max/N]` bzw. Temperatur zwischen Rücklauf und
  Vorlauf, mit Wärmeleitung und Inversionsmischung zwischen den Zonen; der Füllstand SOC bleibt
  die führende Größe (`SimulationPufferspeicher.cs:1180-1215`, Zonengröße `Q_max / n` an
  `:1425`). Einströmendes Wasser schichtet sich ideal in die Zone passender Temperatur ein.
- **Erläuterung:** Ein Schichtspeichermodell teilt den Behälter in übereinanderliegende Scheiben
  und führt je Scheibe eine Temperatur. Erzeuger laden oben (heiß) oder auf Höhe ihrer
  Temperatur, Verbraucher entnehmen auf ihrer Höhe, das Rücklaufwasser kommt unten an. Damit sieht
  der Kollektor (ST2) oder die Wärmepumpe den kalten Teil, der Heizkreis den warmen.
  prEN 15316-5:2024 rechnet so (Methode A, bis 10 Schichten, ohne Wärmeleitung — EPOS rechnet
  feiner).
- **Vorschlag:** (a) **Zonenanteile wählbar** statt gleich groß: Spalte `Schicht_Anteile`
  (z. B. „0,10;0,16;0,37;0,37“ von oben), Vorgabe leer = gleich; Vorgabehilfe die
  Standardaufteilung der Norm (Tabelle B.4) für Kombispeicher. (b) **Einströmmischung** als
  Parameter `m_ein` [0–1]: Anteil des einströmenden Wassers, der sich mit der Eintrittszone
  vermischt, statt ideal einzuschichten (0 = heute). (c) **Bereitschaftsverlust
  temperaturabhängig** `H = Q_B·1000/(24·45 K)` [W/K] je Zone mit `(ϑ_i − ϑ_Raum)` statt
  Tageswert anteilig zum SOC (`SimulationPufferspeicher.cs:416`, `:646-671`) — heute verliert ein
  leerer Puffer nichts, obwohl er auf Rücklauftemperatur steht (Recherche Abschnitt 3.2; Prüfwert
  nach EN 15332 bei 45 K).
- **Empfehlung: (c) ja** (S–M; fachlich klar, der Katalogwert liefert H; Basis neu für jedes
  Referenzprojekt mit Puffer — deshalb als Option mit Vorgabe „Tageswert“); **(a) später** (S,
  erst mit Kombispeichern nötig); **(b) nein** (kein Anwender hat Daten dafür).
- **Entscheidung des Anwenders: ☐**

### PS2 Quellspeicher statisch

- **Stand heute:** Ein Quellspeicher einer Wärmepumpe startet voll und bleibt bei fester
  Temperatur, solange er nicht zugleich Senke eines Erzeugers ist
  (`EPOS.Kern/Allgemein/Simulation/WaermequelleClass.cs:779-840`, „Quellspeicher startet
  gefüllt“).
- **Frage:** Dynamisieren?
- **Vorschlag:** Dynamisch ist er schon, sobald ein Erzeuger ihn lädt (geteilter Puffer). Was
  fehlt, ist ein **Lader ohne Erzeuger**: Abwärme oder Prozessrückwärme als Ganglinie.
  Vorschlag: eine Quellenart „Abwärme (Ganglinie)“, die als Lader auf den Quellspeicher wirkt
  (Leistung je Stunde, Temperatur), dann rechnet der Speicher stündlich mit Ladung und Entzug.
- **Empfehlung: später**, Aufwand M. Nur bei Abwärmeprojekten nötig; diese Fälle zuerst sammeln.
- **Entscheidung des Anwenders: ☐**

### PS3 Durchfluss nicht in Schichten

- **Stand heute:** Was in derselben Stunde über `Q_max` hinaus geladen und entnommen wird, ist
  Durchfluss und wird nicht in Zonen geführt (`SimulationPufferspeicher.cs:1196-1201`).
- **Erläuterung:** Dieser Anteil fließt hydraulisch durch den Behälter (Erzeuger → Verbraucher in
  derselben Stunde) und war nie Speicherinhalt; eine bei Vorlauftemperatur gekappte Zonensumme
  kann ihn nicht darstellen. Die Bilanz bleibt richtig, nur die Zonentemperatur sieht ihn nicht.
- **Empfehlung: nichts ändern.**
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

### PS4 Katalog nur sechs Gerätewerte

- **Stand heute:** Der Katalog führt Bezeichner, Hersteller, Speichertyp, Bereitschaftsverluste,
  Gesamtvolumen und Investitionskosten (`EPOS.Kern/Controller/PufferSpStammCtrl.cs:106-108`).
  Temperaturen, Schwellen, Schichten, Entnahmehöhen und Leistungsgrenzen entstehen in der
  Projektkopie.
- **Erläuterung:** Das Gerät hat Volumen und Dämmung; Temperaturen und Regelung sind Eigenschaften
  der **Anlage**, in der es steht. Deshalb gehören sie in das Projekt.
- **Vorschlag:** Höchstens Höhe und Durchmesser in den Katalog (für Mantelfläche und
  Zonenverlustverteilung nach PS1 (c), prEN 15316-5 Formel 5) — nur mit PS1 (c).
- **Empfehlung: nein** (ohne PS1 (c)); mit PS1 (c) S.
- **Entscheidung des Anwenders: ☐**

### PS5 Kein Wärmeübertrager, kein Frischwassermodul, keine Legionellenschaltung

- **Stand heute:** Keines davon im Modell (Hilfeseite Pufferspeicher, Grenzen).
- **Vorschlag:** (a) Frischwassermodul als Entnahme des Brauchwasserkanals aus der obersten Zone
  mit Mindesttemperatur `ϑ_Zapf + ΔT_FWM` (Vorgabe 5 K) — der Puffer muss diese Temperatur oben
  halten, sonst deckt ein Nachheizer; (b) Wärmeübertrager wie HK4 über die Entladegrenze;
  (c) Legionellenschaltung siehe BW5.
- **Empfehlung: (a) später** (M, zusammen mit der Pufferauslegung und BW3); **(b) nein**;
  **(c) über BW5**.
- **Entscheidung des Anwenders: ☐**

---

## 8. Strombedarf

### SB1 Viertelstunden ohne Unterstruktur — PV und Stromspeicher in 15 Minuten

- **Stand heute — wo gemittelt, wo viertelstündlich gerechnet wird:**
  1. **Strombedarf:** Profile liefern Stundenwerte und werden auf vier gleiche Viertel gespreizt
     (`SimulationStrombedarf.cs:118`); Ganglinien im Viertelstundenraster gehen echt
     viertelstündlich ein, Stundenganglinien werden gespreizt (`:173-176`). Ergebnis:
     `Strombedarf_viertelStundenwerte`, 35 040 Plätze, Ausgabe `strombedarf_viertelstunde.csv`
     (`Referenzlauf/Ergebnisexport.cs:130`).
  2. **Restbedarf der Kaskade** führt die Viertelstunden weiter (`SimulationControl.cs:521`);
     Wärmepumpen-, Kessel- und BHKW-Strom kommen als gespreizte Stundenwerte dazu (`:570-573`).
  3. **Photovoltaik mittelt:** `Strombedarf_stuendlich = Viertelstunden_zu_stunden(Strombedarf)`
     (`SimulationPV.cs:162-163`). Ertrag (aus stündlichen Klimadaten), Direktverbrauch und
     Überschuss werden **stündlich** gerechnet (`:428-445`) und danach auf vier gleiche Viertel
     gespreizt (`:463-465`, `:497-505`). Abgezogen wird die gespreizte PV vom viertelstündlichen
     Restbedarf (`SimulationControl.cs:578-581`) — Restbezug und PV-Kennzahlen stammen damit aus
     zwei Auflösungen.
  4. **Stromspeicher und Flotte** rechnen nativ im Viertelstundenraster (35 040 Intervalle), auf
     der viertelstündlichen Last und der **gespreizten** PV-Reihe
     (`EPOS.Kern/Controller/StromspeicherSimCtrl.cs:1173-1180`).
- **Frage:** PV und Stromspeicher in 15-Minuten-Intervallen rechnen.
- **Vorschlag:** (a) **PV-Bilanz auf 35 040:** Direktverbrauch `min(P_PV,q, P_Last,q)` je Viertel
  statt je Stunde; die PV-Reihe innerhalb der Stunde nicht treppenförmig, sondern
  energieerhaltend nach dem Sonnenstand je Viertelstunde verteilt
  (`P_PV,q = P_PV,h · cos θ_q / Σ cos θ` der Stunde; Summe der Viertel = Stundenwert). Damit
  werden Eigenverbrauch, Einspeisung und Restbezug aus **einer** Auflösung gebildet. (b)
  **Stromprofile im Viertelstundenraster** (672 Werte je Woche) als zweite Form neben 168, für
  Standardlastprofile, die nativ viertelstündlich sind. (c) Klimadaten in 10- oder 15-Minuten
  (DWD liefert 10-Minuten-Werte) — nur als spätere Option.
- **Empfehlung: (a) ja**, Aufwand M. Bei Gewerbelast mit Viertelstundenspitzen sinkt der
  Eigenverbrauch messbar (Größenordnung wenige Prozent), und Peak-Shaving und Leistungspreis
  hängen an der Viertelstunde; die Flotte gewinnt eine glatte PV-Reihe. **Basis neu** für jedes
  Referenzprojekt mit PV — Einfrierregel, eigene Welle. **(b) später** (M), **(c) nein**
  (Datenlage, Rechenzeit, Nutzen klein gegen (a)).
- **Entscheidung des Anwenders: ☐**

### SB2 Projektfilter im Stromzweig — erledigt

- **Stand heute:** Stromverbraucher, Brauchwasser und Prozesswärme werden über die ID der
  Projektkopie zugeordnet; gelesen wird nur die Kopie des eigenen Projekts (**#641**, **#643**;
  `ProfilBedarf.cs:619-624`).
- **Empfehlung: erledigt.** Die Zeile „Kein Projektfilter im Stromzweig“ der Hilfeseite
  Strombedarf (Z. 217) ist nachzuziehen (siehe „Fragliches“).
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

### SB3 Die Peak-Shaving-Maske schreibt nicht

- **Stand heute:** Die Maske ist eine eigenständige Auswertung auf einer gewählten Ganglinie
  (Projekt, Katalog oder Datei) und speichert weder Ganglinie noch Ergebnis; Ausgabe ist CSV. Sie
  hat aber einen Knopf, der **Zielschwelle und Adaptiv-Kennzeichen in die aktive
  Speichervariante** schreibt (`EPOS.UI/Dialoge/Strom/PeakShavingDialog.razor:414-418`,
  `VarianteUebernehmen`, LS‑E‑1 (a)).
- **Erläuterung:** Die Maske beantwortet „Welche Spitze kann dieser Speicher an dieser Last
  höchstens kappen?“, ohne den Projektstand zu verändern. Was sie übernehmen darf, ist der
  Sollwert für den eigentlichen Lauf — und nur das tut sie.
- **Empfehlung: nichts ändern;** die Hilfeseite (Schritt 7 und Grenzen) auf „schreibt nur die
  Schwelle in die aktive Variante“ berichtigen.
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

---

## 9. Photovoltaik

### PV1 Kein Ein-Dioden-Modell

- **Stand heute:** Der Ertrag kommt aus Einstrahlung, Modultemperatur und Temperaturkoeffizient
  (vereinfachtes oder erweitertes Modell); die elektrischen Kenngrößen gehen nur in die
  Auslegungsprüfungen P1–P5 ein (Hilfeseite Photovoltaik, Grenzen).
- **Vorschlag:** Keiner. Ein Ein-Dioden-Modell braucht fünf Parameter je Modul (Photostrom,
  Sättigungsstrom, Idealitätsfaktor, Serien- und Parallelwiderstand), die Datenblätter nicht
  angeben; sie würden aus denselben sechs Kennwerten geschätzt. Der Jahresertrag ändert sich
  dadurch im Bereich der Unsicherheit der Klimadaten.
- **Empfehlung: nein.**
- **Entscheidung des Anwenders: ☐**

### PV2 Keine Leistungsgrenze je MPP-Eingang

- **Stand heute:** Der MPP-Eingang ist Prüfgröße; geklemmt wird am Gerät
  (`SimulationPV.cs:1232-1236`).
- **Vorschlag:** Klemmung je MPPT auf `min(P_DC,MPPT, I_max,MPPT · U_MPP)` — nur wirksam, wenn ein
  Strang den Eingangsstrom überschreitet; das melden die Prüfungen schon.
- **Empfehlung: später**, Aufwand S. Ein Planer korrigiert eine solche Belegung, statt sie zu
  rechnen; die Warnung genügt.
- **Entscheidung des Anwenders: ☐**

### PV3 Keine Netzeinspeisebegrenzung im Stundenlauf

- **Stand heute:** Der PV-Lauf kappt die Einspeisung nicht. Die Flotte kennt eine **harte**
  Einspeisegrenze, die eine Variante unzulässig macht (`SpeicherEngine/FlottenModel.cs:827`,
  Ressource `FLOTTE_ED_EINSPEISEGRENZE`).
- **Vorschlag:** Projektfeld „Einspeisegrenze“ in kW oder in % der installierten Leistung (leer =
  keine). Im PV-Lauf: `E_ein,q = min(Ü_q, P_grenz · Δt)`, der Rest ist **Abregelung**, eigene
  Reihe und Kennzahl (kWh/a, % des Ertrags). Die Flotte liest denselben Wert als weiche Grenze:
  Laden vor Abregeln. Anlass: Einspeisebegrenzungen nach EEG und Vorgaben der Netzbetreiber sind
  Planungsalltag, und der Speicher wird oft genau dafür gekauft.
- **Empfehlung: ja**, Aufwand M (S für den PV-Lauf, Rest Flotte). Vorgabe leer → Basis
  unberührt.
- **Entscheidung des Anwenders: ☐**

### PV4 Albedo fest 0,2

- **Stand heute:** `ALBEDO_BODEN = 0.2` (`EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:333`); an
  zwei weiteren Stellen steht das Literal 0,2 statt der Konstanten (`:455`, `:599`).
- **Frage:** Einstellbar?
- **Vorschlag:** Feld „Albedo“ an der Anlagenzeile (PV und Solarthermie), Vorgabe 0,2, mit
  Auswahlhilfe (Gras 0,2, Beton 0,3, helles Dach 0,5–0,6, Schnee 0,7–0,8). Alle drei Stellen auf
  den Parameter.
- **Empfehlung: ja**, Aufwand S. Für Fassaden- und steile Anlagen und für bifaziale Module wirkt
  der Bodenreflex deutlich. Vorgabe 0,2 → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

---

## 10. Stromspeicher und Speicherflotte

Engine `SpeicherEngine`, Planer `SpeicherPlanung`; Konzept
[`Doku_Mehrspeicher_Konzept_und_Umsetzung.md`](Doku_Mehrspeicher_Konzept_und_Umsetzung.md).
Referenzprojekt 1046 (Einfrierregel „Flottenstand @Projektflotte“).

### SP1 Standby-Verbrauch nicht berücksichtigt

- **Stand heute:** Weder Standby noch Selbstentladung in der Engine (keine Fundstelle in
  `SpeicherEngine/*.cs`).
- **Vorschlag:** Je Einheit `P_standby` [W] (Batteriemanagement, Wechselrichter im Leerlauf):
  je Intervall `E = P_standby · Δt`, gedeckt aus PV-Überschuss, sonst aus dem Netz (nicht aus der
  Batterie, wie reale Systeme es meist tun); optional Selbstentladung [%/Monat] auf den SOC.
  Ausweis „Eigenverbrauch Speichersystem kWh/a“.
- **Empfehlung: ja**, Aufwand S–M. Kleine Speicher mit einigen zehn Watt Leerlauf verlieren damit
  einen merklichen Teil ihres Nutzens — eine Aussage, die Kunden interessiert. Vorgabe 0 → Basis
  unberührt.
- **Entscheidung des Anwenders: ☐**

### SP2 Auslegungsoptimierung schreibt nichts ins Simulationsergebnis

- **Stand heute:** Die Rastersuche ist ein eigener Lauf (Hilfeseite Stromspeicher, Grenzen).
- **Frage:** Aufnehmen?
- **Vorschlag:** Kein Rückschreiben in das Ergebnis, sondern ein Knopf „Beste Variante übernehmen“:
  Er legt den besten Rasterpunkt als Speichervariante an (oder setzt ihn aktiv), danach rechnet
  der normale Projektlauf — so bleibt das Simulationsergebnis immer das Ergebnis **eines** Laufs
  mit gespeicherten Eingaben.
- **Empfehlung: ja**, Aufwand S. Basis unberührt.
- **Entscheidung des Anwenders: ☐**

### SP3 Rastersuche endlich

- **Erläuterung:** Gefunden wird der beste der gerechneten Punkte. Vorschlag für später: eine
  zweite, feinere Suche um den besten Punkt (halbe Schrittweite, Nachbarn).
- **Empfehlung: später**, Aufwand S. Die Kostenkurve ist um das Optimum meist flach.
- **Entscheidung des Anwenders: ☐**

### SP4 Planung optimal nur je Horizont

- **Erläuterung:** Der Planer löst je Horizont ein gemischt-ganzzahliges Problem und rollt weiter;
  über das Jahr ist das eine gute, aber keine bewiesen beste Fahrweise — reale
  Energiemanagementsysteme arbeiten genauso (sie kennen die Zukunft nur über Prognosen). Ein
  Jahresoptimum setzte perfekte Voraussicht voraus und wäre damit zu optimistisch.
- **Empfehlung: nichts ändern.**
- **Entscheidung des Anwenders: ☐ (nur Kenntnisnahme)**

### SP5 Ohne Fahrplaner keine planenden Ziele auf iOS

- **Erläuterung:** Die planenden Ziele `PvPlanung`, `Arbitrage`, `MultiUse` brauchen den
  MILP-Löser Google OR-Tools. Er hängt nur an `SpeicherPlanung`, und nur die Windows-Schale
  registriert den Planer. Das Paket liefert seine native Hälfte für Windows, Linux und macOS,
  **nicht für iOS**; die macOS-Fassung besteht aus rund 100 dynamischen Bibliotheken, die iOS
  nicht lädt (geprüft am 11.09.2026,
  [`Umsetzung_iU10_Nachweise.md`](Umsetzung_iU10_Nachweise.md), Abschnitt zum fehlenden Planer
  auf iOS). Ein eigener Port hieße, die ganze C++-Kette statisch für iOS zu übersetzen. Die
  reaktiven Ziele `PvGreedy` und `PeakShaving` rechnen überall.
- **Vorschlag für später:** ein zweiter Planer hinter `IFlottenPlaner` mit einem reinen
  .NET-Löser (LP-Relaxation oder dynamische Programmierung je Horizont), dann auch auf dem iPad.
- **Empfehlung: später**, Aufwand L. Erst bei Bedarf der iPad-Anwender.
- **Entscheidung des Anwenders: ☐**

### SP6 Rainflow ohne Alterung

- **Stand heute:** Rainflow zählt Zyklen und Schaden; Kapazität und Leistung bleiben über die
  Jahre gleich.
- **Vorschlag:** Alterung **in der Wirtschaftlichkeit**, nicht im Stundenlauf: Kapazität im Jahr n
  `K_n = K_0 · (1 − a_kal · n − a_zyk · D_kum,n)` mit Kalender- und Zyklenalterung aus den
  Garantieangaben (x % nach y Jahren bzw. z Zyklen); der Jahreslauf wird mit `K_n` für drei
  Stützjahre neu gerechnet und interpoliert.
- **Empfehlung: später**, Aufwand M–L. Wirkt auf die Wirtschaftlichkeit über 15–20 Jahre; die
  Daten (Garantiekurven) hat der Anwender meist nur grob.
- **Entscheidung des Anwenders: ☐**

### SP7 Keine Herkunftsschichten im Speicher

- **Erläuterung:** Der Speicher weiß nicht, ob eine Kilowattstunde aus PV, BHKW oder Netz stammt.
  Nötig ist das nur für Herkunftsnachweise (etwa Mieterstrom, Netzladung mit anderem Tarif).
- **Empfehlung: nein** (bis ein Mieterstrom- oder Netzladungsfall es verlangt; dann M).
- **Entscheidung des Anwenders: ☐**

### SP8 Einfacher Tarif

- **Stand heute:** Ein Leistungspreis je Periode, keine Monatsspitzen, keine Tarifstufen.
- **Vorschlag:** Leistungspreis auf die **Jahreshöchstlast** (registrierende Leistungsmessung) und
  optional auf Monatshöchstwerte; Hochlastzeitfenster als Kalender.
- **Empfehlung: später**, Aufwand M. Wichtig für Peak-Shaving-Wirtschaftlichkeit in der
  Industrie; zuerst die Tarifdaten im Kostenteil klären.
- **Entscheidung des Anwenders: ☐**

### SP9 Wärme und Strom nicht gemeinsam optimiert

- **Erläuterung:** BHKW-Fahrplan und Wärmepumpenstrom sind Eingaben der Flotte. Eine gemeinsame
  Optimierung (Wärmepumpe läuft, wenn PV-Strom da ist; BHKW fährt nach Strompreis mit
  Pufferspeicher) wäre ein sektorgekoppeltes MILP mit Wärmespeichern — ein neues Modul.
- **Empfehlung: nein** für jetzt (L); als Konzeptarbeit vormerken, wenn Kunden
  Wärmepumpe + PV + Batterie + Puffer gemeinsam bewerten wollen. Eine einfache Vorstufe ist die
  vorhandene PV-Überschuss-Leistungssteuerung der Wärmepumpe.
- **Entscheidung des Anwenders: ☐**

### SP10 Start-Ladezustand

- **Stand heute:** Im Projektlauf steht er auf der unteren Marke; die Vorprüfung sagt es an.
- **Vorschlag:** Option „periodisch“: zweiter Durchlauf mit `SOC_Start = SOC_Ende` des ersten —
  das entfernt den Anfangseffekt aus der Jahresbilanz.
- **Empfehlung: später**, Aufwand S. Der Effekt ist etwa eine Ladung im Jahr; Basis 1046 neu, wenn
  als Vorgabe gesetzt — deshalb nur als Option.
- **Entscheidung des Anwenders: ☐**

---

## 11. Wärmequelle Erdreich

### EQ1 Prüfergebnisse werden nicht gespeichert

- **Stand heute:** Die Ergebnisse der Prüfung nach VDI 4640 liegen prozessweit je Projekt im
  Speicher („letzter Lauf gewinnt“) und werden nicht persistiert
  (`EPOS.Kern/Allgemein/Simulation/ErdreichAuswertung.cs:62-67`, `:206-211`). Nach einem
  Programmstart zeigt der Dialog „noch kein Simulationslauf“.
- **Frage:** Speichern?
- **Vorschlag:** Mit dem Simulationsergebnis speichern: Tabelle `Tab_ErgebnisErdreich`
  (`ID_Projekt`, `ID_Anlage`, Prüfzeile, Istwert, Grenzwert, Einheit, Grundlage, Hinweis,
  Laufstempel), geschrieben dort, wo die übrigen `Tab_Ergebnis*` geschrieben werden; der Dialog
  liest sie, wenn kein frischer Lauf vorliegt, mit „Stand des Laufs vom …“. Auch der Bericht
  kann die Prüfung dann ohne neuen Lauf zeigen.
- **Empfehlung: ja**, Aufwand S–M (Schemaschritt). Der Planer braucht den Nachweis im Bericht und
  nach einem Neustart. Kein Rechenweg betroffen → Basis unberührt.
- **Entscheidung des Anwenders: ☐**

---

## 12. Programm-Update und Katalog

### KU1 Ein Update lässt den Katalog, wie er ist

- **Stand heute:** Setup und Programmstart überschreiben keinen Katalogsatz und säen keinen nach
  (Hilfeseite Brauchwasser, Grenzen). Die Datenbank kommt nur bei der **Neuinstallation** aus der
  Vorlage (`EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs`: liegt am Ziel schon eine
  Datenbank, wird nichts angefasst). Neue Katalogsätze kommen heute nur über **Saat-Schritte** der
  Schemamigration (etwa `EPOS.Kern/Allgemein/Update/GebaeudeSaat.cs`,
  `KonditionierungsvorlagenSaat.cs`) — je Fall von Hand programmiert — oder über den
  **Katalogimport** von Paketen (`EPOS.Kern/Allgemein/Import/KatalogImportAblauf.cs`; beim
  Zapfprofil mit Katalogversion, `EPOS.Kern/Controller/TwwPaketteilCtrl.cs:15-19`, Statuszeile
  **#615**). Die Auslieferungsvorlage baut `Werkzeuge/Auslieferungsvorlage`.
- **Frage:** Stammdaten updatefähig, eigene Datensätze bleiben.
- **Vorschlag:** **Katalogabgleich mit Katalogfassung.**
  1. **Kennung je ausgelieferter Zeile:** Jede `_STAMM`-Tabelle bekommt `Katalog_Schluessel`
     (stabile Textkennung, über Fassungen gleich) und `Katalog_Pruefsumme` (Prüfsumme der
     ausgelieferten Werte). Eine Anwenderzeile hat keinen Schlüssel. Schemaschritt; die Saat
     belegt die Schlüssel der heutigen ausgelieferten Sätze (`ReadOnly = 1`).
  2. **Katalogpaket je Programmfassung:** `Werkzeuge/Auslieferungsvorlage` schreibt neben der
     Vorlage ein Paket (alle ausgelieferten Sätze mit Schlüssel, Prüfsumme, Fassung) — dasselbe
     Format wie der vorhandene Katalogimport.
  3. **Abgleich nach der Schemamigration** (beim ersten Start einer neuen Fassung, nach
     automatischer Sicherung per `VACUUM INTO`), je Satz des Pakets:
     - Schlüssel fehlt in der Datenbank → **einfügen** (`ReadOnly = 1`);
     - Schlüssel da, Prüfsumme der Zeile = alte ausgelieferte Prüfsumme (der Anwender hat nichts
       geändert) → **aktualisieren**;
     - Schlüssel da, Zeile vom Anwender geändert oder entsperrt → **behalten**, im Bericht als
       „Ihre Anpassung bleibt; der neue Auslieferungsstand liegt als Vergleich vor“ nennen;
     - Satz in der neuen Fassung entfallen → **nicht löschen**, als „ausgelaufen“ kennzeichnen.

     Anwenderzeilen (ohne Schlüssel) und alle **Projektkopien** (`Tab_*` mit `ID_Projekt`)
     werden nie angefasst — Projekte rechnen also nach dem Update wie vorher.
  4. **Bedienung:** Bericht nach dem Abgleich („n neu, m aktualisiert, k behalten“), dazu
     Administration → „Katalog aktualisieren…“ mit „Nur prüfen“ (wie beim Katalogimport) und
     „Auslieferungsstand eines Satzes wiederherstellen“.
- **Empfehlung: ja**, Aufwand L (über alle Kataloge), in Stufen: zuerst die Kataloge mit
  laufender Pflege (Wärmepumpen, Kessel, PV-Module, Brauchwasser- und Prozessprofile nach PW5),
  dann die übrigen. Großer Nutzen für jede Auslieferung — heute bekommt ein Bestandskunde neue
  Katalogsätze nur durch Neuinstallation. **Basis:** Der Abgleich ändert keine Projektkopie; die
  Testdatenbank wird aber nur mit Zustimmung abgeglichen, weil die Einfrierregeln Katalogzeilen
  nennen, die Referenzprojekte benutzen (`Tab_Tww*_STAMM`, Brennstoff- und Emissionswerte).
- **Entscheidung des Anwenders: ☐**

---

## Fragliches

Beim Nachlesen fiel auf (keine Modellfrage, sondern Pflege oder Fehler):

1. **Hilfeseite Heizkessel veraltet:** `Berechnung/Heizkessel.wiki` Z. 311–317 nennt „kein
   Teillastwirkungsgrad“, „keine Taktung“, „keine Brennwertrechnung als eigener Weg“ — gebaut mit
   #625–#635. Seite nachziehen (Wiki-Sammel-Upload).
2. **Hilfeseite Strombedarf veraltet:** „Kein Projektfilter im Stromzweig“ (Z. 217) nach
   #641/#643 nicht mehr zutreffend; „Die Peak-Shaving-Maske schreibt nicht“ (Z. 208, 219)
   übersieht den Knopf „in die Variante übernehmen“ (SB3).
3. **PW6 ist ein Fehler:** Der Abbruch der Profilschleife hinterlässt eine reihenfolgeabhängige
   Teilsumme, die der Wärmezweig still weiterrechnet (`ProfilBedarf.cs:678-686`,
   `SimulationWaermebedarf.cs:1293`, `:1378`); die Hilfeseite beschreibt einen echten Abbruch.
4. **Albedo als Literal:** `SolarPVGISCalculator.cs:455` und `:599` rechnen mit `0.2` statt
   `ALBEDO_BODEN` (`:333`) — bei PV4 mitziehen.
5. **Bezugsfläche der Kollektorkennwerte — geprüft (Welle M2):** Die Kollektorsätze der
   Testdatenbank führen keine Modulfläche (nur ein Prüfsatz); ihre Kennwerte beziehen sich auf die
   Aperturfläche (VDI-3805-Feld 11). In den VDI-Quellen unter `VDI-3805-Daten/` nennen zwei
   Hersteller Feld 11 als Apertur, einer als Absorberfläche (zwischen Apertur und Brutto, als
   Apertur behandelt); keine Quelle bezieht auf die Bruttofläche. Apertur/Brutto bei
   Flachkollektoren 0,86–0,94, bei Röhren bis 0,38. Der Kommentar in `SimulationSolarthermie`
   ist berichtigt.
6. **Zusammengesetzte SQL-Texte** im PV-Zweig (`SimulationPV.cs:166`,
   `SimulationControl.cs:4705`) entgegen der Regel „`?`-Parameter“ der `CLAUDE.md` — bei der
   nächsten Arbeit an der Stelle umstellen.
7. **BHKW-Grenzleistungen der Testdatenbank** von 468 bis 1 027 % (Folgeauftrag 6, Punkt 4) —
   vor BH2 klären.

---

## Übersichtstafel

Geordnet nach Nutzen für die Planungsaussage (oben am größten). Aufwand S ≤ 2 PT, M ≤ 8 PT, L
darüber. „Basis betroffen“: ob die Referenzbasis neu einzufrieren ist, wenn das Neue für ein
Referenzprojekt gilt; „Option“ heißt Vorgabe = heutiges Verhalten, die Basis bleibt bis zum
Umstellen eines Referenzprojekts.

| Punkt | Empfehlung | Aufwand | Basis betroffen | Entscheidung |
|---|---|---|---|---|
| ST2 Arbeitstemperatur aus dem Speicher (mit ST4 Grädigkeit) | umgesetzt (M2) | M | ja, 1049 (R32) | ☑ |
| PW1 Temperaturniveau je Prozess (Stufe 1) | ja | M | nein (Option) | ☐ |
| BW4 Netzverluste je Kanal, Zirkulation im Bestandsweg | ja | M | nein (Option) | ☐ |
| SB1 (a) PV-Bilanz im Viertelstundenraster | ja | M | ja, alle Referenzprojekte mit PV | ☐ |
| KU1 Katalogabgleich mit Katalogfassung | ja | L | nein (Projektkopien unberührt) | ☐ |
| PW2 Kalenderschicht für alle Profile | ja | M | nein (Option) | ☐ |
| WP1 Taktverlust nach EN 14825, Starts | ja | M | nein (Option) | ☐ |
| BH1 BHKW-Teillastkennlinie | ja | M | nein (Option) | ☐ |
| BH2 BHKW-Takten mit Folgeauftrag 6 | ja | M | ja, wenn die Untergrenze in Referenzprojekten wirksam wird | ☐ |
| PV3 Einspeisebegrenzung mit Abregelung | ja | M | nein (Option) | ☐ |
| ST5 Diffus-IAM mit K_dfu | umgesetzt (M2) | S | nein (1049 führt kein K_dfu) | ☑ |
| PS1 (c) Bereitschaftsverlust temperaturabhängig | ja | S–M | ja für Pufferprojekte (als Option erst beim Umstellen) | ☐ |
| PW6 Profil ohne Typ überspringen | ja | S | nein (durch Referenzlauf zu bestätigen) | ☐ |
| PW5 Katalog typischer Betriebsweisen | ja | S | nein | ☐ |
| SP1 Standby des Speichersystems | ja | S–M | nein (Option) | ☐ |
| EQ1 Erdreichprüfung speichern | ja | S–M | nein | ☐ |
| ST1 Pumpenstrom Solarkreis | umgesetzt (M2) | S | nein (Option) | ☑ |
| ST6 Bezugsfläche der Kennwerte | umgesetzt (M2) | S | nein (Option) | ☑ |
| ST3 Stufe 1 Solarkreisverluste als Feld | umgesetzt (M2) | S | nein (Vorgabe 8 %) | ☑ |
| PV4 Albedo einstellbar | ja | S | nein (Vorgabe 0,2) | ☐ |
| SP2 Beste Rastervariante übernehmen | ja | S | nein | ☐ |
| BW3 Brauchwasser-Vorlauf je Erzeuger | später | M | nein (Option) | ☐ |
| BW5 Thermische Desinfektion | später | M | nein (Option) | ☐ |
| PW3 Wochenprofil je Monat oder Saison | später | M | nein (Option) | ☐ |
| PW4 Verluste je Prozesssatz | später (Kanalwert über BW4) | S | nein | ☐ |
| WP3 Abtaufaktor bei Kennfeldern ohne Abtauung | später | M | nein | ☐ |
| PS2 Abwärme als Lader des Quellspeichers | später | M | nein | ☐ |
| PS5 (a) Frischwassermodul | später | M | nein | ☐ |
| PS1 (a) Zonenanteile wählbar | später | S | nein (Option) | ☐ |
| ST3 Stufe 2 Leitungsverlust physikalisch | später | S | ja, 1049 | ☐ |
| SB1 (b) Stromprofile mit 672 Werten | später | M | nein | ☐ |
| SP6 Alterung in der Wirtschaftlichkeit | später | M–L | nein (Wirtschaftlichkeit) | ☐ |
| SP8 Leistungspreis auf Jahres- oder Monatsspitze | später | M | nein | ☐ |
| SP3 Feinsuche um den besten Rasterpunkt | später | S | nein | ☐ |
| SP10 Periodischer Start-SOC | später | S | ja, 1046 (nur als Option) | ☐ |
| PV2 Klemmung je MPP-Eingang | später | S | nein | ☐ |
| SP5 .NET-Planer für iOS | später | L | nein | ☐ |
| HK4 Übertrager zwischen Puffer und Kessel | nein (Hinweistext) | S | nein | ☐ |
| PS1 (b) Einströmmischung | nein | — | — | ☐ |
| PS4 Mehr Katalogwerte Puffer | nein (außer mit PS1 (c)) | — | — | ☐ |
| PS5 (b) Übertrager im Puffer | nein | — | — | ☐ |
| PV1 Ein-Dioden-Modell | nein | — | — | ☐ |
| SB1 (c) Klimadaten in 10 oder 15 Minuten | nein | — | — | ☐ |
| SP7 Herkunftsschichten | nein | — | — | ☐ |
| SP9 Wärme und Strom gemeinsam optimieren | nein (Konzept vormerken) | L | — | ☐ |
| WP2, BH3, PS3, SP4, HK2, HK3, SB3 | Erläuterung, nichts ändern | — | — | ☐ |
| ST7, BW1, BW6, HK1, SB2 | erledigt | — | — | Kenntnis |
| ST8 Solarthermie-Ganglinie als Rechenweg | umgesetzt (Folgeauftrag 4) | — | nein (kein Referenzprojekt führt eine Ganglinie) | ☑ |

---

## Reihenfolge der Umsetzung in Wellen

Grundsatz: Je Welle höchstens **eine** neu eingefrorene Basis; Optionen mit Vorgabe = heute
zuerst, Rechenwegänderungen an Referenzprojekten gebündelt.

| Welle | Inhalt | Basis | Begründung |
|---|---|---|---|
| **M1 Kleinigkeiten und Fehler** | PW6, PV4 (samt Literalen), ST1, ST3 Stufe 1, ST6, SP2, Hilfeseiten nach „Fragliches“ 1–2 | unberührt | je S, sofortiger Nutzen, keine Basis — guter Einstieg |
| **M2 Solarthermie** | ST5, ST2 mit ST4, danach ST8 (Folgeauftrag 4) | **neu** (1049), einmal für alle drei | alle Änderungen am selben Referenzprojekt in einem Schritt |
| **M3 Bedarf** | BW4, PW1 Stufe 1, PW2, PW5 | unberührt (Optionen) | gemeinsame Profilroutine und Kanäle; PW5 liefert die Sätze, mit denen PW1 und PW2 sofort sichtbar werden |
| **M4 Erzeuger in Teillast** | WP1, BH1, BH2 mit Folgeauftrag 6 | neu nur, wenn die BHKW-Untergrenze in Referenzprojekten wirksam wird | dieselbe Bauart wie Kessel E2/E4, Tests und Editor wiederverwendbar |
| **M5 Strom in Viertelstunden** | SB1 (a), PV3, SP1 | **neu** (Referenzprojekte mit PV) | PV-Bilanz, Abregelung und Standby greifen in dieselbe Viertelstundenbilanz |
| **M6 Katalog-Update** | KU1 Stufe 1 (laufend gepflegte Kataloge), EQ1 | unberührt | Schema und Werkzeug, eigene Abnahme mit einer Bestandsdatenbank des Anwenders |
| **M7 Speicher** | PS1 (c), danach nach Bedarf PS1 (a), PS5 (a), BW5 | neu für Pufferprojekte, falls als Vorgabe gesetzt | zusammen mit der Pufferauslegung (Recherchen unter `Pufferspeicher/`) |
| später | BW3, PW3, PW4 je Prozess, WP3, PS2, SB1 (b), SP3, SP6, SP8, SP10, PV2, SP5 | — | nach Anlass und Rückmeldung der Anwender |

Nach dem Entscheid wird je Welle ein Auftrag mit Abnahme (Build, Tests, Referenzlauf) formuliert;
die Entscheidungen werden in der Statusdatei je Welle als Zeile geführt.
