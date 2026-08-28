# Paket P1 — Schichtspeichermodell: Umsetzungsprotokoll

Stand: 27.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Kapitel 7 (L7, Entscheidung F7), 6.2/6.3 (W4/W6), Schritt 53, Paketzeile P1.
Build x64 Debug: 0 Fehler.

## 1. Umfang

`SimulationPufferspeicher` rechnet jetzt als **Multi-Node-Modell** (N = 1…10 Schichten
gleichen Volumens, ideale Einschichtung, Verdrängungsentnahme, vertikaler Ausgleich,
Inversionsmischung, schichtweise Verluste) — **vollständig innerhalb der Klasse**: Kaskade,
Ladeordnung, Herkunftsrechnung und Module sehen dieselbe Energie-Schnittstelle wie bisher.
**SOC bleibt die führende Zustandsgröße** (7.3); die Schichtebene verteilt
A = min(SOC, Q_max), der Überhang B bleibt außerhalb (N6). Dazu: Lade-/Entladeleistungs-
grenzen je Stunde (K2-O6), die `T_oben`-Füllung (E1-O5), die Schichtungs-Bedienung im
Pufferdialog und die Scharfschaltung der vertagten Warnkriterien W4/W6 und des
T_Nutz-Anteils von W3 (S2-O1).

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Schritt 53** | Neun Spalten an `Tab_Pufferspeicher`: `Schichten_Anzahl` (DML 1), `Hoehe` (NULL = aus H/D 2,5), `Lambda_Eff` (NULL = 1,5), `T_Nutz_BW` (NULL = RL_eff), `Entnahme_Heizung/_BW/_Prozess` (NULL = Konzept-Default), `Ladeleistung_Max`/`Entladeleistung_Max` (DML 0 = unbegrenzt); 53a DDL + 53b verhaltensneutrale DML; idempotent (Marker-Rücksetz-Probe: 0 angelegt / 9 vorhanden); `ZIEL_VERSION` 52 → **53**; Tabelle 19 → 28 Spalten | `SchemaMigration.cs`, `SchemaKatalog.Schritt53_Schichtmodell` |
| **Schichtebene** | Zustand `T[i]`/`E[i]` hinter der unveränderten SOC-Arithmetik; Beladung an der Einspeisehöhe (aus `Z_AnlageSenke.Anschlusshoehe` über `Ladeauftrag.Einspeisehoehe`; NULL = oben) abwärts auf VL_eff; Entnahme als ideale Verdrängung ab Entnahmehöhe abwärts; Entladefähigkeit je Kanal = B + Σ Schichten mit T ≥ T_Nutz (Klemmung in `Kaskadenschleife.EntladeKanal` neben `EntnahmeObergrenze`; bei N = 1 `double.MaxValue`); Ausgleich k = λ_eff·A/(H/N) mit 25-%-Kappung; **Inversionsmischung als Blockmittelung** (Pool-Adjacent-Violators — die paarweise Fassung konvergierte nur im Grenzwert, Restinversion 1,3e-5 K in 6842/8760 h; jetzt exakt, energieerhaltend, zweiter Durchlauf nach der Verlustverteilung, 0 Monotonieverletzungen); Verluste je Schicht nach Manteloberfläche, (T−RL_eff)-gewichtet; Kombi: BW-Zone oben (Entnahme 1,0), Heizung 0,5 | `SimulationPufferspeicher.cs` (+~900), `Kaskadenschleife.cs`, `SimulationKanaele.cs` |
| **N = 1 byte-gleich, konstruktiv** | Jede Schichtbuchung steht HINTER der SOC-Arithmetik; die einzige Energiepfad-Rückwirkung (`EntladefaehigkeitKanal`) liefert ohne Schichtung `double.MaxValue`; Leitung/Mischung brauchen ein Schichtpaar; Leistungsgrenzen nur bei Wert > 0. Invariante Σ Schichtenergie == min(SOC, Q_max) als Debug-Probe — gemessen 0,000E+00 | `SimulationPufferspeicher.cs` |
| **Verbund/Quellspeicher** | Leitspeicher eines Parallelverbunds rechnet STETS N = 1 (Laufzeit-Riegel in `VerbundAufaddieren` mit Protokollwarnung); Quellspeicher bleiben per Konstruktion Ein-Zonen (7.6) | `SimulationControl.cs` |
| **K2-O6 Leistungsgrenzen** | Budget je STUNDE (`StundeBeginnen` in der Kaskadenschleife, wo die Reservierungen verfallen); `Entnahmefaehigkeit()` liefert den Budget-Rest — der Zwei-Pass-Durchlauf eines Kombi-/Heizpuffers (P und H je Stunde) erhält die Grenze genau einmal. Nachweis 1041/Kombi, Grenze 3,0 kW: höchste Stundenabgabe exakt 3,0000 kWh (vorher 43,43), beide Kanäle weiter bedient | `SimulationPufferspeicher.cs`, `Kaskadenschleife.cs` |
| **T_oben (E1-O5)** | `T_oben_Mittel`/`T_oben_Min` gefüllt (14/15 Speicherzeilen; die Quellspeicherzeile bleibt NULL — Spreizungen sind keine Speichertemperaturen); bei N = 1 nach der Ein-Zonen-Beziehung aus Kap. 8.2 (Stichproben: 1023 → 54,20; 1024 → 55,23; 1030 → 83,74 — formeltreu); Ganglinien `PUFFER_<ID>_TOBEN`/`_TUNTEN` im `ZeitreihenSatz` (bewusst nicht im kWh-Füllstandsdiagramm) | `SimulationRunner.cs`, `ZeitreihenExtraktor.cs` |
| **Dialog „Schichtung"** | Programmatische Gruppe in `Form_PufferSp_Projekt`: N 1–10; ab N > 1 Höhe/λ_eff/T_Nutz BW (55-°C-Vorschlag beim Wechsel, nur wenn leer)/Entnahmehöhen (nur Kanäle des Klassen-Sets); Lade-/Entladeleistung immer sichtbar (0 = unbegrenzt); Sollhöhen-Logik gegen das FensterEinpassung-Ratchet, d49075e-Anker | `Form_PufferSp_Projekt.cs`, `PufferSpModel/Ctrl` (spaltentolerant, `StelleSchichtSpaltenSicher`) |
| **W6 hart, beidseitig** | Dialog weist N > 1 am Verbund-Leitspeicher ab (`PSP_FEHLER_SCHICHTUNG_AM_VERbund`-Text); Gegenrichtung: `AnlagePufferVerbundCtrl.KonfliktPruefen` Punkt 7 (`GRUND_LEIT_GESCHICHTET`, neuer `IstLeitspeicher`); `WaermesenkeClass.VerbundKonfliktMeldung` hat den zugehörigen `case` (Nacharbeit des Orchestrators — die Datei war für den Dialog-Agenten tabu) | `Form_PufferSp_Projekt.cs`, `AnlagePufferVerbundCtrl.cs`, `WaermesenkeClass.cs` |
| **Warnkatalog scharf (S2-O1)** | W4 (T_Nutz_BW > VL_eff → Klemmhinweis), W6 (Hart, träge Verbundabfrage), W3-T_Nutz-Anteil (Erzeuger-Vorlauf < T_Nutz_BW bei Brauchwasser-Ziel); spaltentolerant — ohne Schritt 53 keine neuen Befunde (belegt: `PruefeProjekt` über 13 Projekte zeichengleich zum Vorstand) | `Warnkriterien.cs` |
| **Karten** | SpeicherKarte-Badge „N Schichten" bei N > 1; `T_oben` in der Detailansicht, wenn das Ergebnis einen Wert trägt | `Form_Simulation_Config.Karten.cs`, `SpeicherKarte.cs` |
| **Nacharbeit Spaltenname** | Der Kern lieferte `Höhe` (Umlaut) — gegen Konzept 7.2 und Hauskonvention; auf **`Hoehe`** korrigiert, bevor irgendeine echte DB Schritt 53 erreichte (produktiv stand auf 51); alle Nutzer binden an `SchemaKatalog.SPALTE_PSP_HOEHE` | `SchemaKatalog.cs:1507` |

