# Übergabe KP3 — Stand beim Anhalten am 02.10.2026, fortgeschrieben am 03.10.2026

**Gültig ist Abschnitt 7 (Stand 03.10.2026); die Abschnitte 1–6 beschreiben den Stand vom 02.10.2026 und bleiben als Herleitung.**

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
| Merge origin | #665–#668 (Solarthermie-Ganglinie, M1a, M3a, **M2 mit Basis R32**, Testdatenbank 165) | gemergt (`8a73e236`); KP3 friert künftig als **R34** ein (`df81974b`) |
| Gate 669 | volles Gate im Hauptbaum auf dem Merge-Stand | siehe Abschnitt 2 |
| D2 | Kennzahlen je Gebäude und Zone, Sommerlüftung NULL, Export E32, Bedarfsergebnis, Auskunft, Herleitungszeile, Hinweis Verbrauchsangabe | Agent fertig gebaut (fünf Commits im Worktree `kp3-d2`, Zweig `kp3-d2` auf `4b4a84e2`), Gate 670 im Worktree grün (Abschnitt 6); **Abnahme offen** |
| E59 | individuelle Rampe, Aufschlag (h und %), manuelle Aufheizzeit je Gebäude mit Vorschlägen | **entschieden 02.10.2026**, Papiere noch nicht geschrieben (Auftrag in Abschnitt 5), Welle R5/O1b vor RP1 |
| O2, O3, RP1, RP2, A | Bedarfsdialog; Bericht; Referenzprojekt 1051 mit Messung ρ_min; Basis R34; Abschluss | offen |

**Beim Anwender offen:** SA1 (KP2) und SA-KP3 unter Windows, Logbuch-Versionsnummer („noch offen“ → Platzhalter „Version <vom
Anwender>“), Entscheid P14 (ρ) nach der Messung in RP1.

## 2. Zustand von Hauptbaum, origin und Worktrees beim Anhalten

