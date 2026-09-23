# Z0 — Zapfprofilgenerator: Grundlagen und Schema (Protokoll, 23.09.2026)

Statuszeile #438 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md);
Auftragsblatt Anhang A und die Nachträge N2 und N3 im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md); Quellen im
[Quellendossier](../../../aktuell/Zapfprofilgenerator/Quellendossier_Zapfprofilgenerator.md).
Zweig `z0` von `90225f59`, Stand des Gates `4ddebed9`.

## Auftrag

Stufe Z0 des Umsetzungskonzepts nach dem Auftragsblatt (Anhang A, Posten P1–P14; Entscheid N1 vom
23.09.2026 „nach Empfehlung"): Datenmodell des Zapfprofilgenerators in Kern und Testdatenbank,
lesbar über Controller, geschützt durch Wachen — ohne dass sich ein Rechenergebnis ändert. P14
(Konzept V2, Bereinigung der Grundlagenpapiere, Ersatz der Digitalisate) hängt an K8 und ist nicht
Teil der Abnahme. Implementierung und Gegenprüfungen je Postengruppe durch Agenten mit `model: opus`
im Worktree `z0`, ohne Push und ohne CI-Lauf.

## Posten und Commits

| Posten | Inhalt | Commits |
|---|---|---|
| P1 | Wache `Normzahlen_stehen_im_gitignore` in `RepositoryOrdnungWacheTests` (ZU11) | `6628e386`, Nachbesserung `436c3501` |
| P2 | `TwwSchema.cs`: DDL der zehn Tabellen, STRICT, Provenienzgruppen; `AUTOINCREMENT`, Bereichsprüfungen, vier Indizes | `3adc123c`, `bdf6d057` |
| P3 | Schemaschritt T1 in `SchemaMigration`, `SchemaStand.Zielversion` 102 | `b218b319` (als 101), `5e96609b` (auf 102) |
| — | Merge `ios_migration_september` (Schritt 101 der Wirtschaftlichkeit, e7) | `f8058c33` |
| P4 | `TestDatenbank.cs`, `Werkzeuge/Testdatenbankschema`, Testdatenbank auf 102 (LFS), fiktiver Testkatalog, Nachtrag in `Referenzlaeufe/LIESMICH.md` | `f1f00a54`, `1fcfcb4d`, `a5ed58cc` |
| P5 | `Parametersatz` aus `Tab_TwwParameter_STAMM`, `Provenienz.cs` | `bdd22ee3` |
| P6 | `ZapfprofilCtrl` lesend: `Weg`, `Verfuegbar`, `Katalog`, `Lies`, `Parameter` | `f93145ec` |
| P7 | `TwwNutzungsartCtrl`: Katalogpflege mit Sperren, „Speichern unter", `TagesgangSpeichern`, Provenienz je Wertgruppe, Rasterregeln | `3ca38a99`, `b4363954` |
| P8 | `KatalogRegistry` um `TWW_NUTZUNGSART`, `TWW_TAGESGANGSATZ`, `TWW_BEDARFSTAG`; Filterprofil; Sperre und natürlicher Schlüssel | `e2f9da05`, `844d592c` |
| P9 | Kopierstellen `ProjektDuplizierenCtrl` und `ProjektExportImportCtrl` | `ae876599`, `f12e21c1` |
| P10 | Auslieferungsvorlage: Schritt 3c, Prüfposten, `--katalogpaket`; Vorlagenprobe P6 auf 129 STRICT-Tabellen | `a1faa990`, `ca12cc9b`, `4ddebed9` |
| P11 | Wache `TwwKatalogWacheTests` | `57e63591`, `b0055988` |
| P12 | `SqlDialektPruefer` nach jedem neuen SQL-Text | in den Posten |
| P13 | Quellendossier samt Indexzeile | `a606be0c` |
| — | Abschlusspapiere: Setup-Konzept 6.1; N2, Kapitel 7, 9 und Anhang A im Umsetzungskonzept; dieses Protokoll; Statuszeile | `5422f3c1`, `9d7678ed`, `f9f695d9`, `d5af9c7a`, `20394b2b`; nach der Papierprüfung `2ece06e8` (N3) |

Alle Commits tragen den Trailer des arbeitenden Modells. Die Schemanummer war zu Beginn auf `z0`,
`main`, `origin/ios_migration_september` und allen Worktrees 100; T1 lief zuerst als 101 und wurde nach
dem Merge des Arbeitszweigs, der 101 an die Wirtschaftlichkeit gab, auf 102 umnummeriert.

## Gates

Im Worktree `z0` auf `4ddebed9`:

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Voller Testlauf `WP-Plan.Kern.slnf` mit den xUnit-Schaltern: 10 853 grün, 1 übersprungen.
- Werkzeugtests `Werkzeuge/Auslieferungsvorlage`: 26/26.
- Windows-Schale (`WindowsFormsApplication1.csproj`, Debug, x64): 0 Fehler — der Schemaschritt liegt
  in der Schale.
- `SqlDialektPruefer` gegen die Testdatenbank: 1 622 Texte, 0 Fundstellen.
- Auslieferungsvorlage `--trocken` gegen die Testdatenbank: 0 Auffälligkeiten; Schemastand 102,
  129 STRICT-Tabellen, alle 19 Zeilen des fiktiven Katalogs fallen, Fremdschlüssel eingeschaltet.
- Referenzlauf 1030, 1007, 1017, 1045, 1046 gegen `2026-09-22_R11_Bestandsbefunde`: PASS,
  byte-gleich. Kein Referenzprojekt setzt die Weiche; die Basis bleibt.

Papierstand im Worktree `z0` auf `2ece06e8` (nach dem Merge `97109834` und der Papierprüfung; seit
`4ddebed9` nur Papiere):

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Wachen (`FullyQualifiedName~Wache`, darin `DokumentationLinkWacheTests` samt Indexzeilen und
  `RepositoryOrdnungWacheTests`): 150/150 grün.
- `ChartProben`: 122 Bilder, 0 Verstöße (kein Renderer berührt; der Lauf ergänzt das Gate).
- Linkprobe über `Dokumentation/`: keine Fehler unter `aktuell/`; Markdown ohne BOM, CR = LF.

Das Gate auf dem Merge-Stand in `ios_migration_september` gehört zum Merge und steht noch aus.

## Gegenprüfungen

Die Posten P1–P4 und P7–P11 wurden nach der Umsetzung in drei Prüfgruppen von einem zweiten Agenten
gegengeprüft; nachgebessert wurde in eigenen Commits. P5 (`Parametersatz`) und P6 (`ZapfprofilCtrl`)
gehörten zu keiner der drei Prüfgruppen; eine eigene Gegenprüfung ist nicht belegt (offener Punkt). Gezählt sind die nachgebesserten Befunde mit ihrer Nummer aus der
Gegenprüfung. Schwereverteilung der drei Gegenprüfungen (aus den Workflow-Journalen der
Orchestrierung): Prüfgruppe P1–P3 zehn Befunde (1 hoch: Schrittnummer 101 doppelt mit e7;
3 mittel; 6 gering), Prüfgruppe P7/P8/P4 elf Befunde (1 hoch: Dublettendialog löschte benutzte
Tww-Zeilen; 3 mittel; 7 gering), Prüfgruppe P9–P11 elf Befunde (0 hoch; 4 mittel; 7 gering) —
zusammen 32 Befunde, 2 hoch, 10 mittel, 20 gering. Beide „hoch" sind behoben (Schritt 102;
Sperre und Rollback in der Katalogbereinigung); die nicht nachgebesserten Befunde sind
Prozesshinweise oder vorbestehend (Beidateien der Testdatenbank, ZU18).

