# Übergabe der Sitzung „Zapfprofilgenerator Cloud“ (14.10.2026)

Dieses Papier übergibt die Sitzung „Zapfprofilgenerator Cloud“ (`session_017jJMrbTBmVq3vxUtLN4evQ`,
Sitzungszweig `claude/zapfprofilgenerator-cloud-tetz98`) an eine Sitzung auf einem anderen Konto. Es nennt
den Stand, die geltenden Entscheide, die Regeln der Sitzung über `CLAUDE.md` hinaus und die offenen Punkte;
der Einstiegs-Prompt steht in Abschnitt 5. Regelquelle bleibt `CLAUDE.md`. Den Fachstand des Generators
(Stufen Z0–Z5, Folgeposten, Bestände außerhalb des Repositoriums, offene Entscheide aus der Zeit davor)
führt weiter [`2026-09-23_Uebergabe_Zapfprofilgenerator.md`](2026-09-23_Uebergabe_Zapfprofilgenerator.md),
Abschnitt 17; er wird hier nicht abgeschrieben. Die laufende Überarbeitung beschreibt das
[Konzept Zapfprofil — Benutzerführung](../Konzept_Zapfprofil_Benutzerfuehrung_EPOS-Plan.md).

## 1 Stand

Die Überarbeitung „Zapfprofil — Abbrüche beseitigen, Benutzerführung und Handhabung verbessern“
(Anwenderauftrag 13.10.2026) ist bis auf die Welle W6 umgesetzt, auf `origin/ios_migration_september` und
CI-grün.

| Welle | Inhalt | Statuszeile | Protokoll unter `Dokumentation/ueberholt/Protokolle/Zapfprofilgenerator/` |
|---|---|---|---|
| W1 (ZB1) | Abbrüche beseitigt: `Zonenvorpruefung`, Bearbeitung ohne Ausnahme | #1043 | `2026-10-13_ZB1_Abbrueche_ohne_Ausnahme.md` |
| W2 (ZB2, ZB4 a, ZB6, ZB10) | neue Zone geführt, waagerechte Wahl, Wohnungstabelle, Anzeigeeinstellungen | #1044 | `2026-10-13_ZB2_Fuehrung_Wahl_Wohnungen.md` |
| W5 (ZB7, ZB11) | DIN-4708-Werte über den Katalogimport, Paketvorlagen DIN 4708 und Typtage VDI 4655 | #1045 | `2026-10-13_ZB7_ZB11_DIN4708_Typtage_Vorlagen.md` |
| W3a (ZB5) | Schemaschritt 224 `TwwZonenferienSchema` (beliebig viele Ferien je Zone) | #1062 | `2026-10-13_ZB5_Zonenferien_Schema.md` |
| W3b (ZB5) | Karte „Kalender“ mit Ferienliste und Jahresraster | #1063 | `2026-10-13_ZB5_Kalenderkarte.md` |
| W4 (ZB3, ZB4 b, ZB8, ZB9) | Karten mit Zusammenfassung, Gruppe „Gebäude und Anlage“, haftende Vorschau, ein Feld je Wert | #1064 | `2026-10-13_ZB3_ZB4b_ZB8_ZB9_Karten.md` |
| W7 (ZB12) | Referenzprojekte 1066–1071 je Zapfprofil-Option, Saatskript, Wache, CI auf achtzehn Projekte | #1065 | `2026-10-13_ZB12_Referenzprojekte_Optionen.md` |
| **W6** | **Wiki-Seite „Brauchwasser-Zapfprofil“ und Logbuch 1.2.0.11** | — | **offen**, Abschnitt 4 |

- **Letzte Stände dieser Sitzung auf origin:** Integration `5f06ee8a1` (W3a, W3b, W4, W7, Testdatenbank 224
  mit 1066–1071), Anmeldezeilen `a953f0454`, CI-Vermerk `8b90dc5e0`. CI-Nachweis: Kern-Lauf 38465767292 grün
  auf `5f06ee8a1` (Sitzungszweig).
- **Seither bei den Nachbarn:** 225 `ZonenLastfelderSchema` hängt an 224; die Basen R54 (Dachflächen) und R55
  (Kältebedarf dezentral, Projekt 1072) sind eingefroren, R53 liegt unter `ueberholt/Referenzbasen/`. Die
  CI-Auswahl bleibt bei achtzehn Projekten, darunter 1066–1071.
- **Lokal:** keine Worktrees, keine laufenden Agenten, keine Routinen dieser Sitzung.

## 2 Geltende Entscheide des Anwenders (13./14.10.2026)

