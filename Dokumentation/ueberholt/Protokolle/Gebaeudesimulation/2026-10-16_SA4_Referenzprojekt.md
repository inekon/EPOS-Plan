# Protokoll SA4 — Referenzprojekt 1073 „Referenzprojekt Schnellaufheizung“, Basis R57

**16.10.2026 · Sitzung Gebäudesimulation · Auftrag SA4 (E128, E153).** Ein Referenzprojekt, an dem die Schnellaufheizung
([Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md), Abschnitt 7) greift und
sichtbar wird, samt Wache und neuer Basis `2026-10-16_R57_Schnellaufheizung`. Kein Schemaschritt, kein Rechenweg geändert.

## Wahl der Kopiervorlage (E153)

Die Schnellaufheizung wirkt nur an Zonen mit wirksamem Heizkreis und Heizkurve (AK1, AK3) und nur mit „Vorheizzeit
vorgeben“ oder „Vorheizzeit berechnen“ (`Vorheizvorgabe.SchnellaufheizungWirksam`, Plan in `Vorheizplanung.PlanBilden`).

| Kandidat | Heizkreis | Befund | Urteil |
|---|---|---|---|
| 1065 (Vorheizen) | nein | Messung SA1b: in jeder Zahl gleich mit und ohne Überhöhung | ungeeignet — wirkungslos |
| 1051 (Konditionierung) | nein | wie 1065 | ungeeignet |
| 1047 (AK1, WP) | ja | t_V bleibt 1 h, 75 überhöhte Stunden, Wärme +0,1 % | Wirkung kaum sichtbar |
| 1058 (AK3, WP, Puffer) | ja | t_V bleibt 1 h, 59 überhöhte Stunden | Wirkung kaum sichtbar |
| **1054 (AK1, drei Zonen, Kessel)** | ja | ohne Überhöhung 365 Nächte durchgeheizt, t_V 9 h; mit 3 K t_V 5 h, 0 Nächte | **gewählt** |

1054 ist der Fall, für den die Schnellaufheizung gebaut ist (Entwurf Abschnitt 7, „Problem“): Ein Heizkreis ohne
Aufheizreserve heizt ohne Überhöhung jede Nacht durch. Der Mehrzonenweg mit gemeinsamem Vorlauf und eigener Übergabe einer
Zone kommt mit. Nicht abgedeckt bleibt die Grenze `Vorlauf_Max` (in 1054 keine Stunde an der Grenze; die Kessel liefern bis
über 58 °C) — sie halten `SchnellaufheizungTests` an 1047/1058.

## Eingaben

Kopie 1054 → 1073 auf dem Kopierweg des Programms (`ProjektDuplizierenCtrl`), Saatskript
`Referenzlaeufe/Skripte/referenzprojekt_1073_schnellaufheizung.cs` (wiederholbar, zweiter Lauf byte-gleich). Gegenüber der
Vorlage geändert, an `Tab_Einstellungen`:

- `Aufheiz_Verfahren` = `BERECHNET` (1054: leer = Sollwertrampe) — ohne Verfahren wirkt die Schnellaufheizung nicht;
  „berechnet“ hält zugleich die Vorausschau mit Überhöhung.
- `Aufheiz_Schnellaufheizung` = 1.
- `Aufheiz_Ueberhoehung_K` = 3 — rund, über der Vorgabe 2 K, damit der Weg über die Spalte gehalten ist; in SA1 gemessen
  ohne Konvergenzproblem (5 K brauchte die Merkregel der Zonenkopplung).
- Toleranz, Regelgenauigkeit, Geltung, Heizkreis einbeziehen, Vorheizzeit leer — Vorgaben des Kerns (wie 1065).

Testdatenbank Schemastand 235 mit 1073: 109 588 480 Byte, LFS-SHA-256 `576fa997…`, `integrity_check` ok,
`foreign_key_check` leer.

## Wirkung

| Lauf | t_V h | Bedarf max h | Nächte o. Absenkung | überhöht h | Vorlauf max °C | Spitze kW | Heizwärme MWh |
|---|---|---|---|---|---|---|---|
| 1073 (Δθ 3 K) | 5 | 9 | 0 | 302 | 58,0 | 26,37 | 50,79 |
| 1073 ohne Schalter (Gegenlauf) | 9 | 12 | 365 | 0 | – | 23,64 | 52,28 |
| 1054 (Sollwertrampe, Vorlage) | – | – | – | – | – | 27,47 | 47,59 |

