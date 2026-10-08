# Übergabe der Sitzung „Dialoge und Korrekturen“ (05.10.2026)

Übergabe auf ein anderes Konto. Dieses Papier nennt den Stand, die geltenden Regeln der
Sitzung, die laufenden Arbeiten und die offenen Punkte. Der Einstiegs-Prompt für die neue
Sitzung steht am Ende. Regelquelle bleibt `CLAUDE.md`; dieses Papier ergänzt nur, was
die Sitzung darüber hinaus vereinbart hat.

## 1. Stand

- **Zweige:** Arbeitszweig `ios_migration_september`; Sitzungszweig
  `claude/dialoge-korrekturen-okt`, der bei jedem Push mitgezogen wird
  (`git push origin HEAD:claude/dialoge-korrekturen-okt`). Beide standen zuletzt gleich.
- **Statuszeilen dieser Sitzung** in `Status_iOS_Migration.md`: #659 bis #678 (Modellgrenzen
  M1–M7, Solarthermie-Ganglinie, Gebäudedialog, IFC-Bauteilliste), #685 (KU1 Stufe 2), #687
  (Projektkopien Brennstoffe und Pufferauslegungs-Vorgaben, Schema 175), #693 (Schema 176
  Konditionierungsnutzung, Dickengrenze 0,5 mm), #694 (Kälte-Chart, BHKW-Stromganglinie,
  Hinweisklappe), #698 (Strom- und Kältebild im Bericht, Katalogfassung 12), #719
  (Speicherrechnung erst nach gerechnetem Strombedarf), #720 (Peak-Vorgabe der Speicherflotte
  aus dem laufenden Durchgang), #739 (Hilfepille im Hauptfenster), dazu zwei Zeilen für
  „Auslegung des Wärmepumpe-Reiters ans Ende“ und „Reihenschalter waagerecht“, die beim
  Schreiben dieses Papiers im Gate standen.
- **Schemaschritte dieser Sitzung:** 163–168, 171–173, 175, 176; die Kopfzeile der Statusdatei
  führt die vergebenen und angemeldeten Nummern.
