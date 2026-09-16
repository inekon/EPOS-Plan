# Befund Q — Mehrzonenmodell: vorhandene Muster für Datenmodell, Kataloge und Listendialoge (15.09.2026)

**Protokoll.** Befund Q eines Erkundungs-Agenten (Modell Opus, nur lesend) im Auftrag des Konzepts [`../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md), Sitzung vom 15.09.2026.

Gefragt war: Welche Muster trägt das Repositorium schon für eine Kette
Gebäude → Zonen → Bauteile → Aufbau → Schichten → Baustoff und für deren Pflege? Der Befund
ist rein aus dem Quelltext erhoben; jede Aussage nennt Datei und Zeile. Er trifft keine
Entscheidung — Kapitel 5 ist ein **Vorschlag** zur Vorlage beim Anwender. Das Einzonenmodell
(Konzept Gebäudesimulation, Stufen G0–G2) bleibt davon unberührt; das Mehrzonenmodell baut auf
dem Bauteilkatalog (G3) und dem IFC-Import (G4) auf.

**Das Ergebnis in fünf Sätzen.**

1. Für **Eltern-Kind-Strukturen** gibt es drei erprobte Bauformen im Haus, und die Wahl
   zwischen ihnen ist die eigentliche Entscheidung: Kopf + Wertetabelle (Wärmepumpen-Kennlinie),
   geordnete Kindliste mit Löschen-und-Neuanlegen (`Z_AnlageStrang`) und JSON-Dokument je
   Projekt (`Tab_SpeicherAuslegung`). Für Zonen, Bauteile und Schichten passt die zweite.
2. Für **Stamm/Projektkopie** gibt es einen alten und einen neuen Weg; der neue
   (`WechselrichterSchema.Fachspalten` + `WechselrichterCtrl.CopyFromStamm`) ist erhaltend und
   wartbar, der alte (`GebaeudeStammCtrl.CopyFromStamm`, 55 Spalten von Hand) **macht aus jedem
   NULL eine 0,0** — das kollidiert mit der Regel „NULL = Vorgabe" aus Konzept 6.1.
3. **Neue Tabellen entstehen nicht in `001_grundschema.sql`** (eingefroren), sondern in einer
   eigenen `*Schema`-Klasse im Kern, aus der sich vier Leser bedienen: Migrationsschritt,
   Werkzeug `Testdatenbankschema`, Kopierweg und Nachweis.
4. Für **Katalog, Liste und Detail** steht die Maschinerie vollständig bereit
   (`KatalogRegistry`, `Katalogfilterprofil`, `KatalogBrowserProfil`, Bausteine `Katalogliste`,
   `Katalogfelder`, `Zeilenraster`); ein Baustoffkatalog ist ein Eintrag in diesen Registern,
   kein neuer Dialogtyp.
5. Zwei **Stolperstellen** sind namentlich zu bedienen, sonst fällt es erst im Ergebnis auf:
   die `KINDER`-Karte des Projektduplizierens (`ProjektDuplizierenCtrl.cs:152-160`) und die
   feste Spaltenliste der Sicht `Abfrage_Projektgebaeude` (`sql/schema/002_views.sql:89`).

---

## 1. Datenmodell: wie Eltern-Kind heute gelöst ist

### 1.1 Drei Bauformen, drei Zwecke

| Bauform | Beispiel | Fundstelle | Wofür sie taugt |
|---|---|---|---|
| **A — Kopf + Wertetabelle** | `Tab_WP` + `Tab_Kenndaten` (`ID_WP`), `Tab_Waermebedarf` + `Tab_WaermebedarfDaten` (`ID_Ganglinie`) | `sql/schema/001_grundschema.sql:1310`, `:2757` | viele gleichförmige Wertzeilen zu einem Kopf; im Katalog gespiegelt als `_STAMM`-Paar |
| **B — geordnete Kindliste** | `Z_AnlageStrang` je PV-Anlage, `Z_AnlageSenke` je Erzeuger | `EPOS.Kern/Allgemein/Update/AnlageStrangSchema.cs`, `EPOS.Kern/Controller/AnlageStrangCtrl.cs:63` | wenige, fachlich verschiedene Kinder in fester Reihenfolge, im Dialog als Liste gepflegt |
| **C — Dokument je Projekt** | `Tab_SpeicherAuslegung` (`Daten TEXT`, gzip+Base64-JSON) | `EPOS.Kern/Controller/SpeicherAuslegungCtrl.cs:32-38`, `:57-64` | ein zusammenhängender Eingabestand, der nie einzeln abgefragt wird |

**Bauform B ist die Vorlage für Zonen, Bauteile und Schichten.** Ihr Klassenkopf beschreibt
genau die Fragen, die auch hier anstehen (`EPOS.Kern/Controller/AnlageStrangCtrl.cs:14-53`):

- **Kein zweites `ID_Projekt` am Kind.** `Z_AnlageStrang` führt bewusst keines; der
  Projektbezug läuft über den Verbund zur Elterntabelle — „die Anlage weiss, zu welchem Projekt
  sie gehört, und eine zweite Wahrheit darüber könnte auseinanderlaufen"
  (`AnlageStrangCtrl.cs:20-23`).
- **Zwei Lesewege**: je Eltern (Dialog) und je Projekt über JOIN (Rechenkern, Rettungsweg) —
  `AnlageStrangCtrl.cs:135` und `:164`. Ein Aufruf je Eltern wären bei fünf Zonen fünf
  Rundreisen für dieselbe Auskunft.
- **Schreiben ist Löschen + Neuanlegen je Eltern**, alles in EINER Transaktion
  (`AnlageStrangCtrl.cs:210`, Begründung `:25-31`): „Der Dialog liefert eine LISTE in einer
  bestimmten Reihenfolge; welche Zeile darin die frühere Zeile 3 ist, ist keine sinnvolle
  Frage." Die Ränge werden lückenlos ab 1 neu vergeben.
- **Eine Schemaprobe mit Gedächtnis** (`AnlageStrangCtrl.cs:101`): `COUNT(*)` statt
  Schemaabfrage — eine leere Tabelle liefert 0, eine fehlende einen Fehler. Fehlt die Tabelle,
  „führt keine Anlage Stränge, und alles rechnet wie bisher".
- **Die Falle der Kaskade** (`AnlageStrangCtrl.cs:33-43`): `ID_Anlage` hängt mit
  `ON DELETE CASCADE` an `Tab_Energieanlagen`, und der Speicherweg der Anlage ist Löschen +
  Neuanlegen — ohne Gegenmaßnahme räumte **jedes Speichern** die Kindliste ab. Die Rettung
  steht dort, wo das Löschen steht (`WizardCtrl.StraengeSichern`), nicht im Controller.
  **Für Zonen gilt dasselbe**, weil `Tab_Gebaeude.ID_ProjektGebaeude` ebenfalls kaskadiert
  (`sql/schema/001_grundschema.sql:1187`).

Die DDL von Bauform B ist knapp und vollständig
(`EPOS.Kern/Allgemein/Update/AnlageStrangSchema.cs`, Auszug):

```sql
CREATE TABLE IF NOT EXISTS "Z_AnlageStrang" (
    "ID" INTEGER PRIMARY KEY AUTOINCREMENT,
    "ID_Anlage" INTEGER NOT NULL,
    "Rang" INTEGER NOT NULL,
    "Bezeichner" TEXT CHECK (length("Bezeichner") <= 50),
    "ID_Wechselrichter" INTEGER,
    FOREIGN KEY ("ID_Anlage") REFERENCES "Tab_Energieanlagen" ("ID") ON DELETE CASCADE,
    FOREIGN KEY ("ID_Wechselrichter") REFERENCES "Tab_Wechselrichter" ("ID")
) STRICT
```

Zu lesen sind daran vier Hausregeln: `STRICT`, `AUTOINCREMENT` am Schlüssel, **Kaskade nur zum
Eltern** (der Katalogverweis kaskadiert nicht — ein gelöschter Wechselrichter darf nicht die
Strangzeile mitnehmen) und `CHECK (length(...))` statt einer Typlänge.

### 1.2 Die vier Beispiele des Auftrags, kurz gemessen

| Gewerk | Kopf | Kind / Zuordnung | Besonderheit |
|---|---|---|---|
| **PV** | `Tab_PV` (Modul, Projektkopie) `001:2094`, `Tab_PV_STAMM` `001:2115` | Stränge in `Z_AnlageStrang` je `Tab_Energieanlagen.ID` | Das **Modul** ist Katalogware, der **Strang** Projektware; die Anlage verbindet beide |
| **Solarthermie** | `Tab_Solarkollektoren` `001:2212` / `_STAMM` `001:2231` | Kollektorzahl als Feld `Tab_Energieanlagen.Kollektormodulanzahl` `001:728` | Keine Kindtabelle — die Zahl steht an der Anlage |
| **Speicherflotte** | `Tab_SpeicherAuslegung` (Bauform C) | Projektzeilen im JSON, Stände `@Aktuell` / `@Projektflotte` | `SpeicherAuslegungCtrl.cs:17`, eindeutiger Index über `(ID_Projekt, COALESCE(ID_Energieanlage,0), Bezeichner)` `:37` |
| **Brauchwasser / Prozesswärme** | `Tab_Brauchwasser` `001:222`, `Tab_Prozesswaerme` `001:2010` | **Typprofil** `Tab_Brauchwassertyp` `001:262` (über `ID_Brauchwasser`) **und** Zuordnung `Z_Projekt_Brauchwasser` `001:2842` | Der einzige Katalog mit **katalogintern echter Referenz**: der Kopf zeigt über die TEXTSPALTE `Typ` auf den Namen des Typprofils (`KatalogRegistry.cs:104-107`) |

**Die Zuordnungstabellen `Z_*` tragen immer dasselbe Muster** (`001:2842`, `:2852`, `:2862`,
`:2898`, `:2907`, `:2916`): `ID` autoinkrement, `ID_Projekt` mit Kaskade, ein `ID_<Sache>` mit
Kaskade auf die **Projektkopie** (nie auf `_STAMM`), dazu ein redundanter `Bezeichner` und
gelegentlich eine `Summe`. `Z_ProjektGebaeude` `001:2873` ist der Sonderfall: Es führt **kein**
`ID_Gebaeude` mehr, stattdessen zeigt `Tab_Gebaeude.ID_ProjektGebaeude` zurück auf die
Zuordnung (`KatalogRegistry.cs:94-95`, `GebaeudeBedarfCtrl.cs:139-149`). Wer Zonen anhängt,
hängt sie an `Tab_Gebaeude.ID`, nicht an `Z_ProjektGebaeude.ID`.

### 1.3 Stamm und Projektkopie: der alte und der neue Kopierweg

**Die Kopiersemantik ist die Regel des Hauses** (`KatalogRegistry.cs:86-99`): „Projekte
KOPIEREN Katalogsätze, alle persistierten Verweise zeigen auf die Projektkopie, nie auf die
`_STAMM`-Tabelle. Löschen im Katalog berührt darum keine Projektdaten." Deshalb führen diese
Kataloge ein **leeres** `VerwendungsPruefungen`-Array.

**Der neue Weg** — `WechselrichterCtrl.CopyFromStamm` (`EPOS.Kern/Controller/WechselrichterCtrl.cs:103`):
eine einzige Spaltenliste `WechselrichterSchema.Fachspalten`
(`EPOS.Kern/Allgemein/Update/WechselrichterSchema.cs:287`) baut `INSERT`-Text und Parameter,
und `Spaltenwert(row, spalte)` (`WechselrichterCtrl.cs`, Hilfsmethode) reicht `DBNull`
**unverändert** durch. Die Liste ist zugleich die Quelle der DDL (`WechselrichterSchema.cs:260`
`Anweisungen`) und des Nachweises; ihr Kommentar nennt den Grund: „eine Spalte nur auf einer
Seite ist beim `CopyFromStamm` sofort ein Datenverlust" (`:280-284`).

**Der alte Weg** — `GebaeudeStammCtrl.CopyFromStamm` (`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:439`):
ein von Hand geschriebener `INSERT` über 55 Spalten, dazu `DataRepository.GetMaxID(TABLE_PROJ) + 1`
als Schlüsselvergabe (`:446`). **Jeder Wert wird dabei entnullt**, Muster
`r["Bauweise"] == DBNull.Value ? 0.0 : Convert.ToDouble(...)` (`:459` ff.).

> **Befund Q-1 (hoch).** Konzept 6.1 verlangt für die neuen Gebäudespalten „kein DDL-DEFAULT auf
> Fachwerten; **NULL = Vorgabe**". Der Kopierweg von `Tab_Gebaeude_STAMM` nach `Tab_Gebaeude`
> macht aus NULL eine **0,0** — ein aus dem Katalog übernommenes Gebäude bekäme also
> Rahmenanteil 0, Verschattungsfaktor 0 und Masseanteil 0 statt der Vorgaben 0,3 / 0,9 / 0,3.
> Wer G1 umsetzt, muss `CopyFromStamm` auf das Wechselrichter-Muster umbauen (Spaltenliste +
> `Spaltenwert`), sonst ist die NULL-Semantik allein im Dialog wahr und in der Datenbank
> falsch. Dasselbe gilt für `Insert`/`Overwrite` (`GebaeudeStammCtrl.cs:406`, `:418`), die
> dieselbe Handliste ein zweites und drittes Mal führen.

### 1.4 Wie eine neue Tabelle ins Schema kommt

`sql/schema/001_grundschema.sql` ist **nicht der Ort dafür**. Der Kopf sagt es (`:1-4`):
„Erzeugt von `sql/tools/Erzeuge-Schema.ps1` … **NICHT VON HAND AENDERN** - neu erzeugen. Ab
S4-Beginn eingefroren." Entsprechend fehlen dort alle jüngeren Tabellen —
`Tab_Wechselrichter`, `Z_AnlageStrang`, `Tab_SpeicherAuslegung`, `Tab_Nutzungsdauer` stehen in
keiner Zeile von 001.

**Der Weg ist ADR-001** ([`../ADR-001_Schema-Ausrollung.md`](../ADR-001_Schema-Ausrollung.md),
Option C, `:145`): nummerierte C#-Schritte in
`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` mit Versionsmarker. **Das
Muster eines Schrittes, der Tabellen anlegt UND sät, ist Schritt 75**
(`SchemaMigration.cs:4893`, registriert `:3703`, Konstante `:2762`). Vier Handgriffe in fester
Reihenfolge (`SchemaMigration.cs:4879-4882`):

1. Tabelle samt Index über `NutzungsdauerSchema.Anweisungen` (`SchemaMigration.cs:4896-4901`),
2. die Verweisspalten an vorhandenen Tabellen über `SqliteSpalteAnlegen` (`:4903-4905`),
3. die **Saat** (`NutzungsdauerSchema.SaatSchreiben()`, `:4911`),
4. die Saat-Zuordnung (`:4912`), die die Ids der Saat braucht.

Zwei Regeln stehen im Kommentar (`SchemaMigration.cs:4883-4891`): **Saat und Zuordnung laufen
über den Kern mit `?`-Parametern**, nicht über zusammengesetzten SQL-Text; und der Zweig läuft
vor dem ersten Fenster, muss also still bleiben — deshalb `try` mit Fehlertext in den Bericht.
Die Schlussnotiz benennt ausdrücklich die Ergebnisneutralität (`:4930-4938`).

**Die Schema-Klasse ist die EINE Quelle** (`EPOS.Kern/Allgemein/Update/NutzungsdauerSchema.cs:135-146`,
`:218` DDL, `:257` `Anweisungen`, `:297` `Saat`). Aus ihr bedienen sich vier Leser:

| Leser | Fundstelle |
|---|---|
| Migrationsschritt (Programmstart) | `SchemaMigration.cs:4893` |
| Werkzeug Testdatenbankschema | `Werkzeuge/Testdatenbankschema/Program.cs:272-302` (Schritt 75), `:161` (Wechselrichter), `:168` (Strangzuordnung), `:244` (Speicherauslegung) |
| Kopier-/Schreibweg des Katalogs | `WechselrichterCtrl.cs:103` |
| Nachweis (Tests) | z. B. `EPOS.Kern.Tests/Migration74Tests.cs` |

Für additive **Spalten** an vorhandenen Tabellen gilt der zweite Katalog:
`EPOS.Kern/Allgemein/Update/SchemaKatalog.cs:46` („EINE Quelle für alle additiv angelegten
Spalten"), Typ `SchemaSpalte` `:9`. Tabellennamen stehen dort als Konstanten (`:47-51`, `:213`,
`:243`) — auch Zonen- und Bauteiltabellen bekämen dort ihren Namen.

### 1.5 Auslieferungsvorlage: schemagetrieben, aber an Namenskonventionen gebunden

`Werkzeuge/Auslieferungsvorlage` pflegt **keine** Tabellenliste; es liest das Schema
(`Werkzeuge/Auslieferungsvorlage/Projektsicht.cs:34`, `:113`). Drei Regeln entscheiden über
jede neue Tabelle (`Projektsicht.cs:25-41`):

1. **Projektspalte** — eine Tabelle mit `ID_Projekt` oder `ProjektID` führt Projektdaten und
   wird beim Bau der Vorlage geleert (Stufe 1, `Projektsicht.cs:70`).
2. **Folgetabellen** — was über einen Fremdschlüssel daran hängt, gehört transitiv dazu
   (Stufe 2, `:80-86`, „ELTERN VOR KINDERN sortiert").
3. **Kataloge sind ausgenommen** — `IstStamm` prüft `EndsWith("_STAMM", StringComparison.Ordinal)`
   (`Projektsicht.cs:110-111`). Der Vergleich ist **ordinal**, und das ist kein Zufall:
   `Tab_Brennstoff_Stamm` in gemischter Schreibweise ist ausdrücklich **kein** `_STAMM`-Katalog
   (`:44-52`).

In Schritt 3 behält das Werkzeug in `*_STAMM` nur, was `ReadOnly = TRUE` trägt
(`Werkzeuge/Auslieferungsvorlage/Vorlagenbau.cs:107-115`); ein Katalog, der dadurch **leer**
würde, löst den Wächter aus (`Vorlagenbau.cs:23`, `:175`, Abbruchcode 4 laut
`Argumente.cs:72`).

> **Folge für den Vorschlag.** `Tab_Baustoff_STAMM` muss (a) genau so geschrieben sein,
> (b) eine Spalte `ReadOnly INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))` führen und
> (c) seine Saat mit `ReadOnly = 1` schreiben — sonst ist der Baustoffkatalog in der
> ausgelieferten Datenbank leer und der Prüflauf rot. `Tab_Zone`, `Tab_Bauteil` und
> `Tab_Bauteilschicht` dagegen sollen **nicht** auf `_STAMM` enden und müssen über
> `ID_Projekt` oder über die FK-Kette an `Tab_Gebaeude` erkennbar sein.

### 1.6 Projekt duplizieren und übertragen

`EPOS.Kern/Controller/ProjektDuplizierenCtrl.cs` ermittelt die zu kopierenden Tabellen
generisch aus dem Schema (`:12-22`: „Neu hinzugefuegte Projekt-Tabellen werden damit
automatisch beruecksichtigt"). **Drei Karten müssen trotzdem von Hand gepflegt werden:**

- `FK_MAP` — FK-Spalte → Zieltabelle, damit die Kopie ihre Ids versetzt (`:85-137`). Enthält
  bereits `{"ID_Gebaeude","Tab_Gebaeude"}` und `{"ID_ProjektGebaeude","Z_ProjektGebaeude"}`
  (`:98-99`). Für `ID_Zone`, `ID_Bauteil`, `ID_Aufbau`, `ID_Baustoff` käme je ein Eintrag dazu.
  **Achtung:** Die Karte vergleicht ohne Rücksicht auf Groß-/Kleinschreibung; ein zweiter
  Eintrag desselben Schlüssels ist keine Redundanz, sondern eine `ArgumentException` beim Laden
  der Klasse (`:113-118`).
- `FK_OVERRIDE` — mehrdeutige Spaltennamen je Tabelle (`:143-150`).
- `KINDER` — Tabellen ohne verlässliches `ID_Projekt`, gefiltert über den Eltern-FK
  (`:152-175`). Das Vorbild steht schon da:
  `{"Tab_DBTagV", "ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0})"}`
  (`:156`) und zweistufig geschachtelt für `Tab_DBTagVDaten` (`:157`). Genau diese Form braucht
  eine Kette Zone → Bauteil → Schicht, und zwar **dreistufig**. Der Kommentar `:168-175` nennt
  auch den Grund, warum man sich nicht auf die Auto-Erkennung verlassen soll: Sie nimmt die
  ERSTE Spalte mit deklarierter Beziehung, und bei zwei Fremdschlüsseln (z. B. `ID_Bauteil` und
  `ID_Baustoff` an der Schicht) entscheidet die Spaltenreihenfolge — über `ID_Baustoff`
  gefiltert fielen alle Schichten weg.

Der Projekttransfer (`EPOS.Kern/Controller/ProjektExportImportCtrl.cs:20`) schreibt ein
Manifest der tatsächlich vorhandenen Spalten und ist damit toleranter; er folgt derselben
Baumlogik (`:212`, `:519-530`).

### 1.7 Auswirkung auf den Referenzlauf

Ein Schemaschritt, der **nur Tabellen anlegt und sät**, ist ergebnisneutral, und
`Referenzlaeufe/LIESMICH.md` hat dafür einen festen Wortlaut (`:236-245` zu Schritt 75,
`:247-259` zu Schritt 76): „Der Referenzlauf ist 5/5 byte-gleich gegen diese Basis … Kein
Rechenwert ändert sich, die Basis wird nicht neu eingefroren, und keine der drei
Einfrierregeln ist berührt — der Schritt legt eine Tabelle an und füllt sie, er rührt keine
gerechnete Größe an." Dazu gehören Schemastand, Dateigröße, Tabellenzahl und `integrity_check`.

Das gilt für `Tab_Zone` & Co. genau so lange, **wie kein Rechenweg sie liest**. Sobald der
Bauteilweg (G3) rechnet, greift die allgemeine Regel aus `CLAUDE.md:138-143`; die vierte
Einfrierregel „gesäte Gebäudedaten" ist mit Entscheid E4 ohnehin angekündigt (Konzept N1.8).

---

## 2. Kataloge und Administration

### 2.1 Das Register: ein Katalog ist ein Eintrag, kein Dialog

`EPOS.Kern/Allgemein/Katalog/KatalogRegistry.cs:80` führt heute **zwanzig** Kataloge
(`Schluessel = …`, `:114` bis `:356`). Ein Eintrag besteht aus (`KatalogRegistry.cs:40-77`):

- `Schluessel` (sprachneutral, ASCII), `Tabelle` (die `_STAMM`-Tabelle),
- `AusschlussSpalten` — vom Anwender gepflegte Felder, die nicht in den Inhaltsvergleich
  eingehen (`:58-64`),
- `Datenbloecke` — **genau der Mechanismus für Kindzeilen**: `Tabelle`, `FkSpalte`,
  `Sortierung`, `WertSpalten` (Beispiel Wärmepumpe `:118-131`),
- `ImportSpalten` — was ein Dateiimport befüllt (`:65-70`),
- `VerwendungsPruefungen` — leer bei Kopiersemantik (`:71-77`).

Der Gebäudekatalog ist heute der kürzeste Eintrag überhaupt (`KatalogRegistry.cs:218-220`):
Schlüssel `GEBAEUDE`, Tabelle `Tab_Gebaeude_STAMM`, sonst nichts — **keine Datenblöcke**. Ein
Baustoffkatalog käme als 21. Eintrag dazu; ein Bauteilaufbau mit Schichten wäre ein
`Datenblock` am Aufbau (FK `ID_Aufbau`, Sortierung `Reihenfolge`).

Zwei Pflichtstellen hängen daran: `KatalogRegistry.Anzeige(schluessel)` (`:392-419`) braucht
einen Ressourcenschlüssel `ADM_KATALOG_*`, und der Kommentar `:383-389` erklärt, warum die
Liste dort und nicht im Dublettendialog steht.

### 2.2 Katalogliste, Filterprofil, Detailfelder

| Baustein | Fundstelle | Rolle |
|---|---|---|
| `Katalogfilterprofil` / `Katalogspalte` | `EPOS.Kern/Allgemein/Katalog/Katalogfilterprofil.cs:57`, Spaltenarten `:32-52` | Welche Spalten die Liste zeigt: `Text` (enthält…), `Zahl` (`>10`, `10..60`, `=15` über `Zahlenausdruck.cs`), `JaNein` (nur Sortierpfeil) |
| `KatalogBrowserProfil` | `EPOS.Kern/Allgemein/Katalog/KatalogBrowserProfil.cs:129`, Feldart `:35`, `BrowserDetailfeld` `:75` | Titel, Listenbeschriftung, Detailüberschrift, Hilfeschlüssel, Detailfelder mit `Schluessel`/`Bezeichnung`/`Einheit`/`Art`/`Editierbar` |
| `Katalogfeld` | `EPOS.Kern/Allgemein/Katalog/Katalogfeld.cs:20` | Leseregeln: **NULL bleibt `null`** (anders als `StilleDb`), weil „NULL etwas anderes ist als eine gemessene Null" (`:12-17`) |
| `Katalogfilterregister` | `EPOS.Kern/Allgemein/Katalog/Katalogfilterregister.cs` | je Katalog EIN Filterstand für die Sitzung |
| Konzept | [`../Konzept_Katalogfilter_EPOS-Plan.md`](../Konzept_Katalogfilter_EPOS-Plan.md), Kapitel 3.2 „Was in einem Filterprofil steht" (`:349`), 5.6 | Herleitung und Hausregeln |

**Die eine Katalogliste des Hauses** ist `EPOS.UI/Bausteine/Katalogliste.razor` (942 Zeilen,
23 Wirte). Drei Dinge daran sind für einen neuen Katalog wichtig:

1. **Gefiltert wird VOR dem Raster** (`Katalogliste.razor:17-26`): `Raster.razor` ist eine
   dünne Hülle um QuickGrid, und QuickGrid filtert nicht. Der Kern schränkt über
   `Katalogfilter.Anwenden` ein, die Komponente reicht die bereits eingeschränkte Liste weiter.
2. **Virtualisierung**: `ItemSize` geht an `Virtualize` **und** ins Stilblatt
   (`Katalogliste.razor:374-381`); der Übergang „virtualisiert → nicht mehr virtualisiert"
   ist der bekannte `@key`-Fix (`:24-26`, `:505-529`).
3. **Die Markierung hängt am Bezeichner, nicht an der Zeilennummer** (`:34-38`) — die gewählte
   Zeile bleibt gewählt, auch wenn ein Filter sie ausblendet.

> **Pflicht aus `CLAUDE.md:126`:** „**Vor jeder Änderung an `Raster`, `Katalogliste` oder den
> `.epos-raster*`-Regeln** `Proben/Rasterprobe` ziehen; bunit allein misst das nicht." Ein
> neuer Katalog, der nur ein Profil hinzufügt, fasst diese Dateien nicht an und braucht die
> Probe nicht; eine neue Spaltenart oder eine geänderte Zeilenhöhe schon.

### 2.3 Katalogimport und Konfliktbehandlung

`EPOS.Kern/Allgemein/Import/KatalogImportAblauf.cs:56` ist der EINE Ablauf (Lesen, Vorprüfen,
Ausführen) für fünf Ausprägungen; was sie unterscheidet, steht als Daten in
`KatalogImportProfil` (`EPOS.Kern/Allgemein/Import/KatalogImportProfil.cs:19` Aufzählungstyp,
`:37-48` die fünfte Ausprägung mit `Quellen`, `Listenspalten`, `Zweitfilter`). Der Ablauf
**zeigt nichts an** (`KatalogImportAblauf.cs:68-71`); der Konfliktdialog ist eine Zäsur, kein
Rückruf. `ImportFortschritt` (`:8-25`) und `ImportBilanz` (`:29-52`) sind die Rückmeldungen.

`EPOS.Kern/Allgemein/Katalog/ImportKonfliktModell.cs:46` trägt die Entscheidungsregeln
oberflächenfrei; die Aktionen sind ein **Wert**, kein Anzeigetext (`:7-13`, Begründung
`:36-45`: Ein Sprachwechsel zur Laufzeit hätte die Rückabbildung aus dem Zelltext zerrissen).
Der Dialog dazu ist `EPOS.UI/Dialoge/Import/ImportKonflikteDialog.razor` samt bunit-Probe
`EPOS.UI.Tests/Dialoge/ImportKonflikteDialogTests.cs`.

**Für einen Baustoffkatalog heißt das:** Ein Dateiimport (z. B. eine Stoffwerteliste) ist eine
sechste Ausprägung von `KatalogImportArt` plus ein Profil — kein neuer Ablauf, kein neuer
Konfliktdialog. Ein IFC-Import dagegen ist **kein** Katalogimport: Er ist projektbezogen und
läuft nach Konzept 8.4 aus dem Gebäudedialog.

### 2.4 Gesäte Standardwerte — das Vorbild

Für die Saat eines Baustoffkatalogs gibt es zwei Vorbilder im Haus:

- **Nutzungsdauer** (`EPOS.Kern/Allgemein/Update/NutzungsdauerSchema.cs:297`): ein
  `NutzungsdauerSaat[]` mit **fester Id** je Zeile („sie bleibt über alle Auslieferungen
  gleich", `:78`), Quellenangabe je Zeile (`QUELLE_VDI`, `QUELLE_VDI_UND_AFA`, `:191`) und
  einer Sortiernummer. Geschrieben wird über `SaatSchreiben()` (`:448`) und
  `ZuordnungSchreiben()` (`:482`) — mit `?`-Parametern, idempotent.
- **Brennstoffe und Emissionsfaktoren** (`sql/schema/001_grundschema.sql:615`
  `Tab_Brennstoff_Stamm`, `:21` `emissionsart`, `:34` `emissionswert`): gesät in der
  Testdatenbank und darum **einfrierpflichtig** (`CLAUDE.md:148-151`).

> Die Saat eines Baustoffkatalogs nach DIN 4108-4 / DIN EN ISO 10456 (Konzept 6.3) folgt dem
> Nutzungsdauer-Muster: feste Ids, `Quelle` je Zeile, `ReadOnly = 1`. Solange kein Rechenweg
> sie liest, ist der Schritt ergebnisneutral (1.7).

### 2.5 Menü

Das Menü ist Daten (`EPOS.UI/Bausteine/Menuetabelle.cs:175` Kopf „Administration"). Ein
Baustoffkatalog wäre ein `Menuepunkt` unter der Rubrik „Wärmebedarf & Heizung"
(`Menuetabelle.cs:177-198`) oder als eigene Rubrik; sein Ziel ist ein `Seitenschluessel`
(`EPOS.UI/Seiten/Seitenschluessel.cs:28`, Beispiel `WechselrichterAdmin` `:252`), der in der
Prüfliste `:401-402` mitgeführt wird. **Regel W16c-E-6:** kein Untermenü mit nur einem Punkt
(`Menuetabelle.cs:73`, `CLAUDE.md:48`).

---

## 3. Listen- und Detaildialoge

### 3.1 Die Hausmuster, wörtlich

`EPOS.UI/CLAUDE.md` nennt sie in vier Zeilen:

- **„Jeder Dialog trägt OK und Abbrechen"** — als `SpeichernLeiste`, nie als eigene Knopfzeile
  (`EPOS.UI/CLAUDE.md:48`).
- **„Eine LISTE steht in einem festen Rahmen mit Rollbalken"**: `.epos-raster-huelle`
  (`:95`).
- **„Projekt ↔ Datenbank immer über `Zweispaltenauswahl`"** (`:98`) — genau der Aufbau des
  Gebäudedialogs.
- **„Ein KATALOGDIALOG nutzt die Höhe"**: Wurzel `epos-katalog-dialog`, Baustein
  `Katalograhmen` (`:102`).
- **„Ein PARAMETERBLOCK steht im `Formularraster`"** (`:105`).
- **„Ein Dialog IN einem Dialog"**: Unterdialoge als `Ueberlagerung` im selben Fenster
  (`:120`) — nie ein zweites Fenster (Risiko R2).

### 3.2 Drei Vorbilder für „Kindliste + Detail"

**(a) `KatalogBrowserDialog.razor` — EINE Komponente, VIER Ausprägungen**
(`EPOS.UI/Dialoge/Erzeuger/KatalogBrowserDialog.razor:1-36`). Liste links, Detailblock rechts
(`:69-121`), Schlussleiste mit „Neu… / Bearbeiten… / Löschen / OK" (`:124-139`), Rückfragen
und Namensabfrage als Überlagerungen (`:141-157`), der **Editor selbst als Überlagerung**
(`:159-167`). Was die vier Ausprägungen trennt, sind acht Werte im
`KatalogBrowserProfil` (`:11-13`). Die Parameter sind sauber getrennt in Daten (`Wege`,
`:194`), Verhalten (`NurLesen` `:189`) und Texte (`:239-262`).

**(b) `PufferSpProjektDialog.razor` — Arbeitsstand, geschrieben wird im OK-Weg.** Der
Klassenkopf ist die Vorschrift für einen Zonendialog
(`EPOS.UI/Dialoge/Simulation/PufferSpProjektDialog.razor:11-18`):

> „Der Dialog fuehrt einen ARBEITSSTAND: ‚Anlegen'/‚Uebernehmen' prueft die Felder und legt sie
> in die Liste, ‚Entfernen' nimmt eine Zeile heraus — alles im Hauptspeicher. Erst OK schreibt,
> und zwar in der Reihenfolge Entfernen, Aendern, Anlegen; dort entsteht auch die Id eines
> neuen Speichers. … Eine vorlaeufige Zeile traegt bis zum OK eine NEGATIVE Nummer; nur eine
> positive Id hat eine Entsprechung in der Datenbank."

Dazu: **drei Rollen, ein Delegatensatz** (`:20-24`), die Pflichtprüfung sitzt im „Übernehmen"
und nicht im Klicken (`:33-35`), die Parameterblöcke stehen im `Formularraster`, **die Listen
bleiben draußen** — „eine Liste ist kein Formularblock" (`:51-58`), und Esc räumt erst eine
stehende Rückfrage weg und wirkt dann wie Abbrechen (`:60-64`).

**(c) `PvStraengeFelder.razor` — die Kindliste im Elterndialog**
(`EPOS.UI/Dialoge/Erzeuger/PvStraengeFelder.razor`, 1 337 Zeilen). Sie ist der nächste
Verwandte einer Bauteilliste im Zonendialog. Drei Lehren stehen im Kopf:

- **Eine Option, die man nur durch Löschen der Kindzeilen abwählen könnte, ist keine Option**
  (`:14-20`) — der Weg steht als eigene Spalte, nicht als abgeleiteter Zustand.
- **Die weiche Sperre am falschen Ort** (`:23-36`): „noch kein Strang" ist keine Voraussetzung
  der Wahl, sondern ihre **Folge**; der einzige Weg zum ersten Strang lag innerhalb des
  gesperrten Weges. Für Zonen heißt das: „Gebäude ohne Zone" darf den Zonenweg nicht sperren.
- **Die Gerätezeile steht immer** (`:38-44`): Auch ohne Kindzeile zeigt der Abschnitt Filter,
  Klappliste und den Knopf „anlegen"; ist der Katalog leer, steht dort der Weg zum Import —
  „ein stummer leerer Abschnitt waere derselbe Befund noch einmal".

### 3.3 Bausteine, die eine Bauteilliste braucht

| Baustein | Fundstelle | Wofür |
|---|---|---|
| `Zeilenraster` | `EPOS.UI/Bausteine/Zeilenraster.razor:1-32` | **Die Zeile IST eine kleine Maske** mit mehreren einander bedingenden Bedienelementen — genau die Bauteilzeile (Art, Fläche, U, Azimut, Neigung, Randbedingung). Spalten über CSS-Spuren (`grid-template-columns`), die Zeilenkomponente trägt `display:contents`; Abschlusszeile „+ Neue Position hinzufügen…" und Summenfuß sind eingebaut. Wirte: `KostenKomponenteDialog`, `PufferSpProjektDialog`, `QuellePufferspeicherDialog`, `WaermesenkeDialog`, `KennlinienEditorDialog` |
| `Raster` | `EPOS.UI/Standards/Raster.razor` | QuickGrid-Hülle für reine **Datenzeilen** (Anzeige, Sortierung) |
| `Katalogliste` | `EPOS.UI/Bausteine/Katalogliste.razor` | Katalogauswahl mit Suche, Spaltenfilter, Trefferzahl |
| `Katalogfelder` | `EPOS.UI/Bausteine/Katalogfelder.razor`, Wirte `HeizkesselDialog`, `KatalogBrowserDialog:105` | Das erklärte Feldraster: welche Feldart wie gezeichnet wird, wann ein Feld setzbar ist — „Zwei Fassungen derselben Regel waeren zwei Fassungen, die auseinanderlaufen" (`KatalogBrowserDialog.razor:101-104`) |
| `Reiter` / `Reiterblatt` | `EPOS.UI/Bausteine/Reiter.razor:1-40` | Reiter im Dialog; die Blätter melden sich **selbst** an (CascadingValue), „zwei Listen waeren zwei Wahrheiten" (`:17-21`); Tastatur und ARIA eingebaut |
| `Zweispaltenauswahl`, `Katalograhmen`, `Formularraster`, `Formulargruppe`, `Gruppenkopf`, `Ueberlagerung`, `Rueckfrage`, `NamensDialog`, `Warnbanner`, `InfoKnopf` | `EPOS.UI/Bausteine/`, `EPOS.UI/Dialoge/Allgemein/NamensDialog.razor` | der übrige Hausbaukasten |

`GebaeudeKatalogDialog.razor` zeigt, wie ein Gebäudesatz heute auf **zwei Reitern** steht
(`:83` `<Reiter>`, `:87` Blatt „FLAECHEN", `:209` Blatt „TEMPERATUREN") und dass der zweite
Reiter einen **eigenen Stand** führt, den erst „Übernehmen" in den Satz schreibt (`:549`,
`:754-757`). Ein Reiter „Zonen" fügt sich hier ohne Bruch ein; die Regel aus
`EPOS.UI/CLAUDE.md:82` gilt dabei: „Ein Reiter zeichnet nie ein VORBELEGTES DTO als Ergebnis."

### 3.4 DTO-Muster und Hüllen

Die DTO liegen **neben** der Komponente (`EPOS.UI/Dialoge/Bedarf/GebaeudeDaten.cs`), nicht in
`EPOS.UI.Daten`; dort liegen die **Hüllen**. Drei Regeln zeigt `GebaeudeDaten.cs`:

- **Der Schlüssel der Zeile ist der Zuordnungsschlüssel, nicht die Stamm-Id** (`:19-22`): Zwei
  gleiche Gebäude im Projekt haben dieselbe Stamm-Id; ohne `IdZ` traf „Entfernen" die falsche
  Zeile. **Für Zonen und Bauteile gilt dasselbe**: der Schlüssel ist die Zeilen-Id, nie der Name.
- **Veränderliche Klasse für die Liste, `record` für den Detailblock** (`:12-15` gegen `:62`,
  `:73`): „der Dialog schreibt beim Ändern hinein, und die Liste gehört der Hülle".
- **Eine noch nicht gespeicherte Zeile bekommt eine geratene Id ab 100000** (`:9-10`) — die
  Variante zur negativen Nummer aus `PufferSpProjektDialog`.

Hüllen: heute `WindowsFormsApplication1/Views/Gebäude/GebaeudeHuelle.cs` (483 Zeilen) und
`GebaeudeKatalogHuelle.cs` (554); plattformfrei bereits
`EPOS.UI.Daten/Pufferspeicher/PufferSpProjektHuelle.cs` und
`EPOS.UI.Daten/Stromspeicher/StromspeicherAuslegungHuelle.cs`. Konzept 8.3 sieht den Umzug
nach `EPOS.UI.Daten/Bedarf/GebaeudeHuelle.cs` mit G1 vor; der Ordner existiert bereits
(`EPOS.UI.Daten/Bedarf/BedarfErgebnisHuelle.cs`).

### 3.5 Navigation zwischen Eltern- und Kinddialog

Zwei Wege, beide vorhanden:

- **Überlagerung im selben Fenster** — der Regelweg (`EPOS.UI/CLAUDE.md:120`,
  `KatalogBrowserDialog.razor:159-167`, `GebaeudeDialog.razor:24-28`: „VIER UEBERLAGERUNGEN
  statt vierer Fenster (Risiko R2)"). Der Wirt reicht den Parametersatz als
  `IReadOnlyDictionary<string,object>` herein (`KatalogBrowserDialog.razor:236`); ein
  unbekannter Schlüssel trifft nur `[Parameter]` (`EPOS.UI/CLAUDE.md:87`).
- **Eigene Maske über `INavigation.OeffneMaske(schluessel, argumente)`**
  (`EPOS.Kern/Allgemein/Dienste/INavigation.cs:26-31`) mit sprachneutralem ASCII-Schlüssel
  (`:19-22`); Seitenschlüssel in `EPOS.UI/Seiten/Seitenschluessel.cs:28`, Sprungziele in
  `EPOS.UI/Dialoge/Allgemein/Sprungziel.cs`.

**Für die Kette Gebäude → Zone → Bauteil → Aufbau ist die Überlagerung der Weg**, und zwar
geschachtelt: Der Gebäudedialog ist bereits Wirt von vier Überlagerungen. Zu beachten ist die
Esc-Regel (`EPOS.UI/CLAUDE.md:45`): „Esc schließt überall, wobei jeder Wirt erst seine
Überlagerungsschalter prüft" — bei drei Ebenen also drei Prüfungen.

### 3.6 Tests

`EPOS.UI.Tests/Dialoge/` führt **96 Testklassen**, je eine Komponente. Das Muster zeigt
`GebaeudeKatalogDialogTests.cs`: `EposBunitContext` als Basis (`:22`), `JSInterop.Mode = Loose`
und Ersatzdienste im Konstruktor (`:34-36`), eine `Satz()`-Fabrik mit vollständig belegtem DTO
(`:39-70`), eine `Aufbauen(...)`-Fabrik mit Delegaten für Speichern/Lesen/Schließen (`:72-79`).
**Die Kultur ist auf de-DE gepinnt** (`Kulturvorrichtung.cs`, Begründung
`GebaeudeKatalogDialogTests.cs:17-19`: der Windows-Läufer läuft mit englischer Oberfläche).
Dazu die Wächter `ListenrahmenTests`, `UeberlagerungstitelTests`, `SchliesskreuzWacheTests`,
`StilblattTests`, `KatalogdialogTests`, `ParametersatzTests`, `HuellenwegTests`.
**Grenze:** „bunit misst weder Farbe noch Breite noch Höhe" (`EPOS.UI/CLAUDE.md:72`) — dafür
`Proben/Rasterprobe`.

---

## 4. Bericht: Tabellen je Teilobjekt

Zwei Muster stehen bereit, und sie sind bewusst getrennt (`BausteineProjekt.cs:100-104`:
„Hier steht die AUFSCHLÜSSELUNG je Speicher — dieselbe Arbeitsteilung wie bei Bedarf und
Deckungsgraden darüber").

**(a) Eigenschaftsblock je Objekt** — so steht das Gebäude heute im Bericht
(`EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineProjekt.cs:35-52`): `Ueberschrift2("Gebäude")`,
dann je Zeile der Tabelle `Ueberschrift3(Gebaeudename)` und ein `k.Eigenschaften(...)`-Block mit
acht Paaren. Die Daten kommen aus `ProjektDetails.Gebaeude`
(`EPOS.Kern/Allgemein/Bericht/ProjektDetails.cs:39-40`, gefüllt `:72` aus `Tab_Gebaeude`).

**(b) Echte Tabelle je Teilobjekt** — die Speichertemperaturen
(`BausteineProjekt.cs:105-140`): Spaltenbreiten als `int[]`, `k.NeueTabelle(w)`, eine Kopfzeile
mit `WordBerichtGenerator.HEAD_FILL`, dann eine `TableRow` je Objekt, fehlende Werte als „—".
**Der Abschnitt entfällt vollständig, wenn kein Objekt einen Wert trägt** (`:89-93`): „Eine
Tabelle voller ‚—' wäre keine Aussage, sondern eine Frage."

**Kennzahlen** stehen in `EPOS.Kern/Allgemein/Bericht/KennzahlenKatalog.cs:204` ff.
(`new Kennzahl(schlüssel, de, en, Einheit, Gruppe, Format, …)`); `AbweichungsErmittler.cs`
führt sie im Variantenvergleich. Konzept 9 sieht dort bereits Rechenmodell, Spitzenwerte,
Kühlenergie vor; Entscheid E2 legt H_T dazu (Konzept N1.6).

> **Vorbild für eine Zonentabelle:** Muster (b), Spalten Zone | Fläche | Volumen |
> H_T [W/K] | H_ve [W/K] | Heizwärme [kWh/a] | Spitze [kW], entfällt bei einem Gebäude ohne
> Zonen. Als Kennzahl je Projekt taugt nur eine **Summe oder ein Extremum** — der
> `KennzahlenKatalog` kennt keine Objektlisten.

---

## 5. Vorschlag für das Datenmodell (zur Vorlage beim Anwender)

**Nicht entschieden.** Der Vorschlag hält sich an die Muster aus Kapitel 1 und an die
Entscheide E1–E6.

### 5.1 Die sechs Tabellen

**`Tab_Zone`** — Bauform B an `Tab_Gebaeude`, keine `_STAMM`-Entsprechung (eine Zone ist
Projektware; wiederverwendbar ist der **Bauteilaufbau**, nicht die Zone).

| Spalte | Typ | NULL? | Bedeutung / NULL bedeutet |
|---|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT | — | |
| `ID_Gebaeude` | INTEGER NOT NULL | — | FK → `Tab_Gebaeude.ID`, `ON DELETE CASCADE` |
| `Rang` | INTEGER NOT NULL | — | Reihenfolge, lückenlos ab 1 (Muster `Z_AnlageStrang.Rang`) |
| `Bezeichner` | TEXT NOT NULL CHECK (length ≤ 80) | — | Zonenname |
| `Nutzflaeche` | REAL | ja | m²; NULL = aus den Bauteilen |
| `Raumhoehe` | REAL | ja | m; NULL = `Tab_Gebaeude.Raumhoehe` |
| `Volumen` | REAL | ja | m³; NULL = Fläche × Höhe |
| `Raumsolltemperatur_Tag`, `_Nachtabsenkung`, `Maximaleraumtemperatur` | REAL | ja | NULL = Wert des Gebäudes |
| `Interne_Waermegewinne`, `Bewohner` | REAL | ja | NULL = anteilig aus dem Gebäude (Flächenschlüssel) |
| `Luftwechsel_Infiltration`, `Luftwechsel_Nutzer` | REAL | ja | NULL = Gebäudewert (Konzept 6.1, Schemaschritt 78) |
| `Heizung_Strahlungsanteil`, `Heizleistung_Max` | REAL | ja | NULL = Gebäudewert |
| `Herkunft` | TEXT | ja | `MANUELL` / `IFC` / `VORGABE`; NULL = unbekannt (Muster `Tab_Wechselrichter.Herkunft`) |
| `IfcGuid` | TEXT CHECK (length ≤ 22) | ja | IfcGloballyUniqueId, Base64-22, NULL = nicht aus IFC |

**`Tab_Bauteil`** — Bauform B an `Tab_Zone` (nicht am Gebäude: im Einzonenfall hängt es an der
einen Zone, und die Abfragen bleiben gleich).

| Spalte | Typ | NULL? | Bedeutung |
|---|---|---|---|
| `ID` | INTEGER PK AUTOINCREMENT | — | |
| `ID_Zone` | INTEGER NOT NULL | — | FK → `Tab_Zone.ID`, `ON DELETE CASCADE` |
| `Rang` | INTEGER NOT NULL | — | Reihenfolge in der Liste |
| `Bezeichnung` | TEXT NOT NULL CHECK (length ≤ 80) | — | |
| `Bauteilart` | TEXT NOT NULL CHECK (IN ('AUSSENWAND','DACH','BODENPLATTE','FENSTER','TUER','INNENWAND','DECKE','SONSTIGES')) | — | Persistenzwerte in `DbWerte`, sprachneutral |
| `ID_Aufbau` | INTEGER | ja | FK → `Tab_Bauteilaufbau.ID`, **ohne** Kaskade; NULL = nur U-Wert |
| `Flaeche` | REAL NOT NULL | — | m² |
| `U_Wert` | REAL | ja | W/(m²K); NULL = aus dem Aufbau gerechnet |
| `g_Wert`, `Rahmenanteil`, `Verschattungsfaktor` | REAL | ja | nur Fenster; NULL = Vorgabe (0,3 / 0,9) |
| `Azimut`, `Neigung` | REAL | ja | °; NULL = nach `Bauteilart` (Dach 0°/Wand 90°) |
| `Randbedingung` | TEXT | ja | `AUSSENLUFT` / `ERDREICH` / `KELLER` / `ZONE` / `UNBEHEIZT`; NULL = Außenluft |
| `ID_Nachbarzone` | INTEGER | ja | FK → `Tab_Zone.ID`, **ohne** Kaskade; nur bei `Randbedingung = 'ZONE'` |
| `Herkunft`, `IfcGuid` | TEXT | ja | wie bei der Zone |

**`Tab_Bauteilaufbau`** — der wiederverwendbare Schichtaufbau, **mit** Stammzwilling
`Tab_Bauteilaufbau_STAMM` (Spalten gleich, statt `ID_Projekt` ein
`ReadOnly INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))`): `ID`, `ID_Projekt`, `Bezeichner`,
`Beschreibung`, `Bauteilart`, `Quelle`, `Herkunft`.

**`Tab_Bauteilschicht`** — Bauform A an `Tab_Bauteilaufbau` (`_STAMM`-Zwilling ebenso):
`ID`, `ID_Aufbau` (FK, Kaskade), `Reihenfolge` INTEGER NOT NULL (innen → außen, ab 1),
`ID_Baustoff` INTEGER (FK auf **die Projektkopie**, ohne Kaskade; NULL = freie Eingabe),
`Dicke` REAL NOT NULL (m), sowie `Lambda`, `Rho`, `cp` als REAL NULL — **die Kopie der
Stoffwerte zum Zeitpunkt der Zuordnung**, damit eine spätere Katalogänderung kein gerechnetes
Ergebnis rückwirkend verschiebt (dieselbe Begründung wie bei der Kopiersemantik,
`KatalogRegistry.cs:86-90`).

**`Tab_Baustoff_STAMM` / `Tab_Baustoff`** — der Katalog und seine Projektkopie, spaltengleich
(Regel `WechselrichterSchema.cs:280-284`): `ID`, `Bezeichner` NOT NULL, `Gruppe` TEXT
(Mauerwerk, Beton, Dämmstoff, Holz, Putz, …), `Lambda` REAL W/(mK), `Rho` REAL kg/m³,
`cp` REAL J/(kgK), `Quelle` TEXT, `Herkunft` TEXT — dazu in `_STAMM` `ReadOnly`, in der
Projektkopie `ID_Projekt` NOT NULL.

**Zonenkopplung:** **kein eigenes `Tab_Zonenkopplung`.** Die Kopplung steht als
`ID_Nachbarzone` am Bauteil. Begründung: Eine Innenwand zwischen zwei Zonen ist genau ein
Bauteil mit Fläche, Aufbau und zwei Seiten; eine zweite Tabelle wäre eine zweite Wahrheit über
dieselbe Fläche, und die Regel „die Anlage weiss, zu welchem Projekt sie gehört, und eine
zweite Wahrheit könnte auseinanderlaufen" (`AnlageStrangCtrl.cs:20-23`) gilt hier wörtlich.
Die Gegenseite wird beim Lesen erzeugt (das Bauteil zählt in Zone A positiv, in Zone B negativ
gespiegelt); ein Wächter prüft, dass nicht **beide** Zonen dieselbe Trennfläche führen.

### 5.2 Verträglichkeit mit dem Einzonenmodell

- **Ein Gebäude ohne Zone bleibt ein Gebäude ohne Zone.** `Tab_Zone` leer heißt: der Klassenweg
  aus Konzept 4.3 rechnet wie bisher; der Leser prüft das über die Schemaprobe mit Gedächtnis
  (`AnlageStrangCtrl.cs:101` als Vorlage) und fällt still zurück.
- **`Tab_Gebaeude` bleibt die führende Ablage der Summen und Vorgaben.** Die fünf
  `k_Wert_*`/Flächenpaare (`001:1152-1161`) bleiben; sie sind der Klassenweg und zugleich die
  **Vorgabe**, aus der eine erste Zone erzeugt wird („Gebäude als eine Zone übernehmen").
- **Die U·A-Zeilen aus Entscheid E2 sind die gemeinsame Zielstruktur** (Konzept N1.6): je
  Bauteilgruppe U, A, U·A, Randbedingung, darunter H_T, H_ve, H_ges. Der Klassenweg füllt sie
  je **Gruppe**, der Bauteilweg je **Bauteil**, der IFC-Import liefert sie mit
  Herkunftskennzeichen. Eine Zone summiert ihre Bauteile in dieselben Zeilen.
- **Summenregel:** Sobald Zonen da sind, sind `Tab_Gebaeude.Wohnflaeche_gesamt` und die
  Flächenspalten **abgeleitete Anzeigen**, keine Eingaben. Das ist im Dialog sichtbar zu machen
  (Muster `Herleitungszeile`, `EPOS.UI/Bausteine/Herleitungszeile.razor`, im Einsatz
  `PufferSpProjektDialog.razor:25-31`), nicht still zu überschreiben.

### 5.3 Dialogskizze

```
Gebäudedialog (GebaeudeDialog.razor, Zweispaltenauswahl)
└─ Katalogeditor (GebaeudeKatalogDialog.razor, Überlagerung)
   ├─ Reiter "Flächen und U-Werte"      (Bestand)
   ├─ Reiter "Temperaturen, Ferien …"   (Bestand)
   ├─ Reiter "Hülle und Rechenmodell"   (E2/G1: U, A, U·A je Gruppe, H_T/H_ve/H_ges)
   └─ Reiter "Zonen"                    (NEU, G3+)
      ├─ Zeilenraster: Zone | Fläche | Volumen | H_T | Bauteile | ▸
      ├─ "+ Neue Zone …" / "Gebäude als eine Zone übernehmen"
      └─ Zonendialog (Überlagerung)
         ├─ Formularraster: Fläche, Höhe, Volumen, Solltemperaturen, Luftwechsel,
         │   Gewinne  — jedes leere Feld zeigt "Vorgabe: <Gebäudewert>"
         └─ Zeilenraster Bauteile: Art | Bezeichnung | A | U | Azimut | Randbed. | ▸
            └─ Bauteildialog (Überlagerung)
               ├─ Formularraster: Art, Fläche, Azimut, Neigung, Randbedingung,
               │   Nachbarzone (nur bei ZONE), g/Rahmen/Verschattung (nur Fenster)
               └─ Aufbau: Katalogwahl (Katalogliste) ODER Schichten
                  └─ Zeilenraster Schichten: Nr | Baustoff | Dicke | λ | ρ | c | R
                     Summenfuß: R_ges, U, C_wirk
