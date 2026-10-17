# EB-1 — Ergebnisreihen im Kern: Schemaschritt 239, Lauf, Energieflussbild, Testdatenbank 239, Basis R59

Sitzung Dialoge und Korrekturen, 17.10.2026, Zweig `eb1` auf `origin/ios_migration_september` (`17a7b985a`). Konzept:
[`2026-10-17_Konzept_Ergebnisreihen_Einspeisung_Standby_Abregelung.md`](../../../aktuell/Dialoge/2026-10-17_Konzept_Ergebnisreihen_Einspeisung_Standby_Abregelung.md);
Anlass: Konzept Energieflussbild, Abschnitt 13 (optionale Ergebnisspalten) und die Anwenderentscheide vom 17.10.2026.
Nicht gepusht.

## Befund

- BHKW-Einspeisung: nur `Tab_ErgebnisStromMatrix.KwkEinspMWh` (geschrieben allein von `WirtschaftlichkeitCtrl.Persistiere`)
  und die Laufreihe `BHKW_UEBERSCHUSS`; Standby des Speichersystems (`SimulationControl.SpeichersystemEigenverbrauchKwh`)
  nur im Referenzlauf-Export; PV-Abregelung nur als Laufreihe `PV_ABREGELUNG`, `KennzahlenKatalog.AbregelungMwh` ohne Reihe 0.
  Ergebniszeitreihen werden nicht persistiert (AP0); der `ZeitreihenSatz` trägt alle drei Reihen schon.
- R58 war im Statuskopf von der Sitzung Kostenbasis angemeldet (16.10.2026 23:21 UTC), „nächste freie Basis R59“ — die
  neue Basis heißt deshalb R59, nicht R58.
- Auftragsänderung während der Welle (Anwender, Punkt 3): kein Schalter für die Abregelung im Energieflussbild; sie ist
  Teil der Bilanz und erscheint als grauer Abzweig „abgeregelt“ an der PV-Quelle nach der Kleinverlustregel (EB-Q1,
  entschieden 17.10.2026); zuschaltbar (Vorgabe aus) wird sie in den Zeitreihen-Grafiken (EB-2).

## Schritt 239 in der Kette

- `ErgebnisreihenSchema.SCHRITT = KlimaRohwerteSchema.SCHRITT + 1` (239 hängt an 238); `Stromeinspeisung` an
  `Tab_ErgebnisBHKW` [MWh/a], `Abregelung` an `Tab_ErgebnisPhotovoltaik` [MWh/a], `Eigenverbrauch_System` an
  `Tab_ErgebnisStromspeicher` [kWh/a]; `REAL`, nullbar, `CHECK (x IS NULL OR x >= 0)`, NULL = nicht erhoben; kein DML.
- Register: `SchemaMigration.SCHRITTE_SQLITE` (`SCHRITT_ERGEBNISREIHEN`, Methode `Schritt_Ergebnisreihen`),
  `Paketanhebung.STUFEN` (Art Ddl), `SchemaStand.Zielversion = 239`, `TestDatenbank.cs`, `Werkzeuge/Testdatenbankschema`.
  Keine neue Tabelle, `VorlageTests` P6 unverändert (198 `STRICT`-Tabellen).
- Testdatenbank mit `Werkzeuge/Testdatenbankschema` aus der origin-Fassung 238 (`20c0a3fe…`) gehoben: 3 Spalten; Objektliste
  (198 Tabellen, Indizes) und Zeilenzahl jeder Tabelle gleich; `integrity_check` ok, `foreign_key_check` leer;
  109 608 960 Byte, LFS-SHA-256 `2fb633f6da4fc4242d8b81b8dd82fd0ffc31c9fb129a171dd93faca2d7828433`. Nachtrag in
  `Referenzlaeufe/LIESMICH.md`. `SqlDialektPruefer`: 3 056 SQL-Texte, 0 Fundstellen.

## Lauf, Modell, Lesewege