- **Basis:** Die aktuelle Basis steht in `Referenzlaeufe/LIESMICH.md` und `CLAUDE.md`
  (zuletzt R38); die Testdatenbank wird von mehreren Sitzungen gehoben. Nach jedem Merge
  prüfen, dass `Referenzlaeufe/Kenndaten_Test.sqlite` im Index liegt und über 80 MB groß ist:
  Ein Sync-Commit vom 04.10. hatte sie entfernt (Statuszeile #720 nennt die Wiederherstellung).

## 2. Regeln der Sitzung (über `CLAUDE.md` hinaus)

- **Wellenfolge:** Merge mit origin → Gate (`Werkzeuge/Gate/gate_linux.sh <Nr> <Repo>`,
  danach die Windows-Schale auf Linux bauen) → Statuszeile samt Logbuchsatz in
  `Wiki_Update_2026-09-26.md` Abschnitt 1.2.0.6 → Push beider Zweige → CI-Vermerk in der
  Statuszeile, sobald der Kern-Lauf grün ist. Push nach grünem Gate ohne Rückfrage; nie ohne
  Rückfrage: iOS-, macOS- oder Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags.
- **Statusnummer** unmittelbar vor dem Commit gegen `origin/ios_migration_september` messen
  (höchste `| **#NNN** |` plus eins); Kopfzeile „Schemaschritt angemeldet“ vor dem Bau eines
  Schemaschritts setzen und allein pushen, beim Push der Welle auf „zuletzt vergeben“ stellen.
- **Modellwahl:** Fable orchestriert nur; `opus-umsetzung` für Rechenweg, Schema, Dialoge,
  Fehlersuche, Konfliktauflösung; `sonnet-mechanik` für Merge ohne Fachkonflikt, Gate,
  Statuszeilen, Push, CI-Vermerk, Ressourcen, Wiki-Quellen; `haiku-pruefung` für Zählungen
  und Encoding. Höchstens zwei Agenten parallel (Platte); Agenten-Worktrees nach dem Merge
  sofort entfernen.
- **Trailer** jedes Commits: `Co-Authored-By: Claude <Modell> <noreply@anthropic.com>` und
  `Claude-Session: <URL der arbeitenden Sitzung>`.
- **Fallstricke:** resx-Merges verlieren gern ein `</data>` am Blockende (XML mit `minidom`
  prüfen, dann `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben`);
  `Werkzeuge/Testdatenbankschema` verändert die Repo-Datenbank auch bei einem No-op-Lauf
  (nur bewusst heben, sonst `git checkout -- Referenzlaeufe/Kenndaten_Test.sqlite && git lfs checkout`);
  ein alter Werkzeug-Binärstand kann die Datenbank auf eine ältere Zielversion zurücksetzen,
  deshalb das Werkzeug vor dem Heben bauen; `pkill -f` mit einem Muster, das den eigenen
  Befehl trifft, beendet die eigene Shell; der Git-Proxy der Cloud-Sitzung verweigert
  `git push --delete` (Zweige löscht der Anwender).

## 3. Laufende Arbeiten beim Schreiben

- Sonnet-Agent: Merge der Wellen „Auslegung des Wärmepumpe-Reiters ans Ende“
  (`5ab641640`, `dc1817854`) und „Reihenschalter waagerecht“ (`5970775e8`), Gate, zwei
  Statuszeilen, Push. Ist das beim Lesen nicht auf origin, liegen die Merge-Commits
  „Merge Waermepumpenreiter …“ und „Merge Reihenwahl …“ lokal oder gar nicht vor; dann aus den
  genannten Commits neu mergen.
- Opus-Agent: **Jahresarbeitszahl im Wärmepumpe-Reiter** (JAZ der Wärmepumpe und des
  Systems mit Heizstab im Block „Strom“, Spalte JAZ je Modul, Texte de/en, Wiki Wärmepumpe,
  keine neuen Platzhalterschlüssel, da Katalogfassung 12 eingefroren ist). Sein Stand liegt,
  wenn fertig, auf einem Worktree-Zweig `worktree-agent-*`, sonst ist die Welle neu zu
  beauftragen.

## 4. Offene Punkte

Beim Anwender:

- LFS auf dem Windows-Rechner prüfen (`git lfs install`, nach dem Pull `git lfs checkout`),
  sonst entfernt `GitHub_Sync.bat` die Testdatenbank erneut.
- Sichtabnahmen unter Windows: Kälte-Chart und Autarkie rechts, BHKW-Stromganglinie,
  Hinweisklappe, Bericht mit Strom- und Kältebild, Dialog „Brennstoffe des Projekts“,
  Bauteilaufbau ab 0,5 mm, Herkunftszeile des Peak-Ziels, Hilfepille, Wärmepumpe-Reiter.
- Windows-Messliste der ChartProben um acht Zeilen nachziehen (zwei neue Bilder seit #694).
- Wiki-Sammel-Upload mit den Logbuchsätzen unter 1.2.0.6 (Versionsnummer erfragen).
- Setup-Kette für `Katalogpaket.json`; ReadOnly-Sätze Heizkessel und PV in der produktiven
  Quelle vor der nächsten Auslieferungsvorlage prüfen.
- Alte `claude/`-Zweige auf origin vom 27.09. (`intelligent-bohr`, `optimistic-bell`,
  `peaceful-johnson`, `practical-ptolemy`) tragen die alte Git-Geschichte und können gelöscht
  werden; `jolly-shannon`, `adoring-cerf`, `wiki-help-assistant-docs` gehören anderen Sitzungen.

Fachlich offen:

- Heizkessel-Reiter: Block „Auslegung“ (nur Kennzahlen) steht vor der Ganglinie; nach unten
  nur auf Wunsch.
- Entschieden ohne Änderung (Statuszeile #693): Stromprofil ohne Typ wird übersprungen,
  gespeicherte Weiche Profil/Ganglinie, Zonenebene im Gebäudekatalog und Feld für die
  WP-Mindestlaufzeit nur auf Wunsch, Desinfektions-Fähigkeitsprüfung bleibt vereinfacht.
- Vorlagenfeldkatalog v12 ist eingefroren; Konzepte, die Marken für Fassung 12 planen,
  müssen auf Fassung 13 ausweichen.
- Kopfzeile zu Schemaschritt 179 (Sitzung „Simulation Pufferspeicher“) trägt den Hinweis,
  `ID_Konditionierungsvorlage` am Gebäude nicht anzulegen (Kopie statt Verweis).

## 5. Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Dialoge und Korrekturen“ für EPOS-Plan (Repo inekon/EPOS-Plan,
Arbeitszweig ios_migration_september). Lies zuerst CLAUDE.md und
Dokumentation/aktuell/Dialoge/2026-10-05_Uebergabe_Dialoge_und_Korrekturen.md, dann den
Kopf und die letzten zwanzig Zeilen von Dokumentation/aktuell/Status_iOS_Migration.md und
`git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september`.

Regeln: Fable orchestriert nur; opus-umsetzung für Umsetzung und Fehlersuche,
sonnet-mechanik für Merge, Gate, Statuszeilen, Push und CI-Vermerk, haiku-pruefung für
Zählungen; Modell bei jedem Agentenaufruf setzen; höchstens zwei Agenten parallel; Agenten
im Worktree, erster Schritt `git reset --hard ios_migration_september`, nicht pushen.
Wellenfolge Merge → Gate → Statuszeile und Logbuchsatz → Push beider Zweige
(ios_migration_september und claude/dialoge-korrekturen-okt) → CI-Vermerk. Statusnummer
vor dem Commit gegen origin messen. Nie ohne Rückfrage: iOS-, macOS-, Setup-Läufe,
Wiki-Upload, Pull Requests, Force-Push, Tags. Commit-Trailer: Co-Authored-By des
arbeitenden Modells und Claude-Session mit der URL dieser Sitzung. Antworten auf Deutsch,
knapp; Zwischenstände nur auf Nachfrage.

Erste Schritte: (1) Prüfen, ob die Merge-Commits „Merge Waermepumpenreiter …“ und „Merge
Reihenwahl …“ samt ihren Statuszeilen auf origin liegen; sonst die Welle aus den Commits
5ab641640, dc1817854 und 5970775e8 fertigstellen. (2) Prüfen, ob ein Worktree-Zweig mit der
Jahresarbeitszahl im Wärmepumpe-Reiter vorliegt; sonst die Welle nach Abschnitt 3 der
Übergabe neu beauftragen. (3) Nach jedem Merge prüfen, dass
Referenzlaeufe/Kenndaten_Test.sqlite im Index liegt und über 80 MB groß ist. (4) Dann die
offenen Punkte aus Abschnitt 4 mit dem Anwender abstimmen.
```
