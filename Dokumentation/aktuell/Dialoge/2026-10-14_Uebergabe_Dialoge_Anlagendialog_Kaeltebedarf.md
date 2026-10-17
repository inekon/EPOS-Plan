# Übergabe der Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, Katalogauswahl“ (14.10.2026)

Dieses Papier übergibt die Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, Katalogauswahl“
(`session_01UNeGChCYZu2EpgFx4P3q1q`) auf den Stand 14.10.2026 an eine Sitzung auf einem anderen Konto. Es nennt den
Stand, die geltenden Entscheide, die offenen Punkte und den Einstiegs-Prompt (Abschnitt 5); die Regeln der Sitzung
über `CLAUDE.md` hinaus stehen im Prompt. Regelquelle bleibt `CLAUDE.md`. Es löst das Papier vom 11.10.2026
([`2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md`](../../ueberholt/2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md))
ab, dessen Entscheide weitergelten (Abschnitt 2). Die parallelen Sitzungen führen eigene Papiere, etwa
[`2026-10-14_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md`](../../ueberholt/2026-10-14_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md)
(Berichte & Kosten) und [`2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md`](../../ueberholt/2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md).

## 1 Stand 14.10.2026

Stand 16.10.2026: Übernahme durch Sitzung `session_019JuX7JfD2tStDzd3CiStwf`; #1105/#1106 gepusht, CI grün (Lauf 38863988800). Aufräumen #1109 (Konzept Kältebedarf und Katalogauswahl-Mockups nach `ueberholt/`). Die Basis R56 `2026-10-16_R56_Wasserkosten` (KB-7) ist eingefroren und enthält die zwei Kälteschlüssel von #1105; das `--ohne` der Gate-Skripte ist gestrichen.

