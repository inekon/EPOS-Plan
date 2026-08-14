# Änderungsvorschlag Fassung 12 — Konzept Quellen/Senken

**Stand:** 14.08.2026 · Bezug: `Konzept_Simulation_QuellenSenken.md` Fassung 11,
[`ADR-001_Schema-Ausrollung.md`](ADR-001_Schema-Ausrollung.md) (angenommen 14.08.2026)

**Prüfmethodik:** Vollverifikation der Fassung 11 gegen den Code-Stand 14.08.2026 —
49 Einzelaussagen durch fünf unabhängige Prüfläufe, jede gemeldete Abweichung
adversarial gegengeprüft. Ergebnis: **42 von 49 Aussagen unverändert zutreffend,
7 bestätigte Abweichungen**, dazu Zusatzbefunde. Das Konzept ist als
Umsetzungsgrundlage weiterhin tragfähig; die Änderungen sind Nachführungen, keine
Neukonzeption. Umsetzungsstand des Konzepts im Code: **0 %** (keine der neuen
Spalten, Dateien oder Engine-Änderungen existiert; alle zehn B0-Fehler sind
unverändert vorhanden).

---

## Teil A — Architekturänderung (ADR-001, verbindlich)

Der Ordner `Allgemein/Update/` mitsamt `UpdateDatabaseFromScript.cs` wurde
**absichtlich gelöscht** (bestätigt 14.08.2026). Zusätzlich gilt: Das externe
DB-Migrationswerkzeug (`DB_Migration`) und das dynamisch erzeugte
`migration*.sql` sind **grundsätzlich nicht Teil der Anwendungsarchitektur** —
das Skript wird bei jeder Migration neu generiert. An dessen Stelle tritt die
versionierte In-Code-Migration mit `SchemaVersion`-Marker in `Tab_Applikation`
(ADR-001).

| # | Stelle | Änderung |
|---|---|---|
| A1 | **Kapitel 5.6** | Absätze zu `UpdateDatabaseFromScript` (Pfad-Vorbehalt, fehlender Aufrufer) und die Randnotiz zu den CLAUDE.md-Skript-Sektionen **ersatzlos streichen**; stattdessen Verweis auf ADR-001. Die beiden `SchemaSicherstellen`-Einschränkungen (hartkodierte Tabelle, verschluckte Fehler) bleiben als Umsetzungsdetails — sie werden im Zuge von ADR-Aufgabe 4 behoben. Zahl korrigieren: nicht „13 neue Spalten", sondern **24 in vier Tabellen** (15 `Tab_Energieanlagen`, 7 `Tab_Pufferspeicher`, 1 `Tab_Klimaregion`, 1 `Tab_Einstellungen`, siehe Kapitel 12) |
| A2 | **Kapitel 6.6** | Den `SQL=`-Skriptblock ersetzen durch: „`Tab_ErgebnisPufferspeicher` entsteht in Migrationsschritt 3 der `SchemaMigration` (ADR-001)." **Korrektur der Absolutaussage** „ein `CREATE TABLE` existiert nirgends im Repository" — falsch: 9 Vorkommen in 4 Dateien (`WirtschaftlichkeitCtrl.cs:72–190` über einen `Ddl()`-Helfer, `EmissionsBilanzRechner.cs:47`, `BerichtCtrl.cs:150`, `VariantenCtrl.cs:245`); nur `ErgebnisCtrl` selbst hat keins. **Das ist eine gute Nachricht:** Es gibt ein erprobtes In-Code-Muster für `CREATE TABLE`, das `StellePufferTabelleSicher()` als Vorbild dient. Zeilenreferenzen nachziehen: `Save` `:66`, DELETE `:85` (Access-Syntax `DELETE ID_Projekt FROM …`), Kaskaden-Kommentar `:83-84`, `StelleEnergieSpaltenSicher` `:690`, `StelleBHKWSpaltenSicher` `:716`; **neu erwähnen:** dritte Methode `StelleModulSpaltenSicher` (`:745`) — alle drei laufen bei **jedem** Save (kein `_schemaGeprueft`-Flag) mit leerem `catch` |
| A3 | **Kapitel 13.2** | Komplett ersetzen durch Verweis auf ADR-001 (DataRepository ist einziger DB-Pfad; O6 gegenstandslos). E12-Zeile in Kapitel 1 und O6-Zeile in Kapitel 10 entsprechend anpassen |
| A4 | **Kapitel 7, Tabelle** | Zeile `Allgemein/Update/MigrationQuellenSenken.cs` ersetzen durch `Allgemein/Update/SchemaMigration.cs` (Mechanismus nach ADR-001; Migration 5.5 = Schritt 5). Neue Zeile: `Controller/ApplikationCtrl.cs` (ändern — `SchemaVersion` lesen/schreiben) |
| A5 | **Kapitel 5.5** | Kopfsatz ergänzen: Die Migration läuft als Schritt 5 der `SchemaMigration`, genau einmal je Datenbank (Run-once über `SchemaVersion`), nicht mehr „einmalig je Projekt" über eine Heuristik |
| A6 | **Kapitel 9** | Paket 1 um den Mechanismus erweitern: +1,5–2 PT (`SchemaMigration`, `SchemaVersion`, Startsequenz in `Program.Main`, Startblockade). Neue Paketsumme ausweisen |
| A7 | **Fassungshistorie** | Zeile Fassung 12 ergänzen; Grundsatz festhalten: DB-Versionsmigration (`DB_Migration`, `migration*.sql`) ist außerhalb der Architektur, das Skript wird dynamisch erzeugt und ist nie Referenz |

