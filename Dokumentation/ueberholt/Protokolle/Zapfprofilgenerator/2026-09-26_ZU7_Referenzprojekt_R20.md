# ZU7 — Projekt 1045 als Referenzprojekt auf dem Generator, Basis R20 (26.09.2026)

Protokoll des Postens **#530**. Auftrag: ZU7 aus der Sperre lösen (terminiert seit N16, Nachtrag
25.09.2026, bis nach der Sichtabnahme Z1–Z5 und K5) — ein Referenzprojekt der Regressionsbasis auf
den Zapfprofilgenerator umstellen, eine neue Einfrierregel für seine gesäten Eingaben einführen und
die Basis unter `Referenzlaeufe/` als **`2026-09-26_R20_Zapfprofil`** neu einfrieren. Zweig `z7`,
Worktree `.claude/worktrees/z7`, Sonnet 5.

Der ursprüngliche Agent des Postens war durch ein Nutzungslimit abgebrochen; ein Nachfolger hat den
committeten Stand aufgenommen (Saatskript, Testdatenbank, R20-Ordner, Basisname in Workflows und
Papieren, teils schon uncommitted im Arbeitsbaum), ihn geprüft, den Merge von `origin` früh
nachgezogen (statt erst am Schluss), die Saat und die Basis auf dem gemergten Stand wiederholt, zwei
weitere Wachen repariert, die restlichen Papiere geschrieben und das Gate gezogen.

---

## 1. Commits

| Commit | Inhalt |
|---|---|
| `9f647118a` | Saatskript `referenzprojekt_zapfprofil.py`, gesäte Testdatenbank (Schemastand 145). Der Commit absorbierte über den geteilten Git-Index auch die vom Vorgänger vorbereitete Archivierung von R19 (Rename `protokoll.txt` nach `Dokumentation/ueberholt/Referenzbasen/`, Löschung der übrigen R19-Dateien) |
| `b25e59c7b` | `ZapfprofilReferenzprojektWacheTests` (neu), `ZapfprofilCtrlTests`/`ZapfprofilWeicheTests` angepasst, `ZapfprofilSpeichernTests.Zeilen()` zählt ohne die Zeilen des Referenzprojekts |
| `3bb4ede0d` | `CLAUDE.md`: siebte Einfrierregel „gesäte Zapfprofil-Eingaben“, Regressionsnetz auf R20 |
| `901fa7e4c` | Basisname R20 in `.github/workflows/{kern,ios}.yml` und den Papieren der Gebäudesimulation/Wirtschaftlichkeit/Basenhistorie; R20-Ordner als Zwischenstand (Schema 145) |
| `918e952d8` | Merge `origin/ios_migration_september` (`070574a4`) — einziger Konflikt `Referenzlaeufe/Kenndaten_Test.sqlite` (origin-Fassung `b02fa02e…` übernommen) |
| `d887b2d45` | Saat auf der gemergten Fassung wiederholt (Schemastand 148), R20 neu gerechnet; Saatskript checkpointet die WAL vor dem Schließen |
| `19c95dcbf` | `tww_testkatalog_fiktiv.py` kennt `REFERENZPROJEKT_GENERATOR = 1045` (sonst brach das Einspielskript ab bzw. die `ERWARTET`-Zähler stimmten nicht mehr) |
| `12b6379d3` | Papiere: `Referenzlaeufe/LIESMICH.md` (Aktuelle Basis R20, Entfernte Basen, Skripte-Tabelle), `Dokumentation/ueberholt/Referenzbasen/LIESMICH.md` (Zeile und „Basis R19 im Einzelnen“), Umsetzungskonzept (Kapitel 9, Nachtrag N24), `Dokumentation/LIESMICH.md`-Index |

---

## 2. Die Projektwahl

Von den sechs CI-Projekten (1030, 1007, 1017, 1045, 1046, 1047) trägt allein **1045** ein echtes,
unkompliziertes Warmwasserprofil: ein Gebäude (10651 „EFH-A-U-347s“, Einfamilienhaus, VDI-6007-Weg),
nicht gekoppelt, keine Kühlung, Bestandsprofil „EFH Wohnen, 1 Person“ (5,0 MWh/a) mit eigenem
Brauchwasser-Puffer, Wärmepumpe und Kessel — der Generator erreicht damit Speicher und Erzeuger.
Nicht 1030 (kein Gebäude), nicht 1017 (Kälte), nicht 1046 (Flottenstand eingefroren, SP‑O‑8), nicht
1047 (Anlagenkopplung AK1), nicht 1007 (sein Bestandsprofil „Haushalt-3“ ist ein Stromprofil, das als
Brauchwasser läuft — kein Vergleichsmaß für eine Warmwasserbilanz).