- Zweig: Arbeitszweig `ios_migration_september`; alle Wellen dieser Sitzung sind gepusht und mit CI-Vermerk versehen, keine Agenten, keine offenen Worktrees. Letzter Push: Merge `54689b556` (Statuszeilen #1180 WK2/WK3, #1181 KV8), Statuscommit folgt.
- Wellen und Statuszeilen: KR1/KR1b runde Wahlkästchen (#1037); K6 Kältebedarf dezentral/Split mit Schemaschritt 223 und Referenzprojekt 1072 (#1056/#1057); K6d (#1066); K6d-N (#1078); K6c (#1089, `8bf550004`); K6c-N Laufmeldung nennt die Hauptzahl (#1090, `bd7abe18a`).
- Basis R55 `Referenzlaeufe/2026-10-14_R55_KaeltebedarfDezentral` (37 Projekte; die CI rechnet 18).
- ChartProben-Messlatte `Proben/ChartProben/Messlatte_2026-10-14.sha256` mit 225 Zeilen; Vermerke #1089/#1090 in `683bc2cac` (Hashes aus Kern-Lauf 38532651105). `kern.yml` schreibt seit `b4b73a9b2` die vollständige Hashliste ins Protokoll (eingeklappte Gruppe „ChartProben-Hashliste ubuntu“); LIESMICH-Bildzahl 257 in `d40c38cd0`.
- CI-Vermerke gesetzt mit `a4e2e2bb4` (#1056/#1057), `5215d8e22` (#1066), `5f5a0023d` (#1078), `683bc2cac` (#1089/#1090); #1180/#1181 ausstehend (Check-in).
- Frühere Wellen (11. bis 13.10.): Katalogauswahl V1 mit Knopfleiste am gewählten Satz (KA-E-17 bis KA-E-27), Wärmepumpen-Überlagerung, Kältemaschinen-Dialog, Vergleich Projekt/Katalog, Gerätesteckbrief, KAL1-Klappgruppe, Kältebedarf K1 bis K5; Einzelheiten im Papier vom 11.10.2026.
- Schemakette und Basenfolge (nur beobachten): 224 Zapfprofil, 225, 226/227, 228 KB-4, 229 KB-5, 230 KBP-2, 231 DA-6, 232 BK-5/BK-6; Basen R56 KB-5, R57 SA4. Maßgeblich ist die Kopfzeile „Schemaschritt angemeldet“ der Statusdatei.

## 2 Geltende Entscheide des Anwenders (Auswahl)

- Kältebedarf (11.10.): K2–K4 als nächste Welle; K3 hier gebaut, abgestimmt mit Kälteanlagen — Abzugsstelle `SimulationKaeltebedarf.AbzugVorDerKaskade(h)`/`BedarfDerKaskade(…)` ist der Andockpunkt für K‑D; Split-Strom je Kühlträger und Zähler erst mit K6 als Schemaschritt (bis dahin Stromträger des Projekts mit Hinweis); Statusbit „Wärmebedarfsdaten“ (16) zählt weiterhin Kühlzeilen.
- Anlagendialog (11.10., KA‑E‑17 bis 22): nur Kästchen, gewählte Projektzeile durch Hervorhebung, Klick wählt, Ankreuzen wählt nicht; Katalog-Knopfleiste in der Zeile „gewählter Satz“, Stift dort entfällt, Stifte je Listenzeile bleiben = Bearbeiten…; Schloss setzen… und Löschen beim Projektsatz weich gesperrt „Katalogsatz wählen“, Neu… immer Katalogsatz, Vergleichen… je Bereich; Bearbeiten… im Projektkopf entfällt; Geltung alle Wirte. Wärmepumpe: Bearbeiten… öffnet für Projekt- und Katalogsatz dieselbe Überlagerung aus Satz und Anlage, Auswahlliste „Wärmepumpen Auswahl“ mit „Modul-Katalog…“ entfällt, je Aufruf eine Anlage mit ihrer Kennlinie, Abschnitte ohne Daten beim Katalogsatz weich gesperrt „nur im Projekt“; Knopfleiste und Schlussleiste der Wärmepumpe wie die anderen Dialoge.
- Anwenderentscheid 13.10.: Die zwei „Vergrößern“ bleiben, wie sie sind — am Projektsatz des Gebäudes (Satz des Projektgebäudes: Ausrichtung, Fläche, Export, Simulation) und neben Bearbeiten… bei Wärmebedarf extern und Stromganglinie (Empfehlung „so lassen“ angenommen).
- KAL1 (11.10.): Abschnitt „Einzeltage (Feiertage, Ausnahmen)“ als Klappgruppe, zugeklappt beim Öffnen, Anzahl im Kopf, „+ Einzeltag“ und „Feiertage laden“ in der Kopfzeile.
- Arbeitsweise: Push am Ende jeder Welle freigegeben; kein Wiki-Upload ohne Rückfrage; Modellregel des Anwenders: Fable 5.1 orchestriert, Opus 5.5 setzt um, Sonnet/Haiku für Mechanik und Zählungen.
- Dialoge 12.10. (KA‑E‑23 bis KA‑E‑27, Konzept Katalogauswahl Abschnitt 3): Wärme-/Kältebedarf extern folgt dem Gerüst des Wärmepumpendialogs (Satzzeile mit Kurzangaben und Knopfleiste, eine Bearbeiten-Überlagerung für Projekt- und Katalogsatz); Vergleichen… vergleicht Projektzeilen und Katalogsätze gemeinsam (zwei bis drei, Herkunft im Spaltenkopf, sonst weich gesperrt); jede Bearbeiten-Überlagerung zeigt links einen Gerätesteckbrief (Wärmepumpe mit Arbeitspunkt am eingestellten Vorlauf); Wärmepumpen mit Kühlkennlinie stehen in der Katalogauswahl des Kältemaschinen-Dialogs (Übernehmen = Projektwärmepumpe mit Kühlbetrieb über `WPCtrl.KuehlkonfigurationSchreiben`, Rang in der Kältefolge anhängen, Entfernen schaltet nur den Kühlbetrieb aus und entfernt den Rang, Bezugspunkt 7 °C Kaltwasser mit Rückfall, Vergleich nur innerhalb einer Art, Zulässigkeit je Art, eine Zeile je Projektkopie); Knopfleiste bricht um und bleibt in der Dialogbreite, Katalogfunktionen im Katalogkopf, Infoknöpfe im Satzkopf (zunächst Kältemaschine). Bearbeiten… einer Wärmepumpe im Kältemaschinen-Dialog ist bis KM‑K2 weich gesperrt.
- Konfiguration Wärmepumpe (12.10.): „Betriebszeiten“ als Klappgruppe, zugeklappt beim Öffnen, Stand im Kopf; Deckungsgruppe der Kältedialoge in der Bearbeiten-Überlagerung, kein eigener Knopf.
- Anwenderentscheid 14.10.: Laufmeldung der Kälteerzeugung nennt mit Split-Zeile die Hauptzahl; der gespeicherte `Kaelte.DeckungsgradProzent` bleibt (K6c-N).
- Anwenderentscheid 13.10.: Die zwei „Vergrößern“-Knöpfe bleiben, wie sie sind (Empfehlung „so lassen“ angenommen).
- Anwenderentscheid KR1b: Kopfhäkchen rund; die Katalogliste bleibt so.
- Anwenderentscheid K6d: neue Kennzahlschlüssel mit Katalogfassung 23; die ausgelieferten Word-Vorlagen werden neu erzeugt; eigener Nachzug nach K6c. Kurzbericht-Schalter einbauen; die Kältetafel der ausführlichen Vorlage bleibt so.
- Anwenderentscheid 14.10. (K6c-N): Die Laufmeldung nennt mit Split-Zeile die Hauptzahl; der gespeicherte `Kaelte.DeckungsgradProzent` bleibt.
- Anwenderentscheid 14.10.: Die Verantwortung für Stromspeicher und Speicherflotte (Schemaschritt 229, KB-5) liegt bei der Kältemaschinen-Sitzung.
- Anwenderentscheid 14.10.: Modellregel — Fable orchestriert nur, sonst Opus 5.5 bzw. Sonnet/Haiku; Check-ins löst ein Sonnet-Agent aus; Fable macht keine GitHub-Abfragen und keine Dateiänderungen.
- Anwenderentscheid 14.10.: Die ChartProben-Messlatte wird über die Hashliste im CI-Protokoll umgesetzt (erledigt, `Messlatte_2026-10-14.sha256`).
- Anwenderentscheid 16.10.: Die Kästchen der Liste „Im Projekt“ bleiben ein Kreis mit ☐/☑ (KA-E-28); die Wiki-Bilder stimmen.
- Anwenderentscheid 16.10.: Der Vorlauf der Wärmepumpe liegt in Wiki-Bildern und -Beispielen nicht über 55 °C (Booster-Beispiel „etwa 55 °C“, `012aa4adb`; Bildprobe „VL max“ 55, `81c19266f`); das Prozesswärme-Beispiel „bis 70 °C“ bleibt.
- Anwenderentscheid 16.10.: Die Hauptzahl wird jetzt gemergt und gegen R55 mit `--ohne` verglichen, bis die nächste Basis sie aufnimmt.
- Anwenderentscheid 16.10.: Die Abbruchregel von `kern.yml` bleibt.
- Anwenderentscheid 16.10.: Kein Kern-Lauf für Pull Requests aus dem eigenen Repository.
- Anwenderentscheid 16.10.: Wiki-Sammel-Upload in der Woche ab 19.10.
- Anwenderentscheid 16.10.: Modellregel — Fable 5 nur für sehr komplexe Aufgaben, sonst Opus 5.5 bzw. Sonnet/Haiku je nach Aufgabe.
- Anwenderentscheid 17.10.: Wiki-Quellen für Seiten ohne Quelle nicht in dieser Sitzung; Übergabepapiere Kontowechsel, Kostenbasis KB4/KB5 und KB5-Inventar nach `ueberholt/`; Kältevorlage 8 auf 230 Nutzungstage nach DIN/TS 18599‑10.
- **KB-8/KB-9 (17.10.2026):** Die Längengrenze von `StromsteuerBefreiungModus` bleibt wie vorgefunden — Testdatenbank ohne `CHECK (length ≤ 20)`, Anwenderdatenbanken aus dem Laufzeitweg mit; kein Tabellenumbau. KB-9 (übrige Vorsorgewege) wird vorbereitet (Abschnitt 6).

## 3 Hinweis

Die Regeln der Sitzung über `CLAUDE.md` hinaus stehen im Einstiegs-Prompt (Abschnitt 5).

## 4 Offene Punkte (mit Empfehlung)

1. ~~Nachweis Messlatte~~ erledigt: Kern-Lauf [38550151112](https://github.com/inekon/EPOS-Plan/actions/runs/38550151112) grün auf `ae1f65a24` (enthält `683bc2cac`), Schritt „ChartProben-Messlatte (nur ubuntu)“: 225 von 225 Zeilen unverändert.
2. Beim Anwender: Versionsnummer des Logbuchs für die zwei Entwurfsblöcke in [`../Wiki_Update_2026-09-26.md`](../Wiki_Update_2026-09-26.md); Wiki-Upload nur nach Freigabe; der Wiki-Sammel-Upload 1.2.0.11 folgt in der Woche ab 19.10., nach Freigabe; Sichtabnahme unter Windows (runde Wahlkästchen, Kältebedarf-Dialoge, Laufmeldung).
3. Erledigt: Hauptzahl in `aggregate.csv` (#1105) und CI-Kern-Lauf in parallelen Jobs (#1106), Wandzeit rund 19,5 statt 84 min.
4. Beobachten (R56 eingefroren, siehe Abschnitt 1): Schemakette 228 KB-4, 229 KB-5 (Stromspeicher/Speicherflotte bei der Sitzung „Dialoge und Korrekturen (Konto Übernahme)“/Kältemaschinen), 230 KBP-2, 231 DA-6, 232 BK-5/BK-6; Basen R56 KB-5, R57 SA4. Die Sitzung „Dialoge Cloud“ arbeitet am Konzept Kältebedarfsprofile (KBP) mit unseren drei Bitten: Deckungsgruppe und Naht `SimulationPlattformwege.KaelteDezentralBearbeiten` unverändert, neue Vorlagen nur als neue Katalogzeilen, Kalenderhinweis als Baustein.
5. Erledigt: ~~Kältebedarf K6~~ erledigt (#1056/#1057, #1066, #1078, #1089, #1090); ~~Sichtabnahme der Katalogauswahl-Wirte~~ in Punkt 2 aufgegangen; ~~Nachzüge Katalogauswahl, KM-K2, Kostenbasis KB-2/KB-3~~ erledigt (Statuszeilen #1034, #1038, #1079, #1080).
6. KBP‑3b: mit #1108 erledigt — die Grafik folgt dem Arbeitsstand (Monatswerte, Wochenprofil, Kalender), die Kalenderwoche ist wählbar, die Tagesarten stehen als Hintergrund. Offen bleibt nur „Woche als Reihen statt Hintergrund“ (Anwenderentscheid).
7. Kern-Test Betriebskalender mit mehr als vier Ferienzeiträumen, sobald die Testdatenbank eine Ferienliste führt.
8. Gemessene Kälte-Lastgänge für Richtwerte der Vorlagen (beim Anwender).
9. Wiki-Bilder neu ziehen: erledigt mit WK3 (#1180).
10. Sichtabnahme Windows Dialoge #1034–#1042 und Kostenmasken KB‑4 (beim Anwender).
11. Setup-Skript der Cloud-Umgebung (beim Anwender).
12. Kältevorlage 8: umgesetzt mit KV8 (#1181): Normkalender als Vorgabe, Schreibweg wählt ihn vor; zwei Befunde als eigene Aufträge (Vorschau-Raster Wochentag 6; Kalenderwahl „keiner“).
13. **#501 Vorsorgewege:** KB-8 (Schritt 240) umgesetzt auf `kb8`, Merge-Welle läuft; KB-9 Auftrag in Abschnitt 6, Schritt 243 (Anmeldung in der Kopfzeile der Statusdatei). Befund dabei: Die iOS-Gerätedatenbank wird nach dem Erststart nie gehoben (keine Schemapflege auf dem Gerät) — Entscheid beim Anwender, ob und wann der Kern eine eigene Schemapflege für iOS bekommt.

## 5 Einstiegs-Prompt für die Übernahme (14.10.2026)

```text
Du übernimmst die Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, Katalogauswahl“ für EPOS-Plan (GitHub `inekon/EPOS-Plan`, Arbeitszweig `ios_migration_september`) von einem anderen Konto. Du bist Fable 5.1 und orchestrierst: Aufträge schneiden, Modell wählen, abnehmen, Anwenderentscheide vorbereiten, knapp berichten. Alles andere delegierst du (Merges, Gates, CI-Vermerke, Status- und Protokollpflege, Nachzüge) — `opus-umsetzung` für Rechenweg/Schema/Tests/Dialoge, `sonnet-mechanik` für Mechanik/Status/Ressourcen/Gate-Zahlen, `haiku-pruefung` für Zählungen; Modell bei jedem Agentenaufruf ausdrücklich setzen. Geplante Check-ins (`send_later`) lösen einen Sonnet-Agenten aus; du selbst machst keine GitHub-Abfragen und keine Dateiänderungen (Ausnahme: eine Zeile, ein Verweis). `CLAUDE.md` gilt vollständig; Antworten auf Deutsch, knapp.

**Erste Schritte (nur lesen, dann melden und abstimmen — keine Arbeit ohne Auftrag):**
1. `CLAUDE.md` lesen. Das Übergabepapier `Dokumentation/aktuell/Dialoge/2026-10-14_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md` lesen — Abschnitte „Stand 14.10.2026“, „Offene Punkte“ und „Anwenderentscheide“ (Pfad: `ls Dokumentation/aktuell/Dialoge/ | grep -i uebergabe`).
2. `git fetch origin && git log --format='%h %<(72,trunc)%s' -n 25 origin/ios_migration_september`; Statuszeilen dieser Sitzung in `Dokumentation/aktuell/Status_iOS_Migration.md` nur per `grep -n '\*\*#10[3-9][0-9]\*\*'` lesen (#1037 KR1, #1056/#1057 K6, #1066 K6d, #1078 K6d-N, #1089 K6c, #1090 K6c-N); Kopfzeile „Schemaschritt angemeldet“ (Zeile 18) nur mit `grep -o '\*\*2[23][0-9] —[^*]*\*\*' | tail -8`.
3. Stand in zehn Zeilen melden und den ersten Auftrag beim Anwender erfragen.

**Stand bei Übergabe (14.10.2026, ~10:30 UTC):** Alle Wellen dieser Sitzung sind gepusht und mit CI-Vermerken versehen: KR1/KR1b (runde Wahlkästchen, #1037), K6 Kältebedarf dezentral/Split mit Schemaschritt 223 und Referenzprojekt 1072 (#1056/#1057, #1066, #1078, #1089), K6c-N Laufmeldung nennt die Hauptzahl (#1090), ChartProben-Messlatte `Messlatte_2026-10-14.sha256` mit 225 Zeilen (`683bc2cac`), LIESMICH-Bildzahl 257 (`d40c38cd0`). Basis ist R55 `Referenzlaeufe/2026-10-14_R55_KaeltebedarfDezentral` (37 Projekte; CI rechnet 18). `kern.yml` schreibt seit `b4b73a9b2` die vollständige ChartProben-Hashliste als eingeklappte Protokollgruppe „ChartProben-Hashliste ubuntu“ — Messlatten lassen sich damit ohne Artefakt-Download nachziehen (der Blob-Host der Artefakte ist in Cloud-Sitzungen gesperrt; lokale Hashes sind nicht ubuntu-gleich, nie verwenden).

**Offene Punkte:**
- **Nachweis Messlatte:** erledigt — Kern-Lauf 38550151112 grün auf `ae1f65a24` (enthält `683bc2cac`), Schritt „ChartProben-Messlatte (nur ubuntu)“ meldet 225 von 225 Zeilen unverändert. Kein Commit nötig.
- **Beim Anwender:** Logbuch-Versionsnummer für die zwei Entwurfsblöcke in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md` („Dialoge mit Katalogauswahl“, „Kältebedarf dezentral (Split)“); Wiki-Upload nur nach Freigabe. Sichtabnahme unter Windows (runde Wahlkästchen, Kältebedarf-Dialoge, Laufmeldung).
- **Nicht beauftragt, klein:** `aggregate.csv` des Referenzlaufs führt für Split-Zeilen keine Hauptzahl; CI-Kern-Testlauf dauert ~87 min und wird auf dem Arbeitszweig fast immer abgebrochen (Blöcke oder Läufe je Sitzungszweig wären ein eigener Auftrag).
- **Nur beobachten:** Schemakette 224 Zapfprofil → 225 → 226/227 → 228 `GeraetekostenEntfallSchema` (KB-4, Sitzung „Dialoge und Korrekturen (Konto Übernahme)“, Push heute angekündigt) → 229 KB-5 Stromspeicher-Kosten (Verantwortung Stromspeicher/Speicherflotte liegt dort, Anwenderentscheid 14.10.) → 230 KBP-2 → 231 DA-6 → 232 BK-5/BK-6; Basenfolge R56 KB-5, R57 SA4. Die Sitzung „Dialoge Cloud“ arbeitet am Konzept „Kältebedarfsprofile wie Wärmebedarfsprofile“ (KBP) mit drei Bitten von uns: Deckungsgruppe und Naht `SimulationPlattformwege.KaelteDezentralBearbeiten` unverändert, neue Vorlagen nur als neue Katalogzeilen, Kalenderhinweis als Baustein.

**Arbeitsregeln der Sitzung (zusätzlich zu `CLAUDE.md`):**
- Agenten arbeiten im eigenen Worktree `.claude/worktrees/<name>` (Zweig von `origin/ios_migration_september`) mit eigenem Scratchpad-Unterordner; dort `git lfs install --local` und `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite` (Datei > 1 MB, sonst Zeiger). Sofort committen, nicht pushen, bin/obj nicht committen. Vor Hauptbaumarbeit `AGENT_LAEUFT` anlegen, danach löschen. Push nur über den Hauptbaum per `--ff-only`.
- Gate im Container: SDK ist 10.0.112, `global.json` verlangt 10.0.400 → vorübergehend `"version": "10.0.100"`, vor jedem Commit `git checkout global.json`. Das Vollgate `gate_linux.sh` läuft ins Zeitlimit; stattdessen `Werkzeuge/Gate/kern_bloecke.sh <wurzel> <ablage> AB CD EF GH IJ K` und `… LM NO PQ R S TUVWXYZ` parallel (~35 min) plus `gate_rest_linux.sh <wurzel> <ablage>` (~20 min), je als genau ein wartender Hintergrundbefehl. Wer die Windows-Schale berührt, baut sie zusätzlich auf Linux (`-p:EnableWindowsTargeting=true`).
- Ressourcen immer de und en (`EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`), parallele Agenten an verschiedenen Stellen der Datei, Designer nach jedem Merge mit `designer_neu.py schreiben` neu; nach Merges die .resx als XML prüfen.
- Wellenfolge: Merge origin → Gate → Statuszeile (Nummer unmittelbar vor dem Commit gegen origin messen: höchste `**#N**` + 1; Muster `| **#N** | TT.MM. | **Titel** | Text … CI-Kern-Lauf: ausstehend (Vermerk folgt). |`) und Logbuchsatz (nur wesentliche, sichtbare Änderungen) → Push (am Ende jeder Welle freigegeben) → CI-Vermerk allein mit `[skip ci]` nach dem Muster „CI-Kern-Lauf [<id>](https://github.com/inekon/EPOS-Plan/actions/runs/<id>) grün auf `<sha7>` (enthält `<push7>`).“
- Nie ohne Rückfrage: macOS-/iOS-/Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags. Stop-Hook-Meldungen „uncommitted/unpushed“ sind kein Auftrag.
- Commit-Trailer: `Co-Authored-By: Claude <arbeitendes Modell> <noreply@anthropic.com>` und `Claude-Session: <URL dieser neuen Sitzung>`.
- Fallen: `pkill -f` tötet die eigene Shell — Probenwirte nur über `pgrep -f '[W]irt'` und eigene PID beenden; Statusnummern doppelt vergeben, wenn nicht gegen origin gemessen; Zähltests werden rot, wenn die Testdatenbank wächst (Zählklassen nachziehen).
- Abstimmung mit anderen Sitzungen über `send_message` (Sitzungen des Anwenderkontos: Gebäudesimulation `session_01NPHEwQvnQDiWMWnQbMmEou`, Zapfprofil `session_017jJMrbTBmVq3vxUtLN4evQ`, Kältemaschinen/KB `session_0178YLFEbS4xWdGaXFaZqG1r`, Dialoge Cloud `session_01Mt7MsJGzcGr77yqBsxwpLj`); erreicht `send_message` sie vom neuen Konto nicht, läuft die Abstimmung über den Anwender und die Kopfzeile der Statusdatei. Vor jedem Push ankündigen, dass er laufende Kern-Läufe des Arbeitszweigs abbricht, wenn eine Sitzung gerade auf einen Nachweis wartet.
```

## 6 Auftrag KB-9 Übrige Vorsorgewege des Kerns (Schemaschritt 243)

Gegenstand ist der Rest von #501: Nach KB-8 (Schritt 240, `ErgebnisWirtschaftlichkeitSpaltenSchema`, Zweig `kb8`) legen im Kern
an 24 Fundstellen noch zur Laufzeit Spalten oder Tabellen an (`ALTER TABLE`/`CREATE TABLE` beim Laden, Speichern oder Rechnen). KB-9 baut
sie ab, nach dem Muster von KB-8: Was ein Schemaschritt oder das Grundschema anlegt, fällt ersatzlos; was nur das Grundschema trägt und
im Code als Literal steht, bekommt mit Schritt 243 einen formalen, idempotenten Anlageschritt; Fachlogik bleibt. Drei Teilwellen
(6.3 bis 6.5), je ein `opus-umsetzung`-Agent mit höchstens rund 150 Werkzeugaufrufen. Pfade repo-relativ, Quelltextpfade der Tabelle
relativ zu `EPOS.Kern/`. Vorbild: `git show kb8:EPOS.Kern/Allgemein/Update/ErgebnisWirtschaftlichkeitSpaltenSchema.cs` und
`git show kb8:EPOS.Kern.Tests/ErgebnisWirtschaftlichkeitSpaltenSchemaTests.cs`; die berührten Register zeigt `git show kb8 --stat`.

### 6.1 Woher jede Datenbank kommt (Altdatenbanken)

- Jede SQLite-Datenbank geht auf `sql/schema/001_grundschema.sql` zurück: Die Hausbestände kamen über den Migrator von Access-Stand 61
  in dieses Schema, die Auslieferungsvorlage entsteht mit `Werkzeuge/Auslieferungsvorlage` aus einer solchen produktiven Datenbank, die
  Testdatenbank ebenso, und die iOS-Seed-Kopie ist die Testdatenbank (`EPOS.iOS/EPOS.iOS.csproj`, `SeedDb`). Eine Übernahme aus Access
  gibt es nicht mehr, eine Datei unter Stand 61 weist das Programm ab ([`BETRIEB_SQLITE.md`](../BETRIEB_SQLITE.md) Abschnitt 1.1).
  `Paketanhebung` hebt Pakete in der flüchtigen `Paketarbeitsdatenbank` und legt keine Anwenderdatenbank an.
- Folge: Die Spalten der Schritte bis 61 und alle Grundschema-Spalten und -Tabellen liegen in jeder Datenbank. Stichproben im
  Grundschema: `Z_ProjektWaermebedarf.Kanal` (Schritt 48), `Tab_Pufferspeicher.Schichten_Anzahl`/`Lambda_Eff` (Schritt 53) und
  `Nutzung_Heizung` (Schritt 49), `Tab_Klimaregion.Klimazone_DIN4710`, `Tab_ErgebnisHeizkessel.Quellwaerme` (Schritt 10),
  `Tab_ProjektWerte.ID_Anlage`/`ID_AnlageGeraet` — alle vorhanden; die Schritte ab 62 fährt `SchemaMigration` beim Programmstart.
- Die 13 Spalten aus Abschnitt B stehen im Grundschema und in der Testdatenbank (Typ REAL bzw. INTEGER, Tabellen STRICT). Ein Schritt
  ist also nicht zwingend; er kommt trotzdem, weil er billig ist und die Deckung dann im Programm steht statt nur in einer SQL-Datei,
  die kein Programmweg ausführt.
- iOS: Die Gerätedatenbank wird beim Erststart einmal aus der Seed-Kopie angelegt und danach nie gehoben
  (`EPOS.iOS/Datenbankbereitstellung.cs`; `SchemaMigration` liegt in der Windows-Schale). Alle Spalten und Tabellen dieses Abbaus liegen
  in der Seed-Kopie; der Abbau ändert für iOS deshalb nichts. Die fehlende Schemapflege auf dem Gerät ist nicht Teil von KB-9 (Punkt
  13 in Abschnitt 4).

### 6.2 Entscheide je Fundstelle

| Datei/Methode | Gegenstand | Entscheid | Schritt nötig? |
|---|---|---|---|
| `Allgemein/Simulation/WaermequelleClass.cs` `SchemaSicherstellen`, `SpalteSicherstellen` (öffentlich) | Rückfallebene über `SchemaKatalog.Alle` (Bestand, Schritte 1, 2, 6, 8, 11, 13 aus dem Grundschema; 63, 64, 66, 68 aus `SchemaMigration`) | entfällt ersatzlos — jede Liste von `Alle` legt das Grundschema oder ein Schritt an | nein |
| `Controller/KlimaregionCtrl.cs` (Zeilen 193, 232) | `Tab_Klimaregion.Klimazone_DIN4710` | entfällt ersatzlos — Grundschema | nein |
| `Controller/StromspeicherCtrl.cs` `StelleGeraetespaltenSicher` | Schritte 11 und 68 | entfällt ersatzlos — Grundschema bzw. Schritt 68 | nein |
| `Controller/KostenPositionCtrl.cs` `StelleSpaltenSicher` | `Tab_ProjektWerte`: Schritte 19, 38, `ID_Anlage`, `ID_AnlageGeraet` | entfällt ersatzlos — Grundschema; der Rückgabewert der Aufrufer (`ProjektExportImportCtrl`, Kostenctrls) wird fest wahr | nein |
| `Controller/Preisanteile.cs` `SpaltenSicherstellen` | Schritte 12, 60 und 83 | entfällt ersatzlos — Grundschema bzw. Schritt 83 | nein |
| `Controller/PufferSpCtrl.cs` `StelleKlassenSetSpaltenSicher` | `Nutzung_Heizung`, `_Brauchwasser`, `_Prozess` | entfällt ersatzlos — Schritt 49, Grundschema | nein |
| `Controller/PufferSpCtrl.cs` `StelleSchichtSpaltenSicher` | neun Spalten des Schichtmodells | entfällt ersatzlos — Schritt 53, Grundschema | nein |
| `Controller/Z_ProjektGebGanglinieCtrl.cs` `StelleKanalSpalteSicher` | `Kanal` samt Vorbelegung | entfällt ersatzlos — Schritt 48a/48b; jeder Leser nimmt NULL als Heizung | nein |
| `Controller/ErgebnisCtrl.cs` `StelleKuehlSpaltenSicher`, `StelleKaelteerzeugerSpaltenSicher`, `StelleKanalSpaltenSicher` | `KuehlungSchema`-Listen, `Schritt52_ErgebnisJeKanal` | entfällt ersatzlos — Schritte 110 und 114 bzw. 52 | nein |
| `Controller/ErgebnisCtrl.cs` `StelleKesselSpaltenSicher` | `Tab_ErgebnisHeizkessel.Quellwaerme` | entfällt ersatzlos — Schritt 10, Grundschema | nein |
| `Controller/ErgebnisCtrl.cs` `StelleModulSpaltenSicher` (Katalogteil) | `VbhThermisch`, `VbhElektrisch`, `Hilfsenergie` | entfällt ersatzlos — Grundschema; die Methode fällt mit ihrem Literalteil (übernächste Zeile) | nein |
| `Controller/ErgebnisCtrl.cs` `StelleEnergieSpaltenSicher` | `Tab_ErgebnisEnergiebedarf.Waermerestbedarf`, `Stromrestbedarf` | entfällt nach Schritt 243 — nur das Grundschema kennt sie | ja |
| `Controller/ErgebnisCtrl.cs` `StelleBHKWSpaltenSicher` | acht Brennstoffspalten an `Tab_ErgebnisBHKW` (`Oelverbrauch` bis `TierischeFette`) | entfällt nach Schritt 243 — dito | ja |
| `Controller/ErgebnisCtrl.cs` `StelleModulSpaltenSicher` (Literalteil) | `carrier_id` an `Tab_ErgebnisBHKWModul` und `Tab_ErgebnisHeizkesselModul`, `Tab_ErgebnisHeizkesselModul.Waermeproduktion` | entfällt nach Schritt 243 — dito | ja |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` `StelleTabellenSicher` | vier Ergebnisspalten | erledigt mit KB-8 (Schritt 240) | — |
| `Controller/VariantenCtrl.cs` `StelleVariantentabelleSicher` | `Tab_Variante` samt `UQ_VarProj` | entfällt ersatzlos — Grundschema und `sql/schema/003_indizes_fk.sql`; Schritt 159 (`KostenStempelSchema`) setzt Trigger an die Tabelle | nein |
| `Controller/StromspeicherVarianteCtrl.cs` `StelleTabelleSicher` | `Tab_StromspeicherVariante` samt Index | entfällt ersatzlos — Grundschema; die Schritte der Preisreihen- und Kostenprofilverweise sowie `SpeicherVarianteAktivEindeutig` bauen an ihr | nein |
| `Controller/ErgebnisCtrl.cs` `StellePufferTabelleSicher` | `Tab_ErgebnisPufferspeicher`, Index, Waisen-DELETE | entfällt ersatzlos — Grundschema mit Fremdschlüssel (das DELETE lief nur ohne ihn); `KuehlungSchema` baut an ihr | nein |
| `Controller/ErgebnisCtrl.cs` `StelleStromspeicherTabelleSicher` samt `SQL_CREATE_ERGEBNISSTROMSPEICHER_SQLITE` | `Tab_ErgebnisStromspeicher` | entfällt ersatzlos — Grundschema, `ErgebnisGebaeudeSchema` baut an ihr | nein |
| `Controller/PreisreiheCtrl.cs` `StelleTabellenSicher` | `Tab_Preisreihe`, `Tab_PreisreiheDaten` | entfällt ersatzlos — Grundschema (Schritt 12); Aufrufer `SpotpreisImportHuelle`, `KostenprofilCtrl` nachziehen | nein |
| `Controller/KostenprofilCtrl.cs` `StelleTabelleSicher` | `Tab_Kostenprofil` | entfällt ersatzlos — Grundschema | nein |
| `Controller/AnlagePufferVerbundCtrl.cs` `StelleTabelleSicher` | `Z_AnlagePufferVerbund` | entfällt ersatzlos — Grundschema (Schritt 14) | nein |
| `Controller/SpeicherAuslegungCtrl.cs` `SchemaSicherstellen` | `Tab_SpeicherAuslegung` | entfällt ersatzlos — Schritte 73/74; einziger Aufrufer ist `EPOS.Kern.Tests/TestDatenbank.cs`, die Konstanten `SQL_TABELLE`/`SQL_INDEX` bleiben als Quelle der Schritte und des Werkzeugs | nein |
| `Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs` `StelleKatalogSicher` (Anlage) | `CREATE TABLE Tab_Gesetzesparameter` | entfällt ersatzlos — Grundschema; Schritte 87 (Index) und 159 (Stempel) bauen an ihr | nein |
| `Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs` `StelleKatalogSicher` (Saat) | generationsweise Nachsaat des Gesetzeskatalogs | bleibt — gepflegte Fachdaten mit eigener Generation, Aufrufer sind die Katalognachsaat der Migration und das Werkzeug | nein |
| `Allgemein/Wirtschaftlichkeit/EmissionsBilanzRechner.cs` `StelleKatalogSicher` | `Tab_Kraftwerkspark` und Saat 1–3 | entfällt nach Schritt 243 — kein Schritt kennt die Tabelle (159 lässt sie bewusst ohne Stempel) | ja |
| `Tab_ProjektWirtschaftlichkeit.Aufschlaege_Anwenden` (Grundschema, `SchemaKatalog.Schritt21_Tarifmodell`) | Altspalte ohne Leser, Schritt 85 entfernt sie | bleibt — Geschichte der Kette | nein |

Zählung: 20 Fundstellen entfallen ersatzlos, 4 entfallen nach Schritt 243, 2 bleiben, 1 ist mit KB-8 erledigt.

**Kraftwerkspark:** Schritt 243 legt `Tab_Kraftwerkspark` an, wenn sie fehlt — STRICT, Spalten wie im Grundschema — und sät die drei
Zeilen nur in diesem Fall, mit den Werten aus `EmissionsBilanzRechner` unverändert. Das ist genau die Bedingung des Laufzeitwegs; eine
vorhandene leere Tabelle bleibt leer, weil ein geleerter Katalog Anwenderwille ist. Die Testdatenbank führt die Tabelle mit den drei
Zeilen; der Schritt ändert dort nichts.

### 6.3 Teilwelle 9a — Schrittklasse 243 samt Tests (Zweig `kb9`)

Voraussetzung: KB-8 ist gemergt (240 auf origin), `kb9` hat `origin/ios_migration_september` gemergt. Arbeitsort Worktree
`.claude/worktrees/kb9`; Regelquelle `CLAUDE.md`; nicht pushen, kein CI-Lauf; Commits sofort, genaue Pfade, Trailer
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

- Neue Klasse `EPOS.Kern/Allgemein/Update/VorsorgewegeSchema.cs` nach dem Muster von KB-8: Liste der 13 Spalten (Tabelle, Name,
  Access-Typ `DOUBLE` bzw. `LONG`, Typ über `StilleDb.SqliteSpaltenTyp`), dazu die Anlage von `Tab_Kraftwerkspark` mit Saat bei
  Neuanlage; `Voraussetzungen()` (die vier Ergebnistabellen), `Offen()`, `Vollstaendig()`, `Ausfuehren(bericht)`; kein DML außer der
  Saat; wiederholbar. Spaltennamen als Konstanten der Klasse, die `ErgebnisCtrl` in 9b übernimmt; die Saatwerte als eine Liste, die
  `EmissionsBilanzRechner` nicht mehr braucht.
- Kette vorläufig wie KB-8: `SCHRITT = <höchste Schrittklasse auf origin>.SCHRITT + n` bis 243, mit Kopfvermerk; nicht in Schrittliste,
  Paketanhebung und Zielstand, solange 241 und 242 nicht auf origin liegen. Ausführung in `SchemaMigration` (Schale) und in
  `Werkzeuge/Testdatenbankschema` vorbereiten wie KB-8, Eintrag `Art.Ddl` in `Paketanhebung` vorbereiten (auskommentiert oder hinter dem
  Umhängen).
- Tests `EPOS.Kern.Tests/VorsorgewegeSchemaTests.cs` nach dem Vorbild: Nummer und Typen; fehlende Spalten werden angelegt, Zweitlauf
  ändert nichts; eine vorhandene Spalte bleibt unberührt; `Tab_Kraftwerkspark` fehlt → angelegt, STRICT, drei Zeilen; vorhanden und leer
  → bleibt leer; vorhanden mit Zeilen → unverändert; die Typen gleichen denen des Grundschemas.
- **Abnahme 9a:** `dotnet build WP-Plan.Kern.slnf -c Release -nologo -v q -clp:ErrorsOnly` 0 Fehler; Windows-Schale auf Linux
  (`-p:EnableWindowsTargeting=true`) 0 Fehler, sobald `SchemaMigration` angefasst ist; `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db
  Referenzlaeufe/Kenndaten_Test.sqlite` 0 Fundstellen; `dotnet test` mit `--filter` auf `VorsorgewegeSchemaTests`,
  `ProjektpaketAnhebungTests`, `ErgebnisWirtschaftlichkeitSpaltenSchemaTests` grün. Testdatenbank nicht anfassen.

### 6.4 Teilwelle 9b — Abbau in Controllern und Rechnern (Zweig `kb9`, nach 9a)

- Die 24 Fundstellen der Tabelle mit „entfällt“ abbauen, Aufrufer per `grep` nachziehen (darunter `SimulationControl`,
  `EPOS.UI.Daten/Simulation/SimulationKonfigHuelle.cs`, `WizardCtrl`, `ProjektExportImportCtrl`, `ErgebnisCtrl.Save`,
  `StromspeicherStammCtrl`, `SpeicherFlottenStudieCtrl`, `BrennstoffBestandteilCtrl`, `StrompreisZerlegungCtrl`, die Hüllen der
  Wirtschaftlichkeit in `EPOS.UI.Daten`); `GesetzKatalog.StelleKatalogSicher` behält nur die Saat. Was still scheiterte,
  braucht keinen Ersatz: Fehlt eine Spalte, ist die Datenbank nicht gehoben, und das meldet die Migration.
- Tests, die die Vorsorgewege selbst prüfen, auf die Schritte umstellen oder streichen (Suche nach den Methodennamen in
  `EPOS.Kern.Tests` und `EPOS.UI.Tests`); `EPOS.Kern.Tests/TestDatenbank.cs` ruft statt `SpeicherAuslegungCtrl.SchemaSicherstellen` die
  Konstanten oder die Schritte 73/74.
- Neue Wache `EPOS.Kern.Tests/LaufzeitDdlWacheTests.cs` (eine solche Wache gibt es noch nicht; `EPOS.Kern/CLAUDE.md` nennt keine): kein
  `ALTER TABLE`, `CREATE TABLE`, `CREATE INDEX` in Zeichenkettenliteralen und kein Aufruf von `AlterTableAddColumn` in
  `EPOS.Kern/**/*.cs` und `EPOS.UI.Daten/**/*.cs` außerhalb `EPOS.Kern/Allgemein/Update/`, Kommentarzeilen ausgenommen; namentliche
  Ausnahmen nur `Allgemein/Simulation/StilleDb.cs` (Baustein) und die zwei Konstanten von `SpeicherAuslegungCtrl`, mit Gegenprobe.
  Eine Zeile zur Wache in `EPOS.Kern/CLAUDE.md` neben den übrigen Wächtern.
- Papiere auf den gültigen Stand: `Dokumentation/aktuell/BETRIEB_SQLITE.md` (Abschnitt der Vorsorgewege), `ADR-001_Schema-Ausrollung.md`,
  `Konzept_KI-Assistent_Aufgabensteuerung.md` (Prosa zu `PreisreiheCtrl`, der Weg fällt), `Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`
  (Gesetzes- und Kraftwerkskatalog); Ressourcen nur, falls ein Text entfällt (beide Sprachen, `designer_neu.py schreiben`).
- **Abnahme 9b:** Kern-Filter und Windows-Schale 0 Fehler; SQL-Prüfer 0; `--filter` auf die Klassen mit `Ergebnis`, `Puffer`,
  `Klimaregion`, `Stromspeicher`, `KostenPosition`, `Preis`, `Variante`, `Gesetz`, `Emission`, `SpeicherAuslegung`, `Wirtschaftlichkeit`,
  `LaufzeitDdlWache`, `DokumentationLinkWache` in `EPOS.Kern.Tests` und `SimulationKonfig`, `Wirtschaftlichkeit` in `EPOS.UI.Tests`
  grün; Referenzlauf `EPOS.Referenzlauf` (zuerst bauen) für 1030, 1046 und 1049 gegen die aktuelle Basis GESAMT PASS — die Projekte
  speichern Ergebnisse über `ErgebnisCtrl.Save`, Stromspeicher und BHKW. `TestdatenbankSchemastandWacheTests` bleibt bis zur Hebung außen
  vor.

### 6.5 KB-9 Merge-Welle (Hauptbaum)

Wie Abschnitt 5.1 des Papiers
[`2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md`](../../ueberholt/2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md) mit diesen
Abweichungen: Voraussetzung ist 242 (KWN‑1) auf origin; `VorsorgewegeSchema.SCHRITT` auf `<Klasse des Schritts 242>.SCHRITT + 1`
umhängen und in Schrittliste, `Paketanhebung`, `SchemaStand.Zielversion` und `EPOS.Kern.Tests/TestDatenbank.cs` eintragen; `kb9` mergen;
Testdatenbank von der jüngsten origin-Fassung (242) mit `Werkzeuge/Testdatenbankschema` auf 243 heben, `integrity_check` ok,
`foreign_key_check` leer. Inhaltsvergleich: einzig `Tab_Applikation.SchemaVersion` ändert sich — die 13 Spalten und
`Tab_Kraftwerkspark` mit drei Zeilen liegen schon; alles andere ist ein Befund. Nachtrag „Schema 243“ in
[`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md). Vollgate gegen die aktuelle Basis; GESAMT PASS erwartet, weil kein
DML läuft. Kopfzeile der Statusdatei: 243 „gebaut mit #NNNN (Datenbank 243)“; kein Logbuch-Eintrag (keine sichtbare Änderung,
Konzept Hilfesystem 13.4).

### 6.6 Nicht im Umfang

- `Aufschlaege_Anwenden` (Grundschema, `SchemaKatalog.Schritt21_Tarifmodell`): Geschichte der Kette, kein Leser, bleibt.
- Die Längengrenze von `StromsteuerBefreiungModus` (Entscheid KB-8/KB-9 in Abschnitt 2).
- Eine Schemapflege der iOS-Gerätedatenbank (Punkt 13 in Abschnitt 4).
