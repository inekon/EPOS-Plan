# Protokoll V29/V31/V32 — Prozesstemperatur, Zapf-Nutzungsarten, Nutzungsprofil über IDs (Schemaschritt 178, 03.10.2026)

Anwenderauftrag 03.10.2026 (Folgeauftrag 10). Opus-Agent im Worktree, Zweig `claude/v29-v31-v32` auf `d9b37b11`;
Merge `edfaaf84`; Kette umgehängt in `46178493`. Statuszeile **@@N3@@**.

## 1 Was gebaut ist

| Commit | Inhalt |
|---|---|
| `2347fa52` | V29: `PufferAuslegungEingang` mit `ProzessVorlaufC`, `ProzessRuecklaufC`, `ErzeugerVorlaufMaxC`; Prozesszone rechnet mit der Spreizung des Prozesses (`HeizzoneRechner.MitSpreizung`); Warncode `PA-PROZESS-TEMPERATUR` (über 95 °C oder über `MAX(Tab_Energieanlagen.Vorlauf)` der Erzeuger); 5 Schlüssel de/en |
| `f15c8095` | V31 und Schritt 178 `ProzessNutzungSchema` (`WaermepumpeSperrprofilSchema.SCHRITT + 1`): STRICT-Tabellen `Tab_Nutzungsprofil_STAMM` (5 Kennungen) und `Z_Nutzungsprofil` (Quelle ZAPF/KONDITIONIERUNG/GEBAEUDEART, Schlüssel, 26 Zuordnungen) mit Saat `INSERT OR IGNORE`; `TwwPaketteilCtrl.NutzungsartenNachtragen` (drei Nutzungsarten mit 3 Tagesgangsätzen, 12 Tagesgängen, 6 Zapfkategorien, nur bei vorhandener Katalogversion); Paketteil `Referenzlaeufe/Katalogpaket_frei` (Nutzungsarten 6 → 9, Tagesgangsätze 5 → 8, Tagesgänge 20 → 32) aus `tww_nichtwohnen_setzung.json` über das Einspielskript |
| `5accb157` | V32: `Nutzungsprofil.Ableiten` über `NutzungsprofilZuordnung` (IDs), Rückfall auf die Zuordnung im Code; keine Textvergleiche gegen Anzeigenamen |
| `d5ec31db`, `7279934c` | `ProzessNutzungTests` (10), Katalogzahlen in `KatalogpflegeTests`, `TwwKatalogimportOhneVersionTests`, `ZapfprofilHuelleKatalogdialogTests`; `TwwVorlageTests` T13 |

## 2 Festlegungen

- **V29 ohne DDL:** `Tab_Prozesswaerme(_STAMM)` führt `Vorlauf`/`Ruecklauf` seit PW1 samt Dialog, Duplizieren, Export
  (`ProzesswaermeTemperaturWegeTests`); neu ist nur die Nutzung in der Auslegung.
- **Katalogversion nicht angehoben:** `Nachladen` lädt nur ohne Version; eine angehobene Version hätte alle Zeilen neu
  angelegt und 1045 berührt. Der Nachtrag läuft über den Schemaschritt; die Testdatenbank bekam die drei Nutzungsarten über
  `tww_testkatalog_fiktiv.py` als EIGEN (24 Zeilen), danach trug der Schritt nichts nach.
- Nutzungsarten „Büro (Setzung)“, „Schule (Setzung)“ (Ferienfaktor 0,1), „Gewerbe Schichtbetrieb (Setzung)“ als Setzung
  nach DIN EN 12831-3, Herkunftsart EIGENKONSTRUKTION, ohne Produktdaten.
- KONDITIONIERUNG WOHNEN bewusst nicht zugeordnet (1030 bleibt „WOHNEN, Vorgabe“); GEBAEUDEART gesät, wirkt nicht in der
  Reihenfolge (`AusGebaeudeart` bereitgestellt). Schlüssel ZAPF ist der Bezeichner der Nutzungsart (kein `Kennung`-Feld).
- Beim Merge mit origin 176 (Nutzung an der Kalenderkopie) und P4c (Verweis am Gebäude): **Kalenderkopie geht vor**, der
  Verweis gilt nur für Gebäude ohne Nutzung am Kalender, der Rückfall über `Bemerkung` ist entfallen (`3766779a`).
- `Tab_Nutzungsprofil_STAMM` steht mit Grund in `Katalogfassung.Ausgenommen` (feste Aufzählung, kein Paketweg).

## 3 Nachweise

Einfrierregel 1045: die benutzte Nutzungsart, ihr Tagesgangsatz, Tagesgänge, Zapfkategorien, alle Parameter,
`Tab_TwwZone`/`Tab_TwwProjekt` von 1045 und alle vorhandenen Nutzungsarten gleich der eingefrorenen Testdatenbank (Test);
Referenzlauf 1045 byte-gleich. Agent: `ProzessNutzungTests` 10/10, Puffer- und Prozesswärme-Tests 77/77, UI 270/270,
Tww-/Katalogtests 417/417 nach Nachzug, Auslieferungsvorlage 24/24 nach Nachzug; Kern-Filter, Windows-Schale,
Schemawerkzeug 0 Fehler; Referenzlauf CI-Sieben 7/7 PASS, 226 Dateien byte-gleich. Wiki: Seite „Prozesswärme“ fehlt
(Monatssummen × Wochenprofil, Zuordnung, Betriebskalender, Temperaturpaar, Deckung, Wirkung in der Simulation,
Pufferauslegung); Logbuch (1.2.0.7): Prozesstemperatur in der Pufferauslegung; neue Brauchwasser-Nutzungsarten Büro,
Schule und Gewerbe.