| Kennung | Entscheid |
|---|---|
| ZB‑Q1 A | Zonenblock als Karten mit Zusammenfassungszeile |
| ZB‑Q2 A | Kalender aufklappbar im Zonenblock (wie im Gebäudedialog) |
| ZB‑Q3 B | beliebig viele eigene Ferien je Zone, mit Schemaschritt (224) |
| ZB‑Q4 A | DIN-4708-Werte über den Katalogimport und eine Paketvorlage |
| ZB‑Q5 | keine Vorbelegung |
| ZB11 | Paketvorlage „Typtage VDI 4655“ |
| ZB12 | je ein Referenzprojekt für alle sechs Optionsgruppen, alle in der CI |
| Logbuch | Version **1.2.0.11** (1.2.0.8 bis 1.2.0.10 sind am 10.10.2026 veröffentlicht) |
| Wiki | Upload am **20.10.2026** auf wiki.epos-plan.de; keine Erinnerung in der alten Sitzung |
| Preiseinheiten | die Vorbelegung €/kWh, €/kW … gehört der Sitzung „Dialoge und Korrekturen“, nicht dieser |
| Modellwahl | Fable 5 nur für sehr komplexe Aufgaben und die Orchestrierung; sonst Opus 5.5, Sonnet und Haiku je nach Aufgabe, auch für Agenten |

Stehende Anweisungen des Anwenders, wörtlich: „nach grünem Gate auf ios_migration_september ohne Rückfrage
committen, mergen und pushen (kein Force, keine Tags, nie main); iOS-, macOS- und Setup-Läufe nie ohne meine
Freigabe; keine Normzahlen (Original) und keine Messreihen ins Repo; Testdatenbank nur über wiederholbare
Saatskripte ändern und Änderungen den Nachbarsitzungen vorher melden.“ Dazu: „Nehme Kern-Läufe vor, wenn
notwendig und sinnvoll.“ K5 (eigene Messobjekte) ruht.

## 3 Regeln der Sitzung (über `CLAUDE.md` hinaus)

- **Nachbarsitzungen:** Gebäudesimulation `session_01NPHEwQvnQDiWMWnQbMmEou`, Anlagendialog
  `session_01UNeGChCYZu2EpgFx4P3q1q`, Dialoge und Korrekturen `session_01Mt7MsJGzcGr77yqBsxwpLj`. Ihre
  Nachrichten kommen als Benachrichtigung. Geantwortet wird mit `send_message`, wo das Werkzeug da ist; sonst
  über die Anmeldezeilen im Kopf der Statusdatei (Worktree von origin, eine Zeile ändern, Commit mit
  `[skip ci]`, Push) oder — nur mit Zustimmung des Anwenders — über eine einmalige Routine in deren Sitzung.
- **CI-Nachweis:** Jeder Push auf den Arbeitszweig bricht den laufenden Kern-Lauf dort ab. Nachweis ist der
  Lauf auf dem Sitzungszweig mit demselben Stand; ist er abgebrochen, über
  `gh api -X POST repos/inekon/EPOS-Plan/actions/runs/<id>/rerun` neu starten. Den Lauf auf dem
  Sitzungszweig nicht abbrechen.
- **Gate:** Kern-Tests in zwei parallelen Blockströmen mit `Werkzeuge/Gate/kern_bloecke.sh`
  (`AB CD EF GH IJ K` und `LM NO PQ R S TUVWXYZ`, rund 55 Minuten), dazu die vier übrigen Testprojekte, die
  Werkzeugtests (`Formularkarte`, `Auslieferungsvorlage`, `Gebaeudevergleich`, `ZapfprofilValidierung`),
  SQL-Dialekt-Prüfer, Designer, ChartProben gegen die jüngste Messlatte und den Referenzlauf. **Die
  Windows-Schale und `Werkzeuge/Testdatenbankschema` stehen nicht im Kern-Filter** — nach jedem Merge
  ausdrücklich bauen; ein Merge hatte dort unbemerkt einen Block abgeschnitten.
- **Testdatenbank und Werkzeugtests:** Auch Werkzeugtests lesen die Testdatenbank (Auslieferungsvorlage
  `T12`); wer Kataloginhalte sät, zieht sie mit.
- **Saatskript 1066–1071** (`Referenzlaeufe/Skripte/referenzprojekte_zapfprofil_optionen.cs`) setzt 1065 als
  höchste Projekt-ID voraus und läuft auf einer Datenbank mit 1072 nicht mehr; nur bei einem Neubau der
  Testdatenbank anzupassen.
- **Statuszeilen:** Nummern unmittelbar vor dem Commit gegen origin messen; je Welle eine Zeile, der
  Nach-Block „Nach #… (Zapfprofil …)“ mit Sichtabnahme, Wiki und CI-Vermerk, der Vermerk als eine Zeile mit
  Lauf-Kennung und Commit.
- **Umgebung:** .NET 10.0.400 unter `$HOME/.dotnet` (`export PATH="$HOME/.dotnet:$PATH"`); `rm -rf` mit einer
  Shell-Variable sperrt die Sicherheitsprüfung — wörtliche Pfade nehmen.