- `ErgebnisBHKWModel.StromeinspeisungMwh`, `ErgebnisPhotovoltaikModel.AbregelungMwh`,
  `ErgebnisStromspeicherModel.EigenverbrauchSystemKwh` (alle `double?`). `SimulationRunner.BaueErgebnis` belegt sie aus
  `SimulationErgebnisCtrl.BhkwEinspeisungMwh(sim)`, dem neuen Helfer `SimulationErgebnisCtrl.PvAbregelungMwh(sim)` (den auch
  der Reiter `Photovoltaik(sim)` nimmt — eine Formel) und `sim.SpeichersystemEigenverbrauchKwh`. Der Rechenweg ist unverändert.
- `ErgebnisCtrl.Save` schreibt die drei Spalten (kaufmännisch gerundet, `RN`), nur wenn `ErgebnisreihenSchema.Lesbar()`;
  `ErgebnisCtrl` liest sie nullbar (`DN`). `KennzahlenKatalog.AbregelungMwh` nimmt die gespeicherte Jahressumme, Rückfall
  auf die Reihe bei Altergebnissen.
- Energieflussbild (`Energiefluss.cs`): Band Strom → „Einspeisung BHKW“ aus der Ergebniszeile (Herkunft Direkt) mit Prüfung
  des Knotens, Rückfall Bilanzierung ohne Wert. Befund der ersten Runde: 1058 und 1063 (BHKW mit Stromspeicher ohne
  Flotte) schlossen nicht mehr — die Zeile trägt den BHKW-Überschuss des Laufs (5,32 bzw. 16,56), der Speicherweg bucht
  danach seine `Ladung_BHKW` (1,86 bzw. 4,79) daraus; ins Netz geht der Rest. Regel: Band = Zeile − `Ladung_BHKW`
  (Herkunft Abgeleitet, Formel `EF_FORMEL_EINSPEISUNG_BHKW`), wenn der Knoten damit schließt und mit der Zeile nicht;
  mit Flotte (Zeile = Netzeinspeisung der Flotte) bleibt die Zeile. Band Strom → „Standby Speichersystem“ (Verlust,
  Kennung `standby:<i>`); Band
  Photovoltaik → „abgeregelt“ (Verlust, Kennung `abregelung_pv`), an den Strombus Erzeugung minus Abregelung, Kleinverlustregel
  im Bild. Kein Parameter in `EnergieflussAbbildung` oder Renderer. Ressourcen `EF_KN_STANDBY`, `EF_KK_STANDBY`,
  `EF_KN_ABREGELUNG`, `EF_KK_ABREGELUNG` (de/en), Designer erzeugt (+4 Einträge).

## Referenzlauf und Basis R59

- 38 Projekte gegen R57 (Linux, `EPOS.Referenzlauf`): 34 „Eintrag nur im Vergleichslauf“ — `BHKW.Stromeinspeisung` je
  BHKW-Projekt, `Photovoltaik.Abregelung` (0) je PV-Projekt; mit `--ohne BHKW.Stromeinspeisung,Photovoltaik.Abregelung`
  **GESAMT PASS (13 025 990 Werte)**, 1 199 von 1 233 CSV byte-gleich (die 34 übrigen sind die `aggregate.csv`). Kein
  bisheriger Wert geändert. `Tab_ErgebnisStromspeicher` steht nicht im Export, der Standby bleibt der Skalar
  `Stromspeicher.EigenverbrauchSystemMwh` (1046: 0,438).
- Neu eingefroren: `Referenzlaeufe/2026-10-17_R59_Ergebnisreihen` (38 Projekte, 1 233 CSV, 8 630 Skalare, Stand
  `53da1da8b`, Testdatenbank 239). R57 nach dem Muster der Vorgänger ins Archiv (Protokoll und Abschnitt unter
  `Dokumentation/ueberholt/Referenzbasen/`), Verweise in `CLAUDE.md`, `kern.yml`, `ios.yml`, Gate, `Dokumentation/LIESMICH.md`
  und vier Wache-Tests umgestellt. Keine neue Einfrierregel (Ausgaben des Laufs, keine gesäten Eingaben).