**Neue Ressourcen: 27** (Gruppe/Labels/Hinweise/Fehlertexte der Schichtung, Karten-Badges,
`SIMWARN_W3_UNTER_TNUTZ`, `SIMWARN_W4_TNUTZ_UEBER_VLEFF`, `SIMWARN_W6_SCHICHTUNG_AM_VERBUND`,
`SIM_VERBUND_KONFLIKT_LEIT_GESCHICHTET`), DE/EN/Designer deckungsgleich.

## 3. Verifikation

- **Referenzlauf 13 Projekte gegen `2026-08-27_E1`** (Kern-Stand): 316/316 Ganglinien
  byte-gleich; einzige aggregate-Änderung die 28 neu gefüllten `T_oben_*`; mit
  `--ohne` dieser zwei Schlüssel **13/13 PASS (3 532 029 Werte)**; Selbstvergleich
  **329/329 byte-gleich**; `pruefen` 13/13. Der kombinierte Stand (Kern + Dialog +
  Nacharbeiten) wurde vom Orchestrator identisch gegengeprüft (eigener Lauf).
- **N>1-Wirkproben** (Wegwerf-Kopie, danach frisch neu angelegt): 1024/BW-Puffer N=5,
  T_Nutz 55: Schicht-Invariante 0 Verletzungen (max |Σ−Ziel| 4,4e-14 kWh), Monotonie 0,
  T-Band nie außerhalb [RL_eff, VL_eff], Energieerhaltung schließt; **2387 Stunden mit
  Inhalt, aber ohne Schicht ≥ 55 °C → BW-Kanal gesperrt** (die Entladefähigkeitsgrenze
  arbeitet). 1023/Heizungspuffer N=5 (39 MWh Durchsatz): Entladung identisch zum
  E1-Basiswert — mit Konzept-Vorgaben ist die Schichtung auch bei N=5 energiepfadneutral.
  Leistungsgrenze 2,0 kW: höchste Stundenabgabe exakt 2,0000 kWh. T_Nutz-Klemmung
  (55 auf VL_eff 10): Protokollwarnung + Rechnung mit 10.
