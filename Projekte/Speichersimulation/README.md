# Speichersimulation für EPOS-Plan

## Aktuelle Mehrspeicher-Dokumentation

Der aktuelle, in EPOS-Plan umgesetzte Mehrspeicherstand ist zuerst in **[Mehrspeicher_EPOS_Plan.docx](Mehrspeicher_EPOS_Plan.docx)** für Anwender und in **[Doku_Mehrspeicher_Konzept_und_Umsetzung.md](../../Doku_Mehrspeicher_Konzept_und_Umsetzung.md)** als prüfbare Konzept-zu-Code-Dokumentation beschrieben. Dort stehen Speicherflotte, Betriebsziele, Projektaktivierung, Ergebnisse, MILP, Wirtschaftlichkeit, Tests und bekannte Grenzen gemeinsam.

Die umfassende fachliche Grundlage bleibt **Speichersimulation_EPOS_Plan.docx**. **[Spezifikation_Stromspeicher_Optimierung.md](../Spezifikation_Stromspeicher_Optimierung.md)** enthält denselben Fachinhalt in einer für Entwicklung und Versionsverwaltung geeigneten Form. Kapitel 13 beschreibt die Integration in die tatsächlich eingesehenen Projekte SpeicherEngine, EPOS.Kern und EPOS.UI. Die Kosten-, Auslegungs- und Importfunktionen aus Kapitel 14 sind inzwischen im EPOS-Plan-Quellcode implementiert; der ursprüngliche Vorschlag in Kapitel 13 bleibt als fachlicher Hintergrund erhalten.

Der Ordner `code` enthält einen ausführbaren Python-Referenzkern mit drei Download-Adaptern, Mehrspeichermodell, Prognoseoptimierung und Kapitalwertfunktionen. Er ist eine fachliche Referenz für die C#-Umsetzung. Nicht implementierte Erweiterungen wie die laufende SoH-Rückwirkung sind in der Spezifikation ausdrücklich benannt.

## Erweiterter EPOS Dialog vom 11. September 2026

**Kapitel 14 der Spezifikation** und die eigenständig lesbare Datei **[Doku_Speicherauslegung_Kosten_Zeitreihen.md](../../Doku_Speicherauslegung_Kosten_Zeitreihen.md)** beschreiben Kosten aus dem Kostenmodul oder der Direkteingabe, editierbare Kostenprofile, dauerhaft gespeicherte Suchbereiche in kW oder kWh und C-Raten sowie unabhängige Last-, PV- und Preisquellen mit CSV-Spaltenauswahl. Relevante EPOS-Markdown-Dokumente und aktuelle Anschlussstellen wurden abgeglichen.

**Umsetzungsstand 11.09.2026:** Der EPOS-Dialog und seine C#-Anbindung sind implementiert. Die Bedienung und Grenzen stehen in EPOS_Dialog_Kosten_und_Zeitreihen.md. Die bestehende HTML-Auswertung und das Python-Beispiel besitzen diese zusätzlichen Dialogfunktionen noch nicht. Die Bedienung des vorhandenen Beispiels bleibt wie unten beschrieben.

## Ergebnisse grafisch ansehen und Parameter bearbeiten

**Grafik_starten.cmd doppelt anklicken.** Die Auswertung öffnet sich im Browser und enthält die vorhandenen eigenen Läufe sowie die mitgelieferten Beispiele. Alternativ kann die bereits erzeugte Datei **Auswertung.html** direkt geöffnet werden. Sie ist ein Stand der Ergebnisse; der Starter aktualisiert sie bei jedem Aufruf.

**Nach Parameteränderungen:** Die heruntergeladene `config_gui.json` zuerst im Ordner Speichersimulation speichern. Danach **Beispiel_neu_berechnen.cmd** doppelt anklicken. Dieser zweite Starter rechnet das synthetische Multi-Use-Beispiel neu, legt einen neuen Ergebnisordner an und öffnet die aktualisierte Grafik. Er verwendet ausdrücklich perfekte Zukunftsinformation. Für andere Daten oder Strategien gelten die in der Oberfläche erzeugten PowerShell-Befehle.

Die Auswertung bevorzugt gespeicherte GUI-Eingaben gegenüber der Beispielkonfiguration. Laufwahl und Peak-Legende nennen den tatsächlich berechneten Zielwert. Weichen aktuelle Eingaben davon ab, erscheint eine Meldung; bestehende Kurven bleiben ihren ursprünglichen Parametern zugeordnet.

Unter „Ergebnisse“ stehen Netz und PV, Speicherleistung, Ladezustand und Strompreise zur Auswahl. Ein Zeitraumfilter und der Zeitregler zeigen die Werte einzelner Viertelstunden. Unter „Eingabeparameter“ werden die Werte erklärt und können für einen neuen Lauf geändert werden. Die Oberfläche speichert eine neue `config_gui.json` per Download und zeigt die notwendigen PowerShell-Befehle. Die Datei muss vor der Berechnung im Ordner Speichersimulation abgelegt werden.