| Posten | Nachgebessert | Wichtigste |
|---|---|---|
| P1 | 1 (Befund 6) | Proben für Unterordner und fremde Endungen, damit eine spätere Gegenregel mit `!` auffällt |
| P2 | 3 (Befunde 3, 8, 9) | `AUTOINCREMENT` für unveränderliche Versionen; CHECK an Ferienspalten und Realisierungen; Indizes |
| P3 | 1 (Befund hoch) | T1 von 101 auf 102 umnummeriert (`5e96609b`) |
| P4 | 1 (Befund 9) | die fiktive Kaltwasser-Bezugstemperatur fiel mit einem normativen Wert zusammen |
| P7 | 3 (Befunde 3, 4, 10) | `TagesgangSpeichern` in einem Vorgang; Provenienz je Wertgruppe beim Ändern; `RasterUngueltig` |
| P8 | 5 (Befunde 1, 2, 5, 6, 8) | Sperre benutzter Zeilen auch in der Katalogbereinigung; Löschen in einem Vorgang mit Rollback; Dublette über Bezeichner und Katalogversion |
| P9 | 4 (Befunde 2, 3, 6, 7) | fehlende Katalogzeile im Paket wird benannt abgelehnt statt still auf eine fremde ID zu zeigen; Satz nur bei Bedarf; `Beleg` und `Freigabe` reisen nicht |
| P10 | 3 (Befunde 4, 5, 9) | `ReadOnly = 1` für Auslieferungszeilen; Nennung des Beispielpakets; ZU11 als echter Posten |
| P11 | 2 (Befunde 1, 8) | Wache verlangt `EIGEN` und `FIKTIV` zugleich samt Quellentext; Skriptlauf mit Frist |

