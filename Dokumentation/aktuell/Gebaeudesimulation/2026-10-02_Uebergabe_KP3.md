# Übergabe KP3 — Stand beim Anhalten am 02.10.2026, Fortsetzung am 03.10.2026 um 10 Uhr

Sitzung „Gebäudesimulation“ (Cloud, Orchestrierung Fable 5.1, Agenten Opus 5.5), Zweig `ios_migration_september`. Dieses Papier
hält alles, was die Fortsetzung braucht; es wandert nach `Dokumentation/ueberholt/` sobald KP3 abgeschlossen ist. Grundlage und
Regeln: [`CLAUDE.md`](../../../CLAUDE.md), [Entwurf KP3](2026-10-02_Entwurf_KP3.md),
[Protokoll KP3](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-02_KP3_Aufheizoptimierung.md),
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md), [Statusdatei iOS-Migration](../Status_iOS_Migration.md).

## 1. Stand der Umsetzung KP3 (Aufheizoptimierung)

| Welle | Inhalt | Stand |
|---|---|---|
| Entwurf, E58 | Entwurf KP3, acht Fragen entschieden (F7 ρ nach Messung → P14) | gepusht, #652 |
| R1, D1 | Aufheizantwort, Stufenformel, Kappungsanteil; Schema 160/161, `Aufheizvorgabe` | gepusht, #658 |
| R2, O1 | Aufheizplan Einzone, Einbau; Projekteinstellung, Hülle, Assistent | gepusht, #663 |
| R3 | Mehrzonen: Nachbarform, Zonenzustände, `Aufheizgebaeude` | gepusht, CI grün, #664 |
| R4 | Kappungsreihe, W3, Rampenmaske, `Aufheizergebnis`, Hinweise `SIMENG_AUFH_*` | gemergt, Gate 665 und Gate 668 grün; Statuszeile **#669** (origin hat #665–#668 vergeben) |
| Merge origin | #665–#668 (Solarthermie-Ganglinie, M1a, M3a, **M2 mit Basis R32**, Testdatenbank 165) | gemergt (`8a73e236`); KP3 friert künftig als **R33** ein (`df81974b`) |
| Gate 669 | volles Gate im Hauptbaum auf dem Merge-Stand | siehe Abschnitt 2 |
| D2 | Kennzahlen je Gebäude und Zone, Sommerlüftung NULL, Export E32, Bedarfsergebnis, Auskunft, Herleitungszeile, Hinweis Verbrauchsangabe | Agent fertig gebaut (fünf Commits im Worktree `kp3-d2`, Zweig `kp3-d2` auf `4b4a84e2`), Gate 670 lief im Worktree; **Bericht und Abnahme offen** |
| E59 | individuelle Rampe, Aufschlag (h und %), manuelle Aufheizzeit je Gebäude mit Vorschlägen | **entschieden 02.10.2026**, Papiere noch nicht geschrieben (Auftrag in Abschnitt 5), Welle R5/O1b vor RP1 |
| O2, O3, RP1, RP2, A | Bedarfsdialog; Bericht; Referenzprojekt 1051 mit Messung ρ_min; Basis R33; Abschluss | offen |

**Beim Anwender offen:** SA1 (KP2) und SA-KP3 unter Windows, Logbuch-Versionsnummer („noch offen“ → Platzhalter „Version <vom
Anwender>“), Entscheid P14 (ρ) nach der Messung in RP1.

## 2. Zustand von Hauptbaum, origin und Worktrees beim Anhalten

