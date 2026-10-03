# Protokoll P4b — Pufferspeicher-Auslegung: Startzähler-Rückkopplung und Probelauf (V13, 03.10.2026)

Auftrag „V13 Startzähler Wärmepumpe und BHKW: Umsetzung nach Idee aus Runde 1“ (Anwender, 03.10.2026). Vorgänger
[`2026-10-03_P4a_Pufferauslegung_Ressourcen_Teillast.md`](2026-10-03_P4a_Pufferauslegung_Ressourcen_Teillast.md).
Opus-Agent im Worktree, Zweig `claude/p4b-pufferauslegung` auf `73f18c19`; Merge `4793d349`. Statuszeile **#698**.

## 1 Was gebaut ist

| Commit | Inhalt |
|---|---|
| `9cdb9716` | `EPOS.Kern/Controller/PufferProbelaufCtrl.cs`: Probelauf der Jahressimulation über `SimulationRunner.Simuliere` (derselbe Lauf wie der Referenzlauf, ohne Speichern); Naht `WaermesenkeClass.ProbelaufVolumen(idPuffer, l)` (`AsyncLocal`, greift in `PufferLesen` und Desinfektionsmenge; ohne offenen Bereich liest die Simulation wie bisher); `SimulationProtokoll.Wiederherstellen` stellt das Protokoll des letzten echten Laufs wieder her; Starts je Erzeuger (Jahr, Heizperiode als Anteil der Einschaltstunden in Bedarfsstunden, Tag), Deckung, Füllstandsreihe; Warncode `PA-STARTS-ABWEICHUNG` (bewusst nicht in `ALLE`); `PufferAuslegungGespeichert.ProbelaufStartsJeTag/ProbelaufAm`; Bericht zeigt Schätzung und Probelauf nebeneinander |
| `504cd439` | Schritt 4 der Seite: Gruppe „Probelauf“ mit Knopf „Mit Jahressimulation nachrechnen“, Spalte „Probelauf (Empfehlung)“ mit Zeitpunkt, Betriebsbild als Tabelle Füllstand min/mittel/max je Monat plus kälteste Woche, Dauer; Hülle meldet benannt (kein Lauf möglich, Lesemodus, neuer Puffer) |
| `f5eb3476` | Starts ohne Zähler aus der Wärmereihe (Einschaltstunden, Hinweis `PAUS_PROBELAUF_AUS_REIHE`); Abweichungstext mit gerundeten Werten |
| `e1ab109c` | `PufferProbelaufTests` (14), `PufferAuslegungSeiteTests` (+5) |

25 Ressourcenschlüssel de/en (`PA_STARTS_ABWEICHUNG`, `BER_PAUS_STARTS_PROBELAUF`, `PAUS_PROBELAUF_*`).

## 2 Befunde und Festlegungen

- **Starts liegen nicht in der Datenbank:** `Starts_WP`, `Starts_BHKW`, `Starts_Spk` leben nur im Laufobjekt; keine
  Ergebnistabelle trägt sie. Deshalb liefert der Probelauf die Starts; das letzte Ergebnis bleibt je Projekt und Puffer
  im Speicher bis zum Programmende (`PufferProbelaufCtrl.Letzter`). Ob die Starts dauerhaft in `Tab_Ergebnis*` sollen,
  wäre ein Schemaschritt und ein Anwenderentscheid.
- **Neuer Puffer:** Der Probelauf lehnt „neu anlegen“ benannt ab, weil ein Puffer im Speicher keine Erzeugerzuordnung hätte.
- **Weg (b) Projektkopie verworfen:** Der Kern hat keine Löschfunktion für ein ganzes Projekt.
- **Befund zu M4:** Wärmepumpen ohne gepflegte Mindestleistung zählen im Lauf keine Starts (die Wärmepumpe von 1045
  zählt 0 und läuft stündlich durch); Rückfall über Einschaltstunden der Wärmereihe, gekennzeichnet.
- **1045 Kombipuffer 1054210** wird von keinem Erzeuger geladen; der Probelauf zeigt „kein Füllstand“.
- Laufzeit auf der Testdatenbank: 1045 rund 0,7 s, 1030 rund 0,3 s, 1046 rund 1,2 s.

## 3 Nachweise

Agent: Kern-Filter 0 Fehler, `PufferProbelaufTests` 14/14, Kern-Auswahl 665/665, UI-Auswahl 232/232, SQL-Prüfer
0 Fundstellen, Referenzlauf CI-Sieben 7/7 PASS, Windows-Schale 0 Fehler. Ergebnistabellen, `Tab_PufferAuslegung` und
`Tab_Pufferspeicher` bleiben beim Probelauf unverändert (Test vergleicht vorher/nachher). Gate auf dem Sammelstand: siehe
[`2026-10-03_P4d_Pufferauslegung_Nachbarstufen_Aufheiz.md`](2026-10-03_P4d_Pufferauslegung_Nachbarstufen_Aufheiz.md).
