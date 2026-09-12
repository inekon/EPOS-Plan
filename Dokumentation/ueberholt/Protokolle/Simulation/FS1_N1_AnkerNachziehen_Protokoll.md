# FS1 N-1 — Kostenanker-Nachzug unter SQLite: Access-`UPDATE … INNER JOIN` in `AnkerNachziehen` ersetzt

Stand: 03.09.2026 · Branch `ios_migration` · Basis HEAD `835092b` (PA1) · Bezug: Nebenbefund **N-1** des
FS1-Protokolls (`FS1_Fachspalten_Protokoll.md` § 4, Chip vom 02.09.2026). Build x64 Debug out-of-tree
(VS-18-MSBuild, `/p:OutputPath` auf Scratch): 0 Fehler, 39 Bestandswarnungen (NU1510, CS0108, CS0109, WFO1000).

## 1. Befund (am Code verifiziert)

`KostenProjektPositionenCtrl.AnkerNachziehen` (`Controller/KostenProjektPositionenCtrl.cs:254`) leitet den
Geräteanker `Tab_ProjektWerte.ID_AnlageGeraet` aller gültig zugeordneten Positionen eines Projekts aus ihrer
Anlagenzeile neu ab — je Kostenkomponente mit Verweisspalte (7: Wärmepumpe `ID_WP`, Heizkessel `ID_Kessel`,
BHKW `ID_BHKW`, Photovoltaik `ID_PV`, Solarthermie `ID_Solar`, Stromspeicher `ID_SP`, Pufferspeicher
`ID_PUFFER`) ein UPDATE. Bis heute stand dort der Access-Dialekt (`:283`):

    UPDATE Tab_ProjektWerte AS w INNER JOIN Tab_Energieanlagen AS a ON w.ID_Anlage = a.ID
    SET w.ID_AnlageGeraet = a.[<Spalte>] WHERE w.ProjektID = ? AND a.ID_Projekt = ? AND w.KomponentenID = ?

SQLite kennt keinen Verbund im UPDATE-Kopf: `SQLite Error 1: 'near "INNER": syntax error'`.
`DataRepository.ExecuteSQL` fängt die Ausnahme, ruft `FehlerMelden` (Bedienung: **MessageBox**; EngineModus:
stille Sammlung) und liefert `false`; `AnkerNachziehen` schluckt das still. Folge seit dem SQLite-Cutover
(02.09.2026): **sieben modale Dialoge** „Datenbankfehler: SQLite Error 1: 'near "INNER": syntax error'" bei
jedem Aufruf, und der Anker wird nicht nachgezogen.

| Aufrufer | Stelle | Wann |
|---|---|---|
| `WizardCtrl.Add_WP_Waermeerzeuger` (Ä25) | `Controller/WizardCtrl.cs:1630` (HEAD `:1321`) | **jedes** Speichern der Erzeugerliste — zehn Del+Add-Paare in sechs Dateien (Karten, Kontextmenüs, Wizard, Simulationsdetail; Liste in FS1 § 1) |
| `ProjektDuplizierenCtrl` | `:316` | jede Variantenkopie (Ä24 — der eigentliche Anlass der Methode) |
| `ProjektExportImportCtrl` | `:508` | jeder `.wpx`-Import |

Im Speicherweg ist die Methode ein **Sicherheitsnetz**: davor ziehen `WizardCtrl.KostenAnkerUmziehen`
(`:1675`, Gerätetausch alte → neue Gerätekopie) und `ZuordnungReparieren` (`:303`, `ID_Anlage` über den
Anker auf die neue Anlagenzeile) — deshalb bleiben die Anker im Speicherweg auch heute meist richtig, während
die Dialoge trotzdem erscheinen. Im Duplizierer-Fall (Anker zeigt auf das Gerät des QUELLprojekts) ist sie
dagegen die einzige Heilung.

**Weitere Access-Verbund-Schreibbefehle im Repo** (grep `UPDATE`/`DELETE` mit `JOIN` im 3-Zeilen-Fenster,
`UPDATE Tab_x, Tab_y`, `DISTINCTROW`, `FROM (`):

