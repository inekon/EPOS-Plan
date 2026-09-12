# iF30 — Lesemodus streng durchsetzen: Protokoll

> Anwenderentscheid vom **04.09.2026** (Entscheidungsregister `iF30` im
> [`Umsetzungskonzept_iOS_EPOS-Plan.md`](../../../Umsetzungskonzept_iOS_EPOS-Plan.md)):
> „**streng** — alle Schreibwege und der Simulationslauf werden über die eine Schreibnaht
> im Kern gesperrt, Ansehen und Berichte bleiben frei, Banner in der `AppWurzel`,
> Warnstufen 30/14/7 Tage vor Ablauf; Ausnahmen Erststart-Migration, Lizenzaktivierung,
> Einstellungen; eigene kleine Welle nach der Windows-Abnahme."
>
> Umgesetzt am **06.09.2026**, nach der Windows-Abnahme vom 05.09.2026.
> Fachgrundlage: [`EPOS-Plan_Konzept_Lizenzierung.md`](../../../EPOS-Plan_Konzept_Lizenzierung.md) § 6.

## 0 — Was die Welle getan hat

`LizenzManager.DarfSchreiben()` gab es seit iU5‑U1; **gefragt hat sie bis heute genau ein
Aufrufer** — `KiAusfuehrer.Schreibrecht` (Befund W15c‑B7). Weder die Simulation noch die
Projektanlage noch irgendein Speicherweg hat den Lizenzzustand gekannt. Der Lesemodus stand
im Konzept, war **sichtbar** (sechs Zustände, drei Stufen, Detailzeile aus W15c) und
**prüfbar** (19 Kern-Fälle `LizenzZustandTests`), aber **nicht durchgesetzt**.

Diese Welle setzt ihn durch — an **einer** Stelle, mit **fünf benannten Ausnahmen** und
**vier Werkzeug-Freigaben**, und sie macht ihn sichtbar: ein Banner in der gemeinsamen
Wurzel beider Plattformen.

| | |
|---|---|
| Neue Dateien | `EPOS.Kern/Allgemein/Lizenz/Schreibnaht.cs`, `…/LizenzLage.cs`, drei Testklassen im Kern, eine in `EPOS.UI.Tests`, dieses Protokoll |
| Geänderte Dateien | 17 |
| Neue Ressourcenschlüssel | 6 (de/en) |
| Kern-Tests | 1 230 → **1 302** (+72) |
| bunit-Tests | 2 679 → **2 691** (+12); nach dem Abschluss-Merge 2 695 |
| Referenzlauf 1030/1007/1017 | **byte-gleich** zu `Referenzlaeufe/2026-09-05_R2_Zeitbasis` |

**Nachtrag vom 06.09.2026 (Anwenderentscheide iF30‑O‑1…5).** Die fünf offenen Punkte aus § 8
sind entschieden; jeder trägt dort seine Zeile „Entschieden 06.09.2026". Vier davon bestätigen
den gebauten Stand, einer ändert ihn: **iF30‑O‑2 — die drei Warnstufen erscheinen seither
einmal je Kalendertag statt bei jedem Programmstart** (§ 5.1, neue Datei
`EPOS.Kern/Allgemein/Lizenz/LizenzWarnungMerker.cs`, Abnahmepunkt A‑iF30‑11). Der
Lesemodus-Zustand bleibt davon unberührt.

---

## 1 — Die Schreibnaht: Fundstelle und Beleg, dass es nur eine gibt

### 1.1 Die Vermessung

Die Frage der Welle lautete: Gibt es EINEN Punkt, durch den jeder schreibende
Datenbankzugriff läuft? Der Bestand sieht auf den ersten Blick nach neun Wegen aus:

| Weg | Datei | schreibt über |
|---|---|---|
| die vier schreibenden Zugriffsmethoden | `EPOS.Kern/Allgemein/SqliteDatenzugriff.cs` | `ExecuteSQL`, `ExecuteNonQuery`, `ExecuteInsertAndGetId`, `ExecuteScalar` |
| der Datenbankvorgang | `EPOS.Kern/Allgemein/DbVorgang.cs` | `Ausfuehren`, `EinfuegenUndId`, `Skalar` |
| der Zeilenzeiger | `EPOS.Kern/Allgemein/RecordSet.cs:143` | `Insert` |
| der stille Weg | `EPOS.Kern/Allgemein/Simulation/StilleDb.cs:113` | `NonQuery` |
| die Wärmequelle | `EPOS.Kern/Allgemein/Simulation/WaermequelleClass.cs:426/452/477` | eigene Verbindung |
| der Pufferspeicher | `EPOS.Kern/Controller/PufferSpCtrl.cs:1373/1478/1679/1698/1753` | eigene Verbindung |
| die Geräte-Waisen | `WindowsFormsApplication1/Allgemein/Update/GeraeteWaisen.cs:401/510` | eigene Verbindung |
| die Schemamigration (SQLite-Zweig) | `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs:4551` | `DataRepository.ExecuteSQL` |
| die Schemamigration (Access-Zweig) | ebenda, `OleDbCommand` auf `l.Conn` | **eigene OleDb-Verbindung** |

