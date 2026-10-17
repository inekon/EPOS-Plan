# EB-4 — Standby der Flottenvorbelegung, Merge mit R58, Testdatenbank 239, Katalogfassung 27, Basis R59 auf R58

Sitzung Dialoge und Korrekturen, 17.10.2026, Zweig `eb2` (Kopf vor EB-4 `bfba9c238`), zweimal mit
`origin/ios_migration_september` zusammengeführt (`835ef152e`, danach der R58-Nachschub `93eb06f0b`). Konzept:
[`2026-10-17_Konzept_Ergebnisreihen_Einspeisung_Standby_Abregelung.md`](../../../aktuell/Dialoge/2026-10-17_Konzept_Ergebnisreihen_Einspeisung_Standby_Abregelung.md);
Vorgänger [EB-1](2026-10-17_EB-1_Ergebnisreihen_Kern.md), [EB-2](2026-10-17_EB-2_Ergebnisreihen_Oberflaeche.md).
Nicht gepusht; Gate und Push folgen in einem eigenen Auftrag.

## Befund: der Standby nach einem Lauf aus der Oberfläche war immer 0

- Der Lauf der Oberfläche (`SimulationErgebnisHuelle.Laufen`, `SimulationControl.SpeicherflotteAusLaufVorbelegen = true`)
  rechnet den Speicher an seiner Speicherstufe über die vorbelegte Flotte: `SpeicherflotteVorbelegen` →
  `StromspeicherAuslegungCtrl.Vorgaben()` → `SpeicherFlottenStudieCtrl.Vorbelegung` → ohne gespeicherten Stand `@Aktuell`
  `EinheitenAusProjektanlagen` (eine Einheit je Speicheranlage, #210) → `Einheit(SpeicherParameter p, …)`.
- **Ursache:** `Einheit` baute die `FlottenEinheit` aus dem Parametersatz der Anlage — Kapazität, Leistung, Wirkungsgrade,
  SoC-Band, Start-SoC — **ohne `p.StandbyKw` und `p.SelbstentladungProzentMonat`**. `StromspeicherSimCtrl.LeseParameter`
  liest beide (geprüft auf 0 … 1 000 W und 0 … 20 %/Monat), der Flottensimulator rechnet den Standby aber allein aus
  `FlottenEinheit.HilfsverbrauchKw` (und die Selbstentladung aus `SelbstentladungProzentProMonat`); beide blieben 0.
  `SpeichersystemEigenverbrauchKwh` ist im Flottenpfad die Summe der Hilfsverbräuche der Intervalle — also 0, und damit
  fehlten Standby-Reihe im Betriebsbild, CSV-Spalte, Kennzahl und die Ergebnisspalte `Eigenverbrauch_System`.
  `Parameter.StandbyKw = 0` im Kontext der Hülle war nur ein Symptom (der Kontext der Flotte trägt den Sammelsatz der
  aktiven Variante; 1046 hat an keinem Gerät einen Standby).
- Dieselbe Lücke traf die **Selbstentladung** (`Selbstentladung_Prozent_Monat`), aus derselben Ursache — mitbehoben.
  Die Katalogabbildung `EinheitAusKatalogsatz` trug beide schon.

## Behebung (Kern)

- `SpeicherFlottenStudieCtrl.Einheit` setzt `HilfsverbrauchKw = p.StandbyKw` und
  `SelbstentladungProzentProMonat = p.SelbstentladungProzentMonat` — dieselbe Abbildung wie aus dem Katalog; die Bereiche
  prüft `LeseParameter` (Werte außerhalb zählen dort nicht). Betroffen sind damit die Vorbelegung des Laufs, die
  Vorbelegung der Auslegungsseite und „Speicher hinzufügen“ aus einer Projektanlage (`EinheitAusProjektanlage`) — ein Weg.
- Neuer Test `EPOS.Kern.Tests/SpeicherStandbyVorbelegungTests` (Projekt 1007, 20 W am Gerät der aktiven Variante; drei der
  vier Speicheranlagen teilen dieses Gerät): die Einheit der Vorbelegung trägt Standby und Selbstentladung ihrer Anlage;
  Einzellauf (`SimulationRunner` ohne Vorbelegung) 175,2 kWh/a, vorbelegte Flotte aller vier Anlagen 525,6 kWh/a (3 × 20 W),
  ohne Standby 0; der Kachelweg (Hülle, `Laufen`) schreibt 525,6 kWh/a in `Tab_ErgebnisStromspeicher.Eigenverbrauch_System`,
  aktiviert seine Flotte als Projektflotte, und der Runner rechnet danach denselben Standby.
- **Folge für die Oberflächenergebnisse:** Nach einem Lauf aus der Oberfläche erscheinen Standby-Reihe („Standby
  Speichersystem“ im Betriebsbild), die CSV-Spalte des Stromspeicher-Reiters, die Kennzahl `energie.speicher_standby` und
  das Band im Energieflussbild, sobald ein Speichergerät des Projekts `Standby_Verbrauch` trägt; die Selbstentladung
  wirkt im Flottenlauf wie im Einzellauf. Ohne gepflegten Standby bleibt alles, wie es war (0, keine Reihe).
- **Was nicht dieselbe Ursache ist (benannt, nicht geändert):** Einzellauf und Lauf der Oberfläche rechnen den Speicher
  nach Entwurf verschieden (SD‑Q2, #210, Anwenderentscheid 04.10.2026): der Einzellauf die Anlage der aktiven Variante,
  die Vorbelegung eine Flotte aus **allen** Speicheranlagen des Projekts — bei 1007 deshalb 525,6 gegen 175,2 kWh/a. Erst
  die Aktivierung der gerechneten Flotte durch die Hülle bringt beide Wege zusammen. Projekt 1046 der Testdatenbank trägt
  eine gesäte Projektflotte (`@Projektflotte`, Hilfsverbrauch 30 + 20 W → 438 kWh/a über `SimulationRunner`), aber keinen
  Stand `@Aktuell`; die Vorbelegung liest `@Projektflotte` nicht und bildet die Flotte aus den vier Anlagen, deren
  Geräte keinen `Standby_Verbrauch` tragen — über die Oberfläche bleibt der Standby von 1046 deshalb auch jetzt 0, bis
  ein Gerät einen Standby bekommt. Der Referenzlauf (`SimulationRunner`) ist von alledem nicht berührt: Vergleich unten.

## Merge mit origin (zweimal)

- `835ef152e` (Sitzung Kostenbasis, #1170): Basis R58 `2026-10-17_R58_Kaeltesummen`, Testdatenbank 238 mit Datenbereinigung
  8e (`c8befb51…`), Vorlagenfeldkatalog v26 (Hilfsstrom). Konflikte: 1 233 Rename/Rename der Basis (R57 → R58 gegen
  R57 → R59) — R58 von origin genommen, unsere R59 entfernt und unten neu eingefroren; `Kenndaten_Test.sqlite` — origin-Fassung
  genommen und gehoben; `Vorlagenfeldkatalog_v26.txt` (add/add) — origin; `Vorlagenfeldkatalog.cs` (Zusammenfassung,
  `KATALOGFASSUNG` 27), `Vorlagenfeldkatalog.Ergebnisse.cs` (beide Konstanten: `FASSUNG_HILFSSTROM` 26,
  `FASSUNG_ERGEBNISREIHEN` 27), `BerichtEnergieflussTests` (Kommentar); die Binärvorlagen — origin, danach neu gebaut;
  `Berichtsvorlage_Beispiel.docx` ist auf origin entfernt (Commit 15), die Löschung bleibt; `kern.yml`, `ios.yml`, Gate-Skript
  und -LIESMICH, vier Wache-Tests, `CLAUDE.md`, `Dokumentation/LIESMICH.md`, Statuskopf Zeile 15, Referenzbasen-LIESMICH
  und Basenhistorie — inhaltlich auf R59 zusammengeführt. `.resx`, Designer, `KennzahlenKatalog`, `ErgebnisCtrl`,
  `SimulationErgebnisCtrl`, `BerichtsDaten`, Wiki-Quellen und `epos-ui.css` gingen ohne Konflikt zusammen.
  Konfliktmarker repoweit: keine.
- `93eb06f0b` (R58-Nachschub „8d einheitlich“, zwölf `aggregate.csv` von R58 nachgeführt, Kältespalten der Modulzeile
  ungerundet `be851c1f0`, BK-9/BK-10, #1172): ohne Konflikt. Der Nachtrag „8d einheitlich“ wandert mit dem Wortlaut von R58
  ins Archiv; R59 ist gegen **dieses** R58 gerechnet.

## Testdatenbank 239

Aus der origin-Fassung 238 (`c8befb51…`, 109 604 864 Byte, mit der Datenbereinigung 8e) mit `Werkzeuge/Testdatenbankschema`
gehoben: Schritt 239 legt die drei Spalten an, kein DML; 623 Objekte (198 `STRICT`-Tabellen, Indizes) und die Zeilenzahl jeder
der 199 Tabellen gleich; `integrity_check` ok, `foreign_key_check` leer. **109 608 960 Byte, LFS-SHA-256
`5bfedf09beef9ccf636f6f0272b46d7fa632e66b22b044713cf717e20fbb1fa2`** (nur mit LFS-Filter committet; der Nachtrag 239 in
`Referenzlaeufe/LIESMICH.md` ersetzt den alten mit `2fb633f6…`).

## Katalogfassung 27

- Fassung 26 von origin (Hilfsstrom: Menge, Emissionen, Stromsteuer) bleibt; unsere drei Kennzahlen (Einspeisung BHKW,
  Abregelung PV, Standby Speichersystem) tragen `Seit = FASSUNG_ERGEBNISREIHEN = 27`; `KATALOGFASSUNG` und
  `KatalogfassungWord` 27, `VorlagenfeldkatalogWacheTests` auf 27.
- Messlatte `EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v27.txt` aus dem Lauf der Wache: die origin-Liste v26 plus Kopfzeile
  „Katalogfassung 27“ und die 45 erzeugten Einträge der drei Kennzahlen (unsere frühere v26-Liste ist durch die von origin ersetzt).
- Neun Berichtsvorlagen mit `Werkzeuge/Berichtsvorlage alle` neu gebaut (`custom.xml` Fassung 27, 1 934 Schnellbausteine je
  Sprache), OpenXmlValidator Office 2007 bis 2021: 0 Fehler. Keine Berichtsmesslatte betroffen.

## Referenzlauf gegen R58 und Basis R59

- 38 Projekte mit dem gemergten Stand (`79cf8532c`, Testdatenbank 239) gegen das nachgeführte R58: 38 von 38 gerechnet;
  der volle Vergleich meldet **genau 34 Einträge „nur im Vergleichslauf“** — `BHKW.Stromeinspeisung` in 19 und
  `Photovoltaik.Abregelung` in 15 `aggregate.csv` —, sonst nichts; mit `--ohne BHKW.Stromeinspeisung,Photovoltaik.Abregelung`
  **GESAMT PASS (13 025 990 Werte)**. Byte-Vergleich (Information): 1 199 der 1 233 CSV gleich, die 34 übrigen sind die
  `aggregate.csv` mit den neuen Schlüsseln. Kein bisheriger Wert hat sich geändert — auch nicht die zwölf nachgeführten
  Kältesummen.
- **R59 `Referenzlaeufe/2026-10-17_R59_Ergebnisreihen`** aus diesem Lauf geschrieben (1 233 CSV, 8 630 Skalare); R58 nach dem
  Muster archiviert: Ordner entfernt, `protokoll.txt` (mit dem Nachtrag „8d einheitlich“) und der Wortlaut des Abschnitts
  „Aktuelle Basis“ unter `Dokumentation/ueberholt/Referenzbasen/` (Tabelle, Abschnitt „Die Basis R58 im Einzelnen“);
  `Referenzlaeufe/LIESMICH.md`: Abschnitt „Aktuelle Basis“ R59 (Anlass, Rezept), „Die Vorgängerbasis R58“, Nachtrag 239 mit
  der neuen Signatur, Liste „Entfernte Basen“. Verweise auf R59: `CLAUDE.md` (Regressionsnetz-Kopf), `kern.yml`, `ios.yml`,
  `Werkzeuge/Gate/nachgate_gebaeudesimulation.sh` und `LIESMICH.md`, die vier Wache-Tests (Kommentare), `Dokumentation/LIESMICH.md`,
  Basenhistorie, Übergabepapier Gebäudesimulation, Statuskopf Zeile 15 (R59 eingefroren mit EB-4 auf R58, nächste freie
  Basis R60), Kurzkonzept Abschnitt 3. Keine neue Einfrierregel.
- **Für den Push-Auftrag — dieselben Befehle, falls R58 bis dahin noch einmal nachgeführt wird** (dann R59 ersetzen):

  ```bash
  dotnet build WP-Plan.Kern.slnf -c Release
  dotnet build EPOS.Referenzlauf/EPOS.Referenzlauf.csproj -c Release
  dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf \
    --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
    --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047,1049,1050,1051,1052,1054,1055,1056,1057,1058,1059,1060,1061,1062,1063,1064,1065,1066,1067,1068,1069,1070,1071,1072,1073 \
    --ziel <ordner-neu>
  # Erwartung: nur „Eintrag nur im Vergleichslauf“ für BHKW.Stromeinspeisung (19) und Photovoltaik.Abregelung (15)
  dotnet run --project EPOS.Referenzlauf -c Release --no-build -- vergleich <R58-ordner> <ordner-neu>
  # Erwartung: GESAMT PASS (13 025 990 Werte)
  dotnet run --project EPOS.Referenzlauf -c Release --no-build -- vergleich <R58-ordner> <ordner-neu> \
    --ohne BHKW.Stromeinspeisung,Photovoltaik.Abregelung
  # Einfrieren: Ordner ersetzen, dann Protokoll und Wortlaut von R58 ins Archiv, Verweise prüfen
  rm -rf Referenzlaeufe/2026-10-17_R59_Ergebnisreihen && cp -r <ordner-neu> Referenzlaeufe/2026-10-17_R59_Ergebnisreihen
  git grep -n '2026-10-17_R58_Kaeltesummen' -- . ':!Dokumentation/ueberholt' ':!Referenzlaeufe/LIESMICH.md'
  ```

## Kurzprüfung (kein volles Gate)

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler (nach beiden Merges).
- `EPOS.Kern.Tests` gefiltert (`Ergebnisreihen|Energiefluss|Stromspeicher|Speicherflotte|SpeicherStandby|SpeicherVorbelegung|Vorlage|
  Bericht|Schema|Wache|Kennzahl`, 29 min): 2 779 Tests, 2 778 grün, 1 rot — `ErgebnisreihenOberflaecheTests` erwartete für die drei
  Kennzahlen noch `Seit = 26`; auf 27 gesetzt (Test heißt jetzt `…_seit_Katalog_v27`). Nachlauf `ErgebnisreihenOberflaeche|
  DokumentationLinkWache|RepositoryOrdnungWache|VorlagenfeldkatalogWache|SpeicherStandbyVorbelegung|BerichtEnergiefluss`: 90/90 grün
  (die Dokumentationswache mit diesem Protokoll, die Katalogwache mit der Messlatte v27).
- `EPOS.UI.Tests` gefiltert (`Ergebnis|Stromspeicher|Uebersicht|Vorlage`): 996/996 grün.
- `Werkzeuge/Auslieferungsvorlage.Tests`: 63/63 grün.
- `SqlDialektPruefer`: 3 058 SQL-Texte, 0 Fundstellen.
- `Werkzeuge/ResourceDesigner/designer_neu.py` (nur prüfen): 19 162 Einträge, unverändert; `.resx` beider Sprachen ohne Dubletten
  (19 165 Einträge je Datei).
- Kein volles Gate, keine ChartProben (kein Bild berührt), keine Windows-Schale (keine Hülle oder Naht der Schale berührt).


## Commits (Zweig `eb2`, nicht gepusht)

- `08d096b0a` EB-4: Standby und Selbstentladung in der Flottenvorbelegung (Kern, Test)
- `cde278ae4` Merge origin/ios_migration_september (R58, Katalog v26, DB 238+8e) in eb2 — Konflikte, Testdatenbank 239,
  Katalogfassung 27 (Konstanten), Papiere auf R59, R58-Wortlaut ins Archiv
- `79cf8532c` Merge origin/ios_migration_september (R58-Nachschub `93eb06f0b`), ohne Konflikt
- `b71ae6d63` EB-4: Basis R59 auf R58 neu eingefroren, R58 ins Archiv (Verweise, LIESMICH, Kurzkonzept)
- `d1ab72334` EB-4: Katalogfassung 27 — Messlatte v27, Berichtsvorlagen neu gebaut
- fünfter Commit: EB-4: Protokoll


## Offen

- Gate und Push (eigener Auftrag); Statuszeilen schreibt der Push-Auftrag.
- Sichtprüfung unter Windows: Lauf aus der Oberfläche mit einem Speicher mit `Standby_Verbrauch` — Betriebsbild,
  CSV-Spalte, Kennzahl und Energieflussbild zeigen den Standby.
- Entwurfsfrage, nicht Teil dieses Auftrags: Die Vorbelegung des Laufs liest eine aktivierte Projektflotte ohne Stand
  `@Aktuell` nicht (Testdatenbank 1046); ob sie das soll, ist ein Anwenderentscheid.