Die Diagramme rechnen nicht im Browser neu. Jeder neue Lauf verwendet den Python-Rechenkern und speichert seine tatsächliche Konfiguration als `config_snapshot.json`. Die Auswertung prüft deren Zuordnung; ohne passende Konfiguration zeigt sie Speicherenergie in kWh statt eines möglicherweise falschen prozentualen Ladezustands.

Die vollständige Erläuterung steht in **Eingabeparameter.md**. Besonders wichtig: Last, PV und effektive Preise kommen aus der CSV. Preisaufschläge aus der JSON wirken erst beim Aufbereiten dieser CSV mit `prepare_inputs.py`.

Manueller Start aus dem Ordner Speichersimulation:

```powershell
.\.venv\Scripts\python.exe code/visualize_results.py --open
```

Für die Darstellung sind keine zusätzlichen Python-Pakete und keine Internetverbindung erforderlich.

## Ausführen

Im Ordner dieses Dokuments mit Python 3.12 oder neuer:

```powershell
python -m pip install -r code/requirements.txt
python -m unittest discover -s code -p "test_*.py" -v
python code/run.py --inputs beispieldaten/synthetisch_3_tage.csv --config config_beispiel.json --strategy multi_use --oracle --out ergebnisse/multi_use
```

Weitere Strategien: `peak`, `pv_greedy`, `threshold`, `pv_predictive`, `arbitrage`. Prognoseverfahren benötigen eine nach Ausgabezeitpunkt gekennzeichnete Prognosedatei über `--forecasts` oder ein bewusstes `--oracle` für perfekte Zukunftsinformation. Reaktive Strategien benötigen dieses Flag nicht.

Die Beispieldaten sind drei fiktive Tage. Sie dienen zur Funktionsprüfung und werden nicht als gemessene Jahresdaten oder Investitionsnachweis ausgegeben. Die Beispielkonfiguration verwendet keinen Leistungspreis für diesen kurzen Testzeitraum. Ergebnisdifferenzen sind roh; die Anfangs- und Endenergie ist vor einer Investitionsbewertung zu normalisieren.

## Downloadquellen im Code

```powershell
python code/downloads.py prices --start 2025-01-01T00:00:00+01:00 --end 2026-01-01T00:00:00+01:00 --out data/preise_2025.csv --cache data/cache
python code/downloads.py pv-history --start 2025-01-01T00:00:00+01:00 --end 2026-01-01T00:00:00+01:00 --lat 52.52 --lon 13.405 --tilt 30 --azimuth 0 --kwp 10 --inverter-kw 10 --pr 0.85 --out data/pv_modell_2025.csv --cache data/cache
python code/downloads.py pv-forecast --lat 52.52 --lon 13.405 --tilt 30 --azimuth 0 --kwp 10 --out data/pv_prognose_stuetzstellen.csv --cache data/cache --refresh
```

Die Standortangaben sind ein Beispiel für Berlin. Endzeitpunkte sind exklusiv. Quellen sind Energy-Charts/Fraunhofer ISE mit Bundesnetzagentur/SMARD-Daten für DE-LU, Open-Meteo und Forecast.Solar. Vollständige Quellen-URLs stehen in `downloads.py` und im Fachkonzept. Originalantworten, Abrufzeit und Prüfsummen bleiben im lokalen Cache erhalten.

`pv-history` liefert eine aus historischer Einstrahlung abgeleitete PV-Referenz mit stündlicher Informationsauflösung. `pv-forecast` liefert aktuelle Watt-Stützstellen mit Ausgabezeitpunkt und Originalantwort. Diese Stützstellen müssen vor dem Simulationsimport noch in korrekt definierte Intervallmittelwerte überführt werden. Keine dieser Reihen wird als vergangene Originalprognose ausgegeben.

## Eigene Lastdaten verwenden

Die lokale Datei enthält `timestamp,load_kw` für die **Bruttolast**, mit Zeitstempel einschließlich UTC-Offset. Messwerte in kWh pro Viertelstunde werden vorab mit dem Faktor 4 in kW umgerechnet. Preise und PV müssen denselben vollständigen Zeitraum abdecken.

```powershell
python code/prepare_inputs.py --load data/standortlast_2025.csv --prices data/preise_2025.csv --pv data/pv_modell_2025.csv --config config_beispiel.json --out data/eingaben_2025.csv
python code/run.py --inputs data/eingaben_2025.csv --config config_beispiel.json --strategy peak --out ergebnisse/peak_2025
```

Speicher- und Tarifparameter vorher anpassen. Ohne `--pv` wird explizit eine Null-PV-Anlage verwendet. Der Zählerlastgang wird nicht automatisch aus öffentlichen nationalen Verbrauchsdaten ersetzt.

Die vollständige Jahresbewertung mit Alterung, Ersatzkosten und Preisannahmen ist in der Spezifikation definiert. `project_value.py` diskontiert dafür explizit vorgegebene Jahreskonten; es erzeugt keine Jahrescashflows durch Hochrechnung der drei Beispieltage.