**Acht der neun laufen durch DIESELBE Methode.** Jeder SQLite-Befehl des Kerns wird von
`SqliteDatenzugriff.ErzeugeKommando(verbindung, transaktion, sql, parameter)` gebaut — auch
die sechs Eigenverbindungen, denn sie holen ihr Kommando ausdrücklich dort
(`DataRepository.ErzeugeKommando` ist die Weiterleitung dorthin). Belegt mit

```bash
grep -rn "ExecuteNonQuery\|ExecuteScalar()\|SqliteCommand\|OeffneVerbindung\|ErzeugeKommando" \
     --include=*.cs . | grep -v /obj/ | grep -v /bin/
```

**Genau zwei Kommandos entstehen daneben, und beide schreiben nicht:**

* die PRAGMA-Zeile des Verbindungsaufbaus (`SqliteDatenzugriff.OeffneVerbindung`,
  `PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;`),
* `SELECT last_insert_rowid()` in `ExecuteInsertAndGetId` und `DbVorgang.EinfuegenUndId`.

Der **neunte** Weg ist der eingefrorene Access-Zweig der Erststart-Migration. Er baut seine
`OleDbCommand` selbst und kommt an der Naht vorbei — das ist unschädlich, weil er ohnehin
zu den Ausnahmen gehört (§ 2) und weil er auf die `.accdb` schreibt, nicht auf die
Arbeitsdatenbank.

**Es gibt also die eine Naht.** Sie musste nicht gebaut werden; sie war da.

### 1.2 Was dort steht

`EPOS.Kern/Allgemein/SqliteDatenzugriff.cs`, erste Zeile von `ErzeugeKommando`:

```csharp
Schreibnaht.Pruefe(sql);
```

`EPOS.Kern/Allgemein/Lizenz/Schreibnaht.cs` (neu, 330 Z.):

| Glied | Zweck |
|---|---|
| `Schreibrecht` (`Func<bool>`) | die Frage. Vorgabe `LizenzManager.DarfSchreiben` — dieselbe Bauart wie `KiAusfuehrer.Schreibrecht` (W15c‑B7). Das Setzen verwirft den Zwischenspeicher |
| `IstSchreibend(sql)` | **die Liste der LESER, nicht der Schreiber**: `SELECT`, `PRAGMA`, `EXPLAIN`, `VALUES` kommen durch, alles andere gilt als schreibend. Wer eine Schreibform vergäße, hätte ein Loch; eine zu viel abgewiesene Leseform fällt sofort auf |
| `ErstesWort(sql)` | das erste Schlüsselwort über Leerraum, `--`-Zeilenkommentare, `/* */`-Blöcke und führende Klammern hinweg |
| `Freigabe(grund)` | ein benannter `using`-Bereich für die Ausnahmen des Programms; schachtelbar, `AsyncLocal`, wird auch von einer Ausnahme geschlossen |
| `WerkzeugFreigabe(grund)` / `…Zuruecknehmen()` | der Weg der Werkzeuge und Prüfstände, prozessweit, mit lesbarem Grund |
| `LesemodusException` | eigene Ausnahme mit fertigem Anwendertext (`LIZ_LESEMODUS_SPERRE`) und der gekürzten Anweisung DANEBEN — im Meldungstext steht kein SQL |

**Der Zwischenspeicher.** `LizenzManager.DarfSchreiben()` liest Ablage und Zeitanker; die
Frage je Anweisung zu stellen hieße bei einem Simulationslauf tausendfacher Datei- und
Einstellungszugriff. Die Antwort gilt deshalb **5 Sekunden** und wird verworfen, sobald
jemand `Schreibrecht` setzt oder `Neubewerten()` ruft — und das tut `LizenzManager` bei
jedem Token-Wechsel (`TokenSpeichern`, `TokenLoeschen`), damit eine frisch aktivierte Lizenz
sofort trägt.

**Zwei Zusagen, die man leicht verliert** (Fälle 14/15 in `SchreibnahtTests`): Wirft die
Schreibrechtsfrage selbst, wird **nicht** gesperrt, und ohne Frage (`null`) ebenso wenig.
„Nie Daten sperren" (Konzept § 9) — dieselbe Linie wie `ZustimmungCtrl` (`catch → true`).

