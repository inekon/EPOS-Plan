# Merge 5 `origin/ios_migration` → `ios_migration` (05.09.2026)

Fünfte Zusammenführung desselben Strangs. Die Merges 1–4 vom 03.09.2026 holten den Umzug nach
`EPOS.Kern`/`EPOS.UI`, iU9/iU10, die Welle 1 des Blazor-Ports und die Stilllegung (Welle 0).
Merge 5 holt **alles Weitere bis zum 05.09.2026**: die iU9-Wellen W2 bis W16c, den Entscheid #76
(Zweispaltenauswahl) und die Befunde der Windows-Abnahmen vom 04./05.09.2026 — und trägt die
lokale Arbeit (PV-Ertragsmodell Paket A/B, Projektdialoge, PV-Katalog-Koeffizienten, FS1) auf
die neue Struktur um, in der **`WindowsFormsApplication1` keine Fachmaske mehr führt**.

* **Merge-Base:** `b0d3d86` (der Remote-Elter von Merge 4 — unser Strang ist seither nicht neu
  verzweigt)
* **Lokal vorher:** `9810d5b` (23 Commits: Merges 1–4 samt Nachweisen, Paket A/B, Projektdialoge,
  FS1, PV-Katalog-Koeffizienten, Konzept Projektstammdaten)
* **Remote:** `4cdc462` (**555** Commits, davon 86 Merge-Commits; **1 301 Dateien,
  +435 986 / −233 407** gegen die Basis)
* **Merge-Commit:** siehe `git log` (Branch `ios_migration`; Eltern `9810d5b` lokal und `4cdc462`
  remote)
* **Sicherungsreferenz:** Branch `sicherung/vor-merge5-2026-09-05` auf `9810d5b`, Bundle
  `Documents\WP-Plan_Git-Sicherungen\ios_migration_vor-merge5_2026-09-05_9810d5b0.bundle`
* **Nachweisanker:** Branch `merge5/ios-2026-09-05`
* **Nachzug:** Der Merge-Commit `8823317` trug fünf Dateien der ersten Portierungsstufe **nicht**
  (`KlimaregionCtrl.cs`, `PhotovoltaikVerguetungDaten.cs`, `PhotovoltaikVerguetungDialog.razor`,
  `SimulationKonfigHuelle.cs`, `PvModulImportDaten.cs`) — das Skript `port1.py` bricht planmäßig
  vor dem Schreiben seiner Dateiliste ab, und die Kopierliste speiste sich aus den Listen. Der
  Export des Merge-Commits baute deshalb mit vier Fehlern (`PvVorpruefung.Gesperrt`); der
  unmittelbar folgende Nachzug-Commit stellt den geprüften Sandbox-Stand her (Nachweis: `diff -rq`
  Sandbox gegen Export ohne Unterschied, Build und Tests des Exports grün, Abschnitt 5).
* Kein Push.

---

## 1. Verfahren — Sandbox zuerst, Hauptbaum zuletzt

Anders als bei den Merges 1–4 war die Berührungsfläche diesmal nicht klein: **20 Konfliktdateien**,
darunter zwölf Masken, die Remote stillgelegt hat, während sie hier geändert wurden. Deshalb
wurde der Merge **vollständig außerhalb des Hauptbaums** vorbereitet und geprüft:

1. `git merge-tree --write-tree HEAD origin/ios_migration` liefert den Merge-Baum mit Markern
   (`17acf45` gegen `d423fd4`, nach den Nachzügen der Gegenseite `a426406` gegen `91bac96` und
   `67b776e` gegen `4cdc462`).
2. Der Baum wurde nach `K:\merge5\src` exportiert (ohne Datenordner), die Konflikte per Skript
   aufgelöst (`aufloesen.py`), die WinForms-Deltas per Skript auf die Razor-Struktur portiert
   (`port1.py` … `port7.py`); jeder Anker trifft genau einmal, sonst schreibt das Skript nichts.