## Teil B — Bestätigte Abweichungen Konzept ↔ Code

| # | Stelle | Befund und Änderung |
|---|---|---|
| B1 | **13.4 MessageBox-Inventar** | „Heute brechen **sechs** Stellen diese Zusage" ist falsch — es sind mindestens **acht**: zusätzlich `SimulationStrombedarf.cs:68` („Fehler bei der Berechnung der Stromprofile!") und `:207` („Fehler in Simulation!"), beide im Headless-Pfad (`SimulationRunner.cs:75`). Tabelle um beide Zeilen ergänzen (Behandlung: Fehler → Protokoll, Lauf abbrechen). **Gravierender:** `DataRepository` selbst zeigt bei jedem DB-Fehler eine MessageBox (`:44, :67, :94, :123, :155, :226`) — die Simulationsklassen gehen durchgängig über DataRepository. Der Protokollkanal löst den Headless-Lauf nur, wenn dieser Pfad mitbehandelt wird (z. B. Engine-Modus: Exception/Fehlercode statt Dialog). Neuer Absatz in 13.4, Paket 8 um ~0,5 PT erhöhen |
| B2 | **13.6 + Kapitel 11: Ressourcenlage** | „Es gibt nur `Resource.resx` und `Resource.en-US.resx`; jeder Schlüssel ist an genau zwei Stellen zu pflegen" ist überholt: **`MyResource/Resource.de-DE.resx` existiert** (21 Einträge, inhaltlich redundant zur neutralen Datei). Empfehlung in 13.6 aufnehmen: de-DE-Satellit **löschen** (Deutsch ist Fallback-Kultur), ebenso die 0-Byte-Datei `Resource.en-US.Designer.cs` — dann stimmt die Zwei-Stellen-Regel wieder |
| B3 | **13.6 + L1: Formular-Satelliten** | „dort fehlen nur die englischen Satelliten (23 Texte)" ist überholt: `Form_Simulation_Config.en-US.resx` (65 echte Einträge, echtes Englisch) und `Form_KonfigPufferspeicher.en-US.resx` (10 Einträge) **existieren**. Abdeckung: 65 von 298 bzw. 10 von 98 Einträgen der neutralen `.resx` — **unvollständig, nicht fehlend**. L1 umformulieren: „Vervollständigen der vorhandenen en-US-Satelliten"; dabei die de-DE-Satelliten bereinigen (`Form_Simulation_Config.de-DE.resx` enthält nur 2 Layout-Einträge, die Position/Größe eines Buttons kulturabhängig überschreiben) |
| B4 | **2.2 Punkt 4** | „`Form_KonfigPufferspeicher` erhält die Liste von `:1474`" ist unpräzise: `:1474` füllt nur das Feld `listPufferSp` für die Rubrik-ComboBox. Die Übergabe an den Dialog passiert in `btn_Hinzu_Click` (`:1617-1621`) und **gefiltert**: erst `AktivePufferSp()`, die volle STAMM-Liste nur als Fallback, wenn keine Puffer-Checkbox aktiv ist |
| B5 | **2.1** | „beide vollständig programmatisch ohne Designer/`.resx`" gilt uneingeschränkt nur für `Form_QuellePufferspeicher`. `Form_Quellprofil` hat keinen Designer, aber eine **`Form_Quellprofil.resx`** (5817 B, inhaltlich leeres VS-Gerüst, per SDK-Glob als EmbeddedResource eingebunden, wirkungslos). Präzisieren; Randnotiz: der Code-Kommentar `Form_Quellprofil.cs:20-21` („keine .resx") ist ebenfalls falsch, Datei bei Gelegenheit löschen |
| B6 | **2.3 letzter Punkt + 5.2** | „Beide Speicherpfade löschen alle Anlagen des Projekts; nach jedem Speichern haben **alle** Anlagen neue IDs" — gilt nur für den Wizard-Pfad (`WizardParent.cs:661`, parameterlose Überladung). Das Kontextmenü (`PufferSpKontextMenuCtrl.cs:133`) nutzt die **typgefilterte** Überladung (`WizardCtrl.cs:32-36`, `AND ID_Type = ?`) und erneuert nur die Puffer-Zeilen. **Folge unverändert:** Puffer-Anlagen-IDs sind auf beiden Pfaden instabil — die E7-Begründung bleibt vollständig gültig, nur die Pauschalaussage ist zu korrigieren |
| B7 | **13.3, Tabellenzeile `:1241-1244`** | „gefüllt aus dem Legacy-Ausdruck Volumen·1,16" gilt nur für den else-Zweig ohne Pufferzuordnung; mit Puffer zeigt die Box `Q_max` + Bezeichner. Der eigentliche Fehler liegt im **Runner** (`:138`): `Volumen_Pufferspeicher * 1.16` ohne ΔT und ohne /1000, Volumen aus dem **WP-Datensatz** statt aus dem zugeordneten Puffer — der in `Tab_Ergebnis` gespeicherte Wert weicht bei jedem Projekt mit Puffer von der Anzeige ab. Zeile präzisieren (6.6 beschreibt es bereits korrekt) |

