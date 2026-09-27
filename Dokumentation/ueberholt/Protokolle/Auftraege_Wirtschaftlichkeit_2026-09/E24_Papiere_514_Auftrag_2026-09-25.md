# Auftrag Papiere E24 — Datenpflege 1018/1023 (Kessel-Träger 63, Erdgaspreis 1023), Neueinfrierung R17, Statusnummer #514, kein Schemaschritt (25.09.2026)

Merge edf89ae8 (pm26 ab origin 822ba803; e24 = 7da6bb81 ab 822ba803). Gate/CI: Platzhalter NACHTRAG-514-GATE / NACHTRAG-514-CI. Muster:
`E22_Papiere_Auftrag_2026-09-25.md`, Statuszeile #503 und Nach #503 (R16-Einfrierung). Statusnummer #514 ist gemessen (origin 822ba803 = #513).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #514 (E24 — Datenpflege der Testdatenbank: 1018 Kessel 10369 und 1023 Kessel 11205 erhalten Träger 63 Erdgas E, 1023
erhält eine Erdgas-Preiszeile (energy_project_settings 10130, energy_price 10185, 0,84 €/Nm³, 1.200 €/a, Hi 10,5, co2 240 auf Projektebene) und
rechnet damit erstmals Energiekosten und Kapitalwert; Referenzbasis neu eingefroren als `Referenzlaeufe/2026-09-25_R17_Datenpflege` (14 Projekte,
432 CSV, nur 1018/1023 aggregate.csv `HeizkesselModul[0].carrier_id` leer → 63, sonst byte-gleich), R16 archiviert; Anwenderentscheid 25.09.2026
„nehme die Empfehlungen vor: für Später" zu Konzept § 6.3 Nr. 24; kein Schemaschritt, Testdatenbank-LFS 76dd9e48 → 0c2fe21a, Schemastand 143
unverändert) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge, kein Stash.
ARBEITSORT: Worktree `.claude/worktrees/papiereE24` (Zweig `papiereE24` ab edf89ae8); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus
`cd .claude/worktrees/papiereE24`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5
<noreply@anthropic.com>`. Formregeln wie #503/#513; Python `"C:\Program Files\Python312\python.exe"` binär, nie `sed -i`; deutsche
Anführungszeichen im Python-Quelltext als „/“. **Die Basisnamen-Ersetzungen (CLAUDE.md, kern.yml, ios.yml, Dokumentation/LIESMICH.md,
Referenzlaeufe/LIESMICH.md, Konzept_Gebaeudesimulation, Systementwurf, Konzept_Wirtschaftlichkeit Kopf/Tabelle/Nr. 21, Analysepapier Kopf/Legende,
Basenhistorie) hat E24 selbst gemacht — prüfen (`git grep -n R16_Anlagenprio` darf nur noch Geschichte treffen: Protokolle, Statuszeilen, Register
731, Konzept 2819/3179, Analyse 152/591, Zapfprofil-Konzept 3172, Archivliste), nicht doppelt ändern.**

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e24_berichte.md` (Phase 0 und Phase 1: Pflegewerte, Zellvergleich, A/B-Tafel, Dateien der
Einfrierung, Fundstellen, Testzahlen, benannte Kessel ohne Träger, BETRIEB_SQLITE-Hinweis, Abnahme), `e21_berichte.md` (Befundtafel, Anlass),
`E24_Auftrag_2026-09-25.md`; Konzept § 6.3 Nr. 24 und Register R‑Rest; `Referenzlaeufe/LIESMICH.md` (Abschnitt Aktuelle Basis R17, von E24
geschrieben) und `Dokumentation/ueberholt/Referenzbasen/LIESMICH.md` (R16-Eintrag von E24). Alles ganz lesen.

AUFGABEN: (1) Statusdatei `Dokumentation/aktuell/Status_iOS_Migration.md`: Zeile #514 (Anlass: Anwender 25.09.2026 „nehme die Empfehlungen vor: für
Später", Konzept § 6.3 Nr. 24) nach der letzten Zeile vor `---` und Block Nach #514: (a) E24‑Q1…Q6 (Entscheide laut Phase 0: co2 240 wie Muster,
energy_price-Zeile ja, nur 1018/1023, kein Gaspreis 1018, nur Fakten-Tests, Ordnername), (b) Abnahme A‑E24‑1 (1023 „Wöhler - Test1" neu rechnen →
Energiekosten Erdgas und Kapitalwert, Kessel mit „Erdgas E"; 1018 Kessel zeigt „Erdgas E", Emissionen unverändert), (c) Nachweis (Zellvergleich
10.645.701 Zellen: 2 Zellen, 2 neue Zeilen, 2 Zähler; A/B gegen R16 nur zwei carrier_id-Skalare; Determinismus; Referenzlauf 14/14 gegen R17;
Testzahlen), (d) Befunde (Rolle „ohne Nachweis" hängt an gespeicherten Ergebniszeilen, nicht an den Daten — Tests unverändert; Em.Kessel.Co2T
unverändert, weil Projektwert 240 greift; Kessel ohne Träger 1007/1008/1017/1046/1047, 1024 = 0, 1018 ohne Gaspreis als Prüffall; E24/4 Nm³ 17→18),
(e) Papiernachzug (Basisname an allen Stellen, Archiv R16), (f) Logbuch: kein Eintrag (Testdaten, nicht Anwendersicht — begründen), (g) Hinweis
BETRIEB_SQLITE: in `Dokumentation/aktuell/BETRIEB_SQLITE.md` einen kurzen Absatz „Trägerzuordnung der Kessel prüfen" (Live-DB: Kessel ohne
ID_Carrier fallen auf EMISSION_OHNE_TRAEGER_KESSEL_* zurück, ohne Erdgas-Preiszeile kein Kapitalwert; Pflege nur nach Entscheid des Anwenders,
Muster e24_pflege), (h) nächste Schritte (E25 Prüfprojekt 1048 läuft; Restpunkte 1018-Puffer, 1024, 1030, 1026), (i) Gate NACHTRAG-514-GATE, CI
NACHTRAG-514-CI (CI läuft gegen R17 — kern.yml/ios.yml geändert). **Zusatz:** in der Statuszeile #513 den CI-Vermerk „steht aus (Beobachtung nach dem
Push)" ersetzen durch „grün, Läufe 36161752595, 36161752448, 36161746706 auf 822ba803". (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/
E24_Datenpflege_1018_1023_R17_Protokoll.md` (Muster E22-Protokoll mit Einfrier-Abschnitt; Pflegetafel, Zellvergleich, A/B, Fundstellen), Index +1.
(3) Konzept § 6.3 Nr. 24 erledigt (Einzeiler: 1018/1023 gepflegt, R17; Rest 1018-Puffer, 1024 kein Datenfehler, 1030 Anker, 1026 Prüffall bleiben
benannt), Kopfzeile Codestand edf89ae8/Basis R17 prüfen; Register R‑Rest Nr. 24 „gebaut", Familie R‑E24 Q1…Q6; Entscheidwege § 8.x; Analysepapier § 5
Zeile E24 (#514) und Basisvermerk. (4) `Dokumentation/ueberholt/Referenzbasen/LIESMICH.md`: Eintrag R16 mit Datum und Grund der Ablösung prüfen/
ergänzen. (5) Kein Mockup, kein Wiki. (6) Bytes, `git diff --stat`, Bericht ohne Dateiabzüge, verbliebene Platzhalter.
