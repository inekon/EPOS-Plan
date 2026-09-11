# Eingabeparameter und grafische Auswertung

## 1 Start der Diagramme

`Grafik_starten.cmd` im Ordner Speichersimulation doppelt anklicken. Der Starter sammelt die Ergebnisse aus `ergebnisse` und `beispielergebnisse`, erzeugt `Auswertung.html` und öffnet sie im Standardbrowser. Der jüngste eigene Lauf erscheint zuerst. Eine bereits geöffnete HTML-Datei enthält ihren gespeicherten Stand und überwacht keine Dateien.

Die Darstellung funktioniert vollständig lokal und benötigt keine zusätzlichen Pakete. Alternativ kann die erzeugte HTML-Datei direkt geöffnet werden.

| Diagramm | Was ist zu sehen? |
|---|---|
| Netz und PV | Netzleistung mit und ohne Speicher, verfügbare PV und gegebenenfalls Peak-Ziel |
| Speicherleistung | Leistung jeder Einheit; positiv entladen, negativ laden |
| Ladezustand | SoC je Einheit in Prozent; ohne bestätigte Kapazitätszuordnung stattdessen kWh |
| Strompreise | Tatsächlich verwendeter Bezugspreis und Einspeisepreis aus der CSV in ct/kWh |

Die Zeitangaben werden in der Standortzeitzone angezeigt. Energiewerte gehören zum Intervallende, Leistungen zum Intervallbeginn. Der Zeitregler ist auch ohne Maus-Hover verwendbar. Bei langen Zeitreihen verdichtet die Zeichnung die Punkte unter Erhalt der lokalen Minima und Maxima; Einzelwerte und Kennzahlen kommen aus den vollständigen Daten.

## 2 Drei Arten von Eingaben

| Eingabe | Datei oder Ort | Wirkt wann? |
|---|---|---|
| Speicher, Anschluss, Regelung, Planung, Leistungspreis | `config_beispiel.json` oder eigene Konfiguration | Beim Aufruf von `run.py` |
| Last, PV, effektive Bezugs- und Einspeisepreise | Eingabe-CSV | Direkt in jedem Simulationsintervall |
| Strategie und Prognosemodus | Aufrufparameter `--strategy`, `--oracle` oder `--forecasts` | Beim Start des Laufs |
| Börsenpreisfaktoren und Preisaufschläge | Tarifbereich der Konfiguration | Beim Erzeugen der Eingabe-CSV mit `prepare_inputs.py` |
| Standort, PV-Größe, Neigung, Azimut, Wechselrichtergrenze, PR | Aufruf von `downloads.py pv-history` | Beim Erzeugen der PV-Zeitreihe |

Eine Änderung an der JSON verändert weder bestehende Ergebnisse noch automatisch die Werte einer vorhandenen Eingabe-CSV.

## 3 Beispielwerte der beiden Speicher

| Parameter im Code | Bedeutung | Speicher A | Speicher B |
|---|---|---:|---:|
| `capacity_kwh` | Gesamte interne Kapazität | 100 kWh | 50 kWh |
| `charge_kw` | Maximale Ladeleistung, AC | 50 kW | 25 kW |
| `discharge_kw` | Maximale Entladeleistung, AC | 50 kW | 25 kW |
| `eta_charge` | Ladewirkungsgrad | 0,95 = 95 % | 0,94 = 94 % |
| `eta_discharge` | Entladewirkungsgrad | 0,95 = 95 % | 0,94 = 94 % |
| `soc_min` | Technischer Mindestladezustand | 0,10 = 10 % | 0,10 = 10 % |
| `soc_max` | Technischer Höchstladezustand | 0,90 = 90 % | 0,90 = 90 % |
| `initial_soc` | Ladezustand zu Beginn | 0,50 = 50 % | 0,50 = 50 % |
| `reserve_kwh` | Zusätzliche geschützte Peak-Energie über dem Minimum | 0 kWh | 0 kWh |
| `aux_kw` | Konstante zusätzliche AC-Hilfsleistung | 0,04 kW = 40 W | 0,03 kW = 30 W |
| `wear_eur_per_kwh_out` | Grenzverschleiß je abgegebener AC-kWh | 0,03 €/kWh | 0,04 €/kWh |

Damit ergeben sich **80 + 40 = 120 kWh nutzbarer Energiebereich** bei 150 kWh Gesamtkapazität. Zu Beginn befinden sich 50 + 25 = 75 kWh in den Speichern. Die Summe der Leistungsgrenzen beträgt 75 kW; die tatsächlich verfügbare Leistung hängt zusätzlich von Energiezustand, Reserven und Anschlussgrenzen ab.