- **Dialog-Wirkproben**: Speichern/Wiederlesen N=5/T_Nutz 55/Ladeleistung 12,5; W6-Abweisung
  am echten Verbund-Leitspeicher 1054187 (Projekt 1040) in beide Richtungen; W4-Text bei
  T_Nutz 95 auf 65/45; Karten-Badge „5 Schichten".
- **Toleranzpfad**: Kopie ohne Schritt 53 → Dialog ohne wirksame Schichtgruppe, Katalog ohne
  W4/W6/T_Nutz-Befunde, `PruefeProjekt`-Ausgabe zeichengleich zum Vorstand.

## 4. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| P1-O1 | Das Referenzlauf-Werkzeug exportiert keine `T_oben`-Ganglinien-CSV (Messinstrument, E1-O3-Linie) — Aufnahme nur mit einem Basiswechsel | dokumentiert |
| P1-O2 | `Z_AnlageSenke.Anschlusshoehe` ist überall NULL — der Einspeisehöhen-Pfad ist gebaut, die Dialogpflege je Senke fehlt | P2/Q1 |
| P1-O3 | `T_Nutz` nur für Brauchwasser (F7); Heizung/Prozess bleiben RL_eff | Konzept, bleibt |
| P1-O4 | Booster-Temperaturkopplung nicht Teil von P1; Schnittstelle steht (`SchichtTemperatur(i)`/`T_oben`, N=1-Ersatztemperatur) | **B1** |
| P1-O5 | Kein T_oben-Diagramm (nur Datenreihe) | P2/Bericht |
| P1-O6 | `Entnahme_*`-Spalten liest die Engine noch nicht kanalweise aus der DB (Registry übernimmt N/Hoehe/T_Nutz; Entnahmehöhen wirken derzeit über die Konzept-Defaults) — Übernahme beim nächsten Engine-Schnitt | P2 |
| P1-O7 | Engine-Protokolltexte inline deutsch (Nachbarstil) | Paket L |
| P1-O8 | Pufferdialog zugeklappt jetzt 826 px Client-Höhe — auf kleinen Schirmen rollt er; FensterEinpassung-Ratchet dokumentiert | kosmetisch |
