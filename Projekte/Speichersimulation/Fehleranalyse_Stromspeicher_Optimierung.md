# Fehleranalyse „Stromspeicher Optimierung“

**Prüfdatum:** 11.09.2026  
**Status:** Reale Fehlermeldung reproduziert; getrenntes Gültigszenario und Aktivierung auf einer SQLite-Arbeitskopie erfolgreich. Die Originaldatenbank wurde nicht verändert.

## 1. Prüfaufbau und Schutz des Originalstands

Der konfigurierte Standardweg wurde anhand von `DataRepository.GetDBPath()` und den Settings geprüft: Ein leeres `DBPath` fällt auf `%ProgramData%\EPOS_PLAN` zurück; `DBName` ist `Kenndaten.sqlite`. Verwendete Quelle:

`C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`

Die Quelle wurde mit SQLite `mode=ro` geöffnet und über die SQLite-Backup-API einschließlich WAL-Stand in diese Arbeitskopie gesichert:

`.work/realprojekt/Kenndaten_StromspeicherOptimierung_20260911_092816.sqlite`

`PRAGMA integrity_check` ergab für Original und Kopie jeweils `ok`. Das Original enthält nach der Prüfung unverändert nur Profil-ID 1, `@Aktuell`, Anlage 14993, Stand `2026-09-11T07:27:18.1475121+00:00`, Datenlänge 2316. Nur die Kopie erhielt beim Aktivierungstest zusätzlich `@Projektflotte` mit `ID_Energieanlage = NULL`. `@Aktuell` blieb auch in der Kopie unverändert.

Ausführbares Prüfharness:

`.work/RealFleetHarness/RealFleetHarness.csproj`

Der Harnesslauf endete mit Exitcode 0.

## 2. Unveränderter gespeicherter Nutzerstand

Projekt ID 1050 heißt **Stromspeicher Optimierung**. Der Stand `@Aktuell` ist an die aktive Einzelanlage 14993 gebunden und enthält:

| Feld | Gespeicherter Wert |
|---|---:|
| Flotteneinheiten | 1 |
| Kapazität | 129 kWh |
| Lade-/Entladeleistung | 100 / 100 kW |
| SoC min/start/max | 10 / 10 / 90 % |
| Betriebsziel | Peak Shaving |
| wirtschaftlicher Peak-Zielwert | 50 kW |
| Last/PV/Preis | EPOS / EPOS / EPOS |
| Modelljahr | 2026 |
| Projektlaufzeit | 20 Jahre |
| eingelesene Projektjahre | 0 |
| Referenzjahr ausdrücklich wiederholen | nein |
| Endbedingung | keine Vorgabe |
| Energie-Ausgleichswert | nicht gesetzt |
| Batterieexport | aus |

Der vollständige normale Projektlauf war erfolgreich. Er stellte jeweils 35.040 Viertelstundenwerte für Last, Reststrom und PV bereit. Auch `SpeicherAuslegungCtrl.Vorbereiten` war erfolgreich und lieferte 35.040 Intervalle.

## 3. Reproduzierter Fehler mit unveränderten Eingaben

Der anschließende Aufruf von `SpeicherFlottenStudieCtrl.Rechnen` brach mit dieser konkreten Ausnahme ab:

> **System.ArgumentException:** Die finanzielle Projektlaufzeit umfasst 20 Jahre, es wurde aber nur ein Referenzjahr bereitgestellt. Weitere Projektjahre müssen eingelesen oder die Referenzjahr-Wiederholung ausdrücklich gewählt werden.

Die Ausnahme entsteht in `SpeicherFlottenStudieCtrl.PruefeProjektjahresAbdeckung`. Sie folgt direkt aus der Kombination „20 Jahre“, „keine Projektjahre“ und „Referenzjahr nicht wiederholen“. Es wurden dafür keine Testparameter ergänzt.

Nach Behebung dieses ersten Fehlers wäre die Kombination „keine Endenergie-Vorgabe“ und fehlender Energie-Ausgleichswert der nächste Validierungsfehler. Auch dieser Wert darf nicht still angenommen werden.

## 4. Hinweise des realen Projektlaufs

Der Projektlauf meldete:

1. `Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10..`
2. Die Photovoltaik ist in diesem Lauf nicht aufgenommen; Einspeisung und Ladung aus Erzeugung bleiben daher ohne Aussage.
3. Die Einzelvariante steht auf Kostenprofil, hat aber kein Profil gewählt; der Altpfad rechnet mit dem Fixpreis.
4. Zum Stichtag 01.01.2026 existiert keine Preisversion; die älteste vorhandene Version wird verwendet.
5. Diese Preisversion hat Arbeitspreis 0; der Altpfad fällt schließlich auf 20 ct/kWh zurück.

### Erklärter Preisaufbau

`Auslegung.Preisquelle` ist `Epos`. Die Flottenvorbereitung verwendet konstant **31,746 ct/kWh**. Dieser Wert ist rechnerisch nachvollziehbar als 20,000 ct/kWh Arbeitspreis-Rückfall plus 11,746 ct/kWh Aufschlagsvorgaben:

- Netzentgelt 6,440 ct/kWh
- Umlagen 2,946 ct/kWh
- Stromsteuer 2,050 ct/kWh
- Konzession 0,110 ct/kWh
- Vertrieb 0,200 ct/kWh

