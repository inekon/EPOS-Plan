# Übergabe der Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, Kostenbasis“ (14.10.2026)

Dieses Papier bereitet den Wechsel der Sitzung auf ein anderes Konto vor. Es löst
[`2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md`](../../ueberholt/2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md)
ab (dort stehen die erledigten Punkte 1–9 der Vorgängerübergabe). Regelquelle bleibt [`CLAUDE.md`](../../../CLAUDE.md);
dieses Papier nennt nur, was die neue Sitzung darüber hinaus wissen muss. Abschnitt 6 ist der Einstiegs-Prompt.

**Zeitpunkt des Wechsels:** nach dem Push von KB-4 (#1095, Schemaschritt 228) und des Sitzungszweigs `kb5` am 14.10.2026 — alles
Übergebene liegt auf origin, kein Agent läuft mehr. Erste Aufgaben der neuen Sitzung: CI-Vermerk #1095 nach grünem Kern-Lauf, dann
KB-5b und die KB-5-Merge-Welle (Abschnitte 5.3 und 5.4). Scratchpad-Dateien der alten Sitzung sind verloren; alles Nötige steht in
diesem Papier und im Inventar [`2026-10-14_Uebergabe_KB5_Inventar.md`](2026-10-14_Uebergabe_KB5_Inventar.md).


## 1 Stand (14.10.2026, 07:30 UTC)

| Welle | Stand | Wo |
|---|---|---|
| KM‑K2, NK3, WK1, ZI1, KD1, KN1 (#1034–#1042) | fertig, gepusht, CI grün | Statusdatei |
| KB-3 Saat der Katalogkosten-Satzvorlagen (226), KB-2 Brennstoffpreise in der Trägerkarte (227) | fertig: #1079/#1080, Push `b67d9995`, CI-Lauf 38502511137 grün, Vermerk `c959f451`; Testdatenbank 227 | origin |
| KB-4 eine Kostenbasis (228 `GeraetekostenEntfallSchema`): KB-4a Heizkessel/BHKW, KB-4b Wärmepumpe/PV/Wechselrichter, KB-4c Solar/Puffer/Kältemaschine, KB-4d eine Leseklasse `KatalogkostensatzCtrl`, Prosa, Konzept; KB-4e sieben Wiki-Seiten | erledigt #1095: Merge `771603419`/`e8b0ec4ba`, Testdatenbank 228 `bba9ebff9`, Vollgate grün (24 841 Tests, Referenzlauf 37/37 gegen R55 PASS); Vorlagenplatzhalter des Berichts geprüft, ohne Gerätespalte | origin (Push 14.10.2026, CI-Vermerk offen) |
| KB-5 Stromspeicher-Kosten auf Position (229 `StromspeicherKostenSchema`) | KB-5a fertig und als Sitzungszweig `kb5` auf origin gepusht (Commits `6632a120c`, `a8220c221`, `b72fccff8`, `96fb796ea`, `4912b1643`: Schritt 229, Satzleser für vier Sätze, Rechenwege, Flotte, Tests; Kern-Filter und Windows-Schale 0 Fehler, SQL-Prüfer 0 gegen die 229-Kopie, Referenzlauf der 18 CI-Projekte gegen R55 PASS, Filterlauf grün bis auf die Schemastand-Wache); KB-5b (Abschnitt 5.3) und die Merge-Welle (Abschnitt 5.4) offen | origin, Zweig `kb5` (`git fetch origin kb5 && git worktree add .claude/worktrees/kb5 kb5`) |
| Schemakette (Kopfzeile der Statusdatei, gepusht `788c0c92d`) | 228 KB-4 (wir) → 229 KB-5 (wir) → 230 `KaeltebedarfsprofilVorlagenSchema` (KBP-2, Sitzung Dialoge/Cloud) → 231 `RaumnutzungDinWerteSchema` (DA-6, Gebäudesimulation) → 232 `BauteilkatalogAbschnittSchema` (BK-5/BK-6, Gebäudesimulation); 233 frei | origin |
| Referenzbasen | aktuell R55 `2026-10-14_R55_KaeltebedarfDezentral` (37 Projekte, Anlagendialog); R56 ist für KB-5 reserviert und nur nötig, wenn sich ein Wert ändert (die Testdatenbank trägt außer `Modulkosten` keine Stromspeicher-Kosten) — sonst bleibt R55; danach R57 SA4 | `Referenzlaeufe/` |

Die Zweige `kb4` und `kb4w` sind gemergt und gepusht; `kb5` liegt als Sitzungszweig auf origin (Stand oben). Weitere lokale Stände
der alten Sitzung gibt es nicht. Erledigte und
gemergte Zweige `kb2`, `kb3`, `kb4a`, `kb4b`, `kb4c` sind als Worktrees entfernt.


## 2 Geltende Entscheide des Anwenders

- **Eine Kostenbasis, keine Doppelhaltung (13.10.2026):** Preise nur über den Energiekosten-Dialog/Trägerkarte; Investitions-,
  Betriebs- und Energiekosten aller Anlagen allein in der Kostenverwaltung; Katalogsätze tragen je genau einen Verweis auf eine
  Satzvorlage des zentralen Kostenkatalogs; Abgleich nie automatisch; die Saat darf Anwenderdatenbanken idempotent füllen (nur Kosten
  > 0 ohne Verweis); Stromspeicher-Rechnung auf Position (KB-5); die Kostenseite zeigt Katalogkosten und bietet den Abgleich.
- **Zeitintervall-Einheit** Option (a) je Liste (#1040). **Logbuch:** bis 16.10.2026 keine neue Versionsnummer, kein Upload; neue
  Sätze stehen ohne Version im Entwurf `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`.
- **Modellwahl:** Fable 5.1 nur für Orchestrierung und sehr komplexe Analyse; Umsetzung Opus 5.5 (`opus-umsetzung`), Mechanik
  Sonnet (`sonnet-mechanik`), Prüfungen Haiku (`haiku-pruefung`); wöchentliche Nutzung sparsam.
- **KB-5, nach Empfehlung entschieden, vom Anwender zu bestätigen:** Verschleiß als Betriebsposition je entladener kWh
  (`EUR_PRO_KWH_ELEKTRISCH`; €/(kWh Nennkapazität · Vollzyklus) ist dieselbe Größe), eine Quelle und eine Buchung; der Fixanteil
  geht weiterhin nicht in die Speicherauslegung ein (heute `IFixEur = 0`); der Testwert 3999,5 €/kWh wird ergebnisneutral übernommen,
  obwohl er wie ein Absolutpreis aussieht; die Flotte liest ihre Einheit aus der Satzvorlage und schreibt bei der Übernahme
  Kostenpositionen der Anlage statt Katalogspalten; der Flottenstand `@Projektflotte` von 1046 bleibt unberührt.
- **Aus KB-5a und der Hebung auf 228, ebenfalls vom Anwender zu bestätigen:** (a) Die Flotten-Übernahme schreibt Kostenpositionen nur
  noch bei „eigene Kosten“ der Einheit (vorher schrieb sie die Katalogspalten immer); (b) die Verweise `ID_KostenVorlage` und
  `ID_KostenVorlageInvestition` gehen den Rückweg Kopie → Katalog nicht mit — ein aus einer Kopie angelegter Katalogsatz hat damit
  keine Kosten; Entscheid in KB-5b, analog zum Wechselrichter in KB-4b; (c) die Saat der Schritte 226/228 setzt an den gesäten
  Positionen keine `NutzungsdauerID`; Schritt 75 holt das nur nach, wenn eine Datenbank in einem Lauf gehoben wird (Testdatenbank 228:
  125 Positionen aus 226 tragen die Nutzungsdauer, die 8 Wechselrichter-Positionen aus 228 nicht; ohne Rechenwirkung) — Empfehlung:
  die Saat setzt die Nutzungsdauer selbst, als kleiner Nachzug in KB-5b.


## 3 Regeln und Fallstricke dieser Sitzung (über `CLAUDE.md` hinaus)

- **Umgebung der Cloud-Sitzung:** Das .NET-SDK fehlt im Container und die Microsoft-Hosts sind gesperrt. Einrichtung: Ubuntu-Pakete
  `dotnet-sdk-10.0` samt Abhängigkeiten mit `apt-get download` holen, mit `dpkg-deb -x` nach `/root/dotnet` entpacken, Symlink
  `~/.dotnet -> /root/dotnet/usr/lib/dotnet`, Alias `sdk/10.0.400 -> 10.0.112` für `global.json`; Aufruf `~/.dotnet/dotnet …` (die
  Gate-Skripte unter `Werkzeuge/Gate/` finden es selbst). `sqlite3` als Kommandozeile fehlt — Python `sqlite3` benutzen. Der Anwender
  ist gebeten, das Setup-Skript der Umgebung zu ergänzen (offen).
- **Datenträger:** Quote rund 40 GB. Jeder Agent löscht `bin/`/`obj/` am Ende; höchstens zwei Bauten gleichzeitig; erledigte Worktrees
  entfernen. Ein voller Kern-Testlauf dauert 60–120 Minuten; Hintergrundbefehle mit Zeitgrenze 7 200 000 ms starten und auf die
  Fertigmeldung warten — keine `sleep`-/`pgrep`-Schleifen (sie finden sich selbst).
- **Container-Neustarts** kommen vor: Agenten verlieren ihre laufenden Befehle, committete Stände bleiben. Deshalb Agenten in
  zusammenhängenden Schritten committen lassen und nach einem Neustart den Stand aus `git log` und dem Scratchpad lesen.
- **Stop-Hook-Meldungen** („unpushed commits“) sind kein Auftrag; gepusht wird nach Gate und Abnahme.
- **Statuszeile:** Nummer unmittelbar vor dem Commit gegen origin messen; Zeile mit vier Zellen; Schluss „CI-Kern-Lauf: ausstehend
  (Vermerk folgt).“; der CI-Vermerk kommt allein mit `[skip ci]`; Registrierungszeilen der Schemakette ebenfalls allein. Ein Push
  bricht den laufenden Kern-Lauf des Zweigs ab — Vermerke erst nach einem Lauf, der den eigenen Stand enthält.
- **Bildmesslatte der ChartProben:** weicht in diesem Container umgebungsbedingt ab (Schriften); die CI auf ubuntu ist der Nachweis.
- **Abstimmung mit den anderen Sitzungen** (alle am selben Zweig): Gebäudesimulation (R54, DA-6 = 231, BK-5/BK-6 = 232), Anlagendialog
  (R55, K6d-N, Word-Katalogfassung 23 — Kältekennzahlen und Vorlagenplatzhalter nur in Abstimmung), Dialoge und Korrekturen Cloud
  (KBP-2 = 230, Einheiten in `EnergietraegerPreiskarte`), Zapfprofilgenerator (ohne Nachrichtenkanal). Kanal in der alten Sitzung
  war `send_message` der Cloud-Werkzeuge; aus einem anderen Konto erreicht man die Sitzungen vermutlich nicht — dann gilt die
  Konvention: Kopfzeile „Schemaschritt angemeldet“ und die Commits auf origin sind die Meldung, Pushs mit Datenbankanhebung werden in
  der Kopfzeile mit Zeitfenster angekündigt.
- **Nachbarsitzungen wechseln ebenfalls das Konto** (Gebäudesimulation angekündigt am 14.10.2026): Die Kennung einer Sitzung steht im
  Commit-Trailer `Claude-Session:` ihrer Commits auf origin; die jüngste Kennung je Thema ist die gültige. Nachrichten per
  `send_message` erreichen nur Sitzungen desselben Kontos — sonst gilt die Kopfzeilen-Konvention.
- **Testdatenbank-Anhebungen** immer auf der Datei aufsetzen, die zuletzt auf origin liegt (andere Sitzungen ändern sie für ihre
  Basen); vor dem Heben origin mergen, die Datei dabei nie aus dem eigenen Zweig nehmen.


## 4 Offene Punkte (mit Empfehlung)

1. **KB-4** ist gepusht (#1095); offen ist allein der CI-Vermerk: Kern-Lauf (kern.yml) auf dem Push abwarten, bei Grün in der
   Statuszeile #1095 „CI-Kern-Lauf: ausstehend (Vermerk folgt)“ durch „CI-Kern-Lauf [<run-id>](https://github.com/inekon/EPOS-Plan/actions/runs/<run-id>) grün auf `<sha>`.“
   ersetzen, Commit allein mit `[skip ci]`, Push.
2. **KB-5b** (Abschnitt 5.3) und **KB-5 Merge-Welle** (Abschnitt 5.4): Hebung auf 229, Vollgate gegen R55; R56 nur bei Wertänderung.
3. **KB-6:** Konzept und Wiki-Quellen sind je Welle nachgeführt; Logbuchsätze ohne Version liegen im Entwurf; Bilder neu ziehen
   (`wikibilder.mjs`), Upload erst ab 16.10.2026 nach Freigabe und Versionsnummer.
4. **Vorlagenplatzhalter Kältemaschine** (#1035 Punkt 3): eigene Welle, Platzhalter mit der Sitzung Anlagendialog abstimmen
   (Katalogfassung 23 unverändert lassen).
5. **Wasserpreis am Rückkühlwerk:** gehört zur Sitzung Kälteanlagen; `Tab_Rueckkuehlwerk.Modulkosten` bleibt (Anwenderentscheid).
6. **Variantenliste:** gebaut als Gruppe „Katalogsätze“, umkehrbar über `KostenVorlagenCtrl.KATALOGSAETZE_GRUPPIEREN` — Anwenderentscheid
   nach Sichtabnahme.
7. **Sichtabnahme unter Windows** der Dialoge aus #1034–#1042 und der Kostenmasken aus KB-4 (Lesewert „Gerätekosten“/„Kostensatz“).
8. **`CLAUDE.md`, Abschnitt Regressionsnetz:** nennt noch R54 mit 36 Projekten, im Ordner liegt R55 mit 37 — die Sitzung Anlagendialog
   zieht das nach; nicht selbst ändern, um Konflikte zu vermeiden.
9. **Setup-Skript der Cloud-Umgebung** (SDK, siehe Abschnitt 3) — beim Anwender.
10. **Kältebedarfsprofile wie Wärmebedarfsprofile:** läuft als KBP-2 (230) in der Sitzung Dialoge und Korrekturen Cloud; die
    Projektkopie-Vormerkung aus NK3 (`Projektkopievormerkung`, `BedarfsProfileHuelle`) dabei erhalten.


## 5 Aufträge für die nächsten Wellen

Die Aufträge sind so formuliert, dass ein `opus-umsetzung`-Agent sie mit höchstens rund 150 Werkzeugaufrufen abarbeitet. Pfade sind
repo-relativ; `<scratchpad>` steht für das Scratchpad-Verzeichnis der jeweiligen Sitzung.

### 5.1 KB-4 Merge-Welle (Hauptbaum)

Voraussetzung: Bericht des KB-4d-Testlaufs liegt vor (alle Kern-Tests grün außer `TestdatenbankSchemastandWacheTests`, die erst nach
der Hebung grün wird); `kb4w` enthält `kb4`. Vorher der Gebäudesimulation und dem Anlagendialog das Zeitfenster des Pushs ankündigen.

Arbeitsort: Hauptbaum (Repo-Wurzel, Zweig `ios_migration_september`). Regelquelle `CLAUDE.md`. SDK `~/.dotnet/dotnet`. Nicht pushen, kein
CI-Lauf. `AGENT_LAEUFT` liegt schon (nicht neu anlegen, am Ende löschen). Commits sofort, genaue Pfade, Betreff ≤ 72 Zeichen, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Lange Läufe genau einmal im Hintergrund mit Fertigmeldung abwarten (keine
`pgrep`-/`sleep`-Schleifen); Gate-Hintergrundbefehl mit Zeitgrenze 7 200 000 ms. Bericht: Befund und Ergebnis, keine Abzüge.

#### 0. Vorbedingungen

1. `git fetch origin ios_migration_september`; `git merge --ff-only origin/ios_migration_september` (Hauptbaum sauber).
2. Prüfen: Testdatenbank `Referenzlaeufe/Kenndaten_Test.sqlite` auf **227** (LFS, > 100 MB; Stand R55 `8bf550004` oder neuer, oid
   `aee043d0…`), aktuelle Basis `Referenzlaeufe/2026-10-14_R55_KaeltebedarfDezentral` (37 Projekte). Weicht das ab (neuere Basis,
   neuere Datenbank anderer Sitzungen), damit arbeiten und im Bericht nennen; eine Datenbank, die schon über 227 steht, ist ein Befund —
   abbrechen und melden.
3. `GeraetekostenEntfallSchema.SCHRITT` auf `kb4` muss `BrennstoffpreiseTraegerkarteSchema.SCHRITT + 1` (= 228) sein.

#### 1. Merge

- `git merge --no-ff kb4w` (enthält `kb4` vollständig: KB-4 bis KB-4e). Konflikte beidseitig lösen: `Resource*.resx` beide Seiten, danach
  `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben`; `Status_iOS_Migration.md` beide Seiten; Testdatenbank **immer von
  origin** (`git checkout --theirs` wäre falsch — bei einem Konflikt an der Datei die Fassung von `ios_migration_september` behalten:
  `git checkout HEAD -- Referenzlaeufe/Kenndaten_Test.sqlite`), Referenzbasis-Ordner und `Referenzlaeufe/LIESMICH.md` beide Seiten;
  Quelltext mit Fachinhalt sorgfältig. Register nach dem Muster früherer Hebungen (`SchemaStand.cs`, `Paketanhebung.cs`,
  `EPOS.Kern.Tests/TestDatenbank.cs`, Schema-Tests) auf 228 setzen, falls `kb4` das nicht schon trägt.
- Build `~/.dotnet/dotnet build WP-Plan.Kern.slnf -c Release -nologo -v q -clp:ErrorsOnly` → 0 Fehler. Merge-Commit.

#### 2. Testdatenbank auf 228

- Mit `Werkzeuge/Testdatenbankschema` aus der 227-Fassung heben (Aufruf wie bei 226/227, siehe Nachtrag in `Referenzlaeufe/LIESMICH.md`).
  Danach `integrity_check` ok, `foreign_key_check` leer, `SchemaVersion` 228 (kein `sqlite3` auf dem Rechner — mit Python `sqlite3` lesen).
- Inhaltsvergleich gegen die 227-Fassung (alle Tabellen): erwartet sind der neue Verweis `ID_KostenVorlageInvestition` an
  `Tab_Wechselrichter(_STAMM)` samt Saat (Satzvorlagen je Wechselrichter mit Art `EUR_PRO_KW_ELEKTRISCH`, 480 €/kW), der Wegfall der
  Kostenspalten an BHKW, Heizkessel, Wärmepumpe, PV, Wechselrichter, Solarkollektoren, Pufferspeicher, Kältemaschine (Katalog und
  Projektkopie), der Sicherheitslauf der Saat (nur Kosten > 0 ohne Verweis), `Tab_Applikation.SchemaVersion`. Alles andere ist ein Befund.
- LFS prüfen (`git lfs ls-files -l`), Byte und SHA-256 notieren. Nachtrag „**Nachtrag — Schema 228 …**“ in `Referenzlaeufe/LIESMICH.md`
  hinter dem jüngsten Nachtrag, Muster der Nachträge 226/227. Commit „Testdatenbank auf 228 (KB-4 Gerätekosten entfallen)“.

#### 3. Abnahme

1. `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite` → 0 Fundstellen.
2. `python3 Werkzeuge/ResourceDesigner/designer_neu.py` (ohne Argument) → keine Abweichung.
3. Windows-Schale auf Linux (`-p:EnableWindowsTargeting=true`) → 0 Fehler.
4. Vollgate `bash Werkzeuge/Gate/gate_linux.sh . <ablage>` mit `<ablage>` = `<scratchpad>/kb4/gate`,
   einmal im Hintergrund (Zeitgrenze 7 200 000 ms). Erwartung: Build 0, alle Tests grün (die Bildmesslatte der ChartProben weicht in
   diesem Container umgebungsbedingt ab — kein Befund, nur nennen), Referenzlauf aller Projekte der Basis R55 GESAMT PASS. Die Kosten sind in
   Kapitalwert und Wirtschaftlichkeit: Die Saat rechnet die alten Beträge exakt zurück; eine Abweichung ist ein Befund, kein Einfrieren.
   Rote Tests nach Dateien aufschlüsseln; eigene beheben und committen (Nachlauf mit `--filter`), fremde benennen.
5. Konfliktmarker repoweit und `UU` → leer.

#### 4. Statuszeile, Logbuch, Vermerke (eigener Commit)

- Entwurf in `statuszeile_kb4.md` neben diesem Auftrag (Nummer `#NNNN`, Commits `…` ausfüllen). **Nummer unmittelbar vor dem Commit
  gegen `origin/ios_migration_september` messen** (`git fetch`, höchste `| **#NNNN** |` + 1). Gate-Satz mit Zahlen, Schluss „CI-Kern-Lauf:
  ausstehend (Vermerk folgt).“ Kopfzeile „Schemaschritt angemeldet“: 228 auf „gebaut mit #NNNN (Datenbank 228)“, 229 unverändert
  (Gebäudesimulation). Logbuchsatz ohne Version in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md` hinter den Sätzen zu #1079/#1080.
  Übergabepapier `Dokumentation/ueberholt/2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md`: KB-4 als „erledigt #NNNN“
  eintragen, offene Anwenderpunkte (Variantenliste Gruppe „Katalogsätze“, `Tab_Rueckkuehlwerk.Modulkosten` bleibt, KB-5 Stromspeicher)
  stehen lassen.
- Commit „Status #NNNN: KB-4 eine Kostenbasis, Schema 228“.

#### 5. Abschluss

- `~/.dotnet/dotnet build-server shutdown`; `AGENT_LAEUFT` löschen; Worktrees nicht entfernen.
- Bericht: Merge-SHAs, Testdatenbank (Byte, SHA-256, Inhaltsvergleich), Gate-Zahlen (Tests gesamt/grün/rot, Referenzlauf), rote Tests mit
  Einordnung, Statusnummer, offene Befunde. Nicht pushen.

### 5.2 KB-5a (läuft auf `kb5`)

Regelquelle `CLAUDE.md`. SDK `~/.dotnet/dotnet`. Arbeitsort: eigener Worktree `.claude/worktrees/kb5` (Zweig `kb5` auf `kb4`, HEAD
`3236ab27` — enthält 226/227/228, die Satzleserklasse `EPOS.Kern/Controller/KatalogkostensatzCtrl.cs` und origin bis R54). Commits sofort,
atomar, genaue Pfade, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; nicht pushen, kein CI-Lauf, Testdatenbank im Repo
nicht heben (Hebungen macht die Merge-Welle). Inventar mit Fundstellen: `Inventar_KB5.md` neben diesem Auftrag (zuerst lesen).

#### Fachlicher Rahmen (Anwenderentscheid 13.10.2026 „eine Kostenbasis, keine Doppelhaltung“)

- `Tab_Stromspeicher(_STAMM)` trägt die Verweise `ID_KostenVorlage` (Betrieb) und `ID_KostenVorlageInvestition` schon (208/209); die Saat 226
  (`KatalogkostenSaatSchema`, Komponente 5) hat Investitions-Satzvorlagen mit `EUR_PRO_KWH_KAPAZITAET` aus `Modulkosten` gesät (Testdatenbank:
  Vorlagen 59–63, Satz 3999,5, Stamm 1 und vier Projektkopien). `Leistungskosten`, `Investition_Fix`, `Verschleisskosten` sind in der
  Testdatenbank überall 0 oder NULL.
- Schritt 232 `StromspeicherKostenSchema`: Sicherheitslauf der Saat (idempotent, nur wo Kosten > 0 und kein Verweis — je Katalogsatz genau eine
  Investitions-Satzvorlage mit bis zu drei Positionen `EUR_PRO_KWH_KAPAZITAET` aus `Modulkosten`, `EUR_PRO_KW_LEISTUNG` aus `Leistungskosten`,
  `BETRAG` aus `Investition_Fix`, und genau eine Betriebs-Satzvorlage mit `EUR_PRO_KWH_ELEKTRISCH` aus `Verschleisskosten`, wo > 0), danach
  `DROP COLUMN` der vier Spalten an Stamm und Projektkopie (8 Spalten) nach dem Muster von `GeraetekostenEntfallSchema` (228): Register
  `StromspeicherSchema`/Fachspalten, Katalogfassung und Prüfsumme (`Katalogfassung.cs`, `Paketanhebung.cs`, neue Fassung wie in 228),
  `SchemaStand`, Schema-Tests (`StromspeicherKostenSchemaTests` neu, nach `GeraetekostenEntfallSchemaTests`).
  **Kette:** `SCHRITT = GeraetekostenEntfallSchema.SCHRITT + 1` mit Kommentar „angemeldet als 232; die Nummer wird beim Merge an die Vorgänger
  229–231 gehängt, falls kein Tausch“ — die Orchestrierung stimmt einen Tausch 229 ↔ 232 ab; die Klasse bleibt so, bis die Merge-Welle die Kette
  festlegt. Der Zielstand in `SchemaStand` folgt der Klasse.
- Einheit des Verschleißes: €/(kWh Nennkapazität · Vollzyklus) ist dieselbe Größe wie € je entladener kWh (ein Vollzyklus entlädt die
  Nennkapazität) — `EUR_PRO_KWH_ELEKTRISCH` (entladene kWh des Laufs, `EndenergieAufloeser`) ist die richtige Bemessung.

#### Rechenwege (KB-5a, dieser Auftrag)

1. **Satzleser erweitern:** `KatalogkostensatzCtrl` liefert für einen Stromspeicher (Katalogsatz oder Projektkopie) die vier Sätze
   c_cap [€/kWh], c_pow [€/kW], I_fix [€], c_ver [€/kWh entladen] aus der Investitions- und Betriebs-Satzvorlage; Rangfolge je Satz:
   Kostenposition der Anlage (`Tab_ProjektWerte` über `KostenPositionCtrl`, wie `SpeicherAuslegungCtrl.Modulkosten` :151 es heute schon tut)
   → Satzvorlage der Projektkopie → Satzvorlage des Katalogsatzes → fehlt: `null` mit benannter Datenlücke (nie still 0 setzen; der Rückfall
   c_ver 0,025 bleibt genau dort, wo er heute greift — Verhalten je Fall wie heute).
2. **`StromspeicherSimCtrl.LeseParameter`** (:1361–1580), **`PeakShavingCtrl`** (:348–350) und **`TechnikPlanwertCtrl.BasenFuellen`**
   (:461–472) lesen die vier Werte über den Satzleser statt aus den Spalten; `SpeicherAuslegungCtrl.Modulkosten` bleibt (liest schon Position),
   `Rechnung.cs:334` `IFixEur = 0` bleibt (Fix geht nicht in die Auslegung — unverändert).
3. **Doppelzählung Verschleiß:** Heute bucht der Lauf den Verschleiß als eigene Betriebskostenzeile aus c_ver × entladene kWh, und
   `TechnikPlanwertCtrl.cs:1362` warnt vor einer zusätzlichen Betriebsposition. Ziel: **eine Quelle, eine Buchung** — die Betriebsposition
   (Satz je entladener kWh) ist die Quelle von c_ver; die Buchung bleibt die heutige Zeile des Laufs (so bleibt das Ergebnis gleich), und die
   Kostenrechnung darf dieselbe Position nicht ein zweites Mal als Betriebskosten buchen: markiere die Verschleißposition (Bemessung
   `EUR_PRO_KWH_ELEKTRISCH` an Komponente 5) als „vom Lauf gebucht“ und schließe sie in der Kostenrechnung/`EndenergieAufloeser` von der
   zweiten Buchung aus; die Warnung in `TechnikPlanwertCtrl` wird zum Hinweis, dass die Position die Quelle ist. Test: ein Projekt mit
   Verschleißposition > 0 bucht den Betrag genau einmal.
4. **Flotte:** `SpeicherFlottenStudieCtrl.EinheitAusKatalog` (:334–354) füllt Modulkosten/Leistungskosten/Investition_Fix der Flotteneinheit
   über den Satzleser; die Übernahme (`…Uebernahme.cs:92–108`) schreibt nicht mehr in Spalten von `Tab_Stromspeicher` zurück, sondern legt
   oder aktualisiert die Kostenpositionen der Anlage (`Tab_ProjektWerte`, Investition je kWh / je kW / Betrag, Betrieb je kWh, über
   `KostenPositionCtrl`); `SQL_GERAET_NEU`/`SQL_GERAET_AENDERN` ohne die Spalten. Der Stand `@Projektflotte` von 1046 (JSON, `EigeneKosten=true`)
   bleibt unberührt.
5. **Kopierwege:** Katalog → Projektkopie (`StromspeicherCtrl`, `CopyFromStamm`), Projekt duplizieren, Komponentenübernahme: Platzhalter und
   Spaltenlisten ohne die vier Spalten, der Verweis wird mitkopiert (wie Wechselrichter in KB-4b).
6. **Tests:** Schema-Test 232 (Saat idempotent, Sicherheitslauf, DROP, Prüfsummen), Satzleser (vier Sätze, Rangfolge, Datenlücke), Rechenweg
   (ein Projekt mit Stromspeicher rechnet vor und nach der Umstellung gleich — Anker aus der Basis R55, Projekte 1007, 1046, 1051, 1065 tragen
   den Vaillant-Speicher mit 3999,5 €/kWh), Doppelzählung, Flotte (EinheitAusKatalog aus Satzvorlage, Übernahme schreibt Positionen).
   Bestehende Tests, die die Spalten nennen (rund 20 Dateien), nachziehen.

#### Nicht in diesem Auftrag (KB-5b, folgt)

Controller/Modell/Katalogregister des Stromspeichers (`StromspeicherStammCtrl`, `StromspeicherCtrl`, `KatalogRegistry`, `ParameterVerwendung`,
`KatalogBrowserProfil`, Importkarte, KI-Feldkatalog), Hüllen, Masken und Steckbrief (Lesewert „Gerätekosten“/„Kostensatz“ nach KB-4-Muster),
Ressourcen, Wiki, Konzept. **Aber:** Was in KB-5a nicht mehr kompiliert oder nicht mehr läuft, weil die Spalten fallen (SQL-Texte mit den
Spaltennamen in `StromspeicherStammCtrl`/`StromspeicherCtrl`, Modellfelder), wird in KB-5a so weit umgestellt, dass Build und Tests grün sind;
die Masken zeigen bis KB-5b notfalls kein Kostenfeld.

#### Abnahme KB-5a

- Build `WP-Plan.Kern.slnf` Release 0 Fehler; Windows-Schale auf Linux (`-p:EnableWindowsTargeting=true`) 0 Fehler.
- 232-Kopie der Testdatenbank heben (Vorbild: `kb4/heben.txt` im Scratchpad, `Werkzeuge/Testdatenbankschema` auf eine Kopie unter
  `<scratchpad>/kb5/`, Ausgang: die 228-Kopie `../kb4/Kenndaten_228.sqlite`);
  SQL-Prüfer gegen die 232-Kopie: 0 Fundstellen.
- Tests mit `--filter` auf Stromspeicher, Speicher, Flotte, PeakShaving, TechnikPlanwert, Kosten, Schema (`FullyQualifiedName~Stromspeicher|…`)
  grün; kein voller Lauf (den fährt die Merge-Welle).
- Referenzlauf der 18 CI-Projekte auf der 232-Kopie gegen R55 (`Referenzlaeufe/2026-10-14_R55_KaeltebedarfDezentral`, wie in `kb4/kette.sh`):
  GESAMT PASS für die 18 (die übrigen „nicht vorhanden“). Eine Abweichung ist ein Befund; melden, nicht einfrieren — R56 nur, wenn sich ein
  Wert ändern muss.
- `bin/`/`obj/` löschen, `build-server shutdown`. Bericht: Commits, Zahlen, Befunde, offene Punkte für KB-5b (Dateien mit Spaltennamen, die
  noch stehen). Höchstens rund 150 Werkzeugaufrufe.

### 5.3 KB-5b (nach KB-5a, Zweig `kb5` oder `kb5b` darauf)

Umfang: Controller, Modell und Katalogregister des Stromspeichers ohne die vier Kostenspalten (`StromspeicherStammCtrl` — rund 28
Fundstellen —, `StromspeicherCtrl`, `KatalogRegistry`, `ParameterVerwendung`, `KatalogBrowserProfil`, Importkarte, KI-Feldkatalog und
KI-Texte), Hüllen, Masken und Steckbrief nach dem KB-4-Muster (Lesewert „Gerätekosten“ bzw. „Kostensatz“ €/kWh gesperrt mit Vermerk
auf die Kostenverwaltung, `StromspeicherHuelle.cs` um Zeile 387), Ressourcen in beiden Sprachen (`designer_neu.py schreiben`),
Wiki-Quellen (`Programm Dokumentation - Stromspeicher.wiki`, Gerätekataloge, Kosten; eingebettete Hilfe `Stromspeicher.wiki`) und die
Konzeptpapiere (Konzept Wirtschaftlichkeit § 1.2/2.14a, Doku Mehrspeicher) auf den Stand „Stromspeicher wie die übrigen Gewerke“;
Tests nachziehen (rund 20 Dateien mit den Spaltennamen, Dialog- und Profilproben, `KiMaskenabdeckungWache` Eingabestellen).
Abnahme: Kern-Filter und Windows-Schale 0 Fehler, SQL-Prüfer 0 gegen die 229-Kopie, Tests mit `--filter` auf Stromspeicher, Speicher,
Flotte, Kosten, Ki, Katalog grün; Statuszeilen-Entwurf für KB-5 gesamt (Teilwellen 5a/5b, Commits), Logbuchsatz ohne Version,
Kopfzeileneintrag „229 — gebaut mit #NNNN (Datenbank 229)“.

### 5.4 KB-5 Merge-Welle (Hauptbaum)

Wie 5.1 mit diesen Abweichungen: Zweig `kb5` (bzw. `kb5b`) mergen; Testdatenbank von der jüngsten origin-Fassung (228) mit
`Werkzeuge/Testdatenbankschema` auf 229 heben; Inhaltsvergleich: Sicherheitslauf der Saat (in der Testdatenbank keine neuen Vorlagen,
weil die Stammzeile 1 und die vier Projektkopien schon verweisen und die übrigen Kosten 0 sind), Wegfall der acht Spalten an
`Tab_Stromspeicher(_STAMM)`, Katalogfassung und Prüfsumme, `Tab_Applikation.SchemaVersion`; Vollgate gegen die aktuelle Basis (R55);
GESAMT PASS erwartet. Weicht ein Wert ab, Befund melden; erst nach Anwenderfreigabe als R56 einfrieren (`Referenzlaeufe/LIESMICH.md`
begründen, Vorgängerbasis nach `Dokumentation/ueberholt/Referenzbasen/`). Danach Kopfzeile: 229 „gebaut mit #NNNN (Datenbank 229)“,
und die Sitzungen Dialoge/Cloud (230) und Gebäudesimulation (231/232) über den Push informieren.


## 6 Einstiegs-Prompt für die neue Sitzung

```text
Du übernimmst die Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, Kostenbasis“ von EPOS-Plan aus einem anderen Konto.
Repo inekon/EPOS-Plan, Arbeitszweig ios_migration_september. Regelquelle ist CLAUDE.md in der Repo-Wurzel; lies sie zuerst ganz.
Dann lies Dokumentation/aktuell/Dialoge/2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md vollständig (Stand, Entscheide, Regeln,
offene Punkte, Aufträge) und das Inventar Dokumentation/aktuell/Dialoge/2026-10-14_Uebergabe_KB5_Inventar.md nur nach Bedarf.

Vorgehen:
1. Stand feststellen, nicht aus dem Gedächtnis: git log --format='%h %<(72,trunc)%s' -n 30 origin/ios_migration_september, die Kopfzeile
   „Schemaschritt angemeldet“ und die letzten Statuszeilen in Dokumentation/aktuell/Status_iOS_Migration.md, die aktuelle Basis unter
   Referenzlaeufe/, den Schemastand der Testdatenbank (Python sqlite3: SELECT SchemaVersion FROM Tab_Applikation). Prüfe, ob KB-4 (228)
   und KB-5 (229) schon auf origin liegen (Statuszeilen, Klassen EPOS.Kern/Allgemein/Update/GeraetekostenEntfallSchema.cs und
   StromspeicherKostenSchema.cs) und hole den Sitzungszweig kb5 (git fetch origin kb5; git worktree add .claude/worktrees/kb5 kb5) — er trägt KB-5a fertig.
2. Umgebung einrichten, falls dotnet fehlt (Abschnitt 3 des Übergabepapiers: SDK aus Ubuntu-Paketen nach /root/dotnet, Symlink
   ~/.dotnet, Alias sdk/10.0.400); git lfs prüfen (Referenzlaeufe/Kenndaten_Test.sqlite > 100 MB, keine Zeigerdatei).
3. Arbeite die offenen Punkte in der Reihenfolge von Abschnitt 4 ab, mit den Aufträgen aus Abschnitt 5: Agenten nach CLAUDE.md
   (Modell je Aufruf ausdrücklich setzen: opus-umsetzung für Umsetzung, sonnet-mechanik für Merge ohne Fachkonflikt, Status, Wiki,
   haiku-pruefung für Prüfungen), eigener Worktree je Welle, AGENT_LAEUFT vor Arbeit im Hauptbaum, Wellenfolge Merge → Gate →
   Statuszeile und Logbuchsatz → Push → CI-Vermerk allein mit [skip ci]. Vor jedem Push mit Datenbankanhebung das Zeitfenster in der
   Kopfzeile ankündigen; nach dem Push den SHA dort eintragen. Logbuch bis 16.10.2026 ohne Versionsnummer, kein Upload.
4. Anwenderentscheide, die das Papier als „vom Anwender zu bestätigen“ führt (KB-5: Verschleiß, Fixanteil, Testwert 3999,5, Flotte),
   beim ersten Bericht kurz zur Bestätigung vorlegen; bis dahin nach Empfehlung weiterarbeiten.
5. Berichte knapp: Stand, Zahlen (Build, Tests, Referenzlauf), Befunde, nächste Schritte; keine Dateiabzüge. Antworten auf Deutsch.
```
