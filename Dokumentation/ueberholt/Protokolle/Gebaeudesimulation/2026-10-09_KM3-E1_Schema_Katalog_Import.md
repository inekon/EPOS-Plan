# Protokoll KM3-E1 — Schemaschritt 210, Katalog, Import der Teillastkurve (09.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#876**. Commits E1-a `62cf8611c`, E1-b `10cfef908`, E1-c `e43f346c5`. **Entscheid:** E114 (KM3-Q1 bis Q11). Konzepte: [`Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](../../../aktuell/Kälteanlagen/Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md) und [`Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](../../../aktuell/Kälteanlagen/Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md).

## 1 Auftrag und Entscheidlage

- **E114** (Anwender, 09.10.2026): Startfreigabe KM3 mit den Entscheiden KM3-Q1 bis Q11 nach Empfehlung a.
- Schemanummer: angemeldet als 209, mit Anwenderentscheid vom 09.10.2026 (Weg B) auf 208 gesetzt (`8cbda3b97`); nach dem Bau von 208 `KatalogkostenUrsprungSchema` und 209 `KatalogkostenInvestitionSchema` durch KA1 (Konto 2, #873) mit Anwenderentscheid 09.10.2026 19:55 UTC auf **210** verschoben, hängt an 209 `KatalogkostenInvestitionSchema` (KM3-M2).
- Entscheid der Orchestrierung: Die Plausibilitätsgrenze des Gütemaßes sinkt auf 0,3, weil sieben Festdrehzahl-Sätze physikalisch plausibel sind.

## 2 Wellen

| Welle | Commits | Ergebnis |
|---|---|---|
| E1-a | `62cf8611c` | Schemaschritt `KaeltemaschineTeillastSchema`: acht Eingabespalten an `Tab_Kaeltemaschine_STAMM`/`Tab_Kaeltemaschine` (`Teillast_Weg`, `Teillastkurve_a`/`_b`/`_c`, `Teillastkurve_Lastgrad_Min`, `Taktverlustfaktor_Cd`, `Verdichterregelung`, `Kennfeld_Randweg`), fünf Ergebnisspalten an `Tab_ErgebnisKaeltemaschine` (`Taktstrom_MWh`, `Starts`, `Teillaststunden`, `Lastgrad_Mittel`, `Stunden_Extrapoliert`), Modell, `KaeltemaschineStammCtrl` (`TeillastSchreiben`, `TeillastPruefen`, `ZahlLesen`), Projektkopie, Prüfsummenliste, 14 Ressourcen, vier Leser |
| E1-b | `10cfef908` | Import der Teillastkurve aus Copper `eir-f-plr` (quadratisch, normiert auf EIRFPLR(1) = 1; kubisch angepasst) und aus CSV-Teillastzeilen (Kleinste Quadrate); `Verdichterregelung` aus `compressor_speed`/`compressor_type`; `KaeltemaschinenTypkennfelder.Ergaenzen` (idempotent, Prüfsumme neu); Vorgabekurven je Regelung in `KaelteFestwerte`; Hinweis `NennEerHinweis` bei mehr als 10 % Abweichung |
| E1-c | `e43f346c5` | Gütemaß-Grenze 0,3 statt 0,5; Vorgabekurve STUFEN aus Copper-Satz 146 |

## 3 Dateien

- Kern: Schemaschritt `KaeltemaschineTeillastSchema` (`SchemaMigration`), `KaeltemaschineStammCtrl`, `KaeltemaschinenTypkennfelder`, `KaelteFestwerte`, Import (Copper, CSV).
- Testdatenbank: `Referenzlaeufe/Kenndaten_Test.sqlite` (Schemastand 210).
- Ressourcen in beiden Sprachen (14 Schlüssel).

## 4 Proben

| Probe | Ergebnis |
|---|---|
| Typkennfelder der Testdatenbank | alle 34 mit Weg: 31 KURVE, 3 LINEAR |
| Beispielgeräte | Teillastfelder leer |
| Ergänzen | idempotent, Prüfsumme neu |

## 5 Festlegungen

Das Opt-in bleibt: ohne `Teillast_Weg` rechnet die Kältemaschine wie zuvor. Der Import normiert die Kurve auf EIRFPLR(1) = 1; weicht der Nenn-EER um mehr als 10 % ab, steht der Hinweis `NennEerHinweis`.

## 6 Offen

E3 (Dialoggruppe, Bericht, Kennzahlen, Vorlagen), E4 (Wiki, Logbuch).

## 7 Gate und CI

**Gate:** @GATE@

**CI:** Kern-Lauf der CI auf dem Sitzungszweig läuft (Vermerk folgt).