Der Round-Trip-Wirkungsgrad beträgt 0,95 × 0,95 = 90,25 % beziehungsweise 0,94 × 0,94 = 88,36 %. Er ist nicht identisch mit einem einzelnen Richtungswirkungsgrad. Die Oberfläche zeigt Wirkungsgrade und Ladezustände in Prozent an; beim Speichern werden sie korrekt in Bruchteile umgerechnet.

Der Grenzverschleiß beeinflusst die Fahrplanentscheidung. Er erscheint nicht zusätzlich als erfundener Posten der Stromrechnung. Für die langfristige Projektbewertung werden tatsächliche Investitionen, Ersatzinvestitionen und Betriebskosten verwendet.

## 4 Anschluss und Regelung

| Parameter | Beispielwert | Bedeutung |
|---|---:|---|
| `connection.grid_charge` | true | Laden aus dem Netz zugelassen |
| `connection.battery_export` | false | Einspeisung aus der Batterie nicht zugelassen; direkte PV-Einspeisung getrennt erlaubt |
| `connection.import_limit_kw` | 200 kW | Technische Bezugsgrenze |
| `connection.export_limit_kw` | 50 kW | Technische Einspeisegrenze; überschüssige PV wird abgeregelt |
| `control.peak_target_kw` | 80 kW | Wirtschaftliches Peak-Ziel für Peak Shaving und Multi Use |
| `control.allocation` | balanced | Reaktive Verteilung nach verfügbarer Energie bzw. freiem Energieplatz |

Alternativen zur Verteilung sind `cascade` (nacheinander auslasten, Reihenfolge je UTC-Tag drehen) und `merit` (nach Grenzverschleiß und Wirkungsgrad). Bei der Prognoseoptimierung wird die Leistung je Speicher als eigene Variable geplant; das reaktive Verteilfeld überschreibt diesen Plan nicht.

Die technische Anschlussgrenze und das wirtschaftliche Peak-Ziel sind verschiedene Werte. Ein zu kleiner Speicher kann den Zielwert verfehlen. Das Ergebnis dokumentiert die verbleibende Netzspitze.

## 5 Betriebsstrategien

| `--strategy` | Arbeitsweise | Relevante Besonderheiten |
|---|---|---|
| `peak` | Reaktive Peak-Kappung und Wiederaufladung unterhalb des Ziels | Peak-Ziel, Verteilung und Netzladefreigabe wirken |
| `pv_greedy` | Überschuss sofort laden, Netzbedarf sofort aus Speicher decken | Für den reinen PV-Vergleich Netzladung und Batterieexport ausschalten |
| `pv_predictive` | Chronologische PV-Optimierung mit Prognose | Netzladung und Batterieexport werden ausgeschaltet; Prognosen oder `--oracle` nötig |
| `arbitrage` | Optimierung von Energiepreis und Grenzverschleiß | Peak-Ziel ist nicht aktiv; Prognosen oder `--oracle` nötig |
| `multi_use` | Peak zuerst, unter dieser Vorgabe Kosten optimieren | Peak-Ziel und Leistungspreis wirken; Prognosen oder `--oracle` nötig |
| `threshold` | Einfacher Preisschwellenregler als Vergleich | Verwendet im Referenzcode 0,10 €/kWh zum Laden und 0,30 €/kWh zum Entladen; diese Schwellen sind derzeit kein JSON-Eingabefeld |

`--oracle` bedeutet perfekte Kenntnis der zukünftigen Last-, PV- und Preiswerte. Es ist ein Vergleichsmodus, keine reale Wetter- oder Lastprognose. Eine reale Prognosedatei enthält zusätzlich zu den Eingangsspalten `decision_time` und `known_at`.

## 6 Prognoseplanung

| Parameter | Beispielwert | Umrechnung |
|---|---:|---|
| `planning.horizon_steps` | 192 | 48 Stunden Prognosehorizont |
| `planning.replan_steps` | 96 | Alle 24 Stunden neu planen |
| `planning.solver_time_limit_s` | 60 | Zeitlimit pro Solver-Aufruf |

Die Schrittzahlen müssen positive ganze Zahlen sein; Neuplanung höchstens nach einem vollständigen Horizont. Ein Schritt entspricht 0,25 Stunden. Der Endenergie-Zielwert je Horizont wird aus `initial_soc × capacity_kwh` gebildet. Fehlt eine gültige Prognose oder eine optimale Solverlösung, wird die dokumentierte Ersatzstrategie ausgeführt und protokolliert.

## 7 Tarife und Zeitreihen

