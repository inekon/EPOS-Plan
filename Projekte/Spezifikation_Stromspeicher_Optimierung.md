# Simulation mehrerer Stromspeicher

## Grundlagen und Algorithmen für die Softwareumsetzung

Version 1.4 | 11. September 2026 | Fachliche und technische Spezifikation

Dieses Dokument definiert eine Offline-Simulation für einen Standort mit Stromverbrauch, optionaler Photovoltaik und mehreren Batteriespeichern. Die Software soll Speichergrößen, Betriebsstrategien und die Verteilung von Lade- und Entladeleistung vergleichen. Maßstab für die wirtschaftliche Auswahl ist der Kapitalwert gegenüber demselben Standort ohne die untersuchten Speicher.

Der Entwurf trennt Datenbeschaffung, Prognose, Fahrplanoptimierung, physikalische Ausführung und Abrechnung. Dadurch lassen sich Strategien austauschen, ohne die Energiebilanz oder die Wirtschaftlichkeitsrechnung zu verändern. Der mitgelieferte Python-Code setzt den definierten Basiskern einschließlich Downloads und einer gemischt-ganzzahligen Optimierung um.

Kapitel 13 beschreibt die Übertragung auf EPOS-Plan. Kapitel 14 ergänzt den Dialog für Kostenprofile, speicherbare Größenbereiche und frei zuordenbare CSV-Zeitreihen. Kapitel 13 bewahrt den ursprünglichen Integrationsvorschlag; Kapitel 14 dokumentiert die nun implementierten Kosten-, Auslegungs- und Importfunktionen und ihre Grenzen.

Die Zahlen in der Beispielkonfiguration und die drei Tage Beispieldaten sind ausschließlich Funktionsbeispiele. Eine konkrete Auslegung und ein belastbarer Kapitalwert entstehen erst mit dem standorteigenen Jahreslastgang, den tatsächlichen Speicherdaten und dem gültigen Tarif.

## 1 Ziel und verbindlicher Modellumfang

### 1.1 Zu beantwortende Fragen

Die Simulation beantwortet, welche Kombination aus Anzahl, Kapazität, Ladeleistung, Entladeleistung und Betriebsstrategie die höchste wirtschaftliche Verbesserung erzielt. Sie zeigt außerdem, welche Lastspitzen verbleiben, wie viel Energie verloren geht und wie unterschiedlich die Speicher beansprucht werden.

Ein positives Ergebnis ist keine Eigenschaft einer Strategie allein. Es hängt vom Lastgang, von PV, Tarifen, Netzgrenzen, Speichertechnik, Investitionskosten und vom Informationsstand des Reglers ab. Deshalb werden die Variante ohne Speicher und sämtliche Speicherlösungen mit denselben exogenen Daten ausgewertet.

### 1.2 Systemgrenze

Das Basismodell verwendet einen gemeinsamen AC-Knoten am Standort. Jeder Speicher verfügt über einen unabhängig steuerbaren bidirektionalen Leistungspfad. Die PV-Eingangsreihe beschreibt die verfügbare AC-Leistung nach dem PV-Wechselrichter und vor einer zusätzlichen Abregelung am Netzanschlusspunkt. Die Lastreihe beschreibt den Bruttoverbrauch ohne den hier simulierten Speicherverbrauch.

Ein bereits saldierter Netzlastgang darf nicht nochmals um PV vermindert werden. Bei bestehenden Speichern muss zunächst der Verbrauch ohne deren Einfluss rekonstruiert werden. Allgemein gilt: Bruttolast = Netzleistung + PV-Leistung + bestehende Batterieentladung - bestehende Batterieladung - separat erfasste Hilfsleistung; sämtliche Größen müssen sich auf denselben Messpunkt beziehen.

Das Raster beträgt 15 Minuten. Jeder Leistungswert ist der Mittelwert seines Intervalls. Die Simulation berechnet Energie- und Abrechnungswirkungen in diesem Raster. Sekundenspitzen, Schutztechnik, Inselnetzstabilität und Regelenergiebereitstellung erfordern gesonderte Modelle und werden aus Viertelstundenmittelwerten nicht abgeleitet.

### 1.3 Basiskern und Erweiterungen

| Funktion | Mitgelieferter Basiskern | Erweiterung für eine Anlagenstudie |
|---|---|---|
| Mehrere Speicher | Eigene Kapazitäten, Leistungen, SoC und Wirkungsgrade | Temperatur, Verfügbarkeit und Betriebszustände je Intervall |
| Physik | Konstante Wirkungsgrade und konstante AC-Hilfsleistung | Gemessene Kennfelder, Selbstentladung, Standby und Einschaltverluste |
| Strategien | Peak Shaving, PV Greedy, PV Planung, Arbitrage, Multi Use | Standortabhängige Zielfunktionen und Reserveprognosen |
| Alterung | Rainflow-Auswertung mit vorgegebener Lebensdauerkurve | Kalibrierte Kapazitäts- und Leistungsalterung im Lebenszyklus |
| Wirtschaftlichkeit | Eine Abrechnungsperiode und Kapitalwert aus Jahreskonten | Mehrperiodentarife, Ersatzinvestitionen und jährliche Neusimulation |
| Netzkopplung | AC-Knoten und Anschlussgrenzen | Gemeinsamer Hybridwechselrichter oder detailliertes DC-Netz |

Die rechte Spalte ist ein definierter Ausbauumfang. Die Referenzdateien enthalten dafür keine verdeckten Ersatzannahmen. Insbesondere aktualisiert der Basiskern die Speicherkapazität während eines Simulationslaufs noch nicht anhand von Alterung.

## 2 Fachliche Grundlagen und Korrekturen

### 2.1 Zwei verschiedene Arten von Strategie

Die Betriebsstrategie entscheidet über Zeitpunkt und Gesamtleistung: etwa Lastspitzen kappen, PV-Energie verschieben oder teure Strombezüge vermeiden. Die Verteilstrategie entscheidet, welcher Speicher welchen Teil dieser Leistung übernimmt. Beide Entscheidungen werden getrennt parametrisiert.

Eine kaskadierte Leistungsaufteilung kann beispielsweise mit Peak Shaving oder mit Arbitrage kombiniert werden. Eine kapazitätsproportionale Aufteilung ist dagegen noch keine wirtschaftliche Optimierung. Bei unterschiedlichen Anfangs-SoC führt sie auch nicht automatisch zu gleichen Ladezuständen.

### 2.2 Wesentliche Bereinigungen der Ausgangsvorlage

| Aussage oder Ansatz der Ausgangsvorlage | Verbindliche Behandlung |
|---|---|
| 90 Prozent Round-Trip-Wirkungsgrad für Laden und Entladen verwenden | Getrennte Richtungswirkungsgrade; ihr Produkt ist der Round-Trip-Wert |
| Peak Shaving anhand der Bruttolast | Maßgeblich ist die Netzleistung einschließlich PV, Speicher und Hilfsverbrauch |
| Entladung stets als Stromverkauf bewerten | Eigenversorgung vermeidet Bezug; nur der Überschuss am Netzpunkt ist Einspeisung |
| PV-Ersparnis und Arbitragegewinn zusätzlich addieren | Ein gemeinsames Rechnungskonto verhindert Doppelzählung |
| Ein verfehltes Peak-Intervall macht alle Einsparungen zunichte | Entscheidend ist das tatsächlich verbleibende Abrechnungsmaximum |
| Immer 96 Preiswerte je Tag | Zeitstempel bestimmen die Länge; Zeitumstellung ergibt 92 oder 100 Viertelstunden |
| AC-Wechselrichter trennen immer galvanisch | Eine galvanische Trennung hängt vom konkreten Gerät ab |
| Direkte DC-Parallelschaltung verlangt immer DC/DC-Steller | Zulässigkeit und Stromaufteilung ergeben sich aus der freigegebenen Systemtopologie |
| Der höchste Wirkungsgrad liegt immer bei Volllast | Kennlinien entscheiden; Pauschalaussagen ersetzen keine Messdaten |
| LCOS sind identisch mit Grenzverschleißkosten | Projektkennzahl und Kosten einer zusätzlichen Nutzung getrennt behandeln |
| Eine Reihenfolge nach PV-Spitzen ist bereits eine Prognoseoptimierung | SoC-Zustände, Entladungen und Grenzen müssen chronologisch gekoppelt bleiben |