### 1.3 Was der Anwender sieht statt eines SQLite-Fehlers

Die vier schreibenden Zugriffsmethoden fangen `LesemodusException` **eigens** ab
(`SqliteDatenzugriff.LesemodusMelden`) und melden **einen Satz** über denselben Weg wie
jeden Datenbankfehler — also als Dialog in der Bedienung und als Protokollzeile im
Engine-Modus. Die Anweisung geht ausschließlich auf die Konsole. Die Rückgabewerte im
Fehlerfall sind unverändert: `false`, `-1`, `0`, `null`.

`DbVorgang` fängt wie eh und je **nichts** ab — dort kommt die Ausnahme beim Aufrufer an,
und ohne `Commit` wird zurückgerollt.

---

## 2 — Die fünf Ausnahmen, jede mit Grund an ihrer Stelle

**Nie über den SQL-Text.** Eine Ausnahme, die an einer Zeichenkette hinge, ließe sich von
jeder gleichlautenden Anweisung mitnehmen. Der Aufrufer sagt statt dessen, **warum** er im
Lesemodus schreiben darf, und das ist an genau seiner Zeile zu lesen.

| # | Fundstelle | Grund | Warum |
|---|---|---|---|
| A‑1 | `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`, `Ausfuehren` | `GRUND_MIGRATION` | Sie läuft in `Program.Main` **vor jedem Fenster**. Ein Anwender mit abgelaufener Lizenz dürfte seine Datenbank sonst nicht einmal mehr öffnen |
| A‑2 | `WindowsFormsApplication1/Allgemein/Update/ErststartMigration.cs`, `Fuehredurch` | `GRUND_MIGRATION` | Die Hebung `.accdb` → SQLite läuft einmal je Bestand. Ohne sie gäbe es keine Datenbank, die man ansehen könnte |
| A‑3 | `EPOS.Kern/Controller/ApplikationCtrl.SetSchemaVersion` | `GRUND_MIGRATION` | Der Schemamarker gehört zur Migration. Ohne ihn liefe sie bei jedem Start erneut an |
| A‑4 | `EPOS.Kern/Controller/ApplikationCtrl.Update` | `GRUND_PROGRAMMZUSTAND` | `Tab_Applikation` ist die Einzelzeilen-Statustabelle und trägt „welches Projekt ist zuletzt geöffnet" — **kein Arbeitsergebnis**. Ohne diese Ausnahme ließe sich im Lesemodus kein Projekt mehr ÖFFNEN, und genau das erlaubt § 6 ausdrücklich |
| A‑5 | `EPOS.iOS/Datenbankbereitstellung.SicherungAnlegen` | `GRUND_SICHERUNG` | `VACUUM INTO` fasst den Bestand nicht an, es schreibt eine **zweite Datei daneben**. Eine Sicherung ist ein Export, und Exportieren bleibt frei |

### Die zwei Ausnahmen des Entscheids, die keine Zeile brauchen

Der Entscheid nennt drei Ausnahmen. Zwei davon sind an der Datenbank **gegenstandslos** —
das ist ein Befund und keine Auslassung:

* **Lizenzaktivierung.** Der ganze Lizenzweg läuft über `Dienste.Lizenzablage` (DPAPI-Datei
  bzw. Schlüsselbund) und `Dienste.Einstellungen` (Registry bzw. `Preferences`). Ein Grep
  über `Tab_Lizenz` im ganzen Bestand liefert **0 Treffer**; die Datenbank kennt keine
  Lizenztabelle. Die Aktivierung berührt die Naht nicht.
* **Einstellungen.** `EPOS.Kern/Controller/EinstellungenCtrl` schreibt ausschließlich
  `Properties.Settings` und `Dienste.Pfade`. Kein `DataRepository`, kein SQL.

Die Konstanten `Schreibnaht.GRUND_LIZENZ` und `GRUND_EINSTELLUNGEN` stehen trotzdem
bereit — falls je ein Weg dieser Art an die Datenbank ginge, ist der Name schon da.

---

## 3 — Der Simulationslauf fragt VOR dem Start

`EPOS.Kern/Controller/SimulationLaufCtrl.Vorpruefen` meldet den Lesemodus als **ersten**
Grund, vor allen fachlichen Prüfungen; die Frage selbst steht als
`SimulationLaufCtrl.LesemodusGrund()` daneben (Text `SIM_MSG_LESEMODUS`).

**Warum ein zweites Mal fragen, wenn die Naht ohnehin sperrt:** Die Naht wirft dort, wo die
erste schreibende Anweisung steht — bei einem Lauf ist das `ErgebnisCtrl.Save`, also NACH
der ganzen Rechnung. Der Anwender sähe seine Meldung, nachdem er eine Minute gewartet hat.

