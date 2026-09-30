# SK5 — Kessel-Kennlinie, Etappe E2: Teillast, Normvorgabe η₃₀, Referenzprojekt 1050, Basis R27

Stand: 30.09.2026 · Zweig `ios_migration_september` · Opus-Agent im Worktree, Zweig `kessel-e2` ab `66893957`
(E1 enthalten, Merge `ac5ae487`). Kein Schemaschritt. Konzept
[`Konzept_Kessel_Kennlinie_EPOS-Plan.md`](../../../aktuell/Konzept_Kessel_Kennlinie_EPOS-Plan.md) (4.1, 4.3, 5, 6, 7);
Vorgänger [`SK4_Kessel_Kennlinie_E1_Protokoll.md`](SK4_Kessel_Kennlinie_E1_Protokoll.md).

Commits:
- `a912cad9` Kern, Oberfläche, Ressourcen, Tests
- `50d45284` Testdatenbank mit Referenzprojekt 1050, Skript
- `147a6730` Merge des lokalen `ios_migration_september` (freier Paketteil) vor dem Einfrieren
- `c160b20e` Basis R27 eingefroren, R26 archiviert
- `148748fd` Papiere (Konzept, Wiki-Quelle, dieses Protokoll)
- `a96c1d7d` Merge `origin/ios_migration_september` (`6941a8a1`, Statuszeilen #616/#617)
- `8cd79488` Merge des lokalen `ios_migration_september` (`a06a3819`, Diagrammzweig; Konflikt nur am Ende beider
  `.resx`, beide Blöcke behalten, Designer neu gezogen)
- `ad68ed01` Bestandswachen der Testdatenbank um die Kopie 1050 nachgezogen

## 1 Auftrag und Entscheide

Anwenderauftrag 29.09.2026: „starte das Konzept in der Reihenfolge E1 → E2 → E3 → E4.“ Es gelten:

- **F1:** Leere Felder nehmen die Normvorgaben aus Konzept 7.1.
- **F3:** η₁₀₀ aus Satz 710.01 (in E1 umgesetzt).
- **F4:** Referenzprojekt als Kopie von 1023, keine AK1-Variante, nicht in der CI-Auswahl.
- **F5** (Rückfall-Rücklauf 50 °C) gehört zu E3 und ist nicht gebaut.

## 2 Umsetzung

- **`Kesselkennlinie`** (`EPOS.Kern/Allgemein/Simulation/Kesselkennlinie.cs`, zustandslos):
  - `Eta(β, η₁₀₀, η₃₀)` = η₃₀ + (η₁₀₀ − η₃₀) · (β − 0,3)/0,7, β auf [0,3; 1] geklemmt. Beide Prüfpunkte werden
    bitgenau getroffen; mit η₃₀ = η₁₀₀ ist das Ergebnis für jedes β bitgleich η₁₀₀.
  - `Laststufe` = brennstoffbasierte Wärme / Nennleistung, auf [0, 1]; ohne Nennleistung Volllast.
  - `Bauart`: `Brennwert` = 1 → Brennwertkessel; Beschreibung beginnt mit „Standard“ → Standardkessel; sonst
    Niedertemperaturkessel.
  - `Eta30Vorgabe`: Brennwert η₁₀₀ + 0,06, höchstens Hs/Hi (Gas 1,11, Heizöl 1,06, Holz/Pellets 1,08, sonst 1,0),
    nie unter η₁₀₀; Standard η₁₀₀ − 0,03; Niedertemperatur η₁₀₀. `Eta30Wirksam`: gepflegter Wert (Prozentregel
    über 1,5) vor der Vorgabe.
  - Elektrokessel (Brennstoff 13) rechnen nie mit Kennlinie.
- **Rechenweg:** `SimulationSPK.Stunde_Abschluss` nimmt je Laufstunde `WirkungsgradDerStunde` statt des festen
  Werts; Gasspitze, Brennstoffzähler, Emissionen und Jahresnutzungsgrad folgen unverändert aus dem Brennstoff.
- **Keine neue Betriebsschwelle:** Die Kurve ist in β stetig, die Klemmung kippt keinen Zustand (Probe
  `Am_Knick_ist_die_Kurve_stetig`); die einzige Betriebsentscheidung bleibt `KesselLaeuft` mit dem Zahlenrand.
  Lineare Arithmetik, keine Funktion der Plattformnaht.
- **Mitschrift:** Brennstoff der Laufstunden, Mehrbrennstoff aus Teillast gegenüber η₁₀₀, Stundenreihe des
  Wirkungsgrads; Laufprotokoll je Kessel mit η₁₀₀, η₃₀ und Herkunft (gepflegt oder Normvorgabe mit Bauart).
- **Kennzahlen und Reiter (Konzept 5):**
  - Gruppe „Betrieb“: mittlerer Wirkungsgrad im Betrieb, Brennstoff aus Teillast gegenüber Nennlast.
  - Kesseltabelle: η₃₀ (mit „(Vorgabe)“), Wirkungsgrad im Betrieb, mittlere Laststufe; Striche beim Elektrokessel.
  - CSV-Export: je Brennstoffkessel die Stundenreihe des Wirkungsgrads.
  - `aggregate.csv`: `Kessel[i].Eta100`, `.Eta30`, `.EtaBetrieb`, `.LaststufeMittel`, `.TeillastKwh`.
  - Ressourcen de/en (13 Schlüssel), Designer gezogen.
- **`ParameterVerwendung`:** η₃₀, `Brennwert` und `Beschreibung` rechnen mit; die übrigen vier Kennlinienfelder
  bleiben Dialog bis E3/E4. Die eingefrorene Formularliste nennt die fünf Kennlinienfelder.
- **Tests:** `KesselKennlinieTests` (Kurve, Bauart, Vorgabe, Lauf 1023/1007/1017, gepflegtes η₃₀, Wache 1050),
  `ParameterVerwendungTests`, `ErzeugerReiterTests`.

## 3 Referenzprojekt 1050

`Referenzlaeufe/Skripte/referenzprojekt_1050_kesselkennlinie.cs` kopiert 1023 auf dem Kopierweg des Programms und
setzt am Projektkessel die neutralen Werte aus Konzept 4.3: η₁₀₀ 0,97, η₃₀ 1,05, `Kennlinie_Brennwert` 1,
`Mindestleistung` 3,86 kW (20 % von 19,3 kW), `Anfahrverlust_kWh` 0,1, `Mindestlaufzeit_min` leer. Die nächste freie
Projektnummer war 1050 (höchste 1049, 28 Projekte). Das Skript ist wiederholbar (zweiter Lauf „nichts zu tun“),
prüft Zielzellen, die unveränderte Vorlage, `integrity_check` und `foreign_key_check`.

Testdatenbank: **81 690 624 Byte, LFS-SHA-256 `5bca909d901db93c211884ade28f9853cb6df60731e54111f7a605922b2909ac`**
(vorher `111be189…`, 71 626 752 Byte; der Zuwachs ist die Projektkopie samt 8 760 Zeilen `Tab_Solar`), mit
LFS-Filter committet. SQL-Prüfer 0 Fundstellen, `Testdatenbankschema --trocken` 0/0.

## 4 Basis R27 und A/B gegen R26

`Referenzlaeufe/2026-09-30_R27_Kesselteillast/` — sechzehn Projekte, 487 CSV, 2 920 Skalare, auf Linux eingefroren.
Zweiter Lauf 487/487 byte-gleich; gestört (`--stoerung ulp`) gegen ungestört 16/16 PASS, 480/487 byte-gleich
(dieselben sieben Dateien wie mit R26). R26 archiviert unter
[`Referenzbasen/`](../../Referenzbasen/LIESMICH.md).

A/B gegen R26 (`vergleich --ohne` die fünf neuen Schlüssel): **14/15 PASS**, 459/460 CSV byte-gleich. Nach Ursache:

| Ursache | Wirkung |
|---|---|
| Rechenweg Teillast und Normvorgabe | allein `aggregate.csv` von 1023 (Gas, Nutzungsgrad, `Em.Kessel.Co2T`, `Em.Kessel.NoxKg`) |
| neue Skalare | `aggregate.csv` aller fünfzehn Projekte um 5 Schlüssel je Kessel |
| neues Projekt | `Projekt_1050/` (27 CSV) |

Je Kesselprojekt (Brennstoff = `HeizkesselModul[0].Verbrauch` in MWh/a, Nutzungsgrad in %):

| Projekt | Bauart | η₁₀₀ / η₃₀ | Brennstoff R26 → R27 | Nutzungsgrad R26 → R27 | η Betrieb | Laststufe | Teillast kWh/a |
|---|---|---|---|---|---|---|---|
| 1007, 1046 | NT | 0,876 / 0,876 | 10,55 → 10,55 | 85,8 → 85,8 | 0,876 | 0,23 | 0 |
| 1008 | NT | 0,876 / 0,876 | 23,65 → 23,65 | 86,9 → 86,9 | 0,876 | 0,38 | 0 |
| 1018 | NT | 1,0 / 1,0 | 11,09 → 11,09 | 100 → 100 | 1,000 | 0,03 | 0 |
| 1023 | Brennwert | 0,874 / 0,934 (Vorgabe) | **91,42 → 91,02** | **87,29 → 87,67** | 0,878 | 0,82 | −396 |
| 1030 | NT | 1,0 / 1,0 | 5 403,1 → 5 403,1 | 100 → 100 | 1,000 | 0,36 | 0 |
| 1039 | NT | 0,98 / 0,98 | 295,17 → 295,17 | 98 → 98 | 0,980 | 0,76 | 0 |
| 1040 | NT | 1,0 / 1,0 | 16,19 → 16,19 | 100 → 100 | 1,000 | 0,09 | 0 |
| 1041 | NT | 1,0 / 1,0 | 149,91 → 149,91 | 100 → 100 | 1,000 | 0,21 | 0 |
| 1042 | NT | 0,98 / 0,98 | 19,32 → 19,32 | 98 → 98 | 0,980 | 0,05 | 0 |
| 1045 | NT | 1,0 / 1,0 | 22,27 → 22,27 | 100 → 100 | 1,000 | 0,09 | 0 |
| 1049 | NT | 1,0 / 1,0 | 6,32 → 6,32 | 100 → 100 | 1,000 | 0,06 | 0 |
| 1017, 1024, 1047 | Elektro | ohne Kennlinie | 0 → 0 (Strom) | unverändert | – | – | 0 |
| **1050** | Brennwert | 0,97 / 1,05 (gepflegt) | – → **81,96** | – → **97,37** | 0,975 | 0,82 | −424 |

**Größenordnung.** 1023 und 1050 erzeugen dieselbe Wärme (79,80 MWh/a). Der Kessel ist mit 19,3 kW knapp — der
Wärmebedarf bleibt zu 45 % ungedeckt —, er läuft im Mittel bei 82 % Last und wärmegewichtet fast immer bei
Volllast. Deshalb liegt sein Wirkungsgrad im Betrieb nur wenig über η₁₀₀ (1023: 0,878 bei η₁₀₀ 0,874; 1050: 0,975
bei 0,97), und die Teillast spart nur 0,4 % bzw. 0,5 % Brennstoff. Das Konzeptbeispiel (β = 0,6: η ≈ 1,016) ist in
`KesselKennlinieTests` gehalten.

**Befund B-1 (Bauartregel).** Nach Konzept 7.1 entscheidet `Brennwert` der Projektkopie. Er ist gesetzt nur in
1023 (und 1009, kein Referenzprojekt); alle übrigen Brennstoffkessel der Referenzprojekte — nach Beschreibung
Brennwertgeräte — rechnen als Niedertemperaturkessel mit η₃₀ = η₁₀₀ und bleiben byte-gleich. Die Erwartung
„jedes Referenzprojekt mit Kessel wandert“ trifft deshalb in E2 nicht zu; sie träfe zu, wenn die Projektkopien das
Kennzeichen trügen (die Nachpflege F2 hat nur den Katalog gekennzeichnet). Mit Kennzeichen bekämen auch die
Platzhalter η₁₀₀ = 1,0 (1018, 1030, 1040, 1041, 1045, 1049) ein η₃₀ von 1,06. Ein Nachziehen der Projektkopien ist
eine eigene Anwenderentscheidung mit Neueinfrierung.

## 5 Einfrierregel

„Gesäte Kesseldaten“ (`CLAUDE.md`, `Referenzlaeufe/LIESMICH.md`) ist erweitert: die fünf Kennlinienspalten eines
Referenzkessels, sein Schalter `Brennwert` und die Bauart in `Beschreibung`, dazu das Anlegen oder Entfernen von
1050. Die in Konzept 3.3 genannte eigene Regel „gesäte Kesselkennlinie“ ist darin aufgegangen.

## 6 Gate

Gate auf `ad68ed01` (enthält origin `6941a8a1` und den Diagrammzweig `a06a3819`), Linux, `TMPDIR=/dev/shm`:

- Kern-Filter Release: 0 Fehler.
- Tests mit den xUnit-Schaltern: KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 (1
  übersprungen), EPOS.UI.Tests 6 956, EPOS.Kern.Tests 9 284 (1 übersprungen) — zusammen **17 202 grün, 0 rot**. Der
  erste volle Lauf fand fünf Bestandswachen, die die Zeilen der Testdatenbank zählen (Gebäude-, Träger- und
  Kostenzeilen der Kopie 1050); nachgezogen in `ad68ed01`, danach EPOS.Kern.Tests voll wiederholt.
- Werkzeugtests: Formularkarte 124, Auslieferungsvorlage 43, Gebaeudevergleich 24, ZapfprofilValidierung 39 — grün.
- SQL-Prüfer (Python 3.12): 2 135 Texte, 0 Fundstellen.
- ChartProben: 221 Bilder, 0 Verstöße; 185 Hashes gleich der Linux-Messlatte `Messlatte_2026-09-29.sha256`.
- `EPOS.Referenzlauf` über alle sechzehn Projekte gegen R27: **GESAMT PASS, 487/487 CSV byte-gleich**; gestört
  (`--stoerung ulp`) gegen ungestört 16/16 PASS, 480/487 byte-gleich.
- Windows-Schale (`EnableWindowsTargeting=true`, Debug x64): 0 Fehler.

Nach dem Merge mit origin `94f91fed` (#618, KP2; Merge `2ed8ecd6`, automatisch) dasselbe Gate noch einmal: Kern-Filter
0 Fehler; Tests **17 319 grün, 0 rot** (EPOS.Kern.Tests 9 333, EPOS.UI.Tests 7 024, KiKern.Tests 549,
SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27; 2 übersprungen); Werkzeugtests 124/43/24/39 grün; SQL-Prüfer
2 135 Texte, 0 Fundstellen; ChartProben 221 Bilder, 0 Verstöße, Messlatte gleich; sechzehn Projekte gegen R27
GESAMT PASS, 487/487 byte-gleich, gestört 16/16 PASS (480/487); Windows-Schale 0 Fehler.

## 7 Wiki und Logbuch

Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Heizkessel.wiki`: Abschnitt „Teillastkennlinie“ (Formel,
Vorgabe je Bauart, neutrales Beispiel), Gruppe „Kennlinie“ unter den Kennwerten, Kennzeichen Brennwertkessel,
Ergebnisse, zwei Fallstricke. Tabu-Grep leer, keine Produktdaten.

Logbuch unter **1.2.0.6**: „Heizkessel rechnen je Stunde mit dem Wirkungsgrad ihrer Laststufe nach einer
Teillastkennlinie; ohne gepflegten Wert bei 30 % Last gilt eine Vorgabe nach Bauart.“

## 8 Offene Punkte

1. **E3 Brennwert:** Rücklaufkette mit Rückfall 50 °C (F5), Brennstofftafel um Taupunkt und Δ₃₀ ergänzen
   (`Kesselkennlinie.HsHi` ist schon da), Kohärenzzeile; Neueinfrierung von 1050. **E4 Takten.**
2. Befund B-1: Kennzeichen `Brennwert` der Projektkopien — Anwenderentscheidung.
3. Nicht in E2: Vorlagenfelder des Berichts (Konzept 5, letzter Punkt) und die kleine Kurve im Katalogeditor.
4. Windows-Lauf gegen R27 steht aus (erwartet wie in `Werkzeuge/Gate/LIESMICH.md`).
5. Statuszeile in `Status_iOS_Migration.md` beim Zusammenführen.
