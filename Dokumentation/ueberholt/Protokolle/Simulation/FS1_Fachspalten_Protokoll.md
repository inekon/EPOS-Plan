# FS1 — Fachspalten-Verlust im Del+Add-Speicherweg der Energieanlagen: Befund, Reproduktion, Fix

Stand: 02.09.2026 · Branch `ios_migration` · Basis HEAD `7c622b1` (PA1c) · Bezug: Befund der
Paket-A-Erkundung (PV-Ertragsmodell, 02.09.2026), Nachbarn **AP9b** (Speichervarianten-Rettung)
und **S1** (Senken-Rettung) in `Controller/WizardCtrl.cs`. Build x64 Debug out-of-tree
(VS-18-MSBuild, `/p:OutputPath` auf Scratch): 0 Fehler, nur die bekannten WFO1000-Warnungen.

## 1. Befund (am Code verifiziert)

Der Speicherweg **aller** Erzeuger ist Löschen + Neuanlegen: `WizardCtrl.Del_Projekt_Waermeerzeuger`
(typlos `:55`, typisiert `:68`) gefolgt von `Add_WP_Waermeerzeuger` (`:1379`), das jede Anlagenzeile
über die **eine** Einfügeanweisung `WizardCtrl.SQL_ANLAGE_INSERT` + `AnlagenParameter` aus dem
`WErzeugerModel` neu schreibt. Die Anweisung führt **58 Spalten**; `Tab_Energieanlagen` hat auf der
Produktiv-DB **71** (nach Migrationsschritt 62: 73). Die Differenz ohne `ID` sind **14 Fachspalten**,
die das Modell bewusst nicht kennt — nach jedem Speichern stehen sie auf **NULL**:

| # | Spalte | Migrationsschritt | Schreibweg (Fachcontroller) | Leser | Datenlage Produktiv-DB 02.09. |
|---|---|---|---|---|---|
| 1–8 | `KWKG_Stichtag`, `KWKG_Inbetriebnahme`, `KWKG_Anlagenart`, `KWKG_Eigenstromfall`, `KWKG_Satz_Einspeisung`, `KWKG_Satz_Eigen`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel` | 22 (Etappe E6) | `KwkgAnlagenCtrl.Speichere` — zielgenaues UPDATE der acht Spalten (`Allgemein/Wirtschaftlichkeit/KwkgAnlagenCtrl.cs:141-168`) | `WirtschaftlichkeitCtrl.LiesBhkwAnlagen`, `KwkgAnlagenCtrl.Lade` | `Anlagenart`/`Eigenstromfall` = `''` auf 11 Nicht-BHKW-Zeilen (Projekte 1032/1035/1043), sonst NULL |
| 9–10 | `WQ_Anschlusshoehe`, `WQ_ID_Quellprofil` (FK auf `Tab_Quellprofil`) | 54 (Paket Q1) | `Form_Simulation_Config.Uebersicht.cs:985/1054` über `WaermequelleClass.WertSchreiben` | `SimulationWaermepumpe :272`, `SimulationSPK :429`, `WaermequelleClass :650`, `Uebersicht :914/1038` | NULL |
| 11 | `WQ_TemperaturModus` | 55 (Paket B2) | `Uebersicht.cs:1002` über `WertSchreiben` | `SimulationControl :3690`, `Warnkriterien :1216`, `Uebersicht :924` | `'Berechnet'` auf **113 von 115** Zeilen |
| 12–14 | `Energiesteuer_Wahl`, `Aufteilung_Methode`, `Hilfsenergie_Anteil` | 61 (Etappe B3 Paket a) | Wirtschaftlichkeitsmodul (zielgenau, Muster E6) | `WirtschaftlichkeitCtrl.LiesAnlagen :4109-4199`, `HilfsstromRechner :191-209`, `KohaerenzPruefung :225-235` | NULL |

Warum das Modell sie nicht trägt: `SchemaKatalog.Alle` (`:3409-3495`) führt die Schritte 22 und 61
bewusst **nicht** in der Rückfallebene — „der Grund ist der LESER, nicht die Tabelle": der Rechenkern
liest keine dieser Spalten, sie gehören dem Wirtschaftlichkeitsmodul. `KwkgAnlagenCtrl` schreibt in
Gegenrichtung ausdrücklich nur seine acht Felder und „darf die Spalten des Rechenkerns nicht anfassen".
Die Schritte 54/55 sind Rechenkern-Spalten, wurden aber ebenfalls nur per Einzel-UPDATE nachgerüstet.
Paket A (Schritt 62) hat seine zwei Spalten in den INSERT aufgenommen und den Rest protokolliert.

**Betroffene Aufrufstellen (zehn Del+Add-Paare, sechs Dateien):** `Views/Hauptformular/Form_Start.cs:600, 641, 678, 1226, 1390, 1534, 1804`;
`Controller/{BHKW,Heizkessel,PufferSp,PV,Solar,Stromspeicher,WP}KontextMenuCtrl.cs` (`:134, :140, :139, :130, :134, :222/:321, :222-224`);
`Views/Simulation/Form_Simulation_Detail.cs:5069`; `Views/Wizard/WizardParent.cs:665-668` (typlos).
Nicht betroffen: `StromspeicherKontextMenuCtrl.cs:423` (Variante anlegen = Insert ohne Löschen) und
`KomponentenUebernahmeCtrl.cs:411` (Übernahme in ein anderes Projekt — kopiert die Fachspalten nie, eigener Weg, siehe 4.).

## 2. Fix — Block FS1 in `Controller/WizardCtrl.cs`

Entscheidung: **Retten statt INSERT erweitern.** Die Werte werden bewusst nur über ihre Fachcontroller
gepflegt; ein Modell, das sie mitschleppt, trüge sie außerdem *veraltet* zurück, sobald der Fachdialog
nach dem Laden der Erzeugerliste gespeichert hat. Die Rettung liest unmittelbar vor dem DELETE aus der
Datenbank — Muster AP9b/S1 (Frage 23 / `Extrapolation_erlaubt` in `KonfigurationCtrl`).

| Baustein | Inhalt | Stelle |
|---|---|---|
| **Spaltenmenge = Komplement** | `WizardCtrl.Fachspalten()`: alle Spalten von `Tab_Energieanlagen` (`DataRepository.SpaltenVonTabelle`) minus die in `SQL_ANLAGE_INSERT` genannten (einmal aus der Anweisung geparst, `InsertSpalten()`), ohne `ID`. **Keine zweite Liste**: eine in den INSERT aufgenommene Spalte verlässt die Rettung von selbst, eine neue Fachspalte ist von selbst geschützt; auf einer DB vor den Migrationsschritten ist die Menge leer. | `WizardCtrl.cs:1149-1187` |
| **Sichern** | `FachspaltenSichern(projektID, nType)`: liest genau die Zeilen, die der Löschbefehl trifft (typlos `ID_Type <> 12` wie FR-1, typisiert `ID_Type = ?`), merkt je Zeile `(ID_Type, Bezeichner)` und **nur belegte** Fachwerte — im Arbeitsspeicher, wie die Nachbarn mit Projekt-Wächter (`m_FachspaltenProjekt`). | `:1199-1257`; Aufrufe `:59` (typlos), `:78` (typisiert) |
| **Wiederherstellen** | `FachspaltenWiederherstellen(projektID)`: nach `SenkenWiederherstellen`, **vor** `GeraeteWaisen.Aufraeumen`; je neuer Zeile, deren Fachspalten sämtlich NULL sind, die erste unverbrauchte Sicherung zu `(ID_Type, Bezeichner)` (Namensdoppel in Zeilenreihenfolge: n-te alte → n-te neue Zeile) — **ein UPDATE** mit genau den alt belegten Spalten. Idempotent, überschreibt nichts Gesetztes, lässt stehen gebliebene Puffer (FR-1) in Ruhe. BEST EFFORT, Konsolenprotokoll „Fachspalten-Rettung: n Fachwert(e) auf m Anlagenzeile(n) …". | `:1264-1347`; Aufruf `:1642` |
| **Verwerfen** | `FachspaltenVerwerfen` auf beiden Fehlerwegen des Add (Insert gescheitert / Ausnahme), wie `SpVariantenVerwerfen`. | `:1352`; Aufrufe `:1550`, `:1662` |
| **Doku** | Absatz „NICHT VOLLSTAENDIG" an `SQL_ANLAGE_INSERT` benennt jetzt die Fachspalten und verweist auf FS1. | `:204-213` |

Unangetastet: `WErzeugerModel`, `WErzeugerCtrl.AusZeile`, `AnlagenParameter`, alle zehn Aufrufstellen.
Encoding der Datei gemessen (perl `:raw`) vor/nach Patch: UTF-8 **mit** BOM, reines CRLF (1914 → 2229),
Nicht-ASCII-Bytes unverändert 288 (neuer Block umlautfrei). +321/−6 Zeilen.

## 3. Verifikation (headless, Kopie der Produktiv-DB)

**Methode:** Snapshot der Produktiv-DB per SQLite-Backup-API (`C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`,
nur lesend geöffnet `mode=ro`; MD5 des Snapshots `47bcefaca0f18d2180ba37786c6cb6b3` = identisch mit
PA0-Snapshot; Zeitstempel der Produktiv-DB 22:07:36 unverändert). Zwei App-Stände aus `git archive`
out-of-tree gebaut: **vorher** = HEAD `7c622b1`, **nachher** = HEAD + `WizardCtrl.cs`-Patch. Prüfstand
`fs1runner` (Muster kd1runner: Resolver auf `appbin`, `e_sqlite3.dll` aus `runtimes\win-x64\native`,
`DataRepository.PfadUeberschreibung` auf die Kopie; `WizardCtrl`/`WErzeugerCtrl` sind `internal` →
Reflection). Ablauf je Lauf, Projekt **1030** „Referenz BHKW-Kaskade" (2 BHKW, 1 Kessel, 1 Puffer):

1. Migration der Kopie 61 → 62 (der INSERT nennt seit Schritt 62 die PV-Spalten).
2. Fachwerte **über die Fachcontroller** setzen: `KwkgAnlagenCtrl.Speichere` (alle 8 KWKG-Spalten, je BHKW verschieden), `WaermequelleClass.WertSchreiben` für `Energiesteuer_Wahl`/`Aufteilung_Methode`/`Hilfsenergie_Anteil` (BHKW) und `WQ_Anschlusshoehe`=0,42, `WQ_ID_Quellprofil`=1 (angelegte Probezeile in `Tab_Quellprofil`), `Hilfsenergie_Anteil`=1,5 (Kessel); `WQ_TemperaturModus`='Berechnet' ist Bestandswert.
3. **Weg A** = BHKW-Karte/-Kontextmenü: `ReadAllFilter("ID_Projekt=1030 and ID_Type=11")` → `Del_Projekt_Waermeerzeuger(1030, 11)` + `Add_WP_Waermeerzeuger`.
4. **Weg B** = Wizard bearbeiten: alle Typen außer Puffer → `Del_Projekt_Waermeerzeuger(1030)` + Add.
5. **Weg C** = Weg B wiederholt (Idempotenz).
6. Vergleich je `(ID_Type, Bezeichner, n-tes Vorkommen)` über alle 14 Spalten, 29 belegte Fachwerte auf 4 Anlagen.

| Lauf | App-Stand | Ergebnis |
|---|---|---|
| **Reproduktion** (`fs1_vorher2_1030.log`) | HEAD ohne Fix | **8 FAIL / 33 PASS.** Weg A: beide BHKW-Zeilen (11332/11333 → 14822/14823) verlieren alle 12 Fachwerte (8 KWKG + 3 Steuer + `WQ_TemperaturModus`). Weg B: zusätzlich der Kessel (11334 → 14824): `WQ_Anschlusshoehe`, `WQ_ID_Quellprofil`, `WQ_TemperaturModus`, `Hilfsenergie_Anteil` → NULL. Nur der Puffer (11331, FR-1) behält seine Werte. Weg C ebenso. Rohprüfung per `sqlite3`: 28 von 29 Werten NULL. |
| **Roundtrip** (`fs1_nachher_1030.log`) | HEAD + FS1 | **42 PASS / 0 FAIL.** `Fachspalten()` liefert genau die 14 erwarteten Spalten. Weg A: „24 Fachwert(e) auf 2 Anlagenzeile(n) wiederhergestellt", Weg B: „28 auf 3", Weg C idempotent (keine erfundenen Werte auf fremden Zeilen). Rohprüfung per `sqlite3` mit `typeof()`: Datumswerte als ISO-Text `2025-03-01 00:00:00` (Format des Migrators), REAL/INTEGER/TEXT erhalten, `WQ_ID_Quellprofil` = 1 mit `PRAGMA foreign_key_check` = 0 Verletzungen, `integrity_check` ok. Anlagen-IDs wechseln wie erwartet (11332 → 14822 → 14825 → 14828). |

Senken-Rettung (S1) lief in allen Läufen parallel und unverändert („2 bzw. 3 Senkenzeile(n) wiederhergestellt").

## 4. Nebenbefunde / offene Punkte

| # | Punkt | Ziel |
|---|---|---|
| N-1 | **`KostenProjektPositionenCtrl.AnkerNachziehen` (`:283`) setzt ein Access-`UPDATE … INNER JOIN … SET` ab — SQLite weist es mit `near "INNER": syntax error` ab.** Läuft in *jedem* `Add_WP_Waermeerzeuger` (Ä25), je Kostenkomponente einmal: **7 Datenbankfehler je Speichern** (im Prüfstand als stille Fehler des EngineModus gezählt; ohne EngineModus im ersten Lauf als 5 modale Fehlerdialoge, der Lauf hing). Die Kostenanker werden damit seit dem SQLite-Cutover **nicht** nachgezogen. Chip erstellt. | SQLite-Nachzug (S-Strang), Einzelfix: korreliertes UPDATE `SET ID_AnlageGeraet = (SELECT …)` |
| N-2 | `Z_AnlagePufferVerbund.ID_Anlage` hängt mit Löschweitergabe an `Tab_Energieanlagen` — kein Rettungsblock in `WizardCtrl` (AP9b/S1/FS1 decken Varianten, Senken, Fachspalten). Auf der Produktiv-DB derzeit **0 Verbundzeilen**, daher heute ohne Wirkung. | prüfen, sobald Verbünde gepflegt werden |
| N-3 | Datenbefund: `KWKG_Anlagenart`/`KWKG_Eigenstromfall` = `''` (Leerstring) auf 11 **Nicht-BHKW**-Zeilen (Typ 1/10/12) in 1032, 1035, 1043 — `KwkgAnlagenCtrl.Textwert` schreibt für leer NULL, Herkunft vermutlich Import/Duplizieren. Harmlos (Leser behandeln leer wie NULL), FS1 rettet sie mit. | Datenpflege bei Gelegenheit |
| N-4 | Grenzen (wie AP9b/S1, dokumentiert im Block): Umbenennen einer Anlage im Dialog verliert ihre Fachwerte; `KomponentenUebernahmeCtrl` kopiert Fachspalten nicht in das Zielprojekt (kein Del+Add, eigener Weg). | bei Bedarf |
| N-5 | `KwkgAnlagenCtrl`-Doku nennt „65 Spalten des Rechenkerns" — Zählung überholt (73). Kosmetik. | — |
| N-6 | **Gegenprobe am Programm steht aus:** BHKW-Projekt öffnen → KWKG-Angaben je Anlage pflegen → Erzeugerliste über Karte oder Wizard speichern → KWKG-Dialog neu öffnen: Werte müssen stehen; Konsole zeigt „Fachspalten-Rettung: … wiederhergestellt". | Philipp |

Läufe, Prüfstand und DB-Kopien liegen außerhalb des Repos unter `Q:\` (subst auf das Session-Scratchpad:
`Q:\runner\{Program.cs, Fs1Smoke.cs, fs1runner.csproj}`, `Q:\logs\fs1_{vorher2,nachher}_1030.log`,
`Q:\db\{vorher2,nachher}\Kenndaten.sqlite`, App-Stände `Q:\appbin_{vorher,nachher}`).