**Ansehen bleibt frei.** Die Prüfung gehört zum LAUF, nicht zur Ergebnisansicht: Ein
gespeichertes Ergebnis darf im Lesemodus geöffnet, betrachtet, berichtet und exportiert
werden.

### Was die Berichtsausgabe braucht

Geprüft: `WordBerichtGenerator`, `ExcelBerichtGenerator` und `CsvExportClass` schreiben
über `IDateiDienst`/`System.IO` in Dateien und **fassen die Datenbank nur lesend an**. Der
Bericht bleibt im Lesemodus vollständig. Die eine Stelle, an der eine Berichtskette schreibt,
ist der **Nachlauf einer Simulation** (`BerichtsDatenSammler` ruft
`SimulationRunner.SimuliereUndSpeichere`, wenn ein Variantenergebnis fehlt) — dort ist die
Sperre gewollt: Sie erzeugt ein neues Arbeitsergebnis.

---

## 4 — Die Werkzeug-Freigaben: die Falle dieser Welle

Referenzlauf, iOS-Prüfmodus und Testvorrichtung laufen **ohne Lizenz** und **schreiben**
(`SimuliereUndSpeichere` legt je Projekt einen Ergebniskopf an). Ohne eine ausdrückliche
Freigabe wäre der Rechennachweis rot ausgefallen — aus einem Grund, der mit dem Rechenweg
nichts zu tun hat.

| Werkzeug | Datei | Zeile |
|---|---|---|
| Referenzlauf (Linux/CI) | `EPOS.Referenzlauf/Program.cs` | `Main` → `WerkzeugFreigabe()`, Rumpf hinter `KulturSetzen` |
| Referenzlauf-Suite (Windows) | `Referenzlauf/Program.cs` | `Main`, hinter `OberflaechenspracheSetzen()` |
| iOS-Prüfmodus | `EPOS.iOS/Pruefung/Prueflauf.cs` | `Ausfuehren`, hinter `KulturSetzen()` |
| Schemawerkzeug | `Werkzeuge/Testdatenbankschema/Program.cs` | `Main`, hinter `PfadUeberschreibung` |
| Testvorrichtung | `EPOS.Kern.Tests/TestDatenbank.cs` | Konstruktor; `Dispose` stellt das vorherige Schreibrecht zurück |

**`Proben/ChartProben` braucht keine.** Ein Grep über das ganze Projekt findet weder
`DataRepository` noch `StilleDb`, `RecordSet`, `DbParam` oder `Sqlite`: Es zeichnet aus
synthetischen Reihen und fasst keine Datenbank an. Eine Freigabe dort wäre eine Zeile ohne
Wirkung.

**Warum die Testvorrichtung das Schreibrecht setzt und keinen `Freigabe`-Bereich öffnet:**
Ein Bereich gälte für die ganze Klasse und ließe sich von innen nicht aufheben —
`SchreibnahtDatenbankTests` könnte den gesperrten Zustand dann gar nicht herstellen. Mit dem
Feld geht es: Die Vorrichtung hebt die Sperre, der einzelne Fall setzt sie für sich zurück.

---

## 5 — Das Banner in der `AppWurzel`

`EPOS.UI/Seiten/AppWurzel.razor` — die gemeinsame Wurzel von Windows und iOS (Entscheid
E‑1) — zeigt über JEDER Ansicht ein `Warnbanner` in einem eigenen Wirt
`.epos-lizenzbanner`.

**Zwei Fälle, zwei Lebensdauern:**

| Lage | Stufe | Frist | Wie oft | Text |
|---|---|---|---|---|
| Lesemodus (auch „nicht aktiviert", „Uhr manipuliert") | Warnung | **keine** — es bleibt stehen | **jeder Start** | `LIZ_BANNER_LESEMODUS` |
| 30 / 14 Tage vor Ablauf | Hinweis | `HinweisDauer` (Vorgabe 20 s) | einmal je Kalendertag | `LIZ_BANNER_ABLAUF` |
| 7 Tage vor Ablauf … Ablauftag | Warnung | `HinweisDauer` | einmal je Kalendertag | `LIZ_BANNER_ABLAUF`, für 1 Tag und 0 Tage eigene Sätze |
| Kulanz, Nachprüfung fällig | Hinweis | `HinweisDauer` | **jeder Start** | die Statuszeile aus W15c |
| gültig, fern vom Ablauf | — | — | — | kein Banner |