## 4 Offene Punkte (mit Empfehlung)

1. **W6 — Wiki am 20.10.2026.** Seite `Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki`
   (die einzige Wiki-Quelle, die die Wellen geändert haben) und der Logbuch-Abschnitt
   „Version 1.2.0.11 — Brauchwasser-Zapfprofil“ (sieben Sätze) in
   [`Wiki_Update_2026-09-26.md`](../Wiki_Update_2026-09-26.md), Abschnitt 2. Vorher mit dem Muster aus
   `CLAUDE.md` gegenlesen und `WikiProduktdatenWacheTests` ziehen. **Mit dem Anwender klären:** nur das
   Zapfprofil laden oder den Sammel-Upload aller ausstehenden Blöcke (dort stehen weitere Entwürfe anderer
   Sitzungen ohne Version; Regel „höchstens einmal je Woche“). Danach: Statusdatei „Nach #1043–#1045“ (b)
   als erledigt mit Revision, Logbuch-Block als veröffentlicht, das Konzept Benutzerführung per `git mv` nach
   `ueberholt/` samt Indexzeile. Der WordPress-Zugang der alten Sitzung meldete einen Verbindungsfehler (502);
   ohne Zugang gibt es keinen Upload.
2. **Sichtabnahme unter Windows** beim Anwender; Prüfpunkte in der Statusdatei unter „Nach #1043–#1045“ und
   „Nach #1062–#1065“.
3. **Klein, ohne Auftrag:** In der Testdatenbank trägt `DIN4708.p_b` die Einheit „Personen“,
   `TwwParameterschluessel` erwartet „P“ — mit der nächsten Saat des Testkatalogs berichtigen (Einfrierregel
   „gesäte Zapfprofil-Eingaben“ vorher prüfen). In `Referenzlaeufe/LIESMICH.md` endet die Liste
   „Entfernte Basen“ bei R47.
4. **Aus der Zeit davor** gelten die offenen Entscheide in Abschnitt 17.2 des Papiers vom 23.09.2026 weiter.

## 5 Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Zapfprofilgenerator Cloud“ für EPOS-Plan (Repo inekon/EPOS-Plan,
Arbeitszweig ios_migration_september). Lies zuerst CLAUDE.md und
Dokumentation/aktuell/Zapfprofilgenerator/2026-10-14_Uebergabe_Zapfprofilgenerator_Kontowechsel.md,
dann Abschnitt 17 von Dokumentation/aktuell/Zapfprofilgenerator/2026-09-23_Uebergabe_Zapfprofilgenerator.md,
den Kopf (Schemaschritt angemeldet, Referenzbasis angemeldet) und die Blöcke „Nach #1043–#1045“ und
„Nach #1062–#1065“ in Dokumentation/aktuell/Status_iOS_Migration.md sowie
`git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september`.

Rolle: Du orchestrierst knapp. Umsetzung an opus-umsetzung (model: opus), Status, Protokolle, Wiki,
Gates und CI-Auswertung an sonnet-mechanik (model: sonnet), Zählungen an haiku-pruefung (model: haiku);
Fable nur für sehr komplexe Analysen. Agenten im eigenen Worktree, Modell immer ausdrücklich setzen.
Stehende Anweisungen (Abschnitt 2 des Papiers): nach grünem Gate ohne Rückfrage committen, mergen und
auf ios_migration_september pushen (kein Force, keine Tags, nie main); iOS-, macOS- und Setup-Läufe nie
ohne Freigabe; keine Normzahlen und keine Messreihen ins Repo; die Testdatenbank nur über wiederholbare
Saatskripte ändern und das vorher den Nachbarsitzungen melden. Trailer: Co-Authored-By des arbeitenden
Modells und Claude-Session mit der URL deiner Sitzung. Antworten auf Deutsch, knapp.

Umgebung prüfen: `dotnet --version` (mindestens 10.0.400), Testdatenbank kein LFS-Zeiger, `df -h`,
`gh api repos/inekon/EPOS-Plan --jq .full_name`, ob ein WordPress-Werkzeug für wiki.epos-plan.de
verbunden ist.

Erste Schritte: (1) Stand prüfen: 224 TwwZonenferienSchema und die Projekte 1066–1071 auf origin, die
CI-Auswahl in .github/workflows/kern.yml mit 1066–1071, keine Konfliktmarker. (2) Den Stand in höchstens
fünf Zeilen melden. (3) Mit dem Anwender W6 abstimmen (Abschnitt 4 Punkt 1): Umfang des Uploads am
20.10.2026 und ob der Wiki-Zugang steht. Bis dahin keine Codeänderung am Zapfprofil ohne Auftrag.
```