Für Projekt 1050 und Strom-Carrier 60 existiert eine `energy_project_settings`-Zeile. Ihre fünf Aufschlagswerte sowie Modus und Override sind `NULL`; nach der dokumentierten NULL-Semantik bleiben deshalb die Modellvorgaben wirksam. Der Hinweis „Rückfallwert 20 ct/kWh“ bezeichnet ausdrücklich den Energieanteil der vorausgehenden Legacy-Vorbereitung. Der Flotteneingang ergänzt korrekt 11,746 ct/kWh Aufschläge und verwendet deshalb 31,746 ct/kWh. Die beiden Werte widersprechen sich nicht.

## 5. Getrenntes Gültigszenario auf der Kopie

Die folgenden Werte sind ausdrücklich **Testannahmen und keine gespeicherten Nutzereingaben**:

1. Das vorhandene Referenzjahr wird ausdrücklich 20-mal wiederholt.
2. Der Energie-Ausgleich wird aus dem tatsächlichen Flotteneingang abgeleitet. Minimum, Maximum und Mittelwert sind jeweils 31,746 ct/kWh; verwendet wurden 0,31746 EUR/kWh.

`@Aktuell` wurde nicht überschrieben. Das Gültigszenario wurde zunächst nur im Arbeitsspeicher gerechnet.

### Technisches Ergebnis

| Größe | Referenz ohne Flotte | Flottenvariante |
|---|---:|---:|
| Netzbezug | 2.850.198,4 kWh | 2.850.198,4 kWh |
| Bezugsspitze | 789,36 kW | 789,36 kW |
| Endenergie | – | 12,9 kWh |
| technisch zulässig | ja | ja |

Die Flotte verändert Bezug und Peak nicht. Im realen Projektlauf ist keine PV-Erzeugungsreihe aufgenommen, Netzladung ist nicht erlaubt und der Speicher startet bereits bei SoC min.

### Kosten und Kapitalwert

Die zuvor verkürzt als „Kosten“ gemeldeten 999.547,184064 EUR sind **Gesamtrechnungskosten**, nicht reine Energiekosten:

| Kostenfeld | Referenz | Flottenvariante |
|---|---:|---:|
| `EnergiekostenEuro` | 904.823,984064 EUR | 904.823,984064 EUR |
| `LeistungskostenEuro` | 94.723,200000 EUR | 94.723,200000 EUR |
| `FixkostenEuro` | 0 EUR | 0 EUR |
| `GesamtEuro` | 999.547,184064 EUR | 999.547,184064 EUR |

Weitere Ergebnisse:

- Endenergie-Ausgleich: 0 EUR
- Investition: 15.000 EUR
- OPEX: 100 EUR/Jahr
- Durchsatzkosten: 0 EUR/Jahr
- Ersatzkosten: 0 EUR/Jahr
- Netto-Cashflow Jahr 1: -100 EUR
- Kapitalwert über 20 ausdrücklich wiederholte Jahre: **-16.487,747486 EUR**

Dieser Kapitalwert gehört nur zum beschriebenen Testszenario und ist kein Ergebnis des unveränderten Nutzerstands.

## 6. Aktivierung und vollständiger Projektlauf nur auf der Kopie

Das Gültigszenario wurde anschließend ausschließlich auf der Arbeitskopie als projektgebundenes `@Projektflotte` aktiviert. Der vollständige Projektlauf war erfolgreich.

| Bilanzgröße | Ergebnis |
|---|---:|
| Intervalle | 35.040 |
| Netzbezug | 2.850.198,4 kWh |
| Gesamtexport | 0 kWh |
| PV-Export | 0 kWh |
| BHKW-Export | 0 kWh |
| Batterieexport | 0 kWh |
| PV-Abregelung | 0 kWh |
| maximale Abweichung allgemeiner Reststrom zu Flottenbezug | 0 kW |
| maximale Abweichung Gesamtexport zu PV + BHKW + Batterie | 0 kW |

Damit ist für diesen realen Projektstand belegt, dass `Rest_Strombedarf_viertelstuendlich` dem Flotten-Netzbezug entspricht und die Exportkomponenten nicht doppelt in die Gesamteinspeisung eingehen. Weil dieses Projekt keine Erzeugung exportiert, ist die Quellaufteilung hier ein konsistenter Nullfall. Der getrennte Zwei-Speicher-Test `SpeicherFlottenNetzbilanzTests` deckt zusätzlich den nichttrivialen Import-/Exportfall mit PV-, BHKW- und Batterieexport ab.

## 7. Ergebnis für die Fehlerbehebung

Der aktuell gespeicherte Nutzerstand kann fachlich nicht gerechnet werden, weil für 20 Projektjahre weder 20 Jahresdatensätze noch eine ausdrückliche Wiederholung des Referenzjahres gewählt sind. Danach fehlt zusätzlich eine explizite Endenergiebehandlung. Die Oberfläche muss diese beiden Eingaben verständlich benennen und den echten Ausnahmeinhalt anzeigen. Sie darf weder `_fehler` als Literal ausgeben noch Jahreswiederholung oder Energiepreis still annehmen.