Das **Dauerbanner** ist die Ausnahme, die die Hausregel **W16b‑E‑6** gelten lässt: „nur für
einen Zustand, den der Anwender beheben MUSS und sonst nicht sieht". Der Lesemodus ist
genau das. Die drei Warnstufen sind dagegen der „dezente Hinweis beim Start" aus § 6 — die
Lizenz trägt noch, nichts ist gesperrt, also verfallen sie.

### 5.1 Der Tagesmerker (Anwenderentscheid iF30‑O‑2, 06.09.2026)

Die Spalte „Wie oft" ist der **Nachtrag vom 06.09.2026**: § 6 des Konzepts verspricht für die
drei Stufen einen „dezenten Hinweis beim Start (**einmal täglich**)", gebaut war zunächst
„einmal je Programmstart". Der Anwenderentscheid lautet „einmal täglich reicht".

`EPOS.Kern/Allgemein/Lizenz/LizenzWarnungMerker.cs` (neu) hält in
`Dienste.Einstellungen` unter dem Schlüssel **`LizenzWarnungGezeigt`** einen Vermerk der Form
`yyyy-MM-dd|stufe` — unter Windows in `HKCU\Software\wp-plan` neben `LizenzAnker`,
`LizenzZugestimmt` und `LizenzDatei`, auf iOS in den `Preferences`. `SollZeigen(stufe, heute)`
ist Frage und Vermerk in einem: Es zeigt, wenn der Tag ein anderer ist oder die Stufe
**dringender** wurde (30 → 14 → 7), und schreibt dabei fort; ein fehlender, leerer oder
unlesbarer Wert — und ebenso eine werfende Ablage — zeigt ebenfalls. Der Fehlerfall darf dem
Anwender nichts wegnehmen; dieselbe Linie wie `Schreibnaht.Lizenzantwort`.

**Die Entscheidung liegt im Kern, nicht in der Oberfläche.** `LizenzLage.MitTagesmerker(heute)`
sitzt in `LizenzLage.Ermitteln` hinter dem reinen `Bilden` und setzt das neue Feld
**`LizenzLage.WarnungZeigen`**; die `AppWurzel` fragt nur dieses Feld. `Bilden` bleibt damit
rein — der Merker SCHREIBT. **Lesemodus, Kulanzfenster und fällige Nachprüfung fassen den
Merker gar nicht erst an** (der Lesemodus ist keine Warnstufe, die beiden anderen tragen
Warnstufe 0) und stehen deshalb unverändert bei jedem Start.

Der Vermerk ist **kein Angriffsziel**: Er trägt keinen Lizenzzustand, nur „an welchem Tag
welche Stufe schon zu sehen war". Wer ihn löscht oder verstellt, bekommt den Hinweis
häufiger zu sehen, nie seltener.

**Die Komponente kennt den Lizenzkern nicht** (Regel S‑2 aus W15c: `LizenzManager.Pruefe()`
liest auf iOS den Schlüsselbund SYNCHRON, und eine Komponente ruft immer vom Zeichenfaden).
Sie bekommt ein fertiges `WindowsFormsApplication1.LizenzLage`:

* **Windows:** `HauptfensterHuelle.Gaben()["Lizenzlage"] = LizenzLage.Ermitteln()` →
  `Hauptfenster.Lizenzlage` → `AppWurzel.Lizenzlage`.
* **iOS:** `IosProjektQuelle.Lizenzlage()` über `IProjektQuelle.Lizenzlage()` (mit
  Standardumsetzung `null`, damit keine vorhandene Quelle bricht).
* Der Parameter schlägt die Quelle.

`EPOS.Kern/Allgemein/Lizenz/LizenzLage.cs` trennt wie W15c: `Bilden(status, token, heute)`
ist **rein** und prüfbar, `Ermitteln()` ist die Fassade, die an die Ablage geht.
`LizenzManager` liefert dafür `RestTage` und `Warnstufe` samt den Konstanten
`WARNSTUFE_1/2/3`.

**Ein Schreibversuch im Lesemodus** zeigt über `DataRepository.FehlerMelden` →
`Dienste.Dialog` den Satz `LIZ_LESEMODUS_SPERRE` — einen Satz, keinen Stapel, kein SQL.

### Die sechs Texte

| Schlüssel | Wo |
|---|---|
| `LIZ_LESEMODUS_SPERRE` | die Meldung am abgewiesenen Schreibversuch |
| `LIZ_BANNER_LESEMODUS` | das Dauerbanner |
| `LIZ_BANNER_ABLAUF` | „…in {0} Tagen ab ({1})" |
| `LIZ_BANNER_ABLAUF_EIN` | „…morgen ab ({0})" |
| `LIZ_BANNER_ABLAUF_HEUTE` | „…heute ab ({0})" |
| `SIM_MSG_LESEMODUS` | die Absage des Simulationslaufs |

