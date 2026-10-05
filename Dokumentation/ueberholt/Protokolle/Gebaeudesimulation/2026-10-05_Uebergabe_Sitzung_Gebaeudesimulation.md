# Übergabe der Sitzung „Gebäudesimulation“ an ein anderes Konto (05.10.2026)

Dieses Papier ist die vollständige Grundlage für eine neue Cloud-Sitzung „Gebäudesimulation“ auf einem anderen Konto. Es beschreibt den Stand, die Arbeitsregeln dieser Sitzung und die nächsten Schritte. Was es nennt, ist auf `origin/ios_migration_september` gepusht; der genaue Commit steht im Startprompt (Abschnitt 7).

## 1. Umgebung einrichten (einmal je Konto)

1. Cloud-Umgebung für `inekon/EPOS-Plan` mit **Netzwerkzugriff Custom** und `lfs.github.com` unter *Allowed domains* (sonst scheitert jeder Push mit einem neuen Datenbankstand; Anleitung: https://code.claude.com/docs/en/cloud-environments#network-access). Prüfung in der Sitzung: `curl -sS -o /dev/null -w '%{http_code}\n' https://lfs.github.com/` liefert 302, nicht 403.
2. `git lfs install`; `Referenzlaeufe/Kenndaten_Test.sqlite` muss rund 88 MB groß sein (eine Datei von 130 Byte ist ein Zeiger: `git lfs checkout`).
3. `dotnet build WP-Plan.Kern.slnf -c Release -nologo -v q -clp:ErrorsOnly` einmal, damit `--no-build`-Testläufe Binaries finden.
4. Agentendefinitionen liegen im Repo unter `.claude/agents/` (`opus-umsetzung`, `sonnet-mechanik`, `haiku-pruefung`); die Gate-Skripte unter `Werkzeuge/Gate/` (`gate_linux.sh` = Kernlauf, `gate_haupt.sh` = Kernlauf plus Windows-Schale, Designer, SQL-Dialekt, Werkzeugtests, BOM, Konfliktmarker; `konflikt_union.py` vereinigt Konflikt-Hunks ours+theirs byte-erhaltend).

## 2. Arbeitsregeln dieser Sitzung (ergänzend zu `CLAUDE.md`)

- **Modelle:** Fable nur, wenn unbedingt nötig (Aufträge schneiden, Abnahme, Entscheide, Bericht); Opus 5.5 für Rechenweg, Schema, Tests, Hüllen, Dialoge, Konfliktlösung mit Fachinhalt, Konzepte; Sonnet für Inventare, Papiere, Gates, Merges, CI-Prüfung; Haiku für Zählungen und Encoding. Modell bei jedem Agentenaufruf ausdrücklich setzen; Agenten in eigenen Worktrees (`isolation: worktree`), Aufträge repo-relativ, höchstens rund 150 Werkzeugaufrufe je Agent, Abnahme mit `--filter`-Tests und Referenzlauf der betroffenen Projekte; nie pushen lassen.
- **Welle:** Merge der Agentenzweige → Gate im eigenen Worktree (`git worktree add --detach .claude/worktrees/gateNNN HEAD`, dann `GATE_ABLAGE=<ordner> bash Werkzeuge/Gate/gate_haupt.sh NNN <worktree>`; rund 60 Minuten, auf das Ende mit genau einem wartenden Hintergrundbefehl warten) → Statuszeile und Protokoll (Sonnet) → Push → CI-Vermerk (Sonnet mit den GitHub-MCP-Werkzeugen; `gh` gibt es in der Cloud nicht). Nie im Hauptbaum mergen, während ein Gate dort läuft.
- **Nummern:** Vor jedem Schemaschritt die Nummer in der Kopfzeile „Schemaschritt angemeldet“ von `Dokumentation/aktuell/Status_iOS_Migration.md` eintragen und die Zeile sofort allein pushen; ebenso „Referenzbasis angemeldet“. Statusnummern (#7xx) und Entscheidnummern (E9x) vor jedem Push gegen origin prüfen: `git show origin/ios_migration_september:<Datei> | grep -o '^| \*\*#74[0-9]\*\*'` bzw. `'^| E9[0-9] '`; Kollision → eigene Nummer hochzählen (nur die eigenen Vorkommen ersetzen).
- **Parallelsitzungen** (Cloud-Sitzungen können einander nicht benachrichtigen; Abstimmung über die Statusdatei und den Anwender): Sitzung „IFC-Ganglinie“ baut HC-1 in `EPOS.Kern/Allgemein/Import/Ifc/` und `Import/Gebaeude/` und hat HC-3 (#736) und den Flächenfilter (#737) gepusht; die Dateien unter `EPOS.Kern/Allgemein/Import/`, `EPOS.UI.Daten/Bedarf/GebaeudeImport*`, `EPOS.UI/Dialoge/Import/` nur nach deren Push und nach Merge anfassen. Eine weitere Sitzung (Kennung session_01LxWpn8bgrFYtFBXppoeoCL) hatte ein eigenes Konzept Nutzungsprofile (Rev. 1) geschrieben; sie baut nach E92 nicht an NP. Der Anwender synchronisiert selbst (`GitHub_Sync.bat`, Commits „Synchronisation vom …“); vor jedem Push `git fetch` und mergen. Jeder Push löst einen Kern-Lauf aus und bricht den vorigen ab — Papier-Commits sammeln und mit dem nächsten Wellen-Push mitnehmen.
- **Token-Sparsamkeit:** große Papiere nur mit `grep -n` und Abschnittslesen; Build- und Testausgaben kürzen; Suchen und Papiere an Sonnet geben; keine Wiederholung von Fakten aus `CLAUDE.md`.

## 3. Stand der Stufen (Entscheide E86–E92 im Register `Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md`)

| Stufe | Stand |
|---|---|
| KU3-6 freie Kühlung über die Wärmequelle (E75) | gebaut, #735, Schemaschritt 187, Basis R37 unverändert, Kern-Lauf grün |
| AK2 Erzeugerfahrplan | abgeschlossen (#732–#734, Basis R37) |
| AK3 geschlossener Kreis | ruht nach H6 bis zur Feldphase (E86); die Kennlinienwahl am gerechneten Vorlauf gibt es seit AK1; VW1 Ausweis der Vorlaufwahl gebaut (E88, #738, Schemaschritt 188, **Basis R38 `2026-10-05_R38_Vorlaufwahl`**, 1047 und 1056 mit neuen Zeilen in `aggregate.csv`); Messwert: rund 35 % der Stunden mit Kennlinienwahl unter der untersten Stützstelle 35 °C |
| GA Altweg ablösen | **entfällt (E89)**: der Altweg bleibt dauerhaft wählbarer Rechenweg; Papiere nachgezogen (Nachtrag N1.70 im Konzept VDI 6007), Projekt 1040 dauerhaft auf dem Tagesbilanz-Weg |
| Zonenmodell, steife Zone | Abbruch der Abschnittsregel bei q < 0 in HeizenGeregelt behoben (`Zonenmodell2K.ErsteInnereVerletzung` mit exakter Nullstelle, Übergabefälle ebenso; Tests `ZonenmodellSteifeZoneTests`); Referenzlauf 21/21 byte-gleich; Statuszeile und Protokoll `2026-10-05_Zonenmodell_Steife_Zone.md` (Nummer beim Push gegen origin geprüft) |
| NP Nutzungsprofile (E90–E92) | Konzept Rev. 2 `Dokumentation/aktuell/Konzept_Nutzungsprofile_EPOS-Plan.md` (Q38–Q47 nach Empfehlung = E91; diese Sitzung baut die ganze Stufe = E92). **NP1a gebaut** (Schemaschritt 189 `RaumnutzungSchema`: `Tab_Raumnutzungskatalog`, `Tab_Raumnutzungsprofil` mit 25 Kennwerten, `Tab_Raumnutzungszeile`, `Tab_Raumnutzungsstunden`, `Tab_Raumnutzungszuordnung`; `Nutzung` an Kalender und Vorlage als freier Text mit Tabellenneubau; `Tab_Zone.Nutzungsprofil`; Saat `RaumnutzungSaat` mit 4 Kategorien, 33 Profilen, 25 Zuordnungen; Testdatenbank 189, 87 793 664 Byte, OID `e90fc05f…`, auf dem LFS-Server). **NP1b** (Generator `Raumnutzungsgenerator`, `RaumnutzungCtrl`, Bitgleichheitsnachweis der drei alten Muster, Herkunft als freier Text) — Stand siehe Startprompt. Danach Papiere NP1 (Statuszeile, Protokoll, Kopfzeile 189 „gebaut“), Gate, Push |

Offen beim Anwender: Sichtabnahme des Sportheims unter Windows (steife Zonen prüfen: winzige Zonen, Bauteile ohne Masse, große Glasflächen), Logbuch-Version für die Sätze zu KU3-6 und VW1, Wiki-Upload der Seiten Kühlung und Wärmepumpe, Befund „Projektdatei: 0 Räume abgeglichen“ (Raumabgleich der HottCAD-Datei, Sitzung IFC-Ganglinie).

## 4. Nächste Schritte in dieser Reihenfolge

1. **NP1 abschließen:** NP1a- und NP1b-Stand mergen (Zweige im Startprompt), Papiere (Sonnet: Statuszeile mit nächster freier Nummer, Protokoll `2026-10-05_NP1_Schema_Katalog_Generator.md`, Indexzeile, Kopfzeile „Schemaschritt angemeldet“: 189 gebaut, 190 frei, Register NP-Zeile), Gate, Push, CI-Vermerk.
2. **NP2** (nach dem Push der Sitzung IFC-Ganglinie, Merge vor dem Bau): Zonenbaum liest die Profile aus dem Katalog (gruppiert nach Kategorie, Herleitungszeile), Vorbelegung über `Tab_Raumnutzungszuordnung` statt der festen Listen (`Zonenplan.NUTZUNGEN`, `NutzungAusKlasse`, `Din18599Nutzung`, `GebaeudeImportZonen.cs`), `ZonenplanCtrl.NutzungUebernehmen` über `RaumnutzungCtrl.ProfilUebernehmen`; Rangfolge Datei vor Profil bleibt; Importproben nachziehen; `IfcAbbildBauer.cs:933` Filter auf Katalognamen.
3. **NP3:** Blatt „Nutzungsprofile“ (Katalogbaum, Profileditor mit Vorschau der erzeugten Kalender, Zuordnungstabelle), Zugänge aus Gebäudeeditor (`GebaeudeKatalogDialog`, Reiter Konditionierung neben „Vorlagen verwalten…“), Zonenbaum und Zonendialog („Nutzungsprofil übernehmen…“); KI-Feldkarten und Wachen (`KiMaskenabdeckungWacheTests`), Ressourcen in beiden Sprachen.
4. **NP4:** CSV-Import der Profilwerte (Format aus Konzept 6.4), Profile aus der HottCAD-Projektdatei in einen eigenen Katalog (Q46), Wiki (Gebäudeimport, Konditionierung), Hilfe, Logbuch-Satz.
5. Danach nach E67: Oberflächenwellen O1b/O2/O3 oder Konditionierungswelle A auf Zuruf; AK3 erst nach der Feldphase; Folgeauftrag Zonenmodell (`InnenUmkehr` über `NullstelleAbleitung`, womöglich neue Basis).

## 5. Muster und Werkzeuge

- Statuszeile: Muster #735/#738 in `Status_iOS_Migration.md` (Spalten: Nummer, Datum, Titel mit Entscheiden, Inhalt mit Commits, Gate-Zahlen, Protokollverweis, Abnahmespalte „… CI-Kennung nach dem Push“); Block „Nach #NNN (Kürzel): (a) … (e)“ darunter; Protokollmuster `2026-10-05_KU3-6_Freie_Kuehlung_Waermequelle.md` (Kopf, 1 Auftrag, 2 Vorgehen, 3 Ergebnis, 4 Festlegungen, 5 Nachweise, 6 Offenes); Indexzeile in `Dokumentation/LIESMICH.md` (Zähler der Zeile `ueberholt/Protokolle/Gebaeudesimulation/` erhöhen).
- Basis einfrieren: Muster VW1b (Commit `b9371b0`: Lauf der 21 Projekte in `Referenzlaeufe/<Datum>_R<n>_<Name>`, Vergleich alt gegen neu, alten Ordner `git rm -r`, Protokoll nach `Dokumentation/ueberholt/Referenzbasen/`, `Referenzlaeufe/LIESMICH.md`, `kern.yml`/`ios.yml`, `CLAUDE.md` Regressionsnetz, Kopfzeile „Referenzbasis angemeldet“).
- Referenzlauf: `dotnet run --project EPOS.Referenzlauf -c Release -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte <Liste der 21> --ziel <Ordner>` und `… vergleich Referenzlaeufe/2026-10-05_R38_Vorlaufwahl <Ordner>`; Ausgabeordner danach löschen (Platte knapp).
- CI: Läufe über `mcp__github__actions_list` (branch-Filter) prüfen; Zwischenläufe werden durch Folge-Pushes abgelöst; CI-Vermerk „Kern-Lauf <id> grün auf <sha7>“ in die Abnahmespalte der Statuszeile.

## 6. Risiken und Fallstricke

- Worktrees der Agenten starten gelegentlich auf einem fremden Stand (8692ab40…): im Auftrag immer `git merge-base --is-ancestor <Basis> HEAD || git reset --hard <Basis>` vorgeben.
- Die Platte ist knapp (Gate-Worktrees 7 GB, Referenzläufe 1 GB): Worktrees nach der Übernahme entfernen (`git worktree remove --force`, bei Sperre `-f -f`), Ausgabeordner löschen.
- `pkill -f` mit einem Muster, das die eigene Shell trifft, tötet die eigene Shell; zum Warten `pgrep … | grep -vx "$$"`.
- Ein Hintergrundbefehl eines Agenten läuft höchstens rund 30 Minuten; Gates von der Orchestrierung mit einem Hintergrundbefehl (`run_in_background`, langer Timeout) abwarten.
- `git merge … | grep` verschluckt den Fehlerstatus: Konflikte immer mit `git status --short | grep '^UU'` prüfen.

## 7. Startprompt für die neue Sitzung

Den folgenden Text als erste Nachricht der neuen Cloud-Sitzung (Repo `inekon/EPOS-Plan`, Zweig `ios_migration_september`) senden; `<COMMIT>` und die Zweignamen ergänzt die abgebende Sitzung beim letzten Push:

> Du bist die Sitzung „Gebäudesimulation“ für EPOS-Plan und führst die Arbeit der Vorgängersitzung fort. Lies zuerst `CLAUDE.md` und dann vollständig `Dokumentation/ueberholt/Protokolle/Gebaeudesimulation/2026-10-05_Uebergabe_Sitzung_Gebaeudesimulation.md`; sie beschreibt Umgebung, Arbeitsregeln, Stand und die nächsten Schritte. Prüfe die Umgebung nach Abschnitt 1 (LFS-Freigabe, Datenbankgröße, Build). Der übergebene Stand ist Commit `<COMMIT>` auf `origin/ios_migration_september`; die Zweige `<NP1-ZWEIG>` auf origin tragen die Wellen NP1a/NP1b, falls sie noch nicht in den Arbeitszweig gemerged sind. Arbeite nach den Regeln in `CLAUDE.md` (Fable nur zum Orchestrieren, Opus 5.5 für Umsetzung, Sonnet für Mechanik, Haiku für Prüfungen; Welle Merge → Gate → Statuszeile → Push → CI-Vermerk; kein macOS-, iOS- oder Setup-Lauf ohne Rückfrage; kein Push ohne Auftrag außer den angemeldeten Kopfzeilen). Beginne mit Abschnitt 4, Schritt 1 (NP1 abschließen) und melde vor dem ersten Push den Stand in fünf Zeilen.

