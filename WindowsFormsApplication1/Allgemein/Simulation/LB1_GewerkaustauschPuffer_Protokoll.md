# L-B1 — Gewerkaustausch „Pufferspeicher" scheitert an `FK_AnlageSenke_Puffer` (behoben)

Stand: 28.08.2026 · Branch `Pufferspeicher` · Befund L-B1 aus
[`L_Aufraeumen_Protokoll.md`](L_Aufraeumen_Protokoll.md) (Abschnitt 7), dort gemessen und
bewusst nicht mitbehoben — der Eingriff in den transaktionalen Löschweg verdiente eine
eigene Wirkprobe. Sie steht hier.

## Mechanismus

Der Komponenten-Gewerkaustausch (`KomponentenUebernahmeCtrl.Uebernehmen`) ersetzt beim
Gewerk „Pufferspeicher" den Speicherbestand des Ziels vollständig: Verweise lösen → löschen
→ aus der Quelle neu anlegen → Verweise über den **Bezeichner** wiederherstellen. Bis zu
diesem Fix kannten `PufferbezuegeSichern` / `PufferverweiseLoesen` /
`PufferverweiseWiederherstellen` aber nur die **vier `Tab_Energieanlagen`-Spalten**
(`PUFFER_VERWEISE`: `ID_PUFFER`, `WQ_ID_Puffer`, `WS_ID_Puffer`, `WS_ID_Puffer2`).

Seit Migrationsschritt 50 zeigt zusätzlich die SENKENLISTE auf die Speicher:
`Z_AnlageSenke.ID_Puffer` ist über `FK_AnlageSenke_Puffer` **restriktiv** erzwungen
(`SchemaMigration.SQL_FK_SENKE_PUFFER`). Jede Senkenzeile einer Zielanlage, die auf einen
der zu löschenden Zielspeicher zeigt, blockierte damit das
`DELETE FROM Tab_Pufferspeicher … WHERE ID_Projekt = ?` — die Transaktion rollte zurück,
die Übernahme schlug mit dem nackten Datenbankfehler fehl. Der Befund ist mit S1
entstanden (die Beziehung kam mit Schritt 50) und unabhängig von A1-O2.

## Fix (symmetrisch zum Bestand, `Controller/KomponentenUebernahmeCtrl.cs`)

| Baustein | Änderung |
|---|---|
| `SenkenbezuegeSichern(idZiel)` | NEU, vor der Transaktion neben `PufferbezuegeSichern`: alle Senkenzeilen, die auf einen Speicher des Zielprojekts zeigen, als (`Z_AnlageSenke.ID`, Puffer-`Bezeichner`) — der Bezeichner überlebt den Austausch, die ID nicht. Schemaprobe `Z_AnlageSenkeCtrl.SpalteVorhanden()`; vor Schritt 50 bleibt die Liste leer |
| `SenkenverweiseLoesen(conn, trans, idZiel)` | NEU, in Schritt 3 neben `PufferverweiseLoesen`: `UPDATE Z_AnlageSenke SET ID_Puffer = NULL WHERE ID_Puffer IN (SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = ?)` — dieselbe Anweisungsform wie beim Bestand, über `VersucheAusfuehren` (fehlende Tabelle auf Alt-Datenbanken bricht nichts ab) |
| `SenkenverweiseWiederherstellen(…)` | NEU, in Schritt 6 neben `PufferverweiseWiederherstellen`: je gesicherter Zeile `UPDATE … SET ID_Puffer = <neue ID> WHERE ID = ?` über die Abbildung Bezeichner → neue Speicher-ID (`neuePufferNachName`). Was sich nicht auflösen lässt, bleibt leer und wird über `BK_KOMP_HINW_PUFFERVERWEIS` gemeldet — nie geraten |

Dazu der Klassenkopf: Der Sonderfall-Absatz nennt die Senkenliste jetzt als dritte
Verweisquelle.

**Abgrenzung zu `PufferSpCtrl.ReferenzenLoesen`** (endgültiges Löschen eines Speichers:
Rang 1 auf Heizkreis normalisieren, Rang ≥ 2 löschen): Beim AUSTAUSCH ist das falsch — die
Senkenliste gehört den Erzeugeranlagen des Ziels, und die fasst der Austausch nicht an.
Ihre Verweise müssen den Tausch überleben, genau wie die der vier Anlagenspalten. Verliert
ein Verweis seinen Bezeichner (Quelle führt den Namen nicht), bleibt er leer: Die Engine
normalisiert Puffer-Ziele ohne Puffer beim Lesen und meldet es
(`WaermesenkeClass`, Regel N5) — derselbe dokumentierte Weg wie bei den `WS_`-Spalten und
in `SenkenNachziehen`. Hinge eine gesicherte Senkenzeile doch an einer gelöschten Anlage
des Tauschgewerks, nähme die Löschweitergabe von `FK_AnlageSenke_Anlage` sie mit und das
Wiederherstellen träfe still 0 Zeilen.

## Beleg (Wegwerf-Kopie der Produktiv-DB, Schemastand 54)

Kopie über `Referenzlauf.exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb <Ziel>`,
Harness `dev/harness_lb1` (Reflection: `Assembly.LoadFrom` + `Settings.DBPath` umbiegen,
`Uebernehmen(quelle, ziel, "Pufferspeicher")` headless). Szenario natürlich vorhanden:
Projekt 1023 führt Senkenzeilen 29/30 (WP-Anlagen 11203/11204, Rang 1 `PufferHeizung`) auf
seinen Speicher 1018023 „Vitocell 140-E 600 Ltr"; Quellprojekt 1019 führt einen
namensgleichen Speicher.

| Probe | ungefixt (80b4ab2) | gefixt |
|---|---|---|
| 1019 → 1023 („Vitocell 140-E 600 Ltr" beidseitig) | **FEHLGESCHLAGEN**: „Der Datensatz kann nicht gelöscht oder geändert werden, da die Tabelle 'Z_AnlageSenke' in Beziehung stehende Datensätze enthält." — Rollback, Bestand unverändert | **OK.** Senkenzeilen 29/30: `ID_Puffer` 1018023 → 1054200 (neue Projektkopie, über Bezeichner); `WS_ID_Puffer` der WP-Anlagen ebenso; Zahl der Senken-Pufferverweise DB-weit vor wie nach 34 |
| Zweitlauf 1019 → 1023 (Wiederholbarkeit) | — | **OK**, Verweise bleiben aufgelöst, keine Verlustmeldung |
| 1018 → 1008 (keine Namensgleichheit) | — | **OK.** Senkenzeilen 3/4: `ID_Puffer = NULL` (ehrlich geleert); eigener Hinweis „2 Verweis(e) … bleiben leer." getrennt vom Vier-Spalten-Altverhalten („4 Verweis(e) …") |

## Neutralität

Der Fix liegt vollständig außerhalb des Rechenkerns (ein Lese-SELECT vor, zwei UPDATE-Formen
innerhalb der bestehenden Austausch-Transaktion). Referenzlauf der 13 Referenzprojekte
(1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030, 1039–1042) gegen die eingefrorene
Basis `Referenzlaeufe/2026-08-28_P1`: **13/13 PASS, 3 532 029 Werte in Toleranz,
329/329 CSV byte-gleich** (SHA-256). Build x64 Debug: 0 Fehler.

## Grenze

Verweise, deren Bezeichner die Quelle nicht führt, bleiben nach dem Austausch leer (Meldung
über die Hinweise der Übernahme) — wie im Vier-Spalten-Bestand. Eine automatische Zuordnung
auf einen anders benannten Quellspeicher wäre Raterei.