---

## 3. Die Saat

`Referenzlaeufe/Skripte/referenzprojekt_zapfprofil.py` setzt genau zwei Zeilen:

- `Tab_TwwProjekt`: `Weg = GENERATOR`, `Jahresreihe_Stochastisch = 0` (deterministische Bilanz),
  Seed 1045, 10 Realisierungen, Perzentil 99, Zirkulationsmethode Flächenkennwert (Anteil),
  Speicherart 1, Personen-Automatik, alle übrigen Spalten NULL.
- `Tab_TwwZone`: Nutzungsart „Wohnen groß (abgeleitet)“ am Gebäude 10651, Bilanzgrenze Zapfstelle wie
  der Bestandsweg (keine Zirkulation), 8,3 Personen (trifft den Bestandsweg-Jahresbedarf von
  5,0 MWh/a über das mittlere Niveau, umgerechnet auf das Kaltwasser-Jahresmittel der Parameter).

Keine Wohnungstypen, kein Konstruktor, keine Messreihe; die Bestandszeile in
`Z_Projekt_Brauchwasser` bleibt stehen und rechnet auf dem Generatorweg nicht mit (Weiche,
Umsetzungskonzept 2.2). Wiederholbar: ein zweiter Lauf meldet „0 Projektzeilen, 0 Zonen“,
`integrity_check` ok, `foreign_key_check` leer. Zellvergleich gegen die jeweilige Vorfassung (vor
und nach dem Merge): **nur die zwei neuen Zeilen plus zwei Zähler in `sqlite_sequence`**, Schema
unverändert (379 bzw. nach dem Merge 392 Objekte in `sqlite_master`).

---

## 4. Die siebte Einfrierregel

**„Gesäte Zapfprofil-Eingaben“** (`CLAUDE.md`, `Referenzlaeufe/LIESMICH.md`): Änderungen an
`Tab_TwwProjekt`/`Tab_TwwZone` eines Referenzprojekts, den benutzten `Tab_Tww*_STAMM`-Zeilen samt
Tagesgangsatz und den drei Kaltwasser-Parametern sowie das Umstellen eines Referenzprojekts auf den
Generator oder zurück erzwingen ein Neueinfrieren.

Gehalten von **`EPOS.Kern.Tests/ZapfprofilReferenzprojektWacheTests`**: jede gesäte Zelle der
Projektzeile und der Zone (inklusive `Aenderungsdatum`, das die Datenzugriffsschicht als `DateTime`
zurückgibt — die Wache vergleicht typgleich, nicht als Text), die Kennzeichen der benutzten
Nutzungsart samt Tagesgangsatz und die drei Kaltwasser-Parameter, und — als schärfste Probe — die
Generator-Bilanz von 1045: Jahresenergie auf 1e‑6 genau und die Stundenreihe Zeichen für Zeichen
gegen `waermebedarf_brauchwasser.csv` der aktuellen Basis.

**Kollateralschäden an drei bestehenden Testklassen, alle behoben:**

1. `ZapfprofilCtrlTests`/`ZapfprofilWeicheTests`: Die Aussage „kein Referenzprojekt auf dem
   Generator“ galt für alle sechs CI-Projekte; sie gilt jetzt für die übrigen fünf, 1045 wird
   ausdrücklich als Generator-Fall geprüft.
2. `ZapfprofilSpeichernTests.Zeilen()` zählte `Tab_TwwZone`/`-Wohnungstyp`/`-Projekt` **global** und
   nahm eine leere Datenbank an (mehrere Fälle prüften `Assert.Equal((0L,0L,0L), Zeilen())` als
   Ausgangs- oder Rollback-Zustand); mit der gesäten Zeile von 1045 zählt die Hilfsfunktion jetzt
   ohne dessen Zeilen (`WHERE ID_Projekt <> 1045`, referenziert über
   `ZapfprofilReferenzprojektWacheTests.PROJEKT`).
3. `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py` (Wache: `TwwKatalogWacheTests`) prüfte, dass
   `Tab_TwwZone`/`Tab_TwwWohnungstyp`/`Tab_TwwProjekt` **vollständig leer** sind — sowohl als
   „fremde Zeile“-Abbruchbedingung vor jedem Schreiben als auch als `ERWARTET`-Sollwert nach dem
   Lauf. Beide Stellen kennen jetzt `REFERENZPROJEKT_GENERATOR = 1045` und erwarten dort genau eine
   Zone und eine Projektzeile, keine Wohnungstypen.