Wirkungsgrad und Eigenverbrauch hängen unter anderem vom Leistungsbereich ab. Die Messungen der HTW Berlin zeigen, warum Teillastverhalten und Energiemanagement nicht durch einen einzigen Spitzenwirkungsgrad beschrieben werden können. Für einen belastbaren Vergleich von Parallel- und Kaskadenbetrieb sind daher Leistungskennfelder und Betriebsverluste erforderlich. [HTW Berlin, Stromspeicher-Inspektion 2025](https://www.htw-berlin.de/einrichtungen/zentrale-referate/kommunikation/pressemitteilungen/stromspeicher-inspektion-2025-neue-wirkungsgradrekorde-und-erstmals-energiemanagement-test-fuer-heimspeicher/)

## 3 Datenvertrag und Downloadquellen

### 3.1 Gemeinsames Zeitraster

Jede Zeile repräsentiert das halboffene Intervall [timestamp, timestamp + 15 Minuten). Intern werden Zeitzonen in UTC umgerechnet. Die Anzeige und die Zuordnung zu Abrechnungsperioden erfolgen mit der Standortzeitzone Europe/Berlin. Zeitstempel ohne UTC-Offset werden abgewiesen.

Ein vollständiges normales Jahr enthält 35.040 Intervalle, ein Schaltjahr 35.136. Lokale Tage können 92, 96 oder 100 Intervalle enthalten. Die lokale Abrechnungsjahresgrenze wird zuerst mit der Zeitzone gebildet und anschließend nach UTC umgerechnet. Eine Division durch 96 ersetzt diese Zuordnung nicht.

| Eingabefeld | Einheit oder Typ | Bedeutung |
|---|---|---|
| timestamp | ISO 8601 mit Offset | Beginn des Intervalls |
| load_kw | kW, nicht negativ | Bruttolast des Standorts |
| pv_kw | kW AC, nicht negativ | Verfügbare PV-Leistung vor Netzabregelung |
| buy_eur_kwh | EUR/kWh, auch negativ | Effektiver variabler Bezugspreis |
| sell_eur_kwh | EUR/kWh, auch negativ | Effektiver Erlös oder Aufwand der Einspeisung |
| decision_time | ISO 8601, nur Prognosedatei | Zeitpunkt der Fahrplanentscheidung |
| known_at | ISO 8601, nur Prognosedatei | Frühester belegter Verfügbarkeitszeitpunkt dieser Eingabe |

Der kanonische CSV-Vertrag des Python-Kerns verwendet Komma als Feldtrenner und Punkt als Dezimalzeichen. Der in Kapitel 14 spezifizierte EPOS-Import akzeptiert auch andere Trennzeichen, Dezimalkomma und frei wählbare Zeit- und Wertspalten. Er normiert getrennte Quelldateien auf die internen EPOS-Reihen; der Python-Vertrag bleibt separat verwendbar. kWh je Viertelstunde werden durch 0,25 h geteilt, um mittlere kW zu erhalten. EUR/MWh werden durch 1.000 geteilt, um EUR/kWh zu erhalten. Prozentwerte der Konfiguration werden als Bruchteile zwischen 0 und 1 gespeichert.

### 3.2 Qualitätsregeln

Pflichtwerte müssen endlich und vollständig sein. Doppelte, unsortierte oder fehlende Intervalle führen zum Abbruch der Datenaufbereitung. Ein fehlender Preis ist nicht null. Eine fehlende Messung ist nicht gleichbedeutend mit keinem Verbrauch. Für eine Studie mit reparierten Daten werden Reparaturverfahren und betroffene Intervalle gesondert gespeichert; die Originaldaten bleiben erhalten.

Leistungsmittelwerte werden durch zeitgewichtete Integration auf das Zielraster überführt. Ein stündlicher Tarif wird in seinen vier Viertelstunden unverändert angesetzt. Er wird nicht durch vier geteilt. Diese Übernahme erzeugt keine zusätzliche Preisinformation. Momentanwerte einer PV-Prognose dürfen nicht ohne Definition ihrer Zeitbedeutung als Intervallmittelwerte verwendet werden.

### 3.3 Quellen im mitgelieferten Code

| Daten | Ausführbarer Adapter | Herkunft und Verwendung |
|---|---|---|
| Standortlast | prepare_inputs.read_series | Lokaler Zählerexport; kein öffentlicher Ersatzlastgang |
| Historische Börsenpreise DE-LU | downloads.download_prices | Energy-Charts über /price; Rohpreise in EUR/MWh |
| Historische modellierte PV | downloads.download_pv_history | Open-Meteo Historical Forecast; Einstrahlung wird in AC-Leistung umgerechnet |
| Aktuelle PV-Prognose | downloads.download_pv_forecast | Forecast.Solar; Originalprognose mit Abrufzeit und Watt-Stützstellen |

Die Energy-Charts-API liefert Zeitstempel am Intervallanfang und nennt die Einheiten sowie Lizenzinformationen in der Antwort. Der Adapter speichert diese Angaben. Bei DE-LU nennt die API Bundesnetzagentur und SMARD.de als Datenquelle. HTTP 429 und Retry-After werden berücksichtigt. [Energy-Charts API](https://api.energy-charts.info/openapi.json)

Seit dem ersten Liefertag 1. Oktober 2025 verwendet der gekoppelte Day-Ahead-Markt Viertelstundenprodukte. Der Code prüft deshalb für die hier festgelegte Preiszone DE-LU das jeweilige historische Intervall und vervierfacht nicht pauschal jede Antwort. [EPEX SPOT, Jahresergebnisse 2025](https://www.epexspot.com/sites/default/files/download_center_files/2026-01-19_EPEX%20SPOT_Annual%20Power%20Trading%20Results%202025_final_0.pdf)

Die historische Open-Meteo-Reihe ist eine aus archivierten Wettermodellen zusammengesetzte Zeitreihe. Sie ist kein unverändert erhaltener damaliger Tagesfahrplan. Die stündliche Einstrahlung bezieht sich auf die vorausgehende Stunde; der Adapter verschiebt die Intervallgrenze entsprechend. [Open-Meteo Historical Forecast API](https://open-meteo.com/en/docs/historical-forecast-api)

Für die PV-Referenz wird eine bewusst einfache Umrechnung verwendet:

```text
PV_AC = min(P_Wechselrichter, P_STC * GTI / 1000 * PR)
```

GTI ist die Einstrahlung auf die geneigte Fläche in W/m², P_STC die installierte Leistung in kWp und PR ein vorgegebener Leistungsfaktor. Eine Anlage mit mehreren Dachflächen wird flächenweise gerechnet und anschließend AC-seitig zusammengeführt. Verschattung, Temperatur und detailliertes Clipping sind mit diesem Modell nur über die gewählten Parameter angenähert. Für belastbare Studien haben gemessene PV-Daten oder ein kalibriertes PV-Modell Vorrang.

Forecast.Solar wird mit Standort, Neigung, Azimut und Anlagenleistung angesprochen. Die Nutzungsbedingungen und der Leistungsumfang richten sich nach dem verwendeten Zugang. Der Code gibt die Prognose zunächst als eindeutig bezeichnete Stützstellen aus und erhält die gesamte Originalantwort. Dieser Export wird vor einer direkten Simulation noch nach der Zeit- und Energiedefinition des Anbieters in Intervallmittelwerte umgewandelt. [Forecast.Solar](https://forecast.solar/)

ENTSO-E ist eine alternative Quelle für Day-Ahead-Preise. Dafür kann ein zusätzlicher Adapter mit Zugangstoken und Auswertung der Periodenauflösung ergänzt werden. Dieser Adapter ist nicht Teil des geprüften Codes; der enthaltene Preisdownload benötigt keinen solchen Token.

### 3.4 Wiederholbarkeit und Informationsstand

Jeder Download speichert URL, Abrufzeit, Originalantwort und SHA-256-Prüfsumme. Ein vorhandener Snapshot wird ohne erneuten Netzabruf wiederverwendet. Die Option --refresh erzeugt einen neuen Snapshot. Ein fertiger Simulationslauf arbeitet nur mit lokalen Dateien und benötigt keine Live-Verbindung.

Für ein realistisches historisches Prognoseexperiment muss jede Prognose mit ihrem damaligen Informationsstand vorliegen. Der Optimierer darf nur Zeilen mit known_at kleiner oder gleich decision_time sehen. Ein heute heruntergeladener historischer Preis hat dadurch noch keinen belegten historischen Veröffentlichungszeitpunkt. Dieser wird aus archivierten Veröffentlichungen oder einem ausdrücklich dokumentierten Veröffentlichungsmodell ergänzt.

Historische Last- und PV-Istwerte dürfen nur im Modus --oracle als zukünftige Eingaben dienen. Dieser Modus liefert eine optimistische Vergleichsgrenze. Für damalige Wetterprognosen sind beispielsweise einzeln archivierte Modellläufe erforderlich. Die Previous-Runs-Schnittstelle bietet festgelegte Vorlaufzeiten; für vollständige einzelne Läufe verweist Open-Meteo auf die Single-Runs-Schnittstelle. [Open-Meteo Previous Runs API](https://open-meteo.com/en/docs/previous-runs-api)

## 4 Physikalisches Modell

### 4.1 Größen und Vorzeichen

| Symbol | Einheit | Definition |
|---|---|---|
| t und i | Index | Zeitintervall und Speicher |
| dt | h | 0,25 im Basismodell |
| L und PV | kW | Bruttolast und verfügbare AC-PV-Leistung |
| c_i und d_i | kW AC | Nicht negative Lade- und Entladeleistung |
| p_i | kW AC | d_i minus c_i; positiv bedeutet Entladen |
| E_i | kWh | Energie im Speicher vor dem Intervall |
| C_i | kWh | Verfügbare Vollbereichskapazität bei aktuellem SoH |
| eta_c und eta_d | Bruchteil | Lade- und Entladewirkungsgrad |
| A | kW AC | Separat modellierte Hilfsleistung |
| K | kW AC | Abgeregelte verfügbare PV-Leistung |
| G | kW | Netzleistung; positiv Bezug, negativ Einspeisung |

Wenn ein Hersteller nur die nutzbare Kapazität innerhalb seines eigenen SoC-Fensters nennt, wird diese nicht nochmals um dasselbe Fenster reduziert. Entweder wird die zugrunde liegende Vollbereichskapazität rekonstruiert oder ein konsistentes effektives Fenster von 0 bis 1 verwendet.

### 4.2 Energie und Leistungsbilanz

```text
E_i[t+1] = E_i[t] + dt * (eta_c_i*c_i[t] - d_i[t]/eta_d_i)
G[t] = L[t] - PV[t] + K[t] + A[t] + sum(c_i[t]-d_i[t])
Import[t] = max(G[t], 0)
Export[t] = max(-G[t], 0)
eta_RT_i = eta_c_i * eta_d_i
```

Die Wirkungsgrade beziehen sich auf denselben AC-Speicher-AC-Pfad. Ist nur ein Round-Trip-Wert bekannt, ist eta_c = eta_d = sqrt(eta_RT) eine dokumentierte Näherung. Bei 90 Prozent ergibt das rund 94,868 Prozent je Richtung. Zwei Richtungswerte von je 90 Prozent ergäben dagegen nur 81 Prozent im Rundlauf. NREL SAM unterscheidet ebenfalls AC/DC- und DC/AC-Umwandlung sowie zusätzliche Hilfsverluste. [NREL SAM, Batteriespeicher und Umwandlung](https://samrepo.nrelcloud.org/help/battery_storage_fom.html)

Die Umwandlungsverluste pro Intervall lauten:

```text
Verlust_i = dt * ((1-eta_c_i)*c_i + (1/eta_d_i-1)*d_i)
Delta_E_i = dt*(c_i-d_i) - Verlust_i
```

Hilfsenergie A*dt ist separat zu berücksichtigen. Eine Wirkungsgradkurve, in der dieser Verbrauch bereits enthalten ist, darf nicht nochmals mit demselben Hilfsverbrauch belastet werden.

### 4.3 Grenzen je Speicher

```text
E_min_i = soc_min_i * C_i
E_max_i = soc_max_i * C_i
c_max_i = min(P_c_i, (E_max_i-E_i)/(eta_c_i*dt))
d_max_i = min(P_d_i, (E_i-E_floor_i)*eta_d_i/dt)
```

Negative Restmöglichkeiten werden auf null begrenzt. E_floor ist im Normalbetrieb E_min plus geschützte Peak-Reserve. Bei einer tatsächlichen Peak-Anforderung kann der Ausführungsregler diese zusätzliche Reserve bis E_min freigeben. Die technische Mindestenergie wird dabei weiter eingehalten.

Ein Sollwert wird erst durch die physikalischen Grenzen zum Istwert. Der Zustand wird ausschließlich mit dem tatsächlich ausgeführten Istwert fortgeschrieben. Ein nachträgliches pauschales Abschneiden des SoC würde Energie erzeugen oder vernichten und ist unzulässig; kleine numerische Rundungsabweichungen werden lediglich innerhalb der Prüftoleranz akzeptiert.

### 4.4 Grenzen am gemeinsamen Anschluss

Netzbezug und Einspeisung haben eigene Leistungsgrenzen. Netzladung und Einspeisung aus der Batterie besitzen getrennte Freigaben. Eine fehlende Freigabe für Batterieexport verhindert keine zulässige direkte PV-Einspeisung. Ohne Freigabe für Netzladung darf nur der verbleibende PV-Überschuss geladen werden.

Der Basiskern versorgt zunächst die aktuelle Last aus PV. Freiwillige wirtschaftliche Abregelung betrifft daher nur PV-Überschüsse. Eine Strategie, die bei negativen Bezugspreisen auch lastdeckende PV abschaltet, wäre eine gesondert zu aktivierende Modellerweiterung.

Kann ein Speicher einen Netzbezugsgrenzwert nicht einhalten, wird die verbleibende Überschreitung ausgegeben. Sie wird weder als erfolgreich gekappte Spitze verbucht noch durch einen unsichtbaren Lastabwurf beseitigt. Eine technische Anschlussgrenze mit Überschreitung macht die betreffende Variante unzulässig. Ein wirtschaftlicher Peak-Zielwert kann hingegen verfehlt werden; abgerechnet wird der reale Restpeak.

## 5 Betriebsstrategien und Leistungsaufteilung

### 5.1 Peak Shaving mit Wiederaufladung

Die Regel verwendet die Netzlast ohne Batterie N = L - PV + A und einen Zielwert H. Sie muss sich an N statt an der Bruttolast orientieren.

```text
wenn N > H:
    Gesamtentladung anfordern = N - H
    zusätzliche Peak-Reserve freigeben
sonst:
    Gesamtladung anfordern = H - N
    durch freien Speicherraum und Ladefreigabe begrenzen
Leistung verteilen, physikalisch ausführen, Restpeak protokollieren
```

Die Wiederaufladung nutzt freie Anschlussleistung unter H. Sie darf keinen neuen Peak über H erzeugen. Das ist eine reaktive Referenzstrategie. Sie lädt auch dann nach, wenn kurz darauf PV erwartet wird; diesen wirtschaftlichen Nachteil kann eine Prognoseplanung vermeiden.

**Der Fall N dauerhaft über H** (ergänzt in Version 1.3, Befund SP‑O‑10 vom 11.09.2026). Die Regel oben behandelt ihn nicht; sie sagt nur „Restpeak protokollieren". Liegt der Zielwert H unter jedem Wert der Netzlast, entsteht daraus eine **arbeitslose Flotte**, und zwar zwangsläufig: Die Anforderung `N - H` ist in jedem Intervall positiv, es wird also nie eine Ladung angefordert, und der Ladedeckel `max(0, H - N)` der Regel „keine Wiederaufladung über H" ist dauerhaft 0. Ist zusätzlich die Netzladung gesperrt, greift der zweite Ladedeckel `max(0, -N)`, der ohne Überschuss ebenfalls 0 ist. Startet der Speicher auf seiner unteren SoC-Marke, hat er schon im ersten Intervall nichts abzugeben. Das Ergebnis ist rechnerisch richtig und dennoch wertlos: Variante und Referenz sind zahlengleich, der Kapitalwert ist die negative Investition zuzüglich Betriebskosten.

Verbindlich sind daraus drei Forderungen an die Umsetzung:

1. **Der Zielwert wird aus der Referenz hergeleitet, nicht geraten.** Die Untergrenze eines sinnvollen H ist das Maximum der Tagesminima von N — unter dieses Maximum fällt die Last an mindestens einem Tag nie, und damit ist an diesem Tag jede Wiederaufladung ausgeschlossen. Die Obergrenze eines wirksamen H ist die Referenzspitze. Ein Vorschlag lautet deshalb `H0 = max(Referenzspitze - Summe Entladeleistung; max der Tagesminima)`.
2. **Die Simulation weist die Sperren aus.** Je Einheit und für die Flotte werden gezählt: Intervalle mit `N > H`, Intervalle mit Lade- und mit Entladeanforderung, Intervalle mit Ladedeckel 0 durch die Peak-Regel, Intervalle mit Ladedeckel 0 durch das Netzladeverbot und Intervalle mit Entladeanforderung an einen leeren Speicher. Aus Lade- und Entladeenergie folgt die Aussage „arbeitslos". Die Gründe sind sprachneutral zu benennen und mit ihren Zahlen auszugeben.
3. **Der Start-Ladezustand wird genannt, nicht stillschweigend geändert.** Bleibt er auf der unteren SoC-Marke, ist am Anfang des Rechenzeitraums nichts zu entladen; eine Spitze in den ersten Stunden kann die Flotte deshalb nicht kappen. Ein voller Start würde diese Spitze schöner rechnen, als sie im Betrieb wäre.

### 5.1.1 Adaptive Entladeschwelle — die kausale Ratsche (ergänzt in Version 1.4, Anwenderbefund 11.09.2026; **umgesetzt #215**)

**Befund.** Die Regel aus 5.1 hält den Zielwert H das ganze Jahr fest. Kann die Flotte eine Spitze nicht
halten (`N − D > H` mit D = verfügbare Entladeleistung), ist die Jahresspitze verloren — die Regel entlädt
danach trotzdem bei jeder kleineren Spitze über H weiter, hält den Ladezustand damit niedrig und verfehlt
auch die nächste große Spitze. Beobachtet am 11.09.2026 (Einheit 1 395 kWh / 500 kW, H = 200 kW): In der
ersten Januarwoche werden die Tagesspitzen von 250–400 kW sauber auf 200 kW gekappt, die Spitze von
523 kW am siebten Tag trifft auf einen leeren Speicher und bleibt ungekappt. Der Anwender hat das Gegenmodell
in einer Excel-Datei belegt (Makro `calc_peakshaving`, Speicher 400 kW / 400 kWh, Viertelstundenlastgang
eines Jahres): Die Jahresspitze fällt von 738,4 kW auf **569,6 kW**, ohne dass ein Zielwert vorgegeben wird.

**Regel R (Ratsche, kausal).** Die Schwelle H ist kein Parameter, sondern ein Zustand, der im Lauf nur
steigen kann. Vor jedem Intervall wird geprüft, ob die Flotte die anstehende Netzlast bis auf H drücken
KANN; kann sie es nicht, wird H auf das Erreichbare nachgezogen. Erst danach gilt die Regel aus 5.1.

```text
H ← H0                                          Startwert (Vorgabe: Grundlast = Maximum der Tagesminima; 0 erlaubt)
je Intervall t:
    D_t ← Σ_j min( P_ent,j · v_j ,  (E_j − floor_j) · η_ent,j / Δt )     verfügbare Entladeleistung der Flotte
    wenn N_t − D_t > H:   H ← N_t − D_t          Spitze ist nicht haltbar → Schwelle nachziehen
    wenn N_t > H:         Entladung anfordern = N_t − H
    sonst:                Ladung anfordern = H − N_t,
                          begrenzt durch Ladefreigabe (Netzladung bzw. nur Überschuss), Speicherraum und Anschluss
Ergebnis: H_end = erreichte Jahresspitze der Variante; die Ganglinie von H ist eine Treppe
```

D_t ist genau die Entladegrenze der Ausführung (`Grenzen()` im Simulator: Leistung × Verfügbarkeit, nutzbare
Energie über der SoC-Untergrenze, Entladewirkungsgrad). Die Peak-Reserve ist bei `N_t > H` definitionsgemäß
freigegeben, sie geht also in D_t ein. Für eine Einheit ohne Verluste und ohne SoC-Fenster ist R wortgleich
mit dem Excel-Makro; das Makro ist damit die Referenzrechnung des Prüfstands (synthetischer Lastgang, der
Kundenlastgang bleibt außerhalb des Repositoriums).

**Eigenschaften.** R ist kausal (kennt keine Zukunft), deterministisch und monoton; sie braucht keinen
Zielwert und keine Bisektion. Sie verschwendet keine Energie an Spitzen, die die Jahresspitze nicht mehr
senken. Sie ist das Verhalten eines realen Reglers ohne Prognose.

**Vergleich mit dem Vorausschau-Optimum.** Die Bisektion aus Konzept 2.4 („Peak-Ziel bestimmen") sucht das
kleinste FESTE H, das ein Jahr mit vollem Wissen gehalten wird; nenne es M*. Es gilt **M* ≤ H_end**: Ein
festes M* ab dem ersten Intervall entlädt in jedem Intervall höchstens so viel wie die Ratsche (deren
Schwelle nie über M* liegt, solange sie nicht scheitert) und lädt mindestens so viel; ihr Ladezustand liegt
deshalb nie unter dem der Ratsche, und was M* hält, hält sie auch. Die Differenz H_end − M* ist der **Wert
der Vorausschau** — die Zahl, die sagt, ob sich ein Prognoseregler lohnt. Beide Werte gehören in die
Ergebnisansicht: „kausal erreicht" (R) und „mit Vorausschau erreichbar" (Bisektion, auf Knopf wie heute).

**Geprüfte Alternativen (11.09.2026).**

| Strategie | Bewertung |
|---|---|
| **S‑A Ratsche über das Jahr** (Regel R) | Empfohlen als Vorgabe für die Lastspitzenkappung. Behebt den Befund, ist ohne Prognose real umsetzbar, reproduziert das Excel-Makro. |
| S‑B Ratsche je Abrechnungsperiode (Rücksetzen am Monatsanfang) | Nicht nötig: Der Leistungspreis des Modells ist ein Jahrespreis in €/(kW·a) (`FlottenTarif.LeistungspreisEuroProKw`); ein Monatsleistungspreis existiert im Modell nicht. Vorgemerkt für den Fall, dass ein solcher Tarif kommt. |
| S‑C Vorausschau-Optimum (Bisektion, Bestand) | Bleibt als Vergleichs- und Auslegungswert. Als Startwert H0 der Ratsche taugt es nicht: Mit H0 = M* ist R mit dem festen Ziel identisch. |
| S‑D Ratsche mit Kurzfristprognose (Persistenz: gestern bzw. gleicher Wochentag der Vorwoche) | Zweite Stufe. Vor einer Entladung wird geprüft, ob die erwartete Restenergie des Tages über H den Ladezustand übersteigt; wenn ja, wird H vorausschauend angehoben oder die Entladung gedrosselt. Der Gewinn ist durch H_end − M* nach oben begrenzt — erst nach S‑A messen, dann entscheiden. Die Prognose-Snapshots des Simulators (Kapitel 6) sind die Naht. |
| S‑E Sicherheitsaufschlag auf H (Prozent oder kW) | In der Simulation überflüssig (deterministisch). Gehört in die Hilfe als Hinweis für den realen Regler, nicht in den Rechenweg. |
| S‑F Laden „rechtzeitig" statt „so schnell wie möglich" | Ohne Preisunterschiede wirkungslos für die Jahresspitze; mit Preisen ist es der Fall Multi Use (6.5). Keine Änderung an R. |

**Bedienung und Anzeige (Vorschlag).** In Schritt 3 der Auslegungsansicht wird das Peak-Ziel zur Wahl
„adaptiv (kausal) | fest": adaptiv ist die Vorgabe NEUER Stände, das Zahlenfeld heißt dann „Startwert" und
ist mit der Grundlast vorbelegt; fest verhält sich wie bisher. Die Ergebnisansicht nennt H_end als
„erreichte Schwelle", das Netzanschluss-Diagramm zeichnet H als Treppe statt als waagerechte Linie, die
Diagnose zählt die Nachzüge (Intervalle mit `N − D > H`). Die Wiederaufladung aus dem Netz bleibt eine
Freigabe des Anwenders; ohne sie lädt R nur aus Überschuss, und die Diagnose sagt das (SP‑O‑10). Gespeicherte
Stände tragen die Vorgabe „fest" und rechnen unverändert — das Prüfprojekt 1046 (festes Ziel 16 kW) bleibt
byte-gleich zur Basis R7 (Muster #183: Vorgaben nur für neue Stände).

**Fragen an den Anwender (PS‑Q1 … PS‑Q4) — entschieden am 11.09.2026 („PS‑Q1 bis Q4, Empfehlung"); Umsetzung Paket P7 (#215).**

**Stand der Umsetzung (#215).** Die Regel R steht im `FlottenSimulator`: `FlottenSimulationOptionen.PeakZielAdaptiv` schaltet sie ein (serialisierte Vorgabe `false`, Vorgabe NEUER Stände über `FlottenVorgaben.PeakZielAdaptivFuer`), `WirtschaftlicherPeakZielwertKw` ist dann der Startwert H₀. Das Ergebnis führt `FlottenSimulationErgebnis.ErreichtesPeakZielKw` (H_end), die Ganglinie `FlottenIntervallErgebnis.PeakZielKw` (Treppe) und der Diagnosezähler `FlottenDiagnose.IntervalleSchwelleNachgezogen`. `FlottenPeakZiel.PeakZielBestimmen` rechnet ausdrücklich ohne Ratsche und heißt in der Anzeige „mit Vorausschau erreichbar"; die Ergebnisansicht stellt beide Werte nebeneinander. Der Prüfstand `SpeicherEngine.Tests/FlottenPeakRatscheTests` hält den Simulator gegen den **Port des Excel-Makros** auf einem synthetischen Viertelstundenlastgang (7 Tage, Grundlast 60 kW, Spitze 740 kW, 400 kW / 400 kWh): Jahresspitze **740 → 540 kW**, M* = 340 kW, H_end − M* = 200 kW. Bei `PeakZielAdaptiv = false` ist der Rechenweg unverändert — Projekt 1046 bleibt byte-gleich zur Basis R7.

| Frage | Empfehlung |
|---|---|
| **PS‑Q1** Ratsche (S‑A) als Vorgabe für neue Stände, Startwert = Grundlast? | Ja. Wer ein festes Ziel will, schaltet um. |
| **PS‑Q2** „Peak-Ziel bestimmen" behalten und als „mit Vorausschau erreichbar" neben „kausal erreicht" zeigen? | Ja; die Differenz ist der Wert einer Prognose und entscheidet über S‑D. |
| **PS‑Q3** Netzladung bei Lastspitzenkappung für BESTEHENDE Stände still einschalten? | Nein. Bestehende Stände bleiben, wie gespeichert; das Diagnosebanner (und künftig der Stromspeicher-Reiter) benennt die Sperre. Neue Stände haben sie seit #183 an. |
| **PS‑Q4** S‑D (Kurzfristprognose) jetzt mit beauftragen? | Nein, erst nach S‑A mit gemessenem H_end − M* über die Prüfprojekte entscheiden. |

### 5.2 PV Eigenverbrauch als Greedy Referenz

Bei PV-Überschuss wird sofort geladen. Bei Nettobedarf wird sofort entladen, soweit keine Reserve oder Leistungsgrenze entgegensteht. Die Gesamtanforderung ist p_soll = N. Für einen reinen PV-Vergleich sind Netzladung und Batterieexport deaktiviert. Diese Strategie benötigt keine Zukunftsinformation und dient als Vergleich für das prognosebasierte Verfahren.

### 5.3 Preisarbitrage

Für Eigenversorgung besitzt eine abgegebene AC-kWh den Wert des vermiedenen Bezugspreises. Für eine tatsächliche Netzeinspeisung gilt der Verkaufspreis. Diese beiden Werte können erheblich voneinander abweichen. Der mitgelieferte Schwellenregler ist nur ein einfacher Vergleichsfall für vermiedenen Bezug; das eigentliche Arbitrageverfahren verwendet die Optimierung aus Kapitel 6.

Für einen vereinfachten vollständigen Zyklus ohne Anschlusskonflikte lautet die Grenzbedingung je abgegebener AC-kWh:

```text
Wert_spaeter > Preis_Ladeenergie / eta_RT + Grenzverschleiss
```

Bei PV-Ladung ist Preis_Ladeenergie der entgangene Einspeiseertrag oder, bei sonstiger Abregelung, der entgangene Wert der abgeregelten Energie. Zusätzliche Entgelte und Vermarktungskosten werden in die jeweiligen effektiven Preise aufgenommen. Die Bedingung ist eine Plausibilitätsprüfung; zeitliche Verfügbarkeit und Reservebedarf prüft das Optimierungsmodell.

### 5.4 Verteilung auf mehrere Speicher

| Modus im Code | Verfahren | Aussagegrenze |
|---|---|---|
| balanced | Anteile nach verfügbarer Energie beim Entladen und freiem Energieplatz beim Laden; begrenzte Restverteilung | Keine exakte Gleichsetzung aller SoC oder SoH |
| cascade | Speicher nacheinander auslasten; Reihenfolge je UTC-Tag rotieren | Kein Zustandsmodell für Schlafen oder Schaltkosten |
| merit | Beim Entladen nach Grenzverschleiß und Wirkungsgrad, beim Laden nach Wirkungsgrad sortieren | Greedy-Reihenfolge, keine garantierte globale Kostenoptimalität |
| Optimierer | Lade- und Entladeleistung jedes Speichers als eigene Variable | Optimal nur innerhalb des formulierten Horizontmodells |

Bei balanced wird ein begrenzter Speicher aus der aktiven Verteilmenge entfernt. Die Restleistung wird auf die übrigen verteilt, bis die Anforderung erfüllt ist oder keine Leistung mehr verfügbar ist. Beispiel: 40 kW Anforderung und zwei zunächst gleich gewichtete Speicher, von denen einer nur 5 kW liefern kann, ergeben 5 und 35 kW statt 5 und 20 kW.

Für echtes SoC-Balancing kann eine zusätzliche Zielfunktion die Streuung von E_i/C_i minimieren. Für gezielte asymmetrische Aufgaben werden Speicherrollen und zugelassene Leistungen je Rolle definiert. Reine Symmetrie oder Rollenrotation belegen noch keine gleichmäßige Alterung; diese wird mit separaten Beanspruchungsgrößen überprüft.

## 6 Prognoseplanung und kombinierte Optimierung

### 6.1 Rollierender Horizont

Zum Entscheidungszeitpunkt werden ein Prognosehorizont und der aktuelle Energiezustand geladen. Die Optimierung plant sämtliche Intervalle chronologisch. Nur die ersten konfigurierten Schritte werden mit den tatsächlichen Eingangsdaten ausgeführt. Danach wird aus dem fortgeschriebenen Istzustand neu geplant.

Der Beispielhorizont umfasst 192 Schritte, also 48 Stunden. Der Demonstrationslauf plant alle 96 Schritte neu. Für einen engeren Rückkopplungstakt wird replan_steps auf 1 oder einen anderen expliziten Wert gesetzt. Der Standard ist eine Rechenkonfiguration, keine Aussage über die notwendige Aktualisierung realer Anlagen.

### 6.2 Variablen und Nebenbedingungen des MILP

Das gemischt-ganzzahlige lineare Modell besitzt c_i,t, d_i,t und E_i,t für jeden Speicher sowie Import I_t, Export X_t und Abregelung K_t. Die Energiebilanzen aus Kapitel 4 gelten in jedem Intervall. Ein Binärwert z_t legt die gemeinsame Batterierichtung fest:

```text
0 <= c_i,t <= P_c_i * z_t
0 <= d_i,t <= P_d_i * (1-z_t)
z_t in {0,1}
```

Damit laden und entladen weder ein einzelner Speicher noch zwei verschiedene Speicher gegeneinander. Für ausdrücklich gewünschte interne Energieverschiebung wäre diese konservative Vorgabe zu ändern und der Grund zu dokumentieren.

Ein zweiter Binärwert g_t verhindert gleichzeitigen Netzbezug und Export:

```text
0 <= I_t <= M_import_t * g_t
0 <= X_t <= M_export_t * (1-g_t)
g_t in {0,1}
I_t - X_t = L_t - PV_t + K_t + A_t + sum(c_i,t-d_i,t)
```

Die Hilfsgrenzen M werden aus Last, PV, Speicherleistungen und Anschlussgrenzen bestimmt. Unnötig große Konstanten verschlechtern die Numerik. Beide Richtungsbedingungen sind insbesondere bei negativen Preisen und ungewöhnlichen Preisrelationen erforderlich.

Anfangszustand und Endzustand werden explizit vorgegeben. Der Basiskern setzt am Horizontende pro Speicher einen Zielenergiewert; im Beispiel ist das der konfigurierte Anfangswert. Das verhindert eine kostenlose Entleerung am Horizontende. Ein solcher zyklischer Abschluss kann aber die Nutzung einschränken. Für reale MPC-Untersuchungen sind alternative Endwertfunktionen oder zeitabhängige Endzielbänder als Sensitivität zu prüfen.

### 6.3 Wirtschaftliche Zielfunktion

```text
minimiere
  sum_t dt * (Preis_Bezug_t*I_t - Preis_Export_t*X_t)
  + sum_i,t dt * Grenzverschleiss_i*d_i,t
  + Leistungspreis * (B - B_bisher)

mit B >= I_t und B >= B_bisher
```

B_bisher ist der bereits erreichte Höchstbezug innerhalb derselben Abrechnungsperiode. Ein bereits bezahlter Peak kann nicht nachträglich eingespart werden. Für einen reinen Arbitragevergleich wird der Leistungspreis in der Fahrplanzielgröße auf null gesetzt; die anschließende tatsächliche Rechnung enthält trotzdem alle zutreffenden Entgelte.

Die termingebundene Jahreshöchstleistung ist nicht vollständig durch einen kurzen Horizont bekannt. Deshalb ist auch ein MILP mit Jahresleistungspreis bei rollierender Anwendung keine Garantie für das Jahresoptimum. Ein globaler Vergleichslauf mit perfekter Information und eine Variation des Peak-Zielwerts dienen als Gegenprüfung.

### 6.4 Prognosebasiertes PV Laden

Im Modus pv wird Netzladung und Batterieexport ausgeschlossen. Das Modell löst nacheinander drei Ziele: zuerst minimalen Netzenergiebezug, anschließend bei festgehaltenem Optimum minimale PV-Abregelung und zuletzt minimale Summe der gespeicherten Energie über die Zeit. Das letzte Ziel verzögert gleichwertige Ladungen und reduziert die Verweildauer hoher Energiezustände.

Die Ziele werden lexikografisch behandelt: Ein späteres Ziel darf das vorherige Optimum nur innerhalb der numerischen Toleranz verändern. Damit wird keine beliebige Gewichtung von Euro, kWh und SoC benötigt. Die Verringerung der gespeicherten Energie über die Zeit ist ein nachvollziehbarer Planungsindikator; sie ersetzt kein kalibriertes kalendarisches Alterungsmodell.

### 6.5 Multi Use mit Peak Priorität

Mit einem Peak-Ziel H wird zunächst die kleinste unvermeidbare Überschreitung U des Horizonts bestimmt. Die Bedingung lautet I_t <= H + U. Anschließend wird U auf sein Optimum begrenzt und unter diesen Bedingungen die wirtschaftliche Zielfunktion minimiert.

Im Ausführungsmodell hat die tatsächliche Peak-Anforderung Vorrang vor einem abweichenden Fahrplan. Die Zusatzreserve kann dafür freigegeben werden. Der Optimierer selbst hält diese Reserve konservativ zurück, damit sie für Prognosefehler verfügbar bleibt. Diese beiden Reserveverwendungen werden bewusst unterschieden.

Ein Fehler oder eine Zeitüberschreitung des Solvers darf nicht als optimierter Nullfahrplan erscheinen. Der Lauf verwendet eine protokollierte Rückfallstrategie: Peak Shaving bei Multi Use, sonst PV Greedy. Die Zahl solcher Intervalle und ihre Gründe gehören in den Ergebnisbericht. PuLP bildet die Modellierungsoberfläche; der enthaltene CBC-Solver bearbeitet das MILP. [PuLP Dokumentation](https://coin-or.github.io/pulp/technical/pulp.html)

## 7 Vollständiger Simulationsalgorithmus

### 7.1 Reihenfolge der Verarbeitung

```text
Eingaben und Konfiguration laden und validieren
Gleiche Referenzanlage ohne untersuchte Speicher simulieren
Fuer jede Kombination aus Hardware und Strategie:
    Anfangsenergien und Zustandszaehler initialisieren
    Fuer jedes Intervall in zeitlicher Reihenfolge:
        falls Planungstermin:
            damals verfuegbare Prognoseversion auswaehlen
            chronologisch gekoppelten Fahrplan berechnen
            Solverstatus und Prognosekennung speichern
        Sollleistung aus Regel oder Fahrplan ermitteln
        tatsaechlichen Peak und Reservefreigabe pruefen
        Leistung je Speicher physikalisch begrenzen
        Ladefreigaben und Anschlussgrenzen beruecksichtigen
        Energie nur mit ausgefuehrten Istleistungen aktualisieren
        PV-Abregelung und verbleibende Grenzverletzung berechnen
        Bilanzen pruefen und Ergebnisse speichern
    Tarifrechnung derselben Abrechnungsperiode bilden
    Anfangs- und Endenergie vergleichbar bewerten
    Alterung auswerten und Jahreskonto speichern
Kapitalwerte ueber die Projektjahre berechnen
Zulaessige Varianten und Referenz ohne Speicher vergleichen
```

Das betriebliche Entscheidungssystem bekommt keine späteren Istwerte zu sehen. Nur die Simulationsumgebung kennt den tatsächlichen Last- und PV-Verlauf. Planung und Ausführung können dadurch bei gleichen Prognosen unterschiedlich ausfallen, wenn Istwerte oder Ausfälle variieren.

### 7.2 Speicherung der Ergebnisse

Für jedes Intervall werden Netzbezug, Export, Abregelung, Hilfsverbrauch, Gesamtverluste, tatsächliche Leistung je Speicher, Energiezustände sowie Peak- und Anschlussverletzungen gespeichert. Zeitreihen und Zusammenfassung besitzen eine gemeinsame Konfigurations- und Datenkennung.

Die Auswertung enthält mindestens Energiebezug und Export, maximales Abrechnungsintervall, Peak-Verletzungen, Verluste, Energieumsatz je Speicher, Anfangs- und Endenergie, Solverrückfälle und die Kostenkomponenten der Rechnung. Für eine Lebensdauerbewertung kommen Kalenderalter, Rainflow-Zyklen, SoH und Ersatzereignisse hinzu.

Autarkie und PV-Eigenverbrauch müssen bei Netzladung mit Herkunftsbilanzen berechnet werden. Eine aus dem Netz geladene und später zur Last entladene kWh ist keine PV-Eigenversorgung. Der Basiskern gibt deshalb keine scheinpräzise Herkunftsquote aus. Eine Erweiterung führt getrennte Speicherenergiekonten für PV- und Netzherkunft und verteilt Verluste konsistent auf diese Konten.

## 8 Alterung und Reserven

### 8.1 Nutzungskennzahlen und Grenzverschleiß

Ein äquivalenter Vollzyklus kann als halber DC-Energiedurchsatz bezogen auf eine festgelegte Referenzkapazität definiert werden:

```text
EFC = sum_t dt*(eta_c*c + d/eta_d) / (2*C_Referenz)
```

Referenzkapazität, einseitige oder beidseitige Zählweise und das verwendete Kapazitätsfenster müssen angegeben werden. EFC beschreiben den Energieumsatz, nicht automatisch die Lebensdauer.

Ein einfacher Grenzverschleißpreis pro abgegebener AC-kWh lässt sich aus Ersatzkosten und dem erwarteten gesamten AC-Entladevolumen bis zum definierten Lebensdauerende ableiten. Wenn die Zykluszahl auf einem DC-Hub C*DoD basiert, geht der Entladewirkungsgrad in das abgegebene Volumen ein. Ein zusätzlicher pauschaler Round-Trip-Faktor wäre eine andere Bezugsdefinition und darf nicht ungeprüft eingesetzt werden.

### 8.2 Rainflow Auswertung

Rainflow zerlegt einen SoC-Verlauf in Voll- und Halbzyklen mit Zyklustiefe und mittlerem SoC. Der mitgelieferte Adapter verwendet die Bibliothek rainflow und gibt Zyklen sowie einen Miner-Schadenswert aus. Die Drei-Punkt-Regel in der Ausgangsvorlage war umgekehrt dargestellt: In der verwendeten Implementierung wird die ältere Schwingweite verarbeitet, wenn die neuere mindestens ebenso groß ist; Randzyklen werden gesondert behandelt. [rainflow Referenzimplementierung](https://github.com/iamlikeme/rainflow/blob/main/src/rainflow.py)

```text
D_zyklisch = sum_k Anzahl_k / Zyklen_bis_EOL(DoD_k, ...)
```

Die Lebensdauerkurve muss zur Zellchemie, Zyklustiefe, Temperatur, Ladegeschwindigkeit und zum gleichen End-of-Life-Kriterium passen. Ohne solche Daten darf keine scheinbar exakte Lebensdauer berechnet werden. Die Referenzfunktion interpoliert die logarithmische Zykluszahl nur innerhalb der gelieferten DoD-Stützstellen; außerhalb bricht sie ab.

Rainflow liefert keine kalendarische Alterung. Temperatur, Zeit und SoC-Verweilzeiten bleiben dafür relevant. Monats- oder Tagesgrenzen dürfen offene Restzyklen nicht unkontrolliert vervielfachen. Die Referenzauswertung verarbeitet deshalb den zusammenhängenden Verlauf; eine später ergänzte Streaming-Version muss ihren Reststapel über Blockgrenzen fortführen.

### 8.3 Fortschreibung über Projektjahre

Für die Anlagenstudie wird ein kalibriertes Modell zur SoH-Fortschreibung ergänzt. Als ausdrücklich vereinfachte Annahme wäre beispielsweise eine Abbildung des akkumulierten Schadens auf einen definierten SoH-Endpunkt möglich. Ein Schaden von 1 bedeutet dabei das gewählte End-of-Life-Kriterium, nicht notwendigerweise null Kapazität.

Die verfügbare Kapazität des nächsten Jahres wird aus dem neuen SoH bestimmt. Danach wird dieses Jahr erneut simuliert. Eine Leistungsalterung ist gegebenenfalls separat abzubilden. Ein Kapazitätsrückgang darf nicht als Entladung ins Netz erscheinen. Zustandsprojektion, nicht mehr nutzbare Energie und Ersatzvorgänge müssen als eigene Ereignisse bilanziert werden.

Ersatzentscheidungen benötigen eine Regel, etwa einen SoH-Grenzwert oder das Verfehlen einer zugesagten Mindestleistung. Kosten, Ausfallzeit, Anfangsenergie des Ersatzspeichers und deren Beschaffung werden im entsprechenden Jahr verbucht.

### 8.4 Reserve ist Energie und Leistung

Eine Notstromreserve ist gespeicherte Energie oberhalb der technischen Mindestgrenze. Freiraum für zukünftige Ladung ist dagegen ungenutzter Platz unterhalb der Obergrenze. Diese Größen werden nicht als dieselbe Art von SoC-Band behandelt.

Für eine erwartete Spitze mit Höhe P und Dauer tau ist mindestens E_reserve = P*tau/eta_d erforderlich, zuzüglich einer begründeten Unsicherheitsmarge. Zusätzlich muss die abrufbare Entladeleistung mindestens P erreichen. Eine große Energiereserve ohne ausreichende Leistung kann keine scharfe Lastspitze kappen.

Feste Reserven sind eine einfache Variante. Eine dynamische Reserve wird aus prognostizierten, zusammenhängenden Defizitphasen, möglichen Nachladefenstern und Prognosefehlern bestimmt. Ihre Einhaltung ist je Speicher und am gemeinsamen Anschluss zu prüfen. Regelenergie- und Notstromerlöse werden erst in die Wirtschaftlichkeit aufgenommen, wenn ein gesondertes Einsatzmodell und die zugrunde liegenden Vertragsbedingungen vorliegen.

## 9 Wirtschaftlichkeit nach der Kapitalwertmethode

### 9.1 Eine konsistente Rechnung je Variante

Die Bewertung verwendet die am Netzanschluss bezogenen und eingespeisten Energiemengen. Sie addiert keine separat aus Batterieentladungen geschätzten Erlöse. Für eine Abrechnungsperiode gilt:

```text
Arbeitskosten = sum_t dt*(Import_t*Bezugspreis_t
                         - Export_t*Einspeisepreis_t)
Leistungskosten = Leistungspreis * max_t(Import_t)
Rechnung = Arbeitskosten + Leistungskosten + Fixkosten
Bruttoeinsparung = Rechnung_Referenz - Rechnung_Speicher
```

Der Leistungspreis wird einmal je zugehöriger Periode angewendet. Bei monatlichem Tarif werden Monatsmaxima getrennt bewertet; ein Jahresleistungspreis wird nicht zwölfmal angesetzt. Tarifstufen, Benutzungsstunden, besondere Zeitfenster oder sonstige Vertragsbestandteile werden als eigene Tariflogik ergänzt. Der Zusammenhang von Jahresleistungspreis und Jahreshöchstleistung ist in § 17 StromNEV beschrieben. [§ 17 StromNEV](https://www.gesetze-im-internet.de/stromnev/__17.html)

Die Referenz enthält dieselbe PV-Anlage, denselben Verbrauch und dieselben Netzgrenzen. Wird zugleich PV neu gebaut oder ein bestehender Speicher erweitert, muss die Referenzinvestition entsprechend eindeutig festgelegt werden. Bereits angefallene Kosten einer Bestandsanlage sind für eine zusätzliche Investitionsentscheidung keine neue Auszahlung.

Einsparungen dürfen negativ sein. Eine Strategie, die durch ungünstiges Nachladen einen höheren Peak verursacht, muss entsprechend schlechter bewertet werden. Die Differenz wird nicht auf null gekappt.

### 9.2 Anfangsenergie und Endenergie

Für einen fairen Vergleich werden gleiche Anfangs- und Endenergien je Speicher verlangt oder die Bestandsänderung wird ausdrücklich bewertet. Ein zu Beginn kostenlos gefüllter und zum Ende leerer Speicher erzeugt sonst einen künstlichen Vorteil.

Bevorzugt wird für einen isolierten Jahresvergleich ein zyklischer Abschluss oder eine ausreichend lange Vorlaufphase mit kontrolliertem Endzustand. Bei fortlaufender Mehrjahressimulation wird der Zustand weitergereicht. Als näherungsweise Korrektur kann die Energieänderung mit einem offengelegten Wert v je interner kWh bewertet werden:

```text
Bereinigte_Einsparung = Rohe_Einsparung + v*(E_Ende-E_Anfang)
```

Diese Bewertung ist eine Näherung, weil Verfügbarkeit, zukünftige Preise und Verluste den wirklichen Wert beeinflussen. Sie wird als Sensitivität ausgewiesen. Der Referenzrunner liefert rohe Rechnungsdifferenzen und die Energieänderung separat; er behauptet keine automatische Bereinigung.

### 9.3 Jahreskonten und Kapitalwert

```text
CF_y = Rechnung_Referenz_y - Rechnung_Speicher_y
       - OPEX_y - Ersatzinvestitionen_y

NPV = -CAPEX_0 + sum_y CF_y/(1+r)^y + Restwert_T/(1+r)^T
```

CAPEX umfasst alle zusätzlich erforderlichen Speicher-, Wechselrichter-, Installations-, Anschluss- und Planungskosten. OPEX enthält zusätzliche tatsächliche Auszahlungen für Betrieb und Wartung. Energieverluste und AC-Hilfsverbrauch sind bereits in der Netzrechnung enthalten und werden nicht nochmals als pauschale Energiekosten addiert.

Der Grenzverschleiß im Optimierer ist ein Entscheidungspreis. Wenn reale Ersatzinvestitionen im Jahrescashflow erfasst sind, wird dieselbe Alterung nicht zusätzlich als fiktive jährliche Auszahlung abgezogen. Ebenso werden Investitionskosten nicht gleichzeitig als CAPEX und als vollständige jährliche LCOS-Auszahlung angesetzt.

Nominale Zahlungsreihen werden mit einem nominalen Zins, reale Zahlungsreihen mit einem realen Zins diskontiert. Die Referenzformel ist vor Steuern und ohne Finanzierungsstruktur. Sind Steuern, Finanzierung oder Förderungen relevant, werden sie in einem konsistenten zusätzlichen Cashflow-Modell berücksichtigt, ohne pauschale Befreiungen zu unterstellen.

### 9.4 Auswahl und Unsicherheit

Die Variante ohne Zusatzspeicher hat gegenüber sich selbst den inkrementellen Kapitalwert null und bleibt immer Teil des Vergleichs. Sind sämtliche zulässigen Speicherlösungen negativ, ist die Investition unter den untersuchten Annahmen wirtschaftlich nicht vorteilhaft. Eine technisch unzulässige Variante gewinnt auch mit hohem rechnerischem Kapitalwert nicht.

Eine einzelne Beispielperiode wird nicht auf ein Jahr hochgerechnet, um einen Jahrespeak oder langfristigen Kapitalwert vorzutäuschen. Für die Projektbewertung sind vollständige Jahreskonten erforderlich. Die langfristige Studie simuliert jedes Projektjahr mit seiner Kapazität und seinen Preis-, Last- und PV-Annahmen erneut oder bezeichnet eine Wiederholung des Referenzjahres ausdrücklich als vereinfachtes Szenario.

Mindestens zu variieren sind Investitionskosten, Ersatzzeitpunkt, Wirkungsgrad, Leistungsgrenzen, Zins, Preisniveau und Preisspreizung, Lastentwicklung sowie Prognosefehler. Der Abstand zwischen realistischer Prognoseplanung und Oracle-Lauf zeigt den Einfluss der Information. Ergebnisse mit unterschiedlichen Informationsständen werden nicht ungekennzeichnet in dieselbe Rangfolge gestellt.

LCOS können ergänzend als diskontierte Lebenszykluskosten je diskontierter abgegebener AC-kWh berechnet werden. Ob Ladeenergiekosten im Zähler enthalten sind, muss in der Kennzahl angegeben werden. Für die Entscheidung über einen zusätzlichen Fahrplan bleiben Grenzkosten und Opportunitätskosten maßgeblich.

## 10 Softwarebausteine und Verwendung

### 10.1 Dateien und Schnittstellen

| Datei | Verantwortung |
|---|---|
| ems.py | Zustandsmodell, physikalische Ausführung, Verteilstrategien, MILP, Abrechnung und NPV-Grundfunktion |
| downloads.py | Reale Downloads, Rohdaten-Snapshots, Zeitnormalisierung und Einheitenumrechnung |
| prepare_inputs.py | Strenger Zeitabgleich von Bruttolast, Preis und PV sowie effektive Tarife |
| run.py | Offline-Lauf, rollierende Planung, Prognoseprüfung, Rückfalllogik und Ergebnisausgabe |
| degradation.py | Rainflow-Auswertung mit externer Lebensdauerkurve |
| project_value.py | Kapitalwert aus expliziten Jahresrechnungen, OPEX und Ersatzkosten |
| test_ems.py | Fachliche Regressionstests |
| make_demo.py | Erzeugung der gekennzeichneten synthetischen Beispieldaten |

Die Simulation lässt sich unabhängig von einer Oberfläche verwenden. Die Trennung erlaubt die spätere Anbindung an eine Desktop-Anwendung, ein Webfrontend oder einen bestehenden Berechnungsdienst. Ein Dashboard verändert weder das Speicherzustandsmodell noch die Rechnungsmethode.

### 10.2 Installation und erster Lauf

Alle folgenden Aufrufe erfolgen im Ordner Speichersimulation. Python 3.12 wurde für die Prüfung verwendet. Für Windows ohne vorhandene Zeitzonendaten kann zusätzlich das Paket tzdata erforderlich sein; es ist in requirements.txt enthalten.

```text
python -m pip install -r code/requirements.txt
python -m unittest discover -s code -p "test_*.py" -v
python code/make_demo.py
python code/run.py --inputs beispieldaten/synthetisch_3_tage.csv
  --config config_beispiel.json --strategy multi_use
  --oracle --out ergebnisse/multi_use
```

Die umgebrochenen Aufrufe sind jeweils eine einzige Shell-Zeile. --oracle ist im Beispiel ausdrücklich gesetzt, weil die fiktiven zukünftigen Last- und PV-Werte als bekannt angenommen werden. Für einen realistischen Lauf wird diese Option entfernt und --forecasts mit einer nach decision_time und known_at gekennzeichneten Prognosedatei angegeben.

Die Beispielkonfiguration setzt den Leistungspreis für den dreitägigen Funktionstest auf null. Für einen Jahreslauf werden ein vollständiges Abrechnungsjahr und der tatsächlich gültige Jahresleistungspreis eingesetzt. Die Speicherkapazitäten, Preise und Ziele der Beispiele sind keine Empfehlung zur Beschaffung.

### 10.3 Historische Preise herunterladen

```text
python code/downloads.py prices
  --start 2025-01-01T00:00:00+01:00
  --end 2026-01-01T00:00:00+01:00
  --out data/preise_2025.csv --cache data/cache
```

Das Enddatum ist exklusiv. Der Adapter lädt einen zusätzlichen Randwert zur sicheren Erkennung des letzten Preisintervalls. Der Quellenvertrag ist auf DE-LU beschränkt. Region und Zeitbedeutung müssen bei einem neuen Adapter ausdrücklich angepasst werden. Bei großen Datenmengen können Downloads blockweise ausgeführt und mit denselben Vollständigkeitsregeln zusammengeführt werden.

### 10.4 Historische PV Referenz herunterladen

```text
python code/downloads.py pv-history
  --start 2025-01-01T00:00:00+01:00
  --end 2026-01-01T00:00:00+01:00
  --lat 52.52 --lon 13.405 --tilt 30 --azimuth 0
  --kwp 10 --inverter-kw 10 --pr 0.85
  --out data/pv_modell_2025.csv --cache data/cache
```

Die Koordinaten und PV-Werte sind ein Beispiel für Berlin. Sie sind vor einer Standortstudie zu ersetzen. Der Adapter verwendet die Azimutkonvention des Anbieters: 0 Grad Süd, negative Werte Ost, positive Werte West. Die erzeugte Viertelstundenreihe behält die stündliche Informationsauflösung; sie rekonstruiert keine echten kurzzeitigen PV-Spitzen.

### 10.5 Aktuelle Prognose und Zusammenführung

```text
python code/downloads.py pv-forecast
  --lat 52.52 --lon 13.405 --tilt 30 --azimuth 0 --kwp 10
  --out data/pv_prognose_stuetzstellen.csv
  --cache data/cache --refresh

python code/prepare_inputs.py
  --load data/standortlast_2025.csv
  --prices data/preise_2025.csv --pv data/pv_modell_2025.csv
  --config config_beispiel.json --out data/eingaben_2025.csv
```

Die Prognose-Stützstellendatei ist ein anderer Datenvertrag als die modellierte historische PV-Intervallreihe. prepare_inputs verwendet hier ausdrücklich die historische Intervallreihe. Eine fehlerhafte Vermischung wird nicht automatisch kaschiert.

Für Prognoseläufe wird zusätzlich die zukünftige Bruttolast prognostiziert, beispielsweise mit einem ausschließlich auf früheren Daten trainierten Modell und einem Kalender der Betriebszeiten. Wetter allein ist keine Lastprognose. Eine stichtagsgerechte Preis- und Lastprognose wird zusammen mit der PV-Prognose in den Vertrag aus Kapitel 3 überführt.

### 10.6 Projektbewertung aus Jahreskonten

```json
{
  "capex_eur": 1000,
  "discount_rate": 0.1,
  "residual_value_eur": 0,
  "years": [
    {"baseline_bill_eur": 2000, "storage_bill_eur": 1300,
     "opex_eur": 100, "replacement_eur": 0},
    {"baseline_bill_eur": 2000, "storage_bill_eur": 1300,
     "opex_eur": 100, "replacement_eur": 0}
  ]
}
```

Diese kleine fiktive Kontrollrechnung ergibt Cashflows von jeweils 600 EUR und einen Kapitalwert von rund 41,32 EUR. Sie dient der Prüfung der Diskontierung. Für die Anlage werden diese Werte durch die vollständigen, bestandsbereinigten Jahreskonten ersetzt. Der Aufruf lautet python code/project_value.py jahreskonten.json.

## 11 Prüfungen und Abnahmekriterien

### 11.1 Handrechenfälle

| Fall | Eingabe | Erwartetes Ergebnis |
|---|---|---|
| Rundlauf | 10 kWh AC laden; eta_RT = 0,90; anschließend vollständig entladen | 9 kWh AC zurück, 1 kWh Verlust |
| Ladegrenze | 1 kWh freier Platz; eta_c = 0,95; dt = 0,25 h | Höchstens 4,210526 kW Ladeleistung |
| PV beim Peak | 150 kW Last, 60 kW PV, Ziel 100 kW, voller Speicher | 90 kW Netzbezug; keine Entladung notwendig |
| Zu wenig Leistung | 40 kW Anforderung; erster Speicher maximal 5 kW | Restleistung auf andere Speicher verteilen |
| PV Opportunität | 1 kWh PV laden; 0,9 kWh später nutzbar; Bezug 0,30 EUR, Export 0,08 EUR | Vorteil 0,19 EUR; keine zusätzliche Arbitragegutschrift |
| Restpeak | Referenz 150 kW, Ziel 100 kW, tatsächlich maximal 110 kW; 120 EUR/kW | Leistungskostenvorteil 4.800 EUR, nicht null und nicht 6.000 EUR |
| Preisintervall | Ein Stundenpreis von 100 EUR/MWh | Vier Werte von 0,10 EUR/kWh |
| Kapitalwert | 1.000 EUR CAPEX, zweimal 600 EUR, 10 Prozent Zins | 41,32 EUR |

### 11.2 Automatische Prüfung

Der Testbestand prüft Energiebilanzen über viele Intervalle, Grenzzustände, Reservefreigabe, Mehrspeicherverteilung, zulässige Netzflüsse, Verlustkosten, Zeitumstellung, Datenlücken, negative Preise, Solver-Unlösbarkeit, Endzustände und den Ausschluss später veröffentlichter Prognosedaten. Die Optimierungstests verwenden handrechenbare kleine Probleme und prüfen die tatsächlichen Auswirkungen nach der physikalischen Ausführung.

Eine zusätzliche Integrationsprüfung verwendet die drei Tage synthetischer Daten. Ergebnisdateien kennzeichnen den perfekten Informationsstand ausdrücklich. Die drei Download-Adapter wurden außerdem mit öffentlichen Beispielabrufen geprüft; die dazu verwendeten Daten sind keine Standortmessungen.

Für die fachliche Abnahme gelten pro Intervall eine Energiebilanzabweichung unter 0,000001 kWh und keine ungeklärten SoC-Verletzungen. Solverwerte werden mit einer Leistungstoleranz von 0,00001 kW verglichen. Größere Abweichungen sind Fehler oder ausdrücklich dokumentierte Modellunterschiede. Für eine konkrete Software können skalierungsabhängige Toleranzen nötig sein; diese werden vor dem Variantenvergleich einheitlich festgelegt.

### 11.3 Sensitivität und unabhängige Prüfung

Die vollständige Studienabnahme ergänzt Vergleichsläufe ohne PV, ohne Speicher, mit einem und mehreren Speichern, bei gleichen Preisen, bei negativen Preisen sowie bei langen Defizitphasen. Hinzu kommen Ausfälle und Prognoseabweichungen, sobald deren Modelle implementiert sind. Bei kritischen Lastspitzen wird eine feinere Zeitauflösung mit tatsächlich verfügbaren höher aufgelösten Daten geprüft.

Ein Solverstatus optimal bedeutet nur, dass das formulierte Problem gelöst wurde. Er bestätigt weder die Eingangsdaten noch die Eignung der Topologie oder die Übertragbarkeit auf ein Jahresoptimum. Diese Punkte werden mit Bilanzprüfungen, Datenherkunft und Szenarien gesondert beurteilt.

## 12 Parametrierung und Auswahl der besten Variante

### 12.1 Projektbezogene Eingaben

| Eingabegruppe | Vor der Anlagenbewertung festzulegen |
|---|---|
| Standort und Daten | Messpunkt, Brutto- oder Netzlast, vollständiges Jahr, PV-Messung oder Modell, Datenqualität |
| Speicher | Anzahl, Alter, Kapazitätsdefinition, Lade- und Entladegrenzen, Wirkungsgrade, SoC-Fenster, Kosten |
| Anschluss | Bezugs- und Einspeisegrenzen, Freigaben für Netzladung und Batterieexport |
| Betrieb | Peak-Ziel, Reserven, Planungsrhythmus, Prognosequellen und Rückfallstrategie |
| Tarif | Bezug, Einspeisung, Leistungspreis und Periode, fixe Kosten und Vermarktungskosten |
| Wirtschaftlichkeit | Betrachtungszeitraum, Zins, OPEX, Ersatz, Restwert, Preis- und Lastentwicklung |

### 12.2 Algorithmus für den Variantenvergleich

Zunächst werden zulässige Hardwarekombinationen gebildet. Zu jeder Kombination werden Kapazität und Leistung separat variiert; doppelte Kapazität bedeutet nicht automatisch doppelte Leistung. Für jede Hardware werden die relevanten Betriebs- und Verteilstrategien sowie Peak-Ziele untersucht.

Die erste Suche kann ein grobes Raster verwenden. Um wirtschaftlich interessante Kandidaten herum wird das Raster anschließend verfeinert. Jeder Kandidat durchläuft dieselbe Jahres- und Lebenszyklusrechnung. Varianten mit unzulässigen Anschlussverletzungen werden vor der wirtschaftlichen Rangfolge markiert. Verfehlte wirtschaftliche Peak-Ziele werden dagegen mit ihren tatsächlich entstandenen Kosten bewertet.

Zur Ergebnisdarstellung gehören pro Variante Kapitalwert, Investition, Jahresrechnungen, Peak, Verluste, Ersatzbedarf, Prognosemodus und technische Zulässigkeit. Die beste Variante ist die zulässige Kombination mit dem höchsten Kapitalwert unter den offengelegten Annahmen. Bleibt die Entscheidung bei plausiblen Sensitivitäten stabil, ist sie belastbarer als ein einzelner Spitzenwert unter einer günstigen Preisannahme.

### 12.3 Umsetzungsreihenfolge

Zuerst werden Standortdaten und Tarif auf den Datenvertrag abgebildet und die Referenzrechnung gegen eine vorhandene Stromrechnung geprüft. Danach werden Speicherparameter eingesetzt und die reaktiven Strategien validiert. Anschließend folgen Prognoseläufe, ein Vergleich mit perfekter Information und die Kombination mehrerer Nutzungen. Zuletzt werden Alterung, Ersatzinvestitionen und sämtliche Projektjahre in die Kapitalwertbewertung integriert.

Der vorhandene Code bietet einen nachvollziehbaren Startpunkt für diesen Ablauf. Die Spezifikation legt zugleich fest, welche Erweiterungen notwendig sind, bevor aus einem Rechenbeispiel eine belastbare Entscheidung für eine konkrete Anlage wird.

## 13 Vorschlag für die Umsetzung in EPOS-Plan

### 13.1 Ausgangspunkt und Architekturentscheidung

Die Erweiterung sollte in die vorhandene C#-Architektur von EPOS-Plan integriert werden. Der Python-Code dieses Pakets dient als ausführbare fachliche Referenz und für Vergleichstests. Für den regulären Betrieb der Anwendung wird daraus keine zusätzliche Python-Laufzeit vorausgesetzt.

Der öffentlich beschriebene Funktionsumfang umfasst bereits Stromspeicher, PV, Wärmeerzeuger und Variantenvergleiche. Die Produktseite nennt Viertelstunden für die Stromseite und Stunden für die Wärmeseite. Diese Kopplung wird im Ausbau bewusst erhalten. [EPOS-Plan Produktbeschreibung](https://epos-plan.de/)

Für den Umsetzungsvorschlag wurden am 10. September 2026 ausgewählte Dateien im Repository gelesen. Das ist eine Prüfung konkreter Integrationsstellen, keine vollständige Revision oder Ausführung der bestehenden Anwendung. Die nachfolgend benannten neuen Typen und Funktionen sind Vorschläge.

| Vorhandener Baustein | Beobachteter Stand | Vorgeschlagene Weiterentwicklung |
|---|---|---|
| SpeicherEngine | UI- und DB-freie Bibliothek für .NET 10 | Physikalisches Mehrspeichermodell und neue Ergebnisverträge hier ergänzen |
| ISpeicherStrategie | Jahreslauf mit SpeicherEingang und einem SpeicherParameter | Bestehende Schnittstelle erhalten; Flottenvertrag daneben einführen |
| SpeicherParameter | Ein PKw für beide Richtungen; eta aus sqrt(eta_RT) | Eigene Lade- und Entladegrenzen sowie Richtungswirkungsgrade je Einheit |
| SpeicherEingang | Last, PV, Preise in ct/kWh; optionale BHKW- und Vergütungsreihen | Zeitachsenkennung, Prognoseversion und getrennte Preispfade ergänzen |
| ArbitragePlaner | Greedy-Planung mit 24-Stunden-Fenstern und SoC-Pfadprüfung | Als Vergleich behalten; zusätzlichen Optimierer hinter einer Schnittstelle anbinden |
| SpeicherOptimierer | Grob- und Feinraster nach äquivalentem Jahresüberschuss | Explizite Kapitalwertbewertung mehrjähriger Cashflows ergänzen |
| EPOS.Kern | Controller beschaffen Projekt-, Varianten- und Preisdaten | Flottenaufbau, Daten-Snapshots und Simulationsaufträge koordinieren |
| EPOS.UI | Plattformfreie Razor-Komponenten für Speicher und Vergleich | Bestehende Seiten um Flotte, Strategieparameter und Quellenstatus erweitern |

Die projektspezifische Trennung bleibt bestehen: SpeicherEngine rechnet, EPOS.Kern koordiniert und greift auf Daten zu, EPOS.UI zeigt Daten an und nimmt Einstellungen entgegen. Netzwerkzugriffe gehören nicht in eine Strategieklasse oder in die innere Simulationsschleife.

### 13.2 Vier fachlich kritische Übergänge

Erstens sind Varianten Alternativen und keine physischen Einheiten. Der eingesehene StromspeicherSimCtrl beschreibt die aktive Variante ausdrücklich als Bezug auf eine Anlagenzeile; Mehrspeicherbetrieb ist dort ein späterer Ausbau. Eine Flotte wird deshalb innerhalb einer Variante als Liste tatsächlich gleichzeitig betriebener Anlagen modelliert. Die Anlagen verschiedener Vergleichsvarianten werden nicht zusammengerechnet.

Zweitens unterscheiden sich Preis- und Zeitkonventionen. Die vorhandenen Reihen verwenden ct/kWh. Die Referenzspezifikation verwendet EUR/kWh. Ein einziger Adapter übernimmt die Umrechnung Preis_EUR = Preis_ct / 100. Die vorhandene SpotreihenAufbereitung beschreibt eine Wanduhrachse des Modelljahres. Neue UTC-Daten dürfen deshalb nicht ohne Zuordnung in diese Arrays kopiert werden. Ein versionierter Zeitachsenadapter unterscheidet bestehende Modelljahre und tatsächliche Messjahre mit Offset und Zeitumstellung.

Drittens ist der bisherige Peak-Shaving-Vertrag zu beachten. Die eingesehene Klasse PeakShaving beschreibt eine lastbasierte Auswertung ohne PV-Einfluss. Für die neue gekoppelte Anlagenrechnung wird zusätzlich eine eindeutig bezeichnete Strategie am Netzanschlusspunkt eingeführt. Der bestehende Regressionsmodus bleibt nachvollziehbar; Lastgangkappung und Netzbezugskappung werden in der Oberfläche nicht unter identischem Namen vermischt.

Viertens muss die Gesamtsimulation alle Netzflüsse aus dem Speicherergebnis übernehmen. Der eingesehene Einbindungspfad in SimulationControl.Stromspeicher liefert Entladeleistung, und der aufrufende Block vermindert damit den Reststrombedarf. Das genügt für einen auf Eigenversorgung begrenzten Entladepfad, ist aber als Vertrag für Netzladung und Netzentladung zu erweitern. Der neue Pfad übergibt Netzbezug, Einspeisung, Ladung nach Quelle, Entladung nach Ziel und Abregelung explizit. Eine vorhandene spätere Sonderverrechnung muss bei der Umsetzung mitgeprüft werden, damit derselbe Netzfluss nur einmal wirkt.

### 13.3 Vorgeschlagenes Datenmodell

Eine neue Speicherflotten-Konfiguration besitzt eine Varianten-ID und eine Liste von Einheiten. Jede Einheit verweist auf eine reale Anlagen-ID und enthält technische Parameter, Anfangszustand, Reserven und gegebenenfalls Alterungsdaten. Betriebsstrategie und Verteilstrategie sind Eigenschaften der Flottenkonfiguration. Eine bereits vorhandene Einzelanlage wird beim Laden als Flotte mit genau einer Einheit abgebildet.

Die Projektdaten speichern keine frei erfundene zweite Stammdatenwelt. Herstellerdaten bleiben im bestehenden Anlagenkatalog. Variantenbezogene Betriebsparameter liegen bei der Variante. Downloads und Prognosen werden über Dataset-IDs mit Quellen-, Einheiten-, Kalender- und Versionsangaben referenziert. Eine Datenbankmigration erhält ihre Nummer erst nach Prüfung des dann aktuellen Schemas.

Der vorgeschlagene Ergebnisvertrag enthält je Speicher Leistung, SoC, Energieverluste und Beanspruchung sowie am Standort vollständige Import- und Exportreihen. Er enthält außerdem die Referenzrechnung, Simulationsrechnung, explizite Jahrescashflows, Kapitalwert, Status und Quellenkennungen. Anzeigen und Exporte verwenden denselben Ergebnisdatensatz.

```csharp
// Vorschlag fuer neue Verträge, keine vorhandenen EPOS-Typen
public interface ISpeicherFlottenStrategie
{
    FlottenErgebnis Berechne(
        FlottenEingang eingang,
        FlottenKonfiguration konfiguration,
        CancellationToken abbruch);
}

public interface IStromFahrplanOptimierer
{
    FahrplanErgebnis Plane(
        PrognoseHorizont prognose,
        FlottenZustand zustand,
        PlanungsParameter parameter,
        CancellationToken abbruch);
}
```

Die eigentliche Implementierung ergänzt vollständig definierte, unveränderliche DTOs. Keine dieser Schnittstellen darf Datenbankobjekte oder UI-Steuerelemente entgegennehmen. Die Namen sind eine Orientierung für die Umsetzung; bestehende Benennungskonventionen des Projekts haben Vorrang.

### 13.4 Solver und Datenbeschaffung

Da SpeicherEngine derzeit ohne Paket- und Projektreferenzen angelegt ist, wird die Solveranbindung in eine gesonderte Implementierung außerhalb dieser Bibliothek gesetzt. Die fachliche Schnittstelle bleibt im Kern. Ein zunächst unter Windows verwendeter MILP-Adapter kann mit den gleichen Eingaben gegen den enthaltenen PuLP/CBC-Referenzlauf geprüft werden. Die konkrete .NET-Bindung wird nach Lizenz, unterstützten Plattformen, Abbruchverhalten und reproduzierbaren Ergebnissen ausgewählt.

Für Plattformen ohne eingebetteten Solver stehen die reaktiven Strategien und der bestehende Greedy-Planer weiter zur Verfügung. Die Oberfläche zeigt die verfügbare Planungsart. Ein fehlender Solver führt nicht unbemerkt zu einer andersartigen, als optimal bezeichneten Berechnung.

Neue Download-Dienste werden in die vorhandene Import- und Controllerstruktur eingegliedert. Ein EnergyChartsPreisDienst lädt und versioniert die Rohdaten. Ein PvPrognoseDienst hält aktuelle Prognoseausgaben fest. Ein ZeitreihenNormalisierer setzt den Quellenvertrag um. Der vorhandene StromPreisCtrl bleibt die zentrale Stelle zur Zusammenstellung der projektbezogenen Preispfade.

Die eingesehene Preisbildung kennt separate Netzladeaufschläge. Für eine neue Studie sollte ein Aufschlag von null nur bei bewusst ausgewähltem und dokumentiertem Tarifzustand verwendet werden. Ein fehlender Parameter darf keine unbelegte Gebührenbefreiung bedeuten. Bestehende PV- und BHKW-Vergütungsreihen bleiben getrennt erhalten; ein pauschaler gemeinsamer Verkaufspreis wäre für diese Anlagenkombination häufig unzureichend.

### 13.5 Kopplung mit Wärme und Erzeugern

Die elektrische Standortlast wird aus dem Gebäudelastgang und den tatsächlich zusätzlich anfallenden Stromverbräuchen von Wärmepumpen, Heizstäben, Pumpen und weiteren Geräten aufgebaut. BHKW-Erzeugung und PV werden als getrennte Quellen geführt. Bereits in einer Lastreihe enthaltene Verbraucher werden nicht nochmals hinzugefügt.

Eine stündliche mittlere Leistung kann in vier gleiche Viertelstundenleistungen überführt werden; ihre Energie bleibt dadurch erhalten. Eine stündliche Energiemenge wird zunächst durch eine Stunde in Leistung umgerechnet. Diese Aufteilung erzeugt keine Kenntnis realer Viertelstundenspitzen. Für belastbares gewerbliches Peak Shaving ist deshalb ein geeigneter gemessener Viertelstundenlastgang maßgeblich.

In der ersten Ausbaustufe wird der thermische Anlagenfahrplan als Eingang der elektrischen Optimierung verwendet. Wenn später Wärmepumpen oder Wärmespeicher selbst preisabhängig verschoben werden sollen, ist eine gemeinsame Optimierung von Wärme- und Stromseite notwendig. Ein bloßes nacheinander Ausführen zweier Optimierer kann sonst widersprüchliche Fahrpläne erzeugen. Diese gemeinsame Optimierung ist eine weitere Ausbaustufe mit eigenen thermischen Nebenbedingungen.

### 13.6 Bedienablauf in EPOS-Plan

Die vorhandene Speicherparameterseite erhält eine Liste der innerhalb einer Variante gleichzeitig betriebenen Speicher. Darunter stehen Betriebsziel und Leistungsaufteilung als zwei getrennte Auswahlfelder. Nur die für das gewählte Ziel benötigten Parameter werden sichtbar, beispielsweise Peak-Grenze, Netzladefreigabe, Reserve und Planungshorizont.

Ein Quellenbereich zeigt Datenzeitraum, Mess- oder Prognoseart, Abrufzeit und fehlende Intervalle. Er bietet die Aktionen Daten importieren, Preise herunterladen und PV-Daten zuordnen. Die Berechnung startet erst mit einem gültigen, gespeicherten Datensatz. Fortschritt und Abbruch werden über die bestehenden Controller an die Oberfläche weitergegeben.

Im Variantenvergleich stehen zuerst Kapitalwert, technische Zulässigkeit, Investition und Jahreskosten. Ein umschaltbares Diagramm zeigt Netzlast vor und nach Speicherung, Peak-Grenze, Speicherleistungen und SoC je Einheit. Ein weiterer Vergleich zeigt tatsächliche Stromkosten nach Arbeit und Leistung. Prognosemodus und Rückfälle bleiben im Ergebnis sichtbar.

Die Anwendung verwendet im Bedienablauf Begriffe wie Prognoseplanung und Preisoptimierung. Solvername, Big-M-Konstanten und numerische Toleranzen gehören in technische Details und das Rechenprotokoll. Der bestehende Berichtsexport übernimmt die Ergebnis- und Quellenkennungen, damit die schriftliche Auswertung dieselbe Berechnung erklärt wie die Oberfläche.

### 13.7 Übernahme der Kapitalwertrechnung

Die bestehende Speicheroptimierung bewertet nach äquivalentem Jahresüberschuss. Bei gleicher Laufzeit und gleichem Zins ist dieser über den positiven Annuitätsfaktor mit dem Kapitalwert verknüpft; unter diesen Bedingungen kann dieselbe Rangfolge entstehen. Bei unterschiedlichen Ersatzzeitpunkten, Jahresverläufen oder Betrachtungsdauern wird stattdessen die explizite Zahlungsreihe maßgeblich.

Für den Ausbau wird daher ein gemeinsamer Projektbewertungsdienst verwendet, der Referenz- und Variantenrechnungen über alle Projektjahre diskontiert. Die bereits vorhandene allgemeine Wirtschaftlichkeitsrechnung und das konsolidierte EPOS-Konzept werden dabei abgeglichen. Eine lokale Speicherersparnis darf nicht zusätzlich zur bereits verminderten Stromrechnung als weiterer Erlös in den Gesamtprojekt-Kapitalwert eingehen.

### 13.8 Umsetzungsstufen und Abnahme

| Stufe | Konkretes Arbeitsergebnis | Abnahmekriterium |
|---|---|---|
| 1 Schnittstellen und Messjahre | Zeitachsenadapter, Einheitenadapter, vollständiger Netzflussvertrag | Dieselbe Viertelstunde und derselbe Preis erreichen alle Komponenten |
| 2 Mehrspeicherphysik | Flotte mit eigenen Zuständen; bestehende Einzelanlage als Sonderfall | Handrechenfälle und Bestandsregression bestehen |
| 3 Reaktive Strategien | Netzpunkt-Peak-Shaving, PV Greedy, Reserven, Leistungsaufteilung | Grenzen und Restverletzungen stimmen mit Referenzdaten überein |
| 4 Prognoseoptimierung | Prognose-Snapshots, Solveradapter, Multi Use, protokollierter Rückfall | Kleine C#-Läufe stimmen mit Python-Fällen überein; keine Zukunftsdaten |
| 5 Projektwirtschaftlichkeit | Jahreskonten, Alterung, Ersatz, Kapitalwert und Variantenrangfolge | Keine doppelte Einsparung; Nullvariante bleibt vergleichbar |
| 6 Oberfläche und Bericht | Bestehende Parameter-, Vergleichs- und Ergebnisseiten erweitert | Alle Ansichten verwenden denselben freigegebenen Ergebnisstand |

Die vorhandenen Testprojekte SpeicherEngine.Tests, EPOS.Kern.Tests und EPOS.UI.Tests werden entsprechend erweitert. Zusätzlich werden sprachübergreifende Referenzfälle als JSON oder CSV gespeichert. Verglichen werden Energien, Netzflüsse und Zielfunktionswerte innerhalb der festgelegten Toleranz; bei mehreren gleichwertigen Optima muss nicht jede einzelne Speicherleistung identisch sein.

### 13.9 Eingesehene Integrationsstellen

Die folgenden Dateien bilden die überprüfbare Grundlage des Umsetzungsvorschlags. Alle Pfade sind repositoriumsrelativ.

| Bereich | Eingesehene Datei |
|---|---|
| Kernarchitektur | SpeicherEngine/SpeicherEngine.csproj; EPOS.Kern/EPOS.Kern.csproj; EPOS.UI/EPOS.UI.csproj |
| Daten und Einheiten | SpeicherEngine/SpeicherEingang.cs; SpeicherEngine/SpeicherParameter.cs; SpeicherEngine/SpotreihenAufbereitung.cs |
| Strategien | SpeicherEngine/ISpeicherStrategie.cs; SpeicherEngine/PeakShaving.cs; SpeicherEngine/ArbitragePlaner.cs |
| Optimierung und NPV | SpeicherEngine/SpeicherOptimierer.cs; SpeicherEngine/Wirtschaftlichkeit.cs |
| Einbindung | EPOS.Kern/Controller/StromspeicherSimCtrl.cs; EPOS.Kern/Allgemein/Simulation/SimulationControl.Stromspeicher.cs; SimulationControl.cs im selben Ordner |
| EPOS-Konzept | Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md |

Dieser Integrationsvorschlag entstand vor der Änderung der Bestandsanwendung. Den anschließend umgesetzten Stand vom 11. September 2026 beschreibt Kapitel 14; seine Angaben ersetzen für diesen Funktionsumfang die damalige Bestandsaufnahme.

## 14 Kostenprofile Suchbereiche und Zeitreihen im EPOS Dialog

Entwicklungsstand: 11.09.2026. Die Erweiterung betrifft die Auslegungsoptimierung der aktiven Speicheranlage in EPOS-Plan. Die eigenständige Python-Simulation und ihre HTML-Auswertung sind separate Werkzeuge.

### 14.1 Bedienung

Unter **Simulation → Detaillierte Simulation → Parameter → Stromspeicher → Auslegung optimieren** werden Suchraum, Kosten und Datenquellen festgelegt. Die technischen Eigenschaften des aktiven Speichers, etwa Wirkungsgrad und SoC-Grenzen, bilden die Grundlage der Suche.

1. **Größenbasis wählen:** Kapazität in kWh oder Leistung in kW; Untergrenze, Obergrenze und optional Schrittweite eingeben. Ohne Größenschritt verwendet die Suche die Anzahl der Stützstellen. Gleiche Grenzen prüfen eine feste Größe.
2. **C-Rate wählen:** Untergrenze, Obergrenze und Schrittweite in 1/h. Bei Kapazitätsvorgabe gilt `P = E · C-Rate`, bei Leistungsvorgabe `E = P / C-Rate`. Gleiche Grenzen prüfen eine feste C-Rate.
3. **Investition und Betrieb getrennt einstellen:** jeweils „Kostenmodul“ oder „Im Dialog“. Die Kostenmodul-Sätze gehören zur aktiven Speicheranlage; ausgeschlossene Positionen erscheinen als Hinweis.
4. **Last, PV und Bezugspreis unabhängig wählen:** EPOS-Projekt oder jeweils eine eigene CSV-Datei; zusätzlich „Keine PV“ beziehungsweise ein editierbares Strompreisprofil.
5. **Profil speichern:** einen Namen vergeben und speichern. Ein geladenes Profil kann bearbeitet oder unter einem neuen Namen dupliziert werden. Bereiche, Kostensätze, Preisprofil, Quellen, CSV-Zuordnung und eingelesene Werte werden gemeinsam gespeichert.
6. **Neu berechnen:** Heatmap und Kennzahlen zeigen die untersuchten Größen und den wirtschaftlich besten Punkt. Nach Eingabeänderungen ist das bisherige Ergebnis als veraltet gekennzeichnet; die Übernahme des alten Bestpunkts bleibt bis zur Neuberechnung gesperrt.

Die Auslegung verändert die Speicheranlage erst bei der bestehenden Aktion zur Übernahme des Bestpunkts. Ein exportiertes Ergebnis gehört zum Eingabestand seines Rechenlaufs.

### 14.2 Kosten und Einheiten

Alle Kostensätze sind netto. Investition und Betrieb dürfen aus unterschiedlichen Quellen stammen. Explizite Nullwerte sind möglich; fehlende oder ungültige Sätze einer ausgewählten Quelle werden nicht stillschweigend zu Nullkosten.

| Feld | Einheit | Ansatz je Größenkandidat |
|---|---|---|
| Investition Leistung | €/kW | Satz × AC-Nennleistung |
| Investition Kapazität | €/kWh | Satz × Nennkapazität |
| Betrieb Leistung | €/(kW·a) | Satz × AC-Nennleistung pro Jahr |
| Betrieb Kapazität | €/(kWh·a) | Satz × Nennkapazität pro Jahr |
| Betrieb Entladung | €/kWh entladen | Satz × simulierte AC-Entladeenergie pro Jahr |

Im Kostenmodul werden die Bemessungen `EUR_PRO_KW_LEISTUNG`, `EUR_PRO_KW_ELEKTRISCH` und `EUR_PRO_KWH_KAPAZITAET` übernommen. Bei Betriebskosten ist zusätzlich `EUR_PRO_KWH_ELEKTRISCH` die entladene Strommenge. Pauschalbeträge, Prozentsätze, Erlöse, Zuschüsse, negative oder fehlende Sätze sowie erst in späteren Jahren beginnende Positionen werden mit Hinweis ausgeschlossen. Die Auslegung bildet diese Kosten ausschließlich über die genannten spezifischen Sätze ab; feste Investitionsbeträge werden in diesem Modus nicht zusätzlich angesetzt.

`I = c_P · P + c_E · E`

`OPEX_a = o_P · P + o_E · E + o_D · Q_Entladung,a`

Der Optimierer berücksichtigt Betriebskosten bei Jahresüberschuss, Kapitalwert und Amortisation. Der optionale Verschleißterm wird getrennt in der Zielfunktion abgezogen. Er darf nicht zugleich als derselbe Kostenanteil unter den entladeabhängigen Betriebskosten eingegeben werden. Die Suche ist eine vereinfachte Bewertung mit den bestehenden Zins-, Lebensdauer- und Degradationsparametern; die vollständige projektweite Wirtschaftlichkeitsrechnung bleibt ein eigener Auswertungsschritt.

Der Betriebskostenbetrag des simulierten Referenzjahres wird in dieser Bewertung als konstanter Jahresbetrag abgezinst. Der vorhandene Degradationsansatz wirkt auf die Ertragsseite. Eine jährlich neu simulierte Alterung der Fahrweise gehört zum weiterführenden Modell.

### 14.3 Datenquellen und CSV-Import

| Rolle | Interne Einheit im Datenvertrag | Bedeutung |
|---|---|---|
| `load_kw` | kW | Bruttolast vor Abzug von PV und BHKW |
| `pv_kw` | kW AC | Verfügbare PV-Leistung nach Wechselrichterverlusten und vor zusätzlicher Abregelung |
| `buy_eur_kwh` | €/kWh | Effektiver variabler Bezugspreis, auch negativ |

Die EPOS-Last verwendet den Strombedarf einschließlich der im Lauf ermittelten elektrischen Zusatzverbraucher. Die PV-Quelle ist die theoretisch verfügbare AC-Produktion des PV-Laufs. Bereits um PV bereinigte Nettolast darf nicht nochmals zusammen mit derselben PV-Reihe als Bruttolast eingelesen werden.

Der CSV-Dialog bietet Vorschau, Trennzeichen, Dezimaltrennzeichen, UTF-8 oder Windows-1252, Kopfzeile und übersprungene Zeilen. Zeitstempel und Werte werden über Spaltenauswahl zugeordnet. Datum und Uhrzeit können auch getrennt vorliegen. Zeitformat, Zeitzone und Intervallanfang oder -ende sind ausdrücklich festzulegen.

Last und PV unterstützen kW, MW oder kWh je Intervall. Preise unterstützen €/kWh, ct/kWh und €/MWh. Der Import normalisiert zusammenhängende 15- oder 60-Minuten-Reihen auf Viertelstunden: Leistungswerte bleiben Intervallmittelwerte, Intervallenergie wird durch die Dauer geteilt, Stundenpreise gelten für vier Viertelstunden. Lücken, doppelte UTC-Zeitstempel, unklare Sommerzeitstunden und nicht endliche Werte werden abgewiesen. Negative Last und PV sind unzulässig; negative Preise sind gültig.

Für die Jahreswirtschaftlichkeit ist ein vollständiges Kalenderjahr erforderlich. Alle ausgewählten Dateien müssen dieselbe UTC-Achse besitzen. Eine Datei mit nur einigen Tagen lässt sich zum Prüfen der Spalten importieren, ersetzt aber keinen vollständigen Jahresdatensatz in der Auslegung.

Sobald Dateien verwendet werden, bleibt deren tatsächliche Zeitachse erhalten, einschließlich Schaltjahr und Zeitumstellung. Bei Mischung mit EPOS-Modellreihen muss die Kalenderzuordnung ausdrücklich eingeschaltet werden: EPOS-Werte werden nach Monat, Tag und Uhrzeit zugeordnet. Für den 29. Februar wird der EPOS-28. Februar verwendet; bei einer wiederholten Ortsstunde wird der entsprechende EPOS-Modellwert erneut verwendet. Die CSV-Werte werden dabei nicht gemittelt oder gelöscht. Diese Annahme erscheint im Ergebnis als Hinweis.

Eine importierte Bezugspreisreihe enthält bereits den effektiven Preis. EPOS addiert darauf keine weiteren Tarifaufschläge. Ein Strompreisprofil verwendet dagegen das bestehende EPOS-Format mit zwölf Monatswerten und optional 168 Wochenstundenwerten in ct/kWh; darauf wird der Projektaufschlag einmal angewendet.

Die PV-Einspeisevergütung stammt auch bei PV aus CSV aus dem Projekt und wird bei der Bewertung entgangener Einspeisung berücksichtigt. Eine reine Dateiauslegung verwendet die eingelesene Last und PV; zusätzliche BHKW-Erzeugung aus einem Simulationslauf wird in diesem Modus nicht hinzugemischt.

### 14.4 Speicherung und technische Anschlussstellen

Migrationsschritt **73** legt `Tab_SpeicherAuslegung` mit Projekt- und Anlagenbezug sowie einem eindeutigen Index je Profilnamen an. Migrationsschritt **74** (Auftrag #178, 11.09.2026) baut dieselbe Tabelle als **STRICT**-Tabelle neu auf — sie war die einzige Fachtabelle des Zielschemas ohne `STRICT`; Spalten, Typen, Fremdschlüssel und Index bleiben wortgleich, die Zeilen werden samt ihrer `ID` übernommen. `@Aktuell` bezeichnet den letzten Eingabestand; dieser reservierte Name wird nicht als Benutzerprofil angeboten. Ein benanntes Profil ist eine eigenständige Kopie. Die Payload ist versioniertes, komprimiertes JSON (`gz1:`), einschließlich eingelesener Werte, Zeitstempel, Dateiname, SHA-256 und Importoptionen. Die Ursprungsdatei muss beim erneuten Öffnen nicht vorhanden sein.

Die Daten bleiben beim Kopieren des Projekts und beim erneuten Speichern derselben Speicheranlage im Assistenten erhalten. Das Löschen des Projekts entfernt seine Profile über Fremdschlüssel. Die Dateien `-wal` und `-shm` werden nicht einzeln behandelt; Sicherung und Schemapflege folgen [BETRIEB_SQLITE.md](BETRIEB_SQLITE.md).

| Schicht | Anschlussstelle | Aufgabe |
|---|---|---|
| Rechenkern | `SpeicherEngine/OptimiererOptionen.cs`, `SpeicherOptimierer.cs`, `PeakShaving.cs` | Größenraster, Betriebskosten, Netzanschlussbewertung |
| EPOS-Kern | `Controller/SpeicherAuslegungCtrl.cs` | Speicherung und spezifische Kosten aus Projektpositionen |
| EPOS-Kern | `Controller/SpeicherAuslegungCtrl.Rechnung.cs` | Eingabestand einfrieren, Quellen und Zeitachsen zusammenführen |
| EPOS-Kern | `Controller/SpeicherZeitreihenImport.cs` | Vorschau, Spaltenwahl, Prüfung und Normierung |
| EPOS-Kern | `Controller/SpeicherOptimierungCtrl.cs` | Kennzahlen, Diagramme und Exporte |
| Oberfläche | `Dialoge/Strom/SpeicherAuslegungEditor.razor`, `SpeicherZeitreihenDialog.razor` | Bedienung und Rückmeldungen |
| Windows | `SimulationErgebnisHuelle.Optimierung.cs` | Dateiauswahl, Speicherung und Hintergrundlauf |

Die Kandidatenrechnung greift nicht auf die Datenbank zu. Sie erhält einen unabhängigen Eingabestand mit bereits aufgelösten Kostensätzen und ausgerichteten Zeitreihen. Änderungen am Dialog verändern einen laufenden Kandidaten nicht.

Die Umsetzung optimiert die **aktive einzelne Speichervariante**. Eine gemeinsame Größenoptimierung mehrerer physisch parallel betriebener Speicher und nichtlineare Investitionskurven mit Stützstellen gehören weiterhin zum weiterführenden Konzept. Benannte Profile speichern in dieser Umsetzung die spezifischen Kostensätze samt vollständiger Auslegungskonfiguration.

### 14.5 Prüfung

Die Tests prüfen Rechenwerte, Zeitachsen, Kostenquellen, Speicherung und Bedienung. Die Ergebnisse stehen im begleitenden Umsetzungs- und Prüfbericht.

### 14.6 Ablauf der implementierten Auslegung

Vor dem Hintergrundlauf werden Quellen, Kostensätze und technische Parameter einmal gelesen und kopiert. Anschließend rechnet jeder Kandidat mit demselben Eingabestand. Die Reihenfolge ist:

```text
eingaben = Dialogeingaben.Kopie()
vorbereitet = SpeicherAuslegungCtrl.Vorbereiten(simulation, projektId, eingaben)
Speichern(projektId, anlageId, "@Aktuell", vorbereitet.Eingaben)
ergebnis = SpeicherOptimierungCtrl.Rechnen(vorbereitet, vorbereitet.Eingaben, ...)
```

Das Grobraster kombiniert Größenachse und C-Raten. Jeder Kandidat erhält eigene Leistung, Kapazität und skalierte SoC-Grenzen. Nach dem Viertelstundenlauf werden Investition, Betriebskosten und Erträge bewertet. Die optionale Feinphase bleibt innerhalb der eingegebenen Grenzen.

In der erweiterten Lastspitzenkappung steuert die Residuallast am Netzanschluss: Bruttolast abzüglich PV und gegebenenfalls BHKW. Die wirtschaftliche Bewertung verwendet die tatsächliche Änderung des Netzbezugs je Intervall und dessen Preis. Das bisherige Verhalten des Rechenkerns ohne neue Auslegungskonfiguration bleibt für vorhandene Aufrufer erhalten. Dies ist eine Rastersuche mit der gewählten Betriebsstrategie, kein Nachweis eines global optimalen Fahrplans für sämtliche Marktoptionen.

### 14.7 Nachrechenbare Abnahmebeispiele

| Eingabe | Erwartetes Ergebnis |
|---|---|
| 100 kWh, 50 kW; 300 €/kWh und 120 €/kW Investition | 36.000 € Investition |
| 3 €/(kWh·a) und 5 €/(kW·a) Betrieb bei derselben Größe | 550 €/a nach Kapazität und Leistung |
| Zusätzlich 20.000 kWh Entladung zu 0,01 €/kWh | Weitere 200 €/a; insgesamt 750 €/a Betrieb |
| 10 kW Bezug über 15 Minuten zu −0,05 €/kWh | −0,125 € variable Bezugskosten |
| 2,5 kWh Intervallenergie über 15 Minuten | 10 kW mittlere Leistung |
| 50 kW und C-Rate 0,5 1/h | 100 kWh Kapazität |

### 14.8 Verwendete Projektunterlagen

Abgeglichen wurden die EPOS-Konzepte zu Kosten, Energieträgern, Speicher, PV und Importkodierung. Für die Speicherung gelten BETRIEB_SQLITE.md und ADR-001_Schema-Ausrollung.md. Die Wiki-Ergänzung folgt Konzept_Hilfesystem_Wikidokumentation.md; EPOSPlan_Dokumentation_DesignSkizze.md wurde als Hintergrund eingesehen. Bestehende Wiki-Anker bleiben erhalten.

## 15 Vollständiger Mehrspeicherstand in EPOS Plan

Stand 11.09.2026. Der nachfolgende Implementierungsstand ergänzt die ursprünglichen Grundlagen und ersetzt die frühere Beschränkung der App-Anbindung auf eine Einzelvariante. Kapitel 14 bleibt als datierte Vorstufe nachvollziehbar.

Die aktuelle [Beschreibung von Konzept, Algorithmus und Implementierung](../Doku_Mehrspeicher_Konzept_und_Umsetzung.md) beschreibt physisch gleichzeitig betriebene Einheiten, fünf Betriebsziele, getrennte Verteilung, rollierende MILP-Planung, prognosegerechte Informationsstände, Jahreskonten, Kapitalwert, Rastersuche und Nullvariante.

Das [Word-Handbuch zum Mehrspeicherbetrieb](Speichersimulation/Mehrspeicher_EPOS_Plan.docx) erklärt die Bedienung und die abgestimmte Detaillierte Simulation. Ein Studienprofil und eine aktivierte Projektflotte bleiben getrennte Stände. Die Projektflotte ersetzt nach einer neuen Projektsimulation die elektrischen Netzflüsse; ihre Detailergebnisse werden im aufrufenden Stromspeicher-Reiter angezeigt.