## Teil C — Ergänzungen aus neuen Befunden

| # | Stelle | Ergänzung |
|---|---|---|
| C1 | **6.2 Speicher-Registry** | Ergänzen: Quellseitig existiert bereits eine Speicherliste — `List<SimulationPufferspeicher> wp_quellspeicher` je WP-Modul (`SimulationWaermepumpe.cs:49`, befüllt über `WaermequelleClass.Quellspeicher()`). Die Registry muss diese Instanzen **übernehmen oder ablösen**, sonst entstehen zwei parallele Speicherverwaltungen |
| C2 | **B0-7 erweitern** | Zweite Diskrepanz derselben Art: Der **Deckungsgrad** wird doppelt und widersprüchlich gerechnet — `SimulationRunner.cs:148-153` produktionsbasiert (landet in `Tab_Ergebnis` → Bericht/Wirtschaftlichkeit), `Form_Simulation_Detail.cs:1223-1227` restbedarfsbasiert (genau die Formel, die der Kommentar dort als „mit Pufferspeicher ungenau" verwirft). Beide kappen bei 100 (Detail zusätzlich bei 0). Zudem zieht `SimulationRunner.cs:137` eine **Strommenge** (`Stromverbrauch_Heizstab`) von einer Wärmemenge ab |
| C3 | **Neu B0-11 (Vorschlag)** | Rückwärts-Mapping Anzeigename → DB-Wert beim Speichern der Zuordnungen: `Form_Simulation_Config.cs:1548-1549` — findet der lokalisierte Anzeigetext keinen Treffer in der LanguageItem-Liste, landet der **Anzeigename** als `Erzeuger` in `Z_ProjektPufferSp`; die Leseseite sucht hart nach `Erzeuger='Wärmepumpe'`. Sprachwechsel zwischen Anlegen und Lesen macht die Zuordnung wirkungslos. Gleiche Familie wie B0-9/B0-10 (Anzeigetext als Steuerwert); wird mit 5.4 obsolet, wirkt aber heute |
| C4 | **Kapitel 8, „Weitere Befunde"-Absatz** | Ergänzen: (a) `KlimaregionCtrl` — `Add()`, `Update()`, `Delete()` schreiben auf Spaltennamen (`Name`, `Längengrad`, …), die die Leseseite (`Bezeichner`, `Longitude`, …) nicht kennt, und widersprechen sich untereinander; `Update()`/`Delete()` müssen zur Laufzeit auf OleDb-Fehler laufen. **Relevant für Paket 3**, weil 13.1 `Tab_Klimaregion` um `Klimazone_DIN4710` erweitert. (b) `SimulationSPK.cs:140/:210` — `Max_Waermebedarf` wird by-value übergeben, das Feld bleibt immer 0. (c) `SimulationSPK.cs:163` — `Stromverbrauch_stuendlich = Kesselleistung_stuendlich` weist die **Referenz** zu (Aliasing wie B0-2), bei Brennstoffart 13 zeigen Strom- und Wärmeganglinie auf dieselbe Instanz. (d) `SimulationControl.cs:211` überschreibt `Reststrom` — die Additionen `:114/:138-139` sind wirkungslos (toter Code). (e) `SimulationWaermepumpe.cs:296-299` — deckt der Puffer die Stunde voll, verlässt `break` die Modulschleife: Quellspeicher bekommen in diesen Stunden weder Regeneration noch `StundeAbschliessen`, ihre SOC-Ganglinie bleibt 0 (verschärft den bestehenden Quellbilanz-Punkt in 6.3). (f) Das stille ID-Reparaturmuster aus 2.3 (`if (idX > 0)`) betrifft **alle Gewerke** in `Add_WP_Waermeerzeuger`, nicht nur den Puffer |
| C5 | **2.3 Tabelle: dritter Entstehungsweg** | `Form_Start.cs:1580-1605` (`pBox_Pufferspeicher_Click`) ist ein dritter Speicherpfad für Puffer-Anlagen: gleiches Del+Add wie das Kontextmenü (typgefiltert), überträgt aber als **einziger** Pfad die vollständigen Modelle (inkl. Vorlauf/Rücklauf/Volumen). Die Aussage „die erzeugenden Stellen setzen nur vier Felder" gilt für diesen Pfad nicht |
| C6 | **2.2 Punkt 6 / Kapitel 9** | Verschärfung der WW-Aussage: `SimulationWaermepumpe.cs:238-243` kappt den vollen Brauchwasservektor auf den Restbedarf — steht die WP **nicht** an erster Kaskadenposition, wird der gesamte Rest als Warmwasser klassifiziert (`rest_heiz = 0`); WP-Module mit `WS_Typ='Heizung'` bleiben dann systematisch aus. Das ist stärker als die bisherige Formulierung „WW-Deckung überschätzt" |
| C7 | **4.1 Layoutzwang** | Herleitung ersetzen: Die Übersicht misst nicht fest 491×91 px, sondern wird dynamisch berechnet (`groupBox_Uebersicht.Height = groupBox_PufferSp.Top − 109 − 10`, `:194`; vierseitig verankert `:210`). Die Schlussfolgerung — Übersichtsumbau und Rubrik-Entfall sind nicht getrennt planbar — **bleibt**, weil die Höhe an der Position der Puffer-Rubrik hängt. Ergänzen: Es gibt einen **zweiten** Satz hartkodierter Spaltenindizes an `listView1` (`:392-427` gegen `:81-85`) — er entfällt mit der Rubrik (Etappe B), bis dahin bei Änderungen mitdenken |
| C8 | **B0-10 präzisieren** | Der SQL-Syntaxfehler entsteht nicht durch ein leeres `where`, sondern im else-Zweig: der erste Filterteil ist nie leer, ohne Volumen-Treffer entsteht `… where <filter> and  order by …` (`Form_PufferSp.cs:198`, `Form_PufferSp_Admin.cs:74`) |

## Teil D — Kleinkorrekturen und Status

| # | Änderung |
|---|---|
| D1 | **Kopfzeile:** „Fassung 12 · Stand 14.08.2026 · verifiziert am Code-Stand 14.08.2026 · Umsetzungsstand 0 % · Bezug: ADR-001 (angenommen)" |
| D2 | **Bezugskonzept TWW:** `Konzept_TWW-Zapfprofile_WP-Plan_1.md` liegt doppelt vor (Repo-Wurzel und `Allgemein/Waermespeicher/`) — maßgebliche Fassung im Kopf benennen |
| D3 | **Registry-Pfad:** `Program.cs:45/:48` verwendet `@"Software\\wp-plan"` — im Verbatim-String ist das ein **literaler Doppel-Backslash** im Schlüsselnamen; Konzept und CLAUDE.md schreiben `Software\wp-plan`. Achtung bei Paket 9: nicht „korrigieren", sonst verlieren Bestandsinstallationen ihre Spracheinstellung (oder Migration vorsehen) |
| D4 | **Zeilendrift-Hinweise** (nur nachziehen, keine inhaltliche Änderung): `NavigatorWaerme` SOC-Zuweisung `:116-118`; Serie bei `:146`; `SimulationRunner` `Min_Spitzenkesselleistung` `:143-146`; `WizardCtrl` Puffer-Zweig `:162-166`; `Form_PufferSp` STAMM-ID `:101`; `SimulationWaermepumpe` Ganglinienfüllung `:589` (B0-7-Referenz `:603-604` betrifft korrekt nur die Summe); `Form_PufferSp_einlesen.cs` Umlautdefekt bei `:119`, nicht `~:44` |
| D5 | **13.7 Nebenbefund CLAUDE.md:** erledigt markieren — beide CLAUDE.md nennen `WP-Plan.sln` und Git inzwischen korrekt |
| D6 | **Bestätigt ohne Änderungsbedarf** (Auswahl): alle zehn B0-Fehler unverändert vorhanden; `Tab_Einstellungen`-Ordinal-Lesung `row[0..22]` exakt; genau 5 Dezimalkomma-Vorgaben (13.6-Zählung stimmt); genau 7 `MyResource`-Schlüssel; die drei duplizierten LanguageItem-Listen (4/5/5 Einträge, dritte in der Schleife allokiert); `Form_Simulation_Config.cs` exakt 1704 Zeilen; `PUFFER_ITEM=13` unerreichbar; `KonfigurationCtrl`-Kette Erdreichdaten (`Stundentemperatur` über `RecordSet` aus `Tab_Solar`) wie beschrieben; MathNet.Numerics 5.0.0 referenziert |

## Auswirkung auf die Pakete (Kapitel 9)

| Paket | Änderung | Delta |
|---|---|---|
| 1 Schema + Migration | + `SchemaMigration`-Mechanismus (ADR-001) | **+1,5–2 PT** |
| 3 Erdreichmodell | + `KlimaregionCtrl`-Reparatur vor Erweiterung von `Tab_Klimaregion` (C4a) | +0,5 PT |
| 8 Engine-Protokoll | + DataRepository-Fehlerpfad (B1) | +0,5 PT |
| 9 Lokalisierung | L1 wird „Vervollständigen" statt „Neuanlage" (B3); Ressourcen-Bereinigung (B2) | ±0 (Umbuchung) |
| B0 | + B0-11 (C3), B0-7-Erweiterung (C2), Präzisierung B0-10 (C8) | +0,5 PT |
| **Summe** | 61–78 → **≈ 64–81,5 PT** | |
