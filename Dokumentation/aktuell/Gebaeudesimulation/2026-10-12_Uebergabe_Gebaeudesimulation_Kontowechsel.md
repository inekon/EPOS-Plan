# Übergabe der Sitzung „Gebäudesimulation“ — Stand 16.10.2026, 00:30 UTC

Dieses Papier hält, was die fortführende Cloud-Sitzung braucht, um die Sitzung „Gebäudesimulation“ ohne Verlust weiterzuführen:
Stand, laufende und nächste Arbeit, offene Anwenderpunkte, Arbeitsweise und am Ende den Einstiegs-Prompt. Regelquelle bleibt
[`CLAUDE.md`](../../../CLAUDE.md) mit den Projekt-`CLAUDE.md`; der dauerhafte Stand steht in der
[Statusdatei iOS-Migration](../Status_iOS_Migration.md) (eine Zeile je Schritt) und in der
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md) (Entscheidungsregister bis E149, Stufentabelle).
Es löst die [Übergabe vom 08.10.2026](../../ueberholt/2026-10-08_Uebergabe_Gebaeudesimulation_Kontowechsel.md) ab und wandert
nach `Dokumentation/ueberholt/`, sobald die Runde 231/232 gepusht und die Sichtabnahme samt zweitem Importlauf erledigt ist.

Das Papier führt den gültigen Stand und wird bei jedem Push nachgeführt.

## 1. Stand

