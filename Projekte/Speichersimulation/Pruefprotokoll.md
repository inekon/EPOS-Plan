# Prüfprotokoll

Stand: 10. September 2026.

Der Python-Referenzkern wurde mit Python 3.12, PuLP 3.3.0 und dem enthaltenen CBC-Solver sowie rainflow 3.2.0 geprüft.

| Prüfung | Ergebnis |
|---|---|
| Automatische Fachtests | 28 bestanden, keine Fehler |
| Integration mit zwei Speichern | Fünf Strategien über jeweils 72 Stunden ausgeführt |
| Solver im Integrationsbeispiel | Keine Rückfälle bei PV-Planung, Arbitrage und Multi Use |
| Energy-Charts | 96 Preisintervalle für den 1. Oktober 2025 erfolgreich geladen |
| Open-Meteo | 96 modellierte PV-Intervalle für den 1. Juni 2025, Beispiel Berlin |
| Forecast.Solar | 30 aktuelle Prognosestützstellen abgerufen, Beispiel Berlin |
| Word-Dokument | 26 Seiten gerendert und visuell geprüft |

Die einzelnen Testnamen und die Ergebnisse der Strategieläufe stehen in `Pruefprotokoll.json`. Die Zeitreihen und Solverprotokolle liegen unter `beispielergebnisse`.

Die drei Tage Eingabedaten sind synthetisch. Optimierte Beispielläufe verwenden ausdrücklich perfekte Zukunftsinformation (`--oracle`). Ergebnisse sind keine Jahresbewertung; unterschiedliche Endenergien der reaktiven Strategien verhindern einen unmittelbaren Wirtschaftlichkeitsvergleich ihrer Rohkosten.

Die Tests prüfen insbesondere Energieerhaltung, Wirkungsgrade, Leistungs- und Energiegrenzen, Reserven, Verteilung auf mehrere Speicher, Netzfreigaben, Restpeaks, Abregelung, negative Preise, Zeitumstellung, Datenlücken, Zukunftsdaten, chronologische Optimierung und Kapitalwertrechnung. Sie ersetzen keine Validierung mit Standortmessungen.

Die aktuelle Forecast.Solar-Datei enthält Watt-Stützstellen. Deren Umrechnung in Prognose-Intervallmittelwerte und die Erzeugung einer historischen Prognosedatei mit belegtem Ausgabezeitpunkt sind bewusst getrennte Arbeitsschritte.

## Ergänzung: grafische Auswertung

Die Erweiterung vom 10. September 2026 wurde zusätzlich in der vorhandenen Python-3.14.5-Umgebung geprüft: **31 Tests bestanden**, einschließlich der 28 bisherigen Fachtests und drei Tests zur korrekten Parameterzuordnung der Diagramme. Der Rechenkern speichert nun die tatsächlich verwendeten Konfigurationsbytes mit passender Prüfsumme.

Im Browser wurden sechs vorhandene Läufe, alle vier Diagrammarten, Intervallende bei SoC, Tagesauswahl, Konfigurationsexport mit Prozentumrechnung und Eingabeprüfung sowie die Ausgabe der Startbefehle geprüft. Die Prüfung umfasste eine schmale Ansicht mit 390 Pixeln Breite und die Darstellung von 35.040 Intervallen. Es wurden keine Browserfehler oder horizontalen Seitenüberläufe festgestellt. Das zugehörige maschinenlesbare Protokoll heißt `Pruefprotokoll_Grafik.json`.

Die anschließende Regression zur Peak-Zieländerung prüft 50 kW in Eingabe, berechneter Konfiguration und sämtlichen Punkten der Ziellinie. Ein älterer Lauf mit 80 kW bleibt korrekt als solcher gekennzeichnet; eine noch nicht berechnete Änderung auf 40 kW löst einen sichtbaren Hinweis aus. Gespeicherte GUI-Eingaben werden beim Start bevorzugt. Nach dieser Ergänzung bestehen insgesamt **32 Tests**. `Beispiel_neu_berechnen.cmd` wurde mit der gespeicherten 50-kW-Konfiguration ausgeführt.

Der EPOS-Plan-Vorschlag wurde mit ausgewählten lokalen Quelldateien abgeglichen. Die Bestandsanwendung wurde für diese Dokumentation weder geändert noch ausgeführt.
