# Reale Projektintegration „Stromspeicher Optimierung“

Stand: 11.09.2026, Test auf isolierter SQLite-Arbeitskopie.

## Datensicherheit und Quelle

- Der Standardpfad folgt `DataRepository.GetDBPath()`: leeres `DBPath` fällt auf `%ProgramData%\EPOS_PLAN` zurück; `DBName` ist `Kenndaten.sqlite`.
- Gelesene Quelle: `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`.
- Arbeitskopie: `C:\Users\DirkEngelmann\Documents\WP-Plan\.work\realprojekt\Kenndaten_StromspeicherOptimierung_20260911_092816.sqlite`.
- Die Kopie entstand mit der SQLite-Backup-API aus einer im Modus `mode=ro` geöffneten Quelle und schließt damit den WAL-Stand konsistent ein.
- `PRAGMA integrity_check` meldete für Original und Kopie `ok`.
- Das Original wurde ausschließlich lesend geöffnet. Es enthält nach dem Test unverändert nur Profil-ID 1, `@Aktuell`, Anlage 14993, Stand `2026-09-11T07:27:18.1475121+00:00`, Datenlänge 2316.
- Nur die Arbeitskopie erhielt durch das Aktivierungsszenario zusätzlich `@Projektflotte` mit `ID_Energieanlage = NULL`. `@Aktuell` blieb auch dort unverändert.

## Unveränderter Nutzerstand

- Projekt: ID 1050, `Stromspeicher Optimierung`.
- Aktive Einzelanlage: ID 14993.
- `@Aktuell`: eine Flotteneinheit, Growatt WIT-M+APX ESS, 129 kWh, 100 kW Laden, 100 kW Entladen.
- SoC min/start/max: 10 % / 10 % / 90 %.
- Betriebsziel: Peak Shaving, Zielwert 50 kW.
- Quellen: Last EPOS, PV EPOS, Preis EPOS; 35.040 Viertelstunden.
- Finanzielle Laufzeit: 20 Jahre.
- Eingelesene Projektjahre: 0.
- `ReferenzjahrExplizitWiederholen`: `false`.
- Endbedingung: keine Vorgabe.
- Energie-Ausgleichswert: `null`.

Der normale Projektlauf war erfolgreich. Die Flottenvorbereitung war ebenfalls erfolgreich und lieferte 35.040 Intervalle. Erst der Studienlauf brach ab:

> System.ArgumentException: Die finanzielle Projektlaufzeit umfasst 20 Jahre, es wurde aber nur ein Referenzjahr bereitgestellt. Weitere Projektjahre müssen eingelesen oder die Referenzjahr-Wiederholung ausdrücklich gewählt werden.

Ursprung: `SpeicherFlottenStudieCtrl.PruefeProjektjahresAbdeckung`. Der Fehler entsteht aus dem gespeicherten Nutzerstand und wurde nicht durch Testwerte erzeugt. Nach dessen Behebung wäre die Kombination „keine Endenergie-Vorgabe“ und Energie-Ausgleichswert `null` ein weiterer Konfigurationsfehler.

Projektlaufhinweise:

- Zeitbasis Klimadaten: UTC nach MEZ/MESZ, Referenzjahr 2025, Umstellung 30.03./26.10.
- Im Projektlauf wurde keine PV-Erzeugungsreihe aufgenommen.
- Die Einzelvariante verweist auf ein Kostenprofil, ohne ein Profil auszuwählen; der Altpfad meldet den Fixpreis-Rückfall.
- Zum 01.01.2026 fehlt eine Preisversion; der Altpfad nennt schließlich 20 ct/kWh als Rückfallwert.
- Die neue Flottenvorbereitung liefert für ihren tatsächlichen Eingang konstant 31,746 ct/kWh. Diese beiden Werte stammen aus verschiedenen Preiszuführungen und wurden im Bericht getrennt gehalten.

## Zusätzliches, ausdrücklich angenommenes Gültigszenario

Nur im Arbeitsspeicher beziehungsweise in `@Projektflotte` der Arbeitskopie wurden gesetzt:

1. Das vorhandene Referenzjahr wird ausdrücklich 20-mal wiederholt.
2. Der Energie-Ausgleich wird aus dem tatsächlichen Flotteneingang abgeleitet: Mittelwert = Minimum = Maximum 31,746 ct/kWh, also 0,31746 EUR/kWh.

`@Aktuell` und die Originaldatenbank wurden dabei nicht geändert.

Ergebnis:

| Größe | Referenz | Flotte |
|---|---:|---:|
| Netzbezug | 2.850.198,4 kWh | 2.850.198,4 kWh |
| Bezugsspitze | 789,36 kW | 789,36 kW |
| Energiekosten | 999.547,184064 EUR | 999.547,184064 EUR |
| Endenergie | – | 12,9 kWh |

- Technisch zulässig: ja.
- Endenergie-Ausgleich: 0 EUR.
- Investition: 15.000 EUR.
- OPEX: 100 EUR/Jahr.
- Durchsatzkosten und Ersatzkosten: 0 EUR.
- Jahres-Cashflow Jahr 1: -100 EUR.
- Kapitalwert über 20 wiederholte Jahre: -16.487,747486 EUR.

Die Flotte ändert Last und Peak in diesem Projektstand nicht: Es gibt keine aufgenommene PV-Erzeugungsreihe, Netzladung ist nicht erlaubt und der Speicher startet bereits bei SoC min.

## Aktivierter echter Projektlauf auf der Kopie

Das gültige Testszenario wurde als projektgebundenes `@Projektflotte`-Profil auf der Arbeitskopie aktiviert. Der anschließende vollständige Projektlauf war erfolgreich.

- 35.040 Flottenintervalle.
- Netzbezug: 2.850.198,4 kWh.
- Gesamtexport, PV-Export, BHKW-Export, Batterieexport und PV-Abregelung: jeweils 0 kWh.
- Maximale Abweichung zwischen allgemeinem `Rest_Strombedarf_viertelstuendlich` und `Speicherflottennetzbilanz.NetzbezugKw`: 0 kW.
- Maximale Abweichung zwischen Gesamtexport und PV + BHKW + Batterie: 0 kW.

Damit ist für diesen realen Projektstand belegt, dass der allgemeine Netzbezug aus der Flottenbilanz kommt und die Exportquellen nicht doppelt summiert werden. Da das Projekt keine Erzeugung exportiert, ist die Quellaufteilung hier ein konsistenter Nullfall; der separate synthetische Zwei-Speicher-Test deckt den nichttrivialen Exportfall ab.

## Reproduktion

Harness: `C:\Users\DirkEngelmann\Documents\WP-Plan\.work\RealFleetHarness\RealFleetHarness.csproj`.

Der Lauf wurde mit `BuildProjectReferences=false` gegen den zuvor fertig gebauten Kern ausgeführt. Exitcode: 0.
