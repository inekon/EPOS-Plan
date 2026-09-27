# Auftrag Papiere E27 — BHKW-Überschuss: Netzbezug nie negativ (N5), Referenzbasis R19, Statusnummer #520, kein Schemaschritt (25.09.2026)

Merge 80a7b9fb (pm26 ab origin 50ecd802 [#519]; e27 = a96500eb ab ba798d8a). Gate/CI: Platzhalter NACHTRAG-520-GATE / NACHTRAG-520-CI. Muster:
Statuszeile #518 und Nach #518 (E26, R18-Einfrierung), `E26_Papiere_518_Auftrag_2026-09-25.md`. Zusatz: CI-Vermerk des Pushs 50ecd802 in der
Statuszeile #519 nachtragen (Wortlaut folgt per Nachricht; bis dahin Platzhalter NACHTRAG-519-CI-NEU einsetzen).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #520 (E27 — Befund N5 aus E26: Nach der Kaskade blieb der BHKW-Überschuss ungeklemmt im Reststromvektor, wenn keine
PV-/Speicherstufe folgte → negativer Netzbezug (1018 −27,46 MWh), im Rollentarif Gutschrift der Reststromkosten trotz Einspeisung im KWK-Split
(Doppelgutschrift), CO₂-Gutschrift; jetzt Klemme am Laufende `SimulationControl.NetzbezugGeklemmt` (nur Werte < 0 → 0, nicht bei Speicherflotte;
Q1 a/Q5 a) und `BhkwReststrombedarfMwh` je Stunde geklemmt (Q4 a); Kapitalwert-Anker 1030 neu (Q2 a); Referenzbasis neu eingefroren als
`Referenzlaeufe/2026-09-25_R19_BhkwNetzbezug`, R18 archiviert, Wirkung nur 1018 und 1030 (4/432 CSV); Anwenderentscheid 25.09.2026 „E27: nach
Empfehlung bauen"; kein Schemaschritt; Testdatenbank unverändert — e27 fror R19 auf der Fassung 19a7b632 ein, der Merge auf pm26 bringt die Fassung
b68638da mit Prüfprojekt 1048 [#519], Referenzlauf 14/14 gegen R19 danach wiederholt: Nachweis in Nach #520 (i)) für EPOS-Plan. Antworten auf
Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge, kein Stash. ARBEITSORT: Worktree `.claude/worktrees/papiere520`
(Zweig `papiere520` ab 80a7b9fb); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere520`, nie im Hauptbaum. Commits sofort
mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Formregeln wie #518/#519; Änderungen mit dem Edit-Werkzeug,
Zeilenenden erhalten; Datum mit `date` prüfen. **Die Basisnamen-Ersetzungen R18 → R19 hat E27 selbst gemacht** (20 Stellen, Liste in den Fakten; der
Merge 80a7b9fb hat die Köpfe von Konzept und Analysepapier zusammengeführt — Konzept-Kopf Codestand noch `3936003c`, bitte auf `80a7b9fb` setzen) —
prüfen (`git grep -n R18_PvAusweis` darf nur noch Geschichte treffen), nicht doppelt ändern.

FAKTEN (ganz lesen): `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e27_berichte.md` (Phase 0: Ursache, Leser des negativen Rests, Wirkung je
Projekt, Ausweisstellen, Fragen Q3…Q8 mit Entscheiden; Phase 1: Commits, Zahlen vorher/nachher 1018/1030 inkl. CO₂ und Kapitalwert, Tests, A/B-Tafel
R18→R19, Dateien der Einfrierung, Fundstellen, Konzeptvermerk-Satz, Logbuch-Vorschlag, Abnahme, Restpunkte), `e26_berichte.md` (N5-Herkunft),
`E27_Auftrag_2026-09-25.md`; Konzept § 3.6 (Strommatrix, Vermerk aus E26), § 6.3 (Nr. 34 E26 nennt N5 als Restpunkt); Register R‑E26 Q7 (N5
gemeldet); `Referenzlaeufe/LIESMICH.md` (Abschnitt Aktuelle Basis R19, von E27 geschrieben, beim Merge um den Nachtrag Prüfprojekt 1048 ergänzt) und
`Dokumentation/ueberholt/Referenzbasen/LIESMICH.md` (R18-Eintrag von E27).

AUFGABEN: (1) Statusdatei: Zeile #520 nach #519 vor `---` (Anlass, Ursache mit Fundstellen, Änderung je Datei, Zahlen 1018/1030 vorher/nachher mit
CO₂ und Kapitalwert, Tests 14.803/2/0 und neue Klasse `BhkwNetzbezugKlemmeTests` 7 Fälle, Anker 1030 neu, SQL 1.927/0, ChartProben 174/0, A/B
R18→R19 4/432 CSV, Determinismus, Basisname 20 Stellen, Merge 80a7b9fb, Zweig e27 Commits bba74bca/8aa53a99/13ae6216/a96500eb; **Gate:**
NACHTRAG-520-GATE; **CI:** NACHTRAG-520-CI; Logbuchsatz aus dem Bericht als Vorschlag mit „Version beim Anwender erfragen"; Anwendersicht:
Netzbezug nie negativ in Ergebnisansicht, BHKW-Reiter, Übersicht, Kennzahlen, Bericht, Excel; Reststromkosten ohne Gutschrift; 1018 CO₂ +11,95 t/a)
und Block Nach #520 vor Nach #519: (a) E27‑Q1…Q8 mit Entscheiden (Q1/Q2 Anwender, Q3…Q8 Orchestrator nach Empfehlung), (b) Abnahme A‑E27‑1 (vier
Punkte), (c) Nachweis (Zahlen, A/B, Determinismus, Referenzlauf 14/14 gegen R19 vor und nach dem Merge mit 1048 — Platzhalter NACHTRAG-520-REF für den
Lauf nach dem Merge), (d) Befunde (Klemme nicht an der BHKW-Stufe, weil spätere Verbraucher derselben Viertelstunde und PV den Überschuss brauchen; PV
nach BHKW byte-gleich; 1018 ohne Kapitalwert in der Testdatenbank; Anker-Klassen lesen gespeicherte Ergebnisse; Nachschliff 8aa53a99 für bitgleiche
1017/1024/1047; Phase 0 Nachher-Werte gerechnet, weil ein temporärer Kern-Eingriff abgelehnt wurde), (e) Papiernachzug (Basisname, Archiv R18,
LIESMICH-Zusammenführung mit 1048-Nachtrag), (f) Logbuch-Vorschlag, (g) Restpunkte (Q3 b eigene Ausweisgröße „BHKW-Einspeisung"; N7 Vorab-Überschuss
PV-Modus der WP und Kessel-Vektorstufe hinter BHKW — Empfehlung: Prüfwelle E28 nach Anwenderentscheid; Q6 Altergebnisse), (h) nächste Schritte, (i)
Gate/CI-Platzhalter (CI läuft gegen R19 — kern.yml/ios.yml geändert). (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/
E27_BhkwNetzbezug_Klemme_R19_Protokoll.md` (Muster E26 mit Einfrier-Abschnitt), Index +1. (3) Konzept: § 3.6 den Konzeptvermerk-Satz aus dem Bericht
einarbeiten; § 6.3 Nr. 34 Restpunkt N5 als erledigt durch E27 vermerken und neue Nr. (fortlaufend, 36) für E27 erledigt mit Restpunkten Q3 b/N7;
Kopfzeile Codestand 80a7b9fb; § 7 Schrittabsatz E27. (4) Register: Familie R‑E27 Q1…Q8, R‑E26 Q7 Umsetzungsstand „N5 gebaut #520", Kopf/Familientafel;
Entscheidwege § 8.x E27. (5) Analysepapier § 5 Zeile E27 (#520), Nachtrag, Kopf Codestand 80a7b9fb (Legende ist schon „seit E27 gilt R19"). (6)
Archiv-LIESMICH: R18-Eintrag mit Datum/Grund prüfen. (7) Kein Mockup; Wiki: kein Fachseiten-Text, nur der Logbuch-Vorschlag. (8) Bytes, `git diff
--stat`, Bericht ohne Dateiabzüge, verbliebene Platzhalter, Zeilennummern.