Summe: 23 nachgebesserte Befunde in neun Posten; P5 und P6 ohne Nachbesserung.

## Abweichungen vom Papier

Die Abweichungen (a)–(m) stehen im Nachtrag N2 des Umsetzungskonzepts, die Berichtigungen dazu aus der
Prüfung der Abschlusspapiere (a)–(d) im Nachtrag N3; beide sind im Hauptteil mit Verweis berichtigt
(3.1, 3.2, Kapitel 6). Kurz: T1 ist Schritt 102; IDs mit `AUTOINCREMENT`;
`Realisierungen_Auslegung` nullbar mit CHECK (≥ 1); zusätzliche UNIQUE-, NOT-NULL-, CHECK- und
Indexfestlegungen; `.gitignore` mit Stern und Ausnahme für das LIESMICH; Controllerregeln für
Tagesgang, Provenienz und Raster; Schalter der Katalogdefinition; fiktiver Testkatalog mit 19 Zeilen;
Mitnahme fehlender Katalogzeilen beim Projektimport mit Status `IMPORT`; Tww-Regel und Katalogpaket
der Auslieferungsvorlage; verschärfte Wache; Vorlagenprobe auf 129 STRICT-Tabellen; vorbestehende
`-shm`/`-wal`-Dateien neben der Testdatenbank.

**Befund zur Mitnahme fehlender Katalogzeilen (3.2).** Vor P9 reisten die Tww-Kataloge über `fill/`
mit der Original-ID; fehlte die Version am Ziel, zeigte die Zone still auf eine fremde Zeile gleicher
ID. Jetzt wird über den natürlichen Schlüssel aufgelöst, eine fehlende Zeile mitgenommen oder der
Import benannt abgelehnt. Das Duplizieren eines Projekts lief schon über die deklarierten
Beziehungen richtig; `Tab_TwwWohnungstyp` und die vier Katalogverweise sind nun ausdrücklich
eingetragen.

## Offene Punkte

- **ZU16–ZU18** (Kapitel 9, Empfehlung, Entscheid offen): Ersetzen vorhandener Auslieferungszeilen
  durch das Katalogpaket; Inhaltsvergleich namensgleicher `EIGEN`-Zeilen beim Import (Z1); Umstellen
  einer oder mehrerer Testklassen (noch aufzuspüren), die die Repo-Testdatenbank direkt öffnen.
- **P14** ruht bis zum Ergebnis von K8; **K1**, **K8** und **ZU15** liegen beim Anwender.
- Bedarfstage und Parameter haben keinen Schreibweg; `ZapfprofilCtrl.Speichern` und `Eingang` folgen
  in Z1.
- Der Auslieferungskatalog bleibt leer, bis ein Katalogpaket nach K8 vorliegt.
- Kapitel 6 (e): Erweiterung der `WikiProduktdatenWacheTests` auf die Tww-Katalogtexte nicht in Z0
  (Stufe Z4, N3 (c)).
- Gegenprüfung von P5 und P6 nicht belegt (siehe „Gegenprüfungen").
- Die Aufrufzeile der Hilfe der Auslieferungsvorlage (`Werkzeuge/Auslieferungsvorlage/Argumente.cs`)
  nennt `[--katalogpaket <ordner>]` nicht, anders als das Setup-Konzept 6.1; die Option steht nur in
  der Optionsliste — eigener Folgeposten.
