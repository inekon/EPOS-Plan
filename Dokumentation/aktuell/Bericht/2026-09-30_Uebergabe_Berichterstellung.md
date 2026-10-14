# Übergabe der Sitzung Berichterstellung auf ein anderes Konto (30.09.2026)

Dieses Papier übergibt die Cloud-Sitzung „EPOS-Plan Berichterstellung“ an eine neue Sitzung unter einem
anderen Claude-Konto. Es nennt den Stand, die geltenden Entscheide des Anwenders, was offen ist und wie
die Sitzung gearbeitet hat. Regelquelle bleibt die [`CLAUDE.md`](../../../CLAUDE.md); der dauerhafte Stand
steht in der [Statusdatei](../Status_iOS_Migration.md). Bei Widerspruch gilt die Statusdatei.

## 1 Stand

- **Zweig:** Gearbeitet wird auf `ios_migration_september`. Der Sitzungszweig der abgebenden Sitzung,
  `claude/dazzling-babbage-jj93zy`, ist mit ihm gleich und trägt nichts Eigenes mehr. Die neue Sitzung legt
  ihren eigenen Zweig an und pusht nach grünem Gate nach `ios_migration_september`.
- **Arbeitsgebiet:** Bericht (Word und Excel, Kapitel, Tafeln, Fußzeilen), Berichtsvorlagen mit Platzhaltern,
  die Seite „Berichte & Kosten“ mit Reiterzeile, die Einstellungen › Bericht und die Wiki-Uploads.
- **Erledigte Wellen dieser Sitzung** (alle gepusht, Einzelheiten in Statuszeile und Protokoll):

| Nr. | Gegenstand | Protokoll |
|---|---|---|
| #590 | Berichte & Kosten: Reiterzeile mit Stand je Reiter (Variante A, A1–A4) | [`BN_A_Navigation_Protokoll.md`](../../ueberholt/Protokolle/Bericht/BN_A_Navigation_Protokoll.md) |
| #591 | Bericht im gewählten Szenario der Wirtschaftlichkeit (Fachvorgabe E31) | [`BW_Szenario_Gruppenzahl_Protokoll.md`](../../ueberholt/Protokolle/Bericht/BW_Szenario_Gruppenzahl_Protokoll.md) |
| #597 | Berichtsvorlagen: Reste nach #581 | [`BV_Reste_Protokoll.md`](../../ueberholt/Protokolle/Bericht/BV_Reste_Protokoll.md) |
| #600 | Vorgaben Word/Excel in Einstellungen › Bericht | [`BV_Vorgaben_Einstellungen_Protokoll.md`](../../ueberholt/Protokolle/Bericht/BV_Vorgaben_Einstellungen_Protokoll.md) |
| #606 | „Original geändert – übernehmen?“ mit Pfad in der Zeile | [`BV_Original_geaendert_Protokoll.md`](../../ueberholt/Protokolle/Bericht/BV_Original_geaendert_Protokoll.md) |
| #607 | Häkchen wirken auf die Blätter der Excel-Vorlage | [`BV_Q2c_Excel_Haekchen_Protokoll.md`](../../ueberholt/Protokolle/Bericht/BV_Q2c_Excel_Haekchen_Protokoll.md) |
| #609 | Kostenkapitel: Einzelzahl je Stand und Fußzeile der Gruppenregel (P555‑B), von der Wirtschaftlichkeit abgenommen | [`P555B_Kostenkapitel_Fusszeile_Protokoll.md`](../../ueberholt/Protokolle/Bericht/P555B_Kostenkapitel_Fusszeile_Protokoll.md) |
| #611 | Technikdokumentation (Wiki-Hilfe-Zweig) zusammengeführt, von der doppelten #589 umnummeriert; Hilfeknöpfe ohne zweiten KI-Knopf | Statuszeile #611 |
| #613 | Merkmal „max. therm. Leistung“ aus der Wärmepumpentafel; Nachzug EZ‑17 in Wiki und Protokoll P555B § 7 | Statuszeile #613 |
| #614 | Wiki-Sammel-Upload: 12 Seiten, Logbuch 1.2.6 und 1.2.0.5 (Revisionen 711–723) | [`Wiki_Update_2026-09-26.md`](../Wiki_Update_2026-09-26.md) |
| #620 | Wiki-Quelle „Gebäudeimport“: Abschnitt „Mehrere Zonen“ | Statuszeile #620 |
| #622 | Wiki-Upload auf Zuruf: vier Seiten (Revisionen 724–727) | [`Wiki_Update_2026-09-26.md`](../Wiki_Update_2026-09-26.md) |