| Stelle | Form | Bewertung |
|---|---|---|
| `Allgemein/Update/SchemaMigration.cs:7474` (`Schritt_46_AnlagenGeraeteanker`) und `:7528` (`Schritt_47_AnkerNachziehen`) | dasselbe JOIN-UPDATE | **eingefrorener Access-Zweig** (`SCHRITTE`): läuft nur in `HebeAltbestand` über `Lauf.Conn` (OleDb); `Ausfuehren()` arbeitet ausschließlich `SCHRITTE_SQLITE` ab. Unverändert, korrekt — die Schrittkörper 1–61 bleiben zeichengetreu (S6). |
| `Allgemein/Bericht/KostenEmissionRechner.cs:320` | `FROM (Tab_Energieanlagen AS e LEFT JOIN Tab_BHKW AS b ON …) LEFT JOIN Tab_Heizkessel AS h ON …` im SELECT | **gültig**: die SQLite-Grammatik erlaubt `( join-clause )` als `table-or-subquery`. Auf der Produktiv-Kopie ausgeführt: 4 Zeilen für Projekt 1030, kein Fehler. Kein Handlungsbedarf. |
| `SchemaMigration.cs:6130`, `Controller/VariantenCtrl.cs:252` | geklammerte Verbünde im SELECT | dieselbe Form, gültig. |

Es gibt damit im Laufzeitcode genau **eine** Stelle mit Verbund im UPDATE-Kopf — diese.

## 2. Fix — `Controller/KostenProjektPositionenCtrl.cs` (`:280–300`)

Korreliertes UPDATE mit derselben Semantik wie der Access-Verbund, je Komponente:

    UPDATE Tab_ProjektWerte SET ID_AnlageGeraet =
      (SELECT a.[<Spalte>] FROM Tab_Energieanlagen AS a
        WHERE a.ID = Tab_ProjektWerte.ID_Anlage AND a.ID_Projekt = <Projekt>)
    WHERE ProjektID = <Projekt> AND KomponentenID = <Komponente>
      AND EXISTS (SELECT 1 FROM Tab_Energieanlagen AS a
                  WHERE a.ID = Tab_ProjektWerte.ID_Anlage AND a.ID_Projekt = <Projekt>)

- **Treffermenge** = Verbundbedingung (`EXISTS`): nur Positionen des Projekts mit einer Anlagenzeile
  DESSELBEN Projekts; Positionen ohne oder mit verwaister Zuordnung bleiben unberührt (wie beim INNER JOIN).
- **Wert** aus genau einer Anlagenzeile (`a.ID` ist Schlüssel) — die Unterabfrage liefert höchstens eine Zeile.
- Ids bleiben **Literale** (Ä21-Befund; die Stelle bleibt frei von `?`, das Verbundfragment steht einmal
  in `anlageDesProjekts`). Namensliste, Reihenfolge, Vorsorge-Prüfung und Best-effort-Fehlerverhalten unverändert.
- Umfang: +18/−5 Zeilen (SQL + Kommentar). Encoding gemessen (perl `:raw`) vor/nach dem Patch: UTF-8 **mit**
  BOM, reines CRLF (546 → 559 Zeilen), Nicht-ASCII-Bytes 231 = 231 mit identischem Multiset (neuer Kommentar
  umlautfrei). Patch byte-erhaltend über `patch_anker.pl` (genau ein Treffer, sonst Abbruch).

**Vorabtest der Anweisung** (Python/sqlite3 3.50.4, Wegwerfkopie der Produktiv-DB, Projekt 1030): Access-Form →
`near "INNER": syntax error`. Neue Form: zwei absichtlich verstellte Anker der Komponente 7 (`1`, `NULL`)
korrigiert (2 Zeilen), ein verstellter Kessel-Anker beim Komponente-7-Lauf **nicht** angefasst, eine Position
des Fremdprojekts 1031 mit `ID_Anlage` auf eine 1030-Anlage **nicht** angefasst (0 Zeilen).