Die Überhöhung spart gegen den Gegenlauf 2,85 % Heizwärme und erhält jede Nachtabsenkung; die Spitze steigt um 12 %.
Vorlaufgrenze, wirkungslose Stunden und Sperrzeit je 0; Überhöhung wirkt an einer der zwei beheizten Zonen
(Ergebniszeile je Zone). Gegen die Vorlage heizt 1073 mehr, weil es vorheizt statt zu rampen; Aufheizzustand BEMESSEN statt
GEKOPPELT. Emissionen: Kessel 4,79 t gegen 4,80 t, BHKW 12,44 t gegen 11,20 t CO₂ (1054).

## Basiswechsel R56 → R57

- Referenzlauf aller 38 Projekte (Linux x64, de-DE): 38 von 38 erfolgreich, 27 s.
- Die 37 Projekte von R56 gegen R56: GESAMT PASS je Projekt (37 von 37, 12 736 541 Werte) und byte-gleich (1 205/1 205
  CSV); 1073 kommt mit 28 CSV und 335 Skalaren hinzu. R57: 1 233 CSV, 8 596 Skalare. Zweiter Lauf byte-gleich (1 233/1 233).
- Plattformnachweis: 1073 gestört (`--stoerung ulp`) gegen ungestört PASS (289 415 Werte).
- R56 archiviert wie R55: Protokoll unter `Dokumentation/ueberholt/Referenzbasen/2026-10-16_R56_Wasserkosten/`,
  Indexzeile, Abschnitt „Die Basis R56 im Einzelnen“, Zählung 59 bzw. 73.
- Nachgezogen: `Referenzlaeufe/LIESMICH.md` (Aktuelle Basis, Vorgängerbasis, Abschnitt 1073, Einfrierregel „gesäte
  Vorheizdaten“ um die Schnellaufheizung), `CLAUDE.md` (Regressionsnetz, Einfrierregel, CI-Auswahl, Lauf-Befehl),
  `kern.yml`, `ios.yml`, `Werkzeuge/Gate/nachgate_gebaeudesimulation.sh` und `LIESMICH.md`, `Dokumentation/LIESMICH.md`,
  Basisverweise in drei Wachen.

**CI-Auswahl: 1073 kommt hinein (neunzehn Projekte).** Kein anderes CI-Projekt rechnet die Überhöhung im Vorheizfenster
oder das Vorheizen eines Gebäudes mit Heizkreis; der Lauf dauert rund 2 s, der gestörte Lauf ist PASS.

## Tests

- Neu: `SchnellaufheizungReferenzprojektWacheTests` (5 Fälle): gesäte Zellen und Datenweg, Kopie von 1054 (Einstellungen,
  Gebäude, Zonen samt Bauteilen), einziges Projekt mit Schnellaufheizung, Lauf mit Wirkung und Gegenlauf ohne Schalter,
  Vorheizschlüssel gegen R57.
- Landschaft um 1073 ergänzt: `ZonenReferenzprojektWacheTests` (`Zonenbestand`), `ZonenKuehlkurveReferenzprojektWacheTests`,
  `ZonenUebergabeTests`, `KonditionierungReferenzprojektWacheTests`, `VorheizReferenzprojektWacheTests`,
  `VorheizenSchemaTests`, `SchnellaufheizungSchemaTests`.

## Wiki

Die Wiki-Quellen führten die Schnellaufheizung nicht: ergänzt in `Programm Dokumentation - Simulation.wiki` (Schalter und
Überhöhung in der Gruppe „Aufheizen vor Nutzungsbeginn“, Anker `schnellaufheizung`) und
`Programm Dokumentation - Simulationsergebnisse.wiki` (Ergebnisgrößen der Überhöhung). Upload gebündelt.

**Logbuch:** kein Eintrag für das Referenzprojekt (nichts Sichtbares). Die Schnellaufheizung selbst ist eine sichtbare
Funktion; ihr Logbuchsatz gehört zur Veröffentlichung von SA3 (Entwurf: „Die Aufheizoptimierung kann im Vorheizfenster die
Heizkurve anheben (Schnellaufheizung).“), Version beim Anwender.

## Wiederholung nach einem Heben der Testdatenbank

```bash
dotnet run Referenzlaeufe/Skripte/referenzprojekt_1073_schnellaufheizung.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
```

Vorausgesetzt ist 1072 als höchste Projekt-ID und 1054 unverändert (Sollwertrampe, AK1); danach R57 mit dem Rezept in
`Referenzlaeufe/LIESMICH.md` (Abschnitt „Aktuelle Basis“) neu einfrieren.