- Hauptbaum `/home/user/EPOS-Plan`, Zweig `ios_migration_september`: lokaler Kopf siehe Statuszeile #669 und die Commits danach
  (`git log --format='%h %<(72,trunc)%s' -n 20`); der Push nach origin erfolgt nach grünem Gate 669 (Eintrag im Block „Nach #669“).
- Worktree `.claude/worktrees/kp3-d2` (Zweig `kp3-d2`, Basis `4b4a84e2` = R4 + origin #665–#667, Testdatenbank 164): Commits
  `ed7bf075` Ergebniszeile, `33ea28be` Export, `bdd08341` Auskunft und Herleitungszeile, `7fb53e94` Hinweis Verbrauchsangabe,
  `5ac9377e` N-AH7 Zeile/Export. Gate-Ablage `/tmp/gate_d2/GATE670`. Abnahme morgen: Bericht lesen, `git merge --no-ff kp3-d2`
  (Konflikte an `.resx` wie gehabt: origin-Stand plus neue Schlüssel, `designer_neu.py schreiben`), Gate im Hauptbaum gegen die
  Basis **R33** (16 Projekte, `Referenzlaeufe/2026-10-02_R33_Viertelstunden`, Testdatenbank Schemastand 168; origin #670–#674 am 03.10. gemergt), Statuszeile (nächste freie Nummer **spät gegen origin**),
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
4. **IFC-Import, Befund aus der Sichtprobe des Anwenders (02.10.2026, Datei `MFH_mittel_1984.ifc`, IFC4, nicht im Repository):** Hinweis „führt keine Raumgrenzen“ (`IMP_IFC_PROT_KEINE_GRENZEN`, Σ Trennfläche 0 m², Z5 eine Zone), Gebäudename bleibt Vorgabe „Gebäude“, Baualtersklasse F (1969–1978) trotz „1984“ im Dateinamen, „18 Werte aus der Datei, 14 Vorgaben, 6 leer“. Auftrag: Ursache je Befund finden und Behebung prüfen (Opus-Agent im Worktree `ifc-befund`, Auftragsdatei `scratchpad/Auftrag_IFC_Befund.md`; Kern: Lesen von `IfcRelSpaceBoundary`/`2ndLevel` und Rückfallweg, Gebäudename aus `IfcBuilding.Name`/Dateiname, `Baujahrregel` gegen die IWU-Tabelle und E47, die sechs leeren Felder; minimale IFC4-Probe im Scratchpad; kleine Behebungen mit Tests committen, Größeres als Vorschlag). Vorher Wochennutzung erfragen; die Datei beim Anwender anfordern, wenn die Probe den Befund nicht nachstellt.
5. R5 (Rechenweg Aufschlag und manuelle Aufheizzeit, Schema, Testdatenbank, Export), dann O1b/O2, O3, RP1, RP2, A — je mit
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

**E60 (03.10.2026, Vorschlag 1 des [Konzepts Heizlastspitzen](2026-10-03_Konzept_Heizlastspitzen_Glaettung.md)):** Auslegungsgröße =
stationäre Auslegungsheizlast + Aufheizleistung aus der KP3-Bemessung; O2 und O3 zeigen ideale Spitze, Tagesmittel und P_auf nebeneinander;
kein Filter im Rechenweg. Der E59-Papierauftrag trägt E60 mit ein (Register, Statusdatei Zeile E60, Teilkonzept 4.8/7.6, Entwurf KP3 Zeilen
O2/O3).

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

## 6. Bericht des D2-Agenten (eingegangen nach dem Anhalten; Abnahme am 03.10.)

**Gate 670 im Worktree** auf `5ac9377e` gegen R31: Kern-Filter 0 Fehler, ChartProben JA (200), Kern 10 116 (1 übersprungen), UI 7 284,
KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), Wachen 35/35, Referenzlauf 16/16 PASS, 487/487 CSV byte-gleich,
gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer ohne Abweichung, beide `.resx` je 13 688 Einträge ohne Dubletten,
SqlDialektPruefer 2 151 Texte 0 Fundstellen. **Zwölf neue Schlüssel je Sprache:** elf `SIMKONF_AUFH_HRL_*`/`SIMKONF_AUFH_QUELLE_*` für die
Herleitungszeile, `SIMENG_AUFH_VERBRAUCH`. Beim Merge in den Hauptbaum (Basis R32, 13 700 Einträge) die `.resx` wieder aus dem
Hauptbaum-Stand plus diesen zwölf Schlüsseln bauen, `designer_neu.py schreiben`, Gate gegen R32.

**Gebaut:** Ergebniszeile und Zonenzeile mit allen 14 Aufheizspalten und Sommerlüftung NULL (Festlegung 26, `GebaeudeKennzahlen.Aufheizwerte`
als einzige Stelle der NULL-Regeln; `ErgebnisGebaeudeTests:260` und `GebaeudeBedarfNachtauskuehlungTests:98` nachgezogen); Export nach E32
(Texte `Aufheizzustand`, `Aufheizbemessung`, `Aufheizleistungsquelle` hinter `Geb[n].Modell`, elf Zahlen nur bei Zustand, Zonenschlüssel
`Geb[n].Zone[k].*`, `heizsollwert_<n>.csv` bei Heizkalender oder Aufheizung ≠ null, Nachtauskühlung nur gesetzt — in den 16 Projekten
unverändert); `GebaeudeBedarfErgebnis.Ergebniszeile` und `GebaeudeBedarfZone.Ergebniszeile`; Auskunft der Aufheizbemessung ohne Jahreslauf
**mit Konditionierungssatz** (= Lauf bitgleich an 17 VDI-Gebäuden, 1018 mit Faktor, Mehrzonen in drei Fällen, 10632 mit Heizkalender:
14 h/182 Sprünge statt 5 h/365 ohne Satz — B14); Herleitungszeile in `SimulationErgebnisHuelle.ParameterGaben` (de/en, 16 Hüllenfälle,
Beispiel „Hotel-G-136: t_auf,max 5 h bei -9,3 °C (kälteste Stunde) · P_auf 34,6 kW Zielleistung (Katalogbau 136,9 kW × Faktor 0,253)“);
Hinweis `SIMENG_AUFH_VERBRAUCH` einmal je Gebäude nur bei geplantem Zustand mit Rampentag; N-AH7 für Zeile und Export (zwei Läufe,
de-DE/en-US, gestörter Lauf: gleiche Zustände, Zähler, Schlüssel).

**Abweichungen von der Spurenregel (zu bestätigen):** (1) Kennzeichen `SommerlueftungGesetzt` und `HeizkalenderWirksam` als
init-Eigenschaften in `GebaeudeModellErgebnis`, gesetzt in `Vdi6007Rechenweg.Laufen`, `Zonenlauf.Ergebnis`, `Zonenrechnung.Gebaeudeergebnis`,
von `Skaliert` getragen — reine Kennzeichen. (2) Auslagerungen für die Auskunft (Regel „Auskunft ruft den Rechenweg“): `Vdi6007Rechenweg.EingangBauen`,
`ZonenBauen`, `Mehrzonenweg`, `Zonenklima`, `Zonenkonditionierung`; `Zonenrechnung.ZonenBauen`; der Lauf ruft dieselben Methoden,
Referenzlauf byte-gleich. Vorläufige Bewertung der Orchestrierung: beides annehmbar, (1) ist Festlegung 26/28 geschuldet, (2) der Kern-Regel;
beim Merge bestätigen und im Protokoll als Festlegung D2 führen.

**Befunde:** `Tab_ErgebnisZone.Aufheiz_Zustand` kennt GEKOPPELT laut CHECK nicht, obwohl R3 eine Zone im Mehrzonenweg (AK1 als ideale
Last, 1047) so setzt — D2 hält die Zonenzeile ohne Aufheizwerte, Zustand am Gebäude; **Schemanachtrag entscheiden** (Kandidat für den
E59-Schemaschritt in R5). B14 bleibt für die Übergabe-Auskunft (H10) offen: `UebergabeEingang`/`KuehluebergabeEingang` bauen ohne
Konditionierungssatz. Mehrzonen mit Regelpaaren: Auskunft braucht den adiabaten Vorlauf, also je Zone ein Jahr. B11: mit Verbrauchsangabe
bleibt der Faktor bis zum Lauf offen (Herleitungszeile nennt P_auf am Katalogbau und „erst der Lauf“).

**Festlegungen D2:** P_auf in Spalte und Export nur endlich und > 0 (+∞ der Testnaht und `Heizleistung_Max` = 0 werden NULL; die Auskunft
behält +∞ roh); Faktor in der Auskunft: Flächenangabe wie der Lauf, Gebäude mit Zone 1, Verbrauchsangabe `null`; feste Nennleistung geht
nicht ein; C_w nicht in der Herleitungszeile; je eine Zeile auch für Tagesbilanz-Gebäude und Fehler des Eingangsbauers.

**Offen für O2:** Darstellung aus `Ergebniszeile` (Gebäude, Zonen), Sommerlüftung je Zone, „—“ für NULL, KI-Sicht. **O3:** Bericht und
Vergleich lesen die Spalten, Abweichungsmerkmale B21, gekoppelte Zone ohne Zonenzeile. **RP1:** 1051 erzeugt `heizsollwert_<n>.csv` und
die Aufheizschlüssel — erwartet, kein Befund.

## 7. Stand 03.10.2026 (fortgeschrieben, gilt)

| Welle | Stand |
|---|---|
| D2 | gemergt und gepusht, Statuszeile **#679** (Gate 677/678 grün gegen R33), CI-Lauf 37114265194 grün |
| IFC-Befund | #680: Baujahr-Rückfall, Dateiname als Namensvorschlag, Hinweis ohne Raumgrenzen |
| E59/E60-Papiere | #681: Entwurf KP3 (R5, O1b, KP-S4 mit `Aufheiz_Art`, Festlegungen 34–43, F9, N-AH11/12), Teilkonzept 9.9, Leitkonzept N1.68 (Festlegungen der Umsetzung → N1.69), Register; Folgeentscheide P15 (Spanne aus τ₂), P16 (Aufschlag nur auf Rampen eines Kalendersprungs, n > 1), P17 (Auslegungsgröße = Auslegungsheizlast + (P_auf − Φ_stat)), Schema A1 |
| IFC-Folgewelle | #682 (Trenndecken und innere Masse über Raumbezüge, Platzhaltername, Jahr im Dateinamen) und **#684** (Trenndeckenfläche aus Raummengen, Erklärung der Datei vor Bezug); #683 ist die Pufferauslegung P2 einer anderen Sitzung |
| R5 | **läuft** (Opus, Worktree `kp3-r5`, Zweig `kp3-r5` auf `51fb613d`): Schemaschritt **174** angemeldet (173 gehört KU1 Stufe 2; Testdatenbank im Worktree auf 173 gehoben — beim Merge gegen origin prüfen, ob 173 schon liegt, sonst Kette und Nummer nachziehen), Commits bis `f5c8bb42`, Gate 682 im Worktree läuft; Auftrag `scratchpad/kp3/Auftrag_KP3_R5.md` |
| G6d | **nächste Welle** nach R5: Auftrag `scratchpad/kp3/Auftrag_G6d.md` (Zonenprojekt 1052 als Kopie von 1018 mit drei Zonen, Wache, Regeltext, kein Einfrieren); vorher Wochennutzung erfragen |
| danach | RP1 (1051, Messung ρ_min → P14), RP2 (Basis **R34** mit 18 Projekten — R34 ist im Kopf der Statusdatei für KP3 angemeldet), O1b/O2/O3, A |

**Regeln, die heute dazukamen (Kopf der Statusdatei):** Schemaschritte und Referenzbasen werden **vor dem Bau angemeldet** (Zeile im Kopf von
`Status_iOS_Migration.md`, sofort gepusht); Statusnummern spät gegen origin — heute waren #665–#668, #677, #678, #683 fremd vergeben.
Merges mit origin mehrmals täglich; die `.resx` werden bei Konflikt aus dem origin-Stand plus den eigenen Schlüsseln neu gebaut
(`designer_neu.py schreiben`), die Testdatenbank per `git lfs smudge` aus dem origin-Zeiger geladen.

**Beim Anwender offen:** `MFH_mittel_1984.ifc` unter `Quellen/`, Logbuch-Version (zwei Logbuchsätze des Gebäudeimports in #680/#684),
Wiki-Upload Gebäudeimport, Sichtproben unter Windows (KP2, KP3, IFC mit beiden Dateien), Spitzboden „Wohnraum“ der 1964er Datei, P14 nach RP1.