## 3. Verifikation (headless, Kopie der Produktiv-DB)

**Methode.** Snapshot der Produktiv-DB per SQLite-Backup-API (`C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`,
nur lesend `mode=ro`; MD5 `47bcefaca0f18d2180ba37786c6cb6b3` = derselbe Stand wie PA0/PA1/FS1; Zeitstempel der
Produktiv-Datei 02.09.2026 22:07:36 und Projekt 1030 dort — 4 Anlagen, 5 Positionen — nach allen Läufen
unverändert). Zwei App-Stände aus `git archive HEAD` (`835092b`) out-of-tree gebaut: **vorher** = HEAD,
**nachher** = HEAD + dieser Patch, sonst byte-gleich. (Der Arbeitsbaum trug während des Laufs halbfertige
Paket-B-Änderungen einer Parallelsession — Migrationsschritt 63, `CS0103 Schritt_63_PvModellwahl` —, die nicht
übersetzbar waren; deshalb reiner HEAD als Basis. Der Befund Ä25 ist in HEAD enthalten.)
Prüfstand `ankerrunner` (Muster fs1runner: Resolver auf `appbin`, `e_sqlite3.dll` aus `runtimes\win-x64\native`,
`DataRepository.PfadUeberschreibung` auf die Kopie; `WizardCtrl`/`WErzeugerCtrl`/`KostenProjektPositionenCtrl`
sind `internal` → Reflection; Speichern im `EngineModus`; DialogWaechter als Netz).

**Zwei unabhängige Fehlermessungen je Phase:** (1) `DataRepository.StilleFehlerAbholen()` nach dem Speichern,
(2) Konsolenzeilen „Datenbankfehler im Simulationslauf (ohne Dialog)", die `FehlerMelden` im EngineModus immer
schreibt. Grund für (2): `WirtschaftlichkeitCtrl.SpalteVorhanden` (`:4982`) **leert die Sammlung selbst**
(Spaltenprobe im eigenen EngineModus, einmalig gecacht) — beim ersten Speichern (Weg A, über die
H3-Pflichtpositionen erreicht) verschwanden die sieben Meldungen aus der Sammlung, die Konsole zählte sie weiter.

**Ablauf** je Lauf, Projekt **1030** „Referenz BHKW-Kaskade (Regressionstest)" (2 BHKW, 1 Kessel, 1 Puffer;
5 Positionen mit Anlagenbezug, alle Anker im Bestand korrekt):

1. Migration der Kopie 61 → 62.
2. **Weg A** = BHKW-Karte/-Kontextmenü: `ReadAllFilter("ID_Projekt=1030 and ID_Type=11")` → `Del_Projekt_Waermeerzeuger(1030, 11)` + `Add_WP_Waermeerzeuger`.
3. **Weg B** = Wizard bearbeiten: alle Typen außer Puffer → `Del_Projekt_Waermeerzeuger(1030)` + Add.
4. **Weg C** = Weg B wiederholt (Idempotenz).
5. **Szenario D** = Duplizierer-Fall (Ä24): Anker zweier Positionen verstellt (`1018148 → 1`, `1018330 → NULL`), dann `AnkerNachziehen(1030)` direkt — die Wirkung der Methode selbst, ohne die heilenden Nachbarn des Speicherwegs.

Prüfung je Phase über alle Positionen mit Anlagenbezug: die Anlagenzeile existiert im Projekt, bei neu
geschriebenen Typen ist es eine **neue** Zeile, und `ID_AnlageGeraet` = Gerätekopie **genau dieser** Zeile
(Verweisspalte je Komponente); keine Position verliert ihre Zuordnung.