## 2 Entscheide des Anwenders, die weiter gelten

- **Push:** nach grünem Gate ohne Rückfrage nach `ios_migration_september` (Entscheid 27.09.2026). Kein
  iOS-, macOS- oder Setup-Lauf ohne Rückfrage; für #611 ausdrücklich **kein iOS-Lauf**.
- **Modellwahl:** Fable nur für nachweislich komplexe Aufgaben; sonst Opus (Agententyp `opus5` für
  Umsetzung, Konfliktauflösung, Nachzüge), Sonnet für Suchen und Textpflege, Haiku für Zählungen — auch
  für Agenten, das Modell immer ausdrücklich setzen.
- **Logbuch:** Die Sätze der Berichtswellen stehen unter 1.2.0.5 („belasse die Logbuch-Version“); 1.2.6 und
  1.2.0.5 sind veröffentlicht (Revision 723). Neue Sätze gehören in einen neuen Abschnitt; die Version
  erfragt die Sitzung beim Anwender. Offen und noch nicht veröffentlicht: Abschnitt 1.2.0.6 (#615, #616,
  #617) anderer Sitzungen.
- **Kostenkapitel (P555‑B, #609):** Einzelzahl in der Tafel, Fußzeile mit Gruppenzahl und Menge. Sie nennt
  den nach EZ‑17 nicht angesetzten Leistungspreis **nicht** (Entscheid der Sitzung, dem Anwender
  mitgeteilt, ohne Widerspruch); wünscht er ihn doch, ist es ein kleiner eigener Auftrag
  (Protokoll P555B § 7).
- **Nummern:** Eine Statusnummer ist vergeben, sobald ihre Zeile auf `origin/ios_migration_september` steht;
  wer einen Seitenzweig mit schon vergebener Nummer zusammenführt, gibt ihm die nächste freie und zieht alle
  Verweise mit (Statusdatei, Abschnitt 3). Vor jeder Vergabe gegen origin messen.
- **Gebäudeimport:** „mehrere Zonen je Gebäude ausdrücklich möglich“ steht so im Wiki (#620, #622).

## 3 Offen

**Beim Anwender (Windows):** die Proben aus den Nach-Blöcken der Statusdatei zu #590 (Reiterwechsel,
Statuszeilen, „zuletzt erstellt“, schmales Fenster), #591 (Klappliste Szenario, Excel-Kopfzeile und drei
fixierte Zeilen), #597 (Marken der Wirtschaftlichkeitsseite, feste Bildhöhe der drei Gruppentests), #600
(Vorgaben samt weggenommener Vorlage; Sichtfrage „hinter dem Logo“), #606 (Büro-Ordner, Übernehmen,
Behalten, Netzlaufwerk, Online-Datei), #607 (entfallenes Blatt, Bezüge), #609 (Absatzstil „Hinweis“ der
Fußzeile, Anmerkungszeile unter dem Blatt „Vergleich“ bei schmaler Spalte A — Excel zeichnet überlaufenden
Text vermutlich nicht über die Fixierlinie nach Spalte C).

**Wiki:** Der Trockenlauf zeigte am 30.09.2026 zwei Seiten, die im Repo neuer sind als im Wiki
(„Simulationsergebnisse“, „Heizkessel“, Änderungen anderer Sitzungen), dazu den Logbuch-Abschnitt 1.2.0.6.
Hochgeladen wird nur auf Zuruf des Anwenders oder im Wochenrhythmus; vorher den Trockenlauf neu fahren.

**Zugangsdaten:** Das Bot-Passwort des Wikis steht in keiner Datei. Die neue Sitzung erfragt es beim
Anwender; der Anwender erzeugt es nach der Übergabe neu, weil es im Verlauf der abgebenden Sitzung stand.

