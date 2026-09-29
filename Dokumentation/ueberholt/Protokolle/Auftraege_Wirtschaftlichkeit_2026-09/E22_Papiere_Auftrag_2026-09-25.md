# Auftrag Papiere E22 — Nr. 18 Engine-Sortierung auf die HB1-Regel „99", Neueinfrierung R15, Statusnummer NACHTRAG-E22-NR, kein Schemaschritt (25.09.2026)

Merge NACHTRAG-E22-MERGE (pm ab origin; e22 ab cbed6dba). Gate/CI: Platzhalter NACHTRAG-E22-GATE / NACHTRAG-E22-CI. Muster:
`E19_Papiere_498_Auftrag_2026-09-25.md`, Statuszeile #498 und Nach #498; für die Basis das Muster der R14-Einfrierung (Statuszeile und
Protokoll der Welle, die R14 einfror — per `git log --grep=R14` finden). Statusnummer beim Merge gemessen (Startnachricht).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile NACHTRAG-E22-NR (E22 — die acht Rechenweg-Leser der Anlagen sortieren nach `Ladeordnung.SqlAnlagenprio` wie das
Hydraulikbild (ungepflegte Priorität nach hinten, Regel „99"); Referenzbasis neu eingefroren als R15, weil Projekt 1042 die Modulreihenfolge
der zwei Wärmepumpen tauscht; Anwenderentscheid 25.09.2026 „Nr. 18: so umsetzen"; kein Schemaschritt, keine Datenänderung) für EPOS-Plan.
Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge, kein Stash. ARBEITSORT: Worktree
`.claude/worktrees/papiereE22` (Zweig `papiereE22` ab NACHTRAG-E22-MERGE); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus
`cd .claude/worktrees/papiereE22`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5
<noreply@anthropic.com>`. Formregeln wie #498; Python `"C:\Program Files\Python312\python.exe"` binär, nie `sed -i`; deutsche
Anführungszeichen im Python-Quelltext als \u201e/\u201c. **Die Basisnamen-Ersetzungen in CLAUDE.md, kern.yml, ios.yml, Referenzlaeufe/LIESMICH.md
und den sieben Papieren hat E22 selbst gemacht — prüfen (`git grep -n R14_Kaelteerzeuger` darf nur noch Geschichte in ueberholt/ und die
Archivliste treffen), nicht doppelt ändern.**

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\mess18_bericht.md` (Messwelle) und `e22_berichte.md` (Bau laut Startnachricht:
Commits, A/B-Tafeln Punkt 1 und Punkt 1+2, E22‑Q1-Entscheid zu den drei Modul-Ladern, Determinismus, Dateien der Neueinfrierung, Fundstellen
des Basisnamens, Testzahlen), `E22_Auftrag_2026-09-25.md`; Konzept § 6.3 Nr. 18 und Register R‑Rest; `HB1_Hydraulikbild_Sortierung_Protokoll.md`
(HB1-O1); `Referenzlaeufe/LIESMICH.md` (neuer Abschnitt Aktuelle Basis R15, von E22 geschrieben) und `Dokumentation/ueberholt/Referenzbasen/
LIESMICH.md`. Alles ganz lesen.

AUFGABEN: (1) Statusdatei: Zeile NACHTRAG-E22-NR (Anlass: Anwender 25.09.2026 „Nr. 18: so umsetzen, Umbau … Regel ‚99'") nach der letzten Zeile
vor `---` und Block Nach NACHTRAG-E22-NR: (a) E22‑Q1 (drei Modul-Lader, Entscheid laut Bau), (b) Abnahme A‑E22‑1 (1042: Modul 1 = WP mit
Priorität 1 in Ergebnis, Bericht und Hydraulikbild), (c) Nachweis (A/B gegen R14: nur 1042 aggregate.csv 10 Werte Index; Determinismus zwei
Läufe byte-gleich; Referenzlauf 13/13 gegen R15; kein Test rot), (d) Befunde (HB1-O1 geschlossen; Kennzahlen unverändert; die drei Lader),
(e) Papiernachzug (Basisname an allen Stellen, Archiv R14), (f) Logbuch (falls sichtbar: Modulreihenfolge folgt der Priorität — sonst „kein
Eintrag" begründen), (g) nächste Schritte (Kandidaten 1018-Träger/1023 für die nächste Neueinfrierung), (h) Gate NACHTRAG-E22-GATE, CI
NACHTRAG-E22-CI (CI muss gegen R15 laufen — `kern.yml` geändert). (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/
E22_Anlagenprio_Rechenweg_R15_Protokoll.md` (Muster E19 + Einfrier-Abschnitt wie beim R14-Protokoll), Index +1. (3) Konzept § 6.3 Nr. 18 erledigt
(Einzeiler, Grund im Entscheidwege-Protokoll), Kopfzeile Codestand/Basis R15; Register R‑Rest Nr. 18 „gebaut", Familie R‑E22 Q1; Entscheidwege
§ 8.x; Analysepapier § 5 Zeile E22 und Basisvermerk; `Dokumentation/aktuell/Umsetzung_iU10_Nachweise.md` o. ä., falls dort die Basis genannt
wird (prüfen per grep, E22 hat sieben Papiere geändert). (4) `Dokumentation/ueberholt/Referenzbasen/LIESMICH.md`: Eintrag R14 mit Datum und
Grund der Ablösung prüfen/ergänzen. (5) Kein Mockup (kein Dialog), Wiki nur falls Logbuch nötig. (6) Bytes, `git diff --stat`, Bericht ohne
Dateiabzüge, verbliebene Platzhalter.