`tariff.demand_eur_kw_period = 0` schaltet im dreitägigen Funktionstest den Leistungspreis aus. Bei einem realen Jahrestarif wird ein vollständiges Abrechnungsjahr simuliert und der gültige Preis in €/kW je Jahr eingesetzt. Der Basiskern berechnet eine einzige Abrechnungsperiode; ein Monatsmodell muss getrennte Monatsmaxima bilden. `billing_scope` beschreibt den Zeitraum, erzeugt ihn aber nicht automatisch.

Für die Preisaufbereitung gelten im Beispiel:

```text
Bezugspreis = Börsenpreis × buy_spot_factor + buy_addition_eur_kwh
            = Börsenpreis × 1,0 + 0,18 €/kWh

Einspeisepreis = fixed_export_eur_kwh = 0,08 €/kWh
```

Nur wenn `fixed_export_eur_kwh` auf `null` steht, verwendet die Aufbereitung stattdessen `Börsenpreis × sell_spot_factor + sell_addition_eur_kwh`. Die mitgelieferte synthetische CSV besitzt bereits eigene effektive Preise. Die vorstehenden Parameter rechnen diese CSV nicht nachträglich um.

Die Eingabe-CSV verwendet genau diese Pflichtspalten:

```csv
timestamp,load_kw,pv_kw,buy_eur_kwh,sell_eur_kwh
2025-06-01T00:00:00+00:00,40,0,0.12,0.08
```

`timestamp` ist der Intervallbeginn mit Zeitzonenoffset. `load_kw` ist die Bruttolast, `pv_kw` verfügbare AC-PV-Leistung. Preise sind in €/kWh, auch negative Preise sind zulässig. Daten müssen lückenlos, geordnet und vollständig sein. Bei bereits um PV verminderten Messwerten muss zuerst die Bruttolast rekonstruiert werden.

Die Zeitzone der Konfiguration steuert die Anzeige in der Auswertung. Der Rechenkern verwendet UTC-Zeitstempel; eine Änderung des Zeitzonenfelds verschiebt die Eingabedaten nicht.

## 8 Werte ändern und neu rechnen

1. In der Auswertung „Eingabeparameter“ öffnen und Werte ändern.
2. „Konfiguration herunterladen“ wählen und die Datei als `config_gui.json` in Speichersimulation ablegen. Wenn der Browser automatisch in Downloads speichert, die Datei von dort kopieren.
3. Für das synthetische Multi-Use-Beispiel `Beispiel_neu_berechnen.cmd` doppelt anklicken. Für andere Strategien, Zeitreihen oder echte Prognosen die angezeigten PowerShell-Befehle ausführen. Beide Wege legen einen neuen Ergebnisordner an.
4. Die neu erzeugte Auswertung zeigt das neue Ergebnis. Die alten Läufe bleiben zum Vergleich erhalten.

Neue Simulationsläufe speichern die exakt verwendete Konfiguration als `config_snapshot.json`. Ihre Prüfsumme muss zum Ergebnis passen. Dadurch bleiben frühere Kurven ihren tatsächlich verwendeten Kapazitäten und Grenzen zugeordnet, wenn später eine andere Konfiguration bearbeitet wird.

Die Oberfläche bereitet Eingaben vor und stellt Ergebnisse dar. Der fachliche Rechenkern bleibt Python; die im Browser geänderten Werte werden erst nach dem tatsächlichen Simulationslauf zu neuen Diagrammen.

`Grafik_starten.cmd` aktualisiert nur die Anzeige und führt keine Simulation aus. Das Ziel in der Peak-Legende gehört zum ausgewählten gespeicherten Lauf. Bei abweichenden Eingaben zeigt die Auswertung eine Meldung mit altem und neuem Peak-Ziel. Ein bereits geöffneter Browser-Reiter muss nach dem Neuerzeugen der HTML-Datei neu geladen werden.

## 9 Implementierter EPOS Dialog mit Kosten und separaten Zeitreihen

Die implementierte EPOS-Erweiterung ist beschrieben in [EPOS_Dialog_Kosten_und_Zeitreihen.md](EPOS_Dialog_Kosten_und_Zeitreihen.md) und Kapitel 14 der Spezifikation: Kostenmodul oder direkte Sätze, gespeicherte Profile und Auslegungsbereiche sowie getrennte CSV-Dateien mit Zeit- und Wertspaltenauswahl für Last, PV und effektive Bezugspreise. Die bisherige HTML-Auswertung unterstützt diese neuen Felder noch nicht.

Bei den neuen Betriebskosteneingaben werden EUR je kWh installierter Kapazität pro Jahr und EUR je tatsächlich entladener kWh ausdrücklich unterschieden. Eine importierte Reihe buy_eur_kwh enthält bereits effektive variable Bezugspreise und darf deshalb nicht nochmals mit Tarifaufschlägen versehen werden.