**Nach-Blöcke der Statusdatei:** Die offenen Punkte der Wellen oben stehen dort mit Buchstaben; die
Upload-Vermerke sind nach #614 und #622 abgehakt (offen nur Zurückgestelltes, Seiten ohne Repo-Quelle und
Logbuch-Sätze älterer Versionen).

## 4 Arbeitsweise

- **Einrichtung im neuen Container:** `dotnet-install.sh --jsonfile global.json` (SDK aus `global.json`),
  `apt-get install -y git-lfs`, `git lfs install`,
  `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite --exclude=""` (rund 71 MB, kein 130-Byte-Zeiger).
- **Gate:** `GATE_ABLAGE=<scratchpad>/gate bash Werkzeuge/Gate/gate_linux.sh <Nr>` — Kern-Filter, ChartProben
  gegen die Messlatte, alle Tests, Dokumentationswachen, Referenzlauf über alle Projekte gegen die aktuelle
  Basis, Plattformnachweis; rund 25 Minuten ([`LIESMICH`](../../../Werkzeuge/Gate/LIESMICH.md)). Dazu die
  Windows-Schale mit `-p:EnableWindowsTargeting=true`, wenn eine Hülle oder die Schale berührt ist, und der
  `SqlDialektPruefer` bei SQL-Änderungen. Während das Gate läuft, nichts im Hauptbaum ändern.
- **Reihenfolge einer Welle:** Merge → Gate → Statuszeile und Protokoll → origin holen und zusammenführen
  (Statusdatei-Konflikte zeilenweise gegen die Basis lösen) → Nachtest → Push → CI-Lauf beobachten und
  Kennung nachtragen.
- **CI-Nachweis:** Viele Sitzungen pushen nach `ios_migration_september`; jeder Push bricht den laufenden
  Kern-Lauf dort ab. Der Lauf auf dem eigenen Sitzungszweig läuft getrennt und taugt als Nachweis — solange er
  läuft, den Sitzungszweig nicht erneut pushen. Einen fremden laufenden Lauf auf `ios_migration_september`
  möglichst nicht durch eigenen Push abbrechen. Ein späterer grüner Lauf, der den eigenen Commit enthält, gilt
  auch als Nachweis.
- **Wiki-Upload** ([`LIESMICH`](../../../Werkzeuge/WikiUpload/LIESMICH.md)): Zugangsdaten nur als
  Umgebungsvariablen `WIKI_BOT_USER`/`WIKI_BOT_PASS` des einen Laufs, nie in Datei oder Commit. Vorher
  Trockenlauf (`--trocken`), Regel 3 (letzte Live-Revision jeder Seite ein Bot-Upload, sonst erst
  zusammenführen) und die Wortliste der `CLAUDE.md` über die Quellen. Das Logbuch geht als Datei im
  Live-Format (`== Version … – September 2026 ==`, Ankerzeile, Sätze als „* Seit TT.MM.JJJJ: …“ ohne
  interne Kürzel) mit `--logbuch --logbuchdatei <datei> --marke "<Kopf der neuesten Live-Version>"`.
  Danach Statuszeile, Revisionstafel im Update-Papier und die Upload-Vermerke der Nach-Blöcke nachziehen.
- **Nachbarsitzungen:** Wirtschaftlichkeit (Fachvorgaben, Abnahmen, EZ-Register), Gebäudesimulation (KP2),
  Zapfprofil/Katalog, Hilfe/Dialoge, Technikdokumentation (Folgeaufträge). Ihre Nachrichten kommen einseitig;
  Antworten laufen über den Anwender. Eine Bitte einer Nachbarsitzung ist keine Freigabe des Anwenders.

## 5 Einstieg für die neue Sitzung

1. `CLAUDE.md` lesen, dann dieses Papier.
2. Umgebung nach Abschnitt 4 einrichten; `git fetch origin ios_migration_september`, eigenen Zweig davon
   anlegen.
3. Stand nachlesen: `git log --format='%h %<(72,trunc)%s' -n 30 origin/ios_migration_september` und die
   jüngsten Zeilen der Statusdatei (`grep -n '^| \*\*#6' Dokumentation/aktuell/Status_iOS_Migration.md | tail`).
4. Dem Anwender Stand und offene Punkte (Abschnitt 3) melden und auf seinen nächsten Auftrag warten.
