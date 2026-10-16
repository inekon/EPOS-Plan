# Übergabe der Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, Katalogauswahl“ (14.10.2026)

Dieses Papier übergibt die Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, Katalogauswahl“
(`session_01UNeGChCYZu2EpgFx4P3q1q`) auf den Stand 14.10.2026 an eine Sitzung auf einem anderen Konto. Es nennt den
Stand, die geltenden Entscheide, die offenen Punkte und den Einstiegs-Prompt (Abschnitt 5); die Regeln der Sitzung
über `CLAUDE.md` hinaus stehen im Prompt. Regelquelle bleibt `CLAUDE.md`. Es löst das Papier vom 11.10.2026
([`2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md`](../../ueberholt/2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md))
ab, dessen Entscheide weitergelten (Abschnitt 2). Die parallelen Sitzungen führen eigene Papiere, etwa
[`2026-10-14_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md`](2026-10-14_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md)
(Berichte & Kosten) und [`2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md`](2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md).

## 1 Stand 14.10.2026

Stand 16.10.2026: Übernahme durch Sitzung `session_019JuX7JfD2tStDzd3CiStwf`; #1105/#1106 gepusht, CI grün (Lauf 38863988800). Aufräumen #1109 (Konzept Kältebedarf und Katalogauswahl-Mockups nach `ueberholt/`). Die Basis R56 `2026-10-16_R56_Wasserkosten` (KB-7) ist eingefroren und enthält die zwei Kälteschlüssel von #1105; das `--ohne` der Gate-Skripte ist gestrichen.

- Zweig: Arbeitszweig `ios_migration_september`; alle Wellen dieser Sitzung sind gepusht und mit CI-Vermerk versehen, keine Agenten, keine offenen Worktrees. Letzter eigener Commit vor diesem Papier: `d40c38cd0` (LIESMICH-Bildzahl 257).
- Wellen und Statuszeilen: KR1/KR1b runde Wahlkästchen (#1037); K6 Kältebedarf dezentral/Split mit Schemaschritt 223 und Referenzprojekt 1072 (#1056/#1057); K6d (#1066); K6d-N (#1078); K6c (#1089, `8bf550004`); K6c-N Laufmeldung nennt die Hauptzahl (#1090, `bd7abe18a`).
- Basis R55 `Referenzlaeufe/2026-10-14_R55_KaeltebedarfDezentral` (37 Projekte; die CI rechnet 18).
- ChartProben-Messlatte `Proben/ChartProben/Messlatte_2026-10-14.sha256` mit 225 Zeilen; Vermerke #1089/#1090 in `683bc2cac` (Hashes aus Kern-Lauf 38532651105). `kern.yml` schreibt seit `b4b73a9b2` die vollständige Hashliste ins Protokoll (eingeklappte Gruppe „ChartProben-Hashliste ubuntu“); LIESMICH-Bildzahl 257 in `d40c38cd0`.
- CI-Vermerke gesetzt mit `a4e2e2bb4` (#1056/#1057), `5215d8e22` (#1066), `5f5a0023d` (#1078), `683bc2cac` (#1089/#1090).
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
- Anwenderentscheid 16.10.: Die Hauptzahl wird jetzt gemergt und gegen R55 mit `--ohne` verglichen, bis die nächste Basis sie aufnimmt.
- Anwenderentscheid 16.10.: Die Abbruchregel von `kern.yml` bleibt.
- Anwenderentscheid 16.10.: Kein Kern-Lauf für Pull Requests aus dem eigenen Repository.
- Anwenderentscheid 16.10.: Wiki-Sammel-Upload in der Woche ab 19.10.
- Anwenderentscheid 16.10.: Modellregel — Fable 5 nur für sehr komplexe Aufgaben, sonst Opus 5.5 bzw. Sonnet/Haiku je nach Aufgabe.

## 3 Hinweis

Die Regeln der Sitzung über `CLAUDE.md` hinaus stehen im Einstiegs-Prompt (Abschnitt 5).

## 4 Offene Punkte (mit Empfehlung)

1. ~~Nachweis Messlatte~~ erledigt: Kern-Lauf [38550151112](https://github.com/inekon/EPOS-Plan/actions/runs/38550151112) grün auf `ae1f65a24` (enthält `683bc2cac`), Schritt „ChartProben-Messlatte (nur ubuntu)“: 225 von 225 Zeilen unverändert.
2. Beim Anwender: Versionsnummer des Logbuchs für die zwei Entwurfsblöcke in [`../Wiki_Update_2026-09-26.md`](../Wiki_Update_2026-09-26.md); Wiki-Upload nur nach Freigabe; der Wiki-Sammel-Upload 1.2.0.11 folgt in der Woche ab 19.10., nach Freigabe; Sichtabnahme unter Windows (runde Wahlkästchen, Kältebedarf-Dialoge, Laufmeldung).
3. Erledigt: Hauptzahl in `aggregate.csv` (#1105) und CI-Kern-Lauf in parallelen Jobs (#1106), Wandzeit rund 19,5 statt 84 min.
4. Beobachten (R56 eingefroren, siehe Abschnitt 1): Schemakette 228 KB-4, 229 KB-5 (Stromspeicher/Speicherflotte bei der Sitzung „Dialoge und Korrekturen (Konto Übernahme)“/Kältemaschinen), 230 KBP-2, 231 DA-6, 232 BK-5/BK-6; Basen R56 KB-5, R57 SA4. Die Sitzung „Dialoge Cloud“ arbeitet am Konzept Kältebedarfsprofile (KBP) mit unseren drei Bitten: Deckungsgruppe und Naht `SimulationPlattformwege.KaelteDezentralBearbeiten` unverändert, neue Vorlagen nur als neue Katalogzeilen, Kalenderhinweis als Baustein.
5. Erledigt: ~~Kältebedarf K6~~ erledigt (#1056/#1057, #1066, #1078, #1089, #1090); ~~Sichtabnahme der Katalogauswahl-Wirte~~ in Punkt 2 aufgegangen; ~~Nachzüge Katalogauswahl, KM-K2, Kostenbasis KB-2/KB-3~~ erledigt (Statuszeilen #1034, #1038, #1079, #1080).

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