3. Dort: **zehn Builds** von `WP-Plan.sln`, `EPOS.UI.Tests`, `EPOS.Kern.Tests`, der Referenzlauf
   M5 und — als Gegenbeweis — Build, Tests und Referenzlauf des **reinen** Remote-Stands.
4. Erst dann im Hauptbaum: Sicherung, `git merge --no-ff --no-commit`, Löschungen annehmen, die
   Sandbox-Dateien einspielen, `git add` der Pfadliste, Commit — in einem Zug (`merge5_hauptbaum.sh`).

Die Gegenseite lief während der Vorbereitung weiter (`d423fd4` → `91bac96` → `4cdc462`, 107
Commits). Die Konfliktmenge blieb je Stand identisch; die Skripte wurden auf jeden neuen Baum
wiederholt. Mit dem dritten Stand kam die **Variantenkennzeichnung der Projektlisten** (W15a‑E‑1:
`ProjektKopfZeile.StammId/Bezeichner/StammName`, `ProjektListe.Gruppiert`, Spalte „Art") von
Remote — der eigene Nachbau derselben Sache (Gruppierung, Einrückung, Präfix) wurde gestrichen,
die Mehrfachauswahl setzt auf `StammId`/`IstVariante` von Remote auf.

---

## 2. Berührungsfläche und Auflösung

**Zwölf Masken: auf Remote gelöscht, hier geändert** (Modify/Delete). Die Löschung ist angenommen;
die lokalen Deltas sind in die Razor-Gegenstücke portiert (Abschnitt 4):

| Maske (lokales Delta) | gelöscht mit | Razor-Gegenstück |
|---|---|---|
| `Form_PV` (+300), `Form_AdminPV` (+243), `Form_CECImport` (+53) | W6.5, W14a, W13 | `PhotovoltaikDialog`, `ModulKatalogDialog`, `PvModulImportDialog` |
| `Form_ProjektDelete` (+112), `.Designer` (+139), `ProjektAuswahl` (+290) | W15a.2, W16a.5 | `ProjektWahlDialog`, `ProjektListe` |
| `Wizard_Projekt` (+141), `WizardParent` (+12) | W15a.6, W16a.5 | `ProjektKopfSeite`, `AssistentSeite` |
| `Form_ProjektExportImport` (8) | W15a.5 | `ProjektTransferDialog` (Varianten und Sicherung dort schon enthalten) |
| `Form_PhotovoltaikVerguetung` (+73) | W2.4 | `PhotovoltaikVerguetungDialog` |
| `Form_Simulation_Config.Karten` / `.Uebersicht` (+17 / +17) | W10b.1 | `SimulationKonfigHuelle`, `KlimaregionCtrl` |

Dazu entfernt, weil ohne Aufrufer: `Form_PVModell` (der WinForms-Wechselrichterdialog des
Pakets B), `Projektloeschwahl` (Kern) und `WinFormsNavigation.LoeschwahlUebernehmen`.

**Acht Inhaltskonflikte:**

| Datei | Auflösung |
|---|---|
| `Resource.resx`, `Resource.en-US.resx` | beide Schlüsselblöcke (31 lokal, 63 + 22 remote), keine Dublette; neu `CEC_MSG_KOPFZEILE`, `PVIMP_PLAUSI_TITEL`, `PVIMP_PLAUSI_FRAGE` |
| `SchemaMigration.cs` (9 Hunks) | Nummernkollision, Abschnitt 3; Remote-Benennung `FREEZE_VERSION`, `ZIEL_VERSION = SchemaStand.Zielversion` |
| `MenueCtrl.cs` | Remote-Löschweg (`ProjektWahlDialog` + `ProjektCtrl.LoeschenMitVorarbeiten`), dazu die lokalen Sicherungshelfer und der Mehrfachweg |
| `WinFormsNavigation.cs`, `HilfeKontext.cs` | Remote (`ProjektWahlHuelle`, `PvModulImportDialog`) |
| `CECDataService.cs` | Remote-Signatur (`CecFortschritt`); der lokale Pflichtspalten-Abbruch meldet als Schlüssel `CEC_MSG_KOPFZEILE`; der Datumsfix W13-B48 ist gleichwertig zum lokalen |
| `Proben/ZugriffsschichtProben/Program.cs` (8 Hunks) | Erwartungen relativ zu `ZIEL_VERSION`, dazu Schritt 62 (Waisen 0/0) und die Schritte 63/64; Fall 14 mit Wegwerf-Schritt `ZIEL_VERSION + 1` |

---

## 3. Die Nummernkollision am Schema

Lokal trug Paket A den Schritt **62** (PV-Anlagenparameter) und Paket B den Schritt **63**
(PV-Modellwahl), Zielversion 63. Remote trägt seit iU9‑W14c den Schritt **62 = Klimadaten-Waisen**
(Anwenderentscheid E‑6), Zielversion 62 — und die Zahl steht seither in `SchemaStand.Zielversion`
im Kern.

**Entscheid: Remote behält 62.** Die PV-Schritte heißen jetzt `SCHRITT_63_PV_ANLAGENPARAMETER`
und `SCHRITT_64_PV_MODELLWAHL` (Methoden `Schritt_63_PvAnlagenparameter`, `Schritt_64_PvModellwahl`,
Kataloge `Schritt63_PvAnlagenparameter`, `Schritt64_PvModellwahl`, `Schritt64_PvStammUndDegradation`),
`SchemaStand.Zielversion = 64`. **Neue Schritte ab 65.**

Warum das schmerzfrei ist: **Keine Anwenderdatenbank hatte die alten Nummern gefahren.** Die
Produktivdatenbank dieses Rechners stand am 05.09.2026 auf **61**, die PV-Spalten fehlten (die
Pakete liefen nur auf Referenzkopien); der andere Rechner steht nach W14c auf 62. Beide fahren
jetzt 63 und 64 nach; die PV-Schritte sind wiederholbar (`SqliteSpalteAnlegen` prüft das
Vorhandensein). Der Referenzlauf M5 belegt die Folge 61 → 62 → 63 → 64 auf einer frischen Kopie.

Nachgezogen: `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` (Nachtrag 3), `KatalogVerwaltungTests`
(fünfzehn PV-Felder), `KatalogImportTests` (vierzehn Importspalten: `Technologie`),
`ModulKatalogDialogTests`, `PhotovoltaikVerguetungDialogTests`, `PvModulImportDialogTests` und das
Test-Fixture `TestDatenbank` (Abschnitt 5).

---

## 4. Anpassung an die neue Struktur — die WinForms-Deltas in Razor

| Was lokal in WinForms stand | Wo es jetzt steht |
|---|---|
| Rechenmodell Einfach/Erweitert, WR-Wirkungsgrad, Systemverluste, Knopf „Wechselrichter…" mit `Form_PVModell` (Nennleistung, η10/50/100, DC/AC) | neuer Baustein **`PvModellFelder`** (Überlagerung statt Zweitfenster, Hausregel R2), Bündel **`PvModellTexte`** (füllt sich aus `MyResource`), `ErzeugerZeile` trägt die sieben Felder, `PhotovoltaikHuelle` schreibt sie in `WErzeugerModel` (Modellwahl wörtlich wie `Form_PV.UpdateProerties`) und liefert die kWp der Anlage; `PhotovoltaikDialog` bindet den Baustein in die Anlagengruppe ein |
| `Form_AdminPV`: NOCT-Feld, Zelltechnologie-Klappliste, Erhalt von `alpha_SC`/`beta_OC` beim Speichern | `ModulKatalogProfil` (PV): Felder `T_NOCT` und `TECHNOLOGIE` — dazu die neue Feldart **`BrowserFeldArt.Auswahl`** mit `Optionen` (Code ↔ Text), gerendert als `Auswahlfeld`; `PvAdminHuelle` liest/schreibt beide; `PhotovoltaikStammCtrl.SpeichernAus` trägt beim Update die gespeicherten Koeffizienten weiter |
| `Form_CECImport`: PAN-Koeffizienten statt „-", Technologie aus CEC/PAN, Plausibilitätsprüfung (Fehler sperren, Warnung fragt) | `UnifiedModule.AlphaSc/BetaOc` aus `muIscAK`/`muVocVK` (0 = nicht geführt = Strich), `NachModell` setzt `m_Technologie`; `PvModulImportHuelle.Vorpruefen` prüft mit `PvModulPlausibilitaet`, `PvVorpruefung` trägt Befund und Sperre, `PvModulImportDialog` sperrt bzw. fragt per `Rueckfrage` |
| `Form_PhotovoltaikVerguetung`: Degradationsfeld, Vorschau mit Eigenverbrauch/Strompreis | `PhotovoltaikVerguetungDialog` (Zahlenfeld in der Anlagengruppe, `PhotovoltaikVerguetungTexte`), `Erloes` ruft `PvErloesRechner.Rechne` mit Eigenverbrauch und Strompreis |
| `Form_Simulation_Config.Karten`: Chip „Modell erweitert · DC/AC" | `SimulationKonfigHuelle.PvDetailchips` |
| `Form_Simulation_Config.Uebersicht`: Erdreich-Temperaturen über den Ortszeit-Lesepfad (B1) | `KlimaregionCtrl.Aussentemperatur` liest über `SolardatenCtrl.ReadOrtszeit` |
| `ProjektAuswahl`: Varianten unter ihrem Stamm, Tooltip, waagerechter Bildlauf, Zählzeile | Varianten unter ihrem Stamm, Spalte „Art" und Herkunftszeile kamen mit W15a‑E‑1 von Remote (`ProjektKopfZeile.StammId`, `ProjektListe.Gruppiert`), der Rahmen mit Rollbalken mit W9‑B‑2; eigener Beitrag: `title` (Beschreibung) je Zeile |
| `Form_ProjektDelete`: Mehrfachauswahl mit Variantenkopplung, Alle/Keine, Zähler, Rückfrage mit Liste, Sicherungskopie | `ProjektListe.Mehrfach` (Häkchen, Kopplung Stamm → Varianten), `ProjektWahlDialog.Mehrfach` + `SicherungAngeboten` + `LoeschauftragErteilt` (Varianten VOR Stämmen), `ProjektWahlHuelle` → `Projektwahl.Mehrere`/`SicherungGewuenscht`, `MenueCtrl.MehrereLoeschen`; gespeicherte Ergebnisse gehen im Kern-Löschweg mit (`ErgebnisCtrl.Delete`) |
| `Wizard_Projekt`/`WizardParent`: Pflichtfelder, Namensdoppel, Klimaregion- und Bearbeiter-Vorbelegung, Prüfung beim Verlassen der Seite | `ProjektKopfRegeln` (Kern, eine Wahrheit), `ProjektKopfSeite` (Hinweis unter den Feldern, Pflichtmarke, Platzhalter), `AssistentSeite.SeitePruefen` (Veto vorwärts und beim Speichern), `ProjektKopfHuelle` (Vorbelegung, `VergebeneNamen`), `AssistentHuelle.SeitePruefen` |

Neue bunit-/xunit-Wachen: `PvModellFelderTests` (4), `PvKoeffizientenTests` (3), dazu je ein bis
zwei Fälle in `ProjektListeTests`, `ProjektWahlDialogTests`, `ProjektKopfSeiteTests`, `AssistentTests`.

**Hausregel-Hinweise:** `PhotovoltaikDialog` (439 → 451 Zeilen) und `PvModulImportDialog`
(> 700) lagen schon vorher über der 400-Zeilen-Grenze; die neue Logik steht in eigenen Bausteinen
bzw. Datenklassen. Das Stilblatt bekam fünf Regeln ohne Nesting (`StilblattTests` grün).

---

## 5. Nachweis

### 5.1 Build

MSBuild aus VS 18 Community, `-restore /p:Platform=x64 /p:Configuration=Debug`, `WP-Plan.sln`
(zwölf Projekte): **0 Fehler** — Merge-Stand (Sandbox, zehnter Build, und der Export des
Merge-Commits) wie reiner Remote-Stand. Ein Build des Hauptbaums nach `bin\x64\Debug` steht aus
(Visual Studio und die App waren offen).

### 5.2 Tests

| Lauf | Merge-Stand | reiner Remote-Stand `4cdc462` |
|---|---|---|
| `EPOS.UI.Tests` | **2 491 / 2 491** | 2 481 / 2 481 |
| `EPOS.Kern.Tests` | **1 077 / 1 077** | 1 074 / 1 074 |

> **Beobachtung, kein Befund am Merge:** In einem von drei Läufen der UI-Suite war
> `AppWurzelTests.Die_Wurzel_meldet_sich_als_Navigationsziel_an` rot — der Test prüft das
> statische `Navigationsziel.Aktuell`, das bei paralleler Ausführung eine andere gerade
> gezeichnete Wurzel tragen kann; isoliert und in der Wiederholung der ganzen Suite grün.

Voraussetzung beider Läufe: `Referenzlaeufe/Importproben` (von Remote) und
`Referenzlaeufe/Kenndaten_Test.sqlite` neben dem Quellbaum. **Befund am Fixture:** Die
Testdatenbank steht auf 61; seit Paket A/B schreibt der Kern die zehn PV-Spalten
(`WErzeugerCtrl`, `PhotovoltaikStammCtrl`, `ProjektPhotovoltaikCtrl`) — `AssistentCtrlTests`
scheiterten an „no column named PV_WrWirkungsgrad". `TestDatenbank` zieht die Spalten der
Schritte 63/64 jetzt aus `SchemaKatalog` nach (ADD COLUMN wie `SqliteSpalteAnlegen`, kein DML)
und setzt den Marker; die Migration selbst bleibt im Anwendungsprojekt.

### 5.3 Referenzlauf — der Dreifach-Nachweis

Werkzeug `Referenzlauf/`, 14 Projekte, Quelle `P:\pa0\Quelle\Kenndaten.sqlite`
(MD5 `47bcefaca0f18d2180ba37786c6cb6b3`), je frische Arbeitskopie; die des Merge-Laufs migriert
**61 → 62 → 63 → 64** (Schritt 62: Waisen 0/0), die des THEIRS-Laufs 61 → 62.

| Vergleich | Ergebnis |
|---|---|
| **MERGE (M5) gegen M4** (`2026-09-05_M5_nach-Merge5` ↔ `2026-09-03_M4_nach-Merge4`) | **355/355 byte-/MD5-gleich**, Toleranzvergleich **14/14 PASS** (3 882 476 Werte) |
| **THEIRS (`4cdc462`) gegen THEIRS (`b0d3d86`)** (`K:\merge5\ref\THEIRS2` ↔ `P:\merge4\theirs_lauf`) | **14/14 PASS** (ebenso `91bac96`) |
| THEIRS gegen MERGE | FAIL, erwartet: die Temperaturreihen (`stundentemperatur`, `wp_quellentemperatur`) tragen die Ortszeit-Zeitbasis des Pakets A (B1) — dieselbe Abweichung wie PA0 → PA1 |
| `pruefen` auf M5 | **plausibel**, dieselben Bestandshinweise wie M1–M4 |

Beide Achsen sind exakt: Die 555 Remote-Commits ändern den Rechenweg nicht, und die
Zusammenführung samt Portierung hat nichts verschoben. Der Lauf ist auf jedem der drei
Remote-Stände (`d423fd4`, `91bac96`, `4cdc462`) gefahren worden — dreimal 355/355.

---

## 6. Offene Punkte

* **Hauptbaum-Build nach `bin\x64\Debug` ist nicht gelaufen** — Visual Studio und die App waren
  während des Merges offen; der Nachweis stammt aus dem Sandbox-Build desselben Stands.
* **Prüfstand `K:\kd1runner`** referenziert gelöschte Masken (`Form_ProjektDelete`, `ProjektAuswahl`,
  `WizardParent`, Dump-Modi) und muss auf `EPOS.Kern`/`EPOS.UI` umgestellt werden; die
  Harness-Migrationsprobe der Parallelsession (`P:\…`, erwartet 61 → 62 → 63) braucht das Ziel 64.
* **Sichtabnahmen** der portierten Stellen am Gerät (PV-Modellfelder, Modulkatalog NOCT/Technologie,
  Import-Plausibilität, Degradation, Simulationschip, Mehrfachlöschen, Pflichtfelder).
* **Konzept Projektstammdaten** (PS5 Speichern-unter): `Form_ProjektSpeichernUnter` ist mit W15a
  gefallen — Ziel ist `ProjektKopieDialog.razor`.
* **Push** wartet auf das Wort des Anwenders.

---

## 7. Merge 6 (05.09.2026, abends) — der Nachschub nach Merge 5

Während der Nachweise zu Merge 5 lief die Gegenseite weiter: `4cdc462` → `ed71d73`, **drei**
Commits, **vier** Dateien — Befund W12‑B‑1 (die Knopfleiste `.epos-leiste` bricht um, `.epos-knopf`
bemisst sich an seiner Beschriftung), die Wache `ZweispaltenauswahlTests` und zwei Protokolle.

* **Merge-Base:** `4cdc462` (der Remote-Elter von Merge 5)
* **Lokal vorher:** `a98cde5` (Merge 5 samt Nachweis und Nachzug, 26 Commits)
* **Merge-Commit:** `328676d5`; Sicherung `sicherung/vor-merge6-2026-09-05`, Anker `merge6/ios-2026-09-05`
* Kein Push.

**Berührungsfläche:** nur `EPOS.UI/wwwroot/epos-ui.css` — Remote ändert die Regeln der Knopfleiste,
Merge 5 hatte am Blattende sechs Regeln angehängt (Mehrfachauswahl, Projektkopf-Hinweis,
Rückfragetext). `git merge-tree` vereinigt beides ohne Konflikt; Klammerbilanz 717/717.

**Nachweis** (Export des Merge-Baums `d98b097`, dasselbe Verfahren wie Abschnitt 1): `WP-Plan.sln`
**0 Fehler**; `EPOS.UI.Tests` und `EPOS.Kern.Tests` grün (Zahlen im Abschluss des Merge-Commits);
Referenzlauf **355/355 byte-gleich** zur Basis `2026-09-05_M5_nach-Merge5` — erwartungsgemäß, denn
kein Rechenweg ist berührt. Die Basis bleibt deshalb M5; ein eigener Referenzordner wäre eine Kopie.

Der Merge selbst lief im Hauptbaum direkt (`git merge --no-ff ed71d738`), ohne Kopierliste.

---

## 8. Die Testdatenbank auf Stand 64 (05.09.2026, Aufgabe 92)

Der Befund aus Abschnitt 5.2 hatte eine zweite Seite, die erst der SQL-Dialektprüfer sichtbar
machte: `Referenzlaeufe/Kenndaten_Test.sqlite` stand auf dem **Freeze-Stand 61**, der Quelltext
seit Merge 5 auf **64**. Die Kern-Tests waren grün, weil `TestDatenbank` die zehn PV-Spalten auf
ihrer Arbeitskopie nachzieht — die eingecheckte Datei selbst blieb alt. Der Prüfer hält seine
`EXPLAIN` aber gegen genau diese Datei und meldete deshalb **zehn Fundstellen** (Gate-Regel: 0):

| Fundstellen | Ursache |
|---|---|
| `KomponentenUebernahmeCtrl.cs:410`, `WErzeugerCtrl.cs:112` | `PV_WrWirkungsgrad` (Schritt 63) fehlte in `Tab_Energieanlagen` |
| `PhotovoltaikCtrl.cs:118/291`, `PhotovoltaikStammCtrl.cs:106/237/282` | `Technologie` (Schritt 64) fehlte in `Tab_PV` / `Tab_PV_STAMM` |
| `ProjektPhotovoltaikCtrl.cs:84/97` | `Degradation` (Schritt 64) fehlte in `Tab_ProjektPhotovoltaik` |
| `WizardCtrl.cs:1032` | **kein Schemafehler** — siehe unten |

**Die Datei steht jetzt auf 64.** Nachgezogen wurde sie nicht von Hand, sondern mit dem neuen
Werkzeug `Werkzeuge/Testdatenbankschema`, das dieselben Quellen fährt wie `SchemaMigration`:
den Spaltenkatalog (`SchemaKatalog.Schritt63_PvAnlagenparameter`,
`Schritt64_PvModellwahl`, `Schritt64_PvStammUndDegradation`), die Typübersetzung
(`StilleDb.SqliteSpaltenTyp`, für Schritt 63 wie dort `REAL` ausgeschrieben) und für Schritt 62
die zwei `DELETE`-Texte aus `KlimaWaisenBereinigung`. Es legt keine DDL aus zweiter Hand an, ist
idempotent (zweiter Lauf: 0 Spalten) und läuft auf Linux — die Migration selbst bleibt im
Anwendungsprojekt und ist dort unerreichbar. Aufruf und Wiederholung stehen in
[`BETRIEB_SQLITE.md`](../../../BETRIEB_SQLITE.md) Abschnitt 6.5.

* **Schritt 62** (Klimawaisen): 0 Waisen in `Tab_Solar_STAMM` und `Tab_Klimadaten_STAMM` — der
  dokumentierte Leerlauf, gemessen statt geglaubt.
* **Schritt 63/64:** zehn Spalten angelegt, kein DML — beide Schritte sind ergebnisneutral.
* **Größe:** 77 000 704 → **68 157 440 Byte** (73,4 → 65,0 MB). Das `VACUUM` am Ende gibt mehr
  frei, als die zehn Spalten kosten; die Datei wird also **kleiner**.

**Der Nachweis der Ergebnisneutralität** ist der Referenzlauf **vor und nach** dem Nachziehen:
`EPOS.Referenzlauf lauf --projekte 1030,1007,1017` gegen dieselbe Datei, `diff -r` über beide
Zielordner. Alle **72 CSV byte-gleich**; abweichend nur `protokoll.txt` in Zeitstempel, Zielordner,
Dateigröße und Laufdauer. Damit ist belegt, was die Schrittbeschreibungen zusagen: Die Migration
verschiebt keinen Rechenwert.

### `WizardCtrl.cs:1032` — kein Schemafehler, eine Lücke im Leser des Prüfers

`WizardCtrl.FachspaltenSelect` baut `SELECT ID, ID_Type, Bezeichner` und hängt in einer Schleife
die Fachspalten an; das `FROM Tab_Energieanlagen WHERE ID_Projekt = ?` kommt erst im `return`
dazu. Der Prüfer sieht nur die Spaltenliste — und **ohne Tabellenbezug scheitert jeder
Spaltenname, auch der richtige**: `EXPLAIN SELECT ID, …` meldet „no such column: ID", während
dieselbe Anweisung **mit** `FROM` anstandslos durchläuft. `ID`, `ID_Type` und `Bezeichner` stehen
alle drei in `Tab_Energieanlagen`.

Berichtigt wurde deshalb der **Prüfer**, nicht die Anweisung: `_spaltenliste_ohne_tabelle` nimmt
einen dynamischen Text von der Objektprüfung aus, wenn er ein `SELECT` **ohne jedes `FROM`** ist
und die Meldung eine fehlende **Spalte** nennt. Eng gehalten und gegengeprüft — mit `FROM`, bei
fehlender **Tabelle**, bei `UPDATE` und bei vollständigen Anweisungen bleibt es ein Fund.
Selbsttest weiter 32/0.

**Stand nach der Aufgabe:** Prüfer **0 Fundstellen** (1 206 Texte, 186 dynamisch, 1 020 in
Ordnung), `EPOS.Kern.Tests` **1 168/1 168** grün (auch unter `LANG=en_US.UTF-8`), beide
Kern-Wächter leer.