| Lauf | Fehler je Phase (Sammlung / Konsole) | Anker | Prüfungen |
|---|---|---|---|
| **vorher** (`anker_vorher_1030.log`) | A 0 / **7**, B **7 / 7**, C **7 / 7**, D **7 / 7** — 28 Meldungen, alle `near "INNER"` | A–C 17/17 richtig (die Nachbarn `KostenAnkerUmziehen` + `ZuordnungReparieren` heilen; das Netz reißt still). **D: Anker bleiben `1` bzw. `NULL`** — 2 FAIL | 84 PASS / **6 FAIL** |
| **nachher** (`anker_nachher_1030.log`) | **0 / 0** in allen vier Phasen | A–C 17/17 richtig; **D: `1 → 1018148`, `NULL → 1018330` korrigiert**, 17/17 | **90 PASS / 0 FAIL** |

Anlagen-IDs wandern wie erwartet (11332/11333/11334 → A 14822/14823 → B 14824–14826 → C 14827–14829); die
Gerätekopien bleiben (`AnlagenEindeutigkeit` nimmt die vorhandene Kopie wieder auf: 1018148, 1018149, 1018330,
Puffer 1054170), und die Anker folgen der neuen Anlagenzeile. Beispiel Position 101600094 (BHKW, Investition):
Bestand `ID_Anlage 11332 / ID_AnlageGeraet 1018148` → nach Weg C `ID_Anlage 14828 / ID_AnlageGeraet 1018148`
= `Tab_Energieanlagen(14828).ID_BHKW`. H3 legt je Anlagenzeile Pflichtpositionen an (5 → 17 Positionen), alle
korrekt verankert. Rohprüfung mit `sqlite3` auf der Nachher-Kopie: `integrity_check` ok, `foreign_key_check` 0,
`typeof(ID_AnlageGeraet) = integer`, Drift-Abfrage „Anker ≠ Gerät der Anlagenzeile" = **0** (Vorher-Kopie: 2,
genau Szenario D). DialogWaechter: 0 Dialoge in allen Läufen. Stringprobe der DLLs (UTF-16): der Access-Text
steht nachher nur noch einmal (Schritte 46/47), das korrelierte UPDATE einmal mehr.

## 4. Nebenbefunde / offene Punkte

| # | Punkt | Ziel |
|---|---|---|
| 1 | `ProjektDuplizierenCtrl:316` und `ProjektExportImportCtrl:508` riefen dieselbe Methode — seit dem Cutover also auch **7 Dialoge je Variantenkopie / je Import**, und im Duplizierer-Fall blieb der Anker auf dem Quellgerät (Szenario D). Mit dem Fix erledigt (eine Methode), nicht gesondert gemessen. | Gegenprobe beim nächsten Duplizieren |
| 2 | Prüfstand-Lehre: `WirtschaftlichkeitCtrl.SpalteVorhanden` leert die EngineModus-Sammlung; stille Fehler headless zusätzlich über die Konsolenzeilen von `FehlerMelden` zählen (so im `ankerrunner`). Anwärter für eine Nacharbeit: eigene Sammlung je Probe statt `StilleFehlerAbholen()` auf der geteilten Liste. | bei Gelegenheit |
| 3 | **Gegenprobe am Programm steht aus:** BHKW-Projekt öffnen → Erzeugerliste über Karte oder Wizard speichern → **keine** Fehlerdialoge mehr; Kosten-Seite zeigt die Positionen an ihren Anlagen. | Philipp |
| 4 | Lage im Repo: Datei uncommitted im Arbeitsbaum `ios_migration` (Sync-Automatik sammelt ein); eine Parallelsession (Paket B) hat HEAD inzwischen auf `f1d16e3` bewegt — kein Berührungspunkt mit dieser Datei. | — |

Läufe, Prüfstand und DB-Kopien liegen außerhalb des Repos unter `T:\` (subst auf das Session-Scratchpad):
`T:\runner\{Program.cs, AnkerSmoke.cs, ankerrunner.csproj}`, `T:\logs\anker_{vorher,nachher}_1030.log`,
`T:\logs\build_{vorher,nachher}.log`, `T:\db\{basis,vorher,nachher}\Kenndaten.sqlite`,
App-Stände `T:\appbin_{vorher,nachher}`, Patchskript `T:\patch_anker.pl`.