---

## 5. Die neue Basis R20

**A/B gegen R19** (14 Projekte): **13/14 PASS und byte-gleich** (416/432 CSV byte-gleich); allein
1045 **FAIL** mit 16 von 32 Dateien (91.713 Abweichungen von 324.299 Werten) — der Bestandsweg
rechnete eine feste Jahresreihe, der Generator eine tagesgangbasierte Bilanz mit anderem
Stundenprofil bei nahezu gleicher Jahresenergie:

| Projekt, Datei, Größe | R19 | R20 |
|---|---|---|
| 1045 `aggregate.csv` `Energiebedarf.Waermebedarf_Brauchwasser` | 5,00 | 5,01 |
| 1045 `aggregate.csv` `Vektor.waermebedarf_brauchwasser.Summe` | 5.000 | 5.006,62138 |
| 1045 `aggregate.csv` `Energiebedarf.Waermebedarf_Gesamt` | 80,94 | 80,95 |
| 1045 `aggregate.csv` `Vektor.waermebedarf.Summe` | 80.940,6927 | 80.947,3141 |
| 1045 `aggregate.csv` `Waermepumpe.Deckung_Brauchwasser` | 3,65 | 3,66 |

Die zusätzlichen 6,62 kWh/a wandern in die Deckung der Wärmepumpe (`Heizkessel.Deckung_Brauchwasser`
bleibt bei 2,53 MWh/a); das andere Stundenprofil verschiebt zugleich, wann die Wärmepumpe Strom
zieht, und damit die stundenweise PV-Eigenverbrauchszuordnung (`pv_produktion.csv`,
`pv_reststrom.csv`, `pv_strombedarf.csv`, `pv_ueberschuss.csv`, `reststrom_viertelstunde.csv`) mit —
die theoretische Erzeugung (`pv_produktion_theoretisch.csv`) bleibt unverändert. Die übrigen
geänderten Dateien: `kessel_leistung.csv`, `kessel_restwaerme.csv`, `kessel_waermebedarf.csv`,
`waermebedarf.csv`, `waermebedarf_brauchwasser.csv`, `waermebedarf_dauerlinie.csv`,
`wp_produktion.csv`, `wp_strom.csv`, `wp_waermebedarf.csv`, `wp_warmwasserbedarf.csv`.

**Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet; NaN nur in den gewollten Lücken der
Vorlauf- und Rücklaufreihen von 1047 (wie in R19).

**Determinismus geprüft:** mehrere unabhängige Läufe auf demselben Stand — vor dem Merge (Schema
145), unmittelbar nach dem Merge (Schema 148) und ein dritter Lauf nach dem WAL-Checkpoint der
Testdatenbank — alle **14/14 byte-gleich**; der Einfrierlauf ist mit allen byte-gleich. Die sechs
CI-Projekte (1030, 1007, 1017, 1045, 1046, 1047) zusätzlich isoliert gegen den committeten R20-Ordner
geprüft: **GESAMT PASS (2.208.587 Werte)**.

R19 ist mit dieser Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll steht unter
`Dokumentation/ueberholt/Referenzbasen/2026-09-25_R19_BhkwNetzbezug/protokoll.txt`, ihre volle
„Aktuelle Basis“-Beschreibung samt aller Nachträge (144–148) als „Basis R19 im Einzelnen“ im selben
Ordner. `.github/workflows/kern.yml` und `ios.yml` zeigen jetzt auf R20.

---

## 6. Der frühe Merge

Auf Hinweis des Orchestrators wurde `origin/ios_migration_september` (Stand `070574a4`) **vor** der
endgültigen Berechnung von R20 gemergt, statt erst am Schluss — origin war inzwischen mit den Wellen
G6b (Zonenkopplung, Zoneneingang, Bauteilweg) und G7a (gbXML-Export, Baualtersklassen/Energiestandard,
Schemaschritt 148) sowie dem CI-Wächter (#531, Standardkultur en-US) vorangekommen. Einziger Konflikt:
`Referenzlaeufe/Kenndaten_Test.sqlite` (Git-LFS-Binärkonflikt) — die origin-Fassung (`b02fa02e…`,
Schemastand 148) wurde übernommen, die Saat auf ihr wiederholt (Abschnitt 3), R20 auf ihr neu
gerechnet (Abschnitt 5). Die fremden Wellen wirken auf keines der 14 Referenzprojekte — Zahlen und
Dateien sind gegenüber dem Zwischenstand vor dem Merge unverändert. Kern-Filter, gefilterte und
volle Testsuite laufen unter der seit #531 geltenden Standardkultur en-US unverändert grün (die
Zapfprofil-Testklassen pinnen keine Kultur und vergleichen ausschließlich mit `InvariantCulture`).

