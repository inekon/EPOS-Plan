# Übergabe: Sitzung „Gebäudesimulation“ — Stand 27.09.2026, Wechsel in die Cloud-Umgebung

Die Sitzung „Gebäudesimulation EPOS-Plan“ (VDI 6007, Kühlung, Anlagenkopplung, Mehrzonen, Import und
Export, Baualtersklassen, Konditionierungsprofile) wird in einer Cloud-Umgebung fortgeführt (Claude Code
im Web, gestartet aus der Desktop-App). Dort gibt es nur, was im Repositorium liegt. Dieses Papier ersetzt
das maschinenlokale Gedächtnis der bisherigen Sitzung: Stand, offene Punkte, Regeln, Nachbarn und was in
der Cloud anders ist. Die Sachlage selbst steht in der [Statusdatei](../Status_Gebaeudesimulation_VDI6007.md),
im [Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md), im
[Leitkonzept](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (Nachträge N1.x) und in den Protokollen
unter [`ueberholt/Protokolle/Gebaeudesimulation/`](../../ueberholt/Protokolle/Gebaeudesimulation/). Die
Vorgängerübergaben bleiben gültig:
[`2026-09-26_Uebergabe_Gebaeudesimulation_Anlagenkopplung_Mehrzonen.md`](2026-09-26_Uebergabe_Gebaeudesimulation_Anlagenkopplung_Mehrzonen.md)
(AK1, G6a, G6b, G7a, Arbeitsweise) und [`2026-09-26_Uebergabe_G6c_Katalog_M_A.md`](2026-09-26_Uebergabe_G6c_Katalog_M_A.md)
(G6c, E51).

## 1 Stand

- `origin/ios_migration_september` = `8bda803` (27.09.2026 13:13, Merge in `kp1-konditionierung`);
  letzter grüner Kern-Lauf auf dem Zweig: **36304861459** auf `3956959`, der den KP1a-Stand
  (#583, Kopf `092544f`) enthält. Auf `8bda803` lief bei der Übergabe der Kern-Lauf 36315033166.
- Referenzbasis **R23 `Referenzlaeufe/2026-09-26_R23_KesselBereitschaft`**, fünfzehn Projekte, CI rechnet
  sieben. Testdatenbank Schemastand **151** (KP-S1, LFS `22b1f882…`).
- **Nächste Nummern** (vor jeder Vergabe gegen `origin` messen, „wer zuerst pusht, behält die Nummer“,
  keine Lücke): Schemaschritt **152**, Entscheid **E54**, Nachtrag Leitkonzept **N1.62**, Statuszeile
  **#588** —
  `git show origin/ios_migration_september:Dokumentation/aktuell/Status_iOS_Migration.md | grep -oE '^\| \*\*#5[89][0-9]' | sort | tail -1`.
- **Fertig und gepusht:** alles bis KP1a. Stufen und Nachweise: Statusdatei Abschnitt 2 (Stufentabelle),
  zuletzt KP0 (Teilkonzept [Konditionierungsprofile](../Konzept_Konditionierungsprofile_EPOS-Plan.md) Rev. 3,
  E52, E53, [Aufheizleistungsprobe](../../ueberholt/Protokolle/Gebaeudesimulation/2026-09-27_KP0_Aufheizleistungsprobe.md))
  und **KP1a** ([Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-09-27_KP1_Konditionierungskalender.md),
  N1.61, Statuszeile #583): Schemaschritt 151, Kalendermodell, Standardfahrplan bitgleich, fünf Reihen,
  `KonditionierungCtrl`; Referenzlauf 15/15 byte-gleich gegen R23.
- **Nachzutragen in Statuszeile #583 und im Block „Nach #583“:** Push (Kopf `092544f` auf
  `ios_migration_september`; dessen eigener Kern-Lauf 36301201490 wurde vom Folgepush abgebrochen und
  ist kein Nachweis) und als CI-Nachweis der grüne Kern-Lauf 36304861459 auf `3956959`, der `092544f`
  enthält. Damit ist auch der im Protokoll (Abschnitt 5)
  offene volle Lauf von `EPOS.Kern.Tests` belegt — der Kern-Lauf führt ihn vollständig aus.

## 2 Was aussteht

### 2.1 Nächste Stufe: KP1b (auf Auftrag)

Wortlaut und Umfang im KP1-Protokoll, Abschnitt 6: Vorlagentabelle `Tab_Konditionierungsvorlage_STAMM`
samt Perioden und Wochen und Fremdschlüssel `ID_Vorlage` (Schemaschritt, Saat der vierzehn Vorlagen erst
mit KP2), Nachtauskühlung (3.7, P9) samt `Nachtauskuehlstunden_H`, Kopierwege Katalog → Projekt
(`CopyFromStamm`, „Speichern unter“, Katalogkopie und Schloss), `KINDER` für Duplikat und Variante,
`.wpx`-Rundlauf, `Werkzeuge/Auslieferungsvorlage` samt Prüfbericht, Hinweis auf Untertemperatur,
Auslegungswerte (3.6), Nutzungszeit aus dem Personenkalender (F16). Danach KP2 (Oberfläche, Schnittstelle
`KonditionierungCtrl`, Hüllen nach `EPOS.UI.Daten/Bedarf/`), KP3 (Aufheizoptimierung, neues
Referenzprojekt, Einfrierregel „gesäte Konditionierungsdaten“, neue Basis), KP4 (Papiere, Wiki, Logbuch).

### 2.2 Weitere Stufen (jeweils auf Auftrag)

| Stufe | Inhalt | Voraussetzung |
|---|---|---|
| G6c Welle E | Wiki-Abschnitt „Mehrere Zonen“ der Seite „Gebäudeimport“; offen aus Welle D: Rasterprobe GI/GJ mit dem Grundriss | — |
| G6d | Referenzprojekt mit Zonen, Einfrierregel „gesäte Zonendaten“, neue Basis; Keller-Befund (R_Rest < 0 nach Gl. (28), vermutlich fehlt eine Erdreichschicht) | Register M11 entscheiden |
| G7b | gbXML-Geometrie, Einstieg im Bedarfsdialog; danach Auslieferung von G7a | Probe 20 braucht einen macOS-Lauf — nur nach Rückfrage |
| KU3, AK2, AK3 | Kältemaschine und freie Kühlung; Erzeugerfahrplan; geschlossener Kreis | nach der Feldphase (E27) |
| GA | Altweg ablösen | Q24 Bedingungen (2) Feldphase und (4) Ausbauprobe |

### 2.3 Offen beim Anwender

1. **Windows-Sichtabnahmen:** G6a, G6b („Nach #538“), G7a („Nach #529“), E47 (vorher Datenbank sichern,
   Schritt 148 benennt Auslieferungssätze um), G4b samt Namensabgleich, E51, G6c-Importdialog samt
   Grundriss. KP1a braucht keine (keine Oberfläche).
2. **Wiki:** Nachschlag-Upload der Seite „Mehrzonenmodell“, der G6b-Nachzüge in „Gebäude“ und
   „Gebäudemodell VDI 6007“ und der Logbuch-Sätze aus dem [Update-Papier](../Wiki_Update_2026-09-26.md)
   (1.2.0.4 und 1.2.0.5). Hochladen mit `Werkzeuge/WikiUpload/`; die Anmeldung mit dem Bot-Passwort führt
   der Anwender selbst aus — in der Cloud werden keine Zugangsdaten eingegeben.
3. **Vor jeder Auslieferung** `GebaeudeExportRegeln.GbxmlExportFreigegeben` ausschalten, bis G7b folgt.
4. **Register:** M11 (vor G6d) ist der einzige offene Punkt.

## 3 Regeln dieser Sitzung (gelten weiter)

- **Entwurf vor Bau:** je Stufe zuerst nur lesend (Leser, zwei Entwürfe, Gegenprüfung, Synthese mit
  Anwenderfragen samt Empfehlung), dann Umsetzung in Wellen durch Agenten im eigenen Worktree; Modell
  nach `CLAUDE.md` ausdrücklich setzen (Agentendefinition `.claude/agents/opus5.md` liegt im Repositorium).
- **Haltepunkte:** H-S vor jedem Schemacommit, H-P vor jedem Papiercommit; Nummern vergibt der
  Orchestrator nach Messung gegen origin.
- **Welle:** Merge → Gate → Statuszeile und Protokoll → Push → CI-Nachweis nach `headSha` (ein
  abgebrochener Lauf ist kein Nachweis; bei Rot zuerst prüfen, ob die verursachende Sitzung schon
  repariert). Nach grünem Gate committen und pushen ohne Rückfrage; **iOS-, macOS- und Setup-Läufe nur
  nach Rückfrage, jedes Mal.**
- **Gate:** voller Testlauf des Kern-Filters mit den xUnit-Schaltern, Referenzlauf der fünfzehn Projekte
  byte-gleich gegen die Basis (bei Rechenwegänderung Neueinfrierung nach `Referenzlaeufe/LIESMICH.md`),
  `SqlDialektPruefer`, Windows-Schale mit `-p:EnableWindowsTargeting=true` 0 Fehler; bei Schemaschritten
  zusätzlich `dotnet test Werkzeuge/Auslieferungsvorlage/Auslieferungsvorlage.sln -c Release` (zählt die
  STRICT-Tabellen, steht nicht im Kern-Filter). Neue Tests vergleichen `double` mit Toleranz statt
  Stellenzahl (Linux und Windows weichen im letzten Bit ab).
- **Testdatenbank:** nur per wiederholbarem Skript (`Werkzeuge/Testdatenbankschema`,
  `Referenzlaeufe/Skripte/*.py`); bei LFS-Konflikt die origin-Fassung nehmen, eigene Schritte neu
  anwenden, Zellvergleich.
- **Schemaschritte:** jeder neue Schritt bekommt seinen Eintrag in `Paketanhebung.STUFEN`; Tabellen mit
  zwei Fremdschlüsseln auf Plantabellen gehören von Hand in `KINDER` (Befund KP1a).
- **Arbeitsdatenbank** des Anwenders nur lesend mit `immutable=1`; in der Cloud gibt es sie nicht.
- **Hauptbaum-Sperre** `AGENT_LAEUFT` atomar anlegen (`set -o noclobber`), erst nach erfolgreichem Push
  löschen. Große Skripte mit dem Schreibwerkzeug anlegen, nicht per Heredoc (Abbruch ab etwa 7 KB).
- **Kontingent:** beim Wochenkontingent von 80 % die Übergabe vorbereiten (alles committet und gepusht,
  Status und Übergabe nachgezogen).

## 4 Nachbarsitzungen

Cloud-Sitzungen „Dialoge und Korrekturen“ ([Übergabe](../Dialoge/2026-09-27_Uebergabe_Dialoge_Korrekturen_Cloud.md)),
„EPOS Plan Wirtschaftlichkeit“ ([Übergabe](../Wirtschaftlichkeit_Kosten/Uebergabe_Cloud_Wirtschaftlichkeit_2026-09-27.md)),
„Zapfprofil“ und „EPOS-Plan Berichterstellung“. Sitzungsnachrichten erreichen sie aus der Cloud nicht;
abgestimmt wird allein über `origin` (Statusdatei, Testdatenbank-oid, Schemaschritte): vor jedem Push
`git fetch` und mergen. Die Berichtsvorlagen-Sitzung vergibt Statusnummern ohne Abstimmung — deshalb
unmittelbar vor dem Commit messen.

## 5 Was in der Cloud anders ist

- **Nur noch Cloud:** Die Desktop-App wird nach dieser Übergabe neu gestartet und arbeitet danach
  allein mit Cloud-Sitzungen; die lokale Sitzung „Gebäudesimulation“ und ihre Worktrees enden. Gültig
  ist allein, was auf `origin` liegt — ein nicht gepushter lokaler Stand ist verloren. Windows-
  Sichtabnahmen und alles unter Abschnitt 2.3 führt der Anwender selbst am Windows-Rechner aus.
- **Frischer Klon ohne Werkzeuge:** Bei der Übergabe fehlten im Container `dotnet` und `git-lfs`, die
  Testdatenbank lag als Zeigerdatei (133 Byte). Das Einrichtungsskript der Umgebung installiert das SDK
  nach `global.json` (`dotnet-install.sh --version 10.0.400`) und `git-lfs`; danach
  `git lfs install && git lfs pull --include Referenzlaeufe/Kenndaten_Test.sqlite`. Die Netzwerkregel muss
  `nuget.org`, `api.github.com` und die LFS-Ablage von GitHub zulassen. Python-Werkzeuge laufen mit
  `python3`.
- **Gate auf Linux:** `Werkzeuge/Gate/gate_linux.sh <Nr>` (Kern-Filter, ChartProben gegen die Linux-
  Messlatte, Tests mit Schaltern, Wachen, Referenzlauf gegen die aktuelle Basis). Die Windows-Schale baut
  nur mit `EnableWindowsTargeting`; einen Anwender-Build und Windows-Sichtabnahmen gibt es nicht. Der
  volle Lauf von `EPOS.Kern.Tests` brach lokal wegen Speicherknappheit ab — in der Cloud mit
  `MaxParallelThreads=2` fahren und bei Abbruch die Testklassen in Gruppen laufen lassen.
- **CI-Nachweis ohne `gh`:** `https://api.github.com/repos/inekon/EPOS-Plan/actions/runs?head_sha=<voll>`
  oder die GitHub-Werkzeuge der Sitzung.
- **Zweig:** Die Cloud-Sitzung arbeitet auf ihrem zugewiesenen Zweig `claude/…`; ein Push auf
  `ios_migration_september` braucht die ausdrückliche Zustimmung des Anwenders in der Cloud-Sitzung.
- **Nicht im Repositorium** und in der Cloud nicht vorhanden (bei Bedarf neu beschaffen oder beim
  Anwender erfragen):
  - `Referenzlaeufe/Normzahlen/aixlib/` — AixLib-Normfälle der lokalen Normproben;
  - `Referenzlaeufe/Schemakopien/GreenBuildingXML_Ver8.01.xsd` — für Probe 3 von G7a, Download freigegeben
    (F2, E48), Quelle GitHub `GreenBuildingXML/gbXML_Schemas`;
  - die VDI-6007-Quellen auf dem Netzlaufwerk `Y:`;
  - Arbeitsdatenbank `%ProgramData%\EPOS_PLAN\Kenndaten.sqlite` samt `DB-Backup/`, Ergebnisse des
    Bestandsvergleichs unter `%TEMP%\EPOS_Gebaeudevergleich\` und die Messdateien M10.

## 6 Einstieg in der Cloud-Sitzung

Erste Anweisung: „Lies `CLAUDE.md`, diese Übergabe, die Statusdatei der Gebäudesimulation und in
`Dokumentation/aktuell/Status_iOS_Migration.md` den Block ‚Nach #583‘; dann `git fetch`, Nummern messen,
Statuszeile #583 nachtragen und KP1b als Entwurf vorlegen.“
