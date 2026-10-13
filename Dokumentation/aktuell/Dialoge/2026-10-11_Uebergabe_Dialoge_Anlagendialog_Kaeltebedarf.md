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

- Zweige: Arbeitszweig `ios_migration_september`; Sitzungszweig `claude/cool-ramanujan-3m1lhl`, nach jedem Push gleich dem Arbeitszweig; letzter Push `37f557526` (Statuscommit #1029 KM‑L1/KM‑K1); Arbeitsbaum sauber, keine Agenten, keine Worktrees. Alle CI-Vermerke sind eingetragen, keine Check-ins mehr aktiv.
- Statuszeilen dieser Sitzung (11.10.2026): #980 K2 Kernwege Kälte, #981 K3 Rechenweg Kältebedarf mit Abzugsstelle für K‑D, #982 K4 Startseite Reiter „Kältebedarf“ (Nachtrag: Hotfix doppelte BOM in `epos-ui.css`); #988 AW1 Knopfleiste am gewählten Satz (Heizkessel, Wärmepumpe); #989 AW2 Wärmepumpe: Satz-Überlagerung und Anlage zusammengeführt; #998 AW3a fünf Erzeugerdialoge; #999 AW3b Stufe 4/5 und Kältemaschine; #1009 KAL1 Einzeltage als Klappgruppe.
- CI-Vermerke: #960–#965, #980–#982, #988/#989, #998/#999 tragen den Kern-Lauf 38181227802 (grün auf `3d7172064`), #1009 den Kern-Lauf 38186204735 (grün auf `f2caa6008`). #1024/#1025 (Lauf 38215901446 auf `c9d5ed5b1`) und #1027–#1029 (Lauf 38226571128 auf `37f557526`) vermerkt. Keine Vermerke ausstehend.
- Katalogauswahl V1: alle Stufen gebaut; Knopfleiste am gewählten Satz (KA‑E‑17 bis KA‑E‑22, Konzept Abschnitt 4.4a) in allen Wirten; Wärmepumpe mit zusammengeführter Überlagerung (`WaermepumpeAnlageDialog` → Baustein `WaermepumpeAnlageFelder`); Nachzüge NK1 (Statuszeile #1021) geschlossen.
- Kältebedarf: Konzept `Konzept_Kaeltebedarf_EPOS-Plan.md`; K1–K5 gebaut (K5 #1024), K6 offen.
- Vergleich Projekt/Katalog (KA‑E‑24, #1025) gebaut.
- Gerätesteckbrief (KA‑E‑25, #1027/#1028) in allen acht Erzeuger-Überlagerungen und bei Wärme-/Kältebedarf extern gebaut; Kältemaschine folgt mit KM‑K1.
- Kältemaschinen-Dialog (KA‑E‑26/27, #1029) gebaut: Wärmepumpen mit Kühlfunktion im Katalog, Layout.
- Basis R52 `2026-10-11_R52_Vorheizen`; Schemaschritt: es gilt allein die Kopfzeile „Schemaschritt angemeldet“ der Statusdatei (zuletzt 222 frei); von dieser Sitzung ist keiner belegt.
- Abgestimmt mit der Sitzung Gebäudesimulation (11.10.): Gebäudedialog/Mehrzonen-Import (ihr Entwurf GD, #1004) und DIN‑18599‑10‑Normprofile liegen dort; Kalenderprobe-Reparatur (`de5043b0a`) und Konditionierungsprobe-Prüfzeile (`516199024`) von uns, mit ihnen abgestimmt.
- Übersicht in Berichte & Kosten (Statuszeile #1033, 13.10.2026, Sitzung `session_01Mt7MsJGzcGr77yqBsxwpLj`): Vergleichstabelle je Gewerk mit Kennwerten und markierten Abweichungen, Kopf in drei Teilen, KI-Sicht erweitert. Sichtabnahme unter Windows und die Version des Logbuchsatzes sind offen.
- Es laufen keine Agenten, keine offenen Worktrees.

## 2 Geltende Entscheide (Auswahl)

- Kältebedarf (11.10.): K2–K4 als nächste Welle; K3 hier gebaut, abgestimmt mit Kälteanlagen — Abzugsstelle `SimulationKaeltebedarf.AbzugVorDerKaskade(h)`/`BedarfDerKaskade(…)` ist der Andockpunkt für K‑D; Split-Strom je Kühlträger und Zähler erst mit K6 als Schemaschritt (bis dahin Stromträger des Projekts mit Hinweis); Statusbit „Wärmebedarfsdaten“ (16) zählt weiterhin Kühlzeilen.
- Anlagendialog (11.10., KA‑E‑17 bis 22): nur Kästchen, gewählte Projektzeile durch Hervorhebung, Klick wählt, Ankreuzen wählt nicht; Katalog-Knopfleiste in der Zeile „gewählter Satz“, Stift dort entfällt, Stifte je Listenzeile bleiben = Bearbeiten…; Schloss setzen… und Löschen beim Projektsatz weich gesperrt „Katalogsatz wählen“, Neu… immer Katalogsatz, Vergleichen… je Bereich; Bearbeiten… im Projektkopf entfällt; Geltung alle Wirte. Wärmepumpe: Bearbeiten… öffnet für Projekt- und Katalogsatz dieselbe Überlagerung aus Satz und Anlage, Auswahlliste „Wärmepumpen Auswahl“ mit „Modul-Katalog…“ entfällt, je Aufruf eine Anlage mit ihrer Kennlinie, Abschnitte ohne Daten beim Katalogsatz weich gesperrt „nur im Projekt“; Knopfleiste und Schlussleiste der Wärmepumpe wie die anderen Dialoge.
- Zur Entscheidung vorgelegt, Empfehlung „so lassen“, Antwort offen: „Vergrößern“ am Projektsatz des Gebäudes (Satz des Projektgebäudes: Ausrichtung, Fläche, Export, Simulation) und „Vergrößern“ neben Bearbeiten… bei Wärmebedarf extern und Stromganglinie.
- KAL1 (11.10.): Abschnitt „Einzeltage (Feiertage, Ausnahmen)“ als Klappgruppe, zugeklappt beim Öffnen, Anzahl im Kopf, „+ Einzeltag“ und „Feiertage laden“ in der Kopfzeile.
- Arbeitsweise: Push am Ende jeder Welle freigegeben; kein Wiki-Upload ohne Rückfrage; Modellregel des Anwenders: Fable 5.1 orchestriert, Opus 5.5 setzt um, Sonnet/Haiku für Mechanik und Zählungen.
- Dialoge 12.10. (KA‑E‑23 bis KA‑E‑27, Konzept Katalogauswahl Abschnitt 3): Wärme-/Kältebedarf extern folgt dem Gerüst des Wärmepumpendialogs (Satzzeile mit Kurzangaben und Knopfleiste, eine Bearbeiten-Überlagerung für Projekt- und Katalogsatz); Vergleichen… vergleicht Projektzeilen und Katalogsätze gemeinsam (zwei bis drei, Herkunft im Spaltenkopf, sonst weich gesperrt); jede Bearbeiten-Überlagerung zeigt links einen Gerätesteckbrief (Wärmepumpe mit Arbeitspunkt am eingestellten Vorlauf); Wärmepumpen mit Kühlkennlinie stehen in der Katalogauswahl des Kältemaschinen-Dialogs (Übernehmen = Projektwärmepumpe mit Kühlbetrieb über `WPCtrl.KuehlkonfigurationSchreiben`, Rang in der Kältefolge anhängen, Entfernen schaltet nur den Kühlbetrieb aus und entfernt den Rang, Bezugspunkt 7 °C Kaltwasser mit Rückfall, Vergleich nur innerhalb einer Art, Zulässigkeit je Art, eine Zeile je Projektkopie); Knopfleiste bricht um und bleibt in der Dialogbreite, Katalogfunktionen im Katalogkopf, Infoknöpfe im Satzkopf (zunächst Kältemaschine). Bearbeiten… einer Wärmepumpe im Kältemaschinen-Dialog ist bis KM‑K2 weich gesperrt.
- Konfiguration Wärmepumpe (12.10.): „Betriebszeiten“ als Klappgruppe, zugeklappt beim Öffnen, Stand im Kopf; Deckungsgruppe der Kältedialoge in der Bearbeiten-Überlagerung, kein eigener Knopf.

## 3 Regeln der Sitzung

- Wie im Papier von Konto 3, Abschnitt 3
  ([`2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md`](2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md)),
  dazu:
- Nach jedem Merge mit origin die ChartProben-Messlatte beachten: Auf einem Container mit anderer Schriftkette (Carlito) weichen alle 222 Hashes ab; der Nachweis ist dann der Vergleichslauf derselben Maschine auf dem origin-Stand (ChartProben-LIESMICH), die CI hält die Messlatte.
- `git lfs install --local` im Container, sonst liegt nach einem Merge ein Zeiger.
- Browserproben: eigener Port, nur die eigene PID beenden; ein fremder Wirt lief auf 5371.
- Fehlt im Container das .NET-SDK und sperrt der Proxy `builds.dotnet.microsoft.com` und `download.visualstudio.microsoft.com`: das Ubuntu-Paket `dotnet-sdk-10.0` samt Abhängigkeiten mit `apt-get download` holen, mit `dpkg-deb -x` benutzerlokal entpacken, `~/.dotnet` auf `usr/lib/dotnet` des Entpackordners verweisen und im SDK-Ordner den Alias `sdk/10.0.400` auf `10.0.112` legen. Dauerhaft gehört die SDK-Installation ins Setup-Skript der Cloud-Umgebung.

## 4 Offene Punkte (mit Empfehlung)

1. Sichtabnahme unter Windows: AW1–AW3 (alle Wirte), die zwei „Vergrößern“, Kältebedarf-Reiter samt zwei abgeleiteten Kachelbildern (`PKaelteMessdaten.jpg`, `PKaelteProfil.jpg`), KAL1-Klappgruppe, Wärmepumpen-Überlagerung; dazu vom 12.10.: Kältebedarf extern und Kältebedarfsprofile mit Deckung (K5), Vergleich Projekt/Katalog (VG1), Gerätesteckbrief in allen Überlagerungen und Betriebszeiten-Klappgruppe (WP1), Kältemaschinen-Dialog mit Wärmepumpen und Layout (KM‑K1), Übersicht in Berichte & Kosten (#1033). Rückmeldungen dazu sind die erste Welle der neuen Sitzung.
2. Kältebedarf K6 (Konzept Abschnitt 7): Fläche „dezentral (Split)“ in `KaelteProduktionBild`, Kältegang, Bedarfsreiter, Posten „Kühlung dezentral“ in der Strombilanz (Reihe `Kaeltestrom_Dezentral_stuendlich`), `ProjektDetails`, Deckungsgrad mit/ohne dezentralen Anteil, Gruppe „Dezentrale Kältedeckung“, Schemaschritt für die Abrechnung des Split-Stroms je Träger, Wiki-Seite Kältebedarf, ggf. Referenzprojekt mit Basis (Nummer mit Gebäudesimulation abstimmen).
3. Nachzüge der Katalogauswahl, Rest: Vormerkung beim OK für Stromverbraucher, Prozesswärme und Kälte (Zuordnungen in `StartseiteHuelle` nach dem Schließen — Vormerkung erst danach abschließen); tote Regel `.epos-dialog--eingebettet` samt Probenfall `warnband` in `fensterprobe.mjs` (mit Fensterprobe); `WaermepumpenKatalogDialog` ohne Wirt (Aufräumkandidat); danach Konzept Katalogauswahl nach `ueberholt/` (nach K5).
4. Wiki: Repo-Quellen aller elf Wirte, Startseite Kältebedarf, Gebäude (Einzeltage) fortgeschrieben; Bilder neu ziehen (`wikibilder.mjs`); Sammel-Upload nur nach Rückfrage mit Versionsnummer; 33 Logbuchsätze unter 1.2.0.9.
5. Fremde Befunde, nur gemeldet: Katalogprobe 8 Querroll-Verstöße in Verwaltungen N13b–N22a (Stammblatt 4–27 px); Fensterprobe `dubletten 1194x834`.
6. Bei der Gebäudesimulation: Gebäudedialog/Mehrzonen-Import (GD), Normprofile DIN 18599‑10 (Außenluft je Person, KI-Anweisungsweg), Jahreswärmebedarf in der Zusammenfassung des Gebäudedialogs (übergeben, Konzept Katalogauswahl Abschnitt 5).
7. (erledigt mit Statuszeile #1034) Nachzug KM‑K2: Naht für eine Wärmepumpen-Anlagenzeile (`WaermepumpeAnlageGaben`), dann öffnet Bearbeiten… im Kältemaschinen-Dialog die Wärmepumpen-Überlagerung; Wärmekaskade bei erster Wärmepumpe aus dem Kältedialog; Verwendungsmarke je Projektkopie; drei unbenutzte Textfelder/Ressourcen der entfallenen Zeile „Wärmepumpen im Kühlbetrieb" entfernen.

## 5 Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Dialoge und Korrekturen — Anlagendialog, Kältebedarf, Katalogauswahl“ für
EPOS-Plan (Repo inekon/EPOS-Plan, Arbeitszweig ios_migration_september) aus einem anderen Konto.
Lies zuerst CLAUDE.md und
Dokumentation/aktuell/Dialoge/2026-10-11_Uebergabe_Dialoge_Anlagendialog_Kaeltebedarf.md,
dann den Kopf (Zeile „Schemaschritt angemeldet“) und die letzten zwanzig Tabellenzeilen von
Dokumentation/aktuell/Status_iOS_Migration.md sowie
`git log --format='%h %<(72,trunc)%s' -n 25 origin/ios_migration_september`.
Stand bei der Übergabe (12.10.2026): letzter Push der Vorgängersitzung 37f557526 (Statuszeile #1029);
Statuszeilen des 12.10.: #1021 NK1, #1024 K5, #1025 VG1, #1027 WP1a, #1028 WP1b, #1029 KM‑L1/KM‑K1;
Alle CI-Vermerke bis #1029 eingetragen (Muster Commit 534664c4c). Basis R52 2026-10-11_R52_Vorheizen. Die
Kopfzeile der Statusdatei gilt vor dem Papier, wenn sie abweicht; der Schemaschritt wird dort gemessen.

Rolle: Fable 5.1 orchestriert und arbeitet selbst nur, wenn ein Agentenaufruf teurer wäre als die
Änderung (eine Zeile, ein Verweis). opus-umsetzung (model: opus) für Dialoge, Hüllen, Rechenweg,
Tests, Fehlersuche, Konzeptabsätze mit Fachinhalt und Konfliktauflösung mit Fachinhalt;
sonnet-mechanik (model: sonnet) für Merge ohne Fachkonflikt, Gate, Statuszeilen, Logbuchsätze,
Übergabepapier, Push und CI-Vermerk; haiku-pruefung (model: haiku) für Zählungen. Modell bei jedem
Agentenaufruf ausdrücklich setzen, höchstens zwei Agenten parallel. Agenten arbeiten im eigenen
Worktree unter .claude/worktrees/<name> (Zweig gleichen Namens) mit eigenem Unterordner im Scratchpad,
zuerst `git lfs install --local` und `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite`,
committen sofort, pushen nicht, löschen bin/ und obj/; Ressourcen in beiden Sprachen, neue Schlüssel
bei parallelen Agenten an verschiedenen Stellen der .resx einfügen (nicht beide ans Ende), Designer
nach dem Merge neu erzeugen. Probenwirt auf eigenem Port, nur die eigene PID beenden. Vor Agentenarbeit
im Hauptbaum AGENT_LAEUFT anlegen, nach der Abnahme löschen. Nach jedem Merge die Ressourcen-XML
prüfen (jedes <data> mit </data>); ein Vorgänger hat beim Auflösen Schlusstags verloren.

Wellenfolge: Merge → Gate → Statuszeile und Logbuchsatz (Wiki_Update_2026-09-26.md, Version 1.2.0.9) →
Push → CI-Vermerk allein mit [skip ci]. Gate: `GATE_ABLAGE=<scratchpad>/gate bash
Werkzeuge/Gate/gate_linux.sh <Nr> <wurzel>` (rund 80 Minuten) bei Rechenweg- oder Schemaänderung,
sonst das Restgate `bash Werkzeuge/Gate/gate_rest_linux.sh <wurzel> <ablage>` (rund 15 Minuten, alle
UI-Tests, Referenzlauf); als ein wartender Hintergrundbefehl, nie zwei Gates gleichzeitig; mehrere
fertige Wellen dürfen ein gemeinsames Gate und einen Statuscommit mit mehreren Zeilen bekommen. Der
Push am Ende jeder Welle ist freigegeben; gepusht werden Arbeitszweig und der Sitzungszweig, den die
Umgebung vorgibt. Statusnummer unmittelbar vor dem Commit gegen origin messen, hinter die höchste
fremde einordnen, genau vier Zellen je Zeile. Schemaschritte vor dem Bau in der Kopfzeile der
Statusdatei anmelden und allein pushen. ChartProben-Messlatte: auf einem Container mit Carlito-Schrift
weichen alle 222 Hashes ab; Nachweis ist der Vergleichslauf derselben Maschine auf dem origin-Stand,
die CI hält die Messlatte. Nie ohne Rückfrage: iOS-, macOS-, Setup-Läufe, Wiki-Upload, Pull Requests,
Force-Push, Tags. Commit-Trailer: Co-Authored-By des arbeitenden Modells und Claude-Session mit der
URL dieser Sitzung. Dialogwünsche mit Probenfotos zur Sichtabnahme vorlegen (SendUserFile).
Stop-Hook-Meldungen „unpushed commits“ sind kein Auftrag. Antworten auf Deutsch, knapp;
Zwischenstände nur auf Nachfrage; beim Kontingent sparsam, der Anwender nennt den Stand des
Wochenlimits.

Parallel auf demselben Arbeitszweig: Gebäudesimulation (Gebäudedialog/Mehrzonen-Import GD,
DIN‑18599‑10‑Normprofile, Sprungziel „Kühlung“ und Jahreswärmebedarf im Gebäudedialog liegen dort),
Kälteanlagen (Rechenweg KWN‑1, Oberfläche Rückkühlwerk K‑F5 — Anlagengruppe „Rückkühlung“ nicht
anfassen, Reihenfolge abstimmen), Konto 3 „Dialoge und Korrekturen“ (KT-Wellen), Administration und
Support, Berichterstellung, Wiki. Statusnummern, Basen und Schemaschritte werden schnell vergeben;
Abstimmung über send_message an die Sitzungen (list_sessions), nicht über Dateien.

Offen beim Anwender: Sichtabnahme unter Windows (Papier Abschnitt 4, Punkt 1) und der Entscheid zu den
zwei „Vergrößern“-Knöpfen (Empfehlung: so lassen).

Erste Schritte: (1) Stand prüfen: `git status` sauber, keine Konfliktmarker, keine liegen gebliebene
AGENT_LAEUFT, Testdatenbank kein LFS-Zeiger, Ressourcen-XML gültig. (2) Stand in höchstens fünf Zeilen
melden. (3) Mit dem Anwender
abstimmen, womit weitergemacht wird. Empfehlung: zuerst die Rückmeldungen aus der Sichtabnahme, dann
KM‑K2 (Papier Abschnitt 4, Punkt 7: Naht für eine Wärmepumpen-Anlagenzeile, Bearbeiten… im
Kältemaschinen-Dialog, Wärmekaskade, Verwendungsmarke, drei unbenutzte Textfelder; Opus, Restgate),
danach die Nachzüge aus Punkt 3, dann Kältebedarf K6 (Konzept_Kaeltebedarf_EPOS-Plan.md Abschnitt 7;
Schemaschritt für den Split-Strom je Träger vorher anmelden; Vollgate; Referenzprojekt mit neuer
Basis mit der Gebäudesimulation abstimmen).
```