- ChartProben: 276 Bilder, 0 Verstöße, Messlatte unverändert (kein Bild ändert sich — kein Referenzbild trägt Standby oder
  Abregelung).

## Tests

- Neu: `ErgebnisreihenSchemaTests` (Kette, Register, Spalten leer mit CHECK, wiederholbar, Testdatenbank auf 239),
  `ErgebnisreihenLaufTests` (1018 Einspeisung = Reihe `BHKW_UEBERSCHUSS`; 1046 Standby = `SpeichersystemEigenverbrauchKwh` =
  Reihe, Abregelung 0; 1040 mit synthetischer Einspeisegrenze 1 kW: Abregelung = Reiter = Reihe, Erzeugung und Überschuss wie
  bisher; Speichern und Lesen der drei Spalten, Kennzahl `eff.pv_eigen` ohne Reihen; Altergebnis mit NULL fällt auf die
  Bilanzierung zurück; Abzweig „abgeregelt“ nach der Kleinverlustregel, 3 % als Band, 0,5 % im Zeigertext).
- Angepasst: `EnergieflussReferenzprojektWacheTests` — 1018, 1058, 1063 Herkunft Direkt gleich der Ergebniszeile; 1046 Band
  „Standby Speichersystem“ (0,438), kein „nicht zugeordnet“, kein Hinweis, alle Knoten geschlossen.
- Läufe: `EPOS.Kern.Tests` gefiltert (`Schema|Ergebnis|Energiefluss|Kennzahl|Strom|BHKW|PV|Speicher|Wache`), Runde 1:
  3 670 Tests, 3 657 grün, 1 übersprungen, 12 rot — acht „nicht genau eine Basis“ (R57 und R59 lagen vor dem Archivieren
  nebeneinander), zwei Energiefluss 1058/1063 (Speicherregel, oben), zwei eigene (Flottenreihe `PV_ABREGELUNG` ist eine
  Nullreihe, CHECK-Probe mit typisiertem Parameter). Runde 2 nach den Korrekturen (`Ergebnisreihen|Energiefluss|
  ZapfprofilReferenzprojektWache|ZapfprofilOptionenReferenzprojekteWache|VorheizReferenzprojektWache|DokumentationLinkWache|
  RepositoryOrdnungWache|Kennzahl`): 304/304 grün. `EPOS.UI.Tests` gefiltert (`Uebersicht|Ergebnis|Simulation`): 749/749.
  `Auslieferungsvorlage.Tests`: 63/63. ChartProben 276 Bilder, 0 Verstöße. `SqlDialektPruefer` 0 Fundstellen.
  `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.

## Commits (Zweig `eb1`, nicht gepusht)

- `53da1da8b` EB-1: Schema 239 Ergebnisreihen, Lauf schreibt Einspeisung/Standby/Abregelung
- `f0f694db0` EB-1: Testdatenbank 239, Basis R59 Ergebnisreihen, R57 ins Archiv
- dritter Commit: EB-1: Tests, Speicherregel der BHKW-Einspeisung im Energieflussbild, Protokoll

## Offen (EB-2 und EB-Q)

- EB-2 (Oberfläche): Ergebnistabellen der Reiter BHKW/PV/Stromspeicher, Zeitreihen-Grafiken mit wählbarer Reihe
  „eingespeister Strom“ (`BHKW_UEBERSCHUSS`) und zuschaltbarer „Abregelung“ (`PV_ABREGELUNG`, Vorgabe aus, Muster der
  RZ-a-Schalter), Übersicht, CSV, KI-Kennzahlen, Bericht, Wiki samt Logbuch — Schnittstelle im Konzept, Abschnitt 5.
- EB-Q2 (Anzeigeeinheit der Standby-Zeile), EB-Q3 (Einspeisung BHKW bei Altergebnis in der Übersicht) — Empfehlungen im
  Konzept, Abschnitt 6.