- Hauptbaum `/home/user/EPOS-Plan`, Zweig `ios_migration_september`: lokaler Kopf siehe Statuszeile #669 und die Commits danach
  (`git log --format='%h %<(72,trunc)%s' -n 20`); der Push nach origin erfolgt nach grünem Gate 669 (Eintrag im Block „Nach #669“).
- Worktree `.claude/worktrees/kp3-d2` (Zweig `kp3-d2`, Basis `4b4a84e2` = R4 + origin #665–#667, Testdatenbank 164): Commits
  `ed7bf075` Ergebniszeile, `33ea28be` Export, `bdd08341` Auskunft und Herleitungszeile, `7fb53e94` Hinweis Verbrauchsangabe,
  `5ac9377e` N-AH7 Zeile/Export. Gate-Ablage `/tmp/gate_d2/GATE670`. Abnahme morgen: Bericht lesen, `git merge --no-ff kp3-d2`
  (Konflikte an `.resx` wie gehabt: origin-Stand plus neue Schlüssel, `designer_neu.py schreiben`), Gate im Hauptbaum gegen die
  Basis R32 (16 Projekte, `Referenzlaeufe/2026-10-02_R32_Solarthermie`), Statuszeile (nächste freie Nummer **spät gegen origin**),
  Push, CI-Vermerk.
- Vor jeder neuen Agentenwelle: Nachfrage zur Wochennutzung (Halt bei 90 %); macOS-, iOS- und Setup-Läufe nur nach Rückfrage.
- Testdatenbank in Worktrees: `cp` aus dem Hauptbaum, `git update-index --refresh`; `git status` zeigt sie trotzdem als geändert
  (LFS-Statcache), `git diff --quiet -- Referenzlaeufe/Kenndaten_Test.sqlite` ist leer — nie committen. Neue Zeigerdatei von origin:
  `git lfs fetch origin origin/ios_migration_september`, dann `git cat-file -p origin/ios_migration_september:Referenzlaeufe/Kenndaten_Test.sqlite | git lfs smudge -- Referenzlaeufe/Kenndaten_Test.sqlite > /tmp/kdb.sqlite && cp …`.
- Umgebung: `export DOTNET_ROOT=$HOME/.dotnet; export PATH=$HOME/.dotnet:$PATH`; Gate der Orchestrierung
  `scratchpad/gate_haupt.sh <Nr> /home/user/EPOS-Plan` (ruft `Werkzeuge/Gate/gate_linux.sh`, Windows-Schale, Designer, BOM,
  Konfliktmarker); Statusskripte `scratchpad/kp3/statuszeile_66x.py` als Muster (Zeile, Block „Nach #NNN“, Protokoll-Abschnitte,
  Nachweistabelle vor `## 7. Offen`, KP3-Zeile der Gebäudesimulation-Statusdatei).

## 3. Reihenfolge der Fortsetzung

1. Gate-Ergebnis 669 und Push prüfen (`git fetch`, `git rev-list --count origin/ios_migration_september..HEAD` = 0), CI-Lauf von
   `kern.yml` für den Push lesen und als Vermerk in Zeile #669 nachtragen (Muster #664).
2. D2 abnehmen (Abschnitt 2), Statuszeile, Push, CI-Vermerk.
3. E59-Papiere durch einen Opus-Agenten im Worktree `kp3-e59` (Auftrag Abschnitt 5), abnehmen (Wachen, Gegenlesen), mergen, pushen.
4. R5 (Rechenweg Aufschlag und manuelle Aufheizzeit, Schema, Testdatenbank, Export), dann O1b/O2, O3, RP1, RP2, A — je mit
   Nachfrage zur Wochennutzung; Aufträge nach dem Muster der bisherigen (`Auftrag_KP3_R4.md`, `Auftrag_KP3_D2.md`: Regeln,
   Worktree, Spurenregel, Was zu bauen, Abnahme mit Gate, Bericht).

## 4. Entscheid E59 (02.10.2026)

Vorgabe: „Die Rampe soll jeweils individuell für ein Gebäude ermittelt werden und nicht pauschal. Ein Aufschlag auf diesen Wert
könnte sinnvoll sein (Benutzervorgabe). Außerdem soll es einen manuellen Wert als Eingabe geben — mit plausiblen Vorschlägen.“
Antworten: manueller Wert **je Gebäude**; Aufschlag in **Stunden und Prozent**, es gilt das Maximum; Umsetzung **nach D2, vor RP1**.
Fachliche Ausgestaltung im Auftrag (Abschnitt 5): Spalten `Tab_Einstellungen.Aufheiz_Aufschlag_H` (0–24) und
`Aufheiz_Aufschlag_Prozent` (0–100), n' = min(48, n + max(Aufschlag_H, ⌈n · Prozent/100⌉)) auf ermittelte n; Art „manuell“ über
`Tab_Gebaeude.Aufheizzeit_Manuell_H` (1–47, NULL = Projektart), Zonen erben; Vorschläge aus der Herleitungszeile und Spanne nach
Bauweise (leicht 1–2 h, mittel 2–4 h, schwer 4–8 h), außerhalb Hinweis statt Sperre; Ergebniszeile `Aufheiz_Bemessung = MANUELL`;
ein Schemaschritt (Nummer spät gegen origin); Wellen R5, O1b, Erweiterung O2/O3; Leitkonzept N1.68 = E59, Festlegungen der
Umsetzung werden N1.69.

## 5. Auftrag KP3-E59 (Papiere) — Wortlaut für den Agenten

Der Auftrag liegt als Datei `scratchpad/kp3/Auftrag_KP3_E59_Papiere.md`; falls das Scratchpad nicht mehr besteht, gilt dieser
Abschnitt: Regeln wie in allen KP3-Aufträgen (nur im Worktree `.claude/worktrees/kp3-e59`, Zweig `kp3-e59`; Commits sofort mit
Trailer Opus 5.5 und Sitzungskennung; kein Push, Merge, CI-Lauf; Markdown ohne BOM; `DokumentationLinkWache` grün). Zu schreiben:
Entwurf KP3 (Wellenplan R5/O1b, Schemaschritt-Tabelle, Festlegungen 34 ff., F9 mit E59, Nachweise N-AH11 Aufschlag und N-AH12
manuell, Ergebnis in Kürze, Aufwand), Teilkonzept Konditionierungsprofile (4.6, 4.8, 5.3, Abschnitt 7 Gebäudedialog, 9.9 E59),
Leitkonzept (N1.68 = E59, Festlegungen der Umsetzung → N1.69 mit allen Verweisen), Register Offene Entscheide (E59 entschieden),
Statusdatei Gebäudesimulation (Zeile E59, KP3-Zeile), `Status_iOS_Migration.md` Block „Nach #669“ Punkt (f), Protokoll KP3
(Abschnitte 1 und 7), Glossar (Aufschlag, Aufheizzeit manuell, Vorschlag). Abnahme: Wachen grün, Gegenlesen der Konzepttexte
mit `seit (dem|der|W)|geändert|Entscheid|Befund|Stand:? *\d|bisher|früher|vorher` (Treffer nur in 9.9, N1.68, F9), Bericht mit
Commits, Abschnitten, Festlegungsnummern, Alternativen.