Alle sechs in `Resource.resx` **und** `Resource.en-US.resx`; die Designer-Datei ist mit
`python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben` erzeugt, nicht von Hand
ergänzt.

---

## 6 — Nachweise

| Nachweis | Ergebnis |
|---|---|
| `EPOS.Kern.Tests` (serialisiert, `LANG=de_DE.UTF-8`) | **1 302** grün (vorher 1 230) |
| `EPOS.UI.Tests` | **2 695** grün (vorher 2 679; +12 aus dieser Welle, +4 aus dem Abschluss-Merge) |
| `EPOS.Referenzlauf` 1030/1007/1017 gegen `2026-09-05_R2_Zeitbasis` | 3 × PASS; `diff -rq` ohne `protokoll.txt` **byte-gleich** |
| `Proben/ChartProben` | 40 Bilder, 0 Verstöße, alle grün (die Welle fasst den Renderer nicht an) |
| `Werkzeuge/SqlDialektPruefer` | 1 212 SQL-Texte, **0 Fundstellen** |
| iU5-Wächter (`Program.*`) | leer |
| Plattform-Wächter (WinForms/Drawing/Registry/OleDb im Kern) | leer |
| `dotnet build WindowsFormsApplication1 -c Release -p:Platform=x64` | 0 Fehler |
| `dotnet build Referenzlauf -c Release -p:Platform=x64` | 0 Fehler |

### Die neuen Proben

| Klasse | Fälle | Was sie sichert |
|---|---|---|
| `EPOS.Kern.Tests/SchreibnahtTests` | 15 (mit `Theory`-Zeilen 39) | die REGEL ohne Datenbank: was als schreibend gilt, wann die Naht wirft, Freigaben samt Schachtelung und Ausnahmefall, die Werkzeug-Freigabe, die zwei „nie sperren"-Zusagen |
| `EPOS.Kern.Tests/SchreibnahtDatenbankTests` | 7 | dass die Naht in der Zugriffsschicht wirklich SITZT — an einer Arbeitskopie, über alle vier Fassadenmethoden, `DbVorgang`, `StilleDb`, `RecordSet`, die benannte Freigabe, den Programmzustand und die Laufsperre |
| `EPOS.Kern.Tests/LizenzWarnstufenTests` | 10 (mit `Theory`-Zeilen 26) | die drei scharfen Ränder 31/30, 15/14, 8/7, der Ablauftag selbst, die eigenen Sätze für 1 und 0 Tage, Kulanz und Nachprüfung, beide Sprachen |
| `EPOS.UI.Tests/Seiten/LizenzbannerTests` | 10 (mit `Theory`-Zeilen 12) | das Banner: ohne Lage keines, Warnfarbe und Dauerhaftigkeit im Lesemodus, die Stufen als verfallender Hinweis, die einstellbare Frist, der iOS-Weg, der Vorrang des Parameters, beide Sprachen |

### Nachtrag iF30‑O‑2 (06.09.2026)

| Klasse | Fälle | Was sie sichert |
|---|---|---|
| `EPOS.Kern.Tests/LizenzWarnungMerkerTests` | 13 (mit `Theory`-Zeilen **24**) | den Tagesmerker: Stufe 0 zeigt nie und merkt nichts, zehn unlesbare Vermerke zeigen, der zweite Start am selben Tag zeigt nicht, der nächste Tag zeigt wieder, 7 nach 30 zeigt und 30 nach 7 nicht, die Uhrzeit ist gleichgültig, eine werfende Ablage zeigt — dazu die Naht zum Lagebild: Lesemodus, Kulanz und Nachprüfung bleiben bei **jedem** Start sichtbar und fassen den Merker nicht an |
| `EPOS.UI.Tests/Seiten/LizenzbannerTests` (erweitert) | +2 (mit `Theory`-Zeilen **+4**) | „heute schon gezeigt → kein Banner" für alle drei Stufen, und das Lesemodus-Banner über drei Programmstarts hinweg |

Die Klasse im Kern trägt `[Collection("Testdatenbank")]` — **Hausregel seit dem 06.09.2026:
Jede Testklasse in `EPOS.Kern.Tests`, die ein `Dienste.*` tauscht, gehört in diese Sammlung.**
`Dienste.Einstellungen` ist prozessweiter Zustand, und xunit fährt Testklassen sonst
nebeneinander.

---

## 7 — Abnahmepunkte für den Anwender (Windows)

