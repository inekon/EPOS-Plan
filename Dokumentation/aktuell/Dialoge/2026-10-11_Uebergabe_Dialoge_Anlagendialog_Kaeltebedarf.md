# Übergabe der Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, KAL1“ (11.10.2026)

Dieses Papier übergibt die Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, KAL1“
an eine neue Sitzung. Es nennt den Stand, die geltenden Entscheide, die Regeln der Sitzung und
die offenen Punkte; der Einstiegs-Prompt steht in Abschnitt 5. Regelquelle bleibt `CLAUDE.md`.
Es ergänzt das Papier der parallel arbeitenden Sitzung „Dialoge und Korrekturen, Konto 3“
([`2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md`](2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md))
und löst das Papier der Vorgängerrolle
([`2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md`](../../ueberholt/2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md))
für die hier genannten Gegenstände ab.

## 1 Stand

- Zweige: Arbeitszweig `ios_migration_september`; Sitzungszweig `claude/cool-ramanujan-3m1lhl`, nach jedem Push gleich dem Arbeitszweig; letzter Push vor diesem Papier `f2caa6008` (Statuszeile #1009).
- Statuszeilen dieser Sitzung (11.10.2026): #980 K2 Kernwege Kälte, #981 K3 Rechenweg Kältebedarf mit Abzugsstelle für K‑D, #982 K4 Startseite Reiter „Kältebedarf“ (Nachtrag: Hotfix doppelte BOM in `epos-ui.css`); #988 AW1 Knopfleiste am gewählten Satz (Heizkessel, Wärmepumpe); #989 AW2 Wärmepumpe: Satz-Überlagerung und Anlage zusammengeführt; #998 AW3a fünf Erzeugerdialoge; #999 AW3b Stufe 4/5 und Kältemaschine; #1009 KAL1 Einzeltage als Klappgruppe.
- CI-Vermerke: an #960–#965, #980–#982, #988/#989, #998/#999, #1009 ausstehend. Am 11.10. haben die Pushes der parallelen Sitzungen jeden Kern-Lauf abgebrochen; nachtragen, sobald ein grüner Lauf existiert, dessen headSha den jeweiligen Push enthält (Muster Commit `ba33792d8`).
- Katalogauswahl V1: alle Stufen gebaut; Knopfleiste am gewählten Satz (KA‑E‑17 bis KA‑E‑22, Konzept Abschnitt 4.4a) in allen Wirten; Wärmepumpe mit zusammengeführter Überlagerung (`WaermepumpeAnlageDialog` → Baustein `WaermepumpeAnlageFelder`).
- Kältebedarf: Konzept `Konzept_Kaeltebedarf_EPOS-Plan.md`; K1–K4 gebaut, K5 und K6 offen.
- Basis R52 `2026-10-11_R52_Vorheizen`; Schemaschritt zuletzt laut Kopfzeile der Statusdatei: 219 angemeldet (Sitzung Gebäudesimulation), 220 frei; von dieser Sitzung ist keiner belegt.
- Abgestimmt mit der Sitzung Gebäudesimulation (11.10.): Gebäudedialog/Mehrzonen-Import (ihr Entwurf GD, #1004) und DIN‑18599‑10‑Normprofile liegen dort; Kalenderprobe-Reparatur (`de5043b0a`) und Konditionierungsprobe-Prüfzeile (`516199024`) von uns, mit ihnen abgestimmt.
- Es laufen keine Agenten, keine offenen Worktrees.

## 2 Geltende Entscheide (Auswahl)

- Kältebedarf (11.10.): K2–K4 als nächste Welle; K3 hier gebaut, abgestimmt mit Kälteanlagen — Abzugsstelle `SimulationKaeltebedarf.AbzugVorDerKaskade(h)`/`BedarfDerKaskade(…)` ist der Andockpunkt für K‑D; Split-Strom je Kühlträger und Zähler erst mit K5/K6 als Schemaschritt (bis dahin Stromträger des Projekts mit Hinweis); Statusbit „Wärmebedarfsdaten“ (16) zählt weiterhin Kühlzeilen.
- Anlagendialog (11.10., KA‑E‑17 bis 22): nur Kästchen, gewählte Projektzeile durch Hervorhebung, Klick wählt, Ankreuzen wählt nicht; Katalog-Knopfleiste in der Zeile „gewählter Satz“, Stift dort entfällt, Stifte je Listenzeile bleiben = Bearbeiten…; Schloss setzen… und Löschen beim Projektsatz weich gesperrt „Katalogsatz wählen“, Neu… immer Katalogsatz, Vergleichen… je Bereich; Bearbeiten… im Projektkopf entfällt; Geltung alle Wirte. Wärmepumpe: Bearbeiten… öffnet für Projekt- und Katalogsatz dieselbe Überlagerung aus Satz und Anlage, Auswahlliste „Wärmepumpen Auswahl“ mit „Modul-Katalog…“ entfällt, je Aufruf eine Anlage mit ihrer Kennlinie, Abschnitte ohne Daten beim Katalogsatz weich gesperrt „nur im Projekt“; Knopfleiste und Schlussleiste der Wärmepumpe wie die anderen Dialoge.
- Zur Entscheidung vorgelegt, Empfehlung „so lassen“, Antwort offen: „Vergrößern“ am Projektsatz des Gebäudes (Satz des Projektgebäudes: Ausrichtung, Fläche, Export, Simulation) und „Vergrößern“ neben Bearbeiten… bei Wärmebedarf extern und Stromganglinie.
- KAL1 (11.10.): Abschnitt „Einzeltage (Feiertage, Ausnahmen)“ als Klappgruppe, zugeklappt beim Öffnen, Anzahl im Kopf, „+ Einzeltag“ und „Feiertage laden“ in der Kopfzeile.
- Arbeitsweise: Push am Ende jeder Welle freigegeben; kein Wiki-Upload ohne Rückfrage; Modellregel des Anwenders: Fable 5.1 orchestriert, Opus 5.5 setzt um, Sonnet/Haiku für Mechanik und Zählungen.

## 3 Regeln der Sitzung

- Wie im Papier von Konto 3, Abschnitt 3
  ([`2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md`](2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md)),
  dazu:
- Nach jedem Merge mit origin die ChartProben-Messlatte beachten: Auf einem Container mit anderer Schriftkette (Carlito) weichen alle 222 Hashes ab; der Nachweis ist dann der Vergleichslauf derselben Maschine auf dem origin-Stand (ChartProben-LIESMICH), die CI hält die Messlatte.
- `git lfs install --local` im Container, sonst liegt nach einem Merge ein Zeiger.
- Browserproben: eigener Port, nur die eigene PID beenden; ein fremder Wirt lief auf 5371.

## 4 Offene Punkte (mit Empfehlung)

1. CI-Vermerke (Abschnitt 1) — zuerst.
2. Sichtabnahme unter Windows: AW1–AW3 (alle Wirte), die zwei „Vergrößern“, Kältebedarf-Reiter samt zwei abgeleiteten Kachelbildern (`PKaelteMessdaten.jpg`, `PKaelteProfil.jpg`), KAL1-Klappgruppe, Wärmepumpen-Überlagerung.
3. Kältebedarf K5 und K6 (Konzept Abschnitt 7): K5 Modus „nur Kühlung“ im Dialog Wärmebedarf extern (Hülle `WaermebedarfExternHuelle.NachModell` führt die Deckungsfelder noch nicht mit), Deckungsfelder im Bedarfsprofile-Dialog, Sprungziel Kühlung im Gebäudedialog, Hinweis OP‑2, Ablehnung „Split ohne EER“ im Dialog; K6 Fläche „dezentral (Split)“ in `KaelteProduktionBild`, Kältegang, Bedarfsreiter, Posten „Kühlung dezentral“ in der Strombilanz (Reihe `Kaeltestrom_Dezentral_stuendlich`), `ProjektDetails`, Deckungsgrad mit/ohne dezentralen Anteil, Gruppe „Dezentrale Kältedeckung“, Schemaschritt für die Abrechnung des Split-Stroms je Träger, Wiki-Seite Kältebedarf, ggf. Referenzprojekt mit Basis (Nummer mit Gebäudesimulation abstimmen).
4. Nachzüge der Katalogauswahl: Kommentarblöcke über `<Zweispaltenauswahl>` in den Erzeugerdialogen (alte Ortsverteilung), Regel `.epos-wp-eingebettet` in `epos-ui.css` ungeprüft, Kommentare mit `WaermepumpeAnlageDialog`, unbenutzte `KMA_*`-Ressourcen, Bedarfsprofile `vormerkung.Entfernt`, Gebäude Jahreswärmebedarf in der Zusammenfassung; danach Konzept Katalogauswahl nach `ueberholt/` (nach K5).
5. Wiki: Repo-Quellen aller elf Wirte, Startseite Kältebedarf, Gebäude (Einzeltage) fortgeschrieben; Bilder neu ziehen (`wikibilder.mjs`); Sammel-Upload nur nach Rückfrage mit Versionsnummer; 33 Logbuchsätze unter 1.2.0.9.
6. Fremde Befunde, nur gemeldet: Katalogprobe 8 Querroll-Verstöße in Verwaltungen N13b–N22a (Stammblatt 4–27 px); Fensterprobe `dubletten 1194x834`; Wärmebedarf extern unter der Mindesthöhe (1 093 × 614) 97 px.
7. Bei der Gebäudesimulation: Gebäudedialog/Mehrzonen-Import (GD), Normprofile DIN 18599‑10 (Außenluft je Person, KI-Anweisungsweg).

## 5 Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, KAL1“ für
EPOS-Plan (Repo inekon/EPOS-Plan, Arbeitszweig ios_migration_september). Lies zuerst CLAUDE.md und
Dokumentation/aktuell/Dialoge/2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md,
dann den Kopf und die letzten zwanzig Zeilen der Tabelle in
Dokumentation/aktuell/Status_iOS_Migration.md und
`git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september`.

Rolle: du orchestrierst mit Opus 5.5 und arbeitest selbst nur, wenn ein Agentenaufruf teurer
wäre; opus-umsetzung für Dialoge, Hüllen, Rechenweg, Tests, Fehlersuche und Konfliktauflösung mit
Fachinhalt; sonnet-mechanik für Merge, Gate, Statuszeilen, Logbuchsätze, Push und CI-Vermerk;
haiku-pruefung für Zählungen. Modell bei jedem Agentenaufruf ausdrücklich setzen, höchstens zwei
Agenten parallel. Agenten im eigenen Worktree unter .claude/worktrees/<name>, eigener Unterordner
im Scratchpad, zuerst `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite`, sofort
committen, nicht pushen, bin/ und obj/ löschen. Probenwirt auf eigenem Port, nur über die eigene
PID beenden. Agenten laufen die betroffenen Testklassen aller angefassten Dialoge und Controller.

Wellenfolge: Merge → Gate (GATE_ABLAGE=<scratchpad>/gate bash Werkzeuge/Gate/gate_linux.sh <Nr>
<wurzel>, rund 80 Minuten, als ein wartender, überwachter Hintergrundbefehl; nie zwei Gates
gleichzeitig) → Statuszeile und Logbuchsatz (Wiki_Update_2026-09-26.md, 1.2.0.9) → Push →
CI-Vermerk allein mit [skip ci]. Der Push am Ende jeder Welle ist freigegeben; gepusht werden der
Arbeitszweig und der Sitzungszweig, den die Umgebung vorgibt. Statusnummer unmittelbar vor dem
Commit gegen origin messen, genau vier Zellen je Zeile. Schemaschritte vor dem Bau in der
Kopfzeile der Statusdatei anmelden und allein pushen. Nie ohne Rückfrage: iOS-, macOS-,
Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags. Commit-Trailer: Co-Authored-By des
arbeitenden Modells und Claude-Session mit der URL deiner Sitzung. Vor jedem Auftrag prüfen, ob
der Wunsch schon gebaut ist. Antworten auf Deutsch, knapp.

Parallel arbeiten auf demselben Arbeitszweig die Sitzung „Dialoge und Korrekturen, Konto 3“
(KT-Wellen) und die Sitzungen Gebäudesimulation, Kälteanlagen, Administration und Support,
Berichterstellung und Wiki; Statusnummern, Basen und Schemaschritte werden schnell vergeben.

Erste Schritte: (1) Stand prüfen: `git status` sauber, keine Konfliktmarker, keine liegen
gebliebene AGENT_LAEUFT, die Zeilen #960–#965, #980–#982, #988/#989, #998/#999, #1009 tragen
ihren CI-Vermerk. (2) Den Stand in höchstens fünf Zeilen melden. (3) Mit dem Anwender abstimmen,
womit weitergemacht wird. Empfehlung: zuerst die CI-Vermerke, dann die Rückmeldungen des
Anwenders aus der Sichtabnahme und die Nachzüge der Katalogauswahl, danach Kältebedarf K5/K6.
```