```

Dazu:

- **Baustoffkatalog** in der Administration: `KatalogBrowserDialog` mit einer fünften
  Ausprägung `KatalogBrowserArt.Baustoff` (`KatalogBrowserProfil.cs:16`) — Liste links
  (Bezeichner, Gruppe, λ, ρ, c, Quelle), Detailblock rechts über `Katalogfelder`, Menüpunkt
  unter „Administration" (`Menuetabelle.cs:175`). **Kein neuer Dialogtyp.**
- **IFC-Zuordnungsdialog** (G4, Konzept 7.6 Punkt 5): eigener Dialog `IfcZuordnungDialog`, im
  Mehrzonenfall mit zwei Listen — Zonenliste (IfcSpace/IfcZone → Zone, mit Vorschlag) und
  Materialzuordnung (IfcMaterial → Baustoff des Katalogs, mit „neu anlegen"). Für beide ist der
  **Konfliktdialog** das Vorbild (`ImportKonfliktModell.cs:46`: Aktion als Wert, nie als Text;
  `ImportKonflikteDialog.razor` für die Tabelle mit Aktionsspalte). **Nichts wird ohne OK
  geschrieben.**
- **Hausmuster überall:** OK/Abbrechen als `SpeichernLeiste` (`EPOS.UI/CLAUDE.md:48`),
  Arbeitsstand im Hauptspeicher mit negativen Ids bis zum OK
  (`PufferSpProjektDialog.razor:11-18`), Esc kaskadiert über die Überlagerungsschalter
  (`EPOS.UI/CLAUDE.md:45`).

### 5.4 Migrationsschritte

| Schritt | Inhalt | Ergebnisneutral? |
|---|---|---|
| **77 / 78** | die Gebäudespalten aus Konzept 6.1 und 6.3 (G1/G2) — **nicht Gegenstand dieses Befundes** | ja (kein Leser im Bestandsweg) |
| **S-A** | `Tab_Baustoff_STAMM` + `Tab_Baustoff` anlegen, Baustoffsaat schreiben (`ReadOnly = 1`), DDL und Saat in `BaustoffSchema.cs` nach Muster `NutzungsdauerSchema` | ja — legt an und sät |
| **S-B** | `Tab_Bauteilaufbau(_STAMM)` + `Tab_Bauteilschicht(_STAMM)` anlegen, Indizes `(ID_Aufbau, Reihenfolge)` | ja |
| **S-C** | `Tab_Zone` + `Tab_Bauteil` anlegen, Index `(ID_Gebaeude, Rang)` bzw. `(ID_Zone, Rang)` | ja, solange kein Rechenweg liest |
| **S-D** | Registerpflege **ohne DDL**: `KatalogRegistry`-Eintrag `BAUSTOFF` (+ `AUFBAU` mit Datenblock), `SchemaKatalog`-Konstanten, `ProjektDuplizierenCtrl.FK_MAP` und `KINDER` (dreistufig), `Seitenschluessel`, `Menuetabelle`, Ressourcen + `ResourceDesigner` | ja |
| **S-E** | `GebaeudeStammCtrl.CopyFromStamm`/`Insert`/`Overwrite` auf die Spaltenlisten-Bauweise umstellen (Befund Q-1) und **Zonen mitkopieren** | **nein** — NULL statt 0,0 kann gerechnete Werte verschieben; eigener, begründeter Einfrierschritt |

Jeder Schritt bekommt Notiztext mit Zahlen nach Muster `SchemaMigration.cs:4930-4938` und einen
Absatz in `Referenzlaeufe/LIESMICH.md` nach Muster `:236-245`. Das Werkzeug
`Werkzeuge/Testdatenbankschema` bekommt dieselben Anweisungen aus derselben Quelle
(`Werkzeuge/Testdatenbankschema/Program.cs:161`, `:168`, `:244` als Vorbild), und nach jeder
neuen SQL-Anweisung läuft
`Werkzeuge/SqlDialektPruefer` (`CLAUDE.md:80-82`).

### 5.5 Aufwand je Teil (Größenordnungen, Entwicklung und Nachweis)

| Teil | Aufwand | Anmerkung |
|---|---|---|
| Schema S-A bis S-C samt Schema-Klassen, Testdatenbankwerkzeug, Migrationstests | 2–3 PT | reines Muster-Nachziehen |
| Baustoffsaat (rund 60 Stoffe, DIN 4108-4 / ISO 10456) samt Quellenpflege | 1–2 PT | Datenarbeit, kein Code |
| Baustoffkatalog in der Administration (Profil, Browser-Ausprägung, Menü, Texte, bunit) | 2–3 PT | kein neuer Dialogtyp |
| Aufbau- und Schichteditor (Zeilenraster, Summenfuß R/U/C, Validierung, bunit) | 3–4 PT | die eigentliche neue Maske |
| Zonenreiter + Zonendialog + Bauteilliste + Bauteildialog samt Hülle und bunit | 5–7 PT | vier Ebenen Überlagerung, Vorgabeanzeige je Feld |
| Controller und Kopierwege (Zone/Bauteil/Schicht, Löschen-und-Neuanlegen, Rettung vor der Kaskade) | 2–3 PT | Muster `AnlageStrangCtrl` + `WizardCtrl.StraengeSichern` |
| Befund Q-1: Gebäudekopierweg auf Spaltenliste (S-E), eigener Einfrierschritt | 1–2 PT | berührt den Referenzlauf |
| Duplizieren/Transfer: `FK_MAP`, `KINDER`, Proben | 1 PT | sonst zeigen Varianten auf fremde Zeilen |
| Bericht: Zonentabelle, Kennzahlen H_T je Gebäude | 1–2 PT | Muster `BausteineProjekt.cs:105` |
| **Summe (ohne Rechenweg und ohne IFC)** | **18–27 PT** | Konzept 11 nennt für G3 allein 8–12 PT — das ist der **Rechenweg** (Reduktion, Normnachweis); dieser Befund misst Datenmodell, Pflege und Bericht |

---

## 6. Fallen, die beim Umsetzen teuer werden

1. **Die Sicht `Abfrage_Projektgebaeude` hat eine feste Spaltenliste**
   (`sql/schema/002_views.sql:89`, eine Zeile mit 56 Spalten). SQLite kennt kein `ALTER VIEW`;
   jede neue Gebäudespalte braucht `DROP VIEW` + `CREATE VIEW` im selben Schritt, und
   `ProjektGebaeudeCtrl.ReadAll` muss vorher von Index- auf **Namenszugriff** umgestellt sein
   (Konzept 6.2). Ohne beides verschiebt sich die Zuordnung still.
2. **Die Kaskade beim Speichern** (1.1): `ON DELETE CASCADE` an einem Eltern, dessen Speicherweg
   Löschen + Neuanlegen ist, räumt jede Kindliste ab. Für Zonen ist die Gegenprobe zu schreiben,
   bevor der Dialog steht.
3. **`CopyFromStamm` entnullt** (Befund Q-1, 1.3) — die Regel „NULL = Vorgabe" bricht genau dort.
4. **`FK_MAP` ist `OrdinalIgnoreCase`** (`ProjektDuplizierenCtrl.cs:85-137`, die Begründung steht im Quelltext `:113-118`): ein zweiter
   Eintrag mit gleichem Schlüssel in anderer Schreibweise ist eine `ArgumentException` beim
   Laden der Klasse — der Fehler zeigt sich als „Programm startet nicht".
5. **`IstStamm` ist ordinal** (`Projektsicht.cs:110`): `Tab_Baustoff_Stamm` in gemischter
   Schreibweise wäre **kein** Auslieferungskatalog, und die Saat wanderte beim Bau der Vorlage
   nicht mit. `Tab_Brennstoff_Stamm` ist der Beleg, dass dieser Fehler schon einmal passiert ist.
6. **Bauteilart, Randbedingung und Herkunft sind Persistenzwerte**, keine Anzeigetexte
   (Drei-Schichten-Regel; `DbWerte.cs:2165-2214` als Vorbild, `ImportKonfliktModell.cs:36-45`
   als Begründung). Ein Sprachwechsel zur Laufzeit darf sie nicht treffen.
7. **Neue Ressourcenschlüssel** in beide `.resx`, danach
   `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben` (`CLAUDE.md:129`) — von Hand
   ergänzt entstehen Duplikate.
8. **Der Baustoffkatalog darf nicht ins Wiki mit Produktdaten** (`CLAUDE.md:261-266`): Stoffe
   nach Norm sind keine Produktdaten, ein Herstellerdämmstoff wäre einer. Der Wächter
   `WikiProduktdatenWacheTests` hält die Repo-Quellen gegen die Katalognamen der Testdatenbank.

---

## 7. Was dieser Befund NICHT beantwortet

- **Den Rechenweg.** Wie aus Zonen, Bauteilen und Schichten ein Mehrzonen-Netz wird (Kopplung
  der Zonenkapazitäten, Luftmassenströme zwischen Zonen, Reduktion mit Kettenmatrix), steht
  nicht hier; Konzept 4.2/4.3 und Befund I sind die Quellen.
- **Die IFC-Leseseite.** Entitäten, Psets, Raumgrenzen und die Bibliothekswahl stehen in
  Konzept 7 und in den Befunden C und N; hier steht nur, **wohin** der Import schreibt.
- **Ob ein Mehrzonenmodell überhaupt kommt und in welcher Stufe.** Das ist Sache des
  Konzeptpapiers und des Anwenders.