---

## 7. Abweichungen

| Nr. | Abweichung | Grund |
|---|---|---|
| A1 | Der Commit `9f647118a` trägt neben Saatskript und Testdatenbank auch die Archivierung von R19 (Rename/Löschung) | Beide Änderungen standen bereits gemeinsam im Git-Index (vom Vorgänger vorbereitet); `git commit` fasst den ganzen Index zusammen, ein `git add <pfad>` allein trennt bestehende Staged-Änderungen nicht. Inhaltlich beides korrekt und zusammengehörig (die R19-Archivierung ist Teil derselben Einfrierung) |
| A2 | Drei bestehende Testklassen/Skripte mussten wegen globaler „leere Datenbank“-Annahmen angepasst werden (`ZapfprofilCtrlTests`, `ZapfprofilWeicheTests`, `ZapfprofilSpeichernTests`, `tww_testkatalog_fiktiv.py`) | Erwartbare Folge der ersten Umstellung eines Referenzprojekts auf den Generator; keine davon war im ursprünglichen Auftrag benannt, aber notwendig für ein grünes Gate |
| A3 | Merge von `origin` **vor** statt nach der endgültigen R20-Berechnung | Abweichung vom ursprünglichen Auftragstext (der einen Merge erst am Schluss vorsah), auf ausdrücklichen Hinweis des Orchestrators während der Sitzung, um eine doppelte Neuberechnung zu vermeiden |
| A4 | Das Saatskript ließ beim ersten Lauf `Kenndaten_Test.sqlite-shm`/`-wal` liegen | WAL-Modus der Datenbank, Python schließt eine Verbindung ohne expliziten Checkpoint nicht sauber; das Skript checkpointet jetzt vor dem Schließen (`PRAGMA wal_checkpoint(TRUNCATE)`). Beide Dateien sind gitignored, kein Repository-Risiko, aber ein unsauberer Arbeitsbaum |

---

## 8. Gates

| Gate | Ergebnis |
|---|---|
| Kern-Filter (vor dem Merge, Schema 145) | 0 Fehler |
| gefilterte Tests Zapfprofil (vor dem Merge) | 4 Fehler (Aenderungsdatum-Kulturformat, `ZapfprofilSpeichernTests` ×3) — behoben |
| Kern-Filter (nach dem Merge, Schema 148) | 0 Fehler |
| gefilterte Tests Zapfprofil (nach dem Merge) | 263/263 erfolgreich |
| `TwwKatalogWacheTests` (nach dem Fund) | 1 Fehler — behoben, danach 11/11 |
| gefilterte Tests (Zapfprofil, Tww, Referenz, Wache, Dokumentations-/Wiki-/Ordnungswache) | 0 Fehler (EPOS.Kern.Tests 756, EPOS.UI.Tests 298, SpeicherEngine 12, SpeicherPlanung 1) |
| voller Testlauf `WP-Plan.Kern.slnf` | 0 Fehler (KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1 übersprungen, EPOS.UI.Tests 6.531, EPOS.Kern.Tests 8.033+1 übersprungen) |
| Windows-Schale (`EnableWindowsTargeting=true`, Debug x64) | 0 Fehler |
| `SqlDialektPruefer` | 1.988 SQL-Texte, 0 Fundstellen |
| `Auslieferungsvorlage.Tests` | 38/38 erfolgreich |
| Referenzlauf sechs CI-Projekte gegen R20 | GESAMT PASS (2.208.587 Werte) |
| Determinismus R20 (mehrfach) | byte-gleich |
| Arbeitsbaum | sauber (Arbeitskopie und WAL-Beidateien entfernt) |

---

## 9. Folgen

- **Push:** noch nicht erfolgt — Statuszeile #530 trägt den Push nach.
- **K5** (Freigabe eigener Messreihen für die Kalibrierung) bleibt offen; ohne Wirkung auf ZU7.
- **Kein Wiki-Satz, kein Logbuch-Eintrag:** keine sichtbare Bedienänderung — der Generator lief
  bereits vor ZU7, nur seine Referenzrolle in der Regressionsbasis ist neu.
- **Sichtabnahme** (Übergabe-Dokument, Abschnitt 16): Projekt 1045 öffnen, Zapfprofil zeigt die
  gesäte Zone „Wohnen“, die Rechnung läuft über den Generator.
