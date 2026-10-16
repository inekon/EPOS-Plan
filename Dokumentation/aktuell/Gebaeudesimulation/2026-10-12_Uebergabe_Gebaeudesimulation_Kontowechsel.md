# Übergabe der Sitzung „Gebäudesimulation“ — Stand 16.10.2026, 14:45 UTC

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
| Fertige Wellen | Runde 231–233 gebaut (#1110 DA-6, #1111 BK-5/BK-6, #1112 MA-6b; Zweig `r6`, Merge-Commits `42de7b173`, `5899125c9`), Push folgt; die Zweige `claude/gebaeudesimulation-da6` und `claude/gebaeudesimulation-bk5` sind erledigt. Danach BK-7 (#1114, `3fa37878d`/`048b485e3`, Merge `2b156e3ae`) und KIS (#1115, `842a462d1`/`cf42f7081`/`4180dc1fa`, Merge `3faa1a36d`) gebaut; Testkorrektur `aa5972ea0` (Lauf 38882648396 grün auf `9f4c6d3ce`); DA-8 (Schritt 237, E151) gebaut auf `da8`, wartet auf 236 |
| Laufende Wellen | **VS** (Farbmodus „Vollständigkeit“, E145, #1099), **N4** (Feiertagsperioden mit Wochenverweis, Gebäude-Höchsttemperatur im Austausch, #1097) und **N5** („Klimaregion wählen…“, Wochenvorschau nach Neulesen, #1098) sind gebaut und im Gate grün auf `7721492ff` (Integrationszweig `integ7`; Push folgt, CI-Vermerk folgt); **PAP** (Papierpflege) gepusht (`7c1bd049b`) |
| Referenzbasis | **R56** `Referenzlaeufe/2026-10-16_R56_Wasserkosten`, 37 Projekte (auf origin mit `f33367c39`); nächste Basis R57 mit SA4 (in Arbeit, Zweig `sa4`); CI-Auswahl laut `CLAUDE.md` |
| Testdatenbank | Schemastand **235** auf origin (Git LFS, SHA-256 `3478881f…`); vor jedem Heben die origin-Fassung nehmen, nie die eigene Kopie |
| Schemakette | 228 `GeraetekostenEntfallSchema` → 229 `StromspeicherKostenSchema` → 230 `KaeltebedarfsprofilVorlagenSchema` → 231 `RaumnutzungDinWerteSchema` (DA-6) → 232 `BauteilkatalogAbschnittSchema` (BK-5/BK-6) → 233 `BauteilUHerkunftSchema` (MA-6b, E149); 234 KBP-5 und 235 KB-7 anderer Sitzungen sind gebaut; 236 KBP-4 (Sitzung Dialoge) steht aus; 237 `BauteilStrahlungSchema` (DA-8, E151) ist gebaut auf Zweig `da8` und wird an 236 gehängt, sobald 236 auf origin liegt. Die Kette hängt immer über `+ 1` an der Vorgängerklasse |
| Statuszeilen | höchste vergebene **#1129** (BK-7b #1126, BK-8 #1127, BK-8b #1128, Papierpflege #1129); davor **#1115** (N4 #1097, N5 #1098, VS #1099, DA-6 #1110, BK-5/BK-6 #1111, MA-6b #1112, BK-7 #1114, KIS #1115; #1100–#1104 anderer Sitzungen); vor jeder neuen Zeile Tabelle und Kopf auf Reservierungen anderer Sitzungen prüfen. Entscheide bis **E158** |
| Nachbarsitzungen | Kennungen stehen in den Commit-Trailern `Claude-Session` auf origin: Kältebedarf (KB-4/KB-5, Schritt 229) seit 16.10.2026 `session_01LxWpn8bgrFYtFBXppoeoCL` (Hebung 229 angekündigt im Statuskopf ab 02:00 UTC, 230 noch nicht auf origin; zuvor `session_0178YLFEbS4xWdGaXFaZqG1r`), Dialoge und Korrekturen Cloud (KBP-2) `session_01Mt7MsJGzcGr77yqBsxwpLj`, Anlagendialog/ChartProben (R55) `session_01UNeGChCYZu2EpgFx4P3q1q`. Abfrage: `git log --format='%(trailers:key=Claude-Session,valueonly)' -n 80 origin/ios_migration_september | sort | uniq -c` |
| Lokale Sitzung | „Epos Lokal“ auf dem Rechner des Anwenders (Rechner, HSETU-Daten, Normen); der Anwender startet sie, Importlauf und DA-6-Lieferung sind geliefert |
| Rückkanal an Epos Lokal | Git-Postfach auf dem verwaisten Zweig `claude/postfach-epos-lokal`, Datei `Quellen/Postfach/an_EPOS_Lokal.md`; je Nachricht eine Überschrift `## <Nr> · <UTC> · <Betreff>`, unten anhängen, Commit mit `[skip ci]`, der Zweig wird nie gemergt. Epos Lokal holt alle 10 Minuten und antwortet per SendMessage. Nachricht 1 (`46d252dc8`) ist bestätigt; der offene Auftrag E147/E148 (zweiter Importlauf und Bereinigung) startet mit Nachricht 2 nach dem Push |
| Kennzahl- und Vorlagenfeldschlüssel | Die Runde 231–233 bringt keine Kennzahl- und keine Vorlagenfeldschlüssel (Rückfrage der Kostenbasis-Sitzung wegen Fassung 24 des Vorlagenfeldkatalogs) |

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
| N4, N5, VS | #1097 bis #1099 | Feiertagsverweis und Gebäude-Höchsttemperatur im Austausch; Wochenvorschau nach Neulesen und „Klimaregion wählen…“; Bedienbegriff „Vollständigkeit“ statt „Befund“ (E145); gebaut, Push folgt |
| DA-6, BK-5/BK-6, MA-6b | #1110 bis #1112 | DIN-Nutzungsprofile mit Werten (Schritt 231); Abschnitte und Fensteraufbauten im Bauteilkatalog (Schritt 232); Hand-U beim Wiederimport (Schritt 233); gebaut, Push folgt |
| BK-7b, BK-8, BK-8b, Papierpflege | #1126 bis #1129 | Teilkataloge aus `Katalog_Zuordnung.csv`; inhomogene Aufbauten nach DIN EN ISO 6946 6.7 (Schichtzahl, Restanteil), normgerechte Teilung, Fehlerschranke e, Vorschau zählt U-Änderungen; 25 erledigte Papiere nach `ueberholt/`; umgesetzt |
| BK-7, KIS | #1114, #1115 | Teilkatalog in der Quelle und Unterordner `Export` beim Bauteilkatalog-Import; KI-Sicht für Fensterwerte, Abschnitte und U-Herkunft, Herkunftszeile im Bauteildialog; gebaut, Push folgt |
| Wiki | #1061, #1085 | Wiki-Quellen und Logbuch-Entwurf; **kein Upload** |

## 3. Laufende Arbeit

**Keine offene Welle im Zweig.** Die Runde 231–233 ist auf `r6` gebaut und gemergt (Gate läuft, Push folgt); die Zweige `claude/gebaeudesimulation-da6` und `claude/gebaeudesimulation-bk5` sind erledigt.

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

1. Erledigt: BK-7b, BK-8, BK-8b, Papierpflege (#1126–#1129); Runde 231/232 und 233 (#1110–#1112, CI-Vermerke eingetragen), BK-7 (#1114) und KIS (#1115); gepusht mit `b13c7edd7`; dann DA-8 (E151) nach 236 und SA4 (E153) nach R56.
2. Erledigt (N6): Nachzüge aus MA-6 — Kalenderknöpfe an einer Zone markieren sie `MANUELL`; alle Zonenfelder des Zonendialogs markieren, der Wiederimport bewahrt sie ([Protokoll N6](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-16_N6_Nachzuege.md)).
3. Nachzugsliste (erledigt mit N4/N5: Klimaregion wählen, Wochenvorschau, Maximaleraumtemperatur, Feiertagsperioden): KI-Sicht für Fensterwerte und Abschnitte, Importdialog Unterordner `Export`, DA-8 nur auf eigenen Entscheid (F8 (a)); erledigt (N6) aus N4/N5/VS: Herkunftszeile in der Klimaregion-Überlagerung; offene Wärmebedarfsansicht liest die neue Region sofort; Bezeichner mit Zeilenumbruch im gbXML-Block; Formatgrenze `Holiday` in der Wiki-Quelle Datenexport benannt; „Befund“-Texte `ADM_DUBLETTEN_BAUM`, `BHW_K_LEER` waren bereits umbenannt.
4. BK-7 und KI-Sicht sind erledigt (#1114, #1115); BK-4 (LocalDB-Werkzeug) nur für einen neuen Katalogstand.
5. Wiki-Quellen für BK-5/BK-6 und DA-6; Sammel-Upload nur auf Zuruf (E144, Version 1.2.0.12 laut Empfehlung). Der am 14.10.2026 vorbereitete Upload (Logbuch „Version 1.2.0.11“ in `Werkzeuge/WikiUpload/logbuch.wiki`, 33 abweichende Seiten) ist nicht geladen; vor dem Upload die Versionsnummer gegen das Wiki bestätigen. Aus der Cloud nur, wenn `WIKI_BOT_USER`/`WIKI_BOT_PASS` als Umgebungsgeheimnisse hinterlegt sind, sonst lokal über „Epos Lokal“ (`py -3 Werkzeuge\WikiUpload\wiki_upload.py --seiten`, dann `--logbuch`); danach Statuszeile und `Wiki_Update_2026-09-26.md` nachführen.
6. SA4 (Referenzprojekt Schnellaufheizung): R56 liegt (`f33367c39`), SA4 in Arbeit (E153), Basis wird R57.
7. Zweiter Importlauf des Bauteilkatalogs: erledigt (E147/E148); Vorschau BK-7b/BK-8/BK-8b über das Postfach.

## 5. Offen beim Anwender

- „Epos Lokal“ starten (Rechner, HSETU-Daten, Normen), wenn die Runde 231/232 gepusht ist. Die Datenbank-Einstellung beim Anwender ist erledigt.
- Gemeinsame Sichtabnahme unter Windows nach dem Push von 232 (E146): Ampelleiste, Fensterteil, Abschnitte, Gebäudedialoge, IFC-Punkte.
- Zweiter Importlauf des Bauteilkatalogs nach dem Push von 232 (E147), Sicherung vorher; dabei Bereinigung des einen Importsatzes mit Name und Gruppe eines Auslieferungssatzes (E148).
- Windows-Sichtabnahme N5 (A-N5-1 bis A-N5-4, Protokoll N5, Abschnitt „Sichtabnahme Windows“).
- Anwenderprobe N4 unter Windows: Export eines Gebäudes mit Feiertagsverweis und oberer Raumtemperatur, Wiedereinlesen im Importdialog (Zeile „Maximale Raumtemperatur“).
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

1. Lies CLAUDE.md ganz und das Übergabepapier Dokumentation/aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md ganz (Stand 16.10.2026, 08:45 UTC). Aus Dokumentation/aktuell/Status_iOS_Migration.md nur den Kopf („Schemaschritt angemeldet“, Referenzbasis) und die Zeilen ab #1091; aus Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md die Entscheide E135 bis E149. Weitere Papiere nur gezielt per grep.
2. git fetch origin ios_migration_september; git log --format='%h %<(72,trunc)%s' -n 30 origin/ios_migration_september: was seit 3a8f1bcb0 hinzukam. Den Statuskopf auf origin gegen Abschnitt 1 des Papiers halten (Schemakette 228 bis 233, Basis R55).
3. Die zwei fertigen Wellen holen: git fetch origin claude/gebaeudesimulation-da6:da6 claude/gebaeudesimulation-bk5:bk5 (erwartet fdb7c7072 und 9407ae602). Sobald die Schritte 229 und 230 auf origin liegen: Opus-Runde nach Abschnitt 3 (merge, Kette umhängen, Tests, Testdatenbank auf 232 heben, Nachgate mit Referenzlauf, zwei Statuszeilen, Push, CI-Vermerk). Danach Schritt 233 und Abschnitt 4 der Reihe nach. Die laufenden Wellen VS, N4, N5 und PAP (Abschnitt 1) abnehmen.
4. Nachbarsitzungen: Kennungen aus den Commit-Trailern Claude-Session (Abschnitt 1). Den Sitzungen Kältebedarf (229) und Dialoge/Cloud (230) per send_message die eigene Kennung mitteilen und um Ankündigung ihrer Pushes bitten. Schemaschritte nur über die Kopfzeile anmelden und allein diese Zeile sofort pushen; vor macOS-, iOS- oder Setup-Läufen, Wiki-Upload, Pull Requests und Force-Push immer Rückfrage.
5. Arbeitsweise: Orchestrierung mit Agenten, Modell immer ausdrücklich — opus-umsetzung für Fachcode, Schema, Tests, Fachkonflikte; sonnet-mechanik für Merges ohne Fachkonflikt, Gates, Status, Protokolle, Übergabepapier, CI-Vermerke, Wiki-Quellen; haiku-pruefung für Zählungen. Fable nur für Auftragsschnitt, Abnahme, Abstimmung, Push und Anwenderentscheide. Je Welle ein Worktree unter /home/user/wt, Integrations-Worktree aus origin; Skripte und Aufrufe in Werkzeuge/Gate/LIESMICH.md (Abschnitt Gebäudesimulation). Push nur auf ios_migration_september und den Sitzungszweig; reine Papier-Pushes mit [skip ci] nur auf den Arbeitszweig. Eigene Check-ins per send_later einrichten (etwa 50 Minuten nach einem Push für den CI-Vermerk über einen Sonnet-Agenten). Übergabepapier bei jedem Push nachführen; erledigte Worktrees nach dem Push entfernen.
6. Grenzen: Anwenderdateien (.sqproj, .mdf, CSV-Export, PDF der Norm) nie ins Repositorium, aus ihnen nur Struktur und Zählungen; Normwerte nur die DIN/TS-18599-10-Profile nach E135 aus der Lieferung (als Saat in da6). Den Rechner des Anwenders erreicht nur die lokale Sitzung „Epos Lokal“, die der Anwender startet.
7. Offene Anwenderfragen (Abschnitt 5) gesammelt mit Empfehlung vorlegen.
```