| Was | Wert |
|---|---|
| Arbeitszweig | `ios_migration_september` (Remote `origin`), Stand der Wellen vom 16.10.2026: `3a8f1bcb0` |
| Sitzungen | fortführend `session_01E52sMc6SB7AA3KC4X4AMMG` (Sitzungszweig `claude/wiki-help-assistant-docs-jllq1r`); `session_01NPHEwQvnQDiWMWnQbMmEou` ist stillgelegt |
| Fertige Wellen auf origin | als Zweige: `claude/gebaeudesimulation-da6` (`fdb7c7072`, DA-6, Schritt 231) und `claude/gebaeudesimulation-bk5` (`9407ae602`, BK-5/BK-6, Schritt 232); ihre Kern-Läufe sind gegen die ungehobene Testdatenbank rot und ohne Bedeutung |
| Laufende Wellen | **VS** (Zweig `vs`: Farbmodus „Vollständigkeit“ und übrige „Befund“-Texte, E145), **N4** (Feiertagsperioden mit Wochenverweis, Gebäudefeld Maximaleraumtemperatur im Austausch), **N5** („Klimaregion wählen…“ im Gebäudedialog, Wochenvorschau nach Neulesen), **PAP** (Papierpflege, dieses Papier) |
| Referenzbasis | **R55** `Referenzlaeufe/2026-10-14_R55_KaeltebedarfDezentral`, 37 Projekte; nächste Basen R56 KB-5, R57 SA4; CI-Auswahl laut `CLAUDE.md` |
| Testdatenbank | Schemastand **228** (Git LFS) |
| Schemakette | 228 `GeraetekostenEntfallSchema` (KB-4, auf origin, #1095) → 229 `StromspeicherKostenSchema` (KB-5a, **nur im Zweig `kb5`, nicht auf origin**) → 230 `KaeltebedarfsprofilVorlagenSchema` (KBP-2, **noch nicht auf origin**) → 231 `RaumnutzungDinWerteSchema` (DA-6) → 232 `BauteilkatalogAbschnittSchema` (BK-5/BK-6); 231 und 232 warten auf 229/230; **233 `BauteilUHerkunftSchema`** (MA-6 (b), E149) angemeldet 16.10.2026 (`3a8f1bcb0`), Bau nach der Runde 231/232. Die Kette hängt immer über `+ 1` an der Vorgängerklasse |
| Statuszeilen | höchste vergebene #1095 laut Stand bei der Übergabe; vor jeder neuen Zeile Tabelle und Kopf auf Reservierungen anderer Sitzungen prüfen. Entscheide bis **E149**, **E150 frei** |
| Nachbarsitzungen | Kennungen stehen in den Commit-Trailern `Claude-Session` auf origin: Kältebedarf (KB-4/KB-5) `session_0178YLFEbS4xWdGaXFaZqG1r`, Dialoge und Korrekturen Cloud (KBP-2) `session_01Mt7MsJGzcGr77yqBsxwpLj`, Anlagendialog/ChartProben (R55) `session_01UNeGChCYZu2EpgFx4P3q1q`. Abfrage: `git log --format='%(trailers:key=Claude-Session,valueonly)' -n 80 origin/ios_migration_september | sort | uniq -c` |
| Lokale Sitzung | „Epos Lokal“ auf dem Rechner des Anwenders (Rechner, HSETU-Daten, Normen); der Anwender startet sie, Importlauf und DA-6-Lieferung sind geliefert |

## 2. Gebaute Wellen

Einzelheiten stehen in der Statuszeile und im Protokoll unter [`ueberholt/Protokolle/Gebaeudesimulation/`](../../ueberholt/Protokolle/Gebaeudesimulation/);
die Stufenübersicht in der [Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md), Abschnitt 2.

| Welle | Zeilen | Inhalt |
|---|---|---|
| UB, KM3, AH V0–V8, SA0–SA3 | bis #995 | Übergabegrenze, Teillast der Kältemaschine, Aufheizoptimierung Fassung 2, Schnellaufheizung (SA4 offen) |
| KS, SQ, SQ2 | #996 bis #1008 | Datum im Gemeinjahr; Projektdatei-Import |
| GD-1 bis GD-6a, GD-N | #1015 bis #1050 | Gebäudekopf mit Herkunft, Gebäudewerte der Projektdatei, automatische Platzierung, Vorbesetzung der Zonen, Mehrzonenübernahme, Simulation aus der Kopfleiste, Nachzüge |
| AB, AB2 bis AB4 | #1010 bis #1048 | Farbmodi, Zonenzuordnung über die Legende, Baustoffwahl, Ein- und Ausblenden |
| NP6, NP7 | #1011, #1022 | Normkategorien, Normwerte einlesen (Schritte 219, 220) |
| BK-1 bis BK-3d | #1023 bis #1094 | Editoren, Schritte 221/222, „Bauteilkatalog einlesen…“, Wiederimport in einem Lauf |
| MA-1 bis MA-7 | #1051 bis #1060 | Modellprüfung mit Ampel, Nutzung und Kalender, Hülle, Aufbauvorschläge, Ansicht als Werkzeug, Wiederimport bewahrt, Einzonen-Vorgaben |
| DA-1 bis DA-5, DA-7, DA-9 | #1049, #1076 bis #1085 | gbXML/IFC-Austausch aller Simulationsdaten, Profilprüfung, Round-Trip-Wache, Wiki-Quellen |
| R54 | #1084 | Basis Dachflächen (E141) |
| N1 bis N3, BK-3d | #1091 bis #1094 | Nachzüge Oberfläche und Kern |
| Wiki | #1061, #1085 | Wiki-Quellen und Logbuch-Entwurf; **kein Upload** |

## 3. Laufende Arbeit

**Zwei fertige, noch nicht gemergte Wellen** liegen als Zweige auf origin; Abholen: `git fetch origin claude/gebaeudesimulation-da6:da6 claude/gebaeudesimulation-bk5:bk5`.

- `da6` (`fdb7c7072`): DIN-Saat `RaumnutzungDinSaat`/`RaumnutzungDinWerteSchema`, NP7-Rücknahme auf die Saat, Auslieferungsvorlage 3d umgekehrt, E143 Wochenmuster; sitzt auf `bk5` Stand `39943d78b` und enthält eine eigene Reparatur der Schalen-Schrittliste.
- `bk5` (`9407ae602`): Abschnitte und Fenster, Katalogpaket, Tests; eigene Reparatur derselben Schalen-Schrittliste — Konflikt beim Zusammenführen erwartet (Opus).

Vorgehen, sobald 229 und 230 auf origin liegen: Opus-Runde in einem Worktree aus origin; `bk5` und `da6` mergen; Kette umhängen (`RaumnutzungDinWerteSchema.SCHRITT = KaeltebedarfsprofilVorlagenSchema.SCHRITT + 1`, `BauteilkatalogAbschnittSchema.SCHRITT = RaumnutzungDinWerteSchema.SCHRITT + 1`); Tests mit festen Nummern nachziehen (`BauteilaufbauAbschnittTests`, `BrennstoffpreiseTraegerkarteSchemaTests`, `RaumnutzungDinWerteSchemaTests` Kettentest, `GebaeudeG3SchemaTests` Spaltenliste, `KiMaskenabdeckungWacheTests`); Testdatenbank von der origin-Datei mit `Werkzeuge/Testdatenbankschema` auf 232 heben und mit LFS committen; `Referenzlaeufe/LIESMICH.md` Schemastand nachtragen; Nachgate mit Referenzlauf (byte-gleich erwartet); zwei Statuszeilen; Push. Danach Schritt 233 (MA-6 (b)).

Routinen (`send_later`) sind sitzungs- und kontogebunden; die Check-ins der stillgelegten Sitzung laufen nicht weiter, die fortführende richtet eigene ein.

Festlegungen, die Folgewellen kennen müssen:

- **Außenluft je Person** (NP7) ist eine eigene Spalte `Aussenluft_Person` (m³/(h·Person)) neben `Aussenluft`/`Aussenluft_Einheit`;
  im Generator geht die raum- oder flächenbezogene Außenluft vor.
- **Bauteilkatalog** (BK-2): Gruppe und Klasse sind Code-Spalten mit CHECK (Texte zweisprachig), Abschnitte stehen in
  `Tab_Bauteilabschnitt(_STAMM)`, das Kennzeichen `Simulation_Ungeeignet` setzt nur der Kern; importierte Sätze tragen
  Herkunft IMPORT, Quelle und Quellkennung und bleiben in der Auslieferung.
- **gbXML-Konditionierung (DA-1):** Herkunft `GbxmlDatei` in `SqprojKonditionierung` mit `NennwertDatei`/`AusGbxml`; Rangfolge Profil, dann je Größe die Datei; Schemabedarf für den Rundlauf (DA-2): `Tab_Zone.Aussenluft_Person`, Licht je Zone, optional `Personen_Waerme` — Nummer vor DA-2 anmelden (225 ist für DA-2 angemeldet; 223 Zapfprofilgenerator, 224 K6 Kältebedarf).
- **Modellprüfung (MA-1a):** Kernbaustein `Modellpruefung` mit `ModellpruefungCtrl`, fünf Gruppen, Rechenstatus; MA-1b baut die Oberfläche darauf.
- **Nutzung und Kalender (MA-2):** Kern `ZonenNutzungCtrl` mit fünf Schreibwegen für n Zonen in einem Vorgang und Sitzungsgedächtnis zum Zurücknehmen; Hülle `ZonenNutzungHuelle`, Blatt `ZonenNutzungsblatt.razor`, Parametersatz `ZonenNutzungGaben` am Gebäudedialog.
- **Einzonen-Vorgaben (MA-7):** Kern `Einzonenvorgaben` (Vorschau, Anwenden, Zurücknehmen, Sperre) über `GebaeudeVorgaben` und die Bauart-Tafel `BAUART_JE_KLASSE`; `GebaeudeKatalogDaten.KlasseGesetzt` hält eine beim Laden leere Baualtersklasse fest; das Profil der Gebäudeart läuft über die Naht `KonditionierungWeg.ProfilDerGebaeudeart`. Kein Schemaschritt, kein SQL.
- **Hülle geschlossen (MA-3):** Gruppe 1 der Modellprüfung prüft je Zone über `Zonenkopplungsregeln.Bilanz` mit den Codes `HUELLE_OBEN_FEHLT`/`HUELLE_UNTEN_FEHLT` (rot), `HUELLE_OFFEN` und `HUELLE_GESPIEGELT` (gelb); die gemeinsame Schwelle (10 %) steht in `Zonenkopplungsregeln.SenkrechtOffen`. `Vorschlagsart.Huellbauteil` (`Modellvorschlagsregeln.Huellbauteil`) schlägt das fehlende Bauteil an der Nachbarzone mit der Lückenfläche vor, Übernahme und Rücknahme über `ModellpruefungCtrl`, Einfügen über `GebaeudeZonenCtrl.BauteilEinfuegen`. Kein Schemaschritt, kein Eingriff in den Rechenweg.
- **Aufbauvorschläge (MA-4):** Kern `Aufbaugruppenabgleich` (Synonymtafel, vier Typgruppen, N4/N5, längstes Synonym gewinnt) ordnet Bauteil- und Aufbaunamen einer Typgruppe zu; Rangfolge Projektaufbau, Katalogaufbau im U-Band ±25 % (Typgruppe, Aufbauklasse, U-Nähe, Katalogfolge), Typaufbau; in Stufe C liefert die Baualtersklasse über `GebaeudeVorgaben.Wert` das Ziel-U, `Pruefvorschlag.UBauteil` trägt es, `ModellpruefungCtrl.AufbauAnwenden` schreibt U und Aufbau in einem UPDATE, die Rücknahme stellt beide zurück. Keine Oberfläche, kein Schemaschritt.
- **Ansicht als Werkzeug (MA-5):** `Modellpruefungsblatt.AnsichtGruppe` reicht die Gruppe an den Gebäudedialog, der die Ansicht im Farbmodus der Gruppe öffnet und `GebaeudeAnsicht.Pruefung` setzt (Befundzeilen mit `IdZone`/`IdBauteil`; Bauteile über `Tab_Importzuordnung`, Zonen über den Namen). `ModellpruefungCtrl.Uebernehmen(gruppe, auswahl)` und `Verwerfen(gruppe, auswahl)` arbeiten je Auswahl (Verwerfen nur im Sitzungsgedächtnis, schreibt nichts); Neulesen über `GebaeudeNeulesestand.AnsichtNeuLesen`; der Farbmodus „Nutzung" kommt aus `ZonenNutzungCtrl.Lesen` über `GebaeudeNeulesenHuelle.MitNutzung`. Kein Schemaschritt, kein SQL, Rechenweg unberührt.
- **Wiederimport (MA-6):** `MehrzonenuebernahmeCtrl.Zusammenfuehren` (in `MehrzonenuebernahmeCtrl.Zusammenfuehrung.cs`) führt in einem Vorgang (`Vorgangsklammer`, Rückweg bei Fehler) über Quellkennung und Herkunft zusammen: Ids bleiben, je Feld Hand vor Datei vor Vorgabe, Sätze ohne Quellkennung bleiben, Entfallendes nur mit Zustimmung; `MehrzonenuebernahmeCtrl.Vorschau` liefert die Unterschiede; Hülle `GebaeudeMehrzonenHuelle` mit Gabe `Wiederimport`, Baustein `EPOS.UI/Dialoge/Import/WiederimportVorschau.razor`; `ZonenNutzungCtrl` setzt `Herkunft = MANUELL` (auch im Ferienweg). Kein Schemaschritt (`Tab_Bauteil.U_Herkunft` kommt mit Schritt 233, E149).
- **Restdaten (DA-4):** `GebaeudeRestdaten` (Kern, `EPOS.Kern/Allgemein/Import/Gebaeude/`) trägt Vorheizzeit, Höchstleistungen, Übergabe und Kühlübergabe je Zone, Luftströme, Umfang, wirksamen U-Wert am Erdreich, Kellertemperatur, Zeilenbild und benannte Wochen; gbXML-Rückfall (Weg 2) ist der Block `EPOS:` mit Schlüssel=Wert in `Description`, IFC die Sätze `EPOS_Zone`/`EPOS_Gebaeude`; der Leser übergeht unlesbare Werte benannt. Kein Schemaschritt.
- **Profilprüfung (DA-5):** `GbxmlRaumtyptafel` (Kern) bildet `spaceType` auf Profile ab; `Raumnutzungsvorbelegung.DinTreffer` löst die DIN-Nummer über die Profile der Kategorie DIN/TS 18599-10 auf; IFC trägt `EPOS_Zone.DinNummer`; Ampel in Prüfgruppe 3 nach Herkunft des Profils (`ZONE_OHNE_PROFIL` gelb, die Zone erbt vom Gebäude).
- **Round-Trip-Wache (DA-7):** `Datenaustauschinventar` (Tafel der EPOS-Felder mit Welle und Grund, `EPOS.Kern.Tests`) und `DatenaustauschRundlaufWacheTests`: jedes Feld eingestuft, Verlustliste ↔ Tafel über `Abstimmung`, gemeinsamer Rundlauf (je Format 70 661 Felder). Eine neue Spalte braucht einen Eintrag in der Tafel, ein neuer Weg einen in der Kerntafel `DatenaustauschInventar`.
- **Maskenschlüssel `Projektgebaeude`** (GD-N) öffnet unter Windows `GebaeudeFenster.ProjektOeffnen`, auf iOS benannte Ablehnung.
- **Haustechnik:** Klasse `HAUSTECHNIK` (Schritt 222) ist die letzte Klasse der Gruppe Sonstige an Baustoff und Bauteilaufbau; der Import setzt sie für Basis-Codes 14, 19, 29, 30.

## 4. Nächste Arbeit (Reihenfolge)

1. Runde 231/232 wie in Abschnitt 3, dann CI-Vermerke.
2. Schritt 233 `BauteilUHerkunftSchema` (MA-6 (b) Hand-U, E149); weitere Nachzüge aus MA-6: ein nur im Kalendereditor geänderter Kalender markiert die Zone nicht; Zonenfelder im Zonendialog ohne MANUELL nimmt die Datei.
3. Laufende Wellen VS, N4, N5 abnehmen und mergen (Abschnitt 1); danach Nachzugsliste: KI-Sicht für Fensterwerte und Abschnitte, Importdialog Unterordner `Export`, DA-8 nur auf eigenen Entscheid (F8 (a)).
4. BK-7 (Teilkatalognamen `Katalog_Inhalt.csv`, Hilfeschlüssel des Importdialogs); BK-4 (LocalDB-Werkzeug) nur für einen neuen Katalogstand.
5. Wiki-Quellen für BK-5/BK-6 und DA-6; Sammel-Upload nur auf Zuruf (E144, Version 1.2.0.12 laut Empfehlung). Der am 14.10.2026 vorbereitete Upload (Logbuch „Version 1.2.0.11“ in `Werkzeuge/WikiUpload/logbuch.wiki`, 33 abweichende Seiten) ist nicht geladen; vor dem Upload die Versionsnummer gegen das Wiki bestätigen. Aus der Cloud nur, wenn `WIKI_BOT_USER`/`WIKI_BOT_PASS` als Umgebungsgeheimnisse hinterlegt sind, sonst lokal über „Epos Lokal“ (`py -3 Werkzeuge\WikiUpload\wiki_upload.py --seiten`, dann `--logbuch`); danach Statuszeile und `Wiki_Update_2026-09-26.md` nachführen.
6. SA4 (Referenzprojekt Schnellaufheizung, Basis R57 nach R56).

## 5. Offen beim Anwender

- „Epos Lokal“ starten (Rechner, HSETU-Daten, Normen), wenn die Runde 231/232 gepusht ist.
- Gemeinsame Sichtabnahme unter Windows nach dem Push von 232 (E146): Ampelleiste, Fensterteil, Abschnitte, Gebäudedialoge, IFC-Punkte.
- Zweiter Importlauf des Bauteilkatalogs nach dem Push von 232 (E147), Sicherung vorher; dabei Bereinigung des einen Importsatzes mit Name und Gruppe eines Auslieferungssatzes (E148).
- Wiki-Upload nur auf Zuruf (E144).

Alle Entscheide des Anwenders vom 14.10.2026 (E144 bis E149) stehen mit Wortlaut im Register der Statusdatei Gebäudesimulation.

## 6. Arbeitsweise in der Cloud

- **Anwenderdateien** (`Quellen/*.sqproj`, Hottgenroth-Katalog `.mdf`) liegen nie im Repositorium; Namen und Werte daraus nie in
  Code, Tests, Protokollen oder Nachrichten — nur Struktur, Zählungen, Anteile. Katalogwerte eines Herstellers nur in die
  Datenbank des Anwenders, nie in die Auslieferung. **Normwerte:** allein die DIN/TS-18599-10-Profile nach E135 kommen ins
  Repositorium und in die Auslieferung, und nur aus der Lieferung des Anwenders; alle anderen Normwerte bleiben tabu. Dateien des Anwenders kommen über die lokale Sitzung „Epos Lokal“ (Rechner des Anwenders: Rechner, HSETU-Daten, Normen; der Anwender startet sie).
- **Agenten** mit ausdrücklichem Modell (`opus-umsetzung` für Fachcode, `sonnet-mechanik`, `haiku-pruefung`), je Welle ein
  Worktree, Commits auf dem eigenen Zweig, kein Push. Die Orchestrierung mergt in einem Integrations-Worktree, fährt das Gate
  und pusht.
- **Gate nach dem Merge:** `Werkzeuge/Gate/nachgate_gebaeudesimulation.sh <Worktree> <Gatelog>` (Schritte 0–7, `REF=1` für den Referenzlauf der zwölf CI-Projekte; Basis aus `Referenzlaeufe/LIESMICH.md`, Ende an `NACHGATE-ENDE`); Aufrufe in `Werkzeuge/Gate/LIESMICH.md`, Abschnitt „Skripte der Sitzung Gebäudesimulation“. Die CI rechnet den Kern-Filter **ohne** Filter: Tests, die feste Zahlen der Kataloge erwarten, auf Konstanten der Saat beziehen.
- **Statusnummern** unmittelbar vor dem Commit gegen origin messen; bei Kollision `status_merge.py` und Verweise per `sed`; `resx_merge.py` für `.resx`-Konflikte, `nachweis.py` für den Nachweis in den Statuszeilen.
- **Ressourcenkonflikte** beim Merge (`.resx`, Anhänge am Dateiende): beide Seiten behalten — zuerst die eigene, dann ein
  `</data>`, dann die fremde Seite, wenn hinter dem Konfliktblock das schließende `</data>` steht; danach
  `designer_neu.py schreiben` und die Namen auf Dubletten prüfen.
- **Testdatenbank** in Worktrees: `git lfs fetch origin ios_migration_september --include=Referenzlaeufe/Kenndaten_Test.sqlite && git lfs checkout Referenzlaeufe/Kenndaten_Test.sqlite` (ohne `-q`); vor jedem Merge mit Datenbankänderung `git checkout -- Referenzlaeufe/Kenndaten_Test.sqlite`.
- **Push:** `git -c "lfs.https://github.com/inekon/EPOS-Plan.git/info/lfs.locksverify=false" push -q origin HEAD:refs/heads/<zweig>` auf `ios_migration_september` und den Sitzungszweig. Pushes auf den Sitzungszweig brechen dessen laufende CI ab; reine Papier-Pushes deshalb mit `[skip ci]` nur auf den Arbeitszweig. Der CI-Nachweis kommt vom Lauf des Sitzungszweigs; Check-in per `send_later` (etwa 50 Minuten), der einen Sonnet-Agenten auslöst. Reihenfolge einer Welle: Merge → Gate → Statuszeile und Protokoll → Push → CI-Vermerk `[skip ci]`.
- **Sicherungszweige:** Fertige, nicht gemergte Wellen liegen als `claude/gebaeudesimulation-<name>` auf origin (Abholen in Abschnitt 3); Refs außerhalb `refs/heads` lehnt das Git-Gateway der Cloud ab, ein roter Kern-Lauf solcher Zweige ist kein Befund.
- **Routinen sind sitzungs- und kontogebunden;** die neue Sitzung richtet eigene Check-ins ein.
- **Container und Platte:** Container-Neustarts beenden Agenten; Worktrees unter `/home/user/wt` überleben, `/tmp` nicht (deshalb liegen die Skripte im Repositorium). Worktrees nach dem Push entfernen; unter 5 GB frei kein Gate.
- **Modellregel (Anwender, 13.10.2026):** Fable 5.1 nur für Orchestrierung und sehr komplexe Analysen; Fachcode, Schema, Tests und Fachkonflikte Opus 5.5; Merges ohne Fachkonflikt, Gates, Nachweise, Status, Protokolle, Wiki und CI-Auswertung Sonnet; Zählungen und Prüfungen Haiku; Check-ins lösen Agenten aus.
- Keine macOS-, iOS- oder Setup-Läufe, kein Wiki-Upload, keine Pull Requests, kein Force-Push und keine Tags ohne Rückfrage.

## 7. Einstiegs-Prompt

```text
Sitzung „Gebäudesimulation“ (EPOS-Plan, inekon/EPOS-Plan) fortsetzen. Arbeitszweig ios_migration_september; der eigene Sitzungszweig wird bei jedem Push mitgezogen. Antworten auf Deutsch, knapp; Stände als Tabelle.

1. Lies CLAUDE.md ganz und das Übergabepapier Dokumentation/aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md ganz (Stand 16.10.2026, 00:30 UTC). Aus Dokumentation/aktuell/Status_iOS_Migration.md nur den Kopf („Schemaschritt angemeldet“, Referenzbasis) und die Zeilen ab #1091; aus Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md die Entscheide E135 bis E149. Weitere Papiere nur gezielt per grep.
2. git fetch origin ios_migration_september; git log --format='%h %<(72,trunc)%s' -n 30 origin/ios_migration_september: was seit 3a8f1bcb0 hinzukam. Den Statuskopf auf origin gegen Abschnitt 1 des Papiers halten (Schemakette 228 bis 233, Basis R55).
3. Die zwei fertigen Wellen holen: git fetch origin claude/gebaeudesimulation-da6:da6 claude/gebaeudesimulation-bk5:bk5 (erwartet fdb7c7072 und 9407ae602). Sobald die Schritte 229 und 230 auf origin liegen: Opus-Runde nach Abschnitt 3 (merge, Kette umhängen, Tests, Testdatenbank auf 232 heben, Nachgate mit Referenzlauf, zwei Statuszeilen, Push, CI-Vermerk). Danach Schritt 233 und Abschnitt 4 der Reihe nach. Die laufenden Wellen VS, N4, N5 und PAP (Abschnitt 1) abnehmen.
4. Nachbarsitzungen: Kennungen aus den Commit-Trailern Claude-Session (Abschnitt 1). Den Sitzungen Kältebedarf (229) und Dialoge/Cloud (230) per send_message die eigene Kennung mitteilen und um Ankündigung ihrer Pushes bitten. Schemaschritte nur über die Kopfzeile anmelden und allein diese Zeile sofort pushen; vor macOS-, iOS- oder Setup-Läufen, Wiki-Upload, Pull Requests und Force-Push immer Rückfrage.
5. Arbeitsweise: Orchestrierung mit Agenten, Modell immer ausdrücklich — opus-umsetzung für Fachcode, Schema, Tests, Fachkonflikte; sonnet-mechanik für Merges ohne Fachkonflikt, Gates, Status, Protokolle, Übergabepapier, CI-Vermerke, Wiki-Quellen; haiku-pruefung für Zählungen. Fable nur für Auftragsschnitt, Abnahme, Abstimmung, Push und Anwenderentscheide. Je Welle ein Worktree unter /home/user/wt, Integrations-Worktree aus origin; Skripte und Aufrufe in Werkzeuge/Gate/LIESMICH.md (Abschnitt Gebäudesimulation). Push nur auf ios_migration_september und den Sitzungszweig; reine Papier-Pushes mit [skip ci] nur auf den Arbeitszweig. Eigene Check-ins per send_later einrichten (etwa 50 Minuten nach einem Push für den CI-Vermerk über einen Sonnet-Agenten). Übergabepapier bei jedem Push nachführen; erledigte Worktrees nach dem Push entfernen.
6. Grenzen: Anwenderdateien (.sqproj, .mdf, CSV-Export, PDF der Norm) nie ins Repositorium, aus ihnen nur Struktur und Zählungen; Normwerte nur die DIN/TS-18599-10-Profile nach E135 aus der Lieferung (als Saat in da6). Den Rechner des Anwenders erreicht nur die lokale Sitzung „Epos Lokal“, die der Anwender startet.
7. Offene Anwenderfragen (Abschnitt 5) gesammelt mit Empfehlung vorlegen.
```