**A‑iF30‑1 — mit gültiger Lizenz ändert sich nichts.** Programm starten: **kein Banner**.
Ein Projekt öffnen, einen Katalogsatz ändern, speichern — geht wie bisher. Eine Simulation
rechnen und das Ergebnis speichern — geht wie bisher.

**A‑iF30‑2 — Warnstufe.** Mit einer Lizenz, die in 30, 14 oder 7 Tagen abläuft: beim **ersten
Start des Tages** ein Banner mit der Zahl der Tage **und dem Datum**; bei 30 und 14 Tagen
leise (Hinweis), bei 7 Tagen in Warnfarbe. Es verschwindet nach rund 20 Sekunden von selbst.
Volle Funktion. Dass es beim zweiten Start desselben Tages ausbleibt, prüft A‑iF30‑11.

**A‑iF30‑3 — Lesemodus, das Banner.** Ohne Lizenz oder mit abgelaufener Lizenz nach dem
Kulanzfenster: ein **dauerhaftes** Banner in Warnfarbe über der Startseite und über jeder
weiteren Ansicht, mit dem Grund und dem Weg „Hilfe → Lizenzverwaltung".

**A‑iF30‑4 — Lesemodus, was noch geht.** Projekt öffnen, Reiter wechseln, gespeicherte
Ergebnisse ansehen, Diagramme zoomen, Bericht erzeugen (Word/Excel), CSV exportieren,
Projekt exportieren. **Alles muss gehen.**

**A‑iF30‑5 — Lesemodus, was nicht mehr geht.** Einen Katalogsatz ändern und speichern: eine
verständliche Meldung („Lesemodus: Die Lizenz erlaubt derzeit keine Änderungen…"), **kein**
Stapel, **kein** SQLite-Fehlertext, und der Satz ist danach unverändert.

**A‑iF30‑6 — Lesemodus, die Simulation.** „Simulation Konfiguration…" öffnen und rechnen
lassen: Die Absage kommt **sofort** („Im Lesemodus lässt sich keine Simulation rechnen…"),
nicht erst nach der Rechnung.

**A‑iF30‑7 — Lesemodus und das zuletzt geöffnete Projekt.** Im Lesemodus ein Projekt öffnen,
Programm schließen, neu starten: Das Projekt steht wieder als „zuletzt geöffnet" da
(Ausnahme A‑4).

**A‑iF30‑8 — Aktivierung wirkt sofort.** Im Lesemodus über „Hilfe → Lizenzverwaltung" eine
gültige Lizenz aktivieren, **ohne** das Programm neu zu starten: Der nächste Speicherversuch
muss durchgehen (der Zwischenspeicher der Naht wird beim Token-Wechsel verworfen). Das
Banner selbst wird erst beim nächsten Start neu gebildet — siehe iF30‑O‑3.

**A‑iF30‑9 — englische Oberfläche.** Sprache auf Englisch umstellen und A‑iF30‑3 und
A‑iF30‑5 wiederholen: Banner und Meldung müssen englisch sein.

**A‑iF30‑10 — Erststart mit abgelaufener Lizenz.** Ein Rechner mit `.accdb`-Altbestand und
ohne gültige Lizenz: Die Erststart- und Schemamigration muss durchlaufen (Ausnahmen A‑1 bis
A‑3), danach steht die Anwendung im Lesemodus.

**A‑iF30‑11 — die Warnstufe kommt einmal am Tag** (Anwenderentscheid iF30‑O‑2, 06.09.2026).
Mit einer Lizenz, die in 30, 14 oder 7 Tagen abläuft, drei Proben nacheinander:

1. **Zweiter Start am selben Tag.** Programm starten — Banner wie in A‑iF30‑2. Programm
   schließen und **sofort neu starten**: **kein Banner mehr**. Volle Funktion, sonst nichts
   verändert.
2. **Nächster Tag.** Am Folgetag starten: das Banner steht **wieder** da. (Ohne bis morgen zu
   warten: `HKCU\Software\wp-plan`, Wert **`LizenzWarnungGezeigt`** — er hat die Form
   `2026-09-06|30`. Das Datum um einen Tag zurücksetzen oder den Wert löschen, dann starten.)
3. **Der Lesemodus bleibt.** Ohne gültige Lizenz starten, schließen, wieder starten: Das
   Lesemodus-Banner steht **bei jedem** Start (A‑iF30‑3 gilt unverändert). Dasselbe für das
   Kulanzfenster.

---

## 8 — Offene Punkte

> **Alle fünf sind am 06.09.2026 entschieden.** Der Anwender hat iF30‑O‑2 und iF30‑O‑5 im
> Wortlaut beschieden und die übrigen drei mit „alle anderen offenen Punkte wie Empfehlung,
> bestätigt" geschlossen. Der Abschnitt bleibt als Begründungsspur stehen; jeder Punkt trägt
> seine Entscheidung als letzte Zeile.

**iF30‑O‑1 — Ist `Tab_Applikation` zu Recht eine Ausnahme?** Umgesetzt ist die sichere
Fassung: Das Fortschreiben des zuletzt geöffneten Projekts läuft auch im Lesemodus, weil
§ 6 „Projekte öffnen" ausdrücklich erlaubt und der Anwender sonst nicht einmal mehr
einsteigen könnte. Die Zeile trägt **kein** Arbeitsergebnis, nur Programmzustand. Der
Anwender möge bestätigen, dass das seiner Absicht entspricht.

> **Entschieden 06.09.2026:** bestätigt — `Tab_Applikation` bleibt Ausnahme A‑4. Es ändert
> sich nichts; A‑iF30‑7 bleibt der Abnahmepunkt dazu.

**iF30‑O‑2 — „einmal täglich" oder „einmal je Start"?** Das Konzept sagt für die Warnstufen
„dezenter Hinweis beim Start (einmal täglich)". Umgesetzt ist **einmal je Programmstart**:
Die Lage wird beim Aufbau der Wurzel gebildet, das Banner verfällt nach 20 s. Eine echte
Tagesunterdrückung bräuchte einen gemerkten Tag in `Dienste.Einstellungen`; das wäre eine
zweite Ablage neben dem Zeitanker. In der Praxis wird das Programm einmal am Tag gestartet.

> **Entschieden 06.09.2026:** „einmal täglich reicht" — **umgesetzt in diesem Commit**. Der
> neue `LizenzWarnungMerker` hält in `Dienste.Einstellungen` unter `LizenzWarnungGezeigt`
> einen Vermerk `yyyy-MM-dd|stufe` und zeigt eine Stufe nur, wenn der Tag ein anderer ist
> oder die Dringlichkeit gestiegen ist (30 → 14 → 7); ein unlesbarer Wert und eine werfende
> Ablage zeigen ebenfalls. Entschieden wird das im Kern
> (`LizenzLage.MitTagesmerker` → `LizenzLage.WarnungZeigen`), die `AppWurzel` fragt nur das
> Feld — **Lesemodus, Kulanz und Nachprüfung fassen den Merker nicht an und bleiben bei
> jedem Start sichtbar**. Einzelheiten in § 5.1, Abnahme in A‑iF30‑11.

**iF30‑O‑3 — Soll das Banner mitlaufen?** Es entsteht **einmal** beim Aufbau der Wurzel. Wer
im laufenden Programm aktiviert, darf sofort wieder schreiben (A‑iF30‑8), sieht das Banner
aber bis zum nächsten Start. Ein Nachziehen ginge über `SeitenZustand`, kostet aber je
Auffrischung einen synchronen Zugriff auf DPAPI bzw. Schlüsselbund.

> **Entschieden 06.09.2026:** bestätigt — das Banner bleibt **bis zum nächsten Start**
> stehen. Kein Nachziehen über `SeitenZustand`; der synchrone Zugriff auf DPAPI bzw.
> Schlüsselbund je Auffrischung wäre der höhere Preis. A‑iF30‑8 bleibt unverändert.

**iF30‑O‑4 — `PRAGMA` steht bei den Lesern.** Es trägt Verbindungsschalter und
Schemaauskunft und ändert keine Daten. Ein `PRAGMA journal_mode=WAL` wäre theoretisch ein
Schreibvorgang; im Bestand setzt ihn nur der Migrator auf seiner eigenen Verbindung, also
nie über die Naht.

> **Entschieden 06.09.2026:** bestätigt — `PRAGMA` gilt weiter als Lesen und bleibt in der
> Liste der vier Leseformen (`SELECT`, `PRAGMA`, `EXPLAIN`, `VALUES`). Wer je ein
> schreibendes `PRAGMA` über die Naht schickt, ändert diese Liste, nicht den Aufruf.

**iF30‑O‑5 — Der Access-Zweig der Erststart-Migration kommt an der Naht vorbei.** Er baut
seine `OleDbCommand` selbst. Das ist heute unschädlich (er ist ohnehin Ausnahme A‑2 und
schreibt auf die `.accdb`), sollte aber bekannt bleiben, falls dort je etwas anderes
entsteht.

> **Entschieden 06.09.2026:** geschlossen — „Access-DB nicht mehr relevant". **Kein Rückbau
> beauftragt**: Der Zweig bleibt, wie er ist, und nichts an ihm wird angefasst. Der Befund
> bleibt hier stehen, damit er bekannt ist, falls dort je etwas anderes entsteht.
